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
# model_catalog.json: secondary fallback for get_model_pool() when state.vscdb is unavailable
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


# ─── VS Code command execution (URL protocol + CLI) ────────────────────────────
#
# KEY TECHNIQUES FOR COPILOT CHAT CONTROL:
#   1. vscode://command/<id>  — executes any VS Code command, no focus required
#   2. code chat "msg" --reuse-window  — sends prompt to existing window's chat
#   3. AppleScript key code  — keyboard shortcuts (requires focus first)
#
# VS Code Chat keyboard shortcuts (macOS, mapped to key codes):
#   Ctrl+Cmd+I   → open/focus Chat panel     (key 34 = i,  ctrl+cmd)
#   Cmd+Shift+I  → open Chat in Agent mode   (key 34 = i,  shift+cmd)
#   Cmd+ESC      → stop generation           (key 53 = ESC, cmd)
#   Cmd+N        → new chat editor in context (key 45 = n,  cmd)
#   Enter        → submit message            (key 36)

# Known VS Code Copilot Chat command IDs
VSCODE_CMD_CHAT_OPEN        = "workbench.action.chat.open"
VSCODE_CMD_CHAT_NEW         = "workbench.action.chat.newChat"
VSCODE_CMD_CHAT_STOP        = "workbench.action.chat.stop"
VSCODE_CMD_CHAT_FOCUS_INPUT = "workbench.action.chat.focusInput"

# AppleScript key codes for macOS
_KEY_ESC   = 53
_KEY_I     = 34
_KEY_N     = 45
_KEY_ENTER = 36


def vscode_exec_command(command_id: str) -> bool:
    """
    Execute a VS Code command via the vscode:// URL protocol.
    No window focus required — works even if VS Code is in the background.
    Returns True if the URL was dispatched (command may still be async).
    """
    try:
        r = subprocess.run(
            ["open", f"vscode://command/{command_id}"],
            capture_output=True, text=True, timeout=5,
        )
        time.sleep(0.3)
        return r.returncode == 0
    except Exception as e:
        logger.debug(f"vscode_exec_command({command_id}) error: {e}")
        return False


def vscode_cli_chat(message: str, mode: str = "agent") -> bool:
    """
    Send a message to VS Code Copilot Chat via the 'code chat' CLI subcommand.
    Uses --reuse-window to target the existing VS Code instance.
    No window focus required.
    Returns True if the CLI exited successfully.
    """
    try:
        r = subprocess.run(
            ["code", "chat", "--mode", mode, "--reuse-window", message],
            capture_output=True, text=True, cwd=str(REPO_ROOT), timeout=20,
        )
        if r.returncode != 0:
            logger.debug(f"vscode_cli_chat failed (rc={r.returncode}): {r.stderr[:200]}")
        return r.returncode == 0
    except Exception as e:
        logger.debug(f"vscode_cli_chat error: {e}")
        return False


def _vscode_keystroke(key_code: int, modifiers: list) -> None:
    """
    Send a key code + modifiers to VS Code via AppleScript System Events.
    REQUIRES VS Code to already be the frontmost application.
    modifiers: list of strings — "command", "control", "option", "shift"
    """
    if modifiers:
        mod_str = "{" + ", ".join(f"{m} down" for m in modifiers) + "}"
        script = (
            f'tell application "System Events" to tell process "Code"\n'
            f'    key code {key_code} using {mod_str}\n'
            f'end tell'
        )
    else:
        script = (
            f'tell application "System Events" to tell process "Code"\n'
            f'    key code {key_code}\n'
            f'end tell'
        )
    run_applescript(script)


def ensure_vscode_focused() -> bool:
    """Activate VS Code and wait for it to become frontmost. Returns True when focused."""
    run_applescript('tell application "Visual Studio Code" to activate')
    time.sleep(0.45)
    return is_vscode_focused()


