---
title: "Pricing Intelligence - Sistema Completo"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🎯 Pricing Intelligence - Sistema Completo

> **Base:** docs/process-matrix/20-PRICING-INTELLIGENCE/  
> **Documentos:** 01-deal-rating.md, 02-valuacion-instantanea.md  
> **Última actualización:** Enero 29, 2026  
> **Estado:** Backend ✅ 100% | UI 🟡 40%

---

## 📊 AUDITORÍA DE ESTADO

### Estado Actual del Backend

| Servicio                       | Estado  | Puerto | Database                   | Tests  |
| ------------------------------ | ------- | ------ | -------------------------- | ------ |
| **PricingIntelligenceService** | ✅ 100% | 5090   | pricingintelligenceservice | 🟡 75% |

**Controllers:**

- ✅ DealRatingController (100%)
- ✅ MarketValueController (100%)
- ✅ ValuationController (100%)

### Estado Actual del UI

| Funcionalidad             | Backend | UI      | Gap Identificado                 |
| ------------------------- | ------- | ------- | -------------------------------- |
| **Deal Rating Badge**     | ✅ 100% | ✅ 100% | Visible en listings ✓            |
| **Explicación de Rating** | ✅ 100% | 🟡 50%  | Tooltip básico, falta detalle    |
| **Histórico de Precios**  | ✅ 100% | 🔴 0%   | Sin gráfico ni tendencias        |
| **Comparables**           | ✅ 100% | 🔴 0%   | Sin lista de vehículos similares |
| **Factores de Precio**    | ✅ 100% | 🔴 0%   | Sin breakdown                    |
| **Valuación Instantánea** | ✅ 100% | 🔴 0%   | Sin wizard ni resultado          |
| **Lead Capture**          | ✅ 100% | 🔴 0%   | Sin flujo                        |

### Procesos Consolidados

| Categoría          | Procesos | Backend    | UI         | Prioridad  |
| ------------------ | -------- | ---------- | ---------- | ---------- |
| **DEAL-CALC-\***   | 5        | ✅ 100%    | 🟡 50%     | 🔴 CRÍTICA |
| **DEAL-ML-\***     | 4        | 🟡 75%     | 🔴 0%      | 🟡 ALTA    |
| **DEAL-HIST-\***   | 3        | ✅ 100%    | 🔴 0%      | 🟡 MEDIA   |
| **DEAL-DISP-\***   | 3        | 🟡 67%     | ✅ 100%    | ✅ DONE    |
| **VAL-CALC-\***    | 4        | ✅ 100%    | 🔴 0%      | 🔴 CRÍTICA |
| **VAL-ML-\***      | 4        | 🟡 75%     | 🔴 0%      | 🟡 ALTA    |
| **VAL-LEAD-\***    | 3        | ✅ 100%    | 🔴 0%      | 🔴 CRÍTICA |
| **VAL-CONVERT-\*** | 3        | 🟡 67%     | 🔴 0%      | 🔴 CRÍTICA |
| **TOTAL**          | **29**   | **🟡 85%** | **🔴 18%** | -          |

**Resumen:** Backend casi completo (85%), UI necesita trabajo significativo (solo 18%)

---

## 🎯 OBJETIVO DEL SISTEMA

### Deal Rating (Calificación de Precios)

**Propósito:**

1. **Transparencia:** Mostrar si un precio es justo
2. **Confianza:** Usuarios confían en la plataforma
3. **Diferenciación:** Feature que SuperCarros NO tiene
4. **Conversión:** Usuarios compran más rápido con info clara

**Inspiración:** CarGurus Deal Rating system

### Valuación Instantánea

**Propósito:**

1. **Captación de vendedores:** "¿Cuánto vale mi carro?" atrae tráfico
2. **Lead generation:** Capturar email/teléfono
3. **Confianza:** OKLA sabe del mercado
4. **Conversión:** De valuación a publicación

**Inspiración:** CarGurus Instant Market Value + Kavak Online Valuation

---

## 🏗️ ARQUITECTURA DEL SISTEMA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   PRICING INTELLIGENCE SYSTEM                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   Frontend Components                  Backend Service                       │
│   ┌─────────────────────────┐      ┌──────────────────────────────────┐    │
│   │ DealRatingBadge         │──┐   │ PricingIntelligenceService       │    │
│   │ (Card overlay)          │  │   │  Port: 5090                      │    │
│   └─────────────────────────┘  │   │  DB: pricingintelligenceservice  │    │
│   ┌─────────────────────────┐  │   │                                  │    │
│   │ PriceAnalysisSection    │──┼──▶│  Controllers:                    │    │
│   │ (Vehicle detail)        │  │   │  • DealRatingController          │    │
│   └─────────────────────────┘  │   │  • MarketValueController         │    │
│   ┌─────────────────────────┐  │   │  • ValuationController           │    │
│   │ ValuationWizard         │──┘   │                                  │    │
│   │ (/sell/valuation)       │      │  ML Engines:                     │    │
│   └─────────────────────────┘      │  • Deal Rating Engine            │    │
│   ┌─────────────────────────┐      │  • Market Value Estimator        │    │
│   │ ValuationResult         │◀─────│  • Comparable Finder             │    │
│   │ (Result page)           │      │  • Valuation Calculator          │    │
│   └─────────────────────────┘      │                                  │    │
│                                    │  Domain:                         │    │
│   API Service                      │  • DealRating                    │    │
│   ┌─────────────────────────┐      │  • MarketValueEstimate           │    │
│   │ pricingService.ts       │      │  • ValuationRequest              │    │
│   │ • getDealRating()       │      │  • ValuationResult               │    │
│   │ • getMarketValue()      │      │  • ComparableVehicle             │    │
│   │ • requestValuation()    │      │  • PricingFactor                 │    │
│   │ • getValuation()        │      └──────────────────────────────────┘    │
│   │ • claimValuation()      │                     │                        │
│   └─────────────────────────┘          ┌──────────┼──────────┐             │
│                                         ▼          ▼          ▼             │
│                                   ┌──────────┬─────────┬──────────┐         │
│                                   │PostgreSQL│  Redis  │ RabbitMQ │         │
│                                   │(Ratings, │(Cache)  │(Events)  │         │
│                                   │Values)   │         │          │         │
│                                   └──────────┴─────────┴──────────┘         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 SISTEMA DE CALIFICACIÓN - DEAL RATING

### Escala de 5 Niveles

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     DEAL RATING - ESCALA DE PRECIOS                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🟢 EXCELENTE PRECIO   │  15%+ por debajo del mercado                  │
│  ────────────────────  │  "Este vehículo tiene un precio excepcional"  │
│                        │  Color: #22c55e (green-500)                   │
│                                                                         │
│  🟢 BUEN PRECIO        │  5-15% por debajo del mercado                 │
│  ────────────────────  │  "Este vehículo tiene buen precio"            │
│                        │  Color: #16a34a (green-600)                   │
│                                                                         │
│  🟡 PRECIO JUSTO       │  Dentro del rango de mercado (±5%)            │
│  ────────────────────  │  "Este vehículo tiene precio de mercado"      │
│                        │  Color: #eab308 (yellow-500)                  │
│                                                                         │
│  🟠 PRECIO ALTO        │  5-15% por encima del mercado                 │
│  ────────────────────  │  "Este vehículo está por encima del mercado"  │
│                        │  Color: #f97316 (orange-500)                  │
│                                                                         │
│  🔴 SOBREPRECIADO      │  15%+ por encima del mercado                  │
│  ────────────────────  │  "Este vehículo está muy por encima"          │
│                        │  Color: #ef4444 (red-500)                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Algoritmo de Cálculo

**1. Encontrar Comparables:**

```typescript
Criterios de búsqueda:
- Marca = igual
- Modelo = igual
- Año = ±2 años
- Kilometraje = ±30%
- Estado = Activo
- Mínimo: 5 comparables

Scoring de Similaridad:
- Año (30%): Menos diferencia = más peso
- Kilometraje (25%): Menos diferencia = más peso
- Trim/Versión (20%): Exacto = +10%
- Ubicación (15%): Misma ciudad = +8%, provincia = +4%
- Condición (10%): Igual = +5%
```

**2. Calcular Valor de Mercado:**

```typescript
Promedio Ponderado:
marketValue = Σ(price × similarityScore) / Σ(similarityScore)

Ajustes:
+ Kilometraje: RD$0.50 por km bajo promedio
+ Condición Excelente: +RD$50,000
+ Features Premium: +RD$30,000-100,000
+ Ubicación Santo Domingo: +RD$20,000
- Alto Kilometraje: -RD$0.50 por km sobre promedio
- Condición Pobre: -RD$80,000
- Historial Accidentes: -RD$150,000
```

**3. Asignar Rating:**

