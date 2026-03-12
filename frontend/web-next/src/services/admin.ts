/**
 * Admin Service - API client for admin operations
 * Connects via API Gateway to AdminService
 */

import { apiClient } from '@/lib/api-client';

// ============================================================
// TYPES
// ============================================================

// Dashboard
export interface DashboardStats {
  totalUsers: number;
  totalVehicles: number;
  activeVehicles: number;
  totalDealers: number;
  activeDealers: number;
  pendingApprovals: number;
  pendingVerifications: number;
  totalReports: number;
  openSupportTickets: number;
  mrr: number;
  mrrChange: number;
  usersChange: number;
  vehiclesChange: number;
  dealersChange: number;
}

export interface RecentActivity {
  id: string;
  action: string;
  subject: string;
  subjectType: 'user' | 'dealer' | 'vehicle' | 'payment' | 'report';
  subjectId: string;
  timestamp: string;
  metadata?: Record<string, unknown>;
}

export interface PendingAction {
  type: 'moderation' | 'verification' | 'report' | 'support';
  title: string;
  count: number;
  priority: 'high' | 'medium' | 'low';
  href: string;
}

// Users
export interface AdminUser {
  id: string;
  name: string;
  email: string;
  phone?: string;
  avatar?: string;
  type: 'buyer' | 'seller' | 'dealer' | 'dealer_employee' | 'admin' | 'platform_employee' | 'guest';
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
  status: 'active' | 'suspended' | 'pending' | 'banned';
  verified: boolean;
  emailVerified: boolean;
  createdAt: string;
  lastActiveAt?: string;
  vehiclesCount: number;
  favoritesCount: number;
  dealsCount: number;
}

export interface AdminUserDetail extends AdminUser {
  address?: string;
  city?: string;
  province?: string;
  country?: string;
  birthDate?: string;
  gender?: string;
  updatedAt?: string;
  twoFactorEnabled: boolean;
  recentActivity: {
    id: string;
    action: string;
    timestamp: string;
    target?: string;
    ipAddress?: string;
  }[];
  vehicles: { id: string; title: string; price: number; status: string; createdAt: string }[];
  reports: { id: string; type: string; status: string; createdAt: string }[];
}

export interface UserFilters {
  search?: string;
  type?: string;
  status?: string;
  verified?: boolean;
  page?: number;
  pageSize?: number;
}

// Vehicles
export interface AdminVehicle {
  id: string;
  title: string;
  slug?: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency: string;
  image: string;
  status: 'active' | 'pending' | 'rejected' | 'sold' | 'paused';
  sellerId: string;
  sellerName: string;
  sellerType: 'seller' | 'dealer';
  views: number;
  leads: number;
  featured: boolean;
  reportsCount: number;
  createdAt: string;
  publishedAt?: string;
  rejectionReason?: string;
}

export interface VehicleFilters {
  search?: string;
  status?: string;
  sellerType?: string;
  featured?: boolean;
  hasReports?: boolean;
  page?: number;
  pageSize?: number;
}

// Dealers
export interface AdminDealer {
  id: string;
  name: string;
  email: string;
  phone: string;
  status: 'active' | 'pending' | 'suspended' | 'rejected';
  verified: boolean;
  plan: 'libre' | 'visible' | 'pro' | 'elite' | 'none';
  vehiclesCount: number;
  salesCount: number;
  rating: number;
  reviewsCount: number;
  location: string;
  createdAt: string;
  mrr: number;
  documentsCount: number;
  pendingDocuments: number;
}

export interface DealerFilters {
  search?: string;
  status?: string;
  plan?: string;
  verified?: boolean;
  registeredFrom?: string;
  registeredTo?: string;
  minListings?: number;
  page?: number;
  pageSize?: number;
}

