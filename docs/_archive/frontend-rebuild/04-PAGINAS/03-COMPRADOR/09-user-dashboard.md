---
title: "69 - User Dashboard & Profile Pages"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 📋 69 - User Dashboard & Profile Pages

**Objetivo:** Dashboard del usuario comprador con tabs de favoritos, listings, búsquedas guardadas, configuración y página de perfil.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** AuthStore, KYCService, FavoritesService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [UserDashboardPage](#-userdashboardpage)
3. [ProfilePage](#-profilepage)
4. [Tabs Components](#-tabs-components)
5. [Validación](#-validación)

---

## 🏗️ ARQUITECTURA

```
pages/user/
├── UserDashboardPage.tsx       # Dashboard principal con tabs (178 líneas)
├── ProfilePage.tsx             # Edición de perfil (250 líneas)
├── SecuritySettingsPage.tsx    # Configuración de seguridad (2084 líneas)
├── PrivacyCenterPage.tsx       # Centro de privacidad ARCO (458 líneas)
├── MessagesPage.tsx            # Inbox de mensajes (300 líneas)
├── WishlistPage.tsx            # Lista de deseos
├── MyDataPage.tsx              # Mis datos (ARCO)
├── DataDownloadPage.tsx        # Descarga de datos
└── DeleteAccountPage.tsx       # Eliminar cuenta

components/organisms/
├── FavoritesTab.tsx            # Tab de favoritos
├── MyListingsTab.tsx           # Tab de mis publicaciones
├── SavedSearchesTab.tsx        # Tab de búsquedas guardadas
└── SettingsTab.tsx             # Tab de configuración
```

---

## 📊 TIPOS

```typescript
// src/types/user.ts

export type TabId = "favorites" | "listings" | "searches" | "settings";

export interface Tab {
  id: TabId;
  labelKey: string;
  icon: React.ReactNode;
}

export interface UserProfile {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  avatar?: string;
  createdAt: string;
  kycStatus?: KYCStatus;
  dealerId?: string;
}

// KYC Status from kycService
export enum KYCStatus {
  Pending = 1,
  InProgress = 2,
  DocsRequired = 3,
  UnderReview = 4,
  Approved = 5,
  Rejected = 6,
  Expired = 7,
  Suspended = 8,
}
```

---

## 📊 USERDASHBOARDPAGE

**Ruta:** `/dashboard` o `/user/dashboard`

```typescript
// src/pages/user/UserDashboardPage.tsx
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import MainLayout from '@/layouts/MainLayout';
import { FiHeart, FiList, FiSearch, FiSettings, FiShield, FiX, FiArrowRight } from 'react-icons/fi';
import FavoritesTab from '@/components/organisms/FavoritesTab';
import MyListingsTab from '@/components/organisms/MyListingsTab';
import SavedSearchesTab from '@/components/organisms/SavedSearchesTab';
import SettingsTab from '@/components/organisms/SettingsTab';
import { kycService, type KYCProfile, KYCStatus } from '@/services/kycService';
import { useAuthStore } from '@/store/authStore';

type TabId = 'favorites' | 'listings' | 'searches' | 'settings';

interface Tab {
  id: TabId;
  labelKey: string;
  icon: React.ReactNode;
}

const tabs: Tab[] = [
  { id: 'favorites', labelKey: 'user:dashboard.tabs.favorites', icon: <FiHeart size={20} /> },
  { id: 'listings', labelKey: 'user:dashboard.tabs.listings', icon: <FiList size={20} /> },
  { id: 'searches', labelKey: 'user:dashboard.tabs.searches', icon: <FiSearch size={20} /> },
  { id: 'settings', labelKey: 'user:dashboard.tabs.settings', icon: <FiSettings size={20} /> },
];

export default function UserDashboardPage() {
  const { t } = useTranslation(['common', 'user']);
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<TabId>('favorites');

  // Get user from auth store
  const user = useAuthStore((state) => state.user);

  // KYC verification state
  const [kycProfile, setKycProfile] = useState<KYCProfile | null>(null);
  const [showKycBanner, setShowKycBanner] = useState(true);
  const [kycLoading, setKycLoading] = useState(true);

  // Fetch KYC status on mount
  useEffect(() => {
    const fetchKycStatus = async () => {
      try {
        if (user?.id) {
          const profile = await kycService.getProfileByUserId(user.id);
          setKycProfile(profile);
        }
      } catch {
        setKycProfile(null);
      } finally {
        setKycLoading(false);
      }
    };
    fetchKycStatus();
  }, [user]);

  // Helper to check if KYC is approved
  const isKycApproved = (profile: KYCProfile | null): boolean => {
    if (!profile) return false;
    return (
      profile.status === KYCStatus.Approved ||
      profile.status === 5 ||
      profile.status === 'Approved'
    );
  };

  const needsKycVerification = !kycLoading && !isKycApproved(kycProfile);

  const renderTabContent = () => {
    switch (activeTab) {
      case 'favorites':
        return <FavoritesTab />;
      case 'listings':
        return <MyListingsTab />;
      case 'searches':
        return <SavedSearchesTab />;
      case 'settings':
        return <SettingsTab />;
      default:
        return null;
    }
  };

  return (
    <MainLayout>
      <div className="bg-gray-50 min-h-screen py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          {/* KYC Verification Banner */}
          {needsKycVerification && showKycBanner && (
            <div className="mb-6 bg-gradient-to-r from-blue-600 to-indigo-600 rounded-xl shadow-lg overflow-hidden">
              <div className="relative p-6">
                <button
                  onClick={() => setShowKycBanner(false)}
                  className="absolute top-4 right-4 text-white/70 hover:text-white"
                >
                  <FiX size={20} />
                </button>

                <div className="flex items-center gap-4">
                  <div className="p-3 bg-white/10 rounded-lg">
                    <FiShield size={32} className="text-white" />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-lg font-semibold text-white mb-1">
                      Verifica tu identidad
                    </h3>
                    <p className="text-white/80 text-sm">
                      Completa la verificación KYC para desbloquear todas las funcionalidades
                    </p>
                  </div>
                  <button
                    onClick={() => navigate('/kyc/verification')}
                    className="flex items-center gap-2 bg-white text-blue-600 px-4 py-2 rounded-lg font-medium hover:bg-blue-50"
                  >
                    Verificar ahora
                    <FiArrowRight size={16} />
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">
              {t('user:dashboard.title', 'Mi Dashboard')}
            </h1>
            <p className="text-gray-600 mt-2">
              {t('user:dashboard.subtitle', 'Gestiona tu cuenta, favoritos y publicaciones')}
            </p>
          </div>

          {/* Tabs Navigation */}
          <div className="bg-white rounded-xl shadow-sm mb-6">
            <div className="border-b border-gray-200">
              <nav className="flex -mb-px overflow-x-auto">
                {tabs.map((tab) => (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    className={`flex items-center gap-2 px-6 py-4 text-sm font-medium whitespace-nowrap border-b-2 transition-colors ${
                      activeTab === tab.id
                        ? 'border-blue-500 text-blue-600'
                        : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                    }`}
                  >
                    {tab.icon}
                    <span>{t(tab.labelKey)}</span>
                  </button>
                ))}
              </nav>
            </div>

            {/* Tab Content */}
            <div className="p-6">{renderTabContent()}</div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 👤 PROFILEPAGE

**Ruta:** `/profile` o `/user/profile`

```typescript
// src/pages/user/ProfilePage.tsx
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/hooks/useAuth';
import Button from '@/components/atoms/Button';
import Input from '@/components/atoms/Input';
import { FiUser, FiMail, FiSave, FiAlertCircle, FiCheckCircle } from 'react-icons/fi';

// Validation schema
const profileSchema = z.object({
  username: z
    .string()
    .min(3, 'Username must be at least 3 characters')
    .max(50, 'Username must be less than 50 characters')
    .regex(/^[a-zA-Z0-9_]+$/, 'Username can only contain letters, numbers, and underscores'),
  email: z.string().email('Please enter a valid email address'),
  firstName: z.string().max(100).optional(),
  lastName: z.string().max(100).optional(),
  phone: z
    .string()
    .max(20)
    .regex(/^[\d\s\-\+\(\)]*$/, 'Invalid phone format')
    .optional()
    .or(z.literal('')),
});

type ProfileFormData = z.infer<typeof profileSchema>;

export default function ProfilePage() {
  const { t } = useTranslation('user');
  const { user, updateUser } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      username: user?.name || '',
      email: user?.email || '',
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      phone: user?.phone || '',
    },
  });

  const onSubmit = async (data: ProfileFormData) => {
    try {
      setSuccessMessage(null);
      setErrorMessage(null);

      // API call to update profile
      await new Promise((resolve) => setTimeout(resolve, 1000));

      updateUser({ ...user!, ...data });
      setSuccessMessage(t('profile.updated'));
      setIsEditing(false);

      setTimeout(() => setSuccessMessage(null), 3000);
    } catch (error: unknown) {
      if (error instanceof Error) {
        setErrorMessage(error.message || t('profile.updateFailed'));
      } else {
        setErrorMessage(t('profile.unexpectedError'));
      }
    }
  };

  const handleCancel = () => {
    reset();
    setIsEditing(false);
    setErrorMessage(null);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold font-heading text-gray-900">Mi Perfil</h1>
        </div>

        {/* Success Message */}
        {successMessage && (
          <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg flex items-center gap-3">
            <FiCheckCircle className="text-green-600" size={20} />
            <span className="text-green-800">{successMessage}</span>
          </div>
        )}

        {/* Error Message */}
        {errorMessage && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-center gap-3">
            <FiAlertCircle className="text-red-600" size={20} />
            <span className="text-red-800">{errorMessage}</span>
          </div>
        )}

        {/* Profile Card */}
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          {/* Avatar Section */}
          <div className="bg-gradient-to-r from-blue-600 to-indigo-600 p-8">
            <div className="flex items-center gap-6">
              <div className="w-24 h-24 bg-white/20 rounded-full flex items-center justify-center">
                {user?.avatar ? (
                  <img
                    src={user.avatar}
                    alt={user.name}
                    className="w-full h-full rounded-full object-cover"
                  />
                ) : (
                  <FiUser size={40} className="text-white" />
                )}
              </div>
              <div className="text-white">
                <h2 className="text-2xl font-bold">{user?.name || 'Usuario'}</h2>
                <p className="text-white/80">{user?.email}</p>
              </div>
            </div>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit(onSubmit)} className="p-8">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Username */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Nombre de usuario
                </label>
                <Input
                  {...register('username')}
                  disabled={!isEditing}
                  error={errors.username?.message}
                  leftIcon={<FiUser className="text-gray-400" />}
                />
              </div>

              {/* Email */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Correo electrónico
                </label>
                <Input
                  {...register('email')}
                  type="email"
                  disabled={!isEditing}
                  error={errors.email?.message}
                  leftIcon={<FiMail className="text-gray-400" />}
                />
              </div>

              {/* First Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Nombre
                </label>
                <Input
                  {...register('firstName')}
                  disabled={!isEditing}
                  error={errors.firstName?.message}
                />
              </div>

              {/* Last Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Apellido
                </label>
                <Input
                  {...register('lastName')}
                  disabled={!isEditing}
                  error={errors.lastName?.message}
                />
              </div>

              {/* Phone */}
              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Teléfono
                </label>
                <Input
                  {...register('phone')}
                  type="tel"
                  disabled={!isEditing}
                  error={errors.phone?.message}
                  placeholder="+1 (809) 555-0123"
                />
              </div>
            </div>

            {/* Actions */}
            <div className="mt-8 flex justify-end gap-4">
              {isEditing ? (
                <>
                  <Button type="button" variant="outline" onClick={handleCancel}>
                    Cancelar
                  </Button>
                  <Button
                    type="submit"
                    isLoading={isSubmitting}
                    leftIcon={<FiSave />}
                  >
                    Guardar cambios
                  </Button>
                </>
              ) : (
                <Button type="button" onClick={() => setIsEditing(true)}>
                  Editar perfil
                </Button>
              )}
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
```

---

## 🧩 TABS COMPONENTS

### FavoritesTab

```typescript
// src/components/organisms/FavoritesTab.tsx
import { useQuery } from '@tanstack/react-query';
import { getFavorites } from '@/services/favoritesService';
import VehicleCard from '@/components/molecules/VehicleCard';
import EmptyState from '@/components/organisms/EmptyState';
import { FiHeart } from 'react-icons/fi';

export default function FavoritesTab() {
  const { data: favorites, isLoading } = useQuery({
    queryKey: ['favorites'],
    queryFn: getFavorites,
  });

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="h-64 bg-gray-100 rounded-lg animate-pulse" />
        ))}
      </div>
    );
  }

  if (!favorites || favorites.length === 0) {
    return (
      <EmptyState
        icon={<FiHeart size={48} className="text-gray-300" />}
        title="No tienes favoritos"
        message="Explora vehículos y agrégalos a tus favoritos"
        actionLabel="Buscar vehículos"
        onAction={() => (window.location.href = '/browse')}
      />
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {favorites.map((vehicle) => (
        <VehicleCard key={vehicle.id} vehicle={vehicle} showFavoriteButton />
      ))}
    </div>
  );
}
```

### MyListingsTab

```typescript
// src/components/organisms/MyListingsTab.tsx
import { useQuery } from '@tanstack/react-query';
import { getMyListings } from '@/services/vehicleService';
import ListingCard from '@/components/molecules/ListingCard';
import EmptyState from '@/components/organisms/EmptyState';
import Button from '@/components/atoms/Button';
import { FiPlus, FiList } from 'react-icons/fi';
import { Link } from 'react-router-dom';

