/**
 * Vehicle Search Zustand Store
 *
 * Centralized client-side state for vehicle search across the app.
 * Manages:
 * - Active search filters
 * - Search history (recent searches)
 * - Saved search drafts (unsaved filter combos)
 * - UI state (sidebar open/close, view mode, sort)
 *
 * This store handles CLIENT-ONLY state. Server state (search results,
 * saved searches persisted in backend) is managed by TanStack Query
 * via the `use-vehicle-search` and `use-alerts` hooks.
 *
 * Persistence: Uses zustand/persist with localStorage.
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

// =============================================================================
// TYPES
// =============================================================================

export interface SearchFilters {
  // Text
  query?: string;

  // Basic
  make?: string;
  model?: string;
  yearMin?: number;
  yearMax?: number;
  priceMin?: number;
  priceMax?: number;

  // Additional
  mileageMax?: number;
  bodyType?: string;
  transmission?: 'automatica' | 'manual' | 'cvt';
  fuelType?: 'gasolina' | 'diesel' | 'electrico' | 'hibrido' | 'glp';
  drivetrain?: 'fwd' | 'rwd' | 'awd' | '4wd';
  condition?: 'nuevo' | 'usado';

  // Location (DR-specific)
  province?: string;
  city?: string;

  // Deal rating
  dealRating?: 'great' | 'good' | 'fair';

  // Seller type
  sellerType?: 'dealer' | 'seller';

  // Vehicle extras
  isCertified?: boolean;
  hasCleanTitle?: boolean;
  color?: string;
  features?: string[];

  // DR-market extended
  seats?: number;
  cylinders?: number;
  interiorColor?: string;

  // Sort & Pagination
  sortBy?:
    | 'price_asc'
    | 'price_desc'
    | 'year_desc'
    | 'year_asc'
    | 'mileage_asc'
    | 'newest'
    | 'relevance';
  page?: number;
  limit?: number;
}

export type ViewMode = 'grid' | 'list' | 'map';

export interface RecentSearch {
  id: string;
  filters: SearchFilters;
  label: string;
  resultCount?: number;
  timestamp: number;
}

export interface SearchDraft {
  id: string;
  name: string;
  filters: SearchFilters;
  createdAt: number;
}

// =============================================================================
// STORE STATE & ACTIONS
// =============================================================================

interface SearchState {
  // ── Active filters ──────────────────────────────────────────
  filters: SearchFilters;
  /** Stores previous filters for undo */
  previousFilters: SearchFilters | null;

  // ── UI state ────────────────────────────────────────────────
  isFilterSidebarOpen: boolean;
  viewMode: ViewMode;
  isSearching: boolean;

  // ── History ─────────────────────────────────────────────────
  recentSearches: RecentSearch[];

  // ── Drafts (unsaved filter combos) ──────────────────────────
  searchDrafts: SearchDraft[];
}

interface SearchActions {
  // ── Filter management ───────────────────────────────────────
  setFilter: <K extends keyof SearchFilters>(key: K, value: SearchFilters[K]) => void;
  setFilters: (filters: Partial<SearchFilters>) => void;
  clearFilter: (key: keyof SearchFilters) => void;
  clearAllFilters: () => void;
  undoFilterChange: () => void;

  // ── UI actions ──────────────────────────────────────────────
  toggleFilterSidebar: () => void;
  setViewMode: (mode: ViewMode) => void;
  setIsSearching: (searching: boolean) => void;

  // ── History actions ─────────────────────────────────────────
  addRecentSearch: (search: Omit<RecentSearch, 'id' | 'timestamp'>) => void;
  clearRecentSearches: () => void;
  removeRecentSearch: (id: string) => void;

  // ── Draft actions ───────────────────────────────────────────
  saveDraft: (name: string) => void;
  loadDraft: (id: string) => void;
  removeDraft: (id: string) => void;

  // ── Computed helpers ────────────────────────────────────────
  getActiveFilterCount: () => number;
  hasActiveFilters: () => boolean;
}

// =============================================================================
// DEFAULTS
// =============================================================================

const DEFAULT_FILTERS: SearchFilters = {
  page: 1,
  limit: 24,
  sortBy: 'relevance',
};

const MAX_RECENT_SEARCHES = 15;
const MAX_DRAFTS = 10;

// Non-filter keys that don't count toward "active filter count"
const PAGINATION_KEYS: (keyof SearchFilters)[] = ['page', 'limit', 'sortBy'];

// =============================================================================
// STORE
// =============================================================================

