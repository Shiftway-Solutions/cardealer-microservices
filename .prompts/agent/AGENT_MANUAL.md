# Manual del Agente OKLA — vscode_copilot_monitor + GEMMA3 Smart Monitor

> Documento vivo. Actualizar cuando se descubran nuevos selectores o errores.

---

## PARTE 1 — Cómo Funciona el DOM/CDP de VS Code

### 1.1 ¿Qué es CDP en VS Code?

Cuando VS Code se inicia con el flag `--remote-debugging-port=9222`, expone el protocolo
**Chrome DevTools Protocol (CDP)** en `http://localhost:9222`. Esto permite conectarse a la
interfaz de VS Code como si fuera una página web en un navegador Chromium, usando Playwright.

El workbench de VS Code es literalmente un HTML/DOM cargado desde `vscode-file://vscode-app/...`.
Todos los elementos del chat, botones, inputs, listas, modelos, son nodos DOM manipulables.

### 1.2 Conexión básica

```python
from playwright.async_api import async_playwright

async with async_playwright() as pw:
    browser = await pw.chromium.connect_over_cdp("http://localhost:9222")
    contexts = browser.contexts
    pages = contexts[0].pages if contexts else []
    # La página del workbench es vscode-file://vscode-app/...
    page = next((p for p in pages if "vscode-app" in p.url), pages[0])
```

### 1.3 Qué puedes hacer con CDP

| Operación                 | Método Playwright                | Ejemplo                             |
| ------------------------- | -------------------------------- | ----------------------------------- |
| Leer texto de un elemento | `page.evaluate(js)`              | Leer título del chat                |
| Click en coordenadas      | `page.mouse.click(x, y)`         | Click en botón `+` nuevo chat       |
| Click via JS              | `page.evaluate("el.click()")`    | Click en `monaco-list-row`          |
| Escribir texto            | `page.keyboard.insert_text(txt)` | Escribir prompt                     |
| Enviar tecla              | `page.keyboard.press("Enter")`   | Enviar mensaje                      |
| Screenshot                | `page.screenshot(path=...)`      | Captura visual                      |
| Buscar elemento           | `page.evaluate(querySelector)`   | Buscar input del chat               |
| Obtener rect              | `getBoundingClientRect()`        | Verificar si el elemento es visible |

### 1.4 Regla crítica: CDP vs AppleScript

| Tipo de elemento                        | CDP funciona                        | AppleScript necesario |
| --------------------------------------- | ----------------------------------- | --------------------- |
| Botones directos (no dropdown)          | ✅ `page.mouse.click(x, y)`         | ❌ No                 |
| Inputs / textareas                      | ✅ click + `keyboard.insert_text`   | ❌ No                 |
| Monaco virtual list (`monaco-list-row`) | ✅ `el.click()` via JS              | ❌ No                 |
| Menús contextuales (`aria-haspopup`)    | ❌ Falla                            | ✅ `osascript`        |
| Command Palette                         | ✅ `keyboard.press("Meta+Shift+P")` | ✅ Alternativa        |
| Cambiar foco de app                     | ❌ No aplica                        | ✅ Necesario          |

### 1.5 Selectores clave confirmados del chat de Copilot

```css
/* Título del chat activo */
.chat-view-title-label span

/* Panel SESSIONS (nuevo chat activo revela la lista) */
#workbench\.panel\.chat .monaco-list-row

/* Botón + (nuevo chat directo, SIN popup) */
.part.auxiliarybar a[aria-label="New Chat (⌘N)"][class*="codicon-plus"]:not([aria-haspopup])

/* Botón ▼ (dropdown — NO clickeable via CDP) */
.part.auxiliarybar a[aria-label="New Chat"][class*="chevron"]

/* Input del chat (Monaco editor, textarea interna) */
.interactive-input-part .monaco-editor textarea
.interactive-input-part [contenteditable="true"]
.chat-input-container textarea

/* Botón STOP (detener generación) */
button[aria-label="Stop Response"]
button[aria-label="Stop"]
a[aria-label*="Stop" i]
.interactive-stop-button

/* Selector de modelo */
.monaco-button[class*="model-picker"]
.interactive-toolbar button[aria-label*="model" i]
.chat-input-part button[title*="Claude" i]
.chat-input-part button[title*="GPT" i]
```

