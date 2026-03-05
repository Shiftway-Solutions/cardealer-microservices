'use client';

/**
 * SearchAgentWidget — AI search assistant for the vehicle marketplace.
 *
 * Floating chat bubble on /vehiculos that helps users find vehicles
 * using natural language via SearchAgent (Claude Haiku 4.5).
 *
 * Converts AI queries into structured filters and applies them to the search.
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import { Sparkles, X, RotateCcw, Minus, AlertCircle, Loader2 } from 'lucide-react';
import { ChatInput } from '@/components/chat/ChatInput';
import { BotMessageContent } from '@/components/chat/BotMessageContent';
import { aiSearch, aiFiltersToSearchParams, type SearchAgentResult } from '@/services/search-agent';

// =============================================================================
// Types
// =============================================================================

interface SearchChatMessage {
  id: string;
  content: string;
  isFromBot: boolean;
  timestamp: Date;
  isLoading?: boolean;
  isError?: boolean;
  filters?: Record<string, string | number | undefined>;
  aiMeta?: {
    confidence: number;
    wasCached: boolean;
    latencyMs: number;
    relaxationLevel: number;
    queryReformulated?: string;
  };
  suggestions?: string[];
}

interface SearchAgentWidgetProps {
  /** Callback to apply AI-generated filters to the vehicle search */
  onFiltersApplied: (filters: Record<string, string | number | undefined>) => void;
}

// =============================================================================
// Constants
// =============================================================================

const WELCOME_MESSAGE: SearchChatMessage = {
  id: 'search-welcome',
  content:
    '¡Hola! 🔍 Soy tu **asistente de búsqueda** con IA. Descríbeme el vehículo que buscas en lenguaje natural y yo lo encuentro.\n\n' +
    'Por ejemplo: *"Toyota Corolla 2020 automático"* o *"SUV familiar bajo RD$1 millón"*',
  isFromBot: true,
  timestamp: new Date(),
  suggestions: [
    'Toyota Corolla 2020 automática',
    'SUV menos de un millón',
    'Pickup diesel 4x4',
    'Carro económico para trabajo',
    'Honda Civic 2022',
  ],
};

// =============================================================================
// Component
// =============================================================================

