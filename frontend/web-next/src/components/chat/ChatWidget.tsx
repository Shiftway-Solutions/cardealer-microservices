'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Lock, X } from 'lucide-react';
import { useChatbot } from '@/hooks/useChatbot';
import { useAuth } from '@/hooks/use-auth';
import { ChatBubble } from './ChatBubble';
import { ChatPanel } from './ChatPanel';

interface ChatWidgetProps {
  /** Optional dealer ID to scope the chat */
  dealerId?: string;
}

/**
 * ChatWidget — Floating AI chatbot widget for the OKLA platform.
 *
 * Renders a floating bubble button and expandable chat panel.
 * Uses Llama 3 8B fine-tuned model for vehicle sales assistance.
 *
 * Usage: Add <ChatWidget /> to the main layout.
 */
export function ChatWidget({ dealerId }: ChatWidgetProps) {
  const chat = useChatbot({
    dealerId,
    maxRetries: 2,
    onLeadGenerated: _leadId => {
      // TODO: track with analytics service
    },
    onTransfer: _agentName => {
      // TODO: track with analytics service
    },
    onLimitReached: () => {
      // TODO: track with analytics service
    },
  });

  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const pathname = usePathname();
  const redirectParam = `?redirect=${encodeURIComponent(pathname)}`;

  return (
    <>
      {/* Floating bubble button */}
      <ChatBubble isOpen={chat.isOpen} onClick={chat.toggle} />

      {/* Auth gate — shown when panel open but user not authenticated */}
      {chat.isOpen && !authLoading && !isAuthenticated && (
        <div
          className="fixed right-4 bottom-20 z-[9999] flex w-[380px] flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl max-sm:inset-0 max-sm:right-0 max-sm:bottom-0 max-sm:w-full max-sm:rounded-none dark:border-gray-700 dark:bg-gray-950"
          style={{ height: 'min(360px, calc(100vh - 120px))' }}
          role="dialog"
          aria-label="Iniciar sesión para chatear"
        >
          {/* Header */}
          <div className="flex items-center justify-between rounded-t-2xl bg-gradient-to-r from-primary to-primary/90 px-4 py-3 text-white">
            <span className="text-sm font-semibold">Asistente OKLA</span>
            <button
              onClick={chat.close}
              className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
              aria-label="Cerrar"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
          {/* Body */}
          <div className="flex flex-1 flex-col items-center justify-center gap-5 px-8 text-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-gray-100 dark:bg-gray-800">
              <Lock className="h-8 w-8 text-gray-400 dark:text-gray-500" />
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">
                Inicia sesión para guardar esta conversación
              </h3>
              <p className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
                Regístrate gratis para que tu historial de chat quede guardado.
              </p>
            </div>
            <div className="flex w-full flex-col gap-2">
              <Link
                href={`/login${redirectParam}`}
                className="block w-full rounded-xl bg-primary px-4 py-2.5 text-center text-sm font-semibold text-white transition-colors hover:bg-primary/90"
                onClick={chat.close}
              >
                Iniciar sesión
              </Link>
              <Link
                href={`/registro${redirectParam}`}
                className="block w-full rounded-xl border border-primary px-4 py-2.5 text-center text-sm font-semibold text-primary transition-colors hover:bg-primary/5"
                onClick={chat.close}
              >
                Crear cuenta gratis
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Chat panel — only shown when authenticated */}
      <ChatPanel
        messages={chat.messages}
        isOpen={chat.isOpen && (isAuthenticated || authLoading)}
        isLoading={chat.isLoading}
        isConnected={chat.isConnected}
        isLimitReached={chat.isLimitReached}
        botName={chat.botName}
        botAvatarUrl={chat.botAvatarUrl}
        remainingInteractions={chat.remainingInteractions}
        error={chat.error}
        onSend={chat.sendMessage}
        onQuickReply={chat.selectQuickReply}
        onClose={chat.close}
        onMinimize={chat.close}
        onReset={chat.resetChat}
        onTransfer={() => chat.requestTransfer()}
        onClearError={chat.clearError}
      />
    </>
  );
}
