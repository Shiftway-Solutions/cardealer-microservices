# 🤖 Prompt Completo — Diseño, Entrenamiento y Producción del Chatbot LLM de OKLA

> **Uso:** Copiar este prompt completo y enviarlo a un AI (Claude, ChatGPT, etc.) para que diseñe el sistema modular de prompts, la estrategia de fine-tuning, y la guía de integración en producción para el chatbot LLM de OKLA.
>
> **Última actualización:** Febrero 15, 2026

---

## EL PROMPT

Necesito que me diseñes un **sistema completo end-to-end** para entrenar, desplegar y operar en producción un chatbot corporativo basado en Llama 3 70B (o modelo similar) para **OKLA**, un marketplace de compra y venta de vehículos en República Dominicana.

**Necesito que cubras las 5 fases completas del ciclo de vida:**

1. **FASE 1 — Diseño de Prompts**: Sistema modular de prompts para el LLM
2. **FASE 2 — Generación de Dataset**: Cómo crear el dataset sintético inicial para fine-tuning
3. **FASE 3 — Entrenamiento (Fine-tuning)**: Paso a paso práctico en Google Colab con QLoRA
4. **FASE 4 — Despliegue en Producción**: Cómo servir el modelo fine-tuneado via API y acoplarlo al backend existente (.NET 8 en Digital Ocean Kubernetes)
5. **FASE 5 — Mejora Continua**: Pipeline automatizado para recolectar datos reales, re-entrenar y re-desplegar

---

## PARTE 1: CONTEXTO COMPLETO DEL PROYECTO

### 1.1 — Qué es OKLA

**OKLA** es una plataforma multi-dealer (marketplace) donde concesionarios y vendedores individuales publican vehículos para venta en República Dominicana. No es un solo concesionario — cada dealer tiene su propia configuración de chatbot (nombre del bot, avatar, horarios de atención, canales habilitados, tono de comunicación).

#### Tipos de cuenta:

| Tipo                    | AccountType | Paga         | Objetivo                     |
| ----------------------- | ----------- | ------------ | ---------------------------- |
| **Comprador**           | Individual  | No (gratis)  | Encontrar y comprar vehículo |
| **Vendedor Individual** | Individual  | $29/listing  | Vender su vehículo personal  |
| **Dealer** ⭐           | Dealer      | $49-$299/mes | Vender inventario completo   |
| **Admin**               | Admin       | No (staff)   | Moderar plataforma           |

---

### 1.2 — Arquitectura Técnica Existente (Stack de Producción)

| Capa                   | Tecnología                | Detalle                                       |
| ---------------------- | ------------------------- | --------------------------------------------- |
| **Backend**            | .NET 8 LTS                | 86 microservicios, Clean Architecture         |
| **Frontend Web**       | Next.js 14 + TypeScript   | App Router, SSR/SSG                           |
| **Frontend Mobile**    | Flutter + Dart            | SDK >=3.4.0                                   |
| **Base de Datos**      | PostgreSQL 16+            | Una DB por servicio                           |
| **Cache**              | Redis 7+                  | Cache distribuido                             |
| **Message Broker**     | RabbitMQ 3.12+            | Eventos entre servicios                       |
| **API Gateway**        | Ocelot 22.0.1             | Routing interno                               |
| **Container Registry** | GitHub Container Registry | ghcr.io                                       |
| **Kubernetes**         | Digital Ocean DOKS 1.28+  | Cluster: `okla-cluster`, namespace: `okla`    |
| **CI/CD**              | GitHub Actions            | Build → Push → Deploy automatizado            |
| **DNS**                | okla.com.do               | BFF Pattern (Gateway NO expuesto al internet) |

**Patrón BFF (Backend For Frontend):**

```
Browser → okla.com.do/api/* → Next.js (rewrite) → gateway:8080 (red interna K8s) → microservicios
```

---

### 1.3 — ChatbotService Existente (Lo que ya tenemos en código)

Ya existe un `ChatbotService` en el backend (.NET 8, Clean Architecture) integrado con **Google Dialogflow ES** para NLU. El objetivo es **complementar/reemplazar Dialogflow con un LLM** para manejar conversaciones más naturales y complejas.

#### Arquitectura del ChatbotService:

```
ChatbotService/
├── ChatbotService.Api/              # Controllers, Program.cs, Workers
│   ├── Controllers/
│   │   ├── ChatController.cs        # API pública del chat
│   │   └── ConfigurationController.cs # Config por dealer (admin)
│   ├── Workers/
│   │   └── MaintenanceWorkerService.cs # Tareas CRON en background
│   └── Program.cs                   # Startup (.NET 8, JWT, EF Core, Redis)
├── ChatbotService.Application/      # CQRS con MediatR
│   └── Features/Sessions/Commands/
│       └── SessionCommandHandlers.cs # StartSession, SendMessage, EndSession, TransferToAgent
├── ChatbotService.Domain/           # Entidades, Interfaces, Enums
│   ├── Entities/                    # 10 entidades
│   ├── Enums/                       # SessionType, LeadTemperature, IntentCategory, etc.
│   ├── Models/                      # DialogflowDetectionResult, etc.
│   └── Interfaces/                  # IDialogflowService, IInventorySyncService, etc.
└── ChatbotService.Infrastructure/   # Implementaciones
    ├── Persistence/                 # EF Core + PostgreSQL (DB: chatbotservice)
    └── Services/
        ├── DialogflowService.cs     # ⬅ AQUÍ ES DONDE SE CONECTA EL LLM
        ├── AutoLearningService.cs   # Clustering de fallbacks para mejora
        ├── InventorySyncService.cs  # Sync de vehículos desde VehiclesSaleService
        ├── HealthMonitoringService.cs
        └── ReportingService.cs
```

#### Entidades de datos existentes:

