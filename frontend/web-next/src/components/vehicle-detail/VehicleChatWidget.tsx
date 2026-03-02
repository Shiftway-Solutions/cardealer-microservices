'use client';

/**
 * VehicleChatWidget — ChatbotService AI assistant for vehicle detail pages.
 *
 * Uses ChatbotService (Claude Sonnet 4.5) in SingleVehicle mode.
 * Opens when user clicks "Chat en vivo" or the vehicle chat bubble.
 * Provides vehicle-specific Q&A, negotiation assistance, and appointment scheduling.
 */

import { useRef, useEffect } from 'react';
import { useChatbot } from '@/hooks/useChatbot';
import { ChatPanel } from '@/components/chat/ChatPanel';
import {
  MessageSquare,
  X,
  Bot,
} from 'lucide-react';
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
  const chat = useChatbot({
    dealerId: vehicle.sellerType === 'dealer' ? vehicle.sellerId : undefined,
    maxRetries: 2,
    onLeadGenerated: leadId => {
      console.log('[OKLA VehicleChat] Lead generated:', leadId);
    },
    onTransfer: agentName => {
      console.log('[OKLA VehicleChat] Transferred to:', agentName);
    },
    onLimitReached: () => {
      console.log('[OKLA VehicleChat] Limit reached');
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

  // Send vehicle context as first message when session starts
  const sentContextRef = useRef(false);
  useEffect(() => {
    if (chat.isConnected && !sentContextRef.current && chat.messages.length <= 1) {
      sentContextRef.current = true;
      const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;
      const price = vehicle.currency === 'USD'
        ? `US$${vehicle.price.toLocaleString()}`
        : `RD$${vehicle.price.toLocaleString()}`;
      // Auto-send vehicle context
      chat.sendMessage(
        `Estoy viendo el ${vehicleTitle} (${price}). Quiero más información sobre este vehículo.`
      );
    }
  }, [chat.isConnected, chat.messages.length, vehicle, chat]);

  return (
    <>
      {/* Floating vehicle chat bubble — blue branded */}
      <button
        onClick={chat.toggle}
        className={`fixed right-4 bottom-4 z-[9998] flex items-center gap-2 rounded-full px-5 py-3 shadow-lg transition-all duration-300 hover:scale-105 focus:ring-2 focus:ring-blue-400 focus:ring-offset-2 focus:outline-none ${
          chat.isOpen
            ? 'bg-gray-600 hover:bg-gray-700'
            : 'bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700'
        }`}
        aria-label={chat.isOpen ? 'Cerrar chat del vehículo' : 'Chatear sobre este vehículo'}
      >
        {chat.isOpen ? (
          <X className="h-5 w-5 text-white" />
        ) : (
          <>
            <Bot className="h-5 w-5 text-white" />
            <span className="text-sm font-semibold text-white max-sm:hidden">
              Chat IA
            </span>
            <span className="absolute inset-0 animate-ping rounded-full bg-blue-500 opacity-20" />
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
        botName={`Asesor IA · ${vehicle.make} ${vehicle.model}`}
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
