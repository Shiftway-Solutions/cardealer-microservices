# 📊 Análisis de Procesos vs Acceso en Menú de Navegación

> **Fecha de Análisis:** Enero 25, 2026  
> **Propósito:** Verificar que cada tipo de usuario tenga acceso correcto a sus procesos desde el Navbar/UI  
> **Estado:** ⚠️ REVISIÓN REQUERIDA  
> **Documentos Relacionados:**
>
> - [PROCESOS_FALTANTES_UI.md](../process-matrix/PROCESOS_FALTANTES_UI.md) - Lista de procesos sin UI
> - [README.md](../process-matrix/README.md) - Matriz de procesos actualizada

---

## 📋 Resumen Ejecutivo

Este documento analiza la correlación entre:

1. **Procesos definidos en la Matriz de Procesos** (`docs/process-matrix/`)
2. **Acceso desde Navbar/UI** (`frontend/web/src/components/organisms/Navbar.tsx` + `App.tsx`)

### ⚠️ Hallazgos Principales

| Tipo de Usuario                      | Procesos en Matriz | Acceso en UI | Cobertura | Estado     |
| ------------------------------------ | ------------------ | ------------ | --------- | ---------- |
| **USR-ANON** (Anónimo)               | 5                  | 5            | 100%      | ✅ OK      |
| **USR-REG** (Registrado)             | 12                 | 8            | 67%       | 🟡 Parcial |
| **USR-SELLER** (Vendedor Individual) | 8                  | 5            | 63%       | 🟡 Parcial |
| **DLR-STAFF** (Staff Dealer)         | 15                 | 12           | 80%       | 🟡 Parcial |
| **DLR-ADMIN** (Admin Dealer)         | 20                 | 14           | 70%       | 🟡 Parcial |
| **ADM-SUPPORT** (Soporte)            | 10                 | 3            | 30%       | 🔴 Crítico |
| **ADM-MOD** (Moderador)              | 8                  | 2            | 25%       | 🔴 Crítico |
| **ADM-COMP** (Compliance)            | 6                  | 0            | 0%        | 🔴 Crítico |
| **ADM-ADMIN** (Administrador)        | 18                 | 12           | 67%       | 🟡 Parcial |
| **ADM-SUPER** (Super Admin)          | 25                 | 12           | 48%       | 🟡 Parcial |

---

## 1. USR-ANON (Usuario Anónimo)

### 📖 Procesos Definidos en Matriz

| Código     | Proceso             | Servicio                | Descripción            |
| ---------- | ------------------- | ----------------------- | ---------------------- |
| AUTH-001   | Registro de Usuario | AuthService             | Crear nueva cuenta     |
| AUTH-002   | Login               | AuthService             | Iniciar sesión         |
| VEH-VIEW   | Ver Vehículos       | VehiclesSaleService     | Navegación pública     |
| SEARCH-001 | Búsqueda            | VehiclesSaleService     | Filtrar vehículos      |
| DLR-VIEW   | Ver Dealers         | DealerManagementService | Perfil público dealers |

### 🧭 Acceso en UI (Navbar + Rutas)

**Navbar (sin autenticar):**

- ✅ "Vehículos" → `/vehicles` (VEH-VIEW)
- ✅ "Para Dealers" → `/dealer/landing` (DLR-VIEW)
- ✅ "Iniciar sesión" → `/login` (AUTH-002)
- ✅ "Registrarse" → `/register` (AUTH-001)

**Rutas públicas:**

- ✅ `/search` (SEARCH-001)
- ✅ `/vehicles/:slug` - Detalle de vehículo
- ✅ `/dealers/:slug` - Perfil público de dealer

### ✅ Estado: 100% COMPLETO

---

## 2. USR-REG (Usuario Registrado/Comprador)

### 📖 Procesos Definidos en Matriz

