#!/usr/bin/env python3
"""
vscode_chat_reader.py — Lee el panel de Copilot Chat en VS Code via CDP
=======================================================================
Conecta a VS Code (Electron/Chromium) por el puerto de remote debugging (9222)
y extrae el texto del chat de Copilot en tiempo real.

Requisitos:
  - VS Code corriendo con puerto CDP abierto (ya está en puerto 9222)
  - pip install playwright && python3 -m playwright install chromium

Uso:
  python3 .prompts/agent/vscode_chat_reader.py            # dump único del chat
  python3 .prompts/agent/vscode_chat_reader.py --watch    # polling cada 5s
  python3 .prompts/agent/vscode_chat_reader.py --watch --interval 10
  python3 .prompts/agent/vscode_chat_reader.py --save     # guarda en .prompts/agent/chat_snapshot.txt
  python3 .prompts/agent/vscode_chat_reader.py --debug    # muestra todas las páginas CDP disponibles
"""

import argparse
import json
import os
import re
import sys
import time
import urllib.request
from datetime import datetime
from pathlib import Path

CDP_PORT   = 9222
CDP_HOST   = "localhost"
REPO_ROOT  = Path(__file__).parent.parent.parent
SNAPSHOT_FILE = REPO_ROOT / ".prompts" / "agent" / "chat_snapshot.txt"
LOG_FILE   = REPO_ROOT / ".github" / "chat-reader.log"


def cdp_list_targets() -> list[dict]:
    """Obtiene la lista de páginas/targets disponibles via CDP HTTP."""
    url = f"http://{CDP_HOST}:{CDP_PORT}/json"
    try:
        with urllib.request.urlopen(url, timeout=3) as resp:
            return json.loads(resp.read())
    except Exception as e:
        print(f"[ERROR] No se puede conectar a CDP en {CDP_HOST}:{CDP_PORT}: {e}")
        print("\n  VS Code debe tener el puerto CDP abierto.")
        print("  Agrega esto a ~/.vscode/argv.json:")
        print('  { "remote-debugging-port": 9222 }')
        print("  Luego reinicia VS Code.\n")
        sys.exit(1)


def find_copilot_target(targets: list[dict], debug: bool = False) -> dict | None:
    """
    Busca el target que corresponde al panel de Copilot Chat.
    VS Code renderiza el chat como una webview con URL interna.
    """
    if debug:
        print("\n[DEBUG] Targets CDP disponibles:")
        for t in targets:
            print(f"  type={t.get('type','?'):12} title={t.get('title','')[:60]:60} url={t.get('url','')[:80]}")
        print()

    # Candidatos por prioridad
    candidates = []
    for t in targets:
        url   = t.get("url", "")
        title = t.get("title", "")
        ttype = t.get("type", "")

        # Webviews de Copilot Chat — distintos formatos según versión de VS Code
        if any(kw in url for kw in [
            "copilot",
            "github.copilot-chat",
            "chat",
            "panel/chat",
            "workbench/panel",
        ]):
            candidates.append((2, t))
            continue

        if any(kw in title.lower() for kw in ["copilot", "chat"]):
            candidates.append((1, t))
            continue

        # Main window (fallback — leer el DOM completo del workbench)
        if ttype == "page" and "workbench" in url:
            candidates.append((0, t))

    if not candidates:
        return None

    candidates.sort(key=lambda x: -x[0])
    return candidates[0][1]


