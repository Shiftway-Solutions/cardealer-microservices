// =============================================================================
// useChatbot Hook — Main hook for OKLA chatbot interaction
// =============================================================================

'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import { useAuth } from '@/hooks/use-auth';
import {
  type ChatMessage,
  type StartSessionResponse,
  type QuickReply,
  type ChatbotResponse,
  startChatSession,
  sendChatMessage,
  endChatSession,
  transferToAgent,
  acceptDisclosure,
  botResponseToMessage,
  createUserMessage,
  createLoadingMessage,
} from '@/services/chatbot';

// =============================================================================
// Types
// =============================================================================

export interface UseChatbotOptions {
  /** Dealer ID to scope the chat session */
  dealerId?: string;
  /** Dealer display name — stored in localStorage so the /mensajes sidebar can list it */
  dealerName?: string;
  /** Whether to auto-start session when hook mounts */
  autoStart?: boolean;
  /** Max retries for failed messages */
  maxRetries?: number;
  /** Callback when a lead is generated */
  onLeadGenerated?: (leadId: string) => void;
  /** Callback when transfer to agent happens */
  onTransfer?: (agentName?: string) => void;
  /** Callback when interaction limit is reached */
  onLimitReached?: () => void;
}

export interface UseChatbotReturn {
  // State
  messages: ChatMessage[];
  isOpen: boolean;
  isLoading: boolean;
  isConnected: boolean;
  sessionToken: string | null;
  botName: string;
  botAvatarUrl: string | null;
  remainingInteractions: number;
  isLimitReached: boolean;
  error: string | null;
  quickReplies: QuickReply[];

  // Actions
  open: () => void;
  close: () => void;
  toggle: () => void;
  sendMessage: (content: string) => Promise<void>;
  selectQuickReply: (reply: QuickReply) => Promise<void>;
  startSession: () => Promise<void>;
  endSession: () => Promise<void>;
  requestTransfer: (reason?: string) => Promise<void>;
  clearError: () => void;
  resetChat: () => void;
}

// =============================================================================
// Constants
// =============================================================================

const MAX_STORED_MESSAGES = 50;

// =============================================================================
// Hook
// =============================================================================

