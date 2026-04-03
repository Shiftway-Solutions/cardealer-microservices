#!/usr/bin/env python3
"""
VS Code Copilot Chat — Model Picker Automation
Técnicas (en orden de prioridad):
  1. CDP (Chrome DevTools Protocol) via WebSocket — si VS Code tiene debug port activo
  2. Screenshot + OCR (pytesseract) — encuentra el botón por texto, sin coordenadas fijas
  3. AppleScript Accessibility API — navega el árbol de accesibilidad de VS Code
  4. PyAutoGUI + teclado — fallback final

Uso:
  python3 click_model_picker.py                    # Abre picker y lista modelos disponibles
  python3 click_model_picker.py "Claude Opus 4.5"  # Selecciona modelo específico
  python3 click_model_picker.py --cdp-port 9222    # Fuerza uso de CDP en puerto específico

Para habilitar CDP en VS Code (cierra y reabre con):
  code --remote-debugging-port=9222
"""
import subprocess
import time
import sys
import json
import re
import argparse
import os
from typing import Optional, Tuple

# ─────────────────────────────────────────────
# CONFIGURACIÓN
# ─────────────────────────────────────────────
DEFAULT_TARGET_MODEL = "Claude Opus 4.5"
CDP_PORTS_TO_TRY = [9222, 9229, 9223, 9224]


# ══════════════════════════════════════════════
# MÉTODO 1: CDP (Chrome DevTools Protocol)
# ══════════════════════════════════════════════

def find_cdp_port() -> Optional[int]:
    """Escanea puertos CDP comunes buscando una instancia de VS Code/Electron."""
    import urllib.request, urllib.error
    for port in CDP_PORTS_TO_TRY:
        try:
            url = f"http://localhost:{port}/json/version"
            with urllib.request.urlopen(url, timeout=0.5) as r:
                data = json.loads(r.read())
                browser = data.get("Browser", "")
                if "Electron" in browser or "Chrome" in browser:
                    print(f"  ✅ CDP encontrado en puerto {port}: {browser[:60]}")
                    return port
        except Exception:
            pass
    return None


def get_cdp_targets(port: int) -> list:
    """Lista todos los targets (páginas/webviews) disponibles via CDP."""
    import urllib.request
    url = f"http://localhost:{port}/json"
    with urllib.request.urlopen(url, timeout=2) as r:
        return json.loads(r.read())


