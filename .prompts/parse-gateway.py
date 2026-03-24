import re

files = [
    '/opt/homebrew/lib/node_modules/openclaw/dist/gateway-runtime-BphJPpz3.js',
    '/opt/homebrew/lib/node_modules/openclaw/dist/esm-CZnJcADF.js',
]

for fpath in files:
    try:
        with open(fpath) as f:
            src = f.read()
    except Exception:
        print(f"Could not open {fpath}")
        continue

    print(f"\n=== {fpath} ===")

    for pat in ['1008', 'invalid.*request.*frame', 'policy.*violation',
                'verifyClient', 'handleUpgrade', 'ws.*server',
                'connect\.challenge', 'nonce', 'subprotocol', 'protocol']:
        m = re.search(r'.{30}' + pat + r'.{80}', src, re.IGNORECASE)
        if m:
            print(f"[{pat}]: {m.group(0)[:200]}")
