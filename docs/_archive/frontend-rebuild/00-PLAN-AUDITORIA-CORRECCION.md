# 🔍 PLAN DE AUDITORÍA Y CORRECCIÓN - Frontend Rebuild vs Process Matrix

> **Fecha de Auditoría:** Enero 29, 2026  
> **Auditor:** GitHub Copilot  
> **Fuentes Analizadas:**
>
> - `docs/process-matrix/` (25 carpetas de procesos)
> - `docs/frontend-rebuild/04-PAGINAS/` (18 documentos)
> - `backend/Gateway/Gateway.Api/ocelot.prod.json` (1661 líneas, 30+ microservicios)
> - `docs/process-matrix/PROCESOS_FALTANTES_UI.md`
> - `docs/frontend-rebuild/ANALISIS-VISTAS-POR-ROL.md`

---

## 📊 RESUMEN EJECUTIVO

### Estado Actual

| Métrica                            | Valor            | Estado              |
| ---------------------------------- | ---------------- | ------------------- |
| **Microservicios en Gateway**      | 30 servicios     | ✅ Backend completo |
| **Carpetas en Process-Matrix**     | 25 categorías    | ✅ Documentado      |
| **Documentos en frontend-rebuild** | 18 páginas       | 🔴 INCOMPLETO       |
| **Vistas documentadas**            | 14 páginas       | 🔴 19%              |
| **Vistas necesarias**              | 73 páginas       | -                   |
| **BRECHA TOTAL**                   | 59 páginas (81%) | 🔴 CRÍTICO          |

### Microservicios Detectados en Gateway (ocelot.prod.json)

| #   | Servicio                   | Prefijo API                                        | UI Documentada  |
| --- | -------------------------- | -------------------------------------------------- | --------------- |
| 1   | aiprocessingservice        | `/api/ai/*`                                        | ❌              |
| 2   | inventorymanagementservice | `/api/inventory/*`                                 | ❌              |
| 3   | errorservice               | `/api/errors/*`                                    | ❌ (admin only) |
| 4   | authservice                | `/api/auth/*`                                      | ✅ 07-auth.md   |
| 5   | notificationservice        | `/api/notifications/*`                             | 🟡 Parcial      |
| 6   | vehiclessaleservice        | `/api/vehicles/*`, `/api/catalog/*`                | ✅              |
| 7   | mediaservice               | `/api/media/*`, `/api/upload/*`                    | 🟡 Parcial      |
| 8   | billingservice             | `/api/billing/*`                                   | ❌              |
| 9   | userservice                | `/api/users/*`, `/api/privacy/*`, `/api/sellers/*` | 🟡              |
| 10  | dealermanagementservice    | `/api/dealers/*`, `/api/subscriptions/*`           | 🟡 06-dealer.md |
| 11  | roleservice                | `/api/roles/*`                                     | ❌              |
| 12  | adminservice               | `/api/admin/*`                                     | ❌              |
| 13  | crmservice                 | `/api/crm/*`                                       | ❌              |
| 14  | reportsservice             | `/api/reports/*`                                   | ❌              |
| 15  | contactservice             | `/api/contactrequests/*`                           | ❌              |
| 16  | comparisonservice          | `/api/vehiclecomparisons/*`                        | ❌              |
| 17  | vehicleintelligenceservice | `/api/vehicleintelligence/*`                       | ❌              |
| 18  | reviewservice              | `/api/reviews/*`                                   | ❌              |
| 19  | recommendationservice      | `/api/recommendations/*`                           | ❌              |
| 20  | chatbotservice             | `/api/chatbot/*`                                   | ❌              |
| 21  | userbehaviorservice        | `/api/userbehavior/*`                              | ❌              |
| 22  | azulpaymentservice         | `/api/azul-payment/*`                              | ❌              |
| 23  | stripepaymentservice       | `/api/stripe-payment/*`                            | ❌              |
| 24  | maintenanceservice         | `/api/maintenance/*`                               | ❌              |
| 25  | alertservice               | `/api/savedsearches/*`, `/api/pricealerts/*`       | ❌              |

---

