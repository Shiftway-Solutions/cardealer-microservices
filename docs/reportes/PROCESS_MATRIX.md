# 🔄 Matriz de Procesos — OKLA Platform

**Fecha:** 2025-07-14  
**Versión:** 1.0

---

## Actores del Sistema

| Actor                    | Rol                   | Descripción                                  |
| ------------------------ | --------------------- | -------------------------------------------- |
| 👤 **Anónimo**           | Visitante             | Usuario no autenticado que navega el sitio   |
| 🛒 **Comprador (Buyer)** | Usuario registrado    | Busca, contacta y compra vehículos           |
| 🏷️ **Vendedor (Seller)** | Persona individual    | Publica vehículos individuales ($29/listado) |
| 🏢 **Dealer**            | Empresa/Concesionario | Tiene plan mensual, múltiples vehículos      |
| 👑 **Admin**             | Administrador         | Gestiona la plataforma, aprueba contenido    |
| 🤖 **Sistema**           | Automatizado          | Procesos automáticos (cron, eventos, AI)     |

---

## 1. Registro y Autenticación

### P1.1 — Registro de Comprador

| Paso | Actor      | Acción                                                           | Servicio            | Endpoint                               |
| ---- | ---------- | ---------------------------------------------------------------- | ------------------- | -------------------------------------- |
| 1    | 👤 Anónimo | Completa formulario de registro                                  | Frontend            | `/registro`                            |
| 2    | 🤖 Sistema | Valida email, contraseña (8+ chars, mayúscula, número, especial) | AuthService         | `POST /api/auth/register`              |
| 3    | 🤖 Sistema | Envía email de verificación                                      | NotificationService | Evento: `auth.user.registered`         |
| 4    | 👤 → 🛒    | Confirma email haciendo clic en enlace                           | AuthService         | `GET /api/auth/verify-email?token=...` |
| 5    | 🤖 Sistema | Activa cuenta y genera JWT                                       | AuthService         | —                                      |

### P1.2 — Registro de Dealer

| Paso | Actor      | Acción                                    | Servicio                | Endpoint                               |
| ---- | ---------- | ----------------------------------------- | ----------------------- | -------------------------------------- |
| 1    | 👤 Anónimo | Completa formulario dealer (empresa, RNC) | Frontend                | `/registro/dealer`                     |
| 2    | 🤖 Sistema | Crea usuario con rol `dealer`             | AuthService             | `POST /api/auth/register`              |
| 3    | 🤖 Sistema | Crea perfil de dealer                     | DealerManagementService | `POST /api/dealers`                    |
| 4    | 👑 Admin   | Revisa y aprueba registro del dealer      | AdminService            | `POST /api/admin/dealers/{id}/approve` |
| 5    | 🤖 Sistema | Notifica al dealer de aprobación          | NotificationService     | Evento: `dealer.registration.approved` |

### P1.3 — Login

| Paso | Actor      | Acción                                                | Servicio    | Endpoint               |
| ---- | ---------- | ----------------------------------------------------- | ----------- | ---------------------- |
| 1    | 🛒/🏷️/🏢   | Ingresa email y contraseña                            | Frontend    | `/iniciar-sesion`      |
| 2    | 🤖 Sistema | Valida credenciales, genera JWT + refresh token       | AuthService | `POST /api/auth/login` |
| 3    | 🤖 Sistema | Registra sesión y último acceso                       | AuthService | —                      |
| 4    | 🤖 Sistema | Redirige según rol (buyer → home, dealer → dashboard) | Frontend    | —                      |

### P1.4 — Verificación KYC

| Paso | Actor      | Acción                                      | Servicio          | Endpoint                           |
| ---- | ---------- | ------------------------------------------- | ----------------- | ---------------------------------- |
| 1    | 🏷️/🏢      | Accede a sección de verificación            | Frontend          | `/cuenta/verificacion`             |
| 2    | 🏷️/🏢      | Captura foto de cédula (frente y reverso)   | Frontend (cámara) | —                                  |
| 3    | 🏷️/🏢      | Captura selfie para liveness check          | Frontend (cámara) | —                                  |
| 4    | 🤖 Sistema | Sube imágenes a S3/DO Spaces                | MediaService      | `POST /api/media/upload`           |
| 5    | 🤖 Sistema | Procesa documentos KYC                      | KYCService        | `POST /api/kyc/submit`             |
| 6    | 👑 Admin   | Revisa y aprueba/rechaza KYC                | AdminService      | `POST /api/admin/kyc/{id}/approve` |
| 7    | 🤖 Sistema | Actualiza nivel de verificación del usuario | KYCService        | —                                  |

