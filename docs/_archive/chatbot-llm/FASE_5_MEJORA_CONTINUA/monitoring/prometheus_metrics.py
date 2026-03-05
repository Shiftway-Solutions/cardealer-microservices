#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Prometheus Metrics Exporter
===============================================
Módulo que expone métricas del LLM server en formato Prometheus.
Se integra directamente en el FastAPI server (server.py) o puede
ejecutarse como sidecar independiente.

Métricas expuestas:
  - okla_llm_requests_total           (Counter)
  - okla_llm_requests_success_total   (Counter)
  - okla_llm_requests_error_total     (Counter)
  - okla_llm_response_duration_ms     (Histogram)
  - okla_llm_tokens_total             (Counter)
  - okla_llm_prompt_tokens_total      (Counter)
  - okla_llm_completion_tokens_total  (Counter)
  - okla_llm_confidence              (Histogram)
  - okla_llm_intent_total             (Counter, label: intent)
  - okla_llm_model_loaded             (Gauge)
  - okla_llm_uptime_seconds           (Gauge)
  - okla_llm_active_requests          (Gauge)
  - okla_llm_model_info               (Info)
  - okla_llm_avg_response_time_ms     (Gauge)
  - okla_llm_fallback_total           (Counter)
  - okla_llm_lead_captured_total      (Counter)

