/**
 * Seller Statistics Page
 *
 * Shows vehicle listing statistics for individual sellers.
 * Aggregates data from multiple sources:
 *  - /api/users/me/stats → vehicle counts & views
 *  - /api/sellers/{id}/stats → accurate rating, review count, response rate
 */

'use client';

import * as React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '@/hooks/use-auth';
import { useSellerByUserId, useSellerStats } from '@/hooks/use-seller';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { BarChart3, Eye, Star, Car, TrendingUp, MessageSquare, Clock } from 'lucide-react';
import { userService } from '@/services/users';
import { PlanGate } from '@/components/plan/plan-gate';

// ─── Metric Card ─────────────────────────────────────────────────────────────

interface MetricCardProps {
  label: string;
  value: string | number;
  icon: React.ElementType;
  color: string;
  bgColor: string;
  isLoading?: boolean;
}

function MetricCard({ label, value, icon: Icon, color, bgColor, isLoading }: MetricCardProps) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="flex items-center gap-4">
          <div className={`rounded-lg p-3 ${bgColor}`}>
            <Icon className={`h-6 w-6 ${color}`} />
          </div>
          <div>
            <p className="text-muted-foreground text-sm">{label}</p>
            {isLoading ? (
              <Skeleton className="mt-1 h-7 w-16" />
            ) : (
              <p className="text-foreground text-2xl font-bold">
                {typeof value === 'number' ? value.toLocaleString('es-DO') : value}
              </p>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

function EstadisticasContent() {
  const { user } = useAuth();

  // Seller profile → needed to get seller-level stats
  const { data: sellerProfile, isLoading: sellerLoading } = useSellerByUserId(user?.id);

  // Seller stats: accurate rating, reviews, responseRate
  const { data: sellerStats, isLoading: sellerStatsLoading } = useSellerStats(sellerProfile?.id);

  // User/vehicle stats: vehiclesPublished, totalViews (cached 5 min)
  const { data: userStats, isLoading: userStatsLoading } = useQuery({
    queryKey: ['user-stats'],
    queryFn: () => userService.getUserStats(),
    enabled: !!user?.id,
    staleTime: 5 * 60 * 1000,
  });

  const isLoading = sellerLoading || sellerStatsLoading || userStatsLoading;

  // Merge — prefer seller-level data for rating/reviews (more accurate)
  const vehiclesPublished = userStats?.vehiclesPublished ?? sellerStats?.totalListings ?? 0;
  const vehiclesSold = userStats?.vehiclesSold ?? sellerStats?.totalSales ?? 0;
  const totalViews = userStats?.totalViews ?? 0;
  const totalInquiries = userStats?.totalInquiries ?? 0;
  const responseRate = sellerStats?.responseRate ?? userStats?.responseRate ?? 0;
  const responseTimeMinutes = sellerStats?.responseTimeMinutes;
  const reviewCount = sellerStats?.totalReviews ?? userStats?.reviewCount ?? 0;
  const averageRating = sellerStats?.averageRating ?? userStats?.averageRating ?? 0;
  const conversionRate = totalViews > 0 ? ((totalInquiries / totalViews) * 100).toFixed(1) : null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-foreground text-2xl font-bold">Estadísticas</h1>
        <p className="text-muted-foreground">Resumen del rendimiento de tus publicaciones</p>
      </div>

      {/* Primary KPIs */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          label="Vehículos Publicados"
          value={vehiclesPublished}
          icon={Car}
          color="text-blue-600"
          bgColor="bg-blue-50"
          isLoading={isLoading}
        />
        <MetricCard
          label="Vistas Totales"
          value={totalViews}
          icon={Eye}
          color="text-green-600"
          bgColor="bg-green-50"
          isLoading={isLoading}
        />
        <MetricCard
          label="Consultas Recibidas"
          value={totalInquiries}
          icon={MessageSquare}
          color="text-purple-600"
          bgColor="bg-purple-50"
          isLoading={isLoading}
        />
        <MetricCard
          label="Vehículos Vendidos"
          value={vehiclesSold}
          icon={TrendingUp}
          color="text-amber-600"
          bgColor="bg-amber-50"
          isLoading={isLoading}
        />
      </div>

      {/* Secondary Metrics */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {/* Response Rate */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <BarChart3 className="h-5 w-5" />
              Tasa de Respuesta
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-9 w-24" />
            ) : (
              <p className="text-foreground text-3xl font-bold">
                {responseRate > 0 ? `${Math.round(responseRate)}%` : 'N/A'}
              </p>
            )}
            <p className="text-muted-foreground mt-1 text-sm">
              Tiempo promedio:{' '}
              {responseTimeMinutes
                ? responseTimeMinutes < 60
                  ? `${responseTimeMinutes} min`
                  : `${Math.round(responseTimeMinutes / 60)} h`
                : 'N/A'}
            </p>
          </CardContent>
        </Card>

        {/* Average Rating */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Star className="h-5 w-5 text-amber-400" />
              Calificación Promedio
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-9 w-20" />
            ) : (
              <div className="flex items-baseline gap-2">
                <p className="text-foreground text-3xl font-bold">
                  {averageRating > 0 ? averageRating.toFixed(1) : 'N/A'}
                </p>
                {averageRating > 0 && <span className="text-muted-foreground text-sm">/ 5.0</span>}
              </div>
            )}
            <p className="text-muted-foreground mt-1 text-sm">
              {reviewCount > 0
                ? `${reviewCount.toLocaleString('es-DO')} reseña${reviewCount !== 1 ? 's' : ''}`
                : 'Sin reseñas aún'}
            </p>
          </CardContent>
        </Card>

        {/* Conversion Rate */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Clock className="h-5 w-5" />
              Conversión
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-9 w-20" />
            ) : (
              <p className="text-foreground text-3xl font-bold">
                {conversionRate ? `${conversionRate}%` : 'N/A'}
              </p>
            )}
            <p className="text-muted-foreground mt-1 text-sm">
              {conversionRate ? 'Consultas por cada 100 vistas' : 'Necesitas vistas para calcular'}
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
export default function EstadisticasPage() {
  return (
    <PlanGate feature="detailedStats">
      <EstadisticasContent />
    </PlanGate>
  );
}
