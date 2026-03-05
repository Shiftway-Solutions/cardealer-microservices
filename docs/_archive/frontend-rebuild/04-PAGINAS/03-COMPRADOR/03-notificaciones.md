---
title: "25. Sistema de Notificaciones (Notification Center)"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 25. Sistema de Notificaciones (Notification Center)

**Objetivo:** Centro unificado de notificaciones con real-time updates vía WebSocket, múltiples tipos de notificaciones (mensajes, favoritos, precio, reviews, sistema), gestión completa (marcar leídas, eliminar, filtrar), y preferencias por canal (email, push, SMS, in-app).

**Prioridad:** P2 (Baja - Engagement)  
**Complejidad:** 🟡 Media (WebSocket, Multiple channels, Preferences)  
**Dependencias:** NotificationService (✅ YA IMPLEMENTADO), AlertService, MessageService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes](#componentes)
4. [WebSocket Integration](#websocket-integration)
5. [Hooks y Servicios](#hooks-y-servicios)
6. [Tipos TypeScript](#tipos-typescript)
7. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### Sistema de Notificaciones

```
┌───────────────────────────────────────────────────────────────────────────┐
│                     NOTIFICATION SYSTEM FLOW                              │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  Evento del sistema (mensaje, precio, etc.)                              │
│      ↓                                                                    │
│  NotificationService.CreateNotification()                                │
│      ├─ Guardar en DB                                                    │
│      ├─ Check user preferences                                           │
│      └─ Dispatch a canales configurados:                                 │
│          ├─ In-App (WebSocket → realtime)                                │
│          ├─ Email (si enabled)                                           │
│          ├─ Push (si enabled + FCM token)                                │
│          └─ SMS (si enabled + phone verified)                            │
│      ↓                                                                    │
│  Frontend recibe por WebSocket:                                          │
│  ├─ Update badge count (navbar)                                          │
│  ├─ Show toast notification (sonner)                                     │
│  └─ Update notification list (React Query cache)                         │
│      ↓                                                                    │
│  Usuario interactúa:                                                     │
│  ├─ Click → Redirigir a entidad (vehículo, mensaje, etc.)               │
│  ├─ Mark as read → Badge count -1                                        │
│  └─ Delete → Remove del list                                             │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

### Tipos de Notificaciones

```typescript
enum NotificationType {
  // Messages
  NEW_MESSAGE = "new_message", // Nuevo mensaje recibido
  MESSAGE_REPLY = "message_reply", // Respuesta a tu mensaje

  // Favorites
  FAVORITE_PRICE_DROP = "favorite_price_drop", // Precio bajó en favorito
  FAVORITE_SOLD = "favorite_sold", // Favorito se vendió

  // Searches
  SEARCH_NEW_MATCH = "search_new_match", // Nuevo match en búsqueda

  // Reviews
  NEW_REVIEW = "new_review", // Nueva review en tu vehículo
  REVIEW_REPLY = "review_reply", // Respuesta a tu review

  // System
  ACCOUNT_VERIFIED = "account_verified", // Cuenta verificada
  SUBSCRIPTION_EXPIRING = "subscription_expiring", // Suscripción por vencer
  MAINTENANCE_SCHEDULED = "maintenance_scheduled", // Mantenimiento programado
}
```

---

## 🔌 BACKEND API

### NotificationService Endpoints (Ya Implementado ✅)

```typescript
// filepath: docs/backend/NotificationService-API.md

GET    /api/notifications                        # Lista de notificaciones
GET    /api/notifications/unread-count           # Contador de no leídas
POST   /api/notifications/{id}/read              # Marcar como leída
POST   /api/notifications/mark-all-read          # Marcar todas leídas
DELETE /api/notifications/{id}                   # Eliminar notificación
DELETE /api/notifications/clear-all              # Eliminar todas

GET    /api/notifications/preferences            # Preferencias del usuario
PUT    /api/notifications/preferences            # Actualizar preferencias

// WebSocket
WS     /ws/notifications                         # Real-time updates
```

### Payload Examples

```json
// GET /api/notifications Response
{
  "items": [
    {
      "id": "notif_123",
      "userId": "user_456",
      "type": "new_message",
      "title": "Nuevo mensaje de Juan Pérez",
      "message": "Hola, estoy interesado en tu Toyota...",
      "entityType": "conversation",
      "entityId": "conv_789",
      "imageUrl": "https://cdn.okla.com.do/avatar/juan.jpg",
      "isRead": false,
      "createdAt": "2026-01-08T10:30:00Z"
    },
    {
      "id": "notif_124",
      "type": "favorite_price_drop",
      "title": "¡Precio rebajado!",
      "message": "Honda Civic 2022 bajó de $25k a $23k",
      "entityType": "vehicle",
      "entityId": "veh_999",
      "imageUrl": "https://cdn.okla.com.do/vehicles/honda-civic.jpg",
      "isRead": false,
      "createdAt": "2026-01-08T09:15:00Z"
    }
  ],
  "unreadCount": 5,
  "totalCount": 48,
  "page": 1,
  "pageSize": 20
}

// PUT /api/notifications/preferences Body
{
  "channels": {
    "email": {
      "enabled": true,
      "types": ["new_message", "search_new_match", "subscription_expiring"]
    },
    "push": {
      "enabled": true,
      "types": ["new_message", "favorite_price_drop"]
    },
    "sms": {
      "enabled": false,
      "types": []
    },
    "inApp": {
      "enabled": true,
      "types": ["*"] // All types
    }
  },
  "quietHours": {
    "enabled": true,
    "start": "22:00",
    "end": "08:00"
  }
}

// CONSENT-PREF-001: POST /api/consent/preferences (Marketing)
{
  "consentType": "marketing_email",
  "granted": true,
  "channel": "email",
  "categories": ["newsletter", "promotions", "vehicle_alerts"],
  "timestamp": "2026-01-29T10:30:00Z",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "source": "preferences_page",
  "version": "1.0"
}

// CONSENT-PREF-002: Granularidad por Canal
{
  "email": {
    "transactional": true,    // Obligatorio (no desactivable)
    "security": true,          // Obligatorio
    "marketing_okla": false,   // Opt-in
    "partners": false,         // Opt-in
    "alerts": true,            // Opt-in
    "surveys": false           // Opt-in
  },
  "sms": {
    "security": true,          // Obligatorio (2FA)
    "alerts": false,           // Opt-in
    "marketing": false         // Opt-in
  },
  "push": {
    "messages": true,          // Recomendado
    "updates": true,           // Recomendado
    "recommendations": false   // Opt-in
  },
  "whatsapp": {
    "marketing": false,        // Opt-in
    "alerts": false            // Opt-in
  }
}
```

---

## 📧 CONSENTIMIENTO DE COMUNICACIONES (Ley 172-13)

### Procesos de Implementación

#### CONSENT-REG-001: Registro con Checkboxes

```typescript
// Ya implementado en RegisterForm (ver 07-auth.md)
// ✅ Checkboxes NO pre-marcados
// ✅ Términos separados de marketing
// ✅ Partners separado de OKLA
```

#### CONSENT-PREF-001: Centro de Preferencias

**Ruta:** `/settings/notifications/preferences`

```typescript
// filepath: src/app/settings/notifications/preferences/page.tsx
import { ConsentPreferencesForm } from '@/components/consent/ConsentPreferencesForm';

export default function ConsentPreferencesPage() {
  return (
    <div className="max-w-3xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-2">Preferencias de Comunicación</h1>
      <p className="text-gray-600 mb-6">
        Gestiona cómo y cuándo quieres recibir comunicaciones de OKLA.
        Según Ley 172-13 de Protección de Datos Personales.
      </p>
      <ConsentPreferencesForm />
    </div>
  );
}
```

#### CONSENT-UNSUB-001: Darse de Baja (Link en Emails)

**Ruta:** `/unsubscribe?token=xxx&type=marketing`

```typescript
// filepath: src/app/unsubscribe/page.tsx
import { UnsubscribeConfirmation } from '@/components/consent/UnsubscribeConfirmation';

export default function UnsubscribePage() {
  return <UnsubscribeConfirmation />;
}
```

#### CONSENT-AUDIT-001: Historial de Consentimientos

**Ruta:** `/settings/privacy/consent-history`

```typescript
// filepath: src/app/settings/privacy/consent-history/page.tsx
import { ConsentHistoryTimeline } from '@/components/consent/ConsentHistoryTimeline';

export default function ConsentHistoryPage() {
  return (
    <div className="max-w-4xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-2">Historial de Consentimientos</h1>
      <p className="text-gray-600 mb-6">
        Registro completo de tus consentimientos según Ley 172-13.
      </p>
      <ConsentHistoryTimeline />
    </div>
  );
}
```

### Endpoints API de Consentimiento

```typescript
// ConsentController (NotificationService)
GET    /api/consent/preferences       # Preferencias actuales
PUT    /api/consent/preferences       # Actualizar preferencias
GET    /api/consent/history           # Historial de cambios
POST   /api/consent/grant             # Otorgar consentimiento
POST   /api/consent/revoke            # Revocar consentimiento

// UnsubscribeController (Público - No auth)
GET    /api/unsubscribe/validate      # Validar token
POST   /api/unsubscribe/confirm       # Confirmar baja
GET    /api/unsubscribe/options       # Opciones de baja
```

### Tipos de Consentimiento

```typescript
enum ConsentType {
  TERMS_OF_SERVICE = "terms", // Obligatorio
  PRIVACY_POLICY = "privacy", // Obligatorio
  MARKETING_OKLA = "marketing_okla", // Opt-in
  MARKETING_PARTNERS = "partners", // Opt-in
  VEHICLE_ALERTS = "vehicle_alerts", // Opt-in
  PRICE_DROP_ALERTS = "price_alerts", // Opt-in
  NEWSLETTER = "newsletter", // Opt-in
  SURVEYS = "surveys", // Opt-in
}

interface ConsentRecord {
  id: string;
  userId: string;
  type: ConsentType;
  channel: "email" | "sms" | "push" | "whatsapp";
  granted: boolean;
  timestamp: string;
  ipAddress: string;
  userAgent: string;
  source: "registration" | "settings" | "unsubscribe";
  version: string; // Versión del texto legal
  revokedAt?: string;
  revokedReason?: string;
}
```

---

## 🎨 COMPONENTES

### PASO 1: NotificationBell - Badge en Navbar

```typescript
// filepath: src/components/notifications/NotificationBell.tsx
"use client";

import { useState, useEffect } from "react";
import { Bell } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/Popover";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { NotificationList } from "./NotificationList";
import { useNotifications } from "@/lib/hooks/useNotifications";
import { useNotificationWebSocket } from "@/lib/hooks/useNotificationWebSocket";

export function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const { data: notifications } = useNotifications({ limit: 5 });
  const unreadCount = notifications?.unreadCount || 0;

  // WebSocket para real-time updates
  useNotificationWebSocket();

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="icon" className="relative">
          <Bell size={20} />
          {unreadCount > 0 && (
            <Badge
              variant="primary"
              className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 text-xs"
            >
              {unreadCount > 9 ? "9+" : unreadCount}
            </Badge>
          )}
        </Button>
      </PopoverTrigger>

      <PopoverContent align="end" className="w-96 p-0">
        <div className="p-4 border-b">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold text-gray-900">Notificaciones</h3>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                // Mark all as read
              }}
            >
              Marcar todas leídas
            </Button>
          </div>
        </div>

        <NotificationList limit={5} />

        <div className="p-3 border-t">
          <Button
            variant="ghost"
            fullWidth
            onClick={() => {
              setIsOpen(false);
              window.location.href = "/notificaciones";
            }}
          >
            Ver todas
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}
```

---

### PASO 2: NotificationItem - Item Individual

```typescript
// filepath: src/components/notifications/NotificationItem.tsx
"use client";

import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import {
  MessageCircle,
  Heart,
  Bell,
  Star,
  Shield,
  X
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { useMarkAsRead, useDeleteNotification } from "@/lib/hooks/useNotifications";
import type { Notification } from "@/types/notification";

const iconMap = {
  new_message: MessageCircle,
  message_reply: MessageCircle,
  favorite_price_drop: Heart,
  favorite_sold: Heart,
  search_new_match: Bell,
  new_review: Star,
  review_reply: Star,
  account_verified: Shield,
  subscription_expiring: Bell,
  maintenance_scheduled: Bell,
};

interface NotificationItemProps {
  notification: Notification;
  onClick?: () => void;
}

export function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const { mutate: markAsRead } = useMarkAsRead();
  const { mutate: deleteNotif } = useDeleteNotification();

  const Icon = iconMap[notification.type] || Bell;

  const handleClick = () => {
    if (!notification.isRead) {
      markAsRead(notification.id);
    }
    if (onClick) {
      onClick();
    } else {
      // Navigate to entity
      const path = getEntityPath(notification);
      window.location.href = path;
    }
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    deleteNotif(notification.id);
  };

  return (
    <div
      onClick={handleClick}
      className={`p-4 hover:bg-gray-50 cursor-pointer border-b transition ${
        !notification.isRead ? "bg-blue-50" : ""
      }`}
    >
      <div className="flex gap-3">
        {/* Icon */}
        <div
          className={`flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center ${
            !notification.isRead ? "bg-primary-100" : "bg-gray-100"
          }`}
        >
          <Icon
            size={20}
            className={!notification.isRead ? "text-primary-600" : "text-gray-500"}
          />
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <p className="font-medium text-gray-900">{notification.title}</p>
          <p className="text-sm text-gray-600 mt-1">{notification.message}</p>
          <time className="text-xs text-gray-500 mt-2 block">
            {formatDistanceToNow(new Date(notification.createdAt), {
              addSuffix: true,
              locale: es,
            })}
          </time>
        </div>

        {/* Delete button */}
        <Button
          variant="ghost"
          size="icon"
          onClick={handleDelete}
          className="flex-shrink-0"
        >
          <X size={16} />
        </Button>
      </div>

      {/* Image (optional) */}
      {notification.imageUrl && (
        <img
          src={notification.imageUrl}
          alt=""
          className="mt-3 ml-13 w-32 h-20 object-cover rounded-lg"
        />
      )}
    </div>
  );
}

function getEntityPath(notification: Notification): string {
  switch (notification.entityType) {
    case "vehicle":
      return `/vehicles/${notification.entityId}`;
    case "conversation":
      return `/messages/${notification.entityId}`;
    case "review":
      return `/reviews/${notification.entityId}`;
    default:
      return "/notificaciones";
  }
}
```

---

### PASO 3: NotificationList - Lista Completa

```typescript
// filepath: src/components/notifications/NotificationList.tsx
"use client";

import { Bell } from "lucide-react";
import { NotificationItem } from "./NotificationItem";
import { useNotifications } from "@/lib/hooks/useNotifications";

interface NotificationListProps {
  limit?: number;
}

export function NotificationList({ limit }: NotificationListProps) {
  const { data, isLoading } = useNotifications({ limit });

  if (isLoading) {
    return (
      <div className="p-8 text-center text-gray-500">Cargando...</div>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <div className="p-8 text-center text-gray-500">
        <Bell size={48} className="mx-auto mb-4 opacity-50" />
        <p>No tienes notificaciones</p>
      </div>
    );
  }

  return (
    <div className="divide-y">
      {data.items.map((notification) => (
        <NotificationItem key={notification.id} notification={notification} />
      ))}
    </div>
  );
}
```

---

### PASO 4: NotificationPreferences - Configuración

```typescript
// filepath: src/components/notifications/NotificationPreferences.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Switch } from "@/components/ui/Switch";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { FormField } from "@/components/ui/FormField";
import {
  usePreferences,
  useUpdatePreferences
} from "@/lib/hooks/useNotifications";

const preferencesSchema = z.object({
  emailEnabled: z.boolean(),
  pushEnabled: z.boolean(),
  smsEnabled: z.boolean(),
  quietHoursEnabled: z.boolean(),
  quietHoursStart: z.string().optional(),
  quietHoursEnd: z.string().optional(),
});

type PreferencesFormData = z.infer<typeof preferencesSchema>;

export function NotificationPreferences() {
  const { data: prefs, isLoading } = usePreferences();
  const { mutate: updatePrefs, isPending } = useUpdatePreferences();

  const { register, handleSubmit, watch } = useForm<PreferencesFormData>({
    resolver: zodResolver(preferencesSchema),
    defaultValues: prefs
      ? {
          emailEnabled: prefs.channels.email.enabled,
          pushEnabled: prefs.channels.push.enabled,
          smsEnabled: prefs.channels.sms.enabled,
          quietHoursEnabled: prefs.quietHours.enabled,
          quietHoursStart: prefs.quietHours.start,
          quietHoursEnd: prefs.quietHours.end,
        }
      : undefined,
  });

  const quietHoursEnabled = watch("quietHoursEnabled");

  const onSubmit = (data: PreferencesFormData) => {
    updatePrefs({
      channels: {
        email: { enabled: data.emailEnabled },
        push: { enabled: data.pushEnabled },
        sms: { enabled: data.smsEnabled },
      },
      quietHours: {
        enabled: data.quietHoursEnabled,
        start: data.quietHoursStart || "22:00",
        end: data.quietHoursEnd || "08:00",
      },
    });
  };

  if (isLoading) {
    return <div>Cargando preferencias...</div>;
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Channels */}
      <div className="bg-white rounded-lg border p-6">
        <h3 className="font-semibold text-gray-900 mb-4">Canales de notificación</h3>

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-gray-900">Email</p>
              <p className="text-sm text-gray-600">
                Recibe notificaciones por correo electrónico
              </p>
            </div>
            <Switch {...register("emailEnabled")} />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-gray-900">Push</p>
              <p className="text-sm text-gray-600">
                Notificaciones en tu navegador
              </p>
            </div>
            <Switch {...register("pushEnabled")} />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-gray-900">SMS</p>
              <p className="text-sm text-gray-600">
                Mensajes de texto (requiere plan Premium)
              </p>
            </div>
            <Switch {...register("smsEnabled")} />
          </div>
        </div>
      </div>

      {/* Quiet Hours */}
      <div className="bg-white rounded-lg border p-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="font-semibold text-gray-900">Horas silenciosas</h3>
            <p className="text-sm text-gray-600 mt-1">
              No enviar notificaciones durante estas horas
            </p>
          </div>
          <Switch {...register("quietHoursEnabled")} />
        </div>

        {quietHoursEnabled && (
          <div className="grid grid-cols-2 gap-4 mt-4">
            <FormField label="Desde">
              <Input type="time" {...register("quietHoursStart")} />
            </FormField>
            <FormField label="Hasta">
              <Input type="time" {...register("quietHoursEnd")} />
            </FormField>
          </div>
        )}
      </div>

      {/* Submit */}
      <Button type="submit" disabled={isPending}>
        Guardar preferencias
      </Button>
    </form>
  );
}
```

---

## 🔌 WEBSOCKET INTEGRATION

### PASO 5: WebSocket Hook

```typescript
// filepath: src/lib/hooks/useNotificationWebSocket.ts
import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useAuthStore } from "@/lib/stores/useAuthStore";

