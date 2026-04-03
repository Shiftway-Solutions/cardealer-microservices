// ================================================================
//  agent/actions.js — Acciones nativas de VS Code
//
//  Todas las interacciones con el chat/UI de VS Code usando la
//  VS Code Extension API nativa (sin AppleScript, OCR ni Python).
//
//  Exporta funciones que extension.js llama desde el AgentLoop.
// ================================================================

"use strict";

const vscode = require("vscode");
const fs = require("fs");
const path = require("path");
const os = require("os");
const { execFile, spawn } = require("child_process");
const crypto = require("crypto");

// ─── Comandos de VS Code Copilot Chat ────────────────────────────────────────
// IDs confirmados con `vscode.commands.getCommands()` en VS Code 1.90+
const CMD_CHAT_OPEN = "workbench.action.chat.open";
const CMD_CHAT_NEW = "workbench.action.chat.newChat";
const CMD_CHAT_SUBMIT = "workbench.action.chat.submit";
const CMD_CHAT_STOP = "workbench.action.chat.stop";
const CMD_CHAT_FOCUS_INPUT = "workbench.action.chat.focusInput";

// ─── Dependencia circular: log() se inyecta desde extension.js ─────────────
let _log = (msg) => console.log(`[actions] ${msg}`);
function setLogger(fn) {
  _log = fn;
}

// ─── Helpers de workspace ────────────────────────────────────────────────────
//
//  getWorkspaceRoot — primer workspace folder del editor activo
//  getPythonBinary  — usa .venv/bin/python3 del repo si existe
//
function getWorkspaceRoot() {
  const folders = vscode.workspace.workspaceFolders || [];
  return folders.length ? folders[0].uri.fsPath : null;
}

function getPythonBinary() {
  const root = getWorkspaceRoot();
  if (root) {
    const venvPy = path.join(root, ".venv", "bin", "python3");
    try {
      if (fs.existsSync(venvPy)) return venvPy;
    } catch {}
  }
  return "python3";
}

function getAgentPromptDeliveryMode() {
  return (
    vscode.workspace
      .getConfiguration("modelCycler")
      .get("agent.promptDeliveryMode") || "agentConsole"
  );
}

function getAgentPromptConsolePath() {
  return (
    vscode.workspace
      .getConfiguration("modelCycler")
      .get("agent.promptConsolePath") || ".prompts/agent_console.md"
  );
}

function resolveAgentConsolePath() {
  const configuredPath = getAgentPromptConsolePath();
  if (path.isAbsolute(configuredPath)) return configuredPath;

  const root = getWorkspaceRoot();
  return root ? path.join(root, configuredPath) : path.resolve(configuredPath);
}

function writePromptToAgentConsole(message, metadata = {}) {
  const fullPath = resolveAgentConsolePath();
  fs.mkdirSync(path.dirname(fullPath), { recursive: true });

  const lines = [
    `## ${new Date().toISOString()} | ${metadata.messageType || "prompt"}`,
    `source: ${metadata.source || "plugin-vscode.agent.actions"}`,
    `reason: ${metadata.reason || "manual"}`,
    "",
    String(message || "").trimEnd(),
    "",
    "---",
    "",
  ];

  fs.appendFileSync(fullPath, lines.join("\n"), "utf8");
  return fullPath;
}

function shouldRoutePromptToAgentConsole(metadata = {}) {
  return (
    getAgentPromptDeliveryMode() === "agentConsole" &&
    metadata.messageType === "prompt"
  );
}

// ─── Guard de frescura del snapshot ──────────────────────────────────────────
//
//  Retorna true si chat_snapshot.txt fue modificado hace menos de maxAgeSecs.
//  Espejo de Python _snapshot_is_fresh() — abortar acción si chat activo.
//
function isChatSnapshotFresh(snapshotPath, maxAgeSecs = 3.0) {
  try {
    if (!snapshotPath || !fs.existsSync(snapshotPath)) return false;
    const ageSecs = (Date.now() - fs.statSync(snapshotPath).mtimeMs) / 1000;
    return ageSecs < maxAgeSecs;
  } catch {
    return false;
  }
}

// ─── code chat CLI (sin focus requerido) ─────────────────────────────────────
//
//  Envía un mensaje al chat de VS Code via CLI 'code chat --reuse-window'.
//  NO requiere que VS Code tenga el foco — funciona desde background.
//  Espejo de Python vscode_cli_chat().
//
//  Tier de fallback más robusto que el clipboard approach.
//
// Cache de verificación de code CLI
let _codeCliAvailable = null;
let _codeCliCheckedAt = 0;