## 🗂️ MATRIZ DE CRUCE: Process-Matrix vs Frontend-Rebuild

### Carpetas de Process-Matrix y su Estado en Frontend

| #   | Carpeta Process-Matrix            | Documentos FR                                 | Estado                     | Prioridad |
| --- | --------------------------------- | --------------------------------------------- | -------------------------- | --------- |
| 01  | `01-AUTENTICACION-SEGURIDAD/`     | 07-auth.md                                    | ✅ Cubierto                | -         |
| 02  | `02-USUARIOS-DEALERS/`            | 05-dashboard.md, 06-dealer.md                 | 🟡 Parcial                 | P1        |
| 03  | `03-VEHICULOS-INVENTARIO/`        | 02-busqueda.md, 03-detalle.md, 04-publicar.md | 🟡 Falta inventario dealer | P1        |
| 04  | `04-BUSQUEDA-FILTROS/`            | 02-busqueda.md                                | ✅ Cubierto                | -         |
| 04  | `04-BUSQUEDA-RECOMENDACIONES/`    | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 04  | `04-PROPIEDADES-INMUEBLES/`       | N/A (fuera de scope v1)                       | ⚪ Skip                    | -         |
| 05  | `05-AGENDAMIENTO/`                | ❌ FALTA                                      | 🔴 No documentado          | P3        |
| 05  | `05-PAGOS-FACTURACION/`           | ❌ FALTA                                      | 🔴 CRÍTICO                 | P0        |
| 06  | `06-CRM-LEADS-CONTACTOS/`         | ❌ FALTA                                      | 🔴 No documentado          | P1        |
| 06  | `06-PAGOS-FACTURACION/`           | ❌ FALTA                                      | 🔴 CRÍTICO                 | P0        |
| 07  | `07-NOTIFICACIONES/`              | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 07  | `07-REVIEWS-REPUTACION/`          | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 08  | `08-COMPLIANCE-LEGAL-RD/`         | ❌ FALTA                                      | 🔴 CRÍTICO LEGAL           | P0        |
| 09  | `09-NOTIFICACIONES/`              | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 09  | `09-REPORTES-ANALYTICS/`          | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 10  | `10-MEDIA-ARCHIVOS/`              | 04-subida-imagenes.md (API)                   | 🟡 Parcial                 | P3        |
| 11  | `11-INFRAESTRUCTURA-DEVOPS/`      | N/A (no UI)                                   | ⚪ Backend only            | -         |
| 12  | `12-ADMINISTRACION/`              | 12-17 admin-\*.md (vacíos)                    | 🔴 Solo títulos            | P1        |
| 13  | `13-INTEGRACIONES-EXTERNAS/`      | ❌ FALTA                                      | 🔴 No documentado          | P3        |
| 14  | `14-FINANCIAMIENTO-TRADEIN/`      | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 15  | `15-CONFIANZA-SEGURIDAD/`         | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 16  | `16-PROMOCION-VISIBILIDAD/`       | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 17  | `17-ENGAGEMENT-RETENCION/`        | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 18  | `18-SEGUROS/`                     | ❌ FALTA                                      | 🔴 No documentado          | P3        |
| 19  | `19-SOPORTE/`                     | 11-help-center.md (vacío)                     | 🔴 Solo título             | P1        |
| 20  | `20-PRICING-INTELLIGENCE/`        | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 21  | `21-REVIEWS-REPUTACION/`          | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 22  | `22-COMUNICACION-REALTIME/`       | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 23  | `23-PROCESAMIENTO-360-VEHICULOS/` | 06-vehicle-360-viewer.md                      | ✅ Recién creado           | -         |
| 24  | `24-CHATBOT-IA/`                  | ❌ FALTA                                      | 🔴 No documentado          | P2        |
| 25  | `25-AUDITORIA-CUMPLIMIENTO/`      | ❌ FALTA                                      | 🔴 CRÍTICO LEGAL           | P0        |

---

## 🔴 PRIORIDAD P0: CRÍTICO (Legal/Operacional)

Documentos que **DEBEN** existir para operación legal:

### 1. `19-pagos-checkout.md` (NUEVO)

