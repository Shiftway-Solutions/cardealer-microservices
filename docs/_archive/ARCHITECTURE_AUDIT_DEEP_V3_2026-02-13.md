# 🔍 Auditoría Profunda de Arquitectura — OKLA Microservicios (V3)

**Fecha:** 13 de Febrero, 2026  
**Auditor:** GitHub Copilot (Claude Opus 4.6)  
**Alcance:** Arquitectura completa de microservicios (.NET 8, Kubernetes, CI/CD)  
**Estándares de referencia:** OWASP Top 10 (2024), 12-Factor App, Microsoft Well-Architected Framework, CNCF Best Practices, ISO/IEC 25010

---

## 📊 RESUMEN EJECUTIVO

| Dimensión                             | Calificación     | Estado                                      |
| ------------------------------------- | ---------------- | ------------------------------------------- |
| **Estructura del Proyecto**           | ⭐⭐⭐⭐☆ (4/5)  | Clean Architecture consistente              |
| **Seguridad**                         | ⭐⭐⭐⭐☆ (4/5)  | Sólida, con áreas de mejora                 |
| **Resiliencia & Tolerancia a Fallos** | ⭐⭐☆☆☆ (2/5)    | Infraestructura construida pero no adoptada |
| **Observabilidad**                    | ⭐⭐⭐☆☆ (3/5)   | Fragmentada entre dos patrones              |
| **Consistencia entre Servicios**      | ⭐⭐☆☆☆ (2/5)    | Desviaciones significativas                 |
| **CI/CD**                             | ⭐⭐⭐⭐☆ (4/5)  | Inteligente pero incompleto                 |
| **Kubernetes & Infraestructura**      | ⭐⭐⭐⭐⭐ (5/5) | Excelente hardening                         |
| **Testing**                           | ⭐⭐⭐☆☆ (3/5)   | 264 test files, gaps en servicios core      |
| **Mensajería & Eventos**              | ⭐⭐☆☆☆ (2/5)    | Riesgo de pérdida de datos                  |
| **Performance**                       | ⭐⭐⭐☆☆ (3/5)   | Sin optimizaciones de queries               |

### **Score General: 32/50 (64%) — NECESITA MEJORAS**

---

## 🔴 HALLAZGOS CRÍTICOS (P0 — Resolver Inmediatamente)

---

### CRIT-01: Dead Letter Queue en Memoria — Pérdida de Datos en Producción

**Estándar violado:** 12-Factor App (Principio IX: Disposability), CNCF Cloud Native (Stateless Processes)

**Hallazgo:** Todos los servicios implementan `InMemoryDeadLetterQueue` usando `ConcurrentQueue<T>` en memoria. Los mensajes fallidos se **pierden irrecuperablemente** al reiniciar un pod.

**Servicios afectados (5 confirmados):**

| Servicio            | Archivo                                                                   |
| ------------------- | ------------------------------------------------------------------------- |
| AuthService         | `AuthService.Infrastructure/Messaging/InMemoryDeadLetterQueue.cs`         |
| ErrorService        | `ErrorService.Infrastructure/Messaging/InMemoryDeadLetterQueue.cs`        |
| MediaService        | `MediaService.Infrastructure/Messaging/InMemoryDeadLetterQueue.cs`        |
| RoleService         | `RoleService.Infrastructure/Messaging/InMemoryDeadLetterQueue.cs`         |
| NotificationService | `NotificationService.Infrastructure/Messaging/InMemoryDeadLetterQueue.cs` |

**Impacto:** En un sistema que procesa pagos (BillingService), notificaciones transaccionales, y eventos de auditoría, perder mensajes puede resultar en:

- Pagos procesados sin confirmación al usuario
- Registros de auditoría faltantes (violación de compliance)
- Eventos de dominio perdidos (datos inconsistentes entre servicios)

**Remediación:**

```
OPCIÓN A (Recomendada): Usar RabbitMQ Native DLQ
  → Configurar dead-letter-exchange y dead-letter-routing-key en las colas
  → Los mensajes rechazados van automáticamente al exchange DLQ
  → Persistencia nativa, sin código adicional

OPCIÓN B: Redis-backed DLQ
  → Usar Redis Lists (RPUSH/LPOP) como store persistente
  → Beneficio: ya tienen Redis en el cluster

OPCIÓN C: PostgreSQL DLQ table
  → Tabla dedicada para mensajes fallidos
  → Beneficio: transaccional con el resto de datos
```