async function isCodeCliAvailable() {
  const now = Date.now();
  // Re-check every 5 minutes
  if (_codeCliAvailable !== null && now - _codeCliCheckedAt < 300000)
    return _codeCliAvailable;
  return new Promise((resolve) => {
    execFile("which", ["code"], { timeout: 3000 }, (err, stdout) => {
      _codeCliAvailable = !err && stdout.trim().length > 0;
      _codeCliCheckedAt = now;
      if (!_codeCliAvailable)
        _log("isCodeCliAvailable: 'code' CLI no encontrado en PATH");
      resolve(_codeCliAvailable);
    });
  });
}

async function codeCliChat(message, mode = "agent") {
  const root = getWorkspaceRoot();
  if (!root) {
    _log("codeCliChat: no workspace root");
    return false;
  }
  const hasCode = await isCodeCliAvailable();
  if (!hasCode) {
    _log("codeCliChat: code CLI no disponible — skip");
    return false;
  }
  return new Promise((resolve) => {
    execFile(
      "code",
      ["chat", "--mode", mode, "--reuse-window", message],
      { cwd: root, timeout: 20000 },
      (err, _stdout, stderr) => {
        if (err) {
          _log(
            `codeCliChat error (rc=${err.code}): ${String(stderr).slice(0, 120)}`,
          );
        }
        resolve(!err);
      },
    );
  });
}

// ─── Enviar mensaje al chat actual ──────────────────────────────────────────
//
//  Estrategia idéntica a sendMessageToCurrentChat() en extension.js pero
//  centralizada aquí para que el AgentLoop la pueda usar directamente.
//  Fix crítico: usar isPartialQuery:true + submit explícito para evitar que
//  un auto-submit del chat se combine con un submit extra del plugin.
//
//  En agentConsole solo los mensajes tipo "prompt" se escriben al archivo.
//  Acciones como "Continuar" siguen ejecutándose en el chat real.
//
async function sendMessage(message, metadata = {}) {
  if (shouldRoutePromptToAgentConsole(metadata)) {
    try {
      const consolePath = writePromptToAgentConsole(message, metadata);
      _log(`sendMessage → agentConsole (${consolePath})`);
      return true;
    } catch (e) {
      _log(`sendMessage agentConsole falló: ${e.message}`);
      return false;
    }
  }

  _log(`sendMessage (${message.length} chars)...`);

  // Método 1: workbench.action.chat.open con query parcial + submit explícito
  try {
    await vscode.commands.executeCommand(CMD_CHAT_OPEN, {
      query: message,
      isPartialQuery: true,
    });
    await sleep(250);
    await vscode.commands.executeCommand(CMD_CHAT_SUBMIT);
    _log("sendMessage → OK (método 1)");
    return true;
  } catch (e) {
    _log(`sendMessage método 1 falló: ${e.message}`);
  }

  // Método 2: code chat CLI (no requiere focus — espejo de Python vscode_cli_chat Tier 1)
  try {
    if (await codeCliChat(message)) {
      _log("sendMessage → OK (método 2 code CLI)");
      return true;
    }
  } catch (e) {
    _log(`sendMessage método 2 (code CLI) falló: ${e.message}`);
  }

  // Método 3: clipboard fallback
  try {
    const prev = await vscode.env.clipboard.readText();
    await vscode.env.clipboard.writeText(message);
    await vscode.commands.executeCommand(CMD_CHAT_OPEN);
    await sleep(400);
    await vscode.commands.executeCommand(CMD_CHAT_OPEN, {
      query: message,
      isPartialQuery: true,
    });
    await sleep(250);
    await vscode.commands.executeCommand(CMD_CHAT_SUBMIT);
    await vscode.env.clipboard.writeText(prev).catch(() => {});
    _log("sendMessage → OK (método 3 clipboard)");
    return true;
  } catch (e) {
    _log(`sendMessage método 3 falló: ${e.message}`);
  }

  return false;
}

// ─── Continuar chat actual ────────────────────────────────────────────────────
async function sendContinue() {
  return sendMessage("Continuar", {
    source: "plugin-vscode.agent.actions.sendContinue",
    reason: "continue",
    messageType: "continue",
  });
}

