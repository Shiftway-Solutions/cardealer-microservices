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

import * as vscode from "vscode";
import {
  AgentState,
  AgentAction,
  LogEvent,
  DOMDelta,
  Cooldowns,
  CycleResult,
  MonitorConfig,
  HealthValidation,
  COOLDOWN_MS,
  VisualAnalysis,
  LastMessageCategory,
  LastMessageReading,
} from "./types";
import { LogWatcher, LogDelta } from "./LogWatcher";
import { ChatDOMWatcher } from "./ChatDOMWatcher";
import { ScreenAnalyzer } from "./ScreenAnalyzer";
import { StateMachine, DecisionInput } from "./StateMachine";
import { ActionExecutor } from "./ActionExecutor";
import { StatusBar } from "./StatusBar";
import { CostGuard } from "./CostGuard";
import { ModelManager } from "./ModelManager";
import { AuditLog, AuditEntry } from "./AuditLog";

export class Monitor {
  private _config!: MonitorConfig;
  private _logWatcher!: LogWatcher;
  private _chatDOMWatcher?: ChatDOMWatcher; // optional: needs CDP (--remote-debugging-port=9222)
  private _screenAnalyzer!: ScreenAnalyzer;
  private _stateMachine: StateMachine = new StateMachine();
  private _executor!: ActionExecutor;
  private _statusBar!: StatusBar;
  private _costGuard!: CostGuard;
  private _modelManager?: ModelManager;
  private _audit!: AuditLog;
  private readonly _output: vscode.LogOutputChannel;

  // ── State ──────────────────────────────────────────────────────────────────
  private _running: boolean = false;
  private _loopTimer: NodeJS.Timeout | null = null;
  private _cycleRunning: boolean = false; // BUG FIX #2: concurrency guard
  private _cancelToken: vscode.CancellationTokenSource | null = null;

  private _currentState: AgentState = AgentState.IDLE;
  private _lastActivityMs: number = Date.now();
  private _lastActionMs: number = 0;
  private _recoveryStartMs: number = 0;
  private _softErrorRetries: number = 0;
  private _newChatCount: number = 0;
  private _modelRotations: number = 0;

  /**
   * Timestamp of the last moment GENERATING was positively confirmed — either
   * by the log (SUCCESS or ACTIVE event) or by a trusted screenshot.
   * Used by the pre-action gate to block SEND_CONTINUE / OPEN_NEW_CHAT even
   * when the screenshot quota is exhausted, preventing interruptions during
   * "Thinking...", "Preparing", "Getting chat ready", and similar silent states.
   */
  private _lastConfirmedGeneratingMs: number = 0;

  /**
   * Last meaningful progress text received from ChatDOMWatcher.
   * Persists across spinner-only batches so the status bar keeps showing
   * e.g. "Working on foo.ts" instead of falling back to "DOM: active".
   * Cleared when isCompleted fires.
   */
  private _lastDOMLabel: string = "";

  /** Vision analysis from the last screenshot taken — stored for the audit entry. */
  private _lastVisionAnalysis:
    | (VisualAnalysis & { durationMs: number; trusted: boolean })
    | null = null;

  private _cooldowns: Cooldowns = {
    sendContinue: 0,
    openNewChat: 0,
    stopAndNewChat: 0,
    cycleModel: 0,
    switchChatModel: 0,
    validateZeroX: 0,
    screenshot: 0,
  };

  private _health: HealthValidation = {
    validatedOk: false,
    validatedAt: 0,
  };

  private _activityLog: CycleResult[] = []; // last 50 cycles

  // ──────────────────────────────────────────────────────────────────────────

  constructor(private readonly _context: vscode.ExtensionContext) {
    this._output = vscode.window.createOutputChannel("Copilot Agent Monitor", {
      log: true,
    });
    this._context.subscriptions.push(this._output);
  }

  start(): void {
    if (this._running) {
      return;
    }
    this._running = true;

    this._loadConfig();
    this._initComponents();
    this._logWatcher.start();

    // Start DOM watcher in background — non-blocking, gracefully disabled when CDP unavailable
    void this._startChatDOMWatcher();

    // First cycle in 5s, then every 30s (log watcher handles intra-cycle events)
    this._scheduleNextCycle(5_000);
    this._statusBar.setState(AgentState.IDLE, "Monitor started");
    this._log("info", "🟢 Copilot Agent Monitor started");
  }

  stop(): void {
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
    this._statusBar?.setState(AgentState.STOPPED, "Monitor stopped");
    this._log("info", "⛔ Copilot Agent Monitor stopped");
  }

