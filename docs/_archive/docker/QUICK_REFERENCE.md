# 🚀 Docker Management - Guía Rápida

**Última actualización:** Enero 8, 2026  
**Versión:** 1.0

---

## ⚡ Comandos Esenciales

### 🔍 Monitorear

```bash
# Verificación completa (ejecutar una vez al mes)
bash /scripts/docker-monitor.sh

# Ver size de todo en Docker
docker system df

# Ver imágenes por tamaño
docker images --format "table {{.Repository}}\t{{.Size}}" | sort -k3 -hr

# Ver volúmenes
docker volume ls
docker volume ls -f dangling=true

# Ver contenedores parados (ocupan espacio)
docker ps -a --filter "status=exited"
```

### 🧹 Limpiar Disco

```bash
# NIVEL 1: Seguro (RECOMENDADO SEMANAL)
docker system prune --volumes -f

# NIVEL 2: Agresivo (MENSUAL)
docker container prune -f          # Contenedores parados
docker image prune -a -f           # Imágenes no usadas
docker volume prune -f             # Volúmenes no usados
docker builder prune -a -f         # Cache de build

# NIVEL 3: Forzado (EMERGENCIA)
docker system prune -a --volumes --force

# NIVEL 4: Manual (Caso extremo)
# ⚠️ PELIGRO: Borra TODO
docker ps -aq | xargs docker rm -f
docker images -aq | xargs docker rmi -f
```

### 📊 Ver Uso

```bash
# Disco total usado
df -h /

# Docker total
docker system df

# Desglose detallado
docker system df --verbose

# Top 10 imágenes más grandes
docker images --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}" | head -11

# Tamaño de volúmenes
du -sh ~/Library/Containers/com.docker.docker/Data/vms/0/data/*

# Cache de build
docker builder du --verbose
```

---

## 📋 Procedimiento Semanal (5 minutos)

```bash
# 1. Monitorear
bash /scripts/docker-monitor.sh

# 2. Si disco > 75%, limpiar
docker system prune --volumes -f

# 3. Verificar nuevamente
df -h /
```

---

## 📋 Procedimiento Mensual (15 minutos)

```bash
# 1. Detener servicios
docker-compose down

# 2. Limpieza agresiva
docker container prune -f
docker image prune -a -f
docker volume prune -f
docker builder prune -a -f

# 3. Limpiar Docker.raw (cada 6 meses)
# En Settings → Resources → Disk image size → reset to initial value

# 4. Reiniciar
docker-compose up -d
```

---

## 🚨 Si Disco Está al 100%

```bash
# ¡URGENTE! Paso 1: Detener Docker
osascript -e 'quit app "Docker"'
sleep 30

# Paso 2: Limpiar manualmente
# Limpiar ~/Library/Containers/com.docker.docker
# (Esto libera espacio inmediatamente)

# Paso 3: Reiniciar
open -a Docker
sleep 60

# Paso 4: Verificar
docker ps
```

---

## 🐳 Docker Desktop Settings

**Para evitar problemas, configurar:**

1. **Abrir Docker Desktop**
2. **Settings (⚙️) → Resources**
3. **Disk Image size:** 40 GB (máximo)
4. **Swap:** 2 GB
5. **CPU:** 4 cores
6. **Memory:** 6 GB

---

## 📊 Estimación de Uso

| Componente             | Tamaño       | Referencia          |
| ---------------------- | ------------ | ------------------- |
| .NET 8 SDK             | 2.2 GB       | Una sola vez        |
| Postgres image         | 150 MB       | Por database        |
| RabbitMQ image         | 190 MB       | Una sola vez        |
| Redis image            | 40 MB        | Una sola vez        |
| Build cache (1º build) | 10-15 GB     | Temporal, se limpia |
| 21 microservicios      | 6-8 GB       | Incluye runtime     |
| **TOTAL ESTIMADO**     | **19-40 GB** | Para OKLA completo  |

---

## ⏱️ Frecuencia Recomendada

| Tarea                   | Frecuencia | Tiempo | Automático |
| ----------------------- | ---------- | ------ | ---------- |
| Monitoreo               | Semanal    | 2 min  | ✅ Cron    |
| Limpieza nivel 1        | Semanal    | 1 min  | ✅ Cron    |
| Limpieza nivel 2        | Mensual    | 5 min  | ✅ Cron    |
| Revisión de Docker.raw  | Trimestral | 30 min | ❌ Manual  |
| Reset de Docker Desktop | Semestral  | 1 hora | ❌ Manual  |

---

## 🎯 Umbrales de Alerta

| Uso de Disco | Estado  | Acción                           |
| ------------ | ------- | -------------------------------- |
| < 70%        | ✅ OK   | Nada, todo bien                  |
| 70-80%       | ⚠️ INFO | Monitor y limpieza básica (cron) |
| 80-90%       | ⚠️ WARN | Limpieza agresiva inmediatamente |
| > 90%        | 🚨 CRIT | PARAR DOCKER, limpiar emergencia |

---

## 🔗 Scripts Disponibles

### docker-monitor.sh

```bash
bash /scripts/docker-monitor.sh
```

**Ejecuta:**

- Verifica disco actual
- Muestra Docker system df
- Lista contenedores (activos/parados)
- Top 10 imágenes por tamaño
- Recomienda acciones basado en thresholds

**Frecuencia:** Semanal (automático)

### docker-auto-clean.sh

```bash
bash /scripts/docker-auto-clean.sh
```

**Ejecuta:**

- Detecta nivel de uso (70%, 80%, 90%)
- Aplica limpieza apropiada automáticamente
- Reinicia Docker si es necesario
- Verifica resultados

**Frecuencia:** Mensual (automático)

---

## 🔑 Puntos Clave

✅ **HACER:**

- Monitorear cada semana
- Limpiar antes de que alcance 80%
- Hacer backup de Docker.raw si es crítico
- Usar 40 GB como límite máximo
- Automatizar con cron jobs

❌ **EVITAR:**

- Esperar a 100% para limpiar
- Compilar 20+ imágenes a la vez
- Dejar Docker.raw sin límite
- Ignorar advertencias del monitor
- Usar docker rm -f en producción

---

## 🐛 Troubleshooting Rápido

| Problema                     | Solución                         |
| ---------------------------- | -------------------------------- |
| Docker no inicia             | Restart: `open -a Docker`        |
| Disk > 80%                   | `docker system prune -a --force` |
| Contenedor usa mucho espacio | `docker exec {id} du -sh /`      |
| Build lento                  | `docker builder prune -a -f`     |
| Volumen lleno                | `docker volume prune -f`         |

---

## 📞 Soporte

Si tienes problemas:

1. Ver `DISK_MANAGEMENT.md` para detalles completos
2. Ver `CRON_SETUP_GUIDE.md` para automación
3. Ejecutar `docker system df --verbose` para diagnóstico
4. Revisar logs: `cat /tmp/docker-monitor.log`

---

**Última revisión:** Enero 8, 2026  
**Próxima revisión:** Abril 2026
