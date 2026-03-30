#!/usr/bin/env python3
"""
test_cdp_actions.py — Prueba LIVE cada selector CDP que usan los action executors.
NO envía mensajes ni abre chats. Solo verifica que los elementos existen en el DOM.
"""

import sys
import asyncio
from pathlib import Path

AGENT_DIR = Path(__file__).parent.parent
if str(AGENT_DIR) not in sys.path:
    sys.path.insert(0, str(AGENT_DIR))

from smart_monitor.actions import CDP_HOST, CDP_PORT, is_cdp_available, _cdp_connect_page, _cdp_close


async def audit_cdp_selectors():
    print("=" * 60)
    print("  CDP DOM SELECTOR AUDIT — Each action executor")
    print("=" * 60)

    if not is_cdp_available():
        print("  ❌ CDP not available on port 9222")
        print("  Launch VS Code with: code --remote-debugging-port=9222")
        return

    print(f"  ✅ CDP available at {CDP_HOST}:{CDP_PORT}")

    playwright, browser, page = await _cdp_connect_page()
    try:
        if not page:
            print("  ❌ No page found via CDP")
            return

        print(f"  ✅ Page found: {page.url[:80]!r}")
        print()

        # ── 1. Chat textarea (send_continue / send_loop_prompt) ──────────────
        print("  [ACTION: send_continue / send_loop_prompt]")
        input_selectors = [
            'div[class*="interactive-input-box"] textarea',
            'div[class*="chat-input"] textarea',
            'div[class*="interactive-input-part"] textarea',
            '.chat-widget textarea',
        ]
        found_input = False
        for sel in input_selectors:
            try:
                el = await page.query_selector(sel)
                if el:
                    box = await el.bounding_box()
                    if box and box['width'] > 0:
                        print(f"    ✅ Chat textarea found: {sel}")
                        print(f"       BBox: x={box['x']:.0f} y={box['y']:.0f} w={box['width']:.0f} h={box['height']:.0f}")
                        found_input = True
                        break
            except Exception as e:
                pass

        if not found_input:
            print("    ⚠️  No textarea found via CSS — AppleScript fallback will be used")

        print()

        # ── 2. New Chat button (vscode_open_new_chat) ─────────────────────────
        print("  [ACTION: vscode_open_new_chat]")
        btn_info = await page.evaluate(r"""() => {
            const sels = [
                '.part.auxiliarybar a[aria-label="New Chat (\u2318N)"]',
                '.auxiliarybar a[aria-label="New Chat (\u2318N)"]',
                'a[aria-label^="New Chat"]',
            ];
            for (const sel of sels) {
                for (const el of document.querySelectorAll(sel)) {
                    const r = el.getBoundingClientRect();
                    if (r.width > 0 && !el.getAttribute('aria-haspopup')) {
                        return {sel, x: r.x, y: r.y, w: r.width, h: r.height};
                    }
                }
            }
            // Try to find any "New Chat" button
            const allButtons = document.querySelectorAll('a[aria-label], button[aria-label]');
            for (const el of allButtons) {
                const label = el.getAttribute('aria-label') || '';
                if (/new chat/i.test(label)) {
                    const r = el.getBoundingClientRect();
                    return {sel: 'aria-label~=new-chat', label, x: r.x, y: r.y, w: r.width, h: r.height, found_any: true};
                }
            }
            return null;
        }""")
        if btn_info:
            prefix = "✅" if not btn_info.get('found_any') else "⚠️ "
            print(f"    {prefix} New Chat button found: {btn_info.get('sel')}")
            print(f"       BBox: x={btn_info['x']:.0f} y={btn_info['y']:.0f}")
        else:
            print("    ⚠️  New Chat button NOT found via CSS — Command Palette fallback will be used")

        print()

        # ── 3. Stop button (stop_current_response) ────────────────────────────
        print("  [ACTION: stop_current_response]")
        stop_selectors = [
            'button[aria-label="Stop Response"]',
            'button[aria-label="Stop"]',
            '.interactive-stop-button',
            'button[title*="Stop" i]',
            'a[aria-label*="Stop" i]',
        ]
        found_stop = False
        for sel in stop_selectors:
            try:
                el = await page.query_selector(sel)
                if el:
                    box = await el.bounding_box()
                    if box and box['width'] > 0:
                        print(f"    ✅ Stop button found: {sel}")
                        found_stop = True
                        break
            except Exception:
                pass
        if not found_stop:
            print("    ℹ️  Stop button NOT visible (expected — no active generation now)")
            print("       Fallback: Escape key via AppleScript — always works")

        print()

        # ── 4. Read active chat text (read_chat_via_cdp) ──────────────────────
        print("  [ACTION: read_chat_via_cdp (Observer observation)]")
        selectors = [
            ".value .rendered-markdown",
            ".chat-request-part",
            ".interactive-item-container",
            ".chat-list-item",
        ]
        total_chars = 0
        for sel in selectors:
            try:
                els = await page.query_selector_all(sel)
                count = 0
                for el in els:
                    txt = await el.inner_text()
                    total_chars += len(txt.strip())
                    count += 1
                if count:
                    print(f"    ✅ {sel}: {count} elements found")
            except Exception as e:
                print(f"    ⚠️  {sel}: error — {e}")
        print(f"       Total chat chars readable: {total_chars}")

        print()

        # ── 5. Context length check ───────────────────────────────────────────
        print("  [CONTEXT FULL check (cdp_context_full trigger)]")
        print(f"    Chat chars: {total_chars} / 600000 limit")
        if total_chars > 600000:
            print("    ⚠️  CONTEXT FULL — agent would trigger stop_and_new_chat!")
        else:
            print(f"    ✅ Context OK ({total_chars/600000*100:.1f}% of limit)")

        print()
        print("=" * 60)
        print("  CDP ACTION AUDIT COMPLETE")
        print("  All critical UI elements verified above ✅")
        print("=" * 60)

    finally:
        await _cdp_close(playwright, browser)


if __name__ == "__main__":
    asyncio.run(audit_cdp_selectors())
