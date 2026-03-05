# 🔐 Auditoría de Gestión de Roles - Frontend Web-Next

**Fecha:** Febrero 6, 2026  
**Versión:** 1.0  
**Autor:** Auditoría automatizada  
**Estado:** ✅ COMPLETADO

---

## 📋 RESUMEN EJECUTIVO

Esta auditoría analiza cómo se gestionan los roles en el frontend `web-next` de OKLA, específicamente para entender:

1. **¿Qué rol se asigna automáticamente al registrarse un usuario comprador?**
2. **¿Puede un usuario normal gestionar roles?**
3. **¿Cómo fluyen los roles desde el backend hasta el frontend?**

---

## 🎯 HALLAZGOS PRINCIPALES

### ✅ Rol por Defecto para Usuarios Compradores

| Aspecto                             | Valor                          |
| ----------------------------------- | ------------------------------ |
| **Rol por defecto**                 | `user` (string en JWT)         |
| **AccountType por defecto**         | `Individual` (enum en backend) |
| **Asignación**                      | AUTOMÁTICA al registrarse      |
| **¿Quién lo asigna?**               | **Backend (AuthService)**      |
| **¿El frontend puede modificarlo?** | ❌ **NO**                      |

### 📊 Mapeo de AccountType a Role

| AccountType (Backend)  | Role (JWT/Frontend) | Descripción                       |
| ---------------------- | ------------------- | --------------------------------- |
| `Guest (0)`            | N/A                 | No tiene cuenta                   |
| `Individual (1)`       | `user`              | **Comprador/Vendedor individual** |
| `Dealer (2)`           | `dealer`            | Propietario de concesionario      |
| `DealerEmployee (3)`   | `dealer_employee`   | Empleado de dealer                |
| `Admin (4)`            | `admin`             | Administrador                     |
| `PlatformEmployee (5)` | `platform_employee` | Empleado OKLA                     |

---

## 🔍 ANÁLISIS DETALLADO

### 1️⃣ Registro de Usuario Normal (Comprador)

#### Flujo Completo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     FLUJO DE REGISTRO DE USUARIO                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. Usuario completa formulario en /registro                                │
│     └── RegisterPage.tsx → authService.register()                           │
│                                                                             │
│  2. Frontend envía POST /api/auth/register                                  │
│     └── { firstName, lastName, email, password, acceptTerms }              │
│     └── ⚠️ NO envía rol ni accountType                                     │
│                                                                             │
│  3. Backend (AuthService) crea usuario                                      │
│     └── ApplicationUser con AccountType = Individual (DEFAULT)             │
│     └── RegisterCommandHandler.cs línea 57                                 │
│                                                                             │
│  4. Backend genera JWT con claims:                                          │
│     └── sub: userId                                                         │
│     └── email: user@example.com                                            │
│     └── role: "user" (extraído de AccountType)                             │
│     └── accountType: "individual"                                           │
│                                                                             │
│  5. Frontend decodifica JWT y almacena user:                               │
│     └── auth-context.tsx extrae role del token                             │
│     └── Fallback: role = 'user' si no viene en token                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Código Backend - Asignación Automática

**Archivo:** [backend/AuthService/AuthService.Domain/Entities/ApplicationUser.cs](../../../backend/AuthService/AuthService.Domain/Entities/ApplicationUser.cs)

```csharp
// Línea 39 - AccountType por defecto
public AccountType AccountType { get; set; } = AccountType.Individual;
```

**Archivo:** [backend/AuthService/AuthService.Domain/Enums/AccountType.cs](../../../backend/AuthService/AuthService.Domain/Enums/AccountType.cs)

```csharp
/// <summary>
/// Usuario individual registrado.
/// Puede ser comprador (gratis) o vendedor ($29/listing).
/// - Comprador: Favoritos, alertas, comparación, contactar vendedores
/// - Vendedor: Publicar vehículos propios
/// </summary>
Individual = 1,
```

#### Código Frontend - Extracción de Rol

**Archivo:** [frontend/web-next/src/contexts/auth-context.tsx](../../../frontend/web-next/src/contexts/auth-context.tsx)

```typescript
// Líneas 117-120 - Extracción del rol del JWT
role:
  payload.role ||
  payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
  'user',  // ← FALLBACK: Si no hay rol, asume 'user'
```

---

### 2️⃣ Gestión de Roles - ¿Quién Puede?

#### ❌ Usuario Normal (Individual/Comprador) NO PUEDE:

| Acción                   | Permitido | Razón                        |
| ------------------------ | --------- | ---------------------------- |
| Ver su propio rol        | ✅ Sí     | Solo lectura en perfil       |
| Cambiar su rol           | ❌ No     | No hay endpoint público      |
| Gestionar roles de otros | ❌ No     | Requiere `admin`             |
| Acceder a RoleService    | ❌ No     | Requiere autenticación admin |
| Crear/editar roles       | ❌ No     | Solo admin                   |

#### ✅ Solo Admin PUEDE:

| Acción           | Endpoint                           | Descripción         |
| ---------------- | ---------------------------------- | ------------------- |
| Listar roles     | `GET /api/roles`                   | Ver todos los roles |
| Crear rol        | `POST /api/roles`                  | Crear nuevo rol     |
| Editar rol       | `PUT /api/roles/{id}`              | Modificar rol       |
| Eliminar rol     | `DELETE /api/roles/{id}`           | Borrar rol          |
| Asignar permisos | `POST /api/roles/{id}/permissions` | Vincular permiso    |

---

### 3️⃣ Protección de Rutas en Frontend

**Archivo:** [frontend/web-next/src/middleware.ts](../../../frontend/web-next/src/middleware.ts)

```typescript
// Rutas protegidas por rol
const roleProtectedRoutes: Record<string, string[]> = {
  "/dealer": ["dealer", "admin"],
  "/dealer/inventario": ["dealer", "admin"],
  "/dealer/analytics": ["dealer", "admin"],
  "/admin": ["admin"],
  "/admin/usuarios": ["admin"],
  "/admin/vehiculos": ["admin"],
  "/publicar": ["user", "seller", "dealer", "admin"], // ← Comprador puede publicar
  "/mis-vehiculos": ["user", "seller", "dealer", "admin"],
  "/cuenta": ["user", "seller", "dealer", "admin"],
};
```

#### Usuario `user` (Comprador) Puede Acceder:

✅ `/cuenta` - Perfil y configuración  
✅ `/cuenta/perfil` - Editar perfil  
✅ `/cuenta/seguridad` - Seguridad  
✅ `/cuenta/favoritos` - Favoritos  
✅ `/cuenta/alertas` - Alertas de precio  
✅ `/cuenta/mensajes` - Mensajes  
✅ `/publicar` - Publicar vehículo (se convierte en vendedor)  
✅ `/mis-vehiculos` - Ver sus publicaciones  
✅ `/buscar` - Buscar vehículos (público)  
✅ `/comparar` - Comparar vehículos (público)

#### Usuario `user` NO Puede Acceder:

❌ `/dealer/*` - Panel de dealer  
❌ `/admin/*` - Panel de admin  
❌ Cualquier endpoint de RoleService

---

### 4️⃣ Estructura de Tipos en Frontend

**Archivo:** [frontend/web-next/src/types/index.ts](../../../frontend/web-next/src/types/index.ts)

```typescript
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;
  phone?: string;
  accountType: "individual" | "dealer" | "admin"; // ← Solo 3 valores en frontend
  isVerified: boolean;
  // ...
}
```

**Archivo:** [frontend/web-next/src/contexts/auth-context.tsx](../../../frontend/web-next/src/contexts/auth-context.tsx)

```typescript
interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;
  accountType: "individual" | "dealer" | "admin";
  role: string; // ← role como string libre
}
```

---

### 5️⃣ DealerRole vs AccountType

#### ⚠️ IMPORTANTE: Son Conceptos Diferentes

| Concepto        | Descripción                              | Quién lo asigna           |
| --------------- | ---------------------------------------- | ------------------------- |
| **AccountType** | Tipo de cuenta del usuario               | Automático al registrarse |
| **DealerRole**  | Rol dentro de un dealer (si es empleado) | El dueño del dealer       |
| **Role (JWT)**  | Claim en el token                        | Backend al generar JWT    |

**DealerRole** solo aplica cuando `AccountType = DealerEmployee`:

```typescript
// frontend/web-next/src/services/dealer-employees.ts
export type DealerRole =
  | "Owner"
  | "Admin"
  | "SalesManager"
  | "Salesperson"
  | "InventoryManager"
  | "Viewer";
```

---

## 📊 MATRIZ DE PERMISOS POR ROL

