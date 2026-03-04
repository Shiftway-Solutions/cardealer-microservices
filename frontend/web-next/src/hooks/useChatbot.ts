// =============================================================================
// useChatbot Hook — Main hook for OKLA chatbot interaction
// =============================================================================

'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import {
  type ChatMessage,
  type StartSessionResponse,
  type QuickReply,
  type ChatbotResponse,
  startChatSession,
  sendChatMessage,
  endChatSession,
  transferToAgent,
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
    autoStart = false,
    maxRetries: _maxRetries = 2,
    onLeadGenerated,
    onTransfer,
    onLimitReached,
  } = options;

  // ── Dealer-scoped storage keys ──────────────────────────────────────────
  // Using dealerId in the key prevents cross-dealer session contamination:
  // opening Dealer B's bot won't restore Dealer A's expired session token.
  const SESSION_KEY = dealerId
    ? `okla_chat_session_${dealerId}`
    : 'okla_chat_session_default';
  const MESSAGES_KEY = dealerId
    ? `okla_chat_messages_${dealerId}`
    : 'okla_chat_messages_default';
  const BOT_NAME_KEY = dealerId
    ? `okla_chat_botname_${dealerId}`
    : 'okla_chat_botname_default';

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
        sessionStorage.setItem(SESSION_KEY, token);
        if (bName) sessionStorage.setItem(BOT_NAME_KEY, bName);
      } catch {
        // sessionStorage not available
      }
    },
    [SESSION_KEY, BOT_NAME_KEY]
  );

  const saveMessages = useCallback(
    (msgs: ChatMessage[]) => {
      try {
        const toStore = msgs.slice(-MAX_STORED_MESSAGES).map(m => ({
          ...m,
          timestamp: m.timestamp.toISOString(),
        }));
        sessionStorage.setItem(MESSAGES_KEY, JSON.stringify(toStore));
      } catch {
        // sessionStorage not available
      }
    },
    [MESSAGES_KEY]
  );

  const clearStorage = useCallback(() => {
    try {
      sessionStorage.removeItem(SESSION_KEY);
      sessionStorage.removeItem(MESSAGES_KEY);
      sessionStorage.removeItem(BOT_NAME_KEY);
    } catch {
      // sessionStorage not available
    }
  }, [SESSION_KEY, MESSAGES_KEY, BOT_NAME_KEY]);

  // ─────────────────────────────────────────────────────────────────────────
  // Restore session on mount
  // ─────────────────────────────────────────────────────────────────────────

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    // Restore dealer-scoped session on mount.
    // SESSION_KEY / MESSAGES_KEY / BOT_NAME_KEY are stable for the lifetime of
    // this component instance (parent mounts with key={conversationId}).
    try {
      const savedToken = sessionStorage.getItem(SESSION_KEY);
      const savedMessages = sessionStorage.getItem(MESSAGES_KEY);
      const savedBotName = sessionStorage.getItem(BOT_NAME_KEY);

      if (savedToken) {
        setSessionToken(savedToken);
        setIsConnected(true);
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

      const result: StartSessionResponse = await startChatSession({
        sessionType: 'WebChat',
        channel: 'web',
        language: 'es',
        dealerId: dealerId,
        deviceType: /Mobi|Android/i.test(navigator.userAgent) ? 'mobile' : 'desktop',
        userAgent: navigator.userAgent,
      });

      const resolvedBotName = result.botName || '';
      setSessionToken(result.sessionToken);
      setBotName(resolvedBotName);
      setBotAvatarUrl(result.botAvatarUrl || null);
      setRemainingInteractions(result.remainingInteractions);
      setIsConnected(true);
      saveSession(result.sessionToken, resolvedBotName);

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

      setMessages([welcomeMsg]);
      saveMessages([welcomeMsg]);

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
  }, [autoStart, sessionToken, handleStartSession]); // eslint-disable-line react-hooks/exhaustive-deps

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
