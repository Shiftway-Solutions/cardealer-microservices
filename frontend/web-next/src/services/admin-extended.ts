/**
 * Admin Extended Service - API client for additional admin operations
 * Covers: Analytics, Compliance, Content, Billing, System, Support, Dealer Detail
 * Connects via API Gateway to AdminService and other backend services
 */

import { apiClient } from '@/lib/api-client';

// ============================================================
// ANALYTICS TYPES
// ============================================================

export interface AnalyticsOverviewStat {
  label: string;
  value: string;
  change: string;
  trend: 'up' | 'down';
  metric: 'visits' | 'users' | 'vehicles' | 'mrr';
}

export interface WeeklyDataPoint {
  day: string;
  visits: number;
  signups: number;
  listings: number;
}

export interface TopVehicleSearch {
  make: string;
  model: string;
  searches: number;
  views: number;
  leads: number;
}

export interface TrafficSource {
  source: string;
  percentage: number;
  visits: string;
}

export interface DeviceBreakdown {
  device: string;
  percentage: number;
}

export interface ConversionRates {
  visitToSignup: number;
  signupToListing: number;
  viewToLead: number;
}

export interface RevenueByChannel {
  channel: string;
  amount: number;
  color: string;
}

export interface PlatformAnalytics {
  overview: AnalyticsOverviewStat[];
  weeklyData: WeeklyDataPoint[];
  topVehicles: TopVehicleSearch[];
  trafficSources: TrafficSource[];
  deviceBreakdown: DeviceBreakdown[];
  conversions: ConversionRates;
  revenueByChannel: RevenueByChannel[];
}

// ============================================================
// COMPLIANCE TYPES
// ============================================================

export interface AmlAlert {
  id: string;
  type: 'high_value_transaction' | 'suspicious_pattern' | 'watchlist_match';
  description: string;
  amount?: string;
  entity?: string;
  pattern?: string;
  transactionCount?: number;
  matchScore?: string;
  matchedName?: string;
  status: 'pending_review' | 'under_investigation' | 'resolved' | 'dismissed';
  createdAt: string;
}

export interface DgiiReport {
  id: string;
  reportNumber: string;
  period: string;
  status: 'pending' | 'submitted' | 'accepted' | 'rejected';
  dueDate?: string;
  submittedAt?: string;
  transactionCount: number;
}

export interface ComplianceStats {
  amlAlerts: number;
  pendingReports: number;
  complianceRate: string;
  kycVerifications: number;
}

// ============================================================
// CONTENT TYPES
// ============================================================

export interface Banner {
  id: string;
  title: string;
  subtitle?: string | null;
  image: string;
  mobileImage?: string | null;
  link: string;
  ctaText?: string | null;
  placement: string;
  status: 'active' | 'scheduled' | 'inactive';
  startDate: string;
  endDate: string;
  views: number;
  clicks: number;
  displayOrder: number;
}

export interface StaticPage {
  id: string;
  title: string;
  slug: string;
  status: 'published' | 'draft';
  lastModified: string;
  author: string;
  views: number;
}

export interface BlogPost {
  id: string;
  title: string;
  slug: string;
  status: 'published' | 'draft';
  author: string;
  publishedAt?: string;
  views: number;
  category: string;
}

export interface ContentOverview {
  banners: Banner[];
  pages: StaticPage[];
  blogPosts: BlogPost[];
  totalViews: string;
}

// ============================================================
// BILLING/FACTURACION TYPES
// ============================================================

export interface RevenueStats {
  mrr: number;
  arr: number;
  growth: number;
  activeSubscriptions: number;
  churnRate: number;
  churnChange: string;
  avgRevenuePerUser: number;
}

export interface BillingTransaction {
  id: string;
  dealerName: string;
  plan: string;
  amount: number;
  currency: string;
  status: 'completed' | 'pending' | 'failed' | 'refunded';
  date: string;
  method: string;
}

export interface PendingPayment {
  id: string;
  dealerName: string;
  amount: number;
  currency: string;
  dueDate: string;
  daysOverdue: number;
}

export interface PlanRevenue {
  plan: string;
  revenue: number;
  count: number;
  percentage: number;
}

// ============================================================
// SYSTEM/SISTEMA TYPES
// ============================================================

export interface ServiceHealth {
  name: string;
  status: 'healthy' | 'warning' | 'error' | 'unknown';
  latency: string;
  uptime: string;
}