| Entidad                | Campos clave                                                                                                                                                                                                                                                                                                                  | Propósito                              |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- |
| `ChatSession`          | SessionToken, UserId, Channel (web/whatsapp/facebook/instagram), InteractionCount, MaxInteractionsPerSession (default:10), CurrentVehicleId, LeadId, Status, Language ("es"), IpAddress, DeviceType                                                                                                                           | Sesión de conversación                 |
| `ChatMessage`          | SessionId, Content, BotResponse, DialogflowIntentName, IntentCategory, ConfidenceScore, ConsumedInteraction (bool), InteractionCost ($0.002), ResponseTimeMs, IsFromBot                                                                                                                                                       | Mensaje individual                     |
| `ChatLead`             | FullName, Email, Phone, InterestedVehicleId, Budget, WantsFinancing, HasTradeIn, PreferredContactMethod, Status (New/Contacted/Qualified/Converted/Lost), Temperature (Cold/Warm/Hot), QualificationScore (0-100), Notes                                                                                                      | Lead generado                          |
| `ChatbotConfiguration` | DealerId, DialogflowProjectId, Plan, MaxInteractionsPerSession, BotName, WelcomeMessage, OfflineMessage, EnableWebChat, EnableWhatsApp, EnableFacebook, EnableInstagram, WhatsAppBusinessPhoneId, BusinessHoursJson, TimeZone ("America/Santo_Domingo"), EnableAutoLearning, InventorySyncIntervalMinutes, CrmIntegrationType | Config multi-tenant                    |
| `ChatbotVehicle`       | Make, Model, Year, Price, Mileage, FuelType, Transmission, BodyType, Colors, IsAvailable, ViewCount, InquiryCount                                                                                                                                                                                                             | Cache de inventario sincronizado       |
| `ChatbotFallback`      | Question, Frequency, Category, IsResolved                                                                                                                                                                                                                                                                                     | Preguntas sin resolver (auto-learning) |
| `QuickResponse`        | Name, Keywords, Response                                                                                                                                                                                                                                                                                                      | Respuestas rápidas sin IA ($0)         |
| `InteractionUsage`     | ConfigurationId, TotalInteractions, FreeUsed, PaidInteractions, TotalCost                                                                                                                                                                                                                                                     | Tracking de costos                     |
| `MaintenanceTask`      | Type, CronExpression, LastRun, NextRun, IsEnabled, Status                                                                                                                                                                                                                                                                     | Tareas programadas                     |
| `DialogflowIntent`     | IntentName, TrainingPhrases, Responses, SuggestedByAutoLearning                                                                                                                                                                                                                                                               | Intents sincronizados                  |

#### Pipeline actual de procesamiento de mensajes (`SendMessageCommandHandler`):

```csharp
// ESTE ES EL FLUJO EXACTO — El LLM debe integrarse aquí:

public async Task<ChatbotResponse> Handle(SendMessageCommand request, CancellationToken ct)
{
    // 1. Validar sesión por token
    var session = await _sessionRepository.GetByTokenAsync(request.SessionToken, ct);

    // 2. Verificar límite de interacciones (10/sesión default)
    if (session.InteractionLimitReached)
        return new ChatbotResponse { Response = "Límite alcanzado. Contacta un agente.", RemainingInteractions = 0 };

    // 3. Intentar Quick Response primero (GRATIS, no consume interacción)
    var quickResponse = await _quickResponseRepository.FindMatchingAsync(config.Id, request.Message, ct);

    if (quickResponse != null)
    {
        // Quick Response: costo $0, no consume interacción
        botResponse = quickResponse.Response;
        consumedInteraction = false;
    }
    else
    {
        // 4. ⬅⬅⬅ AQUÍ ENTRA EL LLM (actualmente usa Dialogflow) ⬅⬅⬅
        var dialogflowResult = await _dialogflowService.DetectIntentAsync(
            session.SessionToken, request.Message, session.Language ?? "es", ct);

        botResponse = dialogflowResult.FulfillmentText;
        intentName = dialogflowResult.DetectedIntent;
        confidenceScore = (decimal)dialogflowResult.ConfidenceScore;
        isFallback = dialogflowResult.IsFallback;
        consumedInteraction = true;                  // Costo: $0.002
        session.InteractionCount++;                  // Incrementa contador
        if (session.InteractionCount >= session.MaxInteractionsPerSession)
            session.InteractionLimitReached = true;  // Bloquea si llega al límite
    }

    // 5. Guardar mensaje del usuario en PostgreSQL
    var userMessage = new ChatMessage { Content = request.Message, IsFromBot = false, ConsumedInteraction = false };

    // 6. Guardar respuesta del bot en PostgreSQL
    var botMessage = new ChatMessage {
        Content = request.Message,
        BotResponse = botResponse,
        DialogflowIntentName = intentName,
        ConfidenceScore = confidenceScore,
        IsFromBot = true,
        ConsumedInteraction = consumedInteraction,
        InteractionCost = consumedInteraction ? 0.002m : 0m,
        ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
    };

    // 7. Retornar respuesta con metadata
    return new ChatbotResponse {
        Response = botResponse,
        IntentName = intentName,
        ConfidenceScore = confidenceScore,
        IsFallback = isFallback,
        RemainingInteractions = session.MaxInteractionsPerSession - session.InteractionCount
    };
}
```

#### Interface del servicio NLU actual — `IDialogflowService`:

```csharp
public interface IDialogflowService
{
    // Este es el método principal — el LLM debe retornar el mismo tipo de resultado
    Task<DialogflowDetectionResult> DetectIntentAsync(
        string sessionId, string text, string? languageCode = null, CancellationToken ct = default);

    Task<bool> TrainAgentAsync(CancellationToken ct = default);
    Task<bool> CreateIntentAsync(SuggestedIntent intent, CancellationToken ct = default);
    Task<bool> AddTrainingPhrasesAsync(string intentName, IEnumerable<string> phrases, CancellationToken ct = default);
    Task<bool> TestConnectivityAsync(CancellationToken ct = default);
    Task<DialogflowHealthStatus> GetHealthStatusAsync(CancellationToken ct = default);
}
```

#### Resultado esperado del NLU — `DialogflowDetectionResult`:

