#!/usr/bin/env python3
"""
actions.py — Inline Action Executors for Smart Monitor
=======================================================
All VS Code interaction code extracted and simplified.
NO dependency on vscode_copilot_monitor.py (v7).

Actions provided:
  - CDP utilities (is_cdp_available, read_chat_via_cdp)
  - VS Code UI (vscode_focus, is_vscode_focused, send, open_new_chat)
  - Model management (get_current_model, get_model_pool, cycle_model_next)
  - System (notify, capture_workbench_screenshot stub)
"""

import hashlib
import json
import logging
import re
import subprocess
import time
import urllib.request
from datetime import datetime
from pathlib import Path
from typing import Optional

logger = logging.getLogger("smart_monitor")

# ─── Paths ─────────────────────────────────────────────────────────────────────
# This file lives at: .prompts/agent/smart_monitor/actions.py
#   -> parent  = smart_monitor/
#   -> parent.parent = .prompts/agent/
#   -> parent.parent.parent = .prompts/
#   -> parent.parent.parent.parent = repo root
_THIS_FILE   = Path(__file__).resolve()
AGENT_DIR    = _THIS_FILE.parent.parent          # .prompts/agent/
REPO_ROOT    = AGENT_DIR.parent.parent           # repo root

CHAT_SNAPSHOT_FILE = AGENT_DIR / "chat_snapshot.txt"
LOOP_PROMPT_FILE   = REPO_ROOT / ".prompts" / "AGENT_LOOP_PROMPT.md"
VSCODE_SETTINGS    = (
    Path.home() / "Library" / "Application Support" / "Code" / "User" / "settings.json"
)
MODEL_CATALOG_FILE = AGENT_DIR / "model_catalog.json"

# ─── CDP ───────────────────────────────────────────────────────────────────────
CDP_HOST = "localhost"
CDP_PORT = 9222

# ─── Error patterns (kept in sync with observer.py) ───────────────────────────
_CHAT_ERROR_PATTERNS = [
    (re.compile(r"rate.limit|429|Too Many Requests|quota.*exhaust", re.I), "rate_limited"),
    (re.compile(r"overloaded|503|502|500|Internal Server Error|capacity", re.I), "hard_error"),
    (re.compile(r"cancelled|canceled", re.I), "cancelled"),
]

# ─── AppleScript / macOS utilities ─────────────────────────────────────────────

def run_applescript(script: str) -> str:
    """Execute an AppleScript. Callers must sanitize dynamic content before passing."""
    r = subprocess.run(["osascript", "-e", script], capture_output=True, text=True)
    return r.stdout.strip()


def notify(title: str, msg: str) -> None:
    """Send a macOS notification. Escapes quotes to prevent AppleScript injection."""
    safe_msg   = msg.replace("\\", "\\\\").replace('"', '\\"')
    safe_title = title.replace("\\", "\\\\").replace('"', '\\"')
    run_applescript(f'display notification "{safe_msg}" with title "{safe_title}"')


def vscode_focus() -> None:
    """Bring VS Code to the foreground."""
    run_applescript('tell application "Visual Studio Code" to activate')
    time.sleep(0.5)


def is_vscode_focused() -> bool:
    """Return True if VS Code is currently the frontmost application."""
    try:
        result = run_applescript(
            'tell application "System Events" to get name of '
            "first application process whose frontmost is true"
        )
        return "Code" in result
    except Exception:
        return True  # assume focused on error


# ─── CDP utilities ─────────────────────────────────────────────────────────────

def is_cdp_available() -> bool:
    """Check if VS Code's CDP debug port is listening."""
    try:
        url = f"http://{CDP_HOST}:{CDP_PORT}/json/version"
        with urllib.request.urlopen(url, timeout=3) as resp:
            data = json.loads(resp.read())
            return bool(data.get("webSocketDebuggerUrl") or data.get("Browser"))
    except Exception:
        return False


def read_chat_via_cdp() -> Optional[str]:
    """Read visible Copilot Chat text via CDP + Playwright. Returns text or None."""
    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        return None

    try:
        with sync_playwright() as p:
            browser = p.chromium.connect_over_cdp(f"http://{CDP_HOST}:{CDP_PORT}")
            try:
                page = None
                for ctx in browser.contexts:
                    for pg in ctx.pages:
                        if any(kw in pg.url for kw in ("workbench", "copilot", "chat")):
                            page = pg
                            break
                    if page:
                        break

                if page is None and browser.contexts and browser.contexts[0].pages:
                    page = browser.contexts[0].pages[0]

                if page is None:
                    return None

                selectors = [
                    ".value .rendered-markdown",
                    ".chat-request-part",
                    ".interactive-item-container",
                    ".chat-list-item",
                ]
                all_texts: list[str] = []
                for sel in selectors:
                    try:
                        for el in page.query_selector_all(sel):
                            txt = el.inner_text().strip()
                            if txt:
                                all_texts.append(txt)
                    except Exception:
                        pass
                return "\n".join(all_texts) if all_texts else None
            finally:
                browser.close()
    except Exception as e:
        logger.debug(f"read_chat_via_cdp error: {e}")
        return None


