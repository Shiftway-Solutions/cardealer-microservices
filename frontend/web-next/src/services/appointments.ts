/**
 * Appointments Service
 *
 * API client for AppointmentService backend
 * Manages test drives, meetings, and inspections
 */

import { apiClient } from '@/lib/api-client';

// ============================================================================
// Types
// ============================================================================

export type AppointmentType =
  | 'TestDrive'
  | 'PropertyTour'
  | 'Consultation'
  | 'Inspection'
  | 'Meeting';

export type AppointmentStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export interface Appointment {
  id: string;
  appointmentNumber: string;
  type: AppointmentType;
  relatedEntityId?: string;
  relatedEntityDescription?: string;

  // Client info
  clientId: string;
  clientName: string;
  clientEmail: string;
  clientPhone?: string;

  // Agent/Seller info
  agentOrSellerId?: string;
  agentOrSellerName?: string;

  // Schedule
  scheduledDate: string;
  scheduledTime: string;
  durationMinutes: number;

  // Location
  location: string;
  locationAddress?: string;
  isVirtualAppointment: boolean;
  virtualMeetingLink?: string;

  // Status
  status: AppointmentStatus;
  createdAt: string;
  confirmedAt?: string;
  completedAt?: string;
  cancelledAt?: string;
  cancellationReason?: string;

  // Notes
  clientNotes?: string;
  internalNotes?: string;

  // Reminders
  reminderSent: boolean;
  reminderSentAt?: string;
}

export interface CreateAppointmentDto {
  type: AppointmentType;
  relatedEntityId?: string;
  relatedEntityDescription?: string;
  clientId?: string;
  clientName: string;
  clientEmail: string;
  clientPhone?: string;
  agentOrSellerId?: string;
  scheduledDate: string;
  scheduledTime: string;
  durationMinutes?: number;
  location: string;
  locationAddress?: string;
  isVirtualAppointment?: boolean;
  virtualMeetingLink?: string;
  clientNotes?: string;
  internalNotes?: string;
}

export interface UpdateAppointmentDto {
  scheduledDate?: string;
  scheduledTime?: string;
  location?: string;
  locationAddress?: string;
  clientNotes?: string;
  internalNotes?: string;
}

export interface TimeSlot {
  time: string;
  available: boolean;
}

export interface AvailabilityResponse {
  date: string;
  providerId: string;
  availableSlots: TimeSlot[];
}

export interface AppointmentFilters {
  status?: AppointmentStatus;
  type?: AppointmentType;
  fromDate?: string;
  toDate?: string;
  clientId?: string;
  agentOrSellerId?: string;
}

