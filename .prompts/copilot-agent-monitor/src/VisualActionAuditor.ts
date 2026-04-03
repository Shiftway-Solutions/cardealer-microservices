/**
 * VisualActionAuditor — Screenshot-based verification for every UI-changing action.
 *
 * Philosophy: The agent NEVER assumes a command worked blindly.
 * For every action that changes the VS Code UI, this system:
 *   1. Captures a BEFORE screenshot (saved to disk, no quota cost)
 *   2. Executes the action
 *   3. Waits for the UI to settle
 *   4. Captures an AFTER screenshot
 *   5. Verifies success via:
 *      a) Programmatic check (fast, free — settings / CDP DOM)
 *      b) Vision 0x model (only if programmatic fails — uses GPT-4o/gpt-4.1/gpt-5-mini)
 *   6. On failure: saves error log + both screenshots to /tmp/okla-audit-errors/
 *
 * This file has zero dependency on the VS Code API in its core logic so it can
 * be unit-tested with a simple vscode mock.
 */

import * as vscode from "vscode";
import { exec } from "child_process";
import * as fs from "fs";
import * as path from "path";
import * as os from "os";
import { AgentState, VisualAnalysis } from "./types";
import { ModelManager, ZERO_X_VISION_FAMILIES } from "./ModelManager";

// ─── Public types ─────────────────────────────────────────────────────────────

export interface ActionAuditResult {
  /** Human label for the action (e.g. "OPEN_NEW_CHAT", "SWITCH_MODEL → gpt-4o"). */
  actionLabel: string;
  /** Unix timestamp ms when the action started. */
  startedAt: number;
  /** Total time from before-screenshot to verification complete (ms). */
  durationMs: number;
  /** Path to before screenshot (/tmp/okla-audit-errors/... on failure, /tmp/... on success). */
  beforeScreenshot: string | null;
  /** Path to after screenshot. */
  afterScreenshot: string | null;
  /** Vision analysis of the before screenshot (only when vision was used). */
  visionBefore: VisualAnalysis | null;
  /** Vision analysis of the after screenshot (only when vision was used). */
  visionAfter: VisualAnalysis | null;
  /** Whether programmatic verification confirmed the action. */
  programmaticConfirmed: boolean;
  /** Whether vision verification confirmed the action (null = not run). */
  visionConfirmed: boolean | null;
  /** Overall confirmation: true if either programmatic or vision confirmed. */
  confirmed: boolean;
  /** Reason for failure, or empty string on success. */
  failureReason: string;
  /** Path to JSON error log if action failed. */
  errorLogPath: string | null;
}

export interface AuditedResult<T> {
  /** The raw return value from the executed action. */
  result: T;
  /** Audit metadata for this action. */
  audit: ActionAuditResult;
  /** Whether the action is confirmed OK (result.ok AND audit.confirmed). */
  ok: boolean;
}

// ─── Internal types ───────────────────────────────────────────────────────────

type ProgrammaticVerifier = () => Promise<{
  confirmed: boolean;
  detail: string;
}>;
type VscodeCancellationToken = vscode.CancellationToken;

/** Minimum ms between full vision verifications (separate from ScreenAnalyzer quota). */
const VISION_MIN_INTERVAL_MS = 30_000; // 30s

// ─── VisualActionAuditor ──────────────────────────────────────────────────────

export class VisualActionAuditor {
  private _modelManager?: ModelManager;
  private _errorDir: string;
  private _lastVisionCallMs: number = 0;

  constructor(modelManager?: ModelManager) {
    this._modelManager = modelManager;
    this._errorDir = path.join(os.tmpdir(), "okla-audit-errors");
    this._ensureErrorDir();
  }

  setModelManager(mm: ModelManager): void {
    this._modelManager = mm;
  }

  // ─── Main API ─────────────────────────────────────────────────────────────

