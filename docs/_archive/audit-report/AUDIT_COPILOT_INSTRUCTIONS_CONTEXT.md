# 🔍 Auditoría: copilot-instructions.md — ¿Tiene Todo para Pasar las 23 Auditorías?

**Proyecto:** OKLA (CarDealer Microservices)  
**Fecha:** Febrero 18, 2026  
**Auditor:** AI Context Specialist  
**Objetivo:** Validar que `.github/copilot-instructions.md` contiene el contexto suficiente para que el código generado por Copilot pase las 23 auditorías con puntuación >90%

---

## 📋 Resumen Ejecutivo

El archivo `copilot-instructions.md` actual tiene **1,656 líneas** y cubre bien las capas de infraestructura/deployment (añadidas recientemente). Sin embargo, tiene **brechas críticas** en las capas que los auditores existentes ya evaluaron y donde las puntuaciones fueron bajas.

### Resultado de la Auditoría

```
Auditorías que el código generado PASARÍA (>90%):     ████████░░░░░░░░░  9 de 23 (39%)
Auditorías que el código FALLARÍA o sería parcial:    ████████████████░  14 de 23 (61%)
```

| Veredicto           | Cantidad | Auditorías                                |
| ------------------- | -------- | ----------------------------------------- |
| ✅ PASA (>90%)      | 9        | #4, #13, #14, #16, #17, #18, #5, #15, #22 |
| ⚠️ PARCIAL (60-90%) | 8        | #1, #2, #3, #7, #9, #10, #11, #12         |
| 🔴 FALLA (<60%)     | 6        | #6, #8, #19, #20, #21, #23                |

---

## 📊 Análisis Detallado por Auditor

### ✅ AUDITORÍAS QUE PASARÍAN (código generado cumple >90%)

#### #4 🔐 Roles & Security — ✅ 95%

**Lo que tiene:** JWT config completa, SecurityValidators (NoSqlInjection, NoXss), CSRF, sanitización, password rules.  
**Gap menor:** Falta el mapeo explícito AccountType → Role y ejemplos de `[Authorize(Roles = "Admin")]`.

#### #13 🐳 Docker & Build — ✅ 92%

**Lo que tiene:** Dockerfile patterns (multi-stage, puertos, contexto), OpenTelemetry version, frontend Docker rules.  
**Gap menor:** Falta `.dockerignore` rules y requisito de non-root user.

#### #14 🔄 CI/CD Pipeline — ✅ 95%

**Lo que tiene:** Workflows, cache strategy, image naming, registry credentials, deploy gates.  
**Sin gaps significativos.**

#### #16 🐇 Messaging (RabbitMQ) — ✅ 93%

**Lo que tiene:** Queue arguments inmutables, PRECONDITION_FAILED, topología, credentials.  
**Gap menor:** Falta patrón de serialización de mensajes y retry policies.

#### #17 🔌 DI Wiring & Startup — ✅ 94%

**Lo que tiene:** IDeadLetterQueue mismatch, startup test, regla general de DI.  
**Gap menor:** Falta reglas de lifetime (Singleton vs Scoped vs Transient).

#### #18 🔑 Secrets & Credentials — ✅ 92%

**Lo que tiene:** Token types, rotation procedures, registry refresh.  
**Sin gaps significativos.**

#### #5 🌐 Gateway — ✅ 90%

**Lo que tiene:** Puertos, ConfigMap commands, ocelot rules.  
**Gap menor:** Falta patrón de routing de Ocelot (orden de rutas, QoS, catch-all).

#### #15 ☸️ Kubernetes & Deploy — ✅ 91%

**Lo que tiene:** Probes, ports, ConfigMaps, checklist de deploy.  
**Gap menor:** Falta resource limits/requests y deployment strategy.

#### #22 🆘 Disaster Recovery — ✅ 90%

**Lo que tiene:** Info de DO Managed PostgreSQL (backups automáticos), PVCs.  
**Gap menor:** Falta RTO/RPO targets y runbook de failover.

---

