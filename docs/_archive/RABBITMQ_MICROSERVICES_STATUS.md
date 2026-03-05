# 📊 Estado de RabbitMQ en Microservicios

**Fecha de verificación:** 6 de Enero 2026  
**Estado global:** ✅ OPERACIONAL

---

## 🟢 Servicios con RabbitMQ FUNCIONANDO

### 1. AuthService (Puerto 15085)
- **Rol:** Publisher de eventos
- **Eventos publicados:**
  - `auth.user.registered` - Cuando un usuario se registra
  - `auth.user.logged_in` - Cuando un usuario inicia sesión
  - `auth.password.changed` - Cuando se cambia la contraseña
  - `auth.error.*` - Errores del servicio
- **Estado:** ✅ Operacional
- **Config:** `RabbitMQ__Enabled: "true"` en compose.yaml

### 2. UserService (Puerto 15100)
- **Rol:** Consumer de eventos
- **Cola:** `userservice.user.registered`
- **Eventos consumidos:**
  - `auth.user.registered` - Sincroniza usuarios de AuthService
- **Implementación:** Lazy connection pattern (no crashea al inicio)
- **Estado:** ✅ Operacional
- **Config:** `RabbitMQ__Enabled: "true"` en compose.yaml

### 3. ErrorService (Puerto 15083)
- **Rol:** Consumer + Publisher
- **Cola:** `error-queue`
- **Eventos consumidos:** Errores de todos los servicios
- **Circuit Breaker:** Polly v8 implementado
- **Estado:** ✅ Operacional
- **Config:** `depends_on: rabbitmq` en compose.yaml

### 4. NotificationService (Puerto 15084)
- **Rol:** Consumer múltiple
- **Colas:**
  - `notification-queue` - Notificaciones generales
  - `notification-email-queue` - Emails
  - `notification-sms-queue` - SMS
  - `notification.error.critical` - Errores críticos
- **Estado:** ✅ Operacional
- **Config:** `depends_on: rabbitmq` en compose.yaml

---

## ⚪ Servicios SIN RabbitMQ (No requerido)

| Servicio | Puerto | Razón |
|----------|--------|-------|
| VehiclesSaleService | 15070 | CRUD básico, no requiere eventos |
| VehiclesRentService | 15071 | CRUD básico, no requiere eventos |
| PropertiesSaleService | 15072 | CRUD básico, no requiere eventos |
| PropertiesRentService | 15073 | CRUD básico, no requiere eventos |
| BillingService | 15008 | Integración directa con Stripe |
| CRMService | 15009 | CRUD básico, no requiere eventos |
| AdminService | 15011 | CRUD básico, no requiere eventos |

---

## ⚙️ Servicios con RabbitMQ OPCIONAL

### MediaService (Puerto 15090)
- **Funcionalidad:** Dead Letter Queue para procesamiento de media
- **Config:** `RabbitMQ:Enabled` en appsettings
- **Estado actual:** Deshabilitado (funciona sin RabbitMQ)

### RoleService (Puerto 15101)
- **Funcionalidad:** Publisher para reportar errores
- **Estado actual:** Disponible pero no crítico

---

## 📋 Colas Activas en RabbitMQ

| Cola | Mensajes Pendientes | Consumidores | Estado |
|------|---------------------|--------------|--------|
| `userservice.user.registered` | 0 | 1 | ✅ |
| `error-queue` | 0 | 1 | ✅ |
| `notification-queue` | 0 | 1 | ✅ |
| `notification-email-queue` | 0 | 1 | ✅ |
| `notification-sms-queue` | 0 | 1 | ✅ |
| `notification.error.critical` | 0 | 1 | ✅ |
| `notification-queue-retry` | 0 | 0 | ⚪ Sin consumer (DLQ) |

---

## 🔄 Flujos de Eventos Verificados

### 1. Registro de Usuario
```
[Usuario] → POST /api/auth/register
     ↓
[AuthService] → Crea usuario en DB
     ↓
[AuthService] → Publica UserRegisteredEvent a RabbitMQ
     ↓
[RabbitMQ] → Exchange: cardealer.events (topic)
     ↓
[UserService] → Consumer recibe evento
     ↓
[UserService] → Crea/actualiza usuario en su DB
```

### 2. Manejo de Errores
```
[Cualquier Servicio] → Error ocurre
     ↓
[Middleware de Error] → Captura excepción
     ↓
[RabbitMQ] → Publica a error-queue
     ↓
[ErrorService] → Consumer procesa y almacena
     ↓
[Métricas] → Actualiza dashboards
```

---

## 🔧 Configuración de RabbitMQ

```yaml
# compose.yaml
rabbitmq:
  image: rabbitmq:3.12-management-alpine
  container_name: rabbitmq
  ports:
    - "10002:5672"   # AMQP
    - "10003:15672"  # Management UI
  environment:
    RABBITMQ_DEFAULT_USER: guest
    RABBITMQ_DEFAULT_PASS: guest
  healthcheck:
    test: rabbitmq-diagnostics -q ping
```

**URLs de acceso:**
- AMQP: `amqp://guest:guest@rabbitmq:5672`
- Management UI: `http://localhost:10003`

---

## ✅ Pruebas Realizadas

1. **Registro de usuario** - Verificado que UserRegisteredEvent se propaga
2. **Sincronización de datos** - Usuarios aparecen en UserService
3. **Procesamiento de errores** - ErrorService consume eventos
4. **Health de colas** - Todas las colas con consumers activos

---

## 📝 Notas Técnicas

### Lazy Connection Pattern
UserService implementa conexión lazy a RabbitMQ para evitar crashes al inicio:
```csharp
private void EnsureConnected()
{
    if (_isConnected) return;
    lock (_connectionLock)
    {
        if (_isConnected) return;
        // Crear conexión aquí
        _isConnected = true;
    }
}
```

### Circuit Breaker
ErrorService usa Polly v8 para Circuit Breaker:
- Abre circuito si 50% de requests fallan en 30s
- Permanece abierto por 30s antes de intentar reconexión

---

## 🚀 Próximos Pasos

- [ ] Implementar notificaciones por WebSocket (SignalR)
- [ ] Agregar métricas de RabbitMQ a Grafana
- [ ] Implementar saga pattern para transacciones distribuidas
