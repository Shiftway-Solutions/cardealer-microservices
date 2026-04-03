/**
 * SelectorStore — Manages learned CSS selectors for VS Code Chat UI elements.
 *
 * Persists selectors to `selectors.json` inside the extension's global storage.
 * Falls back to built-in defaults (researched from VS Code 1.95–1.99 source).
 *
 * Selector confidence levels:
 *   'default'  — pre-researched from VS Code source (not confirmed by user)
 *   'confirmed' — user clicked and confirmed via wizard
 *   'cdp'      — captured in real-time from DOM via Chrome DevTools Protocol
 */

import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";

// ─── Element definitions ───────────────────────────────────────────────────────

export interface KnownElement {
  /** Stable identifier used across the plugin */
  id: string;
  /** Human-readable name shown in wizard */
  name: string;
  /** What this element does */
  description: string;
  /** Hint for the user during wizard capture */
  captureHint: string;
  /** Category for grouping in wizard */
  category: "input" | "toolbar" | "response" | "navigation" | "state";
}

export const KNOWN_ELEMENTS: KnownElement[] = [
  // ── Input area ──────────────────────────────────────────────────────────────
  {
    id: "chatInput",
    name: "Chat Input Textarea",
    description: "The main text area where you type prompts to Copilot",
    captureHint:
      "Click inside the text area at the bottom of the Copilot Chat panel",
    category: "input",
  },
  {
    id: "sendButton",
    name: "Send Button",
    description: "Submits the current message / dispatches to agent",
    captureHint: "Click the send arrow button (→) to the right of the input",
    category: "toolbar",
  },
  {
    id: "stopButton",
    name: "Stop Button",
    description: "Stops the current generation mid-flight",
    captureHint: "While Copilot is generating, click the stop (■) button",
    category: "toolbar",
  },
  {
    id: "newChatButton",
    name: "New Chat Button",
    description: "Opens a fresh chat session (clears context)",
    captureHint: 'Click the "New Chat" (+) icon in the chat panel toolbar',
    category: "navigation",
  },
  {
    id: "modelPickerButton",
    name: "Model Picker",
    description: "Dropdown to switch between AI models (GPT-4o, Claude, etc.)",
    captureHint:
      'Click the model name shown at the bottom of the chat (e.g. "GPT-4o")',
    category: "toolbar",
  },
  {
    id: "attachFilesButton",
    name: "Attach Files Button",
    description: "Attaches files or workspace context to the message",
    captureHint: "Click the paperclip / attach icon near the input area",
    category: "toolbar",
  },
  {
    id: "modePickerButton",
    name: "Mode Picker (Ask/Edit/Agent)",
    description: "Switches between Ask, Edit, and Agent modes",
    captureHint: 'Click the mode selector (shows "Ask", "Edit", or "Agent")',
    category: "navigation",
  },
  {
    id: "clearAllButton",
    name: "Clear All Button",
    description: "Clears all messages from the current chat",
    captureHint: 'Click the "Clear All" button in the chat panel toolbar',
    category: "navigation",
  },
  // ── Response area ───────────────────────────────────────────────────────────
  {
    id: "responseContainer",
    name: "Response Container",
    description: "A single Copilot response message block",
    captureHint: "Click on any assistant message in the chat",
    category: "response",
  },
  {
    id: "progressIndicator",
    name: "Progress / Spinner",
    description: "Loading indicator shown while Copilot is generating",
    captureHint: "Click on the spinning/loading icon while Copilot responds",
    category: "state",
  },
  {
    id: "errorMessage",
    name: "Error Message Block",
    description: "Red error block shown when generation fails",
    captureHint: "Click on any red error banner in the chat",
    category: "state",
  },
  {
    id: "continueInNewSession",
    name: "Continue in New Session Button",
    description: "Button shown when context limit is reached",
    captureHint: 'Click the "Continue in new session" link inside a response',
    category: "response",
  },
  // ── Context / feedback ──────────────────────────────────────────────────────
  {
    id: "likeButton",
    name: "Like Response Button",
    description: "Thumbs up feedback for a response",
    captureHint: "Hover over a response and click the thumbs-up icon",
    category: "response",
  },
  {
    id: "dislikeButton",
    name: "Dislike Response Button",
    description: "Thumbs down feedback for a response",
    captureHint: "Hover over a response and click the thumbs-down icon",
    category: "response",
  },
  {
    id: "chatHistoryButton",
    name: "Chat History Button",
    description: "Opens the list of previous chat sessions",
    captureHint: "Click the clock/history icon in the chat toolbar",
    category: "navigation",
  },
];

// ─── Default selectors (researched from VS Code 1.95–1.99 source) ──────────────
// These work without user confirmation but may shift in future VS Code updates.

