#!/usr/bin/env python3
import json, pathlib, time

ext_json = pathlib.Path.home() / ".vscode/extensions/extensions.json"
data = json.loads(ext_json.read_text())

exists = any(
    e.get("identifier", {}).get("id", "").lower() == "gregory-local.copilot-model-cycler"
    for e in data
)

if exists:
    print("Ya existe en el registro extensions.json")
else:
    entry = {
        "identifier": {"id": "gregory-local.copilot-model-cycler"},
        "version": "3.0.0",
        "location": {
            "$mid": 1,
            "path": "/Users/gregorymoreno/.vscode/extensions/gregory-local.copilot-model-cycler-3.0.0",
            "scheme": "file",
        },
        "relativeLocation": "gregory-local.copilot-model-cycler-3.0.0",
        "metadata": {
            "installedTimestamp": int(time.time() * 1000),
            "pinned": False,
            "source": "vsix",
        },
    }
    data.append(entry)
    ext_json.write_text(json.dumps(data, indent=2))
    print(f"OK — Extensión registrada. Total extensiones: {len(data)}")
