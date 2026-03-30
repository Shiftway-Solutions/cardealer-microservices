# OKLA Agent System — Manual Técnico Completo

> Autor: CPSO Agent · Fecha: 2026-03-28  
> Ubicación: `.prompts/agent/`

---

## Tabla de Contenidos

1. [Arquitectura General](#1-arquitectura-general)
2. [Requisitos Previos](#2-requisitos-previos)
3. [Cómo se Lee el Chat de Copilot (CDP)](#3-cómo-se-lee-el-chat-de-copilot-cdp)
4. [Cómo se Cambia el Modelo del Chat (CDP Model Picker)](#4-cómo-se-cambia-el-modelo-del-chat-cdp-model-picker)
5. [Monitor Watchdog — vscode_copilot_monitor.py](#5-monitor-watchdog)
6. [Estructura de Archivos](#6-estructura-de-archivos)
7. [Guía de Uso Rápido](#7-guía-de-uso-rápido)
8. [Troubleshooting](#8-troubleshooting)
9. [Lecciones Aprendidas (Investigación)](#9-lecciones-aprendidas)

---

## 1. Arquitectura General

```
┌─────────────────────────────────────────────────────┐
│                     VS Code                          │
│  ┌──────────────────────────────────────────────┐   │
│  │   Copilot Chat Panel (Electron/Chromium)      │   │
│  │   ├── Chat messages (DOM)                     │   │
│  │   ├── Model Picker button (a[aria-label])     │   │
│  │   └── Input area                              │   │
│  └──────────────────────────────────────────────┘   │
│         ↕ CDP (Chrome DevTools Protocol)             │
│         puerto 9222                                  │
└─────────────────────────────────────────────────────┘
          ↕
┌─────────────────────────────────────────────────────┐
│  vscode_copilot_monitor.py (Python + Playwright)     │
│  ├── Lee el DOM del chat via CDP                     │
│  ├── Detecta errores (rate limit, overloaded, etc.)  │
│  ├── Cambia el modelo via CDP (trusted mouse clicks)  │
│  ├── Envía "continuar" o abre nuevo chat             │
│  └── Log fallback (lee Copilot Chat log files)       │
└─────────────────────────────────────────────────────┘
```

**Principio clave**: VS Code ES un browser Chromium (Electron). Al lanzarlo con `--remote-debugging-port=9222`, expone CDP. Playwright se conecta por CDP y puede leer/manipular el DOM como si fuera una web normal.

---

## 2. Requisitos Previos

### 2.1 Lanzar VS Code con CDP habilitado

```bash
# Opción A: Script automático
./infra/code-watch.sh

# Opción B: Manual
code --remote-debugging-port=9222 --remote-debugging-address=127.0.0.1 cardealer.code-workspace
```

**Verificar que CDP funciona:**

```bash
curl -s http://localhost:9222/json/version | python3 -m json.tool
# Debe retornar info de Electron/Chromium
```

### 2.2 Python Virtual Environment + Playwright

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install playwright
playwright install chromium
```

### 2.3 Verificar conectividad

```bash
.venv/bin/python3 .prompts/agent/vscode_chat_reader.py --debug
# Debe mostrar las páginas CDP disponibles y el contenido del chat
```

---

## 3. Cómo se Lee el Chat de Copilot (CDP)

### 3.1 El Descubrimiento

VS Code es Electron = Chromium. Al abrir CDP en el puerto 9222, Playwright puede conectarse:

```python
from playwright.async_api import async_playwright

async with async_playwright() as pw:
    browser = await pw.chromium.connect_over_cdp("http://localhost:9222")

    # Encontrar la página principal de VS Code (workbench)
    page = None
    for ctx in browser.contexts:
        for pg in ctx.pages:
            if "workbench" in pg.url:
                page = pg
                break
```

### 3.2 Selectores del Chat

El panel de chat de Copilot vive dentro del DOM de VS Code:

```python
# Todo el texto visible del chat
chat_text = await page.evaluate("""() => {
    const panel = document.querySelector('.interactive-session');
    return panel ? panel.innerText : '';
}""")
```

**Selectores clave descubiertos:**

| Selector                      | Qué es                                          |
| ----------------------------- | ----------------------------------------------- |
| `.interactive-session`        | Panel completo del chat                         |
| `.interactive-item-container` | Cada mensaje individual                         |
| `.chat-input-picker-label`    | Label del modelo activo                         |
| `a[aria-label^="Pick Model"]` | Botón del model picker                          |
| `.context-view [role="menu"]` | Dropdown del model picker (cuando está abierto) |
| `.monaco-list-row.action`     | Items individuales dentro del dropdown          |

### 3.3 Detección de Errores

El monitor lee el texto del chat y busca patrones:

```python
# Ejemplos de patrones detectados
patterns = {
    "rate_limited":   ["rate limit", "429", "too many requests", "quota exceeded"],
    "overloaded":     ["overloaded", "503", "service unavailable"],
    "hard_error":     ["unexpected error", "failed to connect", "ECONNREFUSED"],
    "tool_error":     ["tool validation", "MCP server"],
    "cancelled":      ["cancelled", "was cancelled"],
}
```

### 3.4 Script standalone: vscode_chat_reader.py

```bash
# Dump único del contenido del chat
.venv/bin/python3 .prompts/agent/vscode_chat_reader.py

# Modo watch — polling cada 5 segundos
.venv/bin/python3 .prompts/agent/vscode_chat_reader.py --watch

# Guardar snapshot a archivo
.venv/bin/python3 .prompts/agent/vscode_chat_reader.py --save
```

---

## 4. Cómo se Cambia el Modelo del Chat (CDP Model Picker)

### 4.1 El Problema

VS Code Copilot Chat **NO** tiene un setting público para cambiar el modelo en caliente. El setting `github.copilot.chat.languageModel` en `settings.json` es interno/undocumentado y **NO se aplica en tiempo real** al chat activo.

**El único método es hacer click en el UI model picker** (botón en la esquina inferior del chat).

### 4.2 Eventos Confiados vs No-Confiados (Critical Discovery)

Esta fue la lección más importante de toda la investigación:

```
❌ page.evaluate("el => el.click()", btn)      → isTrusted=false → VS Code IGNORA
❌ element.dispatchEvent(new MouseEvent(...))    → isTrusted=false → VS Code IGNORA
❌ await btn.click()                             → Playwright espera navegación → CUELGA
✅ await page.mouse.click(x, y)                 → Input.dispatchMouseEvent → isTrusted=true → FUNCIONA
```

**VS Code verifica `event.isTrusted`** para acciones UI como abrir dropdowns. Solo `page.mouse.click()` genera eventos CDP de bajo nivel (`Input.dispatchMouseEvent`) que Chromium marca como `isTrusted=true`.

### 4.3 El Algoritmo Final (3 pasos)

```python
# PASO 1: Obtener coordenadas del botón "Pick Model" y hacer click confiado
btn = await page.query_selector('a[aria-label^="Pick Model"]')
box = await btn.bounding_box()
cx = box["x"] + box["width"] / 2
cy = box["y"] + box["height"] / 2
await page.mouse.click(cx, cy)  # ← Trusted CDP event
await asyncio.sleep(1.0)         # Esperar render del dropdown

# PASO 2: Leer items del dropdown abierto
items = await page.evaluate("""() => {
    const view = document.querySelector('.context-view');
    if (!view) return [];
    const rows = view.querySelectorAll('.monaco-list-row.action');
    const results = [];
    for (const el of rows) {
        const text = (el.innerText || '').trim().replace(/\\n/g, ' | ');
        const rect = el.getBoundingClientRect();
        if (rect.height < 5 || rect.height > 50) continue;
        results.push({
            text: text.substring(0, 100),
            x: rect.x + rect.width / 2,
            y: rect.y + rect.height / 2
        });
    }
    return results;
}""")

# PASO 3: Click confiado en el item que coincide
for it in items:
    if "sonnet" in it["text"].lower() and "4.6" in it["text"].lower():
        await page.mouse.click(it["x"], it["y"])
        break
```

### 4.4 Selectores del Dropdown

Cuando el model picker se abre, el dropdown aparece como:

```
.context-view (clase: 'context-view monaco-component top right')
  └── [role="menu"]
       └── .monaco-list-row.action  ← cada item del modelo
            ├── .action-label        ← nombre del modelo
            └── .action-label        ← precio/multiplicador

Ejemplo de items visibles:
  "Auto | 10% discount"
  "Claude Opus 4.6 | High · 3x"
  "Claude Sonnet 4.6 | High · 1x"
  "Other Models"
```

**IMPORTANTE**: `.monaco-list-row` (sin `.action`) también matchea el container `.action-widget`. Filtrar por `height < 50` o usar `.monaco-list-row.action` para evitar falsos positivos.

### 4.5 Verificación Post-Cambio

```python
# Selector específico del label del modelo (excluye el label "Agent/Edit")
lbl = await page.query_selector(
    '.action-item.chat-input-picker-item'
    ':not(.chat-mode-picker-item)'
    ':not(.chat-session-target-picker-item)'
    ' .chat-input-picker-label'
)
model_text = (await lbl.inner_text()).strip()
# → "Claude Sonnet 4.6 · High"
```

### 4.6 Estrategia de Selección de Modelo Económico

El pool de modelos está ordenado de más económico a más caro:

```python
MODEL_POOL = [
    "claude-sonnet-4-6",    # 1x — más económico del picker principal
    "claude-opus-4-6",      # 3x — más caro
]
```

Cuando el monitor necesita cambiar de modelo (ej: rate limit, error):

1. Si está en el modelo más económico → cicla al siguiente
2. Si no está en el pool → selecciona `MODEL_POOL[0]` (el más económico)
3. El cycling es circular: sonnet-4-6 → opus-4-6 → sonnet-4-6

---

## 5. Monitor Watchdog

### 5.1 Qué Hace

`vscode_copilot_monitor.py` es el cerebro del sistema autónomo:

1. **Lee el chat** via CDP (primario) o log files (fallback)
2. **Clasifica el estado**: rate_limited, hard_error, success, stalled, etc.
3. **Ejecuta acciones deterministas**:
   - Rate limit → espera + cicla modelo
   - Hard error → abre nuevo chat + cicla modelo
   - Stalled → envía "continuar"
   - Success → no hace nada
4. **Envía "continuar"** pegando texto en el chat input via AppleScript
5. **Abre nuevo chat** con el prompt del AGENT_LOOP_PROMPT.md

### 5.2 Señales Duales (CDP + Log)

```
CDP (primario, si disponible):
  1. Conecta a localhost:9222
  2. Lee .interactive-session innerText
  3. Compara hash con snapshot anterior
  4. Si cambió → hay actividad

Log (fallback, siempre disponible):
  1. Encuentra ~/Library/Application Support/Code/logs/.../GitHub Copilot Chat.log
  2. Lee solo líneas NUEVAS (tracked file offset)
  3. Busca patrones de error en las líneas nuevas
```

### 5.3 Comandos CLI

```bash
# Loop continuo (modo principal)
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py

# Un solo ciclo
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --once

# Estado actual
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --status

# Intervalo custom
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --interval 30

# Verbose
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --debug

# Acciones manuales
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --action-continue
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --action-new-chat
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --open-chat

# Gestión de modelos
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --show-model
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --set-model claude-sonnet-4-6
.venv/bin/python3 .prompts/agent/vscode_copilot_monitor.py --cycle-model
```

### 5.4 VS Code Task

El monitor se puede lanzar como VS Code Task:

```json
// .vscode/tasks.json
{
  "label": "🧠 Copilot Monitor: Loop Continuo (cada 20s)",
  "type": "shell",
  "command": "source .venv/bin/activate && python3 .prompts/agent/vscode_copilot_monitor.py --interval 20"
}
```

Ejecutar: `Ctrl+Shift+P` → "Tasks: Run Task" → "🧠 Copilot Monitor"

---

## 6. Estructura de Archivos

```
.prompts/
├── AGENT_LOOP_PROMPT.md          ← Prompt del loop autónomo (NO mover)
├── monitor_prompt1.py            ← Monitor de prompt_1.md (NO mover)
├── prompt_1.md                   ← Archivo de tareas (NO mover)
│
└── agent/                        ← TODO lo relacionado con el sistema del agente
    ├── MANUAL.md                 ← Este archivo — documentación completa
    ├── vscode_copilot_monitor.py ← Monitor principal (CDP + Log watchdog)
    ├── vscode_chat_reader.py     ← Lector standalone del chat via CDP
    ├── cdp_network_spy.py        ← Interceptor de network requests via CDP
    ├── audit_watchdog.py         ← Tests de auditoría del monitor
    ├── mac_bot.py                ← Bot de automatización macOS (AppleScript)
    ├── bridge.py                 ← Bridge connector
    ├── start-monitor.py          ← Daemon launcher del monitor
    ├── update_prompt1.py         ← Actualiza prompt_1.md
    ├── sprints_v2.py             ← Automatización de sprints
    ├── append_k8s_agents.py      ← Generador de agentes K8s
    ├── parse-gateway.py          ← Parser de gateway
    ├── MANUAL_TASKS.md           ← Tareas manuales documentadas
    ├── TASK_DELEGATION.md        ← Delegación de tareas
    ├── .monitor_state.json       ← Estado persistido del monitor
    ├── .monitor_pid              ← PID del daemon
    └── chat_snapshot.txt         ← Último snapshot del chat
```

---

## 7. Guía de Uso Rápido

### Arrancar todo desde cero

```bash
# 1. Lanzar VS Code con CDP
./infra/code-watch.sh
# O: code --remote-debugging-port=9222 cardealer.code-workspace

# 2. Verificar CDP
curl -s http://localhost:9222/json/version

# 3. Activar venv
source .venv/bin/activate

# 4. Verificar que lee el chat
python3 .prompts/agent/vscode_chat_reader.py

# 5. Verificar modelo actual
python3 .prompts/agent/vscode_copilot_monitor.py --show-model

# 6. Cambiar al modelo más económico
python3 .prompts/agent/vscode_copilot_monitor.py --set-model claude-sonnet-4-6

# 7. Lanzar monitor en loop
python3 .prompts/agent/vscode_copilot_monitor.py --interval 20
```

### Uso como VS Code Task

1. `Cmd+Shift+P` → "Tasks: Run Task"
2. Seleccionar "🧠 Copilot Monitor: Loop Continuo (cada 20s)"
3. El monitor corre en un terminal dedicado

---

## 8. Troubleshooting

### CDP no conecta

```
Error: connect ECONNREFUSED 127.0.0.1:9222
```

**Solución**: VS Code no fue lanzado con `--remote-debugging-port=9222`. Cerrar VS Code y relanzar con el flag.

### Dropdown no abre / 0 items

```
WARN: Modelo 'X' no encontrado en el picker. Items: []
```

**Solución**: El chat panel puede no estar visible/enfocado. Asegurar que el panel de Copilot Chat esté abierto y visible.

### Model picker muestra "Other Models"

Modelos como Claude Haiku y Sonnet 4.5 están ahora bajo un submenu "Other Models". El monitor solo opera con los modelos directamente visibles en el picker principal.

### `page.evaluate` cierra el dropdown

**Esto NO es un bug**. Después de `page.mouse.click()` (que abre el dropdown), `page.evaluate()` SÍ puede leer el contenido — confirmado experimentalmente. La clave es usar `asyncio.sleep(1.0)` entre el click y el evaluate.

### Playwright import error

```
Import "playwright.async_api" could not be resolved
```

Esto es Pylance/IDE, no un error real. El venv tiene Playwright instalado: `.venv/bin/python3 -c "import playwright; print('OK')"`.

---

## 9. Lecciones Aprendidas (Investigación)

### Cronología del Descubrimiento

1. **VS Code = Chromium**: Descubrimos que VS Code (Electron) acepta `--remote-debugging-port` igual que Chrome, exponiendo CDP.

2. **Playwright como puente**: En vez de usar el protocolo CDP raw, usamos Playwright que abstrae la conexión:

   ```python
   browser = await pw.chromium.connect_over_cdp("http://localhost:9222")
   ```

3. **Chat DOM readable**: El panel de chat vive en `.interactive-session` y es completamente legible via `innerText`.

4. **settings.json NO funciona en tiempo real**: `github.copilot.chat.languageModel` es un setting interno. Al escribirlo en settings.json, VS Code no lo lee hasta reiniciar. La única forma de cambiar el modelo es via la UI.

5. **Trusted vs Untrusted events**: Este fue el descubrimiento más crítico:
   - `el.click()` en un evaluate → `isTrusted: false` → VS Code ignora
   - `page.mouse.click(x, y)` → CDP `Input.dispatchMouseEvent` → `isTrusted: true` → VS Code ejecuta

   La diferencia es que `page.mouse.click()` envía un evento de bajo nivel al proceso Chromium, mientras que `el.click()` ejecuta JavaScript en el render process que Chromium marca como "untrusted".

6. **Selector del dropdown**: El dropdown del model picker NO es `.quick-input-widget` (que es el command palette). Es `.context-view [role="menu"]` con items en `.monaco-list-row.action`.

7. **Height filter necesario**: `.monaco-list-row` sin `.action` matchea el container `.action-widget` (~114px). Los items reales son ~24px. Filtrar con `rect.height > 50` elimina falsos positivos.

8. **Modelos disponibles cambian**: Durante la investigación, los modelos del picker cambiaron (Haiku y Sonnet 4.5 se movieron a "Other Models"). El pool debe reflejar solo los modelos directamente visibles.

### Scripts de Investigación (Archivados)

Los siguientes scripts fueron creados durante la investigación y están archivados como `.bak_done`:

| Script                       | Propósito                               | Resultado                                        |
| ---------------------------- | --------------------------------------- | ------------------------------------------------ |
| `_cdp_model_inspect.py`      | Inspeccionar estructura DOM del picker  | Descubrió `a[aria-label^="Pick Model"]`          |
| `_cdp_inspect2.py`           | HTML detallado del botón                | Confirmó `aria-haspopup="true"`                  |
| `_cdp_model_switch.py`       | Primer intento de switch                | Falló — `el.click()` no es trusted               |
| `_cdp_debug_picker.py`       | Debug paso a paso                       | Confirmó que `.quick-input-widget` es incorrecto |
| `_cdp_trusted_click.py`      | **Breakthrough** — `page.mouse.click()` | Probó que `aria-expanded` cambia a `true`        |
| `_cdp_after_click.py`        | Leer menú después de click              | Confirmó items legibles con sleep(1.5)           |
| `_cdp_model_picker_final.py` | Click en coords del item                | Parcialmente exitoso — wrong selector            |
| `_cdp_model_one_shot.py`     | Single-evaluate approach                | Falló — dispatchEvent no es trusted              |
| `_cdp_model_v2.py`           | **Versión final** — 3 pasos             | ✅ Funciona perfectamente                        |

### Diagrama del Flujo Final

```
set_model("claude-sonnet-4-6")
    │
    ├─ 1. Escribir en settings.json (fallback)
    │     github.copilot.chat.languageModel = "claude-sonnet-4-6"
    │
    ├─ 2. CDP: Conectar a localhost:9222
    │     └─ Encontrar página "workbench"
    │
    ├─ 3. Encontrar botón: a[aria-label^="Pick Model"]
    │     └─ Obtener bounding_box → (cx, cy)
    │
    ├─ 4. page.mouse.click(cx, cy)     ← TRUSTED, abre dropdown
    │     └─ sleep(1.0)
    │
    ├─ 5. page.evaluate() → leer .context-view .monaco-list-row.action
    │     └─ Cada item: { text, x, y }
    │
    ├─ 6. Buscar match: "Claude Sonnet 4.6" en items
    │     └─ page.mouse.click(match.x, match.y)  ← TRUSTED, selecciona modelo
    │
    └─ 7. Verificar: leer .chat-input-picker-label
          └─ "Claude Sonnet 4.6 · High" ✓
```

---

## Apéndice: Referencia Rápida de Selectores CSS

```css
/* Chat panel */
.interactive-session                    /* Panel completo del chat */
.interactive-item-container             /* Cada mensaje */

/* Model picker — botón */
a[aria-label^="Pick Model"]             /* Botón del model picker */
.chat-input-picker-label                /* Label (puede matchear "Agent" también) */

/* Model picker — selector específico del MODELO (no Agent/Edit) */
.action-item.chat-input-picker-item:not(.chat-mode-picker-item):not(.chat-session-target-picker-item) .chat-input-picker-label

/* Model picker — dropdown abierto */
.context-view                           /* Container del dropdown */
.context-view [role="menu"]             /* Menú con role="menu" */
.context-view .monaco-list-row.action   /* Items individuales (h≈24px) */
.context-view .action-widget            /* ⚠️ Container (~114px) — filtrar */

/* Chat input */
.chat-input-part                        /* Área de input */
```
