/**
 * Status Badge Component
 *
 * Unified status and priority badge components for use across
 * admin, cuenta, and dealer dashboards.
 *
 * Eliminates duplication of getStatusBadge/getPriorityBadge functions
 * that were spread across multiple files.
 */

import * as React from 'react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

// =============================================================================
// TYPES
// =============================================================================

export type GeneralStatus =
  | 'active'
  | 'inactive'
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'suspended'
  | 'banned'
  | 'expired'
  | 'draft'
  | 'published';

export type TicketStatus = 'open' | 'pending' | 'in_progress' | 'resolved' | 'closed';

export type VehicleStatus =
  | 'active'
  | 'pending'
  | 'paused'
  | 'sold'
  | 'expired'
  | 'rejected'
  | 'draft';

export type KYCStatus =
  | 'pending'
  | 'in_progress'
  | 'in_review'
  | 'approved'
  | 'rejected'
  | 'expired';

export type Priority = 'high' | 'medium' | 'low' | 'urgent' | 'critical';

export type ContentStatus = 'draft' | 'published' | 'archived' | 'scheduled';

// =============================================================================
// STATUS CONFIGURATIONS
// =============================================================================

interface StatusConfig {
  label: string;
  className: string;
}

const GENERAL_STATUS_CONFIG: Record<GeneralStatus, StatusConfig> = {
  active: { label: 'Activo', className: 'bg-primary/10 text-primary' },
  inactive: { label: 'Inactivo', className: 'bg-gray-100 text-gray-700' },
  pending: { label: 'Pendiente', className: 'bg-amber-100 text-amber-700' },
  approved: { label: 'Aprobado', className: 'bg-primary/10 text-primary' },
  rejected: { label: 'Rechazado', className: 'bg-red-100 text-red-700' },
  suspended: { label: 'Suspendido', className: 'bg-orange-100 text-orange-700' },
  banned: { label: 'Baneado', className: 'bg-red-100 text-red-700' },
  expired: { label: 'Expirado', className: 'bg-gray-100 text-gray-700' },
  draft: { label: 'Borrador', className: 'bg-gray-100 text-gray-700' },
  published: { label: 'Publicado', className: 'bg-primary/10 text-primary' },
};

const TICKET_STATUS_CONFIG: Record<TicketStatus, StatusConfig> = {
  open: { label: 'Abierto', className: 'bg-blue-100 text-blue-700' },
  pending: { label: 'Pendiente', className: 'bg-amber-100 text-amber-700' },
  in_progress: { label: 'En Progreso', className: 'bg-purple-100 text-purple-700' },
  resolved: { label: 'Resuelto', className: 'bg-primary/10 text-primary' },
  closed: { label: 'Cerrado', className: 'bg-gray-100 text-gray-700' },
};

const VEHICLE_STATUS_CONFIG: Record<VehicleStatus, StatusConfig> = {
  active: { label: 'Activo', className: 'bg-primary/10 text-primary' },
  pending: { label: 'Pendiente', className: 'bg-amber-100 text-amber-700' },
  paused: { label: 'Pausado', className: 'bg-gray-100 text-gray-700' },
  sold: { label: 'Vendido', className: 'bg-blue-100 text-blue-700' },
  expired: { label: 'Expirado', className: 'bg-red-100 text-red-700' },
  rejected: { label: 'Rechazado', className: 'bg-red-100 text-red-700' },
  draft: { label: 'Borrador', className: 'bg-gray-100 text-gray-700' },
};

const KYC_STATUS_CONFIG: Record<KYCStatus, StatusConfig> = {
  pending: { label: 'Pendiente', className: 'bg-amber-100 text-amber-700' },
  in_progress: { label: 'En Progreso', className: 'bg-blue-100 text-blue-700' },
  in_review: { label: 'En Revisión', className: 'bg-purple-100 text-purple-700' },
  approved: { label: 'Aprobado', className: 'bg-primary/10 text-primary' },
  rejected: { label: 'Rechazado', className: 'bg-red-100 text-red-700' },
  expired: { label: 'Expirado', className: 'bg-gray-100 text-gray-700' },
};

const PRIORITY_CONFIG: Record<Priority, StatusConfig> = {
  critical: { label: 'Crítica', className: 'bg-red-600 text-white' },
  urgent: { label: 'Urgente', className: 'bg-red-100 text-red-700' },
  high: { label: 'Alta', className: 'bg-red-100 text-red-700' },
  medium: { label: 'Media', className: 'bg-amber-100 text-amber-700' },
  low: { label: 'Baja', className: 'bg-gray-100 text-gray-700' },
};

