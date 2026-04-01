/**
 * ShareDialog
 *
 * Rich share dialog that presents the vehicle with image + specs
 * and provides multiple sharing channels.
 *
 * Share channels:
 * - WhatsApp (with pre-built message including key specs)
 * - Facebook
 * - Twitter/X
 * - Copy link (with toast feedback)
 * - Native Web Share API (mobile)
 */

'use client';

import * as React from 'react';
import Image from 'next/image';
import { X, Copy, Check, Gauge, Fuel, Settings, MapPin } from 'lucide-react';
import { cn, formatCurrency, formatNumber } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';

// =============================================================================
// TYPES
// =============================================================================

export interface ShareVehicle {
  title: string;
  price: number;
  year: number;
  make: string;
  model: string;
  mileage?: number;
  mileageUnit?: string;
  transmission?: string;
  fuelType?: string;
  location?: string;
  imageUrl?: string;
  slug: string;
}

export interface ShareDialogProps {
  open: boolean;
  onClose: () => void;
  vehicle: ShareVehicle;
}

// =============================================================================
// HELPERS
// =============================================================================

function buildShareText(vehicle: ShareVehicle, url: string): string {
  const fuelLabel =
    vehicle.fuelType === 'gasoline'
      ? 'Gasolina'
      : vehicle.fuelType === 'diesel'
        ? 'Diesel'
        : vehicle.fuelType === 'hybrid'
          ? 'Híbrido'
          : vehicle.fuelType === 'electric'
            ? 'Eléctrico'
            : (vehicle.fuelType ?? '');

  const transLabel =
    vehicle.transmission === 'automatic'
      ? 'Automático'
      : vehicle.transmission === 'manual'
        ? 'Manual'
        : (vehicle.transmission ?? '');

  const parts = [
    `🚗 *${vehicle.title}*`,
    `💰 ${formatCurrency(vehicle.price)}`,
    vehicle.mileage != null
      ? `📍 ${formatNumber(vehicle.mileage)} ${vehicle.mileageUnit === 'Miles' || vehicle.mileageUnit === 'mi' ? 'mi' : 'km'}`
      : null,
    transLabel ? `⚙️ ${transLabel}` : null,
    fuelLabel ? `⛽ ${fuelLabel}` : null,
    `\n🔗 ${url}`,
    `\n_Publicado en OKLA — El marketplace de vehículos #1 de República Dominicana_`,
  ].filter(Boolean);

  return parts.join('\n');
}

// =============================================================================
// SHARE CHANNEL BUTTON
// =============================================================================

interface ChannelButtonProps {
  label: string;
  icon: React.ReactNode;
  onClick: () => void;
  className?: string;
}

