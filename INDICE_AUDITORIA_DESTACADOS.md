# 📑 ÍNDICE: Auditoría Completa - Vehículos Destacados/Premium

**Versión:** 2.0 - Auditoría Completa  
**Fecha:** 23 de Febrero, 2026  
**Estado:** ✅ COMPLETO

---

## 🎯 ¿POR DÓNDE EMPIEZO?

### 👤 Según tu rol:

#### 🔵 **GERENTE / PRODUCT OWNER**

→ Lee: [RESUMEN_EJECUTIVO_AUDITORIA_DESTACADOS.md](RESUMEN_EJECUTIVO_AUDITORIA_DESTACADOS.md)

- ⏱️ **Tiempo:** 10-15 minutos
- 📊 **Contiene:**
  - Hallazgos principales en 1 página
  - ¿Qué existe y qué falta?
  - Estimación de esfuerzo y timeline
  - Recomendaciones inmediatas
- 🎯 **Acción:** Decidir si proceder con implementación

---

#### 🟢 **TECH LEAD / ARQUITECTO**

1. Lee: [RESUMEN_EJECUTIVO_AUDITORIA_DESTACADOS.md](RESUMEN_EJECUTIVO_AUDITORIA_DESTACADOS.md) (10 min)
2. Lee: [AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md](AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md) (30 min)
3. Referencia: [DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md](DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md) (según necesites)

- ⏱️ **Tiempo total:** 45-60 minutos
- 📊 **Aprenderás:**
  - Arquitectura actual completa
  - 7 fases del flujo de datos
  - 5 brechas críticas identificadas
  - Soluciones propuestas
- 🎯 **Acción:** Validar diseño con equipo, crear plan detallado

---

#### 🟡 **DESARROLLADOR BACKEND (.NET)**

1. Lee: [PLAN_IMPLEMENTACION_DESTACADOS.md](PLAN_IMPLEMENTACION_DESTACADOS.md) → Sección "FASE 2 y 3" (20 min)
2. Referencia: [DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md](DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md) → Nivel 1-6 (30 min)
3. Usa: [AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md](AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md) → Brechas específicas (según necesites)

- ⏱️ **Tiempo total:** 50-70 minutos
- 📊 **Aprenderás:**
  - Exactamente qué código escribir
  - Estructura de cambios necesarios
  - Dependencias y eventos
  - Tests que pasar
- 🎯 **Acción:** Empezar a implementar sprints 2-3

---

#### 🟠 **DESARROLLADOR FRONTEND (React/Next.js)**

1. Lee: [PLAN_IMPLEMENTACION_DESTACADOS.md](PLAN_IMPLEMENTACION_DESTACADOS.md) → Sección "FASE 4" (15 min)
2. Referencia: [DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md](DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md) → Nivel 4-5 (25 min)

- ⏱️ **Tiempo total:** 40-50 minutos
- 📊 **Aprenderás:**
  - Qué hooks/componentes actualizar
  - Nuevos endpoints a llamar
  - Estructura de datos esperada
  - Tests E2E a escribir
- 🎯 **Acción:** Empezar a implementar sprint 4

---

#### 🔴 **QA / TESTER**

1. Lee: [PLAN_IMPLEMENTACION_DESTACADOS.md](PLAN_IMPLEMENTACION_DESTACADOS.md) → Sección "FASE 5" (15 min)
2. Referencia: [DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md](DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md) → Casos de uso (20 min)

- ⏱️ **Tiempo total:** 35-45 minutos
- 📊 **Aprenderás:**
  - Tests E2E necesarios
  - Casos de fallo a validar
  - Flujos a probar
  - Checklist final
- 🎯 **Acción:** Escribir test plan y crear casos de test

---

## 📚 DOCUMENTOS DISPONIBLES

### 1. 🔴 **RESUMEN_EJECUTIVO_AUDITORIA_DESTACADOS.md** (⭐ LEER PRIMERO)

**Tipo:** Resumen Ejecutivo  
**Audiencia:** Gerentes, Product Owners, Decisores  
**Páginas:** ~5  
**Tiempo de lectura:** 10-15 minutos

**Contenido:**

- ✅ Hallazgos en una sola página
- ✅ Tabla de lo que existe vs. falta
- ✅ Estimación de esfuerzo (4.5 sprints)
- ✅ Timeline propuesto (8-10 semanas)
- ✅ Recomendaciones inmediatas
- ✅ Matriz de completitud (63%)
- ✅ Next steps claros

**Cuando usarlo:**

- Necesitas decidir si proceder
- Necesitas presupuestar recursos
- Necesitas reportar al management

