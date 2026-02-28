/**
 * Alerts Service - API client for price alerts and saved searches
 * Connects via API Gateway to AlertService
 */

import { apiClient } from '@/lib/api-client';
import type { VehicleSearchParams, PaginatedResponse } from '@/types';

// ============================================================
// API TYPES
// ============================================================

// -----------------------------------------------------------------------------
// Price Alerts
// -----------------------------------------------------------------------------

export interface PriceAlert {
  id: string;
  userId: string;
  vehicleId: string;
  vehicleTitle: string; // Display title (e.g., "Toyota Camry 2024")
  vehicleName?: string; // Alias for compatibility
  vehicleImageUrl?: string;
  vehicleSlug: string;
  currentPrice: number;
  targetPrice: number;
  currency: 'DOP' | 'USD';
  notifyOnAnyChange: boolean;
  notifyPercentageThreshold?: number;
  isActive: boolean;
  isTriggered: boolean; // Whether the target price was reached
  lastCheckedAt: string;
  lastNotifiedAt?: string;
  priceHistory: PriceHistoryEntry[];
  createdAt: string;
  updatedAt: string;
}

export interface PriceHistoryEntry {
  price: number;
  date: string;
  change?: number;
  changePercentage?: number;
}

export interface CreatePriceAlertRequest {
  vehicleId: string;
  targetPrice?: number;
  notifyOnAnyChange?: boolean;
  notifyPercentageThreshold?: number;
}

export interface UpdatePriceAlertRequest {
  targetPrice?: number;
  notifyOnAnyChange?: boolean;
  notifyPercentageThreshold?: number;
  isActive?: boolean;
}

// -----------------------------------------------------------------------------
// Saved Searches
// -----------------------------------------------------------------------------

