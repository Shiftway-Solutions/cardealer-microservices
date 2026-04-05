# 🚀 deploy-local-v2.1.sh — Script Mejorado para Deploy Local

**Versión**: 2.1 (2026-04-04)  
**Mejoras**: Corrigidos 4 bugs críticos que causaban servicios "down" después del deploy

---

## 🆕 ¿Qué cambió en v2.1?

| Problema (v2.0)                                     | Solución (v2.1)                                            | Impacto                          |
| --------------------------------------------------- | ---------------------------------------------------------- | -------------------------------- |
| ❌ PASO 2: Solo levanta infra, sin `--profile core` | ✅ Levanta infra → espera healthy → levanta core           | Gateway/Auth no se inicializable |
| ❌ PASO 4: Usa `--no-deps` (peligroso)              | ✅ Quita `--no-deps`, deja que servicios gestionen deps    | Evita race conditions            |
| ❌ PASO 5: Solo espera 8s                           | ✅ Espera hasta que servicios estén "healthy" (max 10 min) | Garantiza readiness              |
| ❌ No detecta servicios "unhealthy"                 | ✅ Chequea `docker compose ps` para estado real            | Visibilidad de problemas         |

---

## 📝 Cómo Usar

### 1️⃣ Reemplazar el script viejo

```bash
cd /ruta/del/proyecto
rm deploy-local.sh
mv deploy-local-v2.1.sh deploy-local.sh
chmod +x deploy-local.sh
```

> **O simplemente usar en la próxima auditoría, sin cambiar nombres**

### 2️⃣ Ejecutar como siempre

```bash
# Rebuild de servicios que cambiaron vs main
./deploy-local.sh

# Forceusto rebuild de servicios específicos
./deploy-local.sh authservice gateway userservice

# Rebuild de TODOS
./deploy-local.sh --all

# Con perfil adicional
./deploy-local.sh --profile vehicles
```

---

## 🔍 Diagnóstico Rápido (si sigue fallando)

### Paso 1: Verificar Docker Desktop

```bash
# Docker está corriendo?
docker info | grep "Server Version"

# Tiene ≥ 16 GB RAM?
docker system df | head -10
```

### Paso 2: Estado de contenedores

```bash
# Ver todos los servicios
docker compose ps

# Salida esperada:
# NAME                 IMAGE                     STATUS
# postgres             postgres:15-alpine        Up (healthy)
# redis                redis:7-alpine            Up (healthy)
# rabbitmq             rabbitmq:3.12-management Up (healthy)
# authservice          okla:authservice          Up (healthy)
# gateway              okla:gateway              Up (healthy)
# userservice          okla:userservice          Up (healthy)
```

### Paso 3: Logs de servicio específico si está unhealthy

```bash
# Logs del servicio problemático
docker compose logs --tail=50 gateway

# O si quieres en tiempo real
docker compose logs -f gateway
```

### Paso 4: Health check manual

```bash
# Gateway disponible?
curl -s http://localhost:18443/health | jq .

# Auth disponible?
curl -s http://localhost:15001/health | jq .

# RabbitMQ listo?
curl -s http://localhost:15672/api/connections | head -5
```

---

## ⏱️ Tiempos Esperados

| Acción                         | Duración    | Notas                       |
| ------------------------------ | ----------- | --------------------------- |
| `docker compose up -d` (infra) | 8-12s       | postgres + redis + rabbitmq |
| Wait for healthy (infra)       | 15-20s      | RabbitMQ es el lento        |
| Build servicio                 | 30-60s      | Depende del tamaño          |
| Restart servicio               | 5-10s       | Recrea container            |
| Wait for healthy (servicio)    | 10-15s      | Startup de .NET             |
| **Total**                      | **1-3 min** | Según # servicios           |

Si toma >5 minutos → algún servicio está stuck

---

## 🐛 Common Errors & Fixes

### Error: "postgres never reached healthy state"

**Causa**: Port 5433 ya en uso o permisos insuficientes

**Fix**:

