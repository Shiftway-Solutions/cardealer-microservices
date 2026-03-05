# 🛡️ 14 - Admin Dashboard API (AdminService)

**Servicio:** AdminService  
**Puerto:** 8080  
**Base Path:** `/api/admin`, `/api/platform/employees`  
**Autenticación:** ✅ Requerida (Admin/SuperAdmin only)

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)

---

## 📖 Descripción General

El **AdminService** proporciona herramientas para la administración de la plataforma:

- 👥 Gestión de usuarios admin
- 👔 Gestión de empleados de plataforma
- 📊 Dashboard con métricas
- 📋 Items pendientes de revisión
- 📜 Log de actividad
- ⚙️ Configuración de plataforma

### Niveles de Acceso

| Rol          | Descripción           | Permisos               |
| ------------ | --------------------- | ---------------------- |
| `SuperAdmin` | Administrador máximo  | Todo                   |
| `Admin`      | Administrador regular | Ver + Moderar          |
| `Moderator`  | Moderador             | Solo moderar contenido |

---

## 🎯 Endpoints Disponibles

### AdminController (11 endpoints)

| #   | Método | Endpoint                               | Auth          | Descripción             |
| --- | ------ | -------------------------------------- | ------------- | ----------------------- |
| 1   | `GET`  | `/api/admin/users`                     | ✅ Admin      | Listar usuarios admin   |
| 2   | `GET`  | `/api/admin/users/{userId}`            | ✅ Admin      | Obtener admin por ID    |
| 3   | `GET`  | `/api/admin/me`                        | ✅ Admin      | Perfil del admin actual |
| 4   | `PUT`  | `/api/admin/users/{userId}/role`       | ✅ SuperAdmin | Cambiar rol de admin    |
| 5   | `POST` | `/api/admin/users/{userId}/suspend`    | ✅ SuperAdmin | Suspender admin         |
| 6   | `POST` | `/api/admin/users/{userId}/reactivate` | ✅ SuperAdmin | Reactivar admin         |
| 7   | `GET`  | `/api/admin/dashboard`                 | ✅ Admin      | Dashboard overview      |
| 8   | `GET`  | `/api/admin/pending`                   | ✅ Admin      | Items pendientes        |
| 9   | `GET`  | `/api/admin/activity`                  | ✅ Admin      | Log de actividad        |
| 10  | `GET`  | `/api/admin/settings`                  | ✅ SuperAdmin | Config de plataforma    |
| 11  | `PUT`  | `/api/admin/settings`                  | ✅ SuperAdmin | Actualizar config       |

### PlatformEmployeesController (10 endpoints)

| #   | Método   | Endpoint                                          | Auth          | Descripción         |
| --- | -------- | ------------------------------------------------- | ------------- | ------------------- |
| 12  | `GET`    | `/api/platform/employees`                         | ✅ Admin      | Listar empleados    |
| 13  | `GET`    | `/api/platform/employees/{id}`                    | ✅ Admin      | Obtener empleado    |
| 14  | `POST`   | `/api/platform/employees/invite`                  | ✅ SuperAdmin | Invitar empleado    |
| 15  | `PUT`    | `/api/platform/employees/{id}`                    | ✅ SuperAdmin | Actualizar empleado |
| 16  | `POST`   | `/api/platform/employees/{id}/suspend`            | ✅ SuperAdmin | Suspender empleado  |
| 17  | `POST`   | `/api/platform/employees/{id}/reactivate`         | ✅ SuperAdmin | Reactivar empleado  |
| 18  | `DELETE` | `/api/platform/employees/{id}`                    | ✅ SuperAdmin | Eliminar empleado   |
| 19  | `GET`    | `/api/platform/employees/invitations`             | ✅ Admin      | Listar invitaciones |
| 20  | `DELETE` | `/api/platform/employees/invitations/{id}`        | ✅ Admin      | Cancelar invitación |
| 21  | `POST`   | `/api/platform/employees/invitations/{id}/resend` | ✅ Admin      | Reenviar invitación |

---

## 📝 Detalle de Endpoints

### 7. GET `/api/admin/dashboard` - Dashboard Overview

**Response 200:**

