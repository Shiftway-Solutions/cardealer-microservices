"use strict";
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
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.AuditLog = void 0;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
// ─── AuditLog ─────────────────────────────────────────────────────────────────
class AuditLog {
    static MAX_SIZE_BYTES = 512 * 1024; // 500 KB per file
    static MAX_ROTATIONS = 3; // keep .jsonl, .jsonl.1, .jsonl.2, .jsonl.3
    _logPath;
    _cycleCounter = 0;
    constructor(workspaceRoot) {
        this._logPath = path.join(workspaceRoot, ".github", "copilot-monitor-audit.jsonl");
        this._ensureDir();
    }
    /** Absolute path to the current audit file. */
    get filePath() {
        return this._logPath;
    }
    /** Returns the next cycle number for this VS Code session. */
    nextCycle() {
        return ++this._cycleCounter;
    }
    /** Append an audit entry. Never throws — audit must not crash the monitor. */
    record(entry) {
        try {
            this._rotateIfNeeded();
            fs.appendFileSync(this._logPath, JSON.stringify(entry) + "\n", "utf-8");
        }
        catch {
            // Silent — the monitor must keep running even if audit fails
        }
    }
    /** Read the last N lines from the audit file. Returns raw JSONL lines. */
    tail(lines = 50) {
        try {
            const content = fs.readFileSync(this._logPath, "utf-8");
            return content.trim().split("\n").filter(Boolean).slice(-lines);
        }
        catch {
            return [];
        }
    }
    /** Parse the last N audit entries (most recent first). */
    recentEntries(count = 50) {
        return this.tail(count)
            .map((line) => {
            try {
                return JSON.parse(line);
            }
            catch {
                return null;
            }
        })
            .filter((e) => e !== null)
            .reverse();
    }
    // ─── Private ───────────────────────────────────────────────────────────────
    _ensureDir() {
        const dir = path.dirname(this._logPath);
        try {
            if (!fs.existsSync(dir)) {
                fs.mkdirSync(dir, { recursive: true });
            }
        }
        catch {
            /* workspace may be read-only — graceful no-op */
        }
    }
    _rotateIfNeeded() {
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
        }
        catch {
            /* rotation errors are non-fatal */
        }
    }
}
exports.AuditLog = AuditLog;