def extract_chat_text_playwright(target: dict) -> str:
    """
    Conecta al target vía CDP con Playwright y extrae el texto del chat.
    """
    from playwright.sync_api import sync_playwright

    ws_url = target.get("webSocketDebuggerUrl")
    if not ws_url:
        return "[ERROR] Target sin webSocketDebuggerUrl"

    with sync_playwright() as p:
        # connect_over_cdp conecta a una instancia Chromium/Electron existente
        browser = p.chromium.connect_over_cdp(f"http://{CDP_HOST}:{CDP_PORT}")
        contexts = browser.contexts
        if not contexts:
            return "[ERROR] Sin contextos en el browser CDP"

        # Buscar la página correcta entre todos los contextos
        found_page = None
        for ctx in contexts:
            for page in ctx.pages:
                page_url   = page.url
                page_title = page.title()
                if any(kw in page_url for kw in ["copilot", "chat", "workbench"]):
                    found_page = page
                    break
            if found_page:
                break

        if not found_page and contexts:
            # Fallback: primera página disponible
            found_page = contexts[0].pages[0] if contexts[0].pages else None

        if not found_page:
            return "[ERROR] No se encontró página en el contexto CDP"

        # Intentar extraer texto del chat con varios selectores
        # (VS Code usa shadow DOM y webviews, el selector varía por versión)
        selectors = [
            # Panel de chat agente (nueva UI 2024+)
            ".interactive-item-container",
            ".chat-list-item",
            # Respuestas del modelo
            ".value .rendered-markdown",
            ".response-value",
            # Request (lo que escribiste)
            ".chat-request-part",
            # Fallback: todo el panel lateral
            ".composite.panel",
            "[id*='chat']",
            # Chat viejo
            ".chat-list",
        ]

        results = []
        for sel in selectors:
            try:
                elements = found_page.query_selector_all(sel)
                if elements:
                    texts = [el.inner_text() for el in elements if el.inner_text().strip()]
                    if texts:
                        results.append(f"\n--- Selector: {sel} ({len(texts)} elementos) ---")
                        results.extend(texts[:10])  # máx 10 por selector
            except Exception:
                pass

        if not results:
            # Si no hay nada, ejecutar JS para explorar el DOM
            try:
                dom_snapshot = found_page.evaluate("""
                    () => {
                        const allText = [];
                        // Buscar webviews (iframes) que puedan contener el chat
                        const iframes = document.querySelectorAll('iframe, webview');
                        allText.push(`Iframes/webviews encontrados: ${iframes.length}`);
                        
                        // Elementos con "chat" en el id o clase
                        const chatEls = document.querySelectorAll('[id*="chat"], [class*="chat"]');
                        allText.push(`Elementos chat: ${chatEls.length}`);
                        
                        // Texto visible del body (primeros 2000 chars)
                        const bodyText = document.body ? document.body.innerText.slice(0, 2000) : 'sin body';
                        allText.push('--- Body text (2000 chars) ---');
                        allText.push(bodyText);
                        
                        return allText.join('\\n');
                    }
                """)
                results.append(dom_snapshot)
            except Exception as e:
                results.append(f"[JS eval error]: {e}")

        browser.close()
        return "\n".join(results) if results else "[VACÍO] No se encontró texto del chat"


def snapshot_to_file(text: str) -> None:
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    content = f"# Chat Snapshot — {ts}\n\n{text}\n"
    SNAPSHOT_FILE.write_text(content, encoding="utf-8")
    print(f"[SAVED] → {SNAPSHOT_FILE}")


def log_line(msg: str) -> None:
    ts   = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] {msg}"
    LOG_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(line + "\n")


def run_once(args) -> str:
    targets = cdp_list_targets()
    target  = find_copilot_target(targets, debug=args.debug)

    if not target:
        if not args.debug:
            find_copilot_target(targets, debug=True)
        print("[WARN] No se encontró target de Copilot Chat. Usando primer target disponible.")
        target = targets[0] if targets else None

    if not target:
        print("[ERROR] Ningún target CDP disponible.")
        sys.exit(1)

    print(f"[TARGET] {target.get('type','?')} — {target.get('title','')[:70]} — {target.get('url','')[:80]}")

    text = extract_chat_text_playwright(target)

    sep = "=" * 70
    ts  = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    output = f"\n{sep}\nCHAT SNAPSHOT — {ts}\n{sep}\n{text}\n{sep}\n"
    print(output)

    if args.save:
        snapshot_to_file(text)

    log_line(f"snapshot — {len(text)} chars — target: {target.get('title','')[:50]}")
    return text


def main():
    parser = argparse.ArgumentParser(description="Lee el Copilot Chat de VS Code via CDP")
    parser.add_argument("--watch",    action="store_true", help="Poll continuo")
    parser.add_argument("--interval", type=int, default=5, help="Segundos entre polls (default: 5)")
    parser.add_argument("--save",     action="store_true", help="Guarda snapshot en .prompts/agent/chat_snapshot.txt")
    parser.add_argument("--debug",    action="store_true", help="Muestra todos los targets CDP")
    args = parser.parse_args()

    print(f"[START] Conectando a VS Code CDP en {CDP_HOST}:{CDP_PORT}")

    if args.watch:
        prev_text = ""
        print(f"[WATCH] Polling cada {args.interval}s — Ctrl+C para salir\n")
        try:
            while True:
                text = run_once(args)
                if text != prev_text:
                    print("[CAMBIO DETECTADO] El chat fue actualizado.\n")
                    if args.save:
                        snapshot_to_file(text)
                prev_text = text
                time.sleep(args.interval)
        except KeyboardInterrupt:
            print("\n[STOP] Monitor detenido.")
    else:
        run_once(args)


if __name__ == "__main__":
    main()
