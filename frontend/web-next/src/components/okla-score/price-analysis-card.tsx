'use client';

import { cn } from '@/lib/utils';
import type { PriceAnalysis, PriceVerdict } from '@/types/okla-score';
import {
  TrendingDown,
  TrendingUp,
  Minus,
  AlertTriangle,
  CheckCircle,
  DollarSign,
  ShieldAlert,
} from 'lucide-react';

// =============================================================================
// OKLA Score™ Price Analysis Card
// =============================================================================

interface PriceAnalysisCardProps {
  analysis: PriceAnalysis;
  className?: string;
}

const VERDICT_CONFIG: Record<
  PriceVerdict,
  {
    labelEs: string;
    color: string;
    bgColor: string;
    borderColor: string;
    icon: React.ElementType;
    description: string;
  }
> = {
  suspicious_deal: {
    labelEs: '🚨 ¡Ojo! Precio Sospechoso',
    color: 'text-red-800',
    bgColor: 'bg-red-100',
    borderColor: 'border-red-400',
    icon: ShieldAlert,
    description:
      'Precio demasiado bajo — puede ser estafa, vehículo con daños ocultos o clonado. Verifica antes de contactar.',
  },
  excellent_deal: {
    labelEs: '¡Excelente Oferta!',
    color: 'text-emerald-700',
    bgColor: 'bg-emerald-50',
    borderColor: 'border-emerald-200',
    icon: CheckCircle,
    description: 'Precio por debajo del mercado. Oportunidad única.',
  },
  good_price: {
    labelEs: 'Buen Precio',
    color: 'text-blue-700',
    bgColor: 'bg-blue-50',
    borderColor: 'border-blue-200',
    icon: TrendingDown,
    description: 'Precio competitivo, ligeramente por debajo del mercado.',
  },
  fair_price: {
    labelEs: 'Precio Justo',
    color: 'text-gray-700',
    bgColor: 'bg-gray-50',
    borderColor: 'border-gray-200',
    icon: Minus,
    description: 'Precio alineado con el valor de mercado.',
  },
  expensive: {
    labelEs: 'Precio Alto',
    color: 'text-amber-700',
    bgColor: 'bg-amber-50',
    borderColor: 'border-amber-200',
    icon: TrendingUp,
    description: 'Ligeramente por encima del mercado. Negocie.',
  },
  very_expensive: {
    labelEs: 'Muy Caro',
    color: 'text-orange-700',
    bgColor: 'bg-orange-50',
    borderColor: 'border-orange-200',
    icon: AlertTriangle,
    description: 'Significativamente sobre el precio de mercado.',
  },
  abusive_price: {
    labelEs: '⚠️ Precio Abusivo',
    color: 'text-red-700',
    bgColor: 'bg-red-50',
    borderColor: 'border-red-200',
    icon: AlertTriangle,
    description: '¡Cuidado! Precio excesivo vs. mercado. No se recomienda.',
  },
};

const formatDOP = (n: number) =>
  new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(n);

const formatUSD = (n: number) =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(n);

export function PriceAnalysisCard({ analysis, className }: PriceAnalysisCardProps) {
  const verdict = VERDICT_CONFIG[analysis.priceVerdict] ?? VERDICT_CONFIG.fair_price;
  const Icon = verdict.icon;
  const diff = analysis.priceDiffPercent;

  return (
    <div className={cn('rounded-xl border p-5', verdict.bgColor, verdict.borderColor, className)}>
      {/* Header */}
      <div className="mb-4 flex items-center gap-3">
        <div
          className={cn('flex h-10 w-10 items-center justify-center rounded-full', verdict.bgColor)}
        >
          <Icon className={cn('h-5 w-5', verdict.color)} />
        </div>
        <div>
          <h3 className={cn('text-lg font-bold', verdict.color)}>{verdict.labelEs}</h3>
          <p className="text-muted-foreground text-sm">{verdict.description}</p>
        </div>
      </div>

      {/* Price comparison */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <p className="text-muted-foreground text-xs font-medium tracking-wider uppercase">
            Precio Listado
          </p>
          <p className="text-xl font-bold">{formatDOP(analysis.listedPriceDOP)}</p>
        </div>
        <div className="space-y-1">
          <p className="text-muted-foreground text-xs font-medium tracking-wider uppercase">
            Precio Justo OKLA
          </p>
          <p className="text-xl font-bold text-emerald-700">{formatDOP(analysis.fairPriceDOP)}</p>
          {analysis.fairPriceUSD > 0 && (
            <p className="text-muted-foreground text-xs">≈ {formatUSD(analysis.fairPriceUSD)}</p>
          )}
        </div>
      </div>

      {/* Difference indicator */}
      <div
        className={cn(
          'mt-4 flex items-center justify-between rounded-lg border px-4 py-2',
          verdict.borderColor
        )}
      >
        <div className="flex items-center gap-2">
          <DollarSign className="text-muted-foreground h-4 w-4" />
          <span className="text-sm font-medium">Diferencia</span>
        </div>
        <span
          className={cn(
            'text-sm font-bold tabular-nums',
            diff > 0 ? 'text-red-600' : diff < -5 ? 'text-emerald-600' : 'text-gray-600'
          )}
        >
          {diff > 0 ? '+' : ''}
          {diff}%{diff > 0 ? ' sobre mercado' : diff < -5 ? ' bajo mercado' : ' al mercado'}
        </span>
      </div>

      {/* Exchange rate */}
      <p className="text-muted-foreground mt-3 text-right text-xs">
        Tasa: 1 USD = {analysis.exchangeRate.toFixed(2)} DOP
      </p>
    </div>
  );
}