async def cdp_find_and_click_model(port: int, target_model: str) -> bool:
    """
    Conecta via CDP WebSocket, inspecciona el DOM del panel de chat
    y hace click en el model picker.
    """
    import asyncio
    try:
        import websockets
    except ImportError:
        print("  ⚠️  websockets no instalado: pip install websockets")
        return False

    targets = get_cdp_targets(port)
    print(f"  📋 Targets CDP disponibles: {len(targets)}")

    # Buscar el target de la webview del chat de Copilot
    chat_target = None
    for t in targets:
        title = t.get("title", "").lower()
        url = t.get("url", "").lower()
        ttype = t.get("type", "")
        print(f"     [{ttype}] {t.get('title', 'sin título')[:70]}")
        if any(kw in title + url for kw in ["copilot", "chat", "github.copilot", "panel"]):
            chat_target = t
            print(f"  🎯 Target seleccionado: {t.get('title')}")

    if not chat_target and targets:
        # Usar el primer target page como fallback
        chat_target = next((t for t in targets if t.get("type") == "page"), targets[0])
        print(f"  ⚠️  No se encontró target de chat, usando: {chat_target.get('title', '?')[:60]}")

    if not chat_target:
        print("  ❌ No hay targets CDP disponibles")
        return False

    ws_url = chat_target["webSocketDebuggerUrl"]
    print(f"  🔌 Conectando WebSocket: {ws_url[:80]}")

    try:
        async with websockets.connect(ws_url, ping_interval=None) as ws:
            msg_id = 1

            async def send(method, params=None):
                nonlocal msg_id
                payload = {"id": msg_id, "method": method, "params": params or {}}
                msg_id += 1
                await ws.send(json.dumps(payload))
                while True:
                    raw = await asyncio.wait_for(ws.recv(), timeout=5)
                    resp = json.loads(raw)
                    if resp.get("id") == payload["id"]:
                        return resp.get("result", {})

            # Habilitar DOM
            await send("DOM.enable")
            await send("Runtime.enable")

            # Buscar el botón del model picker via JS
            js_find_btn = """
            (() => {
                // Busca botones que contengan el texto del modelo actual
                const allBtns = Array.from(document.querySelectorAll('button, [role="button"], [class*="model"], [class*="picker"]'));
                const modelBtn = allBtns.find(el => {
                    const txt = el.textContent || el.getAttribute('aria-label') || '';
                    return /claude|gpt|gemini|o[134]|copilot/i.test(txt);
                });
                if (!modelBtn) return JSON.stringify({found: false, btns: allBtns.slice(0,5).map(b => b.textContent.trim().slice(0,50))});
                const rect = modelBtn.getBoundingClientRect();
                return JSON.stringify({
                    found: true,
                    text: modelBtn.textContent.trim().slice(0,80),
                    x: rect.x + rect.width/2,
                    y: rect.y + rect.height/2
                });
            })()
            """
            result = await send("Runtime.evaluate", {
                "expression": js_find_btn,
                "returnByValue": True
            })
            val = result.get("result", {}).get("value", "{}")
            data = json.loads(val) if isinstance(val, str) else {}
            print(f"  🔎 Resultado búsqueda DOM: {data}")

            if data.get("found"):
                x, y = data["x"], data["y"]
                print(f"  🖱️  Click en model picker '{data['text']}' @ ({x:.0f}, {y:.0f})")
                # Click via CDP Input
                await send("Input.dispatchMouseEvent", {
                    "type": "mousePressed", "x": x, "y": y,
                    "button": "left", "clickCount": 1
                })
                await send("Input.dispatchMouseEvent", {
                    "type": "mouseReleased", "x": x, "y": y,
                    "button": "left", "clickCount": 1
                })
                await asyncio.sleep(0.8)

                # Buscar el target en el dropdown
                js_select = f"""
                (() => {{
                    const items = Array.from(document.querySelectorAll(
                        '[role="option"], [role="menuitem"], li, [class*="model-item"], [class*="quick-pick"]'
                    ));
                    const target = items.find(el => el.textContent.includes("{target_model}"));
                    if (!target) return JSON.stringify({{found: false, available: items.slice(0,10).map(i => i.textContent.trim().slice(0,50))}});
                    const rect = target.getBoundingClientRect();
                    return JSON.stringify({{found: true, text: target.textContent.trim().slice(0,80), x: rect.x + rect.width/2, y: rect.y + rect.height/2}});
                }})()
                """
                result2 = await send("Runtime.evaluate", {
                    "expression": js_select,
                    "returnByValue": True
                })
                val2 = result2.get("result", {}).get("value", "{}")
                data2 = json.loads(val2) if isinstance(val2, str) else {}
                print(f"  🔎 Resultado búsqueda modelo: {data2}")

                if data2.get("found"):
                    x2, y2 = data2["x"], data2["y"]
                    print(f"  ✅ Seleccionando '{data2['text']}' @ ({x2:.0f}, {y2:.0f})")
                    await send("Input.dispatchMouseEvent", {
                        "type": "mousePressed", "x": x2, "y": y2,
                        "button": "left", "clickCount": 1
                    })
                    await send("Input.dispatchMouseEvent", {
                        "type": "mouseReleased", "x": x2, "y": y2,
                        "button": "left", "clickCount": 1
                    })
                    return True
                else:
                    print(f"  ⚠️  Modelo '{target_model}' no encontrado en dropdown")
                    if "available" in data2:
                        print(f"       Disponibles: {data2['available']}")
            else:
                print("  ⚠️  Botón model picker no encontrado en DOM")
                if "btns" in data:
                    print(f"       Botones encontrados: {data['btns']}")

    except Exception as e:
        print(f"  ❌ Error CDP WebSocket: {e}")

    return False