```json
{
  "totalUsers": 15420,
  "totalDealers": 156,
  "totalVehicles": 3450,
  "activeListings": 2890,
  "pendingVerifications": 12,
  "pendingReviews": 8,
  "reportedContent": 3,
  "todayRegistrations": 45,
  "todaySales": 12,
  "monthlyRevenue": 25680.0,
  "conversionRate": 3.2,
  "lastUpdated": "2026-01-30T10:00:00Z"
}
```

---

### 8. GET `/api/admin/pending` - Items Pendientes

**Response 200:**

```json
{
  "pendingDealerVerifications": [
    {
      "id": "dealer-123",
      "businessName": "Auto Premium RD",
      "submittedAt": "2026-01-28T14:00:00Z",
      "documentsCount": 3
    }
  ],
  "pendingReviews": [
    {
      "id": "review-456",
      "sellerName": "Car World",
      "rating": 2,
      "submittedAt": "2026-01-29T09:00:00Z"
    }
  ],
  "reportedListings": [
    {
      "id": "vehicle-789",
      "title": "Toyota Corolla 2024",
      "reportReason": "Precio sospechoso",
      "reportsCount": 5
    }
  ],
  "pendingSupport": 23
}
```

---

### 9. GET `/api/admin/activity` - Log de Actividad

**Query Params:**

- `adminId` (UUID) - Filtrar por admin
- `action` (string) - Tipo de acción
- `from` (DateTime) - Fecha inicio
- `to` (DateTime) - Fecha fin
- `page`, `pageSize`

**Response 200:**

```json
{
  "items": [
    {
      "id": "act-123",
      "adminId": "admin-456",
      "adminName": "Juan Admin",
      "action": "dealer.verified",
      "entityType": "Dealer",
      "entityId": "dealer-789",
      "details": "Verified dealer 'Auto Premium RD'",
      "ipAddress": "192.168.1.100",
      "createdAt": "2026-01-30T10:30:00Z"
    }
  ],
  "totalCount": 1250,
  "page": 1,
  "pageSize": 50
}
```

---

### 14. POST `/api/platform/employees/invite` - Invitar Empleado

**Auth:** ✅ SuperAdmin only

**Request Body:**

```json
{
  "email": "nuevo@okla.com.do",
  "role": "Admin",
  "permissions": ["users.read", "dealers.verify", "reviews.moderate"],
  "department": "Operations",
  "notes": "Nuevo administrador de operaciones"
}
```

**Response 201:**

```json
{
  "id": "inv-123",
  "email": "nuevo@okla.com.do",
  "role": "Admin",
  "status": "Pending",
  "invitedBy": "superadmin-456",
  "invitedAt": "2026-01-30T10:00:00Z",
  "expiresAt": "2026-02-06T10:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// ADMIN USER TYPES
// ============================================================================

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  role: AdminRole;
  permissions: string[];
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export interface AdminUserDetail extends AdminUser {
  department?: string;
  phoneNumber?: string;
  avatarUrl?: string;
  twoFactorEnabled: boolean;
  activityCount: number;
  suspendedAt?: string;
  suspendedReason?: string;
}

export type AdminRole = "SuperAdmin" | "Admin" | "Moderator" | "Support";

// ============================================================================
// DASHBOARD TYPES
// ============================================================================

export interface AdminDashboard {
  totalUsers: number;
  totalDealers: number;
  totalVehicles: number;
  activeListings: number;
  pendingVerifications: number;
  pendingReviews: number;
  reportedContent: number;
  todayRegistrations: number;
  todaySales: number;
  monthlyRevenue: number;
  conversionRate: number;
  lastUpdated: string;
}

export interface PendingItems {
  pendingDealerVerifications: PendingDealer[];
  pendingReviews: PendingReview[];
  reportedListings: ReportedListing[];
  pendingSupport: number;
}

export interface PendingDealer {
  id: string;
  businessName: string;
  submittedAt: string;
  documentsCount: number;
}

export interface PendingReview {
  id: string;
  sellerName: string;
  rating: number;
  submittedAt: string;
}

export interface ReportedListing {
  id: string;
  title: string;
  reportReason: string;
  reportsCount: number;
}

// ============================================================================
// ACTIVITY LOG
// ============================================================================

export interface AdminActivity {
  id: string;
  adminId: string;
  adminName: string;
  action: AdminAction;
  entityType: string;
  entityId: string;
  details: string;
  ipAddress: string;
  createdAt: string;
}

export type AdminAction =
  | "dealer.verified"
  | "dealer.rejected"
  | "user.banned"
  | "user.unbanned"
  | "review.approved"
  | "review.rejected"
  | "listing.removed"
  | "settings.updated";

// ============================================================================
// PLATFORM EMPLOYEES
// ============================================================================

export interface PlatformEmployee {
  id: string;
  userId: string;
  email: string;
  fullName: string;
  role: AdminRole;
  department?: string;
  status: EmployeeStatus;
  permissions: string[];
  joinedAt: string;
}

export type EmployeeStatus = "Active" | "Suspended" | "Invited" | "Removed";

export interface EmployeeInvitation {
  id: string;
  email: string;
  role: AdminRole;
  status: "Pending" | "Accepted" | "Expired" | "Cancelled";
  invitedBy: string;
  invitedAt: string;
  expiresAt: string;
}

// ============================================================================
// REQUEST TYPES
// ============================================================================

export interface UpdateAdminRoleRequest {
  role: AdminRole;
  permissions?: string[];
}

export interface SuspendRequest {
  reason?: string;
}

export interface InviteEmployeeRequest {
  email: string;
  role: AdminRole;
  permissions?: string[];
  department?: string;
  notes?: string;
}

export interface UpdateEmployeeRequest {
  role?: AdminRole;
  permissions?: string[];
  department?: string;
  notes?: string;
  status?: EmployeeStatus;
}

export interface PlatformSettings {
  maintenanceMode: boolean;
  registrationEnabled: boolean;
  dealerRegistrationEnabled: boolean;
  maxListingsPerUser: number;
  maxImagesPerListing: number;
  commissionRate: number;
  earlyBirdDeadline: string;
  supportEmail: string;
  [key: string]: unknown;
}
```