| Funcionalidad        | Guest | User (Comprador) | Dealer | Admin |
| -------------------- | ----- | ---------------- | ------ | ----- |
| Ver vehículos        | ✅    | ✅               | ✅     | ✅    |
| Buscar/Filtrar       | ✅    | ✅               | ✅     | ✅    |
| Comparar             | ✅    | ✅               | ✅     | ✅    |
| Guardar favoritos    | ❌    | ✅               | ✅     | ✅    |
| Crear alertas        | ❌    | ✅               | ✅     | ✅    |
| Contactar vendedor   | ❌    | ✅               | ✅     | ✅    |
| Publicar vehículo    | ❌    | ✅               | ✅     | ✅    |
| Panel de dealer      | ❌    | ❌               | ✅     | ✅    |
| Gestionar inventario | ❌    | ❌               | ✅     | ✅    |
| Analytics de dealer  | ❌    | ❌               | ✅     | ✅    |
| Panel de admin       | ❌    | ❌               | ❌     | ✅    |
| **Gestionar roles**  | ❌    | ❌               | ❌     | ✅    |

---

## 🔒 CONCLUSIONES

### ✅ Lo que está CORRECTO:

1. **Rol automático:** El backend asigna `AccountType.Individual` por defecto - ✅ Seguro
2. **Sin UI para roles:** El frontend NO tiene UI para que usuarios normales cambien roles - ✅ Seguro
3. **Protección de rutas:** El middleware protege rutas según rol - ✅ Implementado
4. **Fallback seguro:** Si el JWT no tiene rol, se asume `'user'` - ✅ Defensivo

### ⚠️ Puntos de Atención:

1. **Rol en JWT:** El claim `role` viene del backend, el frontend solo lo lee
2. **Validación server-side:** Las operaciones críticas DEBEN validarse en backend
3. **Escalación de permisos:** Solo admin puede cambiar AccountType (vía UserService)

### 📝 Recomendaciones:

1. ✅ Mantener la lógica actual - es correcta y segura
2. ⚠️ No agregar UI para cambiar roles en frontend
3. ⚠️ Validar siempre roles en backend, no confiar solo en frontend

---

## 📚 ARCHIVOS AUDITADOS

### Frontend

| Archivo                            | Relevancia                  |
| ---------------------------------- | --------------------------- |
| `src/contexts/auth-context.tsx`    | Extracción de rol del JWT   |
| `src/hooks/use-auth.tsx`           | Hook de autenticación       |
| `src/services/auth.ts`             | Servicio de autenticación   |
| `src/services/dealer-employees.ts` | DealerRoles para empleados  |
| `src/middleware.ts`                | Protección de rutas         |
| `src/types/index.ts`               | Tipos de User y AccountType |
| `src/app/(auth)/registro/page.tsx` | Página de registro          |

### Backend

| Archivo                                                                             | Relevancia              |
| ----------------------------------------------------------------------------------- | ----------------------- |
| `AuthService/Domain/Entities/ApplicationUser.cs`                                    | Entidad con AccountType |
| `AuthService/Domain/Enums/AccountType.cs`                                           | Enum de tipos de cuenta |
| `AuthService/Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs` | Lógica de registro      |
| `RoleService/Domain/Entities/Role.cs`                                               | Entidad de roles        |
| `UserService/Domain/Entities/User.cs`                                               | User con roles          |

### Documentación

| Archivo                                                        | Relevancia             |
| -------------------------------------------------------------- | ---------------------- |
| `docs/frontend-rebuild/05-API-INTEGRATION/02-autenticacion.md` | Guía de auth           |
| `docs/frontend-rebuild/05-API-INTEGRATION/10-roles-api.md`     | API de roles           |
| `backend/RoleService/ARCHITECTURE_AUTH_FLOW.md`                | Flujo de autenticación |

---

## ❓ FAQ

### P: ¿Un comprador puede cambiar su propio rol?

**R:** ❌ NO. No hay endpoint ni UI para esto. Solo admin puede cambiar roles.

### P: ¿Qué pasa si un comprador quiere ser dealer?

**R:** Debe completar el proceso de registro de dealer en `/dealer/registro`. Esto crea una solicitud que admin aprueba, y luego se actualiza su AccountType a `Dealer`.

### P: ¿El rol viene en el JWT o se consulta después?

**R:** El rol básico viene en el JWT. Para permisos detallados, el Gateway enriquece el token consultando RoleService/UserService.

### P: ¿Dónde se configura el rol por defecto?

**R:** En `ApplicationUser.cs` línea 39: `AccountType = AccountType.Individual`

---

_Auditoría generada el 6 de febrero de 2026_  
_OKLA - Sistema de Gestión de Roles_
