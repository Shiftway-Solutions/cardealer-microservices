/**
 * ChatDOMWatcher — Real-time DOM monitoring of VS Code's Copilot Chat panel.
 *
 * Uses CDP (Chrome DevTools Protocol) to inject a MutationObserver into the
 * VS Code renderer (Electron/Chromium). Detects ALL chat activity in real-time:
 *
 *   • "Thinking...", "Preparing", "Getting chat ready"   (silent generating)
 *   • "Compacting conversation"                          (context management)
 *   • "Searching...", "Analyzing...", "Running...",
 *     "Reading...", "Writing...", "Editing...",
 *     "Calling...", "Initializing..."                    (tool calls)
 *   • Stop button appearing/disappearing                 (generation start/end)
 *   • New response blocks streaming in                   (any response activity)
 *   • Loading/spinner elements added to DOM              (any progress indicator)
 *
 * Signal tier: TERTIARY — free (no API calls, no file I/O), ~400ms latency.
 * Complements LogWatcher (PRIMARY) and ScreenAnalyzer (SECONDARY).
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * REQUIRES: VS Code launched with --remote-debugging-port=9222
 *
 *   macOS:   code --remote-debugging-port=9222 /path
 *   Windows: code.exe --remote-debugging-port=9222 /path
 *
 * Gracefully disabled when CDP is unavailable — the Monitor continues with
 * LogWatcher + ScreenAnalyzer as normal.
 * ─────────────────────────────────────────────────────────────────────────────
 */

import { CDPClient } from "./CDPClient";
import {
  DOMEvent,
  DOMQueueItem,
  DOMDelta,
  LastMessageCategory,
  LastMessageReading,
} from "./types";

export type DOMDeltaCallback = (delta: DOMDelta) => void;

// ─── Scripts injected into the VS Code renderer ───────────────────────────────

/**
 * One-shot injection: installs MutationObserver + stop-button poll.
 * Uses `var` and ES5 style intentionally to avoid strict-mode issues
 * in the VS Code renderer context.
 *
 * Events pushed to window.__chatDOMQueue:
 *   { type: DOMEvent, text: string, ts: number }
 */
