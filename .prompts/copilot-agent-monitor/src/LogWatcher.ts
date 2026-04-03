/**
 * LogWatcher — Monitors the GitHub Copilot Chat log file in real-time.
 *
 * This is the PRIMARY and FREE signal source. No API calls.
 * The log file is at:
 *   macOS/Linux: ~/Library/Application Support/Code/logs/<session>/GitHub Copilot Chat.log
 *   Windows:     %APPDATA%\Code\logs\<session>\GitHub Copilot Chat.log
 *
 * Log patterns detected:
 *   ccreq:...|success|   → model completed a request turn
 *   ccreq:...|cancelled| → generation was cancelled
 *   ccreq:...|error|     → request failed
 *   rate limit / 429     → rate limited
 *   500 / 503 / overload → server error
 *   context_length / token limit → context full
 *   shouldContinue=false → agent loop stopped
 */

import * as fs from "fs";
import * as path from "path";
import * as os from "os";
import { LogEvent } from "./types";

// ─── Log patterns ─────────────────────────────────────────────────────────────

const PATTERNS: Array<[RegExp, LogEvent]> = [
  [
    /rate.limit|429|Too Many Requests|quota.*exhaust|RateLimitError/i,
    LogEvent.RATE_LIMITED,
  ],
  [
    /\[error\].*(500|503|502|overload|capacity|Internal Server)/i,
    LogEvent.HARD_ERROR,
  ],
  [
    /context_length_exceeded|context.*too.long|token.*limit|exceeds.*token/i,
    LogEvent.CONTEXT_FULL,
  ],
  [/ccreq:.*\|\s*cancelled\s*\|/i, LogEvent.CANCELLED],
  [
    /shouldContinue=false|ToolCallingLoop.*[Ss]top|agent.*loop.*stop/i,
    LogEvent.LOOP_STOPPED,
  ],
  [/ccreq:.*\|\s*success\s*\|/i, LogEvent.SUCCESS],
  [/ccreq:.*\|\s*error\s*\|/i, LogEvent.REQUEST_ERROR],
];

// Noise patterns — skip these lines entirely
const NOISE =
  /failed validation.*schema must be|Tool mcp_aisquare.*failed validation/i;

export interface LogDelta {
  event: LogEvent;
  newLinesCount: number;
  hasActivity: boolean; // true if there are any non-noise new lines
  raw: string[]; // last few meaningful lines
}

type LogEventCallback = (delta: LogDelta) => void;

export class LogWatcher {
  private _logPath: string | null = null;
  private _offset: number = 0;

  // ── Real-time watch ───────────────────────────────────────────────────────
  // Primary: fs.watch on the log file → fires within milliseconds of a write.
  // Fallback heartbeat: polls every 10s to catch log rotations (new session)
  // or platforms where fs.watch is unreliable (some Linux setups).
  // Debounce (50ms): Copilot writes many tiny chunks per turn; grouping them
  // into one callback avoids triggering the state machine N times per request.

  private _fsWatcher: fs.FSWatcher | null = null;
  private _debounceTimer: NodeJS.Timeout | null = null;
  private _heartbeatTimer: NodeJS.Timeout | null = null;
  private _debounceMs: number;
  private _callback: LogEventCallback;

  constructor(callback: LogEventCallback, _pollIntervalMs = 5_000) {
    // _pollIntervalMs param kept for API compat but ignored — heartbeat is fixed 10s
    this._callback = callback;
    this._debounceMs = 50; // 50ms debounce — groups rapid successive writes
  }

  start(): void {
    this._resolveAndWatch();
    // Heartbeat: re-resolves log path every 10s to handle VS Code restarts /
    // log rotations where the file path changes.
    this._heartbeatTimer = setInterval(() => this._resolveAndWatch(), 10_000);
  }

  stop(): void {
    this._stopFsWatcher();
    if (this._debounceTimer) {
      clearTimeout(this._debounceTimer);
      this._debounceTimer = null;
    }
    if (this._heartbeatTimer) {
      clearInterval(this._heartbeatTimer);
      this._heartbeatTimer = null;
    }
  }

  // ── Resolve log file and attach fs.watch ─────────────────────────────────

  private _resolveAndWatch(): void {
    const logPath = this._findCopilotLog();
    if (!logPath) {
      return;
    }

    if (logPath !== this._logPath) {
      // New log file detected (session restart or first run) → seek to END
      // to avoid replaying historical error events from previous sessions.
      this._logPath = logPath;
      try {
        this._offset = fs.statSync(logPath).size;
      } catch {
        this._offset = 0;
      }
      this._stopFsWatcher();
      this._attachFsWatch(logPath);
    } else if (!this._fsWatcher) {
      // Same path but watcher was lost (e.g. the file was recreated)
      this._attachFsWatch(logPath);
    }
  }

  private _attachFsWatch(logPath: string): void {
    try {
      // 'change' fires synchronously every time the OS flushes a write to the file.
      // No polling interval — purely event-driven.
      this._fsWatcher = fs.watch(logPath, { persistent: false }, (event) => {
        if (event === "rename") {
          // File was rotated / renamed → re-resolve on next heartbeat
          this._stopFsWatcher();
          return;
        }
        // event === 'change' — new content written, debounce before reading
        this._scheduleDebounce();
      });

      this._fsWatcher.on("error", () => {
        // Watcher error (file deleted, permissions) → will re-attach on heartbeat
        this._stopFsWatcher();
      });
    } catch {
      // fs.watch not supported or file gone — heartbeat will retry
    }
  }

