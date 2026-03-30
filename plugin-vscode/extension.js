// ================================================================
//  Copilot Model Cycler v3.0
//
//  Comportamiento ante cada evento:
//
//  1. RATE LIMIT detectado (⌘⇧L o automático)
//     → Cicla al siguiente modelo (NO abre nuevo chat)
//     → Envía "Continuar" en el chat actual
//
//  2. ERROR en chat (⌘⇧E o watchdog)
//     → Abre nuevo chat
//     → Lee .prompts/prompt_1.md del workspace
//     → Envía el contenido completo del archivo al nuevo chat
//
//  3. Límite de mensajes (automático vía trackAndSend)
//     → Igual que caso 2: nuevo chat + prompt del archivo
//
//  4. Ciclo de modelos manual (⌘⇧. / ⌘⇧,)
//     → Solo cambia el modelo, sin tocar el chat
// ================================================================

const vscode = require("vscode");
const fs = require("fs");
const path = require("path");

// ─── Estado global ────────────────────────────────────────────
const state = {
  currentModelIndex: 0,
  messageCount: 0,
  warningShown: false,
  statusBar: null,
  pollTimer: null,
  copilotLogPath: "", // ruta del log activo (se re-descubre si cambia)
  copilotLogOffset: 0, // offset en bytes — solo lee líneas nuevas
  handlingEvent: false, // mutex para evitar doble-trigger
  isAgentBusy: false, // true si el log fue actualizado en los últimos 2×pollInterval
};

// (RATE_LIMIT_PATTERNS y ERROR_PATTERNS eliminados — se usa exclusivamente LOG_PATTERNS abajo)

// ─── Activación ───────────────────────────────────────────────
function activate(context) {
  log("Copilot Model Cycler v3.0 activado");

  // Status bar
  state.statusBar = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100,
  );
  state.statusBar.command = "modelCycler.showStatus";
  state.statusBar.tooltip = [
    "Copilot Model Cycler v3.0",
    "───────────────────────────",
    "⌘⇧L  → Rate limit: ciclar modelo + Continuar",
    "⌘⇧E  → Error: nuevo chat + AGENT_LOOP_PROMPT",
    "⌘⇧R  → Nuevo chat manual + AGENT_LOOP_PROMPT",
    "⌘⇧.  → Siguiente modelo",
    "⌘⇧,  → Modelo anterior",
    "⌘⇧M  → Elegir modelo",
    "───────────────────────────",
    "Click → Estado completo",
  ].join("\n");
  context.subscriptions.push(state.statusBar);

  syncIndexFromCopilotSettings();
  refreshStatusBar();
  if (cfg().showStatusBar) state.statusBar.show();

  // Comandos
  reg(context, "modelCycler.cycleNext", () => cycle(+1));
  reg(context, "modelCycler.cyclePrev", () => cycle(-1));
  reg(context, "modelCycler.pickModel", () => pick());
  reg(context, "modelCycler.trackAndSend", () => trackAndSend());
  reg(context, "modelCycler.resetSession", () =>
    triggerNewChat({ reason: "manual" }),
  );
  reg(context, "modelCycler.handleRateLimit", () => handleRateLimit());
  reg(context, "modelCycler.handleChatError", () => handleChatError());
  reg(context, "modelCycler.resetCounter", () => resetCounter());
  reg(context, "modelCycler.showStatus", () => showStatus());

  // Monitoreo automático del output de Copilot
  startCopilotOutputMonitor(context);

  // Reaccionar a cambios de settings
  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration((e) => {
      if (
        e.affectsConfiguration("modelCycler") ||
        e.affectsConfiguration("github.copilot.chat")
      ) {
        syncIndexFromCopilotSettings();
        refreshStatusBar();
        if (cfg().showStatusBar) state.statusBar.show();
        else state.statusBar.hide();
      }
    }),
  );
}

// ─── Monitor automático — técnica idéntica al Python monitor ─────────────────
//
//  1. Busca "GitHub Copilot Chat.log" recursivamente en el directorios de logs
//  2. Elige el archivo con mtime más reciente (el de la sesión activa)
//  3. Trackea el offset en bytes para leer SOLO líneas nuevas
//  4. Usa patrones de ccreq: que son el formato real del log de Copilot Chat
//  5. Detecta rotación de log (tamaño < offset) y resetea el offset

