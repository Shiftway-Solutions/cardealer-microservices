#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Production Smoke Test
=========================================
Run after deployment to validate the chatbot stack is healthy.

Usage:
  # Against port-forwarded services (default)
  python smoke_test.py

  # Against specific endpoints
  python smoke_test.py --llm-url http://localhost:8000 --chatbot-url http://localhost:8081

  # Against K8s cluster directly (requires port-forward)
  kubectl port-forward svc/llm-server 8000:8000 -n okla &
  kubectl port-forward svc/chatbotservice 8081:8080 -n okla &
  python smoke_test.py

  # Quick mode (health checks only, skip inference)
  python smoke_test.py --quick

Exit codes:
  0 = All tests passed
  1 = One or more critical tests failed
  2 = Warnings (non-critical failures)
"""

import argparse
import json
import sys
import time
from dataclasses import dataclass, field
from typing import Optional

try:
    import requests
except ImportError:
    print("ERROR: 'requests' not installed. Run: pip install requests")
    sys.exit(1)


# ─── Configuration ────────────────────────────────────────────────────────────

LLM_DEFAULT_URL = "http://localhost:8000"
CHATBOT_DEFAULT_URL = "http://localhost:8081"
GATEWAY_DEFAULT_URL = "https://okla.com.do"

TIMEOUT_HEALTH = 10  # seconds
TIMEOUT_INFERENCE = 120  # seconds (LLM can be slow on CPU)

# Thresholds aligned with model-registry.json
MAX_LATENCY_P95 = 60.0  # seconds
MAX_LATENCY_WARNING = 30.0  # seconds


# ─── Test Result Tracking ─────────────────────────────────────────────────────

@dataclass
class TestResult:
    name: str
    passed: bool
    duration_ms: float = 0.0
    details: str = ""
    critical: bool = True


@dataclass
class TestSuite:
    results: list = field(default_factory=list)

    def add(self, result: TestResult):
        self.results.append(result)
        status = "✅ PASS" if result.passed else ("🔴 FAIL" if result.critical else "🟡 WARN")
        print(f"  {status} [{result.duration_ms:.0f}ms] {result.name}")
        if not result.passed and result.details:
            print(f"         └─ {result.details}")

    @property
    def critical_failures(self) -> int:
        return sum(1 for r in self.results if not r.passed and r.critical)

    @property
    def warnings(self) -> int:
        return sum(1 for r in self.results if not r.passed and not r.critical)

    @property
    def passed(self) -> int:
        return sum(1 for r in self.results if r.passed)

    def summary(self) -> int:
        total = len(self.results)
        print(f"\n{'='*60}")
        print(f"  RESULTS: {self.passed}/{total} passed, "
              f"{self.critical_failures} critical failures, "
              f"{self.warnings} warnings")
        print(f"{'='*60}")

        if self.critical_failures > 0:
            print("\n🔴 DEPLOYMENT NOT HEALTHY — Critical failures detected")
            print("   → Check logs: kubectl logs -f deployment/llm-server -n okla")
            return 1
        elif self.warnings > 0:
            print("\n🟡 DEPLOYMENT OK WITH WARNINGS — Monitor closely")
            return 2
        else:
            print("\n✅ DEPLOYMENT HEALTHY — All smoke tests passed")
            return 0


suite = TestSuite()


# ─── Helper ───────────────────────────────────────────────────────────────────

def timed_request(method: str, url: str, timeout: int = TIMEOUT_HEALTH, **kwargs) -> tuple:
    """Returns (response_or_none, duration_ms, error_msg)."""
    start = time.time()
    try:
        resp = requests.request(method, url, timeout=timeout, **kwargs)
        duration = (time.time() - start) * 1000
        return resp, duration, None
    except requests.exceptions.ConnectionError:
        duration = (time.time() - start) * 1000
        return None, duration, f"Connection refused: {url}"
    except requests.exceptions.Timeout:
        duration = (time.time() - start) * 1000
        return None, duration, f"Timeout after {timeout}s: {url}"
    except Exception as e:
        duration = (time.time() - start) * 1000
        return None, duration, str(e)


# ─── Tests: LLM Server ───────────────────────────────────────────────────────

def test_llm_health(llm_url: str):
    """Check LLM server /health endpoint."""
    resp, ms, err = timed_request("GET", f"{llm_url}/health")
    if err:
        suite.add(TestResult("LLM Server /health reachable", False, ms, err))
        return False

    suite.add(TestResult("LLM Server /health reachable", resp.status_code == 200, ms,
                         f"HTTP {resp.status_code}" if resp.status_code != 200 else ""))

    if resp.status_code == 200:
        try:
            data = resp.json()
            model_loaded = data.get("model_loaded", False)
            suite.add(TestResult("LLM model loaded in memory", model_loaded, ms,
                                 f"model_loaded={model_loaded}, model={data.get('model_name', 'unknown')}"))
            return model_loaded
        except (json.JSONDecodeError, KeyError):
            suite.add(TestResult("LLM health response valid JSON", False, ms, "Invalid JSON"))
    return False


def test_llm_metrics(llm_url: str):
    """Check Prometheus metrics endpoint."""
    resp, ms, err = timed_request("GET", f"{llm_url}/metrics")
    if err:
        suite.add(TestResult("LLM /metrics endpoint", False, ms, err, critical=False))
        return
    suite.add(TestResult("LLM /metrics endpoint", resp.status_code == 200, ms,
                         "" if resp.status_code == 200 else f"HTTP {resp.status_code}", critical=False))


def test_llm_inference_sv(llm_url: str):
    """Test SingleVehicle mode inference."""
    payload = {
        "model": "okla-llama3-8b",
        "messages": [
            {
                "role": "system",
                "content": (
                    "Eres OKLA Bot, asistente de ventas de vehículos. Modo: SingleVehicle. "
                    "Vehículo: Toyota Corolla 2023, $25,000, 15000km, gasolina, automático. "
                    "Responde SOLO en JSON válido con estos campos exactos: "
                    "response, intent, confidence, isFallback, parameters, leadSignals, "
                    "suggestedAction, quickReplies."
                )
            },
            {"role": "user", "content": "¿Cuánto cuesta este carro?"}
        ],
        "temperature": 0.3,
        "max_tokens": 400
    }

    resp, ms, err = timed_request("POST", f"{llm_url}/v1/chat/completions",
                                  timeout=TIMEOUT_INFERENCE, json=payload)
    if err:
        suite.add(TestResult("SV inference response received", False, ms, err))
        return

    suite.add(TestResult("SV inference response received",
                         resp.status_code == 200, ms,
                         f"HTTP {resp.status_code}" if resp.status_code != 200 else f"Latency: {ms:.0f}ms"))

    if resp.status_code != 200:
        return

    # Latency check
    suite.add(TestResult("SV inference latency < 60s",
                         ms < MAX_LATENCY_P95 * 1000, ms,
                         f"{ms/1000:.1f}s (limit: {MAX_LATENCY_P95}s)", critical=False))

    # Parse response
    try:
        data = resp.json()
        content = data["choices"][0]["message"]["content"]

        # Try to parse as JSON (the core requirement)
        parsed = json.loads(content)
        suite.add(TestResult("SV response is valid JSON", True, ms, ""))

        # Check required fields
        required_fields = ["response", "intent", "confidence", "isFallback"]
        missing = [f for f in required_fields if f not in parsed]
        suite.add(TestResult("SV JSON has required fields",
                             len(missing) == 0, ms,
                             f"Missing: {missing}" if missing else "All 4 core fields present"))

        # Check confidence is reasonable
        conf = parsed.get("confidence", 0)
        suite.add(TestResult("SV confidence > 0.5",
                             isinstance(conf, (int, float)) and conf > 0.5, ms,
                             f"confidence={conf}", critical=False))

        # Check not fallback
        suite.add(TestResult("SV not a fallback response",
                             not parsed.get("isFallback", True), ms,
                             f"isFallback={parsed.get('isFallback')}", critical=False))

    except json.JSONDecodeError:
        suite.add(TestResult("SV response is valid JSON", False, ms,
                             f"Raw response: {content[:200]}"))
    except (KeyError, IndexError) as e:
        suite.add(TestResult("SV OpenAI-compatible response format", False, ms, str(e)))


def test_llm_inference_di(llm_url: str):
    """Test DealerInventory mode inference."""
    payload = {
        "model": "okla-llama3-8b",
        "messages": [
            {
                "role": "system",
                "content": (
                    "Eres OKLA Bot, asistente de inventario de dealer. Modo: DealerInventory. "
                    "Dealer: AutoPlaza RD, 45 vehículos en inventario. "
                    "Responde SOLO en JSON válido con estos campos exactos: "
                    "response, intent, confidence, isFallback, parameters, leadSignals, "
                    "suggestedAction, quickReplies."
                )
            },
            {"role": "user", "content": "Muéstrame los SUVs que tienen disponibles"}
        ],
        "temperature": 0.3,
        "max_tokens": 400
    }

    resp, ms, err = timed_request("POST", f"{llm_url}/v1/chat/completions",
                                  timeout=TIMEOUT_INFERENCE, json=payload)
    if err:
        suite.add(TestResult("DI inference response received", False, ms, err))
        return

    suite.add(TestResult("DI inference response received",
                         resp.status_code == 200, ms,
                         f"HTTP {resp.status_code}" if resp.status_code != 200 else f"Latency: {ms:.0f}ms"))

    if resp.status_code != 200:
        return

    try:
        data = resp.json()
        content = data["choices"][0]["message"]["content"]
        parsed = json.loads(content)
        suite.add(TestResult("DI response is valid JSON", True, ms, ""))

        # Check intent is DI-related
        intent = parsed.get("intent", "")
        di_intents = ["INVENTORY_SEARCH", "FILTER_BY_TYPE", "INVENTORY_BROWSE",
                      "SHOW_AVAILABLE", "FILTER_VEHICLES"]
        suite.add(TestResult("DI intent is inventory-related",
                             any(kw in intent.upper() for kw in ["INVENTORY", "FILTER", "SEARCH", "BROWSE", "SHOW"]),
                             ms, f"intent={intent}", critical=False))

    except json.JSONDecodeError:
        suite.add(TestResult("DI response is valid JSON", False, ms,
                             f"Raw: {content[:200]}"))
    except (KeyError, IndexError) as e:
        suite.add(TestResult("DI OpenAI-compatible format", False, ms, str(e)))


def test_llm_boundary_enforcement(llm_url: str):
    """Test that the model stays in-domain (no off-topic answers)."""
    payload = {
        "model": "okla-llama3-8b",
        "messages": [
            {
                "role": "system",
                "content": (
                    "Eres OKLA Bot, asistente de ventas de vehículos. Modo: SingleVehicle. "
                    "SOLO respondes sobre vehículos y el marketplace OKLA. "
                    "Para temas fuera de alcance, usa isFallback=true con intent=OUT_OF_SCOPE. "
                    "Responde SOLO en JSON."
                )
            },
            {"role": "user", "content": "¿Cuál es la capital de Francia?"}
        ],
        "temperature": 0.3,
        "max_tokens": 300
    }

    resp, ms, err = timed_request("POST", f"{llm_url}/v1/chat/completions",
                                  timeout=TIMEOUT_INFERENCE, json=payload)
    if err:
        suite.add(TestResult("Boundary test response received", False, ms, err, critical=False))
        return

    if resp.status_code != 200:
        suite.add(TestResult("Boundary test response received", False, ms,
                             f"HTTP {resp.status_code}", critical=False))
        return

    try:
        data = resp.json()
        content = data["choices"][0]["message"]["content"]
        parsed = json.loads(content)

        is_fallback = parsed.get("isFallback", False)
        intent = parsed.get("intent", "").upper()
        is_boundary_enforced = is_fallback or "OUT_OF_SCOPE" in intent or "OFF_TOPIC" in intent

        suite.add(TestResult("Boundary enforcement (off-topic → fallback)",
                             is_boundary_enforced, ms,
                             f"isFallback={is_fallback}, intent={intent}", critical=False))
    except (json.JSONDecodeError, KeyError):
        suite.add(TestResult("Boundary test parseable", False, ms, "Response not valid JSON", critical=False))


# ─── Tests: ChatbotService (.NET 8) ──────────────────────────────────────────

def test_chatbot_health(chatbot_url: str):
    """Check ChatbotService /health endpoint."""
    resp, ms, err = timed_request("GET", f"{chatbot_url}/health")
    if err:
        suite.add(TestResult("ChatbotService /health reachable", False, ms, err))
        return False

    passed = resp.status_code == 200
    suite.add(TestResult("ChatbotService /health reachable", passed, ms,
                         "" if passed else f"HTTP {resp.status_code}"))
    return passed


# ─── Tests: Gateway Routing ───────────────────────────────────────────────────

def test_gateway_routing(gateway_url: str):
    """Check that Gateway routes /api/chat/* to ChatbotService."""
    # Only test if gateway_url is provided and reachable
    resp, ms, err = timed_request("GET", f"{gateway_url}/api/chat/health")
    if err:
        suite.add(TestResult("Gateway /api/chat/* routing", False, ms, err, critical=False))
        return
    suite.add(TestResult("Gateway /api/chat/* routing",
                         resp.status_code in [200, 401, 403], ms,
                         f"HTTP {resp.status_code} (non-404 = route exists)", critical=False))


# ─── Main ─────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="OKLA Chatbot LLM Smoke Test")
    parser.add_argument("--llm-url", default=LLM_DEFAULT_URL, help="LLM Server base URL")
    parser.add_argument("--chatbot-url", default=CHATBOT_DEFAULT_URL, help="ChatbotService base URL")
    parser.add_argument("--gateway-url", default=None, help="Gateway URL (optional, for routing test)")
    parser.add_argument("--quick", action="store_true", help="Quick mode: health checks only")
    parser.add_argument("--skip-llm", action="store_true",
                        help="Skip LLM tests (e.g. when llm-server is scaled to 0 replicas)")
    args = parser.parse_args()

    print("=" * 60)
    print("  OKLA Chatbot LLM — Production Smoke Test")
    print(f"  LLM Server:     {args.llm_url if not args.skip_llm else 'skipped (--skip-llm)'}")
    print(f"  ChatbotService:  {args.chatbot_url}")
    print(f"  Gateway:         {args.gateway_url or 'skipped'}")
    print(f"  Mode:            {'quick' if args.quick else 'full'}")
    print("=" * 60)

    # ── Phase 1: Health Checks ──
    print("\n📡 Phase 1: Health Checks")
    if args.skip_llm:
        print("⚠️  LLM Server tests skipped (scaled to 0 replicas — GPU cost savings mode)")
        model_loaded = False
    else:
        model_loaded = test_llm_health(args.llm_url)
        test_llm_metrics(args.llm_url)
    test_chatbot_health(args.chatbot_url)

    if args.gateway_url:
        test_gateway_routing(args.gateway_url)

    if args.quick:
        return suite.summary()

    # ── Phase 2: Inference Tests (only if model is loaded) ──
    if not model_loaded:
        print("\n⏭️  Skipping inference tests (model not loaded)")
        return suite.summary()

    print("\n🧠 Phase 2: Inference Tests")
    test_llm_inference_sv(args.llm_url)
    test_llm_inference_di(args.llm_url)

    # ── Phase 3: Safety & Boundary Tests ──
    print("\n🛡️  Phase 3: Safety & Boundary Tests")
    test_llm_boundary_enforcement(args.llm_url)

    return suite.summary()


if __name__ == "__main__":
    exit_code = main()
    sys.exit(exit_code)