def ensure_chat_focused() -> bool:
    """
    Full focus recovery chain:
      1. Activate VS Code (bring to front)
      2. Open/focus the Chat panel via vscode:// URL (equivalent to Ctrl+Cmd+I)
      3. Focus the chat input box
    Returns True if VS Code is now in the foreground (best-effort chat focus).
    """
    ensure_vscode_focused()
    vscode_exec_command(VSCODE_CMD_CHAT_OPEN)        # open / bring-to-front chat panel
    time.sleep(0.5)
    vscode_exec_command(VSCODE_CMD_CHAT_FOCUS_INPUT) # focus the text input
    time.sleep(0.3)
    return is_vscode_focused()


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


# ─── Model management (code chat CLI model) ────────────────────────────────────
#
# `code chat` uses the model stored at key "chat.currentLanguageModel.panel"
# in VS Code's SQLite state database (~/.vscode/User/globalStorage/state.vscdb).
# Format: "copilot/<model-family>" e.g. "copilot/claude-sonnet-4.6"
#
# The available pool comes from "chatModelRecentlyUsed" in the same DB.
# This is the CORRECT rotation mechanism — settings.json has no effect on
# which model the code chat CLI session uses.

_VSCODE_STATE_DB = (
    Path.home()
    / "Library" / "Application Support" / "Code" / "User"
    / "globalStorage" / "state.vscdb"
)

# Validation: only accept model IDs of the form vendor/family (e.g. copilot/gpt-5.4)
_MODEL_ID_RE = re.compile(r'^[a-zA-Z0-9][\w.\-]*/[\w.\-]+$')

# Built-in fallback pool if the state DB is unavailable
_FALLBACK_POOL = [
    "copilot/claude-sonnet-4.6",
    "copilot/gpt-5.4",
    "copilot/claude-opus-4.6",
    "copilot/claude-haiku-4.5",
]


def _state_db_query(sql: str, params: tuple = ()) -> Optional[str]:
    """Execute a read-only SQL query against the VS Code state DB. Returns first column of first row."""
    try:
        import sqlite3 as _sqlite3
        with _sqlite3.connect(str(_VSCODE_STATE_DB), timeout=5.0) as con:
            cur = con.execute(sql, params)
            row = cur.fetchone()
            return row[0] if row else None
    except Exception as e:
        logger.debug(f"_state_db_query error: {e}")
        return None


def _state_db_write(sql: str, params: tuple = ()) -> bool:
    """Execute a write SQL statement against the VS Code state DB."""
    try:
        import sqlite3 as _sqlite3
        with _sqlite3.connect(str(_VSCODE_STATE_DB), timeout=5.0) as con:
            con.execute("PRAGMA journal_mode=DELETE;")
            con.execute(sql, params)
            con.commit()
        return True
    except Exception as e:
        logger.error(f"_state_db_write error: {e}")
        return False


def get_current_model() -> str:
    """
    Return the model currently selected for 'code chat' CLI sessions.
    Reads 'chat.currentLanguageModel.panel' from the VS Code state DB.
    Format: 'copilot/claude-sonnet-4.6'
    """
    val = _state_db_query(
        "SELECT value FROM ItemTable WHERE key='chat.currentLanguageModel.panel';"
    )
    return val or ""


def get_model_pool() -> list:
    """
    Return the ordered list of model IDs available for 'code chat' CLI sessions.
    Reads 'chatModelRecentlyUsed' from the VS Code state DB.
    Falls back to model_catalog.json ordered_ids, then to _FALLBACK_POOL.
    """
    # Primary: VS Code state DB (most accurate — reflects what the user has access to)
    raw = _state_db_query("SELECT value FROM ItemTable WHERE key='chatModelRecentlyUsed';")
    if raw:
        try:
            pool = json.loads(raw)
            if isinstance(pool, list) and pool:
                # Only keep valid copilot/* model IDs
                valid = [m for m in pool if _MODEL_ID_RE.match(str(m))]
                if valid:
                    return valid
        except Exception:
            pass

    # Secondary: model_catalog.json (ordered_ids may be populated by older discovery)
    try:
        catalog = json.loads(MODEL_CATALOG_FILE.read_text(encoding="utf-8"))
        ordered = catalog.get("ordered_ids", [])
        if ordered:
            logger.debug("get_model_pool: using model_catalog.json fallback")
            return list(ordered)
    except Exception:
        pass

    logger.debug("get_model_pool: using built-in fallback pool")
    return list(_FALLBACK_POOL)


