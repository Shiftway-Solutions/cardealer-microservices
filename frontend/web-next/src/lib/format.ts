/**
 * Shared formatting utilities
 *
 * Canonical location for formatPrice and other display helpers.
 * Import from here instead of defining local copies.
 */

/**
 * Format a number as a currency string (default: DOP).
 *
 * @example
 * formatPrice(1500000)           // "RD$1,500,000"
 * formatPrice(25000, 'USD')      // "US$25,000"
 */
export function formatPrice(price: number, currency: string = 'DOP'): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(price);
}

/**
 * Format a price change amount with sign.
 */
export function formatPriceChange(change: number, currency: 'DOP' | 'USD' = 'DOP'): string {
  const prefix = change > 0 ? '+' : '';
  return `${prefix}${formatPrice(change, currency)}`;
}

/**
 * Translate internal English fuel type keys (from backend API) to Spanish display labels.
 * Single source of truth for UI — mirrors the mapping in vehicle-card.tsx.
 */
export const FUEL_TYPE_LABELS: Record<string, string> = {
  gasoline: 'Gasolina',
  gasolina: 'Gasolina',
  diesel: 'Diésel',
  diésel: 'Diésel',
  electric: 'Eléctrico',
  electrico: 'Eléctrico',
  eléctrico: 'Eléctrico',
  hybrid: 'Híbrido',
  hibrido: 'Híbrido',
  híbrido: 'Híbrido',
  plugin_hybrid: 'Híbrido Enchufable',
  'plug-in hybrid': 'Híbrido Enchufable',
  flex_fuel: 'Flex Fuel',
  lpg: 'GLP',
  gas: 'GLP',
  glp: 'GLP',
  gnv: 'GNV',
  hev: 'Híbrido',
  bev: 'Eléctrico',
  phev: 'Híbrido Enchufable',
};

export function formatFuelType(fuelType?: string | null): string {
  if (!fuelType) return '—';
  const key = fuelType.toLowerCase().replace(/\s+/g, '');
  return FUEL_TYPE_LABELS[key] ?? fuelType;
}

/**
 * Translate internal English transmission values to Spanish display labels.
 */
const TRANSMISSION_LABELS: Record<string, string> = {
  automatic: 'Automática',
  manual: 'Manual',
  cvt: 'CVT',
  'cvt automatic': 'CVT Automática',
  'dual-clutch': 'Doble Embrague',
  'semi-automatic': 'Semi-Automática',
  automática: 'Automática',
  automatic_cvt: 'CVT Automática',
};

export function formatTransmission(transmission?: string | null): string {
  if (!transmission) return '—';
  const key = transmission.toLowerCase().trim();
  return TRANSMISSION_LABELS[key] ?? transmission;
}

/**
 * Normalize a city name that may be stored without spaces between words.
 * Handles cases like "SantoDomingoNorte" → "Santo Domingo Norte" coming from
 * data import issues in the DR vehicle database.
 */
export function normalizeCity(city?: string | null): string {
  if (!city) return '';
  // Insert space before uppercase letters preceded by a lowercase letter (camelCase → spaced)
  return city.replace(/([a-záéíóúüñ])([A-ZÁÉÍÓÚÜÑ])/g, '$1 $2').trim();
}