export interface DatabaseHealth {
  name: string;
  status: 'healthy' | 'warning' | 'error';
  connections: string;
  responseTime: string;
}

export interface InfraHealth {
  name: string;
  status: 'healthy' | 'warning' | 'error';
  description: string;
  region: string;
}

export interface SystemMetrics {
  cpu: string;
  memory: string;
  storage: string;
  bandwidth: string;
}

// ============================================================
// SUPPORT/SOPORTE TYPES
// ============================================================

export interface SupportTicket {
  id: string;
  ticketNumber: string;
  subject: string;
  userName: string;
  userEmail: string;
  category: 'technical' | 'billing' | 'account' | 'sales' | 'general';
  status: 'open' | 'in_progress' | 'resolved' | 'closed';
  priority: 'high' | 'normal' | 'low' | 'urgent';
  createdAt: string;
  updatedAt: string;
  messagesCount: number;
}

export interface TicketMessage {
  id: string;
  sender: string;
  senderType: 'user' | 'agent' | 'system';
  message: string;
  timestamp: string;
  attachments?: TicketAttachment[];
}

export interface TicketAttachment {
  name: string;
  size: string;
  type: string;
}

export interface SupportTicketDetail extends SupportTicket {
  description: string;
  customer: {
    name: string;
    email: string;
    phone?: string;
    accountType: string;
    memberSince: string;
  };
  assignedTo: string | null;
  messages: TicketMessage[];
  relatedTickets: { id: string; subject: string; status: string }[];
  tags: string[];
}

export interface TicketFilters {
  search?: string;
  status?: string;
  priority?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}

export interface TicketStats {
  total: number;
  open: number;
  inProgress: number;
  resolved: number;
  avgResponseTime: string;
}

// ============================================================
// ADMIN MESSAGES TYPES
// ============================================================

export interface AdminMessage {
  id: string;
  senderName: string;
  senderEmail: string;
  senderType: 'dealer' | 'seller' | 'system';
  subject: string;
  preview: string;
  status: 'new' | 'read' | 'resolved';
  priority: 'high' | 'medium' | 'low';
  category: string;
  createdAt: string;
  messagesCount: number;
  hasAttachments: boolean;
}

// ============================================================
// ADMIN REVIEWS TYPES
// ============================================================

export interface AdminReview {
  id: string;
  authorName: string;
  authorAvatar?: string | null;
  targetName: string;
  targetType: 'dealer' | 'seller';
  rating: number;
  title: string;
  comment: string;
  createdAt: string;
  status: 'pending' | 'approved' | 'rejected';
  reports: string[];
}

export interface ReportedReview extends AdminReview {
  reportCount: number;
  reportReasons: string[];
  lastReportedAt: string;
}

export interface AdminReviewStats {
  totalReviews: number;
  pendingReviews: number;
  approvedReviews: number;
  averageRating: number;
  reportedReviews: number;
}

// ============================================================
// DEALER DETAIL (ADMIN) TYPES
// ============================================================

export interface AdminDealerDetail {
  id: string;
  name: string;
  legalName: string;
  rnc: string;
  email: string;
  phone: string;
  avatar: string | null;
  status: 'active' | 'pending' | 'suspended' | 'rejected';
  verified: boolean;
  plan: string;
  createdAt: string;
  address: string;
  contactPerson: string;
  paymentMethod?: string;
  stats: {
    totalVehicles: number;
    activeListings: number;
    totalSales: number;
    rating: number;
    reviewCount: number;
    monthlyRevenue: number;
  };
}

export interface DealerVehicle {
  id: string;
  title: string;
  price: number;
  status: 'active' | 'pending' | 'sold' | 'paused';
  views: number;
  leads: number;
  createdAt: string;
}

export interface DealerDocument {
  name: string;
  status: 'verified' | 'pending' | 'expired' | 'rejected';
  uploadedAt: string;
}

export interface DealerBillingRecord {
  id: string;
  description: string;
  amount: number;
  currency: string;
  status: 'paid' | 'pending' | 'overdue';
  date: string;
}

// ============================================================
// ANALYTICS API
// ============================================================

export async function getPlatformAnalytics(period: string = '7d'): Promise<PlatformAnalytics> {
  const response = await apiClient.get<PlatformAnalytics>('/api/admin/analytics', {
    params: { period },
  });
  return response.data;
}

