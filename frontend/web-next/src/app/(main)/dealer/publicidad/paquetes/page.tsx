/**
 * Ad Packages — /dealer/publicidad/paquetes
 *
 * Pre-configured advertising packages dealers/sellers can purchase.
 * Each package bundles impressions, featured days, and search positions
 * into a single purchase with clear pricing.
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/hooks/use-auth';
import { useCreateCampaign } from '@/hooks/use-advertising';
import { toast } from 'sonner';
import {
  ArrowLeft,
  Check,
  Sparkles,
  Zap,
  Crown,
  Rocket,
  Eye,
  MousePointerClick,
  TrendingUp,
  Star,
  Shield,
  BarChart3,
  Timer,
  Loader2,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// =============================================================================
// TYPES
// =============================================================================

interface AdPackage {
  id: string;
  name: string;
  tagline: string;
  price: number;
  originalPrice?: number;
  duration: number; // days
  features: string[];
  metrics: { impressions: string; clicks: string; position: string };
  icon: React.ElementType;
  color: string;
  bgGradient: string;
  popular?: boolean;
  bestValue?: boolean;
}

// =============================================================================
// DATA
// =============================================================================

const AD_PACKAGES: AdPackage[] = [
  {
    id: 'starter',
    name: 'Impulso',
    tagline: 'Ideal para empezar',
    price: 1490,
    duration: 7,
    features: [
      'Hasta 5,000 impresiones',
      'Badge "Destacado" en listados',
      'Aparece en resultados de búsqueda',
      'Estadísticas básicas de rendimiento',
    ],
    metrics: { impressions: '5K', clicks: '~150', position: 'Top 20' },
    icon: Star,
    color: 'text-blue-600',
    bgGradient: 'from-blue-50 to-blue-100/50',
  },
  {
    id: 'growth',
    name: 'Crecimiento',
    tagline: 'El más popular',
    price: 3990,
    originalPrice: 4990,
    duration: 15,
    features: [
      'Hasta 20,000 impresiones',
      'Posición prioritaria en búsquedas',
      'Aparece en sección "Recomendados"',
      'Badge "Destacado" premium',
      'Estadísticas detalladas + CTR',
      'Soporte por WhatsApp',
    ],
    metrics: { impressions: '20K', clicks: '~600', position: 'Top 5' },
    icon: Zap,
    color: 'text-emerald-600',
    bgGradient: 'from-emerald-50 to-emerald-100/50',
    popular: true,
  },
  {
    id: 'domination',
    name: 'Dominación',
    tagline: 'Máxima visibilidad',
    price: 7990,
    originalPrice: 9990,
    duration: 30,
    features: [
      'Hasta 60,000 impresiones',
      'Posición #1 en búsquedas relevantes',
      'Homepage: sección destacada',
      'Homepage: banner patrocinado',
      'Retargeting a compradores interesados',
      'Reporte semanal de rendimiento',
      'Asesor de publicidad dedicado',
      'Garantía de satisfacción',
    ],
    metrics: { impressions: '60K', clicks: '~2,400', position: '#1' },
    icon: Crown,
    color: 'text-amber-600',
    bgGradient: 'from-amber-50 to-amber-100/50',
    bestValue: true,
  },
];

const TESTIMONIALS = [
  {
    quote: 'Vendí 3 vehículos en la primera semana con el paquete Crecimiento.',
    author: 'Carlos M.',
    role: 'Dealer en Santo Domingo',
    rating: 5,
  },
  {
    quote: 'El ROI superó mis expectativas. El paquete se paga solo.',
    author: 'María R.',
    role: 'Vendedora independiente',
    rating: 5,
  },
];

// =============================================================================
// HELPERS
// =============================================================================

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(amount);
}

// =============================================================================
// MAIN
// =============================================================================

export default function AdPackagesPage() {
  const { user } = useAuth();
  const createCampaign = useCreateCampaign();
  const [selectedPackage, setSelectedPackage] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  const handlePurchase = async (pkg: AdPackage) => {
    setIsProcessing(true);
    setSelectedPackage(pkg.id);

    try {
      const startDate = new Date();
      const endDate = new Date();
      endDate.setDate(endDate.getDate() + pkg.duration);

      await createCampaign.mutateAsync({
        name: `Paquete ${pkg.name} — ${new Date().toLocaleDateString('es-DO')}`,
        ownerType: 'Dealer',
        ownerId: user?.id || '',
        vehicleIds: [],
        placementType: pkg.id === 'domination' ? 'PremiumSpot' : 'FeaturedSpot',
        pricingModel: 'FlatFee',
        totalBudget: pkg.price,
        dailyBudget: Math.ceil(pkg.price / pkg.duration),
        bidAmount: 0,
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
      });

      toast.success('¡Paquete activado!', {
        description: `Tu paquete ${pkg.name} está activo por ${pkg.duration} días.`,
      });
    } catch {
      toast.error('Error al procesar', {
        description: 'No pudimos procesar tu compra. Intenta de nuevo.',
      });
    } finally {
      setIsProcessing(false);
      setSelectedPackage(null);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      {/* Header */}
      <div className="border-b bg-white">
        <div className="mx-auto max-w-5xl px-4 py-6 sm:px-6">
          <Link
            href="/dealer/publicidad"
            className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            Publicidad
          </Link>
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-500 to-emerald-600 shadow-lg shadow-emerald-200">
              <Sparkles className="h-5 w-5 text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-900">Paquetes Publicitarios</h1>
              <p className="text-sm text-slate-500">
                Elige el paquete ideal para tu negocio. Sin contratos, cancela cuando quieras.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Stats bar */}
      <div className="border-b bg-white/80 backdrop-blur">
        <div className="mx-auto max-w-5xl px-4 py-3 sm:px-6">
          <div className="flex items-center justify-center gap-8 text-center">
            <div>
              <p className="text-lg font-bold text-emerald-600">3.2x</p>
              <p className="text-xs text-slate-500">Más visitas</p>
            </div>
            <div className="h-8 w-px bg-slate-200" />
            <div>
              <p className="text-lg font-bold text-emerald-600">67%</p>
              <p className="text-xs text-slate-500">Más contactos</p>
            </div>
            <div className="h-8 w-px bg-slate-200" />
            <div>
              <p className="text-lg font-bold text-emerald-600">2.1x</p>
              <p className="text-xs text-slate-500">Venta más rápida</p>
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
        {/* Package Cards */}
        <div className="grid gap-6 lg:grid-cols-3">
          {AD_PACKAGES.map((pkg) => (
            <Card
              key={pkg.id}
              className={cn(
                'relative overflow-hidden transition-all duration-300 hover:shadow-lg',
                pkg.popular && 'ring-2 ring-emerald-500 shadow-lg scale-[1.02]',
                pkg.bestValue && 'ring-2 ring-amber-400'
              )}
            >
              {/* Popular / Best Value badge */}
              {pkg.popular && (
                <div className="absolute top-0 right-0 bg-emerald-600 text-white px-3 py-1 text-xs font-bold rounded-bl-lg">
                  Más popular
                </div>
              )}
              {pkg.bestValue && (
                <div className="absolute top-0 right-0 bg-amber-500 text-white px-3 py-1 text-xs font-bold rounded-bl-lg">
                  Mejor valor
                </div>
              )}

              <CardHeader className={cn('pb-4 bg-gradient-to-br', pkg.bgGradient)}>
                <div className="flex items-center gap-3">
                  <div
                    className={cn(
                      'flex h-10 w-10 items-center justify-center rounded-xl bg-white shadow-sm',
                      pkg.color
                    )}
                  >
                    <pkg.icon className="h-5 w-5" />
                  </div>
                  <div>
                    <CardTitle className="text-lg">{pkg.name}</CardTitle>
                    <p className="text-xs text-slate-500">{pkg.tagline}</p>
                  </div>
                </div>

                <div className="mt-4">
                  <div className="flex items-baseline gap-2">
                    <span className="text-3xl font-bold text-slate-900">
                      {formatCurrency(pkg.price)}
                    </span>
                    {pkg.originalPrice && (
                      <span className="text-sm text-slate-400 line-through">
                        {formatCurrency(pkg.originalPrice)}
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-slate-500 mt-0.5">
                    {pkg.duration} días · {formatCurrency(Math.round(pkg.price / pkg.duration))}/día
                  </p>
                </div>
              </CardHeader>

              <CardContent className="pt-5 space-y-5">
                {/* Metrics */}
                <div className="grid grid-cols-3 gap-2 text-center">
                  <div className="rounded-lg bg-slate-50 p-2">
                    <Eye className="h-3.5 w-3.5 text-slate-400 mx-auto mb-0.5" />
                    <p className="text-sm font-bold text-slate-900">{pkg.metrics.impressions}</p>
                    <p className="text-[10px] text-slate-400">Impresiones</p>
                  </div>
                  <div className="rounded-lg bg-slate-50 p-2">
                    <MousePointerClick className="h-3.5 w-3.5 text-slate-400 mx-auto mb-0.5" />
                    <p className="text-sm font-bold text-slate-900">{pkg.metrics.clicks}</p>
                    <p className="text-[10px] text-slate-400">Clics est.</p>
                  </div>
                  <div className="rounded-lg bg-slate-50 p-2">
                    <TrendingUp className="h-3.5 w-3.5 text-slate-400 mx-auto mb-0.5" />
                    <p className="text-sm font-bold text-slate-900">{pkg.metrics.position}</p>
                    <p className="text-[10px] text-slate-400">Posición</p>
                  </div>
                </div>

                {/* Features */}
                <ul className="space-y-2">
                  {pkg.features.map((feat) => (
                    <li key={feat} className="flex items-start gap-2 text-sm">
                      <Check className="h-4 w-4 text-emerald-500 mt-0.5 shrink-0" />
                      <span className="text-slate-700">{feat}</span>
                    </li>
                  ))}
                </ul>

                {/* CTA */}
                <Button
                  className={cn(
                    'w-full',
                    pkg.popular
                      ? 'bg-emerald-600 hover:bg-emerald-700 text-white'
                      : pkg.bestValue
                        ? 'bg-amber-500 hover:bg-amber-600 text-white'
                        : ''
                  )}
                  variant={!pkg.popular && !pkg.bestValue ? 'outline' : 'default'}
                  onClick={() => handlePurchase(pkg)}
                  disabled={isProcessing}
                >
                  {isProcessing && selectedPackage === pkg.id ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : (
                    <Rocket className="h-4 w-4 mr-2" />
                  )}
                  {isProcessing && selectedPackage === pkg.id ? 'Procesando...' : 'Activar paquete'}
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Custom campaign CTA */}
        <div className="mt-8 rounded-xl border-2 border-dashed border-slate-200 p-6 text-center">
          <BarChart3 className="h-8 w-8 text-slate-400 mx-auto mb-3" />
          <h3 className="font-semibold text-slate-900">¿Necesitas algo personalizado?</h3>
          <p className="text-sm text-slate-500 mt-1 mb-4">
            Crea una campaña a medida con presupuesto diario y segmentación avanzada.
          </p>
          <Button asChild variant="outline">
            <Link href="/dealer/publicidad/nueva">
              <Sparkles className="h-4 w-4 mr-2" />
              Crear campaña personalizada
            </Link>
          </Button>
        </div>

        {/* Trust signals */}
        <div className="mt-10 grid gap-4 sm:grid-cols-3">
          <div className="flex items-center gap-3 rounded-lg bg-slate-50 p-4">
            <Shield className="h-8 w-8 text-emerald-600 shrink-0" />
            <div>
              <p className="text-sm font-semibold text-slate-900">Pago seguro</p>
              <p className="text-xs text-slate-500">Procesado por Azul, cifrado SSL</p>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-lg bg-slate-50 p-4">
            <Timer className="h-8 w-8 text-blue-600 shrink-0" />
            <div>
              <p className="text-sm font-semibold text-slate-900">Activo al instante</p>
              <p className="text-xs text-slate-500">Tu anuncio se activa de inmediato</p>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-lg bg-slate-50 p-4">
            <BarChart3 className="h-8 w-8 text-purple-600 shrink-0" />
            <div>
              <p className="text-sm font-semibold text-slate-900">Métricas en tiempo real</p>
              <p className="text-xs text-slate-500">Dashboard con resultados al día</p>
            </div>
          </div>
        </div>

        {/* Testimonials */}
        <div className="mt-10">
          <h3 className="text-center text-lg font-semibold text-slate-900 mb-4">
            Lo que dicen nuestros anunciantes
          </h3>
          <div className="grid gap-4 sm:grid-cols-2">
            {TESTIMONIALS.map((t) => (
              <Card key={t.author} className="bg-slate-50 border-0">
                <CardContent className="pt-5">
                  <div className="flex gap-0.5 mb-2">
                    {Array.from({ length: t.rating }).map((_, i) => (
                      <Star key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />
                    ))}
                  </div>
                  <p className="text-sm text-slate-700 italic">&ldquo;{t.quote}&rdquo;</p>
                  <div className="mt-3">
                    <p className="text-sm font-semibold text-slate-900">{t.author}</p>
                    <p className="text-xs text-slate-500">{t.role}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>

        {/* ROI Calculator CTA */}
        <div className="mt-8 text-center">
          <Link
            href="/dealer/publicidad/roi"
            className="text-sm text-emerald-600 hover:text-emerald-700 font-medium underline underline-offset-4"
          >
            Calcula tu ROI estimado →
          </Link>
        </div>
      </div>
    </div>
  );
}