```csharp
// El LLM debe retornar una estructura compatible con esto:
public class DialogflowDetectionResult
{
    public string DetectedIntent { get; set; }           // "VehicleInquiry", "TestDriveRequest", etc.
    public float ConfidenceScore { get; set; }            // 0.0 - 1.0
    public string? FulfillmentText { get; set; }          // Respuesta al usuario en texto
    public bool IsFallback { get; set; }                  // true si no detectó intent claro
    public Dictionary<string, string>? Parameters { get; set; } // Entidades extraídas (marca, modelo, precio, etc.)
}
```

#### Resiliencia actual de `DialogflowService` (Polly):

```csharp
// Ya implementado — el LLM debe tener las mismas protecciones:
_retryPolicy = Policy.Handle<Exception>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

_circuitBreakerPolicy = Policy.Handle<Exception>()
    .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));
```

#### 17 categorías de intents definidas en `IntentCategory` enum:

```
VehicleInquiry, PriceQuestion, TestDriveRequest, FinancingInfo, TradeIn,
Warranty, Comparison, Availability, DealerInfo, ServiceAppointment,
Insurance, Documentation, Negotiation, Complaint, GeneralQuestion,
Greeting, Farewell
```

#### Endpoints API existentes del ChatbotService:

| Endpoint                                         | Método              | Auth         | Descripción                                  |
| ------------------------------------------------ | ------------------- | ------------ | -------------------------------------------- |
| `POST /api/chat/start`                           | Iniciar sesión      | Anónimo      | Crea sesión, retorna token + welcome         |
| `POST /api/chat/message`                         | Enviar mensaje      | Anónimo      | Pipeline: Quick → Dialogflow/LLM → respuesta |
| `POST /api/chat/end`                             | Finalizar sesión    | Anónimo      | Marca como completada, calcula duración      |
| `POST /api/chat/transfer`                        | Transferir a agente | Anónimo      | Crea lead, transfiere sesión                 |
| `GET /api/chat/session?token=`                   | Obtener sesión      | Anónimo      | Detalles por token                           |
| `GET /api/chat/session/{token}/messages`         | Historial           | Anónimo      | Todos los mensajes de la sesión              |
| `GET /api/chat/sessions/active/count`            | Sesiones activas    | Anónimo      | Conteo de sesiones activas                   |
| `GET /api/chat/health`                           | Health check        | Anónimo      | Estado del servicio                          |
| `GET /api/configuration/{id}`                    | Config              | Admin/Dealer | Configuración del chatbot                    |
| `GET /api/configuration/{id}/vehicles`           | Vehículos           | Admin/Dealer | Inventario sincronizado                      |
| `GET /api/configuration/{id}/vehicles/search?q=` | Buscar              | Admin/Dealer | Búsqueda en inventario                       |

#### Canales soportados (enum `ChannelType`):

WebChat, WhatsApp, Facebook, Instagram, Telegram, SMS, VoiceCall

#### Servicios centralizados que el chatbot utiliza:

| Servicio              | Uso                                      | Endpoint interno (K8s)                                                         |
| --------------------- | ---------------------------------------- | ------------------------------------------------------------------------------ |
| `VehiclesSaleService` | Sincronizar inventario cada 4h           | `GET http://vehiclessaleservice:8080/api/vehicles?pageSize=1000&status=Active` |
| `NotificationService` | Enviar WhatsApp/Email al transferir lead | `POST http://notificationservice:8080/api/notifications/whatsapp`              |
| `AuditService`        | Registrar acciones críticas              | `POST http://auditservice:8080/api/audit/logs`                                 |
| `IdempotencyService`  | Evitar operaciones duplicadas            | `POST http://idempotencyservice:8080/api/idempotency/check`                    |

#### Tareas programadas (Background Worker con CRON):

| Tarea          | Cron           | Frecuencia   | Qué hace                                       |
| -------------- | -------------- | ------------ | ---------------------------------------------- |
| InventorySync  | `0 */4 * * *`  | Cada 4 horas | Sincroniza vehículos desde VehiclesSaleService |
| DailyReport    | `0 6 * * *`    | Diario 6am   | Genera reporte de métricas                     |
| HealthCheck    | `*/15 * * * *` | Cada 15 min  | Verifica Dialogflow, DB, Redis                 |
| AutoLearning   | `0 2 * * 0`    | Domingos 2am | Analiza fallbacks, sugiere intents             |
| SessionCleanup | `0 3 * * *`    | Diario 3am   | Limpia sesiones expiradas                      |

