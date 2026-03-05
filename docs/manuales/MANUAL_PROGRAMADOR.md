# 🛠️ Manual del Programador — Configuración de APIs y Modelos

> **Plataforma OKLA** — Guía técnica para configurar los features implementados

---

## Índice

1. [Variables de Entorno](#1-variables-de-entorno)
2. [OKLA Score™ — APIs y Motor de Scoring](#2-okla-score--apis-y-motor-de-scoring)
3. [Sistema de Publicidad — Ad Engine y Targeting](#3-sistema-de-publicidad--ad-engine-y-targeting)
4. [Analytics & Tracking — Fingerprint, Leads, Pixels](#4-analytics--tracking--fingerprint-leads-pixels)
5. [Retargeting Pixels — Facebook, Google, TikTok](#5-retargeting-pixels--facebook-google-tiktok)
6. [Arquitectura de Archivos](#6-arquitectura-de-archivos)
7. [Modelos de Datos](#7-modelos-de-datos)
8. [Testing](#8-testing)

---

## 1. Variables de Entorno

Crea un archivo `.env.local` en `frontend/web-next/`:

```env
# ===== API Backend =====
NEXT_PUBLIC_API_URL=http://localhost:18443          # Gateway público (dev)
INTERNAL_API_URL=http://gateway-service:8080         # Gateway interno (K8s)

# ===== Google Analytics 4 =====
NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX           # GA4 Measurement ID

# ===== Facebook Pixel =====
NEXT_PUBLIC_FB_PIXEL_ID=123456789012345              # Facebook Pixel ID

# ===== Google Ads =====
NEXT_PUBLIC_GOOGLE_ADS_ID=AW-XXXXXXXXXXX             # Google Ads Account ID

# ===== TikTok Pixel =====
NEXT_PUBLIC_TIKTOK_PIXEL_ID=CXXXXXXXXXXXXXXXXX       # TikTok Pixel ID

# ===== Feature Flags =====
NEXT_PUBLIC_OKLA_SCORE_PHASE=1                       # Fase activa del Score (1-4)
```

---

## 2. OKLA Score™ — APIs y Motor de Scoring

### 2.1 APIs Externas Utilizadas

#### NHTSA vPIC API (Gratuita, sin API key)

- **Propósito:** Decodificación de VIN
- **Endpoint:** `https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVinValues/{vin}?format=json`
- **Cache:** 24 horas (`next: { revalidate: 86400 }`)
- **Archivo BFF:** `src/app/api/score/vin-decode/route.ts`
- **Datos extraídos:** Make, Model, Year, BodyClass, EngineCylinders, FuelType, Transmission, DriveType, PlantCountry, GVWR, ErrorCode

#### NHTSA Recalls API (Gratuita, sin API key)

- **Propósito:** Recalls activos por marca/modelo/año
- **Endpoint:** `https://api.nhtsa.gov/recalls/recallsByVehicle?make={make}&model={model}&modelYear={year}`
- **Cache:** 1 hora (`next: { revalidate: 3600 }`)
- **Archivo BFF:** `src/app/api/score/recalls/route.ts`

#### NHTSA Safety Ratings API (Gratuita, sin API key)

- **Propósito:** Calificaciones de seguridad NCAP
- **Endpoint:** `https://api.nhtsa.gov/SafetyRatings/modelyear/{year}/make/{make}/model/{model}`
- **Cache:** 24 horas
- **Archivo BFF:** `src/app/api/score/calculate/route.ts` (orquestador)

#### NHTSA Complaints API (Gratuita, sin API key)

- **Propósito:** Quejas de consumidores
- **Endpoint:** `https://api.nhtsa.gov/complaints/complaintsByVehicle?make={make}&model={model}&modelYear={year}`
- **Cache:** 1 hora

### 2.2 Motor de Scoring — Configuración

**Archivo:** `src/lib/okla-score-engine.ts`

#### Pesos de las Dimensiones (suman 100%)

```typescript
const DIMENSION_WEIGHTS = {
  D1_VIN_HISTORY: 0.25, // Historial VIN
  D2_MECHANICAL_SAFETY: 0.15, // Mecánica y Seguridad
  D3_MARKET_PRICE: 0.2, // Precio de Mercado
  D4_MILEAGE: 0.1, // Kilometraje
  D5_SELLER_TRUST: 0.1, // Confianza del Vendedor
  D6_DOCUMENTATION: 0.1, // Documentación
  D7_RECALLS: 0.1, // Recalls y Defectos
};
```

#### Umbrales de Niveles

```typescript
const SCORE_LEVELS = {
  EXCELLENT: 800, // 800-1000
  GOOD: 600, // 600-799
  ACCEPTABLE: 400, // 400-599
  RISK: 200, // 200-399
  CRITICAL: 0, // 0-199
};
```

#### Cómo se calcula cada dimensión

| Dimensión        | Peso | Fuente de Datos                | Lógica                                                         |
| ---------------- | ---- | ------------------------------ | -------------------------------------------------------------- |
| D1 VIN History   | 25%  | NHTSA VIN Decode               | ErrorCode=0 → 100%, patrones de salvage/flood → penalización   |
| D2 Mechanical    | 15%  | NHTSA Safety Ratings           | Stars (1-5) → score proporcional                               |
| D3 Market Price  | 20%  | Precio ingresado vs estimación | Ratio precio/mercado: < 0.85 = excelente, > 1.15 = sobreprecio |
| D4 Mileage       | 10%  | Km ingresado vs promedio       | avg = 20,000 km/año × edad; ratio km/avg                       |
| D5 Seller Trust  | 10%  | Backend (KYC, reseñas)         | Default 70% si no hay datos                                    |
| D6 Documentation | 10%  | Backend (docs subidos)         | Default 60% si no hay datos                                    |
| D7 Recalls       | 10%  | NHTSA Recalls                  | 0 recalls = 100%, >5 recalls = penalización severa             |

#### Análisis de Precio

```typescript
const PRICE_VERDICTS = {
  EXCELLENT_DEAL: ratio < 0.85, // >15% bajo el mercado
  GOOD_DEAL: ratio < 0.95, // 5-15% bajo el mercado
  FAIR_PRICE: ratio < 1.05, // ±5% del mercado
  ABOVE_MARKET: ratio < 1.15, // 5-15% sobre el mercado
  OVERPRICED: ratio >= 1.15, // >15% sobre el mercado
};
```

### 2.3 Fases de Implementación

Configuradas en `/admin/okla-score`:

| Fase             | APIs                          | Costo       |
| ---------------- | ----------------------------- | ----------- |
| 1: Tierra Fértil | NHTSA (gratuita)              | $0/mes      |
| 2: Confianza     | + MarketCheck, AutoData       | ~$200/mes   |
| 3: Inteligencia  | + Carfax, AutoCheck           | ~$500/mes   |
| 4: El Estándar   | + IA predictiva, VHR completo | ~$1,000/mes |

---

## 3. Sistema de Publicidad — Ad Engine y Targeting

### 3.1 Ad Engine (GSP Auction)

**Archivo:** `src/lib/ad-engine.ts`

#### Modelo de Subasta GSP (Generalized Second Price)

```
Ad Rank = MaxCPC × Quality Score
Winner pays = (NextAdRank / WinnerQualityScore) + $0.01
```

#### Quality Score — Fórmula

```typescript
Quality Score = (CTR × 0.35) + (Relevance × 0.40) + (Landing × 0.25)
```

| Factor          | Peso | Cómo se calcula                                                                                                  |
| --------------- | ---- | ---------------------------------------------------------------------------------------------------------------- |
| CTR (Expected)  | 35%  | Basado en: fotos (>5=+10%, >10=+20%), precio competitivo (+15%), vehículo reciente (+10%), condición "new" (+8%) |
| Relevance       | 40%  | Match entre el anuncio y la búsqueda del usuario (marca, modelo, precio, tipo)                                   |
| Landing Quality | 25%  | Número de fotos, longitud de descripción, completitud del listing                                                |

#### Purchase Intent Scoring

```typescript
const INTENT_WEIGHTS = {
  browse: 0.3, // Solo mirando
  buy: 1.0, // Quiere comprar
  sell: 0.1, // Quiere vender (baja relevancia para ads de compra)
  buy_and_sell: 0.7, // Ambos
};
```

#### Invalid Traffic (IVT) Detection

```typescript
const IVT_THRESHOLDS = {
  MAX_CLICKS_PER_MINUTE: 5, // Más de 5 clics/min = sospechoso
  MIN_VIEW_DURATION_MS: 500, // Menos de 500ms = bot
  MAX_RAPID_NAVIGATIONS: 10, // Navegación muy rápida
  SESSION_ANOMALY_THRESHOLD: 0.8, // Score de anomalía
};
```

### 3.2 Targeted Ad Rotation

**Archivo:** `src/app/api/advertising/targeted/route.ts`

#### Algoritmo de Targeting (6 factores)

```typescript
targetingScore =
  dealerSpend(0 - 40) +
  budgetRatio(0 - 15) +
  vehicleRelevance(0 - 25) +
  priceMatch(0 - 10) +
  qualityScore(0 - 10) +
  ctrBonus(0 - 10);
```

| Factor            | Puntos | Descripción                                       |
| ----------------- | ------ | ------------------------------------------------- |
| Dealer Spend      | 0-40   | `min(40, budget/1000 × (hotLead ? 2 : 1))`        |
| Budget Ratio      | 0-15   | `(remaining/total) × 15`                          |
| Vehicle Relevance | 0-25   | +25 si la marca coincide con preferencia del lead |
| Price Match       | 0-10   | +10 si el precio está en el rango del lead        |
| Quality Score     | 0-10   | `min(10, qualityScore × 2)`                       |
| CTR Bonus         | 0-10   | `min(10, ctr × 2)` si CTR > 2%                    |

#### Multiplicadores por Lead

- **Hot Lead** (score ≥ 60, budget ≥ 30K): × 1.5
- **Warm Lead** (score ≥ 35, budget ≥ 15K): × 1.2

#### Parámetros del API

```
GET /api/advertising/targeted?leadScore=75&section=FeaturedSpot&makes=Toyota,Honda&priceMin=1000000&priceMax=3000000&limit=6
```

### 3.3 Slots de Publicidad

```typescript
type AdSlotPosition =
  | "HomepageFeatured" // Carrusel principal homepage
  | "HomepageBanner" // Banner en homepage
  | "SearchTop" // Top de resultados de búsqueda
  | "SearchSidebar" // Sidebar de búsqueda
  | "DetailPageSidebar" // Sidebar en detalle de vehículo
  | "DetailPageBanner" // Banner en detalle de vehículo
  | "CategorySpotlight" // Spotlight en categorías
  | "ComparisonBanner" // Banner en comparaciones
  | "MapView" // Vista de mapa
  | "MobileInterstitial"; // Interstitial en móvil
```

---

## 4. Analytics & Tracking — Fingerprint, Leads, Pixels

### 4.1 Device Fingerprinting

**Archivo:** `src/lib/device-fingerprint.ts`

#### Método de Fingerprinting

```
fingerprint = FNV-1a Hash(canvas + webgl + screen + language + cores + memory + timezone + touch)
```

- **Canvas Fingerprint:** Dibuja texto y gradientes → toDataURL() → hash
- **WebGL Fingerprint:** Renderer + Vendor + extensions → hash
- **Combinado con:** screen resolution, pixel ratio, color depth, timezone offset, navigator.language, hardwareConcurrency, deviceMemory, maxTouchPoints

#### Session Management

- `sessionId` → `sessionStorage` (se pierde al cerrar la pestaña)
- `anonymousId` → `localStorage` (persiste entre sesiones)
- Formato: `sess_{timestamp}_{random}` / `anon_{timestamp}_{random}`

#### APIs

```typescript
getDeviceInfo(): DeviceInfo        // Toda la info del dispositivo
getSessionId(): string             // ID de sesión actual
getAnonymousId(): string           // ID anónimo persistente
getUtmParams(): Record<string,string>  // Parámetros UTM de la URL
```

### 4.2 Tracking Provider

**Archivo:** `src/components/providers/tracking-provider.tsx`

#### Configuración

```typescript
BATCH_INTERVAL_MS = 10_000; // Envía eventos cada 10 segundos
MAX_BATCH_SIZE = 50; // Máximo 50 eventos por batch
TRACKING_ENDPOINT = "/api/analytics/track";
```

#### Flujo de Datos

```
User Action → enqueueEvent() → eventQueue (array)
                                    ↓
                          cada 10s o 50 eventos
                                    ↓
                          flushEvents() → sendBeacon() → /api/analytics/track
                                    ↓                           ↓
                          (pixel forwarding)           Backend forwarding
                          FB/Google/TikTok
```

#### Eventos Trackeados Automáticamente

- `session_start` — Al montar el provider
- `page_view` — En cada cambio de pathname (incluye timeOnPreviousPage)

#### Eventos Manuales (via `useTracking()`)

```typescript
const { track, trackVehicleView } = useTracking();

track("search_performed", { query: "Toyota RAV4", make: "Toyota" });
trackVehicleView("vehicle-id", {
  make: "Toyota",
  model: "RAV4",
  price: 2800000,
});
```

### 4.3 Lead Prediction Engine

**Archivo:** `src/app/api/analytics/leads/route.ts`

#### Modelo de Scoring (4 dimensiones, 0-100 total)

| Dimensión  | Rango | Señales                                                                           |
| ---------- | ----- | --------------------------------------------------------------------------------- |
| Engagement | 0-25  | pageViews × 0.5 (cap 10), uniqueVehicles × 2 (cap 8), recency bonus, duration     |
| Intent     | 0-25  | searches × 1.5, filters × 1, gallery × 2, 360view × 3, comparisons × 2            |
| Contact    | 0-30  | callClicked × 10, whatsappClicked × 9, messageSent × 7, testDriveRequested × 12   |
| Financial  | 0-20  | financingCalc × 6, insuranceCheck × 5, paymentPageVisited × 8, favoritesAdded × 3 |

#### Clasificación de Leads

```typescript
if (score >= 60) level = "hot"; // 🔥 Alta probabilidad
if (score >= 35) level = "warm"; // ☀️ Interés moderado
if (score >= 10)
  level = "cold"; // ❄️ Explorando
else level = "inactive"; // 💤 Sin actividad
```

#### Probabilidad de Conversión (Logistic Regression)

```
P(conversion) = 1 / (1 + e^(-0.08 × (score - 50)))
```

#### Días Estimados para Compra

```typescript
if (hot) days = Math.max(1, Math.floor(7 * (1 - score / 100))); // 1-7 días
if (warm) days = Math.floor(7 + 23 * (1 - score / 60)); // 7-30 días
if (cold) days = 60;
```

### 4.4 In-Memory Event Store

**Archivo:** `src/app/api/analytics/track/route.ts`

```typescript
const eventStore = new Map<string, TrackEventRequest[]>(); // anonymousId → events
const sessionStore = new Map<string, SessionInfo>(); // sessionId → session

// Limits
MAX_EVENTS_PER_VISITOR = 500;
MAX_VISITORS = 10_000;
```

**⚠️ IMPORTANTE:** Este store es in-memory y se pierde al reiniciar el servidor. En producción, reemplazar con Redis o PostgreSQL via el backend .NET.

---

## 5. Retargeting Pixels — Facebook, Google, TikTok

### 5.1 Configuración

**Archivo:** `src/lib/retargeting-pixels.ts`

#### Facebook Pixel

1. Ir a [Meta Business Suite](https://business.facebook.com)
2. Events Manager → Crear Pixel
3. Copiar el Pixel ID (15 dígitos)
4. Configurar en `.env.local`: `NEXT_PUBLIC_FB_PIXEL_ID=123456789012345`
5. Para Auto Inventory Ads, configurar el Catálogo de Productos en Meta Business

#### Google Analytics 4

1. Ir a [Google Analytics](https://analytics.google.com)
2. Admin → Crear propiedad → Web Stream
3. Copiar el Measurement ID (formato G-XXXXXXXXXX)
4. Configurar en `.env.local`: `NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX`

#### Google Ads Remarketing

1. Ir a [Google Ads](https://ads.google.com)
2. Tools → Audience Manager → Audience sources
3. Configurar el tag de Google Ads
4. Copiar el ID (formato AW-XXXXXXXXXXX)
5. Configurar en `.env.local`: `NEXT_PUBLIC_GOOGLE_ADS_ID=AW-XXXXXXXXXXX`

#### TikTok Pixel

1. Ir a [TikTok Ads Manager](https://ads.tiktok.com)
2. Assets → Events → Web Events
3. Crear Pixel → Manual Setup
4. Copiar el Pixel ID (formato CXXXXXXXXXXXXXXXXX)
5. Configurar en `.env.local`: `NEXT_PUBLIC_TIKTOK_PIXEL_ID=CXXXXXXXXXXXXXXXXX`

### 5.2 Eventos Emitidos

| Evento OKLA    | Facebook           | Google                         | TikTok             |
| -------------- | ------------------ | ------------------------------ | ------------------ |
| Page View      | `PageView`         | `page_view`                    | `page()`           |
| Vehicle Search | `Search`           | `search`                       | `Search`           |
| Vehicle View   | `ViewContent`      | `view_item`                    | `ViewContent`      |
| Add Favorite   | `AddToWishlist`    | `add_to_wishlist`              | `AddToWishlist`    |
| Contact Dealer | `Lead`             | `generate_lead` + `conversion` | `SubmitForm`       |
| Financing Calc | `InitiateCheckout` | `begin_checkout`               | `InitiateCheckout` |
| User Login     | Advanced Matching  | `set user_properties`          | `identify`         |

### 5.3 Audiencias de Retargeting

Con los pixels configurados, puedes crear estas audiencias en cada plataforma:

| Audiencia               | Descripción                                          | Plataforma         |
| ----------------------- | ---------------------------------------------------- | ------------------ |
| Vehicle Searchers       | Personas que buscaron vehículos                      | FB, Google, TikTok |
| Vehicle Viewers         | Personas que vieron un vehículo específico           | FB, Google, TikTok |
| High-Intent (Favorites) | Personas que guardaron vehículos                     | FB, Google, TikTok |
| Contacted Dealers       | Personas que contactaron un vendedor                 | FB, Google, TikTok |
| Financing Intent        | Personas que usaron la calculadora de financiamiento | FB, Google, TikTok |

---

## 6. Arquitectura de Archivos

```
src/
├── types/
│   ├── okla-score.ts          # 388 líneas — Tipos del Score (OklaScoreResult, ScoreDimension, etc.)
│   ├── advertising.ts         # 285 líneas — Tipos de campañas, reportes, paquetes
│   ├── ads.ts                 # 554 líneas — Slots, formatos, subastas, IVT
│   └── analytics.ts           # 360 líneas — DeviceInfo, TrackingEvent, PredictedLead
│
├── lib/
│   ├── okla-score-engine.ts   # 688 líneas — Motor de scoring de 7 dimensiones
│   ├── ad-engine.ts           # 713 líneas — GSP Auction, Quality Score, IVT detection
│   ├── device-fingerprint.ts  # 306 líneas — Canvas/WebGL fingerprint, device detection
│   └── retargeting-pixels.ts  # 500 líneas — FB/Google/TikTok pixel integration
│
├── hooks/
│   ├── use-okla-score.ts      # VIN decode, score calculation, recalls, safety hooks
│   ├── use-advertising.ts     # 324 líneas — Campaign CRUD, rotation, reports
│   ├── use-ads.ts             # 233 líneas — Ad slot hooks with local engine fallback
│   └── use-ad-tracking.ts     # 336 líneas — Impression/click tracking, IVT detection
│
├── components/
│   ├── okla-score/
│   │   ├── score-gauge.tsx        # Circular SVG gauge
│   │   ├── score-badge.tsx        # Compact inline badge
│   │   ├── dimension-breakdown.tsx # 7-dimension bar chart
│   │   ├── price-analysis-card.tsx # Price verdict card
│   │   ├── score-alerts.tsx       # Critical/warning/info alerts
│   │   └── score-report.tsx       # Full report composition
│   │
│   ├── advertising/
│   │   └── native-ads.tsx         # 639 líneas — Native ad cards + impression tracking
│   │
│   ├── providers/
│   │   └── tracking-provider.tsx  # 293 líneas — Global tracking context + pixel integration
│   │
│   └── analytics/
│       └── google-analytics.tsx   # 426 líneas — GA4 full integration
│
├── app/
│   ├── api/
│   │   ├── score/
│   │   │   ├── calculate/route.ts # Orchestrates VIN decode → scoring
│   │   │   ├── vin-decode/route.ts # NHTSA VIN decode
│   │   │   └── recalls/route.ts   # NHTSA Recalls
│   │   │
│   │   ├── advertising/
│   │   │   ├── campaigns/route.ts  # Campaign CRUD proxy
│   │   │   ├── tracking/route.ts   # Impression/click proxy
│   │   │   ├── targeted/route.ts   # Targeted rotation for hot leads
│   │   │   ├── reports/route.ts    # Platform/campaign reports
│   │   │   └── advertiser-report/route.ts # Aggregated stats
│   │   │
│   │   └── analytics/
│   │       ├── track/route.ts      # Event tracking (batched)
│   │       └── leads/route.ts      # Lead prediction engine
│   │
│   ├── (main)/
│   │   ├── okla-score/page.tsx     # Public Score lookup
│   │   ├── dealer/publicidad/
│   │   │   ├── page.tsx            # Dealer ad dashboard
│   │   │   ├── estadisticas/page.tsx # Stats & reports
│   │   │   ├── roi/page.tsx        # ROI calculator
│   │   │   ├── nueva/page.tsx      # Create campaign wizard
│   │   │   └── paquetes/page.tsx   # Ad packages
│   │   ├── vender/publicidad/page.tsx # Seller boost hub
│   │   └── impulsar/
│   │       ├── page.tsx            # Boost landing
│   │       └── mis-campanas/page.tsx # My campaigns
│   │
│   └── (admin)/admin/
│       ├── okla-score/page.tsx     # Score phase config
│       ├── publicidad/page.tsx     # Ad management
│       ├── leads/page.tsx          # AI leads dashboard
│       └── analytics/page.tsx      # Platform analytics
```

---

## 7. Modelos de Datos

### 7.1 OklaScoreResult

```typescript
interface OklaScoreResult {
  overallScore: number; // 0-1000
  level: ScoreLevel; // 'excellent' | 'good' | 'acceptable' | 'risk' | 'critical'
  dimensions: ScoreDimension[]; // 7 dimensions with individual scores
  alerts: ScoreAlert[]; // Critical/warning/info alerts
  priceAnalysis?: PriceAnalysis; // Price vs market comparison
  vehicleInfo: DecodedVehicle; // VIN-decoded vehicle data
  metadata: ScoreMetadata; // Calculation timestamp, phase, APIs used
}
```

### 7.2 PredictedLead

```typescript
interface PredictedLead {
  visitorId: string;
  userId?: string;
  displayName: string;
  score: number; // 0-100
  level: LeadScoreLevel; // 'hot' | 'warm' | 'cold' | 'inactive'
  breakdown: LeadScoreBreakdown; // 4 dimensions
  signals: LeadSignal[]; // Behavioral signals detected
  interestedVehicles: InterestedVehicle[];
  preferences: VehiclePreference; // Inferred profile
  conversionProbability: number; // 0-1 (logistic regression)
  estimatedDaysToPurchase: number;
  recommendedAction: string;
  lastActiveAt: string;
  firstSeenAt: string;
  deviceInfo: { deviceType; browser; os };
}
```

### 7.3 CampaignAd (Targeting)

```typescript
interface CampaignAd {
  campaignId: string;
  vehicleId: string;
  vehicleTitle: string;
  vehiclePrice: number;
  vehicleMake?: string;
  dealerId: string;
  dealerName: string;
  placementType: string;
  totalBudget: number;
  spent: number;
  remainingBudget: number;
  qualityScore?: number;
  ctr?: number;
  targetingScore?: number; // Computed by targeting algorithm
  boostReason?: string; // Why this ad was boosted
}
```

---

## 8. Testing

### 8.1 Verificar APIs de Score

```bash
# Test VIN decode
curl http://localhost:3000/api/score/vin-decode?vin=1HGBH41JXMN109186

# Test full score calculation
curl -X POST http://localhost:3000/api/score/calculate \
  -H "Content-Type: application/json" \
  -d '{"vin": "1HGBH41JXMN109186", "askingPrice": 1500000, "mileage": 80000}'

# Test recalls
curl http://localhost:3000/api/score/recalls?make=Honda&model=Civic&year=2021
```

### 8.2 Verificar Tracking

```bash
# Send test events
curl -X POST http://localhost:3000/api/analytics/track \
  -H "Content-Type: application/json" \
  -d '{
    "events": [{
      "eventType": "page_view",
      "sessionId": "test-sess",
      "anonymousId": "test-anon",
      "pageUrl": "/vehiculos",
      "properties": {"path": "/vehiculos"}
    }],
    "device": {"deviceType": "desktop", "os": "macOS", "browser": "Chrome"},
    "session": {"sessionId": "test-sess", "anonymousId": "test-anon"}
  }'

# Get leads
curl http://localhost:3000/api/analytics/leads

# Get leads filtered
curl "http://localhost:3000/api/analytics/leads?level=hot&sortBy=score"
```

### 8.3 Verificar Targeted Ads

```bash
# Get targeted ads for a hot lead interested in Toyota
curl "http://localhost:3000/api/advertising/targeted?leadScore=75&makes=Toyota,Honda&priceMin=1000000&priceMax=3000000&limit=3"
```

### 8.4 Verificar Pixels

Abrir el navegador en la app y verificar en:

- **Facebook:** Meta Pixel Helper (extensión de Chrome) → debe mostrar eventos PageView, ViewContent, Search
- **Google:** Google Tag Assistant → debe mostrar gtag configurado
- **TikTok:** TikTok Pixel Helper → debe mostrar eventos page, ViewContent
- **Network tab:** Buscar requests a `connect.facebook.net`, `googletagmanager.com`, `analytics.tiktok.com`

---

## ⚠️ Notas Importantes

1. **Event Store in-memory:** El store de eventos (`Map`) se pierde al reiniciar. En producción, implementar persistencia con Redis o PostgreSQL a través del backend .NET.

2. **NHTSA Rate Limits:** Las APIs de NHTSA no tienen documentación oficial de rate limits, pero se recomienda cachear agresivamente (24h para VIN decode, 1h para recalls).

3. **Pixel Data Privacy:** Los pixels recopilan datos según GDPR/CCPA. Implementar banner de consentimiento de cookies antes de producción.

4. **GSP Auction Fairness:** El ad engine calcula el "second price" pero actualmente no cobra en tiempo real. La integración con el sistema de billing del backend es necesaria.

5. **Lead Scoring Calibration:** Los pesos del modelo de scoring están optimizados para el mercado dominicano. Monitorear y ajustar según datos reales después de 30 días de producción.

---

## 9. Sistema de Etapas (Stage Configuration)

> **Actualizado:** Marzo 2026

### 9.1 Arquitectura

El sistema de etapas controla la habilitación gradual de features sin modificar código.

**Archivos clave:**
- `src/lib/stage-config.ts` — Configuración central con 4 etapas y feature flags
- `src/hooks/use-stage-config.ts` — Hook de React para consumir la configuración

### 9.2 Variables de Entorno

```env
NEXT_PUBLIC_OKLA_STAGE=1        # Etapa: 1=Desarrollo, 2=Beta, 3=Crecimiento, 4=Escala
NEXT_PUBLIC_OKLA_SCORE_PHASE=1  # Fase Score: 1=NHTSA Free, 2=VinAudit Basic, 3=Full
```

### 9.3 Uso en Componentes

```typescript
import { useStageConfig } from '@/hooks/use-stage-config';

function MyComponent() {
  const { stage, scorePhase, features, isEnabled } = useStageConfig();
  
  if (!isEnabled('oklaScore', 'nhtsaRecalls')) {
    return <FeatureNotAvailable />;
  }
  
  return <ScoreDisplay />;
}
```

### 9.4 Categorías de Features

- `oklaScore` — Dimensiones del score (NHTSA, VinAudit, Carfax, etc.)
- `media` — Vista 360°, video upload, background removal
- `advertising` — Campañas, retargeting, analytics
- `sales` — Transacciones, contrato digital, escrow
- `dealerErp` — CRM, facturación NCF, contabilidad
- `platform` — AI Search, chatbot, comparador
- `infrastructure` — Redis cache, CDN, multi-región

---

## 10. SaleTransaction — Modelo de Datos

> **Actualizado:** Marzo 2026

### 10.1 Entidad

Ubicación: `backend/VehiclesSaleService/VehiclesSaleService.Domain/Entities/SaleTransaction.cs`

```
SaleTransaction
├── Id (Guid)
├── VehicleId, SellerId, BuyerId, BuyerEmail
├── ListedPrice, SalePrice → PriceDifference (computed), DiscountPercentage (computed)
├── ListedAt, SoldAt → DaysToSell (computed)
├── VehicleTitle, Vin, Make, Model, Year
├── ConfidenceLevel (Rejected/Low/Medium/High)
├── FraudScore (0-100)
├── BuyerConfirmed, IpAddress, UserAgent
├── Currency, TenantId, Notes
└── CreatedAt, UpdatedAt
```

### 10.2 Creación Automática

La transacción se crea automáticamente en `VehiclesController.MarkAsSold()`:
1. Se captura el precio de lista antes de actualizar el vehículo
2. Se crea un `SaleTransaction` con datos del vehículo e IP/UserAgent
3. Se publica `VehicleSoldEvent` con el ID de la transacción

### 10.3 Tabla de Base de Datos

- Tabla: `sale_transactions`
- Índices: VehicleId, SellerId, BuyerId, SoldAt, BuyerEmail
- Enum `ConfidenceLevel` almacenado como string

---

## 11. Gateway — Configuración de Roles

> **Actualizado:** Marzo 2026

### 11.1 Roles en Ocelot

Las rutas en `ocelot.prod.json` usan `RouteClaimsRequirement` para controlar acceso:

| Patrón de Ruta | Rol Requerido |
|----------------|--------------|
| `/api/admin/{everything}` | `Admin` |
| `/api/kyc/*` (review endpoints) | `Compliance` |
| `/api/configurations/*` | `Compliance` |
| `/api/featureflags/*` | `Compliance` |
| `/api/ai/stats/queue` | `Admin` |

### 11.2 Puertos Internos

- **Producción (DOKS):** Todos los servicios usan puerto **8080**
- **Desarrollo (Docker Compose):** Servicios usan puerto **80**
