/**
 * Checkout Service
 *
 * Handles checkout and payment processing
 */

import { apiClient, authTokens } from '@/lib/api-client';
import type { PlatformPricing } from '@/hooks/use-platform-pricing';

// Server Actions — payment mutations run on the server, invisible to browser DevTools
import {
  serverCreateCheckoutSession,
  serverProcessPayment,
  serverValidatePromoCode,
  serverCreatePaymentIntent,
} from '@/actions/checkout';

// =============================================================================
// TYPES
// =============================================================================

export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  currency: 'DOP' | 'USD';
  type: 'boost' | 'subscription' | 'listing';
  features: string[];
  duration?: number; // days for boost, null for subscription
}

export interface CheckoutSession {
  sessionId: string;
  productId: string;
  product: Product;
  subtotal: number;
  tax: number;
  total: number;
  currency: 'DOP' | 'USD';
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
  paymentUrl?: string; // For redirect to payment gateway
}

export interface CreateCheckoutRequest {
  productId: string;
  vehicleId?: string; // For boost products
  dealerId?: string; // For dealer subscriptions
  promoCode?: string;
  paymentMethod: string; // Dynamic: 'azul', 'cardnet', 'pixelpay', 'fygaro', 'paypal', etc.
  returnUrl?: string;
  cancelUrl?: string;
}

/** Provider info returned by GET /api/payments/providers/available */
export interface AvailableProvider {
  gateway: string; // e.g. "Azul", "CardNET", "PixelPay", "Fygaro", "PayPal"
  name: string;
  type: string; // "CreditCard", "EWallet", etc.
  isConfigured: boolean;
}

export interface AvailableProvidersResponse {
  totalProviders: number;
  providers: AvailableProvider[];
}

export interface PaymentIntent {
  id: string;
  clientSecret: string;
  amount: number;
  currency: 'DOP' | 'USD';
  status:
    | 'requires_payment_method'
    | 'requires_confirmation'
    | 'requires_action'
    | 'processing'
    | 'succeeded'
    | 'canceled';
}

export interface ProcessPaymentRequest {
  sessionId: string;
  paymentMethodId?: string;
  cardToken?: string;
  paymentGateway?: string;
  cardholderName?: string;
  last4?: string;
}

export interface ProcessPaymentResponse {
  success: boolean;
  orderId?: string;
  transactionId?: string;
  receiptUrl?: string;
  error?: string;
}

export interface PromoCodeValidation {
  valid: boolean;
  discountType: 'percentage' | 'fixed';
  discountValue: number;
  discountAmount: number;
  newTotal: number;
  errorMessage?: string;
}

// =============================================================================
// STATIC PRODUCTS (fallback)
// =============================================================================

