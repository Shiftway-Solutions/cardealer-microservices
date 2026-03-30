"""
Comprehensive VS Code Copilot Chat UI Explorer
Discovers: model submenus, new-chat dropdown, chat history, stop button, all toolbar items.
"""
import sys, json, asyncio, time, os
sys.path.insert(0, '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent')
import vscode_copilot_monitor as m

SCREENSHOT_DIR = '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent/screenshots'

async def shot(page, label, meta=None):
    path = await m._cdp_capture_screenshot(page, f"explore_{label}", meta or {})
    print(f"  [SCREEN] {label} -> {os.path.basename(path)}")
    return path

async def escape_all(page):
    for _ in range(3):
        await page.keyboard.press("Escape")
        await asyncio.sleep(0.15)

# ─── 1. Inspect model picker root + submenus ─────────────────────────────────
async def inspect_model_submenus(page):
    print("\n=== MODEL PICKER + SUBMENUS ===")
    if not await m._cdp_open_model_picker(page):
        print("  ERROR: could not open picker")
        return {}
    await asyncio.sleep(0.6)
    await shot(page, "picker_root")

    root_items = await m._cdp_collect_overlay_items(page)
    print(f"  root items: {len(root_items)}")
    result = {"root": [], "submenus": {}}

    # Collect root items
    for item in root_items:
        text = (item.get("text") or item.get("aria") or "").strip()
        if text:
            result["root"].append(text)
            print(f"    [{item.get('role','')}] {text!r}  class={item.get('class','')[:50]}")

    # Find items with arrows (submenu triggers): class contains 'submenu'
    # or aria has expand indicator, or look for elements in root with submenu class
    submenu_triggers = await page.evaluate("""() => {
        const rows = [...document.querySelectorAll('.context-view .monaco-list-row[role="menuitemradio"], .context-view .monaco-list-row[role="menuitem"]')];
        return rows.map((row) => {
            const hasArrow = !!row.querySelector('.submenu-indicator, .codicon-chevron-right, [class*="chevron"]');
            const rect = row.getBoundingClientRect();
            return {
                text: (row.innerText || '').replace(/\\s+/g, ' ').trim(),
                hasSubmenu: hasArrow,
                x: rect.x + rect.width / 2,
                y: rect.y + rect.height / 2,
                width: rect.width,
                height: rect.height,
                className: row.className,
            };
        });
    }""")
    print(f"\n  submenu_triggers scan:")
    for t in submenu_triggers:
        print(f"    hasSubmenu={t['hasSubmenu']} {t['text']!r}")

    # Hover over each item with submenu & screenshot the thinking-effort submenu
    for trigger in submenu_triggers:
        if trigger.get("hasSubmenu") and trigger.get("x"):
            label = trigger["text"][:30].replace(" ", "_")
            print(f"\n  >> Hovering: {trigger['text']!r}")
            await page.mouse.move(trigger["x"], trigger["y"])
            await asyncio.sleep(0.7)
            await shot(page, f"hover_{label}")
            nested = await page.evaluate("""() => {
                const all = [...document.querySelectorAll('.context-view .monaco-list-row')];
                return all.map((row) => {
                    const rect = row.getBoundingClientRect();
                    return {
                        text: (row.innerText || '').replace(/\\s+/g, ' ').trim(),
                        role: row.getAttribute('role') || '',
                        checked: row.getAttribute('aria-checked') || '',
                        x: rect.x, y: rect.y, width: rect.width, height: rect.height,
                        className: row.className,
                    };
                });
            }""")
            # Take only non-root items that appeared
            existing_texts = {t2["text"] for t2 in submenu_triggers}
            new_items = [n for n in nested if n["text"] and n["text"] not in existing_texts]
            if new_items:
                result["submenus"][trigger["text"]] = new_items
                print(f"    nested items ({len(new_items)}):")
                for ni in new_items:
                    print(f"      [{ni.get('checked','')}] {ni['text']!r}")

    await escape_all(page)
    return result


