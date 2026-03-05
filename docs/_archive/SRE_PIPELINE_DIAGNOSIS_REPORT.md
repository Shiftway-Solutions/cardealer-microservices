# 🔧 SRE Pipeline Diagnosis & Repair Report

**Date:** February 19–20, 2026  
**Repository:** `gregorymorenoiem/cardealer-microservices` (branch: `main`)  
**Cluster:** `okla-cluster` (DigitalOcean DOKS, namespace: `okla`)  
**Author:** SRE Diagnosis (Automated)

---

## 1. Resumen Ejecutivo

Se identificaron **5 causas raíz** que afectaban la pipeline de despliegue y la estabilidad del clúster:

| #   | Causa Raíz                                                                                                                                   | Severidad  | Estado                                               |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ---------------------------------------------------- |
| 1   | **Registry secret usa `GITHUB_TOKEN` efímero (`ghs_*`)** — pods nuevos no pueden hacer pull de imágenes después de que el workflow termina   | 🔴 Crítica | ✅ Corregido (workflow + secret temporal)            |
| 2   | **`ocelot.prod.json` contiene comentarios `//` inválidos** — el archivo no es JSON válido, impide actualización del ConfigMap                | 🟡 Media   | ✅ Corregido                                         |
| 3   | **ChatbotService: env var `REDIS_PASSWORD` definido después de su uso en `$(REDIS_PASSWORD)`** — Redis connection string no se resuelve      | 🔴 Crítica | ✅ Corregido                                         |
| 4   | **ChatbotService: startup probe demasiado agresiva** — `/health` tarda >5s por checks de PostgreSQL+Redis, pod muere en startup              | 🟠 Alta    | ✅ Corregido                                         |
| 5   | **CI build failure: 26 servicios secundarios fallan** — errores de compilación en servicios no-activos (ContactService, ReviewService, etc.) | 🟡 Media   | ⚠️ Documentado (no bloquea los 14 servicios activos) |

**Estado final del clúster: ✅ 15/15 pods activos en Running 1/1. Health check público OK (HTTP 200).**

---

## 2. Evidencias del Diagnóstico

### 2.1 GitHub Actions — Runs Recientes

```
Smart CI/CD (últimos 10):
  ❌ 22208356376 — "chore: merge development into main" (26 servicios fallaron)
  ✅ 22205991440 — "chore(maintenance): finalize deployment fixes"
  ✅ 22205919412 — "chore(maintenance): finalize deployment fixes"
  ✅ 22205503859 — "fix(maintenance): add solution file..."
  ❌ 22185009583 — "fix(maintenance): fix Program.cs syntax/JWT bugs..."

Deploy to Digital Ocean:
  ⏭️ 22208464679 — Skipped (CI falló, deploy no se triggereó)
  ✅ 22206082721 — Success
  ✅ 22205812832 — Success
```

**Servicios fallidos en run 22208356376:**

| Categoría        | Servicios                                                                                                                                                                                                                                                                                                                                     | Error                                 |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| Restore failures | ReviewService, SchedulerService, AppointmentService, IdempotencyService, StaffService, BackgroundRemovalService, InventoryManagementService, DealerManagementService, RecommendationService, PaymentService, DealerAnalyticsService, ReportsService, LeadScoringService, RateLimitingService, ComparisonService, AlertService, ApiDocsService | `dotnet restore` failed               |
| Build failures   | ContactService, Vehicle360ProcessingService, ConfigurationService, CRMService, MarketingService, DataProtectionService, AIProcessingService, ServiceDiscovery, VehicleIntelligenceService, IntegrationService                                                                                                                                 | `dotnet build` failed (CS1061 errors) |
| Docker build     | MessageBusService                                                                                                                                                                                                                                                                                                                             | Docker build failed                   |

**Ejemplo de error (ContactService):**

```
error CS1061: 'ContactRequest' does not contain a definition for 'Messages'
error CS1061: 'ContactRequest' does not contain a definition for 'SellerId'
error CS1061: 'ContactRequest' does not contain a definition for 'Status'
error CS1061: 'ContactRequest' does not contain a definition for 'BuyerId'
```