---

### CRIT-02: Orden de Middleware Inconsistente y Erróneo

**Estándar violado:** ASP.NET Core Middleware Pipeline Best Practices (Microsoft Documentation)

**Hallazgo:** **Ningún servicio tiene el mismo orden de middleware.** El Global Error Handler no es siempre el primero, lo que significa que excepciones en middleware anteriores **no son capturadas**.

**Comparación de los 3 servicios principales:**

| Posición | AuthService ✅      | MediaService ❌            | NotificationService ❌ |
| -------- | ------------------- | -------------------------- | ---------------------- |
| 1        | GlobalErrorHandling | Swagger (dev)              | Swagger (dev)          |
| 2        | RequestLogging      | HttpsRedirection           | HttpsRedirection       |
| 3        | SecurityHeaders     | CORS                       | GlobalErrorHandling    |
| 4        | Swagger (dev)       | GlobalErrorHandling        | SecurityHeaders        |
| 5        | CORS                | SecurityHeaders            | AuditMiddleware        |
| 6        | HttpsRedirection    | AuditMiddleware            | CORS                   |
| 7        | RateLimiter         | ErrorHandlingMiddleware ⚠️ | Authentication         |
| 8        | Authentication      | Authentication             | Authorization          |
| 9        | Authorization       | Authorization              | —                      |

**Problemas específicos:**

1. **MediaService:** Tiene DOS middleware de error (`UseGlobalErrorHandling` + `ErrorHandlingMiddleware`) — conflicto
2. **MediaService:** ErrorHandler en posición 4, errores de CORS en posición 3 no son capturados
3. **NotificationService:** CORS después de AuditMiddleware — orden inverso al estándar
4. **Ningún servicio sigue** el orden canónico de Microsoft

**Orden canónico recomendado (Microsoft/OWASP):**

```
1.  UseGlobalErrorHandling()     ← SIEMPRE PRIMERO
2.  UseApiSecurityHeaders()      ← Headers de seguridad temprano
3.  UseRequestLogging()          ← Logging de toda request
4.  UseHttpsRedirection()        ← Redirección HTTPS (solo fuera de K8s)
5.  UseSwagger() (dev only)      ← Solo desarrollo
6.  UseCors()                    ← CORS antes de auth
7.  UseRateLimiter()             ← Rate limit antes de auth
8.  UseAuthentication()          ← Autenticación
9.  UseAuthorization()           ← Autorización
10. UseAuditMiddleware()         ← Auditoría después de auth (tiene userId)
11. MapControllers()             ← Endpoints
12. MapHealthChecks()            ← Health checks
```

---

### CRIT-03: Gateway ConfigMap Incompleto — Servicios Inaccesibles

**Estándar violado:** API Gateway Pattern (Microsoft Microservices Architecture Guide)

**Hallazgo:** El Gateway (Ocelot) solo enruta **8 de los 15+ servicios** desplegados en Kubernetes.

**Servicios con ruta en Gateway:**

- ✅ AuthService, UserService, RoleService, VehiclesSaleService
- ✅ MediaService, BillingService, NotificationService, ErrorService

**Servicios SIN ruta en Gateway (desplegados pero inaccesibles externamente):**

- ❌ **KYCService** — Verificación de identidad NO accesible
- ❌ **AuditService** — Auditoría NO accesible desde admin panel
- ❌ **IdempotencyService** — Control de duplicados NO accesible
- ❌ **AdminService** — Panel de administración NO accesible
- ❌ **ContactService** — Formularios de contacto NO accesibles
- ❌ DealerManagementService, DealerAnalyticsService, ReviewService, etc.

**Impacto:** Los frontends no pueden comunicarse con estos servicios a través del Gateway. Las funcionalidades de KYC, administración, y contacto están rotas en producción.

---

### CRIT-04: 19+ Servicios Ejecutan Migraciones Auto en Startup

**Estándar violado:** 12-Factor App (Principio V: Build, Release, Run), Kubernetes Best Practices

