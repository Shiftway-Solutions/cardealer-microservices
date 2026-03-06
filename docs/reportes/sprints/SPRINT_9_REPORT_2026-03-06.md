# 📊 Sprint 9 Report — Loading Skeletons + Image Performance + Error Boundaries

**Fecha**: 6 marzo 2026
**Sprint**: 9
**Commit**: `341e66ed`
**Branch**: main

---

## Objetivo del Sprint

Mejorar la experiencia de carga percibida (Perceived Performance), optimización de imágenes para LCP, y cobertura de error boundaries en rutas dinámicas clave.

## Hallazgos del Análisis

Auditoría de loading states, Image optimization y error recovery identificó 4 gaps:

1. `/dealers/[slug]` sin `loading.tsx` — perfil de dealer tarda en cargar, pantalla en blanco
2. `/marcas/[marca]` sin `loading.tsx` ni `error.tsx` — brand pages sin feedback de carga ni recuperación
3. 2 componentes `Image` con `fill` sin `sizes` prop — Next.js no genera srcset óptimos, LCP afectado
4. Cambios de sesión anterior no comiteados (Image sizes en 3 archivos más, guias loading skeletons, JSON-LD)

---

## Tareas Ejecutadas

### Task 46 — `dealers/[slug]/loading.tsx` — Skeleton de perfil dealer

**Archivo**: `src/app/(main)/dealers/[slug]/loading.tsx` (NUEVO — 115 líneas)

Skeleton que replica fielmente el layout del componente `dealer-profile-client.tsx`:

- **Cover image**: `Skeleton` full-width con `h-48 md:h-64 lg:h-80`
- **Header card**: Logo circular + nombre + badges + description + stats + action buttons
- **Tab bar**: 3 tabs skeleton (Inventario, Reseñas, Acerca de)
- **Vehicle grid**: 2x2 cards con image + title + price + tags
- **Sidebar**: Contact card + Location map + Schedule card

### Task 47 — `marcas/[marca]/loading.tsx` — Skeleton de brand page

**Archivo**: `src/app/(main)/marcas/[marca]/loading.tsx` (NUEVO — 68 líneas)

Skeleton que replica el layout de la brand page:

- **Hero section**: Gradient `from-gray-900 to-gray-800` + breadcrumb + title + description
- **Filter bar**: 3 filter pills skeleton
- **Vehicle grid**: 3x2 cards con image + title + price + tags + action
- **SEO section**: `bg-muted/50` con título + párrafos

### Task 48 — Image `sizes` prop optimization

**Archivos**: 2 modificados

| Archivo                     | Línea | `sizes` valor | Razón                                      |
| --------------------------- | ----- | ------------- | ------------------------------------------ |
| `cuenta/page.tsx`           | L1098 | `"96px"`      | Container fijo `w-24` (96px), nunca cambia |
| `dealer-profile-client.tsx` | L287  | `"100vw"`     | Cover banner full-viewport-width           |

**Impacto**: Next.js ahora genera srcset optimizado para estos tamaños, reduciendo bytes descargados y mejorando LCP.

### Task 49 — `marcas/[marca]/error.tsx` — Error boundary de marca

**Archivo**: `src/app/(main)/marcas/[marca]/error.tsx` (NUEVO — 62 líneas)

Error boundary con el mismo patrón branded de `dealers/[slug]/error.tsx`:

- AlertTriangle icon rojo en círculo
- Título: "Error cargando vehículos de esta marca"
- Botón retry: `bg-[#00A870]` con RefreshCw icon
- CTA: "Ver todas las marcas" → `/marcas`
- Link: "Volver al inicio" → `/`
- Error digest display

---

## Cambios Incluidos de Sesión Anterior (no comiteados)

| Archivo                     | Cambio                                          |
| --------------------------- | ----------------------------------------------- |
| `admin/vehiculos/page.tsx`  | Image sizes `"(max-width: 1024px) 100vw, 50vw"` |
| `cuenta/favoritos/page.tsx` | Image sizes `"(max-width: 640px) 100vw, 224px"` |
| `cuenta/historial/page.tsx` | Image sizes `"128px"`                           |
| `guias/loading.tsx`         | NUEVO — guías listing skeleton                  |
| `guias/[slug]/loading.tsx`  | NUEVO — guía detail skeleton (72 líneas)        |
| `guias/[slug]/page.tsx`     | BreadcrumbList JSON-LD agregado                 |
| `marcas/[marca]/page.tsx`   | BreadcrumbList JSON-LD format fix               |
| Sprint 8 report             | Markdown table formatting fixes                 |

---

## Métricas

| Métrica                | Antes | Después                           |
| ---------------------- | ----- | --------------------------------- |
| Routes con loading.tsx | 12    | 16 (+4)                           |
| Routes con error.tsx   | 4     | 5 (+1)                            |
| Image fill sin sizes   | 7     | 0                                 |
| Build pages            | 213   | 213                               |
| Build time             | 19.7s | 29.0s (includes more static work) |

---

## Cobertura Loading/Error por Ruta Dinámica

| Ruta                | loading.tsx       | error.tsx     |
| ------------------- | ----------------- | ------------- |
| `/vehiculos/[slug]` | ✅                | ✅            |
| `/dealers/[slug]`   | ✅ (Sprint 9)     | ✅ (Sprint 8) |
| `/marcas/[marca]`   | ✅ (Sprint 9)     | ✅ (Sprint 9) |
| `/blog/[slug]`      | ✅                | ❌ pendiente  |
| `/guias/[slug]`     | ✅ (prev session) | ❌ pendiente  |
| `/vender/registro`  | ✅                | ✅            |
| `/checkout`         | ✅                | ✅            |

---

## Build Verification

```
✓ Compiled successfully in 29.0s
✓ Generating static pages using 9 workers (213/213) in 12.9s
0 errors, 0 warnings
```

---

## Próximos pasos sugeridos

1. **Error boundaries** para `/blog/[slug]` y `/guias/[slug]`
2. **E2E testing** con Playwright para rutas críticas (nuevo requisito del PM)
3. **CI/CD monitoring** — verificar builds en producción
4. **Mobile QA** — probar loading states en dispositivos reales
