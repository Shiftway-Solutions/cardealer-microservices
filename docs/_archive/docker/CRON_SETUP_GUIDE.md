# 🔧 Configuración de Automatización con Cron

**Última actualización:** Enero 8, 2026

---

## 📅 Programar Verificaciones Automáticas

Este documento explica cómo configurar trabajos automáticos en macOS para monitorear y limpiar Docker.

---

## 🚀 Opción 1: Verificación Semanal (Recomendada)

### Paso 1: Crear archivo de cron

```bash
# Editar el crontab del usuario
crontab -e

# Se abrirá un editor de texto (vim, nano, etc.)
```

### Paso 2: Agregar línea de verificación semanal

```bash
# Cada lunes a las 8:00 AM
0 8 * * 1 /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-monitor.sh >> /tmp/docker-monitor.log 2>&1

# Cada domingo a las 9:00 PM
0 21 * * 0 /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-auto-clean.sh >> /tmp/docker-clean.log 2>&1
```

### Paso 3: Guardar y salir

```bash
# Si usas vim:
# 1. Presiona ESC
# 2. Escribe :wq
# 3. Presiona Enter

# Si usas nano:
# 1. Presiona Ctrl+O (guardar)
# 2. Presiona Enter
# 3. Presiona Ctrl+X (salir)
```

### Paso 4: Verificar que se guardó

```bash
crontab -l
```

Deberías ver tus dos líneas nuevas.

---

## 📊 Opción 2: Verificación Automática Diaria

Para monitoreo más agresivo:

```bash
crontab -e

# Agregar estas líneas:

# Verificación diaria a las 8:00 AM
0 8 * * * /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-monitor.sh >> /tmp/docker-monitor.log 2>&1

# Limpieza automática cada 2 semanas (primer y tercer martes)
0 22 8,22 * * /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-auto-clean.sh >> /tmp/docker-clean.log 2>&1
```

---

## 🎯 Opción 3: Limpieza Agresiva Mensual

```bash
crontab -e

# Primer domingo del mes a las 11:00 PM
0 23 1-7 * 0 /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-auto-clean.sh >> /tmp/docker-clean.log 2>&1
```

---

## 📋 Sintaxis de Cron Explicada

```
┌───────────── minuto (0 - 59)
│ ┌───────────── hora (0 - 23)
│ │ ┌───────────── día del mes (1 - 31)
│ │ │ ┌───────────── mes (1 - 12)
│ │ │ │ ┌───────────── día de la semana (0 - 6) (0 = domingo)
│ │ │ │ │
│ │ │ │ │
* * * * * comando_a_ejecutar
```

### Ejemplos Comunes

| Expresión      | Descripción                     |
| -------------- | ------------------------------- |
| `0 8 * * 1`    | Lunes a las 8:00 AM             |
| `0 21 * * 0`   | Domingo a las 9:00 PM           |
| `0 8 * * *`    | Todos los días a las 8:00 AM    |
| `0 */4 * * *`  | Cada 4 horas (0, 4, 8, 12, etc) |
| `0 22 1 * *`   | Primer día del mes a las 10 PM  |
| `30 2 * * 1-5` | Lunes-Viernes a las 2:30 AM     |

---

## 🔍 Verificar Logs de Cron

Después de ejecutar, verifica los logs:

```bash
# Ver logs de monitor
cat /tmp/docker-monitor.log

# Ver logs de limpieza
cat /tmp/docker-clean.log

# En tiempo real
tail -f /tmp/docker-monitor.log
tail -f /tmp/docker-clean.log
```

---

## ⚠️ Troubleshooting

### Cron no ejecuta el script

**Problema:** El script no se ejecuta

**Soluciones:**

1. **Hacer script ejecutable:**

```bash
chmod +x /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-monitor.sh
chmod +x /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-auto-clean.sh
```

2. **Verificar permisos:**

```bash
ls -la /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/
```

Deberían tener `-rwxr-xr-x` (755)

3. **Verificar ruta completa:**

Asegúrate que la ruta en crontab sea ABSOLUTA (no relativa)

