/**
 * Notifications Service Tests
 *
 * Tests for notification management
 * @see src/services/notifications.ts
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// Mock the api-client module
vi.mock('@/lib/api-client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

import { apiClient } from '@/lib/api-client';
import {
  getNotifications,
  getNotification,
  getUnreadCount,
  markAsRead,
  markAllAsRead,
  deleteNotification,
  deleteAllNotifications,
  getPreferences,
  updatePreferences,
  type Notification,
  type NotificationsListResponse,
  type NotificationPreferences,
} from './notifications';

// =============================================================================
// MOCK DATA
// =============================================================================

const mockNotification: Notification = {
  id: 'notif-1',
  userId: 'user-1',
  type: 'message',
  title: 'Nuevo mensaje',
  message: 'Tienes un nuevo mensaje de Juan Pérez',
  link: '/cuenta/mensajes/conversation-1',
  isRead: false,
  createdAt: '2024-01-15T10:00:00Z',
  metadata: { conversationId: 'conv-1' },
};

const mockNotificationsResponse: NotificationsListResponse = {
  notifications: [
    mockNotification,
    {
      id: 'notif-2',
      userId: 'user-1',
      type: 'price',
      title: 'Cambio de precio',
      message: 'El vehículo Toyota Corolla 2022 bajó de precio',
      link: '/vehiculos/toyota-corolla-2022',
      isRead: true,
      createdAt: '2024-01-14T15:30:00Z',
    },
  ],
  total: 2,
  unreadCount: 1,
};

const mockPreferences: NotificationPreferences = {
  email: true,
  push: true,
  sms: false,
  types: {
    message: true,
    inquiry: true,
    price: true,
    view: false,
    system: true,
    marketing: false,
  },
};

// =============================================================================
// TEST SETUP
// =============================================================================

describe('Notifications Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ===========================================================================
  // GET NOTIFICATIONS
  // ===========================================================================

  describe('getNotifications', () => {
    it('should fetch all notifications', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockNotificationsResponse });

      const result = await getNotifications();

      expect(apiClient.get).toHaveBeenCalledWith('/api/notifications', { params: undefined });
      expect(result.notifications).toHaveLength(2);
      expect(result.unreadCount).toBe(1);
    });

    it('should fetch with pagination params', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockNotificationsResponse });

      await getNotifications({ page: 1, pageSize: 20 });

      expect(apiClient.get).toHaveBeenCalledWith('/api/notifications', {
        params: { page: 1, pageSize: 20 },
      });
    });

    it('should fetch only unread notifications', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { ...mockNotificationsResponse, notifications: [mockNotification] },
      });

      await getNotifications({ unreadOnly: true });

      expect(apiClient.get).toHaveBeenCalledWith('/api/notifications', {
        params: { unreadOnly: true },
      });
    });
  });

  // ===========================================================================
  // GET SINGLE NOTIFICATION
  // ===========================================================================

  describe('getNotification', () => {
    it('should fetch single notification by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockNotification });

      const result = await getNotification('notif-1');

      expect(apiClient.get).toHaveBeenCalledWith('/api/notifications/notif-1');
      expect(result.id).toBe('notif-1');
      expect(result.type).toBe('message');
    });
  });

  // ===========================================================================
  // GET UNREAD COUNT
  // ===========================================================================

  describe('getUnreadCount', () => {
    it('should return unread count', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: { count: 5 } });

      const result = await getUnreadCount();

      expect(apiClient.get).toHaveBeenCalledWith('/api/notifications/unread-count');
      expect(result).toBe(5);
    });

    it('should return 0 when no unread notifications', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: { count: 0 } });

      const result = await getUnreadCount();

      expect(result).toBe(0);
    });
  });

  // ===========================================================================
  // MARK AS READ
  // ===========================================================================

  describe('markAsRead', () => {
    it('should mark notification as read', async () => {
      vi.mocked(apiClient.patch).mockResolvedValueOnce({ data: {} });

      await markAsRead('notif-1');

      expect(apiClient.patch).toHaveBeenCalledWith('/api/notifications/notif-1/read');
    });
  });

  describe('markAllAsRead', () => {
    it('should mark all notifications as read', async () => {
      vi.mocked(apiClient.patch).mockResolvedValueOnce({ data: {} });

      await markAllAsRead();

      expect(apiClient.patch).toHaveBeenCalledWith('/api/notifications/read-all');
    });
  });

  // ===========================================================================
  // DELETE NOTIFICATIONS
  // ===========================================================================

  describe('deleteNotification', () => {
    it('should delete single notification', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({ data: {} });

      await deleteNotification('notif-1');

      expect(apiClient.delete).toHaveBeenCalledWith('/api/notifications/notif-1');
    });
  });

  describe('deleteAllNotifications', () => {
    it('should delete all notifications', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({ data: {} });

      await deleteAllNotifications();

      expect(apiClient.delete).toHaveBeenCalledWith('/api/notifications/read');
    });
  });

  // ===========================================================================
  // PREFERENCES
  // ===========================================================================

  describe('getPreferences', () => {
    it('should fetch notification preferences', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockPreferences });

      const result = await getPreferences();

      expect(apiClient.get).toHaveBeenCalledWith('/api/notifications/preferences');
      expect(result.email).toBe(true);
      expect(result.types.message).toBe(true);
    });
  });

  describe('updatePreferences', () => {
    it('should update notification preferences', async () => {
      const updatedPrefs = { ...mockPreferences, sms: true };
      vi.mocked(apiClient.put).mockResolvedValueOnce({ data: updatedPrefs });

      const result = await updatePreferences({ sms: true });

      expect(apiClient.put).toHaveBeenCalledWith('/api/notifications/preferences', { sms: true });
      expect(result.sms).toBe(true);
    });

    it('should allow partial updates', async () => {
      const partialUpdate = { email: false };
      vi.mocked(apiClient.put).mockResolvedValueOnce({
        data: { ...mockPreferences, email: false },
      });

      await updatePreferences(partialUpdate);

      expect(apiClient.put).toHaveBeenCalledWith('/api/notifications/preferences', partialUpdate);
    });
  });
});