Autor: OKLA Team — FASE 5 Mejora Continua
"""

import time
import re
from typing import Optional

from prometheus_client import (
    Counter,
    Histogram,
    Gauge,
    Info,
    generate_latest,
    CollectorRegistry,
    CONTENT_TYPE_LATEST,
)

# ─────────────────────────────────────────────────────────
# Registry (custom para evitar conflicto con default metrics)
# ─────────────────────────────────────────────────────────
REGISTRY = CollectorRegistry()

# ─────────────────────────────────────────────────────────
# Counters
# ─────────────────────────────────────────────────────────

REQUESTS_TOTAL = Counter(
    "okla_llm_requests_total",
    "Total number of inference requests received",
    registry=REGISTRY,
)

REQUESTS_SUCCESS = Counter(
    "okla_llm_requests_success_total",
    "Total successful inference requests",
    registry=REGISTRY,
)

REQUESTS_ERROR = Counter(
    "okla_llm_requests_error_total",
    "Total failed inference requests",
    ["error_type"],
    registry=REGISTRY,
)

TOKENS_TOTAL = Counter(
    "okla_llm_tokens_total",
    "Total tokens generated (prompt + completion)",
    registry=REGISTRY,
)

PROMPT_TOKENS = Counter(
    "okla_llm_prompt_tokens_total",
    "Total prompt tokens consumed",
    registry=REGISTRY,
)

COMPLETION_TOKENS = Counter(
    "okla_llm_completion_tokens_total",
    "Total completion tokens generated",
    registry=REGISTRY,
)

INTENT_COUNTER = Counter(
    "okla_llm_intent_total",
    "Count of detected intents",
    ["intent"],
    registry=REGISTRY,
)

FALLBACK_TOTAL = Counter(
    "okla_llm_fallback_total",
    "Total fallback responses (low confidence or unknown intent)",
    registry=REGISTRY,
)

LEAD_CAPTURED = Counter(
    "okla_llm_lead_captured_total",
    "Total leads captured via chatbot",
    registry=REGISTRY,
)

# ─────────────────────────────────────────────────────────
# Histograms
# ─────────────────────────────────────────────────────────

RESPONSE_DURATION = Histogram(
    "okla_llm_response_duration_ms",
    "Response time in milliseconds",
    buckets=[100, 250, 500, 1000, 2000, 3000, 5000, 8000, 10000, 15000, 30000],
    registry=REGISTRY,
)

CONFIDENCE_HISTOGRAM = Histogram(
    "okla_llm_confidence",
    "Model confidence scores",
    buckets=[0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 0.95, 1.0],
    registry=REGISTRY,
)

# ─────────────────────────────────────────────────────────
# Gauges
# ─────────────────────────────────────────────────────────

MODEL_LOADED = Gauge(
    "okla_llm_model_loaded",
    "Whether the LLM model is loaded and ready (1=yes, 0=no)",
    registry=REGISTRY,
)

UPTIME_SECONDS = Gauge(
    "okla_llm_uptime_seconds",
    "Server uptime in seconds",
    registry=REGISTRY,
)

ACTIVE_REQUESTS = Gauge(
    "okla_llm_active_requests",
    "Number of currently active inference requests",
    registry=REGISTRY,
)

AVG_RESPONSE_TIME = Gauge(
    "okla_llm_avg_response_time_ms",
    "Rolling average response time in milliseconds",
    registry=REGISTRY,
)

# ─────────────────────────────────────────────────────────
# Info
# ─────────────────────────────────────────────────────────

MODEL_INFO = Info(
    "okla_llm_model",
    "Information about the loaded model",
    registry=REGISTRY,
)

# ─────────────────────────────────────────────────────────
# State
# ─────────────────────────────────────────────────────────

_start_time: float = time.time()
_response_times: list[float] = []
_MAX_ROLLING_WINDOW = 1000  # últimos 1000 requests para promedio


# ─────────────────────────────────────────────────────────
# Intent Detection from Response Text
# ─────────────────────────────────────────────────────────

INTENT_PATTERNS = {
    "buscar_vehiculo": [
        r"(?:busco|quiero|necesito|me interesa)\s+(?:un|una|el|la)?\s*(?:carro|vehículo|camioneta|jeepeta|SUV|sedan)",
        r"(?:tienen|hay)\s+(?:algún|alguna|un|una)\s+(?:carro|vehículo)",
    ],
    "consultar_precio": [
        r"(?:cuánto|cuanto|precio|costo|vale)\s+(?:cuesta|sale|es|del)",
        r"(?:rango|presupuesto)\s+(?:de|entre)",
    ],
    "agendar_cita": [
        r"(?:agendar|programar|reservar|hacer)\s+(?:una\s+)?(?:cita|visita|test\s+drive)",
        r"(?:puedo|quiero)\s+(?:ver|visitar|probar)",
    ],
    "financiamiento": [
        r"(?:financiamiento|financiar|cuotas|préstamo|crédito)",
        r"(?:plan\s+de\s+pago|mensualidad|inicial)",
    ],
    "comparar_vehiculos": [
        r"(?:comparar|diferencia|vs|versus|mejor)\s+(?:entre|que)",
        r"(?:cuál|cual)\s+(?:es\s+mejor|me\s+recomiendas)",
    ],
    "contactar_dealer": [
        r"(?:contactar|llamar|hablar|comunicar)\s+(?:al|con|el)\s+(?:dealer|vendedor|concesionario)",
        r"(?:teléfono|dirección|ubicación|horario)\s+(?:del|de)",
    ],
    "info_vehiculo": [
        r"(?:especificaciones|ficha\s+técnica|detalles|características)\s+(?:del|de)",
        r"(?:motor|transmisión|kilometraje|año|color|versión)",
    ],
    "saludo": [
        r"^(?:hola|buenos?\s+(?:días|tardes|noches)|saludos|hey|qué\s+tal)",
    ],
    "despedida": [
        r"(?:adiós|adios|chao|hasta\s+luego|gracias|nos\s+vemos|bye)",
    ],
    "lead_capture": [
        r"(?:mi\s+(?:nombre|teléfono|email|correo|número)\s+es)",
        r"(?:me\s+llamo|soy\s+\w+|pueden?\s+(?:contactarme|llamarme))",
    ],
}


def detect_intent(text: str) -> str:
    """Detecta el intent más probable del texto del usuario."""
    if not text:
        return "unknown"
    lower = text.lower().strip()
    for intent, patterns in INTENT_PATTERNS.items():
        for pattern in patterns:
            if re.search(pattern, lower):
                return intent
    return "general_query"


# ─────────────────────────────────────────────────────────
# Lead Detection from Response
# ─────────────────────────────────────────────────────────

LEAD_INDICATORS = [
    r"(?:nombre|teléfono|email|correo|whatsapp|número)",
    r"(?:contacto|datos\s+de\s+contacto|información\s+de\s+contacto)",
    r"(?:te\s+(?:llamamos|contactamos|escribimos))",
    r"(?:agenda(?:r|do|da)?\s+(?:una\s+)?cita)",
]


def is_lead_captured(user_text: str, bot_response: str) -> bool:
    """Determina si la interacción capturó datos de lead."""
    combined = f"{user_text} {bot_response}".lower()
    matches = sum(1 for p in LEAD_INDICATORS if re.search(p, combined))
    return matches >= 2


# ─────────────────────────────────────────────────────────
# Metric Recording Functions
# ─────────────────────────────────────────────────────────


def record_request_start():
    """Llamar al iniciar una request de inferencia."""
    REQUESTS_TOTAL.inc()
    ACTIVE_REQUESTS.inc()


def record_request_success(
    duration_ms: float,
    prompt_tokens: int = 0,
    completion_tokens: int = 0,
    confidence: float = 0.0,
    user_text: str = "",
    bot_response: str = "",
):
    """Llamar cuando una request completa exitosamente."""
    REQUESTS_SUCCESS.inc()
    ACTIVE_REQUESTS.dec()

    # Latencia
    RESPONSE_DURATION.observe(duration_ms)
    _response_times.append(duration_ms)
    if len(_response_times) > _MAX_ROLLING_WINDOW:
        _response_times.pop(0)
    AVG_RESPONSE_TIME.set(sum(_response_times) / len(_response_times))

    # Tokens
    total_tokens = prompt_tokens + completion_tokens
    TOKENS_TOTAL.inc(total_tokens)
    PROMPT_TOKENS.inc(prompt_tokens)
    COMPLETION_TOKENS.inc(completion_tokens)

    # Confianza
    if confidence > 0:
        CONFIDENCE_HISTOGRAM.observe(confidence)

    # Intent
    intent = detect_intent(user_text)
    INTENT_COUNTER.labels(intent=intent).inc()

    # Fallback
    if confidence < 0.4 or intent == "unknown":
        FALLBACK_TOTAL.inc()

    # Lead
    if is_lead_captured(user_text, bot_response):
        LEAD_CAPTURED.inc()

    # Uptime
    UPTIME_SECONDS.set(time.time() - _start_time)


def record_request_error(error_type: str = "unknown"):
    """Llamar cuando una request falla."""
    REQUESTS_ERROR.labels(error_type=error_type).inc()
    ACTIVE_REQUESTS.dec()
    UPTIME_SECONDS.set(time.time() - _start_time)


def set_model_info(
    model_id: str,
    model_path: str = "",
    quantization: str = "Q4_K_M",
    parameters: str = "8B",
):
    """Establecer información del modelo cargado."""
    MODEL_LOADED.set(1)
    MODEL_INFO.info({
        "model_id": model_id,
        "model_path": model_path,
        "quantization": quantization,
        "parameters": parameters,
    })


def set_model_unloaded():
    """Marcar el modelo como descargado."""
    MODEL_LOADED.set(0)


# ─────────────────────────────────────────────────────────
# Metrics Endpoint
# ─────────────────────────────────────────────────────────


def get_metrics() -> tuple[bytes, str]:
    """
    Retorna las métricas en formato Prometheus.
    Returns: (body_bytes, content_type)
    """
    UPTIME_SECONDS.set(time.time() - _start_time)
    return generate_latest(REGISTRY), CONTENT_TYPE_LATEST


# ─────────────────────────────────────────────────────────
# FastAPI Integration Helper
# ─────────────────────────────────────────────────────────


def setup_metrics_endpoint(app):
    """
    Agrega el endpoint GET /metrics a una app FastAPI.

    Uso:
        from prometheus_metrics import setup_metrics_endpoint
        app = FastAPI()
        setup_metrics_endpoint(app)
    """
    from fastapi import Response

    @app.get("/metrics", tags=["monitoring"])
    async def metrics():
        body, content_type = get_metrics()
        return Response(content=body, media_type=content_type)


# ─────────────────────────────────────────────────────────
# Prometheus scrape config snippet
# ─────────────────────────────────────────────────────────

PROMETHEUS_SCRAPE_CONFIG = """
# Agregar a prometheus.yml → scrape_configs:
- job_name: 'llm-server'
  scrape_interval: 15s
  metrics_path: /metrics
  static_configs:
    - targets: ['llm-server:8000']
      labels:
        service: 'okla-chatbot-llm'
        environment: 'production'

  # En Kubernetes:
  # kubernetes_sd_configs:
  #   - role: pod
  #     namespaces:
  #       names: ['okla']
  # relabel_configs:
  #   - source_labels: [__meta_kubernetes_pod_label_app]
  #     regex: llm-server
  #     action: keep
"""


if __name__ == "__main__":
    # Demo: imprimir métricas de ejemplo
    set_model_info("okla-llama3-8b-v1", "/models/okla-v1.gguf")
    record_request_start()
    record_request_success(
        duration_ms=1250.5,
        prompt_tokens=120,
        completion_tokens=85,
        confidence=0.87,
        user_text="Hola, busco un carro SUV 2024",
        bot_response="¡Hola! Tenemos varias opciones de SUV 2024 disponibles.",
    )
    record_request_start()
    record_request_error("timeout")

    body, ct = get_metrics()
    print(f"Content-Type: {ct}")
    print(body.decode("utf-8"))