4. **Verificar que bash está disponible:**

```bash
which bash
```

Debe retornar `/bin/bash` o similar

### Docker no está disponible en cron

**Problema:** El script dice "Docker no está en ejecución"

**Solución:** Los jobs de cron en macOS a veces no pueden acceder a Docker daemon.

```bash
# Agregar esto ANTES de la línea de Docker en el script:

export PATH=/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin
source ~/.bash_profile

# O ejecutar el script desde una terminal:
```

### Logs vacíos

**Problema:** `/tmp/docker-monitor.log` está vacío

**Solución:**

1. Verifica que el directorio `/tmp` existe
2. Verifica permisos de escritura: `ls -la /tmp | grep -E "^d.*w"`
3. Ejecuta el script manualmente:

```bash
bash /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-monitor.sh
```

---

## 📱 Notificaciones en macOS

Para recibir notificaciones cuando se ejecute la limpieza:

```bash
crontab -e

# Agregar esto:
0 21 * * 0 /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-auto-clean.sh >> /tmp/docker-clean.log 2>&1 && osascript -e 'display notification "Docker cleanup completed" with title "Docker Monitor"'
```

---

## 🛠️ Herramientas Alternativas

### Opción 1: LaunchAgent de macOS (Recomendado)

Más confiable que cron en macOS:

```bash
cat > ~/Library/LaunchAgents/com.okla.docker-monitor.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.okla.docker-monitor</string>
    <key>ProgramArguments</key>
    <array>
        <string>/bin/bash</string>
        <string>/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/scripts/docker-monitor.sh</string>
    </array>
    <key>StartCalendarInterval</key>
    <dict>
        <key>Hour</key>
        <integer>8</integer>
        <key>Minute</key>
        <integer>0</integer>
        <key>Weekday</key>
        <integer>1</integer>
    </dict>
    <key>StandardOutPath</key>
    <string>/tmp/docker-monitor.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/docker-monitor-error.log</string>
</dict>
</plist>
EOF

# Instalar y activar
launchctl load ~/Library/LaunchAgents/com.okla.docker-monitor.plist

# Verificar que está activo
launchctl list | grep docker-monitor
```

### Opción 2: GitHub Actions (Si usas CI/CD)

En `.github/workflows/docker-monitor.yml`:

```yaml
name: Docker Monitor

on:
  schedule:
    - cron: "0 8 * * 1" # Lunes a las 8:00 AM UTC

jobs:
  monitor:
    runs-on: macos-latest
    steps:
      - name: Monitor Docker
        run: |
          docker system df
          docker container ls
          docker image ls
```

---

## 📅 Plan de Monitoreo Recomendado

### Semanal (Lunes 8:00 AM)

```bash
0 8 * * 1 /path/to/docker-monitor.sh >> /tmp/docker-monitor.log 2>&1
```

**Qué hace:**

- Verifica uso de disco
- Lista contenedores activos/parados
- Muestra top 10 imágenes por tamaño
- Recomienda acciones si es necesario

### Mensual (Primer domingo 9:00 PM)

```bash
0 21 1-7 * 0 /path/to/docker-auto-clean.sh >> /tmp/docker-clean.log 2>&1
```

**Qué hace:**

- Limpia contenedores parados
- Elimina imágenes no usadas
- Limpia volúmenes no usados
- Limpia cache de build

---

## ✅ Checklist de Configuración

- [ ] Scripts descargados en `/scripts/`
- [ ] Scripts con permisos 755 (`chmod +x`)
- [ ] Crontab editado con `crontab -e`
- [ ] Líneas de cron agregadas
- [ ] Crontab guardado correctamente
- [ ] Logs configurados en `/tmp/`
- [ ] Verificación manual ejecutada
- [ ] Logs revisados para errores

---

## 🔗 Referencias

- DISK_MANAGEMENT.md - Guía completa de gestión de disco
- docker-monitor.sh - Script de monitoreo
- docker-auto-clean.sh - Script de limpieza automática

---

**¡Listo!** Ahora Docker se monitoreará y limpiará automáticamente.
