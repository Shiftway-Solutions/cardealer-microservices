#!/usr/bin/env python3
from __future__ import annotations
"""
change_model.py — Cambia el modelo de GitHub Copilot Chat en VS Code.

Flujo PRINCIPAL (OCR + click directo en el dropdown inline):
  1. Lee los modelos disponibles desde /tmp/copilot_live_models.json
     (exportado por la extensión Copilot Model Cycler al arrancar VS Code)
  2. Screenshot estado INICIAL
  3. OCR para encontrar el botón del model picker en la toolbar inferior del chat
  4. pyautogui.click() en el picker → abre dropdown WebView inline
  5. screencapture 2x del área sobre el picker → OCR para encontrar el modelo
  6. Si el modelo está visible: click directo en sus coordenadas
  7. Si no está: click en "Other Models" → dropdown expandido → OCR + click
  8. Screenshot de CONFIRMACIÓN

IMPORTANTE: El dropdown del Copilot Chat es un menú WebView inline, NO un
VS Code Quick Pick. No se puede filtrar escribiendo. Hay que hacer click
en el item específico usando sus coordenadas OCR.

NO envía ningún prompt. Solo cambia el modelo y confirma con foto.

Uso:
  python3 change_model.py                  # → picker interactivo con modelos live
  python3 change_model.py "Claude Sonnet 4.6"
  python3 change_model.py "GPT-4o"
  python3 change_model.py --list           # Screenshot + modelo actual
  python3 change_model.py --models         # Lista modelos disponibles en tu plan
"""

import subprocess
import time
import sys
import json
import argparse
from datetime import datetime
from pathlib import Path

# ─── CONFIG ────────────────────────────────────────────────────────────────────
LIVE_MODELS_PATH = Path("/tmp/copilot_live_models.json")
SCREENSHOT_DIR = Path("/tmp/change_model_screenshots")
SCREENSHOT_DIR.mkdir(exist_ok=True)

# Timestamp para nombrar archivos de esta sesión
SESSION_TS = datetime.now().strftime("%H%M%S")

# Palabras clave que identifican el model picker (texto visible en el botón)
MODEL_KEYWORDS = ["claude", "gpt", "opus", "sonnet", "haiku", "gemini", "o1", "o3", "4o", "copilot"]

# Nota Retina: PIL.ImageGrab.grab(bbox=...) devuelve imagen en resolución LÓGICA (1x)
# independientemente de si la pantalla es Retina. El scale se calcula dinámicamente
# comparando el tamaño de la imagen PIL vs el tamaño lógico del bbox.
RETINA_SCALE = 2.0  # Solo afecta screenshots SIN bbox (screencap completo)


# ─── MODELOS LIVE ──────────────────────────────────────────────────────────────

def load_live_models() -> list[dict]:
    """
    Lee los modelos disponibles en tu plan exportados por la extensión
    Copilot Model Cycler a /tmp/copilot_live_models.json.

    Si el archivo no existe:
      - Intenta disparar el comando de exportación via AppleScript
      - Espera hasta 5 segundos a que el archivo aparezca
      - Si sigue sin existir, devuelve lista vacía y avisa al usuario

    El archivo tiene el formato:
      { "exported_at": "...", "count": N, "models": [{id, name, family, vendor}, ...] }
    """
    def _try_trigger_export():
        """Dispara modelCycler.exportLiveModels via command palette."""
        script = '''
tell application "Visual Studio Code" to activate
delay 0.4
tell application "System Events"
    tell process "Code"
        keystroke "p" using {shift down, command down}
        delay 0.6
        keystroke ">Copilot Cycler: Exportar modelos live"
        delay 0.8
        key code 36
    end tell
end tell
'''
        subprocess.run(["osascript", "-e", script], capture_output=True, timeout=8)

    if not LIVE_MODELS_PATH.exists():
        print(f"  ⚠️  {LIVE_MODELS_PATH} no encontrado — disparando exportación...")
        _try_trigger_export()
        # Esperar hasta 5s a que aparezca
        for _ in range(10):
            time.sleep(0.5)
            if LIVE_MODELS_PATH.exists():
                break

    if not LIVE_MODELS_PATH.exists():
        print(f"  ❌ No se pudo obtener modelos live.")
        print(f"     Asegúrate de que la extensión 'Copilot Model Cycler' esté instalada")
        print(f"     y VS Code esté abierto con sesión de Copilot activa.")
        print(f"     Alternativa: ⌘⇧P → 'Copilot Cycler: Exportar modelos live'")
        return []

    try:
        data = json.loads(LIVE_MODELS_PATH.read_text())
        models = data.get("models", [])
        exported_at = data.get("exported_at", "?")
        print(f"  ✅ {len(models)} modelos live cargados (exportado: {exported_at[:19]})")
        return models
    except (json.JSONDecodeError, KeyError) as e:
        print(f"  ❌ Error leyendo {LIVE_MODELS_PATH}: {e}")
        return []