---

## 2. Publicación de Vehículos

### P2.1 — Publicar Vehículo (Seller)

| Paso | Actor      | Acción                                                        | Servicio            | Endpoint                                |
| ---- | ---------- | ------------------------------------------------------------- | ------------------- | --------------------------------------- |
| 1    | 🏷️ Seller  | Accede a publicar vehículo                                    | Frontend            | `/vender/publicar`                      |
| 2    | 🏷️ Seller  | Completa formulario (marca, modelo, año, precio, descripción) | Frontend            | —                                       |
| 3    | 🏷️ Seller  | (Opcional) Ingresa VIN para auto-completar                    | Frontend            | `POST /api/score/vin-decode`            |
| 4    | 🏷️ Seller  | Sube fotos del vehículo (mín 3, máx según plan)               | MediaService        | `POST /api/media/upload`                |
| 5    | 🤖 Sistema | Valida datos, crea listado con estado `pending`               | VehiclesSaleService | `POST /api/vehicles`                    |
| 6    | 🤖 Sistema | Cobra $29 por listado (Stripe)                                | BillingService      | `POST /api/billing/charge`              |
| 7    | 👑 Admin   | Revisa y aprueba vehículo                                     | AdminService        | `POST /api/admin/vehicles/{id}/approve` |
| 8    | 🤖 Sistema | Publica vehículo (estado → `active`)                          | VehiclesSaleService | —                                       |
| 9    | 🤖 Sistema | Indexa en búsqueda                                            | SearchAgent         | Evento: `vehicle.listing.approved`      |

### P2.2 — Publicar Vehículo (Dealer)

| Paso | Actor      | Acción                                                | Servicio            | Endpoint                                |
| ---- | ---------- | ----------------------------------------------------- | ------------------- | --------------------------------------- |
| 1    | 🏢 Dealer  | Accede a publicar desde dashboard                     | Frontend            | `/dealer/publicar`                      |
| 2    | 🏢 Dealer  | Completa formulario con asistente inteligente         | Frontend            | —                                       |
| 3    | 🏢 Dealer  | Sube fotos (límite según plan: 10/20/30/40)           | MediaService        | `POST /api/media/upload`                |
| 4    | 🤖 Sistema | Valida contra límites del plan del dealer             | BillingService      | `GET /api/dealer-billing/plan`          |
| 5    | 🤖 Sistema | Crea listado (sin cobro individual, incluido en plan) | VehiclesSaleService | `POST /api/vehicles`                    |
| 6    | 👑 Admin   | Aprueba vehículo                                      | AdminService        | `POST /api/admin/vehicles/{id}/approve` |
| 7    | 🤖 Sistema | Publica y notifica al dealer                          | NotificationService | —                                       |

### P2.3 — Editar/Eliminar Vehículo

| Paso | Actor      | Acción                                     | Servicio            | Endpoint                       |
| ---- | ---------- | ------------------------------------------ | ------------------- | ------------------------------ |
| 1    | 🏷️/🏢      | Accede a sus listados                      | Frontend            | `/vender/dashboard`            |
| 2    | 🏷️/🏢      | Edita información o marca como vendido     | Frontend            | `/vender/editar/{id}`          |
| 3    | 🤖 Sistema | Actualiza listado                          | VehiclesSaleService | `PUT /api/vehicles/{id}`       |
| 4    | 🤖 Sistema | Si se marca vendido, desactiva de búsqueda | VehiclesSaleService | `POST /api/vehicles/{id}/sold` |

---

## 3. Búsqueda y Navegación

### P3.1 — Búsqueda de Vehículos (Anónimo/Buyer)

