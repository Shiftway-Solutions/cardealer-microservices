/**
 * ModelManager — Live Copilot model management for the Agent Monitor.
 *
 * Responsibilities:
 *   1. Fetch the live model list from VS Code's LM API
 *      (vscode.lm.selectChatModels({ vendor: "copilot" }))
 *
 *   2. FILTER out models that require "Upgrade" — probes each model with a
 *      minimal request (1 token); removes those that fail with upgrade/billing
 *      errors. This keeps the model list in sync with what's actually usable.
 *
 *   3. Classify models as 0x (free tier) or 1x (quota-consuming) using
 *      a configurable `copilotMonitor.zeroXModels` setting.
 *
 *   4. Switch the active model using the internal VS Code command
 *      workbench.action.chat.changeModel({ vendor, id, family })
 *      — the same mechanism the chat UI dropdown uses.
 *
 *   5. Register one VS Code command per live model at runtime:
 *        copilotMonitor.selectModel.<safe-id>
 *      These commands are callable from agent action logic without showing UI.
 *
 *   6. Show a VS Code QuickPick containing only the usable live models
 *      (used by copilotMonitor.switchModel command in the palette).
 *
 *   7. Export model list to /tmp/copilot_live_models.json
 *      — shared with change_model.py so both tools stay in sync.
 */

import * as vscode from "vscode";
import * as fs from "fs";
import { PremiumRequestEntry, PremiumRequestStats } from "./types";

const LIVE_MODELS_PATH = "/tmp/copilot_live_models.json";
const CMD_CHAT_CHANGE_MODEL = "workbench.action.chat.changeModel";

/**
 * 0x models confirmed to support multimodal vision input (GitHub Pro, April 2026).
 * Source: https://docs.github.com/en/copilot/concepts/billing/copilot-requests#model-multipliers
 * and the "Working with visuals" section of the model comparison page.
 * Order = fallback priority for the brain (best → acceptable).
 * NOTE: raptor-mini is 0x but has NO vision — must NOT appear here.
 */
export const ZERO_X_VISION_FAMILIES: readonly string[] = [
  "gpt-4.1", // 0x, vision ✅ — confirmed primary (empirically verified via API)
  "gpt-5-mini", // 0x, vision ✅ — confirmed secondary (empirically verified via API)
] as const;

// Error messages that indicate a model requires an upgrade/purchase
const UPGRADE_ERROR_PATTERNS = [
  "upgrade",
  "subscription",
  "billing",
  "purchase",
  "not available",
  "access",
  "unauthorized",
  "403",
  "payment",
];

export interface LiveModel {
  id: string;
  name: string;
  family: string;
  vendor: string;
  maxInputTokens: number;
  isFree: boolean; // true = 0x (no quota cost), false = 1x (uses request quota)
}

type LogFn = (level: "debug" | "info" | "warn" | "error", msg: string) => void;

export class ModelManager {
  private _liveModels: LiveModel[] = [];
  private _dynamicCmds: vscode.Disposable[] = [];
  private _logger?: LogFn;

  // ─── Last applied model (authoritative for PREMIUM GUARD) ───────────────
  // changeModel() updates the session model in memory but does NOT write to
  // github.copilot.chat.languageModel in settings.json. This field tracks
  // what was actually applied, so _sendToChat() can check the real model.
  private _lastAppliedModel: LiveModel | null = null;

  // ─── Premium (1x) request tracking ─────────────────────────────────────
  private _premiumSessions: PremiumRequestEntry[] = [];
  private _premiumPrompts: PremiumRequestEntry[] = [];
  private _onPremiumUpdate?: (stats: PremiumRequestStats) => void;

  // ─── Manual model switch audit callback ──────────────────────────────────
  /** Fired after every dynamic-command model switch (palette/manual). */
  private _onManualModelSwitch?: (
    modelId: string,
    tier: "0x" | "1x",
    switched: boolean,
  ) => void;

  constructor(logger?: LogFn) {
    this._logger = logger;
  }

  // ─── Premium request tracking API ────────────────────────────────────────

  /**
   * Register a callback fired whenever a new premium (1x) event is recorded.
   * Called by Monitor → feeds StatusBar display.
   */
  onPremiumUpdate(cb: (stats: PremiumRequestStats) => void): void {
    this._onPremiumUpdate = cb;
  }