  /**
   * Wraps a UI-changing action with before/after screenshot audit.
   *
   * @param actionLabel - Human-readable action name (e.g. "SWITCH_MODEL → gpt-4o")
   * @param actionWaitMs - ms to wait for UI to settle after executing (default 1500)
   * @param executor - The action to execute. Must return an object with ok + detail.
   * @param programmaticVerifier - Optional fast check before falling back to vision.
   * @param token - VS Code cancellation token for vision calls.
   */
  async auditedExec<T extends { ok: boolean; detail: string }>(
    actionLabel: string,
    executor: () => Promise<T>,
    options?: {
      actionWaitMs?: number;
      programmaticVerifier?: ProgrammaticVerifier;
      useVisionFallback?: boolean;
      token?: VscodeCancellationToken;
    },
  ): Promise<AuditedResult<T>> {
    const startedAt = Date.now();
    const {
      actionWaitMs = 1_500,
      programmaticVerifier,
      useVisionFallback = false,
      token,
    } = options ?? {};

    // ── Step 1: Before screenshot ───────────────────────────────────────────
    const beforePath = await this.captureToFile(
      `before_${this._slug(actionLabel)}`,
    );

    // ── Step 2: Execute action ──────────────────────────────────────────────
    let result: T;
    try {
      result = await executor();
    } catch (err) {
      const audit = this._buildAudit({
        actionLabel,
        startedAt,
        beforePath,
        afterPath: null,
        visionBefore: null,
        visionAfter: null,
        programmaticConfirmed: false,
        visionConfirmed: null,
        failureReason: `Executor threw: ${String(err)}`,
      });
      await this._saveErrorLog(audit, { executorError: String(err) });
      return {
        result: {
          ok: false,
          detail: `Executor threw: ${String(err)}`,
        } as unknown as T,
        audit,
        ok: false,
      };
    }

    // ── Step 3: Wait for UI to settle ───────────────────────────────────────
    if (actionWaitMs > 0) {
      await this._sleep(actionWaitMs);
    }

    // ── Step 4: After screenshot ────────────────────────────────────────────
    const afterPath = await this.captureToFile(
      `after_${this._slug(actionLabel)}`,
    );

    // ── Step 5a: Programmatic verification ─────────────────────────────────
    let programmaticConfirmed = result.ok; // default: trust executor result
    let programmaticDetail = result.ok ? "executor reported ok" : result.detail;

    if (programmaticVerifier) {
      try {
        const check = await programmaticVerifier();
        programmaticConfirmed = check.confirmed;
        programmaticDetail = check.detail;
      } catch (err) {
        programmaticConfirmed = false;
        programmaticDetail = `Programmatic verifier threw: ${String(err)}`;
      }
    }

    // ── Step 5b: Vision fallback (only when programmatic fails + vision enabled) ─
    let visionConfirmed: boolean | null = null;
    let visionBefore: VisualAnalysis | null = null;
    let visionAfter: VisualAnalysis | null = null;

    if (!programmaticConfirmed && useVisionFallback && afterPath) {
      const visionResult = await this._verifyWithVision(afterPath, token);
      if (visionResult) {
        visionAfter = visionResult.analysis;
        visionConfirmed = visionResult.confirmed;
      }
    }

    // ── Step 6: Build final result ──────────────────────────────────────────
    const confirmed = programmaticConfirmed || visionConfirmed === true;

    const failureReason = confirmed
      ? ""
      : `programmatic: ${programmaticDetail}${visionConfirmed === false ? ` | vision: not confirmed (state=${visionAfter?.state ?? "unknown"})` : ""}${visionConfirmed === null && !programmaticConfirmed ? " | vision: skipped" : ""}`;

    const audit = this._buildAudit({
      actionLabel,
      startedAt,
      beforePath,
      afterPath,
      visionBefore,
      visionAfter,
      programmaticConfirmed,
      visionConfirmed,
      failureReason,
    });

    // ── Save error log on failure ───────────────────────────────────────────
    if (!confirmed) {
      await this._saveErrorLog(audit, {
        executorResult: result,
        programmaticDetail,
        visionAfterState: visionAfter?.state,
      });
    }

    return { result, audit, ok: confirmed };
  }

