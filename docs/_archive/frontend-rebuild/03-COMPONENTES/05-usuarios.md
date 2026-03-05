# 👤 Componentes de Usuario

> **Tiempo estimado:** 40 minutos
> **Prerrequisitos:** Autenticación configurada, tipos de usuario

---

## 📋 OBJETIVO

Implementar componentes de usuario:

- UserAvatar
- UserMenu
- UserProfile
- UserSettings

---

## 🔧 PASO 1: Tipos de Usuario

```typescript
// filepath: src/types/user.ts
export interface User {
  id: string;
  email: string;
  fullName: string;
  avatar?: string;
  phone?: string;
  role: UserRole;
  isVerified: boolean;
  createdAt: string;
  preferences?: UserPreferences;
}

export type UserRole = "buyer" | "individual" | "dealer" | "admin";

export interface UserPreferences {
  emailNotifications: boolean;
  smsNotifications: boolean;
  pushNotifications: boolean;
  newsletter: boolean;
  language: "es" | "en";
}
```

---

## 🔧 PASO 2: UserAvatar

```typescript
// filepath: src/components/user/UserAvatar.tsx
import Image from "next/image";
import { User } from "lucide-react";
import { cn } from "@/lib/utils";

interface UserAvatarProps {
  src?: string | null;
  name?: string;
  size?: "xs" | "sm" | "md" | "lg" | "xl";
  className?: string;
}

const sizes = {
  xs: "w-6 h-6 text-xs",
  sm: "w-8 h-8 text-sm",
  md: "w-10 h-10 text-base",
  lg: "w-12 h-12 text-lg",
  xl: "w-16 h-16 text-xl",
};

const imageSizes = {
  xs: 24,
  sm: 32,
  md: 40,
  lg: 48,
  xl: 64,
};

export function UserAvatar({
  src,
  name,
  size = "md",
  className,
}: UserAvatarProps) {
  const initials = name
    ?.split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <div
      className={cn(
        "relative rounded-full overflow-hidden bg-gray-100 flex items-center justify-center",
        sizes[size],
        className
      )}
    >
      {src ? (
        <Image
          src={src}
          alt={name || "Avatar"}
          fill
          sizes={`${imageSizes[size]}px`}
          className="object-cover"
        />
      ) : initials ? (
        <span className="font-medium text-gray-600">{initials}</span>
      ) : (
        <User className="w-1/2 h-1/2 text-gray-400" />
      )}
    </div>
  );
}
```

---

## 🔧 PASO 3: UserMenu

```typescript
// filepath: src/components/user/UserMenu.tsx
"use client";

import Link from "next/link";
import { User, Settings, Heart, Car, LogOut, ChevronDown } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/DropdownMenu";
import { UserAvatar } from "./UserAvatar";
import { useAuth } from "@/lib/hooks/useAuth";

export function UserMenu() {
  const { user, logout, isDealer } = useAuth();

  if (!user) return null;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger className="flex items-center gap-2 p-1 rounded-full hover:bg-gray-100 transition-colors">
        <UserAvatar src={user.image} name={user.name} size="sm" />
        <span className="hidden md:block text-sm font-medium max-w-[120px] truncate">
          {user.name}
        </span>
        <ChevronDown size={16} className="text-gray-400" />
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-56">
        {/* User info */}
        <div className="px-3 py-2">
          <p className="font-medium text-gray-900">{user.name}</p>
          <p className="text-sm text-gray-500 truncate">{user.email}</p>
        </div>

        <DropdownMenuSeparator />

        {/* Links */}
        <DropdownMenuItem asChild>
          <Link href="/dashboard">
            <User size={16} className="mr-2" />
            Mi cuenta
          </Link>
        </DropdownMenuItem>

        <DropdownMenuItem asChild>
          <Link href="/favoritos">
            <Heart size={16} className="mr-2" />
            Favoritos
          </Link>
        </DropdownMenuItem>

        <DropdownMenuItem asChild>
          <Link href="/mis-vehiculos">
            <Car size={16} className="mr-2" />
            Mis vehículos
          </Link>
        </DropdownMenuItem>

        {isDealer && (
          <DropdownMenuItem asChild>
            <Link href="/dealer/dashboard">
              <Settings size={16} className="mr-2" />
              Panel dealer
            </Link>
          </DropdownMenuItem>
        )}

        <DropdownMenuItem asChild>
          <Link href="/configuracion">
            <Settings size={16} className="mr-2" />
            Configuración
          </Link>
        </DropdownMenuItem>

        <DropdownMenuSeparator />

        <DropdownMenuItem onClick={logout} className="text-red-600">
          <LogOut size={16} className="mr-2" />
          Cerrar sesión
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
```

---

## 🔧 PASO 4: UserProfileCard