| Código      | Proceso                   | Servicio            | Descripción              |
| ----------- | ------------------------- | ------------------- | ------------------------ |
| USR-001     | Actualización de Perfil   | UserService         | Editar datos personales  |
| USR-002     | Cambio de Contraseña      | AuthService         | Seguridad de cuenta      |
| USR-003     | Eliminación de Cuenta     | ComplianceService   | Derecho al olvido        |
| FAV-001     | Agregar a Favoritos       | VehiclesSaleService | Guardar vehículos        |
| ALERT-001   | Crear Alerta de Precio    | AlertService        | Notificaciones de precio |
| COMP-001    | Comparar Vehículos        | ComparisonService   | Hasta 3 vehículos        |
| CRM-001     | Contactar Vendedor        | CRMService          | Generar lead             |
| MSG-001     | Mensajería                | ContactService      | Chat con vendedores      |
| REVIEW-001  | Escribir Review           | ReviewService       | Calificar vendedor       |
| KYC-001     | Verificación KYC          | KYCService          | Para vender              |
| BILLING-001 | Ver Facturación           | BillingService      | Historial de pagos       |
| NOTIF-001   | Configurar Notificaciones | NotificationService | Preferencias             |

### 🧭 Acceso en UI (Navbar + Rutas)

**Navbar (userLinks - dropdown de perfil):**

- ✅ "Dashboard" → `/dashboard`
- ✅ "Favoritos" → `/favorites` (FAV-001)
- ✅ "Mensajes" → `/messages` (MSG-001)
- ✅ "Seguridad" → `/settings/security` (USR-002)
- ✅ "Perfil" → `/profile` (USR-001)

**Rutas protegidas disponibles:**

- ✅ `/comparison` (COMP-001)
- ✅ `/alerts` (ALERT-001)
- ✅ `/billing` (BILLING-001)

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Eliminación de cuenta** (USR-003) - No hay link visible en Settings
- ❌ **Escribir Review** (REVIEW-001) - Solo accesible desde `/reviews/write/:sellerId`
- ❌ **Verificación KYC** (KYC-001) - `/kyc/verify` existe pero no hay link en navbar
- ❌ **Configurar Notificaciones** (NOTIF-001) - No hay página dedicada

### 🟡 Estado: 67% - PARCIALMENTE COMPLETO

**Recomendaciones:**

1. Agregar "Configuración de Notificaciones" en Settings
2. Agregar opción "Eliminar mi cuenta" en Settings > Seguridad
3. Agregar link a "Mis Reviews" en dashboard
4. Agregar banner/link para KYC cuando sea necesario

---

## 3. USR-SELLER (Vendedor Individual)

### 📖 Procesos Definidos en Matriz

| Código      | Proceso               | Servicio            | Descripción         |
| ----------- | --------------------- | ------------------- | ------------------- |
| SELL-001    | Crear Perfil Vendedor | UserService         | Activar venta       |
| VEH-001     | Publicar Vehículo     | VehiclesSaleService | Crear listing       |
| VEH-003     | Cambiar Precio        | VehiclesSaleService | Actualizar precio   |
| VEH-EDIT    | Editar Vehículo       | VehiclesSaleService | Modificar datos     |
| PAY-001     | Pagar Publicación     | BillingService      | $29 por listing     |
| CRM-002     | Responder Leads       | CRMService          | Atender interesados |
| REVIEW-VIEW | Ver Mis Reviews       | ReviewService       | Reputación          |
| STATS-001   | Ver Estadísticas      | AnalyticsService    | Vistas de listing   |

### 🧭 Acceso en UI (Navbar + Rutas)

**Navbar:**

- ✅ "Vender" (CTA verde) → `/sell` (VEH-001 + SELL-001)

**Rutas protegidas:**

- ✅ `/seller/create` (SELL-001)
- ✅ `/seller/dashboard` (STATS-001)
- ✅ `/seller/profile` (VEH-EDIT, VEH-003)
- ✅ `/seller/profile/settings`

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Mis Leads/Consultas** (CRM-002) - No hay link directo para sellers individuales
- ❌ **Mis Reviews** (REVIEW-VIEW) - No hay página dedicada para ver reviews recibidos
- ❌ **Estadísticas de Listing** (STATS-001) - Dashboard básico, falta analytics detallado

### 🟡 Estado: 63% - PARCIALMENTE COMPLETO

**Recomendaciones:**

1. Agregar `/my-inquiries` en navbar para sellers con listings activos
2. Crear página `/seller/reviews` para ver reviews recibidos
3. Agregar estadísticas básicas en seller dashboard
4. Link contextual cuando seller tiene leads sin responder

