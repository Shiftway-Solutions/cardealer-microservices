---
title: "82 - Dealer Employees & Locations Pages"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 82 - Dealer Employees & Locations Pages

## Descripción General

Este documento cubre las páginas de gestión de empleados y ubicaciones del dealer. Incluye la lista de miembros del equipo, gestión de permisos granulares por módulo, e invitaciones pendientes. También incluye la gestión de sucursales y puntos de venta con ubicaciones geográficas.

---

## Arquitectura de Páginas

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     DEALER EMPLOYEES & LOCATIONS                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                  DealerEmployeesPage                                 │   │
│  │  /dealer/employees                                                   │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Stats Cards (4)                                               │ │   │
│  │  │  [Total] [Activos] [Pendientes] [Suspendidos]                  │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Filters: [Search] [Role] [Status]                             │ │   │
│  │  │  Tabs: [Empleados] [Invitaciones]                              │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Table: Employee Rows                                          │ │   │
│  │  │  [Avatar] [Name/Email] [Role Badge] [Status] [Date] [Actions]  │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  Modal: InviteModal                                                  │   │
│  │  [Email] [Role Selector] [Send Invitation]                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │              DealerEmployeePermissionsPage                           │   │
│  │  /dealer/employees/:employeeId/permissions                           │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Employee Info Card                                            │ │   │
│  │  │  [Avatar] [Name] [Current Role] [Permission Stats]             │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Role Selector                                                 │ │   │
│  │  │  [Admin] [SalesManager] [Salesperson] [Inventory] [Viewer]     │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Permission Modules (7 modules)                                │ │   │
│  │  │  Inventario | Leads | Ventas | Analytics | Mensajes | Billing  │ │   │
│  │  │  [Permission Toggle] [Description] [Enable/Disable]            │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      LocationsPage                                   │   │
│  │  /dealer/locations                                                   │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Stats Summary (4)                                             │ │   │
│  │  │  [Ubicaciones] [Activas] [Vehículos Total] [Provincias]        │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Location Cards Grid                                           │ │   │
│  │  │  [Name] [Type Badge] [Primary Star] [Address] [Phone]          │ │   │
│  │  │  [Hours] [Vehicle Count] [Edit] [Delete] [Set Primary]         │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  Modal: Location Form                                                │   │
│  │  [Name] [Type] [Address] [City] [Province] [Phone] [Email] [Hours]  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Tipos TypeScript

### Employee Types

```typescript
// From dealerSettingsService
type DealerRole =
  | "Owner"
  | "Admin"
  | "SalesManager"
  | "Salesperson"
  | "InventoryManager"
  | "Viewer";

interface DealerEmployee {
  id: string;
  userId: string;
  dealerId: string;
  name: string;
  email: string;
  avatarUrl?: string;
  role: DealerRole;
  status: "Active" | "Pending" | "Suspended";
  activationDate?: string;
  createdAt: string;
}

interface DealerEmployeeInvitation {
  id: string;
  email: string;
  role: string;
  expirationDate: string;
  invitedBy: string;
  createdAt: string;
}

interface RoleDefinition {
  role: DealerRole;
  name: string;
  description: string;
  permissions: string[];
}

// Local stats interface
interface EmployeeStats {
  total: number;
  active: number;
  pending: number;
  suspended: number;
}
```

### Permission Types

```typescript
interface Permission {
  id: string; // e.g., "inventory:view"
  name: string; // e.g., "Ver inventario"
  description: string; // e.g., "Ver lista de vehículos"
  module: string; // e.g., "inventory"
  enabled: boolean;
}

interface PermissionModule {
  id: string; // e.g., "inventory"
  name: string; // e.g., "Inventario"
  icon: React.ElementType; // FiTruck
  permissions: Permission[];
}
```

### Location Types

```typescript
interface DealerLocation {
  id: string;
  name: string;
  type: "Headquarters" | "Branch" | "Showroom" | "ServiceCenter" | "Warehouse";
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  phone?: string;
  email?: string;
  isPrimary: boolean;
  isActive: boolean;
  openingHours?: string;
  vehicleCount: number;
  latitude?: number;
  longitude?: number;
  createdAt: string;
}

// From dealerService
interface DealerManagedLocation {
  id: string;
  name: string;
  type: DealerLocation["type"];
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  phone?: string;
  email?: string;
  isPrimary: boolean;
  isActive: boolean;
  openingHours?: string;
  vehicleCount?: number;
  latitude?: number;
  longitude?: number;
  createdAt: string;
}

interface CreateLocationRequest {
  name: string;
  type: DealerLocation["type"];
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  phone?: string;
  email?: string;
  openingHours?: string;
  isPrimary: boolean;
  isActive: boolean;
}

interface LocationsPageProps {
  dealerId: string;
}
```