### 1.6 Flujo completo: Nuevo Chat via CDP

```python
# 1. Encontrar el botón + con getBoundingClientRect()
btn = await page.evaluate("""() => {
    const sel = '.part.auxiliarybar a[aria-label="New Chat (⌘N)"]';
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    if (r.width === 0) return null;
    if (el.getAttribute('aria-haspopup')) return null;  // excluir dropdown
    return { x: r.x + r.width/2, y: r.y + r.height/2 };
}""")

# 2. Click en coordenadas CDP
await page.mouse.click(btn["x"], btn["y"])
await asyncio.sleep(1.0)
# → La lista de sesiones (monaco-list-row) ahora tiene coordenadas reales
```

### 1.7 Flujo completo: Navegar a Chat por Nombre via CDP

```python
# 1. Asegurarse de que el panel de sesiones está visible
# (requiere estar en "nuevo chat" para que los rows sean visibles)

# 2. Leer todas las sesiones
rows = await page.evaluate("""() => {
    return [...document.querySelectorAll('#workbench\\.panel\\.chat .monaco-list-row')]
        .map(el => ({
            dataIndex: el.getAttribute('data-index'),
            ariaLabel: el.getAttribute('aria-label'),
            title:     el.querySelector('.chat-entry-title')?.textContent?.trim() || '',
        }));
}""")

# 3. Buscar por título (solo title/ariaLabel, NO fullText — evitar falsos positivos)
target = next((r for r in rows if search_term.lower() in r['title'].lower()), None)

# 4. Click via JS (más confiable que coordenadas en virtual scroll)
await page.evaluate(f"""() => {{
    const el = document.querySelector('#workbench\\.panel\\.chat .monaco-list-row[data-index="{target['dataIndex']}"]');
    if (el) el.click();
}}""")
```

### 1.8 Flujo completo: Focus + Enviar Prompt

```python
# 1. Encontrar el input
input_info = await page.evaluate("""() => {
    const sel = '.interactive-input-part .monaco-editor textarea';
    const el = document.querySelector(sel);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { x: r.x + r.width/2, y: r.y + r.height/2 };
}""")

# 2. Click para focus
await page.mouse.click(input_info["x"], input_info["y"])
await asyncio.sleep(0.3)

# 3. Limpiar y escribir
await page.keyboard.press("Meta+A")
await page.keyboard.press("Backspace")
await page.keyboard.insert_text("Mi prompt aquí")
await asyncio.sleep(0.2)

# 4. Enviar
await page.keyboard.press("Enter")
```

### 1.9 Flujo completo: Stop Response via CDP

```python
# 1. Buscar el botón stop (solo visible mientras el modelo genera)
stop = await page.evaluate("""() => {
    const SELS = [
        'button[aria-label="Stop Response"]',
        'button[aria-label="Stop"]',
        'a[aria-label*="Stop" i]',
        '.interactive-stop-button',
    ];
    for (const sel of SELS) {
        const el = document.querySelector(sel);
        if (!el) continue;
        const r = el.getBoundingClientRect();
        if (r.width > 0 && r.height > 0)
            return { x: r.x + r.width/2, y: r.y + r.height/2 };
    }
    return null;
}""")

# 2. Click directo
if stop:
    await page.mouse.click(stop["x"], stop["y"])
    await asyncio.sleep(1.0)
```

---

## PARTE 2 — Catálogo Completo de Acciones del Agente

### 2.1 Acciones de lectura / observación

