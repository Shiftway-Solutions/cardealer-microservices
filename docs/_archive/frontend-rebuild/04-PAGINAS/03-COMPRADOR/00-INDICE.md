# 📁 03-COMPRADOR - Flujos del Comprador

> **Descripción:** Páginas y flujos para usuarios compradores  
> **Total:** 10 documentos (fusionados)  
> **Prioridad:** 🟠 P1 - Engagement del usuario  
> **Última actualización:** Enero 30, 2026

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                                | Descripción                                          | Prioridad |
| --- | ------------------------------------------------------ | ---------------------------------------------------- | --------- |
| 1   | [01-perfil.md](01-perfil.md)                           | Perfil del usuario                                   | P1        |
| 2   | [02-alertas-busquedas.md](02-alertas-busquedas.md)     | Alertas de precio y búsquedas guardadas + Comparador | P1        |
| 3   | [03-notificaciones.md](03-notificaciones.md)           | Centro de notificaciones                             | P1        |
| 4   | [04-recomendaciones.md](04-recomendaciones.md)         | Recomendaciones personalizadas (IA)                  | P2        |
| 5   | [05-inquiries-messaging.md](05-inquiries-messaging.md) | Mensajes y consultas                                 | P1        |
| 6   | [06-reviews-reputacion.md](06-reviews-reputacion.md)   | Reviews completo + Request/Response                  | P1        |
| 7   | [07-chatbot.md](07-chatbot.md)                         | Chatbot asistente                                    | P2        |
| 8   | [08-favorites-compare.md](08-favorites-compare.md)     | Favoritos y comparación                              | P1        |
| 9   | [09-user-dashboard.md](09-user-dashboard.md)           | Dashboard del usuario                                | P1        |
| 10  | [10-user-messages.md](10-user-messages.md)             | Bandeja de mensajes                                  | P1        |

---

## 🎯 Orden de Implementación para IA

```
1. 01-perfil.md              → Perfil básico del usuario
2. 09-user-dashboard.md      → Dashboard del comprador
3. 08-favorites-compare.md   → Favoritos y comparación
4. 02-alertas-busquedas.md   → Alertas de precio (incluye comparador)
5. 03-notificaciones.md      → Centro de notificaciones
6. 05-inquiries-messaging.md → Sistema de mensajes
7. 10-user-messages.md       → Bandeja de mensajes
8. 06-reviews-reputacion.md  → Sistema de reviews completo
9. 04-recomendaciones.md     → Recomendaciones IA
10. 07-chatbot.md            → Chatbot
```

---

## 🔗 Dependencias Externas

- **02-AUTH/**: Autenticación requerida
- **01-PUBLICO/**: VehicleCard, detalle de vehículo
- **05-API-INTEGRATION/**: users-api, notifications-api, reviews-api

---

## 📊 APIs Utilizadas

| Servicio              | Endpoints Principales                                  |
| --------------------- | ------------------------------------------------------ |
| UserService           | GET /users/me, PUT /users/profile                      |
| AlertService          | GET /alerts, POST /alerts, DELETE /alerts/:id          |
| NotificationService   | GET /notifications, PUT /notifications/:id/read        |
| ReviewService         | GET /reviews, POST /reviews                            |
| FavoritesService      | GET /favorites, POST /favorites, DELETE /favorites/:id |
| RecommendationService | GET /recommendations                                   |
| ChatbotService        | POST /chatbot/message                                  |
