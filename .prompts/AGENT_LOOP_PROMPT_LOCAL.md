# PROMPT DE INICIO — Loop de Monitoreo continuo

> Copia y pega este texto completo al iniciar una sesión con el agente.

---

Ejecuta el siguiente loop de manera continua. **No termines tu respuesta ni cedas el control al usuario hasta que la condición de parada se active.**

NOTA IMPORTANTE: EL PROMPT QUE TE GOBIERNA ESTES, ".prompts/AGENT_LOOP_PROMPT.md" Y AUNQUE REALICES LAS TAREAS DE `.prompts/prompt_1.md`, EL PROMPT QUE TE GOBIERNA ES ESTE ".prompts/AGENT_LOOP_PROMPT.md".

## Condición de parada (la ÚNICA forma de salir del loop)

- Encuentras la palabra `STOP` como **última línea** del archivo `.prompts/prompt_1.md`

No hay ninguna otra condición de parada. No existen límites de ciclos, contadores ni timeouts que terminen el loop. **El único semáforo de parada es `STOP` en el archivo.**

> ⚠️ **Regla de oro de errores:** Si CUALQUIER herramienta falla (lectura de archivo, git, dotnet, gh, pnpm, sleep), documenta el error en un comentario breve, espera 30 segundos y reintenta. **NUNCA interpretes un error de herramienta como señal de parar el loop.** El loop solo termina con `STOP`.

---

## Loop — Repite indefinidamente hasta condición de parada

```
CICLO:
  1. sleep          → ejecuta: sleep <N> && echo "MONITOR TICK $(date)"
                      N empieza en 60. Si el archivo sigue con READ al leerlo,
                      duplica N cada ciclo (60 → 120 → 240 → 480...) hasta 900s máximo.
                      Cuando hay tareas nuevas, resetea N a 60.

  2. Lee el archivo → ejecuta: cat .prompts/prompt_1.md
     Si el archivo no existe o está vacío → trátalo como READ → vuelve al paso 1.
     Si `cat` falla por cualquier motivo → espera 30s, reintenta. NUNCA pares.

  3. ¿Última línea es STOP?
     └── SÍ → TERMINA el loop. Informa: "Loop detenido por STOP en prompt_1.md"
     └── NO → continúa al paso 4

  4. ¿Última línea es READ?
     └── SÍ  → estado: SIN_TAREAS → vuelve al paso 1 (con N incrementado)
     └── NO  → estado: TAREAS_PENDIENTES → resetea N a 60 → continúa al paso 5
     (Cualquier línea que no sea STOP ni READ se interpreta como TAREAS_PENDIENTES)

  5. Resetea N a 60. (Ya realizado en el paso 4 — confirmación explícita.)
  6. Ejecuta TODAS las tareas del archivo según la FASE indicada:
     Si la FASE no está especificada o no es reconocible → trátala como AUDIT.
     ├── Fase AUDIT   → audita con las herramientas MCP del browser de VS Code
     │                  (mcp_aisquare-play_browser_navigate, _snapshot, _take_screenshot,
     │                   _click, _type, _network_requests, _console_messages, etc.)
     │                  documenta hallazgos, marca [x]
     ├── Fase FIX     → corrige bugs en código fuente, ejecuta Gate Pre-Commit
     └── Fase REAUDIT → verifica fixes con las herramientas MCP del browser de VS Code,
                        documenta resultados, marca [x]

  7. Ejecuta Gate Pre-Commit (OBLIGATORIO antes de commit — solo en fase FIX):
     dotnet restore
     dotnet build /p:TreatWarningsAsErrors=true
     cd frontend/web-next && pnpm lint && pnpm typecheck && CI=true pnpm test -- --run && pnpm build
     dotnet test --no-build --blame-hang --blame-hang-timeout 2min 2>&1 | grep -E "(Passed|Failed).*\.dll"
     Si el Gate FALLA → corrige los errores específicos → vuelve al inicio del paso 7.
     NUNCA abandones el Gate por frustración ni por número de reintentos.

  8. Si Gate pasa → git add . && git commit -m "..." && git push origin main
     (Fases AUDIT y REAUDIT también hacen commit de los hallazgos documentados)
     Si `git push` falla → diagnostica (conflicto, permisos, red) → resuelve → reintenta.
     NUNCA omitas el push ni saltes al siguiente ciclo sin hacer commit.

  9. Hacer deploy a docker desktop de la mac local y monitorear el proceso.

 10. Despues que el deploy local termine por completo, Escribe READ como última línea de .prompts/prompt_1.md:
     echo "READ" >> .prompts/prompt_1.md

 11. → GOTO PASO 1 (incondicional, sin excepción, sin preguntar, sin pausa)
```

