// ================================================================
//  agent/monitor.js — Observer en JavaScript
//
//  Recopila el estado completo del entorno en un objeto Observation:
//    - Copilot log (lectura incremental por offset)
//    - chat_snapshot.txt (lectura de snapshot del chat)
//    - .prompts/prompt_1.md (monitoreo de instrucciones CPSO)
//    - Timers y contadores del estado persistente
//
//  Equivalente al observer.py del Python agent, pero corriendo
//  dentro del proceso de VS Code como JavaScript nativo.
//
//  No tiene dependencias de vscode.* — solo fs/path/os para que
//  sea testeable fuera de VS Code si se necesita.
// ================================================================

"use strict";

const fs = require("fs");
const path = require("path");
const os = require("os");

// ─── Log injection ──────────────────────────────────────────────────────────
let _log = (msg) => console.log(`[monitor] ${msg}`);
function setLogger(fn) {
  _log = fn;
}

// ─── Constantes ──────────────────────────────────────────────────────────────
const COPILOT_LOG_NAME = "GitHub Copilot Chat.log";

// Patrones de error — idénticos a LOG_PATTERNS del extension.js + Python observer
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
  {
    re: /context.*(?:full|limit|window)|context_length_exceeded|max_tokens/i,
    type: "context_full",
  },
];
const BENIGN_WARNING_RE =
  /\[warning\] Tool .* failed validation: schema must be an object if present/;

// Patrones para detectar generación activa en el snapshot del chat
const ACTIVE_GENERATION_RE =
  /Running\s+\w|Compacting conversation|Analyzing\s|Reading\s|Searching\s|Writing\s|Editing\s|Calling\s|\d+%|Thinking\.\.\.|Preparing\.\.\.|●/i;

// ─── Estructura Observation ──────────────────────────────────────────────────
//
//  Objeto plano equivalente al @dataclass Observation de observer.py
//
function createObservation(overrides = {}) {
  return {
    timestamp: Date.now(),
    timestampHuman: new Date().toISOString(),

    // Copilot log
    logPath: "",
    logNewLinesCount: 0,
    logEvents: {}, // { "rate_limited": 1, "success": 2, ... }
    logDominantEvent: "idle",

    // Chat snapshot
    snapshotExists: false,
    snapshotMtimeMs: 0,
    snapshotAgeSecs: 0,
    snapshotBodyHash: "",
    snapshotBodyChanged: false,
    snapshotSizeChars: 0,
    snapshotTail500: "",
    snapshotErrors: [],
    snapshotHasActiveGeneration: false,
    snapshotHasModelFooter: false,

    // Context saturation
    contextSaturated: false,
    chatUiSwitchNeeded: false,

    // Stall detection guards
    // true only when ≥1 Copilot log event was seen since session start.
    // R7/R8 (stall → send_continue / open_new_chat) must NOT fire until
    // Copilot has actually been used — otherwise idle VS Code sessions
    // generate a flood of "continuar" prompts that cost money.
    hasEverSeenCopilotActivity: false,
    // Last dominant log event seen (idle / success / rate_limited / …)
    // R7/R8 must NOT fire right after a successful completion.
    lastCompletedEvent: "idle",

    // Timers
    secsSinceLastActivity: 0,
    secsSinceLastAction: 0,
    secsSinceLastNewChat: 0,
    secsSinceLastContinue: 0,

    // Contadores del estado
    errorContinueCount: 0,
    rateLimitCount: 0,
    newChatCount: 0,
    continueCount: 0,
    sessionAgeMins: 0,
    sessionTooLong: false,

    // prompt_1.md (regla CPSO)
    prompt6Changed: false,
    prompt6Content: "",
    prompt6Path: "",

    // Summary para Brain
    summary: "",

    ...overrides,
  };
}