**Microservicios:** billingservice, azulpaymentservice, stripepaymentservice  
**APIs en Gateway:**

- `/api/billing/*` - BillingService
- `/api/azul-payment/*` - AZUL (Banco Popular)
- `/api/stripe-payment/*` - Stripe

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Checkout | `/checkout` | USR-REG |
| Métodos de pago | `/settings/payment-methods` | USR-REG |
| Historial pagos | `/dashboard/pagos` | USR-REG |
| Facturación dealer | `/dealer/facturacion` | DLR-ADMIN |
| Admin billing | `/admin/billing` | ADM-ADMIN |

### 2. `15-admin-compliance.md` (EXPANDIR)

**Microservicio:** complianceservice (NO en gateway actual)  
**Carpetas process-matrix:**

- `08-COMPLIANCE-LEGAL-RD/`
- `25-AUDITORIA-CUMPLIMIENTO/`

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Dashboard Compliance | `/admin/compliance` | ADM-COMP |
| Reporte 607 DGII | `/admin/compliance/dgii-607` | ADM-COMP |
| Alertas AML | `/admin/compliance/aml` | ADM-COMP |
| Calendario Fiscal | `/admin/compliance/calendar` | ADM-COMP |
| KYC Pendientes | `/admin/compliance/kyc` | ADM-COMP |
| Protección de Datos | `/admin/compliance/data-protection` | ADM-COMP |
| Auditorías | `/admin/compliance/audits` | ADM-COMP |
| Watchlist Check | `/admin/compliance/watchlist` | ADM-COMP |

### 3. `16-admin-support.md` (EXPANDIR) + SupportService Backend

**Microservicio:** ❌ **NO EXISTE EN GATEWAY** - CREAR PRIMERO

**Backend requerido:**

```
backend/SupportService/
├── SupportService.Api/
│   └── Controllers/
│       ├── TicketsController.cs
│       ├── ArticlesController.cs
│       └── LiveChatController.cs
├── SupportService.Application/
├── SupportService.Domain/
└── SupportService.Infrastructure/
```

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Help Center (público) | `/help` | USR-ANON |
| FAQ | `/help/faq` | USR-ANON |
| Mis tickets | `/help/tickets` | USR-REG |
| Dashboard soporte | `/admin/support` | ADM-SUPPORT |
| Gestión tickets | `/admin/support/tickets` | ADM-SUPPORT |
| Gestión FAQ | `/admin/support/faq` | ADM-SUPPORT |
| Live Chat Admin | `/admin/support/chat` | ADM-SUPPORT |

---

## 🟠 PRIORIDAD P1: ALTA (Monetización/Operación)

### 4. `09-dealer-inventario.md` (EXPANDIR)

**Microservicio:** inventorymanagementservice  
**APIs en Gateway:**

- `/api/inventory/*` - CRUD inventario
- `/api/inventory/bulkimport/*` - Import masivo

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Lista inventario | `/dealer/inventario` | DLR-STAFF, DLR-ADMIN |
| Nuevo vehículo | `/dealer/inventario/nuevo` | DLR-STAFF |
| Editar vehículo | `/dealer/inventario/[id]` | DLR-STAFF |
| Import CSV | `/dealer/inventario/import` | DLR-ADMIN |
| Estadísticas | `/dealer/inventario/stats` | DLR-ADMIN |

### 5. `10-dealer-crm.md` (EXPANDIR)

**Microservicio:** crmservice, contactservice  
**APIs en Gateway:**

- `/api/crm/*` - CRM completo
- `/api/contactrequests/*` - Solicitudes de contacto

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Dashboard CRM | `/dealer/crm` | DLR-ADMIN |
| Leads | `/dealer/crm/leads` | DLR-STAFF |
| Lead detail | `/dealer/crm/leads/[id]` | DLR-STAFF |
| Pipeline | `/dealer/crm/pipeline` | DLR-ADMIN |
| Actividades | `/dealer/crm/actividades` | DLR-STAFF |

### 6. `12-admin-dashboard.md` (EXPANDIR)

**Microservicio:** adminservice, reportsservice  
**APIs en Gateway:**

