# 🎯 PROMPTS E ÍNDICE DE AUDITORÍA — OKLA Featured Vehicles (Backend + Frontend)

**Proyecto:** OKLA Cardealer Microservices  
**Objetivo:** Implementación completa de secciones "⭐ Destacado" y "💎 Premium" en homepage  
**Estado:** ✅ Backend implementado (CI: verde) → 🔄 Frontend auditoría en progreso  
**Fecha actualización:** 2026-02-23

---

## 📊 MATRIZ GLOBAL DE DOCUMENTOS

### 🔴 BACKEND (Completado - CI Verde)

| Documento                                                                                      | Propósito                                                                                                  | Audiencia                    | Estado        |
| ---------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- | ---------------------------- | ------------- |
| [REPORT_IMPLEMENTACION_DESTACADOS_20260223.md](./REPORT_IMPLEMENTACION_DESTACADOS_20260223.md) | Reporte completo de implementación backend: 4 GAPs resueltos, 3 bugs corregidos, checklist de arquitectura | Desarrolladores, Arquitectos | ✅ COMPLETADO |
| [.github/copilot-instructions.md](./.github/copilot-instructions.md)                           | Guía OKLA para Copilot: stack, reglas de nombrado, estructura de proyectos, health checks críticos         | Todos los devs               | ✅ REFERENCIA |

**Microservicios implementados:**

- ✅ AdvertisingService (rotación, campañas, tracking)
- ✅ VehiclesSaleService (enriquecimiento de datos en rotación)
- ✅ BillingService (integración de pagos)
- ✅ CI/CD: 13/14 servicios pasando (AuthService solo test pre-existentes pendientes)

---

### 🟡 FRONTEND (En Auditoría)

| Documento                                                                                                                                  | Propósito                                                                                                                                                | Audiencia                    | Acción           |
| ------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------- | ---------------- |
| **[PROMPT_QA_FRONTEND_DESTACADOS.md](./PROMPT_QA_FRONTEND_DESTACADOS.md)**                                                                 | **Auditoría completa de UI:** 9 pasos, checklist de verificación (renderizado, badges, tracking, responsive, accesibilidad), referencias de bugs comunes | QA Frontend, Desarrolladores | 🔄 INICIAR AHORA |
| **[QUICKSTART_QA_FRONTEND.md](./QUICKSTART_QA_FRONTEND.md)**                                                                               | **Setup en 5 minutos:** checklist visual rápida, debugging tips, casos de uso comunes                                                                    | Todos                        | 🔄 LEER PRIMERO  |
| [frontend/web-next/src/components/advertising/featured-vehicles.tsx](./frontend/web-next/src/components/advertising/featured-vehicles.tsx) | Componente React que renderiza tarjetas destacadas/premium                                                                                               | Frontend devs                | 📖 REFERENCIA    |
| [frontend/web-next/src/hooks/use-advertising.ts](./frontend/web-next/src/hooks/use-advertising.ts)                                         | Hooks TanStack Query para datos de rotación y tracking                                                                                                   | Frontend devs                | 📖 REFERENCIA    |
| [frontend/web-next/src/services/advertising.ts](./frontend/web-next/src/services/advertising.ts)                                           | Cliente HTTP para AdvertisingService                                                                                                                     | Frontend devs                | 📖 REFERENCIA    |

---

## 🚀 CÓMO USAR ESTE ÍNDICE

### Escenario 1: "Soy QA y necesito auditar el frontend"

```
1. 📖 LEE: QUICKSTART_QA_FRONTEND.md (5 min)
2. ⚡ HACES: Verificación rápida (10 min)
3. 📋 SI TODO OK: Procede a auditoría completa
4. 🔍 EJECUTAS: PROMPT_QA_FRONTEND_DESTACADOS.md (2-3 horas)
5. 📸 GENERAS: REVISION_FRONTEND_DESTACADOS_YYYYMMDD.md con capturas
```

### Escenario 2: "Soy desarrollador y encontré un bug en el frontend"

```
1. 🐛 DOCUMENTA: Bug en QUICKSTART_QA_FRONTEND.md → sección "Debugging rápido"
2. 🔧 DEBUGGEA: Sigue los comandos JavaScript en Console
3. 📍 LOCALIZA: Verifica en qué archivo está el bug (featured-vehicles.tsx, use-advertising.ts, etc.)
4. ✏️ CORRIGE: Implementa el fix
5. 🧪 TESTA: Re-ejecuta checklist del QUICKSTART
6. 🎁 ENTREGA: Abre PR con descripción clara (plantilla en PROMPT_QA_FRONTEND_DESTACADOS.md)
```

### Escenario 3: "Necesito setup de CI/CD o comandos backend"

