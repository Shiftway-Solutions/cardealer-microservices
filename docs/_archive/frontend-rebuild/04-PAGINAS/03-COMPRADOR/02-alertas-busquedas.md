---
title: "24. Alertas y Búsquedas Guardadas"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "BillingService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 24. Alertas y Búsquedas Guardadas

**Objetivo:** Sistema de búsquedas guardadas con alertas automáticas por email/SMS/push cuando aparezcan vehículos que cumplan criterios, detección de cambios de precio y notificaciones inteligentes.

**Prioridad:** P2 (Baja - Mejora de engagement y retención)  
**Complejidad:** 🟡 Media (Notifications, Cron jobs, Matching engine)  
**Dependencias:** AlertService (✅ YA IMPLEMENTADO en Sprint 1), NotificationService, VehiclesService

---

## ✅ INTEGRACIÓN CON SERVICIOS DE ENGAGEMENT

Este documento complementa:

- [process-matrix/17-ENGAGEMENT-RETENCION/01-alertas-busquedas-guardadas.md](../../process-matrix/17-ENGAGEMENT-RETENCION/01-alertas-busquedas-guardadas.md) - **Alertas** ⭐
- [process-matrix/17-ENGAGEMENT-RETENCION/04-wishlist-compartida.md](../../process-matrix/17-ENGAGEMENT-RETENCION/04-wishlist-compartida.md) - **Wishlist** ⭐

**Estado:** ✅ AlertService 100% BE + 90% UI | 🔴 SharedWishlist 0%

### Servicios de Engagement

| Servicio            | Puerto | Función                         | Estado              |
| ------------------- | ------ | ------------------------------- | ------------------- |
| AlertService        | 5067   | Alertas y búsquedas guardadas   | ✅ 100% BE + 90% UI |
| VehiclesSaleService | 5005   | Wishlist compartida (extendido) | 🔴 0% (Fase 2)      |
| NotificationService | 5006   | Email/SMS/Push notifications    | ✅ 100%             |

### AlertService - Endpoints Principales

| Método   | Endpoint                          | Descripción                    | Auth |
| -------- | --------------------------------- | ------------------------------ | ---- |
| `GET`    | `/api/alerts/recently-viewed`     | Historial de vehículos vistos  | ✅   |
| `POST`   | `/api/alerts/saved-searches`      | Guardar búsqueda con criterios | ✅   |
| `GET`    | `/api/alerts/saved-searches`      | Mis búsquedas guardadas        | ✅   |
| `DELETE` | `/api/alerts/saved-searches/{id}` | Eliminar búsqueda              | ✅   |
| `POST`   | `/api/alerts/price-alerts`        | Crear alerta de precio         | ✅   |
| `GET`    | `/api/alerts/price-alerts`        | Mis alertas de precio          | ✅   |
| `PUT`    | `/api/alerts/price-alerts/{id}`   | Actualizar alerta              | ✅   |
| `DELETE` | `/api/alerts/price-alerts/{id}`   | Eliminar alerta                | ✅   |

### Procesos de AlertService

| Proceso             | Código          | Pasos | Descripción                                   |
| ------------------- | --------------- | ----- | --------------------------------------------- |
| Alertas de Precio   | ALERT-PRICE-001 | 4     | Crear alerta cuando precio baje               |
| Búsquedas Guardadas | ALERT-SAVED-001 | 4     | Guardar criterios y notificar nuevos listings |
| Historial Vistos    | ALERT-HIST-001  | 3     | Trackear vehículos visitados                  |
| Notificaciones      | ALERT-NOTIF-001 | 3     | Enviar notificaciones multicanal              |

### Entidades Clave

```csharp
// AlertService/AlertService.Domain/Entities/
public class SavedSearch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }                    // "SUVs en Santo Domingo"
    public SearchCriteria Criteria { get; set; }       // Marcas, precio, año, etc.
    public bool NotifyOnNewListings { get; set; }
    public NotificationFrequency Frequency { get; set; } // Instant, Daily, Weekly
    public DateTime LastExecutedAt { get; set; }
}

public class PriceAlert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public AlertType Type { get; set; }                // PriceDrop, TargetPrice, PercentageDrop
    public decimal? TargetPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public bool HasTriggered { get; set; }
}

public class RecentlyViewed
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public int ViewCount { get; set; }
    public DateTime LastViewedAt { get; set; }
    public decimal PriceAtLastView { get; set; }
}
```

### 💝 Wishlist Compartida (Planificado Fase 2)

**Estado:** 🔴 0% - Feature social de baja prioridad

**Funcionalidades planificadas:**

- Crear wishlist colaborativa (parejas, familias)
- Invitar colaboradores con permisos (viewer, contributor, admin)
- Votar por vehículos (👍👎)
- Comentar en cada vehículo
- Share link público: `okla.do/w/abc123`
- Privacy: Private, Collaborators, LinkOnly, Public

**Endpoints planificados:**

```typescript
POST / api / wishlists; // Crear wishlist
GET / api / wishlists / shared / { token }; // Ver wishlist pública
POST / api / wishlists / { id } / vehicles; // Agregar vehículo
POST / api / wishlists / { id } / vehicles / { id } / vote; // Votar
POST / api / wishlists / { id } / collaborators; // Invitar
```

