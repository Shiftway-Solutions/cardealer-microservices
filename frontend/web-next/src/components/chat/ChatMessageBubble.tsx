'use client';

import {
  type ChatMessage,
  formatVehiclePrice,
  type QuickReply,
  type VehicleCard,
} from '@/services/chatbot';
import { Bot, ExternalLink, AlertCircle } from 'lucide-react';
import Image from 'next/image';
import { BotMessageContent } from './BotMessageContent';

interface ChatMessageBubbleProps {
  message: ChatMessage;
  botName?: string;
  botAvatarUrl?: string | null;
  onQuickReply?: (reply: QuickReply) => void;
}

export function ChatMessageBubble({
  message,
  botName = 'OKLA Bot',
  botAvatarUrl,
  onQuickReply,
}: ChatMessageBubbleProps) {
  const {
    isFromBot,
    content,
    type,
    isLoading,
    isError,
    vehicleCard,
    vehicleCards,
    quickReplies,
    timestamp,
  } = message;

  // Loading indicator
  if (isLoading) {
    return (
      <div className="flex items-start gap-2.5 px-4 py-1.5">
        <BotAvatar name={botName} avatarUrl={botAvatarUrl} />
        <div className="max-w-[75%] rounded-2xl rounded-tl-md bg-gray-100 px-4 py-3 dark:bg-gray-800">
          <TypingDots />
        </div>
      </div>
    );
  }

  // Error message
  if (isError) {
    return (
      <div className="flex items-start gap-2.5 px-4 py-1.5">
        <BotAvatar name={botName} avatarUrl={botAvatarUrl} />
        <div className="max-w-[75%] rounded-2xl rounded-tl-md border border-red-200 bg-red-50 px-4 py-3 dark:border-red-800 dark:bg-red-950">
          <div className="flex items-center gap-1.5 text-sm text-red-700 dark:text-red-300">
            <AlertCircle className="h-3.5 w-3.5 shrink-0" />
            <span>{content}</span>
          </div>
        </div>
      </div>
    );
  }

  // System message (transfer, etc.)
  if (type === 'System') {
    return (
      <div className="px-4 py-2">
        <div className="rounded-xl bg-blue-50 px-4 py-2.5 text-center text-xs text-blue-700 dark:bg-blue-950 dark:text-blue-300">
          {content}
        </div>
      </div>
    );
  }

  // User message
  if (!isFromBot) {
    return (
      <div className="flex items-start justify-end gap-2.5 px-4 py-1.5">
        <div className="bg-primary max-w-[75%] rounded-2xl rounded-tr-md px-4 py-2.5 text-sm text-white">
          <p className="break-words whitespace-pre-wrap">{content}</p>
          <span className="mt-1 block text-right text-[10px] text-white/60">
            {formatTime(timestamp)}
          </span>
        </div>
      </div>
    );
  }

  // Bot message
  return (
    <div className="flex items-start gap-2.5 px-4 py-1.5">
      <BotAvatar name={botName} avatarUrl={botAvatarUrl} />
      <div className="max-w-[80%] space-y-2">
        {/* Text content */}
        <div className="rounded-2xl rounded-tl-md bg-gray-100 px-4 py-2.5 text-gray-900 dark:bg-gray-800 dark:text-gray-100">
          <BotMessageContent content={content} />
          <span className="mt-1 block text-right text-[10px] text-gray-500 dark:text-gray-400">
            {formatTime(timestamp)}
          </span>
        </div>

        {/* Vehicle card */}
        {vehicleCard && <VehicleCardInline card={vehicleCard} />}

        {/* Multiple vehicle cards */}
        {vehicleCards && vehicleCards.length > 0 && (
          <div className="space-y-2">
            {vehicleCards.map(card => (
              <VehicleCardInline key={card.vehicleId} card={card} />
            ))}
          </div>
        )}

        {/* Quick replies */}
        {quickReplies && quickReplies.length > 0 && onQuickReply && (
          <div className="flex flex-wrap gap-1.5">
            {quickReplies.map((reply, idx) => (
              <button
                key={`${reply.text}-${idx}`}
                onClick={() => onQuickReply(reply)}
                className="border-primary text-primary hover:bg-primary dark:hover:bg-primary rounded-full border bg-white px-3 py-1.5 text-xs font-medium transition-colors hover:text-white dark:bg-gray-900"
              >
                {reply.text}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

// =============================================================================
// Sub-components
// =============================================================================

function BotAvatar({ name, avatarUrl }: { name: string; avatarUrl?: string | null }) {
  if (avatarUrl) {
    return (
      <div className="flex h-8 w-8 shrink-0 items-center justify-center overflow-hidden rounded-full">
        <Image
          src={avatarUrl}
          alt={name}
          width={32}
          height={32}
          sizes="32px"
          className="h-full w-full object-cover"
        />
      </div>
    );
  }

  return (
    <div className="from-primary to-primary/90 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br">
      <Bot className="h-4 w-4 text-white" />
    </div>
  );
}

function TypingDots() {
  return (
    <div className="flex items-center gap-1" aria-label="Escribiendo...">
      <span className="inline-block h-2 w-2 animate-bounce rounded-full bg-gray-400 [animation-delay:0ms]" />
      <span className="inline-block h-2 w-2 animate-bounce rounded-full bg-gray-400 [animation-delay:150ms]" />
      <span className="inline-block h-2 w-2 animate-bounce rounded-full bg-gray-400 [animation-delay:300ms]" />
    </div>
  );
}

function VehicleCardInline({ card }: { card: VehicleCard }) {
  return (
    <div className="overflow-hidden rounded-xl border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-900">
      {/* Image */}
      {card.imageUrl && (
        <div className="relative h-36 w-full bg-gray-100 dark:bg-gray-800">
          <Image src={card.imageUrl} alt={card.title} fill className="object-cover" sizes="300px" />
          {card.isOnSale && (
            <span className="absolute top-2 left-2 rounded-full bg-red-500 px-2 py-0.5 text-[10px] font-bold text-white">
              OFERTA
            </span>
          )}
        </div>
      )}

      {/* Content */}
      <div className="p-3">
        <h4 className="text-sm font-semibold text-gray-900 dark:text-white">{card.title}</h4>
        <p className="mt-0.5 text-xs text-gray-500 dark:text-gray-400">{card.subtitle}</p>

        <div className="mt-2 flex items-baseline gap-2">
          <span className="text-primary text-lg font-bold">{formatVehiclePrice(card.price)}</span>
          {card.originalPrice && card.originalPrice > card.price && (
            <span className="text-xs text-gray-500 line-through">
              {formatVehiclePrice(card.originalPrice)}
            </span>
          )}
        </div>

        {/* Highlights */}
        {card.highlights && card.highlights.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-1">
            {card.highlights.slice(0, 4).map((h, i) => (
              <span
                key={i}
                className="rounded-full bg-gray-100 px-2 py-0.5 text-[10px] text-gray-600 dark:bg-gray-800 dark:text-gray-300"
              >
                {h}
              </span>
            ))}
          </div>
        )}

        {/* CTA */}
        {card.detailsUrl && (
          <a
            href={card.detailsUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="bg-primary hover:bg-primary/90 mt-2.5 flex items-center justify-center gap-1.5 rounded-lg px-3 py-2 text-xs font-medium text-white transition-colors"
          >
            Ver detalles
            <ExternalLink className="h-3 w-3" />
          </a>
        )}
      </div>
    </div>
  );
}

// =============================================================================
// Helpers
// =============================================================================

function formatTime(date: Date): string {
  return date.toLocaleTimeString('es-DO', {
    hour: '2-digit',
    minute: '2-digit',
  });
}