const staticProducts: Record<string, Product> = {
  'boost-basic': {
    id: 'boost-basic',
    name: 'Boost Básico',
    description: 'Destaca tu vehículo por 7 días',
    price: 499,
    currency: 'DOP',
    type: 'boost',
    duration: 7,
    features: ['Publicación destacada 7 días', 'Badge "Destacado"', '+50% más vistas'],
  },
  'boost-premium': {
    id: 'boost-premium',
    name: 'Boost Premium',
    description: 'Destaca tu vehículo por 30 días',
    price: 1499,
    originalPrice: 1996,
    currency: 'DOP',
    type: 'boost',
    duration: 30,
    features: [
      'Publicación destacada 30 días',
      'Badge "Destacado"',
      'Aparece en homepage',
      '+150% más vistas',
      'Estadísticas avanzadas',
    ],
  },
  'dealer-libre': {
    id: 'dealer-libre',
    name: 'Plan Dealer LIBRE',
    description: 'Plan gratuito para dealers',
    price: 0,
    currency: 'DOP',
    type: 'subscription',
    features: [
      'Vehículos ilimitados',
      'Panel de control básico',
      'Soporte por email',
      'Estadísticas básicas',
    ],
  },
  'dealer-visible': {
    id: 'dealer-visible',
    name: 'Plan Dealer VISIBLE',
    description: 'Visibilidad mejorada para dealers',
    price: 1699,
    currency: 'DOP',
    type: 'subscription',
    features: [
      'Vehículos ilimitados',
      'Badge de verificación',
      'Visibilidad mejorada',
      'Estadísticas avanzadas',
      '3 destacados/mes',
    ],
  },
  'dealer-pro': {
    id: 'dealer-pro',
    name: 'Plan Dealer PRO',
    description: 'Plan completo para dealers profesionales',
    price: 5199,
    currency: 'DOP',
    type: 'subscription',
    features: [
      'Todo de VISIBLE +',
      'ChatAgent IA',
      'CRM integrado',
      'Boosts incluidos',
      'Integración WhatsApp',
    ],
  },
  'listing-single': {
    id: 'listing-single',
    name: 'Publicación Individual',
    description: 'Publica un vehículo',
    price: 29,
    currency: 'USD',
    type: 'listing',
    duration: 30,
    features: [
      'Publicación por 30 días',
      'Hasta 20 fotos',
      'Contacto directo',
      'Estadísticas básicas',
    ],
  },
  'dealer-elite': {
    id: 'dealer-elite',
    name: 'Plan Dealer ÉLITE',
    description: 'Plan premium para grandes dealers',
    price: 11599,
    currency: 'DOP',
    type: 'subscription',
    features: [
      'Todo de PRO +',
      'Manager dedicado',
      'API access',
      'Múltiples ubicaciones',
      'Soporte 24/7',
      'White label',
    ],
  },
  'seller-gratis': {
    id: 'seller-gratis',
    name: 'Plan Vendedor Gratis',
    description: 'Plan gratuito para vendedores individuales',
    price: 0,
    currency: 'DOP',
    type: 'subscription',
    features: [
      '1 publicación activa',
      'Hasta 10 fotos por vehículo',
      'Duración: 30 días',
      'Contacto por WhatsApp',
    ],
  },
  'seller-premium': {
    id: 'seller-premium',
    name: 'Plan Vendedor Premium',
    description: 'Vende más rápido con herramientas premium',
    price: 1699,
    currency: 'DOP',
    type: 'subscription',
    features: [
      'Hasta 5 publicaciones activas',
      '30 fotos por vehículo',
      'Publicaciones permanentes',
      'Prioridad en búsquedas',
      'Badge verificado',
      '2 destacadas/mes',
      'Estadísticas detalladas',
    ],
  },
  'seller-pro': {
    id: 'seller-pro',
    name: 'Plan Vendedor PRO',
    description: 'Máxima visibilidad para vendedores profesionales',
    price: 3499,
    currency: 'DOP',
    type: 'subscription',
    features: [
      'Hasta 15 publicaciones activas',
      '50 fotos por vehículo',
      'Publicaciones permanentes',
      'Máxima prioridad',
      'Badge verificado',
      '5 destacadas/mes',
      'Analytics avanzados',
      'Alertas de baja de precio',
      'Soporte prioritario',
    ],
  },
};

// =============================================================================
// API FUNCTIONS
// =============================================================================

/**
 * Get product by ID
 */
export async function getProduct(productId: string): Promise<Product> {
  try {
    const response = await apiClient.get<Product>(`/api/checkout/products/${productId}`);
    return response.data;
  } catch {
    // Fallback to static products
    const product = staticProducts[productId];
    if (product) return product;
    throw new Error('Producto no encontrado');
  }
}

/**
 * Get all available products
 */
export async function getProducts(): Promise<Product[]> {
  try {
    const response = await apiClient.get<Product[]>('/api/checkout/products');
    return response.data;
  } catch {
    return Object.values(staticProducts);
  }
}

/**
 * Get available payment gateways (only those enabled by admin for new users).
 * Calls GET /api/payments/providers/available (AllowAnonymous).
 */
