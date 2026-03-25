/**
 * Vehicles Service - API client for vehicle operations
 * Connects via API Gateway to VehiclesSaleService
 */

import { apiClient } from '@/lib/api-client';
import { normalizeLocationName } from '@/lib/utils';
import type {
  Vehicle,
  VehicleImage,
  VehicleCardData,
  VehicleSearchParams,
  PaginatedResponse,
  DealRating,
} from '@/types';

// ============================================================
// API TYPES (matching backend DTOs)
// ============================================================

export interface VehicleDto {
  id: string;
  slug: string;
  make: string;
  model: string;
  year: number;
  trim?: string;
  bodyType: string;
  price: number;
  originalPrice?: number;
  marketPrice?: number;
  currency: string;
  mileage: number;
  transmission: string;
  fuelType: string;
  drivetrain?: string;
  engineSize?: string;
  horsepower?: number;
  exteriorColor?: string;
  interiorColor?: string;
  doors?: number;
  seats?: number;
  features?: string[];
  images: VehicleImageDto[];
  has360View?: boolean;
  hasVideo?: boolean;
  status: string;
  condition: string;
  isFeatured?: boolean;
  viewCount?: number;
  favoriteCount?: number;
  sellerId: string;
  sellerType: string;
  seller?: SellerDto;
  city: string;
  province: string;
  country: string;
  latitude?: number;
  longitude?: number;
  description?: string;
  vin?: string;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  isNegotiable?: boolean;
  isCertified?: boolean;
  isVerified?: boolean;
}

export interface VehicleImageDto {
  id: string;
  url: string;
  thumbnailUrl?: string;
  alt?: string;
  order: number;
  isPrimary?: boolean;
}

export interface SellerDto {
  id: string;
  name: string;
  type: 'seller' | 'dealer';
  avatar?: string;
  phone?: string;
  email?: string;
  city?: string;
  rating?: number;
  reviewCount?: number;
  responseRate?: number;
  responseTime?: string;
  isVerified?: boolean;
  memberSince?: string;
  listingsCount?: number;
}

