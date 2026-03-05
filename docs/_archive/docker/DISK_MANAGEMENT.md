# 🐳 Docker Disk Management - Gestión de Espacio en Disco

**Última actualización:** Enero 18, 2026

---

## 📋 Tabla de Contenidos

1. [Resumen del Problema](#resumen-del-problema)
2. [Límites de Seguridad](#límites-de-seguridad)
3. [Monitoreo Regular](#monitoreo-regular)
4. [Limpieza de Disco](#limpieza-de-disco)
5. [Configuración de Docker Desktop](#configuración-de-docker-desktop)
6. [Scripts Automatizados](#scripts-automatizados)
7. [Mejores Prácticas](#mejores-prácticas)
8. [Referencia Rápida](#referencia-rápida)

---

## 🔴 Resumen del Problema

### Qué Pasó

En enero 2026, el archivo `Docker.raw` creció hasta **926GB** (100% del disco), causando fallos totales.

### Por Qué Sucedió

Docker.raw es un archivo virtual que crece automáticamente sin límite hasta:

| Causa                  | Tamaño      | %        |
| ---------------------- | ----------- | -------- |
| Build Cache acumulado  | ~200-300 GB | 25-38%   |
| Imágenes no usadas     | ~250-350 GB | 32-44%   |
| Contenedores detenidos | ~100-150 GB | 12-19%   |
| Volúmenes de datos     | ~50-100 GB  | 6-12%    |
| Logs de contenedores   | ~10-20 GB   | 1-2%     |
| **TOTAL**              | **~792 GB** | **100%** |

### Solución Aplicada

- ✅ Eliminado `Docker.raw` (liberados 792 GB)
- ✅ Docker reconstruido desde cero
- ✅ Este documento para prevenir recurrencia

---

## 🎯 Límites de Seguridad

### Límite Máximo Recomendado

```
Disco Total:        926 GB
Límite Seguro (60%): 556 GB
Docker Máximo:       40-50 GB
```

### Cálculo de Espacio para OKLA

```
Imágenes Docker:    10-20 GB
Volúmenes (DBs):     2-5 GB
Build Cache:         5-10 GB
Sistema Docker:      2-3 GB
─────────────────────────────
TOTAL ESPERADO:     19-38 GB
MÁXIMO PERMITIDO:   50 GB
```

**Nota:** Si Docker excede 50 GB, ejecuta limpieza inmediatamente.

---

## 📊 Monitoreo Regular

### 1. Verificar Espacio de Disco (PRIMERO)

```bash
# Ver espacio total del disco
df -h /

# Ver espacio en home
df -h ~
```

**Alarmas:**

- ⚠️ **>80% usado:** Ejecutar limpieza
- 🔴 **>90% usado:** Limpieza urgente + investigar
- 🚨 **>95% usado:** Sistema podría fallar

### 2. Verificar Uso de Docker

```bash
# Ver uso detallado de Docker
docker system df

# Ver contenedores activos
docker ps

# Ver todos los contenedores (incluso parados)
docker ps -a

# Ver imágenes descargadas
docker images

# Ver volúmenes
docker volume ls
```

**Salida esperada de `docker system df`:**

```
TYPE            TOTAL     ACTIVE    SIZE      RECLAIMABLE
Images          25        8         18GB      5GB
Containers      15        5         2GB       1GB
Local Volumes   10        3         3GB       500MB
Build Cache     -         -         8GB       3GB
```

### 3. Monitoreo Automático (Mensual)

Ejecuta esto el **1er viernes de cada mes**:

```bash
# Crear recordatorio en Mac
cat > ~/docker-monitor-reminder.txt << 'EOF'
PRIMER VIERNES DE CADA MES:
1. docker system df
2. Si SIZE > 30GB → ejecutar limpieza
3. Registrar tamaño en log
EOF
```

---

## 🧹 Limpieza de Disco

### Nivel 1: Limpieza Segura (Recomendada)

**Elimina:** Imágenes sin usar + contenedores parados + volúmenes huérfanos

```bash
docker system prune -a --volumes
```

**Efectos:**

- ✅ Seguro ejecutar regularmente
- ✅ No afecta contenedores en ejecución
- 📊 Libera: ~5-15 GB típicamente

**Ejemplo:**

```bash
$ docker system prune -a --volumes

WARNING! This will remove:
  - all stopped containers
  - all networks not used by at least one container
  - all dangling images
  - all dangling build cache
  - all volumes not used by at least one container

Are you sure you want to continue? [y/N] y
```

### Nivel 2: Limpieza Forzada

**Elimina TODO, incluso imágenes activas**

```bash
docker system prune -a --volumes --force
```

**⚠️ Cuidado:** Fuerza limpieza sin preguntar. Usa solo si necesario.

### Nivel 3: Limpieza Manual Selectiva

#### Eliminar contenedor específico

```bash
docker rm <container_id_o_nombre>
```

#### Eliminar imagen específica

```bash
docker rmi <image_id_o_nombre>
```

#### Eliminar volumen específico

```bash
docker volume rm <volume_name>
```

#### Limpiar solo build cache

```bash
docker builder prune -a
```

#### Ver tamaño de cada imagen

```bash
docker images --format "{{.Repository}}:{{.Tag}}\t{{.Size}}"
```

---

## ⚙️ Configuración de Docker Desktop

### Establecer Límite Máximo (40 GB)

**Pasos:**

1. **Abre Docker Desktop**
2. **Settings** (engranaje superior derecho)
3. **Resources** (menú izquierdo)
4. **Disk image size:** Cambia a `40 GB`
5. **Swap:** `1 GB`
6. **Apply & Restart**

**Resultado:**

Docker.raw nunca excederá 40 GB. Si intenta crecer más:

- ⚠️ Build falla
- 💾 Necesita limpieza

### Configuración Adicional (Recomendada)

**Resources → Advanced:**

- **CPUs:** 4 (o máximo disponible)
- **Memory:** 8 GB (de 16 disponibles)
- **Swap:** 1 GB
- **Disk image size:** 40 GB

**File Sharing:**

- Include: `/Users/gregorymoreno/Developer/`

---

## 🤖 Scripts Automatizados

### Script 1: Monitor Mensual

```bash
#!/bin/bash
# docker-monitor.sh
# Ejecutar: bash docker-monitor.sh

echo "=== MONITOR DE DOCKER ==="
echo ""
echo "Espacio del disco:"
df -h / | tail -1

echo ""
echo "Uso de Docker:"
docker system df

echo ""
echo "Contenedores activos:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Size}}"

echo ""
echo "Imágenes:"
docker images --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}"
```

### Script 2: Limpieza Automática

```bash
#!/bin/bash
# docker-auto-clean.sh
# Ejecutar: bash docker-auto-clean.sh

DISK_USAGE=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')

echo "Uso de disco: $DISK_USAGE%"

if [ "$DISK_USAGE" -gt 80 ]; then
    echo "⚠️  Disco > 80%, ejecutando limpieza..."
    docker system prune -a --volumes --force
    echo "✅ Limpieza completada"
else
    echo "✅ Disco en buen estado"
fi
```

### Script 3: Limpieza Programada (Cron)

```bash
# Agregar a crontab (ejecutar primer viernes de cada mes a las 3 AM)
# crontab -e

# Primer viernes de cada mes a las 3:00 AM
0 3 1-7 * * ([ $(date +\%u) -eq 5 ] && /Users/gregorymoreno/scripts/docker-auto-clean.sh) >> /tmp/docker-cleanup.log 2>&1
```

---

## ✅ Mejores Prácticas

### 1. Después de Cada Sesión de Desarrollo

```bash
# Detener contenedores
docker-compose down

# (OPCIONAL) Limpiar si fue sesión pesada
docker system prune --volumes
```

### 2. Después de Cada Deploy a Producción

```bash
# Actualizar imágenes
docker pull <image>

# Eliminar imágenes antiguas
docker image prune -a --filter "until=720h"
```

### 3. Antes de Hacer Build de Múltiples Servicios

```bash
# Asegurar espacio disponible
docker system prune -a --volumes

# Verificar disponibilidad
docker system df
```

### 4. Si Docker Falla por Falta de Espacio

```bash
# Reiniciar Docker
osascript -e 'quit app "Docker"'
sleep 5
open -a Docker

# Esperar a que levante
sleep 30

# Ejecutar limpieza agresiva
docker system prune -a --volumes --force
```

### 5. Desarrollar sin Acumular Cache

```bash
# Usar --no-cache en builds críticos
docker-compose build --no-cache

# Usar --pull para imágenes base nuevas
docker-compose build --pull
```

---

## 📋 Referencia Rápida

### Comandos Esenciales

| Tarea                    | Comando                                                          |
| ------------------------ | ---------------------------------------------------------------- |
| Ver uso disco            | `df -h /`                                                        |
| Ver uso Docker           | `docker system df`                                               |
| Limpieza segura          | `docker system prune -a --volumes`                               |
| Limpieza forzada         | `docker system prune -a --volumes --force`                       |
| Ver contenedores parados | `docker ps -a`                                                   |
| Eliminar contenedor      | `docker rm <container>`                                          |
| Eliminar imagen          | `docker rmi <image>`                                             |
| Ver tamaño imágenes      | `docker images --format "{{.Repository}}:{{.Tag}}\t{{.Size}}"`   |
| Limpiar build cache      | `docker builder prune -a`                                        |
| Reiniciar Docker         | `osascript -e 'quit app "Docker"' && sleep 30 && open -a Docker` |

### Checklist Semanal

```bash
# Ejecutar cada lunes
docker system df
# Si algo > 15GB, ejecutar:
docker system prune -a --volumes
```

### Checklist Mensual

```bash
# Ejecutar primer viernes de cada mes
docker system df                    # Ver estado
docker image ls                     # Ver imágenes
docker ps -a                        # Ver contenedores
# Si total > 30GB:
docker system prune -a --volumes    # Limpiar
```

---

## 🆘 Troubleshooting

### Error: "No space left on device"

```bash
# 1. Verificar espacio
df -h /

# 2. Ver qué ocupa espacio
docker system df

# 3. Limpiar agresivamente
docker system prune -a --volumes --force

# 4. Reiniciar Docker
osascript -e 'quit app "Docker"'
sleep 30
open -a Docker
```

### Docker no inicia después de limpieza

```bash
# Forzar stop
osascript -e 'quit app "Docker"'

# Esperar
sleep 10

# Reiniciar
open -a Docker

# Esperar a que levante completamente
sleep 60

# Verificar
docker ps
```

### Build falla por "disk full"

```bash
# 1. Parar compose
docker-compose down

# 2. Limpiar todo
docker system prune -a --volumes --force

# 3. Limpiar cache
docker builder prune -a

# 4. Reiniciar Docker
osascript -e 'quit app "Docker"'
sleep 30
open -a Docker

# 5. Reintentar build
docker-compose build
```

---

## 📊 Historial de Incidentes

### Enero 18, 2026

**Problema:** Disco llegó a 100% (926 GB used)

**Causa:** Docker.raw sin límite + acumulación de:

- Build cache: ~250 GB
- Imágenes no usadas: ~300 GB
- Contenedores parados: ~150 GB
- Volúmenes: ~80 GB

**Solución:** Eliminar Docker.raw, reconstruir, documentar procesos

**Prevención:** Este documento + scripts + límite de 40 GB

---

## 📞 Contacto y Referencias

- **Documentación Docker:** https://docs.docker.com/
- **Docker Disk Management:** https://docs.docker.com/config/pruning/
- **Troubleshooting:** https://docs.docker.com/desktop/troubleshoot/

**Nota:** Este documento debe revisarse trimestralmente y actualizarse según cambios en la infraestructura.