export async function getAnalyticsOverview(
  period: string = '7d'
): Promise<AnalyticsOverviewStat[]> {
  const response = await apiClient.get<AnalyticsOverviewStat[]>('/api/admin/analytics/overview', {
    params: { period },
  });
  return response.data;
}

export async function getWeeklyData(period: string = '7d'): Promise<WeeklyDataPoint[]> {
  const response = await apiClient.get<WeeklyDataPoint[]>('/api/admin/analytics/weekly', {
    params: { period },
  });
  return response.data;
}

export async function getTopVehicleSearches(limit: number = 5): Promise<TopVehicleSearch[]> {
  const response = await apiClient.get<TopVehicleSearch[]>('/api/admin/analytics/top-vehicles', {
    params: { limit },
  });
  return response.data;
}

export async function getTrafficSources(period: string = '7d'): Promise<TrafficSource[]> {
  const response = await apiClient.get<TrafficSource[]>('/api/admin/analytics/traffic-sources', {
    params: { period },
  });
  return response.data;
}

export async function getDeviceBreakdown(period: string = '7d'): Promise<DeviceBreakdown[]> {
  const response = await apiClient.get<DeviceBreakdown[]>('/api/admin/analytics/devices', {
    params: { period },
  });
  return response.data;
}

export async function getConversionRates(period: string = '7d'): Promise<ConversionRates> {
  const response = await apiClient.get<ConversionRates>('/api/admin/analytics/conversions', {
    params: { period },
  });
  return response.data;
}

export async function getRevenueByChannel(period: string = '7d'): Promise<RevenueByChannel[]> {
  const response = await apiClient.get<RevenueByChannel[]>(
    '/api/admin/analytics/revenue-channels',
    { params: { period } }
  );
  return response.data;
}

export async function exportAnalyticsReport(
  period: string,
  format: 'pdf' | 'csv' = 'pdf'
): Promise<Blob> {
  const response = await apiClient.get('/api/admin/analytics/export', {
    params: { period, format },
    responseType: 'blob',
  });
  return response.data;
}

// ============================================================
// COMPLIANCE API
// ============================================================

export async function getAmlAlerts(): Promise<AmlAlert[]> {
  const response = await apiClient.get<AmlAlert[]>('/api/admin/compliance/aml-alerts');
  return response.data;
}

export async function getDgiiReports(): Promise<DgiiReport[]> {
  const response = await apiClient.get<DgiiReport[]>('/api/admin/compliance/dgii-reports');
  return response.data;
}

export async function getComplianceStats(): Promise<ComplianceStats> {
  const response = await apiClient.get<ComplianceStats>('/api/admin/compliance/stats');
  return response.data;
}

export async function updateAmlAlertStatus(
  id: string,
  status: 'under_investigation' | 'resolved' | 'dismissed'
): Promise<void> {
  await apiClient.patch(`/api/admin/compliance/aml-alerts/${id}/status`, { status });
}

export async function submitDgiiReport(id: string): Promise<void> {
  await apiClient.post(`/api/admin/compliance/dgii-reports/${id}/submit`);
}

// ============================================================
// CONTENT API
// ============================================================

export async function getContentOverview(): Promise<ContentOverview> {
  const response = await apiClient.get<ContentOverview>('/api/admin/content');
  return response.data;
}

export async function getBanners(): Promise<Banner[]> {
  const response = await apiClient.get<Banner[]>('/api/admin/content/banners');
  return response.data;
}

export async function createBanner(data: Partial<Banner>): Promise<Banner> {
  const response = await apiClient.post<Banner>('/api/admin/content/banners', data);
  return response.data;
}

export async function updateBanner(id: string, data: Partial<Banner>): Promise<Banner> {
  const response = await apiClient.put<Banner>(`/api/admin/content/banners/${id}`, data);
  return response.data;
}

export async function deleteBanner(id: string): Promise<void> {
  await apiClient.delete(`/api/admin/content/banners/${id}`);
}

export async function getStaticPages(): Promise<StaticPage[]> {
  const response = await apiClient.get<StaticPage[]>('/api/admin/content/pages');
  return response.data;
}

export async function getBlogPosts(): Promise<BlogPost[]> {
  const response = await apiClient.get<BlogPost[]>('/api/admin/content/blog');
  return response.data;
}

// ============================================================
// BILLING/FACTURACION API
// ============================================================

export async function getRevenueStats(): Promise<RevenueStats> {
  const response = await apiClient.get<RevenueStats>('/api/admin/billing/revenue');
  return response.data;
}

