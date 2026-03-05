# 📊 FASE 2 — Generación de Dataset Sintético para Fine-Tuning

> **Última actualización:** Febrero 15, 2026

---

## 🎯 Objetivo

Generar un dataset de **3,000+ conversaciones sintéticas** en formato JSONL compatible con fine-tuning de Llama 3 (QLoRA), cubriendo **todos los 30+ IntentCategory** del ChatbotService de OKLA.

Cada ejemplo de entrenamiento sigue el formato **chat completion** (system/user/assistant) con respuestas JSON estructuradas.

---

## 📁 Estructura de Archivos

```
FASE_2_DATASET/
├── README.md                          # Este archivo
├── seed_vehicles.json                 # Inventario ficticio de 50 vehículos RD
├── seed_dealers.json                  # 5 dealers ficticios con configuración
├── conversation_templates.py          # Templates por intent con variantes
├── generate_dataset.py                # Script principal de generación
├── validate_dataset.py                # Validación y estadísticas
├── output/                            # (gitignored) Datasets generados
│   ├── okla_train.jsonl               # 80% entrenamiento
│   ├── okla_eval.jsonl                # 10% evaluación
│   ├── okla_test.jsonl                # 10% test
│   └── stats.json                     # Estadísticas de distribución
└── augmentation/                      # Scripts de aumentación
    └── paraphrase_variants.py         # Parafraseo con variaciones dominicanas
```

---

## 📐 Formato JSONL (Chat Completion)

Cada línea del JSONL sigue este formato compatible con fine-tuning de Llama 3:

```json
{
  "messages": [
    {
      "role": "system",
      "content": "Eres OKLA AI, el asistente virtual de {{dealer_name}}..."
    },
    {
      "role": "user",
      "content": "Quiero una yipeta que no pase de 2 millones"
    },
    {
      "role": "assistant",
      "content": "{\"response\": \"¡Claro! Tenemos varias yipetas...\", \"intent\": \"VehicleSearch\", ...}"
    },
    {
      "role": "user",
      "content": "Qué tal esa Hyundai Tucson? Tiene financiamiento?"
    },
    {
      "role": "assistant",
      "content": "{\"response\": \"La Hyundai Tucson 2022 es excelente...\", \"intent\": \"VehicleDetails\", ...}"
    }
  ]
}
```

### Estructura JSON del Assistant

```json
{
  "response": "Texto natural en español dominicano",
  "intent": "VehicleSearch",
  "confidence": 0.95,
  "isFallback": false,
  "parameters": {
    "bodyType": "SUV",
    "maxPrice": 2000000,
    "currency": "DOP"
  },
  "leadSignals": {
    "mentionedBudget": true,
    "requestedTestDrive": false,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggestedAction": null,
  "quickReplies": ["Ver fotos", "Agendar test drive", "Ver financiamiento"]
}
```

---

## 📊 Distribución Objetivo por Intent (3,000 conversaciones)

| IntentCategory            | # Conv | %    | Prioridad |
| ------------------------- | ------ | ---- | --------- |
| **VehicleSearch**         | 450    | 15%  | 🔴 Alta   |
| **VehicleDetails**        | 300    | 10%  | 🔴 Alta   |
| **VehiclePrice**          | 240    | 8%   | 🔴 Alta   |
| **FinancingInfo**         | 210    | 7%   | 🔴 Alta   |
| **TestDriveSchedule**     | 180    | 6%   | 🔴 Alta   |
| **VehicleComparison**     | 150    | 5%   | 🟡 Media  |
| **Greeting**              | 150    | 5%   | 🟡 Media  |
| **DealerHours**           | 120    | 4%   | 🟡 Media  |
| **DealerLocation**        | 120    | 4%   | 🟡 Media  |
| **FinancingCalculation**  | 120    | 4%   | 🟡 Media  |
| **FinancingRequirements** | 90     | 3%   | 🟡 Media  |
| **TradeIn**               | 90     | 3%   | 🟡 Media  |
| **VehicleAvailability**   | 90     | 3%   | 🟡 Media  |
| **VehicleFeatures**       | 90     | 3%   | 🟡 Media  |
| **ContactRequest**        | 75     | 2.5% | 🟢 Normal |
| **QuoteRequest**          | 75     | 2.5% | 🟢 Normal |
| **AppointmentSchedule**   | 60     | 2%   | 🟢 Normal |
| **DealerServices**        | 60     | 2%   | 🟢 Normal |
| **Farewell**              | 60     | 2%   | 🟢 Normal |
| **Complaint**             | 45     | 1.5% | 🟢 Normal |
| **WarrantyInfo**          | 45     | 1.5% | 🟢 Normal |
| **Help**                  | 30     | 1%   | ⚪ Baja   |
| **CallbackRequest**       | 30     | 1%   | ⚪ Baja   |
| **Feedback**              | 30     | 1%   | ⚪ Baja   |
| **AppointmentCancel**     | 15     | 0.5% | ⚪ Baja   |
| **AppointmentReschedule** | 15     | 0.5% | ⚪ Baja   |
| **ServiceAppointment**    | 15     | 0.5% | ⚪ Baja   |
| **PartsInquiry**          | 15     | 0.5% | ⚪ Baja   |
| **DealerContact**         | 15     | 0.5% | ⚪ Baja   |
| **Fallback**              | 15     | 0.5% | ⚪ Baja   |
| **Other**                 | 15     | 0.5% | ⚪ Baja   |

