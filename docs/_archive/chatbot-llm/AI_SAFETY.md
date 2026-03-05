# OKLA Chatbot LLM — AI Safety Layers (Dual-Mode v2.0)

## Overview

The chatbot implements **6 layers of safety** to protect users,
prevent misuse, and ensure compliance with Dominican Republic regulations.

> **v2.0 update:** Added Layer 3.5 (Boundary Enforcement) for mode-specific
> grounding validation in SingleVehicle and DealerInventory modes.

---

## Layer 1: Input Security

### Prompt Injection Detection (`PromptInjectionDetector`)

**Location:** `Application/Services/PromptInjectionDetector.cs`

Detects and blocks attempts to override the system prompt:

| Category        | Examples                                         |
| --------------- | ------------------------------------------------ |
| Role Override   | "Ignore previous instructions", "You are now..." |
| System Leak     | "Print your system prompt", "Show instructions"  |
| Jailbreak       | "DAN mode", "Bypass safety", "No restrictions"   |
| Encoding Bypass | Base64 encoded instructions, unicode tricks      |

**Action:** Block (high threat) or sanitize (medium threat).

### PII Detection (`PiiDetector`)

**Location:** `Application/Services/PiiDetector.cs`

Detects and redacts personal data before it reaches the LLM:

| PII Type     | Pattern (DR-specific)         | Action           |
| ------------ | ----------------------------- | ---------------- | --------------------------- | ---- |
| Cédula       | `\d{3}-?\d{7}-?\d`            | Redact           |
| Credit Card  | `\d{4}[\s-]?\d{4}[\s-]?\d{4}` | Block + transfer |
| Phone        | `(809                         | 829              | 849)[\s-]?\d{3}[\s-]?\d{4}` | Warn |
| Email        | Standard email regex          | Warn             |
| Bank Account | `\d{10,20}` (context-based)   | Redact           |
| RNC          | `\d{1}-?\d{2}-?\d{5}-?\d`     | Warn             |
| Passport     | `[A-Z]{2}\d{7}`               | Redact           |
| Address      | Street patterns with numbers  | Warn             |

### Content Moderation (`ContentModerationFilter`)

**Location:** `Application/Services/ContentModerationFilter.cs`

Blocks inappropriate content:

| Category         | Examples                           | Action         |
| ---------------- | ---------------------------------- | -------------- |
| Scam/Fraud       | "Western union", "pago adelantado" | Block + warn   |
| Violence/Threats | "te voy a matar"                   | Block + report |
| Off-topic        | Non-vehicle solicitation           | Redirect       |
| Hate Speech      | Discriminatory language            | Block          |

---

## Layer 2: Rate Limiting

### Per-Session

- Max interactions per session (configurable, default: 50)
- Graceful degradation with transfer-to-agent option

### WhatsApp Per-Phone

- 10 messages/minute per phone number
- Country filter: Only +1809, +1829, +1849 (Dominican Republic)

### API-Level

- ASP.NET Rate Limiting middleware
- `SessionStart`: 10/min per IP
- `ChatMessage`: 30/min per session

---

## Layer 3: Output Validation

### Grounding Validator (`OutputGroundingValidator`)

**Location:** `Application/Services/OutputGroundingValidator.cs`

Validates LLM output against actual inventory data **per mode**:

| Check                 | SV Mode                                    | DI Mode                                    |
| --------------------- | ------------------------------------------ | ------------------------------------------ |
| Price accuracy        | Within RD$1000 of the SINGLE vehicle price | Within RD$1000 of any RAG-returned vehicle |
| Vehicle existence     | ONLY the vehicle in context                | ONLY vehicles in provided RAG results      |
| Hallucination phrases | Blocks "te consigo", "tenemos otros"       | Blocks "puedo pedirlo", "en otro dealer"   |
| Financial claims      | Never promises loans, approvals, or rates  | Same                                       |
| Cross-context         | Never mentions other vehicles              | Never mentions other dealers               |

### PII Echo-Back Prevention

Even if PII was detected but not blocked, the LLM response
is scanned for any PII before sending to user:

```csharp
botResponse = PiiDetector.SanitizeResponse(llmResult.FulfillmentText);
```

### Identity Integrity

Bot never claims to be human. Detected phrases:

- "soy una persona real"
- "no soy un bot"
- "soy humano"