| Función                                                          | Descripción                                                                | Método primario                               | Fallback       |
| ---------------------------------------------------------------- | -------------------------------------------------------------------------- | --------------------------------------------- | -------------- |
| `read_chat_via_cdp()`                                            | Lee todo el texto del chat activo                                          | CDP `querySelectorAll` sobre nodos de mensaje | Log file       |
| `classify_chat_text(text, prev_hash)`                            | Clasifica: `success`, `rate_limited`, `hard_error`, `cancelled`, `unknown` | Regex sobre últimos 500 chars                 | —              |
| `inspect_chat_dom()`                                             | Snapshot DOM completo: botones, inputs, estado                             | CDP `querySelectorAll` amplio                 | —              |
| `get_current_chat_title()` / `_cdp_get_current_chat_title(page)` | Título del chat activo `.chat-view-title-label span`                       | CDP JS evaluate                               | AX API         |
| `get_chat_history()` / `_cdp_get_chat_history(page)`             | Lista todos los chats: title, ariaLabel, dataIndex                         | CDP `monaco-list-row`                         | —              |
| `is_response_generating()`                                       | Detecta botón Stop visible (modelo generando)                              | CDP selector `button[aria-label="Stop"]`      | DOM snapshot   |
| `is_chat_input_focused()`                                        | Input del chat tiene focus activo                                          | CDP `document.activeElement`                  | AX API macOS   |
| `is_vscode_focused()`                                            | VS Code es la app activa                                                   | `osascript` proceso nombre                    | —              |
| `is_vscode_running()`                                            | VS Code proceso corriendo                                                  | `ps aux` grep                                 | —              |
| `scan_full_ui_inventory()`                                       | Inventario completo de UI: botones, inputs, toolbars                       | CDP DOM snapshot full                         | —              |
| `capture_workbench_screenshot(label)`                            | Screenshot para debugging visual                                           | CDP `page.screenshot()`                       | —              |
| `get_current_model()` / `get_current_model_label()`              | Modelo activo en el picker                                                 | CDP selector de toolbar                       | Snap regex     |
| `get_model_pool()`                                               | Lista de modelos en rotación del pool                                      | `model_catalog.json`                          | Config default |
| `discover_available_models()`                                    | Descubre TODOS los modelos disponibles                                     | CDP model picker DOM                          | —              |
| `inspect_model_picker()`                                         | Snapshot del model picker con opciones                                     | CDP hover + DOM                               | —              |

### 2.2 Acciones de control del chat

| Función                            | Descripción                                        | Método primario                                                           | Fallback                                                                                         |
| ---------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `vscode_send_to_chat(message)`     | Focus → escribe → envía prompt                     | CDP click + `keyboard.insert_text` + Enter                                | Fallback clipboard solo con focus confirmado; si falla, aborta                                   |
| `vscode_send_continue()`           | Envía `"continuar"` al chat                        | `vscode_send_to_chat("continuar")`                                        | —                                                                                                |
| `send_loop_prompt()`               | Envía `AGENT_LOOP_PROMPT.md` al chat               | Lee archivo + `vscode_send_to_chat`                                       | —                                                                                                |
| `send_prompt_1()`                  | Envía `prompt_1.md` (sprint actual)                | Lee archivo + `vscode_send_to_chat`                                       | —                                                                                                |
| `stop_current_response()`          | Click en ⏹ Stop button                             | CDP `getBoundingClientRect` + `page.mouse.click`                          | AppleScript Escape                                                                               |
| `vscode_open_new_chat()`           | Abre nuevo chat vacío                              | CDP click `+` (no `aria-haspopup`)                                        | DOM snapshot → AppleScript Command Palette                                                       |
| `open_new_chat_with_stop()`        | Stop → pausa 1.5s → nuevo chat → envía loop prompt | Secuencia de los anteriores                                               | —                                                                                                |
| `navigate_to_chat_by_title(title)` | Navega a chat específico por nombre                | CDP: `_cdp_ensure_sessions_panel_visible` → match title → `JS el.click()` | —                                                                                                |
| `open_new_chat_option(option)`     | Abre opción del dropdown de nuevo chat             | CDP Command Palette                                                       | —                                                                                                |
| `_focus_chat_input()`              | Fuerza focus en el input del chat                  | CDP click en `.interactive-input-part textarea`                           | Fallback inseguro deshabilitado por defecto; requiere `SMART_MONITOR_ENABLE_UNSAFE_CHAT_FOCUS=1` |

### 2.3 Acciones de gestión de modelos