### ⚠️ AUDITORÍAS CON COBERTURA PARCIAL (60-90%)

#### #1 🏗️ Model Architect — ⚠️ 40%

**Lo que falta (CRÍTICO):**

- Zero contexto sobre ChatbotService/LLM architecture
- No menciona Python inference server (FastAPI + llama-cpp-python)
- No menciona fine-tuning pipeline (QLoRA + GGUF)
- No menciona PiiDetector ni PromptInjectionDetector
- No menciona modelo LLM (Llama 3, Q4_K_M quantization)

#### #2 🔬 AI Researcher — ⚠️ 35%

**Lo que falta (CRÍTICO):**

- No menciona dataset structure (37 intents, 1,376 templates)
- No menciona training pipeline (QLoRA parameters)
- No menciona GBNF grammar para structured output
- No menciona N_CTX/MAX_TOKENS settings
- No menciona system prompt design

#### #3 🖥️ Frontend Auditor — ⚠️ 65%

**Lo que tiene:** Next.js structure, App Router, KYC components, env vars.  
**Lo que falta (CRÍTICO):**

- ❌ **Versión incorrecta:** Dice Next.js 14, pero el proyecto usa **Next.js 16.1.6 + React 19**
- ❌ No menciona **Zustand** para state management
- ❌ No menciona **TanStack Query** (react-query) para data fetching
- ❌ No menciona **react-hook-form + Zod** para formularios
- ❌ No menciona **shadcn/ui** como sistema de componentes
- ❌ No menciona **Tailwind CSS v4**
- ❌ No menciona **Vitest** (NO Jest) para testing frontend
- ❌ No menciona **Playwright** para E2E
- ❌ No menciona **MSW** para API mocking
- ❌ No menciona accessibility (WCAG) ni SEO patterns
- ❌ No menciona `next/image` para optimización de imágenes
- ❌ No menciona Turbopack (`--turbopack`) para dev server

#### #7 📋 Business Coverage — ⚠️ 55%

**Lo que tiene:** Tipos de cuentas, pasarelas de pago.  
**Lo que falta:**

- No documenta lifecycle de vehículos (draft → pending → active → sold → archived)
- No documenta flujo de onboarding de dealers
- No documenta reglas de pricing/billing
- No documenta flujo de pago completo

#### #9 🗣️ Conversational AI — ⚠️ 30%

**Lo que falta (CRÍTICO):**

- Zero contexto sobre el chatbot "Ana"
- No menciona español dominicano ni dialectal patterns
- No menciona intent taxonomy
- No menciona conversation flow patterns

#### #10 ⚙️ MLOps Engineer — ⚠️ 35%

**Lo que falta:**

- No menciona model registry, model versioning
- No menciona canary deployments para modelos
- No menciona drift detection
- No menciona A/B testing
- No menciona DVC para datasets

#### #11 🔴 AI Red Team — ⚠️ 50%

**Lo que tiene:** SecurityValidators, sanitización.  
**Lo que falta:**

- No menciona prompt injection defense patterns
- No menciona PII detection patterns
- No menciona adversarial testing framework

#### #12 🗣️ Computational Linguist — ⚠️ 25%

**Lo que falta:**

- Zero contexto lingüístico
- No menciona variantes del español dominicano
- No menciona glosario automotriz

---

### 🔴 AUDITORÍAS QUE EL CÓDIGO FALLARÍA (<60%)

#### #6 📐 Standards & Observability — 🔴 45%

**Lo que tiene:** Serilog crash fix, code conventions básicas.  
**Lo que falta (CRÍTICO — esta auditoría dio 70/100 y es la más impactante):**