  /**
   * Register a callback fired after every manual model switch via command palette.
   * Used by Monitor to write audit log entries for out-of-cycle model changes.
   */
  onManualModelSwitch(
    cb: (modelId: string, tier: "0x" | "1x", switched: boolean) => void,
  ): void {
    this._onManualModelSwitch = cb;
  }

  get premiumStats(): PremiumRequestStats {
    const log = [...this._premiumSessions, ...this._premiumPrompts].sort(
      (a, b) => a.ts - b.ts,
    );
    return {
      sessionCount: this._premiumSessions.length,
      promptCount: this._premiumPrompts.length,
      totalRequests: this._premiumSessions.length + this._premiumPrompts.length,
      log,
    };
  }

  /** Reset counters (e.g. start of new session or manual clear). */
  resetPremiumCount(): void {
    this._premiumSessions = [];
    this._premiumPrompts = [];
    this._onPremiumUpdate?.(this.premiumStats);
  }

  /**
   * Record that a prompt was sent while a 1x model was active.
   * Call from ActionExecutor whenever _sendToChat() fires on a 1x model.
   */
  recordPremiumPrompt(modelId: string, trigger: string): void {
    this._premiumPrompts.push({ ts: Date.now(), modelId, trigger });
    this._log(
      "info",
      `[1x] Prompt recorded on ${modelId} (trigger=${trigger}) — total prompts: ${this._premiumPrompts.length}`,
    );
    this._onPremiumUpdate?.(this.premiumStats);
  }

  // ─── Public API ───────────────────────────────────────────────────────────

  get models(): LiveModel[] {
    return [...this._liveModels];
  }

  /**
   * The last model successfully applied via applyModel().
   * Authoritative source for what model is active — settings may be stale.
   */
  get lastAppliedModel(): LiveModel | null {
    return this._lastAppliedModel;
  }

  get zeroXModels(): LiveModel[] {
    return this._liveModels.filter((m) => m.isFree);
  }

  get oneXModels(): LiveModel[] {
    return this._liveModels.filter((m) => !m.isFree);
  }

  /**
   * Returns 0x models that also support vision (multimodal image input).
   * The brain MUST ONLY use models from this list — never raptor-mini or other
   * 0x models that lack vision capability.
   */
  get zeroXVisionModels(): LiveModel[] {
    return this._liveModels.filter(
      (m) =>
        m.isFree &&
        ZERO_X_VISION_FAMILIES.some(
          (f) =>
            m.id.toLowerCase().includes(f) ||
            m.family.toLowerCase().includes(f),
        ),
    );
  }

  /**
   * Fetches + probes the live model list from VS Code's LM API.
   * Filters out models that require Upgrade (billing errors on probe).
   * Call on activation and after any config change.
   */
  async refresh(): Promise<LiveModel[]> {
    try {
      const raw = await vscode.lm.selectChatModels({ vendor: "copilot" });
      const zeroXIds = this._getZeroXIds();

      const probed: LiveModel[] = [];
      for (const m of raw) {
        const usable = await this._probeModel(m);
        if (usable) {
          probed.push({
            id: m.id,
            name: m.name,
            family: m.family,
            vendor: m.vendor,
            maxInputTokens: m.maxInputTokens,
            isFree: this._isZeroX(m.id, m.family, zeroXIds),
          });
        } else {
          this._log(
            "info",
            `Filtered out '${m.name}' (id=${m.id}) — requires Upgrade`,
          );
        }
      }

      this._liveModels = probed;
      this._log(
        "info",
        `Refreshed — ${probed.length} usable models (${this.zeroXModels.length} 0x, ${this.oneXModels.length} 1x)`,
      );
      this._exportToFile();
      return this._liveModels;
    } catch (err) {
      this._log("warn", `refresh() failed: ${String(err)}`);
      return [];
    }
  }