- `/api/admin/*` - Admin general
- `/api/reports/*` - Reportes

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Dashboard Admin | `/admin` | ADM-ADMIN |
| Usuarios | `/admin/users` | ADM-ADMIN |
| Dealers | `/admin/dealers` | ADM-ADMIN |
| Listings | `/admin/listings` | ADM-ADMIN |
| Reportes | `/admin/reports` | ADM-ADMIN |
| Configuración | `/admin/settings` | ADM-ADMIN |

### 7. `14-admin-moderation.md` (EXPANDIR)

**Microservicio:** adminservice  
**APIs:** `/api/admin/moderation/*`

**Vistas necesarias:**
| Vista | Ruta | Rol |
|-------|------|-----|
| Cola moderación | `/admin/moderation/queue` | ADM-MOD |
| Pendientes | `/admin/moderation/pending` | ADM-MOD |
| Reportes contenido | `/admin/moderation/reports` | ADM-MOD |
| Historial | `/admin/moderation/history` | ADM-MOD |
| Usuarios flaggeados | `/admin/moderation/users` | ADM-MOD |

---

## 🟡 PRIORIDAD P2: MEDIA (Features Diferenciadores)

### 8. `20-reviews-reputacion.md` (NUEVO)

**Microservicio:** reviewservice  
**APIs:** `/api/reviews/*`

**Vistas:**

- Reviews de dealers `/dealers/[slug]/reviews`
- Escribir review `/dealers/[slug]/reviews/new`
- Mis reviews `/dashboard/reviews`
- Admin reviews `/admin/reviews`

### 9. `21-recomendaciones.md` (NUEVO)

**Microservicio:** recommendationservice, userbehaviorservice  
**APIs:** `/api/recommendations/*`, `/api/userbehavior/*`

**Vistas:**

- Sección "Para Ti" en home
- "Vehículos similares" en detalle
- Historial de vistas `/dashboard/historial`

### 10. `22-chatbot.md` (NUEVO)

**Microservicio:** chatbotservice  
**APIs:** `/api/chatbot/*`

**Vistas:**

- Widget chatbot (flotante en todas las páginas)
- Admin chatbot `/admin/chatbot`
- Conversaciones `/admin/chatbot/conversations`

### 11. `23-comparador.md` (NUEVO)

**Microservicio:** comparisonservice  
**APIs:** `/api/vehiclecomparisons/*`

**Vistas:**

- Comparador `/comparar`
- Comparación guardada `/comparar/[id]`

### 12. `24-alertas-busquedas.md` (NUEVO)

**Microservicio:** alertservice  
**APIs:** `/api/savedsearches/*`, `/api/pricealerts/*`

**Vistas:**

- Alertas precio `/dashboard/alertas/precios`
- Búsquedas guardadas `/dashboard/alertas/busquedas`
- Configuración notificaciones `/settings/notifications`

### 13. `25-notificaciones.md` (NUEVO)

**Microservicio:** notificationservice  
**APIs:** `/api/notifications/*`, `/api/templates/*`

**Vistas:**

- Centro notificaciones `/notifications`
- Preferencias `/settings/notifications`
- Admin templates `/admin/notifications/templates`

---

## 🟢 PRIORIDAD P3: BAJA (Nice-to-have)

### 14. `17-admin-system.md` (EXPANDIR)

**Microservicios:** maintenanceservice, errorservice  
**APIs:** `/api/maintenance/*`, `/api/errors/*`

**Vistas:**

- Modo mantenimiento `/admin/maintenance`
- Logs de errores `/admin/errors`
- Health check `/admin/health`
- Feature flags `/admin/features`
- API keys `/admin/api-keys`
- Jobs/Scheduler `/admin/jobs`

### 15. Otros documentos menores

- `26-agendamiento.md` - Test drives
- `27-financiamiento.md` - Integraciones bancos
- `28-seguros.md` - Cotizaciones seguros
- `29-pricing-intelligence.md` - Sugerencias de precio IA

---

## 📋 CHECKLIST DE DOCUMENTOS A CREAR/EXPANDIR

### ✅ Ya Existen - COMPLETOS (800+ líneas)

