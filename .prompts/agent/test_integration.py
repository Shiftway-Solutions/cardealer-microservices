"""Test de integración del sistema GEMMA3 smart_monitor."""
export = {}
import sys, sqlite3
sys.path.insert(0, '/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/agent')

from smart_monitor.observer import ACTIVE_GENERATION_PATTERNS, MODEL_COMPLETION_PATTERN
from smart_monitor.brain import Brain
from smart_monitor.memory import Memory, seed_initial_lessons, DB_PATH

print("=== INTEGRACIÓN GEMMA3 SMART MONITOR ===")
print("✅ observer.py imports OK")
print("✅ brain.py imports OK")
print("✅ memory.py imports OK")

# Verify memory.db
conn = sqlite3.connect(str(DB_PATH))
total = conn.execute("SELECT COUNT(*) FROM lessons").fetchone()[0]
cats = conn.execute("SELECT category, COUNT(*) FROM lessons GROUP BY category ORDER BY category").fetchall()
conn.close()
print(f"✅ memory.db: {total} lecciones")
for cat, cnt in cats:
    print(f"   {cat}: {cnt}")

# Test ACTIVE_GENERATION_PATTERNS
print()
print("=== ACTIVE_GENERATION_PATTERNS ===")
tests = [
    ("Thinking...", True, "Thinking"),
    ("Running read_file", True, "Running tool"),
    ("23% completado", True, "% progress"),
    ("Analyzing file structure", True, "Analyzing"),
    ("Editing main.cs", True, "Editing"),
    ("Writing output", True, "Writing"),
    ("Compacting conversation...", True, "Compacting"),
    ("texto simple terminado.", False, "texto plano"),
    ("Error: rate limit exceeded 429", False, "rate limit"),
]
ok = 0
for txt, expected, label in tests:
    result = bool(ACTIVE_GENERATION_PATTERNS.search(txt))
    s = "OK" if result == expected else "FAIL"
    ok += 1 if s == "OK" else 0
    print(f"  {s}  [{label}]  →  {txt[:45]}")
print(f"  TOTAL: {ok}/{len(tests)} OK\n")

# Test MODEL_COMPLETION_PATTERN
print("=== MODEL_COMPLETION_PATTERN ===")
model_tests = [
    ("Claude Sonnet 4.5\nShared\n· Xhigh\n", True, "Claude Xhigh"),
    ("GPT-4o\nRemote\n· High\n", True, "GPT-4o High"),
    ("GPT-5\nRemote\n· High\n", True, "GPT-5"),
    ("o3-mini\nLocal\n· Low\n", True, "o3-mini"),
    ("texto sin modelo", False, "sin modelo"),
]
ok2 = 0
for txt, expected, label in model_tests:
    result = bool(MODEL_COMPLETION_PATTERN.search(txt))
    s = "OK" if result == expected else "FAIL"
    ok2 += 1 if s == "OK" else 0
    print(f"  {s}  [{label}]")
print(f"  TOTAL: {ok2}/{len(model_tests)} OK")

print()
total_ok = ok + ok2
total_tests = len(tests) + len(model_tests)
print(f"=== RESULTADO FINAL: {total_ok}/{total_tests} tests OK ===")
if total_ok == total_tests:
    print("✅ SISTEMA LISTO PARA PRODUCCIÓN")
else:
    print("⚠️  ALGUNOS TESTS FALLARON — REVISAR")