---

## Layer 3.5: Boundary Enforcement (NEW in v2.0)

**Location:** `Application/Services/OutputGroundingValidator.cs` + `ChatModeRouter`

Mode-specific boundary rules that prevent the chatbot from exceeding its knowledge scope:

### SingleVehicle Boundaries

| Violation                            | Detection                   | Action                                            |
| ------------------------------------ | --------------------------- | ------------------------------------------------- |
| Mentions another vehicle by name     | Regex/NLU on response       | Replace with redirect to dealer profile           |
| Offers alternatives ("tenemos un X") | Keyword detection           | Replace with "solo puedo hablar de este vehículo" |
| Compares with another vehicle        | Detects comparison patterns | Replace with redirect to DI mode                  |
| Invents specs not in context         | Check against vehicle data  | Strip ungrounded claims                           |

### DealerInventory Boundaries

| Violation                        | Detection                               | Action                                                |
| -------------------------------- | --------------------------------------- | ----------------------------------------------------- |
| Mentions competitor dealer       | Named entity detection                  | Replace with "solo manejo inventario de {dealerName}" |
| Recommends "the best" vehicle    | Pattern: "te recomiendo", "la mejor es" | Neutralize to factual comparison                      |
| Invents vehicle not in RAG       | Check vehicleId against results         | Strip or correct                                      |
| Promises cross-dealer comparison | Pattern detection                       | CrossDealerRefusal response                           |

### Boundary Enforcement Flow

```
LLM Response Generated
    │
    ├── [SV Mode]
    │     ├── Mentions other vehicle? ──Yes──► REWRITE to redirect
    │     ├── Offers alternatives? ──Yes──► REWRITE to single-vehicle focus
    │     └── Invents specs? ──Yes──► STRIP ungrounded claims
    │
    ├── [DI Mode]
    │     ├── Mentions competitor? ──Yes──► REWRITE to CrossDealerRefusal
    │     ├── Recommends "best"? ──Yes──► NEUTRALIZE to factual
    │     └── Invents vehicle? ──Yes──► STRIP or suggest search
    │
    └── [General Mode]
          └── Recommends specific vehicle? ──Yes──► REDIRECT to browse site
```

---

## Layer 4: Handoff Safety

### Bot → Human Takeover

- Dealer can take over any chat session
- System message notifies user clearly
- Bot stops responding until control returned

### Human → Bot Return

- Agent returns control with explicit action
- System message confirms bot is back
- Full conversation history preserved

### Auto-Transfer Triggers

- Credit card PII detected → transfer to agent
- 3+ consecutive fallbacks → suggest agent
- User explicitly requests human help

---

## Layer 5: Compliance (DR)

### DGII / Tax

- Never provides tax advice
- Never calculates ITBIS

### Pro-Consumidor

- Never makes false claims about warranties
- Always suggests verifying with dealer
- Includes disclaimers on pricing

### Data Protection

- PII never stored in LLM conversation history
- Sessions expire after configurable timeout
- User can request data deletion

---

## Safety Decision Matrix (v2.0)

```
User Input
    │
    ├─ [Prompt Injection?] ──Yes──► BLOCK (return safe message)
    │         No
    ├─ [PII Detected?] ──CreditCard──► BLOCK + TRANSFER
    │         │
    │     Other PII ──► REDACT and continue
    │         No
    ├─ [Content Moderation] ──Unsafe──► BLOCK/REDIRECT
    │         Safe
    ├─ [Rate Limited?] ──Yes──► THROTTLE
    │         No
    ▼
  LLM Processing (mode-aware: SV / DI / GEN)
    │
    ▼
  Output
    │
    ├─ [Grounding Valid?] ──No──► SANITIZE + DISCLAIMER
    │         Yes
    ├─ [Boundary Check (v2.0)]
    │     ├─ SV: other vehicle? ──Yes──► REWRITE redirect
    │     ├─ DI: other dealer?  ──Yes──► REWRITE CrossDealerRefusal
    │     └─ DI: recommends "best"? ──Yes──► NEUTRALIZE
    │         Pass
    ├─ [PII in Response?] ──Yes──► REDACT
    │         No
    ├─ [Identity Claim?] ──Yes──► REPLACE
    │         No
    ▼
  Safe Response → User
```
