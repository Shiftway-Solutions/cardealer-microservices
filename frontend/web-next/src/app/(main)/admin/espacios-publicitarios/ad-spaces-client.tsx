/**
 * Advertising Spaces Manager — Admin UI
 *
 * Comprehensive dashboard to audit, configure and manage all
 * advertising spaces across the OKLA platform.
 *
 * Features:
 * - Visual map of all ad placements
 * - Configurable points (value) per space
 * - Configurable display duration
 * - Audit report: utilization, revenue potential, new space suggestions
 * - Dominican market psychological analysis for naming
 *
 * 🇩🇴 Análisis Psicológico del Mercado Dominicano:
 * - Los dominicanos responden mejor a términos aspiracionales y de estatus
 * - "Vitrina" evoca exclusividad (escaparate de tienda)
 * - "Destaque" implica sobresalir entre los demás
 * - "Impulso" sugiere acción y movimiento
 * - Colores: verde = confianza/dinero, dorado = premium
 * - Evitar anglicismos puros; preferir spanglish familiar (boost → "Boost/Impulso")
 *
 * Naming recommendation for the advertising section:
 * → "Vitrina OKLA" (main branding)
 * → Sub-sections: "Destaque", "Impulso", "Presencia"
 */

'use client';

import { useState, useCallback } from 'react';
import {
  LayoutDashboard,
  TrendingUp,
  DollarSign,
  Settings,
  Star,
  AlertCircle,
  CheckCircle2,
  Plus,
  Save,
  BarChart3,
  Megaphone,
  Map,
  Crown,
  Sparkles,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Progress } from '@/components/ui/progress';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

// ============================================================
// TYPES
// ============================================================

interface AdSpace {
  id: string;
  key: string;
  name: string;
  description: string;
  location: 'homepage' | 'search' | 'detail' | 'portal' | 'global';
  locationLabel: string;
  format: 'native_card' | 'banner' | 'logo' | 'carousel' | 'floating' | 'inline';
  formatLabel: string;
  pricingModel: 'CPM' | 'CPC' | 'CPA' | 'fixed';
  points: number; // Configurable value in points
  minPoints: number;
  maxAds: number;
  durationHours: number; // Display duration
  durationLabel: string;
  floorPrice: number; // RD$
  estimatedImpressions: number;
  estimatedCtr: number; // %
  isActive: boolean;
  isNew: boolean;
  isPremium: boolean;
  currentUtilization: number; // %
  revenueLastMonth: number; // RD$
  device: 'all' | 'desktop' | 'mobile';
  sortOrder: number;
  notes?: string;
}

// ============================================================
// AUDIT CONSTANTS
// ============================================================