export function useChatbot(options: UseChatbotOptions = {}): UseChatbotReturn {
  const {
    dealerId,
    dealerName,
    autoStart = false,
    maxRetries: _maxRetries = 2,
    onLeadGenerated,
    onTransfer,
    onLimitReached,
  } = options;

  // ── Dealer-scoped storage keys ──────────────────────────────────────────
  // Using dealerId in the key prevents cross-dealer session contamination:
  // opening Dealer B's bot won't restore Dealer A's expired session token.
  // Authenticated: localStorage (persist across sessions for /mensajes sidebar).
  // Unauthenticated: sessionStorage (ephemeral — cleared when tab closes).
  const { isAuthenticated } = useAuth();
  const storage =
    typeof window !== 'undefined' ? (isAuthenticated ? localStorage : sessionStorage) : null;
  const SESSION_KEY = dealerId ? `okla_chat_session_${dealerId}` : 'okla_chat_session_default';
  const MESSAGES_KEY = dealerId ? `okla_chat_messages_${dealerId}` : 'okla_chat_messages_default';
  const BOT_NAME_KEY = dealerId ? `okla_chat_botname_${dealerId}` : 'okla_chat_botname_default';
  const DEALER_NAME_KEY = dealerId ? `okla_chat_dealername_${dealerId}` : null;

  // State
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isConnected, setIsConnected] = useState(false);
  const [sessionToken, setSessionToken] = useState<string | null>(null);
  // Initialize to '' (empty/falsy) so DealerBotPanel's displayName fallback works:
  //   chat.botName || `Asistente de ${dealerName}`
  // 'OKLA Bot' was truthy and always suppressed the fallback.
  const [botName, setBotName] = useState('');
  const [botAvatarUrl, setBotAvatarUrl] = useState<string | null>(null);
  const [remainingInteractions, setRemainingInteractions] = useState(0);
  const [isLimitReached, setIsLimitReached] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [quickReplies, setQuickReplies] = useState<QuickReply[]>([]);

  // Refs
  const retryCountRef = useRef(0);
  const sessionStartedRef = useRef(false);

  // ─────────────────────────────────────────────────────────────────────────
  // Session persistence
  // ─────────────────────────────────────────────────────────────────────────

  const saveSession = useCallback(
    (token: string, bName?: string) => {
      try {
        storage?.setItem(SESSION_KEY, token);
        if (bName) storage?.setItem(BOT_NAME_KEY, bName);
        if (DEALER_NAME_KEY && dealerName) storage?.setItem(DEALER_NAME_KEY, dealerName);
      } catch {
        // storage not available
      }
    },
    [storage, SESSION_KEY, BOT_NAME_KEY, DEALER_NAME_KEY, dealerName]
  );

  const saveMessages = useCallback(
    (msgs: ChatMessage[]) => {
      try {
        const toStore = msgs.slice(-MAX_STORED_MESSAGES).map(m => ({
          ...m,
          timestamp: m.timestamp.toISOString(),
        }));
        storage?.setItem(MESSAGES_KEY, JSON.stringify(toStore));
      } catch {
        // storage not available
      }
    },
    [storage, MESSAGES_KEY]
  );

  const clearStorage = useCallback(() => {
    try {
      storage?.removeItem(SESSION_KEY);
      storage?.removeItem(MESSAGES_KEY);
      storage?.removeItem(BOT_NAME_KEY);
      if (DEALER_NAME_KEY) storage?.removeItem(DEALER_NAME_KEY);
    } catch {
      // storage not available
    }
  }, [storage, SESSION_KEY, MESSAGES_KEY, BOT_NAME_KEY, DEALER_NAME_KEY]);

  // ─────────────────────────────────────────────────────────────────────────
  // Restore session on mount
  // ─────────────────────────────────────────────────────────────────────────

  useEffect(() => {
    // Restore dealer-scoped session on mount.
    // SESSION_KEY / MESSAGES_KEY / BOT_NAME_KEY are stable for the lifetime of
    // this component instance (parent mounts with key={conversationId}).
    try {
      const savedToken = storage?.getItem(SESSION_KEY) ?? null;
      const savedMessages = storage?.getItem(MESSAGES_KEY) ?? null;
      const savedBotName = storage?.getItem(BOT_NAME_KEY) ?? null;

      if (savedToken) {
        setSessionToken(savedToken);
        setIsConnected(true);
        // Ensure disclosure consent is accepted for restored sessions (idempotent call)
        acceptDisclosure(savedToken).catch(() => {
          /* non-blocking */
        });
      }

      if (savedBotName) {
        setBotName(savedBotName);
      }

      if (savedMessages) {
        const parsed = JSON.parse(savedMessages) as Array<ChatMessage & { timestamp: string }>;
        setMessages(
          parsed.map(m => ({
            ...m,
            timestamp: new Date(m.timestamp),
          }))
        );
      }
    } catch {
      // Failed to restore — start fresh
    }
    // run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ─────────────────────────────────────────────────────────────────────────
  // Start session
  // ─────────────────────────────────────────────────────────────────────────

  const handleStartSession = useCallback(async () => {
    if (sessionStartedRef.current || sessionToken) return;
    sessionStartedRef.current = true;

    try {
      setIsLoading(true);
      setError(null);

      // SEM FIX: Attach persisted UTM params to chat session for lead attribution
      let utmFields: Record<string, string | undefined> = {};
      try {
        const { getAdParams } = await import('@/lib/ad-params');
        const adParams = getAdParams();
        if (adParams) {
          utmFields = {
            utmSource: adParams.utm_source,
            utmMedium: adParams.utm_medium,
            utmCampaign: adParams.utm_campaign,
            utmTerm: adParams.utm_term,
            utmContent: adParams.utm_content,
            gclid: adParams.gclid,
            landingPage: adParams.landing_page,
          };
        }
      } catch {
        /* ad-params not available */
      }

      const result: StartSessionResponse = await startChatSession({
        sessionType: 'WebChat',
        channel: 'web',
        language: 'es',
        dealerId: dealerId,
        deviceType: /Mobi|Android/i.test(navigator.userAgent) ? 'mobile' : 'desktop',
        userAgent: navigator.userAgent,
        ...utmFields,
      });

      const resolvedBotName = result.botName || '';
      setSessionToken(result.sessionToken);
      setBotName(resolvedBotName);
      setBotAvatarUrl(result.botAvatarUrl || null);
      setRemainingInteractions(result.remainingInteractions);
      setIsConnected(true);
      saveSession(result.sessionToken, resolvedBotName);

      // REMARKETING FIX: Fire pixel events on chat session start.
      // Creates remarketing audiences of "high-intent users who started a chat".
      try {
        const { trackChatStart } = await import('@/lib/retargeting-pixels');
        trackChatStart({ dealerId, channel: 'web' });
      } catch {
        /* silently fail */
      }

      // Build initial messages array
      const initialMessages: ChatMessage[] = [];

      // Show disclosure message first if backend requires consent
      if (result.disclosureMessage) {
        const disclosureMsg: ChatMessage = {
          id: `disclosure-${Date.now()}`,
          content: result.disclosureMessage,
          isFromBot: true,
          timestamp: new Date(),
          type: 'BotText',
        };
        initialMessages.push(disclosureMsg);
      }

      // Add welcome message
      const welcomeMsg: ChatMessage = {
        id: `welcome-${Date.now()}`,
        content:
          result.welcomeMessage ||
          '¡Hola! 👋 Soy OKLA Bot, tu asistente para encontrar el vehículo perfecto en República Dominicana. ¿En qué te puedo ayudar?',
        isFromBot: true,
        timestamp: new Date(),
        type: 'BotText',
        quickReplies: result.initialQuickReplies,
      };
      initialMessages.push(welcomeMsg);

      setMessages(initialMessages);
      saveMessages(initialMessages);

      // Auto-accept disclosure (implicit consent: "al continuar esta conversación, aceptas")
      if (result.requiresConsent) {
        try {
          await acceptDisclosure(result.sessionToken);
        } catch {
          // Non-blocking — if accept fails, user will see the error on first message
        }
      }

      if (result.initialQuickReplies?.length) {
        setQuickReplies(result.initialQuickReplies);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No se pudo conectar al chat';
      setError(message);
      sessionStartedRef.current = false;
    } finally {
      setIsLoading(false);
    }
  }, [dealerId, sessionToken, saveSession, saveMessages]);

  // ─────────────────────────────────────────────────────────────────────────
  // Send message
  // ─────────────────────────────────────────────────────────────────────────

  const handleSendMessage = useCallback(
    async (content: string) => {
      if (!sessionToken || !content.trim() || isLoading || isLimitReached) return;

      const userMsg = createUserMessage(content.trim());
      const loadingMsg = createLoadingMessage();

      setMessages(prev => {
        const updated = [...prev, userMsg, loadingMsg];
        saveMessages(updated);
        return updated;
      });
      setQuickReplies([]);
      setIsLoading(true);
      setError(null);

      try {
        const response: ChatbotResponse = await sendChatMessage({
          sessionToken,
          message: content.trim(),
        });

        const botMsg = botResponseToMessage(response);

        setMessages(prev => {
          const updated = prev.filter(m => !m.isLoading);
          updated.push(botMsg);
          saveMessages(updated);
          return updated;
        });

        setRemainingInteractions(response.remainingInteractions);

        if (response.quickReplies?.length) {
          setQuickReplies(response.quickReplies);
        }

        if (response.interactionLimitReached) {
          setIsLimitReached(true);
          onLimitReached?.();
        }

        if (response.leadGenerated && response.leadId) {
          onLeadGenerated?.(response.leadId);
        }

        retryCountRef.current = 0;
      } catch (err) {
        // Remove loading message
        setMessages(prev => prev.filter(m => !m.isLoading));

        // Auto-reconnect: if the session expired (404), clear storage and restart.
        // This handles the case where the user re-opens the bot after the backend
        // session has expired, but the old token was still in sessionStorage.
        const httpStatus = (err as { response?: { status?: number } })?.response?.status;
        if (httpStatus === 404) {
          clearStorage();
          setSessionToken(null);
          setIsConnected(false);
          sessionStartedRef.current = false;
          const reconnectMsg: ChatMessage = {
            id: `reconnect-${Date.now()}`,
            content: 'Tu sesión había expirado. Reconectando automáticamente…',
            isFromBot: true,
            timestamp: new Date(),
            type: 'System',
          };
          setMessages(prev => [...prev, reconnectMsg]);
          // handleStartSession will start a fresh session with a new welcome message
          await handleStartSession();
          return;
        }

        const errorMsg: ChatMessage = {
          id: `error-${Date.now()}`,
          content:
            'Lo siento, hubo un problema al procesar tu mensaje. Por favor intenta de nuevo.',
          isFromBot: true,
          timestamp: new Date(),
          type: 'System',
          isError: true,
        };

        setMessages(prev => {
          const updated = [...prev, errorMsg];
          saveMessages(updated);
          return updated;
        });

        setError(err instanceof Error ? err.message : 'Error enviando mensaje');
      } finally {
        setIsLoading(false);
      }
    },
    [
      sessionToken,
      isLoading,
      isLimitReached,
      onLeadGenerated,
      onLimitReached,
      saveMessages,
      clearStorage,
      handleStartSession,
    ]
  );

  // ─────────────────────────────────────────────────────────────────────────
  // Quick reply selection
  // ─────────────────────────────────────────────────────────────────────────

  const selectQuickReply = useCallback(
    async (reply: QuickReply) => {
      await handleSendMessage(reply.payload || reply.text);
    },
    [handleSendMessage]
  );

  // ─────────────────────────────────────────────────────────────────────────
  // End session
  // ─────────────────────────────────────────────────────────────────────────

  const handleEndSession = useCallback(async () => {
    if (!sessionToken) return;

    try {
      await endChatSession({ sessionToken, endReason: 'user_closed' });
    } catch {
      // Best effort — session will expire on backend
    } finally {
      setSessionToken(null);
      setIsConnected(false);
      setMessages([]);
      setQuickReplies([]);
      setIsLimitReached(false);
      setRemainingInteractions(0);
      clearStorage();
      sessionStartedRef.current = false;
    }
  }, [sessionToken, clearStorage]);

  // ─────────────────────────────────────────────────────────────────────────
  // Transfer to agent
  // ─────────────────────────────────────────────────────────────────────────

  const requestTransfer = useCallback(
    async (reason?: string) => {
      if (!sessionToken) return;

      try {
        setIsLoading(true);
        const result = await transferToAgent({
          sessionToken,
          transferReason: reason || 'Solicitud del usuario',
        });

        const transferMsg: ChatMessage = {
          id: `transfer-${Date.now()}`,
          content:
            result.message ||
            `Tu conversación ha sido transferida a ${result.agentName || 'un agente'}. Tiempo estimado de espera: ${result.estimatedWaitTimeMinutes || 5} minutos.`,
          isFromBot: true,
          timestamp: new Date(),
          type: 'System',
        };

        setMessages(prev => [...prev, transferMsg]);
        onTransfer?.(result.agentName ?? undefined);
      } catch {
        setError('No se pudo transferir al agente. Por favor intenta de nuevo.');
      } finally {
        setIsLoading(false);
      }
    },
    [sessionToken, onTransfer]
  );

  // ─────────────────────────────────────────────────────────────────────────
  // Open / Close / Toggle
  // ─────────────────────────────────────────────────────────────────────────

  const open = useCallback(() => {
    setIsOpen(true);
    if (!sessionToken && !sessionStartedRef.current) {
      handleStartSession();
    }
  }, [sessionToken, handleStartSession]);

  const close = useCallback(() => {
    setIsOpen(false);
  }, []);

  const toggle = useCallback(() => {
    if (isOpen) {
      close();
    } else {
      open();
    }
  }, [isOpen, open, close]);

  // ─────────────────────────────────────────────────────────────────────────
  // Reset chat
  // ─────────────────────────────────────────────────────────────────────────

  const resetChat = useCallback(async () => {
    await handleEndSession();
    // Will auto-start on next open
  }, [handleEndSession]);

  const clearError = useCallback(() => setError(null), []);

  // ─────────────────────────────────────────────────────────────────────────
  // Auto-start if configured
  // ─────────────────────────────────────────────────────────────────────────

  useEffect(() => {
    if (autoStart && !sessionToken && !sessionStartedRef.current) {
      handleStartSession();
    }
  }, [autoStart, sessionToken, handleStartSession]);

  // ─────────────────────────────────────────────────────────────────────────
  // Return
  // ─────────────────────────────────────────────────────────────────────────

  return {
    // State
    messages,
    isOpen,
    isLoading,
    isConnected,
    sessionToken,
    botName,
    botAvatarUrl,
    remainingInteractions,
    isLimitReached,
    error,
    quickReplies,

    // Actions
    open,
    close,
    toggle,
    sendMessage: handleSendMessage,
    selectQuickReply,
    startSession: handleStartSession,
    endSession: handleEndSession,
    requestTransfer,
    clearError,
    resetChat,
  };
}
