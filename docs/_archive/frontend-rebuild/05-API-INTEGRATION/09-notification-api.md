# 🔔 09 - Notification API (NotificationService)

**Servicio:** NotificationService  
**Puerto:** 8080  
**Base Path:** `/api/notifications`, `/api/notifications/preferences`  
**Autenticación:** ✅ Requerida (JWT Bearer Token) | 🟡 Opcional para algunos endpoints

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)
7. [Tipos de Notificaciones](#tipos-de-notificaciones)
8. [Canales de Notificación](#canales-de-notificación)
9. [Notas de Seguridad](#notas-de-seguridad)

---

## 📖 Descripción General

El **NotificationService** gestiona todas las notificaciones del sistema: **in-app**, **email**, **SMS** y **push notifications**. Permite enviar notificaciones, gestionar preferencias de usuario y ver historial.

### Casos de Uso

- 🔔 Notificaciones in-app (campana en header)
- 📧 Envío de emails transaccionales
- 📱 SMS para verificación y alertas
- 📲 Push notifications móviles
- ⚙️ Gestión de preferencias de usuario
- 📊 Historial de notificaciones

### Flujo Típico

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   FLUJO DE NOTIFICACIONES                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ EVENTO GENERA NOTIFICACIÓN                                              │
│  ├─> Usuario recibe consulta sobre vehículo                                │
│  ├─> Sistema crea notificación in-app: "Nueva consulta sobre tu Corolla"   │
│  ├─> POST /api/notifications → Crea UserNotification                       │
│  └─> Si preferencias habilitadas → También envía email/SMS/push            │
│                                                                             │
│  2️⃣ USUARIO VE NOTIFICACIÓN EN HEADER                                       │
│  ├─> GET /api/notifications/unread/count → Badge muestra "5"               │
│  ├─> Click en campana → Dropdown con lista                                 │
│  ├─> GET /api/notifications?unreadOnly=true                                │
│  └─> Ve notificaciones recientes con íconos y links                        │
│                                                                             │
│  3️⃣ USUARIO INTERACTÚA                                                      │
│  ├─> Click en notificación → PATCH /api/notifications/{id}/read            │
│  ├─> Badge actualiza a "4" automáticamente                                 │
│  ├─> Navegación automática al link (ej: /inquiries/123)                    │
│  └─> O puede eliminar: DELETE /api/notifications/{id}                      │
│                                                                             │
│  4️⃣ GESTIÓN DE PREFERENCIAS                                                 │
│  ├─> Usuario va a Settings → Notificaciones                                │
│  ├─> GET /api/notifications/preferences                                    │
│  ├─> Deshabilita "Nuevas consultas" por email                              │
│  ├─> PUT /api/notifications/preferences/NEW_INQUIRY                        │
│  └─> Ahora solo recibirá in-app, sin emails                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Endpoints Disponibles

### UserNotificationsController (In-App Notifications)

#### 1. GET `/api/notifications` - Listar Notificaciones

Obtiene notificaciones paginadas del usuario actual (con filtros).

**Auth:** ✅ Required  
**Query Params:**

- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `unreadOnly` (bool, default: false) - Solo no leídas

**Response 200:**

```json
{
  "notifications": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
      "type": "NEW_INQUIRY",
      "title": "Nueva consulta sobre tu Toyota Corolla",
      "message": "Juan Pérez pregunta: ¿Está disponible para prueba?",
      "icon": "📧",
      "link": "/inquiries/abc-123",
      "isRead": false,
      "createdAt": "2026-01-30T10:30:00Z",
      "expiresAt": null
    },
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
      "type": "PRICE_DROP_ALERT",
      "title": "¡Bajó el precio del BMW X5!",
      "message": "De $45,000 a $42,500 (-$2,500)",
      "icon": "💰",
      "link": "/vehicles/bmw-x5-2023",
      "isRead": true,
      "createdAt": "2026-01-29T15:20:00Z",
      "expiresAt": "2026-02-05T23:59:59Z"
    }
  ],
  "total": 24,
  "unreadCount": 5,
  "page": 1,
  "pageSize": 20,
  "totalPages": 2
}
```

---

#### 2. GET `/api/notifications/unread/count` - Contador de Sin Leer

Obtiene el número de notificaciones sin leer para mostrar badge.

**Auth:** ✅ Required  
**Query Params:** Ninguno

**Response 200:**

```json
{
  "count": 5
}
```

---

#### 3. PATCH `/api/notifications/{notificationId}/read` - Marcar como Leída

Marca una notificación específica como leída.

**Auth:** ✅ Required  
**Path Params:**

- `notificationId` (UUID)

**Response 200:**

```json
{
  "message": "Notification marked as read"
}
```

**Response 404:** Notificación no encontrada

---

#### 4. PATCH `/api/notifications/read-all` - Marcar Todas como Leídas

Marca todas las notificaciones del usuario como leídas.

**Auth:** ✅ Required  
**Query Params:** Ninguno

**Response 200:**

```json
{
  "message": "All notifications marked as read"
}
```

---

#### 5. DELETE `/api/notifications/{notificationId}` - Eliminar Notificación

Elimina una notificación específica.

**Auth:** ✅ Required  
**Path Params:**

- `notificationId` (UUID)

**Response 200:**

```json
{
  "message": "Notification deleted"
}
```

---

#### 6. DELETE `/api/notifications/read` - Eliminar Todas las Leídas

Elimina todas las notificaciones marcadas como leídas.

**Auth:** ✅ Required  
**Query Params:** Ninguno

**Response 200:**

```json
{
  "message": "Deleted 12 read notifications"
}
```

---

#### 7. POST `/api/notifications` - Crear Notificación (Sistema/Admin)

Crea una nueva notificación in-app (uso interno del sistema).

**Auth:** ✅ Required (Admin/System)  
**Request Body:**

```json
{
  "userId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "type": "NEW_INQUIRY",
  "title": "Nueva consulta",
  "message": "Tienes una nueva consulta sobre tu vehículo",
  "icon": "📧",
  "link": "/inquiries/123",
  "dealerId": null,
  "expiresAt": "2026-02-15T23:59:59Z"
}
```

**Response 201:**

```json
{
  "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
  "userId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "type": "NEW_INQUIRY",
  "title": "Nueva consulta",
  "message": "Tienes una nueva consulta sobre tu vehículo",
  "icon": "📧",
  "link": "/inquiries/123",
  "isRead": false,
  "createdAt": "2026-01-30T10:30:00Z",
  "expiresAt": "2026-02-15T23:59:59Z"
}
```

---

#### 8. POST `/api/notifications/bulk` - Envío Masivo (Sistema/Admin)

Envía la misma notificación a múltiples usuarios.

**Auth:** ✅ Required (Admin/System)  
**Request Body:**

```json
{
  "userIds": ["user-id-1", "user-id-2", "user-id-3"],
  "type": "SYSTEM_ANNOUNCEMENT",
  "title": "Mantenimiento programado",
  "message": "Habrá mantenimiento mañana de 2-4 AM",
  "icon": "🛠️",
  "link": null,
  "expiresAt": "2026-02-01T00:00:00Z"
}
```

**Response 200:**

```json
{
  "message": "Created 3 notifications successfully"
}
```

---

### NotificationPreferencesController

#### 9. GET `/api/notifications/preferences` - Obtener Preferencias

Obtiene las preferencias de notificación del usuario actual.

**Auth:** ✅ Required  
**Headers:**

- `X-Dealer-Id` (opcional) - Para dealers
- `X-User-Id` (opcional) - Override para admin

**Response 200:**

```json
[
  {
    "type": "NEW_INQUIRY",
    "label": "Nuevas consultas",
    "description": "Cuando alguien te contacta sobre un vehículo",
    "enabled": true,
    "channels": ["in_app", "email", "push"]
  },
  {
    "type": "PRICE_DROP_ALERT",
    "label": "Alertas de precio",
    "description": "Cuando baja el precio de un vehículo guardado",
    "enabled": true,
    "channels": ["in_app", "email"]
  },
  {
    "type": "SAVED_SEARCH_MATCH",
    "label": "Nuevos vehículos coincidentes",
    "description": "Cuando hay vehículos que coinciden con tus búsquedas guardadas",
    "enabled": false,
    "channels": ["in_app"]
  },
  {
    "type": "VEHICLE_SOLD",
    "label": "Vehículo vendido",
    "description": "Cuando uno de tus vehículos se marca como vendido",
    "enabled": true,
    "channels": ["in_app", "email", "sms"]
  }
]
```

---

#### 10. PUT `/api/notifications/preferences/{type}` - Actualizar Preferencia

Actualiza una preferencia específica.

**Auth:** ✅ Required  
**Path Params:**

- `type` (string) - Tipo de notificación (ej: "NEW_INQUIRY")

**Request Body:**

```json
{
  "enabled": true,
  "channels": ["in_app", "email"]
}
```

**Response 200:**

```json
{
  "type": "NEW_INQUIRY",
  "label": "Nuevas consultas",
  "description": "Cuando alguien te contacta sobre un vehículo",
  "enabled": true,
  "channels": ["in_app", "email"]
}
```

---

#### 11. PUT `/api/notifications/preferences` - Actualizar Múltiples Preferencias

Actualiza varias preferencias en una sola llamada.

**Auth:** ✅ Required  
**Request Body:**

```json
[
  {
    "type": "NEW_INQUIRY",
    "enabled": true,
    "channels": ["in_app", "email"]
  },
  {
    "type": "PRICE_DROP_ALERT",
    "enabled": false,
    "channels": []
  }
]
```

**Response 200:**

```json
[
  {
    "type": "NEW_INQUIRY",
    "label": "Nuevas consultas",
    "enabled": true,
    "channels": ["in_app", "email"]
  },
  {
    "type": "PRICE_DROP_ALERT",
    "label": "Alertas de precio",
    "enabled": false,
    "channels": []
  }
]
```

---

### NotificationsController (Email/SMS/Push)

#### 12. POST `/api/notifications/email` - Enviar Email

Envía un email transaccional.

**Auth:** ✅ Required (Sistema)  
**Request Body:**

```json
{
  "to": "user@example.com",
  "subject": "Verificación de cuenta",
  "body": "<h1>Hola</h1><p>Haz clic para verificar...</p>",
  "isHtml": true,
  "templateId": "email-verification"
}
```

**Response 200:**

```json
{
  "success": true,
  "notificationId": "abc-123-def",
  "sentAt": "2026-01-30T10:30:00Z"
}
```

---

#### 13. POST `/api/notifications/sms` - Enviar SMS

Envía un SMS.

**Auth:** ✅ Required (Sistema)  
**Request Body:**

```json
{
  "to": "+1-809-555-1234",
  "message": "Tu código de verificación es: 123456"
}
```

**Response 200:**

```json
{
  "success": true,
  "notificationId": "xyz-456-ghi",
  "sentAt": "2026-01-30T10:30:00Z"
}
```

---

#### 14. POST `/api/notifications/push` - Enviar Push Notification

Envía una push notification móvil.

**Auth:** ✅ Required (Sistema)  
**Request Body:**

```json
{
  "userId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "title": "Nueva consulta",
  "body": "Tienes una consulta sobre tu Toyota Corolla",
  "data": {
    "link": "/inquiries/123",
    "action": "OPEN_INQUIRY"
  }
}
```

**Response 200:**

```json
{
  "success": true,
  "notificationId": "push-789-jkl",
  "sentAt": "2026-01-30T10:30:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// USER NOTIFICATION TYPES (In-App)
// ============================================================================

export interface UserNotification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  icon?: string;
  link?: string;
  isRead: boolean;
  createdAt: string;
  expiresAt?: string;
  dealerId?: string;
}

export type NotificationType =
  | "NEW_INQUIRY" // Nueva consulta
  | "INQUIRY_RESPONSE" // Respuesta a consulta
  | "PRICE_DROP_ALERT" // Alerta de precio
  | "SAVED_SEARCH_MATCH" // Vehículo coincide con búsqueda guardada
  | "VEHICLE_SOLD" // Tu vehículo se vendió
  | "VEHICLE_APPROVED" // Tu listing fue aprobado
  | "VEHICLE_REJECTED" // Tu listing fue rechazado
  | "VERIFICATION_COMPLETE" // Verificación completada
  | "PAYMENT_SUCCESS" // Pago exitoso
  | "PAYMENT_FAILED" // Pago fallido
  | "SUBSCRIPTION_EXPIRING" // Suscripción por vencer
  | "SYSTEM_ANNOUNCEMENT" // Anuncio del sistema
  | "MAINTENANCE_SCHEDULED"; // Mantenimiento programado

export interface UserNotificationsListResponse {
  notifications: UserNotification[];
  total: number;
  unreadCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UnreadCountResponse {
  count: number;
}

export interface CreateUserNotificationRequest {
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  icon?: string;
  link?: string;
  dealerId?: string;
  expiresAt?: string;
}

export interface BulkNotificationRequest {
  userIds: string[];
  type: NotificationType;
  title: string;
  message: string;
  icon?: string;
  link?: string;
  expiresAt?: string;
}

// ============================================================================
// NOTIFICATION PREFERENCES TYPES
// ============================================================================

export interface NotificationPreference {
  type: NotificationType;
  label: string;
  description: string;
  enabled: boolean;
  channels: NotificationChannel[];
}

export type NotificationChannel =
  | "in_app" // Notificación in-app (campana)
  | "email" // Email
  | "sms" // SMS
  | "push"; // Push notification móvil

export interface UpdateNotificationPreferenceRequest {
  type: NotificationType;
  enabled: boolean;
  channels?: NotificationChannel[];
}

// ============================================================================
// EMAIL/SMS/PUSH TYPES
// ============================================================================

export interface SendEmailNotificationRequest {
  to: string;
  subject: string;
  body: string;
  isHtml?: boolean;
  templateId?: string;
}

export interface SendSmsNotificationRequest {
  to: string;
  message: string;
}

export interface SendPushNotificationRequest {
  userId: string;
  title: string;
  body: string;
  data?: Record<string, any>;
}

export interface NotificationResponse {
  success: boolean;
  notificationId: string;
  sentAt: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/notificationService.ts
import { apiClient } from "./api-client";
import type {
  UserNotification,
  UserNotificationsListResponse,
  UnreadCountResponse,
  CreateUserNotificationRequest,
  BulkNotificationRequest,
  NotificationPreference,
  UpdateNotificationPreferenceRequest,
  SendEmailNotificationRequest,
  SendSmsNotificationRequest,
  SendPushNotificationRequest,
  NotificationResponse,
} from "@/types/notification";

class NotificationService {
  // ============================================================================
  // USER NOTIFICATIONS (IN-APP)
  // ============================================================================

  /**
   * Obtener notificaciones paginadas
   */
  async getNotifications(params?: {
    page?: number;
    pageSize?: number;
    unreadOnly?: boolean;
  }): Promise<UserNotificationsListResponse> {
    const response = await apiClient.get<UserNotificationsListResponse>(
      "/api/notifications",
      { params },
    );
    return response.data;
  }

  /**
   * Obtener contador de notificaciones sin leer
   */
  async getUnreadCount(): Promise<number> {
    const response = await apiClient.get<UnreadCountResponse>(
      "/api/notifications/unread/count",
    );
    return response.data.count;
  }

  /**
   * Marcar notificación como leída
   */
  async markAsRead(notificationId: string): Promise<void> {
    await apiClient.patch(`/api/notifications/${notificationId}/read`);
  }

  /**
   * Marcar todas las notificaciones como leídas
   */
  async markAllAsRead(): Promise<void> {
    await apiClient.patch("/api/notifications/read-all");
  }

  /**
   * Eliminar notificación
   */
  async deleteNotification(notificationId: string): Promise<void> {
    await apiClient.delete(`/api/notifications/${notificationId}`);
  }

  /**
   * Eliminar todas las notificaciones leídas
   */
  async deleteAllRead(): Promise<void> {
    await apiClient.delete("/api/notifications/read");
  }

  /**
   * Crear notificación (sistema/admin)
   */
  async createNotification(
    request: CreateUserNotificationRequest,
  ): Promise<UserNotification> {
    const response = await apiClient.post<UserNotification>(
      "/api/notifications",
      request,
    );
    return response.data;
  }

  /**
   * Envío masivo de notificaciones
   */
  async bulkCreateNotifications(
    request: BulkNotificationRequest,
  ): Promise<void> {
    await apiClient.post("/api/notifications/bulk", request);
  }

  // ============================================================================
  // NOTIFICATION PREFERENCES
  // ============================================================================

  /**
   * Obtener preferencias de notificación
   */
  async getPreferences(): Promise<NotificationPreference[]> {
    const response = await apiClient.get<NotificationPreference[]>(
      "/api/notifications/preferences",
    );
    return response.data;
  }

  /**
   * Actualizar preferencia específica
   */
  async updatePreference(
    type: string,
    request: UpdateNotificationPreferenceRequest,
  ): Promise<NotificationPreference> {
    const response = await apiClient.put<NotificationPreference>(
      `/api/notifications/preferences/${type}`,
      request,
    );
    return response.data;
  }

  /**
   * Actualizar múltiples preferencias
   */
  async updatePreferences(
    requests: UpdateNotificationPreferenceRequest[],
  ): Promise<NotificationPreference[]> {
    const response = await apiClient.put<NotificationPreference[]>(
      "/api/notifications/preferences",
      requests,
    );
    return response.data;
  }

  // ============================================================================
  // EMAIL/SMS/PUSH (Sistema)
  // ============================================================================

  /**
   * Enviar email (sistema)
   */
  async sendEmail(
    request: SendEmailNotificationRequest,
  ): Promise<NotificationResponse> {
    const response = await apiClient.post<NotificationResponse>(
      "/api/notifications/email",
      request,
    );
    return response.data;
  }

  /**
   * Enviar SMS (sistema)
   */
  async sendSms(
    request: SendSmsNotificationRequest,
  ): Promise<NotificationResponse> {
    const response = await apiClient.post<NotificationResponse>(
      "/api/notifications/sms",
      request,
    );
    return response.data;
  }

  /**
   * Enviar push notification (sistema)
   */
  async sendPush(
    request: SendPushNotificationRequest,
  ): Promise<NotificationResponse> {
    const response = await apiClient.post<NotificationResponse>(
      "/api/notifications/push",
      request,
    );
    return response.data;
  }
}

export const notificationService = new NotificationService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useNotifications.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notificationService } from "@/services/notificationService";
import type {
  CreateUserNotificationRequest,
  UpdateNotificationPreferenceRequest,
} from "@/types/notification";

// ============================================================================
// QUERY KEYS
// ============================================================================

export const notificationKeys = {
  all: ["notifications"] as const,
  list: (params?: any) => [...notificationKeys.all, "list", params] as const,
  unreadCount: () => [...notificationKeys.all, "unread-count"] as const,
  preferences: () => [...notificationKeys.all, "preferences"] as const,
};

// ============================================================================
// QUERIES
// ============================================================================

/**
 * Hook: Obtener notificaciones
 */
export function useNotifications(params?: {
  page?: number;
  pageSize?: number;
  unreadOnly?: boolean;
}) {
  return useQuery({
    queryKey: notificationKeys.list(params),
    queryFn: () => notificationService.getNotifications(params),
  });
}

/**
 * Hook: Contador de notificaciones sin leer
 */
export function useUnreadNotificationsCount() {
  return useQuery({
    queryKey: notificationKeys.unreadCount(),
    queryFn: () => notificationService.getUnreadCount(),
    refetchInterval: 30000, // Refetch cada 30 segundos
  });
}

/**
 * Hook: Obtener preferencias
 */
export function useNotificationPreferences() {
  return useQuery({
    queryKey: notificationKeys.preferences(),
    queryFn: () => notificationService.getPreferences(),
  });
}

// ============================================================================
// MUTATIONS
// ============================================================================

/**
 * Hook: Marcar notificación como leída
 */
export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationService.markAsRead(notificationId),
    onSuccess: () => {
      // Invalidar contador y lista
      queryClient.invalidateQueries({
        queryKey: notificationKeys.unreadCount(),
      });
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

/**
 * Hook: Marcar todas como leídas
 */
export function useMarkAllAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: notificationKeys.unreadCount(),
      });
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

/**
 * Hook: Eliminar notificación
 */
export function useDeleteNotification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationService.deleteNotification(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

/**
 * Hook: Eliminar todas las leídas
 */
export function useDeleteAllRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => notificationService.deleteAllRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

/**
 * Hook: Actualizar preferencia
 */
export function useUpdateNotificationPreference() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      type,
      request,
    }: {
      type: string;
      request: UpdateNotificationPreferenceRequest;
    }) => notificationService.updatePreference(type, request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: notificationKeys.preferences(),
      });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### 1. NotificationDropdown - Campana con Dropdown

```typescript
// src/components/notifications/NotificationDropdown.tsx
import { useState } from "react";
import { useNotifications, useUnreadNotificationsCount, useMarkNotificationAsRead } from "@/hooks/useNotifications";
import { FiBell, FiCheck, FiTrash2 } from "react-icons/fi";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import { Link } from "react-router-dom";

export const NotificationDropdown = () => {
  const [isOpen, setIsOpen] = useState(false);
  const { data: count } = useUnreadNotificationsCount();
  const { data: notifications } = useNotifications({ unreadOnly: true, pageSize: 5 });
  const markAsReadMutation = useMarkNotificationAsRead();

  const handleNotificationClick = async (notificationId: string, link?: string) => {
    await markAsReadMutation.mutateAsync(notificationId);
    setIsOpen(false);

    if (link) {
      window.location.href = link;
    }
  };

  return (
    <div className="relative">
      {/* Bell Icon */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-gray-700 hover:bg-gray-100 rounded-full"
      >
        <FiBell className="h-6 w-6" />
        {count && count > 0 && (
          <span className="absolute -top-1 -right-1 bg-red-600 text-white text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center">
            {count > 9 ? "9+" : count}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-96 bg-white rounded-lg shadow-xl border z-50">
          {/* Header */}
          <div className="px-4 py-3 border-b flex items-center justify-between">
            <h3 className="font-semibold text-lg">Notificaciones</h3>
            <Link
              to="/notifications"
              onClick={() => setIsOpen(false)}
              className="text-sm text-blue-600 hover:underline"
            >
              Ver todas
            </Link>
          </div>

          {/* List */}
          <div className="max-h-[400px] overflow-y-auto">
            {notifications && notifications.notifications.length > 0 ? (
              notifications.notifications.map((notification) => (
                <div
                  key={notification.id}
                  onClick={() => handleNotificationClick(notification.id, notification.link)}
                  className="px-4 py-3 hover:bg-gray-50 cursor-pointer border-b last:border-b-0"
                >
                  <div className="flex items-start gap-3">
                    {/* Icon */}
                    <span className="text-2xl">{notification.icon || "🔔"}</span>

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-sm">{notification.title}</p>
                      <p className="text-sm text-gray-600 mt-1 line-clamp-2">
                        {notification.message}
                      </p>
                      <p className="text-xs text-gray-400 mt-1">
                        {formatDistanceToNow(new Date(notification.createdAt), {
                          addSuffix: true,
                          locale: es,
                        })}
                      </p>
                    </div>

                    {/* Unread Dot */}
                    {!notification.isRead && (
                      <div className="w-2 h-2 bg-blue-600 rounded-full mt-1"></div>
                    )}
                  </div>
                </div>
              ))
            ) : (
              <div className="px-4 py-12 text-center text-gray-500">
                <FiBell className="h-12 w-12 mx-auto mb-2 opacity-30" />
                <p>No tienes notificaciones nuevas</p>
              </div>
            )}
          </div>

          {/* Footer */}
          {count && count > 0 && (
            <div className="px-4 py-2 border-t bg-gray-50">
              <button
                onClick={() => {
                  // markAllAsRead();
                  setIsOpen(false);
                }}
                className="text-sm text-blue-600 hover:underline"
              >
                Marcar todas como leídas
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
```

---

### 2. NotificationPreferencesPage - Página de Preferencias

```typescript
// src/pages/NotificationPreferencesPage.tsx
import {
  useNotificationPreferences,
  useUpdateNotificationPreference,
} from "@/hooks/useNotifications";
import { useState } from "react";
import type { NotificationChannel } from "@/types/notification";

export const NotificationPreferencesPage = () => {
  const { data: preferences, isLoading } = useNotificationPreferences();
  const updateMutation = useUpdateNotificationPreference();

  const handleToggleChannel = async (
    type: string,
    channel: NotificationChannel,
    currentChannels: NotificationChannel[]
  ) => {
    const newChannels = currentChannels.includes(channel)
      ? currentChannels.filter((c) => c !== channel)
      : [...currentChannels, channel];

    await updateMutation.mutateAsync({
      type,
      request: {
        type,
        enabled: true,
        channels: newChannels,
      },
    });
  };

  if (isLoading) {
    return <div className="text-center py-8">Cargando preferencias...</div>;
  }

  return (
    <div className="container mx-auto py-8 max-w-4xl">
      <h1 className="text-3xl font-bold mb-2">Preferencias de Notificaciones</h1>
      <p className="text-gray-600 mb-8">
        Elige cómo quieres recibir notificaciones para cada tipo de evento
      </p>

      <div className="space-y-6">
        {preferences?.map((pref) => (
          <div key={pref.type} className="bg-white p-6 rounded-lg border">
            {/* Header */}
            <div className="flex items-start justify-between mb-4">
              <div>
                <h3 className="font-semibold text-lg">{pref.label}</h3>
                <p className="text-sm text-gray-600">{pref.description}</p>
              </div>
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={pref.enabled}
                  onChange={(e) =>
                    updateMutation.mutate({
                      type: pref.type,
                      request: {
                        type: pref.type,
                        enabled: e.target.checked,
                        channels: pref.channels,
                      },
                    })
                  }
                  className="w-5 h-5 text-blue-600"
                />
              </label>
            </div>

            {/* Channels */}
            {pref.enabled && (
              <div className="flex gap-4">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={pref.channels.includes("in_app")}
                    onChange={() =>
                      handleToggleChannel(pref.type, "in_app", pref.channels)
                    }
                  />
                  <span className="text-sm">🔔 App</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={pref.channels.includes("email")}
                    onChange={() =>
                      handleToggleChannel(pref.type, "email", pref.channels)
                    }
                  />
                  <span className="text-sm">📧 Email</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={pref.channels.includes("sms")}
                    onChange={() =>
                      handleToggleChannel(pref.type, "sms", pref.channels)
                    }
                  />
                  <span className="text-sm">📱 SMS</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={pref.channels.includes("push")}
                    onChange={() =>
                      handleToggleChannel(pref.type, "push", pref.channels)
                    }
                  />
                  <span className="text-sm">📲 Push</span>
                </label>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};
```

---

## 📊 Tipos de Notificaciones

| Tipo                    | Descripción                          | Canales Recomendados |
| ----------------------- | ------------------------------------ | -------------------- |
| `NEW_INQUIRY`           | Nueva consulta sobre vehículo        | In-App, Email, Push  |
| `INQUIRY_RESPONSE`      | Respuesta a tu consulta              | In-App, Email, Push  |
| `PRICE_DROP_ALERT`      | Bajó el precio de vehículo guardado  | In-App, Email        |
| `SAVED_SEARCH_MATCH`    | Nuevo vehículo coincide con búsqueda | In-App, Email        |
| `VEHICLE_SOLD`          | Tu vehículo se vendió                | In-App, Email, SMS   |
| `VEHICLE_APPROVED`      | Tu listing fue aprobado              | In-App, Email        |
| `VEHICLE_REJECTED`      | Tu listing fue rechazado             | In-App, Email        |
| `VERIFICATION_COMPLETE` | Verificación de cuenta completada    | In-App, Email        |
| `PAYMENT_SUCCESS`       | Pago exitoso                         | In-App, Email        |
| `PAYMENT_FAILED`        | Pago fallido                         | In-App, Email, SMS   |
| `SUBSCRIPTION_EXPIRING` | Suscripción por vencer               | In-App, Email, SMS   |
| `SYSTEM_ANNOUNCEMENT`   | Anuncio del sistema                  | In-App               |
| `MAINTENANCE_SCHEDULED` | Mantenimiento programado             | In-App, Email        |

---

## 🔒 Notas de Seguridad

1. **Autorización:** Solo se ven notificaciones del usuario actual
2. **Rate Limiting:** Máximo 100 notificaciones por usuario por hora
3. **Expiración:** Notificaciones pueden tener fecha de expiración
4. **GDPR:** Usuario puede exportar/eliminar todas sus notificaciones
5. **Push Tokens:** Se deben registrar dispositivos para push notifications

---

## 🎉 Resumen

✅ **14 Endpoints documentados**  
✅ **TypeScript Types completos**  
✅ **Service Layer** con 14 métodos  
✅ **React Query Hooks** optimizados  
✅ **2 Componentes UI** completos (Dropdown + Preferences)  
✅ **Real-time updates** con refetchInterval  
✅ **Multi-canal** (In-App, Email, SMS, Push)

---

_Última actualización: Enero 30, 2026_