def run_cdp(target_model: str, port: Optional[int] = None) -> bool:
    """Orquesta la conexión CDP."""
    import asyncio
    if port is None:
        port = find_cdp_port()
    if port is None:
        print("  ❌ No se encontró puerto CDP activo.")
        print("     Para activarlo, cierra VS Code y ábrelo con:")
        print("     code --remote-debugging-port=9222")
        return False
    return asyncio.run(cdp_find_and_click_model(port, target_model))


# ══════════════════════════════════════════════
# MÉTODO 2: Screenshot + OCR
# ══════════════════════════════════════════════

def get_vscode_window_bounds() -> Optional[Tuple[int, int, int, int]]:
    """Obtiene posición y tamaño de la ventana de VS Code via AppleScript."""
    script = '''
    tell application "System Events"
        tell process "Code"
            set w to front window
            set pos to position of w
            set sz to size of w
            return (item 1 of pos as string) & "," & (item 2 of pos as string) & "," & (item 1 of sz as string) & "," & (item 2 of sz as string)
        end tell
    end tell
    '''
    result = subprocess.run(['osascript', '-e', script],
                            capture_output=True, text=True, timeout=5)
    if result.returncode == 0 and result.stdout.strip():
        parts = result.stdout.strip().split(',')
        if len(parts) == 4:
            x, y, w, h = map(int, parts)
            print(f"  📐 VS Code window: ({x},{y}) {w}×{h}")
            return x, y, w, h
    return None


def ocr_find_element(screenshot, search_text: str) -> Optional[Tuple[int, int]]:
    """Usa OCR para encontrar la posición de un texto en pantalla."""
    try:
        import pytesseract
        import numpy as np
        from PIL import Image

        # OCR con datos detallados (bounding boxes)
        ocr_data = pytesseract.image_to_data(screenshot, output_type=pytesseract.Output.DICT,
                                              config='--psm 11')
        # Normalizar búsqueda
        search_lower = search_text.lower()
        n = len(ocr_data["text"])
        # Primero buscar frase completa palabra por palabra en secuencia
        words = [w.lower() for w in search_text.split()]
        for i in range(n - len(words) + 1):
            window = [ocr_data["text"][i+j].lower().strip() for j in range(len(words))]
            if window == words and all(int(ocr_data["conf"][i+j]) > 30 for j in range(len(words))):
                # Centro del grupo de palabras
                x_start = ocr_data["left"][i]
                y_mid = ocr_data["top"][i] + ocr_data["height"][i] // 2
                x_end = ocr_data["left"][i + len(words) - 1] + ocr_data["width"][i + len(words) - 1]
                cx = (x_start + x_end) // 2
                return cx, y_mid
        # Fallback: buscar cada palabra individualmente
        for i in range(n):
            if search_lower in ocr_data["text"][i].lower() and int(ocr_data["conf"][i]) > 30:
                cx = ocr_data["left"][i] + ocr_data["width"][i] // 2
                cy = ocr_data["top"][i] + ocr_data["height"][i] // 2
                return cx, cy
    except Exception as e:
        print(f"  ⚠️  OCR error: {e}")
    return None