def cycle_model_next(state: dict) -> str:
    """
    Rotate the model used by 'code chat' CLI sessions.

    1. Reads current pool from VS Code state DB (chatModelRecentlyUsed).
    2. Picks the next model in the pool (index-based, no DOM dependency).
    3. Writes the new model to 'chat.currentLanguageModel.panel' in state.vscdb.
       This is the ONLY key that controls which model code chat uses.

    Returns the new model ID string, or '' on failure.
    """
    pool = get_model_pool()
    if not pool:
        logger.warning("cycle_model_next: empty pool — cannot rotate model")
        return state.get("current_model", "")

    current_idx = state.get("model_index", 0)
    if not (0 <= current_idx < len(pool)):
        current_idx = 0

    new_idx   = (current_idx + 1) % len(pool)
    new_model = pool[new_idx]

    if not _MODEL_ID_RE.match(new_model):
        logger.warning(f"cycle_model_next: skipping invalid model id '{new_model}'")
        return state.get("current_model", pool[current_idx])

    logger.info(
        f"cycle_model_next: {pool[current_idx]} → {new_model} "
        f"(index {current_idx} → {new_idx} of {len(pool)})"
    )

    # Write to state.vscdb — this is what code chat reads at session start
    ok = _state_db_write(
        "UPDATE ItemTable SET value=? WHERE key='chat.currentLanguageModel.panel';",
        (new_model,),
    )
    if not ok:
        logger.warning("cycle_model_next: DB write failed — model not changed")
        return state.get("current_model", pool[current_idx])

    state["current_model"] = new_model
    state["model_index"]   = new_idx
    logger.info(f"cycle_model_next: wrote '{new_model}' to state.vscdb → code chat will use it on next session")
    return new_model


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

_CHAT_TEXTAREA_SELECTORS = (
    'div[class*="interactive-input-box"] textarea, '
    'div[class*="chat-input"] textarea, '
    'div[class*="interactive-input-part"] textarea, '
    '.chat-widget textarea'
)


def _vscode_send_to_chat(message: str) -> bool:
    """
    Send a message to the active VS Code Copilot Chat.

    Tier 1 — code chat CLI (--reuse-window): no focus required, most reliable.
    Tier 2 — CDP Playwright: click textarea → clear → insert_text → Enter.
    Tier 3 — AppleScript keyboard: focus chat input → pbcopy → paste → Enter.

    Returns True if the message was dispatched.
    """
    if _snapshot_is_fresh(3.0):
        logger.warning("_vscode_send_to_chat: snapshot fresh — agent active, aborting")
        return False

    # ── Tier 1: code chat CLI ─────────────────────────────────────────────────
    if vscode_cli_chat(message):
        logger.info(f"_vscode_send_to_chat: sent via code chat CLI ({len(message)} chars)")
        return True

    # ── Tier 2: CDP Playwright ────────────────────────────────────────────────
    if is_cdp_available():
        try:
            import asyncio as _asyncio

            async def _do_send():
                playwright, browser, page = await _cdp_connect_page()
                try:
                    if not page:
                        return False
                    try:
                        el = await page.wait_for_selector(_CHAT_TEXTAREA_SELECTORS, timeout=2000)
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
                logger.info(f"_vscode_send_to_chat: sent via CDP ({len(message)} chars)")
                return True
        except Exception as e:
            logger.debug(f"CDP send error: {e}")

    # ── Tier 3: AppleScript focus + pbcopy + paste + Enter ────────────────────
    try:
        ensure_chat_focused()
        time.sleep(0.3)
        subprocess.run(["pbcopy"], input=message.encode("utf-8"), check=True)
        time.sleep(0.2)
        run_applescript(
            'tell application "System Events" to tell process "Code"\n'
            '    keystroke "v" using {command down}\n'
            '    delay 0.3\n'
            f'    key code {_KEY_ENTER}\n'
            'end tell'
        )
        logger.info(f"_vscode_send_to_chat: sent via AppleScript ({len(message)} chars)")
        return True
    except Exception as e:
        logger.error(f"_vscode_send_to_chat AppleScript error: {e}")
        return False


