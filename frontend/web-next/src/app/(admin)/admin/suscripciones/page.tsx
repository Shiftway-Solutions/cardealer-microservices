/**
 * Admin Subscriptions Management Page
 *
 * Shows all subscription plans from plan-config.ts (both Dealer and Seller plans),
 * displays features, pricing, mock subscriber counts, and admin actions.
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { formatCurrency } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  CreditCard,
  Users,
  DollarSign,
  TrendingUp,
  Crown,
  Star,
  Zap,
  Eye,
  Pencil,
  Copy,
  Plus,
  Check,
  X,
  Camera,
  BarChart3,
  MessageSquare,
  Shield,
  Building2,
  Sparkles,
} from 'lucide-react';
import {
  DealerPlan,
  SellerPlan,
  DEALER_PLAN_PRICES,
  DEALER_PLAN_LIMITS,
  SELLER_PLAN_LIMITS,
  type DealerPlanFeatures,
  type SellerPlanFeatures,
} from '@/lib/plan-config';

// =============================================================================
// TYPES & MOCK DATA
// =============================================================================

interface PlanCardData {
  id: string;
  key: string;
  name: string;
  description: string;
  monthlyPrice: number;
  targetAudience: 'dealer' | 'seller';
  color: string;
  bgColor: string;
  textColor: string;
  icon: React.ElementType;
  subscriberCount: number; // Mock
  isPopular?: boolean;
}

// Mock subscriber counts since no backend exists
const MOCK_SUBSCRIBER_COUNTS: Record<string, number> = {
  libre: 342,
  visible: 128,
  pro: 67,
  elite: 23,
  gratis: 1204,
  premium: 89,
  'seller-pro': 34,
};

const DEALER_PLAN_CARDS: PlanCardData[] = [
  {
    id: 'd1',
    key: DealerPlan.LIBRE,
    name: 'LIBRE',
    description: 'Plan gratuito para empezar en la plataforma',
    monthlyPrice: 0,
    targetAudience: 'dealer',
    color: 'gray',
    bgColor: 'bg-gray-50',
    textColor: 'text-gray-600',
    icon: Shield,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['libre'],
  },
  {
    id: 'd2',
    key: DealerPlan.VISIBLE,
    name: 'VISIBLE',
    description: 'Para dealers que quieren destacar',
    monthlyPrice: DEALER_PLAN_PRICES[DealerPlan.VISIBLE],
    targetAudience: 'dealer',
    color: 'blue',
    bgColor: 'bg-blue-50',
    textColor: 'text-blue-600',
    icon: Eye,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['visible'],
  },
  {
    id: 'd3',
    key: DealerPlan.PRO,
    name: 'PRO',
    description: 'Para dealers profesionales',
    monthlyPrice: DEALER_PLAN_PRICES[DealerPlan.PRO],
    targetAudience: 'dealer',
    color: 'purple',
    bgColor: 'bg-purple-50',
    textColor: 'text-purple-600',
    icon: Star,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['pro'],
    isPopular: true,
  },
  {
    id: 'd4',
    key: DealerPlan.ELITE,
    name: 'ÉLITE',
    description: 'Todo incluido para los mejores dealers',
    monthlyPrice: DEALER_PLAN_PRICES[DealerPlan.ELITE],
    targetAudience: 'dealer',
    color: 'amber',
    bgColor: 'bg-amber-50',
    textColor: 'text-amber-600',
    icon: Crown,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['elite'],
  },
];

const SELLER_PLAN_CARDS: PlanCardData[] = [
  {
    id: 's1',
    key: SellerPlan.LIBRE,
    name: 'LIBRE',
    description: 'Para vendedores ocasionales',
    monthlyPrice: 0,
    targetAudience: 'seller',
    color: 'gray',
    bgColor: 'bg-gray-50',
    textColor: 'text-gray-600',
    icon: Shield,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['gratis'],
  },
  {
    id: 's2',
    key: SellerPlan.ESTANDAR,
    name: 'ESTÁNDAR',
    description: 'Para vendedores frecuentes',
    monthlyPrice: 579,
    targetAudience: 'seller',
    color: 'blue',
    bgColor: 'bg-blue-50',
    textColor: 'text-blue-600',
    icon: Star,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['premium'],
    isPopular: true,
  },
  {
    id: 's3',
    key: SellerPlan.VERIFICADO,
    name: 'VERIFICADO',
    description: 'Para vendedores profesionales',
    monthlyPrice: 2029, // RD$ 2,029 DOP = $34.99 USD (2029/58) — matches AdminService sellerVerificado
    targetAudience: 'seller',
    color: 'purple',
    bgColor: 'bg-purple-50',
    textColor: 'text-purple-600',
    icon: Zap,
    subscriberCount: MOCK_SUBSCRIBER_COUNTS['seller-pro'],
  },
];

// =============================================================================
// FEATURE DISPLAY HELPERS
// =============================================================================

interface FeatureDisplay {
  label: string;
  value: string | boolean;
  icon: React.ElementType;
}

function getDealerFeatureDisplay(features: DealerPlanFeatures): FeatureDisplay[] {
  return [
    {
      label: 'Publicaciones',
      value: features.maxListings >= 999999 ? 'Ilimitadas' : String(features.maxListings),
      icon: CreditCard,
    },
    { label: 'Fotos por vehículo', value: String(features.maxImages), icon: Camera },
    { label: 'Publicaciones destacadas', value: String(features.featuredListings), icon: Sparkles },
    { label: 'Analíticas', value: features.analyticsAccess, icon: BarChart3 },
    { label: 'Análisis de mercado', value: features.marketPriceAnalysis, icon: TrendingUp },
    { label: 'Carga masiva (CSV)', value: features.bulkUpload, icon: Plus },
    { label: 'Gestión de leads', value: features.leadManagement, icon: Users },
    { label: 'Email automation', value: features.emailAutomation, icon: MessageSquare },
    { label: 'Branding personalizado', value: features.customBranding, icon: Building2 },
    { label: 'Acceso API', value: features.apiAccess, icon: Zap },
    { label: 'Soporte prioritario', value: features.prioritySupport, icon: Shield },
    { label: 'WhatsApp', value: features.whatsappIntegration, icon: MessageSquare },
    { label: 'Prioridad búsqueda', value: features.searchPriority, icon: TrendingUp },
    { label: 'OKLA Coins/mes', value: String(features.monthlyOklaCoinsCredits), icon: DollarSign },
    {
      label: 'Badge',
      value: features.badgeType === 'none' ? false : features.badgeType,
      icon: Crown,
    },
    {
      label: 'ChatBot IA (web)',
      value:
        features.chatAgentWeb === -1
          ? 'Ilimitado'
          : features.chatAgentWeb === 0
            ? false
            : String(features.chatAgentWeb),
      icon: MessageSquare,
    },
    {
      label: 'Dashboard',
      value: features.dashboardLevel === 'none' ? false : features.dashboardLevel,
      icon: BarChart3,
    },
    { label: 'Vista 360°', value: features.view360Available, icon: Eye },
    { label: 'Video tour', value: features.videoTour, icon: Camera },
    { label: 'Máx. videos', value: String(features.maxVideos), icon: Camera },
  ];
}

function getSellerFeatureDisplay(features: SellerPlanFeatures): FeatureDisplay[] {
  return [
    { label: 'Publicaciones activas', value: String(features.maxListings), icon: CreditCard },
    { label: 'Fotos por vehículo', value: String(features.maxImages), icon: Camera },
    {
      label: 'Duración publicación',
      value: features.listingDuration === 0 ? 'Permanente' : `${features.listingDuration} días`,
      icon: CreditCard,
    },
    { label: 'Publicaciones destacadas', value: String(features.featuredListings), icon: Sparkles },
    { label: 'Analíticas', value: features.analyticsAccess, icon: BarChart3 },
    { label: 'Prioridad búsqueda', value: features.searchPriority, icon: TrendingUp },
    { label: 'Badge verificado', value: features.verifiedBadge, icon: Crown },
    { label: 'Contacto WhatsApp', value: features.whatsappContact, icon: MessageSquare },
    { label: 'Estadísticas detalladas', value: features.detailedStats, icon: BarChart3 },
    { label: 'Boosts', value: features.boostAvailable, icon: Zap },
    { label: 'Compartir redes', value: features.socialSharing, icon: MessageSquare },
    { label: 'Alertas de precio', value: features.priceDropAlerts, icon: DollarSign },
    { label: 'Vista 360°', value: features.view360Available, icon: Eye },
    { label: 'Videos', value: String(features.maxVideos), icon: Camera },
  ];
}

function FeatureValue({ value }: { value: string | boolean }) {
  if (typeof value === 'boolean') {
    return value ? (
      <Check className="h-4 w-4 text-emerald-500" />
    ) : (
      <X className="h-4 w-4 text-gray-300" />
    );
  }
  if (value === '0') {
    return <span className="text-xs text-gray-400">—</span>;
  }
  return <span className="text-xs font-semibold text-emerald-600">{value}</span>;
}

// =============================================================================
// PLAN CARD
// =============================================================================

function PlanDetailCard({ plan, features }: { plan: PlanCardData; features: FeatureDisplay[] }) {
  const PlanIcon = plan.icon;
  const formatPrice = (p: number) =>
    p === 0
      ? 'Gratis'
      : new Intl.NumberFormat('es-DO', {
          style: 'currency',
          currency: 'DOP',
          maximumFractionDigits: 0,
        }).format(p);

  return (
    <Card className={`relative ${plan.isPopular ? 'ring-2 ring-purple-500' : ''}`}>
      {plan.isPopular && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <Badge className="bg-purple-500 text-white">Más Popular</Badge>
        </div>
      )}
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className={`rounded-lg ${plan.bgColor} p-2.5`}>
              <PlanIcon className={`h-5 w-5 ${plan.textColor}`} />
            </div>
            <div>
              <CardTitle className="text-lg">{plan.name}</CardTitle>
              <CardDescription className="text-xs">{plan.description}</CardDescription>
            </div>
          </div>
        </div>
        <div className="mt-3">
          <p className="text-2xl font-bold">
            {formatPrice(plan.monthlyPrice)}
            {plan.monthlyPrice > 0 && (
              <span className="text-muted-foreground text-sm font-normal">/mes</span>
            )}
          </p>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Subscriber count */}
        <div className="flex items-center gap-2 rounded-lg bg-gray-50 p-3">
          <Users className="h-4 w-4 text-gray-500" />
          <span className="text-sm font-medium">{plan.subscriberCount} suscriptores</span>
          <Badge variant="outline" className="ml-auto text-xs">
            {plan.targetAudience === 'dealer' ? 'Dealer' : 'Seller'}
          </Badge>
        </div>

        {/* Features list */}
        <div className="space-y-1.5">
          <p className="text-xs font-semibold text-gray-500 uppercase">Funcionalidades</p>
          <div className="max-h-[280px] space-y-1 overflow-y-auto pr-1">
            {features.map((f, i) => (
              <div key={i} className="flex items-center justify-between py-0.5">
                <span className="text-xs text-gray-600">{f.label}</span>
                <FeatureValue value={f.value} />
              </div>
            ))}
          </div>
        </div>

        {/* Actions */}
        <div className="flex gap-2 pt-2">
          <Button asChild variant="outline" size="sm" className="flex-1 gap-1.5">
            <Link href={`/admin/planes?edit=${plan.key}`}>
              <Pencil className="h-3.5 w-3.5" />
              Editar
            </Link>
          </Button>
          <Button asChild variant="outline" size="sm" className="flex-1 gap-1.5">
            <Link href={`/admin/planes?copy=${plan.key}`}>
              <Copy className="h-3.5 w-3.5" />
              Copiar
            </Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function SuscripcionesPage() {
  const totalDealerSubs = DEALER_PLAN_CARDS.reduce((s, p) => s + p.subscriberCount, 0);
  const totalSellerSubs = SELLER_PLAN_CARDS.reduce((s, p) => s + p.subscriberCount, 0);
  const totalSubs = totalDealerSubs + totalSellerSubs;

  const dealerMrr = DEALER_PLAN_CARDS.reduce((s, p) => s + p.monthlyPrice * p.subscriberCount, 0);
  const sellerMrr = SELLER_PLAN_CARDS.reduce((s, p) => s + p.monthlyPrice * p.subscriberCount, 0);
  const totalMrr = dealerMrr + sellerMrr;

  const paidSubs =
    DEALER_PLAN_CARDS.filter(p => p.monthlyPrice > 0).reduce((s, p) => s + p.subscriberCount, 0) +
    SELLER_PLAN_CARDS.filter(p => p.monthlyPrice > 0).reduce((s, p) => s + p.subscriberCount, 0);

  const conversionRate = totalSubs > 0 ? ((paidSubs / totalSubs) * 100).toFixed(1) : '0';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Planes y Suscripciones</h1>
          <p className="text-muted-foreground">
            Visualiza todos los planes de la plataforma, sus características y suscriptores
          </p>
        </div>
        <Button asChild className="gap-2">
          <Link href="/admin/planes">
            <Plus className="h-4 w-4" />
            Crear Nuevo Plan
          </Link>
        </Button>
      </div>

      {/* Overview Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-blue-50 p-3">
              <Users className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Total Suscriptores</p>
              <p className="text-2xl font-bold">{totalSubs.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-green-50 p-3">
              <CreditCard className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Suscriptores Pagos</p>
              <p className="text-2xl font-bold">{paidSubs.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="bg-primary/10 rounded-lg p-3">
              <DollarSign className="text-primary h-6 w-6" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">MRR Estimado</p>
              <p className="text-2xl font-bold">{formatCurrency(totalMrr)}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-purple-50 p-3">
              <TrendingUp className="h-6 w-6 text-purple-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Tasa Conversión</p>
              <p className="text-2xl font-bold">{conversionRate}%</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Plans by Type */}
      <Tabs defaultValue="dealers">
        <TabsList>
          <TabsTrigger value="dealers" className="gap-2">
            <Building2 className="h-4 w-4" />
            Planes Dealers ({DEALER_PLAN_CARDS.length})
          </TabsTrigger>
          <TabsTrigger value="sellers" className="gap-2">
            <Users className="h-4 w-4" />
            Planes Sellers ({SELLER_PLAN_CARDS.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="dealers" className="mt-6">
          <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-4">
            {DEALER_PLAN_CARDS.map(plan => (
              <PlanDetailCard
                key={plan.id}
                plan={plan}
                features={getDealerFeatureDisplay(DEALER_PLAN_LIMITS[plan.key as DealerPlan])}
              />
            ))}
          </div>
        </TabsContent>

        <TabsContent value="sellers" className="mt-6">
          <div className="grid gap-6 sm:grid-cols-2 xl:grid-cols-3">
            {SELLER_PLAN_CARDS.map(plan => (
              <PlanDetailCard
                key={plan.id}
                plan={plan}
                features={getSellerFeatureDisplay(SELLER_PLAN_LIMITS[plan.key as SellerPlan])}
              />
            ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
