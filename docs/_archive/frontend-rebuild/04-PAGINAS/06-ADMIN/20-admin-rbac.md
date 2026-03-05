# 29. Admin RBAC (Roles y Permisos)

**Objetivo:** Panel de administración para gestionar roles, permisos y asignación de usuarios con visualización de matriz de permisos y operaciones en batch.

**Prioridad:** P1 (IMPORTANTE - Backend completo, falta UI)  
**Complejidad:** 🟠 Media (CRUD estándar + Permission Matrix)  
**Dependencias:** RoleService (✅ YA IMPLEMENTADO - 41 tests pasando)

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

### Sistema RBAC (Role-Based Access Control)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          RBAC HIERARCHY                                    │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  USERS                                                                     │
│  ├─ Admin (userId: "xxx")                                                 │
│  │   └─ Roles: [Admin]                                                    │
│  ├─ Dealer (userId: "yyy")                                                │
│  │   └─ Roles: [Dealer, VehicleManager]                                  │
│  └─ Individual (userId: "zzz")                                            │
│      └─ Roles: [Buyer]                                                    │
│                                                                            │
│  ROLES                                                                     │
│  ├─ Admin                                                                 │
│  │   └─ Permissions: [Users.*, Roles.*, Vehicles.*, Reports.*]           │
│  ├─ Dealer                                                                │
│  │   └─ Permissions: [Vehicles.Create, Vehicles.Update, Vehicles.Delete] │
│  ├─ VehicleManager                                                        │
│  │   └─ Permissions: [Vehicles.View, Vehicles.Update]                    │
│  └─ Buyer                                                                 │
│      └─ Permissions: [Vehicles.View, Favorites.*, Comparisons.*]         │
│                                                                            │
│  PERMISSIONS                                                               │
│  ├─ Users.Create                                                          │
│  ├─ Users.Update                                                          │
│  ├─ Users.Delete                                                          │
│  ├─ Vehicles.Create                                                       │
│  ├─ Vehicles.Update                                                       │
│  ├─ Vehicles.Delete                                                       │
│  ├─ Roles.Create                                                          │
│  ├─ Roles.Update                                                          │
│  └─ ... (50+ permissions total)                                           │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

### Flujo Admin: Crear Rol

```
1️⃣ Admin accede a /admin/roles
2️⃣ Click "Crear Rol"
3️⃣ Modal: Ingresa nombre ("Moderator") y descripción
4️⃣ POST /api/roles → Rol creado
5️⃣ Redirect a /admin/roles/{id}/permissions
6️⃣ Matrix de permisos: Check boxes por categoría
7️⃣ Click "Guardar Permisos" → POST /api/role-permissions
8️⃣ Toast: "Rol creado con X permisos"
```

### Flujo Admin: Asignar Rol a Usuario

```
1️⃣ Admin accede a /admin/users
2️⃣ Busca usuario "juan@email.com"
3️⃣ Click "Gestionar Roles"
4️⃣ Modal: Lista de roles disponibles
5️⃣ Check "Dealer" y "VehicleManager"
6️⃣ Click "Guardar"
7️⃣ POST /api/user-roles/bulk → Asigna 2 roles
8️⃣ Usuario ahora tiene ambos permisos combinados
```

---

## 🔌 BACKEND API

### Endpoints (Ya Implementados ✅)

```typescript
// Roles
GET    /api/roles                      # Listar roles (paginado)
POST   /api/roles                      # Crear rol
GET    /api/roles/{id}                 # Obtener rol
PUT    /api/roles/{id}                 # Actualizar rol
DELETE /api/roles/{id}                 # Eliminar rol
GET    /api/roles/{id}/permissions     # Permisos del rol

// Permissions
GET    /api/permissions                # Listar todos los permisos
POST   /api/permissions                # Crear permiso
GET    /api/permissions/{id}           # Obtener permiso
PUT    /api/permissions/{id}           # Actualizar permiso
DELETE /api/permissions/{id}           # Eliminar permiso

// Role-Permissions
POST   /api/role-permissions           # Asignar permiso a rol
DELETE /api/role-permissions/{roleId}/{permissionId} # Remover permiso
POST   /api/role-permissions/bulk      # Asignar múltiples permisos
GET    /api/role-permissions/matrix    # Matriz completa (visual)

// User-Roles
GET    /api/user-roles?userId={id}     # Roles del usuario
POST   /api/user-roles                 # Asignar rol a usuario
DELETE /api/user-roles/{userId}/{roleId} # Remover rol
POST   /api/user-roles/bulk            # Asignar múltiples roles
```

