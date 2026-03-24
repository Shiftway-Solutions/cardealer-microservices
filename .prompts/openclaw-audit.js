/**
 * openclaw-audit.js
 * Conecta al gateway WebSocket de OpenClaw y envia el prompt de auditoria
 * de los Agentes IA de OKLA.
 *
 * Uso: node .prompts/openclaw-audit.js
 */

const WebSocket = require("ws");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const TOKEN = "f24fcea34191739b761db1153cefd910706083c1546c9c00";

const AUDIT_PROMPT = `Audita los Agentes IA de OKLA en produccion en https://okla.com.do.

Prueba cada agente en orden usando el browser (Chrome). Para CADA agente captura errores de consola (F12 > Console) y reporta si funciona o falla:

1. **SearchAgent** (https://okla.com.do/vehiculos)
   - Logueate como buyer002@okla-test.com / BuyerTest2026!
   - Escribe en el buscador: "Toyota Corolla 2020 automatica menos de 1 millon"
   - Verifica que el SearchAgent responde con resultados relevantes
   - Captura cualquier error de consola o red

2. **DealerChatAgent - SingleVehicle**
   - Abre cualquier vehiculo individual en https://okla.com.do/vehiculos
   - Busca el widget de chat y envia el mensaje: "Hola, me interesa este vehiculo, cual es su precio final?"
   - Verifica que el DealerChatAgent responde en espanol

3. **DealerChatAgent - DealerInventory**
   - Ve a la pagina de un dealer (https://okla.com.do/dealer)
   - Activa el chat de inventario y pregunta: "Que vehiculos tienen disponibles bajo 800 mil pesos?"
   - Verifica respuesta coherente del agente

4. **PricingAgent**
   - Ve a https://okla.com.do/dealer/pricing
   - Logueate como nmateo@okla.com.do / Dealer2026!@#
   - Verifica que la pagina de pricing carga con datos
   - Prueba obtener precio de: Toyota Corolla 2020, 50,000 km, condicion usada

5. **RecoAgent**
   - Ve a https://okla.com.do (homepage)
   - Logueate como buyer002@okla-test.com / BuyerTest2026!
   - Verifica que aparecen recomendaciones personalizadas de vehiculos
   - Captura errores de consola

6. **LLM Gateway Health**
   - Llama a https://okla.com.do/api/admin/llm-gateway/health
   - Logueate como admin@okla.local / Admin123!@# si requiere auth
   - Verifica el estado del cascade (Claude -> Gemini -> Llama)

Al terminar genera un reporte en formato markdown con:
- Estado de cada agente: OK / FALLO
- Errores de consola encontrados
- Tiempo de respuesta aproximado
- Recomendaciones de mejora
Guarda el reporte en /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/audit-reports/AI_AGENTS_AUDIT_$(date +%Y%m%d).md`;

const REPORT_DIR = path.join(__dirname, "../audit-reports");
const LOG_FILE = path.join(REPORT_DIR, `ws-audit-${Date.now()}.log`);

if (!fs.existsSync(REPORT_DIR)) fs.mkdirSync(REPORT_DIR, { recursive: true });
const logStream = fs.createWriteStream(LOG_FILE, { flags: "a" });

function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`;
  console.log(line);
  logStream.write(line + "\n");
}

const ws = new WebSocket("ws://127.0.0.1:18789");
let authenticated = false;
let responseBuffer = "";
let streamActive = false;

ws.on("open", () =>
  log("[WS] Conectado al gateway OpenClaw ws://127.0.0.1:18789"),
);

ws.on("message", (raw) => {
  let msg;
  try {
    msg = JSON.parse(raw.toString());
  } catch (e) {
    log("[WARN] Mensaje no-JSON: " + raw.toString().substring(0, 100));
    return;
  }

  const event = msg.event || msg.type || "";

  // --- Autenticacion ---
  if (event === "connect.challenge") {
    const nonce = msg.payload && msg.payload.nonce;
    log("[AUTH] Challenge recibido, nonce: " + nonce);
    // Intentar varios formatos de auth
    const hmac = crypto.createHmac("sha256", TOKEN).update(nonce).digest("hex");
    ws.send(
      JSON.stringify({
        type: "authenticate",
        payload: { token: TOKEN, hmac, nonce },
      }),
    );
  }

  if (
    event === "connect.ready" ||
    event === "authenticated" ||
    event === "auth.ok" ||
    event === "ready"
  ) {
    authenticated = true;
    log("[AUTH] Autenticado OK. Enviando prompt de auditoria...");
    ws.send(
      JSON.stringify({
        type: "message",
        payload: { text: AUDIT_PROMPT, role: "user" },
      }),
    );
  }

  // Tambien intentar enviar si llegamos a un estado idle
  if (!authenticated && event === "agent.idle") {
    authenticated = true;
    log("[AUTH] Agente idle detectado, enviando prompt...");
    ws.send(
      JSON.stringify({
        type: "message",
        payload: { text: AUDIT_PROMPT, role: "user" },
      }),
    );
  }

  // --- Streaming de respuesta ---
  if (event === "stream.delta" || event === "token" || event === "text.delta") {
    const delta =
      (msg.payload && (msg.payload.delta || msg.payload.text)) ||
      msg.delta ||
      "";
    if (delta) {
      process.stdout.write(delta);
      responseBuffer += delta;
      streamActive = true;
    }
    return; // No loguear cada token
  }

  if (
    event === "stream.end" ||
    event === "message.complete" ||
    event === "turn.end"
  ) {
    streamActive = false;
    log(
      "\n[STREAM] Respuesta completa recibida (" +
        responseBuffer.length +
        " chars)",
    );
    // Guardar respuesta
    const reportFile = path.join(
      REPORT_DIR,
      `AI_AGENTS_AUDIT_${new Date().toISOString().slice(0, 10)}.md`,
    );
    if (responseBuffer.length > 100) {
      fs.writeFileSync(reportFile, responseBuffer, "utf8");
      log("[REPORTE] Guardado en: " + reportFile);
    }
  }

  // --- Mensajes completos del agente ---
  if (event === "agent.message" || event === "message") {
    const text =
      (msg.payload && (msg.payload.text || msg.payload.content)) ||
      msg.content ||
      "";
    if (text && text.length > 0) {
      log("[AGENTE] " + text.substring(0, 300));
      responseBuffer += text;
    }
  }

  // --- Log de todos los otros eventos ---
  if (
    !["stream.delta", "token", "text.delta", "heartbeat", "ping"].includes(
      event,
    )
  ) {
    log(
      "[EVT] " +
        event +
        " " +
        JSON.stringify(msg.payload || msg).substring(0, 200),
    );
  }
});

ws.on("error", (e) => log("[ERROR] " + e.message));
ws.on("close", (code, reason) => {
  log("[WS] Conexion cerrada, code: " + code + ", reason: " + (reason || ""));
  logStream.end();
  process.exit(code === 1000 ? 0 : 1);
});

// Timeout de seguridad 5 minutos
setTimeout(() => {
  log("[TIMEOUT] 5 minutos - cerrando conexion");
  ws.close(1000);
}, 300000);

log("[START] Iniciando auditoria de Agentes IA de OKLA via OpenClaw WebSocket");
