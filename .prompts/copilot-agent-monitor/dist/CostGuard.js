"use strict";
/**
 * CostGuard — Sliding-window rate limiter for agent actions.
 *
 * Prevents runaway loops where the state machine spams SEND_CONTINUE /
 * OPEN_NEW_CHAT requests. Once the window fills up, all actions are blocked
 * until the window expires or the user manually resets.
 *
 * Constructor:
 *   new CostGuard(maxActionsPerWindow, windowMs, autoResetMs)
 *
 * Usage:
 *   const guard = costGuard.check();
 *   if (!guard.allowed) { ... }
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.CostGuard = void 0;
class CostGuard {
    _maxActions;
    _windowMs;
    _autoResetMs;
    /** Timestamps of allowed actions within the current window. */
    _timestamps = [];
    /** Whether the guard has already tripped (notification sent). */
    _tripped = false;
    /** Session-wide counters for diagnostics. */
    _allowedCount = 0;
    _blockedCount = 0;
    /**
     * @param maxActionsPerWindow  Maximum action tokens in the sliding window.
     * @param windowMs             Sliding window width in milliseconds.
     * @param autoResetMs          After being blocked for this long, auto-reset (safety valve).
     */
    constructor(maxActionsPerWindow, windowMs, autoResetMs) {
        this._maxActions = maxActionsPerWindow;
        this._windowMs = windowMs;
        this._autoResetMs = autoResetMs;
    }
    /**
     * Call before every action. Returns whether the action is allowed.
     * If allowed: records a token in the window.
     * If blocked: returns reason string and justTripped flag.
     */
    check() {
        const now = Date.now();
        // Auto-reset: if we've been blocked for longer than autoResetMs, clear the window.
        if (this._tripped && this._timestamps.length > 0) {
            const oldestTs = this._timestamps[0];
            if (now - oldestTs > this._autoResetMs) {
                this._timestamps = [];
                this._tripped = false;
            }
        }
        // Evict timestamps outside the sliding window.
        this._timestamps = this._timestamps.filter((ts) => now - ts < this._windowMs);
        if (this._timestamps.length >= this._maxActions) {
            const wasTripped = this._tripped;
            this._tripped = true;
            this._blockedCount++;
            const windowSec = Math.round(this._windowMs / 1000);
            const reason = `Actions limit (${this._maxActions}/${windowSec}s window) exceeded — monitor paused to prevent loops`;
            return { allowed: false, reason, justTripped: !wasTripped };
        }
        // Allow: record token.
        this._timestamps.push(now);
        this._allowedCount++;
        return { allowed: true, reason: "", justTripped: false };
    }
    /** Manually clear the window (e.g. user clicks "Reset Guard"). */
    reset() {
        this._timestamps = [];
        this._tripped = false;
    }
    /** Session-wide stats (for diagnostics / log messages). */
    get stats() {
        return {
            allowed: this._allowedCount,
            blocked: this._blockedCount,
        };
    }
}
exports.CostGuard = CostGuard;
