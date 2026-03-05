# 📁 09-COMPONENTES-COMUNES - Componentes y Layouts

> **Descripción:** Componentes reutilizables, layouts y páginas estáticas  
> **Total:** 6 documentos  
> **Prioridad:** 🔴 P0 - Fundamentos

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                              | Descripción                              | Prioridad |
| --- | ---------------------------------------------------- | ---------------------------------------- | --------- |
| 1   | [01-common-components.md](01-common-components.md)   | Componentes comunes (Button, Card, etc.) | P0        |
| 2   | [02-layouts.md](02-layouts.md)                       | Layouts (MainLayout, AdminLayout, etc.)  | P0        |
| 3   | [03-static-pages.md](03-static-pages.md)             | Páginas estáticas (About, Contact, etc.) | P2        |
| 4   | [04-vehicle-media.md](04-vehicle-media.md)           | Componentes de media de vehículos        | P1        |
| 5   | [05-video-tour.md](05-video-tour.md)                 | Video tours y test drive virtual         | P2        |
| 6   | [06-event-tracking-sdk.md](06-event-tracking-sdk.md) | SDK de tracking de eventos               | P1        |

---

## 🎯 Orden de Implementación para IA

```
1. 01-common-components.md   → Componentes base
2. 02-layouts.md             → Layouts principales
3. 04-vehicle-media.md       → Media de vehículos
4. 06-event-tracking-sdk.md  → Tracking de eventos
5. 03-static-pages.md        → Páginas estáticas
6. 05-video-tour.md          → Video tours
```

---

## 🔗 Dependencias Externas

- **02-UX-DESIGN-SYSTEM/**: Design tokens, principios UX
- **03-COMPONENTES/**: Componentes específicos
- **05-API-INTEGRATION/**: Analytics API

---

## 📊 Ubicación de Código

| Documento                | Ruta en Código             |
| ------------------------ | -------------------------- |
| 01-common-components.md  | `src/components/ui/`       |
| 02-layouts.md            | `src/components/layouts/`  |
| 03-static-pages.md       | `src/app/(public)/`        |
| 04-vehicle-media.md      | `src/components/vehicles/` |
| 06-event-tracking-sdk.md | `src/lib/tracking/`        |

---

## ⚠️ Nota Importante

Estos documentos definen la **base** sobre la cual se construyen todas las demás páginas. Deben implementarse **PRIMERO** antes de cualquier página específica.
