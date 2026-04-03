"use strict";
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
exports.LogWatcher = void 0;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const os = __importStar(require("os"));
// ─── Log patterns ─────────────────────────────────────────────────────────────
const PATTERNS = [
    [
        /rate.limit|429|Too Many Requests|quota.*exhaust|RateLimitError/i,
        "rate_limited" /* LogEvent.RATE_LIMITED */,
    ],
    [
        /\[error\].*(500|503|502|overload|capacity|Internal Server)/i,
        "hard_error" /* LogEvent.HARD_ERROR */,
    ],
    [
        /context_length_exceeded|context.*too.long|token.*limit|exceeds.*token/i,
        "context_full" /* LogEvent.CONTEXT_FULL */,
    ],
    [/ccreq:.*\|\s*cancelled\s*\|/i, "cancelled" /* LogEvent.CANCELLED */],
    [
        /shouldContinue=false|ToolCallingLoop.*[Ss]top|agent.*loop.*stop/i,
        "loop_stopped" /* LogEvent.LOOP_STOPPED */,
    ],
    [/ccreq:.*\|\s*success\s*\|/i, "success" /* LogEvent.SUCCESS */],
    [/ccreq:.*\|\s*error\s*\|/i, "request_error" /* LogEvent.REQUEST_ERROR */],
];
// Noise patterns — skip these lines entirely
const NOISE = /failed validation.*schema must be|Tool mcp_aisquare.*failed validation/i;
class LogWatcher {
    _logPath = null;
    _offset = 0;
    // ── Real-time watch ───────────────────────────────────────────────────────
    // Primary: fs.watch on the log file → fires within milliseconds of a write.
    // Fallback heartbeat: polls every 10s to catch log rotations (new session)
    // or platforms where fs.watch is unreliable (some Linux setups).
    // Debounce (50ms): Copilot writes many tiny chunks per turn; grouping them
    // into one callback avoids triggering the state machine N times per request.
    _fsWatcher = null;
    _debounceTimer = null;
    _heartbeatTimer = null;
    _debounceMs;
    _callback;
    constructor(callback, _pollIntervalMs = 5_000) {
        // _pollIntervalMs param kept for API compat but ignored — heartbeat is fixed 10s
        this._callback = callback;
        this._debounceMs = 50; // 50ms debounce — groups rapid successive writes
    }
    start() {
        this._resolveAndWatch();
        // Heartbeat: re-resolves log path every 10s to handle VS Code restarts /
        // log rotations where the file path changes.
        this._heartbeatTimer = setInterval(() => this._resolveAndWatch(), 10_000);
    }
    stop() {
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
    _resolveAndWatch() {
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
            }
            catch {
                this._offset = 0;
            }
            this._stopFsWatcher();
            this._attachFsWatch(logPath);
        }
        else if (!this._fsWatcher) {
            // Same path but watcher was lost (e.g. the file was recreated)
            this._attachFsWatch(logPath);
        }
    }
    _attachFsWatch(logPath) {
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
        }
        catch {
            // fs.watch not supported or file gone — heartbeat will retry
        }
    }
    _stopFsWatcher() {
        if (this._fsWatcher) {
            try {
                this._fsWatcher.close();
            }
            catch {
                /* ignore */
            }
            this._fsWatcher = null;
        }
    }
    // ── Debounce → read new bytes ─────────────────────────────────────────────
    _scheduleDebounce() {
        if (this._debounceTimer) {
            // Reset debounce: more writes are coming in this burst — wait for them
            clearTimeout(this._debounceTimer);
        }
        this._debounceTimer = setTimeout(() => {
            this._debounceTimer = null;
            this._readNewBytes();
        }, this._debounceMs);
    }
    _readNewBytes() {
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
        }
        catch {
            // Log file locked or rotated — skip silently
        }
    }
    _processLines(lines) {
        const meaningful = [];
        const eventCounts = {};
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
        let dominantEvent = "idle" /* LogEvent.IDLE */;
        if (eventCounts["rate_limited" /* LogEvent.RATE_LIMITED */]) {
            dominantEvent = "rate_limited" /* LogEvent.RATE_LIMITED */;
        }
        else if (eventCounts["context_full" /* LogEvent.CONTEXT_FULL */]) {
            dominantEvent = "context_full" /* LogEvent.CONTEXT_FULL */;
        }
        else if (eventCounts["hard_error" /* LogEvent.HARD_ERROR */] &&
            !eventCounts["success" /* LogEvent.SUCCESS */]) {
            dominantEvent = "hard_error" /* LogEvent.HARD_ERROR */;
        }
        else if (eventCounts["loop_stopped" /* LogEvent.LOOP_STOPPED */]) {
            dominantEvent = "loop_stopped" /* LogEvent.LOOP_STOPPED */;
        }
        else if (eventCounts["cancelled" /* LogEvent.CANCELLED */] &&
            !eventCounts["success" /* LogEvent.SUCCESS */]) {
            dominantEvent = "cancelled" /* LogEvent.CANCELLED */;
        }
        else if (eventCounts["success" /* LogEvent.SUCCESS */]) {
            dominantEvent = "success" /* LogEvent.SUCCESS */;
        }
        else if (eventCounts["request_error" /* LogEvent.REQUEST_ERROR */]) {
            dominantEvent = "request_error" /* LogEvent.REQUEST_ERROR */;
        }
        // If no terminal event matched but there is real log output, emit ACTIVE.
        // This covers every "silent generating" state: "Thinking...", "Preparing",
        // "Getting chat ready", "Compacting conversation", inter-tool-call pauses,
        // and any other UI state where the model is working but has not yet written
        // a ccreq terminal pattern. This prevents the Monitor from misreading
        // active processing as IDLE and firing premature SEND_CONTINUE actions.
        const emittedEvent = dominantEvent === "idle" /* LogEvent.IDLE */ ? "active" /* LogEvent.ACTIVE */ : dominantEvent;
        this._callback({
            event: emittedEvent,
            newLinesCount: meaningful.length,
            hasActivity: meaningful.length > 0,
            raw: meaningful.slice(-10), // last 10 lines for context
        });
    }
    _findCopilotLog() {
        let logsBase;
        if (process.platform === "darwin") {
            logsBase = path.join(os.homedir(), "Library", "Application Support", "Code", "logs");
        }
        else if (process.platform === "win32") {
            logsBase = path.join(process.env.APPDATA ?? "", "Code", "logs");
        }
        else {
            logsBase = path.join(os.homedir(), ".config", "Code", "logs");
        }
        if (!fs.existsSync(logsBase)) {
            return null;
        }
        let bestPath = null;
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
        }
        catch {
            /* directory may not exist yet */
        }
        return bestPath;
    }
    _findInDir(dir, target) {
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
        }
        catch {
            /* skip */
        }
        return null;
    }
}
exports.LogWatcher = LogWatcher;
