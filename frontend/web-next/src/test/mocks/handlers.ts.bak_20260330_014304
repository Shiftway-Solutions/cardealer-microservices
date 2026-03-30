/**
 * MSW Request Handlers
 * Mock API responses for testing
 */

import { http, HttpResponse, delay } from 'msw';

const API_URL = 'http://localhost:8080';

// =============================================================================
// MOCK DATA
// =============================================================================

export const mockUser = {
  id: 'user-123',
  email: 'test@example.com',
  firstName: 'Juan',
  lastName: 'Pérez',
  fullName: 'Juan Pérez',
  avatarUrl: 'https://example.com/avatar.jpg',
  phone: '+1809555001',
  accountType: 'buyer' as const,
  isVerified: true,
  isEmailVerified: true,
  isPhoneVerified: false,
  preferredLocale: 'es-DO',
  preferredCurrency: 'DOP' as const,
  createdAt: '2024-01-15T10:00:00Z',
  lastLoginAt: '2026-02-01T08:30:00Z',
};

export const mockVehicle = {
  id: 'vehicle-123',
  slug: 'toyota-corolla-2023',
  make: 'Toyota',
  model: 'Corolla',
  year: 2023,
  trim: 'LE',
  bodyType: 'sedan',
  price: 1850000,
  originalPrice: 2000000,
  marketPrice: 1900000,
  currency: 'DOP',
  mileage: 15000,
  transmission: 'automatic',
  fuelType: 'gasoline',
  drivetrain: 'fwd',
  engineSize: '1.8L',
  horsepower: 169,
  exteriorColor: 'Blanco',
  interiorColor: 'Negro',
  doors: 4,
  seats: 5,
  features: ['A/C', 'Bluetooth', 'Cámara de reversa', 'CarPlay'],
  images: [
    {
      id: 'img-1',
      url: 'https://example.com/car1.jpg',
      thumbnailUrl: 'https://example.com/car1-thumb.jpg',
      alt: 'Toyota Corolla 2023',
      order: 0,
      isPrimary: true,
    },
  ],
  has360View: false,
  hasVideo: false,
  status: 'active',
  condition: 'used',
  isFeatured: true,
  viewCount: 150,
  favoriteCount: 25,
  sellerId: 'seller-456',
  sellerType: 'dealer',
  seller: {
    id: 'seller-456',
    name: 'Auto Premium RD',
    type: 'dealer' as const,
    avatar: 'https://example.com/dealer.jpg',
    phone: '+18095551234',
    email: 'ventas@autopremium.do',
    city: 'Santo Domingo',
    rating: 4.8,
    reviewCount: 125,
    responseRate: 95,
    responseTime: '< 1 hora',
    isVerified: true,
    memberSince: '2020-03-15',
    listingsCount: 45,
  },
  city: 'Santo Domingo',
  province: 'Distrito Nacional',
  country: 'DO',
  latitude: 18.4861,
  longitude: -69.9312,
  description: 'Excelente vehículo en perfectas condiciones.',
  vin: 'JTDKN3DU5A0123456',
  createdAt: '2025-12-01T10:00:00Z',
  updatedAt: '2026-01-15T14:30:00Z',
  publishedAt: '2025-12-01T12:00:00Z',
  isNegotiable: true,
  isCertified: true,
  isVerified: true,
};

export const mockVehicles = [
  mockVehicle,
  {
    ...mockVehicle,
    id: 'vehicle-124',
    slug: 'honda-civic-2024',
    make: 'Honda',
    model: 'Civic',
    year: 2024,
    price: 2100000,
  },
  {
    ...mockVehicle,
    id: 'vehicle-125',
    slug: 'hyundai-elantra-2023',
    make: 'Hyundai',
    model: 'Elantra',
    year: 2023,
    price: 1650000,
  },
];

export const mockDealer = {
  id: 'dealer-123',
  userId: 'user-456',
  businessName: 'Auto Premium RD',
  legalName: 'Auto Premium SRL',
  rnc: '101234567',
  type: 'independent',
  status: 'active',
  verificationStatus: 'verified',
  plan: 'pro',
  email: 'info@autopremium.do',
  phone: '+18095551234',
  website: 'https://autopremium.do',
  address: 'Av. 27 de Febrero #123',
  city: 'Santo Domingo',
  province: 'Distrito Nacional',
  description: 'Dealer premium de vehículos usados certificados.',
  logoUrl: 'https://example.com/dealer-logo.jpg',
  rating: 4.8,
  reviewCount: 125,
  activeListingsCount: 45,
  totalSalesCount: 320,
  isSubscriptionActive: true,
  createdAt: '2020-03-15T10:00:00Z',
};

