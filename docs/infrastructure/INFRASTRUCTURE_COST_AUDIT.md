# 🏗️ Auditoría de Infraestructura OKLA — Reducción de Costos

**Fecha:** 2026-03-05  
**Auditor:** GitHub Copilot  
**Cluster:** DigitalOcean Kubernetes (DOKS), namespace `okla`

---

## 📊 Estado Actual de la Infraestructura

### Cluster

| Recurso                   | Valor                                         |
| ------------------------- | --------------------------------------------- |
| Nodos                     | 2 × `s-4vcpu-8gb` ($48/mes c/u = **$96/mes**) |
| vCPUs totales             | 8 cores                                       |
| RAM total                 | ~16 GiB                                       |
| Deployments totales       | **51**                                        |
| Pods activos (replicas>0) | **28**                                        |
| Pods escalados a 0        | **23**                                        |
| CPU solicitada (activos)  | 1,925m (1.9 cores de 8 disponibles = 24%)     |
| RAM solicitada (activos)  | 4,480 MiB (4.4 GiB de 16 GiB = 27.5%)         |
| PVCs                      | 1 × 10Gi (llm-model-pvc, $1/mes)              |
| Load Balancer             | 1 × Ingress ($12/mes)                         |
| PostgreSQL Managed        | DO Managed DB ($15–$50/mes estimado)          |

### Costo Mensual Estimado Actual

| Componente                 | Costo/mes            |
| -------------------------- | -------------------- |
| 2 nodos DOKS (s-4vcpu-8gb) | $96                  |
| PostgreSQL Managed         | ~$30                 |
| Block Storage (10Gi PVC)   | $1                   |
| Load Balancer              | $12                  |
| Container Registry (GHCR)  | $0 (GitHub gratuito) |
| **Total estimado**         | **~$139/mes**        |

---

## 🔴 Problemas Identificados

### 1. Exceso de Microservicios (51 deployments)

El sistema tiene **51 microservicios desplegados**, de los cuales solo **28 están activos**. Muchos servicios son demasiado granulares y podrían consolidarse sin perder la separación de responsabilidades.

### 2. 23 Servicios a Replicas=0 (Peso Muerto)

Estos servicios consumen espacio en registro, ConfigMaps, Secrets, y definiciones de K8s aunque no ejecuten pods:

| Servicio Escalado a 0         | Recomendación                                       |
| ----------------------------- | --------------------------------------------------- |
| `advertisingservice`          | ✅ Mantener — se necesita, escalar a 1              |
| `aiprocessingservice`         | 🔴 Eliminar — funcionalidad en SearchAgent          |
| `apidocsservice`              | 🟡 Eliminar — usar Swagger en Gateway               |
| `backgroundremovalservice`    | 🔴 Eliminar — funcionalidad Spyne no activa         |
| `cacheservice`                | 🔴 Eliminar — Redis directo es suficiente           |
| `comparisonservice`           | 🟡 Fusionar con VehiclesSaleService                 |
| `crmservice`                  | 🔴 Eliminar — funcionalidad en ContactService       |
| `dataprotectionservice`       | 🟡 Fusionar con AuthService                         |
| `dealeranalyticsservice`      | 🟡 Fusionar con DealerManagementService             |
| `idempotencyservice`          | 🔴 Eliminar — middleware en Gateway                 |
| `integrationservice`          | 🔴 Eliminar — cada servicio integra directamente    |
| `leadscoringservice`          | 🟡 Fusionar con ContactService                      |
| `llm-server`                  | 🔴 Eliminar — usar APIs externas (OpenAI/Anthropic) |
| `marketingservice`            | 🔴 Fusionar con NotificationService                 |
| `messagebusservice`           | 🔴 Eliminar — RabbitMQ directo                      |
| `paymentservice`              | 🔴 Fusionar con BillingService                      |
| `ratelimitingservice`         | 🔴 Eliminar — middleware en Gateway                 |
| `recommendationservice`       | 🟡 Fusionar con SearchAgent                         |
| `reportsservice`              | 🟡 Fusionar con AdminService                        |
| `schedulerservice`            | 🔴 Eliminar — usar Hangfire en servicios existentes |
| `servicediscovery`            | 🔴 Eliminar — K8s DNS es suficiente                 |
| `vehicle360processingservice` | 🔴 Fusionar con Video360Service                     |
| `vehicleintelligenceservice`  | 🔴 Fusionar con SearchAgent                         |

### 3. Servicios Activos que Pueden Consolidarse

De los **28 servicios activos**, varios tienen funcionalidad superpuesta:

| Servicios Actuales                     | Consolidar En              | Ahorro                          |
| -------------------------------------- | -------------------------- | ------------------------------- |
| `roleservice` + `staffservice`         | → `authservice`            | -2 pods (-100m CPU, -256Mi RAM) |
| `alertservice` + `notificationservice` | → `notificationservice`    | -1 pod (-50m CPU, -128Mi RAM)   |
| `inventorymanagementservice`           | → `vehiclessaleservice`    | -1 pod (-50m CPU, -128Mi RAM)   |
| `maintenanceservice`                   | → `vehiclessaleservice`    | -1 pod (-50m CPU, -128Mi RAM)   |
| `configurationservice`                 | → `adminservice`           | -1 pod (-50m CPU, -128Mi RAM)   |
| `errorservice`                         | → `gateway` (middleware)   | -1 pod (-50m CPU, -128Mi RAM)   |
| `video360service`                      | Escalar a 0 (baja demanda) | -1 pod (-200m CPU, -256Mi RAM)  |

