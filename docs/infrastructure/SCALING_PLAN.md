# 📈 OKLA — Plan de Escalamiento por Crecimiento de Usuarios

**Fecha:** 2025-07-14  
**Autor:** GitHub Copilot (Claude)  
**Estado actual:** 28 servicios activos, 2× s-4vcpu-8gb nodes ($96/mes)

---

## 1. Estado Actual del Cluster (Post-Optimización)

| Métrica            | Antes   | Después         | Reducción |
| ------------------ | ------- | --------------- | --------- |
| Deployments        | 51      | 34 (28 activos) | 33%       |
| CPU requests       | 1,825m  | 1,075m          | 41%       |
| Memory requests    | 4,480Mi | 2,608Mi         | 42%       |
| CPU utilization    | 22.8%   | 13.4%           | —         |
| Memory utilization | 27.3%   | 15.9%           | —         |

**Capacidad libre actual:** ~6,900m CPU y ~13,700Mi RAM disponibles.

---

## 2. Fases de Escalamiento

### 🟢 Fase 0: MVP / Lanzamiento (0–500 usuarios)

**Infraestructura actual es suficiente.**

| Componente    | Configuración                |
| ------------- | ---------------------------- |
| Nodos         | 2× s-4vcpu-8gb ($96/mes)     |
| PostgreSQL    | DO Managed - Basic ($15/mes) |
| Load Balancer | DO LB ($12/mes)              |
| Total         | **~$123/mes**                |

**Acciones:**

- ✅ Ya implementado
- Monitorear con `kubectl top` si se instala metrics-server
- Configurar alertas básicas (CPU > 70%, Memory > 75%)
- HPA no necesario aún

**Capacidad estimada:**

- ~500 usuarios concurrentes
- ~50 dealers activos
- ~1,000 vehículos publicados

---

### 🟡 Fase 1: Crecimiento Inicial (500–2,000 usuarios)

**Trigger:** CPU requests > 50% o Memory > 60% sostenido

| Componente | Cambio                                    |
| ---------- | ----------------------------------------- |
| Nodos      | Mantener 2× s-4vcpu-8gb                   |
| Réplicas   | Frontend: 2, AuthService: 2, Gateway: 2   |
| Redis      | Subir a 128Mi limit                       |
| PostgreSQL | Subir a DO Managed - Standard ($30/mes)   |
| CDN        | Activar DO Spaces CDN para media ($5/mes) |
| Total      | **~$148/mes** (+$25)                      |

**Acciones:**

```bash
# Escalar servicios críticos
kubectl scale deployment frontend-web --replicas=2 -n okla
kubectl scale deployment authservice --replicas=2 -n okla
kubectl scale deployment gateway --replicas=2 -n okla

# Instalar metrics-server para HPA
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Configurar HPA para frontend
kubectl autoscale deployment frontend-web -n okla \
  --cpu-percent=70 --min=2 --max=4
```

**Capacidad estimada:**

- ~2,000 usuarios concurrentes
- ~200 dealers activos
- ~5,000 vehículos publicados

---

### 🟠 Fase 2: Escalamiento Medio (2,000–10,000 usuarios)

**Trigger:** Nodos al 65%+ CPU o necesidad de alta disponibilidad

| Componente | Cambio                                                 |
| ---------- | ------------------------------------------------------ |
| Nodos      | 3× s-4vcpu-8gb ($144/mes) o 2× s-8vcpu-16gb ($192/mes) |
| Réplicas   | Servicios críticos: 2–3, resto: 1                      |
| PostgreSQL | Connection pooling (PgBouncer)                         |
| Redis      | Cluster mode o DO Managed Redis ($15/mes)              |
| RabbitMQ   | 2 nodos (clustering)                                   |
| CDN        | Cloudflare Free tier para caché y DDoS                 |
| Total      | **~$230–280/mes**                                      |

**Acciones:**

```bash
# HPA para servicios de alto tráfico
for svc in frontend-web gateway authservice vehiclessaleservice chatbotservice; do
  kubectl autoscale deployment $svc -n okla \
    --cpu-percent=65 --min=2 --max=5
done

# Agregar tercer nodo (vía DO dashboard o doctl)
doctl kubernetes cluster node-pool update <cluster-id> <pool-id> \
  --count 3

# PgBouncer como sidecar o deployment separado
# Ver docs/ARCHITECTURE.md para detalles
```

