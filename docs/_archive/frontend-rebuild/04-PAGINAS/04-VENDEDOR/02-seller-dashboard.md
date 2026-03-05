---
title: "54 - Seller Dashboard"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "AuthService"]
status: complete
last_updated: "2026-01-30"
---

# 🏪 54 - Seller Dashboard

**Objetivo:** Dashboard para vendedores individuales (no dealers) para gestionar sus publicaciones personales.

**Prioridad:** P1 (Alta)  
**Complejidad:** 🟡 Media  
**Dependencias:** VehiclesSaleService, AuthService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [Páginas](#-páginas)
3. [Tipos TypeScript](#-tipos-typescript)
4. [Flujo de Usuario](#-flujo-de-usuario)

---

## 🏗️ ARQUITECTURA

```
pages/seller/
├── SellerDashboardPage.tsx       # Dashboard principal del vendedor
├── SellerProfileSettingsPage.tsx # Configuración de perfil
└── SellerPublicProfilePage.tsx   # Perfil público
```

### Diferencia Seller vs Dealer

| Aspecto          | Seller (Individual)   | Dealer                 |
| ---------------- | --------------------- | ---------------------- |
| Tipo de Cuenta   | Individual            | Business               |
| Pago             | Por publicación ($29) | Suscripción mensual    |
| Límite Vehículos | 3 activos máximo      | Según plan (15-∞)      |
| Dashboard        | Básico                | Avanzado con analytics |
| Layout           | MainLayout            | DealerPortalLayout     |

### Navegación

```
/seller/dashboard    → Dashboard de mis vehículos
/seller/settings     → Configuración de perfil
/seller/:id          → Perfil público del vendedor
/sell                → Publicar nuevo vehículo
```

---

## 📄 PÁGINAS

### 1. SellerDashboardPage.tsx

**Ruta:** `/seller/dashboard`

```typescript
// src/pages/seller/SellerDashboardPage.tsx
"use client";

import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import MainLayout from '@/layouts/MainLayout';
import { useAuth } from '@/hooks/useAuth';
import {
  FiPlus,
  FiEye,
  FiMessageSquare,
  FiDollarSign,
  FiTrendingUp,
  FiClock,
  FiCheckCircle,
  FiAlertCircle,
  FiEdit,
  FiTrash2,
  FiMoreVertical,
  FiLoader,
} from 'react-icons/fi';
import { FaCar } from 'react-icons/fa';
import { useSellerListings } from '@/hooks/useSellerListings';

interface VehicleListing {
  id: string;
  title: string;
  price: number;
  status: 'active' | 'pending' | 'sold' | 'expired';
  views: number;
  inquiries: number;
  createdAt: string;
  image: string;
  expiresAt?: string;
}

const statusConfig = {
  active: {
    label: 'Activo',
    color: 'bg-green-100 text-green-700',
    icon: FiCheckCircle
  },
  pending: {
    label: 'Pendiente',
    color: 'bg-yellow-100 text-yellow-700',
    icon: FiClock
  },
  sold: {
    label: 'Vendido',
    color: 'bg-blue-100 text-blue-700',
    icon: FiDollarSign
  },
  expired: {
    label: 'Expirado',
    color: 'bg-red-100 text-red-700',
    icon: FiAlertCircle
  },
};

export default function SellerDashboardPage() {
  const { user } = useAuth();
  const { listings, isLoading, stats, refetch } = useSellerListings();

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      minimumFractionDigits: 0,
    }).format(amount);
  };

  const statCards = [
    {
      label: 'Publicaciones Activas',
      value: stats?.activeCount ?? 0,
      max: 3,
      icon: FaCar,
      color: 'text-blue-600 bg-blue-100',
    },
    {
      label: 'Vistas Totales',
      value: stats?.totalViews ?? 0,
      icon: FiEye,
      color: 'text-purple-600 bg-purple-100',
    },
    {
      label: 'Consultas Recibidas',
      value: stats?.totalInquiries ?? 0,
      icon: FiMessageSquare,
      color: 'text-green-600 bg-green-100',
    },
    {
      label: 'Valor Total',
      value: formatCurrency(stats?.totalValue ?? 0),
      icon: FiDollarSign,
      color: 'text-amber-600 bg-amber-100',
    },
  ];

  if (isLoading) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center">
          <FiLoader className="w-8 h-8 animate-spin text-blue-600" />
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {/* Header */}
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
            <div>
              <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">
                Mis Vehículos en Venta
              </h1>
              <p className="text-gray-600 mt-1">
                Hola, {user?.firstName || 'Vendedor'}. Gestiona tus publicaciones.
              </p>
            </div>
            <Link
              to="/sell"
              className="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-purple-600 to-indigo-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all"
            >
              <FiPlus className="w-5 h-5" />
              Publicar Vehículo
            </Link>
          </div>

          {/* Stats Grid */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            {statCards.map((stat, index) => (
              <div
                key={index}
                className="bg-white rounded-xl p-4 sm:p-6 shadow-sm border border-gray-100"
              >
                <div className="flex items-center gap-3">
                  <div className={`p-3 rounded-xl ${stat.color}`}>
                    <stat.icon className="w-5 h-5 sm:w-6 sm:h-6" />
                  </div>
                  <div>
                    <div className="flex items-baseline gap-1">
                      <p className="text-xl sm:text-2xl font-bold text-gray-900">
                        {stat.value}
                      </p>
                      {stat.max && (
                        <span className="text-sm text-gray-500">/{stat.max}</span>
                      )}
                    </div>
                    <p className="text-xs sm:text-sm text-gray-500">{stat.label}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Tips Banner */}
          <div className="bg-gradient-to-r from-purple-600 to-indigo-600 rounded-2xl p-6 mb-8 text-white">
            <div className="flex items-start gap-4">
              <div className="p-3 bg-white/20 rounded-xl">
                <FiTrendingUp className="w-6 h-6" />
              </div>
              <div>
                <h3 className="font-semibold text-lg">Tips para vender más rápido</h3>
                <ul className="mt-2 space-y-1 text-purple-100 text-sm">
                  <li>• Agrega fotos de alta calidad (mínimo 5 fotos)</li>
                  <li>• Completa todos los detalles del vehículo</li>
                  <li>• Responde rápido a las consultas</li>
                  <li>• Mantén tu precio competitivo con el mercado</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Upgrade to Dealer Banner */}
          {stats?.activeCount >= 2 && (
            <div className="bg-gradient-to-r from-amber-500 to-orange-500 rounded-2xl p-6 mb-8 text-white">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="font-bold text-lg">¿Vendes más de 3 vehículos?</h3>
                  <p className="text-amber-100 mt-1">
                    Conviértete en Dealer y obtén publicaciones ilimitadas desde RD$2,900/mes
                  </p>
                </div>
                <Link
                  to="/dealer/landing"
                  className="px-6 py-3 bg-white text-amber-600 rounded-xl font-semibold hover:bg-amber-50 transition-colors"
                >
                  Conocer Planes
                </Link>
              </div>
            </div>
          )}

          {/* Listings */}
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-100">
              <h2 className="text-lg font-semibold text-gray-900">Mis Publicaciones</h2>
            </div>

            {listings.length === 0 ? (
              <EmptyState />
            ) : (
              <div className="divide-y divide-gray-100">
                {listings.map((listing) => (
                  <ListingRow key={listing.id} listing={listing} />
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </MainLayout>
  );
}

// Empty State Component
function EmptyState() {
  return (
    <div className="p-12 text-center">
      <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <FaCar className="w-8 h-8 text-gray-400" />
      </div>
      <h3 className="text-lg font-medium text-gray-900 mb-2">
        No tienes publicaciones aún
      </h3>
      <p className="text-gray-500 mb-6">
        Comienza a vender tu vehículo publicándolo en OKLA
      </p>
      <Link
        to="/sell"
        className="inline-flex items-center gap-2 px-6 py-3 bg-purple-600 text-white rounded-xl font-semibold hover:bg-purple-700 transition-colors"
      >
        <FiPlus className="w-5 h-5" />
        Publicar mi primer vehículo
      </Link>
    </div>
  );
}

// Listing Row Component
function ListingRow({ listing }: { listing: VehicleListing }) {
  const StatusIcon = statusConfig[listing.status].icon;

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      minimumFractionDigits: 0,
    }).format(amount);
  };

  return (
    <div className="p-4 sm:p-6 hover:bg-gray-50 transition-colors">
      <div className="flex flex-col sm:flex-row gap-4">
        {/* Image */}
        <div className="w-full sm:w-40 h-32 sm:h-28 rounded-xl overflow-hidden flex-shrink-0">
          <img
            src={listing.image}
            alt={listing.title}
            className="w-full h-full object-cover"
          />
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <div>
              <h3 className="font-semibold text-gray-900 truncate">
                {listing.title}
              </h3>
              <p className="text-lg font-bold text-blue-600 mt-1">
                {formatCurrency(listing.price)}
              </p>
            </div>
            <span
              className={`flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium ${
                statusConfig[listing.status].color
              }`}
            >
              <StatusIcon className="w-3 h-3" />
              {statusConfig[listing.status].label}
            </span>
          </div>

          {/* Stats */}
          <div className="flex items-center gap-4 mt-3 text-sm text-gray-500">
            <span className="flex items-center gap-1">
              <FiEye className="w-4 h-4" />
              {listing.views} vistas
            </span>
            <span className="flex items-center gap-1">
              <FiMessageSquare className="w-4 h-4" />
              {listing.inquiries} consultas
            </span>
            <span className="flex items-center gap-1">
              <FiClock className="w-4 h-4" />
              {new Date(listing.createdAt).toLocaleDateString('es-DO')}
            </span>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-2 mt-4">
            <Link
              to={`/vehicles/${listing.id}/edit`}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <FiEdit className="w-4 h-4" />
              Editar
            </Link>
            <Link
              to={`/vehicles/${listing.id}`}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <FiEye className="w-4 h-4" />
              Ver
            </Link>
            {listing.status === 'active' && (
              <button
                className="flex items-center gap-1 px-3 py-1.5 text-sm text-green-600 hover:bg-green-50 rounded-lg transition-colors"
              >
                <FiDollarSign className="w-4 h-4" />
                Marcar Vendido
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

### 2. SellerProfileSettingsPage.tsx

**Ruta:** `/seller/settings`

```typescript
// src/pages/seller/SellerProfileSettingsPage.tsx
"use client";

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import MainLayout from '@/layouts/MainLayout';
import { useAuth } from '@/hooks/useAuth';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  FiUser,
  FiPhone,
  FiMail,
  FiMapPin,
  FiCamera,
  FiSave,
  FiLoader,
} from 'react-icons/fi';
import { toast } from 'sonner';

const profileSchema = z.object({
  displayName: z.string().min(2, 'Mínimo 2 caracteres'),
  phone: z.string().min(10, 'Teléfono inválido'),
  whatsapp: z.string().optional(),
  location: z.string().min(2, 'Ingresa tu ubicación'),
  bio: z.string().max(500, 'Máximo 500 caracteres').optional(),
  showPhone: z.boolean(),
  showWhatsapp: z.boolean(),
  showEmail: z.boolean(),
});

type ProfileFormData = z.infer<typeof profileSchema>;

export default function SellerProfileSettingsPage() {
  const { user, updateProfile } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      displayName: user?.name || '',
      phone: user?.phone || '',
      whatsapp: user?.whatsapp || '',
      location: user?.location || '',
      bio: user?.bio || '',
      showPhone: user?.showPhone ?? true,
      showWhatsapp: user?.showWhatsapp ?? true,
      showEmail: user?.showEmail ?? false,
    },
  });

  const handleAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setAvatarFile(file);
      setAvatarPreview(URL.createObjectURL(file));
    }
  };

  const onSubmit = async (data: ProfileFormData) => {
    setIsSubmitting(true);
    try {
      await updateProfile(data);
      toast.success('Perfil actualizado correctamente');
    } catch {
      toast.error('Error al actualizar perfil');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-2xl mx-auto px-4">
          <h1 className="text-2xl font-bold text-gray-900 mb-8">
            Configuración de Perfil de Vendedor
          </h1>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            {/* Avatar */}
            <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
              <h2 className="font-semibold text-gray-900 mb-4">Foto de Perfil</h2>
              <div className="flex items-center gap-6">
                <div className="relative">
                  <div className="w-24 h-24 rounded-full bg-gray-200 overflow-hidden">
                    {avatarPreview || user?.avatar ? (
                      <img
                        src={avatarPreview || user?.avatar}
                        alt="Avatar"
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <FiUser className="w-12 h-12 text-gray-400" />
                      </div>
                    )}
                  </div>
                  <label className="absolute bottom-0 right-0 p-2 bg-blue-600 text-white rounded-full cursor-pointer hover:bg-blue-700">
                    <FiCamera className="w-4 h-4" />
                    <input
                      type="file"
                      accept="image/*"
                      onChange={handleAvatarChange}
                      className="hidden"
                    />
                  </label>
                </div>
                <p className="text-sm text-gray-500">
                  JPG o PNG. Máximo 2MB.
                </p>
              </div>
            </div>

            {/* Basic Info */}
            <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
              <h2 className="font-semibold text-gray-900 mb-4">Información Básica</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nombre para mostrar
                  </label>
                  <input
                    {...register('displayName')}
                    className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                  />
                  {errors.displayName && (
                    <p className="text-red-500 text-sm mt-1">{errors.displayName.message}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Teléfono
                  </label>
                  <input
                    {...register('phone')}
                    type="tel"
                    className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                  />
                  {errors.phone && (
                    <p className="text-red-500 text-sm mt-1">{errors.phone.message}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    WhatsApp (opcional)
                  </label>
                  <input
                    {...register('whatsapp')}
                    type="tel"
                    className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Ubicación
                  </label>
                  <input
                    {...register('location')}
                    placeholder="Santo Domingo, DN"
                    className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                  />
                  {errors.location && (
                    <p className="text-red-500 text-sm mt-1">{errors.location.message}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Sobre mí (opcional)
                  </label>
                  <textarea
                    {...register('bio')}
                    rows={3}
                    maxLength={500}
                    className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                    placeholder="Cuéntale a los compradores sobre ti..."
                  />
                </div>
              </div>
            </div>

            {/* Privacy Settings */}
            <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
              <h2 className="font-semibold text-gray-900 mb-4">Privacidad de Contacto</h2>
              <div className="space-y-3">
                <label className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    {...register('showPhone')}
                    className="w-5 h-5 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-gray-700">Mostrar número de teléfono</span>
                </label>
                <label className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    {...register('showWhatsapp')}
                    className="w-5 h-5 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-gray-700">Mostrar botón de WhatsApp</span>
                </label>
                <label className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    {...register('showEmail')}
                    className="w-5 h-5 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-gray-700">Mostrar email</span>
                </label>
              </div>
            </div>

            {/* Submit */}
            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full py-3 bg-blue-600 text-white rounded-xl font-semibold hover:bg-blue-700 disabled:opacity-50 flex items-center justify-center gap-2"
            >
              {isSubmitting ? (
                <FiLoader className="w-5 h-5 animate-spin" />
              ) : (
                <FiSave className="w-5 h-5" />
              )}
              Guardar Cambios
            </button>
          </form>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 📝 TIPOS TYPESCRIPT

```typescript
// src/types/seller.ts
export interface SellerListing {
  id: string;
  title: string;
  price: number;
  status: "active" | "pending" | "sold" | "expired";
  views: number;
  inquiries: number;
  createdAt: string;
  expiresAt?: string;
  image: string;
  make: string;
  model: string;
  year: number;
}

export interface SellerStats {
  activeCount: number;
  pendingCount: number;
  soldCount: number;
  totalViews: number;
  totalInquiries: number;
  totalValue: number;
}

export interface SellerProfile {
  id: string;
  displayName: string;
  avatar?: string;
  phone?: string;
  whatsapp?: string;
  email: string;
  location: string;
  bio?: string;
  memberSince: string;
  totalSold: number;
  responseRate: number;
  averageResponseTime: string;
  showPhone: boolean;
  showWhatsapp: boolean;
  showEmail: boolean;
}
```

---

## 🪝 HOOKS

```typescript
// src/hooks/useSellerListings.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { vehicleService } from "@/services/vehicleService";
import { useAuth } from "@/hooks/useAuth";

export function useSellerListings() {
  const { user } = useAuth();

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["seller-listings", user?.id],
    queryFn: () => vehicleService.getMyListings(),
    enabled: !!user?.id,
  });

  const stats = data
    ? {
        activeCount: data.filter((l) => l.status === "active").length,
        pendingCount: data.filter((l) => l.status === "pending").length,
        soldCount: data.filter((l) => l.status === "sold").length,
        totalViews: data.reduce((acc, l) => acc + l.views, 0),
        totalInquiries: data.reduce((acc, l) => acc + l.inquiries, 0),
        totalValue: data.reduce((acc, l) => acc + l.price, 0),
      }
    : null;

  return {
    listings: data || [],
    isLoading,
    stats,
    refetch,
  };
}
```

---

## 🛣️ RUTAS

```typescript
// src/App.tsx
import SellerDashboardPage from './pages/seller/SellerDashboardPage';
import SellerProfileSettingsPage from './pages/seller/SellerProfileSettingsPage';
import SellerPublicProfilePage from './pages/seller/SellerPublicProfilePage';

<Route
  path="/seller/dashboard"
  element={
    <ProtectedRoute>
      <SellerDashboardPage />
    </ProtectedRoute>
  }
/>
<Route
  path="/seller/settings"
  element={
    <ProtectedRoute>
      <SellerProfileSettingsPage />
    </ProtectedRoute>
  }
/>
<Route
  path="/seller/:id"
  element={<SellerPublicProfilePage />}
/>
```

---

## 🔄 FLUJO DE USUARIO

```
┌─────────────────────────────────────────────────────────────────┐
│              FLUJO VENDEDOR INDIVIDUAL                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1️⃣ PUBLICAR VEHÍCULO                                           │
│  ├─> Usuario hace clic en "Vender" en Navbar                   │
│  ├─> /sell → Formulario paso a paso                            │
│  ├─> Pago de RD$29 por publicación                             │
│  └─> Vehículo queda en status "pending"                        │
│                                                                 │
│  2️⃣ GESTIONAR PUBLICACIONES                                     │
│  ├─> /seller/dashboard → Dashboard con stats                   │
│  ├─> Ver vistas, consultas, estado                             │
│  ├─> Editar → /vehicles/{id}/edit                              │
│  ├─> Marcar como vendido                                       │
│  └─> Máximo 3 publicaciones activas                            │
│                                                                 │
│  3️⃣ RESPONDER CONSULTAS                                         │
│  ├─> /messages → Ver consultas de compradores                  │
│  ├─> Responder por chat interno o WhatsApp                     │
│  └─> Tasa de respuesta afecta visibilidad                      │
│                                                                 │
│  4️⃣ PERFIL PÚBLICO                                              │
│  ├─> /seller/{id} → Perfil visible para compradores            │
│  ├─> Muestra: nombre, ubicación, vehículos, rating             │
│  └─> Configurar en /seller/settings                            │
│                                                                 │
│  5️⃣ UPGRADE A DEALER (OPCIONAL)                                 │
│  ├─> Si vende >3 vehículos, sugerir upgrade                    │
│  ├─> /dealer/landing → Ver beneficios                          │
│  └─> Suscripción mensual desde RD$2,900                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ VALIDACIÓN

- [ ] Dashboard muestra stats correctos
- [ ] Lista de vehículos con estados
- [ ] Límite de 3 publicaciones activas
- [ ] Botón "Marcar Vendido" funciona
- [ ] Link a edición de vehículo
- [ ] Banner de upgrade a dealer
- [ ] Tips para vender más rápido
- [ ] Configuración de perfil funcional
- [ ] Privacidad de contacto

---

_Última actualización: Enero 2026_

---

# ANEXO: Dashboard de Usuario (Integración)

> Fusionado desde 02-dashboard.md

Este documento complementa:

- [process-matrix/15-CONFIANZA-SEGURIDAD/02-garantia-inspeccion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/02-garantia-inspeccion.md) - **Garantías** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/03-devolucion-cancelacion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/03-devolucion-cancelacion.md) - **Devoluciones** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/04-disputas-mediacion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/04-disputas-mediacion.md) - **Disputas** ⭐

**Estado:** ✅ WarrantyService 100% BE | 🔴 ReturnService 0% | ✅ DisputeService 80% BE

### Nuevas Secciones en el Dashboard

```typescript
const navItems = [
  { href: "/dashboard", label: "Resumen", icon: LayoutDashboard },
  { href: "/dashboard/vehiculos", label: "Mis vehículos", icon: Car },
  { href: "/dashboard/favoritos", label: "Favoritos", icon: Heart },
  { href: "/dashboard/mensajes", label: "Mensajes", icon: MessageCircle },

  // NUEVAS SECCIONES DE PROTECCIÓN ⭐
  { href: "/dashboard/compras", label: "Mis compras", icon: ShoppingBag },
  { href: "/dashboard/disputas", label: "Disputas", icon: AlertTriangle },
  { href: "/dashboard/garantias", label: "Garantías", icon: Shield },
  { href: "/dashboard/devoluciones", label: "Devoluciones", icon: RefreshCcw },

  { href: "/dashboard/perfil", label: "Mi perfil", icon: User },
  { href: "/dashboard/configuracion", label: "Configuración", icon: Settings },
];
```

### Servicios y Endpoints

| Servicio        | Puerto | Función                 | Estado              |
| --------------- | ------ | ----------------------- | ------------------- |
| WarrantyService | 5083   | Garantías, inspecciones | ✅ 100% BE + 40% UI |
| DisputeService  | 5089   | Resolución de disputas  | ✅ 80% BE + 0% UI   |
| TrustService    | 5082   | Devoluciones            | 🔴 0% (planificado) |

**Endpoints clave para Dashboard:**

```typescript
GET / api / warranty / inspections; // Mis inspecciones
GET / api / warranty / claims; // Mis reclamos de garantía
GET / api / disputes; // Mis disputas
GET / api / trust / returns; // Mis solicitudes de devolución
```

### Stats Cards a Agregar

```tsx
// En DashboardStats component
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
  {/* Existing cards: Vehiculos, Favoritos, Mensajes, Vistas */}

  {/* NEW: Warranty & Protection Cards */}
  <StatCard
    icon={Shield}
    label="Garantías Activas"
    value={stats.activeWarranties}
    change="+2 este mes"
    changeType="positive"
  />
  <StatCard
    icon={AlertTriangle}
    label="Disputas Abiertas"
    value={stats.openDisputes}
    change={stats.openDisputes > 0 ? "Requiere atención" : "Todo bien"}
    changeType={stats.openDisputes > 0 ? "warning" : "neutral"}
  />
</div>
```

### Recent Activity - Nuevos Tipos

```typescript
// Agregar estos tipos a RecentActivity component
type ActivityType =
  | "vehicle_created"
  | "vehicle_sold"
  | "message_received"
  | "warranty_claimed" // NUEVO ⭐
  | "dispute_opened" // NUEVO ⭐
  | "return_requested" // NUEVO ⭐
  | "inspection_completed"; // NUEVO ⭐
```

---

## 📋 OBJETIVO

Implementar dashboard de usuario:

- Vista general de actividad
- Mis vehículos
- Favoritos
- Mensajes
- **Mis compras y transacciones** ⭐
- **Disputas activas** ⭐
- **Garantías y reclamos** ⭐
- **Solicitudes de devolución** ⭐
- Configuración rápida

---

## 🔧 PASO 1: Layout del Dashboard

```typescript
// filepath: src/app/(main)/dashboard/layout.tsx
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { DashboardSidebar } from "@/components/dashboard/DashboardSidebar";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dashboard");
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container py-8">
        <div className="flex flex-col lg:flex-row gap-8">
          {/* Sidebar */}
          <aside className="lg:w-64 flex-shrink-0">
            <DashboardSidebar />
          </aside>

          {/* Main content */}
          <main className="flex-1 min-w-0">{children}</main>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: DashboardSidebar

```typescript
// filepath: src/components/dashboard/DashboardSidebar.tsx
"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LayoutDashboard, Car, Heart, MessageCircle, Settings, User } from "lucide-react";
import { UserAvatar } from "@/components/user/UserAvatar";
import { useAuth } from "@/lib/hooks/useAuth";
import { cn } from "@/lib/utils";

const navItems = [
  { href: "/dashboard", label: "Resumen", icon: LayoutDashboard },
  { href: "/dashboard/vehiculos", label: "Mis vehículos", icon: Car },
  { href: "/dashboard/favoritos", label: "Favoritos", icon: Heart },
  { href: "/dashboard/mensajes", label: "Mensajes", icon: MessageCircle },
  { href: "/dashboard/perfil", label: "Mi perfil", icon: User },
  { href: "/dashboard/configuracion", label: "Configuración", icon: Settings },
];

export function DashboardSidebar() {
  const pathname = usePathname();
  const { user } = useAuth();

  return (
    <div className="bg-white rounded-xl border p-4 lg:sticky lg:top-24">
      {/* User info */}
      <div className="flex items-center gap-3 p-3 mb-4">
        <UserAvatar src={user?.image} name={user?.name} size="md" />
        <div className="min-w-0">
          <p className="font-medium text-gray-900 truncate">{user?.name}</p>
          <p className="text-sm text-gray-500 truncate">{user?.email}</p>
        </div>
      </div>

      {/* Navigation */}
      <nav className="space-y-1">
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors",
                isActive
                  ? "bg-primary-50 text-primary-700"
                  : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
              )}
            >
              <item.icon size={18} />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
```

---

## 🔧 PASO 3: Página Principal del Dashboard

```typescript
// filepath: src/app/(main)/dashboard/page.tsx
import { Metadata } from "next";
import { auth } from "@/lib/auth";
import { DashboardStats } from "@/components/dashboard/DashboardStats";
import { RecentActivity } from "@/components/dashboard/RecentActivity";
import { QuickActions } from "@/components/dashboard/QuickActions";
import { MyVehiclesPreview } from "@/components/dashboard/MyVehiclesPreview";

export const metadata: Metadata = {
  title: "Dashboard | OKLA",
};

export default async function DashboardPage() {
  const session = await auth();

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Hola, {session?.user?.name?.split(" ")[0]}
        </h1>
        <p className="text-gray-600">Aquí está el resumen de tu actividad</p>
      </div>

      {/* Stats */}
      <DashboardStats />

      {/* Quick Actions */}
      <QuickActions />

      {/* My Vehicles Preview */}
      <MyVehiclesPreview />

      {/* Recent Activity */}
      <RecentActivity />
    </div>
  );
}
```

---

## 🔧 PASO 4: DashboardStats

```typescript
// filepath: src/components/dashboard/DashboardStats.tsx
"use client";

import { Car, Eye, Heart, MessageCircle } from "lucide-react";
import { useDashboardStats } from "@/lib/hooks/useDashboardStats";

export function DashboardStats() {
  const { data: stats, isLoading } = useDashboardStats();

  const items = [
    {
      label: "Mis vehículos",
      value: stats?.vehicleCount ?? 0,
      icon: Car,
      color: "bg-blue-50 text-blue-600",
    },
    {
      label: "Vistas totales",
      value: stats?.totalViews ?? 0,
      icon: Eye,
      color: "bg-green-50 text-green-600",
    },
    {
      label: "Favoritos",
      value: stats?.favoriteCount ?? 0,
      icon: Heart,
      color: "bg-pink-50 text-pink-600",
    },
    {
      label: "Mensajes",
      value: stats?.messageCount ?? 0,
      icon: MessageCircle,
      color: "bg-purple-50 text-purple-600",
    },
  ];

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl border p-4 animate-pulse">
            <div className="w-10 h-10 bg-gray-200 rounded-lg" />
            <div className="h-6 w-16 bg-gray-200 rounded mt-3" />
            <div className="h-4 w-24 bg-gray-200 rounded mt-1" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {items.map((item) => (
        <div key={item.label} className="bg-white rounded-xl border p-4">
          <div className={`inline-flex p-2 rounded-lg ${item.color}`}>
            <item.icon size={20} />
          </div>
          <p className="text-2xl font-bold text-gray-900 mt-3">
            {item.value.toLocaleString()}
          </p>
          <p className="text-sm text-gray-500">{item.label}</p>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 5: QuickActions

```typescript
// filepath: src/components/dashboard/QuickActions.tsx
import Link from "next/link";
import { Plus, Search, Heart, Settings } from "lucide-react";

const actions = [
  {
    href: "/publicar",
    label: "Publicar vehículo",
    icon: Plus,
    color: "bg-primary-600 text-white hover:bg-primary-700",
  },
  {
    href: "/vehiculos",
    label: "Buscar vehículos",
    icon: Search,
    color: "bg-gray-100 text-gray-700 hover:bg-gray-200",
  },
  {
    href: "/dashboard/favoritos",
    label: "Ver favoritos",
    icon: Heart,
    color: "bg-gray-100 text-gray-700 hover:bg-gray-200",
  },
  {
    href: "/dashboard/configuracion",
    label: "Configuración",
    icon: Settings,
    color: "bg-gray-100 text-gray-700 hover:bg-gray-200",
  },
];

export function QuickActions() {
  return (
    <div className="bg-white rounded-xl border p-6">
      <h2 className="font-semibold text-gray-900 mb-4">Acciones rápidas</h2>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {actions.map((action) => (
          <Link
            key={action.href}
            href={action.href}
            className={`flex items-center gap-2 px-4 py-3 rounded-lg font-medium transition-colors ${action.color}`}
          >
            <action.icon size={18} />
            <span className="text-sm">{action.label}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: MyVehiclesPreview

```typescript
// filepath: src/components/dashboard/MyVehiclesPreview.tsx
"use client";

import Link from "next/link";
import { Plus, ArrowRight } from "lucide-react";
import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { useMyVehicles } from "@/lib/hooks/useMyVehicles";
import { Button } from "@/components/ui/Button";

export function MyVehiclesPreview() {
  const { data, isLoading } = useMyVehicles({ limit: 3 });
  const vehicles = data?.items ?? [];

  return (
    <div className="bg-white rounded-xl border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-semibold text-gray-900">Mis vehículos</h2>
        <Link
          href="/dashboard/vehiculos"
          className="text-sm text-primary-600 hover:text-primary-700 flex items-center gap-1"
        >
          Ver todos
          <ArrowRight size={14} />
        </Link>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="aspect-[4/3] bg-gray-100 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : vehicles.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {vehicles.map((vehicle) => (
            <VehicleCard key={vehicle.id} vehicle={vehicle} variant="compact" />
          ))}
        </div>
      ) : (
        <div className="text-center py-8">
          <p className="text-gray-500 mb-4">No tienes vehículos publicados</p>
          <Button asChild>
            <Link href="/publicar">
              <Plus size={16} className="mr-2" />
              Publicar vehículo
            </Link>
          </Button>
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 7: RecentActivity

```typescript
// filepath: src/components/dashboard/RecentActivity.tsx
"use client";

import { Eye, Heart, MessageCircle, Check } from "lucide-react";
import { useRecentActivity } from "@/lib/hooks/useRecentActivity";
import { formatRelativeDate } from "@/lib/utils";

const icons = {
  view: Eye,
  favorite: Heart,
  message: MessageCircle,
  sold: Check,
};

const colors = {
  view: "bg-blue-50 text-blue-600",
  favorite: "bg-pink-50 text-pink-600",
  message: "bg-purple-50 text-purple-600",
  sold: "bg-green-50 text-green-600",
};

export function RecentActivity() {
  const { data: activities, isLoading } = useRecentActivity({ limit: 5 });

  if (isLoading) {
    return (
      <div className="bg-white rounded-xl border p-6">
        <div className="h-5 w-32 bg-gray-200 rounded animate-pulse mb-4" />
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="flex gap-3">
              <div className="w-10 h-10 bg-gray-200 rounded-lg animate-pulse" />
              <div className="flex-1 space-y-2">
                <div className="h-4 w-3/4 bg-gray-200 rounded animate-pulse" />
                <div className="h-3 w-1/4 bg-gray-200 rounded animate-pulse" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border p-6">
      <h2 className="font-semibold text-gray-900 mb-4">Actividad reciente</h2>

      {activities && activities.length > 0 ? (
        <div className="space-y-4">
          {activities.map((activity) => {
            const Icon = icons[activity.type as keyof typeof icons] || Eye;
            const colorClass = colors[activity.type as keyof typeof colors] || colors.view;

            return (
              <div key={activity.id} className="flex items-start gap-3">
                <div className={`p-2 rounded-lg ${colorClass}`}>
                  <Icon size={16} />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-gray-900">{activity.message}</p>
                  <p className="text-xs text-gray-500 mt-1">
                    {formatRelativeDate(activity.createdAt)}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <p className="text-gray-500 text-center py-4">Sin actividad reciente</p>
      )}
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar en http://localhost:3000/dashboard:
# - Requiere autenticación
# - Sidebar navigation funciona
# - Stats se cargan
# - Quick actions funcionan
# - Preview de vehículos
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/seller-dashboard.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsSeller } from "../helpers/auth";

test.describe("Seller Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsSeller(page);
  });

  test("debe mostrar dashboard del vendedor", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(
      page.getByRole("heading", { name: /mi dashboard/i }),
    ).toBeVisible();
    await expect(page.getByTestId("seller-stats")).toBeVisible();
  });

  test("debe mostrar estadísticas de vehículos", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByTestId("stat-active-listings")).toBeVisible();
    await expect(page.getByTestId("stat-total-views")).toBeVisible();
    await expect(page.getByTestId("stat-inquiries")).toBeVisible();
  });

  test("debe navegar con sidebar", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: /mis vehículos/i }).click();
    await expect(page).toHaveURL(/\/dashboard\/vehiculos/);
  });

  test("debe acceder a acciones rápidas", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("button", { name: /publicar nuevo/i }).click();
    await expect(page).toHaveURL("/publicar");
  });

  test("debe mostrar preview de vehículos activos", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByTestId("active-vehicles-preview")).toBeVisible();
  });

  test("debe mostrar consultas pendientes", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByTestId("pending-inquiries")).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/05-API-INTEGRATION/03-formularios.md`