// Reports
export interface AdminReport {
  id: string;
  type: 'vehicle' | 'user' | 'message' | 'dealer';
  targetId: string;
  targetTitle: string;
  reason: string;
  description: string;
  reportedById: string;
  reportedByEmail: string;
  status: 'pending' | 'investigating' | 'resolved' | 'dismissed';
  priority: 'high' | 'medium' | 'low';
  createdAt: string;
  resolvedAt?: string;
  resolvedById?: string;
  resolution?: string;
  reportCount: number;
}

export interface ReportFilters {
  type?: string;
  status?: string;
  priority?: string;
  page?: number;
  pageSize?: number;
}

// Paginated Response
export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============================================================
// DASHBOARD
// ============================================================

export async function getDashboardStats(): Promise<DashboardStats> {
  const response = await apiClient.get<DashboardStats>('/api/admin/dashboard/stats');
  return response.data;
}

export async function getRecentActivity(limit: number = 10): Promise<RecentActivity[]> {
  const response = await apiClient.get<RecentActivity[]>('/api/admin/dashboard/activity', {
    params: { limit },
  });
  return response.data;
}

export async function getPendingActions(): Promise<PendingAction[]> {
  const response = await apiClient.get<PendingAction[]>('/api/admin/dashboard/pending');
  return response.data;
}

// ============================================================
// USERS
// ============================================================

export async function getUsers(filters: UserFilters = {}): Promise<PaginatedResponse<AdminUser>> {
  const response = await apiClient.get<PaginatedResponse<AdminUser>>('/api/admin/users', {
    params: filters,
  });
  return response.data;
}

export async function getUserById(id: string): Promise<AdminUserDetail> {
  const response = await apiClient.get<AdminUserDetail>(`/api/admin/users/${id}`);
  return response.data;
}

export async function updateUserStatus(
  id: string,
  status: 'active' | 'suspended' | 'banned'
): Promise<void> {
  await apiClient.patch(`/api/admin/users/${id}/status`, { status });
}

export async function verifyUser(id: string): Promise<void> {
  await apiClient.post(`/api/admin/users/${id}/verify`);
}

export async function deleteUser(id: string): Promise<void> {
  await apiClient.delete(`/api/admin/users/${id}`);
}

export async function getUserStats(): Promise<{
  total: number;
  active: number;
  suspended: number;
  pending: number;
  newThisMonth: number;
}> {
  const response = await apiClient.get('/api/admin/users/stats');
  return response.data;
}

// ============================================================
// VEHICLES
// ============================================================

export async function getAdminVehicles(
  filters: VehicleFilters = {}
): Promise<PaginatedResponse<AdminVehicle>> {
  const response = await apiClient.get<PaginatedResponse<AdminVehicle>>('/api/admin/vehicles', {
    params: filters,
  });
  return response.data;
}

export async function getVehicleById(id: string): Promise<AdminVehicle> {
  const response = await apiClient.get<AdminVehicle>(`/api/admin/vehicles/${id}`);
  return response.data;
}

export async function approveVehicle(id: string): Promise<void> {
  // Call VehiclesSaleService directly — PendingReview → Active
  await apiClient.post(`/api/vehicles/${id}/approve`);
}

export async function rejectVehicle(id: string, reason: string): Promise<void> {
  // Call VehiclesSaleService directly — PendingReview → Rejected
  await apiClient.post(`/api/vehicles/${id}/reject`, { reason });
}

export async function toggleFeatured(id: string, featured: boolean): Promise<void> {
  await apiClient.patch(`/api/admin/vehicles/${id}/featured`, { featured });
}

export async function deleteVehicle(id: string): Promise<void> {
  await apiClient.delete(`/api/admin/vehicles/${id}`);
}

export async function getVehicleStats(): Promise<{
  total: number;
  active: number;
  pending: number;
  rejected: number;
  featured: number;
  withReports: number;
}> {
  const response = await apiClient.get('/api/admin/vehicles/stats');
  return response.data;
}

// ============================================================
// DEALERS
// ============================================================

