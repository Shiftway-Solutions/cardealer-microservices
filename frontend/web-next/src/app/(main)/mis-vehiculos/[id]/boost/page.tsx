/**
 * Boost Vehicle Page
 *
 * Promote/boost a vehicle listing for more visibility
 */

'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  ArrowLeft,
  Sparkles,
  Check,
  Eye,
  TrendingUp,
  Clock,
  Zap,
  Star,
  Crown,
  Shield,
  CreditCard,
} from 'lucide-react';
import Link from 'next/link';
import { usePlatformPricing } from '@/hooks/use-platform-pricing';
import { useAuth } from '@/hooks/use-auth';
import { useVehicle } from '@/hooks/use-vehicles';
import { useCreateCampaign } from '@/hooks/use-advertising';
import { toast } from 'sonner';
import type { AdPlacementType, CampaignPricingModel } from '@/types/advertising';

function useBoostPlans() {
  const { pricing, formatPrice, isLoading } = usePlatformPricing();

  const boostPlans = [
    {
      id: 'basic',
      name: 'Boost Básico',
      duration: `${pricing.boostBasicDays} días`,
      price: pricing.boostBasicPrice,
      savings: null,
      features: ['3x más vistas', 'Aparece primero en búsquedas', 'Badge "Destacado"'],
      icon: Zap,
      color: 'blue',
      popular: false,
    },
    {
      id: 'pro',
      name: 'Boost Pro',
      duration: `${pricing.boostProDays} días`,
      price: pricing.boostProPrice,
      savings: 260,
      features: [
        '5x más vistas',
        'Aparece primero en búsquedas',
        'Badge "Destacado"',
        'Rotación en homepage',
        'Estadísticas avanzadas',
      ],
      icon: Star,
      color: 'amber',
      popular: true,
    },
    {
      id: 'premium',
      name: 'Boost Premium',
      duration: `${pricing.boostPremiumDays} días`,
      price: pricing.boostPremiumPrice,
      savings: 500,
      features: [
        '10x más vistas',
        'Aparece primero en búsquedas',
        'Badge "Premium"',
        'Rotación en homepage',
        'Estadísticas avanzadas',
        'Soporte prioritario',
        'Compartir en redes sociales',
      ],
      icon: Crown,
      color: 'purple',
      popular: false,
    },
  ];

  const testimonials = [
    {
      name: 'Carlos Martínez',
      text: 'Vendí mi Toyota en 3 días después de usar el boost. ¡Increíble!',
      vehicle: 'Toyota Corolla 2021',
    },
    {
      name: 'María García',
      text: 'Las vistas aumentaron 8 veces. Recibí muchas más llamadas.',
      vehicle: 'Honda CR-V 2020',
    },
    {
      name: 'Juan Pérez',
      text: 'Mejor inversión que pude hacer. Mi carro se vendió rápido.',
      vehicle: 'Hyundai Tucson 2022',
    },
  ];

  return { boostPlans, testimonials, formatPrice, isLoading };
}

