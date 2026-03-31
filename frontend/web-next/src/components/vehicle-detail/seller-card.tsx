/**
 * Seller Card Component
 * Displays seller information and contact buttons
 */

'use client';

import * as React from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  Phone,
  MessageCircle,
  MessageSquare,
  MapPin,
  Star,
  Check,
  Shield,
  User,
  Bot,
  CalendarDays,
  Store,
  LogIn,
  AlertCircle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/use-auth';
import type { Vehicle } from '@/types';

interface SellerCardProps {
  vehicle: Vehicle;
  className?: string;
  /** Callback to open the ChatbotService chat widget */
  onChatClick?: () => void;
}

// Extended vehicle type with seller info (to be enhanced)
interface VehicleWithSeller extends Vehicle {
  seller?: {
    id: string;
    name: string;
    type: 'seller' | 'dealer';
    avatar?: string;
    phone?: string;
    email?: string;
    city?: string;
    rating?: number;
    reviewCount?: number;
    responseRate?: number;
    responseTime?: string;
    isVerified?: boolean;
    memberSince?: string;
    listingsCount?: number;
  };
}

export function SellerCard({ vehicle, className, onChatClick: _onChatClick }: SellerCardProps) {
  const [showPhone, setShowPhone] = React.useState(false);
  const [loginPromptSource, setLoginPromptSource] = React.useState<'chat' | 'ana' | null>(null);
  const { isAuthenticated } = useAuth();
  const router = useRouter();
  const vehicleWithSeller = vehicle as VehicleWithSeller;
  const seller = vehicleWithSeller.seller;
  const isDealer = vehicle.sellerType === 'dealer';

  // Fallback seller data if not provided from API
  const sellerData = seller || {
    id: vehicle.sellerId,
    name: isDealer ? 'Dealer Verificado' : 'Vendedor Particular',
    type: vehicle.sellerType,
    phone: undefined,
    city: vehicle.location.city,
    rating: undefined,
    reviewCount: undefined,
    isVerified: false,
  };

  const handleCall = () => {
    if (sellerData.phone) {
      // SEM FIX: Track phone call as conversion BEFORE the tel: redirect.
      // Uses a small delay to ensure the event fires.
      try {
        import('@/lib/retargeting-pixels').then(({ trackContactDealer }) => {
          trackContactDealer({
            vehicleId: vehicle.id,
            title: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
            price: vehicle.price,
            dealerId: sellerData.id,
            contactMethod: 'call',
          });
        });
      } catch {
        /* silently fail */
      }
      // Small delay to let the tracking event fire
      setTimeout(() => {
        window.location.href = `tel:${sellerData.phone}`;
      }, 150);
    }
  };

  const handleWhatsApp = () => {
    const message = encodeURIComponent(
      `Hola, me interesa el ${vehicle.year} ${vehicle.make} ${vehicle.model} que vi en OKLA.`
    );
    // SEM FIX: Normalize phone number — don't blindly prepend '1' if it already has country code.
    // DR phone numbers: +1-809-XXX-XXXX, +1-829-XXX-XXXX, +1-849-XXX-XXXX
    let phone = sellerData.phone?.replace(/\D/g, '') || '';
    if (phone.length === 10 && /^(809|829|849)/.test(phone)) {
      phone = `1${phone}`; // Add country code 1 for DR numbers without it
    } else if (phone.length === 11 && phone.startsWith('1')) {
      // Already has country code — use as is
    }
    // SEM FIX: Removed 7-digit fallback that assumed 809 area code.
    // DR has 3 area codes (809/829/849) — guessing wrong sends messages to wrong numbers.
    // If phone is <10 digits, WhatsApp will still attempt to match the number.
    // Track WhatsApp as conversion
    try {
      import('@/lib/retargeting-pixels').then(({ trackContactDealer }) => {
        trackContactDealer({
          vehicleId: vehicle.id,
          title: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
          price: vehicle.price,
          dealerId: sellerData.id,
          contactMethod: 'whatsapp',
        });
      });
    } catch {
      /* silently fail */
    }
    // REMARKETING FIX: 150ms delay to let pixel events fire before window.open
    // changes tab focus. Without this, FB/Google network requests may not complete.
    setTimeout(() => {
      window.open(`https://wa.me/${phone}?text=${message}`, '_blank');
    }, 150);
  };

  const handleShowPhone = () => {
    setShowPhone(true);
    // SEM FIX: Track phone reveal as high-intent event with UTM context
    try {
      import('@/lib/ad-params').then(({ getAdParams }) => {
        const adParams = getAdParams();
        window.gtag?.('event', 'phone_reveal', {
          event_category: 'engagement',
          vehicle_id: vehicle.id,
          dealer_id: sellerData.id,
          // UTM attribution for campaign-level analysis
          utm_source: adParams?.utm_source,
          utm_medium: adParams?.utm_medium,
          utm_campaign: adParams?.utm_campaign,
          gclid: adParams?.gclid,
        });
      });
    } catch {
      /* silently fail */
    }
  };

  return (
    <div
      className={cn(
        'border-border rounded-2xl border bg-white p-5 shadow-sm dark:bg-slate-900',
        className
      )}
      data-testid="seller-info"
    >
      {/* Seller header */}
      <div className="flex items-start gap-4">
        {/* Avatar */}
        <div className="bg-muted relative flex h-14 w-14 flex-shrink-0 items-center justify-center overflow-hidden rounded-full">
          {sellerData.avatar ? (
            <Image
              src={sellerData.avatar}
              alt={sellerData.name}
              fill
              sizes="56px"
              className="object-cover"
            />
          ) : (
            <User className="text-muted-foreground h-6 w-6" />
          )}
          {sellerData.isVerified && (
            <div className="bg-primary absolute -right-0.5 -bottom-0.5 flex h-5 w-5 items-center justify-center rounded-full border-2 border-white">
              <Check className="h-3 w-3 text-white" />
            </div>
          )}
        </div>

        {/* Seller info */}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <h3 className="text-foreground truncate font-semibold">{sellerData.name}</h3>
            {isDealer && (
              <Badge variant="secondary" className="text-xs">
                Dealer
              </Badge>
            )}
          </div>

          {/* Rating */}
          {sellerData.rating && (
            <div className="mt-1 flex items-center gap-1.5">
              <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
              <span className="text-sm font-medium">{sellerData.rating.toFixed(1)}</span>
              {sellerData.reviewCount && (
                <span className="text-muted-foreground text-sm">
                  ({sellerData.reviewCount} reseñas)
                </span>
              )}
            </div>
          )}

          {/* Location */}
          <div className="text-muted-foreground mt-1 flex items-center gap-1.5 text-sm">
            <MapPin className="h-3.5 w-3.5" />
            <span>{sellerData.city || vehicle.location.city}</span>
          </div>
        </div>
      </div>

      {/* Stats */}
      {isDealer && (
        <div className="border-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div className="text-center">
            <p className="text-foreground text-lg font-semibold">
              {sellerData.responseRate || 95}%
            </p>
            <p className="text-muted-foreground text-xs">Tasa de respuesta</p>
          </div>
          <div className="text-center">
            <p className="text-foreground text-lg font-semibold">
              {sellerData.responseTime || '< 1h'}
            </p>
            <p className="text-muted-foreground text-xs">Tiempo de respuesta</p>
          </div>
        </div>
      )}

      {/* Contact Buttons */}
      <div className="mt-6 space-y-2.5">
        {/* PRIMARY CTA: Chat en vivo — requires auth, redirects to /mensajes */}
        <Button
          className="bg-primary hover:bg-primary/80 h-11 w-full gap-2 text-base font-semibold text-white shadow-md"
          onClick={() => {
            if (isAuthenticated) {
              // Redirect to messaging view with seller context
              const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;
              const emailParam = sellerData.email
                ? `&sellerEmail=${encodeURIComponent(sellerData.email)}`
                : '';
              router.push(
                `/mensajes?sellerId=${vehicle.sellerId}&vehicleId=${vehicle.id}&vehicleTitle=${encodeURIComponent(vehicleTitle)}${emailParam}`
              );
            } else {
              setLoginPromptSource('chat');
            }
          }}
        >
          <MessageSquare className="h-5 w-5" />
          Chat en vivo
        </Button>

        {/* Login prompt for unauthenticated users — chat en vivo */}
        {loginPromptSource === 'chat' && !isAuthenticated && (
          <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-950">
            <div className="flex items-start gap-3">
              <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-amber-500" />
              <div className="flex-1">
                <p className="text-sm font-semibold text-amber-800 dark:text-amber-200">
                  Inicia sesión para chatear
                </p>
                <p className="mt-1 text-xs text-amber-700 dark:text-amber-300">
                  Necesitas una cuenta para contactar al vendedor por chat. Es gratis y toma menos
                  de un minuto.
                </p>
                <div className="mt-3 flex gap-2">
                  <Button
                    size="sm"
                    className="bg-primary hover:bg-primary/80 h-8 gap-1.5 text-xs text-white"
                    onClick={() =>
                      router.push(
                        `/login?callbackUrl=${encodeURIComponent(`/vehiculos/${vehicle.slug || vehicle.id}`)}`
                      )
                    }
                  >
                    <LogIn className="h-3.5 w-3.5" />
                    Iniciar sesión
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    className="h-8 text-xs"
                    onClick={() =>
                      router.push(
                        `/registro?callbackUrl=${encodeURIComponent(`/vehiculos/${vehicle.slug || vehicle.id}`)}`
                      )
                    }
                  >
                    Crear cuenta gratis
                  </Button>
                </div>
              </div>
              <button
                onClick={() => setLoginPromptSource(null)}
                className="text-amber-400 hover:text-amber-600"
                aria-label="Cerrar"
              >
                <span className="text-lg leading-none">×</span>
              </button>
            </div>
          </div>
        )}

        {/* SECONDARY: WhatsApp */}
        <Button
          onClick={handleWhatsApp}
          variant="outline"
          className="w-full gap-2 border-[#25D366] text-[#25D366] hover:bg-[#25D366]/10 hover:text-[#1aa650]"
        >
          <MessageCircle className="h-5 w-5" />
          WhatsApp
        </Button>

        {/* TERTIARY: Phone */}
        {showPhone ? (
          <Button
            onClick={handleCall}
            variant="ghost"
            className="text-muted-foreground w-full gap-2 text-sm"
          >
            <Phone className="h-4 w-4" />
            {sellerData.phone}
          </Button>
        ) : (
          <Button
            onClick={handleShowPhone}
            variant="ghost"
            className="text-muted-foreground w-full gap-2 text-sm"
          >
            <Phone className="h-4 w-4" />
            Ver teléfono
          </Button>
        )}

        {/* Dealer-specific extras */}
        {isDealer && (
          <>
            <Button
              variant="outline"
              className="w-full gap-2 text-sm"
              onClick={() => {
                if (isAuthenticated) {
                  const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;
                  const emailParam = sellerData.email
                    ? `&sellerEmail=${encodeURIComponent(sellerData.email)}`
                    : '';
                  router.push(
                    `/mensajes?sellerId=${vehicle.sellerId}&vehicleId=${vehicle.id}&vehicleTitle=${encodeURIComponent(vehicleTitle)}&dealerChat=true${emailParam}`
                  );
                } else {
                  setLoginPromptSource('ana');
                }
              }}
            >
              <Bot className="h-4 w-4" />
              Chatear con Ana (IA)
            </Button>
            {/* Login prompt for unauthenticated users — Ana (IA) */}
            {loginPromptSource === 'ana' && !isAuthenticated && (
              <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-950">
                <div className="flex items-start gap-3">
                  <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-amber-500" />
                  <div className="flex-1">
                    <p className="text-sm font-semibold text-amber-800 dark:text-amber-200">
                      Inicia sesión para chatear con Ana
                    </p>
                    <p className="mt-1 text-xs text-amber-700 dark:text-amber-300">
                      Necesitas una cuenta para usar el asistente IA. Es gratis y toma menos de un
                      minuto.
                    </p>
                    <div className="mt-3 flex gap-2">
                      <Button
                        size="sm"
                        className="bg-primary hover:bg-primary/80 h-8 gap-1.5 text-xs text-white"
                        onClick={() =>
                          router.push(
                            `/login?callbackUrl=${encodeURIComponent(`/vehiculos/${vehicle.slug || vehicle.id}`)}`
                          )
                        }
                      >
                        <LogIn className="h-3.5 w-3.5" />
                        Iniciar sesión
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="h-8 text-xs"
                        onClick={() =>
                          router.push(
                            `/registro?callbackUrl=${encodeURIComponent(`/vehiculos/${vehicle.slug || vehicle.id}`)}`
                          )
                        }
                      >
                        Crear cuenta gratis
                      </Button>
                    </div>
                  </div>
                  <button
                    onClick={() => setLoginPromptSource(null)}
                    className="text-amber-400 hover:text-amber-600"
                    aria-label="Cerrar"
                  >
                    <span className="text-lg leading-none">×</span>
                  </button>
                </div>
              </div>
            )}
            <Button variant="outline" className="w-full gap-2 text-sm" asChild>
              <Link href={`/dealers/${sellerData.id}#agendar`}>
                <CalendarDays className="h-4 w-4" />
                Agendar visita
              </Link>
            </Button>
          </>
        )}
      </div>

      {/* Trust signals */}
      <div className="border-border mt-6 border-t pt-4">
        <div className="text-muted-foreground flex items-center gap-2 text-sm">
          <Shield className="text-primary h-4 w-4" />
          <span>Contacto verificado por OKLA</span>
        </div>
        <p className="text-muted-foreground mt-2 text-xs">
          Nunca envíes dinero por adelantado. Conoce al vendedor en un lugar público.
        </p>
      </div>

      {/* View Dealer Profile + Inventory */}
      {isDealer && (
        <div className="mt-4 flex flex-col items-center gap-1">
          <Link
            href={`/dealers/${sellerData.id}`}
            className="text-primary flex items-center gap-1 text-sm hover:underline"
          >
            <Store className="h-3.5 w-3.5" />
            Ver perfil y todo el inventario
            <span aria-hidden="true">→</span>
          </Link>
        </div>
      )}
    </div>
  );
}

export default SellerCard;