| Función                                         | Descripción                                                       |
| ----------------------------------------------- | ----------------------------------------------------------------- |
| `set_model(model_id)`                           | Cambia al modelo especificado via CDP model picker                |
| `cycle_model(state, direction=1)`               | Rota al siguiente/anterior modelo del pool                        |
| `ensure_preferred_model()`                      | Verifica y fuerza el modelo preferido (GPT-5.4 Xhigh por defecto) |
| `set_model_with_thinking_effort(model, effort)` | Cambia modelo + nivel thinking (High/Medium/Low)                  |
| `discover_models_with_thinking_effort()`        | Descubre modelos que soportan thinking effort                     |

### 2.4 Acciones autónomas del loop (ejecutadas por `execute_action`)

| Action string               | Cuándo se activa                                   | Comportamiento                                         |
| --------------------------- | -------------------------------------------------- | ------------------------------------------------------ |
| `wait` / `do_nothing`       | Agente generando normalmente                       | No actuar, resetear contadores de error                |
| `send_continue`             | Stall >15 min sin errores                          | Envía `"continuar"` al chat actual                     |
| `open_new_chat`             | Hard error + max retries alcanzado / stall >20 min | Nuevo chat + loop prompt                               |
| `stop_and_new_chat`         | Contexto >600k chars (lleno)                       | Stop → nuevo chat inmediato                            |
| `cycle_model`               | Rate limit detectado                               | Rota modelo → envía continuar tras 2s                  |
| `focus_vscode`              | VS Code sin foco                                   | Solo trae VS Code al frente; no interactúa con el chat |
| `restart_mcp_or_vscode`     | Tool validation error                              | Solo notificación macOS (sin acción automática)        |
| `check_if_progress_stalled` | Loop detenido sin actividad                        | Evalúa stall time → `send_continue` o `open_new_chat`  |
| `observe_or_retry`          | Request cancelado                                  | Observa siguiente ciclo sin actuar                     |

Nota: desde el endurecimiento de seguridad, el monitor no debe escribir `Focus on Copilot Chat View` en el chat. Si el focus del input no puede confirmarse por DOM/CDP, el envío se cancela en vez de hacer un pegado ciego.

---

## PARTE 3 — Catálogo Completo de Errores del Chat

### 3.1 Errores detectados en `chat_snapshot.txt`

| Error visible en chat                                                                   | Código interno   | Qué significa                                       | Acción a tomar                                                              |
| --------------------------------------------------------------------------------------- | ---------------- | --------------------------------------------------- | --------------------------------------------------------------------------- |
| `"rate limit"` / `"429"` / `"Too Many Requests"` / `"quota exhausted"`                  | `rate_limited`   | Límite de uso del modelo alcanzado                  | `cycle_model` → esperar cooldown (600s) → `send_continue`                   |
| `"overloaded"` / `"503"` / `"502"` / `"500"` / `"Internal Server Error"` / `"capacity"` | `hard_error`     | Servidor Anthropic/OpenAI caído o sobrecargado      | Esperar 60s → `send_continue` hasta MAX_ERROR_RETRIES (3) → `open_new_chat` |
| `"cancelled"` / `"canceled"`                                                            | `cancelled`      | El usuario o el agente canceló la generación        | Observar → si no hay actividad en 5 min → `send_continue`                   |
| `"context window full"` / chat_len > 600,000 chars                                      | `context_full`   | Contexto del chat saturado                          | `stop_and_new_chat` INMEDIATAMENTE                                          |
| _(Texto dejó de cambiar por >15 min)_                                                   | `stall`          | Agente inactivo / completó tarea / error silencioso | `send_continue` → si 3 intentos fallan → `open_new_chat`                    |
| _(Texto dejó de cambiar por >20 min)_                                                   | `stall_critical` | Agente completamente parado                         | `open_new_chat` con nuevo modelo                                            |

### 3.2 Errores detectados en el Log de Copilot (`GitHub Copilot Chat.log`)

