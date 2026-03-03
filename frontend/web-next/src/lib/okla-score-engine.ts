// =============================================================================
// OKLA Score™ — Calculation Engine (MVP)
// =============================================================================
// Calculates the OKLA Score (0–1,000) from 7 weighted dimensions.
// This is the client-side MVP engine. Production will run on backend with
// real API integrations (VinAudit, NHTSA, MarketCheck, etc.)
// =============================================================================

import {
  DIMENSION_CONFIG,
  type DimensionScore,
  type OklaScoreReport,
  type OklaScoreLevel,
  type ScoreFactor,
  type VinDecodeResult,
  type VinHistoryReport,
  type NhtsaSafetyRating,
  type NhtsaRecall,
  type PriceAnalysis,
  type PriceVerdict,
  type ScoreAlert,
  type TitleType,
  getScoreLevel,
} from '@/types/okla-score';

// =============================================================================
// INPUT TYPES
// =============================================================================

export interface ScoreInput {
  vin: string;
  vinDecode: VinDecodeResult;
  history?: VinHistoryReport;
  listedPriceDOP: number;
  marketPriceDOP?: number;
  marketPriceUSD?: number;
  declaredMileage: number;
  mileageUnit: 'miles' | 'km';
  safetyRating?: NhtsaSafetyRating;
  recalls?: NhtsaRecall[];
  sellerType: 'dealer' | 'individual';
  sellerScore?: number; // 0–1 internal reputation
  sellerDisputes?: number;
  exchangeRate?: number; // USD → DOP
}

// =============================================================================
// D1 — VIN HISTORY (25%, max 250 pts)
// =============================================================================

function calculateD1(history?: VinHistoryReport): DimensionScore {
  const factors: ScoreFactor[] = [];
  let raw = 100; // base score for clean title

  if (!history) {
    factors.push({
      name: 'No history available',
      nameEs: 'Sin historial disponible',
      impact: 0,
      description: 'VIN history report not available',
      descriptionEs: 'Reporte de historial VIN no disponible',
      source: 'N/A',
    });
    return buildDimension('D1', 100, factors);
  }

  // Title type
  const titlePenalties: Record<TitleType, number> = {
    Clean: 0,
    Salvage: -200,
    Rebuilt: -120,
    Flood: -180,
    Junk: -250,
    Unknown: -50,
  };
  const titlePenalty = titlePenalties[history.titleType] || 0;
  if (titlePenalty !== 0) {
    raw += titlePenalty;
    factors.push({
      name: `Title: ${history.titleType}`,
      nameEs: `Título: ${history.titleType}`,
      impact: titlePenalty,
      description: `Vehicle has a ${history.titleType} title`,
      descriptionEs: `El vehículo tiene título ${history.titleType}`,
      source: 'VinAudit/NMVTIS',
    });
  }

  // Flood damage
  if (history.hasFloodDamage) {
    raw -= 180;
    factors.push({
      name: 'Flood Damage',
      nameEs: 'Daño por inundación',
      impact: -180,
      description: 'Severe internal corrosion risk',
      descriptionEs: 'Riesgo severo de corrosión interna',
      source: 'VinAudit',
    });
  }

  // Frame damage
  if (history.hasFrameDamage) {
    raw -= 150;
    factors.push({
      name: 'Frame Damage',
      nameEs: 'Daño estructural',
      impact: -150,
      description: 'Structural integrity compromised',
      descriptionEs: 'Integridad estructural comprometida',
      source: 'VinAudit',
    });
  }

  // Hail damage
  if (history.hasHailDamage) {
    raw -= 60;
    factors.push({
      name: 'Hail Damage',
      nameEs: 'Daño de granizo',
      impact: -60,
      description: 'Cosmetic damage, significant depreciation',
      descriptionEs: 'Daño cosmético, depreciación significativa',
      source: 'VinAudit',
    });
  }

  // Lemon buyback
  if (history.isLemonBuyback) {
    raw -= 180;
    factors.push({
      name: 'Lemon Buyback',
      nameEs: 'Lemon Law Buyback',
      impact: -180,
      description: 'Manufacturer repurchased due to chronic defects',
      descriptionEs: 'El fabricante recompró por defectos crónicos',
      source: 'VinAudit',
    });
  }

  // Stolen/cloned
  if (history.isStolenOrCloned) {
    raw = -250; // Block
    factors.push({
      name: 'Stolen/Cloned VIN',
      nameEs: 'VIN clonado/robado',
      impact: -250,
      description: 'BLOCKED: Vehicle cannot be listed',
      descriptionEs: 'BLOQUEADO: El vehículo no puede ser listado',
      source: 'NMVTIS',
    });
  }

  // Accidents
  if (history.accidentCount === 0) {
    raw += 50;
    factors.push({
      name: 'No Accidents',
      nameEs: 'Sin accidentes',
      impact: 50,
      description: 'Clean accident history',
      descriptionEs: 'Historial de accidentes limpio',
      source: 'VinAudit',
    });
  } else if (history.accidentSeverity === 'Minor') {
    raw -= 30;
    factors.push({
      name: 'Minor Accidents',
      nameEs: 'Accidentes menores',
      impact: -30,
      description: `${history.accidentCount} minor accident(s)`,
      descriptionEs: `${history.accidentCount} accidente(s) menores`,
      source: 'VinAudit',
    });
  } else if (history.accidentSeverity === 'Moderate') {
    raw -= 70;
    factors.push({
      name: 'Moderate Accidents',
      nameEs: 'Accidentes moderados',
      impact: -70,
      description: 'Body/suspension damage reported',
      descriptionEs: 'Daño a carrocería/suspensión reportado',
      source: 'VinAudit',
    });
  }

  // Rental/fleet
  if (history.isRentalFleet) {
    raw -= 40;
    factors.push({
      name: 'Rental/Fleet',
      nameEs: 'Alquiler/Flota',
      impact: -40,
      description: 'Heavy usage, variable maintenance',
      descriptionEs: 'Uso intensivo, mantenimiento variable',
      source: 'VinAudit',
    });
  }

  // Single owner bonus
  if (history.totalOwners === 1) {
    raw += 20;
    factors.push({
      name: 'Single Owner',
      nameEs: 'Un solo propietario',
      impact: 20,
      description: 'More consistent care',
      descriptionEs: 'Cuidado más consistente',
      source: 'VinAudit',
    });
  }

  // Service records
  if (history.serviceRecords > 5) {
    raw += 30;
    factors.push({
      name: 'Verified Maintenance',
      nameEs: 'Mantenimiento verificado',
      impact: 30,
      description: 'Service history at authorized dealers',
      descriptionEs: 'Historial de servicio en talleres autorizados',
      source: 'VinAudit',
    });
  }

  return buildDimension('D1', Math.max(0, Math.min(250, raw)), factors);
}

