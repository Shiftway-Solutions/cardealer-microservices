# 🤖 ChatbotService - Estrategia Completa

## Microservicio de Chatbot con Calificación de Leads para OKLA

**Fecha:** Enero 2026  
**Puerto:** 5060  
**Versión:** 1.0

---

## 📋 RESUMEN EJECUTIVO

### Concepto Mejorado

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FLUJO DEL CHATBOT OKLA                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  👤 Comprador                                                               │
│      │                                                                      │
│      ▼                                                                      │
│  ┌────────────────────────────────────────┐                                │
│  │  1️⃣ PRIMER CONTACTO                    │                                │
│  │  Widget de chat en listing             │                                │
│  │  "¡Hola! ¿Te interesa este Toyota?"    │                                │
│  └────────────────────────────────────────┘                                │
│      │                                                                      │
│      ▼                                                                      │
│  ┌────────────────────────────────────────┐                                │
│  │  2️⃣ CONVERSACIÓN INTELIGENTE           │                                │
│  │  - Responde preguntas del vehículo     │                                │
│  │  - Detecta intención de compra         │                                │
│  │  - Recopila información del lead       │                                │
│  └────────────────────────────────────────┘                                │
│      │                                                                      │
│      ▼                                                                      │
│  ┌────────────────────────────────────────┐                                │
│  │  3️⃣ CALIFICACIÓN DEL LEAD (ML)         │                                │
│  │  🔥 HOT (85-100): Conectar ahora       │                                │
│  │  🟡 WARM (50-84): Nutrir más           │                                │
│  │  🔵 COLD (0-49): Solo seguimiento      │                                │
│  └────────────────────────────────────────┘                                │
│      │                                                                      │
│      ▼ (Si es HOT)                                                         │
│  ┌────────────────────────────────────────┐                                │
│  │  4️⃣ TRANSFERENCIA A WHATSAPP           │                                │
│  │  "Te conecto con Juan del dealer..."   │                                │
│  │  → WhatsApp Business API               │                                │
│  │  → Notificación push al vendedor       │                                │
│  └────────────────────────────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Propuesta de Valor

| Problema Actual | Solución con Chatbot |
|-----------------|---------------------|
| Dealers pierden tiempo con leads fríos | Solo reciben leads calificados (HOT) |
| Compradores no obtienen respuestas rápidas | Respuesta instantánea 24/7 |
| No hay filtro de intención | ML detecta intención real de compra |
| Leads se pierden fuera de horario | Chatbot captura leads siempre |

---

## 🏗️ ARQUITECTURA DEL SERVICIO

### Stack Tecnológico

| Componente | Tecnología | Propósito |
|------------|------------|-----------|
| **LLM Base** | OpenAI GPT-4o-mini | Conversación natural |
| **Framework** | LangChain (.NET) | Orquestación de prompts |
| **Vector DB** | Pinecone | RAG para info de vehículos |
| **Backend** | .NET 8 | API del microservicio |
| **Real-time** | SignalR | Chat en tiempo real |
| **WhatsApp** | Twilio/Meta API | Transferencia de leads |
| **Analytics** | ClickHouse | Métricas de conversaciones |

### Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ChatbotService (Puerto 5060)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │   Chat Widget   │───▶│  SignalR Hub    │───▶│ Conversation    │         │
│  │   (Frontend)    │    │  (Real-time)    │    │ Manager         │         │
│  └─────────────────┘    └─────────────────┘    └────────┬────────┘         │
│                                                          │                  │
│                         ┌────────────────────────────────┼──────────────┐  │
│                         │                                ▼              │  │
│                         │         ┌─────────────────────────────────┐   │  │
│                         │         │        LLM Orchestrator         │   │  │
│                         │         │  ┌─────────┐  ┌──────────────┐  │   │  │
│                         │         │  │ System  │  │   RAG        │  │   │  │
│                         │         │  │ Prompt  │  │  (Pinecone)  │  │   │  │
│                         │         │  └─────────┘  └──────────────┘  │   │  │
│                         │         └─────────────────────────────────┘   │  │
│                         │                        │                      │  │
│  ┌─────────────────┐    │    ┌───────────────────▼───────────────────┐  │  │
│  │ Lead Scoring    │◀───┼────│         Intent Analyzer              │  │  │
│  │ Service (5055)  │    │    │  - Buying signals detection          │  │  │
│  └────────┬────────┘    │    │  - Urgency detection                 │  │  │
│           │             │    │  - Budget qualification              │  │  │
│           │             │    └───────────────────────────────────────┘  │  │
│           ▼             │                                               │  │
│  ┌─────────────────┐    │    ┌───────────────────────────────────────┐  │  │
│  │ WhatsApp        │◀───┼────│      Handoff Controller              │  │  │
│  │ Integration     │    │    │  - Transfer to human agent           │  │  │
│  └─────────────────┘    │    │  - Context preservation              │  │  │
│                         │    └───────────────────────────────────────┘  │  │
│                         └───────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 💬 SISTEMA DE CONVERSACIÓN

### System Prompt Base

```
Eres OKLA Bot, el asistente virtual de OKLA.com.do, el marketplace #1 de 
vehículos en República Dominicana.

TU PERSONALIDAD:
- Amigable y profesional
- Conocedor de vehículos
- Hablas español dominicano (pero entiendes inglés)
- Usas emojis con moderación

TU OBJETIVO PRINCIPAL:
1. Ayudar al usuario con información del vehículo
2. Detectar si tiene intención REAL de comprar
3. Calificar el lead (HOT/WARM/COLD)
4. Si es HOT, transferir al vendedor

INFORMACIÓN QUE DEBES RECOPILAR (naturalmente):
- ¿Es para uso personal o negocio?
- ¿Cuándo planea comprar? (urgencia)
- ¿Ya tiene financiamiento o pagará cash?
- ¿Tiene vehículo para trade-in?
- ¿Ya vio el vehículo en persona?

SEÑALES DE LEAD HOT:
✅ Pregunta por disponibilidad inmediata
✅ Pregunta por financiamiento específico
✅ Quiere agendar test drive
✅ Pregunta por documentos para comprar
✅ Menciona que tiene el dinero listo
✅ Pregunta por proceso de trade-in

SEÑALES DE LEAD COLD:
❌ Solo pregunta precio sin más interés
❌ Dice "solo estoy mirando"
❌ No responde preguntas de seguimiento
❌ Tiempo de compra > 6 meses

NUNCA:
- Inventes información del vehículo
- Des precios que no están en el listing
- Presiones al usuario agresivamente
- Compartas información de otros usuarios
```

### Flujo de Conversación Ejemplo

```
OKLA Bot: ¡Hola! 👋 Veo que estás viendo este Toyota RAV4 2024. 
          ¿Te puedo ayudar con alguna pregunta?

Usuario:  Cuánto cuesta?

OKLA Bot: El Toyota RAV4 2024 está en $42,500 USD. 
          Es la versión XLE con:
          - 23,000 km
          - Sensores de reversa
          - Pantalla táctil
          
          ¿Es para uso personal o para tu negocio? 🚗

Usuario:  Personal, para mi familia

OKLA Bot: ¡Excelente elección para familia! El RAV4 tiene 5 asientos 
          muy cómodos y buen espacio de carga.
          
          ¿Ya tienes idea de cuándo te gustaría tenerlo? 📅

Usuario:  Lo antes posible, necesito cambiar mi carro actual

OKLA Bot: Entiendo la urgencia 👍 
          
          ¿Tienes vehículo para trade-in? El dealer ofrece 
          evaluación gratuita y podría reducir el precio final.

Usuario:  Sí, tengo un Honda CRV 2019

OKLA Bot: ¡Perfecto! Un Honda CRV 2019 tiene buen valor de trade-in.
          
          ¿Te gustaría que te conecte con Juan del dealer 
          "AutoMax RD" para una evaluación? Él puede:
          - Evaluar tu trade-in
          - Darte precio final
          - Agendar test drive
          
          ¿Te parece? 📱

Usuario:  Sí, dale

OKLA Bot: ¡Listo! Te conecto con Juan por WhatsApp ahora mismo.
          
          📱 Abriendo WhatsApp...
          
          Juan ya tiene el contexto de nuestra conversación 
          y los detalles del vehículo. ¡Éxito con tu compra! 🎉

[Sistema: Lead calificado como HOT (92%) - Transferido a WhatsApp]
```