export const mockHomepageSections = [
  {
    id: 'section-featured',
    name: 'Destacados',
    slug: 'destacados',
    displayOrder: 1,
    vehicles: mockVehicles.slice(0, 2),
  },
  {
    id: 'section-sedans',
    name: 'Sedanes',
    slug: 'sedanes',
    displayOrder: 2,
    vehicles: mockVehicles,
  },
];

// =============================================================================
// AUTH HANDLERS
// =============================================================================

export const authHandlers = [
  // Login
  http.post(`${API_URL}/api/auth/login`, async ({ request }) => {
    await delay(100);
    const body = (await request.json()) as { email: string; password: string };

    if (body.email === 'invalid@example.com') {
      return HttpResponse.json(
        { code: 'INVALID_CREDENTIALS', message: 'Email o contraseña incorrectos' },
        { status: 401 }
      );
    }

    return HttpResponse.json({
      accessToken: 'mock-access-token-123',
      refreshToken: 'mock-refresh-token-456',
      expiresIn: 3600,
      user: mockUser,
    });
  }),

  // Register
  http.post(`${API_URL}/api/auth/register`, async ({ request }) => {
    await delay(100);
    const body = (await request.json()) as { email: string };

    if (body.email === 'existing@example.com') {
      return HttpResponse.json(
        { code: 'EMAIL_EXISTS', message: 'Este email ya está registrado' },
        { status: 409 }
      );
    }

    return HttpResponse.json({
      accessToken: 'mock-access-token-new',
      refreshToken: 'mock-refresh-token-new',
      expiresIn: 3600,
      user: mockUser,
    });
  }),

  // Get current user
  http.get(`${API_URL}/api/auth/me`, async ({ request }) => {
    await delay(50);
    const authHeader = request.headers.get('Authorization');

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return HttpResponse.json({ code: 'UNAUTHORIZED', message: 'No autorizado' }, { status: 401 });
    }

    return HttpResponse.json(mockUser);
  }),

  // Logout
  http.post(`${API_URL}/api/auth/logout`, async () => {
    await delay(50);
    return HttpResponse.json({ success: true });
  }),

  // Refresh token
  http.post(`${API_URL}/api/auth/refresh`, async () => {
    await delay(50);
    return HttpResponse.json({
      accessToken: 'mock-access-token-refreshed',
      refreshToken: 'mock-refresh-token-refreshed',
      expiresIn: 3600,
    });
  }),

  // Forgot password
  http.post(`${API_URL}/api/auth/forgot-password`, async () => {
    await delay(100);
    return HttpResponse.json({ success: true, message: 'Email enviado' });
  }),

  // Reset password
  http.post(`${API_URL}/api/auth/reset-password`, async () => {
    await delay(100);
    return HttpResponse.json({ success: true });
  }),
];

// =============================================================================
// VEHICLES HANDLERS
// =============================================================================

export const vehicleHandlers = [
  // Search vehicles
  http.get(`${API_URL}/api/vehicles`, async ({ request }) => {
    await delay(100);
    const url = new URL(request.url);
    const page = parseInt(url.searchParams.get('page') || '1');
    const pageSize = parseInt(url.searchParams.get('pageSize') || '12');

    return HttpResponse.json({
      items: mockVehicles,
      page,
      pageSize,
      totalItems: mockVehicles.length,
      totalPages: 1,
    });
  }),

  // Get vehicle by ID
  http.get(`${API_URL}/api/vehicles/:id`, async ({ params }) => {
    await delay(50);
    const { id } = params;

    if (id === 'not-found') {
      return HttpResponse.json(
        { code: 'NOT_FOUND', message: 'Vehículo no encontrado' },
        { status: 404 }
      );
    }

    return HttpResponse.json(mockVehicle);
  }),

  // Get vehicle by slug
  http.get(`${API_URL}/api/vehicles/slug/:slug`, async ({ params }) => {
    await delay(50);
    const { slug } = params;

    if (slug === 'not-found') {
      return HttpResponse.json(
        { code: 'NOT_FOUND', message: 'Vehículo no encontrado' },
        { status: 404 }
      );
    }

    return HttpResponse.json(mockVehicle);
  }),

  // Get similar vehicles
  http.get(`${API_URL}/api/vehicles/:id/similar`, async () => {
    await delay(50);
    return HttpResponse.json(mockVehicles.slice(1));
  }),

  // Get featured vehicles
  http.get(`${API_URL}/api/vehicles/featured`, async () => {
    await delay(50);
    return HttpResponse.json(mockVehicles.filter(v => v.isFeatured));
  }),

  // Get vehicle history
  http.get(`${API_URL}/api/vehicles/:id/history`, async () => {
    await delay(50);
    return HttpResponse.json({
      records: [
        { date: '2024-01-15', type: 'service', description: 'Cambio de aceite' },
        { date: '2023-06-20', type: 'inspection', description: 'Inspección anual' },
      ],
    });
  }),

  // Increment view count
  http.post(`${API_URL}/api/vehicles/:id/view`, async () => {
    await delay(20);
    return HttpResponse.json({ success: true });
  }),
];

