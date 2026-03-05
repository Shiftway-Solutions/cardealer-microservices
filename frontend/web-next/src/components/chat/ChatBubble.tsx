'use client';

import { MessageCircle, X } from 'lucide-react';
import { useState, useEffect } from 'react';

interface ChatBubbleProps {
  isOpen: boolean;
  onClick: () => void;
  unreadCount?: number;
  /** Position class override for avoiding overlap with other chatbots */
  positionClass?: string;
}

export function ChatBubble({ isOpen, onClick, unreadCount = 0, positionClass }: ChatBubbleProps) {
  const [isAnimated, setIsAnimated] = useState(false);

  // Entrance animation
  useEffect(() => {
    const timer = setTimeout(() => setIsAnimated(true), 500);
    return () => clearTimeout(timer);
  }, []);

  return (
    <button
      onClick={onClick}
      className={`fixed z-[9996] flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-all duration-300 hover:scale-110 focus:ring-2 focus:ring-[#00A870] focus:ring-offset-2 focus:outline-none ${
        positionClass || 'right-4 bottom-20'
      } ${isAnimated ? 'translate-y-0 opacity-100' : 'translate-y-4 opacity-0'} ${
        isOpen
          ? 'bg-gray-600 hover:bg-gray-700'
          : 'bg-gradient-to-br from-[#00A870] to-[#009663] hover:from-[#009663] hover:to-[#008555]'
      }`}
      aria-label={isOpen ? 'Cerrar chat' : 'Abrir chat con OKLA Bot'}
    >
      {isOpen ? (
        <X className="h-6 w-6 text-white" />
      ) : (
        <>
          <MessageCircle className="h-6 w-6 text-white" />
          {/* Unread badge */}
          {unreadCount > 0 && (
            <span className="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white">
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
          {/* Pulse animation when not open */}
          <span className="absolute inset-0 animate-ping rounded-full bg-[#00A870] opacity-20 [animation-iteration-count:3]" />
        </>
      )}
    </button>
  );
}
