# 🚗 Plan de Sprints - Portal del Dealer OKLA

**Fecha de Creación:** Enero 2026
**Estado:** En Desarrollo
**Versión:** 1.0

---

## 📋 Resumen Ejecutivo

Este documento detalla el plan de desarrollo del Portal del Dealer para OKLA Marketplace. El portal permite a los dealers gestionar su inventario, leads, analytics y configuraciones desde una interfaz unificada con el mismo look & feel del sitio público.

---

## 🎯 Objetivos del Portal

1. **Experiencia Unificada**: Mismo tema visual que Homepage y Vehículos
2. **Gestión Completa**: Inventario, leads, analytics, facturación
3. **Escalabilidad por Plan**: Funciones según suscripción (Starter/Pro/Enterprise)
4. **Mobile-First**: Responsive design completo

---

## 📊 Estado Actual del Portal

### ✅ Completado

| Componente            | Estado | Descripción                             |
| --------------------- | ------ | --------------------------------------- |
| `DealerPortalLayout`  | ✅     | Layout unificado con navbar + sidebar   |
| `DealerHomePage`      | ✅     | Dashboard principal con métricas        |
| `DealerInventoryPage` | ✅     | Vista de inventario con grid/list       |
| `DealerLeadsPage`     | ✅     | Gestión de leads con temperatura        |
| `DealerAnalyticsPage` | ✅     | Analytics con gráficos                  |
| `DealerSettingsPage`  | ✅     | Configuración en tabs                   |
| Rutas en App.tsx      | ✅     | Todas las rutas configuradas            |
| Navbar integration    | ✅     | Link "Para Dealers" en navbar principal |

### 🔄 En Progreso

| Componente     | Estado | Descripción                          |
| -------------- | ------ | ------------------------------------ |
| Backend APIs   | 🔄     | Algunos endpoints mock, otros reales |
| Real-time data | 🔄     | Datos mock en frontend               |

---

## 📅 Plan de Sprints

### Sprint DP-1: Fundamentos del Portal (Completado ✅)

**Duración:** 1 semana
**Story Points:** 21

#### Objetivos

- [x] Crear DealerPortalLayout con navbar y sidebar
- [x] Implementar DealerHomePage con métricas
- [x] Crear páginas base (Inventory, Leads, Analytics, Settings)
- [x] Configurar rutas protegidas
- [x] Integrar con sistema de permisos

#### Entregables

- Layout unificado con tema consistente
- Dashboard con métricas principales
- Navegación completa funcional
- Responsive design

---

### Sprint DP-2: Gestión de Inventario

**Duración:** 2 semanas
**Story Points:** 34
**Dependencias:** Sprint DP-1

#### Objetivos

- [ ] CRUD completo de vehículos
- [ ] Galería de imágenes con drag & drop
- [ ] Bulk actions (activar, pausar, eliminar)
- [ ] Importación CSV/Excel
- [ ] Validación de límites por plan

#### Historias de Usuario

| ID    | Historia                                                          | SP  | Prioridad |
| ----- | ----------------------------------------------------------------- | --- | --------- |
| DP2-1 | Como dealer, quiero agregar un vehículo con fotos para publicarlo | 8   | Alta      |
| DP2-2 | Como dealer, quiero editar los detalles de mis vehículos          | 5   | Alta      |
| DP2-3 | Como dealer, quiero pausar/activar vehículos masivamente          | 5   | Media     |
| DP2-4 | Como dealer, quiero importar vehículos desde Excel                | 8   | Media     |
| DP2-5 | Como dealer, quiero ver cuántos slots me quedan según mi plan     | 3   | Alta      |
| DP2-6 | Como dealer, quiero duplicar un vehículo existente                | 5   | Baja      |

#### Criterios de Aceptación

- [ ] Formulario de vehículo con validación completa
- [ ] Upload de imágenes hasta 20 por vehículo
- [ ] Drag & drop para reordenar imágenes
- [ ] Indicador de slots usados/disponibles
- [ ] Notificación al alcanzar 80% del límite

---

### Sprint DP-3: Gestión de Leads

**Duración:** 2 semanas
**Story Points:** 34
**Dependencias:** Sprint DP-1, LeadScoringService (Sprint 11 backend)

#### Objetivos

- [ ] Vista de leads con filtros avanzados
- [ ] Detalle de lead con historial de contacto
- [ ] Quick actions (llamar, email, WhatsApp)
- [ ] Asignación de leads a usuarios del dealer
- [ ] Pipeline visual (Kanban)

#### Historias de Usuario