  async forceAnalyze(): Promise<void> {
    if (!this._running) {
      vscode.window.showWarningMessage(
        "Copilot Monitor is not running. Start it first.",
      );
      return;
    }
    this._log("info", "📸 Manual screenshot analysis requested");
    await this._runCycle(true);
  }

  getActivityLog(): CycleResult[] {
    return [...this._activityLog];
  }

  reloadConfig(): void {
    this._loadConfig();
    this._executor.updateConfig(this._workspaceRoot(), this._config.modelPool);
    this._screenAnalyzer.updateLimits(
      this._config.screenshotMinIntervalSecs * 1000,
      this._config.maxScreenshotsPerHour,
    );
    this._modelManager
      ?.refresh()
      .then(() => this._modelManager?.registerDynamicCommands(this._context));
    this._log("info", "⚙️  Configuration reloaded");
  }

  setModelManager(mm: ModelManager): void {
    this._modelManager = mm;
    if (this._executor) {
      this._executor.setModelManager(mm);
      this._executor.setHealthValidatedCallback((ok) => {
        this._health = { validatedOk: ok, validatedAt: Date.now() };
        this._log(
          "info",
          `Health validation result: ${ok ? "OK ✅" : "FAILED ❌"}`,
        );
      });
    }
    // Wire ModelManager into ScreenAnalyzer so it can validate the vision model is 0x
    if (this._screenAnalyzer) {
      this._screenAnalyzer.setModelManager(mm);
    }
    // BUG FIX #4: Dynamic model commands bypass CostGuard/Audit.
    // Register a callback so manual palette switches are recorded in the JSONL audit trail.
    mm.onManualModelSwitch((modelId, tier, switched) => {
      this._log(
        switched ? "info" : "warn",
        `[MANUAL] Model switch via command palette: ${modelId} (${tier}) → ${switched ? "OK" : "FAILED"}`,
      );
      if (this._audit) {
        this._audit.record(
          this._buildAuditEntry(this._audit.nextCycle(), Date.now(), {
            source: "manual-command-palette",
            state: this._currentState,
            action: "MANUAL_MODEL_SWITCH",
            reasoning: `User manually switched to ${modelId} (${tier}) via command palette`,
            ok: switched,
            detail: `Manual switch to ${modelId} (${tier}) ${switched ? "succeeded" : "failed"}`,
          }),
        );
      }
    });
  }

  // ─── Internal cycle ────────────────────────────────────────────────────────

