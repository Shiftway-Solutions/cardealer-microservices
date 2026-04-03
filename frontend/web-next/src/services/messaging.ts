/**
 * Messaging Service
 *
 * High-level messaging abstraction over ContactService
 * Provides conversation-based API for the messages page
 */

import { apiClient } from '@/lib/api-client';
import {
  getMyInquiries,
  getReceivedInquiries,
  getContactRequest,
  replyToContactRequest,
  type ContactMessage,
} from './contact';

// =============================================================================
// TYPES
// =============================================================================

export interface ConversationUser {
  id: string;
  name: string;
  avatarUrl: string | null;
  phone?: string; // WhatsApp contact number (available for received inquiries)
}

export interface ConversationVehicle {
  id: string;
  slug: string;
  title: string;
  imageUrl: string | null;
  price: number;
}

export interface Message {
  id: string;
  content: string;
  sentAt: string;
  isFromMe: boolean;
  isRead: boolean;
}

export interface Conversation {
  id: string;
  type: 'inquiry' | 'received'; // Whether I initiated or received
  otherUser: ConversationUser;
  vehicle: ConversationVehicle;
  lastMessage: Message | null;
  unreadCount: number;
  createdAt: string;
  status: string;
}

export interface ConversationDetail {
  conversation: Conversation;
  messages: Message[];
}

// =============================================================================
// API FUNCTIONS
// =============================================================================

/**
 * Get all conversations (both my inquiries and received inquiries)
 */
export async function getConversations(): Promise<Conversation[]> {
  try {
    // Fetch both types of inquiries in parallel
    const [myInquiries, receivedInquiries] = await Promise.all([
      getMyInquiries().catch(() => []),
      getReceivedInquiries().catch(() => []),
    ]);

    const conversations: Conversation[] = [];

    // Map my inquiries (I'm the buyer)
    for (const inquiry of myInquiries) {
      conversations.push({
        id: inquiry.id,
        type: 'inquiry',
        otherUser: {
          id: '', // Seller ID not available in summary
          name: inquiry.sellerName || 'Vendedor',
          avatarUrl: null,
        },
        vehicle: {
          id: inquiry.vehicleId,
          slug: '', // Will be populated when detail is loaded
          title: inquiry.subject.replace('Consulta sobre ', ''),
          imageUrl: null,
          price: 0,
        },
        lastMessage: inquiry.lastMessage
          ? {
              id: inquiry.id,
              content: inquiry.lastMessage,
              sentAt: inquiry.respondedAt || inquiry.createdAt,
              isFromMe: inquiry.status === 'Pending', // If pending, last message is mine
              isRead: inquiry.status !== 'Pending',
            }
          : null,
        unreadCount: 0, // Will be calculated when detail is loaded
        createdAt: inquiry.createdAt,
        status: inquiry.status,
      });
    }

    // Map received inquiries (I'm the seller)
    for (const inquiry of receivedInquiries) {
      conversations.push({
        id: inquiry.id,
        type: 'received',
        otherUser: {
          id: '', // Buyer ID not directly available
          name: inquiry.buyerName,
          avatarUrl: null,
          phone: inquiry.buyerPhone,
        },
        vehicle: {
          id: inquiry.vehicleId,
          slug: '', // Will be populated when detail is loaded
          title: inquiry.vehicleTitle || inquiry.subject.replace('Consulta sobre ', ''),
          imageUrl: null,
          price: 0,
        },
        lastMessage: inquiry.lastMessage
          ? {
              id: inquiry.id,
              content: inquiry.lastMessage,
              sentAt: inquiry.respondedAt || inquiry.createdAt,
              isFromMe: inquiry.status === 'Responded',
              isRead: inquiry.unreadCount === 0,
            }
          : null,
        unreadCount: inquiry.unreadCount,
        createdAt: inquiry.createdAt,
        status: inquiry.status,
      });
    }

    // Sort by most recent activity
    conversations.sort((a, b) => {
      const dateA = a.lastMessage?.sentAt || a.createdAt;
      const dateB = b.lastMessage?.sentAt || b.createdAt;
      return new Date(dateB).getTime() - new Date(dateA).getTime();
    });

    return conversations;
  } catch (error) {
    console.error('Error fetching conversations:', error);
    throw error;
  }
}

/**
 * Get conversation detail with messages
 */
export async function getConversationDetail(
  conversationId: string,
  type: 'inquiry' | 'received'
): Promise<ConversationDetail> {
  const detail = await getContactRequest(conversationId);

  // Map messages
  const messages: Message[] = detail.messages.map(msg => ({
    id: msg.id,
    content: msg.message,
    sentAt: msg.sentAt,
    isFromMe: type === 'inquiry' ? msg.isFromBuyer : !msg.isFromBuyer,
    isRead: msg.isRead,
  }));

  // Build conversation from detail
  const conversation: Conversation = {
    id: detail.id,
    type,
    otherUser: {
      id: '', // Not directly available
      name: type === 'received' ? detail.buyerName : detail.sellerName || 'Vendedor',
      avatarUrl: null,
    },
    vehicle: {
      id: detail.vehicleId,
      slug: '', // Need to fetch from vehicle service
      title: detail.subject.replace('Consulta sobre ', ''),
      imageUrl: null,
      price: 0,
    },
    lastMessage: messages.length > 0 ? messages[messages.length - 1] : null,
    unreadCount: messages.filter(m => !m.isRead && !m.isFromMe).length,
    createdAt: detail.createdAt,
    status: detail.status,
  };

  return {
    conversation,
    messages,
  };
}

/**
 * Send a message in a conversation
 */
export async function sendMessage(conversationId: string, content: string): Promise<Message> {
  const response = await replyToContactRequest(conversationId, content);

  return {
    id: response.id,
    content: response.message,
    sentAt: response.sentAt,
    isFromMe: true,
    isRead: false,
  };
}

/**
 * Mark conversation as read
 */
export async function markConversationAsRead(conversationId: string): Promise<void> {
  // Backend: PATCH /api/contactrequests/{id}/read
  await apiClient.patch(`/api/contactrequests/${conversationId}/read`);
}

/**
 * Archive a conversation
 */
export async function archiveConversation(conversationId: string): Promise<void> {
  // Backend: PATCH /api/contactrequests/{id}/archive
  await apiClient.patch(`/api/contactrequests/${conversationId}/archive`);
}

/**
 * Delete a conversation
 */
export async function deleteConversation(conversationId: string): Promise<void> {
  await apiClient.delete(`/api/contactrequests/${conversationId}`);
}

/**
 * Get total unread count across all conversations
 */
export async function getTotalUnreadCount(): Promise<number> {
  try {
    const response = await apiClient.get<{ count: number }>('/api/contactmessages/unread-count');
    return response.data.count;
  } catch {
    return 0;
  }
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

export function formatMessageTime(dateString: string): string {
  return new Date(dateString).toLocaleTimeString('es-DO', {
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function formatConversationTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffMins < 1) return 'Ahora';
  if (diffMins < 60) return `${diffMins}m`;
  if (diffHours < 24) return `${diffHours}h`;
  if (diffDays < 7) return `${diffDays}d`;

  return date.toLocaleDateString('es-DO', {
    month: 'short',
    day: 'numeric',
  });
}

// =============================================================================
// EXPORT SERVICE OBJECT
// =============================================================================

export const messagingService = {
  getConversations,
  getConversationDetail,
  sendMessage,
  markConversationAsRead,
  archiveConversation,
  deleteConversation,
  getTotalUnreadCount,
  formatMessageTime,
  formatConversationTime,
};

export type { ContactMessage };
