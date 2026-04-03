# Copilot Agent Monitor v1.3.7 — Secuencia de Comandos por Acción

> Documento de auditoría: cada acción del agente con la secuencia exacta de
> comandos VS Code, llamadas API, y operaciones I/O que ejecuta.
>
> **Política 0x:** El brain NUNCA usa modelos 1x. PREMIUM GUARD bloquea envíos en 1x.
> **No-blocking:** Ninguna operación bloquea el ciclo del monitor.

---

## Flujo General del Ciclo

```
LogWatcher(fs.watch) → _onLogDelta() → _scheduleNextCycle(2-5s)
                                  ↓
                          _runCycle() [concurrency-guarded]
                                  ↓
           ┌──────────────────────┴──────────────────────┐
           │ _observeState()                              │
           │   1. Check recovery window                   │
           │   2. _stateFromLog() (log-first)             │
           │   3. Screenshot if stall suspected           │
           └──────────────────────┬──────────────────────┘
                                  ↓
           ┌──────────────────────┴──────────────────────┐
           │ StateMachine.decide(input) → {action, reason}│
           │   Pure function — no side effects             │
           └──────────────────────┬──────────────────────┘
                                  ↓
                    action === WAIT? → return (no audit)
                                  ↓
           ┌──────────────────────┴──────────────────────┐
           │ _preActionGate(action)                       │
           │   Solo SEND_CONTINUE / OPEN_NEW_CHAT         │
           │   Screenshot → GENERATING? → BLOCK           │
           └──────────────────────┬──────────────────────┘
                                  ↓
           ┌──────────────────────┴──────────────────────┐
           │ CostGuard.check()                            │
           │   ≤8 acciones/5min? → proceed                │
           │   >8? → BLOCK + fire-and-forget notification │
           └──────────────────────┬──────────────────────┘
                                  ↓
           ┌──────────────────────┴──────────────────────┐
           │ ActionExecutor.execute(action)               │
           │   → _activeAbort = new AbortController()     │
           │   → _executeInner(action)                    │
           │   → finally: _activeAbort = null             │
           └──────────────────────┬──────────────────────┘
                                  ↓
           ┌──────────────────────┴──────────────────────┐
           │ _postAction(action, ok, prevState)           │
           │   Stamp cooldown, update counters, → RECOVERING│
           └─────────────────────────────────────────────┘
```

---

## 1. VALIDATE_ZERO_X (Health Check)

**Trigger:** Rate limit, hard error 2+ retries, or called by SEND_CONTINUE/OPEN_NEW_CHAT.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
1. _auditor.captureToFile("before_validateZeroX") | screencapture | NO (async)  | ~500ms
2. ModelManager.applyBestZeroX()                  | vscode.lm API | NO (async)  | ~50ms
   → vscode.commands.executeCommand               |               |             |
     ("workbench.action.chat.changeModel",        |               |             |
      { vendor, id, family })                     |               |             |
3. _verifyModelSelection(model, 3000)             | Poll loop     | NO (async)  | 0-3s
   → _readConfiguredModel() [settings read]       | sync read     | (µs)        |
   → _readModelIndicatorViaCDP() [CDP getText]    | WebSocket     | NO (async)  | ~100ms
   → _sleep(250) [cancellable]                    | setTimeout    | NO          | 250ms
4. _sleep(800) [cancellable]                      | setTimeout    | NO          | 800ms
5. [mode="new-chat"] CMD_CHAT_NEW                 | executeCommand| NO (async)  | ~50ms
6. [mode="new-chat"] _sleep(1500) [cancellable]   | setTimeout    | NO          | 1500ms
7. vscode.lm.selectChatModels({vendor, family})   | vscode.lm API | NO (async)  | ~100ms
8. lmModel.sendRequest(                           | LM API stream | NO (async)  | 2-8s
     "¿Estás funcionando? Responde SI o NO.",     |               |             |
     {justification: "0x health check"},          |               |             |
     cancellationToken [15s timeout])             |               |             |