```typescript
diffPercent = (listedPrice - marketValue) / marketValue × 100

if (diffPercent <= -15) → 🟢 EXCELENTE PRECIO
else if (diffPercent <= -5) → 🟢 BUEN PRECIO
else if (diffPercent <= 5) → 🟡 PRECIO JUSTO
else if (diffPercent <= 15) → 🟠 PRECIO ALTO
else → 🔴 SOBREPRECIADO
```

**4. Calcular Confidence Score:**

```typescript
score = 0

// Cantidad de comparables (máx 40 pts)
if (comparablesCount >= 20) score += 40
else score += comparablesCount * 2

// Similaridad promedio (máx 30 pts)
score += avgSimilarity * 30

// Dispersión de precios (máx 30 pts)
priceVariance = (maxPrice - minPrice) / avgPrice
if (priceVariance < 0.15) score += 30
else if (priceVariance < 0.30) score += 20
else score += 10

Resultado:
0-50: Low Confidence
51-75: Medium Confidence
76-100: High Confidence
```

---

## 💰 SISTEMA DE VALUACIÓN INSTANTÁNEA

### 3 Valores Calculados

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      RANGOS DE VALUACIÓN                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  TRADE-IN VALUE (Valor Trade-In)                                       │
│  ════════════════════════════════════════════════════════════════════  │
│  El precio más bajo - Lo que un dealer te pagaría                      │
│  Cálculo: Market Value × 0.85                                          │
│  Ejemplo: RD$ 980,000                                                  │
│                                                                         │
│  PRIVATE PARTY VALUE (Venta Particular) ⭐ RECOMENDADO                 │
│  ════════════════════════════════════════════════════════════════════  │
│  Valor realista entre particulares - Precio sugerido de publicación   │
│  Cálculo: Market Value × 1.0 (base)                                   │
│  Ejemplo: RD$ 1,150,000                                               │
│                                                                         │
│  DEALER RETAIL VALUE (Precio Dealer)                                   │
│  ════════════════════════════════════════════════════════════════════  │
│  El precio más alto - Lo que un dealer lo vendería                    │
│  Cálculo: Market Value × 1.15                                         │
│  Ejemplo: RD$ 1,320,000                                               │
│                                                                         │
│  RANGO DE VENTA RÁPIDA                                                 │
│  ════════════════════════════════════════════════════════════════════  │
│  Low: TradeIn - 10%    →    RD$ 882,000                               │
│  High: Dealer + 10%    →    RD$ 1,452,000                             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Factores que Afectan el Precio

**Positivos:**

```
✅ Un solo dueño                    +RD$ 40,000-60,000
✅ Sin historial de accidentes      +RD$ 60,000-150,000
✅ Transmisión automática           +RD$ 25,000-50,000
✅ Bajo kilometraje                 +RD$ 30,000-80,000
✅ Condición excelente              +RD$ 50,000-100,000
✅ Mantenimiento documentado        +RD$ 20,000-40,000
✅ Garantía vigente                 +RD$ 30,000-60,000
✅ Features premium (techo, cuero)  +RD$ 40,000-80,000
✅ Color popular (blanco, negro)    +RD$ 10,000-20,000
✅ Ubicación Santo Domingo          +RD$ 20,000-50,000
```

**Negativos:**

```
❌ 2+ dueños                        -RD$ 20,000-40,000
❌ Historial de accidentes          -RD$ 80,000-200,000
❌ Alto kilometraje                 -RD$ 50,000-150,000
❌ Condición pobre                  -RD$ 100,000-200,000
❌ Sin mantenimiento documentado    -RD$ 15,000-30,000
❌ Transmisión manual               -RD$ 20,000-40,000
❌ Color poco común                 -RD$ 10,000-20,000
❌ Ubicación remota                 -RD$ 30,000-60,000
❌ Fumador                          -RD$ 15,000-25,000
❌ Modificaciones no profesionales  -RD$ 20,000-50,000
```

---

## 🎨 COMPONENTES PRINCIPALES

### 1️⃣ DealRatingBadge (Badge en Listing Cards)

**Ubicación:** Overlay en imagen del vehículo en cards y search results

**Props:**

```typescript
interface DealRatingBadgeProps {
  rating: DealRatingLevel;
  listedPrice: number;
  marketValue: number;
  priceDifference: number;
  priceDifferencePercent: number;
  showTooltip?: boolean;
}

enum DealRatingLevel {
  GreatDeal = 1, // 🟢 Excelente Precio
  GoodDeal = 2, // 🟢 Buen Precio
  FairDeal = 3, // 🟡 Precio Justo
  HighPrice = 4, // 🟠 Precio Alto
  Overpriced = 5, // 🔴 Sobrepreciado
}
```

**Implementación:**

```tsx
// components/vehicles/DealRatingBadge.tsx (120 LOC)

import { Badge } from "@/components/ui/badge";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { formatCurrency } from "@/lib/utils";

const ratingConfig = {
  [DealRatingLevel.GreatDeal]: {
    label: "EXCELENTE PRECIO",
    emoji: "🟢",
    color: "bg-green-500 text-white",
    borderColor: "border-green-600",
  },
  [DealRatingLevel.GoodDeal]: {
    label: "BUEN PRECIO",
    emoji: "🟢",
    color: "bg-green-600 text-white",
    borderColor: "border-green-700",
  },
  [DealRatingLevel.FairDeal]: {
    label: "PRECIO JUSTO",
    emoji: "🟡",
    color: "bg-yellow-500 text-white",
    borderColor: "border-yellow-600",
  },
  [DealRatingLevel.HighPrice]: {
    label: "PRECIO ALTO",
    emoji: "🟠",
    color: "bg-orange-500 text-white",
    borderColor: "border-orange-600",
  },
  [DealRatingLevel.Overpriced]: {
    label: "SOBREPRECIADO",
    emoji: "🔴",
    color: "bg-red-500 text-white",
    borderColor: "border-red-600",
  },
};

export const DealRatingBadge = ({
  rating,
  listedPrice,
  marketValue,
  priceDifference,
  priceDifferencePercent,
  showTooltip = true,
}: DealRatingBadgeProps) => {
  const config = ratingConfig[rating];
  const isGoodDeal = rating <= DealRatingLevel.GoodDeal;
  const savingsText = isGoodDeal
    ? `${formatCurrency(Math.abs(priceDifference))} por debajo del mercado`
    : `${formatCurrency(priceDifference)} por encima del mercado`;

  const badge = (
    <Badge
      className={`${config.color} ${config.borderColor} border-2 font-bold text-xs px-2 py-1 shadow-lg`}
    >
      {config.emoji} {config.label}
    </Badge>
  );

  if (!showTooltip) return badge;

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>{badge}</TooltipTrigger>
        <TooltipContent className="max-w-xs">
          <div className="space-y-2">
            <p className="font-semibold text-sm">{config.label}</p>
            <div className="text-xs space-y-1">
              <div className="flex justify-between">
                <span className="text-gray-500">Precio listado:</span>
                <span className="font-medium">
                  {formatCurrency(listedPrice)}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Valor de mercado:</span>
                <span className="font-medium">
                  {formatCurrency(marketValue)}
                </span>
              </div>
              <div className="flex justify-between border-t pt-1">
                <span className="text-gray-500">Diferencia:</span>
                <span
                  className={`font-bold ${isGoodDeal ? "text-green-600" : "text-red-600"}`}
                >
                  {priceDifferencePercent > 0 ? "+" : ""}
                  {priceDifferencePercent.toFixed(1)}%
                </span>
              </div>
            </div>
            <p className="text-xs text-gray-600 italic">{savingsText}</p>
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
};
```

**Uso en VehicleCard:**

```tsx
// En VehicleCard.tsx - agregar en overlay de imagen

<div className="relative">
  <Image src={vehicle.images[0]} alt={vehicle.title} />

  {/* Deal Rating Badge - Top Right */}
  {vehicle.dealRating && (
    <div className="absolute top-2 right-2">
      <DealRatingBadge
        rating={vehicle.dealRating.rating}
        listedPrice={vehicle.price}
        marketValue={vehicle.dealRating.marketValue}
        priceDifference={vehicle.dealRating.priceDifference}
        priceDifferencePercent={vehicle.dealRating.priceDifferencePercent}
      />
    </div>
  )}

  {/* Savings Text - Bottom */}
  {vehicle.dealRating?.rating <= DealRatingLevel.GoodDeal && (
    <div className="absolute bottom-2 left-2 right-2">
      <div className="bg-green-600/90 text-white text-xs font-medium px-2 py-1 rounded">
        💡 {formatCurrency(Math.abs(vehicle.dealRating.priceDifference))} por
        debajo del mercado
      </div>
    </div>
  )}
</div>
```

---

### 2️⃣ PriceAnalysisSection (Detalle en Vehicle Page)

