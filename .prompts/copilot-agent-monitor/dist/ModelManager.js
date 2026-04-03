"use strict";
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
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.ModelManager = exports.ZERO_X_VISION_FAMILIES = void 0;
const vscode = __importStar(require("vscode"));
const fs = __importStar(require("fs"));
const LIVE_MODELS_PATH = "/tmp/copilot_live_models.json";
const CMD_CHAT_CHANGE_MODEL = "workbench.action.chat.changeModel";
/**
 * 0x models confirmed to support multimodal vision input (GitHub Pro, April 2026).
 * Source: https://docs.github.com/en/copilot/concepts/billing/copilot-requests#model-multipliers
 * and the "Working with visuals" section of the model comparison page.
 * Order = fallback priority for the brain (best → acceptable).
 * NOTE: raptor-mini is 0x but has NO vision — must NOT appear here.
 */
exports.ZERO_X_VISION_FAMILIES = [
    "gpt-4.1", // 0x, vision ✅ — confirmed primary (empirically verified via API)
    "gpt-5-mini", // 0x, vision ✅ — confirmed secondary (empirically verified via API)
];
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
class ModelManager {
    _liveModels = [];
    _dynamicCmds = [];
    _logger;
    // ─── Last applied model (authoritative for PREMIUM GUARD) ───────────────
    // changeModel() updates the session model in memory but does NOT write to
    // github.copilot.chat.languageModel in settings.json. This field tracks
    // what was actually applied, so _sendToChat() can check the real model.
    _lastAppliedModel = null;
    // ─── Premium (1x) request tracking ─────────────────────────────────────
    _premiumSessions = [];
    _premiumPrompts = [];
    _onPremiumUpdate;
    // ─── Manual model switch audit callback ──────────────────────────────────
    /** Fired after every dynamic-command model switch (palette/manual). */
    _onManualModelSwitch;
    constructor(logger) {
        this._logger = logger;
    }
    // ─── Premium request tracking API ────────────────────────────────────────
    /**
     * Register a callback fired whenever a new premium (1x) event is recorded.
     * Called by Monitor → feeds StatusBar display.
     */
    onPremiumUpdate(cb) {
        this._onPremiumUpdate = cb;
    }
    /**
     * Register a callback fired after every manual model switch via command palette.
     * Used by Monitor to write audit log entries for out-of-cycle model changes.
     */
    onManualModelSwitch(cb) {
        this._onManualModelSwitch = cb;
    }
    get premiumStats() {
        const log = [...this._premiumSessions, ...this._premiumPrompts].sort((a, b) => a.ts - b.ts);
        return {
            sessionCount: this._premiumSessions.length,
            promptCount: this._premiumPrompts.length,
            totalRequests: this._premiumSessions.length + this._premiumPrompts.length,
            log,
        };
    }
    /** Reset counters (e.g. start of new session or manual clear). */
    resetPremiumCount() {
        this._premiumSessions = [];
        this._premiumPrompts = [];
        this._onPremiumUpdate?.(this.premiumStats);
    }
    /**
     * Record that a prompt was sent while a 1x model was active.
     * Call from ActionExecutor whenever _sendToChat() fires on a 1x model.
     */
    recordPremiumPrompt(modelId, trigger) {
        this._premiumPrompts.push({ ts: Date.now(), modelId, trigger });
        this._log("info", `[1x] Prompt recorded on ${modelId} (trigger=${trigger}) — total prompts: ${this._premiumPrompts.length}`);
        this._onPremiumUpdate?.(this.premiumStats);
    }
    // ─── Public API ───────────────────────────────────────────────────────────
    get models() {
        return [...this._liveModels];
    }
    /**
     * The last model successfully applied via applyModel().
     * Authoritative source for what model is active — settings may be stale.
     */
    get lastAppliedModel() {
        return this._lastAppliedModel;
    }
    get zeroXModels() {
        return this._liveModels.filter((m) => m.isFree);
    }
    get oneXModels() {
        return this._liveModels.filter((m) => !m.isFree);
    }
    /**
     * Returns 0x models that also support vision (multimodal image input).
     * The brain MUST ONLY use models from this list — never raptor-mini or other
     * 0x models that lack vision capability.
     */
    get zeroXVisionModels() {
        return this._liveModels.filter((m) => m.isFree &&
            exports.ZERO_X_VISION_FAMILIES.some((f) => m.id.toLowerCase().includes(f) ||
                m.family.toLowerCase().includes(f)));
    }
    /**
     * Fetches + probes the live model list from VS Code's LM API.
     * Filters out models that require Upgrade (billing errors on probe).
     * Call on activation and after any config change.
     */
    async refresh() {
        try {
            const raw = await vscode.lm.selectChatModels({ vendor: "copilot" });
            const zeroXIds = this._getZeroXIds();
            const probed = [];
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
                }
                else {
                    this._log("info", `Filtered out '${m.name}' (id=${m.id}) — requires Upgrade`);
                }
            }
            this._liveModels = probed;
            this._log("info", `Refreshed — ${probed.length} usable models (${this.zeroXModels.length} 0x, ${this.oneXModels.length} 1x)`);
            this._exportToFile();
            return this._liveModels;
        }
        catch (err) {
            this._log("warn", `refresh() failed: ${String(err)}`);
            return [];
        }
    }
    /**
     * Switches the active Copilot Chat model.
     * Accepts id, family, or name (partial match).
     */
    async applyModel(modelId) {
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
            this._log("info", `Applied '${model.name}' (id=${model.id}, isFree=${model.isFree})`);
            return true;
        }
        catch (err) {
            this._log("warn", `applyModel command failed: ${String(err)}`);
            return false;
        }
    }
    /**
     * Syncs the authoritative session model after a UI/native picker switch.
     * Used when the model was selected through chat UI automation instead of
     * applyModel(), but the monitor still needs correct guard/audit state.
     */
    syncAppliedModel(modelId, options) {
        const model = this._findModel(modelId);
        if (!model) {
            this._log("warn", `syncAppliedModel: '${modelId}' not in live models`);
            return false;
        }
        this._lastAppliedModel = model;
        this._log("info", `Synced applied model to '${model.name}' (id=${model.id}, isFree=${model.isFree})`);
        if (!model.isFree && options?.premiumSessionTrigger) {
            this._premiumSessions.push({
                ts: Date.now(),
                modelId: model.id,
                trigger: options.premiumSessionTrigger,
            });
            this._log("info", `[1x] Session started on ${model.id} — total sessions: ${this._premiumSessions.length}`);
            this._onPremiumUpdate?.(this.premiumStats);
        }
        return true;
    }
    /**
     * Switches to the best available 0x (free) model.
     * Returns the model that was selected, or undefined if none available.
     */
    async applyBestZeroX() {
        if (this._liveModels.length === 0) {
            await this.refresh();
        }
        const candidates = this.zeroXModels;
        if (candidates.length === 0) {
            this._log("warn", "No 0x models available");
            return undefined;
        }
        // Prefer in order: gpt-4.1 → gpt-5-mini → first available (ZERO_X_VISION_FAMILIES priority)
        const preferred = exports.ZERO_X_VISION_FAMILIES.reduce((found, family) => {
            if (found) {
                return found;
            }
            return candidates.find((m) => m.id.toLowerCase().includes(family) ||
                m.family.toLowerCase().includes(family));
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
    async applyBestZeroXVision() {
        if (this._liveModels.length === 0) {
            await this.refresh();
        }
        const candidates = this.zeroXVisionModels;
        if (candidates.length === 0) {
            this._log("warn", "No 0x vision models available — brain cannot proceed");
            return undefined;
        }
        // Use ZERO_X_VISION_FAMILIES priority order
        const preferred = exports.ZERO_X_VISION_FAMILIES.reduce((found, family) => {
            if (found) {
                return found;
            }
            return candidates.find((m) => m.id.toLowerCase().includes(family) ||
                m.family.toLowerCase().includes(family));
        }, undefined) ?? candidates[0];
        const ok = await this.applyModel(preferred.id);
        return ok ? preferred : undefined;
    }
    /**
     * Switches to the best available 1x agent model.
     * Records a premium session entry when the switch succeeds.
     * Returns the model that was selected, or undefined if none available.
     */
    async applyBestOneX() {
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
            this._log("info", `[1x] Session started on ${best.id} — total sessions: ${this._premiumSessions.length}`);
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
    registerDynamicCommands(context) {
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
                this._log("info", `[MANUAL] User invoked model switch via command palette: ${model.id} (${tier})`);
                if (!model.isFree) {
                    // Record premium session entry for non-free (1x) manual switches.
                    this._premiumSessions.push({
                        ts: Date.now(),
                        modelId: model.id,
                        trigger: "manual-command-palette",
                    });
                    this._onPremiumUpdate?.(this.premiumStats);
                    vscode.window.showWarningMessage(`⚠️ [1x PREMIUM] Activando modelo ${model.name} — este modelo consume cuota de requests.`);
                }
                const ok = await this.applyModel(model.id);
                if (ok) {
                    vscode.window.showInformationMessage(`✅ [${tier}] Modelo activo: ${model.name}`);
                }
                else {
                    vscode.window.showWarningMessage(`⚠️ No se pudo activar: ${model.name}`);
                }
                // Fire audit callback so Monitor can write a MANUAL_MODEL_SWITCH record.
                this._onManualModelSwitch?.(model.id, tier, ok);
            });
            this._dynamicCmds.push(disposable);
            context.subscriptions.push(disposable);
        }
        const ids = this._liveModels
            .map((m) => `${m.id.replace(/[^a-zA-Z0-9\-_]/g, "-")}(${m.isFree ? "0x" : "1x"})`)
            .join(", ");
        this._log("info", `Registered ${this._dynamicCmds.length} model commands: ${ids}`);
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
    async showModelPicker() {
        if (this._liveModels.length === 0) {
            await this.refresh();
        }
        if (this._liveModels.length === 0) {
            vscode.window.showWarningMessage("No se encontraron modelos usables de Copilot. ¿La extensión Copilot Chat está instalada?");
            return undefined;
        }
        // POLICY: only 0x models are offered — brain must never use 1x.
        const candidates = this.zeroXModels;
        if (candidates.length === 0) {
            vscode.window.showWarningMessage("No hay modelos 0x disponibles. Verifica tu plan Copilot Pro y la config copilotMonitor.zeroXModels.");
            return undefined;
        }
        const items = candidates.map((m) => ({
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
            vscode.window.showInformationMessage(`✅ [0x] Modelo activo: ${found?.name ?? picked.modelId}`);
        }
        return ok ? picked.modelId : undefined;
    }
    dispose() {
        this._dynamicCmds.forEach((d) => d.dispose());
        this._dynamicCmds = [];
    }
    // ─── Internals ────────────────────────────────────────────────────────────
    /**
     * Probes a model with a minimal request to verify it's accessible.
     * Models requiring "Upgrade" will fail with a billing/access error.
     */
    async _probeModel(model) {
        try {
            const cts = new vscode.CancellationTokenSource();
            const timeout = setTimeout(() => cts.cancel(), 5_000);
            try {
                const response = await model.sendRequest([vscode.LanguageModelChatMessage.User("ok")], {}, cts.token);
                // Consume the stream minimally to confirm success
                for await (const _chunk of response.text) {
                    break; // one chunk is enough
                }
                return true;
            }
            finally {
                clearTimeout(timeout);
                cts.dispose();
            }
        }
        catch (err) {
            const msg = String(err).toLowerCase();
            const isUpgrade = UPGRADE_ERROR_PATTERNS.some((p) => msg.includes(p));
            if (isUpgrade) {
                return false; // requires upgrade — filter out
            }
            // Other errors (network, timeout) — assume usable to avoid false negatives
            this._log("debug", `Probe error for '${model.name}' (non-upgrade): ${String(err)}`);
            return true;
        }
    }
    _getZeroXIds() {
        const cfg = vscode.workspace.getConfiguration("copilotMonitor");
        // Default matches official GitHub Pro 0x models (April 2026):
        // GPT-4o, GPT-4.1, GPT-5 mini, Raptor mini — all 0x.
        // gpt-4o-mini removed: no longer listed in GitHub docs multiplier table.
        return cfg.get("zeroXModels", [
            "gpt-4o",
            "gpt-5-mini",
            "gpt-4.1",
            "raptor-mini",
        ]);
    }
    _isZeroX(id, family, zeroXIds) {
        const needle = (s) => s.toLowerCase().replace(/[._/]/g, "-");
        return zeroXIds.some((z) => {
            const zn = needle(z);
            return needle(id).includes(zn) || needle(family).includes(zn);
        });
    }
    _findModel(modelId) {
        const needle = modelId.trim().toLowerCase();
        return (this._liveModels.find((m) => m.id.toLowerCase() === needle) ??
            this._liveModels.find((m) => m.family.toLowerCase() === needle) ??
            this._liveModels.find((m) => m.id.toLowerCase().includes(needle) ||
                m.name.toLowerCase().includes(needle) ||
                needle.includes(m.family.toLowerCase())));
    }
    _exportToFile() {
        try {
            const data = {
                exported_at: new Date().toISOString(),
                count: this._liveModels.length,
                zero_x_count: this.zeroXModels.length,
                one_x_count: this.oneXModels.length,
                models: this._liveModels,
            };
            fs.writeFileSync(LIVE_MODELS_PATH, JSON.stringify(data, null, 2));
            this._log("debug", `Exported ${this._liveModels.length} models → ${LIVE_MODELS_PATH}`);
        }
        catch (err) {
            this._log("warn", `Export to ${LIVE_MODELS_PATH} failed: ${String(err)}`);
        }
    }
    _log(level, msg) {
        this._logger?.(level, `[ModelManager] ${msg}`);
    }
}
exports.ModelManager = ModelManager;