const INJECT_SCRIPT = /* javascript */ `
(function() {
  if (window.__chatObserverInstalled) { return 'ALREADY'; }
  window.__chatObserverInstalled = true;
  window.__chatDOMQueue = [];
  window.__chatStopBtnPresent = !!document.querySelector('[aria-label="Stop"]');

  var MAX_QUEUE = 120;
  // TEXT_PATTERNS: all known Copilot Chat / Agent activity phrases.
  // Any text matching this while the stop-button is present → TEXT_CHANGE event.
  // PROGRESS_POLL is now an unconditional heartbeat (no TEXT_PATTERNS guard)
  // so the stall timer resets even for unmatched phrases like "Working".
  var TEXT_PATTERNS = /Thinking|Preparing|Getting\\s+chat\\s+ready|Compacting|Searching|Analyzing|Running|Reading|Writing|Editing|Calling|Initializ|Loading\\s+chat|Fetching|Generating|Working|Processing|Verifying|Checking|Creating|Updating|Deleting|Testing|Formatting|Refactoring|Summarizing|Executing|Building|Scanning|Reviewing|Parsing|Indexing/i;

  function push(type, text) {
    if (window.__chatDOMQueue.length >= MAX_QUEUE) { window.__chatDOMQueue.shift(); }
    window.__chatDOMQueue.push({ type: type, text: (text || '').slice(0, 200), ts: Date.now() });
  }

  // Extract progress/status text from an element or its notable descendants.
  function extractProgressText(el) {
    if (!(el instanceof Element)) { return null; }
    var prog = (
      el.classList.contains('interactive-response-progress-tree') ||
      el.classList.contains('chat-progress-message') ||
      el.classList.contains('chat-tree-item-label') ||
      el.classList.contains('rendered-markdown') ||
      el.classList.contains('chat-markdown-part')
    ) ? el : el.querySelector(
      '.interactive-response-progress-tree, .chat-progress-message, .chat-tree-item-label'
    );
    if (!prog) { return null; }
    return (prog.innerText || prog.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
  }

  function hasSpinner(el) {
    if (!(el instanceof Element)) { return false; }
    if (el.classList.contains('codicon-loading') || el.classList.contains('progress-bit')) { return true; }
    return !!el.querySelector('.codicon-loading, .progress-bit, .codicon-modifier-spin');
  }

  function isResponseContainer(el) {
    if (!(el instanceof Element)) { return false; }
    return el.classList.contains('interactive-item-container') &&
           el.classList.contains('interactive-response');
  }

  function handleMutations(mutations) {
    for (var i = 0; i < mutations.length; i++) {
      var m = mutations[i];

      // ── Added nodes ───────────────────────────────────────────────────────
      for (var j = 0; j < m.addedNodes.length; j++) {
        var node = m.addedNodes[j];

        if (hasSpinner(node)) {
          push('SPINNER', '');
          continue;
        }

        if (isResponseContainer(node)) {
          push('NEW_RESPONSE', '');
          continue;
        }

        var pt = extractProgressText(node);
        if (pt && TEXT_PATTERNS.test(pt)) {
          push('PROGRESS_TEXT', pt);
        }
      }

      // ── Text node / childList changes in progress areas ───────────────────
      // BUG FIX 3+5: target for characterData mutations is a TextNode which
      // has no .closest() in Chromium/Electron → use parentElement to get an
      // Element. Also removed '.interactive-item-container' from the selector
      // because it matches already-completed response containers and causes
      // false TEXT_CHANGE → isGenerating=true on post-render adjustments.
      if (m.type === 'characterData' || m.type === 'childList') {
        try {
          var target = m.target;
          var el = (target.nodeType === 1) ? target : target.parentElement;
          var closest = el ? el.closest(
            '.interactive-response-progress-tree, .chat-progress-message, ' +
            '.chat-tree-item-label, .chat-typed-input'
          ) : null;
          if (closest) {
            var txt = (closest.innerText || closest.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
            if (txt && TEXT_PATTERNS.test(txt)) {
              push('TEXT_CHANGE', txt);
            }
          }
        } catch(e) { /* ignore unexpected DOM errors */ }
      }
    }
  }

  // ── Chat input typing detection ─────────────────────────────────────────────
  // Listens for keydown / input events on the chat input element.
  // Fires USER_TYPING whenever the user presses a key in the chat box.
  // Uses a short debounce (1s) so we emit one event per typing burst.
  var __typingTimer = null;
  var __lastTyping = 0;
  function onChatInput() {
    var now = Date.now();
    // Debounce: emit at most once per 800ms
    if (now - __lastTyping < 800) { return; }
    __lastTyping = now;
    push('USER_TYPING', '');
  }
  function bindChatInputWatcher() {
    var INPUT_SELECTORS = [
      '.interactive-input-part textarea',
      '.chat-input textarea',
      '[aria-label="Chat Input"] textarea',
      '.monaco-editor[data-keybinding-context] textarea'
    ];
    for (var si = 0; si < INPUT_SELECTORS.length; si++) {
      var inp = document.querySelector(INPUT_SELECTORS[si]);
      if (inp && !inp.__oklaTypingBound) {
        inp.addEventListener('keydown', onChatInput, { passive: true });
        inp.addEventListener('input', onChatInput, { passive: true });
        inp.__oklaTypingBound = true;
      }
    }
  }
  bindChatInputWatcher();
  // Re-bind every 5s in case the chat panel was re-created
  setInterval(bindChatInputWatcher, 5000);

  // ── MutationObserver ──────────────────────────────────────────────────────
  var root = document.querySelector('.monaco-workbench') || document.body;
  var observer = new MutationObserver(handleMutations);
  observer.observe(root, { childList: true, subtree: true, characterData: true });
  window.__chatMutationObserver = observer;

  // ── Stop-button poll (every 500 ms) ───────────────────────────────────────
  // Most reliable single indicator: present = generating, absent = idle/done.
  // Also scrapes current progress text while the button is present.
  window.__chatStopBtnInterval = setInterval(function() {
    var hasStop = !!document.querySelector('[aria-label="Stop"]');
    var had = window.__chatStopBtnPresent;
    window.__chatStopBtnPresent = hasStop;

    if (hasStop && !had)  { push('STOP_BTN_APPEARED', ''); }
    if (!hasStop && had)  { push('STOP_BTN_GONE', ''); }

    // While stop-button is active, ALWAYS push PROGRESS_POLL as a heartbeat.
    // BUG FIX C: document.querySelector returns the first matching element even
    // if empty. Iterate each selector independently and use the first one that
    // has actual text content, so nested labels like .chat-tree-item-label are
    // found even when the parent .interactive-response-progress-tree is empty.
    if (hasStop) {
      var PROG_SELECTORS = [
        '.chat-progress-message',
        '.chat-tree-item-label',
        '.interactive-response-progress-tree',
        '.chat-status-message',
        '.interactive-progress-message',
        '.chat-markdown-part p'
      ];
      var t = '';
      for (var pi = 0; pi < PROG_SELECTORS.length; pi++) {
        var progEl = document.querySelector(PROG_SELECTORS[pi]);
        if (progEl) {
          var candidate = (progEl.innerText || progEl.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 200);
          if (candidate) { t = candidate; break; }
        }
      }
      push('PROGRESS_POLL', t); // always emit — heartbeat + optional label
    }

    // ── Compacting scan (runs always — even without stop button) ─────────────
    // VS Code Copilot's context compaction notification may appear as an
    // interactive-item-container without a stop button being present.
    // We check the last 4 containers to stay O(1) regardless of chat length.
    var containers = document.querySelectorAll('.interactive-item-container');
    if (containers.length) {
      var scanFrom = Math.max(0, containers.length - 4);
      for (var ci = scanFrom; ci < containers.length; ci++) {
        var ct = (containers[ci].innerText || containers[ci].textContent || '').replace(/\s+/g, ' ').trim();
        if (ct && /compact/i.test(ct) && ct.length < 400) {
          // Only push if not already pushed this tick (dedupe with PROGRESS_POLL t)
          if (!hasStop || ct !== t) { push('PROGRESS_POLL', ct.slice(0, 200)); }
          break;
        }
      }
    }
  }, 500);

  return 'INSTALLED';
})()
`.trim();

