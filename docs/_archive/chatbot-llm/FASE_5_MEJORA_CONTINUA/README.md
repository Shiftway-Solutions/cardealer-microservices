# 📈 FASE 5 — Mejora Continua del Chatbot LLM OKLA

> **Estado:** ✅ Completa  
> **Fecha:** Febrero 2026  
> **Prerequisito:** FASE 4 (Deployment) completada — Dialogflow **ELIMINADO**, LLM en producción.

---

## 📋 Resumen

FASE 5 implementa el ecosistema completo de **mejora continua** para el chatbot LLM OKLA. Incluye 6 módulos que trabajan en conjunto para monitorear, evaluar, recolectar feedback, detectar degradación, comparar modelos y automatizar el re-entrenamiento.

```
┌─────────────────────────────────────────────────────────────┐
│                    CICLO DE MEJORA CONTINUA                  │
│                                                             │
│   ┌──────────┐    ┌───────────┐    ┌──────────────────┐    │
│   │ Monitor  │───▶│  Evaluar  │───▶│ Recolectar       │    │
│   │ (Prom +  │    │ (evaluate │    │ Feedback          │    │
│   │  Grafana)│    │  _model)  │    │ (feedback_system) │    │
│   └──────────┘    └───────────┘    └────────┬─────────┘    │
│        ▲                                     │              │
│        │                                     ▼              │
│   ┌──────────┐    ┌───────────┐    ┌──────────────────┐    │
│   │ A/B Test │◀───│ Re-train  │◀───│ Detectar Drift   │    │
│   │ (ab_test │    │ (retrain  │    │ (drift_detector)  │    │
│   │  ing)    │    │  pipeline)│    │                   │    │
│   └──────────┘    └───────────┘    └──────────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 📂 Estructura de Archivos

```
FASE_5_MEJORA_CONTINUA/
├── README.md                          ← Este archivo
├── evaluation/
│   └── evaluate_model.py             ← Pipeline de evaluación automatizada
├── feedback/
│   └── feedback_system.py            ← Recolección y análisis de feedback
├── retrain/
│   └── retrain_pipeline.py           ← Pipeline de re-entrenamiento
├── monitoring/
│   ├── drift_detector.py             ← Detección de degradación del modelo
│   ├── prometheus_metrics.py         ← Exportador de métricas Prometheus
│   └── grafana-dashboard.json        ← Dashboard Grafana importable
└── ab_testing/
    └── ab_testing.py                 ← Framework de pruebas A/B
```

**Archivos modificados en producción:**

| Archivo                                             | Cambio                                              |
| --------------------------------------------------- | --------------------------------------------------- |
| `backend/ChatbotService/LlmServer/server.py`        | + endpoint `/metrics`, + instrumentación Prometheus |
| `backend/ChatbotService/LlmServer/requirements.txt` | + `prometheus-client>=0.21.0`                       |

---

## 🔧 Módulos

### 1. 📊 Evaluación Automatizada (`evaluation/evaluate_model.py`)

Evalúa la calidad del modelo contra un test set con 5 dimensiones:

| Dimensión           | Métrica                       | Threshold Pass |
| ------------------- | ----------------------------- | -------------- |
| **Intent Accuracy** | Match rate vs expected intent | ≥ 70%          |
| **Latencia**        | p50, p90, p95, p99            | p95 < 10s      |
| **Seguridad**       | 10 prompts adversarios        | 0 violaciones  |
| **Naturalidad RD**  | Vocabulario dominicano        | ≥ 50% score    |
| **Lead Capture**    | Captura de datos de contacto  | ≥ 40% rate     |

#### Uso

```bash
# Evaluar contra test set
python evaluate_model.py \
    --test-data ../FASE_2_DATASET/output/test.jsonl \
    --server-url http://localhost:8000 \
    --output-dir ./results

# Solo latencia
python evaluate_model.py \
    --test-data test.jsonl \
    --server-url http://localhost:8000 \
    --skip-safety --skip-naturalness --skip-lead