| ID    | Historia                                                               | SP  | Prioridad |
| ----- | ---------------------------------------------------------------------- | --- | --------- |
| DP3-1 | Como dealer, quiero ver todos mis leads ordenados por temperatura      | 5   | Alta      |
| DP3-2 | Como dealer, quiero filtrar leads por fecha, vehículo, estado          | 5   | Alta      |
| DP3-3 | Como dealer, quiero ver el historial de interacciones con un lead      | 8   | Alta      |
| DP3-4 | Como dealer, quiero contactar un lead con un clic (tel/email/WhatsApp) | 3   | Alta      |
| DP3-5 | Como dealer, quiero asignar leads a mis vendedores                     | 5   | Media     |
| DP3-6 | Como dealer, quiero mover leads entre etapas del pipeline              | 8   | Media     |

#### Criterios de Aceptación

- [ ] Lista de leads con badges de temperatura (Hot/Warm/Cold)
- [ ] Filtros funcionando en tiempo real
- [ ] Integración con LeadScoringService
- [ ] Vista Kanban opcional
- [ ] Notificaciones de nuevos leads hot

---

### Sprint DP-4: Analytics y Reportes

**Duración:** 2 semanas
**Story Points:** 29
**Dependencias:** Sprint DP-2, Sprint DP-3

#### Objetivos

- [ ] Dashboard de métricas en tiempo real
- [ ] Gráficos de tendencias (vistas, leads, conversión)
- [ ] Top vehículos por performance
- [ ] Comparación período vs período
- [ ] Exportación de reportes PDF/Excel

#### Historias de Usuario

| ID    | Historia                                                         | SP  | Prioridad |
| ----- | ---------------------------------------------------------------- | --- | --------- |
| DP4-1 | Como dealer, quiero ver mis métricas clave del mes               | 5   | Alta      |
| DP4-2 | Como dealer, quiero gráficos de tendencia de los últimos 6 meses | 8   | Alta      |
| DP4-3 | Como dealer, quiero ver cuáles vehículos tienen mejor conversión | 5   | Alta      |
| DP4-4 | Como dealer, quiero comparar este mes vs el anterior             | 5   | Media     |
| DP4-5 | Como dealer, quiero exportar un reporte en PDF                   | 3   | Media     |
| DP4-6 | Como dealer, quiero ver el funnel de conversión                  | 3   | Media     |

#### Criterios de Aceptación

- [ ] Datos en tiempo real del backend
- [ ] Gráficos interactivos con hover tooltips
- [ ] Filtro de rango de fechas
- [ ] Comparación visual mes vs mes
- [ ] Export funcional a PDF y Excel

---

### Sprint DP-5: Billing y Suscripciones

**Duración:** 2 semanas
**Story Points:** 34
**Dependencias:** BillingService (Sprint 4)

#### Objetivos

- [ ] Vista del plan actual y uso
- [ ] Upgrade/Downgrade de plan
- [ ] Historial de facturas
- [ ] Métodos de pago (Stripe + AZUL)
- [ ] Cancelación de suscripción

#### Historias de Usuario

| ID    | Historia                                                | SP  | Prioridad |
| ----- | ------------------------------------------------------- | --- | --------- |
| DP5-1 | Como dealer, quiero ver mi plan actual y lo que incluye | 3   | Alta      |
| DP5-2 | Como dealer, quiero upgrade mi plan para más vehículos  | 8   | Alta      |
| DP5-3 | Como dealer, quiero ver mi historial de facturas        | 5   | Alta      |
| DP5-4 | Como dealer, quiero agregar/cambiar método de pago      | 5   | Alta      |
| DP5-5 | Como dealer, quiero cancelar mi suscripción             | 5   | Media     |
| DP5-6 | Como dealer, quiero ver cuánto ahorro con Early Bird    | 3   | Baja      |
| DP5-7 | Como dealer, quiero pagar con tarjeta dominicana (AZUL) | 5   | Alta      |

#### Criterios de Aceptación

- [ ] Checkout con Stripe y AZUL
- [ ] Cambio de plan inmediato o al próximo ciclo
- [ ] Facturas descargables en PDF
- [ ] Confirmación de cancelación con razón
- [ ] Webhooks procesando eventos de pago

---

### Sprint DP-6: Multi-Usuario y Roles

**Duración:** 2 semanas
**Story Points:** 26
**Dependencias:** RoleService

#### Objetivos

- [ ] Invitar usuarios al dealer
- [ ] Roles: Admin, Manager, Vendedor
- [ ] Permisos granulares por sección
- [ ] Actividad por usuario
- [ ] Límites de usuarios por plan

#### Historias de Usuario

