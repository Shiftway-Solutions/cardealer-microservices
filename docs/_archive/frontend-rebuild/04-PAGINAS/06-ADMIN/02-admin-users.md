---
title: "👥 Admin - Gestión de Usuarios"
priority: P2
estimated_time: "40 minutos"
dependencies: []
apis: ["AuthService", "UserService", "AdminService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 👥 Admin - Gestión de Usuarios

> **Tiempo estimado:** 40 minutos
> **Prerrequisitos:** Admin layout, UserService
> **Roles:** ADM-ADMIN, ADM-SUPER
> **Dependencias:** AdminService (Puerto 5011), AuthService, RoleService

---

## ✅ INTEGRACIÓN CON ADMINSERVICE

Este documento complementa:

- [12-admin-dashboard.md](./01-admin-dashboard.md) - Dashboard administrativo
- [17-admin-system.md](./06-admin-system.md) - Configuración del sistema
- [process-matrix/12-ADMINISTRACION/02-admin-users.md](../../process-matrix/12-ADMINISTRACION/02-admin-users.md) - **Procesos detallados** ⭐

**Estado:** ✅ Backend 80% | 🟡 UI 60%

### Servicios Involucrados

| Servicio            | Puerto | Función           | Estado  |
| ------------------- | ------ | ----------------- | ------- |
| AdminService        | 5011   | Gestión de admins | ✅ 80%  |
| AuthService         | 5001   | Autenticación     | ✅ 100% |
| RoleService         | 5003   | Roles y permisos  | ✅ 100% |
| NotificationService | 5006   | Notificaciones    | ✅ 100% |
| AuditService        | 5091   | Log de acciones   | 🟡 70%  |

### Endpoints de AdminService

| Método   | Endpoint                               | Descripción              | Rol        |
| -------- | -------------------------------------- | ------------------------ | ---------- |
| `GET`    | `/api/admin/users`                     | Listar usuarios admin    | Admin      |
| `GET`    | `/api/admin/users/{id}`                | Detalle de usuario admin | Admin      |
| `POST`   | `/api/admin/users`                     | Crear usuario admin      | SuperAdmin |
| `PUT`    | `/api/admin/users/{id}`                | Actualizar usuario admin | SuperAdmin |
| `DELETE` | `/api/admin/users/{id}`                | Desactivar usuario admin | SuperAdmin |
| `POST`   | `/api/admin/users/{id}/roles`          | Asignar roles            | SuperAdmin |
| `POST`   | `/api/admin/users/{id}/reset-password` | Reset password           | SuperAdmin |
| `POST`   | `/api/admin/users/{id}/2fa/enable`     | Habilitar 2FA            | SuperAdmin |
| `GET`    | `/api/admin/users/{id}/sessions`       | Ver sesiones activas     | SuperAdmin |
| `GET`    | `/api/admin/users/{id}/activity`       | Log de actividad         | SuperAdmin |

### Procesos del Backend

| Proceso      | Nombre                       | Pasos | Archivo process-matrix |
| ------------ | ---------------------------- | ----- | ---------------------- |
| ADM-USER-001 | Crear Usuario Administrativo | 10    | 02-admin-users.md      |
| ADM-USER-002 | Asignar Roles a Usuario      | 8     | 02-admin-users.md      |
| ADM-USER-003 | Suspender Usuario Admin      | 7     | 02-admin-users.md      |
| ADM-USER-004 | Habilitar 2FA                | 6     | 02-admin-users.md      |
| ADM-USER-005 | Gestionar Sesiones           | 5     | 02-admin-users.md      |

### Entidades del Backend

```csharp
// AdminService/AdminService.Domain/Entities/AdminUser.cs
public class AdminUser
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } // FK a AuthService.Users
    public string Email { get; set; }
    public string FullName { get; set; }
    public AdminUserStatus Status { get; set; } // Active, Inactive, Locked
    public bool IsTwoFactorEnabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public ICollection<AdminUserRole> Roles { get; set; }
    public ICollection<AdminSession> Sessions { get; set; }
}

public class AdminSession
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string SessionToken { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsRevoked { get; set; }
}

public class AdminActivityLog
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public AdminAction Action { get; set; } // Login, CreateUser, UpdateUser, etc.
    public string ResourceType { get; set; } // User, Vehicle, Dealer
```

---

## 🎨 WIREFRAME - LISTA DE USUARIOS ADMIN

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  GESTIÓN DE USUARIOS                      [+ Nuevo Usuario]   │
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 👥 Users◀│  ┌──────────────────────────────────────────────────────────┐ │
│ │ 🛡️ Mod   │  │ 🔍 Buscar usuario...          [Rol ▼] [Estado ▼] [Filtrar]│ │
│ │ ⚙️ System│  └──────────────────────────────────────────────────────────┘ │
│ │          │                                                                │
│ │          │  ┌──────────────────────────────────────────────────────────┐ │
│ │          │  │ ☑️ │ Usuario          │ Email              │ Rol    │ Estado│
│ │          │  │────┼──────────────────┼────────────────────┼────────┼──────│
│ │          │  │ ☐ │ 👤 Juan Pérez    │ juan@okla.do       │ Admin  │ 🟢   │
│ │          │  │ ☐ │ 👤 María García  │ maria@okla.do      │ Mod    │ 🟢   │
│ │          │  │ ☐ │ 👤 Pedro López   │ pedro@okla.do      │ Support│ 🟡   │
│ │          │  │ ☐ │ 👤 Ana Martínez  │ ana@okla.do        │ Super  │ 🟢   │
│ │          │  │ ☐ │ 👤 Carlos Ruiz   │ carlos@okla.do     │ Comp   │ 🔴   │
│ │          │  └──────────────────────────────────────────────────────────┘ │
│ │          │                                                                │
│ │          │  Mostrando 1-5 de 12           [← Anterior] [1] [2] [3] [→]   │
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - DETALLE/EDITAR USUARIO ADMIN

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ← Volver a Lista                                         [Guardar Cambios]  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ INFORMACIÓN DEL USUARIO                                                 │ │
│  │                                                                         │ │
│  │  ┌──────────┐  Juan Pérez                                               │ │
│  │  │  AVATAR  │  juan@okla.do                                             │ │
│  │  │   80px   │  Último acceso: Hace 2 horas • IP: 192.168.1.1           │ │
│  │  └──────────┘  Miembro desde: Enero 2024 • 2FA: ✅ Activo               │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ ROLES ASIGNADOS                                     [+ Asignar Rol]     │ │
│  │                                                                         │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │ │
│  │  │ ADM-ADMIN    ✕ │  │ ADM-MOD      ✕ │  │ ADM-COMP     ✕ │          │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘          │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ SESIONES ACTIVAS                                    [Cerrar Todas]      │ │
│  │                                                                         │ │
│  │  🖥️ Chrome/Windows • 192.168.1.1 • Hace 5 min              [Cerrar]    │ │
│  │  📱 Safari/iOS     • 192.168.1.50 • Hace 2 horas           [Cerrar]    │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ ACCIONES                                                                │ │
│  │                                                                         │ │
│  │  [🔑 Reset Password]  [🔐 Toggle 2FA]  [⏸️ Suspender]  [🗑️ Desactivar] │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
    public string? ResourceId { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 📋 OBJETIVO

Implementar gestión de usuarios administrativos:

- Lista paginada con filtros
- Crear y editar usuarios admin
- Asignar roles y permisos
- Gestionar sesiones activas
- Log de actividad
- Habilitar/deshabilitar 2FA
- Suspender/Activar usuarios

---

## 🔧 PASO 1: Lista de Usuarios

```typescript
// filepath: src/app/(admin)/admin/usuarios/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { UsersHeader } from "@/components/admin/users/UsersHeader";
import { UsersFilters } from "@/components/admin/users/UsersFilters";
import { UsersTable } from "@/components/admin/users/UsersTable";
import { LoadingTable } from "@/components/ui/LoadingTable";

export const metadata: Metadata = {
  title: "Usuarios | Admin OKLA",
};

interface Props {
  searchParams: Promise<{
    search?: string;
    role?: string;
    status?: string;
    page?: string;
  }>;
}

export default async function UsersPage({ searchParams }: Props) {
  const params = await searchParams;

  return (
    <div className="space-y-6">
      <UsersHeader />
      <UsersFilters />
      <Suspense fallback={<LoadingTable rows={10} cols={6} />}>
        <UsersTable
          search={params.search}
          role={params.role}
          status={params.status}
          page={Number(params.page) || 1}
        />
      </Suspense>
    </div>
  );
}
```

---

## 🔧 PASO 2: UsersTable

```typescript
// filepath: src/components/admin/users/UsersTable.tsx
import Link from "next/link";
import { MoreHorizontal, Eye, Edit, Ban, Shield } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Avatar } from "@/components/ui/Avatar";
import { DropdownMenu } from "@/components/ui/DropdownMenu";
import { Pagination } from "@/components/ui/Pagination";
import { adminService } from "@/lib/services/adminService";
import { formatDate } from "@/lib/utils";

interface Props {
  search?: string;
  role?: string;
  status?: string;
  page: number;
}

const roleColors = {
  "USR-ANON": "default",
  "USR-REG": "info",
  "USR-SELLER": "warning",
  "DLR-STAFF": "purple",
  "DLR-ADMIN": "purple",
  "ADM-SUPPORT": "success",
  "ADM-MOD": "success",
  "ADM-COMP": "success",
  "ADM-ADMIN": "success",
  "ADM-SUPER": "danger",
} as const;

const statusColors = {
  Active: "success",
  Suspended: "danger",
  Pending: "warning",
} as const;

export async function UsersTable({ search, role, status, page }: Props) {
  const data = await adminService.getUsers({ search, role, status, page, pageSize: 20 });

  return (
    <div className="bg-white rounded-xl border">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Usuario</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Email</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Rol</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Estado</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Registro</th>
              <th className="p-4 w-12"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {data.items.map((user) => (
              <tr key={user.id} className="hover:bg-gray-50">
                <td className="p-4">
                  <div className="flex items-center gap-3">
                    <Avatar src={user.avatar} name={user.fullName} size="sm" />
                    <div>
                      <p className="font-medium text-gray-900">{user.fullName}</p>
                      <p className="text-sm text-gray-500">@{user.username}</p>
                    </div>
                  </div>
                </td>
                <td className="p-4 text-gray-600">{user.email}</td>
                <td className="p-4">
                  <Badge variant={roleColors[user.role as keyof typeof roleColors]}>
                    {user.role}
                  </Badge>
                </td>
                <td className="p-4">
                  <Badge variant={statusColors[user.status as keyof typeof statusColors]}>
                    {user.status}
                  </Badge>
                </td>
                <td className="p-4 text-sm text-gray-500">{formatDate(user.createdAt)}</td>
                <td className="p-4">
                  <DropdownMenu
                    trigger={
                      <button className="p-2 hover:bg-gray-100 rounded-lg">
                        <MoreHorizontal size={16} />
                      </button>
                    }
                    items={[
                      { icon: Eye, label: "Ver perfil", href: `/admin/usuarios/${user.id}` },
                      { icon: Edit, label: "Editar", href: `/admin/usuarios/${user.id}/editar` },
                      { icon: Shield, label: "Cambiar rol", onClick: () => {} },
                      { icon: Ban, label: user.status === "Active" ? "Suspender" : "Activar", onClick: () => {} },
                    ]}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="p-4 border-t">
        <Pagination
          currentPage={page}
          totalPages={Math.ceil(data.totalCount / 20)}
          baseUrl="/admin/usuarios"
        />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: Detalle de Usuario

```typescript
// filepath: src/app/(admin)/admin/usuarios/[id]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { UserProfile } from "@/components/admin/users/UserProfile";
import { UserActivity } from "@/components/admin/users/UserActivity";
import { UserListings } from "@/components/admin/users/UserListings";
import { UserActions } from "@/components/admin/users/UserActions";
import { adminService } from "@/lib/services/adminService";

interface Props {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const user = await adminService.getUserById(id);
  return { title: user ? `${user.fullName} | Admin` : "Usuario no encontrado" };
}

export default async function UserDetailPage({ params }: Props) {
  const { id } = await params;
  const user = await adminService.getUserById(id);

  if (!user) notFound();

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Profile Card */}
        <div className="lg:col-span-1">
          <UserProfile user={user} />
          <UserActions user={user} className="mt-4" />
        </div>

        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          <UserActivity userId={user.id} />
          <UserListings userId={user.id} />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: UserProfile Card

```typescript
// filepath: src/components/admin/users/UserProfile.tsx
import { Mail, Phone, Calendar, MapPin, Shield } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { formatDate } from "@/lib/utils";
import type { User } from "@/types";

interface Props {
  user: User;
}

export function UserProfile({ user }: Props) {
  return (
    <div className="bg-white rounded-xl border p-6">
      <div className="text-center mb-6">
        <Avatar src={user.avatar} name={user.fullName} size="xl" className="mx-auto mb-4" />
        <h2 className="text-xl font-bold text-gray-900">{user.fullName}</h2>
        <p className="text-gray-500">@{user.username}</p>
        <div className="flex items-center justify-center gap-2 mt-2">
          <Badge>{user.role}</Badge>
          <Badge variant={user.status === "Active" ? "success" : "danger"}>
            {user.status}
          </Badge>
        </div>
      </div>

      <div className="space-y-3 text-sm">
        <div className="flex items-center gap-3 text-gray-600">
          <Mail size={16} className="text-gray-400" />
          {user.email}
        </div>

        {user.phone && (
          <div className="flex items-center gap-3 text-gray-600">
            <Phone size={16} className="text-gray-400" />
            {user.phone}
          </div>
        )}

        {user.location && (
          <div className="flex items-center gap-3 text-gray-600">
            <MapPin size={16} className="text-gray-400" />
            {user.location}
          </div>
        )}

        <div className="flex items-center gap-3 text-gray-600">
          <Calendar size={16} className="text-gray-400" />
          Miembro desde {formatDate(user.createdAt)}
        </div>

        {user.emailVerified && (
          <div className="flex items-center gap-3 text-green-600">
            <Shield size={16} />
            Email verificado
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: UserActions

```typescript
// filepath: src/components/admin/users/UserActions.tsx
"use client";

import { useState } from "react";
import { Shield, Ban, Key, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { Dialog } from "@/components/ui/Dialog";
import { useAdminUserActions } from "@/lib/hooks/useAdminUserActions";
import { cn } from "@/lib/utils";
import type { User } from "@/types";

interface Props {
  user: User;
  className?: string;
}

const roles = [
  { value: "USR-REG", label: "Usuario Registrado" },
  { value: "USR-SELLER", label: "Vendedor" },
  { value: "DLR-STAFF", label: "Staff Dealer" },
  { value: "DLR-ADMIN", label: "Admin Dealer" },
  { value: "ADM-SUPPORT", label: "Soporte" },
  { value: "ADM-MOD", label: "Moderador" },
  { value: "ADM-COMP", label: "Compliance" },
  { value: "ADM-ADMIN", label: "Administrador" },
];

export function UserActions({ user, className }: Props) {
  const [showRoleDialog, setShowRoleDialog] = useState(false);
  const [newRole, setNewRole] = useState(user.role);
  const { changeRole, toggleStatus, resetPassword, isLoading } = useAdminUserActions(user.id);

  return (
    <div className={cn("bg-white rounded-xl border p-4 space-y-3", className)}>
      <h3 className="font-medium text-gray-900 mb-4">Acciones</h3>

      <Button
        variant="outline"
        className="w-full justify-start"
        onClick={() => setShowRoleDialog(true)}
      >
        <Shield size={16} className="mr-2" />
        Cambiar rol
      </Button>

      <Button
        variant="outline"
        className="w-full justify-start"
        onClick={() => toggleStatus()}
        disabled={isLoading}
      >
        <Ban size={16} className="mr-2" />
        {user.status === "Active" ? "Suspender usuario" : "Activar usuario"}
      </Button>

      <Button
        variant="outline"
        className="w-full justify-start"
        onClick={() => resetPassword()}
        disabled={isLoading}
      >
        <Key size={16} className="mr-2" />
        Enviar reset de contraseña
      </Button>

      {/* Role Dialog */}
      <Dialog open={showRoleDialog} onOpenChange={setShowRoleDialog}>
        <Dialog.Content>
          <Dialog.Header>
            <Dialog.Title>Cambiar rol de {user.fullName}</Dialog.Title>
          </Dialog.Header>

          <div className="py-4">
            <Select value={newRole} onChange={(e) => setNewRole(e.target.value)}>
              {roles.map((role) => (
                <option key={role.value} value={role.value}>
                  {role.label}
                </option>
              ))}
            </Select>
          </div>

          <Dialog.Footer>
            <Button variant="outline" onClick={() => setShowRoleDialog(false)}>
              Cancelar
            </Button>
            <Button
              onClick={() => {
                changeRole(newRole);
                setShowRoleDialog(false);
              }}
            >
              Guardar cambios
            </Button>
          </Dialog.Footer>
        </Dialog.Content>
      </Dialog>
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /admin/usuarios muestra tabla
# - Filtros funcionan
# - Detalle carga correctamente
# - Acciones funcionan
```

---

## 🔧 PASO 6: Formulario Crear/Editar Usuario

```typescript
// filepath: src/components/admin/users/UserForm.tsx
"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { Switch } from "@/components/ui/Switch";
import { Dialog } from "@/components/ui/Dialog";
import { adminUsersService } from "@/lib/services/adminUsersService";
import { toast } from "sonner";
import type { AdminUser, AdminRole } from "@/types";

const userSchema = z.object({
  email: z.string().email("Email inválido"),
  fullName: z.string().min(2, "Mínimo 2 caracteres"),
  roleId: z.string().min(1, "Selecciona un rol"),
  isTwoFactorRequired: z.boolean(),
});

type UserFormData = z.infer<typeof userSchema>;

interface UserFormProps {
  user?: AdminUser;
  roles: AdminRole[];
  onSuccess: () => void;
  onCancel: () => void;
}

export function UserForm({ user, roles, onSuccess, onCancel }: UserFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const isEditing = !!user;

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<UserFormData>({
    resolver: zodResolver(userSchema),
    defaultValues: {
      email: user?.email || "",
      fullName: user?.fullName || "",
      roleId: user?.roles?.[0]?.id || "",
      isTwoFactorRequired: user?.isTwoFactorEnabled || false,
    },
  });

  const isTwoFactorRequired = watch("isTwoFactorRequired");

  const onSubmit = async (data: UserFormData) => {
    setIsLoading(true);
    try {
      if (isEditing) {
        await adminUsersService.update(user.id, data);
        toast.success("Usuario actualizado correctamente");
      } else {
        await adminUsersService.create(data);
        toast.success("Usuario creado. Se envió email con credenciales.");
      }
      onSuccess();
    } catch (error) {
      toast.error(isEditing ? "Error al actualizar" : "Error al crear usuario");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Email
        </label>
        <Input
          type="email"
          {...register("email")}
          disabled={isEditing}
          placeholder="usuario@okla.com.do"
        />
        {errors.email && (
          <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Nombre completo
        </label>
        <Input {...register("fullName")} placeholder="Juan Pérez" />
        {errors.fullName && (
          <p className="text-red-500 text-sm mt-1">{errors.fullName.message}</p>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Rol
        </label>
        <Select {...register("roleId")}>
          <option value="">Seleccionar rol...</option>
          {roles.map((role) => (
            <option key={role.id} value={role.id}>
              {role.name} - {role.description}
            </option>
          ))}
        </Select>
        {errors.roleId && (
          <p className="text-red-500 text-sm mt-1">{errors.roleId.message}</p>
        )}
      </div>

      <div className="flex items-center justify-between py-2">
        <div>
          <p className="font-medium text-gray-900">Requerir 2FA</p>
          <p className="text-sm text-gray-500">
            El usuario deberá configurar autenticación de dos factores
          </p>
        </div>
        <Switch
          checked={isTwoFactorRequired}
          onCheckedChange={(checked) => setValue("isTwoFactorRequired", checked)}
        />
      </div>

      <div className="flex gap-3 pt-4 border-t">
        <Button type="button" variant="outline" onClick={onCancel} className="flex-1">
          Cancelar
        </Button>
        <Button type="submit" disabled={isLoading} className="flex-1">
          {isLoading ? "Guardando..." : isEditing ? "Guardar cambios" : "Crear usuario"}
        </Button>
      </div>
    </form>
  );
}
```

---

## 🔧 PASO 7: Servicio de Usuarios Admin

```typescript
// filepath: src/lib/services/adminUsersService.ts
import { apiClient } from "@/lib/apiClient";
import type { AdminUser, PaginatedResponse, AdminRole } from "@/types";

interface GetUsersParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: "Active" | "Inactive" | "Locked";
  roleId?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

interface CreateUserDto {
  email: string;
  fullName: string;
  roleId: string;
  isTwoFactorRequired: boolean;
}

interface UpdateUserDto {
  fullName?: string;
  roleId?: string;
  isTwoFactorRequired?: boolean;
}

interface UserStats {
  total: number;
  active: number;
  inactive: number;
  locked: number;
  newThisMonth: number;
}

interface SessionInfo {
  id: string;
  ipAddress: string;
  userAgent: string;
  createdAt: string;
  lastActivityAt: string;
  isCurrent: boolean;
}

interface ActivityLog {
  id: string;
  action: string;
  resourceType: string;
  resourceId?: string;
  details?: string;
  ipAddress: string;
  createdAt: string;
}

export const adminUsersService = {
  // Listar usuarios con paginación
  async getUsers(
    params: GetUsersParams = {},
  ): Promise<PaginatedResponse<AdminUser>> {
    const searchParams = new URLSearchParams();
    if (params.page) searchParams.set("page", params.page.toString());
    if (params.pageSize)
      searchParams.set("pageSize", params.pageSize.toString());
    if (params.search) searchParams.set("search", params.search);
    if (params.status) searchParams.set("status", params.status);
    if (params.roleId) searchParams.set("roleId", params.roleId);
    if (params.sortBy) searchParams.set("sortBy", params.sortBy);
    if (params.sortOrder) searchParams.set("sortOrder", params.sortOrder);

    return apiClient.get(`/admin/users?${searchParams.toString()}`);
  },

  // Obtener usuario por ID
  async getById(id: string): Promise<AdminUser> {
    return apiClient.get(`/admin/users/${id}`);
  },

  // Crear usuario
  async create(data: CreateUserDto): Promise<AdminUser> {
    return apiClient.post("/admin/users", data);
  },

  // Actualizar usuario
  async update(id: string, data: UpdateUserDto): Promise<AdminUser> {
    return apiClient.put(`/admin/users/${id}`, data);
  },

  // Cambiar estado (activar/desactivar)
  async toggleStatus(id: string): Promise<AdminUser> {
    return apiClient.post(`/admin/users/${id}/toggle-status`);
  },

  // Enviar reset de contraseña
  async resetPassword(id: string): Promise<void> {
    return apiClient.post(`/admin/users/${id}/reset-password`);
  },

  // Forzar 2FA
  async enableTwoFactor(id: string): Promise<void> {
    return apiClient.post(`/admin/users/${id}/2fa/enable`);
  },

  // Deshabilitar 2FA
  async disableTwoFactor(id: string): Promise<void> {
    return apiClient.post(`/admin/users/${id}/2fa/disable`);
  },

  // Obtener sesiones activas
  async getSessions(id: string): Promise<SessionInfo[]> {
    return apiClient.get(`/admin/users/${id}/sessions`);
  },

  // Revocar sesión específica
  async revokeSession(userId: string, sessionId: string): Promise<void> {
    return apiClient.delete(`/admin/users/${userId}/sessions/${sessionId}`);
  },

  // Revocar todas las sesiones
  async revokeAllSessions(userId: string): Promise<void> {
    return apiClient.delete(`/admin/users/${userId}/sessions`);
  },

  // Obtener log de actividad
  async getActivityLog(
    id: string,
    page = 1,
  ): Promise<PaginatedResponse<ActivityLog>> {
    return apiClient.get(`/admin/users/${id}/activity?page=${page}`);
  },

  // Obtener estadísticas
  async getStats(): Promise<UserStats> {
    return apiClient.get("/admin/users/stats");
  },

  // Obtener roles disponibles
  async getRoles(): Promise<AdminRole[]> {
    return apiClient.get("/admin/roles");
  },

  // Exportar usuarios a CSV
  async exportToCsv(params: GetUsersParams = {}): Promise<Blob> {
    const searchParams = new URLSearchParams();
    if (params.status) searchParams.set("status", params.status);
    if (params.roleId) searchParams.set("roleId", params.roleId);

    return apiClient.get(`/admin/users/export?${searchParams.toString()}`, {
      responseType: "blob",
    });
  },
};
```

---

## 🎨 Estados de UI

### Loading States

```typescript
// Skeleton para tabla de usuarios
export function UsersTableSkeleton() {
  return (
    <div className="space-y-4">
      {/* Header skeleton */}
      <div className="flex justify-between items-center">
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-10 w-32" />
      </div>

      {/* Table skeleton */}
      <div className="bg-white rounded-xl border overflow-hidden">
        <div className="p-4 border-b bg-gray-50">
          <div className="grid grid-cols-5 gap-4">
            {[1, 2, 3, 4, 5].map((i) => (
              <Skeleton key={i} className="h-4" />
            ))}
          </div>
        </div>
        {[1, 2, 3, 4, 5].map((row) => (
          <div key={row} className="p-4 border-b">
            <div className="grid grid-cols-5 gap-4">
              {[1, 2, 3, 4, 5].map((i) => (
                <Skeleton key={i} className="h-4" />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

### Empty State

```typescript
// Estado vacío
export function UsersEmptyState({ onCreateClick }: { onCreateClick: () => void }) {
  return (
    <div className="text-center py-12 bg-white rounded-xl border">
      <Users size={48} className="mx-auto text-gray-300 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        No hay usuarios administrativos
      </h3>
      <p className="text-gray-500 mb-4 max-w-sm mx-auto">
        Crea el primer usuario administrador para comenzar a gestionar la plataforma.
      </p>
      <Button onClick={onCreateClick}>
        <Plus size={16} className="mr-2" />
        Crear primer usuario
      </Button>
    </div>
  );
}
```

### Error State

```typescript
// Estado de error
export function UsersErrorState({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="text-center py-12 bg-white rounded-xl border">
      <AlertCircle size={48} className="mx-auto text-red-400 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        Error al cargar usuarios
      </h3>
      <p className="text-gray-500 mb-4">
        Ocurrió un problema al obtener la lista de usuarios.
      </p>
      <Button variant="outline" onClick={onRetry}>
        <RefreshCw size={16} className="mr-2" />
        Reintentar
      </Button>
    </div>
  );
}
```

---

## 🧪 Testing E2E

```typescript
// filepath: e2e/admin/users.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Users Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page, "superadmin");
  });

  test("should display users table", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await expect(page.getByRole("heading", { name: "Usuarios" })).toBeVisible();
    await expect(page.getByRole("table")).toBeVisible();
  });

  test("should filter users by status", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByRole("combobox", { name: "Estado" }).click();
    await page.getByRole("option", { name: "Activo" }).click();

    await expect(page).toHaveURL(/status=Active/);
  });

  test("should search users", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByPlaceholder("Buscar por nombre o email").fill("admin");
    await page.keyboard.press("Enter");

    await expect(page).toHaveURL(/search=admin/);
  });

  test("should open user detail", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByRole("row").nth(1).click();

    await expect(page.getByTestId("user-detail")).toBeVisible();
  });

  test("should create new user (SuperAdmin only)", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByRole("button", { name: "Nuevo usuario" }).click();

    await page.getByLabel("Email").fill("nuevo@okla.com.do");
    await page.getByLabel("Nombre completo").fill("Usuario Nuevo");
    await page.getByRole("combobox", { name: "Rol" }).click();
    await page.getByRole("option", { name: "Moderador" }).click();

    await page.getByRole("button", { name: "Crear usuario" }).click();

    await expect(page.getByText("Usuario creado")).toBeVisible();
  });

  test("should toggle user status", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByRole("row").nth(1).click();
    await page.getByRole("button", { name: /Suspender|Activar/ }).click();

    await expect(
      page.getByText(/usuario activado|usuario suspendido/i),
    ).toBeVisible();
  });

  test("should reset user password", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByRole("row").nth(1).click();
    await page
      .getByRole("button", { name: "Enviar reset de contraseña" })
      .click();

    await expect(page.getByText("Email enviado")).toBeVisible();
  });

  test("should revoke all sessions", async ({ page }) => {
    await page.goto("/admin/usuarios");

    await page.getByRole("row").nth(1).click();
    await page.getByRole("tab", { name: "Sesiones" }).click();
    await page
      .getByRole("button", { name: "Cerrar todas las sesiones" })
      .click();
    await page.getByRole("button", { name: "Confirmar" }).click();

    await expect(page.getByText("Sesiones cerradas")).toBeVisible();
  });
});
```

---

## 📊 Métricas y Analytics

```typescript
// filepath: src/lib/analytics/adminUsersEvents.ts
import { analytics } from "@/lib/analytics";

export const adminUsersEvents = {
  // Eventos de visualización
  viewUsersList: () => {
    analytics.track("admin_users_list_viewed");
  },

  viewUserDetail: (userId: string) => {
    analytics.track("admin_user_detail_viewed", { userId });
  },

  // Eventos de acción
  createUser: (roleId: string) => {
    analytics.track("admin_user_created", { roleId });
  },

  updateUser: (userId: string, changes: string[]) => {
    analytics.track("admin_user_updated", { userId, changes });
  },

  toggleUserStatus: (userId: string, newStatus: string) => {
    analytics.track("admin_user_status_changed", { userId, newStatus });
  },

  resetPassword: (userId: string) => {
    analytics.track("admin_user_password_reset", { userId });
  },

  revokeSession: (userId: string) => {
    analytics.track("admin_user_session_revoked", { userId });
  },

  // Eventos de búsqueda/filtro
  searchUsers: (query: string, resultsCount: number) => {
    analytics.track("admin_users_searched", { query, resultsCount });
  },

  filterUsers: (filters: Record<string, string>) => {
    analytics.track("admin_users_filtered", filters);
  },

  exportUsers: (format: string, count: number) => {
    analytics.track("admin_users_exported", { format, count });
  },
};
```

---

## 🔐 Permisos y Roles

| Acción                 | ADM-SUPER | ADM-ADMIN | ADM-MOD | ADM-SUPPORT |
| ---------------------- | --------- | --------- | ------- | ----------- |
| Ver lista de usuarios  | ✅        | ✅        | ❌      | ❌          |
| Ver detalle de usuario | ✅        | ✅        | ❌      | ❌          |
| Crear usuario          | ✅        | ❌        | ❌      | ❌          |
| Editar usuario         | ✅        | ❌        | ❌      | ❌          |
| Cambiar estado         | ✅        | ❌        | ❌      | ❌          |
| Reset contraseña       | ✅        | ❌        | ❌      | ❌          |
| Gestionar 2FA          | ✅        | ❌        | ❌      | ❌          |
| Ver sesiones           | ✅        | ✅        | ❌      | ❌          |
| Revocar sesiones       | ✅        | ❌        | ❌      | ❌          |
| Ver log de actividad   | ✅        | ✅        | ❌      | ❌          |
| Exportar usuarios      | ✅        | ✅        | ❌      | ❌          |

---

## ✅ Checklist de Implementación

### Backend (AdminService)

- [ ] Endpoint `GET /api/admin/users` con paginación y filtros
- [ ] Endpoint `GET /api/admin/users/{id}` con detalle completo
- [ ] Endpoint `POST /api/admin/users` crear usuario
- [ ] Endpoint `PUT /api/admin/users/{id}` actualizar usuario
- [ ] Endpoint `POST /api/admin/users/{id}/toggle-status` cambiar estado
- [ ] Endpoint `POST /api/admin/users/{id}/reset-password` enviar reset
- [ ] Endpoint `POST /api/admin/users/{id}/2fa/enable` habilitar 2FA
- [ ] Endpoint `POST /api/admin/users/{id}/2fa/disable` deshabilitar 2FA
- [ ] Endpoint `GET /api/admin/users/{id}/sessions` listar sesiones
- [ ] Endpoint `DELETE /api/admin/users/{id}/sessions/{sessionId}` revocar sesión
- [ ] Endpoint `DELETE /api/admin/users/{id}/sessions` revocar todas
- [ ] Endpoint `GET /api/admin/users/{id}/activity` log de actividad
- [ ] Endpoint `GET /api/admin/users/stats` estadísticas
- [ ] Endpoint `GET /api/admin/users/export` exportar CSV

### Frontend

- [ ] Página `/admin/usuarios` con tabla y filtros
- [ ] Componente `UsersTable` con ordenamiento
- [ ] Componente `UserDetail` panel lateral
- [ ] Componente `UserForm` crear/editar
- [ ] Componente `UserActions` botones de acción
- [ ] Estados: Loading, Empty, Error
- [ ] Servicio `adminUsersService`
- [ ] Tests E2E completos
- [ ] Analytics tracking

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-users.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Users Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar lista de usuarios", async ({ page }) => {
    await page.goto("/admin/users");

    await expect(page.getByTestId("users-table")).toBeVisible();
  });

  test("debe buscar usuario por email", async ({ page }) => {
    await page.goto("/admin/users");

    await page.fill('input[placeholder*="Buscar"]', "test@example.com");
    await page.keyboard.press("Enter");

    await expect(page).toHaveURL(/search=test/);
  });

  test("debe ver detalle de usuario", async ({ page }) => {
    await page.goto("/admin/users");

    await page.getByTestId("user-row").first().click();
    await expect(page.getByTestId("user-detail")).toBeVisible();
  });

  test("debe suspender usuario", async ({ page }) => {
    await page.goto("/admin/users");
    await page.getByTestId("user-row").first().click();

    await page.getByRole("button", { name: /suspender/i }).click();
    await page.fill('textarea[name="reason"]', "Violación de términos");
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/usuario suspendido/i)).toBeVisible();
  });

  test("debe filtrar por rol", async ({ page }) => {
    await page.goto("/admin/users");

    await page.getByRole("combobox", { name: /rol/i }).click();
    await page.getByRole("option", { name: /dealer/i }).click();

    await expect(page).toHaveURL(/role=dealer/);
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: [03-admin-moderation.md](./03-admin-moderation.md)
