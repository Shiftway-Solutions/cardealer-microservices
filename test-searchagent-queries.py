#!/usr/bin/env python3
"""Test all 10 SearchAgent queries and report results."""
import json
import time
import urllib.request

GATEWAY = "http://localhost:18443"
QUERIES = [
    "Busco un jeepetón bueno pa la familia",
    "Algo menor de un palo",
    "Toyota o Honda automático en Santiago",
    "Carro bueno y barato para primer carro",
    "Algo eléctrico o híbrido",
    "SUV 7 pasajeros para viaje al campo",
    "",
    "asdfghjkl",
    "Quiero financiamiento",
    "El más barato que haya",
]

for i, q in enumerate(QUERIES, 1):
    payload = json.dumps({"query": q}).encode()
    req = urllib.request.Request(
        f"{GATEWAY}/api/search-agent/search",
        data=payload,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    start = time.time()
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            body = json.loads(resp.read())
            elapsed = int((time.time() - start) * 1000)
            d = body["data"]
            ai = d["isAiSearchEnabled"]
            latency = d["latencyMs"]
            conf = d["aiFilters"]["confianza"]
            msg = (d["aiFilters"].get("mensaje_usuario") or "")[:60]
            warns = d["aiFilters"].get("advertencias", [])
            warn_txt = "; ".join(warns)[:60] if warns else "-"
            status = "OK"
    except Exception as e:
        elapsed = int((time.time() - start) * 1000)
        ai = latency = conf = "-"
        msg = str(e)[:60]
        warn_txt = "-"
        status = "ERROR"

    print(f"Q{i:02d} | {status:5} | HTTP200 | AI={ai} | Conf={conf} | "
          f"Lat={latency}ms | Total={elapsed}ms | Warns: {warn_txt}")
    print(f"     Query: \"{q}\"")
    print(f"     Msg: {msg}")
    print()