def pick_model_interactive(live_models: list[dict]) -> str | None:
    """
    Muestra un selector interactivo en terminal con los modelos disponibles.
    Retorna el nombre del modelo seleccionado, o None si el usuario cancela.
    """
    if not live_models:
        print("  ❌ Sin modelos disponibles para seleccionar")
        return None

    print("\n  Modelos disponibles en tu plan de Copilot:")
    print("  " + "─" * 50)
    for i, m in enumerate(live_models, 1):
        name = m.get("name") or m.get("id", "?")
        mid = m.get("id", "")
        tokens = m.get("maxInputTokens", 0)
        token_str = f"  [{tokens:,} tokens]" if tokens else ""
        print(f"  {i:>2}. {name:<40} ({mid}){token_str}")
    print("   0. Cancelar")
    print("  " + "─" * 50)

    while True:
        try:
            raw = input("  Selecciona número: ").strip()
            if raw == "0" or raw.lower() in ("q", "exit", "cancel", ""):
                return None
            idx = int(raw) - 1
            if 0 <= idx < len(live_models):
                selected = live_models[idx]
                name = selected.get("name") or selected.get("id", "")
                print(f"  → Seleccionado: {name}")
                return name
            print(f"  ⚠️  Número fuera de rango (1-{len(live_models)})")
        except (ValueError, EOFError):
            print("  ⚠️  Ingresa un número válido")

# ─── UTILIDADES ────────────────────────────────────────────────────────────────

def applescript(script: str) -> tuple[str, int]:
    r = subprocess.run(["osascript", "-e", script], capture_output=True, text=True, timeout=15)
    return r.stdout.strip(), r.returncode


def activate_vscode():
    applescript('tell application "Visual Studio Code" to activate')
    time.sleep(0.4)


def get_vscode_window() -> tuple[int, int, int, int]:
    """Retorna (x, y, width, height) de la ventana frontal de VS Code."""
    out, rc = applescript("""
tell application "System Events"
    tell process "Code"
        set w to front window
        set pos to position of w
        set sz to size of w
        return (item 1 of pos as string) & "," & (item 2 of pos as string) & "," & (item 1 of sz as string) & "," & (item 2 of sz as string)
    end tell
end tell
""")
    if rc != 0 or not out:
        raise RuntimeError("No se pudo obtener la posición de la ventana de VS Code")
    parts = list(map(int, out.strip().split(",")))
    return parts[0], parts[1], parts[2], parts[3]


def screenshot(bbox=None, label="") -> tuple:
    """Captura screenshot. bbox=(x1,y1,x2,y2) en coords de pantalla."""
    from PIL import ImageGrab
    img = ImageGrab.grab(bbox=bbox)
    ts = datetime.now().strftime("%H%M%S%f")[:9]
    fname = SCREENSHOT_DIR / f"{SESSION_TS}_{ts}_{label}.png"
    img.save(str(fname))
    print(f"  📸 Screenshot guardado: {fname}")
    return img, str(fname)