#### `appsettings.json` del ChatbotService:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=chatbotservice;Username=${DB_USER};Password=${DB_PASSWORD}"
  },
  "Jwt": {
    "Key": "${JWT_SECRET_KEY}",
    "Issuer": "okla.com.do",
    "Audience": "okla.com.do"
  },
  "Dialogflow": {
    "ProjectId": "${DIALOGFLOW_PROJECT_ID}",
    "CredentialsPath": "/app/credentials/dialogflow.json",
    "LanguageCode": "es"
  },
  "ChatbotLimits": {
    "MaxInteractionsPerSession": 10,
    "MaxInteractionsPerUserPerDay": 50,
    "MaxGlobalInteractionsPerMonth": 100000,
    "FreeInteractionsPerMonth": 180,
    "CostPerInteraction": 0.002
  },
  "Redis": { "ConnectionString": "redis:6379", "InstanceName": "chatbot:" },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": 5672,
    "Exchange": "chatbot.events"
  },
  "ServiceUrls": {
    "VehiclesSaleService": "http://vehiclessaleservice:8080",
    "NotificationService": "http://notificationservice:8080"
  }
}
```

#### Estado de despliegue actual:

| Área                   | Estado           | Detalle                                                   |
| ---------------------- | ---------------- | --------------------------------------------------------- |
| **Gateway routes**     | ❌ No registrado | No hay rutas para chatbot en ocelot configs               |
| **CI/CD pipeline**     | ❌ No incluido   | No está en `smart-cicd.yml` ni `deploy-digitalocean.yml`  |
| **K8s manifests**      | ❌ No tiene      | No hay deployment/service en `k8s/`                       |
| **docker-compose.yml** | ❌ No definido   | Solo existe en `docker-compose.qa.yml`                    |
| **Código backend**     | ✅ Completo      | Compilable, Clean Architecture, 10 entidades, 5 servicios |
| **Frontend UI**        | ❌ No existe     | No hay componentes de chat en Next.js actual              |

---

### 1.4 — Datos del inventario de vehículos (formato JSON disponible)

```json
{
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "brand": "Toyota",
  "model": "RAV4",
  "year": 2024,
  "price": 2850000,
  "currency": "DOP",
  "priceUSD": 48305,
  "type": "SUV",
  "condition": "Nuevo",
  "mileage": 0,
  "transmission": "Automática",
  "fuelType": "Gasolina",
  "color": "Blanco Perla",
  "engineSize": "2.5L",
  "doors": 5,
  "features": [
    "Cámara de reversa",
    "Apple CarPlay",
    "Android Auto",
    "Sensores de estacionamiento"
  ],
  "location": "Santo Domingo, DN",
  "dealerName": "Auto Toyota Dominicana",
  "dealerPhone": "+1-809-555-0100",
  "isAvailable": true,
  "slug": "toyota-rav4-2024-blanco-santo-domingo",
  "images": ["url1", "url2", "url3"]
}
```

---

## PARTE 2: FUNCIONALIDADES DEL CHATBOT

1. **Agendar citas** — Tres tipos:
   - **Prueba de manejo** (test drive) — Requiere: nombre, teléfono, vehículo de interés, fecha/hora preferida, licencia de conducir vigente
   - **Taller mecánico** (service) — Requiere: nombre, teléfono, vehículo del cliente (marca/modelo/año), descripción del problema, fecha preferida
   - **Visita para compra** — Requiere: nombre, teléfono, vehículo(s) de interés, fecha/hora preferida

2. **Consultar inventario** — Buscar vehículos por marca, modelo, año, rango de precio, tipo (SUV, Sedan, Pickup, etc.), transmisión, combustible, condición (nuevo/usado)

3. **Calificar leads automáticamente** — Asignar temperatura y score:
   - **HOT (85-100)**: Menciona presupuesto específico, pide test drive, pregunta por financiamiento, compara modelos específicos
   - **WARM (50-84)**: Preguntas detalladas sobre un vehículo, pregunta por disponibilidad, menciona que está buscando
   - **COLD (0-49)**: Solo navega, preguntas generales, no da datos de contacto

4. **Transferir a agente humano** — Cuando lead es HOT (≥85), solicitud explícita, quejas, o consulta no resuelta

5. **Responder FAQ** — Horarios, ubicación, financiamiento, documentación, garantía, trade-in

6. **Recomendar vehículos** — Basado en presupuesto, necesidades (familia, trabajo, ciudad), preferencias

---

## PARTE 3: CUMPLIMIENTO LEGAL (República Dominicana)

El chatbot DEBE cumplir estrictamente con estas leyes. Cada respuesta debe validarse:

### Ley 358-05 — Protección al Consumidor (Pro-Consumidor)

- **Art. 33-35**: Transparencia total en precios — siempre en DOP, incluir ITBIS si aplica
- **Art. 40**: No prometer garantías que no existan formalmente por escrito
- **Art. 44**: Informar derecho a retractación (7 días hábiles fuera de establecimiento)
- **Art. 83-84**: Afirmaciones del chatbot son vinculantes — NO prometer precios, descuentos o condiciones sin confirmación
- **Implicación práctica**: Toda información de precio debe incluir: _"Precio de referencia sujeto a confirmación. Consulte con nuestro equipo de ventas para una cotización oficial."_

### Ley 172-13 — Protección de Datos Personales

- **Art. 5**: Consentimiento EXPLÍCITO antes de recopilar datos personales
- **Art. 10-11**: Informar POR QUÉ y CÓMO se usarán los datos
- **Art. 27**: Derecho al olvido — eliminación de datos a solicitud
- **Art. 29**: Prohibido compartir con terceros sin autorización expresa
- **Implicación práctica**: Antes de pedir datos: _"Para agendar tu cita necesito algunos datos. Tu información será usada únicamente para coordinar la visita y está protegida según la Ley 172-13. ¿Deseas continuar?"_

### Código Civil Dominicano

- **Art. 1101-1108**: Afirmaciones del bot = oferta contractual potencialmente vinculante
- **NUNCA prometer**: precio fijo, disponibilidad garantizada, condiciones de financiamiento específicas, plazos de entrega

### Normas DGII (Dirección General de Impuestos Internos)

- Compromisos de precio pueden crear obligación fiscal
- SIEMPRE aclarar: precios NO incluyen traspaso, ITBIS adicional, impuestos de primera placa
- NUNCA cotizar "todo incluido" sin validación humana

---

## PARTE 4: REQUERIMIENTOS ESPECÍFICOS

### 4.1 — Pipeline de auditoría de respuestas (Pre-envío)

El modelo debe auditar cada respuesta ANTES de enviarla al cliente:

- **Opción A**: El mismo modelo con un segundo prompt de auditoría (chain-of-thought)
- **Opción B**: Un modelo más pequeño (Llama 3 8B) como auditor

Verificaciones obligatorias:

- ✅ Cumplimiento con las 4 leyes dominicanas
- ✅ Datos del vehículo coinciden con inventario real (no inventar specs)
- ✅ No hay compromisos vinculantes no autorizados
- ✅ No se exponen datos sensibles (cédulas, tarjetas, direcciones completas)
- ✅ Tono profesional y apropiado
- ✅ No hay info médica, legal o financiera que requiera profesional certificado

Si falla → reformular automáticamente antes de enviar.

### 4.2 — Seguridad de datos sensibles del cliente

- **Detectar PII**: Cédulas (XXX-XXXXXXX-X), tarjetas (16 dígitos), direcciones, teléfonos (+1-809-XXX-XXXX)
- **NUNCA** repetir datos sensibles completos
- **Enmascarar** en logs: `***-*******-3`, `****-****-****-1234`
- **Solo pedir** datos mínimos necesarios
- Si comparten datos no solicitados (tarjeta): _"Por tu seguridad, no proceses datos de pago por este canal."_

### 4.3 — Multi-tenant — Parametrización por dealer

Variables dinámicas (pobladas desde `ChatbotConfiguration`):

| Variable                  | Campo en ChatbotConfiguration | Ejemplo                         |
| ------------------------- | ----------------------------- | ------------------------------- |
| `{{dealer_name}}`         | `Name` + relación Dealer      | "Auto Toyota Dominicana"        |
| `{{dealer_phone}}`        | Relación DealerService        | "+1-809-555-0100"               |
| `{{dealer_address}}`      | Relación DealerService        | "Av. 27 de Febrero #100, SD"    |
| `{{dealer_hours}}`        | `BusinessHoursJson`           | "Lun-Vie 8AM-6PM, Sáb 9AM-1PM"  |
| `{{dealer_tone}}`         | Campo nuevo a agregar         | "formal" / "casual" / "premium" |
| `{{bot_name}}`            | `BotName`                     | "Ana" / "Asistente OKLA"        |
| `{{welcome_message}}`     | `WelcomeMessage`              | "¡Bienvenido a Auto Toyota!"    |
| `{{financing_available}}` | Campo nuevo                   | true/false                      |
| `{{trade_in_available}}`  | Campo nuevo                   | true/false                      |
| `{{service_available}}`   | Campo nuevo                   | true/false                      |
| `{{max_interactions}}`    | `MaxInteractionsPerSession`   | 10                              |
| `{{timezone}}`            | `TimeZone`                    | "America/Santo_Domingo"         |

---

## PARTE 5: ENTREGABLES — LAS 5 FASES COMPLETAS

---

### 📐 FASE 1 — DISEÑO DE PROMPTS (Sistema Modular)

Diseña los siguientes prompts:

#### Prompt 1 — System Prompt Base

Personalidad del chatbot OKLA, contexto de RD, límites legales, parametrizable por dealer. Se envía como `system message` en cada conversación.

#### Prompt 2 — Consulta de Inventario

Cómo interpretar búsquedas naturales del usuario (_"busco algo para familia que no gaste mucha gasolina por debajo de 2 millones"_) y presentar resultados con disclaimers legales. El inventario se pasa como contexto JSON al prompt.

#### Prompt 3 — Agendamiento de Citas

Protocolo paso a paso:

1. Identificar tipo de cita (test drive / taller / compra)
2. Confirmar vehículo de interés
3. Proponer fechas/horarios según `{{dealer_hours}}`
4. Recopilar datos (con consentimiento Ley 172-13)
5. Confirmar con resumen y disclaimers
6. **Generar JSON estructurado** que el backend pueda procesar directamente:

```json
{
  "action": "SCHEDULE_APPOINTMENT",
  "type": "test_drive",
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "Juan Pérez",
  "customerPhone": "+18095551234",
  "preferredDate": "2026-02-20",
  "preferredTime": "10:00",
  "notes": "Interesado en financiamiento",
  "consentGiven": true
}
```

#### Prompt 4 — Auditoría Legal (Pre-envío)

Chain-of-thought para clasificar respuestas. Debe retornar JSON:

```json
{
  "verdict": "APPROVED | NEEDS_REVISION | BLOCKED",
  "original_response": "...",
  "revised_response": null,
  "flags": ["price_commitment", "missing_disclaimer"],
  "legal_references": ["Ley 358-05 Art. 83"]
}
```

#### Prompt 5 — Calificación de Leads

Criterios para score y temperatura. Retorna JSON compatible con entidad `ChatLead`:

```json
{
  "score": 87,
  "temperature": "Hot",
  "signals": ["mentioned_budget", "requested_test_drive", "asked_financing"],
  "recommended_action": "transfer_to_agent",
  "summary": "Cliente con presupuesto de 2.5M DOP, interesado en RAV4 2024, quiere financiamiento"
}
```

#### Prompt 6 — Transferencia a Humano

Cuándo escalar y resumen para el agente:

```json
{
  "action": "TRANSFER_TO_AGENT",
  "reason": "hot_lead | customer_request | complaint | unresolved",
  "customer": { "name": "...", "phone": "...", "email": "..." },
  "context": {
    "vehicleOfInterest": "Toyota RAV4 2024",
    "budget": "2,500,000 DOP",
    "urgency": "high",
    "sentiment": "positive",
    "conversationSummary": "...",
    "messagesCount": 8,
    "leadScore": 87
  }
}
```

#### Prompt 7 — Mejora Continua (Análisis de Conversaciones)

Para analizar conversaciones completadas y generar pares de fine-tuning.

#### Prompt 8 — Cualquier otro que consideres necesario

(manejo de objeciones, reactivación de leads, comparación de vehículos, etc.)

**Formato requerido por cada prompt:**

1. **Nombre y rol** en el pipeline
2. **Trigger** — cuándo se ejecuta y qué lo activa
3. **Variables dinámicas** requeridas
4. **Texto completo del prompt** con `{{variable}}`
5. **Ejemplo real de input/output**
6. **Notas de implementación** (cómo conectarlo al código .NET existente)

---

### 📊 FASE 2 — GENERACIÓN DE DATASET PARA FINE-TUNING

#### 2.1 — Estrategia de dataset sintético inicial

- Cómo usar los prompts de la Fase 1 para generar conversaciones sintéticas
- Cuántas conversaciones necesito como mínimo para un fine-tuning efectivo
- Distribución por categoría de intent (¿cuántas VehicleInquiry vs TestDriveRequest vs Complaint?)
- Cómo garantizar diversidad (marcas, precios, modismos dominicanos)

#### 2.2 — Formato del dataset

```jsonl
{
  "messages": [
    {
      "role": "system",
      "content": "..."
    },
    {
      "role": "user",
      "content": "..."
    },
    {
      "role": "assistant",
      "content": "..."
    }
  ]
}
```

- ¿Un turno o multi-turno por línea?
- ¿Incluir el system prompt completo en cada ejemplo?
- ¿Cómo representar las acciones estructuradas (JSON de agendamiento, lead scoring)?

#### 2.3 — Script de generación

- Script Python que use API de un modelo (Claude/GPT-4) para generar conversaciones sintéticas
- Parametrizado por: número de ejemplos, distribución de intents, inventario
- Genera archivo JSONL listo para fine-tuning

#### 2.4 — Validación del dataset

- Cómo verificar calidad antes de entrenar
- Métricas de diversidad y cobertura
- Checklist de validación legal

---

### 🔧 FASE 3 — ENTRENAMIENTO (Fine-tuning en Google Colab)

Necesito un **notebook de Colab paso a paso** con:

#### 3.1 — Setup del entorno

```
- GPU: A100 (40GB) o T4 (16GB) — qué cambia
- Paquetes: transformers, peft, bitsandbytes, trl, datasets, accelerate
- Modelo base: meta-llama/Meta-Llama-3-70B-Instruct (o alternativa que quepa)
- Si 70B no cabe en Colab, recomendar alternativa (Llama 3 8B, Mixtral 8x7B)
```

#### 3.2 — Carga y preparación del dataset

- Cómo cargar JSONL de la Fase 2
- Tokenización con tokenizer de Llama 3
- Train/validation split

#### 3.3 — Configuración de QLoRA

```python
# ¿Qué valores usar específicamente para chatbot de ventas automotrices?
lora_config = LoraConfig(
    r = ?,                    # rank — ¿qué valor para este caso de uso?
    lora_alpha = ?,           # scaling
    target_modules = ?,       # qué capas adaptar
    lora_dropout = ?,
    bias = ?,
    task_type = ?
)
```

#### 3.4 — Training loop

- SFTTrainer configuration completa
- Hiperparámetros recomendados para chatbot de ventas
- Épocas, learning rate, warmup steps
- Cómo monitorear loss y evitar overfitting

#### 3.5 — Evaluación post-training

- Evaluar fine-tuneado vs base
- Métricas específicas para chatbot de ventas (no solo perplexity)
- Test suite con conversaciones gold standard

#### 3.6 — Exportación del modelo

- Guardar LoRA adapters
- Mergear adapters con modelo base
- Formatos: GGUF, safetensors
- Subir a Hugging Face Hub (repo privado)

---

### 🚀 FASE 4 — DESPLIEGUE EN PRODUCCIÓN

**Esta es la fase más crítica.** El modelo debe integrarse con el `ChatbotService` existente en .NET 8 corriendo en Digital Ocean Kubernetes.

#### 4.1 — Infraestructura de inferencia

Compara estas opciones y recomienda la mejor para OKLA:

| Opción | Proveedor         | Descripción                    | Costo estimado/1K conversaciones |
| ------ | ----------------- | ------------------------------ | -------------------------------- |
| **A**  | RunPod Serverless | GPU dedicada, baja latencia    | $?                               |
| **B**  | Together AI       | API managed, pago por token    | $?                               |
| **C**  | Replicate         | Serverless, auto-scaling       | $?                               |
| **D**  | vLLM en DOKS      | Control total en nuestro K8s   | $?                               |
| **E**  | Ollama en VPS     | Simple, económico, baja escala | $?                               |

Para cada opción incluir:

- Cómo desplegar el modelo fine-tuneado
- Endpoint API resultante
- Latencia esperada (primera respuesta y streaming)
- Costo estimado por 1,000 conversaciones (8 mensajes promedio)
- Pros y contras para nuestro caso

#### 4.2 — Integración con el ChatbotService (.NET 8)

El LLM debe acoplarse al pipeline existente. Necesito que diseñes:

**a) Nueva interfaz `ILlmService`** — ¿Reemplaza o coexiste con `IDialogflowService`?:

```csharp
public interface ILlmService
{
    // Método principal — debe retornar algo compatible con DialogflowDetectionResult
    Task<LlmResponse> GenerateResponseAsync(LlmRequest request, CancellationToken ct = default);

