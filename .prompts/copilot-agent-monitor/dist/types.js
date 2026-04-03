"use strict";
// ─── Agent States ─────────────────────────────────────────────────────────────
// Each state represents what GitHub Copilot Agent is doing RIGHT NOW,
// determined from the screenshot + log analysis.
Object.defineProperty(exports, "__esModule", { value: true });
exports.COOLDOWN_MS = void 0;
// ─── Cooldown durations (ms) ──────────────────────────────────────────────────
exports.COOLDOWN_MS = {
    sendContinue: 60_000, //  1 min
    openNewChat: 90_000, //  1.5 min
    stopAndNewChat: 90_000, //  1.5 min
    cycleModel: 120_000, //  2 min
    switchChatModel: 90_000, //  1.5 min
    validateZeroX: 180_000, //  3 min between health-checks
    recovering: 120_000, //  2 min post-action verification window
};
