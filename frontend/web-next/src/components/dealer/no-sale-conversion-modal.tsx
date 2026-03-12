'use client';

import { useState } from 'react';
import { TrendingUp, Clock, ArrowRight, Crown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { useDealerDashboard } from '@/hooks/use-dealers';
import Link from 'next/link';

// =============================================================================
// No-Sale Conversion Modal
//
// Shows LIBRE-plan dealers a modal after 45 days without making a sale,
// highlighting that VISIBLE dealers sell on average 18 days faster.
//
// Trigger: dealer.createdAt > 45 days ago AND stats.totalRevenue === 0
// Message: "¿Sabías que dealers VISIBLE venden en promedio 18 días antes?"
//
// Visibility:
//   - Only for LIBRE plan dealers
//   - Only when 45+ days without a sale
//   - Dismissable for 7 days (localStorage)
// =============================================================================

const DISMISS_KEY = 'okla_nosale_modal_dismissed';
const DISMISS_DURATION_MS = 7 * 24 * 60 * 60 * 1000; // 7 days
const NO_SALE_THRESHOLD_DAYS = 45;

function isDismissed(): boolean {
  if (typeof window === 'undefined') return true;
  try {
    const stored = localStorage.getItem(DISMISS_KEY);
    if (!stored) return false;
    const dismissedAt = parseInt(stored, 10);
    return Date.now() - dismissedAt < DISMISS_DURATION_MS;
  } catch {
    return true;
  }
}

export function NoSaleConversionModal() {
  const { currentPlan, isDealer } = usePlanAccess();
  const { dealer, stats, isLoading } = useDealerDashboard();
  const [dismissed, setDismissed] = useState(false);

  // Determine if conditions are met (without Date.now in render)
  const meetsConditions = (() => {
    if (!isDealer || currentPlan !== 'libre' || isLoading || !dealer || !stats) return false;
    if ((stats.totalRevenue ?? 0) > 0) return false;
    const createdAt = new Date(dealer.createdAt);
    const now = new Date();
    const daysSinceCreation = (now.getTime() - createdAt.getTime()) / (1000 * 60 * 60 * 24);
    return daysSinceCreation >= NO_SALE_THRESHOLD_DAYS;
  })();

  const open = meetsConditions && !dismissed && !isDismissed();

  const handleClose = () => {
    try {
      localStorage.setItem(DISMISS_KEY, Date.now().toString());
    } catch {
      // Silently fail
    }
    setDismissed(true);
  };

  if (!open) return null;

  return (
    <Dialog
      open
      onOpenChange={isOpen => {
        if (!isOpen) handleClose();
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-gradient-to-br from-amber-100 to-orange-100">
            <Clock className="h-7 w-7 text-amber-600" />
          </div>
          <DialogTitle className="text-center text-lg">
            ¿Sabías que dealers VISIBLE venden en promedio 18 días antes?
          </DialogTitle>
          <DialogDescription className="text-center">
            Llevas más de {NO_SALE_THRESHOLD_DAYS} días en OKLA sin concretar una venta. Los dealers
            con plan VISIBLE tienen{' '}
            <span className="font-semibold text-amber-700">3.5x más visibilidad</span>, badge de
            verificación y mejor posición en búsquedas.
          </DialogDescription>
        </DialogHeader>

        <div className="mt-2 space-y-3">
          <div className="flex items-center gap-3 rounded-lg bg-green-50 p-3">
            <TrendingUp className="h-5 w-5 flex-shrink-0 text-green-600" />
            <div>
              <p className="text-sm font-medium text-green-900">
                Dealers VISIBLE venden más rápido
              </p>
              <p className="text-xs text-green-700">
                Tiempo promedio de venta: 27 días vs 45+ días en LIBRE
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-2 pt-2">
            <Button asChild className="w-full gap-2">
              <Link href="/cuenta/upgrade?plan=visible&type=dealer">
                <Crown className="h-4 w-4" />
                Activar plan VISIBLE — Desde RD$1,699/mes
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
            <Button variant="ghost" onClick={handleClose} className="text-sm">
              Ahora no
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