---

## 🎨 COMPONENTES

### PASO 1: RolesTable - Lista de Roles

```typescript
// filepath: src/components/admin/rbac/RolesTable.tsx
"use client";

import { useState } from "react";
import { Edit, Trash2, Shield, Users } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useRoles, useDeleteRole } from "@/lib/hooks/useRBAC";
import { CreateRoleModal } from "./CreateRoleModal";
import { EditRoleModal } from "./EditRoleModal";

export function RolesTable() {
  const { data: roles, isLoading } = useRoles();
  const { mutate: deleteRole } = useDeleteRole();
  const [creatingRole, setCreatingRole] = useState(false);
  const [editingRole, setEditingRole] = useState<string | null>(null);

  if (isLoading) {
    return <div>Cargando roles...</div>;
  }

  return (
    <>
      <div className="bg-white rounded-lg border overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Roles</h2>
            <p className="text-sm text-gray-600">
              {roles?.length || 0} roles configurados
            </p>
          </div>
          <Button onClick={() => setCreatingRole(true)}>
            Crear Rol
          </Button>
        </div>

        {/* Table */}
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Rol
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Descripción
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Permisos
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Usuarios
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Acciones
              </th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {roles?.map((role) => (
              <tr key={role.id} className="hover:bg-gray-50">
                <td className="px-6 py-4">
                  <div className="flex items-center gap-2">
                    <Shield size={16} className="text-primary-600" />
                    <span className="font-medium text-gray-900">
                      {role.name}
                    </span>
                    {role.isSystem && (
                      <Badge variant="gray" size="sm">
                        Sistema
                      </Badge>
                    )}
                  </div>
                </td>
                <td className="px-6 py-4 text-sm text-gray-600">
                  {role.description || "Sin descripción"}
                </td>
                <td className="px-6 py-4">
                  <Badge variant="primary">{role.permissionsCount} permisos</Badge>
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center gap-1 text-sm text-gray-600">
                    <Users size={14} />
                    {role.usersCount} usuarios
                  </div>
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setEditingRole(role.id)}
                    >
                      <Edit size={14} className="mr-1" />
                      Editar
                    </Button>
                    {!role.isSystem && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          if (
                            confirm(
                              `¿Eliminar el rol "${role.name}"? Los usuarios perderán estos permisos.`
                            )
                          ) {
                            deleteRole(role.id);
                          }
                        }}
                      >
                        <Trash2 size={14} className="text-red-600" />
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Modals */}
      {creatingRole && (
        <CreateRoleModal onClose={() => setCreatingRole(false)} />
      )}
      {editingRole && (
        <EditRoleModal
          roleId={editingRole}
          onClose={() => setEditingRole(null)}
        />
      )}
    </>
  );
}
```

---

### PASO 2: PermissionMatrix - Matriz de Permisos