export function useNotificationWebSocket() {
  const queryClient = useQueryClient();
  const { user } = useAuthStore();

  useEffect(() => {
    if (!user) return;

    const ws = new WebSocket(
      `${process.env.NEXT_PUBLIC_WS_URL}/ws/notifications?token=${user.token}`,
    );

    ws.onopen = () => {
      console.log("✅ WebSocket connected (notifications)");
    };

    ws.onmessage = (event) => {
      const notification = JSON.parse(event.data);

      // Update cache
      queryClient.invalidateQueries({ queryKey: ["notifications"] });

      // Show toast
      toast(notification.title, {
        description: notification.message,
        action: {
          label: "Ver",
          onClick: () => {
            window.location.href = getEntityPath(notification);
          },
        },
      });
    };

    ws.onerror = (error) => {
      console.error("❌ WebSocket error:", error);
    };

    ws.onclose = () => {
      console.log("🔌 WebSocket closed (notifications)");
    };

    return () => {
      ws.close();
    };
  }, [user, queryClient]);
}

function getEntityPath(notification: any): string {
  switch (notification.entityType) {
    case "vehicle":
      return `/vehicles/${notification.entityId}`;
    case "conversation":
      return `/messages/${notification.entityId}`;
    case "review":
      return `/reviews/${notification.entityId}`;
    default:
      return "/notificaciones";
  }
}
```

---

## 📄 PÁGINAS

### PASO 6: Página de Notificaciones

```typescript
// filepath: src/app/(main)/notificaciones/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { Settings } from "lucide-react";
import { Link } from "next/link";
import { auth } from "@/lib/auth";
import { NotificationList } from "@/components/notifications/NotificationList";
import { NotificationPreferences } from "@/components/notifications/NotificationPreferences";