export async function getAvailableGateways(): Promise<AvailableProvider[]> {
  try {
    const response = await apiClient.get<AvailableProvidersResponse>(
      '/api/payments/providers/available'
    );
    return response.data.providers.filter(p => p.isConfigured);
  } catch {
    // Fallback: show Azul only (default Dominican gateway)
    return [
      {
        gateway: 'Azul',
        name: 'Azul (Banco Popular)',
        type: 'CreditCard',
        isConfigured: true,
      },
    ];
  }
}

/**
 * Create checkout session
 */
export async function createCheckoutSession(
  request: CreateCheckoutRequest
): Promise<CheckoutSession> {
  // ── Server Action: checkout session created server-side, invisible to browser ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverCreateCheckoutSession(
    request.productId,
    request.paymentMethod,
    accessToken || '',
    request.vehicleId,
    request.dealerId,
    request.promoCode,
    request.returnUrl,
    request.cancelUrl
  );

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al crear la sesión de pago');
  }

  // Get the product for the full CheckoutSession response
  const product = staticProducts[request.productId] || {
    id: request.productId,
    name: request.productId,
    description: '',
    price: result.data.subtotal,
    currency: result.data.currency,
    type: 'boost' as const,
    features: [],
  };

  return {
    sessionId: result.data.sessionId,
    productId: result.data.productId,
    product,
    subtotal: result.data.subtotal,
    tax: result.data.tax,
    total: result.data.total,
    currency: result.data.currency,
    status: result.data.status as CheckoutSession['status'],
    paymentUrl: result.data.paymentUrl,
  };
}

/**
 * Get checkout session
 */
export async function getCheckoutSession(sessionId: string): Promise<CheckoutSession> {
  const response = await apiClient.get<CheckoutSession>(`/api/checkout/sessions/${sessionId}`);
  return response.data;
}

/**
 * Create payment intent (for Stripe)
 */
export async function createPaymentIntent(sessionId: string): Promise<PaymentIntent> {
  // ── Server Action: payment intent created server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverCreatePaymentIntent(sessionId, accessToken || '');

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al crear el intento de pago');
  }

  return {
    id: result.data.id,
    clientSecret: result.data.clientSecret,
    amount: result.data.amount,
    currency: result.data.currency,
    status: result.data.status as PaymentIntent['status'],
  };
}

/**
 * Process payment
 */
export async function processPayment(
  request: ProcessPaymentRequest
): Promise<ProcessPaymentResponse> {
  // ── Server Action: payment processed server-side, card data invisible to browser ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverProcessPayment(
    request.sessionId,
    accessToken || '',
    request.paymentMethodId,
    request.cardToken
  );

  if (!result.success) {
    return {
      success: false,
      error: result.error || 'Error al procesar el pago',
    };
  }

  return {
    success: true,
    orderId: result.data?.orderId,
    transactionId: result.data?.transactionId,
    receiptUrl: result.data?.receiptUrl,
  };
}

/**
 * Validate promo code
 */
export async function validatePromoCode(
  code: string,
  productId: string
): Promise<PromoCodeValidation> {
  // ── Server Action: promo validation server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverValidatePromoCode(code, productId, accessToken || '');

  if (!result.success || !result.data) {
    return {
      valid: false,
      discountType: 'percentage',
      discountValue: 0,
      discountAmount: 0,
      newTotal: 0,
      errorMessage: result.error || 'Código promocional no válido',
    };
  }

  return result.data;
}

/**
 * Get checkout history
 */
