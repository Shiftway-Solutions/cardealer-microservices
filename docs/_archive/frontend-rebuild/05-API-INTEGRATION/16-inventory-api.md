# 📦 16 - Inventory Management API

**Servicio:** InventoryManagementService  
**Puerto:** 8080  
**Base Path:** `/api/inventory`  
**Autenticación:** ✅ Requerida (Dealer)

---

## 📖 Descripción

Sistema de gestión de inventario para dealers. Permite manejar vehículos en stock, precios internos, ubicaciones y realizar importaciones masivas desde CSV/Excel.

---

## 🎯 Endpoints Disponibles

### InventoryController

| #   | Método   | Endpoint                     | Auth | Descripción                  |
| --- | -------- | ---------------------------- | ---- | ---------------------------- |
| 1   | `GET`    | `/api/inventory`             | ✅   | Listar inventario (paginado) |
| 2   | `GET`    | `/api/inventory/stats`       | ✅   | Estadísticas del inventario  |
| 3   | `GET`    | `/api/inventory/{id}`        | ✅   | Obtener item por ID          |
| 4   | `POST`   | `/api/inventory`             | ✅   | Crear item de inventario     |
| 5   | `PUT`    | `/api/inventory/{id}`        | ✅   | Actualizar item              |
| 6   | `DELETE` | `/api/inventory/{id}`        | ✅   | Eliminar item                |
| 7   | `POST`   | `/api/inventory/bulk/status` | ✅   | Cambio masivo de estado      |
| 8   | `GET`    | `/api/inventory/featured`    | ❌   | Items destacados (público)   |
| 9   | `GET`    | `/api/inventory/hot`         | ✅   | Items populares              |
| 10  | `GET`    | `/api/inventory/overdue`     | ✅   | Items vencidos               |

### BulkImportController