export const DEFAULT_SELECTORS: Record<string, string> = {
  chatInput: ".interactive-input-part .monaco-editor textarea",
  sendButton: '[aria-label="Send and Dispatch"]',
  stopButton: '[aria-label="Stop"]',
  newChatButton: '[aria-label="New Chat"]',
  modelPickerButton: ".chat-model-picker-button",
  attachFilesButton: '[aria-label="Attach files"]',
  modePickerButton: ".chat-mode-picker-button",
  clearAllButton: '[aria-label="Clear All"]',
  responseContainer: ".interactive-item-container.interactive-response",
  progressIndicator: ".interactive-response-progress-tree .codicon-loading",
  errorMessage: ".interactive-item-container.interactive-response.error",
  continueInNewSession: '[aria-label="Continue in new session"]',
  likeButton: '[aria-label="Like Response"]',
  dislikeButton: '[aria-label="Dislike Response"]',
  chatHistoryButton: '[aria-label="Chat History"]',
};

// ─── Stored selector entry ─────────────────────────────────────────────────────

export interface StoredSelector {
  selector: string;
  confidence: "default" | "confirmed" | "cdp";
  learnedAt?: string; // ISO date
  vscodeVersion?: string;
}

export type SelectorsMap = Record<string, StoredSelector>;

// ─── Store class ───────────────────────────────────────────────────────────────

export class SelectorStore {
  private _filePath: string;
  private _cache: SelectorsMap | null = null;

  constructor(private readonly _context: vscode.ExtensionContext) {
    this._filePath = path.join(
      _context.globalStorageUri.fsPath,
      "selectors.json",
    );
  }

  /** Loads all selectors (cache + merge with defaults). */
  load(): SelectorsMap {
    if (this._cache) {
      return this._cache;
    }

    let saved: SelectorsMap = {};

    if (fs.existsSync(this._filePath)) {
      try {
        saved = JSON.parse(
          fs.readFileSync(this._filePath, "utf8"),
        ) as SelectorsMap;
      } catch {
        saved = {};
      }
    }

    // Merge: saved overrides defaults
    const result: SelectorsMap = {};
    for (const el of KNOWN_ELEMENTS) {
      if (saved[el.id]) {
        result[el.id] = saved[el.id];
      } else {
        result[el.id] = {
          selector: DEFAULT_SELECTORS[el.id] ?? "",
          confidence: "default",
        };
      }
    }

    this._cache = result;
    return this._cache;
  }

  /** Returns the CSS selector for a given element id. */
  getSelector(elementId: string): string {
    const map = this.load();
    return map[elementId]?.selector ?? DEFAULT_SELECTORS[elementId] ?? "";
  }

  /** Saves or updates a single selector entry. */
  save(
    elementId: string,
    selector: string,
    confidence: StoredSelector["confidence"],
  ): void {
    const map = this.load();

    map[elementId] = {
      selector,
      confidence,
      learnedAt: new Date().toISOString(),
      vscodeVersion: vscode.version,
    };

    this._cache = map;
    this._persist();
  }

  /** Saves the entire map at once (used after wizard completes). */
  saveAll(
    entries: Record<
      string,
      { selector: string; confidence: StoredSelector["confidence"] }
    >,
  ): void {
    const map = this.load();
    for (const [id, data] of Object.entries(entries)) {
      map[id] = {
        selector: data.selector,
        confidence: data.confidence,
        learnedAt: new Date().toISOString(),
        vscodeVersion: vscode.version,
      };
    }
    this._cache = map;
    this._persist();
  }

  /** Resets a single selector back to its default. */
  reset(elementId: string): void {
    const map = this.load();
    map[elementId] = {
      selector: DEFAULT_SELECTORS[elementId] ?? "",
      confidence: "default",
    };
    this._cache = map;
    this._persist();
  }

  /** Resets ALL selectors to defaults. */
  resetAll(): void {
    this._cache = null;
    if (fs.existsSync(this._filePath)) {
      fs.unlinkSync(this._filePath);
    }
  }

  /** Returns a JSON-serializable snapshot for the wizard UI. */
  snapshot(): SelectorsMap {
    return structuredClone(this.load());
  }

  /** How many elements have been confirmed by the user (not just defaults). */
  confirmedCount(): number {
    const map = this.load();
    return Object.values(map).filter(
      (e) => e.confidence === "confirmed" || e.confidence === "cdp",
    ).length;
  }

  private _persist(): void {
    try {
      const dir = path.dirname(this._filePath);
      if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
      }
      fs.writeFileSync(
        this._filePath,
        JSON.stringify(this._cache, null, 2),
        "utf8",
      );
    } catch (err) {
      console.error("[SelectorStore] Failed to persist selectors:", err);
    }
  }
}
