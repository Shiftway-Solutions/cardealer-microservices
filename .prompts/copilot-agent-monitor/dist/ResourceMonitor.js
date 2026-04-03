"use strict";
/**
 * ResourceMonitor — System resource monitoring for VS Code + Docker.
 *
 * Polls every 60s (configurable). Emits pressure events when thresholds crossed:
 *
 *   RAM WARN     → ramFreePercent < 20%  (notify, suggestions)
 *   RAM CRITICAL → ramFreePercent < 10%  (trigger RELEASE_MEMORY action)
 *   DISK WARN    → diskFreeGb    < 10 GB (notify)
 *   DISK CRITICAL→ diskFreeGb    < 3 GB  (trigger PRUNE_DOCKER_CACHE action)
 *
 * Actions this module can perform directly (no agent cycle needed):
 *   - Kill Docker containers using >500 MB each (with notification)
 *   - Prune Docker build cache (dangling images, stopped containers)
 *
 * Actions delegated to Monitor (emitted via callback):
 *   - RELEASE_MEMORY  → Monitor opens "free memory" dialog, optionally restarts VS Code
 *   - PRUNE_DOCKER_CACHE → Monitor requests docker system prune
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
exports.ResourceMonitor = void 0;
const os = __importStar(require("os"));
const child_process_1 = require("child_process");
// ─── Thresholds ───────────────────────────────────────────────────────────────
const RAM_WARN_PCT = 20; // warn when free RAM < 20%
const RAM_CRITICAL_PCT = 10; // critical when free RAM < 10%
const DISK_WARN_GB = 10; // warn when disk free < 10 GB
const DISK_CRITICAL_GB = 3; // critical when disk free < 3 GB
// Containers using more than this will be listed in RAM CRITICAL notification
const DOCKER_HEAVY_MB = 400;
class ResourceMonitor {
    _timer = null;
    _intervalMs;
    _onPressure;
    _lastSnapshot = null;
    /** Cooldown per pressure type — only fire once per 5 min to avoid notification spam */
    _lastFiredAt = {
        none: 0,
        ram_warn: 0,
        ram_critical: 0,
        disk_warn: 0,
        disk_critical: 0,
    };
    _cooldownMs = 5 * 60_000; // 5 minutes
    constructor(callback, intervalMs = 60_000) {
        this._onPressure = callback;
        this._intervalMs = intervalMs;
    }
    start() {
        // First poll immediately so the status bar shows data on startup
        void this._poll();
        this._timer = setInterval(() => void this._poll(), this._intervalMs);
    }
    stop() {
        if (this._timer) {
            clearInterval(this._timer);
            this._timer = null;
        }
    }
    get lastSnapshot() {
        return this._lastSnapshot;
    }
    /** Public one-shot capture (used by ActionExecutor for on-demand snapshots) */
    async capture() {
        return this._capture();
    }
    /** Format snapshot as a short string for the status bar */
    static format(snap) {
        const ramFree = Math.round(snap.ramFreeMb / 1024 * 10) / 10;
        const diskFree = Math.round(snap.diskFreeGb * 10) / 10;
        const docker = snap.dockerAvailable
            ? ` Docker:${Math.round(snap.dockerTotalMb / 1024 * 10) / 10}GB`
            : "";
        return `RAM:${ramFree}GB free  Disk:${diskFree}GB free${docker}`;
    }
    // ─── Core poll ─────────────────────────────────────────────────────────────
    async _poll() {
        const snap = await this._capture();
        this._lastSnapshot = snap;
        // Determine highest pressure level (critical > warn > none)
        const pressure = this._evaluate(snap);
        if (pressure !== "none") {
            const now = Date.now();
            if (now - this._lastFiredAt[pressure] >= this._cooldownMs) {
                this._lastFiredAt[pressure] = now;
                this._onPressure({
                    pressure,
                    snapshot: snap,
                    message: this._buildMessage(pressure, snap),
                });
            }
        }
    }
    _evaluate(snap) {
        if (snap.ramUsedPct >= 100 - RAM_CRITICAL_PCT)
            return "ram_critical";
        if (snap.diskFreeGb < DISK_CRITICAL_GB)
            return "disk_critical";
        if (snap.ramUsedPct >= 100 - RAM_WARN_PCT)
            return "ram_warn";
        if (snap.diskFreeGb < DISK_WARN_GB)
            return "disk_warn";
        return "none";
    }
    _buildMessage(pressure, snap) {
        switch (pressure) {
            case "ram_critical":
                return (`🚨 RAM CRÍTICA: solo ${Math.round(snap.ramFreeMb / 1024 * 10) / 10} GB libres ` +
                    `(VSCode: ${Math.round(snap.vscodeProcMb)}MB` +
                    (snap.dockerAvailable ? `, Docker: ${Math.round(snap.dockerTotalMb)}MB)` : ")"));
            case "ram_warn":
                return (`⚠️ RAM baja: ${Math.round(snap.ramFreeMb / 1024 * 10) / 10} GB libres ` +
                    `(${Math.round(100 - snap.ramUsedPct)}% disponible)`);
            case "disk_critical":
                return `🚨 DISCO CRÍTICO: solo ${snap.diskFreeGb.toFixed(1)} GB libres`;
            case "disk_warn":
                return `⚠️ Disco bajo: ${snap.diskFreeGb.toFixed(1)} GB libres`;
            default:
                return "";
        }
    }
    // ─── Capture snapshot ─────────────────────────────────────────────────────
    async _capture() {
        const totalMem = os.totalmem();
        const freeMem = os.freemem();
        const ramTotalMb = totalMem / 1_048_576;
        const ramFreeMb = freeMem / 1_048_576;
        const ramUsedPct = ((totalMem - freeMem) / totalMem) * 100;
        // VS Code process RSS (this extension runs inside VS Code)
        const vscodeProcMb = process.memoryUsage().rss / 1_048_576;
        // Disk (workspace root drive)
        const { diskFreeGb, diskTotalGb, diskUsedPct } = await this._diskStats();
        // Docker
        const { containers, available } = await this._dockerStats();
        const dockerTotalMb = containers.reduce((s, c) => s + c.memUsageMb, 0);
        return {
            ramTotalMb,
            ramFreeMb,
            ramUsedPct,
            diskTotalGb,
            diskFreeGb,
            diskUsedPct,
            vscodeProcMb,
            dockerTotalMb,
            dockerContainers: containers,
            dockerAvailable: available,
            capturedAt: Date.now(),
        };
    }
    // ─── Disk stats via `df` ──────────────────────────────────────────────────
    _diskStats() {
        return new Promise((resolve) => {
            const fallback = { diskFreeGb: 999, diskTotalGb: 999, diskUsedPct: 0 };
            (0, child_process_1.execFile)("df", ["-k", "/"], { timeout: 5_000 }, (err, stdout) => {
                if (err) {
                    resolve(fallback);
                    return;
                }
                // df -k output: Filesystem 1K-blocks Used Available Use% Mounted
                const lines = stdout.trim().split("\n");
                const dataLine = lines[lines.length - 1]; // last line (handles wrapped output)
                const parts = dataLine.split(/\s+/);
                // columns: Filesystem, 1K-blocks, Used, Available, Use%, Mounted
                const totalKb = parseInt(parts[1] ?? "0", 10);
                const availKb = parseInt(parts[3] ?? "0", 10);
                const usePct = parseInt((parts[4] ?? "0%").replace("%", ""), 10);
                if (!totalKb) {
                    resolve(fallback);
                    return;
                }
                resolve({
                    diskTotalGb: totalKb / 1_048_576,
                    diskFreeGb: availKb / 1_048_576,
                    diskUsedPct: usePct,
                });
            });
        });
    }
    // ─── Docker stats via `docker stats --no-stream` ──────────────────────────
    _dockerStats() {
        return new Promise((resolve) => {
            (0, child_process_1.execFile)("docker", ["stats", "--no-stream", "--format",
                "{{.Name}}\t{{.MemUsage}}\t{{.CPUPerc}}"], { timeout: 10_000 }, (err, stdout) => {
                if (err) {
                    resolve({ containers: [], available: false });
                    return;
                }
                const containers = [];
                for (const line of stdout.trim().split("\n")) {
                    if (!line.trim())
                        continue;
                    const [name, memUsage, cpuRaw] = line.split("\t");
                    if (!name || !memUsage)
                        continue;
                    // memUsage format: "512MiB / 16GiB"
                    const memParts = memUsage.split("/");
                    const memUsageMb = ResourceMonitor._parseMemMb(memParts[0]?.trim() ?? "0");
                    const memLimitMb = ResourceMonitor._parseMemMb(memParts[1]?.trim() ?? "0");
                    const cpuPct = parseFloat((cpuRaw ?? "0%").replace("%", "")) || 0;
                    containers.push({ name: name.trim(), memUsageMb, memLimitMb, cpuPct });
                }
                containers.sort((a, b) => b.memUsageMb - a.memUsageMb);
                resolve({ containers, available: true });
            });
        });
    }
    static _parseMemMb(value) {
        // "512MiB", "1.5GiB", "200MB", "1.2GB", "0B"
        const match = /^([0-9.]+)\s*(B|KB|KiB|MB|MiB|GB|GiB|TB|TiB)$/i.exec(value);
        if (!match)
            return 0;
        const num = parseFloat(match[1]);
        const unit = match[2].toLowerCase();
        if (unit === "b")
            return num / 1_048_576;
        if (unit.startsWith("k"))
            return num / 1024;
        if (unit.startsWith("m"))
            return num;
        if (unit.startsWith("g"))
            return num * 1024;
        if (unit.startsWith("t"))
            return num * 1_048_576;
        return 0;
    }
    // ─── Remediation helpers (called by ActionExecutor) ───────────────────────
    /**
     * Returns a formatted summary of top memory consumers for a notification.
     */
    static buildRamActionMessage(snap) {
        const lines = [
            `RAM libre: ${Math.round(snap.ramFreeMb / 1024 * 10) / 10} GB de ${Math.round(snap.ramTotalMb / 1024 * 10) / 10} GB`,
            `VS Code proceso: ${Math.round(snap.vscodeProcMb)} MB`,
        ];
        if (snap.dockerAvailable && snap.dockerContainers.length > 0) {
            const heavy = snap.dockerContainers.filter(c => c.memUsageMb > DOCKER_HEAVY_MB);
            if (heavy.length > 0) {
                lines.push("");
                lines.push("Contenedores Docker pesados:");
                for (const c of heavy.slice(0, 5)) {
                    lines.push(`  ${c.name}: ${Math.round(c.memUsageMb)} MB`);
                }
            }
        }
        return lines.join("\n");
    }
    /**
     * Run `docker system prune -f --volumes=false` to reclaim build cache.
     * Called from ActionExecutor.releaseDockerCache().
     * Only removes: stopped containers + dangling images + unused networks + build cache.
     * Does NOT remove volumes (--volumes=false is the safe default).
     */
    static pruneDockerCache() {
        return new Promise((resolve) => {
            (0, child_process_1.execFile)("docker", ["system", "prune", "-f", "--volumes=false"], { timeout: 120_000 }, (err, stdout) => {
                if (err) {
                    resolve({ ok: false, freedMb: 0, detail: `docker prune failed: ${err.message}` });
                    return;
                }
                // Parse "Total reclaimed space: 2.5GB"
                const match = /Total reclaimed space:\s*([0-9.]+)\s*(B|KB|MB|GB|TB)/i.exec(stdout);
                const freedMb = match ? ResourceMonitor._parseMemMb(`${match[1]}${match[2]}`) : 0;
                resolve({
                    ok: true,
                    freedMb,
                    detail: `Docker cache prunado — liberado: ${match ? match[0] : "desconocido"}`,
                });
            });
        });
    }
}
exports.ResourceMonitor = ResourceMonitor;