**Ubicación:** Sección completa en `/vehicles/:slug` después de specs

**Props:**

```typescript
interface PriceAnalysisSectionProps {
  vehicleId: string;
  listedPrice: number;
}
```

**Implementación:**

```tsx
// components/vehicles/PriceAnalysisSection.tsx (450 LOC)

import { useQuery } from "@tanstack/react-query";
import { pricingService } from "@/lib/services/pricingService";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { formatCurrency, cn } from "@/lib/utils";
import { CheckCircle2, XCircle, Info } from "lucide-react";

export const PriceAnalysisSection = ({
  vehicleId,
  listedPrice,
}: PriceAnalysisSectionProps) => {
  const { data: dealRating, isLoading } = useQuery({
    queryKey: ["dealRating", vehicleId],
    queryFn: () => pricingService.getDealRating(vehicleId),
  });

  const { data: marketValue } = useQuery({
    queryKey: ["marketValue", vehicleId],
    queryFn: () => pricingService.getMarketValue(vehicleId),
  });

  if (isLoading) return <PriceAnalysisSkeleton />;
  if (!dealRating) return null;

  const isGoodDeal = dealRating.rating <= DealRatingLevel.GoodDeal;
  const config = ratingConfig[dealRating.rating];

  return (
    <Card className="border-2">
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            📊 Análisis de Precio
          </CardTitle>
          <Badge className={config.color}>
            {config.emoji} {config.label}
          </Badge>
        </div>
        <p className="text-sm text-gray-600">{dealRating.ratingDescription}</p>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Price Comparison */}
        <div className="bg-gray-50 rounded-lg p-4 space-y-3">
          <div className="flex justify-between items-center">
            <span className="text-sm text-gray-600">Precio listado:</span>
            <span className="text-lg font-bold">
              {formatCurrency(listedPrice)}
            </span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-sm text-gray-600">Valor de mercado:</span>
            <span className="text-lg font-medium">
              {formatCurrency(dealRating.marketValue)}
            </span>
          </div>
          <div className="border-t pt-2 flex justify-between items-center">
            <span className="text-sm font-medium">
              {isGoodDeal ? "Ahorro estimado:" : "Por encima del mercado:"}
            </span>
            <span
              className={cn(
                "text-lg font-bold",
                isGoodDeal ? "text-green-600" : "text-red-600",
              )}
            >
              {isGoodDeal && "✓ "}
              {formatCurrency(Math.abs(dealRating.priceDifference))}
            </span>
          </div>
        </div>

        {/* Price Range Slider */}
        <div>
          <p className="text-sm text-gray-600 mb-2">
            📈 Rango de precios para {marketValue?.make} {marketValue?.model}{" "}
            {marketValue?.year} similar:
          </p>
          <div className="relative h-12 bg-gradient-to-r from-green-100 via-yellow-100 to-red-100 rounded-lg">
            {/* Min/Max Labels */}
            <div className="absolute -top-6 left-0 text-xs text-gray-500">
              {formatCurrency(dealRating.similarVehiclesMinPrice)}
            </div>
            <div className="absolute -top-6 right-0 text-xs text-gray-500">
              {formatCurrency(dealRating.similarVehiclesMaxPrice)}
            </div>

            {/* Current Price Marker */}
            <div
              className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2"
              style={{
                left: `${calculatePosition(
                  listedPrice,
                  dealRating.similarVehiclesMinPrice,
                  dealRating.similarVehiclesMaxPrice,
                )}%`,
              }}
            >
              <div className="w-3 h-3 bg-blue-600 rounded-full border-2 border-white shadow-lg" />
              <div className="absolute top-6 left-1/2 -translate-x-1/2 whitespace-nowrap text-xs font-medium">
                ↑ Este vehículo
              </div>
            </div>
          </div>
        </div>

        {/* Pricing Factors */}
        {dealRating.factors && dealRating.factors.length > 0 && (
          <div>
            <p className="text-sm font-medium mb-3">
              📋 Factores que afectan el precio:
            </p>
            <div className="space-y-2">
              {dealRating.factors.map((factor, idx) => (
                <div key={idx} className="flex items-start gap-2 text-sm">
                  {factor.impact === "Positive" ? (
                    <CheckCircle2 className="h-4 w-4 text-green-600 mt-0.5 flex-shrink-0" />
                  ) : (
                    <XCircle className="h-4 w-4 text-red-600 mt-0.5 flex-shrink-0" />
                  )}
                  <div className="flex-1">
                    <span className="font-medium">{factor.name}</span>
                    <p className="text-xs text-gray-500">
                      {factor.description}
                    </p>
                  </div>
                  <span
                    className={cn(
                      "font-medium",
                      factor.impact === "Positive"
                        ? "text-green-600"
                        : "text-red-600",
                    )}
                  >
                    {factor.impact === "Positive" ? "+" : ""}
                    {formatCurrency(factor.impactAmount)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Comparables Info */}
        <div className="bg-blue-50 rounded-lg p-3 text-xs text-gray-600 flex items-start gap-2">
          <Info className="h-4 w-4 text-blue-600 mt-0.5 flex-shrink-0" />
          <div>
            <p>
              Basado en{" "}
              <strong>
                {dealRating.similarVehiclesCount} vehículos similares
              </strong>{" "}
              en el mercado
            </p>
            <p className="mt-1">
              Confianza:{" "}
              <strong
                className={getConfidenceColor(dealRating.confidenceScore)}
              >
                {dealRating.confidenceScore}% (
                {getConfidenceLabel(dealRating.confidenceScore)})
              </strong>
            </p>
            <p className="text-xs text-gray-500 mt-1">
              🔄 Actualizado: {formatRelativeTime(dealRating.calculatedAt)}
            </p>
          </div>
        </div>

        {/* Price History Chart (Future - Recharts) */}
        {marketValue?.priceHistory && (
          <div>
            <p className="text-sm font-medium mb-3">
              📊 Histórico de precios (últimos 90 días)
            </p>
            <PriceHistoryChart data={marketValue.priceHistory} />
          </div>
        )}
      </CardContent>
    </Card>
  );
};

// Helper functions
const calculatePosition = (price: number, min: number, max: number) => {
  return ((price - min) / (max - min)) * 100;
};

const getConfidenceColor = (score: number) => {
  if (score >= 76) return "text-green-600";
  if (score >= 51) return "text-yellow-600";
  return "text-red-600";
};

const getConfidenceLabel = (score: number) => {
  if (score >= 76) return "Alta";
  if (score >= 51) return "Media";
  return "Baja";
};

const PriceAnalysisSkeleton = () => (
  <Card>
    <CardHeader>
      <Skeleton className="h-6 w-48" />
    </CardHeader>
    <CardContent className="space-y-4">
      <Skeleton className="h-24 w-full" />
      <Skeleton className="h-12 w-full" />
      <Skeleton className="h-32 w-full" />
    </CardContent>
  </Card>
);
```

---

### 3️⃣ ValuationWizard (Formulario de Valuación)

**Ubicación:** `/sell/valuation` - Landing page de valuación

**Props:**

```typescript
interface ValuationWizardProps {
  onComplete?: (result: ValuationResult) => void;
}
```

**Implementación:**

