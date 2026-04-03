"use strict";
/**
 * StatusBar — Shows the agent state + premium (1x) request counter.
 *
 * Two items:
 *   Left item  — agent state (existing)
 *   Right item — premium request count: "$(server-process) 1x: N req"
 *                Clickable → shows detailed log of premium requests
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
exports.StatusBar = void 0;
const vscode = __importStar(require("vscode"));
const STATE_CONFIG = {
    // v1.3.11: GENERATING gets a prominent background so DOM-active status is
    // always visible in the status bar (was color: undefined → invisible).
    ["GENERATING" /* AgentState.GENERATING */]: {
        icon: "$(loading~spin)",
        color: new vscode.ThemeColor("statusBarItem.prominentBackground"),
    },
    ["COMPLETED" /* AgentState.COMPLETED */]: { icon: "$(check)", color: undefined },
    ["IDLE" /* AgentState.IDLE */]: { icon: "$(eye)", color: undefined },
    ["STALLED_SOFT" /* AgentState.STALLED_SOFT */]: {
        icon: "$(warning)",
        color: new vscode.ThemeColor("statusBarItem.warningBackground"),
    },
    ["STALLED_HARD" /* AgentState.STALLED_HARD */]: {
        icon: "$(error)",
        color: new vscode.ThemeColor("statusBarItem.errorBackground"),
    },
    ["ERROR_RATE_LIMIT" /* AgentState.ERROR_RATE_LIMIT */]: {
        icon: "$(clock)",
        color: new vscode.ThemeColor("statusBarItem.warningBackground"),
    },
    ["ERROR_HARD" /* AgentState.ERROR_HARD */]: {
        icon: "$(server-process)",
        color: new vscode.ThemeColor("statusBarItem.warningBackground"),
    },
    ["ERROR_CONTEXT" /* AgentState.ERROR_CONTEXT */]: {
        icon: "$(graph)",
        color: new vscode.ThemeColor("statusBarItem.errorBackground"),
    },
    ["ERROR_SWITCH_MODEL" /* AgentState.ERROR_SWITCH_MODEL */]: {
        icon: "$(sync)",
        color: new vscode.ThemeColor("statusBarItem.warningBackground"),
    },
    ["VSCODE_HIDDEN" /* AgentState.VSCODE_HIDDEN */]: { icon: "$(window)", color: undefined },
    ["RECOVERING" /* AgentState.RECOVERING */]: { icon: "$(sync~spin)", color: undefined },
    ["STOPPED" /* AgentState.STOPPED */]: { icon: "$(debug-stop)", color: undefined },
};
class StatusBar {
    _stateItem;
    _premiumItem;
    constructor() {
        // ── State indicator (right side, priority 100) ─────────────────────────
        this._stateItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
        this._stateItem.command = "copilotMonitor.showLog";
        this._stateItem.tooltip = "Copilot Agent Monitor — click for activity log";
        this._stateItem.text = "$(eye) Agent Monitor";
        this._stateItem.show();
        // ── Premium request counter (right side, priority 99 — just left of state) ─
        this._premiumItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 99);
        this._premiumItem.command = "copilotMonitor.show1xRequests";
        this._premiumItem.text = "$(server-process) 1x: 0 req";
        this._premiumItem.tooltip =
            "Premium (1x) model requests this session\nClick for breakdown";
        this._premiumItem.show();
    }
    setState(state, detail) {
        const cfg = STATE_CONFIG[state] ?? { icon: "$(question)" };
        const text = `${cfg.icon} ${detail.slice(0, 40)}`;
        this._stateItem.text = text;
        this._stateItem.backgroundColor = cfg.color
            ? new vscode.ThemeColor(String(cfg.color))
            : undefined;
        this._stateItem.tooltip = `Copilot Monitor: ${state}\n${detail}`;
    }
    /**
     * Update the premium request counter.
     * Called by Monitor.ts whenever ModelManager fires a premium callback.
     */
    updatePremiumCount(stats) {
        const n = stats.totalRequests;
        const icon = n >= 50 ? "$(error)" : n >= 20 ? "$(warning)" : "$(server-process)";
        this._premiumItem.text = `${icon} 1x: ${n} req`;
        const sessionLine = `Sessions: ${stats.sessionCount}`;
        const promptLine = `Prompts: ${stats.promptCount}`;
        const totalLine = `Total estimated: ${stats.totalRequests}`;
        const lastLine = stats.log.length > 0
            ? `Last: ${new Date(stats.log[stats.log.length - 1].ts).toLocaleTimeString()} – ${stats.log[stats.log.length - 1].modelId}`
            : "";
        this._premiumItem.tooltip =
            `$(server-process) Premium (1x) requests this session\n` +
                `${sessionLine}\n${promptLine}\n${totalLine}` +
                (lastLine ? `\n${lastLine}` : "") +
                `\n\nClick for full log`;
        // Turn red at high usage to alert the user
        this._premiumItem.backgroundColor =
            n >= 50
                ? new vscode.ThemeColor("statusBarItem.errorBackground")
                : n >= 20
                    ? new vscode.ThemeColor("statusBarItem.warningBackground")
                    : undefined;
    }
    dispose() {
        this._stateItem.dispose();
        this._premiumItem.dispose();
    }
}
exports.StatusBar = StatusBar;
