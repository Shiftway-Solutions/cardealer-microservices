# Copilot Model Cycler v3.0

Plugin de VS Code que mantiene el agente trabajando continuamente:
detecta rate limits y errores, cicla modelos automáticamente,
y re-envía el AGENT_LOOP_PROMPT al abrir un nuevo chat.

---

## Instalación

```bash
cp -r copilot-model-cycler \
  ~/.vscode/extensions/copilot-model-cycler-3.0.0
```

`⇧⌘P` → **Developer: Restart Extension Host**

---

## Los 3 escenarios y qué hace el plugin

### 1. Rate Limit: `⌘⇧L`
```
Error: "You've hit the rate limit for this model..."
         │
         ▼
⌘⇧L (o automático si detectado en logs)
         │
         ├─→ Cicla al siguiente modelo (Opus 4.6 → Opus 4.5 → Sonnet 4.6...)
         ├─→ NO abre nuevo chat
         └─→ Envía "Continuar" en el chat actual
```

### 2. Error en chat: `⌘⇧E`
```
Cualquier error que corta el agente
         │
         ▼
⌘⇧E (o automático si detectado en logs)
         │
         ├─→ Vuelve a Opus 4.6 (el más poderoso)
         ├─→ Abre nuevo chat
         └─→ Lee .prompts/prompt_1.md y lo envía completo al nuevo chat
```

### 3. Límite de mensajes (automático)
```
Cada Enter en el chat → contador ++
Al llegar a maxMessages (default: 25)
         │
         ├─→ Vuelve a Opus 4.6
         ├─→ Abre nuevo chat
         └─→ Lee .prompts/prompt_1.md y lo envía completo al nuevo chat
```

---

## Setup crítico — settings.json

```jsonc
// El archivo que se enviará como prompt al abrir nuevo chat
// Ruta relativa a la raíz del workspace
"modelCycler.session.promptFilePath": ".prompts/prompt_1.md",

// Modelos en orden de más a menos poderoso
"modelCycler.models": [
  "claude-opus-4-6",
  "claude-opus-4-5",
  "claude-sonnet-4-6",
  "claude-sonnet-4-5",
  "claude-sonnet-4"
],

// Límite de mensajes antes de auto-reset
"modelCycler.session.maxMessages": 25,

// Al resetear → "top" vuelve a Opus 4.6 siempre
"modelCycler.session.resetModel": "top",

// Rate limit → ciclar al "next" (siguiente) o "top" (más poderoso)
"modelCycler.session.rateLimitCycleTo": "next",

// Delay antes de enviar el prompt al nuevo chat (ms)
"modelCycler.session.continuationDelay": 1000
```

---

## Atajos de teclado

| Acción | Mac | Windows |
|---|---|---|
| **Rate limit** → ciclar modelo + "Continuar" | `⌘⇧L` | `Ctrl+Shift+L` |
| **Error chat** → nuevo chat + AGENT_LOOP_PROMPT | `⌘⇧E` | `Ctrl+Shift+E` |
| Nuevo chat manual + AGENT_LOOP_PROMPT | `⌘⇧R` | `Ctrl+Shift+R` |
| Siguiente modelo | `⌘⇧.` | `Ctrl+Shift+.` |
| Modelo anterior | `⌘⇧,` | `Ctrl+Shift+,` |
| Elegir modelo | `⌘⇧M` | `Ctrl+Shift+M` |

---

## Barra de estado

`🤖 Opus 4.6 ⚡⚡⚡  💬8/25  [1/5]`

- Verde → sesión normal
- Amarillo → >75% del límite
- Rojo → límite alcanzado

Click → estado completo (incluyendo si el archivo de prompt existe ✅ o no ❌)

---

## Cómo se envía el prompt

El plugin intenta 3 métodos en orden:

1. `workbench.action.chat.open` con `{ query: contenido }` (nativo VS Code)
2. Clipboard → focus → paste → acceptInput (fallback robusto)
3. Botón "Copiar al clipboard" para pegado manual (último recurso)

---

## Monitoreo automático de logs

El plugin intenta detectar rate limits y errores automáticamente
leyendo los logs de VS Code (intervalo configurable):

```jsonc
"modelCycler.monitoring.pollInterval": 3000  // ms, 0 = desactivado
```

Si el auto-detect no funciona en tu entorno, usa los shortcuts
manuales `⌘⇧L` y `⌘⇧E` cuando veas el error en pantalla.

---

## Diagnóstico

`⇧⌘P` → **Output: Show Output Channels** → **Copilot Model Cycler**

Verás cada mensaje contado, cada trigger de rate limit/error,
y cada prompt enviado con su método.