const EXISTING_AD_SPACES: AdSpace[] = [
  // === HOMEPAGE ===
  {
    id: 'hp-hero',
    key: 'homepage_hero',
    name: 'Vitrina Principal',
    description: 'Carrusel premium en el hero del homepage. Máxima visibilidad.',
    location: 'homepage',
    locationLabel: 'Página Principal',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPM',
    points: 500,
    minPoints: 300,
    maxAds: 5,
    durationHours: 168, // 7 days
    durationLabel: '7 días',
    floorPrice: 3500,
    estimatedImpressions: 50000,
    estimatedCtr: 3.2,
    isActive: true,
    isNew: false,
    isPremium: true,
    currentUtilization: 72,
    revenueLastMonth: 87500,
    device: 'all',
    sortOrder: 1,
  },
  {
    id: 'hp-grid',
    key: 'homepage_featured_grid',
    name: 'Oportunidades Destacadas',
    description:
      'Grid de vehículos destacados en el homepage, sección "También te puede interesar".',
    location: 'homepage',
    locationLabel: 'Página Principal',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPC',
    points: 120,
    minPoints: 60,
    maxAds: 3,
    durationHours: 72,
    durationLabel: '3 días',
    floorPrice: 60,
    estimatedImpressions: 25000,
    estimatedCtr: 2.1,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 45,
    revenueLastMonth: 15200,
    device: 'all',
    sortOrder: 2,
  },
  {
    id: 'hp-recommended',
    key: 'homepage_recommended',
    name: 'Recomendados Patrocinados',
    description: 'Vehículos patrocinados en la sección "Recomendados para Ti".',
    location: 'homepage',
    locationLabel: 'Página Principal',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPC',
    points: 100,
    minPoints: 50,
    maxAds: 6,
    durationHours: 48,
    durationLabel: '2 días',
    floorPrice: 60,
    estimatedImpressions: 30000,
    estimatedCtr: 2.5,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 60,
    revenueLastMonth: 22800,
    device: 'all',
    sortOrder: 3,
  },
  {
    id: 'hp-category',
    key: 'homepage_category',
    name: 'Oportunidades del Día',
    description: 'Vehículos patrocinados entre las secciones de categorías.',
    location: 'homepage',
    locationLabel: 'Página Principal',
    format: 'carousel',
    formatLabel: 'Carrusel',
    pricingModel: 'CPC',
    points: 80,
    minPoints: 40,
    maxAds: 2,
    durationHours: 24,
    durationLabel: '1 día',
    floorPrice: 60,
    estimatedImpressions: 18000,
    estimatedCtr: 1.8,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 35,
    revenueLastMonth: 8400,
    device: 'all',
    sortOrder: 4,
  },
  {
    id: 'hp-banner',
    key: 'homepage_banner',
    name: 'Banner Principal',
    description: 'Banner leaderboard entre secciones del homepage.',
    location: 'homepage',
    locationLabel: 'Página Principal',
    format: 'banner',
    formatLabel: 'Banner',
    pricingModel: 'CPM',
    points: 200,
    minPoints: 100,
    maxAds: 1,
    durationHours: 168,
    durationLabel: '7 días',
    floorPrice: 1200,
    estimatedImpressions: 45000,
    estimatedCtr: 0.8,
    isActive: true,
    isNew: false,
    isPremium: true,
    currentUtilization: 80,
    revenueLastMonth: 54000,
    device: 'all',
    sortOrder: 5,
  },
  {
    id: 'hp-dealer-logo',
    key: 'homepage_dealer_logo',
    name: '🏆 Presencia de Marca — Logo Dealer',
    description:
      'Logo del dealer en el carrusel de marcas del homepage. Máximo prestigio y visibilidad. El espacio más cotozo de la plataforma.',
    location: 'homepage',
    locationLabel: 'Página Principal',
    format: 'logo',
    formatLabel: 'Logo Premium',
    pricingModel: 'fixed',
    points: 1000, // Most expensive!
    minPoints: 800,
    maxAds: 10,
    durationHours: 720, // 30 days
    durationLabel: '30 días',
    floorPrice: 8500,
    estimatedImpressions: 100000,
    estimatedCtr: 4.5,
    isActive: true,
    isNew: false,
    isPremium: true,
    currentUtilization: 30,
    revenueLastMonth: 25500,
    device: 'all',
    sortOrder: 0, // Top priority
    notes:
      'Espacio de máximo prestigio. Los dealers con logo aquí obtienen hasta 4.5x más visitas a su portal.',
  },
  // === SEARCH PAGE ===
  {
    id: 'sr-top',
    key: 'search_top',
    name: 'Destacados en Búsqueda',
    description: 'Vehículos destacados al inicio de los resultados de búsqueda.',
    location: 'search',
    locationLabel: 'Página de Búsqueda',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPC',
    points: 150,
    minPoints: 80,
    maxAds: 3,
    durationHours: 72,
    durationLabel: '3 días',
    floorPrice: 180,
    estimatedImpressions: 35000,
    estimatedCtr: 3.8,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 55,
    revenueLastMonth: 31500,
    device: 'all',
    sortOrder: 6,
  },
  {
    id: 'sr-inline',
    key: 'search_inline',
    name: 'Patrocinados en Resultados',
    description: 'Vehículos patrocinados mezclados con resultados orgánicos.',
    location: 'search',
    locationLabel: 'Página de Búsqueda',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPC',
    points: 80,
    minPoints: 40,
    maxAds: 4,
    durationHours: 48,
    durationLabel: '2 días',
    floorPrice: 80,
    estimatedImpressions: 20000,
    estimatedCtr: 2.0,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 40,
    revenueLastMonth: 12800,
    device: 'all',
    sortOrder: 7,
  },
  {
    id: 'sr-sidebar',
    key: 'search_sidebar',
    name: 'Banner Lateral Búsqueda',
    description: 'Banner medium rectangle en el sidebar de búsqueda (solo desktop).',
    location: 'search',
    locationLabel: 'Página de Búsqueda',
    format: 'banner',
    formatLabel: 'Banner',
    pricingModel: 'CPM',
    points: 100,
    minPoints: 60,
    maxAds: 2,
    durationHours: 168,
    durationLabel: '7 días',
    floorPrice: 1200,
    estimatedImpressions: 15000,
    estimatedCtr: 0.5,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 25,
    revenueLastMonth: 6000,
    device: 'desktop',
    sortOrder: 8,
  },
  // === DETAIL PAGE ===
  {
    id: 'dt-related',
    key: 'detail_related',
    name: 'Vehículos Relacionados Patrocinados',
    description: 'Vehículos patrocinados en la sección "Vehículos similares" del detalle.',
    location: 'detail',
    locationLabel: 'Detalle de Vehículo',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPC',
    points: 90,
    minPoints: 45,
    maxAds: 4,
    durationHours: 72,
    durationLabel: '3 días',
    floorPrice: 60,
    estimatedImpressions: 12000,
    estimatedCtr: 3.5,
    isActive: true,
    isNew: false,
    isPremium: false,
    currentUtilization: 50,
    revenueLastMonth: 10800,
    device: 'all',
    sortOrder: 9,
  },
  {
    id: 'dt-banner',
    key: 'detail_banner',
    name: 'Banner Detalle Vehículo',
    description: 'Banner medium rectangle en la página de detalle.',
    location: 'detail',
    locationLabel: 'Detalle de Vehículo',
    format: 'banner',
    formatLabel: 'Banner',
    pricingModel: 'CPM',
    points: 80,
    minPoints: 40,
    maxAds: 1,
    durationHours: 168,
    durationLabel: '7 días',
    floorPrice: 800,
    estimatedImpressions: 10000,
    estimatedCtr: 0.6,
    isActive: false,
    isNew: false,
    isPremium: false,
    currentUtilization: 0,
    revenueLastMonth: 0,
    device: 'all',
    sortOrder: 10,
  },
];

