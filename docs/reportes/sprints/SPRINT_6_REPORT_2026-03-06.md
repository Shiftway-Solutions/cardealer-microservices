# 📋 SPRINT 6 — Accesibilidad + Mobile UX + Code Quality

**Fecha**: 6 de marzo 2026
**Commit principal**: `960416ed`
**Build**: ✅ 213 páginas generadas, 19.7s compilación

---

## 🎯 Objetivo del Sprint

Remediar WCAG Level A/AA failures en componentes de alto tráfico, mejorar touch targets para usuarios móviles dominicanos (prioridad explícita del PM: "todo lo que se hace via web se debe hacer via mobil"), y eliminar dead code acumulado.

## 📊 Auditoría Previa

Auditoría WCAG 2.1 + Mobile UX de ~10 componentes core identificó:

1. **Hero search input sin aria-label** — WCAG 4.1.2 Level A failure (screen readers no identifican campo)
2. **Favorite buttons 36px** — Debajo de 44px mínimo WCAG 2.5.8 AA
3. **Footer links ~22px tall** — Sin padding mínimo para touch targets
4. **~150 líneas dead code** — 3 exports + 2 constants nunca importados en hero-compact.tsx
5. **Favorite button oculto en mobile** — `opacity-0 sm:opacity-100` inaccessible en touch

---

## ✅ Tareas Completadas

### Tarea 34: Hero Search — `aria-label` + Form Wrapper

**Archivo**: `src/components/homepage/hero-compact.tsx`

- Reemplazado `<div>` container con `<form role="search" aria-label="Buscar vehículos">`
- Agregado `aria-label="Buscar vehículos por marca, modelo, tipo o descripción"` al `<input>`
- Cambiado `onClick` del botón a `type="submit"` — form submit nativo
- Eliminado `handleKeyDown` callback manual (ya no necesario con form submit)

**Impacto WCAG**: Resuelve 4.1.2 Level A — Screen readers ahora identifican correctamente el campo de búsqueda principal

### Tarea 35: Vehicle Card Favorite Buttons — 44px Touch Targets

**Archivo**: `src/components/ui/vehicle-card.tsx`

- Default variant: `p-2` → `p-2.5` + `min-h-[44px] min-w-[44px]`
- Mobile visibility: `isHovered ? 'opacity-100' : 'opacity-0 sm:opacity-100'` → `opacity-100 sm:opacity-0 sm:group-hover:opacity-100`
- Horizontal variant: `p-2` → `p-2.5` + `min-h-[44px] min-w-[44px]`

**Impacto**: Botón siempre visible y tocable en mobile (44×44px), se oculta solo en desktop hasta hover

### Tarea 36: Footer Links — Mobile Touch Targets

**Archivo**: `src/components/layout/footer.tsx`

- Link columns: `space-y-2` → `space-y-0`, agregado `py-2 inline-block` a cada link
- Bottom bar: `gap-6` → `gap-2 sm:gap-6`, agregado `px-2 py-2` a links legales
- Touch target efectivo: ~22px → 44px+ en mobile

**Impacto**: 20+ links en footer ahora tienen touch targets adecuados. Links legales (Términos, Privacidad, Cookies) correctamente espaciados.

### Tarea 37: Dead Code Cleanup — hero-compact.tsx

**Archivo**: `src/components/homepage/hero-compact.tsx`

Removidos (~150 líneas):

- `POPULAR_MAKES` constant (8 marcas, nunca referenciado)
- `modelsByMake` constant (8×7 = 56 modelos, nunca referenciado)
- `export function QuickFilters()` — Exportado pero nunca importado (hero usa `QuickFiltersHero` privado)
- `export function TrustBadgesInline()` — Exportado pero nunca importado (hero usa `TrustBadgesHero` privado)
- `export function VehicleCardCompact()` + `VehicleCardCompactProps` — 90+ líneas con state, useMemo, Image, Heart, tier badges — todo sin usar

Imports limpiados:

- Removidos: `useMemo`, `Heart`, `Gauge`, `MapPin`, `formatCurrency`, `formatMileage`, `Vehicle` type

**Resultado**: Archivo reducido de 460 → ~285 líneas (-38%)

---

## 📈 Métricas del Sprint

| Métrica                        | Antes | Después |
| ------------------------------ | ----- | ------- |
| WCAG Level A violations (hero) | 1     | 0       |
| Touch target ≥44px (fav btns)  | 0/2   | 2/2     |
| Footer link touch target       | ~22px | 44px+   |
| hero-compact.tsx lines         | 460   | ~285    |
| Unused exports removed         | 3     | 0       |
| Unused constants removed       | 2     | 0       |
| Build time                     | ~26s  | ~20s    |

---

## 🔍 Hallazgos para futuros sprints

- **Navbar dropdown keyboard nav** — WCAG 2.1.1 Level A, needs refactor to Radix DropdownMenu (M)
- **Hero quick filter pills** — 32px, should be 44px on mobile (S)
- **ItemList JSON-LD** en /vehiculos — SEO rich snippets still pending (M)
- **Homepage 12 API calls** → batch/lazy load for mobile performance (L)
- **/comparar SSR** + dynamic WhatsApp metadata (M)
- **Console.log cleanup** — Some production console.logs remain in checkout/error flows

---

## 📝 Notas Técnicas

- WCAG 2.5.8 (Target Size) requiere 44×44px mínimo para interactivos — `min-h-[44px] min-w-[44px]` es más robusto que `p-2.5` solo
- La migración de `<div>` a `<form role="search">` elimina la necesidad de `handleKeyDown` manual — el form submit nativo maneja Enter
- El pattern `opacity-100 sm:opacity-0 sm:group-hover:opacity-100` = visible en mobile, hidden→reveal on desktop hover
- Dead code removal de ~175 líneas reduce bundle size del homepage chunk

---

_Reporte generado automáticamente por PM Agent — Sprint 6_