---

## Componentes

### 1. DealerEmployeesPage (819 líneas)

**Ubicación:** `src/pages/dealer/DealerEmployeesPage.tsx`

**Layout:** DealerPortalLayout

**Características:**

- Lista de empleados con tabla
- Invitaciones pendientes en tab separado
- Modal para invitar nuevos miembros
- Filtros por rol, estado y búsqueda
- Stats cards con conteos
- Manejo de permisos (solo Owner puede editar)

**State Management:**

```typescript
const [loading, setLoading] = useState(true);
const [saving, setSaving] = useState(false);
const [error, setError] = useState<string | null>(null);
const [successMessage, setSuccessMessage] = useState<string | null>(null);

// Data states
const [employees, setEmployees] = useState<DealerEmployee[]>([]);
const [invitations, setInvitations] = useState<DealerEmployeeInvitation[]>([]);
const [_availableRoles, setAvailableRoles] = useState<RoleDefinition[]>([]);
const [stats, setStats] = useState<EmployeeStats>({...});

// UI states
const [showInviteModal, setShowInviteModal] = useState(false);
const [searchTerm, setSearchTerm] = useState('');
const [filterRole, setFilterRole] = useState<string>('all');
const [filterStatus, setFilterStatus] = useState<string>('all');
const [activeTab, setActiveTab] = useState<'employees' | 'invitations'>('employees');
```

**Role Badge Colors:**

| Role             | Color  | Label             |
| ---------------- | ------ | ----------------- |
| Owner            | purple | Dueño             |
| Admin            | blue   | Administrador     |
| SalesManager     | green  | Gerente de Ventas |
| Salesperson      | yellow | Vendedor          |
| InventoryManager | orange | Inventario        |
| Viewer           | gray   | Visualizador      |

**Status Badge Colors:**

| Status    | Color  | Icon    |
| --------- | ------ | ------- |
| Active    | green  | FiCheck |
| Pending   | yellow | FiClock |
| Suspended | red    | FiX     |

**Role Descriptions (Invite Modal):**

```typescript
{
  role === "Admin" && "Acceso completo a todas las funciones del dealer";
}
{
  role === "SalesManager" && "Gestión de ventas, leads y equipo de ventas";
}
{
  role === "Salesperson" && "Acceso a inventario y leads asignados";
}
{
  role === "InventoryManager" && "Gestión de inventario y vehículos";
}
{
  role === "Viewer" && "Solo puede ver información, sin editar";
}
```

---

### 2. DealerEmployeePermissionsPage (820 líneas)

**Ubicación:** `src/pages/dealer/DealerEmployeePermissionsPage.tsx`

**Layout:** DealerPortalLayout

**Características:**

- Vista detallada de empleado
- Selector de rol con presets
- Permisos granulares por módulo
- Toggle switches para cada permiso
- Unsaved changes tracking

**Permission Modules (7 total):**

| Module        | Icon            | Permissions                          |
| ------------- | --------------- | ------------------------------------ |
| Inventario    | FiTruck         | view, create, edit, delete, import   |
| Leads/CRM     | FiUsers         | view, view_all, edit, assign, delete |
| Ventas        | FiDollarSign    | view, create, edit                   |
| Analytics     | FiBarChart2     | view, export, advanced               |
| Mensajería    | FiMessageSquare | view, reply, view_all                |
| Facturación   | FiFileText      | view, manage                         |
| Configuración | FiSettings      | view, edit, team:manage              |

**Permission ID Format:**

```
{module}:{action}

Examples:
- inventory:view
- inventory:create
- leads:view_all
- analytics:export
- team:manage
```

**Role Presets:**