export default function MyListingsTab() {
  const { data: listings, isLoading } = useQuery({
    queryKey: ['my-listings'],
    queryFn: getMyListings,
  });

  if (isLoading) {
    return <div className="animate-pulse h-48 bg-gray-100 rounded-lg" />;
  }

  if (!listings || listings.length === 0) {
    return (
      <EmptyState
        icon={<FiList size={48} className="text-gray-300" />}
        title="No tienes publicaciones"
        message="¿Tienes un vehículo para vender? Crea tu primera publicación"
        actionLabel="Publicar vehículo"
        onAction={() => (window.location.href = '/sell')}
      />
    );
  }

  return (
    <div>
      <div className="flex justify-end mb-6">
        <Link to="/sell">
          <Button leftIcon={<FiPlus />}>Nueva publicación</Button>
        </Link>
      </div>

      <div className="space-y-4">
        {listings.map((listing) => (
          <ListingCard key={listing.id} listing={listing} showActions />
        ))}
      </div>
    </div>
  );
}
```

### SavedSearchesTab

```typescript
// src/components/organisms/SavedSearchesTab.tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getSavedSearches, deleteSavedSearch } from '@/services/searchService';
import { FiSearch, FiTrash2, FiBell } from 'react-icons/fi';
import EmptyState from '@/components/organisms/EmptyState';

