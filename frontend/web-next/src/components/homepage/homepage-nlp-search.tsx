/**
 * Homepage NLP Search Bar
 *
 * A single natural-language search input that lives on the homepage.
 * Users type what they want in plain Spanish and the SearchAgent (Claude AI)
 * parses it into structured vehicle filters and redirects to /vehiculos.
 *
 * Examples:
 * - "Toyota Corolla 2020 automático menos de 800 mil"
 * - "SUV para familia, gasolina, buen precio"
 * - "Carro barato usado en Santiago"
 * - "Algo deportivo, no importa la marca"
 */

'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Search, Sparkles, X, Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { aiSearch, aiFiltersToSearchParams } from '@/services/search-agent';

// ============================================================
// SUGGESTION CHIPS — Common Dominican searches
// ============================================================

const SUGGESTION_CHIPS = [
  { label: '🚗 Toyota Corolla', query: 'Toyota Corolla' },
  { label: '🏔️ SUV Familiar', query: 'SUV familiar buen precio' },
  { label: '💰 Menos de 500K', query: 'Carro usado menos de 500 mil pesos' },
  { label: '⚡ Híbrido', query: 'Vehículo híbrido' },
  { label: '🛻 Camioneta', query: 'Camioneta pickup' },
  { label: '✨ Nuevo', query: 'Vehículo nuevo 2024 2025' },
];

// ============================================================
// COMPONENT
// ============================================================

interface HomepageNlpSearchProps {
  className?: string;
}

export function HomepageNlpSearch({ className }: HomepageNlpSearchProps) {
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState('');
  const [isSearching, setIsSearching] = useState(false);
  const [isFocused, setIsFocused] = useState(false);
  const [_error, setError] = useState<string | null>(null);

  const handleSearch = useCallback(
    async (searchQuery?: string) => {
      const q = (searchQuery || query).trim();
      if (!q) return;

      setIsSearching(true);
      setError(null);

      try {
        // Call the SearchAgent AI to parse natural language → filters
        const result = await aiSearch({ query: q });

        if (result.aiFilters?.filtros_exactos) {
          // Convert AI filters to URL search params
          const params = aiFiltersToSearchParams(result.aiFilters.filtros_exactos);

          // Build query string
          const searchParams = new URLSearchParams();
          searchParams.set('q', q); // Keep original query for display

          Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined && value !== null && value !== '') {
              searchParams.set(key, String(value));
            }
          });

          // Add sort if AI suggested one
          if (result.aiFilters.ordenar_por) {
            searchParams.set('sortBy', result.aiFilters.ordenar_por);
          }

          router.push(`/vehiculos?${searchParams.toString()}`);
        } else {
          // Fallback: just send the raw query
          router.push(`/vehiculos?q=${encodeURIComponent(q)}`);
        }
      } catch {
        // Fallback to basic search on error
        router.push(`/vehiculos?q=${encodeURIComponent(q)}`);
      } finally {
        setIsSearching(false);
      }
    },
    [query, router]
  );

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        handleSearch();
      }
    },
    [handleSearch]
  );

  const handleChipClick = useCallback(
    (chipQuery: string) => {
      setQuery(chipQuery);
      handleSearch(chipQuery);
    },
    [handleSearch]
  );

  // Focus input on mount for engagement
  useEffect(() => {
    const timer = setTimeout(() => {
      inputRef.current?.focus();
    }, 1500); // Wait for hero animation
    return () => clearTimeout(timer);
  }, []);

  return (
    <div className={cn('w-full', className)}>
      {/* Search Container */}
      <div
        className={cn(
          'relative mx-auto max-w-2xl transition-all duration-300',
          isFocused && 'scale-[1.02]'
        )}
      >
        {/* Glow effect on focus */}
        {isFocused && (
          <div className="absolute -inset-1 animate-pulse rounded-2xl bg-gradient-to-r from-emerald-400/30 via-teal-400/30 to-cyan-400/30 blur-lg" />
        )}

        {/* Input wrapper */}
        <div
          className={cn(
            'relative flex items-center gap-2 rounded-2xl border-2 bg-white px-4 py-3 shadow-xl transition-all sm:px-5 sm:py-4',
            isFocused
              ? 'border-emerald-400 shadow-emerald-100'
              : 'border-gray-200 shadow-gray-100/50'
          )}
        >
          {/* AI indicator */}
          <div className="flex items-center gap-1.5">
            <Sparkles
              className={cn(
                'h-5 w-5 transition-colors',
                isFocused ? 'text-emerald-500' : 'text-gray-300'
              )}
            />
          </div>

          {/* Input */}
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={e => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            onFocus={() => setIsFocused(true)}
            onBlur={() => setIsFocused(false)}
            placeholder='Busca tu vehículo ideal... ej: "SUV Toyota menos de 1 millón"'
            className="flex-1 bg-transparent text-base text-gray-900 placeholder-gray-400 outline-none sm:text-lg"
            disabled={isSearching}
            autoComplete="off"
          />

          {/* Clear button */}
          {query && !isSearching && (
            <button
              onClick={() => setQuery('')}
              className="rounded-full p-1 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600"
            >
              <X className="h-4 w-4" />
            </button>
          )}

          {/* Search button */}
          <button
            onClick={() => handleSearch()}
            disabled={isSearching || !query.trim()}
            className={cn(
              'flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold text-white transition-all',
              isSearching
                ? 'bg-gray-400'
                : query.trim()
                  ? 'bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-600 hover:to-teal-700 active:scale-95'
                  : 'bg-gray-300'
            )}
          >
            {isSearching ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                <span className="hidden sm:inline">Buscando...</span>
              </>
            ) : (
              <>
                <Search className="h-4 w-4" />
                <span className="hidden sm:inline">Buscar</span>
              </>
            )}
          </button>
        </div>

        {/* AI tag */}
        <div className="mt-2 flex items-center justify-center gap-1.5 text-xs text-gray-400">
          <Sparkles className="h-3 w-3" />
          <span>Búsqueda con IA — escribe lo que quieras en español</span>
        </div>
      </div>

      {/* Suggestion Chips */}
      <div className="mx-auto mt-4 flex max-w-2xl flex-wrap items-center justify-center gap-2">
        {SUGGESTION_CHIPS.map(chip => (
          <button
            key={chip.label}
            onClick={() => handleChipClick(chip.query)}
            disabled={isSearching}
            className="rounded-full border border-white/20 bg-white/10 px-3 py-1.5 text-xs font-medium text-white/90 backdrop-blur-sm transition-all hover:bg-white/20 hover:text-white active:scale-95"
          >
            {chip.label}
          </button>
        ))}
      </div>
    </div>
  );
}

export default HomepageNlpSearch;