```typescript
const getRolePreset = (role: DealerRole): string[] => {
  switch (role) {
    case "Owner":
    case "Admin":
      // All permissions
      return getDefaultPermissions()
        .flatMap((m) => m.permissions)
        .map((p) => p.id);

    case "SalesManager":
      return [
        "inventory:view",
        "inventory:create",
        "inventory:edit",
        "leads:view",
        "leads:view_all",
        "leads:edit",
        "leads:assign",
        "sales:view",
        "sales:create",
        "sales:edit",
        "analytics:view",
        "analytics:export",
        "analytics:advanced",
        "messages:view",
        "messages:reply",
        "messages:view_all",
      ];

    case "Salesperson":
      return [
        "inventory:view",
        "leads:view",
        "leads:edit",
        "sales:view",
        "sales:create",
        "analytics:view",
        "messages:view",
        "messages:reply",
      ];

    case "InventoryManager":
      return [
        "inventory:view",
        "inventory:create",
        "inventory:edit",
        "inventory:delete",
        "inventory:import",
        "analytics:view",
      ];

    case "Viewer":
      return [
        "inventory:view",
        "leads:view",
        "sales:view",
        "analytics:view",
        "messages:view",
      ];
  }
};
```

**Permission Toggle Component:**

```typescript
<PermissionToggle
  permission={permission}
  onChange={(permissionId, enabled) => handlePermissionChange(permissionId, enabled)}
  disabled={isOwnerEmployee}
/>
```

---

### 3. LocationsPage (621 líneas)

**Ubicación:** `src/pages/dealer/LocationsPage.tsx`

**Layout:** MainLayout

**Características:**

- Grid de cards de ubicaciones
- Modal para crear/editar
- Dropdown de provincias dominicanas
- Set primary location
- Stats summary (4 cards)
- Active/inactive toggle

**Props:**

```typescript
interface LocationsPageProps {
  dealerId: string;
}
```

**Location Types:**

| Type          | Label              | Color  |
| ------------- | ------------------ | ------ |
| Headquarters  | Sede Principal     | purple |
| Branch        | Sucursal           | blue   |
| Showroom      | Showroom           | green  |
| ServiceCenter | Centro de Servicio | yellow |
| Warehouse     | Almacén            | gray   |

**Form Data:**

```typescript
const [formData, setFormData] = useState({
  name: "",
  type: "Showroom" as DealerLocation["type"],
  address: "",
  city: "",
  province: "",
  postalCode: "",
  phone: "",
  email: "",
  openingHours: "",
  isPrimary: false,
  isActive: true,
});
```

**Dominican Provinces (10 shown):**

- Distrito Nacional
- Santiago
- La Altagracia
- Puerto Plata
- San Cristóbal
- La Romana
- San Pedro de Macorís
- Duarte
- La Vega
- Espaillat

**Stats Summary (4 cards):**

| Stat            | Icon     | Color  | Calculation                              |
| --------------- | -------- | ------ | ---------------------------------------- |
| Ubicaciones     | FiMapPin | blue   | locations.length                         |
| Activas         | FiCheck  | green  | locations.filter(l => l.isActive).length |
| Vehículos Total | FiHome   | purple | Sum of vehicleCount                      |
| Provincias      | FiMapPin | yellow | Unique provinces count                   |

---

## Servicios API

### dealerSettingsApi (dealerSettingsService)

```typescript
import {
  dealerSettingsApi,
  type DealerEmployee,
  type DealerEmployeeInvitation,
  type RoleDefinition,
  type DealerRole,
} from "@/services/dealerSettingsService";

// Team members
dealerSettingsApi.getTeamMembers(dealerId);
dealerSettingsApi.getTeamMember(dealerId, employeeId);
dealerSettingsApi.removeTeamMember(dealerId, employeeId);
dealerSettingsApi.updateTeamMemberRole(dealerId, employeeId, { role });

// Invitations
dealerSettingsApi.getInvitations(dealerId);
dealerSettingsApi.inviteTeamMember(dealerId, { email, role, invitedBy });
dealerSettingsApi.cancelInvitation(dealerId, invitationId);

// Roles
dealerSettingsApi.getAvailableRoles();
```

### dealerService

```typescript
import dealerService, {
  type DealerManagedLocation,
  type CreateLocationRequest,
} from "@/services/dealerService";

// Locations
dealerService.getDealerLocations(dealerId);
dealerService.createLocation(dealerId, locationData);
dealerService.updateLocation(dealerId, locationId, locationData);
dealerService.deleteLocation(dealerId, locationId);
dealerService.setPrimaryLocation(dealerId, locationId);
```

---

## Hooks Utilizados

### useAuth

```typescript
const { user } = useAuth();
const dealerId = user?.dealerId || "";
const isOwner = employees.find((e) => e.userId === user?.id)?.role === "Owner";
```

### useParams

```typescript
const { employeeId } = useParams<{ employeeId: string }>();
```

### useNavigate

```typescript
const navigate = useNavigate();
navigate(`/dealer/employees/${employeeId}/permissions`);
```