/** Atomically drains up to 50 items from the shared DOM event queue. */
const DRAIN_SCRIPT = /* javascript */ `
(function() {
  if (!Array.isArray(window.__chatDOMQueue) || !window.__chatDOMQueue.length) { return '[]'; }
  var items = window.__chatDOMQueue.splice(0, 50);
  return JSON.stringify(items);
})()
`.trim();

/** Removes the MutationObserver and stop-button interval. */
const REMOVE_SCRIPT = /* javascript */ `
(function() {
  window.__chatObserverInstalled = false;
  if (window.__chatStopBtnInterval) {
    clearInterval(window.__chatStopBtnInterval);
    window.__chatStopBtnInterval = null;
  }
  if (window.__chatMutationObserver) {
    window.__chatMutationObserver.disconnect();
    window.__chatMutationObserver = null;
  }
  window.__chatDOMQueue = [];
})()
`.trim();

/**
 * Reads the text content of the LAST response block in the chat DOM.
 * Returns JSON: { text: string, found: boolean }
 * Used to classify what the model's last message says (done, error, question, etc.)
 */
const READ_LAST_MSG_SCRIPT = /* javascript */ `
(function() {
  // Prefer the rendered markdown of the last response (clean model text)
  var responses = document.querySelectorAll(
    '.interactive-item-container.interactive-response'
  );
  if (!responses.length) { return JSON.stringify({ text: '', found: false }); }
  var last = responses[responses.length - 1];
  // Try rendered-markdown first (actual model reply text)
  var mdEl = last.querySelector('.rendered-markdown, .chat-markdown-part');
  var text = mdEl
    ? (mdEl.innerText || mdEl.textContent || '')
    : (last.innerText || last.textContent || '');
  text = text.replace(/\\s+/g, ' ').trim().slice(0, 600);
  return JSON.stringify({ text: text, found: !!text });
})()
`.trim();

// ─── ChatDOMWatcher ───────────────────────────────────────────────────────────

export class ChatDOMWatcher {
  private _cdp: CDPClient;
  private _callback: DOMDeltaCallback;
  private _pollTimer: NodeJS.Timeout | null = null;
  private _pollIntervalMs: number;
  private _running = false;
  private _installed = false;
  private _cdpPort: number;

  /**
   * @param cdpPort    CDP port (default 9222, mirrors copilotMonitor.cdpPort)
   * @param callback   Called on every poll cycle that has at least one DOM event.
   * @param pollIntervalMs  Interval between queue drains (default 400ms).
   */
  constructor(
    cdpPort: number = 9222,
    callback: DOMDeltaCallback,
    pollIntervalMs: number = 400,
  ) {
    this._cdpPort = cdpPort;
    this._cdp = new CDPClient(cdpPort);
    this._callback = callback;
    this._pollIntervalMs = pollIntervalMs;
  }