  /**
   * Switches the active Copilot Chat model.
   * Accepts id, family, or name (partial match).
   */
  async applyModel(modelId: string): Promise<boolean> {
    if (this._liveModels.length === 0) {
      await this.refresh();
    }

    const model = this._findModel(modelId);
    if (!model) {
      this._log("warn", `applyModel: '${modelId}' not in live models`);
      return false;
    }

    try {
      await vscode.commands.executeCommand(CMD_CHAT_CHANGE_MODEL, {
        vendor: model.vendor,
        id: model.id,
        family: model.family,
      });
      this._lastAppliedModel = model;
      this._log(
        "info",
        `Applied '${model.name}' (id=${model.id}, isFree=${model.isFree})`,
      );
      return true;
    } catch (err) {
      this._log("warn", `applyModel command failed: ${String(err)}`);
      return false;
    }
  }

  /**
   * Syncs the authoritative session model after a UI/native picker switch.
   * Used when the model was selected through chat UI automation instead of
   * applyModel(), but the monitor still needs correct guard/audit state.
   */
  syncAppliedModel(
    modelId: string,
    options?: { premiumSessionTrigger?: string },
  ): boolean {
    const model = this._findModel(modelId);
    if (!model) {
      this._log("warn", `syncAppliedModel: '${modelId}' not in live models`);
      return false;
    }

    this._lastAppliedModel = model;
    this._log(
      "info",
      `Synced applied model to '${model.name}' (id=${model.id}, isFree=${model.isFree})`,
    );

    if (!model.isFree && options?.premiumSessionTrigger) {
      this._premiumSessions.push({
        ts: Date.now(),
        modelId: model.id,
        trigger: options.premiumSessionTrigger,
      });
      this._log(
        "info",
        `[1x] Session started on ${model.id} — total sessions: ${this._premiumSessions.length}`,
      );
      this._onPremiumUpdate?.(this.premiumStats);
    }

    return true;
  }

  /**
   * Switches to the best available 0x (free) model.
   * Returns the model that was selected, or undefined if none available.
   */
  async applyBestZeroX(): Promise<LiveModel | undefined> {
    if (this._liveModels.length === 0) {
      await this.refresh();
    }
    const candidates = this.zeroXModels;
    if (candidates.length === 0) {
      this._log("warn", "No 0x models available");
      return undefined;
    }
    // Prefer in order: gpt-4.1 → gpt-5-mini → first available (ZERO_X_VISION_FAMILIES priority)
    const preferred =
      ZERO_X_VISION_FAMILIES.reduce<LiveModel | undefined>((found, family) => {
        if (found) {
          return found;
        }
        return candidates.find(
          (m) =>
            m.id.toLowerCase().includes(family) ||
            m.family.toLowerCase().includes(family),
        );
      }, undefined) ?? candidates[0];

    const ok = await this.applyModel(preferred.id);
    return ok ? preferred : undefined;
  }

  /**
   * Switches to the best available 0x model WITH vision capability.
   * Used exclusively by the brain (ScreenAnalyzer). Never selects raptor-mini
   * or other 0x models that lack multimodal input support.
   * Falls back through: gpt-4.1 → gpt-5-mini (ZERO_X_VISION_FAMILIES order)
   */
  async applyBestZeroXVision(): Promise<LiveModel | undefined> {
    if (this._liveModels.length === 0) {
      await this.refresh();
    }
    const candidates = this.zeroXVisionModels;
    if (candidates.length === 0) {
      this._log("warn", "No 0x vision models available — brain cannot proceed");
      return undefined;
    }
    // Use ZERO_X_VISION_FAMILIES priority order
    const preferred =
      ZERO_X_VISION_FAMILIES.reduce<LiveModel | undefined>((found, family) => {
        if (found) {
          return found;
        }
        return candidates.find(
          (m) =>
            m.id.toLowerCase().includes(family) ||
            m.family.toLowerCase().includes(family),
        );
      }, undefined) ?? candidates[0];

    const ok = await this.applyModel(preferred.id);
    return ok ? preferred : undefined;
  }

  /**
   * Switches to the best available 1x agent model.
   * Records a premium session entry when the switch succeeds.
   * Returns the model that was selected, or undefined if none available.
   */
  async applyBestOneX(): Promise<LiveModel | undefined> {
    if (this._liveModels.length === 0) {
      await this.refresh();
    }
    const candidates = this.oneXModels;
    if (candidates.length === 0) {
      this._log("warn", "No 1x models available — falling back to any model");
      return undefined;
    }
    const best = candidates[0];
    const ok = await this.applyModel(best.id);
    if (ok) {
      // Record premium session start
      this._premiumSessions.push({
        ts: Date.now(),
        modelId: best.id,
        trigger: "applyBestOneX",
      });
      this._log(
        "info",
        `[1x] Session started on ${best.id} — total sessions: ${this._premiumSessions.length}`,
      );
      this._onPremiumUpdate?.(this.premiumStats);
    }
    return ok ? best : undefined;
  }