```tsx
// app/(public)/sell/valuation/page.tsx (600 LOC)

"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery } from "@tanstack/react-query";
import { pricingService } from "@/lib/services/pricingService";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Loader2 } from "lucide-react";

const valuationSchema = z.object({
  make: z.string().min(1, "Selecciona una marca"),
  model: z.string().min(1, "Selecciona un modelo"),
  year: z
    .number()
    .min(2000)
    .max(new Date().getFullYear() + 1),
  trim: z.string().optional(),
  mileage: z.number().min(0).max(500000),
  condition: z.enum(["Excellent", "Good", "Fair", "Poor"]),
  transmission: z.enum(["Automatic", "Manual"]),
  fuelType: z.enum(["Gasoline", "Diesel", "Hybrid", "Electric"]),
  hasAccidentHistory: z.boolean(),
  numberOfOwners: z.number().min(1).max(10),
  city: z.string().min(1, "Selecciona una ciudad"),
  province: z.string().min(1, "Selecciona una provincia"),
});

type ValuationFormData = z.infer<typeof valuationSchema>;

const STEPS = [
  { id: 1, title: "Vehículo", fields: ["make", "model", "year", "trim"] },
  {
    id: 2,
    title: "Detalles",
    fields: ["mileage", "condition", "transmission", "fuelType"],
  },
  {
    id: 3,
    title: "Historial",
    fields: ["hasAccidentHistory", "numberOfOwners"],
  },
  { id: 4, title: "Ubicación", fields: ["city", "province"] },
];

export default function ValuationWizardPage() {
  const router = useRouter();
  const [currentStep, setCurrentStep] = useState(1);
  const [selectedMake, setSelectedMake] = useState("");
  const [selectedModel, setSelectedModel] = useState("");

  const form = useForm<ValuationFormData>({
    resolver: zodResolver(valuationSchema),
    defaultValues: {
      condition: "Good",
      transmission: "Automatic",
      fuelType: "Gasoline",
      hasAccidentHistory: false,
      numberOfOwners: 1,
    },
  });

  // Fetch makes
  const { data: makes } = useQuery({
    queryKey: ["makes"],
    queryFn: () => pricingService.getMakes(),
  });

  // Fetch models when make selected
  const { data: models } = useQuery({
    queryKey: ["models", selectedMake],
    queryFn: () => pricingService.getModels(selectedMake),
    enabled: !!selectedMake,
  });

  // Fetch trims when model selected
  const { data: trims } = useQuery({
    queryKey: ["trims", selectedMake, selectedModel, form.watch("year")],
    queryFn: () =>
      pricingService.getTrims(selectedMake, selectedModel, form.watch("year")),
    enabled: !!selectedMake && !!selectedModel && !!form.watch("year"),
  });

  // Submit valuation
  const valuationMutation = useMutation({
    mutationFn: (data: ValuationFormData) =>
      pricingService.requestValuation(data),
    onSuccess: (result) => {
      router.push(`/valuation/result/${result.id}`);
    },
  });

  const handleNext = async () => {
    const currentFields = STEPS[currentStep - 1].fields;
    const isValid = await form.trigger(currentFields as any);

    if (isValid) {
      if (currentStep === STEPS.length) {
        form.handleSubmit((data) => valuationMutation.mutate(data))();
      } else {
        setCurrentStep((prev) => prev + 1);
      }
    }
  };

  const handleBack = () => {
    setCurrentStep((prev) => Math.max(1, prev - 1));
  };

  const progress = (currentStep / STEPS.length) * 100;

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 py-12">
      <div className="container max-w-2xl mx-auto px-4">
        {/* Hero Section */}
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">
            💰 ¿Cuánto Vale Tu Vehículo?
          </h1>
          <p className="text-lg text-gray-600">
            Obtén una valuación instantánea basada en datos reales del mercado
          </p>
        </div>

        {/* Progress Bar */}
        <div className="mb-8">
          <div className="flex justify-between mb-2">
            {STEPS.map((step) => (
              <div
                key={step.id}
                className={`flex-1 text-center text-sm font-medium ${
                  step.id <= currentStep ? "text-blue-600" : "text-gray-400"
                }`}
              >
                {step.title}
              </div>
            ))}
          </div>
          <Progress value={progress} className="h-2" />
        </div>

        <Card>
          <CardHeader>
            <CardTitle>
              Paso {currentStep} de {STEPS.length}:{" "}
              {STEPS[currentStep - 1].title}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form className="space-y-6">
              {/* Step 1: Vehicle */}
              {currentStep === 1 && (
                <div className="space-y-4">
                  <div>
                    <Label htmlFor="make">Marca *</Label>
                    <Select
                      value={form.watch("make")}
                      onValueChange={(value) => {
                        form.setValue("make", value);
                        setSelectedMake(value);
                        form.setValue("model", "");
                        setSelectedModel("");
                      }}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Selecciona una marca" />
                      </SelectTrigger>
                      <SelectContent>
                        {makes?.map((make) => (
                          <SelectItem key={make} value={make}>
                            {make}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {form.formState.errors.make && (
                      <p className="text-sm text-red-600 mt-1">
                        {form.formState.errors.make.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <Label htmlFor="model">Modelo *</Label>
                    <Select
                      value={form.watch("model")}
                      onValueChange={(value) => {
                        form.setValue("model", value);
                        setSelectedModel(value);
                      }}
                      disabled={!selectedMake}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Selecciona un modelo" />
                      </SelectTrigger>
                      <SelectContent>
                        {models?.map((model) => (
                          <SelectItem key={model} value={model}>
                            {model}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {form.formState.errors.model && (
                      <p className="text-sm text-red-600 mt-1">
                        {form.formState.errors.model.message}
                      </p>
                    )}
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <Label htmlFor="year">Año *</Label>
                      <Input
                        type="number"
                        min={2000}
                        max={new Date().getFullYear() + 1}
                        {...form.register("year", { valueAsNumber: true })}
                        placeholder="2021"
                      />
                      {form.formState.errors.year && (
                        <p className="text-sm text-red-600 mt-1">
                          {form.formState.errors.year.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <Label htmlFor="trim">Versión (opcional)</Label>
                      <Select
                        value={form.watch("trim")}
                        onValueChange={(value) => form.setValue("trim", value)}
                        disabled={!trims || trims.length === 0}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Selecciona" />
                        </SelectTrigger>
                        <SelectContent>
                          {trims?.map((trim) => (
                            <SelectItem key={trim} value={trim}>
                              {trim}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </div>
              )}

              {/* Step 2: Details */}
              {currentStep === 2 && (
                <div className="space-y-4">
                  <div>
                    <Label htmlFor="mileage">Kilometraje *</Label>
                    <Input
                      type="number"
                      {...form.register("mileage", { valueAsNumber: true })}
                      placeholder="45000"
                    />
                    <p className="text-xs text-gray-500 mt-1">
                      Kilómetros recorridos
                    </p>
                    {form.formState.errors.mileage && (
                      <p className="text-sm text-red-600 mt-1">
                        {form.formState.errors.mileage.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <Label>Condición *</Label>
                    <RadioGroup
                      value={form.watch("condition")}
                      onValueChange={(value) =>
                        form.setValue("condition", value as any)
                      }
                      className="grid grid-cols-2 gap-3 mt-2"
                    >
                      {["Excellent", "Good", "Fair", "Poor"].map((cond) => (
                        <div key={cond} className="flex items-center space-x-2">
                          <RadioGroupItem value={cond} id={cond} />
                          <Label
                            htmlFor={cond}
                            className="font-normal cursor-pointer"
                          >
                            {conditionLabels[cond]}
                          </Label>
                        </div>
                      ))}
                    </RadioGroup>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <Label>Transmisión *</Label>
                      <Select
                        value={form.watch("transmission")}
                        onValueChange={(value) =>
                          form.setValue("transmission", value as any)
                        }
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Automatic">Automática</SelectItem>
                          <SelectItem value="Manual">Manual</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div>
                      <Label>Combustible *</Label>
                      <Select
                        value={form.watch("fuelType")}
                        onValueChange={(value) =>
                          form.setValue("fuelType", value as any)
                        }
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Gasoline">Gasolina</SelectItem>
                          <SelectItem value="Diesel">Diésel</SelectItem>
                          <SelectItem value="Hybrid">Híbrido</SelectItem>
                          <SelectItem value="Electric">Eléctrico</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </div>
              )}

              {/* Step 3: History */}
              {currentStep === 3 && (
                <div className="space-y-4">
                  <div>
                    <Label>¿Ha tenido accidentes? *</Label>
                    <RadioGroup
                      value={form.watch("hasAccidentHistory").toString()}
                      onValueChange={(value) =>
                        form.setValue("hasAccidentHistory", value === "true")
                      }
                      className="flex gap-4 mt-2"
                    >
                      <div className="flex items-center space-x-2">
                        <RadioGroupItem value="false" id="no-accidents" />
                        <Label
                          htmlFor="no-accidents"
                          className="font-normal cursor-pointer"
                        >
                          No
                        </Label>
                      </div>
                      <div className="flex items-center space-x-2">
                        <RadioGroupItem value="true" id="yes-accidents" />
                        <Label
                          htmlFor="yes-accidents"
                          className="font-normal cursor-pointer"
                        >
                          Sí
                        </Label>
                      </div>
                    </RadioGroup>
                  </div>

                  <div>
                    <Label>¿Cuántos dueños ha tenido? *</Label>
                    <RadioGroup
                      value={form.watch("numberOfOwners").toString()}
                      onValueChange={(value) =>
                        form.setValue("numberOfOwners", parseInt(value))
                      }
                      className="flex gap-4 mt-2"
                    >
                      {[1, 2, 3].map((num) => (
                        <div key={num} className="flex items-center space-x-2">
                          <RadioGroupItem
                            value={num.toString()}
                            id={`owners-${num}`}
                          />
                          <Label
                            htmlFor={`owners-${num}`}
                            className="font-normal cursor-pointer"
                          >
                            {num}
                            {num === 3 ? "+" : ""}
                          </Label>
                        </div>
                      ))}
                    </RadioGroup>
                  </div>

                  <div className="bg-blue-50 rounded-lg p-4 text-sm text-gray-700">
                    <p className="font-medium mb-1">💡 ¿Sabías que...?</p>
                    <p>
                      Los vehículos con un solo dueño y sin historial de
                      accidentes pueden valer hasta{" "}
                      <strong>RD$100,000 más</strong> que vehículos similares.
                    </p>
                  </div>
                </div>
              )}

              {/* Step 4: Location */}
              {currentStep === 4 && (
                <div className="space-y-4">
                  <div>
                    <Label htmlFor="province">Provincia *</Label>
                    <Select
                      value={form.watch("province")}
                      onValueChange={(value) =>
                        form.setValue("province", value)
                      }
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Selecciona una provincia" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Distrito Nacional">
                          Distrito Nacional
                        </SelectItem>
                        <SelectItem value="Santo Domingo">
                          Santo Domingo
                        </SelectItem>
                        <SelectItem value="Santiago">Santiago</SelectItem>
                        <SelectItem value="La Vega">La Vega</SelectItem>
                        {/* Add more provinces */}
                      </SelectContent>
                    </Select>
                    {form.formState.errors.province && (
                      <p className="text-sm text-red-600 mt-1">
                        {form.formState.errors.province.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <Label htmlFor="city">Ciudad *</Label>
                    <Input
                      {...form.register("city")}
                      placeholder="Santo Domingo"
                    />
                    {form.formState.errors.city && (
                      <p className="text-sm text-red-600 mt-1">
                        {form.formState.errors.city.message}
                      </p>
                    )}
                  </div>

                  <div className="bg-green-50 rounded-lg p-4 text-sm text-gray-700">
                    <p className="font-medium mb-1">🎉 ¡Casi listo!</p>
                    <p>
                      En unos segundos verás el valor estimado de tu vehículo
                      basado en datos reales del mercado dominicano.
                    </p>
                  </div>
                </div>
              )}

              {/* Navigation Buttons */}
              <div className="flex gap-3 pt-4">
                {currentStep > 1 && (
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleBack}
                    className="flex-1"
                  >
                    ← Atrás
                  </Button>
                )}
                <Button
                  type="button"
                  onClick={handleNext}
                  disabled={valuationMutation.isPending}
                  className="flex-1"
                >
                  {valuationMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Calculando...
                    </>
                  ) : currentStep === STEPS.length ? (
                    "💰 Ver Mi Valuación"
                  ) : (
                    "Siguiente →"
                  )}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>

        {/* Trust Indicators */}
        <div className="mt-6 text-center text-sm text-gray-600">
          <p>🔒 Tu información es privada. No compartimos tus datos.</p>
        </div>
      </div>
    </div>
  );
}

const conditionLabels = {
  Excellent: "Excelente",
  Good: "Buena",
  Fair: "Regular",
  Poor: "Pobre",
};
```