**Hallazgo:** 19 servicios ejecutan `context.Database.Migrate()` o `EnsureCreated()` durante el arranque. Solo AuthService tiene una flag condicional (`Database__AutoMigrate`).

**Riesgo en producción con múltiples réplicas (HPA activo):**

```
Pod A starts → Database.Migrate() ───────────┐
Pod B starts → Database.Migrate() ─────────┐ │ ← RACE CONDITION
                                            ▼ ▼
                                     Migration Lock Conflict
                                     → One pod crashes
                                     → Deployment fails / partial rollout
```

**Remediación:**

```
OPCIÓN A (Recomendada): Kubernetes Init Container / Job
  → Job dedicado que corre migraciones antes del deployment
  → Solo un proceso ejecuta migraciones

OPCIÓN B: Líder election
  → Solo el primer pod ejecuta migraciones
  → Usar distributed lock (Redis o PostgreSQL advisory lock)

OPCIÓN C: Flag condicional (como AuthService)
  → Database__AutoMigrate=false en producción
  → Pipeline CI/CD ejecuta migraciones como paso separado
```

---

### CRIT-05: Dos Patrones de Observabilidad Divergentes

**Estándar violado:** CNCF OpenTelemetry Specification — Consistency Principle

**Hallazgo:** El proyecto tiene librerías compartidas (`CarDealer.Logging`, `CarDealer.Tracing`) pero solo una minoría de servicios las usa. El resto configura OpenTelemetry y Serilog manualmente con ~50 líneas de código duplicado.

| Patrón                  | Servicios                                                                 |
| ----------------------- | ------------------------------------------------------------------------- |
| **Shared Libraries** ✅ | Gateway, AuthService, AdminService                                        |
| **Manual Inline** ❌    | MediaService, NotificationService, ErrorService, RoleService, UserService |

**Consecuencias:**

- Configuración de sampling inconsistente (10% vs 100%)
- Exportadores diferentes (algunos a Jaeger, otros a OTLP genérico)
- Health checks no excluidos del tracing en servicios manuales
- Imposible cambiar configuración de observabilidad globalmente

---

## 🟡 HALLAZGOS MEDIOS (P1 — Planificar para Próximo Sprint)

---

### MED-01: API Versioning No Implementado

**Estándar violado:** API Lifecycle Management (Microsoft REST API Guidelines, Google API Design Guide)

Existe `CarDealer.ApiVersioning` con atributos `[ApiV1]`, `[ApiV2]`, `[ApiV3]`, pero **CERO controladores la usan**. Todos los endpoints son `/api/[controller]` sin versión.

**Impacto:** Cualquier cambio breaking afecta a TODOS los clientes simultáneamente (web, mobile, integraciones).

---

### MED-02: ValidationBehavior de MediatR No Registrado Globalmente

**Estándar violado:** Fail-Fast Validation (Clean Architecture Best Practices)

Solo **4 de 15+ servicios** registran `ValidationBehavior<,>` en el pipeline de MediatR:

| Con ValidationBehavior ✅ | Sin ValidationBehavior ❌   |
| ------------------------- | --------------------------- |
| ErrorService              | **AuthService**             |
| RoleService               | **NotificationService**     |
| UserService               | **AdminService**            |
| MediaService              | **ContactService**, Gateway |

**Impacto:** Los validadores `NoSqlInjection()` y `NoXss()` que existen en 33 servicios **no se ejecutan automáticamente** en los servicios sin `ValidationBehavior`. La validación solo ocurre si el controlador la invoca manualmente.

---

### MED-03: `UseHttpsRedirection()` Dentro de Kubernetes

Múltiples servicios llaman a `UseHttpsRedirection()` pero corren detrás de un Ingress con TLS termination. El tráfico interno es HTTP en puerto 8080.

**Impacto:** Llamadas inter-servicio pueden recibir 307 redirects que causan fallos silenciosos o loops.

**Remediación:**

```csharp
if (!app.Environment.IsProduction()) // En K8s, TLS termina en Ingress
    app.UseHttpsRedirection();
```

---

### MED-04: Clave JWT Compartida (Simétrica) entre Todos los Servicios

**Estándar violado:** Defense in Depth (NIST SP 800-53)

Todos los servicios usan `MicroserviceSecretsConfiguration.GetJwtConfig()` que retorna la **misma clave HMAC-SHA256**. Si un servicio es comprometido, los tokens son válidos en todos los demás.