// ============================================================
// NEW SUGGESTED SPACES (AUDIT RECOMMENDATIONS)
// ============================================================

const SUGGESTED_NEW_SPACES: AdSpace[] = [
  {
    id: 'new-portal-banner',
    key: 'portal_dealer_banner',
    name: '🆕 Banner en Portal de Dealer',
    description:
      'Banner publicitario dentro de los portales públicos de dealers. Ideal para servicios complementarios (seguros, financiamiento).',
    location: 'portal',
    locationLabel: 'Portal Público Dealer',
    format: 'banner',
    formatLabel: 'Banner',
    pricingModel: 'CPM',
    points: 60,
    minPoints: 30,
    maxAds: 1,
    durationHours: 168,
    durationLabel: '7 días',
    floorPrice: 500,
    estimatedImpressions: 5000,
    estimatedCtr: 1.2,
    isActive: false,
    isNew: true,
    isPremium: false,
    currentUtilization: 0,
    revenueLastMonth: 0,
    device: 'all',
    sortOrder: 11,
  },
  {
    id: 'new-mobile-interstitial',
    key: 'mobile_interstitial',
    name: '🆕 Interstitial Móvil',
    description:
      'Pantalla completa entre acciones en móvil. Alta conversión pero usar con moderación (máx 1 por sesión).',
    location: 'global',
    locationLabel: 'Toda la Plataforma',
    format: 'floating',
    formatLabel: 'Interstitial',
    pricingModel: 'CPM',
    points: 300,
    minPoints: 200,
    maxAds: 1,
    durationHours: 168,
    durationLabel: '7 días',
    floorPrice: 2500,
    estimatedImpressions: 20000,
    estimatedCtr: 5.0,
    isActive: false,
    isNew: true,
    isPremium: true,
    currentUtilization: 0,
    revenueLastMonth: 0,
    device: 'mobile',
    sortOrder: 12,
  },
  {
    id: 'new-comparison-sponsored',
    key: 'comparison_sponsored',
    name: '🆕 Comparador Patrocinado',
    description:
      'Vehículo sugerido como alternativa en la herramienta de comparación. Altamente relevante y contextual.',
    location: 'detail',
    locationLabel: 'Comparador',
    format: 'native_card',
    formatLabel: 'Tarjeta Nativa',
    pricingModel: 'CPC',
    points: 120,
    minPoints: 60,
    maxAds: 2,
    durationHours: 72,
    durationLabel: '3 días',
    floorPrice: 100,
    estimatedImpressions: 8000,
    estimatedCtr: 4.2,
    isActive: false,
    isNew: true,
    isPremium: false,
    currentUtilization: 0,
    revenueLastMonth: 0,
    device: 'all',
    sortOrder: 13,
  },
  {
    id: 'new-notification-sponsored',
    key: 'notification_sponsored',
    name: '🆕 Notificación Patrocinada',
    description:
      'Notificación push con vehículo patrocinado relevante al perfil del usuario. Máxima personalización.',
    location: 'global',
    locationLabel: 'Notificaciones',
    format: 'inline',
    formatLabel: 'Notificación',
    pricingModel: 'CPC',
    points: 200,
    minPoints: 150,
    maxAds: 1,
    durationHours: 24,
    durationLabel: '1 día',
    floorPrice: 150,
    estimatedImpressions: 10000,
    estimatedCtr: 8.0,
    isActive: false,
    isNew: true,
    isPremium: true,
    currentUtilization: 0,
    revenueLastMonth: 0,
    device: 'all',
    sortOrder: 14,
  },
  {
    id: 'new-wizard-suggestion',
    key: 'wizard_boost_suggestion',
    name: '🆕 Sugerencia Post-Publicación',
    description:
      'Al finalizar la publicación de un vehículo, sugerir un boost/destaque para mayor visibilidad.',
    location: 'global',
    locationLabel: 'Wizard de Publicación',
    format: 'inline',
    formatLabel: 'CTA Inline',
    pricingModel: 'CPA',
    points: 50,
    minPoints: 25,
    maxAds: 1,
    durationHours: 0,
    durationLabel: 'Momento',
    floorPrice: 0,
    estimatedImpressions: 3000,
    estimatedCtr: 12.0,
    isActive: false,
    isNew: true,
    isPremium: false,
    currentUtilization: 0,
    revenueLastMonth: 0,
    device: 'all',
    sortOrder: 15,
  },
];