# ─── 2. Inspect new-chat dropdown ────────────────────────────────────────────
async def inspect_new_chat_dropdown(page):
    print("\n=== NEW CHAT DROPDOWN (+v button) ===")
    # Find the dropdown arrow next to the + button  
    dropdown_info = await page.evaluate("""() => {
        // Look for the dropdown button in the chat header
        const candidates = [
            ...document.querySelectorAll('.chat-view-welcome, .panel-pane, .composite.panel [class*="action"], [aria-label*="new chat" i], [aria-label*="new" i], .action-item button[aria-haspopup]')
        ];
        // Also scan all buttons that say New Chat or have dropdown arrow
        const allButtons = [...document.querySelectorAll('a[role="button"], button')];
        const results = [];
        for (const btn of allButtons) {
            const text = (btn.innerText || btn.getAttribute('aria-label') || '').trim();
            const rect = btn.getBoundingClientRect();
            if (rect.width === 0) continue;
            if (btn.classList.contains('codicon-chevron-down') || 
                btn.getAttribute('aria-haspopup') ||
                /new.?chat|more actions/i.test(text)) {
                results.push({
                    tag: btn.tagName.toLowerCase(),
                    text,
                    aria: btn.getAttribute('aria-label') || '',
                    ariaHaspopup: btn.getAttribute('aria-haspopup') || '',
                    className: btn.className,
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2,
                    width: rect.width,
                    height: rect.height,
                });
            }
        }
        return results.slice(0, 20);
    }""")
    print(f"  Candidates: {len(dropdown_info)}")
    for d in dropdown_info:
        print(f"    {d['tag']} text={d['text']!r} aria={d['aria']!r} x={d['x']:.0f} y={d['y']:.0f}")

    # Find the + with dropdown (the v chevron beside it)
    new_chat_btn = await page.evaluate("""() => {
        // The + v button area is usually in chat panel header
        const chatHeader = document.querySelector('.chat-panel-item, .pane-header[id*="chat"], .composite.panel');
        const allAnchors = [...document.querySelectorAll('a[role="button"][aria-label], li[role="presentation"] a')];
        for (const a of allAnchors) {
            const label = a.getAttribute('aria-label') || '';
            if (/new.?(chat|conversation)/i.test(label) || /create.*chat/i.test(label)) {
                const rect = a.getBoundingClientRect();
                if (rect.width > 0) return {
                    label,
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2,
                    className: a.className,
                };
            }
        }
        // Also scan toolbar buttons
        const toolbarBtns = [...document.querySelectorAll('.actions-container .action-item a[role="button"]')];
        const matched = toolbarBtns.filter((a) => {
            const rect = a.getBoundingClientRect();
            return rect.y < 110 && rect.y > 40; // header area
        });
        return matched.map((a) => ({
            label: a.getAttribute('aria-label') || a.innerText,
            className: a.className,
            x: a.getBoundingClientRect().x + a.getBoundingClientRect().width/2,
            y: a.getBoundingClientRect().y + a.getBoundingClientRect().height/2,
        }));
    }""")
    print(f"\n  new_chat_btn search: {json.dumps(new_chat_btn, indent=2)[:400]}")

    # Click the dropdown arrow (the v beside +)
    # It's typically a button in the panel header with aria-haspopup
    clicked = await page.evaluate("""() => {
        // Find the v/chevron button beside the + in chat panel
        const all = [...document.querySelectorAll('.panel .actions-container a[role="button"], .chat-widget .actions-container a[role="button"]')];
        for (const a of all) {
            const rect = a.getBoundingClientRect();
            if (rect.width === 0 || rect.y > 120) continue;
            const label = (a.getAttribute('aria-label') || '').toLowerCase();
            // Look for the dropdown part (usually has aria-haspopup or contains chevron)
            const hasChevron = !!a.querySelector('.codicon-chevron-down');
            const isNewChat = /new.?chat|chat.view.new/i.test(label);
            if (isNewChat || hasChevron) {
                const btn = a;
                return {
                    label,
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2,
                    hasChevron,
                };
            }
        }
        return null;
    }""")
    print(f"  clicked candidate: {clicked}")

    # Actually find and click the + button using broader search
    all_header_actions = await page.evaluate("""() => {
        // Scan for all visible action items in the top panel area (y < 120)
        const items = [...document.querySelectorAll('.action-item a[role="button"]')];
        return items
            .filter((a) => {
                const r = a.getBoundingClientRect();
                return r.width > 0 && r.y < 120 && r.y > 30;
            })
            .map((a) => {
                const r = a.getBoundingClientRect();
                return {
                    label: a.getAttribute('aria-label') || a.innerText || '',
                    className: a.className,
                    x: r.x + r.width/2,
                    y: r.y + r.height/2,
                    width: r.width,
                    height: r.height,
                };
            });
    }""")
    print(f"\n  Header actions ({len(all_header_actions)}):")
    for a in all_header_actions:
        print(f"    label={a['label']!r}  x={a['x']:.0f} y={a['y']:.0f}")

    # Find the new chat dropdown specifically
    dropdown_btn = next(
        (a for a in all_header_actions if "new" in a["label"].lower() or "+" in a["label"] or "chat" in a["label"].lower()),
        None
    )
    if not dropdown_btn and all_header_actions:
        dropdown_btn = all_header_actions[0]
    
    new_chat_items = []
    if dropdown_btn:
        print(f"\n  >> Clicking: {dropdown_btn['label']!r} at ({dropdown_btn['x']:.0f}, {dropdown_btn['y']:.0f})")
        await page.mouse.click(dropdown_btn["x"], dropdown_btn["y"])
        await asyncio.sleep(0.6)
        await shot(page, "new_chat_dropdown")
        
        menu_items = await page.evaluate("""() => {
            const sel = [
                '.context-view .monaco-list-row',
                '.context-view .action-item',
                '.quick-input-widget .monaco-list-row',
                '.monaco-menu .action-item',
            ];
            const results = [];
            for (const s of sel) {
                for (const el of document.querySelectorAll(s)) {
                    const rect = el.getBoundingClientRect();
                    if (rect.width === 0) continue;
                    const text = (el.innerText || '').replace(/\\s+/g, ' ').trim();
                    if (!text) continue;
                    results.push({
                        text,
                        role: el.getAttribute('role') || '',
                        aria: el.getAttribute('aria-label') || '',
                        x: rect.x + rect.width/2,
                        y: rect.y + rect.height/2,
                    });
                }
            }
            return results.slice(0, 30);
        }""")
        new_chat_items = menu_items
        print(f"  Menu items ({len(menu_items)}):")
        for item in menu_items:
            print(f"    {item['text']!r}  x={item['x']:.0f} y={item['y']:.0f}")
        await escape_all(page)

    return {"header_actions": all_header_actions, "dropdown_items": new_chat_items}