```bash
# Liberar puerto
lsof -i :5433 | grep postgres | awk '{print $2}' | xargs kill -9

# O simplemente restart Docker Desktop (Settings → Resources → Restart)
```

### Error: "rabbitmq never reached healthy state"

**Causa**: RabbitMQ necesita >10s para estar 100% ready

**Fix**: El script v2.1 ya espera 40s → si sigue fallando:

```bash
docker compose logs rabbitmq | tail -20  # Ver qué está mal
```

### Error: "gateway showing unhealthy during health check"

**Causa**: Gateway intenta conectar a authservice que aún no está ready

**Fix**: Ya corregido en v2.1 (espera a authservice primero). Si persiste:

```bash
# Restart solo gateway después de que auth esté OK
docker compose restart authservice
sleep 10
docker compose restart gateway
```

---

## 🏃 Flujo de Desarrollo Recomendado

```bash
# 1. Infra siempre corriendo (terminal 1)
docker compose up -d && docker compose logs -f

# 2. Desarrollar con hot-reload (terminal 2)
cd backend/AuthService && dotnet watch run

# 3. Cuando cambios importantes → rebuild local
cd /proyecto && ./deploy-local.sh authservice

# 4. Verificar que todo esté up
docker compose ps | grep -E "authservice|gateway|userservice"

# 5. Volver a hot-reload
# El servidor local de dotnet watch ya captó los cambios
```

---

## ✅ Verificación Pre-Deploy: Checklist

Antes de ejecutar `./deploy-local.sh`, asegurate:

- [ ] `docker compose ps` muestra all services healthy
- [ ] Backend: `cd backend && dotnet build` pasa sin errores
- [ ] Frontend: `cd frontend/web-next && pnpm build` pasa sin errores
- [ ] Git: No hay cambios sin commitear (o committed)
- [ ] `docker info` confirma Docker está corriendo con ≥10 GB free

Si algo falla en el checklist → NO ejecutes deploy-local.sh (primero arregla el error)

---

## 📊 Monitoreo En Tiempo Real

```bash
# Terminal dedicada: Ver logs while deploying
watch -n 1 'docker compose ps | grep -E "STATUS|authservice|gateway"'

# O con logs:
docker compose logs -f --tail=30 gateway authservice userservice
```

---

## 🆚 Diferencia v2.0 vs v2.1 (lado a lado)

### v2.0 PASO 2 (INCORRECTO)

```bash
docker compose up -d  # Solo infra, sin core profile
sleep 8               # Espera fija insuficiente
```

### v2.1 PASO 2 (CORRECTO)

```bash
docker compose up -d  # Infra
wait_for_healthy "postgres" 30  # Espera real
wait_for_healthy "redis" 20
wait_for_healthy "rabbitmq" 40
docker compose --profile core up -d  # Luego levanta core
wait_for_healthy "authservice" 40
wait_for_healthy "gateway" 40
```

---

## 📞 Si Sigue Fallando (Debugging Avanzado)

```bash
# 1. Ver logs detallados de container
docker compose logs --tail=100 authservice | grep -i "error\|exception"

# 2. Revisar estado de postgres
docker compose exec postgres pg_isready

# 3. Chequear conectividad redis desde gateway
docker compose exec gateway redis-cli -h redis -p 6379 ping

# 4. Inspect del container (si está stuck)
docker inspect <container_id> | jq .State
```

Si ves `"ExitCode": 1` pero container sigue "Up" → es proceso zombie, restart:

```bash
docker compose restart <servicio>
```

---

## 📝 Notas

- v2.1 es **backward compatible** — reemplaza directamente v2.0
- El script registra cada deploy en `.github/copilot-audit.log`
- Tiempos en el script son conservadores (mejor esperar de más que de menos)
- Si quieres modo silencioso, redirige: `./deploy-local.sh 2>/dev/null`

---

**Última actualización**: 2026-04-04  
**Responsable**: Copilot Audit  
**Status**: ✅ Listo para produksction