// ─── Abrir nuevo chat ─────────────────────────────────────────────────────────
async function openNewChat() {
  try {
    await vscode.commands.executeCommand(CMD_CHAT_NEW);
    _log("openNewChat → OK (workbench.action.chat.newChat)");
    return true;
  } catch {
    try {
      await vscode.commands.executeCommand(CMD_CHAT_OPEN);
      _log("openNewChat → OK (workbench.action.chat.open fallback)");
      return true;
    } catch (e) {
      _log(`openNewChat falló: ${e.message}`);
      return false;
    }
  }
}

// ─── Detener generación ───────────────────────────────────────────────────────
async function stopGeneration() {
  try {
    await vscode.commands.executeCommand(CMD_CHAT_STOP);
    _log("stopGeneration → OK");
    return true;
  } catch (e) {
    _log(`stopGeneration falló: ${e.message}`);
    return false;
  }
}

// ─── Detener + abrir nuevo chat ───────────────────────────────────────────────
async function stopAndNewChat() {
  await stopGeneration();
  await sleep(600);
  return openNewChat();
}

// ─── Seleccionar modelo de chat ───────────────────────────────────────────────
//
//  Escribe en TRES lugares para garantizar que tanto el panel UI
//  como el 'code chat' CLI usen el modelo correcto:
//  1. settings globales (github.copilot.chat.languageModel) — panel VS Code
//  2. state.vscdb 'chat.currentLanguageModel.panel' — code chat CLI
//  3. vscode.lm.selectChatModels() si disponible (≥ 1.92)
//
async function selectChatModel(modelId) {
  _log(`selectChatModel("${modelId}")`);
  let ok = false;

  // Estrategia 1: settings globales (panel UI)
  try {
    await vscode.workspace
      .getConfiguration("github.copilot.chat")
      .update("languageModel", modelId, vscode.ConfigurationTarget.Global);
    _log(`selectChatModel → settings OK (${modelId})`);
    ok = true;
  } catch (e) {
    _log(`selectChatModel settings falló: ${e.message}`);
  }

  // Estrategia 2: state.vscdb (code chat CLI) — siempre intentar en paralelo
  setModelInDB(modelId)
    .then((dbOk) => {
      if (dbOk) _log(`selectChatModel → DB OK (${modelId})`);
      else _log(`selectChatModel → DB no actualizado para ${modelId}`);
    })
    .catch(() => {});

  // Estrategia 3: VS Code LM API (≥ 1.92)
  if (!ok && vscode.lm && typeof vscode.lm.selectChatModels === "function") {
    try {
      const models = await vscode.lm.selectChatModels({ id: modelId });
      if (models && models.length > 0) {
        _log(`selectChatModel → vscode.lm OK (${modelId})`);
        ok = true;
      }
    } catch (e) {
      _log(`selectChatModel vscode.lm falló: ${e.message}`);
    }
  }

  return ok;
}

// ─── Ciclar modelo (siguiente / anterior) ─────────────────────────────────────
//
//  Delega a modelCycler.cycleNext/Prev (ya implementado en extension.js).
//  Para que el AgentLoop pueda pedir un ciclo de modelo sin duplicar lógica.
//
async function cycleModelNext() {
  try {
    await vscode.commands.executeCommand("modelCycler.cycleNext");
    _log("cycleModelNext → OK");
    return true;
  } catch (e) {
    _log(`cycleModelNext falló: ${e.message}`);
    return false;
  }
}

// ─── Enfocar VS Code ──────────────────────────────────────────────────────────
async function focusVSCode() {
  try {
    await vscode.commands.executeCommand(CMD_CHAT_OPEN);
    _log("focusVSCode → OK");
    return true;
  } catch (e) {
    _log(`focusVSCode falló: ${e.message}`);
    return false;
  }
}

// ─── Leer archivo de prompt ───────────────────────────────────────────────────
//
//  Lee el archivo de prompt desde el workspace. Busca en todos los folders.
//
function readPromptFile(relativePath) {
  const folders = vscode.workspace.workspaceFolders || [];
  for (const folder of folders) {
    const fullPath = path.join(folder.uri.fsPath, relativePath);
    try {
      if (fs.existsSync(fullPath)) {
        return fs.readFileSync(fullPath, "utf8");
      }
    } catch {
      /* continuar */
    }
  }
  return null;
}

