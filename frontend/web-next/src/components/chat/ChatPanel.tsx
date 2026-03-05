'use client';

import { useRef, useEffect } from 'react';
import { ChatHeader } from './ChatHeader';
import { ChatMessageBubble } from './ChatMessageBubble';
import { ChatInput } from './ChatInput';
import { type ChatMessage, type QuickReply } from '@/services/chatbot';
import { AlertCircle, WifiOff } from 'lucide-react';

interface ChatPanelProps {
  messages: ChatMessage[];
  isOpen: boolean;
  isLoading: boolean;
  isConnected: boolean;
  isLimitReached: boolean;
  botName: string;
  botAvatarUrl: string | null;
  remainingInteractions: number;
  error: string | null;
  onSend: (message: string) => void;
  onQuickReply: (reply: QuickReply) => void;
  onClose: () => void;
  onMinimize: () => void;
  onReset: () => void;
  onTransfer: () => void;
  onClearError: () => void;
}

export function ChatPanel({
  messages,
  isOpen,
  isLoading,
  isConnected,
  isLimitReached,
  botName,
  botAvatarUrl,
  remainingInteractions,
  error,
  onSend,
  onQuickReply,
  onClose,
  onMinimize,
  onReset,
  onTransfer,
  onClearError,
}: ChatPanelProps) {
  const scrollRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTo({
        top: scrollRef.current.scrollHeight,
        behavior: 'smooth',
      });
    }
  }, [messages]);

  // Close on Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed right-4 bottom-20 z-[9999] flex w-[380px] flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl transition-all duration-300 max-sm:inset-0 max-sm:right-0 max-sm:bottom-0 max-sm:w-full max-sm:rounded-none dark:border-gray-700 dark:bg-gray-950"
      style={{ height: 'min(600px, calc(100vh - 120px))' }}
      role="dialog"
      aria-label="Chat con OKLA Bot"
    >
      {/* Header */}
      <ChatHeader
        botName={botName}
        isConnected={isConnected}
        onClose={onClose}
        onMinimize={onMinimize}
        onReset={onReset}
        onTransfer={onTransfer}
      />

      {/* Connection error banner */}
      {error && (
        <div className="flex items-center gap-2 bg-red-50 px-4 py-2 text-xs text-red-700 dark:bg-red-950 dark:text-red-300">
          <AlertCircle className="h-3.5 w-3.5 shrink-0" />
          <span className="flex-1">{error}</span>
          <button onClick={onClearError} className="text-red-500 underline hover:no-underline">
            Cerrar
          </button>
        </div>
      )}

      {/* Messages area */}
      <div
        ref={scrollRef}
        className="flex-1 overflow-y-auto py-3"
        role="log"
        aria-live="polite"
        aria-label="Mensajes del chat"
      >
        {messages.length === 0 && !isConnected && (
          <div className="flex h-full flex-col items-center justify-center gap-3 px-6 text-center">
            <WifiOff className="h-10 w-10 text-gray-300 dark:text-gray-600" />
            <p className="text-sm text-gray-500 dark:text-gray-400">Conectando con OKLA Bot...</p>
          </div>
        )}

        {messages.map(msg => (
          <ChatMessageBubble
            key={msg.id}
            message={msg}
            botName={botName}
            botAvatarUrl={botAvatarUrl}
            onQuickReply={onQuickReply}
          />
        ))}
      </div>

      {/* Remaining interactions indicator */}
      {isConnected && remainingInteractions > 0 && remainingInteractions <= 5 && (
        <div className="border-t border-gray-100 bg-amber-50 px-4 py-1.5 text-center text-[11px] text-amber-700 dark:border-gray-800 dark:bg-amber-950 dark:text-amber-300">
          {remainingInteractions}{' '}
          {remainingInteractions === 1 ? 'interacción restante' : 'interacciones restantes'}
        </div>
      )}

      {/* Limit reached banner */}
      {isLimitReached && (
        <div className="border-t border-gray-100 bg-gray-50 px-4 py-3 text-center dark:border-gray-800 dark:bg-gray-900">
          <p className="text-xs text-gray-600 dark:text-gray-400">
            Has alcanzado el límite de interacciones.
          </p>
          <button
            onClick={onTransfer}
            className="mt-1.5 text-xs font-medium text-[#00A870] hover:underline"
          >
            Hablar con un agente →
          </button>
        </div>
      )}

      {/* Input */}
      <ChatInput
        onSend={onSend}
        disabled={!isConnected || isLimitReached}
        isLoading={isLoading}
        placeholder={
          isLimitReached
            ? 'Límite alcanzado'
            : !isConnected
              ? 'Conectando...'
              : 'Escribe tu mensaje...'
        }
      />
    </div>
  );
}