// ─── Directorio de logs de VS Code ───────────────────────────────────────────
function getVSCodeLogsDir() {
  const home = os.homedir();
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

// ─── Encontrar log de Copilot más reciente ───────────────────────────────────
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

// ─── Leer nuevas líneas del log (incremental) ────────────────────────────────
//
//  Retorna { newContent, newOffset } para que el caller actualice el estado.
//
function readNewLogLines(logPath, offset) {
  try {
    const stat = fs.statSync(logPath);
    const size = stat.size;
    if (size < offset) {
      _log(`Log rotado — reseteando offset`);
      return { newContent: "", newOffset: 0, mtimeMs: stat.mtimeMs };
    }
    if (size <= offset)
      return { newContent: "", newOffset: offset, mtimeMs: stat.mtimeMs };

    const fd = fs.openSync(logPath, "r");
    const buffer = Buffer.allocUnsafe(size - offset);
    fs.readSync(fd, buffer, 0, buffer.length, offset);
    fs.closeSync(fd);
    return {
      newContent: buffer.toString("utf8"),
      newOffset: size,
      mtimeMs: stat.mtimeMs,
    };
  } catch {
    return { newContent: "", newOffset: offset, mtimeMs: 0 };
  }
}

// ─── Analizar contenido del log ───────────────────────────────────────────────
function analyzeLogContent(content) {
  const events = {};
  let hasRateLimit = false;
  let hasHardError = false;
  let hasContextFull = false;

  for (const line of content.split("\n")) {
    if (BENIGN_WARNING_RE.test(line)) continue;
    for (const { re, type } of LOG_PATTERNS) {
      if (re.test(line)) {
        events[type] = (events[type] || 0) + 1;
        if (type === "rate_limited") hasRateLimit = true;
        if (type === "hard_error") hasHardError = true;
        if (type === "context_full") hasContextFull = true;
        break;
      }
    }
  }

  let dominant = "idle";
  if (hasRateLimit) dominant = "rate_limited";
  else if (hasHardError) dominant = "hard_error";
  else if (hasContextFull) dominant = "context_full";
  else if (events["cancelled"]) dominant = "cancelled";
  else if (events["loop_stopped"]) dominant = "loop_stopped";
  else if (events["success"]) dominant = "success";

  return { events, dominant, hasContextFull };
}

// ─── Leer y analizar chat_snapshot.txt ────────────────────────────────────────
function readChatSnapshot(snapshotPath) {
  if (!snapshotPath || !fs.existsSync(snapshotPath)) {
    return { exists: false };
  }
  try {
    const stat = fs.statSync(snapshotPath);
    const content = fs.readFileSync(snapshotPath, "utf8");
    const ageSecs = (Date.now() - stat.mtimeMs) / 1000;

    // Detectar errores en el snapshot
    const errors = [];
    for (const { re, type } of LOG_PATTERNS) {
      if (re.test(content)) errors.push(type);
    }

    // Detectar generación activa
    const tail500 = content.slice(-500);
    const hasActiveGeneration = ACTIVE_GENERATION_RE.test(tail500);

    // Detectar footer del modelo (sesión terminada normalmente)
    const hasModelFooter = /claude|gpt|gemini|sonnet|haiku|opus/i.test(
      tail500.slice(-100),
    );

    const hash = simpleHash(content);

    return {
      exists: true,
      mtimeMs: stat.mtimeMs,
      ageSecs,
      sizeChars: content.length,
      tail500,
      errors,
      hasActiveGeneration,
      hasModelFooter,
      hash,
      content,
    };
  } catch {
    return { exists: false };
  }
}

// ─── Leer prompt_1.md (regla CPSO) ────────────────────────────────────────────
//
//  Retorna { changed, content, path } comparando con lastHash.
//
function readPrompt6(workspaceFolders, lastHash) {
  const PROMPT6_PATH = ".prompts/prompt_1.md";

  for (const folder of workspaceFolders || []) {
    const fullPath = path.join(
      folder.uri ? folder.uri.fsPath : folder,
      PROMPT6_PATH,
    );
    try {
      if (!fs.existsSync(fullPath)) continue;
      const content = fs.readFileSync(fullPath, "utf8");
      // Ignorar si termina en READ (ya procesado) o está vacío
      const trimmed = content.trim();
      if (!trimmed || trimmed.endsWith("READ")) {
        return { changed: false, content: trimmed, path: fullPath };
      }
      const hash = simpleHash(trimmed);
      const changed = hash !== lastHash;
      return { changed, content: trimmed, path: fullPath };
    } catch {
      /* continuar */
    }
  }
  return { changed: false, content: "", path: "" };
}

// ─── Hash simple (FNV-like) ───────────────────────────────────────────────────
function simpleHash(str) {
  let h = 0x811c9dc5;
  for (let i = 0; i < str.length; i++) {
    h ^= str.charCodeAt(i);
    h = (h * 0x01000193) >>> 0;
  }
  return h.toString(16);
}

// ─── Construir resumen para Brain ─────────────────────────────────────────────
function buildSummary(obs) {
  const parts = [];
  if (obs.snapshotHasActiveGeneration) parts.push("GENERACIÓN ACTIVA");
  if (obs.logDominantEvent !== "idle")
    parts.push(`LOG:${obs.logDominantEvent.toUpperCase()}`);
  if (obs.snapshotErrors.length)
    parts.push(`CHAT_ERRORS:[${obs.snapshotErrors.join(",")}]`);
  if (obs.contextSaturated) parts.push("CONTEXTO_LLENO");
  if (obs.secsSinceLastActivity > 480)
    parts.push(`STALL:${Math.round(obs.secsSinceLastActivity)}s`);
  else if (obs.secsSinceLastActivity > 300)
    parts.push(`SOFT_STALL:${Math.round(obs.secsSinceLastActivity)}s`);
  if (obs.sessionTooLong) parts.push("SESSION_LARGA");
  if (obs.prompt6Changed) parts.push("PROMPT6_NUEVO");
  return parts.length ? parts.join(" | ") : "NORMAL";
}

// ─── Clase Monitor ────────────────────────────────────────────────────────────
class Monitor {
  constructor() {
    this._logPath = "";
    this._logOffset = 0;
    this._snapshotHash = "";
    this._prompt6Hash = "";
    this._chatSnapshotPath = ""; // se inyecta desde afuera
    this._workspaceFolders = []; // se inyecta desde afuera
    this._sessionStartTs = Date.now();
  }

  // Inyectar dependencias (llamar desde extension.js en activate)
  configure({ chatSnapshotPath, workspaceFolders }) {
    this._chatSnapshotPath = chatSnapshotPath || "";
    this._workspaceFolders = workspaceFolders || [];
  }

  // ─── Ciclo de observación ────────────────────────────────────────────────
  //
  //  Recibe el estado persistente del agente y retorna un objeto Observation.
  //  El estado se actualiza in-place (offsets del log, hashes, etc.).
  //
  observe(agentState) {
    const now = Date.now();
    const obs = createObservation();

    // ── 1. Copilot log ───────────────────────────────────────────────
    const logsDir = getVSCodeLogsDir();
    if (logsDir) {
      // Re-descubrir log solo si cambió o desapareció
      const needsDiscover = !this._logPath || !fs.existsSync(this._logPath);
      if (needsDiscover) {
        const found = findActiveCopilotLog(logsDir);
        if (found && found !== this._logPath) {
          _log(`Nuevo log activo: ${found}`);
          this._logPath = found;
          // BUG FIX: Start reading from the END of the file, not from offset 0.
          // Offset 0 causes the monitor to scan the entire historical log on
          // first activation and find old rate_limit / hard_error events from
          // previous sessions — triggering send_continue / open_new_chat
          // immediately upon plugin install, costing money for zero reason.
          try {
            this._logOffset = fs.statSync(found).size;
          } catch {
            this._logOffset = 0;
          }
        }
      }

      if (this._logPath) {
        const { newContent, newOffset, mtimeMs } = readNewLogLines(
          this._logPath,
          this._logOffset,
        );
        this._logOffset = newOffset;

        if (newContent) {
          const { events, dominant, hasContextFull } =
            analyzeLogContent(newContent);
          obs.logPath = this._logPath;
          obs.logNewLinesCount = newContent.split("\n").length;
          obs.logEvents = events;
          obs.logDominantEvent = dominant;
          obs.contextSaturated = hasContextFull;

          if (dominant === "rate_limited" || dominant === "hard_error") {
            obs.chatUiSwitchNeeded = true;
          }

          // Actualizar timestamp de actividad si hubo eventos
          if (Object.keys(events).length > 0) {
            agentState.lastActivityTs = now;
            // Mark that Copilot was actually used in this session
            agentState.hasEverSeenCopilotActivity = true;
            // Track last completed event for stall-detection gating
            if (dominant === "success" || dominant === "cancelled") {
              agentState.lastCompletedEvent = dominant;
            } else if (
              dominant === "rate_limited" ||
              dominant === "hard_error"
            ) {
              agentState.lastCompletedEvent = dominant;
            }
          }
        }

        // Actualizar isAgentBusy basado en mtime del log
        try {
          const stat = fs.statSync(this._logPath);
          const pollMs = 3000;
          agentState.isAgentBusy = now - stat.mtimeMs < pollMs * 2;
        } catch {
          /* ignorar */
        }
      }
    }

    // ── 2. Chat snapshot ─────────────────────────────────────────────
    const snap = readChatSnapshot(this._chatSnapshotPath);
    if (snap.exists) {
      obs.snapshotExists = true;
      obs.snapshotMtimeMs = snap.mtimeMs;
      obs.snapshotAgeSecs = snap.ageSecs;
      obs.snapshotSizeChars = snap.sizeChars;
      obs.snapshotTail500 = snap.tail500;
      obs.snapshotErrors = snap.errors;
      obs.snapshotHasActiveGeneration = snap.hasActiveGeneration;
      obs.snapshotHasModelFooter = snap.hasModelFooter;
      obs.snapshotBodyHash = snap.hash;
      obs.snapshotBodyChanged = snap.hash !== this._snapshotHash;
      this._snapshotHash = snap.hash;

      // Si el snapshot cambió recientemente → actualizar actividad
      if (obs.snapshotBodyChanged || snap.ageSecs < 30) {
        agentState.lastActivityTs = now;
      }

      // Context saturation por tamaño del snapshot
      if (snap.sizeChars > 600000) obs.contextSaturated = true;
    }

    // ── 3. prompt_1.md ───────────────────────────────────────────────
    const p6 = readPrompt6(this._workspaceFolders, this._prompt6Hash);
    if (p6.changed) {
      if (this._prompt6Hash === "") {
        // BUG FIX: First observe() cycle — _prompt6Hash is empty (just activated).
        // Any existing prompt_1.md would look like a "new change" and get sent
        // to the chat immediately — costing tokens on install for no reason.
        // Silently seed the hash so only FUTURE edits trigger execute_prompt6.
        _log(
          "prompt_6: Bootstrap — seeding initial hash, skipping execute_prompt6",
        );
      } else {
        // Genuine change made AFTER the extension was already running.
        obs.prompt6Changed = true;
      }
      this._prompt6Hash = simpleHash(p6.content);
    }
    obs.prompt6Content = p6.content;
    obs.prompt6Path = p6.path;

    // ── 3b. Stall-detection guards ────────────────────────────────────
    obs.hasEverSeenCopilotActivity =
      agentState.hasEverSeenCopilotActivity === true;
    obs.lastCompletedEvent = agentState.lastCompletedEvent || "idle";

    // ── 4. Timers ────────────────────────────────────────────────────
    obs.secsSinceLastActivity =
      (now - (agentState.lastActivityTs || now)) / 1000;
    obs.secsSinceLastAction = (now - (agentState.lastActionTs || 0)) / 1000;
    obs.secsSinceLastNewChat = (now - (agentState.lastNewChatTs || 0)) / 1000;
    obs.secsSinceLastContinue = (now - (agentState.lastContinueTs || 0)) / 1000;

    // ── 5. Contadores ─────────────────────────────────────────────────
    obs.errorContinueCount = agentState.errorContinueCount || 0;
    obs.rateLimitCount = agentState.rateLimitCount || 0;
    obs.newChatCount = agentState.newChatCount || 0;
    obs.continueCount = agentState.continueCount || 0;

    // ── 6. Sesión larga ───────────────────────────────────────────────
    obs.sessionAgeMins = (now - this._sessionStartTs) / 60000;
    obs.sessionTooLong = obs.sessionAgeMins > 180 || obs.continueCount > 20;

    // ── 7. Resumen ────────────────────────────────────────────────────
    obs.summary = buildSummary(obs);

    return obs;
  }
}

module.exports = {
  Monitor,
  createObservation,
  LOG_PATTERNS,
  readPrompt6,
  simpleHash,
  setLogger,
};