| Documento              | Líneas | Estado           |
| ---------------------- | ------ | ---------------- |
| 03-detalle-vehiculo.md | 1150   | ✅ Completo      |
| 02-busqueda.md         | 1066   | ✅ Completo      |
| 04-publicar.md         | 1060   | ✅ Completo      |
| 01-home.md             | 894    | ✅ Completo      |
| 18-vehicle-360-page.md | 804    | ✅ Recién creado |

### 🟡 Ya Existen - PARCIALES (400-600 líneas)

| Documento               | Líneas | Falta                     |
| ----------------------- | ------ | ------------------------- |
| 17-admin-system.md      | 602    | Logs viewer, backups      |
| 16-admin-support.md     | 520    | Live chat, métricas       |
| 14-admin-moderation.md  | 513    | Queue avanzada            |
| 05-dashboard.md         | 456    | Historial, alertas        |
| 15-admin-compliance.md  | 455    | DGII 607, AML, calendario |
| 07-auth.md              | 446    | OAuth social, recovery    |
| 09-dealer-inventario.md | 439    | Bulk import UI            |
| 13-admin-users.md       | 422    | Bulk actions              |
| 06-dealer-dashboard.md  | 416    | Facturación               |

### 🔴 Ya Existen - MÍNIMOS (< 400 líneas)

| Documento             | Líneas | Acción Requerida                   |
| --------------------- | ------ | ---------------------------------- |
| 10-dealer-crm.md      | 372    | **Expandir pipeline, actividades** |
| 11-help-center.md     | 366    | **Expandir FAQ editor, tickets**   |
| 12-admin-dashboard.md | 350    | **Expandir KPIs, gráficos**        |
| 08-perfil.md          | 293    | **Expandir settings avanzados**    |

### 🔴 CREAR NUEVOS (No existen)

- [ ] 19-pagos-checkout.md (flujo Stripe + AZUL) **← P0 CRÍTICO**
- [ ] 20-reviews-reputacion.md
- [ ] 21-recomendaciones.md
- [ ] 22-chatbot.md
- [ ] 23-comparador.md
- [ ] 24-alertas-busquedas.md
- [ ] 25-notificaciones.md

### Componentes (03-COMPONENTES)

- [x] 06-vehicle-360-viewer.md ✅ (recién creado)

### API Integration (05-API-INTEGRATION)

- [x] 05-vehicle-360-api.md ✅ (recién creado)

---

## 🎯 RESUMEN DE ACCIONES POR PRIORIDAD

### 🔴 P0: CREAR NUEVO (1 documento)

| Documento            | Servicios Gateway     | Vistas | Estimado |
| -------------------- | --------------------- | ------ | -------- |
| 19-pagos-checkout.md | billing, azul, stripe | 5      | 4 horas  |

### 🟠 P1: EXPANDIR (4 documentos < 400 líneas)

| Documento             | Líneas Actuales | Líneas Objetivo | Falta Agregar           |
| --------------------- | --------------- | --------------- | ----------------------- |
| 10-dealer-crm.md      | 372             | 600+            | Pipeline, actividades   |
| 11-help-center.md     | 366             | 600+            | FAQ editor, mis tickets |
| 12-admin-dashboard.md | 350             | 600+            | KPIs, gráficos          |
| 08-perfil.md          | 293             | 500+            | Privacy, settings       |

### 🟡 P2: CREAR NUEVOS (7 documentos)

| Documento                | Servicios Gateway         | Prioridad |
| ------------------------ | ------------------------- | --------- |
| 20-reviews-reputacion.md | reviewservice             | Alta      |
| 21-recomendaciones.md    | recommendationservice     | Alta      |
| 22-chatbot.md            | chatbotservice            | Media     |
| 23-comparador.md         | comparisonservice         | Alta      |
| 24-alertas-busquedas.md  | alertservice              | Alta      |
| 25-notificaciones.md     | notificationservice       | Media     |
| 26-privacy-gdpr.md       | userservice (privacy API) | Alta      |

### 🟢 P3: EXPANDIR PARCIALES (mejoras opcionales)

