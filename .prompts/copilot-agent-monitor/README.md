# Copilot Agent Monitor

VS Code extension that keeps GitHub Copilot Agent always coding — autonomously, without abusing the 0x free model quota.

## How it works

```
Every 30s (or on log event):

  1. READ Copilot log file       ← FREE, no API calls
     success / rate_limit / error / context_full → derive state

  2. IF stall suspected AND 3+ min since last screenshot:
     TAKE screenshot              ← one GPT-4o call (0x, free)
     ANALYZE what the UI shows   → confirmed state

  3. STATE MACHINE decides action (pure logic, no LLM)
     GENERATING      → wait
     COMPLETED/IDLE  → wait
     STALLED 5 min   → send "continuar"
     STALLED 8 min   → open new chat + loop prompt
     RATE LIMIT      → switch model in current chat
     ERROR 500/503   → retry up to 3x, then new chat
     CONTEXT FULL    → stop + new chat
```

In normal operation (model coding → model completes → continue), the extension makes **zero vision API calls**. Screenshots only happen when there's real ambiguity (stall detection).

## Installation

```bash
# 1. Clone and install
git clone <repo>
cd copilot-agent-monitor
npm install
npm run compile

# 2. Open in VS Code
code .

# 3. Press F5 to launch Extension Development Host
# OR package it:
npx @vscode/vsce package
code --install-extension copilot-agent-monitor-1.0.0.vsix
```

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `copilotMonitor.enabled` | `true` | Auto-start on VS Code open |
| `copilotMonitor.stallWarnSecs` | `300` | Seconds before sending "continuar" (5 min) |
| `copilotMonitor.stallHardSecs` | `480` | Seconds before opening new chat (8 min) |
| `copilotMonitor.screenshotMinIntervalSecs` | `180` | Min seconds between screenshots (3 min) |
| `copilotMonitor.maxScreenshotsPerHour` | `15` | Hard cap on vision API calls per hour |
| `copilotMonitor.loopPromptFile` | `.prompts/AGENT_LOOP_PROMPT.md` | Prompt sent when opening a new chat |
| `copilotMonitor.modelPool` | `["gpt-4o", "gpt-4.1", "gpt-5-mini"]` | 0x models to rotate on rate limit |
| `copilotMonitor.visionModel` | `"gpt-4o"` | Model for screenshot analysis (needs vision) |

## Loop Prompt

Create `.prompts/AGENT_LOOP_PROMPT.md` in your workspace:

```markdown
Continúa con las tareas pendientes del sprint. Trabaja en modo agente.
Sigue el plan en TASKS.md. Usa las herramientas disponibles.
No pidas confirmación — actúa directamente.
```

## Commands

| Command | Description |
|---------|-------------|
| `Copilot Monitor: Start` | Start the monitor |
| `Copilot Monitor: Stop` | Stop the monitor |
| `Copilot Monitor: Analyze Now (Screenshot)` | Force immediate screenshot analysis |
| `Copilot Monitor: Show Activity Log` | Show last 20 actions in QuickPick |

## Status Bar

The status bar item (bottom right) always shows the current state:

| Icon | State | Meaning |
|------|-------|---------|
| `⟳` | GENERATING | Model is actively coding |
| `✓` | COMPLETED | Model finished its turn |
| `👁` | IDLE | Waiting, no issues |
| `⚠` | STALLED_SOFT | Sending continue soon |
| `✗` | STALLED_HARD | Opening new chat |
| `🕐` | ERROR_RATE_LIMIT | Switching model |
| `↺` | RECOVERING | Waiting for action result |

Click the status bar item to see the full activity log.

## Why not Gemma / Ollama?

The previous Python agent required Ollama running locally with gemma3:4b. This extension:
- Uses **GPT-4o (0x tier)** via VS Code's built-in Copilot integration — no local LLM needed
- Uses the vision model only when needed (≤15 calls/hour default)
- Everything else is pure logic — the state machine handles 95% of decisions for free

## Model quota impact

Typical session (8 hours, model coding continuously):
- Log file reads: ~1,000 (completely free)
- Screenshot analyses: ~10-20 (only during stalls/ambiguity)
- Model rotation calls: ~0-5 (only on rate limits)

The 0x models are "free" but GitHub still has fair-use policies. 15 vision calls/hour is well within normal developer usage.
