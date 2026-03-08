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
  type NhtsaComplaintSummary,
  type PriceAnalysis,
  type PriceVerdict,
  type ScoreAlert,
  type TitleType,
  getScoreLevel,
} from '@/types/okla-score';
import { DOP_USD_EXCHANGE_RATE } from '@/lib/constants';

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
  complaints?: NhtsaComplaintSummary;
  /** Known safety/ADAS features from Edmunds or VIN decode */
  safetyFeatures?: string[];
  sellerType: 'dealer' | 'individual';
  sellerScore?: number; // 0–1 internal reputation
  sellerDisputes?: number;
  exchangeRate?: number; // USD → DOP
}

// =============================================================================
// D1 — VIN HISTORY (25%, max 250 pts)
// =============================================================================

/** Spanish translations for US title types (shown to DR buyers) */
const TITLE_TYPE_ES: Record<string, string> = {
  Clean: 'Limpio',
  Salvage: 'Salvamento',
  Rebuilt: 'Reconstruido',
  Flood: 'Inundación',
  Junk: 'Chatarra',
  Unknown: 'Desconocido',
};

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
      nameEs: `Título: ${TITLE_TYPE_ES[history.titleType] || history.titleType}`,
      impact: titlePenalty,
      description: `Vehicle has a ${history.titleType} title`,
      descriptionEs: `El vehículo tiene título de ${TITLE_TYPE_ES[history.titleType] || history.titleType}`,
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
      nameEs: 'Recompra por Ley Limón',
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
// Spec: Engine (+60), Transmission (+30), Drivetrain (+25),
//       Safety Tech (+20), Recalls (−15 each), Complaints (−5/10)
// =============================================================================