export async function getAdminDealers(
  filters: DealerFilters = {}
): Promise<PaginatedResponse<AdminDealer>> {
  const response = await apiClient.get<PaginatedResponse<AdminDealer>>('/api/admin/dealers', {
    params: filters,
  });
  return response.data;
}

export async function getDealerById(id: string): Promise<AdminDealer> {
  const response = await apiClient.get<AdminDealer>(`/api/admin/dealers/${id}`);
  return response.data;
}

export async function verifyDealer(id: string): Promise<void> {
  await apiClient.post(`/api/admin/dealers/${id}/verify`);
}

export async function suspendDealer(id: string, reason: string): Promise<void> {
  await apiClient.post(`/api/admin/dealers/${id}/suspend`, { reason });
}

export async function reactivateDealer(id: string): Promise<void> {
  await apiClient.post(`/api/admin/dealers/${id}/reactivate`);
}

export async function deleteDealer(id: string): Promise<void> {
  await apiClient.delete(`/api/admin/dealers/${id}`);
}

export async function getDealerStats(): Promise<{
  total: number;
  active: number;
  pending: number;
  suspended: number;
  totalMrr: number;
  byPlan: { libre: number; visible: number; pro: number; elite: number };
}> {
  const response = await apiClient.get('/api/admin/dealers/stats');
  return response.data;
}

export async function adminChangeDealerPlan(
  dealerId: string,
  plan: string,
  justification: string
): Promise<void> {
  await apiClient.post(`/api/admin/dealers/${dealerId}/change-plan`, {
    plan,
    justification,
  });
}

// ============================================================
// REPORTS
// ============================================================

export async function getReports(
  filters: ReportFilters = {}
): Promise<PaginatedResponse<AdminReport>> {
  const response = await apiClient.get<PaginatedResponse<AdminReport>>('/api/admin/reports', {
    params: filters,
  });
  return response.data;
}

export async function getReportById(id: string): Promise<AdminReport> {
  const response = await apiClient.get<AdminReport>(`/api/admin/reports/${id}`);
  return response.data;
}

export async function updateReportStatus(
  id: string,
  status: 'investigating' | 'resolved' | 'dismissed',
  resolution?: string
): Promise<void> {
  await apiClient.patch(`/api/admin/reports/${id}/status`, { status, resolution });
}

export async function getReportStats(): Promise<{
  total: number;
  pending: number;
  investigating: number;
  resolved: number;
  dismissed: number;
  highPriority: number;
}> {
  const response = await apiClient.get('/api/admin/reports/stats');
  return response.data;
}

// ============================================================
// MODERATION
// ============================================================

export interface ModerationItem {
  id: string;
  type: 'vehicle' | 'dealer' | 'user' | 'content';
  targetId: string;
  title: string;
  description: string;
  price?: number;
  images: string[];
  vin?: string;
  oklaScore?: number;
  aiScore?: number;
  aiPhotoScores?: { url: string; score: number; rejectionReason?: string }[];
  aiDecision?: 'approve' | 'reject' | 'review';
  sellerName: string;
  sellerType: 'seller' | 'dealer';
  sellerId: string;
  submittedAt: string;
  flagReason?: string;
  priority: 'normal' | 'high' | 'urgent';
  status: 'pending' | 'approved' | 'rejected' | 'escalated';
  reviewedAt?: string;
  reviewedBy?: string;
  rejectionReason?: string;
  notes?: string;
}

export interface ModerationFilters {
  type?: string;
  priority?: string;
  status?: string;
  search?: string;
  minOklaScore?: number;
  maxOklaScore?: number;
  page?: number;
  pageSize?: number;
}

export interface ModerationStats {
  inQueue: number;
  highPriority: number;
  reviewedToday: number;
  rejectedToday: number;
  approvedToday: number;
  escalatedCount: number;
  avgReviewTimeMinutes: number;
}

