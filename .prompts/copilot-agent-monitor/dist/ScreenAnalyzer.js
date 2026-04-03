"use strict";
/**
 * ScreenAnalyzer — Screenshot capture + GPT-4o vision analysis.
 *
 * This is the SECONDARY signal source — used only when the log is ambiguous
 * or a stall is suspected. Uses GPT-4o (0x multiplier in Copilot Pro) via the
 * VS Code Language Model API — no external keys needed.
 *
 * Anti-abuse rules (enforced here, NOT in Monitor.ts):
 *   - Min interval between captures (config: screenshotMinIntervalSecs, default 3 min)
 *   - Hard hourly cap (config: maxScreenshotsPerHour, default 15)
 *   - NEVER called when log clearly shows GENERATING (log-first policy)
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
exports.ScreenAnalyzer = void 0;
const vscode = __importStar(require("vscode"));
const child_process_1 = require("child_process");
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const os = __importStar(require("os"));
const ModelManager_1 = require("./ModelManager");
// ─── Vision system prompt ──────────────────────────────────────────────────────
// Designed for GPT-4o. Extremely specific to minimize ambiguous responses.
const VISION_SYSTEM_PROMPT = `
You analyze screenshots of VS Code with GitHub Copilot running in Agent mode.
Look ONLY at the Copilot Chat panel on the right (or bottom) of the screen.
Respond ONLY with a single JSON object — no markdown, no explanation.

STATES to detect (pick exactly one):

GENERATING   — Any of: percentage progress bar ("45%"), spinner animation,
               "Running...", "Reading...", "Writing...", "Thinking...",
               "Searching...", "Analyzing...", "Editing...", "Calling...",
               "Compacting conversation", "Getting chat ready",
               "Preparing chat", "Loading chat", "Initializing...".
               Model is actively working or the chat session is loading.

COMPLETED    — Model response is fully shown. A model name footer is visible
               (e.g. "Claude Sonnet · High", "GPT-4o · Remote"). No spinner.
               Chat input box is active and empty.

IDLE         — Chat is open but shows no response, no spinner, no model footer.
               Waiting for user to type something.

ERROR_RATE_LIMIT — Text visible: "rate limit", "429", "Too Many Requests",
               "quota exhausted", "RateLimitError", "agent request limit".

ERROR_HARD   — Text visible: "500", "503", "502", "Internal Server Error",
               "overloaded", "capacity". Server-side transient error.

ERROR_CONTEXT — Text visible: "context too long", "This conversation is too long",
               "token limit", "context window full", "context_length_exceeded".

ERROR_SWITCH_MODEL — Text visible: "switch model", "try a different model",
               "model not available", "this model isn't available",
               "please switch", "unable to use model".

VSCODE_HIDDEN — VS Code is not visible, the chat panel is closed, or the
               screenshot shows another application in the foreground.

Response schema (strict JSON, no extra fields):
{
  "state": "<one of the states above>",
  "confidence": <0.0 to 1.0>,
  "detail": "<max 15 words describing exactly what you see>",
  "errorText": "<exact error text copied from screenshot, or null>"
}
`.trim();
class ScreenAnalyzer {
    _lastScreenshotMs = 0;
    _hourlyBucket = { hour: 0, count: 0 };
    _minIntervalMs;
    _maxPerHour;
    _modelManager;
    constructor(minIntervalMs, maxPerHour) {
        this._minIntervalMs = minIntervalMs;
        this._maxPerHour = maxPerHour;
    }
    /**
     * Inject ModelManager so the analyzer can verify the vision model is 0x.
     * Must be called before the first analyze() call for the validation to work.
     */
    setModelManager(mm) {
        this._modelManager = mm;
    }
    updateLimits(minIntervalMs, maxPerHour) {
        this._minIntervalMs = minIntervalMs;
        this._maxPerHour = maxPerHour;
    }
    /** Returns true if we are allowed to take a screenshot right now. */
    canCapture() {
        const now = Date.now();
        const hour = Math.floor(now / 3_600_000);
        // Reset hourly counter at the start of a new hour
        if (hour !== this._hourlyBucket.hour) {
            this._hourlyBucket = { hour, count: 0 };
        }
        return (now - this._lastScreenshotMs >= this._minIntervalMs &&
            this._hourlyBucket.count < this._maxPerHour);
    }
    secsUntilNextCapture() {
        const elapsed = (Date.now() - this._lastScreenshotMs) / 1000;
        return Math.max(0, this._minIntervalMs / 1000 - elapsed);
    }
    /**
     * Capture screenshot + analyze with GPT-4o (0x model via vscode.lm).
     * Throws if quota exceeded — caller must check canCapture() first.
     */
    async analyze(token) {
        if (!this.canCapture()) {
            throw new Error(`Screenshot quota: next in ${this.secsUntilNextCapture().toFixed(0)}s ` +
                `or hourly cap reached (${this._maxPerHour}/h)`);
        }
        // 1. Capture screenshot
        const imageBuffer = await this._captureScreen();
        if (!imageBuffer) {
            throw new Error("Screenshot capture failed — check OS permissions");
        }
        // 2. Update counters BEFORE the API call (even if the call fails,
        //    we already took the screenshot and shouldn't spam immediately after)
        const now = Date.now();
        const hour = Math.floor(now / 3_600_000);
        if (hour !== this._hourlyBucket.hour) {
            this._hourlyBucket = { hour, count: 0 };
        }
        this._lastScreenshotMs = now;
        this._hourlyBucket.count++;
        // 3. Analyze with GPT-4o via VS Code LM API
        return this._analyzeWithVisionModel(imageBuffer, token);
    }
    // ─── Screenshot capture ────────────────────────────────────────────────────
    async _captureScreen() {
        const tmpFile = path.join(os.tmpdir(), `cam-${Date.now()}.png`);
        return new Promise((resolve) => {
            let cmd;
            if (process.platform === "darwin") {
                // macOS: -x = no sound, -o = no shadow, full screen
                cmd = `screencapture -x -o "${tmpFile}"`;
            }
            else if (process.platform === "linux") {
                // Linux: requires scrot or imagemagick
                cmd = `scrot "${tmpFile}" 2>/dev/null || import -window root "${tmpFile}"`;
            }
            else {
                // Windows: PowerShell
                cmd = [
                    'powershell -NoProfile -Command "',
                    "Add-Type -AssemblyName System.Windows.Forms,System.Drawing;",
                    "$s=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds;",
                    "$b=New-Object System.Drawing.Bitmap($s.Width,$s.Height);",
                    "$g=[System.Drawing.Graphics]::FromImage($b);",
                    `$g.CopyFromScreen($s.Location,[System.Drawing.Point]::Empty,$s.Size);`,
                    `$b.Save('${tmpFile.replace(/\\/g, "\\\\")}')`,
                    '"',
                ].join("");
            }
            (0, child_process_1.exec)(cmd, { timeout: 10_000 }, (err) => {
                if (err || !fs.existsSync(tmpFile)) {
                    resolve(null);
                    return;
                }
                try {
                    const buf = fs.readFileSync(tmpFile);
                    fs.unlinkSync(tmpFile);
                    resolve(buf);
                }
                catch {
                    resolve(null);
                }
            });
        });
    }
    // ─── Vision model call ─────────────────────────────────────────────────────
    async _analyzeWithVisionModel(imageBuffer, token) {
        // Select the vision-capable model (GPT-4o is 0x in Copilot Pro and has vision)
        const config = vscode.workspace.getConfiguration("copilotMonitor");
        const modelId = config.get("visionModel", "gpt-4o");
        // ── 0x + vision validation ─────────────────────────────────────────────
        // The brain MUST ONLY use a model that is:
        //   (a) 0x — no quota cost
        //   (b) vision-capable — supports multimodal image input
        // Official 0x vision models (GitHub Pro, April 2026):
        //   gpt-4o → gpt-5-mini → gpt-4.1
        // raptor-mini is 0x but has NO vision — must NEVER be used here.
        let visionIsZeroX = false;
        let resolvedModelId = modelId;
        if (this._modelManager) {
            const visionModels = this._modelManager.zeroXVisionModels;
            // Check if the configured model is in the 0x+vision list
            const configuredOk = visionModels.some((m) => m.id.toLowerCase().includes(modelId.toLowerCase()) ||
                m.family.toLowerCase().includes(modelId.toLowerCase()));
            if (!configuredOk) {
                // The configured model is either not 0x or not vision-capable.
                // Find the best fallback in priority order: gpt-4o → gpt-5-mini → gpt-4.1
                const fallback = ModelManager_1.ZERO_X_VISION_FAMILIES.reduce((found, family) => {
                    if (found) {
                        return found;
                    }
                    return visionModels.find((m) => m.id.toLowerCase().includes(family) ||
                        m.family.toLowerCase().includes(family));
                }, undefined);
                if (fallback) {
                    console.warn(`[ScreenAnalyzer] ⚠️  Vision model '${modelId}' is not 0x+vision — ` +
                        `falling back to '${fallback.id}' (0x, vision). ` +
                        `Set copilotMonitor.visionModel to one of: ${ModelManager_1.ZERO_X_VISION_FAMILIES.join(", ")}`);
                    resolvedModelId = fallback.family || fallback.id;
                    visionIsZeroX = true;
                }
                else {
                    // No 0x vision model available at all — hard fail to prevent 1x spend.
                    throw new Error(`[ScreenAnalyzer] No 0x vision model available. ` +
                        `Brain cannot proceed without a free vision model. ` +
                        `Expected one of: ${ModelManager_1.ZERO_X_VISION_FAMILIES.join(", ")}. ` +
                        `Check your Copilot plan.`);
                }
            }
            else {
                visionIsZeroX = true;
            }
        }
        else {
            // ModelManager not yet wired — validate locally using ZERO_X_VISION_FAMILIES.
            // Covers extension startup race before ModelManager.refresh() completes.
            const isKnownVision = ModelManager_1.ZERO_X_VISION_FAMILIES.some((f) => modelId.toLowerCase().includes(f));
            if (!isKnownVision) {
                // Unknown model — pick first known 0x vision model as safe default
                resolvedModelId = ModelManager_1.ZERO_X_VISION_FAMILIES[0]; // gpt-4.1 (confirmed primary)
                console.warn(`[ScreenAnalyzer] ModelManager not ready — overriding '${modelId}' ` +
                    `with safe default '${resolvedModelId}' (confirmed 0x vision).`);
            }
            visionIsZeroX = true; // ZERO_X_VISION_FAMILIES are all 0x by definition
        }
        const [model] = await vscode.lm.selectChatModels({
            vendor: "copilot",
            family: resolvedModelId,
        });
        if (!model) {
            throw new Error(`Vision model '${resolvedModelId}' not available. Check your Copilot plan.`);
        }
        const base64 = imageBuffer.toString("base64");
        // Build multimodal message — VS Code LM API (1.99+)
        const userContent = [
            new vscode.LanguageModelTextPart(VISION_SYSTEM_PROMPT),
            // Image as data part — GPT-4o vision
            new vscode.LanguageModelDataPart(Buffer.from(base64, "base64"), "image/png"),
        ];
        const messages = [vscode.LanguageModelChatMessage.User(userContent)];
        let rawText = "";
        try {
            const response = await model.sendRequest(messages, { justification: "Copilot Agent Monitor — screenshot state analysis" }, token);
            for await (const chunk of response.text) {
                rawText += chunk;
                if (token.isCancellationRequested) {
                    break;
                }
            }
        }
        catch (err) {
            throw new Error(`Vision API call failed: ${err}`);
        }
        return {
            ...this._parseAnalysisResponse(rawText),
            visionModelId: resolvedModelId,
            visionIsZeroX,
        };
    }
    _parseAnalysisResponse(raw) {
        // Strip markdown fences if model added them
        const cleaned = raw.replace(/```json|```/g, "").trim();
        let data;
        try {
            data = JSON.parse(cleaned);
        }
        catch {
            // Last resort: extract JSON object from anywhere in the response
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
        const stateStr = String(data.state ?? "").toUpperCase();
        const validStates = [
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
        const state = validStates.includes(stateStr)
            ? stateStr
            : "IDLE" /* AgentState.IDLE */;
        return {
            state,
            confidence: Math.min(1, Math.max(0, Number(data.confidence ?? 0.5))),
            detail: String(data.detail ?? "").slice(0, 200),
            errorText: data.errorText
                ? String(data.errorText).slice(0, 500)
                : undefined,
            rawResponse: raw.slice(0, 500),
        };
    }
}
exports.ScreenAnalyzer = ScreenAnalyzer;
