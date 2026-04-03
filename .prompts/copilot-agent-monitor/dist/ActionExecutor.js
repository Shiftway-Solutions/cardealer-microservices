"use strict";
/**
 * ActionExecutor — Executes agent decisions using VS Code APIs.
 *
 * Being INSIDE VS Code gives us reliable, stable APIs:
 *   - vscode.commands.executeCommand() — no focus hacks needed
 *   - code chat CLI — for sending messages to the chat
 *   - vscode.lm model cycling — reads/writes VS Code state directly
 *
 * No AppleScript. No DOM scraping. No CDP.
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
exports.ActionExecutor = void 0;
const vscode = __importStar(require("vscode"));
const child_process_1 = require("child_process");
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const CDPClient_1 = require("./CDPClient");
const SelectorStore_1 = require("./SelectorStore");
const VisualActionAuditor_1 = require("./VisualActionAuditor");
// VS Code internal chat command IDs (stable since VS Code 1.95)
const CMD_CHAT_OPEN = "workbench.action.chat.open";
const CMD_CHAT_NEW = "workbench.action.chat.newChat";
const CMD_CHAT_STOP = "workbench.action.chat.stop";
const CMD_CHAT_FOCUS_INPUT = "workbench.action.chat.focusInput";
const CMD_CHAT_CHANGE_MODEL = "workbench.action.chat.changeModel";
class ActionExecutor {
    _workspaceRoot;
    _modelPool;
    _currentModelIndex = -1;
    _cdp;
    _store;
    _logger;
    _modelManager;
    _auditor;
    _onHealthValidated;
    /**
     * Optional: reads and classifies the last visible chat response via DOM.
     * Wired from ChatDOMWatcher.readLastMessage — used after sending a yes/no
     * question to the chat to check the model's response without OCR.
     */
    _readLastMessage;
    /** AbortController for the currently-running action — resolved early on cancel. */
    _activeAbort = null;
    /** One-shot bypass for the next intentional 1x send after a UI model switch. */
    _premiumSendAuthorization = null;
    constructor(workspaceRoot, modelPool, context, logger) {
        this._workspaceRoot = workspaceRoot;
        this._modelPool = modelPool;
        this._logger = logger;
        const port = vscode.workspace
            .getConfiguration("copilotMonitor")
            .get("cdpPort", 9222);
        this._cdp = new CDPClient_1.CDPClient(port);
        this._store = context ? new SelectorStore_1.SelectorStore(context) : null;
        this._auditor = new VisualActionAuditor_1.VisualActionAuditor();
        this._syncModelIndexFromConfiguredModel();
    }
    updateConfig(workspaceRoot, modelPool) {
        this._workspaceRoot = workspaceRoot;
        this._modelPool = modelPool;
        this._syncModelIndexFromConfiguredModel();
    }
    setModelManager(mm) {
        this._modelManager = mm;
        this._auditor.setModelManager(mm);
    }
    setHealthValidatedCallback(cb) {
        this._onHealthValidated = cb;
    }
    /**
     * Provides DOM-based response reading for the yes/no recovery checks.
     * Call from Monitor after ChatDOMWatcher is started.
     */
    setReadLastMessageCallback(cb) {
        this._readLastMessage = cb;
    }
    /** Cancels any currently-executing action's non-blocking sleeps immediately. */
    cancelCurrentAction() {
        this._activeAbort?.abort();
    }
    /**
     * Selects a specific model by id without cycling through the pool.
     * Uses ModelManager.applyModel() if available; falls back to picker navigation.
     */
    async selectSpecificModel(modelId) {
        const action = "SELECT_SPECIFIC_MODEL" /* AgentAction.SELECT_SPECIFIC_MODEL */;
        if (this._modelManager) {
            const ok = await this._modelManager.applyModel(modelId);
            return {
                ok,
                action,
                detail: ok
                    ? `ModelManager applied '${modelId}'`
                    : `ModelManager could not find '${modelId}' in live models`,
            };
        }
        // Fallback: use picker navigation without new chat
        const ok = await this._selectModelInPicker(modelId, false);
        return {
            ok,
            action,
            detail: ok
                ? `Picker selected '${modelId}'`
                : `Picker navigation failed for '${modelId}'`,
        };
    }
    _log(level, message) {
        this._logger?.(level, `[ActionExecutor] ${message}`);
    }
    _normalizeModelId(modelId) {
        return (modelId || "")
            .trim()
            .toLowerCase()
            .replace(/^copilot\//, "")
            .replace(/^github\.copilot\//, "");
    }
    _normalizeModelText(value) {
        return (value || "")
            .trim()
            .toLowerCase()
            .replace(/^copilot\//, "")
            .replace(/^github\.copilot\//, "")
            .replace(/[._/]+/g, "-")
            .replace(/\s+/g, "-");
    }
    _modelCandidates(modelId) {
        const normalized = this._normalizeModelId(modelId);
        const displayName = modelId.includes("/")
            ? (modelId.split("/").pop() ?? modelId)
            : modelId;
        const normalizedDisplay = this._normalizeModelText(displayName);
        return Array.from(new Set([
            normalized,
            normalizedDisplay,
            normalized.replace(/-/g, " "),
            normalizedDisplay.replace(/-/g, " "),
        ].filter(Boolean)));
    }
    _matchesModel(expectedModelId, actualValue) {
        if (!expectedModelId || !actualValue) {
            return false;
        }
        const haystack = this._normalizeModelText(actualValue);
        return this._modelCandidates(expectedModelId).some((candidate) => {
            const needle = this._normalizeModelText(candidate);
            return (needle.length > 0 && (haystack === needle || haystack.includes(needle)));
        });
    }
    _authorizeNextPremiumSend(modelId, reason, ttlMs = 45_000) {
        this._premiumSendAuthorization = {
            modelId: this._normalizeModelId(modelId),
            reason,
            expiresAt: Date.now() + ttlMs,
        };
        this._log("info", `Premium send armed for '${modelId}' (reason=${reason}, ttlMs=${ttlMs})`);
    }
    _getAuthorizedPremiumSend(modelId) {
        const auth = this._premiumSendAuthorization;
        if (!auth) {
            return null;
        }
        if (Date.now() > auth.expiresAt) {
            this._log("warn", `Premium send authorization expired for '${auth.modelId}' (reason=${auth.reason})`);
            this._premiumSendAuthorization = null;
            return null;
        }
        return this._matchesModel(modelId, auth.modelId) ? auth : null;
    }
    _readConfiguredModel() {
        return (vscode.workspace
            .getConfiguration("github.copilot.chat")
            .get("languageModel", "") ?? "");
    }
    _syncModelIndexFromConfiguredModel() {
        const configuredModel = this._readConfiguredModel();
        const configuredIndex = this._modelPool.findIndex((modelId) => this._matchesModel(modelId, configuredModel));
        this._currentModelIndex = configuredIndex;
        if (configuredIndex >= 0) {
            this._log("debug", `Synced model index from configured model '${configuredModel}' → index ${configuredIndex}`);
        }
        else if (configuredModel) {
            this._log("debug", `Configured model '${configuredModel}' is outside the modelPool`);
        }
        return configuredModel;
    }
    _selectorFor(elementId) {
        const savedSelector = this._store?.snapshot()[elementId]?.selector?.trim() ?? "";
        const defaultSelector = SelectorStore_1.DEFAULT_SELECTORS[elementId] ?? "";
        if (!savedSelector ||
            savedSelector ===
                "div.file-icons-enabled.monaco-enable-motion.monaco-workbench" ||
            savedSelector === "div.rendered-markdown.progress-step > p") {
            return defaultSelector;
        }
        return savedSelector;
    }
    async _readModelIndicatorViaCDP() {
        const selector = this._selectorFor("modelPickerButton");
        if (!selector) {
            return "";
        }
        try {
            if (!this._cdp["_ws"] || this._cdp["_ws"].readyState !== WebSocket.OPEN) {
                await this._cdp.connect();
            }
            return await this._cdp.getText(selector);
        }
        catch (err) {
            this._log("debug", `Model indicator read via CDP failed: ${String(err)}`);
            return "";
        }
    }
    async _verifyModelSelection(expectedModelId, timeoutMs = 4_000) {
        const deadline = Date.now() + timeoutMs;
        let lastConfigured = "";
        let lastIndicator = "";
        while (Date.now() <= deadline) {
            lastConfigured = this._readConfiguredModel();
            if (this._matchesModel(expectedModelId, lastConfigured)) {
                return {
                    ok: true,
                    configured: lastConfigured,
                    indicator: lastIndicator,
                };
            }
            lastIndicator = await this._readModelIndicatorViaCDP();
            if (this._matchesModel(expectedModelId, lastIndicator)) {
                return {
                    ok: true,
                    configured: lastConfigured,
                    indicator: lastIndicator,
                };
            }
            await this._sleep(250);
        }
        return { ok: false, configured: lastConfigured, indicator: lastIndicator };
    }
    async _finalizeModelSwitch(openNewChat) {
        if (!openNewChat) {
            return true;
        }
        // When ModelManager is available: switch to 0x → yes/no validation → if OK STAY on 0x + work prompt
        if (this._modelManager) {
            const result = await this._validateZeroX();
            return result.ok;
        }
        // Fallback (no ModelManager): open chat + send loop prompt directly
        await vscode.commands.executeCommand(CMD_CHAT_NEW);
        await this._sleep(1500);
        const promptSent = await this._sendToChat(this._readLoopPrompt());
        if (!promptSent) {
            this._log("warn", "Model switched, but loop prompt delivery failed after opening a new chat");
        }
        return promptSent;
    }
    async _selectOneXModelViaUi(reason) {
        if (!this._modelManager) {
            return {
                ok: false,
                modelId: "",
                modelName: "",
                detail: "ModelManager not available",
            };
        }
        const oneX = this._modelManager.oneXModels[0];
        if (!oneX) {
            return {
                ok: false,
                modelId: "",
                modelName: "",
                detail: "No 1x models available in live model list",
            };
        }
        const switched = await this._selectModelInPicker(oneX.id, false, {
            uiOnly: true,
        });
        if (!switched) {
            return {
                ok: false,
                modelId: oneX.id,
                modelName: oneX.name,
                detail: `Failed to switch to 1x '${oneX.name}' via chat UI`,
            };
        }
        const synced = this._modelManager.syncAppliedModel(oneX.id, {
            premiumSessionTrigger: reason,
        });
        if (!synced) {
            return {
                ok: false,
                modelId: oneX.id,
                modelName: oneX.name,
                detail: `1x '${oneX.name}' was selected in UI but ModelManager could not sync it`,
            };
        }
        this._authorizeNextPremiumSend(oneX.id, reason);
        this._log("info", `1x active via chat UI: ${oneX.name} (${oneX.id})`);
        return {
            ok: true,
            modelId: oneX.id,
            modelName: oneX.name,
            detail: `1x '${oneX.name}' selected in chat UI`,
        };
    }
    // ─── CDP helper — click a UI element using learned selector ──────────────
    // Tries to connect CDP if not connected, then clicks. Returns false if CDP
    // is unavailable (VS Code not launched with --remote-debugging-port).
    async _clickViaCDP(elementId) {
        const selector = this._selectorFor(elementId);
        if (!selector) {
            return false;
        }
        try {
            if (!this._cdp["_ws"] || this._cdp["_ws"].readyState !== WebSocket.OPEN) {
                await this._cdp.connect();
            }
            return await this._cdp.clickSelector(selector);
        }
        catch {
            return false;
        }
    }
    async execute(action) {
        // Create a fresh abort controller for this action so cancelCurrentAction()
        // can short-circuit any _sleep() calls without affecting future actions.
        this._activeAbort = new AbortController();
        try {
            return await this._executeInner(action);
        }
        finally {
            this._activeAbort = null;
            this._premiumSendAuthorization = null;
        }
    }
    async _executeInner(action) {
        switch (action) {
            case "WAIT" /* AgentAction.WAIT */:
                return { ok: true, action, detail: "No action needed" };
            case "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */:
                return this._sendContinue();
            case "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */:
                return this._openNewChat();
            case "STOP_AND_NEW_CHAT" /* AgentAction.STOP_AND_NEW_CHAT */:
                return this._stopAndNewChat();
            case "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */:
                return this._cycleModel();
            case "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */:
                return this._switchChatModel();
            case "SELECT_SPECIFIC_MODEL" /* AgentAction.SELECT_SPECIFIC_MODEL */:
                return {
                    ok: false,
                    action,
                    detail: "Use selectSpecificModel(modelId) directly",
                };
            case "VALIDATE_ZERO_X" /* AgentAction.VALIDATE_ZERO_X */:
                return this._validateZeroX();
            case "FOCUS_VSCODE" /* AgentAction.FOCUS_VSCODE */:
                return this._focusVSCode();
            default:
                return { ok: false, action, detail: `Unknown action: ${action}` };
        }
    }
    // ─── Send "continuar" to the active chat ──────────────────────────────────
    //
    // Recovery flow:
    //   1. Switch to 0x in UI
    //   2. Ask "¿Terminaste la tarea?" in chat
    //   3. NO  → select 1x from the chat UI → send "continuar" in same chat
    //   4. YES → open new chat → select 1x from the chat UI → send loop/work prompt
    //   5. Ambiguous / no response → stay on 0x, retry next cycle
    async _sendContinue() {
        if (this._modelManager) {
            return this._problemRecovery("same-chat");
        }
        // Fallback: no ModelManager — still MUST validate before sending
        const question = "¿Terminaste la tarea? Responde ÚNICAMENTE con SI o NO.";
        const answer = await this._askYesNoFallback(question);
        if (answer === null) {
            this._log("warn", "[_sendContinue fallback] Ambiguous yes/no answer — retry next cycle");
            return {
                ok: false,
                action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
                detail: "Fallback yes/no check: ambiguous answer — retry next cycle",
            };
        }
        if (answer) {
            // YES — task done: escalate to new chat
            this._log("info", "[_sendContinue fallback] YES → task done, escalating to new chat");
            return this._openNewChat();
        }
        // NO — task not done: send "continuar" in same chat
        const ok = await this._sendToChat("continuar");
        return {
            ok,
            action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
            detail: ok
                ? '[fallback] NO → "continuar" sent'
                : '[fallback] NO → failed to send "continuar"',
        };
    }
    // ─── Open new chat + send loop prompt ────────────────────────────────────
    async _openNewChat() {
        if (this._modelManager) {
            return this._problemRecovery("new-chat");
        }
        // Fallback: no ModelManager — still MUST validate before opening new chat
        const question = "¿Terminaste la tarea? Responde ÚNICAMENTE con SI o NO.";
        const answer = await this._askYesNoFallback(question);
        if (answer === null) {
            this._log("warn", "[_openNewChat fallback] Ambiguous yes/no answer — retry next cycle");
            return {
                ok: false,
                action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                detail: "Fallback yes/no check: ambiguous answer — retry next cycle",
            };
        }
        if (!answer) {
            // NO — task not done: do not open new chat yet
            this._log("info", "[_openNewChat fallback] NO → task not done, skipping new chat");
            return {
                ok: false,
                action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                detail: "[fallback] NO → task not done — not opening new chat",
            };
        }
        // YES — task done: open new chat + send loop prompt
        const { result, audit } = await this._auditor.auditedExec("OPEN_NEW_CHAT", async () => {
            const openedViaCDP = await this._clickViaCDP("newChatButton");
            if (!openedViaCDP) {
                await vscode.commands.executeCommand(CMD_CHAT_NEW);
            }
            await this._sleep(1500);
            const prompt = this._readLoopPrompt();
            const sent = await this._sendToChat(prompt);
            return {
                ok: sent,
                action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                detail: sent
                    ? `[fallback] YES → new chat opened (CDP:${openedViaCDP}) and loop prompt sent`
                    : "[fallback] YES → new chat opened but prompt send failed",
            };
        }, {
            actionWaitMs: 0, // already waited inside executor
            useVisionFallback: true,
        });
        return { ...result, audit };
    }
    // ─── Stop current generation + new chat ──────────────────────────────────
    async _stopAndNewChat() {
        const { result, audit } = await this._auditor.auditedExec("STOP_AND_NEW_CHAT", async () => {
            try {
                const stoppedViaCDP = await this._clickViaCDP("stopButton");
                if (!stoppedViaCDP) {
                    await vscode.commands.executeCommand(CMD_CHAT_STOP);
                }
                await this._sleep(1500);
                return this._openNewChat();
            }
            catch (err) {
                return {
                    ok: false,
                    action: "STOP_AND_NEW_CHAT" /* AgentAction.STOP_AND_NEW_CHAT */,
                    detail: `Error: ${err}`,
                };
            }
        }, { actionWaitMs: 0 });
        return { ...result, audit };
    }
    // ─── Switch model IN current chat session (no new chat) ──────────────────
    //
    // The VS Code model picker has TWO levels:
    //   Level 1 — Recently used / pinned models (top of the QuickPick)
    //   Level 2 — "Other Models" submenu (all remaining models)
    //
    // The naive approach (type name → Enter) fails when the model is in level 2
    // because the first Enter enters the submenu instead of selecting the model.
    //
    // Strategy (3-tier, each more forceful than the last):
    //   Tier 0 — Try undocumented command argument: changeModel({ modelId })
    //             Works in VS Code ≥1.99 with Copilot Chat; zero UI interaction.
    //   Tier 1 — Open picker → type name → Enter.
    //             Wait 400ms and check if picker is still open (entered submenu).
    //             If submenu opened: Enter again to pick from submenu.
    //   Tier 2 — CDP: click element with matching text via learned selector.
    async _switchChatModel() {
        // POLICY: Brain cycle MUST ONLY use 0x (free) models.
        // Query live 0x models from ModelManager — never cycle to a 1x model.
        // Bug fix: previous version cycled through _modelPool raw strings which could
        // include 1x models if misconfigured, setting lastAppliedModel to 1x and
        // triggering the PREMIUM GUARD on the next _sendToChat call.
        if (this._modelManager) {
            const zeroXModels = this._modelManager.zeroXModels;
            if (zeroXModels.length === 0) {
                return {
                    ok: false,
                    action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
                    detail: "No 0x models available to switch to — cannot satisfy rate limit without 1x model",
                };
            }
            // Round-robin among 0x live models only
            const lastApplied = this._modelManager.lastAppliedModel;
            const currentIdx = lastApplied
                ? zeroXModels.findIndex((m) => m.id === lastApplied.id)
                : -1;
            const nextIdx = (currentIdx + 1) % zeroXModels.length;
            const nextModel = zeroXModels[nextIdx];
            this._log("info", `Switching chat model to 0x model '${nextModel.name}' (${nextModel.id}) — POLICY: brain never uses 1x`);
            const ok = await this._modelManager.applyModel(nextModel.id);
            return {
                ok,
                action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
                detail: ok
                    ? `Switched to 0x model: ${nextModel.name} (${nextModel.id})`
                    : `Failed to apply 0x model: ${nextModel.name}`,
            };
        }
        // Fallback: no ModelManager — fall back to original pool-based rotation
        if (this._modelPool.length === 0) {
            return {
                ok: false,
                action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
                detail: "Model pool is empty",
            };
        }
        const configuredModel = this._syncModelIndexFromConfiguredModel();
        const currentIndex = this._currentModelIndex;
        const nextIndex = (currentIndex + 1 + this._modelPool.length) % this._modelPool.length;
        const nextModel = this._modelPool[nextIndex];
        this._log("info", `[FALLBACK] Switching chat model from '${configuredModel || "unknown"}' to '${nextModel}' (no ModelManager — 0x filter bypassed)`);
        const ok = await this._selectModelInPicker(nextModel, 
        /* openNewChat */ false);
        if (ok) {
            this._currentModelIndex = nextIndex;
        }
        return {
            ok,
            action: "SWITCH_CHAT_MODEL" /* AgentAction.SWITCH_CHAT_MODEL */,
            detail: ok
                ? `Switched to ${nextModel} in current chat session`
                : `Failed to switch to ${nextModel} — picker navigation failed`,
        };
    }
    // ─── Cycle to next model in pool (writes to VS Code state DB) ────────────
    async _cycleModel() {
        // POLICY: Brain cycle MUST ONLY use 0x (free) models.
        // Bug fix: previous version cycled through _modelPool raw strings which could
        // include 1x models if misconfigured. Although _finalizeModelSwitch(true) calls
        // _validateZeroX which self-corrects, we enforce 0x-only selection at the source.
        if (this._modelManager) {
            const zeroXModels = this._modelManager.zeroXModels;
            if (zeroXModels.length === 0) {
                return {
                    ok: false,
                    action: "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */,
                    detail: "No 0x models available to cycle to — monitor cannot recover from rate limit",
                };
            }
            // Round-robin among 0x live models only
            const lastApplied = this._modelManager.lastAppliedModel;
            const currentIdx = lastApplied
                ? zeroXModels.findIndex((m) => m.id === lastApplied.id)
                : -1;
            const nextIdx = (currentIdx + 1) % zeroXModels.length;
            const nextModel = zeroXModels[nextIdx];
            this._log("info", `Cycling to 0x model '${nextModel.name}' (${nextModel.id}) and opening new chat — POLICY: brain never uses 1x`);
            const ok = await this._modelManager.applyModel(nextModel.id);
            if (ok) {
                const finalizeOk = await this._finalizeModelSwitch(true);
                return {
                    ok: finalizeOk,
                    action: "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */,
                    detail: finalizeOk
                        ? `Rotated to 0x model: ${nextModel.name} + new chat opened`
                        : `Applied 0x model: ${nextModel.name} but new chat/prompt failed`,
                };
            }
            return {
                ok: false,
                action: "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */,
                detail: `Failed to apply 0x model: ${nextModel.name}`,
            };
        }
        // Fallback: no ModelManager
        if (this._modelPool.length === 0) {
            return {
                ok: false,
                action: "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */,
                detail: "Model pool is empty",
            };
        }
        const configuredModel = this._syncModelIndexFromConfiguredModel();
        this._currentModelIndex =
            (this._currentModelIndex + 1 + this._modelPool.length) %
                this._modelPool.length;
        const nextModel = this._modelPool[this._currentModelIndex];
        this._log("info", `[FALLBACK] Cycling model from '${configuredModel || "unknown"}' to '${nextModel}' (no ModelManager)`);
        const ok = await this._selectModelInPicker(nextModel, 
        /* openNewChat */ true);
        return {
            ok,
            action: "CYCLE_MODEL" /* AgentAction.CYCLE_MODEL */,
            detail: ok
                ? `Rotated to model: ${nextModel}`
                : `Cycle model error: picker navigation failed for ${nextModel}`,
        };
    }
    /**
     * Selects a model in the VS Code chat model picker.
     * Handles both the single-level picker (recently used) and the two-level
     * picker where models not in the recent list live under "Other Models".
     *
     * @param modelId  Full model identifier from modelPool (e.g. "gpt-4.1" or "openai/gpt-4.1")
     * @param openNewChat  If true, opens a new chat + sends loop prompt after switching.
     */
    async _selectModelInPicker(modelId, openNewChat, options) {
        const uiOnly = options?.uiOnly === true;
        const displayName = modelId.includes("/")
            ? modelId.split("/").pop()
            : modelId;
        const commandInput = { modelId };
        this._log("debug", `Selecting model '${modelId}' (display='${displayName}', openNewChat=${openNewChat}, uiOnly=${uiOnly})`);
        // ── Tier 0a: ModelManager — uses { vendor, id, family } payload (most reliable) ──
        // NOTE: changeModel({ vendor, id, family }) is the authoritative command the UI uses.
        // It updates the session model in memory but does NOT write to
        // github.copilot.chat.languageModel in settings.json. When CDP is unavailable,
        // _verifyModelSelection will always time out. Trust applyModel() success in that case.
        if (!uiOnly && this._modelManager) {
            try {
                const ok = await this._modelManager.applyModel(modelId);
                if (ok) {
                    const verification = await this._verifyModelSelection(modelId, 3_000);
                    if (verification.ok) {
                        this._log("info", `Tier 0a ModelManager switch verified for '${modelId}' (configured='${verification.configured}')`);
                        return this._finalizeModelSwitch(openNewChat);
                    }
                    // applyModel succeeded (command didn't throw) but verification can't confirm
                    // because settings are stale and CDP is unavailable. Trust the command.
                    this._log("info", `Tier 0a ModelManager applied '${modelId}' — trusted (settings stale: '${verification.configured}', cdp: '${verification.indicator || "unavailable"}')`);
                    return this._finalizeModelSwitch(openNewChat);
                }
            }
            catch (err) {
                this._log("debug", `Tier 0a ModelManager failed: ${String(err)}`);
            }
        }
        // ── Tier 0b: Undocumented command argument { modelId } (VS Code ≥1.99) ──
        if (!uiOnly) {
            try {
                await vscode.commands.executeCommand(CMD_CHAT_CHANGE_MODEL, commandInput);
                const verification = await this._verifyModelSelection(modelId);
                if (verification.ok) {
                    this._log("info", `Tier 0b model switch verified for '${modelId}' (configured='${verification.configured}', indicator='${verification.indicator}')`);
                    return this._finalizeModelSwitch(openNewChat);
                }
                this._log("warn", `Tier 0b accepted '${modelId}' but verification failed (configured='${verification.configured}', indicator='${verification.indicator}')`);
            }
            catch (err) {
                this._log("debug", `Tier 0b changeModel({ modelId }) failed: ${String(err)}`);
            }
        }
        // ── Tier 1: Open picker → type name → Navigate two-level structure ──────
        try {
            await vscode.commands.executeCommand(CMD_CHAT_CHANGE_MODEL);
            await this._sleep(600);
            await vscode.commands.executeCommand("workbench.action.type", {
                text: displayName,
            });
            await this._sleep(500);
            await vscode.commands.executeCommand("workbench.action.acceptSelectedQuickOpenItem");
            await this._sleep(500);
            await vscode.commands.executeCommand("workbench.action.type", {
                text: displayName,
            });
            await this._sleep(400);
            await vscode.commands.executeCommand("workbench.action.acceptSelectedQuickOpenItem");
            await this._sleep(600);
            const verification = await this._verifyModelSelection(modelId);
            if (verification.ok) {
                this._log("info", `Tier 1 QuickPick switch verified for '${modelId}' (configured='${verification.configured}', indicator='${verification.indicator}')`);
                return this._finalizeModelSwitch(openNewChat);
            }
            if (uiOnly && !verification.indicator) {
                this._log("info", `Tier 1 QuickPick switch trusted for '${modelId}' (UI-only path, CDP unavailable, configured='${verification.configured}')`);
                return this._finalizeModelSwitch(openNewChat);
            }
            this._log("warn", `Tier 1 QuickPick flow completed but verification failed for '${modelId}' (configured='${verification.configured}', indicator='${verification.indicator}')`);
        }
        catch (err) {
            this._log("debug", `Tier 1 QuickPick switch failed: ${String(err)}`);
        }
        // ── Tier 2: CDP — open picker, then click target model by visible text ──
        try {
            if (!(await this._cdp.isAvailable())) {
                this._log("warn", "Tier 2 CDP switch unavailable — VS Code is not exposing a remote debugging port");
                return false;
            }
            const pickerClicked = await this._clickViaCDP("modelPickerButton");
            if (!pickerClicked) {
                this._log("warn", "Tier 2 CDP could not click the model picker button");
                return false;
            }
            await this._sleep(500);
            const modelClickedDirectly = await this._cdp.clickByText([
                displayName,
                displayName.replace(/[-_]/g, " "),
            ]);
            let modelClicked = modelClickedDirectly;
            if (!modelClicked) {
                const submenuOpened = await this._cdp.clickByText([
                    "Other Models",
                    "More Models",
                    "Browse Models",
                ]);
                if (submenuOpened) {
                    await this._sleep(400);
                    modelClicked = await this._cdp.clickByText([
                        displayName,
                        displayName.replace(/[-_]/g, " "),
                    ]);
                }
            }
            if (!modelClicked) {
                this._log("warn", `Tier 2 CDP did not find a clickable entry for '${modelId}'`);
                return false;
            }
            await this._sleep(600);
            const verification = await this._verifyModelSelection(modelId, 3_000);
            if (verification.ok) {
                this._log("info", `Tier 2 CDP switch verified for '${modelId}' (configured='${verification.configured}', indicator='${verification.indicator}')`);
                return this._finalizeModelSwitch(openNewChat);
            }
            this._log("warn", `Tier 2 CDP clicked '${modelId}' but verification failed (configured='${verification.configured}', indicator='${verification.indicator}')`);
        }
        catch (err) {
            this._log("debug", `Tier 2 CDP switch failed: ${String(err)}`);
        }
        return false;
    }
    // ─── Problem Recovery ──────────────────────────────────────────────────────
    //
    // Triggered when any problem is detected (error, stall, etc.).
    //
    //  1. Switch to 0x in UI
    //  2. Send "¿Terminaste la tarea? SI o NO" in the SAME chat
    //  3. Wait ~10s for model response; read via DOM callback or LM API fallback
    //  4. NO (task not done)  → select 1x in the chat UI → send "continuar"
    //  5. YES (task done)     → open NEW chat → select 1x in the chat UI → send work prompt
    //  6. No response / error → stay on 0x, return failure (retry next cycle)
    //
    //  Before ANY continue/work prompt is sent, the monitor must select 1x via
    //  the chat UI, sync that selection locally, and authorize exactly one 1x send.
    async _problemRecovery(mode) {
        const action = mode === "same-chat"
            ? "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */
            : "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */;
        this._log("info", `🩺 Problem recovery — asking task-completion question...`);
        if (!this._modelManager) {
            return { ok: false, action, detail: "ModelManager not available" };
        }
        // ── Step 1: Switch to 0x in UI ──────────────────────────────────────────
        const zeroXModel = await this._modelManager.applyBestZeroX();
        if (!zeroXModel) {
            this._log("error", "Problem recovery: no 0x model available — monitor will stop");
            return { ok: false, action, detail: "NO_ZERO_X_MODEL_AVAILABLE" };
        }
        this._log("info", `0x active: ${zeroXModel.name} (${zeroXModel.id})`);
        await this._sleep(600);
        // ── Step 2: Send yes/no question in SAME chat ───────────────────────────
        const question = "¿Terminaste la tarea? Responde ÚNICAMENTE con SI o NO.";
        const questionSent = await this._sendToChat(question);
        if (!questionSent) {
            this._log("warn", "Problem recovery: failed to send yes/no question");
            return {
                ok: false,
                action,
                detail: "Failed to send recovery question — retry next cycle",
            };
        }
        // ── Step 3: Wait for model to respond ───────────────────────────────────
        await this._sleep(10_000);
        // ── Step 4: Read response ────────────────────────────────────────────────
        let answeredYes = null; // null = no clear answer
        if (this._readLastMessage) {
            try {
                const reading = await this._readLastMessage();
                const lower = reading.text.toLowerCase();
                const hasYes = /\bsi\b|\bsí\b|\byes\b/.test(lower);
                const hasNo = /\bno\b/.test(lower);
                if (hasYes && !hasNo) {
                    answeredYes = true;
                }
                else if (hasNo && !hasYes) {
                    answeredYes = false;
                }
                // Both or neither → null (ambiguous)
                this._log("info", `DOM response: "${reading.text.slice(0, 100)}" → ${answeredYes === true ? "YES (task done)" : answeredYes === false ? "NO (task not done)" : "AMBIGUOUS"}`);
            }
            catch {
                answeredYes = null;
            }
        }
        else {
            const lmYes = await this._askYesNoViaLM(zeroXModel, question);
            answeredYes = lmYes ? true : false;
        }
        this._onHealthValidated?.(answeredYes === true);
        // ── Step 5: No clear answer → stay on 0x, retry next cycle ──────────────
        if (answeredYes === null) {
            this._log("warn", "❓ Ambiguous response — staying on 0x, retry next cycle");
            return {
                ok: false,
                action,
                detail: "Recovery: ambiguous answer — retry next cycle",
            };
        }
        if (!answeredYes) {
            this._log("info", "❌ NO → task not done — selecting 1x in chat UI before 'continuar'");
            const oneXSwitch = await this._selectOneXModelViaUi("problem-recovery-same-chat");
            if (!oneXSwitch.ok) {
                this._log("warn", oneXSwitch.detail);
                return {
                    ok: false,
                    action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
                    detail: oneXSwitch.detail,
                };
            }
            // v1.3.11: Validate the chat UI actually shows the 1x model before sending
            // the work prompt — prevents sending on wrong model when picker switch fails.
            await this._validateOneXInChatUI(oneXSwitch.modelId, "problem-recovery-same-chat");
            const sent = await this._sendToChat("continuar");
            return {
                ok: sent,
                action: "SEND_CONTINUE" /* AgentAction.SEND_CONTINUE */,
                detail: `Recovery NO [1x UI: ${oneXSwitch.modelName}] — "continuar" sent ${sent ? "✅" : "❌"}`,
            };
        }
        this._log("info", "✅ YES → task done — opening new chat");
        const openedViaCDP = await this._clickViaCDP("newChatButton");
        if (!openedViaCDP) {
            await vscode.commands.executeCommand(CMD_CHAT_NEW);
        }
        await this._sleep(1500);
        const oneXSwitch = await this._selectOneXModelViaUi("problem-recovery-new-chat");
        if (!oneXSwitch.ok) {
            this._log("warn", oneXSwitch.detail);
            return {
                ok: false,
                action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
                detail: oneXSwitch.detail,
            };
        }
        // v1.3.11: Validate the chat UI actually shows the 1x model before sending
        // the work prompt — prevents sending on wrong model when picker switch fails.
        await this._validateOneXInChatUI(oneXSwitch.modelId, "problem-recovery-new-chat");
        const promptSent = await this._sendToChat(this._readLoopPrompt());
        return {
            ok: promptSent,
            action: "OPEN_NEW_CHAT" /* AgentAction.OPEN_NEW_CHAT */,
            detail: `Recovery YES [1x UI: ${oneXSwitch.modelName}] — new chat + loop prompt ${promptSent ? "✅" : "❌"}`,
        };
    }
    /**
     * Asks a yes/no question to the 0x model via the LM API directly.
     * Fallback when ChatDOMWatcher (DOM callback) is not available.
     */
    async _askYesNoViaLM(zeroXModel, question) {
        try {
            const [lmModel] = await vscode.lm.selectChatModels({
                vendor: "copilot",
                family: zeroXModel.family,
            });
            if (!lmModel) {
                this._log("warn", `_askYesNoViaLM: model '${zeroXModel.family}' not found in LM API`);
                return false;
            }
            const cts = new vscode.CancellationTokenSource();
            const timeout = setTimeout(() => cts.cancel(), 15_000);
            try {
                const response = await lmModel.sendRequest([vscode.LanguageModelChatMessage.User(question)], { justification: "Copilot Agent Monitor — problem recovery check" }, cts.token);
                let responseText = "";
                for await (const chunk of response.text) {
                    responseText += chunk;
                    if (responseText.length > 200) {
                        break;
                    }
                }
                clearTimeout(timeout);
                const lower = responseText.toLowerCase();
                const hasYes = /\bsi\b|\bsí\b|\byes\b/.test(lower);
                this._log("info", `LM fallback: "${responseText.trim().slice(0, 80)}" → ${hasYes ? "YES ✅" : "NO ❌"}`);
                return hasYes;
            }
            catch (err) {
                clearTimeout(timeout);
                this._log("warn", `LM request failed: ${String(err)}`);
                return false;
            }
        }
        catch (err) {
            this._log("warn", `LM model selection failed: ${String(err)}`);
            return false;
        }
    }
    /**
     * Minimal yes/no validation without ModelManager.
     * Sends the question to the active chat and reads the response via:
     *   1. DOM callback (_readLastMessage) — zero-cost, preferred
     *   2. VS Code LM API (any available copilot model) — fallback
     * Returns: true = YES, false = NO, null = ambiguous / failed
     */
    async _askYesNoFallback(question) {
        const sent = await this._sendToChat(question);
        if (!sent) {
            this._log("warn", "[_askYesNoFallback] Failed to send yes/no question");
            return null;
        }
        await this._sleep(10_000);
        // Tier 1: DOM callback (ChatDOMWatcher — no extra API call)
        if (this._readLastMessage) {
            try {
                const reading = await this._readLastMessage();
                const lower = reading.text.toLowerCase();
                const hasYes = /\bsi\b|\bsí\b|\byes\b/.test(lower);
                const hasNo = /\bno\b/.test(lower);
                this._log("info", `[_askYesNoFallback] DOM: "${reading.text.slice(0, 100)}" → ${hasYes && !hasNo ? "YES" : !hasYes && hasNo ? "NO" : "AMBIGUOUS"}`);
                if (hasYes && !hasNo)
                    return true;
                if (hasNo && !hasYes)
                    return false;
                return null;
            }
            catch {
                // fall through to LM tier
            }
        }
        // Tier 2: VS Code LM API — any available copilot model (no ModelManager needed)
        try {
            const [lmModel] = await vscode.lm.selectChatModels({ vendor: "copilot" });
            if (!lmModel) {
                this._log("warn", "[_askYesNoFallback] No copilot model available via LM API");
                return null;
            }
            const cts = new vscode.CancellationTokenSource();
            const timeout = setTimeout(() => cts.cancel(), 15_000);
            try {
                const response = await lmModel.sendRequest([vscode.LanguageModelChatMessage.User(question)], {
                    justification: "Copilot Agent Monitor — no-ModelManager recovery check",
                }, cts.token);
                let responseText = "";
                for await (const chunk of response.text) {
                    responseText += chunk;
                    if (responseText.length > 200)
                        break;
                }
                clearTimeout(timeout);
                const lower = responseText.toLowerCase();
                const hasYes = /\bsi\b|\bsí\b|\byes\b/.test(lower);
                const hasNo = /\bno\b/.test(lower);
                this._log("info", `[_askYesNoFallback] LM: "${responseText.trim().slice(0, 80)}" → ${hasYes && !hasNo ? "YES ✅" : !hasYes && hasNo ? "NO ❌" : "AMBIGUOUS"}`);
                if (hasYes && !hasNo)
                    return true;
                if (hasNo && !hasYes)
                    return false;
                return null;
            }
            catch (err) {
                clearTimeout(timeout);
                this._log("warn", `[_askYesNoFallback] LM request failed: ${String(err)}`);
                return null;
            }
        }
        catch (err) {
            this._log("warn", `[_askYesNoFallback] LM model selection failed: ${String(err)}`);
            return null;
        }
    }
    // ─── Legacy: VALIDATE_ZERO_X action (called directly from StateMachine) ───
    // Delegates to _problemRecovery("new-chat") to keep action enum compatibility.
    async _validateZeroX() {
        const result = await this._problemRecovery("new-chat");
        return { ...result, action: "VALIDATE_ZERO_X" /* AgentAction.VALIDATE_ZERO_X */ };
    }
    // ─── Focus VS Code window ─────────────────────────────────────────────────
    async _focusVSCode() {
        try {
            // Tier 1: CDP click directly on the chat input
            const focusedViaCDP = await this._clickViaCDP("chatInput");
            if (!focusedViaCDP) {
                // Tier 2: VS Code commands fallback
                await vscode.commands.executeCommand(CMD_CHAT_OPEN);
                await this._sleep(300);
                await vscode.commands.executeCommand(CMD_CHAT_FOCUS_INPUT);
            }
            return {
                ok: true,
                action: "FOCUS_VSCODE" /* AgentAction.FOCUS_VSCODE */,
                detail: `Chat panel focused (CDP:${focusedViaCDP})`,
            };
        }
        catch (err) {
            return {
                ok: false,
                action: "FOCUS_VSCODE" /* AgentAction.FOCUS_VSCODE */,
                detail: `Focus error: ${err}`,
            };
        }
    }
    // ─── Send message to chat ─────────────────────────────────────────────────
    //
    // Tier 1: `code chat --mode agent --reuse-window "message"`
    //         Reliable CLI. Works without focus. No keyboard simulation.
    // Tier 2: Focus chat → clipboard paste → Enter
    //         Fallback if CLI not available.
    async _sendToChat(message) {
        // PREMIUM GUARD: Block the send entirely if a 1x (non-free) model is active.
        // The brain must NEVER make requests on a 1x model — this is a hard policy.
        //
        // BUG FIX: Use ModelManager.lastAppliedModel (authoritative) instead of
        // github.copilot.chat.languageModel from settings.json. The changeModel()
        // command updates the session model in memory but does NOT write to settings.
        // Reading settings here caused ALL prompts to be blocked when the user's
        // settings still referenced a 1x model like claude-opus-4-6.
        let authorizedPremiumSend = null;
        let premiumModelId = "";
        if (this._modelManager) {
            const lastApplied = this._modelManager.lastAppliedModel;
            if (lastApplied) {
                // Use the authoritative last-applied model from ModelManager
                if (!lastApplied.isFree) {
                    const auth = this._getAuthorizedPremiumSend(lastApplied.id);
                    if (auth) {
                        authorizedPremiumSend = auth;
                        premiumModelId = lastApplied.id;
                        this._log("info", `Premium guard: allowing intentional 1x send on '${lastApplied.id}' (reason=${auth.reason})`);
                    }
                    else {
                        this._log("error", `🚨 PREMIUM GUARD: attempted send on 1x model '${lastApplied.id}' — request BLOCKED. ` +
                            "This is a monitor bug — the brain must only use 0x models.");
                        this._modelManager.recordPremiumPrompt(lastApplied.id, "BLOCKED_by_premium_guard");
                        vscode.window.showErrorMessage(`🚨 Copilot Monitor: PREMIUM GUARD — request bloqueado en modelo 1x '${lastApplied.id}'. ` +
                            "Revisa el audit log (.github/copilot-monitor-audit.jsonl).");
                        return false;
                    }
                }
                // lastApplied is 0x — safe to proceed
            }
            // If lastAppliedModel is null (never applied by monitor), allow the send.
            // The monitor hasn't switched models yet, so we can't determine tier.
            // The first action should be VALIDATE_ZERO_X which will apply a 0x model.
        }
        const finalizePremiumSend = () => {
            if (!authorizedPremiumSend || !this._modelManager || !premiumModelId) {
                return;
            }
            this._modelManager.recordPremiumPrompt(premiumModelId, authorizedPremiumSend.reason);
            this._premiumSendAuthorization = null;
            this._log("info", `Premium guard authorization consumed for '${premiumModelId}' (reason=${authorizedPremiumSend.reason})`);
        };
        // BUG FIX: Check abort signal BEFORE the actual send.
        // cancelCurrentAction() only resolves _sleep() timers early — it does NOT
        // prevent VS Code API calls. Without this check, a cancelled action
        // (triggered by USER_TYPING or isGenerating DOM event) would still send
        // the message to the chat.
        if (this._activeAbort?.signal.aborted) {
            this._log("warn", "🛑 _sendToChat: aborted before send — message NOT sent");
            return false;
        }
        if (await this._sendViaCLI(message)) {
            finalizePremiumSend();
            return true;
        }
        // Check abort again — CLI may have taken time; clipboard is still a send.
        if (this._activeAbort?.signal.aborted) {
            this._log("warn", "🛑 _sendToChat: aborted before clipboard fallback — message NOT sent");
            return false;
        }
        this._log("warn", "Falling back to clipboard-based chat delivery because code chat CLI did not succeed");
        const sent = await this._sendViaClipboard(message);
        if (sent) {
            finalizePremiumSend();
        }
        return sent;
    }
    _sendViaCLI(message) {
        return new Promise((resolve) => {
            const args = ["chat", "--mode", "agent", "--reuse-window", message];
            this._log("debug", `Executing CLI: code ${args.slice(0, 4).join(" ")} <message:${message.length} chars>`);
            (0, child_process_1.execFile)("code", args, { cwd: this._workspaceRoot, timeout: 20_000 }, (err, stdout, stderr) => {
                const stdOut = stdout?.trim();
                const stdErr = stderr?.trim();
                if (stdOut) {
                    this._log("debug", `code chat stdout: ${stdOut.slice(0, 400)}`);
                }
                if (stdErr) {
                    this._log("warn", `code chat stderr: ${stdErr.slice(0, 400)}`);
                }
                if (err) {
                    this._log("warn", `code chat failed: ${err.message}`);
                    resolve(false);
                    return;
                }
                this._log("info", "code chat CLI delivered the message successfully");
                resolve(true);
            });
        });
    }
    async _sendViaClipboard(message) {
        // AUDIT: Clipboard fallback is a visible side effect — notify user before executing.
        // Do NOT submit silently; the clipboard will be overwritten.
        vscode.window.showWarningMessage(`[Copilot Monitor] ⚠️ code chat CLI unavailable — using clipboard fallback to deliver message (${message.length} chars). Your clipboard will be overwritten.`);
        this._log("warn", `Clipboard fallback activated — message length: ${message.length} chars. Clipboard will be overwritten.`);
        try {
            await vscode.commands.executeCommand(CMD_CHAT_FOCUS_INPUT);
            await this._sleep(400);
            await vscode.env.clipboard.writeText(message);
            // Select all then paste (clears any existing draft)
            await vscode.commands.executeCommand("editor.action.selectAll");
            await vscode.commands.executeCommand("editor.action.clipboardPasteAction");
            await this._sleep(300);
            await vscode.commands.executeCommand("workbench.action.chat.submit");
            this._log("info", "Clipboard fallback delivered the message to the active chat input");
            return true;
        }
        catch (err) {
            this._log("warn", `Clipboard fallback failed: ${String(err)}`);
            return false;
        }
    }
    // ─── Read loop prompt file ────────────────────────────────────────────────
    _readLoopPrompt() {
        try {
            const config = vscode.workspace.getConfiguration("copilotMonitor");
            const relPath = config.get("loopPromptFile", ".prompts/AGENT_LOOP_PROMPT.md");
            const fullPath = path.join(this._workspaceRoot, relPath);
            if (fs.existsSync(fullPath)) {
                return fs.readFileSync(fullPath, "utf-8").trim();
            }
        }
        catch {
            /* fallback below */
        }
        return "Continúa con las tareas pendientes del sprint actual en modo agente.";
    }
    /**
     * Non-blocking sleep that resolves early when `cancelCurrentAction()` is called.
     * If the active AbortController is already aborted, resolves immediately.
     */
    /**
     * v1.3.11 — Validates that the chat UI model indicator (read via CDP) is a
     * 1x model before sending a work prompt. If it shows a 0x (free) model or a
     * model that doesn't match the expected 1x selection, logs a warning and
     * re-attempts the model switch.
     *
     * This closes the gap where _selectOneXModelViaUi() trusts the QuickPick
     * switch without verifying what the chat UI actually displays.
     *
     * @param expectedModelId  The 1x model we just tried to select.
     * @param context          Log label (e.g. "problem-recovery-same-chat").
     */
    async _validateOneXInChatUI(expectedModelId, context) {
        const indicator = await this._readModelIndicatorViaCDP();
        if (!indicator) {
            this._log("warn", `[MODEL VALIDATE] ${context}: CDP unavailable — cannot verify chat UI model before sending work prompt`);
            return;
        }
        // Determine if the indicator matches a 0x (free) model.
        const isZeroX = this._modelManager
            ? this._modelManager.zeroXModels.some((m) => this._matchesModel(m.id, indicator))
            : // Fallback heuristic: check known 0x family names when ModelManager unavailable
                /gpt-4\.1|gpt-5-mini|gpt-4o/i.test(indicator);
        const matchesExpected = this._matchesModel(expectedModelId, indicator);
        if (!matchesExpected || isZeroX) {
            this._log("warn", `[MODEL VALIDATE] ${context}: Chat UI shows '${indicator}' — expected 1x '${expectedModelId}' (isZeroX=${isZeroX}). Re-attempting 1x switch.`);
            // Re-attempt once; failure is logged but does NOT block the send
            // (the premium guard in _sendToChat provides the hard gate).
            const retry = await this._selectOneXModelViaUi(context + "-validate-retry");
            this._log(retry.ok ? "info" : "warn", `[MODEL VALIDATE] ${context}: Retry result: ${retry.detail}`);
        }
        else {
            this._log("info", `[MODEL VALIDATE] ${context}: ✅ Chat UI shows '${indicator}' — 1x confirmed before work prompt`);
        }
    }
    _sleep(ms) {
        const signal = this._activeAbort?.signal;
        if (!signal) {
            return new Promise((resolve) => setTimeout(resolve, ms));
        }
        if (signal.aborted) {
            return Promise.resolve();
        }
        return new Promise((resolve) => {
            const timer = setTimeout(resolve, ms);
            signal.addEventListener("abort", () => {
                clearTimeout(timer);
                resolve();
            }, { once: true });
        });
    }
}
exports.ActionExecutor = ActionExecutor;
