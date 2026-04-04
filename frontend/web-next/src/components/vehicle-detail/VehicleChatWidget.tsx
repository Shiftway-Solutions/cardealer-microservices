'use client';

/**
 * VehicleChatWidget — DealerChatAgent AI assistant for vehicle detail pages.
 *
 * Uses ChatbotService (Claude Sonnet 4.5) scoped to the specific dealer's ChatAgent.
 * Opens when buyer clicks the chat bubble on a vehicle detail page.
 * Connects buyer directly to the dealer's AI: inventory, pricing, test drives, etc.
 *
 * When dealerId is provided: dealer-scoped mode (DealerChatAgent).
 * When dealerId is absent: falls back to global OKLA support mode.
 *
 * Vehicle context: vehicleId is passed to the session so the backend uses
 * SingleVehicleStrategy and loads all vehicle data into the system prompt.
 *
 * Plan gate: if dealer is on LIBRE/VISIBLE plan (chatAgentWeb=0), isAiEnabled=false
 * and the widget shows a "direct message to dealer" form instead of AI chat.
 */

import { useRef, useEffect } from 'react';
import { useChatbot } from '@/hooks/useChatbot';
import { useAuth } from '@/hooks/use-auth';
import { ChatPanel } from '@/components/chat/ChatPanel';
import { X, Bot, Info, MessageCircle } from 'lucide-react';
import type { Vehicle } from '@/types';

interface VehicleChatWidgetProps {
  vehicle: Vehicle;
  /** Dealer ID to scope chat to the specific dealer's ChatAgent */
  dealerId?: string;
  /** Dealer display name shown in the chat header */
  dealerName?: string;
  /** Whether the chat panel should be open initially */
  isOpenInitial?: boolean;
  /** Callback when state changes */
  onOpenChange?: (isOpen: boolean) => void;
  /** Pre-probed AI status — avoids showing "Chat con..." for LIBRE plan dealers before session starts */
  initialAiEnabled?: boolean;
}

export function VehicleChatWidget({
  vehicle,
  dealerId,
  dealerName,
  isOpenInitial = false,
  onOpenChange,
  initialAiEnabled,
}: VehicleChatWidgetProps) {
  const { isAuthenticated } = useAuth();
  const chat = useChatbot({
    dealerId,
    dealerName,
    vehicleId: vehicle.id,
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

  // Open on initial if requested
  useEffect(() => {
    if (isOpenInitial && !chat.isOpen) {
      chat.open();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpenInitial]);

  // Notify parent of open state changes
  useEffect(() => {
    onOpenChange?.(chat.isOpen);
  }, [chat.isOpen, onOpenChange]);

  // NOTE: Vehicle context is now passed via vehicleId in startChatSession.
  // The backend uses SingleVehicleStrategy which fetches full vehicle data from DB
  // and builds a rich system prompt. The manual context message is no longer needed.
  const _sentContextRef = useRef(false);

  const displayBotName =
    chat.botName || (dealerName ? `Asistente de ${dealerName}` : 'Soporte OKLA');
  // Use initialAiEnabled as pre-session signal to avoid showing "Chat con..." for non-AI dealers
  const effectiveAiEnabled = chat.isConnected
    ? chat.isAiEnabled
    : (initialAiEnabled ?? chat.isAiEnabled);
  const bubbleLabel = dealerName
    ? effectiveAiEnabled
      ? `Chat con ${dealerName}`
      : `Mensaje a ${dealerName}`
    : 'Soporte OKLA';

  return (
    <>
      {/* Floating chat bubble — branded per dealer or global OKLA */}
      <button
        onClick={chat.toggle}
        className={`focus:ring-primary fixed right-4 bottom-4 z-[9998] flex items-center gap-2 rounded-full px-5 py-3 shadow-lg transition-all duration-300 hover:scale-105 focus:ring-2 focus:ring-offset-2 focus:outline-none ${
          chat.isOpen
            ? 'bg-gray-600 hover:bg-gray-700'
            : 'from-primary hover:from-primary/90 bg-gradient-to-r to-emerald-600 hover:to-emerald-700'
        }`}
        aria-label={chat.isOpen ? `Cerrar chat` : `Abrir chat con ${bubbleLabel}`}
      >
        {chat.isOpen ? (
          <X className="h-5 w-5 text-white" />
        ) : (
          <>
            {/* Use MessageCircle icon when AI is disabled (human-only messaging) */}
            {effectiveAiEnabled ? (
              <Bot className="h-5 w-5 text-white" />
            ) : (
              <MessageCircle className="h-5 w-5 text-white" />
            )}
            <span className="text-sm font-semibold text-white max-sm:hidden">{bubbleLabel}</span>
            <span className="bg-primary absolute inset-0 animate-ping rounded-full opacity-20" />
          </>
        )}
      </button>

      {/* Chat panel — reuses the existing ChatPanel */}
      {/* Auth warning banner for unauthenticated users (non-blocking — chat still works) */}
      {chat.isOpen && !isAuthenticated && (
        <div className="fixed right-4 bottom-[calc(100vh-120px)] z-[9999] flex w-[380px] items-center gap-2 rounded-t-xl border border-b-0 border-amber-200 bg-amber-50 px-4 py-2 text-xs text-amber-700 max-sm:right-0 max-sm:w-full dark:border-amber-800 dark:bg-amber-950 dark:text-amber-300">
          <Info className="h-4 w-4 shrink-0" />
          <span>Inicia sesión para guardar esta conversación</span>
        </div>
      )}
      {/* Non-AI mode banner: inform buyer that messages go to human dealer */}
      {chat.isOpen && !chat.isAiEnabled && (
        <div className="fixed right-4 bottom-[calc(100vh-160px)] z-[9999] flex w-[380px] items-center gap-2 rounded-t-xl border border-b-0 border-blue-200 bg-blue-50 px-4 py-2 text-xs text-blue-700 max-sm:right-0 max-sm:w-full dark:border-blue-800 dark:bg-blue-950 dark:text-blue-300">
          <MessageCircle className="h-4 w-4 shrink-0" />
          <span>El vendedor responde directamente — no hay asistente automático</span>
        </div>
      )}
      <ChatPanel
        messages={chat.messages}
        isOpen={chat.isOpen}
        isLoading={chat.isLoading}
        isConnected={chat.isConnected}
        isLimitReached={chat.isLimitReached}
        botName={displayBotName}
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

export default VehicleChatWidget;