| Documento               | Líneas Actuales | Mejoras Sugeridas     |
| ----------------------- | --------------- | --------------------- |
| 15-admin-compliance.md  | 455             | +DGII 607, calendario |
| 17-admin-system.md      | 602             | +Logs viewer, backups |
| 09-dealer-inventario.md | 439             | +Bulk import wizard   |

---

## 🔧 PLAN DE EJECUCIÓN

### Fase 1: P0 Crítico (Semana 1-2)

```
Día 1-2:
├── Expandir 15-admin-compliance.md
├── Documentar 8 vistas de compliance
└── Mapear a APIs existentes (o documentar que faltan)

Día 3-4:
├── Crear 19-pagos-checkout.md
├── Documentar flujo Stripe + AZUL
└── 5 vistas de pagos

Día 5-7:
├── Documentar backend SupportService (spec)
├── Expandir 11-help-center.md
├── Expandir 16-admin-support.md
└── 7 vistas de soporte
```

### Fase 2: P1 Monetización (Semana 3-4)

```
Día 8-10:
├── Expandir 09-dealer-inventario.md
├── Documentar integración con inventorymanagementservice
└── 5 vistas de inventario

Día 11-12:
├── Expandir 10-dealer-crm.md
├── Documentar integración con crmservice
└── 5 vistas CRM

Día 13-14:
├── Expandir 12-admin-dashboard.md
├── Expandir 13-admin-users.md
├── Expandir 14-admin-moderation.md
└── 15 vistas admin
```

### Fase 3: P2 Features (Semana 5-6)

```
Día 15-18:
├── Crear 20-reviews-reputacion.md
├── Crear 21-recomendaciones.md
├── Crear 22-chatbot.md
└── 10 vistas

Día 19-21:
├── Crear 23-comparador.md
├── Crear 24-alertas-busquedas.md
├── Crear 25-notificaciones.md
└── 8 vistas
```

### Fase 4: P3 Nice-to-have (Semana 7+)

```
Resto de documentos según capacity
```

---

## 📊 MÉTRICAS DE ÉXITO

### Antes de la Auditoría

- Documentos: 18
- Vistas cubiertas: 14 (19%)
- Servicios mapeados: 5/30 (17%)

### Después de Completar P0-P1

- Documentos: 27 (+9)
- Vistas cubiertas: 45 (62%)
- Servicios mapeados: 18/30 (60%)

### Después de Completar Todo

- Documentos: 35 (+17)
- Vistas cubiertas: 73 (100%)
- Servicios mapeados: 25/30 (83%)

---

## ⚠️ GAPS DE BACKEND IDENTIFICADOS

### Servicios en Gateway SIN UI documentada:

1. ✅ aiprocessingservice - Usado internamente
2. ❌ complianceservice - **NO ESTÁ EN GATEWAY** (AGREGAR)
3. ❌ supportservice - **NO EXISTE** (CREAR)
4. ❌ auditservice - **NO ESTÁ EN GATEWAY** (AGREGAR)

### Servicios en Process-Matrix SIN Gateway route:

1. ComplianceService (puerto 5073)
2. AntiMoneyLaunderingService (puerto 5074)
3. ComplianceReportingService (puerto 5075)
4. TaxComplianceService (puerto 5076)
5. AuditService

**Acción:** Agregar rutas en `ocelot.prod.json`:

```json
{
  "UpstreamPathTemplate": "/api/compliance/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/compliance/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "complianceservice", "Port": 8080 }],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "RouteClaimsRequirement": { "role": "Admin,Compliance" }
}
```

---

## 📝 NOTAS FINALES

1. **El backend está más completo que el frontend** - 30 servicios vs 18 docs
2. **Compliance es crítico legal** - Sin UI, los procesos no son auditables
3. **SupportService NO EXISTE** - Único servicio que requiere creación completa
4. **Los docs existentes son de alta calidad** - Solo expandir, no reescribir
5. **El flujo 360° recién documentado** es un buen ejemplo del nivel esperado

---

**Próximo paso:** Ejecutar `Fase 1: Día 1-2` → Expandir `15-admin-compliance.md`
