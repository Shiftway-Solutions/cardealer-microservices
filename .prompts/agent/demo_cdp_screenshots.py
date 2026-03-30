"""
DEMO con screenshots: Flujo completo DOM/CDP puro
1. Screenshot del chat actual
2. Click DOM/CDP en botón + → nuevo chat → screenshot
3. Screenshot del panel de sesiones con lista de chats
4. JS el.click() para volver al chat original → screenshot
"""
import asyncio, sys
sys.path.insert(0, '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent')
import vscode_copilot_monitor as m

SS = '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent/screenshots'

async def demo():
    pw, browser, page = await m._cdp_connect_workbench_page()
    try:
        # ── PASO 1: Estado inicial ──────────────────────────────
        title_before = await m._cdp_get_current_chat_title(page)
        print(f"[PASO 1] Título ANTES: {title_before!r}")
        await page.screenshot(path=f'{SS}/demo_01_antes.png')
        print(f"  📸 demo_01_antes.png")

        # ── PASO 2: Click DOM/CDP en botón + ───────────────────
        btn = await page.evaluate(r"""() => {
            for (const el of document.querySelectorAll('.part.auxiliarybar a[aria-label="New Chat (⌘N)"]')) {
                const r = el.getBoundingClientRect();
                if (r.width > 0) return { x: Math.round(r.x + r.width/2), y: Math.round(r.y + r.height/2), label: el.getAttribute('aria-label') };
            }
            return null;
        }""")
        print(f"\n[PASO 2] Botón +: selector='.part.auxiliarybar a[aria-label=\"New Chat (⌘N)\"]'")
        print(f"         coords: x={btn['x']}, y={btn['y']}")
        print(f"  → page.mouse.click({btn['x']}, {btn['y']}) [CDP]")

        await page.mouse.click(btn['x'], btn['y'])
        await asyncio.sleep(1.3)

        title_new = await m._cdp_get_current_chat_title(page)
        print(f"  Título DESPUÉS de nuevo chat: {title_new!r}")
        await page.screenshot(path=f'{SS}/demo_02_nuevo_chat.png')
        print(f"  📸 demo_02_nuevo_chat.png")

        # ── PASO 3: Screenshot con panel de sesiones visible ───
        rows = await page.evaluate(r"""() => {
            const results = [];
            for (const el of document.querySelectorAll('#workbench\\.panel\\.chat .monaco-list-row')) {
                const r = el.getBoundingClientRect();
                const label = el.getAttribute('aria-label') || '';
                const match = label.match(/(?:Local session\s+)?(.+?)\s*\((?:Completed|Failed|In Progress)[^)]*\)/);
                const title = match ? match[1].trim() : label.substring(0, 60);
                results.push({
                    title,
                    dataIndex: el.getAttribute('data-index') || '',
                    visible: r.width > 0 && r.height > 0,
                    x: Math.round(r.x + r.width/2),
                    y: Math.round(r.y + r.height/2),
                });
            }
            return results;
        }""")
        visible = [r for r in rows if r['visible']]
        print(f"\n[PASO 3] Sesiones en DOM: {len(rows)} total, {len(visible)} visibles")
        for row in visible[:6]:
            print(f"  dataIndex={row['dataIndex']:>2} | {row['title'][:55]!r:60s} | x={row['x']}, y={row['y']}")
        await page.screenshot(path=f'{SS}/demo_03_sesiones.png')
        print(f"  📸 demo_03_sesiones.png")

        # ── PASO 4: Buscar chat original por nombre ─────────────
        search = title_before
        target = next((r for r in visible if search.lower() in r['title'].lower()), None)
        if not target:
            target = next((r for r in rows if search.lower() in r['title'].lower()), None)

        print(f"\n[PASO 4] Buscando chat: {search!r}")
        print(f"  Encontrado: dataIndex={target['dataIndex']}, title={target['title']!r}")

        # ── PASO 5: JS el.click() para navegar ──────────────────
        print(f"\n[PASO 5] Ejecutando JS: el.click() en dataIndex={target['dataIndex']}")
        result = await page.evaluate(r"""(idx) => {
            const el = [...document.querySelectorAll('#workbench\\.panel\\.chat .monaco-list-row')]
                .find(r => r.getAttribute('data-index') === String(idx));
            if (!el) return 'NOT FOUND';
            el.scrollIntoView({ block: 'nearest', behavior: 'instant' });
            el.click();
            return `clicked: ${el.tagName} [data-index="${el.getAttribute('data-index')}"]`;
        }""", target['dataIndex'])
        print(f"  JS result: {result}")
        await asyncio.sleep(1.2)

        title_final = await m._cdp_get_current_chat_title(page)
        print(f"  Título FINAL: {title_final!r}")
        await page.screenshot(path=f'{SS}/demo_04_de_vuelta.png')
        print(f"  📸 demo_04_de_vuelta.png")

        # ── RESUMEN ─────────────────────────────────────────────
        print(f"\n{'═'*60}")
        print(f"  RESUMEN FINAL")
        print(f"{'═'*60}")
        print(f"  Antes:    {title_before!r}")
        print(f"  Nuevo:    {title_new!r}  (vacío = nuevo chat)")
        print(f"  Final:    {title_final!r}")
        ok = title_before.lower() in (title_final or '').lower()
        print(f"  Resultado: {'✅ ÉXITO — regresamos al chat original' if ok else '❌ FALLO'}")
        print(f"  AppleScript usado: NO — solo DOM/CDP")
        print(f"{'═'*60}")

    finally:
        await m._cdp_close(pw, browser)

asyncio.run(demo())