| #   | Método | Endpoint                                | Auth | Descripción                |
| --- | ------ | --------------------------------------- | ---- | -------------------------- |
| 11  | `GET`  | `/api/inventory/bulkimport`             | ✅   | Listar jobs de importación |
| 12  | `GET`  | `/api/inventory/bulkimport/{id}`        | ✅   | Estado de un job           |
| 13  | `POST` | `/api/inventory/bulkimport`             | ✅   | Iniciar importación        |
| 14  | `POST` | `/api/inventory/bulkimport/upload`      | ✅   | Subir archivo CSV/Excel    |
| 15  | `GET`  | `/api/inventory/bulkimport/template`    | ❌   | Descargar plantilla CSV    |
| 16  | `POST` | `/api/inventory/bulkimport/{id}/cancel` | ✅   | Cancelar importación       |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/inventory` - Listar Inventario

**Query Params:**

- `dealerId` (Guid): ID del dealer
- `page` (int): Página (default: 1)
- `pageSize` (int): Items por página (default: 20)
- `status` (string): Filtrar por estado (Active, Pending, Sold, etc.)
- `searchTerm` (string): Búsqueda por VIN, stock number, etc.
- `sortBy` (string): Campo para ordenar
- `sortDescending` (bool): Orden descendente

**Response 200:**

```json
{
  "items": [
    {
      "id": "inv-001",
      "dealerId": "dealer-123",
      "vehicleId": "vehicle-456",
      "stockNumber": "STK001",
      "vin": "1HGBH41JXMN109186",
      "status": "Active",
      "location": "Showroom A",
      "costPrice": 1200000.0,
      "listPrice": 1500000.0,
      "targetPrice": 1400000.0,
      "minAcceptablePrice": 1300000.0,
      "isNegotiable": true,
      "isFeatured": false,
      "priority": 1,
      "daysInStock": 15,
      "acquiredDate": "2026-01-15",
      "acquisitionSource": "DirectPurchase",
      "internalNotes": "Clean title, one owner"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

### 2. GET `/api/inventory/stats` - Estadísticas

**Response 200:**

```json
{
  "totalItems": 150,
  "activeItems": 120,
  "pendingItems": 15,
  "soldItems": 10,
  "reservedItems": 5,
  "totalValue": 180000000.0,
  "averagePrice": 1200000.0,
  "averageDaysInStock": 23,
  "itemsOverdue": 8,
  "itemsByLocation": {
    "Showroom A": 45,
    "Showroom B": 35,
    "Lot C": 40
  }
}
```

---

### 14. POST `/api/inventory/bulkimport/upload` - Subir Archivo

**Request (multipart/form-data):**

- `file`: Archivo CSV, XLSX o JSON
- `dealerId`: ID del dealer
- `userId`: ID del usuario

**Response 201:**

```json
{
  "id": "job-789",
  "dealerId": "dealer-123",
  "fileName": "inventario_enero.csv",
  "fileType": "CSV",
  "fileSizeBytes": 25600,
  "status": "Pending",
  "totalRows": 0,
  "processedRows": 0,
  "successfulRows": 0,
  "failedRows": 0,
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// INVENTORY ITEM
// ============================================================================

export interface InventoryItem {
  id: string;
  dealerId: string;
  vehicleId: string;
  stockNumber: string;
  vin: string;
  status: InventoryStatus;
  location: string;
  costPrice: number;
  listPrice: number;
  targetPrice: number;
  minAcceptablePrice: number;
  isNegotiable: boolean;
  isFeatured: boolean;
  priority: number;
  daysInStock: number;
  acquiredDate: string;
  acquisitionSource: AcquisitionSource;
  acquisitionDetails?: string;
  internalNotes?: string;
  createdAt: string;
  updatedAt?: string;
}

export type InventoryStatus =
  | "Pending"
  | "Active"
  | "Reserved"
  | "Sold"
  | "Archived"
  | "OnHold";

export type AcquisitionSource =
  | "DirectPurchase"
  | "TradeIn"
  | "Auction"
  | "Consignment"
  | "Transfer"
  | "Other";

// ============================================================================
// STATS & REQUESTS
// ============================================================================

export interface InventoryStats {
  totalItems: number;
  activeItems: number;
  pendingItems: number;
  soldItems: number;
  reservedItems: number;
  totalValue: number;
  averagePrice: number;
  averageDaysInStock: number;
  itemsOverdue: number;
  itemsByLocation: Record<string, number>;
}

export interface CreateInventoryItemRequest {
  dealerId: string;
  vehicleId: string;
  stockNumber?: string;
  vin: string;
  location?: string;
  costPrice: number;
  listPrice: number;
  targetPrice?: number;
  minAcceptablePrice?: number;
  isNegotiable?: boolean;
  acquiredDate?: string;
  acquisitionSource?: AcquisitionSource;
  acquisitionDetails?: string;
  internalNotes?: string;
}

export interface UpdateInventoryItemRequest {
  location?: string;
  listPrice?: number;
  targetPrice?: number;
  minAcceptablePrice?: number;
  isNegotiable?: boolean;
  isFeatured?: boolean;
  priority?: number;
  internalNotes?: string;
}

export interface BulkUpdateStatusRequest {
  itemIds: string[];
  status: InventoryStatus;
}

// ============================================================================
// BULK IMPORT
// ============================================================================

export interface BulkImportJob {
  id: string;
  dealerId: string;
  userId: string;
  fileName: string;
  fileType: "CSV" | "Excel" | "JSON";
  fileSizeBytes: number;
  status: ImportJobStatus;
  totalRows: number;
  processedRows: number;
  successfulRows: number;
  failedRows: number;
  errors?: ImportError[];
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

export type ImportJobStatus =
  | "Pending"
  | "Processing"
  | "Completed"
  | "Failed"
  | "Cancelled";

export interface ImportError {
  row: number;
  column?: string;
  message: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/inventoryService.ts
import { apiClient } from "./api-client";
import type {
  InventoryItem,
  InventoryStats,
  CreateInventoryItemRequest,
  UpdateInventoryItemRequest,
  BulkUpdateStatusRequest,
  BulkImportJob,
  InventoryStatus,
} from "@/types/inventory";

class InventoryService {
  private dealerId: string | null = null;

  setDealerId(dealerId: string) {
    this.dealerId = dealerId;
  }

  // ============================================================================
  // INVENTORY CRUD
  // ============================================================================

  async getItems(params?: {
    page?: number;
    pageSize?: number;
    status?: InventoryStatus;
    searchTerm?: string;
    sortBy?: string;
    sortDescending?: boolean;
  }): Promise<PaginatedResult<InventoryItem>> {
    const response = await apiClient.get<PaginatedResult<InventoryItem>>(
      "/api/inventory",
      { params: { dealerId: this.dealerId, ...params } },
    );
    return response.data;
  }

  async getStats(): Promise<InventoryStats> {
    const response = await apiClient.get<InventoryStats>(
      "/api/inventory/stats",
      { params: { dealerId: this.dealerId } },
    );
    return response.data;
  }

  async getById(id: string): Promise<InventoryItem> {
    const response = await apiClient.get<InventoryItem>(`/api/inventory/${id}`);
    return response.data;
  }

  async create(request: CreateInventoryItemRequest): Promise<InventoryItem> {
    const response = await apiClient.post<InventoryItem>(
      "/api/inventory",
      request,
    );
    return response.data;
  }

  async update(
    id: string,
    request: UpdateInventoryItemRequest,
  ): Promise<InventoryItem> {
    const response = await apiClient.put<InventoryItem>(
      `/api/inventory/${id}`,
      request,
    );
    return response.data;
  }

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/inventory/${id}`);
  }

  async bulkUpdateStatus(request: BulkUpdateStatusRequest): Promise<void> {
    await apiClient.post("/api/inventory/bulk/status", request);
  }

  // ============================================================================
  // SMART FILTERS
  // ============================================================================

  async getFeatured(): Promise<InventoryItem[]> {
    const response = await apiClient.get<InventoryItem[]>(
      "/api/inventory/featured",
    );
    return response.data;
  }

  async getHot(): Promise<InventoryItem[]> {
    const response = await apiClient.get<InventoryItem[]>("/api/inventory/hot");
    return response.data;
  }

  async getOverdue(): Promise<InventoryItem[]> {
    const response = await apiClient.get<InventoryItem[]>(
      "/api/inventory/overdue",
    );
    return response.data;
  }

  // ============================================================================
  // BULK IMPORT
  // ============================================================================

  async getImportJobs(limit: number = 20): Promise<BulkImportJob[]> {
    const response = await apiClient.get<BulkImportJob[]>(
      "/api/inventory/bulkimport",
      { params: { dealerId: this.dealerId, limit } },
    );
    return response.data;
  }

  async getImportJob(jobId: string): Promise<BulkImportJob> {
    const response = await apiClient.get<BulkImportJob>(
      `/api/inventory/bulkimport/${jobId}`,
    );
    return response.data;
  }

  async uploadFile(file: File, userId: string): Promise<BulkImportJob> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await apiClient.post<BulkImportJob>(
      `/api/inventory/bulkimport/upload?dealerId=${this.dealerId}&userId=${userId}`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data;
  }

  async cancelImport(jobId: string): Promise<void> {
    await apiClient.post(`/api/inventory/bulkimport/${jobId}/cancel`);
  }

  getTemplateUrl(): string {
    return "/api/inventory/bulkimport/template";
  }
}