  // ─── Screenshot capture ───────────────────────────────────────────────────

  /**
   * Capture a screenshot and save it to /tmp/ with an optional label.
   * Returns the file path, or null if capture fails.
   * This method does NOT go through the ScreenAnalyzer quota system —
   * it is for audit purposes only.
   */
  async captureToFile(label: string): Promise<string | null> {
    const ts = Date.now();
    const filename = `${label}_${ts}.png`.replace(/[^\w_.-]/g, "_");
    const tmpPath = path.join(os.tmpdir(), filename);

    return new Promise((resolve) => {
      let cmd: string;

      if (process.platform === "darwin") {
        cmd = `screencapture -x -o "${tmpPath}"`;
      } else if (process.platform === "linux") {
        cmd = `scrot "${tmpPath}" 2>/dev/null || import -window root "${tmpPath}"`;
      } else {
        cmd = [
          'powershell -NoProfile -Command "',
          "Add-Type -AssemblyName System.Windows.Forms,System.Drawing;",
          "$s=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds;",
          "$b=New-Object System.Drawing.Bitmap($s.Width,$s.Height);",
          "$g=[System.Drawing.Graphics]::FromImage($b);",
          "$g.CopyFromScreen($s.Location,[System.Drawing.Point]::Empty,$s.Size);",
          `$b.Save('${tmpPath.replace(/\\/g, "\\\\")}')`,
          '"',
        ].join("");
      }

      exec(cmd, { timeout: 10_000 }, (err: Error | null) => {
        if (err || !fs.existsSync(tmpPath)) {
          resolve(null);
          return;
        }
        resolve(tmpPath);
      });
    });
  }

  // ─── Vision post-action verification ─────────────────────────────────────

  /**
   * Analyze a screenshot with a 0x vision model to verify an action.
   * Rate-limited to avoid saturating quota between consecutive actions.
   * Returns null if model unavailable or rate limited.
   */
  private async _verifyWithVision(
    screenshotPath: string,
    token?: VscodeCancellationToken,
  ): Promise<{ analysis: VisualAnalysis; confirmed: boolean } | null> {
    // Rate limit: don't call vision more than once per 30s for action verification
    const now = Date.now();
    if (now - this._lastVisionCallMs < VISION_MIN_INTERVAL_MS) {
      return null;
    }

    try {
      const imageBuffer = fs.readFileSync(screenshotPath);
      const analysis = await this._analyzeWithVisionModel(imageBuffer, token);
      this._lastVisionCallMs = Date.now();

      // Consider confirmed if agent is in a productive state (not an error)
      const confirmed =
        !analysis.state.startsWith("ERROR_") &&
        analysis.state !== AgentState.VSCODE_HIDDEN &&
        analysis.confidence >= 0.5;

      return { analysis, confirmed };
    } catch (err) {
      console.warn(
        `[VisualActionAuditor] Vision verify failed: ${String(err)}`,
      );
      return null;
    }
  }

  // ─── Vision model call (mirrors ScreenAnalyzer — always uses 0x vision) ──