// ─── Escribir en archivo ──────────────────────────────────────────────────────
//
//  Append o write de texto en archivo del workspace.
//
function appendToFile(relativePath, text) {
  const folders = vscode.workspace.workspaceFolders || [];
  if (!folders.length) return false;
  const fullPath = path.join(folders[0].uri.fsPath, relativePath);
  try {
    fs.mkdirSync(path.dirname(fullPath), { recursive: true });
    fs.appendFileSync(fullPath, text, "utf8");
    return true;
  } catch (e) {
    _log(`appendToFile(${relativePath}) error: ${e.message}`);
    return false;
  }
}

function writeFile(relativePath, text) {
  const folders = vscode.workspace.workspaceFolders || [];
  if (!folders.length) return false;
  const fullPath = path.join(folders[0].uri.fsPath, relativePath);
  try {
    fs.mkdirSync(path.dirname(fullPath), { recursive: true });
    fs.writeFileSync(fullPath, text, "utf8");
    return true;
  } catch (e) {
    _log(`writeFile(${relativePath}) error: ${e.message}`);
    return false;
  }
}

// ─── Notificar usuario ────────────────────────────────────────────────────────
function notify(level, msg, ...actions) {
  if (level === "error") return vscode.window.showErrorMessage(msg, ...actions);
  if (level === "warning")
    return vscode.window.showWarningMessage(msg, ...actions);
  return vscode.window.showInformationMessage(msg, ...actions);
}

// ─── CDP — leer contenido del chat vía Playwright bridge ────────────────────
//
//  El plugin corre DENTRO de VS Code, por lo que tiene acceso a vscode.* API.
//  Sin embargo, la extensión API NO expone el contenido del Chat (DOM privado).
//
//  Solución: delegar a Python .venv + playwright ya instalado en el repo.
//  Espejo de Python read_chat_via_cdp() / is_cdp_available().
//
async function isCDPAvailable() {
  const httpM = require("http");
  return new Promise((resolve) => {
    const req = httpM.get(
      "http://localhost:9222/json/version",
      { timeout: 3000 },
      (res) => resolve(res.statusCode === 200),
    );
    req.on("error", () => resolve(false));
    req.setTimeout(3000, () => {
      req.destroy();
      resolve(false);
    });
  });
}

/**
 * Lee el texto del chat Copilot vía CDP + Playwright.
 * Delega a Python actions.py (playwright ya instalado en .venv).
 * Returns: string | null
 */
async function readChatViaCDP() {
  const root = getWorkspaceRoot();
  if (!root) return null;
  const pyBin = getPythonBinary();
  const smDir = path
    .join(root, ".prompts", "agent", "smart_monitor")
    .replace(/\\/g, "/");
  const pyScript = [
    "import sys",
    `sys.path.insert(0, "${smDir}")`,
    "from actions import read_chat_via_cdp",
    "result = read_chat_via_cdp()",
    "print(result or '', end='')",
  ].join("\n");
  return new Promise((resolve) => {
    let out = "";
    const proc = spawn(pyBin, ["-c", pyScript], { cwd: root, timeout: 10000 });
    proc.stdout.on("data", (d) => {
      out += d;
    });
    proc.on("close", () => resolve(out.trim() || null));
    proc.on("error", () => resolve(null));
  });
}

/**
 * Lee chat via CDP y lo escribe en snapshotPath (mantiene monitor.js fresco).
 * Solo escribe si el snapshot NO es fresco (evita sobre-escribir generación activa).
 * Returns: boolean (true = snapshot actualizado)
 */
async function writeSnapshotFromCDP(snapshotPath) {
  if (isChatSnapshotFresh(snapshotPath, 3.0)) {
    _log("writeSnapshotFromCDP: snapshot fresco — omitiendo");
    return false;
  }
  const content = await readChatViaCDP();
  if (!content) return false;
  try {
    fs.mkdirSync(path.dirname(snapshotPath), { recursive: true });
    fs.writeFileSync(snapshotPath, content, "utf8");
    _log(`writeSnapshotFromCDP → ${content.length} chars guardados`);
    return true;
  } catch (e) {
    _log(`writeSnapshotFromCDP error: ${e.message}`);
    return false;
  }
}

// ─── VS Code State DB (state.vscdb) — gestión de modelos para code chat CLI ──
//
//  El modelo activo para 'code chat' CLI se lee de la clave:
//    'chat.currentLanguageModel.panel'  en state.vscdb
//
//  La settings.json NO afecta el CLI — solo el DB.
//  Espejo de Python get_current_model(), cycle_model_next(), get_model_pool().
//
const _STATE_DB_PATH = path.join(
  os.homedir(),
  "Library",
  "Application Support",
  "Code",
  "User",
  "globalStorage",
  "state.vscdb",
);