// Nombre exacto del log de Copilot Chat (mismo que usa el Python monitor)
const COPILOT_LOG_NAME = "GitHub Copilot Chat.log";

// Líneas de warning benignas que aparecen en CADA request (≈36 líneas por request).
// Se excluyen antes de entrar al loop de LOG_PATTERNS para no interrumpir el scan.
// Confirmado con análisis del log real: estas líneas aparecen ANTES de los eventos críticos.
const BENIGN_WARNING_RE =
  /\[warning\] Tool .* failed validation: schema must be an object if present/;

// Patrones exactos del log de Copilot (formato ccreq:)
// Basados en el Python monitor v5 — mismo orden de prioridad
const LOG_PATTERNS = [
  {
    re: /rate.limit|429|Too Many Requests|quota.*exhaust|exhausted.*quota|rate_limited|RateLimitError/i,
    type: "rate_limited",
  },
  {
    re: /\[error\].*(?:500|503|502|overload|capacity|Internal Server)|overloaded_error|overload_error|ccreq:.*\|\s*error\s*\||hard.error/i,
    type: "hard_error",
  },
  {
    re: /failed validation.*schema must be|ToolValidationError/i,
    type: "tool_validation_error",
  },
  { re: /ccreq:.*\|\s*cancelled\s*\|/i, type: "cancelled" },
  {
    re: /Stop hook result.*shouldContinue=false|ToolCallingLoop.*[Ss]top|agent.*loop.*stop/i,
    type: "loop_stopped",
  },
  { re: /ccreq:.*\|\s*success\s*\|/i, type: "success" },
];

function startCopilotOutputMonitor(context) {
  const interval = cfg().get("monitoring.pollInterval") ?? 3000;
  if (interval <= 0) {
    log("Monitor automático desactivado (pollInterval=0)");
    return;
  }

  log(
    `Monitor automático iniciado (cada ${interval}ms) — leyendo ${COPILOT_LOG_NAME}`,
  );

  state.pollTimer = setInterval(async () => {
    if (state.handlingEvent) return;
    await checkCopilotLogs();
  }, interval);

  context.subscriptions.push({
    dispose: () => {
      if (state.pollTimer) clearInterval(state.pollTimer);
    },
  });
}

// Encuentra el "GitHub Copilot Chat.log" con mtime más reciente
// buscando recursivamente bajo el directorio base de logs de VS Code.
// Idéntico a find_copilot_log() del Python monitor.
function findActiveCopilotLog(logsDir) {
  let bestPath = null;
  let bestMtime = 0;

  function recurse(dir) {
    let entries;
    try {
      entries = fs.readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }

    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        recurse(full);
      } else if (entry.isFile() && entry.name === COPILOT_LOG_NAME) {
        try {
          const mt = fs.statSync(full).mtimeMs;
          if (mt > bestMtime) {
            bestMtime = mt;
            bestPath = full;
          }
        } catch {
          /* ignorar */
        }
      }
    }
  }

  recurse(logsDir);
  return bestPath;
}

function getVSCodeLogsDir() {
  const home = process.env.HOME || process.env.USERPROFILE || "";
  const candidates = [
    path.join(home, "Library", "Application Support", "Code", "logs"), // macOS
    path.join(home, ".config", "Code", "logs"), // Linux
    path.join(process.env.APPDATA || "", "Code", "logs"), // Windows
  ];
  return (
    candidates.find((p) => {
      try {
        return fs.existsSync(p);
      } catch {
        return false;
      }
    }) || null
  );
}