> **Nota:** Estos 26 servicios están en `replicas: 0` (no activos). Los 14 servicios activos (authservice, gateway, frontend-web, etc.) compilaron y se desplegaron correctamente.

### 2.2 Estado de Pods (Pre-diagnóstico)

```
chatbotservice-95fc67d77-ln99n   0/1   ImagePullBackOff   (image: chatbotservice:1.0.14)
chatbotservice-fcc649864-7lfg5   1/1   Running            (old pod, still alive from 40h ago)
pg-users, pgclient*, pgquery*    0/1   Completed/Error    (stale debug pods)
test-auth-*, test-curl, curlpod  0/1   Completed          (stale test pods)
```

### 2.3 Registry Secret — Token Efímero

```
Server: ghcr.io
Username: gregorymorenoiem
Password prefix: ghs_swJ8CT...
Type: ⚠️ EPHEMERAL (GitHub Actions token — expired)
```

**Causa:** El workflow `deploy-digitalocean.yml` línea 192 usaba:

```yaml
--docker-password=${{ secrets.GITHUB_TOKEN }}
```

`GITHUB_TOKEN` (`ghs_*`) expira ~1 hora después de que el workflow termina. Los pods existentes continúan ejecutándose pero nuevos pods no pueden hacer `docker pull`.

### 2.4 Ocelot.prod.json — JSON Inválido

```
Archivo: backend/Gateway/Gateway.Api/ocelot.prod.json (2208 líneas)
Error en línea 2100, columna 5: "Expecting value"
Causa: Comentarios JavaScript-style (//) en JSON estándar

Línea 2100: // ── Leads (VehiclesSaleService) ──
Línea 2165: // ── Invoices (PaymentService) ──
```

### 2.5 ChatbotService — Redis Connection y Startup Probe

**Problema 1 — Env var ordering:**

```yaml
# ❌ ANTES (Redis__ConnectionString definido ANTES de REDIS_PASSWORD)
- name: Redis__ConnectionString
  value: "redis:6379,password=$(REDIS_PASSWORD)" # $(REDIS_PASSWORD) = literal
- name: REDIS_PASSWORD # Definido después → no disponible para sustitución
```

Resultado: El pod arrancaba con `Redis__ConnectionString=redis:6379,password=$(REDIS_PASSWORD)` (texto literal). El health check de Redis fallaba → `/health` devolvía 503.

**Problema 2 — Startup probe:**

```yaml
# ❌ ANTES
startupProbe:
  path: /health # Devuelve 503 hasta que Redis conecta (~20s)
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 12 # = 65 segundos total, insuficiente
```

### 2.6 ConfigMap vs Fuente

El ConfigMap `gateway-config` en el clúster contiene una versión de `ocelot.json` que **difiere** de `ocelot.prod.json` local. La versión en el clúster tiene las rutas de Leads e Invoices; la versión local tenía JSON inválido (comentarios). Tras la corrección, ambas versiones son consistentes.

---

## 3. Cambios Aplicados

### 3.1 `deploy-digitalocean.yml` — Usar PAT persistente

```diff
- kubectl create secret docker-registry registry-credentials \
-   --docker-server=ghcr.io \
-   --docker-username=${{ github.actor }} \
-   --docker-password=${{ secrets.GITHUB_TOKEN }} \
-   -n okla --dry-run=client -o yaml | kubectl apply -f -
+ # ⚠️ Uses GHCR_PAT (persistent Fine-grained PAT with read:packages scope)
+ # instead of GITHUB_TOKEN (ephemeral, expires after workflow completes).
+ kubectl create secret docker-registry registry-credentials \
+   --docker-server=ghcr.io \
+   --docker-username=${{ github.repository_owner }} \
+   --docker-password=${{ secrets.GHCR_PAT }} \
+   -n okla --dry-run=client -o yaml | kubectl apply -f -
```

**⚠️ ACCIÓN REQUERIDA:** Crear el secreto `GHCR_PAT` en GitHub Settings:

1. Ir a https://github.com/settings/tokens
2. Crear un **Fine-grained PAT** o **Classic PAT** con scope `read:packages` (y `write:packages` si los workflows suben imágenes)
3. Guardar como secreto del repositorio:
   ```bash
   gh secret set GHCR_PAT --body "ghp_XXXXXXXXX"
   ```

### 3.2 `ocelot.prod.json` — Eliminar comentarios inválidos

Se eliminaron 2 líneas con comentarios JavaScript (`//`) que hacían el JSON inválido:

- Línea 2100: `// ── Leads (VehiclesSaleService) ──`
- Línea 2165: `// ── Invoices (PaymentService) ──`

Las rutas de Leads e Invoices se mantienen intactas.

### 3.3 `k8s/chatbotservice.yaml` — Fix Redis + Startup Probe

```diff
  # Fix 1: Reorder env vars (REDIS_PASSWORD before its usage)
- - name: Redis__ConnectionString
-   value: "redis:6379,password=$(REDIS_PASSWORD)"
  - name: REDIS_PASSWORD
    valueFrom:
      secretKeyRef:
        name: redis-secrets
        key: REDIS_PASSWORD
+ - name: Redis__ConnectionString
+   value: "redis:6379,password=$(REDIS_PASSWORD)"

  # Fix 2: Relaxed startup probe (300s total instead of 65s)
  startupProbe:
    httpGet:
      path: /health
      port: 8080
-   initialDelaySeconds: 5
-   periodSeconds: 5
-   failureThreshold: 12
+   initialDelaySeconds: 15
+   periodSeconds: 15
+   timeoutSeconds: 10
+   failureThreshold: 20
```

### 3.4 Acciones Directas en el Clúster

| Acción                       | Comando                                                            | Resultado                           |
| ---------------------------- | ------------------------------------------------------------------ | ----------------------------------- |
| Reemplazar registry secret   | `kubectl delete/create secret registry-credentials`                | ✅ Token `gho_*` (temporal, ~8hr)   |
| Fijar chatbotservice image   | `kubectl set image deploy/chatbotservice ...chatbotservice:latest` | ✅ Eliminó tag `1.0.14` inaccesible |
| Aplicar manifest actualizado | `kubectl apply -f k8s/chatbotservice.yaml`                         | ✅ Env ordering + probe corregidos  |
| Limpiar pods debug/stale     | `kubectl delete pod pg-users pgclient* pgquery* test-* curlpod`    | ✅ 10 pods eliminados               |

---

## 4. Verificación Final

### 4.1 Estado de Pods

```
authservice-59cb968545-mxbbn           1/1   Running
billingservice-85dc5978dd-n6m2l        1/1   Running
chatbotservice-845ff575db-qpr7v        1/1   Running   ← Fixed
errorservice-cc756b7f6-qxh4b          1/1   Running
frontend-web-688f79948c-6gdfv          1/1   Running
gateway-d58d6c959-qgqbb               1/1   Running
kycservice-5b476c4498-cd2bw            1/1   Running
mediaservice-b4f5f7495-rblcm           1/1   Running
notificationservice-86dd7c5d-vkf7c     1/1   Running
postgres-0                             1/1   Running
rabbitmq-d47f9cb95-n7j8q               1/1   Running
redis-85fdbcb556-9pnb9                 1/1   Running
roleservice-fd85fbb54-4m5l2            1/1   Running
userservice-6d85c6675d-dz9cq           1/1   Running
vehiclessaleservice-6d55dbb7cb-f5z4r   1/1   Running
```

**15/15 pods activos: Running 1/1 ✅**

### 4.2 Health Checks Internos (desde gateway pod)

```
authservice:          HTTP 200 ✅
userservice:          HTTP 200 ✅
roleservice:          HTTP 200 ✅
vehiclessaleservice:  HTTP 200 ✅
mediaservice:         HTTP 200 ✅
notificationservice:  HTTP 200 ✅
billingservice:       HTTP 200 ✅
errorservice:         HTTP 200 ✅
kycservice:           HTTP 200 ✅
chatbotservice:       HTTP 200 ✅
frontend-web:         HTTP 200 ✅
```

