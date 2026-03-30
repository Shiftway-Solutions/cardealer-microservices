# Copilot Watchdog — Arquitectura CDP

Servicio de supervisión automática del agente GitHub Copilot en VS Code.  
Conecta directamente al proceso de VS Code via **Chrome DevTools Protocol (CDP)** — sin OCR, sin capturas de pantalla, sin APIs de pago externas.

---

## Por qué CDP y no OCR/Vision

| Método anterior                       | Método actual (CDP)                       |
| ------------------------------------- | ----------------------------------------- |
| Screenshot → OCR → interpretar texto  | Acceso directo al DOM de VS Code          |
| Requiere Claude/OpenAI Vision API ($) | Sin APIs externas — costo cero            |
| AppleScript para clicks por pantalla  | Playwright manipula el DOM directamente   |
| Frágil si cambia el layout de VS Code | Selectores CSS del Workbench (estables)   |
| Permisos de Accesibilidad macOS       | Solo necesita el puerto 9222 abierto      |

---

## El concepto fundamental: VS Code es Chromium

VS Code está construido sobre **Electron**, que es Chromium embebido con Node.js. Chrome expone el **Chrome DevTools Protocol (CDP)**: un servidor HTTP/WebSocket que permite controlar el navegador desde afuera — hacer clicks, inyectar JS, leer el DOM, enviar teclado, etc.

El mismo protocolo que usas para controlar Chrome con Playwright funciona **exactamente igual** para controlar VS Code:

```
Bot de Chrome:    Chrome.app  --remote-debugging-port=9222
                       ↑
                  playwright.chromium.connect_over_cdp("http://127.0.0.1:9222")

Watchdog OKLA:    Code.app    --remote-debugging-port=9222
                       ↑
                  playwright.chromium.connect_over_cdp("http://127.0.0.1:9222")
                  (mismo código, distinto proceso)
```

---

## Archivos del sistema

| Archivo | Descripción |
|---|---|
| `.prompts/vscode_copilot_monitor.py` | Script principal del watchdog |
| `.prompts/AGENT_LOOP_PROMPT.md` | Prompt completo que se envía al abrir un nuevo chat |
| `.prompts/prompt_1.md` | Semáforo de actividad — el watchdog monitorea su `mtime` |
| `.prompts/.monitor_state.json` | Estado persistente entre ciclos (contadores, timestamps) |
| `infra/code-watch.sh` | Launcher: abre VS Code con CDP + inicia el watchdog |
| `.github/copilot-monitor.log` | Log de todas las acciones del watchdog |

---

## Arranque

```
./infra/code-watch.sh
        │
        ├─ 1. Lanza VS Code con --remote-debugging-port=9222
        │       VS Code abre un servidor en http://127.0.0.1:9222
        │       que expone el DOM de todas sus ventanas internas
        │
        └─ 2. Lanza vscode_copilot_monitor.py en background
                playwright.chromium.connect_over_cdp("http://127.0.0.1:9222")
                busca la page del workbench por URL ("workbench", "vscode-file"...)
                → tiene acceso completo al DOM de VS Code
```

Modos del launcher:

```bash
./infra/code-watch.sh          # VS Code con CDP + watchdog (default)
./infra/code-watch.sh --code   # solo VS Code con CDP
./infra/code-watch.sh --watch  # solo watchdog (VS Code ya corriendo con CDP)
```

---

## Flujo de trabajo completo

```
┌─────────────────────────────────────────────────────────────────────┐
│  vscode_copilot_monitor.py — CICLO CADA 30 SEGUNDOS                │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
               ┌───────────────▼─────────────────┐
               │   ¿VS Code está corriendo?       │
               └───────┬─────────────────┬────────┘
                    NO │                 │ SÍ
                       ▼                 ▼
               ┌──────────────┐  ┌───────────────────────┐
               │  Lanzar VS   │  │  ¿CDP en puerto 9222? │
               │  Code normal │  └──────┬──────────┬──────┘
               └──────────────┘      NO │          │ SÍ
                                        ▼          ▼
                               ┌──────────────┐  ┌──────────────────────────┐
                               │ Avisar user: │  │ ¿Cambió prompt_1.md?     │
                               │ "ejecuta     │  │ ¿Cambió algún archivo?   │
                               │  code-watch" │  └──────┬──────────┬─────────┘
                               └──────────────┘      SÍ │          │ NO
                                                         ▼          ▼
                                                  ┌──────────┐ ┌─────────────────────┐
                                                  │ Copilot  │ │ ¿Cuánto sin cambios?│
                                                  │ activo   │ └──┬──────┬──────┬────┘
                                                  │ OK       │ <5m│  >5m │ >10m │>20m
                                                  └──────────┘    ▼      ▼      ▼
                                                             esperar CAPA1 CAPA2 CAPA3
```

### Las 3 capas de intervención

| Capa | Umbral | Acción |
|------|--------|--------|
| **CAPA 1** | > 5 min sin actividad | Envía `"continuar"` al chat activo (hasta 3 veces) |
| **CAPA 2** | > 10 min sin actividad | Abre nuevo chat + envía `AGENT_LOOP_PROMPT.md` completo |
| **CAPA 3** | > 20 min sin actividad | Abre otro nuevo chat + resetea contadores |

---

## Cómo detecta que Copilot está trabajando

El watchdog **no lee el contenido del chat** — monitorea si hay actividad en el sistema de archivos:

**Señal primaria — `prompt_1.md`:**  
El agente escribe `READ` al final de `prompt_1.md` cuando termina un ciclo. El watchdog compara el `mtime` del archivo en cada ciclo. Si cambió → Copilot está vivo.

