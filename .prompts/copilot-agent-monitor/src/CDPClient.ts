/**
 * CDPClient — Chrome DevTools Protocol client for VS Code DOM inspection.
 *
 * VS Code runs on Electron (Chromium). When launched with `--remote-debugging-port=9222`,
 * the renderer process exposes CDP on that port. This client:
 *
 *   1. Checks if CDP is available at localhost:9222 (or the configured port)
 *   2. Finds the correct page (VS Code workbench, not extension host)
 *   3. Injects a one-shot click listener that captures an element's best CSS selector
 *   4. Returns the selector to the caller via a Promise
 *
 * ──────────────────────────────────────────────────────────────────────────────
 *
 * HOW TO ENABLE CDP IN VS CODE:
 *   macOS/Linux:  code --remote-debugging-port=9222 /path/to/folder
 *   Windows:      code.exe --remote-debugging-port=9222 /path/to/folder
 *
 *   Or add to your launch script:
 *   export ELECTRON_RUN_AS_NODE=0
 *   /Applications/Visual\ Studio\ Code.app/Contents/MacOS/Electron \
 *     --remote-debugging-port=9222 &
 *
 * ──────────────────────────────────────────────────────────────────────────────
 *
 * SECURITY: CDP gives full DOM access to localhost only. Do NOT expose port
 * 9222 to external networks. Only used during the wizard session, then disabled.
 */

import * as http from "http";
import * as net from "net";

// ─── CDP Page descriptor (from /json/list) ────────────────────────────────────

interface CDPPage {
  id: string;
  type: string; // 'page', 'background_page', 'iframe', etc.
  title: string;
  url: string;
  webSocketDebuggerUrl: string;
  devtoolsFrontendUrl: string;
}

// ─── Minimal CDP message types ────────────────────────────────────────────────

interface CDPRequest {
  id: number;
  method: string;
  params?: Record<string, unknown>;
}

interface CDPResponse {
  id?: number;
  result?: Record<string, unknown>;
  error?: { code: number; message: string };
  method?: string; // for events
  params?: Record<string, unknown>;
}

// ─── Script injected into VS Code renderer to capture clicked element ─────────
// Uses a priority chain: data-testid → id → aria-label → class path → tag path
// One-shot: removes itself after first click.

const CAPTURE_SCRIPT = `
(function() {
  if (window.__selectorWizardCapturing) { return 'ALREADY_CAPTURING'; }
  window.__selectorWizardCapturing = true;
  window.__selectorWizardResult   = null;

  function escape(s) {
    return s.replace(/\\\\/g, '\\\\\\\\').replace(/"/g, '\\\\"');
  }

  function bestSelector(el) {
    // Walk up to document body finding the most specific stable selector
    const parts = [];
    let cur = el;

    while (cur && cur.nodeType === 1 && cur !== document.body && cur !== document.documentElement) {
      // Priority 1: data-testid (most stable, added by tests)
      const testId = cur.getAttribute('data-testid');
      if (testId) { parts.unshift('[data-testid="' + escape(testId) + '"]'); break; }

      // Priority 2: meaningful id (not auto-generated numbers/GUIDs)
      if (cur.id && !/^[0-9]/.test(cur.id) && !/^[a-z]{1,3}[0-9]{4,}/i.test(cur.id)) {
        parts.unshift('#' + CSS.escape(cur.id)); break;
      }

      // Priority 3: aria-label (very stable for action buttons)
      const ariaLabel = cur.getAttribute('aria-label');
      if (ariaLabel) {
        let part = cur.localName + '[aria-label="' + escape(ariaLabel) + '"]';
        parts.unshift(part);
        // Check if already unique
        if (document.querySelectorAll(parts.join(' ')).length <= 1) break;
        cur = cur.parentElement;
        continue;
      }

      // Priority 4: stable class names (skip dynamic ones)
      const stableClasses = Array.from(cur.classList).filter(function(c) {
        return !/^(hover|focus|active|selected|expanded|checked|disabled|pressed|loading)/.test(c)
          && !/^[a-z]{1,2}[0-9]/.test(c)   // skip minified class names
          && c.length > 2;
      }).slice(0, 3);

      let part = cur.localName;
      if (stableClasses.length) { part += '.' + stableClasses.join('.'); }
      parts.unshift(part);

      // If already unique enough, stop walking up
      try {
        if (document.querySelectorAll(parts.join(' ')).length <= 1) break;
      } catch(e) { break; }

      cur = cur.parentElement;
    }

    return parts.join(' > ') || el.localName;
  }

  document.addEventListener('click', function handler(e) {
    e.stopImmediatePropagation();
    e.preventDefault();
    document.removeEventListener('click', handler, true);
    window.__selectorWizardCapturing = false;
    window.__selectorWizardResult = {
      selector: bestSelector(e.target),
      tagName:  e.target.tagName.toLowerCase(),
      id:       e.target.id || null,
      ariaLabel: e.target.getAttribute ? e.target.getAttribute('aria-label') : null,
      classes:  Array.from(e.target.classList).join(' '),
      x: e.clientX,
      y: e.clientY,
    };
  }, true);

  return 'CAPTURE_STARTED';
})();
`.trim();

