'use client';

import { cn } from '@/lib/utils';
import { getScoreLevel, type OklaScoreLevel } from '@/types/okla-score';

// =============================================================================
// OKLA Score™ Gauge — Circular score visualization
// =============================================================================

interface ScoreGaugeProps {
  score: number;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  showLabel?: boolean;
  animated?: boolean;
  className?: string;
}

const SIZE_MAP = {
  sm: { width: 80, stroke: 6, fontSize: 'text-lg', labelSize: 'text-[10px]' },
  md: { width: 120, stroke: 8, fontSize: 'text-2xl', labelSize: 'text-xs' },
  lg: { width: 160, stroke: 10, fontSize: 'text-3xl', labelSize: 'text-sm' },
  xl: { width: 200, stroke: 12, fontSize: 'text-4xl', labelSize: 'text-base' },
};

const LEVEL_COLORS: Record<OklaScoreLevel, { stroke: string; text: string; bg: string }> = {
  excellent: { stroke: '#10b981', text: 'text-emerald-600', bg: 'bg-emerald-50' },
  good: { stroke: '#3b82f6', text: 'text-blue-600', bg: 'bg-blue-50' },
  regular: { stroke: '#f59e0b', text: 'text-amber-600', bg: 'bg-amber-50' },
  deficient: { stroke: '#ef4444', text: 'text-red-600', bg: 'bg-red-50' },
  critical: { stroke: '#64748b', text: 'text-slate-600', bg: 'bg-slate-100' },
};

export function ScoreGauge({
  score,
  size = 'lg',
  showLabel = true,
  animated = true,
  className,
}: ScoreGaugeProps) {
  const config = SIZE_MAP[size];
  const level = getScoreLevel(score);
  const colors = LEVEL_COLORS[level.level];
  const radius = (config.width - config.stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const progress = Math.min(score / 1000, 1);
  const dashOffset = circumference * (1 - progress);

  return (
    <div className={cn('flex flex-col items-center gap-2', className)}>
      <div className="relative" style={{ width: config.width, height: config.width }}>
        <svg
          width={config.width}
          height={config.width}
          className="-rotate-90"
          role="img"
          aria-label={`OKLA Score: ${score} de 1000, ${level.labelEs}`}
        >
          {/* Background circle */}
          <circle
            cx={config.width / 2}
            cy={config.width / 2}
            r={radius}
            fill="none"
            stroke="currentColor"
            strokeWidth={config.stroke}
            className="text-gray-200 dark:text-gray-700"
          />
          {/* Score arc */}
          <circle
            cx={config.width / 2}
            cy={config.width / 2}
            r={radius}
            fill="none"
            stroke={colors.stroke}
            strokeWidth={config.stroke}
            strokeDasharray={circumference}
            strokeDashoffset={dashOffset}
            strokeLinecap="round"
            className={animated ? 'transition-[stroke-dashoffset] duration-1000 ease-out' : ''}
          />
        </svg>
        {/* Center text */}
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className={cn('font-bold tabular-nums', config.fontSize, colors.text)}>
            {score}
          </span>
          <span className={cn('font-medium opacity-60', config.labelSize)}>/ 1,000</span>
        </div>
      </div>
      {showLabel && (
        <div className="text-center">
          <span
            className={cn(
              'inline-flex items-center gap-1 rounded-full px-3 py-1 text-sm font-semibold',
              colors.bg,
              colors.text
            )}
          >
            {level.emoji} {level.labelEs}
          </span>
          <p className="text-muted-foreground mt-1 text-xs">{level.recommendationEs}</p>
        </div>
      )}
    </div>
  );
}