---

## 🎯 SISTEMA DE CALIFICACIÓN DE LEADS

### Señales de Intención (Features para ML)

| Categoría | Señal | Peso |
|-----------|-------|------|
| **Urgencia** | "Lo necesito esta semana" | +25 |
| **Urgencia** | "En los próximos días" | +20 |
| **Urgencia** | "Este mes" | +15 |
| **Urgencia** | "Solo mirando" | -15 |
| **Financiero** | "Tengo el dinero" | +20 |
| **Financiero** | Pregunta por financiamiento | +15 |
| **Financiero** | Pregunta por inicial | +15 |
| **Financiero** | No menciona budget | -10 |
| **Trade-in** | Tiene vehículo para entregar | +15 |
| **Compromiso** | Quiere test drive | +25 |
| **Compromiso** | Pregunta por documentos | +20 |
| **Compromiso** | Pregunta disponibilidad | +15 |
| **Engagement** | Respuestas largas/detalladas | +10 |
| **Engagement** | Respuestas cortas/evasivas | -10 |
| **Engagement** | Hace múltiples preguntas | +10 |

### Modelo de Scoring

```python
# Pseudocódigo del modelo de scoring
def calculate_lead_score(conversation):
    base_score = 50
    
    # Análisis de intención con LLM
    intent_analysis = llm.analyze(
        conversation,
        prompt="Analiza la intención de compra..."
    )
    
    # Features extraídas
    features = {
        'urgency_score': detect_urgency(conversation),
        'financial_readiness': detect_financial_signals(conversation),
        'engagement_level': calculate_engagement(conversation),
        'buying_signals_count': count_buying_signals(conversation),
        'objections_count': count_objections(conversation),
        'questions_about_purchase': count_purchase_questions(conversation)
    }
    
    # Modelo ML entrenado
    score = ml_model.predict(features)
    
    return {
        'score': score,  # 0-100
        'category': 'HOT' if score >= 85 else 'WARM' if score >= 50 else 'COLD',
        'confidence': ml_model.confidence,
        'key_signals': extract_key_signals(conversation),
        'recommended_action': get_recommended_action(score)
    }
```

### Acciones por Categoría

| Score | Categoría | Acción Automática |
|-------|-----------|-------------------|
| 85-100 | 🔥 HOT | Transferir a WhatsApp inmediatamente |
| 70-84 | 🟠 WARM-HOT | Ofrecer test drive, insistir un poco |
| 50-69 | 🟡 WARM | Nutrir con más información |
| 30-49 | 🔵 COOL | Email follow-up automático |
| 0-29 | ❄️ COLD | Solo guardar para remarketing |

---

## 📚 ENTRENAMIENTO DEL MODELO

### Fase 1: Recopilación de Datos (Semanas 1-4)

#### Fuentes de Datos

