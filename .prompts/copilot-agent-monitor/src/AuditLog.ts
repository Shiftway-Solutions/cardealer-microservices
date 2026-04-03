/**
 * AuditLog — Persistent JSONL audit trail for every monitor cycle.
 *
 * File location:  <workspace>/.github/copilot-monitor-audit.jsonl
 * Format:         One JSON object per line — easy to query with jq or Python.
 * Rotation:       New file when it exceeds 500 KB → kept as .jsonl.1, .jsonl.2, .jsonl.3
 *
 * Purpose:
 *   - Replay every decision the monitor made to find bugs retroactively.
 *   - Understand which states trigger which actions and why.
 *   - Verify the 0x / 1x model discipline is respected at runtime.
 *
 * Query examples:
 *   # All non-WAIT actions today
 *   jq 'select(.action != "WAIT")' .github/copilot-monitor-audit.jsonl
 *
 *   # Failed actions only
 *   jq 'select(.ok == false)' .github/copilot-monitor-audit.jsonl
 *
 *   # Vision calls and their 0x status
 *   jq 'select(.vision != null) | {ts, .vision.modelId, .vision.isZeroX, .vision.state}' .github/copilot-monitor-audit.jsonl
 *
 *   # Rate-limit triggers
 *   jq 'select(.state == "ERROR_RATE_LIMIT")' .github/copilot-monitor-audit.jsonl
 *
 *   # Cycles where cost guard fired
 *   jq 'select(.costGuard.blocked == true)' .github/copilot-monitor-audit.jsonl
 */

import * as fs from "fs";
import * as path from "path";

// ─── Schema ───────────────────────────────────────────────────────────────────

/** Vision analysis metadata — included only when a screenshot was taken. */
export interface VisionAudit {
  /** Family name of the model used (e.g. "gpt-4o"). */
  modelId: string;
  /** Was this model classified as 0x (free / no quota cost)? */
  isZeroX: boolean;
  /** AgentState the vision model predicted. */
  state: string;
  /** Confidence 0–1. */
  confidence: number;
  /** Human-readable description of what was seen. */
  detail: string;
  /** Was the confidence ≥ 0.7 so we trusted it over the log signal? */
  trusted: boolean;
  /** How long the vision API call + parsing took (ms). */
  durationMs: number;
}

/** One record per monitor cycle that took an action (WAIT cycles are skipped). */
export interface AuditEntry {
  /** ISO 8601 timestamp. */
  ts: string;
  /** Schema version — increment if format changes. */
  v: 1;
  /** Incrementing counter within this VS Code session. */
  cycle: number;
  /** Total time the cycle took from observe to action completion (ms). */
  durationMs: number;

  // ── Observation ──────────────────────────────────────────────────────────
  /** How we derived the state: "log" | "screenshot" | "recovering" | "stall_timer". */
  source: string;
  /** AgentState observed this cycle. */
  state: string;
  /** Milliseconds since the last activity timestamp was updated. */
  stallMs: number;

  // ── Decision ─────────────────────────────────────────────────────────────
  /** AgentAction decided by the StateMachine. */
  action: string;
  /** Human-readable reasoning from the StateMachine. */
  reasoning: string;

  // ── Execution ─────────────────────────────────────────────────────────────
  /**
   * Whether the action succeeded. null = action was WAIT or blocked by cost guard.
   */
  ok: boolean | null;
  /** Detail string from the executor (or "WAIT" / "COST_GUARD_BLOCKED"). */
  detail: string;

  // ── Context snapshot ─────────────────────────────────────────────────────
  /** Current value of github.copilot.chat.languageModel setting. */
  configuredModel: string;
  /** Consecutive soft-error retries before this cycle. */
  softErrorRetries: number;
  /** Total model rotations this session. */
  modelRotations: number;
  /** Total new chats opened this session. */
  newChatCount: number;

  // ── Optional detail blocks ────────────────────────────────────────────────
  /** Present only when a screenshot was taken. */
  vision?: VisionAudit;
  /** Present only when the cost guard fired. */
  costGuard?: { blocked: boolean; reason: string };
}

// ─── AuditLog ─────────────────────────────────────────────────────────────────

export class AuditLog {
  private static readonly MAX_SIZE_BYTES = 512 * 1024; // 500 KB per file
  private static readonly MAX_ROTATIONS = 3; // keep .jsonl, .jsonl.1, .jsonl.2, .jsonl.3

  private _logPath: string;
  private _cycleCounter: number = 0;

  constructor(workspaceRoot: string) {
    this._logPath = path.join(
      workspaceRoot,
      ".github",
      "copilot-monitor-audit.jsonl",
    );
    this._ensureDir();
  }

  /** Absolute path to the current audit file. */
  get filePath(): string {
    return this._logPath;
  }

  /** Returns the next cycle number for this VS Code session. */
  nextCycle(): number {
    return ++this._cycleCounter;
  }

  /** Append an audit entry. Never throws — audit must not crash the monitor. */
  record(entry: AuditEntry): void {
    try {
      this._rotateIfNeeded();
      fs.appendFileSync(this._logPath, JSON.stringify(entry) + "\n", "utf-8");
    } catch {
      // Silent — the monitor must keep running even if audit fails
    }
  }

  /** Read the last N lines from the audit file. Returns raw JSONL lines. */
  tail(lines = 50): string[] {
    try {
      const content = fs.readFileSync(this._logPath, "utf-8");
      return content.trim().split("\n").filter(Boolean).slice(-lines);
    } catch {
      return [];
    }
  }

  /** Parse the last N audit entries (most recent first). */
  recentEntries(count = 50): AuditEntry[] {
    return this.tail(count)
      .map((line) => {
        try {
          return JSON.parse(line) as AuditEntry;
        } catch {
          return null;
        }
      })
      .filter((e): e is AuditEntry => e !== null)
      .reverse();
  }

  // ─── Private ───────────────────────────────────────────────────────────────

  private _ensureDir(): void {
    const dir = path.dirname(this._logPath);
    try {
      if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
      }
    } catch {
      /* workspace may be read-only — graceful no-op */
    }
  }

  private _rotateIfNeeded(): void {
    try {
      if (!fs.existsSync(this._logPath)) {
        return;
      }
      const { size } = fs.statSync(this._logPath);
      if (size < AuditLog.MAX_SIZE_BYTES) {
        return;
      }

      // Shift: .jsonl.3 (oldest) is discarded if exists, then cascade
      for (let i = AuditLog.MAX_ROTATIONS; i >= 1; i--) {
        const older = `${this._logPath}.${i}`;
        const newer = i === 1 ? this._logPath : `${this._logPath}.${i - 1}`;
        if (fs.existsSync(newer)) {
          // overwrite the older slot (or create it)
          fs.renameSync(newer, older);
        }
      }
    } catch {
      /* rotation errors are non-fatal */
    }
  }
}