  private _scheduleNextCycle(delayMs = 30_000): void {
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

  private async _runCycle(forceScreenshot = false): Promise<void> {
    // BUG FIX #2: Prevent concurrent cycle execution.
    // Without this, accumulated timers (Bug #1) or rapid log events could run
    // multiple cycles simultaneously, bypassing cooldown checks.
    if (this._cycleRunning) {
      this._log(
        "debug",
        "Cycle already running — skipping overlapping invocation",
      );
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
      // Read last chat message when stall may be approaching (cheap DOM read, no API).
      // We read at 50% of stallWarnMs so we have the info ready before the timer fires.
      let lastMessage: LastMessageReading | undefined;
      const msSinceActivity = Date.now() - this._lastActivityMs;
      const stallApproaching =
        msSinceActivity >= this._config.stallWarnSecs * 1000 * 0.5;
      if (stallApproaching && this._chatDOMWatcher?.active) {
        lastMessage = await this._chatDOMWatcher.readLastMessage();
        if (lastMessage.category !== LastMessageCategory.UNKNOWN) {
          this._log(
            "debug",
            `📖 Last msg: ${lastMessage.category} (${(lastMessage.confidence * 100).toFixed(0)}%) — "${lastMessage.text.slice(0, 80)}…"`,
          );
        }
      }

      const input: DecisionInput = {
        state,
        mssSinceLastActivity: Date.now() - this._lastActivityMs,
        msSinceLastAction: Date.now() - this._lastActionMs,
        msSinceRecoveryStart:
          this._recoveryStartMs > 0 ? Date.now() - this._recoveryStartMs : 0,
        softErrorRetries: this._softErrorRetries,
        modelRotations: this._modelRotations,
        newChatCount: this._newChatCount,
        cooldowns: this._cooldowns,
        health: this._health,
        stallWarnMs: this._config.stallWarnSecs * 1000,
        stallHardMs: this._config.stallHardSecs * 1000,
        lastMessage,
      };

      const { action, reasoning } = this._stateMachine.decide(input);

      this._log(
        action === AgentAction.WAIT ? "debug" : "info",
        `[${state}] → ${action} — ${reasoning}`,
      );

      // ── WAIT cycles: not recorded in audit (too noisy) ───────────────────
      if (action === AgentAction.WAIT) {
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
        this._audit.record(
          this._buildAuditEntry(cycleNum, cycleStart, {
            source: "screenshot",
            state,
            action,
            reasoning: gate.reason,
            ok: null,
            detail: `PRE_ACTION_GATE_BLOCKED: ${gate.reason}`,
          }),
        );
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

        this._audit.record(
          this._buildAuditEntry(cycleNum, cycleStart, {
            source: forceScreenshot ? "screenshot" : "log",
            state,
            action,
            reasoning,
            ok: null,
            detail: "COST_GUARD_BLOCKED",
            costGuard: { blocked: true, reason: guard.reason },
          }),
        );

        if (guard.justTripped) {
          // Fire-and-forget — NEVER block the cycle waiting for user click
          vscode.window
            .showErrorMessage(
              `🚨 Copilot Monitor: COST GUARD TRIPPED\n${guard.reason}`,
              "Reset Guard",
              "Dismiss",
            )
            .then((choice) => {
              if (choice === "Reset Guard") {
                this._costGuard.reset();
                this._log(
                  "info",
                  "[COST GUARD] Manually reset by user from notification",
                );
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
        this._log(
          "info",
          `⚡ Cycle cancelled before ${action} — user/model activity detected`,
        );
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
        this._log(
          "error",
          "🚫 NO 0x MODEL AVAILABLE — stopping monitor to protect premium quota",
        );
        this._audit.record(
          this._buildAuditEntry(cycleNum, cycleStart, {
            source: forceScreenshot ? "screenshot" : "log",
            state,
            action,
            reasoning,
            ok: false,
            detail: "MONITOR_STOPPED_NO_ZERO_X_MODEL",
          }),
        );
        this.stop();
        // Fire-and-forget — monitor already stopped, don't block the cycle
        vscode.window.showErrorMessage(
          "🔴 Copilot Monitor DETENIDO — Sin modelos 0x disponibles.\n\n" +
            "El monitor no puede operar sin un modelo 0x con visión " +
            "(gpt-4o, gpt-5-mini o gpt-4.1).\n\n" +
            "Verifica tu plan de GitHub Copilot Pro y que la extensión " +
            "Copilot Chat está activa.",
          { modal: true },
          "Entendido",
        );
        return;
      }

      // ─── UPDATE STATE ─────────────────────────────────────────────────────
      this._postAction(action, result.ok, state);

      const cycleResult: CycleResult = {
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
      this._audit.record(
        this._buildAuditEntry(cycleNum, cycleStart, {
          source: forceScreenshot ? "screenshot" : "log",
          state,
          action,
          reasoning,
          ok: result.ok,
          detail: result.detail,
        }),
      );

      const icon = result.ok ? "✅" : "❌";
      this._log("info", `${icon} ${action} → ${result.detail}`);
      this._statusBar.setState(state, `${icon} ${action}`);

      if (result.ok) {
        vscode.window.setStatusBarMessage(
          `$(copilot) Agent Monitor: ${action} executed`,
          5000,
        );
      }
    } catch (err) {
      if (!token.isCancellationRequested) {
        this._log("error", `Cycle error: ${err}`);
        this._audit.record(
          this._buildAuditEntry(cycleNum, cycleStart, {
            source: "log",
            state: this._currentState,
            action: AgentAction.WAIT,
            reasoning: "Cycle threw an unexpected error",
            ok: false,
            detail: String(err),
          }),
        );
      }
    } finally {
      this._cycleRunning = false;
    }
  }

  // ─── State observation (log-first, screenshot-second) ─────────────────────

  private async _observeState(
    forceScreenshot: boolean,
    token: vscode.CancellationToken,
  ): Promise<AgentState> {
    // If we're in recovery window → stay recovering
    if (
      this._currentState === AgentState.RECOVERING &&
      this._recoveryStartMs > 0 &&
      Date.now() - this._recoveryStartMs < COOLDOWN_MS.recovering
    ) {
      return AgentState.RECOVERING;
    }

    // Use log-derived state as primary
    const logDerivedState = this._stateFromLog();

    // GENERATING is confirmed from log — no screenshot needed
    if (logDerivedState === AgentState.GENERATING) {
      this._lastConfirmedGeneratingMs = Date.now();
      return AgentState.GENERATING;
    }

    // v1.3.11: If DOM confirmed generating within the extended log window (90s),
    // trust DOM even when the log is silent. Without this, cycles that run while
    // log is quiet (e.g. pure DOM activity like "Thinking...") would downgrade
    // the state to IDLE and overwrite the "DOM: active" status bar text.
    const msSinceConfirmedGenerating =
      Date.now() - this._lastConfirmedGeneratingMs;
    if (
      this._lastConfirmedGeneratingMs > 0 &&
      msSinceConfirmedGenerating < 90_000
    ) {
      return AgentState.GENERATING;
    }

    // Decide if we should take a screenshot
    const msSinceActivity = Date.now() - this._lastActivityMs;
    const stallPending =
      msSinceActivity >= this._config.stallWarnSecs * 1000 * 0.8;
    const shouldScreenshot =
      forceScreenshot || (stallPending && this._screenAnalyzer.canCapture());

    // BUG FIX: Screenshot interval blocked a re-capture, but the last confirmed
    // state was GENERATING. Trusting the screenshot is safer than trusting the
    // stall timer — extend the stall window and keep GENERATING to avoid
    // switching models or opening new chats mid-generation.
    if (
      stallPending &&
      !forceScreenshot &&
      !this._screenAnalyzer.canCapture() &&
      this._currentState === AgentState.GENERATING
    ) {
      this._lastActivityMs = Date.now(); // extend stall timer by one cycle
      this._lastConfirmedGeneratingMs = Date.now(); // still generating
      this._log(
        "debug",
        "Stall suspected but cannot re-screenshot yet — last confirmed state was GENERATING → extending stall timer to avoid interrupting model",
      );
      return AgentState.GENERATING;
    }

    if (shouldScreenshot) {
      this._statusBar.setState(this._currentState, "📸 Analyzing screen...");
      const visionStart = Date.now();
      try {
        const analysis = await this._screenAnalyzer.analyze(token);
        const visionDuration = Date.now() - visionStart;
        const trusted = analysis.confidence >= 0.7;
        this._log(
          "info",
          `📸 Screenshot: ${analysis.state} (${(analysis.confidence * 100).toFixed(0)}%) [${analysis.visionModelId} 0x=${analysis.visionIsZeroX}] — ${analysis.detail}`,
        );

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
          if (analysis.state === AgentState.GENERATING) {
            this._lastActivityMs = Date.now();
            this._lastConfirmedGeneratingMs = Date.now();
          }
          return analysis.state;
        }
        this._log(
          "debug",
          "Low-confidence screenshot — falling back to log state",
        );
      } catch (err) {
        this._log("warn", `Screenshot failed: ${err}`);
      }
    }

    return logDerivedState;
  }

  // ─── Derive state from latest log event ───────────────────────────────────

  private _latestLogEvent: LogEvent = LogEvent.IDLE;
  private _lastLogActivityMs: number = 0;

  private _stateFromLog(): AgentState {
    const msSinceLogActivity = Date.now() - this._lastLogActivityMs;

    // Extended generating window (90s, up from 30s) for models that are silent
    // mid-request: extended thinking (o1, o3, Claude 3.7+), "Preparing" /
    // "Getting chat ready" init states, and pauses between agent tool calls.
    //
    // SUCCESS  → a tool-call ccreq just completed; more turns likely follow.
    // ACTIVE   → non-terminal log lines arrived (model is working, not done).
    // Both confidently indicate the model is generating and must not be interrupted.
    if (
      msSinceLogActivity < 90_000 &&
      (this._latestLogEvent === LogEvent.SUCCESS ||
        this._latestLogEvent === LogEvent.ACTIVE)
    ) {
      return AgentState.GENERATING;
    }

    switch (this._latestLogEvent) {
      case LogEvent.RATE_LIMITED:
        return AgentState.ERROR_RATE_LIMIT;
      case LogEvent.HARD_ERROR:
        return AgentState.ERROR_HARD;
      case LogEvent.REQUEST_ERROR: // BUG FIX C: treat ccreq.*error as hard error
        return AgentState.ERROR_HARD;
      case LogEvent.CONTEXT_FULL:
        return AgentState.ERROR_CONTEXT;
      case LogEvent.LOOP_STOPPED:
        return AgentState.COMPLETED;
      case LogEvent.CANCELLED:
        return AgentState.IDLE;
    }

    // No definitive log signal → determine from inactivity timer
    const msSinceActivity = Date.now() - this._lastActivityMs;
    if (msSinceActivity >= this._config.stallHardSecs * 1000) {
      return AgentState.STALLED_HARD;
    }
    if (msSinceActivity >= this._config.stallWarnSecs * 1000) {
      return AgentState.STALLED_SOFT;
    }

    return AgentState.IDLE;
  }

  // ─── Handle log events from LogWatcher ────────────────────────────────────

  private _onLogDelta(delta: LogDelta): void {
    this._latestLogEvent = delta.event;
    this._lastLogActivityMs = Date.now();

    if (delta.event === LogEvent.SUCCESS) {
      this._lastActivityMs = Date.now();
      this._lastConfirmedGeneratingMs = Date.now(); // tool-call completed; more turns likely
      this._softErrorRetries = 0;
      this._log("debug", `✓ Log: success (${delta.newLinesCount} new lines)`);
      // Trigger a cycle soon — model may have just completed its turn
      this._scheduleNextCycle(3_000);
    } else if (delta.event === LogEvent.LOOP_STOPPED) {
      // BUG FIX #3: Reset activity timer when the agent loop stops cleanly.
      this._lastActivityMs = Date.now();
      this._softErrorRetries = 0;
      this._log(
        "info",
        "⏹️  Agent loop completed — task finished, resetting activity timer",
      );
      this._scheduleNextCycle(5_000);
    } else if (delta.event === LogEvent.ACTIVE) {
      // Non-terminal log activity: model is processing — Thinking, Preparing,
      // Getting chat ready, Compacting conversation, inter-tool-call pauses, etc.
      // Update both the stall timer AND the confirmed-generating timestamp.
      // No cycle trigger needed — the 90s window in _stateFromLog keeps us GENERATING.
      this._lastActivityMs = Date.now();
      this._lastConfirmedGeneratingMs = Date.now();
      this._log(
        "debug",
        `⚡ Log: active (${delta.newLinesCount} lines — non-terminal, model processing)`,
      );
    } else if (delta.event !== LogEvent.IDLE) {
      this._log("info", `⚠️  Log event: ${delta.event}`);
      // Trigger cycle soon for error handling
      this._scheduleNextCycle(2_000);
    }
    // Note: LogEvent.IDLE is only emitted by initialization / manual reset
    // (LogWatcher now emits ACTIVE for any real log content). No action needed.
  }

  // ─── ChatDOMWatcher lifecycle ──────────────────────────────────────────────

  /** Starts the DOM watcher. Non-blocking; succeeds silently when CDP available. */
  private async _startChatDOMWatcher(): Promise<void> {
    const port = vscode.workspace
      .getConfiguration("copilotMonitor")
      .get<number>("cdpPort", 9222);

    this._chatDOMWatcher = new ChatDOMWatcher(
      port,
      (delta) => this._onDOMDelta(delta),
      400, // poll every 400ms
    );

    const ok = await this._chatDOMWatcher.start();
    if (ok) {
      this._log(
        "info",
        "🔭 ChatDOMWatcher started — real-time DOM activity detection active",
      );
      // Wire readLastMessage so ActionExecutor can read DOM responses
      // for yes/no recovery checks without going through OCR.
      this._executor.setReadLastMessageCallback(() =>
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        this._chatDOMWatcher!.readLastMessage(),
      );
    } else {
      this._log(
        "debug",
        "ChatDOMWatcher: CDP not available (VS Code needs --remote-debugging-port=9222) — DOM signal disabled",
      );
      this._chatDOMWatcher = undefined;
    }
  }

  // ─── Handle DOM delta events from ChatDOMWatcher ───────────────────────────

  private _onDOMDelta(delta: DOMDelta): void {
    if (delta.isGenerating) {
      // DOM confirms the model is actively working.
      // v1.3.11: Full state reset on any DOM activity so stale error counters,
      // recovery windows, and log signals cannot override what the DOM is seeing.
      this._lastActivityMs = Date.now();
      this._lastConfirmedGeneratingMs = Date.now();
      this._currentState = AgentState.GENERATING; // ← v1.3.11: keeps cycle aware
      this._latestLogEvent = LogEvent.ACTIVE; // ← v1.3.11: resets log state
      this._softErrorRetries = 0; // ← v1.3.11: clears error counter
      this._recoveryStartMs = 0; // ← v1.3.11: cancels stale recovery

      // ⚡ HIGH PRIORITY: if a cycle is currently in-flight (gate, action executor
      // typing into the chat), cancel it immediately. DOM activity = the model is
      // working right now — any SEND_CONTINUE or OPEN_NEW_CHAT would be disruptive.
      if (this._cycleRunning) {
        this._log(
          "warn",
          "⚡ DOM activity while action in-flight — cancelling to avoid interruption",
        );
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
      this._statusBar.setState(
        AgentState.GENERATING,
        label ? `DOM: ${label.slice(0, 60)}` : "DOM: active",
      );

      this._log(
        "debug",
        `🔭 DOM(${delta.events.length} events)${label ? ` — ${label}` : ""}`,
      );
    } else if (delta.isCompleted) {
      // Stop button disappeared → chat is definitively done.
      // BUG FIX 2: Invalidate the 90-second log window so _stateFromLog()
      // stops returning GENERATING immediately, instead of waiting up to 90s
      // for the SUCCESS window to expire.
      this._lastConfirmedGeneratingMs = 0;
      this._latestLogEvent = LogEvent.IDLE;
      this._lastDOMLabel = ""; // clear label cache when generation ends
      // Reset stall timer from 0 — task completion IS an activity event.
      // The monitor will wait stallWarnSecs of SILENCE after this point.
      this._lastActivityMs = Date.now();
      this._log(
        "debug",
        "🔭 DOM: stop button gone → cleared 90s window, triggering cycle",
      );
      this._scheduleNextCycle(2_000);
    } else if (delta.isUserTyping) {
      // User is actively typing in the chat input → reset inactivity timer.
      // This prevents the monitor from treating a user-active session as a stall.
      this._lastActivityMs = Date.now();

      // BUG FIX: Cancel any in-flight stall action — user is actively managing
      // the session. Previously USER_TYPING only reset _lastActivityMs but did
      // NOT stop a cycle that was already past the gate and about to fire
      // SEND_CONTINUE or OPEN_NEW_CHAT. This caused the monitor to send a
      // command while the user was actively typing.
      if (this._cycleRunning) {
        this._log(
          "warn",
          "⌨️ User typing while cycle in-flight — cancelling stall action",
        );
        this._cancelToken?.cancel();
        this._executor.cancelCurrentAction();
      }

      // Clear stall state immediately so the next cycle doesn't re-trigger.
      // _stateFromLog() will recompute on the next cycle, but _currentState is
      // checked by the pre-action gate and statusBar, so clearing it now is
      // safer than waiting for the next 30s cycle.
      if (
        this._currentState === AgentState.STALLED_SOFT ||
        this._currentState === AgentState.STALLED_HARD
      ) {
        this._currentState = AgentState.IDLE;
        this._statusBar.setState(
          AgentState.IDLE,
          "⌨️ User typing — stall cleared",
        );
      }

      this._log("debug", "⌨️ User typing detected — inactivity timer reset");
    }

    // ── Cancel recovery on ANY DOM activity ──────────────────────────────────
    // If the model is generating or user is typing while we're in RECOVERING
    // state, the situation resolved itself — exit recovery immediately.
    if (
      delta.hasActivity &&
      this._currentState === AgentState.RECOVERING &&
      this._recoveryStartMs > 0
    ) {
      this._recoveryStartMs = 0;
      this._currentState = AgentState.IDLE;
      this._log(
        "info",
        "🔄 DOM activity during recovery — recovery cancelled, returning to IDLE",
      );
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

  private async _preActionGate(
    action: AgentAction,
    token: vscode.CancellationToken,
  ): Promise<{ proceed: boolean; reason: string }> {
    const gatedActions: AgentAction[] = [
      AgentAction.SEND_CONTINUE,
      AgentAction.OPEN_NEW_CHAT,
    ];

    if (!gatedActions.includes(action)) {
      return {
        proceed: true,
        reason: "action type does not require visual gate",
      };
    }

    // If screenshot quota is exhausted, check if GENERATING was confirmed recently.
    // If yes: block the action — we cannot risk interrupting an active model just
    // because the screenshot rate-limiter fired. Safety window = 1.5× the minimum
    // screenshot interval (default 3 min × 1.5 = 4.5 min hold after last confirmation).
    // If no recent confirmation: allow with a warning — never block permanently.
    if (!this._screenAnalyzer.canCapture()) {
      const timeSinceGenerating = Date.now() - this._lastConfirmedGeneratingMs;
      const safeWindowMs = this._config.screenshotMinIntervalSecs * 1000 * 1.5;

      if (
        this._lastConfirmedGeneratingMs > 0 &&
        timeSinceGenerating < safeWindowMs
      ) {
        // GENERATING was confirmed recently — hold the action and extend the stall
        // timer so the monitor keeps waiting without re-triggering immediately.
        this._lastActivityMs = Date.now();
        this._log(
          "warn",
          `🛡️ Gate: GENERATING confirmed ${Math.floor(timeSinceGenerating / 1000)}s ago — ` +
            `blocking ${action} during safety hold (quota exhausted, ${Math.floor(safeWindowMs / 1000)}s window)`,
        );
        return {
          proceed: false,
          reason: `GENERATING confirmed ${Math.floor(timeSinceGenerating / 1000)}s ago — safety hold (screenshot quota exhausted)`,
        };
      }

      this._log(
        "warn",
        `🛡️ Gate: screenshot quota reached — allowing ${action} without visual confirmation`,
      );
      return {
        proceed: true,
        reason:
          "screenshot quota reached — proceeding without visual confirmation",
      };
    }

    this._log(
      "info",
      `🛡️ Pre-action gate: capturing screen before ${action}...`,
    );
    this._statusBar.setState(
      this._currentState,
      `🛡️ Gate: verifying before ${action}...`,
    );

    try {
      const gateStart = Date.now();
      const analysis = await this._screenAnalyzer.analyze(token);
      const gateDurationMs = Date.now() - gateStart;

      this._log(
        "info",
        `🛡️ Gate [${action}]: screen=${analysis.state} conf=${(analysis.confidence * 100).toFixed(0)}% — ${analysis.detail}`,
      );

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
      if (analysis.state === AgentState.GENERATING) {
        // Reset stall timer so the next cycle doesn't immediately re-trigger
        this._currentState = AgentState.GENERATING;
        this._lastActivityMs = Date.now();
        return {
          proceed: false,
          reason: `agent is GENERATING (${analysis.detail}) — ${action} blocked`,
        };
      }

      // BLOCK: Semantic mismatch — task done but action is SEND_CONTINUE
      // Correct state so the next cycle chooses OPEN_NEW_CHAT instead
      if (
        action === AgentAction.SEND_CONTINUE &&
        analysis.state === AgentState.COMPLETED
      ) {
        this._currentState = AgentState.COMPLETED;
        this._latestLogEvent = LogEvent.LOOP_STOPPED;
        return {
          proceed: false,
          reason:
            "screen shows COMPLETED — SEND_CONTINUE is semantically wrong; next cycle will use OPEN_NEW_CHAT",
        };
      }

      return { proceed: true, reason: `gate passed: screen=${analysis.state}` };
    } catch (err) {
      this._log(
        "warn",
        `🛡️ Gate screenshot failed: ${err} — allowing ${action} without visual confirmation`,
      );
      return {
        proceed: true,
        reason: `gate screenshot error (${String(err)}) — proceeding`,
      };
    }
  }

  // ─── Post-action state updates ────────────────────────────────────────────

  private _postAction(
    action: AgentAction,
    ok: boolean,
    prevState: AgentState,
  ): void {
    const now = Date.now();

    // Always stamp the cooldown — even on failure (prevents spam retries)
    switch (action) {
      case AgentAction.SEND_CONTINUE:
        this._cooldowns.sendContinue = now;
        if (ok) {
          this._softErrorRetries++;
          this._lastActivityMs = now; // BUG FIX B: reset stall timer after nudge
          this._lastActionMs = now;
          this._recoveryStartMs = now;
          this._currentState = AgentState.RECOVERING;
          this._latestLogEvent = LogEvent.IDLE; // BUG FIX A: clear stale error signal
        }
        break;

      case AgentAction.OPEN_NEW_CHAT:
        this._cooldowns.openNewChat = now;
        if (ok) {
          this._newChatCount++;
          this._lastActivityMs = now; // opening new chat = activity → reset stall timer
          this._lastActionMs = now;
          this._recoveryStartMs = now;
          this._softErrorRetries = 0;
          this._currentState = AgentState.RECOVERING;
          this._latestLogEvent = LogEvent.IDLE; // BUG FIX A: clear stale error signal
        }
        break;

      case AgentAction.STOP_AND_NEW_CHAT:
        this._cooldowns.stopAndNewChat = now;
        this._cooldowns.openNewChat = now;
        if (ok) {
          this._newChatCount++;
          this._lastActivityMs = now; // opening new chat = activity → reset stall timer
          this._lastActionMs = now;
          this._recoveryStartMs = now;
          this._softErrorRetries = 0;
          this._currentState = AgentState.RECOVERING;
          this._latestLogEvent = LogEvent.IDLE; // BUG FIX A: clear stale error signal
        }
        break;

      case AgentAction.CYCLE_MODEL:
        this._cooldowns.cycleModel = now;
        this._cooldowns.openNewChat = now;
        if (ok) {
          this._modelRotations++;
          this._lastActivityMs = now;
          this._softErrorRetries = 0;
          this._currentState = AgentState.RECOVERING;
          this._recoveryStartMs = now;
          this._latestLogEvent = LogEvent.IDLE; // BUG FIX A: clear stale error signal
        }
        break;

      case AgentAction.SWITCH_CHAT_MODEL:
        this._cooldowns.switchChatModel = now;
        if (ok) {
          this._modelRotations++;
          this._lastActivityMs = now; // BUG FIX B: reset stall timer
          this._softErrorRetries = 0;
          this._currentState = AgentState.RECOVERING;
          this._recoveryStartMs = now;
          this._latestLogEvent = LogEvent.IDLE; // BUG FIX A: clear stale error signal
        }
        break;

      case AgentAction.VALIDATE_ZERO_X:
        this._cooldowns.validateZeroX = now;
        if (ok) {
          // Health validated (0x model confirmed) + new chat opened → agent loop prompt sent
          this._modelRotations++;
          this._softErrorRetries = 0;
          this._lastActivityMs = now;
          this._lastActionMs = now;
          this._recoveryStartMs = now;
          this._currentState = AgentState.RECOVERING;
          this._latestLogEvent = LogEvent.IDLE;
        }
        break;
    }
  }

  // ─── Init helpers ─────────────────────────────────────────────────────────

  private _loadConfig(): void {
    const cfg = vscode.workspace.getConfiguration("copilotMonitor");
    this._config = {
      stallWarnSecs: cfg.get("stallWarnSecs", 300),
      stallHardSecs: cfg.get("stallHardSecs", 480),
      screenshotMinIntervalSecs: cfg.get("screenshotMinIntervalSecs", 180),
      maxScreenshotsPerHour: cfg.get("maxScreenshotsPerHour", 15),
      loopPromptFile: cfg.get(
        "loopPromptFile",
        ".prompts/AGENT_LOOP_PROMPT.md",
      ),
      modelPool: cfg.get("modelPool", ["gpt-4o", "gpt-4.1", "gpt-5-mini"]),
      visionModel: cfg.get("visionModel", "gpt-4o"),
    };
  }

  /** Manually reset the cost guard circuit breaker (e.g. from command palette). */
  resetCostGuard(): void {
    this._costGuard?.reset();
    const stats = this._costGuard?.stats;
    this._log(
      "info",
      `[COST GUARD] Manually reset. Session totals: ${stats?.allowed ?? 0} allowed, ${stats?.blocked ?? 0} blocked`,
    );
    vscode.window.showInformationMessage(
      "Copilot Monitor: Cost Guard reset ✅ Monitor is active again.",
    );
  }

  private _initComponents(): void {
    this._logWatcher = new LogWatcher(
      (delta) => this._onLogDelta(delta),
      5_000,
    );

    this._screenAnalyzer = new ScreenAnalyzer(
      this._config.screenshotMinIntervalSecs * 1000,
      this._config.maxScreenshotsPerHour,
    );

    this._executor = new ActionExecutor(
      this._workspaceRoot(),
      this._config.modelPool,
      this._context,
      (level, message) => this._log(level, message),
    );
    if (this._modelManager) {
      this._executor.setModelManager(this._modelManager);
      this._screenAnalyzer.setModelManager(this._modelManager);
      this._executor.setHealthValidatedCallback((ok) => {
        this._health = { validatedOk: ok, validatedAt: Date.now() };
        this._log(
          "info",
          `Health validation result: ${ok ? "OK ✅" : "FAILED ❌"}`,
        );
      });
    }

    const cfg = vscode.workspace.getConfiguration("copilotMonitor");
    this._costGuard = new CostGuard(
      cfg.get("costGuard.maxActionsPerWindow", 8),
      cfg.get("costGuard.windowMinutes", 5) * 60_000,
      cfg.get("costGuard.autoResetMinutes", 10) * 60_000,
    );

    // StatusBar duplication fix: only create once. If start() is called
    // multiple times (e.g. user clicks "Start" again), re-use the existing
    // instance instead of creating a new StatusBarItem each time.
    if (!this._statusBar) {
      this._statusBar = new StatusBar();
      this._context.subscriptions.push(this._statusBar);
    }

    this._audit = new AuditLog(this._workspaceRoot());
    this._log("info", `📋 Audit log: ${this._audit.filePath}`);
  }

  private _workspaceRoot(): string {
    return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? process.cwd();
  }

  /** Returns the absolute path of the audit JSONL file. */
  getAuditPath(): string {
    return this._audit?.filePath ?? "";
  }

  /** Returns the most recent audit entries (most recent first). */
  getRecentAuditEntries(count = 50): AuditEntry[] {
    return this._audit?.recentEntries(count) ?? [];
  }

  private _buildAuditEntry(
    cycle: number,
    startMs: number,
    data: {
      source: string;
      state: AgentState | string;
      action: string;
      reasoning: string;
      ok: boolean | null;
      detail: string;
      costGuard?: { blocked: boolean; reason: string };
    },
  ): AuditEntry {
    const vision = this._lastVisionAnalysis;
    const configuredModel = vscode.workspace
      .getConfiguration("github.copilot.chat")
      .get<string>("languageModel", "");

    const entry: AuditEntry = {
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

  private _log(level: "debug" | "info" | "warn" | "error", msg: string): void {
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
