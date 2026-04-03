"use strict";
/**
 * Monitor — Main orchestrator. Runs the observe → decide → act loop.
 *
 * Signal hierarchy (cheapest first, most expensive last):
 *
 *   1. Copilot log events   — FREE, fs.watch real-time, ~50ms latency
 *      Gives us: success, cancelled, rate_limit, hard_error, context_full
 *
 *   2. Stall timer          — FREE, just Date.now() math
 *      Gives us: stalled_soft, stalled_hard
 *
 *   3. Screenshot + GPT-4o  — COSTS one 0x API call, ~2-5s latency
 *      Used ONLY when: log silent AND stall suspected
 *      Gives us: visual confirmation of exactly what the UI shows
 *
 * This ordering means in normal operation (model coding, model completes, continue)
 * we make ZERO vision API calls. Only when something is ambiguous do we "look".
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
exports.Monitor = void 0;
const vscode = __importStar(require("vscode"));
const types_1 = require("./types");
const LogWatcher_1 = require("./LogWatcher");
const ChatDOMWatcher_1 = require("./ChatDOMWatcher");
const ScreenAnalyzer_1 = require("./ScreenAnalyzer");
const StateMachine_1 = require("./StateMachine");
const ActionExecutor_1 = require("./ActionExecutor");
const StatusBar_1 = require("./StatusBar");
const CostGuard_1 = require("./CostGuard");
const AuditLog_1 = require("./AuditLog");
const ResourceMonitor_1 = require("./ResourceMonitor");
const ChatHealthMonitor_1 = require("./ChatHealthMonitor");
class Monitor {
    _context;
    _config;
    _logWatcher;
    _chatDOMWatcher; // optional: needs CDP (--remote-debugging-port=9222)
    _resourceMonitor;
    _chatHealthMonitor;
    _screenAnalyzer;
    _stateMachine = new StateMachine_1.StateMachine();
    _executor;
    _statusBar;
    _costGuard;
    _modelManager;
    _audit;
    _output;
    // ── State ──────────────────────────────────────────────────────────────────
    _running = false;
    _loopTimer = null;
    _cycleRunning = false; // BUG FIX #2: concurrency guard
    _cancelToken = null;
    _currentState = "IDLE" /* AgentState.IDLE */;
    _lastActivityMs = Date.now();
    _lastActionMs = 0;
    _recoveryStartMs = 0;
    _softErrorRetries = 0;
    _newChatCount = 0;
    _modelRotations = 0;
    /**
     * Timestamp of the last moment GENERATING was positively confirmed — either
     * by the log (SUCCESS or ACTIVE event) or by a trusted screenshot.
     * Used by the pre-action gate to block SEND_CONTINUE / OPEN_NEW_CHAT even
     * when the screenshot quota is exhausted, preventing interruptions during
     * "Thinking...", "Preparing", "Getting chat ready", and similar silent states.
     */
    _lastConfirmedGeneratingMs = 0;
    /**
     * Last meaningful progress text received from ChatDOMWatcher.
     * Persists across spinner-only batches so the status bar keeps showing
     * e.g. "Working on foo.ts" instead of falling back to "DOM: active".
     * Cleared when isCompleted fires.
     */
    _lastDOMLabel = "";
    /**
     * Counts consecutive cycles where _preActionGate had NO usable signal
     * (screenshot quota exhausted + DOM unavailable/unknown). After
     * MAX_BLIND_ACT_STRIKES the gate allows the action as a safety valve.
     */
    _consecutiveBlindActAttempts = 0;
    /** Current resource pressure level from ResourceMonitor */
    _resourcePressure = "none";
    /** Whether ChatHealthMonitor has flagged the session as critically heavy */
    _chatIsCritical = false;
    /** Vision analysis from the last screenshot taken — stored for the audit entry. */
    _lastVisionAnalysis = null;
    _cooldowns = {
        sendContinue: 0,
        openNewChat: 0,
        stopAndNewChat: 0,
        cycleModel: 0,
        switchChatModel: 0,
        validateZeroX: 0,
        screenshot: 0,
    };
    _health = {
        validatedOk: false,
        validatedAt: 0,
    };
    _activityLog = []; // last 50 cycles
    // ──────────────────────────────────────────────────────────────────────────
    constructor(_context) {
        this._context = _context;
        this._output = vscode.window.createOutputChannel("Copilot Agent Monitor", {
            log: true,
        });
        this._context.subscriptions.push(this._output);
    }
    start() {
        if (this._running) {
            return;
        }
        this._running = true;
        this._loadConfig();
        this._initComponents();
        this._logWatcher.start();
        // ResourceMonitor — polls every 60s, fires pressure events
        this._resourceMonitor.start();
        // Start DOM watcher in background — non-blocking, gracefully disabled when CDP unavailable
        void this._startChatDOMWatcher();
        // First cycle in 5s, then every 30s (log watcher handles intra-cycle events)
        this._scheduleNextCycle(5_000);
        this._statusBar.setState("IDLE" /* AgentState.IDLE */, "Monitor started");
        this._log("info", "🟢 Copilot Agent Monitor started");
    }
    stop() {
        this._running = false;
        if (this._loopTimer) {
            clearTimeout(this._loopTimer);
            this._loopTimer = null;
        }
        if (this._cancelToken) {
            this._cancelToken.cancel();
        }
        this._logWatcher?.stop();
        this._chatDOMWatcher?.stop();
        this._resourceMonitor?.stop();
        this._chatHealthMonitor?.stop();
        this._statusBar?.setState("STOPPED" /* AgentState.STOPPED */, "Monitor stopped");
        this._log("info", "⛔ Copilot Agent Monitor stopped");
    }
    async forceAnalyze() {
        if (!this._running) {
            vscode.window.showWarningMessage("Copilot Monitor is not running. Start it first.");
            return;
        }
        this._log("info", "📸 Manual screenshot analysis requested");
        await this._runCycle(true);
    }
    getActivityLog() {
        return [...this._activityLog];
    }
    reloadConfig() {
        this._loadConfig();
        this._executor.updateConfig(this._workspaceRoot(), this._config.modelPool);
        this._screenAnalyzer.updateLimits(this._config.screenshotMinIntervalSecs * 1000, this._config.maxScreenshotsPerHour);
        this._modelManager
            ?.refresh()
            .then(() => this._modelManager?.registerDynamicCommands(this._context));
        this._log("info", "⚙️  Configuration reloaded");
    }
    setModelManager(mm) {
        this._modelManager = mm;
        if (this._executor) {
            this._executor.setModelManager(mm);
            this._executor.setHealthValidatedCallback((ok) => {
                this._health = { validatedOk: ok, validatedAt: Date.now() };
                this._log("info", `Health validation result: ${ok ? "OK ✅" : "FAILED ❌"}`);
            });
        }
        // Wire ModelManager into ScreenAnalyzer so it can validate the vision model is 0x
        if (this._screenAnalyzer) {
            this._screenAnalyzer.setModelManager(mm);
        }
        // BUG FIX #4: Dynamic model commands bypass CostGuard/Audit.
        // Register a callback so manual palette switches are recorded in the JSONL audit trail.
        mm.onManualModelSwitch((modelId, tier, switched) => {
            this._log(switched ? "info" : "warn", `[MANUAL] Model switch via command palette: ${modelId} (${tier}) → ${switched ? "OK" : "FAILED"}`);
            if (this._audit) {
                this._audit.record(this._buildAuditEntry(this._audit.nextCycle(), Date.now(), {
                    source: "manual-command-palette",
                    state: this._currentState,
                    action: "MANUAL_MODEL_SWITCH",
                    reasoning: `User manually switched to ${modelId} (${tier}) via command palette`,
                    ok: switched,
                    detail: `Manual switch to ${modelId} (${tier}) ${switched ? "succeeded" : "failed"}`,
                }));
            }
        });
    }
    // ─── Internal cycle ────────────────────────────────────────────────────────
    _scheduleNextCycle(delayMs = 30_000) {
        if (!this._running) {
            return;
        }
        // BUG FIX #1: Always cancel existing timer before scheduling a new one.
        // Without this, _onLogDelta would accumulate timers exponentially (one per
        // log poll = ~120 timers after 10 min), each firing SEND_CONTINUE concurrently.
        if (this._loopTimer) {
            clearTimeout(this._loopTimer);
            this._loopTimer = null;
        }
        this._loopTimer = setTimeout(async () => {
            this._loopTimer = null;
            await this._runCycle();
            this._scheduleNextCycle(30_000);
        }, delayMs);
    }
    async _runCycle(forceScreenshot = false) {
        // BUG FIX #2: Prevent concurrent cycle execution.
        // Without this, accumulated timers (Bug #1) or rapid log events could run
        // multiple cycles simultaneously, bypassing cooldown checks.
        if (this._cycleRunning) {
            this._log("debug", "Cycle already running — skipping overlapping invocation");
            return;
        }
        this._cycleRunning = true;
        if (this._cancelToken) {
            this._cancelToken.cancel();
        }
        this._cancelToken = new vscode.CancellationTokenSource();
        const token = this._cancelToken.token;
        const cycleStart = Date.now();
        const cycleNum = this._audit.nextCycle();
        this._lastVisionAnalysis = null; // reset per cycle
        try {
            // ─── OBSERVE ─────────────────────────────────────────────────────────
            const state = await this._observeState(forceScreenshot, token);
            this._currentState = state;
            this._statusBar.setState(state, "Analyzing...");
            // ─── DECIDE ──────────────────────────────────────────────────────────
            // Always read the last chat message via DOM before every decision — free,
            // real-time, no API cost. Previously gated at 50% stallWarnMs which meant
            // the StateMachine often decided with no content signal (UNKNOWN) and fell
            // back to blind timer-based sends. Reading every cycle ensures the content-
            // aware paths in StateMachine are always active.
            let lastMessage;
            if (this._chatDOMWatcher?.active) {
                lastMessage = await this._chatDOMWatcher.readLastMessage();
                if (lastMessage.category !== "UNKNOWN" /* LastMessageCategory.UNKNOWN */) {
                    this._log("debug", `📖 Last msg: ${lastMessage.category} (${(lastMessage.confidence * 100).toFixed(0)}%) — "${lastMessage.text.slice(0, 80)}…"`);
                }
            }
            const input = {
                state,
                mssSinceLastActivity: Date.now() - this._lastActivityMs,
                msSinceLastAction: Date.now() - this._lastActionMs,
                msSinceRecoveryStart: this._recoveryStartMs > 0 ? Date.now() - this._recoveryStartMs : 0,
                softErrorRetries: this._softErrorRetries,
                modelRotations: this._modelRotations,
                newChatCount: this._newChatCount,
                cooldowns: this._cooldowns,
                health: this._health,
                stallWarnMs: this._config.stallWarnSecs * 1000,
                stallHardMs: this._config.stallHardSecs * 1000,
                lastMessage,
                domWatcherActive: this._chatDOMWatcher?.active ?? false,
                resourcePressure: this._resourcePressure,
                chatIsCritical: this._chatIsCritical,
            };
            const { action, reasoning } = this._stateMachine.decide(input);
            this._log(action === "WAIT" /* AgentAction.WAIT */ ? "debug" : "info", `[${state}] → ${action} — ${reasoning}`);
            // ── WAIT cycles: not recorded in audit (too noisy) ───────────────────
            if (action === "WAIT" /* AgentAction.WAIT */) {
                this._statusBar.setState(state, reasoning);
                return;
            }
            // ─── PRE-ACTION VISUAL GATE (v1.3.5) ─────────────────────────────────
            // For SEND_CONTINUE and OPEN_NEW_CHAT, ALWAYS take a screenshot and
            // analyze it before executing.  Blocks actions when:
            //   • Agent is actively GENERATING (log can be silent during long tool calls)
            //   • SEND_CONTINUE on a COMPLETED chat (semantic mismatch)
            const gate = await this._preActionGate(action, token);
            if (!gate.proceed) {
                this._log("info", `🛡️ Gate BLOCKED ${action}: ${gate.reason}`);
                this._statusBar.setState(state, `🛡️ ${gate.reason.slice(0, 60)}`);
                this._audit.record(this._buildAuditEntry(cycleNum, cycleStart, {
                    source: "screenshot",
                    state,
                    action,
                    reasoning: gate.reason,
                    ok: null,
                    detail: `PRE_ACTION_GATE_BLOCKED: ${gate.reason}`,
                }));
                return;
            }
            // ─── COST GUARD — safety layer before ANY API-sending action ───────────
            // Blocks execution if too many actions fired in the sliding window.
            // This catches bugs where the state machine loops and spams requests.
            //
            // VALIDATE_ZERO_X now uses the LM API directly for the health check
            // (1 request) and then _sendToChat for the work prompt (1 request).
            // Both go through CostGuard separately, so count as 1 token each.
            const guard = this._costGuard.check();
            if (!guard.allowed) {
                this._log("warn", `[COST GUARD] ${guard.reason}`);
                this._statusBar.setState(state, "⚡ COST GUARD — action blocked");
                this._audit.record(this._buildAuditEntry(cycleNum, cycleStart, {
                    source: forceScreenshot ? "screenshot" : "log",
                    state,
                    action,
                    reasoning,
                    ok: null,
                    detail: "COST_GUARD_BLOCKED",
                    costGuard: { blocked: true, reason: guard.reason },
                }));
                if (guard.justTripped) {
                    // Fire-and-forget — NEVER block the cycle waiting for user click
                    vscode.window
                        .showErrorMessage(`🚨 Copilot Monitor: COST GUARD TRIPPED\n${guard.reason}`, "Reset Guard", "Dismiss")
                        .then((choice) => {
                        if (choice === "Reset Guard") {
                            this._costGuard.reset();
                            this._log("info", "[COST GUARD] Manually reset by user from notification");
                        }
                    });
                }
                return;
            }
            // ─── ACT ─────────────────────────────────────────────────────────────
            // BUG FIX: Check cancellation token BEFORE calling execute().
            // The token can be cancelled after the gate passes (e.g. DOM detects
            // isGenerating or USER_TYPING fires) but the action would still execute
            // because execute() does not receive the cancellation token.
            if (token.isCancellationRequested) {
                this._log("info", `⚡ Cycle cancelled before ${action} — user/model activity detected`);
                this._statusBar.setState(state, "⚡ Cancelled — activity detected");
                return;
            }
            this._statusBar.setState(state, `Executing: ${action}...`);
            const result = await this._executor.execute(action);
            // ─── CRITICAL STOP: No 0x model available ────────────────────────────
            // If the executor could not find any 0x (free) model, the monitor MUST
            // stop immediately — there is no safe model to use and continuing would
            // risk sending requests on premium (1x) models.
            if (result.detail === "NO_ZERO_X_MODEL_AVAILABLE") {
                this._log("error", "🚫 NO 0x MODEL AVAILABLE — stopping monitor to protect premium quota");
                this._audit.record(this._buildAuditEntry(cycleNum, cycleStart, {
                    source: forceScreenshot ? "screenshot" : "log",
                    state,
                    action,
                    reasoning,
                    ok: false,
                    detail: "MONITOR_STOPPED_NO_ZERO_X_MODEL",
                }));
                this.stop();
                // Fire-and-forget — monitor already stopped, don't block the cycle
                vscode.window.showErrorMessage("🔴 Copilot Monitor DETENIDO — Sin modelos 0x disponibles.\n\n" +
                    "El monitor no puede operar sin un modelo 0x con visión " +
                    "(gpt-4o, gpt-5-mini o gpt-4.1).\n\n" +
                    "Verifica tu plan de GitHub Copilot Pro y que la extensión " +
                    "Copilot Chat está activa.", { modal: true }, "Entendido");
                return;
            }
            // ─── UPDATE STATE ─────────────────────────────────────────────────────
            this._postAction(action, result.ok, state);
            const cycleResult = {
                state,
                action,
                source: forceScreenshot ? "screenshot" : "log",
                reasoning,
                executedAt: new Date(),
                actionOk: result.ok,
            };
            this._activityLog.push(cycleResult);
            if (this._activityLog.length > 50) {
                this._activityLog.shift();
            }
            // ─── AUDIT ───────────────────────────────────────────────────────────
            this._audit.record(this._buildAuditEntry(cycleNum, cycleStart, {
                source: forceScreenshot ? "screenshot" : "log",
                state,
                action,
                reasoning,
                ok: result.ok,
                detail: result.detail,
            }));
            const icon = result.ok ? "✅" : "❌";
            this._log("info", `${icon} ${action} → ${result.detail}`);
            this._statusBar.setState(state, `${icon} ${action}`);
            if (result.ok) {
                vscode.window.setStatusBarMessage(`$(copilot) Agent Monitor: ${action} executed`, 5000);
            }
        }
        catch (err) {
            if (!token.isCancellationRequested) {
                this._log("error", `Cycle error: ${err}`);
                this._audit.record(this._buildAuditEntry(cycleNum, cycleStart, {
                    source: "log",
                    state: this._currentState,
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: "Cycle threw an unexpected error",
                    ok: false,
                    detail: String(err),
                }));
            }
        }
        finally {
            this._cycleRunning = false;
        }
    }
    // ─── State observation (log-first, screenshot-second) ─────────────────────
    async _observeState(forceScreenshot, token) {
        // If we're in recovery window → stay recovering
        if (this._currentState === "RECOVERING" /* AgentState.RECOVERING */ &&
            this._recoveryStartMs > 0 &&
            Date.now() - this._recoveryStartMs < types_1.COOLDOWN_MS.recovering) {
            return "RECOVERING" /* AgentState.RECOVERING */;
        }
        // Use log-derived state as primary
        const logDerivedState = this._stateFromLog();
        // GENERATING is confirmed from log — no screenshot needed
        if (logDerivedState === "GENERATING" /* AgentState.GENERATING */) {
            this._lastConfirmedGeneratingMs = Date.now();
            return "GENERATING" /* AgentState.GENERATING */;
        }
        // v1.3.11: If DOM confirmed generating within the extended log window (90s),
        // trust DOM even when the log is silent. Without this, cycles that run while
        // log is quiet (e.g. pure DOM activity like "Thinking...") would downgrade
        // the state to IDLE and overwrite the "DOM: active" status bar text.
        const msSinceConfirmedGenerating = Date.now() - this._lastConfirmedGeneratingMs;
        if (this._lastConfirmedGeneratingMs > 0 &&
            msSinceConfirmedGenerating < 90_000) {
            return "GENERATING" /* AgentState.GENERATING */;
        }
        // Decide if we should take a screenshot
        const msSinceActivity = Date.now() - this._lastActivityMs;
        const stallPending = msSinceActivity >= this._config.stallWarnSecs * 1000 * 0.8;
        const shouldScreenshot = forceScreenshot || (stallPending && this._screenAnalyzer.canCapture());
        // BUG FIX: Screenshot interval blocked a re-capture, but the last confirmed
        // state was GENERATING. Trusting the screenshot is safer than trusting the
        // stall timer — extend the stall window and keep GENERATING to avoid
        // switching models or opening new chats mid-generation.
        if (stallPending &&
            !forceScreenshot &&
            !this._screenAnalyzer.canCapture() &&
            this._currentState === "GENERATING" /* AgentState.GENERATING */) {
            this._lastActivityMs = Date.now(); // extend stall timer by one cycle
            this._lastConfirmedGeneratingMs = Date.now(); // still generating
            this._log("debug", "Stall suspected but cannot re-screenshot yet — last confirmed state was GENERATING → extending stall timer to avoid interrupting model");
            return "GENERATING" /* AgentState.GENERATING */;
        }
        if (shouldScreenshot) {
            this._statusBar.setState(this._currentState, "📸 Analyzing screen...");
            const visionStart = Date.now();
            try {
                const analysis = await this._screenAnalyzer.analyze(token);
                const visionDuration = Date.now() - visionStart;
                const trusted = analysis.confidence >= 0.7;
                this._log("info", `📸 Screenshot: ${analysis.state} (${(analysis.confidence * 100).toFixed(0)}%) [${analysis.visionModelId} 0x=${analysis.visionIsZeroX}] — ${analysis.detail}`);
                // Store for audit entry
                this._lastVisionAnalysis = {
                    ...analysis,
                    durationMs: visionDuration,
                    trusted,
                };
                // Trust screenshot if confidence ≥ 0.7, else fall back to log.
                // When GENERATING: also reset stall timer so the next cycle doesn't
                // immediately re-fire on a still-generating model.
                if (trusted) {
                    if (analysis.state === "GENERATING" /* AgentState.GENERATING */) {
                        this._lastActivityMs = Date.now();
                        this._lastConfirmedGeneratingMs = Date.now();
                    }
                    return analysis.state;
                }
                this._log("debug", "Low-confidence screenshot — falling back to log state");
            }
            catch (err) {
                this._log("warn", `Screenshot failed: ${err}`);
            }
        }
        return logDerivedState;
    }
    // ─── Derive state from latest log event ───────────────────────────────────
    _latestLogEvent = "idle" /* LogEvent.IDLE */;
    _lastLogActivityMs = 0;
    _stateFromLog() {
        const msSinceLogActivity = Date.now() - this._lastLogActivityMs;
        // Extended generating window (90s, up from 30s) for models that are silent
        // mid-request: extended thinking (o1, o3, Claude 3.7+), "Preparing" /
        // "Getting chat ready" init states, and pauses between agent tool calls.
        //
        // SUCCESS  → a tool-call ccreq just completed; more turns likely follow.
        // ACTIVE   → non-terminal log lines arrived (model is working, not done).
        // Both confidently indicate the model is generating and must not be interrupted.
        if (msSinceLogActivity < 90_000 &&
            (this._latestLogEvent === "success" /* LogEvent.SUCCESS */ ||
                this._latestLogEvent === "active" /* LogEvent.ACTIVE */)) {
            return "GENERATING" /* AgentState.GENERATING */;
        }
        switch (this._latestLogEvent) {
            case "rate_limited" /* LogEvent.RATE_LIMITED */:
                return "ERROR_RATE_LIMIT" /* AgentState.ERROR_RATE_LIMIT */;
            case "hard_error" /* LogEvent.HARD_ERROR */:
                return "ERROR_HARD" /* AgentState.ERROR_HARD */;
            case "request_error" /* LogEvent.REQUEST_ERROR */: // BUG FIX C: treat ccreq.*error as hard error
                return "ERROR_HARD" /* AgentState.ERROR_HARD */;
            case "context_full" /* LogEvent.CONTEXT_FULL */:
                return "ERROR_CONTEXT" /* AgentState.ERROR_CONTEXT */;
            case "loop_stopped" /* LogEvent.LOOP_STOPPED */:
                return "COMPLETED" /* AgentState.COMPLETED */;
            case "cancelled" /* LogEvent.CANCELLED */:
                return "IDLE" /* AgentState.IDLE */;
        }
        // No definitive log signal → determine from inactivity timer
        const msSinceActivity = Date.now() - this._lastActivityMs;
        if (msSinceActivity >= this._config.stallHardSecs * 1000) {
            return "STALLED_HARD" /* AgentState.STALLED_HARD */;
        }
        if (msSinceActivity >= this._config.stallWarnSecs * 1000) {
            return "STALLED_SOFT" /* AgentState.STALLED_SOFT */;
        }
        return "IDLE" /* AgentState.IDLE */;
    }
    // ─── Handle log events from LogWatcher ────────────────────────────────────
    _onLogDelta(delta) {
        this._latestLogEvent = delta.event;
        this._lastLogActivityMs = Date.now();
        if (delta.event === "success" /* LogEvent.SUCCESS */) {
            this._lastActivityMs = Date.now();
            this._lastConfirmedGeneratingMs = Date.now(); // tool-call completed; more turns likely
            this._softErrorRetries = 0;
            this._log("debug", `✓ Log: success (${delta.newLinesCount} new lines)`);
            // Trigger a cycle soon — model may have just completed its turn
            this._scheduleNextCycle(3_000);
        }
        else if (delta.event === "loop_stopped" /* LogEvent.LOOP_STOPPED */) {
            // BUG FIX #3: Reset activity timer when the agent loop stops cleanly.
            this._lastActivityMs = Date.now();
            this._softErrorRetries = 0;
            this._log("info", "⏹️  Agent loop completed — task finished, resetting activity timer");
            this._scheduleNextCycle(5_000);
        }
        else if (delta.event === "active" /* LogEvent.ACTIVE */) {
            // Non-terminal log activity: model is processing — Thinking, Preparing,
            // Getting chat ready, Compacting conversation, inter-tool-call pauses, etc.
            // Update both the stall timer AND the confirmed-generating timestamp.
            // No cycle trigger needed — the 90s window in _stateFromLog keeps us GENERATING.
            this._lastActivityMs = Date.now();
            this._lastConfirmedGeneratingMs = Date.now();
            this._log("debug", `⚡ Log: active (${delta.newLinesCount} lines — non-terminal, model processing)`);
        }
        else if (delta.event !== "idle" /* LogEvent.IDLE */) {
            this._log("info", `⚠️  Log event: ${delta.event}`);
            // Trigger cycle soon for error handling
            this._scheduleNextCycle(2_000);
        }
        // Note: LogEvent.IDLE is only emitted by initialization / manual reset
        // (LogWatcher now emits ACTIVE for any real log content). No action needed.
    }
    // ─── ChatDOMWatcher lifecycle ──────────────────────────────────────────────
    /** Starts the DOM watcher. Non-blocking; succeeds silently when CDP available. */
    async _startChatDOMWatcher() {
        const port = vscode.workspace
            .getConfiguration("copilotMonitor")
            .get("cdpPort", 9222);
        this._chatDOMWatcher = new ChatDOMWatcher_1.ChatDOMWatcher(port, (delta) => this._onDOMDelta(delta), 50);
        const ok = await this._chatDOMWatcher.start();
        if (ok) {
            this._log("info", "🔭 ChatDOMWatcher started — real-time DOM activity detection active");
            // Wire readLastMessage so ActionExecutor can read DOM responses
            // for yes/no recovery checks without going through OCR.
            this._executor.setReadLastMessageCallback(() => 
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            this._chatDOMWatcher.readLastMessage());
            // ChatHealthMonitor — shares same CDP port, polls every 30s
            this._chatHealthMonitor = new ChatHealthMonitor_1.ChatHealthMonitor(port, (health) => this._onChatHealth(health), 30_000);
            const healthOk = await this._chatHealthMonitor.start();
            this._log(healthOk ? "info" : "debug", healthOk
                ? "💬 ChatHealthMonitor started — detecting heavy sessions (>50 msgs)"
                : "💬 ChatHealthMonitor: CDP start failed — chat health monitoring disabled");
        }
        else {
            this._log("debug", "ChatDOMWatcher: CDP not available (VS Code needs --remote-debugging-port=9222) — DOM signal disabled");
            this._chatDOMWatcher = undefined;
        }
    }
    // ─── Handle resource pressure events from ResourceMonitor ─────────────────
    _onResourcePressure(event) {
        this._resourcePressure = event.pressure;
        this._log("warn", `💾 Resource pressure: ${event.pressure} — ${event.message}`);
        // For critical levels, upgrade state and schedule an immediate cycle
        if (event.pressure === "ram_critical" || event.pressure === "disk_critical") {
            this._currentState = "RESOURCE_PRESSURE" /* AgentState.RESOURCE_PRESSURE */;
            this._statusBar.setState("RESOURCE_PRESSURE" /* AgentState.RESOURCE_PRESSURE */, `💾 ${event.message.slice(0, 60)}`);
            this._scheduleNextCycle(1_000);
        }
        else {
            // Warn levels: update status bar text but don't interrupt the agent
            const snap = event.snapshot;
            const detail = ResourceMonitor_1.ResourceMonitor.format(snap);
            this._log("info", `💾 Resources: ${detail}`);
        }
    }
    // ─── Handle chat health events from ChatHealthMonitor ─────────────────────
    _onChatHealth(health) {
        this._chatIsCritical = health.isCritical;
        if (health.hasSpinnerStuck) {
            this._log("warn", `💬 Spinner atascado detectado (${Math.round((Date.now() - health.capturedAt) / 1000)}s) — el agente puede estar congelado`);
        }
        if (health.isCritical) {
            this._log("warn", `💬 Chat CRÍTICO: ${health.messageCount} mensajes, ~${health.estimatedTokens} tokens, ${health.domSizeKb}KB DOM — reset preventivo necesario`);
            // Only mark CHAT_HEAVY if NOT generating — never interrupt the model
            if (this._currentState !== "GENERATING" /* AgentState.GENERATING */) {
                this._currentState = "CHAT_HEAVY" /* AgentState.CHAT_HEAVY */;
                this._statusBar.setState("CHAT_HEAVY" /* AgentState.CHAT_HEAVY */, `💬 Chat pesado (${health.messageCount} msgs) — reset preventivo`);
                this._scheduleNextCycle(2_000);
            }
        }
        else if (health.isGettingHeavy) {
            this._log("info", `💬 Chat acercándose al límite: ${health.messageCount} mensajes — considerar nueva sesión pronto`);
        }
    }
    // ─── Handle DOM delta events from ChatDOMWatcher ───────────────────────────
    _onDOMDelta(delta) {
        if (delta.isGenerating) {
            // DOM confirms the model is actively working.
            // v1.3.11: Full state reset on any DOM activity so stale error counters,
            // recovery windows, and log signals cannot override what the DOM is seeing.
            this._lastActivityMs = Date.now();
            this._lastConfirmedGeneratingMs = Date.now();
            this._currentState = "GENERATING" /* AgentState.GENERATING */; // ← v1.3.11: keeps cycle aware
            this._latestLogEvent = "active" /* LogEvent.ACTIVE */; // ← v1.3.11: resets log state
            this._softErrorRetries = 0; // ← v1.3.11: clears error counter
            this._recoveryStartMs = 0; // ← v1.3.11: cancels stale recovery
            // ⚡ HIGH PRIORITY: if a cycle is currently in-flight (gate, action executor
            // typing into the chat), cancel it immediately. DOM activity = the model is
            // working right now — any SEND_CONTINUE or OPEN_NEW_CHAT would be disruptive.
            if (this._cycleRunning) {
                this._log("warn", "⚡ DOM activity while action in-flight — cancelling to avoid interruption");
                this._cancelToken?.cancel();
                this._executor.cancelCurrentAction();
                // Note: _cycleRunning is NOT reset here — the _runCycle finally{} block
                // owns that flag and will clear it when the cancelled execution unwinds.
            }
            // Update status bar — use the freshest text, or fall back to the cached
            // label from a previous batch (BUG FIX D: spinner-only batches have no
            // progressTexts but the last known label is still valid).
            const freshLabel = delta.progressTexts[0];
            if (freshLabel) {
                this._lastDOMLabel = freshLabel; // update cache
            }
            const label = this._lastDOMLabel;
            this._statusBar.setState("GENERATING" /* AgentState.GENERATING */, label ? `DOM: ${label.slice(0, 60)}` : "DOM: active");
            this._log("debug", `🔭 DOM(${delta.events.length} events)${label ? ` — ${label}` : ""}`);
        }
        else if (delta.isCompleted) {
            // Stop button disappeared → chat is definitively done.
            // BUG FIX 2: Invalidate the 90-second log window so _stateFromLog()
            // stops returning GENERATING immediately, instead of waiting up to 90s
            // for the SUCCESS window to expire.
            this._lastConfirmedGeneratingMs = 0;
            this._latestLogEvent = "idle" /* LogEvent.IDLE */;
            this._lastDOMLabel = ""; // clear label cache when generation ends
            // Reset stall timer from 0 — task completion IS an activity event.
            // The monitor will wait stallWarnSecs of SILENCE after this point.
            this._lastActivityMs = Date.now();
            this._log("debug", "🔭 DOM: stop button gone → cleared 90s window, triggering cycle");
            this._scheduleNextCycle(2_000);
        }
        else if (delta.isUserTyping) {
            // User is actively typing in the chat input → reset inactivity timer.
            // This prevents the monitor from treating a user-active session as a stall.
            this._lastActivityMs = Date.now();
            // BUG FIX: Cancel any in-flight stall action — user is actively managing
            // the session. Previously USER_TYPING only reset _lastActivityMs but did
            // NOT stop a cycle that was already past the gate and about to fire
            // SEND_CONTINUE or OPEN_NEW_CHAT. This caused the monitor to send a
            // command while the user was actively typing.
            if (this._cycleRunning) {
                this._log("warn", "⌨️ User typing while cycle in-flight — cancelling stall action");
                this._cancelToken?.cancel();
                this._executor.cancelCurrentAction();
            }
            // Clear stall state immediately so the next cycle doesn't re-trigger.
            // _stateFromLog() will recompute on the next cycle, but _currentState is
            // checked by the pre-action gate and statusBar, so clearing it now is
            // safer than waiting for the next 30s cycle.
            if (this._currentState === "STALLED_SOFT" /* AgentState.STALLED_SOFT */ ||
                this._currentState === "STALLED_HARD" /* AgentState.STALLED_HARD */) {
                this._currentState = "IDLE" /* AgentState.IDLE */;
                this._statusBar.setState("IDLE" /* AgentState.IDLE */, "⌨️ User typing — stall cleared");
            }
            this._log("debug", "⌨️ User typing detected — inactivity timer reset");
        }
        // ── Cancel recovery on ANY DOM activity ──────────────────────────────────
        // If the model is generating or user is typing while we're in RECOVERING
        // state, the situation resolved itself — exit recovery immediately.
        if (delta.hasActivity &&
            this._currentState === "RECOVERING" /* AgentState.RECOVERING */ &&
            this._recoveryStartMs > 0) {
            this._recoveryStartMs = 0;
            this._currentState = "IDLE" /* AgentState.IDLE */;
            this._log("info", "🔄 DOM activity during recovery — recovery cancelled, returning to IDLE");
        }
    }
    // ─── Pre-action visual gate (v1.3.5) ──────────────────────────────────────
    //
    // Before executing SEND_CONTINUE or OPEN_NEW_CHAT the agent MUST take a
    // screenshot and analyze it.  Two blocker conditions exist:
    //
    //   1. Screen shows GENERATING → agent is actively working.  Never interrupt.
    //      Also resets the stall timer so the next cycle doesn't re-fire.
    //
    //   2. Action is SEND_CONTINUE but screen shows COMPLETED → semantic mismatch.
    //      The task finished cleanly; "continuar" in the same chat is wrong.
    //      Updated state so the next cycle picks OPEN_NEW_CHAT instead.
    //
    // If the screenshot quota is exhausted, the gate ALLOWS the action with a
    // warning (never block permanently due to rate-limiting).
    // If the screenshot itself throws, the gate ALLOWS the action (fail-open).
    //
    // Gated actions:  SEND_CONTINUE, OPEN_NEW_CHAT
    // Ungated:        STOP_AND_NEW_CHAT, VALIDATE_ZERO_X, CYCLE_MODEL,
    //                 SWITCH_CHAT_MODEL, FOCUS_VSCODE
    //                 (all triggered by confirmed errors from the log)
    async _preActionGate(action, token) {
        const gatedActions = [
            "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
            "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
        ];
        if (!gatedActions.includes(action)) {
            return {
                proceed: true,
                reason: "action type does not require visual gate",
            };
        }
        // ─── Step 0: DOM read (FREE — no quota, real-time, always first) ─────────
        // Evaluate the chat content before touching any screenshot quota. This lets
        // the gate block blindly-timed sends when the agent is clearly still working
        // or when the task is already complete (semantic mismatch guard).
        if (this._chatDOMWatcher?.active) {
            const domReading = await this._chatDOMWatcher.readLastMessage();
            if (domReading.confidence >= 0.6) {
                if (domReading.category === "STILL_WORKING" /* LastMessageCategory.STILL_WORKING */) {
                    // Agent is mid-task — reset stall timer, do not interrupt.
                    this._lastActivityMs = Date.now();
                    this._lastConfirmedGeneratingMs = Date.now();
                    this._consecutiveBlindActAttempts = 0;
                    return {
                        proceed: false,
                        reason: `DOM: STILL_WORKING ("${domReading.text.slice(0, 60)}") — agent is active, ${action} blocked`,
                    };
                }
                if (action === "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */ &&
                    domReading.category === "TASK_COMPLETE" /* LastMessageCategory.TASK_COMPLETE */) {
                    // Task is done — SEND_CONTINUE is semantically wrong.
                    // Correct the state so the next cycle opens a fresh chat instead.
                    this._currentState = "COMPLETED" /* AgentState.COMPLETED */;
                    this._latestLogEvent = "loop_stopped" /* LogEvent.LOOP_STOPPED */;
                    this._consecutiveBlindActAttempts = 0;
                    return {
                        proceed: false,
                        reason: `DOM: TASK_COMPLETE — SEND_CONTINUE is semantically wrong; next cycle uses OPEN_NEW_CHAT`,
                    };
                }
                // DOM gave a real, confident signal — reset no-signal counter.
                this._consecutiveBlindActAttempts = 0;
                this._log("debug", `🛡️ Gate DOM: ${domReading.category} (${(domReading.confidence * 100).toFixed(0)}%) — proceeding to screenshot check`);
            }
        }
        // ─── Step 1: Screenshot quota check ──────────────────────────────────────
        // If screenshot is unavailable, use the last confirmed GENERATING timestamp
        // and a consecutive-no-signal counter to decide whether to allow or block.
        if (!this._screenAnalyzer.canCapture()) {
            const timeSinceGenerating = Date.now() - this._lastConfirmedGeneratingMs;
            const safeWindowMs = this._config.screenshotMinIntervalSecs * 1000 * 1.5;
            if (this._lastConfirmedGeneratingMs > 0 &&
                timeSinceGenerating < safeWindowMs) {
                // GENERATING was confirmed recently — hold the action and extend the stall
                // timer so the monitor keeps waiting without re-triggering immediately.
                this._lastActivityMs = Date.now();
                this._log("warn", `🛡️ Gate: GENERATING confirmed ${Math.floor(timeSinceGenerating / 1000)}s ago — ` +
                    `blocking ${action} during safety hold (quota exhausted, ${Math.floor(safeWindowMs / 1000)}s window)`);
                return {
                    proceed: false,
                    reason: `GENERATING confirmed ${Math.floor(timeSinceGenerating / 1000)}s ago — safety hold (screenshot quota exhausted)`,
                };
            }
            // No screenshot AND no useful DOM signal — block up to MAX_BLIND_ACT_STRIKES
            // times before allowing as a safety valve. Prevents blind sends when the
            // monitor has zero knowledge of the current chat state.
            const MAX_BLIND_ACT_STRIKES = 3;
            this._consecutiveBlindActAttempts++;
            if (this._consecutiveBlindActAttempts <= MAX_BLIND_ACT_STRIKES) {
                this._log("warn", `🛡️ Gate: no signal (screenshot quota exhausted + DOM unavailable/unknown) — ` +
                    `blocking ${action} (strike ${this._consecutiveBlindActAttempts}/${MAX_BLIND_ACT_STRIKES})`);
                return {
                    proceed: false,
                    reason: `no signal available — waiting for screenshot quota reset (strike ${this._consecutiveBlindActAttempts}/${MAX_BLIND_ACT_STRIKES})`,
                };
            }
            // Safety valve: after MAX_BLIND_ACT_STRIKES we allow once, then reset the
            // counter so the pattern repeats rather than acting every cycle.
            this._consecutiveBlindActAttempts = 0;
            this._log("warn", `🛡️ Gate: ${MAX_BLIND_ACT_STRIKES} consecutive no-signal blocks exhausted — ` +
                `allowing ${action} as safety valve`);
            return {
                proceed: true,
                reason: `no signal (${MAX_BLIND_ACT_STRIKES} consecutive blocks) — proceeding as safety valve`,
            };
        }
        // ─── Step 2: Take screenshot ──────────────────────────────────────────────
        this._log("info", `🛡️ Pre-action gate: capturing screen before ${action}...`);
        this._statusBar.setState(this._currentState, `🛡️ Gate: verifying before ${action}...`);
        try {
            const gateStart = Date.now();
            const analysis = await this._screenAnalyzer.analyze(token);
            const gateDurationMs = Date.now() - gateStart;
            this._log("info", `🛡️ Gate [${action}]: screen=${analysis.state} conf=${(analysis.confidence * 100).toFixed(0)}% — ${analysis.detail}`);
            // Screenshot succeeded — reset no-signal counter.
            this._consecutiveBlindActAttempts = 0;
            // Store for audit entry
            this._lastVisionAnalysis = {
                ...analysis,
                durationMs: gateDurationMs,
                trusted: analysis.confidence >= 0.7,
            };
            // Low-confidence: allow but warn (we can't reliably block on a guess)
            if (analysis.confidence < 0.5) {
                return {
                    proceed: true,
                    reason: `low-confidence gate (${analysis.state} @ ${(analysis.confidence * 100).toFixed(0)}%) — proceeding`,
                };
            }
            // BLOCK: Agent is actively generating — never interrupt
            if (analysis.state === "GENERATING" /* AgentState.GENERATING */) {
                // Reset stall timer so the next cycle doesn't immediately re-trigger
                this._currentState = "GENERATING" /* AgentState.GENERATING */;
                this._lastActivityMs = Date.now();
                return {
                    proceed: false,
                    reason: `agent is GENERATING (${analysis.detail}) — ${action} blocked`,
                };
            }
            // BLOCK: Semantic mismatch — task done but action is SEND_CONTINUE
            // Correct state so the next cycle chooses OPEN_NEW_CHAT instead
            if (action === "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */ &&
                analysis.state === "COMPLETED" /* AgentState.COMPLETED */) {
                this._currentState = "COMPLETED" /* AgentState.COMPLETED */;
                this._latestLogEvent = "loop_stopped" /* LogEvent.LOOP_STOPPED */;
                return {
                    proceed: false,
                    reason: "screen shows COMPLETED — SEND_CONTINUE is semantically wrong; next cycle will use OPEN_NEW_CHAT",
                };
            }
            return { proceed: true, reason: `gate passed: screen=${analysis.state}` };
        }
        catch (err) {
            this._log("warn", `🛡️ Gate screenshot failed: ${err} — allowing ${action} without visual confirmation`);
            return {
                proceed: true,
                reason: `gate screenshot error (${String(err)}) — proceeding`,
            };
        }
    }
    // ─── Post-action state updates ────────────────────────────────────────────
    _postAction(action, ok, prevState) {
        const now = Date.now();
        // Always stamp the cooldown — even on failure (prevents spam retries)
        switch (action) {
            case "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */:
                this._cooldowns.sendContinue = now;
                if (ok) {
                    this._softErrorRetries++;
                    this._lastActivityMs = now; // BUG FIX B: reset stall timer after nudge
                    this._lastActionMs = now;
                    this._recoveryStartMs = now;
                    this._currentState = "RECOVERING" /* AgentState.RECOVERING */;
                    this._latestLogEvent = "idle" /* LogEvent.IDLE */; // BUG FIX A: clear stale error signal
                }
                break;
            case "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */:
                this._cooldowns.openNewChat = now;
                if (ok) {
                    this._newChatCount++;
                    this._lastActivityMs = now; // opening new chat = activity → reset stall timer
                    this._lastActionMs = now;
                    this._recoveryStartMs = now;
                    this._softErrorRetries = 0;
                    this._currentState = "RECOVERING" /* AgentState.RECOVERING */;
                    this._latestLogEvent = "idle" /* LogEvent.IDLE */; // BUG FIX A: clear stale error signal
                }
                break;
            case "STOP_AND_NEW_CHAT" /* AgentAction.STOP_AND_NEW_CHAT */:
                this._cooldowns.stopAndNewChat = now;
                this._cooldowns.openNewChat = now;
                if (ok) {
                    this._newChatCount++;
                    this._lastActivityMs = now; // opening new chat = activity → reset stall timer
                    this._lastActionMs = now;
                    this._recoveryStartMs = now;
                    this._softErrorRetries = 0;
                    this._currentState = "RECOVERING" /* AgentState.RECOVERING */;
                    this._latestLogEvent = "idle" /* LogEvent.IDLE */; // BUG FIX A: clear stale error signal
                }
                break;
            case "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */:
                this._cooldowns.cycleModel = now;
                this._cooldowns.openNewChat = now;
                if (ok) {
                    this._modelRotations++;
                    this._lastActivityMs = now;
                    this._softErrorRetries = 0;
                    this._currentState = "RECOVERING" /* AgentState.RECOVERING */;
                    this._recoveryStartMs = now;
                    this._latestLogEvent = "idle" /* LogEvent.IDLE */; // BUG FIX A: clear stale error signal
                }
                break;
            case "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */:
                this._cooldowns.switchChatModel = now;
                if (ok) {
                    this._modelRotations++;
                    this._lastActivityMs = now; // BUG FIX B: reset stall timer
                    this._softErrorRetries = 0;
                    this._currentState = "RECOVERING" /* AgentState.RECOVERING */;
                    this._recoveryStartMs = now;
                    this._latestLogEvent = "idle" /* LogEvent.IDLE */; // BUG FIX A: clear stale error signal
                }
                break;
            case "VALIDATE_ZERO_X" /* AgentAction.VALIDATE_ZERO_X */:
                this._cooldowns.validateZeroX = now;
                if (ok) {
                    // Health validated (0x model confirmed) + new chat opened → agent loop prompt sent
                    this._modelRotations++;
                    this._softErrorRetries = 0;
                    this._lastActivityMs = now;
                    this._lastActionMs = now;
                    this._recoveryStartMs = now;
                    this._currentState = "RECOVERING" /* AgentState.RECOVERING */;
                    this._latestLogEvent = "idle" /* LogEvent.IDLE */;
                }
                break;
        }
    }
    // ─── Init helpers ─────────────────────────────────────────────────────────
    _loadConfig() {
        const cfg = vscode.workspace.getConfiguration("copilotMonitor");
        this._config = {
            stallWarnSecs: cfg.get("stallWarnSecs", 300),
            stallHardSecs: cfg.get("stallHardSecs", 480),
            screenshotMinIntervalSecs: cfg.get("screenshotMinIntervalSecs", 180),
            maxScreenshotsPerHour: cfg.get("maxScreenshotsPerHour", 15),
            loopPromptFile: cfg.get("loopPromptFile", ".prompts/AGENT_LOOP_PROMPT.md"),
            modelPool: cfg.get("modelPool", ["gpt-4o", "gpt-4.1", "gpt-5-mini"]),
            visionModel: cfg.get("visionModel", "gpt-4o"),
        };
    }
    /** Manually reset the cost guard circuit breaker (e.g. from command palette). */
    resetCostGuard() {
        this._costGuard?.reset();
        const stats = this._costGuard?.stats;
        this._log("info", `[COST GUARD] Manually reset. Session totals: ${stats?.allowed ?? 0} allowed, ${stats?.blocked ?? 0} blocked`);
        vscode.window.showInformationMessage("Copilot Monitor: Cost Guard reset ✅ Monitor is active again.");
    }
    _initComponents() {
        this._logWatcher = new LogWatcher_1.LogWatcher((delta) => this._onLogDelta(delta), 5_000);
        this._screenAnalyzer = new ScreenAnalyzer_1.ScreenAnalyzer(this._config.screenshotMinIntervalSecs * 1000, this._config.maxScreenshotsPerHour);
        this._executor = new ActionExecutor_1.ActionExecutor(this._workspaceRoot(), this._config.modelPool, this._context, (level, message) => this._log(level, message));
        if (this._modelManager) {
            this._executor.setModelManager(this._modelManager);
            this._screenAnalyzer.setModelManager(this._modelManager);
            this._executor.setHealthValidatedCallback((ok) => {
                this._health = { validatedOk: ok, validatedAt: Date.now() };
                this._log("info", `Health validation result: ${ok ? "OK ✅" : "FAILED ❌"}`);
            });
        }
        // ResourceMonitor — 60s poll, reacts to RAM + disk pressure
        this._resourceMonitor = new ResourceMonitor_1.ResourceMonitor((event) => this._onResourcePressure(event), 60_000);
        const cfg = vscode.workspace.getConfiguration("copilotMonitor");
        this._costGuard = new CostGuard_1.CostGuard(cfg.get("costGuard.maxActionsPerWindow", 8), cfg.get("costGuard.windowMinutes", 5) * 60_000, cfg.get("costGuard.autoResetMinutes", 10) * 60_000);
        // StatusBar duplication fix: only create once. If start() is called
        // multiple times (e.g. user clicks "Start" again), re-use the existing
        // instance instead of creating a new StatusBarItem each time.
        if (!this._statusBar) {
            this._statusBar = new StatusBar_1.StatusBar();
            this._context.subscriptions.push(this._statusBar);
        }
        this._audit = new AuditLog_1.AuditLog(this._workspaceRoot());
        this._log("info", `📋 Audit log: ${this._audit.filePath}`);
    }
    _workspaceRoot() {
        return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? process.cwd();
    }
    /** Returns the absolute path of the audit JSONL file. */
    getAuditPath() {
        return this._audit?.filePath ?? "";
    }
    /** Returns the most recent audit entries (most recent first). */
    getRecentAuditEntries(count = 50) {
        return this._audit?.recentEntries(count) ?? [];
    }
    _buildAuditEntry(cycle, startMs, data) {
        const vision = this._lastVisionAnalysis;
        const configuredModel = vscode.workspace
            .getConfiguration("github.copilot.chat")
            .get("languageModel", "");
        const entry = {
            ts: new Date().toISOString(),
            v: 1,
            cycle,
            durationMs: Date.now() - startMs,
            source: data.source,
            state: String(data.state),
            stallMs: Date.now() - this._lastActivityMs,
            action: data.action,
            reasoning: data.reasoning,
            ok: data.ok,
            detail: data.detail,
            configuredModel,
            softErrorRetries: this._softErrorRetries,
            modelRotations: this._modelRotations,
            newChatCount: this._newChatCount,
        };
        if (vision) {
            entry.vision = {
                modelId: vision.visionModelId,
                isZeroX: vision.visionIsZeroX,
                state: String(vision.state),
                confidence: vision.confidence,
                detail: vision.detail,
                trusted: vision.trusted,
                durationMs: vision.durationMs,
            };
        }
        if (data.costGuard) {
            entry.costGuard = data.costGuard;
        }
        return entry;
    }
    _log(level, msg) {
        const prefix = "[Copilot Monitor]";
        switch (level) {
            case "debug":
                console.debug(`${prefix} ${msg}`);
                this._output.debug(msg);
                break;
            case "info":
                console.info(`${prefix} ${msg}`);
                this._output.info(msg);
                break;
            case "warn":
                console.warn(`${prefix} ${msg}`);
                this._output.warn(msg);
                break;
            case "error":
                console.error(`${prefix} ${msg}`);
                this._output.error(msg);
                break;
        }
    }
}
exports.Monitor = Monitor;
