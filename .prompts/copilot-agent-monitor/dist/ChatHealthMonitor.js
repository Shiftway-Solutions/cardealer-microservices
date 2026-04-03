"use strict";
/**
 * ChatHealthMonitor — Proactive chat session health monitoring via CDP.
 *
 * Problem: VS Code Copilot Chat gets slower and can freeze when a session
 * accumulates too many messages (typically >60-80 exchanges). The monitor
 * detects this BEFORE the context limit error fires, allowing a graceful
 * reset before freezing occurs.
 *
 * What it measures (all via CDP, zero API cost):
 *   - messageCount    : number of .interactive-item-container elements
 *   - responseTimeSec : time from last send to first DOM response update
 *   - domSizeKb       : total text length of the chat DOM (proxy for slowdown)
 *   - hasSpinnerStuck : spinner present for > spinnerStuckSecs (default 90s)
 *
 * Thresholds:
 *   HEAVY   → messageCount > 50  (chat is getting slow, recommend new chat soon)
 *   CRITICAL→ messageCount > 75  (chat likely frozen or about to freeze → RESET)
 *   STUCK   → spinner present > spinnerStuckSecs (generation stuck)
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.ChatHealthMonitor = void 0;
const CDPClient_1 = require("./CDPClient");
// ─── CDP script to measure chat DOM size ─────────────────────────────────────
const MEASURE_SCRIPT = /* javascript */ `
(function() {
  var containers = document.querySelectorAll('.interactive-item-container');
  var totalChars = 0;
  for (var i = 0; i < containers.length; i++) {
    totalChars += (containers[i].textContent || '').length;
  }
  var hasSpinner = !!document.querySelector('.codicon-loading, .codicon-modifier-spin, .progress-bit');
  return JSON.stringify({
    count: containers.length,
    chars: totalChars,
    hasSpinner: hasSpinner
  });
})()
`.trim();
// ─── Thresholds ───────────────────────────────────────────────────────────────
const MSG_HEAVY = 50; // warn and schedule new chat
const MSG_CRITICAL = 75; // force new chat
const SPINNER_STUCK_SEC = 90; // spinner present this long = stuck
class ChatHealthMonitor {
    _cdp;
    _timer = null;
    _intervalMs;
    _callback;
    _running = false;
    _cdpPort;
    /** Timestamp when spinner was first detected (0 = no spinner) */
    _spinnerFirstSeenMs = 0;
    /** Last health reading */
    _lastHealth = null;
    constructor(cdpPort = 9222, callback, intervalMs = 30_000) {
        this._cdpPort = cdpPort;
        this._cdp = new CDPClient_1.CDPClient(cdpPort);
        this._callback = callback;
        this._intervalMs = intervalMs;
    }
    async start() {
        if (this._running)
            return true;
        if (!(await this._cdp.isAvailable()))
            return false;
        try {
            await this._cdp.connect();
        }
        catch {
            return false;
        }
        this._running = true;
        this._timer = setInterval(() => void this._poll(), this._intervalMs);
        return true;
    }
    stop() {
        this._running = false;
        if (this._timer) {
            clearInterval(this._timer);
            this._timer = null;
        }
        this._cdp.disconnect();
    }
    get active() {
        return this._running;
    }
    get lastHealth() {
        return this._lastHealth;
    }
    /** Force an immediate measurement (called by Monitor before deciding) */
    async measure() {
        if (!this._running)
            return null;
        return this._poll();
    }
    // ─── Poll ────────────────────────────────────────────────────────────────
    async _poll() {
        let raw;
        try {
            raw = await this._cdp.evaluate(MEASURE_SCRIPT);
        }
        catch {
            return null;
        }
        let parsed;
        try {
            parsed = JSON.parse(raw);
        }
        catch {
            return null;
        }
        const now = Date.now();
        // Track spinner stuck detector
        if (parsed.hasSpinner) {
            if (this._spinnerFirstSeenMs === 0) {
                this._spinnerFirstSeenMs = now;
            }
        }
        else {
            this._spinnerFirstSeenMs = 0;
        }
        const spinnerDurationSec = this._spinnerFirstSeenMs > 0
            ? (now - this._spinnerFirstSeenMs) / 1000
            : 0;
        const health = {
            messageCount: parsed.count,
            estimatedTokens: Math.round(parsed.chars / 4),
            domSizeKb: Math.round(parsed.chars / 1024),
            isGettingHeavy: parsed.count > MSG_HEAVY,
            isCritical: parsed.count > MSG_CRITICAL,
            hasSpinnerStuck: spinnerDurationSec > SPINNER_STUCK_SEC,
            capturedAt: now,
        };
        this._lastHealth = health;
        if (health.isCritical || health.isGettingHeavy || health.hasSpinnerStuck) {
            this._callback(health);
        }
        return health;
    }
}
exports.ChatHealthMonitor = ChatHealthMonitor;
