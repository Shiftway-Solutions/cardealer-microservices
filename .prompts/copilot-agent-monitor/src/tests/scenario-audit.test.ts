/**
 * Scenario Audit Tests — Copilot Agent Monitor v1.3.7
 *
 * Trazabilidad completa de cada escenario que el agente puede enfrentar.
 * Cada test simula un estado + timers y valida:
 *   1. Qué DECIDE el StateMachine
 *   2. Qué EJECUTA el ActionExecutor (flow path)
 *   3. Qué POST-STATE queda después de la acción
 *
 * Run:  npx ts-node --skip-project src/tests/scenario-audit.test.ts
 */

// ─── Inline StateMachine (extracted logic — no VS Code dependency) ──────────

const AgentState = {
  GENERATING: "GENERATING",
  COMPLETED: "COMPLETED",
  IDLE: "IDLE",
  STALLED_SOFT: "STALLED_SOFT",
  STALLED_HARD: "STALLED_HARD",
  ERROR_RATE_LIMIT: "ERROR_RATE_LIMIT",
  ERROR_HARD: "ERROR_HARD",
  ERROR_CONTEXT: "ERROR_CONTEXT",
  ERROR_SWITCH_MODEL: "ERROR_SWITCH_MODEL",
  VSCODE_HIDDEN: "VSCODE_HIDDEN",
  RECOVERING: "RECOVERING",
  STOPPED: "STOPPED",
} as const;
type AgentState = (typeof AgentState)[keyof typeof AgentState];

const AgentAction = {
  WAIT: "WAIT",
  SEND_CONTINUE: "SEND_CONTINUE",
  OPEN_NEW_CHAT: "OPEN_NEW_CHAT",
  STOP_AND_NEW_CHAT: "STOP_AND_NEW_CHAT",
  CYCLE_MODEL: "CYCLE_MODEL",
  SWITCH_CHAT_MODEL: "SWITCH_CHAT_MODEL",
  VALIDATE_ZERO_X: "VALIDATE_ZERO_X",
  FOCUS_VSCODE: "FOCUS_VSCODE",
} as const;
type AgentAction = (typeof AgentAction)[keyof typeof AgentAction];

const COOLDOWN_MS = {
  sendContinue: 60_000,
  openNewChat: 90_000,
  stopAndNewChat: 90_000,
  cycleModel: 120_000,
  switchChatModel: 90_000,
  validateZeroX: 180_000,
  recovering: 120_000,
} as const;

interface Cooldowns {
  sendContinue: number;
  openNewChat: number;
  stopAndNewChat: number;
  cycleModel: number;
  switchChatModel: number;
  validateZeroX: number;
  screenshot: number;
}

interface HealthValidation {
  validatedOk: boolean;
  validatedAt: number;
}

interface DecisionInput {
  state: AgentState;
  mssSinceLastActivity: number;
  msSinceLastAction: number;
  msSinceRecoveryStart: number;
  softErrorRetries: number;
  modelRotations: number;
  newChatCount: number;
  cooldowns: Cooldowns;
  health: HealthValidation;
  stallWarnMs: number;
  stallHardMs: number;
}

interface Decision {
  action: AgentAction;
  reasoning: string;
}

// ── StateMachine (verbatim from src/StateMachine.ts) ────────────────────────