def _text_hash(text: str) -> str:
    return hashlib.md5(text.encode("utf-8")).hexdigest()


def classify_chat_text(current_text: str, prev_hash: str) -> tuple:
    """Returns (event_type, has_changed). Mirrors v7's classify_chat_text."""
    current_hash = _text_hash(current_text)
    has_changed = current_hash != prev_hash

    tail = current_text[-500:] if len(current_text) > 500 else current_text
    for pattern, event_type in _CHAT_ERROR_PATTERNS:
        if pattern.search(tail):
            return event_type, has_changed

    return ("success" if has_changed else "unknown"), has_changed


# ─── Model management ───────────────────────────────────────────────────────────

def get_current_model() -> str:
    """Read the active model ID from VS Code settings.json."""
    try:
        raw = VSCODE_SETTINGS.read_text(encoding="utf-8")
        m = re.search(r'"github\.copilot\.chat\.languageModel"\s*:\s*"([^"]+)"', raw)
        return m.group(1) if m else ""
    except Exception:
        return ""


def get_model_pool() -> list:
    """Return ordered model IDs from model_catalog.json. Empty list if unavailable."""
    try:
        catalog = json.loads(MODEL_CATALOG_FILE.read_text(encoding="utf-8"))
        ordered = catalog.get("ordered_ids", [])
        if ordered:
            return list(ordered)
    except Exception:
        pass
    return []


def _write_model_to_settings(model_id: str) -> bool:
    """
    Write model_id to VS Code settings.json.
    Only accepts IDs matching [a-z0-9][a-z0-9.\\-]+ to prevent injection.
    """
    if not model_id or not re.fullmatch(r"[a-z0-9][a-z0-9.\-]+", model_id):
        logger.warning(f"_write_model_to_settings: invalid id '{model_id}'")
        return False
    try:
        raw = VSCODE_SETTINGS.read_text(encoding="utf-8")
        pattern = r'"github\.copilot\.chat\.languageModel"\s*:\s*"[^"]*"'
        replacement = f'"github.copilot.chat.languageModel": "{model_id}"'
        if re.search(pattern, raw):
            new_raw = re.sub(pattern, replacement, raw)
        else:
            # Setting not present yet — insert after opening brace
            new_raw = raw.replace("{", '{\n  ' + replacement + ',', 1)
        VSCODE_SETTINGS.write_text(new_raw, encoding="utf-8")
        logger.info(f"Model written to settings.json: {model_id}")
        return True
    except Exception as e:
        logger.error(f"_write_model_to_settings error: {e}")
        return False


def cycle_model_next(state: dict) -> str:
    """
    Cycle to the next model using state['model_index'] (index-based, no DOM).
    Fixes the v7 slug-mismatch bug where DOM label ≠ catalog ID.

    Updates state in-place.
    Returns the new model ID, or current if pool is empty.
    """
    pool = get_model_pool()
    if not pool:
        logger.warning("cycle_model_next: empty pool — cannot cycle")
        return state.get("current_model", "")

    # Use state index as ground truth (not DOM, which is unreliable)
    current_idx = state.get("model_index", 0)
    if not (0 <= current_idx < len(pool)):
        current_idx = 0

    new_idx   = (current_idx + 1) % len(pool)
    new_model = pool[new_idx]

    logger.info(
        f"cycle_model_next: {pool[current_idx]} → {new_model} "
        f"(index {current_idx} → {new_idx} of {len(pool)})"
    )

    if _write_model_to_settings(new_model):
        state["current_model"] = new_model
        state["model_index"]   = new_idx
        return new_model

    # settings write failed — return current model unchanged
    return state.get("current_model", pool[current_idx])


# ─── Snapshot freshness guard ───────────────────────────────────────────────────

def _snapshot_is_fresh(max_age_secs: float = 3.0) -> bool:
    """
    Return True if chat_snapshot.txt was modified within max_age_secs.
    Used as an abort guard: if the snapshot is fresh, the agent is actively
    processing and we should NOT inject keystrokes or send messages.
    """
    try:
        if CHAT_SNAPSHOT_FILE.exists():
            age = time.time() - CHAT_SNAPSHOT_FILE.stat().st_mtime
            return age < max_age_secs
    except Exception:
        pass
    return False


# ─── CDP async helpers ─────────────────────────────────────────────────────────