**Remediación recomendada:**

```
OPCIÓN A: Migrar a JWT asimétrico (RS256)
  → AuthService firma con clave privada
  → Demás servicios verifican con clave pública
  → Compromiso de un servicio NO permite crear tokens

OPCIÓN B: Per-service audience validation
  → Cada servicio valida su propio audience claim
  → Tokens para VehicleService no sirven en BillingService
```

---

### MED-05: AdminService Usa Puertos Incorrectos por Defecto

La configuración default de `AdminService.Api/appsettings.json` define URLs inter-servicio en puerto **80**:

```json
"UserServiceUrl": "http://userservice:80"
"AuthServiceUrl": "http://authservice:80"
```

Pero los servicios de Kubernetes corren en puerto **8080**. Falla en producción si ENV vars no sobrescriben.

---

### MED-06: `AsNoTracking()` Ausente en Mayoría de Read Queries

**Estándar violado:** EF Core Performance Best Practices

Solo **5 de 20+ servicios** usan `AsNoTracking()` para queries de lectura:

| Con `AsNoTracking()` ✅       | Sin `AsNoTracking()` ❌                      |
| ----------------------------- | -------------------------------------------- |
| ErrorService (3 usos)         | **AuthService**                              |
| VehiclesSaleService (5)       | **UserService**                              |
| RoleService (3)               | **MediaService**                             |
| SpyneIntegrationService (~30) | **NotificationService**                      |
|                               | **AdminService, KYCService, BillingService** |

**Impacto:** EF Core rastrea cambios de todas las entidades leídas, consumiendo memoria y CPU. Para endpoints de alto tráfico (listado de vehículos), afecta latencia y throughput significativamente.

---

### MED-07: CI/CD Deploy List Incompleta

El workflow `deploy-digitalocean.yml` solo incluye **9 servicios** pero **15+** están en K8s.

**Servicios faltantes del pipeline:** KYCService, AuditService, IdempotencyService, RoleService, ContactService, AdminService, DealerManagementService, SchedulerService.

---

### MED-08: Sin Escaneo de Vulnerabilidades en Imágenes Docker

**Estándar violado:** CNCF Supply Chain Security, SLSA Framework

El pipeline CI/CD construye y publica imágenes sin escáneres de vulnerabilidades (Trivy, Snyk, Grype).

**Remediación:**

```yaml
- name: Scan image for vulnerabilities
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: ghcr.io/${{ github.repository }}/${{ matrix.service }}:${{ github.sha }}
    severity: "CRITICAL,HIGH"
    exit-code: "1"
```

---

### MED-09: Sin Reconexión en Consumers de RabbitMQ

**Estándar violado:** Resilient Messaging (Enterprise Integration Patterns)

Los `IHostedService` consumers de RabbitMQ no implementan lógica de reconexión. Si RabbitMQ es temporalmente inaccesible durante el arranque, el consumer falla permanentemente.

---

### MED-10: Sin Deduplicación de Mensajes en Consumers

**Estándar violado:** Exactly-Once Processing (Microservices Patterns — Chris Richardson)

A pesar de tener `IdempotencyService`, los consumers de RabbitMQ no verifican si un mensaje ya fue procesado. Con at-least-once delivery, pueden crear duplicados.

---

### MED-11: Observability Stack No Desplegado en K8s

Los ConfigMaps referencian `otel-collector:4317`, `seq:5341`, `jaeger:4317` pero **no existen deployments** en los manifiestos de `k8s/`. Los datos de telemetría van a un vacío.

---

### MED-12: Sin Tests de Integración en CI/CD

**Estándar violado:** Test Pyramid (Martin Fowler)

El pipeline de PR Checks solo ejecuta tests unitarios. No hay:

- Tests de integración con `WebApplicationFactory`
- Tests de contrato (Pact) entre servicios
- Smoke tests post-deploy
- Tests E2E automatizados

---

## 🟢 HALLAZGOS MENORES (P2 — Tech Debt Backlog)

---

### LOW-01: Librerías Compartidas Construidas pero No Adoptadas

