/**
 * User Service - API client for user operations
 * Connects via API Gateway to UserService
 */

import { apiClient } from '@/lib/api-client';
import type { User } from '@/types';

// ============================================================
// API TYPES
// ============================================================

export interface UserProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;
  phone?: string;
  bio?: string;
  location?: string;
  city?: string;
  province?: string;
  accountType:
    | 'buyer'
    | 'seller'
    | 'dealer'
    | 'dealer_employee'
    | 'admin'
    | 'platform_employee'
    | 'guest';
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
  isVerified: boolean;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  vehicleCount: number;
  reviewCount: number;
  averageRating: number;
  responseRate: number;
  memberSince: string;
  lastActive?: string;
  badges: UserBadge[];
  preferredLocale: string;
  preferredCurrency: 'DOP' | 'USD';
}

export interface UserBadge {
  id: string;
  name: string;
  icon: string;
  description: string;
  earnedAt: string;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  phone?: string;
  bio?: string;
  location?: string;
  city?: string;
  province?: string;
  preferredLocale?: string;
  preferredCurrency?: 'DOP' | 'USD';
}

export interface UserStats {
  vehiclesPublished: number;
  vehiclesSold: number;
  totalViews: number;
  totalInquiries: number;
  responseRate: number;
  averageResponseTime: string;
  reviewCount: number;
  averageRating: number;
}

export interface UserVehicleDto {
  id: string;
  slug: string;
  title: string;
  price: number;
  currency: 'DOP' | 'USD';
  year: number;
  mileage: number;
  imageUrl: string;
  status: 'active' | 'pending' | 'paused' | 'sold' | 'expired' | 'rejected';
  isFeatured: boolean;
  viewCount: number;
  inquiryCount: number;
  createdAt: string;
  expiresAt: string;
}

// ============================================================
// TRANSFORM FUNCTIONS
// ============================================================

export const transformUserProfile = (dto: UserProfileDto): User => ({
  id: dto.id,
  email: dto.email,
  firstName: dto.firstName,
  lastName: dto.lastName,
  fullName: dto.fullName,
  avatarUrl: dto.avatarUrl,
  phone: dto.phone,
  accountType: dto.accountType,
  userIntent: dto.userIntent,
  isVerified: dto.isVerified,
  isEmailVerified: dto.isEmailVerified,
  isPhoneVerified: dto.isPhoneVerified,
  preferredLocale: dto.preferredLocale,
  preferredCurrency: dto.preferredCurrency,
  createdAt: dto.memberSince,
});

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Get user profile by ID (public)
 */
export async function getUserById(id: string): Promise<UserProfileDto | null> {
  try {
    const response = await apiClient.get<UserProfileDto>(`/api/users/${id}`);
    return response.data;
  } catch {
    return null;
  }
}

/**
 * Get current user profile (authenticated)
 * Throws a specific error if user profile doesn't exist (404)
 */
export async function getCurrentProfile(): Promise<UserProfileDto> {
  try {
    const response = await apiClient.get<UserProfileDto>('/api/users/me');
    return response.data;
  } catch (error: unknown) {
    // If 404, user exists in AuthService but not in UserService (common with OAuth users)
    const axiosError = error as { response?: { status: number } };
    if (axiosError.response?.status === 404) {
      throw Object.assign(new Error('Profile not found in UserService'), {
        code: 'PROFILE_NOT_FOUND',
      });
    }
    throw error;
  }
}

/**
 * Update current user profile
 * Throws if user profile doesn't exist
 */
export async function updateProfile(data: UpdateProfileRequest): Promise<UserProfileDto> {
  try {
    const response = await apiClient.put<UserProfileDto>('/api/users/me', data);
    return response.data;
  } catch (error: unknown) {
    const axiosError = error as { response?: { status: number } };
    if (axiosError.response?.status === 404) {
      throw Object.assign(
        new Error('Tu perfil aún no está sincronizado. Por favor intenta más tarde.'),
        { code: 'PROFILE_NOT_FOUND' }
      );
    }
    throw error;
  }
}

/**
 * Upload avatar image
 */
export async function uploadAvatar(file: File): Promise<{ avatarUrl: string }> {
  const formData = new FormData();
  formData.append('file', file);

  try {
    const response = await apiClient.post<{ avatarUrl: string }>('/api/users/me/avatar', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  } catch (error: unknown) {
    const axiosError = error as { response?: { status: number } };
    if (axiosError.response?.status === 404) {
      throw Object.assign(
        new Error('Tu perfil aún no está sincronizado. Por favor intenta más tarde.'),
        { code: 'PROFILE_NOT_FOUND' }
      );
    }
    throw error;
  }
}

/**
 * Delete avatar
 */
export async function deleteAvatar(): Promise<void> {
  try {
    await apiClient.delete('/api/users/me/avatar');
  } catch (error: unknown) {
    const axiosError = error as { response?: { status: number } };
    if (axiosError.response?.status === 404) {
      // Ignore 404 - profile doesn't exist
      return;
    }
    throw error;
  }
}

/**
 * Get user stats (for dashboard)
 * Returns null if user profile doesn't exist
 */
export async function getUserStats(): Promise<UserStats | null> {
  try {
    const response = await apiClient.get<UserStats>('/api/users/me/stats');
    return response.data;
  } catch (error: unknown) {
    const axiosError = error as { response?: { status: number } };
    if (axiosError.response?.status === 404) {
      return null;
    }
    throw error;
  }
}

/**
 * Get user's vehicles
 * Returns empty array if user profile doesn't exist or endpoint is not available
 */
export async function getUserVehicles(params?: {
  status?: 'active' | 'pending' | 'sold' | 'expired' | 'all';
  page?: number;
  limit?: number;
}): Promise<{
  vehicles: UserVehicleDto[];
  total: number;
  page: number;
  totalPages: number;
}> {
  try {
    const response = await apiClient.get<{
      vehicles: UserVehicleDto[];
      total: number;
      page: number;
      totalPages: number;
    }>('/api/users/me/vehicles', { params });

    return response.data;
  } catch {
    // Always return empty list - the endpoint may not exist yet
    // or the user hasn't published any vehicles
    // The api-client interceptor already handles error transformation
    return { vehicles: [], total: 0, page: 1, totalPages: 0 };
  }
}

/**
 * Get another user's public vehicles
 */
export async function getPublicUserVehicles(
  userId: string,
  params?: {
    page?: number;
    limit?: number;
  }
): Promise<{
  vehicles: UserVehicleDto[];
  total: number;
  page: number;
  totalPages: number;
}> {
  const response = await apiClient.get<{
    vehicles: UserVehicleDto[];
    total: number;
    page: number;
    totalPages: number;
  }>(`/api/users/${userId}/vehicles`, { params });

  return response.data;
}

/**
 * Delete user account
 */
export async function deleteAccount(password: string): Promise<void> {
  await apiClient.delete('/api/users/me', { data: { password } });
}

/**
 * User service object
 */
export const userService = {
  getUserById,
  getCurrentProfile,
  updateProfile,
  uploadAvatar,
  deleteAvatar,
  getUserStats,
  getUserVehicles,
  getPublicUserVehicles,
  deleteAccount,
};

export default userService;