def ocr_find_all(img, keywords: list[str]) -> list[tuple[int, int, str, float]]:
    """
    Busca keywords en la imagen con OCR.
    Retorna lista de (cx, cy, texto_encontrado, confianza) en coords RELATIVAS a img.
    """
    import pytesseract
    data = pytesseract.image_to_data(img, output_type=pytesseract.Output.DICT)
    results = []
    for i in range(len(data["text"])):
        txt = data["text"][i].strip()
        conf = float(data["conf"][i])
        if not txt or conf < 10:
            continue
        txt_lower = txt.lower()
        for kw in keywords:
            if kw.lower() in txt_lower:
                cx = data["left"][i] + data["width"][i] // 2
                cy = data["top"][i] + data["height"][i] // 2
                results.append((cx, cy, txt, conf))
                break
    return results


def click_at(abs_x: int, abs_y: int, label: str = ""):
    """Hace click en coordenadas absolutas de pantalla via AppleScript."""
    print(f"  🖱️  Click en ({abs_x}, {abs_y}) {label}")
    applescript(f"""
tell application "System Events"
    tell process "Code"
        click at {{{abs_x}, {abs_y}}}
    end tell
end tell
""")


# ─── PASO 1: Encontrar el botón del model picker ────────────────────────────────

def find_model_picker_button(wx: int, wy: int, ww: int, wh: int) -> tuple[int, int, str] | None:
    """
    Escanea la toolbar inferior del chat para encontrar el botón con el modelo actual.
    Retorna (abs_x, abs_y, texto_encontrado) en coordenadas LÓGICAS (1x), o None.

    RETINA FIX: PIL captura a resolución física (2x). Las coords OCR se dividen
    por RETINA_SCALE antes de sumar la origen del bbox (que está en coords lógicas).
    """
    print("\n[PASO 1] Buscando el model picker en la toolbar del chat...")

    # Escanear distintas franjas verticales desde el fondo (coordenadas lógicas)
    scan_zones = [
        (90, 55, "zona_principal"),
        (100, 60, "zona_plus10"),
        (75, 45, "zona_menos10"),
        (110, 70, "zona_plus20"),
        (60, 38, "zona_menos20"),
        (120, 80, "zona_plus30"),
    ]

    for top_off, bot_off, zone_label in scan_zones:
        x1 = wx + ww // 3        # Buscar en los 2/3 derechos
        y1 = wy + wh - top_off
        x2 = wx + ww
        y2 = wy + wh - bot_off

        if y1 >= y2:
            continue

        strip, _ = screenshot(bbox=(x1, y1, x2, y2), label=f"scan_{zone_label}")
        hits = ocr_find_all(strip, MODEL_KEYWORDS)

        if hits:
            best = max(hits, key=lambda h: h[3])
            rel_cx, rel_cy, txt, conf = best
            # Scale dinámico: img.width / ancho_lógico_del_bbox
            # bbox con ImageGrab.grab(bbox=...) → iamgen 1x (scale≈1.0)
            scale_x = strip.width  / (x2 - x1)
            scale_y = strip.height / (y2 - y1)
            abs_cx = x1 + int(rel_cx / scale_x)
            abs_cy = y1 + int(rel_cy / scale_y)
            print(f"  ✅ Encontrado '{txt}' (conf={conf:.0f}) img_scale={scale_x:.2f}x zona={zone_label}: lógico=({abs_cx},{abs_cy})")
            return abs_cx, abs_cy, txt

    print("  ❌ Model picker no encontrado en ninguna zona de scan")
    return None


# ─── PASO 2: Encontrar el modelo target en el dropdown ──────────────────────────