  /**
   * Tries to connect to CDP and install the MutationObserver.
   * Returns `true` if successfully installed, `false` if CDP is unavailable
   * (VS Code not running with --remote-debugging-port).
   */
  async start(): Promise<boolean> {
    if (this._running) {
      return true;
    }

    // Check if CDP is reachable
    if (!(await this._cdp.isAvailable())) {
      return false;
    }

    try {
      await this._cdp.connect();
    } catch {
      return false;
    }

    // Inject the MutationObserver + stop-btn poll
    const result = await this._cdp.evaluate(INJECT_SCRIPT);
    if (result !== "INSTALLED" && result !== "ALREADY") {
      this._cdp.disconnect();
      return false;
    }

    this._installed = true;
    this._running = true;
    this._pollTimer = setInterval(
      () => void this._poll(),
      this._pollIntervalMs,
    );
    return true;
  }

  /** Stops polling and removes the injected observer from the renderer. */
  stop(): void {
    this._running = false;
    if (this._pollTimer) {
      clearInterval(this._pollTimer);
      this._pollTimer = null;
    }
    if (this._installed) {
      // Fire-and-forget — clean up in the renderer
      this._cdp.evaluate(REMOVE_SCRIPT).catch(() => {});
      this._cdp.disconnect();
      this._installed = false;
    }
  }

  /** True if connected and observer is installed. */
  get active(): boolean {
    return this._running && this._installed;
  }

  /**
   * Reads and classifies the last chat response message via CDP.
   * Returns UNKNOWN when CDP is unavailable or the chat DOM has no responses.
   *
   * Classification (no API cost — pure regex):
   *   TASK_COMPLETE      — model signed off cleanly
   *   WAITING_FOR_INPUT  — model asked a question / needs user decision
   *   ERROR_VISIBLE      — error text visible in chat
   *   STILL_WORKING      — tool calls / progress still visible
   *   UNKNOWN            — can't determine
   */
  async readLastMessage(): Promise<LastMessageReading> {
    const unknown: LastMessageReading = {
      category: LastMessageCategory.UNKNOWN,
      text: "",
      confidence: 0,
    };

    if (!this._running || !this._installed) {
      return unknown;
    }

    let raw: string;
    try {
      raw = await this._cdp.evaluate(READ_LAST_MSG_SCRIPT);
    } catch {
      return unknown;
    }

    let parsed: { text: string; found: boolean };
    try {
      parsed = JSON.parse(raw) as { text: string; found: boolean };
    } catch {
      return unknown;
    }

    if (!parsed.found || !parsed.text) {
      return unknown;
    }

    const text = parsed.text;
    return { ...ChatDOMWatcher._classify(text), text };
  }