    // Lead scoring basado en conversación completa
    Task<LeadScore> ScoreLeadAsync(ConversationContext context, CancellationToken ct = default);

    // Auditoría pre-envío
    Task<AuditResult> AuditResponseAsync(string proposedResponse, ConversationContext context, CancellationToken ct = default);

    // Health check
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
```

Diseña los DTOs completos: `LlmRequest`, `LlmResponse`, `LlmRequest`, `ConversationContext`, `LeadScore`, `AuditResult`.

**b) Implementación `LlmService`** — Clase que llame a la API del proveedor elegido:

```csharp
public class LlmService : ILlmService
{
    // HttpClient con Polly (retry + circuit breaker)
    // Serialización del historial de conversación
    // Inyección del system prompt con variables del dealer
    // Inyección de inventario relevante (top N vehículos)
    // Parseo de JSON estructurado de la respuesta
    // Fallback a Dialogflow si falla
}
```

**c) Modificación del `SendMessageCommandHandler`** — Nuevo pipeline:

```
Quick Response → LLM → Auditoría → Respuesta
                  │ FALLA/TIMEOUT
                  ▼
           Dialogflow (fallback)
```

¿Cómo cambiar el handler existente para incorporar el LLM sin romper lo que funciona?

**d) Manejo de contexto/historial:**

- ¿Cuántos mensajes anteriores enviar como contexto?
- ¿Cómo formatear historial para Llama 3 chat template?
- ¿Cómo inyectar inventario relevante (¿RAG o context stuffing?)?
- ¿Context window de Llama 3 (8K tokens) alcanza?

**e) Configuración `appsettings.json` — sección nueva:**

```json
{
  "LlmService": {
    "Provider": "together_ai | runpod | replicate | ollama",
    "ApiUrl": "https://api.together.ai/v1/chat/completions",
    "ApiKey": "${LLM_API_KEY}",
    "ModelId": "okla-chatbot-v1",
    "MaxTokens": 512,
    "Temperature": 0.7,
    "TimeoutSeconds": 10,
    "MaxRetries": 3,
    "EnableAudit": true,
    "AuditModelId": "meta-llama/Llama-3-8B-Instruct",
    "FallbackToDialogflow": true,
    "MaxHistoryMessages": 10,
    "MaxInventoryResults": 5,
    "StreamingEnabled": true
  }
}
```

**f) Streaming de respuestas (SSE):**

- ¿Cómo implementar Server-Sent Events en .NET 8?
- ¿Cómo modificar el endpoint `/api/chat/message` para soportar streaming?
- ¿Se audita antes del stream (bloqueante) o después (riesgo)?
- ¿Cambios necesarios en el frontend?

**g) DI Registration** — agregar a `DependencyInjection.cs`:

```csharp
services.Configure<LlmSettings>(configuration.GetSection("LlmService"));
services.AddScoped<ILlmService, LlmService>();
services.AddHttpClient("LlmApi", client => { ... })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);
```

#### 4.3 — Kubernetes — Despliegue del ChatbotService

El ChatbotService actualmente NO está desplegado. Necesito los manifests exactos:

**a) Si el LLM corre como API externa (recomendado para inicio):**

```yaml
# k8s/chatbotservice-deployment.yaml — ¿cómo debe verse?
# k8s/chatbotservice-service.yaml
# k8s/chatbotservice-configmap.yaml
# k8s/chatbotservice-secret.yaml (API key del LLM)
```

**b) Si el LLM corre auto-hosted en DOKS:**

```yaml
# k8s/llm-inference-deployment.yaml (vLLM/Ollama)
# GPU node pool en Digital Ocean
# Resource limits y requests
# HPA configuration
```

**c) Health checks y circuit breaker:**

- Readiness probe que valide conexión al LLM
- Liveness probe del servicio
- Prometheus métricas (latencia, tokens, costo, fallbacks)

#### 4.4 — Gateway — Agregar rutas del ChatbotService

Proporciona los snippets JSON exactos para agregar a:

**`ocelot.Development.json`** (desarrollo local):

```json
{
  "UpstreamPathTemplate": "/api/chatbot/{everything}",
  "DownstreamPathTemplate": "/api/chat/{everything}",
  "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 80 }]
}
```

**`ocelot.prod.json`** (producción K8s):

```json
{
  "UpstreamPathTemplate": "/api/chatbot/{everything}",
  "DownstreamPathTemplate": "/api/chat/{everything}",
  "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }]
}
```

⚠️ Nota: Los controllers usan `/api/chat/...` pero el Gateway debe exponer como `/api/chatbot/...`

#### 4.5 — CI/CD — Agregar ChatbotService al pipeline

Snippets exactos para agregar a:

- `.github/workflows/smart-cicd.yml` — Path filter + build job
- `.github/workflows/deploy-digitalocean.yml` — Deployment step
- `docker-compose.yml` — Servicio para desarrollo local

#### 4.6 — Docker Compose — Servicio para desarrollo local

```yaml
chatbotservice:
  build:
    context: ./backend
    dockerfile: ChatbotService/Dockerfile
  ports:
    - "5060:8080"
  environment:
    - ConnectionStrings__DefaultConnection=...
    - LlmService__Provider=ollama
    - LlmService__ApiUrl=http://ollama:11434
  depends_on:
    - postgres
    - redis
    - rabbitmq
    - ollama