def vscode_send_continue() -> bool:
    """Send 'continuar' to the active chat (resume after stall or error)."""
    return _vscode_send_to_chat("continuar")


def send_loop_prompt() -> bool:
    """Send AGENT_LOOP_PROMPT.md content to the active chat."""
    try:
        prompt_text = LOOP_PROMPT_FILE.read_text(encoding="utf-8").strip()
    except Exception:
        prompt_text = "continuar con las tareas pendientes del sprint actual"
    return _vscode_send_to_chat(prompt_text)


# ─── Stop generation ────────────────────────────────────────────────────────────

def stop_current_response() -> bool:
    """
    Stop the current Copilot Chat generation.

    Primary:  vscode://command/workbench.action.chat.stop  (no focus required)
    Fallback: Cmd+ESC keyboard shortcut  (requires chat focus — key code 53)

    Returns True if the stop command was dispatched.
    """
    # Primary: URL protocol command — most reliable, no focus dependency
    if vscode_exec_command(VSCODE_CMD_CHAT_STOP):
        logger.info("stop_current_response: dispatched via vscode:// URL protocol")
        time.sleep(0.6)
        return True

    # Fallback: Cmd+ESC keyboard (user-confirmed shortcut)
    logger.debug("stop_current_response: URL command failed — falling back to Cmd+ESC")
    ensure_chat_focused()
    time.sleep(0.3)
    _vscode_keystroke(_KEY_ESC, ["command"])   # Cmd+ESC = stop generation
    time.sleep(0.5)
    return True


# ─── Open new chat ──────────────────────────────────────────────────────────────

def vscode_open_new_chat() -> bool:
    """
    Open a new Copilot Chat session.

    Primary:  vscode://command/workbench.action.chat.newChat  (no focus required)
    Fallback: Cmd+N keyboard shortcut while chat panel is focused (key code 45)

    Returns True if the new-chat command was dispatched.
    """
    if _snapshot_is_fresh(3.0):
        logger.warning("vscode_open_new_chat: snapshot fresh — agent active, aborting")
        return False

    # Primary: URL protocol
    if vscode_exec_command(VSCODE_CMD_CHAT_NEW):
        logger.info("vscode_open_new_chat: opened via vscode:// URL protocol")
        time.sleep(1.0)
        return True

    # Fallback: Focus chat panel then Cmd+N (new chat editor shortcut)
    ensure_chat_focused()
    time.sleep(0.3)
    _vscode_keystroke(_KEY_N, ["command"])   # Cmd+N = new chat editor
    time.sleep(1.0)
    logger.info("vscode_open_new_chat: opened via Cmd+N keyboard fallback")
    return True


# ─── Full recovery: stop + new chat + re-prompt ─────────────────────────────────

