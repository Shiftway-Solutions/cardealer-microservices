#!/usr/bin/env python3
"""Scan the bottom of VS Code chat panel to find the model picker location."""
from PIL import Image, ImageGrab
import pytesseract
import subprocess
import time

# Activate VS Code
subprocess.run(['osascript', '-e', 'tell application "Visual Studio Code" to activate'],
               capture_output=True)
time.sleep(0.5)

# Get window bounds
script = """
tell application "System Events"
    tell process "Code"
        set w to front window
        set pos to position of w
        set sz to size of w
        return (item 1 of pos as string) & "," & (item 2 of pos as string) & "," & (item 1 of sz as string) & "," & (item 2 of sz as string)
    end tell
end tell
"""
r = subprocess.run(['osascript', '-e', script],
    capture_output=True, text=True, timeout=5)
x, y, w, h = map(int, r.stdout.strip().split(','))
print(f"Window: ({x},{y}) {w}x{h}")

# Full screenshot for reference
full = ImageGrab.grab(bbox=(x, y, x + w, y + h))
full.save("/tmp/vscode_full.png")

# Bottom strip: last 80px of the window, right half (where chat is)
bottom = ImageGrab.grab(bbox=(x + w // 2, y + h - 80, x + w, y + h))
bottom.save("/tmp/vscode_bottom.png")
print(f"Bottom-right: {bottom.size}")

# OCR the bottom strip
data = pytesseract.image_to_data(bottom, output_type=pytesseract.Output.DICT)
n = len(data["text"])
for i in range(n):
    txt = data["text"][i].strip()
    conf = int(data["conf"][i])
    if txt and conf > 15:
        cx = data["left"][i] + data["width"][i] // 2
        cy = data["top"][i] + data["height"][i] // 2
        # Convert to absolute screen coords
        abs_x = x + w // 2 + cx
        abs_y = y + h - 80 + cy
        print(f'  [{conf:3d}] "{txt}" rel=({cx},{cy}) abs=({abs_x},{abs_y})')

# Also scan the chat input area (bottom 50px, right 60% of window)
chat_area = ImageGrab.grab(bbox=(x + int(w * 0.6), y + h - 50, x + w, y + h))
chat_area.save("/tmp/vscode_chat_input.png")
data2 = pytesseract.image_to_data(chat_area, output_type=pytesseract.Output.DICT)
n2 = len(data2["text"])
print("\nChat input area OCR:")
for i in range(n2):
    txt = data2["text"][i].strip()
    conf = int(data2["conf"][i])
    if txt and conf > 15:
        cx = data2["left"][i] + data2["width"][i] // 2
        cy = data2["top"][i] + data2["height"][i] // 2
        abs_x = x + int(w * 0.6) + cx
        abs_y = y + h - 50 + cy
        print(f'  [{conf:3d}] "{txt}" rel=({cx},{cy}) abs=({abs_x},{abs_y})')
