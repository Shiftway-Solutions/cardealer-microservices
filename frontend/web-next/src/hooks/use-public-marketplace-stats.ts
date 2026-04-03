'use client';

import { useQuery } from '@tanstack/react-query';
import { dealerService } from '@/services/dealers';
import { searchVehicles } from '@/services/vehicles';

const countFormatter = new Intl.NumberFormat('es-DO');

function formatCount(value: number): string {
  return countFormatter.format(Math.max(0, value));
}

export interface PublicMarketplaceStats {
  dealerCount: number | null;
  formattedDealerCount: string | null;
  formattedVehicleCount: string | null;
  vehicleCount: number | null;
}

export function usePublicMarketplaceStats() {
  return useQuery<PublicMarketplaceStats>({
    queryKey: ['public-marketplace-stats'],
    queryFn: async () => {
      const [vehicleResult, dealerResult] = await Promise.allSettled([
        searchVehicles({ page: 1, pageSize: 1 }),
        dealerService.getDealers({ page: 1, pageSize: 1 }),
      ]);

      const vehicleCount =
        vehicleResult.status === 'fulfilled' ? vehicleResult.value.pagination.totalItems : null;
      const dealerCount =
        dealerResult.status === 'fulfilled' ? dealerResult.value.pagination.totalItems : null;

      return {
        dealerCount,
        formattedDealerCount: dealerCount === null ? null : formatCount(dealerCount),
        formattedVehicleCount: vehicleCount === null ? null : formatCount(vehicleCount),
        vehicleCount,
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 5 * 60 * 1000,
  });
}