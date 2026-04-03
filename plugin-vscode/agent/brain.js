// ================================================================
//  agent/brain.js — Motor de Decisión
//
//  Toma un objeto Observation y retorna una Decision.
//
//  Estrategia en orden:
//  1. Override rules (determinísticas, no requieren LLM)
//     → prompt_6 nuevo, contexto lleno, generación activa, etc.
//  2. GitHub Copilot vía vscode.lm API (sesión ya logueada en VS Code)
//     → Toma screenshots del chat → los procesa con visión → decide
//     → Si no está disponible → Gemma 3 vía Ollama como fallback
//  3. Gemma 3 via Ollama (http://localhost:11434) — fallback local
//
//  Equivalente al brain.py / brain_gpt.py del Python agent.
// ================================================================

"use strict";

const http = require("http");
const fs = require("fs");

// ─── Log injection ──────────────────────────────────────────────────────────
let _log = (msg) => console.log(`[brain] ${msg}`);
function setLogger(fn) {
  _log = fn;
}

// ─── Ollama config ───────────────────────────────────────────────────────────
const OLLAMA_HOST = "localhost";
const OLLAMA_PORT = 11434;
const OLLAMA_MODEL = "gemma3:4b";
const OLLAMA_TIMEOUT = 90000; // 90s — gemma3 cold start puede ser lento

// ─── System Prompt ────────────────────────────────────────────────────────────
//
//  Mismo sistema de razonamiento que el Brain Python, adaptado a JS context.
//
const SYSTEM_PROMPT = `\
Eres el cerebro de un agente watchdog que monitorea GitHub Copilot en VS Code.
Tu misión es mantener SIEMPRE un modelo de Copilot generando código.
Analiza la observación del entorno y decide la acción óptima.

━━━ CATÁLOGO DE MODELOS (IDs reales en modelCycler.models) ━━━

Tier-0 (0.33x — prioridad máxima, usar primero):
  - copilot/claude-haiku-4.5      → Haiku 4.5 ★ PREFERIDO
  - copilot/gemini-3-flash-preview → Gemini 3 Flash
  - copilot/gpt-5.1-codex-mini   → GPT-5.1 Codex Mini
  - copilot/gpt-5.4-mini          → GPT-5.4 mini

Tier-1 (0x — fallback cuando los 0.33x están con rate limit):
  - copilot/gpt-4.1
  - copilot/gpt-5-mini
  - copilot/oswe-vscode-prime     → Raptor mini

cycle_model cicla al SIGUIENTE índice en la lista — el orden de la
lista en settings determina cuál modelo se usa tras cada rate limit.

━━━ ÁRBOL DE DECISIÓN (SEGUIR EN ORDEN) ━━━

1. prompt6Changed=true → execute_prompt6 INMEDIATO
2. contextSaturated=true → stop_and_new_chat INMEDIATO
3. snapshotHasActiveGeneration=true → wait SIEMPRE
4. secsSinceLastActivity < 120 && logDominantEvent no es error → wait
5. chatUiSwitchNeeded=true o logDominantEvent="rate_limited" → cycle_model
6. logDominantEvent="hard_error" → send_continue (hasta 3) → open_new_chat
7. secsSinceLastActivity entre 300-480 → send_continue
8. secsSinceLastActivity > 480 → open_new_chat
9. sessionTooLong=true → open_new_chat
10. Sin situación clara → wait

DECISIONES VÁLIDAS (responde EXACTAMENTE una):
- wait
- send_continue
- open_new_chat
- stop_and_new_chat
- cycle_model
- focus_vscode
- execute_prompt6

FORMATO JSON estricto:
{
  "decision": "<decisión>",
  "confidence": <0.0-1.0>,
  "reasoning": "<breve explicación>",
  "wait_before_action_secs": <0-120>
}`;

