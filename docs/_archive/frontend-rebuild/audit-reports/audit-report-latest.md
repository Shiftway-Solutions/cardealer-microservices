# 📊 Reporte de Auditoría de Documentación de API

**Fecha:** January 30, 2026 04:50:36
**Generado por:** audit-api-documentation.py

---

## 📈 Resumen Ejecutivo

| Métrica                      | Valor           |
|------------------------------|-----------------||
| **Endpoints Documentados**   | 12 |
| **Rutas en Gateway**         | 129  |
| **Cobertura de Documentación** | 9.3%   |

---

## 📋 Endpoints Documentados

| Método | Ruta | Archivo |
|--------|------|---------||
| `GET` | `/api/vehicle360processing/jobs/{jobId}` | 05-vehicle-360-api.md |
| `GET` | `/api/vehicle360processing/viewer/{vehicleId}` | 05-vehicle-360-api.md |
| `POST` | `/api/auth/change-password` | 02-autenticacion.md |
| `POST` | `/api/auth/forgot-password` | 02-autenticacion.md |
| `POST` | `/api/auth/login` | 02-autenticacion.md |
| `POST` | `/api/auth/refresh` | 02-autenticacion.md |
| `POST` | `/api/auth/register` | 02-autenticacion.md |
| `POST` | `/api/auth/resend-verification` | 02-autenticacion.md |
| `POST` | `/api/auth/reset-password` | 02-autenticacion.md |
| `POST` | `/api/auth/verify-email` | 02-autenticacion.md |
| `POST` | `/api/media/upload` | 04-subida-imagenes.md |
| `POST` | `/api/vehicle360processing/upload-and-process` | 05-vehicle-360-api.md |

---

## 📊 Desglose por Archivo

| Archivo | Endpoints Documentados |
|---------|------------------------||
| 02-autenticacion.md | 8 |
| 04-subida-imagenes.md | 1 |
| 05-vehicle-360-api.md | 3 |

---

## 🎯 Próximos Pasos

### Servicios Pendientes de Documentar

Basado en el Gateway, los siguientes servicios necesitan documentación:

- **VehiclesService:** Endpoints de vehículos (búsqueda, filtrado, CRUD)
- **UserService:** Gestión de usuarios y perfiles
- **BillingService:** Pagos, suscripciones, planes
- **RoleService:** Roles y permisos
- **NotificationService:** Notificaciones push, email, SMS
- **Y más...**

### Recomendaciones

1. **Prioridad Alta:** Documentar servicios core (Vehicles, Users, Billing)
2. **Prioridad Media:** Documentar servicios de soporte (Notifications, Media)
3. **Prioridad Baja:** Documentar servicios administrativos

---

_Generado automáticamente por audit-api-documentation.py_
