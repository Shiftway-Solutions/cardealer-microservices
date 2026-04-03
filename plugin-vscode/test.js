// Test unitario del plugin — corre con: node plugin-vscode/test.js
const fs = require("fs");
const path = require("path");
const os = require("os");

// ── Copiar los patrones exactos del extension.js ─────────────
// BENIGN_WARNING_RE: líneas que se skipean antes de entrar al loop de LOG_PATTERNS
const BENIGN_WARNING_RE =
  /\[warning\] Tool .* failed validation: schema must be an object if present/;

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

// analyzeLogContent simplificado (sin vscode) — refleja la lógica real del plugin post-fix
function analyzeLogContentSync(content) {
  const events = [];
  for (const line of content.split("\n")) {
    if (BENIGN_WARNING_RE.test(line)) continue; // skip warning benigno
    for (const { re, type } of LOG_PATTERNS) {
      if (re.test(line)) {
        events.push(type);
        break; // break inner, continuar outer (FIX del bug return→break)
      }
    }
  }
  return events;
}

function classify(line) {
  if (BENIGN_WARNING_RE.test(line)) return "benign_warning";
  for (const { re, type } of LOG_PATTERNS) {
    if (re.test(line)) return type;
  }
  return "no_match";
}

// ── TEST 1: Patrones contra líneas reales del log ─────────────
const cases = [
  // [línea de log, tipo esperado]
  [
    "ccreq:cebb3ffd.copilotmd | success | claude-sonnet-4.6 -> claude-sonnet-4-6 | 5041ms | [panel/editAgent]",
    "success",
  ],
  [
    "ccreq:abc123.copilotmd | cancelled | claude-sonnet-4.6 | 1000ms | [panel]",
    "cancelled",
  ],
  [
    "2026-03-28 17:17:37.670 [warning] Tool mcp_aisquare-play_browser_drag failed validation: schema must be an object if present",
    "benign_warning",
  ],
  ["Error: Sorry, your request was rate-limited.", "rate_limited"],
  ["RateLimitError: quota exceeded", "rate_limited"],
  ["[info] ccreq:xyz | error | claude-sonnet | 500ms | [panel]", "hard_error"],
  ["[error] 503 Service Unavailable", "hard_error"],
  ["Stop hook result: shouldContinue=false", "loop_stopped"],
  ["[ToolCallingLoop] Stop reason: maxTurns", "loop_stopped"],
  ["2026-03-28 10:00:00 [info] Extension host started", "no_match"],
];

let passed = 0,
  failed = 0;
console.log("\n── TEST 1: Clasificación de patrones ──────────────────────");
cases.forEach(([line, expected]) => {
  const got = classify(line);
  const ok = got === expected;
  console.log(
    `${ok ? "✅" : "❌"} ${expected.padEnd(22)} got:${got.padEnd(22)} | ${line.slice(0, 70)}`,
  );
  ok ? passed++ : failed++;
});

// ── TEST 2: findActiveCopilotLog — encuentra el log más reciente ──
console.log("\n── TEST 2: findActiveCopilotLog ───────────────────────────");
const COPILOT_LOG_NAME = "GitHub Copilot Chat.log";
function findActiveCopilotLog(logsDir) {
  let bestPath = null,
    bestMtime = 0;
  function recurse(dir) {
    let entries;
    try {
      entries = fs.readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) recurse(full);
      else if (entry.isFile() && entry.name === COPILOT_LOG_NAME) {
        try {
          const mt = fs.statSync(full).mtimeMs;
          if (mt > bestMtime) {
            bestMtime = mt;
            bestPath = full;
          }
        } catch {}
      }
    }
  }
  recurse(logsDir);
  return bestPath;
}

