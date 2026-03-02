// =============================================================================
// useSupportAgent Hook — Support chatbot for OKLA platform assistance
// =============================================================================
// Connects to SupportAgent (Claude Haiku 4.5) for platform support & buyer guidance

'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import {
  type SupportChatMessage,
  sendSupportMessage,
  supportResponseToMessage,
  createSupportUserMessage,
  createSupportLoadingMessage,
} from '@/services/support-agent';

// =============================================================================
// Types
// =============================================================================

export interface UseSupportAgentOptions {
  /** Auto-send welcome message when first opened */
  autoWelcome?: boolean;
}

export interface UseSupportAgentReturn {
  messages: SupportChatMessage[];
  isOpen: boolean;
  isLoading: boolean;
  sessionId: string | null;
  error: string | null;

  open: () => void;
  close: () => void;
  toggle: () => void;
  sendMessage: (content: string) => Promise<void>;
  selectSuggestion: (text: string) => Promise<void>;
  clearError: () => void;
  resetChat: () => void;
}

// =============================================================================
// Constants
// =============================================================================

const SESSION_KEY = 'okla_support_session';
const MESSAGES_KEY = 'okla_support_messages';
const MAX_STORED = 50;

const WELCOME_MESSAGE: SupportChatMessage = {
  id: 'support-welcome',
  content:
    '¡Hola! 👋 Soy tu asistente de soporte de **OKLA**. Puedo ayudarte con:\n\n' +
    '• **Cómo usar la plataforma** (registro, publicar, KYC, pagos)\n' +
    '• **Orientación para compradores** (verificar vehículos, evitar estafas, proceso legal)\n' +
    '• **Preguntas frecuentes** sobre OKLA\n\n' +
    '¿En qué te puedo ayudar?',
  isFromBot: true,
  timestamp: new Date(),
  type: 'BotText',
  suggestedActions: [
    '¿Cómo publico un vehículo?',
    '¿Cómo verifico mi cuenta?',
    '¿Es seguro comprar en OKLA?',
    '¿Cuánto cuesta publicar?',
  ],
};

// =============================================================================
// Hook
// =============================================================================

export function useSupportAgent(
  options: UseSupportAgentOptions = {}
): UseSupportAgentReturn {
  const { autoWelcome = true } = options;

  const [messages, setMessages] = useState<SupportChatMessage[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const initializedRef = useRef(false);

  // ── Persistence ──────────────────────────────────────────────────────────

  const saveState = useCallback((sid: string | null, msgs: SupportChatMessage[]) => {
    try {
      if (sid) sessionStorage.setItem(SESSION_KEY, sid);
      const toStore = msgs.slice(-MAX_STORED).map(m => ({
        ...m,
        timestamp: m.timestamp instanceof Date ? m.timestamp.toISOString() : m.timestamp,
      }));
      sessionStorage.setItem(MESSAGES_KEY, JSON.stringify(toStore));
    } catch {
      // sessionStorage not available
    }
  }, []);

  const clearStorage = useCallback(() => {
    try {
      sessionStorage.removeItem(SESSION_KEY);
      sessionStorage.removeItem(MESSAGES_KEY);
    } catch {
      // ignore
    }
  }, []);

  // ── Restore on mount ────────────────────────────────────────────────────

  useEffect(() => {
    if (initializedRef.current) return;
    initializedRef.current = true;

    try {
      const savedSid = sessionStorage.getItem(SESSION_KEY);
      const savedMsgs = sessionStorage.getItem(MESSAGES_KEY);

      if (savedSid) setSessionId(savedSid);

      if (savedMsgs) {
        const parsed = JSON.parse(savedMsgs) as Array<
          SupportChatMessage & { timestamp: string }
        >;
        setMessages(
          parsed.map(m => ({ ...m, timestamp: new Date(m.timestamp) }))
        );
      } else if (autoWelcome) {
        setMessages([WELCOME_MESSAGE]);
      }
    } catch {
      if (autoWelcome) setMessages([WELCOME_MESSAGE]);
    }
  }, [autoWelcome]);

  // ── Send Message ─────────────────────────────────────────────────────────

  const handleSendMessage = useCallback(
    async (content: string) => {
      if (!content.trim() || isLoading) return;

      const userMsg = createSupportUserMessage(content.trim());
      const loadingMsg = createSupportLoadingMessage();

      setMessages(prev => {
        const updated = [...prev, userMsg, loadingMsg];
        saveState(sessionId, updated);
        return updated;
      });
      setIsLoading(true);
      setError(null);

      try {
        const response = await sendSupportMessage({
          sessionId: sessionId ?? undefined,
          message: content.trim(),
        });

        // Save session ID from first response
        if (!sessionId && response.sessionId) {
          setSessionId(response.sessionId);
        }

        const botMsg = supportResponseToMessage(response);

        setMessages(prev => {
          const updated = prev.filter(m => !m.isLoading);
          updated.push(botMsg);
          saveState(response.sessionId || sessionId, updated);
          return updated;
        });
      } catch (err) {
        setMessages(prev => prev.filter(m => !m.isLoading));

        const errorMsg: SupportChatMessage = {
          id: `support-error-${Date.now()}`,
          content: 'Lo siento, hubo un problema. Por favor intenta de nuevo.',
          isFromBot: true,
          timestamp: new Date(),
          type: 'System',
          isError: true,
        };

        setMessages(prev => {
          const updated = [...prev, errorMsg];
          saveState(sessionId, updated);
          return updated;
        });

        setError(
          err instanceof Error ? err.message : 'Error enviando mensaje'
        );
      } finally {
        setIsLoading(false);
      }
    },
    [isLoading, sessionId, saveState]
  );

  // ── Actions ──────────────────────────────────────────────────────────────

  const selectSuggestion = useCallback(
    async (text: string) => {
      await handleSendMessage(text);
    },
    [handleSendMessage]
  );

  const open = useCallback(() => {
    setIsOpen(true);
    if (messages.length === 0 && autoWelcome) {
      setMessages([WELCOME_MESSAGE]);
    }
  }, [messages.length, autoWelcome]);

  const close = useCallback(() => setIsOpen(false), []);

  const toggle = useCallback(() => {
    if (isOpen) close();
    else open();
  }, [isOpen, open, close]);

  const resetChat = useCallback(() => {
    setMessages([WELCOME_MESSAGE]);
    setSessionId(null);
    setError(null);
    clearStorage();
  }, [clearStorage]);

  const clearError = useCallback(() => setError(null), []);

  return {
    messages,
    isOpen,
    isLoading,
    sessionId,
    error,
    open,
    close,
    toggle,
    sendMessage: handleSendMessage,
    selectSuggestion,
    clearError,
    resetChat,
  };
}
