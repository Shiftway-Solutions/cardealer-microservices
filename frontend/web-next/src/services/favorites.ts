/**
 * Favorites Service
 *
 * Handles all favorite-related API calls
 * Manages user's saved/favorited vehicles
 */

import { apiClient } from '@/lib/api-client';

// =============================================================================
// TYPES
// =============================================================================

export interface FavoriteVehicle {
  id: string;
  vehicleId: string;
  userId: string;
  createdAt: string;
  vehicle: {
    id: string;
    slug: string;
    title: string;
    make: string;
    model: string;
    year: number;
    price: number;
    mileage: number;
    transmission: string;
    fuelType: string;
    /** bodyType (del campo BodyStyle del backend) */
    bodyType: string;
    location: string;
    imageUrl: string;
    dealRating?: 'great' | 'good' | 'fair' | 'high';
    /** Siempre en minúsculas: 'active' | 'sold' | 'pending' | 'removed' */
    status: 'active' | 'sold' | 'pending' | 'removed';
    priceChanged?: boolean;
    previousPrice?: number;
  };
  notes?: string;
  /** notifyOnPriceChange — campo notifyPriceChange del backend mapeado con "On" */
  notifyOnPriceChange: boolean;
}

export interface AddFavoriteRequest {
  vehicleId: string;
  notes?: string;
  notifyOnPriceChange?: boolean;
}

export interface UpdateFavoriteRequest {
  notes?: string;
  notifyOnPriceChange?: boolean;
}

export interface FavoritesListResponse {
  favorites: FavoriteVehicle[];
  total: number;
}

// =============================================================================
// LOCAL STORAGE (for unauthenticated users)
// =============================================================================

const FAVORITES_STORAGE_KEY = 'okla-favorites';

function getLocalFavorites(): string[] {
  if (typeof window === 'undefined') return [];

  try {
    const stored = localStorage.getItem(FAVORITES_STORAGE_KEY);
    return stored ? JSON.parse(stored) : [];
  } catch {
    return [];
  }
}

function setLocalFavorites(vehicleIds: string[]): void {
  if (typeof window === 'undefined') return;

  try {
    localStorage.setItem(FAVORITES_STORAGE_KEY, JSON.stringify(vehicleIds));
  } catch {
    // Storage full or disabled
  }
}

function addLocalFavorite(vehicleId: string): void {
  const favorites = getLocalFavorites();
  if (!favorites.includes(vehicleId)) {
    favorites.push(vehicleId);
    setLocalFavorites(favorites);
  }
}

function removeLocalFavorite(vehicleId: string): void {
  const favorites = getLocalFavorites();
  const index = favorites.indexOf(vehicleId);
  if (index > -1) {
    favorites.splice(index, 1);
    setLocalFavorites(favorites);
  }
}

function isLocalFavorite(vehicleId: string): boolean {
  const favorites = getLocalFavorites();
  return favorites.includes(vehicleId);
}

function clearLocalFavorites(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(FAVORITES_STORAGE_KEY);
}

// =============================================================================
// API SERVICE
// =============================================================================

export const favoritesService = {
  /**
   * Get all favorites for the authenticated user
   */
  async getFavorites(): Promise<FavoritesListResponse> {
    const response = await apiClient.get<FavoritesListResponse>('/api/favorites');
    return response.data;
  },

  /**
   * Check if a vehicle is in favorites
   */
  async isFavorite(vehicleId: string): Promise<boolean> {
    try {
      const response = await apiClient.get<{ isFavorite: boolean }>(
        `/api/favorites/check/${vehicleId}`
      );
      return response.data.isFavorite;
    } catch {
      return false;
    }
  },

  /**
   * Add a vehicle to favorites
   */
  async addFavorite(request: AddFavoriteRequest): Promise<FavoriteVehicle> {
    const { vehicleId, ...body } = request;
    const response = await apiClient.post<FavoriteVehicle>(`/api/favorites/${vehicleId}`, body);
    return response.data;
  },

  /**
   * Remove a vehicle from favorites
   */
  async removeFavorite(vehicleId: string): Promise<void> {
    await apiClient.delete(`/api/favorites/${vehicleId}`);
  },

  /**
   * Update favorite settings (notes, notifications)
   */
  async updateFavorite(
    vehicleId: string,
    request: UpdateFavoriteRequest
  ): Promise<FavoriteVehicle> {
    const response = await apiClient.put<FavoriteVehicle>(`/api/favorites/${vehicleId}`, request);
    return response.data;
  },

  /**
   * Get count of favorites
   */
  async getFavoritesCount(): Promise<number> {
    const response = await apiClient.get<{ count: number }>('/api/favorites/count');
    return response.data.count;
  },

  /**
   * Sync local favorites to server after login
   */
  async syncLocalFavorites(): Promise<void> {
    const localFavorites = getLocalFavorites();

    if (localFavorites.length === 0) return;

    // Add each local favorite to server
    await Promise.all(
      localFavorites.map(vehicleId =>
        this.addFavorite({ vehicleId }).catch(() => {
          // Ignore errors (vehicle might not exist or already favorited)
        })
      )
    );

    // Clear local storage after sync
    clearLocalFavorites();
  },

  // =============================================================================
  // LOCAL STORAGE METHODS (for unauthenticated users)
  // =============================================================================

  local: {
    getFavorites: getLocalFavorites,
    addFavorite: addLocalFavorite,
    removeFavorite: removeLocalFavorite,
    isFavorite: isLocalFavorite,
    clear: clearLocalFavorites,
  },
};

export default favoritesService;