// ─── Cooldowns mínimos por tipo de acción ────────────────────────────────────
const ACTION_COOLDOWNS = {
  send_continue: 60,
  open_new_chat: 90,
  stop_and_new_chat: 90,
  cycle_model: 120,
  focus_vscode: 30,
  execute_prompt6: 5,
};

// ─── Decision ────────────────────────────────────────────────────────────────
function createDecision(overrides = {}) {
  return {
    action: "wait",
    confidence: 1.0,
    reasoning: "",
    waitBeforeActionSecs: 0,
    source: "override", // "override" | "gemma3" | "fallback"
    latencyMs: 0,
    ...overrides,
  };
}

// ─── Override rules (determinísticas) ────────────────────────────────────────
//
//  Reglas de alta confianza que NO necesitan LLM.
//  Si ninguna aplica → retorna null (→ invocar Gemma3).
//
function applyOverrideRules(obs, agentState) {
  const cooldown = (type) => {
    const last = agentState[`last_${type}_ts`] || 0;
    const minSecs = ACTION_COOLDOWNS[type] || 60;
    return (Date.now() - last) / 1000 < minSecs;
  };

  // R1: prompt_1.md nuevo → procesarlo inmediatamente
  if (obs.prompt6Changed) {
    return createDecision({
      action: "execute_prompt6",
      confidence: 1.0,
      reasoning: "prompt_1.md tiene instrucciones nuevas del CPSO",
      source: "override",
    });
  }

  // R2: Contexto lleno → stop and new chat
  if (obs.contextSaturated && !cooldown("stop_and_new_chat")) {
    return createDecision({
      action: "stop_and_new_chat",
      confidence: 1.0,
      reasoning: `Contexto saturado (tamaño: ${obs.snapshotSizeChars})`,
      source: "override",
    });
  }

  // R3: Generación activa → wait siempre (no interrumpir)
  if (obs.snapshotHasActiveGeneration) {
    return createDecision({
      action: "wait",
      confidence: 0.95,
      reasoning: "Generación activa en el chat — no interrumpir",
      source: "override",
    });
  }

  // R4: Post-action cooldown → wait
  const lastActionSecs = obs.secsSinceLastAction;
  if (
    lastActionSecs < 120 &&
    agentState.lastActionType &&
    agentState.lastActionType !== "wait"
  ) {
    return createDecision({
      action: "wait",
      confidence: 0.9,
      reasoning: `Post-action cooldown: ${Math.round(lastActionSecs)}s desde ${agentState.lastActionType}`,
      source: "override",
    });
  }

  // R5: Rate limit confirmado → cycle model
  if (obs.logDominantEvent === "rate_limited" || obs.chatUiSwitchNeeded) {
    if (!cooldown("cycle_model")) {
      return createDecision({
        action: "cycle_model",
        confidence: 0.95,
        reasoning: `Rate limit detectado en log (${obs.logDominantEvent})`,
        source: "override",
      });
    }
  }

  // R6: Hard error → send continue (hasta 3) o new chat
  if (obs.logDominantEvent === "hard_error") {
    if (obs.errorContinueCount >= 3) {
      if (!cooldown("open_new_chat")) {
        return createDecision({
          action: "open_new_chat",
          confidence: 0.9,
          reasoning: `Hard error persistente (${obs.errorContinueCount} continuaciones)`,
          source: "override",
        });
      }
    } else if (!cooldown("send_continue")) {
      return createDecision({
        action: "send_continue",
        confidence: 0.85,
        reasoning: `Hard error (intento ${obs.errorContinueCount + 1}/3)`,
        source: "override",
      });
    }
  }

  // R7: Stall suave (5-8 min)
  //
  //  GUARD: Only fire if Copilot was actually used in this session
  //  (hasEverSeenCopilotActivity) AND the last visible event was NOT a normal
  //  completion. Without this guard, an idle VS Code session (user just has the
  //  editor open) triggers "continuar" every 60 s — sending paid prompts for no
  //  reason.
  if (
    obs.secsSinceLastActivity > 300 &&
    obs.secsSinceLastActivity <= 480 &&
    obs.hasEverSeenCopilotActivity === true &&
    obs.lastCompletedEvent !== "success" &&
    obs.lastCompletedEvent !== "cancelled"
  ) {
    if (!cooldown("send_continue")) {
      return createDecision({
        action: "send_continue",
        confidence: 0.8,
        reasoning: `Stall suave: ${Math.round(obs.secsSinceLastActivity)}s sin actividad`,
        source: "override",
      });
    }
  }

  // R8: Stall duro (>8 min)
  //
  //  Same guard as R7: only fire when there was prior Copilot activity and it
  //  didn't complete cleanly. Opening a new chat on an idle session sends the
  //  full prompt_1.md which also costs money.
  if (
    obs.secsSinceLastActivity > 480 &&
    obs.hasEverSeenCopilotActivity === true &&
    obs.lastCompletedEvent !== "success" &&
    obs.lastCompletedEvent !== "cancelled"
  ) {
    if (!cooldown("open_new_chat")) {
      return createDecision({
        action: "open_new_chat",
        confidence: 0.85,
        reasoning: `Stall duro: ${Math.round(obs.secsSinceLastActivity)}s sin actividad`,
        source: "override",
      });
    }
  }

  // R9: Sesión larga
  if (obs.sessionTooLong && !cooldown("open_new_chat")) {
    return createDecision({
      action: "open_new_chat",
      confidence: 0.8,
      reasoning: `Sesión larga: ${Math.round(obs.sessionAgeMins)} min, ${obs.continueCount} continuaciones`,
      source: "override",
    });
  }

  // Sin override aplica → delegar a Gemma3
  return null;
}