// =============================================================================
// D2 — MECHANICAL CONDITION (20%, max 200 pts)
// =============================================================================

function calculateD2(vinDecode: VinDecodeResult): DimensionScore {
  const factors: ScoreFactor[] = [];
  let raw = 100; // base

  // Engine type bonuses
  if (
    vinDecode.engineType?.toLowerCase().includes('hybrid') ||
    vinDecode.engineType?.toLowerCase().includes('electric')
  ) {
    raw += 40;
    factors.push({
      name: 'Hybrid/Electric',
      nameEs: 'Híbrido/Eléctrico',
      impact: 40,
      description: 'Modern powertrain',
      descriptionEs: 'Tren motriz moderno',
      source: 'NHTSA vPIC',
    });
  } else if (vinDecode.engineType?.toLowerCase().includes('turbo')) {
    raw += 20;
    factors.push({
      name: 'Turbo Engine',
      nameEs: 'Motor Turbo',
      impact: 20,
      description: 'Performance engine',
      descriptionEs: 'Motor de rendimiento',
      source: 'NHTSA vPIC',
    });
  }

  // Cylinders (higher = more power = higher base)
  if (vinDecode.engineCylinders && vinDecode.engineCylinders >= 6) {
    raw += 20;
  }

  // Drivetrain
  if (vinDecode.drivetrain === 'AWD' || vinDecode.drivetrain === '4WD') {
    raw += 25;
    factors.push({
      name: 'AWD/4WD',
      nameEs: 'AWD/4WD',
      impact: 25,
      description: 'All-wheel drive',
      descriptionEs: 'Tracción total',
      source: 'NHTSA vPIC',
    });
  }

  // Transmission
  if (vinDecode.transmission?.toLowerCase().includes('auto')) {
    raw += 15;
  }

  return buildDimension('D2', Math.min(200, raw), factors);
}

// =============================================================================
// D3 — MILEAGE / ODOMETER (18%, max 180 pts)
// =============================================================================