ollama: # Para desarrollo local
  image: ollama/ollama
  ports:
    - "11434:11434"
  volumes:
    - ollama-data:/root/.ollama
```

#### 4.7 — Testing en producción

- **A/B testing**: Dialogflow vs LLM (% de tráfico configurable por dealer)
- **Feature flag**: Activar/desactivar LLM por dealer via `ChatbotConfiguration.UseLlm`
- **Canary deployment**: Rollout gradual (5% → 25% → 50% → 100%)
- **Métricas a monitorear**:
  - Latencia P50, P95, P99 de respuesta
  - Tasa de fallback a Dialogflow
  - Tasa de transferencia a agente humano
  - Leads generados (cantidad y calidad)
  - Citas agendadas exitosamente
  - Satisfacción del usuario (si hay encuesta)
  - Costo por conversación vs Dialogflow
  - Tasa de hallucination (respuestas con datos incorrectos)

---

### 🔄 FASE 5 — MEJORA CONTINUA (Post-producción)

#### 5.1 — Pipeline de recolección de datos

```
Conversaciones en producción (PostgreSQL)
        │
        ▼
  Filtrar por criterios de calidad:
  ✅ Cita agendada exitosamente
  ✅ Lead HOT generado
  ✅ Fallback resuelto posteriormente por humano
  ❌ Abandonada sin resolución
  ❌ Datos sensibles sin anonimizar
        │
        ▼
  Anonimizar PII automáticamente
  (regex: cédulas, teléfonos, nombres → sintéticos)
        │
        ▼
  Almacenar en tabla `training_candidates`
        │
        ▼
  Curación humana (admin aprueba/edita/rechaza)
        │
        ▼
  Exportar a JSONL → agregar a dataset
        │
        ▼
  Re-entrenar con QLoRA (incremental)
        │
        ▼
  Evaluar vs gold standard + modelo anterior
        │
        ▼
  Si supera baseline → deploy como nueva versión
  Si no → descartar, investigar
