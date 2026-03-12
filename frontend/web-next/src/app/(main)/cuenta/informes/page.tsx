/**
 * /cuenta/informes — Purchased OKLA Score™ Reports History
 *
 * Shows all purchased reports for the authenticated buyer.
 * Reports purchased as a guest (by email) are linked automatically
 * when the user registers with the same email.
 *
 * Fetches from backend API and merges with locally cached purchases.
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import { FileText, ExternalLink, Clock, Car } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import { getMyReports } from '@/services/report-purchase';
import { useAuth } from '@/hooks/use-auth';

export default function InformesPage() {
  const { isAuthenticated } = useAuth();

  const { data: purchases = [], isLoading } = useQuery({
    queryKey: ['report-purchases'],
    queryFn: getMyReports,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000,
  });

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">
          Mis Informes OKLA Score™
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Informes de vehículos que has comprado. Acceso permanente incluido.
        </p>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="space-y-3">
          {[1, 2, 3].map(i => (
            <div
              key={i}
              className="flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900"
            >
              <div className="flex items-center gap-3">
                <Skeleton className="h-10 w-10 rounded-full" />
                <div>
                  <Skeleton className="mb-2 h-4 w-40" />
                  <Skeleton className="h-3 w-28" />
                </div>
              </div>
              <Skeleton className="h-8 w-24 rounded-lg" />
            </div>
          ))}
        </div>
      )}

      {/* Empty state */}
      {!isLoading && purchases.length === 0 && (
        <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-6 py-16 text-center dark:border-slate-700 dark:bg-slate-900">
          <FileText className="mx-auto mb-4 h-12 w-12 text-slate-400" />
          <h2 className="text-lg font-semibold text-slate-700 dark:text-slate-300">
            No tienes informes comprados
          </h2>
          <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
            Cuando compres un informe OKLA Score™ desde la página de un vehículo, aparecerá aquí.
          </p>
          <Link
            href="/vehiculos"
            className="mt-6 inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-emerald-700"
          >
            <Car className="h-4 w-4" />
            Explorar vehículos
          </Link>
        </div>
      )}

      {/* Report list */}
      {!isLoading && purchases.length > 0 && (
        <div className="space-y-3">
          {purchases.map(purchase => (
            <div
              key={purchase.vehicleId}
              className="flex items-center justify-between rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition-colors hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800"
            >
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 dark:bg-emerald-900/50">
                  <FileText className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                </div>
                <div>
                  <p className="font-semibold text-slate-900 dark:text-white">
                    Informe OKLA Score™
                  </p>
                  <div className="flex items-center gap-3 text-xs text-slate-500 dark:text-slate-400">
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {new Date(purchase.purchasedAt).toLocaleDateString('es-DO', {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                      })}
                    </span>
                    <span>ID: {purchase.vehicleId.substring(0, 8)}...</span>
                  </div>
                </div>
              </div>

              <Link
                href={`/okla-score?vin=${encodeURIComponent(purchase.vehicleId)}`}
                className="inline-flex items-center gap-1.5 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700 transition-colors hover:bg-emerald-100 dark:border-emerald-800 dark:bg-emerald-950 dark:text-emerald-300"
              >
                <ExternalLink className="h-3.5 w-3.5" />
                Ver informe
              </Link>
            </div>
          ))}
        </div>
      )}

      {/* Info footer */}
      <div className="mt-8 rounded-xl border border-slate-200 bg-slate-50 p-4 text-xs text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
        <p>
          <strong>💡 Nota:</strong> Los informes comprados como invitado se vinculan automáticamente
          a tu cuenta cuando te registras con el mismo email del recibo. Si no ves un informe que
          compraste, verifica que tu email de registro coincida con el del recibo.
        </p>
      </div>
    </div>
  );
}