async def _cdp_connect_page():
    """Connect via CDP and return (playwright, browser, page). Caller must close."""
    from playwright.async_api import async_playwright
    playwright = await async_playwright().start()
    browser = None
    try:
        browser = await playwright.chromium.connect_over_cdp(
            f"http://{CDP_HOST}:{CDP_PORT}"
        )
        page = None
        for ctx in browser.contexts:
            for pg in ctx.pages:
                if any(t in pg.url for t in ("workbench", "copilot", "chat")):
                    page = pg
                    break
            if page:
                break

        if page is None and browser.contexts and browser.contexts[0].pages:
            page = browser.contexts[0].pages[0]

        if page is not None:
            try:
                await page.bring_to_front()
            except Exception:
                pass

        return playwright, browser, page
    except Exception:
        if browser is not None:
            await browser.close()
        await playwright.stop()
        raise


async def _cdp_close(playwright, browser) -> None:
    try:
        if browser is not None:
            await browser.close()
    finally:
        if playwright is not None:
            await playwright.stop()


# ─── Send message to chat ───────────────────────────────────────────────────────

def _vscode_send_to_chat(message: str) -> bool:
    """
    Send a message to the active Copilot Chat input.

    1. Abort guard: skip if snapshot is fresh (agent still actively generating)
    2. Focus VS Code
    3. Try CDP (Playwright): click chat textarea → clear → insert_text → Enter
    4. Fallback: pbcopy + AppleScript keystroke

    Returns True if the message was sent (best effort).
    """
    # Safety guard: don't send while agent is generating
    if _snapshot_is_fresh(3.0):
        logger.warning(
            "_vscode_send_to_chat: chat snapshot updated <3s ago — agent active, aborting"
        )
        return False

    vscode_focus()

    # ── Try CDP ────────────────────────────────────────────────────────────────
    if is_cdp_available():
        try:
            import asyncio as _asyncio

            async def _do_send():
                playwright, browser, page = await _cdp_connect_page()
                try:
                    if not page:
                        return False

                    # Chat textarea selectors (in priority order)
                    input_sel = (
                        'div[class*="interactive-input-box"] textarea, '
                        'div[class*="chat-input"] textarea, '
                        'div[class*="interactive-input-part"] textarea, '
                        '.chat-widget textarea'
                    )
                    try:
                        el = await page.wait_for_selector(input_sel, timeout=2000)
                    except Exception:
                        el = None

                    if el:
                        await el.click()
                        await page.keyboard.press("Meta+A")
                        await page.keyboard.press("Backspace")
                        await page.keyboard.insert_text(message)
                        await page.keyboard.press("Enter")
                        return True

                    return False
                finally:
                    await _cdp_close(playwright, browser)

            if _asyncio.run(_do_send()):
                logger.info(f"Message sent via CDP ({len(message)} chars)")
                return True
        except Exception as e:
            logger.debug(f"CDP send error: {e}")

    # ── Fallback: pbcopy + AppleScript ─────────────────────────────────────────
    try:
        # Try to focus the chat input first via keyboard shortcut
        run_applescript("""
            tell application "System Events"
                tell process "Code"
                    keystroke "l" using {command down}
                    delay 0.5
                end tell
            end tell
        """)
        subprocess.run(["pbcopy"], input=message.encode("utf-8"), check=True)
        time.sleep(0.2)
        run_applescript("""
            tell application "System Events"
                tell process "Code"
                    keystroke "v" using {command down}
                    delay 0.5
                    key code 36
                end tell
            end tell
        """)
        logger.info(f"Message sent via AppleScript ({len(message)} chars)")
        return True
    except Exception as e:
        logger.error(f"AppleScript send error: {e}")
        return False


def vscode_send_continue() -> bool:
    """Send 'continuar' to the active chat (for resuming after errors/stall)."""
    return _vscode_send_to_chat("continuar")


def send_loop_prompt() -> bool:
    """Send AGENT_LOOP_PROMPT.md content to the active chat."""
    try:
        prompt_text = LOOP_PROMPT_FILE.read_text(encoding="utf-8").strip()
    except Exception:
        prompt_text = "continuar con las tareas pendientes del sprint actual"
    return _vscode_send_to_chat(prompt_text)


# ─── Open new chat ──────────────────────────────────────────────────────────────