function calculateD3(
  declaredMileage: number,
  unit: 'miles' | 'km',
  history?: VinHistoryReport
): DimensionScore {
  const factors: ScoreFactor[] = [];
  const miles = unit === 'km' ? declaredMileage / 1.60934 : declaredMileage;

  // Score based on mileage
  let raw: number;
  if (miles <= 30000) {
    raw = 180;
  } else if (miles <= 60000) {
    raw = 140;
  } else if (miles <= 90000) {
    raw = 100;
  } else if (miles <= 120000) {
    raw = 60;
  } else if (miles <= 150000) {
    raw = 30;
  } else {
    raw = 10;
  }

  factors.push({
    name: `Mileage: ${Math.round(miles).toLocaleString()} mi`,
    nameEs: `Kilometraje: ${Math.round(miles * 1.60934).toLocaleString()} km`,
    impact: raw - 90, // relative to "moderate" baseline
    description: `${Math.round(miles).toLocaleString()} miles reported`,
    descriptionEs: `${Math.round(miles * 1.60934).toLocaleString()} km reportados`,
    source: 'Seller declaration',
  });

  // Odometer fraud check
  if (history?.lastReportedMileage) {
    const lastMiles = history.lastReportedMileage;
    const discrepancy = Math.abs(miles - lastMiles) / Math.max(lastMiles, 1);
    if (discrepancy > 0.2) {
      raw = 0; // fraud detected
      factors.push({
        name: 'ODOMETER FRAUD',
        nameEs: 'FRAUDE DE ODÓMETRO',
        impact: -180,
        description: `Declared ${Math.round(miles)} mi vs historical ${Math.round(lastMiles)} mi (${(discrepancy * 100).toFixed(0)}% discrepancy)`,
        descriptionEs: `Declarado ${Math.round(miles)} mi vs histórico ${Math.round(lastMiles)} mi (${(discrepancy * 100).toFixed(0)}% discrepancia)`,
        source: 'VinAudit/NMVTIS',
      });
    }
  }

  return buildDimension('D3', Math.max(0, raw), factors);
}

// =============================================================================
// D4 — PRICE VS MARKET (17%, max 170 pts)
// =============================================================================

function calculateD4(listedPriceDOP: number, marketPriceDOP?: number): DimensionScore {
  const factors: ScoreFactor[] = [];

  if (!marketPriceDOP || marketPriceDOP <= 0) {
    return buildDimension('D4', 85, [
      {
        // neutral when no market data
        name: 'No market data',
        nameEs: 'Sin datos de mercado',
        impact: 0,
        description: 'Market price comparison not available',
        descriptionEs: 'Comparación de precio de mercado no disponible',
      },
    ]);
  }

  const diff = ((listedPriceDOP - marketPriceDOP) / marketPriceDOP) * 100;
  let raw: number;

  if (diff <= -15) {
    raw = 170;
  } else if (diff <= -5) {
    raw = 140;
  } else if (Math.abs(diff) <= 5) {
    raw = 110;
  } else if (diff <= 15) {
    raw = 60;
  } else if (diff <= 30) {
    raw = 20;
  } else {
    raw = 0;
  }

  const formatDOP = (n: number) =>
    new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(n);

  factors.push({
    name: `Price: ${diff > 0 ? '+' : ''}${diff.toFixed(1)}% vs market`,
    nameEs: `Precio: ${diff > 0 ? '+' : ''}${diff.toFixed(1)}% vs mercado`,
    impact: raw - 85,
    description: `Listed ${formatDOP(listedPriceDOP)} vs market avg ${formatDOP(marketPriceDOP)}`,
    descriptionEs: `Listado ${formatDOP(listedPriceDOP)} vs promedio ${formatDOP(marketPriceDOP)}`,
    source: 'OKLA Market Algorithm',
  });

  return buildDimension('D4', raw, factors);
}

// =============================================================================
// D5 — SAFETY & RECALLS (10%, max 100 pts)
// =============================================================================