export function SearchAgentWidget({ onFiltersApplied }: SearchAgentWidgetProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<SearchChatMessage[]>([WELCOME_MESSAGE]);
  const [isLoading, setIsLoading] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  // Auto-scroll
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
        setIsOpen(false);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen]);

  const handleSearch = useCallback(
    async (query: string) => {
      if (!query.trim() || isLoading) return;

      // Add user message
      const userMsg: SearchChatMessage = {
        id: `search-user-${Date.now()}`,
        content: query.trim(),
        isFromBot: false,
        timestamp: new Date(),
      };

      const loadingMsg: SearchChatMessage = {
        id: `search-loading-${Date.now()}`,
        content: '',
        isFromBot: true,
        timestamp: new Date(),
        isLoading: true,
      };

      setMessages(prev => [...prev, userMsg, loadingMsg]);
      setIsLoading(true);

      try {
        const result: SearchAgentResult = await aiSearch({ query: query.trim() });
        const ai = result.aiFilters;

        if (!result.isAiSearchEnabled) {
          setMessages(prev => [
            ...prev.filter(m => !m.isLoading),
            {
              id: `search-bot-${Date.now()}`,
              content:
                '⚠️ La búsqueda con IA no está disponible en este momento. Usa los filtros manuales.',
              isFromBot: true,
              timestamp: new Date(),
              isError: true,
            },
          ]);
          return;
        }

        // Out-of-context query
        if (ai.confianza === 0 && ai.mensaje_usuario) {
          setMessages(prev => [
            ...prev.filter(m => !m.isLoading),
            {
              id: `search-bot-${Date.now()}`,
              content: ai.mensaje_usuario!,
              isFromBot: true,
              timestamp: new Date(),
              suggestions: ['Toyota Corolla 2020', 'SUV familiar', 'Carro económico'],
            },
          ]);
          return;
        }

        // Build filters and apply
        let appliedFilters: Record<string, string | number | undefined> = {};
        if (ai.filtros_exactos) {
          appliedFilters = aiFiltersToSearchParams(ai.filtros_exactos);
          onFiltersApplied(appliedFilters);
        }

        // Build response message
        let responseText = '';

        if (ai.query_reformulada) {
          responseText += `🔎 Busqué: **${ai.query_reformulada}**\n\n`;
        }

        responseText += '✅ ¡Filtros aplicados! He encontrado vehículos que coinciden.\n\n';

        // Show applied filters summary
        const filterParts: string[] = [];
        const f = ai.filtros_exactos;
        if (f) {
          if (f.marca) filterParts.push(`Marca: **${f.marca}**`);
          if (f.modelo) filterParts.push(`Modelo: **${f.modelo}**`);
          if (f.anio_desde || f.anio_hasta) {
            filterParts.push(`Año: **${f.anio_desde || '?'}–${f.anio_hasta || '?'}**`);
          }
          if (f.precio_min || f.precio_max) {
            const fmtPrice = (v: number) =>
              v >= 1_000_000
                ? `RD$${(v / 1_000_000).toFixed(v % 1_000_000 === 0 ? 0 : 1)}M`
                : `RD$${(v / 1_000).toFixed(0)}K`;
            const min = f.precio_min ? fmtPrice(f.precio_min) : null;
            const max = f.precio_max ? fmtPrice(f.precio_max) : null;
            if (min && max) filterParts.push(`Precio: **${min} – ${max}**`);
            else if (max) filterParts.push(`Precio: **hasta ${max}**`);
            else if (min) filterParts.push(`Precio: **desde ${min}**`);
          }
          if (f.transmision) filterParts.push(`Transmisión: **${f.transmision}**`);
          if (f.combustible) filterParts.push(`Combustible: **${f.combustible}**`);
          if (f.tipo_vehiculo) filterParts.push(`Tipo: **${f.tipo_vehiculo}**`);
          if (f.condicion) filterParts.push(`Condición: **${f.condicion}**`);
        }

        if (filterParts.length > 0) {
          responseText += filterParts.map(p => `• ${p}`).join('\n');
        }

        if (ai.mensaje_relajamiento) {
          responseText += `\n\n⚠️ ${ai.mensaje_relajamiento}`;
        }

        if (ai.advertencias && ai.advertencias.length > 0) {
          responseText += '\n\n' + ai.advertencias.map(w => `⚠ ${w}`).join('\n');
        }

        const botMsg: SearchChatMessage = {
          id: `search-bot-${Date.now()}`,
          content: responseText,
          isFromBot: true,
          timestamp: new Date(),
          filters: appliedFilters,
          aiMeta: {
            confidence: ai.confianza,
            wasCached: result.wasCached,
            latencyMs: result.latencyMs,
            relaxationLevel: ai.nivel_filtros_activo,
            queryReformulated: ai.query_reformulada ?? undefined,
          },
          suggestions: ['Mostrar más baratos', 'Solo automáticos', 'Con menos kilometraje'],
        };

        setMessages(prev => [...prev.filter(m => !m.isLoading), botMsg]);
      } catch {
        setMessages(prev => [
          ...prev.filter(m => !m.isLoading),
          {
            id: `search-error-${Date.now()}`,
            content: 'Error al procesar la búsqueda. Intenta de nuevo.',
            isFromBot: true,
            timestamp: new Date(),
            isError: true,
          },
        ]);
      } finally {
        setIsLoading(false);
      }
    },
    [isLoading, onFiltersApplied]
  );

  const resetChat = () => {
    setMessages([WELCOME_MESSAGE]);
  };

  // ── Render ─────────────────────────────────────────────────────────────

  return (
    <>
      {/* Floating bubble — AI search branded (OKLA green/sparkles) */}
      <button
        onClick={() => setIsOpen(o => !o)}
        className={`fixed right-20 bottom-4 z-[9995] flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-all duration-300 hover:scale-110 focus:ring-2 focus:ring-[#00A870] focus:ring-offset-2 focus:outline-none ${
          isOpen
            ? 'bg-gray-600 hover:bg-gray-700'
            : 'bg-gradient-to-br from-[#00A870] to-[#009663] hover:from-[#009663] hover:to-[#008555]'
        }`}
        aria-label={isOpen ? 'Cerrar búsqueda IA' : 'Buscar con IA'}
      >
        {isOpen ? (
          <X className="h-6 w-6 text-white" />
        ) : (
          <>
            <Sparkles className="h-6 w-6 text-white" />
            <span className="absolute inset-0 animate-ping rounded-full bg-[#00A870] opacity-20 [animation-iteration-count:3]" />
          </>
        )}
      </button>

      {/* Panel */}
      {isOpen && (
        <div
          className="fixed right-4 bottom-20 z-[9999] flex w-[380px] flex-col overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-2xl transition-all duration-300 max-sm:inset-0 max-sm:right-0 max-sm:bottom-0 max-sm:w-full max-sm:rounded-none dark:border-gray-700 dark:bg-gray-950"
          style={{ height: 'min(550px, calc(100vh - 120px))' }}
          role="dialog"
          aria-label="Búsqueda con IA"
        >
          {/* Header */}
          <div className="flex items-center justify-between rounded-t-2xl bg-gradient-to-r from-[#00A870] to-[#009663] px-4 py-3 text-white">
            <div className="flex items-center gap-3">
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-white/20">
                <Sparkles className="h-5 w-5" />
              </div>
              <div>
                <h3 className="text-sm leading-tight font-semibold">Búsqueda IA</h3>
                <span className="text-[11px] text-white/80">
                  Encuentra vehículos con lenguaje natural
                </span>
              </div>
            </div>
            <div className="flex items-center gap-1">
              <button
                onClick={resetChat}
                className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
                title="Nueva búsqueda"
                aria-label="Nueva búsqueda"
              >
                <RotateCcw className="h-4 w-4" />
              </button>
              <button
                onClick={() => setIsOpen(false)}
                className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
                title="Minimizar"
                aria-label="Minimizar búsqueda"
              >
                <Minus className="h-4 w-4" />
              </button>
              <button
                onClick={() => setIsOpen(false)}
                className="rounded-lg p-1.5 transition-colors hover:bg-white/20"
                title="Cerrar"
                aria-label="Cerrar búsqueda"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Messages */}
          <div
            ref={scrollRef}
            className="flex-1 overflow-y-auto py-3"
            role="log"
            aria-live="polite"
          >
            {messages.map(msg => (
              <div key={msg.id}>
                {/* Loading */}
                {msg.isLoading && (
                  <div className="flex gap-2.5 px-4 py-1.5">
                    <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[#00A870]/10 dark:bg-[#00A870]/20">
                      <Loader2 className="h-3.5 w-3.5 animate-spin text-[#00A870]" />
                    </div>
                    <div className="max-w-[80%] rounded-2xl rounded-tl-sm bg-gray-100 px-4 py-3 dark:bg-gray-800">
                      <span className="text-sm text-gray-500">Analizando tu búsqueda...</span>
                    </div>
                  </div>
                )}

                {/* Error */}
                {!msg.isLoading && msg.isError && (
                  <div className="flex gap-2.5 px-4 py-1.5">
                    <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-red-100">
                      <AlertCircle className="h-3.5 w-3.5 text-red-500" />
                    </div>
                    <div className="max-w-[80%] rounded-2xl rounded-tl-sm border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                      {msg.content}
                    </div>
                  </div>
                )}

                {/* User */}
                {!msg.isLoading && !msg.isError && !msg.isFromBot && (
                  <div className="flex justify-end px-4 py-1.5">
                    <div className="max-w-[80%] rounded-2xl rounded-tr-sm bg-[#00A870] px-4 py-2.5 text-sm text-white">
                      {msg.content}
                    </div>
                  </div>
                )}

                {/* Bot */}
                {!msg.isLoading && !msg.isError && msg.isFromBot && (
                  <div className="flex gap-2.5 px-4 py-1.5">
                    <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[#00A870]/10 dark:bg-[#00A870]/20">
                      <Sparkles className="h-3.5 w-3.5 text-[#00A870]" />
                    </div>
                    <div className="max-w-[80%] space-y-2">
                      <div className="rounded-2xl rounded-tl-sm bg-gray-100 px-4 py-3 text-sm dark:bg-gray-800">
                        <BotMessageContent content={msg.content} />
                      </div>

                      {/* AI meta badges */}
                      {msg.aiMeta && (
                        <div className="flex flex-wrap gap-1 pl-1">
                          <span className="rounded-full bg-[#00A870]/10 px-2 py-0.5 text-[10px] font-medium text-[#00A870]">
                            {Math.round(msg.aiMeta.confidence * 100)}% confianza
                          </span>
                          <span className="rounded-full bg-gray-100 px-2 py-0.5 text-[10px] text-gray-500">
                            {msg.aiMeta.latencyMs}ms
                          </span>
                          {msg.aiMeta.wasCached && (
                            <span className="rounded-full bg-green-100 px-2 py-0.5 text-[10px] text-green-600">
                              Caché
                            </span>
                          )}
                        </div>
                      )}

                      {/* Suggestions */}
                      {msg.suggestions && msg.suggestions.length > 0 && (
                        <div className="flex flex-wrap gap-1.5 pl-1">
                          {msg.suggestions.map((s, i) => (
                            <button
                              key={i}
                              onClick={() => handleSearch(s)}
                              className="rounded-full border border-[#00A870]/30 bg-[#00A870]/5 px-3 py-1.5 text-xs font-medium text-[#00A870] transition-colors hover:bg-[#00A870]/15"
                            >
                              {s}
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Input */}
          <ChatInput
            onSend={handleSearch}
            disabled={isLoading}
            isLoading={isLoading}
            placeholder='Ej: "SUV familiar menos de RD$1M"'
          />
        </div>
      )}
    </>
  );
}
