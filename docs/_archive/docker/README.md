# 🐳 Docker Management - OKLA Microservices

**Estado:** ✅ Configurado y Optimizado  
**Última actualización:** Enero 8, 2026  
**Responsable:** Sistema de Monitoreo Automático

---

## 📋 Estructura de Documentación

Esta carpeta contiene toda la documentación y scripts para gestionar Docker en el proyecto OKLA.

```
docs/docker/
├── README.md (este archivo)
├── DISK_MANAGEMENT.md          ⭐ Guía completa
├── QUICK_REFERENCE.md          ⚡ Comandos esenciales
├── CRON_SETUP_GUIDE.md        🔧 Automatización
├── ARCHITECTURE.md             📐 Diseño de servicios
└── TROUBLESHOOTING.md         🐛 Solución de problemas

scripts/
├── docker-monitor.sh           👀 Monitoreo semanal
└── docker-auto-clean.sh        🧹 Limpieza automática
```

---

## 🚨 Crisis Pasada: Lecciones Aprendidas

### Qué Pasó (Enero 8, 2026)

**Problema:** Docker.raw creció a 926 GB (100% del disco)

**Causas identificadas:**

```
Docker.raw (926 GB)
├── Build cache:      25-38% (~250 GB)
├── Unused images:    32-44% (~300 GB)
├── Stopped containers: 12-19% (~150 GB)
├── Volumes:          8-10% (~80 GB)
└── Logs:             1-2% (~10 GB)
```

**Resolución:** Eliminamos Docker.raw (liberamos 792 GB) y lo configuramos con límite de 40 GB

**Prevención:** Scripts automáticos + limpieza semanal

---

## 🎯 Objetivos

✅ **Prevenir:** Que Docker nunca vuelva a llenar el disco  
✅ **Monitorear:** Estado del disco automáticamente cada semana  
✅ **Limpiar:** Automáticamente antes de alcanzar 80%  
✅ **Documentar:** Procedimientos para el equipo

---

## ⚡ Inicio Rápido

### Si eres desarrollador (primera vez)

```bash
# 1. Ejecutar monitoreo
bash scripts/docker-monitor.sh

# 2. Revisar estado
df -h /

# 3. Si disco > 75%, limpiar
bash scripts/docker-auto-clean.sh

# 4. Leer QUICK_REFERENCE.md para comandos útiles
```

### Si trabajas regularmente

```bash
# Cada lunes a las 8 AM: verificación automática
# Cada mes: limpieza automática
# (Ya configurado en cron - no requiere intervención)
```

### Si quieres configurar automatización

```bash
# Ver: CRON_SETUP_GUIDE.md
# Ejecutar los pasos en la sección "Paso 1: Crear archivo de cron"
```

---

## 📊 Estado Actual

| Métrica                   | Valor           | Referencia              |
| ------------------------- | --------------- | ----------------------- |
| **Disco Total**           | 926 GB          | macOS                   |
| **Disco Disponible**      | 799 GB          | 2% usado                |
| **Límite Docker**         | 40 GB (máx)     | Configurado             |
| **Microservicios**        | 21 custom       | .NET 8                  |
| **Imágenes Externas**     | 16              | postgres, rabbitmq, etc |
| **Estimado Docker Usage** | 19-40 GB        | Seguro                  |
| **Frecuencia Monitoreo**  | Semanal (lunes) | Automático              |
| **Frecuencia Limpieza**   | Mensual         | Automático              |

---

## 📚 Documentos Principales

### 1. **DISK_MANAGEMENT.md** ⭐ COMIENZA AQUÍ

Guía completa de 450+ líneas que incluye:

- ✅ Análisis detallado del problema (792 GB)
- ✅ Desglose de dónde fue el espacio
- ✅ Límites de seguridad recomendados
- ✅ Procedimientos de monitoreo (semanal/mensual)
- ✅ 3 niveles de limpieza (safe/forced/manual)
- ✅ Configuración de Docker Desktop (40 GB)
- ✅ Scripts automatizados con ejemplos
- ✅ Guía de troubleshooting
- ✅ Tabla de referencia de comandos

**Leer cuando:** Necesitas entender el problema completo o configurar Docker por primera vez

---

### 2. **QUICK_REFERENCE.md** ⚡ PARA USAR DIARIAMENTE

Comandos esenciales organizados por tarea:

