# 🎯 RESUMEN EJECUTIVO — Prompts de Auditoría QA Frontend OKLA

**Generado:** 2026-02-23 | **Estado:** ✅ LISTO PARA QA  
**Objetivo:** Adaptación completa del prompt genérico al contexto específico del codebase OKLA (frontend destacados/premium)

---

## 📋 ¿QUÉ SE ENTREGÓ?

Se crearon **3 documentos complementarios** específicamente adaptados a tu contexto:

### 1️⃣ **QUICKSTART_QA_FRONTEND.md** (5 minutos)

- ⚡ Setup inmediato (pnpm install + pnpm dev)
- 📊 Checklist visual de 14 elementos
- 🔧 Debugging rápido (errores comunes)
- 📱 Test responsive en 1 minuto
- 🎬 Casos de uso: "Verificar en 10 minutos"

**Cuándo usarlo:** Cuando necesitas verificación rápida o no tienes mucho tiempo

---

### 2️⃣ **PROMPT_QA_FRONTEND_DESTACADOS.md** (2-3 horas)

- 🚀 9 pasos de auditoría completa (PASO 0 → PASO 9)
- 📸 Especificación exacta de 8 screenshots a capturar
- ✅ Checklist de verificación de 30+ puntos
- 🐛 Referencia de 6 bugs comunes con soluciones
- 📋 Plantilla de informe final (REVISION_FRONTEND_DESTACADOS_YYYYMMDD.md)
- 📖 FAQ + recursos de referencia

**Cuándo usarlo:** Auditoría completa con documentación + PR

---

### 3️⃣ **INDEX_AUDITORÍA_FRONTEND_QA.md** (Mapa de navegación)

- 🗺️ Matriz de documentos (backend ✅ + frontend 🔄)
- 📊 Progreso global (100% backend, pending frontend)
- 💡 Mapeo: "Necesito X" → Documento Y
- 🎯 4 escenarios diferentes con pasos exactos
- 🔗 Commits relacionados + PR links
- 📞 FAQ global

**Cuándo usarlo:** Punto de partida para entender qué documento necesitas

---

## 🎯 ADAPTACIONES AL CONTEXTO OKLA

### ✅ Lo específico de TU codebase

Cada documento menciona:

| Elemento               | Referencia específica                                                             |
| ---------------------- | --------------------------------------------------------------------------------- |
| **Archivo componente** | `frontend/web-next/src/components/advertising/featured-vehicles.tsx` (200 líneas) |
| **Hook de datos**      | `useHomepageRotation()` en `use-advertising.ts`                                   |
| **API client**         | `getHomepageRotation()` en `services/advertising.ts`                              |
| **Tipos**              | `RotatedVehicle`, `HomepageRotation` en `types/advertising.ts`                    |
| **Endpoints**          | `/api/advertising/rotation/{section}`, `/api/advertising/tracking/*`              |
| **Stack**              | Next.js 16, TanStack Query v5, shadcn/ui, Tailwind v4                             |
| **Package manager**    | pnpm (NO npm/yarn)                                                                |
| **UI badges**          | ⭐ Destacado (isFeatured) y 💎 Premium (isPremium)                                |
| **Formato precio**     | RD$900,000 (es-DO locale) o US$X (en-US)                                          |

### ✅ Casos límite validados

- Imagen faltante → Mostrar 🚗 placeholder
- Precio faltante → No renderizar nada (o "Consultar")
- Ubicación faltante → No mostrar línea
- Lista vacía → Retornar null o mensaje
- Error de red → Mostrar mensaje amigable

### ✅ Bugs conocidos del proyecto

Referencia de 6 bugs que ya fueron identificados + soluciones:

1. **Badges superpuestos** (ya implementado XOR correctamente)
2. **Imagen no carga** (ya manejado con placeholder)
3. **Impresiones duplicadas** (ya usado `useRef` correctamente)
4. **Precio sin formato** (ya usa `formatPrice()`)
5. **Sin skeleton de carga** (ya implementado)
6. **Links rotos a vehículos** (a validar: `/vehiculos/{slug}`)

---

## 📊 CÓMO FLUYE TODO