const POLL_RESULT_SCRIPT = `(function() {
  if (window.__selectorWizardResult) {
    const r = JSON.stringify(window.__selectorWizardResult);
    window.__selectorWizardResult = null;
    return r;
  }
  if (window.__selectorWizardCapturing) { return 'WAITING'; }
  return 'IDLE';
})()`;

const CANCEL_SCRIPT = `(function() {
  window.__selectorWizardCapturing = false;
  window.__selectorWizardResult = null;
})()`;

// ─── CDPClient ────────────────────────────────────────────────────────────────

export interface CapturedElement {
  selector: string;
  tagName: string;
  id: string | null;
  ariaLabel: string | null;
  classes: string;
  x: number;
  y: number;
}

export class CDPClient {
  private _port: number;
  private _ws: WebSocket | null = null;
  private _nextId: number = 1;
  private _pending: Map<
    number,
    {
      resolve: (v: CDPResponse) => void;
      reject: (e: Error) => void;
    }
  > = new Map();
  private _targetId: string | null = null;

  constructor(port: number = 9222) {
    this._port = port;
  }

  // ─── Public API ─────────────────────────────────────────────────────────────

  /** Returns true if VS Code is running with CDP enabled on the configured port. */
  async isAvailable(): Promise<boolean> {
    return new Promise((resolve) => {
      const req = http.get(
        `http://localhost:${this._port}/json/list`,
        { timeout: 1500 },
        (res) => {
          resolve(res.statusCode === 200);
          res.resume();
        },
      );
      req.on("error", () => resolve(false));
      req.on("timeout", () => {
        req.destroy();
        resolve(false);
      });
    });
  }

  /**
   * Connects to VS Code's main workbench page via CDP.
   * Must be called before captureClick() or cancelCapture().
   */
  async connect(): Promise<void> {
    const pages = await this._fetchPageList();

    // Find VS Code workbench page (not the extension host, not devtools)
    const workbench =
      pages.find(
        (p) =>
          p.type === "page" &&
          (p.url.includes("vscode-file://vscode-app") ||
            p.url.includes("workbench.html") ||
            p.title.includes("Visual Studio Code")),
      ) ?? pages.find((p) => p.type === "page");

    if (!workbench) {
      throw new Error(
        "CDP: Could not find VS Code workbench page. Is VS Code running with --remote-debugging-port?",
      );
    }

    this._targetId = workbench.id;
    await this._connectWS(workbench.webSocketDebuggerUrl);
  }

