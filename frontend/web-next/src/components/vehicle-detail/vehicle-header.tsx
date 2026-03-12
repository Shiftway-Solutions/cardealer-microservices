/**
 * Vehicle Header Component
 * Displays vehicle title, price, badges, and actions.
 *
 * Save (favorite) behaviour:
 *   - Authenticated → toggles favorite via API (optimistic update)
 *   - Unauthenticated → opens AuthPromptDialog so user can register/login
 *
 * Share behaviour:
 *   - Opens a rich ShareDialog with vehicle image + specs + social channels
 */

'use client';

import * as React from 'react';
import { Heart, Share2, MapPin, Calendar, Gauge, Shield, Clock, Flag, FileText } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DealRatingBadge } from '@/components/ui/deal-rating-badge';
import { ScoreBadge } from '@/components/okla-score/score-badge';
import { AuthPromptDialog } from '@/components/ui/auth-prompt-dialog';
import { ShareDialog } from '@/components/ui/share-dialog';
import { ReportVehicleModal } from './report-vehicle-modal';
import { OdometerAlert } from './odometer-alert';
import { BrokenImagesAlert } from './broken-images-alert';
import { useAuth } from '@/hooks/use-auth';
import { useFavoriteStatus } from '@/hooks/use-favorites';
import { cn, formatCurrency, formatNumber, getAlternateCurrencyDisplay } from '@/lib/utils';
import type { Vehicle } from '@/types';

interface VehicleHeaderProps {
  vehicle: Vehicle;
  /** Callback to open the OKLA Score report purchase modal */
  onPurchaseReportClick?: () => void;
  className?: string;
}