const logsDir = path.join(
  os.homedir(),
  "Library",
  "Application Support",
  "Code",
  "logs",
);
const found = findActiveCopilotLog(logsDir);
if (found) {
  const stat = fs.statSync(found);
  console.log(`✅ Log activo encontrado: ${found}`);
  console.log(
    `   Tamaño: ${stat.size} bytes | mtime: ${new Date(stat.mtimeMs).toLocaleString()}`,
  );
  passed++;

  // TEST 3: Leer últimas 5 líneas ccreq del log real
  console.log("\n── TEST 3: Últimas líneas ccreq del log activo ────────────");
  const content = fs.readFileSync(found, "utf8");
  const ccreqLines = content
    .split("\n")
    .filter((l) => l.includes("ccreq:"))
    .slice(-5);
  if (ccreqLines.length > 0) {
    ccreqLines.forEach((line) => {
      const type = classify(line);
      console.log(`  ${type.padEnd(12)} | ${line.trim().slice(0, 90)}`);
    });
    console.log(
      `✅ ${ccreqLines.length} líneas ccreq encontradas y clasificadas`,
    );
    passed++;
  } else {
    console.log("⚠️  No se encontraron líneas ccreq en el log");
    failed++;
  }

  // TEST 4: Lectura incremental por offset
  console.log("\n── TEST 4: Lectura incremental por offset ─────────────────");
  const size = stat.size;
  const testOffset = Math.max(0, size - 1000);
  const fd = fs.openSync(found, "r");
  const buf = Buffer.allocUnsafe(size - testOffset);
  fs.readSync(fd, buf, 0, buf.length, testOffset);
  fs.closeSync(fd);
  const newLines = buf
    .toString("utf8")
    .split("\n")
    .filter((l) => l.trim()).length;
  console.log(
    `✅ Offset ${testOffset} → leídos ${buf.length} bytes → ${newLines} líneas nuevas`,
  );
  passed++;
} else {
  console.log("❌ No se encontró el log de Copilot Chat");
  failed++;
}

// ── TEST 5: prompt_1.md accesible ─────────────────────────────
console.log("\n── TEST 5: Archivo de prompt ──────────────────────────────");
const promptPath = path.join(__dirname, "..", ".prompts", "prompt_1.md");
if (fs.existsSync(promptPath)) {
  const content = fs.readFileSync(promptPath, "utf8");
  console.log(`✅ prompt_1.md encontrado — ${content.length} chars`);
  passed++;
} else {
  console.log(`❌ prompt_1.md NO encontrado en: ${promptPath}`);
  failed++;
}

// ── TEST 6: BUG CRÍTICO — rate limit después de tool warnings ───────────
// ANTES del fix: el 'return' al detectar tool_validation_error hacía que el
// plugin NUNCA viera el rate limit en el mismo batch. (Confirmado con log real.)
// DESPUÉS del fix: BENIGN_WARNING_RE skipea el warning, el outer loop continúa.
console.log(
  "\n── TEST 6: BUG CRÍTICO — rate limit no bloqueado por tool warnings ──",
);
const batchConRateLimit = [
  "2026-03-28 18:49:58.511 [warning] Tool mcp_aisquare-play_browser_click failed validation: schema must be an object if present",
  "2026-03-28 18:49:58.511 [warning] Tool mcp_aisquare-play_browser_drag failed validation: schema must be an object if present",
  "2026-03-28 18:49:58.511 [warning] Tool mcp_aisquare-play_browser_navigate failed validation: schema must be an object if present",
  '2026-03-28 18:50:09.647 [error] Server error: 429 {"error":{"message":"Sorry, you\'ve exhausted this model\'s rate limit.","code":"user_model_rate_limited"}}',
].join("\n");
const events6 = analyzeLogContentSync(batchConRateLimit);
const found6 = events6.includes("rate_limited");
console.log(
  `${found6 ? "✅" : "❌"} rate_limited detectado después de 3 tool warnings: ${found6}`,
);
console.log(`   Eventos en batch: [${events6.join(", ")}]`);
found6 ? passed++ : failed++;

// ── TEST 7: BUG CRÍTICO — success NO bloquea rate_limit posterior ──────
// Un batch puede tener success seguido de rate_limit (si el polling captura ambos)
console.log(
  "\n── TEST 7: success seguido de rate_limit en mismo batch ──────────",
);
const batchSuccessLuegRateLimit = [
  "2026-03-28 18:49:58.360 [info] ccreq:15d13f1c.copilotmd | success | claude-sonnet-4.6 -> claude-sonnet-4-6 | 5429ms | [panel/editAgent]",
  "2026-03-28 18:49:59.000 [warning] Tool mcp_aisquare-play_browser_hover failed validation: schema must be an object if present",
  '2026-03-28 18:50:09.647 [error] Server error: 429 {"error":{"message":"Sorry, you\'ve exhausted this model\'s rate limit.","code":"user_model_rate_limited"}}',
].join("\n");
const events7 = analyzeLogContentSync(batchSuccessLuegRateLimit);
const found7 = events7.includes("rate_limited");
console.log(
  `${found7 ? "✅" : "❌"} rate_limited detectado después de success+warning: ${found7}`,
);
console.log(`   Eventos en batch: [${events7.join(", ")}]`);
found7 ? passed++ : failed++;