def open_new_chat_with_stop() -> bool:
    """
    Full context-recovery sequence:
      1. Abort if chat snapshot is still fresh (agent generating — don't interrupt)
      2. Stop current response (vscode:// command → Cmd+ESC fallback)
      3. Open new chat session (vscode:// command → Cmd+N fallback)
      4. Send AGENT_LOOP_PROMPT to resume the sprint loop

    Returns True if the new chat was opened successfully.
    """
    if _snapshot_is_fresh(3.0):
        logger.warning("open_new_chat_with_stop: snapshot fresh — aborting")
        return False

    notify("OKLA Smart Monitor", "Contexto lleno → deteniendo y abriendo nuevo chat")

    stopped = stop_current_response()
    logger.info(
        f"open_new_chat_with_stop: stop = {'ok' if stopped else 'no-op (already idle)'}"
    )
    time.sleep(1.5)

    if _snapshot_is_fresh(3.0):
        logger.warning("open_new_chat_with_stop: agent became active after stop — aborting")
        return False

    if not vscode_open_new_chat():
        return False

    if _snapshot_is_fresh(3.0):
        logger.info("open_new_chat_with_stop: snapshot fresh after new-chat — skipping loop prompt")
        return True

    send_loop_prompt()
    return True


# ─── Chat UI model rotation: click the VS Code chat model selector ────────────
#
# Triggered when the chat UI shows a rate-limit / hard-error / "switch model" message.
# This rotates the model IN THE CURRENT OPEN CHAT SESSION — no new chat needed.
#
# Different from cycle_model_next() (state.vscdb write → takes effect on NEXT session).
#
# Tier 1: CDP Playwright — find the model selector button → click → type model name
#         into the VS Code QuickPick → Enter
# Tier 2: vscode://command/workbench.action.chat.changeModel → type model name → Enter
# Tier 3: AppleScript accessibility — scan VS Code window buttons by label → click → Enter

# CSS selectors for the VS Code chat model selector button (tried in order)
_CHAT_MODEL_BTN_SELECTORS = [
    'button[aria-label*="Select model" i]',
    'button[aria-label*="Change model" i]',
    'button[aria-label*="model" i][class*="action"]',
    '[class*="interactive-input"] button[class*="model"]',
    '[class*="chat-input"] button[class*="model"]',
    '[class*="chatInput"] button[class*="model"]',
    'button[aria-label*="Claude" i]',
    'button[aria-label*="GPT" i]',
    'button[aria-label*="Sonnet" i]',
    'button[aria-label*="Haiku" i]',
    'button[aria-label*="Opus" i]',
]

VSCODE_CMD_CHAT_CHANGE_MODEL = "workbench.action.chat.changeModel"


def _quickpick_select(model_family: str) -> bool:
    """
    After a VS Code QuickPick is open, type the model family name to filter
    and press Enter to select it.
    Uses pbcopy + Cmd+V to avoid char-by-char key code issues.
    """
    try:
        ensure_vscode_focused()
        time.sleep(0.3)
        subprocess.run(["pbcopy"], input=model_family.encode("utf-8"), check=True)
        run_applescript(
            'tell application "System Events" to tell process "Code"\n'
            '    keystroke "v" using {command down}\n'
            '    delay 0.5\n'
            f'    key code {_KEY_ENTER}\n'
            'end tell'
        )
        return True
    except Exception as e:
        logger.debug(f"_quickpick_select error: {e}")
        return False