```
┌─────────────────────────────────────────────────────────────┐
│           🔴 BACKEND: COMPLETADO (2026-02-23)              │
├─────────────────────────────────────────────────────────────┤
│  ✅ 4 GAPs implementados (AdvertisingService)              │
│  ✅ 3 bugs backend corregidos + CI fixes (13/14 verde)     │
│  ✅ REPORT_IMPLEMENTACION_DESTACADOS_20260223.md           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│   🟡 FRONTEND: AUDITORÍA EN PROGRESO (2026-02-23)          │
├─────────────────────────────────────────────────────────────┤
│  📋 QUICKSTART_QA_FRONTEND.md (5 min)                      │
│  📋 PROMPT_QA_FRONTEND_DESTACADOS.md (2-3 horas)          │
│  📋 INDEX_AUDITORÍA_FRONTEND_QA.md (mapa)                 │
│                                                             │
│  TU ACCIÓN: Elige uno de los 2 documentos anteriores      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 CUÁL DOCUMENTO USAR (DECISIÓN RÁPIDA)

### Si tienes **10-15 minutos**

→ **QUICKSTART_QA_FRONTEND.md**

```bash
1. pnpm dev (backend + frontend)
2. Abre http://localhost:3000
3. F12 → ejecuta checklist
4. Resultado: ✅ OK o ❌ BUG
```

### Si tienes **2-3 horas** (recomendado para entrega)

→ **PROMPT_QA_FRONTEND_DESTACADOS.md**

```bash
1. Sigue PASO 0 → PASO 9
2. Toma 8 screenshots
3. Completa checklist
4. Genera informe + abre PR (si hay bugs)
```

### Si no sabes por dónde empezar

→ **INDEX_AUDITORÍA_FRONTEND_QA.md**

```bash
1. Lee sección "CÓMO USAR ESTE ÍNDICE"
2. Elige tu escenario (QA, Dev, etc.)
3. Sigue pasos sugeridos
```

---

## 📈 ESTRUCTURA DEL PROMPT LARGO

El **PROMPT_QA_FRONTEND_DESTACADOS.md** está organizado así:

```
├── 🎯 RESUMEN EJECUTIVO (contexto rápido)
├── 🗂️ ARQUITECTURA FRONTEND ACTUAL
│   ├── Tabla de componentes
│   ├── Stack (Next.js, TanStack Query, shadcn/ui, etc.)
│   └── Datos esperados (RotatedVehicle interface)
├── 🚀 PASOS DE VERIFICACIÓN (9 pasos)
│   ├── PASO 0: Preparación
│   ├── PASO 1: Visual en homepage
│   ├── PASO 2: Análisis de featured-vehicles.tsx
│   ├── PASO 3: Análisis de hooks/servicios
│   ├── PASO 4: Testing con mock
│   ├── PASO 5: Casos límite
│   ├── PASO 6: Responsive
│   ├── PASO 7: Tracking
│   ├── PASO 8: Accesibilidad
│   └── PASO 9: E2E manual
├── 🐛 POSIBLES BUGS A BUSCAR (6 referencias)
├── ✅ CHECKLIST FINAL (30+ puntos)
├── 📸 CAPTURAS REQUERIDAS (8 específicas)
├── 💾 COMMITS & PULL REQUEST (template)
├── ⏱️ TIEMPO ESTIMADO (por fase)
├── 📊 PLANTILLA INFORME FINAL
├── 🎓 REFERENCIAS
└── ❓ PREGUNTAS FRECUENTES
```

---

## 💡 EJEMPLO DE USO (Caso real)

**Escenario:** Eres QA y necesitas verificar la UI en 1 hora

```bash
1. ⏰ Abre QUICKSTART_QA_FRONTEND.md
2. ⚡ Ejecutas: pnpm dev + http://localhost:3000
3. 📋 Haces: Checklist visual (14 puntos, 10 min)
4. 📸 Capturas: 1 screenshot
5. ✅ Resultado:
   - Si TODO ✅: "VERDE - Ready for production"
   - Si ❌ bug: "Rojo - Bug en featured-vehicles.tsx línea XX"
