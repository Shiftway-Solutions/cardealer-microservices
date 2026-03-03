'use client';

import { cn } from '@/lib/utils';
import { DIMENSION_CONFIG, type DimensionScore, type OklaScoreLevel } from '@/types/okla-score';

// =============================================================================
// OKLA Score™ Dimension Breakdown — Visual breakdown of 7 dimensions
// =============================================================================

interface DimensionBreakdownProps {
  dimensions: DimensionScore[];
  className?: string;
}

const BAR_COLORS: Record<string, { fill: string; bg: string }> = {
  D1: { fill: 'bg-purple-500', bg: 'bg-purple-100' },
  D2: { fill: 'bg-sky-500', bg: 'bg-sky-100' },
  D3: { fill: 'bg-teal-500', bg: 'bg-teal-100' },
  D4: { fill: 'bg-emerald-500', bg: 'bg-emerald-100' },
  D5: { fill: 'bg-amber-500', bg: 'bg-amber-100' },
  D6: { fill: 'bg-orange-500', bg: 'bg-orange-100' },
  D7: { fill: 'bg-blue-500', bg: 'bg-blue-100' },
};

export function DimensionBreakdown({ dimensions, className }: DimensionBreakdownProps) {
  return (
    <div className={cn('space-y-3', className)}>
      {dimensions.map(dim => {
        const config = DIMENSION_CONFIG[dim.dimension];
        const colors = BAR_COLORS[dim.dimension] || { fill: 'bg-gray-500', bg: 'bg-gray-100' };
        const pct = (dim.rawScore / dim.maxPoints) * 100;

        return (
          <div key={dim.dimension} className="space-y-1">
            <div className="flex items-center justify-between text-sm">
              <div className="flex items-center gap-2">
                <span className="text-base">{config.icon}</span>
                <span className="font-medium">{config.labelEs}</span>
                <span className="text-muted-foreground text-xs">({config.weight}%)</span>
              </div>
              <div className="flex items-center gap-2 text-sm tabular-nums">
                <span className="font-semibold">{dim.rawScore}</span>
                <span className="text-muted-foreground">/ {dim.maxPoints}</span>
              </div>
            </div>
            <div className={cn('h-2 w-full overflow-hidden rounded-full', colors.bg)}>
              <div
                className={cn(
                  'h-full rounded-full transition-all duration-700 ease-out',
                  colors.fill
                )}
                style={{ width: `${Math.max(0, Math.min(100, pct))}%` }}
              />
            </div>
            {/* Factors */}
            {dim.factors.length > 0 && (
              <div className="space-y-0.5 pl-7">
                {dim.factors.map((f, i) => (
                  <div key={i} className="text-muted-foreground flex items-center gap-2 text-xs">
                    <span
                      className={cn(
                        'font-mono',
                        f.impact > 0
                          ? 'text-emerald-600'
                          : f.impact < 0
                            ? 'text-red-600'
                            : 'text-gray-400'
                      )}
                    >
                      {f.impact > 0 ? '+' : ''}
                      {f.impact}
                    </span>
                    <span>{f.nameEs}</span>
                    {f.source && <span className="opacity-50">({f.source})</span>}
                  </div>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

// =============================================================================
// Dimension Summary Card — Single dimension in a card layout
// =============================================================================

interface DimensionCardProps {
  dimension: DimensionScore;
  className?: string;
}

export function DimensionCard({ dimension, className }: DimensionCardProps) {
  const config = DIMENSION_CONFIG[dimension.dimension];
  const pct = Math.round((dimension.rawScore / dimension.maxPoints) * 100);
  const colors = BAR_COLORS[dimension.dimension] || { fill: 'bg-gray-500', bg: 'bg-gray-100' };

  let level: OklaScoreLevel;
  if (pct >= 85) level = 'excellent';
  else if (pct >= 70) level = 'good';
  else if (pct >= 50) level = 'regular';
  else if (pct >= 30) level = 'deficient';
  else level = 'critical';

  const levelColors: Record<OklaScoreLevel, string> = {
    excellent: 'border-emerald-200 bg-emerald-50/50',
    good: 'border-blue-200 bg-blue-50/50',
    regular: 'border-amber-200 bg-amber-50/50',
    deficient: 'border-red-200 bg-red-50/50',
    critical: 'border-slate-200 bg-slate-50/50',
  };

  return (
    <div className={cn('rounded-xl border p-4', levelColors[level], className)}>
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-2">
          <span className="text-xl">{config.icon}</span>
          <div>
            <h4 className="text-sm font-semibold">{config.labelEs}</h4>
            <p className="text-muted-foreground text-xs">Peso: {config.weight}%</p>
          </div>
        </div>
        <div className="text-right">
          <p className="text-xl font-bold tabular-nums">{dimension.rawScore}</p>
          <p className="text-muted-foreground text-xs">/ {dimension.maxPoints}</p>
        </div>
      </div>
      <div className={cn('mt-3 h-2 w-full overflow-hidden rounded-full', colors.bg)}>
        <div
          className={cn('h-full rounded-full transition-all duration-500', colors.fill)}
          style={{ width: `${pct}%` }}
        />
      </div>
      {dimension.factors.length > 0 && (
        <div className="mt-3 space-y-1">
          {dimension.factors.map((f, i) => (
            <div key={i} className="flex items-center justify-between text-xs">
              <span className="text-muted-foreground">{f.nameEs}</span>
              <span
                className={cn(
                  'font-mono font-medium',
                  f.impact > 0 ? 'text-emerald-600' : f.impact < 0 ? 'text-red-600' : ''
                )}
              >
                {f.impact > 0 ? '+' : ''}
                {f.impact}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