// ─── Llamar a Ollama/Gemma3 ───────────────────────────────────────────────────
//
//  Hace un POST a http://localhost:11434/api/generate con los datos
//  de la observación. Retorna el JSON parseado de la respuesta.
//
function callOllama(observationJson) {
  return new Promise((resolve, reject) => {
    const prompt = `${SYSTEM_PROMPT}\n\n---\n\nOBSERVACIÓN ACTUAL:\n${JSON.stringify(observationJson, null, 2)}\n\n---\n\nDECISIÓN (JSON):`;

    const body = JSON.stringify({
      model: OLLAMA_MODEL,
      prompt,
      stream: false,
      options: { temperature: 0.1, num_predict: 200 },
    });

    const req = http.request(
      {
        host: OLLAMA_HOST,
        port: OLLAMA_PORT,
        path: "/api/generate",
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Content-Length": Buffer.byteLength(body),
        },
      },
      (res) => {
        let data = "";
        res.on("data", (chunk) => {
          data += chunk;
        });
        res.on("end", () => {
          try {
            const parsed = JSON.parse(data);
            resolve(parsed.response || "");
          } catch (e) {
            reject(new Error(`Ollama parse error: ${e.message}`));
          }
        });
      },
    );
    req.on("error", reject);
    req.setTimeout(OLLAMA_TIMEOUT, () => {
      req.destroy(new Error("Ollama timeout"));
    });
    req.write(body);
    req.end();
  });
}

// ─── Parsear respuesta de Gemma3 ──────────────────────────────────────────────
function parseGemmaResponse(raw) {
  if (!raw) return null;
  // Buscar JSON en la respuesta (puede haber texto antes/después)
  const match = raw.match(/\{[\s\S]*"decision"[\s\S]*\}/);
  if (!match) return null;
  try {
    return JSON.parse(match[0]);
  } catch {
    return null;
  }
}

const VALID_ACTIONS = new Set([
  "wait",
  "send_continue",
  "open_new_chat",
  "stop_and_new_chat",
  "cycle_model",
  "focus_vscode",
  "execute_prompt6",
]);