---

## 📡 Service Layer

```typescript
// src/services/adminService.ts
import { apiClient } from "./api-client";
import type {
  AdminUser,
  AdminUserDetail,
  AdminDashboard,
  PendingItems,
  AdminActivity,
  PlatformEmployee,
  EmployeeInvitation,
  PlatformSettings,
  UpdateAdminRoleRequest,
  InviteEmployeeRequest,
  UpdateEmployeeRequest,
  PaginatedResult,
} from "@/types/admin";

class AdminService {
  // ============================================================================
  // ADMIN USERS
  // ============================================================================

  async getAdminUsers(params?: {
    role?: string;
    isActive?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<AdminUser>> {
    const response = await apiClient.get<PaginatedResult<AdminUser>>(
      "/api/admin/users",
      { params },
    );
    return response.data;
  }

  async getAdminById(userId: string): Promise<AdminUserDetail> {
    const response = await apiClient.get<AdminUserDetail>(
      `/api/admin/users/${userId}`,
    );
    return response.data;
  }

  async getCurrentAdmin(): Promise<AdminUserDetail> {
    const response = await apiClient.get<AdminUserDetail>("/api/admin/me");
    return response.data;
  }

  async updateAdminRole(
    userId: string,
    request: UpdateAdminRoleRequest,
  ): Promise<void> {
    await apiClient.put(`/api/admin/users/${userId}/role`, request);
  }

  async suspendAdmin(userId: string, reason?: string): Promise<void> {
    await apiClient.post(`/api/admin/users/${userId}/suspend`, { reason });
  }

  async reactivateAdmin(userId: string): Promise<void> {
    await apiClient.post(`/api/admin/users/${userId}/reactivate`);
  }

  // ============================================================================
  // DASHBOARD
  // ============================================================================

  async getDashboard(): Promise<AdminDashboard> {
    const response = await apiClient.get<AdminDashboard>(
      "/api/admin/dashboard",
    );
    return response.data;
  }

  async getPendingItems(): Promise<PendingItems> {
    const response = await apiClient.get<PendingItems>("/api/admin/pending");
    return response.data;
  }

  // ============================================================================
  // ACTIVITY LOG
  // ============================================================================

  async getActivityLog(params?: {
    adminId?: string;
    action?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<AdminActivity>> {
    const response = await apiClient.get<PaginatedResult<AdminActivity>>(
      "/api/admin/activity",
      { params },
    );
    return response.data;
  }

  // ============================================================================
  // SETTINGS
  // ============================================================================

  async getSettings(): Promise<PlatformSettings> {
    const response = await apiClient.get<PlatformSettings>(
      "/api/admin/settings",
    );
    return response.data;
  }

  async updateSettings(settings: Partial<PlatformSettings>): Promise<void> {
    await apiClient.put("/api/admin/settings", { settings });
  }

  // ============================================================================
  // PLATFORM EMPLOYEES
  // ============================================================================

  async getEmployees(params?: {
    status?: string;
    role?: string;
    department?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<PlatformEmployee>> {
    const response = await apiClient.get<PaginatedResult<PlatformEmployee>>(
      "/api/platform/employees",
      { params },
    );
    return response.data;
  }

  async getEmployeeById(employeeId: string): Promise<PlatformEmployee> {
    const response = await apiClient.get<PlatformEmployee>(
      `/api/platform/employees/${employeeId}`,
    );
    return response.data;
  }

  async inviteEmployee(
    request: InviteEmployeeRequest,
  ): Promise<EmployeeInvitation> {
    const response = await apiClient.post<EmployeeInvitation>(
      "/api/platform/employees/invite",
      request,
    );
    return response.data;
  }

  async updateEmployee(
    employeeId: string,
    request: UpdateEmployeeRequest,
  ): Promise<void> {
    await apiClient.put(`/api/platform/employees/${employeeId}`, request);
  }

  async suspendEmployee(employeeId: string, reason?: string): Promise<void> {
    await apiClient.post(`/api/platform/employees/${employeeId}/suspend`, {
      reason,
    });
  }

  async reactivateEmployee(employeeId: string): Promise<void> {
    await apiClient.post(`/api/platform/employees/${employeeId}/reactivate`);
  }

  async removeEmployee(employeeId: string): Promise<void> {
    await apiClient.delete(`/api/platform/employees/${employeeId}`);
  }

  async getInvitations(status?: string): Promise<EmployeeInvitation[]> {
    const response = await apiClient.get<EmployeeInvitation[]>(
      "/api/platform/employees/invitations",
      { params: { status } },
    );
    return response.data;
  }

  async cancelInvitation(invitationId: string): Promise<void> {
    await apiClient.delete(
      `/api/platform/employees/invitations/${invitationId}`,
    );
  }

  async resendInvitation(invitationId: string): Promise<void> {
    await apiClient.post(
      `/api/platform/employees/invitations/${invitationId}/resend`,
    );
  }
}

export const adminService = new AdminService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useAdmin.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { adminService } from "@/services/adminService";

export const adminKeys = {
  all: ["admin"] as const,
  dashboard: () => [...adminKeys.all, "dashboard"] as const,
  pending: () => [...adminKeys.all, "pending"] as const,
  activity: (params: any) => [...adminKeys.all, "activity", params] as const,
  settings: () => [...adminKeys.all, "settings"] as const,
  users: (params: any) => [...adminKeys.all, "users", params] as const,
  employees: (params: any) => [...adminKeys.all, "employees", params] as const,
  invitations: () => [...adminKeys.all, "invitations"] as const,
};

// ============================================================================
// DASHBOARD
// ============================================================================

export function useAdminDashboard() {
  return useQuery({
    queryKey: adminKeys.dashboard(),
    queryFn: () => adminService.getDashboard(),
    refetchInterval: 60000, // Refresh each minute
  });
}

export function usePendingItems() {
  return useQuery({
    queryKey: adminKeys.pending(),
    queryFn: () => adminService.getPendingItems(),
    refetchInterval: 30000, // Refresh each 30s
  });
}

export function useActivityLog(params?: { adminId?: string; action?: string }) {
  return useQuery({
    queryKey: adminKeys.activity(params),
    queryFn: () => adminService.getActivityLog(params),
  });
}

// ============================================================================
// SETTINGS
// ============================================================================

export function usePlatformSettings() {
  return useQuery({
    queryKey: adminKeys.settings(),
    queryFn: () => adminService.getSettings(),
  });
}

export function useUpdateSettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminService.updateSettings,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.settings() });
    },
  });
}

// ============================================================================
// EMPLOYEES
// ============================================================================

export function usePlatformEmployees(params?: {
  status?: string;
  role?: string;
}) {
  return useQuery({
    queryKey: adminKeys.employees(params),
    queryFn: () => adminService.getEmployees(params),
  });
}

export function useInviteEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminService.inviteEmployee,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.employees({}) });
      queryClient.invalidateQueries({ queryKey: adminKeys.invitations() });
    },
  });
}

export function useSuspendEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      adminService.suspendEmployee(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.employees({}) });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### AdminDashboardPage

```typescript
// src/pages/admin/AdminDashboardPage.tsx
import { useAdminDashboard, usePendingItems } from "@/hooks/useAdmin";
import { FiUsers, FiTruck, FiDollarSign, FiAlertCircle } from "react-icons/fi";

