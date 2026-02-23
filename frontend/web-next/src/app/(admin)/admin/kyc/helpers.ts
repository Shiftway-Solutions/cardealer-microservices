/**
 * Admin KYC - Shared helpers, types, and constants
 */

import { KYCStatus } from '@/services/kyc';

// =============================================================================
// HELPERS
// =============================================================================

export const statusToFilterKey = (status: KYCStatus): string => {
  switch (status) {
    case KYCStatus.Pending:
      return 'pending';
    case KYCStatus.InProgress:
      return 'in_progress';
    case KYCStatus.UnderReview:
      return 'in_review';
    case KYCStatus.Approved:
      return 'approved';
    case KYCStatus.Rejected:
      return 'rejected';
    case KYCStatus.DocumentsRequired:
      return 'documents_required';
    default:
      return 'pending';
  }
};

export const filterKeyToStatus = (key: string): KYCStatus | null => {
  switch (key) {
    case 'pending':
      return KYCStatus.Pending;
    case 'in_progress':
      return KYCStatus.InProgress;
    case 'in_review':
      return KYCStatus.UnderReview;
    case 'approved':
      return KYCStatus.Approved;
    case 'rejected':
      return KYCStatus.Rejected;
    case 'documents_required':
      return KYCStatus.DocumentsRequired;
    default:
      return null;
  }
};

export const getInitials = (name: string | null | undefined): string => {
  if (!name) return '??';
  return name
    .split(' ')
    .map(n => n[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
};

export const formatDate = (date: string | null | undefined): string => {
  if (!date) return 'N/A';
  return new Date(date).toLocaleDateString('es-DO', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
};

/**
 * UTC-safe date formatter for date-only fields (e.g. dateOfBirth).
 * Avoids off-by-one day errors caused by UTC midnight → local timezone conversion.
 * Example: "1989-12-12T00:00:00Z" → "12 dic de 1989" (not "11 dic de 1989" in UTC-4).
 */
export const formatDateOnly = (date: string | null | undefined): string => {
  if (!date) return 'N/A';
  const datePart = date.split('T')[0];
  if (!datePart) return 'N/A';
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) return 'N/A';
  return new Date(year, month - 1, day).toLocaleDateString('es-DO', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
};

// =============================================================================
// CONSTANTS
// =============================================================================

export const FIELD_VALIDATION_KEYS = [
  'nameMatches',
  'documentNumberMatches',
  'dobValid',
  'nationalityValid',
  'emailValid',
  'phoneValid',
  'addressValid',
  'documentAuthentic',
  'documentNotExpired',
  'documentFormatValid',
  'photoMatchesSelfie',
  'livenessGenuine',
  'livenessComplete',
];

export const VERIFICATION_ITEMS = [
  {
    key: 'documentAuthentic',
    label: 'Documento parece auténtico (no editado)',
    category: 'Documento',
  },
  { key: 'documentNotExpired', label: 'Documento no está expirado', category: 'Documento' },
  { key: 'documentFormatValid', label: 'Formato válido (cédula RD)', category: 'Documento' },
  { key: 'nameMatches', label: 'Nombre coincide con el registrado', category: 'Datos' },
  { key: 'documentNumberMatches', label: 'Número de documento coincide', category: 'Datos' },
  {
    key: 'photoMatchesSelfie',
    label: 'Foto del documento coincide con selfie',
    category: 'Identidad',
  },
  {
    key: 'livenessGenuine',
    label: 'Selfie parece genuino (no foto de foto)',
    category: 'Identidad',
  },
  { key: 'livenessComplete', label: 'Prueba de vida completada', category: 'Identidad' },
];
