# 👤 Integración API - User Service

**Fecha:** Enero 30, 2026  
**Propósito:** Documentación completa del UserService para gestión de perfiles de usuario  
**Endpoints Documentados:** 5 endpoints (CRUD completo)

---

## 📋 ÍNDICE

1. [Cliente HTTP Base](#1-cliente-http-base)
2. [Tipos TypeScript](#2-tipos-typescript)
3. [Users Service](#3-users-service)
4. [Hooks de React Query](#4-hooks-de-react-query)
5. [Componentes de Ejemplo](#5-componentes-de-ejemplo)

---

## 1. Cliente HTTP Base

Utiliza el cliente HTTP configurado en [01-cliente-http.md](01-cliente-http.md).

```typescript
// filepath: src/services/api/apiClient.ts
import axios from "axios";

const apiClient = axios.create({
  baseURL: process.env.REACT_APP_API_URL || "https://api.okla.com.do",
  headers: {
    "Content-Type": "application/json",
  },
});

// Interceptor para agregar token automáticamente
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
```

---

## 2. Tipos TypeScript

```typescript
// filepath: src/types/user.ts

export enum AccountType {
  Individual = "Individual",
  Dealer = "Dealer",
  Admin = "Admin",
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  avatarUrl?: string;
  accountType: AccountType;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;

  // Campos adicionales para dealers
  dealerId?: string;
  businessName?: string;

  // Preferencias
  preferredLanguage?: string;
  timezone?: string;
  notificationsEnabled: boolean;
  emailNotifications: boolean;
  smsNotifications: boolean;
  pushNotifications: boolean;
}

export interface CreateUserRequest {
  email: string;
  fullName: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  accountType: AccountType;
  preferredLanguage?: string;
  timezone?: string;
}

export interface UpdateUserRequest {
  fullName?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  avatarUrl?: string;
  preferredLanguage?: string;
  timezone?: string;
  notificationsEnabled?: boolean;
  emailNotifications?: boolean;
  smsNotifications?: boolean;
  pushNotifications?: boolean;
}

export interface UserListResponse {
  users: User[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserListRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  accountType?: AccountType;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}
```

---

## 3. Users Service

### 3.1 CRUD Completo de Usuarios

```typescript
// filepath: src/services/api/usersService.ts
import apiClient from "./apiClient";
import type {
  User,
  CreateUserRequest,
  UpdateUserRequest,
  UserListRequest,
  UserListResponse,
} from "@/types/user";

export const usersService = {
  /**
   * Obtener lista de usuarios con filtros y paginación
   * Requiere autenticación: Admin o Dealer (solo puede ver sus propios usuarios)
   */
  async getUsers(params?: UserListRequest): Promise<UserListResponse> {
    const response = await apiClient.get<UserListResponse>("/users", {
      params: {
        page: params?.page || 1,
        pageSize: params?.pageSize || 20,
        ...params,
      },
    });
    return response.data;
  },

  /**
   * Obtener usuario por ID
   * Requiere autenticación: Admin o el propio usuario
   */
  async getUserById(userId: string): Promise<User> {
    const response = await apiClient.get<User>(`/users/${userId}`);
    return response.data;
  },

  /**
   * Crear nuevo usuario
   * Requiere autenticación: Admin
   * Nota: Los usuarios normales se crean a través del endpoint /auth/register
   */
  async createUser(data: CreateUserRequest): Promise<User> {
    const response = await apiClient.post<User>("/users", data);
    return response.data;
  },

  /**
   * Actualizar información de usuario
   * Requiere autenticación: Admin o el propio usuario
   */
  async updateUser(userId: string, data: UpdateUserRequest): Promise<User> {
    const response = await apiClient.put<User>(`/users/${userId}`, data);
    return response.data;
  },

  /**
   * Eliminar usuario (soft delete)
   * Requiere autenticación: Admin
   */
  async deleteUser(userId: string): Promise<void> {
    await apiClient.delete(`/users/${userId}`);
  },

  /**
   * Obtener perfil del usuario autenticado actual
   * Requiere autenticación
   */
  async getCurrentUser(): Promise<User> {
    const response = await apiClient.get<User>("/auth/me");
    return response.data;
  },

  /**
   * Actualizar perfil del usuario autenticado
   * Requiere autenticación
   */
  async updateCurrentUser(data: UpdateUserRequest): Promise<User> {
    const currentUser = await this.getCurrentUser();
    return this.updateUser(currentUser.id, data);
  },

  /**
   * Subir avatar del usuario
   * Requiere autenticación
   */
  async uploadAvatar(userId: string, file: File): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("userId", userId);

    const response = await apiClient.post<{ url: string }>(
      "/media/upload/avatar",
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );

    // Actualizar usuario con nueva URL de avatar
    await this.updateUser(userId, { avatarUrl: response.data.url });

    return response.data.url;
  },

  /**
   * Verificar email del usuario
   * Requiere autenticación: Admin
   */
  async verifyEmail(userId: string): Promise<User> {
    const response = await apiClient.post<User>(
      `/users/${userId}/verify-email`,
    );
    return response.data;
  },

  /**
   * Activar/Desactivar usuario
   * Requiere autenticación: Admin
   */
  async toggleUserStatus(userId: string, isActive: boolean): Promise<User> {
    const response = await apiClient.put<User>(`/users/${userId}/status`, {
      isActive,
    });
    return response.data;
  },
};
```

---

## 4. Hooks de React Query

### 4.1 Hooks para Gestión de Usuarios

```typescript
// filepath: src/hooks/useUsers.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { usersService } from "@/services/api/usersService";
import type {
  CreateUserRequest,
  UpdateUserRequest,
  UserListRequest,
} from "@/types/user";

/**
 * Hook para obtener lista de usuarios con filtros
 */
export const useUsers = (params?: UserListRequest) => {
  return useQuery({
    queryKey: ["users", "list", params],
    queryFn: () => usersService.getUsers(params),
    keepPreviousData: true,
  });
};

/**
 * Hook para obtener un usuario por ID
 */
export const useUser = (userId: string) => {
  return useQuery({
    queryKey: ["users", userId],
    queryFn: () => usersService.getUserById(userId),
    enabled: !!userId,
  });
};

/**
 * Hook para obtener el usuario autenticado actual
 */
export const useCurrentUser = () => {
  return useQuery({
    queryKey: ["users", "current"],
    queryFn: () => usersService.getCurrentUser(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false, // No reintentar si falla (probablemente no autenticado)
  });
};

/**
 * Hook para crear un nuevo usuario
 */
export const useCreateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateUserRequest) => usersService.createUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries(["users", "list"]);
    },
  });
};

/**
 * Hook para actualizar un usuario
 */
export const useUpdateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      userId,
      data,
    }: {
      userId: string;
      data: UpdateUserRequest;
    }) => usersService.updateUser(userId, data),
    onSuccess: (updatedUser) => {
      queryClient.invalidateQueries(["users", updatedUser.id]);
      queryClient.invalidateQueries(["users", "list"]);
      // Si es el usuario actual, actualizar cache
      queryClient.setQueryData(["users", "current"], updatedUser);
    },
  });
};

/**
 * Hook para actualizar el perfil del usuario autenticado
 */
export const useUpdateCurrentUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateUserRequest) =>
      usersService.updateCurrentUser(data),
    onSuccess: (updatedUser) => {
      queryClient.setQueryData(["users", "current"], updatedUser);
      queryClient.invalidateQueries(["users", updatedUser.id]);
    },
  });
};

/**
 * Hook para eliminar un usuario
 */
export const useDeleteUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => usersService.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries(["users", "list"]);
    },
  });
};

/**
 * Hook para subir avatar
 */
export const useUploadAvatar = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, file }: { userId: string; file: File }) =>
      usersService.uploadAvatar(userId, file),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries(["users", variables.userId]);
      queryClient.invalidateQueries(["users", "current"]);
    },
  });
};

/**
 * Hook para verificar email de usuario
 */
export const useVerifyEmail = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => usersService.verifyEmail(userId),
    onSuccess: (updatedUser) => {
      queryClient.invalidateQueries(["users", updatedUser.id]);
    },
  });
};

/**
 * Hook para activar/desactivar usuario
 */
export const useToggleUserStatus = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      usersService.toggleUserStatus(userId, isActive),
    onSuccess: (updatedUser) => {
      queryClient.invalidateQueries(["users", updatedUser.id]);
      queryClient.invalidateQueries(["users", "list"]);
    },
  });
};
```

---

## 5. Componentes de Ejemplo

### 5.1 Página de Perfil de Usuario

```typescript
// filepath: src/pages/UserProfilePage.tsx
import React, { useState } from 'react';
import { useCurrentUser, useUpdateCurrentUser, useUploadAvatar } from '@/hooks/useUsers';
import type { UpdateUserRequest } from '@/types/user';

export const UserProfilePage: React.FC = () => {
  const { data: user, isLoading } = useCurrentUser();
  const updateUser = useUpdateCurrentUser();
  const uploadAvatar = useUploadAvatar();

  const [formData, setFormData] = useState<UpdateUserRequest>({});
  const [isEditing, setIsEditing] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await updateUser.mutateAsync(formData);
      setIsEditing(false);
    } catch (error) {
      console.error('Error al actualizar perfil:', error);
    }
  };

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !user) return;

    try {
      await uploadAvatar.mutateAsync({ userId: user.id, file });
    } catch (error) {
      console.error('Error al subir avatar:', error);
    }
  };

  if (isLoading) return <div>Cargando perfil...</div>;
  if (!user) return <div>Usuario no encontrado</div>;

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <h1 className="text-3xl font-bold mb-8">Mi Perfil</h1>

      <div className="bg-white rounded-lg shadow p-6">
        {/* Avatar */}
        <div className="flex items-center gap-6 mb-8">
          <div className="relative">
            <img
              src={user.avatarUrl || '/default-avatar.png'}
              alt={user.fullName}
              className="w-24 h-24 rounded-full object-cover"
            />
            {isEditing && (
              <label className="absolute bottom-0 right-0 bg-blue-600 text-white p-2 rounded-full cursor-pointer hover:bg-blue-700">
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleAvatarChange}
                  className="hidden"
                />
                📷
              </label>
            )}
          </div>
          <div>
            <h2 className="text-2xl font-bold">{user.fullName}</h2>
            <p className="text-gray-600">{user.email}</p>
            <span
              className={`inline-block px-3 py-1 rounded-full text-sm mt-2 ${
                user.isEmailVerified
                  ? 'bg-green-100 text-green-800'
                  : 'bg-yellow-100 text-yellow-800'
              }`}
            >
              {user.isEmailVerified ? '✓ Email Verificado' : '⚠ Email Sin Verificar'}
            </span>
          </div>
        </div>

        {/* Formulario */}
        <form onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">
                  Nombre
                </label>
                <input
                  type="text"
                  defaultValue={user.firstName}
                  onChange={(e) =>
                    setFormData({ ...formData, firstName: e.target.value })
                  }
                  disabled={!isEditing}
                  className="w-full px-4 py-2 border rounded disabled:bg-gray-100"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  Apellido
                </label>
                <input
                  type="text"
                  defaultValue={user.lastName}
                  onChange={(e) =>
                    setFormData({ ...formData, lastName: e.target.value })
                  }
                  disabled={!isEditing}
                  className="w-full px-4 py-2 border rounded disabled:bg-gray-100"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">
                Teléfono
              </label>
              <input
                type="tel"
                defaultValue={user.phoneNumber}
                onChange={(e) =>
                  setFormData({ ...formData, phoneNumber: e.target.value })
                }
                disabled={!isEditing}
                className="w-full px-4 py-2 border rounded disabled:bg-gray-100"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">
                Idioma Preferido
              </label>
              <select
                defaultValue={user.preferredLanguage || 'es'}
                onChange={(e) =>
                  setFormData({ ...formData, preferredLanguage: e.target.value })
                }
                disabled={!isEditing}
                className="w-full px-4 py-2 border rounded disabled:bg-gray-100"
              >
                <option value="es">Español</option>
                <option value="en">English</option>
              </select>
            </div>

            {/* Notificaciones */}
            {isEditing && (
              <div className="border-t pt-4 mt-4">
                <h3 className="font-bold mb-3">Preferencias de Notificaciones</h3>
                <div className="space-y-2">
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      defaultChecked={user.emailNotifications}
                      onChange={(e) =>
                        setFormData({ ...formData, emailNotifications: e.target.checked })
                      }
                    />
                    <span>Email</span>
                  </label>
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      defaultChecked={user.smsNotifications}
                      onChange={(e) =>
                        setFormData({ ...formData, smsNotifications: e.target.checked })
                      }
                    />
                    <span>SMS</span>
                  </label>
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      defaultChecked={user.pushNotifications}
                      onChange={(e) =>
                        setFormData({ ...formData, pushNotifications: e.target.checked })
                      }
                    />
                    <span>Push Notifications</span>
                  </label>
                </div>
              </div>
            )}
          </div>

          {/* Botones */}
          <div className="flex gap-4 mt-6">
            {isEditing ? (
              <>
                <button
                  type="submit"
                  disabled={updateUser.isLoading}
                  className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                >
                  {updateUser.isLoading ? 'Guardando...' : 'Guardar Cambios'}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setIsEditing(false);
                    setFormData({});
                  }}
                  className="px-6 py-2 bg-gray-200 rounded hover:bg-gray-300"
                >
                  Cancelar
                </button>
              </>
            ) : (
              <button
                type="button"
                onClick={() => setIsEditing(true)}
                className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
              >
                Editar Perfil
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Información adicional */}
      <div className="bg-gray-50 rounded-lg p-6 mt-6">
        <h3 className="font-bold mb-4">Información de Cuenta</h3>
        <div className="space-y-2 text-sm">
          <p>
            <strong>Tipo de Cuenta:</strong> {user.accountType}
          </p>
          <p>
            <strong>Miembro desde:</strong>{' '}
            {new Date(user.createdAt).toLocaleDateString('es-DO')}
          </p>
          {user.lastLoginAt && (
            <p>
              <strong>Último acceso:</strong>{' '}
              {new Date(user.lastLoginAt).toLocaleString('es-DO')}
            </p>
          )}
        </div>
      </div>
    </div>
  );
};
```

### 5.2 Lista de Usuarios (Admin)

```typescript
// filepath: src/pages/admin/UsersListPage.tsx
import React, { useState } from 'react';
import { useUsers, useToggleUserStatus, useDeleteUser } from '@/hooks/useUsers';
import type { UserListRequest, User } from '@/types/user';

export const UsersListPage: React.FC = () => {
  const [filters, setFilters] = useState<UserListRequest>({
    page: 1,
    pageSize: 20,
  });

  const { data: response, isLoading } = useUsers(filters);
  const toggleStatus = useToggleUserStatus();
  const deleteUser = useDeleteUser();

  const handleToggleStatus = async (user: User) => {
    if (confirm(`¿Deseas ${user.isActive ? 'desactivar' : 'activar'} a ${user.fullName}?`)) {
      await toggleStatus.mutateAsync({ userId: user.id, isActive: !user.isActive });
    }
  };

  const handleDelete = async (user: User) => {
    if (confirm(`¿Estás seguro de eliminar a ${user.fullName}? Esta acción no se puede deshacer.`)) {
      await deleteUser.mutateAsync(user.id);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold">Gestión de Usuarios</h1>
        <button className="px-4 py-2 bg-blue-600 text-white rounded">
          + Nuevo Usuario
        </button>
      </div>

      {/* Filtros */}
      <div className="bg-white rounded-lg shadow p-4 mb-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <input
            type="text"
            placeholder="Buscar por nombre o email..."
            onChange={(e) => setFilters({ ...filters, search: e.target.value, page: 1 })}
            className="px-4 py-2 border rounded"
          />
          <select
            onChange={(e) => setFilters({ ...filters, accountType: e.target.value as any, page: 1 })}
            className="px-4 py-2 border rounded"
          >
            <option value="">Todos los tipos</option>
            <option value="Individual">Individual</option>
            <option value="Dealer">Dealer</option>
            <option value="Admin">Admin</option>
          </select>
          <select
            onChange={(e) => setFilters({ ...filters, isActive: e.target.value === 'true', page: 1 })}
            className="px-4 py-2 border rounded"
          >
            <option value="">Todos los estados</option>
            <option value="true">Activos</option>
            <option value="false">Inactivos</option>
          </select>
          <button
            onClick={() => setFilters({ page: 1, pageSize: 20 })}
            className="px-4 py-2 bg-gray-200 rounded"
          >
            Limpiar
          </button>
        </div>
      </div>

      {/* Tabla */}
      {isLoading ? (
        <div>Cargando usuarios...</div>
      ) : (
        <>
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Usuario
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Email
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Tipo
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Estado
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Registro
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {response?.users.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <img
                          src={user.avatarUrl || '/default-avatar.png'}
                          alt={user.fullName}
                          className="w-10 h-10 rounded-full"
                        />
                        <div>
                          <div className="font-medium">{user.fullName}</div>
                          {user.phoneNumber && (
                            <div className="text-sm text-gray-500">{user.phoneNumber}</div>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div>{user.email}</div>
                      {user.isEmailVerified && (
                        <span className="text-xs text-green-600">✓ Verificado</span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <span className="px-2 py-1 text-xs rounded bg-blue-100 text-blue-800">
                        {user.accountType}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 text-xs rounded ${
                          user.isActive
                            ? 'bg-green-100 text-green-800'
                            : 'bg-red-100 text-red-800'
                        }`}
                      >
                        {user.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {new Date(user.createdAt).toLocaleDateString('es-DO')}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => handleToggleStatus(user)}
                        className="text-blue-600 hover:text-blue-800 mr-3"
                      >
                        {user.isActive ? 'Desactivar' : 'Activar'}
                      </button>
                      <button
                        onClick={() => handleDelete(user)}
                        className="text-red-600 hover:text-red-800"
                      >
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Paginación */}
          <div className="flex justify-between items-center mt-6">
            <div className="text-sm text-gray-600">
              Mostrando {((filters.page || 1) - 1) * (filters.pageSize || 20) + 1} -{' '}
              {Math.min((filters.page || 1) * (filters.pageSize || 20), response?.totalCount || 0)} de{' '}
              {response?.totalCount} usuarios
            </div>
            <div className="flex gap-2">
              <button
                disabled={(filters.page || 1) === 1}
                onClick={() => setFilters({ ...filters, page: (filters.page || 1) - 1 })}
                className="px-4 py-2 border rounded disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                disabled={(filters.page || 1) >= (response?.totalPages || 1)}
                onClick={() => setFilters({ ...filters, page: (filters.page || 1) + 1 })}
                className="px-4 py-2 border rounded disabled:opacity-50"
              >
                Siguiente
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
```

---

## 🎯 Resumen de Endpoints Documentados

| Método   | Endpoint              | Autenticación   | Descripción                    |
| -------- | --------------------- | --------------- | ------------------------------ |
| `GET`    | `/api/users`          | ✅ Admin/Dealer | Listar usuarios con filtros    |
| `GET`    | `/api/users/{userId}` | ✅ Admin/Self   | Obtener usuario por ID         |
| `POST`   | `/api/users`          | ✅ Admin        | Crear nuevo usuario            |
| `PUT`    | `/api/users/{userId}` | ✅ Admin/Self   | Actualizar usuario             |
| `DELETE` | `/api/users/{userId}` | ✅ Admin        | Eliminar usuario (soft delete) |

### Endpoints Adicionales (extendidos en el servicio):

- `GET /api/auth/me` - Obtener usuario actual (ver [02-autenticacion.md](02-autenticacion.md))
- `POST /api/users/{userId}/verify-email` - Verificar email (Admin)
- `PUT /api/users/{userId}/status` - Activar/desactivar usuario (Admin)
- `POST /api/media/upload/avatar` - Subir avatar (ver [04-subida-imagenes.md](04-subida-imagenes.md))

**Total: 5 endpoints principales + 4 extendidos = 9 endpoints documentados** ✅

---

## 🔒 Notas de Seguridad

### Autorización

- **Admin**: Acceso completo a todos los endpoints
- **Dealer**: Puede listar y ver sus propios usuarios/empleados
- **Usuario Individual**: Solo puede ver y editar su propio perfil

### Validaciones

- Email debe ser único en el sistema
- Phone number debe seguir formato internacional
- Avatar files máximo 5MB, formatos: JPG, PNG, WEBP
- Soft delete: Los usuarios eliminados mantienen sus datos históricos

---

_Generado: Enero 30, 2026_  
_Actualizado por: Sistema de Auditoría Automatizado_