```
┌─────────────────────────────────────────────────────────────────┐
│                    FUENTES DE DATOS DE ENTRENAMIENTO            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1️⃣ DATOS INTERNOS (Más valiosos)                              │
│  ├── Conversaciones WhatsApp históricas de dealers            │
│  ├── Emails de consultas de vehículos                         │
│  ├── Formularios de contacto completados                      │
│  ├── Leads que terminaron en venta (positivos)                │
│  └── Leads que no compraron (negativos)                       │
│                                                                 │
│  2️⃣ DATOS SINTÉTICOS                                           │
│  ├── Conversaciones generadas con GPT-4                       │
│  ├── Variaciones de preguntas comunes                         │
│  └── Escenarios de edge cases                                 │
│                                                                 │
│  3️⃣ DATOS EXTERNOS                                             │
│  ├── Datasets públicos de conversaciones de ventas            │
│  ├── FAQs de otros marketplaces de autos                      │
│  └── Reviews y comentarios de compradores                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

#### Volumen Necesario

| Tipo de Dato | Cantidad Mínima | Objetivo |
|--------------|-----------------|----------|
| Conversaciones etiquetadas | 2,000 | 5,000 |
| Leads HOT confirmados | 500 | 1,500 |
| Leads que compraron | 200 | 500 |
| Preguntas únicas | 500 | 2,000 |

### Fase 2: Preparación de Datos (Semanas 5-6)

```python
# Estructura de datos de entrenamiento
training_example = {
    "conversation_id": "conv_12345",
    "messages": [
        {"role": "user", "content": "Cuánto cuesta este carro?"},
        {"role": "assistant", "content": "El Toyota está en $42,500..."},
        # ... más mensajes
    ],
    "metadata": {
        "vehicle_id": "veh_abc123",
        "vehicle_type": "SUV",
        "vehicle_price": 42500,
        "dealer_id": "dealer_xyz"
    },
    "labels": {
        "lead_score": 87,
        "lead_category": "HOT",
        "converted_to_sale": True,
        "time_to_conversion": "5 days",
        "buying_signals": [
            "asked_about_financing",
            "has_trade_in",
            "wants_test_drive"
        ]
    }
}
```

### Fase 3: Fine-tuning del Modelo (Semanas 7-10)

#### Opción A: Fine-tuning de GPT-4o-mini (Recomendado)

```python
# Preparar datos en formato OpenAI
training_data = [
    {
        "messages": [
            {
                "role": "system",
                "content": SYSTEM_PROMPT
            },
            {
                "role": "user", 
                "content": "Cuánto cuesta?"
            },
            {
                "role": "assistant",
                "content": "El vehículo está en $42,500..."
            }
        ]
    }
]

# Subir a OpenAI
client.files.create(
    file=open("training.jsonl", "rb"),
    purpose="fine-tune"
)

# Crear fine-tune job
client.fine_tuning.jobs.create(
    training_file="file-abc123",
    model="gpt-4o-mini-2024-07-18"
)
```

#### Opción B: RAG con Embeddings

```python
# Crear embeddings de información de vehículos
from langchain.embeddings import OpenAIEmbeddings
from langchain.vectorstores import Pinecone

embeddings = OpenAIEmbeddings()

# Indexar información de vehículos
documents = [
    {
        "content": "Toyota RAV4 2024 XLE...",
        "metadata": {"vehicle_id": "123", "price": 42500}
    }
]

vectorstore = Pinecone.from_documents(
    documents,
    embeddings,
    index_name="okla-vehicles"
)

# Query con contexto
context = vectorstore.similarity_search(user_question, k=3)
response = llm.chat(
    system=SYSTEM_PROMPT,
    context=context,
    user_message=user_question
)
```

### Fase 4: Modelo de Lead Scoring (Semanas 11-12)

```python
# Entrenar modelo de clasificación de leads
from sklearn.ensemble import GradientBoostingClassifier
import xgboost as xgb

# Features del lead
features = [
    'message_count',
    'avg_response_length',
    'urgency_keywords_count',
    'financial_keywords_count',
    'asked_test_drive',
    'asked_availability',
    'has_trade_in',
    'time_in_conversation',
    'sentiment_score',
    'question_depth_score'
]

# Entrenar
model = xgb.XGBClassifier(
    n_estimators=100,
    max_depth=6,
    learning_rate=0.1
)
model.fit(X_train, y_train)