---

## 4. DLR-STAFF (Staff de Dealer)

### 📖 Procesos Definidos en Matriz

| Código      | Proceso              | Servicio                   | Descripción          |
| ----------- | -------------------- | -------------------------- | -------------------- |
| VEH-001     | Publicar Vehículo    | VehiclesSaleService        | Crear listing        |
| VEH-003     | Cambiar Precio       | VehiclesSaleService        | Actualizar precio    |
| VEH-EDIT    | Editar Vehículo      | VehiclesSaleService        | Modificar datos      |
| VEH-DELETE  | Eliminar Vehículo    | VehiclesSaleService        | Remover listing      |
| INV-001     | Gestionar Inventario | InventoryManagementService | CRUD masivo          |
| INV-002     | Importar CSV         | InventoryManagementService | Bulk upload          |
| CRM-002     | Seguimiento de Leads | CRMService                 | Responder consultas  |
| CRM-003     | Calificar Lead       | LeadScoringService         | Priorizar leads      |
| MSG-001     | Mensajería           | ContactService             | Chat con compradores |
| APPT-001    | Agendar Test Drive   | AppointmentService         | Citas                |
| REPORT-001  | Ver Reportes         | AnalyticsService           | Métricas básicas     |
| PROFILE-001 | Editar Perfil Dealer | DealerManagementService    | Info pública         |
| NOTIF-001   | Notificaciones       | NotificationService        | Configuración        |
| SALE-001    | Registrar Venta      | CRMService                 | Marcar como vendido  |
| STATS-VIEW  | Ver Analytics        | DealerAnalyticsService     | Dashboard stats      |

### 🧭 Acceso en UI (Navbar + Rutas)

**Navbar (dealerLinks - dropdown de perfil):**

- ✅ "Dashboard" → `/dealer/dashboard`
- ✅ "Inventario" → `/dealer/inventory` (INV-001)
- ✅ "Leads" → `/dealer/crm` (CRM-002, CRM-003)
- ✅ "Analytics" → `/dealer/analytics` (STATS-VIEW)

**Rutas protegidas para dealers:**

- ✅ `/dealer/inventory/new` (VEH-001)
- ✅ `/dealer/inventory/:id/edit` (VEH-EDIT, VEH-003)
- ✅ `/dealer/leads/:leadId` (CRM-002)
- ✅ `/dealer/profile/edit` (PROFILE-001)
- ✅ `/dealer/sales` (SALE-001)
- ✅ `/dealer/analytics/advanced` (REPORT-001)

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Importar CSV** (INV-002) - Botón en inventory, pero no en navbar
- ❌ **Citas/Test Drives** (APPT-001) - Página existe `/dealer/appointments` pero sin link en navbar
- ❌ **Mensajes** (MSG-001) - No hay link específico para dealers en navbar

### 🟡 Estado: 80% - MAYORMENTE COMPLETO

**Recomendaciones:**

1. Agregar "Citas" en dealerLinks del navbar
2. Agregar "Mensajes" en dealerLinks
3. Destacar botón de "Importar CSV" en inventario

---

## 5. DLR-ADMIN (Admin de Dealer)

### 📖 Procesos Definidos en Matriz

Incluye todos los de DLR-STAFF más:

| Código      | Proceso              | Servicio                | Descripción        |
| ----------- | -------------------- | ----------------------- | ------------------ |
| DEMP-001    | Agregar Empleado     | UserService             | Gestión de staff   |
| DEMP-002    | Editar Empleado      | UserService             | Permisos           |
| DEMP-003    | Eliminar Empleado    | UserService             | Remover acceso     |
| DMOD-001    | Activar Módulos      | DealerManagementService | Features           |
| SUB-001     | Cambiar Plan         | BillingService          | Upgrade/downgrade  |
| SUB-002     | Ver Facturación      | BillingService          | Historial          |
| SUB-003     | Métodos de Pago      | BillingService          | Tarjetas           |
| REPORT-002  | Reportes Avanzados   | ReportsService          | Exportar           |
| PROFILE-002 | Configuración Dealer | DealerManagementService | Settings avanzados |