def find_target_in_dropdown(
    wx: int, wy: int, ww: int, wh: int,
    picker_abs_y: int, target: str
) -> tuple[int, int, str] | None:
    """
    Captura el área del Quick Pick (parte SUPERIOR de la ventana) para encontrar
    el modelo target.

    RETINA FIX: coords OCR divididas por RETINA_SCALE antes de añadir origen del bbox.
    """
    print(f"\n[PASO 3] Buscando '{target}' en el Quick Pick (parte superior)...")

    # El Quick Pick de VS Code aparece centrado en la parte SUPERIOR
    capture_zones = [
        (30,  350, "quickpick_top"),
        (30,  500, "quickpick_medio"),
        (30,  650, "quickpick_amplio"),
        (0,   wh,  "full_window"),
    ]

    target_parts = [p for p in target.lower().split() if p]

    for y1r, y2r, zone_label in capture_zones:
        x1 = wx + ww // 4
        y1 = wy + y1r
        x2 = wx + ww * 3 // 4
        y2 = wy + min(y2r, wh)

        popup_img, _ = screenshot(bbox=(x1, y1, x2, y2), label=f"dropdown_{zone_label}")
        hits = ocr_find_all(popup_img, target_parts)

        if hits:
            print(f"  ✅ Hits en zona {zone_label}: {[(h[2], round(h[3])) for h in hits]}")
            best = max(hits, key=lambda h: h[3])
            rel_cx, rel_cy, txt, conf = best
            scale_x = popup_img.width  / (x2 - x1)
            scale_y = popup_img.height / (y2 - y1)
            abs_cx = x1 + int(rel_cx / scale_x)
            abs_cy = y1 + int(rel_cy / scale_y)
            print(f"  🎯 Target '{txt}' img_scale={scale_x:.2f}x lógico=({abs_cx},{abs_cy})")
            return abs_cx, abs_cy, txt

    # Fallback: listar modelos visibles
    print("  ⚠️  No encontrado con nombre específico, listando modelos visibles...")
    x1, y1 = wx + ww // 4, wy + 30
    x2, y2 = wx + ww * 3 // 4, wy + 500
    popup_img, _ = screenshot(bbox=(x1, y1, x2, y2), label="dropdown_fallback_top")
    hits = ocr_find_all(popup_img, MODEL_KEYWORDS)
    if hits:
        print(f"  Modelos visibles en Quick Pick: {list(set(h[2] for h in hits))}")
    else:
        full_img, _ = screenshot(bbox=(wx, wy, wx + ww, wy + wh), label="dropdown_fallback_full")
        hits = ocr_find_all(full_img, MODEL_KEYWORDS)
        if hits:
            print(f"  Modelos en ventana completa: {list(set(h[2] for h in hits))}")
    return None


# ─── ESTRATEGIA: OCR para picker → click → teclado para selección ──────────────

def _ocr_region_2x(x1: int, y1: int, x2: int, y2: int, fname: str):
    """
    screencapture -R retorna imagen 2x (Retina). Todas las coords OCR
    deben dividirse entre 2 para obtener coordenadas lógicas.
    """
    from PIL import Image
    w = max(1, x2 - x1)
    h = max(1, y2 - y1)
    subprocess.run(
        ["screencapture", "-R", f"{x1},{y1},{w},{h}", "-x", fname],
        capture_output=True, check=True
    )
    return Image.open(fname)


def _ocr_find_words_2x(img, words: list[str], x1_abs: int, y1_abs: int,
                        min_conf: float = 12.0) -> list[tuple[int, int, str, float]]:
    """
    Busca palabras en una imagen 2x.
    Retorna lista de (abs_logical_x, abs_logical_y, texto, confianza).
    Los pixels OCR se dividen entre 2 para convertir de físico a lógico.
    """
    import pytesseract
    data = pytesseract.image_to_data(img, output_type=pytesseract.Output.DICT)
    results = []
    for i in range(len(data["text"])):
        txt = data["text"][i].strip()
        conf = float(data["conf"][i])
        if not txt or conf < min_conf:
            continue
        txt_lower = txt.lower()
        for w in words:
            if w.lower() in txt_lower:
                phys_cx = data["left"][i] + data["width"][i] // 2
                phys_cy = data["top"][i] + data["height"][i] // 2
                log_x = x1_abs + phys_cx / 2
                log_y = y1_abs + phys_cy / 2
                results.append((int(log_x), int(log_y), txt, conf))
                break
    return results