# Evaluar
accuracy = model.score(X_test, y_test)
# Target: > 85% accuracy
```

---

## 💰 ESTIMACIÓN DE COSTOS

### Costos de Desarrollo (Una vez)

| Concepto | Costo Estimado |
|----------|----------------|
| Desarrollo del microservicio (3 devs, 2 meses) | $15,000 - $25,000 |
| Integración WhatsApp Business API | $2,000 - $5,000 |
| Fine-tuning y entrenamiento ML | $3,000 - $8,000 |
| Testing y QA | $3,000 - $5,000 |
| **Total Desarrollo** | **$23,000 - $43,000** |

### Costos Mensuales de Operación

| Servicio | Costo/Mes (Estimado) |
|----------|---------------------|
| **OpenAI API (GPT-4o-mini)** | |
| - 50,000 conversaciones/mes | $150 - $300 |
| - Fine-tuned model hosting | $0 (incluido) |
| **Pinecone (Vector DB)** | |
| - Starter plan | $70/mes |
| **WhatsApp Business API** | |
| - Twilio (per message) | $100 - $300 |
| - Meta direct (cheaper) | $50 - $150 |
| **Infraestructura** | |
| - Kubernetes pod (1 replica) | $30 - $50 |
| - Redis para sesiones | $20 - $30 |
| **Monitoring** | |
| - Logs y métricas | $20 - $40 |
| **Total Mensual** | **$440 - $870** |

### Cálculo de ROI

```
Escenario: 100 leads HOT/mes transferidos a WhatsApp

Sin Chatbot:
- Dealer responde a 500 mensajes (todos los leads)
- Tiempo promedio: 10 min/lead = 83 horas/mes
- Solo 20% son leads serios
- Conversión: 5%

Con Chatbot:
- Chatbot filtra y solo transfiere 100 leads HOT
- Tiempo dealer: 10 min/lead = 17 horas/mes
- 85% son leads serios
- Conversión esperada: 15%

Ahorro en tiempo: 66 horas/mes
Valor del tiempo (a $20/hora): $1,320/mes

Incremento en ventas:
- Sin chatbot: 500 * 0.2 * 0.05 = 5 ventas
- Con chatbot: 100 * 0.85 * 0.15 = 12.75 ventas
- Incremento: +7.75 ventas/mes

Si comisión promedio = $500/venta:
Incremento en ingresos: $3,875/mes

ROI = (Beneficio - Costo) / Costo
ROI = ($1,320 + $3,875 - $655) / $655 = 693%
```

### Desglose de Costos OpenAI

```
GPT-4o-mini pricing (Enero 2026):
- Input: $0.15 / 1M tokens
- Output: $0.60 / 1M tokens

Conversación promedio:
- System prompt: ~500 tokens
- Contexto RAG: ~1,000 tokens
- Usuario (5 mensajes): ~200 tokens
- Asistente (5 respuestas): ~500 tokens
- Total: ~2,200 tokens/conversación

50,000 conversaciones/mes:
- Input tokens: 85M tokens = $12.75
- Output tokens: 25M tokens = $15.00
- Total: ~$28/mes (base)

Con overhead y retries: ~$50-100/mes
```

---

## 🚀 PLAN DE PRODUCCIÓN

### Infraestructura

```yaml
# k8s/chatbot-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: chatbotservice
  namespace: okla
spec:
  replicas: 2  # Alta disponibilidad
  selector:
    matchLabels:
      app: chatbotservice
  template:
    spec:
      containers:
      - name: chatbotservice
        image: ghcr.io/gregorymorenoiem/cardealer-chatbotservice:latest
        ports:
        - containerPort: 8080
        env:
        - name: OPENAI_API_KEY
          valueFrom:
            secretKeyRef:
              name: chatbot-secrets
              key: openai-api-key
        - name: PINECONE_API_KEY
          valueFrom:
            secretKeyRef:
              name: chatbot-secrets
              key: pinecone-api-key
        - name: WHATSAPP_TOKEN
          valueFrom:
            secretKeyRef:
              name: chatbot-secrets
              key: whatsapp-token
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: chatbotservice
  namespace: okla
spec:
  ports:
  - port: 8080
    targetPort: 8080
  selector:
    app: chatbotservice
