# 👤 23 - Sellers API

**Servicio:** UserService  
**Puerto:** 8080  
**Base Path:** `/api/sellers`  
**Autenticación:** ✅ Parcial

---

## 📖 Descripción

Gestión de perfiles de vendedores individuales (no dealers). Permite crear perfiles de vendedor, gestionar verificación y consultar estadísticas.

---

## 🎯 Endpoints Disponibles

| #   | Método | Endpoint                         | Auth     | Descripción                |
| --- | ------ | -------------------------------- | -------- | -------------------------- |
| 1   | `POST` | `/api/sellers`                   | ✅       | Crear perfil de vendedor   |
| 2   | `GET`  | `/api/sellers/{sellerId}`        | ❌       | Obtener perfil por ID      |
| 3   | `GET`  | `/api/sellers/user/{userId}`     | ❌       | Obtener perfil por User ID |
| 4   | `PUT`  | `/api/sellers/{sellerId}`        | ✅       | Actualizar perfil          |
| 5   | `POST` | `/api/sellers/{sellerId}/verify` | ✅ Admin | Verificar vendedor         |
| 6   | `GET`  | `/api/sellers/{sellerId}/stats`  | ❌       | Estadísticas del vendedor  |

---

## 📝 Detalle de Endpoints

### 1. POST `/api/sellers` - Crear Perfil

**Request:**

```json
{
  "userId": "user-123",
  "displayName": "Carlos Venta",
  "phone": "+1 809-555-0100",
  "whatsapp": "+1 809-555-0100",
  "bio": "Vendedor particular con 5 años de experiencia",
  "province": "Santo Domingo",
  "city": "Santo Domingo Este",
  "preferredContactMethod": "WhatsApp",
  "availableHours": "9AM - 6PM",
  "languages": ["Español", "English"]
}
```

**Response 201:**