```typescript
// filepath: src/components/admin/rbac/PermissionMatrix.tsx
"use client";

import { useState } from "react";
import { CheckSquare, Square } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { usePermissions, useRolePermissions, useUpdateRolePermissions } from "@/lib/hooks/useRBAC";

interface PermissionMatrixProps {
  roleId: string;
}

export function PermissionMatrix({ roleId }: PermissionMatrixProps) {
  const { data: allPermissions, isLoading: loadingPerms } = usePermissions();
  const { data: rolePermissions, isLoading: loadingRolePerms } = useRolePermissions(roleId);
  const { mutate: updatePermissions, isPending } = useUpdateRolePermissions();

  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(
    new Set(rolePermissions?.map((p) => p.id) || [])
  );

  // Group permissions by resource (Users, Vehicles, Roles, etc)
  const groupedPermissions = allPermissions?.reduce((acc, perm) => {
    const [resource] = perm.name.split(".");
    if (!acc[resource]) acc[resource] = [];
    acc[resource].push(perm);
    return acc;
  }, {} as Record<string, typeof allPermissions>);

  const togglePermission = (permId: string) => {
    const newSet = new Set(selectedPermissions);
    if (newSet.has(permId)) {
      newSet.delete(permId);
    } else {
      newSet.add(permId);
    }
    setSelectedPermissions(newSet);
  };

  const selectAll = (resource: string) => {
    const newSet = new Set(selectedPermissions);
    groupedPermissions?.[resource]?.forEach((p) => newSet.add(p.id));
    setSelectedPermissions(newSet);
  };

  const deselectAll = (resource: string) => {
    const newSet = new Set(selectedPermissions);
    groupedPermissions?.[resource]?.forEach((p) => newSet.delete(p.id));
    setSelectedPermissions(newSet);
  };

  const handleSave = () => {
    updatePermissions({
      roleId,
      permissionIds: Array.from(selectedPermissions),
    });
  };

  if (loadingPerms || loadingRolePerms) {
    return <div>Cargando permisos...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">
            Permisos del Rol
          </h3>
          <p className="text-sm text-gray-600 mt-1">
            {selectedPermissions.size} de {allPermissions?.length || 0} permisos seleccionados
          </p>
        </div>
        <Button onClick={handleSave} disabled={isPending}>
          {isPending ? "Guardando..." : "Guardar Permisos"}
        </Button>
      </div>

      {/* Matrix by resource */}
      <div className="space-y-6">
        {Object.entries(groupedPermissions || {}).map(([resource, permissions]) => (
          <div key={resource} className="bg-white rounded-lg border p-6">
            {/* Resource header */}
            <div className="flex items-center justify-between mb-4">
              <h4 className="font-semibold text-gray-900">{resource}</h4>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => selectAll(resource)}
                >
                  Seleccionar todo
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => deselectAll(resource)}
                >
                  Desmarcar todo
                </Button>
              </div>
            </div>

            {/* Permissions grid */}
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
              {permissions.map((perm) => {
                const isSelected = selectedPermissions.has(perm.id);

                return (
                  <button
                    key={perm.id}
                    onClick={() => togglePermission(perm.id)}
                    className={`flex items-center gap-2 p-3 rounded-lg border transition ${
                      isSelected
                        ? "bg-primary-50 border-primary-200 text-primary-900"
                        : "bg-white border-gray-200 text-gray-700 hover:bg-gray-50"
                    }`}
                  >
                    {isSelected ? (
                      <CheckSquare size={18} className="text-primary-600 flex-shrink-0" />
                    ) : (
                      <Square size={18} className="text-gray-400 flex-shrink-0" />
                    )}
                    <span className="text-sm font-medium">{perm.name}</span>
                  </button>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

### PASO 3: AssignRolesModal - Asignar Roles a Usuario

```typescript
// filepath: src/components/admin/rbac/AssignRolesModal.tsx
"use client";

import { useState, useEffect } from "react";
import { CheckSquare, Square } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { useRoles, useUserRoles, useUpdateUserRoles } from "@/lib/hooks/useRBAC";

interface AssignRolesModalProps {
  userId: string;
  userName: string;
  onClose: () => void;
}

