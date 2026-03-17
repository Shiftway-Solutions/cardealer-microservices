/**
 * User Billing Service
 *
 * Frontend service for user billing operations (non-dealers)
 * Handles transactions, Early Bird status, payment history, and saved payment methods
 */

import { apiClient } from '@/lib/api-client';
import { formatDate } from '@/lib/utils';
export { formatDate };

// ============================================================================
// Types
// ============================================================================

export type TransactionStatus = 'Approved' | 'Declined' | 'Cancelled' | 'Error';

// Payment Gateway types
export type PaymentGateway = 'Azul' | 'CardNET' | 'PixelPay' | 'Fygaro' | 'PayPal' | 'Stripe';

export interface PaymentGatewayInfo {
  id: PaymentGateway;
  name: string;
  description: string;
  icon?: string;
  recommended?: boolean;
  local?: boolean; // true = Dominican Republic local
}

export interface CardInfo {
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
  cardHolderName?: string;
}

export interface PaymentMethodInfo {
  id: string;
  type: 'card' | 'bank_account';
  gateway: string; // Backend returns string, not enum
  isDefault: boolean;
  isActive: boolean;
  nickName?: string;
  card?: CardInfo;
  createdAt: string;
  lastUsedAt?: string;
  usageCount: number;
  isExpired: boolean;
  expiresSoon: boolean;
}

export interface CardDataRequest {
  number: string;
  expMonth: number;
  expYear: number;
  cvv: string;
  cardHolderName: string;
}

export interface AddPaymentMethodRequest {
  type: 'card' | 'bank_account'; // Requerido por backend
  gateway: PaymentGateway;
  card: CardDataRequest;
  nickName?: string;
  setAsDefault?: boolean;
}

// Response del backend para GET /payment-methods
export interface PaymentMethodsListResponse {
  methods: PaymentMethodInfo[];
  defaultMethodId: string | null;
  total: number;
  expiredCount: number;
  expiringSoonCount: number;
}

// Available payment gateways
export const availableGateways: PaymentGatewayInfo[] = [
  {
    id: 'Azul',
    name: 'Azul',
    description: 'Tarjetas dominicanas (Visa, MC, Discover)',
    recommended: true,
    local: true,
  },
  {
    id: 'CardNET',
    name: 'CardNET',
    description: 'Red de pagos local Visa/MasterCard',
    local: true,
  },
  {
    id: 'PixelPay',
    name: 'PixelPay',
    description: 'Centroamérica y Caribe',
    local: true,
  },
  {
    id: 'Fygaro',
    name: 'Fygaro',
    description: 'Crédito y débito local',
    local: true,
  },
  {
    id: 'PayPal',
    name: 'PayPal',
    description: 'Tarjetas internacionales y PayPal',
    local: false,
  },
  {
    id: 'Stripe',
    name: 'Stripe',
    description: 'Tarjetas internacionales, Apple Pay, Google Pay',
    recommended: false,
    local: false,
  },
];

export interface EarlyBirdStatus {
  isEnrolled: boolean;
  hasFounderBadge: boolean;
  isInFreePeriod: boolean;
  remainingFreeDays: number;
  enrolledAt?: string;
  freeUntil?: string;
  hasUsedBenefit?: boolean;
  benefitUsedAt?: string;
  message?: string;
}

export interface UserBillingSummary {
  totalTransactions: number;
  totalApproved: number;
  totalAmount: number;
  currency: string;
  isEarlyBirdMember: boolean;
  earlyBirdStatus?: EarlyBirdStatus;
}

export interface UserTransaction {
  id: string;
  orderNumber: string;
  amount: number;
  itbis: number;
  total: number;
  currency: string;
  status: TransactionStatus;
  statusDisplay: string;
  authorizationCode?: string;
  transactionDate: string;
  cardBrand?: string;
  cardLast4?: string;
  description: string;
}

