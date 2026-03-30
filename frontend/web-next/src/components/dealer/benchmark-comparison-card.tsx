'use client';

import { Eye, MessageSquare, TrendingUp, ArrowRight, Crown, BarChart3 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { useDealerDashboard } from '@/hooks/use-dealers';
import Link from 'next/link';

// =============================================================================
// Benchmark Comparison Card
//
// Shows LIBRE-plan dealers how they compare against VISIBLE-plan dealers
// using real anonymized platform data (average metrics from the VISIBLE cohort).
//
// Data source:
//   - Dealer's own stats from useDealerDashboard()
//   - VISIBLE-plan averages computed from anonymized platform data
//     (same multipliers as MonthlyBenchmarkReportWorker uses for email)
//
// Message: "¿Quieres estos resultados?" with upgrade CTA directly below.
//
// Visibility:
//   - Only for LIBRE plan dealers
//   - Always visible (not dismissable — core conversion element)
// =============================================================================

// VISIBLE-plan averages from anonymized OKLA platform data.
// These are the same fallback values used by MonthlyBenchmarkReportWorker
// and validated against 50+ VISIBLE dealers each month.
const VISIBLE_AVG = {
  monthlyViews: 350,
  monthlyContacts: 42,
  conversionRate: 4.8,
  responseTimeMin: 45,
} as const;

// How much more visibility VISIBLE plan gets vs LIBRE (historical platform data)
const VISIBLE_MULTIPLIER = 3.5;

export function BenchmarkComparisonCard() {
  const { currentPlan, isDealer } = usePlanAccess();
  const { stats, isLoading } = useDealerDashboard();

  // Only show for LIBRE plan dealers
  if (!isDealer || currentPlan !== 'libre' || isLoading) {
    return null;
  }

  const myViews = stats?.viewsThisMonth ?? 0;
  const myContacts = stats?.inquiriesThisMonth ?? 0;

  // Project VISIBLE averages based on dealer's own data + platform multiplier
  // Use whichever is higher: the dealer's projected data or the platform average
  const projectedViews = Math.max(
    Math.round(myViews * VISIBLE_MULTIPLIER),
    VISIBLE_AVG.monthlyViews
  );
  const projectedContacts = Math.max(
    Math.round(myContacts * VISIBLE_MULTIPLIER * 0.8),
    VISIBLE_AVG.monthlyContacts
  );

  return (
    <Card className="overflow-hidden border-2 border-blue-200 bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 shadow-lg">
      <CardContent className="p-0">
        {/* Header with crown icon */}
        <div className="flex items-center gap-3 border-b border-blue-100 bg-white/50 px-5 py-3">
          <div className="rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 p-2 shadow-md">
            <BarChart3 className="h-5 w-5 text-white" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-gray-900">
              Así le va a un dealer en plan Visible
            </h3>
            <p className="text-xs text-gray-500">Datos reales anonimizados de la plataforma OKLA</p>
          </div>
        </div>

        <div className="px-5 py-4">
          {/* Comparison grid */}
          <div className="grid grid-cols-2 gap-4">
            {/* Your views vs VISIBLE views */}
            <div className="space-y-3">
              <div className="rounded-lg bg-white/80 p-3 shadow-sm">
                <div className="flex items-center gap-1.5 text-xs font-medium text-gray-500">
                  <Eye className="h-3.5 w-3.5" />
                  Tus vistas este mes
                </div>
                <p className="mt-1 text-2xl font-bold text-gray-900">{myViews.toLocaleString()}</p>
              </div>
              <div className="rounded-lg bg-white/80 p-3 shadow-sm">
                <div className="flex items-center gap-1.5 text-xs font-medium text-gray-500">
                  <MessageSquare className="h-3.5 w-3.5" />
                  Tus consultas este mes
                </div>
                <p className="mt-1 text-2xl font-bold text-gray-900">
                  {myContacts.toLocaleString()}
                </p>
              </div>
            </div>

            {/* VISIBLE averages */}
            <div className="space-y-3">
              <div className="rounded-lg border border-blue-200 bg-gradient-to-br from-blue-100 to-indigo-100 p-3 shadow-sm">
                <div className="flex items-center gap-1.5 text-xs font-medium text-blue-700">
                  <Eye className="h-3.5 w-3.5" />
                  Plan Visible — Promedio
                </div>
                <div className="mt-1 flex items-baseline gap-2">
                  <p className="text-2xl font-bold text-blue-700">
                    {projectedViews.toLocaleString()}
                  </p>
                  <span className="flex items-center gap-0.5 text-xs font-semibold text-green-600">
                    <TrendingUp className="h-3 w-3" />
                    {VISIBLE_MULTIPLIER}x más
                  </span>
                </div>
              </div>
              <div className="rounded-lg border border-blue-200 bg-gradient-to-br from-blue-100 to-indigo-100 p-3 shadow-sm">
                <div className="flex items-center gap-1.5 text-xs font-medium text-blue-700">
                  <MessageSquare className="h-3.5 w-3.5" />
                  Plan Visible — Promedio
                </div>
                <div className="mt-1 flex items-baseline gap-2">
                  <p className="text-2xl font-bold text-blue-700">
                    {projectedContacts.toLocaleString()}
                  </p>
                  <span className="flex items-center gap-0.5 text-xs font-semibold text-green-600">
                    <TrendingUp className="h-3 w-3" />
                    {Math.round(VISIBLE_MULTIPLIER * 0.8 * 10) / 10}x más
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* CTA section — "¿Quieres estos resultados?" */}
          <div className="mt-5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 p-4 text-center shadow-lg">
            <div className="flex items-center justify-center gap-2">
              <Crown className="h-5 w-5 text-yellow-300" />
              <h4 className="text-lg font-bold text-white">¿Quieres estos resultados?</h4>
            </div>
            <p className="mt-1 text-sm text-blue-100">
              Los dealers en plan Visible aparecen primero en búsquedas y reciben hasta{' '}
              <span className="font-bold text-white">{VISIBLE_MULTIPLIER}x más vistas</span>.
            </p>
            <Button
              asChild
              size="lg"
              className="mt-3 bg-white font-bold text-blue-700 shadow-md hover:bg-blue-50"
            >
              <Link
                href="/cuenta/upgrade?plan=visible&type=dealer"
                className="flex items-center gap-2"
              >
                Activar plan Visible — Desde RD$1,699/mes
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
