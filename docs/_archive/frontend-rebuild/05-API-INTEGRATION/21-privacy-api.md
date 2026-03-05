# 🔒 21 - Privacy API (Derechos ARCO - Ley 172-13 RD)

**Servicio:** UserService  
**Puerto:** 8080  
**Base Path:** `/api/privacy`  
**Autenticación:** ✅ Requerida

---

## 📖 Descripción

Implementación de derechos ARCO según Ley 172-13 de República Dominicana:

- **A**cceso: Ver datos personales
- **R**ectificación: Corregir datos (en UsersController)
- **C**ancelación: Eliminar cuenta
- **O**posición: Gestionar preferencias de comunicación
- **Portabilidad**: Exportar datos

---

## 🎯 Endpoints Disponibles

| #   | Método | Endpoint                                 | Auth | Descripción               |
| --- | ------ | ---------------------------------------- | ---- | ------------------------- |
| 1   | `GET`  | `/api/privacy/my-data`                   | ✅   | Resumen de datos (Acceso) |
| 2   | `GET`  | `/api/privacy/my-data/full`              | ✅   | Datos completos (Acceso)  |
| 3   | `POST` | `/api/privacy/export/request`            | ✅   | Solicitar exportación     |
| 4   | `GET`  | `/api/privacy/export/status`             | ✅   | Estado de exportación     |
| 5   | `GET`  | `/api/privacy/export/download/{token}`   | ✅   | Descargar archivo         |
| 6   | `POST` | `/api/privacy/delete-account/request`    | ✅   | Solicitar eliminación     |
| 7   | `POST` | `/api/privacy/delete-account/confirm`    | ✅   | Confirmar eliminación     |
| 8   | `POST` | `/api/privacy/delete-account/cancel`     | ✅   | Cancelar eliminación      |
| 9   | `GET`  | `/api/privacy/delete-account/status`     | ✅   | Estado de eliminación     |
| 10  | `GET`  | `/api/privacy/communication-preferences` | ✅   | Preferencias comunicación |
| 11  | `PUT`  | `/api/privacy/communication-preferences` | ✅   | Actualizar preferencias   |
| 12  | `GET`  | `/api/privacy/request-history`           | ✅   | Historial de solicitudes  |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/privacy/my-data` - Resumen de Datos

**Response 200:**

```json
{
  "userId": "user-123",
  "email": "usuario@example.com",
  "fullName": "Juan Pérez",
  "createdAt": "2025-06-15T10:00:00Z",
  "dataSummary": {
    "profileComplete": true,
    "vehiclesListed": 3,
    "favoritesSaved": 12,
    "messagesCount": 45,
    "transactionsCount": 2,
    "lastLoginAt": "2026-01-30T08:00:00Z"
  },
  "dataCategories": [
    { "category": "Profile", "recordCount": 1, "lastUpdated": "2026-01-15" },
    { "category": "Vehicles", "recordCount": 3, "lastUpdated": "2026-01-28" },
    { "category": "Favorites", "recordCount": 12, "lastUpdated": "2026-01-30" },
    { "category": "Messages", "recordCount": 45, "lastUpdated": "2026-01-29" }
  ]
}
```

---

### 3. POST `/api/privacy/export/request` - Solicitar Exportación

**Request:**

```json
{
  "format": "JSON",
  "includeProfile": true,
  "includeActivity": true,
  "includeMessages": true,
  "includeFavorites": true,
  "includeTransactions": true
}
```

**Formatos disponibles:** `JSON`, `CSV`, `PDF`

**Response 202:**

```json
{
  "requestId": "export-789",
  "status": "Processing",
  "estimatedCompletionTime": "2026-01-30T12:00:00Z",
  "message": "Su solicitud está siendo procesada. Recibirá un email cuando esté lista."
}
```

---

### 6. POST `/api/privacy/delete-account/request` - Solicitar Eliminación

**Request:**

```json
{
  "reason": "NoLongerNeeded",
  "otherReason": null,
  "feedback": "Vendí mi vehículo, ya no necesito la cuenta"
}
```

**Razones disponibles:**

- `NoLongerNeeded` - Ya no necesito la cuenta
- `PrivacyConcerns` - Preocupaciones de privacidad
- `TooManyEmails` - Demasiados correos
- `BadExperience` - Mala experiencia
- `Other` - Otra razón

**Response 202:**

```json
{
  "requestId": "delete-456",
  "status": "PendingConfirmation",
  "confirmationCodeSentTo": "j***@example.com",
  "expiresAt": "2026-01-31T10:00:00Z",
  "gracePeriodDays": 30,
  "message": "Se ha enviado un código de confirmación a su email."
}
```

---

### 10. GET `/api/privacy/communication-preferences` - Preferencias

**Response 200:**

```json
{
  "userId": "user-123",
  "preferences": {
    "emailMarketing": false,
    "emailTransactional": true,
    "emailNewListings": true,
    "emailPriceAlerts": true,
    "smsNotifications": false,
    "pushNotifications": true,
    "weeklyDigest": false
  },
  "updatedAt": "2026-01-15T10:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// DATA ACCESS (ARCO - Acceso)
// ============================================================================

export interface UserDataSummary {
  userId: string;
  email: string;
  fullName: string;
  createdAt: string;
  dataSummary: {
    profileComplete: boolean;
    vehiclesListed: number;
    favoritesSaved: number;
    messagesCount: number;
    transactionsCount: number;
    lastLoginAt: string;
  };
  dataCategories: DataCategory[];
}

export interface DataCategory {
  category: string;
  recordCount: number;
  lastUpdated: string;
}

export interface UserFullData {
  profile: UserProfile;
  vehicles: VehicleSummary[];
  favorites: FavoriteSummary[];
  messages: MessageSummary[];
  transactions: TransactionSummary[];
  activityLog: ActivityLogEntry[];
}

// ============================================================================
// DATA EXPORT (ARCO - Portabilidad)
// ============================================================================

export type ExportFormat = "JSON" | "CSV" | "PDF";

export interface RequestDataExportDto {
  format: ExportFormat;
  includeProfile: boolean;
  includeActivity: boolean;
  includeMessages: boolean;
  includeFavorites: boolean;
  includeTransactions: boolean;
}

export interface DataExportRequestResponse {
  requestId: string;
  status: ExportStatus;
  estimatedCompletionTime: string;
  message: string;
}

export type ExportStatus =
  | "Pending"
  | "Processing"
  | "Completed"
  | "Failed"
  | "Expired";

export interface DataExportStatus {
  requestId: string;
  status: ExportStatus;
  progress: number;
  downloadUrl?: string;
  downloadToken?: string;
  expiresAt?: string;
  createdAt: string;
}

// ============================================================================
// ACCOUNT DELETION (ARCO - Cancelación)
// ============================================================================

export type DeletionReason =
  | "NoLongerNeeded"
  | "PrivacyConcerns"
  | "TooManyEmails"
  | "BadExperience"
  | "Other";

export interface RequestAccountDeletionDto {
  reason: DeletionReason;
  otherReason?: string;
  feedback?: string;
}

export interface AccountDeletionResponse {
  requestId: string;
  status: DeletionStatus;
  confirmationCodeSentTo: string;
  expiresAt: string;
  gracePeriodDays: number;
  message: string;
}

export type DeletionStatus =
  | "PendingConfirmation"
  | "Confirmed"
  | "InGracePeriod"
  | "Completed"
  | "Cancelled";

export interface ConfirmAccountDeletionDto {
  confirmationCode: string;
  password: string;
}

// ============================================================================
// COMMUNICATION PREFERENCES (ARCO - Oposición)
// ============================================================================

export interface CommunicationPreferences {
  userId: string;
  preferences: {
    emailMarketing: boolean;
    emailTransactional: boolean;
    emailNewListings: boolean;
    emailPriceAlerts: boolean;
    smsNotifications: boolean;
    pushNotifications: boolean;
    weeklyDigest: boolean;
  };
  updatedAt: string;
}

export interface UpdatePreferencesRequest {
  emailMarketing?: boolean;
  emailNewListings?: boolean;
  emailPriceAlerts?: boolean;
  smsNotifications?: boolean;
  pushNotifications?: boolean;
  weeklyDigest?: boolean;
}

// ============================================================================
// REQUEST HISTORY
// ============================================================================

export interface PrivacyRequest {
  id: string;
  type: "DataExport" | "AccountDeletion" | "PreferencesUpdate";
  status: string;
  createdAt: string;
  completedAt?: string;
  ipAddress: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/privacyService.ts
import { apiClient } from "./api-client";
import type {
  UserDataSummary,
  UserFullData,
  RequestDataExportDto,
  DataExportRequestResponse,
  DataExportStatus,
  RequestAccountDeletionDto,
  AccountDeletionResponse,
  ConfirmAccountDeletionDto,
  CommunicationPreferences,
  UpdatePreferencesRequest,
  PrivacyRequest,
} from "@/types/privacy";

class PrivacyService {
  // ============================================================================
  // DATA ACCESS
  // ============================================================================

  async getMyDataSummary(): Promise<UserDataSummary> {
    const response = await apiClient.get<UserDataSummary>(
      "/api/privacy/my-data",
    );
    return response.data;
  }

  async getMyFullData(): Promise<UserFullData> {
    const response = await apiClient.get<UserFullData>(
      "/api/privacy/my-data/full",
    );
    return response.data;
  }

  // ============================================================================
  // DATA EXPORT
  // ============================================================================

  async requestExport(
    request: RequestDataExportDto,
  ): Promise<DataExportRequestResponse> {
    const response = await apiClient.post<DataExportRequestResponse>(
      "/api/privacy/export/request",
      request,
    );
    return response.data;
  }

  async getExportStatus(): Promise<DataExportStatus> {
    const response = await apiClient.get<DataExportStatus>(
      "/api/privacy/export/status",
    );
    return response.data;
  }

  getDownloadUrl(token: string): string {
    return `/api/privacy/export/download/${token}`;
  }

  // ============================================================================
  // ACCOUNT DELETION
  // ============================================================================

  async requestAccountDeletion(
    request: RequestAccountDeletionDto,
  ): Promise<AccountDeletionResponse> {
    const response = await apiClient.post<AccountDeletionResponse>(
      "/api/privacy/delete-account/request",
      request,
    );
    return response.data;
  }

  async confirmAccountDeletion(
    request: ConfirmAccountDeletionDto,
  ): Promise<void> {
    await apiClient.post("/api/privacy/delete-account/confirm", request);
  }

  async cancelAccountDeletion(): Promise<void> {
    await apiClient.post("/api/privacy/delete-account/cancel");
  }

  async getDeletionStatus(): Promise<{ status: string; scheduledAt?: string }> {
    const response = await apiClient.get("/api/privacy/delete-account/status");
    return response.data;
  }

  // ============================================================================
  // COMMUNICATION PREFERENCES
  // ============================================================================

  async getPreferences(): Promise<CommunicationPreferences> {
    const response = await apiClient.get<CommunicationPreferences>(
      "/api/privacy/communication-preferences",
    );
    return response.data;
  }

  async updatePreferences(
    request: UpdatePreferencesRequest,
  ): Promise<CommunicationPreferences> {
    const response = await apiClient.put<CommunicationPreferences>(
      "/api/privacy/communication-preferences",
      request,
    );
    return response.data;
  }

  // ============================================================================
  // HISTORY
  // ============================================================================

  async getRequestHistory(): Promise<PrivacyRequest[]> {
    const response = await apiClient.get<PrivacyRequest[]>(
      "/api/privacy/request-history",
    );
    return response.data;
  }
}

export const privacyService = new PrivacyService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/usePrivacy.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { privacyService } from "@/services/privacyService";
import type {
  RequestDataExportDto,
  RequestAccountDeletionDto,
  UpdatePreferencesRequest,
} from "@/types/privacy";

export const privacyKeys = {
  all: ["privacy"] as const,
  dataSummary: () => [...privacyKeys.all, "data-summary"] as const,
  fullData: () => [...privacyKeys.all, "full-data"] as const,
  exportStatus: () => [...privacyKeys.all, "export-status"] as const,
  deletionStatus: () => [...privacyKeys.all, "deletion-status"] as const,
  preferences: () => [...privacyKeys.all, "preferences"] as const,
  history: () => [...privacyKeys.all, "history"] as const,
};

export function useMyDataSummary() {
  return useQuery({
    queryKey: privacyKeys.dataSummary(),
    queryFn: () => privacyService.getMyDataSummary(),
  });
}

export function useMyFullData() {
  return useQuery({
    queryKey: privacyKeys.fullData(),
    queryFn: () => privacyService.getMyFullData(),
    enabled: false, // Manual trigger only
  });
}

export function useRequestDataExport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: RequestDataExportDto) =>
      privacyService.requestExport(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: privacyKeys.exportStatus() });
      queryClient.invalidateQueries({ queryKey: privacyKeys.history() });
    },
  });
}

