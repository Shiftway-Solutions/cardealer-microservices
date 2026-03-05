# 📈 27 - User Behavior API

**Servicio:** UserBehaviorService  
**Puerto:** 8080  
**Base Path:** `/api/userbehavior`  
**Autenticación:** ✅ Parcial

---

## 📖 Descripción

Servicio de tracking y análisis de comportamiento de usuarios para:

- Perfiles de comportamiento
- Historial de acciones
- Analytics de uso
- Segmentación de usuarios

---

## 🎯 Endpoints Disponibles

| #   | Método | Endpoint                             | Auth     | Descripción              |
| --- | ------ | ------------------------------------ | -------- | ------------------------ |
| 1   | `GET`  | `/api/userbehavior/{userId}`         | ✅       | Perfil de comportamiento |
| 2   | `GET`  | `/api/userbehavior/{userId}/actions` | ✅       | Historial de acciones    |
| 3   | `POST` | `/api/userbehavior/actions`          | ✅       | Registrar acción         |
| 4   | `GET`  | `/api/userbehavior/summary`          | ✅ Admin | Resumen global           |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/userbehavior/{userId}` - Perfil de Comportamiento

**Response 200:**

```json
{
  "userId": "user-123",
  "totalActions": 245,
  "lastActivityAt": "2026-01-30T10:00:00Z",
  "preferredCategories": ["SUV", "Sedan"],
  "preferredMakes": ["Toyota", "Honda"],
  "priceRangeMin": 1000000,
  "priceRangeMax": 2500000,
  "averageSessionDuration": "12m 30s",
  "topSearchQueries": ["toyota camry 2024", "suv 4x4"],
  "favoriteCount": 15,
  "comparisonCount": 8,
  "contactsInitiated": 3,
  "segments": ["HighIntent", "PremiumBuyer"]
}
```

---

### 2. GET `/api/userbehavior/{userId}/actions` - Historial

**Query Params:**

- `limit` (int): Número de acciones (default: 50)

**Response 200:**

```json
[
  {
    "id": "action-001",
    "userId": "user-123",
    "actionType": "VehicleView",
    "actionDetails": {
      "vehicleId": "vehicle-456",
      "vehicleTitle": "Toyota Camry 2024"
    },
    "relatedVehicleId": "vehicle-456",
    "sessionId": "session-789",
    "deviceType": "Mobile",
    "createdAt": "2026-01-30T09:55:00Z"
  },
  {
    "id": "action-002",
    "userId": "user-123",
    "actionType": "Search",
    "searchQuery": "suv toyota 2024",
    "createdAt": "2026-01-30T09:50:00Z"
  }
]
```

---

### 3. POST `/api/userbehavior/actions` - Registrar Acción

**Request:**

```json
{
  "userId": "user-123",
  "actionType": "VehicleView",
  "actionDetails": {
    "vehicleId": "vehicle-456",
    "source": "SearchResults"
  },
  "relatedVehicleId": "vehicle-456",
  "searchQuery": null,
  "sessionId": "session-789",
  "deviceType": "Desktop"
}
```

**Action Types:**

- `VehicleView` - Vista de vehículo
- `VehicleDetailView` - Vista detallada
- `Search` - Búsqueda
- `Filter` - Aplicar filtro
- `Favorite` - Agregar favorito
- `Compare` - Agregar comparación
- `Contact` - Contactar vendedor
- `Share` - Compartir
- `PriceAlert` - Crear alerta
- `SaveSearch` - Guardar búsqueda

**Response 201:**

```json
{
  "id": "action-003",
  "userId": "user-123",
  "actionType": "VehicleView",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 4. GET `/api/userbehavior/summary` - Resumen Global (Admin)

**Response 200:**

```json
{
  "totalUsers": 15420,
  "activeUsersToday": 1250,
  "activeUsersWeek": 5800,
  "totalActions": 458000,
  "actionsToday": 12500,
  "topActionTypes": [
    { "type": "Search", "count": 45000 },
    { "type": "VehicleView", "count": 38000 },
    { "type": "Favorite", "count": 8500 }
  ],
  "topSearchQueries": [
    { "query": "toyota camry", "count": 1250 },
    { "query": "suv 2024", "count": 980 }
  ],
  "deviceBreakdown": {
    "Desktop": 45,
    "Mobile": 48,
    "Tablet": 7
  }
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// USER BEHAVIOR PROFILE
// ============================================================================

export interface UserBehaviorProfile {
  userId: string;
  totalActions: number;
  lastActivityAt: string;
  preferredCategories: string[];
  preferredMakes: string[];
  priceRangeMin: number;
  priceRangeMax: number;
  averageSessionDuration: string;
  topSearchQueries: string[];
  favoriteCount: number;
  comparisonCount: number;
  contactsInitiated: number;
  segments: UserSegment[];
}

export type UserSegment =
  | "HighIntent"
  | "LowIntent"
  | "PremiumBuyer"
  | "BudgetBuyer"
  | "FirstTimeBuyer"
  | "ReturnVisitor"
  | "Researcher";

// ============================================================================
// USER ACTIONS
// ============================================================================

export type ActionType =
  | "VehicleView"
  | "VehicleDetailView"
  | "Search"
  | "Filter"
  | "Favorite"
  | "Compare"
  | "Contact"
  | "Share"
  | "PriceAlert"
  | "SaveSearch";

export type DeviceType = "Desktop" | "Mobile" | "Tablet";

export interface UserAction {
  id: string;
  userId: string;
  actionType: ActionType;
  actionDetails?: Record<string, any>;
  relatedVehicleId?: string;
  searchQuery?: string;
  sessionId?: string;
  deviceType?: DeviceType;
  createdAt: string;
}

export interface RecordActionRequest {
  userId: string;
  actionType: ActionType;
  actionDetails?: Record<string, any>;
  relatedVehicleId?: string;
  searchQuery?: string;
  sessionId?: string;
  deviceType?: DeviceType;
}

// ============================================================================
// SUMMARY
// ============================================================================

export interface UserBehaviorSummary {
  totalUsers: number;
  activeUsersToday: number;
  activeUsersWeek: number;
  totalActions: number;
  actionsToday: number;
  topActionTypes: { type: string; count: number }[];
  topSearchQueries: { query: string; count: number }[];
  deviceBreakdown: Record<DeviceType, number>;
}
```

---

## 📡 Service Layer

```typescript
// src/services/userBehaviorService.ts
import { apiClient } from "./api-client";
import type {
  UserBehaviorProfile,
  UserAction,
  RecordActionRequest,
  UserBehaviorSummary,
} from "@/types/userBehavior";

class UserBehaviorService {
  async getProfile(userId: string): Promise<UserBehaviorProfile> {
    const response = await apiClient.get<UserBehaviorProfile>(
      `/api/userbehavior/${userId}`,
    );
    return response.data;
  }

  async getActions(userId: string, limit: number = 50): Promise<UserAction[]> {
    const response = await apiClient.get<UserAction[]>(
      `/api/userbehavior/${userId}/actions`,
      {
        params: { limit },
      },
    );
    return response.data;
  }

  async recordAction(request: RecordActionRequest): Promise<UserAction> {
    const response = await apiClient.post<UserAction>(
      "/api/userbehavior/actions",
      request,
    );
    return response.data;
  }

  async getSummary(): Promise<UserBehaviorSummary> {
    const response = await apiClient.get<UserBehaviorSummary>(
      "/api/userbehavior/summary",
    );
    return response.data;
  }
}

export const userBehaviorService = new UserBehaviorService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useUserBehavior.ts
import { useMutation, useQuery } from "@tanstack/react-query";
import { userBehaviorService } from "@/services/userBehaviorService";
import type { RecordActionRequest } from "@/types/userBehavior";

export const behaviorKeys = {
  all: ["userBehavior"] as const,
  profile: (userId: string) =>
    [...behaviorKeys.all, "profile", userId] as const,
  actions: (userId: string) =>
    [...behaviorKeys.all, "actions", userId] as const,
  summary: () => [...behaviorKeys.all, "summary"] as const,
};

export function useUserBehaviorProfile(userId: string) {
  return useQuery({
    queryKey: behaviorKeys.profile(userId),
    queryFn: () => userBehaviorService.getProfile(userId),
    enabled: !!userId,
  });
}

export function useUserActions(userId: string, limit?: number) {
  return useQuery({
    queryKey: behaviorKeys.actions(userId),
    queryFn: () => userBehaviorService.getActions(userId, limit),
    enabled: !!userId,
  });
}

export function useRecordAction() {
  return useMutation({
    mutationFn: (request: RecordActionRequest) =>
      userBehaviorService.recordAction(request),
  });
}

export function useBehaviorSummary() {
  return useQuery({
    queryKey: behaviorKeys.summary(),
    queryFn: () => userBehaviorService.getSummary(),
  });
}
```

---

## 🎉 Resumen

✅ **4 Endpoints documentados**  
✅ **TypeScript Types** (Profile, Actions, Summary)  
✅ **Service Layer** (4 métodos)  
✅ **React Query Hooks** (4 hooks)

---

_Última actualización: Enero 30, 2026_