  /**
   * Injects a one-shot click listener into the VS Code renderer.
   * Resolves when the user clicks a UI element (or times out after `timeoutMs`).
   *
   * @param timeoutMs Maximum wait time. Default: 30s.
   */
  async captureClick(timeoutMs: number = 30_000): Promise<CapturedElement> {
    if (!this._ws || this._ws.readyState !== WebSocket.OPEN) {
      throw new Error("CDP: Not connected. Call connect() first.");
    }

    // Inject the capture script
    const injectResult = await this._evaluate(CAPTURE_SCRIPT);
    const status = injectResult.result?.result as
      | { value?: string }
      | undefined;

    if (status?.value === "ALREADY_CAPTURING") {
      // Previous capture still running — cancel it first
      await this._evaluate(CANCEL_SCRIPT);
      await this._evaluate(CAPTURE_SCRIPT);
    }

    // Poll for result
    return new Promise((resolve, reject) => {
      const deadline = Date.now() + timeoutMs;

      const poll = async () => {
        if (Date.now() > deadline) {
          await this._evaluate(CANCEL_SCRIPT).catch(() => {});
          reject(new Error("CDP: Capture timed out. No element was clicked."));
          return;
        }

        try {
          const res = await this._evaluate(POLL_RESULT_SCRIPT);
          const value =
            (res.result?.result as { value?: string } | undefined)?.value ??
            "IDLE";

          if (value === "WAITING") {
            setTimeout(poll, 200);
            return;
          }

          if (value === "IDLE") {
            reject(new Error("CDP: Capture was cancelled externally."));
            return;
          }

          resolve(JSON.parse(value) as CapturedElement);
        } catch (err) {
          reject(err);
        }
      };

      setTimeout(poll, 200);
    });
  }

  /** Cancels an in-progress click capture. */
  async cancelCapture(): Promise<void> {
    if (this._ws?.readyState === WebSocket.OPEN) {
      await this._evaluate(CANCEL_SCRIPT).catch(() => {});
    }
  }

  /** Highlights elements matching `selector` in the VS Code UI (flash outline). */
  async highlightSelector(selector: string): Promise<number> {
    if (!this._ws || this._ws.readyState !== WebSocket.OPEN) {
      return 0;
    }

    const script = `(function() {
      try {
        const els = Array.from(document.querySelectorAll(${JSON.stringify(selector)}));
        els.forEach(function(el) {
          const prev = el.style.outline;
          el.style.outline = '2px solid #f00';
          el.style.outlineOffset = '1px';
          setTimeout(function() {
            el.style.outline = prev;
            el.style.outlineOffset = '';
          }, 2000);
        });
        return els.length;
      } catch(e) { return -1; }
    })()`;

    const res = await this._evaluate(script).catch(() => null);
    const count =
      (res?.result?.result as { value?: number } | undefined)?.value ?? 0;
    return count;
  }

  /**
   * Clicks the first element matching `selector` via CDP (simulated MouseEvent).
   * Returns true if an element was found and clicked.
   * Falls back gracefully — if selector empty or element not found, returns false.
   */
  async clickSelector(selector: string): Promise<boolean> {
    if (!selector || !this._ws || this._ws.readyState !== WebSocket.OPEN) {
      return false;
    }

    const script = `(function() {
      try {
        const el = document.querySelector(${JSON.stringify(selector)});
        if (!el) { return false; }
        // Use both focus() and click() to handle both buttons and inputs
        if (typeof el.focus === 'function') { el.focus(); }
        el.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true, view: window }));
        el.dispatchEvent(new MouseEvent('mouseup',   { bubbles: true, cancelable: true, view: window }));
        el.dispatchEvent(new MouseEvent('click',     { bubbles: true, cancelable: true, view: window }));
        return true;
      } catch(e) { return false; }
    })()`;

    const res = await this._evaluate(script).catch(() => null);
    return (
      (res?.result?.result as { value?: boolean } | undefined)?.value === true
    );
  }

  /** Returns the visible text for the first element matching `selector`. */
  async getText(selector: string): Promise<string> {
    if (!selector || !this._ws || this._ws.readyState !== WebSocket.OPEN) {
      return "";
    }

    const script = `(function() {
      try {
        const el = document.querySelector(${JSON.stringify(selector)});
        if (!el) { return ''; }
        return (el.innerText || el.textContent || el.getAttribute('aria-label') || '')
          .replace(/\s+/g, ' ')
          .trim();
      } catch(e) { return ''; }
    })()`;

    const res = await this._evaluate(script).catch(() => null);
    return (
      (res?.result?.result as { value?: string } | undefined)?.value ?? ""
    ).trim();
  }