| ID    | Historia                                           | SP  | Prioridad |
| ----- | -------------------------------------------------- | --- | --------- |
| DP6-1 | Como admin, quiero invitar usuarios a mi dealer    | 5   | Alta      |
| DP6-2 | Como admin, quiero asignar roles a mis usuarios    | 5   | Alta      |
| DP6-3 | Como admin, quiero ver qué hace cada usuario       | 5   | Media     |
| DP6-4 | Como admin, quiero desactivar usuarios             | 3   | Alta      |
| DP6-5 | Como admin, quiero configurar permisos por sección | 8   | Media     |

#### Criterios de Aceptación

- [ ] Invitación por email funcional
- [ ] 3 roles predefinidos con permisos diferentes
- [ ] Log de actividad por usuario
- [ ] Límites: Starter=2, Pro=5, Enterprise=ilimitado

---

### Sprint DP-7: Sucursales y Ubicaciones

**Duración:** 1 semana
**Story Points:** 18
**Dependencias:** Sprint DP-1

#### Objetivos

- [ ] CRUD de sucursales
- [ ] Mapa con ubicaciones
- [ ] Horarios por sucursal
- [ ] Asociar vehículos a sucursales

#### Historias de Usuario

| ID    | Historia                                                       | SP  | Prioridad |
| ----- | -------------------------------------------------------------- | --- | --------- |
| DP7-1 | Como dealer, quiero agregar mis sucursales                     | 5   | Alta      |
| DP7-2 | Como dealer, quiero ver mis sucursales en un mapa              | 5   | Media     |
| DP7-3 | Como dealer, quiero configurar horarios por sucursal           | 3   | Media     |
| DP7-4 | Como dealer, quiero indicar en qué sucursal está cada vehículo | 5   | Media     |

---

### Sprint DP-8: Notificaciones y Alertas

**Duración:** 1 semana
**Story Points:** 18
**Dependencias:** NotificationService

#### Objetivos

- [ ] Centro de notificaciones in-app
- [ ] Configuración de preferencias
- [ ] Notificaciones push (PWA)
- [ ] Email digests

#### Historias de Usuario

| ID    | Historia                                                  | SP  | Prioridad |
| ----- | --------------------------------------------------------- | --- | --------- |
| DP8-1 | Como dealer, quiero ver todas mis notificaciones          | 5   | Alta      |
| DP8-2 | Como dealer, quiero configurar qué notificaciones recibir | 5   | Alta      |
| DP8-3 | Como dealer, quiero notificaciones push en el navegador   | 5   | Media     |
| DP8-4 | Como dealer, quiero un resumen diario por email           | 3   | Baja      |

---

### Sprint DP-9: Chatbot y Mensajería

**Duración:** 2 semanas
**Story Points:** 34
**Dependencias:** ChatbotService (Sprint 17)

#### Objetivos

- [ ] Ver conversaciones del chatbot
- [ ] Tomar control de conversaciones
- [ ] Respuestas predefinidas
- [ ] Integración WhatsApp

#### Historias de Usuario

| ID    | Historia                                                            | SP  | Prioridad |
| ----- | ------------------------------------------------------------------- | --- | --------- |
| DP9-1 | Como dealer, quiero ver las conversaciones del chatbot con clientes | 8   | Alta      |
| DP9-2 | Como dealer, quiero intervenir en una conversación                  | 8   | Alta      |
| DP9-3 | Como dealer, quiero respuestas predefinidas para usar               | 5   | Media     |
| DP9-4 | Como dealer, quiero continuar conversaciones por WhatsApp           | 8   | Media     |
| DP9-5 | Como dealer, quiero ver el score del lead en la conversación        | 5   | Media     |

---

### Sprint DP-10: Polish y Optimización

**Duración:** 1 semana
**Story Points:** 13

#### Objetivos

- [ ] Performance optimization
- [ ] Lazy loading de componentes
- [ ] Skeleton loaders
- [ ] Error boundaries
- [ ] Accessibility audit
- [ ] Tests E2E

#### Tareas

| ID     | Tarea                             | SP  |
| ------ | --------------------------------- | --- |
| DP10-1 | Implementar lazy loading en rutas | 3   |
| DP10-2 | Agregar skeleton loaders          | 3   |
| DP10-3 | Audit y fix de accessibility      | 3   |
| DP10-4 | Tests E2E con Playwright          | 4   |

---

## 📊 Resumen de Sprints