/** Matches backend VehicleSearchResult (camelCase from .NET) */
export interface VehicleSearchResponse {
  vehicles: VehicleDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/** Matches backend SellerVehicleDto */
export interface SellerVehicleDto {
  id: string;
  title: string;
  slug: string;
  price: number;
  currency: string;
  status: string;
  mainImageUrl?: string;
  year: number;
  make: string;
  model: string;
  mileage: number;
  transmission?: string;
  fuelType?: string;
  views: number;
  favorites: number;
  createdAt: string;
}

/** Matches backend SellerVehiclesResponse */
export interface SellerVehiclesResponse {
  data: SellerVehicleDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ============================================================
// TRANSFORM FUNCTIONS
// ============================================================

/**
 * Compute slug from Vehicle entity fields (mirrors backend GenerateSlug logic)
 * Backend: "{year}-{make}-{model}-{id[..8]}"
 */
function computeSlugFromEntity(entity: {
  year?: number;
  make?: string;
  model?: string;
  id?: string;
}): string {
  const year = entity.year || '';
  const make = (entity.make || '').toLowerCase().replace(/\s+/g, '-');
  const model = (entity.model || '').toLowerCase().replace(/\s+/g, '-');
  const shortId = (entity.id || '').replace(/-/g, '').slice(0, 8).toLowerCase();
  return `${year}-${make}-${model}-${shortId}`.replace(/--+/g, '-');
}

/**
 * Map backend BodyStyle enum string to frontend VehicleBodyType
 */
function mapBodyStyle(bodyStyle: string | number | undefined): Vehicle['bodyType'] {
  if (bodyStyle === undefined || bodyStyle === null) return 'sedan';
  const s = String(bodyStyle).toLowerCase().replace(/\s+/g, '');
  const map: Record<string, Vehicle['bodyType']> = {
    '0': 'sedan',
    sedan: 'sedan',
    '1': 'coupe',
    coupe: 'coupe',
    '2': 'hatchback',
    hatchback: 'hatchback',
    '3': 'wagon',
    wagon: 'wagon',
    '4': 'suv',
    suv: 'suv',
    '5': 'crossover',
    crossover: 'crossover',
    '6': 'pickup',
    pickup: 'pickup',
    truck: 'pickup',
    '7': 'sedan',
    van: 'sedan',
    '8': 'minivan',
    minivan: 'minivan',
    '9': 'convertible',
    convertible: 'convertible',
    '10': 'sports',
    sportscar: 'sports',
    sports: 'sports',
    sport: 'sports',
  };
  return map[s] || 'sedan';
}

/**
 * Map backend VehicleCondition enum string to frontend condition
 */
function mapCondition(condition: string | number | undefined): Vehicle['condition'] {
  if (condition === undefined || condition === null) return 'used';
  const s = String(condition).toLowerCase().replace(/\s+/g, '');
  const map: Record<string, Vehicle['condition']> = {
    '0': 'new',
    new: 'new',
    '1': 'certified',
    certifiedpreowned: 'certified',
    certified: 'certified',
    '2': 'used',
    used: 'used',
    salvage: 'used',
    rebuilt: 'used',
    '3': 'used',
    '4': 'used',
  };
  return map[s] || 'used';
}

/**
 * Map backend SellerType enum string to frontend sellerType
 */
function mapSellerType(sellerType: string | number | undefined): Vehicle['sellerType'] {
  if (sellerType === undefined || sellerType === null) return 'seller';
  const s = String(sellerType).toLowerCase();
  if (s === '1' || s === 'dealer' || s === 'franchise' || s === 'wholesale') return 'dealer';
  return 'seller';
}

/**
 * Map backend VehicleStatus enum string to frontend VehicleStatus
 */
function mapStatus(status: string | number | undefined): Vehicle['status'] {
  if (status === undefined || status === null) return 'active';
  const s = String(status).toLowerCase();
  const map: Record<string, Vehicle['status']> = {
    '0': 'draft',
    draft: 'draft',
    '1': 'pending',
    pendingreview: 'pending',
    '2': 'active',
    active: 'active',
    '3': 'reserved',
    reserved: 'reserved',
    '4': 'sold',
    sold: 'sold',
    '5': 'paused',
    archived: 'paused',
    '6': 'rejected',
    rejected: 'rejected',
    '8': 'pending_media',
    pendingmedia: 'pending_media',
    expired: 'expired',
    paused: 'paused',
  };
  return map[s] || 'active';
}

/**
 * Map backend transmission enum to frontend string
 */
function mapTransmission(val: string | number | undefined): string {
  const s = String(val ?? '')
    .toLowerCase()
    .replace(/\s+/g, '');
  const map: Record<string, string> = {
    '0': 'automatic',
    automatic: 'automatic',
    '1': 'manual',
    manual: 'manual',
    '2': 'cvt',
    cvt: 'cvt',
    '3': 'semi-automatic',
    automated: 'semi-automatic',
    '4': 'semi-automatic',
    dualclutch: 'semi-automatic',
  };
  return map[s] || s || 'automatic';
}

/**
 * Map backend fuelType enum to Spanish display label
 */
function mapFuelType(val: string | number | undefined): string {
  const s = String(val ?? '')
    .toLowerCase()
    .replace(/\s+/g, '');
  const map: Record<string, string> = {
    '0': 'Gasolina',
    gasoline: 'Gasolina',
    gasolina: 'Gasolina',
    '1': 'Diésel',
    diesel: 'Diésel',
    diésel: 'Diésel',
    '2': 'Eléctrico',
    electric: 'Eléctrico',
    eléctrico: 'Eléctrico',
    '3': 'Híbrido',
    hybrid: 'Híbrido',
    híbrido: 'Híbrido',
    '4': 'Híbrido',
    pluginhybrid: 'Híbrido',
    '5': 'Gasolina',
    hydrogen: 'Gasolina',
    '6': 'Gasolina',
    flexfuel: 'Gasolina',
    '7': 'Gasolina',
    naturalgas: 'GLP',
  };
  return map[s] || s || 'Gasolina';
}

/**
 * Parse FeaturesJson string to array
 */
function parseFeaturesJson(featuresJson: string | string[] | undefined): string[] {
  if (!featuresJson) return [];
  if (Array.isArray(featuresJson)) return featuresJson;
  try {
    const parsed = JSON.parse(featuresJson);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

/**
 * Transform backend Vehicle entity or VehicleDto to frontend Vehicle type.
 * Handles both old VehicleDto shape and raw Vehicle entity from backend.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const transformVehicle = (dto: VehicleDto | Record<string, any>): Vehicle => {
  const raw = dto as Record<string, unknown>;

  // Compute slug: use existing slug field or compute from entity fields
  const slug =
    (raw.slug as string) ||
    computeSlugFromEntity({
      year: raw.year as number,
      make: raw.make as string,
      model: raw.model as string,
      id: raw.id as string,
    });

  // Handle images: VehicleImageDto or VehicleImage entity
  const rawImages = (raw.images as Record<string, unknown>[]) || [];
  const images: VehicleImage[] = rawImages.map(img => ({
    id: (img.id as string) || '',
    url: (img.url as string) || '',
    thumbnailUrl: (img.thumbnailUrl as string) || undefined,
    alt: (img.alt as string) || (img.caption as string) || undefined,
    order: (img.order as number) ?? (img.sortOrder as number) ?? 0,
    isPrimary: (img.isPrimary as boolean) ?? false,
  }));

  // Location: handle both {city, province} and {city, state} shapes
  const city = normalizeLocationName((raw.city as string) || '');
  const province = normalizeLocationName((raw.province as string) || (raw.state as string) || '');
  const country = (raw.country as string) || 'DO';

  // Features: handle both array and JSON string
  const features = parseFeaturesJson(
    (raw.features as string[] | string) || (raw.featuresJson as string)
  );

  // Seller info
  const sellerRaw = raw.seller as Record<string, unknown> | undefined;

  return {
    id: raw.id as string,
    slug,
    make: raw.make as string,
    model: raw.model as string,
    year: raw.year as number,
    trim: raw.trim as string | undefined,
    bodyType: mapBodyStyle((raw.bodyType as string) || (raw.bodyStyle as string)),
    description: raw.description as string | undefined,
    price: raw.price as number,
    originalPrice: raw.originalPrice as number | undefined,
    marketPrice: raw.marketPrice as number | undefined,
    currency: ((raw.currency as string) || 'DOP') as 'DOP' | 'USD',
    dealRating: calculateDealRating(raw.price as number, raw.marketPrice as number | undefined),
    isNegotiable: raw.isNegotiable as boolean | undefined,
    mileage: raw.mileage as number,
    transmission: mapTransmission(raw.transmission as string | number) as Vehicle['transmission'],
    fuelType: mapFuelType(raw.fuelType as string | number) as Vehicle['fuelType'],
    drivetrain: undefined,
    engineSize: raw.engineSize as string | undefined,
    horsepower: raw.horsepower as number | undefined,
    exteriorColor: raw.exteriorColor as string | undefined,
    interiorColor: raw.interiorColor as string | undefined,
    doors: raw.doors as number | undefined,
    seats: raw.seats as number | undefined,
    features,
    images,
    has360View: raw.has360View as boolean | undefined,
    hasVideo: raw.hasVideo as boolean | undefined,
    status: mapStatus(raw.status as string | number),
    condition: mapCondition(raw.condition as string | number),
    isFeatured: raw.isFeatured as boolean | undefined,
    viewCount: raw.viewCount as number | undefined,
    favoriteCount: raw.favoriteCount as number | undefined,
    sellerId: (raw.sellerId as string) || '',
    sellerType: mapSellerType(raw.sellerType as string | number),
    location: {
      city,
      province,
      country,
      coordinates:
        raw.latitude && raw.longitude
          ? { latitude: raw.latitude as number, longitude: raw.longitude as number }
          : undefined,
    },
    rejectionReason: raw.rejectionReason as string | undefined,
    vin: raw.vin as string | undefined,
    createdAt: raw.createdAt as string,
    updatedAt: raw.updatedAt as string,
    publishedAt: raw.publishedAt as string | undefined,
    // Attach seller info: prefer nested seller object, fall back to flat fields on entity
    ...(sellerRaw
      ? {
          seller: {
            id: (sellerRaw.id as string) || (raw.sellerId as string) || '',
            name: (sellerRaw.name as string) || '',
            type: mapSellerType(sellerRaw.type as string) as 'seller' | 'dealer',
            avatar: sellerRaw.avatar as string | undefined,
            phone: sellerRaw.phone as string | undefined,
            email: sellerRaw.email as string | undefined,
            city: sellerRaw.city as string | undefined,
            rating: sellerRaw.rating as number | undefined,
            reviewCount: sellerRaw.reviewCount as number | undefined,
            isVerified: sellerRaw.isVerified as boolean | undefined,
          },
        }
      : {
          // Flat seller fields from raw Vehicle entity
          seller: {
            id: (raw.sellerId as string) || '',
            name: (raw.sellerName as string) || '',
            type: mapSellerType(raw.sellerType as string) as 'seller' | 'dealer',
            phone: (raw.sellerPhone as string) || undefined,
            email: (raw.sellerEmail as string) || undefined,
            city,
            isVerified: undefined,
          },
        }),
  } as Vehicle;
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const transformToCardData = (dto: VehicleDto | Record<string, any>): VehicleCardData => {
  const raw = dto as Record<string, unknown>;
  // Compute slug if not present (raw entity from backend)
  const slug =
    (raw.slug as string) ||
    computeSlugFromEntity({
      year: raw.year as number,
      make: raw.make as string,
      model: raw.model as string,
      id: raw.id as string,
    });
  // Handle images: entity uses sortOrder, DTO uses order
  const rawImages = (raw.images as Record<string, unknown>[]) || [];
  const firstImage = rawImages.slice().sort((a, b) => {
    const aOrder = (a.sortOrder as number) ?? (a.order as number) ?? 99;
    const bOrder = (b.sortOrder as number) ?? (b.order as number) ?? 99;
    return aOrder - bOrder;
  })[0];
  const imageUrl = (firstImage?.url as string) || '/placeholder-car.jpg';
  // Location: backend entity uses state, DTO uses province
  const city = normalizeLocationName((raw.city as string) || '');
  const province = normalizeLocationName((raw.province as string) || (raw.state as string) || '');
  const location = [city, province].filter(Boolean).join(', ');
  // Seller info
  const seller = raw.seller as Record<string, unknown> | undefined;
  // Condition: map to boolean isNew
  const conditionStr = String(raw.condition ?? '').toLowerCase();
  const isNew = conditionStr === 'new' || conditionStr === '0';
  const isCertified =
    conditionStr === 'certified' || conditionStr === 'certifiedpreowned' || conditionStr === '1';

  return {
    id: raw.id as string,
    slug,
    make: raw.make as string,
    model: raw.model as string,
    year: raw.year as number,
    price: raw.price as number,
    currency: ((raw.currency as string) || 'DOP') as 'DOP' | 'USD',
    mileage: raw.mileage as number,
    transmission: mapTransmission(raw.transmission as string | number),
    fuelType: mapFuelType(raw.fuelType as string | number),
    imageUrl,
    dealRating: calculateDealRating(raw.price as number, raw.marketPrice as number | undefined),
    location,
    trim: raw.trim as string | undefined,
    photoCount: rawImages.length,
    isNew,
    isCertified,
    monthlyPayment: calculateMonthlyPayment(raw.price as number),
    dealerName: seller?.name as string | undefined,
    dealerRating: seller?.rating as number | undefined,
    status: mapStatus(raw.status as string | number) as VehicleCardData['status'],
    viewCount: raw.viewCount as number | undefined,
    createdAt: raw.createdAt as string,
  };
};

function calculateDealRating(price: number, marketPrice?: number): DealRating | undefined {
  if (!marketPrice || marketPrice === 0) return undefined;

  const priceDiff = ((marketPrice - price) / marketPrice) * 100;

  if (priceDiff >= 10) return 'great';
  if (priceDiff >= 5) return 'good';
  if (priceDiff >= -5) return 'fair';
  if (priceDiff < -10) return 'high';
  return 'uncertain';
}

function calculateMonthlyPayment(price: number): number {
  // Simple calculation: 60 months, 12% annual rate
  const months = 60;
  const annualRate = 0.12;
  const monthlyRate = annualRate / 12;
  const payment =
    (price * monthlyRate * Math.pow(1 + monthlyRate, months)) /
    (Math.pow(1 + monthlyRate, months) - 1);
  return Math.round(payment);
}

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Get vehicle by slug
 */
export async function getVehicleBySlug(slug: string): Promise<Vehicle> {
  const response = await apiClient.get<VehicleDto>(`/api/vehicles/slug/${slug}`);
  return transformVehicle(response.data);
}

/**
 * Get vehicle by ID
 */
export async function getVehicleById(id: string): Promise<Vehicle> {
  const response = await apiClient.get<VehicleDto>(`/api/vehicles/${id}`);
  return transformVehicle(response.data);
}

/**
 * Search vehicles with filters
 */
export async function searchVehicles(
  params: VehicleSearchParams
): Promise<PaginatedResponse<VehicleCardData>> {
  // Map frontend VehicleSearchParams → backend VehicleSearchRequest
  // Backend uses: Search, Page, PageSize, SortBy, SortDescending, Make, Model,
  //   MinYear, MaxYear, MinPrice, MaxPrice, MaxMileage, BodyStyle, FuelType,
  //   Transmission, Condition, State
  const backendParams: Record<string, unknown> = {
    Page: params.page ?? 1,
    PageSize: params.pageSize ?? 12,
  };
  // Maps frontend Spanish/abbreviated strings → C# enum member names (case-insensitive
  // ASP.NET Core binding handles exact English names; Spanish strings cause text-search fallback).
  const conditionMap: Record<string, string> = {
    nuevo: 'New',
    usado: 'Used',
  };
  const fuelTypeMap: Record<string, string> = {
    gasolina: 'Gasoline',
    diesel: 'Diesel',
    hibrido: 'Hybrid',
    electrico: 'Electric',
    glp: 'NaturalGas',
    pluginhybrid: 'PlugInHybrid',
  };
  const transmissionMap: Record<string, string> = {
    automatica: 'Automatic',
    manual: 'Manual',
    cvt: 'CVT',
  };
  const driveTypeMap: Record<string, string> = {
    fwd: 'FWD',
    rwd: 'RWD',
    awd: 'AWD',
    '4wd': 'FourWD',
  };
  const sellerTypeMap: Record<string, string> = {
    dealer: 'Dealer',
    seller: 'Seller',
  };

  if (params.q) backendParams.Search = params.q;
  if (params.make) backendParams.Make = params.make;
  if (params.model) backendParams.Model = params.model;
  if (params.yearMin) backendParams.MinYear = params.yearMin;
  if (params.yearMax) backendParams.MaxYear = params.yearMax;
  if (params.priceMin) backendParams.MinPrice = params.priceMin;
  if (params.priceMax) backendParams.MaxPrice = params.priceMax;
  if (params.mileageMax) backendParams.MaxMileage = params.mileageMax;
  if (params.bodyType) backendParams.BodyStyle = params.bodyType;
  if (params.fuelType) backendParams.FuelType = fuelTypeMap[params.fuelType] ?? params.fuelType;
  if (params.transmission)
    backendParams.Transmission = transmissionMap[params.transmission] ?? params.transmission;
  if (params.condition)
    backendParams.Condition = conditionMap[params.condition] ?? params.condition;
  if (params.province) backendParams.State = params.province;
  if (params.city) backendParams.City = params.city;
  if (params.drivetrain)
    backendParams.DriveType = driveTypeMap[params.drivetrain] ?? params.drivetrain;
  if (params.color) backendParams.ExteriorColor = params.color;
  if (params.isCertified !== undefined) backendParams.IsCertified = params.isCertified;
  if (params.hasCleanTitle !== undefined) backendParams.HasCleanTitle = params.hasCleanTitle;
  if (params.sellerType)
    backendParams.SellerType = sellerTypeMap[params.sellerType] ?? params.sellerType;
  // Extended DR-market filters
  if (params.seats) backendParams.MinSeats = params.seats;
  if (params.cylinders) backendParams.Cylinders = params.cylinders;
  if (params.interiorColor) backendParams.InteriorColor = params.interiorColor;
  if (params.features?.length) backendParams.Features = params.features.join(',');
  if (params.sortBy) {
    backendParams.SortBy = params.sortBy;
    backendParams.SortDescending = params.sortOrder === 'desc';
  }

  const response = await apiClient.get<VehicleSearchResponse>('/api/vehicles', {
    params: backendParams,
  });

  return {
    items: response.data.vehicles.map(transformToCardData),
    pagination: {
      page: response.data.page,
      pageSize: response.data.pageSize,
      totalItems: response.data.totalCount,
      totalPages: response.data.totalPages,
      hasNextPage: response.data.page < response.data.totalPages,
      hasPreviousPage: response.data.page > 1,
    },
  };
}

/**
 * Get similar vehicles
 */
export async function getSimilarVehicles(
  vehicleId: string,
  limit: number = 6
): Promise<VehicleCardData[]> {
  const response = await apiClient.get<VehicleDto[]>(`/api/vehicles/${vehicleId}/similar`, {
    params: { limit },
  });
  return response.data.map(transformToCardData);
}

/**
 * Get featured vehicles
 */
export async function getFeaturedVehicles(limit: number = 8): Promise<VehicleCardData[]> {
  const response = await apiClient.get<VehicleDto[]>('/api/vehicles/featured', {
    params: { limit },
  });
  return response.data.map(transformToCardData);
}

/**
 * Track vehicle view
 */
export async function trackVehicleView(vehicleId: string): Promise<void> {
  await apiClient.post(`/api/vehicles/${vehicleId}/views`);
}

/**
 * Get vehicles by dealer ID
 */
export async function getVehiclesByDealer(
  dealerId: string,
  params: { page?: number; pageSize?: number; status?: string } = {}
): Promise<PaginatedResponse<VehicleCardData>> {
  const page = params.page || 1;
  const pageSize = params.pageSize || 20;

  const queryParams: Record<string, string | number> = { page, pageSize };
  if (params.status) queryParams.status = params.status;

  const response = await apiClient.get<SellerVehiclesResponse>(`/api/vehicles/seller/${dealerId}`, {
    params: queryParams,
  });

  const items: VehicleCardData[] = response.data.data.map(v => ({
    id: v.id,
    slug: v.slug,
    make: v.make,
    model: v.model,
    year: v.year,
    price: v.price,
    currency: (v.currency as 'DOP' | 'USD') || 'DOP',
    mileage: v.mileage,
    transmission: v.transmission || '',
    fuelType: v.fuelType || '',
    imageUrl: v.mainImageUrl || '/placeholder-car.jpg',
    location: '',
    status: (v.status?.toLowerCase() as VehicleCardData['status']) || 'active',
    viewCount: v.views,
    createdAt: v.createdAt,
  }));

  const totalPages = response.data.totalPages;

  return {
    items,
    pagination: {
      page: response.data.page,
      pageSize: response.data.pageSize,
      totalItems: response.data.totalCount,
      totalPages,
      hasNextPage: response.data.page < totalPages,
      hasPreviousPage: response.data.page > 1,
    },
  };
}

/**
 * Get vehicles by IDs (for comparison)
 * Uses POST /api/vehicles/compare which accepts { vehicleIds: Guid[] }
 */
export async function getVehiclesByIds(ids: string[]): Promise<VehicleCardData[]> {
  if (ids.length === 0) return [];

  try {
    const response = await apiClient.post<VehicleDto[]>('/api/vehicles/compare', {
      vehicleIds: ids,
    });
    return response.data.map(transformToCardData);
  } catch {
    // Fallback: Fetch each vehicle individually (parallel)
    const vehicles = await Promise.all(
      ids.map(async id => {
        try {
          const response = await apiClient.get<VehicleDto>(`/api/vehicles/${id}`);
          return transformToCardData(response.data);
        } catch {
          return null;
        }
      })
    );
    return vehicles.filter((v): v is VehicleCardData => v !== null);
  }
}

/**
 * Get FULL vehicle DTOs for comparison page (includes specs, features, etc.)
 * Returns VehicleDto[] without stripping to VehicleCardData.
 */
export async function getVehicleDtosForComparison(ids: string[]): Promise<VehicleDto[]> {
  if (ids.length === 0) return [];

  try {
    const response = await apiClient.post<VehicleDto[]>('/api/vehicles/compare', {
      vehicleIds: ids,
    });
    return response.data;
  } catch {
    // Fallback: Fetch each vehicle individually (parallel)
    const vehicles = await Promise.all(
      ids.map(async id => {
        try {
          const response = await apiClient.get<VehicleDto>(`/api/vehicles/${id}`);
          return response.data;
        } catch {
          return null;
        }
      })
    );
    return vehicles.filter((v): v is VehicleDto => v !== null);
  }
}

// ============================================================
// 360° VIEW TYPES
// ============================================================

export interface Hotspot360 {
  id: string;
  x: number; // percentage 0-100
  y: number; // percentage 0-100
  label: string;
  description: string;
}

export interface View360Data {
  vehicleId: string;
  vehicleTitle: string;
  exteriorImages: string[];
  interiorImages: string[];
  hotspots: Hotspot360[];
  hasExterior360: boolean;
  hasInterior360: boolean;
}

/**
 * Get 360° view data for a vehicle
 */
export async function getVehicle360View(vehicleIdOrSlug: string): Promise<View360Data> {
  try {
    const response = await apiClient.get<View360Data>(`/api/vehicles/${vehicleIdOrSlug}/360`);
    return response.data;
  } catch {
    // Return empty data if 360 view not available
    // The API will return vehicle basic info if no 360 images uploaded
    const vehicle = await getVehicleBySlug(vehicleIdOrSlug);
    return {
      vehicleId: vehicle.id,
      vehicleTitle: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
      exteriorImages: vehicle.images.map(img => img.url),
      interiorImages: [],
      hotspots: [],
      hasExterior360: false,
      hasInterior360: false,
    };
  }
}

// ============================================================
// VIN DECODE TYPES
// ============================================================

export interface FieldConfidence {
  value: string;
  source: string;
  confidence: number; // 0.0 - 1.0
}

export interface SmartVinDecodeResult {
  vin: string;
  make: string;
  model: string;
  year: number | null;
  trim: string | null;
  bodyStyle: string | null;
  vehicleType: string | null;
  engineSize: string | null;
  cylinders: number | null;
  horsepower: number | null;
  fuelType: string | null;
  transmission: string | null;
  driveType: string | null;
  doors: number | null;
  manufacturedIn: string | null;
  plantCountry: string | null;

  // Catalog match
  catalogMakeId: string | null;
  catalogModelId: string | null;
  catalogTrimId: string | null;
  hasCatalogMatch: boolean;

  // Duplicate
  isDuplicate: boolean;
  existingVehicleId: string | null;
  existingVehicleSlug: string | null;

  // Quality
  fieldConfidences: Record<string, FieldConfidence>;
  suggestedDescription: string | null;

  // Auto-fill
  autoFill: VinAutoFillData | null;
}

export interface VinAutoFillData {
  make: string;
  model: string;
  year: number;
  trim: string | null;
  vehicleType: string;
  bodyStyle: string;
  fuelType: string;
  transmission: string;
  driveType: string;
  engineSize: string | null;
  horsepower: number | null;
  cylinders: number | null;
}

export interface VinExistsResponse {
  exists: boolean;
  vehicleId?: string;
  slug?: string;
  status?: string;
}

export interface BatchVinDecodeResponse {
  results: SmartVinDecodeResult[];
  errors: Record<string, string>;
  totalRequested: number;
  totalDecoded: number;
  totalFailed: number;
}

// ============================================================
// VIN DECODE FUNCTIONS
// ============================================================

/**
 * Smart VIN decode with catalog matching and duplicate detection
 */
export async function decodeVinSmart(vin: string): Promise<SmartVinDecodeResult> {
  const response = await apiClient.get<SmartVinDecodeResult>(
    `/api/catalog/vin/${encodeURIComponent(vin)}/decode-smart`
  );
  return response.data;
}

/**
 * Check if a VIN already exists (fast endpoint for real-time validation)
 */
export async function checkVinExists(vin: string): Promise<VinExistsResponse> {
  const response = await apiClient.get<VinExistsResponse>(
    `/api/vehicles/vin/${encodeURIComponent(vin)}/exists`
  );
  return response.data;
}

/**
 * Batch VIN decode for dealers (max 50 VINs)
 */
export async function decodeVinBatch(vins: string[]): Promise<BatchVinDecodeResponse> {
  const response = await apiClient.post<BatchVinDecodeResponse>('/api/catalog/vin/decode-batch', {
    vins,
    maxItems: 50,
  });
  return response.data;
}

/**
 * Generate a vehicle description from specs (client-side template)
 */
export function generateVehicleDescription(specs: {
  year?: number;
  make?: string;
  model?: string;
  trim?: string;
  engineSize?: string;
  cylinders?: number;
  horsepower?: number;
  fuelType?: string;
  transmission?: string;
  driveType?: string;
  mileage?: number;
  condition?: string;
  province?: string;
}): string {
  const parts: string[] = [];

  // Title
  const titleParts = [specs.year, specs.make, specs.model, specs.trim].filter(Boolean);
  if (titleParts.length > 0) parts.push(titleParts.join(' '));

  // Condition
  if (specs.condition) {
    const conditionLabels: Record<string, string> = {
      new: 'nuevo',
      used: 'usado',
      certified: 'certificado pre-owned',
    };
    parts.push(`en condición ${conditionLabels[specs.condition] || specs.condition}.`);
  }

  // Engine
  const engineParts: string[] = [];
  if (specs.engineSize) engineParts.push(`Motor ${specs.engineSize}`);
  if (specs.cylinders) engineParts.push(`${specs.cylinders} cilindros`);
  if (specs.horsepower) engineParts.push(`${specs.horsepower} HP`);
  if (engineParts.length > 0) parts.push(engineParts.join(', ') + '.');

  // Transmission & drive
  const transParts: string[] = [];
  const transmissionLabels: Record<string, string> = {
    automatic: 'automática',
    manual: 'manual',
    cvt: 'CVT',
    dct: 'doble embrague',
  };
  const driveLabels: Record<string, string> = {
    fwd: 'delantera (FWD)',
    rwd: 'trasera (RWD)',
    awd: 'total (AWD)',
    '4wd': '4x4',
  };
  if (specs.transmission)
    transParts.push(
      `Transmisión ${transmissionLabels[specs.transmission.toLowerCase()] || specs.transmission}`
    );
  if (specs.driveType)
    transParts.push(`tracción ${driveLabels[specs.driveType.toLowerCase()] || specs.driveType}`);
  if (transParts.length > 0) parts.push(transParts.join(', ') + '.');

  // Mileage
  if (specs.mileage) parts.push(`${specs.mileage.toLocaleString('es-DO')} km recorridos.`);

  // Fuel
  const fuelLabels: Record<string, string> = {
    gasoline: 'gasolina',
    diesel: 'diésel',
    hybrid: 'híbrido',
    electric: 'eléctrico',
  };
  if (specs.fuelType)
    parts.push(`Combustible: ${fuelLabels[specs.fuelType.toLowerCase()] || specs.fuelType}.`);

  // Location
  if (specs.province) {
    parts.push(`Ubicado en ${specs.province}, República Dominicana.`);
  } else {
    parts.push('Ubicado en República Dominicana.');
  }

  return parts.join(' ');
}

// ============================================================
// CREATE VEHICLE TYPES
// ============================================================

export interface CreateVehicleRequest {
  make: string;
  model: string;
  year: number;
  trim?: string;
  mileage: number;
  mileageUnit?: 'km' | 'mi'; // FIX B1: Send unit to backend (maps to Kilometers/Miles enum)
  vin?: string;
  transmission: string;
  fuelType: string;
  bodyType: string;
  exteriorColor?: string;
  interiorColor?: string;
  condition: string;
  price: number;
  currency?: 'DOP' | 'USD';
  description?: string;
  features: string[];
  images: CreateVehicleImage[];
  city: string;
  province: string;
  sellerName?: string;
  sellerPhone?: string;
  sellerEmail?: string;
  sellerWhatsApp?: string;
  isNegotiable?: boolean;
  /** The authenticated user's ID — links the listing to the seller's account */
  sellerId?: string;
}

export interface CreateVehicleImage {
  url: string;
  order: number;
  isPrimary?: boolean;
  alt?: string;
}

export interface CreateVehicleResponse {
  id: string;
  slug: string;
  status: string;
  message: string;
}

/**
 * Photo moderation result returned from publish endpoint.
 */
export interface PhotoModerationDto {
  imageId: string;
  status: string;
  flags: string[];
  rejectionReason?: string;
  dealerMessage?: string;
}

/**
 * Response from the publish endpoint with extended moderation data.
 */
export interface PublishVehicleResponse {
  id: string;
  status: string;
  publishedAt?: string;
  expiresAt?: string;
  message: string;
  vinDiscrepancies?: Array<{
    field: string;
    declaredValue: string;
    vinValue: string;
    severity: string;
  }>;
  photoModeration?: PhotoModerationDto[];
}

/**
 * Update vehicle request - extends CreateVehicleRequest with status management
 */
export interface UpdateVehicleRequest extends Partial<CreateVehicleRequest> {
  status?:
    | 'draft'
    | 'pending'
    | 'active'
    | 'paused'
    | 'sold'
    | 'reserved'
    | 'expired'
    | 'rejected'
    | 'pending_media';
}

// ============================================================
// CREATE VEHICLE FUNCTION
// ============================================================

// ── Enum mappings: frontend/catalog values → backend enum names ──
// The catalog API and static fallbacks may use lowercase/snake_case values
// but the backend expects PascalCase enum names for deserialization.
const CONDITION_TO_ENUM: Record<string, string> = {
  new: 'New',
  'like-new': 'CertifiedPreOwned',
  excellent: 'Used',
  good: 'Used',
  fair: 'Used',
  used: 'Used',
  salvage: 'Salvage',
  rebuilt: 'Rebuilt',
  certifiedpreowned: 'CertifiedPreOwned',
};
const FUEL_TYPE_TO_ENUM: Record<string, string> = {
  gasoline: 'Gasoline',
  diesel: 'Diesel',
  hybrid: 'Hybrid',
  electric: 'Electric',
  plugin_hybrid: 'PlugInHybrid',
  pluginhybrid: 'PlugInHybrid',
  lpg: 'NaturalGas',
  naturalgas: 'NaturalGas',
  flex_fuel: 'FlexFuel',
  flexfuel: 'FlexFuel',
  hydrogen: 'Hydrogen',
};
const TRANSMISSION_TO_ENUM: Record<string, string> = {
  automatic: 'Automatic',
  manual: 'Manual',
  cvt: 'CVT',
  dct: 'DualClutch',
  dualclutch: 'DualClutch',
  'semi-automatic': 'Automated',
  automated: 'Automated',
};
/** Resolve a frontend/catalog value to its backend enum name */
function toEnum(value: string, map: Record<string, string>): string {
  return map[value.toLowerCase()] ?? value;
}

/**
 * Create a new vehicle listing (creates as Draft).
 * Maps frontend field names to backend expected names.
 */
export async function createVehicle(data: CreateVehicleRequest): Promise<CreateVehicleResponse> {
  // Auto-generate title if not set (backend may require it for publish)
  const titleParts = [data.year, data.make, data.model, data.trim].filter(Boolean);
  const title = titleParts.join(' ');

  // Map images: send as imageObjects (array of {url, sortOrder, isPrimary})
  const imageObjects = data.images
    .filter(img => img.url)
    .map(img => ({
      url: img.url,
      thumbnailUrl: undefined as string | undefined,
      sortOrder: img.order,
      isPrimary: img.isPrimary ?? false,
      alt: img.alt,
    }));

  // Map features array to JSON string (backend FeaturesJson field)
  const featuresJson = data.features?.length ? JSON.stringify(data.features) : undefined;

  const payload = {
    // Required fields
    title,
    make: data.make,
    model: data.model,
    year: data.year,
    trim: data.trim,
    mileage: data.mileage ?? 0,
    mileageUnit: data.mileageUnit === 'mi' ? 'Miles' : 'Kilometers', // FIX B1: Map frontend 'km'/'mi' → backend enum
    vin: data.vin,
    // Enums — map to PascalCase backend enum names
    transmission: toEnum(data.transmission, TRANSMISSION_TO_ENUM),
    fuelType: toEnum(data.fuelType, FUEL_TYPE_TO_ENUM),
    bodyType: data.bodyType, // backend alias for BodyStyle (has its own mapper)
    condition: toEnum(data.condition, CONDITION_TO_ENUM),
    // Pricing
    price: data.price,
    currency: data.currency ?? 'DOP',
    isNegotiable: data.isNegotiable ?? false,
    // Description + features
    description: data.description,
    features: data.features ?? [], // backend Features alias for FeaturesJson
    featuresJson, // redundant but safe
    // Location — backend uses State field; province is an alias we added
    province: data.province,
    state: data.province,
    city: data.city,
    country: 'DO',
    // Images as objects
    imageObjects,
    // Seller identity + contact
    sellerId: data.sellerId,
    sellerName: data.sellerName,
    sellerPhone: data.sellerPhone,
    sellerEmail: data.sellerEmail,
    sellerWhatsApp: data.sellerWhatsApp,
    // Appearance
    exteriorColor: data.exteriorColor,
    interiorColor: data.interiorColor,
  };

  const response = await apiClient.post<CreateVehicleResponse>('/api/vehicles', payload);
  return response.data;
}

/**
 * Publish a vehicle (change status from Draft → PendingReview).
 * Must be called after createVehicle once all required fields are filled.
 * The vehicle will be reviewed by staff before becoming visible.
 */
export async function publishVehicle(
  id: string,
  body?: { disclaimerAccepted: boolean; tosVersion?: string }
): Promise<PublishVehicleResponse> {
  const response = await apiClient.post<PublishVehicleResponse>(
    `/api/vehicles/${id}/publish`,
    body ?? { disclaimerAccepted: true }
  );
  return response.data;
}

/**
 * Unpublish/pause a vehicle (Active → Archived)
 */
export async function unpublishVehicle(id: string, reason?: string): Promise<void> {
  await apiClient.post(`/api/vehicles/${id}/unpublish`, { reason });
}

/**
 * Mark a vehicle as sold
 */
export async function markVehicleSold(id: string, soldPrice?: number): Promise<void> {
  await apiClient.post(`/api/vehicles/${id}/sold`, { soldPrice });
}

/**
 * Update an existing vehicle
 */
export async function updateVehicle(id: string, data: UpdateVehicleRequest): Promise<Vehicle> {
  // Apply same enum mappings as createVehicle for backend compatibility
  const mapped: Record<string, unknown> = { ...data };
  if (data.fuelType) mapped.fuelType = toEnum(data.fuelType, FUEL_TYPE_TO_ENUM);
  if (data.transmission) mapped.transmission = toEnum(data.transmission, TRANSMISSION_TO_ENUM);
  if (data.condition) mapped.condition = toEnum(data.condition, CONDITION_TO_ENUM);
  const response = await apiClient.put<VehicleDto>(`/api/vehicles/${id}`, mapped);
  return transformVehicle(response.data);
}

/**
 * Delete a vehicle
 */
export async function deleteVehicle(id: string): Promise<void> {
  await apiClient.delete(`/api/vehicles/${id}`);
}

// ============================================================
// CATALOG TYPES
// ============================================================

export interface VehicleMake {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  modelsCount?: number;
}

export interface VehicleModel {
  id: string;
  name: string;
  slug: string;
  makeId: string;
  years?: number[];
}

export interface CatalogOption {
  value: string;
  label: string;
}

/**
 * Normalize a raw catalog item from the API to { value, label }.
 *
 * The backend CatalogOptionDto previously used { id, name } property names.
 * It was corrected to { value, label }, but this helper provides a safety net
 * for cached responses or any future schema drift.
 */
function normalizeCatalogOption(item: unknown): CatalogOption {
  const obj = item as Record<string, unknown>;
  return {
    value: ((obj.value ?? obj.id ?? '') as string).toLowerCase(),
    label: (obj.label ?? obj.name ?? '') as string,
  };
}

// ============================================================
// CATALOG FUNCTIONS
// ============================================================

/**
 * Get all vehicle makes
 */
export async function getMakes(): Promise<VehicleMake[]> {
  try {
    const response = await apiClient.get<VehicleMake[]>('/api/catalog/makes');
    const data = response.data;
    // Fallback if API returns empty array (catalog not seeded yet)
    if (!Array.isArray(data) || data.length === 0) return getStaticMakes();
    return data;
  } catch {
    // Fallback to static list if API fails
    return getStaticMakes();
  }
}

/**
 * Get models for a specific make
 */
export async function getModelsByMake(makeId: string): Promise<VehicleModel[]> {
  try {
    const response = await apiClient.get<VehicleModel[]>(`/api/catalog/makes/${makeId}/models`);
    const data = response.data;
    // Fallback if API returns empty array (catalog not seeded yet)
    if (!Array.isArray(data) || data.length === 0) return getStaticModelsByMake(makeId);
    return data;
  } catch {
    return getStaticModelsByMake(makeId);
  }
}

/**
 * Get body types
 */
export async function getBodyTypes(): Promise<CatalogOption[]> {
  try {
    const response = await apiClient.get<unknown[]>('/api/catalog/body-types');
    const data = response.data;
    if (!Array.isArray(data) || data.length === 0) return getStaticBodyTypes();
    // Normalize: handle both { value, label } and legacy { id, name } formats
    const normalized = data.map(normalizeCatalogOption).filter(o => !!o.value);
    return normalized.length > 0 ? normalized : getStaticBodyTypes();
  } catch {
    return getStaticBodyTypes();
  }
}

/**
 * Get fuel types
 */
export async function getFuelTypes(): Promise<CatalogOption[]> {
  try {
    const response = await apiClient.get<unknown[]>('/api/catalog/fuel-types');
    const data = response.data;
    if (!Array.isArray(data) || data.length === 0) return getStaticFuelTypes();
    const normalized = data.map(normalizeCatalogOption).filter(o => !!o.value);
    return normalized.length > 0 ? normalized : getStaticFuelTypes();
  } catch {
    return getStaticFuelTypes();
  }
}

/**
 * Get transmissions
 */
export async function getTransmissions(): Promise<CatalogOption[]> {
  try {
    const response = await apiClient.get<unknown[]>('/api/catalog/transmissions');
    const data = response.data;
    if (!Array.isArray(data) || data.length === 0) return getStaticTransmissions();
    const normalized = data.map(normalizeCatalogOption).filter(o => !!o.value);
    return normalized.length > 0 ? normalized : getStaticTransmissions();
  } catch {
    return getStaticTransmissions();
  }
}

/**
 * Get colors
 */
export async function getColors(): Promise<CatalogOption[]> {
  try {
    const response = await apiClient.get<unknown[]>('/api/catalog/colors');
    const data = response.data;
    if (!Array.isArray(data) || data.length === 0) return getStaticColors();
    const normalized = data.map(normalizeCatalogOption).filter(o => !!o.value);
    return normalized.length > 0 ? normalized : getStaticColors();
  } catch {
    return getStaticColors();
  }
}

/**
 * Get provinces (locations)
 */
export async function getProvinces(): Promise<CatalogOption[]> {
  try {
    const response = await apiClient.get<unknown[]>('/api/catalog/provinces');
    const data = response.data;
    if (!Array.isArray(data) || data.length === 0) return getStaticProvinces();
    const normalized = data.map(normalizeCatalogOption).filter(o => !!o.value);
    return normalized.length > 0 ? normalized : getStaticProvinces();
  } catch {
    return getStaticProvinces();
  }
}

/**
 * Get features by category
 */
export async function getFeatures(): Promise<Record<string, string[]>> {
  try {
    const response = await apiClient.get<Record<string, string[]>>('/api/catalog/features');
    return response.data;
  } catch {
    return getStaticFeatures();
  }
}

// ============================================================
// STATIC FALLBACK DATA
// ============================================================

const STATIC_MODELS_BY_MAKE: Record<string, string[]> = {
  toyota: [
    'Corolla',
    'Camry',
    'RAV4',
    'Hilux',
    'Fortuner',
    'Land Cruiser',
    'Prado',
    'Yaris',
    'C-HR',
    'Highlander',
    'Tacoma',
    'Tundra',
    '4Runner',
    'Sequoia',
  ],
  honda: [
    'Civic',
    'Accord',
    'CR-V',
    'HR-V',
    'Pilot',
    'Odyssey',
    'Passport',
    'Ridgeline',
    'Fit',
    'Jazz',
  ],
  hyundai: [
    'Elantra',
    'Sonata',
    'Tucson',
    'Santa Fe',
    'Accent',
    'Kona',
    'Palisade',
    'Creta',
    'Ioniq 5',
    'Genesis G80',
  ],
  nissan: [
    'Sentra',
    'Altima',
    'Maxima',
    'Pathfinder',
    'Frontier',
    'Titan',
    'Rogue',
    'Murano',
    'Armada',
    'X-Trail',
    'Kicks',
    'Versa',
  ],
  kia: [
    'Rio',
    'Forte',
    'Stinger',
    'Sportage',
    'Sorento',
    'Telluride',
    'Seltos',
    'Soul',
    'Carnival',
    'Stonic',
  ],
  mazda: ['Mazda3', 'Mazda6', 'CX-3', 'CX-5', 'CX-9', 'CX-30', 'MX-5 Miata', 'BT-50'],
  ford: [
    'Mustang',
    'F-150',
    'Explorer',
    'Escape',
    'Bronco',
    'Edge',
    'Expedition',
    'EcoSport',
    'Ranger',
    'Maverick',
  ],
  chevrolet: [
    'Spark',
    'Cruze',
    'Malibu',
    'Camaro',
    'Silverado',
    'Colorado',
    'Equinox',
    'Blazer',
    'Traverse',
    'Tahoe',
    'Suburban',
    'Trailblazer',
  ],
  bmw: ['Serie 3', 'Serie 5', 'Serie 7', 'X1', 'X3', 'X5', 'X7', 'M3', 'M5'],
  'mercedes-benz': ['Clase C', 'Clase E', 'Clase S', 'GLA', 'GLC', 'GLE', 'GLS', 'CLA', 'AMG GT'],
  audi: ['A3', 'A4', 'A6', 'A8', 'Q3', 'Q5', 'Q7', 'TT', 'R8'],
  volkswagen: ['Jetta', 'Passat', 'Golf', 'Tiguan', 'Atlas', 'Taos', 'Polo', 'T-Cross'],
  lexus: ['IS', 'ES', 'GS', 'LS', 'NX', 'RX', 'GX', 'LX'],
  jeep: ['Wrangler', 'Cherokee', 'Grand Cherokee', 'Compass', 'Renegade', 'Gladiator', 'Commander'],
  mitsubishi: ['Lancer', 'Galant', 'Eclipse Cross', 'Outlander', 'ASX', 'L200', 'Montero'],
  suzuki: ['Swift', 'Vitara', 'S-Cross', 'Grand Vitara', 'Jimny', 'Ertiga'],
  subaru: ['Impreza', 'Legacy', 'Outback', 'Forester', 'Crosstrek', 'WRX', 'BRZ'],
  dodge: ['Charger', 'Challenger', 'Durango', 'Ram 1500', 'Journey', 'Dart'],
};

function getStaticModelsByMake(makeId: string): VehicleModel[] {
  // makeId can be "make-1", "Toyota", "toyota", etc.
  const normalizedKey = makeId
    .toLowerCase()
    .replace(/^make-\d+$/, '') // strip "make-N" IDs
    .replace(/\s+/g, '-');

  // Try direct match first, then partial match
  const models =
    STATIC_MODELS_BY_MAKE[normalizedKey] ??
    Object.entries(STATIC_MODELS_BY_MAKE).find(
      ([key]) => key.includes(normalizedKey) || normalizedKey.includes(key)
    )?.[1] ??
    [];

  return models.map((name, i) => ({
    id: `model-${normalizedKey}-${i + 1}`,
    name,
    slug: name.toLowerCase().replace(/\s+/g, '-'),
    makeId,
  }));
}

function getStaticMakes(): VehicleMake[] {
  const makes = [
    'Toyota',
    'Honda',
    'Hyundai',
    'Nissan',
    'Kia',
    'Mazda',
    'Ford',
    'Chevrolet',
    'BMW',
    'Mercedes-Benz',
    'Audi',
    'Volkswagen',
    'Lexus',
    'Jeep',
    'Mitsubishi',
    'Suzuki',
    'Subaru',
    'Dodge',
  ];
  return makes.map((name, i) => ({
    id: `make-${i + 1}`,
    name,
    slug: name.toLowerCase().replace(/\s+/g, '-'),
  }));
}

function getStaticBodyTypes(): CatalogOption[] {
  return [
    { value: 'sedan', label: 'Sedán' },
    { value: 'suv', label: 'SUV' },
    { value: 'pickup', label: 'Pickup' },
    { value: 'hatchback', label: 'Hatchback' },
    { value: 'coupe', label: 'Coupé' },
    { value: 'minivan', label: 'Minivan' },
    { value: 'crossover', label: 'Crossover' },
    { value: 'wagon', label: 'Wagon' },
    { value: 'convertible', label: 'Convertible' },
  ];
}

function getStaticFuelTypes(): CatalogOption[] {
  return [
    { value: 'gasoline', label: 'Gasolina' },
    { value: 'diesel', label: 'Diésel' },
    { value: 'hybrid', label: 'Híbrido' },
    { value: 'electric', label: 'Eléctrico' },
    { value: 'pluginhybrid', label: 'Híbrido Enchufable' },
    { value: 'naturalgas', label: 'GLP / Gas' },
    { value: 'flexfuel', label: 'Flex Fuel' },
  ];
}

function getStaticTransmissions(): CatalogOption[] {
  return [
    { value: 'automatic', label: 'Automática' },
    { value: 'manual', label: 'Manual' },
    { value: 'cvt', label: 'CVT' },
    { value: 'dualclutch', label: 'Doble Embrague (DCT)' },
    { value: 'automated', label: 'Semi-automática' },
  ];
}

function getStaticColors(): CatalogOption[] {
  return [
    { value: 'white', label: 'Blanco' },
    { value: 'black', label: 'Negro' },
    { value: 'gray', label: 'Gris' },
    { value: 'silver', label: 'Plata' },
    { value: 'red', label: 'Rojo' },
    { value: 'blue', label: 'Azul' },
    { value: 'green', label: 'Verde' },
    { value: 'brown', label: 'Marrón' },
    { value: 'beige', label: 'Beige' },
    { value: 'gold', label: 'Dorado' },
  ];
}

function getStaticProvinces(): CatalogOption[] {
  return [
    { value: 'santo-domingo', label: 'Santo Domingo' },
    { value: 'distrito-nacional', label: 'Distrito Nacional' },
    { value: 'santiago', label: 'Santiago' },
    { value: 'la-vega', label: 'La Vega' },
    { value: 'san-cristobal', label: 'San Cristóbal' },
    { value: 'puerto-plata', label: 'Puerto Plata' },
    { value: 'samana', label: 'Samaná' },
    { value: 'la-romana', label: 'La Romana' },
    { value: 'san-pedro', label: 'San Pedro de Macorís' },
    { value: 'punta-cana', label: 'Punta Cana' },
  ];
}

function getStaticFeatures(): Record<string, string[]> {
  return {
    seguridad: [
      'ABS',
      'Airbags',
      'Cámara de reversa',
      'Sensores de estacionamiento',
      'Control de estabilidad',
      'Alarma',
      'Bloqueo central',
    ],
    confort: [
      'Aire acondicionado',
      'Asientos de cuero',
      'Asientos calefactados',
      'Sunroof',
      'Control crucero',
      'Sensor de lluvia',
      'Techo panorámico',
    ],
    entretenimiento: [
      'Bluetooth',
      'Apple CarPlay',
      'Android Auto',
      'Sistema de sonido premium',
      'Pantalla táctil',
      'Navegación GPS',
      'Cargador inalámbrico',
    ],
    rendimiento: [
      'Modo Sport',
      'Paletas de cambio',
      'Turbo',
      'Tracción 4x4',
      'Suspensión deportiva',
      'Control de tracción',
    ],
  };
}

/**
 * Vehicle service object
 */
export const vehicleService = {
  // Read operations
  getBySlug: getVehicleBySlug,
  getById: getVehicleById,
  search: searchVehicles,
  getSimilar: getSimilarVehicles,
  getFeatured: getFeaturedVehicles,
  trackView: trackVehicleView,
  getByDealer: getVehiclesByDealer,
  getByIds: getVehiclesByIds,
  get360View: getVehicle360View,

  // Write operations
  create: createVehicle,
  publish: publishVehicle,
  unpublish: unpublishVehicle,
  markSold: markVehicleSold,
  update: updateVehicle,
  delete: deleteVehicle,

  // VIN operations
  decodeVinSmart,
  checkVinExists,
  decodeVinBatch,
  generateDescription: generateVehicleDescription,

  // Catalog operations
  getMakes,
  getModelsByMake,
  getBodyTypes,
  getFuelTypes,
  getTransmissions,
  getColors,
  getProvinces,
  getFeatures,
};

export default vehicleService;