```

- Script Python para exportar conversaciones de PostgreSQL
- Lógica de selección automática
- Pipeline de anonimización (regex para cédulas, teléfonos, nombres RD)
- Interfaz simple de curación (puede ser página admin en Next.js)

#### 5.2 — Re-entrenamiento periódico

- ¿Cuándo re-entrenar? (cada X conversaciones nuevas, o mensual)
- ¿Incremental (LoRA merge + nuevo LoRA) o desde modelo base?
- Cómo evitar catastrophic forgetting
- Versionado: okla-chatbot-v1, v2, v3...
- Almacenamiento de adapters en Hugging Face Hub (privado)

#### 5.3 — Evaluación y rollback

- Benchmark automático contra gold standard (50-100 conversaciones)
- Criterios de aprobación:
  - Precisión de intent detection ≥ actual
  - Cumplimiento legal 100%
  - Latencia ≤ 3s
  - No hallucinations vs inventario
- Proceso de rollback si nuevo modelo es peor
- Métricas de negocio vs métricas técnicas

#### 5.4 — Evolución del AutoLearningService

El `AutoLearningService` actual agrupa fallbacks por similitud de palabras. Adaptarlo para:

- Usar embeddings del LLM en vez de word overlap
- Generar pares de fine-tuning automáticamente a partir de fallbacks resueltos
- Conectar con el pipeline de re-entrenamiento (Fase 5.1)
- Sugerir nuevas Quick Responses basadas en patrones frecuentes

---

## PARTE 6: CONSIDERACIONES TÉCNICAS FINALES

### Entrenamiento

- Plataforma: **Google Colab Pro** (GPU A100 40GB)
- Técnica: **QLoRA** (4-bit quantization + LoRA adapters)
- Modelo base: Llama 3 70B-Instruct quantizado, o Llama 3 8B si no cabe
- Framework: Transformers + PEFT + bitsandbytes + TRL
- Dataset inicial: Conversaciones sintéticas + curadas

### Inferencia en producción

- Via API externa (RunPod / Together AI / Replicate) o auto-hosted (Ollama/vLLM)
- Latencia: máximo **3 segundos** primera respuesta (streaming SSE)
- Fallback: Dialogflow ES si LLM falla o tarda >5s
- Circuit breaker: Polly (5 fallos → 1 min abierto)

### Coexistencia con Dialogflow ES

```
Usuario envía mensaje
        │
        ▼
  ¿Quick Response?  ──YES──▶ Respuesta inmediata ($0, sin IA)
        │ NO
        ▼
  LLM (Llama 3)  ──▶  Auditoría  ──▶  Respuesta al usuario
        │                                   │
        │ FALLA / TIMEOUT >5s              │
        ▼                                   │
  Dialogflow ES (fallback seguro)           │
        │                                   │
        ▼                                   ▼
  Respuesta al usuario ◀───────────────────┘