function calculateD2(
  vinDecode: VinDecodeResult,
  recalls?: NhtsaRecall[],
  complaints?: NhtsaComplaintSummary,
  safetyFeatures?: string[]
): DimensionScore {
  const factors: ScoreFactor[] = [];
  let raw = 100; // base score for a standard vehicle

  // ── 1. ENGINE SCORING (up to +60 pts) ──────────────────────────
  // Hybrid/Electric > Turbo > High Displacement > Standard
  const engineLower = vinDecode.engineType?.toLowerCase() || '';
  const isElectric = engineLower.includes('electric');
  const isHybrid = engineLower.includes('hybrid');
  const isTurbo = engineLower.includes('turbo') || engineLower.includes('supercharg');
  const displacement = vinDecode.displacementL || 0;
  const cylinders = vinDecode.engineCylinders || 4;

  if (isElectric) {
    raw += 60;
    factors.push({
      name: 'Electric Powertrain',
      nameEs: 'Motor Eléctrico',
      impact: 60,
      description: 'Zero-emission electric drivetrain — highest value retention',
      descriptionEs: 'Tren motriz eléctrico cero emisiones — mayor retención de valor',
      source: 'NHTSA vPIC',
    });
  } else if (isHybrid) {
    raw += 50;
    factors.push({
      name: 'Hybrid Powertrain',
      nameEs: 'Motor Híbrido',
      impact: 50,
      description: 'Fuel-efficient hybrid drivetrain',
      descriptionEs: 'Tren motriz híbrido de alta eficiencia',
      source: 'NHTSA vPIC',
    });
  } else {
    // ICE engine — score by displacement and turbo
    let enginePts = 0;

    if (isTurbo) {
      enginePts += 25;
      factors.push({
        name: 'Turbocharged Engine',
        nameEs: 'Motor Turbo',
        impact: 25,
        description: 'Forced induction for better performance',
        descriptionEs: 'Inducción forzada para mejor rendimiento',
        source: 'NHTSA vPIC',
      });
    }

    // Displacement bonus (sweet spot: 2.0L–3.5L for DR market)
    if (displacement >= 2.0 && displacement <= 3.5) {
      const dispPts = Math.round(15 + (displacement - 2.0) * 5); // 15–22 pts
      enginePts += Math.min(dispPts, 22);
    } else if (displacement > 3.5) {
      enginePts += 15; // Large engines — functional but less efficient for DR
    } else if (displacement > 0 && displacement < 2.0) {
      enginePts += 10; // Small engines — economical
    }

    // Cylinder bonus
    if (cylinders >= 8) {
      enginePts += 10;
    } else if (cylinders >= 6) {
      enginePts += 8;
    }

    enginePts = Math.min(enginePts, 60); // cap at spec max
    if (enginePts > 0) {
      raw += enginePts;
      if (!isTurbo) {
        factors.push({
          name: `${displacement > 0 ? displacement.toFixed(1) + 'L ' : ''}${cylinders}-cyl Engine`,
          nameEs: `Motor ${displacement > 0 ? displacement.toFixed(1) + 'L ' : ''}${cylinders} cilindros`,
          impact: enginePts,
          description: `Engine displacement and configuration bonus`,
          descriptionEs: `Bonificación por cilindrada y configuración del motor`,
          source: 'NHTSA vPIC',
        });
      }
    }
  }

  // ── 2. TRANSMISSION SCORING (up to +30 pts) ───────────────────
  const transLower = vinDecode.transmission?.toLowerCase() || '';
  let transPts = 0;

  if (transLower.includes('cvt')) {
    transPts = 30;
    factors.push({
      name: 'CVT Transmission',
      nameEs: 'Transmisión CVT',
      impact: 30,
      description: 'Continuously variable — optimal fuel efficiency',
      descriptionEs: 'Variable continua — eficiencia óptima de combustible',
      source: 'NHTSA vPIC',
    });
  } else if (transLower.includes('dct') || transLower.includes('dual clutch')) {
    transPts = 28;
    factors.push({
      name: 'DCT Transmission',
      nameEs: 'Transmisión DCT',
      impact: 28,
      description: 'Dual-clutch for fast shifting and efficiency',
      descriptionEs: 'Doble embrague para cambios rápidos y eficiencia',
      source: 'NHTSA vPIC',
    });
  } else if (transLower.includes('tiptronic') || transLower.includes('sport')) {
    transPts = 25;
    factors.push({
      name: 'Tiptronic/Sport Auto',
      nameEs: 'Tiptronic/Sport Automática',
      impact: 25,
      description: 'Sport automatic with manual mode',
      descriptionEs: 'Automática deportiva con modo manual',
      source: 'NHTSA vPIC',
    });
  } else if (transLower.includes('auto')) {
    transPts = 20;
    factors.push({
      name: 'Automatic Transmission',
      nameEs: 'Transmisión Automática',
      impact: 20,
      description: 'Standard automatic transmission',
      descriptionEs: 'Transmisión automática estándar',
      source: 'NHTSA vPIC',
    });
  } else if (transLower.includes('manual')) {
    transPts = 10;
    factors.push({
      name: 'Manual Transmission',
      nameEs: 'Transmisión Manual',
      impact: 10,
      description: 'Manual — lower demand in DR market',
      descriptionEs: 'Manual — menor demanda en mercado RD',
      source: 'NHTSA vPIC',
    });
  }
  raw += transPts;

  // ── 3. DRIVETRAIN (+25 pts for AWD/4WD) ───────────────────────
  if (vinDecode.drivetrain === 'AWD' || vinDecode.drivetrain === '4WD') {
    raw += 25;
    factors.push({
      name: 'AWD/4WD',
      nameEs: 'Tracción Total AWD/4WD',
      impact: 25,
      description: 'All-wheel or four-wheel drive — versatility and resale value',
      descriptionEs: 'Tracción total — versatilidad y valor de reventa',
      source: 'NHTSA vPIC',
    });
  }

  // ── 4. SAFETY TECHNOLOGY (+20 pts) ────────────────────────────
  // ADAS features from Edmunds or VIN equipment data
  const knownFeatures = (safetyFeatures || []).map(f => f.toLowerCase());
  const adasKeywords = [
    { keyword: 'lane', label: 'Lane Assist', labelEs: 'Asistente de Carril' },
    { keyword: 'aeb', label: 'AEB', labelEs: 'Frenado Automático de Emergencia' },
    { keyword: 'autonomous emergency', label: 'AEB', labelEs: 'Frenado Automático' },
    { keyword: 'blind spot', label: 'Blind Spot Monitor', labelEs: 'Monitor de Punto Ciego' },
    { keyword: 'collision', label: 'Collision Warning', labelEs: 'Alerta de Colisión' },
    { keyword: 'adaptive cruise', label: 'Adaptive Cruise', labelEs: 'Cruise Adaptativo' },
  ];

  const detectedAdas = new Set<string>();
  for (const feat of adasKeywords) {
    if (knownFeatures.some(f => f.includes(feat.keyword))) {
      detectedAdas.add(feat.label);
    }
  }

  if (detectedAdas.size > 0) {
    // +7 pts per ADAS feature, capped at +20
    const techPts = Math.min(detectedAdas.size * 7, 20);
    raw += techPts;
    factors.push({
      name: `Safety Tech: ${Array.from(detectedAdas).join(', ')}`,
      nameEs: `Tecnología de Seguridad: ${Array.from(detectedAdas).join(', ')}`,
      impact: techPts,
      description: `${detectedAdas.size} active safety feature(s) detected`,
      descriptionEs: `${detectedAdas.size} función(es) de seguridad activa detectada(s)`,
      source: 'Edmunds',
    });
  }

  // ── 5. ACTIVE RECALLS PENALTY (−15 pts per active recall) ─────
  const activeRecalls = recalls?.filter(r => !r.isResolved) || [];
  if (activeRecalls.length > 0) {
    const recallPenalty = activeRecalls.length * -15;
    raw += recallPenalty;
    factors.push({
      name: `${activeRecalls.length} Active Recall(s)`,
      nameEs: `${activeRecalls.length} retiro(s) de fábrica pendiente(s)`,
      impact: recallPenalty,
      description: `Unresolved recalls: ${activeRecalls.map(r => r.component).join(', ')}`,
      descriptionEs: `Retiros de fábrica sin resolver: ${activeRecalls.map(r => r.component).join(', ')}`,
      source: 'NHTSA',
    });
  }

  // ── 6. NHTSA COMPLAINTS PENALTY (−5 pts per 10 complaints) ────
  if (complaints && complaints.totalComplaints > 0) {
    const complaintGroups = Math.floor(complaints.totalComplaints / 10);
    if (complaintGroups > 0) {
      const complaintPenalty = complaintGroups * -5;
      raw += complaintPenalty;

      // Top complaint components
      const topComponents = Object.entries(complaints.componentBreakdown)
        .sort(([, a], [, b]) => b - a)
        .slice(0, 3)
        .map(([comp, count]) => `${comp} (${count})`);

      factors.push({
        name: `${complaints.totalComplaints} NHTSA Complaints`,
        nameEs: `${complaints.totalComplaints} Quejas NHTSA`,
        impact: complaintPenalty,
        description: `Top issues: ${topComponents.join(', ')}`,
        descriptionEs: `Principales problemas: ${topComponents.join(', ')}`,
        source: 'NHTSA',
      });
    }
  }

  return buildDimension('D2', Math.max(0, Math.min(200, raw)), factors);
}