export default function BoostVehiclePage() {
  const params = useParams();
  const router = useRouter();
  const vehicleId = params.id as string;
  const { user } = useAuth();
  const { data: vehicle } = useVehicle(vehicleId);
  const createCampaignMutation = useCreateCampaign();

  const [selectedPlan, setSelectedPlan] = useState('pro');
  const [isProcessing, setIsProcessing] = useState(false);
  const { boostPlans, testimonials } = useBoostPlans();

  const vehicleTitle = vehicle
    ? `${vehicle.make} ${vehicle.model} ${vehicle.year}`
    : 'Cargando vehículo...';

  const planConfig: Record<string, { placement: AdPlacementType; days: number }> = {
    basic: { placement: 'FeaturedSpot', days: 7 },
    pro: { placement: 'FeaturedSpot', days: 15 },
    premium: { placement: 'PremiumSpot', days: 30 },
  };

  const handlePurchase = async () => {
    if (!user?.id || !vehicleId) {
      toast.error('Debes iniciar sesión para destacar tu vehículo');
      return;
    }

    const plan = boostPlans.find(p => p.id === selectedPlan);
    const config = planConfig[selectedPlan];
    if (!plan || !config) return;

    setIsProcessing(true);
    try {
      const startDate = new Date();
      const endDate = new Date();
      endDate.setDate(endDate.getDate() + config.days);

      await createCampaignMutation.mutateAsync({
        ownerId: user.id,
        ownerType: 'Individual',
        vehicleId,
        placementType: config.placement,
        pricingModel: 'FixedMonthly' as CampaignPricingModel,
        totalBudget: plan.price,
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
      });

      toast.success('¡Campaña creada! Procede al pago para activarla.');
      router.push('/mis-vehiculos');
    } catch {
      toast.error('Error al crear la campaña. Inténtalo de nuevo.');
    } finally {
      setIsProcessing(false);
    }
  };

  const getColorClasses = (color: string, isSelected: boolean) => {
    if (!isSelected) return 'border-border hover:border-border';
    switch (color) {
      case 'blue':
        return 'border-blue-500 bg-blue-50';
      case 'amber':
        return 'border-amber-500 bg-amber-50';
      case 'purple':
        return 'border-purple-500 bg-purple-50';
      default:
        return 'border-primary bg-primary/10';
    }
  };

  const getIconColor = (color: string) => {
    switch (color) {
      case 'blue':
        return 'text-blue-500';
      case 'amber':
        return 'text-amber-500';
      case 'purple':
        return 'text-purple-500';
      default:
        return 'text-primary';
    }
  };

  const selectedPlanData = boostPlans.find(p => p.id === selectedPlan);

  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="mx-auto max-w-5xl px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex items-center gap-4">
          <Link href="/mis-vehiculos">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-foreground text-2xl font-bold">Destacar Vehículo</h1>
            <p className="text-muted-foreground">{vehicleTitle}</p>
          </div>
        </div>

        {/* Hero Stats */}
        <Card className="from-primary mb-8 border-0 bg-gradient-to-r to-teal-500 text-white">
          <CardContent className="py-8">
            <div className="mb-6 text-center">
              <Sparkles className="mx-auto mb-4 h-12 w-12" />
              <h2 className="mb-2 text-2xl font-bold">Vende hasta 10x más rápido</h2>
              <p className="text-primary-foreground">
                Los vehículos destacados reciben en promedio 10 veces más vistas
              </p>
            </div>
            <div className="mx-auto grid max-w-lg grid-cols-3 gap-4">
              <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <Eye className="h-5 w-5" />
                  <span className="text-2xl font-bold">10x</span>
                </div>
                <p className="text-primary-foreground text-sm">Más vistas</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <TrendingUp className="h-5 w-5" />
                  <span className="text-2xl font-bold">85%</span>
                </div>
                <p className="text-primary-foreground text-sm">Venden más rápido</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <Clock className="h-5 w-5" />
                  <span className="text-2xl font-bold">7 días</span>
                </div>
                <p className="text-primary-foreground text-sm">Tiempo promedio</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Plans */}
          <div className="space-y-4 lg:col-span-2">
            <h3 className="text-foreground text-lg font-semibold">Selecciona tu plan</h3>

            {boostPlans.map(plan => {
              const Icon = plan.icon;
              const isSelected = selectedPlan === plan.id;

              return (
                <Card
                  key={plan.id}
                  className={`cursor-pointer transition-all ${getColorClasses(plan.color, isSelected)}`}
                  onClick={() => setSelectedPlan(plan.id)}
                >
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-4">
                        <div
                          className={`rounded-xl p-3 ${
                            plan.color === 'blue'
                              ? 'bg-blue-100'
                              : plan.color === 'amber'
                                ? 'bg-amber-100'
                                : 'bg-purple-100'
                          }`}
                        >
                          <Icon className={`h-6 w-6 ${getIconColor(plan.color)}`} />
                        </div>
                        <div>
                          <div className="flex items-center gap-2">
                            <h4 className="text-foreground font-semibold">{plan.name}</h4>
                            {plan.popular && <Badge className="bg-amber-500">Más Popular</Badge>}
                          </div>
                          <p className="text-muted-foreground mb-3 text-sm">{plan.duration}</p>
                          <ul className="space-y-1">
                            {plan.features.map((feature, i) => (
                              <li
                                key={i}
                                className="text-muted-foreground flex items-center gap-2 text-sm"
                              >
                                <Check className={`h-4 w-4 ${getIconColor(plan.color)}`} />
                                {feature}
                              </li>
                            ))}
                          </ul>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="text-foreground text-2xl font-bold">
                          RD$ {plan.price.toLocaleString()}
                        </p>
                        {plan.savings && (
                          <p className="text-primary text-sm">Ahorras RD$ {plan.savings}</p>
                        )}
                        <div
                          className={`mt-2 ml-auto h-5 w-5 rounded-full border-2 ${
                            isSelected
                              ? plan.color === 'blue'
                                ? 'border-blue-500 bg-blue-500'
                                : plan.color === 'amber'
                                  ? 'border-amber-500 bg-amber-500'
                                  : 'border-purple-500 bg-purple-500'
                              : 'border-border'
                          } flex items-center justify-center`}
                        >
                          {isSelected && <Check className="h-3 w-3 text-white" />}
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {/* Checkout Sidebar */}
          <div className="space-y-6">
            <Card className="sticky top-6">
              <CardHeader>
                <CardTitle>Resumen de Compra</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="border-border flex justify-between border-b py-2">
                  <span className="text-muted-foreground">Plan</span>
                  <span className="font-medium">{selectedPlanData?.name}</span>
                </div>
                <div className="border-border flex justify-between border-b py-2">
                  <span className="text-muted-foreground">Duración</span>
                  <span className="font-medium">{selectedPlanData?.duration}</span>
                </div>
                <div className="border-border flex justify-between border-b py-2">
                  <span className="text-muted-foreground">Vehículo</span>
                  <span className="text-right text-sm font-medium">{vehicleTitle}</span>
                </div>
                <div className="flex justify-between py-3 text-lg">
                  <span className="font-semibold">Total</span>
                  <span className="text-primary font-bold">
                    RD$ {selectedPlanData?.price.toLocaleString()}
                  </span>
                </div>

                <Button
                  className="bg-primary hover:bg-primary/90 w-full"
                  size="lg"
                  onClick={handlePurchase}
                  disabled={isProcessing}
                >
                  {isProcessing ? (
                    <>
                      <div className="mr-2 h-4 w-4 animate-spin rounded-full border-b-2 border-white" />
                      Procesando...
                    </>
                  ) : (
                    <>
                      <CreditCard className="mr-2 h-4 w-4" />
                      Pagar Ahora
                    </>
                  )}
                </Button>

                <div className="text-muted-foreground flex items-center justify-center gap-2 text-xs">
                  <Shield className="h-4 w-4" />
                  Pago seguro con SSL
                </div>
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Testimonials */}
        <div className="mt-12">
          <h3 className="text-foreground mb-6 text-center text-lg font-semibold">
            Lo que dicen nuestros vendedores
          </h3>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            {testimonials.map((testimonial, i) => (
              <Card key={i}>
                <CardContent className="p-6">
                  <div className="mb-3 flex gap-1">
                    {[...Array(5)].map((_, j) => (
                      <Star key={j} className="h-4 w-4 fill-amber-400 text-amber-400" />
                    ))}
                  </div>
                  <p className="text-muted-foreground mb-4 text-sm">&ldquo;{testimonial.text}&rdquo;</p>
                  <div>
                    <p className="text-sm font-medium">{testimonial.name}</p>
                    <p className="text-muted-foreground text-xs">{testimonial.vehicle}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>

        {/* FAQ */}
        <Card className="mt-12">
          <CardHeader>
            <CardTitle>Preguntas Frecuentes</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {[
              {
                q: '¿Cuándo empieza a funcionar el boost?',
                a: 'El boost se activa inmediatamente después del pago y dura por el período seleccionado.',
              },
              {
                q: '¿Puedo cancelar el boost?',
                a: 'El boost no es reembolsable una vez activado, pero puedes contactar soporte si tienes problemas.',
              },
              {
                q: '¿Qué pasa si vendo el vehículo antes?',
                a: 'Si vendes tu vehículo, puedes contactarnos para transferir el boost restante a otra publicación.',
              },
            ].map((faq, i) => (
              <div key={i} className="bg-muted/50 rounded-lg p-4">
                <p className="text-foreground mb-1 font-medium">{faq.q}</p>
                <p className="text-muted-foreground text-sm">{faq.a}</p>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