def cycle_chat_ui_model(state: dict) -> str:
    """
    Rotate the model in the CURRENT VS Code chat session by interacting with the UI.

    Use case: VS Code chat shows a rate-limit error, a hard error, or a
    "switch to a different model" message.  This switches models WITHOUT
    opening a new chat session — the new model is active immediately.

    Model pool and index are tracked on state['chat_ui_model_index'], which is
    independent from the CLI-rotation index used by cycle_model_next().

    Tiers:
      1. CDP: find the model selector button in the chat panel → click it →
         type the model family name into the opened QuickPick → Enter
      2. vscode:// command → QuickPick filter keyboard interaction
         (fallback when CDP unavailable or button not found)
      3. AppleScript accessibility → scan window buttons by aria description → click

    Returns the new model ID (e.g. 'copilot/claude-opus-4.6') or '' on failure.
    """
    pool = get_model_pool()
    if not pool:
        logger.warning("cycle_chat_ui_model: empty pool")
        return ""

    current_idx = state.get("chat_ui_model_index", 0)
    if not (0 <= current_idx < len(pool)):
        current_idx = 0

    new_idx   = (current_idx + 1) % len(pool)
    new_model = pool[new_idx]

    if not _MODEL_ID_RE.match(new_model):
        logger.warning(f"cycle_chat_ui_model: invalid model id '{new_model}'")
        return ""

    # Extract the family part: "copilot/claude-sonnet-4.6" → "claude-sonnet-4.6"
    model_family = new_model.split("/", 1)[-1] if "/" in new_model else new_model

    logger.info(
        f"cycle_chat_ui_model: {pool[current_idx]} → {new_model} "
        f"(index {current_idx}→{new_idx}, family='{model_family}')"
    )

    success = False

    # ── Tier 1: CDP — find model selector button → click → QuickPick filter ──
    if is_cdp_available():
        try:
            import asyncio as _asyncio

            async def _cdp_click_model():
                import asyncio
                playwright, browser, page = await _cdp_connect_page()
                try:
                    if page is None:
                        return False
                    for sel in _CHAT_MODEL_BTN_SELECTORS:
                        try:
                            btn = await page.wait_for_selector(sel, timeout=1200)
                            if btn:
                                await btn.click()
                                await asyncio.sleep(0.8)
                                # QuickPick filter: type model family, then Enter
                                await page.keyboard.insert_text(model_family)
                                await asyncio.sleep(0.4)
                                await page.keyboard.press("Enter")
                                return True
                        except Exception:
                            continue
                    return False
                finally:
                    await _cdp_close(playwright, browser)

            if _asyncio.run(_cdp_click_model()):
                success = True
                logger.info("cycle_chat_ui_model: ✅ via CDP click → QuickPick filter")
        except Exception as e:
            logger.debug(f"cycle_chat_ui_model CDP error: {e}")

    # ── Tier 2: vscode:// command → QuickPick keyboard interaction ────────────
    if not success:
        dispatched = vscode_exec_command(VSCODE_CMD_CHAT_CHANGE_MODEL)
        if dispatched:
            time.sleep(0.9)   # wait for QuickPick to open
            if _quickpick_select(model_family):
                success = True
                logger.info("cycle_chat_ui_model: ✅ via vscode:// command → QuickPick filter")

    # ── Tier 3: AppleScript accessibility → find button by label ─────────────
    if not success:
        try:
            ensure_vscode_focused()
            time.sleep(0.4)
            as_script = (
                'tell application "System Events" to tell process "Code"\n'
                '    set win to first window whose subrole is "AXStandardWindow"\n'
                '    repeat with b in (every button of win)\n'
                '        set lbl to ""\n'
                '        try\n'
                '            set lbl to description of b\n'
                '        end try\n'
                '        if lbl contains "model" or lbl contains "Claude" '
                'or lbl contains "GPT" or lbl contains "Sonnet" then\n'
                '            click b\n'
                '            return "ok"\n'
                '        end if\n'
                '    end repeat\n'
                '    return "not_found"\n'
                'end tell'
            )
            result = run_applescript(as_script)
            if "ok" in result:
                time.sleep(0.8)
                if _quickpick_select(model_family):
                    success = True
                    logger.info("cycle_chat_ui_model: ✅ via AppleScript accessibility")
            else:
                logger.debug("cycle_chat_ui_model: AppleScript did not find model button")
        except Exception as e:
            logger.debug(f"cycle_chat_ui_model AppleScript error: {e}")

    if success:
        state["chat_ui_model_index"] = new_idx
        logger.info(f"cycle_chat_ui_model: now using {new_model} in chat UI")
    else:
        logger.warning("cycle_chat_ui_model: all tiers failed — chat UI model unchanged")

    return new_model if success else ""