```
1. 📖 CONSULTA: REPORT_IMPLEMENTACION_DESTACADOS_20260223.md
2. 📚 REFERENCIAS: .github/copilot-instructions.md
3. 🔗 CI/CD: Ver commits realizados y workflows en .github/workflows/
```

### Escenario 4: "Quiero todo implementado automáticamente"

```
⚠️ Este no es un prompt de ejecución automática.
Es una auditoría manual + documentación de verificación.
Para cambios = lee PROMPT_QA_FRONTEND_DESTACADOS.md PASO 2-3 (análisis de código).
```

---

## ✅ CHECKLIST DE PROGRESO GLOBAL

### Backend ✅ (Completado)

- [x] **GAP-001:** Endpoint rotación con datos enriquecidos
- [x] **GAP-002:** Tracking de impresiones
- [x] **GAP-003:** Tracking de clicks
- [x] **GAP-004:** Sincronización de campañas con vehículos
- [x] **BUG-001:** JWT SigningKey (fixed)
- [x] **BUG-002:** RabbitMQ skip en tests
- [x] **BUG-003:** Test assertions (2FA handlers)
- [x] **CI/CD:** 13/14 servicios pasando (4 commits fix + 1 report)
- [x] **Documentación:** REPORT_IMPLEMENTACION_DESTACADOS_20260223.md

**Estado:** ✅ LISTO PARA QA FRONTEND

---

### Frontend 🔄 (En Progreso)

- [ ] **PASO 0:** Preparación entorno (pnpm install + pnpm dev)
- [ ] **PASO 1:** Verificación visual en homepage
- [ ] **PASO 2:** Análisis de componente featured-vehicles.tsx
- [ ] **PASO 3:** Análisis de hooks y servicios
- [ ] **PASO 4:** Testing con datos mock
- [ ] **PASO 5:** Validación de casos límite
- [ ] **PASO 6:** Testing responsive (375px, 768px, 1280px+)
- [ ] **PASO 7:** Validación de tracking (impresiones + clicks)
- [ ] **PASO 8:** Accesibilidad (axe DevTools, opcional)
- [ ] **PASO 9:** Validación E2E manual
- [ ] **ENTREGA:** Screenshots + informe + PR

**Estado:** 🟡 PENDIENTE DE INICIO

---

## 📂 ESTRUCTURA DE ARCHIVOS AUDITABLES

```
cardealer-microservices/
│
├── 📖 PROMPTS & DOCUMENTACIÓN (TÚ ESTÁS AQUÍ)
│   ├── PROMPT_QA_FRONTEND_DESTACADOS.md        ⭐ AUDITORÍA COMPLETA
│   ├── QUICKSTART_QA_FRONTEND.md               ⭐ QUICK START (5 min)
│   ├── REPORT_IMPLEMENTACION_DESTACADOS_20260223.md   (backend, ya hecho)
│   └── INDEX_AUDITORÍA_COMPLETA.md             (este archivo)
│
├── backend/
│   ├── AdvertisingService/                     ✅ Implementado
│   │   ├── AdvertisingService.Api/
│   │   │   └── Controllers/
│   │   │       ├── RotationController.cs        (GET /api/advertising/rotation/{section})
│   │   │       └── TrackingController.cs        (POST /api/advertising/tracking/*)
│   │   └── AdvertisingService.Infrastructure/
│   │       └── Messaging/Consumers/
│   │           └── BillingPaymentCompletedConsumer.cs
│   │
│   ├── VehiclesSaleService/                    ✅ Implementado
│   │   └── Manejadores de eventos de vehículos con enriquecimiento
│   │
│   └── BillingService/                         ✅ Implementado
│       └── Procesamiento de pagos para campañas
│
├── frontend/web-next/
│   ├── src/components/advertising/
│   │   └── featured-vehicles.tsx               🔍 REVISAR (PASO 2)
│   │
│   ├── src/hooks/
│   │   └── use-advertising.ts                  🔍 REVISAR (PASO 3)
│   │
│   ├── src/services/
│   │   └── advertising.ts                      🔍 REVISAR (PASO 3)
│   │
│   ├── src/types/
│   │   └── advertising.ts                      🔍 REVISAR (PASO 3)
│   │
│   └── QA_SCREENSHOTS/                         📸 AQUÍ GUARDAN SCREENSHOTS
│       ├── 01_homepage_overview.png
│       ├── 02_console_clean.png
│       └── ... (8 total esperadas)
│
└── .github/workflows/
    ├── ci-cd.yml                               (CI Backend)
    └── _reusable-dotnet-service.yml            (reusable para servicios)
```

---

## 🎯 CÓMO EMPEZAR EN 2 MINUTOS

### Opción A: Quick Audit (30 minutos)