export interface GetTransactionsParams {
  page?: number;
  pageSize?: number;
  status?: TransactionStatus;
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get billing summary for current user
 */
export async function getBillingSummary(): Promise<UserBillingSummary> {
  const response = await apiClient.get<UserBillingSummary>('/api/user-billing/summary');
  return response.data;
}

/**
 * Get transactions for current user
 */
export async function getTransactions(
  params: GetTransactionsParams = {}
): Promise<UserTransaction[]> {
  const { page = 1, pageSize = 10, status } = params;

  const queryParams = new URLSearchParams();
  queryParams.append('page', page.toString());
  queryParams.append('pageSize', pageSize.toString());
  if (status) {
    queryParams.append('status', status);
  }

  const response = await apiClient.get<UserTransaction[]>(
    `/api/user-billing/transactions?${queryParams.toString()}`
  );
  return response.data;
}

/**
 * Get a specific transaction by ID
 */
export async function getTransaction(transactionId: string): Promise<UserTransaction> {
  const response = await apiClient.get<UserTransaction>(
    `/api/user-billing/transactions/${transactionId}`
  );
  return response.data;
}

/**
 * Get Early Bird status for current user
 */
export async function getEarlyBirdStatus(): Promise<EarlyBirdStatus> {
  const response = await apiClient.get<EarlyBirdStatus>('/api/user-billing/early-bird');
  return response.data;
}

/**
 * Enroll current user in Early Bird program
 */
export async function enrollEarlyBird(freeMonths: number = 3): Promise<EarlyBirdStatus> {
  const response = await apiClient.post<EarlyBirdStatus>('/api/user-billing/early-bird/enroll', {
    freeMonths,
  });
  return response.data;
}

// ============================================================================
// Payment Methods API Functions
// ============================================================================

/**
 * Get saved payment methods for current user
 * Backend returns PaymentMethodsListDto with methods array
 */
export async function getPaymentMethods(): Promise<PaymentMethodInfo[]> {
  const response = await apiClient.get<PaymentMethodsListResponse>('/api/payment-methods');
  return response.data.methods || [];
}

/**
 * Get full payment methods response including metadata
 */
export async function getPaymentMethodsFull(): Promise<PaymentMethodsListResponse> {
  const response = await apiClient.get<PaymentMethodsListResponse>('/api/payment-methods');
  return response.data;
}

/**
 * Add a new payment method
 */
export async function addPaymentMethod(
  request: AddPaymentMethodRequest
): Promise<PaymentMethodInfo> {
  const response = await apiClient.post<PaymentMethodInfo>('/api/payment-methods', request);
  return response.data;
}

/**
 * Set a payment method as default
 * Backend uses POST not PUT
 */
export async function setDefaultPaymentMethod(paymentMethodId: string): Promise<void> {
  await apiClient.post(`/api/payment-methods/${paymentMethodId}/default`);
}

/**
 * Remove a payment method
 */
export async function removePaymentMethod(paymentMethodId: string): Promise<void> {
  await apiClient.delete(`/api/payment-methods/${paymentMethodId}`);
}

/**
 * Initiate card tokenization with a payment provider
 * Returns configuration for redirect, iframe, or SDK integration
 */
export interface TokenizationInitRequest {
  gateway: PaymentGateway;
  returnUrl: string;
  cancelUrl?: string;
  setAsDefault?: boolean;
  nickName?: string;
}

export interface SdkConfiguration {
  publicKey?: string;
  clientId?: string;
  merchantId?: string;
  environment: string;
  sdkUrl?: string;
  containerId?: string;
  styles?: Record<string, unknown>;
}

export interface TokenizationInitResponse {
  sessionId: string;
  gateway: string;
  integrationType: 'redirect' | 'iframe' | 'sdk' | 'popup';
  tokenizationUrl?: string;
  iframeUrl?: string;
  sdkConfig?: SdkConfiguration;
  formData?: Record<string, string>;
  expiresAt: string;
  providerData?: Record<string, unknown>;
}

export async function initiateTokenization(
  request: TokenizationInitRequest
): Promise<TokenizationInitResponse> {
  const response = await apiClient.post<TokenizationInitResponse>(
    '/api/payment-methods/tokenize/init',
    request
  );
  return response.data;
}

/**
 * Complete tokenization after provider callback
 * Called when user returns from provider's tokenization page
 */
export interface TokenizationCompleteRequest {
  sessionId: string;
  providerToken?: string;
  gateway: PaymentGateway;
  setAsDefault?: boolean;
  providerResponse?: Record<string, string>;
  // Provider-specific fields
  azulDataVaultToken?: string;
  azulOrderId?: string;
  cardNetToken?: string;
  pixelPayToken?: string;
  fygaroToken?: string;
  payPalVaultId?: string;
  braintreeNonce?: string;
}

export async function completeTokenization(
  request: TokenizationCompleteRequest
): Promise<PaymentMethodInfo> {
  const response = await apiClient.post<PaymentMethodInfo>(
    '/api/payment-methods/tokenize/complete',
    request
  );
  return response.data;
}

/**
 * Get tokenization configuration for a specific provider
 */
export interface ProviderTokenizationConfig {
  gateway: string;
  integrationType: 'redirect' | 'iframe' | 'sdk' | 'popup';
  displayName: string;
  description: string;
  supportsVaulting: boolean;
  supportedCardBrands: string[];
  isTestMode: boolean;
}

export async function getProviderConfig(
  gateway: PaymentGateway
): Promise<ProviderTokenizationConfig> {
  const response = await apiClient.get<ProviderTokenizationConfig>(
    `/api/payment-methods/tokenize/config/${gateway}`
  );
  return response.data;
}

/**
 * Get tokenization session status
 */
export interface TokenizationSession {
  sessionId: string;
  userId: string;
  gateway: string;
  returnUrl: string;
  cancelUrl?: string;
  setAsDefault: boolean;
  nickName?: string;
  createdAt: string;
  expiresAt: string;
  isCompleted: boolean;
}

export async function getTokenizationSession(sessionId: string): Promise<TokenizationSession> {
  const response = await apiClient.get<TokenizationSession>(
    `/api/payment-methods/tokenize/session/${sessionId}`
  );
  return response.data;
}

/**
 * Detect card brand from card number
 */
export function detectCardBrand(cardNumber: string): string {
  const number = cardNumber.replace(/\s/g, '');
  if (/^4/.test(number)) return 'Visa';
  if (/^5[1-5]/.test(number)) return 'Mastercard';
  if (/^3[47]/.test(number)) return 'Amex';
  if (/^6(?:011|5)/.test(number)) return 'Discover';
  if (/^35/.test(number)) return 'JCB';
  if (/^3(?:0[0-5]|[68])/.test(number)) return 'Diners';
  return 'Card';
}

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Format currency amount
 */
export function formatCurrency(amount: number, currency: string = 'DOP'): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: currency,
    minimumFractionDigits: 2,
  }).format(amount);
}

/**
 * Get status color class
 */
export function getStatusColor(status: TransactionStatus): string {
  switch (status) {
    case 'Approved':
      return 'text-green-600 bg-green-50';
    case 'Declined':
      return 'text-red-600 bg-red-50';
    case 'Cancelled':
      return 'text-yellow-600 bg-yellow-50';
    case 'Error':
      return 'text-red-600 bg-red-50';
    default:
      return 'text-gray-600 bg-gray-50';
  }
}

/**
 * Format date with time
 */
export function formatDateTime(dateString: string): string {
  return new Date(dateString).toLocaleString('es-DO', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// Export as default object for convenience
export const userBillingService = {
  getBillingSummary,
  getTransactions,
  getTransaction,
  getEarlyBirdStatus,
  enrollEarlyBird,
  // Payment methods
  getPaymentMethods,
  getPaymentMethodsFull,
  addPaymentMethod,
  setDefaultPaymentMethod,
  removePaymentMethod,
  // Tokenization
  initiateTokenization,
  completeTokenization,
  getProviderConfig,
  getTokenizationSession,
  detectCardBrand,
  // Available gateways
  availableGateways,
  // Utilities
  formatCurrency,
  getStatusColor,
  formatDate,
  formatDateTime,
};
