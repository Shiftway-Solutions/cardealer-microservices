# AdminService - Context Documentation

## 📋 INFORMACIÓN GENERAL

- **Nombre del Servicio:** AdminService
- **Puerto en Desarrollo:** 5029
- **Estado:** 🚧 **EN DESARROLLO - NO DESPLEGADO**
- **Base de Datos:** PostgreSQL (`adminservice`)
- **Imagen Docker:** Local only

### Propósito
Panel de administración del sistema. Gestión de usuarios, contenido, configuraciones globales, moderación y estadísticas de la plataforma.

---

## 🏗️ ARQUITECTURA

```
AdminService/
├── AdminService.Api/
│   ├── Controllers/
│   │   ├── DashboardController.cs
│   │   ├── UsersManagementController.cs
│   │   ├── ContentModerationController.cs
│   │   ├── SystemConfigController.cs
│   │   └── PlatformStatsController.cs
│   └── Program.cs
├── AdminService.Application/
├── AdminService.Domain/
│   ├── Entities/
│   │   ├── AdminAction.cs
│   │   ├── ModerationQueue.cs
│   │   └── SystemAlert.cs
│   └── Enums/
│       ├── ActionType.cs
│       └── ModerationStatus.cs
└── AdminService.Infrastructure/
```

---

## 📦 ENTIDADES PRINCIPALES

### AdminAction (Audit log de acciones administrativas)
```csharp
public class AdminAction
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string AdminUserName { get; set; }
    
    // Acción
    public ActionType Type { get; set; }           // UserSuspended, ListingApproved, ConfigChanged
    public string Description { get; set; }
    public string? TargetEntityType { get; set; }  // "User", "Vehicle", "Property"
    public Guid? TargetEntityId { get; set; }
    
    // Cambios realizados
    public string? BeforeValue { get; set; }       // JSON
    public string? AfterValue { get; set; }        // JSON
    
    // Metadata
    public string IpAddress { get; set; }
    public DateTime PerformedAt { get; set; }
}
```

### ModerationQueue
```csharp
public class ModerationQueue
{
    public Guid Id { get; set; }
    
    // Item a moderar
    public string EntityType { get; set; }         // "VehicleListing", "PropertyListing", "UserReview"
    public Guid EntityId { get; set; }
    public string EntityTitle { get; set; }
    
    // Razón de moderación
    public string Reason { get; set; }             // "FlaggedByUser", "AutomaticDetection", "NewListing"
    public string? ReportDetails { get; set; }
    public Guid? ReportedByUserId { get; set; }
    
    // Estado
    public ModerationStatus Status { get; set; }   // Pending, Approved, Rejected, Escalated
    public DateTime SubmittedAt { get; set; }
    public Guid? ModeratorId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ModeratorNotes { get; set; }
    
    // Prioridad
    public int Priority { get; set; } = 1;         // 1=Normal, 2=High, 3=Urgent
}
```

### SystemAlert
```csharp
public class SystemAlert
{
    public Guid Id { get; set; }
    
    // Tipo de alerta
    public AlertSeverity Severity { get; set; }    // Info, Warning, Critical
    public string Title { get; set; }
    public string Message { get; set; }
    
    // Origen
    public string Source { get; set; }             // "HealthCheck", "ErrorService", "NCF"
    public string? SourceEntityId { get; set; }
    
    // Estado
    public bool IsAcknowledged { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsResolved { get; set; }
}
```

---

## 📡 ENDPOINTS (Propuestos)

### Dashboard
- `GET /api/admin/dashboard` - Estadísticas principales
  ```json
  {
    "totalUsers": 15234,
    "activeListings": 1892,
    "pendingModeration": 12,
    "todayRegistrations": 45,
    "todayTransactions": 23,
    "revenue30Days": 125000,
    "systemAlerts": 3
  }
  ```

### Gestión de Usuarios
- `GET /api/admin/users` - Listar usuarios (con filtros)
- `GET /api/admin/users/{id}` - Detalle de usuario
- `PUT /api/admin/users/{id}/suspend` - Suspender usuario
- `PUT /api/admin/users/{id}/activate` - Reactivar usuario
- `PUT /api/admin/users/{id}/verify` - Verificar cuenta manualmente
- `DELETE /api/admin/users/{id}` - Eliminar usuario (soft delete)

### Moderación de Contenido
- `GET /api/admin/moderation` - Cola de moderación
- `GET /api/admin/moderation/{id}` - Detalle de item
- `PUT /api/admin/moderation/{id}/approve` - Aprobar
- `PUT /api/admin/moderation/{id}/reject` - Rechazar
- `POST /api/admin/moderation/flag` - Marcar contenido para revisión

### Configuración del Sistema
- `GET /api/admin/config` - Todas las configuraciones
- `PUT /api/admin/config/{key}` - Actualizar configuración
- `GET /api/admin/feature-flags` - Ver feature flags
- `PUT /api/admin/feature-flags/{id}` - Toggle feature flag

### Alertas
- `GET /api/admin/alerts` - Listar alertas activas
- `PUT /api/admin/alerts/{id}/acknowledge` - Reconocer alerta
- `PUT /api/admin/alerts/{id}/resolve` - Resolver alerta

### Audit Log
- `GET /api/admin/audit-log` - Historial de acciones administrativas
- `GET /api/admin/audit-log/user/{userId}` - Acciones de un admin específico

---

## 💡 FUNCIONALIDADES PLANEADAS

### Dashboard Widgets
- **Usuarios:** Registros hoy/semana/mes, activos vs inactivos
- **Listings:** Totales por categoría, pendientes de aprobación
- **Transacciones:** Volumen de ventas, conversión
- **Errores:** Errores críticos últimas 24h
- **Performance:** Response time promedio, uptime

### Bulk Operations
- Aprobar múltiples listings de una vez
- Suspender usuarios en masa
- Exportar datos a CSV/Excel

### Automated Moderation
- Detección de palabras prohibidas
- Verificación de imágenes (contenido inapropiado)
- Detección de duplicados (mismo listing publicado varias veces)
- Análisis de sentiment en reviews

### User Insights
- Actividad reciente del usuario
- Historial de compras/ventas
- Reports recibidos
- Ratio de conversión

### Content Management
- Gestionar homepage sections
- Featured listings (destacar manualmente)
- Banners y promociones
- FAQ y páginas estáticas

### Reporting Tools
- Generar reportes personalizados
- Scheduler de reportes automáticos
- Export a PDF/Excel

---

## 🔗 INTEGRACIÓN CON OTROS SERVICIOS

### UserService
- Obtener/actualizar datos de usuarios
- Cambiar roles y permisos

### VehiclesSaleService / PropertiesSaleService
- Moderar listings
- Cambiar status (aprobar, rechazar, destacar)

### ErrorService
- Dashboard de errores
- Alertas de errores críticos

### AuditService
- Registrar todas las acciones administrativas

### NotificationService
- Notificar a usuarios sobre acciones (suspensión, aprobación de listing)

---

## 🎯 BUSINESS RULES

### Permisos Administrativos
- Super Admin: acceso completo
- Moderator: solo moderación de contenido
- Support: solo gestión de usuarios
- Analyst: solo lectura de reportes

### Audit Trail
- Todas las acciones deben registrarse
- No se puede editar/eliminar audit log
- Retención: mínimo 2 años

### Moderación
- Listings nuevos → auto-aprobados si usuario verificado
- Listings de usuarios no verificados → requieren moderación
- 3 rechazos → suspender usuario temporalmente

---

**Estado:** 🚧 EN DESARROLLO - No desplegado en producción  
**Versión:** 0.1.0