---

### 4️⃣ ValuationResultPage (Resultado de Valuación)

**Ubicación:** `/valuation/result/:id`

**Implementación:**

```tsx
// app/(public)/valuation/result/[id]/page.tsx (500 LOC)

"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import { pricingService } from "@/lib/services/pricingService";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/utils";
import {
  CheckCircle2,
  XCircle,
  TrendingUp,
  TrendingDown,
  Mail,
  Car,
  ArrowRight,
  Info,
} from "lucide-react";
import confetti from "canvas-confetti";

export default function ValuationResultPage({
  params,
}: {
  params: { id: string };
}) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [showEmailCapture, setShowEmailCapture] = useState(true);

  const { data: valuation, isLoading } = useQuery({
    queryKey: ["valuation", params.id],
    queryFn: () => pricingService.getValuation(params.id),
  });

  const claimMutation = useMutation({
    mutationFn: (email: string) =>
      pricingService.claimValuation(params.id, email),
    onSuccess: () => {
      setShowEmailCapture(false);
      confetti({
        particleCount: 100,
        spread: 70,
        origin: { y: 0.6 },
      });
    },
  });

  useEffect(() => {
    if (valuation && !valuation.claimedAt) {
      // Show email capture after 10 seconds
      const timer = setTimeout(() => {
        setShowEmailCapture(true);
      }, 10000);
      return () => clearTimeout(timer);
    }
  }, [valuation]);

  if (isLoading) return <ValuationResultSkeleton />;
  if (!valuation) return <div>Valuación no encontrada</div>;

  const result = valuation.result;
  const vehicle = `${valuation.make} ${valuation.model} ${valuation.year}`;
  const hasPositiveFactors =
    result.positiveFactors && result.positiveFactors.length > 0;
  const hasNegativeFactors =
    result.negativeFactors && result.negativeFactors.length > 0;

  return (
    <div className="min-h-screen bg-gradient-to-br from-green-50 to-emerald-100 py-12">
      <div className="container max-w-4xl mx-auto px-4">
        {/* Success Header */}
        <div className="text-center mb-8">
          <div className="inline-block bg-green-500 text-white rounded-full p-3 mb-4">
            <CheckCircle2 className="h-8 w-8" />
          </div>
          <h1 className="text-4xl font-bold text-gray-900 mb-2">
            ✅ ¡Valuación Lista!
          </h1>
          <p className="text-lg text-gray-600">
            Tu {vehicle} con {valuation.mileage.toLocaleString()} km
          </p>
        </div>

        {/* Main Valuation Card */}
        <Card className="mb-6 border-2 border-green-200">
          <CardHeader className="bg-gradient-to-r from-green-50 to-emerald-50">
            <CardTitle className="text-center">
              VALOR ESTIMADO DE VENTA PARTICULAR
            </CardTitle>
          </CardHeader>
          <CardContent className="pt-6">
            {/* Main Value */}
            <div className="text-center mb-6">
              <div className="text-5xl font-bold text-green-600 mb-2">
                {formatCurrency(result.privatePartyValue)}
              </div>
              <p className="text-sm text-gray-600">
                Rango: {formatCurrency(result.valueRangeLow)} -{" "}
                {formatCurrency(result.valueRangeHigh)}
              </p>
            </div>

            {/* Value Range Slider */}
            <div className="relative mb-8">
              <div className="flex justify-between text-xs text-gray-500 mb-2">
                <span>Trade-In</span>
                <span>Particular</span>
                <span>Dealer</span>
              </div>
              <div className="relative h-16 bg-gradient-to-r from-blue-100 via-green-100 to-red-100 rounded-lg">
                {/* Value Markers */}
                <div className="absolute bottom-0 left-0 w-1/3 h-full flex flex-col justify-end pb-2">
                  <div className="text-center text-xs font-medium">
                    {formatCurrency(result.tradeInValue)}
                  </div>
                </div>
                <div className="absolute bottom-0 left-1/3 w-1/3 h-full flex flex-col justify-end pb-2">
                  <div className="text-center text-xs font-bold text-green-600">
                    {formatCurrency(result.privatePartyValue)}
                  </div>
                  <div className="mx-auto w-3 h-3 bg-green-600 rounded-full border-2 border-white shadow-lg"></div>
                </div>
                <div className="absolute bottom-0 right-0 w-1/3 h-full flex flex-col justify-end pb-2">
                  <div className="text-center text-xs font-medium">
                    {formatCurrency(result.dealerRetailValue)}
                  </div>
                </div>
              </div>
            </div>

            {/* Recommended Listing Price */}
            <div className="bg-green-50 border-2 border-green-200 rounded-lg p-4 mb-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-gray-600">
                    Precio sugerido de publicación:
                  </p>
                  <p className="text-2xl font-bold text-green-600">
                    {formatCurrency(result.privatePartyValue)}
                  </p>
                </div>
                <Info className="h-5 w-5 text-green-600" />
              </div>
              <p className="text-xs text-gray-600 mt-2">
                Este precio te ayudará a vender rápido y obtener buen valor
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Pricing Factors */}
        {(hasPositiveFactors || hasNegativeFactors) && (
          <Card className="mb-6">
            <CardHeader>
              <CardTitle className="text-lg">
                📈 Factores que Afectan Tu Precio
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Positive Factors */}
              {hasPositiveFactors && (
                <div className="space-y-2">
                  {result.positiveFactors.map((factor, idx) => (
                    <div key={idx} className="flex items-start gap-2 text-sm">
                      <CheckCircle2 className="h-5 w-5 text-green-600 mt-0.5 flex-shrink-0" />
                      <div className="flex-1">
                        <span className="font-medium">{factor.name}</span>
                        <p className="text-xs text-gray-500">
                          {factor.description}
                        </p>
                      </div>
                      <span className="text-green-600 font-bold">
                        +{formatCurrency(factor.impact)}
                      </span>
                    </div>
                  ))}
                </div>
              )}

              {/* Negative Factors */}
              {hasNegativeFactors && (
                <>
                  {hasPositiveFactors && <Separator />}
                  <div className="space-y-2">
                    {result.negativeFactors.map((factor, idx) => (
                      <div key={idx} className="flex items-start gap-2 text-sm">
                        <XCircle className="h-5 w-5 text-red-600 mt-0.5 flex-shrink-0" />
                        <div className="flex-1">
                          <span className="font-medium">{factor.name}</span>
                          <p className="text-xs text-gray-500">
                            {factor.description}
                          </p>
                        </div>
                        <span className="text-red-600 font-bold">
                          {formatCurrency(factor.impact)}
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        )}

        {/* Market Trend */}
        {result.trendDirection && (
          <Card className="mb-6">
            <CardContent className="pt-6">
              <div className="flex items-center gap-3">
                {result.trendDirection === "up" ? (
                  <TrendingUp className="h-6 w-6 text-green-600" />
                ) : result.trendDirection === "down" ? (
                  <TrendingDown className="h-6 w-6 text-red-600" />
                ) : (
                  <span className="text-2xl">→</span>
                )}
                <div>
                  <p className="font-medium">
                    {result.trendDirection === "up" && "Precios en aumento"}
                    {result.trendDirection === "down" && "Precios en descenso"}
                    {result.trendDirection === "stable" && "Precios estables"}
                  </p>
                  <p className="text-sm text-gray-600">
                    {result.priceChange30Days > 0 ? "+" : ""}
                    {result.priceChange30Days.toFixed(1)}% en los últimos 30
                    días
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* CTA: Publish Listing */}
        <Card className="mb-6 border-2 border-blue-200">
          <CardContent className="pt-6">
            <div className="text-center space-y-4">
              <h3 className="text-2xl font-bold">🚗 ¿Listo para Vender?</h3>
              <p className="text-gray-600">
                Publica tu vehículo ahora y alcanza miles de compradores
                potenciales
              </p>
              <Button
                size="lg"
                onClick={() => router.push("/sell")}
                className="w-full sm:w-auto"
              >
                <Car className="mr-2 h-5 w-5" />
                Publicar Mi Vehículo Ahora
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
              <p className="text-xs text-gray-500">
                Tu valuación se guardará automáticamente en tu cuenta
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Email Capture */}
        {showEmailCapture && !valuation.claimedAt && (
          <Card className="border-2 border-yellow-200 bg-yellow-50">
            <CardContent className="pt-6">
              <div className="space-y-4">
                <div className="text-center">
                  <Mail className="h-8 w-8 text-yellow-600 mx-auto mb-2" />
                  <h3 className="text-lg font-bold">
                    📧 ¿Quieres guardar esta valuación?
                  </h3>
                  <p className="text-sm text-gray-600">
                    Te enviaremos un email con tu valuación para que la tengas
                    siempre disponible
                  </p>
                </div>
                <div className="flex gap-2">
                  <Input
                    type="email"
                    placeholder="tu@email.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                  <Button
                    onClick={() => claimMutation.mutate(email)}
                    disabled={!email || claimMutation.isPending}
                  >
                    {claimMutation.isPending ? "Guardando..." : "Guardar"}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Footer Info */}
        <div className="mt-6 text-center text-sm text-gray-600 space-y-1">
          <p>
            ℹ️ Basado en{" "}
            <strong>{result.comparablesCount} vehículos similares</strong>
          </p>
          <p>
            Confianza:{" "}
            <strong className={getConfidenceColor(result.confidenceScore)}>
              {result.confidenceScore}% (
              {getConfidenceLabel(result.confidenceScore)})
            </strong>
          </p>
          <p className="text-xs">
            Válido por 7 días · Calculado:{" "}
            {new Date(result.calculatedAt).toLocaleDateString()}
          </p>
        </div>
      </div>
    </div>
  );
}

const ValuationResultSkeleton = () => (
  <div className="min-h-screen bg-gradient-to-br from-green-50 to-emerald-100 py-12">
    <div className="container max-w-4xl mx-auto px-4">
      <div className="animate-pulse space-y-6">
        <div className="h-32 bg-white rounded-lg" />
        <div className="h-64 bg-white rounded-lg" />
        <div className="h-48 bg-white rounded-lg" />
      </div>
    </div>
  </div>
);

const getConfidenceColor = (score: number) => {
  if (score >= 76) return "text-green-600";
  if (score >= 51) return "text-yellow-600";
  return "text-red-600";
};

const getConfidenceLabel = (score: number) => {
  if (score >= 76) return "Alta";
  if (score >= 51) return "Media";
  return "Baja";
};
```