```json
{
  "id": "seller-001",
  "userId": "user-123",
  "displayName": "Carlos Venta",
  "phone": "+1 809-555-0100",
  "whatsapp": "+1 809-555-0100",
  "bio": "Vendedor particular con 5 años de experiencia",
  "province": "Santo Domingo",
  "city": "Santo Domingo Este",
  "preferredContactMethod": "WhatsApp",
  "isVerified": false,
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 2. GET `/api/sellers/{sellerId}` - Obtener Perfil

**Response 200:**

```json
{
  "id": "seller-001",
  "userId": "user-123",
  "displayName": "Carlos Venta",
  "phone": "+1 809-555-0100",
  "whatsapp": "+1 809-555-0100",
  "bio": "Vendedor particular con 5 años de experiencia",
  "province": "Santo Domingo",
  "city": "Santo Domingo Este",
  "preferredContactMethod": "WhatsApp",
  "availableHours": "9AM - 6PM",
  "languages": ["Español", "English"],
  "isVerified": true,
  "verifiedAt": "2026-01-20T10:00:00Z",
  "rating": 4.8,
  "reviewCount": 12,
  "createdAt": "2026-01-15T10:00:00Z"
}
```

---

### 5. POST `/api/sellers/{sellerId}/verify` - Verificar (Admin)

**Request:**

```json
{
  "isVerified": true,
  "verificationNotes": "Documentos verificados correctamente"
}
```

**Response 200:**

```json
{
  "message": "Seller verified successfully"
}
```

---

### 6. GET `/api/sellers/{sellerId}/stats` - Estadísticas

**Response 200:**

```json
{
  "sellerId": "seller-001",
  "totalListings": 8,
  "activeListings": 3,
  "soldListings": 5,
  "totalViews": 2450,
  "totalContacts": 89,
  "averageResponseTime": "2h 15m",
  "rating": 4.8,
  "reviewCount": 12,
  "memberSince": "2025-06-15",
  "lastActiveAt": "2026-01-30T08:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// SELLER PROFILE
// ============================================================================

export interface SellerProfile {
  id: string;
  userId: string;
  displayName: string;
  phone?: string;
  whatsapp?: string;
  bio?: string;
  province: string;
  city?: string;
  preferredContactMethod: ContactMethod;
  availableHours?: string;
  languages: string[];
  isVerified: boolean;
  verifiedAt?: string;
  rating?: number;
  reviewCount: number;
  createdAt: string;
  updatedAt?: string;
}

export type ContactMethod = "Phone" | "WhatsApp" | "Email" | "Any";

// ============================================================================
// SELLER STATS
// ============================================================================

export interface SellerStats {
  sellerId: string;
  totalListings: number;
  activeListings: number;
  soldListings: number;
  totalViews: number;
  totalContacts: number;
  averageResponseTime: string;
  rating: number;
  reviewCount: number;
  memberSince: string;
  lastActiveAt: string;
}

// ============================================================================
// REQUESTS
// ============================================================================

export interface CreateSellerProfileRequest {
  userId: string;
  displayName: string;
  phone?: string;
  whatsapp?: string;
  bio?: string;
  province: string;
  city?: string;
  preferredContactMethod?: ContactMethod;
  availableHours?: string;
  languages?: string[];
}

export interface UpdateSellerProfileRequest {
  displayName?: string;
  phone?: string;
  whatsapp?: string;
  bio?: string;
  province?: string;
  city?: string;
  preferredContactMethod?: ContactMethod;
  availableHours?: string;
  languages?: string[];
}

export interface VerifySellerProfileRequest {
  isVerified: boolean;
  verificationNotes?: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/sellerService.ts
import { apiClient } from "./api-client";
import type {
  SellerProfile,
  SellerStats,
  CreateSellerProfileRequest,
  UpdateSellerProfileRequest,
  VerifySellerProfileRequest,
} from "@/types/seller";

class SellerService {
  async create(request: CreateSellerProfileRequest): Promise<SellerProfile> {
    const response = await apiClient.post<SellerProfile>(
      "/api/sellers",
      request,
    );
    return response.data;
  }

  async getById(sellerId: string): Promise<SellerProfile> {
    const response = await apiClient.get<SellerProfile>(
      `/api/sellers/${sellerId}`,
    );
    return response.data;
  }

  async getByUserId(userId: string): Promise<SellerProfile> {
    const response = await apiClient.get<SellerProfile>(
      `/api/sellers/user/${userId}`,
    );
    return response.data;
  }

  async update(
    sellerId: string,
    request: UpdateSellerProfileRequest,
  ): Promise<SellerProfile> {
    const response = await apiClient.put<SellerProfile>(
      `/api/sellers/${sellerId}`,
      request,
    );
    return response.data;
  }

  async verify(
    sellerId: string,
    request: VerifySellerProfileRequest,
  ): Promise<void> {
    await apiClient.post(`/api/sellers/${sellerId}/verify`, request);
  }

  async getStats(sellerId: string): Promise<SellerStats> {
    const response = await apiClient.get<SellerStats>(
      `/api/sellers/${sellerId}/stats`,
    );
    return response.data;
  }
}

export const sellerService = new SellerService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useSellers.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { sellerService } from "@/services/sellerService";
import type {
  CreateSellerProfileRequest,
  UpdateSellerProfileRequest,
} from "@/types/seller";

export const sellerKeys = {
  all: ["sellers"] as const,
  details: () => [...sellerKeys.all, "detail"] as const,
  detail: (id: string) => [...sellerKeys.details(), id] as const,
  byUser: (userId: string) => [...sellerKeys.all, "user", userId] as const,
  stats: (id: string) => [...sellerKeys.all, "stats", id] as const,
};

export function useSeller(sellerId: string) {
  return useQuery({
    queryKey: sellerKeys.detail(sellerId),
    queryFn: () => sellerService.getById(sellerId),
    enabled: !!sellerId,
  });
}

export function useSellerByUser(userId: string) {
  return useQuery({
    queryKey: sellerKeys.byUser(userId),
    queryFn: () => sellerService.getByUserId(userId),
    enabled: !!userId,
    retry: false, // Don't retry if no seller profile exists
  });
}

export function useSellerStats(sellerId: string) {
  return useQuery({
    queryKey: sellerKeys.stats(sellerId),
    queryFn: () => sellerService.getStats(sellerId),
    enabled: !!sellerId,
  });
}

export function useCreateSellerProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateSellerProfileRequest) =>
      sellerService.create(request),
    onSuccess: (data) => {
      queryClient.setQueryData(sellerKeys.detail(data.id), data);
      queryClient.setQueryData(sellerKeys.byUser(data.userId), data);
    },
  });
}

