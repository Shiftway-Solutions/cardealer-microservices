---
title: "Página de Perfil"
priority: P1
estimated_time: "30 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 👤 Página de Perfil

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** Dashboard, componentes de usuario

---

## 📋 OBJETIVO

Implementar página de perfil:

- Ver perfil público
- Editar perfil propio
- Listado de vehículos del usuario
- Tabs de información

---

## 🎨 WIREFRAME - PERFIL PÚBLICO

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER (Navbar)                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ USER PROFILE HEADER                                                     │ │
│  │ ┌──────────┐                                                           │ │
│  │ │          │  Juan Pérez                    [📩 Contactar] [⭐ Seguir] │ │
│  │ │  AVATAR  │  @juanperez • Santo Domingo                               │ │
│  │ │   80px   │  ⭐ 4.8 (45 reseñas) • Miembro desde Ene 2024             │ │
│  │ │          │  ✅ Verificado • 🏆 Vendedor Destacado                     │ │
│  │ └──────────┘                                                           │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ [Vehículos (8)] [Reseñas (45)] [Información]                          │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────────┤
│  │ VEHÍCULOS PUBLICADOS                                                     │
│  │ ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐          │
│  │ │   🚗 IMG   │  │   🚗 IMG   │  │   🚗 IMG   │  │   🚗 IMG   │          │
│  │ ├────────────┤  ├────────────┤  ├────────────┤  ├────────────┤          │
│  │ │Toyota Camry│  │Honda Civic │  │BMW X5 2023 │  │Mercedes C  │          │
│  │ │$25,000     │  │$18,500     │  │$45,000     │  │$38,000     │          │
│  │ │2022 • 15K  │  │2021 • 28K  │  │2023 • 8K   │  │2022 • 12K  │          │
│  │ └────────────┘  └────────────┘  └────────────┘  └────────────┘          │
│  └──────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────────┤
│  │ ESTADÍSTICAS                                                             │
│  │ ┌───────────────┐  ┌───────────────┐  ┌───────────────┐                 │
│  │ │ 8 Publicados  │  │ 45 Reseñas    │  │ 98% Respuesta │                 │
│  │ └───────────────┘  └───────────────┘  └───────────────┘                 │
│  └──────────────────────────────────────────────────────────────────────────┤
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│ FOOTER                                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - EDITAR PERFIL

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER (Navbar)                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Mi Perfil                                              [Guardar Cambios]   │
│  ─────────                                                                   │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ FOTO DE PERFIL                                                          │ │
│  │     ┌──────────┐                                                        │ │
│  │     │          │   [📷 Cambiar foto]                                    │ │
│  │     │  AVATAR  │                                                        │ │
│  │     │  120px   │   Formatos: JPG, PNG • Max: 5MB                       │ │
│  │     │          │                                                        │ │
│  │     └──────────┘                                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ INFORMACIÓN PERSONAL                                                    │ │
│  │                                                                         │ │
│  │  Nombre                           Apellido                              │ │
│  │  ┌─────────────────────────────┐  ┌─────────────────────────────┐       │ │
│  │  │ Juan                        │  │ Pérez                       │       │ │
│  │  └─────────────────────────────┘  └─────────────────────────────┘       │ │
│  │                                                                         │ │
│  │  Email                            Teléfono                              │ │
│  │  ┌─────────────────────────────┐  ┌─────────────────────────────┐       │ │
│  │  │ juan@email.com    ✅ Verif. │  │ +1 809-555-1234             │       │ │
│  │  └─────────────────────────────┘  └─────────────────────────────┘       │ │
│  │                                                                         │ │
│  │  Bio (Opcional)                                                         │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │ │
│  │  │ Cuéntanos sobre ti...                                           │    │ │
│  │  │                                                                 │    │ │
│  │  └─────────────────────────────────────────────────────────────────┘    │ │
│  │                                                                         │ │
│  │  Ubicación                                                              │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │ │
│  │  │ Santo Domingo, DN                                        ▼      │    │ │
│  │  └─────────────────────────────────────────────────────────────────┘    │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│ FOOTER                                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Perfil Público

```typescript
// filepath: src/app/(main)/usuario/[id]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { UserProfileHeader } from "@/components/user/UserProfileHeader";
import { UserVehicleList } from "@/components/user/UserVehicleList";
import { userService } from "@/lib/services/userService";

interface Props {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const user = await userService.getById(id);

  if (!user) return { title: "Usuario no encontrado" };

  return {
    title: `${user.name} | OKLA`,
    description: `Ver perfil de ${user.name} en OKLA`,
  };
}

export default async function UserProfilePage({ params }: Props) {
  const { id } = await params;
  const user = await userService.getById(id);

  if (!user) notFound();

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container max-w-4xl">
        <UserProfileHeader user={user} />
        <div className="mt-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">
            Vehículos publicados
          </h2>
          <UserVehicleList userId={user.id} />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: UserProfileHeader

```typescript
// filepath: src/components/user/UserProfileHeader.tsx
import { MapPin, Calendar, Shield } from "lucide-react";
import { UserAvatar } from "./UserAvatar";
import { Badge } from "@/components/ui/Badge";
import { formatDate } from "@/lib/utils";
import type { User } from "@/types";

interface UserProfileHeaderProps {
  user: User;
}

