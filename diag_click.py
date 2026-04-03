#!/usr/bin/env python3
"""Diagnóstico: tomar screenshots antes/después del click para ver si el Quick Pick abre."""
import subprocess
import time
from PIL import ImageGrab
from pathlib import Path

Path("/tmp/diag_screenshots").mkdir(exist_ok=True)


def applescript(s):
    r = subprocess.run(["osascript", "-e", s], capture_output=True, text=True, timeout=15)
    return r.stdout.strip(), r.returncode


# Obtener ventana
out, _ = applescript("""
tell application "System Events"
    tell process "Code"
        set w to front window
        set pos to position of w
        set sz to size of w
        return (item 1 of pos as string) & "," & (item 2 of pos as string) & "," & (item 1 of sz as string) & "," & (item 2 of sz as string)
    end tell
end tell
""")
wx, wy, ww, wh = map(int, out.split(","))
print(f"Ventana: ({wx},{wy}) {ww}x{wh}")

# Activar VS Code
applescript('tell application "Visual Studio Code" to activate')
time.sleep(0.8)

# Screenshot inicial
img0 = ImageGrab.grab()
print(f"Screen size (PIL): {img0.size}")
img0.save("/tmp/diag_screenshots/00_antes.png")
print("00_antes.png guardado")

# Click en el model picker (coord encontrada por OCR)
click_x, click_y = 971, 856
print(f"\nClickeando en ({click_x}, {click_y})...")
applescript(f"""
tell application "System Events"
    tell process "Code"
        click at {{{click_x}, {click_y}}}
    end tell
end tell
""")

# Screenshots rápidos para atrapar el Quick Pick
for delay, fname in [(0.15, "01_0.15s"), (0.4, "02_0.5s"), (0.8, "03_1.3s"), (0.7, "04_2.0s")]:
    time.sleep(delay)
    img = ImageGrab.grab()
    path = f"/tmp/diag_screenshots/{fname}.png"
    img.save(path)
    print(f"  {fname}.png guardado")

print("\nDone. Screenshots en /tmp/diag_screenshots/")
print("Presiona Enter para ver los screenshots...")