const CONTENT_STATUS_CONFIG: Record<ContentStatus, StatusConfig> = {
  draft: { label: 'Borrador', className: 'bg-gray-100 text-gray-700' },
  published: { label: 'Publicado', className: 'bg-primary/10 text-primary' },
  archived: { label: 'Archivado', className: 'bg-gray-100 text-gray-700' },
  scheduled: { label: 'Programado', className: 'bg-blue-100 text-blue-700' },
};

// =============================================================================
// COMPONENTS
// =============================================================================

interface StatusBadgeProps {
  status: string;
  className?: string;
  customLabel?: string;
}

/**
 * General purpose status badge
 */
export function StatusBadge({ status, className, customLabel }: StatusBadgeProps) {
  const config = GENERAL_STATUS_CONFIG[status as GeneralStatus];

  if (!config) {
    return (
      <Badge variant="outline" className={className}>
        {customLabel || status}
      </Badge>
    );
  }

  return <Badge className={cn(config.className, className)}>{customLabel || config.label}</Badge>;
}

/**
 * Ticket/Support status badge
 */
export function TicketStatusBadge({ status, className, customLabel }: StatusBadgeProps) {
  const config = TICKET_STATUS_CONFIG[status as TicketStatus];

  if (!config) {
    return (
      <Badge variant="outline" className={className}>
        {customLabel || status}
      </Badge>
    );
  }

  return <Badge className={cn(config.className, className)}>{customLabel || config.label}</Badge>;
}

/**
 * Vehicle listing status badge
 */
export function VehicleStatusBadge({ status, className, customLabel }: StatusBadgeProps) {
  const config = VEHICLE_STATUS_CONFIG[status as VehicleStatus];

  if (!config) {
    return (
      <Badge variant="outline" className={className}>
        {customLabel || status}
      </Badge>
    );
  }

  return <Badge className={cn(config.className, className)}>{customLabel || config.label}</Badge>;
}

/**
 * KYC verification status badge
 */
export function KYCStatusBadge({ status, className, customLabel }: StatusBadgeProps) {
  const config = KYC_STATUS_CONFIG[status as KYCStatus];

  if (!config) {
    return (
      <Badge variant="outline" className={className}>
        {customLabel || status}
      </Badge>
    );
  }

  return <Badge className={cn(config.className, className)}>{customLabel || config.label}</Badge>;
}

/**
 * Priority badge for tickets, tasks, etc.
 */
export function PriorityBadge({ priority, className }: { priority: string; className?: string }) {
  const config = PRIORITY_CONFIG[priority as Priority];

  if (!config) {
    return (
      <Badge variant="outline" className={className}>
        {priority}
      </Badge>
    );
  }

  return <Badge className={cn(config.className, className)}>{config.label}</Badge>;
}

/**
 * Content status badge for banners, pages, blog posts
 */
export function ContentStatusBadge({ status, className, customLabel }: StatusBadgeProps) {
  const config = CONTENT_STATUS_CONFIG[status as ContentStatus];

  if (!config) {
    return (
      <Badge variant="outline" className={className}>
        {customLabel || status}
      </Badge>
    );
  }

  return <Badge className={cn(config.className, className)}>{customLabel || config.label}</Badge>;
}

// =============================================================================
// UTILITY FUNCTIONS (for backwards compatibility)
// =============================================================================

/**
 * Get status badge JSX element - utility for existing code migration
 * @deprecated Use StatusBadge component directly
 */
export function getStatusBadge(status: string): React.ReactElement {
  return <StatusBadge status={status} />;
}

/**
 * Get ticket status badge JSX element - utility for existing code migration
 * @deprecated Use TicketStatusBadge component directly
 */
export function getTicketStatusBadge(status: string): React.ReactElement {
  return <TicketStatusBadge status={status} />;
}

/**
 * Get vehicle status badge JSX element - utility for existing code migration
 * @deprecated Use VehicleStatusBadge component directly
 */
export function getVehicleStatusBadge(status: string): React.ReactElement {
  return <VehicleStatusBadge status={status} />;
}

/**
 * Get KYC status badge JSX element - utility for existing code migration
 * @deprecated Use KYCStatusBadge component directly
 */
export function getKYCStatusBadge(status: string): React.ReactElement {
  return <KYCStatusBadge status={status} />;
}

/**
 * Get priority badge JSX element - utility for existing code migration
 * @deprecated Use PriorityBadge component directly
 */
export function getPriorityBadge(priority: string): React.ReactElement {
  return <PriorityBadge priority={priority} />;
}
