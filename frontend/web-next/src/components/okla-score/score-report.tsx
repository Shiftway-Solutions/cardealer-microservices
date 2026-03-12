'use client';

import { cn } from '@/lib/utils';
import type { OklaScoreReport } from '@/types/okla-score';
import { ScoreGauge } from './score-gauge';
import { ScoreBadge } from './score-badge';
import { DimensionBreakdown, DimensionCard } from './dimension-breakdown';
import { PriceAnalysisCard } from './price-analysis-card';
import { ScoreAlerts } from './score-alerts';
import { Car, Calendar, Hash, Gauge, Fuel, Settings, Lock } from 'lucide-react';

// =============================================================================
// OKLA Score™ Full Report — Complete score report view
// =============================================================================

interface ScoreReportProps {
  report: OklaScoreReport;
  layout?: 'full' | 'compact';
  /** Whether to show the full detailed breakdown (paid) or just the summary (free) */
  showFullReport?: boolean;
  /** Vehicle ID for the paywall CTA link */
  vehicleId?: string;
  /** Vehicle title for the purchase modal */
  vehicleTitle?: string;
  /** Callback to open the purchase modal (injected by parent) */
  onPurchaseClick?: () => void;
  className?: string;
}

export function ScoreReport({
  report,
  layout = 'full',
  showFullReport = true,
  vehicleId,
  vehicleTitle,
  onPurchaseClick,
  className,
}: ScoreReportProps) {
  const vin = report.vinDecode;

  if (layout === 'compact') {
    return (
      <div className={cn('space-y-4', className)}>
        <div className="flex items-center gap-4">
          <ScoreGauge score={report.score} size="md" showLabel={false} />
          <div>
            <ScoreBadge score={report.score} variant="full" />
            <p className="text-muted-foreground mt-2 text-sm">
              {vin.year} {vin.make} {vin.model}
            </p>
          </div>
        </div>
        {report.alerts.length > 0 && <ScoreAlerts alerts={report.alerts} />}
        {showFullReport ? (
          <DimensionBreakdown dimensions={report.dimensions} />
        ) : (
          <PaywallOverlay
            vehicleId={vehicleId}
            section="Desglose de las 7 Dimensiones"
            onPurchaseClick={onPurchaseClick}
          />
        )}
      </div>
    );
  }

  return (
    <div className={cn('space-y-6', className)}>
      {/* Alerts first */}
      {report.alerts.length > 0 && <ScoreAlerts alerts={report.alerts} />}

      {/* Header: Score + Vehicle info */}
      <div className="bg-card flex flex-col items-center gap-6 rounded-2xl border p-6 shadow-sm md:flex-row">
        <ScoreGauge score={report.score} size="xl" />
        <div className="flex-1 text-center md:text-left">
          <h2 className="text-2xl font-bold">
            {vin.year} {vin.make} {vin.model} {vin.trim || ''}
          </h2>
          <p className="text-muted-foreground mt-1 font-mono text-sm">VIN: {report.vin}</p>

          {/* Vehicle specs grid */}
          <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
            <VehicleSpec icon={Car} label="Tipo" value={vin.bodyType || vin.vehicleType || 'N/A'} />
            <VehicleSpec icon={Gauge} label="Motor" value={vin.engineType || 'N/A'} />
            <VehicleSpec icon={Settings} label="Transmisión" value={vin.transmission || 'N/A'} />
            <VehicleSpec icon={Fuel} label="Combustible" value={vin.fuelType || 'N/A'} />
            <VehicleSpec
              icon={Hash}
              label="Cilindros"
              value={vin.engineCylinders ? `${vin.engineCylinders}` : 'N/A'}
            />
            <VehicleSpec icon={Calendar} label="País Fab." value={vin.plantCountry || 'N/A'} />
          </div>
        </div>
      </div>

      {/* Price analysis */}
      {showFullReport ? (
        <PriceAnalysisCard analysis={report.priceAnalysis} />
      ) : (
        <PaywallOverlay
          vehicleId={vehicleId}
          section="Análisis de Precio vs. Mercado"
          onPurchaseClick={onPurchaseClick}
        />
      )}

      {/* Dimension breakdown */}
      {showFullReport ? (
        <div>
          <h3 className="mb-4 text-lg font-bold">📊 Desglose por Dimensión</h3>
          <div className="grid gap-3 md:grid-cols-2">
            {report.dimensions.map(d => (
              <DimensionCard key={d.dimension} dimension={d} />
            ))}
          </div>
        </div>
      ) : (
        <PaywallOverlay
          vehicleId={vehicleId}
          section="Desglose de las 7 Dimensiones"
          onPurchaseClick={onPurchaseClick}
        />
      )}

      {/* Safety / Recalls */}
      {report.recalls && report.recalls.length > 0 && (
        <div>
          <h3 className="mb-3 text-lg font-bold">🛡️ Recalls Activos</h3>
          <div className="space-y-2">
            {report.recalls.map(r => (
              <div
                key={r.campaignNumber}
                className="rounded-lg border border-amber-200 bg-amber-50 p-3"
              >
                <div className="flex items-center justify-between">
                  <span className="text-sm font-semibold text-amber-800">{r.component}</span>
                  <span className="text-muted-foreground font-mono text-xs">
                    {r.campaignNumber}
                  </span>
                </div>
                <p className="mt-1 text-xs text-amber-700">{r.summary}</p>
                <p className="text-muted-foreground mt-1 text-xs">
                  <strong>Remedio:</strong> {r.remedy}
                </p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* NHTSA Safety Rating */}
      {report.safetyRating && (
        <div>
          <h3 className="mb-3 text-lg font-bold">⭐ Calificación de Seguridad NHTSA</h3>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <SafetyStar label="General" rating={report.safetyRating.overallRating} />
            <SafetyStar label="Impacto Frontal" rating={report.safetyRating.frontalCrashRating} />
            <SafetyStar label="Impacto Lateral" rating={report.safetyRating.sideCrashRating} />
            <SafetyStar label="Volcadura" rating={report.safetyRating.rolloverRating} />
          </div>
        </div>
      )}

      {/* Footer */}
      <div className="text-muted-foreground flex items-center justify-between border-t pt-4 text-xs">
        <span>Generado: {new Date(report.generatedAt).toLocaleString('es-DO')}</span>
        <span>Versión: {report.version}</span>
        <span>Vigente hasta: {new Date(report.expiresAt).toLocaleDateString('es-DO')}</span>
      </div>
    </div>
  );
}

// --- Internal sub-components ---

function VehicleSpec({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ElementType;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-center gap-2 text-sm">
      <Icon className="text-muted-foreground h-4 w-4" />
      <div>
        <p className="text-muted-foreground text-[10px] uppercase">{label}</p>
        <p className="truncate font-medium">{value}</p>
      </div>
    </div>
  );
}

function SafetyStar({ label, rating }: { label: string; rating?: number }) {
  return (
    <div className="bg-card rounded-lg border p-3 text-center">
      <p className="text-muted-foreground mb-1 text-xs">{label}</p>
      <div className="text-xl">{rating ? '⭐'.repeat(rating) + '☆'.repeat(5 - rating) : 'N/A'}</div>
      {rating && <p className="mt-1 text-xs font-semibold">{rating}/5</p>}
    </div>
  );
}

function PaywallOverlay({
  vehicleId,
  section,
  onPurchaseClick,
}: {
  vehicleId?: string;
  section: string;
  onPurchaseClick?: () => void;
}) {
  const checkoutUrl = vehicleId
    ? `/checkout?product=okla-score-report&vehicleId=${vehicleId}`
    : '/checkout?product=okla-score-report';

  return (
    <div className="relative overflow-hidden rounded-xl border border-dashed border-emerald-300 bg-gradient-to-b from-emerald-50/60 to-white p-8 text-center dark:from-emerald-950/30 dark:to-gray-950">
      <Lock className="mx-auto mb-3 h-8 w-8 text-emerald-500/60" />
      <h4 className="text-base font-bold text-emerald-800 dark:text-emerald-300">{section}</h4>
      <p className="text-muted-foreground mt-1 text-sm">
        Este contenido está disponible en el informe completo OKLA Score™
      </p>
      {onPurchaseClick ? (
        <button
          onClick={onPurchaseClick}
          className="mt-4 inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-6 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-emerald-700"
        >
          Ver informe completo — RD$420
        </button>
      ) : (
        <a
          href={checkoutUrl}
          className="mt-4 inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-6 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-emerald-700"
        >
          Ver informe completo — RD$420
        </a>
      )}
      <p className="text-muted-foreground mt-2 text-xs">≈ US$7 · Pago único por vehículo</p>
    </div>
  );
}