```

### Configuración del Gateway

```json
// ocelot.prod.json - Nueva ruta
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/chat/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "chatbotservice", "Port": 8080 }
      ],
      "UpstreamPathTemplate": "/api/chat/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "OPTIONS" ]
    }
  ]
}
```

### Roadmap de Implementación

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ROADMAP DE IMPLEMENTACIÓN                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  FASE 1: MVP (Semanas 1-4)                                     ████░░░░░   │
│  ├── Microservicio base con .NET 8                                         │
│  ├── Integración OpenAI GPT-4o-mini                                        │
│  ├── Chat widget básico en frontend                                        │
│  └── Transferencia manual a WhatsApp                                       │
│                                                                             │
│  FASE 2: RAG + Contexto (Semanas 5-8)                          ░░░░████░   │
│  ├── Pinecone para información de vehículos                                │
│  ├── Contexto de listings en conversación                                  │
│  ├── Historial de conversaciones                                           │
│  └── Métricas básicas                                                      │
│                                                                             │
│  FASE 3: Lead Scoring (Semanas 9-12)                           ░░░░░░░███  │
│  ├── Modelo de clasificación de leads                                      │
│  ├── Integración con LeadScoringService (5055)                             │
│  ├── Transferencia automática a WhatsApp                                   │
│  └── Dashboard para dealers                                                │
│                                                                             │
│  FASE 4: Fine-tuning (Semanas 13-16)                           ░░░░░░░░░██ │
│  ├── Recopilar datos de conversaciones reales                              │
│  ├── Fine-tune modelo personalizado                                        │
│  ├── A/B testing                                                           │
│  └── Optimización continua                                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 MÉTRICAS Y KPIs

### Métricas del Chatbot

| Métrica | Objetivo | Cómo Medir |
|---------|----------|------------|
| Tasa de respuesta | > 95% | Mensajes respondidos / total |
| Tiempo de respuesta | < 3 seg | Promedio de latencia |
| Satisfacción usuario | > 4.2/5 | Encuesta post-chat |
| Tasa de transferencia | 15-25% | Leads transferidos / conversaciones |
| Precisión de scoring | > 85% | Predicción vs realidad |

### Métricas de Negocio

| Métrica | Objetivo | Fórmula |
|---------|----------|---------|
| Conversión HOT leads | > 15% | Ventas / Leads HOT |
| Ahorro de tiempo dealer | > 50% | Tiempo antes - después |
| Incremento en ventas | > 20% | Ventas con chatbot / sin |
| Costo por lead calificado | < $5 | Costo mensual / leads HOT |

---

## 🔌 APIs del Servicio

### Endpoints Principales

```csharp
// ChatController.cs
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    // Iniciar conversación
    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDto>> StartConversation(
        StartConversationRequest request)
    
    // Enviar mensaje
    [HttpPost("conversations/{id}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(
        Guid id, SendMessageRequest request)
    
    // Obtener historial
    [HttpGet("conversations/{id}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(Guid id)
    
    // Transferir a WhatsApp
    [HttpPost("conversations/{id}/transfer")]
    public async Task<ActionResult> TransferToWhatsApp(
        Guid id, TransferRequest request)
    
    // Obtener score del lead
    [HttpGet("conversations/{id}/score")]
    public async Task<ActionResult<LeadScoreDto>> GetLeadScore(Guid id)
}
```

### WebSocket (SignalR)

```typescript
// Frontend - conexión al chat
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/api/chat/hub")
    .withAutomaticReconnect()
    .build();

// Recibir mensajes
connection.on("ReceiveMessage", (message: ChatMessage) => {
    setMessages(prev => [...prev, message]);
});

// Enviar mensaje
await connection.invoke("SendMessage", {
    conversationId: convId,
    content: userInput
});

// Notificación de typing
connection.on("BotTyping", () => {
    setIsTyping(true);
});
```

---

## 🔒 CONSIDERACIONES DE SEGURIDAD

### Protección de Datos

```
✅ Encriptar conversaciones en reposo (AES-256)
✅ TLS 1.3 para todas las comunicaciones
✅ No almacenar datos de tarjetas de crédito
✅ Cumplir con GDPR/ley de protección de datos RD
✅ Opción de eliminar historial de conversaciones
✅ Rate limiting por usuario
✅ Detección de spam/abuso
```

### Moderación de Contenido

```python
# Filtrar contenido inapropiado
def moderate_message(message):
    # OpenAI moderation API
    response = openai.Moderation.create(input=message)
    
    if response.results[0].flagged:
        return {
            "allowed": False,
            "reason": response.results[0].categories
        }
    
    return {"allowed": True}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Pre-desarrollo
- [ ] Definir casos de uso detallados
- [ ] Recopilar conversaciones históricas de dealers
- [ ] Configurar cuenta OpenAI con billing
- [ ] Configurar Pinecone
- [ ] Configurar WhatsApp Business API (Twilio o Meta)

### Desarrollo
- [ ] Crear proyecto ChatbotService.Api
- [ ] Implementar integración OpenAI
- [ ] Implementar SignalR hub
- [ ] Crear widget de chat en frontend
- [ ] Implementar RAG con Pinecone
- [ ] Crear modelo de lead scoring
- [ ] Integrar con LeadScoringService
- [ ] Implementar transferencia a WhatsApp

### Testing
- [ ] Unit tests (>80% coverage)
- [ ] Integration tests con mocks de OpenAI
- [ ] Load testing (1000 conversaciones simultáneas)
- [ ] A/B testing de prompts
- [ ] User acceptance testing con dealers

### Producción
- [ ] Configurar secrets en Kubernetes
- [ ] Desplegar a DOKS
- [ ] Configurar autoscaling
- [ ] Configurar alertas y monitoreo
- [ ] Documentar runbook de operaciones
- [ ] Capacitar equipo de soporte

---

## 📞 INTEGRACIÓN WHATSAPP

### Opción A: Twilio API

```csharp
// WhatsAppService.cs
public class TwilioWhatsAppService : IWhatsAppService
{
    public async Task<bool> SendMessage(
        string toPhone, 
        string message,
        ConversationContext context)
    {
        var messageOptions = new CreateMessageOptions(
            new PhoneNumber($"whatsapp:{toPhone}"))
        {
            From = new PhoneNumber("whatsapp:+1234567890"),
            Body = FormatHandoffMessage(message, context)
        };
        
        var msg = await MessageResource.CreateAsync(messageOptions);
        return msg.Status != MessageResource.StatusEnum.Failed;
    }
    
    private string FormatHandoffMessage(string intro, ConversationContext ctx)
    {
        return $"""
            {intro}
            
            📋 *Resumen de la conversación:*
            - Vehículo: {ctx.VehicleName}
            - Precio: ${ctx.VehiclePrice:N0}
            - Interés: {ctx.LeadScore}% (Lead {ctx.LeadCategory})
            
            💬 *Puntos clave:*
            {string.Join("\n", ctx.KeyPoints.Select(p => $"• {p}"))}
            
            El cliente está listo para hablar contigo.
            """;
    }
}
```

### Opción B: Meta WhatsApp Business API (Más económico)

```csharp
// MetaWhatsAppService.cs
public class MetaWhatsAppService : IWhatsAppService
{
    private readonly string _accessToken;
    private readonly string _phoneNumberId;
    
    public async Task<bool> SendTemplate(
        string toPhone,
        string templateName,
        Dictionary<string, string> parameters)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "es" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameters.Select(p => 
                            new { type = "text", text = p.Value })
                    }
                }
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"https://graph.facebook.com/v18.0/{_phoneNumberId}/messages",
            payload);
            
        return response.IsSuccessStatusCode;
    }
}
```

---

*Documento creado: Enero 2026*  
*Autor: Equipo OKLA*  
*Próxima revisión: Después de MVP*