def change_model_via_ocr_click(picker_x: int, picker_y: int, target: str,
                                win: tuple[int, int, int, int]) -> bool:
    """
    Flujo completo OCR + click directo en el dropdown inline del Copilot Chat:

      1. pyautogui.click() en el model picker → abre dropdown WebView inline
      2. screencapture 2x del área SOBRE el picker → OCR para encontrar el modelo
      3. Si está en la lista principal → pyautogui.click() en sus coordenadas
      4. Si NO está → click en 'Other Models' → dropdown expandido → OCR + click
      5. Screenshot de confirmación

    El dropdown NO es un Quick Pick filtrable. Escribir texto cierra el dropdown
    y lo escribe en el chat. Hay que hacer click en el item directamente.
    """
    import pyautogui
    from PIL import Image
    pyautogui.FAILSAFE = False

    wx, wy, ww, wh = win
    target_words = [w for w in target.lower().split() if len(w) > 2]
    print(f"\n[OCR-CLICK] Click picker({picker_x},{picker_y}) → OCR dropdown → click '{target}'")

    # ── Activar VS Code ──────────────────────────────────────────────────────
    applescript('tell application "Visual Studio Code" to activate')
    time.sleep(0.5)

    # ── Phase 1: Abrir dropdown ───────────────────────────────────────────────
    pyautogui.moveTo(picker_x, picker_y, duration=0.2)
    time.sleep(0.15)
    pyautogui.click()
    time.sleep(1.5)

    # ── Phase 2: Screenshot del área del dropdown (SOBRE el picker) ──────────
    dd_x1 = max(wx, picker_x - 200)
    dd_y1 = max(wy, picker_y - 320)
    dd_x2 = min(wx + ww, picker_x + 200)
    dd_y2 = picker_y - 5
    dd_fname = "/tmp/cm_dropdown_main.png"

    dd_img = _ocr_region_2x(dd_x1, dd_y1, dd_x2, dd_y2, dd_fname)
    ts = datetime.now().strftime("%H%M%S%f")[:9]
    saved = SCREENSHOT_DIR / f"{SESSION_TS}_{ts}_02a_dropdown.png"
    dd_img.save(str(saved))
    print(f"  📸 Dropdown OCR screenshot: {saved}")

    # ── Phase 3: Buscar modelo target en dropdown principal ───────────────────
    hits = _ocr_find_words_2x(dd_img, target_words, dd_x1, dd_y1)
    if hits:
        best = max(hits, key=lambda h: h[3])
        abs_x, abs_y, found_txt, conf = best
        print(f"  ✅ Target '{found_txt}' encontrado directamente (conf={conf:.0f}) → click({abs_x},{abs_y})")
        pyautogui.moveTo(abs_x, abs_y, duration=0.2)
        time.sleep(0.15)
        pyautogui.click()
        return True

    print(f"  ⚠️  '{target}' no en dropdown principal — intentando expandir 'Other Models'...")

    # ── Phase 4: Click en "Other Models" para expandir ───────────────────────
    other_hits = _ocr_find_words_2x(dd_img, ["other", "more", "models"], dd_x1, dd_y1)
    if not other_hits:
        # Listar lo que hay disponible
        avail = _ocr_find_words_2x(dd_img, ["claude", "gpt", "auto", "sonnet", "haiku",
                                              "gemini", "o1", "o3", "4o", "manage"], dd_x1, dd_y1)
        print(f"  Modelos visibles: {list(set(h[2] for h in avail))}")
        print("  ❌ 'Other Models' no encontrado — dropdown puede estar cerrado")
        return False

    # click en "Other Models" (el que tiene mayor conf)
    other_best = max(other_hits, key=lambda h: h[3])
    ox, oy, otxt, oconf = other_best
    print(f"  🖱️  Click en '{otxt}' (conf={oconf:.0f}) → ({ox},{oy})")
    pyautogui.moveTo(ox, oy, duration=0.2)
    time.sleep(0.15)
    pyautogui.click()
    time.sleep(1.5)

    # ── Phase 5: OCR del dropdown expandido ──────────────────────────────────
    dd_y1_exp = max(wy, picker_y - 500)   # más espacio arriba tras expansión
    dd_fname2 = "/tmp/cm_dropdown_expanded.png"
    dd_img2 = _ocr_region_2x(dd_x1, dd_y1_exp, dd_x2, dd_y2, dd_fname2)
    ts2 = datetime.now().strftime("%H%M%S%f")[:9]
    saved2 = SCREENSHOT_DIR / f"{SESSION_TS}_{ts2}_02b_dropdown_expanded.png"
    dd_img2.save(str(saved2))
    print(f"  📸 Expanded dropdown: {saved2}")

    hits2 = _ocr_find_words_2x(dd_img2, target_words, dd_x1, dd_y1_exp)
    if hits2:
        best2 = max(hits2, key=lambda h: h[3])
        abs_x2, abs_y2, found_txt2, conf2 = best2
        print(f"  ✅ Target '{found_txt2}' en expanded (conf={conf2:.0f}) → click({abs_x2},{abs_y2})")
        pyautogui.moveTo(abs_x2, abs_y2, duration=0.2)
        time.sleep(0.15)
        pyautogui.click()
        return True

    # Listar modelos disponibles para orientar al usuario — primero desde el JSON live
    live_models = load_live_models()
    if live_models:
        live_names = [m.get("name") or m.get("id", "") for m in live_models]
        print(f"\n  ❌ '{target}' no visible en el dropdown (puede estar en 'Other Models').")
        print(f"  Modelos disponibles en tu plan:")
        for n in live_names:
            print(f"    • {n}")
        print(f"  Usa: python3 change_model.py \"<nombre_exacto>\"")
    else:
        all_kws = ["claude", "gpt", "auto", "sonnet", "haiku", "gemini", "o1", "o3", "4o", "manage"]
        avail2 = _ocr_find_words_2x(dd_img2, all_kws, dd_x1, dd_y1_exp)
        available_names = sorted(set(h[2] for h in avail2))
        print(f"\n  ❌ '{target}' no disponible en este plan de Copilot.")
        print(f"  Modelos detectados por OCR: {available_names}")
        print(f"  Usa: python3 change_model.py \"<modelo_disponible>\"")

    # ESC para cerrar el dropdown
    applescript('tell application "System Events" to tell process "Code" to key code 53')
    return False


