/**
 * Dealer Billing Service
 *
 * Frontend service for dealer billing operations
 */

import { apiClient } from '@/lib/api-client';
import { formatCurrency } from '@/lib/utils';
export { formatCurrency };

// ============================================================================
// Types
// ============================================================================

export type InvoiceStatus = 'Pending' | 'Paid' | 'Overdue' | 'Cancelled';
export type PaymentMethod = 'CreditCard' | 'BankTransfer' | 'Azul' | 'Stripe';

export interface Invoice {
  id: string;
  dealerId: string;
  invoiceNumber: string;
  description: string;
  amount: number;
  currency: 'DOP' | 'USD';
  status: InvoiceStatus;
  dueDate: string;
  paidAt?: string;
  createdAt: string;
  pdfUrl?: string;
}

export interface PaymentMethodInfo {
  id: string;
  type: PaymentMethod;
  brand?: string;
  last4: string;
  expiryMonth?: number;
  expiryYear?: number;
  isDefault: boolean;
}

export interface Subscription {
  id: string;
  dealerId: string;
  planId: string;
  planName: string;
  status: 'Active' | 'Cancelled' | 'PastDue' | 'Trialing';
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelAtPeriodEnd: boolean;
  monthlyPrice: number;
  currency: 'DOP' | 'USD';
}

export interface UsageMetrics {
  activeListings: number;
  maxListings: number;
  featuredListings: number;
  maxFeatured: number;
  teamMembers: number;
  maxTeamMembers: number;
  storageUsedMb: number;
  maxStorageMb: number;
}

export interface BillingStats {
  totalPaid: number;
  monthlyAverage: number;
  pendingAmount: number;
  nextBillingDate: string;
  invoiceCount: number;
}

export interface PlanConfig {
  id: string;
  name: string;
  price: number;
  currency: 'DOP' | 'USD';
  interval: 'monthly' | 'yearly';
  features: string[];
  maxListings: number;
  maxFeatured: number;
  maxTeamMembers: number;
  maxStorageMb: number;
  isPopular?: boolean;
}

export interface BillingDashboard {
  subscription: Subscription | null;
  invoices: Invoice[];
  paymentMethods: PaymentMethodInfo[];
  usage: UsageMetrics;
  stats: BillingStats;
  plans: PlanConfig[];
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get complete billing dashboard
 */
export async function getBillingDashboard(dealerId: string): Promise<BillingDashboard> {
  const response = await apiClient.get<BillingDashboard>(
    `/api/dealer-billing/dashboard/${dealerId}`
  );
  return response.data;
}

/**
 * Get current subscription
 */
export async function getSubscription(dealerId: string): Promise<Subscription | null> {
  try {
    const response = await apiClient.get<Subscription>(`/api/dealer-billing/subscription`, {
      headers: { 'X-Dealer-Id': dealerId },
    });
    return response.data;
  } catch {
    return null;
  }
}

/**
 * Get all invoices
 */
export async function getInvoices(dealerId: string): Promise<Invoice[]> {
  const response = await apiClient.get<Invoice[]>(`/api/dealer-billing/invoices`, {
    headers: { 'X-Dealer-Id': dealerId },
  });
  return response.data;
}

/**
 * Get invoice by ID
 */
export async function getInvoiceById(invoiceId: string): Promise<Invoice> {
  const response = await apiClient.get<Invoice>(`/api/dealer-billing/invoices/${invoiceId}`);
  return response.data;
}

/**
 * Download invoice PDF
 */
export async function downloadInvoicePdf(invoiceId: string): Promise<Blob> {
  const response = await apiClient.get(`/api/dealer-billing/invoices/${invoiceId}/pdf`, {
    responseType: 'blob',
  });
  return response.data;
}

/**
 * Get payment methods
 */
export async function getPaymentMethods(dealerId: string): Promise<PaymentMethodInfo[]> {
  const response = await apiClient.get<PaymentMethodInfo[]>(`/api/dealer-billing/payment-methods`, {
    headers: { 'X-Dealer-Id': dealerId },
  });
  return response.data;
}

/**
 * Add payment method
 */
export async function addPaymentMethod(
  dealerId: string,
  token: string
): Promise<PaymentMethodInfo> {
  const response = await apiClient.post<PaymentMethodInfo>(
    `/api/dealer-billing/payment-methods`,
    { token },
    { headers: { 'X-Dealer-Id': dealerId } }
  );
  return response.data;
}

/**
 * Remove payment method
 */
export async function removePaymentMethod(
  dealerId: string,
  paymentMethodId: string
): Promise<void> {
  await apiClient.delete(`/api/dealer-billing/payment-methods/${paymentMethodId}`, {
    headers: { 'X-Dealer-Id': dealerId },
  });
}

/**
 * Set default payment method
 */
export async function setDefaultPaymentMethod(
  dealerId: string,
  paymentMethodId: string
): Promise<void> {
  await apiClient.put(
    `/api/dealer-billing/payment-methods/${paymentMethodId}/default`,
    {},
    { headers: { 'X-Dealer-Id': dealerId } }
  );
}

/**
 * Get usage metrics
 */
export async function getUsageMetrics(dealerId: string): Promise<UsageMetrics> {
  const response = await apiClient.get<UsageMetrics>(`/api/dealer-billing/usage`, {
    headers: { 'X-Dealer-Id': dealerId },
  });
  return response.data;
}

/**
 * Get billing stats
 */
export async function getBillingStats(dealerId: string): Promise<BillingStats> {
  const response = await apiClient.get<BillingStats>(`/api/dealer-billing/stats`, {
    headers: { 'X-Dealer-Id': dealerId },
  });
  return response.data;
}

/**
 * Get available plans
 */
export async function getPlans(): Promise<PlanConfig[]> {
  const response = await apiClient.get<PlanConfig[]>(`/api/dealer-billing/plans`);
  return response.data;
}

/**
 * Change subscription plan
 */
export async function changePlan(dealerId: string, planId: string): Promise<Subscription> {
  const response = await apiClient.post<Subscription>(
    `/api/dealer-billing/subscription/change`,
    { planId },
    { headers: { 'X-Dealer-Id': dealerId } }
  );
  return response.data;
}

/**
 * Cancel subscription
 */
export async function cancelSubscription(
  dealerId: string,
  immediately: boolean = false
): Promise<Subscription> {
  const response = await apiClient.post<Subscription>(
    `/api/dealer-billing/subscription/cancel`,
    { immediately },
    { headers: { 'X-Dealer-Id': dealerId } }
  );
  return response.data;
}

/**
 * Reactivate subscription
 */
export async function reactivateSubscription(dealerId: string): Promise<Subscription> {
  const response = await apiClient.post<Subscription>(
    `/api/dealer-billing/subscription/reactivate`,
    {},
    { headers: { 'X-Dealer-Id': dealerId } }
  );
  return response.data;
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Get status label in Spanish
 */
export function getInvoiceStatusLabel(status: InvoiceStatus): string {
  const labels: Record<InvoiceStatus, string> = {
    Pending: 'Pendiente',
    Paid: 'Pagada',
    Overdue: 'Vencida',
    Cancelled: 'Cancelada',
  };
  return labels[status] || status;
}

/**
 * Get status color for badges
 */
export function getInvoiceStatusColor(
  status: InvoiceStatus
): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case 'Paid':
      return 'default';
    case 'Pending':
      return 'secondary';
    case 'Overdue':
      return 'destructive';
    case 'Cancelled':
      return 'outline';
    default:
      return 'default';
  }
}

