# 🧠 FASE 3 — Fine-Tuning Llama 3 8B con QLoRA

> **Proyecto:** OKLA — Marketplace de Vehículos (República Dominicana)  
> **Última actualización:** Febrero 15, 2026

---

## ⚠️ DIRECTIVA CRÍTICA — REEMPLAZO DE DIALOGFLOW

> El ChatbotService actual basado en Google Dialogflow ES **DEBE SER COMPLETAMENTE ELIMINADO**  
> y reemplazado por el modelo LLM fine-tuned producido en esta fase.

---

## 📋 Contenido

| Archivo                      | Descripción                                        |
| ---------------------------- | -------------------------------------------------- |
| `okla_finetune_llama3.ipynb` | Notebook principal — ejecutar en Colab via VS Code |

## 🔧 Pre-requisitos

### 1. Extensión Colab en VS Code

La extensión **Google Colab** (`Google.colab`) debe estar instalada en VS Code.

### 2. Cuenta de HuggingFace

- Crear cuenta en [huggingface.co](https://huggingface.co)
- Solicitar acceso a [meta-llama/Meta-Llama-3-8B-Instruct](https://huggingface.co/meta-llama/Meta-Llama-3-8B-Instruct)
- Generar token en [Settings → Tokens](https://huggingface.co/settings/tokens)

### 3. Dataset de FASE 2

Los archivos JSONL generados en FASE 2:

- `okla_train.jsonl` (80% — ~2,400 conversaciones)
- `okla_eval.jsonl` (10% — ~300 conversaciones)
- `okla_test.jsonl` (10% — ~300 conversaciones)

---

## 🚀 Cómo Ejecutar

### Paso 1: Abrir notebook

Abre `okla_finetune_llama3.ipynb` en VS Code.

### Paso 2: Conectar a Colab

1. Click **"Select Kernel"** (esquina superior derecha)
2. Selecciona **"Colab"** → **"New Colab Server"**
3. Inicia sesión con tu cuenta de Google
4. El runtime se conecta automáticamente con **GPU T4 (16GB)**

### Paso 3: Ejecutar celdas

Ejecuta celda por celda en orden:

| #   | Celda                 | Tiempo estimado |
| --- | --------------------- | --------------- |
| 1   | Verificar GPU         | ~5 seg          |
| 2   | Instalar dependencias | ~3-5 min        |
| 3   | Subir dataset         | ~1 min (upload) |
| 4   | Cargar y formatear    | ~2 min          |
| 5   | Cargar modelo 4-bit   | ~5-8 min        |
| 6   | Configurar QLoRA      | ~10 seg         |
| 7   | Entrenar              | **45-120 min**  |
| 8   | Evaluar               | ~5-10 min       |
| 9   | Guardar modelo        | ~5-10 min       |
| 10  | Exportar GGUF         | ~10-15 min      |
| 11  | Subir a Drive         | ~5 min          |

**Tiempo total estimado: ~2-3 horas**

---

## 📊 Especificaciones Técnicas

| Componente                 | Configuración                                                 |
| -------------------------- | ------------------------------------------------------------- |
| **Modelo base**            | Meta-Llama-3-8B-Instruct                                      |
| **Cuantización**           | NF4 (4-bit) con doble cuantización                            |
| **Método**                 | QLoRA (PEFT)                                                  |
| **LoRA rank**              | 16                                                            |
| **LoRA alpha**             | 32                                                            |
| **Target modules**         | q_proj, k_proj, v_proj, o_proj, gate_proj, up_proj, down_proj |
| **Parámetros entrenables** | ~1.5-2% del total                                             |
| **Optimizer**              | Paged AdamW 8-bit                                             |
| **Learning rate**          | 2e-4 con cosine annealing                                     |
| **Batch size**             | 2 × 8 gradient accumulation = 16 efectivo                     |
| **Epochs**                 | 3                                                             |
| **Precision**              | FP16 (T4)                                                     |
| **Max seq length**         | Dinámico (P95 del dataset)                                    |
| **VRAM requerida**         | ~12-14 GB (cabe en T4 16GB)                                   |

---

## 📦 Artefactos de Salida

Después del entrenamiento, tendrás:

| Artefacto            | Tamaño     | Uso                                |
| -------------------- | ---------- | ---------------------------------- |
| **LoRA Adapters**    | ~50-100 MB | Para cargar sobre modelo base      |
| **Merged Model**     | ~15 GB     | Modelo completo en FP16            |
| **GGUF Q4_K_M**      | ~4.7 GB    | **Producción** — CPU con 6-8GB RAM |
| **Training Metrics** | <1 MB      | Loss, perplexity, JSON accuracy    |

### Ubicación en Google Drive

```
MyDrive/OKLA/models/
├── okla-llama3-adapter/        # LoRA adapters
├── okla-llama3-8b-q4_k_m.gguf # Modelo para producción
└── training_metrics/           # Métricas
```

---

## 🎯 Métricas de Calidad Esperadas

| Métrica              | Umbral aceptable | Ideal |
| -------------------- | ---------------- | ----- |
| **Eval loss**        | < 1.5            | < 0.8 |
| **Perplexity**       | < 5.0            | < 2.5 |
| **JSON válido**      | > 85%            | > 95% |
| **Campos completos** | > 80%            | > 90% |
| **Intent correcto**  | > 70%            | > 85% |

---

## ⚠️ Colab Free — Limitaciones

| Limitación       | Impacto                  | Mitigación                     |
| ---------------- | ------------------------ | ------------------------------ |
| Sesión max 12h   | Puede desconectarse      | Checkpoints cada 100 steps     |
| GPU intermitente | Puede no haber GPU       | Reintentar más tarde           |
| RAM ~12.7GB      | Merge puede fallar       | Skip merge, usar solo adapters |
| Disco ~78GB      | Suficiente para Llama 8B | Limpiar antes de GGUF export   |

---

## 🔜 Siguiente: FASE 4 — Deployment

El GGUF generado aquí se usará en FASE 4 para:

1. Servir con `llama.cpp` server en Docker/K8s
2. Reemplazar `DialogflowService.cs` por `LlmService.cs`
3. Nuevo endpoint `/api/chatbot/llm/completions`