  private async _analyzeWithVisionModel(
    imageBuffer: Buffer,
    token?: VscodeCancellationToken,
  ): Promise<VisualAnalysis> {
    let resolvedModelId = "gpt-4o";
    let visionIsZeroX = true;

    if (this._modelManager) {
      const visionModels = this._modelManager.zeroXVisionModels;
      const best = ZERO_X_VISION_FAMILIES.reduce<
        import("./ModelManager").LiveModel | undefined
      >(
        (found, family) =>
          found ??
          visionModels.find(
            (m) =>
              m.id.toLowerCase().includes(family) ||
              m.family.toLowerCase().includes(family),
          ),
        undefined,
      );

      if (!best) {
        throw new Error(
          "[VisualActionAuditor] No 0x vision model available — cannot verify",
        );
      }
      resolvedModelId = best.family || best.id;
      visionIsZeroX = true;
    }

    const [model] = await vscode.lm.selectChatModels({
      vendor: "copilot",
      family: resolvedModelId,
    });

    if (!model) {
      throw new Error(
        `[VisualActionAuditor] Vision model '${resolvedModelId}' not available`,
      );
    }

    const cancelToken = token ?? new vscode.CancellationTokenSource().token;
    const base64 = imageBuffer.toString("base64");

    const userContent: Array<
      vscode.LanguageModelTextPart | vscode.LanguageModelDataPart
    > = [
      new vscode.LanguageModelTextPart(AUDIT_VISION_PROMPT),
      new vscode.LanguageModelDataPart(
        Buffer.from(base64, "base64"),
        "image/png",
      ),
    ];

    let rawText = "";
    const response = await model.sendRequest(
      [vscode.LanguageModelChatMessage.User(userContent)],
      {
        justification:
          "Copilot Agent Monitor — post-action UI audit (0x vision)",
      },
      cancelToken,
    );
    for await (const chunk of response.text) {
      rawText += chunk;
      if (cancelToken.isCancellationRequested) break;
    }

    return {
      ...parseVisionResponse(rawText),
      visionModelId: resolvedModelId,
      visionIsZeroX,
    };
  }

  // ─── Error log persistence ────────────────────────────────────────────────

  private async _saveErrorLog(
    audit: ActionAuditResult,
    extra: Record<string, unknown>,
  ): Promise<void> {
    try {
      this._ensureErrorDir();

      const ts = audit.startedAt;
      const slug = this._slug(audit.actionLabel);

      // Copy screenshots to error dir so they persist
      if (audit.beforeScreenshot && fs.existsSync(audit.beforeScreenshot)) {
        const dest = path.join(this._errorDir, `${slug}_${ts}_before.png`);
        fs.copyFileSync(audit.beforeScreenshot, dest);
        (audit as { beforeScreenshot: string }).beforeScreenshot = dest;
      }
      if (audit.afterScreenshot && fs.existsSync(audit.afterScreenshot)) {
        const dest = path.join(this._errorDir, `${slug}_${ts}_after.png`);
        fs.copyFileSync(audit.afterScreenshot, dest);
        (audit as { afterScreenshot: string }).afterScreenshot = dest;
      }

      const logPath = path.join(this._errorDir, `${slug}_${ts}_error.json`);
      const logData = {
        ...audit,
        extra,
        logWrittenAt: new Date().toISOString(),
      };
      fs.writeFileSync(logPath, JSON.stringify(logData, null, 2), "utf-8");
      (audit as { errorLogPath: string }).errorLogPath = logPath;
    } catch (err) {
      console.warn(
        `[VisualActionAuditor] Could not save error log: ${String(err)}`,
      );
    }
  }

  // ─── Error dir path (public for tests/listing) ───────────────────────────

  get errorDir(): string {
    return this._errorDir;
  }