---

### 2. 🟡 **AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md** (LECTURA PROFUNDA)

**Tipo:** Auditoría Técnica Completa  
**Audiencia:** Tech Leads, Arquitectos, Desarrolladores Senior  
**Páginas:** ~20  
**Tiempo de lectura:** 30-45 minutos

**Contenido:**

- ✅ Resumen ejecutivo de hallazgos
- ✅ Fase 1-5 del flujo completo
- ✅ 5 brechas críticas identificadas con detalles
- ✅ Checklist de verificación (5 secciones)
- ✅ Flujo completo PROPUESTO (ideal)
- ✅ 5 recomendaciones inmediatas con código
- ✅ Matriz de dependencias
- ✅ Próximos pasos

**Cuando usarlo:**

- Necesitas entender el problema profundamente
- Necesitas validar hallazgos
- Necesitas diseñar la solución
- Necesitas explicarle a otros el problema

**Secciones clave:**

```
1. Resumen ejecutivo (2 pág)
2. Fase 1: Publicación (2 pág)
3. Fase 2: Decisión de publicidad (2 pág)
4. Fase 3: Asignación a secciones (2 pág)
5. Fase 4: Frontend obtiene datos (2 pág)
6. Fase 5: Renderización (2 pág)
7. Brechas identificadas (5 pág) ← CRÍTICO
8. Checklist de verificación (2 pág)
9. Flujo propuesto (2 pág)
10. Recomendaciones (2 pág)
```

---

### 3. 🟢 **DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md** (REFERENCIA VISUAL)

**Tipo:** Diagramas Técnicos + Detalles  
**Audiencia:** Desarrolladores, Arquitectos  
**Páginas:** ~25  
**Tiempo de lectura:** 30-60 minutos (como referencia)

**Contenido:**

- ✅ Nivel 1: Arquitectura general (diagram ASCII)
- ✅ Nivel 2: Flujo de publicación (paso a paso)
- ✅ Nivel 3A: Opción destacado manual (con endpoints)
- ✅ Nivel 3B: Opción premium pagado (con payloads)
- ✅ Nivel 4: Frontend - Obtención de datos (hooks)
- ✅ Nivel 5: Renderización en pantalla (visual)
- ✅ Nivel 6: Tracking & Métricas (flujo de datos)
- ✅ Nivel 7: Schema SQL completo (con índices)
- ✅ Nivel 8: Eventos RabbitMQ (payloads)
- ✅ Bonus: Mapa de URLs backend completo

**Cuando usarlo:**

- Necesitas diagramas para presentar
- Necesitas entender un nivel específico
- Necesitas Schema SQL exacto
- Necesitas payloads de API
- Necesitas estructura de eventos

**Cómo buscar:**

```bash
# Buscar un nivel específico:
grep -n "^## Nivel" DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md

# Buscar un componente:
grep -n "FeaturedVehicles\|RotationController\|Campaign" DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md

# Buscar SQL:
grep -n "CREATE TABLE\|CREATE INDEX" DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md
```

---

### 4. 🔵 **PLAN_IMPLEMENTACION_DESTACADOS.md** (EJECUCIÓN)

**Tipo:** Plan Detallado + Checklist  
**Audiencia:** Product Managers, Scrum Masters, Desarrolladores  
**Páginas:** ~30  
**Tiempo de lectura:** 45-60 minutos (completo) / 20-30 minutos (por fase)

**Contenido:**

- ✅ Checklist de verificación (5 partes, ~50 items)
- ✅ Fase 1: Preparación (1 semana)
- ✅ Fase 2: VehiclesSaleService (1.5 sprints)
  - Código C# exacto para implementar
  - Migraciones de BD
  - Tests
- ✅ Fase 3: AdvertisingService (1 sprint)
  - Código C# exacto
  - Job scheduling
- ✅ Fase 4: Frontend (1 sprint)
  - Código TypeScript exacto
  - Hooks a actualizar
  - Componentes
- ✅ Fase 5: Testing (1 sprint)
  - E2E tests
  - Deployment checklist
- ✅ Matriz de riesgos (7 riesgos identificados)
- ✅ Tickets Jira a crear (6 stories + tasks)
- ✅ Timeline visual (4 sprints / 8 semanas)
- ✅ Criterios de aceptación (12 items)

**Cuando usarlo:**

- Necesitas crear tickets en Jira
- Necesitas código listo para copiar/pegar
- Necesitas estimar sprints
- Necesitas comunicar timeline
- Necesitas saber exactamente qué hacer

**Estructura de lectura recomendada:**

