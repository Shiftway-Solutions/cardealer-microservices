/**
 * Appointments Hooks
 *
 * React Query hooks for AppointmentService API
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAppointments,
  getMyAppointments,
  getDealerAppointments,
  getAppointmentById,
  createAppointment,
  updateAppointment,
  confirmAppointment,
  cancelAppointment,
  completeAppointment,
  getAvailability,
  getTodayAppointments,
  calculateAppointmentStats,
  type CreateAppointmentDto,
  type UpdateAppointmentDto,
  type AppointmentFilters,
} from '@/services/appointments';

// ============================================================================
// Query Keys
// ============================================================================

export const appointmentKeys = {
  all: ['appointments'] as const,
  lists: () => [...appointmentKeys.all, 'list'] as const,
  list: (filters?: AppointmentFilters) => [...appointmentKeys.lists(), filters] as const,
  myAppointments: () => [...appointmentKeys.all, 'my'] as const,
  dealerAppointments: (dealerId: string, filters?: AppointmentFilters) =>
    [...appointmentKeys.all, 'dealer', dealerId, filters] as const,
  details: () => [...appointmentKeys.all, 'detail'] as const,
  detail: (id: string) => [...appointmentKeys.details(), id] as const,
  today: () => [...appointmentKeys.all, 'today'] as const,
  availability: (providerId: string, date: string) =>
    [...appointmentKeys.all, 'availability', providerId, date] as const,
  stats: (dealerId?: string) => [...appointmentKeys.all, 'stats', dealerId] as const,
};

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Get all appointments with optional filters
 */
export function useAppointments(filters?: AppointmentFilters) {
  return useQuery({
    queryKey: appointmentKeys.list(filters),
    queryFn: () => getAppointments(filters),
  });
}

/**
 * Get appointments for current user
 */
export function useMyAppointments() {
  return useQuery({
    queryKey: appointmentKeys.myAppointments(),
    queryFn: getMyAppointments,
  });
}

/**
 * Get appointments for a dealer
 */
export function useDealerAppointments(dealerId: string, filters?: AppointmentFilters) {
  return useQuery({
    queryKey: appointmentKeys.dealerAppointments(dealerId, filters),
    queryFn: () => getDealerAppointments(dealerId, filters),
    enabled: !!dealerId,
    retry: 1,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get single appointment by ID
 */
export function useAppointment(id: string) {
  return useQuery({
    queryKey: appointmentKeys.detail(id),
    queryFn: () => getAppointmentById(id),
    enabled: !!id,
  });
}

/**
 * Get today's appointments
 */
export function useTodayAppointments() {
  return useQuery({
    queryKey: appointmentKeys.today(),
    queryFn: getTodayAppointments,
    refetchInterval: 60000, // Refetch every minute
  });
}

/**
 * Get availability for a provider on a specific date
 */
export function useAvailability(providerId: string, date: string) {
  return useQuery({
    queryKey: appointmentKeys.availability(providerId, date),
    queryFn: () => getAvailability(providerId, date),
    enabled: !!providerId && !!date,
  });
}

/**
 * Get appointment statistics for a dealer
 */
export function useAppointmentStats(dealerId?: string) {
  const { data: appointments } = useDealerAppointments(dealerId || '', {});

  return useQuery({
    queryKey: appointmentKeys.stats(dealerId),
    queryFn: () => {
      if (!appointments) {
        return { today: 0, thisWeek: 0, pending: 0, completedThisMonth: 0 };
      }
      return calculateAppointmentStats(appointments);
    },
    enabled: !!dealerId && !!appointments,
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Create a new appointment
 */
export function useCreateAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateAppointmentDto) => createAppointment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: appointmentKeys.all });
    },
  });
}

/**
 * Update an appointment
 */
export function useUpdateAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAppointmentDto }) =>
      updateAppointment(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: appointmentKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: appointmentKeys.lists() });
    },
  });
}

/**
 * Confirm an appointment
 */
export function useConfirmAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => confirmAppointment(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: appointmentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: appointmentKeys.lists() });
    },
  });
}

/**
 * Cancel an appointment
 */
export function useCancelAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => cancelAppointment(id, reason),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: appointmentKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: appointmentKeys.lists() });
    },
  });
}

/**
 * Complete an appointment
 */
export function useCompleteAppointment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => completeAppointment(id, notes),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: appointmentKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: appointmentKeys.lists() });
    },
  });
}

// ============================================================================
// Derived Hooks
// ============================================================================

/**
 * Get filtered appointments by status
 */
export function useAppointmentsByStatus(
  dealerId: string,
  status: 'pending' | 'confirmed' | 'completed' | 'cancelled'
) {
  const { data: appointments, ...rest } = useDealerAppointments(dealerId);

  const filteredAppointments = appointments?.filter(a => {
    switch (status) {
      case 'pending':
        return a.status === 'Scheduled';
      case 'confirmed':
        return a.status === 'Confirmed' || a.status === 'InProgress';
      case 'completed':
        return a.status === 'Completed';
      case 'cancelled':
        return a.status === 'Cancelled' || a.status === 'NoShow';
      default:
        return true;
    }
  });

  return { data: filteredAppointments, ...rest };
}

/**
 * Get upcoming appointments for a dealer
 */
export function useUpcomingAppointments(dealerId: string, limit?: number) {
  const { data: appointments, ...rest } = useDealerAppointments(dealerId);

  const upcomingAppointments = appointments
    ?.filter(a => {
      const appointmentDateTime = new Date(`${a.scheduledDate}T${a.scheduledTime}`);
      return (
        appointmentDateTime > new Date() && (a.status === 'Scheduled' || a.status === 'Confirmed')
      );
    })
    .sort((a, b) => {
      const dateA = new Date(`${a.scheduledDate}T${a.scheduledTime}`);
      const dateB = new Date(`${b.scheduledDate}T${b.scheduledTime}`);
      return dateA.getTime() - dateB.getTime();
    })
    .slice(0, limit);

  return { data: upcomingAppointments, ...rest };
}

/**
 * Get appointments for a specific date
 */
export function useAppointmentsByDate(dealerId: string, date: string) {
  const { data: appointments, ...rest } = useDealerAppointments(dealerId, {
    fromDate: date,
    toDate: date,
  });

  return { data: appointments, ...rest };
}