// ─── Motor de Decisión Principal ──────────────────────────────────────────────
class Brain {
  constructor() {
    this._ollamaAvailable = null; // null=desconocido, true/false
    this._vscode = null; // inyectado desde extension.js
    this._actions = null; // inyectado para poder tomar screenshots
  }

  /**
   * Inyectar la referencia a vscode.*  (llamar desde extension.js activate)
   * Necesario para acceder a vscode.lm sin require('vscode') directo.
   */
  setVSCode(vscode) {
    this._vscode = vscode;
    _log(
      "Brain: vscode.lm inyectado — usará sesión de Copilot para decisiones",
    );
  }

  /**
   * Inyectar referencia al módulo actions para acceder a takeChatScreenshots.
   */
  setActions(actions) {
    this._actions = actions;
  }

  /**
   * Llama al modelo Copilot vía vscode.lm (sesión ya logueada en VS Code).
   * Envía la observación en JSON + screenshots opcionales como imágenes (visión).
   *
   * Prioriza modelos con capacidad de visión (Claude, GPT-4o, Gemini).
   * Si la API de imágenes no está disponible en esta versión de VS Code,
   * envía solo texto.
   *
   * @param {object} obsForLLM   Subset serializable de Observation
   * @param {string[]} screenshots  Rutas PNG a adjuntar (max 3)
   * @returns {{ text: string, latencyMs: number, modelId: string, imagesAttached: number }}
   */
  async callCopilotLM(obsForLLM, screenshots = []) {
    const vsc = this._vscode;
    if (!vsc || !vsc.lm) throw new Error("vscode.lm no disponible");

    // Seleccionar modelo — prioriza soporte de imagen cuando hay screenshots.
    let models;
    try {
      models = await vsc.lm.selectChatModels({ vendor: "copilot" });
    } catch (e) {
      throw new Error(`selectChatModels error: ${e.message}`);
    }
    if (!models || !models.length)
      throw new Error("No hay modelos Copilot disponibles");

    const validShots = (screenshots || [])
      .filter((p) => p && fs.existsSync(p))
      .slice(0, 3);

    const candidateModels = validShots.length
      ? models.filter((m) => m.capabilities?.imageInput)
      : models;

    const rankedModels = candidateModels.length ? candidateModels : models;

    // Preferir modelos con visión (Claude > GPT-4x > Gemini > cualquier otro)
    const model =
      rankedModels.find(
        (m) =>
          m.family?.toLowerCase().includes("claude") ||
          m.id?.toLowerCase().includes("claude"),
      ) ||
      rankedModels.find(
        (m) =>
          m.family?.toLowerCase().includes("gpt-4") ||
          m.id?.toLowerCase().includes("gpt-4"),
      ) ||
      rankedModels.find(
        (m) =>
          m.family?.toLowerCase().includes("gemini") ||
          m.id?.toLowerCase().includes("gemini"),
      ) ||
      rankedModels[0];

    // Construir mensaje de texto con el contexto de la observación
    const textContent = [
      SYSTEM_PROMPT,
      "",
      "---",
      "",
      "OBSERVACIÓN ACTUAL (con screenshots adjuntos si la visión está disponible):",
      JSON.stringify(obsForLLM, null, 2),
      "",
      "---",
      "",
      "DECISIÓN (JSON):",
    ].join("\n");

    // Construir partes del mensaje — imagen si la API lo soporta
    const textPart = new vsc.LanguageModelTextPart(textContent);
    const parts = [textPart];

    let imagesAttached = 0;

    for (const screenshotPath of validShots) {
      try {
        const data = fs.readFileSync(screenshotPath);
        // VS Code 1.98+ → LanguageModelDataPart(buffer, mime)
        if (typeof vsc.LanguageModelDataPart === "function") {
          parts.push(new vsc.LanguageModelDataPart(data, "image/png"));
          imagesAttached++;
          _log(
            `callCopilotLM: imagen adjuntada ${screenshotPath.split("/").pop()} (${data.length} bytes)`,
          );
        } else if (typeof vsc.LanguageModelImagePart === "function") {
          parts.push(
            new vsc.LanguageModelImagePart(vsc.Uri.file(screenshotPath)),
          );
          imagesAttached++;
          _log(
            `callCopilotLM: imagen adjuntada vía ImagePart ${screenshotPath.split("/").pop()}`,
          );
        } else {
          _log(
            `callCopilotLM: API de imagen no disponible — LanguageModelDataPart=${typeof vsc.LanguageModelDataPart}, LanguageModelImagePart=${typeof vsc.LanguageModelImagePart}`,
          );
        }
      } catch (e) {
        _log(`callCopilotLM: imagen no adjuntada (${e.message})`);
      }
    }

    if (imagesAttached > 0) {
      _log(
        `callCopilotLM: ${imagesAttached}/${validShots.length} screenshot(s) → modelo ${model.id || model.family}`,
      );
    } else if (validShots.length > 0) {
      _log(
        `callCopilotLM: 0/${validShots.length} imágenes adjuntadas — decidiendo solo con texto (${model.id})`,
      );
    }

    // Preferir static factory .User() (1.95+) sobre constructor con role enum
    let messages;
    if (typeof vsc.LanguageModelChatMessage.User === "function") {
      messages = [vsc.LanguageModelChatMessage.User(parts)];
    } else {
      const userRole =
        vsc.LanguageModelChatMessageRole?.User ??
        vsc.LanguageModelChatMessageRole?.user ??
        1;
      messages = [new vsc.LanguageModelChatMessage(userRole, parts)];
    }

    const t0 = Date.now();
    const response = await model.sendRequest(messages, {
      justification:
        "OKLA AgentLoop: analizar estado del chat y decidir acción",
    });

    let text = "";
    for await (const part of response.stream) {
      if (part instanceof vsc.LanguageModelTextPart) {
        text += part.value;
      }
    }

    return {
      text,
      latencyMs: Date.now() - t0,
      modelId: model.id || model.family || "copilot",
      imagesAttached,
    };
  }