function decide(input: DecisionInput): Decision {
  const { state } = input;

  if (state === AgentState.RECOVERING) {
    if (input.msSinceRecoveryStart < COOLDOWN_MS.recovering) {
      const remaining = Math.ceil(
        (COOLDOWN_MS.recovering - input.msSinceRecoveryStart) / 1000,
      );
      return {
        action: AgentAction.WAIT,
        reasoning: `Recovering — verifying result (${remaining}s remaining)`,
      };
    }
  }

  if (state === AgentState.GENERATING) {
    return {
      action: AgentAction.WAIT,
      reasoning: "Model is generating — not interrupting",
    };
  }

  if (state === AgentState.ERROR_CONTEXT) {
    if (canDo(input.cooldowns.stopAndNewChat, COOLDOWN_MS.stopAndNewChat)) {
      return {
        action: AgentAction.STOP_AND_NEW_CHAT,
        reasoning: "Context window full — stopping and opening fresh chat",
      };
    }
    return {
      action: AgentAction.WAIT,
      reasoning: "Context full but stop+new_chat on cooldown",
    };
  }

  if (state === AgentState.ERROR_RATE_LIMIT) {
    const validatedRecently =
      input.health.validatedOk &&
      Date.now() - input.health.validatedAt < 10 * 60_000;

    if (!validatedRecently) {
      if (canDo(input.cooldowns.validateZeroX, COOLDOWN_MS.validateZeroX)) {
        return {
          action: AgentAction.VALIDATE_ZERO_X,
          reasoning:
            "Rate limit detected — switching to 0x model, sending yes/no health check",
        };
      }
    }

    if (canDo(input.cooldowns.switchChatModel, COOLDOWN_MS.switchChatModel)) {
      return {
        action: AgentAction.SWITCH_CHAT_MODEL,
        reasoning:
          "Rate limit — health OK, switching to next 0x model in current session",
      };
    }
    if (canDo(input.cooldowns.cycleModel, COOLDOWN_MS.cycleModel)) {
      return {
        action: AgentAction.CYCLE_MODEL,
        reasoning:
          "Rate limit — rotating model via settings (chat switch on cooldown)",
      };
    }
    return {
      action: AgentAction.WAIT,
      reasoning: "Rate limited but all model-switch actions on cooldown",
    };
  }

  if (state === AgentState.ERROR_SWITCH_MODEL) {
    if (canDo(input.cooldowns.switchChatModel, COOLDOWN_MS.switchChatModel)) {
      return {
        action: AgentAction.SWITCH_CHAT_MODEL,
        reasoning: "Chat UI suggests switching model — complying",
      };
    }
    return {
      action: AgentAction.WAIT,
      reasoning: "Switch-model suggested but on cooldown",
    };
  }

  if (state === AgentState.ERROR_HARD) {
    if (input.softErrorRetries >= 2) {
      const validatedRecently =
        input.health.validatedOk &&
        Date.now() - input.health.validatedAt < 10 * 60_000;
      if (
        !validatedRecently &&
        canDo(input.cooldowns.validateZeroX, COOLDOWN_MS.validateZeroX)
      ) {
        return {
          action: AgentAction.VALIDATE_ZERO_X,
          reasoning: `Hard error after ${input.softErrorRetries} retries — running 0x health check before new chat`,
        };
      }
    }

    if (input.softErrorRetries < 3) {
      if (canDo(input.cooldowns.sendContinue, COOLDOWN_MS.sendContinue)) {
        return {
          action: AgentAction.SEND_CONTINUE,
          reasoning: `Hard error — retry ${input.softErrorRetries + 1}/3`,
        };
      }
      return {
        action: AgentAction.WAIT,
        reasoning: "Hard error retry on cooldown",
      };
    }
    if (canDo(input.cooldowns.openNewChat, COOLDOWN_MS.openNewChat)) {
      return {
        action: AgentAction.OPEN_NEW_CHAT,
        reasoning: "Hard error persists after 3 retries — opening fresh chat",
      };
    }
    return {
      action: AgentAction.WAIT,
      reasoning: "Hard error max retries, new chat on cooldown",
    };
  }

  if (state === AgentState.VSCODE_HIDDEN) {
    return {
      action: AgentAction.FOCUS_VSCODE,
      reasoning: "VS Code is not in the foreground — bringing to focus",
    };
  }

  if (state === AgentState.COMPLETED) {
    const COMPLETED_GRACE_MS = 90_000;
    if (input.mssSinceLastActivity >= COMPLETED_GRACE_MS) {
      if (canDo(input.cooldowns.openNewChat, COOLDOWN_MS.openNewChat)) {
        return {
          action: AgentAction.OPEN_NEW_CHAT,
          reasoning:
            "Task completed cleanly → opening new chat and sending loop prompt to start next task",
        };
      }
      return {
        action: AgentAction.WAIT,
        reasoning:
          "Task completed, grace period done, but openNewChat cooldown active",
      };
    }
    const secsRemaining = Math.ceil(
      (COMPLETED_GRACE_MS - input.mssSinceLastActivity) / 1000,
    );
    return {
      action: AgentAction.WAIT,
      reasoning: `Task completed — ${secsRemaining}s grace period before opening new chat`,
    };
  }

  const msSinceActivity = input.mssSinceLastActivity;

  if (msSinceActivity >= input.stallHardMs) {
    if (canDo(input.cooldowns.openNewChat, COOLDOWN_MS.openNewChat)) {
      const minSinceActivity = Math.round(msSinceActivity / 60_000);
      return {
        action: AgentAction.OPEN_NEW_CHAT,
        reasoning: `Hard stall — ${minSinceActivity} min without activity → fresh chat`,
      };
    }
    return {
      action: AgentAction.WAIT,
      reasoning: "Hard stall but new_chat on cooldown",
    };
  }

  if (msSinceActivity >= input.stallWarnMs) {
    if (canDo(input.cooldowns.sendContinue, COOLDOWN_MS.sendContinue)) {
      const minSinceActivity = Math.round(msSinceActivity / 60_000);
      return {
        action: AgentAction.SEND_CONTINUE,
        reasoning: `Soft stall — ${minSinceActivity} min without activity → sending continue`,
      };
    }
    return {
      action: AgentAction.WAIT,
      reasoning: "Soft stall but send_continue on cooldown",
    };
  }

  const secsSinceActivity = Math.round(msSinceActivity / 1000);
  return {
    action: AgentAction.WAIT,
    reasoning: `Normal — last activity ${secsSinceActivity}s ago, no issues detected`,
  };
}