```

#### Output

- `results/evaluation_report_YYYYMMDD_HHMMSS.json` — Datos crudos
- `results/evaluation_report_YYYYMMDD_HHMMSS.md` — Reporte legible

---

### 2. 💬 Sistema de Feedback (`feedback/feedback_system.py`)

Recolecta, analiza y exporta feedback de usuarios para alimentar el re-entrenamiento.

#### Categorías de Feedback (12)

| Categoría            | Descripción                 |
| -------------------- | --------------------------- |
| `wrong_intent`       | Intent incorrecto detectado |
| `hallucination`      | Información inventada       |
| `incomplete`         | Respuesta incompleta        |
| `wrong_language`     | No usa español dominicano   |
| `too_formal`         | Tono demasiado formal       |
| `too_informal`       | Tono demasiado informal     |
| `privacy_leak`       | Filtró datos sensibles      |
| `wrong_price`        | Precio incorrecto           |
| `wrong_vehicle_info` | Info de vehículo incorrecta |
| `slow_response`      | Respuesta muy lenta         |
| `good_response`      | Respuesta buena (positivo)  |
| `other`              | Otro                        |

#### Uso

```bash
# Analizar todo el feedback recolectado
python feedback_system.py analyze --data-dir ./feedback_data

# Exportar ejemplos para re-entrenamiento
python feedback_system.py export \
    --data-dir ./feedback_data \
    --output-dir ./export \
    --min-rating 4

# Generar reporte completo
python feedback_system.py report --data-dir ./feedback_data
```

#### Almacenamiento

- Formato: JSONL con rotación diaria
- Archivos: `feedback_data/feedback_YYYYMMDD.jsonl`
- Cada línea: `{ session_id, timestamp, rating, thumbs, user_query, bot_response, correction, category, metadata }`

---

### 3. 🔄 Pipeline de Re-entrenamiento (`retrain/retrain_pipeline.py`)

Automatiza el ciclo completo: recolección → validación → merge → split → script de Colab.

#### Flujo

```
Feedback (JSONL)  ──┐
                    ├──▶ Collect ──▶ Deduplicate ──▶ Validate
Conversations (DB) ─┘                                  │
                                                        ▼
                                             Merge con Dataset Original
                                             (ratio configurable 70/30)
                                                        │
                                                        ▼
                                             Split (85% train / 10% eval / 5% test)
                                                        │
                                                        ▼
                                             Generate Colab Script
                                             (Unsloth + QLoRA + LoRA merge)
```

#### Modelo de Versiones

| Estado      | Descripción                                       |
| ----------- | ------------------------------------------------- |
| `candidate` | Modelo recién entrenado, en evaluación            |
| `promoted`  | Modelo que pasó evaluación, listo para producción |
| `retired`   | Modelo anterior, reemplazado por versión nueva    |

#### Criterios de Re-entrenamiento Automático

- **Feedback count ≥ 50** nuevos ejemplos
- **Edad del modelo ≥ 30 días**
- **Accuracy < 70%** en última evaluación

#### Uso

```bash
# Recolectar nuevos datos de feedback
python retrain_pipeline.py collect \
    --feedback-dir ../feedback/feedback_data \
    --output-dir ./collected

# Preparar dataset combinado
python retrain_pipeline.py prepare \
    --new-data ./collected/combined.jsonl \
    --original-data ../../FASE_2_DATASET/output/train.jsonl \
    --output-dir ./prepared \
    --original-ratio 0.7

# Verificar si toca re-entrenar
python retrain_pipeline.py check \
    --versions-file ./model_versions.json \
    --feedback-dir ../feedback/feedback_data

# Generar script de Colab
python retrain_pipeline.py generate-script \
    --train-data ./prepared/train.jsonl \
    --eval-data ./prepared/eval.jsonl \
    --output-script ./retrain_colab.py
