# 🐛 Docker Troubleshooting Guide

**Última actualización:** Enero 8, 2026  
**Versión:** 1.0

---

## 📋 Tabla de Contenidos

- [🚨 Problemas Críticos](#-problemas-críticos)
- [⚠️ Problemas Comunes](#️-problemas-comunes)
- [🔧 Problemas de Scripts](#-problemas-de-scripts)
- [🌐 Problemas de Red](#-problemas-de-red)
- [📊 Problemas de Performance](#-problemas-de-performance)
- [🆘 Última Opción: Reset Completo](#-última-opción-reset-completo)

---

## 🚨 Problemas Críticos

### 1. Disco al 100% - EMERGENCIA

**Síntomas:**

```
Disk full
No space left on device
Cannot write file
```

**Solución Rápida (5 minutos):**

```bash
# Paso 1: Detener Docker INMEDIATAMENTE
osascript -e 'quit app "Docker"'
sleep 30

# Paso 2: Verificar espacio
df -h /

# Paso 3: Si aún lleno, buscar archivos grandes
find ~ -type f -size +1G 2>/dev/null | head -20

# Paso 4: Limpiar archivos temporales
rm -rf ~/Downloads/*        # Descargas antiguas
rm -rf ~/.cache/*           # Cache
rm -rf /tmp/*               # Temporales

# Paso 5: Reiniciar Docker
open -a Docker
sleep 60

# Paso 6: Verificar
docker ps
```

**Si aún sigue lleno (Plan B):**

```bash
# Opción A: Limpiar Docker.raw completamente
# ⚠️ PELIGRO: Esto borra TODO los contenedores y volúmenes locales

osascript -e 'quit app "Docker"'
sleep 30

# Localizar Docker.raw
ls -lh ~/Library/Containers/com.docker.docker/Data/vms/0/data/Docker.raw

# Borrarlo (libera espacio inmediatamente)
rm ~/Library/Containers/com.docker.docker/Data/vms/0/data/Docker.raw

# Reiniciar Docker (reconstruirá Docker.raw)
open -a Docker
sleep 120

docker ps  # Verificar
```

**Si sigue no funcionando (Plan C):**

```bash
# Desinstalar y reinstalar Docker completamente
osascript -e 'quit app "Docker"'
sleep 10

# Borrar archivos de Docker
rm -rf ~/Library/Containers/com.docker.docker
rm -rf ~/Library/Application\ Support/Docker
rm -rf ~/.docker

# Reinstalar Docker (descargar desde docker.com)
# Luego: open /Applications/Docker.app
```

---

### 2. Docker no inicia

**Síntomas:**

```
Docker.app won't start
Cannot connect to Docker daemon
docker: Cannot connect to the Docker daemon
```

**Paso 1: Intentar reinicio simple**

```bash
# Cerrar Docker
osascript -e 'quit app "Docker"'
sleep 10

# Abrir de nuevo
open -a Docker

# Esperar a que inicie
sleep 60

# Verificar
docker ps
```

**Paso 2: Limpiar procesos colgados**

```bash
# Ver procesos Docker
ps aux | grep -i docker

# Matar procesos (si hay)
killall Docker
killall com.docker.backend
killall docker

# Esperar y reiniciar
sleep 10
open -a Docker
sleep 60
```

**Paso 3: Resetear Docker daemon**

```bash
# En macOS
osascript -e 'quit app "Docker"'
sleep 30

# Limpiar sockets
rm -f ~/Library/Containers/com.docker.docker/Data/socket/docker.sock

# Reiniciar
open -a Docker
sleep 120
docker ps
```

**Paso 4: Reinstalar (última opción)**

```bash
# Ver opción "Plan C" en "Disco al 100%"
```

---

### 3. Contenedores no inician después de limpieza

**Síntomas:**

```
Failed to create container
Error response from daemon
Container exited with code 127
```

**Solución:**

```bash
# Paso 1: Ver logs del contenedor
docker logs {container_name}

# Paso 2: Verificar que imagen existe
docker images | grep {image_name}

# Paso 3: Si imagen falta, reconstruir
docker build -t {image_name}:{tag} {dockerfile_path}

# Paso 4: Reintentar
docker-compose up -d

# Paso 5: Verificar logs nuevamente
docker-compose logs -f {service_name}
```

---

## ⚠️ Problemas Comunes

### 1. Docker.raw muy grande (> 30 GB)

**Síntomas:**

```
Disk usage increasing
Docker system df shows large unused data
~/Library/.../Docker.raw is huge
```

**Solución:**

```bash
# Paso 1: Limpiar agresivamente
docker system prune -a --volumes -f
docker builder prune -a -f

# Paso 2: Reiniciar Docker
osascript -e 'quit app "Docker"'
sleep 30
open -a Docker
sleep 60

# Paso 3: Verificar tamaño (debería ser < 5 GB después)
du -sh ~/Library/Containers/com.docker.docker/Data/vms/0/data/Docker.raw

# Paso 4: Si aún grande, opción nuclear
# Settings → Resources → Disk image → "Reset to initial value"
# ⚠️ Esto borra TODO (nuevamente, Plan B en sección anterior)
```

---

### 2. Containers parados ocupan espacio

**Síntomas:**

```
docker ps -a shows many exited containers
Disk usage high despite unused containers
```

**Solución:**

```bash
# Ver containers parados
docker ps -a --filter "status=exited"

# Limpiarlos
docker container prune -f

# O borrar uno específico
docker rm {container_id}

# Verificar espacio
docker system df
```

---

### 3. Imágenes no usadas

**Síntomas:**

```
docker images shows many <none>:<none> images
Disk usage high
```

**Solución:**

```bash
# Ver imágenes no taggeadas
docker images -f "dangling=true"

# Limpiar
docker image prune -f

# O aggressive cleanup
docker image prune -a -f

# Verificar
docker images
```

---

### 4. Volúmenes fantasma

**Síntomas:**

```
docker volume ls shows many unused volumes
Disk space occupied by old projects
```

**Solución:**

```bash
# Ver volúmenes
docker volume ls

# Ver volúmenes no usados
docker volume ls -f "dangling=true"

# Limpiar
docker volume prune -f

# Específico
docker volume rm {volume_name}
```

---

### 5. Build cache muy grande

**Síntomas:**

```
First docker build es muy lento
Docker system df shows large cache
```

**Solución:**

```bash
# Ver cache
docker builder du --verbose

# Limpiar cache
docker builder prune -a -f

# Verificar
docker builder du
```

---

## 🔧 Problemas de Scripts

### 1. docker-monitor.sh no funciona

**Síntomas:**

```
bash: docker-monitor.sh: command not found
docker: Cannot connect to the Docker daemon
```

**Solución:**

```bash
# Paso 1: Verificar que el archivo existe
ls -la /scripts/docker-monitor.sh

# Paso 2: Hacer ejecutable
chmod +x /scripts/docker-monitor.sh

# Paso 3: Ejecutar con bash explícito
bash /scripts/docker-monitor.sh

# Paso 4: Si aún falla, verificar ruta absoluta
echo $HOME
# Asegúrate que la ruta en el script es correcta

# Paso 5: Verificar que Docker está corriendo
docker ps
```

---

### 2. docker-auto-clean.sh no limpia

**Síntomas:**

```
Script runs pero no limpia nada
Disco sigue lleno
```

**Solución:**

```bash
# Paso 1: Ejecutar manualmente
bash /scripts/docker-auto-clean.sh

# Paso 2: Ver logs
cat /tmp/docker-clean.log

# Paso 3: Si error de permisos
sudo bash /scripts/docker-auto-clean.sh

# Paso 4: Si Docker no responde
docker ps  # Debe funcionar

# Paso 5: Ejecutar limpieza manual
docker system prune -a --volumes -f
```

---

### 3. Cron job no ejecuta

**Síntomas:**

```
cron job no se ejecuta a la hora programada
/tmp/docker-monitor.log vacío
```

**Solución:**

```bash
# Paso 1: Verificar que cron está activo
crontab -l

# Paso 2: Si no hay nada, crear entry
crontab -e

# Paso 3: Agregar línea (ejemplo)
0 8 * * 1 /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-monitor.sh >> /tmp/docker-monitor.log 2>&1

# Paso 4: Guardar (vim: :wq, nano: Ctrl+O, Ctrl+X)

# Paso 5: Verificar que se guardó
crontab -l | grep docker-monitor

# Paso 6: Ver logs de cron
log stream --predicate 'process == "cron"' --level debug

# Paso 7: Ejecutar script manualmente
bash /scripts/docker-monitor.sh
```

**Nota:** En macOS, a veces cron necesita permisos especiales:

```bash
# Ver si cron está en Full Disk Access
System Preferences → Security & Privacy → Privacy → Full Disk Access
# Si no está: agregar /usr/sbin/cron

# O alternativa: usar LaunchAgent (ver CRON_SETUP_GUIDE.md)
```

---

### 4. Scripts retornan errores

**Síntomas:**

```
bash: line 1: docker: command not found
bash: line 2: df: command not found
```

**Solución:**

```bash
# Paso 1: Verificar que Docker está en PATH
which docker

# Si no retorna nada:
export PATH="/usr/local/bin:$PATH"

# Paso 2: Agregar a ~/.bash_profile
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.bash_profile
source ~/.bash_profile

# Paso 3: Verificar bash version
bash --version

# Paso 4: Si es zsh, agregar a ~/.zshrc también
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

---

## 🌐 Problemas de Red

### 1. No puedo acceder a contenedores

**Síntomas:**

```
curl: (7) Failed to connect
Connection refused
Cannot reach localhost:8080
```

**Solución:**

```bash
# Paso 1: Verificar que contenedor está corriendo
docker ps | grep {nombre}

# Paso 2: Ver IP del contenedor
docker inspect {container_id} | grep IPAddress

# Paso 3: Intentar conectar directamente
docker exec {container_id} curl localhost:8080

# Paso 4: Verificar puerto mapeado
docker ps  # Ver PORTS column

# Paso 5: Si puerto no está mapeado, recrear
docker-compose down
docker-compose up -d

# Paso 6: Verificar que puerto está libre
lsof -i :{puerto}  # Ej: lsof -i :8080

# Paso 7: Si puerto está ocupado por otro proceso
killall {process_name}
```

---

### 2. Docker no puede resolver DNS

**Síntomas:**

```
Failed to resolve 'database.okla.svc.cluster.local'
getaddrinfo: Name or service not known
```

**Solución:**

```bash
# Paso 1: Ver DNS del contenedor
docker exec {container_id} cat /etc/resolv.conf

# Paso 2: Verificar que docker network existe
docker network ls

# Paso 3: Verificar que servicio está en la network
docker inspect {network_id} | grep -A 20 "Containers"

# Paso 4: Reiniciar servicio
docker-compose restart {service}

# Paso 5: Si problema persiste, resetear network
docker network rm {network_name}
docker-compose up -d
```

---

## 📊 Problemas de Performance

### 1. Docker muy lento

**Síntomas:**

```
docker pull tarda horas
docker build es extremadamente lento
Docker Desktop usa 100% CPU
```

**Solución:**

```bash
# Paso 1: Verificar CPU/Memory allocation
# Settings → Resources → Aumentar CPU y Memory

# Paso 2: Limpiar cache de build
docker builder prune -a -f

# Paso 3: Resetear Docker
osascript -e 'quit app "Docker"'
sleep 30
open -a Docker
sleep 120

# Paso 4: Verificar actividad
docker stats

# Paso 5: Si contenedor usa mucho, investigar
docker logs {container_id}
docker exec {container_id} top -b -n 1

# Paso 6: Considerar aumentar specs de Docker
# Settings → Resources → CPU: 8, Memory: 8GB
```

---

### 2. Contenedor usa mucho CPU

**Síntomas:**

```
docker stats muestra > 100% CPU
Machine is very slow
```

**Solución:**

```bash
# Paso 1: Identificar contenedor culpable
docker stats

# Paso 2: Ver logs
docker logs -f {container_id}

# Paso 3: Entrar al contenedor
docker exec -it {container_id} /bin/bash

# Paso 4: Ver procesos
top -b -n 1
ps aux

# Paso 5: Si aplicación está en loop
docker stop {container_id}
docker rm {container_id}

# Paso 6: Revisar código o configuración
```

---

### 3. Contenedor usa mucho memoria

**Síntomas:**

```
docker stats muestra > 1GB memory
Out of memory errors
```

**Solución:**

```bash
# Paso 1: Ver memoria actual
docker stats {container_id}

# Paso 2: Establecer límite de memoria
docker run -m 512m {image}

# Paso 3: En docker-compose:
# services:
#   myservice:
#     deploy:
#       resources:
#         limits:
#           memory: 512M

# Paso 4: Si necesita más, escalar máquina
# Settings → Resources → aumentar Memory disponible

# Paso 5: Investigar memory leaks
docker logs {container_id}
```

---

## 🆘 Última Opción: Reset Completo

**⚠️ PELIGRO:** Esto borra TODO y reconstruye desde cero

```bash
# Paso 0: Backup (IMPORTANTE)
mkdir -p ~/docker-backup
docker exec {container_id} mysqldump -u root > ~/docker-backup/database.sql
docker volume inspect {volume_name}  # Anotar información importante

# Paso 1: Parar todo
docker-compose down -v  # -v borra volúmenes también
docker stop $(docker ps -aq)

# Paso 2: Borrar contenedores
docker rm $(docker ps -aq)

# Paso 3: Borrar imágenes (opcional)
docker rmi $(docker images -aq)

# Paso 4: Borrar volúmenes (opcional)
docker volume prune -f

# Paso 5: Borrar networks (opcional)
docker network prune -f

# Paso 6: Borrar builder cache
docker builder prune -a -f

# Paso 7: Cerrar Docker
osascript -e 'quit app "Docker"'
sleep 30

# Paso 8: Borrar Docker.raw completamente
rm ~/Library/Containers/com.docker.docker/Data/vms/0/data/Docker.raw

# Paso 9: Borrar config completamente (opcional)
rm -rf ~/Library/Containers/com.docker.docker
rm -rf ~/.docker

# Paso 10: Reiniciar Docker
open -a Docker
sleep 120

# Paso 11: Verificar
docker ps

# Paso 12: Reconstruir
docker-compose up --build -d
```

---

## 🔍 Comandos de Diagnóstico

### Ver estado completo

```bash
# Todo en uno
echo "=== DISK ===" && \
df -h / && \
echo "=== DOCKER ===" && \
docker system df --verbose && \
echo "=== CONTAINERS ===" && \
docker ps -a && \
echo "=== IMAGES ===" && \
docker images && \
echo "=== VOLUMES ===" && \
docker volume ls && \
echo "=== NETWORKS ===" && \
docker network ls
```

### Generar reporte de diagnóstico

```bash
# Crear archivo
cat > ~/docker-diagnostics.txt << 'EOF'
=== TIMESTAMP ===
$(date)

=== DISK ===
$(df -h /)

=== DOCKER STATS ===
$(docker system df --verbose)

=== CONTAINERS ===
$(docker ps -a)

=== TOP IMAGES ===
$(docker images --format "table {{.Repository}}\t{{.Size}}" | head -20)

=== DOCKER LOGS ===
$(docker logs $(docker ps -aq | head -1) 2>&1 | tail -50)

=== DOCKER DAEMON LOG ===
$(log show --predicate 'process == "Docker"' --level debug --last 1h 2>&1 | tail -50)
EOF

cat ~/docker-diagnostics.txt
```

---

## 📞 Cuándo Contactar Soporte

Contacta al equipo de DevOps si:

- ✉️ Disco > 85% y los scripts no lo resuelven
- ✉️ Docker no inicia después de intentar todas las soluciones
- ✉️ Necesitas recuperar datos perdidos
- ✉️ Los contenedores de producción fallan regularmente
- ✉️ Necesitas aumentar los límites de Docker

**Email:** devops@okla.com.do  
**Slack:** #docker-support  
**Urgente:** Llamar a Gregory Moreno

---

## ✅ Checklist de Troubleshooting

Antes de contactar soporte, asegúrate de:

- [ ] Ejecutado `docker-monitor.sh` para diagnóstico
- [ ] Intentado un reinicio simple de Docker
- [ ] Verificado logs: `docker logs {container}`
- [ ] Intentado `docker system prune -a --volumes -f`
- [ ] Verificado que disco tiene espacio > 5GB
- [ ] Recopilado error messages exactos
- [ ] Generado reporte de diagnóstico

---

## 🔗 Referencias

- [DISK_MANAGEMENT.md](DISK_MANAGEMENT.md) - Guía de gestión
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Comandos rápidos
- [CRON_SETUP_GUIDE.md](CRON_SETUP_GUIDE.md) - Automatización
- [README.md](README.md) - Documentación principal

---

**Última actualización:** Enero 8, 2026  
**Versión:** 1.0  
**Próxima revisión:** Abril 2026