  /**
   * Registers one VS Code command per live model:
   *   copilotMonitor.selectModel.<safe-model-id>
   *
   * Disposes the previous set before re-registering — safe to call multiple times.
   */
  registerDynamicCommands(context: vscode.ExtensionContext): void {
    this._dynamicCmds.forEach((d) => d.dispose());
    this._dynamicCmds = [];

    for (const model of this._liveModels) {
      const safeId = model.id.replace(/[^a-zA-Z0-9\-_]/g, "-");
      const cmdId = `copilotMonitor.selectModel.${safeId}`;
      const tier = model.isFree ? "0x" : "1x";

      const disposable = vscode.commands.registerCommand(cmdId, async () => {
        // AUDIT: Dynamic model commands are user-triggered — always notify + log.
        // These commands bypass the Monitor's CostGuard (they are manual overrides),
        // but they MUST NOT be silent. Show tier and record if 1x.
        this._log(
          "info",
          `[MANUAL] User invoked model switch via command palette: ${model.id} (${tier})`,
        );
        if (!model.isFree) {
          // Record premium session entry for non-free (1x) manual switches.
          this._premiumSessions.push({
            ts: Date.now(),
            modelId: model.id,
            trigger: "manual-command-palette",
          });
          this._onPremiumUpdate?.(this.premiumStats);
          vscode.window.showWarningMessage(
            `⚠️ [1x PREMIUM] Activando modelo ${model.name} — este modelo consume cuota de requests.`,
          );
        }
        const ok = await this.applyModel(model.id);
        if (ok) {
          vscode.window.showInformationMessage(
            `✅ [${tier}] Modelo activo: ${model.name}`,
          );
        } else {
          vscode.window.showWarningMessage(
            `⚠️ No se pudo activar: ${model.name}`,
          );
        }
        // Fire audit callback so Monitor can write a MANUAL_MODEL_SWITCH record.
        this._onManualModelSwitch?.(model.id, tier as "0x" | "1x", ok);
      });

      this._dynamicCmds.push(disposable);
      context.subscriptions.push(disposable);
    }

    const ids = this._liveModels
      .map(
        (m) =>
          `${m.id.replace(/[^a-zA-Z0-9\-_]/g, "-")}(${m.isFree ? "0x" : "1x"})`,
      )
      .join(", ");
    this._log(
      "info",
      `Registered ${this._dynamicCmds.length} model commands: ${ids}`,
    );
  }

  /**
   * Opens a VS Code QuickPick showing ONLY 0x (free) models.
   *
   * POLICY: The brain NEVER uses 1x models. This picker is a brain tool
   * (used by the monitor cycle for manual overrides). Showing 1x models
   * here would let the user accidentally pollute lastAppliedModel with a 1x
   * reference, causing the PREMIUM GUARD to block all subsequent sends.
   *
   * If the user needs to switch to a 1x model they should use the native
   * VS Code chat model picker (the dropdown in the chat panel).
   */
  async showModelPicker(): Promise<string | undefined> {
    if (this._liveModels.length === 0) {
      await this.refresh();
    }

    if (this._liveModels.length === 0) {
      vscode.window.showWarningMessage(
        "No se encontraron modelos usables de Copilot. ¿La extensión Copilot Chat está instalada?",
      );
      return undefined;
    }

    // POLICY: only 0x models are offered — brain must never use 1x.
    const candidates = this.zeroXModels;
    if (candidates.length === 0) {
      vscode.window.showWarningMessage(
        "No hay modelos 0x disponibles. Verifica tu plan Copilot Pro y la config copilotMonitor.zeroXModels.",
      );
      return undefined;
    }

    const items: (vscode.QuickPickItem & { modelId: string })[] =
      candidates.map((m) => ({
        label: m.name,
        description: `0x · gratis · ${m.id}`,
        detail: `$(chip) ${m.maxInputTokens.toLocaleString()} tokens · vision: ${this.zeroXVisionModels.some((v) => v.id === m.id) ? "✅" : "❌"}`,
        modelId: m.id,
      }));

    const picked = await vscode.window.showQuickPick(items, {
      title: "Copilot Monitor — Cambiar modelo 0x (brain-safe)",
      placeHolder: "Solo modelos 0x — el brain nunca usa modelos 1x",
      matchOnDescription: true,
    });

    if (!picked) {
      return undefined;
    }

    const ok = await this.applyModel(picked.modelId);
    if (ok) {
      const found = this._findModel(picked.modelId);
      vscode.window.showInformationMessage(
        `✅ [0x] Modelo activo: ${found?.name ?? picked.modelId}`,
      );
    }
    return ok ? picked.modelId : undefined;
  }