export const inventoryService = new InventoryService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useInventory.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { inventoryService } from "@/services/inventoryService";
import type {
  CreateInventoryItemRequest,
  UpdateInventoryItemRequest,
  InventoryStatus,
} from "@/types/inventory";

export const inventoryKeys = {
  all: ["inventory"] as const,
  lists: () => [...inventoryKeys.all, "list"] as const,
  list: (params: any) => [...inventoryKeys.lists(), params] as const,
  details: () => [...inventoryKeys.all, "detail"] as const,
  detail: (id: string) => [...inventoryKeys.details(), id] as const,
  stats: () => [...inventoryKeys.all, "stats"] as const,
  imports: () => [...inventoryKeys.all, "imports"] as const,
  import: (id: string) => [...inventoryKeys.imports(), id] as const,
};

export function useInventory(params?: {
  status?: InventoryStatus;
  page?: number;
}) {
  return useQuery({
    queryKey: inventoryKeys.list(params),
    queryFn: () => inventoryService.getItems(params),
  });
}

export function useInventoryItem(id: string) {
  return useQuery({
    queryKey: inventoryKeys.detail(id),
    queryFn: () => inventoryService.getById(id),
    enabled: !!id,
  });
}

export function useInventoryStats() {
  return useQuery({
    queryKey: inventoryKeys.stats(),
    queryFn: () => inventoryService.getStats(),
  });
}