# ─── 3. Chat history / list ───────────────────────────────────────────────────
async def inspect_chat_history(page):
    print("\n=== CHAT HISTORY LIST ===")
    chat_list = await page.evaluate("""() => {
        // Chat history items
        const listSels = [
            '[aria-label*="chat history" i] .monaco-list-row',
            '.chat-history .monaco-list-row',
            '[data-uri*="chat"] .monaco-list-row',
            '.pane-body .monaco-list-row',
        ];
        const results = [];
        for (const sel of listSels) {
            const els = [...document.querySelectorAll(sel)];
            if (els.length) {
                for (const el of els) {
                    const text = (el.innerText || '').replace(/\\s+/g, ' ').trim();
                    if (text) results.push({ selector: sel, text, className: el.className });
                }
                break;
            }
        }
        return results.slice(0, 20);
    }""")

    # Also find the current chat title in header
    chat_title = await page.evaluate("""() => {
        // The chat header title (e.g., "PROBLEMAS CON EL AGENTE SMART_MONITOR Y LOGS")
        const titleSels = [
            '.pane-header .title',
            '.composite.panel .title',
            '.panel .panel-section-title',
            '.chat-view .title-label',
            'h2.title',
            '.chat-title',
            '[aria-label*="chat" i].title',
            '.pane-header span.title',
        ];
        for (const sel of titleSels) {
            const el = document.querySelector(sel);
            if (el) {
                const text = (el.innerText || el.textContent || '').trim();
                if (text) return { selector: sel, text, className: el.className, id: el.id };
            }
        }
        // Fallback: scan for .pane-header
        const headers = [...document.querySelectorAll('.pane-header')];
        return headers.map((h) => ({
            selector: '.pane-header', 
            text: (h.innerText || '').replace(/\\s+/g, ' ').trim().slice(0, 100),
            id: h.id,
            className: h.className,
        }));
    }""")
    print(f"  Current chat title: {json.dumps(chat_title, indent=2)[:400]}")
    print(f"  Chat list items: {len(chat_list)}")
    for item in chat_list[:10]:
        print(f"    {item['text'][:80]!r}")

    # Also look for the back arrow / thread list
    thread_nav = await page.evaluate("""() => {
        // The back arrow button and chat list navigation
        const navBtns = [...document.querySelectorAll('a[role="button"][aria-label], button[aria-label]')];
        return navBtns
            .filter((b) => {
                const label = (b.getAttribute('aria-label') || '').toLowerCase();
                const rect = b.getBoundingClientRect();
                return rect.width > 0 && (
                    /back|history|thread|chat.list|previous/i.test(label)
                );
            })
            .map((b) => {
                const rect = b.getBoundingClientRect();
                return {
                    label: b.getAttribute('aria-label') || '',
                    className: b.className,
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2,
                };
            });
    }""")
    print(f"\n  Navigation buttons: {json.dumps(thread_nav, indent=2)[:400]}")
    return {"title": chat_title, "list": chat_list, "nav": thread_nav}


