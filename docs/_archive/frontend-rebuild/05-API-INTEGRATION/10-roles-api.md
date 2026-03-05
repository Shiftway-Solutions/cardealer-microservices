# 🔐 10 - Roles & Permissions API (RoleService)

**Servicio:** RoleService  
**Puerto:** 8080  
**Base Path:** `/api/roles`, `/api/permissions`  
**Autenticación:** ✅ Requerida (Admin only)

---

## 📖 Descripción

Sistema RBAC (Role-Based Access Control) para gestionar roles, permisos y asignaciones.

### Endpoints

#### RolesController

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/roles` | Crear rol |
| `GET` | `/api/roles` | Listar roles (paginado) |
| `GET` | `/api/roles/{id}` | Obtener rol con permisos |
| `PUT` | `/api/roles/{id}` | Actualizar rol |
| `DELETE` | `/api/roles/{id}` | Eliminar rol |

#### PermissionsController

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/permissions` | Listar todos los permisos |
| `GET` | `/api/permissions/{id}` | Obtener permiso específico |

#### RolePermissionsController

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/roles/{roleId}/permissions/{permissionId}` | Asignar permiso a rol |
| `DELETE` | `/api/roles/{roleId}/permissions/{permissionId}` | Remover permiso de rol |

---

## 🔧 TypeScript Types

\`\`\`typescript
export interface Role {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isSystemRole: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  permissions?: Permission[];
}

export interface Permission {
  id: string;
  resource: string;
  action: PermissionAction;
  displayName: string;
  description?: string;
}

export type PermissionAction = 
  | "Create" | "Read" | "Update" | "Delete" 
  | "Manage" | "Execute";

export interface CreateRoleRequest {
  name: string;
  displayName: string;
  description?: string;
}

export interface UpdateRoleRequest {
  displayName?: string;
  description?: string;
  isActive?: boolean;
}
\`\`\`

---

## 📡 Service Layer

\`\`\`typescript
// src/services/roleService.ts
import { apiClient } from "./api-client";
import type { Role, Permission, CreateRoleRequest, UpdateRoleRequest } from "@/types/role";

class RoleService {
  // Roles
  async createRole(request: CreateRoleRequest): Promise<Role> {
    const response = await apiClient.post<Role>("/api/roles", request);
    return response.data;
  }

  async getRoles(params?: { isActive?: boolean; page?: number; pageSize?: number }): Promise<PaginatedResult<Role>> {
    const response = await apiClient.get<PaginatedResult<Role>>("/api/roles", { params });
    return response.data;
  }

  async getRoleById(id: string): Promise<Role> {
    const response = await apiClient.get<Role>(\`/api/roles/\${id}\`);
    return response.data;
  }

  async updateRole(id: string, request: UpdateRoleRequest): Promise<Role> {
    const response = await apiClient.put<Role>(\`/api/roles/\${id}\`, request);
    return response.data;
  }

  async deleteRole(id: string): Promise<void> {
    await apiClient.delete(\`/api/roles/\${id}\`);
  }

  // Permissions
  async getPermissions(): Promise<Permission[]> {
    const response = await apiClient.get<Permission[]>("/api/permissions");
    return response.data;
  }

  async assignPermission(roleId: string, permissionId: string): Promise<void> {
    await apiClient.post(\`/api/roles/\${roleId}/permissions/\${permissionId}\`);
  }

  async removePermission(roleId: string, permissionId: string): Promise<void> {
    await apiClient.delete(\`/api/roles/\${roleId}/permissions/\${permissionId}\`);
  }
}

export const roleService = new RoleService();
\`\`\`

---

## 🎣 React Query Hooks

\`\`\`typescript
// src/hooks/useRoles.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { roleService } from "@/services/roleService";

export const roleKeys = {
  all: ["roles"] as const,
  lists: () => [...roleKeys.all, "list"] as const,
  list: (params: any) => [...roleKeys.lists(), params] as const,
  details: () => [...roleKeys.all, "detail"] as const,
  detail: (id: string) => [...roleKeys.details(), id] as const,
  permissions: () => [...roleKeys.all, "permissions"] as const,
};

export function useRoles(params?: { isActive?: boolean }) {
  return useQuery({
    queryKey: roleKeys.list(params),
    queryFn: () => roleService.getRoles(params),
  });
}

export function useRole(id: string) {
  return useQuery({
    queryKey: roleKeys.detail(id),
    queryFn: () => roleService.getRoleById(id),
    enabled: !!id,
  });
}

export function usePermissions() {
  return useQuery({
    queryKey: roleKeys.permissions(),
    queryFn: () => roleService.getPermissions(),
  });
}

export function useCreateRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roleService.createRole,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: roleKeys.lists() }),
  });
}

export function useUpdateRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateRoleRequest }) =>
      roleService.updateRole(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: roleKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: roleKeys.lists() });
    },
  });
}

export function useDeleteRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: roleService.deleteRole,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: roleKeys.lists() }),
  });
}
\`\`\`

---

## 🚀 Ejemplo de Uso

\`\`\`typescript
// src/pages/RolesManagementPage.tsx
import { useRoles, useCreateRole } from "@/hooks/useRoles";

export const RolesManagementPage = () => {
  const { data: roles } = useRoles({ isActive: true });
  const createMutation = useCreateRole();

  return (
    <div>
      <h1>Gestión de Roles</h1>
      <button onClick={() => createMutation.mutate({
        name: "moderator",
        displayName: "Moderador",
        description: "Modera contenido"
      })}>
        Crear Rol
      </button>
      
      {roles?.data.map(role => (
        <div key={role.id}>
          {role.displayName} ({role.permissions?.length} permisos)
        </div>
      ))}
    </div>
  );
};
\`\`\`

---

✅ **9 Endpoints documentados**  
_Última actualización: Enero 30, 2026_