# ─── Screenshot + OCR ──────────────────────────────────────────────────────────

_SCREENSHOTS_DIR = AGENT_DIR / "smart_monitor" / "screenshots"


def _get_vscode_window_id() -> Optional[int]:
    """Get VS Code main window ID for targeted screencapture."""
    try:
        result = run_applescript(
            'tell application "System Events" to tell process "Code" '
            'to return id of first window'
        )
        val = result.strip()
        return int(val) if val.isdigit() else None
    except Exception:
        return None


def ocr_screenshot(png_path: str) -> str:
    """
    Extract visible text from a PNG using Apple Vision (pyobjc) with
    pytesseract as fallback. Returns extracted text or "" on failure.
    """
    if not png_path or not Path(png_path).exists():
        return ""
    try:
        # Apple Vision — most accurate on macOS, no external process needed
        import Vision  # pyobjc-framework-Vision
        from Foundation import NSData  # type: ignore
        img_data = NSData.dataWithContentsOfFile_(png_path)
        if img_data is None:
            raise ValueError("NSData returned None")
        handler = Vision.VNImageRequestHandler.alloc().initWithData_options_(
            img_data, None
        )
        request = Vision.VNRecognizeTextRequest.alloc().init()
        request.setRecognitionLevel_(Vision.VNRequestTextRecognitionLevelAccurate)
        request.setUsesLanguageCorrection_(True)
        handler.performRequests_error_([request], None)
        lines = []
        for obs in (request.results() or []):
            candidates = obs.topCandidates_(1)
            if candidates:
                lines.append(str(candidates[0].string()))
        return "\n".join(lines)
    except Exception as vision_err:
        logger.debug(f"Vision OCR failed ({vision_err}), trying pytesseract")
        try:
            import pytesseract  # type: ignore
            from PIL import Image
            img = Image.open(png_path)
            return pytesseract.image_to_string(img, lang="spa+eng")
        except Exception as tess_err:
            logger.debug(f"pytesseract also failed: {tess_err}")
            return ""


def capture_workbench_screenshot(label: str = "manual", extra: dict = None) -> str:
    """
    Capture VS Code workbench screenshot using macOS screencapture.

    1. Tries to capture only the VS Code window (via window ID).
    2. Falls back to full-screen capture if window ID unavailable.
    3. Overlays a JSON metadata strip onto the image (label, timestamp, extra).

    Returns the absolute path to the saved PNG, or "" on any failure.
    """
    try:
        _SCREENSHOTS_DIR.mkdir(parents=True, exist_ok=True)
        ts = datetime.now().strftime("%Y%m%d_%H%M%S_%f")[:19]  # drop microsec excess
        safe_label = re.sub(r"[^a-zA-Z0-9_\-]", "_", label)[:40]
        png_path = _SCREENSHOTS_DIR / f"{ts}_{safe_label}.png"

        window_id = _get_vscode_window_id()
        if window_id:
            result = subprocess.run(
                ["screencapture", "-x", "-l", str(window_id), str(png_path)],
                capture_output=True,
                timeout=10,
            )
        else:
            # Fallback: full-screen silent capture
            result = subprocess.run(
                ["screencapture", "-x", str(png_path)],
                capture_output=True,
                timeout=10,
            )

        if result.returncode != 0 or not png_path.exists():
            logger.debug(f"screencapture failed (rc={result.returncode})")
            return ""

        # Optionally write a sidecar JSON with metadata (label + extra)
        if extra:
            sidecar = png_path.with_suffix(".json")
            try:
                sidecar.write_text(
                    json.dumps({"label": label, "ts": ts, **extra}, ensure_ascii=False),
                    encoding="utf-8",
                )
            except Exception:
                pass  # sidecar is optional

        logger.debug(f"Screenshot saved: {png_path.name}")
        return str(png_path)

    except Exception as exc:
        logger.debug(f"capture_workbench_screenshot error: {exc}")
        return ""