  private _stopFsWatcher(): void {
    if (this._fsWatcher) {
      try {
        this._fsWatcher.close();
      } catch {
        /* ignore */
      }
      this._fsWatcher = null;
    }
  }

  // ── Debounce → read new bytes ─────────────────────────────────────────────

  private _scheduleDebounce(): void {
    if (this._debounceTimer) {
      // Reset debounce: more writes are coming in this burst — wait for them
      clearTimeout(this._debounceTimer);
    }
    this._debounceTimer = setTimeout(() => {
      this._debounceTimer = null;
      this._readNewBytes();
    }, this._debounceMs);
  }

  private _readNewBytes(): void {
    if (!this._logPath) {
      return;
    }
    try {
      const stat = fs.statSync(this._logPath);
      if (stat.size < this._offset) {
        this._offset = 0;
      } // log rotated
      if (stat.size <= this._offset) {
        return;
      } // no new data

      const fd = fs.openSync(this._logPath, "r");
      const length = stat.size - this._offset;
      const buffer = Buffer.alloc(length);
      fs.readSync(fd, buffer, 0, length, this._offset);
      fs.closeSync(fd);
      this._offset += length;

      const lines = buffer.toString("utf-8", 0, length).split("\n");
      this._processLines(lines);
    } catch {
      // Log file locked or rotated — skip silently
    }
  }

  private _processLines(lines: string[]): void {
    const meaningful: string[] = [];
    const eventCounts: Partial<Record<LogEvent, number>> = {};

    for (const line of lines) {
      if (!line.trim() || NOISE.test(line)) {
        continue;
      }
      meaningful.push(line);

      for (const [pattern, event] of PATTERNS) {
        if (pattern.test(line)) {
          eventCounts[event] = (eventCounts[event] ?? 0) + 1;
          break;
        }
      }
    }

    if (meaningful.length === 0) {
      return;
    }

    // Determine dominant event (priority order)
    let dominantEvent: LogEvent = LogEvent.IDLE;
    if (eventCounts[LogEvent.RATE_LIMITED]) {
      dominantEvent = LogEvent.RATE_LIMITED;
    } else if (eventCounts[LogEvent.CONTEXT_FULL]) {
      dominantEvent = LogEvent.CONTEXT_FULL;
    } else if (
      eventCounts[LogEvent.HARD_ERROR] &&
      !eventCounts[LogEvent.SUCCESS]
    ) {
      dominantEvent = LogEvent.HARD_ERROR;
    } else if (eventCounts[LogEvent.LOOP_STOPPED]) {
      dominantEvent = LogEvent.LOOP_STOPPED;
    } else if (
      eventCounts[LogEvent.CANCELLED] &&
      !eventCounts[LogEvent.SUCCESS]
    ) {
      dominantEvent = LogEvent.CANCELLED;
    } else if (eventCounts[LogEvent.SUCCESS]) {
      dominantEvent = LogEvent.SUCCESS;
    } else if (eventCounts[LogEvent.REQUEST_ERROR]) {
      dominantEvent = LogEvent.REQUEST_ERROR;
    }

    // If no terminal event matched but there is real log output, emit ACTIVE.
    // This covers every "silent generating" state: "Thinking...", "Preparing",
    // "Getting chat ready", "Compacting conversation", inter-tool-call pauses,
    // and any other UI state where the model is working but has not yet written
    // a ccreq terminal pattern. This prevents the Monitor from misreading
    // active processing as IDLE and firing premature SEND_CONTINUE actions.
    const emittedEvent =
      dominantEvent === LogEvent.IDLE ? LogEvent.ACTIVE : dominantEvent;

    this._callback({
      event: emittedEvent,
      newLinesCount: meaningful.length,
      hasActivity: meaningful.length > 0,
      raw: meaningful.slice(-10), // last 10 lines for context
    });
  }

  private _findCopilotLog(): string | null {
    let logsBase: string;

    if (process.platform === "darwin") {
      logsBase = path.join(
        os.homedir(),
        "Library",
        "Application Support",
        "Code",
        "logs",
      );
    } else if (process.platform === "win32") {
      logsBase = path.join(process.env.APPDATA ?? "", "Code", "logs");
    } else {
      logsBase = path.join(os.homedir(), ".config", "Code", "logs");
    }

    if (!fs.existsSync(logsBase)) {
      return null;
    }

    let bestPath: string | null = null;
    let bestMtime = 0;

    try {
      for (const session of fs.readdirSync(logsBase)) {
        const sessionDir = path.join(logsBase, session);
        if (!fs.statSync(sessionDir).isDirectory()) {
          continue;
        }

        // Search recursively for GitHub Copilot Chat.log
        const found = this._findInDir(sessionDir, "GitHub Copilot Chat.log");
        if (found) {
          const mtime = fs.statSync(found).mtimeMs;
          if (mtime > bestMtime) {
            bestMtime = mtime;
            bestPath = found;
          }
        }
      }
    } catch {
      /* directory may not exist yet */
    }

    return bestPath;
  }

  private _findInDir(dir: string, target: string): string | null {
    try {
      for (const entry of fs.readdirSync(dir)) {
        const full = path.join(dir, entry);
        if (entry === target && fs.statSync(full).isFile()) {
          return full;
        }
        if (fs.statSync(full).isDirectory()) {
          const found = this._findInDir(full, target);
          if (found) {
            return found;
          }
        }
      }
    } catch {
      /* skip */
    }
    return null;
  }
}