**Optimizaciones adicionales:**

- Implementar caché de consultas frecuentes en Redis (vehículos, planes)
- Rate limiting por IP en Nginx Ingress
- Compresión gzip/brotli en Nginx
- Image optimization con Next.js (ya incluido)
- Database read replicas para consultas de lectura

**Capacidad estimada:**

- ~10,000 usuarios concurrentes
- ~1,000 dealers activos
- ~25,000 vehículos publicados

---

### 🔴 Fase 3: Alta Escala (10,000–50,000 usuarios)

**Trigger:** Revenue justifica inversión mayor, múltiples regiones

| Componente | Cambio                                    |
| ---------- | ----------------------------------------- |
| Nodos      | 4–6× s-4vcpu-8gb o auto-scaling node pool |
| PostgreSQL | DO Managed HA ($60/mes) + Read Replicas   |
| Redis      | DO Managed Redis HA ($30/mes)             |
| RabbitMQ   | 3-nodo cluster con quorum queues          |
| CDN        | Cloudflare Pro ($20/mes)                  |
| Monitoring | Grafana Cloud Free + Prometheus           |
| Total      | **~$500–700/mes**                         |

**Arquitectura:**

```
                    ┌─────────────┐
                    │ Cloudflare  │
                    │   CDN/WAF   │
                    └──────┬──────┘
                           │
                    ┌──────┴──────┐
                    │  DO LB x2   │
                    │  (HA pair)  │
                    └──────┬──────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
        ┌─────┴─────┐ ┌───┴───┐ ┌─────┴─────┐
        │ Node Pool │ │  Pool │ │ Node Pool │
        │  Frontend │ │ Infra │ │  Backend  │
        │  (2-4)    │ │ (1-2) │ │  (2-4)    │
        └───────────┘ └───────┘ └───────────┘
```

**Acciones:**

- Separar node pools: frontend, backend, infrastructure
- Database sharding por dealer (si necesario)
- Event sourcing para audit trail
- Implementar circuit breakers (Polly ya disponible)
- API versioning formal (v1, v2)

---

### ⚫ Fase 4: Escala Enterprise (50,000+ usuarios)

**Trigger:** Expansión a otros países, necesidad multi-región

| Componente    | Cambio                                 |
| ------------- | -------------------------------------- |
| Nodos         | Multi-cluster (DO NYC + DO SFO)        |
| PostgreSQL    | Citus/CockroachDB para geo-distributed |
| Message Queue | RabbitMQ → Apache Kafka                |
| CDN           | Cloudflare Enterprise                  |
| Search        | Elasticsearch/Meilisearch dedicado     |
| Total         | **~$2,000–5,000/mes**                  |

**Consideraciones:**

- Service mesh (Istio/Linkerd) para observabilidad
- GitOps con ArgoCD para deployments
- Feature flags (LaunchDarkly/Unleash)
- A/B testing framework
- Compliance (PCI-DSS si procesamos pagos directamente)

---

## 3. Consolidación de Microservicios (Recomendado en Fase 2+)

Para reducir overhead operacional, algunos servicios pueden fusionarse:

| Servicios Actuales                               | Servicio Consolidado    | Razón                     |
| ------------------------------------------------ | ----------------------- | ------------------------- |
| userservice + roleservice + staffservice         | **IdentityService**     | Mismo dominio (usuarios)  |
| alertservice + notificationservice               | **NotificationService** | Canales de comunicación   |
| searchagent + supportagent                       | **AgentService**        | Misma infraestructura LLM |
| configurationservice + maintenanceservice        | **PlatformService**     | Config/mantenimiento      |
| inventorymanagementservice + vehiclessaleservice | **VehicleService**      | Mismo bounded context     |

**Resultado:** 28 → ~18 servicios (36% menos deployments)

⚠️ **Solo consolidar cuando haya evidencia de que el overhead de microservicios supera los beneficios.** En la fase actual (MVP/lanzamiento), mantener separados permite iteración rápida.

---

## 4. Métricas de Decisión para Escalar

### Cuándo agregar nodos