```

---

### 4. 📉 Detector de Drift (`monitoring/drift_detector.py`)

Detecta degradación del modelo en producción comparando métricas actuales con un baseline.

#### Señales de Drift (5)

| Señal                       | Threshold | Descripción                    |
| --------------------------- | --------- | ------------------------------ |
| `confidence_drop`           | -10%      | Confianza promedio cayó        |
| `fallback_rate_increase`    | +5pp      | Más respuestas de fallback     |
| `latency_increase`          | +50%      | Latencia p95 aumentó           |
| `intent_distribution_shift` | KL > 0.3  | Distribución de intents cambió |
| `token_usage_change`        | ±30%      | Tokens promedio cambió         |

#### Alertas

- **Slack webhook** automático cuando se detecta drift
- Payload incluye: señales detectadas, métricas actual vs baseline, recomendaciones

#### Uso

```bash
# Establecer baseline (ejecutar cuando el modelo es nuevo)
python drift_detector.py baseline \
    --server-url http://localhost:8000 \
    --test-data ../FASE_2_DATASET/output/test.jsonl \
    --output ./baseline.json

# Monitorear vs baseline
python drift_detector.py monitor \
    --server-url http://localhost:8000 \
    --test-data ../FASE_2_DATASET/output/test.jsonl \
    --baseline ./baseline.json \
    --slack-webhook https://hooks.slack.com/services/XXX/YYY/ZZZ

# Comparar dos baselines
python drift_detector.py compare \
    --baseline-a ./baseline_v1.json \
    --baseline-b ./baseline_v2.json
```

---

### 5. 🔀 Framework de A/B Testing (`ab_testing/ab_testing.py`)

Compara versiones de modelo en producción con significancia estadística.

#### Tests Estadísticos

| Test               | Tipo de Métrica    | Uso                             |
| ------------------ | ------------------ | ------------------------------- |
| **Chi-squared**    | Proporciones (0/1) | Satisfaction rate, lead capture |
| **Welch's t-test** | Medias continuas   | Latency, confidence             |

#### Sistema de Puntuación para Decisión

| Métrica      | Peso  | Justificación                  |
| ------------ | ----- | ------------------------------ |
| Satisfacción | 3 pts | Experiencia de usuario primero |
| Lead capture | 2 pts | Objetivo de negocio            |
| Latencia     | 1 pt  | Performance técnica            |
| Confianza    | 1 pt  | Calidad del modelo             |

#### Uso

```bash
# Crear experimento
python ab_testing.py create \
    --name "v1-vs-v2" \
    --control "okla-v1.0" \
    --treatment "okla-v2.0" \
    --traffic-split 50 \
    --min-samples 100 \
    --output-dir ./experiments

# Registrar resultado de una interacción
python ab_testing.py log \
    --experiment ./experiments/v1-vs-v2.json \
    --variant control \
    --satisfied true \
    --lead-captured false \
    --latency-ms 1250 \
    --confidence 0.87

# Analizar resultados
python ab_testing.py analyze \
    --experiment ./experiments/v1-vs-v2.json

# Decidir ganador
python ab_testing.py decide \
    --experiment ./experiments/v1-vs-v2.json
```

---

### 6. 📡 Métricas Prometheus + Dashboard Grafana

#### Métricas Expuestas en `/metrics`

| Métrica                            | Tipo      | Descripción                  |
| ---------------------------------- | --------- | ---------------------------- |
| `okla_llm_requests_total`          | Counter   | Total de requests            |
| `okla_llm_requests_success_total`  | Counter   | Requests exitosas            |
| `okla_llm_requests_error_total`    | Counter   | Requests fallidas (por tipo) |
| `okla_llm_response_duration_ms`    | Histogram | Latencia (11 buckets)        |
| `okla_llm_tokens_total`            | Counter   | Tokens totales               |
| `okla_llm_prompt_tokens_total`     | Counter   | Tokens de prompt             |
| `okla_llm_completion_tokens_total` | Counter   | Tokens de completion         |
| `okla_llm_model_loaded`            | Gauge     | Modelo cargado (1/0)         |
| `okla_llm_uptime_seconds`          | Gauge     | Uptime del server            |
| `okla_llm_active_requests`         | Gauge     | Requests en vuelo            |
| `okla_llm_avg_response_time_ms`    | Gauge     | Promedio rolling de latencia |
| `okla_llm_model_info`              | Info      | Metadatos del modelo         |

#### Configuración de Prometheus

Agregar a `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: "llm-server"
    scrape_interval: 15s
    metrics_path: /metrics
    static_configs:
      - targets: ["llm-server:8000"]
        labels:
          service: "okla-chatbot-llm"
          environment: "production"