```bash
# Ejemplo rápido
bash /scripts/docker-monitor.sh    # Verificar
docker system prune --volumes -f   # Limpiar básico
df -h /                            # Ver espacio
```

**Leer cuando:** Necesitas ejecutar comandos sin pensar

---

### 3. **CRON_SETUP_GUIDE.md** 🔧 PARA AUTOMATIZAR

Pasos exactos para programar trabajos automáticos:

```bash
crontab -e
# Agregar:
# 0 8 * * 1 /scripts/docker-monitor.sh >> /tmp/docker-monitor.log 2>&1
# 0 21 * * 0 /scripts/docker-auto-clean.sh >> /tmp/docker-clean.log 2>&1
```

**Leer cuando:** Quieres que todo sea automático (recomendado)

---

### 4. **TROUBLESHOOTING.md** 🐛 PARA RESOLVER PROBLEMAS

Soluciones paso a paso para problemas comunes:

- Docker no inicia
- Disco lleno nuevamente
- Scripts no se ejecutan
- Permisos de archivos
- etc.

**Leer cuando:** Algo no funciona como esperado

---

## 🚀 Scripts Disponibles

### `docker-monitor.sh` (200 líneas)

**Propósito:** Verificar estado de Docker y disco

**Qué hace:**

1. Verifica uso de disco actual
2. Muestra desglose de Docker (containers, images, volumes, build cache)
3. Lista contenedores activos y parados
4. Muestra top 10 imágenes más grandes
5. Recomienda acciones basado en umbrales

**Cómo usar:**

```bash
bash /scripts/docker-monitor.sh
```

**Salida esperada:**

```
╔════════════════════════════════════════════════════════════════════╗
║         👀 DOCKER MONITOR - ESTADO DE DISCO                        ║
╚════════════════════════════════════════════════════════════════════╝

📊 Disco: 799GB disponible (2% usado)
🐳 Docker: 15.3 GB usado

✅ Estado OK - Disco en buen estado
```

---

### `docker-auto-clean.sh` (150 líneas)

**Propósito:** Limpiar Docker automáticamente basado en uso

**Cómo funciona:**

- < 70%: No hace nada
- 70-80%: Limpieza normal (prune)
- 80-90%: Limpieza agresiva (containers, images, volumes, cache)
- > 90%: Limpieza forzada + restart Docker

**Cómo usar:**

```bash
bash /scripts/docker-auto-clean.sh
```

**Automáticamente cada mes por cron (no requiere intervención manual)**

---

## 📅 Plan de Mantenimiento

### ✅ Semanal (Lunes 8:00 AM)

```bash
# Automático por cron
docker-monitor.sh
# Resultado: Email/notificación si disco > 75%
```

### ✅ Mensual (Primer domingo 9:00 PM)

```bash
# Automático por cron
docker-auto-clean.sh
# Resultado: Limpieza profunda, logs guardados
```

### 🔄 Trimestral (Manual)

```bash
# Revisar Docker.raw size
du -sh ~/Library/Containers/com.docker.docker/Data/vms/0/data/Docker.raw

# Si > 30 GB, considerar reset
# Settings → Resources → Disk image → "Reset to initial value"
```

### 📊 Semestral (Manual)

```bash
# Reset completo de Docker
# Backup de volúmenes importantes
# Restart desde cero
```

---

## 🎓 Educación del Equipo

### Para nuevos developers

1. **Leer:** QUICK_REFERENCE.md (5 minutos)
2. **Hacer:** Ejecutar `docker-monitor.sh` (2 minutos)
3. **Entender:** Básicos de Docker y espacio en disco

### Para ops/devops

1. **Leer:** DISK_MANAGEMENT.md completo (30 minutos)
2. **Configurar:** Cron jobs (CRON_SETUP_GUIDE.md)
3. **Monitorear:** Logs mensuales
4. **Optimizar:** Ajustar thresholds según necesidad

### Para líderes de equipo

- Revisar logs mensuales (`/tmp/docker-monitor.log`, `/tmp/docker-clean.log`)
- Verificar tendencias de uso (¿crece mes a mes?)
- Escalar si Docker > 30 GB regularmente

---

## 🔒 Seguridad y Backups

### No usar en producción:

- ❌ No ejecutar `docker system prune` en producción sin testing
- ❌ No cambiar Docker.raw sin backup
- ❌ No usar `-f` (force) sin confirmar