// =============================================================================
// D3 — MILEAGE / ODOMETER (18%, max 180 pts)
// =============================================================================

function calculateD3(
  declaredMileage: number,
  unit: 'miles' | 'km',
  vehicleYear: number,
  history?: VinHistoryReport
): DimensionScore {
  const factors: ScoreFactor[] = [];
  const miles = unit === 'km' ? declaredMileage / 1.60934 : declaredMileage;

  // ── FIX C1: Penalize zero/missing mileage instead of rewarding it ──
  if (!declaredMileage || declaredMileage <= 0) {
    factors.push({
      name: 'No mileage declared',
      nameEs: 'Sin kilometraje declarado',
      impact: -40,
      description: 'Mileage was not provided — cannot verify odometer',
      descriptionEs: 'No se proporcionó el kilometraje — no se puede verificar el odómetro',
      source: 'OKLA Validation',
    });
    return buildDimension('D3', 50, factors); // neutral-low instead of max 180
  }

  // ── FIX C2: Age-adjusted mileage scoring ──
  // Use miles-per-year instead of absolute mileage for fairer comparison.
  // Average in DR/US market: ~12,000 mi/yr
  const currentYear = new Date().getFullYear();
  const vehicleAge = Math.max(1, currentYear - (vehicleYear || currentYear));
  const milesPerYear = miles / vehicleAge;

  let raw: number;
  if (milesPerYear <= 8000) {
    // Excellent: under 8k mi/yr (≈13k km/yr)
    raw = 180;
  } else if (milesPerYear <= 12000) {
    // Good: average usage
    raw = 150;
  } else if (milesPerYear <= 15000) {
    // Above average
    raw = 120;
  } else if (milesPerYear <= 20000) {
    // High usage
    raw = 80;
  } else if (milesPerYear <= 25000) {
    // Very high
    raw = 40;
  } else {
    // Extreme (commercial/taxi use likely)
    raw = 10;
  }

  factors.push({
    name: `Mileage: ${Math.round(miles).toLocaleString()} mi (${Math.round(milesPerYear).toLocaleString()} mi/yr)`,
    nameEs: `Kilometraje: ${Math.round(miles * 1.60934).toLocaleString()} km (${Math.round(milesPerYear * 1.60934).toLocaleString()} km/año)`,
    impact: raw - 90,
    description: `${Math.round(miles).toLocaleString()} miles over ${vehicleAge} years = ${Math.round(milesPerYear).toLocaleString()} mi/yr`,
    descriptionEs: `${Math.round(miles * 1.60934).toLocaleString()} km en ${vehicleAge} años = ${Math.round(milesPerYear * 1.60934).toLocaleString()} km/año`,
    source: 'Seller declaration',
  });

  // ── FIX A2: Use backend's sequential rollback detection (VinAudit/NMVTIS) ──
  if (history?.odometerRollback) {
    raw = 0; // hard penalty — backend confirmed sequential rollback
    factors.push({
      name: 'ODOMETER ROLLBACK DETECTED',
      nameEs: 'RETROCESO DE ODÓMETRO DETECTADO',
      impact: -180,
      description: 'Sequential odometer records show a decrease >500 miles — confirmed rollback',
      descriptionEs:
        'Registros secuenciales del odómetro muestran una disminución >500 millas — retroceso confirmado',
      source: 'VinAudit/NMVTIS',
    });
    return buildDimension('D3', 0, factors);
  }

  // ── FIX A1: DIRECTIONAL discrepancy check (replaces broken Math.abs) ──
  // Only flag as suspicious if declared mileage is LOWER than last reported.
  // A higher declared mileage is a NORMAL increase — not fraud.
  if (history?.lastReportedMileage && history.lastReportedMileage > 0) {
    const lastMiles = history.lastReportedMileage;

    if (miles < lastMiles * 0.85) {
      // Declared mileage is significantly LOWER than historical → potential fraud
      const discrepancy = ((lastMiles - miles) / lastMiles) * 100;
      raw = 0;
      factors.push({
        name: 'ODOMETER FRAUD SUSPECTED',
        nameEs: 'SOSPECHA DE FRAUDE DE ODÓMETRO',
        impact: -180,
        description: `Declared ${Math.round(miles).toLocaleString()} mi but last reported was ${Math.round(lastMiles).toLocaleString()} mi (${discrepancy.toFixed(0)}% lower)`,
        descriptionEs: `Declarado ${Math.round(miles).toLocaleString()} mi pero último reporte fue ${Math.round(lastMiles).toLocaleString()} mi (${discrepancy.toFixed(0)}% menor)`,
        source: 'VinAudit/NMVTIS',
      });
    } else if (miles > lastMiles) {
      // Normal increase — optionally check if rate is realistic
      const daysSinceReport = history.lastReportedDate
        ? Math.max(
            1,
            (Date.now() - new Date(history.lastReportedDate).getTime()) / (1000 * 60 * 60 * 24)
          )
        : 365;
      const milesPerDay = (miles - lastMiles) / daysSinceReport;

      // > 200 mi/day sustained is suspicious (~73k mi/yr, well above commercial)
      if (milesPerDay > 200) {
        raw = Math.max(10, raw - 60);
        factors.push({
          name: 'Unusually rapid mileage increase',
          nameEs: 'Aumento inusualmente rápido del kilometraje',
          impact: -60,
          description: `${Math.round(miles - lastMiles).toLocaleString()} mi increase in ~${Math.round(daysSinceReport)} days (${Math.round(milesPerDay)} mi/day)`,
          descriptionEs: `${Math.round((miles - lastMiles) * 1.60934).toLocaleString()} km de aumento en ~${Math.round(daysSinceReport)} días`,
          source: 'VinAudit/NMVTIS',
        });
      }
    }
  }

  // ── FIX A3: Use odometerReadings for sequential analysis when available ──
  if (history?.odometerReadings && history.odometerReadings.length >= 2) {
    const sorted = [...history.odometerReadings].sort(
      (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
    );
    for (let i = 1; i < sorted.length; i++) {
      if (sorted[i].mileage < sorted[i - 1].mileage - 500) {
        raw = 0;
        factors.push({
          name: 'SEQUENTIAL ROLLBACK IN RECORDS',
          nameEs: 'RETROCESO SECUENCIAL EN REGISTROS',
          impact: -180,
          description: `Record ${sorted[i].date}: ${sorted[i].mileage.toLocaleString()} mi < prior ${sorted[i - 1].date}: ${sorted[i - 1].mileage.toLocaleString()} mi`,
          descriptionEs: `Registro ${sorted[i].date}: ${sorted[i].mileage.toLocaleString()} mi < anterior ${sorted[i - 1].date}: ${sorted[i - 1].mileage.toLocaleString()} mi`,
          source: 'Service Records',
        });
        break;
      }
    }
  }

  return buildDimension('D3', Math.max(0, raw), factors);
}

// =============================================================================
// D4 — PRICE VS MARKET (17%, max 170 pts)
// =============================================================================

function calculateD4(listedPriceDOP: number, marketPriceDOP?: number): DimensionScore {
  const factors: ScoreFactor[] = [];

  // FIX B9: When no market data, return LOW-neutral score (60/170 ≈ 35%)
  // instead of generous 85/170 (50%). This prevents artificially inflating
  // scores for vehicles whose pricing hasn't been validated.
  if (!marketPriceDOP || marketPriceDOP <= 0) {
    return buildDimension('D4', 60, [
      {
        name: 'Price unverified',
        nameEs: 'Precio no verificado',
        impact: -25,
        description: 'No market comparison data available — price cannot be validated',
        descriptionEs: 'Sin datos de comparación de mercado — no se puede validar el precio',
        source: 'OKLA Market Algorithm',
      },
    ]);
  }

  // FIX B1: Reject clearly invalid prices (listed price must be > 0)
  if (listedPriceDOP <= 0) {
    return buildDimension('D4', 0, [
      {
        name: 'Invalid price',
        nameEs: 'Precio inválido',
        impact: -170,
        description: 'Listed price is zero or negative',
        descriptionEs: 'El precio listado es cero o negativo',
        source: 'OKLA Market Algorithm',
      },
    ]);
  }

  const diff = ((listedPriceDOP - marketPriceDOP) / marketPriceDOP) * 100;
  let raw: number;

  // FIX: Improved scoring curve with smoother transitions and
  // "suspiciously low" detection (>40% below market = possible scam)
  if (diff <= -40) {
    // Suspiciously low — possible scam, hidden damage, or bait pricing
    raw = 80;
  } else if (diff <= -15) {
    // Great deal (verified low price)
    raw = 170;
  } else if (diff <= -5) {
    raw = 140;
  } else if (diff <= 5) {
    // At market (±5%)
    raw = 120;
  } else if (diff <= 10) {
    raw = 90;
  } else if (diff <= 15) {
    raw = 60;
  } else if (diff <= 25) {
    raw = 30;
  } else if (diff <= 35) {
    raw = 10;
  } else {
    // >35% above market
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

  // FIX: Add suspicious pricing alert factor
  if (diff <= -40) {
    factors.push({
      name: 'SUSPICIOUS_LOW_PRICE',
      nameEs: 'PRECIO SOSPECHOSAMENTE BAJO',
      impact: -90,
      description: `Price is ${Math.abs(diff).toFixed(0)}% below market — possible scam or undisclosed issues`,
      descriptionEs: `Precio está ${Math.abs(diff).toFixed(0)}% por debajo del mercado — posible fraude o problemas no divulgados`,
      source: 'OKLA Fraud Detection',
    });
  }

  // FIX: Add "abusive pricing" alert factor
  if (diff > 30) {
    factors.push({
      name: 'ABUSIVE_PRICE',
      nameEs: 'PRECIO ABUSIVO',
      impact: -85,
      description: `Price is ${diff.toFixed(0)}% above market average`,
      descriptionEs: `Precio está ${diff.toFixed(0)}% por encima del promedio del mercado`,
      source: 'OKLA Market Algorithm',
    });
  }

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
  const d2 = calculateD2(input.vinDecode, input.recalls, input.complaints, input.safetyFeatures);
  const d3 = calculateD3(
    input.declaredMileage,
    input.mileageUnit,
    input.vinDecode.year,
    input.history
  );
  const d4 = calculateD4(input.listedPriceDOP, input.marketPriceDOP);
  const d5 = calculateD5(input.recalls, input.safetyRating);
  const d6 = calculateD6(input.vinDecode.year);
  const d7 = calculateD7(input.sellerType, input.sellerScore, input.sellerDisputes);

  const dimensions = [d1, d2, d3, d4, d5, d6, d7];
  const totalScore = Math.round(dimensions.reduce((sum, d) => sum + d.weightedScore, 0));
  const clampedScore = Math.max(0, Math.min(1000, totalScore));
  const level = getScoreLevel(clampedScore);

  // Price analysis
  // Falls back to the default constant when no live rate is provided
  const rate = input.exchangeRate || DOP_USD_EXCHANGE_RATE;
  const fairPriceUSD =
    input.marketPriceUSD || (input.marketPriceDOP ? input.marketPriceDOP / rate : 0);
  const fairPriceDOP = input.marketPriceDOP || fairPriceUSD * rate;
  const priceDiff =
    fairPriceDOP > 0 ? ((input.listedPriceDOP - fairPriceDOP) / fairPriceDOP) * 100 : 0;

  let priceVerdict: PriceVerdict = 'fair_price';
  if (priceDiff <= -40)
    priceVerdict = 'suspicious_deal'; // FIX: new category for scam-level pricing
  else if (priceDiff <= -15) priceVerdict = 'excellent_deal';
  else if (priceDiff <= -5) priceVerdict = 'good_price';
  else if (priceDiff <= 5) priceVerdict = 'fair_price';
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

  // D2 FIX: Active recalls mechanical alert
  const activeRecallsCount = (input.recalls || []).filter(r => !r.isResolved).length;
  if (activeRecallsCount >= 3) {
    alerts.push({
      severity: 'warning',
      code: 'MULTIPLE_ACTIVE_RECALLS',
      title: 'Multiple Unresolved Recalls',
      titleEs: 'Múltiples Retiros de Fábrica Pendientes',
      description: `${activeRecallsCount} active recalls may indicate mechanical risk`,
      descriptionEs: `${activeRecallsCount} retiros de fábrica activos pueden indicar riesgo mecánico`,
      dimension: 'D2',
    });
  }

  // D2 FIX: High NHTSA complaint count alert
  if (input.complaints && input.complaints.totalComplaints >= 50) {
    alerts.push({
      severity: 'info',
      code: 'HIGH_NHTSA_COMPLAINTS',
      title: 'High NHTSA Complaint Volume',
      titleEs: 'Alto Volumen de Quejas NHTSA',
      description: `${input.complaints.totalComplaints} complaints reported for this make/model/year`,
      descriptionEs: `${input.complaints.totalComplaints} quejas reportadas para esta marca/modelo/año`,
      dimension: 'D2',
    });
  }

  // FIX A4: Odometer fraud/rollback alert — was missing entirely
  if (d3.factors.some(f => f.name.includes('ODOMETER') || f.name.includes('ROLLBACK'))) {
    alerts.push({
      severity: 'critical',
      code: 'ODOMETER_FRAUD',
      title: 'Odometer Tampering Detected',
      titleEs: 'Manipulación del Odómetro Detectada',
      description: 'Odometer readings indicate possible fraud or rollback',
      descriptionEs: 'Las lecturas del odómetro indican posible fraude o retroceso',
      dimension: 'D3',
    });
  }

  // D4 FIX: Price-based alerts
  if (d4.factors.some(f => f.name === 'SUSPICIOUS_LOW_PRICE')) {
    alerts.push({
      severity: 'warning',
      code: 'SUSPICIOUS_LOW_PRICE',
      title: 'Suspiciously Low Price',
      titleEs: 'Precio Sospechosamente Bajo',
      description:
        'This vehicle is priced significantly below market value — investigate before purchasing',
      descriptionEs:
        'Este vehículo tiene un precio muy por debajo del valor de mercado — investigue antes de comprar',
      dimension: 'D4',
    });
  }

  if (d4.factors.some(f => f.name === 'ABUSIVE_PRICE')) {
    alerts.push({
      severity: 'info',
      code: 'ABUSIVE_PRICE',
      title: 'Price Significantly Above Market',
      titleEs: 'Precio Significativamente Sobre el Mercado',
      description: 'This vehicle is priced well above the market average for similar vehicles',
      descriptionEs:
        'Este vehículo tiene un precio muy superior al promedio del mercado para vehículos similares',
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