// Pool de modelos en orden de prioridad (espejo de Python _PRIORITY_POOL)
const PRIORITY_POOL = [
  "copilot/claude-haiku-4.5",
  "copilot/gemini-3-flash-preview",
  "copilot/gpt-5.1-codex-mini",
  "copilot/gpt-5.4-mini",
  "copilot/gpt-4.1",
  "copilot/gpt-5-mini",
  "copilot/oswe-vscode-prime",
];

/** Ejecuta SQL en state.vscdb via Python (sqlite3 built-in). */
function _pyStateDB(pyScript) {
  const pyBin = getPythonBinary();
  const root = getWorkspaceRoot();
  return new Promise((resolve) => {
    let out = "";
    const proc = spawn(pyBin, ["-c", pyScript], {
      cwd: root || process.cwd(),
      timeout: 5000,
    });
    proc.stdout.on("data", (d) => {
      out += d;
    });
    proc.on("close", () => resolve(out.trim()));
    proc.on("error", () => resolve(""));
  });
}

/** Lee el modelo activo del DB de VS Code. Returns: "copilot/..." | "" */
async function getCurrentModelFromDB() {
  const db = _STATE_DB_PATH.replace(/\\/g, "/");
  return _pyStateDB(
    `import sqlite3\ntry:\n  c=sqlite3.connect(r'${db}',timeout=5)` +
      `.execute("SELECT value FROM ItemTable WHERE key='chat.currentLanguageModel.panel'")` +
      `.fetchone();print(c[0] if c else '',end='')\nexcept:print('',end='')`,
  );
}