```

### Volumen estimado

- Fase inicial: ~500-2,000 conversaciones/día
- Crecimiento: hasta 10,000/día en 12 meses
- Promedio: 6-10 mensajes por conversación

### Idioma

- **Español dominicano** (modismos y expresiones locales)
- Debe entender: "guagua" (vehículo/bus), "yipeta" (SUV/Jeep), "carro" (auto), "motor" (motocicleta), "pasola" (scooter), "moto" (motocicleta), "pela'o" (barato), "chivo" (buena oferta), "tigueraje" (negociación agresiva), "vaina" (cosa), "tato" (ok/de acuerdo)
- Responder en español neutro-caribeño, profesional pero cercano

### Análisis de costos

Incluye comparativo:

- Costo actual con Dialogflow ES ($0.002/interacción × vol estimado)
- Costo estimado con LLM por cada opción de inferencia
- Break-even point
- ROI esperado (mejora en conversión de leads, reducción de carga a agentes humanos)

---

## PARTE 7: SOBRE MÍ (Contexto del desarrollador)

- **No soy experto en entrenamiento de modelos** — explica cada paso de forma práctica con comandos y código copiable
- **Sí tengo experiencia** con .NET 8, Kubernetes, PostgreSQL, Redis, CI/CD con GitHub Actions
- El sistema debe ser **production-ready**, no un prototipo académico
- Prioriza **seguridad legal** sobre experiencia de usuario — preferible respuesta conservadora a problema legal
- El mercado dominicano tiene particularidades: precios en DOP, financiamiento local, documentación específica (matrícula, traspaso, primera placa)
- Tengo acceso a Google Colab Pro (GPU A100)
- El cluster DOKS está operativo con 16+ servicios en producción
- Presupuesto mensual estimado para inferencia: $100-$500 USD (fase inicial)

---

## RESUMEN DE ENTREGABLES ESPERADOS

| #   | Entregable                                                  | Fase   | Formato                   |
| --- | ----------------------------------------------------------- | ------ | ------------------------- |
| 1   | 7-8 prompts modulares con formato completo                  | Fase 1 | Texto con `{{variables}}` |
| 2   | Estrategia de dataset + script Python de generación         | Fase 2 | Python + JSONL            |
| 3   | Notebook de Colab paso a paso para QLoRA                    | Fase 3 | Código Python             |
| 4   | Comparativa de proveedores de inferencia con recomendación  | Fase 4 | Tabla comparativa         |
| 5   | Código C# de `ILlmService` + `LlmService`                   | Fase 4 | .NET 8 C#                 |
| 6   | `SendMessageCommandHandler` modificado                      | Fase 4 | .NET 8 C#                 |
| 7   | `appsettings.json` sección LLM completa                     | Fase 4 | JSON                      |
| 8   | K8s manifests (deployment, service, configmap, secret)      | Fase 4 | YAML                      |
| 9   | Gateway routes (ocelot.Development.json + ocelot.prod.json) | Fase 4 | JSON                      |
| 10  | docker-compose.yml servicio chatbot + ollama                | Fase 4 | YAML                      |
| 11  | CI/CD snippets (smart-cicd.yml + deploy-digitalocean.yml)   | Fase 4 | YAML                      |
| 12  | Script de exportación + anonimización de conversaciones     | Fase 5 | Python                    |
| 13  | Pipeline de re-entrenamiento con versionado                 | Fase 5 | Proceso + código          |
| 14  | Métricas y criterios de evaluación/rollback                 | Fase 5 | Documento                 |
| 15  | Análisis de costos comparativo                              | Todas  | Tabla con $               |

---

Diseña este sistema completo end-to-end usando todo tu conocimiento sobre ingeniería de prompts, fine-tuning de LLMs, MLOps, integración con .NET/Kubernetes, cumplimiento legal dominicano y mejores prácticas para chatbots corporativos de ventas automotrices.