| Paso | Actor      | Acción                                            | Servicio            | Endpoint                        |
| ---- | ---------- | ------------------------------------------------- | ------------------- | ------------------------------- |
| 1    | 👤/🛒      | Accede a búsqueda de vehículos                    | Frontend            | `/vehiculos`                    |
| 2    | 👤/🛒      | Aplica filtros (marca, modelo, año, precio, tipo) | Frontend            | —                               |
| 3    | 🤖 Sistema | Busca vehículos con filtros                       | VehiclesSaleService | `GET /api/vehicles?filters...`  |
| 4    | 🤖 Sistema | (Opcional) Muestra anuncios patrocinados          | AdvertisingService  | `GET /api/advertising/rotation` |
| 5    | 👤/🛒      | Selecciona vehículo para ver detalle              | Frontend            | `/vehiculos/{slug}`             |

### P3.2 — Búsqueda con AI (SearchAgent)

| Paso | Actor      | Acción                                 | Servicio            | Endpoint                       |
| ---- | ---------- | -------------------------------------- | ------------------- | ------------------------------ |
| 1    | 🛒 Buyer   | Escribe consulta en lenguaje natural   | Frontend            | Widget AI Search               |
| 2    | 🤖 Sistema | Detecta intención con Claude Haiku 4.5 | SearchAgent         | `POST /api/searchagent/search` |
| 3    | 🤖 Sistema | Convierte a filtros estructurados      | SearchAgent         | —                              |
| 4    | 🤖 Sistema | Busca vehículos matching               | VehiclesSaleService | `GET /api/vehicles?...`        |
| 5    | 🤖 Sistema | Presenta resultados con explicación AI | Frontend            | —                              |

### P3.3 — Guardar Favoritos

| Paso | Actor      | Acción                      | Servicio            | Endpoint                           |
| ---- | ---------- | --------------------------- | ------------------- | ---------------------------------- |
| 1    | 🛒 Buyer   | Hace clic en ❤️ en vehículo | Frontend            | —                                  |
| 2    | 🤖 Sistema | Guarda favorito             | VehiclesSaleService | `POST /api/vehicles/{id}/favorite` |
| 3    | 🛒 Buyer   | Accede a sus favoritos      | Frontend            | `/cuenta/favoritos`                |

---

## 4. Contacto y Comunicación

### P4.1 — Contactar al Vendedor

| Paso | Actor      | Acción                                          | Servicio            | Endpoint                          |
| ---- | ---------- | ----------------------------------------------- | ------------------- | --------------------------------- |
| 1    | 🛒 Buyer   | Hace clic en "Contactar" en detalle de vehículo | Frontend            | —                                 |
| 2    | 🛒 Buyer   | Completa formulario (asunto, mensaje, teléfono) | Frontend            | —                                 |
| 3    | 🤖 Sistema | Crea solicitud de contacto                      | ContactService      | `POST /api/contactrequests`       |
| 4    | 🤖 Sistema | Notifica al vendedor por email/push             | NotificationService | Evento: `contact.request.created` |
| 5    | 🏷️/🏢      | Responde al contacto                            | Frontend            | `/mensajes`                       |

### P4.2 — Agendar Cita (Test Drive)

| Paso | Actor      | Acción                           | Servicio            | Endpoint                          |
| ---- | ---------- | -------------------------------- | ------------------- | --------------------------------- |
| 1    | 🛒 Buyer   | Selecciona fecha/hora disponible | Frontend            | —                                 |
| 2    | 🤖 Sistema | Verifica disponibilidad          | AppointmentService  | `GET /api/timeslots?dealerId=...` |
| 3    | 🤖 Sistema | Crea cita                        | AppointmentService  | `POST /api/appointments`          |
| 4    | 🤖 Sistema | Notifica a ambas partes          | NotificationService | Evento: `appointment.created`     |
| 5    | 🏢 Dealer  | Confirma o rechaza cita          | Frontend            | `/dealer/citas`                   |

### P4.3 — Chat con Soporte (SupportAgent)