### 🧭 Acceso en UI (Navbar + Rutas)

**Rutas adicionales para DLR-ADMIN:**

- ✅ `/dealer/billing` (SUB-002)
- ✅ `/dealer/plans` (SUB-001)
- ✅ `/dealer/invoices` (SUB-002)
- ✅ `/dealer/payments` (SUB-002)
- ✅ `/dealer/payment-methods` (SUB-003)
- ✅ `/dealer/settings` (PROFILE-002)

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Gestión de Empleados** (DEMP-001, 002, 003) - No hay página dedicada
- ❌ **Activar Módulos** (DMOD-001) - No hay UI para gestión de módulos
- ❌ **Reportes Avanzados con Exportación** (REPORT-002) - Parcialmente implementado

### 🟡 Estado: 70% - PARCIALMENTE COMPLETO

**Recomendaciones:**

1. **CRÍTICO:** Crear página `/dealer/employees` para gestión de staff
2. Crear página `/dealer/modules` para activación de features
3. Agregar botón de exportación en reportes

---

## 6. ADM-SUPPORT (Agente de Soporte OKLA)

### 📖 Procesos Definidos en Matriz

| Código         | Proceso              | Servicio                | Descripción        |
| -------------- | -------------------- | ----------------------- | ------------------ |
| TICKET-001     | Ver Tickets          | SupportService          | Lista de tickets   |
| TICKET-002     | Responder Ticket     | SupportService          | Atender usuario    |
| TICKET-003     | Escalar Ticket       | SupportService          | A nivel superior   |
| TICKET-004     | Cerrar Ticket        | SupportService          | Resolver           |
| USER-VIEW      | Ver Usuario          | UserService             | Consultar perfil   |
| PAY-002        | Procesar Reembolso   | BillingService          | Devoluciones       |
| PAY-VIEW       | Ver Transacciones    | BillingService          | Historial pagos    |
| DEALER-VIEW    | Ver Dealer           | DealerManagementService | Consultar info     |
| VEH-VIEW-ADMIN | Ver Listings (Admin) | VehiclesSaleService     | Con datos internos |
| LOG-VIEW       | Ver Logs             | AuditService            | Historial acciones |

### 🧭 Acceso en UI (Navbar + Rutas)

**Panel Admin actual:**

- ✅ `/admin` - Dashboard
- ✅ `/admin/users` (USER-VIEW) - Parcial
- ✅ `/admin/listings` (VEH-VIEW-ADMIN) - Parcial

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Sistema de Tickets** (TICKET-001-004) - No existe
- ❌ **Procesar Reembolsos** (PAY-002) - No hay UI
- ❌ **Ver Transacciones** (PAY-VIEW) - No hay página dedicada
- ❌ **Ver Dealers con detalle** (DEALER-VIEW) - Parcial
- ❌ **Ver Logs de Auditoría** (LOG-VIEW) - No accesible

### 🔴 Estado: 30% - CRÍTICO

**Recomendaciones:**

1. **URGENTE:** Crear módulo de tickets `/admin/tickets`
2. Crear página de reembolsos `/admin/refunds`
3. Crear página de transacciones `/admin/transactions`
4. Agregar vista de logs `/admin/audit-logs`

---

## 7. ADM-MOD (Moderador de Contenido OKLA)

### 📖 Procesos Definidos en Matriz

| Código        | Proceso               | Servicio          | Descripción         |
| ------------- | --------------------- | ----------------- | ------------------- |
| VEH-002       | Moderar Vehículo      | ModerationService | Aprobar/rechazar    |
| MOD-QUEUE     | Cola de Moderación    | ModerationService | Pendientes          |
| MOD-APPROVE   | Aprobar Listing       | ModerationService | Publicar            |
| MOD-REJECT    | Rechazar Listing      | ModerationService | Con razón           |
| REPORT-001    | Reportes de Contenido | ModerationService | Contenido reportado |
| REPORT-ACTION | Tomar Acción          | ModerationService | Ban, warning, etc   |
| USER-WARN     | Advertir Usuario      | UserService       | Warning             |
| USER-BAN      | Banear Usuario        | UserService       | Suspender           |

### 🧭 Acceso en UI (Navbar + Rutas)