- ❌ No documenta `AddStandardObservability()` — el método shared que TODOS los servicios deben usar
- ❌ No documenta la clase `ServiceMetrics` pattern (Counter, Histogram naming)
- ❌ No documenta niveles de log estructurado (cuándo usar Information vs Warning vs Error)
- ❌ No documenta el middleware pipeline canónico (12 pasos en orden)
- ❌ No documenta las **12+ extension methods** de CarDealer.Shared que son OBLIGATORIAS
- ❌ No documenta `ValidationBehavior<T>` (MediatR pipeline que auto-ejecuta FluentValidation)
- ❌ No documenta el **dual response format** (ApiResponse<T> para success + ProblemDetails para errors)
- ❌ No documenta `EventBase` contract (EventType naming, SchemaVersion, CorrelationId)
- ❌ No documenta `public partial class Program { }` requerido para integration tests
- ❌ No documenta test naming convention (Method_Scenario_Expected)
- ❌ No documenta test stack (xUnit + FluentAssertions + Moq + WebApplicationFactory)

#### #8 📝 API Documentation — 🔴 20%

**Lo que tiene:** Lista de endpoints principales.  
**Lo que falta (CRÍTICO — esta auditoría dio 9.3%):**

- ❌ No documenta formato estándar de respuestas (ApiResponse<T>)
- ❌ No documenta formato de errores (RFC 7807 ProblemDetails)
- ❌ No documenta patrones de paginación (PagedResult<T>)
- ❌ No documenta patrones de filtrado/sorting
- ❌ No documenta configuración de Swagger/OpenAPI
- ❌ No documenta versionado de API

#### #19 🗄️ Database & Migrations — 🔴 35%

**Lo que tiene:** Lista de bases de datos, connection strings.  
**Lo que falta:**

- ❌ No documenta EF Core migration commands
- ❌ No documenta DbContext naming convention (`{Service}DbContext`)
- ❌ No documenta entity configuration pattern (IEntityTypeConfiguration<T>)
- ❌ No documenta soft delete pattern (IsDeleted + QueryFilter)
- ❌ No documenta auto-migration config (EnableAutoMigration)
- ❌ No documenta `AddStandardDatabase()` shared extension
- ❌ No documenta folder convention (Configurations/ vs EntityConfigurations/)

#### #20 ⚡ Performance & Load Testing — 🔴 15%

**Lo que falta (todo):**

- ❌ No define response time targets
- ❌ No define throughput targets
- ❌ No menciona query optimization patterns
- ❌ No menciona caching patterns (Redis)
- ❌ No menciona connection pool settings
- ❌ No menciona pagination obligatoria para listados

#### #21 🛡️ Security Infrastructure — 🔴 50%

**Lo que tiene:** JWT, CORS, HTTPS.  
**Lo que falta:**

- ❌ No documenta `UseApiSecurityHeaders()` (OWASP headers)
- ❌ No documenta security headers del frontend (CSP, HSTS, X-Frame-Options)
- ❌ No documenta network policies de K8s
- ❌ No documenta container image scanning

#### #23 🔗 E2E Integration — 🔴 25%

**Lo que falta:**

- ❌ No documenta smoke test patterns
- ❌ No documenta integration test infrastructure (CustomWebApplicationFactory)
- ❌ No documenta Playwright E2E patterns
- ❌ No documenta deploy validation steps

---

## 🎯 Los 15 Gaps Más Críticos (Ordenados por Impacto)