6. 📝 Reportas: En GitHub Issues con screenshot
```

**Total:** 15-20 min

---

## 🎁 ENTREGABLES FINALES

Este es un conjunto completo de **prompts auditables**:

- ✅ **QUICKSTART_QA_FRONTEND.md** — Para verificación rápida
- ✅ **PROMPT_QA_FRONTEND_DESTACADOS.md** — Para auditoría completa (libro de 50+ páginas)
- ✅ **INDEX_AUDITORÍA_FRONTEND_QA.md** — Mapa de navegación
- ✅ **Este documento** — Resumen ejecutivo

Todos están en el **repo principal** del proyecto:

```
/cardealer-microservices/
├── QUICKSTART_QA_FRONTEND.md
├── PROMPT_QA_FRONTEND_DESTACADOS.md
├── INDEX_AUDITORÍA_FRONTEND_QA.md
└── SUMMARY_QA_FRONTEND_PROMPTS.md (este)
```

---

## ✨ VENTAJAS DE ESTOS DOCUMENTOS

### ✅ Contexto específico

No es un prompt genérico. Todo está adaptado a:

- Tu codebase OKLA (nombres de archivos, funciones, tipos)
- Tu stack (Next.js 16, TanStack Query v5, shadcn/ui)
- Tu arquitectura (BFF, microservicios .NET 8)
- Tu CI/CD (GitHub Actions, Kubernetes DOKS)

### ✅ Múltiples niveles de profundidad

- 5 min (QUICKSTART)
- 2-3 horas (FULL AUDIT)
- Mapa de navegación (INDEX)

### ✅ Documentación ejecutable

No solo describe QUÉ hacer, sino exactamente CÓMO y DÓNDE:

- Líneas de código específicas
- Nombres de archivos completos
- URLs exactas
- Comandos copiar-pegar

### ✅ Trazabilidad completa

Cada paso genera artefactos:

- 8 screenshots específicas
- Checklist de 30+ puntos
- Template de informe
- Template de PR

### ✅ Referencia de bugs

No necesitas adivinar. Aquí están los 6 bugs más comunes del proyecto con soluciones.

---

## 🎯 PRÓXIMOS PASOS

### Opción 1: QA Inmediata (30 min)

```
1. Abre: QUICKSTART_QA_FRONTEND.md
2. Lee: Primera sección (2 min)
3. Ejecuta: Checklist visual (10 min)
4. Reporta: ✅ OK o ❌ BUG
```

### Opción 2: Auditoría Completa (2-3 horas)

```
1. Abre: PROMPT_QA_FRONTEND_DESTACADOS.md
2. Sigue: PASO 0 → PASO 9
3. Captura: 8 screenshots
4. Completa: Checklist final
5. Genera: REVISION_FRONTEND_DESTACADOS_YYYYMMDD.md
6. Abre: PR en GitHub
```

### Opción 3: Entender Primero (10 min)

```
1. Abre: INDEX_AUDITORÍA_FRONTEND_QA.md
2. Lee: Sección "CÓMO USAR"
3. Elige: Tu escenario (QA, Dev, Manager)
4. Sigue: Pasos sugeridos
```

---

## 📞 SOPORTE

- **Dudas sobre frontend:** PROMPT_QA_FRONTEND_DESTACADOS.md → FAQ
- **Debugging rápido:** QUICKSTART_QA_FRONTEND.md → Debugging tips
- **¿Qué documento necesito?:** INDEX_AUDITORÍA_FRONTEND_QA.md → Matriz
- **Errores o sugerencias:** GitHub Issues con etiqueta `qa-frontend`

---

## 📊 ESTADÍSTICAS

| Métrica               | Valor                                                                  |
| --------------------- | ---------------------------------------------------------------------- |
| Documentos creados    | 3 + 1 (este)                                                           |
| Pasos auditables      | 9 (PASO 0 → 9)                                                         |
| Screenshots esperadas | 8 específicas                                                          |
| Bugs referenciados    | 6 (con soluciones)                                                     |
| Checklist items       | 30+ puntos                                                             |
| Tiempo quick audit    | 5-15 min                                                               |
| Tiempo full audit     | 2-3 horas                                                              |
| Stack frontend        | Next.js 16, TanStack Query v5, shadcn/ui                               |
| Archivos auditables   | 4 específicos (featured-vehicles, use-advertising, advertising, types) |

---

## ✅ CONCLUSIÓN

Recibiste un **conjunto completo de prompts QA personalizados** para auditar la UI de vehículos destacados y premium en OKLA:

1. ✅ **QUICKSTART** (5 min) — Para verificación rápida
2. ✅ **FULL PROMPT** (2-3 horas) — Para auditoría profunda
3. ✅ **INDEX** (mapa) — Para navegar entre documentos

Todo está listo. Solo falta que hagas clic y comiences.

---

**👉 Empieza ahora:** [QUICKSTART_QA_FRONTEND.md](./QUICKSTART_QA_FRONTEND.md)

⏱️ **5 minutos de setup**  
🎯 **Verificación de UI**  
✨ **Resultado inmediato**

---

_Generado: 2026-02-23 | Status: ✅ READY TO USE_
