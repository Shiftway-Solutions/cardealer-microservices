"use strict";
/**
 * SelectorWizard — Guided wizard for learning CSS selectors of VS Code Chat UI elements.
 *
 * ─── HOW IT WORKS ────────────────────────────────────────────────────────────
 *
 * Two modes depending on CDP availability:
 *
 *  MODE A — CDP (recommended, requires --remote-debugging-port=9222):
 *    1. Extension injects a one-shot click listener into VS Code's renderer
 *    2. User clicks the highlighted UI element in VS Code
 *    3. We receive the element's best CSS selector automatically
 *    4. User can edit + confirm or skip
 *
 *  MODE B — Manual (always works, no extra setup):
 *    1. Wizard shows element name, description, and a SCREENSHOT
 *    2. Pre-populated selector from our VS Code 1.95–1.99 research
 *    3. User can test the selector (flashes red outline if CDP is available)
 *       or just confirm/edit the text
 *    4. User proceeds to next element
 *
 * ─── WIZARD PANEL ────────────────────────────────────────────────────────────
 *  A VS Code WebviewPanel (full panel) with:
 *  - Dark VS Code-themed UI
 *  - Step indicator (e.g. "3 / 15")
 *  - Category grouping
 *  - CDP capture button (greyed if unavailable)
 *  - Selector text input  *  - "Test" button → flashes matched elements in VS Code
 *  - Next / Skip / Back
 *  - Final "Save All" page with JSON preview
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
exports.SelectorWizard = void 0;
const vscode = __importStar(require("vscode"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const os = __importStar(require("os"));
const child_process_1 = require("child_process");
const SelectorStore_1 = require("./SelectorStore");
const CDPClient_1 = require("./CDPClient");
// ─────────────────────────────────────────────────────────────────────────────
class SelectorWizard {
    _context;
    _panel = null;
    _cdp;
    _store;
    _state;
    _capturing = false;
    constructor(_context, cdpPort) {
        this._context = _context;
        const port = cdpPort ??
            vscode.workspace
                .getConfiguration("copilotMonitor")
                .get("cdpPort", 9222);
        this._cdp = new CDPClient_1.CDPClient(port);
        this._store = new SelectorStore_1.SelectorStore(_context);
        // Build the CDP launch command using the ACTUAL Electron binary (process.execPath).
        // macOS: /Applications/Visual Studio Code.app/Contents/MacOS/Code
        // This bypasses the `code` CLI shell script which uses ELECTRON_RUN_AS_NODE=1
        // and forwards open-folder IPC to an already-running VS Code, dropping --remote-debugging-port.
        const workspacePath = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? process.cwd();
        const cdpLaunchCmd = `"${process.execPath}" --remote-debugging-port=${port} "${workspacePath}"`;
        const stored = this._store.snapshot();
        this._state = {
            currentIndex: 0,
            elements: SelectorStore_1.KNOWN_ELEMENTS,
            selectors: Object.fromEntries(SelectorStore_1.KNOWN_ELEMENTS.map((el) => [el.id, stored[el.id]?.selector ?? ""])),
            confidences: Object.fromEntries(SelectorStore_1.KNOWN_ELEMENTS.map((el) => [
                el.id,
                stored[el.id]?.confidence ?? "default",
            ])),
            cdpAvailable: false,
            capturing: false,
            total: SelectorStore_1.KNOWN_ELEMENTS.length,
            cdpLaunchCmd,
        };
    }
    // ─── Open wizard ────────────────────────────────────────────────────────────
    async open() {
        if (this._panel) {
            this._panel.reveal();
            return;
        }
        this._panel = vscode.window.createWebviewPanel("selectorWizard", "🎯 Selector Wizard — Learn Chat UI", vscode.ViewColumn.One, {
            enableScripts: true,
            retainContextWhenHidden: true,
            localResourceRoots: [this._context.extensionUri],
        });
        this._panel.webview.html = this._buildHtml();
        // Handle messages from the webview
        this._panel.webview.onDidReceiveMessage((msg) => this._handleMessage(msg), undefined, this._context.subscriptions);
        this._panel.onDidDispose(() => {
            this._panel = null;
            this._capturing = false;
            this._cdp.disconnect();
        });
        // Check CDP availability in background
        this._cdp.isAvailable().then(async (available) => {
            this._state.cdpAvailable = available;
            if (available) {
                try {
                    await this._cdp.connect();
                }
                catch {
                    this._state.cdpAvailable = false;
                }
            }
            this._sendState();
        });
    }
    // ─── Message handler ─────────────────────────────────────────────────────────
    async _handleMessage(msg) {
        switch (msg.type) {
            case "ready":
                this._sendUpdate({ type: "init", state: this._state });
                break;
            case "navigate": {
                if (msg.direction === "next" || msg.direction === "skip") {
                    if (this._state.currentIndex < this._state.total - 1) {
                        this._state.currentIndex++;
                    }
                }
                else if (msg.direction === "prev") {
                    if (this._state.currentIndex > 0) {
                        this._state.currentIndex--;
                    }
                }
                else if (msg.direction === "finish") {
                    this._saveAll();
                    return;
                }
                this._sendState();
                break;
            }
            case "startCapture": {
                if (!this._state.cdpAvailable || this._capturing) {
                    break;
                }
                this._capturing = true;
                this._state.capturing = true;
                this._sendUpdate({ type: "captureStarted" });
                // Focus VS Code window so user can click
                vscode.commands.executeCommand("workbench.action.focusSideBar");
                try {
                    const result = await this._cdp.captureClick(30_000);
                    const el = SelectorStore_1.KNOWN_ELEMENTS[this._state.currentIndex];
                    this._state.selectors[el.id] = result.selector;
                    this._state.confidences[el.id] = "cdp";
                    this._state.capturing = false;
                    this._capturing = false;
                    this._sendUpdate({
                        type: "captureResult",
                        result: {
                            selector: result.selector,
                            tagName: result.tagName,
                            ariaLabel: result.ariaLabel,
                            classes: result.classes,
                        },
                    });
                    this._sendState();
                }
                catch (err) {
                    this._state.capturing = false;
                    this._capturing = false;
                    this._sendUpdate({
                        type: "captureError",
                        message: String(err instanceof Error ? err.message : err),
                    });
                    this._sendState();
                }
                break;
            }
            case "cancelCapture": {
                this._capturing = false;
                this._state.capturing = false;
                await this._cdp.cancelCapture().catch(() => { });
                this._sendState();
                break;
            }
            case "testSelector": {
                if (!msg.selector) {
                    break;
                }
                let matched = 0;
                if (this._state.cdpAvailable) {
                    matched = await this._cdp
                        .highlightSelector(msg.selector)
                        .catch(() => -1);
                }
                this._sendUpdate({
                    type: "testResult",
                    matched,
                    selector: msg.selector,
                });
                break;
            }
            case "selectorEdited": {
                const { elementId, selector } = msg;
                if (SelectorStore_1.KNOWN_ELEMENTS.find((e) => e.id === elementId)) {
                    this._state.selectors[elementId] = selector;
                    this._state.confidences[elementId] = "confirmed";
                }
                break;
            }
            case "requestScreenshot": {
                const buf = await this._captureScreen();
                if (buf) {
                    const dataUrl = `data:image/png;base64,${buf.toString("base64")}`;
                    this._sendUpdate({ type: "screenshot", dataUrl });
                }
                break;
            }
            case "launchCDP": {
                // Copy the CORRECT CDP launch command to clipboard.
                // KEY INSIGHT: `code --remote-debugging-port=9222 .` fails silently when VS Code is
                // already running — the CLI wrapper (ELECTRON_RUN_AS_NODE=1 + cli.js) detects the
                // existing IPC socket and forwards only the open-folder command, dropping the port.
                // Fix: use process.execPath (the real Electron binary) which bypasses cli.js entirely.
                const cmd = this._state.cdpLaunchCmd;
                await vscode.env.clipboard.writeText(cmd);
                // On macOS: open Terminal.app so the user has an EXTERNAL terminal ready before quitting VS Code
                if (process.platform === "darwin") {
                    const { spawn } = await import("child_process");
                    spawn("open", ["-a", "Terminal"], {
                        detached: true,
                        stdio: "ignore",
                    });
                }
                const sel = await vscode.window.showInformationMessage(`✅ Command copied! Terminal.app is opening.\n\n` +
                    `Paste the command there, then click "Quit VS Code" — after VS Code restarts, open the wizard again.`, { modal: true }, "Quit VS Code", "Later");
                if (sel === "Quit VS Code") {
                    vscode.commands.executeCommand("workbench.action.quit");
                }
                break;
            }
            case "resetAll": {
                this._store.resetAll();
                const fresh = new SelectorStore_1.SelectorStore(this._context).snapshot();
                this._state.selectors = Object.fromEntries(SelectorStore_1.KNOWN_ELEMENTS.map((el) => [el.id, fresh[el.id]?.selector ?? ""]));
                this._state.confidences = Object.fromEntries(SelectorStore_1.KNOWN_ELEMENTS.map((el) => [el.id, "default"]));
                this._state.currentIndex = 0;
                this._sendState();
                vscode.window.showInformationMessage("All selectors reset to defaults.");
                break;
            }
        }
    }
    // ─── Save all learned selectors ──────────────────────────────────────────────
    _saveAll() {
        const entries = {};
        for (const el of SelectorStore_1.KNOWN_ELEMENTS) {
            entries[el.id] = {
                selector: this._state.selectors[el.id] ?? "",
                confidence: this._state.confidences[el.id] ?? "confirmed",
            };
        }
        this._store.saveAll(entries);
        this._sendUpdate({ type: "saved" });
        vscode.window.showInformationMessage(`Selector Wizard: ${SelectorStore_1.KNOWN_ELEMENTS.length} selectors saved! ✅`);
    }
    _sendState() {
        this._sendUpdate({ type: "stateUpdate", state: { ...this._state } });
    }
    _sendUpdate(update) {
        this._panel?.webview.postMessage(update);
    }
    // ─── Screenshot helper ───────────────────────────────────────────────────────
    _captureScreen() {
        const tmpFile = path.join(os.tmpdir(), `wizard-${Date.now()}.png`);
        return new Promise((resolve) => {
            let cmd;
            if (process.platform === "darwin") {
                cmd = `screencapture -x -o "${tmpFile}"`;
            }
            else if (process.platform === "linux") {
                cmd = `scrot "${tmpFile}" 2>/dev/null || import -window root "${tmpFile}"`;
            }
            else {
                cmd = `powershell -NoProfile -Command "Add-Type -AssemblyName System.Windows.Forms,System.Drawing; $s=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds; $b=New-Object System.Drawing.Bitmap($s.Width,$s.Height); $g=[System.Drawing.Graphics]::FromImage($b); $g.CopyFromScreen($s.Location,[System.Drawing.Point]::Empty,$s.Size); $b.Save('${tmpFile.replace(/\\/g, "\\\\")}');"`;
            }
            (0, child_process_1.exec)(cmd, { timeout: 10_000 }, (err) => {
                if (err || !fs.existsSync(tmpFile)) {
                    resolve(null);
                    return;
                }
                try {
                    const buf = fs.readFileSync(tmpFile);
                    fs.unlinkSync(tmpFile);
                    resolve(buf);
                }
                catch {
                    resolve(null);
                }
            });
        });
    }
    // ─── HTML ────────────────────────────────────────────────────────────────────
    _buildHtml() {
        return /* html */ `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Selector Wizard</title>
  <style>
    :root {
      --bg:      var(--vscode-editor-background, #1e1e1e);
      --fg:      var(--vscode-editor-foreground, #d4d4d4);
      --accent:  var(--vscode-button-background, #0e639c);
      --accent2: var(--vscode-button-hoverBackground, #1177bb);
      --border:  var(--vscode-panel-border, #404040);
      --input-bg: var(--vscode-input-background, #3c3c3c);
      --badge:   var(--vscode-badge-background, #4d4d4d);
      --success: #4ec9a0;
      --error:   #f44747;
      --warn:    #dcdcaa;
    }
    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: var(--vscode-font-family, 'Segoe UI', system-ui, sans-serif);
      font-size:   var(--vscode-font-size, 13px);
      background:  var(--bg);
      color:       var(--fg);
      height: 100vh;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    /* ── Header ────────────────────────────────── */
    header {
      padding: 12px 20px;
      background: rgba(255,255,255,0.035);
      border-bottom: 1px solid var(--border);
      display: flex;
      align-items: center;
      gap: 12px;
      flex-shrink: 0;
    }
    header h1 { font-size: 14px; font-weight: 600; flex: 1; }
    .cdp-badge {
      font-size: 11px;
      padding: 2px 8px;
      border-radius: 10px;
      font-weight: 600;
    }
    .cdp-badge.on  { background: var(--success); color: #000; }
    .cdp-badge.off { background: var(--badge);   color: var(--fg); cursor: help; }

    /* ── Progress bar ──────────────────────────── */
    .progress-bar {
      height: 3px;
      background: var(--border);
      flex-shrink: 0;
      position: relative;
    }
    .progress-fill {
      position: absolute; top: 0; left: 0; height: 100%;
      background: var(--accent);
      transition: width .3s ease;
    }

    /* ── Main layout ──────────────────────────── */
    .main {
      display: flex;
      flex: 1;
      overflow: hidden;
    }

    /* ── Sidebar: element list ────────────────── */
    .sidebar {
      width: 220px;
      border-right: 1px solid var(--border);
      overflow-y: auto;
      flex-shrink: 0;
    }
    .sidebar-category {
      padding: 8px 12px 4px;
      font-size: 10px;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
      color: var(--vscode-descriptionForeground, #808080);
    }
    .sidebar-item {
      padding: 6px 12px;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 8px;
      border-left: 2px solid transparent;
      transition: background .1s;
    }
    .sidebar-item:hover  { background: rgba(255,255,255,.05); }
    .sidebar-item.active { background: rgba(255,255,255,.08); border-left-color: var(--accent); }
    .sidebar-item .dot {
      width: 7px; height: 7px;
      border-radius: 50%;
      flex-shrink: 0;
    }
    .dot.default   { background: var(--badge); }
    .dot.confirmed { background: var(--success); }
    .dot.cdp       { background: var(--accent); }
    .sidebar-item .item-name { font-size: 12px; }

    /* ── Content area ─────────────────────────── */
    .content {
      flex: 1;
      overflow-y: auto;
      padding: 24px 28px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .card {
      background: rgba(255,255,255,.04);
      border: 1px solid var(--border);
      border-radius: 6px;
      padding: 16px 20px;
    }

    .card-title {
      font-size: 15px;
      font-weight: 600;
      margin-bottom: 4px;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .category-badge {
      font-size: 10px;
      padding: 1px 7px;
      border-radius: 10px;
      background: var(--badge);
      text-transform: uppercase;
      font-weight: 600;
      letter-spacing: .06em;
    }
    .card-description {
      color: var(--vscode-descriptionForeground, #808080);
      margin-bottom: 12px;
      font-size: 12px;
    }

    .hint-box {
      background: rgba(14,99,156,.2);
      border: 1px solid var(--accent);
      border-radius: 4px;
      padding: 10px 14px;
      font-size: 12px;
      color: #9cdcfe;
      margin-bottom: 12px;
    }
    .hint-box strong { display: block; margin-bottom: 2px; color: #fff; font-size: 11px; text-transform: uppercase; letter-spacing: .05em; }

    /* ── Selector row ─────────────────────────── */
    .selector-row {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .selector-label {
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: .05em;
      margin-bottom: 6px;
      color: var(--vscode-descriptionForeground, #808080);
    }
    .selector-input {
      flex: 1;
      background: var(--input-bg);
      border: 1px solid var(--border);
      border-radius: 3px;
      color: var(--fg);
      padding: 6px 10px;
      font-family: var(--vscode-editor-font-family, 'Menlo', monospace);
      font-size: 12px;
      outline: none;
      transition: border-color .15s;
    }
    .selector-input:focus { border-color: var(--accent); }

    .btn {
      padding: 5px 12px;
      border-radius: 3px;
      border: none;
      cursor: pointer;
      font-size: 12px;
      font-weight: 500;
      transition: background .15s, opacity .15s;
    }
    .btn:disabled { opacity: .4; cursor: default; }
    .btn-primary  { background: var(--accent);  color: #fff; }
    .btn-primary:not(:disabled):hover  { background: var(--accent2); }
    .btn-secondary { background: rgba(255,255,255,.1); color: var(--fg); }
    .btn-secondary:not(:disabled):hover { background: rgba(255,255,255,.15); }
    .btn-danger    { background: rgba(244,71,71,.15); color: var(--error); border: 1px solid rgba(244,71,71,.3); }
    .btn-danger:not(:disabled):hover  { background: rgba(244,71,71,.25); }
    .btn-success   { background: rgba(78,201,160,.15); color: var(--success); border: 1px solid rgba(78,201,160,.3); }
    .btn-capture {
      background: var(--accent); color: #fff;
      display: inline-flex; align-items: center; gap: 6px;
      padding: 6px 14px;
      font-size: 12px;
      white-space: nowrap;
    }
    .btn-capture .dot-pulse { width: 8px; height: 8px; border-radius: 50%; background: #fff; flex-shrink: 0; display: inline-block; }
    .btn-capture.capturing .dot-pulse { animation: pulse 1s infinite; }
    @keyframes pulse { 0%,100%{ opacity:1; transform:scale(1); } 50%{ opacity:.5; transform:scale(0.6); } }

    /* ── Test result ──────────────────────────── */
    .test-result {
      font-size: 11px;
      padding: 4px 10px;
      border-radius: 3px;
      margin-top: 6px;
      display: none;
    }
    .test-result.show { display: block; }
    .test-result.ok   { background: rgba(78,201,160,.15); color: var(--success); }
    .test-result.fail { background: rgba(244,71,71,.15);  color: var(--error); }

    /* ── Screenshot ───────────────────────────── */
    .screenshot-section { margin-top: 4px; }
    .screenshot-section summary {
      cursor: pointer;
      font-size: 12px;
      color: var(--vscode-descriptionForeground, #808080);
      padding: 4px 0;
      user-select: none;
    }
    .screenshot-img { width: 100%; border-radius: 4px; border: 1px solid var(--border); margin-top: 8px; }
    .screenshot-loading { font-size: 11px; color: var(--vscode-descriptionForeground); padding: 8px 0; }

    /* ── Footer nav ───────────────────────────── */
    footer {
      padding: 10px 28px;
      border-top: 1px solid var(--border);
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;
      background: rgba(255,255,255,.02);
    }
    .step-counter {
      font-size: 11px;
      color: var(--vscode-descriptionForeground, #808080);
      flex: 1;
    }

    /* ── Summary page ─────────────────────────── */
    .summary-page { display: none; }
    .summary-page.show { display: block; }
    .summary-json {
      background: var(--input-bg);
      border: 1px solid var(--border);
      border-radius: 4px;
      padding: 12px;
      font-family: monospace;
      font-size: 11px;
      overflow: auto;
      max-height: 300px;
    }
    .summary-stat { font-size: 28px; font-weight: 700; color: var(--accent); }
    .summary-stat span { font-size: 14px; font-weight: 400; color: var(--fg); }

    /* ── Welcome screen ───────────────────────── */
    #welcome {
      max-width: 560px;
      margin: 0 auto;
      padding: 32px 0;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }
    #welcome h2 { font-size: 20px; font-weight: 700; }
    #welcome p  { color: var(--vscode-descriptionForeground, #808080); line-height: 1.6; }

    .mode-cards { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .mode-card {
      border: 1px solid var(--border);
      border-radius: 6px;
      padding: 14px 16px;
    }
    .mode-card h3 { font-size: 13px; margin-bottom: 4px; }
    .mode-card p  { font-size: 11px; color: var(--vscode-descriptionForeground); line-height: 1.5; }
    .mode-card.preferred { border-color: var(--accent); background: rgba(14,99,156,.1); }

    .cdp-setup-box {
      background: rgba(220,220,170,.08);
      border: 1px solid rgba(220,220,170,.3);
      border-radius: 4px;
      padding: 12px 16px;
      font-size: 12px;
    }
    .cdp-setup-box code {
      display: block;
      background: rgba(0,0,0,.3);
      border-radius: 3px;
      padding: 6px 10px;
      margin-top: 6px;
      font-family: monospace;
      word-break: break-all;
    }
    /* ── Manual mode banner ───────────────────── */
    .manual-banner {
      background: rgba(78,201,160,.1);
      border: 1px solid rgba(78,201,160,.35);
      border-radius: 4px;
      padding: 10px 14px;
      font-size: 12px;
      color: var(--success);
      display: flex;
      align-items: flex-start;
      gap: 10px;
      margin-bottom: 12px;
    }
    .manual-banner .icon { font-size: 16px; flex-shrink: 0; margin-top: 1px; }
    .manual-banner p { margin: 0; line-height: 1.5; color: var(--fg); }
    .manual-banner p strong { color: var(--success); }
    .cdp-inline-tip {
      font-size: 11px;
      background: rgba(220,220,170,.07);
      border: 1px solid rgba(220,220,170,.25);
      border-radius: 3px;
      padding: 6px 10px;
      margin-top: 10px;
      color: var(--vscode-descriptionForeground, #808080);
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }
    .cdp-inline-tip code { background: rgba(0,0,0,.3); padding: 1px 5px; border-radius: 2px; font-family: monospace; }
  </style>
</head>
<body>

<header>
  <h1>🎯 Selector Wizard — Teach me your VS Code Chat UI</h1>
  <span class="cdp-badge off" id="cdpBadge" title="CDP not available — using manual mode">CDP: OFF</span>
</header>

<div class="progress-bar">
  <div class="progress-fill" id="progressFill" style="width:0%"></div>
</div>

<div class="main" id="mainLayout">

  <!-- ── Sidebar ────────────────────── -->
  <nav class="sidebar" id="sidebar">
    <!-- dynamically populated -->
  </nav>

  <!-- ── Content ────────────────────── -->
  <div class="content" id="contentArea">

    <!-- Welcome screen (shown first) -->
    <div id="welcome">
      <h2>Welcome 👋</h2>
      <p>This wizard teaches the Copilot Agent Monitor the exact CSS selectors
         for each element in the GitHub Copilot Chat panel.</p>
      <p>Once learned, the plugin can click buttons, read state, and automate
         the chat — all via reliable selectors instead of screenshot guessing.</p>

      <div class="mode-cards">
        <div class="mode-card preferred">
          <h3>⚡ Mode A — CDP Live Capture</h3>
          <p>You click elements in VS Code. We auto-capture their selectors in real-time.</p>
          <p style="margin-top:8px; color: var(--warn);" id="cdpStatusMsg">Checking...</p>
        </div>
        <div class="mode-card">
          <h3>✏️ Mode B — Manual / Pre-filled</h3>
          <p>We pre-populate selectors from VS Code source. You review &amp; confirm each one.</p>
          <p style="margin-top:8px; color: var(--success);">Always available — no setup needed.</p>
        </div>
      </div>

      <div class="cdp-setup-box" id="cdpSetupBox" style="display:none">
        <strong style="color: var(--warn);">📡 Enable CDP for live capture:</strong>
        Restart VS Code with the remote debugging flag:
        <code id="cdpCmd">code --remote-debugging-port=9222 .</code>
        <div style="margin-top: 8px; display: flex; gap: 8px; flex-wrap: wrap;">
          <button class="btn btn-secondary" style="font-size:11px" id="copyCdpWelcomeBtn">📋 Copy command &amp; Open Terminal</button>
          <span style="font-size:11px; color: var(--vscode-descriptionForeground); align-self:center">Then reopen this wizard</span>
        </div>
      </div>

      <div style="display: flex; gap: 8px; flex-wrap: wrap; align-items: center;">
        <button class="btn btn-primary" style="width:fit-content; padding: 8px 24px; font-size:13px;" id="startBtn">
          Start in Manual Mode →
        </button>
        <span style="font-size:11px; color: var(--vscode-descriptionForeground);" id="startBtnNote">Selectors are pre-filled — review &amp; confirm each one</span>
      </div>
    </div>

    <!-- Step card (hidden until wizard starts) -->
    <div id="stepCard" style="display:none">
      <div class="card">
        <div class="card-title">
          <span id="elementName">Element Name</span>
          <span class="category-badge" id="elementCategory">input</span>
        </div>
        <div class="card-description" id="elementDescription">Description</div>

        <div class="hint-box" id="hintBox">
          <strong>⬆ Where to click</strong>
          <span id="hintText">Click the element...</span>
        </div>

        <!-- Manual mode banner (shown when CDP is OFF) -->
        <div class="manual-banner" id="manualBanner" style="display:none">
          <div class="icon">✏️</div>
          <div>
            <p><strong>Manual Mode</strong> — The selector below is pre-filled from VS Code source.</p>
            <p style="margin-top:4px; color: var(--vscode-descriptionForeground);">Review it, edit if needed, then click <strong style="color:var(--success)">✅ Confirm &amp; Next</strong>.</p>
          </div>
        </div>

        <!-- CDP capture row (shown only when CDP is ON) -->
        <div id="captureRow" style="margin-bottom: 12px; display: none; align-items: center; gap: 10px; flex-wrap: wrap;">
          <button class="btn btn-capture" id="captureBtn">
            <span class="dot-pulse"></span>
            <span id="captureBtnText">🎯 Capture Live Click</span>
          </button>
          <button class="btn btn-secondary" id="cancelBtn" style="display:none">Cancel</button>
          <span id="captureStatus" style="font-size: 11px; color: var(--vscode-descriptionForeground)"></span>
        </div>

        <!-- CDP inline tip (shown in manual mode) -->
        <div class="cdp-inline-tip" id="cdpInlineTip" style="display:none">
          <span>⚡ Want live capture?</span>
          <code id="cdpTipCmd">code --remote-debugging-port=9222 .</code>
          <button class="btn btn-secondary" style="font-size:10px; padding:2px 8px;" id="copyCdpBtn">📋 Copy &amp; Open Terminal</button>
        </div>

        <!-- Selector input -->
        <div class="selector-label" style="margin-top: 12px;">CSS Selector</div>
        <div class="selector-row">
          <input type="text" class="selector-input" id="selectorInput" placeholder="e.g. .interactive-input-part textarea" />
          <button class="btn btn-secondary" id="testBtn">Test ▶</button>
        </div>
        <div class="test-result" id="testResult"></div>

        <!-- Screenshot -->
        <details class="screenshot-section">
          <summary>📸 See screenshot of current VS Code state</summary>
          <p class="screenshot-loading" id="screenshotLoading">Click to capture...</p>
          <img class="screenshot-img" id="screenshotImg" style="display:none" alt="VS Code screenshot"/>
        </details>
      </div>
    </div>

    <!-- Summary / finish page -->
    <div id="summaryPage" style="display:none" class="card">
      <div class="card-title" style="font-size:18px; margin-bottom: 16px;">🎉 All done!</div>
      <div style="display:flex; gap:32px; margin-bottom: 20px;">
        <div>
          <div class="summary-stat" id="confirmedCount">0</div>
          <div class="summary-stat" style="font-size:18px"><span>confirmed</span></div>
        </div>
        <div>
          <div class="summary-stat" id="defaultCount">0</div>
          <div class="summary-stat" style="font-size:18px"><span>using defaults</span></div>
        </div>
      </div>
      <p style="color: var(--vscode-descriptionForeground); margin-bottom:16px;">
        Selectors are saved to <code style="background:rgba(0,0,0,.3);padding:1px 5px;border-radius:3px">globalStorage/selectors.json</code>
        and used by the Copilot Monitor automatically.
      </p>
      <div class="summary-json" id="summaryJson"></div>
      <div style="margin-top:16px; display:flex; gap:8px;">
        <button class="btn btn-primary" id="saveBtn">Save &amp; Close</button>
        <button class="btn btn-danger" id="resetAllBtn">Reset All to Defaults</button>
      </div>
    </div>

  </div>
</div>

<footer>
  <span class="step-counter" id="stepCounter"></span>
  <button class="btn btn-secondary" id="prevBtn" disabled>← Back</button>
  <button class="btn btn-secondary" id="skipBtn" style="display:none">Skip</button>
  <button class="btn btn-success" id="confirmBtn" style="display:none">✅ Confirm &amp; Next</button>
  <button class="btn btn-primary" id="nextBtn" disabled>Next →</button>
</footer>

<script>
  const vscode = acquireVsCodeApi();

  let state = null;
  let screenshotRequested = false;

  // ── Elements ────────────────────────────────────────────────
  const $ = (id) => document.getElementById(id);

  // ── Receive messages from extension ────────────────────────
  window.addEventListener('message', ({ data }) => {
    const msg = data;

    if (msg.type === 'init' || msg.type === 'stateUpdate') {
      state = msg.state;
      render();
      return;
    }

    if (msg.type === 'captureStarted') {
      $('captureStatus').textContent = '👆 Switch to VS Code and click the element now…';
      $('captureBtn').classList.add('capturing');
      $('captureBtnText').textContent = 'Waiting for click…';
      $('cancelBtn').style.display = 'inline-flex';
      $('captureBtn').disabled = true;
      return;
    }

    if (msg.type === 'captureResult') {
      $('captureStatus').textContent = '✅ Captured!';
      $('captureBtn').classList.remove('capturing');
      $('captureBtnText').textContent = '🎯 Capture Live Click';
      $('captureBtn').disabled = false;
      $('cancelBtn').style.display = 'none';
      $('selectorInput').value = msg.result.selector;
      showTestResult(true, 'Captured from DOM (CDP) — ariaLabel: ' + (msg.result.ariaLabel || 'none'));
      return;
    }

    if (msg.type === 'captureError') {
      $('captureStatus').textContent = '';
      $('captureBtn').classList.remove('capturing');
      $('captureBtnText').textContent = '🎯 Capture Live Click';
      $('cancelBtn').style.display = 'none';
      if (state) { state.capturing = false; $('captureBtn').disabled = false; }
      showTestResult(false, '⚠ ' + msg.message);
      return;
    }

    if (msg.type === 'testResult') {
      if (msg.matched < 0) {
        showTestResult(false, 'CDP not connected — cannot test in live UI');
      } else if (msg.matched === 0) {
        showTestResult(false, 'No elements matched. Selector might be wrong or element not visible.');
      } else {
        showTestResult(true, msg.matched + ' element(s) found &amp; flashed red in VS Code');
      }
      return;
    }

    if (msg.type === 'screenshot') {
      const img = $('screenshotImg');
      const loading = $('screenshotLoading');
      img.src = msg.dataUrl;
      img.style.display = 'block';
      if (loading) loading.style.display = 'none';
      screenshotRequested = false;
      return;
    }

    if (msg.type === 'saved') {
      $('saveBtn').textContent = '✅ Saved!';
      $('saveBtn').disabled = true;
      return;
    }
  });

  // ── render ──────────────────────────────────────────────────
  function render() {
    if (!state) return;

    updateCdpBadge();
    updateSidebar();

    const isWelcomeVisible = !$('stepCard').style.display || $('stepCard').style.display === 'none';

    if ($('welcome').style.display !== 'none') {
      // Welcome screen
      updateWelcomeScreen();
      updateFooter(false);
      return;
    }

    const el = state.elements[state.currentIndex];
    const isSummary = state.currentIndex >= state.total;

    if (isSummary) {
      showSummary();
      return;
    }

    $('stepCard').style.display = 'block';
    $('summaryPage').style.display = 'none';

    $('elementName').textContent     = el.name;
    $('elementCategory').textContent = el.category;
    $('elementDescription').textContent = el.description;
    $('hintText').textContent        = el.captureHint;
    $('selectorInput').value         = state.selectors[el.id] || '';

    // ── CDP ON: show capture row; CDP OFF: show manual banner + tip ──
    if (state.cdpAvailable) {
      $('manualBanner').style.display = 'none';
      $('cdpInlineTip').style.display = 'none';
      $('captureRow').style.display   = 'flex';
    } else {
      $('manualBanner').style.display = 'flex';
      $('cdpInlineTip').style.display = 'flex';
      $('captureRow').style.display   = 'none';
      // Fill CDP tip command — use the exact command provided by the extension
      $('cdpTipCmd').textContent = state.cdpLaunchCmd;
    }

    $('captureBtn').disabled = !state.cdpAvailable || state.capturing;
    if (state.capturing) {
      $('captureBtn').classList.add('capturing');
      $('captureBtnText').textContent = 'Waiting for click…';
      $('captureStatus').textContent  = '👆 Switch to VS Code and click the element now…';
      $('cancelBtn').style.display = 'inline-flex';
    } else {
      $('captureBtn').classList.remove('capturing');
      $('captureBtnText').textContent = '🎯 Capture Live Click';
      $('captureStatus').textContent  = '';
      $('cancelBtn').style.display = 'none';
    }

    // Reset test result
    $('testResult').classList.remove('show', 'ok', 'fail');

    // Screenshot
    $('screenshotImg').style.display = 'none';
    $('screenshotLoading').style.display = 'block';
    screenshotRequested = false;

    updateFooter(true);
  }

  function updateCdpBadge() {
    const badge = $('cdpBadge');
    if (state.cdpAvailable) {
      badge.className = 'cdp-badge on';
      badge.textContent = 'CDP: ON';
      badge.title = 'Chrome DevTools Protocol connected — live selector capture enabled';
    } else {
      badge.className = 'cdp-badge off';
      badge.textContent = 'CDP: OFF';
      badge.title = 'CDP not available. Restart VS Code with --remote-debugging-port=9222 to enable live capture.';
    }
  }

  function updateWelcomeScreen() {
    const cdpMsg = $('cdpStatusMsg');
    const cdpBox = $('cdpSetupBox');
    const startBtn = $('startBtn');
    const startNote = $('startBtnNote');
    if (state.cdpAvailable) {
      cdpMsg.style.color = 'var(--success)';
      cdpMsg.textContent = '✅ CDP connected — live capture available!';
      cdpBox.style.display = 'none';
      startBtn.textContent = 'Start with Live Capture →';
      if (startNote) startNote.style.display = 'none';
    } else {
      cdpMsg.style.color = 'var(--vscode-descriptionForeground)';
      cdpMsg.textContent = '○ CDP not detected — manual mode (pre-filled selectors).';
      cdpBox.style.display = 'block';
      startBtn.textContent = 'Start in Manual Mode →';
      // Use the exact command computed by the extension (process.execPath-based)
      $('cdpCmd').textContent = state.cdpLaunchCmd;
    }
  }

  function updateSidebar() {
    const sidebar = $('sidebar');
    const elements = state.elements;

    // Group by category
    const categories = [...new Set(elements.map(e => e.category))];
    let html = '';
    for (const cat of categories) {
      html += \`<div class="sidebar-category">\${cat}</div>\`;
      for (let i = 0; i < elements.length; i++) {
        const el = elements[i];
        if (el.category !== cat) continue;
        const conf = state.confidences[el.id] || 'default';
        const active = i === state.currentIndex && $('stepCard').style.display !== 'none';
        html += \`<div class="sidebar-item \${active ? 'active' : ''}" onclick="jumpTo(\${i})">
          <div class="dot \${conf}"></div>
          <span class="item-name">\${el.name}</span>
        </div>\`;
      }
    }
    sidebar.innerHTML = html;
  }

  function updateFooter(showNav) {
    const counter = $('stepCounter');
    const prev = $('prevBtn');
    const skip = $('skipBtn');
    const next = $('nextBtn');
    const confirm = $('confirmBtn');

    if (!showNav) {
      counter.textContent = '';
      prev.disabled = true;
      skip.style.display = 'none';
      next.disabled = true;
      confirm.style.display = 'none';
      return;
    }

    counter.textContent = \`Step \${state.currentIndex + 1} of \${state.total}\`;
    prev.disabled = state.currentIndex === 0;
    skip.style.display = 'inline-block';
    next.disabled = false;

    const isLast = state.currentIndex === state.total - 1;

    if (!state.cdpAvailable) {
      // Manual mode: show big green Confirm button
      confirm.style.display = 'inline-block';
      confirm.textContent = isLast ? '✅ Confirm & Review →' : '✅ Confirm & Next';
      next.style.display = 'none';
    } else {
      // CDP mode: use normal Next
      confirm.style.display = 'none';
      next.style.display = 'inline-block';
      next.textContent = isLast ? 'Review & Save →' : 'Next →';
    }
  }

  function showSummary() {
    $('stepCard').style.display   = 'none';
    $('summaryPage').style.display = 'block';

    const confirmed = Object.values(state.confidences).filter(c => c !== 'default').length;
    const defaults  = state.total - confirmed;
    $('confirmedCount').textContent = confirmed;
    $('defaultCount').textContent   = defaults;

    // Build JSON preview
    const preview = {};
    state.elements.forEach(el => {
      preview[el.id] = {
        selector:   state.selectors[el.id],
        confidence: state.confidences[el.id],
      };
    });
    $('summaryJson').textContent = JSON.stringify(preview, null, 2);

    $('stepCounter').textContent = 'Ready to save';
    $('prevBtn').disabled = false;
    $('skipBtn').style.display = 'none';
    $('nextBtn').disabled = true;
  }

  function showTestResult(ok, text) {
    const el = $('testResult');
    el.innerHTML = text;
    el.className = 'test-result show ' + (ok ? 'ok' : 'fail');
  }

  function jumpTo(index) {
    if (!state) return;
    notifySelector();
    state.currentIndex = index;
    $('welcome').style.display = 'none';
    $('stepCard').style.display = 'block';
    render();
  }

  function notifySelector() {
    if (!state) return;
    const el = state.elements[state.currentIndex];
    const input = $('selectorInput');
    if (!el || !input) return;
    vscode.postMessage({ type: 'selectorEdited', elementId: el.id, selector: input.value });
    state.selectors[el.id] = input.value;
  }

  // ── Event listeners ─────────────────────────────────────────
  $('startBtn').addEventListener('click', () => {
    $('welcome').style.display = 'none';
    $('stepCard').style.display = 'block';
    render();
  });

  $('nextBtn').addEventListener('click', () => {
    notifySelector();
    if (state.currentIndex >= state.total - 1) {
      state.currentIndex = state.total;
      showSummary();
      return;
    }
    vscode.postMessage({ type: 'navigate', direction: 'next' });
  });

  $('prevBtn').addEventListener('click', () => {
    notifySelector();
    if (state.currentIndex >= state.total) {
      state.currentIndex = state.total - 1;
      $('summaryPage').style.display = 'none';
      $('stepCard').style.display = 'block';
      updateFooter(true);
      render();
      return;
    }
    vscode.postMessage({ type: 'navigate', direction: 'prev' });
  });

  $('skipBtn').addEventListener('click', () => {
    vscode.postMessage({ type: 'navigate', direction: 'skip' });
  });

  $('confirmBtn').addEventListener('click', () => {
    notifySelector();
    if (state.currentIndex >= state.total - 1) {
      state.currentIndex = state.total;
      showSummary();
      return;
    }
    vscode.postMessage({ type: 'navigate', direction: 'next' });
  });

  $('captureBtn').addEventListener('click', () => {
    vscode.postMessage({ type: 'startCapture' });
  });

  $('cancelBtn').addEventListener('click', () => {
    vscode.postMessage({ type: 'cancelCapture' });
  });

  $('testBtn').addEventListener('click', () => {
    const sel = $('selectorInput').value.trim();
    if (!sel) return;
    vscode.postMessage({ type: 'testSelector', selector: sel });
  });

  $('saveBtn').addEventListener('click', () => {
    notifySelector();
    vscode.postMessage({ type: 'navigate', direction: 'finish' });
  });

  $('resetAllBtn').addEventListener('click', () => {
    if (!confirm('Reset ALL selectors to defaults? This cannot be undone.')) return;
    vscode.postMessage({ type: 'resetAll' });
  });

  $('selectorInput').addEventListener('input', () => {
    $('testResult').classList.remove('show');
  });

  // CDP copy buttons
  function handleCopyCdp() {
    vscode.postMessage({ type: 'launchCDP' });
  }
  const copyCdpBtn = $('copyCdpBtn');
  if (copyCdpBtn) copyCdpBtn.addEventListener('click', handleCopyCdp);
  const copyCdpWelcomeBtn = $('copyCdpWelcomeBtn');
  if (copyCdpWelcomeBtn) copyCdpWelcomeBtn.addEventListener('click', handleCopyCdp);

  // Screenshot on <details> open
  document.querySelector('.screenshot-section').addEventListener('toggle', function(e) {
    if (this.open && !screenshotRequested) {
      screenshotRequested = true;
      $('screenshotLoading').style.display = 'block';
      $('screenshotImg').style.display = 'none';
      vscode.postMessage({ type: 'requestScreenshot' });
    }
  });

  // ── Init ─────────────────────────────────────────────────────
  vscode.postMessage({ type: 'ready' });
</script>
</body>
</html>`;
    }
}
exports.SelectorWizard = SelectorWizard;