# ─── FLUJO PRINCIPAL ────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Cambia el modelo de Copilot Chat en VS Code")
    parser.add_argument("target", nargs="?", default=None, help="Nombre del modelo (vacío = selector interactivo)")
    parser.add_argument("--list", action="store_true", help="Solo captura screenshot y muestra el estado actual")
    parser.add_argument("--models", action="store_true", help="Lista los modelos disponibles en tu plan y sale")
    args = parser.parse_args()

    # ── Carga modelos live (siempre, para todas las rutas) ───────────────────
    print("[MODELOS] Cargando modelos disponibles en tu plan...")
    live_models = load_live_models()

    # ── Modo --models: solo listar y salir ───────────────────────────────────
    if args.models:
        if live_models:
            print(f"\n  {len(live_models)} modelos disponibles en tu plan de Copilot:\n")
            for i, m in enumerate(live_models, 1):
                name = m.get("name") or m.get("id", "?")
                mid = m.get("id", "")
                tokens = m.get("maxInputTokens", 0)
                token_str = f"  [{tokens:,} tokens]" if tokens else ""
                print(f"  {i:>2}. {name:<45} id={mid}{token_str}")
        else:
            print("  Sin modelos disponibles — ¿extensión instalada y VS Code abierto?")
        return 0

    # ── Determinar target ────────────────────────────────────────────────────
    target = args.target
    only_list = args.list

    if not only_list and target is None:
        # Selector interactivo usando los modelos live
        if live_models:
            target = pick_model_interactive(live_models)
            if target is None:
                print("Cancelado.")
                return 0
        else:
            print("❌ Sin modelos live disponibles. Usa: python3 change_model.py \"Nombre del modelo\"")
            return 1

    print("=" * 60)
    print(f"🤖 change_model.py")
    print(f"   Target    : {target if not only_list else '(solo listar estado)'}")
    print(f"   Estrategia: OCR picker → click dropdown → click modelo")
    print(f"   Output    : {SCREENSHOT_DIR}")
    print("=" * 60)

    # ── Activar VS Code ──────────────────────────────────────────────────────
    print("\n[INIT] Activando VS Code...")
    activate_vscode()

    # ── Obtener ventana ──────────────────────────────────────────────────────
    wx, wy, ww, wh = get_vscode_window()
    print(f"  Ventana: ({wx},{wy}) {ww}x{wh}")

    # ── Screenshot INICIAL ────────────────────────────────────────────────────
    print("\n[SCREENSHOT] Capturando estado INICIAL...")
    initial_img, initial_path = screenshot(
        bbox=(wx, wy, wx + ww, wy + wh),
        label="00_INICIAL"
    )

    if only_list:
        print("\n[INFO] Modo --list: analizando modelo actual...")
        hits = ocr_find_all(initial_img, MODEL_KEYWORDS)
        if hits:
            seen = {}
            for cx, cy, txt, conf in hits:
                if txt not in seen or conf > seen[txt][2]:
                    seen[txt] = (cx, cy, conf)
            print(f"  Modelos detectados por OCR:")
            for txt, (cx, cy, conf) in sorted(seen.items(), key=lambda x: -x[1][2]):
                print(f"    '{txt}' conf={conf:.0f} imagen=({cx},{cy})")
        else:
            print("  No se detectó texto de modelo con OCR")
        if live_models:
            print(f"\n  Modelos disponibles en tu plan ({len(live_models)}):")
            for m in live_models:
                print(f"    • {m.get('name') or m.get('id', '?')}")
        print(f"\n  open {SCREENSHOT_DIR}")
        return 0

    # ════════════════════════════════════════════════════════
    # FLUJO PRINCIPAL: OCR picker → click → OCR dropdown → click modelo
    # ════════════════════════════════════════════════════════
    print("\n[FLUJO] OCR picker → click dropdown → click modelo...")

    result = find_model_picker_button(wx, wy, ww, wh)
    if not result:
        print("\n❌ FALLO: No se encontró el botón del model picker.")
        print("   Asegúrate de que el panel de Copilot Chat esté abierto.")
        print(f"   ver: open {SCREENSHOT_DIR}")
        return 1

    picker_abs_x, picker_abs_y, current_model_text = result
    screenshot(bbox=(wx, wy, wx + ww, wy + wh), label="01_picker_detectado")

    # Verificar si ya está el modelo correcto
    target_kws = [p for p in target.lower().split() if len(p) > 1]
    already_selected = bool(target_kws) and all(kw in current_model_text.lower() for kw in target_kws)
    if already_selected:
        print(f"\n✅ '{current_model_text}' ya coincide con '{target}'. Sin cambio necesario.")
        screenshot(bbox=(wx, wy, wx + ww, wy + wh), label="02_CONFIRMACION_ya_correcto")
        return 0

    ok = change_model_via_ocr_click(picker_abs_x, picker_abs_y, target,
                                    win=(wx, wy, ww, wh))
    time.sleep(1.2)

    # Screenshot de confirmación
    print("\n[SCREENSHOT] Capturando estado FINAL (post-cambio)...")
    confirm_img, confirm_path = screenshot(
        bbox=(wx, wy, wx + ww, wy + wh),
        label="03_CONFIRMACION_final"
    )

    # Verificar el cambio con OCR en la toolbar inferior
    post_result = find_model_picker_button(wx, wy, ww, wh)
    if post_result:
        _, _, new_model_text = post_result
        target_parts = [p for p in target.lower().split() if len(p) > 2]
        success = any(p in new_model_text.lower() for p in target_parts)
        print(f"\n{'=' * 60}")
        print(f"  Modelo ANTES  : {current_model_text}")
        print(f"  Modelo DESPUÉS: {new_model_text}")
        if success:
            print(f"  ✅ CAMBIO EXITOSO — '{target}' activo")
        else:
            print(f"  ⚠️  Picker muestra '{new_model_text}' — verificar imagen")
        print(f"  📸 {confirm_path}")
        print(f"{'=' * 60}")
    else:
        print(f"\n[INFO] OCR no leyó el picker post-cambio — ver: {confirm_path}")

    print(f"\n📁 Screenshots en: {SCREENSHOT_DIR}")
    print(f"   open {SCREENSHOT_DIR}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