```typescript
// filepath: src/components/user/UserProfileCard.tsx
"use client";

import { Camera, Mail, Phone, MapPin, Calendar, Check } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { UserAvatar } from "./UserAvatar";
import { formatDate } from "@/lib/utils";
import type { User } from "@/types";

interface UserProfileCardProps {
  user: User;
  onEditAvatar?: () => void;
}

export function UserProfileCard({ user, onEditAvatar }: UserProfileCardProps) {
  return (
    <div className="bg-white rounded-xl border p-6">
      {/* Avatar section */}
      <div className="flex flex-col items-center text-center">
        <div className="relative">
          <UserAvatar src={user.avatar} name={user.fullName} size="xl" />
          {onEditAvatar && (
            <button
              onClick={onEditAvatar}
              className="absolute bottom-0 right-0 p-1.5 bg-primary-600 text-white rounded-full hover:bg-primary-700"
            >
              <Camera size={14} />
            </button>
          )}
        </div>

        <h2 className="mt-4 text-xl font-semibold text-gray-900">
          {user.fullName}
        </h2>

        <div className="flex items-center gap-2 mt-1">
          {user.isVerified && (
            <Badge variant="success" size="sm">
              <Check size={10} className="mr-1" />
              Verificado
            </Badge>
          )}
          <Badge variant="secondary" size="sm">
            {user.role === "dealer" ? "Dealer" : "Usuario"}
          </Badge>
        </div>
      </div>

      {/* Info */}
      <div className="mt-6 space-y-3">
        <div className="flex items-center gap-3 text-sm">
          <Mail size={16} className="text-gray-400" />
          <span className="text-gray-600">{user.email}</span>
        </div>

        {user.phone && (
          <div className="flex items-center gap-3 text-sm">
            <Phone size={16} className="text-gray-400" />
            <span className="text-gray-600">{user.phone}</span>
          </div>
        )}

        <div className="flex items-center gap-3 text-sm">
          <Calendar size={16} className="text-gray-400" />
          <span className="text-gray-600">
            Miembro desde {formatDate(user.createdAt)}
          </span>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: UserSettingsForm

```typescript
// filepath: src/components/user/UserSettingsForm.tsx
"use client";

import * as React from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Switch } from "@/components/ui/Switch";
import { FormField } from "@/components/ui/FormField";
import { showToast } from "@/lib/toast";
import { userService } from "@/lib/services/userService";
import type { User, UserPreferences } from "@/types";

const settingsSchema = z.object({
  fullName: z.string().min(2, "Nombre requerido"),
  phone: z.string().optional(),
  emailNotifications: z.boolean(),
  smsNotifications: z.boolean(),
  newsletter: z.boolean(),
});

type SettingsFormData = z.infer<typeof settingsSchema>;

interface UserSettingsFormProps {
  user: User;
  onSuccess?: () => void;
}

export function UserSettingsForm({ user, onSuccess }: UserSettingsFormProps) {
  const form = useForm<SettingsFormData>({
    resolver: zodResolver(settingsSchema),
    defaultValues: {
      fullName: user.fullName,
      phone: user.phone ?? "",
      emailNotifications: user.preferences?.emailNotifications ?? true,
      smsNotifications: user.preferences?.smsNotifications ?? false,
      newsletter: user.preferences?.newsletter ?? false,
    },
  });

  const onSubmit = async (data: SettingsFormData) => {
    try {
      await userService.updateProfile(user.id, {
        fullName: data.fullName,
        phone: data.phone,
        preferences: {
          emailNotifications: data.emailNotifications,
          smsNotifications: data.smsNotifications,
          newsletter: data.newsletter,
        },
      });

      showToast.success("Configuración actualizada");
      onSuccess?.();
    } catch {
      showToast.error("Error al guardar cambios");
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
      {/* Profile section */}
      <div className="space-y-4">
        <h3 className="font-semibold text-gray-900">Información personal</h3>

        <FormField
          label="Nombre completo"
          error={form.formState.errors.fullName?.message}
        >
          <Input {...form.register("fullName")} />
        </FormField>

        <FormField label="Teléfono">
          <Input type="tel" {...form.register("phone")} />
        </FormField>
      </div>

      {/* Notifications section */}
      <div className="space-y-4 pt-6 border-t">
        <h3 className="font-semibold text-gray-900">Notificaciones</h3>

        <div className="flex items-center justify-between">
          <div>
            <p className="font-medium text-gray-700">Email</p>
            <p className="text-sm text-gray-500">
              Recibir actualizaciones por email
            </p>
          </div>
          <Switch
            checked={form.watch("emailNotifications")}
            onCheckedChange={(v) => form.setValue("emailNotifications", v)}
          />
        </div>

        <div className="flex items-center justify-between">
          <div>
            <p className="font-medium text-gray-700">SMS</p>
            <p className="text-sm text-gray-500">
              Recibir alertas por SMS
            </p>
          </div>
          <Switch
            checked={form.watch("smsNotifications")}
            onCheckedChange={(v) => form.setValue("smsNotifications", v)}
          />
        </div>

        <div className="flex items-center justify-between">
          <div>
            <p className="font-medium text-gray-700">Newsletter</p>
            <p className="text-sm text-gray-500">
              Ofertas y novedades semanales
            </p>
          </div>
          <Switch
            checked={form.watch("newsletter")}
            onCheckedChange={(v) => form.setValue("newsletter", v)}
          />
        </div>
      </div>

      <Button
        type="submit"
        className="w-full"
        isLoading={form.formState.isSubmitting}
      >
        Guardar cambios
      </Button>
    </form>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - UserAvatar muestra imagen o iniciales
# - UserMenu dropdown funciona
# - UserSettingsForm guarda cambios
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/04-publicar.md`
