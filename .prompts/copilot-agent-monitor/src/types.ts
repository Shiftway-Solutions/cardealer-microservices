// ─── Agent States ─────────────────────────────────────────────────────────────
// Each state represents what GitHub Copilot Agent is doing RIGHT NOW,
// determined from the screenshot + log analysis.

export const enum AgentState {
  // Model is actively generating: progress %, "Running...", "Writing...", etc.
  GENERATING = "GENERATING",
  // Model finished its turn cleanly — footer visible, no spinner
  COMPLETED = "COMPLETED",
  // Waiting for user input — chat is idle, no error, no footer
  IDLE = "IDLE",
  // No activity for stallWarnSecs — needs a nudge
  STALLED_SOFT = "STALLED_SOFT",
  // No activity for stallHardSecs — needs a fresh chat
  STALLED_HARD = "STALLED_HARD",
  // "rate limit" / "429" / "quota exhausted" visible in chat
  ERROR_RATE_LIMIT = "ERROR_RATE_LIMIT",
  // "500" / "503" / "overloaded" — transient server error, retry-able
  ERROR_HARD = "ERROR_HARD",
  // "context too long" / "token limit" — session must be reset
  ERROR_CONTEXT = "ERROR_CONTEXT",
  // "switch model" / "model not available" — switch model in current session
  ERROR_SWITCH_MODEL = "ERROR_SWITCH_MODEL",
  // VS Code not visible or chat panel closed
  VSCODE_HIDDEN = "VSCODE_HIDDEN",
  // Just took an action, waiting to see the result (120s window)
  RECOVERING = "RECOVERING",
  // Monitor is off
  STOPPED = "STOPPED",
  // Chat session has >50 messages — proactive reset before freeze
  CHAT_HEAVY = "CHAT_HEAVY",
  // RAM or disk critically low — must free resources before continuing
  RESOURCE_PRESSURE = "RESOURCE_PRESSURE",
}

// ─── Actions ──────────────────────────────────────────────────────────────────

export const enum AgentAction {
  WAIT = "WAIT",
  SEND_CONTINUE = "SEND_CONTINUE",
  OPEN_NEW_CHAT = "OPEN_NEW_CHAT",
  STOP_AND_NEW_CHAT = "STOP_AND_NEW_CHAT",
  CYCLE_MODEL = "CYCLE_MODEL", // cycle to next model in pool
  SWITCH_CHAT_MODEL = "SWITCH_CHAT_MODEL", // change model in current session
  SELECT_SPECIFIC_MODEL = "SELECT_SPECIFIC_MODEL", // switch to a named model directly
  VALIDATE_ZERO_X = "VALIDATE_ZERO_X", // switch to 0x model → yes/no health check → screenshot → if OK stay on 0x + new chat
  FOCUS_VSCODE = "FOCUS_VSCODE",
  /** Free system RAM: notify user + show Docker containers consuming memory */
  RELEASE_MEMORY = "RELEASE_MEMORY",
  /** docker system prune (dangling images, stopped containers, build cache — no volumes) */
  PRUNE_DOCKER_CACHE = "PRUNE_DOCKER_CACHE",
  /** Chat session is too long (>50 msgs) — open fresh chat before freeze */
  RESET_HEAVY_CHAT = "RESET_HEAVY_CHAT",
}

// ─── Log Event Types (from Copilot Chat log file) ─────────────────────────────

export const enum LogEvent {
  SUCCESS = "success",
  CANCELLED = "cancelled",
  RATE_LIMITED = "rate_limited",
  HARD_ERROR = "hard_error",
  CONTEXT_FULL = "context_full",
  LOOP_STOPPED = "loop_stopped",
  REQUEST_ERROR = "request_error",
  // Non-terminal activity: log received content but no ccreq pattern matched.
  // Covers: "Thinking...", "Preparing", "Getting chat ready", inter-tool-call
  // pauses, "Compacting conversation", and any other UI states where the model
  // is working but has not yet emitted a terminal ccreq event.
  ACTIVE = "active",
  IDLE = "idle",
}

// ─── Screenshot Analysis Result ───────────────────────────────────────────────

export interface VisualAnalysis {
  state: AgentState;
  confidence: number; // 0.0 – 1.0
  detail: string; // human-readable description of what was seen
  errorText?: string; // exact error text if an error state was detected
  rawResponse: string; // raw JSON from the model
  /** Family name of the LM used for this analysis (e.g. "gpt-4o"). */
  visionModelId: string;
  /** Was the model classified as 0x (free / no quota cost)? */
  visionIsZeroX: boolean;
}

// ─── Monitor Cycle Result ─────────────────────────────────────────────────────

export interface CycleResult {
  state: AgentState;
  action: AgentAction;
  source: "log" | "screenshot" | "timer" | "override";
  reasoning: string;
  executedAt: Date;
  actionOk: boolean;
}

// ─── Cooldown Registry ────────────────────────────────────────────────────────

export interface Cooldowns {
  sendContinue: number; // timestamp ms
  openNewChat: number;
  stopAndNewChat: number;
  cycleModel: number;
  switchChatModel: number;
  validateZeroX: number; // last 0x health-check timestamp
  screenshot: number; // last screenshot taken
}

