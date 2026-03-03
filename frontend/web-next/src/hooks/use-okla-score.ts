'use client';

import { useMutation, useQuery } from '@tanstack/react-query';
import type {
  VinDecodeResult,
  OklaScoreReport,
  NhtsaRecall,
  NhtsaSafetyRating,
  NhtsaComplaintSummary,
  ScoreLookupRequest,
} from '@/types/okla-score';

// =============================================================================
// OKLA Score™ Hooks — TanStack Query integration
// =============================================================================

// --- VIN Decode ---

export function useVinDecode(vin: string | null) {
  return useQuery<VinDecodeResult | null>({
    queryKey: ['vin-decode', vin],
    queryFn: async () => {
      if (!vin || vin.length !== 17) return null;
      const res = await fetch(`/api/score/vin-decode?vin=${vin}`);
      const data = await res.json();
      if (!data.success) throw new Error(data.error);
      return data.data;
    },
    enabled: !!vin && vin.length === 17,
    staleTime: 24 * 60 * 60 * 1000, // 24h — VIN data doesn't change
    retry: 1,
  });
}

// --- Recalls ---

export function useRecalls(make?: string, model?: string, year?: number) {
  return useQuery<NhtsaRecall[]>({
    queryKey: ['recalls', make, model, year],
    queryFn: async () => {
      const res = await fetch(
        `/api/score/recalls?make=${encodeURIComponent(make!)}&model=${encodeURIComponent(model!)}&year=${year}`
      );
      const data = await res.json();
      return data.success ? data.data || [] : [];
    },
    enabled: !!make && !!model && !!year,
    staleTime: 60 * 60 * 1000, // 1h
  });
}

// --- Safety Rating ---

export function useSafetyRating(make?: string, model?: string, year?: number) {
  return useQuery<NhtsaSafetyRating | null>({
    queryKey: ['safety-rating', make, model, year],
    queryFn: async () => {
      const res = await fetch(
        `/api/score/safety?make=${encodeURIComponent(make!)}&model=${encodeURIComponent(model!)}&year=${year}`
      );
      const data = await res.json();
      return data.success ? data.data : null;
    },
    enabled: !!make && !!model && !!year,
    staleTime: 24 * 60 * 60 * 1000,
  });
}

// --- Complaints Summary ---

export function useComplaints(make?: string, model?: string, year?: number) {
  return useQuery<NhtsaComplaintSummary>({
    queryKey: ['complaints', make, model, year],
    queryFn: async () => {
      const res = await fetch(
        `/api/score/complaints?make=${encodeURIComponent(make!)}&model=${encodeURIComponent(model!)}&year=${year}`
      );
      const data = await res.json();
      return data.success ? data.data : { totalComplaints: 0, componentBreakdown: {} };
    },
    enabled: !!make && !!model && !!year,
    staleTime: 60 * 60 * 1000,
  });
}

// --- Full OKLA Score Calculation ---

export function useCalculateScore() {
  return useMutation<OklaScoreReport, Error, ScoreLookupRequest>({
    mutationFn: async request => {
      const res = await fetch('/api/score/calculate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      });
      const data = await res.json();
      if (!data.success) throw new Error(data.error || 'Error calculando score');
      return data.data;
    },
  });
}
