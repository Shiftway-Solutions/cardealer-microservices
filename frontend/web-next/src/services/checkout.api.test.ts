/**
 * Checkout Service Tests
 *
 * Tests for checkout and payment processing
 * @see src/services/checkout.ts
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// Mock server actions FIRST
vi.mock('@/actions/checkout', () => ({
  serverCreateCheckoutSession: vi.fn(),
  serverProcessPayment: vi.fn(),
  serverValidatePromoCode: vi.fn(),
}));

// Mock the api-client module
vi.mock('@/lib/api-client', () => ({
  apiClient: {
    get: vi.fn(),
  },
  authTokens: {
    getAccessToken: vi.fn().mockReturnValue('mock-token'),
    getRefreshToken: vi.fn().mockReturnValue('mock-refresh-token'),
    setAccessToken: vi.fn(),
    setRefreshToken: vi.fn(),
    clearTokens: vi.fn(),
  },
}));

import { apiClient } from '@/lib/api-client';
import {
  serverCreateCheckoutSession,
  serverProcessPayment,
  serverValidatePromoCode,
} from '@/actions/checkout';
import {
  getProduct,
  getProducts,
  createCheckoutSession,
  getCheckoutSession,
  processPayment,
  validatePromoCode,
  calculateTax,
  calculateTotal,
  getStaticProduct,
  type Product,
  type CheckoutSession,
} from './checkout';

// =============================================================================
// MOCK DATA
// =============================================================================

const mockProduct: Product = {
  id: 'boost-basic',
  name: 'Boost Básico',
  description: 'Destaca tu vehículo por 7 días',
  price: 499,
  currency: 'DOP',
  type: 'boost',
  duration: 7,
  features: ['Publicación destacada 7 días', 'Badge "Destacado"', '+50% más vistas'],
};

const mockProducts: Product[] = [
  mockProduct,
  {
    id: 'boost-premium',
    name: 'Boost Premium',
    description: 'Destaca tu vehículo por 30 días',
    price: 1499,
    currency: 'DOP',
    type: 'boost',
    duration: 30,
    features: ['Publicación destacada 30 días'],
  },
];

const mockCheckoutSession: CheckoutSession = {
  sessionId: 'session-123',
  productId: 'boost-basic',
  product: mockProduct,
  subtotal: 499,
  tax: 90,
  total: 589,
  currency: 'DOP',
  status: 'pending',
};

// =============================================================================
// TEST SETUP
// =============================================================================

describe('Checkout Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  // ===========================================================================
  // GET PRODUCTS
  // ===========================================================================

  describe('getProducts', () => {
    it('should fetch all products from API', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockProducts });

      const result = await getProducts();

      expect(apiClient.get).toHaveBeenCalledWith('/api/checkout/products');
      expect(result).toHaveLength(2);
      expect(result[0].id).toBe('boost-basic');
    });

    it('should fallback to static products on API error', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'));

      const result = await getProducts();

      // Should return static products as fallback
      expect(result.length).toBeGreaterThan(0);
      expect(result.some(p => p.id === 'boost-basic')).toBe(true);
    });
  });

  // ===========================================================================
  // GET PRODUCT BY ID
  // ===========================================================================

  describe('getProduct', () => {
    it('should fetch single product by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockProduct });

      const result = await getProduct('boost-basic');

      expect(apiClient.get).toHaveBeenCalledWith('/api/checkout/products/boost-basic');
      expect(result.id).toBe('boost-basic');
      expect(result.price).toBe(499);
    });

    it('should fallback to static product on API error', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Not found'));

      const result = await getProduct('boost-basic');

      // Should return static product as fallback
      expect(result.id).toBe('boost-basic');
    });

    it('should throw error when product not found in static fallback', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Not found'));

      await expect(getProduct('non-existent-product')).rejects.toThrow('Producto no encontrado');
    });
  });

  // ===========================================================================
  // CREATE CHECKOUT SESSION
  // ===========================================================================

  describe('createCheckoutSession', () => {
    it('should create checkout session', async () => {
      const mockResult = {
        success: true,
        data: {
          sessionId: 'session-123',
          productId: 'boost-basic',
          subtotal: 499,
          tax: 90,
          total: 589,
          currency: 'DOP',
          status: 'pending',
          paymentUrl: 'https://payment.example.com/session-123',
        },
      };

      vi.mocked(serverCreateCheckoutSession).mockResolvedValueOnce(mockResult);

      const result = await createCheckoutSession({
        productId: 'boost-basic',
        paymentMethod: 'card',
      });

      expect(serverCreateCheckoutSession).toHaveBeenCalled();
      expect(result.sessionId).toBe('session-123');
      expect(result.total).toBe(589);
    });

    it('should include promo code in request', async () => {
      const mockResult = {
        success: true,
        data: {
          sessionId: 'session-124',
          productId: 'boost-basic',
          subtotal: 399,
          tax: 72,
          total: 471,
          currency: 'DOP',
          status: 'pending',
          paymentUrl: 'https://payment.example.com/session-124',
        },
      };

      vi.mocked(serverCreateCheckoutSession).mockResolvedValueOnce(mockResult);

      await createCheckoutSession({
        productId: 'boost-basic',
        paymentMethod: 'azul',
        promoCode: 'DESCUENTO100',
      });

      expect(serverCreateCheckoutSession).toHaveBeenCalledWith(
        'boost-basic',
        'azul',
        'mock-token',
        undefined,
        undefined,
        'DESCUENTO100',
        undefined,
        undefined
      );
    });

    it('should support AZUL payment method', async () => {
      const mockResult = {
        success: true,
        data: {
          sessionId: 'session-125',
          productId: 'boost-basic',
          subtotal: 499,
          tax: 90,
          total: 589,
          currency: 'DOP',
          status: 'pending',
          paymentUrl: 'https://azul.example.com/session-125',
        },
      };

      vi.mocked(serverCreateCheckoutSession).mockResolvedValueOnce(mockResult);

      const result = await createCheckoutSession({
        productId: 'boost-basic',
        paymentMethod: 'azul',
      });

      expect(serverCreateCheckoutSession).toHaveBeenCalledWith(
        'boost-basic',
        'azul',
        'mock-token',
        undefined,
        undefined,
        undefined,
        undefined,
        undefined
      );
      expect(result.paymentUrl).toBeDefined();
    });
  });

  // ===========================================================================
  // GET CHECKOUT SESSION
  // ===========================================================================

  describe('getCheckoutSession', () => {
    it('should fetch checkout session by ID', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockCheckoutSession });

      const result = await getCheckoutSession('session-123');

      expect(apiClient.get).toHaveBeenCalledWith('/api/checkout/sessions/session-123');
      expect(result.sessionId).toBe('session-123');
    });
  });

  // ===========================================================================
  // PROCESS PAYMENT
  // ===========================================================================

  describe('processPayment', () => {
    it('should process payment successfully', async () => {
      vi.mocked(serverProcessPayment).mockResolvedValueOnce({
        success: true,
        data: {
          orderId: 'order-123',
          transactionId: 'txn-456',
          receiptUrl: 'https://receipt.url',
        },
      });

      const result = await processPayment({
        sessionId: 'session-123',
        paymentMethodId: 'pm_123',
      });

      expect(serverProcessPayment).toHaveBeenCalled();
      expect(result.orderId).toBe('order-123');
    });

    it('should handle payment failure', async () => {
      vi.mocked(serverProcessPayment).mockResolvedValueOnce({
        success: false,
        error: 'Insufficient funds',
      });

      const result = await processPayment({
        sessionId: 'session-123',
      });

      expect(result.success).toBe(false);
      expect(result.error).toBe('Insufficient funds');
    });
  });

  // ===========================================================================
  // VALIDATE PROMO CODE
  // ===========================================================================

  describe('validatePromoCode', () => {
    it('should validate valid promo code', async () => {
      vi.mocked(serverValidatePromoCode).mockResolvedValueOnce({
        success: true,
        data: {
          valid: true,
          discountType: 'percentage',
          discountValue: 20,
          discountAmount: 100,
          newTotal: 399,
        },
      });

      const result = await validatePromoCode('SAVE20', 'boost-basic');

      expect(serverValidatePromoCode).toHaveBeenCalled();
      expect(result.valid).toBe(true);
      expect(result.discountValue).toBe(20);
    });

    it('should return invalid for non-existent promo code', async () => {
      vi.mocked(serverValidatePromoCode).mockResolvedValueOnce({
        success: false,
        error: 'Not found',
      });

      const result = await validatePromoCode('INVALID', 'boost-basic');

      expect(result.valid).toBe(false);
    });
  });

  // ===========================================================================
  // TAX CALCULATIONS
  // ===========================================================================

  describe('calculateTax', () => {
    it('should calculate 18% ITBIS tax', () => {
      expect(calculateTax(1000)).toBe(180);
    });

    it('should round tax to nearest integer', () => {
      expect(calculateTax(499)).toBe(90); // 499 * 0.18 = 89.82 ≈ 90
    });

    it('should return 0 for 0 amount', () => {
      expect(calculateTax(0)).toBe(0);
    });
  });

  describe('calculateTotal', () => {
    it('should calculate total with tax', () => {
      expect(calculateTotal(1000)).toBe(1180);
    });

    it('should work with product prices', () => {
      const subtotal = 499;
      const total = calculateTotal(subtotal);
      expect(total).toBe(subtotal + calculateTax(subtotal));
    });
  });

  // ===========================================================================
  // STATIC PRODUCTS
  // ===========================================================================

  describe('getStaticProduct', () => {
    it('should return static product by ID', () => {
      const product = getStaticProduct('boost-basic');
      expect(product).not.toBeNull();
      expect(product?.id).toBe('boost-basic');
      expect(product?.price).toBe(499);
    });

    it('should return null for non-existent product', () => {
      const product = getStaticProduct('non-existent');
      expect(product).toBeNull();
    });

    it('should have all required product types', () => {
      expect(getStaticProduct('boost-basic')).not.toBeNull();
      expect(getStaticProduct('boost-premium')).not.toBeNull();
      expect(getStaticProduct('dealer-starter')).not.toBeNull();
      expect(getStaticProduct('dealer-pro')).not.toBeNull();
    });
  });
});
