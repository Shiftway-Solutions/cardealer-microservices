#!/usr/bin/env python3
"""Click model picker and select a specific model. Uses precise coordinates from OCR scan."""
import subprocess
import time
import sys

TARGET = sys.argv[1] if len(sys.argv) > 1 else "Claude Haiku 4.5"

def activate_vscode():
    subprocess.run(['osascript', '-e', 'tell application "Visual Studio Code" to activate'],
                   capture_output=True)
    time.sleep(0.4)

def get_window():
    r = subprocess.run(['osascript', '-e', '''
tell application "System Events"
    tell process "Code"
        set w to front window
        set pos to position of w
        set sz to size of w
        return (item 1 of pos as string) & "," & (item 2 of pos as string) & "," & (item 1 of sz as string) & "," & (item 2 of sz as string)
    end tell
end tell
'''], capture_output=True, text=True, timeout=5)
    return tuple(map(int, r.stdout.strip().split(',')))

def ocr_find(img, search):
    import pytesseract
    data = pytesseract.image_to_data(img, output_type=pytesseract.Output.DICT)
    n = len(data["text"])
    words = search.lower().split()
    # Multi-word search
    for i in range(n - len(words) + 1):
        window = [data["text"][i+j].lower().strip() for j in range(len(words))]
        if all(w in ww for w, ww in zip(words, window)):
            x1 = data["left"][i]
            xe = data["left"][i+len(words)-1] + data["width"][i+len(words)-1]
            cy = data["top"][i] + data["height"][i] // 2
            return (x1 + xe) // 2, cy
    # Single word fallback
    for i in range(n):
        if search.lower().split()[0] in data["text"][i].lower() and int(data["conf"][i]) > 20:
            return data["left"][i] + data["width"][i] // 2, data["top"][i] + data["height"][i] // 2
    return None

def ocr_find_all(img, search):
    """Find ALL occurrences of search text."""
    import pytesseract
    data = pytesseract.image_to_data(img, output_type=pytesseract.Output.DICT)
    n = len(data["text"])
    results = []
    search_lower = search.lower()
    for i in range(n):
        if search_lower in data["text"][i].lower() and int(data["conf"][i]) > 20:
            cx = data["left"][i] + data["width"][i] // 2
            cy = data["top"][i] + data["height"][i] // 2
            results.append((cx, cy, data["text"][i]))
    return results

def main():
    import pyautogui
    from PIL import ImageGrab

    print(f"Target: {TARGET}")
    activate_vscode()

    x, y, w, h = get_window()
    print(f"Window: ({x},{y}) {w}x{h}")

    # Step 1: Click on the model name in the chat toolbar
    # The chat toolbar is ~70px above the bottom of the window (above the status bar)
    # Scan a strip: bottom-70 to bottom-30, right half only
    toolbar_top = y + h - 70
    toolbar_bottom = y + h - 30
    toolbar = ImageGrab.grab(bbox=(x + w//2, toolbar_top, x + w, toolbar_bottom))
    toolbar.save("/tmp/step1_toolbar.png")

    pos = ocr_find(toolbar, "Claude")
    if not pos:
        pos = ocr_find(toolbar, "Opus")
    if not pos:
        pos = ocr_find(toolbar, "Sonnet")
    if not pos:
        pos = ocr_find(toolbar, "Haiku")
    if not pos:
        pos = ocr_find(toolbar, "GPT")

    if not pos:
        print("ERROR: Model picker not found in chat toolbar")
        print("  Trying broader scan...")
        # Fallback: scan last 100px
        broader = ImageGrab.grab(bbox=(x + w//2, y + h - 100, x + w, y + h))
        broader.save("/tmp/step1_broader.png")
        pos = ocr_find(broader, "Claude")
        if pos:
            abs_x = x + w//2 + pos[0]
            abs_y = y + h - 100 + pos[1]
        else:
            return 1
    else:
        abs_x = x + w//2 + pos[0]
        abs_y = toolbar_top + pos[1]
    print(f"Step 1: Click model picker at ({abs_x}, {abs_y})")
    pyautogui.moveTo(abs_x, abs_y, duration=0.2)
    pyautogui.click()
    time.sleep(1.2)  # Wait for quick pick to open

    # Step 2: VS Code Quick Pick opens at the TOP CENTER of the window
    # Scan the top 400px of the window for the dropdown
    top = ImageGrab.grab(bbox=(x, y, x + w, y + 500))
    top.save("/tmp/step2_quickpick.png")

    # Find ALL occurrences of "Haiku" or model name in the quick pick area
    search_word = TARGET.split()[-1] if len(TARGET.split()) > 1 else TARGET  # e.g. "4.5" or "Haiku"
    # Try specific identifiers
    target_pos = None
    for search in [TARGET, "Haiku 4.5", "Haiku"]:
        hits = ocr_find_all(top, search.split()[-1] if " " in search else search)
        print(f"  Searching '{search}': {len(hits)} hits")
        for hx, hy, txt in hits:
            print(f"    '{txt}' at ({hx},{hy})")

        if hits:
            # The quick pick is typically in the center-top area
            # Filter hits to the center horizontal band (where quick pick appears)
            center_x = w // 2
            quickpick_hits = [(hx, hy, txt) for hx, hy, txt in hits
                              if abs(hx - center_x) < w * 0.3 and hy > 30 and hy < 450]
            if quickpick_hits:
                target_pos = quickpick_hits[0][:2]
                print(f"  Quick pick match: '{quickpick_hits[0][2]}' at {target_pos}")
                break
            elif hits:
                target_pos = hits[0][:2]
                print(f"  Using first match: '{hits[0][2]}' at {target_pos}")
                break

    if not target_pos:
        print(f"ERROR: '{TARGET}' not found in quick pick. Check /tmp/step2_quickpick.png")
        return 1

    abs_tx = x + target_pos[0]
    abs_ty = y + target_pos[1]
    print(f"Step 2: Click '{TARGET}' at ({abs_tx}, {abs_ty})")
    pyautogui.moveTo(abs_tx, abs_ty, duration=0.2)
    pyautogui.click()
    time.sleep(0.5)

    # Verify
    time.sleep(0.5)
    verify = ImageGrab.grab(bbox=(x + w//2, y + h - 40, x + w, y + h - 10))
    verify.save("/tmp/step3_verify.png")
    import pytesseract
    vtext = pytesseract.image_to_string(verify).lower()
    if "haiku" in vtext:
        print(f"VERIFIED: Model changed to Haiku!")
    elif "opus" in vtext:
        print(f"WARNING: Model still shows Opus. May need retry.")
    else:
        print(f"Toolbar text: {vtext.strip()}")

    return 0

if __name__ == "__main__":
    sys.exit(main())
