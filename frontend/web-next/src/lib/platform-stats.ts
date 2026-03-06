/**
 * OKLA Platform Stats — Single Source of Truth
 *
 * All pages MUST import stats from here to avoid inconsistencies.
 * Update ONLY this file when stats change.
 *
 * Last verified: March 2026
 */

export const PLATFORM_STATS = {
  /** Total vehicles published on the platform */
  vehiclesPublished: '10,000+',
  /** Total registered active users */
  activeUsers: '50,000+',
  /** Total registered dealers */
  registeredDealers: '500+',
  /** Customer satisfaction rate */
  satisfactionRate: '95%',
  /** Year OKLA was founded */
  foundingYear: '2025',
  /** Average time to sell a vehicle */
  avgSaleTime: '7 días',
  /** Total value transacted through the platform */
  totalTransacted: 'RD$500M+',
  /** Total vehicles sold */
  vehiclesSold: '10K+',
  /** Support response time */
  supportResponseTime: '24h',
} as const;

/** Stats for the /vender (sell) page */
export const SELLER_STATS = [
  { value: PLATFORM_STATS.vehiclesSold, label: 'Vehículos vendidos' },
  { value: PLATFORM_STATS.avgSaleTime, label: 'Tiempo promedio de venta' },
  { value: PLATFORM_STATS.satisfactionRate, label: 'Clientes satisfechos' },
  { value: PLATFORM_STATS.totalTransacted, label: 'Valor transado' },
];

/** Stats for the /nosotros (about) page */
export const ABOUT_STATS = [
  { value: PLATFORM_STATS.vehiclesPublished, label: 'Vehículos publicados' },
  { value: PLATFORM_STATS.activeUsers, label: 'Usuarios activos' },
  { value: PLATFORM_STATS.registeredDealers, label: 'Dealers registrados' },
  { value: PLATFORM_STATS.satisfactionRate, label: 'Satisfacción del cliente' },
];

/** Stats for the /prensa (press) page */
export const PRESS_STATS = [
  { value: PLATFORM_STATS.vehiclesPublished, label: 'Vehículos en plataforma' },
  { value: PLATFORM_STATS.activeUsers, label: 'Usuarios activos' },
  { value: PLATFORM_STATS.registeredDealers, label: 'Dealers registrados' },
  { value: PLATFORM_STATS.foundingYear, label: 'Año de fundación' },
];

/** Stats for the /dealers page */
export const DEALER_STATS = [
  { value: PLATFORM_STATS.registeredDealers, label: 'Dealers activos' },
  { value: PLATFORM_STATS.vehiclesSold, label: 'Ventas mensuales' },
  { value: PLATFORM_STATS.satisfactionRate, label: 'Satisfacción' },
  { value: PLATFORM_STATS.supportResponseTime, label: 'Tiempo de soporte' },
];

/** Stats for homepage social proof section */
export const HOMEPAGE_STATS = [
  { value: PLATFORM_STATS.vehiclesPublished, label: 'Vehículos publicados' },
  { value: PLATFORM_STATS.activeUsers, label: 'Usuarios activos' },
  { value: PLATFORM_STATS.registeredDealers, label: 'Dealers registrados' },
  { value: PLATFORM_STATS.satisfactionRate, label: 'Satisfacción del cliente' },
];