export function UserProfileHeader({ user }: UserProfileHeaderProps) {
  return (
    <div className="bg-white rounded-xl border p-6">
      <div className="flex flex-col sm:flex-row gap-6">
        <UserAvatar src={user.image} name={user.name} size="xl" />

        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">{user.name}</h1>
            {user.isVerified && (
              <Badge variant="success">
                <Shield size={12} className="mr-1" />
                Verificado
              </Badge>
            )}
          </div>

          {user.bio && (
            <p className="mt-2 text-gray-600">{user.bio}</p>
          )}

          <div className="mt-4 flex flex-wrap gap-4 text-sm text-gray-500">
            {user.location && (
              <span className="flex items-center gap-1">
                <MapPin size={14} />
                {user.location}
              </span>
            )}
            <span className="flex items-center gap-1">
              <Calendar size={14} />
              Miembro desde {formatDate(user.createdAt, "MMMM yyyy")}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: UserVehicleList

```typescript
// filepath: src/components/user/UserVehicleList.tsx
"use client";

import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { useUserVehicles } from "@/lib/hooks/useUserVehicles";
import { EmptyState } from "@/components/ui/EmptyState";
import { VehicleGridSkeleton } from "@/components/vehicles/VehicleGridSkeleton";
import { Car } from "lucide-react";

interface UserVehicleListProps {
  userId: string;
}

export function UserVehicleList({ userId }: UserVehicleListProps) {
  const { data, isLoading } = useUserVehicles(userId);

  if (isLoading) {
    return <VehicleGridSkeleton count={4} />;
  }

  if (!data?.items?.length) {
    return (
      <EmptyState
        icon={Car}
        title="Sin vehículos"
        description="Este usuario no tiene vehículos publicados"
      />
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {data.items.map((vehicle) => (
        <VehicleCard key={vehicle.id} vehicle={vehicle} />
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 4: Página de Editar Perfil

```typescript
// filepath: src/app/(main)/dashboard/perfil/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { ProfileForm } from "@/components/forms/ProfileForm";
import { userService } from "@/lib/services/userService";

export const metadata: Metadata = {
  title: "Mi Perfil | OKLA",
};

export default async function ProfilePage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dashboard/perfil");
  }

  const user = await userService.getById(session.user.id);

  if (!user) redirect("/dashboard");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Mi Perfil</h1>
        <p className="text-gray-600 mt-1">
          Actualiza tu información personal
        </p>
      </div>

      <div className="bg-white rounded-xl border p-6">
        <ProfileForm user={user} />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: Hooks

```typescript
// filepath: src/lib/hooks/useUserVehicles.ts
import { useQuery } from "@tanstack/react-query";
import { vehicleService } from "@/lib/services/vehicleService";

export function useUserVehicles(userId: string) {
  return useQuery({
    queryKey: ["vehicles", "user", userId],
    queryFn: () => vehicleService.getByUser(userId),
    enabled: !!userId,
  });
}
```

```typescript
// filepath: src/lib/hooks/useProfile.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { userService } from "@/lib/services/userService";
import { useSession } from "next-auth/react";

export function useProfile() {
  const { data: session, update } = useSession();

  const query = useQuery({
    queryKey: ["user", "me"],
    queryFn: () => userService.getMe(),
    enabled: !!session?.user,
  });

  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: userService.updateProfile,
    onSuccess: (data) => {
      queryClient.setQueryData(["user", "me"], data);
      update({ user: data });
    },
  });

  return {
    user: query.data,
    isLoading: query.isLoading,
    updateProfile: mutation.mutateAsync,
    isUpdating: mutation.isPending,
  };
}
```

---

## 🔧 PASO 6: ProfileForm

```typescript
// filepath: src/components/forms/ProfileForm.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Button } from "@/components/ui/Button";
import { AvatarUpload } from "./AvatarUpload";
import { useProfile } from "@/lib/hooks/useProfile";
import type { User } from "@/types";

const profileSchema = z.object({
  name: z.string().min(2, "Nombre muy corto"),
  email: z.string().email("Email inválido"),
  phone: z.string().optional(),
  location: z.string().optional(),
  bio: z.string().max(500, "Máximo 500 caracteres").optional(),
});

type ProfileFormData = z.infer<typeof profileSchema>;

interface ProfileFormProps {
  user: User;
}

export function ProfileForm({ user }: ProfileFormProps) {
  const { updateProfile, isUpdating } = useProfile();

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      name: user.name,
      email: user.email,
      phone: user.phone || "",
      location: user.location || "",
      bio: user.bio || "",
    },
  });

  const onSubmit = async (data: ProfileFormData) => {
    await updateProfile(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Avatar */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Foto de perfil
        </label>
        <AvatarUpload currentImage={user.image} userId={user.id} />
      </div>

      {/* Name */}
      <FormField label="Nombre completo" error={errors.name?.message}>
        <Input {...register("name")} placeholder="Juan Pérez" />
      </FormField>

      {/* Email */}
      <FormField label="Email" error={errors.email?.message}>
        <Input {...register("email")} type="email" placeholder="juan@example.com" />
      </FormField>

      {/* Phone */}
      <FormField label="Teléfono" error={errors.phone?.message}>
        <Input {...register("phone")} type="tel" placeholder="+1 (809) 555-1234" />
      </FormField>

      {/* Location */}
      <FormField label="Ubicación" error={errors.location?.message}>
        <Input {...register("location")} placeholder="Santo Domingo, RD" />
      </FormField>

      {/* Bio */}
      <FormField label="Biografía" error={errors.bio?.message}>
        <Textarea
          {...register("bio")}
          rows={4}
          placeholder="Cuéntanos sobre ti..."
          maxLength={500}
        />
      </FormField>

      {/* Submit */}
      <Button type="submit" disabled={!isDirty || isUpdating}>
        {isUpdating ? "Guardando..." : "Guardar cambios"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 7: Configuración de Notificaciones

```typescript
// filepath: src/app/(main)/dashboard/perfil/notificaciones/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { NotificationSettings } from "@/components/settings/NotificationSettings";

export const metadata: Metadata = {
  title: "Notificaciones | OKLA",
};

export default async function NotificationsPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dashboard/perfil/notificaciones");
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Notificaciones</h1>
        <p className="text-gray-600 mt-1">
          Configura cómo quieres recibir notificaciones
        </p>
      </div>

      <div className="bg-white rounded-xl border p-6">
        <NotificationSettings userId={session.user.id} />
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/settings/NotificationSettings.tsx
"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { Bell, Mail, MessageCircle } from "lucide-react";
import { Switch } from "@/components/ui/Switch";
import { Button } from "@/components/ui/Button";
import { useNotificationSettings } from "@/lib/hooks/useNotificationSettings";

interface NotificationSettingsProps {
  userId: string;
}

export function NotificationSettings({ userId }: NotificationSettingsProps) {
  const { data: settings, updateSettings, isLoading } = useNotificationSettings(userId);
  const { register, handleSubmit, reset, formState: { isDirty } } = useForm();

  useEffect(() => {
    if (settings) {
      reset(settings);
    }
  }, [settings, reset]);

  const onSubmit = async (data: any) => {
    await updateSettings(data);
  };

  if (isLoading) {
    return <div>Cargando configuración...</div>;
  }

  const sections = [
    {
      icon: Bell,
      title: "Notificaciones push",
      description: "Recibe notificaciones en tiempo real",
      settings: [
        { key: "push.newMessage", label: "Nuevos mensajes" },
        { key: "push.vehicleInterest", label: "Interés en tu vehículo" },
        { key: "push.priceAlert", label: "Alertas de precio" },
      ],
    },
    {
      icon: Mail,
      title: "Email",
      description: "Recibe emails informativos",
      settings: [
        { key: "email.weeklyDigest", label: "Resumen semanal" },
        { key: "email.promotions", label: "Promociones y ofertas" },
        { key: "email.newsletter", label: "Newsletter" },
        { key: "email.systemUpdates", label: "Actualizaciones del sistema" },
      ],
    },
    {
      icon: MessageCircle,
      title: "SMS",
      description: "Recibe mensajes de texto",
      settings: [
        { key: "sms.securityAlerts", label: "Alertas de seguridad" },
        { key: "sms.appointmentReminders", label: "Recordatorios de citas" },
      ],
    },
  ];

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">
      {sections.map((section) => (
        <div key={section.title}>
          <div className="flex items-start gap-3 mb-4">
            <div className="p-2 bg-primary-100 rounded-lg">
              <section.icon size={20} className="text-primary-600" />
            </div>
            <div>
              <h3 className="font-semibold text-gray-900">{section.title}</h3>
              <p className="text-sm text-gray-600">{section.description}</p>
            </div>
          </div>

          <div className="space-y-3 ml-11">
            {section.settings.map((setting) => (
              <div
                key={setting.key}
                className="flex items-center justify-between py-3 border-b last:border-0"
              >
                <label className="text-sm text-gray-700">{setting.label}</label>
                <Switch {...register(setting.key)} />
              </div>
            ))}
          </div>
        </div>
      ))}

      <Button type="submit" disabled={!isDirty}>
        Guardar preferencias
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 8: Configuración de Privacidad

```typescript
// filepath: src/app/(main)/dashboard/perfil/privacidad/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { PrivacySettings } from "@/components/settings/PrivacySettings";

export const metadata: Metadata = {
  title: "Privacidad | OKLA",
};

export default async function PrivacyPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dashboard/perfil/privacidad");
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Privacidad</h1>
        <p className="text-gray-600 mt-1">
          Controla quién puede ver tu información
        </p>
      </div>

      <div className="bg-white rounded-xl border p-6">
        <PrivacySettings userId={session.user.id} />
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/settings/PrivacySettings.tsx
"use client";

import { useForm } from "react-hook-form";
import { Shield, Eye, Phone, Mail } from "lucide-react";
import { Select } from "@/components/ui/Select";
import { Switch } from "@/components/ui/Switch";
import { Button } from "@/components/ui/Button";
import { usePrivacySettings } from "@/lib/hooks/usePrivacySettings";

interface PrivacySettingsProps {
  userId: string;
}

export function PrivacySettings({ userId }: PrivacySettingsProps) {
  const { data: settings, updateSettings, isLoading } = usePrivacySettings(userId);
  const { register, handleSubmit, formState: { isDirty } } = useForm({
    defaultValues: settings,
  });

  const onSubmit = async (data: any) => {
    await updateSettings(data);
  };

  if (isLoading) {
    return <div>Cargando configuración...</div>;
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Profile Visibility */}
      <div>
        <div className="flex items-center gap-2 mb-4">
          <Eye size={20} className="text-gray-600" />
          <h3 className="font-semibold text-gray-900">Visibilidad del perfil</h3>
        </div>
        <div className="space-y-3 ml-7">
          <div>
            <label className="block text-sm text-gray-700 mb-2">
              ¿Quién puede ver tu perfil?
            </label>
            <Select {...register("profileVisibility")}>
              <option value="public">Todos</option>
              <option value="registered">Solo usuarios registrados</option>
              <option value="private">Solo yo</option>
            </Select>
          </div>

          <div className="flex items-center justify-between py-2">
            <label className="text-sm text-gray-700">
              Mostrar mis vehículos en mi perfil
            </label>
            <Switch {...register("showVehicles")} />
          </div>

          <div className="flex items-center justify-between py-2">
            <label className="text-sm text-gray-700">
              Aparecer en búsqueda de usuarios
            </label>
            <Switch {...register("searchable")} />
          </div>
        </div>
      </div>

      {/* Contact Information */}
      <div>
        <div className="flex items-center gap-2 mb-4">
          <Phone size={20} className="text-gray-600" />
          <h3 className="font-semibold text-gray-900">Información de contacto</h3>
        </div>
        <div className="space-y-3 ml-7">
          <div>
            <label className="block text-sm text-gray-700 mb-2">
              Mostrar teléfono
            </label>
            <Select {...register("phoneVisibility")}>
              <option value="public">A todos</option>
              <option value="interested">Solo a interesados en mis vehículos</option>
              <option value="private">No mostrar</option>
            </Select>
          </div>

          <div>
            <label className="block text-sm text-gray-700 mb-2">
              Mostrar email
            </label>
            <Select {...register("emailVisibility")}>
              <option value="public">A todos</option>
              <option value="interested">Solo a interesados en mis vehículos</option>
              <option value="private">No mostrar</option>
            </Select>
          </div>
        </div>
      </div>

      {/* Activity Tracking */}
      <div>
        <div className="flex items-center gap-2 mb-4">
          <Shield size={20} className="text-gray-600" />
          <h3 className="font-semibold text-gray-900">Seguimiento de actividad</h3>
        </div>
        <div className="space-y-3 ml-7">
          <div className="flex items-center justify-between py-2">
            <div>
              <label className="text-sm text-gray-700">Analytics</label>
              <p className="text-xs text-gray-500">
                Ayúdanos a mejorar con datos anónimos de uso
              </p>
            </div>
            <Switch {...register("allowAnalytics")} />
          </div>

          <div className="flex items-center justify-between py-2">
            <div>
              <label className="text-sm text-gray-700">Personalización</label>
              <p className="text-xs text-gray-500">
                Recibe recomendaciones personalizadas
              </p>
            </div>
            <Switch {...register("allowPersonalization")} />
          </div>
        </div>
      </div>

      <Button type="submit" disabled={!isDirty}>
        Guardar configuración
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 9: Seguridad y Contraseña

```typescript
// filepath: src/app/(main)/dashboard/perfil/seguridad/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { ChangePasswordForm } from "@/components/settings/ChangePasswordForm";
import { TwoFactorAuth } from "@/components/settings/TwoFactorAuth";
import { ActiveSessions } from "@/components/settings/ActiveSessions";

export const metadata: Metadata = {
  title: "Seguridad | OKLA",
};

export default async function SecurityPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dashboard/perfil/seguridad");
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Seguridad</h1>
        <p className="text-gray-600 mt-1">
          Protege tu cuenta y datos personales
        </p>
      </div>

      {/* Change Password */}
      <div className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold text-gray-900 mb-4">Cambiar contraseña</h2>
        <ChangePasswordForm />
      </div>

      {/* Two-Factor Authentication */}
      <div className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold text-gray-900 mb-4">
          Autenticación de dos factores (2FA)
        </h2>
        <p className="text-sm text-gray-600 mb-6">
          Agrega una capa extra de seguridad requiriendo un código adicional al iniciar sesión
        </p>
        <TwoFactorAuth userId={session.user.id} />
      </div>

      {/* Active Sessions */}
      <div className="bg-white rounded-xl border p-6">
        <h2 className="font-semibold text-gray-900 mb-4">Sesiones activas</h2>
        <ActiveSessions userId={session.user.id} />
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/settings/ChangePasswordForm.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { useChangePassword } from "@/lib/hooks/useProfile";

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Contraseña actual requerida"),
    newPassword: z.string().min(8, "Mínimo 8 caracteres"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Las contraseñas no coinciden",
    path: ["confirmPassword"],
  });

type PasswordFormData = z.infer<typeof passwordSchema>;

export function ChangePasswordForm() {
  const { mutate: changePassword, isPending } = useChangePassword();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
  });

  const onSubmit = async (data: PasswordFormData) => {
    changePassword(
      {
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      },
      {
        onSuccess: () => reset(),
      }
    );
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 max-w-md">
      <FormField label="Contraseña actual" error={errors.currentPassword?.message}>
        <Input {...register("currentPassword")} type="password" />
      </FormField>

      <FormField label="Nueva contraseña" error={errors.newPassword?.message}>
        <Input {...register("newPassword")} type="password" />
      </FormField>

      <FormField label="Confirmar contraseña" error={errors.confirmPassword?.message}>
        <Input {...register("confirmPassword")} type="password" />
      </FormField>

      <Button type="submit" disabled={isPending}>
        {isPending ? "Cambiando..." : "Cambiar contraseña"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 10: Tabs de Configuración

```typescript
// filepath: src/app/(main)/dashboard/perfil/layout.tsx
"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { User, Bell, Shield, Lock } from "lucide-react";
import { cn } from "@/lib/utils";

const tabs = [
  { href: "/dashboard/perfil", label: "Perfil", icon: User },
  { href: "/dashboard/perfil/notificaciones", label: "Notificaciones", icon: Bell },
  { href: "/dashboard/perfil/privacidad", label: "Privacidad", icon: Shield },
  { href: "/dashboard/perfil/seguridad", label: "Seguridad", icon: Lock },
];

export default function ProfileLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  return (
    <div>
      {/* Tabs */}
      <div className="border-b mb-6">
        <nav className="flex gap-6">
          {tabs.map((tab) => {
            const isActive = pathname === tab.href;

            return (
              <Link
                key={tab.href}
                href={tab.href}
                className={cn(
                  "flex items-center gap-2 px-1 py-4 border-b-2 font-medium transition-colors",
                  isActive
                    ? "border-primary-600 text-primary-600"
                    : "border-transparent text-gray-500 hover:text-gray-700"
                )}
              >
                <tab.icon size={18} />
                {tab.label}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Content */}
      {children}
    </div>
  );
}
```

```typescript
// filepath: src/components/settings/TwoFactorAuth.tsx
"use client";

import { useState } from "react";
import { Shield, CheckCircle, AlertTriangle, Key, Smartphone, Download } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Input } from "@/components/ui/Input";
import { RadioGroup } from "@/components/ui/RadioGroup";
import { use2FAStatus, useEnable2FA, useDisable2FA, useGenerate2FA } from "@/lib/hooks/use2FA";
import { QRCodeSVG } from "qrcode.react";

interface TwoFactorAuthProps {
  userId: string;
}

type TwoFAMethod = "totp" | "sms";

export function TwoFactorAuth({ userId }: TwoFactorAuthProps) {
  const { data: status, isLoading } = use2FAStatus();
  const [setupStep, setSetupStep] = useState<"method" | "qr" | "verify" | "backup" | null>(null);
  const [selectedMethod, setSelectedMethod] = useState<TwoFAMethod>("totp");
  const [verificationCode, setVerificationCode] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");

  const { mutate: generate2FA, data: setup } = useGenerate2FA();
  const { mutate: enable2FA, isPending: isEnabling } = useEnable2FA();
  const { mutate: disable2FA, isPending: isDisabling } = useDisable2FA();

  // Already enabled
  if (status?.isEnabled && setupStep === null) {
    return (
      <div className="space-y-6">
        {/* Status badge */}
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-3">
            <div className="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
              <CheckCircle size={20} className="text-green-600" />
            </div>
            <div>
              <h3 className="font-semibold text-gray-900">
                2FA Activado
              </h3>
              <p className="text-sm text-gray-600 mt-1">
                Tu cuenta está protegida con autenticación de dos factores
              </p>
              <div className="flex items-center gap-2 mt-2">
                <Badge variant="success">
                  {status.method === "totp" ? "Aplicación (TOTP)" : "SMS"}
                </Badge>
                <span className="text-xs text-gray-500">
                  Activado el {new Date(status.enabledAt!).toLocaleDateString("es-DO")}
                </span>
              </div>
            </div>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => disable2FA()}
            disabled={isDisabling}
          >
            {isDisabling ? "Desactivando..." : "Desactivar"}
          </Button>
        </div>

        {/* Recovery codes */}
        {status.recoveryCodes && status.recoveryCodes.length > 0 && (
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <Key size={20} className="text-yellow-600 flex-shrink-0 mt-0.5" />
              <div>
                <h4 className="text-sm font-semibold text-yellow-900">
                  Códigos de recuperación
                </h4>
                <p className="text-sm text-yellow-700 mt-1">
                  Guardaste {status.recoveryCodes.length} códigos de recuperación.
                  Úsalos si pierdes acceso a tu método de 2FA.
                </p>
                <Button
                  size="sm"
                  variant="outline"
                  className="mt-3"
                  onClick={() => {
                    const blob = new Blob([status.recoveryCodes!.join("\n")], { type: "text/plain" });
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = "okla-recovery-codes.txt";
                    a.click();
                  }}
                >
                  <Download size={14} className="mr-2" />
                  Descargar códigos
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  }

  // Not enabled - show setup button
  if (setupStep === null) {
    return (
      <div className="space-y-6">
        <div className="flex items-start gap-3">
          <div className="w-10 h-10 bg-gray-100 rounded-full flex items-center justify-center flex-shrink-0">
            <AlertTriangle size={20} className="text-gray-600" />
          </div>
          <div className="flex-1">
            <h3 className="font-semibold text-gray-900">
              2FA no configurado
            </h3>
            <p className="text-sm text-gray-600 mt-1">
              Protege tu cuenta con un código adicional al iniciar sesión
            </p>
          </div>
          <Button onClick={() => setSetupStep("method")}>
            Activar 2FA
          </Button>
        </div>

        {/* Benefits */}
        <div className="grid grid-cols-2 gap-4">
          <div className="bg-blue-50 rounded-lg p-4">
            <Shield size={24} className="text-blue-600 mb-2" />
            <h4 className="font-medium text-gray-900">Mayor seguridad</h4>
            <p className="text-sm text-gray-600 mt-1">
              Protección contra accesos no autorizados
            </p>
          </div>
          <div className="bg-green-50 rounded-lg p-4">
            <CheckCircle size={24} className="text-green-600 mb-2" />
            <h4 className="font-medium text-gray-900">Fácil de usar</h4>
            <p className="text-sm text-gray-600 mt-1">
              Configuración en menos de 2 minutos
            </p>
          </div>
        </div>
      </div>
    );
  }

  // STEP 1: Choose method
  if (setupStep === "method") {
    return (
      <div className="space-y-6">
        <div>
          <h3 className="font-semibold text-gray-900 mb-2">
            Elige tu método de 2FA
          </h3>
          <p className="text-sm text-gray-600">
            Selecciona cómo deseas recibir los códigos de verificación
          </p>
        </div>

        <RadioGroup
          value={selectedMethod}
          onChange={(value) => setSelectedMethod(value as TwoFAMethod)}
          options={[
            {
              value: "totp",
              label: "Aplicación de autenticación (TOTP)",
              description:
                "Usa Google Authenticator, Authy o similar. Más seguro.",
              icon: <Key size={20} className="text-primary-600" />,
            },
            {
              value: "sms",
              label: "Mensaje de texto (SMS)",
              description: "Recibe códigos por SMS. Requiere señal móvil.",
              icon: <Smartphone size={20} className="text-primary-600" />,
            },
          ]}
        />

        <div className="flex gap-3">
          <Button variant="outline" onClick={() => setSetupStep(null)}>
            Cancelar
          </Button>
          <Button
            onClick={() => {
              if (selectedMethod === "totp") {
                generate2FA({ method: "totp" });
                setSetupStep("qr");
              } else {
                setSetupStep("verify");
              }
            }}
          >
            Continuar
          </Button>
        </div>
      </div>
    );
  }

  // STEP 2: Scan QR (TOTP only)
  if (setupStep === "qr" && setup) {
    return (
      <div className="space-y-6">
        <div>
          <h3 className="font-semibold text-gray-900 mb-2">
            Escanea el código QR
          </h3>
          <p className="text-sm text-gray-600">
            Abre tu app de autenticación y escanea este código
          </p>
        </div>

        {/* QR Code */}
        <div className="flex justify-center py-6">
          <div className="bg-white p-6 rounded-lg border">
            <QRCodeSVG value={setup.qrCode} size={200} />
          </div>
        </div>

        {/* Manual entry */}
        <div className="bg-gray-50 rounded-lg p-4">
          <p className="text-sm font-medium text-gray-700 mb-2">
            O ingresa este código manualmente:
          </p>
          <code className="text-sm bg-white px-3 py-2 rounded border font-mono block break-all">
            {setup.secret}
          </code>
        </div>

        {/* Apps recommendation */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p className="text-sm text-blue-800">
            <strong>Apps recomendadas:</strong> Google Authenticator, Microsoft Authenticator,
            Authy, 1Password
          </p>
        </div>

        <div className="flex gap-3">
          <Button variant="outline" onClick={() => setSetupStep("method")}>
            Atrás
          </Button>
          <Button onClick={() => setSetupStep("verify")}>
            Ya escaneé el código
          </Button>
        </div>
      </div>
    );
  }

  // STEP 3: Verify code
  if (setupStep === "verify") {
    return (
      <div className="space-y-6">
        <div>
          <h3 className="font-semibold text-gray-900 mb-2">
            Verifica el código
          </h3>
          <p className="text-sm text-gray-600">
            {selectedMethod === "totp"
              ? "Ingresa el código de 6 dígitos de tu app"
              : "Ingresa tu número de teléfono para recibir el código"}
          </p>
        </div>

        {selectedMethod === "sms" && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Número de teléfono
            </label>
            <Input
              type="tel"
              placeholder="+1 (809) 123-4567"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
            />
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Código de verificación
          </label>
          <Input
            type="text"
            placeholder="000000"
            value={verificationCode}
            onChange={(e) =>
              setVerificationCode(e.target.value.replace(/\D/g, "").slice(0, 6))
            }
            maxLength={6}
            className="text-center text-2xl tracking-widest"
          />
        </div>

        <div className="flex gap-3">
          <Button
            variant="outline"
            onClick={() => setSetupStep(selectedMethod === "totp" ? "qr" : "method")}
          >
            Atrás
          </Button>
          <Button
            onClick={() => {
              enable2FA(
                {
                  method: selectedMethod,
                  code: verificationCode,
                  phoneNumber: selectedMethod === "sms" ? phoneNumber : undefined,
                },
                {
                  onSuccess: () => {
                    setSetupStep("backup");
                  },
                }
              );
            }}
            disabled={verificationCode.length !== 6 || isEnabling}
          >
            {isEnabling ? "Verificando..." : "Verificar y activar"}
          </Button>
        </div>
      </div>
    );
  }

  // STEP 4: Backup codes
  if (setupStep === "backup" && setup?.backupCodes) {
    return (
      <div className="space-y-6">
        <div className="text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <CheckCircle size={32} className="text-green-600" />
          </div>
          <h3 className="text-xl font-semibold text-gray-900">
            ¡2FA Activado!
          </h3>
          <p className="text-gray-600 mt-2">
            Guarda estos códigos de recuperación en un lugar seguro
          </p>
        </div>

        {/* Recovery codes grid */}
        <div className="bg-gray-50 rounded-lg p-6">
          <div className="grid grid-cols-2 gap-3 mb-4">
            {setup.backupCodes.map((code, i) => (
              <code
                key={i}
                className="text-sm bg-white px-4 py-3 rounded border font-mono text-center"
              >
                {code}
              </code>
            ))}
          </div>
          <Button
            variant="outline"
            className="w-full"
            onClick={() => {
              const blob = new Blob([setup.backupCodes!.join("\n")], {
                type: "text/plain",
              });
              const url = URL.createObjectURL(blob);
              const a = document.createElement("a");
              a.href = url;
              a.download = "okla-recovery-codes.txt";
              a.click();
            }}
          >
            <Download size={16} className="mr-2" />
            Descargar códigos
          </Button>
        </div>

        {/* Warning */}
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <AlertTriangle size={20} className="text-yellow-600 flex-shrink-0" />
            <div>
              <p className="text-sm font-medium text-yellow-900">Importante</p>
              <p className="text-sm text-yellow-700 mt-1">
                Cada código solo puede usarse una vez. Si pierdes estos códigos y
                tu dispositivo 2FA, NO podrás acceder a tu cuenta.
              </p>
            </div>
          </div>
        </div>

        <Button onClick={() => setSetupStep(null)} className="w-full">
          Entendido, guardar códigos
        </Button>
      </div>
    );
  }

  return null;
}
```

---

## 🔧 PASO 11: Device Fingerprinting UI (P2 - Opcional)

Agrega visualización de dispositivos en ActiveSessions:

```typescript
// filepath: src/components/settings/ActiveSessions.tsx (Expandido)
"use client";

import { useState } from "react";
import { Monitor, Smartphone, Tablet, MapPin, Trash2, CheckCircle } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useActiveSessions, useRevokeSession } from "@/lib/hooks/useProfile";

export function ActiveSessions({ userId }: { userId: string }) {
  const { data: sessions, isLoading } = useActiveSessions(userId);
  const { mutate: revokeSession } = useRevokeSession();
  const [revokingId, setRevokingId] = useState<string | null>(null);

  const getDeviceIcon = (deviceType: string) => {
    switch (deviceType.toLowerCase()) {
      case "mobile":
        return Smartphone;
      case "tablet":
        return Tablet;
      default:
        return Monitor;
    }
  };

  const handleRevoke = async (sessionId: string) => {
    if (sessionId === sessions?.currentSessionId) {
      if (!confirm("Esta es tu sesión actual. Se cerrará inmediatamente.")) {
        return;
      }
    }

    setRevokingId(sessionId);
    revokeSession(sessionId, {
      onSettled: () => setRevokingId(null),
    });
  };

  if (isLoading) {
    return <div>Cargando sesiones...</div>;
  }

  return (
    <div className="space-y-4">
      {sessions?.items.map((session) => {
        const DeviceIcon = getDeviceIcon(session.deviceType);
        const isCurrent = session.id === sessions.currentSessionId;

        return (
          <div
            key={session.id}
            className={`flex items-start gap-4 p-4 rounded-lg border ${
              isCurrent ? "bg-green-50 border-green-200" : "bg-white"
            }`}
          >
            {/* Device icon */}
            <div className={`w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0 ${
              isCurrent ? "bg-green-100" : "bg-gray-100"
            }`}>
              <DeviceIcon size={24} className={isCurrent ? "text-green-600" : "text-gray-600"} />
            </div>

            {/* Session info */}
            <div className="flex-1">
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <h4 className="font-medium text-gray-900">
                      {session.deviceName || session.browser}
                    </h4>
                    {isCurrent && (
                      <Badge variant="success" size="sm">
                        <CheckCircle size={12} className="mr-1" />
                        Esta sesión
                      </Badge>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 mt-1">
                    {session.browser} • {session.os}
                  </p>
                </div>
                {!isCurrent && (
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleRevoke(session.id)}
                    disabled={revokingId === session.id}
                  >
                    <Trash2 size={14} className="text-red-600" />
                  </Button>
                )}
              </div>

              {/* Location & IP */}
              <div className="mt-3 space-y-2">
                <div className="flex items-center gap-2 text-sm text-gray-600">
                  <MapPin size={14} />
                  <span>
                    {session.city}, {session.country}
                  </span>
                </div>
                <div className="flex items-center gap-2 text-sm text-gray-500">
                  <span>IP: {maskIP(session.ipAddress)}</span>
                  <span>•</span>
                  <span>
                    {session.isTrusted && "✓ Dispositivo confiable • "}
                    Última actividad: {formatRelativeTime(session.lastActivityAt)}
                  </span>
                </div>
              </div>

              {/* Trust device checkbox (only for current session) */}
              {isCurrent && (
                <label className="flex items-center gap-2 mt-3 text-sm">
                  <input
                    type="checkbox"
                    checked={session.isTrusted}
                    onChange={(e) => {
                      // TODO: API call to toggle trust
                      console.log("Toggle trust:", e.target.checked);
                    }}
                    className="rounded border-gray-300"
                  />
                  <span className="text-gray-700">Confiar en este dispositivo (no pedir 2FA)</span>
                </label>
              )}
            </div>
          </div>
        );
      })}

      {/* Revoke all button */}
      {sessions && sessions.items.length > 1 && (
        <Button
          variant="outline"
          onClick={() => {
            if (confirm("¿Cerrar todas las sesiones excepto la actual?")) {
              sessions.items
                .filter((s) => s.id !== sessions.currentSessionId)
                .forEach((s) => revokeSession(s.id));
            }
          }}
          className="w-full text-red-600 hover:bg-red-50"
        >
          Cerrar todas las otras sesiones
        </Button>
      )}
    </div>
  );
}

// Helper functions
function maskIP(ip: string): string {
  const parts = ip.split(".");
  if (parts.length === 4) {
    return `${parts[0]}.${parts[1]}.${parts[2]}.***`;
  }
  return ip;
}

function formatRelativeTime(date: string): string {
  const now = new Date();
  const then = new Date(date);
  const diff = now.getTime() - then.getTime();
  const minutes = Math.floor(diff / 60000);

  if (minutes < 1) return "Ahora";
  if (minutes < 60) return `Hace ${minutes} minutos`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `Hace ${hours} horas`;
  const days = Math.floor(hours / 24);
  return `Hace ${days} días`;
}
```

---

## 🔧 PASO 12: SMS 2FA Setup Flow (P2 - Opcional)

Expande TwoFactorAuth para incluir flujo completo de SMS:

```typescript
// filepath: src/components/settings/TwoFactorAuth.tsx (SMS Flow Expansion)

// Agregar al componente existente, después del paso "verify":

  // STEP 3b: SMS Setup (when selectedMethod === "sms")
  if (setupStep === "sms-setup" && selectedMethod === "sms") {
    return (
      <div className="space-y-6">
        <div>
          <h3 className="font-semibold text-gray-900 mb-2">
            Configura SMS 2FA
          </h3>
          <p className="text-sm text-gray-600">
            Ingresa tu número de teléfono para recibir códigos por SMS
          </p>
        </div>

        {/* Phone number input */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Número de teléfono
          </label>
          <div className="flex gap-2">
            <select className="w-24 px-3 py-2 border rounded-lg">
              <option value="+1">🇩🇴 +1</option>
              <option value="+1">🇺🇸 +1</option>
              <option value="+34">🇪🇸 +34</option>
              <option value="+52">🇲🇽 +52</option>
            </select>
            <Input
              type="tel"
              placeholder="(809) 123-4567"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              className="flex-1"
            />
          </div>
          <p className="text-xs text-gray-500 mt-2">
            Formato: 809-123-4567 (sin guiones ni espacios)
          </p>
        </div>

        {/* Send test SMS button */}
        <Button
          onClick={async () => {
            try {
              await authService.sendTestSMS(phoneNumber);
              showToast.success("SMS enviado", "Revisa tu teléfono");
              setSetupStep("verify");
            } catch {
              showToast.error("Error al enviar SMS");
            }
          }}
          disabled={!phoneNumber || phoneNumber.length < 10}
        >
          Enviar código de prueba
        </Button>

        {/* Info about SMS costs */}
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <Smartphone size={20} className="text-yellow-600 flex-shrink-0" />
            <div>
              <p className="text-sm font-medium text-yellow-900">
                Sobre SMS 2FA
              </p>
              <ul className="text-sm text-yellow-700 mt-1 space-y-1">
                <li>• Requiere señal móvil cada vez que inicies sesión</li>
                <li>• Pueden aplicar cargos estándar de SMS</li>
                <li>• Menos seguro que app de autenticación</li>
              </ul>
            </div>
          </div>
        </div>

        <div className="flex gap-3">
          <Button variant="outline" onClick={() => setSetupStep("method")}>
            Atrás
          </Button>
        </div>
      </div>
    );
  }

  // Modificar el STEP 3 "verify" para manejar SMS:
  if (setupStep === "verify") {
    return (
      <div className="space-y-6">
        <div>
          <h3 className="font-semibold text-gray-900 mb-2">
            Verifica el código
          </h3>
          <p className="text-sm text-gray-600">
            {selectedMethod === "totp"
              ? "Ingresa el código de 6 dígitos de tu app"
              : `Ingresa el código enviado a ${phoneNumber}`}
          </p>
        </div>

        {/* Only show phone input for SMS if not set */}
        {selectedMethod === "sms" && !phoneNumber && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Número de teléfono
            </label>
            <Input
              type="tel"
              placeholder="+1 (809) 123-4567"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
            />
            <Button
              size="sm"
              className="mt-2"
              onClick={async () => {
                try {
                  await authService.sendTestSMS(phoneNumber);
                  showToast.success("Código enviado");
                } catch {
                  showToast.error("Error al enviar SMS");
                }
              }}
            >
              Enviar código
            </Button>
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Código de verificación
          </label>
          <Input
            type="text"
            placeholder="000000"
            value={verificationCode}
            onChange={(e) =>
              setVerificationCode(e.target.value.replace(/\D/g, "").slice(0, 6))
            }
            maxLength={6}
            className="text-center text-2xl tracking-widest"
          />
        </div>

        {/* Resend SMS (only for SMS method) */}
        {selectedMethod === "sms" && (
          <button
            onClick={async () => {
              try {
                await authService.sendTestSMS(phoneNumber);
                showToast.success("Código reenviado");
              } catch {
                showToast.error("Error al reenviar código");
              }
            }}
            className="text-sm text-primary-600 hover:underline"
          >
            ¿No recibiste el código? Reenviar
          </button>
        )}

        <div className="flex gap-3">
          <Button
            variant="outline"
            onClick={() =>
              setSetupStep(selectedMethod === "totp" ? "qr" : "method")
            }
          >
            Atrás
          </Button>
          <Button
            onClick={() => {
              enable2FA(
                {
                  method: selectedMethod,
                  code: verificationCode,
                  phoneNumber: selectedMethod === "sms" ? phoneNumber : undefined,
                },
                {
                  onSuccess: () => {
                    setSetupStep("backup");
                  },
                }
              );
            }}
            disabled={
              verificationCode.length !== 6 ||
              (selectedMethod === "sms" && !phoneNumber) ||
              isEnabling
            }
          >
            {isEnabling ? "Verificando..." : "Verificar y activar"}
          </Button>
        </div>
      </div>
    );
  }
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /usuario/[id] muestra perfil público correctamente
# - /dashboard/perfil permite editar información personal
# - /dashboard/perfil/notificaciones muestra configuración
# - /dashboard/perfil/privacidad permite controlar visibilidad
# - /dashboard/perfil/seguridad permite cambiar contraseña
# - Vehículos del usuario se muestran en su perfil
# - Avatar se puede actualizar
# - Tabs de configuración funcionan
# - Switches y selects guardan cambios
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/user-profile.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("User Profile", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar información del perfil", async ({ page }) => {
    await page.goto("/dashboard/perfil");

    await expect(page.getByTestId("user-avatar")).toBeVisible();
    await expect(page.getByTestId("user-name")).toBeVisible();
    await expect(page.getByTestId("user-email")).toBeVisible();
  });

  test("debe editar información personal", async ({ page }) => {
    await page.goto("/dashboard/perfil");

    await page.getByRole("button", { name: /editar/i }).click();
    await page.fill('input[name="firstName"]', "Juan Carlos");
    await page.click('button[type="submit"]');

    await expect(page.getByText(/perfil actualizado/i)).toBeVisible();
  });

  test("debe actualizar avatar", async ({ page }) => {
    await page.goto("/dashboard/perfil");

    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles("./fixtures/avatar.jpg");

    await expect(page.getByText(/foto actualizada/i)).toBeVisible();
  });

  test("debe navegar entre tabs de configuración", async ({ page }) => {
    await page.goto("/dashboard/perfil");

    await page.getByRole("tab", { name: /notificaciones/i }).click();
    await expect(page).toHaveURL(/notificaciones/);

    await page.getByRole("tab", { name: /privacidad/i }).click();
    await expect(page).toHaveURL(/privacidad/);

    await page.getByRole("tab", { name: /seguridad/i }).click();
    await expect(page).toHaveURL(/seguridad/);
  });

  test("debe mostrar vehículos publicados del usuario", async ({ page }) => {
    await page.goto("/dashboard/perfil");

    await expect(page.getByTestId("user-vehicles")).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/09-configuracion.md`