### useCallback

```typescript
const loadData = useCallback(async () => {
  const [employeesData, invitationsData, rolesData] = await Promise.all([
    dealerSettingsApi.getTeamMembers(dealerId),
    dealerSettingsApi.getInvitations(dealerId),
    dealerSettingsApi.getAvailableRoles(),
  ]);
}, [dealerId]);
```

---

## Sub-Componentes

### RoleBadge

```typescript
const RoleBadge = ({ role }: { role: DealerRole }) => {
  const roleConfig: Record<DealerRole, { color: string; label: string }> = {...};
  return <span className={`...${config.color}`}>{config.label}</span>;
};
```

### StatusBadge

```typescript
const StatusBadge = ({ status }: { status: string }) => {
  const statusConfig: Record<string, { color: string; label: string; icon: React.ElementType }> = {...};
  return <span>{Icon}{config.label}</span>;
};
```

### InviteModal

```typescript
interface InviteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onInvite: (email: string, role: DealerRole) => void;
  loading: boolean;
}
```

### EmployeeRow

```typescript
interface EmployeeRowProps {
  employee: DealerEmployee;
  onRemove: (id: string) => void;
  onEditPermissions: (id: string) => void;
  isOwner: boolean;
}
```

### PendingInvitationRow

```typescript
interface PendingInvitationRowProps {
  invitation: DealerEmployeeInvitation;
  onCancel: (id: string) => void;
  onResend: (id: string) => void;
}
```

### PermissionToggle

```typescript
interface PermissionToggleProps {
  permission: Permission;
  onChange: (permissionId: string, enabled: boolean) => void;
  disabled: boolean;
}
```

---

## Rutas

```typescript
// Rutas definidas en App.tsx
<Route path="/dealer/employees" element={<DealerEmployeesPage />} />
<Route path="/dealer/employees/:employeeId/permissions" element={<DealerEmployeePermissionsPage />} />
<Route path="/dealer/locations" element={<LocationsPage dealerId={...} />} />
```

---

## Iconos Utilizados

### react-icons/fi (Feather)

- FiTrash2 - Eliminar
- FiMail - Email/Invitación
- FiUser - Usuario
- FiUsers - Equipo
- FiShield - Permisos
- FiCheck - Activo/Éxito
- FiX - Cerrar/Error
- FiClock - Pendiente
- FiMoreVertical - Menú acciones
- FiRefreshCw - Actualizar/Loading
- FiSearch - Búsqueda
- FiAlertCircle - Error
- FiUserPlus - Invitar
- FiArrowLeft - Volver
- FiSave - Guardar
- FiLock/FiUnlock - Permiso bloqueado/desbloqueado
- FiSettings - Configuración
- FiTruck - Inventario
- FiDollarSign - Ventas
- FiBarChart2 - Analytics
- FiMessageSquare - Mensajes
- FiFileText - Facturación
- FiMapPin - Ubicación
- FiPlus - Nuevo
- FiEdit2 - Editar
- FiHome - Principal
- FiPhone - Teléfono

---

## Formateo de Fechas

```typescript
// Fecha de activación
new Date(employee.activationDate).toLocaleDateString("es-DO", {
  day: "numeric",
  month: "short",
  year: "numeric",
});
// Output: "8 ene 2026"

// Fecha de expiración de invitación
expiresAt.toLocaleDateString("es-DO", {
  day: "numeric",
  month: "short",
  year: "numeric",
});
```

---

## Validaciones

### Email Validation (Invite Modal)

```typescript
if (!email.trim()) {
  setError("El email es requerido");
  return;
}

if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
  setError("Email inválido");
  return;
}
```

### Confirmation Dialogs

```typescript
// Remove employee
if (!window.confirm(`¿Estás seguro de remover a ${employee?.name}?`)) return;

// Cancel invitation
if (!window.confirm("¿Cancelar esta invitación?")) return;

// Delete location
if (!confirm("¿Está seguro de eliminar esta ubicación?")) return;
```

---

## Estados de UI

### Loading

```typescript
{loading && (
  <div className="flex justify-center items-center h-64">
    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
  </div>
)}
```

### Error Message

```typescript
{error && (
  <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-center gap-3">
    <FiAlertCircle className="w-5 h-5 text-red-600" />
    <p className="text-red-700">{error}</p>
    <button onClick={() => setError(null)} className="ml-auto">×</button>
  </div>
)}
```

### Success Message