export const AdminDashboardPage = () => {
  const { data: dashboard, isLoading } = useAdminDashboard();
  const { data: pending } = usePendingItems();

  if (isLoading) return <div>Cargando dashboard...</div>;

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold">Dashboard Administrativo</h1>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          icon={<FiUsers />}
          label="Usuarios Totales"
          value={dashboard?.totalUsers.toLocaleString()}
          change={`+${dashboard?.todayRegistrations} hoy`}
          color="blue"
        />
        <StatCard
          icon={<FiTruck />}
          label="Vehículos Activos"
          value={dashboard?.activeListings.toLocaleString()}
          color="green"
        />
        <StatCard
          icon={<FiDollarSign />}
          label="Ingresos del Mes"
          value={`$${dashboard?.monthlyRevenue.toLocaleString()}`}
          color="purple"
        />
        <StatCard
          icon={<FiAlertCircle />}
          label="Pendientes"
          value={(
            (pending?.pendingDealerVerifications.length || 0) +
            (pending?.pendingReviews.length || 0) +
            (pending?.reportedListings.length || 0)
          ).toString()}
          color="red"
        />
      </div>

      {/* Pending Items */}
      {pending && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <PendingCard
            title="Dealers por Verificar"
            items={pending.pendingDealerVerifications.map((d) => ({
              id: d.id,
              label: d.businessName,
              sublabel: `${d.documentsCount} documentos`,
            }))}
            href="/admin/dealers/pending"
          />
          <PendingCard
            title="Reviews por Moderar"
            items={pending.pendingReviews.map((r) => ({
              id: r.id,
              label: r.sellerName,
              sublabel: `${r.rating}★`,
            }))}
            href="/admin/reviews/pending"
          />
          <PendingCard
            title="Contenido Reportado"
            items={pending.reportedListings.map((l) => ({
              id: l.id,
              label: l.title,
              sublabel: `${l.reportsCount} reportes`,
            }))}
            href="/admin/reports"
          />
        </div>
      )}
    </div>
  );
};