| Métrica                 | Umbral          | Acción                    |
| ----------------------- | --------------- | ------------------------- |
| CPU node utilization    | > 70% por 15min | Agregar nodo              |
| Memory node utilization | > 80% por 15min | Agregar nodo              |
| Pod pending             | > 0 por 5min    | Nodo lleno, agregar       |
| HTTP 503 rate           | > 1%            | Escalar servicio afectado |

### Cuándo escalar servicios (HPA)

| Métrica                | Umbral      | Réplicas           |
| ---------------------- | ----------- | ------------------ |
| CPU pod                | > 65%       | +1 réplica         |
| Request latency P95    | > 2s        | +1 réplica         |
| Queue depth (RabbitMQ) | > 1000 msgs | +1 consumer        |
| Error rate             | > 5%        | Investigar primero |

### Cuándo escalar base de datos

| Métrica           | Umbral    | Acción           |
| ----------------- | --------- | ---------------- |
| Connections       | > 80% max | Add PgBouncer    |
| Query latency P95 | > 500ms   | Add read replica |
| Storage           | > 80%     | Resize disk      |
| CPU               | > 70%     | Upgrade plan     |

---

## 5. Estimación de Costos por Fase

```
Fase 0 (MVP):      $123/mes  ← ACTUAL
Fase 1 (2K users):  $148/mes  (+20%)
Fase 2 (10K users): $280/mes  (+90% vs Fase 1)
Fase 3 (50K users): $700/mes  (+150% vs Fase 2)
Fase 4 (50K+):    $2,000+/mes (enterprise)
```

### Revenue vs Costo (Break-even Analysis)

| Fase | Costo/mes | Dealers necesarios  | Revenue estimado |
| ---- | --------- | ------------------- | ---------------- |
| 0    | $123      | 3 dealers Pro ($89) | $267/mes         |
| 1    | $148      | 4 dealers Pro       | $356/mes         |
| 2    | $280      | 8 dealers Pro       | $712/mes         |
| 3    | $700      | 20 dealers Pro      | $1,780/mes       |

**Con solo 3 dealers en plan Pro ($89/mes) + listados individuales, la plataforma se autofinancia.**

---

## 6. Checklist de Implementación

### Inmediato (Fase 0)

- [x] Eliminar deployments muertos (17 eliminados)
- [x] Optimizar resource requests (41% CPU, 42% RAM reducción)
- [x] Validar E2E después de cambios (100% pass)
- [ ] Instalar metrics-server para monitoreo
- [ ] Configurar alertas básicas (Prometheus/Alertmanager o DO Monitoring)

### Próximo trimestre (si crecimiento > 500 usuarios)

- [ ] Escalar frontend y auth a 2 réplicas
- [ ] Activar CDN para media
- [ ] Configurar HPA en servicios críticos
- [ ] Implementar connection pooling (PgBouncer)

### Próximo año (si crecimiento > 2,000 usuarios)

- [ ] Agregar tercer nodo o migrar a nodos más grandes
- [ ] Consolidar microservicios según tabla de Sección 3
- [ ] Implementar read replicas
- [ ] Evaluar Cloudflare para caché y DDoS protection

---

## 7. Comandos Útiles de Monitoreo

```bash
# Estado general del cluster
kubectl get nodes -o wide
kubectl get pods -n okla --sort-by=.status.startTime

# Uso de recursos (requiere metrics-server)
kubectl top nodes
kubectl top pods -n okla --sort-by=memory

# Pods con problemas
kubectl get pods -n okla --field-selector=status.phase!=Running

# HPA status
kubectl get hpa -n okla

# Eventos recientes
kubectl get events -n okla --sort-by=.lastTimestamp | tail -20

# Resource requests totales
kubectl get pods -n okla -o json | python3 -c "
import json,sys
data=json.load(sys.stdin)
cpu=mem=0
for p in data['items']:
  for c in p['spec'].get('containers',[]):
    r=c.get('resources',{}).get('requests',{})
    v=r.get('cpu','0m')
    cpu+=int(v[:-1]) if v.endswith('m') else int(float(v)*1000)
    v=r.get('memory','0Mi')
    mem+=int(v[:-2]) if v.endswith('Mi') else 0
print(f'CPU: {cpu}m, Memory: {mem}Mi')
"
```

---

_Este plan se actualizará conforme crezca la plataforma. Revisar trimestralmente._