  /**
   * Clicks the first visible element whose text/aria-label matches one of the
   * provided candidates. Matching is case-insensitive and prefers exact matches.
   */
  async clickByText(candidates: string[]): Promise<boolean> {
    const wanted = candidates
      .map((candidate) => candidate.trim())
      .filter(Boolean);
    if (!wanted.length || !this._ws || this._ws.readyState !== WebSocket.OPEN) {
      return false;
    }

    const script = `(function() {
      try {
        const wanted = ${JSON.stringify(wanted)}
          .map((value) => value.toLowerCase())
          .filter(Boolean);
        const selectors = [
          '[role="option"]',
          '[role="menuitem"]',
          '[role="treeitem"]',
          '.quick-input-list-entry',
          '.monaco-list-row',
          'button',
          'a',
          'div',
          'span'
        ];

        const norm = (value) => (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
        const score = (text) => {
          const haystack = norm(text);
          if (!haystack) { return -1; }
          for (const candidate of wanted) {
            if (haystack === candidate) { return 1000 - haystack.length; }
          }
          for (const candidate of wanted) {
            if (haystack.includes(candidate)) { return 500 - haystack.length; }
          }
          return -1;
        };

        const seen = new Set();
        const hits = [];
        for (const selector of selectors) {
          for (const el of document.querySelectorAll(selector)) {
            if (!(el instanceof HTMLElement)) { continue; }
            if (seen.has(el)) { continue; }
            seen.add(el);

            const rect = el.getBoundingClientRect();
            if (rect.width < 4 || rect.height < 4) { continue; }

            const style = window.getComputedStyle(el);
            if (style.display === 'none' || style.visibility === 'hidden' || style.pointerEvents === 'none') {
              continue;
            }

            const text = el.innerText || el.textContent || el.getAttribute('aria-label') || '';
            const matchScore = score(text);
            if (matchScore < 0) { continue; }
            hits.push({ el, matchScore });
          }
        }

        hits.sort((left, right) => right.matchScore - left.matchScore);
        const target = hits[0]?.el;
        if (!target) { return false; }

        target.focus();
        target.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true, view: window }));
        target.dispatchEvent(new MouseEvent('mouseup',   { bubbles: true, cancelable: true, view: window }));
        target.dispatchEvent(new MouseEvent('click',     { bubbles: true, cancelable: true, view: window }));
        return true;
      } catch (e) {
        return false;
      }
    })()`;

    const res = await this._evaluate(script).catch(() => null);
    return (
      (res?.result?.result as { value?: boolean } | undefined)?.value === true
    );
  }

  /**
   * Sets the value of an input/textarea matching `selector` via CDP.
   * Triggers React synthetic input events so frameworks like Monaco react.
   */
  async typeInSelector(selector: string, text: string): Promise<boolean> {
    if (!selector || !this._ws || this._ws.readyState !== WebSocket.OPEN) {
      return false;
    }

    const escaped = JSON.stringify(text);
    const script = `(function() {
      try {
        const el = document.querySelector(${JSON.stringify(selector)});
        if (!el) { return false; }
        el.focus();
        // For contenteditable / Monaco editors
        if (el.isContentEditable) {
          document.execCommand('selectAll', false);
          document.execCommand('insertText', false, ${escaped});
          return true;
        }
        // For standard inputs/textareas
        const nativeInput = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')
          || Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, 'value');
        if (nativeInput && nativeInput.set) {
          nativeInput.set.call(el, ${escaped});
          el.dispatchEvent(new Event('input', { bubbles: true }));
          el.dispatchEvent(new Event('change', { bubbles: true }));
          return true;
        }
        return false;
      } catch(e) { return false; }
    })()`;

    const res = await this._evaluate(script).catch(() => null);
    return (
      (res?.result?.result as { value?: boolean } | undefined)?.value === true
    );
  }

