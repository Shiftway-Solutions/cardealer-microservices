'use client';

import * as React from 'react';
import { Clock, X, Trash2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useSearchStore, useRecentSearches, type RecentSearch } from '@/stores/search-store';

interface RecentSearchesDropdownProps {
  /** Whether the dropdown is visible */
  isOpen: boolean;
  /** Called when user clicks a recent search to apply it */
  onSelect: (search: RecentSearch) => void;
  /** Called when the dropdown should close */
  onClose: () => void;
  className?: string;
}

/**
 * RecentSearchesDropdown — Shows the user's recent search history
 * below the search input. Only renders when there are recent searches
 * and the input is focused.
 *
 * Integrated with Zustand search-store via `useRecentSearches()`.
 */
export function RecentSearchesDropdown({
  isOpen,
  onSelect,
  onClose,
  className,
}: RecentSearchesDropdownProps) {
  const recentSearches = useRecentSearches();
  const removeRecentSearch = useSearchStore(s => s.removeRecentSearch);
  const clearRecentSearches = useSearchStore(s => s.clearRecentSearches);
  const dropdownRef = React.useRef<HTMLDivElement>(null);

  // Close on click outside
  React.useEffect(() => {
    if (!isOpen) return;
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        onClose();
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [isOpen, onClose]);

  // Close on Escape
  React.useEffect(() => {
    if (!isOpen) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [isOpen, onClose]);

  if (!isOpen || recentSearches.length === 0) return null;

  // Show max 5 in dropdown
  const displaySearches = recentSearches.slice(0, 5);

  return (
    <div
      ref={dropdownRef}
      className={cn(
        'bg-popover border-border absolute top-full right-0 left-0 z-50 mt-1 overflow-hidden rounded-xl border shadow-xl',
        className
      )}
      role="listbox"
      aria-label="Búsquedas recientes"
    >
      {/* Header */}
      <div className="border-border flex items-center justify-between border-b px-3 py-2">
        <span className="text-muted-foreground flex items-center gap-1.5 text-xs font-medium">
          <Clock className="h-3.5 w-3.5" />
          Búsquedas recientes
        </span>
        <button
          type="button"
          onClick={e => {
            e.preventDefault();
            e.stopPropagation();
            clearRecentSearches();
            onClose();
          }}
          className="text-muted-foreground hover:text-foreground flex items-center gap-1 text-xs transition-colors"
          aria-label="Limpiar historial"
        >
          <Trash2 className="h-3 w-3" />
          Limpiar
        </button>
      </div>

      {/* Search items */}
      <ul className="py-1">
        {displaySearches.map(search => (
          <li key={search.id}>
            <button
              type="button"
              className="hover:bg-muted flex w-full items-center justify-between gap-2 px-3 py-2.5 text-left transition-colors"
              role="option"
              aria-selected={false}
              onClick={e => {
                e.preventDefault();
                e.stopPropagation();
                onSelect(search);
                onClose();
              }}
            >
              <div className="flex min-w-0 items-center gap-2">
                <Clock className="text-muted-foreground h-3.5 w-3.5 flex-shrink-0" />
                <span className="text-foreground truncate text-sm">{search.label}</span>
              </div>
              <div className="flex items-center gap-2">
                {search.resultCount != null && (
                  <span className="text-muted-foreground text-xs">
                    {search.resultCount} resultados
                  </span>
                )}
                <span
                  role="button"
                  tabIndex={0}
                  onClick={e => {
                    e.preventDefault();
                    e.stopPropagation();
                    removeRecentSearch(search.id);
                  }}
                  onKeyDown={e => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      e.stopPropagation();
                      removeRecentSearch(search.id);
                    }
                  }}
                  className="text-muted-foreground hover:text-foreground rounded p-0.5 transition-colors cursor-pointer"
                  aria-label={`Eliminar búsqueda: ${search.label}`}
                >
                  <X className="h-3.5 w-3.5" />
                </span>
              </div>
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
