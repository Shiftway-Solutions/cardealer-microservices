"use strict";
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
exports.VisualActionAuditor = void 0;
exports.parseVisionResponse = parseVisionResponse;
const vscode = __importStar(require("vscode"));
const child_process_1 = require("child_process");
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const os = __importStar(require("os"));
const ModelManager_1 = require("./ModelManager");
/** Minimum ms between full vision verifications (separate from ScreenAnalyzer quota). */
const VISION_MIN_INTERVAL_MS = 30_000; // 30s
// ─── VisualActionAuditor ──────────────────────────────────────────────────────
class VisualActionAuditor {
    _modelManager;
    _errorDir;
    _lastVisionCallMs = 0;
    constructor(modelManager) {
        this._modelManager = modelManager;
        this._errorDir = path.join(os.tmpdir(), "okla-audit-errors");
        this._ensureErrorDir();
    }
    setModelManager(mm) {
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
    async auditedExec(actionLabel, executor, options) {
        const startedAt = Date.now();
        const { actionWaitMs = 1_500, programmaticVerifier, useVisionFallback = false, token, } = options ?? {};
        // ── Step 1: Before screenshot ───────────────────────────────────────────
        const beforePath = await this.captureToFile(`before_${this._slug(actionLabel)}`);
        // ── Step 2: Execute action ──────────────────────────────────────────────
        let result;
        try {
            result = await executor();
        }
        catch (err) {
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
                },
                audit,
                ok: false,
            };
        }
        // ── Step 3: Wait for UI to settle ───────────────────────────────────────
        if (actionWaitMs > 0) {
            await this._sleep(actionWaitMs);
        }
        // ── Step 4: After screenshot ────────────────────────────────────────────
        const afterPath = await this.captureToFile(`after_${this._slug(actionLabel)}`);
        // ── Step 5a: Programmatic verification ─────────────────────────────────
        let programmaticConfirmed = result.ok; // default: trust executor result
        let programmaticDetail = result.ok ? "executor reported ok" : result.detail;
        if (programmaticVerifier) {
            try {
                const check = await programmaticVerifier();
                programmaticConfirmed = check.confirmed;
                programmaticDetail = check.detail;
            }
            catch (err) {
                programmaticConfirmed = false;
                programmaticDetail = `Programmatic verifier threw: ${String(err)}`;
            }
        }
        // ── Step 5b: Vision fallback (only when programmatic fails + vision enabled) ─
        let visionConfirmed = null;
        let visionBefore = null;
        let visionAfter = null;
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
    async captureToFile(label) {
        const ts = Date.now();
        const filename = `${label}_${ts}.png`.replace(/[^\w_.-]/g, "_");
        const tmpPath = path.join(os.tmpdir(), filename);
        return new Promise((resolve) => {
            let cmd;
            if (process.platform === "darwin") {
                cmd = `screencapture -x -o "${tmpPath}"`;
            }
            else if (process.platform === "linux") {
                cmd = `scrot "${tmpPath}" 2>/dev/null || import -window root "${tmpPath}"`;
            }
            else {
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
            (0, child_process_1.exec)(cmd, { timeout: 10_000 }, (err) => {
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
    async _verifyWithVision(screenshotPath, token) {
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
            const confirmed = !analysis.state.startsWith("ERROR_") &&
                analysis.state !== "VSCODE_HIDDEN" /* AgentState.VSCODE_HIDDEN */ &&
                analysis.confidence >= 0.5;
            return { analysis, confirmed };
        }
        catch (err) {
            console.warn(`[VisualActionAuditor] Vision verify failed: ${String(err)}`);
            return null;
        }
    }
    // ─── Vision model call (mirrors ScreenAnalyzer — always uses 0x vision) ──
    async _analyzeWithVisionModel(imageBuffer, token) {
        let resolvedModelId = "gpt-4o";
        let visionIsZeroX = true;
        if (this._modelManager) {
            const visionModels = this._modelManager.zeroXVisionModels;
            const best = ModelManager_1.ZERO_X_VISION_FAMILIES.reduce((found, family) => found ??
                visionModels.find((m) => m.id.toLowerCase().includes(family) ||
                    m.family.toLowerCase().includes(family)), undefined);
            if (!best) {
                throw new Error("[VisualActionAuditor] No 0x vision model available — cannot verify");
            }
            resolvedModelId = best.family || best.id;
            visionIsZeroX = true;
        }
        const [model] = await vscode.lm.selectChatModels({
            vendor: "copilot",
            family: resolvedModelId,
        });
        if (!model) {
            throw new Error(`[VisualActionAuditor] Vision model '${resolvedModelId}' not available`);
        }
        const cancelToken = token ?? new vscode.CancellationTokenSource().token;
        const base64 = imageBuffer.toString("base64");
        const userContent = [
            new vscode.LanguageModelTextPart(AUDIT_VISION_PROMPT),
            new vscode.LanguageModelDataPart(Buffer.from(base64, "base64"), "image/png"),
        ];
        let rawText = "";
        const response = await model.sendRequest([vscode.LanguageModelChatMessage.User(userContent)], {
            justification: "Copilot Agent Monitor — post-action UI audit (0x vision)",
        }, cancelToken);
        for await (const chunk of response.text) {
            rawText += chunk;
            if (cancelToken.isCancellationRequested)
                break;
        }
        return {
            ...parseVisionResponse(rawText),
            visionModelId: resolvedModelId,
            visionIsZeroX,
        };
    }
    // ─── Error log persistence ────────────────────────────────────────────────
    async _saveErrorLog(audit, extra) {
        try {
            this._ensureErrorDir();
            const ts = audit.startedAt;
            const slug = this._slug(audit.actionLabel);
            // Copy screenshots to error dir so they persist
            if (audit.beforeScreenshot && fs.existsSync(audit.beforeScreenshot)) {
                const dest = path.join(this._errorDir, `${slug}_${ts}_before.png`);
                fs.copyFileSync(audit.beforeScreenshot, dest);
                audit.beforeScreenshot = dest;
            }
            if (audit.afterScreenshot && fs.existsSync(audit.afterScreenshot)) {
                const dest = path.join(this._errorDir, `${slug}_${ts}_after.png`);
                fs.copyFileSync(audit.afterScreenshot, dest);
                audit.afterScreenshot = dest;
            }
            const logPath = path.join(this._errorDir, `${slug}_${ts}_error.json`);
            const logData = {
                ...audit,
                extra,
                logWrittenAt: new Date().toISOString(),
            };
            fs.writeFileSync(logPath, JSON.stringify(logData, null, 2), "utf-8");
            audit.errorLogPath = logPath;
        }
        catch (err) {
            console.warn(`[VisualActionAuditor] Could not save error log: ${String(err)}`);
        }
    }
    // ─── Error dir path (public for tests/listing) ───────────────────────────
    get errorDir() {
        return this._errorDir;
    }
    /**
     * Lists recent audit error files (JSON + PNG pairs).
     * Returns at most `limit` entries, newest first.
     */
    listErrors(limit = 20) {
        try {
            const files = fs.readdirSync(this._errorDir)
                .filter((f) => f.endsWith("_error.json"))
                .map((f) => {
                const full = path.join(this._errorDir, f);
                const ts = parseInt(f.split("_").slice(-2, -1)[0] ?? "0", 10) || 0;
                return { label: f, ts, logPath: full };
            })
                .sort((a, b) => b.ts - a.ts)
                .slice(0, limit);
            return files;
        }
        catch {
            return [];
        }
    }
    // ─── Helpers ──────────────────────────────────────────────────────────────
    _ensureErrorDir() {
        try {
            if (!fs.existsSync(this._errorDir)) {
                fs.mkdirSync(this._errorDir, { recursive: true });
            }
        }
        catch {
            // silent
        }
    }
    _buildAudit(params) {
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
            confirmed: params.programmaticConfirmed || params.visionConfirmed === true,
            failureReason: params.failureReason,
            errorLogPath: null,
        };
    }
    _slug(label) {
        return label
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, "_")
            .replace(/^_|_$/g, "")
            .slice(0, 40);
    }
    _sleep(ms) {
        return new Promise((resolve) => setTimeout(resolve, ms));
    }
}
exports.VisualActionAuditor = VisualActionAuditor;
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
function parseVisionResponse(raw) {
    const cleaned = raw.replace(/```json|```/g, "").trim();
    let data;
    try {
        data = JSON.parse(cleaned);
    }
    catch {
        const match = raw.match(/\{[\s\S]*"state"[\s\S]*\}/);
        if (!match) {
            return {
                state: "IDLE" /* AgentState.IDLE */,
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
        ? stateStr
        : "IDLE" /* AgentState.IDLE */;
    return {
        state,
        confidence: typeof data.confidence === "number"
            ? Math.max(0, Math.min(1, data.confidence))
            : 0.5,
        detail: String(data.detail ?? "").slice(0, 120),
        errorText: data.errorText ? String(data.errorText) : undefined,
        rawResponse: raw,
    };
}