async function checkCopilotLogs() {
  try {
    const logsDir = getVSCodeLogsDir();
    if (!logsDir) return;

    // Cache: solo re-escanear el directorio de logs si el archivo actual ya no existe.
    // findActiveCopilotLog() es O(n) sobre todos los logs — escanear cada 3s genera I/O innecesario.
    // En la práctica el log activo no cambia durante una sesión de VS Code.
    const needsDiscover =
      !state.copilotLogPath || !fs.existsSync(state.copilotLogPath);

    if (needsDiscover) {
      const logPath = findActiveCopilotLog(logsDir);
      if (!logPath) return;
      if (logPath !== state.copilotLogPath) {
        log(`Nuevo log activo: ${logPath}`);
        state.copilotLogPath = logPath;
        state.copilotLogOffset = 0;
      }
    }

    const activePath = state.copilotLogPath;
    const stat = fs.statSync(activePath);
    const size = stat.size;

    // Detectar actividad: si el log fue modificado dentro de 2×pollInterval → agente ocupado
    const pollInterval = cfg().get("monitoring.pollInterval") ?? 3000;
    const wasBusy = state.isAgentBusy;
    state.isAgentBusy = Date.now() - stat.mtimeMs < pollInterval * 2;
    if (wasBusy !== state.isAgentBusy) refreshStatusBar();

    // Detección de rotación: el archivo se truncó
    if (size < state.copilotLogOffset) {
      log("Log rotado/truncado — reseteando offset");
      state.copilotLogOffset = 0;
    }

    if (size <= state.copilotLogOffset) return; // sin líneas nuevas

    // Leer solo los bytes nuevos (equivalente a read_new_lines del Python)
    const fd = fs.openSync(activePath, "r");
    const buffer = Buffer.allocUnsafe(size - state.copilotLogOffset);
    fs.readSync(fd, buffer, 0, buffer.length, state.copilotLogOffset);
    fs.closeSync(fd);
    state.copilotLogOffset = size;

    const newContent = buffer.toString("utf8");
    await analyzeLogContent(newContent);
  } catch {
    // Silencioso — best-effort
  }
}

async function analyzeLogContent(content) {
  if (state.handlingEvent) return;

  // Evaluar línea por línea usando LOG_PATTERNS (formato ccreq: real del log).
  //
  // BUG CRÍTICO CONFIRMADO (2026-03-28):
  //   El log real tiene ≈36 líneas "[warning] Tool XXX failed validation" por request.
  //   Estas líneas aparecen ANTES del [error] de rate limit en el mismo batch.
  //   Con 'return' al detectar tool_validation_error → el plugin era CIEGO a rate limits.
  //   Fix: early-exit para benign warnings + 'break' (inner) en lugar de 'return' (outer).
  for (const line of content.split("\n")) {
    // Skip rápido de líneas de warning benignas (aparecen 36× por request)
    if (BENIGN_WARNING_RE.test(line)) continue;

    for (const { re, type } of LOG_PATTERNS) {
      if (re.test(line)) {
        if (type === "rate_limited") {
          log(`⚡ RATE LIMIT detectado: ${line.trim().slice(0, 120)}`);
          await handleRateLimit();
          return;
        }
        if (type === "hard_error") {
          log(`🚨 ERROR DURO detectado: ${line.trim().slice(0, 120)}`);
          await handleChatError();
          return;
        }
        if (type === "loop_stopped") {
          // loop_stopped = agente terminó una tarea normalmente (shouldContinue=false)
          // NO es un error — no tomar ninguna acción
          log(
            `ℹ️ Loop detenido normalmente (ignorado): ${line.trim().slice(0, 80)}`,
          );
        }
        // success, cancelled, tool_validation_error, loop_stopped:
        // break el inner loop (no 'return') → seguir con la siguiente línea
        // Esto garantiza que un rate_limit POSTERIOR al success en el mismo batch sí se detecte.
        break;
      }
    }
  }
}

// ─── HANDLER: Rate Limit ──────────────────────────────────────
//   NO abre nuevo chat. Solo cicla modelo + envía "Continuar"