def vscode_open_new_chat() -> bool:
    """
    Open a new Copilot Chat window.

    1. Abort guard: skip if snapshot is fresh
    2. Focus VS Code
    3. Try CDP: click '+ New Chat' button via DOM selector
    4. Fallback: Command Palette → 'Chat: New Chat'
    """
    vscode_focus()
    time.sleep(0.3)

    if _snapshot_is_fresh(3.0):
        logger.warning("vscode_open_new_chat: snapshot fresh — agent active, aborting")
        return False

    # ── Try CDP ────────────────────────────────────────────────────────────────
    if is_cdp_available():
        try:
            import asyncio as _asyncio

            async def _do_open():
                playwright, browser, page = await _cdp_connect_page()
                try:
                    if not page:
                        return False

                    # Click '+' (New Chat) button — must NOT have aria-haspopup
                    btn = await page.evaluate(r"""() => {
                        const sels = [
                            '.part.auxiliarybar a[aria-label="New Chat (\u2318N)"]',
                            '.auxiliarybar a[aria-label="New Chat (\u2318N)"]',
                            'a[aria-label^="New Chat"]',
                        ];
                        for (const sel of sels) {
                            for (const el of document.querySelectorAll(sel)) {
                                const r = el.getBoundingClientRect();
                                if (r.width > 0 && !el.getAttribute('aria-haspopup')) {
                                    return {
                                        x: Math.round(r.x + r.width / 2),
                                        y: Math.round(r.y + r.height / 2),
                                    };
                                }
                            }
                        }
                        return null;
                    }""")

                    if btn:
                        import asyncio
                        await page.mouse.click(btn["x"], btn["y"])
                        await asyncio.sleep(1.0)
                        return True

                    return False
                finally:
                    await _cdp_close(playwright, browser)

            if _asyncio.run(_do_open()):
                logger.info("New chat opened via CDP")
                time.sleep(0.5)
                return True
        except Exception as e:
            logger.debug(f"CDP open_new_chat error: {e}")

    # ── Fallback: Command Palette ───────────────────────────────────────────────
    run_applescript("""
        tell application "System Events"
            tell process "Code"
                keystroke "p" using {command down, shift down}
                delay 0.8
                keystroke "Chat: New Chat"
                delay 0.5
                key code 36
                delay 2.0
            end tell
        end tell
    """)
    logger.info("New chat opened via Command Palette")
    return True


# ─── Stop current response ──────────────────────────────────────────────────────

def stop_current_response() -> bool:
    """
    Click the Stop button in the chat.

    1. Try CDP: look for known Stop button selectors
    2. Fallback: Escape key via AppleScript
    """
    if is_cdp_available():
        try:
            import asyncio as _asyncio

            async def _do_stop():
                playwright, browser, page = await _cdp_connect_page()
                try:
                    if not page:
                        return False

                    stop_selectors = [
                        'button[aria-label="Stop Response"]',
                        'button[aria-label="Stop"]',
                        '.interactive-stop-button',
                        'button[title*="Stop" i]',
                        'a[aria-label*="Stop" i]',
                    ]
                    for sel in stop_selectors:
                        try:
                            btn = await page.query_selector(sel)
                            if btn:
                                box = await btn.bounding_box()
                                if box:
                                    await page.mouse.click(
                                        box["x"] + box["width"] / 2,
                                        box["y"] + box["height"] / 2,
                                    )
                                    return True
                        except Exception:
                            continue
                    return False
                finally:
                    await _cdp_close(playwright, browser)

            ok = _asyncio.run(_do_stop())
            if ok:
                time.sleep(1.0)
                return True
        except Exception as e:
            logger.debug(f"CDP stop_current_response error: {e}")

    # Fallback: Escape
    vscode_focus()
    run_applescript("""
        tell application "System Events"
            tell process "Code"
                key code 53
                delay 0.5
            end tell
        end tell
    """)
    return False


def open_new_chat_with_stop() -> bool:
    """
    Full context-full recovery:
      1. Stop current response
      2. Open new chat
      3. Send AGENT_LOOP_PROMPT if chat is no longer freshly active
    """
    if _snapshot_is_fresh(3.0):
        logger.warning("open_new_chat_with_stop: snapshot fresh — agent active, aborting")
        return False

    notify("OKLA Smart Monitor", "Contexto lleno → deteniendo y abriendo nuevo chat")

    stopped = stop_current_response()
    logger.info(
        f"stop_current_response: {'stopped' if stopped else 'no effect (may have already finished)'}"
    )
    time.sleep(1.5)

    if _snapshot_is_fresh(3.0):
        logger.warning("open_new_chat_with_stop: snapshot changed after stop — aborting")
        return False

    ok = vscode_open_new_chat()
    if not ok:
        return False

    # Only send loop prompt if no recent agent activity
    if _snapshot_is_fresh(3.0):
        logger.info("open_new_chat_with_stop: snapshot fresh after open — skipping loop prompt")
        return True

    send_loop_prompt()
    return True


# ─── Screenshot stub ────────────────────────────────────────────────────────────

def capture_workbench_screenshot(label: str = "manual", extra: dict = None) -> str:
    """
    Stub implementation — screenshots not captured in standalone agent.
    v7 dependency removed; re-implement with async CDP if needed.
    Returns empty string (no-op).
    """
    return ""