export function AssignRolesModal({
  userId,
  userName,
  onClose,
}: AssignRolesModalProps) {
  const { data: allRoles, isLoading: loadingRoles } = useRoles();
  const { data: userRoles, isLoading: loadingUserRoles } = useUserRoles(userId);
  const { mutate: updateRoles, isPending } = useUpdateUserRoles();

  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (userRoles) {
      setSelectedRoles(new Set(userRoles.map((r) => r.id)));
    }
  }, [userRoles]);

  const toggleRole = (roleId: string) => {
    const newSet = new Set(selectedRoles);
    if (newSet.has(roleId)) {
      newSet.delete(roleId);
    } else {
      newSet.add(roleId);
    }
    setSelectedRoles(newSet);
  };

  const handleSave = () => {
    updateRoles(
      {
        userId,
        roleIds: Array.from(selectedRoles),
      },
      {
        onSuccess: () => {
          onClose();
        },
      }
    );
  };

  if (loadingRoles || loadingUserRoles) {
    return (
      <Modal isOpen onClose={onClose} title="Cargando...">
        <div>Cargando roles...</div>
      </Modal>
    );
  }

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={`Asignar Roles a ${userName}`}
    >
      <div className="space-y-6">
        <p className="text-sm text-gray-600">
          Selecciona los roles que deseas asignar a este usuario. Los permisos
          se combinan (unión).
        </p>

        {/* Roles list */}
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {allRoles?.map((role) => {
            const isSelected = selectedRoles.has(role.id);

            return (
              <button
                key={role.id}
                onClick={() => toggleRole(role.id)}
                className={`w-full flex items-start gap-3 p-4 rounded-lg border transition ${
                  isSelected
                    ? "bg-primary-50 border-primary-200"
                    : "bg-white border-gray-200 hover:bg-gray-50"
                }`}
              >
                {isSelected ? (
                  <CheckSquare size={20} className="text-primary-600 flex-shrink-0 mt-0.5" />
                ) : (
                  <Square size={20} className="text-gray-400 flex-shrink-0 mt-0.5" />
                )}
                <div className="flex-1 text-left">
                  <p className="font-medium text-gray-900">{role.name}</p>
                  {role.description && (
                    <p className="text-sm text-gray-600 mt-1">
                      {role.description}
                    </p>
                  )}
                  <p className="text-xs text-gray-500 mt-1">
                    {role.permissionsCount} permisos
                  </p>
                </div>
              </button>
            );
          })}
        </div>

        {/* Actions */}
        <div className="flex gap-3">
          <Button variant="outline" onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button onClick={handleSave} disabled={isPending} className="flex-1">
            {isPending ? "Guardando..." : "Guardar Roles"}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
```

---

## 📄 PÁGINAS

### PASO 4: Admin Roles Page

```typescript
// filepath: src/app/(admin)/admin/roles/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Shield } from "lucide-react";
import { auth } from "@/lib/auth";
import { RolesTable } from "@/components/admin/rbac/RolesTable";

export const metadata: Metadata = {
  title: "Gestión de Roles | Admin OKLA",
};

export default async function RolesPage() {
  const session = await auth();

  if (!session?.user || session.user.role !== "Admin") {
    redirect("/login");
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-8">
        <Shield size={32} className="text-primary-600" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Gestión de Roles y Permisos
          </h1>
          <p className="text-gray-600">
            Configura roles y asigna permisos (RBAC)
          </p>
        </div>
      </div>

      <RolesTable />
    </div>
  );
}
```

---

### PASO 5: Role Permissions Page

```typescript
// filepath: src/app/(admin)/admin/roles/[id]/permissions/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Shield } from "lucide-react";
import { auth } from "@/lib/auth";
import { PermissionMatrix } from "@/components/admin/rbac/PermissionMatrix";

export const metadata: Metadata = {
  title: "Permisos del Rol | Admin OKLA",
};

interface RolePermissionsPageProps {
  params: { id: string };
}