export interface SavedSearch {
  id: string;
  userId: string;
  name: string;
  searchParams: VehicleSearchParams;
  notifyNewListings: boolean;
  notifyFrequency: NotifyFrequency;
  matchCount: number;
  newMatchCount: number;
  lastMatchedAt?: string;
  lastNotifiedAt?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export type NotifyFrequency = 'instant' | 'daily' | 'weekly' | 'never';

export interface CreateSavedSearchRequest {
  name: string;
  searchParams: VehicleSearchParams;
  notifyNewListings?: boolean;
  notifyFrequency?: NotifyFrequency;
}

export interface UpdateSavedSearchRequest {
  name?: string;
  searchParams?: VehicleSearchParams;
  notifyNewListings?: boolean;
  notifyFrequency?: NotifyFrequency;
  isActive?: boolean;
}

export interface SavedSearchMatch {
  vehicleId: string;
  vehicleName: string;
  vehicleImageUrl?: string;
  vehicleSlug: string;
  price: number;
  matchedAt: string;
  isNew: boolean;
}

// -----------------------------------------------------------------------------
// Alert Stats
// -----------------------------------------------------------------------------

export interface AlertStats {
  totalPriceAlerts: number;
  activePriceAlerts: number;
  priceDropsThisMonth: number;
  totalSavedSearches: number;
  activeSavedSearches: number;
  newMatchesThisWeek: number;
}

// ============================================================
// API FUNCTIONS - PRICE ALERTS
// ============================================================

/**
 * Get user's price alerts
 */
export async function getPriceAlerts(
  params: { isActive?: boolean; page?: number; pageSize?: number } = {}
): Promise<PaginatedResponse<PriceAlert>> {
  const response = await apiClient.get('/api/pricealerts', { params });
  const data = response.data as Record<string, unknown>;

  // Handle both flat array and paginated response
  if (Array.isArray(data)) {
    const items = data as PriceAlert[];
    return {
      items,
      pagination: {
        page: 1,
        pageSize: items.length || 10,
        totalItems: items.length,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    };
  }

  const items = (data.items as PriceAlert[]) || [];
  const pagination = (data.pagination as {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  }) || { page: 1, pageSize: items.length, totalItems: items.length, totalPages: 1 };

  return {
    items,
    pagination: {
      ...pagination,
      hasNextPage: pagination.page < pagination.totalPages,
      hasPreviousPage: pagination.page > 1,
    },
  };
}

/**
 * Get price alert by ID
 */
export async function getPriceAlertById(id: string): Promise<PriceAlert> {
  const response = await apiClient.get<PriceAlert>(`/api/pricealerts/${id}`);
  return response.data;
}

/**
 * Get price alert for a specific vehicle
 */
export async function getPriceAlertForVehicle(vehicleId: string): Promise<PriceAlert | null> {
  try {
    const response = await apiClient.get<PriceAlert>(`/api/pricealerts/vehicle/${vehicleId}`);
    return response.data;
  } catch {
    return null;
  }
}

/**
 * Create a new price alert
 */
export async function createPriceAlert(data: CreatePriceAlertRequest): Promise<PriceAlert> {
  const response = await apiClient.post<PriceAlert>('/api/pricealerts', data);
  return response.data;
}

/**
 * Update a price alert
 */
export async function updatePriceAlert(
  id: string,
  data: UpdatePriceAlertRequest
): Promise<PriceAlert> {
  const response = await apiClient.put<PriceAlert>(`/api/pricealerts/${id}`, data);
  return response.data;
}

/**
 * Delete a price alert
 */
export async function deletePriceAlert(id: string): Promise<void> {
  await apiClient.delete(`/api/pricealerts/${id}`);
}

/**
 * Toggle price alert active status.
 * Calls /activate or /deactivate based on the current isActive state.
 * (Backend has no /toggle endpoint.)
 */
export async function togglePriceAlert(id: string, isActive: boolean): Promise<PriceAlert> {
  // isActive = current state → call opposite endpoint
  const endpoint = isActive ? 'deactivate' : 'activate';
  const response = await apiClient.post<PriceAlert>(`/api/pricealerts/${id}/${endpoint}`);
  return response.data;
}

/**
 * Get price history for a vehicle
 */
export async function getVehiclePriceHistory(vehicleId: string): Promise<PriceHistoryEntry[]> {
  const response = await apiClient.get<PriceHistoryEntry[]>(
    `/api/pricealerts/history/${vehicleId}`
  );
  return response.data;
}

// ============================================================
// API FUNCTIONS - SAVED SEARCHES
// ============================================================

/**
 * Get user's saved searches
 */
// Map backend SavedSearchDto (searchCriteria string) → frontend SavedSearch (searchParams object)
function mapBackendSavedSearch(item: Record<string, unknown>): SavedSearch {
  let searchParams: VehicleSearchParams = {};
  try {
    const raw = (item.searchCriteria as string) || '{}';
    searchParams = JSON.parse(raw) as VehicleSearchParams;
  } catch {
    // keep empty searchParams
  }
  const freqRaw = ((item.frequency as string) || 'daily').toLowerCase();
  const validFreqs: NotifyFrequency[] = ['instant', 'daily', 'weekly', 'never'];
  const notifyFrequency: NotifyFrequency = validFreqs.includes(freqRaw as NotifyFrequency)
    ? (freqRaw as NotifyFrequency)
    : 'daily';

  return {
    id: item.id as string,
    userId: (item.userId as string) || '',
    name: (item.name as string) || '',
    searchParams,
    notifyNewListings: (item.sendEmailNotifications as boolean) ?? true,
    notifyFrequency,
    matchCount: (item.matchCount as number) ?? 0,
    newMatchCount: (item.newMatchCount as number) ?? 0,
    lastMatchedAt: item.lastMatchedAt as string | undefined,
    lastNotifiedAt:
      (item.lastNotificationSent as string | undefined) ??
      (item.lastNotifiedAt as string | undefined),
    isActive: (item.isActive as boolean) ?? true,
    createdAt: item.createdAt as string,
    updatedAt: item.updatedAt as string,
  };
}

export async function getSavedSearches(
  params: { isActive?: boolean; page?: number; pageSize?: number } = {}
): Promise<PaginatedResponse<SavedSearch>> {
  const response = await apiClient.get<
    | Record<string, unknown>[]
    | {
        items: Record<string, unknown>[];
        pagination: { page: number; pageSize: number; totalItems: number; totalPages: number };
      }
  >('/api/savedsearches', { params });

  // Backend may return a plain array or a paginated object
  if (Array.isArray(response.data)) {
    const items = response.data.map(mapBackendSavedSearch);
    return {
      items,
      pagination: {
        page: 1,
        pageSize: items.length || 10,
        totalItems: items.length,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    };
  }

  const paginatedData = response.data as {
    items: Record<string, unknown>[];
    pagination: { page: number; pageSize: number; totalItems: number; totalPages: number };
  };
  const items = (paginatedData.items || []).map(mapBackendSavedSearch);
  const pagination = paginatedData.pagination || {
    page: 1,
    pageSize: items.length,
    totalItems: items.length,
    totalPages: 1,
  };
  return {
    items,
    pagination: {
      ...pagination,
      hasNextPage: pagination.page < pagination.totalPages,
      hasPreviousPage: pagination.page > 1,
    },
  };
}

/**
 * Get saved search by ID
 */
export async function getSavedSearchById(id: string): Promise<SavedSearch> {
  const response = await apiClient.get<Record<string, unknown>>(`/api/savedsearches/${id}`);
  return mapBackendSavedSearch(response.data);
}

/**
 * Create a new saved search
 * Maps frontend DTO → backend DTO:
 *   searchParams (object) → searchCriteria (JSON string)
 *   notifyNewListings     → sendEmailNotifications
 *   notifyFrequency       → frequency (numeric: 0=Instant,1=Daily,2=Weekly)
 */
export async function createSavedSearch(data: CreateSavedSearchRequest): Promise<SavedSearch> {
  const freqMap: Record<string, number> = { instant: 0, daily: 1, weekly: 2, never: 1 };
  const freq = data.notifyFrequency ?? 'daily';
  const payload = {
    name: data.name,
    searchCriteria: JSON.stringify(data.searchParams || {}),
    sendEmailNotifications: data.notifyNewListings ?? true,
    frequency: freqMap[freq] ?? 1,
  };
  const response = await apiClient.post<Record<string, unknown>>('/api/savedsearches', payload);
  return mapBackendSavedSearch(response.data);
}

/**
 * Update a saved search
 */
export async function updateSavedSearch(
  id: string,
  data: UpdateSavedSearchRequest
): Promise<SavedSearch> {
  const payload: Record<string, unknown> = {};
  if (data.name !== undefined) payload.name = data.name;
  if (data.searchParams !== undefined) payload.searchCriteria = JSON.stringify(data.searchParams);
  if (data.notifyNewListings !== undefined) payload.sendEmailNotifications = data.notifyNewListings;
  if (data.notifyFrequency !== undefined) {
    const freqMap: Record<string, number> = { instant: 0, daily: 1, weekly: 2, never: 1 };
    payload.frequency = freqMap[data.notifyFrequency] ?? 1;
  }
  if (data.isActive !== undefined) payload.isActive = data.isActive;
  const response = await apiClient.put<Record<string, unknown>>(
    `/api/savedsearches/${id}`,
    payload
  );
  return mapBackendSavedSearch(response.data);
}

/**
 * Delete a saved search
 */
export async function deleteSavedSearch(id: string): Promise<void> {
  await apiClient.delete(`/api/savedsearches/${id}`);
}

/**
 * Toggle saved search notification status.
 * Calls PUT /api/savedsearches/{id}/notifications with flipped sendEmailNotifications.
 * (Backend has no /toggle endpoint — activate/deactivate control isActive, not notifications.)
 */
export async function toggleSavedSearch(
  id: string,
  currentState: { notifyNewListings: boolean; notifyFrequency: NotifyFrequency }
): Promise<SavedSearch> {
  const freqMap: Record<string, number> = { instant: 0, daily: 1, weekly: 2, never: 1 };
  const response = await apiClient.put<Record<string, unknown>>(
    `/api/savedsearches/${id}/notifications`,
    {
      sendEmailNotifications: !currentState.notifyNewListings,
      frequency: freqMap[currentState.notifyFrequency] ?? 1,
    }
  );
  return mapBackendSavedSearch(response.data);
}

/**
 * Get matches for a saved search
 */
export async function getSavedSearchMatches(
  id: string,
  params: { page?: number; pageSize?: number } = {}
): Promise<PaginatedResponse<SavedSearchMatch>> {
  const response = await apiClient.get<{
    items: SavedSearchMatch[];
    pagination: {
      page: number;
      pageSize: number;
      totalItems: number;
      totalPages: number;
    };
  }>(`/api/savedsearches/${id}/matches`, { params });

  return {
    items: response.data.items,
    pagination: {
      ...response.data.pagination,
      hasNextPage: response.data.pagination.page < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.page > 1,
    },
  };
}

/**
 * Mark saved search matches as seen
 */
export async function markMatchesAsSeen(id: string): Promise<void> {
  await apiClient.post(`/api/savedsearches/${id}/mark-seen`);
}

/**
 * Run saved search — no-op: the backend has no /run endpoint.
 * Navigation to /vehiculos with the saved search params is handled
 * directly in the UI (busquedas/page.tsx handleRunSearch).
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function runSavedSearch(_id: string): Promise<void> {
  return Promise.resolve();
}

// ============================================================
// API FUNCTIONS - STATS
// ============================================================

/**
 * Get alert statistics for the user
 */
export async function getAlertStats(): Promise<AlertStats> {
  try {
    const response = await apiClient.get<AlertStats>('/api/pricealerts/stats');
    return response.data;
  } catch {
    // Fallback: return zeros if stats endpoint not available
    return {
      totalPriceAlerts: 0,
      activePriceAlerts: 0,
      priceDropsThisMonth: 0,
      totalSavedSearches: 0,
      activeSavedSearches: 0,
      newMatchesThisWeek: 0,
    };
  }
}

// ============================================================
// HELPER FUNCTIONS
// ============================================================

/**
 * Format price change
 */
export function formatPriceChange(change: number, currency: 'DOP' | 'USD' = 'DOP'): string {
  const symbol = currency === 'USD' ? '$' : 'RD$';
  const prefix = change > 0 ? '+' : '';
  return `${prefix}${symbol}${Math.abs(change).toLocaleString()}`;
}

/**
 * Format percentage change
 */
export function formatPercentageChange(percentage: number): string {
  const prefix = percentage > 0 ? '+' : '';
  return `${prefix}${percentage.toFixed(1)}%`;
}

/**
 * Get change color class
 */
export function getChangeColor(change: number): string {
  if (change < 0) return 'text-emerald-600'; // Price dropped = good for buyer
  if (change > 0) return 'text-red-600'; // Price increased = bad for buyer
  return 'text-gray-600';
}

/**
 * Format notify frequency label
 */
export function formatNotifyFrequency(frequency: NotifyFrequency): string {
  const labels: Record<NotifyFrequency, string> = {
    instant: 'Instantáneo',
    daily: 'Diario',
    weekly: 'Semanal',
    never: 'Nunca',
  };
  return labels[frequency];
}

/**
 * Build search params description
 */
export function buildSearchDescription(params: VehicleSearchParams): string {
  const parts: string[] = [];

  if (params.make) parts.push(params.make);
  if (params.model) parts.push(params.model);
  if (params.yearMin && params.yearMax) {
    parts.push(`${params.yearMin}-${params.yearMax}`);
  } else if (params.yearMin) {
    parts.push(`${params.yearMin}+`);
  } else if (params.yearMax) {
    parts.push(`Hasta ${params.yearMax}`);
  }
  if (params.priceMax) {
    parts.push(`< RD$${(params.priceMax / 1000).toFixed(0)}K`);
  }
  if (params.bodyType) parts.push(params.bodyType);
  if (params.province) parts.push(params.province);

  return parts.join(' • ') || 'Todos los vehículos';
}

// ============================================================
// SERVICE EXPORT
// ============================================================

export const alertService = {
  // Price alerts
  getPriceAlerts,
  getPriceAlertById,
  getPriceAlertForVehicle,
  createPriceAlert,
  updatePriceAlert,
  deletePriceAlert,
  togglePriceAlert,
  getVehiclePriceHistory,
  // Saved searches
  getSavedSearches,
  getSavedSearchById,
  createSavedSearch,
  updateSavedSearch,
  deleteSavedSearch,
  toggleSavedSearch,
  getSavedSearchMatches,
  markMatchesAsSeen,
  runSavedSearch,
  // Stats
  getAlertStats,
  // Helpers
  formatPriceChange,
  formatPercentageChange,
  getChangeColor,
  formatNotifyFrequency,
  buildSearchDescription,
};

export default alertService;