async function handleRateLimit() {
  if (state.handlingEvent) return;
  state.handlingEvent = true;

  try {
    log("Manejando rate limit...");
    const models = getModels();
    const resetMode = cfg().get("session.rateLimitCycleTo") || "next";

    // Determinar el modelo destino (evitar el actual que tiene rate limit)
    let targetIndex;
    if (resetMode === "top") {
      // Ir al más poderoso disponible que no sea el actual
      targetIndex = 0;
      if (targetIndex === state.currentModelIndex && models.length > 1) {
        targetIndex = 1;
      }
    } else {
      // next: siguiente en la lista
      targetIndex = (state.currentModelIndex + 1) % models.length;
    }

    state.currentModelIndex = targetIndex;
    const newModel = models[targetIndex];

    // 1. Cambiar modelo
    await applyModel(newModel, true);
    const label = labelFor(newModel);

    // 2. Notificación visible
    vscode.window.showWarningMessage(
      `⚡ Rate Limit → Cambiando a ${label}. Enviando "Continuar"...`,
      { modal: false },
    );

    // 3. Enviar "Continuar" en el chat ACTUAL (sin abrir nuevo chat)
    await sleep(cfg().get("monitoring.continueDelay") ?? 600);
    await sendMessageToCurrentChat("Continuar");

    log(`Rate limit manejado. Modelo nuevo: ${newModel}`);
  } finally {
    await sleep(cfg().get("monitoring.rateLimitCooldown") ?? 2000);
    state.handlingEvent = false;
  }
}

// ─── HANDLER: Error en chat ───────────────────────────────────
//   Abre nuevo chat + envía el AGENT_LOOP_PROMPT completo

async function handleChatError() {
  if (state.handlingEvent) return;
  state.handlingEvent = true;

  try {
    log(
      "Manejando error en chat → abriendo nuevo chat con AGENT_LOOP_PROMPT...",
    );
    await triggerNewChat({ reason: "error" });
  } finally {
    await sleep(cfg().get("monitoring.errorCooldown") ?? 3000);
    state.handlingEvent = false;
  }
}

// ─── Abrir nuevo chat + enviar prompt del archivo ─────────────

async function triggerNewChat({ reason = "manual" }) {
  const models = getModels();
  const resetMode = cfg().get("session.resetModel") || "top";
  const delay = cfg().get("session.continuationDelay") ?? 1000;

  log(`Trigger nuevo chat. Razón: ${reason}`);

  // 1. Cambiar modelo según configuración
  if (models.length > 0) {
    let targetIndex = state.currentModelIndex;
    if (resetMode === "top") targetIndex = 0;
    else if (resetMode === "cycle")
      targetIndex = (state.currentModelIndex + 1) % models.length;
    state.currentModelIndex = targetIndex;
    await applyModel(models[targetIndex], true);
  }

  // 2. Resetear contadores
  const prevCount = state.messageCount;
  state.messageCount = 0;
  state.warningShown = false;
  refreshStatusBar();

  // 3. Abrir nuevo chat
  let opened = false;
  try {
    await vscode.commands.executeCommand("workbench.action.chat.newChat");
    opened = true;
    log("Nuevo chat abierto vía workbench.action.chat.newChat");
  } catch {
    try {
      await vscode.commands.executeCommand("workbench.action.chat.open");
      opened = true;
      log("Nuevo chat abierto vía workbench.action.chat.open");
    } catch (err2) {
      log(`Error abriendo nuevo chat: ${err2.message}`);
    }
  }

  // 4. Notificación
  const label = labelFor(models[state.currentModelIndex] || "");
  const prefix =
    reason === "error"
      ? "🚨 Error recuperado"
      : reason === "limit"
        ? `🔄 Límite de ${prevCount} mensajes`
        : "🔄 Reset manual";

  vscode.window.showInformationMessage(
    `${prefix} → Nuevo chat. Modelo: ${label}`,
  );

  // 5. Leer y enviar el AGENT_LOOP_PROMPT desde el archivo
  if (opened) {
    await sleep(delay);
    const promptContent = await readPromptFile();
    if (promptContent) {
      await sendMessageToCurrentChat(promptContent);
    } else {
      // Si no se encontró el archivo, notificar al usuario
      vscode.window
        .showWarningMessage(
          `Model Cycler: No se encontró el archivo de prompt. ` +
            `Configura "modelCycler.session.promptFilePath" o crea ` +
            `".prompts/prompt_1.md" en tu workspace.`,
          "Abrir Settings",
        )
        .then((sel) => {
          if (sel === "Abrir Settings") {
            vscode.commands.executeCommand(
              "workbench.action.openSettings",
              "modelCycler.session.promptFilePath",
            );
          }
        });
    }
  }
}