export default function SavedSearchesTab() {
  const queryClient = useQueryClient();

  const { data: searches, isLoading } = useQuery({
    queryKey: ['saved-searches'],
    queryFn: getSavedSearches,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteSavedSearch,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['saved-searches'] });
    },
  });

  if (isLoading) {
    return <div className="animate-pulse h-48 bg-gray-100 rounded-lg" />;
  }

  if (!searches || searches.length === 0) {
    return (
      <EmptyState
        icon={<FiSearch size={48} className="text-gray-300" />}
        title="No tienes búsquedas guardadas"
        message="Guarda tus búsquedas frecuentes para acceder rápidamente"
        actionLabel="Buscar vehículos"
        onAction={() => (window.location.href = '/browse')}
      />
    );
  }

  return (
    <div className="space-y-4">
      {searches.map((search) => (
        <div
          key={search.id}
          className="border rounded-lg p-4 flex items-center justify-between hover:bg-gray-50"
        >
          <div className="flex items-center gap-4">
            <div className="p-2 bg-blue-100 rounded-lg">
              <FiSearch className="text-blue-600" size={20} />
            </div>
            <div>
              <h4 className="font-medium text-gray-900">{search.name}</h4>
              <p className="text-sm text-gray-500">{search.filtersDescription}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {search.notificationsEnabled && (
              <span className="flex items-center gap-1 text-xs text-green-600 bg-green-100 px-2 py-1 rounded">
                <FiBell size={12} /> Alertas activas
              </span>
            )}
            <button
              onClick={() => deleteMutation.mutate(search.id)}
              className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
            >
              <FiTrash2 size={18} />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

### UserDashboardPage

- [ ] 4 tabs funcionan (Favorites, Listings, Searches, Settings)
- [ ] KYC banner muestra si no verificado
- [ ] Banner dismissible con X
- [ ] Botón "Verificar ahora" navega a /kyc/verification
- [ ] Contenido de cada tab se renderiza
- [ ] Responsive design

### ProfilePage

- [ ] Form muestra datos del usuario
- [ ] Modo edición toggle funciona
- [ ] Validación con Zod funciona
- [ ] Mensajes de éxito/error visibles
- [ ] Avatar o placeholder visible
- [ ] Botón guardar muestra loading

### FavoritesTab

- [ ] Lista de favoritos del usuario
- [ ] Empty state cuando no hay favoritos
- [ ] Loading skeleton
- [ ] VehicleCard con botón favorito

### MyListingsTab

- [ ] Lista de publicaciones del usuario
- [ ] Botón "Nueva publicación"
- [ ] Empty state cuando no hay listings
- [ ] Acciones por listing (editar, pausar, eliminar)

### SavedSearchesTab

- [ ] Lista de búsquedas guardadas
- [ ] Badge de alertas activas
- [ ] Botón eliminar búsqueda
- [ ] Empty state

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/user-dashboard.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("User Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar dashboard con resumen", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(
      page.getByRole("heading", { name: /mi dashboard/i }),
    ).toBeVisible();
    await expect(page.getByTestId("dashboard-stats")).toBeVisible();
  });

  test("debe mostrar actividad reciente", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByTestId("recent-activity")).toBeVisible();
  });

  test("debe mostrar accesos rápidos", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByRole("link", { name: /favoritos/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /mensajes/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /alertas/i })).toBeVisible();
  });

  test("debe navegar a secciones desde dashboard", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: /ver favoritos/i }).click();
    await expect(page).toHaveURL("/favoritos");
  });

  test("debe mostrar vehículos vistos recientemente", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByTestId("recently-viewed")).toBeVisible();
  });
});
```

---

## 🔗 RUTAS

```typescript
// src/App.tsx
<Route path="/dashboard" element={<ProtectedRoute><UserDashboardPage /></ProtectedRoute>} />
<Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
<Route path="/user/dashboard" element={<Navigate to="/dashboard" replace />} />
```

---

_Última actualización: Enero 2026_
