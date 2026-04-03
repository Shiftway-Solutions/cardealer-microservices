# Bypass Approvals Mode — Guía de Activación

**Modalidad**: Auto-aprueba todas las herramientas sin diálogos + Auto-retry en errores  
**Ambiente**: Controlado (máquina local) ← máxima autonomía  
**Status**: ✅ Configurado

---

## Resumen rápido

Tienes **2 formas** de usar Bypass Approvals en Copilot:

### Forma 1️⃣ — Manual (sin daemon)

```bash
# Copiar el prompt de loop infinito
cat .prompts/AGENT_LOOP_PROMPT.md | pbcopy

# En VS Code:
# 1. Abre Copilot Agent Chat (Ctrl+Shift+I)
# 2. Pega el contenido
# 3. El agente ejecuta el loop en modalidad Bypass Approvals
```

**Características**:

- No hay diálogos de confirmación
- Reintenta automáticamente en errores (hasta 3 veces)
- El loop continúa indefinidamente hasta que escribas `STOP` en `.prompts/prompt_1.md`

---

### Forma 2️⃣ — Daemon (automático)

```bash
source .venv/bin/activate
python3 .prompts/prompt_loop_daemon.py
```

**Lo que hace**:

- Monitorea `.prompts/prompt_1.md` cada 60-900 segundos
- Detecta cambios automáticamente
- Despacha tareas al agente Copilot en modalidad Bypass Approvals
- El daemon nunca se detiene — solo termina si escribes `STOP`

---

## Configuración implementada

### En `~/Library/Application Support/Code/User/settings.json` (Global)

```json
// BYPASS APPROVALS MODE
"chat.tools.global.autoApprove": true,
"chat.tools.global.autoRetry": true,
"chat.tools.global.retryCount": 3,

"chat.tools.terminal.enableAutoApprove": true,
"chat.tools.terminal.autoRetry": true,
"chat.tools.terminal.retryCount": 3,

"github.copilot.chat.agent.autoApplyEdits": true,
"github.copilot.chat.agent.bypassApprovalsMode": "always",
"github.copilot.chat.agent.autoRetry": true,

"chat.editing.confirmEditRequestRemoval": false,
"chat.editing.confirmEditRequestRetry": false,
"chat.editing.confirmBeforeApply": false,
```

### En `.github/agents/bypass-approvals.agent.md` (Workspace)

Archivo de agente personalizado que define el comportamiento Bypass Approvals.

---

## Flujo de Bypass Approvals

```
┌─ Tarea recibida
│
├─ Ejecuta herramienta (terminal, file edit, etc.)
│
├─¿Error?
│ ├─ NO → Continúa
│ └─ SÍ → Reintenta (hasta 3 veces, delay 2s)
│
├─¿3 reintentos fallaron?
│ ├─ NO → Resultado OK, sigue
│ └─ SÍ → Documenta error, busca alternativa, continúa IGUAL
│
└─ ¿Más tareas?
   ├─ SÍ → Vuelve a ejecutar herramienta
   └─ NO → Espera siguiente prompt
```

### Punto clave: Sin paradas por error

Si falla una herramienta → intenta 3 veces → si sigue fallando → **documenta y continúa**.  
**Nunca** pausa esperando confirmación del usuario.

---

## Deteniendo el agente

### Opción A — Escribir STOP

```bash
echo "STOP" >> .prompts/prompt_1.md
```

El agente verá `STOP` como última línea y detendrá el loop automáticamente.

### Opción B — Ctrl+C en terminal (si usas daemon)

```bash
# Si ejecutaste: python3 .prompts/prompt_loop_daemon.py
# Simplemente presiona Ctrl+C
```

---

## Auditoría

Todas las acciones del agente se registran en `.github/copilot-audit.log`:

```bash
tail -50 .github/copilot-audit.log
```

Formato:

```
[2026-04-03 10:15:32] [TIPO] OBJETIVO — DESCRIPCIÓN
[2026-04-03 10:15:45] [EJECUCIÓN] Gate Pre-Commit — paso 2/4 (dotnet build)
[2026-04-03 10:16:02] [MODIFICACIÓN] backend/Service/Handler.cs — Fix X
[2026-04-03 10:16:45] [GIT] git push origin feature/nombre — Enviando
```

---

## Troubleshooting

| Problema                                     | Solución                                                                                     |
| -------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Agente sigue pidiendo confirmación           | Reinicia VS Code, verifica `settings.json` global fue guardado                               |
| Terminal commands se aprovaron pero fallaron | Bypass Approvals reintenta hasta 3 veces automáticamente, revisa `.github/copilot-audit.log` |
| Daemon no detecta cambios en prompt_1.md     | Reinicia daemon, verifica ruta `.prompts/prompt_1.md` existe                                 |
| ¿Cómo veo qué hace el agente?                | Abre `.github/copilot-audit.log` en tiempo real con `tail -f`                                |

---

## Notas Importantes

⚠️ **Ambiente controlado**: Bypass Approvals tiene permisos máximos LOCALES. No usar en máquinas compartidas sin revisar permisos.

⚠️ **Archivo de tareas**: Las tareas viven en `.prompts/prompt_1.md`. El agente lee automáticamente y ejecuta.

⚠️ **No hay "pausa manual"**: Una vez activo, Bypass Approvals no se detiene hasta `STOP`. Si quieres pausar temporalmente → escribe `READ` al final de `prompt_1.md`.

✅ **Mejor práctica**: Usa el daemon `prompt_loop_daemon.py` para automonitoreo continuo entre sesiones de VS Code.

---

**Implementado**: 2026-04-03  
**Modelo base**: GitHub Copilot Claude Haiku 4.5  
**Versión Bypass Approvals**: 1.0