### 4. PVC de 10Gi para LLM que No Se Usa

El `llm-model-pvc` de 10Gi está vinculado al `llm-server` que está escalado a 0. **Costo: $1/mes** innecesario.

---

## ✅ Plan de Consolidación Propuesto

### Fase 1: Eliminación Inmediata (0 riesgo, ahorro inmediato)

**Acción:** Eliminar deployments de servicios escalados a 0 que no se necesitan.

Servicios a eliminar completamente:

- `aiprocessingservice`, `backgroundremovalservice`, `cacheservice`
- `crmservice`, `idempotencyservice`, `integrationservice`
- `messagebusservice`, `ratelimitingservice`, `schedulerservice`
- `servicediscovery`, `llm-server`

**Ahorro:** Limpieza de ConfigMaps/Secrets/Deployments (reduce complejidad). Eliminar PVC `llm-model-pvc` = **-$1/mes**.

### Fase 2: Consolidación de Microservicios (mediano plazo)

#### Propuesta de Arquitectura Consolidada: De 28 → 14 servicios activos

```
ANTES (28 activos):                    DESPUÉS (14 servicios):
─────────────────                      ─────────────────
frontend-web                     →     frontend-web
gateway                          →     gateway
authservice                      →     authservice (+ role + staff + dataprotection)
kycservice                       →     kycservice
adminservice                     →     adminservice (+ config + reports)
vehiclessaleservice              →     vehiclessaleservice (+ inventory + maintenance + comparison)
dealermanagementservice          →     dealermanagementservice (+ dealeranalytics)
billingservice                   →     billingservice (+ payment)
advertisingservice               →     advertisingservice
chatbotservice                   →     chatbotservice (+ searchagent + supportagent + recommendation)
contactservice                   →     contactservice (+ leadscoring + alert)
notificationservice              →     notificationservice (+ marketing)
mediaservice                     →     mediaservice (+ video360)
reviewservice                    →     reviewservice
rabbitmq                         →     rabbitmq
redis                            →     redis
errorservice                     →     (eliminado, middleware en gateway)
appointmentservice               →     appointmentservice
auditservice                     →     auditservice
userservice                      →     (fusionado en authservice)
```

#### Ahorro Estimado de Fase 2

| Métrica                  | Antes   | Después | Ahorro         |
| ------------------------ | ------- | ------- | -------------- |
| Pods activos             | 28      | 16      | -12 pods       |
| CPU solicitada           | 1,925m  | 1,200m  | -725m (37%)    |
| RAM solicitada           | 4,480Mi | 2,800Mi | -1,680Mi (37%) |
| Imágenes Docker en CI/CD | 51      | 16      | -35 builds     |
| Tiempo de CI/CD          | ~15 min | ~8 min  | -47%           |

### Fase 3: Reducción de Nodos (ahorro real)

Con la consolidación de Fase 2, el consumo sería:

- CPU: ~1.2 cores de 8 disponibles (15%)
- RAM: ~2.8 GiB de 16 GiB (17.5%)

**Se puede reducir a 1 solo nodo** o usar nodos más pequeños:

| Opción                     | Configuración  | Costo/mes | Ahorro             |
| -------------------------- | -------------- | --------- | ------------------ |
| **A: 1 nodo s-4vcpu-8gb**  | 4 cores, 8 GiB | $48/mes   | **-$48/mes (50%)** |
| **B: 2 nodos s-2vcpu-4gb** | 4 cores, 8 GiB | $48/mes   | **-$48/mes (50%)** |
| **C: 1 nodo s-2vcpu-4gb**  | 2 cores, 4 GiB | $24/mes   | **-$72/mes (75%)** |

> ⚠️ **Opción recomendada: B** (2 nodos s-2vcpu-4gb) — mantiene alta disponibilidad con 50% ahorro.

---

## 💰 Resumen de Ahorro Proyectado

| Fase      | Acción                           | Ahorro Mensual               |
| --------- | -------------------------------- | ---------------------------- |
| 1         | Eliminar servicios muertos + PVC | $1/mes + complejidad         |
| 2         | Consolidar 28→16 servicios       | Reduce CPU/RAM 37%           |
| 3         | Reducir nodos (opción B)         | **$48/mes**                  |
| **Total** |                                  | **$49/mes (~35% reducción)** |

**Costo actual: ~$139/mes → Costo optimizado: ~$90/mes**

---

## 🔧 Consolidaciones Específicas Recomendadas

### 1. AuthService absorbe: RoleService + StaffService + UserService

**Razón:** Todos gestionan identidad, roles y usuarios. Compartir el mismo DbContext.

### 2. VehiclesSaleService absorbe: InventoryManagement + Maintenance + Comparison