| Patrón en log                                                   | Código interno          | Descripción                                                                |
| --------------------------------------------------------------- | ----------------------- | -------------------------------------------------------------------------- |
| `rate.limit\|429\|Too Many Requests\|RateLimitError`            | `rate_limited`          | Igual que arriba, pero detectado en log antes del chat                     |
| `[error].*500\|503\|502\|overload\|capacity\|overloaded_error`  | `hard_error`            | Error del server-side                                                      |
| `failed validation.*schema must be\|ToolValidationError`        | `tool_validation_error` | Herramienta MCP con schema inválido — notificar, no actuar automáticamente |
| `ccreq:.*\|cancelled\|`                                         | `cancelled`             | Request cancelado internamente                                             |
| `Stop hook result.*shouldContinue=false\|ToolCallingLoop.*Stop` | `loop_stopped`          | El loop del agente se detuvo (completó o error)                            |
| `ccreq:.*\|success\|`                                           | `success`               | Request completado con éxito                                               |

### 3.3 Estados del chat que NO son errores

| Estado                                                                | Descripción                         | Acción                                          |
| --------------------------------------------------------------------- | ----------------------------------- | ----------------------------------------------- |
| `% en barra de progreso` o texto cambiando cada ~2s                   | Generación activa normal            | `wait` — NO intervenir                          |
| Footer del modelo visible (`Claude Sonnet 4.6 · High`) sin porcentaje | Modelo terminó su turno             | `open_new_chat` si el agente no retoma en 5 min |
| Texto de respuesta completo visible, sin cambios, sin `%`             | Copilot respondió, esperando input  | `send_continue` si stall >15 min                |
| `"Preparing..."` o spinner visible                                    | Modelo iniciando generación         | `wait`                                          |
| `"Compacting conversation..."`                                        | Compactación automática de contexto | `wait` ~30-60s                                  |

### 3.4 Tabla resumen: Señal → Decisión

| Señal detectada                     | Confianza | Acción                                | Cooldown       |
| ----------------------------------- | --------- | ------------------------------------- | -------------- |
| Generación activa (texto cambiando) | 0.95      | `wait`                                | —              |
| Actividad normal últimos 2 min      | 0.90      | `wait`                                | —              |
| Rate limit (429 / quota)            | 1.00      | `cycle_model`                         | 600s           |
| Hard error (500/502/503)            | 0.85      | `send_continue` × 3 → `open_new_chat` | 60s/intento    |
| Tool validation error               | 0.70      | notificar (no actuar)                 | —              |
| Contexto >600k chars                | 1.00      | `stop_and_new_chat`                   | 0s (inmediato) |
| Stall 15 min sin error              | 0.80      | `send_continue`                       | 60s            |
| Stall 20 min                        | 0.85      | `open_new_chat`                       | 60s            |
| Request cancelado                   | 0.60      | `wait` → `send_continue` si stall     | —              |
| VS Code sin foco                    | 0.85      | `focus_vscode`                        | 30s            |
| Loop detenido (footer visible)      | 0.75      | `open_new_chat` si stall >5 min       | 90s            |

---

## PARTE 4 — Arquitectura del Agente GEMMA3 (Smart Monitor)

```
chat_snapshot.txt
     │
     ▼
┌─────────────┐    ┌──────────────┐    ┌──────────────────┐
│  Observer   │───▶│    Brain     │───▶│  ActionExecutor  │
│ (sensores)  │    │  (Gemma3 +   │    │  (CDP + Apple-   │
│             │    │  overrides   │    │   Script v7)     │
└─────────────┘    │  fallback)   │    └──────────────────┘
      │            └──────────────┘            │
      │                   │                    │
      ▼                   ▼                    ▼
CDP/Log/Snapshot    Ollama local          VS Code UI
                  (gemma3:4b)          (chat, modelos,
                                       focus, stop, new)
                                             │
                                             ▼
                                    ┌─────────────────┐
                                    │  FeedbackLoop   │
                                    │ (verificar si   │
                                    │  funcionó)      │
                                    └─────────────────┘
                                             │
                                             ▼
                                    ┌─────────────────┐
                                    │    Memory       │
                                    │ (SQLite, aprende│
                                    │ de outcomes)    │
                                    └─────────────────┘
```

### 4.1 Ciclo Observar → Pensar → Actuar → Aprender