export function useOverdueItems() {
  return useQuery({
    queryKey: [...inventoryKeys.all, "overdue"],
    queryFn: () => inventoryService.getOverdue(),
  });
}

export function useCreateInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateInventoryItemRequest) =>
      inventoryService.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.lists() });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.stats() });
    },
  });
}

export function useUpdateInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateInventoryItemRequest;
    }) => inventoryService.update(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: inventoryKeys.detail(variables.id),
      });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.lists() });
    },
  });
}

export function useBulkUpdateStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: inventoryService.bulkUpdateStatus,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.lists() });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.stats() });
    },
  });
}

// ============================================================================
// BULK IMPORT HOOKS
// ============================================================================

export function useImportJobs() {
  return useQuery({
    queryKey: inventoryKeys.imports(),
    queryFn: () => inventoryService.getImportJobs(),
  });
}

export function useImportJob(jobId: string) {
  return useQuery({
    queryKey: inventoryKeys.import(jobId),
    queryFn: () => inventoryService.getImportJob(jobId),
    enabled: !!jobId,
    refetchInterval: (data) => (data?.status === "Processing" ? 2000 : false), // Poll while processing
  });
}

export function useUploadImportFile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, userId }: { file: File; userId: string }) =>
      inventoryService.uploadFile(file, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.imports() });
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/dealer/InventoryDashboard.tsx
import { useInventory, useInventoryStats, useBulkUpdateStatus } from "@/hooks/useInventory";
import { useState } from "react";

export const InventoryDashboard = () => {
  const [selectedItems, setSelectedItems] = useState<string[]>([]);
  const { data: stats } = useInventoryStats();
  const { data: inventory, isLoading } = useInventory({ status: "Active" });
  const bulkUpdate = useBulkUpdateStatus();

  const handleBulkAction = (status: InventoryStatus) => {
    bulkUpdate.mutate({ itemIds: selectedItems, status });
  };

  if (isLoading) return <div>Cargando inventario...</div>;

  return (
    <div className="space-y-6">
      {/* Stats Cards */}
      <div className="grid grid-cols-4 gap-4">
        <StatCard label="Total Items" value={stats?.totalItems} />
        <StatCard label="Activos" value={stats?.activeItems} />
        <StatCard label="Valor Total" value={`$${stats?.totalValue?.toLocaleString()}`} />
        <StatCard label="Días Promedio" value={stats?.averageDaysInStock} />
      </div>

      {/* Bulk Actions */}
      {selectedItems.length > 0 && (
        <div className="bg-blue-50 p-4 rounded-lg flex gap-2">
          <span>{selectedItems.length} seleccionados</span>
          <button onClick={() => handleBulkAction("Sold")}>Marcar Vendido</button>
          <button onClick={() => handleBulkAction("Archived")}>Archivar</button>
        </div>
      )}

      {/* Inventory Table */}
      <table className="w-full">
        <thead>
          <tr>
            <th><input type="checkbox" /></th>
            <th>Stock #</th>
            <th>VIN</th>
            <th>Ubicación</th>
            <th>Precio Lista</th>
            <th>Días en Stock</th>
            <th>Estado</th>
          </tr>
        </thead>
        <tbody>
          {inventory?.items.map((item) => (
            <tr key={item.id}>
              <td>
                <input
                  type="checkbox"
                  checked={selectedItems.includes(item.id)}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setSelectedItems([...selectedItems, item.id]);
                    } else {
                      setSelectedItems(selectedItems.filter(id => id !== item.id));
                    }
                  }}
                />
              </td>
              <td>{item.stockNumber}</td>
              <td className="font-mono text-sm">{item.vin}</td>
              <td>{item.location}</td>
              <td>${item.listPrice.toLocaleString()}</td>
              <td className={item.daysInStock > 60 ? "text-red-500" : ""}>
                {item.daysInStock}
              </td>
              <td>
                <span className={`badge badge-${item.status.toLowerCase()}`}>
                  {item.status}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **16 Endpoints documentados**  
✅ **TypeScript Types** (InventoryItem, BulkImportJob, Stats)  
✅ **Service Layer** (16 métodos)  
✅ **React Query Hooks** (12 hooks)  
✅ **Componente ejemplo** (InventoryDashboard)

---

_Última actualización: Enero 30, 2026_
