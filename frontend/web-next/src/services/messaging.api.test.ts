/**
 * Messaging Service Tests
 *
 * Tests for the high-level messaging service built on ContactService
 * @see src/services/messaging.ts
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// Mock the contact service
vi.mock('./contact', () => ({
  getMyInquiries: vi.fn(),
  getReceivedInquiries: vi.fn(),
  getContactRequest: vi.fn(),
  replyToContactRequest: vi.fn(),
  formatRelativeTime: vi.fn((date: string) => date),
}));

// Mock the api-client module
vi.mock('@/lib/api-client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

import { apiClient } from '@/lib/api-client';
import {
  getMyInquiries,
  getReceivedInquiries,
  getContactRequest,
  replyToContactRequest,
} from './contact';
import {
  getConversations,
  getConversationDetail,
  sendMessage,
  markConversationAsRead,
  archiveConversation,
  deleteConversation,
  getTotalUnreadCount,
  formatMessageTime,
  formatConversationTime,
} from './messaging';

// =============================================================================
// MOCK DATA
// =============================================================================

const mockMyInquiries = [
  {
    id: 'inquiry-1',
    vehicleId: 'vehicle-1',
    subject: 'Consulta sobre Toyota Corolla 2022',
    lastMessage: '¿Todavía está disponible?',
    respondedAt: '2024-01-15T10:30:00Z',
    createdAt: '2024-01-15T10:00:00Z',
    status: 'Pending',
  },
];

const mockReceivedInquiries = [
  {
    id: 'received-1',
    vehicleId: 'vehicle-2',
    subject: 'Consulta sobre Honda Civic 2023',
    vehicleTitle: 'Honda Civic 2023',
    buyerName: 'Juan Pérez',
    lastMessage: 'Me interesa, ¿cuál es el precio final?',
    respondedAt: '2024-01-15T11:00:00Z',
    createdAt: '2024-01-15T09:00:00Z',
    status: 'Responded',
    unreadCount: 2,
  },
];

const mockContactDetail = {
  id: 'inquiry-1',
  vehicleId: 'vehicle-1',
  buyerName: 'Juan Pérez',
  subject: 'Consulta sobre Toyota Corolla 2022',
  createdAt: '2024-01-15T10:00:00Z',
  status: 'Pending',
  messages: [
    {
      id: 'msg-1',
      message: 'Hola, me interesa el vehículo',
      sentAt: '2024-01-15T10:00:00Z',
      isFromBuyer: true,
      isRead: true,
    },
    {
      id: 'msg-2',
      message: 'Sí, está disponible',
      sentAt: '2024-01-15T10:30:00Z',
      isFromBuyer: false,
      isRead: true,
    },
  ],
};

// =============================================================================
// TEST SETUP
// =============================================================================

describe('Messaging Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ===========================================================================
  // GET CONVERSATIONS
  // ===========================================================================

  describe('getConversations', () => {
    it('should return combined conversations from my inquiries and received', async () => {
      vi.mocked(getMyInquiries).mockResolvedValueOnce(mockMyInquiries);
      vi.mocked(getReceivedInquiries).mockResolvedValueOnce(mockReceivedInquiries);

      const result = await getConversations();

      expect(getMyInquiries).toHaveBeenCalled();
      expect(getReceivedInquiries).toHaveBeenCalled();
      expect(result).toHaveLength(2);
    });

    it('should map inquiry type correctly', async () => {
      vi.mocked(getMyInquiries).mockResolvedValueOnce(mockMyInquiries);
      vi.mocked(getReceivedInquiries).mockResolvedValueOnce([]);

      const result = await getConversations();

      expect(result[0].type).toBe('inquiry');
      expect(result[0].id).toBe('inquiry-1');
    });

    it('should map received type correctly', async () => {
      vi.mocked(getMyInquiries).mockResolvedValueOnce([]);
      vi.mocked(getReceivedInquiries).mockResolvedValueOnce(mockReceivedInquiries);

      const result = await getConversations();

      expect(result[0].type).toBe('received');
      expect(result[0].otherUser.name).toBe('Juan Pérez');
    });

    it('should handle empty inquiries gracefully', async () => {
      vi.mocked(getMyInquiries).mockResolvedValueOnce([]);
      vi.mocked(getReceivedInquiries).mockResolvedValueOnce([]);

      const result = await getConversations();

      expect(result).toHaveLength(0);
    });

    it('should handle API errors gracefully', async () => {
      vi.mocked(getMyInquiries).mockRejectedValueOnce(new Error('Network error'));
      vi.mocked(getReceivedInquiries).mockRejectedValueOnce(new Error('Network error'));

      const result = await getConversations();

      // Should return empty array when both fail
      expect(result).toHaveLength(0);
    });

    it('should sort by most recent activity', async () => {
      const olderInquiry = {
        ...mockMyInquiries[0],
        id: 'old-1',
        createdAt: '2024-01-10T10:00:00Z',
      };
      const newerInquiry = {
        ...mockReceivedInquiries[0],
        id: 'new-1',
        createdAt: '2024-01-16T10:00:00Z',
      };

      vi.mocked(getMyInquiries).mockResolvedValueOnce([olderInquiry]);
      vi.mocked(getReceivedInquiries).mockResolvedValueOnce([newerInquiry]);

      const result = await getConversations();

      expect(result[0].id).toBe('new-1');
      expect(result[1].id).toBe('old-1');
    });
  });

  // ===========================================================================
  // GET CONVERSATION DETAIL
  // ===========================================================================

  describe('getConversationDetail', () => {
    it('should return conversation with messages', async () => {
      vi.mocked(getContactRequest).mockResolvedValueOnce(mockContactDetail);

      const result = await getConversationDetail('inquiry-1', 'inquiry');

      expect(getContactRequest).toHaveBeenCalledWith('inquiry-1');
      expect(result.conversation.id).toBe('inquiry-1');
      expect(result.messages).toHaveLength(2);
    });

    it('should map messages correctly for inquiry type', async () => {
      vi.mocked(getContactRequest).mockResolvedValueOnce(mockContactDetail);

      const result = await getConversationDetail('inquiry-1', 'inquiry');

      // For inquiry type, isFromBuyer=true means isFromMe=true
      expect(result.messages[0].isFromMe).toBe(true);
      expect(result.messages[1].isFromMe).toBe(false);
    });

    it('should map messages correctly for received type', async () => {
      vi.mocked(getContactRequest).mockResolvedValueOnce(mockContactDetail);

      const result = await getConversationDetail('inquiry-1', 'received');

      // For received type, isFromBuyer=true means isFromMe=false
      expect(result.messages[0].isFromMe).toBe(false);
      expect(result.messages[1].isFromMe).toBe(true);
    });

    it('should throw error when conversation not found', async () => {
      vi.mocked(getContactRequest).mockRejectedValueOnce(new Error('Not found'));

      await expect(getConversationDetail('non-existent', 'inquiry')).rejects.toThrow('Not found');
    });
  });

  // ===========================================================================
  // SEND MESSAGE
  // ===========================================================================

  describe('sendMessage', () => {
    it('should send message via replyToContactRequest', async () => {
      vi.mocked(replyToContactRequest).mockResolvedValueOnce({
        id: 'new-msg-1',
        message: 'Test message',
        sentAt: '2024-01-15T12:00:00Z',
      });

      const result = await sendMessage('conversation-1', 'Test message');

      expect(replyToContactRequest).toHaveBeenCalledWith('conversation-1', 'Test message');
      expect(result.content).toBe('Test message');
      expect(result.isFromMe).toBe(true);
    });

    it('should throw error on failed send', async () => {
      vi.mocked(replyToContactRequest).mockRejectedValueOnce(new Error('Send failed'));

      await expect(sendMessage('conversation-1', 'Test')).rejects.toThrow('Send failed');
    });
  });

  // ===========================================================================
  // MARK AS READ
  // ===========================================================================

  describe('markConversationAsRead', () => {
    it('should call API to mark conversation as read', async () => {
      vi.mocked(apiClient.patch).mockResolvedValueOnce({ data: {} });

      await markConversationAsRead('conversation-1');

      expect(apiClient.patch).toHaveBeenCalledWith('/api/contactrequests/conversation-1/read');
    });
  });

  // ===========================================================================
  // ARCHIVE CONVERSATION
  // ===========================================================================

  describe('archiveConversation', () => {
    it('should call API to archive conversation', async () => {
      vi.mocked(apiClient.patch).mockResolvedValueOnce({ data: {} });

      await archiveConversation('conversation-1');

      expect(apiClient.patch).toHaveBeenCalledWith('/api/contactrequests/conversation-1/archive');
    });
  });

  // ===========================================================================
  // DELETE CONVERSATION
  // ===========================================================================

  describe('deleteConversation', () => {
    it('should call API to delete conversation', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({ data: {} });

      await deleteConversation('conversation-1');

      expect(apiClient.delete).toHaveBeenCalledWith('/api/contactrequests/conversation-1');
    });
  });

  // ===========================================================================
  // GET TOTAL UNREAD COUNT
  // ===========================================================================

  describe('getTotalUnreadCount', () => {
    it('should return unread count from API', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: { count: 5 } });

      const result = await getTotalUnreadCount();

      expect(apiClient.get).toHaveBeenCalledWith('/api/contactmessages/unread-count');
      expect(result).toBe(5);
    });

    it('should return 0 on API error', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'));

      const result = await getTotalUnreadCount();

      expect(result).toBe(0);
    });
  });

  // ===========================================================================
  // FORMAT FUNCTIONS
  // ===========================================================================

  describe('formatMessageTime', () => {
    it('should format time correctly', () => {
      const result = formatMessageTime('2024-01-15T10:30:00Z');
      expect(result).toContain(':');
    });
  });

  describe('formatConversationTime', () => {
    it('should return "Ahora" for recent messages', () => {
      const now = new Date().toISOString();
      const result = formatConversationTime(now);
      expect(result).toBe('Ahora');
    });

    it('should return minutes for messages within the hour', () => {
      const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000).toISOString();
      const result = formatConversationTime(fiveMinutesAgo);
      expect(result).toContain('m');
    });
  });
});