| Librería                       | Estado                                                 |
| ------------------------------ | ------------------------------------------------------ |
| `CarDealer.ResiliencePatterns` | ❌ 0 servicios la usan (todos configuran Polly inline) |
| `CarDealer.SagaOrchestration`  | ❌ 0 servicios implementan sagas                       |
| `CarDealer.FeatureToggles`     | ❌ Sin evidencia de feature flags activos              |
| `CarDealer.MultiTenancy`       | ❌ Infraestructura sin adopción                        |
| `CarDealer.ApiVersioning`      | ❌ Atributos sin uso en controladores                  |

**Impacto:** ~5 librerías de código muerto que aumentan complejidad sin valor.

---

### LOW-02: HSTS No Implementado en Ningún Servicio Activo

**Estándar violado:** OWASP Security Headers

Ningún servicio activo llama a `UseHsts()`. TLS termina en Ingress (que tiene HSTS via annotations), pero defense in depth recomienda HSTS también en la aplicación.

---

### LOW-03: Health Checks No Estandarizados

| Patrón                                          | Servicios                                      |
| ----------------------------------------------- | ---------------------------------------------- |
| `MapHealthChecks("/health")` ✅                 | AuthService, MediaService, NotificationService |
| Paths separados `/health/live`, `/health/ready` | AuthService (ambos)                            |
| Manual `app.MapGet("/health", ...)` ❌          | AdminService                                   |

**Recomendación:** Estandarizar con `CarDealer.HealthChecks`:

- `/health/live` — Liveness (proceso responde)
- `/health/ready` — Readiness (DB, RabbitMQ, Redis conectados)

---

### LOW-04: Credenciales Default de RabbitMQ en Código

Los fallbacks usan `guest`/`guest`:

```csharp
var username = configuration["RabbitMQ:Username"] ?? "guest";
```

En producción se sobrescriben por K8s Secrets, pero el código no debería tener defaults inseguros.

---

### LOW-05: Sin Contract Testing entre Servicios

Con 15+ servicios comunicándose via HTTP y RabbitMQ, no hay tests Pact que validen compatibilidad de contratos. Un cambio en `UserRegisteredEvent` en AuthService puede romper el consumer en UserService sin detección.

---

### LOW-06: Swagger Potencialmente Expuesto en Producción

Swagger condicionado a `IsDevelopment()`. Si `ASPNETCORE_ENVIRONMENT` es `Staging` o está mal configurado, Swagger queda expuesto con try-it-out.

---

### LOW-07: Sin Circuit Breaker en Gateway

Polly está registrado pero no hay políticas de circuit breaker en rutas de Ocelot. Un servicio downstream colgado degrada todo el Gateway.

---

### LOW-08: ClockSkew de JWT Inconsistente

La documentación del proyecto dice `ClockSkew = TimeSpan.Zero` pero todos los servicios usan `TimeSpan.FromMinutes(5)` (el default de .NET). Esto permite tokens "expirados" hasta por 5 minutos extra.

---

## 📋 CUMPLIMIENTO CON ESTÁNDARES INTERNACIONALES

### 12-Factor App Compliance

| Factor                 | Estado | Detalle                                            |
| ---------------------- | ------ | -------------------------------------------------- |
| I. Codebase            | ✅     | Monorepo con CI/CD                                 |
| II. Dependencies       | ✅     | NuGet packages declarados explícitamente           |
| III. Config            | ✅     | ENV vars + K8s Secrets/ConfigMaps                  |
| IV. Backing Services   | ✅     | PostgreSQL, Redis, RabbitMQ como recursos adjuntos |
| V. Build, Release, Run | ⚠️     | **Migraciones en runtime violan separación**       |
| VI. Processes          | ⚠️     | **InMemoryDLQ almacena estado en proceso**         |
| VII. Port Binding      | ✅     | Kestrel self-hosted en 8080                        |
| VIII. Concurrency      | ✅     | HPA para horizontal scaling                        |
| IX. Disposability      | ❌     | **Estado perdido en restart (DLQ)**                |
| X. Dev/Prod Parity     | ⚠️     | Docker Compose vs K8s gaps                         |
| XI. Logs               | ✅     | Serilog a stdout/Seq                               |
| XII. Admin Processes   | ⚠️     | Migraciones como proceso normal, no admin          |

**Score: 8/12 factores cumplidos completamente**

---

### OWASP Top 10 Compliance (2024)