```
1. Intro + Checklist (30 min)
2. Tu fase específica (15-30 min según el rol)
3. Testing & Deployment (20 min)
4. Timeline + Tickets (15 min)
```

---

## 🗺️ MAPA CONCEPTUAL

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      FLUJO DE LECTURA RECOMENDADO                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  START HERE                                                              │
│      ▼                                                                   │
│  RESUMEN_EJECUTIVO (5 min)  ◄─── ¿Quién eres?                         │
│      │                                                                   │
│      ├─── Si eres Gerente/PM         ──► LISTO, DECIDE PROCEDER       │
│      │                                                                   │
│      ├─── Si eres Tech Lead/Architect ──► Lee AUDITORIA_FLUJO (30 min)│
│      │                                    ↓                             │
│      │                                    Lee DIAGRAMA_TECNICO (20 min)│
│      │                                    ↓                             │
│      │                                    ✅ Validar diseño             │
│      │                                                                   │
│      ├─── Si eres Dev Backend (.NET)  ──► Lee PLAN FASE 2-3 (20 min) │
│      │                                    ↓                             │
│      │                                    Ref. DIAGRAMA (30 min)        │
│      │                                    ↓                             │
│      │                                    ✅ Listo para codificar      │
│      │                                                                   │
│      ├─── Si eres Dev Frontend (React)──► Lee PLAN FASE 4 (15 min)   │
│      │                                    ↓                             │
│      │                                    Ref. DIAGRAMA NIVEL 4-5       │
│      │                                    ↓                             │
│      │                                    ✅ Listo para codificar      │
│      │                                                                   │
│      └─── Si eres QA                  ──► Lee PLAN FASE 5 (15 min)   │
│                                          ↓                              │
│                                          Ref. DIAGRAMA (20 min)        │
│                                          ↓                              │
│                                          ✅ Listo para testear         │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🎓 GLOSARIO DE TÉRMINOS

| Término                  | Definición                                                            | Ejemplo                                                 |
| ------------------------ | --------------------------------------------------------------------- | ------------------------------------------------------- |
| **Featured** (Destacado) | Vehículo mostrado en sección especial sin pago (asignado por admin)   | "⭐ Vehículos Destacados"                               |
| **Premium**              | Vehículo mostrado en sección especial CON PAGO (campaña pagada)       | "💎 Vehículos Premium"                                  |
| **Campaign**             | Campaña de publicidad pagada para un vehículo en AdvertisingService   | `Campaign { VehicleId, PlacementType, Budget, Status }` |
| **Placement**            | Tipo de ubicación para mostrar (FeaturedSpot, PremiumSpot, Banner)    | `placementType: "PremiumSpot"`                          |
| **Rotation**             | Selección dinámica de qué vehículos mostrar (rotación aleatoria/fifo) | GET /api/advertising/rotation                           |
| **Impression**           | Registro de que un usuario vio el vehículo                            | POST /api/advertising/tracking/impression               |
| **Click**                | Registro de que un usuario hizo clic en el vehículo                   | POST /api/advertising/tracking/click                    |
| **Homepage Section**     | Sección configurable del homepage (e.g., "Sedanes", "SUVs")           | `HomepageSection { Name, Slug, Vehicles }`              |
| **Dealer**               | Distribuidor de vehículos (puede pagar por publicidad)                | Usuario con rol "Dealer"                                |
| **Seller**               | Vendedor particular (generalmente no paga publicidad)                 | Usuario con rol "Seller"                                |

---

## 🔍 BÚSQUEDAS RÁPIDAS

### Por Brecha:

**¿Cuál es la brecha 1 (IsFeatured/IsPremium)?**
→ AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md → Sección "BRECHA #1"

**¿Cuál es la brecha 2 (Sincronización)?**
→ AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md → Sección "BRECHA #2"

**¿Cómo se resuelven las brechas?**
→ PLAN_IMPLEMENTACION_DESTACADOS.md → Sección "FASE 2 y 3"

---

### Por Componente:

**¿Qué es HomepageSectionsController?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md → Nivel 3

**¿Qué es RotationController?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md → Nivel 8 / PLAN → FASE 3

**¿Cómo funciona FeaturedVehicles component?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md → Nivel 4 y 5

---

### Por Tecnología:

**¿Cuál es el Schema SQL?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md → Nivel 7

**¿Cuáles son los eventos RabbitMQ?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md → Nivel 8

**¿Cuáles son los endpoints?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md → Mapa de URLs Backend

**¿Cuál es el código C# para implementar?**
→ PLAN_IMPLEMENTACION_DESTACADOS.md → FASE 2 y 3