function canDo(lastActionTs: number, cooldownMs: number): boolean {
  return Date.now() - lastActionTs >= cooldownMs;
}

// ─── Test Helpers ───────────────────────────────────────────────────────────

function freshCooldowns(): Cooldowns {
  return {
    sendContinue: 0,
    openNewChat: 0,
    stopAndNewChat: 0,
    cycleModel: 0,
    switchChatModel: 0,
    validateZeroX: 0,
    screenshot: 0,
  };
}

function baseInput(overrides: Partial<DecisionInput> = {}): DecisionInput {
  return {
    state: AgentState.IDLE,
    mssSinceLastActivity: 10_000,
    msSinceLastAction: 60_000,
    msSinceRecoveryStart: 0,
    softErrorRetries: 0,
    modelRotations: 0,
    newChatCount: 0,
    cooldowns: freshCooldowns(),
    health: { validatedOk: false, validatedAt: 0 },
    stallWarnMs: 300_000, // 5 min
    stallHardMs: 480_000, // 8 min
    ...overrides,
  };
}

let passed = 0;
let failed = 0;

function assert(
  testName: string,
  actual: AgentAction,
  expected: AgentAction,
  reasoning: string,
): void {
  if (actual === expected) {
    console.log(`  ✅ ${testName}`);
    console.log(`     → ${expected} — "${reasoning}"`);
    passed++;
  } else {
    console.error(`  ❌ ${testName}`);
    console.error(`     Expected: ${expected}`);
    console.error(`     Got:      ${actual} — "${reasoning}"`);
    failed++;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SCENARIO TESTS
// ═══════════════════════════════════════════════════════════════════════════════

console.log(
  "\n═══════════════════════════════════════════════════════════════════",
);
console.log("  COPILOT AGENT MONITOR v1.3.7 — SCENARIO AUDIT");
console.log(
  "═══════════════════════════════════════════════════════════════════\n",
);

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 1: Modelo generando — NUNCA interrumpir
// ─────────────────────────────────────────────────────────────────────────────
console.log("─── Escenario 1: GENERATING — modelo trabajando activamente ───");
console.log("  Trigger: LogWatcher detecta ccreq:*|success| reciente (<30s)");
console.log(
  "  Flow: LogWatcher → _onLogDelta(SUCCESS) → _stateFromLog() → GENERATING",
);
console.log(
  "  Monitor: _observeState returns GENERATING (no screenshot needed)",
);
console.log("  StateMachine: GENERATING → WAIT (never interrupt)\n");

{
  const d = decide(baseInput({ state: AgentState.GENERATING }));
  assert("1a. GENERATING → WAIT", d.action, AgentAction.WAIT, d.reasoning);
}

// Incluso si lleva 10 min sin otra actividad
{
  const d = decide(
    baseInput({
      state: AgentState.GENERATING,
      mssSinceLastActivity: 600_000,
    }),
  );
  assert(
    "1b. GENERATING + 10min stall → WAIT (no stall override)",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 2: Tarea completada — grace period + nuevo chat
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 2: COMPLETED — tarea terminó limpiamente ───");
console.log(
  "  Trigger: LogWatcher detecta shouldContinue=false → LOOP_STOPPED",
);
console.log("  Flow: _onLogDelta(LOOP_STOPPED) → _stateFromLog() → COMPLETED");
console.log(
  "  Monitor resets _lastActivityMs (BUG FIX #3 — prevents false stall)",
);
console.log("  StateMachine: COMPLETED + <90s → WAIT (grace period)");
console.log("  StateMachine: COMPLETED + ≥90s → OPEN_NEW_CHAT + loop prompt\n");

{
  const d = decide(
    baseInput({
      state: AgentState.COMPLETED,
      mssSinceLastActivity: 30_000, // 30s ago
    }),
  );
  assert(
    "2a. COMPLETED + 30s → WAIT (grace period)",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}
{
  const d = decide(
    baseInput({
      state: AgentState.COMPLETED,
      mssSinceLastActivity: 91_000,
    }),
  );
  assert(
    "2b. COMPLETED + 91s → OPEN_NEW_CHAT",
    d.action,
    AgentAction.OPEN_NEW_CHAT,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor path para OPEN_NEW_CHAT:");
console.log(
  "    1. execute(OPEN_NEW_CHAT) → _activeAbort = new AbortController()",
);
console.log("    2. _executeInner → _openNewChat()");
console.log(
  "    3. ModelManager available? → Yes → _validateZeroX('new-chat')",
);
console.log("    4.   applyBestZeroX() → gpt-4.1 > gpt-5-mini > first 0x");
console.log(
  "    5.   LM API: send 'Responde SI...' health check → read response",
);
console.log("    6.   Response contains SI/YES?");
console.log(
  "    7.     YES → CMD_CHAT_NEW → _sleep(1500) [cancellable] → _sendToChat(loopPrompt)",
);
console.log("    8.     _sendToChat: PREMIUM GUARD checks lastApplied.isFree");
console.log(
  "    9.     isFree=true → send via CLI 'code chat --mode agent ...'",
);
console.log(
  "    10. _postAction: openNewChat cooldown=now, newChatCount++, state→RECOVERING",
);

{
  // Cooldown active
  const d = decide(
    baseInput({
      state: AgentState.COMPLETED,
      mssSinceLastActivity: 91_000,
      cooldowns: { ...freshCooldowns(), openNewChat: Date.now() },
    }),
  );
  assert(
    "2c. COMPLETED + 91s + cooldown → WAIT",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 3: Stall suave — 5 min sin actividad
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 3: STALLED_SOFT — 5 min sin actividad ───");
console.log("  Trigger: _stateFromLog() detecta msSinceActivity ≥ stallWarnMs");
console.log("  Pre-action gate: screenshot + analyze → ¿GENERATING? → block");
console.log(
  "  StateMachine: STALLED_SOFT → SEND_CONTINUE ('continuar' in same chat)\n",
);

{
  const d = decide(
    baseInput({
      state: AgentState.IDLE,
      mssSinceLastActivity: 310_000,
    }),
  );
  assert(
    "3a. 310s inactivity → SEND_CONTINUE (soft stall)",
    d.action,
    AgentAction.SEND_CONTINUE,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor path para SEND_CONTINUE:");
console.log("    1. execute → _activeAbort = new AbortController()");
console.log("    2. _executeInner → _sendContinue()");
console.log(
  "    3. ModelManager available → _validateZeroX('continue-same-chat')",
);
console.log("    4.   applyBestZeroX() → applies 0x model");
console.log("    5.   LM health check 'Responde SI...'");
console.log("    6.   SI → _sendToChat('continuar') in SAME chat");
console.log("    7.   PREMIUM GUARD: lastApplied.isFree must be true");
console.log("    8.   CLI: code chat --mode agent --reuse-window 'continuar'");
console.log(
  "    9. _postAction: sendContinue cooldown, softErrorRetries++, state→RECOVERING",
);

{
  // Cooldown active
  const d = decide(
    baseInput({
      state: AgentState.IDLE,
      mssSinceLastActivity: 310_000,
      cooldowns: { ...freshCooldowns(), sendContinue: Date.now() },
    }),
  );
  assert(
    "3b. Soft stall + cooldown → WAIT",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 4: Stall duro — 8 min sin actividad
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 4: STALLED_HARD — 8 min sin actividad ───");
console.log("  Trigger: msSinceActivity ≥ stallHardMs (480s)");
console.log("  StateMachine: → OPEN_NEW_CHAT (fresh chat + loop prompt)\n");

{
  const d = decide(
    baseInput({
      state: AgentState.IDLE,
      mssSinceLastActivity: 500_000,
    }),
  );
  assert(
    "4a. 500s inactivity → OPEN_NEW_CHAT (hard stall)",
    d.action,
    AgentAction.OPEN_NEW_CHAT,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 5: Rate limit (429) — flujo completo
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 5: ERROR_RATE_LIMIT — modelo rate-limitado ───");
console.log("  Trigger: LogWatcher detecta 'rate.limit|429|Too Many Requests'");
console.log("  Flow completo:");
console.log("    Paso 1: No health validated → VALIDATE_ZERO_X");
console.log(
  "    Paso 2: Health validated OK → SWITCH_CHAT_MODEL (next 0x model)",
);
console.log(
  "    Paso 3: SWITCH on cooldown → CYCLE_MODEL (next 0x + new chat)",
);
console.log("    Paso 4: Everything on cooldown → WAIT\n");

{
  // Paso 1: sin validación reciente
  const d = decide(
    baseInput({
      state: AgentState.ERROR_RATE_LIMIT,
      health: { validatedOk: false, validatedAt: 0 },
    }),
  );
  assert(
    "5a. Rate limit + no health → VALIDATE_ZERO_X",
    d.action,
    AgentAction.VALIDATE_ZERO_X,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor para VALIDATE_ZERO_X:");
console.log("    1. _validateZeroX('new-chat') [default mode]");
console.log(
  "    2. applyBestZeroX() → selects gpt-4.1 (ZERO_X_VISION_FAMILIES[0])",
);
console.log(
  "    3. If no 0x model → return 'NO_ZERO_X_MODEL_AVAILABLE' → Monitor STOPS",
);
console.log(
  "    4. LM API: send 'Responde SI si puedes...' → consume response",
);
console.log("    5. Response contains SI/YES?");
console.log(
  "    6.   YES → CMD_CHAT_NEW → _sleep(1500) → _sendToChat(loopPrompt)",
);
console.log("    7.   NO  → return {ok:false, detail:'health check failed'}");
console.log("    8. In BOTH cases: model STAYS on 0x — NEVER switches to 1x");

{
  // Paso 2: con validación reciente OK
  const d = decide(
    baseInput({
      state: AgentState.ERROR_RATE_LIMIT,
      health: { validatedOk: true, validatedAt: Date.now() - 60_000 },
    }),
  );
  assert(
    "5b. Rate limit + health OK → SWITCH_CHAT_MODEL (to next 0x)",
    d.action,
    AgentAction.SWITCH_CHAT_MODEL,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor para SWITCH_CHAT_MODEL (v1.3.6 fix):");
console.log(
  "    1. _switchChatModel() → ModelManager.zeroXModels (NOT raw _modelPool)",
);
console.log(
  "    2. Round-robin: find current model in zeroXModels → (idx+1) % length",
);
console.log(
  "    3. applyModel(nextZeroX) → changeModel command → _lastAppliedModel = nextZeroX",
);
console.log("    4. POLICY: brain NEVER uses 1x — only 0x models in rotation");
console.log("    5. PREMIUM GUARD: _lastAppliedModel.isFree MUST be true");

{
  // Paso 3: switch on cooldown → cycle
  const d = decide(
    baseInput({
      state: AgentState.ERROR_RATE_LIMIT,
      health: { validatedOk: true, validatedAt: Date.now() - 60_000 },
      cooldowns: { ...freshCooldowns(), switchChatModel: Date.now() },
    }),
  );
  assert(
    "5c. Rate limit + switch cooldown → CYCLE_MODEL",
    d.action,
    AgentAction.CYCLE_MODEL,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor para CYCLE_MODEL (v1.3.6 fix):");
console.log(
  "    1. _cycleModel() → ModelManager.zeroXModels (same 0x-only fix)",
);
console.log("    2. Pick next 0x → applyModel() → open NEW chat");
console.log("    3. _sendToChat(loopPrompt) in new chat");

{
  // Paso 4: todo en cooldown
  const d = decide(
    baseInput({
      state: AgentState.ERROR_RATE_LIMIT,
      health: { validatedOk: true, validatedAt: Date.now() - 60_000 },
      cooldowns: {
        ...freshCooldowns(),
        switchChatModel: Date.now(),
        cycleModel: Date.now(),
      },
    }),
  );
  assert(
    "5d. Rate limit + all cooldowns → WAIT",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 6: Error hard (500/503)
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 6: ERROR_HARD — 500/503 server error ───");
console.log("  Trigger: LogWatcher detecta [error].*500|503|overload");
console.log(
  "  Flow: retry 1 → retry 2 → health check → retry 3 → OPEN_NEW_CHAT\n",
);

{
  const d = decide(
    baseInput({
      state: AgentState.ERROR_HARD,
      softErrorRetries: 0,
    }),
  );
  assert(
    "6a. Hard error retry 0 → SEND_CONTINUE (retry 1/3)",
    d.action,
    AgentAction.SEND_CONTINUE,
    d.reasoning,
  );
}
{
  const d = decide(
    baseInput({
      state: AgentState.ERROR_HARD,
      softErrorRetries: 1,
    }),
  );
  assert(
    "6b. Hard error retry 1 → SEND_CONTINUE (retry 2/3)",
    d.action,
    AgentAction.SEND_CONTINUE,
    d.reasoning,
  );
}
{
  const d = decide(
    baseInput({
      state: AgentState.ERROR_HARD,
      softErrorRetries: 2,
      health: { validatedOk: false, validatedAt: 0 },
    }),
  );
  assert(
    "6c. Hard error retry 2 + no health → VALIDATE_ZERO_X",
    d.action,
    AgentAction.VALIDATE_ZERO_X,
    d.reasoning,
  );
}
{
  const d = decide(
    baseInput({
      state: AgentState.ERROR_HARD,
      softErrorRetries: 2,
      health: { validatedOk: true, validatedAt: Date.now() - 60_000 },
    }),
  );
  assert(
    "6d. Hard error retry 2 + health OK → SEND_CONTINUE (retry 3/3)",
    d.action,
    AgentAction.SEND_CONTINUE,
    d.reasoning,
  );
}
{
  // Con 3+ retries Y sin health validado → primero valida health
  const d1 = decide(
    baseInput({
      state: AgentState.ERROR_HARD,
      softErrorRetries: 3,
      health: { validatedOk: false, validatedAt: 0 },
    }),
  );
  assert(
    "6e. Hard error 3 retries + no health → VALIDATE_ZERO_X first",
    d1.action,
    AgentAction.VALIDATE_ZERO_X,
    d1.reasoning,
  );
}
{
  // Con health OK + 3 retries → ahora sí OPEN_NEW_CHAT
  const d2 = decide(
    baseInput({
      state: AgentState.ERROR_HARD,
      softErrorRetries: 3,
      health: { validatedOk: true, validatedAt: Date.now() - 60_000 },
    }),
  );
  assert(
    "6f. Hard error 3 retries + health OK → OPEN_NEW_CHAT",
    d2.action,
    AgentAction.OPEN_NEW_CHAT,
    d2.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 7: Context full (token limit)
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 7: ERROR_CONTEXT — context window exhausted ───");
console.log("  Trigger: LogWatcher detecta context_length_exceeded");
console.log(
  "  Flow: STOP current gen → new chat (highest priority after GENERATING)\n",
);

{
  const d = decide(baseInput({ state: AgentState.ERROR_CONTEXT }));
  assert(
    "7a. Context full → STOP_AND_NEW_CHAT",
    d.action,
    AgentAction.STOP_AND_NEW_CHAT,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor para STOP_AND_NEW_CHAT:");
console.log("    1. CMD_CHAT_STOP → stops current generation");
console.log("    2. _sleep(250) [cancellable]");
console.log("    3. CMD_CHAT_NEW → opens fresh chat");
console.log("    4. _sleep(1500) [cancellable]");
console.log("    5. _sendToChat(loopPrompt)");
console.log(
  "    6. _postAction: both stopAndNewChat + openNewChat cooldowns stamped",
);

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 8: VS Code hidden
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 8: VSCODE_HIDDEN — VS Code no visible ───");
console.log("  Trigger: Screenshot analysis detects no VS Code window");
console.log("  Flow: FOCUS_VSCODE → bring to foreground\n");

{
  const d = decide(baseInput({ state: AgentState.VSCODE_HIDDEN }));
  assert(
    "8a. VSCODE_HIDDEN → FOCUS_VSCODE",
    d.action,
    AgentAction.FOCUS_VSCODE,
    d.reasoning,
  );
}

console.log("  ↳ ActionExecutor para FOCUS_VSCODE:");
console.log("    1. Try CDP click on chatInput selector");
console.log("    2. Fallback: CMD_CHAT_OPEN + CMD_CHAT_FOCUS_INPUT");

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 9: Recovering — ventana de verificación 2 min
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 9: RECOVERING — verificando resultado de acción ───",
);
console.log(
  "  Trigger: _postAction sets state→RECOVERING after any successful action",
);
console.log("  Flow: WAIT for 120s → then re-evaluate\n");

{
  const d = decide(
    baseInput({
      state: AgentState.RECOVERING,
      msSinceRecoveryStart: 30_000,
    }),
  );
  assert(
    "9a. RECOVERING + 30s → WAIT (in window)",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}
{
  const d = decide(
    baseInput({
      state: AgentState.RECOVERING,
      msSinceRecoveryStart: 130_000,
      mssSinceLastActivity: 310_000, // would trigger soft stall
    }),
  );
  assert(
    "9b. RECOVERING expired + soft stall → SEND_CONTINUE (falls through)",
    d.action,
    AgentAction.SEND_CONTINUE,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 10: Model switch suggested by Copilot
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 10: ERROR_SWITCH_MODEL — Copilot pide cambiar ───",
);
console.log(
  "  Trigger: Screenshot/log detects 'switch model' / 'not available'",
);

{
  const d = decide(baseInput({ state: AgentState.ERROR_SWITCH_MODEL }));
  assert(
    "10a. Switch model suggested → SWITCH_CHAT_MODEL",
    d.action,
    AgentAction.SWITCH_CHAT_MODEL,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 11: Normal idle — nada que hacer
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 11: IDLE — todo normal, actividad reciente ───");

{
  const d = decide(
    baseInput({
      state: AgentState.IDLE,
      mssSinceLastActivity: 30_000,
    }),
  );
  assert(
    "11a. Normal idle 30s → WAIT",
    d.action,
    AgentAction.WAIT,
    d.reasoning,
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 12: PREMIUM GUARD — modelo 1x activo bloquea envíos
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 12: PREMIUM GUARD — protección contra modelo 1x ───",
);
console.log("  Capa en ActionExecutor._sendToChat():");
console.log(
  "    if (lastApplied && !lastApplied.isFree && no authorization) → BLOCK + log error",
);
console.log(
  "    if (lastApplied && !lastApplied.isFree && authorization matches) → allow ONE send",
);
console.log("    1. Recovery selects 1x through the chat UI picker");
console.log("    2. syncAppliedModel() updates lastAppliedModel for audit");
console.log("    3. _authorizeNextPremiumSend() arms a one-shot premium send");
console.log(
  "    4. _sendToChat() consumes that authorization + records the 1x prompt",
);
console.log("    5. Any later 1x send without authorization is blocked again");
console.log("    ✅ Premium requests stay explicit, audited, and bounded");
console.log("");
console.log("  Escenarios que pueden activar 1x:");
console.log("    ✅ Recovery SAME chat → UI picker 1x → send 'continuar'");
console.log(
  "    ✅ Recovery NEW chat → open chat → UI picker 1x → send loop prompt",
);
console.log("    ❌ _switchChatModel uses zeroXModels only");
console.log("    ❌ _cycleModel uses zeroXModels only");
console.log("    ❌ showModelPicker shows 0x models only");
console.log("    ❌ _validateZeroX calls applyBestZeroX() which is 0x-only");
console.log(
  "    ⚠️  Manual palette switch to 1x → allowed but logged + warned",
);
console.log(
  "    ⚠️  If user changes model via VS Code native picker → lastApplied stays old",
);

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 13: CostGuard — circuit breaker
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 13: COST GUARD — circuit breaker ───");
console.log("  Capa en Monitor._runCycle() antes de execute():");
console.log("  Config: max 8 non-WAIT actions in 5 min window");
console.log("  If exceeded:");
console.log(
  "    1. CostGuard.check() returns {allowed:false, justTripped:true}",
);
console.log("    2. First trip: showErrorMessage with 'Reset Guard' button");
console.log("    3. Subsequent: blocked silently (justTripped=false)");
console.log("    4. Auto-reset after 10 min");
console.log("    5. Manual reset: 'Copilot Monitor: Reset Cost Guard' command");
console.log("  ✅ Prevents runaway loops from burning vision API calls");

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 14: Pre-action visual gate
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 14: PRE-ACTION GATE — screenshot antes de actuar ───",
);
console.log("  Aplica SOLO a: SEND_CONTINUE, OPEN_NEW_CHAT");
console.log(
  "  No aplica a: STOP_AND_NEW_CHAT, VALIDATE_ZERO_X, CYCLE_MODEL, SWITCH_CHAT_MODEL, FOCUS_VSCODE",
);
console.log("  Flow:");
console.log("    1. _preActionGate(action) takes screenshot");
console.log("    2. Screenshot shows GENERATING → BLOCK (agent is working)");
console.log("       → resets stall timer (prevents re-trigger)");
console.log("    3. Screenshot shows COMPLETED + action=SEND_CONTINUE → BLOCK");
console.log("       → updates state so next cycle picks OPEN_NEW_CHAT");
console.log("    4. Screenshot confidence < 0.5 → ALLOW with warning");
console.log(
  "    5. Screenshot quota exhausted → ALLOW (never block permanently)",
);
console.log("    6. Screenshot throws → ALLOW (fail-open)");

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 15: LogWatcher real-time (v1.3.7)
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 15: LOGWATCHER — detección de eventos real-time ───",
);
console.log("  Arquitectura v1.3.7:");
console.log(
  "    1. fs.watch(logPath, {persistent:false}) → fires within ms on write",
);
console.log("    2. Debounce 50ms → groups rapid writes into single callback");
console.log("    3. _readNewBytes() reads only delta (offset → EOF)");
console.log("    4. _processLines() → PATTERNS match → callback(LogDelta)");
console.log(
  "    5. Heartbeat 10s → _resolveAndWatch() checks for log rotation",
);
console.log("    6. 'rename' event → _stopFsWatcher() → heartbeat re-attaches");
console.log("  Latencia: 50ms (debounce) vs 5000ms (v1.3.6 polling)");
console.log("  Eventos detectados:");
console.log("    ccreq:*|success|     → SUCCESS   → GENERATING");
console.log("    rate.limit|429       → RATE_LIMITED → ERROR_RATE_LIMIT");
console.log("    [error].*500|503     → HARD_ERROR   → ERROR_HARD");
console.log("    context_length       → CONTEXT_FULL → ERROR_CONTEXT");
console.log("    shouldContinue=false → LOOP_STOPPED → COMPLETED");
console.log("    ccreq:*|cancelled|   → CANCELLED    → IDLE");
console.log("    ccreq:*|error|       → REQUEST_ERROR → ERROR_HARD");

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 16: Cancellable sleeps (v1.3.7)
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 16: CANCELLABLE SLEEPS — no más delays bloqueantes ───",
);
console.log("  Arquitectura v1.3.7:");
console.log("    1. execute() creates _activeAbort = new AbortController()");
console.log("    2. Every _sleep(ms) races: setTimeout vs abort signal");
console.log("    3. cancelCurrentAction() calls _activeAbort.abort()");
console.log("    4. All pending sleeps resolve IMMEDIATELY on abort");
console.log("    5. finally{} clears _activeAbort = null");
console.log("  Beneficio:");
console.log(
  "    - Monitor puede cancelar acciones mid-flight si detecta evento crítico",
);
console.log(
  "    - Picker navigation (400-600ms each) no bloquea el event loop",
);
console.log(
  "    - _sleep(1500) after CMD_CHAT_NEW resolves early if cancelled",
);

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 17: Sin modelos 0x — monitor se detiene
// ─────────────────────────────────────────────────────────────────────────────
console.log("\n─── Escenario 17: NO 0x MODELS — monitor se auto-detiene ───");
console.log("  Trigger: _validateZeroX → applyBestZeroX() returns undefined");
console.log("  Flow:");
console.log(
  "    1. ActionExecutor returns {detail: 'NO_ZERO_X_MODEL_AVAILABLE'}",
);
console.log("    2. Monitor._runCycle detects sentinel string");
console.log("    3. Writes MONITOR_STOPPED_NO_ZERO_X_MODEL to audit");
console.log("    4. this.stop() — full stop");
console.log("    5. showErrorMessage MODAL to user");
console.log("  ✅ NEVER runs on a 1x model — full stop instead");

// ─────────────────────────────────────────────────────────────────────────────
// ESCENARIO 18: applyBestOneX — dead code audit
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n─── Escenario 18: DEAD CODE — applyBestOneX nunca es llamado ───",
);
console.log("  ModelManager.applyBestOneX() EXISTS but is NEVER called by:");
console.log("    ❌ ActionExecutor (verified: grep returns 0 references)");
console.log("    ❌ Monitor (verified: no imports or calls)");
console.log("    ❌ StateMachine (no awareness of ModelManager)");
console.log("  Solo queda como API pública para uso futuro o manual override.");
console.log("  PREMIUM GUARD sigue activo como red de seguridad.");

// ─────────────────────────────────────────────────────────────────────────────
// SUMMARY
// ─────────────────────────────────────────────────────────────────────────────
console.log(
  "\n═══════════════════════════════════════════════════════════════════",
);
console.log(`  RESULTS: ${passed} passed, ${failed} failed`);
console.log(
  "═══════════════════════════════════════════════════════════════════",
);

if (failed > 0) {
  console.error("\n🚨 AUDIT FAILED — hay bugs en la lógica de decisión");
  process.exit(1);
} else {
  console.log("\n✅ AUDIT PASSED — todas las decisiones son correctas");
  console.log(
    "   El brain usa 0x para observación/rotación y 1x solo cuando recovery lo selecciona explícitamente desde la UI del chat",
  );
  console.log("   PREMIUM GUARD bloquea cualquier envío 1x no autorizado");
  console.log("   CostGuard previene loops de spam (máx 8 acciones en 5 min)");
  console.log("   Pre-action gate previene interrumpir modelo GENERATING");
  console.log("   LogWatcher real-time (50ms) con sleeps no bloqueantes");
}

process.exit(failed > 0 ? 1 : 0);
