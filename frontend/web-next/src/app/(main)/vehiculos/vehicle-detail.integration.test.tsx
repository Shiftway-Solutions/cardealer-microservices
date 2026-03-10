/**
 * Vehicle Detail Integration Tests
 * Priority: P0 - Vehicle detail page functionality
 *
 * Tests mock the service layer directly for reliable testing
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import * as React from 'react';

// Mock the vehicles service
vi.mock('@/services/vehicles', () => ({
  vehicleService: {
    search: vi.fn(),
    getById: vi.fn(),
    getBySlug: vi.fn(),
    getSimilar: vi.fn(),
    getFeatured: vi.fn(),
    trackView: vi.fn(),
  },
  getVehiclesByDealer: vi.fn(),
  getVehiclesByIds: vi.fn(),
  createVehicle: vi.fn(),
  updateVehicle: vi.fn(),
  deleteVehicle: vi.fn(),
  getMakes: vi.fn(),
  getModelsByMake: vi.fn(),
  getBodyTypes: vi.fn(),
  getFuelTypes: vi.fn(),
  getTransmissions: vi.fn(),
  getColors: vi.fn(),
  getProvinces: vi.fn(),
  getFeatures: vi.fn(),
}));

// Import after mocking
import { useVehicle, useVehicleBySlug, useSimilarVehicles } from '@/hooks/use-vehicles';
import { vehicleService } from '@/services/vehicles';

const mockGetById = vehicleService.getById as ReturnType<typeof vi.fn>;
const mockGetBySlug = vehicleService.getBySlug as ReturnType<typeof vi.fn>;
const mockGetSimilar = vehicleService.getSimilar as ReturnType<typeof vi.fn>;

// Mock vehicle detail data
const mockVehicleDetail = {
  id: 'v1',
  slug: 'toyota-corolla-2023',
  make: 'Toyota',
  model: 'Corolla',
  year: 2023,
  price: 1850000,
  marketPrice: 2000000,
  currency: 'DOP',
  mileage: 15000,
  transmission: 'automatic',
  fuelType: 'gasoline',
  engineSize: '1.8L',
  cylinders: 4,
  horsepower: 139,
  drivetrain: 'FWD',
  exteriorColor: 'Blanco Perla',
  interiorColor: 'Negro',
  vin: 'JTDBR32E123456789',
  description: 'Excelente condición, único dueño, mantenimiento al día.',
  features: [
    'Cámara de reversa',
    'Apple CarPlay',
    'Android Auto',
    'Sensores de estacionamiento',
    'Control crucero adaptativo',
  ],
  images: [
    { id: 'img1', url: 'https://example.com/car1.jpg', order: 0, isPrimary: true },
    { id: 'img2', url: 'https://example.com/car2.jpg', order: 1, isPrimary: false },
    { id: 'img3', url: 'https://example.com/car3.jpg', order: 2, isPrimary: false },
  ],
  city: 'Santo Domingo',
  province: 'DN',
  status: 'active',
  condition: 'used',
  isFeatured: true,
  viewCount: 1250,
  createdAt: '2024-01-15T10:00:00Z',
  updatedAt: '2024-01-20T15:30:00Z',
  seller: {
    id: 'seller1',
    name: 'Auto Excellence RD',
    type: 'dealer',
    phone: '+1 809 555 1234',
    whatsapp: '+1 809 555 1234',
    isVerified: true,
    rating: 4.8,
    reviewCount: 156,
    responseTime: '< 1 hora',
    memberSince: '2020-05-10',
    listingsCount: 45,
  },
};

const mockSimilarVehicles = [
  { id: 'v2', make: 'Honda', model: 'Civic', year: 2023, price: 1900000 },
  { id: 'v3', make: 'Nissan', model: 'Sentra', year: 2023, price: 1700000 },
  { id: 'v4', make: 'Hyundai', model: 'Elantra', year: 2024, price: 1950000 },
];

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
    },
  });

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('Vehicle Detail Hook', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetById.mockResolvedValue(mockVehicleDetail);
    mockGetBySlug.mockResolvedValue(mockVehicleDetail);
    mockGetSimilar.mockResolvedValue(mockSimilarVehicles);
  });

  describe('useVehicle (by ID)', () => {
    it('should fetch vehicle by ID', async () => {
      const { result } = renderHook(() => useVehicle('v1'), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toBeDefined();
      expect(result.current.data?.id).toBe('v1');
      expect(result.current.data?.make).toBe('Toyota');
      expect(mockGetById).toHaveBeenCalledWith('v1');
    });

    it('should handle vehicle not found', async () => {
      mockGetById.mockRejectedValue(new Error('Vehicle not found'));

      const { result } = renderHook(() => useVehicle('non-existent'), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.error).toBeTruthy();
    });

    it('should not fetch when ID is undefined', () => {
      const { result } = renderHook(() => useVehicle(''), {
        wrapper: createWrapper(),
      });

      expect(result.current.isLoading).toBe(false);
      expect(mockGetById).not.toHaveBeenCalled();
    });
  });

  describe('useVehicleBySlug', () => {
    it('should fetch vehicle by slug', async () => {
      const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toBeDefined();
      expect(result.current.data?.slug).toBe('toyota-corolla-2023');
      expect(mockGetBySlug).toHaveBeenCalledWith('toyota-corolla-2023');
    });

    it('should handle invalid slug', async () => {
      mockGetBySlug.mockRejectedValue(new Error('Vehicle not found'));

      const { result } = renderHook(() => useVehicleBySlug('invalid-slug'), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.error).toBeTruthy();
    });
  });
});

describe('Vehicle Detail Data', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetBySlug.mockResolvedValue(mockVehicleDetail);
  });

  it('should include vehicle specifications', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.transmission).toBe('automatic');
    expect(vehicle?.fuelType).toBe('gasoline');
    expect(vehicle?.engineSize).toBe('1.8L');
    expect(vehicle?.cylinders).toBe(4);
    expect(vehicle?.horsepower).toBe(139);
    expect(vehicle?.drivetrain).toBe('FWD');
  });

  it('should include images array', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.images).toHaveLength(3);
    expect(vehicle?.images[0].isPrimary).toBe(true);
  });

  it('should include features list', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.features).toBeInstanceOf(Array);
    expect(vehicle?.features.length).toBeGreaterThan(0);
    expect(vehicle?.features).toContain('Apple CarPlay');
  });

  it('should include seller information', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const seller = result.current.data?.seller;
    expect(seller).toBeDefined();
    expect(seller?.name).toBe('Auto Excellence RD');
    expect(seller?.type).toBe('dealer');
    expect(seller?.isVerified).toBe(true);
    expect(seller?.rating).toBe(4.8);
  });

  it('should include price comparison data', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.price).toBe(1850000);
    expect(vehicle?.marketPrice).toBe(2000000);
    // Price is below market - good deal
    expect(vehicle?.price).toBeLessThan(vehicle?.marketPrice || 0);
  });

  it('should include location data', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.city).toBe('Santo Domingo');
    expect(vehicle?.province).toBe('DN');
  });
});

describe('Similar Vehicles', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetSimilar.mockResolvedValue(mockSimilarVehicles);
  });

  it('should fetch similar vehicles', async () => {
    const { result } = renderHook(() => useSimilarVehicles('v1'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toHaveLength(3);
    expect(result.current.data?.every(v => v.id !== 'v1')).toBe(true);
    expect(mockGetSimilar).toHaveBeenCalledWith('v1', 6);
  });

  it('should handle empty similar vehicles', async () => {
    mockGetSimilar.mockResolvedValue([]);

    const { result } = renderHook(() => useSimilarVehicles('v1'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toEqual([]);
  });

  it('should limit similar vehicles', async () => {
    const { result } = renderHook(() => useSimilarVehicles('v1', 2), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(mockGetSimilar).toHaveBeenCalledWith('v1', 2);
  });
});

describe('Vehicle Detail Caching', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetBySlug.mockResolvedValue(mockVehicleDetail);
  });

  it('should cache vehicle detail by slug', async () => {
    const wrapper = createWrapper();

    // First fetch
    const { result: result1 } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper,
    });

    await waitFor(() => {
      expect(result1.current.isLoading).toBe(false);
    });

    expect(mockGetBySlug).toHaveBeenCalledTimes(1);

    // Second fetch should use cache
    const { result: result2 } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper,
    });

    expect(result2.current.data?.slug).toBe('toyota-corolla-2023');
    expect(mockGetBySlug).toHaveBeenCalledTimes(1); // No additional call
  });

  it('should cache similar vehicles', async () => {
    mockGetSimilar.mockResolvedValue(mockSimilarVehicles);
    const wrapper = createWrapper();

    const { result: result1 } = renderHook(() => useSimilarVehicles('v1'), { wrapper });

    await waitFor(() => {
      expect(result1.current.isLoading).toBe(false);
    });

    expect(mockGetSimilar).toHaveBeenCalledTimes(1);

    // Same query should use cache
    const { result: result2 } = renderHook(() => useSimilarVehicles('v1'), { wrapper });

    expect(result2.current.data).toHaveLength(3);
    expect(mockGetSimilar).toHaveBeenCalledTimes(1);
  });
});

describe('Vehicle Contact Seller', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetBySlug.mockResolvedValue(mockVehicleDetail);
  });

  it('should have seller contact info', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const seller = result.current.data?.seller;
    expect(seller?.phone).toBeDefined();
    expect(seller?.whatsapp).toBeDefined();
  });

  it('should format WhatsApp link correctly', async () => {
    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const seller = result.current.data?.seller;
    expect(seller?.whatsapp).toBeDefined();

    // WhatsApp number should be present
    const whatsappNumber = seller?.whatsapp?.replace(/[^0-9]/g, '');
    expect(whatsappNumber?.length).toBeGreaterThan(10);
  });
});

describe('Deal Rating Calculation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should identify great deal (price 10%+ below market)', async () => {
    mockGetBySlug.mockResolvedValue({
      ...mockVehicleDetail,
      price: 1700000,
      marketPrice: 2000000, // 15% below market
    });

    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    const discount =
      ((vehicle?.marketPrice || 0) - (vehicle?.price || 0)) / (vehicle?.marketPrice || 1);
    expect(discount).toBeGreaterThanOrEqual(0.1); // 10%+ is a great deal
  });

  it('should identify high price (above market)', async () => {
    mockGetBySlug.mockResolvedValue({
      ...mockVehicleDetail,
      price: 2200000,
      marketPrice: 2000000, // 10% above market
    });

    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.price).toBeGreaterThan(vehicle?.marketPrice || 0);
  });

  it('should handle missing market price', async () => {
    mockGetBySlug.mockResolvedValue({
      ...mockVehicleDetail,
      price: 1850000,
      marketPrice: undefined,
    });

    const { result } = renderHook(() => useVehicleBySlug('toyota-corolla-2023'), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.data;
    expect(vehicle?.price).toBe(1850000);
    expect(vehicle?.marketPrice).toBeUndefined();
  });
});