---

## 📡 API SERVICE

### pricingService.ts

**Ubicación:** `src/lib/services/pricingService.ts`

**Implementación:**

```typescript
// lib/services/pricingService.ts (300 LOC)

import axios from "axios";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:18443";

// Types
export interface DealRating {
  id: string;
  vehicleId: string;
  rating: DealRatingLevel;
  ratingLabel: string;
  ratingDescription: string;
  ratingColor: string;
  listedPrice: number;
  marketValue: number;
  priceDifference: number;
  priceDifferencePercent: number;
  similarVehiclesCount: number;
  similarVehiclesAvgPrice: number;
  similarVehiclesMinPrice: number;
  similarVehiclesMaxPrice: number;
  factors: PricingFactor[];
  calculatedAt: string;
  expiresAt: string;
  confidenceScore: number;
}

export enum DealRatingLevel {
  GreatDeal = 1,
  GoodDeal = 2,
  FairDeal = 3,
  HighPrice = 4,
  Overpriced = 5,
}

export interface PricingFactor {
  name: string;
  description: string;
  impact: "Positive" | "Negative" | "Neutral";
  impactAmount: number;
}

export interface MarketValueEstimate {
  id: string;
  make: string;
  model: string;
  year: number;
  trim: string;
  mileage: number;
  condition: string;
  estimatedValue: number;
  valueRangeLow: number;
  valueRangeHigh: number;
  comparablesCount: number;
  comparables: ComparableVehicle[];
  priceChange30Days: number;
  priceChange90Days: number;
  trend: "up" | "down" | "stable";
  calculatedAt: string;
}

export interface ComparableVehicle {
  vehicleId: string;
  title: string;
  price: number;
  mileage: number;
  year: number;
  location: string;
  daysOnMarket: number;
  similarityScore: number;
}

export interface ValuationRequest {
  make: string;
  model: string;
  year: number;
  trim?: string;
  mileage: number;
  condition: "Excellent" | "Good" | "Fair" | "Poor";
  transmission: "Automatic" | "Manual";
  fuelType: "Gasoline" | "Diesel" | "Hybrid" | "Electric";
  hasAccidentHistory: boolean;
  numberOfOwners: number;
  city: string;
  province: string;
  email?: string;
  phone?: string;
}

export interface ValuationResult {
  id: string;
  requestId: string;
  tradeInValue: number;
  privatePartyValue: number;
  dealerRetailValue: number;
  valueRangeLow: number;
  valueRangeHigh: number;
  comparablesCount: number;
  comparables: ComparableVehicle[];
  positiveFactors: ValuationFactor[];
  negativeFactors: ValuationFactor[];
  priceChange30Days: number;
  trendDirection: "up" | "down" | "stable";
  confidenceScore: number;
  confidenceLevel: string;
  calculatedAt: string;
  validUntil: string;
}

export interface ValuationFactor {
  name: string;
  description: string;
  impact: number;
  type: "Positive" | "Negative";
}

export interface Valuation {
  id: string;
  make: string;
  model: string;
  year: number;
  trim?: string;
  mileage: number;
  condition: string;
  transmission: string;
  fuelType: string;
  hasAccidentHistory: boolean;
  numberOfOwners: number;
  city: string;
  province: string;
  result: ValuationResult;
  email?: string;
  phone?: string;
  claimedAt?: string;
  createdAt: string;
}

class PricingService {
  private axiosInstance = axios.create({
    baseURL: `${API_URL}/api/pricing`,
    headers: {
      "Content-Type": "application/json",
    },
  });

  constructor() {
    // Add auth token interceptor
    this.axiosInstance.interceptors.request.use((config) => {
      const token = localStorage.getItem("authToken");
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
  }

  // Deal Rating
  async getDealRating(vehicleId: string): Promise<DealRating> {
    const response = await this.axiosInstance.get(`/deal-rating/${vehicleId}`);
    return response.data;
  }

  async getMarketValue(vehicleId: string): Promise<MarketValueEstimate> {
    const response = await this.axiosInstance.get(`/market-value/${vehicleId}`);
    return response.data;
  }

  async getSimilarPrices(params: {
    make: string;
    model: string;
    year: number;
    mileage: number;
    limit?: number;
  }): Promise<ComparableVehicle[]> {
    const response = await this.axiosInstance.get("/similar-prices", {
      params,
    });
    return response.data;
  }

  async getPricingFactors(vehicleId: string): Promise<PricingFactor[]> {
    const response = await this.axiosInstance.get(`/factors/${vehicleId}`);
    return response.data;
  }

  // Valuation
  async requestValuation(request: ValuationRequest): Promise<Valuation> {
    const response = await this.axiosInstance.post(
      "/valuation/instant",
      request,
    );
    return response.data;
  }

  async getValuation(id: string): Promise<Valuation> {
    const response = await this.axiosInstance.get(`/valuation/${id}`);
    return response.data;
  }

  async claimValuation(
    id: string,
    email: string,
    phone?: string,
  ): Promise<void> {
    await this.axiosInstance.post(`/valuation/${id}/claim`, { email, phone });
  }

  // Catalog for valuation wizard
  async getMakes(): Promise<string[]> {
    const response = await this.axiosInstance.get("/valuation/makes");
    return response.data;
  }

  async getModels(make: string): Promise<string[]> {
    const response = await this.axiosInstance.get(`/valuation/models/${make}`);
    return response.data;
  }

  async getTrims(make: string, model: string, year: number): Promise<string[]> {
    const response = await this.axiosInstance.get(
      `/valuation/trims/${make}/${model}/${year}`,
    );
    return response.data;
  }
}

export const pricingService = new PricingService();
```

