/**
 * Contact/Messages Service
 *
 * API client for ContactService - inquiries and messages
 */

import { apiClient } from '@/lib/api-client';

// ============================================================================
// Types
// ============================================================================

export type ContactRequestStatus = 'Pending' | 'Read' | 'Responded' | 'Closed';

export interface ContactMessage {
  id: string;
  senderId: string;
  message: string;
  isFromBuyer: boolean;
  isRead: boolean;
  sentAt: string;
}

export interface ContactRequestSummary {
  id: string;
  vehicleId: string;
  subject: string;
  status: ContactRequestStatus;
  createdAt: string;
  respondedAt?: string;
  messageCount: number;
  lastMessage?: string;
}

export interface ReceivedInquiry extends ContactRequestSummary {
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  vehicleTitle?: string;
  unreadCount: number;
}

export interface ContactRequestDetail {
  id: string;
  vehicleId: string;
  subject: string;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  status: ContactRequestStatus;
  createdAt: string;
  messages: ContactMessage[];
}

export interface CreateContactRequestDto {
  vehicleId: string;
  sellerId: string;
  subject: string;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  message: string;
}

export interface ReplyToContactRequestDto {
  message: string;
}

export interface UnreadCountResponse {
  count: number;
}

// ContactService wraps all responses in ApiResponse<T>
interface ContactApiResponse<T> {
  success: boolean;
  data: T;
  message?: string | null;
  error?: string | null;
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get my inquiries (buyer perspective)
 */
export async function getMyInquiries(): Promise<ContactRequestSummary[]> {
  const response = await apiClient.get<ContactApiResponse<ContactRequestSummary[]>>(
    '/api/contactrequests/my-inquiries'
  );
  return response.data.data ?? [];
}

/**
 * Get received inquiries (seller/dealer perspective)
 */
export async function getReceivedInquiries(): Promise<ReceivedInquiry[]> {
  const response = await apiClient.get<ContactApiResponse<ReceivedInquiry[]>>(
    '/api/contactrequests/received'
  );
  return response.data.data ?? [];
}

/**
 * Get contact request detail with messages
 */
export async function getContactRequest(id: string): Promise<ContactRequestDetail> {
  const response = await apiClient.get<ContactApiResponse<ContactRequestDetail>>(
    `/api/contactrequests/${id}`
  );
  return response.data.data;
}

/**
 * Create a new contact request (inquiry about a vehicle)
 */
export async function createContactRequest(dto: CreateContactRequestDto): Promise<{ id: string }> {
  const response = await apiClient.post<ContactApiResponse<{ id: string }>>(
    '/api/contactrequests',
    dto
  );
  return response.data.data;
}

/**
 * Reply to a contact request
 */
export async function replyToContactRequest(id: string, message: string): Promise<ContactMessage> {
  const response = await apiClient.post<ContactApiResponse<ContactMessage>>(
    `/api/contactrequests/${id}/reply`,
    {
      message,
    }
  );
  return response.data.data;
}

/**
 * Mark message as read
 */
export async function markMessageAsRead(messageId: string): Promise<void> {
  await apiClient.post(`/api/contactmessages/${messageId}/mark-read`);
}

/**
 * Get unread message count
 */
export async function getUnreadCount(): Promise<number> {
  const response = await apiClient.get<ContactApiResponse<UnreadCountResponse>>(
    '/api/contactmessages/unread-count'
  );
  return response.data.data?.count ?? 0;
}

// ============================================================================
// Helper Functions
// ============================================================================

export function getStatusLabel(status: ContactRequestStatus): string {
  const labels: Record<ContactRequestStatus, string> = {
    Pending: 'Pendiente',
    Read: 'Leído',
    Responded: 'Respondido',
    Closed: 'Cerrado',
  };
  return labels[status] || status;
}

export function getStatusColor(status: ContactRequestStatus): string {
  const colors: Record<ContactRequestStatus, string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Read: 'bg-blue-100 text-blue-800',
    Responded: 'bg-green-100 text-green-800',
    Closed: 'bg-gray-100 text-gray-800',
  };
  return colors[status] || 'bg-gray-100 text-gray-800';
}

export function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffMins < 1) return 'Ahora';
  if (diffMins < 60) return `Hace ${diffMins} min`;
  if (diffHours < 24) return `Hace ${diffHours}h`;
  if (diffDays === 1) return 'Ayer';
  if (diffDays < 7) return `Hace ${diffDays} días`;

  return date.toLocaleDateString('es-DO', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined,
  });
}