/** Escribe un modelId en state.vscdb. Returns: boolean */
async function setModelInDB(modelId) {
  if (!/^[\w.\-]+\/[\w.\-]+$/.test(modelId)) {
    _log(`setModelInDB: ID inválido "${modelId}"`);
    return false;
  }
  const db = _STATE_DB_PATH.replace(/\\/g, "/");
  const escaped = modelId.replace(/'/g, "\\'");
  const result = await _pyStateDB(
    `import sqlite3\ntry:\n  con=sqlite3.connect(r'${db}',timeout=5)` +
      `;con.execute("PRAGMA journal_mode=DELETE;")` +
      `;con.execute("UPDATE ItemTable SET value=? WHERE key='chat.currentLanguageModel.panel'"` +
      `,(r'${escaped}',));con.commit();print('ok',end='')\nexcept Exception as e:print('err:'+str(e),end='')`,
  );
  const ok = result === "ok";
  _log(`setModelInDB("${modelId}") → ${ok ? "ok" : result}`);
  return ok;
}

/**
 * Lee el pool de modelos de chatModelRecentlyUsed en state.vscdb.
 * Retorna la intersección con PRIORITY_POOL (ordenada por tier).
 * Espejo de Python get_model_pool().
 */
async function getModelPoolFromDB() {
  const db = _STATE_DB_PATH.replace(/\\/g, "/");
  const raw = await _pyStateDB(
    `import sqlite3\ntry:\n  c=sqlite3.connect(r'${db}',timeout=5)` +
      `.execute("SELECT value FROM ItemTable WHERE key='chatModelRecentlyUsed'")` +
      `.fetchone();print(c[0] if c else '',end='')\nexcept:print('',end='')`,
  );
  if (!raw) return [...PRIORITY_POOL];
  try {
    const pool = JSON.parse(raw);
    if (Array.isArray(pool)) {
      const available = new Set(
        pool.filter((m) => /^[\w.\-]+\/[\w.\-]+$/.test(String(m))),
      );
      const intersection = PRIORITY_POOL.filter((m) => available.has(m));
      if (intersection.length) {
        _log(
          `getModelPoolFromDB: ${intersection.length} modelos prioritarios disponibles`,
        );
        return intersection;
      }
    }
  } catch (e) {
    _log(`getModelPoolFromDB parse error: ${e.message}`);
  }
  return [...PRIORITY_POOL];
}

/**
 * Cicla al siguiente modelo en el pool y lo escribe en state.vscdb + settings.
 * Espejo de Python cycle_model_next().
 * @param {number} currentIdx - índice actual en el pool
 * @returns {{ modelId: string, newIdx: number, pool: string[] }}
 */
async function cycleModelNextViaDB(currentIdx = 0) {
  const pool = await getModelPoolFromDB();
  const safeIdx = currentIdx >= 0 && currentIdx < pool.length ? currentIdx : 0;
  const newIdx = (safeIdx + 1) % pool.length;
  const modelId = pool[newIdx];
  // Escribir en state.vscdb (code chat CLI) + settings (VS Code panel UI)
  await Promise.all([setModelInDB(modelId), selectChatModel(modelId)]);
  _log(
    `cycleModelNextViaDB: ${pool[safeIdx]} → ${modelId} (idx ${safeIdx}→${newIdx}/${pool.length - 1})`,
  );
  return { modelId, newIdx, pool };
}

// ─── vscode:// URL commands (macOS — sin focus requerido) ────────────────────
//
//  Espejo de Python vscode_exec_command() / ensure_vscode_focused().
//  Usa 'open vscode://command/...' via child_process — no requiere el foco.
//
async function execVSCodeURLCommand(commandId) {
  return new Promise((resolve) => {
    execFile(
      "open",
      [`vscode://command/${commandId}`],
      { timeout: 5000 },
      (err) => {
        setTimeout(() => resolve(!err), 300);
      },
    );
  });
}

/**
 * Activa VS Code y lo trae al frente (equivalente a AppleScript 'activate').
 * Returns: boolean
 */
async function ensureVSCodeFocused() {
  return new Promise((resolve) => {
    execFile("open", ["-a", "Visual Studio Code"], { timeout: 5000 }, (err) => {
      setTimeout(() => resolve(!err), 500);
    });
  });
}

// ─── Clasificación de texto del chat ─────────────────────────────────────────
//
//  Detecta errores y cambios comparando con hash previo.
//  Espejo de Python classify_chat_text().
//
const _CHAT_TEXT_ERROR_PATTERNS = [
  {
    re: /rate.limit|429|Too Many Requests|quota.*exhaust|exhausted.*quota|RateLimitError/i,
    type: "rate_limited",
  },
  {
    re: /overloaded|503|502|500|Internal Server Error|capacity|overloaded_error/i,
    type: "hard_error",
  },
  { re: /cancelled|canceled/i, type: "cancelled" },
  {
    re: /switch.*(?:model|agent)|change.*model|try.*different.*model|model.*not.*available/i,
    type: "agent_switch",
  },
  {
    re: /context.*(?:full|limit|too.long|exceeded|window)|conversation.*too.long|This conversation is too long/i,
    type: "context_full",
  },
];

function _md5Hash(text) {
  return crypto.createHash("md5").update(text, "utf8").digest("hex");
}

/**
 * Clasifica el texto actual del chat y detecta cambios respecto al hash previo.
 * Returns: { type: string, hasChanged: boolean, hash: string }
 */
function classifyChatText(currentText, prevHash = "") {
  const currentHash = _md5Hash(currentText);
  const hasChanged = currentHash !== prevHash;
  const tail = currentText.length > 500 ? currentText.slice(-500) : currentText;
  for (const { re, type } of _CHAT_TEXT_ERROR_PATTERNS) {
    if (re.test(tail)) return { type, hasChanged, hash: currentHash };
  }
  return {
    type: hasChanged ? "success" : "unknown",
    hasChanged,
    hash: currentHash,
  };
}

// ─── Screenshots (macOS screencapture) ───────────────────────────────────────
//
//  Captura la ventana de VS Code como PNG y guarda en:
//    .prompts/agent/smart_monitor/screenshots/
//
//  Espejo de Python capture_workbench_screenshot().
//  Usa el Window ID de AppleScript para captura dirigida (sin interferencia de
//  otras ventanas). Si AppleScript falla → full-screen como fallback.
//
//  REQUIERE: macOS (screencapture es exclusivo de macOS).
//

/**
 * Obtiene el CGWindowID de la ventana principal de VS Code vía Quartz.
 * El CGWindowID se usa con `screencapture -l <id>` para captura dirigida.
 * AppleScript `id of first window` NO funciona para Electron/VS Code.
 * Returns: string (CGWindowID) | null
 */
async function getVSCodeWindowId() {
  const pyBin = getPythonBinary();
  const pyScript = [
    "import Quartz",
    "windows = Quartz.CGWindowListCopyWindowInfo(Quartz.kCGWindowListOptionOnScreenOnly, Quartz.kCGNullWindowID)",
    "for w in windows:",
    "  if 'Code' in str(w.get('kCGWindowOwnerName', '')) and w.get('kCGWindowLayer', -1) == 0:",
    "    print(w['kCGWindowNumber'], end=''); break",
  ].join("\n");
  return new Promise((resolve) => {
    execFile(pyBin, ["-c", pyScript], { timeout: 5000 }, (err, stdout) => {
      if (err) {
        _log(
          `getVSCodeWindowId: Quartz falló (${err.message}) — fallback a captura completa`,
        );
        return resolve(null);
      }
      const id = stdout.trim();
      resolve(/^\d+$/.test(id) ? id : null);
    });
  });
}

/**
 * Captura la ventana de VS Code como PNG.
 * @param {string} label  Etiqueta corta para el nombre del archivo (ej: "pre_cycle_model")
 * @returns {Promise<string|null>} Ruta absoluta al PNG, o null si falla.
 */
async function captureScreenshot(label = "action") {
  const root = getWorkspaceRoot();
  const screenshotsDir = root
    ? path.join(root, ".prompts", "agent", "smart_monitor", "screenshots")
    : path.join(os.homedir(), ".okla_screenshots");

  try {
    fs.mkdirSync(screenshotsDir, { recursive: true });
  } catch {
    /* si ya existe, ignorar */
  }

  const ts = new Date()
    .toISOString()
    .replace(/[:.]/g, "")
    .replace("T", "_")
    .slice(0, 15);
  const safeLabel = label.replace(/[^a-zA-Z0-9_\-]/g, "_").slice(0, 40);
  const pngPath = path.join(screenshotsDir, `${ts}_${safeLabel}.png`);

  const windowId = await getVSCodeWindowId();
  const spawnArgs = windowId
    ? ["-x", "-l", windowId, pngPath]
    : ["-x", pngPath];

  return new Promise((resolve) => {
    execFile("screencapture", spawnArgs, { timeout: 10000 }, (err) => {
      if (err || !fs.existsSync(pngPath)) {
        _log(
          `captureScreenshot("${label}") falló: ${err?.message || "archivo no creado"}`,
        );
        resolve(null);
      } else {
        _log(`captureScreenshot → ${path.basename(pngPath)}`);
        resolve(pngPath);
      }
    });
  });
}

/**
 * Toma N screenshots del chat con breve delay entre ellas.
 * Usadas por el Brain para enviar contexto visual al modelo Copilot.
 * @param {string} label  Etiqueta base (se añade _1, _2, ...)
 * @param {number} count  Cantidad de capturas (default: 2)
 * @returns {Promise<string[]>} Array de rutas PNG (puede ser vacío).
 */
async function takeChatScreenshots(label = "agent", count = 2) {
  const shots = [];
  for (let i = 0; i < count; i++) {
    if (i > 0) await sleep(500); // delay entre capturas
    const p = await captureScreenshot(`${label}_${i + 1}`);
    if (p) shots.push(p);
  }
  if (shots.length) {
    _log(
      `takeChatScreenshots("${label}"): ${shots.length}/${count} capturas OK`,
    );
  }
  return shots;
}

// ─── Helper ────────────────────────────────────────────────────────────────────
function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

module.exports = {
  setLogger,
  // Envío de mensajes
  sendMessage,
  sendContinue,
  codeCliChat,
  isCodeCliAvailable,
  // Chat control
  openNewChat,
  stopGeneration,
  stopAndNewChat,
  // Modelos
  selectChatModel,
  cycleModelNext,
  // State DB (code chat CLI model management)
  getCurrentModelFromDB,
  setModelInDB,
  getModelPoolFromDB,
  cycleModelNextViaDB,
  PRIORITY_POOL,
  // CDP — lectura del chat
  isCDPAvailable,
  readChatViaCDP,
  writeSnapshotFromCDP,
  // Snapshot guard
  isChatSnapshotFresh,
  // Clasificación de texto
  classifyChatText,
  // URL commands + focus (macOS)
  execVSCodeURLCommand,
  ensureVSCodeFocused,
  // VS Code focus/UI
  focusVSCode,
  // Screenshots (macOS)
  getVSCodeWindowId,
  captureScreenshot,
  takeChatScreenshots,
  // Archivos
  readPromptFile,
  appendToFile,
  writeFile,
  // Notificaciones
  notify,
  // Helper
  sleep,
};
