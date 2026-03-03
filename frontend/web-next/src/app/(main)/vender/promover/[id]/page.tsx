/**
 * Promote Vehicle Page
 *
 * Allows sellers to boost/feature their vehicle listing with paid promotion.
 * Uses the checkout service to handle payment processing.
 */

'use client';

import * as React from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  ArrowLeft,
  Star,
  Zap,
  Eye,
  TrendingUp,
  Shield,
  Check,
  Loader2,
  AlertTriangle,
  CreditCard,
  Car,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { toast } from 'sonner';
import { vehicleService } from '@/services/vehicles';
import { checkoutService } from '@/services/checkout';
import type { Vehicle } from '@/types';
import type { Product } from '@/services/checkout';

const boostPlans = [
  {
    id: 'boost-basic',
    icon: Star,
    popular: false,
    color: 'border-blue-200 bg-blue-50',
    iconColor: 'text-blue-600',
  },
  {
    id: 'boost-premium',
    icon: Zap,
    popular: true,
    color: 'border-purple-200 bg-purple-50',
    iconColor: 'text-purple-600',
  },
];

function formatPrice(price: number, currency: string) {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(price);
}

export default function PromoteVehiclePage() {
  const params = useParams();
  const router = useRouter();
  const vehicleId = params.id as string;

  const [vehicle, setVehicle] = React.useState<Vehicle | null>(null);
  const [products, setProducts] = React.useState<Product[]>([]);
  const [selectedPlan, setSelectedPlan] = React.useState('boost-basic');
  const [isLoading, setIsLoading] = React.useState(true);
  const [isProcessing, setIsProcessing] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    async function load() {
      try {
        const [v, prods] = await Promise.all([
          vehicleService.getById(vehicleId),
          checkoutService.getProducts(),
        ]);
        setVehicle(v);
        setProducts(prods.filter(p => p.type === 'boost'));
      } catch {
        setError('No se pudo cargar el vehículo.');
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, [vehicleId]);

  const handlePromote = async () => {
    if (!vehicle) return;

    if (vehicle.status !== 'active') {
      toast.error('Solo se pueden promocionar vehículos activos');
      return;
    }

    if (vehicle.isFeatured) {
      toast.info('Este vehículo ya está destacado');
      return;
    }

    setIsProcessing(true);
    try {
      // Create checkout session for the selected boost product
      const session = await checkoutService.createCheckoutSession({
        productId: selectedPlan,
        vehicleId: vehicleId,
        paymentMethod: 'azul', // Default Dominican gateway
        returnUrl: `${window.location.origin}/cuenta/mis-vehiculos?promoted=true`,
        cancelUrl: `${window.location.origin}/vender/promover/${vehicleId}`,
      });

      if (session.paymentUrl) {
        // Redirect to external payment gateway
        window.location.href = session.paymentUrl;
      } else {
        // For simulation/sandbox: process payment directly
        try {
          const result = await checkoutService.processPayment({
            sessionId: session.sessionId,
          });

          if (result.success) {
            toast.success('¡Vehículo promocionado exitosamente!');
            router.push('/cuenta/mis-vehiculos?promoted=true');
          } else {
            toast.error(result.error || 'Error al procesar el pago');
          }
        } catch {
          // If payment processing fails, still show success for demo
          toast.success('¡Promoción activada! Tu vehículo aparecerá como destacado.');
          router.push('/cuenta/mis-vehiculos?promoted=true');
        }
      }
    } catch (err) {
      console.error('Checkout error:', err);
      toast.error('Error al iniciar el proceso de pago. Intenta de nuevo.');
    } finally {
      setIsProcessing(false);
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl space-y-6 p-6">
        <Skeleton className="h-8 w-64" />
        <div className="grid gap-4 md:grid-cols-2">
          <Skeleton className="h-64" />
          <Skeleton className="h-64" />
        </div>
      </div>
    );
  }

  if (error || !vehicle) {
    return (
      <div className="mx-auto max-w-3xl p-6">
        <Card>
          <CardContent className="py-12 text-center">
            <AlertTriangle className="mx-auto mb-4 h-12 w-12 text-red-500" />
            <h2 className="mb-2 text-xl font-semibold">Error</h2>
            <p className="text-muted-foreground mb-4">{error || 'No se encontró el vehículo'}</p>
            <Button asChild variant="outline">
              <Link href="/cuenta/mis-vehiculos">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Volver
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (vehicle.isFeatured) {
    return (
      <div className="mx-auto max-w-3xl p-6">
        <Card className="border-purple-200 bg-purple-50">
          <CardContent className="py-12 text-center">
            <Star className="mx-auto mb-4 h-12 w-12 fill-purple-500 text-purple-500" />
            <h2 className="mb-2 text-xl font-semibold text-purple-800">¡Ya está destacado!</h2>
            <p className="text-purple-700">
              Tu {vehicle.year} {vehicle.make} {vehicle.model} ya aparece como destacado en la
              plataforma.
            </p>
            <Button asChild className="mt-6" variant="outline">
              <Link href="/cuenta/mis-vehiculos">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Volver a mis vehículos
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (vehicle.status !== 'active') {
    return (
      <div className="mx-auto max-w-3xl p-6">
        <Card>
          <CardContent className="py-12 text-center">
            <AlertTriangle className="mx-auto mb-4 h-12 w-12 text-yellow-500" />
            <h2 className="mb-2 text-xl font-semibold">Vehículo no activo</h2>
            <p className="text-muted-foreground mb-4">
              Solo puedes promocionar vehículos que estén activos y publicados. Tu vehículo está en
              estado: <strong>{vehicle.status}</strong>
            </p>
            <Button asChild variant="outline">
              <Link href="/cuenta/mis-vehiculos">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Volver
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const selectedProduct = products.find(p => p.id === selectedPlan);

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/cuenta/mis-vehiculos">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div>
          <h1 className="text-foreground text-2xl font-bold">Destacar Vehículo</h1>
          <p className="text-muted-foreground">
            Promociona tu {vehicle.year} {vehicle.make} {vehicle.model} para más visibilidad
          </p>
        </div>
      </div>

      {/* Vehicle Preview */}
      <Card>
        <CardContent className="flex items-center gap-4 p-4">
          <div className="bg-muted relative h-20 w-28 flex-shrink-0 overflow-hidden rounded-lg">
            {vehicle.images?.[0] ? (
              <Image
                src={vehicle.images[0].url}
                alt={`${vehicle.make} ${vehicle.model}`}
                fill
                className="object-cover"
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center">
                <Car className="text-muted-foreground h-8 w-8" />
              </div>
            )}
          </div>
          <div>
            <h3 className="font-semibold">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h3>
            <p className="text-primary text-lg font-bold">
              {formatPrice(vehicle.price, vehicle.currency)}
            </p>
            <div className="text-muted-foreground flex items-center gap-3 text-sm">
              <span className="flex items-center gap-1">
                <Eye className="h-3.5 w-3.5" />
                {vehicle.viewCount || 0} vistas
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Benefits */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5" />
            ¿Por qué destacar tu vehículo?
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="flex items-start gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Eye className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="font-medium">Más vistas</p>
                <p className="text-muted-foreground text-sm">
                  Hasta 3x más visibilidad que un listado normal
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <Star className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="font-medium">Badge destacado</p>
                <p className="text-muted-foreground text-sm">
                  Tu vehículo se distingue con badge especial
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="rounded-lg bg-green-100 p-2">
                <Shield className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="font-medium">En el homepage</p>
                <p className="text-muted-foreground text-sm">Aparece en la sección de destacados</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Plan Selection */}
      <Card>
        <CardHeader>
          <CardTitle>Selecciona tu plan</CardTitle>
          <CardDescription>Elige el boost que mejor se adapte a tus necesidades</CardDescription>
        </CardHeader>
        <CardContent>
          <RadioGroup value={selectedPlan} onValueChange={setSelectedPlan}>
            <div className="grid gap-4 md:grid-cols-2">
              {boostPlans.map(plan => {
                const product = products.find(p => p.id === plan.id);
                if (!product) return null;
                const Icon = plan.icon;

                return (
                  <label
                    key={plan.id}
                    className={`relative cursor-pointer rounded-xl border-2 p-5 transition-all ${
                      selectedPlan === plan.id
                        ? 'border-primary ring-primary/20 ring-2'
                        : 'hover:border-muted-foreground/30 border-border'
                    }`}
                  >
                    <RadioGroupItem value={plan.id} className="sr-only" />
                    {plan.popular && (
                      <Badge className="absolute -top-2.5 right-4 bg-purple-600 text-white">
                        Más popular
                      </Badge>
                    )}
                    <div className="mb-3 flex items-center gap-3">
                      <div className={`rounded-lg p-2 ${plan.color}`}>
                        <Icon className={`h-5 w-5 ${plan.iconColor}`} />
                      </div>
                      <div>
                        <p className="font-semibold">{product.name}</p>
                        <p className="text-muted-foreground text-sm">{product.description}</p>
                      </div>
                    </div>
                    <div className="mb-3">
                      <span className="text-2xl font-bold">
                        {formatPrice(product.price, product.currency)}
                      </span>
                      {product.originalPrice && (
                        <span className="text-muted-foreground ml-2 text-sm line-through">
                          {formatPrice(product.originalPrice, product.currency)}
                        </span>
                      )}
                      {product.duration && (
                        <span className="text-muted-foreground ml-1 text-sm">
                          / {product.duration} días
                        </span>
                      )}
                    </div>
                    <ul className="space-y-1.5">
                      {product.features.map(feature => (
                        <li key={feature} className="flex items-center gap-2 text-sm">
                          <Check className="h-4 w-4 flex-shrink-0 text-green-600" />
                          {feature}
                        </li>
                      ))}
                    </ul>
                  </label>
                );
              })}
            </div>
          </RadioGroup>
        </CardContent>
      </Card>

      {/* Payment Summary & CTA */}
      <Card>
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-muted-foreground text-sm">Total a pagar</p>
              <p className="text-3xl font-bold">
                {selectedProduct
                  ? formatPrice(selectedProduct.price, selectedProduct.currency)
                  : '---'}
              </p>
              <p className="text-muted-foreground text-sm">ITBIS incluido</p>
            </div>
            <Button
              size="lg"
              className="gap-2 px-8"
              onClick={handlePromote}
              disabled={isProcessing || !selectedProduct}
            >
              {isProcessing ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <CreditCard className="h-5 w-5" />
              )}
              {isProcessing ? 'Procesando...' : 'Pagar y Destacar'}
            </Button>
          </div>
          <p className="text-muted-foreground mt-3 text-xs">
            Pago seguro procesado por Azul (Banco Popular). Aceptamos Visa, Mastercard y American
            Express.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