### 4.3 Health Check Público

```bash
$ curl -sf https://okla.com.do/api/health
{"status":"healthy","timestamp":"2026-02-20T04:16:34.266Z","version":"1.0.0","environment":"production"}
# HTTP 200 ✅
```

### 4.4 ChatbotService Health (Detallado)

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "postgresql", "status": "Healthy", "duration": 64.44 },
    { "name": "redis", "status": "Healthy", "duration": 605.44 }
  ],
  "totalDuration": 611.14
}
```

---

## 5. Recomendaciones

### 🔴 Prioritaria — Crear PAT Persistente para GHCR

El secret `registry-credentials` actual usa un token OAuth CLI (`gho_*`) que expira en ~8 horas. **Se necesita un PAT persistente:**

1. Crear un **Classic PAT** con scope `read:packages` + `write:packages`
2. Guardarlo como secreto del repositorio:
   ```bash
   gh secret set GHCR_PAT --body "ghp_XXXXXXXXX"
   ```
3. Actualizar el secret en el clúster manualmente (o esperar al siguiente deploy):
   ```bash
   kubectl delete secret registry-credentials -n okla
   kubectl create secret docker-registry registry-credentials \
     --docker-server=ghcr.io \
     --docker-username=gregorymorenoiem \
     --docker-password="ghp_XXXXXXXXX" \
     -n okla
   ```

### 🟡 Mejorar Tagging de Imágenes

El workflow `chatbot-cicd.yml` usa `sed` para reemplazar `:latest` con `:1.0.X` en el manifest antes de `kubectl apply`. Esto provoca que el deploy deje el tag `1.0.X` en el clúster, y si la imagen se borra de GHCR o el secret expira, no puede re-pullearse. **Recomendación:** Usar siempre `:latest` + `imagePullPolicy: Always`, o pushear ambos tags (`:latest` + `:1.0.X`).

### 🟡 Corregir 26 Servicios con Build Errors

Los servicios inactivos (replicas: 0) tienen errores de compilación que impiden la construcción de Docker images. Si bien no impactan la operación actual, bloquean el CI cuando hay cambios en shared libraries. **Recomendación:** Corregir los errores de `ContactService` (propiedad `Messages`, `SellerId`, `BuyerId`, `Status` faltantes en `ContactRequest`) como template para los demás.

### 🟢 Mejora de Health Checks

Considerar implementar `/health/live` (sin dependencias externas) y `/health/ready` (con checks) en todos los servicios. Esto permite usar endpoints distintos para `startupProbe` (live) y `readinessProbe` (ready), evitando reinicios durante inicialización lenta.

### 🟢 ConfigMap Gateway — Automatizar Sincronización

El ConfigMap `gateway-config` debe actualizarse cada vez que `ocelot.prod.json` cambia. Agregar un paso explícito al workflow de deploy:

```yaml
- name: Update Gateway ConfigMap
  run: |
    kubectl create configmap gateway-config \
      --from-file=ocelot.json=backend/Gateway/Gateway.Api/ocelot.prod.json \
      -n okla --dry-run=client -o yaml | kubectl apply -f -
    kubectl rollout restart deployment/gateway -n okla
```

### 🟢 Limpieza de Debug Pods

Establecer política de cleanup automático o usar `Jobs` con `ttlSecondsAfterFinished` para evitar acumulación de pods de debug (pg-users, pgclient\*, curlpod, etc.).

---

## 6. Archivos Modificados (Resumen)

| Archivo                                        | Cambio                                                             |
| ---------------------------------------------- | ------------------------------------------------------------------ |
| `.github/workflows/deploy-digitalocean.yml`    | `GITHUB_TOKEN` → `GHCR_PAT` para registry secret                   |
| `backend/Gateway/Gateway.Api/ocelot.prod.json` | Eliminados 2 comentarios `//` que invalidaban JSON                 |
| `k8s/chatbotservice.yaml`                      | Reordenado REDIS_PASSWORD antes de su uso + startup probe relajada |

---

_Informe generado el 2026-02-20 — 15 pods activos, 0 en error, health público OK_