export interface ModerationActionRequest {
  action: 'approve' | 'reject' | 'escalate' | 'skip';
  reason?: string;
  notes?: string;
  overrideJustification?: string;
}

export interface ModerationActionResponse {
  success: boolean;
  message: string;
  itemId: string;
  newStatus: string;
}

export async function getModerationQueue(
  filters: ModerationFilters = {}
): Promise<PaginatedResponse<ModerationItem>> {
  const response = await apiClient.get<PaginatedResponse<ModerationItem>>(
    '/api/admin/moderation/queue',
    { params: filters }
  );
  return response.data;
}

export async function getModerationStats(): Promise<ModerationStats> {
  const response = await apiClient.get<ModerationStats>('/api/admin/moderation/stats');
  return response.data;
}

export async function getModerationItem(id: string): Promise<ModerationItem> {
  const response = await apiClient.get<ModerationItem>(`/api/admin/moderation/${id}`);
  return response.data;
}

export async function processModerationAction(
  id: string,
  action: ModerationActionRequest
): Promise<ModerationActionResponse> {
  const response = await apiClient.post<ModerationActionResponse>(
    `/api/admin/moderation/${id}/action`,
    action
  );
  return response.data;
}

export async function approveModerationItem(id: string): Promise<ModerationActionResponse> {
  const response = await apiClient.post<ModerationActionResponse>(
    `/api/admin/moderation/${id}/approve`
  );
  return response.data;
}

export async function rejectModerationItem(
  id: string,
  reason: string
): Promise<ModerationActionResponse> {
  const response = await apiClient.post<ModerationActionResponse>(
    `/api/admin/moderation/${id}/reject`,
    { reason }
  );
  return response.data;
}

// ============================================================
// FINANCIAL DASHBOARD
// ============================================================

export interface CancelledDealer {
  id: string;
  name: string;
  previousPlan: string;
  cancelledAt: string;
  reason?: string;
  mrrLost: number;
}

export interface TopChatAgentDealer {
  dealerId: string;
  dealerName: string;
  plan: string;
  conversationCount: number;
  avgPerDay: number;
}

export async function getCancelledDealers(month?: string): Promise<CancelledDealer[]> {
  const response = await apiClient.get<CancelledDealer[]>('/api/admin/dealers/cancelled', {
    params: { month },
  });
  return response.data;
}

export async function getTopChatAgentDealers(limit: number = 10): Promise<TopChatAgentDealer[]> {
  const response = await apiClient.get<TopChatAgentDealer[]>(
    '/api/admin/dashboard/top-chatagent-dealers',
    { params: { limit } }
  );
  return response.data;
}

export async function exportDashboardExcel(): Promise<Blob> {
  const response = await apiClient.get('/api/admin/dashboard/export', {
    params: { format: 'xlsx' },
    responseType: 'blob',
  });
  return response.data;
}

// ============================================================
// EXPORT
// ============================================================

export const adminService = {
  // Dashboard
  getDashboardStats,
  getRecentActivity,
  getPendingActions,
  // Users
  getUsers,
  getUserById,
  updateUserStatus,
  verifyUser,
  deleteUser,
  getUserStats,
  // Vehicles
  getAdminVehicles,
  getVehicleById,
  approveVehicle,
  rejectVehicle,
  toggleFeatured,
  deleteVehicle,
  getVehicleStats,
  // Dealers
  getAdminDealers,
  getDealerById,
  verifyDealer,
  suspendDealer,
  reactivateDealer,
  deleteDealer,
  getDealerStats,
  adminChangeDealerPlan,
  // Reports
  getReports,
  getReportById,
  updateReportStatus,
  getReportStats,
  // Moderation
  getModerationQueue,
  getModerationStats,
  getModerationItem,
  processModerationAction,
  approveModerationItem,
  rejectModerationItem,
  // Financial Dashboard
  getCancelledDealers,
  getTopChatAgentDealers,
  exportDashboardExcel,
};

export default adminService;