  /**
   * Lists recent audit error files (JSON + PNG pairs).
   * Returns at most `limit` entries, newest first.
   */
  listErrors(
    limit = 20,
  ): Array<{ label: string; ts: number; logPath: string }> {
    try {
      const files = (fs.readdirSync(this._errorDir) as string[])
        .filter((f: string) => f.endsWith("_error.json"))
        .map((f: string) => {
          const full = path.join(this._errorDir, f);
          const ts = parseInt(f.split("_").slice(-2, -1)[0] ?? "0", 10) || 0;
          return { label: f, ts, logPath: full };
        })
        .sort((a: { ts: number }, b: { ts: number }) => b.ts - a.ts)
        .slice(0, limit);
      return files;
    } catch {
      return [];
    }
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  private _ensureErrorDir(): void {
    try {
      if (!fs.existsSync(this._errorDir)) {
        fs.mkdirSync(this._errorDir, { recursive: true });
      }
    } catch {
      // silent
    }
  }

  private _buildAudit(params: {
    actionLabel: string;
    startedAt: number;
    beforePath: string | null;
    afterPath: string | null;
    visionBefore: VisualAnalysis | null;
    visionAfter: VisualAnalysis | null;
    programmaticConfirmed: boolean;
    visionConfirmed: boolean | null;
    failureReason: string;
  }): ActionAuditResult {
    return {
      actionLabel: params.actionLabel,
      startedAt: params.startedAt,
      durationMs: Date.now() - params.startedAt,
      beforeScreenshot: params.beforePath,
      afterScreenshot: params.afterPath,
      visionBefore: params.visionBefore,
      visionAfter: params.visionAfter,
      programmaticConfirmed: params.programmaticConfirmed,
      visionConfirmed: params.visionConfirmed,
      confirmed:
        params.programmaticConfirmed || params.visionConfirmed === true,
      failureReason: params.failureReason,
      errorLogPath: null,
    };
  }

  private _slug(label: string): string {
    return label
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "_")
      .replace(/^_|_$/g, "")
      .slice(0, 40);
  }

  private _sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}

// ─── Vision prompt for post-action audit ─────────────────────────────────────
// Simpler than the main state analysis — just confirms VS Code is visible and
// the chat panel is in a sensible non-error state after an action.

const AUDIT_VISION_PROMPT = `
You analyze a VS Code screenshot taken immediately after a Copilot Chat action was executed.
Look ONLY at the Copilot Chat panel. Respond ONLY with a single JSON object.

States to detect (exactly one):
GENERATING   — Chat is actively generating (spinner, progress, "Running...", "Writing...", etc.)
COMPLETED    — Chat completed, model name footer visible, no spinner.
IDLE         — Chat open, no response shown, waiting for user input.
ERROR_RATE_LIMIT — "rate limit", "429", "quota exhausted" visible.
ERROR_HARD   — "500", "503", "overloaded", "Internal Server Error" visible.
ERROR_CONTEXT — "context too long", "token limit" visible.
ERROR_SWITCH_MODEL — "switch model", "model not available" visible.
VSCODE_HIDDEN — VS Code not visible or chat panel closed.

Response (strict JSON, no markdown):
{"state":"<state>","confidence":<0.0-1.0>,"detail":"<max 12 words>","errorText":<"exact error" or null>}
`.trim();

// ─── Shared vision response parser ───────────────────────────────────────────

export function parseVisionResponse(
  raw: string,
): Omit<VisualAnalysis, "visionModelId" | "visionIsZeroX"> {
  const cleaned = raw.replace(/```json|```/g, "").trim();

  let data: Record<string, unknown>;
  try {
    data = JSON.parse(cleaned);
  } catch {
    const match = raw.match(/\{[\s\S]*"state"[\s\S]*\}/);
    if (!match) {
      return {
        state: AgentState.IDLE,
        confidence: 0.1,
        detail: "Could not parse vision response",
        rawResponse: raw,
      };
    }
    data = JSON.parse(match[0]);
  }

  // const enum cannot use Object.values at runtime — list explicitly
  const VALID_STATES = [
    "GENERATING",
    "COMPLETED",
    "IDLE",
    "STALLED_SOFT",
    "STALLED_HARD",
    "ERROR_RATE_LIMIT",
    "ERROR_HARD",
    "ERROR_CONTEXT",
    "ERROR_SWITCH_MODEL",
    "VSCODE_HIDDEN",
    "RECOVERING",
    "STOPPED",
  ];
  const stateStr = String(data.state ?? "").toUpperCase();
  const state = VALID_STATES.includes(stateStr)
    ? (stateStr as AgentState)
    : AgentState.IDLE;

  return {
    state,
    confidence:
      typeof data.confidence === "number"
        ? Math.max(0, Math.min(1, data.confidence))
        : 0.5,
    detail: String(data.detail ?? "").slice(0, 120),
    errorText: data.errorText ? String(data.errorText) : undefined,
    rawResponse: raw,
  };
}