| Paso | Actor      | Acción                                        | Servicio     | Endpoint                         |
| ---- | ---------- | --------------------------------------------- | ------------ | -------------------------------- |
| 1    | 🛒/🏷️/🏢   | Abre widget de soporte                        | Frontend     | Widget flotante                  |
| 2    | 🤖 Sistema | Inicia sesión de chat                         | SupportAgent | `POST /api/supportagent/session` |
| 3    | 🛒/🏷️/🏢   | Escribe pregunta                              | Frontend     | —                                |
| 4    | 🤖 Sistema | Busca en FAQ cache primero                    | SupportAgent | —                                |
| 5    | 🤖 Sistema | Si no hay cache, consulta Claude Haiku 4.5    | SupportAgent | `POST /api/supportagent/message` |
| 6    | 🤖 Sistema | Responde con información de la plataforma     | Frontend     | —                                |
| 7    | 🤖 Sistema | Si no puede resolver, sugiere contacto humano | SupportAgent | —                                |

### P4.4 — Chatbot de Ventas (Dealer)

| Paso | Actor      | Acción                                                      | Servicio       | Endpoint                    |
| ---- | ---------- | ----------------------------------------------------------- | -------------- | --------------------------- |
| 1    | 🛒 Buyer   | Abre chat en página del dealer/vehículo                     | Frontend       | Widget en detalle           |
| 2    | 🤖 Sistema | Inicia sesión con contexto del vehículo                     | ChatbotService | `POST /api/chatbot/session` |
| 3    | 🛒 Buyer   | Hace preguntas sobre el vehículo                            | Frontend       | —                           |
| 4    | 🤖 Sistema | Responde con Claude Sonnet 4.5 (contexto dealer + vehículo) | ChatbotService | `POST /api/chatbot/message` |
| 5    | 🤖 Sistema | Registra interacción para analytics                         | ChatbotService | —                           |

---

## 5. Facturación y Pagos

### P5.1 — Suscripción a Plan (Dealer)

| Paso | Actor      | Acción                                    | Servicio            | Endpoint                               |
| ---- | ---------- | ----------------------------------------- | ------------------- | -------------------------------------- |
| 1    | 🏢 Dealer  | Selecciona plan (Libre/Visible/Pro/Elite) | Frontend            | `/precios`                             |
| 2    | 🤖 Sistema | Crea sesión de checkout en Stripe         | BillingService      | `POST /api/dealer-billing/subscribe`   |
| 3    | 🏢 Dealer  | Completa pago con tarjeta                 | Stripe Checkout     | —                                      |
| 4    | 🤖 Sistema | Recibe webhook de Stripe                  | BillingService      | `POST /api/webhook/stripe`             |
| 5    | 🤖 Sistema | Activa plan del dealer                    | BillingService      | —                                      |
| 6    | 🤖 Sistema | Notifica al dealer                        | NotificationService | Evento: `billing.subscription.created` |

### P5.2 — Compra de OKLA Coins

| Paso | Actor      | Acción                              | Servicio       | Endpoint                          |
| ---- | ---------- | ----------------------------------- | -------------- | --------------------------------- |
| 1    | 🏢 Dealer  | Selecciona paquete de coins         | Frontend       | `/dealer/publicidad/catalogo`     |
| 2    | 🤖 Sistema | Crea sesión de pago                 | BillingService | `POST /api/okla-coins/purchase`   |
| 3    | 🏢 Dealer  | Completa pago                       | Stripe         | —                                 |
| 4    | 🤖 Sistema | Acredita coins al wallet del dealer | BillingService | Evento: `billing.coins.purchased` |

### P5.3 — Pago de Listado Individual (Seller)

| Paso | Actor      | Acción                         | Servicio            | Endpoint                   |
| ---- | ---------- | ------------------------------ | ------------------- | -------------------------- |
| 1    | 🏷️ Seller  | Publica vehículo               | Frontend            | `/vender/publicar`         |
| 2    | 🤖 Sistema | Cobra $29 por listado          | BillingService      | `POST /api/billing/charge` |
| 3    | 🤖 Sistema | Procesa pago con Stripe        | Stripe              | —                          |
| 4    | 🤖 Sistema | Activa listado si pago exitoso | VehiclesSaleService | —                          |

---

## 6. Reseñas y Reputación

### P6.1 — Escribir Reseña

