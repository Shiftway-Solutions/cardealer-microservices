'use client';

/**
 * SupportAgentWidget — Global floating support chatbot.
 *
 * Uses SupportAgent (Claude Haiku 4.5) for platform help & buyer guidance.
 * Appears on ALL main pages as a green floating bubble (bottom-right).
 */

import { useRef, useEffect } from 'react';
import Link from 'next/link';
import { useSupportAgent } from '@/hooks/useSupportAgent';
import { useAuth } from '@/hooks/use-auth';
import { ChatInput } from './ChatInput';
import { BotMessageContent } from './BotMessageContent';
import { X, RotateCcw, Minus, Headphones, AlertCircle, WifiOff, Lock } from 'lucide-react';
import type { SupportChatMessage } from '@/services/support-agent';

// =============================================================================
// Support Chat Panel (inline — specific to SupportAgent)
// =============================================================================

function SupportMessageBubble({
  message,
  onSuggestion,
}: {
  message: SupportChatMessage;
  onSuggestion?: (text: string) => void;
}) {
  // Loading state
  if (message.isLoading) {
    return (
      <div className="flex gap-2.5 px-4 py-1.5">
        <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[#00A870]/10">
          <Headphones className="h-3.5 w-3.5 text-[#00A870]" />
        </div>
        <div className="max-w-[80%] rounded-2xl rounded-tl-sm bg-gray-100 px-4 py-3 dark:bg-gray-800">
          <div className="flex items-center gap-1">
            <span className="h-2 w-2 animate-bounce rounded-full bg-gray-400 [animation-delay:-0.3s]" />
            <span className="h-2 w-2 animate-bounce rounded-full bg-gray-400 [animation-delay:-0.15s]" />
            <span className="h-2 w-2 animate-bounce rounded-full bg-gray-400" />
          </div>
        </div>
      </div>
    );
  }

  // Error
  if (message.isError) {
    return (
      <div className="flex gap-2.5 px-4 py-1.5">
        <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-red-100">
          <AlertCircle className="h-3.5 w-3.5 text-red-500" />
        </div>
        <div className="max-w-[80%] rounded-2xl rounded-tl-sm border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-300">
          {message.content}
        </div>
      </div>
    );
  }

  // System
  if (message.type === 'System') {
    return (
      <div className="my-2 flex justify-center px-4">
        <span className="rounded-full bg-blue-50 px-3 py-1 text-xs text-blue-600 dark:bg-blue-950 dark:text-blue-300">
          {message.content}
        </span>
      </div>
    );
  }

  // User
  if (!message.isFromBot) {
    return (
      <div className="flex justify-end px-4 py-1.5">
        <div className="max-w-[80%] rounded-2xl rounded-tr-sm bg-[#00A870] px-4 py-2.5 text-sm text-white">
          {message.content}
        </div>
      </div>
    );
  }

  // Bot
  return (
    <div className="flex gap-2.5 px-4 py-1.5">
      <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[#00A870]/10">
        <Headphones className="h-3.5 w-3.5 text-[#00A870]" />
      </div>
      <div className="max-w-[80%] space-y-2">
        <div className="rounded-2xl rounded-tl-sm bg-gray-100 px-4 py-3 text-sm dark:bg-gray-800">
          <BotMessageContent content={message.content} />
        </div>

        {/* Suggested actions */}
        {message.suggestedActions && message.suggestedActions.length > 0 && (
          <div className="flex flex-wrap gap-1.5 pl-1">
            {message.suggestedActions.map((action, i) => (
              <button
                key={i}
                onClick={() => onSuggestion?.(action)}
                className="rounded-full border border-[#00A870]/30 bg-[#00A870]/5 px-3 py-1.5 text-xs font-medium text-[#00A870] transition-colors hover:bg-[#00A870]/15"
              >
                {action}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

// =============================================================================
// Main Widget
// =============================================================================

export function SupportAgentWidget() {
  const chat = useSupportAgent({ autoWelcome: true });
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const scrollRef = useRef<HTMLDivElement>(null);
  // Support bubble always on the right side across all views
  const bubblePosition = 'right-4 bottom-4';

  // Auto-scroll
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTo({
        top: scrollRef.current.scrollHeight,
        behavior: 'smooth',
      });
    }
  }, [chat.messages]);

  return (
    <>
      {/* Floating support bubble — distinct from vehicle chatbot */}
      <button
        onClick={chat.toggle}
        className={`fixed ${bubblePosition} z-[9997] flex items-center gap-2 rounded-full px-4 py-3 shadow-lg transition-all duration-300 hover:scale-105 focus:ring-2 focus:ring-[#00A870] focus:ring-offset-2 focus:outline-none ${
          chat.isOpen
            ? 'bg-gray-600 hover:bg-gray-700'
            : 'bg-gradient-to-br from-[#00A870] to-[#009663] hover:from-[#009663] hover:to-[#008555]'
        }`}
        aria-label={chat.isOpen ? 'Cerrar soporte' : 'Soporte OKLA'}
      >
        {chat.isOpen ? (
          <X className="h-5 w-5 text-white" />
        ) : (
          <>
            <Headphones className="h-5 w-5 text-white" />
            <span className="text-sm font-semibold text-white max-sm:hidden">Soporte</span>
            <span className="absolute inset-0 animate-ping rounded-full bg-[#00A870] opacity-20" />
          </>
        )}
      </button>

      {/* Panel */}
      {chat.isOpen && (
        <div
          className={`fixed right-4 bottom-20 z-[9999] flex w-[380px] flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl transition-all duration-300 max-sm:inset-0 max-sm:right-0 max-sm:bottom-0 max-sm:left-0 max-sm:w-full max-sm:rounded-none dark:border-gray-700 dark:bg-gray-950`}
          style={{ height: 'min(600px, calc(100vh - 120px))' }}
          role="dialog"
          aria-label="Soporte OKLA"
        >
          {/* Header */}
          <div className="flex items-center justify-between rounded-t-2xl bg-gradient-to-r from-[#00A870] to-[#009663] px-4 py-3 text-white">
            <div className="flex items-center gap-3">
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-white/20">
                <Headphones className="h-5 w-5" />
              </div>
              <div>
                <h3 className="text-sm leading-tight font-semibold">Soporte OKLA</h3>
                <div className="flex items-center gap-1.5">
                  <span className="inline-block h-2 w-2 rounded-full bg-green-300" />
                  <span className="text-[11px] text-white/80">En línea</span>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-1">
              <button
                onClick={chat.resetChat}
                className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
                title="Nuevo chat"
                aria-label="Reiniciar chat"
              >
                <RotateCcw className="h-4 w-4" />
              </button>
              <button
                onClick={chat.close}
                className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
                title="Minimizar"
                aria-label="Minimizar chat"
              >
                <Minus className="h-4 w-4" />
              </button>
              <button
                onClick={chat.close}
                className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
                title="Cerrar chat"
                aria-label="Cerrar chat"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Error banner */}
          {chat.error && (
            <div className="flex items-center gap-2 bg-red-50 px-4 py-2 text-xs text-red-700 dark:bg-red-950 dark:text-red-300">
              <AlertCircle className="h-3.5 w-3.5 shrink-0" />
              <span className="flex-1">{chat.error}</span>
              <button
                onClick={chat.clearError}
                className="text-red-500 underline hover:no-underline"
              >
                Cerrar
              </button>
            </div>
          )}

          {/* Messages */}
          <div
            ref={scrollRef}
            className="flex-1 overflow-y-auto py-3"
            role="log"
            aria-live="polite"
          >
            {!authLoading && !isAuthenticated ? (
              <div className="flex h-full flex-col items-center justify-center gap-5 px-8 text-center">
                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-gray-100 dark:bg-gray-800">
                  <Lock className="h-8 w-8 text-gray-400 dark:text-gray-500" />
                </div>
                <div>
                  <h3 className="font-semibold text-gray-900 dark:text-gray-100">
                    Soporte para usuarios registrados
                  </h3>
                  <p className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
                    Inicia sesión o regístrate gratis para acceder al soporte IA 24/7.
                  </p>
                </div>
                <div className="flex w-full flex-col gap-2">
                  <Link
                    href="/login"
                    className="block w-full rounded-xl bg-[#00A870] px-4 py-2.5 text-center text-sm font-semibold text-white transition-colors hover:bg-[#009663]"
                    onClick={chat.close}
                  >
                    Iniciar sesión
                  </Link>
                  <Link
                    href="/registro"
                    className="block w-full rounded-xl border border-[#00A870] px-4 py-2.5 text-center text-sm font-semibold text-[#00A870] transition-colors hover:bg-[#00A870]/5"
                    onClick={chat.close}
                  >
                    Crear cuenta gratis
                  </Link>
                </div>
              </div>
            ) : (
              <>
                {chat.messages.length === 0 && (
                  <div className="flex h-full flex-col items-center justify-center gap-3 px-6 text-center">
                    <WifiOff className="h-10 w-10 text-gray-300 dark:text-gray-600" />
                    <p className="text-sm text-gray-500">Conectando...</p>
                  </div>
                )}

                {chat.messages.map(msg => (
                  <SupportMessageBubble
                    key={msg.id}
                    message={msg}
                    onSuggestion={chat.selectSuggestion}
                  />
                ))}
              </>
            )}
          </div>

          {/* Input */}
          <ChatInput
            onSend={chat.sendMessage}
            disabled={chat.isLoading || (!authLoading && !isAuthenticated)}
            isLoading={chat.isLoading}
            placeholder={
              !authLoading && !isAuthenticated
                ? 'Inicia sesión para chatear'
                : 'Escribe tu pregunta...'
            }
          />
        </div>
      )}
    </>
  );
}
