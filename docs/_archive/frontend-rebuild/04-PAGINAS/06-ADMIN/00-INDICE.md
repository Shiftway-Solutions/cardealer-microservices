# 📁 06-ADMIN - Panel Administrativo

> **Descripción:** Panel de administración de la plataforma  
> **Total:** 15 documentos  
> **Prioridad:** 🟡 P2 - Operaciones internas

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                                        | Descripción                 | Prioridad |
| --- | -------------------------------------------------------------- | --------------------------- | --------- |
| 1   | [01-admin-dashboard.md](01-admin-dashboard.md)                 | Dashboard administrativo    | P1        |
| 2   | [02-admin-users.md](02-admin-users.md)                         | Gestión de usuarios         | P1        |
| 3   | [03-admin-moderation.md](03-admin-moderation.md)               | Moderación de contenido     | P1        |
| 4   | [04-admin-compliance.md](04-admin-compliance.md)               | Compliance general          | P2        |
| 5   | [05-admin-support.md](05-admin-support.md)                     | Soporte al cliente          | P1        |
| 6   | [06-admin-system.md](06-admin-system.md)                       | Configuración del sistema   | P2        |
| 7   | [07-notificaciones-admin.md](07-notificaciones-admin.md)       | Notificaciones masivas      | P2        |
| 8   | [08-admin-review-moderation.md](08-admin-review-moderation.md) | Moderación de reviews       | P2        |
| 9   | [09-admin-compliance-alerts.md](09-admin-compliance-alerts.md) | Alertas de compliance       | P2        |
| 10  | [10-admin-operations.md](10-admin-operations.md)               | Operaciones diarias         | P2        |
| 11  | [11-users-roles-management.md](11-users-roles-management.md)   | Gestión de roles y permisos | P1        |
| 12  | [12-listings-approvals.md](12-listings-approvals.md)           | Aprobación de publicaciones | P1        |
| 13  | [13-reports-kyc-queue.md](13-reports-kyc-queue.md)             | Cola de reportes y KYC      | P1        |
| 14  | [14-admin-settings.md](14-admin-settings.md)                   | Configuración y categorías  | P2        |
| 15  | [15-ml-admin-dashboards.md](15-ml-admin-dashboards.md)         | Dashboards de ML/IA         | P3        |

---

## 🎯 Orden de Implementación para IA

```
1. 01-admin-dashboard.md       → Dashboard principal
2. 11-users-roles-management.md → Usuarios y roles
3. 02-admin-users.md           → Gestión de usuarios
4. 12-listings-approvals.md    → Aprobación de listings
5. 03-admin-moderation.md      → Moderación
6. 13-reports-kyc-queue.md     → Cola de reportes
7. 05-admin-support.md         → Soporte
8. 08-admin-review-moderation.md → Reviews
9. 04-admin-compliance.md      → Compliance
10. 09-admin-compliance-alerts.md → Alertas
11. 07-notificaciones-admin.md  → Notificaciones
12. 10-admin-operations.md      → Operaciones
13. 06-admin-system.md          → Sistema
14. 14-admin-settings.md        → Configuración
15. 15-ml-admin-dashboards.md   → ML dashboards
```

---

## 🔗 Dependencias Externas

- **02-AUTH/**: Roles administrativos (ADM-\*)
- **05-API-INTEGRATION/**: admin-api, users-api
- **05-ADMIN/29-admin-rbac.md**: Sistema de permisos

---

## 📊 APIs Utilizadas

| Servicio          | Endpoints Principales                      |
| ----------------- | ------------------------------------------ |
| AdminService      | GET /admin/stats, GET /admin/logs          |
| UserService       | GET /users, PUT /users/:id/status          |
| RoleService       | GET /roles, POST /roles                    |
| ModerationService | GET /moderation/queue, PUT /moderation/:id |
| SupportService    | GET /tickets, PUT /tickets/:id             |
| AnalyticsService  | GET /analytics/platform                    |