# ─── 4. All UI components full scan ──────────────────────────────────────────
async def inspect_all_ui_components(page):
    print("\n=== ALL VISIBLE UI COMPONENTS ===")
    all_components = await page.evaluate("""() => {
        const results = {
            toolbars: [],
            pickers: [],
            buttons: [],
            inputs: [],
            panels: [],
        };
        // Pickers (bottom toolbar items)
        for (const el of document.querySelectorAll('.chat-input-picker-item')) {
            const rect = el.getBoundingClientRect();
            if (!rect.width) continue;
            results.pickers.push({
                text: (el.innerText || '').replace(/\\s+/g, ' ').trim(),
                aria: el.getAttribute('aria-label') || '',
                className: el.className,
                x: rect.x, y: rect.y,
            });
        }
        // All visible action buttons in chat area
        for (const el of document.querySelectorAll('.chat-input-toolbars a[role="button"], .chat-input-toolbars button')) {
            const rect = el.getBoundingClientRect();
            if (!rect.width) continue;
            results.buttons.push({
                text: (el.innerText || el.getAttribute('aria-label') || '').replace(/\\s+/g, ' ').trim(),
                aria: el.getAttribute('aria-label') || '',
                className: el.className,
                x: rect.x, y: rect.y,
                width: rect.width, height: rect.height,
            });
        }
        // Panel header buttons
        for (const el of document.querySelectorAll('.panel .actions-container a[role="button"]')) {
            const rect = el.getBoundingClientRect();
            if (!rect.width) continue;
            results.toolbars.push({
                text: (el.innerText || el.getAttribute('aria-label') || '').replace(/\\s+/g, ' ').trim(),
                aria: el.getAttribute('aria-label') || '',
                className: el.className,
                x: rect.x, y: rect.y,
            });
        }
        // Textarea / input
        for (const el of document.querySelectorAll('textarea, input[type="text"]')) {
            const rect = el.getBoundingClientRect();
            if (!rect.width) continue;
            results.inputs.push({
                tag: el.tagName.toLowerCase(),
                placeholder: el.getAttribute('placeholder') || '',
                className: el.className,
                x: rect.x, y: rect.y,
                width: rect.width, height: rect.height,
            });
        }
        return results;
    }""")
    for category, items in all_components.items():
        print(f"\n  [{category.upper()}] ({len(items)} items):")
        for item in items[:15]:
            label = item.get("aria") or item.get("text") or item.get("placeholder") or ""
            print(f"    x={item.get('x',0):.0f} y={item.get('y',0):.0f}  {label[:60]!r}")
    return all_components