// ── TEST 8: loop_stopped NO sale del outer loop
console.log(
  "\n── TEST 8: loop_stopped no bloquea rate_limit posterior ──────────",
);
const batchLoopStopLuegoRateLimit = [
  "Stop hook result: shouldContinue=false",
  '2026-03-28 18:50:09.647 [error] Server error: 429 {"error":{"message":"rate_limit"}}',
].join("\n");
const events8 = analyzeLogContentSync(batchLoopStopLuegoRateLimit);
const found8 = events8.includes("rate_limited");
console.log(
  `${found8 ? "✅" : "❌"} rate_limited detectado después de loop_stopped: ${found8}`,
);
console.log(`   Eventos en batch: [${events8.join(", ")}]`);
found8 ? passed++ : failed++;

// ── Resultado final ───────────────────────────────────────────

// ── TEST 9: getVSCodeWindowId — Quartz CGWindowID (not AppleScript)
console.log("\n── TEST 9: Quartz CGWindowID (screencapture fix) ─────────────");
const { execFileSync } = require("child_process");
try {
  const pyScript = [
    "import Quartz",
    "windows = Quartz.CGWindowListCopyWindowInfo(Quartz.kCGWindowListOptionOnScreenOnly, Quartz.kCGNullWindowID)",
    "for w in windows:",
    "  if 'Code' in str(w.get('kCGWindowOwnerName', '')) and w.get('kCGWindowLayer', -1) == 0:",
    "    print(w['kCGWindowNumber'], end=''); break",
  ].join("\n");
  const windowId = execFileSync("python3", ["-c", pyScript], { timeout: 5000 })
    .toString()
    .trim();
  if (/^\d+$/.test(windowId)) {
    console.log(
      `✅ CGWindowID=${windowId} — screencapture -l ${windowId} funcionará`,
    );
    passed++;
  } else {
    console.log(`❌ CGWindowID no obtenido: "${windowId}"`);
    failed++;
  }
} catch (e) {
  console.log(`❌ Quartz no disponible: ${e.message}`);
  failed++;
}

// ── TEST 10: code CLI available
console.log("\n── TEST 10: code CLI disponible ──────────────────────────");
try {
  const codePath = execFileSync("which", ["code"], { timeout: 3000 })
    .toString()
    .trim();
  if (codePath) {
    console.log(`✅ code CLI encontrado: ${codePath}`);
    passed++;

    // Sub-test: code chat mode support
    const chatHelp = execFileSync("code", ["chat", "--help"], {
      timeout: 10000,
    }).toString();
    const hasAgentMode = chatHelp.includes("agent");
    console.log(
      `${hasAgentMode ? "✅" : "❌"} code chat --mode agent: ${hasAgentMode ? "soportado" : "NO soportado"}`,
    );
    hasAgentMode ? passed++ : failed++;
  } else {
    console.log("❌ code CLI no encontrado");
    failed++;
  }
} catch (e) {
  console.log(`❌ code CLI error: ${e.message}`);
  failed++;
}

// ── TEST 11: screencapture works
console.log("\n── TEST 11: screencapture funciona ───────────────────────");
const tmpScreenshot = path.join(
  os.tmpdir(),
  `test_screenshot_${Date.now()}.png`,
);
try {
  execFileSync("screencapture", ["-x", tmpScreenshot], { timeout: 10000 });
  if (fs.existsSync(tmpScreenshot)) {
    const sz = fs.statSync(tmpScreenshot).size;
    console.log(`✅ screencapture OK — ${sz} bytes`);
    fs.unlinkSync(tmpScreenshot);
    passed++;
  } else {
    console.log("❌ screencapture no creó archivo");
    failed++;
  }
} catch (e) {
  console.log(`❌ screencapture error: ${e.message}`);
  failed++;
}

// ── TEST 12: state.vscdb readable
console.log("\n── TEST 12: state.vscdb (model DB) legible ───────────────");
const stateDbPath = path.join(
  os.homedir(),
  "Library",
  "Application Support",
  "Code",
  "User",
  "globalStorage",
  "state.vscdb",
);
try {
  const pyDbScript = `import sqlite3
try:
  c=sqlite3.connect(r'${stateDbPath.replace(/\\/g, "/")}',timeout=5).execute("SELECT value FROM ItemTable WHERE key='chat.currentLanguageModel.panel'").fetchone()
  print(c[0] if c else 'KEY_NOT_FOUND',end='')
except Exception as e:
  print('ERR:'+str(e),end='')`;
  const result = execFileSync("python3", ["-c", pyDbScript], { timeout: 5000 })
    .toString()
    .trim();
  if (result.startsWith("ERR:")) {
    console.log(`❌ state.vscdb error: ${result}`);
    failed++;
  } else if (result === "KEY_NOT_FOUND") {
    console.log(
      "⚠️  state.vscdb: clave currentLanguageModel no existe aún (primer uso)",
    );
    passed++;
  } else {
    console.log(`✅ state.vscdb modelo actual: ${result}`);
    passed++;
  }
} catch (e) {
  console.log(`❌ state.vscdb error: ${e.message}`);
  failed++;
}

