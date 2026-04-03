# 🧪 TEST: Copilot Agent Monitor — Activity Detection

## ✅ Estado de la Extensión

```
✅ okla-dev.copilot-agent-monitor v1.3.9 — Instalada
```

## 🚀 Cómo probar la detección de actividad

### Paso 1: Iniciar VS Code con debugging

```bash
code --remote-debugging-port=9222 /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices
```

### Paso 2: En VS Code, activar el Monitor

- Pulsa `Cmd+Shift+P`
- Busca: **"Copilot Monitor: Start"**
- Presiona Enter
- Verifica la barra de estado (abajo a la derecha) — debe mostrar un ícono

### Paso 3: Abrir Copilot Chat

- Pulsa `Cmd+Shift+L` (o abre el panel de Chat manualmente)
- Esto abre el panel de Copilot Chat de GitHub

### Paso 4: Generar actividad — Envía una de estas solicitudes

**Opción A (corta, ~5-15 seg):**

```
Escribe un fixture de Vitest para testing una función que valida emails
```

**Opción B (larga, ~20-40 seg, recomendada):**

```
Analiza el patrón CQRS en backend/UserService/src/Application
Explica cómo se separan Commands de Queries y por qué
```

**Opción C (inmediata, ~2 seg):**

```
Qué es Event Sourcing en .NET?
```

### Paso 5: Observar mientras procesa

#### En la **barra de estado** (abajo a la derecha):

- **Durante procesamiento**: Ícono giratorio `(loading~spin)` + texto dinámico
  - "DOM: Thinking..."
  - "DOM: Processing..."
  - "DOM: Analyzing backend/"
  - Texto específico del análisis

- **Cuando termina**: Ícono de check `(check)` + "COMPLETED"

- **Si falla o queda inactivo**: Ícono de error rojo

#### En el **Activity Log**:

- Pulsa `Cmd+Shift+P`
- Busca: **"Copilot Monitor: Show Activity Log"**
- Deberías ver entradas como:
  ```
  🔭 DOM(12 events)  — Thinking...
  🔭 DOM: active
  DOM: Searching... (123 ms ago, 2 events, ✅)
  ```

#### En el **Output Channel** (debug detallado):

- Pulsa `Cmd+Shift+U`
- Selecciona: **"Copilot Agent Monitor"**
- Busca líneas como:
  ```
  [DEBUG] 🔭 DOM(12 events) — Thinking...
  [DEBUG] ⚡ Log: active (42 new lines — non-terminal, model processing)
  [INFO] 🟢 State: GENERATING → DOM: Thinking...
  ```

---

## 🔍 Qué debería ver

### ✅ Comportamiento CORRECTO

1. **Pasos 1-3 completados** → Ícono en barra de estado = `$(eye)` IDLE

2. **Envío de solicitud** → Ícono cambia a `$(loading~spin)` GENERATING

3. **Mientras Copilot procesa**:
   - `$(loading~spin)` se mantiene visible
   - El texto en la barra cambia a reflejar la actividad:
     - "DOM: Thinking..." (primeros 2-5 seg)
     - "DOM: Analyzing..." (si hay búsqueda en archivos)
     - "DOM: Processing..." (cálculos internos)

4. **Cuando termina**:
   - Ícono cambia a `$(check)` COMPLETED
   - Texto muestra "COMPLETED" o "Idle"
   - Activity Log registra: `✅ COMPLETED`

### ❌ Comportamiento INCORRECTO (indica Bug)

**Síntoma A: Ícono nunca cambia de IDLE**

```
Razón: El DOM watcher NO está conectado
Solución:
  1. Verifica que VS Code fue iniciado con --remote-debugging-port=9222
  2. Ejecuta: lsof -i :9222 (debe mostrar VS Code listening)
  3. Abre Output channel → busca "ChatDOMWatcher"
     - Si dice "CDP not available" → reinicia VS Code con flag
```

**Síntoma B: Ícono girador NO se detiene después de terminar**

```
Razón: Bug 2 (ventana 90s) — _onDOMDelta no limpia el estado
Solución: Ya fue arreglado en v1.3.9
  1. Recarga la extensión: Cmd+Shift+P → "Developer: Reload Window"
  2. Reintenta el test
```

**Síntoma C: Activity Log está vacío**

```
Razón: Ciclos del monitor aún no iniciaron o no hay eventos detectados
Solución:
  1. Espera 5 segundos después de iniciar el monitor
  2. Verifica Output channel para mensajes de error
  3. Si ves [ERROR] — reporta aquí
```

---

## 📊 Eventos que DEBE detectar

| Evento                 | Origen            | Estado                 | Visibilidad                      |
| ---------------------- | ----------------- | ---------------------- | -------------------------------- |
| "Thinking..."          | Chat UI           | GENERATING             | Barra de estado INMEDIATAMENTE   |
| "Processing..."        | Chat UI           | GENERATING             | 1-2 seg después                  |
| "Analyzing backend/"   | Chat UI           | GENERATING             | Durante análisis                 |
| Stop button aparece    | DOM mutation      | GENERATING             | 0.5 seg (poll)                   |
| Stop button desaparece | DOM mutation      | COMPLETED              | < 1 seg (real-time)              |
| Response aparece       | DOM mutation      | puede ser NEW_RESPONSE | Ignorado (no implica generación) |
| Text streaming         | DOM CharacterData | TEXT_CHANGE            | Ignorado (nueva regla Bug 4)     |

---

## 🛠 Comandos útiles para debug

```bash
# Verificar que CDP está disponible (puerto 9222)
lsof -i :9222

# Ver versión de la extensión instalada
code --list-extensions | grep copilot-agent-monitor

# Ubicar el audit log de la sesión
find ~/.config/Code/User/workspaceStorage -name "*monitor*audit*" 2>/dev/null | head -1

# Ver últimas entradas del audit log (si existe)
tail -20 ~/.config/Code/User/workspaceStorage/*/GitHub.copilot-chat/copilot-monitor-audit.jsonl | jq '.'
```

---

## 📝 Registro de prueba

Copia este formato para registrar tu test:

```
Fecha: [YYYY-MM-DD]
Hora: [HH:MM:SS]
Solicitud: [Opción A/B/C]
Duración estimada: [seg]

Resultado de barra de estado:
- Cambió a GENERATING? Sí/No
- Mostró texto dinámico? Sí/No
- Cuál? [Thinking/Processing/Analyzing...]
- Cambió a COMPLETED? Sí/No
- Tiempo hasta COMPLETED: [seg]

Activity Log:
- ¿Mostró eventos DOM? Sí/No
- Cuántos eventos? [N]

Output Channel:
- ¿DEBUG messages? Sí/No
- Algún ERROR? Sí/No

Conclusión: PASS / FAIL
Notas: [...]
```

---

## 🎯 Conclusión esperada

Si **TODOS** estos eventos se cumplen en < 2 seg:

✅ Bug 1 (isCompleted race) — FIXED
✅ Bug 2 (ventana 90s) — FIXED
✅ Bug 3 (TEXT_CHANGE falsos positivos) — FIXED
✅ Bug 4 (NEW_RESPONSE en isGenerating) — FIXED
✅ Bug 5 (characterData TextNode) — FIXED
✅ StatusBar duplication — FIXED

Entonces la detección **es real-time** y el monitor está listo para producción.
