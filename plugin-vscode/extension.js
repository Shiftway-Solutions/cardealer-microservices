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

// ─── Módulos del agente integrado ────────────────────────────────────────────
const { Monitor, setLogger: setMonitorLogger } = require("./agent/monitor");
const {
  Brain,
  ACTION_COOLDOWNS,
  setLogger: setBrainLogger,
} = require("./agent/brain");
const agentActions = require("./agent/actions");

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

  // ── Estado del AgentLoop integrado ──────────────────────────────────────
  agentEnabled: false, // true cuando el AgentLoop está corriendo
  agentLoopTimer: null, // setInterval del AgentLoop
  // Timestamps de última acción (para cooldowns)
  last_send_continue_ts: 0,
  last_open_new_chat_ts: 0,
  last_stop_and_new_chat_ts: 0,
  last_cycle_model_ts: 0,
  last_focus_vscode_ts: 0,
  last_execute_prompt6_ts: 0,
  // Tipo de última acción (para post-action cooldown)
  lastActionType: "",
  lastActionTs: 0,
  // Timestamps específicos
  lastActivityTs: Date.now(),
  lastNewChatTs: 0,
  lastContinueTs: 0,
  // Contadores
  errorContinueCount: 0,
  rateLimitCount: 0,
  newChatCount: 0,
  continueCount: 0,
};

// ─── Instancias del agente integrado ─────────────────────────────────────────
const agentMonitor = new Monitor();
const agentBrain = new Brain();

// (RATE_LIMIT_PATTERNS y ERROR_PATTERNS eliminados — se usa exclusivamente LOG_PATTERNS abajo)

// ─── Activación ───────────────────────────────────────────────
function activate(context) {
  log("Copilot Model Cycler v3.0 activado");

  // Status bar
  state.statusBar = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100,
  );
  state.statusBar.command = "modelCycler.showAgentMenu";
  state.statusBar.tooltip = "Copilot Model Cycler";
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
  reg(context, "modelCycler.showAgentMenu", () => showAgentMenu());
  reg(context, "modelCycler.showStatus", () => showStatus());
  reg(context, "modelCycler.openAgentConsole", () => openAgentConsole());
  reg(context, "modelCycler.openAgentPrompt", () => openAgentPrompt());
  reg(context, "modelCycler.toggleAgent", () => toggleAgentLoop());
  reg(context, "modelCycler.executePrompt6", () => forceExecutePrompt6());

  // Monitoreo automático — AgentLoop integrado (reemplaza Python monitor)
  startAgentLoop(context);

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

// ─── AgentLoop integrado ─────────────────────────────────────────────────────
//
//  Ciclo completo: Observe → Decide → Act
//  Usa los módulos del agente (Monitor + Brain + Actions) que corren
//  directamente en el proceso de VS Code, sin Python externo ni AppleScript.
//
//  El loop también maneja prompt_1.md (regla CPSO) vía FileSystemWatcher.

// Nombre exacto del log de Copilot Chat (conservado para compatibilidad)
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