| Paso | Actor      | Acción                                 | Servicio       | Endpoint            |
| ---- | ---------- | -------------------------------------- | -------------- | ------------------- |
| 1    | 🛒 Buyer   | Accede a perfil del vendedor/dealer    | Frontend       | `/portal/{slug}`    |
| 2    | 🛒 Buyer   | Escribe reseña (1-5 ★, texto)          | Frontend       | —                   |
| 3    | 🤖 Sistema | Valida que el buyer contactó al seller | ContactService | —                   |
| 4    | 🤖 Sistema | Crea reseña                            | ReviewService  | `POST /api/reviews` |
| 5    | 🤖 Sistema | Recalcula promedio del seller          | ReviewService  | —                   |
| 6    | 🤖 Sistema | Actualiza OKLA Score del seller        | ReviewService  | —                   |

### P6.2 — Responder Reseña (Seller/Dealer)

| Paso | Actor      | Acción                                | Servicio      | Endpoint                          |
| ---- | ---------- | ------------------------------------- | ------------- | --------------------------------- |
| 1    | 🏷️/🏢      | Ve notificación de nueva reseña       | Frontend      | —                                 |
| 2    | 🏷️/🏢      | Escribe respuesta                     | Frontend      | `/dealer/resenas`                 |
| 3    | 🤖 Sistema | Guarda respuesta asociada a la reseña | ReviewService | `POST /api/reviews/{id}/response` |

### P6.3 — Reportar Reseña

| Paso | Actor      | Acción                       | Servicio      | Endpoint                          |
| ---- | ---------- | ---------------------------- | ------------- | --------------------------------- |
| 1    | 🏷️/🏢      | Reporta reseña inapropiada   | Frontend      | —                                 |
| 2    | 🤖 Sistema | Crea reporte                 | ReviewService | `POST /api/reviews/{id}/report`   |
| 3    | 👑 Admin   | Revisa reporte               | AdminService  | `GET /api/admin/reviews/reported` |
| 4    | 👑 Admin   | Elimina o mantiene la reseña | AdminService  | `DELETE /api/admin/reviews/{id}`  |

---

## 7. Publicidad

### P7.1 — Comprar Publicidad

| Paso | Actor      | Acción                                        | Servicio           | Endpoint                          |
| ---- | ---------- | --------------------------------------------- | ------------------ | --------------------------------- |
| 1    | 🏢 Dealer  | Accede al catálogo de publicidad              | Frontend           | `/dealer/publicidad/catalogo`     |
| 2    | 🏢 Dealer  | Selecciona producto (banner, spotlight, etc.) | Frontend           | —                                 |
| 3    | 🤖 Sistema | Procesa pago (Stripe o OKLA Coins)            | BillingService     | `POST /api/advertising/purchase`  |
| 4    | 🤖 Sistema | Activa campaña publicitaria                   | AdvertisingService | `POST /api/advertising/campaigns` |
| 5    | 🤖 Sistema | Muestra anuncio en rotación                   | AdvertisingService | `GET /api/advertising/rotation`   |

### P7.2 — Rotación de Anuncios

| Paso | Actor      | Acción                                         | Servicio           | Endpoint                                     |
| ---- | ---------- | ---------------------------------------------- | ------------------ | -------------------------------------------- |
| 1    | 🤖 Sistema | Frontend solicita anuncios para posición       | AdvertisingService | `GET /api/advertising/rotation?position=...` |
| 2    | 🤖 Sistema | Selecciona anuncios según prioridad/relevancia | AdvertisingService | —                                            |
| 3    | 🤖 Sistema | Registra impresión                             | AdvertisingService | `POST /api/advertising/tracking/impression`  |
| 4    | 👤/🛒      | Hace clic en anuncio                           | Frontend           | —                                            |
| 5    | 🤖 Sistema | Registra clic                                  | AdvertisingService | `POST /api/advertising/tracking/click`       |

---

## 8. Administración

### P8.1 — Aprobación de Vehículos

| Paso | Actor      | Acción                                 | Servicio            | Endpoint                                |
| ---- | ---------- | -------------------------------------- | ------------------- | --------------------------------------- |
| 1    | 👑 Admin   | Accede a lista de vehículos pendientes | Frontend            | `/admin/vehiculos`                      |
| 2    | 👑 Admin   | Revisa fotos, descripción, precio      | Frontend            | —                                       |
| 3    | 👑 Admin   | Aprueba o rechaza                      | AdminService        | `POST /api/admin/vehicles/{id}/approve` |
| 4    | 🤖 Sistema | Notifica al vendedor                   | NotificationService | —                                       |