export default async function RolePermissionsPage({
  params,
}: RolePermissionsPageProps) {
  const session = await auth();

  if (!session?.user || session.user.role !== "Admin") {
    redirect("/login");
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-8">
        <Shield size={32} className="text-primary-600" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Configurar Permisos
          </h1>
          <p className="text-gray-600">
            Selecciona los permisos para este rol
          </p>
        </div>
      </div>

      <PermissionMatrix roleId={params.id} />
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

```typescript
// filepath: src/lib/hooks/useRBAC.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { rbacService } from "@/lib/services/rbacService";
import { toast } from "sonner";

export function useRoles() {
  return useQuery({
    queryKey: ["roles"],
    queryFn: () => rbacService.getRoles(),
  });
}

export function usePermissions() {
  return useQuery({
    queryKey: ["permissions"],
    queryFn: () => rbacService.getPermissions(),
  });
}

export function useRolePermissions(roleId: string) {
  return useQuery({
    queryKey: ["rolePermissions", roleId],
    queryFn: () => rbacService.getRolePermissions(roleId),
    enabled: !!roleId,
  });
}

export function useUpdateRolePermissions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      roleId,
      permissionIds,
    }: {
      roleId: string;
      permissionIds: string[];
    }) => rbacService.updateRolePermissions(roleId, permissionIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rolePermissions"] });
      toast.success("Permisos actualizados");
    },
  });
}

export function useUserRoles(userId: string) {
  return useQuery({
    queryKey: ["userRoles", userId],
    queryFn: () => rbacService.getUserRoles(userId),
    enabled: !!userId,
  });
}

export function useUpdateUserRoles() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, roleIds }: { userId: string; roleIds: string[] }) =>
      rbacService.updateUserRoles(userId, roleIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["userRoles"] });
      toast.success("Roles actualizados");
    },
  });
}

export function useDeleteRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (roleId: string) => rbacService.deleteRole(roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      toast.success("Rol eliminado");
    },
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

```typescript
// filepath: src/types/rbac.ts
export interface Role {
  id: string;
  name: string;
  description?: string;
  isSystem: boolean; // Roles del sistema (no eliminables)
  permissionsCount: number;
  usersCount: number;
  createdAt: string;
}

export interface Permission {
  id: string;
  name: string; // "Users.Create", "Vehicles.Delete"
  description?: string;
  resource: string; // "Users", "Vehicles"
  action: string; // "Create", "Delete"
}

export interface RolePermission {
  roleId: string;
  permissionId: string;
  permission: Permission;
}

export interface UserRole {
  userId: string;
  roleId: string;
  role: Role;
  assignedAt: string;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Flujo Completo:
# 1. Admin Roles Page
# - /admin/roles muestra tabla con roles existentes
# - Crear nuevo rol funciona
# - Editar rol actualiza nombre/descripción
# - Eliminar rol funciona (solo custom roles)

# 2. Permission Matrix
# - /admin/roles/{id}/permissions muestra matriz
# - Permisos agrupados por recurso (Users, Vehicles, etc)
# - Seleccionar/deseleccionar funciona
# - "Seleccionar todo" marca todos de ese recurso
# - Guardar actualiza permisos del rol

# 3. Assign Roles to User
# - Modal desde /admin/users
# - Lista todos los roles disponibles
# - Check/uncheck roles funciona
# - Guardar asigna múltiples roles al usuario
# - Usuario obtiene permisos combinados
```

---

## 🚀 MEJORAS FUTURAS

1. **Permission Templates**: Templates predefinidos (Read-Only, Power User, etc)
2. **Audit Log**: Registro de cambios en roles/permisos
3. **Bulk Role Assignment**: Asignar rol a múltiples usuarios a la vez
4. **Permission Dependencies**: Auto-seleccionar permisos dependientes

---

✅ **Documentos P0 y P1 completados:**

- 27-kyc-verificacion.md (P0 - Crítico)
- 28-oauth-management.md (P0 - Crítico)
- 08-perfil.md - Expansión 2FA (P1 - Importante)
- 29-admin-rbac.md (P1 - Importante)

**Próximos pasos (P2):**

- Email Verification Flow en 07-auth.md
- Social Login Expansion (Facebook, Apple)
- Device Fingerprinting UI
- SMS 2FA Setup Flow
