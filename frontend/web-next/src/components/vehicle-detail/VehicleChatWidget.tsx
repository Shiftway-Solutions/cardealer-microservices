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
 */

import { useRef, useEffect } from 'react';
import { useChatbot } from '@/hooks/useChatbot';
import { useAuth } from '@/hooks/use-auth';
import { ChatPanel } from '@/components/chat/ChatPanel';
import { X, Bot, Info } from 'lucide-react';
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
}

export function VehicleChatWidget({
  vehicle,
  dealerId,
  dealerName,
  isOpenInitial = false,
  onOpenChange,
}: VehicleChatWidgetProps) {
  const { isAuthenticated } = useAuth();
  const chat = useChatbot({
    dealerId,
    dealerName,
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

  // Send vehicle context when session starts so OKLA support knows which vehicle the user is viewing
  const sentContextRef = useRef(false);
  useEffect(() => {
    if (chat.isConnected && !sentContextRef.current && chat.messages.length <= 1) {
      sentContextRef.current = true;
      const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;
      const price =
        vehicle.currency === 'USD'
          ? `US$${vehicle.price.toLocaleString()}`
          : `RD$${vehicle.price.toLocaleString()}`;
      chat.sendMessage(
        `Hola, estoy viendo el ${vehicleTitle} (${price}) en OKLA. ¿Me pueden ayudar con información sobre este vehículo?`
      );
    }
  }, [chat.isConnected, chat.messages.length, vehicle, chat]);

  const displayBotName =
    chat.botName || (dealerName ? `Asistente de ${dealerName}` : 'Soporte OKLA');
  const bubbleLabel = dealerName ? `Chat con ${dealerName}` : 'Soporte OKLA';

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
            <Bot className="h-5 w-5 text-white" />
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