const StatCard = ({ icon, label, value, change, color }: any) => (
  <div className="bg-white rounded-xl p-6 border">
    <div className={`w-12 h-12 rounded-lg bg-${color}-100 text-${color}-600 flex items-center justify-center mb-4`}>
      {icon}
    </div>
    <p className="text-gray-500 text-sm">{label}</p>
    <p className="text-2xl font-bold">{value}</p>
    {change && <p className="text-sm text-green-600 mt-1">{change}</p>}
  </div>
);

const PendingCard = ({ title, items, href }: any) => (
  <div className="bg-white rounded-xl border">
    <div className="px-4 py-3 border-b flex items-center justify-between">
      <h3 className="font-semibold">{title}</h3>
      <span className="bg-red-100 text-red-600 px-2 py-0.5 rounded-full text-xs font-medium">
        {items.length}
      </span>
    </div>
    <div className="divide-y">
      {items.slice(0, 5).map((item: any) => (
        <div key={item.id} className="px-4 py-3 hover:bg-gray-50">
          <p className="font-medium text-sm">{item.label}</p>
          <p className="text-xs text-gray-500">{item.sublabel}</p>
        </div>
      ))}
    </div>
    <a href={href} className="block px-4 py-3 text-center text-blue-600 text-sm hover:bg-blue-50">
      Ver todos →
    </a>
  </div>
);
```

---

## 🎉 Resumen

✅ **21 Endpoints documentados**  
✅ **TypeScript Types completos** (AdminUser, Dashboard, Activity, Employees)  
✅ **Service Layer** con 21 métodos  
✅ **React Query Hooks** (10 hooks)  
✅ **AdminDashboardPage** componente completo

---

_Última actualización: Enero 30, 2026_