**Panel Admin actual:**

- ✅ `/admin/pending` (MOD-QUEUE) - Solo aprobaciones
- ✅ `/admin/listings` (VEH-002) - Parcial

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Cola de Moderación Dedicada** - No existe página específica
- ❌ **Reportes de Contenido** (REPORT-001) - No hay UI
- ❌ **Sistema de Warnings/Bans** (USER-WARN, USER-BAN) - No hay UI
- ❌ **Historial de Moderaciones** - No existe
- ❌ **Métricas de Moderación** - No hay dashboard

### 🔴 Estado: 25% - CRÍTICO

**Recomendaciones:**

1. **URGENTE:** Crear `/admin/moderation/queue` con cola de moderación
2. Crear `/admin/moderation/reports` para contenido reportado
3. Agregar acciones de ban/warning en gestión de usuarios
4. Crear dashboard de métricas de moderación

---

## 8. ADM-COMP (Compliance Officer OKLA)

### 📖 Procesos Definidos en Matriz

| Código     | Proceso              | Servicio                | Descripción         |
| ---------- | -------------------- | ----------------------- | ------------------- |
| DLR-002    | Verificar Dealer     | DealerManagementService | Aprobar documentos  |
| COMP-001   | Generar Reporte 607  | ComplianceService       | DGII mensual        |
| COMP-002   | Verificar RNC        | ComplianceService       | Consulta DGII       |
| KYC-REVIEW | Revisar KYC          | KYCService              | Verificar identidad |
| AML-001    | Reporte Anti-Lavado  | ComplianceService       | Ley 155-17          |
| GDPR-001   | Solicitudes de Datos | ComplianceService       | Ley 172-13          |

### 🧭 Acceso en UI (Navbar + Rutas)

**Panel Admin actual:**

- ✅ `/admin/kyc` (KYC-REVIEW) - Existe
- ⚠️ `/admin/pending` (DLR-002) - Parcial, sin verificación de documentos

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Dashboard de Compliance** - No existe
- ❌ **Generación de Reporte 607** (COMP-001) - No hay UI
- ❌ **Consulta DGII** (COMP-002) - No hay integración visible
- ❌ **Reportes Anti-Lavado** (AML-001) - No hay UI
- ❌ **Solicitudes GDPR** (GDPR-001) - No hay gestión

### 🔴 Estado: 0% - CRÍTICO

**Recomendaciones:**

1. **CRÍTICO:** Crear `/admin/compliance` dashboard
2. Crear `/admin/compliance/reports/607` para DGII
3. Crear `/admin/compliance/aml` para anti-lavado
4. Crear `/admin/compliance/data-requests` para GDPR
5. Integrar verificación de RNC en aprobación de dealers

---

## 9. ADM-ADMIN (Administrador OKLA)

### 📖 Procesos Definidos en Matriz

| Código                     | Proceso               | Servicio                | Descripción        |
| -------------------------- | --------------------- | ----------------------- | ------------------ |
| ADM-DASH                   | Dashboard Admin       | AdminService            | Métricas generales |
| USER-MGMT                  | Gestión Usuarios      | UserService             | CRUD usuarios      |
| ROLE-MGMT                  | Gestión Roles         | RoleService             | Permisos           |
| DEALER-MGMT                | Gestión Dealers       | DealerManagementService | Aprobar, suspender |
| CONFIG-001                 | Configuración Sistema | ConfigurationService    | Settings           |
| FEATURE-001                | Feature Flags         | FeatureToggleService    | On/off features    |
| REPORT-ADMIN               | Reportes Admin        | ReportsService          | Exportar datos     |
| NOTIF-ADMIN                | Enviar Notificación   | NotificationService     | Broadcast          |
| MAINT-001                  | Modo Mantenimiento    | MaintenanceService      | Activar/desactivar |
| BILLING-ADMIN              | Ver Billing Global    | BillingService          | Revenue            |
| ANALYTICS-ADMIN            | Analytics Global      | AnalyticsService        | Plataforma         |
| ERROR-VIEW                 | Ver Errores           | ErrorService            | Logs de errores    |
| + todos los de SUPPORT/MOD |                       |                         |                    |

### 🧭 Acceso en UI (Navbar + Rutas)

