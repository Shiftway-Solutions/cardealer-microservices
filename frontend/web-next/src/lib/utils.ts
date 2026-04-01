import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

import { DOP_USD_EXCHANGE_RATE } from '@/lib/constants';

/**
 * Utility function to merge Tailwind CSS classes
 * Handles conflicts and removes duplicates
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Format currency in Dominican Pesos
 */
export function formatCurrency(
  amount: number,
  options?: {
    locale?: string;
    currency?: string;
    notation?: 'standard' | 'compact';
  }
): string {
  const { locale = 'es-DO', currency = 'DOP', notation = 'standard' } = options || {};

  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    notation,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

/**
 * Get the alternate currency amount.
 * If the listing is in DOP, returns the USD equivalent and vice-versa.
 */
export function getAlternateCurrencyDisplay(
  amount: number,
  listingCurrency: 'DOP' | 'USD' = 'DOP'
): { text: string; currency: string } {
  if (listingCurrency === 'USD') {
    const dop = Math.round(amount * DOP_USD_EXCHANGE_RATE);
    return { text: formatCurrency(dop, { currency: 'DOP' }), currency: 'DOP' };
  }
  const usd = Math.round(amount / DOP_USD_EXCHANGE_RATE);
  return { text: formatCurrency(usd, { currency: 'USD', locale: 'en-US' }), currency: 'USD' };
}

/**
 * Format number with thousand separators
 */
export function formatNumber(
  value: number,
  options?: {
    locale?: string;
    notation?: 'standard' | 'compact';
  }
): string {
  const { locale = 'es-DO', notation = 'standard' } = options || {};

  return new Intl.NumberFormat(locale, { notation }).format(value);
}

/**
 * Format mileage — RD market always uses km.
 * Legacy seed data may have stored 'Miles' as unit even though values are km;
 * normalise to 'km' for all display purposes.
 */
export function formatMileage(km: number, _unit?: string): string {
  if (km >= 1000) {
    return `${formatNumber(km)} km`;
  }
  return `${km} km`;
}

/**
 * FIX P0-008: Normalize known Dominican Republic city names
 * that may be stored without proper spacing (e.g., "Santo DomingoNorte" → "Santo Domingo Norte")
 */
const CITY_NORMALIZATIONS: Record<string, string> = {
  'Santo DomingoNorte': 'Santo Domingo Norte',
  'Santo DomingoEste': 'Santo Domingo Este',
  'Santo DomingoOeste': 'Santo Domingo Oeste',
  SantoDomingoNorte: 'Santo Domingo Norte',
  SantoDomingoEste: 'Santo Domingo Este',
  SantoDomingoOeste: 'Santo Domingo Oeste',
  SantoDomingo: 'Santo Domingo',
  'SantoDomingo Norte': 'Santo Domingo Norte',
  'SantoDomingo Este': 'Santo Domingo Este',
  'SantoDomingo Oeste': 'Santo Domingo Oeste',
};

export function normalizeLocationName(name: string): string {
  if (!name) return name;
  // Direct lookup first
  const mapped = CITY_NORMALIZATIONS[name];
  if (mapped) return mapped;
  // Regex: insert space before capital letters in "DomingoNorte", "DomingoEste", etc.
  return name.replace(/Domingo(Norte|Este|Oeste|de\s)/g, 'Domingo $1');
}

/**
 * Format date in local format
 */
export function formatDate(date: Date | string, options?: Intl.DateTimeFormatOptions): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    ...options,
  };

  return new Intl.DateTimeFormat('es-DO', defaultOptions).format(dateObj);
}

/**
 * Format relative time (e.g., "hace 2 días")
 */
