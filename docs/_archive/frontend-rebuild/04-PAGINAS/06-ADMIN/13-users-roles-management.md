---
title: "👥 60 - Users & Roles Management"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["AuthService", "UserService"]
status: complete
last_updated: "2026-01-30"
---

# 👥 60 - Users & Roles Management

**Objetivo:** Gestión completa de usuarios, roles y permisos con sistema RBAC.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🔴 Alta  
**Dependencias:** UserService, RoleService, AuthService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [UsersManagementPage](#-usersmanagementpage)
3. [RolesManagementPage](#-rolesmanagementpage)
4. [PermissionsManagementPage](#-permissionsmanagementpage)
5. [Servicios API](#-servicios-api)

---

## 🏗️ ARQUITECTURA

```
pages/admin/
├── UsersManagementPage.tsx         # Gestión de usuarios (334 líneas)
├── RolesManagementPage.tsx         # Gestión de roles (564 líneas)
├── RoleDetailPage.tsx              # Detalle de rol
├── PermissionsManagementPage.tsx   # Permisos del sistema
└── components/
    ├── UserTable.tsx               # Tabla de usuarios
    ├── RoleCard.tsx                # Card de rol
    ├── PermissionMatrix.tsx        # Matriz de permisos
    └── CreateRoleModal.tsx         # Modal crear rol
```

---

## 👤 USERSMANAGEMENTPAGE

**Ruta:** `/admin/users`

```typescript
// src/pages/admin/UsersManagementPage.tsx
import { useState, useMemo } from 'react';
import { useUsersManagement, useBanUser, useUnbanUser, useDeleteUser } from '@/hooks';
import type { AdminUser } from '@/types/admin';
import {
  FiSearch,
  FiShield,
  FiLock,
  FiUnlock,
  FiTrash2,
  FiMoreVertical,
  FiRefreshCw,
} from 'react-icons/fi';

type FilterStatus = 'all' | 'active' | 'banned' | 'suspended';

interface AdminUser {
  id: string;
  username: string;
  email: string;
  role: 'admin' | 'moderator' | 'seller' | 'buyer';
  status: 'active' | 'banned' | 'suspended';
  listingsCount: number;
  joinedAt: string;
  lastLogin?: string;
}

const UsersManagementPage = () => {
  const { users: apiUsers, isLoading, refetch } = useUsersManagement();
  const banUser = useBanUser();
  const unbanUser = useUnbanUser();
  const deleteUserMutation = useDeleteUser();

  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<FilterStatus>('all');
  const [selectedUser, setSelectedUser] = useState<string | null>(null);

  const users: AdminUser[] = useMemo(() => {
    if (apiUsers && apiUsers.length > 0) {
      return apiUsers.map((u) => ({
        id: u.id,
        username: u.username || u.email?.split('@')[0] || 'user',
        email: u.email || '',
        role: u.role || 'buyer',
        status: u.status || 'active',
        listingsCount: u.listingsCount || 0,
        joinedAt: u.joinedAt || u.createdAt || new Date().toISOString(),
        lastLogin: u.lastLogin,
      }));
    }
    return [];
  }, [apiUsers]);

  const filteredUsers = users.filter((user) => {
    if (filterStatus !== 'all' && user.status !== filterStatus) return false;
    if (searchTerm) {
      const search = searchTerm.toLowerCase();
      return (
        user.username.toLowerCase().includes(search) ||
        user.email.toLowerCase().includes(search)
      );
    }
    return true;
  });

  const formatDate = (timestamp: string) => {
    return new Date(timestamp).toLocaleDateString('es-DO', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const getRoleBadgeColor = (role: string) => {
    switch (role) {
      case 'admin':
        return 'bg-purple-100 text-purple-800';
      case 'moderator':
        return 'bg-blue-100 text-blue-800';
      case 'seller':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusBadge = (status: AdminUser['status']) => {
    const styles = {
      active: 'bg-green-100 text-green-800',
      banned: 'bg-red-100 text-red-800',
      suspended: 'bg-yellow-100 text-yellow-800',
    };

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-full ${styles[status]}`}>
        {status === 'active' ? 'Activo' : status === 'banned' ? 'Baneado' : 'Suspendido'}
      </span>
    );
  };

  const handleBanUser = async (userId: string) => {
    const reason = prompt('Razón del ban:');
    if (!reason) return;

    try {
      await banUser.mutateAsync({ id: userId, reason });
      refetch();
    } catch (error) {
      console.error('Error banning user:', error);
    }
    setSelectedUser(null);
  };

  const handleUnbanUser = async (userId: string) => {
    try {
      await unbanUser.mutateAsync(userId);
      refetch();
    } catch (error) {
      console.error('Error unbanning user:', error);
    }
    setSelectedUser(null);
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('¿Estás seguro de eliminar este usuario? Esta acción no se puede deshacer.')) {
      return;
    }

    try {
      await deleteUserMutation.mutateAsync(userId);
      refetch();
    } catch (error) {
      console.error('Error deleting user:', error);
    }
    setSelectedUser(null);
  };

  return (
    <div>
      {/* Header */}
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Gestión de Usuarios</h1>
          <p className="text-gray-600 mt-1">Administrar cuentas y permisos de usuarios</p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors disabled:opacity-50"
        >
          <FiRefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          Actualizar
        </button>
      </div>

      {/* Filters & Search */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <div className="flex flex-col sm:flex-row gap-4">
          {/* Search */}
          <div className="flex-1 relative">
            <FiSearch className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 w-5 h-5" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar por nombre o email..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
            />
          </div>

          {/* Status Filter */}
          <div className="flex gap-2">
            {(['all', 'active', 'banned', 'suspended'] as const).map((status) => (
              <button
                key={status}
                onClick={() => setFilterStatus(status)}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                  filterStatus === status
                    ? 'bg-primary text-white'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                }`}
              >
                {status === 'all' ? 'Todos' : status === 'active' ? 'Activos' : status === 'banned' ? 'Baneados' : 'Suspendidos'}
                <span className="ml-1 text-xs opacity-75">
                  ({users.filter((u) => status === 'all' || u.status === status).length})
                </span>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {isLoading ? (
          <div className="p-12 text-center">
            <div className="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
            <p className="text-gray-600 mt-4">Cargando usuarios...</p>
          </div>
        ) : filteredUsers.length === 0 ? (
          <div className="p-12 text-center">
            <p className="text-gray-600">No se encontraron usuarios</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Usuario
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Rol
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Estado
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Publicaciones
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Registro
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {filteredUsers.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div>
                        <p className="font-medium text-gray-900">{user.username}</p>
                        <p className="text-sm text-gray-500">{user.email}</p>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`px-2 py-1 text-xs font-medium rounded-full ${getRoleBadgeColor(user.role)}`}>
                        {user.role}
                      </span>
                    </td>
                    <td className="px-6 py-4">{getStatusBadge(user.status)}</td>
                    <td className="px-6 py-4 text-gray-700">{user.listingsCount}</td>
                    <td className="px-6 py-4 text-gray-500 text-sm">{formatDate(user.joinedAt)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2 relative">
                        <button
                          onClick={() => setSelectedUser(selectedUser === user.id ? null : user.id)}
                          className="p-2 text-gray-600 hover:bg-gray-100 rounded-lg"
                        >
                          <FiMoreVertical />
                        </button>

                        {/* Dropdown Menu */}
                        {selectedUser === user.id && (
                          <div className="absolute right-0 top-10 bg-white border border-gray-200 rounded-lg shadow-lg py-1 z-10 min-w-[150px]">
                            {user.status === 'active' ? (
                              <button
                                onClick={() => handleBanUser(user.id)}
                                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                              >
                                <FiLock className="w-4 h-4" />
                                Banear
                              </button>
                            ) : (
                              <button
                                onClick={() => handleUnbanUser(user.id)}
                                className="w-full flex items-center gap-2 px-4 py-2 text-sm text-green-600 hover:bg-green-50"
                              >
                                <FiUnlock className="w-4 h-4" />
                                Desbanear
                              </button>
                            )}
                            <button
                              onClick={() => handleDeleteUser(user.id)}
                              className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                            >
                              <FiTrash2 className="w-4 h-4" />
                              Eliminar
                            </button>
                          </div>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default UsersManagementPage;
```

---

## 🛡️ ROLESMANAGEMENTPAGE

**Ruta:** `/admin/roles`

```typescript
// src/pages/admin/RolesManagementPage.tsx
import { useState, useEffect, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  FiShield,
  FiSearch,
  FiPlus,
  FiEdit2,
  FiTrash2,
  FiEye,
  FiRefreshCw,
  FiUsers,
  FiKey,
  FiCheck,
  FiX,
} from 'react-icons/fi';
import roleService from '@/services/roleService';
import type { Role, CreateRoleRequest } from '@/services/roleService';

type FilterStatus = 'all' | 'active' | 'inactive' | 'system';

interface Role {
  id: string;
  name: string;
  displayName: string;
  description: string;
  isActive: boolean;
  isSystem: boolean; // Roles del sistema no se pueden eliminar
  usersCount: number;
  permissionsCount: number;
  createdAt: string;
}

const RolesManagementPage = () => {
  const navigate = useNavigate();
  const [roles, setRoles] = useState<Role[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<FilterStatus>('all');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isCreating, setIsCreating] = useState(false);

  const [newRole, setNewRole] = useState<CreateRoleRequest>({
    name: '',
    displayName: '',
    description: '',
    isActive: true,
  });

  const fetchRoles = async () => {
    setIsLoading(true);
    try {
      const result = await roleService.getRoles({ pageSize: 100 });
      setRoles(result.items);
    } catch (err) {
      console.error('Error fetching roles:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchRoles();
  }, []);

  const filteredRoles = useMemo(() => {
    return roles.filter((role) => {
      const matchesSearch =
        role.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        role.displayName.toLowerCase().includes(searchTerm.toLowerCase());

      let matchesStatus = true;
      if (filterStatus === 'active') matchesStatus = role.isActive;
      else if (filterStatus === 'inactive') matchesStatus = !role.isActive;
      else if (filterStatus === 'system') matchesStatus = role.isSystem;

      return matchesSearch && matchesStatus;
    });
  }, [roles, searchTerm, filterStatus]);

  const handleCreateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsCreating(true);
    try {
      await roleService.createRole(newRole);
      setShowCreateModal(false);
      setNewRole({ name: '', displayName: '', description: '', isActive: true });
      fetchRoles();
    } catch (err) {
      alert('Error al crear el rol');
    } finally {
      setIsCreating(false);
    }
  };

  const handleDeleteRole = async (roleId: string, roleName: string) => {
    if (!confirm(`¿Eliminar el rol "${roleName}"? Esta acción no se puede deshacer.`)) return;

    try {
      await roleService.deleteRole(roleId);
      fetchRoles();
    } catch (err) {
      alert('Error al eliminar el rol. Es posible que tenga usuarios asignados.');
    }
  };

  const stats = useMemo(() => ({
    total: roles.length,
    active: roles.filter((r) => r.isActive).length,
    system: roles.filter((r) => r.isSystem).length,
    custom: roles.filter((r) => !r.isSystem).length,
  }), [roles]);

  return (
    <div>
      {/* Header */}
      <div className="mb-8 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-3 bg-purple-100 rounded-lg">
            <FiShield className="h-8 w-8 text-purple-600" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Gestión de Roles</h1>
            <p className="text-sm text-gray-500">Administra los roles del sistema RBAC</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={fetchRoles}
            className="px-4 py-2 text-gray-600 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center gap-2"
          >
            <FiRefreshCw className={isLoading ? 'animate-spin' : ''} />
            Actualizar
          </button>
          <button
            onClick={() => setShowCreateModal(true)}
            className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 flex items-center gap-2"
          >
            <FiPlus />
            Nuevo Rol
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <StatCard label="Total Roles" value={stats.total} icon={FiShield} color="purple" />
        <StatCard label="Activos" value={stats.active} icon={FiCheck} color="green" />
        <StatCard label="Del Sistema" value={stats.system} icon={FiKey} color="blue" />
        <StatCard label="Personalizados" value={stats.custom} icon={FiUsers} color="orange" />
      </div>

      {/* Search & Filter */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-6">
        <div className="flex flex-col md:flex-row gap-4">
          <div className="flex-1 relative">
            <FiSearch className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar roles..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
            />
          </div>
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value as FilterStatus)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500"
          >
            <option value="all">Todos</option>
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
            <option value="system">Del Sistema</option>
          </select>
        </div>
      </div>

      {/* Roles Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filteredRoles.map((role) => (
          <RoleCard
            key={role.id}
            role={role}
            onView={() => navigate(`/admin/roles/${role.id}`)}
            onEdit={() => navigate(`/admin/roles/${role.id}/edit`)}
            onDelete={() => handleDeleteRole(role.id, role.displayName)}
          />
        ))}
      </div>

      {/* Create Role Modal */}
      {showCreateModal && (
        <CreateRoleModal
          newRole={newRole}
          setNewRole={setNewRole}
          onSubmit={handleCreateRole}
          onClose={() => setShowCreateModal(false)}
          isCreating={isCreating}
        />
      )}
    </div>
  );
};

// RoleCard Component
function RoleCard({
  role,
  onView,
  onEdit,
  onDelete,
}: {
  role: Role;
  onView: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className={`p-2 rounded-lg ${role.isSystem ? 'bg-blue-100' : 'bg-purple-100'}`}>
            <FiShield className={`w-5 h-5 ${role.isSystem ? 'text-blue-600' : 'text-purple-600'}`} />
          </div>
          <div>
            <h3 className="font-semibold text-gray-900">{role.displayName}</h3>
            <p className="text-sm text-gray-500">{role.name}</p>
          </div>
        </div>
        <span
          className={`px-2 py-1 text-xs font-medium rounded-full ${
            role.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'
          }`}
        >
          {role.isActive ? 'Activo' : 'Inactivo'}
        </span>
      </div>

      <p className="text-sm text-gray-600 mb-4 line-clamp-2">{role.description}</p>

      <div className="flex items-center justify-between text-sm text-gray-500 mb-4">
        <span className="flex items-center gap-1">
          <FiUsers className="w-4 h-4" />
          {role.usersCount} usuarios
        </span>
        <span className="flex items-center gap-1">
          <FiKey className="w-4 h-4" />
          {role.permissionsCount} permisos
        </span>
      </div>

      {role.isSystem && (
        <div className="bg-blue-50 text-blue-700 text-xs px-3 py-2 rounded-lg mb-4">
          🔒 Rol del sistema - No se puede eliminar
        </div>
      )}

      <div className="flex items-center gap-2">
        <button
          onClick={onView}
          className="flex-1 flex items-center justify-center gap-2 px-3 py-2 text-sm text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50"
        >
          <FiEye className="w-4 h-4" />
          Ver
        </button>
        <button
          onClick={onEdit}
          className="flex-1 flex items-center justify-center gap-2 px-3 py-2 text-sm text-purple-700 border border-purple-300 rounded-lg hover:bg-purple-50"
        >
          <FiEdit2 className="w-4 h-4" />
          Editar
        </button>
        {!role.isSystem && (
          <button
            onClick={onDelete}
            className="p-2 text-red-600 hover:bg-red-50 rounded-lg"
          >
            <FiTrash2 className="w-4 h-4" />
          </button>
        )}
      </div>
    </div>
  );
}

export default RolesManagementPage;
```

---

## 🔑 PERMISSIONSMANAGEMENTPAGE

**Ruta:** `/admin/permissions`

```typescript
// src/pages/admin/PermissionsManagementPage.tsx
interface Permission {
  id: string;
  name: string;
  displayName: string;
  description: string;
  category: string;
  isActive: boolean;
}

interface PermissionCategory {
  name: string;
  displayName: string;
  permissions: Permission[];
}

// Categorías de permisos
const permissionCategories: PermissionCategory[] = [
  {
    name: "users",
    displayName: "Usuarios",
    permissions: [
      {
        id: "1",
        name: "users.view",
        displayName: "Ver usuarios",
        description: "Ver lista de usuarios",
        category: "users",
        isActive: true,
      },
      {
        id: "2",
        name: "users.create",
        displayName: "Crear usuarios",
        description: "Crear nuevos usuarios",
        category: "users",
        isActive: true,
      },
      {
        id: "3",
        name: "users.edit",
        displayName: "Editar usuarios",
        description: "Editar usuarios existentes",
        category: "users",
        isActive: true,
      },
      {
        id: "4",
        name: "users.delete",
        displayName: "Eliminar usuarios",
        description: "Eliminar usuarios",
        category: "users",
        isActive: true,
      },
      {
        id: "5",
        name: "users.ban",
        displayName: "Banear usuarios",
        description: "Banear/desbanear usuarios",
        category: "users",
        isActive: true,
      },
    ],
  },
  {
    name: "vehicles",
    displayName: "Vehículos",
    permissions: [
      {
        id: "6",
        name: "vehicles.view",
        displayName: "Ver vehículos",
        description: "Ver todos los vehículos",
        category: "vehicles",
        isActive: true,
      },
      {
        id: "7",
        name: "vehicles.approve",
        displayName: "Aprobar vehículos",
        description: "Aprobar publicaciones",
        category: "vehicles",
        isActive: true,
      },
      {
        id: "8",
        name: "vehicles.reject",
        displayName: "Rechazar vehículos",
        description: "Rechazar publicaciones",
        category: "vehicles",
        isActive: true,
      },
      {
        id: "9",
        name: "vehicles.delete",
        displayName: "Eliminar vehículos",
        description: "Eliminar vehículos",
        category: "vehicles",
        isActive: true,
      },
    ],
  },
  {
    name: "dealers",
    displayName: "Dealers",
    permissions: [
      {
        id: "10",
        name: "dealers.view",
        displayName: "Ver dealers",
        description: "Ver lista de dealers",
        category: "dealers",
        isActive: true,
      },
      {
        id: "11",
        name: "dealers.verify",
        displayName: "Verificar dealers",
        description: "Verificar documentos",
        category: "dealers",
        isActive: true,
      },
      {
        id: "12",
        name: "dealers.suspend",
        displayName: "Suspender dealers",
        description: "Suspender cuentas",
        category: "dealers",
        isActive: true,
      },
    ],
  },
  {
    name: "billing",
    displayName: "Facturación",
    permissions: [
      {
        id: "13",
        name: "billing.view",
        displayName: "Ver facturación",
        description: "Ver facturas y pagos",
        category: "billing",
        isActive: true,
      },
      {
        id: "14",
        name: "billing.refund",
        displayName: "Reembolsos",
        description: "Procesar reembolsos",
        category: "billing",
        isActive: true,
      },
    ],
  },
  {
    name: "system",
    displayName: "Sistema",
    permissions: [
      {
        id: "15",
        name: "system.settings",
        displayName: "Configuración",
        description: "Modificar configuración",
        category: "system",
        isActive: true,
      },
      {
        id: "16",
        name: "system.roles",
        displayName: "Gestión de roles",
        description: "Administrar roles",
        category: "system",
        isActive: true,
      },
      {
        id: "17",
        name: "system.logs",
        displayName: "Ver logs",
        description: "Ver logs del sistema",
        category: "system",
        isActive: true,
      },
    ],
  },
];
```

---

## 🔌 SERVICIOS API

```typescript
// src/services/roleService.ts
import api from "./api";

export interface Role {
  id: string;
  name: string;
  displayName: string;
  description: string;
  isActive: boolean;
  isSystem: boolean;
  usersCount: number;
  permissionsCount: number;
  permissions: string[];
  createdAt: string;
}

export interface CreateRoleRequest {
  name: string;
  displayName: string;
  description: string;
  isActive: boolean;
  permissions?: string[];
}

const roleService = {
  getRoles: async (params?: { pageSize?: number }) => {
    const response = await api.get("/api/roles", { params });
    return response.data;
  },

  getRole: async (id: string): Promise<Role> => {
    const response = await api.get(`/api/roles/${id}`);
    return response.data;
  },

  createRole: async (data: CreateRoleRequest): Promise<Role> => {
    const response = await api.post("/api/roles", data);
    return response.data;
  },

  updateRole: async (
    id: string,
    data: Partial<CreateRoleRequest>,
  ): Promise<Role> => {
    const response = await api.put(`/api/roles/${id}`, data);
    return response.data;
  },

  deleteRole: async (id: string): Promise<void> => {
    await api.delete(`/api/roles/${id}`);
  },

  assignPermissions: async (
    roleId: string,
    permissions: string[],
  ): Promise<void> => {
    await api.post(`/api/roles/${roleId}/permissions`, { permissions });
  },
};

export default roleService;

// src/hooks/useUsersManagement.ts
export function useUsersManagement() {
  const query = useQuery({
    queryKey: ["admin", "users"],
    queryFn: () => adminService.getUsers({ pageSize: 100 }),
  });

  return {
    users: query.data?.items || [],
    total: query.data?.totalCount || 0,
    isLoading: query.isLoading,
    refetch: query.refetch,
  };
}

export function useBanUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      adminService.banUser(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
    },
  });
}

export function useUnbanUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => adminService.unbanUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
    },
  });
}

export function useDeleteUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => adminService.deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
    },
  });
}
```

---

## ✅ VALIDACIÓN

- [ ] Tabla de usuarios con búsqueda
- [ ] Filtros por status (all, active, banned, suspended)
- [ ] Acciones: ban, unban, delete
- [ ] Dropdown menu con confirmación
- [ ] Grid de roles con cards
- [ ] Crear rol modal
- [ ] Roles del sistema no eliminables
- [ ] Matriz de permisos por categoría
- [ ] RBAC funcionando correctamente

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/users-roles-management.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Users & Roles Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test.describe("Roles", () => {
    test("debe mostrar lista de roles", async ({ page }) => {
      await page.goto("/admin/roles");

      await expect(page.getByTestId("roles-grid")).toBeVisible();
    });

    test("debe crear nuevo rol", async ({ page }) => {
      await page.goto("/admin/roles");

      await page.getByRole("button", { name: /crear rol/i }).click();
      await page.fill('input[name="name"]', "Moderador Sr");
      await page.getByRole("checkbox", { name: /aprobar listings/i }).click();
      await page.getByRole("button", { name: /guardar/i }).click();

      await expect(page.getByText(/rol creado/i)).toBeVisible();
    });

    test("no debe poder eliminar roles del sistema", async ({ page }) => {
      await page.goto("/admin/roles");

      const systemRole = page.getByTestId("role-admin");
      await expect(
        systemRole.getByRole("button", { name: /eliminar/i }),
      ).toBeDisabled();
    });
  });

  test.describe("Permisos", () => {
    test("debe mostrar matriz de permisos", async ({ page }) => {
      await page.goto("/admin/roles/permissions");

      await expect(page.getByTestId("permissions-matrix")).toBeVisible();
    });

    test("debe editar permisos de rol", async ({ page }) => {
      await page.goto("/admin/roles");
      await page.getByTestId("role-moderator").click();

      await page.getByRole("checkbox", { name: /ver analytics/i }).click();
      await page.getByRole("button", { name: /guardar/i }).click();

      await expect(page.getByText(/permisos actualizados/i)).toBeVisible();
    });
  });
});
```

---

_Última actualización: Enero 2026_
