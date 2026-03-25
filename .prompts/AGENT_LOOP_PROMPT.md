# PROMPT DE INICIO — Loop de Monitoreo continuo de `.prompts/prompt_1.md`

> Copia y pega este texto completo al iniciar una sesión con el agente.

---

Ejecuta el siguiente loop de manera continua. **No termines tu respuesta ni cedas el control al usuario hasta que la condición de parada se active.**

## Condición de parada (la ÚNICA forma de salir del loop)

- Encuentras la palabra `STOP` como **última línea** del archivo `.prompts/prompt_1.md`

No hay ninguna otra condición de parada. No existen límites de ciclos, contadores ni timeouts que terminen el loop. **El único semáforo de parada es `STOP` en el archivo.**

---

## Loop — Repite indefinidamente hasta condición de parada

```
CICLO:
  1. sleep          → ejecuta: sleep <N> && echo "MONITOR TICK $(date)"
                      N empieza en 60. Si el archivo sigue con READ al leerlo,
                      duplica N cada ciclo (60 → 120 → 240 → 480...) hasta 900s máximo.
                      Cuando hay tareas nuevas, resetea N a 60.

  2. Lee el archivo → ejecuta: cat .prompts/prompt_1.md

  3. ¿Última línea es STOP?
     └── SÍ → TERMINA el loop. Informa: "Loop detenido por STOP en prompt_1.md"

  4. ¿Última línea es READ?
     └── SÍ → sin tareas nuevas → vuelve al paso 1 (con N incrementado)
     └── NO → hay tareas nuevas → continúa al paso 5

  5. Resetea N a 60
  6. Ejecuta TODAS las tareas del archivo según la FASE indicada:
     ├── Fase AUDIT   → audita con Chrome (Playwright MCP), documenta hallazgos, marca [x]
     ├── Fase FIX     → corrige bugs en código fuente, ejecuta Gate Pre-Commit
     └── Fase REAUDIT → verifica fixes con Chrome, documenta resultados, marca [x]

  7. Ejecuta Gate Pre-Commit (OBLIGATORIO antes de commit — solo en fase FIX):
     dotnet restore
     dotnet build /p:TreatWarningsAsErrors=true
     cd frontend/web-next && pnpm lint && pnpm typecheck && CI=true pnpm test -- --run && pnpm build
     dotnet test --no-build --blame-hang --blame-hang-timeout 2min 2>&1 | grep -E "(Passed|Failed).*\.dll"

  8. Si Gate pasa → git add . && git commit -m "..." && git push origin main
     (Fases AUDIT y REAUDIT también hacen commit de los hallazgos documentados)

  9. Verifica CI/CD post-push:
     gh run list --workflow=smart-cicd.yml --limit=1 --json status,conclusion,createdAt
     - conclusion=success → OK, continúa
     - status=in_progress → sleep 30 y repite la consulta
     - conclusion=failure → revisa logs, corrige, vuelve al Gate

     Monitorea este procesos hasta que el ci/cd termine completamente. Y luego pasa al paso siguiente, el paso 10.

 10. Despues que el ci/cd termine por completo, Escribe READ como última línea de .prompts/prompt_1.md:
     echo "READ" >> .prompts/prompt_1.md

 11. Vuelve al paso 1
```

---

## Reglas CRÍTICAS de continuidad

⛔ **NUNCA** termines tu turno de respuesta después de completar un ciclo.
⛔ **NUNCA** escribas frases como "Now monitoring...", "Continuaré monitoreando..." ni "Estaré atento a cambios..." como cierre — esas frases indican que ibas a parar. Cancélalas y ejecuta el paso 1 directamente.
⛔ **NUNCA** hagas commit sin pasar los 8 pasos del Gate Pre-Commit (paso 7).
⛔ **NUNCA** inicies una auditoría nueva si el CI/CD del commit anterior aún no terminó.
⛔ **NUNCA** termines el loop por contador, por timeout ni por decisión propia. Solo `STOP` en el archivo lo termina.

✅ **SIEMPRE** después de escribir `READ`, ejecuta inmediatamente el sleep del paso 1 — sin pausar ni preguntar nada.
✅ **SIEMPRE** que detectes un cambio (sin READ al final), ejecuta las tareas completas antes de volver al sleep.
✅ **SIEMPRE** usa Chrome (Playwright MCP) como humano real para las fases AUDIT y REAUDIT.

---

## Estado interno del loop (no mostrar al usuario)

```
ciclo_actual: N
sleep_actual: N segundos (60 inicial, se duplica sin tareas, max 900). Cada ves que encuentra una nueva tarea el sleep_actual se resetea a 60 segundos.
ultimo_estado: READ | TAREAS_PENDIENTES | STOP
```

---

## Inicio

Empieza AHORA mismo con el paso 1 del loop (sleep 60). No me preguntes nada.