| #   | Gap                                                                      | Auditorías Afectadas | Impacto                            |
| --- | ------------------------------------------------------------------------ | -------------------- | ---------------------------------- |
| 1   | **Versión de Next.js incorrecta** (dice 14, es 16) + React 19            | #3 Frontend          | 🔴 Genera código incompatible      |
| 2   | **Frontend stack no documentado** (Zustand, TanStack Query, Zod, Vitest) | #3, #23              | 🔴 Usa librerías incorrectas       |
| 3   | **Shared library extensions no documentadas** (12+ métodos)              | #6, #17, #19         | 🔴 DI incompleto, código duplicado |
| 4   | **Middleware pipeline order no documentado**                             | #6, #21              | 🔴 Seguridad comprometida          |
| 5   | **Dual response format** (ApiResponse + ProblemDetails)                  | #6, #8, #3           | 🔴 Frontend no maneja errores      |
| 6   | **Observability patterns** (AddStandardObservability, ServiceMetrics)    | #6                   | 🟡 OTel inline en vez de shared    |
| 7   | **Testing stack y convenciones** (xUnit, FluentAssertions, naming)       | #6, #23              | 🟡 Tests no compilan               |
| 8   | **EF Core patterns** (DbContext, migrations, soft delete)                | #19                  | 🟡 Persistencia inconsistente      |
| 9   | **API documentation standards** (Swagger, pagination)                    | #8                   | 🟡 9.3% documentation coverage     |
| 10  | **EventBase contract** (naming, versioning)                              | #6, #16              | 🟡 Eventos incompatibles           |
| 11  | **ChatbotService/LLM context**                                           | #1, #2, #9, #10      | 🟡 4 auditorías sin contexto       |
| 12  | **Performance targets**                                                  | #20                  | 🟡 Sin SLAs definidos              |
| 13  | **CORS standard config** (specific headers)                              | #4, #21              | 🟡 Headers faltantes               |
| 14  | **ValidationBehavior pipeline** (auto FluentValidation)                  | #6                   | 🟡 Validación manual innecesaria   |
| 15  | **`public partial class Program { }`** en Program.cs                     | #6, #23              | 🟡 Integration tests fallan        |

---

## 📈 Puntuación Actual vs Objetivo

| Auditoría                    | Puntuación Actual | Con Gaps Corregidos |
| ---------------------------- | :---------------: | :-----------------: |
| #1 Model Architect           |        40%        |        90%+         |
| #2 AI Researcher             |        35%        |        90%+         |
| #3 Frontend Auditor          |        65%        |        95%+         |
| #4 Roles & Security          |        95%        |        98%+         |
| #5 Gateway Auditor           |        90%        |        95%+         |
| #6 Standards & Observability |      **45%**      |      **95%+**       |
| #7 Business Coverage         |        55%        |        85%+         |
| #8 API Documentation         |      **20%**      |      **90%+**       |
| #9 Conversational AI         |        30%        |        85%+         |
| #10 MLOps Engineer           |        35%        |        85%+         |
| #11 AI Red Team              |        50%        |        85%+         |
| #12 Computational Linguist   |        25%        |        80%+         |
| #13 Docker & Build           |        92%        |        97%+         |
| #14 CI/CD Pipeline           |        95%        |        98%+         |
| #15 K8s & Deploy             |        91%        |        96%+         |
| #16 RabbitMQ                 |        93%        |        97%+         |
| #17 DI Wiring                |        94%        |        98%+         |
| #18 Secrets                  |        92%        |        96%+         |
| #19 Database                 |      **35%**      |      **92%+**       |
| #20 Performance              |      **15%**      |      **85%+**       |
| #21 Security Infra           |        50%        |        90%+         |
| #22 Disaster Recovery        |        90%        |        95%+         |
| #23 E2E Integration          |      **25%**      |      **90%+**       |

---

## ✅ Plan de Remediación

Para que el código generado pase las 23 auditorías, se deben agregar las siguientes secciones a `copilot-instructions.md`:

1. **Corregir versión de Next.js** (14 → 16) y documentar frontend stack completo
2. **Agregar sección: Shared Library Extensions** (12+ métodos obligatorios)
3. **Agregar sección: Middleware Pipeline Order** (12 pasos canónicos)
4. **Agregar sección: API Response Formats** (ApiResponse<T> + ProblemDetails)
5. **Agregar sección: Observability Patterns** (OTel, metrics, logging levels)
6. **Agregar sección: Testing Standards** (stack, naming, coverage targets)
7. **Agregar sección: EF Core Patterns** (DbContext, migrations, soft delete)
8. **Agregar sección: Performance Standards** (targets, optimization)
9. **Agregar sección: ChatbotService/LLM Context** (brief architecture)
10. **Agregar a Code Conventions:** EventBase, CORS, `partial class Program`

---

_Auditoría generada el 18 de febrero de 2026_  
_Proyecto OKLA — Validación de Contexto para Generación de Código_
