// =============================================================================
// SupportAgent Service — API Client for SupportAgent (.NET 8)
// =============================================================================
// Platform support & buyer protection guidance chatbot (Claude Haiku 4.5)
// Two modules: Soporte Técnico (how to use OKLA) + Orientación al Comprador

import { apiClient } from '@/lib/api-client';
import { cleanBotResponse } from '@/lib/chat-format';

// =============================================================================
// Types & Interfaces
// =============================================================================

export interface SupportMessageRequest {
  sessionId?: string;
  message: string;
  userName?: string;
}

export interface SupportMessageResponse {
  sessionId: string;
  response: string;
  category?: string;
  confidence?: number;
  suggestedActions?: string[];
  timestamp: string;
}

export interface SupportSessionHistory {
  sessionId: string;
  messages: SupportHistoryMessage[];
  startedAt: string;
  lastActivityAt: string;
}

export interface SupportHistoryMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: string;
}

export interface SupportAgentStatus {
  service: string;
  status: string;
  version?: string;
  timestamp: string;
}

// =============================================================================
// UI Model (compatible with ChatMessage from chatbot.ts)
// =============================================================================

export interface SupportChatMessage {
  id: string;
  content: string;
  isFromBot: boolean;
  timestamp: Date;
  type: 'UserText' | 'BotText' | 'System';
  suggestedActions?: string[];
  isLoading?: boolean;
  isError?: boolean;
}

// =============================================================================
// API Functions
// =============================================================================

/**
 * Send a message to the SupportAgent and get a response.
 * Claude Haiku responds in 300ms-1.5s; timeout set to 15s for safety.
 */
export async function sendSupportMessage(
  request: SupportMessageRequest
): Promise<SupportMessageResponse> {
  const response = await apiClient.post<SupportMessageResponse>('/api/support/message', request, {
    timeout: 15_000,
  });
  return response.data;
}

/**
 * Get session history
 */
export async function getSupportSessionHistory(sessionId: string): Promise<SupportSessionHistory> {
  const response = await apiClient.get<SupportSessionHistory>(
    `/api/support/session/${encodeURIComponent(sessionId)}`
  );
  return response.data;
}

/**
 * Check SupportAgent health
 */
export async function getSupportAgentStatus(): Promise<SupportAgentStatus> {
  const response = await apiClient.get<SupportAgentStatus>('/api/support/status');
  return response.data;
}

// =============================================================================
// Transform helpers
// =============================================================================

/**
 * Convert a SupportMessageResponse to a UI SupportChatMessage
 */
export function supportResponseToMessage(response: SupportMessageResponse): SupportChatMessage {
  return {
    id: `support-bot-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    content: cleanBotResponse(response.response),
    isFromBot: true,
    timestamp: new Date(response.timestamp),
    type: 'BotText',
    suggestedActions: response.suggestedActions,
  };
}

/**
 * Create a user message for the UI
 */
export function createSupportUserMessage(content: string): SupportChatMessage {
  return {
    id: `support-user-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    content,
    isFromBot: false,
    timestamp: new Date(),
    type: 'UserText',
  };
}

/**
 * Create a loading placeholder
 */
export function createSupportLoadingMessage(): SupportChatMessage {
  return {
    id: `support-loading-${Date.now()}`,
    content: '',
    isFromBot: true,
    timestamp: new Date(),
    type: 'BotText',
    isLoading: true,
  };
}

// Default export
const supportAgentService = {
  sendSupportMessage,
  getSupportSessionHistory,
  getSupportAgentStatus,
  supportResponseToMessage,
  createSupportUserMessage,
  createSupportLoadingMessage,
};

export default supportAgentService;
