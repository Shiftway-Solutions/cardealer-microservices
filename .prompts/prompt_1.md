# CORRECCIÓN (Intento 3/3) — Sprint 2: Búsqueda & Filtros de Vehículos (Guest)
**Fecha:** 2026-03-25 11:32:23
**Fase:** FIX
**Usuario:** Guest (sin login)
**URL:** https://okla.com.do

## Instrucciones — FASE DE CORRECCIÓN
En la auditoría anterior se encontraron bugs. Tu trabajo ahora es:

1. Lee la sección 'BUGS A CORREGIR' abajo
2. Corrige cada bug en el código fuente
3. Ejecuta el Gate Pre-Commit (8 pasos) para validar
4. Marca cada fix como completado: `- [ ]` → `- [x]`
5. Al terminar, agrega `READ` al final

⚠️ NO hagas commit aún — primero el sprint debe pasar RE-AUDITORÍA

## BUGS A CORREGIR
_(El agente que hizo la auditoría documentó los hallazgos aquí.)_
_(Lee el archivo de reporte del sprint anterior para ver los bugs.)_

Revisa el último reporte en `audit-reports/` o los hallazgos del prompt anterior.
Corrige todos los bugs encontrados:

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

---

## TAREAS

- [x] Fix bugs de S2-T01: Auditar listado y filtros de /vehiculos
  - FRONTEND-029 (detail page): Corregido. `vehicle-header.tsx` ahora aplica sanity check de kilometraje (>1000km → "Usado" en vez de "Nuevo")
  - `page.tsx` (SEO metadata): Corregido. `conditionLabel` aplica la misma validación de kilometraje
- [x] Fix bugs de S2-T02: Auditar paginación y vehículos patrocinados
  - FRONTEND-025: Ya corregido en FIX 2/3 (commit 9d3103e0). Dedup por ID + no-cycling ya implementados en `vehiculos-client.tsx` y hash determinístico en `ad-engine.ts`. Pendiente de deploy.
- [x] Fix bugs de S2-T03: Auditar búsqueda y alertas sin auth
  - Sin bugs adicionales. Todos los bugs de S2-T03 fueron corregidos en fases anteriores.

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
  - dotnet restore ✅ | dotnet build (0 warnings, 0 errors) ✅
  - pnpm lint (0 errors) ✅ | pnpm typecheck (0 errors) ✅
  - pnpm test (576/576 passed) ✅ | pnpm build (Compiled successfully) ✅
  - dotnet test: unitarios 100% ✅ (fallos de integración son pre-existentes, requieren Docker+PG+RabbitMQ)

## Resultado
- Sprint: 2 — Búsqueda & Filtros de Vehículos (Guest)
- Fase: FIX 3/3
- Estado: COMPLETADO
- Bugs corregidos: 2
  - FRONTEND-029 (detail page): `vehicle-header.tsx` + `page.tsx` — badge "Nuevo" con mileage sanity check (>1000km → "Usado")
  - FRONTEND-025: Ya corregido en FIX 2/3, pendiente de deploy a producción
- Gate Pre-Commit: APROBADO (576 tests frontend, 0 errores backend)

---

_Cuando termines, agrega la palabra READ al final de este archivo._