### Conversaciones Multi-Turn

- **60%** de las conversaciones tienen 2-4 turnos (multi-intent)
- **25%** tienen 5-8 turnos (conversación completa con lead)
- **15%** tienen 1 turno (pregunta directa)

---

## 🗣️ Variaciones Lingüísticas (Español Dominicano)

### Registros de Habla

| Registro          | %   | Ejemplo                                                             |
| ----------------- | --- | ------------------------------------------------------------------- |
| **Informal**      | 40% | "Dime a ver, tienen algo por ahí que no sea muy caro?"              |
| **Semi-formal**   | 40% | "Buenos días, estoy buscando un SUV para la familia"                |
| **Formal**        | 15% | "Quisiera información sobre vehículos disponibles en su inventario" |
| **WhatsApp/Chat** | 5%  | "q hay d carros q tengan"                                           |

### Modismos Dominicanos Incluidos

| Palabra RD       | Significado            | Frecuencia |
| ---------------- | ---------------------- | ---------- |
| yipeta           | SUV/crossover          | Alta       |
| guagua           | Van, minivan, vehículo | Alta       |
| motor            | Motocicleta            | Media      |
| pasola           | Scooter                | Baja       |
| pela'o / barato  | Económico              | Media      |
| chivo / flow     | Bonito, lujoso         | Media      |
| tato / ta bien   | OK, de acuerdo         | Alta       |
| vaina            | Cosa (genérico)        | Alta       |
| un chin          | Un poco                | Media      |
| klk / que lo que | Saludo informal        | Media      |
| jevi / chevere   | Genial                 | Media      |
| pila de          | Muchos                 | Baja       |
| motoconcho       | Mototaxi               | Baja       |
| concesionario    | Dealer                 | Media      |

### Moneda y Precios

- **DOP (dominicanos):** "2 millones", "millón y medio", "como 800 mil"
- **USD referencia:** "como 30 mil dólares" → convertir a ~1,770,000 DOP
- **Ambiguo:** "no muy caro" → $500K-$1.5M DOP

---

## 🚀 Cómo Usar

### Requisitos

```bash
pip install faker tqdm jsonlines pydantic
```

### Generar Dataset

```bash
cd docs/chatbot-llm/FASE_2_DATASET

# Generar 3,000 conversaciones
python generate_dataset.py --count 3000 --output output/

# Validar dataset
python validate_dataset.py output/okla_train.jsonl

# Ver estadísticas
python validate_dataset.py output/ --stats
```

### Splits

| Split | Archivo            | %           | Uso                         |
| ----- | ------------------ | ----------- | --------------------------- |
| Train | `okla_train.jsonl` | 80% (2,400) | Fine-tuning QLoRA           |
| Eval  | `okla_eval.jsonl`  | 10% (300)   | Validación durante training |
| Test  | `okla_test.jsonl`  | 10% (300)   | Evaluación final            |

---

## ⚠️ DIRECTIVA: REEMPLAZO DE DIALOGFLOW

Este dataset está diseñado para entrenar un modelo LLM que **REEMPLAZA COMPLETAMENTE** a Google Dialogflow ES. Las conversaciones del dataset:

1. ✅ Generan **respuestas JSON estructuradas** (no texto plano como Dialogflow)
2. ✅ Incluyen **detección de intent** en la respuesta (no dependen de Dialogflow)
3. ✅ Manejan **multi-turn context** nativamente (no con Dialogflow contexts)
4. ✅ Calculan **lead scoring** en la respuesta (no en un servicio separado)
5. ✅ Respetan **marco legal RD** en cada respuesta
6. ✅ Usan **español dominicano** natural (no traducciones genéricas)