export function useUpdateSellerProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      sellerId,
      request,
    }: {
      sellerId: string;
      request: UpdateSellerProfileRequest;
    }) => sellerService.update(sellerId, request),
    onSuccess: (data, variables) => {
      queryClient.setQueryData(sellerKeys.detail(variables.sellerId), data);
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/seller/SellerProfileCard.tsx
import { useSeller, useSellerStats } from "@/hooks/useSellers";
import { FiCheck, FiPhone, FiMessageCircle, FiStar } from "react-icons/fi";
import { FaWhatsapp } from "react-icons/fa";

interface SellerProfileCardProps {
  sellerId: string;
  onContact?: () => void;
}

export const SellerProfileCard = ({ sellerId, onContact }: SellerProfileCardProps) => {
  const { data: seller, isLoading } = useSeller(sellerId);
  const { data: stats } = useSellerStats(sellerId);

  if (isLoading) return <div className="animate-pulse h-48 bg-gray-100 rounded-lg" />;
  if (!seller) return null;

  return (
    <div className="border rounded-lg p-4 bg-white shadow-sm">
      {/* Header */}
      <div className="flex items-center gap-3 mb-4">
        <div className="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center text-xl font-bold text-blue-600">
          {seller.displayName[0]}
        </div>
        <div>
          <h3 className="font-semibold flex items-center gap-2">
            {seller.displayName}
            {seller.isVerified && (
              <span className="bg-green-100 text-green-600 p-1 rounded-full">
                <FiCheck className="w-3 h-3" />
              </span>
            )}
          </h3>
          <p className="text-sm text-gray-500">{seller.city}, {seller.province}</p>
        </div>
      </div>

      {/* Rating */}
      {seller.rating && (
        <div className="flex items-center gap-2 mb-3">
          <FiStar className="text-yellow-500 fill-current" />
          <span className="font-medium">{seller.rating.toFixed(1)}</span>
          <span className="text-gray-500 text-sm">({seller.reviewCount} reseñas)</span>
        </div>
      )}

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-3 gap-2 mb-4 text-center text-sm">
          <div className="bg-gray-50 p-2 rounded">
            <div className="font-bold">{stats.soldListings}</div>
            <div className="text-gray-500">Vendidos</div>
          </div>
          <div className="bg-gray-50 p-2 rounded">
            <div className="font-bold">{stats.activeListings}</div>
            <div className="text-gray-500">Activos</div>
          </div>
          <div className="bg-gray-50 p-2 rounded">
            <div className="font-bold">{stats.averageResponseTime}</div>
            <div className="text-gray-500">Respuesta</div>
          </div>
        </div>
      )}

      {/* Contact Buttons */}
      <div className="flex gap-2">
        {seller.whatsapp && (
          <a
            href={`https://wa.me/${seller.whatsapp.replace(/\D/g, '')}`}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 btn btn-success flex items-center justify-center gap-2"
          >
            <FaWhatsapp /> WhatsApp
          </a>
        )}
        {seller.phone && (
          <a
            href={`tel:${seller.phone}`}
            className="flex-1 btn btn-outline flex items-center justify-center gap-2"
          >
            <FiPhone /> Llamar
          </a>
        )}
      </div>

      {/* Bio */}
      {seller.bio && (
        <p className="text-sm text-gray-600 mt-4">{seller.bio}</p>
      )}
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **6 Endpoints documentados**  
✅ **TypeScript Types** (SellerProfile, SellerStats)  
✅ **Service Layer** (6 métodos)  
✅ **React Query Hooks** (5 hooks)  
✅ **Componente ejemplo** (SellerProfileCard)

---

_Última actualización: Enero 30, 2026_