| #   | Vulnerabilidad            | Estado | Implementación                                        |
| --- | ------------------------- | ------ | ----------------------------------------------------- |
| A01 | Broken Access Control     | ✅     | JWT + RBAC + Authorization middleware                 |
| A02 | Cryptographic Failures    | ✅     | TLS, HMAC-SHA256 JWT (mejorable a RS256)              |
| A03 | Injection                 | ✅     | SecurityValidators en 33 servicios + FluentValidation |
| A04 | Insecure Design           | ⚠️     | API versioning ausente, DLQ en memoria                |
| A05 | Security Misconfiguration | ⚠️     | Swagger potencialmente expuesto, HSTS faltante        |
| A06 | Vulnerable Components     | ❌     | **Sin escaneo de imágenes Docker**                    |
| A07 | Auth Failures             | ✅     | Rate limiting, password policies, 2FA                 |
| A08 | Data Integrity Failures   | ⚠️     | Sin verificación integridad mensajes RabbitMQ         |
| A09 | Logging & Monitoring      | ✅     | Serilog + OpenTelemetry + AuditService                |
| A10 | SSRF                      | ✅     | No hay endpoints que acepten URLs externas            |

**Score: 6/10 completamente mitigados**

---

### Microsoft Well-Architected Framework

| Pilar                      | Score     | Notas                                                                    |
| -------------------------- | --------- | ------------------------------------------------------------------------ |
| **Reliability**            | ⭐⭐⭐☆☆  | HPA, PDB, health checks. Sin circuit breakers, sagas, ni DLQ persistente |
| **Security**               | ⭐⭐⭐⭐☆ | JWT, NetworkPolicies, RBAC, Headers. Mejorar con RS256 + image scanning  |
| **Cost Optimization**      | ⭐⭐⭐⭐☆ | Resource limits, HPA, Alpine images, smart CI/CD                         |
| **Operational Excellence** | ⭐⭐⭐☆☆  | CI/CD inteligente pero observabilidad fragmentada                        |
| **Performance Efficiency** | ⭐⭐⭐☆☆  | Sin AsNoTracking, sin compiled queries, sin response caching             |

---

## 📈 MÉTRICAS DE TESTING

### Cobertura por Servicio

| Servicio                    | Test Files | Criticidad | Evaluación             |
| --------------------------- | ---------- | ---------- | ---------------------- |
| AuthService                 | 38         | 🔴 Crítico | ✅ Adecuado            |
| UserService                 | 24         | 🔴 Crítico | ✅ Adecuado            |
| RoleService                 | 21         | 🟡 Alto    | ✅ Adecuado            |
| Gateway                     | 13         | 🔴 Crítico | ✅ Aceptable           |
| IdempotencyService          | 11         | 🟡 Alto    | ✅ Aceptable           |
| ErrorService                | 8          | 🟡 Alto    | ✅ Aceptable           |
| VehiclesSaleService         | 7          | 🔴 Crítico | ⚠️ Bajo                |
| ContactService              | 7          | 🟢 Medio   | ✅ Aceptable           |
| MediaService                | 6          | 🟡 Alto    | ⚠️ Bajo                |
| NotificationService         | 6          | 🟡 Alto    | ⚠️ Bajo                |
| KYCService                  | 6          | 🟡 Alto    | ⚠️ Bajo                |
| BillingService              | 5          | 🔴 Crítico | ⚠️ **Bajo para pagos** |
| AdminService                | 3          | 🟡 Alto    | ❌ Insuficiente        |
| **DealerManagementService** | **0**      | 🟡 Alto    | ❌ **Sin tests**       |
| **DealerAnalyticsService**  | **0**      | 🟡 Alto    | ❌ **Sin tests**       |
| **ReviewService**           | **0**      | 🟢 Medio   | ❌ **Sin tests**       |

**Total: ~264 test files** | **10+ servicios con 0 tests**

---

## ✅ FORTALEZAS DESTACADAS

A pesar de los hallazgos, la arquitectura tiene **bases muy sólidas:**