// =============================================================================
// FAVORITES HANDLERS
// =============================================================================

export const favoriteHandlers = [
  // Get favorites
  http.get(`${API_URL}/api/favorites`, async () => {
    await delay(100);
    return HttpResponse.json({
      items: mockVehicles.slice(0, 2).map(v => ({
        vehicleId: v.id,
        vehicle: v,
        addedAt: '2026-01-20T10:00:00Z',
        notes: 'Me interesa mucho',
        priceAtSave: v.price,
      })),
      totalCount: 2,
    });
  }),

  // Add to favorites
  http.post(`${API_URL}/api/favorites`, async () => {
    await delay(50);
    return HttpResponse.json({ success: true }, { status: 201 });
  }),

  // Remove from favorites
  http.delete(`${API_URL}/api/favorites/:vehicleId`, async () => {
    await delay(50);
    return HttpResponse.json({ success: true });
  }),

  // Check if favorite
  http.get(`${API_URL}/api/favorites/check/:vehicleId`, async () => {
    await delay(20);
    return HttpResponse.json({ isFavorite: true });
  }),
];

// =============================================================================
// HOMEPAGE SECTIONS HANDLERS
// =============================================================================

export const homepageHandlers = [
  http.get(`${API_URL}/api/homepagesections/homepage`, async () => {
    await delay(100);
    return HttpResponse.json(mockHomepageSections);
  }),
];

// =============================================================================
// DEALERS HANDLERS
// =============================================================================

export const dealerHandlers = [
  // Get dealer by ID
  http.get(`${API_URL}/api/dealers/:id`, async ({ params }) => {
    await delay(50);
    const { id } = params;

    if (id === 'not-found') {
      return HttpResponse.json(
        { code: 'NOT_FOUND', message: 'Dealer no encontrado' },
        { status: 404 }
      );
    }

    return HttpResponse.json(mockDealer);
  }),

  // Get dealer by user ID
  http.get(`${API_URL}/api/dealers/user/:userId`, async () => {
    await delay(50);
    return HttpResponse.json(mockDealer);
  }),

  // Get dealer vehicles
  http.get(`${API_URL}/api/dealers/:id/vehicles`, async () => {
    await delay(100);
    return HttpResponse.json({
      items: mockVehicles,
      page: 1,
      pageSize: 12,
      totalItems: mockVehicles.length,
      totalPages: 1,
    });
  }),
];

// =============================================================================
// CHECKOUT HANDLERS
// =============================================================================

export const checkoutHandlers = [
  // Create checkout session
  http.post(`${API_URL}/api/checkout/session`, async () => {
    await delay(100);
    return HttpResponse.json({
      sessionId: 'checkout-session-123',
      url: 'https://checkout.stripe.com/pay/session-123',
    });
  }),

  // Verify payment
  http.get(`${API_URL}/api/checkout/verify/:sessionId`, async () => {
    await delay(100);
    return HttpResponse.json({
      status: 'completed',
      paymentId: 'payment-123',
    });
  }),
];

// =============================================================================
// CONTACT HANDLERS
// =============================================================================

export const contactHandlers = [
  // Send contact message
  http.post(`${API_URL}/api/contact/vehicle/:vehicleId`, async () => {
    await delay(100);
    return HttpResponse.json({ success: true, messageId: 'msg-123' });
  }),

  // Get contact messages
  http.get(`${API_URL}/api/contact/messages`, async () => {
    await delay(100);
    return HttpResponse.json({
      items: [],
      totalCount: 0,
    });
  }),
];

// =============================================================================
// COMBINED HANDLERS
// =============================================================================

export const handlers = [
  ...authHandlers,
  ...vehicleHandlers,
  ...favoriteHandlers,
  ...homepageHandlers,
  ...dealerHandlers,
  ...checkoutHandlers,
  ...contactHandlers,
];
