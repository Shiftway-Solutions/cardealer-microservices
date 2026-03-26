/**
 * Homepage Integration Tests
 * Priority: P0 - Main landing page functionality
 *
 * These tests mock the service layer directly for reliable testing
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import * as React from 'react';

// Mock the homepage-sections service
vi.mock('@/services/homepage-sections', () => ({
  getHomepageSections: vi.fn(),
  transformSection: vi.fn((dto: unknown) => dto),
}));

// Import after mocking
import { useHomepageSections } from '@/hooks/use-homepage-sections';
import { getHomepageSections } from '@/services/homepage-sections';

const mockGetHomepageSections = getHomepageSections as ReturnType<typeof vi.fn>;

// Mock section data
const mockSections = [
  {
    id: '1',
    name: 'Carousel Principal',
    slug: 'carousel',
    description: 'Banner principal',
    displayOrder: 1,
    maxItems: 5,
    isActive: true,
    icon: null,
    accentColor: 'blue',
    viewAllHref: '/vehiculos',
    layoutType: 'Hero',
    subtitle: '',
    vehicles: [
      {
        id: 'v1',
        name: 'Toyota Corolla',
        make: 'Toyota',
        model: 'Corolla',
        year: 2023,
        price: 1850000,
      },
      { id: 'v2', name: 'Honda Civic', make: 'Honda', model: 'Civic', year: 2024, price: 2100000 },
    ],
  },
  {
    id: '2',
    name: 'Sedanes',
    slug: 'sedanes',
    description: 'Los mejores sedanes',
    displayOrder: 2,
    maxItems: 10,
    isActive: true,
    icon: null,
    accentColor: 'gray',
    viewAllHref: '/vehiculos?bodyType=sedan',
    layoutType: 'Carousel',
    subtitle: 'Elegancia y confort',
    vehicles: [
      {
        id: 'v3',
        name: 'Nissan Sentra',
        make: 'Nissan',
        model: 'Sentra',
        year: 2023,
        price: 1600000,
      },
    ],
  },
  {
    id: '3',
    name: 'SUVs',
    slug: 'suvs',
    description: 'SUVs populares',
    displayOrder: 3,
    maxItems: 10,
    isActive: true,
    icon: null,
    accentColor: 'green',
    viewAllHref: '/vehiculos?bodyType=suv',
    layoutType: 'Grid',
    subtitle: 'Aventura y espacio',
    vehicles: [],
  },
  {
    id: '4',
    name: 'Destacados',
    slug: 'destacados',
    description: 'Vehículos destacados',
    displayOrder: 4,
    maxItems: 9,
    isActive: true,
    icon: null,
    accentColor: 'amber',
    viewAllHref: '/vehiculos?featured=true',
    layoutType: 'Featured',
    subtitle: 'Lo mejor de lo mejor',
    vehicles: [{ id: 'v4', name: 'BMW X5', make: 'BMW', model: 'X5', year: 2024, price: 4500000 }],
  },
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

describe('Homepage Sections Hook', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHomepageSections.mockResolvedValue(mockSections);
  });

  describe('useHomepageSections', () => {
    it('should fetch homepage sections', async () => {
      const { result } = renderHook(() => useHomepageSections(), {
        wrapper: createWrapper(),
      });

      expect(result.current.isLoading).toBe(true);

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.sections).toBeInstanceOf(Array);
      expect(result.current.sections.length).toBe(4);
      expect(mockGetHomepageSections).toHaveBeenCalledTimes(1);
    });

    it('should provide named section getters', async () => {
      const { result } = renderHook(() => useHomepageSections(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.carousel).toBeDefined();
      expect(result.current.carousel?.slug).toBe('carousel');
      expect(result.current.sedanes).toBeDefined();
      expect(result.current.sedanes?.slug).toBe('sedanes');
      expect(result.current.suvs).toBeDefined();
      expect(result.current.suvs?.slug).toBe('suvs');
      expect(result.current.destacados).toBeDefined();
      expect(result.current.destacados?.slug).toBe('destacados');
    });

    it('should provide getSection helper', async () => {
      const { result } = renderHook(() => useHomepageSections(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      const carousel = result.current.getSection('carousel');
      expect(carousel).toBeDefined();
      expect(carousel?.name).toBe('Carousel Principal');

      const sedanes = result.current.getSection('sedanes');
      expect(sedanes?.name).toBe('Sedanes');

      const nonExistent = result.current.getSection('non-existent');
      expect(nonExistent).toBeUndefined();
    });

    it('should handle error state', async () => {
      mockGetHomepageSections.mockRejectedValue(new Error('Network error'));

      const { result } = renderHook(() => useHomepageSections(), {
        wrapper: createWrapper(),
      });

      await waitFor(
        () => {
          expect(result.current.isLoading).toBe(false);
        },
        { timeout: 10_000 }
      );

      expect(result.current.error).toBeTruthy();
      expect(result.current.sections).toEqual([]);
    }, 15_000);

    it('should handle empty sections', async () => {
      mockGetHomepageSections.mockResolvedValue([]);

      const { result } = renderHook(() => useHomepageSections(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.sections).toEqual([]);
      expect(result.current.carousel).toBeUndefined();
      expect(result.current.sedanes).toBeUndefined();
    });
  });
});

describe('Homepage Sections with Vehicles', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHomepageSections.mockResolvedValue(mockSections);
  });

  it('should load sections with vehicles', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const carousel = result.current.carousel;
    expect(carousel).toBeDefined();
    expect(carousel?.vehicles).toHaveLength(2);
    expect(carousel?.vehicles[0].make).toBe('Toyota');
    expect(carousel?.vehicles[1].make).toBe('Honda');
  });

  it('should handle sections with empty vehicles', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const suvs = result.current.suvs;
    expect(suvs).toBeDefined();
    expect(suvs?.vehicles).toHaveLength(0);
  });

  it('should respect maxItems configuration', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const carousel = result.current.carousel;
    expect(carousel?.maxItems).toBe(5);
    expect(carousel?.vehicles.length).toBeLessThanOrEqual(carousel?.maxItems || 0);
  });
});

describe('Homepage Data Caching', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHomepageSections.mockResolvedValue(mockSections);
  });

  it('should cache sections data', async () => {
    const wrapper = createWrapper();

    // First render
    const { result: result1 } = renderHook(() => useHomepageSections(), { wrapper });

    await waitFor(() => {
      expect(result1.current.isLoading).toBe(false);
    });

    expect(mockGetHomepageSections).toHaveBeenCalledTimes(1);

    // Second render with same wrapper (shared cache)
    const { result: result2 } = renderHook(() => useHomepageSections(), { wrapper });

    // Should have data immediately (cached)
    expect(result2.current.sections.length).toBe(4);
    // Should not make a new API call
    expect(mockGetHomepageSections).toHaveBeenCalledTimes(1);
  });

  it('should provide refetch function', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(typeof result.current.refetch).toBe('function');

    // Refetch should work
    await result.current.refetch();
    expect(mockGetHomepageSections).toHaveBeenCalledTimes(2);
  });
});

describe('Homepage Section Display Order', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHomepageSections.mockResolvedValue(mockSections);
  });

  it('should maintain section order', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const sections = result.current.sections;
    expect(sections[0].slug).toBe('carousel');
    expect(sections[0].displayOrder).toBe(1);
    expect(sections[1].slug).toBe('sedanes');
    expect(sections[1].displayOrder).toBe(2);
    expect(sections[2].slug).toBe('suvs');
    expect(sections[2].displayOrder).toBe(3);
    expect(sections[3].slug).toBe('destacados');
    expect(sections[3].displayOrder).toBe(4);
  });
});

describe('Homepage Vehicle Card Data', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHomepageSections.mockResolvedValue(mockSections);
  });

  it('should have vehicle data for cards', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    const vehicle = result.current.carousel?.vehicles[0];
    expect(vehicle).toBeDefined();
    expect(vehicle?.id).toBe('v1');
    expect(vehicle?.make).toBe('Toyota');
    expect(vehicle?.model).toBe('Corolla');
    expect(vehicle?.year).toBe(2023);
    expect(vehicle?.price).toBe(1850000);
  });
});

describe('Homepage Section Layout Types', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHomepageSections.mockResolvedValue(mockSections);
  });

  it('should have different layout types', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.carousel?.layoutType).toBe('Hero');
    expect(result.current.sedanes?.layoutType).toBe('Carousel');
    expect(result.current.suvs?.layoutType).toBe('Grid');
    expect(result.current.destacados?.layoutType).toBe('Featured');
  });

  it('should have accent colors', async () => {
    const { result } = renderHook(() => useHomepageSections(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.carousel?.accentColor).toBe('blue');
    expect(result.current.sedanes?.accentColor).toBe('gray');
    expect(result.current.suvs?.accentColor).toBe('green');
    expect(result.current.destacados?.accentColor).toBe('amber');
  });
});