9. Stream response → check /\bsi\b|\byes\b/      | String parse  | (µs)        |
10a. [SI + new-chat] CMD_CHAT_NEW                 | executeCommand| NO (async)  | ~50ms
10b. [SI + new-chat] _sleep(1500) [cancellable]   | setTimeout    | NO          | 1500ms
10c. [SI + new-chat] _sendToChat(loopPrompt)      | CLI/clipboard | NO (async)  | 1-5s
10d. [SI + continue] _sendToChat("continuar")     | CLI/clipboard | NO (async)  | 1-5s
11.  [NO] return {ok:false}                       | -             | -           | 0ms
```

**PREMIUM GUARD** en paso 10c/10d:

```
→ ModelManager.lastAppliedModel.isFree === false?
  → BLOCK: log error + recordPremiumPrompt() + showErrorMessage (fire-and-forget)
  → return false
```

**Total latencia**: 5-15s (mayoría es la respuesta LM del health check)
**Comandos bloqueantes**: NINGUNO — todos son async o cancellable

---

## 2. SEND_CONTINUE ("continuar" en mismo chat)

**Trigger:** Soft stall (5 min inactivity), hard error retry 1-3.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[PRE-ACTION GATE]
1. ScreenAnalyzer.analyze(token)                  | screencapture | NO (async)  | 2-5s
   → exec("screencapture -x /tmp/...")            | child_process | NO (async)  |
   → readFileSync(tmpFile) [in callback]          | sync read     | (µs)        |
   → fs.unlinkSync(tmpFile) [in callback]         | sync delete   | (µs)        |
   → vscode.lm.sendRequest(screenshot, prompt)    | LM vision API | NO (async)  |
2. Gate result:
   → GENERATING? → BLOCK + reset stall timer → return
   → COMPLETED + action=SEND_CONTINUE? → BLOCK → return
   → Low confidence? → ALLOW with warning
   → OK? → proceed

[ACTION — _validateZeroX("continue-same-chat")]
3. ModelManager.applyBestZeroX()                  | vscode API    | NO (async)  | ~50ms
   → executeCommand("changeModel", {vendor,id,family})
4. _verifyModelSelection(model, 3000)             | Poll loop     | NO (async)  | 0-3s
5. _sleep(800) [cancellable]                      | setTimeout    | NO          | 800ms
6. vscode.lm.selectChatModels(...)                | vscode.lm     | NO (async)  | ~100ms
7. lmModel.sendRequest("Responde SI o NO")        | LM API stream | NO (async)  | 2-8s
8. [SI] _sendToChat("continuar")                  | code chat CLI | NO (async)  | 1-5s
   → PREMIUM GUARD check                         |               |             |
   → execFile("code", ["chat","--mode","agent",   |               |             |
     "--reuse-window","continuar"], {timeout:20s}) |               |             |

[POST-ACTION]
9. _cooldowns.sendContinue = Date.now()
10. _softErrorRetries++
11. _lastActivityMs = now
12. _recoveryStartMs = now
13. _currentState = RECOVERING
14. _latestLogEvent = IDLE
```

**Comandos bloqueantes**: NINGUNO

---

## 3. OPEN_NEW_CHAT (nuevo chat + loop prompt)

