// =============================================================================
// Chatbot Service — API Client for ChatbotService (.NET 8)
// =============================================================================
// Handles all communication with the OKLA chatbot (Llama 3 8B fine-tuned)

import { apiClient } from '@/lib/api-client';
import { cleanBotResponse } from '@/lib/chat-format';

// =============================================================================
// Types & Interfaces
// =============================================================================

export type SessionType = 'WebChat' | 'WhatsApp' | 'Messenger' | 'Widget';
export type MessageType = 'UserText' | 'UserMedia' | 'BotText' | 'BotCard' | 'System';
export type SessionStatus = 'Active' | 'Ended' | 'Transferred' | 'Expired' | 'LimitReached';

export type IntentCategory =
  | 'Greeting'
  | 'VehicleSearch'
  | 'VehicleDetails'
  | 'VehicleComparison'
  | 'PriceInquiry'
  | 'Financing'
  | 'TestDrive'
  | 'Appointment'
  | 'Negotiation'
  | 'TradeIn'
  | 'Insurance'
  | 'Documentation'
  | 'Warranty'
  | 'DealerInfo'
  | 'Complaint'
  | 'Farewell'
  | 'OutOfScope'
  | 'SmallTalk'
  | 'Fallback'
  | 'Unknown';

export type ConfidenceLevel = 'High' | 'Medium' | 'Low' | 'VeryLow';

export interface QuickReply {
  text: string;
  payload?: string;
  iconUrl?: string;
}

export interface VehicleCard {
  vehicleId: string;
  title: string;
  subtitle: string;
  imageUrl: string;
  price: number;
  originalPrice?: number;
  isOnSale: boolean;
  detailsUrl?: string;
  highlights?: string[];
  actions?: QuickReply[];
}

// --- Request DTOs ---

export interface StartSessionRequest {
  userId?: string;
  userName?: string;
  userEmail?: string;
  userPhone?: string;
  sessionType?: SessionType;
  channel?: string;
  channelUserId?: string;
  userAgent?: string;
  ipAddress?: string;
  deviceType?: string;
  language?: string;
  dealerId?: string;
  // SEM FIX: UTM attribution so chat-generated leads can be traced to campaigns
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  utmTerm?: string;
  utmContent?: string;
  gclid?: string;
  landingPage?: string;
}

export interface SendMessageRequest {
  sessionToken: string;
  message: string;
  type?: MessageType;
  mediaUrl?: string;
}

export interface EndSessionRequest {
  sessionToken: string;
  endReason?: string;
}

export interface TransferToAgentRequest {
  sessionToken: string;
  transferReason?: string;
}

// --- Response DTOs ---

export interface StartSessionResponse {
  sessionId: string;
  sessionToken: string;
  welcomeMessage: string;
  botName: string;
  botAvatarUrl?: string;
  initialQuickReplies?: QuickReply[];
  maxInteractionsPerSession: number;
  remainingInteractions: number;
  disclosureMessage?: string;
  privacyPolicyUrl?: string;
  requiresConsent?: boolean;
}

export interface ChatbotResponse {
  messageId: string;
  response: string;
  intentName?: string;
  intentCategory: IntentCategory;
  confidenceScore: number;
  confidenceLevel: ConfidenceLevel;
  quickReplies?: QuickReply[];
  vehicleCard?: VehicleCard;
  vehicleCards?: VehicleCard[];
  isFallback: boolean;
  responseTimeMs: number;
  remainingInteractions: number;
  interactionLimitReached: boolean;
  limitReachedMessage?: string;
  leadGenerated: boolean;
  leadId?: string;
}

export interface ChatSessionDto {
  id: string;
  sessionToken: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  sessionType: SessionType;
  channel: string;
  status: SessionStatus;
  messageCount: number;
  interactionCount: number;
  maxInteractionsPerSession: number;
  interactionLimitReached: boolean;
  currentVehicleId?: string;
  currentVehicleName?: string;
  leadId?: string;
  createdAt: string;
  lastActivityAt: string;
  endedAt?: string;
  sessionDurationSeconds: number;
}

export interface ChatMessageDto {
  id: string;
  sessionId: string;
  type: MessageType;
  content: string;
  mediaUrl?: string;
  intentName?: string;
  intentCategory: IntentCategory;
  confidenceScore: number;
  botResponse?: string;
  isFromBot: boolean;
  responseTimeMs: number;
  consumedInteraction: boolean;
  createdAt: string;
}

export interface TransferResult {
  success: boolean;
  agentName?: string;
  estimatedWaitTimeMinutes?: number;
  message: string;
}

// =============================================================================
// Chat Message (UI model)
// =============================================================================

export interface ChatMessage {
  id: string;
  content: string;
  isFromBot: boolean;
  timestamp: Date;
  type: MessageType;
  intentCategory?: IntentCategory;
  quickReplies?: QuickReply[];
  vehicleCard?: VehicleCard;
  vehicleCards?: VehicleCard[];
  isLoading?: boolean;
  isError?: boolean;
}

// =============================================================================
// API Functions
// =============================================================================

/**
 * Accept bot disclosure and privacy policy.
 * Must be called after startChatSession and before sending messages.
 */
