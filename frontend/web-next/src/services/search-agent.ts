import apiClient from '@/lib/api-client';

// ============================================================
// SearchAgent Service — AI-powered vehicle search via Claude
// v2.1 — production ready, natural language search
// ============================================================

/**
 * Request DTO for the AI search endpoint
 */
export interface SearchAgentRequest {
  query: string;
  sessionId?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Search filters extracted by Claude AI
 */
export interface SearchFilters {
  marca: string | null;
  modelo: string | null;
  anio_desde: number | null;
  anio_hasta: number | null;
  precio_min: number | null;
  precio_max: number | null;
  moneda: string | null;
  tipo_vehiculo: string | null;
  transmision: string | null;
  combustible: string | null;
  condicion: string | null;
  kilometraje_max: number | null;
  provincia: string | null;
  ciudad: string | null;
  color: string | null;
  traccion: string | null;
}

export interface SponsoredConfig {
  umbral_afinidad: number;
  tipo_vehiculo_afinidad: string[];
  marcas_afinidad: string[];
  precio_rango_afinidad: { min: number; max: number; moneda: string } | null;
  anio_rango_afinidad: { desde: number; hasta: number } | null;
  max_porcentaje_resultados: number;
  posiciones_fijas: number[];
  etiqueta: string;
}

/**
 * Full AI response from SearchAgent
 */
export interface SearchAgentAiResponse {
  filtros_exactos: SearchFilters | null;
  filtros_relajados: SearchFilters | null;
  resultado_minimo_garantizado: number;
  nivel_filtros_activo: number;
  patrocinados_config: SponsoredConfig | null;
  ordenar_por: string;
  dealer_verificado: boolean | null;
  confianza: number;
  query_reformulada: string | null;
  advertencias: string[];
  mensaje_relajamiento: string | null;
  mensaje_usuario: string | null;
}

/**
 * Complete result from the SearchAgent endpoint
 */
export interface SearchAgentResult {
  aiFilters: SearchAgentAiResponse;
  wasCached: boolean;
  latencyMs: number;
  isAiSearchEnabled: boolean;
}

/**
 * SearchAgent configuration (admin only)
 */
export interface SearchAgentConfig {
  id: string;
  isEnabled: boolean;
  model: string;
  temperature: number;
  maxTokens: number;
  minResultsPerPage: number;
  maxResultsPerPage: number;
  sponsoredAffinityThreshold: number;
  maxSponsoredPercentage: number;
  sponsoredPositions: string;
  sponsoredLabel: string;
  priceRelaxPercent: number;
  yearRelaxRange: number;
  maxRelaxationLevel: number;
  enableCache: boolean;
  cacheTtlSeconds: number;
  semanticCacheThreshold: number;
  maxQueriesPerMinutePerIp: number;
  aiSearchTrafficPercent: number;
  systemPromptOverride: string | null;
  createdAt: string;
  updatedAt: string;
  updatedBy: string | null;
}

// ========================
// API Functions
// ========================

/**
 * Process a natural language vehicle search query using AI.
 * Public endpoint — no auth required.
 */
export async function aiSearch(request: SearchAgentRequest): Promise<SearchAgentResult> {
  const response = await apiClient.post('/api/search-agent/search', request);
  return response.data?.data ?? response.data;
}

/**
 * Get the SearchAgent service status.
 */
export async function getSearchAgentStatus(): Promise<{
  service: string;
  status: string;
  version: string;
  timestamp: string;
}> {
  const response = await apiClient.get('/api/search-agent/status');
  return response.data?.data ?? response.data;
}

/**
 * Get SearchAgent configuration (admin only).
 */
export async function getSearchAgentConfig(): Promise<SearchAgentConfig> {
  const response = await apiClient.get('/api/search-agent/config');
  return response.data?.data ?? response.data;
}

/**
 * Update SearchAgent configuration (admin only).
 * Supports partial updates — only send fields to change.
 */
export async function updateSearchAgentConfig(
  config: Partial<SearchAgentConfig>
): Promise<SearchAgentConfig> {
  const response = await apiClient.put('/api/search-agent/config', config);
  return response.data?.data ?? response.data;
}

/**
 * Convert AI filters to the VehicleSearchFilters format used by useVehicleSearch hook.
 * Keys match VehicleSearchFilters interface (camelCase).
 * For URL params, use aiFiltersToUrlParams() which outputs snake_case keys.
 */
// Map backend traccion values to frontend drivetrain values
const TRACCION_TO_DRIVETRAIN: Record<string, string> = {
  '4x4': '4wd',
  fwd: 'fwd',
  rwd: 'rwd',
  awd: 'awd',
};

export function aiFiltersToSearchParams(
  filters: SearchFilters
): Record<string, string | number | undefined> {
  return {
    make: filters.marca ?? undefined,
    model: filters.modelo ?? undefined,
    yearMin: filters.anio_desde ?? undefined,
    yearMax: filters.anio_hasta ?? undefined,
    priceMin: filters.precio_min ?? undefined,
    priceMax: filters.precio_max ?? undefined,
    currency: filters.moneda ?? undefined,
    bodyType: filters.tipo_vehiculo ?? undefined,
    transmission: filters.transmision ?? undefined,
    fuelType: filters.combustible ?? undefined,
    condition: filters.condicion ?? undefined,
    mileageMax: filters.kilometraje_max ?? undefined,
    province: filters.provincia ?? undefined,
    city: filters.ciudad ?? undefined,
    color: filters.color ?? undefined,
    drivetrain: filters.traccion
      ? (TRACCION_TO_DRIVETRAIN[filters.traccion] ?? filters.traccion)
      : undefined,
  };
}

/**
 * Mapping from VehicleSearchFilters camelCase keys to URL snake_case param names.
 * Used by the homepage redirect path where filters are serialized into the URL.
 */
const CAMEL_TO_URL_PARAM: Record<string, string> = {
  yearMin: 'year_min',
  yearMax: 'year_max',
  priceMin: 'price_min',
  priceMax: 'price_max',
  mileageMax: 'mileage_max',
  bodyType: 'body_type',
  fuelType: 'fuel_type',
};

/**
 * Convert AI filters directly to URL search param entries (snake_case keys).
 * Used by the homepage NLP search to build the redirect URL.
 */
export function aiFiltersToUrlParams(
  filters: SearchFilters
): Record<string, string | number | undefined> {
  const camelParams = aiFiltersToSearchParams(filters);
  const urlParams: Record<string, string | number | undefined> = {};
  for (const [key, value] of Object.entries(camelParams)) {
    const urlKey = CAMEL_TO_URL_PARAM[key] || key;
    urlParams[urlKey] = value;
  }
  return urlParams;
}

// Default export as a service object
const searchAgentService = {
  aiSearch,
  getSearchAgentStatus,
  getSearchAgentConfig,
  updateSearchAgentConfig,
  aiFiltersToSearchParams,
  aiFiltersToUrlParams,
};

export default searchAgentService;