export async function getCheckoutHistory(): Promise<CheckoutSession[]> {
  const response = await apiClient.get<CheckoutSession[]>('/api/checkout/history');
  return response.data;
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

// =============================================================================
// DYNAMIC PRICING INTEGRATION
// =============================================================================

// Current ITBIS rate (updated from ConfigurationService)
let _currentItbisRate = 0.18;
let _currentCurrency = 'DOP';

/**
 * Update static products with dynamic pricing from ConfigurationService.
 * Called by usePlatformPricing hook when pricing data is loaded.
 */
export function updateProductsWithPricing(pricing: PlatformPricing): void {
  _currentItbisRate = pricing.itbisPercentage / 100;
  _currentCurrency = pricing.currency;

  // Update boost-basic (featured listing pricing)
  if (staticProducts['boost-basic']) {
    staticProducts['boost-basic'].price = pricing.featuredListing;
    staticProducts['boost-basic'].currency = pricing.currency as 'DOP' | 'USD';
  }

  // Update boost-premium (premium listing pricing)
  if (staticProducts['boost-premium']) {
    staticProducts['boost-premium'].price = pricing.premiumListing;
    staticProducts['boost-premium'].currency = pricing.currency as 'DOP' | 'USD';
  }

  // Update dealer plans
  if (staticProducts['dealer-libre']) {
    staticProducts['dealer-libre'].price = pricing.dealerLibre;
    staticProducts['dealer-libre'].currency = pricing.currency as 'DOP' | 'USD';
  }
  if (staticProducts['dealer-visible']) {
    staticProducts['dealer-visible'].price = pricing.dealerVisible;
    staticProducts['dealer-visible'].currency = pricing.currency as 'DOP' | 'USD';
  }
  if (staticProducts['dealer-pro']) {
    staticProducts['dealer-pro'].price = pricing.dealerPro;
    staticProducts['dealer-pro'].currency = pricing.currency as 'DOP' | 'USD';
  }
  if (staticProducts['dealer-elite']) {
    staticProducts['dealer-elite'].price = pricing.dealerElite;
    staticProducts['dealer-elite'].currency = pricing.currency as 'DOP' | 'USD';
  }

  // Update listing-single
  if (staticProducts['listing-single']) {
    staticProducts['listing-single'].price = pricing.individualListingPrice;
    if (pricing.individualListingDays) {
      staticProducts['listing-single'].duration = pricing.individualListingDays;
    }
  }

  // Update seller plans
  if (staticProducts['seller-gratis']) {
    staticProducts['seller-gratis'].price = pricing.sellerGratis;
    staticProducts['seller-gratis'].currency = pricing.currency as 'DOP' | 'USD';
  }
  if (staticProducts['seller-premium']) {
    staticProducts['seller-premium'].price = pricing.sellerPremium;
    staticProducts['seller-premium'].currency = pricing.currency as 'DOP' | 'USD';
  }
  if (staticProducts['seller-pro']) {
    staticProducts['seller-pro'].price = pricing.sellerProPlan;
    staticProducts['seller-pro'].currency = pricing.currency as 'DOP' | 'USD';
  }
}

/**
 * Calculate ITBIS tax (dynamic rate from ConfigurationService)
 */
export function calculateTax(subtotal: number): number {
  return Math.round(subtotal * _currentItbisRate);
}

/**
 * Calculate total with tax
 */
export function calculateTotal(subtotal: number): number {
  return subtotal + calculateTax(subtotal);
}

/**
 * Get current ITBIS rate
 */
export function getItbisRate(): number {
  return _currentItbisRate;
}

/**
 * Get current currency
 */
export function getCurrentCurrency(): string {
  return _currentCurrency;
}

/**
 * Format currency
 */
export function formatCheckoutCurrency(amount: number, currency: 'DOP' | 'USD' = 'DOP'): string {
  if (currency === 'USD') {
    return `$${amount.toLocaleString()}`;
  }
  return `RD$${amount.toLocaleString()}`;
}

/**
 * Get static product (for fallback)
 */
export function getStaticProduct(productId: string): Product | null {
  return staticProducts[productId] || null;
}

// =============================================================================
// EXPORT SERVICE
// =============================================================================

export const checkoutService = {
  getProduct,
  getProducts,
  getAvailableGateways,
  createCheckoutSession,
  getCheckoutSession,
  createPaymentIntent,
  processPayment,
  validatePromoCode,
  getCheckoutHistory,
  calculateTax,
  calculateTotal,
  formatCurrency: formatCheckoutCurrency,
  getStaticProduct,
  updateProductsWithPricing,
  getItbisRate,
  getCurrentCurrency,
};

export default checkoutService;