**Trigger:** COMPLETED + 90s grace, hard stall (8 min), hard error 3+ retries.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[PRE-ACTION GATE — same as SEND_CONTINUE #1-#2]

[ACTION — _validateZeroX("new-chat")]
1. ModelManager.applyBestZeroX()                  | vscode API    | NO (async)  | ~50ms
2. _verifyModelSelection(model, 3000)             | Poll loop     | NO (async)  | 0-3s
3. _sleep(800) [cancellable]                      | setTimeout    | NO          | 800ms
4. CMD_CHAT_NEW                                   | executeCommand| NO (async)  | ~50ms
   → "workbench.action.chat.newChat"
5. _sleep(1500) [cancellable]                     | setTimeout    | NO          | 1500ms
6. vscode.lm.selectChatModels(...)                | vscode.lm     | NO (async)  | ~100ms
7. lmModel.sendRequest("SI o NO")                 | LM API        | NO (async)  | 2-8s
8. [SI] CMD_CHAT_NEW                              | executeCommand| NO (async)  | ~50ms
   → Abre OTRO chat limpio para el work prompt
9. _sleep(1500) [cancellable]                     | setTimeout    | NO          | 1500ms
10. _sendToChat(loopPrompt)                       | code chat CLI | NO (async)  | 1-5s
    → PREMIUM GUARD check
    → execFile("code", ["chat","--mode","agent",
      "--reuse-window", loopPrompt], {timeout:20s})

[POST-ACTION]
11. _cooldowns.openNewChat = now
12. _newChatCount++
13. _softErrorRetries = 0
14. _currentState = RECOVERING
```

**Comandos bloqueantes**: NINGUNO

---

## 4. STOP_AND_NEW_CHAT (detener generación + nuevo chat)

**Trigger:** Context window full (ERROR_CONTEXT).

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[NO PRE-ACTION GATE — triggered by confirmed log error]

[ACTION — _stopAndNewChat()]
1. _clickViaCDP("stopButton")                     | CDP WebSocket | NO (async)  | ~200ms
   → Fallback: CMD_CHAT_STOP                      | executeCommand| NO (async)  | ~50ms
     → "workbench.action.chat.stop"
2. _sleep(1500) [cancellable]                     | setTimeout    | NO          | 1500ms
3. → _openNewChat() [full VALIDATE_ZERO_X flow]   |               |             |
   → applyBestZeroX()                             |               |             |
   → _verifyModelSelection()                      |               |             |
   → _sleep(800)                                  |               |             |
   → CMD_CHAT_NEW                                 |               |             |
   → _sleep(1500)                                 |               |             |
   → LM health check                              |               |             |
   → [SI] CMD_CHAT_NEW + _sleep(1500)             |               |             |
   → _sendToChat(loopPrompt)                      |               |             |

[POST-ACTION]
4. _cooldowns.stopAndNewChat = now
5. _cooldowns.openNewChat = now (both stamped)
6. _newChatCount++
7. _softErrorRetries = 0
8. _currentState = RECOVERING
```

**Comandos bloqueantes**: NINGUNO

---

## 5. SWITCH_CHAT_MODEL (cambiar modelo en mismo chat)

**Trigger:** Rate limit + health OK, or model switch suggested.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[NO PRE-ACTION GATE — triggered by confirmed log error]

[ACTION — _switchChatModel()]
→ CON ModelManager (camino principal):
1. ModelManager.zeroXModels                       | Property read | (µs)        |
   → Round-robin: (currentIdx+1) % length
2. ModelManager.applyModel(nextModel.id)          | vscode API    | NO (async)  | ~50ms
   → executeCommand("changeModel", {vendor,id,family})

→ SIN ModelManager (fallback — 3 tiers):
Tier 0b:
1. CMD_CHAT_CHANGE_MODEL({modelId})               | executeCommand| NO (async)  | ~50ms
   → "workbench.action.chat.changeModel"
2. _verifyModelSelection(4000)                    | Poll loop     | NO (async)  | 0-4s

Tier 1 (QuickPick):
1. CMD_CHAT_CHANGE_MODEL (sin args — abre picker) | executeCommand| NO (async)  | ~50ms
2. _sleep(600) [cancellable]                      | setTimeout    | NO          | 600ms
3. "workbench.action.type" {text: displayName}    | executeCommand| NO (async)  | ~50ms
4. _sleep(500) [cancellable]                      | setTimeout    | NO          | 500ms
5. "workbench.action.acceptSelectedQuickOpenItem" | executeCommand| NO (async)  | ~50ms
6. _sleep(500) [cancellable]                      | setTimeout    | NO          | 500ms
7. "workbench.action.type" {text: displayName}    | executeCommand| NO (async)  | ~50ms
8. _sleep(400) [cancellable]                      | setTimeout    | NO          | 400ms
9. "workbench.action.acceptSelectedQuickOpenItem" | executeCommand| NO (async)  | ~50ms
10. _sleep(600) [cancellable]                     | setTimeout    | NO          | 600ms
11. _verifyModelSelection(4000)                   | Poll loop     | NO (async)  | 0-4s

Tier 2 (CDP):
1. _clickViaCDP("modelPickerButton")              | CDP WebSocket | NO (async)  | ~200ms
2. _sleep(500) [cancellable]                      | setTimeout    | NO          | 500ms
3. cdp.clickByText([displayName])                 | CDP WebSocket | NO (async)  | ~200ms
4. [no match] cdp.clickByText(["Other Models"])   | CDP WebSocket | NO (async)  | ~200ms
5. _sleep(400) [cancellable]                      | setTimeout    | NO          | 400ms
6. cdp.clickByText([displayName])                 | CDP WebSocket | NO (async)  | ~200ms
7. _sleep(600) [cancellable]                      | setTimeout    | NO          | 600ms
8. _verifyModelSelection(3000)                    | Poll loop     | NO (async)  | 0-3s

[POST-ACTION]
. _cooldowns.switchChatModel = now
. _modelRotations++
. _softErrorRetries = 0
. _currentState = RECOVERING
```

**Comandos bloqueantes**: NINGUNO

---

## 6. CYCLE_MODEL (rotar modelo + nuevo chat)

**Trigger:** Rate limit + SWITCH on cooldown.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[NO PRE-ACTION GATE]

[ACTION — _cycleModel()]
→ CON ModelManager:
1. ModelManager.zeroXModels → round-robin nextIdx | Property read | (µs)        |
2. ModelManager.applyModel(nextModel.id)          | vscode API    | NO (async)  | ~50ms
3. → _finalizeModelSwitch(true)                   |               |             |
   → _validateZeroX("new-chat")                   |               |             |
   → [full health check flow — see #1 VALIDATE_ZERO_X]

→ SIN ModelManager:
1. _syncModelIndexFromConfiguredModel()            | settings read| (µs)        |
2. _selectModelInPicker(nextModel, openNewChat=true)| 3-tier      | NO (async)  | 2-10s
   → [Tier 0a/0b/1/2 — see #5 SWITCH_CHAT_MODEL]  |             |             |
   → _finalizeModelSwitch(true)                     |             |             |
     → CMD_CHAT_NEW                                 |             |             |
     → _sleep(1500) [cancellable]                   |             |             |
     → _sendToChat(loopPrompt)                      |             |             |

[POST-ACTION]
. _cooldowns.cycleModel = now
. _cooldowns.openNewChat = now
. _modelRotations++
. _softErrorRetries = 0
. _currentState = RECOVERING
```

**Comandos bloqueantes**: NINGUNO

---

## 7. FOCUS_VSCODE (traer VS Code al frente)

**Trigger:** VSCODE_HIDDEN state detected by screenshot.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[NO PRE-ACTION GATE]

[ACTION — _focusVSCode()]
Tier 1 (CDP):
1. _clickViaCDP("chatInput")                      | CDP WebSocket | NO (async)  | ~200ms

Tier 2 (fallback):
1. CMD_CHAT_OPEN                                  | executeCommand| NO (async)  | ~50ms
   → "workbench.action.chat.open"
2. _sleep(300) [cancellable]                      | setTimeout    | NO          | 300ms
3. CMD_CHAT_FOCUS_INPUT                           | executeCommand| NO (async)  | ~50ms
   → "workbench.action.chat.focusInput"

[POST-ACTION — no cooldown stamp for FOCUS_VSCODE]
```

**Comandos bloqueantes**: NINGUNO

---

## 8. \_sendToChat() — Entrega de mensajes

**Usado por:** SEND_CONTINUE, OPEN_NEW_CHAT, STOP_AND_NEW_CHAT, CYCLE_MODEL, VALIDATE_ZERO_X.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
[PREMIUM GUARD]
1. ModelManager.lastAppliedModel                  | Property read | (µs)        |
   → isFree === false?                            |               |             |
   → BLOCK: log error + recordPremiumPrompt()     |               |             |
   → showErrorMessage (fire-and-forget)           |               |             |
   → return false                                 |               |             |

   → isFree === true? → proceed                   |               |             |

[Tier 1: CLI]
2. execFile("code",                               | child_process | NO (async)  | 1-20s
     ["chat","--mode","agent",                    |               |             |
      "--reuse-window", message],                 |               |             |
     {cwd: workspaceRoot, timeout: 20_000})       |               |             |

[Tier 2: Clipboard fallback — solo si CLI falla]
3. showWarningMessage("clipboard fallback...")     | fire-and-forget| NO         | 0ms
4. CMD_CHAT_FOCUS_INPUT                           | executeCommand| NO (async)  | ~50ms
5. _sleep(400) [cancellable]                      | setTimeout    | NO          | 400ms
6. vscode.env.clipboard.writeText(message)        | clipboard API | NO (async)  | ~10ms
7. "editor.action.selectAll"                      | executeCommand| NO (async)  | ~10ms
8. "editor.action.clipboardPasteAction"           | executeCommand| NO (async)  | ~10ms
9. _sleep(300) [cancellable]                      | setTimeout    | NO          | 300ms
10. "workbench.action.chat.submit"                | executeCommand| NO (async)  | ~50ms
```

**Comandos bloqueantes**: NINGUNO

---

## 9. Pre-Action Gate (Screenshot Visual)

**Aplica a:** SEND_CONTINUE, OPEN_NEW_CHAT solamente.
**No aplica a:** STOP_AND_NEW_CHAT, VALIDATE_ZERO_X, CYCLE_MODEL, SWITCH_CHAT_MODEL, FOCUS_VSCODE.

```
Comando                                          | Tipo          | Bloqueante | Duración
─────────────────────────────────────────────────────────────────────────────────────────
1. ScreenAnalyzer.canCapture()                    | Property check| (µs)        |
   → Quota exhausted? → ALLOW (fail-open)
2. screencapture -x /tmp/cam-XXX.png              | exec          | NO (async)  | ~500ms
3. readFileSync(tmpFile) [in exec callback]       | sync FS       | (µs)        |
4. unlinkSync(tmpFile) [in exec callback]         | sync FS       | (µs)        |
5. base64 encode → LM vision request              | vscode.lm     | NO (async)  | 2-5s
6. Parse response → derive AgentState + confidence| String parse  | (µs)        |
7. Decisions:
   → GENERATING (conf ≥0.5)? → BLOCK + reset stall timer
   → COMPLETED + SEND_CONTINUE? → BLOCK + update state
   → conf < 0.5? → ALLOW with warning
   → Exception? → ALLOW (fail-open)
```

---

## 10. CostGuard — Circuit Breaker

**Aplica a:** Toda acción != WAIT, después del pre-action gate.

```
check() {
  1. dropOlderThan(windowMs)           | Array filter   | (µs)
  2. count = actions.length            | Property read  | (µs)
  3. count >= max?                     |                |
     → justTripped (first time only)?  |                |
       → showErrorMessage (FIRE-AND-FORGET — no await)
     → return {allowed: false}         |                |
  4. count < max?                      |                |
     → push(now)                       |                |
     → return {allowed: true}          |                |
}
```

**Antes (v1.3.7):**

```typescript
// ❌ BLOQUEANTE — await en showErrorMessage detenía el ciclo
const choice = await vscode.window.showErrorMessage(...);
```

**Después (v1.3.8 fix):**

```typescript
// ✅ NO BLOQUEANTE — fire-and-forget con .then()
vscode.window.showErrorMessage(...).then((choice) => {
  if (choice === "Reset Guard") this._costGuard.reset();
});
```

---

## 11. NO_ZERO_X_MODEL — Monitor Stop

```
Antes (v1.3.7):
  1. this.stop()
  2. await vscode.window.showErrorMessage({modal: true})  ← ❌ BLOQUEANTE
  3. return

Después (v1.3.8 fix):
  1. this.stop()
  2. vscode.window.showErrorMessage({modal: true})  ← ✅ fire-and-forget
  3. return
```

---

## Resumen de Bloqueos Corregidos

| Ubicación                           | Antes (bloqueante)                           | Después (no bloqueante)                    |
| ----------------------------------- | -------------------------------------------- | ------------------------------------------ |
| Monitor.\_runCycle → CostGuard trip | `const choice = await showErrorMessage(...)` | `showErrorMessage(...).then(...)`          |
| Monitor.\_runCycle → NO_ZERO_X      | `await showErrorMessage({modal:true})`       | `showErrorMessage({modal:true})` sin await |

---

## Inventario Completo de Sync I/O

Operaciones síncronas que bloquean el event loop de Node.js (todas <1ms):

| Archivo        | Operación                                                | Contexto                             | Impacto                  |
| -------------- | -------------------------------------------------------- | ------------------------------------ | ------------------------ |
| LogWatcher     | `statSync`, `openSync`, `readSync`, `closeSync`          | `_readNewBytes()` cada 50ms debounce | <100µs — archivo local   |
| AuditLog       | `appendFileSync`, `existsSync`, `statSync`, `renameSync` | `record()` por ciclo con acción      | <500µs — una línea JSONL |
| AuditLog       | `readFileSync`                                           | `tail()` — solo en UI commands       | No en hot path           |
| SelectorStore  | `readFileSync`, `writeFileSync`, `existsSync`            | init + save — solo en wizard         | No en hot path           |
| ScreenAnalyzer | `readFileSync`, `unlinkSync`                             | Dentro de exec callback              | <100µs — temp file       |
| ActionExecutor | `existsSync`, `readFileSync`                             | `_readLoopPrompt()` — por acción     | <100µs — MD file         |
| ModelManager   | `writeFileSync`                                          | `_persistCache()` — on refresh       | No en hot path           |

**Veredicto**: Todos los sync I/O son operaciones locales de <1ms. No son bloqueantes en la práctica.
Los únicos bloqueos reales eran los `await showErrorMessage()` modals — **ya corregidos**.

---

## Todos los \_sleep() Son Cancellables

```typescript
// v1.3.7 — AbortController pattern
private _sleep(ms: number): Promise<void> {
  const signal = this._activeAbort?.signal;
  if (!signal) return new Promise(r => setTimeout(r, ms));
  if (signal.aborted) return Promise.resolve();
  return new Promise(resolve => {
    const timer = setTimeout(resolve, ms);
    signal.addEventListener("abort", () => {
      clearTimeout(timer);
      resolve();
    }, { once: true });
  });
}

// Cancelar acción en curso:
cancelCurrentAction() → _activeAbort.abort() → todos los _sleep() resuelven inmediatamente
```

**14 llamadas a \_sleep() en ActionExecutor — todas cancellables:**

- `_verifyModelSelection`: 250ms (polling loop)
- `_finalizeModelSwitch`: 1500ms
- `_stopAndNewChat`: 1500ms
- `_selectModelInPicker` Tier 1: 600 + 500 + 500 + 400 + 600ms
- `_selectModelInPicker` Tier 2: 500 + 400 + 600ms
- `_validateZeroX`: 800 + 1500ms
- `_sendViaClipboard`: 400 + 300ms
- `_focusVSCode`: 300ms
