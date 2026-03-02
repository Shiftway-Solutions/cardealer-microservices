'use client';

import { useState, useCallback, useRef } from 'react';
import { Search, Sparkles, Loader2, AlertCircle } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { aiSearch, aiFiltersToSearchParams } from '@/services/search-agent';
import type { SearchAgentResult, SearchAgentAiResponse } from '@/services/search-agent';

interface AiSearchBarProps {
  onFiltersApplied: (filters: Record<string, string | number | undefined>) => void;
  onAiResponse?: (response: SearchAgentAiResponse) => void;
  className?: string;
}

/**
 * AI-powered search bar that accepts natural language queries.
 * Sends the query to SearchAgent (Claude Haiku 4.5) and converts
 * the AI response into vehicle search filters.
 */
export default function AiSearchBar({
  onFiltersApplied,
  onAiResponse,
  className = '',
}: AiSearchBarProps) {
  const [query, setQuery] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [lastResult, setLastResult] = useState<SearchAgentResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleSearch = useCallback(async () => {
    const trimmed = query.trim();
    if (!trimmed || trimmed.length < 2) return;

    setIsLoading(true);
    setError(null);

    try {
      const result = await aiSearch({ query: trimmed });
      setLastResult(result);

      if (!result.isAiSearchEnabled) {
        setError('La búsqueda inteligente no está disponible en este momento.');
        return;
      }

      const aiResponse = result.aiFilters;
      onAiResponse?.(aiResponse);

      // Handle out-of-context queries
      if (aiResponse.confianza === 0 && aiResponse.mensaje_usuario) {
        setError(aiResponse.mensaje_usuario);
        return;
      }

      // Convert AI filters to search params and apply
      if (aiResponse.filtros_exactos) {
        const searchParams = aiFiltersToSearchParams(aiResponse.filtros_exactos);
        onFiltersApplied(searchParams);
      }
    } catch (err) {
      console.error('AI search error:', err);
      setError('Error al procesar la búsqueda. Intenta de nuevo.');
    } finally {
      setIsLoading(false);
    }
  }, [query, onFiltersApplied, onAiResponse]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleSearch();
    }
  };

  const suggestions = [
    'Toyota Corolla 2020 automática',
    'SUV menos de un millón de pesos',
    'Pickup diesel 4x4',
    'Carro económico para el trabajo',
    'Honda Civic 2022 manual',
  ];

  return (
    <div className={`w-full space-y-3 ${className}`}>
      {/* Search Input */}
      <div className="relative flex items-center gap-2">
        <div className="relative flex-1">
          <Sparkles className="text-primary absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
          <Input
            ref={inputRef}
            type="text"
            placeholder='Busca con IA: "Toyota 2020 automática" o "SUV familiar bajo RD$1M"'
            value={query}
            onChange={e => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            className="h-12 pr-4 pl-10 text-base"
            aria-label="Búsqueda inteligente de vehículos"
            disabled={isLoading}
          />
        </div>
        <Button
          onClick={handleSearch}
          disabled={isLoading || query.trim().length < 2}
          className="h-12 px-6"
          aria-label="Buscar con inteligencia artificial"
        >
          {isLoading ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Search className="h-4 w-4" />
          )}
          <span className="ml-2 hidden sm:inline">Buscar</span>
        </Button>
      </div>

      {/* Quick Suggestions */}
      {!lastResult && !error && (
        <div className="flex flex-wrap gap-2">
          <span className="text-muted-foreground self-center text-xs">Prueba:</span>
          {suggestions.map(s => (
            <button
              key={s}
              onClick={() => {
                setQuery(s);
                inputRef.current?.focus();
              }}
              className="bg-muted hover:bg-muted/80 text-muted-foreground hover:text-foreground rounded-full px-2 py-1 text-xs transition-colors"
            >
              {s}
            </button>
          ))}
        </div>
      )}

      {/* AI Result Metadata */}
      {lastResult && lastResult.aiFilters && !error && (
        <div className="text-muted-foreground flex flex-wrap items-center gap-2 text-xs">
          {lastResult.aiFilters.query_reformulada && (
            <span>
              Interpretado como: <strong>{lastResult.aiFilters.query_reformulada}</strong>
            </span>
          )}
          <Badge variant="outline" className="text-xs">
            Confianza: {Math.round(lastResult.aiFilters.confianza * 100)}%
          </Badge>
          {lastResult.wasCached && (
            <Badge variant="secondary" className="text-xs">
              Caché
            </Badge>
          )}
          <Badge variant="outline" className="text-xs">
            {lastResult.latencyMs}ms
          </Badge>
          {lastResult.aiFilters.nivel_filtros_activo > 1 && (
            <Badge variant="destructive" className="text-xs">
              Filtros relajados (nivel {lastResult.aiFilters.nivel_filtros_activo})
            </Badge>
          )}
        </div>
      )}

      {/* Relaxation Message */}
      {lastResult?.aiFilters?.mensaje_relajamiento && (
        <p className="flex items-center gap-1 text-sm text-amber-600 dark:text-amber-400">
          <AlertCircle className="h-3.5 w-3.5" />
          {lastResult.aiFilters.mensaje_relajamiento}
        </p>
      )}

      {/* Warnings */}
      {lastResult?.aiFilters?.advertencias && lastResult.aiFilters.advertencias.length > 0 && (
        <div className="text-muted-foreground space-y-0.5 text-xs">
          {lastResult.aiFilters.advertencias.map((w, i) => (
            <p key={i}>⚠ {w}</p>
          ))}
        </div>
      )}

      {/* Error */}
      {error && (
        <p className="text-destructive flex items-center gap-1 text-sm">
          <AlertCircle className="h-3.5 w-3.5" />
          {error}
        </p>
      )}
    </div>
  );
}
