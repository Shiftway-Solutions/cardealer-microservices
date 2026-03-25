# OpenClaw — Guía Completa de Modificaciones de UI

> **Versión documentada:** OpenClaw `2026.3.23-beta.1`  
> **Fuente oficial:** [docs.openclaw.ai](https://docs.openclaw.ai)  
> **Config principal:** `~/.openclaw/openclaw.json`  
> **URL Control UI (local):** [http://127.0.0.1:18789](http://127.0.0.1:18789)

---

## Índice

1. [Control UI — Visión general](#1-control-ui--visión-general)
2. [Acceso y autenticación de la UI](#2-acceso-y-autenticación-de-la-ui)
3. [Configuración de `gateway.controlUi`](#3-configuración-de-gatewaycontrolui)
4. [Idioma de la UI](#4-idioma-de-la-ui)
5. [Paneles disponibles en la Control UI](#5-paneles-disponibles-en-la-control-ui)
6. [Dropdown de Modelos — Cómo modificarlo](#6-dropdown-de-modelos--cómo-modificarlo)
7. [Dropdown de Agentes — Cómo modificarlo](#7-dropdown-de-agentes--cómo-modificarlo)
8. [Dropdown de Sesiones — Cómo modificarlo](#8-dropdown-de-sesiones--cómo-modificarlo)
9. [Overrides de sesión desde la UI](#9-overrides-de-sesión-desde-la-ui)
10. [TUI — Terminal UI](#10-tui--terminal-ui)
11. [Config CLI — Edición no-interactiva](#11-config-cli--edición-no-interactiva)
12. [Identidad del Agente (nombre, emoji, avatar)](#12-identidad-del-agente-nombre-emoji-avatar)
13. [Construcción y desarrollo de la UI](#13-construcción-y-desarrollo-de-la-ui)
14. [Configuración para OKLA](#14-configuración-para-okla)

---

## 1. Control UI — Visión general

La **Control UI** es una SPA (Vite + Lit) servida directamente por el Gateway en el mismo puerto. Habla con el Gateway via **WebSocket**.

```
Arquitectura:
Browser ──── WebSocket (WS) ───→ Gateway (puerto 18789) ──→ Agentes LLM
```

### 3 formas de abrir la UI

```bash
# Opción A: Comando directo
openclaw dashboard

# Opción B: URL en el browser
http://127.0.0.1:18789/

# Opción C: TUI (Terminal UI)
openclaw tui
```

---

## 2. Acceso y autenticación de la UI

### Primer acceso (local)

Las conexiones desde `127.0.0.1` se **auto-aprueban**. Solo pega el token del Gateway en el panel de configuración de la UI.

### Acceso remoto (requiere pairing)

Al conectarse desde un browser/dispositivo nuevo, aparece:

```
disconnected (1008): pairing required
```

**Aprobar el dispositivo:**

```bash
# Ver solicitudes pendientes
openclaw devices list

# Aprobar por requestId
openclaw devices approve <requestId>

# Revocar un dispositivo
openclaw devices revoke --device <id> --role <role>
```

> Cada perfil de browser genera un Device ID único. Cambiar de browser o borrar cookies requiere re-pairing.

### Token desde URL (para desarrollo/testing)

```
# Con token en fragment (recomendado — no se envía al servidor)
http://localhost:5173/?gatewayUrl=ws://127.0.0.1:18789#token=<gateway-token>

# Con URL remota via Tailscale
https://<magicdns>/<basePath>
```

---

## 3. Configuración de `gateway.controlUi`

Todas las opciones de la Control UI se configuran bajo `gateway.controlUi` en `~/.openclaw/openclaw.json`.

```json5
{
  gateway: {
    controlUi: {
      // ─── Opciones básicas ───────────────────────────────────────────
      enabled: true,                  // activar/desactivar la Control UI

      basePath: "/openclaw",          // cambiar la ruta base
                                      // Por defecto: "/" → http://host:18789/
                                      // Con basePath: http://host:18789/openclaw

      // ─── Orígenes permitidos (para acceso remoto) ───────────────────
      allowedOrigins: [               // REQUERIDO para orígenes no-loopback
        "https://control.ejemplo.com",
        "http://localhost:5173",       // solo para dev
      ],

      // ─── Compatibilidad HTTP inseguro ───────────────────────────────
      allowInsecureAuth: false,       // permite sesiones localhost sin device
                                      // identity en contextos HTTP no-seguro
                                      // (NO bypasea pairing checks)

      // ─── Break-glass (solo emergencias) ────────────────────────────
      dangerouslyDisableDeviceAuth: false,  // deshabilita device identity checks
                                            // REVERTIR INMEDIATAMENTE tras uso

      dangerouslyAllowHostHeaderOriginFallback: false, // modo Host-header origin
    }
  }
}
```

### Aplicar cambios via CLI (sin editar el JSON)

```bash
# Cambiar basePath
openclaw config set gateway.controlUi.basePath "/mi-ruta"

# Habilitar allowInsecureAuth (para HTTP local sin HTTPS)
openclaw config set gateway.controlUi.allowInsecureAuth true --strict-json

# Agregar origen permitido
openclaw config set gateway.controlUi.allowedOrigins '["https://control.okla.com.do"]' --strict-json

# Deshabilitar la UI completamente
openclaw config set gateway.controlUi.enabled false --strict-json

# Verificar el valor actual
openclaw config get gateway.controlUi
```

---

## 4. Idioma de la UI

La Control UI se auto-detecta según el locale del browser. Se puede cambiar manualmente desde el **panel Access** en la UI.

| Locale | Idioma |
|--------|--------|
| `en` | English (default fallback) |
| `es` | Español |
| `zh-CN` | Chino Simplificado |
| `zh-TW` | Chino Tradicional |
| `pt-BR` | Portugués Brasileño |
| `de` | Alemán |

> El idioma seleccionado se guarda en `localStorage` del browser. Traducciones no-inglesas se cargan lazy. Claves faltantes hacen fallback a inglés.

---

## 5. Paneles disponibles en la Control UI

| Panel | Funcionalidades |
|-------|----------------|
| **Chat** | Enviar mensajes, ver tool calls en streaming, tool output cards, abort runs |
| **Channels** | Estado WhatsApp/Telegram/Discord/Slack, QR login, config por canal |
| **Instances** | Lista de presencia de instancias, refresh (`system-presence`) |
| **Sessions** | Listar sesiones, aplicar overrides (thinking/fast/verbose/reasoning) |
| **Cron** | Agregar/editar/ejecutar/habilitar/deshabilitar jobs + historial |
| **Skills** | Estado, enable/disable, instalar desde ClawHub, actualizar API keys |
| **Nodes** | Lista de nodos disponibles y sus capacidades |
| **Exec Approvals** | Editar allowlists del gateway/node, política de `exec` |
| **Config** | Editar `openclaw.json` via form visual **o** Raw JSON editor |
| **Debug** | Status/health/models snapshots, event log, RPC manual |
| **Logs** | Live tail de logs del gateway con filtros y export |
| **Update** | Actualizar el paquete + restart con reporte |

### Notas del panel Cron

- Delivery por defecto para jobs aislados: `announce summary`
- Cambiar a `none` para runs internos sin delivery
- Campos de channel/target solo aparecen cuando `announce` está seleccionado
- Webhook mode: `delivery.mode = "webhook"` con `delivery.to` como URL HTTPS
- Token para webhook: configurar `cron.webhookToken` (sin él, no hay header `Authorization`)
- Controles avanzados: delete-after-run, model/thinking overrides por job, best-effort delivery

### Notas del panel Config

- Renderiza un **formulario** generado desde el schema de config (incluyendo schemas de plugins y canales)
- Siempre disponible el **Raw JSON editor** como fallback
- Las escrituras incluyen un **base-hash guard** para prevenir sobrescritura concurrente
- Valida y aplica en caliente sin restart (excepto ciertos campos del gateway)

---

## 6. Dropdown de Modelos — Cómo modificarlo

El **Model Picker** (dropdown de modelos) en la UI se alimenta de `agents.defaults.models` en `openclaw.json`.

### Regla clave

> Si `agents.defaults.models` **tiene valores**, actúa como **allowlist** — solo esos modelos aparecen en el picker.  
> Si está **vacío o no definido**, el picker muestra todo el catálogo del proveedor.

### Configuración en `openclaw.json`

```json5
{
  agents: {
    defaults: {
      // ─── Modelo primario (el que aparece seleccionado por defecto) ──
      model: {
        primary: "github-copilot/claude-haiku-4.5",
        fallbacks: [
          "github-copilot/claude-sonnet-4-5",
          "ollama/llama3.2"
        ]
      },

      // ─── Catálogo visible en el dropdown ───────────────────────────
      // Solo los modelos listados aquí aparecerán en el Model Picker
      models: {
        "github-copilot/claude-haiku-4.5": {
          alias: "Haiku (Rápido)"         // nombre visible en el picker
        },
        "github-copilot/claude-sonnet-4-5": {
          alias: "Sonnet 4.5 (Calidad)"
        },
        "github-copilot/claude-sonnet-4-6": {
          alias: "Sonnet 4.6 (Latest)"
        },
        "ollama/llama3.2": {
          alias: "Llama 3.2 (Local)"
        }
      }
    }
  }
}
```

### Agregar/quitar modelos del dropdown via CLI

```bash
# Ver modelos actuales
openclaw models list

# Establecer modelo primario (lo que aparece seleccionado por defecto)
openclaw models set github-copilot/claude-sonnet-4-6

# Agregar un alias para un modelo (cambia el nombre visible en el picker)
openclaw models aliases add "Sonnet Latest" github-copilot/claude-sonnet-4-6
openclaw models aliases add "Haiku Fast"   github-copilot/claude-haiku-4.5

# Ver todos los aliases
openclaw models aliases list

# Remover un alias
openclaw models aliases remove "Haiku Fast"

# Agregar fallback
openclaw models fallbacks add github-copilot/claude-haiku-4.5

# Ver fallbacks
openclaw models fallbacks list

# Limpiar todos los fallbacks
openclaw models fallbacks clear

# Agregar modelo de imagen
openclaw models set-image github-copilot/claude-sonnet-4-5

# Fallbacks de imagen
openclaw models image-fallbacks add github-copilot/claude-haiku-4.5
```

### Agregar modelos al dropdown via config CLI (reemplaza el catálogo completo)

```bash
# Definir qué modelos aparecen en el picker con sus aliases
openclaw config set agents.defaults.models \
  '{"github-copilot/claude-haiku-4.5":{"alias":"Haiku"},"github-copilot/claude-sonnet-4-5":{"alias":"Sonnet 4.5"},"github-copilot/claude-sonnet-4-6":{"alias":"Sonnet 4.6"}}' \
  --strict-json

# Cambiar el modelo primario
openclaw config set agents.defaults.model.primary "github-copilot/claude-sonnet-4-6"

# Ver el catálogo actual
openclaw config get agents.defaults.models

# Limpiar el allowlist (muestra todos los modelos del proveedor)
openclaw config unset agents.defaults.models
```

### Cambiar modelo desde la UI sin reiniciar (slash command)

En el Chat de la Control UI o TUI:

```
/model                       # abre el model picker interactivo
/model list                  # lista modelos numerados
/model 3                     # selecciona el tercer modelo de la lista
/model github-copilot/claude-sonnet-4-6   # selecciona por ID
/model status                # estado detallado con auth
```

### Atajo de teclado (TUI)

```
Ctrl+L  →  Abre el Model Picker
```

### Escanear modelos disponibles (OpenRouter free)

```bash
# Escanear catálogo free de OpenRouter
openclaw models scan

# Sin probes (solo metadata)
openclaw models scan --no-probe

# Filtrar por tamaño mínimo de parámetros
openclaw models scan --min-params 7

# Filtrar por proveedor
openclaw models scan --provider anthropic

# Establecer el mejor resultado como modelo primario automáticamente
openclaw models scan --set-default
```

---

## 7. Dropdown de Agentes — Cómo modificarlo

El **Agent Picker** lista todos los agentes en `agents.list` de `openclaw.json`.

### Agregar un nuevo agente (aparece automáticamente en el dropdown)

```bash
# Agregar agente básico
openclaw agents add mi-agente \
  --workspace ~/.openclaw/workspace-mi-agente

# Agente con modelo específico
openclaw agents add qa-agent \
  --workspace ~/.openclaw/workspace-qa \
  --model github-copilot/claude-haiku-4.5

# Agente con channel binding (routing dedicado)
openclaw agents add work \
  --workspace ~/.openclaw/workspace-work \
  --bind telegram:ops \
  --bind discord:guild-a

# Agente non-interactivo (para scripts/CI)
openclaw agents add work \
  --workspace ~/.openclaw/workspace-work \
  --model github-copilot/claude-sonnet-4-6 \
  --non-interactive \
  --json
```

### Ver y gestionar agentes

```bash
# Listar todos los agentes (lo que aparece en el dropdown)
openclaw agents list

# Ver bindings de routing
openclaw agents bindings
openclaw agents bindings --agent work   # filtrado por agente
openclaw agents bindings --json

# Agregar binding de canal
openclaw agents bind --agent work --bind telegram:ops

# Quitar binding
openclaw agents unbind --agent work --bind telegram:ops
openclaw agents unbind --agent work --all   # quitar todos

# Eliminar agente
openclaw agents delete mi-agente
```

### Configuración directa en `openclaw.json`

```json5
{
  agents: {
    list: [
      // ─── Agente "main" (ya configurado) ───────────────────────────
      {
        id: "main",
        agentDir: "/Users/gregorymoreno/.openclaw/agents/main/agent",
        model: "github-copilot/claude-haiku-4.5"
      },

      // ─── Agente "dev-senior" (ya configurado) ─────────────────────
      {
        id: "dev-senior",
        name: "dev-senior",
        workspace: "/Users/gregorymoreno/.openclaw/agents/dev-senior/workspace",
        agentDir: "/Users/gregorymoreno/.openclaw/agents/dev-senior/agent",
        model: "github-copilot/claude-haiku-4.5",
        identity: {
          name: "QA-Senior",
          theme: "Auditor autónomo — un inspector de calidad incansable",
          emoji: "🔍",
          avatar: "_(none)"
        }
      },

      // ─── Agregar nuevo agente aquí ─────────────────────────────────
      {
        id: "okla-agent",
        name: "okla-agent",
        workspace: "/Users/gregorymoreno/.openclaw/agents/okla-agent/workspace",
        agentDir: "/Users/gregorymoreno/.openclaw/agents/okla-agent/agent",
        model: "github-copilot/claude-sonnet-4-6",
        identity: {
          name: "OKLA Agent",
          theme: "Especialista en marketplace de vehículos dominicano",
          emoji: "🚗",
          avatar: "_(none)"
        }
      }
    ]
  }
}
```

### Atajo de teclado (TUI)

```
Ctrl+G  →  Abre el Agent Picker
```

### Slash command

```
/agent <id>     # cambiar a ese agente
/agents         # listar y elegir interactivamente
```

---

## 8. Dropdown de Sesiones — Cómo modificarlo

El **Session Picker** muestra todas las sesiones del agente actual.

> Las sesiones se crean automáticamente al chatear. Pertenecen al agente actual.

### Gestión de sesiones

```bash
# En el TUI — cambiar o crear sesión
/session main                  # cambiar a sesión "main"
/session mi-sesion-nueva       # crear/cambiar a esa sesión
/session agent:work:main       # sesión de otro agente (explícito)
/sessions                      # lista todas las sesiones del agente actual

# Resetear sesión (nueva conversación)
/new
/reset

# La sesión siempre se muestra en el footer del TUI:
# agent:<agentId>:<sessionKey>
```

### Scope de sesiones

| Scope | Comportamiento |
|-------|---------------|
| `per-sender` | Cada agente tiene múltiples sesiones (default) |
| `global` | El TUI siempre usa la sesión `global` (el picker puede estar vacío) |

### Atajo de teclado (TUI)

```
Ctrl+P  →  Abre el Session Picker
```

---

## 9. Overrides de sesión desde la UI

Desde el panel **Sessions** de la Control UI (o con slash commands en el TUI), puedes modificar el comportamiento de **cada sesión individualmente**:

| Override | Valores | Descripción |
|----------|---------|-------------|
| `think` | `off`, `minimal`, `low`, `medium`, `high` | Nivel de razonamiento extendido |
| `fast` | `on`, `off` | Modo rápido (menor latencia) |
| `verbose` | `on`, `full`, `off` | Verbosidad de respuestas |
| `reasoning` | `on`, `off`, `stream` | Mostrar razonamiento en streaming |
| `usage` | `off`, `tokens`, `full` | Mostrar uso de tokens |
| `elevated` | `on`, `off`, `ask`, `full` | Nivel elevado de permisos |
| `activation` | `mention`, `always` | Cuándo el agente responde |
| `deliver` | `on`, `off` | Entregar respuestas al canal |

### Slash commands correspondientes (TUI y Chat UI)

```
/think medium          # establecer nivel de thinking
/fast on               # activar modo rápido
/verbose full          # máxima verbosidad
/reasoning stream      # mostrar razonamiento en tiempo real
/usage tokens          # mostrar conteo de tokens
/elevated ask          # pedir confirmación antes de ejecutar
/activation always     # siempre responder (sin necesidad de mención)
/deliver on            # activar delivery al canal
```

---

## 10. TUI — Terminal UI

La **TUI** (Terminal UI) es una interfaz de texto que conecta al Gateway. Útil para acceso remoto sin browser.

```bash
# Abrir TUI local
openclaw tui

# TUI con URL/token explícito (gateway remoto)
openclaw tui --url ws://127.0.0.1:18789 --token <token>

# TUI con sesión inicial
openclaw tui --session main

# TUI con delivery activado desde inicio
openclaw tui --deliver
```

### Layout de la TUI

```
┌─────────────────────────────────────────────────────────────┐
│ Header: URL ▸ agente ▸ sesión                                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Chat log                                                   │
│  (mensajes, respuestas, tool cards)                         │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ Status: connecting / running / streaming / idle / error     │
├─────────────────────────────────────────────────────────────┤
│ Footer: URL | agente | sesión | modelo | think | tokens     │
├─────────────────────────────────────────────────────────────┤
│ Input: > _                                                  │
└─────────────────────────────────────────────────────────────┘
```

### Atajos de teclado completos

| Shortcut | Acción |
|----------|--------|
| `Enter` | Enviar mensaje |
| `Esc` | Abortar run activo |
| `Ctrl+C` | Limpiar input (2x = salir) |
| `Ctrl+D` | Salir |
| `Ctrl+L` | **Model Picker** 🔑 |
| `Ctrl+G` | **Agent Picker** 🔑 |
| `Ctrl+P` | **Session Picker** 🔑 |
| `Ctrl+O` | Toggle tool output expanded/collapsed |
| `Ctrl+T` | Toggle thinking visibility |

### Variables de entorno para tema

```bash
# Para terminales con fondo claro
export OPENCLAW_THEME=light

# Para forzar el palette oscuro original
export OPENCLAW_THEME=dark
```

### Opciones de la TUI

| Flag | Descripción |
|------|-------------|
| `--url <url>` | URL del Gateway WS (default: `ws://127.0.0.1:<port>`) |
| `--token <token>` | Token del Gateway |
| `--password <password>` | Password del Gateway |
| `--session <key>` | Clave de sesión inicial (default: `main`) |
| `--deliver` | Activar delivery desde inicio |
| `--thinking <level>` | Override de thinking para sends |
| `--timeout-ms <ms>` | Timeout del agente |
| `--history-limit <n>` | Límite de historial (default: 200) |

---

## 11. Config CLI — Edición no-interactiva

La forma más precisa de modificar la UI es via el CLI de configuración:

```bash
# ─── Ver configuración actual ───────────────────────────────
openclaw config file                          # ruta del archivo
openclaw config get agents.defaults.models    # ver el catálogo de modelos
openclaw config get gateway.controlUi         # ver toda la config de la UI
openclaw config get agents.list               # ver todos los agentes

# ─── Modificar valores ─────────────────────────────────────
openclaw config set <path> <value>            # modo valor
openclaw config unset <path>                  # eliminar clave

# ─── Validar antes de aplicar ──────────────────────────────
openclaw config validate
openclaw config validate --json

# ─── Dry-run (validar sin escribir) ────────────────────────
openclaw config set gateway.controlUi.basePath "/test" --dry-run

# ─── Abrir el wizard interactivo ───────────────────────────
openclaw configure
openclaw configure --section model            # solo sección de modelos
openclaw configure --section channels         # solo canales
```

### Paths más usados

| Path | Qué controla |
|------|-------------|
| `agents.defaults.model.primary` | Modelo primario (default en el picker) |
| `agents.defaults.model.fallbacks` | Fallbacks en orden |
| `agents.defaults.models` | **Catálogo del Model Picker (allowlist)** |
| `agents.defaults.imageModel.primary` | Modelo para imágenes |
| `agents.list[0].model` | Modelo específico del agente 0 |
| `agents.list[0].identity.name` | Nombre visible del agente 0 |
| `agents.list[0].identity.emoji` | Emoji del agente 0 |
| `gateway.controlUi.enabled` | Habilitar/deshabilitar la UI |
| `gateway.controlUi.basePath` | Ruta base de la UI |
| `gateway.controlUi.allowedOrigins` | Orígenes permitidos |
| `gateway.port` | Puerto del gateway (default: 18789) |

---

## 12. Identidad del Agente (nombre, emoji, avatar)

La identidad del agente es lo que aparece en el **Agent Picker** y en el header de la UI.

### Configurar identidad via CLI

```bash
# Cargar desde IDENTITY.md en el workspace
openclaw agents set-identity \
  --workspace ~/.openclaw/workspace \
  --from-identity

# Sobrescribir campos directamente
openclaw agents set-identity \
  --agent main \
  --name "OKLA Bot" \
  --emoji "🚗" \
  --avatar avatars/okla.png

# Con tema descriptivo
openclaw agents set-identity \
  --agent dev-senior \
  --name "QA-Senior" \
  --emoji "🔍" \
  --theme "Auditor autónomo — inspector de calidad incansable"
```

### Configuración directa en `openclaw.json`

```json5
{
  agents: {
    list: [
      {
        id: "main",
        identity: {
          name: "OKLA Agent",          // nombre visible en el picker
          theme: "Asistente de vehículos para la República Dominicana",
          emoji: "🦞",                 // emoji en la UI
          avatar: "avatars/okla.png",  // ruta relativa al workspace, URL https, o data URI
        }
      }
    ]
  }
}
```

### Archivo IDENTITY.md (en el workspace del agente)

```markdown
# OKLA Agent

Especialista en el marketplace de vehículos OKLA de la República Dominicana.

**Emoji:** 🚗
**Avatar:** avatars/okla.png
```

---

## 13. Construcción y desarrollo de la UI

### Reconstruir la UI (si los assets están desactualizados)

```bash
# Construir los assets de la Control UI
pnpm ui:build
# Auto-instala deps de UI en el primer run

# Con basePath fijo (URLs absolutas en assets)
OPENCLAW_CONTROL_UI_BASE_PATH=/openclaw/ pnpm ui:build
```

### Servidor de desarrollo (para modificar la UI)

```bash
# Iniciar servidor de desarrollo (Vite, hot reload)
pnpm ui:dev
# Auto-instala deps en el primer run

# Apuntar al Gateway local
http://localhost:5173/?gatewayUrl=ws://127.0.0.1:18789

# Con token (via fragment para no exponer en logs)
http://localhost:5173/?gatewayUrl=ws://127.0.0.1:18789#token=<gateway-token>
```

### `gatewayUrl` y notas de desarrollo

- Se guarda en `localStorage` después de cargarse y se elimina de la URL
- `token` debe pasarse via fragment `#token=...` (no se envía al servidor)
- Solo aceptado en ventana top-level (no iframes — previene clickjacking)
- Para acceso remoto: **configurar** `gateway.controlUi.allowedOrigins` explícitamente

---

## 14. Configuración para OKLA

Configuración actual de `~/.openclaw/openclaw.json` para el proyecto OKLA:

```json5
// Estado actual: 2026-03-24
{
  meta: {
    lastTouchedVersion: "2026.3.23-beta.1",
  },
  browser: {
    enabled: true
  },
  agents: {
    defaults: {
      model: {
        primary: "github-copilot/claude-haiku-4.5"
      },
      // Sin allowlist definida → muestra TODO el catálogo del proveedor
      models: {}
    },
    list: [
      {
        id: "main",
        agentDir: "/Users/gregorymoreno/.openclaw/agents/main/agent",
        model: "github-copilot/claude-haiku-4.5"
      },
      {
        id: "dev-senior",
        name: "dev-senior",
        agentDir: "/Users/gregorymoreno/.openclaw/agents/dev-senior/agent",
        model: "github-copilot/claude-haiku-4.5",
        identity: {
          name: "QA-Senior",
          theme: "Auditor autónomo",
          emoji: "🔍",
          avatar: "_(none)"
        }
      }
    ]
  },
  gateway: {
    port: 18789,
    mode: "local",
    bind: "loopback",
    auth: {
      mode: "token",
      token: "f24fcea34191739b761db1153cefd910706083c1546c9c00"
    }
  }
}
```

### Cambios recomendados para OKLA

#### A. Agregar modelos al dropdown (con aliases legibles)

```bash
openclaw config set agents.defaults.models \
  '{"github-copilot/claude-haiku-4.5":{"alias":"Haiku 4.5 (Rápido)"},"github-copilot/claude-sonnet-4-5":{"alias":"Sonnet 4.5 (Balanceado)"},"github-copilot/claude-sonnet-4-6":{"alias":"Sonnet 4.6 (Latest)"}}' \
  --strict-json
```

#### B. Cambiar el modelo primario a Sonnet 4.6

```bash
openclaw config set agents.defaults.model.primary "github-copilot/claude-sonnet-4-6"
```

#### C. Agregar un fallback a Haiku

```bash
openclaw config set agents.defaults.model.fallbacks \
  '["github-copilot/claude-haiku-4.5"]' --strict-json
```

#### D. Abrir la Control UI ahora

```bash
openclaw dashboard
# → Abre http://127.0.0.1:18789 con el token del gateway auto-aplicado
```

---

## Referencias a la documentación oficial

| Sección | URL |
|---------|-----|
| Control UI | [docs.openclaw.ai/web/control-ui](https://docs.openclaw.ai/web/control-ui) |
| TUI | [docs.openclaw.ai/web/tui](https://docs.openclaw.ai/web/tui) |
| Config CLI | [docs.openclaw.ai/cli/config](https://docs.openclaw.ai/cli/config) |
| Models CLI | [docs.openclaw.ai/concepts/models](https://docs.openclaw.ai/concepts/models) |
| Agents CLI | [docs.openclaw.ai/cli/agents](https://docs.openclaw.ai/cli/agents) |
| Configure wizard | [docs.openclaw.ai/cli/configure](https://docs.openclaw.ai/cli/configure) |
| Config Reference | [docs.openclaw.ai/gateway/configuration-reference](https://docs.openclaw.ai/gateway/configuration-reference) |
| Providers + Modelos | [docs.openclaw.ai/providers/models](https://docs.openclaw.ai/providers/models) |
| Devices (pairing) | [docs.openclaw.ai/cli/devices](https://docs.openclaw.ai/cli/devices) |
| Skills | [docs.openclaw.ai/tools/skills](https://docs.openclaw.ai/tools/skills) |

---

*Documentación generada: 2026-03-24 — Basada en `openclaw@2026.3.23-beta.1` y [docs.openclaw.ai](https://docs.openclaw.ai)*