// ─── Monitor Config (resolved from vscode settings) ───────────────────────────

// ─── Health Validation Result ────────────────────────────────────────────────

export interface HealthValidation {
  validatedOk: boolean; // true = 0x model responded without error
  validatedAt: number; // timestamp ms (0 = never validated)
}

// ─── Monitor Config (resolved from vscode settings) ──────────────────────────

export interface MonitorConfig {
  stallWarnSecs: number;
  stallHardSecs: number;
  screenshotMinIntervalSecs: number;
  maxScreenshotsPerHour: number;
  loopPromptFile: string;
  modelPool: string[];
  visionModel: string;
}

// ─── Last Chat Message Reading ────────────────────────────────────────────────
// Classifies the last visible response in the chat DOM so the StateMachine
// can make content-aware decisions instead of relying purely on timers.

export const enum LastMessageCategory {
  /** Model finished its task cleanly ("I've completed...", "All done", etc.) */
  TASK_COMPLETE = "TASK_COMPLETE",
  /** Model asked a question or is waiting for user input ("Would you like...?") */
  WAITING_FOR_INPUT = "WAITING_FOR_INPUT",
  /** An error is visible in chat ("rate limit", "500", "context too long", etc.) */
  ERROR_VISIBLE = "ERROR_VISIBLE",
  /** Chat suggests switching to a different model ("model not available", "try a different model", etc.) */
  ERROR_SWITCH_MODEL = "ERROR_SWITCH_MODEL",
  /** Model appears actively mid-task (tool call results still open, partial output) */
  STILL_WORKING = "STILL_WORKING",
  /** Could not read the chat DOM or text is too ambiguous to classify */
  UNKNOWN = "UNKNOWN",
}

export interface LastMessageReading {
  category: LastMessageCategory;
  /** Raw text of the last response (first 500 chars) */
  text: string;
  /** 0.0–1.0 — how confident we are in the classification */
  confidence: number;
}

// ─── Cooldown durations (ms) ──────────────────────────────────────────────────

export const COOLDOWN_MS = {
  sendContinue: 60_000, //  1 min
  openNewChat: 90_000, //  1.5 min
  stopAndNewChat: 90_000, //  1.5 min
  cycleModel: 120_000, //  2 min
  switchChatModel: 90_000, //  1.5 min
  validateZeroX: 180_000, //  3 min between health-checks
  recovering: 120_000, //  2 min post-action verification window
} as const;

// ─── DOM Event Types (from ChatDOMWatcher / MutationObserver via CDP) ─────────

export const enum DOMEvent {
  /** A .codicon-loading or .progress-bit spinner was added to the DOM. */
  SPINNER = "SPINNER",
  /** A progress-tree row with recognizable text (e.g. "Thinking...", "Searching...") was added. */
  PROGRESS_TEXT = "PROGRESS_TEXT",
  /** A new .interactive-item-container.interactive-response was added (new response block). */
  NEW_RESPONSE = "NEW_RESPONSE",
  /** A text node inside a progress area changed — includes streaming status text. */
  TEXT_CHANGE = "TEXT_CHANGE",
  /** Progress text captured by the 500ms stop-button poll (still active generation). */
  PROGRESS_POLL = "PROGRESS_POLL",
  /** The Stop button [aria-label="Stop"] just appeared — generation started. */
  STOP_BTN_APPEARED = "STOP_BTN_APPEARED",
  /** The Stop button just disappeared — generation ended. */
  STOP_BTN_GONE = "STOP_BTN_GONE",
  /** User is typing in the chat input box — resets inactivity timer. */
  USER_TYPING = "USER_TYPING",
}

export interface DOMQueueItem {
  type: DOMEvent;
  text: string;
  ts: number; // Date.now() at the time of observation in the renderer
}

export interface DOMDelta {
  events: DOMQueueItem[];
  /** True when any event indicates the model is actively generating. */
  isGenerating: boolean;
  /** True when the stop button just disappeared and no spinner/progress events preceded it. */
  isCompleted: boolean;
  hasActivity: boolean;
  /** Extracted progress/status texts (e.g. "Thinking...", "Searching for files"). */
  progressTexts: string[];
  /** True when the user is actively typing in the chat input — resets inactivity timer. */
  isUserTyping: boolean;
}

// ─── Premium (1x) request tracking ───────────────────────────────────────────

/** One entry per premium model session start (applyBestOneX succeeded). */
export interface PremiumRequestEntry {
  /** Unix timestamp ms. */
  ts: number;
  /** The 1x model that was activated (e.g. "claude-sonnet-4-5"). */
  modelId: string;
  /** What triggered this switch (e.g. "applyBestOneX", "sendContinue"). */
  trigger: string;
}

/** Aggregate stats for premium request display. */
export interface PremiumRequestStats {
  /** Total model session starts on a 1x model this VS Code session. */
  sessionCount: number;
  /** Total prompts sent while a 1x model was active. */
  promptCount: number;
  /** Combined total (sessions + prompts) = estimated billable requests. */
  totalRequests: number;
  /** Recent log entries. */
  log: PremiumRequestEntry[];
}