function calculateD5(recalls?: NhtsaRecall[], safetyRating?: NhtsaSafetyRating): DimensionScore {
  const factors: ScoreFactor[] = [];
  const activeRecalls = recalls?.filter(r => !r.isResolved) || [];
  let raw: number;

  if (activeRecalls.length === 0) {
    raw = 100;
  } else if (activeRecalls.length === 1) {
    raw = 60;
  } else if (activeRecalls.length <= 3) {
    raw = 30;
  } else {
    raw = 0;
  }

  if (activeRecalls.length > 0) {
    factors.push({
      name: `${activeRecalls.length} Active Recalls`,
      nameEs: `${activeRecalls.length} Recall(s) Activos`,
      impact: raw - 100,
      description: activeRecalls.map(r => r.component).join(', '),
      descriptionEs: activeRecalls.map(r => r.component).join(', '),
      source: 'NHTSA',
    });
  }

  // Safety rating bonus
  if (safetyRating?.overallRating === 5) {
    raw += 20;
    factors.push({
      name: '5-Star Rating',
      nameEs: 'Calificación 5 Estrellas',
      impact: 20,
      description: 'NHTSA 5-star safety',
      descriptionEs: 'Seguridad 5 estrellas NHTSA',
      source: 'NHTSA',
    });
  } else if (safetyRating?.overallRating === 4) {
    raw += 10;
    factors.push({
      name: '4-Star Rating',
      nameEs: 'Calificación 4 Estrellas',
      impact: 10,
      description: 'NHTSA 4-star safety',
      descriptionEs: 'Seguridad 4 estrellas NHTSA',
      source: 'NHTSA',
    });
  }

  return buildDimension('D5', Math.min(100, raw), factors);
}

// =============================================================================
// D6 — DEPRECIATION & YEAR (6%, max 60 pts)
// =============================================================================

function calculateD6(year: number): DimensionScore {
  const currentYear = new Date().getFullYear();
  const age = currentYear - year;
  const factors: ScoreFactor[] = [];
  let raw: number;

  if (age <= 0) {
    raw = 60;
  } else if (age <= 2) {
    raw = 50;
  } else if (age <= 4) {
    raw = 40;
  } else if (age <= 6) {
    raw = 30;
  } else if (age <= 9) {
    raw = 20;
  } else if (age <= 12) {
    raw = 12;
  } else {
    raw = 5;
  }

  factors.push({
    name: `${year} Model (${age}y old)`,
    nameEs: `Modelo ${year} (${age} años)`,
    impact: raw - 30,
    description: `${age} years of depreciation`,
    descriptionEs: `${age} años de depreciación`,
    source: 'OKLA',
  });

  return buildDimension('D6', raw, factors);
}

// =============================================================================
// D7 — SELLER REPUTATION (4%, max 40 pts)
// =============================================================================

function calculateD7(
  sellerType: 'dealer' | 'individual',
  sellerScore?: number,
  sellerDisputes?: number
): DimensionScore {
  const factors: ScoreFactor[] = [];
  let raw: number;

  if (sellerScore !== undefined && sellerScore >= 0.9) {
    raw = 40;
    factors.push({
      name: 'OKLA Verified Dealer',
      nameEs: 'Dealer OKLA Verificado',
      impact: 40,
      description: 'Top-rated seller',
      descriptionEs: 'Vendedor con mejor calificación',
      source: 'OKLA',
    });
  } else if (sellerScore !== undefined && sellerScore >= 0.7) {
    raw = 25;
  } else if (sellerType === 'individual') {
    raw = 20;
    factors.push({
      name: 'Private Seller',
      nameEs: 'Vendedor privado',
      impact: 0,
      description: 'Individual verified seller',
      descriptionEs: 'Vendedor individual verificado',
      source: 'OKLA',
    });
  } else {
    raw = 15;
  }

  if (sellerDisputes && sellerDisputes > 0) {
    const penalty = Math.min(raw, sellerDisputes * 10);
    raw -= penalty;
    factors.push({
      name: `${sellerDisputes} Dispute(s)`,
      nameEs: `${sellerDisputes} Disputa(s)`,
      impact: -penalty,
      description: 'Active disputes reduce score',
      descriptionEs: 'Disputas activas reducen el score',
      source: 'OKLA',
    });
  }

  return buildDimension('D7', Math.max(0, raw), factors);
}

// =============================================================================
// BUILDER HELPER
// =============================================================================

function buildDimension(
  dim: keyof typeof DIMENSION_CONFIG,
  rawScore: number,
  factors: ScoreFactor[]
): DimensionScore {
  const config = DIMENSION_CONFIG[dim];
  const clampedRaw = Math.max(0, Math.min(config.maxPoints, rawScore));
  // Scale to 1000-point system: (raw / maxPoints) × weight% × 10
  const weightedScore = (clampedRaw / config.maxPoints) * config.weight * 10;

  return {
    dimension: dim,
    label: config.label,
    labelEs: config.labelEs,
    weight: config.weight,
    maxPoints: config.maxPoints,
    rawScore: clampedRaw,
    weightedScore: Math.round(weightedScore),
    factors,
  };
}