```bash
# 1. Terminal
cd ~/Developer/Web/Backend/cardealer-microservices/frontend/web-next
pnpm dev

# 2. Navegador: http://localhost:3000
# 3. DevTools: F12
# 4. Chequea QUICKSTART_QA_FRONTEND.md

# Resultado: "✅ VERDE" o "❌ ROJO"
```

### Opción B: Full Audit (2-3 horas)

```bash
# 1. Lee QUICKSTART_QA_FRONTEND.md (5 min)
# 2. Si verde, procede a PROMPT_QA_FRONTEND_DESTACADOS.md
# 3. Ejecuta PASO 0 → PASO 9
# 4. Genera REVISION_FRONTEND_DESTACADOS_YYYYMMDD.md
# 5. Abre PR (si hay bugs + fixes)
```

---

## 💡 MAPEO: "¿Qué documento necesito?"

| Necesidad                       | Documento                                                       |
| ------------------------------- | --------------------------------------------------------------- |
| Setup en 5 min                  | QUICKSTART_QA_FRONTEND.md                                       |
| Auditoría completa              | PROMPT_QA_FRONTEND_DESTACADOS.md                                |
| Ver qué se implementó (backend) | REPORT_IMPLEMENTACION_DESTACADOS_20260223.md                    |
| Reglas del proyecto             | .github/copilot-instructions.md                                 |
| Código a revisar                | featured-vehicles.tsx (línea 1-200)                             |
| Datos esperados                 | advertising.ts (types) + use-advertising.ts (hooks)             |
| Bug reference                   | PROMPT_QA_FRONTEND_DESTACADOS.md sección "POSIBLES BUGS"        |
| Final report template           | PROMPT_QA_FRONTEND_DESTACADOS.md sección "PLANTILLA DE INFORME" |

---

## 🔗 COMMITS RELACIONADOS

| Hash       | Mensaje                                          | Fecha      |
| ---------- | ------------------------------------------------ | ---------- |
| `64f5abe0` | fix(ci): resolve remaining test failures         | 2026-02-23 |
| `a4775b16` | fix(ci): align MediaService.Workers DI           | 2026-02-23 |
| `3adb74f2` | fix(ci): remove orphaned NestedProjects GUIDs    | 2026-02-23 |
| `e6a55cf6` | fix(ci): resolve pre-existing test failures      | 2026-02-23 |
| `766217e7` | fix(ci): re2-safe gitleaks + RabbitMQ-optional   | 2026-02-23 |
| `0ea376f4` | docs(qa): add frontend QA audit prompts          | 2026-02-23 |
| `e9714a5b` | feat(homepage): enrich rotation + premium badges | 2026-02-22 |

**PR Relacionado:** [#4 - feat: homepage featured vehicles enrichment](https://github.com/gregorymorenoiem/cardealer-microservices/pull/4)

---

## 📞 PREGUNTAS FRECUENTES

### P: ¿Cuánto tiempo tarda la auditoría?

**R:**

- Quick check: 30 min
- Full audit: 2-3 horas (con screenshots y informe)

### P: ¿Necesito conocer de backend?

**R:** No. Este prompt es **solo frontend**. Backend ya está hecho (CI verde).

### P: ¿Qué pasa si encuentro un bug?

**R:**

1. Documenta con screenshot
2. Sigue "Debugging rápido" en QUICKSTART_QA_FRONTEND.md
3. Abre issue en GitHub con etiqueta `frontend-bug`
4. O corrige tú mismo si eres dev

### P: ¿Puedo testear sin backend corriendo?

**R:** Sí, crea mock en `use-advertising.ts` (ver PASO 4 del prompt principal).

### P: ¿Necesito axe DevTools para accesibilidad?

**R:** Opcional. Solo si quieres validación profunda.

---

## 🎓 STACK DE REFERENCIA

| Componente     | Versión         | Docs                              |
| -------------- | --------------- | --------------------------------- |
| Next.js        | 16 (App Router) | https://nextjs.org/docs           |
| TanStack Query | v5              | https://tanstack.com/query/latest |
| shadcn/ui      | Latest          | https://ui.shadcn.com/            |
| Tailwind CSS   | v4              | https://tailwindcss.com/docs      |
| TypeScript     | 5.x             | https://www.typescriptlang.org/   |
| pnpm           | 9.x             | https://pnpm.io/es/               |

---

## ✨ PRÓXIMO PASO

**👉 Abre:** [QUICKSTART_QA_FRONTEND.md](./QUICKSTART_QA_FRONTEND.md)

**⏱️ Tiempo:** 5 minutos de setup

**🎯 Objetivo:** Verificación rápida de que todo funciona

---

_Índice actualizado: 2026-02-23 | Versión: 1.0_  
_Para cambios o preguntas → GitHub Issues con etiqueta `qa-frontend`_