// ─── Leer el archivo de prompt ────────────────────────────────

async function readPromptFile() {
  const relativePath =
    cfg().get("session.promptFilePath") || ".prompts/prompt_1.md";

  // Buscar en todos los workspace folders
  const folders = vscode.workspace.workspaceFolders || [];
  for (const folder of folders) {
    const fullPath = path.join(folder.uri.fsPath, relativePath);
    try {
      const content = fs.readFileSync(fullPath, "utf8");
      log(`Prompt leído de: ${fullPath} (${content.length} chars)`);
      return content;
    } catch {
      // No está en este folder, seguir buscando
    }
  }

  // Buscar también como ruta absoluta por si el usuario la configuró así
  try {
    if (path.isAbsolute(relativePath)) {
      const content = fs.readFileSync(relativePath, "utf8");
      log(`Prompt leído de ruta absoluta: ${relativePath}`);
      return content;
    }
  } catch {}

  log(`Archivo de prompt no encontrado: ${relativePath}`);
  return null;
}

// ─── Enviar mensaje al chat actual ────────────────────────────
//
//  Estrategia (por orden de prioridad):
//  1. workbench.action.chat.open con { query, isPartialQuery:false } + submit de seguridad
//  2. Clipboard → focus chat → submit (por si Method 1 pobló pero no envió)
//  3. Mostrar botón "Copiar prompt" al usuario

async function sendMessageToCurrentChat(message) {
  log(`Enviando mensaje al chat (${message.length} chars)...`);

  // Método 1: API directa de VS Code con query
  // isPartialQuery:false auto-envía en VS Code ≥1.91.
  // Si la versión lo ignora, el texto queda en el input → Method 2 lo envía.
  try {
    await vscode.commands.executeCommand("workbench.action.chat.open", {
      query: message,
      isPartialQuery: false,
    });
    // Delay de 1000ms (era 300ms):
    // - Si isPartialQuery:false funcionó → el submit llega después de que el input ya está vacío → no-op
    // - Si isPartialQuery:false fue ignorado → el texto sigue en el input → submit lo envía correctamente
    // - 300ms era demasiado corto: podía interrumpir la respuesta de Copilot si ya había empezado
    await sleep(1000);
    await vscode.commands.executeCommand("workbench.action.chat.submit");
    log("Mensaje enviado vía workbench.action.chat.open con query");
    return;
  } catch {
    log("Método 1 falló, intentando método 2 (clipboard)...");
  }

  // Método 2: clipboard → focus chat → submit
  // Nota: editor.action.clipboardPasteAction NO funciona en el chat input (es un
  // componente React, no un TextEditor). Se usa workbench.action.chat.open sin
  // query para garantizar el foco, luego submit envía lo que haya en el input.
  try {
    const prevClipboard = await vscode.env.clipboard.readText();
    await vscode.env.clipboard.writeText(message);

    await vscode.commands.executeCommand("workbench.action.chat.open");
    await sleep(500);

    // En VS Code moderno el focus en el chat input permite cmd+v nativo,
    // pero como no podemos invocar paste en el chat desde aquí, lanzamos
    // chat.open con query como segundo intento de inserción.
    await vscode.commands.executeCommand("workbench.action.chat.open", {
      query: message,
      isPartialQuery: false,
    });
    await sleep(300);
    await vscode.commands.executeCommand("workbench.action.chat.submit");

    await vscode.env.clipboard.writeText(prevClipboard);
    log("Mensaje enviado vía clipboard + chat.open fallback");
    return;
  } catch (err2) {
    log(`Método 2 falló: ${err2.message}`);
  }

  // Método 3: fallback manual — mostrar el mensaje para que el usuario lo copie
  const isBig = message.length > 200;
  const preview = isBig ? message.substring(0, 100) + "..." : message;

  log(`Ambos métodos fallaron. Mostrando opción manual.`);

  const action = await vscode.window.showWarningMessage(
    `Model Cycler: No se pudo enviar el prompt automáticamente. ` +
      `Copia el prompt y pégalo en el chat.`,
    "Copiar prompt al clipboard",
    "Cancelar",
  );

  if (action === "Copiar prompt al clipboard") {
    await vscode.env.clipboard.writeText(message);
    vscode.window.showInformationMessage(
      `📋 Prompt copiado (${message.length} chars). Pégalo en el chat con ⌘V.`,
    );
  }
}