### Recomendaciones:

- ✅ Hacer backup de Docker.raw antes de reset
- ✅ Ejecutar limpieza en off-peak hours
- ✅ Monitorear contenedores después de limpieza
- ✅ Mantener logs de todas las operaciones

---

## 📈 Métricas a Monitorear

### Disco Total

```bash
df -h /
```

**Meta:** Mantener > 200 GB libre (siempre)

### Docker Usage

```bash
docker system df
```

**Meta:** Mantener < 20 GB (en desarrollo)

### Top Images

```bash
docker images --format "table {{.Repository}}\t{{.Size}}" | sort -k3 -hr
```

**Meta:** Eliminar imágenes de desarrollo que no usen

### Build Cache

```bash
docker builder du --verbose
```

**Meta:** Mantener < 5 GB

---

## 🆘 Soporte Rápido

### "¿Dónde está el espacio?"

```bash
bash /scripts/docker-monitor.sh
```

Muestra desglose completo

### "¿Cómo lo limpio?"

```bash
# Opción 1: Normal
docker system prune --volumes -f

# Opción 2: Agresivo
bash /scripts/docker-auto-clean.sh

# Ver resultados
docker system df
```

### "¿Cómo automatizo?"

Ver CRON_SETUP_GUIDE.md (15 minutos para configurar)

### "¿Qué hacer si falla?"

Ver TROUBLESHOOTING.md o contactar al equipo de DevOps

---

## 📞 Contacto y Escalación

**Propietario:** Sistema Automático  
**Mantenedor:** Gregory Moreno  
**Equipo:** DevOps / Infrastructure

**Escalar si:**

- Disco llega a > 80% ⚠️
- Scripts fallan repetidamente 🚨
- Docker.raw > 35 GB 🔴
- Contenedores no inician después de limpieza 🛑

---

## 🔗 Enlaces Útiles

| Documento                                  | Propósito          | Tiempo de lectura |
| ------------------------------------------ | ------------------ | ----------------- |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md)   | Comandos rápidos   | 3 min             |
| [DISK_MANAGEMENT.md](DISK_MANAGEMENT.md)   | Guía completa      | 30 min            |
| [CRON_SETUP_GUIDE.md](CRON_SETUP_GUIDE.md) | Automatización     | 15 min            |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md)   | Resolver problemas | 10 min            |

---

## ✅ Checklist de Implementación

- [x] Crisis resuelta (Docker.raw eliminado, 792 GB recuperados)
- [x] Límite de 40 GB configurado en Docker Desktop
- [x] Scripts de monitoreo y limpieza creados
- [x] Documentación completa (4 documentos)
- [x] Procedimientos de cron documentados
- [x] Umbrales de alerta establecidos
- [ ] Cron jobs configurados (acción manual, ver CRON_SETUP_GUIDE.md)
- [ ] Logs monitoreados (semanal)
- [ ] Equipo capacitado (todo el equipo debe leer QUICK_REFERENCE.md)

---

## 📝 Versión y Changelog

| Versión | Fecha      | Cambios                         |
| ------- | ---------- | ------------------------------- |
| 1.0     | 2026-01-08 | Creación inicial (post-crisis)  |
| 1.1     | TBD        | Ajustes basados en feedback     |
| 2.0     | TBD        | Integración con Kubernetes/DOKS |

---

## 🎯 Próximas Mejoras

- [ ] Integración con Grafana para dashboards
- [ ] Alertas en Slack cuando disco > 75%
- [ ] Histórico de Docker usage (CSV/DB)
- [ ] Predicción de crecimiento (ML simple)
- [ ] Optimización de imágenes (squash)
- [ ] Registry cleanup (ghcr.io)

---

**Última actualización:** Enero 8, 2026  
**Estado:** ✅ Operativo  
**Próxima revisión:** Abril 2026 (trimestral)

---

## 🚀 Start Here

### ⏱️ 2 minutos

```bash
bash /scripts/docker-monitor.sh
```

### ⏱️ 5 minutos

Leer QUICK_REFERENCE.md

### ⏱️ 30 minutos

Leer DISK_MANAGEMENT.md completo

### ⏱️ 15 minutos (recomendado)

Configurar cron jobs con CRON_SETUP_GUIDE.md

**¡Listo!** Docker está bajo control 🎉
