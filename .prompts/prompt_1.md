# CORRECCIÓN (Intento 2/3) — Sprint 7: Flujo Completo del Dealer

**Fecha:** 2026-03-25 18:44:31
**Fase:** FIX
**Usuario:** Dealer (nmateo@okla.com.do / Dealer2026!@#)
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

| Rol                 | Email                  | Password       |
| ------------------- | ---------------------- | -------------- |
| Admin               | admin@okla.local       | Admin123!@#    |
| Buyer               | buyer002@okla-test.com | BuyerTest2026! |
| Dealer              | nmateo@okla.com.do     | Dealer2026!@#  |
| Vendedor Particular | gmoreno@okla.com.do    | $Gregory1      |

---

## TAREAS

- [x] Fix bugs de S7-T01: Proceso: Dealer accede a dashboard y revisa inventario
- [x] Fix bugs de S7-T02: Proceso: Dealer revisa suscripción y planes
- [x] Fix bugs de S7-T03: Proceso: Dealer publica y gestiona vehículos
- [x] Fix bugs de S7-T04: Proceso: Dealer verifica página pública

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)

## Resultado

- Sprint: 7 — Flujo Completo del Dealer
- Fase: FIX
- Estado: COMPLETADO
- Bugs encontrados y corregidos:
  - BUG-S7-001 (CRÍTICO): Middleware redirect usa 307 (preserva POST) → dealer login falla con 404. Fix: usar 303 para POST.
  - BUG-S7-002 (ALTO): CSP connect-src falta https://www.google.com para Google Ads conversion tracking. Fix: agregado.
  - BUG-S7-003 (CRÍTICO): PlanFeatureLimits.GetLimits() falta cases para 'starter' y 'enterprise' → caen a LibreLimits. Fix: agregados.
  - BUG-S7-004 (ALTO): Precios Pro y Elite en PlanFeatureLimits no coinciden con PlanConfiguration ($89→$99, $199→$349). Fix: sincronizados.
  - BUG-S7-005 (ALTO): Gateway ClockSkew=0 vs AuthService=5min → tokens válidos rechazados intermitentemente. Fix: Gateway ahora usa 5min.

---

_Cuando termines, agrega la palabra READ al final de este archivo._