export const useSearchStore = create<SearchState & SearchActions>()(
  persist(
    (set, get) => ({
      // ── Initial state ─────────────────────────────────────────
      filters: { ...DEFAULT_FILTERS },
      previousFilters: null,
      isFilterSidebarOpen: false,
      viewMode: 'grid',
      isSearching: false,
      recentSearches: [],
      searchDrafts: [],

      // ── Filter management ─────────────────────────────────────

      setFilter: (key, value) => {
        set(state => ({
          previousFilters: { ...state.filters },
          filters: {
            ...state.filters,
            [key]: value,
            // Reset page to 1 when changing non-pagination filters
            ...(!PAGINATION_KEYS.includes(key) && { page: 1 }),
          },
        }));
      },

      setFilters: newFilters => {
        set(state => ({
          previousFilters: { ...state.filters },
          filters: {
            ...state.filters,
            ...newFilters,
            // Reset page when bulk-changing filters (unless page is explicitly set)
            page: newFilters.page ?? 1,
          },
        }));
      },

      clearFilter: key => {
        set(state => {
          const updated = { ...state.filters };
          delete updated[key];
          return {
            previousFilters: { ...state.filters },
            filters: { ...updated, page: 1 },
          };
        });
      },

      clearAllFilters: () => {
        set(state => ({
          previousFilters: { ...state.filters },
          filters: { ...DEFAULT_FILTERS },
        }));
      },

      undoFilterChange: () => {
        const { previousFilters } = get();
        if (previousFilters) {
          set(state => ({
            filters: previousFilters,
            previousFilters: { ...state.filters },
          }));
        }
      },

      // ── UI actions ────────────────────────────────────────────

      toggleFilterSidebar: () => {
        set(state => ({ isFilterSidebarOpen: !state.isFilterSidebarOpen }));
      },

      setViewMode: mode => {
        set({ viewMode: mode });
      },

      setIsSearching: searching => {
        set({ isSearching: searching });
      },

      // ── History actions ───────────────────────────────────────

      addRecentSearch: search => {
        set(state => {
          const newEntry: RecentSearch = {
            ...search,
            id: crypto.randomUUID(),
            timestamp: Date.now(),
          };

          // Deduplicate by label and keep latest
          const filtered = state.recentSearches.filter(s => s.label !== search.label);

          return {
            recentSearches: [newEntry, ...filtered].slice(0, MAX_RECENT_SEARCHES),
          };
        });
      },

      clearRecentSearches: () => {
        set({ recentSearches: [] });
      },

      removeRecentSearch: id => {
        set(state => ({
          recentSearches: state.recentSearches.filter(s => s.id !== id),
        }));
      },

      // ── Draft actions ─────────────────────────────────────────

      saveDraft: name => {
        set(state => {
          const draft: SearchDraft = {
            id: crypto.randomUUID(),
            name,
            filters: { ...state.filters },
            createdAt: Date.now(),
          };

          return {
            searchDrafts: [draft, ...state.searchDrafts].slice(0, MAX_DRAFTS),
          };
        });
      },

      loadDraft: id => {
        const { searchDrafts, filters } = get();
        const draft = searchDrafts.find(d => d.id === id);
        if (draft) {
          set({
            previousFilters: { ...filters },
            filters: { ...draft.filters, page: 1 },
          });
        }
      },

      removeDraft: id => {
        set(state => ({
          searchDrafts: state.searchDrafts.filter(d => d.id !== id),
        }));
      },

      // ── Computed helpers ──────────────────────────────────────

      getActiveFilterCount: () => {
        const { filters } = get();
        let count = 0;
        for (const [key, value] of Object.entries(filters)) {
          if (PAGINATION_KEYS.includes(key as keyof SearchFilters)) continue;
          if (value === undefined || value === null || value === '') continue;
          if (Array.isArray(value) && value.length === 0) continue;
          count++;
        }
        return count;
      },

      hasActiveFilters: () => {
        return get().getActiveFilterCount() > 0;
      },
    }),
    {
      name: 'okla-search-state',
      storage: createJSONStorage(() => localStorage),
      // Only persist these keys (exclude transient UI state)
      partialize: state => ({
        filters: state.filters,
        recentSearches: state.recentSearches,
        searchDrafts: state.searchDrafts,
        viewMode: state.viewMode,
      }),
      version: 1,
    }
  )
);

// =============================================================================
// SELECTOR HOOKS (for optimal re-renders)
// =============================================================================

/** Select only filters — avoids re-render on UI state changes */
export const useSearchFilters = () => useSearchStore(s => s.filters);

/** Select only view mode */
export const useSearchViewMode = () => useSearchStore(s => s.viewMode);

/** Select only recent searches */
export const useRecentSearches = () => useSearchStore(s => s.recentSearches);

/** Select active filter count */
export const useActiveFilterCount = () => useSearchStore(s => s.getActiveFilterCount());

/** Select sidebar state */
export const useFilterSidebarOpen = () => useSearchStore(s => s.isFilterSidebarOpen);
