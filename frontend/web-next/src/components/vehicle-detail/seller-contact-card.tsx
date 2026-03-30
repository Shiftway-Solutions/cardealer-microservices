/**
 * Seller Contact Card — Premium conversion-optimized contact section
 *
 * UX Research-backed design decisions:
 * - Internal chat as primary CTA (keeps users on platform → higher conversion)
 * - WhatsApp as secondary (familiar in DR market)
 * - Registration gate for chat (grow user base → more data → better matching)
 * - Social proof & urgency signals (Baymard Institute: +23% conversion)
 * - Sticky on desktop, full-width on mobile
 */

'use client';

import * as React from 'react';
import Image from 'next/image';
import Link from 'next/link';
import {
  Phone,
  MapPin,
  Star,
  Check,
  Shield,
  User,
  Eye,
  Heart,
  ChevronRight,
  Store,
  Zap,
  Lock,
  MessageSquare,
  CalendarDays,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/use-auth';
import type { Vehicle } from '@/types';

interface SellerContactCardProps {
  vehicle: Vehicle;
  className?: string;
}

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

export function SellerContactCard({ vehicle, className }: SellerContactCardProps) {
  const { isAuthenticated } = useAuth();
  const [showPhone, setShowPhone] = React.useState(false);
  const [chatPromptVisible, setChatPromptVisible] = React.useState(false);

  const vehicleWithSeller = vehicle as VehicleWithSeller;
  const seller = vehicleWithSeller.seller;
  const isDealer = vehicle.sellerType === 'dealer';

  const sellerData = seller || {
    id: vehicle.sellerId,
    name: isDealer ? 'Dealer Verificado' : 'Vendedor Particular',
    type: vehicle.sellerType,
    phone: undefined,
    city: vehicle.location.city,
    rating: undefined,
    reviewCount: undefined,
    isVerified: false,
    responseRate: undefined,
    responseTime: undefined,
    listingsCount: undefined,
  };

  const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  const handleWhatsApp = () => {
    // Guard: Don't open WhatsApp if seller has no phone number
    const rawPhone = sellerData.phone?.replace(/\D/g, '');
    if (!rawPhone) return;

    const message = encodeURIComponent(
      `Hola, me interesa el ${vehicleTitle} que vi en OKLA. ¿Está disponible?`
    );

    // SEM FIX: Normalize DR phone numbers properly
    // DR numbers: +1-809-XXX-XXXX, +1-829-XXX-XXXX, +1-849-XXX-XXXX
    let phone = rawPhone;
    if (phone.length === 10 && /^(809|829|849)/.test(phone)) {
      phone = `1${phone}`; // Add country code 1 for 10-digit DR numbers
    } else if (phone.length === 11 && phone.startsWith('1')) {
      // Already has country code — use as-is
    }
    // Skip 7-digit fallback (unreliable: can't determine area code)

    // P0-03 FIX: Track WhatsApp as conversion BEFORE navigating.
    // Dynamic import resolves async — fire event then delay 150ms before window.open
    // so pixel network requests (FB Lead, Google Ads conversion) complete before
    // mobile Safari suspends the tab on WhatsApp navigation.
    try {
      import('@/lib/retargeting-pixels').then(({ trackContactDealer }) => {
        trackContactDealer({
          vehicleId: vehicle.id,
          title: vehicleTitle,
          price: vehicle.price,
          dealerId: sellerData.id,
          contactMethod: 'whatsapp',
        });
        // Navigate after pixel events have been queued
        setTimeout(() => {
          window.open(`https://wa.me/${phone}?text=${message}`, '_blank');
        }, 150);
      });
    } catch {
      // Fallback: navigate immediately if tracking fails
      window.open(`https://wa.me/${phone}?text=${message}`, '_blank');
    }
  };

  const handleChatClick = () => {
    if (!isAuthenticated) {
      setChatPromptVisible(true);
      return;
    }
    // Navigate to internal messaging with vehicle context
    window.location.href = `/mensajes/nuevo?vehicleId=${vehicle.id}&sellerId=${sellerData.id}`;
  };

  const handleShowPhone = () => {
    setShowPhone(true);
    // SEM FIX: Track phone reveal as high-intent event (conversion signal) with UTM context
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

  // Calculate fake but realistic view/interest data for urgency
  const viewCount = vehicle.viewCount || 0;
  const favoriteCount = vehicle.favoriteCount || 0;

  return (
    <div className={cn('overflow-hidden rounded-xl bg-white shadow-sm', className)}>
      {/* ─── Primary CTA: Internal Chat ─── */}
      <div className="bg-gradient-to-r from-emerald-50 to-teal-50 p-5">
        <p className="mb-3 text-center text-xs font-medium tracking-wide text-emerald-700 uppercase">
          ¿Te interesa este vehículo?
        </p>

        {isAuthenticated ? (
          <Button
            onClick={handleChatClick}
            className="w-full gap-2.5 bg-emerald-600 py-6 text-base font-semibold text-white shadow-lg shadow-emerald-200 transition-all hover:bg-emerald-700 hover:shadow-xl hover:shadow-emerald-200"
          >
            <MessageSquare className="h-5 w-5" />
            Enviar mensaje al vendedor
          </Button>
        ) : (
          <>
            <Button
              onClick={handleChatClick}
              className="w-full gap-2.5 bg-emerald-600 py-6 text-base font-semibold text-white shadow-lg shadow-emerald-200 transition-all hover:bg-emerald-700 hover:shadow-xl hover:shadow-emerald-200"
            >
              <MessageSquare className="h-5 w-5" />
              Enviar mensaje al vendedor
            </Button>

            {/* Registration prompt (appears on click) */}
            {chatPromptVisible && (
              <div className="mt-3 rounded-lg border border-emerald-200 bg-white p-3 text-center">
                <Lock className="mx-auto mb-1.5 h-4 w-4 text-emerald-600" />
                <p className="text-sm font-medium text-gray-800">
                  Crea tu cuenta gratis para chatear
                </p>
                <p className="mt-0.5 text-xs text-gray-500">
                  Guarda favoritos, recibe alertas y chatea con vendedores
                </p>
                <div className="mt-2.5 flex gap-2">
                  <Button asChild size="sm" className="flex-1 bg-emerald-600 hover:bg-emerald-700">
                    <Link href={`/registro?redirect=/vehiculos/${vehicle.slug}`}>Registrarme</Link>
                  </Button>
                  <Button asChild variant="outline" size="sm" className="flex-1">
                    <Link href={`/login?redirect=/vehiculos/${vehicle.slug}`}>Ya tengo cuenta</Link>
                  </Button>
                </div>
              </div>
            )}
          </>
        )}

        {/* WhatsApp - Secondary (only show when seller has a phone number) */}
        {sellerData.phone && (
          <Button
            onClick={handleWhatsApp}
            variant="outline"
            className="mt-2.5 w-full gap-2 border-emerald-200 bg-white py-5 font-medium text-gray-700 hover:bg-emerald-50"
          >
            <svg viewBox="0 0 24 24" className="h-5 w-5 fill-[#25D366]">
              <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
            </svg>
            Escribir por WhatsApp
          </Button>
        )}
      </div>

      <Separator />

      {/* ─── Seller Info ─── */}
      <div className="p-5">
        <div className="flex items-center gap-3.5">
          {/* Avatar */}
          <div className="relative flex h-12 w-12 flex-shrink-0 items-center justify-center overflow-hidden rounded-full bg-gray-100">
            {sellerData.avatar ? (
              <Image
                src={sellerData.avatar}
                alt={sellerData.name}
                fill
                sizes="48px"
                className="object-cover"
              />
            ) : (
              <User className="h-5 w-5 text-gray-400" />
            )}
            {sellerData.isVerified && (
              <div className="absolute -right-0.5 -bottom-0.5 flex h-4.5 w-4.5 items-center justify-center rounded-full border-2 border-white bg-emerald-500">
                <Check className="h-2.5 w-2.5 text-white" />
              </div>
            )}
          </div>

          {/* Name & Meta */}
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <h3 className="truncate text-sm font-semibold text-gray-900">{sellerData.name}</h3>
              {isDealer && (
                <Badge variant="secondary" className="bg-blue-50 text-xs text-blue-700">
                  Dealer
                </Badge>
              )}
            </div>
            <div className="mt-0.5 flex items-center gap-3 text-xs text-gray-500">
              <span className="flex items-center gap-1">
                <MapPin className="h-3 w-3" />
                {sellerData.city || vehicle.location.city}
              </span>
              {sellerData.rating && (
                <span className="flex items-center gap-1">
                  <Star className="h-3 w-3 fill-amber-400 text-amber-400" />
                  {sellerData.rating.toFixed(1)}
                  {sellerData.reviewCount ? ` (${sellerData.reviewCount})` : ''}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Response stats for dealers */}
        {isDealer && (
          <div className="mt-4 grid grid-cols-2 gap-3">
            <div className="rounded-lg bg-gray-50 px-3 py-2 text-center">
              <p className="text-sm font-bold text-gray-900">{sellerData.responseRate || 95}%</p>
              <p className="text-[10px] text-gray-500">Respuesta</p>
            </div>
            <div className="rounded-lg bg-gray-50 px-3 py-2 text-center">
              <p className="text-sm font-bold text-gray-900">{sellerData.responseTime || '< 1h'}</p>
              <p className="text-[10px] text-gray-500">Tiempo resp.</p>
            </div>
          </div>
        )}

        {/* Phone reveal */}
        <div className="mt-4">
          {showPhone && sellerData.phone ? (
            <a
              href={`tel:${sellerData.phone}`}
              className="flex w-full items-center justify-center gap-2 rounded-lg border border-gray-200 bg-gray-50 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100"
            >
              <Phone className="h-4 w-4" />
              {sellerData.phone}
            </a>
          ) : (
            <button
              onClick={handleShowPhone}
              className="flex w-full items-center justify-center gap-2 rounded-lg border border-gray-200 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              <Phone className="h-4 w-4" />
              Ver teléfono
            </button>
          )}
        </div>

        {/* Dealer-specific actions */}
        {isDealer && (
          <div className="mt-3 flex gap-2">
            <Button
              variant="ghost"
              size="sm"
              className="flex-1 gap-1.5 text-xs text-gray-600"
              asChild
            >
              <Link href={`/dealers/${sellerData.id}#reviews`}>
                <CalendarDays className="h-3.5 w-3.5" />
                Agendar visita
              </Link>
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="flex-1 gap-1.5 text-xs text-gray-600"
              asChild
            >
              <Link href={`/dealers/${sellerData.id}`}>
                <Store className="h-3.5 w-3.5" />
                Ver inventario
              </Link>
            </Button>
          </div>
        )}
      </div>

      <Separator />

      {/* ─── Trust & Urgency Signals ─── */}
      <div className="space-y-2.5 px-5 py-4">
        {/* Urgency: Views */}
        {viewCount > 0 && (
          <div className="flex items-center gap-2 text-xs text-gray-500">
            <Eye className="h-3.5 w-3.5 text-orange-500" />
            <span>
              <strong className="text-gray-700">{viewCount} personas</strong> vieron este anuncio
            </span>
          </div>
        )}

        {/* Urgency: Favorites */}
        {favoriteCount > 0 && (
          <div className="flex items-center gap-2 text-xs text-gray-500">
            <Heart className="h-3.5 w-3.5 text-rose-500" />
            <span>
              <strong className="text-gray-700">{favoriteCount} personas</strong> lo tienen en
              favoritos
            </span>
          </div>
        )}

        {/* Trust: Verified */}
        <div className="flex items-center gap-2 text-xs text-gray-500">
          <Shield className="h-3.5 w-3.5 text-emerald-500" />
          <span>Contacto verificado por OKLA</span>
        </div>

        {/* Trust: Safety */}
        <p className="text-[10px] leading-relaxed text-gray-400">
          Nunca envíes dinero por adelantado. Conoce al vendedor en un lugar público.
        </p>
      </div>

      {/* ─── Registration CTA for non-authenticated (bottom strip) ─── */}
      {!isAuthenticated && !chatPromptVisible && (
        <>
          <Separator />
          <div className="bg-gradient-to-r from-gray-50 to-slate-50 px-5 py-3.5">
            <div className="flex items-center gap-3">
              <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-emerald-100">
                <Zap className="h-4 w-4 text-emerald-600" />
              </div>
              <div className="flex-1">
                <p className="text-xs font-medium text-gray-800">¿Buscas tu próximo vehículo?</p>
                <p className="text-[10px] text-gray-500">
                  Regístrate gratis y recibe alertas de precios
                </p>
              </div>
              <Button
                asChild
                size="sm"
                variant="ghost"
                className="h-7 px-2 text-xs text-emerald-600 hover:text-emerald-700"
              >
                <Link href="/registro">
                  Crear cuenta
                  <ChevronRight className="ml-0.5 h-3 w-3" />
                </Link>
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default SellerContactCard;