  // Verificar si Ollama está disponible (check rápido)
  async checkOllama() {
    return new Promise((resolve) => {
      const req = http.request(
        {
          host: OLLAMA_HOST,
          port: OLLAMA_PORT,
          path: "/api/tags",
          method: "GET",
        },
        (res) => {
          resolve(res.statusCode === 200);
        },
      );
      req.on("error", () => resolve(false));
      req.setTimeout(3000, () => {
        req.destroy();
        resolve(false);
      });
      req.end();
    });
  }

  // Tomar una decisión
  async decide(obs, agentState) {
    const t0 = Date.now();

    // 1. Override rules (determinísticas, sin LLM)
    const override = applyOverrideRules(obs, agentState);
    if (override) {
      override.latencyMs = Date.now() - t0;
      _log(`Decision [override]: ${override.action} — ${override.reasoning}`);
      return override;
    }

    // 2. Si actividad reciente y sin errores → wait sin consultar LLM
    if (obs.secsSinceLastActivity < 120 && obs.logDominantEvent === "idle") {
      return createDecision({
        action: "wait",
        confidence: 0.9,
        reasoning: `Actividad reciente (${Math.round(obs.secsSinceLastActivity)}s), sin errores`,
        source: "fallback",
        latencyMs: Date.now() - t0,
      });
    }

    // Subset de la observación a enviar al LLM (sin campos voluminosos)
    const obsForLLM = {
      summary: obs.summary,
      logDominantEvent: obs.logDominantEvent,
      snapshotHasActiveGeneration: obs.snapshotHasActiveGeneration,
      snapshotErrors: obs.snapshotErrors,
      contextSaturated: obs.contextSaturated,
      secsSinceLastActivity: Math.round(obs.secsSinceLastActivity),
      secsSinceLastAction: Math.round(obs.secsSinceLastAction),
      errorContinueCount: obs.errorContinueCount,
      rateLimitCount: obs.rateLimitCount,
      sessionAgeMins: Math.round(obs.sessionAgeMins),
      sessionTooLong: obs.sessionTooLong,
      prompt6Changed: obs.prompt6Changed,
    };

    // 3. Tomar screenshots del chat antes de consultar el LLM
    //    Las fotos permiten que el modelo VEA el estado visual del chat,
    //    complementando las señales textuales del log y el snapshot.
    let screenshots = [];
    if (this._actions && obs.logDominantEvent !== "idle") {
      try {
        screenshots = await this._actions.takeChatScreenshots(
          "brain_decide",
          2,
        );
        if (screenshots.length) {
          _log(
            `Brain: ${screenshots.length} screenshot(s) capturado(s) para análisis visual`,
          );
        }
      } catch (e) {
        _log(
          `Brain: screenshots fallaron (${e.message}) — continuando sin visión`,
        );
      }
    }

    // 4. Copilot LM vía vscode.lm (sesión ya logueada — prioridad máxima)
    if (this._vscode) {
      try {
        const { text, latencyMs, modelId, imagesAttached } =
          await this.callCopilotLM(obsForLLM, screenshots);
        const parsed = parseGemmaResponse(text);
        if (parsed && VALID_ACTIONS.has(parsed.decision)) {
          _log(
            `Decision [copilot_lm:${modelId}+${imagesAttached}img ${latencyMs}ms]: ` +
              `${parsed.decision} (conf=${parsed.confidence ?? "?"}) — ${parsed.reasoning}`,
          );
          return createDecision({
            action: parsed.decision,
            confidence: parsed.confidence ?? 0.85,
            reasoning: parsed.reasoning ?? "",
            waitBeforeActionSecs: parsed.wait_before_action_secs ?? 0,
            source: "copilot_lm",
            latencyMs,
          });
        }
        _log(
          `Copilot LM respuesta no parseada: "${text?.slice(0, 120)}" — fallback a Ollama`,
        );
      } catch (e) {
        _log(`Copilot LM error (${e.message}) — fallback a Ollama`);
      }
    }

    // 5. Fallback: Gemma3 vía Ollama (modelo local sin necesidad de login)
    const ollamaOk = await this.checkOllama();
    if (!ollamaOk) {
      _log("Ollama no disponible — Copilot LM también falló → wait");
      return createDecision({
        action: "wait",
        confidence: 0.5,
        reasoning:
          "Copilot LM y Gemma3/Ollama no disponibles — sin cerebro no se actúa",
        source: "fallback",
        latencyMs: Date.now() - t0,
      });
    }

    // Llamar a Gemma3 via Ollama
    try {
      const raw = await callOllama(obsForLLM);
      const parsed = parseGemmaResponse(raw);

      if (parsed && VALID_ACTIONS.has(parsed.decision)) {
        const decision = createDecision({
          action: parsed.decision,
          confidence:
            typeof parsed.confidence === "number" ? parsed.confidence : 0.7,
          reasoning: parsed.reasoning || "",
          waitBeforeActionSecs: parsed.wait_before_action_secs || 0,
          source: "gemma3",
          latencyMs: Date.now() - t0,
        });
        _log(
          `Decision [gemma3] (${decision.latencyMs}ms): ${decision.action} — ${decision.reasoning}`,
        );
        return decision;
      }

      _log(`Gemma3 retornó respuesta inválida: ${raw?.slice(0, 100)}`);
    } catch (e) {
      _log(`Gemma3 error: ${e.message}`);
    }

    // Fallback final: wait
    return createDecision({
      action: "wait",
      confidence: 0.5,
      reasoning: "Gemma3 no retornó decisión válida — wait seguro",
      source: "fallback",
      latencyMs: Date.now() - t0,
    });
  }
}

module.exports = {
  Brain,
  createDecision,
  VALID_ACTIONS,
  ACTION_COOLDOWNS,
  setLogger,
};