**Nota:** La funcionalidad de favoritos individuales ya está implementada en `/favorites`. Wishlist compartida es una evolución social para grupos.

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes](#componentes)
4. [Páginas](#páginas)
5. [Hooks y Servicios](#hooks-y-servicios)
6. [Tipos TypeScript](#tipos-typescript)
7. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### Sistema de Alertas

```
Usuario crea búsqueda con criterios
    ↓
AlertService guarda SavedSearch
    ↓
Cron Job (cada hora)
├─ Query DB con criterios de cada SavedSearch activa
├─ Compara con últimos resultados (cache)
└─ Si hay nuevos vehículos:
    ├─ Crear notificación
    ├─ Enviar email (inmediato si HOT, digest si no)
    ├─ Enviar push notification
    └─ Enviar SMS (solo si usuario configuró)
    ↓
Usuario recibe notificación
├─ Click → Ver vehículos nuevos
└─ Snooze/Disable alert
    ↓
Track analytics
├─ Open rate
├─ Click rate
└─ Conversion rate
```

### Matching Engine

```typescript
interface MatchingEngine {
  // Cada hora ejecuta:
  async checkAllAlerts() {
    const activeAlerts = await getActiveAlerts();

    for (const alert of activeAlerts) {
      const newVehicles = await findNewMatches(alert);

      if (newVehicles.length > 0) {
        await sendNotification({
          alertId: alert.id,
          vehicles: newVehicles,
          channels: alert.notificationChannels,
        });
      }
    }
  }
}
```

---

## 🔌 BACKEND API

### AlertService Endpoints (Ya Implementados ✅ en Sprint 1)

```typescript
// filepath: docs/backend/AlertService-API.md (Sprint 1)

GET    /api/alerts/saved-searches         # Búsquedas guardadas
GET    /api/alerts/saved-searches/{id}    # Búsqueda específica
POST   /api/alerts/saved-searches         # Crear búsqueda guardada
PUT    /api/alerts/saved-searches/{id}    # Actualizar búsqueda
DELETE /api/alerts/saved-searches/{id}    # Eliminar búsqueda

GET    /api/alerts/price-alerts           # Alertas de precio
POST   /api/alerts/price-alerts           # Crear alerta de precio
PUT    /api/alerts/price-alerts/{id}      # Actualizar alerta
DELETE /api/alerts/price-alerts/{id}      # Eliminar alerta

GET    /api/alerts/notifications          # Notificaciones de alertas
POST   /api/alerts/notifications/{id}/read # Marcar como leída
DELETE /api/alerts/notifications/{id}     # Eliminar notificación

GET    /api/alerts/free-days-left         # Días gratis restantes (premium feature)
```

---

## 🎨 COMPONENTES

### PASO 1: SavedSearchCard - Tarjeta de Búsqueda Guardada

```typescript
// filepath: src/components/alerts/SavedSearchCard.tsx
"use client";

import { useState } from "react";
import { Bell, BellOff, Edit2, Trash2, Clock } from "lucide-react";
import { format } from "date-fns";
import { es } from "date-fns/locale";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Switch } from "@/components/ui/Switch";
import type { SavedSearch } from "@/types/alert";

interface SavedSearchCardProps {
  search: SavedSearch;
  onToggle: (id: string, isActive: boolean) => void;
  onEdit: (search: SavedSearch) => void;
  onDelete: (id: string) => void;
}

export function SavedSearchCard({
  search,
  onToggle,
  onEdit,
  onDelete,
}: SavedSearchCardProps) {
  const [isActive, setIsActive] = useState(search.isActive);

  const handleToggle = () => {
    const newState = !isActive;
    setIsActive(newState);
    onToggle(search.id, newState);
  };

  const getCriteriaDisplay = () => {
    const criteria = search.criteria;
    const parts: string[] = [];

    if (criteria.make) parts.push(criteria.make);
    if (criteria.model) parts.push(criteria.model);
    if (criteria.minPrice || criteria.maxPrice) {
      const priceRange = `$${criteria.minPrice?.toLocaleString() || "0"} - $${
        criteria.maxPrice?.toLocaleString() || "∞"
      }`;
      parts.push(priceRange);
    }
    if (criteria.minYear || criteria.maxYear) {
      parts.push(`${criteria.minYear || "Cualquier"} - ${criteria.maxYear || "Actual"}`);
    }
    if (criteria.bodyType) parts.push(criteria.bodyType);
    if (criteria.transmission) parts.push(criteria.transmission);

    return parts.join(" • ");
  };

  return (
    <div className="bg-white rounded-lg border p-6 hover:shadow-md transition">
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-2">
            <h3 className="font-semibold text-lg text-gray-900">
              {search.name}
            </h3>
            {search.newMatchesCount > 0 && (
              <Badge variant="primary">
                {search.newMatchesCount} nuevo{search.newMatchesCount > 1 ? "s" : ""}
              </Badge>
            )}
          </div>
          <p className="text-sm text-gray-600">{getCriteriaDisplay()}</p>
        </div>

        {/* Active Toggle */}
        <div className="flex items-center gap-2">
          <Switch checked={isActive} onChange={handleToggle} />
          {isActive ? (
            <Bell size={18} className="text-primary-600" />
          ) : (
            <BellOff size={18} className="text-gray-400" />
          )}
        </div>
      </div>

      {/* Notification Channels */}
      <div className="flex items-center gap-2 mb-4">
        <span className="text-xs text-gray-500">Notificar por:</span>
        {search.notificationChannels.email && (
          <Badge variant="secondary" size="sm">
            Email
          </Badge>
        )}
        {search.notificationChannels.push && (
          <Badge variant="secondary" size="sm">
            Push
          </Badge>
        )}
        {search.notificationChannels.sms && (
          <Badge variant="secondary" size="sm">
            SMS
          </Badge>
        )}
      </div>

      {/* Frequency */}
      <div className="flex items-center gap-2 text-xs text-gray-500 mb-4">
        <Clock size={14} />
        <span>
          Frecuencia: {search.frequency === "instant" ? "Inmediata" : "Diaria"}
        </span>
        <span>•</span>
        <span>
          Última actualización:{" "}
          {format(new Date(search.lastCheckedAt), "dd MMM, HH:mm", {
            locale: es,
          })}
        </span>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2 pt-4 border-t">
        <Button
          variant="outline"
          size="sm"
          onClick={() => onEdit(search)}
        >
          <Edit2 size={14} className="mr-1" />
          Editar
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => onDelete(search.id)}
          className="text-red-600 hover:bg-red-50"
        >
          <Trash2 size={14} className="mr-1" />
          Eliminar
        </Button>
        {search.newMatchesCount > 0 && (
          <Button size="sm" className="ml-auto">
            Ver {search.newMatchesCount} vehículo{search.newMatchesCount > 1 ? "s" : ""}
          </Button>
        )}
      </div>
    </div>
  );
}
```

---

### PASO 2: CreateAlertModal - Crear/Editar Alerta

```typescript
// filepath: src/components/alerts/CreateAlertModal.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/Dialog";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { Button } from "@/components/ui/Button";
import { Switch } from "@/components/ui/Switch";
import { useCreateSavedSearch, useUpdateSavedSearch } from "@/lib/hooks/useAlerts";
import type { SavedSearch } from "@/types/alert";

const alertSchema = z.object({
  name: z.string().min(3, "Mínimo 3 caracteres"),
  make: z.string().optional(),
  model: z.string().optional(),
  minPrice: z.number().optional(),
  maxPrice: z.number().optional(),
  minYear: z.number().optional(),
  maxYear: z.number().optional(),
  bodyType: z.string().optional(),
  transmission: z.string().optional(),
  fuelType: z.string().optional(),
  frequency: z.enum(["instant", "daily"]),
  notifyEmail: z.boolean(),
  notifyPush: z.boolean(),
  notifySMS: z.boolean(),
});

type AlertFormData = z.infer<typeof alertSchema>;

interface CreateAlertModalProps {
  isOpen: boolean;
  onClose: () => void;
  editingSearch?: SavedSearch;
}

export function CreateAlertModal({
  isOpen,
  onClose,
  editingSearch,
}: CreateAlertModalProps) {
  const { mutate: createSearch, isPending: isCreating } = useCreateSavedSearch();
  const { mutate: updateSearch, isPending: isUpdating } = useUpdateSavedSearch();

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<AlertFormData>({
    resolver: zodResolver(alertSchema),
    defaultValues: editingSearch
      ? {
          name: editingSearch.name,
          ...editingSearch.criteria,
          frequency: editingSearch.frequency,
          notifyEmail: editingSearch.notificationChannels.email,
          notifyPush: editingSearch.notificationChannels.push,
          notifySMS: editingSearch.notificationChannels.sms,
        }
      : {
          frequency: "instant",
          notifyEmail: true,
          notifyPush: true,
          notifySMS: false,
        },
  });

  const onSubmit = (data: AlertFormData) => {
    const searchData = {
      name: data.name,
      criteria: {
        make: data.make,
        model: data.model,
        minPrice: data.minPrice,
        maxPrice: data.maxPrice,
        minYear: data.minYear,
        maxYear: data.maxYear,
        bodyType: data.bodyType,
        transmission: data.transmission,
        fuelType: data.fuelType,
      },
      frequency: data.frequency,
      notificationChannels: {
        email: data.notifyEmail,
        push: data.notifyPush,
        sms: data.notifySMS,
      },
    };

    if (editingSearch) {
      updateSearch({ id: editingSearch.id, data: searchData }, { onSuccess: onClose });
    } else {
      createSearch(searchData, { onSuccess: onClose });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {editingSearch ? "Editar" : "Crear"} búsqueda guardada
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          {/* Name */}
          <FormField label="Nombre de la alerta" error={errors.name?.message}>
            <Input {...register("name")} placeholder="Ej: SUVs Toyota bajo $30k" />
          </FormField>

          {/* Vehicle Criteria */}
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Marca">
              <Select {...register("make")}>
                <option value="">Todas</option>
                <option value="Toyota">Toyota</option>
                <option value="Honda">Honda</option>
                <option value="Nissan">Nissan</option>
                {/* ... más opciones */}
              </Select>
            </FormField>

            <FormField label="Modelo">
              <Input {...register("model")} placeholder="Ej: RAV4" />
            </FormField>

            <FormField label="Precio mínimo">
              <Input
                {...register("minPrice", { valueAsNumber: true })}
                type="number"
                placeholder="10000"
              />
            </FormField>

            <FormField label="Precio máximo">
              <Input
                {...register("maxPrice", { valueAsNumber: true })}
                type="number"
                placeholder="50000"
              />
            </FormField>

            <FormField label="Año mínimo">
              <Input
                {...register("minYear", { valueAsNumber: true })}
                type="number"
                placeholder="2015"
              />
            </FormField>

            <FormField label="Año máximo">
              <Input
                {...register("maxYear", { valueAsNumber: true })}
                type="number"
                placeholder="2024"
              />
            </FormField>

            <FormField label="Tipo de carrocería">
              <Select {...register("bodyType")}>
                <option value="">Todos</option>
                <option value="sedan">Sedán</option>
                <option value="suv">SUV</option>
                <option value="truck">Camioneta</option>
                <option value="coupe">Coupé</option>
              </Select>
            </FormField>

            <FormField label="Transmisión">
              <Select {...register("transmission")}>
                <option value="">Todas</option>
                <option value="automatic">Automática</option>
                <option value="manual">Manual</option>
              </Select>
            </FormField>
          </div>

          {/* Notification Settings */}
          <div className="bg-gray-50 rounded-lg p-4 space-y-4">
            <h3 className="font-semibold text-gray-900">Configuración de notificaciones</h3>

            {/* Frequency */}
            <FormField label="Frecuencia">
              <Select {...register("frequency")}>
                <option value="instant">Inmediata (en cuanto aparezca)</option>
                <option value="daily">Diaria (resumen al final del día)</option>
              </Select>
            </FormField>

            {/* Channels */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <label className="text-sm text-gray-700">Email</label>
                <Switch {...register("notifyEmail")} />
              </div>
              <div className="flex items-center justify-between">
                <label className="text-sm text-gray-700">Notificación push</label>
                <Switch {...register("notifyPush")} />
              </div>
              <div className="flex items-center justify-between">
                <label className="text-sm text-gray-700">
                  SMS (requiere plan Premium)
                </label>
                <Switch {...register("notifySMS")} />
              </div>
            </div>
          </div>

          {/* Submit */}
          <div className="flex items-center gap-3">
            <Button type="submit" disabled={isCreating || isUpdating}>
              {editingSearch ? "Actualizar" : "Crear"} alerta
            </Button>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancelar
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

---

### PASO 3: PriceAlertCard - Tarjeta de Alerta de Precio

```typescript
// filepath: src/components/alerts/PriceAlertCard.tsx
"use client";

import { TrendingDown, DollarSign, Bell } from "lucide-react";
import { format } from "date-fns";
import { es } from "date-fns/locale";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import type { PriceAlert } from "@/types/alert";

interface PriceAlertCardProps {
  alert: PriceAlert;
  onDelete: (id: string) => void;
}

export function PriceAlertCard({ alert, onDelete }: PriceAlertCardProps) {
  const currentPrice = alert.vehicle.price;
  const targetPrice = alert.targetPrice;
  const difference = currentPrice - targetPrice;
  const percentageOff = ((difference / currentPrice) * 100).toFixed(1);

  const isTriggered = currentPrice <= targetPrice;

  return (
    <div
      className={`bg-white rounded-lg border p-4 ${
        isTriggered ? "border-green-500 border-2" : ""
      }`}
    >
      <div className="flex gap-4">
        {/* Vehicle Image */}
        <img
          src={alert.vehicle.images?.[0]}
          alt={alert.vehicle.name}
          className="w-32 h-24 object-cover rounded-lg"
        />

        {/* Info */}
        <div className="flex-1">
          <h3 className="font-semibold text-gray-900">{alert.vehicle.name}</h3>

          {/* Price Info */}
          <div className="flex items-center gap-4 mt-2">
            <div>
              <p className="text-xs text-gray-500">Precio actual</p>
              <p className="text-lg font-bold text-gray-900">
                ${currentPrice.toLocaleString()}
              </p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Precio objetivo</p>
              <p className="text-lg font-bold text-primary-600">
                ${targetPrice.toLocaleString()}
              </p>
            </div>
          </div>

          {/* Status */}
          <div className="mt-3">
            {isTriggered ? (
              <Badge variant="success">
                <Bell size={12} className="mr-1" />
                ¡Precio alcanzado!
              </Badge>
            ) : (
              <div className="text-sm text-gray-600">
                Falta ${difference.toLocaleString()} ({percentageOff}%)
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex flex-col justify-between">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onDelete(alert.id)}
          >
            Eliminar
          </Button>
          {isTriggered && (
            <Button size="sm">Ver vehículo</Button>
          )}
        </div>
      </div>

      {/* Created Date */}
      <div className="mt-3 pt-3 border-t text-xs text-gray-500">
        Creada:{" "}
        {format(new Date(alert.createdAt), "dd MMM yyyy", { locale: es })}
      </div>
    </div>
  );
}
```

---

### PASO 4: NotificationList - Lista de Notificaciones

```typescript
// filepath: src/components/alerts/NotificationList.tsx
"use client";

import { Bell, Check } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import { Button } from "@/components/ui/Button";
import { useNotifications, useMarkAsRead } from "@/lib/hooks/useAlerts";

export function NotificationList() {
  const { data: notifications, isLoading } = useNotifications();
  const { mutate: markAsRead } = useMarkAsRead();

  if (isLoading) {
    return <div>Cargando notificaciones...</div>;
  }

  if (!notifications || notifications.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        <Bell size={48} className="mx-auto mb-4 opacity-50" />
        <p>No tienes notificaciones</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {notifications.map((notification) => (
        <div
          key={notification.id}
          className={`bg-white rounded-lg border p-4 ${
            !notification.isRead ? "border-l-4 border-l-primary-600" : ""
          }`}
        >
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h4 className="font-semibold text-gray-900">
                {notification.title}
              </h4>
              <p className="text-sm text-gray-600 mt-1">
                {notification.message}
              </p>
              <time className="text-xs text-gray-500 mt-2 block">
                {formatDistanceToNow(new Date(notification.createdAt), {
                  addSuffix: true,
                  locale: es,
                })}
              </time>
            </div>

            {!notification.isRead && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => markAsRead(notification.id)}
              >
                <Check size={16} />
              </Button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 📄 PÁGINAS

### PASO 5: Página de Alertas

```typescript
// filepath: src/app/(main)/alertas/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { Button } from "@/components/ui/Button";
import { Plus } from "lucide-react";
import { auth } from "@/lib/auth";
import { SavedSearchesList } from "@/components/alerts/SavedSearchesList";
import { PriceAlertsList } from "@/components/alerts/PriceAlertsList";
import { NotificationList } from "@/components/alerts/NotificationList";

export const metadata: Metadata = {
  title: "Mis Alertas | OKLA",
  description: "Gestiona tus búsquedas guardadas y alertas de precio",
};

export default async function AlertsPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/alertas");
  }

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Mis Alertas</h1>
          <p className="text-gray-600 mt-1">
            Recibe notificaciones cuando aparezcan vehículos que te interesen
          </p>
        </div>
        <Button>
          <Plus size={16} className="mr-1" />
          Nueva alerta
        </Button>
      </div>

      <Tabs defaultValue="searches">
        <TabsList>
          <TabsTrigger value="searches">Búsquedas guardadas</TabsTrigger>
          <TabsTrigger value="prices">Alertas de precio</TabsTrigger>
          <TabsTrigger value="notifications">Notificaciones</TabsTrigger>
        </TabsList>

        <TabsContent value="searches">
          <SavedSearchesList />
        </TabsContent>

        <TabsContent value="prices">
          <PriceAlertsList />
        </TabsContent>

        <TabsContent value="notifications">
          <NotificationList />
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 6: useAlerts Hooks

```typescript
// filepath: src/lib/hooks/useAlerts.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { alertService } from "@/lib/services/alertService";
import { toast } from "sonner";

export function useSavedSearches() {
  return useQuery({
    queryKey: ["savedSearches"],
    queryFn: () => alertService.getSavedSearches(),
  });
}

export function useCreateSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) => alertService.createSavedSearch(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["savedSearches"] });
      toast.success("Alerta creada correctamente");
    },
  });
}

export function useUpdateSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) =>
      alertService.updateSavedSearch(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["savedSearches"] });
      toast.success("Alerta actualizada");
    },
  });
}

