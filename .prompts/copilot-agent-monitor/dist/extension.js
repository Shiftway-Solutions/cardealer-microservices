"use strict";
/**
 * extension.ts — VS Code Extension Entry Point
 *
 * Copilot Agent Monitor — keeps GitHub Copilot Agent always coding.
 *
 * Architecture:
 *   LogWatcher     → FREE real-time events from Copilot log file
 *   StateMachine   → pure deterministic transitions, no LLM
 *   ScreenAnalyzer → screenshot + GPT-4o vision (0x model, used sparingly)
 *   ActionExecutor → VS Code commands + `code chat` CLI
 *   StatusBar      → developer always sees what the agent is doing
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
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const Monitor_1 = require("./Monitor");
const SelectorWizard_1 = require("./SelectorWizard");
const SelectorStore_1 = require("./SelectorStore");
const ModelManager_1 = require("./ModelManager");
let monitor;
let selectorWizard;
let selectorStore;
let modelManager;
function activate(context) {
    monitor = new Monitor_1.Monitor(context);
    selectorStore = new SelectorStore_1.SelectorStore(context);
    selectorWizard = new SelectorWizard_1.SelectorWizard(context);
    // ── ModelManager: fetch live models + register per-model commands ──────────────
    modelManager = new ModelManager_1.ModelManager((level, msg) => console[level](`[ModelManager] ${msg}`));
    context.subscriptions.push({ dispose: () => modelManager?.dispose() });
    // BUG FIX: Wire ModelManager into Monitor IMMEDIATELY, before refresh() completes.
    // refresh() is async (probes each model ~1-2s each), and the monitor's first cycle
    // fires in 5s. Without this, the first cycle sees no ModelManager and cannot run
    // VALIDATE_ZERO_X or any 0x/1x model flows.
    // ModelManager.applyBestZeroX / applyBestOneX call refresh() lazily when _liveModels=[].
    monitor.setModelManager(modelManager);
    modelManager.refresh().then(() => {
        modelManager.registerDynamicCommands(context);
    });
    // ── Commands ──────────────────────────────────────────────────────────────
    context.subscriptions.push(vscode.commands.registerCommand("copilotMonitor.start", () => {
        monitor?.start();
        vscode.window.showInformationMessage("Copilot Agent Monitor started ✅");
    }), vscode.commands.registerCommand("copilotMonitor.stop", () => {
        monitor?.stop();
        vscode.window.showInformationMessage("Copilot Agent Monitor stopped ⛔");
    }), vscode.commands.registerCommand("copilotMonitor.resetCostGuard", () => {
        monitor?.resetCostGuard();
    }), vscode.commands.registerCommand("copilotMonitor.forceAnalyze", async () => {
        await monitor?.forceAnalyze();
    }), vscode.commands.registerCommand("copilotMonitor.showLog", () => {
        const log = monitor?.getActivityLog() ?? [];
        if (log.length === 0) {
            vscode.window.showInformationMessage("No activity recorded yet.");
            return;
        }
        // Show in a quick pick — last 20 actions, newest first
        const items = log
            .slice(-20)
            .reverse()
            .map((entry) => ({
            label: `$(${actionIcon(entry.action)}) ${entry.action}`,
            description: entry.executedAt.toLocaleTimeString(),
            detail: `${entry.state} — ${entry.reasoning} ${entry.actionOk ? "✅" : "❌"}`,
        }));
        vscode.window.showQuickPick(items, {
            title: "Copilot Monitor — Recent Activity",
            placeHolder: "Last 20 agent actions",
        });
    }), 
    // ── Selector Wizard ────────────────────────────────────────────────────
    vscode.commands.registerCommand("copilotMonitor.learnSelectors", async () => {
        await selectorWizard?.open();
    }), vscode.commands.registerCommand("copilotMonitor.showSelectors", () => {
        if (!selectorStore) {
            return;
        }
        const map = selectorStore.snapshot();
        const confirmed = selectorStore.confirmedCount();
        const items = Object.entries(map).map(([id, entry]) => ({
            label: `$(${entry.confidence === "default" ? "circle-slash" : "check"}) ${id}`,
            description: entry.confidence === "default"
                ? "default"
                : `\u2705 ${entry.confidence}`,
            detail: entry.selector,
        }));
        vscode.window.showQuickPick(items, {
            title: "Copilot Monitor \u2014 Learned Selectors",
            placeHolder: `${confirmed} confirmed  |  ${items.length - confirmed} default`,
        });
    }), 
    // ── Model switcher: QuickPick with live models only ─────────────────────
    vscode.commands.registerCommand("copilotMonitor.switchModel", async () => {
        if (!modelManager) {
            vscode.window.showWarningMessage("ModelManager no inicializado.");
            return;
        }
        // Always refresh before showing picker to show current available models
        await modelManager.refresh();
        await modelManager.showModelPicker();
    }), 
    // ── Audit log viewer ──────────────────────────────────────────────────
    vscode.commands.registerCommand("copilotMonitor.openAudit", async () => {
        const auditPath = monitor?.getAuditPath();
        if (!auditPath) {
            vscode.window.showWarningMessage("Monitor not started — no audit file yet.");
            return;
        }
        const uri = vscode.Uri.file(auditPath);
        await vscode.commands.executeCommand("vscode.open", uri);
    }), 
    // ── Premium (1x) request breakdown — clicked from StatusBar ────────────
    vscode.commands.registerCommand("copilotMonitor.show1xRequests", () => {
        if (!modelManager) {
            vscode.window.showInformationMessage("ModelManager no inicializado.");
            return;
        }
        const stats = modelManager.premiumStats;
        if (stats.totalRequests === 0) {
            vscode.window.showInformationMessage("$(server-process) No se han registrado requests en modelos 1x esta sesión.");
            return;
        }
        const items = stats.log
            .slice(-20)
            .reverse()
            .map((entry) => ({
            label: `$(server-process) ${entry.modelId}`,
            description: new Date(entry.ts).toLocaleTimeString(),
            detail: `Trigger: ${entry.trigger}`,
        }));
        vscode.window.showQuickPick(items, {
            title: "Premium (1x) Requests — Last 20",
            placeHolder: `Sessions: ${stats.sessionCount} | Prompts: ${stats.promptCount} | Total: ${stats.totalRequests}`,
        });
    }));
    // ── Configuration change handler ──────────────────────────────────────────
    context.subscriptions.push(vscode.workspace.onDidChangeConfiguration((e) => {
        if (e.affectsConfiguration("copilotMonitor")) {
            monitor?.reloadConfig();
        }
    }));
    // ── Auto-start if enabled ─────────────────────────────────────────────────
    const cfg = vscode.workspace.getConfiguration("copilotMonitor");
    if (cfg.get("enabled", true)) {
        monitor.start();
    }
}
function deactivate() {
    monitor?.stop();
}
// ─── Helpers ─────────────────────────────────────────────────────────────────
function actionIcon(action) {
    const icons = {
        WAIT: "circle-slash",
        SEND_CONTINUE: "play",
        OPEN_NEW_CHAT: "add",
        STOP_AND_NEW_CHAT: "stop-circle",
        CYCLE_MODEL: "sync",
        SWITCH_CHAT_MODEL: "arrow-swap",
        SELECT_SPECIFIC_MODEL: "chevron-right",
        VALIDATE_ZERO_X: "beaker",
        FOCUS_VSCODE: "window",
    };
    return icons[action] ?? "question";
}