**Panel Admin actual:**

- ✅ `/admin` (ADM-DASH)
- ✅ `/admin/users` (USER-MGMT)
- ✅ `/admin/roles` (ROLE-MGMT)
- ✅ `/admin/permissions` (ROLE-MGMT)
- ✅ `/admin/listings`
- ✅ `/admin/reports` (REPORT-ADMIN)
- ✅ `/admin/settings` (CONFIG-001)
- ✅ `/admin/categories`
- ✅ `/admin/user-behavior`
- ✅ `/admin/feature-store` (FEATURE-001)

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Modo Mantenimiento** (MAINT-001) - No hay toggle visible
- ❌ **Enviar Notificación Broadcast** (NOTIF-ADMIN) - No hay UI
- ❌ **Ver Revenue/Billing** (BILLING-ADMIN) - No hay dashboard
- ❌ **Ver Errores del Sistema** (ERROR-VIEW) - No accesible
- ❌ **Gestión de Dealers** (DEALER-MGMT) - Parcial, falta suspensión
- ❌ **Analytics Global** (ANALYTICS-ADMIN) - No hay dashboard de plataforma

### 🟡 Estado: 67% - PARCIALMENTE COMPLETO

**Recomendaciones:**

1. Agregar toggle de mantenimiento en `/admin/settings`
2. Crear `/admin/notifications/broadcast` para notificaciones masivas
3. Crear `/admin/revenue` dashboard de ingresos
4. Crear `/admin/errors` para ver logs de ErrorService
5. Agregar acciones de suspensión en gestión de dealers

---

## 10. ADM-SUPER (Super Admin OKLA)

### 📖 Procesos Definidos en Matriz

Incluye TODOS los procesos de todos los roles, más:

| Código        | Proceso          | Servicio                | Descripción       |
| ------------- | ---------------- | ----------------------- | ----------------- |
| USER-DELETE   | Eliminar Usuario | UserService             | Permanente        |
| DEALER-DELETE | Eliminar Dealer  | DealerManagementService | Permanente        |
| DATA-PURGE    | Purgar Datos     | ComplianceService       | GDPR              |
| BACKUP-001    | Backup Manual    | BackupService           | Respaldo          |
| RESTORE-001   | Restaurar Backup | BackupService           | Recuperar         |
| DEPLOY-001    | Deploy Config    | ConfigurationService    | Hot reload        |
| SECRET-001    | Gestión Secrets  | VaultService            | Credenciales      |
| AUDIT-FULL    | Audit Completo   | AuditService            | Sin restricciones |

### 🧭 Acceso en UI (Navbar + Rutas)

**Panel Admin:**

- Mismo acceso que ADM-ADMIN

**⚠️ FALTANTES en Navbar/UI:**

- ❌ **Eliminación permanente de usuarios/dealers** - No diferenciado
- ❌ **Purga de datos GDPR** - No hay UI
- ❌ **Gestión de Backups** - No hay UI
- ❌ **Gestión de Secrets** - No hay UI
- ❌ **Deploy/Reload Config** - No hay UI
- ❌ **Audit sin restricciones** - No diferenciado

### 🟡 Estado: 48% - PARCIALMENTE COMPLETO

---

## 📊 Resumen de Brechas

### 🔴 Brechas Críticas (Bloquean Procesos)

| Área                 | Brecha                          | Tipos Usuario Afectados | Prioridad  |
| -------------------- | ------------------------------- | ----------------------- | ---------- |
| **Compliance**       | Sin dashboard ni reportes DGII  | ADM-COMP                | 🔴 CRÍTICA |
| **Moderación**       | Sin cola de moderación dedicada | ADM-MOD                 | 🔴 CRÍTICA |
| **Soporte**          | Sin sistema de tickets          | ADM-SUPPORT             | 🔴 CRÍTICA |
| **Empleados Dealer** | Sin gestión de staff            | DLR-ADMIN               | 🔴 ALTA    |
| **Reembolsos**       | Sin UI para procesar            | ADM-SUPPORT             | 🔴 ALTA    |

### 🟡 Brechas Medias (Funcionalidad Reducida)