// ─── Intercepción de Enter → contador de mensajes ─────────────

async function trackAndSend() {
  // Primero enviar el mensaje real
  await vscode.commands.executeCommand("workbench.action.chat.submit");

  // Contar
  state.messageCount++;
  refreshStatusBar();
  log(`Mensaje #${state.messageCount}`);

  // Verificar umbrales
  const maxMsg = cfg().get("session.maxMessages") ?? 25;
  const warnAt = cfg().get("session.warningAt") ?? 20;
  const autoRst = cfg().get("session.autoReset") ?? true;

  if (maxMsg > 0 && state.messageCount >= maxMsg) {
    if (autoRst) {
      log(`Auto-reset por límite de ${maxMsg} mensajes`);
      await triggerNewChat({ reason: "limit" });
    } else if (!state.warningShown) {
      state.warningShown = true;
      const sel = await vscode.window.showWarningMessage(
        `⚠️ ${state.messageCount} mensajes en esta sesión. ¿Abrir nuevo chat?`,
        "Sí, abrir nuevo chat",
        "Ignorar",
      );
      if (sel === "Sí, abrir nuevo chat") {
        await triggerNewChat({ reason: "limit" });
      }
    }
  } else if (!state.warningShown && state.messageCount >= warnAt) {
    state.warningShown = true;
    const rem = maxMsg - state.messageCount;
    vscode.window.showInformationMessage(
      `ℹ️ Copilot Cycler: ${state.messageCount} mensajes. ` +
        `Nuevo chat automático en ${rem} mensajes.`,
      { modal: false },
    );
  }
}

// ─── Aplicar modelo ───────────────────────────────────────────

async function applyModel(modelId, silent = false) {
  // Intentar escribir la preferencia de modelo en la configuración global.
  // En algunas versiones de VS Code esta setting es de solo lectura — si falla,
  // solo actualizamos el tracking interno (status bar) sin mostrar error.
  try {
    await vscode.workspace
      .getConfiguration("github.copilot.chat")
      .update("languageModel", modelId, vscode.ConfigurationTarget.Global);
  } catch {
    // Setting no escribible — continuar de todos modos (solo tracking local)
    log(
      `ℹ️ applyModel: no se pudo escribir languageModel (solo tracking local)`,
    );
  }

  refreshStatusBar();

  if (!silent && cfg().notifyOnSwitch) {
    const label = labelFor(modelId);
    const models = getModels();
    const pos = models.indexOf(modelId);
    vscode.window.showInformationMessage(
      `🤖 Copilot → ${label}  [${pos + 1}/${models.length}]`,
    );
  }
}

// ─── Ciclar modelos ───────────────────────────────────────────

async function cycle(direction) {
  const models = getModels();
  if (!models.length) {
    warnEmptyList();
    return;
  }
  state.currentModelIndex =
    (state.currentModelIndex + direction + models.length) % models.length;
  await applyModel(models[state.currentModelIndex]);
}

async function pick() {
  const models = getModels();
  if (!models.length) {
    warnEmptyList();
    return;
  }
  const current = getCurrentCopilotModel();
  const items = models.map((m, i) => ({
    label:
      (m === current ? "$(circle-filled) " : "$(circle-outline) ") +
      labelFor(m),
    description: m,
    detail: m === current ? "← activo ahora" : undefined,
    index: i,
  }));
  const sel = await vscode.window.showQuickPick(items, {
    title: "Copilot Model Cycler — elige modelo",
    placeHolder: "Filtra por nombre o ID...",
    matchOnDescription: true,
  });
  if (sel) {
    state.currentModelIndex = sel.index;
    await applyModel(models[sel.index]);
  }
}

