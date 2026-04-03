"use strict";
/**
 * StateMachine — Pure decision logic.
 *
 * Input:  current AgentState + timers + counters
 * Output: AgentAction to execute
 *
 * No side effects, no API calls. Deterministic given inputs.
 * The 8 rows of the decision table from the audit:
 *
 *  State              │ Action
 * ────────────────────┼──────────────────────
 *  GENERATING         │ WAIT
 *  COMPLETED / IDLE   │ WAIT (activity just happened)
 *  STALLED_SOFT       │ SEND_CONTINUE
 *  STALLED_HARD       │ OPEN_NEW_CHAT
 *  ERROR_RATE_LIMIT   │ SWITCH_CHAT_MODEL (in-session) or CYCLE_MODEL
 *  ERROR_HARD (<3x)   │ SEND_CONTINUE
 *  ERROR_HARD (≥3x)   │ OPEN_NEW_CHAT
 *  ERROR_CONTEXT      │ STOP_AND_NEW_CHAT
 *  ERROR_SWITCH_MODEL │ SWITCH_CHAT_MODEL
 *  VSCODE_HIDDEN      │ FOCUS_VSCODE
 *  RECOVERING         │ WAIT (still in 2-min verification window)
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.StateMachine = void 0;
const types_1 = require("./types");
const MODEL_ERROR_CATEGORIES = new Set([
    "ERROR_SWITCH_MODEL" /* LastMessageCategory.ERROR_SWITCH_MODEL */,
]);
class StateMachine {
    decide(input) {
        const { state } = input;
        // ── 1. Recovery window — always wait, no matter what ────────────────────
        if (state === "RECOVERING" /* AgentState.RECOVERING */) {
            if (input.msSinceRecoveryStart < types_1.COOLDOWN_MS.recovering) {
                const remaining = Math.ceil((types_1.COOLDOWN_MS.recovering - input.msSinceRecoveryStart) / 1000);
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: `Recovering — verifying result (${remaining}s remaining)`,
                };
            }
            // Recovery window expired without normalization → fall through to re-evaluate
        }
        // ── 2. Model is actively working — never interrupt ───────────────────────
        if (state === "GENERATING" /* AgentState.GENERATING */) {
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Model is generating — not interrupting",
            };
        }
        // ── 3. Context is full — top priority after generating ───────────────────
        if (state === "ERROR_CONTEXT" /* AgentState.ERROR_CONTEXT */) {
            if (this._canDo(input.cooldowns.stopAndNewChat, types_1.COOLDOWN_MS.stopAndNewChat)) {
                return {
                    action: "STOP_AND_NEW_CHAT" /* AgentAction.STOP_AND_NEW_CHAT */,
                    reasoning: "Context window full — stopping and opening fresh chat",
                };
            }
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Context full but stop+new_chat on cooldown",
            };
        }
        // ── 4. Rate limit — health-check with 0x, then switch to next 0x ──────────
        if (state === "ERROR_RATE_LIMIT" /* AgentState.ERROR_RATE_LIMIT */) {
            // If already health-validated OK recently, go straight to model switch
            const validatedRecently = input.health.validatedOk &&
                Date.now() - input.health.validatedAt < 10 * 60_000; // within 10 min
            if (!validatedRecently) {
                if (this._canDo(input.cooldowns.validateZeroX, types_1.COOLDOWN_MS.validateZeroX)) {
                    return {
                        action: "VALIDATE_ZERO_X" /* AgentAction.VALIDATE_ZERO_X */,
                        reasoning: "Rate limit detected — switching to 0x model, sending yes/no health check",
                    };
                }
            }
            if (this._canDo(input.cooldowns.switchChatModel, types_1.COOLDOWN_MS.switchChatModel)) {
                return {
                    action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
                    reasoning: "Rate limit — health OK, switching to next 0x model in current session",
                };
            }
            if (this._canDo(input.cooldowns.cycleModel, types_1.COOLDOWN_MS.cycleModel)) {
                return {
                    action: "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */,
                    reasoning: "Rate limit — rotating model via settings (chat switch on cooldown)",
                };
            }
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Rate limited but all model-switch actions on cooldown",
            };
        }
        // ── 5. Model suggests switching ──────────────────────────────────────────
        if (state === "ERROR_SWITCH_MODEL" /* AgentState.ERROR_SWITCH_MODEL */) {
            if (this._canDo(input.cooldowns.switchChatModel, types_1.COOLDOWN_MS.switchChatModel)) {
                return {
                    action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
                    reasoning: "Chat UI suggests switching model — complying",
                };
            }
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Switch-model suggested but on cooldown",
            };
        }
        // ── 6. Hard server error (500/503) ───────────────────────────────────────
        if (state === "ERROR_HARD" /* AgentState.ERROR_HARD */) {
            // After 2 retries, health-check with 0x before opening new chat
            if (input.softErrorRetries >= 2) {
                const validatedRecently = input.health.validatedOk &&
                    Date.now() - input.health.validatedAt < 10 * 60_000;
                if (!validatedRecently &&
                    this._canDo(input.cooldowns.validateZeroX, types_1.COOLDOWN_MS.validateZeroX)) {
                    return {
                        action: "VALIDATE_ZERO_X" /* AgentAction.VALIDATE_ZERO_X */,
                        reasoning: `Hard error after ${input.softErrorRetries} retries — running 0x health check before new chat`,
                    };
                }
            }
            if (input.softErrorRetries < 3) {
                if (this._canDo(input.cooldowns.sendContinue, types_1.COOLDOWN_MS.sendContinue)) {
                    return {
                        action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
                        reasoning: `Hard error — retry ${input.softErrorRetries + 1}/3`,
                    };
                }
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: "Hard error retry on cooldown",
                };
            }
            // Max retries reached
            if (this._canDo(input.cooldowns.openNewChat, types_1.COOLDOWN_MS.openNewChat)) {
                return {
                    action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                    reasoning: "Hard error persists after 3 retries — opening fresh chat",
                };
            }
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Hard error max retries, new chat on cooldown",
            };
        }
        // ── 7. VS Code not visible ───────────────────────────────────────────────
        if (state === "VSCODE_HIDDEN" /* AgentState.VSCODE_HIDDEN */) {
            return {
                action: "FOCUS_VSCODE" /* AgentAction.FOCUS_VSCODE */,
                reasoning: "VS Code is not in the foreground — bringing to focus",
            };
        }
        // ── 8. Task completed cleanly (LOOP_STOPPED) ─────────────────────────────
        //
        // CRITICAL SEMANTIC DISTINCTION — SEND_CONTINUE ≠ OPEN_NEW_CHAT:
        //   SEND_CONTINUE  → sends "continuar" in the SAME chat.
        //                    Correct ONLY when agent got stuck mid-task (stall/error).
        //   OPEN_NEW_CHAT  → opens a FRESH chat + sends AGENT_LOOP_PROMPT.md.
        //                    Correct when agent FINISHED its task cleanly (signed off).
        //
        // Bug #5: previous handler used SEND_CONTINUE here, which is semantically
        // wrong and expensive — it kept the loop in the same (already finished) chat
        // instead of starting the next task in a clean context.
        //
        // Grace period: 90s lets the user read the final output before the next loop.
        if (state === "COMPLETED" /* AgentState.COMPLETED */) {
            const COMPLETED_GRACE_MS = 300_000; // 5 min grace: time to read the result (v1.3.11: increased from 90s)
            if (input.mssSinceLastActivity >= COMPLETED_GRACE_MS) {
                if (this._canDo(input.cooldowns.openNewChat, types_1.COOLDOWN_MS.openNewChat)) {
                    return {
                        action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                        reasoning: "Task completed cleanly → opening new chat and sending loop prompt to start next task",
                    };
                }
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: "Task completed, grace period done, but openNewChat cooldown active",
                };
            }
            const secsRemaining = Math.ceil((COMPLETED_GRACE_MS - input.mssSinceLastActivity) / 1000);
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: `Task completed — ${secsRemaining}s grace period before opening new chat`,
            };
        }
        // ── 9. Stall detection (for IDLE / STALLED_SOFT / STALLED_HARD) ─────────
        const msSinceActivity = input.mssSinceLastActivity;
        const msg = input.lastMessage;
        const msgCategory = msg?.category ?? "UNKNOWN" /* LastMessageCategory.UNKNOWN */;
        // Content-aware fast-path: if the last message clearly indicates task is done
        // or the model is waiting for a reply, act — but ONLY after the full stall
        // timeout (stallWarnMs, default 5 min) so DOM/log activity has had time to
        // contradict the stall. Prevents premature sends when the model is still working.
        if (msSinceActivity >= input.stallWarnMs && msg && msg.confidence >= 0.7) {
            if (msgCategory === "TASK_COMPLETE" /* LastMessageCategory.TASK_COMPLETE */) {
                if (this._canDo(input.cooldowns.openNewChat, types_1.COOLDOWN_MS.openNewChat)) {
                    return {
                        action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                        reasoning: `📖 Last msg = TASK_COMPLETE (${(msg.confidence * 100).toFixed(0)}%) → open new chat now`,
                    };
                }
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: "Last msg TASK_COMPLETE but openNewChat cooldown active",
                };
            }
            if (msgCategory === "WAITING_FOR_INPUT" /* LastMessageCategory.WAITING_FOR_INPUT */) {
                if (this._canDo(input.cooldowns.sendContinue, types_1.COOLDOWN_MS.sendContinue)) {
                    return {
                        action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
                        reasoning: `📖 Last msg = WAITING_FOR_INPUT (${(msg.confidence * 100).toFixed(0)}%) → sending continue`,
                    };
                }
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: "Last msg WAITING_FOR_INPUT but sendContinue cooldown active",
                };
            }
            if (msgCategory === "ERROR_SWITCH_MODEL" /* LastMessageCategory.ERROR_SWITCH_MODEL */) {
                if (this._canDo(input.cooldowns.switchChatModel, types_1.COOLDOWN_MS.switchChatModel)) {
                    return {
                        action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
                        reasoning: `📖 Last msg = ERROR_SWITCH_MODEL (${(msg.confidence * 100).toFixed(0)}%) → switching chat model`,
                    };
                }
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: `📖 Last msg = ERROR_SWITCH_MODEL but switchChatModel on cooldown`,
                };
            }
            if (msgCategory === "ERROR_VISIBLE" /* LastMessageCategory.ERROR_VISIBLE */) {
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: `📖 Last msg = ERROR_VISIBLE (${(msg.confidence * 100).toFixed(0)}%) — waiting for state to transition to ERROR`,
                };
            }
            // STILL_WORKING or UNKNOWN with high confidence → don't interrupt
            if (msgCategory === "STILL_WORKING" /* LastMessageCategory.STILL_WORKING */) {
                return {
                    action: "WAIT" /* AgentAction.WAIT */,
                    reasoning: `📖 Last msg = STILL_WORKING (${(msg.confidence * 100).toFixed(0)}%) — agent is active, not stalled`,
                };
            }
        }
        // Timer-based stall fallback (used when lastMessage is UNKNOWN or unavailable)
        if (msSinceActivity >= input.stallHardMs) {
            if (this._canDo(input.cooldowns.openNewChat, types_1.COOLDOWN_MS.openNewChat)) {
                const minSinceActivity = Math.round(msSinceActivity / 60_000);
                return {
                    action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                    reasoning: `Hard stall — ${minSinceActivity} min without activity → fresh chat`,
                };
            }
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Hard stall but new_chat on cooldown",
            };
        }
        if (msSinceActivity >= input.stallWarnMs) {
            if (this._canDo(input.cooldowns.sendContinue, types_1.COOLDOWN_MS.sendContinue)) {
                const minSinceActivity = Math.round(msSinceActivity / 60_000);
                return {
                    action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
                    reasoning: `Soft stall — ${minSinceActivity} min without activity → sending continue`,
                };
            }
            return {
                action: "WAIT" /* AgentAction.WAIT */,
                reasoning: "Soft stall but send_continue on cooldown",
            };
        }
        // ── 9. Everything looks fine ─────────────────────────────────────────────
        const secsSinceActivity = Math.round(msSinceActivity / 1000);
        return {
            action: "WAIT" /* AgentAction.WAIT */,
            reasoning: `Normal — last activity ${secsSinceActivity}s ago, no issues detected`,
        };
    }
    _canDo(lastActionTs, cooldownMs) {
        return Date.now() - lastActionTs >= cooldownMs;
    }
}
exports.StateMachine = StateMachine;