function ChannelButton({ label, icon, onClick, className }: ChannelButtonProps) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'flex flex-col items-center gap-1.5 rounded-xl p-3 transition-all hover:scale-105 active:scale-95',
        className
      )}
    >
      <div className="flex h-12 w-12 items-center justify-center rounded-full text-white shadow-sm">
        {icon}
      </div>
      <span className="text-xs font-medium text-gray-600">{label}</span>
    </button>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function ShareDialog({ open, onClose, vehicle }: ShareDialogProps) {
  const [copied, setCopied] = React.useState(false);

  // Close on Escape
  React.useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [open, onClose]);

  // Prevent body scroll
  React.useEffect(() => {
    if (open) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [open]);

  if (!open) return null;

  const url =
    typeof window !== 'undefined'
      ? `https://okla.com.do/vehiculos/${vehicle.slug}`
      : `https://okla.com.do/vehiculos/${vehicle.slug}`;

  const shareText = buildShareText(vehicle, url);

  const fuelLabel =
    vehicle.fuelType === 'gasoline'
      ? 'Gasolina'
      : vehicle.fuelType === 'diesel'
        ? 'Diesel'
        : vehicle.fuelType === 'hybrid'
          ? 'Híbrido'
          : vehicle.fuelType === 'electric'
            ? 'Eléctrico'
            : (vehicle.fuelType ?? '');

  const transLabel =
    vehicle.transmission === 'automatic'
      ? 'Automático'
      : vehicle.transmission === 'manual'
        ? 'Manual'
        : (vehicle.transmission ?? '');

  // Share handlers
  const handleWhatsApp = () => {
    window.open(
      `https://wa.me/?text=${encodeURIComponent(shareText)}`,
      '_blank',
      'noopener,noreferrer'
    );
    onClose();
  };

  const handleFacebook = () => {
    window.open(
      `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}&quote=${encodeURIComponent(`${vehicle.title} — ${formatCurrency(vehicle.price)}`)}`,
      '_blank',
      'noopener,noreferrer,width=600,height=400'
    );
    onClose();
  };

  const handleTwitter = () => {
    const tweet = `${vehicle.title} — ${formatCurrency(vehicle.price)}\n${url}\n\n#OKLA #Vehiculos #RD`;
    window.open(
      `https://twitter.com/intent/tweet?text=${encodeURIComponent(tweet)}`,
      '_blank',
      'noopener,noreferrer,width=600,height=400'
    );
    onClose();
  };

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      toast.success('¡Enlace copiado!', {
        description: 'El enlace ha sido copiado al portapapeles.',
        duration: 2500,
      });
      setTimeout(() => setCopied(false), 2500);
    } catch {
      toast.error('No se pudo copiar el enlace');
    }
  };

  const handleNativeShare = async () => {
    if (navigator.share) {
      try {
        await navigator.share({
          title: vehicle.title,
          text: `${vehicle.title} — ${formatCurrency(vehicle.price)}`,
          url,
        });
        onClose();
      } catch {
        // User cancelled — ignore
      }
    }
  };

  const hasNativeShare = typeof navigator !== 'undefined' && typeof navigator.share === 'function';

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Panel */}
      <div
        role="dialog"
        aria-modal="true"
        aria-label={`Compartir ${vehicle.title}`}
        className={cn(
          'fixed z-50 w-full max-w-sm bg-white shadow-2xl',
          'right-0 bottom-0 left-0 rounded-t-2xl',
          'sm:top-1/2 sm:bottom-auto sm:left-1/2 sm:-translate-x-1/2 sm:-translate-y-1/2 sm:rounded-2xl'
        )}
      >
        {/* Drag handle */}
        <div className="flex justify-center pt-3 sm:hidden">
          <div className="h-1.5 w-10 rounded-full bg-gray-200" />
        </div>

        {/* Close */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 rounded-full p-1.5 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          aria-label="Cerrar"
        >
          <X className="h-4 w-4" />
        </button>

        <div className="px-5 pt-4 pb-8">
          {/* Header */}
          <h2 className="mb-4 text-base font-bold text-gray-900">Compartir publicación</h2>

          {/* Vehicle preview card */}
          <div className="mb-5 overflow-hidden rounded-xl border border-gray-100 bg-gray-50">
            {/* Image */}
            <div className="relative h-36 w-full bg-gray-200">
              {vehicle.imageUrl ? (
                <Image
                  src={vehicle.imageUrl}
                  alt={vehicle.title}
                  fill
                  className="object-cover"
                  sizes="(max-width: 640px) 100vw, 384px"
                />
              ) : (
                <div className="flex h-full items-center justify-center">
                  <span className="text-4xl">🚗</span>
                </div>
              )}
              {/* Price overlay */}
              <div className="absolute right-0 bottom-0 left-0 bg-gradient-to-t from-black/70 to-transparent px-3 pt-6 pb-2">
                <span className="text-lg font-bold text-white">
                  {formatCurrency(vehicle.price)}
                </span>
              </div>
            </div>

            {/* Specs bar */}
            <div className="px-3 py-2.5">
              <p className="mb-1.5 truncate text-sm font-semibold text-gray-900">{vehicle.title}</p>
              <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-gray-500">
                {vehicle.mileage != null && (
                  <span className="flex items-center gap-1">
                    <Gauge className="h-3 w-3" />
                    {formatNumber(vehicle.mileage)}{' '}
                    {vehicle.mileageUnit === 'Miles' || vehicle.mileageUnit === 'mi' ? 'mi' : 'km'}
                  </span>
                )}
                {transLabel && (
                  <span className="flex items-center gap-1">
                    <Settings className="h-3 w-3" />
                    {transLabel}
                  </span>
                )}
                {fuelLabel && (
                  <span className="flex items-center gap-1">
                    <Fuel className="h-3 w-3" />
                    {fuelLabel}
                  </span>
                )}
                {vehicle.location && (
                  <span className="flex items-center gap-1">
                    <MapPin className="h-3 w-3" />
                    {vehicle.location}
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* Share channels */}
          <div className="mb-5 flex justify-around">
            {/* WhatsApp */}
            <ChannelButton
              label="WhatsApp"
              onClick={handleWhatsApp}
              icon={
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-[#25D366]">
                  <svg viewBox="0 0 24 24" className="h-6 w-6 fill-white">
                    <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
                  </svg>
                </div>
              }
            />

            {/* Facebook */}
            <ChannelButton
              label="Facebook"
              onClick={handleFacebook}
              icon={
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-[#1877F2]">
                  <svg viewBox="0 0 24 24" className="h-6 w-6 fill-white">
                    <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
                  </svg>
                </div>
              }
            />

            {/* Twitter/X */}
            <ChannelButton
              label="Twitter / X"
              onClick={handleTwitter}
              icon={
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-black">
                  <svg viewBox="0 0 24 24" className="h-5 w-5 fill-white">
                    <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.748l7.73-8.835L1.254 2.25H8.08l4.258 5.631zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
                  </svg>
                </div>
              }
            />

            {/* Native share (mobile only) */}
            {hasNativeShare && (
              <ChannelButton
                label="Más opciones"
                onClick={handleNativeShare}
                icon={
                  <div className="flex h-12 w-12 items-center justify-center rounded-full bg-gray-700">
                    <svg viewBox="0 0 24 24" className="h-5 w-5 fill-white">
                      <path d="M18 16.08c-.76 0-1.44.3-1.96.77L8.91 12.7c.05-.23.09-.46.09-.7s-.04-.47-.09-.7l7.05-4.11c.54.5 1.25.81 2.04.81 1.66 0 3-1.34 3-3s-1.34-3-3-3-3 1.34-3 3c0 .24.04.47.09.7L8.04 9.81C7.5 9.31 6.79 9 6 9c-1.66 0-3 1.34-3 3s1.34 3 3 3c.79 0 1.5-.31 2.04-.81l7.12 4.16c-.05.21-.08.43-.08.65 0 1.61 1.31 2.92 2.92 2.92s2.92-1.31 2.92-2.92-1.31-2.92-2.92-2.92z" />
                    </svg>
                  </div>
                }
              />
            )}
          </div>

          {/* Copy link */}
          <div className="flex items-center gap-2 rounded-xl border border-gray-200 bg-gray-50 p-3">
            <input
              readOnly
              value={url}
              className="min-w-0 flex-1 bg-transparent text-xs text-gray-500 outline-none select-all"
              onClick={e => (e.target as HTMLInputElement).select()}
            />
            <Button
              size="sm"
              variant={copied ? 'default' : 'outline'}
              onClick={handleCopy}
              className={cn(
                'flex-shrink-0 gap-1.5 transition-all',
                copied && 'bg-emerald-600 text-white hover:bg-emerald-700'
              )}
            >
              {copied ? (
                <>
                  <Check className="h-3.5 w-3.5" />
                  Copiado
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5" />
                  Copiar
                </>
              )}
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}

export default ShareDialog;