---

## 🧪 ESTRATEGIA DE TESTING

### Unit Tests

```typescript
// __tests__/components/DealRatingBadge.test.tsx

import { render, screen } from '@testing-library/react';
import { DealRatingBadge } from '@/components/vehicles/DealRatingBadge';
import { DealRatingLevel } from '@/lib/services/pricingService';

describe('DealRatingBadge', () => {
  it('should display Great Deal badge correctly', () => {
    render(
      <DealRatingBadge
        rating={DealRatingLevel.GreatDeal}
        listedPrice={1250000}
        marketValue={1500000}
        priceDifference={-250000}
        priceDifferencePercent={-16.67}
      />
    );

    expect(screen.getByText(/EXCELENTE PRECIO/i)).toBeInTheDocument();
    expect(screen.getByText(/🟢/)).toBeInTheDocument();
  });

  it('should show tooltip on hover', async () => {
    // Test tooltip interaction
  });

  it('should display correct color for each rating level', () => {
    // Test all 5 levels
  });
});
```

### Integration Tests

```typescript
// __tests__/services/pricingService.test.ts

import { pricingService } from "@/lib/services/pricingService";
import { server } from "@/mocks/server";
import { rest } from "msw";

describe("PricingService", () => {
  it("should fetch deal rating successfully", async () => {
    const vehicleId = "veh-123";
    const dealRating = await pricingService.getDealRating(vehicleId);

    expect(dealRating.vehicleId).toBe(vehicleId);
    expect(dealRating.rating).toBeDefined();
    expect(dealRating.marketValue).toBeGreaterThan(0);
  });

  it("should submit valuation request", async () => {
    const request = {
      make: "Toyota",
      model: "Corolla",
      year: 2021,
      mileage: 45000,
      condition: "Good" as const,
      transmission: "Automatic" as const,
      fuelType: "Gasoline" as const,
      hasAccidentHistory: false,
      numberOfOwners: 1,
      city: "Santo Domingo",
      province: "Distrito Nacional",
    };

    const result = await pricingService.requestValuation(request);

    expect(result.id).toBeDefined();
    expect(result.result.privatePartyValue).toBeGreaterThan(0);
  });
});
```

### E2E Tests

```typescript
// e2e/valuation-flow.spec.ts

import { test, expect } from "@playwright/test";

test.describe("Valuation Flow", () => {
  test("should complete valuation wizard", async ({ page }) => {
    await page.goto("/sell/valuation");

    // Step 1: Vehicle
    await page.selectOption('select[name="make"]', "Toyota");
    await page.waitForTimeout(500);
    await page.selectOption('select[name="model"]', "Corolla");
    await page.fill('input[name="year"]', "2021");
    await page.click('button:has-text("Siguiente")');

    // Step 2: Details
    await page.fill('input[name="mileage"]', "45000");
    await page.check('input[value="Good"]');
    await page.click('button:has-text("Siguiente")');

    // Step 3: History
    await page.check('input[value="false"]'); // No accidents
    await page.check('input[value="1"]'); // 1 owner
    await page.click('button:has-text("Siguiente")');

    // Step 4: Location
    await page.selectOption('select[name="province"]', "Distrito Nacional");
    await page.fill('input[name="city"]', "Santo Domingo");
    await page.click('button:has-text("Ver Mi Valuación")');

    // Should redirect to result page
    await expect(page).toHaveURL(/\/valuation\/result\//);

    // Should show valuation result
    await expect(page.locator("text=¡Valuación Lista!")).toBeVisible();
    await expect(page.locator("text=/RD\$/i")).toBeVisible();
  });

  test("should show deal rating badge on vehicle cards", async ({ page }) => {
    await page.goto("/vehicles");

    // Should show badge on first vehicle
    const badge = page.locator('[data-testid="deal-rating-badge"]').first();
    await expect(badge).toBeVisible();

    // Should show savings text for good deals
    const savings = page.locator("text=/por debajo del mercado/i").first();
    if (await savings.isVisible()) {
      await expect(savings).toContainText("RD$");
    }
  });

  test("should show price analysis on vehicle detail", async ({ page }) => {
    await page.goto("/vehicles/toyota-corolla-2023");

    // Should show price analysis section
    await expect(page.locator("text=Análisis de Precio")).toBeVisible();
    await expect(page.locator("text=Valor de mercado")).toBeVisible();

    // Should show price range slider
    await expect(
      page.locator('[data-testid="price-range-slider"]'),
    ).toBeVisible();

    // Should show pricing factors
    await expect(
      page.locator("text=Factores que afectan el precio"),
    ).toBeVisible();
  });
});
```

---

## 📊 MÉTRICAS DE ÉXITO

### KPIs para Deal Rating

| Métrica                   | Target | Descripción                      |
| ------------------------- | ------ | -------------------------------- |
| **Badge View Rate**       | >80%   | % de listings con badge visible  |
| **Tooltip Interaction**   | >15%   | % de usuarios que hacen hover    |
| **Great Deal Conversion** | +25%   | Conversión de Great Deal vs Fair |
| **Price Analysis Views**  | >40%   | % que ven sección en detail page |
| **Trust Score**           | >8/10  | Rating de confianza de usuarios  |

### KPIs para Valuación

| Métrica                  | Target | Descripción                      |
| ------------------------ | ------ | -------------------------------- |
| **Completion Rate**      | >60%   | % que completan wizard (4 steps) |
| **Lead Capture Rate**    | >50%   | % que dan email                  |
| **Valuation to Listing** | >30%   | % que publican después           |
| **Email Open Rate**      | >40%   | Emails con valuación             |
| **Return Rate**          | >20%   | Vuelven en 7 días                |

### Engagement Metrics

```typescript
// Analytics tracking

// Deal Rating
analytics.track("deal_rating_badge_viewed", {
  vehicleId,
  rating: dealRating.rating,
  priceDifference: dealRating.priceDifference,
  similarVehiclesCount: dealRating.similarVehiclesCount,
});

analytics.track("deal_rating_tooltip_opened", {
  vehicleId,
  rating: dealRating.rating,
});

analytics.track("price_analysis_section_viewed", {
  vehicleId,
  timeSpent: seconds,
});

// Valuation
analytics.track("valuation_wizard_started", {
  source: "homepage" | "navbar" | "footer",
});

analytics.track("valuation_step_completed", {
  step: 1 | 2 | 3 | 4,
  timeSpent: seconds,
});

analytics.track("valuation_completed", {
  vehicleMake: request.make,
  vehicleModel: request.model,
  estimatedValue: result.privatePartyValue,
  confidenceScore: result.confidenceScore,
});

analytics.track("valuation_lead_captured", {
  email: request.email,
  wantsToSell: true,
});

analytics.track("valuation_cta_clicked", {
  action: "publish_listing",
  fromValuationId: valuationId,
});
```

---

## 🎯 PRIORIZACIÓN POR SPRINTS

### Sprint 1: Deal Rating en Listings (🔴 CRÍTICA - 1.5 semanas)

**Objetivo:** Usuarios ven Deal Rating badge en todos los listings

**Entregables:**

- [x] DealRatingBadge component (120 LOC)
- [ ] Integration en VehicleCard
- [ ] Integration en SearchResultCard
- [ ] Integration en VehicleList
- [ ] Tooltip mejorado con más info
- [ ] pricingService.getDealRating() funcional
- [ ] Tests E2E del badge

**Story Points:** 25 SP

**Métricas de Éxito:**

- Badge visible en >95% de listings
- Tooltip interaction rate >15%
- 0 errores en producción

### Sprint 2: Price Analysis Section (🔴 ALTA - 2 semanas)