**Señal secundaria — workspace:**  
Escanea `mtime` de todos los archivos del proyecto (excluyendo `.git`, `node_modules`, `bin`, `obj`, `.next`, etc.). Si Copilot está editando código, los archivos cambian.

```python
# Pseudocódigo del detector
prompt_mtime = stat("prompt_1.md").mtime
if prompt_mtime > state.last_prompt_mtime:
    # Copilot activo, resetear timer

latest_mtime = max(stat(f).mtime for f in workspace_files)
if latest_mtime > state.last_known_mtime:
    # Copilot activo, resetear timer

# Si ninguno cambió en > 5 minutos → intervenir
```

---

## Cómo envía mensajes al chat de Copilot

Una vez conectado via CDP, Playwright tiene acceso al DOM de VS Code:

```python
# 1. Foca el panel de Copilot Chat con el keybinding nativo
await page.keyboard.press("Meta+Control+i")    # Cmd+Ctrl+I

# 2. Click en el input del chat por selector CSS del Workbench
await page.locator(".interactive-input-editor").first.click()

# 3. Pega el mensaje via clipboard (pbcopy + Cmd+V)
#    Razón: keyboard.type() es lento para textos largos y puede perder caracteres
subprocess.run(["pbcopy"], input=message.encode("utf-8"))
await page.keyboard.press("Meta+v")

# 4. Envía con Enter
await page.keyboard.press("Enter")
```

Para abrir un **nuevo chat**:

```python
await page.keyboard.press("Meta+Shift+p")   # Command Palette
await page.keyboard.type("Chat: New Chat")
await page.keyboard.press("Enter")
# → luego envía AGENT_LOOP_PROMPT.md completo
```

---

## Relación con OpenClaw

El watchdog y OpenClaw son **procesos completamente independientes** que comparten solo el archivo `prompt_1.md` como semáforo:

```
┌─────────────────────────────┐        ┌────────────────────────────────────┐
│  openclaw_loop.py           │        │  vscode_copilot_monitor.py         │
│  "El agente que trabaja"    │        │  "El guardián que lo mantiene vivo" │
├─────────────────────────────┤        ├────────────────────────────────────┤
│ Lee tareas de prompt_1.md   │        │ Lee prompt_1.md (solo mtime)       │
│ Hace llamadas a APIs        │ shared │ Cero llamadas a APIs externas      │
│ Ejecuta herramientas        │  file  │ Monitorea archivos del workspace   │
│ Escribe READ al final       │        │ Interviene si Copilot se para      │
│                             │        │ Conecta a VS Code via CDP          │
└─────────────────────────────┘        └────────────────────────────────────┘

                prompt_1.md
                ┌───────────────────────────────┐
                │  [ ] Tarea 1: ...             │
                │  [ ] Tarea 2: ...             │
                │  READ  ← Copilot escribió     │  → watchdog detecta → OK
                └───────────────────────────────┘
```

OpenClaw **no necesita estar corriendo** para que el watchdog funcione. El watchdog vigila Copilot en VS Code directamente.

---

## Estado persistente

El watchdog guarda su estado en `.prompts/.monitor_state.json` entre reinicios:

```json
{
  "last_file_change_ts": 1774608933.0,
  "last_prompt_mtime":   1774608000.0,
  "last_known_mtime":    1774608900.0,
  "last_continue_ts":    0.0,
  "last_new_chat_ts":    0.0,
  "last_cdp_warn_ts":    0.0,
  "continue_count":      0,
  "new_chat_count":      0,
  "stall_start_ts":      1774608933.0
}
```

---

## Comandos disponibles

```bash
# Estado actual (sin arrancar el loop)
python3 .prompts/vscode_copilot_monitor.py --status

# Loop infinito (default: cada 30s)
python3 .prompts/vscode_copilot_monitor.py

# Loop con intervalo personalizado
python3 .prompts/vscode_copilot_monitor.py --interval 60

# Un solo ciclo y salir
python3 .prompts/vscode_copilot_monitor.py --once

# Debug verbose
python3 .prompts/vscode_copilot_monitor.py --once --debug

# Forzar envío de "continuar" ahora
python3 .prompts/vscode_copilot_monitor.py --continue

# Abrir nuevo chat + enviar AGENT_LOOP_PROMPT ahora
python3 .prompts/vscode_copilot_monitor.py --new-chat
```

---

## Requisitos

```bash
# Python deps (ya en .venv)
pip install playwright
python -m playwright install chromium

# VS Code debe arrancar con el flag CDP
# El launcher lo hace automáticamente:
./infra/code-watch.sh

# O manualmente:
/Applications/Visual\ Studio\ Code.app/Contents/Resources/app/bin/code \
    --remote-debugging-port=9222 \
    --remote-debugging-address=127.0.0.1 \
    cardealer.code-workspace
```

---

## Solución de problemas

### `CDP no disponible` — watchdog no puede conectarse

VS Code está corriendo sin el flag de debugging. Ciérralo y usa el launcher:

```bash
./infra/code-watch.sh
```

### Ver logs en tiempo real

```bash
tail -f .github/copilot-monitor.log
```

### Resetear el estado (si los contadores están mal)

```bash
python3 -c "
import json, time
from pathlib import Path
s = {'last_file_change_ts': time.time(), 'last_prompt_mtime': 0.0,
     'last_known_mtime': 0.0, 'last_continue_ts': 0.0, 'last_new_chat_ts': 0.0,
     'last_restart_ts': 0.0, 'last_cdp_warn_ts': 0.0, 'continue_count': 0,
     'new_chat_count': 0, 'stall_start_ts': time.time()}
Path('.prompts/.monitor_state.json').write_text(json.dumps(s, indent=2))
print('Estado reseteado')
"
```