export async function getRecentTransactions(limit: number = 10): Promise<BillingTransaction[]> {
  const response = await apiClient.get<BillingTransaction[]>('/api/admin/billing/transactions', {
    params: { limit },
  });
  return response.data;
}

export async function getPendingPayments(): Promise<PendingPayment[]> {
  const response = await apiClient.get<PendingPayment[]>('/api/admin/billing/pending');
  return response.data;
}

export async function getRevenueByPlan(): Promise<PlanRevenue[]> {
  const response = await apiClient.get('/api/admin/billing/revenue-by-plan');
  const raw = response.data;
  // API returns { byPlan: [...] } wrapper — extract and map to PlanRevenue[]
  const plans = Array.isArray(raw) ? raw : Array.isArray(raw?.byPlan) ? raw.byPlan : [];
  return plans.map((p: Record<string, unknown>) => ({
    plan: (p.planName ?? p.planKey ?? p.plan ?? 'N/A') as string,
    revenue: (p.totalMrr ?? p.revenue ?? 0) as number,
    count: (p.dealerCount ?? p.count ?? 0) as number,
    percentage: (p.percentOfMrr ?? p.percentage ?? 0) as number,
  }));
}

export async function exportBillingReport(format: 'pdf' | 'csv' = 'pdf'): Promise<Blob> {
  const response = await apiClient.get('/api/admin/billing/export', {
    params: { format },
    responseType: 'blob',
  });
  return response.data;
}

// ============================================================
// SYSTEM/SISTEMA API
// ============================================================

export async function getServicesHealth(): Promise<ServiceHealth[]> {
  const response = await apiClient.get<ServiceHealth[]>('/api/admin/system/services');
  return response.data;
}

export async function getDatabasesHealth(): Promise<DatabaseHealth[]> {
  const response = await apiClient.get<DatabaseHealth[]>('/api/admin/system/databases');
  return response.data;
}

export async function getInfrastructureHealth(): Promise<InfraHealth[]> {
  const response = await apiClient.get<InfraHealth[]>('/api/admin/system/infrastructure');
  return response.data;
}

export async function getSystemMetrics(): Promise<SystemMetrics> {
  const response = await apiClient.get<SystemMetrics>('/api/admin/system/metrics');
  return response.data;
}

export async function restartService(serviceName: string): Promise<void> {
  await apiClient.post(`/api/admin/system/services/${serviceName}/restart`);
}

// ============================================================
// SUPPORT/SOPORTE API
// ============================================================

export async function getSupportTickets(
  filters: TicketFilters = {}
): Promise<{ items: SupportTicket[]; total: number }> {
  const response = await apiClient.get<{ items: SupportTicket[]; total: number }>(
    '/api/admin/support/tickets',
    { params: filters }
  );
  return response.data;
}

export async function getSupportTicket(id: string): Promise<SupportTicketDetail> {
  const response = await apiClient.get<SupportTicketDetail>(`/api/admin/support/tickets/${id}`);
  return response.data;
}

export async function replySupportTicket(id: string, message: string): Promise<TicketMessage> {
  const response = await apiClient.post<TicketMessage>(`/api/admin/support/tickets/${id}/reply`, {
    message,
  });
  return response.data;
}

export async function updateTicketStatus(
  id: string,
  status: 'in_progress' | 'resolved' | 'closed'
): Promise<void> {
  await apiClient.patch(`/api/admin/support/tickets/${id}/status`, { status });
}

export async function assignTicket(id: string, agentId: string): Promise<void> {
  await apiClient.patch(`/api/admin/support/tickets/${id}/assign`, { agentId });
}

export async function getTicketStats(): Promise<TicketStats> {
  const response = await apiClient.get<TicketStats>('/api/admin/support/stats');
  return response.data;
}

// ============================================================
// ADMIN MESSAGES API
// ============================================================

export async function getAdminMessages(
  filters: { search?: string; status?: string; priority?: string } = {}
): Promise<{ items: AdminMessage[]; total: number }> {
  const response = await apiClient.get<{ items: AdminMessage[]; total: number }>(
    '/api/admin/messages',
    { params: filters }
  );
  return response.data;
}

export async function markMessageRead(id: string): Promise<void> {
  await apiClient.patch(`/api/admin/messages/${id}/read`);
}

export async function replyToMessage(id: string, message: string): Promise<void> {
  await apiClient.post(`/api/admin/messages/${id}/reply`, { message });
}