// ─── Estado y status bar ──────────────────────────────────────

function refreshStatusBar() {
  const models = getModels();
  const modelId = getCurrentCopilotModel();
  const label = labelFor(modelId);
  const pos = models.indexOf(modelId);
  const maxMsg = cfg().get("session.maxMessages") ?? 25;
  const msgs = state.messageCount;

  const pct = maxMsg > 0 ? msgs / maxMsg : 0;
  if (pct >= 1)
    state.statusBar.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground",
    );
  else if (pct >= 0.75)
    state.statusBar.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.warningBackground",
    );
  else state.statusBar.backgroundColor = undefined;

  const posStr = pos >= 0 ? `${pos + 1}/${models.length}` : "?";
  const busyStr = state.isAgentBusy ? "  $(sync~spin)" : "";
  state.statusBar.text = `$(robot) ${label}  💬${msgs}/${maxMsg}  [${posStr}]${busyStr}`;
}

function showStatus() {
  const models = getModels();
  const modelId = getCurrentCopilotModel();
  const maxMsg = cfg().get("session.maxMessages") ?? 25;
  const warnAt = cfg().get("session.warningAt") ?? 20;
  const autoRst = cfg().get("session.autoReset") ?? true;
  const filePath =
    cfg().get("session.promptFilePath") || ".prompts/prompt_1.md";
  const resetM = cfg().get("session.rateLimitCycleTo") || "next";

  const fileExists = (vscode.workspace.workspaceFolders || []).some((f) =>
    fs.existsSync(path.join(f.uri.fsPath, filePath)),
  );

  vscode.window.showInformationMessage(
    [
      `🤖 Modelo: ${labelFor(modelId)}`,
      `📊 Posición: ${models.indexOf(modelId) + 1}/${models.length}`,
      `💬 Mensajes: ${state.messageCount}/${maxMsg}`,
      `⚠️  Advertencia en: ${warnAt}`,
      `🔄 Auto-reset: ${autoRst ? "ON" : "OFF"}`,
      `📄 Prompt file: ${filePath} ${fileExists ? "✅" : "❌ NO ENCONTRADO"}`,
      `⚡ Rate limit cicla a: ${resetM}`,
      "",
      `Lista: ${models.map((m) => labelFor(m)).join(" → ")}`,
    ].join("\n"),
    { modal: true },
    "OK",
  );
}

function resetCounter() {
  state.messageCount = 0;
  state.warningShown = false;
  refreshStatusBar();
  vscode.window.showInformationMessage("↺ Contador reiniciado.");
}

// ─── Helpers ──────────────────────────────────────────────────

function cfg() {
  return vscode.workspace.getConfiguration("modelCycler");
}

function getModels() {
  return (cfg().get("models") || []).filter(
    (m) => typeof m === "string" && m.trim(),
  );
}

function labelFor(modelId) {
  const labels = cfg().get("modelLabels") || {};
  return labels[modelId] || modelId;
}

function getCurrentCopilotModel() {
  return (
    vscode.workspace
      .getConfiguration("github.copilot.chat")
      .get("languageModel") || ""
  );
}

function syncIndexFromCopilotSettings() {
  const models = getModels();
  const active = getCurrentCopilotModel();
  const idx = models.indexOf(active);
  if (idx >= 0) state.currentModelIndex = idx;
}

function warnEmptyList() {
  vscode.window.showWarningMessage(
    'Model Cycler: lista de modelos vacía. Edita "modelCycler.models".',
  );
}

function reg(ctx, id, fn) {
  ctx.subscriptions.push(vscode.commands.registerCommand(id, fn));
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

const out = vscode.window.createOutputChannel("Copilot Model Cycler");
function log(msg) {
  out.appendLine(`[${new Date().toLocaleTimeString()}] ${msg}`);
}

// ─── Desactivación ────────────────────────────────────────────

function deactivate() {
  if (state.pollTimer) clearInterval(state.pollTimer);
  if (state.statusBar) state.statusBar.dispose();
  out.dispose();
}

module.exports = { activate, deactivate };