| Sprint    | Nombre                   | Semanas        | SP         | Estado         |
| --------- | ------------------------ | -------------- | ---------- | -------------- |
| DP-1      | Fundamentos del Portal   | 1              | 21         | ✅ Completado  |
| DP-2      | Gestión de Inventario    | 2              | 34         | ⏳ Próximo     |
| DP-3      | Gestión de Leads         | 2              | 34         | 📋 Planificado |
| DP-4      | Analytics y Reportes     | 2              | 29         | 📋 Planificado |
| DP-5      | Billing y Suscripciones  | 2              | 34         | 📋 Planificado |
| DP-6      | Multi-Usuario y Roles    | 2              | 26         | 📋 Planificado |
| DP-7      | Sucursales y Ubicaciones | 1              | 18         | 📋 Planificado |
| DP-8      | Notificaciones y Alertas | 1              | 18         | 📋 Planificado |
| DP-9      | Chatbot y Mensajería     | 2              | 34         | 📋 Planificado |
| DP-10     | Polish y Optimización    | 1              | 13         | 📋 Planificado |
| **TOTAL** |                          | **16 semanas** | **261 SP** |                |

---

## 🎨 Guía de Diseño del Portal

### Colores Principales

```css
/* Gradientes */
--gradient-primary: linear-gradient(to right, #2563eb, #10b981);
--gradient-button: linear-gradient(to right, #3b82f6, #06b6d4);

/* Backgrounds */
--bg-sidebar: #1f2937; /* gray-800 */
--bg-card: #ffffff;
--bg-page: #f9fafb; /* gray-50 */

/* Accents */
--accent-blue: #3b82f6;
--accent-emerald: #10b981;
--accent-orange: #f97316;
```

### Componentes Comunes

```
┌─────────────────────────────────────────────────────────┐
│ 🔵 NAVBAR (fixed top)                               👤  │
├─────────┬───────────────────────────────────────────────┤
│         │                                               │
│ 📋      │  📊 Page Content                              │
│ SIDEBAR │                                               │
│         │  ┌─────────┐ ┌─────────┐ ┌─────────┐          │
│ • Home  │  │ Metric  │ │ Metric  │ │ Metric  │          │
│ • Inv   │  │ Card    │ │ Card    │ │ Card    │          │
│ • Leads │  └─────────┘ └─────────┘ └─────────┘          │
│ • Stats │                                               │
│ • ⚙️    │  ┌─────────────────────────────────────┐      │
│         │  │                                     │      │
│         │  │     Main Content Area              │      │
│         │  │                                     │      │
│         │  └─────────────────────────────────────┘      │
└─────────┴───────────────────────────────────────────────┘
```

### Card Component

```tsx
<div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
  <h3 className="text-lg font-semibold text-gray-900">Title</h3>
  <p className="text-gray-500 mt-1">Description</p>
</div>
```

### Metric Card

```tsx
<div className="bg-white rounded-2xl p-6 border border-gray-100">
  <div className="flex items-center gap-3">
    <div className="p-3 rounded-xl bg-blue-50">
      <FiPackage className="w-6 h-6 text-blue-600" />
    </div>
    <div>
      <p className="text-sm text-gray-500">Vehículos</p>
      <p className="text-2xl font-bold text-gray-900">24</p>
    </div>
  </div>
</div>
```

---

## 🔗 Dependencias de Backend

| Servicio                   | Sprint Requerido | Estado           |
| -------------------------- | ---------------- | ---------------- |
| DealerManagementService    | DP-1 a DP-10     | ✅ Activo        |
| InventoryManagementService | DP-2             | ✅ Activo        |
| LeadScoringService         | DP-3             | ✅ Activo        |
| DealerAnalyticsService     | DP-4             | 🔄 Parcial       |
| BillingService             | DP-5             | ✅ Activo        |
| RoleService                | DP-6             | ✅ Activo        |
| NotificationService        | DP-8             | ✅ Activo        |
| ChatbotService             | DP-9             | 🔄 En desarrollo |

---

## 📝 Notas de Implementación

### Priorización

1. **MVP (Sprints DP-1 a DP-5)**: Core funcional para dealers
2. **Growth (Sprints DP-6 a DP-8)**: Features de expansión
3. **Advanced (Sprints DP-9 a DP-10)**: IA y optimización

### Consideraciones Técnicas

- Usar React Query para cache de datos
- Implementar optimistic updates
- Skeleton loaders en todas las vistas
- Error boundaries por sección
- Lazy loading de rutas pesadas

### Testing

- Unit tests para hooks y utilities
- Integration tests para formularios
- E2E tests para flujos críticos (CRUD, billing)

---

## 📞 Contacto

**Product Owner:** Gregory Moreno
**Tech Lead:** Gregory Moreno
**Email:** gmoreno@okla.com.do

---

_Última actualización: Enero 2026_
