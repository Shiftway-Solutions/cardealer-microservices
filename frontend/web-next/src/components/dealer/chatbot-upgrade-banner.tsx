'use client';

import { useState } from 'react';
import { MessageSquare, Bot, ArrowRight, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { useDealerDashboard } from '@/hooks/use-dealers';
import Link from 'next/link';

// =============================================================================
// ChatBot Upgrade Banner
//
// Shows LIBRE-plan dealers a nudge after they receive their first inquiry,
// highlighting that ChatAgent on VISIBLE handles responses automatically.
//
// Trigger: totalInquiries > 0 (dealer has received at least one inquiry)
// Message: "Respondiste manualmente — el ChatAgent de VISIBLE lo hace solo"
//
// Visibility:
//   - Only for LIBRE plan dealers
//   - Only when dealer has received at least 1 inquiry
//   - Dismissable for 7 days (localStorage)
// =============================================================================

const DISMISS_KEY = 'okla_chatbot_upgrade_dismissed';
const DISMISS_DURATION_MS = 7 * 24 * 60 * 60 * 1000; // 7 days

export function ChatbotUpgradeBanner() {
  const { currentPlan, isDealer } = usePlanAccess();
  const { stats, isLoading } = useDealerDashboard();
  const [dismissed, setDismissed] = useState(() => {
    if (typeof window === 'undefined') return true;
    try {
      const stored = localStorage.getItem(DISMISS_KEY);
      if (!stored) return false;
      const dismissedAt = parseInt(stored, 10);
      return Date.now() - dismissedAt < DISMISS_DURATION_MS;
    } catch {
      return false;
    }
  });

  // Only show for LIBRE plan dealers who have received inquiries
  if (!isDealer || currentPlan !== 'libre' || isLoading || dismissed) {
    return null;
  }

  const totalInquiries = stats?.totalInquiries ?? 0;
  if (totalInquiries === 0) {
    return null;
  }

  const handleDismiss = () => {
    try {
      localStorage.setItem(DISMISS_KEY, Date.now().toString());
    } catch {
      // Silently fail
    }
    setDismissed(true);
  };

  return (
    <div className="relative overflow-hidden rounded-xl border border-blue-200 bg-gradient-to-r from-blue-50 via-indigo-50 to-purple-50 p-4 shadow-sm">
      <button
        onClick={handleDismiss}
        className="absolute top-3 right-3 rounded-full p-1 text-blue-400 transition-colors hover:bg-blue-100 hover:text-blue-600"
        aria-label="Cerrar"
      >
        <X className="h-4 w-4" />
      </button>

      <div className="flex items-start gap-4">
        <div className="flex-shrink-0 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 p-3 shadow-lg">
          <Bot className="h-6 w-6 text-white" />
        </div>

        <div className="flex-1 pr-8">
          <div className="flex items-center gap-2">
            <h3 className="text-base font-bold text-gray-900">
              Respondiste manualmente — el ChatAgent de VISIBLE lo hace solo
            </h3>
          </div>

          <p className="mt-1 text-sm text-gray-600">
            Has recibido{' '}
            <span className="font-semibold text-blue-700">
              {totalInquiries} consulta{totalInquiries > 1 ? 's' : ''}
            </span>{' '}
            y cada una requiere tu tiempo. Con el plan VISIBLE, el ChatAgent IA responde
            automáticamente 24/7, agenda citas y califica leads sin que levantes un dedo.
          </p>

          <div className="mt-3 flex flex-wrap gap-2">
            <span className="inline-flex items-center gap-1 rounded-full bg-white/80 px-3 py-1 text-xs font-medium text-gray-700 shadow-sm">
              <MessageSquare className="h-3 w-3 text-blue-500" />
              {totalInquiries} consulta{totalInquiries > 1 ? 's' : ''} recibida
              {totalInquiries > 1 ? 's' : ''}
            </span>
            <span className="inline-flex items-center gap-1 rounded-full bg-indigo-100 px-3 py-1 text-xs font-medium text-indigo-700">
              <Bot className="h-3 w-3" />
              ChatAgent: respuesta en &lt;30 seg
            </span>
          </div>

          <div className="mt-3">
            <Button
              asChild
              size="sm"
              className="bg-gradient-to-r from-blue-500 to-indigo-600 font-semibold text-white shadow-md hover:from-blue-600 hover:to-indigo-700"
            >
              <Link
                href="/cuenta/upgrade?plan=visible&type=dealer"
                className="flex items-center gap-2"
              >
                Activar ChatAgent — Desde RD$1,699/mes
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