export function VehicleHeader({ vehicle, className, onPurchaseReportClick }: VehicleHeaderProps) {
  const { isAuthenticated } = useAuth();
  const { isFavorite, toggle, isLoading: isFavoriteLoading } = useFavoriteStatus(vehicle.id);

  const [showAuthPrompt, setShowAuthPrompt] = React.useState(false);
  const [showShare, setShowShare] = React.useState(false);
  const [showReport, setShowReport] = React.useState(false);

  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  /** Save/unsave — gate behind auth check */
  const handleFavorite = () => {
    if (!isAuthenticated) {
      setShowAuthPrompt(true);
      return;
    }
    toggle();
  };

  /** Open rich share dialog */
  const handleShare = () => {
    setShowShare(true);
  };

  return (
    <>
      <div
        className={cn(
          'border-border rounded-2xl border bg-white p-5 shadow-sm dark:bg-slate-900',
          className
        )}
      >
        {/* Badges */}
        <div className="mb-3 flex flex-wrap gap-2">
          {vehicle.condition === 'new' && (
            <Badge className="bg-blue-100 text-blue-700 hover:bg-blue-100">Nuevo</Badge>
          )}
          {vehicle.condition === 'used' && (
            <Badge className="bg-muted text-foreground hover:bg-muted">Usado</Badge>
          )}
          {vehicle.condition === 'certified' && (
            <Badge className="gap-1 bg-purple-100 text-purple-700 hover:bg-purple-100">
              <Shield className="h-3 w-3" />
              Certificado
            </Badge>
          )}
          {vehicle.isFeatured && (
            <Badge className="bg-amber-100 text-amber-700 hover:bg-amber-100">Destacado</Badge>
          )}
        </div>

        {/* Odometer Rollback Alert — visible to buyers */}
        {vehicle.odometerRollbackDetected && (
          <OdometerAlert
            declaredMileage={vehicle.mileage}
            historicalMileage={vehicle.historicalMileage}
          />
        )}

        {/* Broken Images Alert — visible to buyers when CDN images are unreachable */}
        {vehicle.hasBrokenImages && <BrokenImagesAlert />}

        {/* Title */}
        <h1 className="text-foreground text-2xl font-bold lg:text-3xl">{title}</h1>

        {/* Trim */}
        {vehicle.trim && <p className="text-muted-foreground mt-1 text-lg">{vehicle.trim}</p>}

        {/* Quick specs */}
        <div className="text-muted-foreground mt-4 flex flex-wrap items-center gap-4 text-sm">
          <div className="flex items-center gap-1.5">
            <Calendar className="text-muted-foreground h-4 w-4" />
            <span>{vehicle.year}</span>
          </div>
          {vehicle.mileage !== undefined && (
            <div className="flex items-center gap-1.5">
              <Gauge className="text-muted-foreground h-4 w-4" />
              <span>{formatNumber(vehicle.mileage)} km</span>
            </div>
          )}
          <div className="flex items-center gap-1.5">
            <MapPin className="text-muted-foreground h-4 w-4" />
            <span>{vehicle.location.city}</span>
          </div>
        </div>

        {/* Price Section */}
        <div className="border-border mt-6 border-t pt-6" data-testid="vehicle-price">
          {/* Deal Rating + OKLA Score */}
          <div className="mb-3 flex flex-wrap items-center gap-3">
            {vehicle.dealRating && <DealRatingBadge rating={vehicle.dealRating} size="lg" />}
            {vehicle.oklaScore != null && vehicle.oklaScore > 0 && (
              <ScoreBadge score={vehicle.oklaScore} variant="full" />
            )}
            {vehicle.oklaScore != null && vehicle.oklaScore > 0 && (
              <button
                onClick={onPurchaseReportClick}
                className="inline-flex items-center gap-1.5 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700 transition-colors hover:bg-emerald-100 dark:border-emerald-800 dark:bg-emerald-950 dark:text-emerald-300"
              >
                <FileText className="h-3.5 w-3.5" />
                Ver informe completo OKLA Score™ — RD$420
              </button>
            )}
          </div>

          {/* Price */}
          <div className="flex items-baseline gap-3">
            <span className="text-primary text-3xl font-extrabold lg:text-4xl">
              {formatCurrency(vehicle.price, { currency: vehicle.currency || 'DOP' })}
            </span>
            {vehicle.originalPrice && vehicle.originalPrice > vehicle.price && (
              <span className="text-muted-foreground text-lg line-through">
                {formatCurrency(vehicle.originalPrice, { currency: vehicle.currency || 'DOP' })}
              </span>
            )}
          </div>
          <p className="text-muted-foreground mt-1 text-sm">
            ≈ {getAlternateCurrencyDisplay(vehicle.price, (vehicle.currency as 'DOP' | 'USD') || 'DOP').text}
          </p>

          {/* Monthly Payment Estimate */}
          <p className="text-muted-foreground mt-2 text-sm">
            Estimado:{' '}
            <span className="font-medium">
              {formatCurrency(Math.round(vehicle.price / 60))}/mes
            </span>
            <span className="ml-1 text-xs">(60 meses)</span>
            <a
              href={`/herramientas/calculadora-financiamiento?precio=${vehicle.price}`}
              className="text-primary ml-2 text-xs font-medium hover:underline"
            >
              Calcular cuota real →
            </a>
          </p>
        </div>

        {/* Actions */}
        <div className="mt-6 flex gap-3">
          <Button
            variant={isFavorite ? 'default' : 'outline'}
            onClick={handleFavorite}
            disabled={isFavoriteLoading}
            aria-label={isFavorite ? 'Quitar de favoritos' : 'Guardar en favoritos'}
            className={cn(
              'flex-1 gap-2 transition-colors',
              isFavorite && 'border-rose-500 bg-rose-500 hover:bg-rose-600'
            )}
          >
            <Heart className={cn('h-5 w-5', isFavorite && 'fill-current')} />
            {isFavorite ? 'Guardado' : 'Guardar'}
          </Button>

          <Button
            variant="outline"
            onClick={handleShare}
            aria-label="Compartir publicación"
            className="gap-2"
          >
            <Share2 className="h-5 w-5" />
            Compartir
          </Button>
        </div>

        {/* Report — visible to everyone, no auth required */}
        <button
          onClick={() => setShowReport(true)}
          className="mt-3 flex w-full items-center justify-center gap-1.5 rounded-lg py-2 text-xs font-medium text-slate-400 transition-colors hover:bg-red-50 hover:text-red-600 dark:text-slate-500 dark:hover:bg-red-900/20 dark:hover:text-red-400"
          aria-label="Reportar este vehículo"
        >
          <Flag className="h-3.5 w-3.5" />
          Reportar este vehículo
        </button>

        {/* Published Date */}
        <div className="text-muted-foreground mt-4 flex items-center justify-center gap-1.5 text-xs">
          <Clock className="h-3 w-3" />
          <span>Publicado el {new Date(vehicle.createdAt).toLocaleDateString('es-DO')}</span>
        </div>
      </div>

      {/* Auth prompt — shown when unauthenticated user tries to save */}
      <AuthPromptDialog
        open={showAuthPrompt}
        onClose={() => setShowAuthPrompt(false)}
        action="guardar este vehículo"
        vehicle={{
          title,
          imageUrl: vehicle.images?.[0]?.url,
        }}
      />

      {/* Rich share dialog */}
      <ShareDialog
        open={showShare}
        onClose={() => setShowShare(false)}
        vehicle={{
          title,
          price: vehicle.price,
          year: vehicle.year,
          make: vehicle.make,
          model: vehicle.model,
          mileage: vehicle.mileage,
          transmission: vehicle.transmission,
          fuelType: vehicle.fuelType,
          location: vehicle.location?.city,
          imageUrl: vehicle.images?.[0]?.url,
          slug: vehicle.slug,
        }}
      />

      {/* Report vehicle modal — no auth required */}
      <ReportVehicleModal
        open={showReport}
        onClose={() => setShowReport(false)}
        vehicleId={vehicle.id}
        vehicleTitle={title}
      />
    </>
  );
}

export default VehicleHeader;