export function formatRelativeTime(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - dateObj.getTime()) / 1000);

  const rtf = new Intl.RelativeTimeFormat('es-DO', { numeric: 'auto' });

  if (diffInSeconds < 60) {
    return rtf.format(-diffInSeconds, 'second');
  }

  const diffInMinutes = Math.floor(diffInSeconds / 60);
  if (diffInMinutes < 60) {
    return rtf.format(-diffInMinutes, 'minute');
  }

  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24) {
    return rtf.format(-diffInHours, 'hour');
  }

  const diffInDays = Math.floor(diffInHours / 24);
  if (diffInDays < 30) {
    return rtf.format(-diffInDays, 'day');
  }

  const diffInMonths = Math.floor(diffInDays / 30);
  if (diffInMonths < 12) {
    return rtf.format(-diffInMonths, 'month');
  }

  const diffInYears = Math.floor(diffInMonths / 12);
  return rtf.format(-diffInYears, 'year');
}

/**
 * Format date as short date (e.g., "7 feb 2026")
 */
export function formatShortDate(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return new Intl.DateTimeFormat('es-DO', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(dateObj);
}

/**
 * Format date with time (e.g., "7 de febrero de 2026, 14:30")
 */
export function formatDateTime(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return new Intl.DateTimeFormat('es-DO', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(dateObj);
}

/**
 * Format time only (e.g., "14:30")
 */
export function formatTime(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return new Intl.DateTimeFormat('es-DO', {
    hour: '2-digit',
    minute: '2-digit',
  }).format(dateObj);
}

/**
 * Get smart date format based on how recent the date is
 * - Today: "Hoy a las 14:30"
 * - Yesterday: "Ayer a las 14:30"
 * - This week: "Lunes a las 14:30"
 * - This year: "7 de febrero"
 * - Older: "7 de febrero de 2025"
 */
export function formatSmartDate(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const targetDate = new Date(dateObj.getFullYear(), dateObj.getMonth(), dateObj.getDate());
  const diffDays = Math.floor((today.getTime() - targetDate.getTime()) / (1000 * 60 * 60 * 24));

  const timeStr = formatTime(dateObj);

  if (diffDays === 0) {
    return `Hoy a las ${timeStr}`;
  }
  if (diffDays === 1) {
    return `Ayer a las ${timeStr}`;
  }
  if (diffDays < 7) {
    const dayName = new Intl.DateTimeFormat('es-DO', { weekday: 'long' }).format(dateObj);
    return `${dayName.charAt(0).toUpperCase() + dayName.slice(1)} a las ${timeStr}`;
  }
  if (dateObj.getFullYear() === now.getFullYear()) {
    return new Intl.DateTimeFormat('es-DO', { day: 'numeric', month: 'long' }).format(dateObj);
  }
  return formatDate(dateObj);
}

/**
 * Truncate text with ellipsis
 */
export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return `${text.slice(0, maxLength)}...`;
}

/**
 * Generate slug from text
 */
export function slugify(text: string): string {
  return text
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

/**
 * Debounce function
 */
export function debounce<T extends (...args: unknown[]) => unknown>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout> | null = null;

  return (...args: Parameters<T>) => {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
    timeoutId = setTimeout(() => func(...args), wait);
  };
}

/**
 * Generate random ID
 */
export function generateId(prefix = 'id'): string {
  return `${prefix}-${Math.random().toString(36).substring(2, 11)}`;
}

/**
 * Check if running on client side
 */
export function isClient(): boolean {
  return typeof window !== 'undefined';
}

/**
 * Check if running on server side
 */
export function isServer(): boolean {
  return typeof window === 'undefined';
}

/**
 * Sleep utility for async operations
 */
export function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

/**
 * Capitalize first letter
 */
export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
}

/**
 * Format phone number (Dominican Republic format)
 */
export function formatPhoneNumber(phone: string): string {
  const cleaned = phone.replace(/\D/g, '');
  if (cleaned.length === 10) {
    return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
  }
  if (cleaned.length === 11 && cleaned.startsWith('1')) {
    return `+1 (${cleaned.slice(1, 4)}) ${cleaned.slice(4, 7)}-${cleaned.slice(7)}`;
  }
  return phone;
}

/**
 * Validate email format
 */
export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

/**
 * Get initials from name
 */
export function getInitials(name: string, maxLength = 2): string {
  return name
    .split(' ')
    .map(word => word[0])
    .filter(Boolean)
    .slice(0, maxLength)
    .join('')
    .toUpperCase();
}
