#!/usr/bin/env python3
"""
Select a model in VS Code Copilot Chat.
Approach: AppleScript click to open popup + screenshot/OCR to find + AppleScript click to select.
Usage: python3 _select_model3.py "Claude Haiku 4.5"
"""
import subprocess
import time
import sys

TARGET = sys.argv[1] if len(sys.argv) > 1 else "Claude Haiku 4.5"


def applescript(script):
    r = subprocess.run(['osascript', '-e', script], capture_output=True, text=True, timeout=15)
    return r.stdout.strip(), r.returncode


def get_window():
    out, _ = applescript('''
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


def find_text_in_image(img, keyword):
    """Find all occurrences of keyword in image, return list of (cx, cy, full_text)."""
    import pytesseract
    data = pytesseract.image_to_data(img, output_type=pytesseract.Output.DICT)
    results = []
    kw = keyword.lower()
    for i in range(len(data["text"])):
        if kw in data["text"][i].lower().strip() and int(data["conf"][i]) > 15:
            cx = data["left"][i] + data["width"][i] // 2
            cy = data["top"][i] + data["height"][i] // 2
            results.append((cx, cy, data["text"][i].strip()))
    return results


def main():
    from PIL import ImageGrab

    print(f"=== Selecting model: {TARGET} ===")

    # Activate VS Code
    applescript('tell application "Visual Studio Code" to activate')
    time.sleep(0.5)

    wx, wy, ww, wh = get_window()
    print(f"Window: ({wx},{wy}) {ww}x{wh}")

    # Step 1: Find the model picker button in the chat toolbar
    # Scan a vertical strip in the right half of the window, covering the chat toolbar area
    # The toolbar is typically 50-70px above the bottom
    print("\n[Step 1] Finding model picker button...")
    for top_off in [75, 70, 65, 80, 85, 60, 90]:
        strip = ImageGrab.grab(bbox=(wx + ww//2, wy + wh - top_off, wx + ww, wy + wh - top_off + 25))
        hits = find_text_in_image(strip, "Claude")
        if not hits:
            hits = find_text_in_image(strip, "Opus")
        if not hits:
            hits = find_text_in_image(strip, "Sonnet")
        if not hits:
            hits = find_text_in_image(strip, "Haiku")
        if hits:
            rel_cx, rel_cy, txt = hits[0]
            abs_cx = wx + ww//2 + rel_cx
            abs_cy = wy + wh - top_off + rel_cy
            print(f"  Found '{txt}' at abs ({abs_cx}, {abs_cy})")
            break
    else:
        print("  ERROR: Model picker button not found")
        return 1

    # Step 2: Click the model picker using AppleScript
    print(f"\n[Step 2] Clicking model picker at ({abs_cx}, {abs_cy})...")
    applescript(f'''
tell application "System Events"
    tell process "Code"
        click at {{{abs_cx}, {abs_cy}}}
    end tell
end tell
''')
    time.sleep(1.5)  # Wait for popup to fully render

    # Step 3: Screenshot the popup area and find the target model
    # The popup appears ABOVE the model picker button, typically spanning 200-300px vertically
    print("\n[Step 3] Looking for target model in popup...")
    # Capture the area above the button: from button_y - 300 to button_y + 20
    popup_top = max(wy, abs_cy - 300)
    popup_bottom = abs_cy + 20
    popup_left = wx + ww // 2 - 50  # slightly wider than right half
    popup_right = wx + ww

    popup_img = ImageGrab.grab(bbox=(popup_left, popup_top, popup_right, popup_bottom))
    popup_img.save("/tmp/model_popup.png")
    print(f"  Popup region: ({popup_left},{popup_top}) -> ({popup_right},{popup_bottom})")

    # Extract the distinguishing keyword from target (e.g., "Haiku" from "Claude Haiku 4.5")
    keywords = []
    for part in TARGET.split():
        if part.lower() not in ("claude", "4.5", "4.6", "3.5"):
            keywords.append(part)
    if not keywords:
        keywords = [TARGET.split()[-1]]

    target_hits = []
    for kw in keywords:
        hits = find_text_in_image(popup_img, kw)
        print(f"  Searching '{kw}': {len(hits)} hits")
        for hx, hy, txt in hits:
            print(f"    '{txt}' at popup-relative ({hx},{hy})")
        target_hits.extend(hits)
        if target_hits:
            break

    if not target_hits:
        print(f"\n  ERROR: '{TARGET}' not found in popup")
        print("  Check /tmp/model_popup.png")
        # Close popup with Escape
        applescript('''
tell application "System Events"
    key code 53
end tell
''')
        return 1

    # Use the first hit 
    hit_cx, hit_cy, hit_txt = target_hits[0]
    abs_tx = popup_left + hit_cx
    abs_ty = popup_top + hit_cy
    print(f"\n[Step 4] Clicking '{hit_txt}' at abs ({abs_tx}, {abs_ty})...")

    applescript(f'''
tell application "System Events"
    tell process "Code"
        click at {{{abs_tx}, {abs_ty}}}
    end tell
end tell
''')
    time.sleep(0.8)

    # Step 5: Verify
    print("\n[Step 5] Verifying...")
    verify_hits = []
    for top_off in [75, 70, 65, 80]:
        strip = ImageGrab.grab(bbox=(wx + ww//2, wy + wh - top_off, wx + ww, wy + wh - top_off + 25))
        verify_hits = find_text_in_image(strip, keywords[0])
        if verify_hits:
            print(f"  SUCCESS: Model toolbar now shows '{verify_hits[0][2]}'")
            return 0
    
    print(f"  Could not confirm change. Check VS Code manually.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