**Objetivo:** Usuarios ven análisis completo en vehicle detail page

**Entregables:**

- [x] PriceAnalysisSection component (450 LOC)
- [ ] Price range slider con posición actual
- [ ] Pricing factors list (positivos/negativos)
- [ ] Comparables info con confidence score
- [ ] Integration en VehicleDetailPage
- [ ] Responsive design (mobile/tablet/desktop)
- [ ] Tests unitarios + E2E

**Story Points:** 35 SP

**Métricas de Éxito:**

- > 40% de usuarios ven la sección
- Time on section >30 segundos
- Contact rate +20% vs sin sección

### Sprint 3: Valuación Wizard (🔴 CRÍTICA - 2.5 semanas)

**Objetivo:** Usuarios pueden obtener valuación instantánea de su vehículo

**Entregables:**

- [x] ValuationWizard component (600 LOC)
- [x] 4-step wizard con validación
- [ ] Formulario con progress bar
- [ ] Cascading selects (make → model → trim)
- [ ] Integration con pricingService
- [ ] Landing page `/sell/valuation`
- [ ] SEO optimization (title, meta, structured data)
- [ ] Tests E2E del flujo completo

**Story Points:** 40 SP

**Métricas de Éxito:**

- Completion rate >60%
- Average time <2 minutos
- 0 errores de validación no manejados

### Sprint 4: Valuación Result Page (🔴 ALTA - 2 semanas)

**Objetivo:** Usuarios ven resultado de valuación y CTA para publicar

**Entregables:**

- [x] ValuationResultPage component (500 LOC)
- [ ] 3 valores mostrados (Trade-In, Particular, Dealer)
- [ ] Value range slider visual
- [ ] Pricing factors con iconos
- [ ] Market trend indicator
- [ ] Email capture form
- [ ] CTA prominente "Publicar Mi Vehículo"
- [ ] Confetti animation en éxito
- [ ] Tests E2E

**Story Points:** 35 SP

**Métricas de Éxito:**

- Lead capture rate >50%
- Valuation to listing >30%
- Email open rate >40%
- CTA click rate >35%

### Sprint 5: Price History & Comparables (🟡 MEDIA - 2 semanas)

**Objetivo:** Usuarios ven gráfico de histórico de precios y vehículos comparables

**Entregables:**

- [ ] PriceHistoryChart component (Recharts)
- [ ] ComparablesList component
- [ ] Integration en PriceAnalysisSection
- [ ] Backend: historical price tracking
- [ ] API endpoints nuevos
- [ ] Tests

**Story Points:** 30 SP

**Dependencias:**

- Requiere data histórica (3+ meses)
- Backend debe trackear cambios de precio

---

## 💡 INSIGHTS DE PRODUCTO

### Comportamiento de Usuarios (Data Ficticia)

1. **Interacción con Deal Rating:**
   - 78% de usuarios notan el badge en <3 segundos
   - 22% hacen hover para ver tooltip
   - Listings con "Great Deal" tienen +45% conversion rate
   - "Fair Price" genera más confianza que sin badge (+18%)

2. **Valuación Wizard:**
   - 68% completan los 4 pasos
   - Abandono mayor en Step 2 (detalles): 15%
   - 52% dan email voluntariamente
   - 34% publican su vehículo en 7 días
   - Peak de uso: Domingos 3-6pm (investigación pre-venta)

3. **Factores de Precio:**
   - Top 3 factores positivos más comunes:
     - Sin accidentes: 78% de valuaciones
     - 1 solo dueño: 45% de valuaciones
     - Transmisión automática: 85% (mercado RD prefiere auto)
   - Top 3 factores negativos:
     - Alto kilometraje (>100K): 32%
     - Historial de accidentes: 12%
     - Ubicación remota: 8%

4. **Confianza en Valuación:**
   - Confidence Score promedio: 82% (High)
   - Usuarios con High Confidence publican 2.5x más
   - Valuaciones con <5 comparables: -40% trust score

### Oportunidades de Mejora

**Fase 2 (Q2 2026):**

- [ ] Price alerts: "Tu vehículo subió/bajó de precio"
- [ ] Valuation history: Track multiple valuations del mismo usuario
- [ ] Compare my car: Comparar tu valuación con listings similares
- [ ] Smart recommendations: "Agrega estas features para +RD$50K"

**Fase 3 (Q3 2026):**

- [ ] Predictive pricing: ML model que predice mejor precio de venta
- [ ] Seasonal adjustments: Ajustar por temporada (verano: SUVs ↑)
- [ ] Demand heatmaps: Mostrar dónde hay más demanda
- [ ] Trade-in calculator: Si quiere trade-in, cuánto recibiría + cuánto necesita financiar

---

## 🔗 REFERENCIAS

### Documentación Backend

- `/backend/PricingIntelligenceService/` (puerto 5090)
- Database: `pricingintelligenceservice`
- Controllers: DealRatingController, MarketValueController, ValuationController

### Endpoints Backend

| Método | Endpoint                                          | Descripción                 |
| ------ | ------------------------------------------------- | --------------------------- |
| `GET`  | `/api/pricing/deal-rating/:vehicleId`             | Deal rating de un vehículo  |
| `GET`  | `/api/pricing/market-value/:vehicleId`            | Valor de mercado estimado   |
| `GET`  | `/api/pricing/similar-prices`                     | Precios de comparables      |
| `GET`  | `/api/pricing/factors/:vehicleId`                 | Factores de precio          |
| `POST` | `/api/pricing/valuation/instant`                  | Crear valuación instantánea |
| `GET`  | `/api/pricing/valuation/:id`                      | Obtener valuación guardada  |
| `POST` | `/api/pricing/valuation/:id/claim`                | Reclamar con email          |
| `GET`  | `/api/pricing/valuation/makes`                    | Marcas disponibles          |
| `GET`  | `/api/pricing/valuation/models/:make`             | Modelos por marca           |
| `GET`  | `/api/pricing/valuation/trims/:make/:model/:year` | Versiones                   |

### Inspiración de Diseño

- **CarGurus Deal Rating** - Sistema de 5 niveles con badge
- **CarGurus Instant Market Value** - Wizard de valuación
- **Kavak Online Valuation** - Flujo simple y rápido
- **Kelley Blue Book (KBB)** - 3 valores (Trade-In, Private, Retail)
- **Edmunds True Market Value** - Pricing factors y comparables

### Documentos Relacionados

- [03-VEHICULOS-INVENTARIO/01-vehicles-sale-service.md](../process-matrix/03-VEHICULOS-INVENTARIO/01-vehicles-sale-service.md)
- [04-BUSQUEDA-FILTROS/03-filtros-avanzados.md](../process-matrix/04-BUSQUEDA-FILTROS/03-filtros-avanzados.md)
- [14-FINANCIAMIENTO-TRADEIN/02-trade-in-estimador.md](../process-matrix/14-FINANCIAMIENTO-TRADEIN/02-trade-in-estimador.md)

---

## ✅ CONCLUSIÓN

Se ha consolidado toda la documentación de Pricing Intelligence en un solo documento frontend-rebuild:

**Documentos Consolidados:**

- ✅ `01-deal-rating.md` (606 líneas, 15 procesos)
- ✅ `02-valuacion-instantanea.md` (498 líneas, 14 procesos)

**Componentes Principales:**

1. ✅ DealRatingBadge - Badge en listings (120 LOC)
2. ✅ PriceAnalysisSection - Análisis completo en detail page (450 LOC)
3. ✅ ValuationWizard - Formulario 4 pasos (600 LOC)
4. ✅ ValuationResultPage - Resultado con CTA (500 LOC)

**API Service:**

- ✅ pricingService.ts (300 LOC) - 12 métodos

**Total Procesos:** 29 (15 Deal Rating + 14 Valuación)

**Estado Backend:** ✅ 85% (muy alto)
**Estado UI:** 🔴 18% (necesita trabajo significativo)

**Próximos Pasos:**

1. **Sprint 1** (🔴 CRÍTICA): Deal Rating Badge - 25 SP
2. **Sprint 2** (🔴 ALTA): Price Analysis Section - 35 SP
3. **Sprint 3** (🔴 CRÍTICA): Valuación Wizard - 40 SP
4. **Sprint 4** (🔴 ALTA): Valuación Result Page - 35 SP

**Impacto Esperado:**

- Great Deal conversion: +45% vs Fair Price
- Valuation to listing: >30%
- Lead capture rate: >50%
- Trust score: >8/10

**Diferenciador Competitivo:**

- SuperCarros NO tiene deal rating ni valuación instantánea
- Este es el feature #1 que diferencia a OKLA del mercado

---

**✅ PRICING INTELLIGENCE COMPLETAMENTE DOCUMENTADO**

_29 procesos consolidados en componentes UI completos con código TypeScript/React listo para implementar._
