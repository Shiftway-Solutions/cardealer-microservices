/**
 * Admin Layout
 *
 * Layout for admin panel with sidebar navigation.
 * Protected by AdminAuthGuard - only accessible by admin users.
 *
 * Security:
 * - Requires authentication
 * - Requires accountType === 'admin'
 * - Redirects to login if not authenticated
 * - Shows access denied if not admin
 */

import { Metadata } from 'next';
import { AdminAuthGuard } from '@/components/admin/admin-auth-guard';

export const metadata: Metadata = {
  title: 'Admin Panel',
  description: 'Panel de administración de OKLA',
};

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <AdminAuthGuard>{children}</AdminAuthGuard>;
}
