/**
 * StatusBar — Shows the agent state + premium (1x) request counter.
 *
 * Two items:
 *   Left item  — agent state (existing)
 *   Right item — premium request count: "$(server-process) 1x: N req"
 *                Clickable → shows detailed log of premium requests
 */

import * as vscode from "vscode";
import { AgentState, PremiumRequestStats } from "./types";

const STATE_CONFIG: Record<AgentState, { icon: string; color?: string }> = {
  // v1.3.11: GENERATING gets a prominent background so DOM-active status is
  // always visible in the status bar (was color: undefined → invisible).
  [AgentState.GENERATING]: {
    icon: "$(loading~spin)",
    color: new vscode.ThemeColor("statusBarItem.prominentBackground") as any,
  },
  [AgentState.COMPLETED]: { icon: "$(check)", color: undefined },
  [AgentState.IDLE]: { icon: "$(eye)", color: undefined },
  [AgentState.STALLED_SOFT]: {
    icon: "$(warning)",
    color: new vscode.ThemeColor("statusBarItem.warningBackground") as any,
  },
  [AgentState.STALLED_HARD]: {
    icon: "$(error)",
    color: new vscode.ThemeColor("statusBarItem.errorBackground") as any,
  },
  [AgentState.ERROR_RATE_LIMIT]: {
    icon: "$(clock)",
    color: new vscode.ThemeColor("statusBarItem.warningBackground") as any,
  },
  [AgentState.ERROR_HARD]: {
    icon: "$(server-process)",
    color: new vscode.ThemeColor("statusBarItem.warningBackground") as any,
  },
  [AgentState.ERROR_CONTEXT]: {
    icon: "$(graph)",
    color: new vscode.ThemeColor("statusBarItem.errorBackground") as any,
  },
  [AgentState.ERROR_SWITCH_MODEL]: {
    icon: "$(sync)",
    color: new vscode.ThemeColor("statusBarItem.warningBackground") as any,
  },
  [AgentState.VSCODE_HIDDEN]: { icon: "$(window)", color: undefined },
  [AgentState.RECOVERING]: { icon: "$(sync~spin)", color: undefined },
  [AgentState.STOPPED]: { icon: "$(debug-stop)", color: undefined },
};

export class StatusBar implements vscode.Disposable {
  private _stateItem: vscode.StatusBarItem;
  private _premiumItem: vscode.StatusBarItem;

  constructor() {
    // ── State indicator (right side, priority 100) ─────────────────────────
    this._stateItem = vscode.window.createStatusBarItem(
      vscode.StatusBarAlignment.Right,
      100,
    );
    this._stateItem.command = "copilotMonitor.showLog";
    this._stateItem.tooltip = "Copilot Agent Monitor — click for activity log";
    this._stateItem.text = "$(eye) Agent Monitor";
    this._stateItem.show();

    // ── Premium request counter (right side, priority 99 — just left of state) ─
    this._premiumItem = vscode.window.createStatusBarItem(
      vscode.StatusBarAlignment.Right,
      99,
    );
    this._premiumItem.command = "copilotMonitor.show1xRequests";
    this._premiumItem.text = "$(server-process) 1x: 0 req";
    this._premiumItem.tooltip =
      "Premium (1x) model requests this session\nClick for breakdown";
    this._premiumItem.show();
  }

  setState(state: AgentState, detail: string): void {
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
  updatePremiumCount(stats: PremiumRequestStats): void {
    const n = stats.totalRequests;
    const icon =
      n >= 50 ? "$(error)" : n >= 20 ? "$(warning)" : "$(server-process)";
    this._premiumItem.text = `${icon} 1x: ${n} req`;

    const sessionLine = `Sessions: ${stats.sessionCount}`;
    const promptLine = `Prompts: ${stats.promptCount}`;
    const totalLine = `Total estimated: ${stats.totalRequests}`;
    const lastLine =
      stats.log.length > 0
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

  dispose(): void {
    this._stateItem.dispose();
    this._premiumItem.dispose();
  }
}