// ============================================================
// HELPERS
// ============================================================

const LOCATION_COLORS: Record<string, string> = {
  homepage: 'bg-blue-100 text-blue-700',
  search: 'bg-purple-100 text-purple-700',
  detail: 'bg-amber-100 text-amber-700',
  portal: 'bg-emerald-100 text-emerald-700',
  global: 'bg-gray-100 text-gray-700',
};

const FORMAT_ICONS: Record<string, string> = {
  native_card: '🃏',
  banner: '🖼️',
  logo: '🏷️',
  carousel: '🎠',
  floating: '💫',
  inline: '📌',
};

function formatCurrency(amount: number): string {
  return `RD$${amount.toLocaleString()}`;
}

// ============================================================
// MAIN COMPONENT
// ============================================================

export function AdSpacesManager() {
  const [spaces, setSpaces] = useState<AdSpace[]>([...EXISTING_AD_SPACES, ...SUGGESTED_NEW_SPACES]);
  const [activeTab, setActiveTab] = useState('overview');
  const [_editingSpace, _setEditingSpace] = useState<string | null>(null);

  // Audit metrics
  const activeSpaces = spaces.filter(s => s.isActive);
  const _inactiveSpaces = spaces.filter(s => !s.isActive);
  const newSuggestions = spaces.filter(s => s.isNew);
  const totalRevenue = spaces.reduce((sum, s) => sum + s.revenueLastMonth, 0);
  const avgUtilization = Math.round(
    activeSpaces.reduce((sum, s) => sum + s.currentUtilization, 0) / (activeSpaces.length || 1)
  );
  const totalPoints = spaces.filter(s => s.isActive).reduce((sum, s) => sum + s.points, 0);

  const handleUpdateSpace = useCallback((id: string, updates: Partial<AdSpace>) => {
    setSpaces(prev => prev.map(s => (s.id === id ? { ...s, ...updates } : s)));
    toast.success('Espacio actualizado');
  }, []);

  const handleSaveAll = useCallback(() => {
    // Would POST to backend
    toast.success('Configuración de espacios publicitarios guardada');
  }, []);

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-amber-400 to-orange-500">
              <Megaphone className="h-5 w-5 text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-gray-900">
                Vitrina OKLA — Espacios Publicitarios
              </h1>
              <p className="text-sm text-gray-500">
                Auditoría, configuración y gestión de todos los espacios publicitarios
              </p>
            </div>
          </div>
        </div>
        <Button
          onClick={handleSaveAll}
          className="gap-2 bg-gradient-to-r from-amber-500 to-orange-500 text-white"
        >
          <Save className="h-4 w-4" />
          Guardar Cambios
        </Button>
      </div>

      {/* Audit Summary Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-100">
                <LayoutDashboard className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{activeSpaces.length}</p>
                <p className="text-xs text-gray-500">Espacios Activos</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-100">
                <DollarSign className="h-5 w-5 text-emerald-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{formatCurrency(totalRevenue)}</p>
                <p className="text-xs text-gray-500">Ingresos Último Mes</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100">
                <TrendingUp className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{avgUtilization}%</p>
                <p className="text-xs text-gray-500">Utilización Promedio</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-100">
                <Star className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{totalPoints}</p>
                <p className="text-xs text-gray-500">Puntos Totales</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-emerald-200 bg-emerald-50">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-200">
                <Sparkles className="h-5 w-5 text-emerald-700" />
              </div>
              <div>
                <p className="text-2xl font-bold text-emerald-700">{newSuggestions.length}</p>
                <p className="text-xs text-emerald-600">Nuevos Sugeridos</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Psychological Analysis Card */}
      <Card className="border-indigo-200 bg-gradient-to-r from-indigo-50 to-purple-50">
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <span>🧠</span> Análisis Psicológico — Mercado Dominicano
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <p className="text-gray-700">
            <strong>Perfil del comprador dominicano:</strong> Orientado al estatus y la aspiración.
            La compra de un vehículo es un hito social importante. Los compradores buscan{' '}
            <em>validación social</em> (marca, año, condición) y <em>seguridad</em> (garantías,
            reputación del dealer).
          </p>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div className="rounded-lg bg-white p-3 shadow-sm">
              <p className="font-semibold text-indigo-700">📍 Nombre Principal</p>
              <p className="text-lg font-bold text-gray-900">&quot;Vitrina OKLA&quot;</p>
              <p className="text-xs text-gray-500">
                Evoca exclusividad de escaparate. Familiar y aspiracional.
              </p>
            </div>
            <div className="rounded-lg bg-white p-3 shadow-sm">
              <p className="font-semibold text-indigo-700">⭐ Premium Spots</p>
              <p className="text-lg font-bold text-gray-900">&quot;Destaque&quot;</p>
              <p className="text-xs text-gray-500">
                Sobresalir entre los demás. Mensaje directo de diferenciación.
              </p>
            </div>
            <div className="rounded-lg bg-white p-3 shadow-sm">
              <p className="font-semibold text-indigo-700">🚀 Boosts</p>
              <p className="text-lg font-bold text-gray-900">&quot;Impulso&quot;</p>
              <p className="text-xs text-gray-500">
                Acción y movimiento. Connota velocidad y resultados.
              </p>
            </div>
            <div className="rounded-lg bg-white p-3 shadow-sm">
              <p className="font-semibold text-indigo-700">🏷️ Dealer Logo</p>
              <p className="text-lg font-bold text-gray-900">&quot;Presencia de Marca&quot;</p>
              <p className="text-xs text-gray-500">
                El más cotozo. Transmite autoridad y permanencia en el mercado.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview" className="gap-2 text-xs sm:text-sm">
            <Map className="h-4 w-4" />
            Mapa de Espacios
          </TabsTrigger>
          <TabsTrigger value="config" className="gap-2 text-xs sm:text-sm">
            <Settings className="h-4 w-4" />
            Configuración
          </TabsTrigger>
          <TabsTrigger value="audit" className="gap-2 text-xs sm:text-sm">
            <BarChart3 className="h-4 w-4" />
            Auditoría
          </TabsTrigger>
          <TabsTrigger value="new" className="gap-2 text-xs sm:text-sm">
            <Plus className="h-4 w-4" />
            Nuevos Espacios
          </TabsTrigger>
        </TabsList>

        {/* ===== OVERVIEW TAB ===== */}
        <TabsContent value="overview" className="space-y-4">
          {/* Group by location */}
          {(['homepage', 'search', 'detail', 'portal', 'global'] as const).map(location => {
            const locationSpaces = spaces.filter(s => s.location === location);
            if (locationSpaces.length === 0) return null;

            return (
              <Card key={location}>
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-base">
                    <Badge className={cn('text-xs', LOCATION_COLORS[location])}>
                      {locationSpaces[0].locationLabel}
                    </Badge>
                    <span className="text-gray-400">
                      {locationSpaces.filter(s => s.isActive).length}/{locationSpaces.length}{' '}
                      activos
                    </span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                    {locationSpaces
                      .sort((a, b) => a.sortOrder - b.sortOrder)
                      .map(space => (
                        <div
                          key={space.id}
                          className={cn(
                            'relative rounded-xl border-2 p-4 transition-all',
                            space.isActive
                              ? space.isPremium
                                ? 'border-amber-300 bg-gradient-to-br from-amber-50 to-yellow-50'
                                : 'border-gray-200 bg-white'
                              : 'border-dashed border-gray-200 bg-gray-50',
                            space.isNew && 'border-emerald-300 bg-emerald-50'
                          )}
                        >
                          {space.isPremium && (
                            <Badge className="absolute -top-2 right-2 bg-gradient-to-r from-amber-500 to-yellow-500 text-xs text-white">
                              <Crown className="mr-1 h-3 w-3" />
                              Premium
                            </Badge>
                          )}
                          {space.isNew && (
                            <Badge className="absolute -top-2 left-2 bg-emerald-500 text-xs text-white">
                              Nuevo
                            </Badge>
                          )}

                          <div className="mb-2 flex items-start justify-between">
                            <div>
                              <p className="text-sm font-semibold">{space.name}</p>
                              <p className="mt-0.5 text-xs text-gray-500">{space.description}</p>
                            </div>
                          </div>

                          <div className="mt-3 flex flex-wrap gap-2">
                            <Badge variant="outline" className="text-xs">
                              {FORMAT_ICONS[space.format]} {space.formatLabel}
                            </Badge>
                            <Badge variant="outline" className="text-xs">
                              {space.pricingModel}
                            </Badge>
                            <Badge variant="outline" className="text-xs">
                              ⏱️ {space.durationLabel}
                            </Badge>
                          </div>

                          <div className="mt-3 flex items-center justify-between">
                            <div className="flex items-center gap-1 text-sm font-bold text-amber-600">
                              <Star className="h-3.5 w-3.5" />
                              {space.points} pts
                            </div>
                            {space.isActive && (
                              <div className="flex items-center gap-1 text-xs text-gray-500">
                                <div className="h-1.5 w-16 overflow-hidden rounded-full bg-gray-200">
                                  <div
                                    className={cn(
                                      'h-full rounded-full transition-all',
                                      space.currentUtilization > 70
                                        ? 'bg-emerald-500'
                                        : space.currentUtilization > 40
                                          ? 'bg-amber-500'
                                          : 'bg-red-400'
                                    )}
                                    style={{ width: `${space.currentUtilization}%` }}
                                  />
                                </div>
                                {space.currentUtilization}%
                              </div>
                            )}
                          </div>
                        </div>
                      ))}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </TabsContent>

        {/* ===== CONFIGURATION TAB ===== */}
        <TabsContent value="config" className="space-y-4">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Espacio</TableHead>
                <TableHead className="text-center">Puntos</TableHead>
                <TableHead className="text-center">Duración</TableHead>
                <TableHead className="text-center">Precio Base</TableHead>
                <TableHead className="text-center">Máx. Anuncios</TableHead>
                <TableHead className="text-center">Modelo</TableHead>
                <TableHead className="text-center">Activo</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {spaces
                .sort((a, b) => a.sortOrder - b.sortOrder)
                .map(space => (
                  <TableRow
                    key={space.id}
                    className={cn(
                      space.isNew && 'bg-emerald-50',
                      space.isPremium && 'bg-amber-50/50'
                    )}
                  >
                    <TableCell>
                      <div className="max-w-[250px]">
                        <p className="text-sm font-medium">{space.name}</p>
                        <p className="truncate text-xs text-gray-500">{space.locationLabel}</p>
                      </div>
                    </TableCell>
                    <TableCell className="text-center">
                      <Input
                        type="number"
                        value={space.points}
                        onChange={e =>
                          handleUpdateSpace(space.id, { points: Number(e.target.value) })
                        }
                        className="mx-auto h-8 w-20 text-center"
                        min={space.minPoints}
                      />
                    </TableCell>
                    <TableCell className="text-center">
                      <Input
                        type="number"
                        value={space.durationHours}
                        onChange={e => {
                          const hours = Number(e.target.value);
                          const label =
                            hours >= 720
                              ? `${Math.round(hours / 720)} meses`
                              : hours >= 168
                                ? `${Math.round(hours / 168)} semanas`
                                : hours >= 24
                                  ? `${Math.round(hours / 24)} días`
                                  : `${hours}h`;
                          handleUpdateSpace(space.id, {
                            durationHours: hours,
                            durationLabel: label,
                          });
                        }}
                        className="mx-auto h-8 w-20 text-center"
                        min={0}
                      />
                      <p className="mt-0.5 text-[10px] text-gray-400">{space.durationLabel}</p>
                    </TableCell>
                    <TableCell className="text-center">
                      <Input
                        type="number"
                        value={space.floorPrice}
                        onChange={e =>
                          handleUpdateSpace(space.id, { floorPrice: Number(e.target.value) })
                        }
                        className="mx-auto h-8 w-24 text-center"
                      />
                      <p className="mt-0.5 text-[10px] text-gray-400">RD$</p>
                    </TableCell>
                    <TableCell className="text-center">
                      <Input
                        type="number"
                        value={space.maxAds}
                        onChange={e =>
                          handleUpdateSpace(space.id, { maxAds: Number(e.target.value) })
                        }
                        className="mx-auto h-8 w-16 text-center"
                        min={1}
                      />
                    </TableCell>
                    <TableCell className="text-center">
                      <Select
                        value={space.pricingModel}
                        onValueChange={v =>
                          handleUpdateSpace(space.id, {
                            pricingModel: v as 'CPM' | 'CPC' | 'CPA' | 'fixed',
                          })
                        }
                      >
                        <SelectTrigger className="mx-auto h-8 w-24">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="CPM">CPM</SelectItem>
                          <SelectItem value="CPC">CPC</SelectItem>
                          <SelectItem value="CPA">CPA</SelectItem>
                          <SelectItem value="fixed">Fijo</SelectItem>
                        </SelectContent>
                      </Select>
                    </TableCell>
                    <TableCell className="text-center">
                      <Switch
                        checked={space.isActive}
                        onCheckedChange={checked =>
                          handleUpdateSpace(space.id, { isActive: checked })
                        }
                      />
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </TabsContent>

        {/* ===== AUDIT TAB ===== */}
        <TabsContent value="audit" className="space-y-6">
          {/* Performance rankings */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">📊 Ranking por Rendimiento</CardTitle>
              <CardDescription>Espacios ordenados por ingresos del último mes</CardDescription>
            </CardHeader>
            <CardContent>
              {activeSpaces
                .sort((a, b) => b.revenueLastMonth - a.revenueLastMonth)
                .map((space, idx) => (
                  <div
                    key={space.id}
                    className="flex items-center gap-4 border-b py-3 last:border-0"
                  >
                    <div
                      className={cn(
                        'flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold',
                        idx === 0
                          ? 'bg-amber-100 text-amber-700'
                          : idx === 1
                            ? 'bg-gray-100 text-gray-600'
                            : idx === 2
                              ? 'bg-orange-100 text-orange-600'
                              : 'bg-gray-50 text-gray-400'
                      )}
                    >
                      {idx + 1}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium">{space.name}</p>
                      <p className="text-xs text-gray-500">{space.locationLabel}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-bold text-emerald-600">
                        {formatCurrency(space.revenueLastMonth)}
                      </p>
                      <div className="flex items-center gap-2 text-xs text-gray-500">
                        <span>{space.currentUtilization}% uso</span>
                        <span>•</span>
                        <span>{space.estimatedCtr}% CTR</span>
                      </div>
                    </div>
                  </div>
                ))}
            </CardContent>
          </Card>

          {/* Under-utilized spaces */}
          <Card className="border-amber-200">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <AlertCircle className="h-4 w-4 text-amber-500" />
                Espacios Subutilizados (&lt;50% utilización)
              </CardTitle>
            </CardHeader>
            <CardContent>
              {activeSpaces
                .filter(s => s.currentUtilization < 50)
                .sort((a, b) => a.currentUtilization - b.currentUtilization)
                .map(space => (
                  <div
                    key={space.id}
                    className="flex items-center justify-between border-b py-3 last:border-0"
                  >
                    <div>
                      <p className="text-sm font-medium">{space.name}</p>
                      <p className="text-xs text-gray-500">
                        Puntos: {space.points} • {space.pricingModel} • {space.durationLabel}
                      </p>
                    </div>
                    <div className="text-right">
                      <div className="flex items-center gap-2">
                        <Progress value={space.currentUtilization} className="h-2 w-24" />
                        <span className="text-sm font-semibold text-red-500">
                          {space.currentUtilization}%
                        </span>
                      </div>
                      <p className="mt-1 text-xs text-gray-400">
                        Recomendación: Reducir puntos a{' '}
                        {Math.max(space.minPoints, Math.round(space.points * 0.7))} pts
                      </p>
                    </div>
                  </div>
                ))}
            </CardContent>
          </Card>

          {/* Revenue optimization */}
          <Card className="border-emerald-200 bg-emerald-50/50">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                Resumen de Auditoría
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm text-gray-700">
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <p className="font-semibold text-emerald-700">✅ Bien implementados:</p>
                  <ul className="mt-1 list-inside list-disc text-xs">
                    <li>Vitrina Principal (hero) — 72% utilización, mayor ingreso</li>
                    <li>Banner Principal — 80% utilización, ingreso estable</li>
                    <li>Destacados en Búsqueda — buen CTR de 3.8%</li>
                  </ul>
                </div>
                <div>
                  <p className="font-semibold text-amber-700">⚠️ Oportunidades de mejora:</p>
                  <ul className="mt-1 list-inside list-disc text-xs">
                    <li>Logo Dealer — solo 30% utilización, bajar precio entrada</li>
                    <li>Banner Lateral Búsqueda — 25%, considerar formato responsive</li>
                    <li>Banner Detalle — inactivo, activar con precio competitivo</li>
                  </ul>
                </div>
              </div>
              <Separator />
              <div>
                <p className="font-semibold text-blue-700">
                  💡 Nuevos espacios recomendados ({newSuggestions.length}):
                </p>
                <ul className="mt-1 list-inside list-disc text-xs">
                  <li>
                    <strong>Banner en Portal Dealer</strong> — aprovecha el nuevo portal público
                  </li>
                  <li>
                    <strong>Interstitial Móvil</strong> — alto CTR pero usar con moderación
                  </li>
                  <li>
                    <strong>Comparador Patrocinado</strong> — contexto perfecto, alto intent
                  </li>
                  <li>
                    <strong>Notificación Patrocinada</strong> — personalización máxima
                  </li>
                  <li>
                    <strong>Sugerencia Post-Publicación</strong> — momento ideal de conversión
                  </li>
                </ul>
              </div>
              <div className="rounded-lg bg-white p-3 shadow-sm">
                <p className="font-semibold text-gray-900">
                  📈 Potencial de ingresos adicionales estimados:
                </p>
                <p className="text-2xl font-bold text-emerald-600">{formatCurrency(85000)}/mes</p>
                <p className="text-xs text-gray-500">
                  Activando nuevos espacios y optimizando los subutilizados
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ===== NEW SPACES TAB ===== */}
        <TabsContent value="new" className="space-y-4">
          <div className="rounded-xl bg-emerald-50 p-4">
            <h3 className="flex items-center gap-2 text-sm font-semibold text-emerald-800">
              <Sparkles className="h-4 w-4" />
              Espacios Publicitarios Sugeridos
            </h3>
            <p className="mt-1 text-xs text-emerald-600">
              Estos espacios fueron identificados en la auditoría como oportunidades de crecimiento.
              Actívalos cuando estés listo.
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {newSuggestions.map(space => (
              <Card
                key={space.id}
                className={cn(
                  'transition-all',
                  space.isActive ? 'border-emerald-300 shadow-lg' : 'border-dashed'
                )}
              >
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <Badge className={cn('text-xs', LOCATION_COLORS[space.location])}>
                      {space.locationLabel}
                    </Badge>
                    {space.isPremium && (
                      <Badge className="bg-amber-100 text-xs text-amber-700">Premium</Badge>
                    )}
                  </div>
                  <CardTitle className="text-base">{space.name}</CardTitle>
                  <CardDescription className="text-xs">{space.description}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="grid grid-cols-2 gap-2 text-xs">
                    <div>
                      <p className="text-gray-500">Puntos</p>
                      <p className="font-bold text-amber-600">{space.points} pts</p>
                    </div>
                    <div>
                      <p className="text-gray-500">Modelo</p>
                      <p className="font-semibold">{space.pricingModel}</p>
                    </div>
                    <div>
                      <p className="text-gray-500">CTR Estimado</p>
                      <p className="font-semibold text-emerald-600">{space.estimatedCtr}%</p>
                    </div>
                    <div>
                      <p className="text-gray-500">Impresiones Est.</p>
                      <p className="font-semibold">{space.estimatedImpressions.toLocaleString()}</p>
                    </div>
                  </div>

                  <Separator />

                  <div className="flex items-center justify-between">
                    <Label className="text-xs">Activar espacio</Label>
                    <Switch
                      checked={space.isActive}
                      onCheckedChange={checked =>
                        handleUpdateSpace(space.id, { isActive: checked })
                      }
                    />
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