export async function acceptDisclosure(sessionToken: string): Promise<{ success: boolean }> {
  const response = await apiClient.post<{ success: boolean }>('/api/chat/accept-disclosure', {
    sessionToken,
  });
  return response.data;
}

/**
 * Start a new chat session with the OKLA bot
 */
export async function startChatSession(
  request: StartSessionRequest
): Promise<StartSessionResponse> {
  const response = await apiClient.post<StartSessionResponse>('/api/chat/start', {
    ...request,
    sessionType: request.sessionType ?? 'WebChat',
    channel: request.channel ?? 'web',
    language: request.language ?? 'es',
  });
  return response.data;
}

/**
 * Send a message to the chatbot and get a response.
 * Claude Sonnet responds in 1-5s; timeout set to 30s for safety.
 */
export async function sendChatMessage(request: SendMessageRequest): Promise<ChatbotResponse> {
  const response = await apiClient.post<ChatbotResponse>(
    '/api/chat/message',
    {
      ...request,
      type: request.type ?? 'UserText',
    },
    { timeout: 30_000 }
  );
  return response.data;
}

/**
 * End a chat session
 */
export async function endChatSession(request: EndSessionRequest): Promise<{ message: string }> {
  const response = await apiClient.post<{ message: string }>('/api/chat/end', request);
  return response.data;
}

/**
 * Transfer session to a human agent
 */
export async function transferToAgent(request: TransferToAgentRequest): Promise<TransferResult> {
  const response = await apiClient.post<TransferResult>('/api/chat/transfer', request);
  return response.data;
}

/**
 * Get session details by token
 */
export async function getChatSession(sessionToken: string): Promise<ChatSessionDto> {
  const response = await apiClient.get<ChatSessionDto>(
    `/api/chat/session?token=${encodeURIComponent(sessionToken)}`
  );
  return response.data;
}

/**
 * Get all messages for a session
 */
export async function getSessionMessages(sessionToken: string): Promise<ChatMessageDto[]> {
  const response = await apiClient.get<ChatMessageDto[]>(
    `/api/chat/session/${encodeURIComponent(sessionToken)}/messages`
  );
  return response.data;
}

/**
 * Get active sessions count (admin)
 */
export async function getActiveSessionsCount(): Promise<number> {
  const response = await apiClient.get<{ count: number }>('/api/chat/sessions/active/count');
  return response.data.count;
}

/**
 * Check chatbot health
 */
export async function getChatbotHealth(): Promise<{
  status: string;
  timestamp: string;
}> {
  const response = await apiClient.get<{ status: string; timestamp: string }>('/api/chat/health');
  return response.data;
}

// =============================================================================
// ChatBot Configuration (Dealer management)
// =============================================================================

export interface ChatbotConfigurationDto {
  id: string;
  dealerId: string;
  name: string;
  isActive: boolean;
  plan: string;
  freeInteractionsPerMonth: number;
  costPerInteraction: number;
  maxInteractionsPerSession: number;
  maxInteractionsPerUserPerDay: number;
  maxInteractionsPerUserPerMonth: number;
  botName: string;
  botAvatarUrl: string;
  welcomeMessage: string;
  enableWebChat: boolean;
  enableWhatsApp: boolean;
  enableFacebook: boolean;
  enableAutoInventorySync: boolean;
  enableAutoReports: boolean;
  enableAutoLearning: boolean;
  enableHealthMonitoring: boolean;
}

/**
 * Get chatbot configuration for a dealer
 */
export async function getChatbotConfigByDealer(dealerId: string): Promise<ChatbotConfigurationDto> {
  const response = await apiClient.get<ChatbotConfigurationDto>(
    `/api/configuration/dealer/${encodeURIComponent(dealerId)}`
  );
  return response.data;
}

// =============================================================================
// Transform helpers
// =============================================================================

/**
 * Convert a ChatbotResponse to a UI ChatMessage.
 *
 * Cleans the raw response text:
 *  - Strips JSON wrappers (`{"response": "..."}`) the LLM may produce.
 *  - Normalises escaped newlines (`\\n` → `\n`).
 */
export function botResponseToMessage(response: ChatbotResponse): ChatMessage {
  return {
    id: response.messageId,
    content: cleanBotResponse(response.response),
    isFromBot: true,
    timestamp: new Date(),
    type: 'BotText',
    intentCategory: response.intentCategory,
    quickReplies: response.quickReplies,
    vehicleCard: response.vehicleCard,
    vehicleCards: response.vehicleCards,
  };
}

/**
 * Create a user message for the UI
 */
export function createUserMessage(content: string): ChatMessage {
  return {
    id: `user-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`,
    content,
    isFromBot: false,
    timestamp: new Date(),
    type: 'UserText',
  };
}

/**
 * Create a loading placeholder message
 */
export function createLoadingMessage(): ChatMessage {
  return {
    id: `loading-${Date.now()}`,
    content: '',
    isFromBot: true,
    timestamp: new Date(),
    type: 'BotText',
    isLoading: true,
  };
}

/**
 * Format price for Dominican market (RD$)
 */
export function formatVehiclePrice(price: number): string {
  return `RD$ ${price.toLocaleString('es-DO')}`;
}
