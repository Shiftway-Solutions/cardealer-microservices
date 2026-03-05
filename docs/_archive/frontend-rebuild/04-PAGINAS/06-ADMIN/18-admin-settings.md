---
title: "63 - Admin Settings & Categories"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["AdminService"]
status: complete
last_updated: "2026-01-30"
---

# 📋 63 - Admin Settings & Categories

**Objetivo:** Configuración del sistema y gestión de categorías para admins.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** AdminService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [AdminSettingsPage](#-adminsettingspage)
3. [CategoriesManagementPage](#-categoriesmanagementpage)
4. [Tipos y Enums](#-tipos-y-enums)
5. [Servicios API](#-servicios-api)

---

## 🏗️ ARQUITECTURA

```
pages/admin/
├── AdminSettingsPage.tsx           # Configuración del sistema (322 líneas)
├── CategoriesManagementPage.tsx    # Gestión de categorías (920 líneas)
└── components/
    ├── SettingToggle.tsx           # Toggle de configuración
    ├── CategoryCard.tsx            # Card de categoría
    └── CategoryModal.tsx           # Modal CRUD categoría
```

---

## 📊 TIPOS Y ENUMS

```typescript
// src/types/admin.ts

// =====================
// SYSTEM SETTINGS
// =====================
export interface SystemSettings {
  // General
  siteName: string;
  siteDescription: string;
  contactEmail: string;
  supportPhone: string;

  // Operation
  maintenanceMode: boolean;
  maintenanceMessage?: string;
  allowRegistrations: boolean;
  requireEmailVerification: boolean;
  requirePhoneVerification: boolean;

  // Listings
  requireApproval: boolean;
  maxImagesPerListing: number;
  maxListingsPerUser: number;
  listingExpirationDays: number;

  // Security
  maxLoginAttempts: number;
  sessionTimeoutMinutes: number;
  enableTwoFactor: boolean;

  // Notifications
  emailNotificationsEnabled: boolean;
  smsNotificationsEnabled: boolean;
  pushNotificationsEnabled: boolean;
}

// =====================
// CATEGORIES
// =====================
export type VerticalType = "vehicles" | "properties";

export interface Category {
  id: string;
  name: string;
  slug: string;
  icon: string;
  color: string;
  description?: string;
  vertical: VerticalType;
  isActive: boolean;
  sortOrder: number;
  subcategories: Subcategory[];
  listingsCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface Subcategory {
  id: string;
  categoryId: string;
  name: string;
  slug: string;
  icon?: string;
  description?: string;
  isActive: boolean;
  sortOrder: number;
  listingsCount: number;
}

export interface CategoryFormData {
  name: string;
  slug: string;
  icon: string;
  color: string;
  description: string;
  vertical: VerticalType;
  isActive: boolean;
  sortOrder: number;
}

export interface SubcategoryFormData {
  name: string;
  slug: string;
  icon: string;
  description: string;
  isActive: boolean;
  sortOrder: number;
}
```

---

## ⚙️ ADMINSETTINGSPAGE

**Ruta:** `/admin/settings`

```typescript
// src/pages/admin/AdminSettingsPage.tsx
import { useState, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminService } from '@/services/adminService';
import type { SystemSettings } from '@/types/admin';
import {
  FiSettings,
  FiSave,
  FiRefreshCw,
  FiAlertTriangle,
  FiShield,
  FiBell,
  FiImage,
  FiUsers,
  FiLock,
  FiMail,
  FiSmartphone,
  FiToggleLeft,
  FiToggleRight,
} from 'react-icons/fi';

export default function AdminSettingsPage() {
  const queryClient = useQueryClient();
  const [hasChanges, setHasChanges] = useState(false);

  // Query: Get settings
  const { data: settings, isLoading } = useQuery({
    queryKey: ['admin', 'settings'],
    queryFn: () => adminService.getSettings(),
  });

  // Local state for editing
  const [localSettings, setLocalSettings] = useState<SystemSettings | null>(null);

  useEffect(() => {
    if (settings) {
      setLocalSettings(settings);
    }
  }, [settings]);

  // Mutation: Save settings
  const saveSettings = useMutation({
    mutationFn: (data: SystemSettings) => adminService.updateSettings(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'settings'] });
      setHasChanges(false);
      alert('Configuración guardada exitosamente');
    },
    onError: () => {
      alert('Error al guardar la configuración');
    },
  });

  const handleToggle = (key: keyof SystemSettings) => {
    if (!localSettings) return;
    setLocalSettings({
      ...localSettings,
      [key]: !localSettings[key],
    });
    setHasChanges(true);
  };

  const handleNumberChange = (key: keyof SystemSettings, value: number) => {
    if (!localSettings) return;
    setLocalSettings({
      ...localSettings,
      [key]: value,
    });
    setHasChanges(true);
  };

  const handleTextChange = (key: keyof SystemSettings, value: string) => {
    if (!localSettings) return;
    setLocalSettings({
      ...localSettings,
      [key]: value,
    });
    setHasChanges(true);
  };

  const handleSave = () => {
    if (!localSettings) return;
    saveSettings.mutate(localSettings);
  };

  const handleReset = () => {
    if (settings) {
      setLocalSettings(settings);
      setHasChanges(false);
    }
  };

  if (isLoading) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 rounded w-1/3" />
          <div className="h-64 bg-gray-200 rounded" />
        </div>
      </div>
    );
  }

  if (!localSettings) return null;

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Configuración del Sistema</h1>
          <p className="text-gray-600">Ajustes generales de la plataforma</p>
        </div>
        <div className="flex gap-3">
          {hasChanges && (
            <button
              onClick={handleReset}
              className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
            >
              <FiRefreshCw />
              Descartar
            </button>
          )}
          <button
            onClick={handleSave}
            disabled={!hasChanges || saveSettings.isPending}
            className="flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary-dark disabled:opacity-50"
          >
            <FiSave />
            {saveSettings.isPending ? 'Guardando...' : 'Guardar Cambios'}
          </button>
        </div>
      </div>

      {/* Settings Sections */}
      <div className="space-y-6">
        {/* Maintenance Mode */}
        <SettingsCard
          title="Modo Mantenimiento"
          description="Activar para bloquear el acceso a la plataforma"
          icon={FiAlertTriangle}
          iconColor="text-yellow-600"
        >
          <SettingToggle
            label="Modo Mantenimiento"
            description="Los usuarios verán una página de mantenimiento"
            enabled={localSettings.maintenanceMode}
            onChange={() => handleToggle('maintenanceMode')}
            danger
          />
          {localSettings.maintenanceMode && (
            <div className="mt-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Mensaje de Mantenimiento
              </label>
              <textarea
                value={localSettings.maintenanceMessage || ''}
                onChange={(e) => handleTextChange('maintenanceMessage', e.target.value)}
                rows={3}
                placeholder="Estamos realizando mejoras en la plataforma..."
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
              />
            </div>
          )}
        </SettingsCard>

        {/* Registration */}
        <SettingsCard
          title="Registro de Usuarios"
          description="Controlar cómo los usuarios pueden registrarse"
          icon={FiUsers}
          iconColor="text-blue-600"
        >
          <div className="space-y-4">
            <SettingToggle
              label="Permitir Nuevos Registros"
              description="Desactivar para cerrar temporalmente los registros"
              enabled={localSettings.allowRegistrations}
              onChange={() => handleToggle('allowRegistrations')}
            />
            <SettingToggle
              label="Verificación de Email Requerida"
              description="Los usuarios deben verificar su email antes de acceder"
              enabled={localSettings.requireEmailVerification}
              onChange={() => handleToggle('requireEmailVerification')}
            />
            <SettingToggle
              label="Verificación de Teléfono Requerida"
              description="Los usuarios deben verificar su teléfono con SMS"
              enabled={localSettings.requirePhoneVerification}
              onChange={() => handleToggle('requirePhoneVerification')}
            />
          </div>
        </SettingsCard>

        {/* Listings */}
        <SettingsCard
          title="Publicaciones"
          description="Configuración de publicaciones de vehículos"
          icon={FiImage}
          iconColor="text-green-600"
        >
          <div className="space-y-4">
            <SettingToggle
              label="Requiere Aprobación de Admin"
              description="Las publicaciones deben ser aprobadas antes de publicarse"
              enabled={localSettings.requireApproval}
              onChange={() => handleToggle('requireApproval')}
            />

            <SettingNumber
              label="Máximo de Imágenes por Publicación"
              value={localSettings.maxImagesPerListing}
              onChange={(value) => handleNumberChange('maxImagesPerListing', value)}
              min={1}
              max={50}
            />

            <SettingNumber
              label="Máximo de Publicaciones por Usuario"
              value={localSettings.maxListingsPerUser}
              onChange={(value) => handleNumberChange('maxListingsPerUser', value)}
              min={1}
              max={1000}
            />

            <SettingNumber
              label="Días de Expiración de Publicaciones"
              value={localSettings.listingExpirationDays}
              onChange={(value) => handleNumberChange('listingExpirationDays', value)}
              min={7}
              max={365}
            />
          </div>
        </SettingsCard>

        {/* Security */}
        <SettingsCard
          title="Seguridad"
          description="Configuración de seguridad de la plataforma"
          icon={FiShield}
          iconColor="text-red-600"
        >
          <div className="space-y-4">
            <SettingNumber
              label="Intentos Máximos de Login"
              value={localSettings.maxLoginAttempts}
              onChange={(value) => handleNumberChange('maxLoginAttempts', value)}
              min={3}
              max={10}
            />

            <SettingNumber
              label="Timeout de Sesión (minutos)"
              value={localSettings.sessionTimeoutMinutes}
              onChange={(value) => handleNumberChange('sessionTimeoutMinutes', value)}
              min={15}
              max={1440}
            />

            <SettingToggle
              label="Habilitar Autenticación de Dos Factores"
              description="Los usuarios pueden activar 2FA en su cuenta"
              enabled={localSettings.enableTwoFactor}
              onChange={() => handleToggle('enableTwoFactor')}
            />
          </div>
        </SettingsCard>

        {/* Notifications */}
        <SettingsCard
          title="Notificaciones"
          description="Canales de notificación habilitados"
          icon={FiBell}
          iconColor="text-purple-600"
        >
          <div className="space-y-4">
            <SettingToggle
              label="Notificaciones por Email"
              description="Enviar notificaciones por correo electrónico"
              enabled={localSettings.emailNotificationsEnabled}
              onChange={() => handleToggle('emailNotificationsEnabled')}
            />
            <SettingToggle
              label="Notificaciones por SMS"
              description="Enviar notificaciones por mensaje de texto"
              enabled={localSettings.smsNotificationsEnabled}
              onChange={() => handleToggle('smsNotificationsEnabled')}
            />
            <SettingToggle
              label="Notificaciones Push"
              description="Enviar notificaciones push a dispositivos móviles"
              enabled={localSettings.pushNotificationsEnabled}
              onChange={() => handleToggle('pushNotificationsEnabled')}
            />
          </div>
        </SettingsCard>
      </div>
    </div>
  );
}

// SettingsCard Component
function SettingsCard({
  title,
  description,
  icon: Icon,
  iconColor,
  children,
}: {
  title: string;
  description: string;
  icon: React.ElementType;
  iconColor: string;
  children: React.ReactNode;
}) {
  return (
    <div className="bg-white rounded-xl shadow-card p-6">
      <div className="flex items-start gap-4 mb-6">
        <div className={`p-3 rounded-lg bg-gray-50 ${iconColor}`}>
          <Icon className="w-6 h-6" />
        </div>
        <div>
          <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
          <p className="text-sm text-gray-600">{description}</p>
        </div>
      </div>
      {children}
    </div>
  );
}

// SettingToggle Component
function SettingToggle({
  label,
  description,
  enabled,
  onChange,
  danger = false,
}: {
  label: string;
  description: string;
  enabled: boolean;
  onChange: () => void;
  danger?: boolean;
}) {
  return (
    <div className="flex items-center justify-between py-3 border-b border-gray-100 last:border-0">
      <div>
        <p className="font-medium text-gray-900">{label}</p>
        <p className="text-sm text-gray-500">{description}</p>
      </div>
      <button
        onClick={onChange}
        className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
          enabled
            ? danger
              ? 'bg-red-600'
              : 'bg-green-600'
            : 'bg-gray-300'
        }`}
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
            enabled ? 'translate-x-6' : 'translate-x-1'
          }`}
        />
      </button>
    </div>
  );
}

// SettingNumber Component
function SettingNumber({
  label,
  value,
  onChange,
  min,
  max,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
}) {
  return (
    <div className="flex items-center justify-between py-3 border-b border-gray-100 last:border-0">
      <label className="font-medium text-gray-900">{label}</label>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(Math.min(max, Math.max(min, Number(e.target.value))))}
        min={min}
        max={max}
        className="w-24 px-3 py-2 border border-gray-300 rounded-lg text-right focus:ring-2 focus:ring-primary"
      />
    </div>
  );
}
```

---

## 📂 CATEGORIESMANAGEMENTPAGE

**Ruta:** `/admin/categories`

```typescript
// src/pages/admin/CategoriesManagementPage.tsx
import { useState, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminService } from '@/services/adminService';
import type { Category, Subcategory, CategoryFormData, VerticalType } from '@/types/admin';
import {
  FiPlus,
  FiEdit2,
  FiTrash2,
  FiChevronDown,
  FiChevronRight,
  FiSearch,
  FiTruck,
  FiHome,
  FiGrid,
  FiToggleLeft,
  FiToggleRight,
  FiMove,
  FiX,
  FiCheck,
  FiLayers,
  FiTag,
  FiEye,
  FiEyeOff,
} from 'react-icons/fi';

// Icon options for categories
const CATEGORY_ICONS = ['🚗', '🚙', '🛻', '⚡', '🏎️', '🚐', '🏠', '🏢', '🌳', '🏪', '🏛️', '📦', '✨', '🔑', '💎', '🏗️'];

// Color options
const CATEGORY_COLORS = [
  '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444', '#EC4899',
  '#06B6D4', '#84CC16', '#F97316', '#6366F1', '#14B8A6', '#A855F7',
];

export default function CategoriesManagementPage() {
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');
  const [activeVertical, setActiveVertical] = useState<VerticalType | 'all'>('all');
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set());

  // Modal states
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalType, setModalType] = useState<'category' | 'subcategory'>('category');
  const [editingItem, setEditingItem] = useState<Category | Subcategory | null>(null);
  const [parentCategoryId, setParentCategoryId] = useState<string | null>(null);

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState<{
    type: 'category' | 'subcategory';
    id: string;
    name: string;
  } | null>(null);

  // Form state
  const [formData, setFormData] = useState<CategoryFormData>({
    name: '',
    slug: '',
    icon: '🚗',
    color: '#3B82F6',
    description: '',
    vertical: 'vehicles',
    isActive: true,
    sortOrder: 1,
  });

  // Query: Get categories
  const { data: categories = [], isLoading } = useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: () => adminService.getCategories(),
  });

  // Mutations
  const createCategory = useMutation({
    mutationFn: (data: CategoryFormData) => adminService.createCategory(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
      closeModal();
    },
  });

  const updateCategory = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CategoryFormData }) =>
      adminService.updateCategory(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
      closeModal();
    },
  });

  const deleteCategory = useMutation({
    mutationFn: (id: string) => adminService.deleteCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
      setDeleteConfirm(null);
    },
  });

  const toggleCategoryStatus = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      adminService.toggleCategoryStatus(id, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    },
  });

  // Filter categories
  const filteredCategories = useMemo(() => {
    return categories.filter((cat) => {
      const matchesVertical = activeVertical === 'all' || cat.vertical === activeVertical;
      const matchesSearch =
        !searchTerm ||
        cat.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        cat.subcategories.some((sub) =>
          sub.name.toLowerCase().includes(searchTerm.toLowerCase())
        );
      return matchesVertical && matchesSearch;
    });
  }, [categories, activeVertical, searchTerm]);

  // Stats
  const stats = useMemo(() => {
    return {
      totalCategories: categories.length,
      totalSubcategories: categories.reduce((acc, cat) => acc + cat.subcategories.length, 0),
      vehicleCategories: categories.filter((c) => c.vertical === 'vehicles').length,
      propertyCategories: categories.filter((c) => c.vertical === 'properties').length,
      activeCategories: categories.filter((c) => c.isActive).length,
    };
  }, [categories]);

  // Toggle category expansion
  const toggleExpand = (categoryId: string) => {
    setExpandedCategories((prev) => {
      const next = new Set(prev);
      if (next.has(categoryId)) {
        next.delete(categoryId);
      } else {
        next.add(categoryId);
      }
      return next;
    });
  };

  // Open modal
  const openModal = (
    type: 'category' | 'subcategory',
    item?: Category | Subcategory,
    parentId?: string
  ) => {
    setModalType(type);
    setEditingItem(item || null);
    setParentCategoryId(parentId || null);

    if (item) {
      const cat = item as Category;
      setFormData({
        name: cat.name,
        slug: cat.slug,
        icon: cat.icon,
        color: cat.color,
        description: cat.description || '',
        vertical: cat.vertical,
        isActive: cat.isActive,
        sortOrder: cat.sortOrder,
      });
    } else {
      // Reset form for new item
      setFormData({
        name: '',
        slug: '',
        icon: '🚗',
        color: '#3B82F6',
        description: '',
        vertical: activeVertical !== 'all' ? activeVertical : 'vehicles',
        isActive: true,
        sortOrder: categories.length + 1,
      });
    }

    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingItem(null);
    setParentCategoryId(null);
  };

  // Auto-generate slug from name
  const generateSlug = (name: string) => {
    return name
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '') // Remove accents
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)/g, '');
  };

  const handleNameChange = (name: string) => {
    setFormData({
      ...formData,
      name,
      slug: generateSlug(name),
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (editingItem) {
      updateCategory.mutate({ id: editingItem.id, data: formData });
    } else {
      createCategory.mutate(formData);
    }
  };

  const handleDelete = () => {
    if (!deleteConfirm) return;
    deleteCategory.mutate(deleteConfirm.id);
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Gestión de Categorías</h1>
          <p className="text-gray-600">
            Administrar categorías y subcategorías del marketplace
          </p>
        </div>
        <button
          onClick={() => openModal('category')}
          className="flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary-dark"
        >
          <FiPlus />
          Nueva Categoría
        </button>
      </div>

      {/* Stats */}
      <div className="grid md:grid-cols-5 gap-4 mb-6">
        {[
          { label: 'Categorías', value: stats.totalCategories, icon: FiLayers, color: 'blue' },
          { label: 'Subcategorías', value: stats.totalSubcategories, icon: FiTag, color: 'purple' },
          { label: 'Vehículos', value: stats.vehicleCategories, icon: FiTruck, color: 'green' },
          { label: 'Propiedades', value: stats.propertyCategories, icon: FiHome, color: 'orange' },
          { label: 'Activas', value: stats.activeCategories, icon: FiEye, color: 'teal' },
        ].map((stat) => (
          <div key={stat.label} className="bg-white rounded-xl shadow-card p-4">
            <div className="flex items-center gap-3">
              <div className={`p-2 rounded-lg bg-${stat.color}-100`}>
                <stat.icon className={`w-5 h-5 text-${stat.color}-600`} />
              </div>
              <div>
                <p className="text-xs text-gray-500">{stat.label}</p>
                <p className="text-xl font-bold text-gray-900">{stat.value}</p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-card p-4 mb-6">
        <div className="flex flex-col md:flex-row gap-4">
          {/* Search */}
          <div className="flex-1 relative">
            <FiSearch className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar categorías..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
            />
          </div>

          {/* Vertical Filter */}
          <div className="flex gap-2">
            {[
              { value: 'all', label: 'Todos', icon: FiGrid },
              { value: 'vehicles', label: 'Vehículos', icon: FiTruck },
              { value: 'properties', label: 'Propiedades', icon: FiHome },
            ].map((v) => (
              <button
                key={v.value}
                onClick={() => setActiveVertical(v.value as VerticalType | 'all')}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${
                  activeVertical === v.value
                    ? 'bg-primary text-white'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                }`}
              >
                <v.icon />
                {v.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Categories List */}
      {isLoading ? (
        <div className="bg-white rounded-xl shadow-card p-12 text-center">
          <div className="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
          <p className="text-gray-600 mt-4">Cargando categorías...</p>
        </div>
      ) : filteredCategories.length === 0 ? (
        <div className="bg-white rounded-xl shadow-card p-12 text-center">
          <FiLayers className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 mb-2">No hay categorías</h3>
          <p className="text-gray-600 mb-4">Crea la primera categoría para comenzar</p>
          <button
            onClick={() => openModal('category')}
            className="px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary-dark"
          >
            Crear Categoría
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          <AnimatePresence>
            {filteredCategories.map((category) => (
              <motion.div
                key={category.id}
                layout
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -20 }}
                className="bg-white rounded-xl shadow-card overflow-hidden"
              >
                {/* Category Header */}
                <div className="p-4 flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <button
                      onClick={() => toggleExpand(category.id)}
                      className="p-2 hover:bg-gray-100 rounded-lg"
                    >
                      {expandedCategories.has(category.id) ? (
                        <FiChevronDown />
                      ) : (
                        <FiChevronRight />
                      )}
                    </button>

                    <div
                      className="w-10 h-10 rounded-lg flex items-center justify-center text-xl"
                      style={{ backgroundColor: category.color + '20' }}
                    >
                      {category.icon}
                    </div>

                    <div>
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold text-gray-900">{category.name}</h3>
                        <span
                          className={`px-2 py-0.5 text-xs rounded-full ${
                            category.vertical === 'vehicles'
                              ? 'bg-blue-100 text-blue-800'
                              : 'bg-green-100 text-green-800'
                          }`}
                        >
                          {category.vertical === 'vehicles' ? 'Vehículos' : 'Propiedades'}
                        </span>
                        <span
                          className={`px-2 py-0.5 text-xs rounded-full ${
                            category.isActive
                              ? 'bg-green-100 text-green-800'
                              : 'bg-gray-100 text-gray-600'
                          }`}
                        >
                          {category.isActive ? 'Activa' : 'Inactiva'}
                        </span>
                      </div>
                      <p className="text-sm text-gray-500">
                        {category.subcategories.length} subcategorías •{' '}
                        {category.listingsCount} publicaciones
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <button
                      onClick={() =>
                        toggleCategoryStatus.mutate({
                          id: category.id,
                          isActive: !category.isActive,
                        })
                      }
                      className={`p-2 rounded-lg ${
                        category.isActive
                          ? 'text-green-600 hover:bg-green-50'
                          : 'text-gray-400 hover:bg-gray-100'
                      }`}
                      title={category.isActive ? 'Desactivar' : 'Activar'}
                    >
                      {category.isActive ? <FiToggleRight size={20} /> : <FiToggleLeft size={20} />}
                    </button>
                    <button
                      onClick={() => openModal('subcategory', undefined, category.id)}
                      className="p-2 text-blue-600 hover:bg-blue-50 rounded-lg"
                      title="Agregar subcategoría"
                    >
                      <FiPlus />
                    </button>
                    <button
                      onClick={() => openModal('category', category)}
                      className="p-2 text-gray-600 hover:bg-gray-100 rounded-lg"
                      title="Editar"
                    >
                      <FiEdit2 />
                    </button>
                    <button
                      onClick={() =>
                        setDeleteConfirm({
                          type: 'category',
                          id: category.id,
                          name: category.name,
                        })
                      }
                      className="p-2 text-red-600 hover:bg-red-50 rounded-lg"
                      title="Eliminar"
                    >
                      <FiTrash2 />
                    </button>
                  </div>
                </div>

                {/* Subcategories */}
                <AnimatePresence>
                  {expandedCategories.has(category.id) && category.subcategories.length > 0 && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      className="border-t border-gray-100 bg-gray-50"
                    >
                      {category.subcategories.map((sub) => (
                        <div
                          key={sub.id}
                          className="flex items-center justify-between px-6 py-3 pl-16 border-b border-gray-100 last:border-0"
                        >
                          <div className="flex items-center gap-3">
                            <span className="text-lg">{sub.icon || '📁'}</span>
                            <div>
                              <p className="font-medium text-gray-900">{sub.name}</p>
                              <p className="text-xs text-gray-500">
                                {sub.listingsCount} publicaciones
                              </p>
                            </div>
                            <span
                              className={`px-2 py-0.5 text-xs rounded-full ${
                                sub.isActive
                                  ? 'bg-green-100 text-green-800'
                                  : 'bg-gray-100 text-gray-600'
                              }`}
                            >
                              {sub.isActive ? 'Activa' : 'Inactiva'}
                            </span>
                          </div>
                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => openModal('subcategory', sub, category.id)}
                              className="p-1.5 text-gray-600 hover:bg-gray-200 rounded"
                              title="Editar"
                            >
                              <FiEdit2 size={14} />
                            </button>
                            <button
                              onClick={() =>
                                setDeleteConfirm({
                                  type: 'subcategory',
                                  id: sub.id,
                                  name: sub.name,
                                })
                              }
                              className="p-1.5 text-red-600 hover:bg-red-100 rounded"
                              title="Eliminar"
                            >
                              <FiTrash2 size={14} />
                            </button>
                          </div>
                        </div>
                      ))}
                    </motion.div>
                  )}
                </AnimatePresence>
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      )}

      {/* Category/Subcategory Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            className="bg-white rounded-xl shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto"
          >
            <div className="p-6 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">
                {editingItem
                  ? `Editar ${modalType === 'category' ? 'Categoría' : 'Subcategoría'}`
                  : `Nueva ${modalType === 'category' ? 'Categoría' : 'Subcategoría'}`}
              </h3>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Nombre *
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => handleNameChange(e.target.value)}
                  required
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
                />
              </div>

              {/* Slug */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
                <input
                  type="text"
                  value={formData.slug}
                  onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
                />
              </div>

              {/* Icon Picker */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Icono</label>
                <div className="flex flex-wrap gap-2">
                  {CATEGORY_ICONS.map((icon) => (
                    <button
                      key={icon}
                      type="button"
                      onClick={() => setFormData({ ...formData, icon })}
                      className={`w-10 h-10 text-xl rounded-lg border-2 ${
                        formData.icon === icon
                          ? 'border-primary bg-primary/10'
                          : 'border-gray-200 hover:border-gray-300'
                      }`}
                    >
                      {icon}
                    </button>
                  ))}
                </div>
              </div>

              {/* Color Picker (only for categories) */}
              {modalType === 'category' && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Color</label>
                  <div className="flex flex-wrap gap-2">
                    {CATEGORY_COLORS.map((color) => (
                      <button
                        key={color}
                        type="button"
                        onClick={() => setFormData({ ...formData, color })}
                        className={`w-8 h-8 rounded-full border-2 ${
                          formData.color === color ? 'border-gray-900' : 'border-transparent'
                        }`}
                        style={{ backgroundColor: color }}
                      />
                    ))}
                  </div>
                </div>
              )}

              {/* Vertical (only for categories) */}
              {modalType === 'category' && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Vertical</label>
                  <div className="flex gap-4">
                    <label className="flex items-center gap-2">
                      <input
                        type="radio"
                        name="vertical"
                        value="vehicles"
                        checked={formData.vertical === 'vehicles'}
                        onChange={() => setFormData({ ...formData, vertical: 'vehicles' })}
                        className="text-primary focus:ring-primary"
                      />
                      <FiTruck /> Vehículos
                    </label>
                    <label className="flex items-center gap-2">
                      <input
                        type="radio"
                        name="vertical"
                        value="properties"
                        checked={formData.vertical === 'properties'}
                        onChange={() => setFormData({ ...formData, vertical: 'properties' })}
                        className="text-primary focus:ring-primary"
                      />
                      <FiHome /> Propiedades
                    </label>
                  </div>
                </div>
              )}

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Descripción
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  rows={3}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
                />
              </div>

              {/* Active Toggle */}
              <div className="flex items-center justify-between">
                <span className="font-medium text-gray-700">Activa</span>
                <button
                  type="button"
                  onClick={() => setFormData({ ...formData, isActive: !formData.isActive })}
                  className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                    formData.isActive ? 'bg-green-600' : 'bg-gray-300'
                  }`}
                >
                  <span
                    className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                      formData.isActive ? 'translate-x-6' : 'translate-x-1'
                    }`}
                  />
                </button>
              </div>

              {/* Actions */}
              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={createCategory.isPending || updateCategory.isPending}
                  className="flex-1 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary-dark disabled:opacity-50"
                >
                  {createCategory.isPending || updateCategory.isPending
                    ? 'Guardando...'
                    : editingItem
                    ? 'Guardar Cambios'
                    : 'Crear'}
                </button>
              </div>
            </form>
          </motion.div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl shadow-xl max-w-sm w-full p-6">
            <div className="text-center mb-6">
              <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <FiTrash2 className="w-6 h-6 text-red-600" />
              </div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                Eliminar {deleteConfirm.type === 'category' ? 'Categoría' : 'Subcategoría'}
              </h3>
              <p className="text-gray-600">
                ¿Estás seguro de que deseas eliminar{' '}
                <strong>"{deleteConfirm.name}"</strong>? Esta acción no se puede deshacer.
              </p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteCategory.isPending}
                className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {deleteCategory.isPending ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## 🔌 SERVICIOS API

```typescript
// src/services/adminService.ts

export const adminService = {
  // =====================
  // SETTINGS
  // =====================
  getSettings: async (): Promise<SystemSettings> => {
    const response = await api.get("/api/admin/settings");
    return response.data;
  },

  updateSettings: async (data: SystemSettings): Promise<void> => {
    await api.put("/api/admin/settings", data);
  },

  // =====================
  // CATEGORIES
  // =====================
  getCategories: async (): Promise<Category[]> => {
    const response = await api.get("/api/admin/categories");
    return response.data;
  },

  createCategory: async (data: CategoryFormData): Promise<Category> => {
    const response = await api.post("/api/admin/categories", data);
    return response.data;
  },

  updateCategory: async (
    id: string,
    data: CategoryFormData,
  ): Promise<Category> => {
    const response = await api.put(`/api/admin/categories/${id}`, data);
    return response.data;
  },

  deleteCategory: async (id: string): Promise<void> => {
    await api.delete(`/api/admin/categories/${id}`);
  },

  toggleCategoryStatus: async (
    id: string,
    isActive: boolean,
  ): Promise<void> => {
    await api.patch(`/api/admin/categories/${id}/status`, { isActive });
  },

  // Subcategories
  createSubcategory: async (
    categoryId: string,
    data: SubcategoryFormData,
  ): Promise<Subcategory> => {
    const response = await api.post(
      `/api/admin/categories/${categoryId}/subcategories`,
      data,
    );
    return response.data;
  },

  updateSubcategory: async (
    categoryId: string,
    id: string,
    data: SubcategoryFormData,
  ): Promise<Subcategory> => {
    const response = await api.put(
      `/api/admin/categories/${categoryId}/subcategories/${id}`,
      data,
    );
    return response.data;
  },

  deleteSubcategory: async (categoryId: string, id: string): Promise<void> => {
    await api.delete(`/api/admin/categories/${categoryId}/subcategories/${id}`);
  },
};
```

---

## ✅ VALIDACIÓN

### Settings Page

- [ ] Carga de configuración actual
- [ ] Toggle de modo mantenimiento con mensaje
- [ ] Toggles de registro y verificación
- [ ] Inputs numéricos con min/max
- [ ] Toggles de notificaciones
- [ ] Botón guardar con disabled state
- [ ] Botón descartar cambios
- [ ] Feedback de éxito/error

### Categories Page

- [ ] Lista de categorías con expansión
- [ ] Filtro por vertical (vehicles/properties)
- [ ] Búsqueda por nombre
- [ ] Stats row con conteos
- [ ] Modal crear/editar categoría
- [ ] Modal crear/editar subcategoría
- [ ] Icon picker con emojis
- [ ] Color picker
- [ ] Toggle activo/inactivo
- [ ] Confirmación de eliminación
- [ ] Auto-generación de slug
- [ ] Animaciones con framer-motion

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-settings.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Settings", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar configuración general", async ({ page }) => {
    await page.goto("/admin/settings");

    await expect(
      page.getByRole("heading", { name: /configuración/i }),
    ).toBeVisible();
  });

  test("debe editar configuración del sitio", async ({ page }) => {
    await page.goto("/admin/settings/site");

    await page.fill('input[name="siteName"]', "OKLA Marketplace");
    await page.getByRole("button", { name: /guardar/i }).click();

    await expect(page.getByText(/configuración guardada/i)).toBeVisible();
  });

  test("debe gestionar categorías", async ({ page }) => {
    await page.goto("/admin/settings/categories");

    await expect(page.getByTestId("categories-list")).toBeVisible();
  });

  test("debe crear nueva categoría", async ({ page }) => {
    await page.goto("/admin/settings/categories");

    await page.getByRole("button", { name: /nueva categoría/i }).click();
    await page.fill('input[name="name"]', "Camiones");
    await page.getByRole("button", { name: /guardar/i }).click();

    await expect(page.getByText(/categoría creada/i)).toBeVisible();
  });

  test("debe confirmar eliminación", async ({ page }) => {
    await page.goto("/admin/settings/categories");

    await page
      .getByTestId("category-row")
      .first()
      .getByRole("button", { name: /eliminar/i })
      .click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.getByText(/¿estás seguro/i)).toBeVisible();
  });
});
```

---

_Última actualización: Enero 2026_