function startAgentLoop(context) {
  const interval = cfg().get("monitoring.pollInterval") ?? 3000;
  const agentEnabled = cfg().get("agent.enabled") ?? true;

  if (interval <= 0) {
    log("AgentLoop desactivado (pollInterval=0)");
    return;
  }

  // Inyectar vscode y actions en el Brain para que pueda usar:
  //   1. vscode.lm → sesión Copilot ya logueada → decisiones con el modelo
  //   2. agentActions.takeChatScreenshots → fotos del chat antes de decidir
  agentBrain.setVSCode(vscode);
  agentBrain.setActions(agentActions);

  // Inyectar logger en los módulos del agente
  const agentLog = (msg) => log(`[agent] ${msg}`);
  setMonitorLogger(agentLog);
  setBrainLogger(agentLog);
  agentActions.setLogger(agentLog);

  // Configurar el Monitor con el path del chat snapshot
  const folders = vscode.workspace.workspaceFolders || [];
  const snapshotPath = folders.length
    ? path.join(folders[0].uri.fsPath, ".prompts", "agent", "chat_snapshot.txt")
    : "";
  agentMonitor.configure({
    chatSnapshotPath: snapshotPath,
    workspaceFolders: folders,
  });

  if (agentEnabled) {
    log(
      `AgentLoop iniciado (cada ${interval}ms) — Monitor+Brain+Actions integrados`,
    );
    state.agentEnabled = true;

    // FileSystemWatcher para prompt_1.md (regla CPSO — reacción inmediata)
    if (folders.length) {
      const prompt6Pattern = new vscode.RelativePattern(
        folders[0],
        ".prompts/prompt_1.md",
      );
      const watcher = vscode.workspace.createFileSystemWatcher(prompt6Pattern);
      const onPrompt6Change = () => {
        log("[agent] prompt_1.md cambió — procesando en siguiente ciclo");
      };
      watcher.onDidChange(onPrompt6Change);
      watcher.onDidCreate(onPrompt6Change);
      context.subscriptions.push(watcher);
    }

    state.agentLoopTimer = setInterval(async () => {
      if (state.handlingEvent) return;
      await runAgentCycle();
    }, interval);

    context.subscriptions.push({
      dispose: () => {
        if (state.agentLoopTimer) clearInterval(state.agentLoopTimer);
        state.agentEnabled = false;
      },
    });
  } else {
    // Modo legacy: solo el monitor de logs (sin Brain ni Actions del agente)
    log(`AgentLoop en modo legacy (agent.enabled=false) — solo Log Monitor`);
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
}

// ─── Ciclo principal del agente ──────────────────────────────────────────────
async function runAgentCycle() {
  try {
    // 1. Observar
    const obs = agentMonitor.observe(state);

    // Actualizar isAgentBusy en el status bar
    const wasBusy = state.isAgentBusy;
    if (wasBusy !== state.isAgentBusy) refreshStatusBar();

    // Log resumen solo cuando hay algo relevante
    if (obs.summary !== "NORMAL") {
      log(`[agent] Obs: ${obs.summary}`);
    }

    // 2. Decidir
    const decision = await agentBrain.decide(obs, state);

    // 3. Actuar (si la decisión no es wait)
    if (decision.action !== "wait") {
      await executeAgentDecision(decision, obs);
    }
  } catch (e) {
    log(`[agent] Error en ciclo: ${e.message}`);
  }
}

// ─── Ejecutar decisión del agente ────────────────────────────────────────────
async function executeAgentDecision(decision, obs) {
  const action = decision.action;
  log(
    `[agent] Ejecutando: ${action} (${decision.source}, conf=${decision.confidence.toFixed(2)}) — ${decision.reasoning}`,
  );

  // Cooldown pre-acción
  if (decision.waitBeforeActionSecs > 0) {
    await agentActions.sleep(decision.waitBeforeActionSecs * 1000);
  }

  // ── Pre-action screenshot (para acciones no triviales) ──────────────────────
  //    Toma fotos del chat y las registra en el log de auditoría.
  //    La validación visual principal ya ocurrió en Brain.decide() con estas
  //    mismas shots; las de aquí son el registro pre-ejecución.
  if (action !== "wait" && action !== "focus_vscode") {
    try {
      const preShots = await agentActions.takeChatScreenshots(
        `pre_${action}`,
        2,
      );
      if (preShots.length) {
        log(
          `[agent] Pre-action screenshots: ${preShots.length} captura(s) para '${action}'`,
        );
      }
    } catch (e) {
      log(
        `[agent] Pre-action screenshots fallaron (${e.message}) — ejecutando de todos modos`,
      );
    }
  }

  await runAgentAction(action, obs);
}

async function runAgentAction(action, obs, options = {}) {
  const { manual = false } = options;
  const now = Date.now();

  state[`last_${action}_ts`] = now;
  state.lastActionTs = now;
  state.lastActionType = action;
  if (action !== "focus_vscode") {
    state.lastActivityTs = now;
  }

  try {
    switch (action) {
      case "send_continue": {
        const ok = await agentActions.sendContinue();
        if (ok) {
          state.continueCount++;
          state.errorContinueCount++;
          state.lastContinueTs = now;
        }
        return ok;
      }

      case "open_new_chat": {
        const resetResult = await triggerNewChat({
          reason: manual ? "manual_agent_menu" : "agent",
        });
        if (resetResult.opened) {
          state.newChatCount++;
          state.errorContinueCount = 0;
          state.lastNewChatTs = now;
        }
        return Boolean(resetResult.opened && resetResult.promptDelivered);
      }

      case "stop_and_new_chat": {
        const stopOk = await agentActions.stopGeneration();
        await agentActions.sleep(600);
        const resetResult = await triggerNewChat({
          reason: manual ? "manual_stop_and_new_chat" : "context_full",
        });
        if (resetResult.opened) {
          state.newChatCount++;
          state.errorContinueCount = 0;
          state.lastNewChatTs = now;
        }
        return Boolean(
          stopOk && resetResult.opened && resetResult.promptDelivered,
        );
      }

      case "cycle_model": {
        const currentIdx = Number.isInteger(state.currentModelIndex)
          ? state.currentModelIndex
          : 0;
        const { modelId, newIdx } =
          await agentActions.cycleModelNextViaDB(currentIdx);
        if (Number.isInteger(newIdx) && newIdx >= 0) {
          state.currentModelIndex = newIdx;
        }
        if (modelId) {
          log(`[agent] Modelo ciclado → ${modelId}`);
        }
        state.rateLimitCount++;
        await agentActions.sleep(1500);
        const continueOk = await agentActions.sendContinue();
        if (continueOk) {
          state.continueCount++;
          state.lastContinueTs = Date.now();
        }
        return Boolean(modelId) && continueOk;
      }

      case "focus_vscode":
        return agentActions.focusVSCode();

      case "execute_prompt6": {
        const promptObs =
          obs && obs.prompt6Content && obs.prompt6Path
            ? obs
            : loadPrompt6Observation();
        if (!promptObs.prompt6Content || !promptObs.prompt6Path) {
          vscode.window.showWarningMessage(
            "prompt_1.md: archivo vacío o no encontrado",
          );
          return false;
        }
        await executePrompt6(promptObs);
        return true;
      }

      default:
        log(`[agent] Acción desconocida: ${action}`);
        return false;
    }
  } finally {
    syncIndexFromCopilotSettings();
    refreshStatusBar();
  }
}

// ─── Ejecutar prompt_1.md (regla CPSO) ───────────────────────────────────────
async function executePrompt6(obs) {
  if (!obs.prompt6Content || !obs.prompt6Path) {
    log("[agent] prompt_6: sin contenido o path");
    return;
  }

  const content = obs.prompt6Content;
  log(`[agent] Ejecutando prompt_1.md (${content.length} chars)`);

  // Enviar el contenido al chat
  const sent = await agentActions.sendMessage(content, {
    source: "extension.executePrompt6",
    reason: "prompt_1_changed",
    messageType: "prompt",
  });

  // Marcar como leído escribiendo READ al final
  try {
    fs.appendFileSync(obs.prompt6Path, "\nREAD", "utf8");
    log(`[agent] prompt_1.md marcado como READ`);
  } catch (e) {
    log(`[agent] Error escribiendo READ en prompt_6: ${e.message}`);
  }

  if (sent) {
    vscode.window.showInformationMessage(
      `[CPSO] prompt_1.md ejecutado (${content.length} chars)`,
    );
  }
}

// startCopilotOutputMonitor conservado para el modo legacy (agent.enabled=false)
function startCopilotOutputMonitor(context) {
  const interval = cfg().get("monitoring.pollInterval") ?? 3000;
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

    // Sync AgentLoop timestamps (para que el Brain no re-dispare)
    const now = Date.now();
    state.last_cycle_model_ts = now;
    state.lastActionTs = now;
    state.lastActionType = "cycle_model";
    state.lastActivityTs = now;
    state.rateLimitCount = (state.rateLimitCount || 0) + 1;

    // 2. Notificación visible
    vscode.window.showWarningMessage(
      `⚡ Rate Limit → Cambiando a ${label}. Enviando "Continuar"...`,
      { modal: false },
    );

    // 3. Enviar "Continuar" en el chat ACTUAL (sin abrir nuevo chat)
    await sleep(cfg().get("monitoring.continueDelay") ?? 600);
    await sendMessageToCurrentChat("Continuar", {
      source: "extension.handleRateLimit",
      reason: "rate_limit",
      messageType: "continue",
    });

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

    // Sync AgentLoop timestamps
    const now = Date.now();
    state.last_open_new_chat_ts = now;
    state.lastActionTs = now;
    state.lastActionType = "open_new_chat";
    state.lastActivityTs = now;
    state.errorContinueCount = 0;
    state.newChatCount = (state.newChatCount || 0) + 1;

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
  const promptWillGoToAgentConsole = shouldRoutePromptToAgentConsole({
    messageType: "prompt",
  });

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
  let promptDelivered = false;
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
  const promptTarget = promptWillGoToAgentConsole
    ? `Prompt escrito en ${getAgentPromptConsolePath()}`
    : "Prompt enviado al chat";

  vscode.window.showInformationMessage(
    opened
      ? `${prefix} → Nuevo chat. ${promptTarget}. Modelo: ${label}`
      : promptWillGoToAgentConsole
        ? `${prefix} → No se pudo abrir nuevo chat, pero el prompt se escribió en ${getAgentPromptConsolePath()}. Modelo: ${label}`
        : `${prefix} → No se pudo abrir nuevo chat. Modelo: ${label}`,
  );

  // 5. Leer y enviar el AGENT_LOOP_PROMPT desde el archivo
  if (opened || promptWillGoToAgentConsole) {
    await sleep(delay);
    const promptContent = await readPromptFile();
    if (promptContent) {
      promptDelivered = Boolean(
        await sendMessageToCurrentChat(promptContent, {
          source: "extension.triggerNewChat",
          reason,
          messageType: "prompt",
        }),
      );
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

  return {
    opened,
    promptDelivered,
    promptWillGoToAgentConsole,
  };
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
//  1. workbench.action.chat.open con { query, isPartialQuery:true } + submit
//     controlado por el plugin. Esto evita el doble envío que ocurría cuando
//     VS Code auto-enviaba y luego el plugin disparaba un submit extra.
//  2. Clipboard → focus chat → submit
//  3. Mostrar botón "Copiar prompt" al usuario
//
//  En modo agentConsole, solo los mensajes de tipo "prompt" se redirigen al
//  archivo .prompts/agent_console.md. Las demás acciones siguen ejecutándose en
//  el chat real de VS Code.

async function sendMessageToCurrentChat(message, metadata = {}) {
  if (shouldRoutePromptToAgentConsole(metadata)) {
    try {
      const consolePath = writePromptToAgentConsole(message, metadata);
      log(
        `Mensaje redirigido a agent_console (${message.length} chars) -> ${consolePath}`,
      );
      return true;
    } catch (err) {
      log(`Error escribiendo agent_console: ${err.message}`);
      return false;
    }
  }

  log(`Enviando mensaje al chat (${message.length} chars)...`);

  // Método 1: API directa de VS Code con query + submit explícito.
  // isPartialQuery:true fuerza que el texto se quede en el input y evita que
  // VS Code haga auto-submit antes de tiempo. El plugin hace UN solo submit.
  try {
    await vscode.commands.executeCommand("workbench.action.chat.open", {
      query: message,
      isPartialQuery: true,
    });
    await sleep(250);
    await vscode.commands.executeCommand("workbench.action.chat.submit");
    log("Mensaje enviado vía workbench.action.chat.open + submit explícito");
    return true;
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

    // Reusar chat.open con query parcial y submit controlado evita el
    // doble-disparo que producía acciones/comandos erróneos en el chat.
    await vscode.commands.executeCommand("workbench.action.chat.open", {
      query: message,
      isPartialQuery: true,
    });
    await sleep(250);
    await vscode.commands.executeCommand("workbench.action.chat.submit");

    await vscode.env.clipboard.writeText(prevClipboard);
    log("Mensaje enviado vía clipboard + chat.open fallback");
    return true;
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

  return false;
}

// ─── Submit rastreado → contador de mensajes ──────────────────

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

async function openWorkspaceTextFile(relativePath, options = {}) {
  const { createIfMissing = false, revealLastLine = false } = options;
  const fullPath = resolveWorkspacePath(relativePath);

  try {
    if (!fs.existsSync(fullPath)) {
      if (!createIfMissing) return null;
      fs.mkdirSync(path.dirname(fullPath), { recursive: true });
      fs.writeFileSync(fullPath, "", "utf8");
    }

    const document = await vscode.workspace.openTextDocument(fullPath);
    const editor = await vscode.window.showTextDocument(document, {
      preview: false,
      preserveFocus: false,
    });

    if (revealLastLine && document.lineCount > 0) {
      const lastLine = document.lineAt(document.lineCount - 1);
      editor.selection = new vscode.Selection(
        lastLine.range.end,
        lastLine.range.end,
      );
      editor.revealRange(
        new vscode.Range(lastLine.range.end, lastLine.range.end),
        vscode.TextEditorRevealType.InCenterIfOutsideViewport,
      );
    }

    return fullPath;
  } catch (err) {
    vscode.window.showErrorMessage(
      `No se pudo abrir ${relativePath}: ${err.message}`,
    );
    return null;
  }
}

async function openAgentConsole() {
  const fullPath = await openWorkspaceTextFile(getAgentPromptConsolePath(), {
    createIfMissing: true,
    revealLastLine: true,
  });
  if (fullPath) {
    log(`[agent-menu] agent_console abierto: ${fullPath}`);
  }
  return fullPath;
}

async function openAgentPrompt() {
  const promptPath = getAgentPrompt6Path();
  const fullPath = await openWorkspaceTextFile(promptPath, {
    createIfMissing: false,
  });
  if (!fullPath) {
    vscode.window.showWarningMessage(
      `No se encontró ${promptPath}. Revisa modelCycler.agent.prompt6Path.`,
    );
    return null;
  }
  log(`[agent-menu] prompt_1 abierto: ${fullPath}`);
  return fullPath;
}

function buildAgentMenuItems() {
  const deliveryMode = getAgentPromptDeliveryMode();
  const isAgentConsole = deliveryMode === "agentConsole";
  const actionVerb = "Ejecutar";
  const consolePath = getAgentPromptConsolePath();
  const promptPath = getAgentPrompt6Path();
  const isAgentLoopRunning = Boolean(state.agentLoopTimer);

  return [
    {
      kind: vscode.QuickPickItemKind.Separator,
      label: isAgentConsole
        ? "Acciones del agente (prompts por consola)"
        : "Acciones del agente",
    },
    {
      label: `$(play) ${actionVerb} send_continue`,
      description: "chat actual",
      detail: 'Envía "Continuar" al chat actual',
      actionId: "send_continue",
    },
    {
      label: `$(comment-discussion) ${actionVerb} open_new_chat`,
      description: "nuevo chat",
      detail: isAgentConsole
        ? `Abre un chat nuevo y escribe solo el prompt en ${consolePath}`
        : "Abre un chat nuevo y envía el prompt configurado",
      actionId: "open_new_chat",
    },
    {
      label: `$(debug-stop) ${actionVerb} stop_and_new_chat`,
      description: isAgentConsole ? "stop + nuevo chat" : "stop + nuevo chat",
      detail: isAgentConsole
        ? `Detiene la respuesta actual, abre un chat nuevo y escribe el prompt en ${consolePath}`
        : "Detiene la respuesta actual, abre un chat nuevo y envía el prompt",
      actionId: "stop_and_new_chat",
    },
    {
      label: `$(sync) ${actionVerb} cycle_model`,
      description: "modelo + continuar",
      detail: 'Cambia al siguiente modelo y envía "Continuar" al chat',
      actionId: "cycle_model",
    },
    {
      label: `$(screen-full) ${actionVerb} focus_vscode`,
      description: "workbench",
      detail: "Trae VS Code al frente y enfoca el workbench",
      actionId: "focus_vscode",
    },
    {
      label: `$(file-code) ${actionVerb} execute_prompt6`,
      description: "prompt_1.md",
      detail: `Lee ${promptPath}, lo entrega según el modo actual y marca READ`,
      actionId: "execute_prompt6",
    },
    {
      kind: vscode.QuickPickItemKind.Separator,
      label: "Archivos",
    },
    {
      label: "$(go-to-file) Abrir agent_console.md",
      detail: consolePath,
      actionId: "open_agent_console",
    },
    {
      label: "$(book) Abrir prompt_1.md monitoreado",
      detail: promptPath,
      actionId: "open_agent_prompt",
    },
    {
      kind: vscode.QuickPickItemKind.Separator,
      label: "Control",
    },
    {
      label: "$(info) Ver estado del plugin",
      detail: "Muestra sesión, rutas, contador y modo activo",
      actionId: "show_status",
    },
    {
      label: isAgentLoopRunning
        ? "$(debug-stop) Detener AgentLoop"
        : "$(debug-start) Iniciar AgentLoop",
      detail: isAgentLoopRunning
        ? "Pausa la automatización del agente"
        : "Reanuda la automatización del agente",
      actionId: "toggle_agent",
    },
  ];
}

function describeManualAgentActionResult(actionId) {
  const isAgentConsole = getAgentPromptDeliveryMode() === "agentConsole";
  const consolePath = getAgentPromptConsolePath();
  const currentModelId =
    getCurrentCopilotModel() || getModels()[state.currentModelIndex] || "";
  const currentLabel = labelFor(currentModelId);

  switch (actionId) {
    case "send_continue":
      return "send_continue → enviado al chat actual";
    case "open_new_chat":
      return isAgentConsole
        ? `open_new_chat → nuevo chat abierto y prompt escrito en ${consolePath}`
        : "open_new_chat → nuevo chat lanzado con el prompt configurado";
    case "stop_and_new_chat":
      return isAgentConsole
        ? `stop_and_new_chat → stop ejecutado, nuevo chat abierto y prompt escrito en ${consolePath}`
        : "stop_and_new_chat → stop ejecutado y nuevo chat lanzado";
    case "cycle_model":
      return `cycle_model → ${currentLabel} y \"Continuar\" enviado al chat`;
    case "focus_vscode":
      return "focus_vscode → VS Code traído al frente";
    default:
      return `${actionId} → ejecutado`;
  }
}

async function executeManualAgentAction(actionId) {
  if (state.handlingEvent) {
    vscode.window.showWarningMessage(
      "El agente ya está ejecutando otra acción. Espera a que termine.",
    );
    return false;
  }

  state.handlingEvent = true;
  try {
    log(
      `[agent-menu] Acción manual: ${actionId} (modo=${getAgentPromptDeliveryMode()})`,
    );
    const ok = await runAgentAction(actionId, null, { manual: true });
    if (!ok) {
      vscode.window.showWarningMessage(
        `No se pudo ejecutar ${actionId}. Revisa el Output \"Copilot Model Cycler\".`,
      );
      return false;
    }
    if (actionId !== "execute_prompt6") {
      vscode.window.showInformationMessage(
        describeManualAgentActionResult(actionId),
      );
    }
    return true;
  } catch (err) {
    log(`[agent-menu] Error ejecutando ${actionId}: ${err.message}`);
    vscode.window.showErrorMessage(`Falló ${actionId}: ${err.message}`);
    return false;
  } finally {
    state.handlingEvent = false;
    refreshStatusBar();
  }
}

async function showAgentMenu() {
  const deliveryMode = getAgentPromptDeliveryMode();
  const selection = await vscode.window.showQuickPick(buildAgentMenuItems(), {
    title: `Copilot Model Cycler — Menú del agente (${deliveryMode})`,
    placeHolder:
      deliveryMode === "agentConsole"
        ? "Selecciona una acción: el agente ejecuta todo y solo los prompts van a agent_console.md"
        : "Selecciona una acción del agente",
    matchOnDetail: true,
  });

  if (!selection || selection.kind === vscode.QuickPickItemKind.Separator) {
    return;
  }

  switch (selection.actionId) {
    case "send_continue":
    case "open_new_chat":
    case "stop_and_new_chat":
    case "cycle_model":
    case "focus_vscode":
    case "execute_prompt6":
      await executeManualAgentAction(selection.actionId);
      break;
    case "open_agent_console":
      await openAgentConsole();
      break;
    case "open_agent_prompt":
      await openAgentPrompt();
      break;
    case "show_status":
      showStatus();
      break;
    case "toggle_agent":
      toggleAgentLoop();
      break;
    default:
      log(`[agent-menu] Opción no manejada: ${selection.actionId}`);
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
  refreshStatusBarTooltip();
}

function refreshStatusBarTooltip() {
  const deliveryMode = getAgentPromptDeliveryMode();
  const isAgentLoopRunning = Boolean(state.agentLoopTimer);

  state.statusBar.tooltip = [
    "Copilot Model Cycler",
    "───────────────────────────",
    `Modo agente: ${deliveryMode}`,
    `AgentLoop: ${isAgentLoopRunning ? "ON" : "OFF"}`,
    `Prompt de sesión: ${getSessionPromptFilePath()}`,
    `Prompt_1 monitoreado: ${getAgentPrompt6Path()}`,
    `Agent console: ${getAgentPromptConsolePath()}`,
    "───────────────────────────",
    "Click → menú del agente",
    "⌘⇧L → Rate limit",
    "⌘⇧E → Error de chat",
    "⌘⇧R → Reset de sesión",
    "⌘⇧M → Elegir modelo",
  ].join("\n");
}

function showStatus() {
  const models = getModels();
  const modelId = getCurrentCopilotModel();
  const maxMsg = cfg().get("session.maxMessages") ?? 25;
  const warnAt = cfg().get("session.warningAt") ?? 20;
  const autoRst = cfg().get("session.autoReset") ?? true;
  const sessionPromptPath = getSessionPromptFilePath();
  const agentPromptPath = getAgentPrompt6Path();
  const resetM = cfg().get("session.rateLimitCycleTo") || "next";

  const sessionPromptExists = workspacePathExists(sessionPromptPath);
  const agentPromptExists = workspacePathExists(agentPromptPath);
  const agentConsoleExists = workspacePathExists(getAgentPromptConsolePath());

  vscode.window.showInformationMessage(
    [
      `🤖 Modelo: ${labelFor(modelId)}`,
      `📊 Posición: ${models.indexOf(modelId) + 1}/${models.length}`,
      `💬 Mensajes: ${state.messageCount}/${maxMsg}`,
      `⚠️  Advertencia en: ${warnAt}`,
      `🔄 Auto-reset: ${autoRst ? "ON" : "OFF"}`,
      `📄 Prompt de sesión: ${sessionPromptPath} ${sessionPromptExists ? "✅" : "❌ NO ENCONTRADO"}`,
      `📋 Prompt del agente: ${agentPromptPath} ${agentPromptExists ? "✅" : "❌ NO ENCONTRADO"}`,
      `🧾 Entrega agente: ${getAgentPromptDeliveryMode()}`,
      `📝 Agent console: ${getAgentPromptConsolePath()} ${agentConsoleExists ? "✅" : "🆕 se crea al escribir"}`,
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

function getSessionPromptFilePath() {
  return cfg().get("session.promptFilePath") || ".prompts/prompt_1.md";
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

function getAgentPromptDeliveryMode() {
  return cfg().get("agent.promptDeliveryMode") || "agentConsole";
}

function getAgentPromptConsolePath() {
  return cfg().get("agent.promptConsolePath") || ".prompts/agent_console.md";
}

function getAgentPrompt6Path() {
  return cfg().get("agent.prompt6Path") || ".prompts/prompt_1.md";
}

function shouldRoutePromptToAgentConsole(metadata = {}) {
  return metadata.messageType === "prompt";
}

function resolveWorkspacePath(relativePath) {
  if (path.isAbsolute(relativePath)) return relativePath;

  const folders = vscode.workspace.workspaceFolders || [];
  if (!folders.length) return path.resolve(relativePath);

  return path.join(folders[0].uri.fsPath, relativePath);
}

function workspacePathExists(relativePath) {
  try {
    return fs.existsSync(resolveWorkspacePath(relativePath));
  } catch {
    return false;
  }
}

function writePromptToAgentConsole(message, metadata = {}) {
  const targetPath = resolveWorkspacePath(getAgentPromptConsolePath());
  fs.mkdirSync(path.dirname(targetPath), { recursive: true });

  const lines = [
    `## ${new Date().toISOString()} | ${metadata.messageType || "prompt"}`,
    `source: ${metadata.source || "plugin-vscode"}`,
    `reason: ${metadata.reason || "manual"}`,
    "",
    String(message || "").trimEnd(),
    "",
    "---",
    "",
  ];

  fs.appendFileSync(targetPath, lines.join("\n"), "utf8");
  return targetPath;
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

// ─── Toggle AgentLoop ─────────────────────────────────────────

function toggleAgentLoop() {
  if (state.agentLoopTimer) {
    clearInterval(state.agentLoopTimer);
    state.agentLoopTimer = null;
    state.agentEnabled = false;
    vscode.window.showInformationMessage("🤖 AgentLoop → DETENIDO");
    log("AgentLoop detenido manualmente");
  } else {
    state.agentEnabled = true;
    const interval = cfg().get("monitoring.pollInterval") ?? 3000;
    state.agentLoopTimer = setInterval(async () => {
      if (state.handlingEvent) return;
      await runAgentCycle();
    }, interval);
    vscode.window.showInformationMessage(
      `🤖 AgentLoop → INICIADO (cada ${interval}ms)`,
    );
    log("AgentLoop iniciado manualmente");
  }
  refreshStatusBar();
}

// ─── Forzar ejecución de prompt_1.md ──────────────────────────

function loadPrompt6Observation() {
  const folders = vscode.workspace.workspaceFolders || [];
  const { readPrompt6 } = require("./agent/monitor");
  const prompt6 = readPrompt6(folders, "");
  return {
    prompt6Content: prompt6.content,
    prompt6Path: prompt6.path,
  };
}

async function forceExecutePrompt6() {
  const promptObs = loadPrompt6Observation();
  if (!promptObs.prompt6Content || !promptObs.prompt6Path) {
    vscode.window.showWarningMessage(
      "prompt_1.md: archivo vacío o no encontrado",
    );
    return;
  }
  await executePrompt6(promptObs);
}

// ─── Desactivación ────────────────────────────────────────────

function deactivate() {
  if (state.pollTimer) clearInterval(state.pollTimer);
  if (state.statusBar) state.statusBar.dispose();
  out.dispose();
}

module.exports = { activate, deactivate };