export function usePriceAlerts() {
  return useQuery({
    queryKey: ["priceAlerts"],
    queryFn: () => alertService.getPriceAlerts(),
  });
}

export function useNotifications() {
  return useQuery({
    queryKey: ["notifications"],
    queryFn: () => alertService.getNotifications(),
  });
}

export function useMarkAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId: string) =>
      alertService.markAsRead(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 7: Tipos de Alert

```typescript
// filepath: src/types/alert.ts
import type { Vehicle } from "./vehicle";

export interface SavedSearch {
  id: string;
  userId: string;
  name: string;
  criteria: SearchCriteria;
  frequency: "instant" | "daily";
  notificationChannels: {
    email: boolean;
    push: boolean;
    sms: boolean;
  };
  isActive: boolean;
  newMatchesCount: number;
  lastCheckedAt: string;
  createdAt: string;
  updatedAt: string;
}

export interface SearchCriteria {
  make?: string;
  model?: string;
  minPrice?: number;
  maxPrice?: number;
  minYear?: number;
  maxYear?: number;
  bodyType?: string;
  transmission?: string;
  fuelType?: string;
}

export interface PriceAlert {
  id: string;
  userId: string;
  vehicleId: string;
  vehicle: Vehicle;
  targetPrice: number;
  isActive: boolean;
  isTriggered: boolean;
  triggeredAt?: string;
  createdAt: string;
}

export interface AlertNotification {
  id: string;
  userId: string;
  type: "new_match" | "price_drop" | "price_target";
  title: string;
  message: string;
  vehicleIds: string[];
  isRead: boolean;
  createdAt: string;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - /alertas muestra 3 tabs (búsquedas, precios, notificaciones)
# - Crear búsqueda guardada funciona con todos los filtros
# - Toggle activo/inactivo funciona
# - Editar búsqueda guardada funciona
# - Eliminar búsqueda guardada funciona
# - Badge "X nuevos" aparece cuando hay matches
# - Alertas de precio muestran diferencia y progreso
# - Notificaciones se marcan como leídas
# - Frecuencia (instantánea/diaria) se guarda correctamente
# - Canales de notificación (email/push/SMS) funcionan
# - Premium badge aparece en SMS option
```

---

## 🚀 MEJORAS FUTURAS

1. **Smart Scheduling**: Enviar notificaciones en horario preferido del usuario
2. **Digest Emails**: Resumen diario elegante con todos los matches
3. **WhatsApp Integration**: Notificaciones por WhatsApp Business
4. **Voice Alerts**: "Alexa, ¿hay vehículos nuevos en mi búsqueda?"
5. **Geo-fencing**: Alertas cuando estés cerca de un dealer con match

---

**Siguiente documento:** `25-notificaciones.md` - Centro de notificaciones unificado

---

# ANEXO: Páginas de Alertas y Comparación

> Fusionado desde 11-alerts-comparison.md

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    USER ALERTS & COMPARISON PAGES                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                       AlertsPage                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  [Tabs: Alertas de Precio | Búsquedas Guardadas]            │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  │  ┌──────────────────────────────┐  ┌───────────────────────────┐   │   │
│  │  │ Price Alert Card             │  │ Saved Search Card         │   │   │
│  │  │ - Vehicle title              │  │ - Search name             │   │   │
│  │  │ - Current price              │  │ - Filters applied         │   │   │
│  │  │ - Target price               │  │ - Toggle active           │   │   │
│  │  │ - Toggle/Delete              │  │ - Delete button           │   │   │
│  │  └──────────────────────────────┘  └───────────────────────────┘   │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  "X días gratis restantes" badge (Early Bird)               │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      ComparisonPage                                  │   │
│  │  ┌────────────────────┐  ┌──────────────────────────────────────┐   │   │
│  │  │  Sidebar           │  │  Comparison Table                    │   │   │
│  │  │  - My Comparisons  │  │  ┌─────────┬─────────┬─────────┐     │   │   │
│  │  │    - Family Sedans │  │  │ Vehicle │ Vehicle │ Vehicle │     │   │   │
│  │  │    - Compact Cars  │  │  │    A    │    B    │    C    │     │   │   │
│  │  │  ────────────────  │  │  ├─────────┼─────────┼─────────┤     │   │   │
│  │  │  + New Comparison  │  │  │ Price   │ Price   │ Price   │     │   │   │
│  │  └────────────────────┘  │  │ Year    │ Year    │ Year    │     │   │   │
│  │                          │  │ Mileage │ Mileage │ Mileage │     │   │   │
│  │                          │  │ Engine  │ Engine  │ Engine  │     │   │   │
│  │                          │  │ HP      │ HP      │ HP      │     │   │   │
│  │                          │  └─────────┴─────────┴─────────┘     │   │   │
│  │                          └──────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. AlertsPage.tsx

### Ubicación

`frontend/web/src/pages/AlertsPage.tsx`

### Descripción

Página para gestionar alertas de precio y búsquedas guardadas del usuario.

### Ruta

`/alerts`

### Características

- **Tabs** - Alternar entre Price Alerts y Saved Searches
- **Price Alerts** - Notificaciones cuando baja el precio de un vehículo
- **Saved Searches** - Búsquedas guardadas con filtros personalizados
- **Toggle Active** - Activar/pausar alertas individuales
- **Delete** - Eliminar alertas
- **Free Days Badge** - Muestra días gratis restantes (Early Bird)

### Líneas de Código

~382 líneas

### Dependencias

```typescript
import { useState, useEffect } from "react";
import { FiBell, FiSearch, FiTrash2, FiEdit3, FiClock } from "react-icons/fi";
import MainLayout from "@/layouts/MainLayout";
import Button from "@/components/atoms/Button";
import Input from "@/components/atoms/Input";
import EmptyState from "@/components/organisms/EmptyState";
```

### TypeScript Types

```typescript
interface PriceAlert {
  id: string;
  vehicleId: string;
  vehicleTitle: string;
  currentPrice: number;
  targetPrice: number;
  isActive: boolean;
  createdAt: string;
}

interface SavedSearch {
  id: string;
  name: string;
  filters: {
    make?: string;
    model?: string;
    minYear?: number;
    maxYear?: number;
    minPrice?: number;
    maxPrice?: number;
  };
  isActive: boolean;
  createdAt: string;
}
```

### Estado del Componente

```typescript
const [activeTab, setActiveTab] = useState<"price" | "search">("price");
const [priceAlerts, setPriceAlerts] = useState<PriceAlert[]>([]);
const [savedSearches, setSavedSearches] = useState<SavedSearch[]>([]);
const [loading, setLoading] = useState(true);
const [freeDaysLeft, setFreeDaysLeft] = useState<number>(0);
```

### Llamadas API

```typescript
const loadAlerts = async () => {
  const token = localStorage.getItem("accessToken");

  const [priceResponse, searchResponse, daysResponse] = await Promise.all([
    fetch("https://api.okla.com.do/api/alerts/price-alerts", {
      headers: { Authorization: `Bearer ${token}` },
    }),
    fetch("https://api.okla.com.do/api/alerts/saved-searches", {
      headers: { Authorization: `Bearer ${token}` },
    }),
    fetch("https://api.okla.com.do/api/alerts/free-days-left", {
      headers: { Authorization: `Bearer ${token}` },
    }),
  ]);

  setPriceAlerts(await priceResponse.json());
  setSavedSearches(await searchResponse.json());
  setFreeDaysLeft((await daysResponse.json()).daysLeft);
};
```

### Handlers

```typescript
// Toggle price alert
const togglePriceAlert = async (alertId: string, isActive: boolean) => {
  await fetch(`/api/alerts/price-alerts/${alertId}/toggle`, {
    method: "PUT",
    body: JSON.stringify({ isActive: !isActive }),
  });
  setPriceAlerts(
    priceAlerts.map((a) =>
      a.id === alertId ? { ...a, isActive: !isActive } : a,
    ),
  );
};

// Delete price alert
const deletePriceAlert = async (alertId: string) => {
  if (!confirm("¿Eliminar esta alerta?")) return;
  await fetch(`/api/alerts/price-alerts/${alertId}`, { method: "DELETE" });
  setPriceAlerts(priceAlerts.filter((a) => a.id !== alertId));
};

// Toggle saved search
const toggleSavedSearch = async (searchId: string, isActive: boolean) => {
  await fetch(`/api/alerts/saved-searches/${searchId}/toggle`, {
    method: "PUT",
    body: JSON.stringify({ isActive: !isActive }),
  });
  setSavedSearches(
    savedSearches.map((s) =>
      s.id === searchId ? { ...s, isActive: !isActive } : s,
    ),
  );
};

// Delete saved search
const deleteSavedSearch = async (searchId: string) => {
  if (!confirm("¿Eliminar esta búsqueda guardada?")) return;
  await fetch(`/api/alerts/saved-searches/${searchId}`, { method: "DELETE" });
  setSavedSearches(savedSearches.filter((s) => s.id !== searchId));
};
```

### Formateo de Precio

```typescript
const formatPrice = (price: number) => {
  return new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency: "DOP",
    minimumFractionDigits: 0,
  }).format(price);
};
```

---

## 2. ComparisonPage.tsx

### Ubicación

`frontend/web/src/pages/ComparisonPage.tsx`

### Descripción

Comparador de vehículos que permite comparar hasta 3 vehículos lado a lado.

### Ruta

`/comparison`

### Características

- **My Comparisons** - Sidebar con lista de comparaciones guardadas
- **Comparison Table** - Tabla con specs lado a lado
- **Create Comparison** - Modal para crear nueva comparación
- **Add Vehicles** - Modal para agregar vehículos
- **Share** - Generar link público para compartir
- **Remove Vehicle** - Quitar vehículo de la comparación
- **Delete Comparison** - Eliminar comparación completa

### Líneas de Código

~683 líneas

### Dependencias

```typescript
import React, { useState, useEffect } from "react";
import {
  FiX,
  FiShare2,
  FiPlus,
  FiGrid,
  FiCalendar,
  FiDollarSign,
  FiActivity,
  FiDroplet,
  FiCopy,
  FiCheck,
  FiSearch,
  FiEdit3,
  FiTrash2,
} from "react-icons/fi";
import { FaCar } from "react-icons/fa";
import MainLayout from "@/layouts/MainLayout";
import Button from "@/components/atoms/Button";
import EmptyState from "@/components/organisms/EmptyState";
```

### TypeScript Types

```typescript
interface Vehicle {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage?: number;
  transmission?: string;
  fuelType?: string;
  condition?: string;
  engineSize?: string;
  horsepower?: number;
  imageUrl: string;
  location?: string;
}

interface VehicleComparison {
  id: string;
  name: string;
  vehicleCount: number;
  createdAt: string;
  isShared: boolean;
  vehicleIds: string[];
  vehicles?: Vehicle[];
}
```

### Estado del Componente

```typescript
const [comparisons, setComparisons] = useState<VehicleComparison[]>([]);
const [selectedComparison, setSelectedComparison] =
  useState<VehicleComparison | null>(null);
const [loading, setLoading] = useState(true);
const [showCreateModal, setShowCreateModal] = useState(false);
const [showVehicleModal, setShowVehicleModal] = useState(false);
const [newComparisonName, setNewComparisonName] = useState("");
const [selectedVehicles, setSelectedVehicles] = useState<string[]>([]);
const [copiedShareUrl, setCopiedShareUrl] = useState(false);
```

### Handlers Principales

```typescript
// Create comparison
const createComparison = async () => {
  const newComparison: VehicleComparison = {
    id: `comp-${Date.now()}`,
    name: newComparisonName,
    vehicleCount: selectedVehicles.length,
    createdAt: new Date().toISOString(),
    isShared: false,
    vehicleIds: selectedVehicles,
    vehicles: mockVehicles.filter((v) => selectedVehicles.includes(v.id)),
  };
  setComparisons((prev) => [newComparison, ...prev]);
  setSelectedComparison(newComparison);
  setShowCreateModal(false);
};

// Delete comparison
const deleteComparison = async (comparisonId: string) => {
  if (!confirm("Are you sure you want to delete this comparison?")) return;
  setComparisons((prev) => prev.filter((c) => c.id !== comparisonId));
};

// Share comparison
const shareComparison = async (comparisonId: string) => {
  const shareToken = Math.random().toString(36).substring(2, 18);
  const shareUrl = `${window.location.origin}/comparison/shared/${shareToken}`;
  await navigator.clipboard.writeText(shareUrl);
  setCopiedShareUrl(true);
  setTimeout(() => setCopiedShareUrl(false), 2000);
  setComparisons((prev) =>
    prev.map((c) => (c.id === comparisonId ? { ...c, isShared: true } : c)),
  );
};

// Remove vehicle from comparison
const removeVehicle = (vehicleId: string) => {
  if (!selectedComparison) return;
  const updatedVehicles =
    selectedComparison.vehicles?.filter((v) => v.id !== vehicleId) || [];
  if (updatedVehicles.length === 0) {
    alert("Cannot remove the last vehicle from comparison");
    return;
  }
  // Update state...
};
```

### Mock Data

```typescript
const mockVehicles: Vehicle[] = [
  {
    id: "1",
    title: "Toyota Camry 2022 Hybrid",
    make: "Toyota",
    model: "Camry",
    year: 2022,
    price: 1800000,
    mileage: 25000,
    fuelType: "Hybrid",
    transmission: "Automatic",
    condition: "Excellent",
    engineSize: "2.5L",
    horsepower: 208,
    imageUrl: "https://images.unsplash.com/...",
    location: "Santo Domingo",
  },
  // ... más vehículos
];
```

### Formateo

```typescript
const formatPrice = (price: number) => {
  return new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency: "DOP",
    minimumFractionDigits: 0,
  }).format(price);
};

const formatMileage = (mileage?: number) => {
  if (!mileage) return "N/A";
  return new Intl.NumberFormat("es-DO").format(mileage) + " km";
};
```

---

## API Endpoints

### AlertsPage

```
GET /api/alerts/price-alerts
Response: PriceAlert[]

GET /api/alerts/saved-searches
Response: SavedSearch[]

GET /api/alerts/free-days-left
Response: { daysLeft: number }

PUT /api/alerts/price-alerts/{id}/toggle
Body: { isActive: boolean }

DELETE /api/alerts/price-alerts/{id}

PUT /api/alerts/saved-searches/{id}/toggle
Body: { isActive: boolean }

DELETE /api/alerts/saved-searches/{id}
```

### ComparisonPage

```
GET /api/comparisons
Response: VehicleComparison[]

POST /api/comparisons
Body: { name: string, vehicleIds: string[] }

DELETE /api/comparisons/{id}

POST /api/comparisons/{id}/share
Response: { shareToken: string, shareUrl: string }

POST /api/comparisons/{id}/vehicles
Body: { vehicleId: string }

DELETE /api/comparisons/{id}/vehicles/{vehicleId}

GET /api/comparisons/shared/{shareToken}
Response: VehicleComparison (public)
```

---

## Servicios Relacionados

| Servicio            | Uso                         |
| ------------------- | --------------------------- |
| `AlertService`      | CRUD de alertas de precio   |
| `AlertService`      | CRUD de búsquedas guardadas |
| `BillingService`    | Días gratis restantes       |
| `ComparisonService` | CRUD de comparaciones       |
| `VehicleService`    | Detalles de vehículos       |

---

## Checklist de Validación

### AlertsPage

- [ ] Tabs funcionan correctamente
- [ ] Price alerts se cargan desde API
- [ ] Toggle active/inactive funciona
- [ ] Delete con confirmación
- [ ] Saved searches se cargan
- [ ] Free days badge se muestra
- [ ] Empty state cuando no hay alertas
- [ ] Loading state mientras carga
- [ ] Precio formateado en DOP

### ComparisonPage

- [ ] Sidebar muestra comparaciones guardadas
- [ ] Click en comparison la selecciona
- [ ] Tabla de specs renderiza hasta 3 vehículos
- [ ] Create modal funciona
- [ ] Vehicle selector funciona
- [ ] Share genera URL y copia al portapapeles
- [ ] Feedback "Copied" después de share
- [ ] Delete comparison con confirmación
- [ ] Remove vehicle funciona (excepto último)
- [ ] Loading y empty states

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/alertas-busquedas.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Alertas y Búsquedas Guardadas", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test.describe("Alertas de Precio", () => {
    test("debe mostrar lista de alertas", async ({ page }) => {
      await page.goto("/alertas");

      await expect(
        page.getByRole("tab", { name: /alertas de precio/i }),
      ).toBeVisible();
      await expect(page.getByTestId("price-alerts-list")).toBeVisible();
    });

    test("debe crear nueva alerta de precio", async ({ page }) => {
      await page.goto("/alertas");

      await page.getByRole("button", { name: /nueva alerta/i }).click();
      await page.getByRole("combobox", { name: /vehículo/i }).click();
      await page.getByRole("option").first().click();
      await page.fill('input[name="targetPrice"]', "800000");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/alerta creada/i)).toBeVisible();
    });

    test("debe activar/desactivar alerta", async ({ page }) => {
      await page.goto("/alertas");

      await page.getByTestId("alert-toggle").first().click();
      await expect(
        page.getByText(/alerta (activada|desactivada)/i),
      ).toBeVisible();
    });
  });

  test.describe("Búsquedas Guardadas", () => {
    test("debe mostrar búsquedas guardadas", async ({ page }) => {
      await page.goto("/alertas");

      await page.getByRole("tab", { name: /búsquedas guardadas/i }).click();
      await expect(page.getByTestId("saved-searches-list")).toBeVisible();
    });

    test("debe ejecutar búsqueda guardada", async ({ page }) => {
      await page.goto("/alertas");
      await page.getByRole("tab", { name: /búsquedas/i }).click();

      await page.getByTestId("saved-search").first().click();
      await expect(page).toHaveURL(/\/vehiculos\?/);
    });

    test("debe eliminar búsqueda con confirmación", async ({ page }) => {
      await page.goto("/alertas");
      await page.getByRole("tab", { name: /búsquedas/i }).click();

      await page.getByTestId("delete-search").first().click();
      await page.getByRole("button", { name: /confirmar/i }).click();

      await expect(page.getByText(/búsqueda eliminada/i)).toBeVisible();
    });
  });
});
```

---

## Notas de Implementación

1. **ProtectedRoute**: Ambas páginas requieren autenticación
2. **Currency**: Precios en DOP (Peso Dominicano)
3. **Limit**: Máximo 3 vehículos por comparación
4. **Share Token**: Genera token aleatorio para URLs públicas
5. **Early Bird**: Muestra días gratis restantes del programa
6. **Mobile**: Tabla de comparación se hace horizontal scrollable
7. **Accessibility**: Labels ARIA en tabs y botones