export const metadata: Metadata = {
  title: "Notificaciones | OKLA",
  description: "Centro de notificaciones",
};

export default async function NotificationsPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/notificaciones");
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Notificaciones</h1>
          <p className="text-gray-600 mt-1">
            Mantente informado de todo lo importante
          </p>
        </div>
        <Link
          href="/notificaciones/configuracion"
          className="text-primary-600 hover:text-primary-700"
        >
          <Settings size={20} />
        </Link>
      </div>

      <Tabs defaultValue="all">
        <TabsList>
          <TabsTrigger value="all">Todas</TabsTrigger>
          <TabsTrigger value="messages">Mensajes</TabsTrigger>
          <TabsTrigger value="alerts">Alertas</TabsTrigger>
          <TabsTrigger value="system">Sistema</TabsTrigger>
        </TabsList>

        <TabsContent value="all">
          <div className="bg-white rounded-lg border">
            <NotificationList />
          </div>
        </TabsContent>

        <TabsContent value="messages">
          <div className="bg-white rounded-lg border">
            <NotificationList filter="messages" />
          </div>
        </TabsContent>

        <TabsContent value="alerts">
          <div className="bg-white rounded-lg border">
            <NotificationList filter="alerts" />
          </div>
        </TabsContent>

        <TabsContent value="system">
          <div className="bg-white rounded-lg border">
            <NotificationList filter="system" />
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 7: Hooks de Notificaciones