1. ✅ **Clean Architecture consistente** — Domain, Application, Infrastructure, Api bien separadas
2. ✅ **Kubernetes hardening excelente** — NetworkPolicies default-deny, RBAC, non-root, readOnlyRootFilesystem, drop ALL capabilities, PDB, HPA, ResourceQuotas, LimitRange
3. ✅ **Security Validators en 33 servicios** — SQL injection y XSS con buena adopción
4. ✅ **CI/CD inteligente** — Monorepo-aware, solo buildea servicios que cambiaron (ahorro ~80% Actions minutes)
5. ✅ **Secret management robusto** — Cascada ENV → Docker Secrets → Config con fail-fast
6. ✅ **18 shared libraries bien diseñadas** — Patterns centralizados listos para adopción
7. ✅ **Docker multi-stage builds** con Alpine images y non-root user
8. ✅ **Domain Events via RabbitMQ** — Comunicación asíncrona correcta
9. ✅ **OWASP Security Headers** — CSP, X-Frame-Options, HSTS, X-Content-Type-Options
10. ✅ **CQRS + MediatR** — Separación clara de commands y queries

---

## 🎯 PLAN DE REMEDIACIÓN PRIORIZADO

### Sprint Inmediato (P0 — Esta Semana)

| #   | Acción                                                       | Esfuerzo | Impacto                            |
| --- | ------------------------------------------------------------ | -------- | ---------------------------------- |
| 1   | Reemplazar `InMemoryDeadLetterQueue` con RabbitMQ native DLQ | 2 días   | 🔴 Previene pérdida de datos       |
| 2   | Estandarizar orden de middleware en TODOS los servicios      | 1 día    | 🔴 Corrige error handling          |
| 3   | Completar rutas en Gateway ConfigMap                         | 0.5 días | 🔴 Desbloquea funcionalidades      |
| 4   | Registrar `ValidationBehavior` en todos los servicios        | 0.5 días | 🔴 Activa validadores de seguridad |

### Sprint Siguiente (P1 — Próximas 2 Semanas)

| #   | Acción                                  | Esfuerzo | Impacto                 |
| --- | --------------------------------------- | -------- | ----------------------- |
| 5   | Migrar a shared observability libraries | 3 días   | Consistencia telemetría |
| 6   | Migraciones como K8s Init Container/Job | 2 días   | Elimina race conditions |
| 7   | Condicionar `UseHttpsRedirection`       | 0.5 días | Evita redirect loops    |
| 8   | Agregar Trivy image scanning al CI/CD   | 0.5 días | Supply chain security   |
| 9   | Completar deploy service list en CI/CD  | 0.5 días | Auto-deploy completo    |
| 10  | Corregir puertos default AdminService   | 0.5 días | Evita fallos en K8s     |
| 11  | Agregar `AsNoTracking()` a read queries | 2 días   | Performance             |

### Backlog (P2 — Próximo Trimestre)

| #   | Acción                                      | Esfuerzo | Impacto                         |
| --- | ------------------------------------------- | -------- | ------------------------------- |
| 12  | Adoptar API versioning (`/api/v1/`)         | 3 días   | Breaking change management      |
| 13  | Migrar a JWT asimétrico (RS256)             | 3 días   | Defense in depth                |
| 14  | Implementar contract testing (Pact)         | 5 días   | Previene roturas inter-servicio |
| 15  | Circuit breakers en Gateway                 | 2 días   | Resilience                      |
| 16  | Adoptar shared ResilienceFactory            | 2 días   | Elimina duplicación             |
| 17  | Sagas para operaciones multi-servicio       | 5 días   | Consistencia transaccional      |
| 18  | Reconnection logic en RabbitMQ consumers    | 2 días   | Resilience                      |
| 19  | Message deduplication en consumers          | 2 días   | Exactly-once semantics          |
| 20  | Eliminar librerías compartidas no adoptadas | 1 día    | Reduce complejidad              |
| 21  | Desplegar observability stack en K8s        | 3 días   | Telemetría funcional            |
| 22  | Integration tests con WebApplicationFactory | 5 días   | Test quality                    |

---

_Auditoría completada el 13 de Febrero, 2026_  
_Servicios auditados: AuthService, MediaService, NotificationService, ErrorService, AdminService, Gateway + shared libraries, Docker, K8s, CI/CD_  
_Metodología: Revisión estática de código, configuración, y arquitectura contra estándares internacionales (OWASP, 12-Factor, CNCF, Microsoft WAF, ISO 25010)_
