/**
 * Contact/Messages Hooks
 *
 * React Query hooks for ContactService operations
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getMyInquiries,
  getReceivedInquiries,
  getContactRequest,
  createContactRequest,
  replyToContactRequest,
  markMessageAsRead,
  getUnreadCount,
  type CreateContactRequestDto,
} from '@/services/contact';

// ============================================================================
// Query Keys
// ============================================================================

export const contactKeys = {
  all: ['contact'] as const,
  myInquiries: () => [...contactKeys.all, 'my-inquiries'] as const,
  receivedInquiries: () => [...contactKeys.all, 'received'] as const,
  conversation: (id: string) => [...contactKeys.all, 'conversation', id] as const,
  unreadCount: () => [...contactKeys.all, 'unread-count'] as const,
};

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Get my inquiries (as a buyer)
 */
export function useMyInquiries() {
  return useQuery({
    queryKey: contactKeys.myInquiries(),
    queryFn: getMyInquiries,
  });
}

/**
 * Get received inquiries (as a seller/dealer)
 */
export function useReceivedInquiries() {
  return useQuery({
    queryKey: contactKeys.receivedInquiries(),
    queryFn: getReceivedInquiries,
  });
}

/**
 * Get conversation detail with messages
 */
export function useConversation(id: string) {
  return useQuery({
    queryKey: contactKeys.conversation(id),
    queryFn: () => getContactRequest(id),
    enabled: !!id,
  });
}

/**
 * Get unread message count
 */
export function useUnreadCount() {
  return useQuery({
    queryKey: contactKeys.unreadCount(),
    queryFn: getUnreadCount,
    refetchInterval: 30000, // Refetch every 30 seconds
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Create a new contact request (inquiry)
 */
export function useCreateContactRequest() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: CreateContactRequestDto) => createContactRequest(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contactKeys.myInquiries() });
    },
  });
}

/**
 * Reply to a contact request
 */
export function useReplyToContactRequest() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, message }: { id: string; message: string }) =>
      replyToContactRequest(id, message),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: contactKeys.conversation(variables.id) });
      queryClient.invalidateQueries({ queryKey: contactKeys.receivedInquiries() });
      queryClient.invalidateQueries({ queryKey: contactKeys.myInquiries() });
    },
  });
}

/**
 * Mark a message as read
 */
export function useMarkMessageAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: markMessageAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contactKeys.unreadCount() });
    },
  });
}

// ============================================================================
// Combined Hooks
// ============================================================================

/**
 * Get all conversations (combines sent and received)
 */
export function useAllConversations() {
  const myInquiries = useMyInquiries();
  const receivedInquiries = useReceivedInquiries();

  const isLoading = myInquiries.isLoading || receivedInquiries.isLoading;
  const error = myInquiries.error || receivedInquiries.error;

  // Transform to unified format
  const conversations = [
    ...(myInquiries.data?.map(i => ({
      ...i,
      type: 'sent' as const,
      otherPartyName: 'Vendedor', // Would need vehicle/seller info
      unreadCount: 0,
    })) || []),
    ...(receivedInquiries.data?.map(i => ({
      ...i,
      type: 'received' as const,
      otherPartyName: i.buyerName,
    })) || []),
  ].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  return {
    data: conversations,
    isLoading,
    error,
    refetch: () => {
      myInquiries.refetch();
      receivedInquiries.refetch();
    },
  };
}