```typescript
// filepath: src/lib/hooks/useNotifications.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notificationService } from "@/lib/services/notificationService";
import { toast } from "sonner";

export function useNotifications(params?: { limit?: number; filter?: string }) {
  return useQuery({
    queryKey: ["notifications", params],
    queryFn: () => notificationService.getNotifications(params),
  });
}

export function useMarkAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationService.markAsRead(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });
}

export function useMarkAllAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      toast.success("Todas marcadas como leídas");
    },
  });
}

export function useDeleteNotification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationService.deleteNotification(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });
}

export function usePreferences() {
  return useQuery({
    queryKey: ["notificationPreferences"],
    queryFn: () => notificationService.getPreferences(),
  });
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) => notificationService.updatePreferences(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notificationPreferences"] });
      toast.success("Preferencias actualizadas");
    },
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 8: Tipos de Notification

```typescript
// filepath: src/types/notification.ts
export type NotificationType =
  | "new_message"
  | "message_reply"
  | "favorite_price_drop"
  | "favorite_sold"
  | "search_new_match"
  | "new_review"
  | "review_reply"
  | "account_verified"
  | "subscription_expiring"
  | "maintenance_scheduled";

export type EntityType = "vehicle" | "conversation" | "review" | "alert";

export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  entityType: EntityType;
  entityId: string;
  imageUrl?: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationPreferences {
  channels: {
    email: {
      enabled: boolean;
      types?: NotificationType[];
    };
    push: {
      enabled: boolean;
      types?: NotificationType[];
    };
    sms: {
      enabled: boolean;
      types?: NotificationType[];
    };
    inApp: {
      enabled: boolean;
      types?: NotificationType[];
    };
  };
  quietHours: {
    enabled: boolean;
    start: string; // "22:00"
    end: string; // "08:00"
  };
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - Badge en navbar muestra contador de no leídas
# - Click en badge abre dropdown con últimas 5 notificaciones
# - Click en notificación redirige a entidad correcta
# - Marcar como leída funciona (badge count -1)
# - Eliminar notificación funciona
# - WebSocket recibe notificaciones en tiempo real
# - Toast aparece con nuevas notificaciones
# - Página /notificaciones lista todas las notificaciones
# - Filtros por tipo funcionan (mensajes, alertas, sistema)
# - Preferencias se guardan correctamente
# - Quiet hours respetado (no enviar entre 22:00-08:00)
# - Iconos correctos por tipo de notificación
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/notificaciones.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Notificaciones", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar lista de notificaciones", async ({ page }) => {
    await page.goto("/notificaciones");

    await expect(
      page.getByRole("heading", { name: /notificaciones/i }),
    ).toBeVisible();
    await expect(page.getByTestId("notifications-list")).toBeVisible();
  });

  test("debe marcar notificación como leída", async ({ page }) => {
    await page.goto("/notificaciones");

    await page.getByTestId("notification-unread").first().click();
    await expect(page.getByTestId("notification-unread")).toHaveCount({
      max: await page.getByTestId("notification-unread").count(),
    });
  });

  test("debe marcar todas como leídas", async ({ page }) => {
    await page.goto("/notificaciones");

    await page.getByRole("button", { name: /marcar todas/i }).click();
    await expect(page.getByTestId("notification-unread")).toHaveCount(0);
  });

  test("debe filtrar por tipo", async ({ page }) => {
    await page.goto("/notificaciones");

    await page.getByRole("combobox", { name: /tipo/i }).click();
    await page.getByRole("option", { name: /mensajes/i }).click();

    await expect(page).toHaveURL(/type=message/);
  });

  test("debe navegar a destino de notificación", async ({ page }) => {
    await page.goto("/notificaciones");

    await page.getByTestId("notification-item").first().click();
    // Debe redirigir al contenido relacionado
    await expect(page.url()).not.toContain("/notificaciones");
  });

  test("debe mostrar badge en navbar", async ({ page }) => {
    await page.goto("/");

    const notifBadge = page.getByTestId("notification-badge");
    if (await notifBadge.isVisible()) {
      await expect(notifBadge).toContainText(/\d+/);
    }
  });
});
```

---

## 🚀 MEJORAS FUTURAS

1. **Priority Inbox**: Notificaciones más importantes primero
2. **Digest Mode**: Resumen semanal de actividad
3. **Smart Grouping**: Agrupar notificaciones similares ("3 nuevos mensajes")
4. **Notification Archive**: Historial de notificaciones antiguas
5. **Custom Sounds**: Sonidos personalizados por tipo

---

**Siguiente documento:** `26-privacy-gdpr.md` - Privacidad y cumplimiento GDPR