```
1. OBSERVAR (Observer.observe(state))
   - Leer chat_snapshot.txt (mtime, hash, tail 500 chars, errores regex)
   - Leer log de Copilot (líneas nuevas, clasificar patrones)
   - CDP: leer texto del chat, tamaño, cambios
   - Verificar VS Code running + focused
   - Calcular timers: secs_since_last_activity, secs_since_last_continue, etc.

2. PENSAR (Brain.decide(obs))
   - Aplicar overrides deterministas primero (contexto lleno, VS Code muerto, etc.)
   - Si no hay override: construir prompt para Gemma3
   - Enviar a Ollama (gemma3:4b en localhost:11434)
   - Parsear respuesta JSON: { decision, confidence, reasoning, wait_before_action_secs }
   - Fallback determinista si Gemma falla

3. ACTUAR (ActionExecutor.execute(decision))
   - Verificar cooldown (anti-spam)
   - Ejecutar la acción via v7 (CDP primero, AppleScript como fallback)
   - Registrar en state: last_action_ts, post_action_ts

4. APRENDER (Memory + FeedbackLoop)
   - Esperar 2 min para verificar si la acción funcionó
   - Clasificar outcome: resolved / no_effect / escalated
   - Guardar en SQLite con lesson aprendida
   - Alimentar context de Gemma en próximas decisiones
```

---

## PARTE 5 — Cómo Mantener Copilot Siempre Generando

### 5.1 Regla de oro

> **No intervenir cuando el agente está activo.** Las intervenciones innecesarias
> interrumpen la generación y confunden al agente. La prioridad es `wait`.

### 5.2 Árbol de decisión completo

```
¿VS Code corriendo?
 ├─ NO → wait (nada que hacer)
 └─ SÍ
     ├─ ¿Contexto >600k chars?
     │     └─ SÍ → stop_and_new_chat (INMEDIATO)
     │
     ├─ ¿Generación activa? (texto cambiando + % visible)
     │     └─ SÍ → wait (no tocar en ningún caso)
     │
     ├─ ¿VS Code tiene foco?
     │     └─ NO → focus_vscode
     │
     ├─ ¿Rate limit en snapshot o log? (429/quota)
     │     └─ SÍ → cycle_model → esperar 600s → send_continue
     │
     ├─ ¿Hard error? (500/502/503/overloaded)
     │     ├─ Intentos < 3 → send_continue
     │     └─ Intentos >= 3 → open_new_chat (nuevo modelo)
     │
     ├─ ¿Tool validation error? (schema inválido)
     │     └─ SÍ → notificar, wait (requiere fix manual)
     │
     ├─ ¿Request cancelado?
     │     └─ SÍ → wait → si stall >5 min → send_continue
     │
     ├─ ¿Stall <15 min? (chat no cambió)
     │     └─ wait (normal completion o typing largo)
     │
     ├─ ¿Stall 15-20 min?
     │     └─ send_continue (el agente terminó su turno)
     │
     └─ ¿Stall >20 min?
           └─ open_new_chat (con nuevo modelo del pool)
```

### 5.3 Pool de modelos (orden de rotación)

El agente cicla modelos cuando hay rate limit o error persistente:

```
Tier 1x (1 request unit):
  1. GPT-5.4 · Xhigh      ← PREFERIDO — arrancar siempre con este
  2. Claude Sonnet 4.6 · High
  3. GPT-4.1
  4. o3

Tier 3x (3 request units — solo si los 1x fallan todos):
  5. Claude Opus 4.5
  6. o4-mini · High
  7. GPT-4.5
```

### 5.4 Cooldowns entre acciones (anti-spam)

| Acción              | Cooldown mínimo       |
| ------------------- | --------------------- |
| `send_continue`     | 60 segundos           |
| `open_new_chat`     | 90 segundos           |
| `stop_and_new_chat` | 90 segundos           |
| `cycle_model`       | 120 segundos          |
| `focus_vscode`      | 30 segundos           |
| Rate limit recovery | 600 segundos (10 min) |

### 5.5 Leer `chat_snapshot.txt` para decidir

El archivo `.prompts/agent/chat_snapshot.txt` es la **fuente primaria de verdad**.

