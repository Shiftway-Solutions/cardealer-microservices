'use client';

import { cn } from '@/lib/utils';
import { getScoreBadge, getScoreLevel, type OklaScoreLevel } from '@/types/okla-score';
import { Shield, ShieldCheck, ShieldAlert, ShieldOff } from 'lucide-react';

// =============================================================================
// OKLA Score™ Badge — Compact badge for vehicle listings
// =============================================================================

interface ScoreBadgeProps {
  score: number;
  variant?: 'compact' | 'full';
  className?: string;
}

const BADGE_STYLES: Record<OklaScoreLevel, string> = {
  excellent:
    'bg-emerald-100 text-emerald-800 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-300',
  good: 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300',
  regular: 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30 dark:text-amber-300',
  deficient: 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300',
  critical: 'bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800/30 dark:text-slate-300',
};

const BADGE_ICONS: Record<OklaScoreLevel, React.ElementType> = {
  excellent: ShieldCheck,
  good: ShieldCheck,
  regular: Shield,
  deficient: ShieldAlert,
  critical: ShieldOff,
};

export function ScoreBadge({ score, variant = 'compact', className }: ScoreBadgeProps) {
  const level = getScoreLevel(score);
  const badge = getScoreBadge(score);
  const Icon = BADGE_ICONS[level.level];

  if (variant === 'compact') {
    return (
      <span
        className={cn(
          'inline-flex items-center gap-1 rounded-md border px-2 py-0.5 text-xs font-semibold',
          BADGE_STYLES[level.level],
          className
        )}
        title={`OKLA Score™ ${score}/1000 — ${badge.labelEs}`}
      >
        <Icon className="h-3 w-3" />
        {score}
      </span>
    );
  }

  return (
    <div
      className={cn(
        'inline-flex items-center gap-2 rounded-lg border px-3 py-1.5 text-sm font-semibold',
        BADGE_STYLES[level.level],
        className
      )}
    >
      <Icon className="h-4 w-4" />
      <span className="tabular-nums">{score}</span>
      <span className="text-xs font-normal opacity-75">{badge.shortLabelEs}</span>
    </div>
  );
}