def open_model_picker_via_screenshot(target_model: str) -> bool:
    """Toma un screenshot, localiza el model button por OCR y hace click."""
    try:
        import pyautogui
        from PIL import Image, ImageGrab
    except ImportError as e:
        print(f"  ❌ {e}")
        return False

    print("  📸 Tomando screenshot...")
    # Activar VS Code primero
    subprocess.run(['osascript', '-e', 'tell application "Visual Studio Code" to activate'],
                   capture_output=True)
    time.sleep(0.6)

    bounds = get_vscode_window_bounds()
    if bounds:
        x, y, w, h = bounds
        screenshot = ImageGrab.grab(bbox=(x, y, x + w, y + h))
        offset_x, offset_y = x, y
    else:
        screenshot = ImageGrab.grab()
        offset_x, offset_y = 0, 0

    # Guardar para debug
    screenshot.save("/tmp/vscode_screenshot.png")
    print(f"  💾 Screenshot guardado: /tmp/vscode_screenshot.png ({screenshot.size})")

    # Liste de textos a buscar (modelo actual puede variar)
    model_indicators = [
        "Claude Sonnet 4.5", "Claude Sonnet 4.6", "Claude Sonnet",
        "Claude Opus 4.5", "GPT-4o", "o4-mini", "Gemini",
        "claude-", "model"
    ]

    print("  🔍 Buscando model picker via OCR...")
    picker_pos = None
    found_text = None
    for indicator in model_indicators:
        pos = ocr_find_element(screenshot, indicator)
        if pos:
            picker_pos = pos
            found_text = indicator
            print(f"  ✅ Encontrado '{indicator}' en ({pos[0]}, {pos[1]}) relativo a ventana")
            break

    if not picker_pos:
        # Último recurso: buscar cerca del borde inferior del panel de chat
        # El model picker suele estar en la parte inferior izquierda del chat
        if bounds:
            # Área típica: 20-50% del ancho, en los últimos 100px de alto
            picker_pos = (w // 4, h - 60)
            print(f"  ⚠️  OCR no encontró el picker. Usando posición estimada: {picker_pos}")
        else:
            print("  ❌ No se puede determinar la posición del picker")
            return False

    # Coordenadas absolutas en pantalla
    abs_x = offset_x + picker_pos[0]
    abs_y = offset_y + picker_pos[1]
    print(f"  🖱️  Click en model picker @ pantalla ({abs_x}, {abs_y})")

    pyautogui.moveTo(abs_x, abs_y, duration=0.3)
    pyautogui.click()
    time.sleep(0.9)  # Esperar que abra el dropdown

    # Segundo screenshot para encontrar el modelo target en el dropdown
    screenshot2 = ImageGrab.grab(bbox=(offset_x, offset_y, offset_x + (bounds[2] if bounds else 1920),
                                        offset_y + (bounds[3] if bounds else 1080)) if bounds else None)
    screenshot2.save("/tmp/vscode_dropdown.png")
    print(f"  💾 Dropdown screenshot: /tmp/vscode_dropdown.png")

    print(f"  🔍 Buscando '{target_model}' en dropdown...")
    target_pos = ocr_find_element(screenshot2, target_model)

    # Intentar con nombre parcial si no se encuentra
    if not target_pos:
        parts = target_model.split()
        for part_len in range(len(parts), 0, -1):
            partial = " ".join(parts[:part_len])
            target_pos = ocr_find_element(screenshot2, partial)
            if target_pos:
                print(f"  ✅ Encontrado parcial '{partial}' @ {target_pos}")
                break

    if target_pos:
        abs_tx = offset_x + target_pos[0]
        abs_ty = offset_y + target_pos[1]
        print(f"  ✅ Seleccionando '{target_model}' @ ({abs_tx}, {abs_ty})")
        pyautogui.moveTo(abs_tx, abs_ty, duration=0.3)
        pyautogui.click()
        return True
    else:
        print(f"  ❌ '{target_model}' no encontrado en dropdown")
        print("     Revisa /tmp/vscode_dropdown.png para ver el estado actual")
        return False


# ══════════════════════════════════════════════
# MÉTODO 3: AppleScript Accessibility API
# ══════════════════════════════════════════════

def applescript_accessibility_click(target_model: str) -> bool:
    """
    Usa la API de Accesibilidad de macOS para navegar el árbol de UI de VS Code
    y encontrar el model picker por texto.
    """
    # Script que busca recursivamente un botón con texto relacionado a modelo
    script = f'''
    on findModelButton(uiElem, depth)
        if depth > 8 then return missing value
        try
            set elemClass to class of uiElem
            if elemClass is in {{button, menu button, pop up button}} then
                set elemTitle to ""
                try
                    set elemTitle to title of uiElem
                end try
                try
                    set elemTitle to value of uiElem & ""
                end try
                if elemTitle contains "Claude" or elemTitle contains "GPT" or ¬
                   elemTitle contains "Gemini" or elemTitle contains "model" or ¬
                   elemTitle contains "o4" or elemTitle contains "o3" then
                    return uiElem
                end if
            end if
        end try
        try
            set children to UI elements of uiElem
            repeat with child in children
                set found to findModelButton(child, depth + 1)
                if found is not missing value then return found
            end repeat
        end try
        return missing value
    end findModelButton

    tell application "System Events"
        tell process "Code"
            activate
            delay 0.4
            set modelBtn to findModelButton(front window, 0)
            if modelBtn is missing value then
                return "NOT_FOUND"
            end if
            click modelBtn
            delay 0.8
            -- Ahora buscar el item del modelo target en el dropdown
            set modelBtn2 to findModelButton(front window, 0)
            return "PICKER_OPENED"
        end tell
    end tell
    '''
    try:
        result = subprocess.run(['osascript', '-e', script],
                                capture_output=True, text=True, timeout=15)
        output = result.stdout.strip()
        print(f"  AppleScript result: '{output}' (rc={result.returncode})")
        if result.stderr:
            print(f"  stderr: {result.stderr.strip()[:200]}")
        return "PICKER_OPENED" in output or result.returncode == 0
    except Exception as e:
        print(f"  ❌ AppleScript error: {e}")
        return False


def applescript_keyboard_select(target_model: str) -> bool:
    """Abre el picker con Cmd+, o navegación, luego selecciona por teclado."""
    # Intentar usar el quick input (Cmd+Shift+P) para buscar el modelo
    script = f'''
    tell application "System Events"
        tell process "Code"
            activate
            delay 0.3
            -- Abrir command palette
            keystroke "p" using {{command down, shift down}}
            delay 0.5
            -- Escribir el nombre del modelo a buscar
            keystroke "chat model"
            delay 0.5
            -- Enter para ejecutar
            keystroke return
            delay 0.8
        end tell
    end tell
    '''
    try:
        result = subprocess.run(['osascript', '-e', script],
                                capture_output=True, text=True, timeout=10)
        return result.returncode == 0
    except Exception as e:
        print(f"  ❌ {e}")
        return False


# ══════════════════════════════════════════════
# ORQUESTADOR PRINCIPAL
# ══════════════════════════════════════════════

def main():
    parser = argparse.ArgumentParser(description="VS Code Copilot Chat Model Picker")
    parser.add_argument("model", nargs="?", default=DEFAULT_TARGET_MODEL,
                        help=f"Modelo a seleccionar (default: '{DEFAULT_TARGET_MODEL}')")
    parser.add_argument("--cdp-port", type=int, default=None,
                        help="Puerto CDP específico (default: autodetect)")
    parser.add_argument("--method", choices=["cdp", "ocr", "accessibility", "all"],
                        default="all", help="Método a usar (default: all)")
    args = parser.parse_args()

    target_model = args.model
    print(f"\n🎯 VS Code Model Picker — Seleccionando: '{target_model}'")
    print("=" * 60)

    # ─── MÉTODO 1: CDP ───
    if args.method in ("cdp", "all"):
        print("\n[1/3] 🔌 CDP (Chrome DevTools Protocol)")
        if run_cdp(target_model, args.cdp_port):
            print(f"\n✨ ¡Modelo '{target_model}' seleccionado via CDP!\n")
            return 0
        print("  → CDP no disponible, intentando siguiente método...")

    # ─── MÉTODO 2: Screenshot + OCR ───
    if args.method in ("ocr", "all"):
        print("\n[2/3] 📸 Screenshot + OCR")
        if open_model_picker_via_screenshot(target_model):
            print(f"\n✨ ¡Modelo '{target_model}' seleccionado via OCR!\n")
            return 0
        print("  → OCR no encontró el modelo, intentando siguiente método...")

    # ─── MÉTODO 3: Accessibility API ───
    if args.method in ("accessibility", "all"):
        print("\n[3/3] ♿ AppleScript Accessibility API")
        if applescript_accessibility_click(target_model):
            print(f"\n✨ Picker abierto via Accessibility. Verifica visualmente.\n")
            return 0

    print(f"\n❌ No se pudo seleccionar '{target_model}'.")
    print("   Sugerencias:")
    print("   • Asegúrate de que el panel de Copilot Chat esté visible")
    print(f"   • Para CDP: cierra VS Code y ábrelo con: code --remote-debugging-port=9222")
    print("   • Verifica /tmp/vscode_screenshot.png y /tmp/vscode_dropdown.png")
    return 1


if __name__ == "__main__":
    sys.exit(main())
