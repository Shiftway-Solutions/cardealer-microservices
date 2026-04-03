#!/usr/bin/env python3
"""
Select a model in VS Code Copilot Chat using AppleScript click + keyboard.
Usage: python3 _select_model.py "Claude Haiku 4.5"
"""
import subprocess
import time
import sys

TARGET = sys.argv[1] if len(sys.argv) > 1 else "Claude Haiku 4.5"


def run_applescript(script):
    r = subprocess.run(['osascript', '-e', script], capture_output=True, text=True, timeout=15)
    return r.stdout.strip(), r.returncode, r.stderr.strip()


def get_window():
    out, _, _ = run_applescript('''
tell application "System Events"
    tell process "Code"
        set w to front window
        set pos to position of w
        set sz to size of w
        return (item 1 of pos as string) & "," & (item 2 of pos as string) & "," & (item 1 of sz as string) & "," & (item 2 of sz as string)
    end tell
end tell
''')
    return tuple(map(int, out.split(',')))


def find_model_button():
    """OCR scan the chat toolbar to find the model picker button."""
    from PIL import ImageGrab
    import pytesseract

    x, y, w, h = get_window()

    # Scan multiple vertical strips in the right half, looking for model text
    for top_off, bot_off in [(75, 45), (85, 35), (95, 25), (65, 50)]:
        crop = ImageGrab.grab(bbox=(x + w//2, y + h - top_off, x + w, y + h - bot_off))
        data = pytesseract.image_to_data(crop, output_type=pytesseract.Output.DICT)
        for i in range(len(data["text"])):
            txt = data["text"][i].strip().lower()
            if any(k in txt for k in ["claude", "opus", "sonnet", "haiku", "gpt"]):
                abs_cx = x + w//2 + data["left"][i] + data["width"][i]//2
                abs_cy = y + h - top_off + data["top"][i] + data["height"][i]//2
                return abs_cx, abs_cy, data["text"][i].strip()
    return None


def main():
    print(f"Target: {TARGET}")

    result = find_model_button()
    if not result:
        print("ERROR: Could not find model picker button")
        return 1

    click_x, click_y, found_text = result
    print(f"Found '{found_text}' at ({click_x}, {click_y})")

    # Build a filter keyword from target (e.g., "Haiku" from "Claude Haiku 4.5")
    filter_keyword = "haiku"
    parts = TARGET.lower().split()
    for p in parts:
        if p not in ("claude", "4.5", "4.6", "3.5"):
            filter_keyword = p
            break

    print(f"Step 1: Click model picker, Step 2: Type '{filter_keyword}' + Enter")

    # Do everything in one AppleScript for atomicity
    out, rc, err = run_applescript(f'''
tell application "Visual Studio Code"
    activate
end tell
delay 0.5
tell application "System Events"
    tell process "Code"
        -- Click the model picker button
        click at {{{click_x}, {click_y}}}
        delay 1.2
        -- Type to filter the quick pick dropdown
        keystroke "{filter_keyword}"
        delay 0.6
        -- Press Enter to select top match
        keystroke return
        delay 0.5
    end tell
end tell
''')
    print(f"AppleScript rc={rc}")
    if err:
        print(f"  err: {err[:200]}")

    # Verify
    time.sleep(0.5)
    result2 = find_model_button()
    if result2:
        print(f"Model now shows: '{result2[2]}'")
        if filter_keyword in result2[2].lower():
            print(f"SUCCESS: Model changed to {TARGET}")
        else:
            print(f"Model may not have changed. Current: '{result2[2]}'")
    else:
        print("Could not verify (model button not found in rescan)")

    return 0


if __name__ == "__main__":
    sys.exit(main())
