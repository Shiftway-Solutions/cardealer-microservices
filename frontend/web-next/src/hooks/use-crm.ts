/**
 * CRM Hooks
 *
 * React Query hooks for CRM operations: leads management
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getLeads,
  getLeadById,
  getLeadsByStatus,
  searchLeads,
  getRecentLeads,
  createLead,
  updateLead,
  deleteLead,
  calculateLeadStats,
  type LeadStatus,
  type CreateLeadRequest,
  type UpdateLeadRequest,
  type LeadStats,
} from '@/services/crm';

// ============================================================================
// Query Keys
// ============================================================================

export const crmKeys = {
  all: ['crm'] as const,
  leads: () => [...crmKeys.all, 'leads'] as const,
  lead: (id: string) => [...crmKeys.leads(), id] as const,
  leadsByStatus: (status: LeadStatus) => [...crmKeys.leads(), 'status', status] as const,
  leadsSearch: (query: string) => [...crmKeys.leads(), 'search', query] as const,
  leadsRecent: (count: number) => [...crmKeys.leads(), 'recent', count] as const,
  leadStats: () => [...crmKeys.leads(), 'stats'] as const,
};

// ============================================================================
// Leads Hooks
// ============================================================================

/**
 * Get all leads
 */
export function useLeads() {
  return useQuery({
    queryKey: crmKeys.leads(),
    queryFn: getLeads,
  });
}

/**
 * Get lead by ID
 */
export function useLead(id: string) {
  return useQuery({
    queryKey: crmKeys.lead(id),
    queryFn: () => getLeadById(id),
    enabled: !!id,
  });
}

/**
 * Get leads by status
 */
export function useLeadsByStatus(status: LeadStatus) {
  return useQuery({
    queryKey: crmKeys.leadsByStatus(status),
    queryFn: () => getLeadsByStatus(status),
    enabled: !!status,
  });
}

/**
 * Search leads
 */
export function useSearchLeads(query: string) {
  return useQuery({
    queryKey: crmKeys.leadsSearch(query),
    queryFn: () => searchLeads(query),
    enabled: query.length >= 2,
  });
}

/**
 * Get recent leads
 */
export function useRecentLeads(count: number = 10) {
  return useQuery({
    queryKey: crmKeys.leadsRecent(count),
    queryFn: () => getRecentLeads(count),
    retry: 1,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get lead statistics (calculated from leads list)
 */
export function useLeadStats() {
  const { data: leads, ...rest } = useLeads();

  const stats: LeadStats | undefined = leads ? calculateLeadStats(leads) : undefined;

  return {
    data: stats,
    leads,
    ...rest,
  };
}

/**
 * Create a new lead
 */
export function useCreateLead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ request, dealerId }: { request: CreateLeadRequest; dealerId: string }) =>
      createLead(request, dealerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: crmKeys.leads() });
    },
  });
}

/**
 * Update a lead
 */
export function useUpdateLead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateLeadRequest }) =>
      updateLead(id, request),
    onSuccess: data => {
      queryClient.invalidateQueries({ queryKey: crmKeys.leads() });
      queryClient.setQueryData(crmKeys.lead(data.id), data);
    },
  });
}

/**
 * Delete a lead
 */
export function useDeleteLead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: deleteLead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: crmKeys.leads() });
    },
  });
}

// ============================================================================
// Helper Hook: Leads with Filters
// ============================================================================

interface LeadFiltersOptions {
  status?: LeadStatus;
  search?: string;
  minScore?: number;
  maxScore?: number;
  source?: string;
}

export function useFilteredLeads(filters: LeadFiltersOptions = {}) {
  const { data: allLeads, ...rest } = useLeads();

  const filteredLeads = allLeads?.filter(lead => {
    // Status filter
    if (filters.status && lead.status !== filters.status) {
      return false;
    }

    // Search filter
    if (filters.search) {
      const searchLower = filters.search.toLowerCase();
      const matchesName = lead.fullName.toLowerCase().includes(searchLower);
      const matchesEmail = lead.email.toLowerCase().includes(searchLower);
      const matchesPhone = lead.phone?.includes(searchLower);
      const matchesCompany = lead.company?.toLowerCase().includes(searchLower);

      if (!matchesName && !matchesEmail && !matchesPhone && !matchesCompany) {
        return false;
      }
    }

    // Score filters
    if (filters.minScore !== undefined && lead.score < filters.minScore) {
      return false;
    }
    if (filters.maxScore !== undefined && lead.score > filters.maxScore) {
      return false;
    }

    // Source filter
    if (filters.source && lead.source !== filters.source) {
      return false;
    }

    return true;
  });

  return {
    data: filteredLeads,
    allLeads,
    ...rest,
  };
}