### P8.2 — Gestión de Secciones del Homepage

| Paso | Actor      | Acción                                  | Servicio             | Endpoint                           |
| ---- | ---------- | --------------------------------------- | -------------------- | ---------------------------------- |
| 1    | 👑 Admin   | Accede a gestión de secciones           | Frontend             | `/admin/secciones`                 |
| 2    | 👑 Admin   | Configura categorías, marcas destacadas | AdminService         | `POST /api/admin/content/sections` |
| 3    | 🤖 Sistema | Actualiza caché del homepage            | ConfigurationService | —                                  |
| 4    | 👤 Anónimo | Ve contenido actualizado en homepage    | Frontend             | `/`                                |

### P8.3 — Gestión de Planes y Precios

| Paso | Actor      | Acción                                | Servicio       | Endpoint                    |
| ---- | ---------- | ------------------------------------- | -------------- | --------------------------- |
| 1    | 👑 Admin   | Accede a gestión de planes            | Frontend       | `/admin/planes`             |
| 2    | 👑 Admin   | Modifica precios o features de planes | AdminService   | `PUT /api/admin/plans/{id}` |
| 3    | 🤖 Sistema | Sincroniza con Stripe Products        | BillingService | —                           |

---

## 9. Notificaciones

### P9.1 — Sistema de Notificaciones

| Trigger                 | Tipo         | Canal                              | Servicio                       |
| ----------------------- | ------------ | ---------------------------------- | ------------------------------ |
| Nuevo contacto recibido | Push + Email | NotificationService                | `contact.request.created`      |
| Cita agendada           | Email        | NotificationService                | `appointment.created`          |
| Vehículo aprobado       | Push         | NotificationService                | `vehicle.listing.approved`     |
| Nueva reseña            | Push         | NotificationService                | `review.created`               |
| Plan activado           | Email        | NotificationService                | `billing.subscription.created` |
| KYC aprobado            | Push + Email | NotificationService                | `kyc.verification.completed`   |
| Alerta de precio        | Push         | NotificationService + AlertService | `alert.price.triggered`        |

---

## 10. Monitoreo y Observabilidad

### P10.1 — Health Checks

| Servicio | Endpoint        | Tags                 |
| -------- | --------------- | -------------------- |
| Todos    | `/health`       | Excluye `external`   |
| Todos    | `/health/ready` | Solo `ready`         |
| Todos    | `/health/live`  | Ninguno (siempre OK) |

### P10.2 — Auditoría

| Paso | Actor      | Acción                                  | Servicio     | Endpoint               |
| ---- | ---------- | --------------------------------------- | ------------ | ---------------------- |
| 1    | 🤖 Sistema | Middleware captura todas las acciones   | AuditService | —                      |
| 2    | 🤖 Sistema | Registra usuario, acción, timestamp, IP | AuditService | —                      |
| 3    | 👑 Admin   | Consulta logs de auditoría              | AdminService | `GET /api/admin/audit` |

---

## Diagrama de Flujo Principal

```
👤 Anónimo ──→ 🏠 Homepage ──→ 🔍 Buscar ──→ 📋 Detalle Vehículo
    │                                              │
    ├──→ 📝 Registro ──→ 🛒 Buyer                 │
    │        │                  │                   │
    │        └──→ 🏢 Dealer     ├──→ 📩 Contactar ──┘
    │              │            ├──→ 📅 Agendar Cita
    │              │            ├──→ ❤️ Guardar Favorito
    │              │            └──→ ⭐ Escribir Reseña
    │              │
    │              ├──→ 📤 Publicar Vehículo
    │              ├──→ 💳 Suscribir Plan
    │              ├──→ 🪙 Comprar OKLA Coins
    │              ├──→ 📢 Comprar Publicidad
    │              └──→ 📊 Ver Analytics
    │
    └──→ 🤖 Chat Soporte (SupportAgent)
```

---

_Este documento se actualiza con cada nuevo proceso agregado a la plataforma._