// ============================================================
// ADMIN REVIEWS API
// ============================================================

export async function getAdminReviews(
  filters: { status?: string; search?: string; page?: number; pageSize?: number } = {}
): Promise<{ items: AdminReview[]; total: number }> {
  const response = await apiClient.get<{ items: AdminReview[]; total: number }>(
    '/api/admin/reviews',
    { params: filters }
  );
  return response.data;
}

export async function getReportedReviews(): Promise<ReportedReview[]> {
  const response = await apiClient.get<ReportedReview[]>('/api/admin/reviews/reported');
  return response.data;
}

export async function approveReview(id: string): Promise<void> {
  await apiClient.post(`/api/admin/reviews/${id}/approve`);
}

export async function rejectReview(id: string, reason?: string): Promise<void> {
  await apiClient.post(`/api/admin/reviews/${id}/reject`, { reason });
}

export async function deleteReview(id: string): Promise<void> {
  await apiClient.delete(`/api/admin/reviews/${id}`);
}

export async function getAdminReviewStats(): Promise<AdminReviewStats> {
  const response = await apiClient.get<AdminReviewStats>('/api/admin/reviews/stats');
  return response.data;
}

// ============================================================
// DEALER DETAIL (ADMIN) API
// ============================================================

export async function getAdminDealerDetail(id: string): Promise<AdminDealerDetail> {
  const response = await apiClient.get<
    AdminDealerDetail & {
      rating?: number;
      reviewsCount?: number;
      vehiclesCount?: number;
      salesCount?: number;
      mrr?: number;
      location?: string;
    }
  >(`/api/admin/dealers/${id}`);
  const raw = response.data;
  if (!raw.stats) {
    raw.stats = {
      rating: raw.rating ?? 0,
      reviewCount: raw.reviewsCount ?? 0,
      totalVehicles: raw.vehiclesCount ?? 0,
      activeListings: raw.vehiclesCount ?? 0,
      totalSales: raw.salesCount ?? 0,
      monthlyRevenue: raw.mrr ?? 0,
    };
  }
  if (!raw.address && raw.location) {
    raw.address = raw.location;
  }
  return raw;
}

export async function getDealerVehicles(dealerId: string): Promise<DealerVehicle[]> {
  const response = await apiClient.get<DealerVehicle[]>(`/api/admin/dealers/${dealerId}/vehicles`);
  return response.data;
}

export async function getDealerDocuments(dealerId: string): Promise<DealerDocument[]> {
  const response = await apiClient.get<DealerDocument[]>(
    `/api/admin/dealers/${dealerId}/documents`
  );
  return response.data;
}

export async function getDealerBillingHistory(dealerId: string): Promise<DealerBillingRecord[]> {
  const response = await apiClient.get<DealerBillingRecord[]>(
    `/api/admin/dealers/${dealerId}/billing`
  );
  return response.data;
}

// ============================================================
// EXPORT
// ============================================================

export const adminExtendedService = {
  // Analytics
  getPlatformAnalytics,
  getAnalyticsOverview,
  getWeeklyData,
  getTopVehicleSearches,
  getTrafficSources,
  getDeviceBreakdown,
  getConversionRates,
  getRevenueByChannel,
  exportAnalyticsReport,
  // Compliance
  getAmlAlerts,
  getDgiiReports,
  getComplianceStats,
  updateAmlAlertStatus,
  submitDgiiReport,
  // Content
  getContentOverview,
  getBanners,
  createBanner,
  updateBanner,
  deleteBanner,
  getStaticPages,
  getBlogPosts,
  // Billing
  getRevenueStats,
  getRecentTransactions,
  getPendingPayments,
  getRevenueByPlan,
  exportBillingReport,
  // System
  getServicesHealth,
  getDatabasesHealth,
  getInfrastructureHealth,
  getSystemMetrics,
  restartService,
  // Support
  getSupportTickets,
  getSupportTicket,
  replySupportTicket,
  updateTicketStatus,
  assignTicket,
  getTicketStats,
  // Messages
  getAdminMessages,
  markMessageRead,
  replyToMessage,
  // Reviews
  getAdminReviews,
  getReportedReviews,
  approveReview,
  rejectReview,
  deleteReview,
  getAdminReviewStats,
  // Dealer Detail
  getAdminDealerDetail,
  getDealerVehicles,
  getDealerDocuments,
  getDealerBillingHistory,
};

export default adminExtendedService;