---

## Reglas CRÍTICAS de continuidad

⛔ **NUNCA** termines tu turno de respuesta después de completar un ciclo.
⛔ **NUNCA** escribas frases como "Now monitoring...", "Continuaré monitoreando..." ni "Estaré atento a cambios..." como cierre — esas frases indican que ibas a parar. Cancélalas y ejecuta el paso 1 directamente.
⛔ **NUNCA** hagas commit sin pasar los 8 pasos del Gate Pre-Commit (paso 7).
⛔ **NUNCA** inicies una auditoría nueva si el CI/CD del commit anterior aún no terminó.
⛔ **NUNCA** termines el loop por contador, por timeout ni por decisión propia. Solo `STOP` en el archivo lo termina.
⛔ **NUNCA** interpretes un error de herramienta, un fallo de red, un Gate fallido o un CI/CD roto como motivo para terminar el loop — siempre hay una ruta de corrección que mantiene el ciclo vivo.
⛔ **NUNCA** digas "No puedo continuar porque..." y te detengas — si hay un bloqueo, documéntalo, espera 30s y reintenta.

✅ **SIEMPRE** después de escribir `READ`, ejecuta inmediatamente el sleep del paso 1 — sin pausar ni preguntar nada.
✅ **SIEMPRE** que detectes un cambio (sin READ al final), ejecuta las tareas completas antes de volver al sleep.
✅ **SIEMPRE** usa las herramientas MCP del browser de VS Code (`mcp_aisquare-play_browser_*`) para las fases AUDIT y REAUDIT — NUNCA abras Chrome como aplicación GUI ni uses AppleScript para controlar el browser.

**Herramientas MCP disponibles (usar en orden lógico):**

- `mcp_aisquare-play_browser_navigate` → navegar a una URL
- `mcp_aisquare-play_browser_snapshot` → obtener snapshot del DOM/accesibilidad (preferir sobre screenshot para verificar contenido)
- `mcp_aisquare-play_browser_take_screenshot` → screenshot visual para documentar
- `mcp_aisquare-play_browser_click` → hacer click en un elemento
- `mcp_aisquare-play_browser_type` → escribir texto en un campo
- `mcp_aisquare-play_browser_select_option` → seleccionar un dropdown
- `mcp_aisquare-play_browser_network_requests` → capturar requests de red
- `mcp_aisquare-play_browser_console_messages` → capturar errores de consola JS
- `mcp_aisquare-play_browser_press_key` → enviar teclas (Enter, Tab, Escape, etc.)
- `mcp_aisquare-play_browser_wait_for` → esperar que aparezca un elemento

---

## Estado interno del loop

> Trackea internamente. **NO imprimas este bloque en ningún mensaje al usuario** — hacerlo crea la ilusión de un turno terminado.

```
ciclo_actual: N
sleep_actual: N segundos (60 inicial, se duplica sin tareas, max 900). Cada vez que encuentra una nueva tarea el sleep_actual se resetea a 60 segundos.
ultimo_estado: SIN_TAREAS | TAREAS_PENDIENTES | STOP
```

---

## Inicio

No respondas este mensaje. No confirmes que lo recibiste. No saludes. Ejecuta AHORA el paso 1 directamente (sleep 60 → cat → evalúa → actúa → repite).