export interface AppointmentStats {
  today: number;
  thisWeek: number;
  pending: number;
  completedThisMonth: number;
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get all appointments with optional filters
 */
export async function getAppointments(filters?: AppointmentFilters): Promise<Appointment[]> {
  const params = new URLSearchParams();

  if (filters?.status) params.append('status', filters.status);
  if (filters?.type) params.append('type', filters.type);
  if (filters?.fromDate) params.append('fromDate', filters.fromDate);
  if (filters?.toDate) params.append('toDate', filters.toDate);
  if (filters?.clientId) params.append('clientId', filters.clientId);
  if (filters?.agentOrSellerId) params.append('agentOrSellerId', filters.agentOrSellerId);

  const queryString = params.toString();
  const url = `/api/appointments${queryString ? `?${queryString}` : ''}`;

  const response = await apiClient.get<Appointment[]>(url);
  return response.data;
}

/**
 * Get appointments for current user (my appointments)
 */
export async function getMyAppointments(): Promise<Appointment[]> {
  const response = await apiClient.get<Appointment[]>('/api/appointments/my-appointments');
  return response.data;
}

/**
 * Get appointments for a dealer
 */
export async function getDealerAppointments(
  dealerId: string,
  filters?: AppointmentFilters
): Promise<Appointment[]> {
  const params = new URLSearchParams();

  if (filters?.status) params.append('status', filters.status);
  if (filters?.type) params.append('type', filters.type);
  if (filters?.fromDate) params.append('fromDate', filters.fromDate);
  if (filters?.toDate) params.append('toDate', filters.toDate);

  const queryString = params.toString();
  const url = `/api/appointments/dealer/${dealerId}${queryString ? `?${queryString}` : ''}`;

  const response = await apiClient.get<{ items: Appointment[] } | Appointment[]>(url);
  const data = response.data;
  return Array.isArray(data) ? data : ((data as { items: Appointment[] }).items ?? []);
}

/**
 * Get appointment by ID
 */
export async function getAppointmentById(id: string): Promise<Appointment> {
  const response = await apiClient.get<Appointment>(`/api/appointments/${id}`);
  return response.data;
}

/**
 * Create a new appointment
 */
export async function createAppointment(data: CreateAppointmentDto): Promise<Appointment> {
  const response = await apiClient.post<Appointment>('/api/appointments', data);
  return response.data;
}

/**
 * Update an appointment
 */
export async function updateAppointment(
  id: string,
  data: UpdateAppointmentDto
): Promise<Appointment> {
  const response = await apiClient.put<Appointment>(`/api/appointments/${id}`, data);
  return response.data;
}

/**
 * Confirm an appointment
 */
export async function confirmAppointment(id: string): Promise<Appointment> {
  const response = await apiClient.put<Appointment>(`/api/appointments/${id}/confirm`);
  return response.data;
}

/**
 * Cancel an appointment
 */
export async function cancelAppointment(id: string, reason?: string): Promise<Appointment> {
  const response = await apiClient.put<Appointment>(`/api/appointments/${id}/cancel`, { reason });
  return response.data;
}

/**
 * Mark appointment as completed
 */
export async function completeAppointment(id: string, notes?: string): Promise<Appointment> {
  const response = await apiClient.put<Appointment>(`/api/appointments/${id}/complete`, {
    internalNotes: notes,
  });
  return response.data;
}

/**
 * Get availability for a provider (agent/dealer)
 */
export async function getAvailability(
  providerId: string,
  date: string
): Promise<AvailabilityResponse> {
  const response = await apiClient.get<AvailabilityResponse>(
    `/api/timeslots/${providerId}/availability?date=${date}`
  );
  return response.data;
}

/**
 * Get appointments for today
 */
export async function getTodayAppointments(): Promise<Appointment[]> {
  const today = new Date().toISOString().split('T')[0];
  return getAppointments({ fromDate: today, toDate: today });
}

/**
 * Calculate appointment statistics
 */
export function calculateAppointmentStats(appointments: Appointment[]): AppointmentStats {
  const now = new Date();
  const today = now.toISOString().split('T')[0];

  // Start of week (Sunday)
  const startOfWeek = new Date(now);
  startOfWeek.setDate(now.getDate() - now.getDay());
  const weekStart = startOfWeek.toISOString().split('T')[0];

  // End of week (Saturday)
  const endOfWeek = new Date(now);
  endOfWeek.setDate(now.getDate() + (6 - now.getDay()));
  const weekEnd = endOfWeek.toISOString().split('T')[0];

  // Start of month
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
  const monthStart = startOfMonth.toISOString().split('T')[0];

  return {
    today: appointments.filter(a => a.scheduledDate === today).length,
    thisWeek: appointments.filter(a => a.scheduledDate >= weekStart && a.scheduledDate <= weekEnd)
      .length,
    pending: appointments.filter(a => a.status === 'Scheduled' || a.status === 'Confirmed').length,
    completedThisMonth: appointments.filter(
      a => a.status === 'Completed' && a.completedAt && a.completedAt >= monthStart
    ).length,
  };
}

// ============================================================================
// Helper Functions
// ============================================================================

export function getAppointmentTypeLabel(type: AppointmentType): string {
  const labels: Record<AppointmentType, string> = {
    TestDrive: 'Test Drive',
    PropertyTour: 'Tour de Propiedad',
    Consultation: 'Consulta',
    Inspection: 'Inspección',
    Meeting: 'Reunión',
  };
  return labels[type] || type;
}

export function getAppointmentTypeIcon(type: AppointmentType): string {
  const icons: Record<AppointmentType, string> = {
    TestDrive: '🚗',
    PropertyTour: '🏠',
    Consultation: '💬',
    Inspection: '🔍',
    Meeting: '📅',
  };
  return icons[type] || '📅';
}

export function getStatusColor(status: AppointmentStatus): string {
  const colors: Record<AppointmentStatus, string> = {
    Scheduled: 'bg-blue-100 text-blue-700',
    Confirmed: 'bg-emerald-100 text-emerald-700',
    InProgress: 'bg-purple-100 text-purple-700',
    Completed: 'bg-gray-100 text-gray-700',
    Cancelled: 'bg-red-100 text-red-700',
    NoShow: 'bg-amber-100 text-amber-700',
  };
  return colors[status] || 'bg-gray-100 text-gray-700';
}

export function getStatusLabel(status: AppointmentStatus): string {
  const labels: Record<AppointmentStatus, string> = {
    Scheduled: 'Programada',
    Confirmed: 'Confirmada',
    InProgress: 'En Progreso',
    Completed: 'Completada',
    Cancelled: 'Cancelada',
    NoShow: 'No Asistió',
  };
  return labels[status] || status;
}

export function formatAppointmentTime(date: string, time: string): string {
  const dateObj = new Date(`${date}T${time}`);
  return dateObj.toLocaleTimeString('es-DO', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
  });
}

export function formatAppointmentDate(date: string): string {
  const dateObj = new Date(date);
  return dateObj.toLocaleDateString('es-DO', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}

export function isUpcoming(appointment: Appointment): boolean {
  const appointmentDateTime = new Date(`${appointment.scheduledDate}T${appointment.scheduledTime}`);
  return appointmentDateTime > new Date();
}

export function isPast(appointment: Appointment): boolean {
  const appointmentDateTime = new Date(`${appointment.scheduledDate}T${appointment.scheduledTime}`);
  return appointmentDateTime < new Date();
}