**¿Cuál es el código TypeScript para implementar?**
→ PLAN_IMPLEMENTACION_DESTACADOS.md → FASE 4

---

## ✅ CHECKLIST DE LECTURA

**Para Gerente/PM:**

- [ ] Leer RESUMEN_EJECUTIVO completo (10 min)
- [ ] Entender 3 hallazgos principales
- [ ] Conocer estimación de esfuerzo (4.5 sprints)
- [ ] Decidir si proceder

**Para Tech Lead:**

- [ ] Leer RESUMEN_EJECUTIVO (10 min)
- [ ] Leer AUDITORIA completo (30 min)
- [ ] Revisar DIAGRAMA Nivel 1-3 (15 min)
- [ ] Revisar PLAN Intro (10 min)
- [ ] Validar con equipo

**Para Dev Backend:**

- [ ] Leer PLAN FASE 2-3 (20 min)
- [ ] Revisar DIAGRAMA Nivel 6-7 (20 min)
- [ ] Revisar código C# propuesto (15 min)
- [ ] Verificar checklist en AUDITORIA
- [ ] Listo para sprint 2

**Para Dev Frontend:**

- [ ] Leer PLAN FASE 4 (15 min)
- [ ] Revisar DIAGRAMA Nivel 4-5 (15 min)
- [ ] Revisar código TypeScript propuesto (15 min)
- [ ] Revisar mapa de URLs (10 min)
- [ ] Listo para sprint 4

**Para QA:**

- [ ] Leer PLAN FASE 5 (15 min)
- [ ] Revisar DIAGRAMA Level 6 (Tracking) (10 min)
- [ ] Revisar AUDITORIA casos de fallo (10 min)
- [ ] Listo para escribir tests

---

## 💡 TIPS DE USO

### Si tienes 5 minutos:

→ Lee RESUMEN_EJECUTIVO

### Si tienes 15 minutos:

→ Lee RESUMEN_EJECUTIVO + intro de AUDITORIA

### Si tienes 30 minutos:

→ Lee RESUMEN_EJECUTIVO + AUDITORIA (Hallazgos + Brechas)

### Si tienes 1 hora:

→ Lee RESUMEN_EJECUTIVO + AUDITORIA completo + DIAGRAMA Nivel 1-3

### Si tienes 2 horas:

→ Lee todos los documentos en orden

### Si necesitas solo para tu rol:

→ Ve a la sección "Según tu rol" al inicio

---

## 🚀 PRÓXIMO PASO

1. **Comparte** estos 4 documentos con tu equipo
2. **Lee** el documento apropiado para tu rol (ver arriba)
3. **Convoca** reunión de kickoff para sprint 1
4. **Ejecuta** checklist de verificación
5. **Crea** tickets en Jira basado en PLAN_IMPLEMENTACION

---

## 📊 ESTADÍSTICAS DE LOS DOCUMENTOS

| Documento           | Páginas | Palabras   | Tiempo de lectura   | Mejor para             |
| ------------------- | ------- | ---------- | ------------------- | ---------------------- |
| RESUMEN_EJECUTIVO   | 5       | 2,500      | 10-15 min           | Decisión rápida        |
| AUDITORIA_FLUJO     | 20      | 10,000     | 30-45 min           | Entendimiento profundo |
| DIAGRAMA_TECNICO    | 25      | 8,000      | 30-60 min\*         | Referencia técnica     |
| PLAN_IMPLEMENTACION | 30      | 12,000     | 45-90 min\*         | Ejecución              |
| **TOTAL**           | **80**  | **32,500** | **2-4 horas total** | **Visión 360°**        |

\*Tiempo variable según si lees completo o solo tu sección

---

## 🎯 OBJETIVOS ALCANZADOS

✅ Flujo de datos mapeado completamente  
✅ 5 brechas críticas identificadas  
✅ Soluciones propuestas con código  
✅ Timeline estimado (4.5 sprints)  
✅ Checklist de verificación (50+ items)  
✅ Tickets Jira listos para crear  
✅ Documentación completa  
✅ Diagramas técnicos detallados

---

## 📞 SOPORTE

**Preguntas sobre hallazgos?**
→ AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md

**Preguntas sobre implementación?**
→ PLAN_IMPLEMENTACION_DESTACADOS.md

**Preguntas técnicas?**
→ DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md

**Preguntas rápidas?**
→ RESUMEN_EJECUTIVO_AUDITORIA_DESTACADOS.md

---

_Índice generado: 23 de Febrero, 2026_  
_Versión: 1.0_  
_Estado: COMPLETO_