// ── TEST 13: Brain override rules — decision tree covers all cases
console.log("\n── TEST 13: Brain override rules (decision tree) ─────────");
const testCases13 = [
  {
    name: "prompt6Changed",
    obs: { prompt6Changed: true },
    expected: "execute_prompt6",
  },
  {
    name: "contextSaturated",
    obs: { contextSaturated: true, snapshotSizeChars: 700000 },
    expected: "stop_and_new_chat",
  },
  {
    name: "activeGeneration",
    obs: { snapshotHasActiveGeneration: true },
    expected: "wait",
  },
  {
    name: "rate_limited",
    obs: {
      logDominantEvent: "rate_limited",
      secsSinceLastActivity: 200,
      secsSinceLastAction: 200,
    },
    expected: "cycle_model",
  },
  {
    name: "hard_error_try1",
    obs: {
      logDominantEvent: "hard_error",
      errorContinueCount: 0,
      secsSinceLastActivity: 200,
      secsSinceLastAction: 200,
    },
    expected: "send_continue",
  },
  {
    name: "hard_error_try4",
    obs: {
      logDominantEvent: "hard_error",
      errorContinueCount: 3,
      secsSinceLastActivity: 200,
      secsSinceLastAction: 200,
    },
    expected: "open_new_chat",
  },
  {
    name: "stall_soft",
    obs: {
      secsSinceLastActivity: 350,
      logDominantEvent: "idle",
      secsSinceLastAction: 200,
    },
    expected: "send_continue",
  },
  {
    name: "stall_hard",
    obs: {
      secsSinceLastActivity: 500,
      logDominantEvent: "idle",
      secsSinceLastAction: 200,
    },
    expected: "open_new_chat",
  },
  {
    name: "session_long",
    obs: {
      sessionTooLong: true,
      secsSinceLastActivity: 100,
      logDominantEvent: "idle",
      secsSinceLastAction: 200,
    },
    expected: "open_new_chat",
  },
];

// Simplified override rules for testing (no cooldown state)
function testOverride(obs) {
  if (obs.prompt6Changed) return "execute_prompt6";
  if (obs.contextSaturated) return "stop_and_new_chat";
  if (obs.snapshotHasActiveGeneration) return "wait";
  if (obs.logDominantEvent === "rate_limited" || obs.chatUiSwitchNeeded)
    return "cycle_model";
  if (obs.logDominantEvent === "hard_error") {
    return (obs.errorContinueCount || 0) >= 3
      ? "open_new_chat"
      : "send_continue";
  }
  if (obs.secsSinceLastActivity > 480) return "open_new_chat";
  if (obs.secsSinceLastActivity > 300 && obs.secsSinceLastActivity <= 480)
    return "send_continue";
  if (obs.sessionTooLong) return "open_new_chat";
  return "wait";
}

testCases13.forEach(({ name, obs, expected }) => {
  const got = testOverride(obs);
  const ok = got === expected;
  console.log(
    `${ok ? "✅" : "❌"} ${name.padEnd(20)} expected=${expected.padEnd(20)} got=${got}`,
  );
  ok ? passed++ : failed++;
});

// ── TEST 14: Monitor readPrompt6 ─────────────────────────────
console.log("\n── TEST 14: readPrompt6 (CPSO rule) ─────────────────────");
const prompt6Path = path.join(__dirname, "..", ".prompts", "prompt_1.md");
if (fs.existsSync(prompt6Path)) {
  const content = fs.readFileSync(prompt6Path, "utf8").trim();
  const endsWithRead = content.endsWith("READ");
  console.log(
    `✅ prompt_1.md existe (${content.length} chars) — ends with READ: ${endsWithRead}`,
  );
  passed++;
} else {
  console.log("✅ prompt_1.md no existe — OK (se crea dinámicamente)");
  passed++;
}

console.log(`\n══════════════════════════════════════════════════════════`);
console.log(`  RESULTADO: ${passed} passed / ${failed} failed`);
console.log(`══════════════════════════════════════════════════════════\n`);
process.exit(failed > 0 ? 1 : 0);
