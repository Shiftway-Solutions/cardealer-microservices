/**
 * Notifications Service
 *
 * API client for notification management
 */

import { apiClient } from '@/lib/api-client';

// =============================================================================
// TYPES
// =============================================================================

export type NotificationType =
  | 'message'
  | 'favorite'
  | 'view'
  | 'price'
  | 'system'
  | 'inquiry'
  | 'vehicle_sold'
  | 'vehicle_approved'
  | 'vehicle_rejected'
  | 'payment_received'
  | 'subscription_expiring';

export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  link?: string;
  isRead: boolean;
  createdAt: string;
  metadata?: Record<string, unknown>;
}

export interface NotificationsListResponse {
  notifications: Notification[];
  total: number;
  unreadCount: number;
}

export interface NotificationPreferences {
  email: boolean;
  push: boolean;
  sms: boolean;
  types: {
    message: boolean;
    inquiry: boolean;
    price: boolean;
    view: boolean;
    system: boolean;
    marketing: boolean;
  };
}

export interface UnreadCountResponse {
  count: number;
}

// =============================================================================
// API FUNCTIONS
// =============================================================================

/**
 * Get all notifications for the current user
 */
export async function getNotifications(params?: {
  page?: number;
  pageSize?: number;
  unreadOnly?: boolean;
}): Promise<NotificationsListResponse> {
  const response = await apiClient.get<NotificationsListResponse>('/api/notifications', {
    params,
  });
  return response.data;
}

/**
 * Get single notification by ID
 */
export async function getNotification(id: string): Promise<Notification> {
  const response = await apiClient.get<Notification>(`/api/notifications/${id}`);
  return response.data;
}

/**
 * Get unread notification count
 */
export async function getUnreadCount(): Promise<number> {
  const response = await apiClient.get<UnreadCountResponse>('/api/notifications/unread/count');
  return response.data.count;
}

/**
 * Mark a notification as read
 */
export async function markAsRead(notificationId: string): Promise<void> {
  await apiClient.patch(`/api/notifications/${notificationId}/read`);
}

/**
 * Mark all notifications as read
 */
export async function markAllAsRead(): Promise<void> {
  await apiClient.patch('/api/notifications/read-all');
}

/**
 * Delete a notification
 */
export async function deleteNotification(notificationId: string): Promise<void> {
  await apiClient.delete(`/api/notifications/${notificationId}`);
}

/**
 * Delete all read notifications
 */
export async function deleteAllNotifications(): Promise<void> {
  await apiClient.delete('/api/notifications/read');
}

/**
 * Get notification preferences
 */
export async function getPreferences(): Promise<NotificationPreferences> {
  const response = await apiClient.get<NotificationPreferences>('/api/notifications/preferences');
  return response.data;
}

/**
 * Update notification preferences
 */
export async function updatePreferences(
  preferences: Partial<NotificationPreferences>
): Promise<NotificationPreferences> {
  const response = await apiClient.put<NotificationPreferences>(
    '/api/notifications/preferences',
    preferences
  );
  return response.data;
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

export function getNotificationIcon(type: NotificationType): string {
  const icons: Record<NotificationType, string> = {
    message: 'MessageCircle',
    favorite: 'Heart',
    view: 'Eye',
    price: 'DollarSign',
    system: 'AlertCircle',
    inquiry: 'MessageCircle',
    vehicle_sold: 'CheckCircle',
    vehicle_approved: 'CheckCircle',
    vehicle_rejected: 'XCircle',
    payment_received: 'CreditCard',
    subscription_expiring: 'Clock',
  };
  return icons[type] || 'Bell';
}

export function getNotificationColor(type: NotificationType): string {
  const colors: Record<NotificationType, string> = {
    message: 'bg-blue-100 text-blue-600',
    favorite: 'bg-red-100 text-red-600',
    view: 'bg-purple-100 text-purple-600',
    price: 'bg-green-100 text-green-600',
    system: 'bg-yellow-100 text-yellow-600',
    inquiry: 'bg-indigo-100 text-indigo-600',
    vehicle_sold: 'bg-emerald-100 text-emerald-600',
    vehicle_approved: 'bg-green-100 text-green-600',
    vehicle_rejected: 'bg-red-100 text-red-600',
    payment_received: 'bg-blue-100 text-blue-600',
    subscription_expiring: 'bg-orange-100 text-orange-600',
  };
  return colors[type] || 'bg-gray-100 text-gray-600';
}

export function formatNotificationTime(dateString: string): string {
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
  });
}

// =============================================================================
// EXPORT SERVICE OBJECT
// =============================================================================

export const notificationsService = {
  getNotifications,
  getNotification,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
  deleteNotification,
  deleteAllNotifications,
  getPreferences,
  updatePreferences,
  getNotificationIcon,
  getNotificationColor,
  formatNotificationTime,
};