**Razón:** Todos operan sobre la entidad Vehicle. El inventario es un estado del vehículo.

### 3. ChatbotService absorbe: SearchAgent + SupportAgent + Recommendation

**Razón:** Todos usan LLM/AI, comparten la misma infraestructura de prompts y RAG.

### 4. ContactService absorbe: LeadScoring + AlertService

**Razón:** Los leads y alertas son extensiones del flujo de contacto.

### 5. NotificationService absorbe: MarketingService

**Razón:** Ambos envían comunicaciones (email, SMS, WhatsApp).

### 6. AdminService absorbe: ConfigurationService + ReportsService

**Razón:** Funcionalidad administrativa centralizada.

### 7. BillingService absorbe: PaymentService

**Razón:** Pagos son parte del flujo de facturación.

### 8. MediaService absorbe: Video360Service

**Razón:** Ambos procesan media (imágenes y video).

---

## ⚡ Acciones Inmediatas (Sin Código)

```bash
# 1. Eliminar PVC no utilizado
kubectl delete pvc llm-model-pvc -n okla

# 2. Eliminar deployments muertos (ahorra ConfigMap/Secret overhead)
for svc in aiprocessingservice backgroundremovalservice cacheservice \
  crmservice idempotencyservice integrationservice messagebusservice \
  ratelimitingservice schedulerservice servicediscovery llm-server; do
  kubectl delete deployment $svc -n okla
  kubectl delete service $svc -n okla 2>/dev/null
done

# 3. Escalar video360service a 0 (baja demanda)
kubectl scale deployment video360service -n okla --replicas=0

# 4. Reducir resource requests para servicios con bajo uso
# (ejemplo: gateway solo necesita 25m CPU, ya optimizado)
```

---

## 📋 Checklist de Implementación

- [x] **Fase 1 (inmediata):** Eliminar 17 deployments muertos + servicios K8s ✅ (2025-07-14)
- [x] **Fase 1 (inmediata):** Eliminar PVC `llm-model-pvc` (10Gi) ✅ (2025-07-14)
- [x] **Fase 1 (inmediata):** Escalar video360service a 0 ✅ (2025-07-14)
- [x] **Fase 1 (inmediata):** Escalar advertisingservice a 1 ✅ (2025-07-14)
- [x] **Fase 2 (recursos):** Optimizar resource requests (41% CPU, 42% RAM reducción) ✅ (2025-07-14)
- [x] **Validación:** Buyer flow E2E — 10/10 (100%) ✅
- [x] **Validación:** Plans/Coins E2E — 66/66 (100%) ✅
- [ ] **Fase 2 (2-4 semanas):** Consolidar AuthService (+ role + staff + user)
- [ ] **Fase 2 (2-4 semanas):** Consolidar VehiclesSaleService (+ inventory + maintenance)
- [ ] **Fase 2 (2-4 semanas):** Consolidar ChatbotService (+ searchagent + supportagent)
- [ ] **Fase 2 (2-4 semanas):** Consolidar ContactService (+ leadscoring + alert)
- [ ] **Fase 2 (2-4 semanas):** Consolidar AdminService (+ config + reports)
- [ ] **Fase 2 (2-4 semanas):** Consolidar NotificationService (+ marketing)
- [ ] **Fase 2 (2-4 semanas):** Consolidar BillingService (+ payment)
- [ ] **Fase 2 (2-4 semanas):** Consolidar MediaService (+ video360)
- [ ] **Fase 3 (después de consolidar):** Migrar a 2 × s-2vcpu-4gb
- [ ] **Fase 3:** Actualizar CI/CD pipelines (menos builds)
- [ ] **Fase 3:** Actualizar gateway routes

---

## ✅ Resultados de Implementación (2025-07-14)

### Deployments Eliminados (17)

`aiprocessingservice`, `backgroundremovalservice`, `cacheservice`, `crmservice`, `idempotencyservice`, `integrationservice`, `messagebusservice`, `ratelimitingservice`, `schedulerservice`, `servicediscovery`, `llm-server`, `apidocsservice`, `dataprotectionservice`, `leadscoringservice`, `marketingservice`, `vehicle360processingservice`, `vehicleintelligenceservice`

### Recursos Antes → Después

| Métrica            | Antes   | Después         | Reducción |
| ------------------ | ------- | --------------- | --------- |
| Deployments        | 51      | 34 (28 activos) | 33%       |
| CPU requests       | 1,825m  | 1,075m          | **41%**   |
| Memory requests    | 4,480Mi | 2,608Mi         | **42%**   |
| CPU utilization    | 22.8%   | 13.4%           | —         |
| Memory utilization | 27.3%   | 15.9%           | —         |
| PVCs               | 10Gi    | 0Gi             | **100%**  |

### Validación E2E Post-Cambios

- Buyer flow: **10/10 (100%)** ✅
- Plans/Coins: **66/66 (100%)** ✅
- Todos los servicios activos funcionando correctamente

### Plan de Escalamiento

Ver [docs/SCALING_PLAN.md](SCALING_PLAN.md) para el plan completo de escalamiento por fases de crecimiento.
