'use client';

/**
 * VehicleChatWidget — OKLA Support AI assistant for vehicle detail pages.
 *
 * Uses ChatbotService (Claude Sonnet 4.5) in global OKLA support mode (no dealer scoping).
 * Opens when user clicks the OKLA support bubble.
 * Provides platform-level support: questions about the vehicle, help buying, contact info.
 */

import { useRef, useEffect } from 'react';
import { useChatbot } from '@/hooks/useChatbot';
import { ChatPanel } from '@/components/chat/ChatPanel';
import { X, Bot } from 'lucide-react';
import type { Vehicle } from '@/types';

interface VehicleChatWidgetProps {
  vehicle: Vehicle;
  /** Whether the chat panel should be open initially */
  isOpenInitial?: boolean;
  /** Callback when state changes */
  onOpenChange?: (isOpen: boolean) => void;
}

export function VehicleChatWidget({
  vehicle,
  isOpenInitial = false,
  onOpenChange,
}: VehicleChatWidgetProps) {
  // No dealerId — global OKLA support mode (not dealer-specific)
  const chat = useChatbot({
    maxRetries: 2,
    onLeadGenerated: leadId => {
      console.log('[OKLA Support] Lead generated:', leadId);
    },
    onTransfer: agentName => {
      console.log('[OKLA Support] Transferred to:', agentName);
    },
    onLimitReached: () => {
      console.log('[OKLA Support] Limit reached');
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

  return (
    <>
      {/* OKLA Support floating bubble — green branded */}
      <button
        onClick={chat.toggle}
        className={`fixed right-4 bottom-4 z-[9998] flex items-center gap-2 rounded-full px-5 py-3 shadow-lg transition-all duration-300 hover:scale-105 focus:ring-2 focus:ring-[#00A870] focus:ring-offset-2 focus:outline-none ${
          chat.isOpen
            ? 'bg-gray-600 hover:bg-gray-700'
            : 'bg-gradient-to-r from-[#00A870] to-emerald-600 hover:from-[#009060] hover:to-emerald-700'
        }`}
        aria-label={chat.isOpen ? 'Cerrar soporte OKLA' : 'Abrir asistente de soporte OKLA'}
      >
        {chat.isOpen ? (
          <X className="h-5 w-5 text-white" />
        ) : (
          <>
            <Bot className="h-5 w-5 text-white" />
            <span className="text-sm font-semibold text-white max-sm:hidden">Soporte OKLA</span>
            <span className="absolute inset-0 animate-ping rounded-full bg-[#00A870] opacity-20" />
          </>
        )}
      </button>

      {/* Chat panel — reuses the existing ChatPanel */}
      <ChatPanel
        messages={chat.messages}
        isOpen={chat.isOpen}
        isLoading={chat.isLoading}
        isConnected={chat.isConnected}
        isLimitReached={chat.isLimitReached}
        botName="Soporte OKLA"
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