  dispose(): void {
    this._dynamicCmds.forEach((d) => d.dispose());
    this._dynamicCmds = [];
  }

  // ─── Internals ────────────────────────────────────────────────────────────

  /**
   * Probes a model with a minimal request to verify it's accessible.
   * Models requiring "Upgrade" will fail with a billing/access error.
   */
  private async _probeModel(model: vscode.LanguageModelChat): Promise<boolean> {
    try {
      const cts = new vscode.CancellationTokenSource();
      const timeout = setTimeout(() => cts.cancel(), 5_000);
      try {
        const response = await model.sendRequest(
          [vscode.LanguageModelChatMessage.User("ok")],
          {},
          cts.token,
        );
        // Consume the stream minimally to confirm success
        for await (const _chunk of response.text) {
          break; // one chunk is enough
        }
        return true;
      } finally {
        clearTimeout(timeout);
        cts.dispose();
      }
    } catch (err) {
      const msg = String(err).toLowerCase();
      const isUpgrade = UPGRADE_ERROR_PATTERNS.some((p) => msg.includes(p));
      if (isUpgrade) {
        return false; // requires upgrade — filter out
      }
      // Other errors (network, timeout) — assume usable to avoid false negatives
      this._log(
        "debug",
        `Probe error for '${model.name}' (non-upgrade): ${String(err)}`,
      );
      return true;
    }
  }

  private _getZeroXIds(): string[] {
    const cfg = vscode.workspace.getConfiguration("copilotMonitor");
    // Default matches official GitHub Pro 0x models (April 2026):
    // GPT-4o, GPT-4.1, GPT-5 mini, Raptor mini — all 0x.
    // gpt-4o-mini removed: no longer listed in GitHub docs multiplier table.
    return cfg.get<string[]>("zeroXModels", [
      "gpt-4o",
      "gpt-5-mini",
      "gpt-4.1",
      "raptor-mini",
    ]);
  }

  private _isZeroX(id: string, family: string, zeroXIds: string[]): boolean {
    const needle = (s: string) => s.toLowerCase().replace(/[._/]/g, "-");
    return zeroXIds.some((z) => {
      const zn = needle(z);
      return needle(id).includes(zn) || needle(family).includes(zn);
    });
  }

  private _findModel(modelId: string): LiveModel | undefined {
    const needle = modelId.trim().toLowerCase();
    return (
      this._liveModels.find((m) => m.id.toLowerCase() === needle) ??
      this._liveModels.find((m) => m.family.toLowerCase() === needle) ??
      this._liveModels.find(
        (m) =>
          m.id.toLowerCase().includes(needle) ||
          m.name.toLowerCase().includes(needle) ||
          needle.includes(m.family.toLowerCase()),
      )
    );
  }

  private _exportToFile(): void {
    try {
      const data = {
        exported_at: new Date().toISOString(),
        count: this._liveModels.length,
        zero_x_count: this.zeroXModels.length,
        one_x_count: this.oneXModels.length,
        models: this._liveModels,
      };
      fs.writeFileSync(LIVE_MODELS_PATH, JSON.stringify(data, null, 2));
      this._log(
        "debug",
        `Exported ${this._liveModels.length} models → ${LIVE_MODELS_PATH}`,
      );
    } catch (err) {
      this._log("warn", `Export to ${LIVE_MODELS_PATH} failed: ${String(err)}`);
    }
  }

  private _log(level: "debug" | "info" | "warn" | "error", msg: string): void {
    this._logger?.(level, `[ModelManager] ${msg}`);
  }
}