// =============================================================================
// MAIN CALCULATION
// =============================================================================

export function calculateOklaScore(input: ScoreInput): OklaScoreReport {
  // Calculate all 7 dimensions
  const d1 = calculateD1(input.history);
  const d2 = calculateD2(input.vinDecode);
  const d3 = calculateD3(input.declaredMileage, input.mileageUnit, input.history);
  const d4 = calculateD4(input.listedPriceDOP, input.marketPriceDOP);
  const d5 = calculateD5(input.recalls, input.safetyRating);
  const d6 = calculateD6(input.vinDecode.year);
  const d7 = calculateD7(input.sellerType, input.sellerScore, input.sellerDisputes);

  const dimensions = [d1, d2, d3, d4, d5, d6, d7];
  const totalScore = Math.round(dimensions.reduce((sum, d) => sum + d.weightedScore, 0));
  const clampedScore = Math.max(0, Math.min(1000, totalScore));
  const level = getScoreLevel(clampedScore);

  // Price analysis
  const rate = input.exchangeRate || 58.5;
  const fairPriceUSD =
    input.marketPriceUSD || (input.marketPriceDOP ? input.marketPriceDOP / rate : 0);
  const fairPriceDOP = input.marketPriceDOP || fairPriceUSD * rate;
  const priceDiff =
    fairPriceDOP > 0 ? ((input.listedPriceDOP - fairPriceDOP) / fairPriceDOP) * 100 : 0;

  let priceVerdict: PriceVerdict = 'fair_price';
  if (priceDiff <= -15) priceVerdict = 'excellent_deal';
  else if (priceDiff <= -5) priceVerdict = 'good_price';
  else if (Math.abs(priceDiff) <= 5) priceVerdict = 'fair_price';
  else if (priceDiff <= 15) priceVerdict = 'expensive';
  else if (priceDiff <= 30) priceVerdict = 'very_expensive';
  else priceVerdict = 'abusive_price';

  const priceAnalysis: PriceAnalysis = {
    listedPriceDOP: input.listedPriceDOP,
    fairPriceDOP,
    fairPriceUSD,
    priceDiffPercent: Math.round(priceDiff * 10) / 10,
    priceVerdict,
    exchangeRate: rate,
    sources: [],
  };

  // Build alerts
  const alerts: ScoreAlert[] = [];
  if (input.history?.isStolenOrCloned) {
    alerts.push({
      severity: 'critical',
      code: 'VIN_CLONED',
      title: 'Cloned/Stolen VIN',
      titleEs: 'VIN Clonado/Robado',
      description: 'This vehicle has a flagged VIN',
      descriptionEs: 'Este vehículo tiene un VIN marcado como robado o clonado',
      dimension: 'D1',
    });
  }
  if (input.history?.hasFloodDamage) {
    alerts.push({
      severity: 'critical',
      code: 'FLOOD_DAMAGE',
      title: 'Flood Damage',
      titleEs: 'Daño por Inundación',
      description: 'Vehicle has flood damage history',
      descriptionEs: 'El vehículo tiene historial de daño por inundación',
      dimension: 'D1',
    });
  }
  if (input.history?.hasFrameDamage) {
    alerts.push({
      severity: 'critical',
      code: 'FRAME_DAMAGE',
      title: 'Frame Damage',
      titleEs: 'Daño Estructural',
      description: 'Structural integrity compromised',
      descriptionEs: 'La integridad estructural está comprometida',
      dimension: 'D1',
    });
  }
  if (priceDiff > 30) {
    alerts.push({
      severity: 'warning',
      code: 'ABUSIVE_PRICE',
      title: 'Abusive Price',
      titleEs: 'Precio Abusivo',
      description: `Price is ${priceDiff.toFixed(0)}% above market`,
      descriptionEs: `Precio ${priceDiff.toFixed(0)}% por encima del mercado`,
      dimension: 'D4',
    });
  }

  const now = new Date();
  const expires = new Date(now);
  expires.setDate(expires.getDate() + 7);

  return {
    id: `okla-${input.vin}-${Date.now()}`,
    vin: input.vin,
    score: clampedScore,
    level: level.level as OklaScoreLevel,
    dimensions,
    priceAnalysis,
    alerts,
    vinDecode: input.vinDecode,
    safetyRating: input.safetyRating,
    recalls: input.recalls || [],
    generatedAt: now.toISOString(),
    expiresAt: expires.toISOString(),
    version: '1.0.0',
  };
}