```

#### Dashboard Grafana

1. Importar `monitoring/grafana-dashboard.json` en Grafana
2. Configurar datasource Prometheus
3. Dashboard incluye 4 secciones:
   - **Estado General** — Model status, uptime, request count, avg latency, success rate
   - **Latencia e Inferencia** — p50/p90/p95/p99, request rate, tokens/request, throughput
   - **Intents y Calidad** — Distribución de intents (pie chart), confianza (percentiles)
   - **Recursos del Sistema** — Memory RSS/VMS, CPU usage

---

## 🔄 Flujo Operativo Recomendado

### Semanal

1. **Revisar Dashboard Grafana** — latencia, error rate, distribución de intents
2. **Ejecutar evaluación** — `python evaluate_model.py --test-data test.jsonl`
3. **Analizar feedback** — `python feedback_system.py report`

### Mensual

4. **Detectar drift** — `python drift_detector.py monitor --baseline baseline.json`
5. **Verificar re-entrenamiento** — `python retrain_pipeline.py check`
6. Si aplica: recolectar datos → preparar → generar script → entrenar en Colab

### Por Release de Modelo

7. **Establecer nuevo baseline** — `python drift_detector.py baseline`
8. **Crear experimento A/B** — `python ab_testing.py create`
9. Recolectar ~100+ interacciones por variante
10. **Decidir ganador** — `python ab_testing.py decide`
11. Promover modelo ganador — `python retrain_pipeline.py` (model versions)

---

## 🛠️ Dependencias

### Python (ya incluidas en requirements.txt del LLM server)

```
prometheus-client>=0.21.0   # Métricas Prometheus
```

### Módulos de FASE 5 (standalone, sin deps adicionales)

Todos los scripts usan únicamente stdlib de Python 3.11+ (`json`, `statistics`, `math`, `hashlib`, `datetime`, `re`, `pathlib`, `argparse`, `http.client`). La única dependencia externa es `requests` para comunicación HTTP con el LLM server.

```bash
pip install requests  # Para evaluate_model.py y drift_detector.py
```

### Infraestructura

| Componente           | Para qué                           |
| -------------------- | ---------------------------------- |
| **Prometheus**       | Scraping de métricas de `/metrics` |
| **Grafana**          | Visualización del dashboard        |
| **Slack** (opcional) | Alertas de drift                   |

---

## 📊 Relación entre Fases

```
FASE 1 (Prompts)
    └──▶ FASE 2 (Dataset) ──▶ 2,989 conversaciones
            └──▶ FASE 3 (Training) ──▶ Modelo GGUF Q4_K_M
                    └──▶ FASE 4 (Deploy) ──▶ LLM Server en K8s
                            └──▶ FASE 5 (Mejora Continua) ◀── Estás aquí
                                    │
                                    ├── Monitoreo en tiempo real (Prometheus + Grafana)
                                    ├── Evaluación periódica (test set + safety + RD)
                                    ├── Feedback → análisis → exportar para retrain
                                    ├── Drift detection → alertas → trigger retrain
                                    ├── Re-entrenamiento → Colab → nuevo GGUF
                                    └── A/B testing → significancia estadística → promote
```

---

## ⚠️ Notas Importantes

1. **Dialogflow está ELIMINADO** — Todo el stack de NLU es ahora LLM (Llama 3 8B fine-tuned)
2. **Prometheus es nuevo** — Antes de FASE 5, el monitoring era solo in-memory counters y DB queries
3. **Los scripts de evaluación requieren el LLM server corriendo** — Usan HTTP contra `/v1/chat/completions`
4. **El re-entrenamiento es manual via Colab** — El script se genera automáticamente pero debe ejecutarse en Google Colab con GPU
5. **A/B testing requiere routing** — El `LlmService.cs` en ChatbotService debe implementar lógica de routing de tráfico entre variantes

---

_FASE 5 — Mejora Continua — OKLA Chatbot LLM — Febrero 2026_