# ─── 5. Stop response button ─────────────────────────────────────────────────
async def inspect_stop_button(page):
    print("\n=== STOP RESPONSE BUTTON ===")
    stop_info = await page.evaluate("""() => {
        // Various possible stop button selectors
        const sels = [
            'a[aria-label="Stop Response"]',
            'button[aria-label="Stop Response"]', 
            'a[aria-label*="stop" i]',
            'button[aria-label*="stop" i]',
            '.chat-input-toolbars a[role="button"]',
            '.interactive-input-followups a',
        ];
        const results = [];
        const seen = new Set();
        for (const sel of sels) {
            for (const el of document.querySelectorAll(sel)) {
                const label = (el.getAttribute('aria-label') || el.innerText || '').trim();
                if (seen.has(label)) continue;
                seen.add(label);
                const rect = el.getBoundingClientRect();
                results.push({
                    selector: sel,
                    tag: el.tagName.toLowerCase(),
                    label,
                    className: el.className,
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2,
                    width: rect.width,
                    height: rect.height,
                    visible: rect.width > 0 && rect.height > 0,
                });
            }
        }
        return results.slice(0, 20);
    }""")
    print(f"  Stop button candidates ({len(stop_info)}):")
    for s in stop_info:
        print(f"    {s['label']!r}  visible={s['visible']}  x={s['x']:.0f} y={s['y']:.0f}  class={s['className'][:50]}")

    # Also look at chat response actions (the stop during streaming)
    stop_streaming = await page.evaluate("""() => {
        // Scan for ANY element with stop/cancel text
        const results = [];
        for (const el of document.querySelectorAll('a[role="button"], button, .action-label')) {
            const text = (el.getAttribute('aria-label') || el.innerText || el.title || '').toLowerCase().trim();
            if (!text) continue;
            if (text.includes('stop') || text.includes('cancel') || text.includes('interrupt')) {
                const rect = el.getBoundingClientRect();
                results.push({
                    tag: el.tagName.toLowerCase(),
                    text: text.slice(0, 60),
                    aria: el.getAttribute('aria-label') || '',
                    className: el.className.slice(0, 60),
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2,
                    width: rect.width,
                    height: rect.height,
                });
            }
        }
        return results.slice(0, 20);
    }""")
    print(f"\n  Stop/Cancel elements scan ({len(stop_streaming)}):")
    for s in stop_streaming:
        print(f"    {s['text']!r}  x={s['x']:.0f} y={s['y']:.0f}  w={s['width']:.0f}  class={s['className']}")

    return {"stop_candidates": stop_info, "stop_elements": stop_streaming}


# ─── main ─────────────────────────────────────────────────────────────────────
async def main():
    m.vscode_focus()
    time.sleep(0.8)
    pw, browser, page = await m._cdp_connect_workbench_page()
    try:
        await shot(page, "start")
        
        # 1. Model submenus
        model_data = await inspect_model_submenus(page)
        await asyncio.sleep(0.5)

        # 2. New chat dropdown
        new_chat_data = await inspect_new_chat_dropdown(page)
        await asyncio.sleep(0.5)

        # 3. Chat history
        chat_history_data = await inspect_chat_history(page)
        await asyncio.sleep(0.3)

        # 4. All UI components
        ui_data = await inspect_all_ui_components(page)
        await asyncio.sleep(0.3)

        # 5. Stop button
        stop_data = await inspect_stop_button(page)

        await shot(page, "end")

        # Save full report
        report = {
            "model_submenus": model_data,
            "new_chat_dropdown": new_chat_data,
            "chat_history": chat_history_data,
            "ui_components": ui_data,
            "stop_button": stop_data,
        }
        import pathlib
        out = '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent/ui_exploration_report.json'
        pathlib.Path(out).write_text(json.dumps(report, indent=2, ensure_ascii=False))
        print(f"\n✅ Report saved: {out}")

    finally:
        await m._cdp_close(pw, browser)

asyncio.run(main())