```typescript
{successMessage && (
  <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg flex items-center gap-3">
    <FiCheck className="w-5 h-5 text-green-600" />
    <p className="text-green-700">{successMessage}</p>
  </div>
)}
```

### Auto-dismiss Messages

```typescript
useEffect(() => {
  if (successMessage) {
    const timer = setTimeout(() => setSuccessMessage(null), 3000);
    return () => clearTimeout(timer);
  }
}, [successMessage]);
```

---

## Checklist de Implementación

### DealerEmployeesPage

- [ ] Layout con DealerPortalLayout
- [ ] Stats cards (4): Total, Activos, Pendientes, Suspendidos
- [ ] Tabs: Empleados / Invitaciones
- [ ] Filtros: Búsqueda, Rol, Estado
- [ ] Tabla de empleados con EmployeeRow
- [ ] Tabla de invitaciones con PendingInvitationRow
- [ ] RoleBadge y StatusBadge components
- [ ] InviteModal con validación de email
- [ ] Menú de acciones (solo Owner)
- [ ] Navegación a permisos

### DealerEmployeePermissionsPage

- [ ] Link "Volver al equipo"
- [ ] Employee info card
- [ ] Role selector con presets
- [ ] 7 permission modules con toggles
- [ ] hasChanges tracking
- [ ] Save button disabled cuando no hay cambios
- [ ] Permission stats (enabled/total)
- [ ] Owner employees son read-only

### LocationsPage

- [ ] Layout con MainLayout
- [ ] Stats summary (4 cards)
- [ ] Grid de location cards
- [ ] Primary badge (⭐)
- [ ] Type badges con colores
- [ ] Modal para crear/editar
- [ ] Dropdown de provincias (10+)
- [ ] Set primary functionality
- [ ] Delete con confirmación
- [ ] Active toggle
- [ ] Add new card con border dashed

---

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-employees-locations.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer Employees & Locations", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test.describe("Employees", () => {
    test("debe mostrar lista de empleados", async ({ page }) => {
      await page.goto("/dealer/employees");

      await expect(
        page.getByRole("heading", { name: /empleados/i }),
      ).toBeVisible();
      await expect(page.getByTestId("employees-list")).toBeVisible();
    });

    test("debe invitar nuevo empleado", async ({ page }) => {
      await page.goto("/dealer/employees");

      await page.getByRole("button", { name: /invitar/i }).click();
      await page.fill('input[name="email"]', "empleado@test.com");
      await page.getByRole("combobox", { name: /rol/i }).click();
      await page.getByRole("option", { name: /vendedor/i }).click();
      await page.getByRole("button", { name: /enviar invitación/i }).click();

      await expect(page.getByText(/invitación enviada/i)).toBeVisible();
    });

    test("debe editar permisos de empleado", async ({ page }) => {
      await page.goto("/dealer/employees");

      await page
        .getByTestId("employee-row")
        .first()
        .getByRole("button", { name: /editar/i })
        .click();
      await page.getByRole("checkbox", { name: /ver analytics/i }).click();
      await page.getByRole("button", { name: /guardar/i }).click();

      await expect(page.getByText(/permisos actualizados/i)).toBeVisible();
    });
  });

  test.describe("Locations", () => {
    test("debe mostrar lista de sucursales", async ({ page }) => {
      await page.goto("/dealer/locations");

      await expect(
        page.getByRole("heading", { name: /sucursales/i }),
      ).toBeVisible();
      await expect(page.getByTestId("locations-list")).toBeVisible();
    });

    test("debe agregar nueva sucursal", async ({ page }) => {
      await page.goto("/dealer/locations");

      await page.getByRole("button", { name: /agregar sucursal/i }).click();
      await page.fill('input[name="name"]', "Sucursal Santiago");
      await page.fill('input[name="address"]', "Av. 27 de Febrero #123");
      await page.getByRole("button", { name: /guardar/i }).click();

      await expect(page.getByText(/sucursal creada/i)).toBeVisible();
    });
  });
});
```

---

## Notas de Integración

1. **DealerEmployeesPage** usa `useAuth` para verificar si es Owner
2. **Permissions** se aplican como presets según el rol seleccionado
3. **LocationsPage** recibe `dealerId` como prop
4. Invitaciones expiran automáticamente (verificar `expirationDate`)
5. Solo el Owner puede editar permisos y remover empleados
6. Primary location no puede ser eliminada
7. Provincias son específicas de República Dominicana
8. Messages auto-dismiss después de 3 segundos