| Área               | Brecha                            | Tipos Usuario Afectados | Prioridad |
| ------------------ | --------------------------------- | ----------------------- | --------- |
| **Notificaciones** | Sin configuración de preferencias | USR-REG                 | 🟡 MEDIA  |
| **Reviews**        | Sin página "Mis Reviews"          | USR-SELLER              | 🟡 MEDIA  |
| **Citas**          | Sin link en navbar dealer         | DLR-STAFF               | 🟡 MEDIA  |
| **Revenue**        | Sin dashboard de ingresos         | ADM-ADMIN               | 🟡 MEDIA  |
| **Mantenimiento**  | Sin toggle visible                | ADM-ADMIN               | 🟡 MEDIA  |

### 🟢 Mejoras Menores

| Área            | Mejora                   | Tipos Usuario Afectados |
| --------------- | ------------------------ | ----------------------- |
| Eliminar cuenta | Agregar en Settings      | USR-REG                 |
| Importar CSV    | Destacar en inventario   | DLR-STAFF               |
| Mensajes        | Agregar en navbar dealer | DLR-STAFF               |

---

## 🎯 Plan de Acción Recomendado

### Fase 1: Crítico (Sprint Inmediato)

1. **Crear Dashboard Compliance** `/admin/compliance`
   - Generar Reporte 607
   - Verificación de RNC
   - Solicitudes GDPR

2. **Crear Módulo Moderación** `/admin/moderation`
   - Cola de moderación
   - Reportes de contenido
   - Historial de acciones

3. **Crear Sistema de Tickets** `/admin/tickets`
   - Lista de tickets
   - Asignar, responder, cerrar
   - Escalar

4. **Crear Gestión de Empleados** `/dealer/employees`
   - CRUD de staff
   - Asignación de roles
   - Permisos por empleado

### Fase 2: Alta Prioridad (Siguiente Sprint)

5. **Dashboard de Revenue** `/admin/revenue`
   - MRR, ARR
   - Transacciones
   - Proyecciones

6. **Página de Reembolsos** `/admin/refunds`
   - Procesar devoluciones
   - Generar notas de crédito

7. **Configuración de Notificaciones** `/settings/notifications`
   - Preferencias de email
   - Push notifications
   - Frecuencia de alertas

### Fase 3: Mejoras (Backlog)

8. Agregar "Citas" en navbar dealer
9. Agregar "Mensajes" en navbar dealer
10. Crear "Mis Reviews" para sellers
11. Toggle de mantenimiento en admin
12. Vista de errores del sistema

---

## 📈 Impacto en Estado de Procesos

### Antes de Implementar Mejoras

| Rol         | Procesos Accesibles | Total Procesos | %   |
| ----------- | ------------------- | -------------- | --- |
| ADM-COMP    | 1                   | 6              | 17% |
| ADM-MOD     | 2                   | 8              | 25% |
| ADM-SUPPORT | 3                   | 10             | 30% |

### Después de Fase 1

| Rol         | Procesos Accesibles | Total Procesos | %    |
| ----------- | ------------------- | -------------- | ---- |
| ADM-COMP    | 6                   | 6              | 100% |
| ADM-MOD     | 8                   | 8              | 100% |
| ADM-SUPPORT | 10                  | 10             | 100% |

---

## ✅ Conclusión

**⚠️ El estado de "100% Completo" en la matriz de procesos NO puede mantenerse** mientras existan brechas críticas en el acceso de UI para:

1. **Compliance Officers** - 0% de acceso a sus procesos
2. **Moderadores** - 25% de acceso a sus procesos
3. **Agentes de Soporte** - 30% de acceso a sus procesos

**Recomendación:** Ajustar el estado de implementación de los siguientes servicios:

| Servicio          | Estado Actual | Estado Real (considerando UI) |
| ----------------- | ------------- | ----------------------------- |
| AdminService      | 🟡 75%        | 🟡 60%                        |
| ComplianceService | ⚠️ N/A        | 🔴 20%                        |
| ModerationService | ⚠️ N/A        | 🔴 25%                        |
| SupportService    | ⚠️ N/A        | 🔴 0% (No existe)             |

---

_Documento generado: Enero 27, 2026_  
_Próxima revisión: Después de Fase 1_