/**
 * Get payment method brand label
 */
export function getPaymentMethodBrand(brand?: string): string {
  if (brand) return brand;
  return 'Tarjeta';
}

/**
 * Format date for display
 */
export function formatBillingDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString('es-DO', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Calculate usage percentage
 */
export function calculateUsagePercentage(used: number, max: number): number {
  if (max === 0) return 0;
  return Math.round((used / max) * 100);
}

/**
 * Get usage color based on percentage
 */
export function getUsageColor(percentage: number): string {
  if (percentage >= 90) return 'text-red-600';
  if (percentage >= 70) return 'text-amber-600';
  return 'text-emerald-600';
}

// ============================================================================
// Plan Enforcement & Feature Gating (Quick-win integrations)
// ============================================================================

export interface PlanEnforcement {
  canPublish: boolean;
  currentCount: number;
  maxAllowed: number; // -1 = unlimited
  remaining: number; // -1 = unlimited
  plan: string;
  message: string;
}

export interface PlanFeatures {
  plan: string;
  maxVehicles: number;
  maxUsers: number;
  maxPhotosPerListing: number;
  featuredListingsPerMonth: number;
  oklaCoinsMonthly: number;
  chatAgentConversations: number;
  videoTourEnabled: boolean;
  verifiedBadge: boolean;
  whatsAppIntegration: boolean;
}

/**
 * Check if a dealer can publish more vehicles.
 * Called before showing the "create listing" form.
 * All OKLA plans have unlimited listings, but this enforces the check.
 */
export async function canPublish(
  dealerId: string,
  currentVehicleCount: number
): Promise<PlanEnforcement> {
  const response = await apiClient.get<PlanEnforcement>(
    `/api/subscriptions/dealer/${dealerId}/can-publish?currentVehicleCount=${currentVehicleCount}`
  );
  return response.data;
}

/**
 * Get plan features for feature gating (photos, featured listings, etc.)
 * Used to show/hide premium features in the UI.
 */
export async function getPlanFeatures(dealerId: string): Promise<PlanFeatures> {
  const response = await apiClient.get<PlanFeatures>(
    `/api/subscriptions/dealer/${dealerId}/plan-features`
  );
  return response.data;
}

/**
 * Upgrade dealer's subscription plan
 */
export async function upgradePlan(
  subscriptionId: string,
  newPlan: string
): Promise<{ success: boolean; message: string }> {
  const response = await apiClient.put<{ success: boolean; message: string }>(
    `/api/subscriptions/${subscriptionId}/upgrade`,
    { newPlan }
  );
  return response.data;
}

/**
 * Switch billing cycle (monthly ↔ annual)
 */
export async function switchBillingCycle(
  subscriptionId: string,
  newCycle: 'monthly' | 'annual'
): Promise<{ success: boolean; message: string }> {
  const response = await apiClient.put<{ success: boolean; message: string }>(
    `/api/subscriptions/${subscriptionId}/billing-cycle`,
    { billingCycle: newCycle }
  );
  return response.data;
}