export function useExportStatus() {
  return useQuery({
    queryKey: privacyKeys.exportStatus(),
    queryFn: () => privacyService.getExportStatus(),
    refetchInterval: (data) => (data?.status === "Processing" ? 5000 : false),
  });
}

export function useRequestAccountDeletion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: RequestAccountDeletionDto) =>
      privacyService.requestAccountDeletion(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: privacyKeys.deletionStatus() });
    },
  });
}

export function useCommunicationPreferences() {
  return useQuery({
    queryKey: privacyKeys.preferences(),
    queryFn: () => privacyService.getPreferences(),
  });
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdatePreferencesRequest) =>
      privacyService.updatePreferences(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: privacyKeys.preferences() });
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/settings/PrivacySettings.tsx
import { useMyDataSummary, useCommunicationPreferences, useUpdatePreferences } from "@/hooks/usePrivacy";
import { FiDownload, FiTrash2, FiMail, FiBell } from "react-icons/fi";

export const PrivacySettings = () => {
  const { data: summary } = useMyDataSummary();
  const { data: preferences } = useCommunicationPreferences();
  const updatePreferences = useUpdatePreferences();

  const handleToggle = (key: string, value: boolean) => {
    updatePreferences.mutate({ [key]: value });
  };

  return (
    <div className="space-y-8">
      {/* Data Summary */}
      <section>
        <h2 className="text-xl font-semibold mb-4">Tus Datos</h2>
        <div className="bg-gray-50 p-4 rounded-lg">
          <p>Email: {summary?.email}</p>
          <p>Vehículos publicados: {summary?.dataSummary.vehiclesListed}</p>
          <p>Favoritos guardados: {summary?.dataSummary.favoritesSaved}</p>

          <div className="flex gap-2 mt-4">
            <button className="btn btn-outline flex items-center gap-2">
              <FiDownload /> Exportar mis datos
            </button>
          </div>
        </div>
      </section>

      {/* Communication Preferences */}
      <section>
        <h2 className="text-xl font-semibold mb-4">Preferencias de Comunicación</h2>
        <div className="space-y-3">
          {[
            { key: "emailMarketing", label: "Emails promocionales", icon: FiMail },
            { key: "emailPriceAlerts", label: "Alertas de precio", icon: FiBell },
            { key: "pushNotifications", label: "Notificaciones push", icon: FiBell },
            { key: "weeklyDigest", label: "Resumen semanal", icon: FiMail },
          ].map(({ key, label, icon: Icon }) => (
            <label key={key} className="flex items-center justify-between p-3 border rounded">
              <span className="flex items-center gap-2">
                <Icon /> {label}
              </span>
              <input
                type="checkbox"
                checked={preferences?.preferences[key as keyof typeof preferences.preferences] ?? false}
                onChange={(e) => handleToggle(key, e.target.checked)}
                className="toggle"
              />
            </label>
          ))}
        </div>
      </section>

      {/* Danger Zone */}
      <section className="border-t pt-8">
        <h2 className="text-xl font-semibold text-red-600 mb-4">Zona de Peligro</h2>
        <button className="btn btn-outline btn-error flex items-center gap-2">
          <FiTrash2 /> Eliminar mi cuenta
        </button>
        <p className="text-sm text-gray-500 mt-2">
          Tendrás 30 días para cancelar la eliminación.
        </p>
      </section>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **12 Endpoints documentados**  
✅ **Cumplimiento Ley 172-13 RD** (ARCO completo)  
✅ **TypeScript Types** (Export, Deletion, Preferences)  
✅ **Service Layer** (12 métodos)  
✅ **React Query Hooks** (7 hooks)  
✅ **Componente ejemplo** (PrivacySettings)

---

_Última actualización: Enero 30, 2026_
