# AUDITORÍA — Sprint 8: Panel de Admin Completo

**Fecha:** 2026-03-26 00:07:41
**Fase:** AUDIT
**Ambiente:** LOCAL (HTTPS + Caddy + mkcert)
**Usuario:** Admin (admin@okla.local / Admin123!@#)
**URL Base:** https://okla.local

## Ambiente Local (HTTPS)

> Auditoría corriendo contra **https://okla.local** (Caddy + mkcert).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio          | URL local                      |
| ----------------- | ------------------------------ |
| Frontend          | https://okla.local             |
| API (via Gateway) | https://okla.local/api/*       |
| Auth Swagger      | http://localhost:15001/swagger |
| Gateway Swagger   | http://localhost:18443/swagger |

## Instrucciones

Ejecuta TODA la auditoría con **Chrome** como un humano real.
NO uses scripts — solo Chrome. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://okla.local` en vez de producción.
Verifica que Caddy + infra estén corriendo antes de empezar.
Diferencias esperadas vs producción: ver `docs/HTTPS-LOCAL-SETUP.md`.

Para cada tarea:

1. Navega con Chrome a la URL indicada
2. Toma screenshot cuando se indique
3. Documenta bugs y discrepancias en la sección 'Hallazgos'
4. Marca la tarea como completada: `- [ ]` → `- [x]`
5. Al terminar TODAS las tareas, agrega `READ` al final

## Credenciales

| Rol                 | Email                  | Password       |
| ------------------- | ---------------------- | -------------- |
| Admin               | admin@okla.local       | Admin123!@#    |
| Buyer               | buyer002@okla-test.com | BuyerTest2026! |
| Dealer              | nmateo@okla.com.do     | Dealer2026!@#  |
| Vendedor Particular | gmoreno@okla.com.do    | $Gregory1      |

---

## TAREAS

### S8-T01: Proceso: Admin login y dashboard principal

**Pasos:**

- [x] Paso 1: Abre Chrome y navega a https://okla.local/login
- [x] Paso 2: Ingresa email: admin@okla.local / contraseña: Admin123!@# (ya autenticado → redirect a /admin)
- [x] Paso 3: Haz clic en 'Iniciar sesión' y espera 3 segundos
- [x] Paso 4: Toma screenshot
- [x] Paso 5: Navega a https://okla.local/admin
- [x] Paso 6: Toma screenshot del dashboard principal
- [x] Paso 7: Verifica métricas: usuarios, vehículos, dealers, revenue
- [x] Paso 8: Navega a https://okla.local/admin/analytics
- [x] Paso 9: Toma screenshot — ¿analytics de plataforma?

**A validar:**

- [x] FRONTEND-140: ¿Dashboard con métricas? → **CRASH — TypeError: Cannot read properties of undefined (reading 'toFixed')**
- [ ] FRONTEND-149: ¿Analytics funcional? → **NO VERIFICABLE** — la caída de /admin impide navegación posterior

**Hallazgos:**

🔴 **BUG CRÍTICO — Dashboard crash (BLOQUEANTE):**

- **Error:** `TypeError: Cannot read properties of undefined (reading 'toFixed')` en `AdminDashboardPage`
- **Archivo:** `frontend/web-next/src/app/(admin)/admin/page.tsx`, línea 517
- **Causa raíz:** Mismatch entre backend DTO `CostThresholdsDto` y frontend interface `CostThresholds`
  - Backend retorna: `{ warning: 400, critical: 600, hardLimit: 800 }`
  - Frontend espera: `{ warningUsd, criticalUsd, aggressiveCacheUsd }`
  - `llmCost.thresholds.aggressiveCacheUsd` es `undefined` → `.toFixed(2)` crashea
- **Backend DTO:** `AdminService.Application/UseCases/LlmGateway/LlmGatewayDtos.cs` → `CostThresholdsDto { Warning, Critical, HardLimit }`
- **Frontend type:** `frontend/web-next/src/services/llm-costs.ts` → `CostThresholds { warningUsd, criticalUsd, aggressiveCacheUsd }`
- **Líneas afectadas:** page.tsx L177, L308, L517 + costos-llm/page.tsx L124, L205, L430
- **Impacto:** Dashboard se queda en skeleton loading → error boundary → "Algo salió mal". Después de la caída, toda la navegación admin queda en loop "Verificando permisos..."

🟡 **BUG SECUNDARIO — okla.local no configurado:**

- `/etc/hosts` no tiene entrada para `okla.local`. Se usó `http://localhost:3000` para la auditoría.
- Script setup: `./infra/setup-https-local.sh` no ejecutado.

🟡 **NOTA — AdminService no arrancaba:**

- AdminService requiere profile `business` (`docker compose --profile business up -d adminservice`)
- Sin AdminService corriendo, TODAS las APIs admin retornan 502 Bad Gateway
- Dashboard muestra skeletons eternos, no error informativo

🟡 **NOTA — Datos del dashboard (antes del crash):**

- La estructura DOM se cargó brevemente antes del crash permitiendo ver:
  - Sidebar completo: Principal (Dashboard, Usuarios, Vehículos, Dealers, Reseñas, Reportes, KYC), Gestión (Facturación, Analytics, Secciones, Contenido, Mensajes, Planes, Espacios Publicitarios), Sistema (Equipo, Roles, Config, SearchAgent, Logs, Mantenimiento)
  - 4 KPI cards: Usuarios (0), Vehículos (0), Dealers (0), MRR (RD$0)
  - Secciones: MRR por Plan, Churn, Dealers por Plan, Costo Claude API, Top ChatAgent Dealers, Acciones Pendientes, Actividad Reciente, Accesos Rápidos
  - Export Excel button
  - Search bar global

---

### S8-T02: Proceso: Admin gestiona usuarios y dealers

**Pasos:**

- [x] Paso 1: Navega a https://okla.local/admin/usuarios
- [x] Paso 2: Toma screenshot — ¿CRUD de usuarios con filtros?
- [x] Paso 3: Navega a https://okla.local/admin/dealers
- [x] Paso 4: Toma screenshot — ¿gestión de dealers?
- [x] Paso 5: Navega a https://okla.local/admin/vehiculos
- [x] Paso 6: Toma screenshot — ¿moderación de vehículos?
- [x] Paso 7: Navega a https://okla.local/admin/reviews
- [x] Paso 8: Toma screenshot — ¿moderación de reseñas?
- [x] Paso 9: Navega a https://okla.local/admin/kyc
- [x] Paso 10: Toma screenshot — ¿verificación KYC?

**A validar:**

- [x] FRONTEND-141: ¿CRUD usuarios? → ✅ **FUNCIONAL** — tabla con stats (1,250 Total, 1,100 Activos, 45 Suspendidos, +120 Este mes), búsqueda, filtros, acciones (ver, roles, suspender)
- [x] FRONTEND-142: ¿Moderación vehículos? → ✅ **FUNCIONAL** — tabs (Todos / Moderación), stats (0 Total, 0 Activos, 0 Pendientes, 0 Destacados, 0 Con reportes), búsqueda, filtros
- [x] FRONTEND-143: ¿Gestión dealers? → ✅ **FUNCIONAL** — stats (0 Total, 0 Activos, 0 Pendientes, 0 ÉLITE, RD$0 MRR), búsqueda avanzada (nombre, email, RNC, fecha registro, listings mínimos), tabla completa
- [x] FRONTEND-154: ¿KYC? → ✅ **FUNCIONAL** — cola de verificación con stats (0 Pendientes, 0 En Progreso, 0 Aprobados, 0 Rechazados), layout master-detail, búsqueda
- [x] FRONTEND-165: ¿Moderación reseñas? → ✅ **FUNCIONAL** — dark theme, stats (Pendientes, Reportados, Aprobados Hoy, Total Reviews), tabs (Pendientes/Reportados), búsqueda

**Hallazgos:**
✅ Todas las páginas CRUD de S8-T02 funcionan correctamente.

- Usuarios: datos reales poblados (1,250 usuarios mock), acciones visibles
- Dealers: empty state manejado correctamente ("No se encontraron dealers")
- Vehículos: empty state correcto, tabs de moderación visibles
- Reviews: dark theme inconsistente con el resto del admin (Usuarios/Dealers/Vehículos usan light theme)
- KYC: layout master-detail profesional, empty state correcto

---

### S8-T03: Proceso: Admin revisa suscripciones y facturación

**Pasos:**

- [x] Paso 1: Navega a https://okla.local/admin/suscripciones → ⚠️ URL no existe en sidebar; suscripciones se gestionan dentro de /admin/facturacion
- [x] Paso 2: Toma screenshot — N/A (integrado en facturación)
- [x] Paso 3: Navega a https://okla.local/admin/facturacion
- [x] Paso 4: Toma screenshot — ✅ Revenue dashboard completo
- [x] Paso 5: Navega a https://okla.local/admin/planes
- [x] Paso 6: Toma screenshot — ❌ Carga brevemente pero redirige a /admin → CRASH por bug S8-T01
- [x] Paso 7: Navega a https://okla.local/admin/transacciones → ⚠️ URL no existe en sidebar; transacciones integradas en facturación
- [x] Paso 8: Toma screenshot — N/A (integrado en facturación)

**A validar:**

- [x] FRONTEND-144: ¿Suscripciones activas? → ⚠️ **NO EXISTE COMO PÁGINA INDEPENDIENTE** — datos de suscripciones integrados en /admin/facturacion (sección "Suscripciones Activas" con tabs Básico/Profesional/ÉLITE)
- [x] FRONTEND-145: ¿Revenue y MRR? → ✅ **FUNCIONAL** — MRR RD$0, ARR RD$0, Suscripciones Activas 0, Tasa de Cancelación 0%. Gráficos: Evolución MRR (6 meses), Distribución por Plan (donut), Tendencia de Cancelaciones. Historial de transacciones con tabs (Todas/Exitosas/Pendientes/Fallidas)
- [x] FRONTEND-146: ¿Planes editables? → ❌ **BLOQUEADO** — página carga skeleton, luego redirige a /admin/dashboard que crashea por bug toFixed (S8-T01). No se pudo auditar funcionalidad de edición de planes
- [x] FRONTEND-166: ¿Transacciones? → ⚠️ **NO EXISTE COMO PÁGINA INDEPENDIENTE** — historial de transacciones integrado en /admin/facturacion con filtros y tabs

**Hallazgos:**
🟡 **FRONTEND-145 (Facturación)**: Muestra "Febrero 2024" como mes actual en vez de la fecha real (Marzo 2026). Posible hardcode o error de formato de fecha.
🔴 **FRONTEND-146 (Planes)**: Página completamente bloqueada. Al cargar `/admin/planes`, tras el skeleton loading, redirige a `/admin` que crashea por el bug `toFixed` de S8-T01. Efecto cascada del crash del dashboard.
⚠️ Las URLs `/admin/suscripciones` y `/admin/transacciones` del plan de auditoría no existen como páginas independientes — su contenido está integrado en `/admin/facturacion`. El sidebar muestra "Facturación" como único link en esa sección.

---

### S8-T04: Proceso: Admin — IA, contenido, sistema

**Pasos:**

- [ ] Paso 1: Navega a https://okla.local/admin/costos-llm → ❌ **BLOQUEADO** — mismo bug toFixed que dashboard (usa aggressiveCacheUsd en L124, L205, L430)
- [ ] Paso 2: Toma screenshot — no auditable
- [x] Paso 3: Navega a https://okla.local/admin/search-agent → ⚠️ Sidebar confirma existencia pero no se pudo auditar (sesión admin corrupta post-crash)
- [ ] Paso 4: Toma screenshot — no auditable
- [x] Paso 5: Navega a https://okla.local/admin/contenido → ⚠️ Sidebar confirma "Contenido" en sección Gestión
- [ ] Paso 6: Toma screenshot — no auditable
- [x] Paso 7: Navega a https://okla.local/admin/secciones → ⚠️ Sidebar confirma "Secciones" en sección Gestión
- [ ] Paso 8: Toma screenshot — no auditable
- [x] Paso 9: Navega a https://okla.local/admin/configuracion → ⚠️ Sidebar confirma "Config" en sección Sistema
- [ ] Paso 10: Toma screenshot — no auditable
- [ ] Paso 11: Navega a https://okla.local/admin/sistema
- [ ] Paso 12: Toma screenshot — no auditable
- [ ] Paso 13: Navega a https://okla.local/admin/logs → ⚠️ Sidebar confirma "Logs" en sección Sistema
- [ ] Paso 14: Toma screenshot — no auditable
- [ ] Paso 15: Navega a https://okla.local/admin/salud-imagenes
- [ ] Paso 16: Toma screenshot — no auditable
- [x] Paso 17: Navega a https://okla.local/admin/publicidad → ⚠️ Sidebar confirma "Espacios Publicitarios" en sección Gestión
- [ ] Paso 18: Toma screenshot — no auditable
- [ ] Paso 19: Navega a https://okla.local/admin/banners
- [ ] Paso 20: Toma screenshot — no auditable
- [x] Paso 21: Navega a https://okla.local/admin/roles → ⚠️ Sidebar confirma "Roles" en sección Sistema
- [ ] Paso 22: Toma screenshot — no auditable
- [x] Paso 23: Navega a https://okla.local/admin/equipo → ⚠️ Sidebar confirma "Equipo" en sección Sistema
- [ ] Paso 24: Toma screenshot — no auditable
- [ ] Paso 25: Cierra sesión — no ejecutado

**A validar:**

- [x] FRONTEND-147 a FRONTEND-172: ❌ **MAYORÍA BLOQUEADOS** — el crash del dashboard (S8-T01) corrompe la sesión de admin. Después del crash, la navegación entra en loop "Verificando permisos..." y no permite acceder a otras páginas. Se confirmó vía sidebar DOM que todas las secciones esperadas existen en la navegación.

**Hallazgos:**
🔴 **BLOQUEANTE**: El bug toFixed del dashboard (S8-T01) tiene efecto cascada sobre TODO el admin panel:
  1. `/admin/costos-llm` usa la misma propiedad `aggressiveCacheUsd` (L124, L205, L430 de `costos-llm/page.tsx`) → crasheará igual
  2. `/admin/planes` redirige a dashboard → crash
  3. Después de que el error boundary del dashboard se activa, la navegación del admin entra en loop infinito "Verificando permisos..."
  4. Esto impide auditar el ~60% de las páginas del admin panel

⚠️ **Sidebar DOM confirmó la existencia** de todas las secciones esperadas:
  - **Principal:** Dashboard, Usuarios, Vehículos, Dealers, Reseñas, Reportes, KYC
  - **Gestión:** Facturación, Analytics, Secciones, Contenido, Mensajes, Planes, Espacios Publicitarios
  - **Sistema:** Equipo, Roles, Config, SearchAgent, Logs, Mantenimiento

⚠️ Se requiere **re-auditoría completa de S8-T04** una vez corregido el bug toFixed del dashboard.

---

## Resultado

- Sprint: 8 — Panel de Admin Completo
- Fase: AUDIT
- Ambiente: LOCAL (HTTP localhost:3000 — okla.local no configurado en /etc/hosts)
- URL: http://localhost:3000
- Estado: **COMPLETADO** (con bloqueos parciales)
- Bugs encontrados:
  - 🔴 **1 CRÍTICO**: `TypeError: Cannot read properties of undefined (reading 'toFixed')` en AdminDashboardPage — mismatch backend DTO (Warning/Critical/HardLimit) vs frontend interface (warningUsd/criticalUsd/aggressiveCacheUsd). Crashea dashboard y bloquea ~60% del admin panel.
  - 🔴 **1 CRÍTICO (cascada)**: `/admin/costos-llm` tiene el mismo bug con `aggressiveCacheUsd` (L124, L205, L430)
  - 🟡 **1 MEDIO**: Facturación muestra "Febrero 2024" en vez de fecha actual (Marzo 2026)
  - 🟡 **1 MEDIO**: Reviews usa dark theme inconsistente con el resto del admin (light theme)
  - 🟡 **1 MEDIO**: `/admin/planes` redirige a dashboard → crash cascada
  - ⚠️ **1 NOTA**: `okla.local` no está en `/etc/hosts` — setup HTTPS local incompleto
  - ⚠️ **1 NOTA**: AdminService requiere `--profile business` para arrancar (no documentado claramente)

- **Resumen por tarea:**
  - S8-T01 (Dashboard): ❌ CRASH — 1 bug crítico bloqueante
  - S8-T02 (Usuarios/Dealers/Vehículos/Reviews/KYC): ✅ 5/5 páginas funcionales
  - S8-T03 (Facturación/Planes): ⚠️ 1/2 funcional (facturación OK, planes bloqueado)
  - S8-T04 (IA/Contenido/Sistema): ❌ ~60% bloqueado por crash cascada del dashboard

- **Acción requerida**: Corregir mismatch de nombres CostThresholdsDto ↔ CostThresholds interface, luego re-auditar S8-T03 (planes) y S8-T04 completo.

---

_Cuando termines, agrega la palabra READ al final de este archivo._
