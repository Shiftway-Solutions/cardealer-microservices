/**
 * Zustand Stores - Barrel Export
 *
 * All Zustand stores for client-side state management.
 * Server state is handled by TanStack Query (see hooks/).
 */

export {
  useSearchStore,
  useSearchFilters,
  useSearchViewMode,
  useRecentSearches,
  useActiveFilterCount,
  useFilterSidebarOpen,
  type SearchFilters,
  type ViewMode,
  type RecentSearch,
  type SearchDraft,
} from './search-store';