  /**
   * Evaluates a JavaScript expression in the VS Code renderer and returns
   * the raw string value. Returns an empty string if CDP is not connected or
   * the expression throws. Intended for use by ChatDOMWatcher.
   */
  async evaluate(expression: string): Promise<string> {
    if (!this._ws || this._ws.readyState !== WebSocket.OPEN) {
      return "";
    }
    try {
      const res = await this._evaluate(expression);
      return (
        (res?.result?.result as { value?: unknown } | undefined)?.value ?? ""
      )
        ?.toString()
        .trim();
    } catch {
      return "";
    }
  }

  /** Disconnects from CDP. */
  disconnect(): void {
    if (this._ws) {
      this._ws.close();
      this._ws = null;
    }
    this._pending.forEach((p) => p.reject(new Error("CDP disconnected")));
    this._pending.clear();
  }

  // ─── Internal ────────────────────────────────────────────────────────────────

  private async _fetchPageList(): Promise<CDPPage[]> {
    return new Promise((resolve, reject) => {
      const req = http.get(
        `http://localhost:${this._port}/json/list`,
        { timeout: 3000 },
        (res) => {
          let data = "";
          res.on("data", (c: string) => (data += c));
          res.on("end", () => {
            try {
              resolve(JSON.parse(data) as CDPPage[]);
            } catch (e) {
              reject(e);
            }
          });
        },
      );
      req.on("error", reject);
      req.on("timeout", () => {
        req.destroy();
        reject(new Error("CDP: Timeout fetching page list"));
      });
    });
  }

  private _connectWS(url: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const ws = new WebSocket(url);

      ws.onopen = () => {
        this._ws = ws;
        resolve();
      };

      ws.onerror = (e) =>
        reject(new Error(`CDP WebSocket error: ${String(e)}`));

      ws.onclose = () => {
        this._ws = null;
        this._pending.forEach((p) =>
          p.reject(new Error("CDP: WebSocket closed unexpectedly")),
        );
        this._pending.clear();
      };

      ws.onmessage = (event) => {
        try {
          const msg = JSON.parse(event.data as string) as CDPResponse;
          if (msg.id !== undefined) {
            const pending = this._pending.get(msg.id);
            if (pending) {
              this._pending.delete(msg.id);
              if (msg.error) {
                pending.reject(new Error(`CDP error: ${msg.error.message}`));
              } else {
                pending.resolve(msg);
              }
            }
          }
        } catch {
          /* ignore malformed messages */
        }
      };
    });
  }

  private _evaluate(expression: string): Promise<CDPResponse> {
    return new Promise((resolve, reject) => {
      if (!this._ws || this._ws.readyState !== WebSocket.OPEN) {
        reject(new Error("CDP: WebSocket not open"));
        return;
      }

      const id = this._nextId++;
      this._pending.set(id, { resolve, reject });

      const msg: CDPRequest = {
        id,
        method: "Runtime.evaluate",
        params: {
          expression,
          returnByValue: true,
          awaitPromise: false,
        },
      };

      this._ws.send(JSON.stringify(msg));

      // Timeout individual calls
      setTimeout(() => {
        if (this._pending.has(id)) {
          this._pending.delete(id);
          reject(
            new Error(
              `CDP: evaluate timed out for: ${expression.slice(0, 60)}...`,
            ),
          );
        }
      }, 10_000);
    });
  }
}

// ─── Helper: check if port is in use (to give friendly error messages) ─────────

export async function isPortOpen(port: number): Promise<boolean> {
  return new Promise((resolve) => {
    const socket = new net.Socket();
    socket.setTimeout(1000);
    socket
      .connect(port, "127.0.0.1", () => {
        socket.destroy();
        resolve(true);
      })
      .on("error", () => resolve(false))
      .on("timeout", () => {
        socket.destroy();
        resolve(false);
      });
  });
}