  /**
   * Classifies a chat message text into a LastMessageCategory.
   * Pure function — no side effects, no API calls.
   */
  private static _classify(
    text: string,
  ): Pick<LastMessageReading, "category" | "confidence"> {
    const t = text.toLowerCase();

    // ── ERROR patterns (highest priority — act immediately) ─────────────────    // Model-switch errors (separate check, highest confidence)
    const MODEL_SWITCH_PAT =
      /please (switch|change|use a different) model|try (a )?different model|this model (is not available|can'?t|cannot|doesn'?t)|model (not available|unavailable|not supported)|switch to (another|a different) model|use (another|a different) model|upgrade your (plan|account)|requires? (a paid plan|copilot pro|subscription|upgrade)|I('m| am) (sorry|unable).*model|model.*isn'?t (available|supported|responding)|consider (using|switching to) (another|a different)/i;
    if (MODEL_SWITCH_PAT.test(text)) {
      return {
        category: LastMessageCategory.ERROR_SWITCH_MODEL,
        confidence: 0.95,
      };
    }
    const ERROR_PAT =
      /rate.?limit|quota.?exceed|too many requests|429|503|500 |server error|context.{0,10}(too long|window|limit|full)|token.{0,10}limit|maximum context|overloaded|try again later|something went wrong|error occurred|failed to (connect|respond|process)|unable to (complete|process|respond)/i;
    if (ERROR_PAT.test(text)) {
      return { category: LastMessageCategory.ERROR_VISIBLE, confidence: 0.9 };
    }

    // ── TASK_COMPLETE patterns ───────────────────────────────────────────────
    const DONE_PAT =
      /\b(i('ve| have) (completed|finished|implemented|created|updated|added|removed|fixed|done|applied)|all (changes|done|set|complete)|task (is )?(complete|done|finished)|successfully (created|updated|added|removed|implemented|applied|fixed|built|generated)|everything is (in order|ready|done|set up|complete)|implementation (is )?(complete|done|ready)|has been (created|updated|added|removed|fixed|implemented|applied)|all (tests? )?(pass|passing)|done!|complete!|finished!)\b/i;
    if (DONE_PAT.test(text)) {
      return { category: LastMessageCategory.TASK_COMPLETE, confidence: 0.85 };
    }

    // ── WAITING_FOR_INPUT patterns ───────────────────────────────────────────
    const WAIT_PAT =
      /\b(would you like|should i|do you want|shall i|what would you|could you (please |confirm |let me know|clarify)|please (let me know|confirm|tell me|clarify|specify|provide)|are you (sure|ready|ok)|which (one|option|approach|version)|how would you (like|prefer)|where (should|do you want)|when you('re)? ready|let me know (if|what|how|which|when))\b|\?(\s*$)/i;
    if (WAIT_PAT.test(text)) {
      return {
        category: LastMessageCategory.WAITING_FOR_INPUT,
        confidence: 0.8,
      };
    }

    // ── STILL_WORKING patterns (tool results mid-stream) ────────────────────
    const WORKING_PAT =
      /^(running|reading|writing|editing|searching|analyzing|calling|creating|updating|deleting|fetching|generating|thinking|preparing|processing|executing|building|checking|verifying)/i;
    if (WORKING_PAT.test(text.trimStart())) {
      return { category: LastMessageCategory.STILL_WORKING, confidence: 0.7 };
    }

    return { category: LastMessageCategory.UNKNOWN, confidence: 0 };
  }

  // ─── Private ───────────────────────────────────────────────────────────────

  private async _poll(): Promise<void> {
    if (!this._running) {
      return;
    }

    let raw: string;
    try {
      raw = await this._cdp.evaluate(DRAIN_SCRIPT);
    } catch {
      // CDP WebSocket may have closed — attempt reconnect on next poll
      this._installed = false;
      void this._tryReconnect();
      return;
    }

    if (!raw || raw === "[]") {
      return;
    }

    let items: DOMQueueItem[];
    try {
      items = JSON.parse(raw) as DOMQueueItem[];
    } catch {
      return;
    }

    if (!items.length) {
      return;
    }

    const progressTexts = items
      .filter(
        (i) =>
          i.type === DOMEvent.PROGRESS_TEXT ||
          i.type === DOMEvent.TEXT_CHANGE ||
          i.type === DOMEvent.PROGRESS_POLL,
      )
      .map((i) => i.text)
      .filter((t) => !!t);

    const isUserTyping = items.some((i) => i.type === DOMEvent.USER_TYPING);

    // BUG FIX 4: NEW_RESPONSE is excluded from isGenerating because DOM can
    // render/re-hydrate a completed response container without any active
    // generation. A NEW_RESPONSE alone is not sufficient evidence of activity.
    const isGenerating = items.some(
      (i) =>
        i.type === DOMEvent.SPINNER ||
        i.type === DOMEvent.STOP_BTN_APPEARED ||
        i.type === DOMEvent.PROGRESS_TEXT ||
        i.type === DOMEvent.TEXT_CHANGE ||
        i.type === DOMEvent.PROGRESS_POLL,
    );

    // BUG FIX 1: isCompleted was always false when STOP_BTN_GONE arrived in
    // the same 400ms batch as TEXT_CHANGE / NEW_RESPONSE (end-of-stream flush).
    // Solution: STOP_BTN_GONE is authoritative. If the only "generating" signals
    // in this batch are TEXT_CHANGE or NEW_RESPONSE (post-stream residue), we
    // still treat the cycle as completed.
    const hasBtnGone = items.some((i) => i.type === DOMEvent.STOP_BTN_GONE);
    const hasOnlyPostCompletionActivity = items.every(
      (i) =>
        i.type === DOMEvent.TEXT_CHANGE ||
        i.type === DOMEvent.NEW_RESPONSE ||
        i.type === DOMEvent.STOP_BTN_GONE,
    );
    const isCompleted =
      hasBtnGone && (!isGenerating || hasOnlyPostCompletionActivity);

    this._callback({
      events: items,
      isGenerating,
      isCompleted,
      hasActivity: true,
      progressTexts,
      isUserTyping,
    });
  }

  /** Attempts to reconnect CDP if the WebSocket was dropped. */
  private async _tryReconnect(): Promise<void> {
    if (!this._running) {
      return;
    }
    try {
      if (!(await this._cdp.isAvailable())) {
        return;
      }
      // Create a fresh CDPClient (old WS is gone)
      this._cdp = new CDPClient(this._cdpPort);
      await this._cdp.connect();
      const result = await this._cdp.evaluate(INJECT_SCRIPT);
      if (result === "INSTALLED" || result === "ALREADY") {
        this._installed = true;
      }
    } catch {
      /* reconnect will be retried on next poll interval */
    }
  }
}
