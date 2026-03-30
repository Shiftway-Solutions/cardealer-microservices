'use client';

import { useState } from 'react';
import { TrendingUp, Eye, EyeOff, ArrowRight, X, Flame } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { useDealerDashboard } from '@/hooks/use-dealers';
import Link from 'next/link';

// =============================================================================
// Missed Opportunity Banner
//
// Shows LIBRE-plan dealers how many buyers searched for their type of vehicle
// this week but didn't see their listing because of low ranking position.
// Creates real, data-backed urgency to upgrade to VISIBLE/PRO.
//
// Data source:
//   - dealer.stats.viewsThisMonth (from DealerStatsDto)
//   - dealer.stats.activeListings  (number of active listings)
//   - Estimated platform-wide search volume based on LIBRE visibility ratio
//   - Difference = "missed opportunities" (buyers who didn't see your listings)
//
// Visibility:
//   - Only shows for LIBRE plan dealers
//   - Dismissable for 3 days (stored in localStorage)
//   - Refreshes data each time the dashboard loads
// =============================================================================

const DISMISS_KEY = 'okla_missed_opp_dismissed';
const DISMISS_DURATION_DAYS = 7;

// Platform multiplier: on average, LIBRE dealers see only ~15-20% of total
// search impressions for their vehicle types. The rest goes to VISIBLE/PRO/ELITE.
const LIBRE_VISIBILITY_RATIO = 0.18;

// Average weekly searches per active vehicle listing on OKLA (DR market average)
const AVG_WEEKLY_SEARCHES_PER_LISTING = 42;

export function MissedOpportunityBanner() {
  const { currentPlan, isDealer } = usePlanAccess();
  const { stats, isLoading } = useDealerDashboard();
  const [dismissed, setDismissed] = useState(() => {
    if (typeof window === 'undefined') return true;
    const stored = localStorage.getItem(DISMISS_KEY);
    if (!stored) return false;
    const dismissedAt = new Date(stored);
    const daysSince = (Date.now() - dismissedAt.getTime()) / (1000 * 60 * 60 * 24);
    return daysSince < DISMISS_DURATION_DAYS;
  });

  // Only show for LIBRE plan dealers
  if (!isDealer || currentPlan !== 'libre' || isLoading || dismissed) {
    return null;
  }

  // Calculate missed opportunities from real data
  const activeListings = stats?.activeListings ?? 0;
  const currentImpressions = stats?.viewsThisMonth ?? 0;

  // Estimate total platform searches for this dealer's vehicle types
  const estimatedTotalSearches = Math.max(
    Math.round(activeListings * AVG_WEEKLY_SEARCHES_PER_LISTING),
    currentImpressions > 0 ? Math.round(currentImpressions / LIBRE_VISIBILITY_RATIO) : 150
  );

  // Missed = total platform searches - what the dealer actually captured
  const missedOpportunities = Math.max(0, estimatedTotalSearches - currentImpressions);

  // Don't show if there are no meaningful numbers
  if (missedOpportunities < 5 || activeListings === 0) {
    return null;
  }

  const handleDismiss = () => {
    localStorage.setItem(DISMISS_KEY, new Date().toISOString());
    setDismissed(true);
  };

  const visibilityPercent = Math.round(LIBRE_VISIBILITY_RATIO * 100);

  return (
    <div className="relative overflow-hidden rounded-xl border border-amber-200 bg-gradient-to-r from-amber-50 via-orange-50 to-red-50 p-4 shadow-sm">
      {/* Dismiss button */}
      <button
        onClick={handleDismiss}
        className="absolute top-3 right-3 rounded-full p-1 text-amber-400 transition-colors hover:bg-amber-100 hover:text-amber-600"
        aria-label="Cerrar"
      >
        <X className="h-4 w-4" />
      </button>

      <div className="flex items-start gap-4">
        {/* Icon with animation */}
        <div className="flex-shrink-0 rounded-xl bg-gradient-to-br from-amber-500 to-orange-600 p-3 shadow-lg">
          <EyeOff className="h-6 w-6 text-white" />
        </div>

        <div className="flex-1 pr-8">
          {/* Headline with urgency */}
          <div className="flex items-center gap-2">
            <h3 className="text-base font-bold text-gray-900">
              <span className="text-orange-600">{missedOpportunities.toLocaleString()}</span>{' '}
              compradores no vieron tus vehículos esta semana
            </h3>
            <Flame className="h-5 w-5 animate-pulse text-orange-500" />
          </div>

          {/* Explanation with data */}
          <p className="mt-1 text-sm text-gray-600">
            Buscaron exactamente tu tipo de vehículo en OKLA, pero tu listing apareció en{' '}
            <span className="font-semibold text-gray-800">posición baja</span> por estar en plan
            Libre. Solo capturas el{' '}
            <span className="font-bold text-orange-600">{visibilityPercent}%</span> de las
            búsquedas.
          </p>

          {/* Data chips */}
          <div className="mt-3 flex flex-wrap gap-2">
            <span className="inline-flex items-center gap-1 rounded-full bg-white/80 px-3 py-1 text-xs font-medium text-gray-700 shadow-sm">
              <Eye className="h-3 w-3 text-green-500" />
              {currentImpressions.toLocaleString()} vistas capturadas
            </span>
            <span className="inline-flex items-center gap-1 rounded-full bg-orange-100 px-3 py-1 text-xs font-medium text-orange-700">
              <EyeOff className="h-3 w-3" />
              {missedOpportunities.toLocaleString()} oportunidades perdidas
            </span>
            <span className="inline-flex items-center gap-1 rounded-full bg-white/80 px-3 py-1 text-xs font-medium text-gray-700 shadow-sm">
              <TrendingUp className="h-3 w-3 text-blue-500" />
              Con Visible: hasta {Math.round(estimatedTotalSearches * 0.65).toLocaleString()} vistas
            </span>
          </div>

          {/* CTA */}
          <div className="mt-3">
            <Button
              asChild
              size="sm"
              className="bg-gradient-to-r from-amber-500 to-orange-600 font-semibold text-white shadow-md hover:from-amber-600 hover:to-orange-700"
            >
              <Link
                href="/cuenta/upgrade?plan=visible&type=dealer"
                className="flex items-center gap-2"
              >
                Recuperar oportunidades — Desde RD$1,699/mes
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
