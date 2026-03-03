'use client';

import { cn } from '@/lib/utils';
import type { ScoreAlert } from '@/types/okla-score';
import { AlertTriangle, XOctagon, Info } from 'lucide-react';

// =============================================================================
// OKLA Score™ Alert Banners — Critical, Warning, Info alerts
// =============================================================================

interface ScoreAlertsProps {
  alerts: ScoreAlert[];
  className?: string;
}

const SEVERITY_CONFIG = {
  critical: {
    icon: XOctagon,
    bg: 'bg-red-50 dark:bg-red-950/30',
    border: 'border-red-300 dark:border-red-800',
    text: 'text-red-800 dark:text-red-300',
    iconColor: 'text-red-600',
    label: 'ALERTA CRÍTICA',
  },
  warning: {
    icon: AlertTriangle,
    bg: 'bg-amber-50 dark:bg-amber-950/30',
    border: 'border-amber-300 dark:border-amber-800',
    text: 'text-amber-800 dark:text-amber-300',
    iconColor: 'text-amber-600',
    label: 'ADVERTENCIA',
  },
  info: {
    icon: Info,
    bg: 'bg-blue-50 dark:bg-blue-950/30',
    border: 'border-blue-300 dark:border-blue-800',
    text: 'text-blue-800 dark:text-blue-300',
    iconColor: 'text-blue-600',
    label: 'INFORMACIÓN',
  },
};

export function ScoreAlerts({ alerts, className }: ScoreAlertsProps) {
  if (!alerts || alerts.length === 0) return null;

  // Sort: critical first, then warning, then info
  const sorted = [...alerts].sort((a, b) => {
    const order = { critical: 0, warning: 1, info: 2 };
    return order[a.severity] - order[b.severity];
  });

  return (
    <div className={cn('space-y-3', className)}>
      {sorted.map((alert, idx) => {
        const config = SEVERITY_CONFIG[alert.severity];
        const Icon = config.icon;

        return (
          <div
            key={`${alert.code}-${idx}`}
            className={cn('flex items-start gap-3 rounded-lg border p-4', config.bg, config.border)}
            role="alert"
          >
            <Icon className={cn('mt-0.5 h-5 w-5 flex-shrink-0', config.iconColor)} />
            <div className="min-w-0 flex-1">
              <div className="mb-1 flex items-center gap-2">
                <span className={cn('text-[10px] font-bold tracking-wider uppercase', config.text)}>
                  {config.label}
                </span>
                <span className="text-muted-foreground font-mono text-[10px]">
                  {alert.dimension}
                </span>
              </div>
              <p className={cn('text-sm font-semibold', config.text)}>{alert.titleEs}</p>
              <p className="text-muted-foreground mt-0.5 text-xs">{alert.descriptionEs}</p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