```python
# Estructura del snapshot
# ─────────────────────────────────────────────
# # Chat Snapshot -- 2026-03-29 20:52:24
#
# [contenido completo del chat visible en VS Code]
# ─────────────────────────────────────────────

def analizar_snapshot(text: str) -> str:
    tail = text[-500:]  # Solo revisar los últimos 500 chars

    # Señales de error
    if re.search(r"rate.limit|429|Too Many Requests", tail, re.I):
        return "rate_limited"
    if re.search(r"overloaded|503|502|500|Internal Server Error", tail, re.I):
        return "hard_error"
    if re.search(r"cancelled|canceled", tail, re.I):
        return "cancelled"

    # Señal de generación activa (el agente está trabajando)
    if re.search(r"\d+%|Preparing\.\.\.|Running|Thinking", tail, re.I):
        return "generating"

    # Footer del modelo (terminó su turno)
    if re.search(r"(Claude|GPT|Sonnet|Opus|o\d).{0,40}(High|Low|Medium|Local|Remote)", tail, re.I):
        return "model_footer_visible"

    return "unknown"
```

---

## PARTE 6 — Comandos de Operación del Agente

### 6.1 Iniciar el Smart Monitor (GEMMA3)

```bash
# Loop continuo (cada 20s)
cd /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices
source .venv/bin/activate
python3 -m smart_monitor.agent --interval 20

# Una vez (diagnóstico)
python3 -m smart_monitor.agent --once

# Estado actual
python3 -m smart_monitor.agent --status

# Debug verbose
python3 -m smart_monitor.agent --debug

# Dry-run (no ejecuta acciones, solo loggea)
python3 -m smart_monitor.agent --dry-run
```

### 6.2 Funciones de diagnóstico del monitor v7

```bash
# Ver estado del chat
python3 -c "import vscode_copilot_monitor as m; print(m.get_current_chat_title())"

# Ver si hay generación activa
python3 -c "import vscode_copilot_monitor as m; print(m.is_response_generating())"

# Ver el modelo activo
python3 -c "import vscode_copilot_monitor as m; print(m.get_current_model())"

# Leer el chat completo
python3 -c "import vscode_copilot_monitor as m; print(m.read_chat_via_cdp()[:500])"

# Historial de chats
python3 -c "import vscode_copilot_monitor as m; import json; print(json.dumps(m.get_chat_history(), indent=2))"
```

### 6.3 Screenshots de diagnóstico

```bash
# Las demos anteriores prueban el flujo completo:
python3 .prompts/agent/demo_focus_stop.py     # Focus + Send + Stop
# Screenshots en: .prompts/agent/screenshots/focus_0N_*.png
```

---

## PARTE 7 — Archivos del Sistema

| Archivo                                          | Propósito                                         |
| ------------------------------------------------ | ------------------------------------------------- |
| `.prompts/agent/vscode_copilot_monitor.py`       | Monitor v7 — todas las funciones CDP + acciones   |
| `.prompts/agent/smart_monitor/agent.py`          | Entry point del agente GEMMA3                     |
| `.prompts/agent/smart_monitor/brain.py`          | Motor de decisión (Gemma3 + overrides + fallback) |
| `.prompts/agent/smart_monitor/observer.py`       | Capa de observación estructurada                  |
| `.prompts/agent/smart_monitor/memory.py`         | Memoria SQLite (lecciones aprendidas)             |
| `.prompts/agent/smart_monitor/feedback.py`       | Verificación post-acción                          |
| `.prompts/agent/smart_monitor/memory.db`         | Base de datos SQLite de lecciones                 |
| `.prompts/agent/chat_snapshot.txt`               | Snapshot actual del chat (fuente primaria)        |
| `.prompts/agent/model_catalog.json`              | Catálogo de modelos descubiertos                  |
| `.prompts/agent/smart_monitor/.agent_state.json` | Estado persistido entre ciclos                    |
| `.prompts/agent/smart_monitor/.agent_pid`        | PID del proceso del agente                        |
| `.github/smart-monitor.log`                      | Log del smart monitor                             |
| `.github/copilot-monitor.log`                    | Log del monitor v7                                |
