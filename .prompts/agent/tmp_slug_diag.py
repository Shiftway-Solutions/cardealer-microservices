"""Diagnose slug matching issue in set_model."""
import sys, json, asyncio, time
sys.path.insert(0, '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent')
import vscode_copilot_monitor as m

async def main():
    m.vscode_focus()
    time.sleep(0.5)
    pw, browser, page = await m._cdp_connect_workbench_page()
    try:
        current_label = await m._cdp_read_selected_model_label(page)
        print(f"current_label: {current_label!r}")
        if not await m._cdp_open_model_picker(page):
            print("FAILED to open picker")
            return
        await asyncio.sleep(0.6)
        root_items = await m._cdp_collect_overlay_items(page)
        catalog_entries = []
        for item in root_items:
            entry = m._extract_model_entry(item, "root", current_label)
            if entry:
                catalog_entries.append(entry)
                print(f"  entry id={entry['id']!r}")
                print(f"        label={entry['label']!r}")
                print(f"        raw_text={entry['raw_text']!r}")
        
        model_ref = "claude-sonnet-4.6-high-1x"
        slug_ref = m._slugify_model_label(model_ref)
        norm_ref = m._normalize_ui_text(model_ref)
        print(f"\nmodel_ref: {model_ref!r}")
        print(f"slug_ref:  {slug_ref!r}")
        print(f"norm_ref:  {norm_ref!r}")
        
        for entry in catalog_entries:
            eid = entry['id']
            match = slug_ref == eid
            haystack = m._normalize_ui_text(f"{entry.get('label', '')} {entry.get('raw_text', '')}")
            contains = norm_ref in haystack
            print(f"  vs {eid!r}: slug_match={match}, haystack_contains={contains}")
            print(f"    haystack={haystack!r}")
        
        result = m._resolve_model_reference(model_ref, catalog_entries)
        print(f"\n_resolve result: {result}")
        
        await page.keyboard.press("Escape")
    finally:
        await m._cdp_close(pw, browser)

asyncio.run(main())
