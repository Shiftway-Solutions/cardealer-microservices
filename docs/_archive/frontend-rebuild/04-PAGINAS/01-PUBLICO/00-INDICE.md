# 📁 01-PUBLICO - Páginas Públicas

> **Descripción:** Páginas accesibles sin autenticación  
> **Total:** 11 documentos  
> **Prioridad:** 🔴 P0 - Core del sistema

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                            | Descripción                              | Prioridad | Estado  |
| --- | -------------------------------------------------- | ---------------------------------------- | --------- | ------- |
| 1   | [01-home.md](01-home.md)                           | Página principal (diseño original)       | P0        | 📝 Doc  |
| 1b  | [01-home-implementado.md](01-home-implementado.md) | **Homepage Next.js IMPLEMENTADO**        | P0        | ✅ Impl |
| 2   | [02-busqueda.md](02-busqueda.md)                   | Búsqueda avanzada de vehículos           | P0        | 📝 Doc  |
| 3   | [03-detalle-vehiculo.md](03-detalle-vehiculo.md)   | Página de detalle de vehículo            | P0        | 📝 Doc  |
| 4   | [04-help-center.md](04-help-center.md)             | Centro de ayuda y FAQ                    | P2        | 📝 Doc  |
| 5   | [05-vehicle-360-page.md](05-vehicle-360-page.md)   | Visor 360° de vehículos                  | P1        | 📝 Doc  |
| 6   | [06-comparador.md](06-comparador.md)               | Comparador de vehículos                  | P1        | 📝 Doc  |
| 7   | [07-filtros-avanzados.md](07-filtros-avanzados.md) | Sistema de filtros avanzados             | P0        | 📝 Doc  |
| 8   | [08-search-completo.md](08-search-completo.md)     | Búsqueda completa con todos los features | P0        | 📝 Doc  |
| 9   | [09-vehicle-browse.md](09-vehicle-browse.md)       | Navegación de vehículos                  | P1        | 📝 Doc  |
| 10  | [10-static-pages.md](10-static-pages.md)           | Páginas estáticas                        | P2        | 📝 Doc  |

---

## ✅ Estado de Implementación Next.js

| Página       | Archivo Next.js                   | Estado |
| ------------ | --------------------------------- | ------ |
| **Homepage** | `src/app/page.tsx`                | ✅     |
| **Búsqueda** | `src/app/vehiculos/page.tsx`      | ✅     |
| Detalle      | `src/app/vehiculos/[id]/page.tsx` | ❌     |
| Help Center  | `src/app/ayuda/page.tsx`          | ❌     |
| 360 Viewer   | `src/app/vehiculos/360/page.tsx`  | ❌     |
| Comparador   | `src/app/comparar/page.tsx`       | ❌     |

---

## 🎯 Orden de Implementación para IA

```
✅ 1. 01-home-implementado.md → Página principal (COMPLETADO)
   2. 07-filtros-avanzados.md → Sistema de filtros (se usa en búsqueda)
   3. 02-busqueda.md       → Búsqueda de vehículos
   4. 08-search-completo.md → Búsqueda avanzada completa
   5. 03-detalle-vehiculo.md → Detalle de vehículo
   6. 05-vehicle-360-page.md → Vista 360°
   7. 06-comparador.md     → Comparador
   8. 09-vehicle-browse.md → Navegación
   9. 10-static-pages.md   → Páginas estáticas
  10. 04-help-center.md   → Centro de ayuda
```

---

## 🔗 Dependencias Externas

- **03-COMPONENTES/**: VehicleCard, SearchFilters, Gallery
- **05-API-INTEGRATION/**: vehicles-api, catalog-api
- **02-UX-DESIGN-SYSTEM/**: Design tokens, componentes base

---

## 📊 APIs Utilizadas

| Servicio            | Endpoints Principales                                  |
| ------------------- | ------------------------------------------------------ |
| VehiclesSaleService | GET /vehicles, GET /vehicles/:id, GET /vehicles/search |
| HomepageSections    | GET /api/homepagesections/homepage ✅                  |
| CatalogService      | GET /catalog/makes, GET /catalog/models                |
| Vehicle360Service   | GET /vehicle360/:id                                    |
| ComparisonService   | GET /comparisons, POST /comparisons                    |
