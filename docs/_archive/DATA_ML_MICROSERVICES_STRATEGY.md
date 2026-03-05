# 🤖 Estrategia de Data & Machine Learning - OKLA Marketplace

**Fecha:** Enero 8, 2026  
**Objetivo:** Recopilar, organizar y aprovechar datos para entrenar modelos de ML  
**Beneficiarios:** Dealers, Vendedores Individuales y Compradores

---

## 📋 RESUMEN EJECUTIVO

Para convertir a OKLA en el mejor marketplace de vehículos de República Dominicana, necesitamos:

1. **Recopilar TODO** lo que hacen los usuarios (eventos, clicks, búsquedas, tiempo en página)
2. **Organizar datos** de vehículos de forma estructurada para ML
3. **Entrenar modelos** para recomendaciones, scoring de leads, pricing
4. **Entregar insights** accionables a dealers, vendedores y compradores

---

## 🆕 NUEVOS MICROSERVICIOS DE DATA & ML

### Arquitectura de Datos

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          FRONTEND (Web/Mobile)                               │
│                                                                              │
│  [Clicks] [Views] [Searches] [Scrolls] [Favorites] [Time on Page] [Shares]  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    1. EVENT TRACKING SERVICE (5050)                          │
│  Recopila TODOS los eventos del usuario en tiempo real                      │
│  - Page views, clicks, scrolls, hovers                                      │
│  - Búsquedas realizadas, filtros aplicados                                  │
│  - Tiempo en cada página/vehículo                                           │
│  - Interacciones (favoritos, compartir, contactar)                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
┌─────────────────────────────────┐   ┌─────────────────────────────────────┐
│  2. DATA PIPELINE SERVICE       │   │  3. USER BEHAVIOR SERVICE (5052)    │
│        (5051)                   │   │  Perfil de comportamiento por user  │
│  ETL, transformación,           │   │  - Preferencias inferidas           │
│  normalización                  │   │  - Historial de acciones            │
│                                 │   │  - Segmentación automática          │
└─────────────────────────────────┘   └─────────────────────────────────────┘
                    │                               │
                    ▼                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    4. FEATURE STORE SERVICE (5053)                           │
│  Almacén centralizado de features para ML                                   │
│  - Features de usuarios (comportamiento, preferencias)                      │
│  - Features de vehículos (popularidad, velocidad de venta)                  │
│  - Features de dealers (performance, rating)                                │
│  - Features de mercado (demanda por categoría, tendencias)                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
┌─────────────────────┐ ┌─────────────────┐ ┌─────────────────────────────────┐
│ 5. RECOMMENDATION   │ │ 6. LEAD SCORING │ │ 7. VEHICLE INTELLIGENCE         │
│    SERVICE (5054)   │ │   SERVICE (5055)│ │    SERVICE (5056)               │
│                     │ │                 │ │                                 │
│ - Vehículos para ti │ │ - Hot/Warm/Cold │ │ - Pricing óptimo                │
│ - Similar vehicles  │ │ - Probabilidad  │ │ - Demanda predictiva            │
│ - Compradores para  │ │   de conversión │ │ - Tiempo estimado de venta      │
│   tu vehículo       │ │ - Priorización  │ │ - Anomalías (precio muy bajo)   │
└─────────────────────┘ └─────────────────┘ └─────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    8. ML TRAINING SERVICE (5057)                             │
│  Pipeline de entrenamiento de modelos                                       │
│  - Scheduled training jobs                                                  │
│  - Model versioning                                                         │
│  - A/B testing de modelos                                                   │
│  - Model monitoring                                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. 📡 EVENT TRACKING SERVICE (Puerto 5050) ⭐⭐⭐ CRÍTICO

### ¿Por qué es necesario?
- **Sin datos no hay ML** - Necesitamos capturar CADA interacción
- Los eventos son la materia prima para todos los modelos
- Permite entender el journey completo del usuario

### Eventos a Capturar

#### 👤 Eventos de Usuario (Buyer/Seller)
```csharp
public enum UserEventType
{
    // Navegación
    PageView,                    // Vio una página
    VehicleView,                 // Vio detalle de vehículo
    VehicleListView,             // Vio lista de vehículos
    DealerProfileView,           // Vio perfil de dealer
    
    // Búsqueda
    SearchPerformed,             // Realizó búsqueda
    FilterApplied,               // Aplicó filtro
    SortChanged,                 // Cambió ordenamiento
    SearchResultsViewed,         // Vio resultados
    
    // Engagement
    VehicleFavorited,            // Añadió a favoritos
    VehicleUnfavorited,          // Quitó de favoritos
    VehicleShared,               // Compartió vehículo
    VehicleCompared,             // Añadió a comparar
    PhotoGalleryViewed,          // Vio galería completa
    PhotoZoomed,                 // Zoom en foto
    VideoPlayed,                 // Reprodujo video
    
    // Interacción
    ContactFormOpened,           // Abrió formulario contacto
    ContactFormSubmitted,        // Envió formulario
    ChatStarted,                 // Inició chat
    MessageSent,                 // Envió mensaje
    PhoneNumberRevealed,         // Vio teléfono (si aplica)
    
    // Financiamiento
    FinanceCalculatorUsed,       // Usó calculadora
    FinanceApplicationStarted,   // Inició solicitud
    FinanceApplicationSubmitted, // Envió solicitud
    
    // Appointments
    TestDriveScheduled,          // Agendó test drive
    TestDriveCompleted,          // Completó test drive
    TestDriveCanceled,           // Canceló test drive
    
    // Conversión
    PurchaseIntentShown,         // Mostró intención de compra
    OfferMade,                   // Hizo oferta
    OfferAccepted,               // Oferta aceptada
    VehiclePurchased,            // Compró vehículo
    
    // Sesión
    SessionStarted,              // Inició sesión
    SessionEnded,                // Cerró sesión
    AppOpened,                   // Abrió app móvil
    PushNotificationReceived,    // Recibió push
    PushNotificationClicked,     // Clickeó push
    
    // Engagement negativo
    VehicleReported,             // Reportó vehículo
    UnsubscribedFromAlerts,      // Se desuscribió
    AccountDeleted               // Eliminó cuenta
}
```

#### 🏢 Eventos de Dealer
```csharp
public enum DealerEventType
{
    // Inventario
    VehicleCreated,              // Creó listing
    VehicleUpdated,              // Actualizó listing
    VehicleDeleted,              // Eliminó listing
    VehiclePriceChanged,         // Cambió precio
    VehiclePhotosAdded,          // Añadió fotos
    BulkImportPerformed,         // Importación masiva
    
    // Leads
    LeadReceived,                // Recibió lead
    LeadViewed,                  // Vio lead
    LeadContacted,               // Contactó lead
    LeadStatusChanged,           // Cambió estado de lead
    LeadConvertedToSale,         // Lead → Venta
    LeadLost,                    // Lead perdido
    
    // Dashboard
    DashboardViewed,             // Vio dashboard
    ReportDownloaded,            // Descargó reporte
    AnalyticsViewed,             // Vio analytics
    
    // Promoción
    VehicleFeatured,             // Destacó vehículo
    PromotionCreated,            // Creó promoción
    
    // Configuración
    SubscriptionChanged,         // Cambió plan
    TeamMemberAdded,             // Añadió vendedor
    SettingsUpdated              // Actualizó config
}
```

### Entidades Principales

```csharp
public class UserEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }           // UserEventType como string
    public Guid? UserId { get; set; }               // Null para anónimos
    public string SessionId { get; set; }           // ID de sesión
    public string DeviceId { get; set; }            // Fingerprint del device
    public DateTime Timestamp { get; set; }
    
    // Contexto del evento
    public Guid? VehicleId { get; set; }
    public Guid? DealerId { get; set; }
    public string SearchQuery { get; set; }
    public Dictionary<string, string> Filters { get; set; }
    
    // Métricas
    public int? DurationMs { get; set; }            // Tiempo en página
    public int? ScrollDepthPercent { get; set; }    // Cuánto scrolleó
    public int? ClickPosition { get; set; }         // Posición del click
    
    // Contexto técnico
    public string UserAgent { get; set; }
    public string IpAddress { get; set; }           // Hasheado
    public string Referrer { get; set; }
    public string PageUrl { get; set; }
    public string DeviceType { get; set; }          // mobile/desktop/tablet
    public string Browser { get; set; }
    public string OS { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    
    // Metadata adicional
    public Dictionary<string, object> Properties { get; set; }
}

public class DealerEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public Guid DealerId { get; set; }
    public Guid? UserId { get; set; }               // Usuario del dealer
    public DateTime Timestamp { get; set; }
    
    // Contexto
    public Guid? VehicleId { get; set; }
    public Guid? LeadId { get; set; }
    public string Action { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    
    public Dictionary<string, object> Properties { get; set; }
}
```

### Tecnologías
- **Ingesta:** Apache Kafka / RabbitMQ Streams
- **Almacenamiento Raw:** ClickHouse / TimescaleDB
- **Procesamiento Real-time:** Apache Flink / Kafka Streams
- **SDK Frontend:** Custom JavaScript SDK + Mobile SDK

### Endpoints
```
POST /api/events/track           - Registrar evento individual
POST /api/events/batch           - Registrar batch de eventos
GET  /api/events/user/{userId}   - Eventos de un usuario
GET  /api/events/vehicle/{id}    - Eventos de un vehículo
GET  /api/events/session/{id}    - Eventos de una sesión
```

---

## 2. 🔄 DATA PIPELINE SERVICE (Puerto 5051) ⭐⭐⭐ CRÍTICO

### ¿Por qué es necesario?
- Los datos raw necesitan transformación y limpieza
- Normalización para que ML pueda consumirlos
- Agregaciones para reportes y analytics

### Funcionalidades

```csharp
// Pipelines principales
public enum PipelineType
{
    // Procesamiento de eventos
    EventAggregation,            // Agregar eventos por usuario/vehículo/día
    SessionReconstruction,       // Reconstruir sesiones completas
    JourneyMapping,              // Mapear customer journey
    
    // Transformaciones
    FeatureExtraction,           // Extraer features para ML
    DataNormalization,           // Normalizar valores
    OutlierDetection,            // Detectar anomalías
    
    // Agregaciones
    DailyMetrics,                // Métricas diarias
    WeeklyReports,               // Reportes semanales
    VehiclePerformanceCalc,      // Calcular performance de vehículos
    DealerScoreCalc,             // Calcular score de dealers
    
    // ML Preparation
    TrainingDataPrep,            // Preparar datos para training
    FeatureStoreUpdate,          // Actualizar Feature Store
    ModelInputPrep               // Preparar inputs para modelos
}
```

### Pipelines Específicos

#### Pipeline: Vehicle Performance Score
```python
# Calcular score de performance de cada vehículo
vehicle_performance = (
    views_last_7_days * 0.1 +
    favorites_count * 0.2 +
    contact_requests * 0.3 +
    test_drives_scheduled * 0.25 +
    time_on_page_avg * 0.15
) / days_listed

# Output: VehiclePerformanceScore (0-100)
```

#### Pipeline: User Interest Profile
```python
# Construir perfil de intereses del usuario
user_profile = {
    'preferred_makes': top_5_makes_viewed,
    'preferred_body_types': most_viewed_body_types,
    'price_range': (min_viewed, max_viewed, avg_viewed),
    'year_preference': (min_year, max_year),
    'location_preference': most_searched_locations,
    'engagement_level': calculate_engagement_score(),
    'purchase_probability': predict_conversion()
}
```

### Entidades

```csharp
public class Pipeline
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public PipelineType Type { get; set; }
    public string CronSchedule { get; set; }        // "0 0 * * *" = diario
    public bool IsActive { get; set; }
    public DateTime LastRunAt { get; set; }
    public TimeSpan AvgDuration { get; set; }
    public string Configuration { get; set; }       // JSON config
}

public class PipelineRun
{
    public Guid Id { get; set; }
    public Guid PipelineId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; }              // Running, Completed, Failed
    public long RecordsProcessed { get; set; }
    public string ErrorMessage { get; set; }
}

public class AggregatedMetric
{
    public Guid Id { get; set; }
    public string MetricType { get; set; }
    public Guid? EntityId { get; set; }             // VehicleId, UserId, DealerId
    public string EntityType { get; set; }
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public Dictionary<string, decimal> Dimensions { get; set; }
}
```

### Tecnologías
- **Orquestación:** Apache Airflow / Dagster / Prefect
- **Procesamiento:** Apache Spark / dbt
- **Almacenamiento:** PostgreSQL + Data Warehouse (BigQuery/Snowflake)

---

## 3. 👤 USER BEHAVIOR SERVICE (Puerto 5052) ⭐⭐⭐ CRÍTICO

### ¿Por qué es necesario?
- Cada usuario tiene preferencias únicas
- Permite personalización y mejores recomendaciones
- Segmentación automática para marketing

### Funcionalidades

#### Perfiles de Comportamiento

```csharp
public class UserBehaviorProfile
{
    public Guid UserId { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Preferencias de vehículos (inferidas de comportamiento)
    public VehiclePreferences VehiclePreferences { get; set; }
    
    // Comportamiento de navegación
    public NavigationBehavior NavigationBehavior { get; set; }
    
    // Estado en el funnel
    public FunnelPosition FunnelPosition { get; set; }
    
    // Segmentación
    public List<string> Segments { get; set; }
    
    // Scores calculados
    public Dictionary<string, decimal> Scores { get; set; }
}

public class VehiclePreferences
{
    // Marcas preferidas (ordenadas por frecuencia de vista)
    public List<RankedItem> PreferredMakes { get; set; }
    
    // Modelos preferidos
    public List<RankedItem> PreferredModels { get; set; }
    
    // Tipos de carrocería
    public List<RankedItem> PreferredBodyTypes { get; set; }
    
    // Rango de precio
    public decimal PriceMin { get; set; }
    public decimal PriceMax { get; set; }
    public decimal PriceAvg { get; set; }
    
    // Rango de año
    public int YearMin { get; set; }
    public int YearMax { get; set; }
    
    // Kilometraje
    public int MileageMax { get; set; }
    
    // Colores preferidos
    public List<RankedItem> PreferredColors { get; set; }
    
    // Combustible
    public List<string> PreferredFuelTypes { get; set; }
    
    // Transmisión
    public string PreferredTransmission { get; set; }
    
    // Ubicación
    public List<string> PreferredLocations { get; set; }
    
    // Confianza en las preferencias (0-1)
    public decimal ConfidenceScore { get; set; }
}

public class NavigationBehavior
{
    // Tiempo promedio en página de vehículo
    public TimeSpan AvgTimeOnVehiclePage { get; set; }
    
    // Páginas vistas por sesión
    public decimal AvgPagesPerSession { get; set; }
    
    // Frecuencia de visita
    public int VisitsLast30Days { get; set; }
    public int VisitsLast7Days { get; set; }
    
    // Dispositivo preferido
    public string PrimaryDevice { get; set; }
    
    // Horas de actividad (cuándo navega más)
    public List<int> PeakHours { get; set; }
    
    // Días de actividad
    public List<DayOfWeek> PeakDays { get; set; }
    
    // Patrones de scroll
    public decimal AvgScrollDepth { get; set; }
    
    // Interacción con fotos
    public bool ViewsFullGallery { get; set; }
    public decimal AvgPhotosViewedPerVehicle { get; set; }
}

public enum FunnelPosition
{
    Browsing,                    // Solo mirando
    Researching,                 // Investigando activamente
    Comparing,                   // Comparando opciones
    ReadyToBuy,                  // Listo para comprar
    Contacted,                   // Ya contactó vendedor
    TestDriveScheduled,          // Agendó test drive
    Negotiating,                 // Negociando
    Purchased                    // Compró
}
```

#### Segmentación Automática

```csharp
public enum UserSegment
{
    // Por intención
    JustBrowsing,                // Solo mira, no interactúa
    SeriousBuyer,                // Alto engagement
    FirstTimeBuyer,              // Primera vez comprando
    RepeatBuyer,                 // Ya compró antes
    
    // Por presupuesto
    BudgetBuyer,                 // < $15,000
    MidRangeBuyer,               // $15,000 - $35,000
    PremiumBuyer,                // $35,000 - $60,000
    LuxuryBuyer,                 // > $60,000
    
    // Por tipo de vehículo
    SUVEnthusiast,               // Prefiere SUVs
    SedanLover,                  // Prefiere sedanes
    TruckBuyer,                  // Busca camionetas
    SportsCar Fan,               // Busca deportivos
    FamilyCar,                   // Busca vehículo familiar
    EcoFriendly,                 // Busca eléctricos/híbridos
    
    // Por comportamiento
    QuickDecider,                // Toma decisiones rápido
    ResearchHeavy,               // Investiga mucho
    PriceSensitive,              // Muy sensible al precio
    BrandLoyal,                  // Fiel a una marca
    
    // Por engagement
    HighlyEngaged,               // Muy activo
    LowEngagement,               // Poco activo
    ChurnRisk,                   // Riesgo de abandono
    WinBack                      // Puede recuperarse
}
```

### Endpoints

```
GET  /api/behavior/user/{userId}              - Perfil completo de usuario
GET  /api/behavior/user/{userId}/preferences  - Preferencias inferidas
GET  /api/behavior/user/{userId}/segments     - Segmentos del usuario
GET  /api/behavior/user/{userId}/funnel       - Posición en funnel
POST /api/behavior/segment/users              - Usuarios por segmento
GET  /api/behavior/similar-users/{userId}     - Usuarios similares
```

---

## 4. 📦 FEATURE STORE SERVICE (Puerto 5053) ⭐⭐⭐ CRÍTICO

### ¿Por qué es necesario?
- Centraliza todas las features para ML
- Evita duplicación de cálculos
- Consistencia entre training e inference
- Versionado de features

### Categorías de Features

#### Features de Usuario
```csharp
public class UserFeatures
{
    public Guid UserId { get; set; }
    public DateTime ComputedAt { get; set; }
    
    // Engagement
    public int TotalViews { get; set; }
    public int ViewsLast7Days { get; set; }
    public int TotalFavorites { get; set; }
    public int TotalContacts { get; set; }
    public decimal AvgSessionDuration { get; set; }
    public decimal AvgPagesPerSession { get; set; }
    
    // Actividad
    public int DaysSinceFirstVisit { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public int TotalSessions { get; set; }
    public decimal SessionFrequency { get; set; }    // Sessions per week
    
    // Preferencias numéricas
    public decimal AvgPriceViewed { get; set; }
    public decimal AvgYearViewed { get; set; }
    public decimal AvgMileageViewed { get; set; }
    
    // Conversión
    public int TestDrivesScheduled { get; set; }
    public int OffersSubmitted { get; set; }
    public int Purchases { get; set; }
    
    // Scores
    public decimal EngagementScore { get; set; }     // 0-100
    public decimal ConversionProbability { get; set; } // 0-1
    public decimal ChurnRisk { get; set; }           // 0-1
}
```

#### Features de Vehículo
```csharp
public class VehicleFeatures
{
    public Guid VehicleId { get; set; }
    public DateTime ComputedAt { get; set; }
    
    // Popularidad
    public int TotalViews { get; set; }
    public int ViewsLast7Days { get; set; }
    public int UniqueBuyers { get; set; }
    public int Favorites { get; set; }
    public int ContactRequests { get; set; }
    public int TestDrives { get; set; }
    
    // Engagement
    public decimal AvgTimeOnPage { get; set; }
    public decimal AvgScrollDepth { get; set; }
    public decimal PhotoViewRate { get; set; }       // % que ve todas las fotos
    
    // Performance
    public int DaysListed { get; set; }
    public decimal ViewsPerDay { get; set; }
    public decimal ContactRate { get; set; }         // Contacts / Views
    public decimal ConversionRate { get; set; }      // Sales / Contacts
    
    // Precio
    public decimal PriceVsMarket { get; set; }       // 1.0 = mismo que mercado
    public int PriceChanges { get; set; }
    public decimal LastPriceChange { get; set; }
    
    // Calidad del listing
    public int PhotoCount { get; set; }
    public bool HasVideo { get; set; }
    public int DescriptionLength { get; set; }
    public decimal ListingCompleteness { get; set; } // 0-100
    
    // Scores
    public decimal PopularityScore { get; set; }     // 0-100
    public decimal QualityScore { get; set; }        // 0-100
    public decimal ValueScore { get; set; }          // 0-100 (value for money)
    public int PredictedDaysToSale { get; set; }
}
```

#### Features de Dealer
```csharp
public class DealerFeatures
{
    public Guid DealerId { get; set; }
    public DateTime ComputedAt { get; set; }
    
    // Inventario
    public int ActiveListings { get; set; }
    public int TotalListingsAllTime { get; set; }
    public decimal AvgListingPrice { get; set; }
    public decimal AvgDaysToSale { get; set; }
    
    // Performance
    public int TotalSales { get; set; }
    public int SalesLast30Days { get; set; }
    public decimal SalesVelocity { get; set; }       // Sales per month
    public decimal InventoryTurnover { get; set; }
    
    // Leads
    public int TotalLeads { get; set; }
    public int LeadsLast30Days { get; set; }
    public decimal LeadConversionRate { get; set; }
    public decimal AvgResponseTime { get; set; }     // Minutes
    
    // Engagement
    public int TotalProfileViews { get; set; }
    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }
    
    // Calidad
    public decimal AvgListingQuality { get; set; }
    public decimal AvgPhotosPerListing { get; set; }
    public decimal VideoListingRate { get; set; }
    
    // Scores
    public decimal ReputationScore { get; set; }     // 0-100
    public decimal PerformanceScore { get; set; }    // 0-100
    public decimal TrustScore { get; set; }          // 0-100
}
```

#### Features de Mercado
```csharp
public class MarketFeatures
{
    public DateTime Date { get; set; }
    public string Market { get; set; }               // "Santo Domingo", "Nacional"
    
    // Demanda
    public int TotalSearches { get; set; }
    public int UniqueSearchers { get; set; }
    public Dictionary<string, int> SearchesByMake { get; set; }
    public Dictionary<string, int> SearchesByBodyType { get; set; }
    
    // Inventario
    public int ActiveListings { get; set; }
    public int NewListingsToday { get; set; }
    public int SoldToday { get; set; }
    
    // Precios
    public decimal AvgPrice { get; set; }
    public decimal MedianPrice { get; set; }
    public Dictionary<string, decimal> AvgPriceByMake { get; set; }
    
    // Tendencias
    public decimal DemandVsSupply { get; set; }      // > 1 = más demanda
    public List<string> TrendingMakes { get; set; }
    public List<string> TrendingModels { get; set; }
}
```

### Endpoints

```
GET  /api/features/user/{userId}          - Features de usuario
GET  /api/features/vehicle/{vehicleId}    - Features de vehículo
GET  /api/features/dealer/{dealerId}      - Features de dealer
GET  /api/features/market/{date}          - Features de mercado
POST /api/features/batch                  - Batch de features (para ML)
GET  /api/features/version/{version}      - Features por versión
```

---

## 5. 🎯 RECOMMENDATION SERVICE (Puerto 5054) ⭐⭐⭐ CRÍTICO

### ¿Por qué es necesario?
- Personalización = más engagement = más conversión
- "Vehículos para ti" aumenta tiempo en sitio
- "Compradores interesados" es MEGA valioso para dealers

### Tipos de Recomendaciones

#### Para Compradores
```csharp
public class BuyerRecommendations
{
    // Recomendaciones personalizadas basadas en historial
    public List<VehicleRecommendation> ForYou { get; set; }
    
    // Vehículos similares a uno que está viendo
    public List<VehicleRecommendation> SimilarTo(Guid vehicleId) { get; }
    
    // Basado en lo que vieron usuarios similares
    public List<VehicleRecommendation> UsersAlsoViewed { get; set; }
    
    // Nuevos listings que le pueden interesar
    public List<VehicleRecommendation> NewArrivals { get; set; }
    
    // Deals destacados en su rango de precio
    public List<VehicleRecommendation> BestDeals { get; set; }
    
    // Búsquedas recomendadas
    public List<SearchRecommendation> SuggestedSearches { get; set; }
}

public class VehicleRecommendation
{
    public Guid VehicleId { get; set; }
    public decimal RelevanceScore { get; set; }      // 0-1
    public string RecommendationType { get; set; }   // "similar", "forYou", etc.
    public string Explanation { get; set; }          // "Porque te gustó Honda CR-V"
    public List<string> MatchingReasons { get; set; }
}
```

#### Para Dealers/Vendedores ⭐ VALOR EXTREMO
```csharp
public class SellerRecommendations
{
    // Compradores potenciales para un vehículo específico
    public List<BuyerRecommendation> InterestedBuyers(Guid vehicleId) { get; }
    
    // Compradores que probablemente comprarán pronto
    public List<BuyerRecommendation> HotBuyers { get; set; }
    
    // Sugerencias de precios
    public PriceRecommendation SuggestedPrice(Guid vehicleId) { get; }
    
    // Vehículos que debería comprar para inventario
    public List<InventoryRecommendation> SuggestedInventory { get; set; }
    
    // Mejoras sugeridas para listings
    public List<ListingImprovement> SuggestedImprovements { get; set; }
}

public class BuyerRecommendation
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }             // Solo iniciales para privacidad
    public decimal InterestScore { get; set; }       // 0-100
    public string InterestLevel { get; set; }        // "Very High", "High", "Medium"
    public List<string> MatchingCriteria { get; set; }
    public DateTime LastActiveAt { get; set; }
    public FunnelPosition FunnelPosition { get; set; }
    
    // Acciones sugeridas
    public List<SuggestedAction> SuggestedActions { get; set; }
}

// Ejemplo de output:
// "3 compradores muy interesados en tu Toyota Corolla 2020:"
// - Usuario A.M. (Santo Domingo) - Score 92%
//   * Vio el vehículo 5 veces en los últimos 7 días
//   * Tiene 3 favoritos en la categoría sedanes $15-20k
//   * Usó calculadora de financiamiento
//   * Acción: Enviar mensaje proactivo
```

### Algoritmos de Recomendación

```python
# 1. Collaborative Filtering
# "Usuarios que vieron X también vieron Y"
def collaborative_filtering(user_id, vehicle_id):
    similar_users = find_similar_users(user_id)
    their_viewed = get_vehicles_viewed_by(similar_users)
    return rank_by_relevance(their_viewed)

# 2. Content-Based Filtering
# "Basado en características del vehículo"
def content_based(vehicle_id, user_preferences):
    similar_vehicles = find_similar_vehicles(vehicle_id)
    return filter_by_preferences(similar_vehicles, user_preferences)

# 3. Hybrid (Lo que usaremos)
# Combina ambos + features adicionales
def hybrid_recommendation(user_id, context):
    collab_score = collaborative_filtering(user_id)
    content_score = content_based_for_user(user_id)
    popularity_score = get_popularity()
    freshness_score = get_freshness()
    dealer_boost = get_dealer_boost()  # Dealers pagando más → más visibilidad
    
    final_score = (
        collab_score * 0.35 +
        content_score * 0.30 +
        popularity_score * 0.15 +
        freshness_score * 0.10 +
        dealer_boost * 0.10
    )
    return sorted_by(final_score)
```

### Endpoints

```
GET  /api/recommendations/user/{userId}                    - Recomendaciones para comprador
GET  /api/recommendations/vehicle/{vehicleId}/similar      - Vehículos similares
GET  /api/recommendations/vehicle/{vehicleId}/buyers       - Compradores potenciales
GET  /api/recommendations/dealer/{dealerId}/buyers         - Compradores para dealer
GET  /api/recommendations/dealer/{dealerId}/inventory      - Inventario sugerido
POST /api/recommendations/explain                          - Explicar recomendación
```

---

## 6. 📊 LEAD SCORING SERVICE (Puerto 5055) ⭐⭐⭐ MEGA VALIOSO PARA DEALERS

### ¿Por qué es necesario?
- Dealers reciben 50-200 leads/mes
- No todos los leads son iguales
- Priorizar leads = más ventas = dealers felices = renuevan suscripción

### Modelo de Scoring

```csharp
public class LeadScore
{
    public Guid LeadId { get; set; }
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DealerId { get; set; }
    
    // Score principal (0-100)
    public int Score { get; set; }
    
    // Clasificación
    public LeadTemperature Temperature { get; set; }  // Hot, Warm, Cold
    
    // Probabilidad de conversión (0-100%)
    public decimal ConversionProbability { get; set; }
    
    // Tiempo estimado para decisión
    public TimeSpan EstimatedTimeToDecision { get; set; }
    
    // Factores que contribuyen al score
    public LeadScoreFactors Factors { get; set; }
    
    // Acciones recomendadas
    public List<SuggestedAction> SuggestedActions { get; set; }
    
    // Urgencia
    public LeadUrgency Urgency { get; set; }
    public string UrgencyReason { get; set; }
}

public enum LeadTemperature
{
    Hot,       // Score 80-100: Contactar inmediatamente
    Warm,      // Score 50-79: Contactar hoy
    Cold,      // Score 20-49: Seguimiento regular
    Ice        // Score 0-19: Nurturing largo plazo
}

public class LeadScoreFactors
{
    // Engagement con el vehículo (0-25 pts)
    public int VehicleEngagement { get; set; }
    // - Vistas múltiples: +5
    // - Tiempo en página > 2 min: +5
    // - Vio todas las fotos: +5
    // - Añadió a favoritos: +5
    // - Usó calculadora: +5
    
    // Intención de compra (0-25 pts)
    public int PurchaseIntent { get; set; }
    // - Contactó vendedor: +10
    // - Agendó test drive: +15
    // - Preguntó por financiamiento: +10
    // - Hizo oferta: +15
    
    // Fit con el vehículo (0-20 pts)
    public int VehicleFit { get; set; }
    // - Precio en su rango habitual: +10
    // - Marca/modelo en sus preferencias: +10
    
    // Comportamiento general (0-15 pts)
    public int GeneralBehavior { get; set; }
    // - Usuario activo (visitó últimos 3 días): +5
    // - Múltiples sesiones: +5
    // - Perfil completo: +5
    
    // Señales de urgencia (0-15 pts)
    public int UrgencySignals { get; set; }
    // - Búsquedas frecuentes: +5
    // - Comparando múltiples: +5
    // - Mencionó fecha límite: +10
}

public enum LeadUrgency
{
    Immediate,      // "Este comprador está activo AHORA"
    Today,          // "Contactar hoy"
    ThisWeek,       // "Contactar esta semana"
    NoRush          // "Sin urgencia particular"
}

public class SuggestedAction
{
    public string Action { get; set; }
    public string Channel { get; set; }           // "call", "sms", "email", "whatsapp"
    public string Script { get; set; }            // Guión sugerido
    public TimeSpan TimeWindow { get; set; }      // Contactar en los próximos X
    public decimal ImpactScore { get; set; }      // Probabilidad de que funcione
}
```

### Ejemplo de Output para Dealer

```
🔥 LEAD HOT (Score: 92/100)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Usuario: J.P. (Santo Domingo)
Vehículo: Toyota Corolla 2020 - $18,500

📊 Por qué es HOT:
✅ Vio el vehículo 7 veces en 3 días
✅ Pasó 8 minutos promedio en cada visita
✅ Usó calculadora de financiamiento ($350/mes)
✅ Añadió a favoritos
✅ Envió mensaje preguntando disponibilidad

⏰ Urgencia: INMEDIATA
• Usuario activo hace 12 minutos
• Está en la página de tu vehículo AHORA

🎯 Acción Recomendada:
1. Llamar inmediatamente (probabilidad éxito: 78%)
2. Script: "Hola, vi que está interesado en nuestro 
   Toyota Corolla. ¿Le gustaría agendar un test drive 
   para hoy o mañana?"

💡 Insight:
Este comprador ha visto 15 sedanes en su rango de 
precio en los últimos 7 días. Está comparando activamente.
Probabilidad de comprar esta semana: 65%
```

### Endpoints

```
GET  /api/leadscoring/lead/{leadId}                 - Score de un lead
GET  /api/leadscoring/dealer/{dealerId}/hot         - Leads HOT del dealer
GET  /api/leadscoring/dealer/{dealerId}/pipeline    - Pipeline ordenado por score
GET  /api/leadscoring/vehicle/{vehicleId}/leads     - Leads del vehículo con score
POST /api/leadscoring/recalculate                   - Recalcular scores
GET  /api/leadscoring/insights/{dealerId}           - Insights para dealer
```

---

## 7. 🚗 VEHICLE INTELLIGENCE SERVICE (Puerto 5056) ⭐⭐⭐ DIFERENCIADOR

### ¿Por qué es necesario?
- Pricing inteligente = vehículos se venden más rápido
- Predicción de demanda = dealers compran mejor inventario
- Detección de anomalías = evita fraudes y errores

### Funcionalidades

#### Pricing Intelligence
```csharp
public class PriceAnalysis
{
    public Guid VehicleId { get; set; }
    public decimal CurrentPrice { get; set; }
    
    // Precio sugerido
    public decimal SuggestedPrice { get; set; }
    public decimal SuggestedPriceMin { get; set; }
    public decimal SuggestedPriceMax { get; set; }
    
    // Comparación con mercado
    public decimal MarketAvgPrice { get; set; }
    public decimal PriceVsMarket { get; set; }        // 1.05 = 5% arriba
    public string PricePosition { get; set; }         // "Above Market", "Below", "Fair"
    
    // Competencia directa
    public List<CompetitorVehicle> SimilarListings { get; set; }
    
    // Predicción
    public int PredictedDaysToSaleAtCurrentPrice { get; set; }
    public int PredictedDaysToSaleAtSuggestedPrice { get; set; }
    
    // Recomendaciones
    public List<PriceRecommendation> Recommendations { get; set; }
}

public class PriceRecommendation
{
    public string Type { get; set; }                  // "reduce", "maintain", "highlight"
    public string Reason { get; set; }
    public decimal? SuggestedValue { get; set; }
    public string ImpactDescription { get; set; }
}

// Ejemplo de output:
// "Tu Honda CR-V 2021 está $2,000 arriba del mercado.
//  Tiempo estimado de venta: 45 días.
//  Si reduces a $28,500 (nuestro precio sugerido):
//  Tiempo estimado: 18 días."
```

#### Demand Prediction
```csharp
public class DemandPrediction
{
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    
    // Demanda actual
    public DemandLevel CurrentDemand { get; set; }
    public decimal DemandScore { get; set; }          // 0-100
    
    // Tendencia
    public TrendDirection Trend { get; set; }         // Rising, Falling, Stable
    public decimal TrendStrength { get; set; }        // 0-1
    
    // Predicción
    public DemandLevel PredictedDemand30Days { get; set; }
    public DemandLevel PredictedDemand90Days { get; set; }
    
    // Insights
    public List<string> Insights { get; set; }
    
    // Para dealers: ¿debería comprar este modelo?
    public BuyRecommendation BuyRecommendation { get; set; }
}

public enum DemandLevel
{
    VeryHigh,   // "Se venden en menos de 15 días"
    High,       // "Se venden en 15-30 días"
    Medium,     // "Se venden en 30-60 días"
    Low,        // "Se venden en 60-90 días"
    VeryLow     // "Difícil de vender, >90 días"
}

// Ejemplo de output:
// "Toyota RAV4 2022 - DEMANDA MUY ALTA
//  • 45 búsquedas diarias (2x promedio)
//  • Solo 8 disponibles en el mercado
//  • Tiempo promedio de venta: 12 días
//  • Tendencia: ↗️ Subiendo 15% vs mes anterior
//  
//  RECOMENDACIÓN: Excelente para inventario.
//  Si puedes conseguirlo a $32,000, margen estimado: $3,500"
```

#### Anomaly Detection
```csharp
public class VehicleAnomaly
{
    public Guid VehicleId { get; set; }
    public AnomalyType Type { get; set; }
    public AnomalySeverity Severity { get; set; }
    public string Description { get; set; }
    public List<string> Indicators { get; set; }
    public SuggestedAction Action { get; set; }
}

public enum AnomalyType
{
    PriceTooLow,              // Precio sospechosamente bajo
    PriceTooHigh,             // Precio muy por encima del mercado
    MileageInconsistent,      // Kilometraje no cuadra con año
    DescriptionMismatch,      // Descripción no coincide con fotos
    DuplicateListing,         // Listing duplicado
    SuspiciousActivity,       // Actividad sospechosa
    QualityIssue              // Problema de calidad del listing
}

// Ejemplo para Admin:
// "⚠️ ALERTA: Listing #12345
//  Mercedes-Benz C300 2021 - $15,000
//  
//  ANOMALÍA: Precio 60% por debajo del mercado
//  Precio promedio similar: $38,000
//  
//  Posibles causas:
//  1. Error de tipeo (¿falta un dígito?)
//  2. Salvage/rebuilt title no declarado
//  3. Posible estafa
//  
//  ACCIÓN: Revisar manualmente antes de aprobar"
```

### Endpoints

```
GET  /api/vehicleintel/price/{vehicleId}           - Análisis de precio
GET  /api/vehicleintel/demand/{make}/{model}       - Predicción de demanda
GET  /api/vehicleintel/market/trends               - Tendencias del mercado
GET  /api/vehicleintel/anomalies                   - Anomalías detectadas
POST /api/vehicleintel/evaluate                    - Evaluar vehículo nuevo
GET  /api/vehicleintel/dealer/{dealerId}/insights  - Insights para dealer
```

---

## 8. 🤖 ML TRAINING SERVICE (Puerto 5057)

### ¿Por qué es necesario?
- Los modelos necesitan re-entrenamiento periódico
- Versionado y tracking de modelos
- A/B testing de diferentes versiones

### Modelos a Entrenar

```csharp
public enum MLModel
{
    // Recomendaciones
    VehicleRecommender,           // Recomendar vehículos a usuarios
    BuyerRecommender,             // Recomendar compradores a dealers
    SimilarVehicles,              // Encontrar vehículos similares
    
    // Scoring
    LeadScorer,                   // Scoring de leads
    ChurnPredictor,               // Predecir abandono
    ConversionPredictor,          // Predecir conversión
    
    // Pricing
    PricePredictor,               // Predecir precio óptimo
    DaysToSalePredictor,          // Predecir días para venta
    
    // Clasificación
    UserSegmenter,                // Clasificar usuarios en segmentos
    VehicleClassifier,            // Clasificar vehículos
    FraudDetector,                // Detectar fraude/anomalías
    
    // NLP
    DescriptionAnalyzer,          // Analizar descripciones
    SentimentAnalyzer,            // Analizar sentimiento de reviews
    SearchIntentClassifier        // Clasificar intención de búsqueda
}

public class ModelVersion
{
    public Guid Id { get; set; }
    public MLModel Model { get; set; }
    public string Version { get; set; }              // "1.2.3"
    public DateTime TrainedAt { get; set; }
    
    // Métricas
    public Dictionary<string, decimal> Metrics { get; set; }
    // - accuracy, precision, recall, f1, auc, rmse, etc.
    
    // Training info
    public long TrainingDataSize { get; set; }
    public TimeSpan TrainingDuration { get; set; }
    public string TrainingConfig { get; set; }       // JSON
    
    // Estado
    public ModelStatus Status { get; set; }          // Training, Ready, Deployed, Deprecated
    public decimal TrafficPercentage { get; set; }   // % de tráfico (A/B testing)
}
```

### Endpoints

```
POST /api/mltraining/train/{model}               - Iniciar entrenamiento
GET  /api/mltraining/models                      - Listar modelos
GET  /api/mltraining/model/{model}/versions      - Versiones de un modelo
POST /api/mltraining/deploy/{model}/{version}    - Desplegar versión
POST /api/mltraining/rollback/{model}            - Rollback a versión anterior
GET  /api/mltraining/metrics/{model}             - Métricas de modelo
POST /api/mltraining/abtest                      - Configurar A/B test
```

---

## 9. 📊 LISTING ANALYTICS SERVICE (Puerto 5058) ⭐⭐⭐⭐⭐ ESENCIAL

### ¿Por qué es necesario?
- **Dealers y Vendedores NECESITAN ver estadísticas de sus publicaciones**
- Es una funcionalidad básica esperada en cualquier marketplace
- Motiva a vendedores a mejorar sus listings
- Justifica el pago de la suscripción para dealers

### Funcionalidades

#### Dashboard de Estadísticas por Publicación

```csharp
public class ListingStatistics
{
    public Guid VehicleId { get; set; }
    public Guid SellerId { get; set; }           // Dealer o Individual
    public string SellerType { get; set; }        // "Dealer" | "Individual"
    
    // === VISTAS ===
    public int TotalViews { get; set; }           // Vistas totales
    public int UniqueViews { get; set; }          // Visitantes únicos
    public int ViewsToday { get; set; }           // Vistas hoy
    public int ViewsLast7Days { get; set; }       // Última semana
    public int ViewsLast30Days { get; set; }      // Último mes
    public List<DailyViewCount> ViewsHistory { get; set; }  // Historial por día
    
    // === ENGAGEMENT ===
    public int FavoritesCount { get; set; }       // Veces guardado en favoritos
    public int SharesCount { get; set; }          // Veces compartido
    public int PhotoViewsCount { get; set; }      // Cuántos vieron todas las fotos
    public decimal AvgTimeOnPage { get; set; }    // Tiempo promedio en segundos
    public decimal AvgScrollDepth { get; set; }   // Qué tanto scrollean (0-100%)
    
    // === CONTACTO ===
    public int ContactRequests { get; set; }      // Formularios enviados
    public int ChatMessages { get; set; }         // Mensajes recibidos
    public int PhoneCalls { get; set; }           // Clicks en "Llamar" (si aplica)
    public int WhatsAppClicks { get; set; }       // Clicks en WhatsApp
    
    // === APPOINTMENTS ===
    public int TestDriveRequests { get; set; }    // Solicitudes de test drive
    public int TestDriveCompleted { get; set; }   // Test drives realizados
    
    // === FINANCIAMIENTO ===
    public int FinanceCalculatorUses { get; set; }  // Usaron calculadora
    public int FinanceApplications { get; set; }    // Aplicaron financiamiento
    
    // === CONVERSIÓN ===
    public decimal ViewToContactRate { get; set; }  // Vistas → Contacto (%)
    public decimal ContactToTestDriveRate { get; set; }  // Contacto → Test Drive (%)
    
    // === COMPARACIÓN ===
    public MarketComparison Comparison { get; set; }
}

public class DailyViewCount
{
    public DateTime Date { get; set; }
    public int Views { get; set; }
    public int UniqueViews { get; set; }
}

public class MarketComparison
{
    public decimal AvgViewsInCategory { get; set; }     // Promedio de la categoría
    public string PerformanceLevel { get; set; }         // "Above Average", "Average", "Below"
    public decimal PercentileRank { get; set; }          // Top 10%, Top 50%, etc.
    public List<string> ImprovementTips { get; set; }    // Tips para mejorar
}
```

#### Vista para Vendedor Individual (Simplificada)

```
┌─────────────────────────────────────────────────────────────┐
│  📊 Estadísticas de tu Honda Civic 2020                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  👁️ VISTAS                        📱 CONTACTOS              │
│  ┌─────────────────────┐          ┌──────────────────────┐ │
│  │     156             │          │       12             │ │
│  │   vistas totales    │          │    inquiries         │ │
│  │   (+23 esta semana) │          │   (+3 esta semana)   │ │
│  └─────────────────────┘          └──────────────────────┘ │
│                                                             │
│  ❤️ 8 favoritos  │  🔗 5 compartidos  │  📅 2 test drives   │
│                                                             │
│  📈 RENDIMIENTO: Tu publicación está por encima del        │
│     promedio en tu categoría (Top 30%)                      │
│                                                             │
│  💡 TIPS PARA MEJORAR:                                      │
│  • Añade más fotos (solo tienes 6, promedio es 12)          │
│  • Considera bajar el precio $500 (estás 5% arriba)         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Vista para Dealer (Completa con Gráficas)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  📊 Dashboard de Estadísticas - AutoMax Dealer                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  RESUMEN (Últimos 30 días)                                                  │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐│
│  │  12,456    │ │    892     │ │    156     │ │     45     │ │     12     ││
│  │  Vistas    │ │ Contactos  │ │Test Drives │ │   Ventas   │ │Fin. Aprob. ││
│  │  ↑ 15%     │ │  ↑ 8%      │ │  ↑ 12%     │ │   ↑ 5%     │ │   ↑ 20%    ││
│  └────────────┘ └────────────┘ └────────────┘ └────────────┘ └────────────┘│
│                                                                             │
│  📈 VISTAS POR DÍA (últimos 30 días)                                        │
│  500│     ▄▄                                                                │
│  400│   ▄████  ▄▄                    ▄▄▄▄▄▄                                │
│  300│  ████████████▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄████████▄▄▄▄                            │
│  200│ ██████████████████████████████████████████▄▄▄▄▄▄▄                    │
│  100│████████████████████████████████████████████████████                  │
│     └───────────────────────────────────────────────────────               │
│       1   5   10   15   20   25   30                                        │
│                                                                             │
│  🚗 TOP 5 VEHÍCULOS MÁS VISTOS                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 1. Toyota RAV4 2022      │ 856 vistas │ 45 contactos │ ⭐ Destacado  │   │
│  │ 2. Honda CR-V 2021       │ 654 vistas │ 32 contactos │              │   │
│  │ 3. Hyundai Tucson 2023   │ 521 vistas │ 28 contactos │              │   │
│  │ 4. Toyota Corolla 2020   │ 445 vistas │ 25 contactos │              │   │
│  │ 5. Nissan Sentra 2021    │ 389 vistas │ 18 contactos │              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ⚠️ VEHÍCULOS QUE NECESITAN ATENCIÓN                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ • Mazda 3 2019 - Solo 23 vistas en 30 días (promedio: 150)          │   │
│  │   💡 Recomendación: Bajar precio 10% o añadir a Destacados          │   │
│  │                                                                      │   │
│  │ • Ford Explorer 2020 - 0 contactos en 14 días                       │   │
│  │   💡 Recomendación: Revisar descripción y añadir más fotos          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  📍 DE DÓNDE VIENEN TUS VISITANTES                                          │
│  ┌────────────────────────────────────────────┐                            │
│  │ 🔍 Búsqueda en OKLA     45%  ████████████  │                            │
│  │ 📱 Redes Sociales       25%  ███████       │                            │
│  │ 🔗 Link Directo         15%  ████          │                            │
│  │ 🌐 Google               10%  ███           │                            │
│  │ 📧 Email Marketing       5%  █             │                            │
│  └────────────────────────────────────────────┘                            │
│                                                                             │
│  [📥 Exportar Reporte PDF]  [📧 Programar Reporte Semanal]                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Entidades

```csharp
public class ListingView
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? UserId { get; set; }              // Null si anónimo
    public string SessionId { get; set; }
    public DateTime ViewedAt { get; set; }
    public int DurationSeconds { get; set; }
    public decimal ScrollDepth { get; set; }
    public string Source { get; set; }             // "search", "homepage", "direct"
    public string DeviceType { get; set; }         // "mobile", "desktop"
    public string City { get; set; }
}

public class ListingDailyStats
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime Date { get; set; }
    
    public int Views { get; set; }
    public int UniqueViews { get; set; }
    public int Favorites { get; set; }
    public int Shares { get; set; }
    public int ContactRequests { get; set; }
    public int TestDriveRequests { get; set; }
    public decimal AvgTimeOnPage { get; set; }
}

public class SellerDashboardStats
{
    public Guid SellerId { get; set; }
    public string SellerType { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    public int TotalListings { get; set; }
    public int TotalViews { get; set; }
    public int TotalContacts { get; set; }
    public int TotalTestDrives { get; set; }
    public int TotalSales { get; set; }
    
    public List<VehicleStats> VehicleStats { get; set; }
    public List<TrendPoint> ViewsTrend { get; set; }
    public Dictionary<string, int> ViewsBySource { get; set; }
}
```

### Endpoints

```
# Para Vendedor Individual
GET  /api/listinganalytics/vehicle/{vehicleId}              - Stats de un vehículo
GET  /api/listinganalytics/seller/{sellerId}/summary        - Resumen del vendedor
GET  /api/listinganalytics/vehicle/{vehicleId}/views        - Historial de vistas

# Para Dealer (completo)
GET  /api/listinganalytics/dealer/{dealerId}/dashboard      - Dashboard completo
GET  /api/listinganalytics/dealer/{dealerId}/vehicles       - Stats de todos los vehículos
GET  /api/listinganalytics/dealer/{dealerId}/trends         - Tendencias
GET  /api/listinganalytics/dealer/{dealerId}/sources        - Fuentes de tráfico
GET  /api/listinganalytics/dealer/{dealerId}/top            - Top performers
GET  /api/listinganalytics/dealer/{dealerId}/attention      - Necesitan atención
POST /api/listinganalytics/report/schedule                  - Programar reportes
GET  /api/listinganalytics/report/export/{format}           - Exportar (pdf, excel)

# Comparación
GET  /api/listinganalytics/vehicle/{vehicleId}/compare      - Comparar con mercado
GET  /api/listinganalytics/vehicle/{vehicleId}/tips         - Tips para mejorar
```

### Tecnologías
- **Backend:** .NET 8 con Clean Architecture
- **Base de datos:** PostgreSQL para stats agregadas
- **Time-series:** TimescaleDB o ClickHouse para datos de vistas
- **Cache:** Redis para dashboards en tiempo real
- **Charts:** Datos preparados para frontend (Chart.js, Recharts)

---

## 10. ⭐ REVIEW SERVICE (Puerto 5059) ⭐⭐⭐⭐⭐ ESENCIAL - ESTILO AMAZON

### ¿Por qué es necesario?
- **Confianza:** Los compradores confían en las opiniones de otros compradores
- **Reputación:** Dealers y vendedores son evaluados por su historial
- **Diferenciación:** Buenos vendedores se destacan, malos son identificados
- **SEO:** Reviews generan contenido único que mejora posicionamiento

### Funcionalidades al Estilo Amazon

#### Sistema de Reviews Completo

```csharp
public class Review
{
    public Guid Id { get; set; }
    
    // ¿Quién hace la review?
    public Guid ReviewerId { get; set; }            // Comprador que hace review
    public string ReviewerName { get; set; }        // "Juan P." (nombre + inicial)
    public string ReviewerLocation { get; set; }    // "Santo Domingo"
    
    // ¿A quién se le hace review?
    public Guid SellerId { get; set; }              // Dealer o Vendedor Individual
    public SellerType SellerType { get; set; }      // Dealer | Individual
    
    // ¿Sobre qué vehículo? (opcional - puede ser solo sobre el vendedor)
    public Guid? VehicleId { get; set; }
    public string VehicleName { get; set; }         // "Toyota Corolla 2020"
    
    // === RATING (Estrellas) ===
    public int OverallRating { get; set; }          // 1-5 estrellas
    
    // Ratings detallados (opcional)
    public int? CommunicationRating { get; set; }   // Comunicación
    public int? AccuracyRating { get; set; }        // ¿El vehículo era como lo describían?
    public int? SpeedRating { get; set; }           // Rapidez del proceso
    public int? ValueRating { get; set; }           // Relación calidad-precio
    
    // === CONTENIDO ===
    public string Title { get; set; }               // "Excelente experiencia de compra"
    public string Body { get; set; }                // Texto del review (500-2000 chars)
    public List<string> PhotoUrls { get; set; }     // Fotos adjuntas (máx 5)
    
    // === VERIFICACIÓN ===
    public bool IsVerifiedPurchase { get; set; }    // ✓ Compra verificada
    public DateTime? PurchaseDate { get; set; }
    public Guid? TransactionId { get; set; }
    
    // === METADATA ===
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ReviewStatus Status { get; set; }        // Pending, Approved, Rejected, Flagged
    
    // === INTERACCIÓN ===
    public int HelpfulVotes { get; set; }           // "X personas encontraron útil esta opinión"
    public int NotHelpfulVotes { get; set; }
    public int ReportCount { get; set; }            // Veces reportada
    
    // === RESPUESTA DEL VENDEDOR ===
    public SellerResponse? SellerResponse { get; set; }
}

public class SellerResponse
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid ResponderId { get; set; }           // Usuario del dealer que responde
    public string ResponderName { get; set; }       // "AutoMax Dealer"
    public string Body { get; set; }                // Respuesta del vendedor
    public DateTime RespondedAt { get; set; }
}

public enum ReviewStatus
{
    Pending,        // Esperando moderación
    Approved,       // Publicada
    Rejected,       // Rechazada (spam, inapropiada)
    Flagged,        // Marcada para revisión
    Hidden          // Oculta por el sistema
}

public enum SellerType
{
    Individual,
    Dealer
}
```

#### Resumen de Ratings del Vendedor (Estilo Amazon)

```csharp
public class SellerRatingSummary
{
    public Guid SellerId { get; set; }
    public SellerType SellerType { get; set; }
    
    // === PROMEDIO GENERAL ===
    public decimal AverageRating { get; set; }      // 4.7
    public int TotalReviews { get; set; }           // 156 calificaciones
    
    // === DISTRIBUCIÓN DE ESTRELLAS ===
    public int FiveStarCount { get; set; }          // 120 (77%)
    public int FourStarCount { get; set; }          // 25 (16%)
    public int ThreeStarCount { get; set; }         // 8 (5%)
    public int TwoStarCount { get; set; }           // 2 (1%)
    public int OneStarCount { get; set; }           // 1 (1%)
    
    public decimal FiveStarPercentage { get; set; }
    public decimal FourStarPercentage { get; set; }
    public decimal ThreeStarPercentage { get; set; }
    public decimal TwoStarPercentage { get; set; }
    public decimal OneStarPercentage { get; set; }
    
    // === PROMEDIOS DETALLADOS ===
    public decimal AvgCommunication { get; set; }   // 4.8
    public decimal AvgAccuracy { get; set; }        // 4.6
    public decimal AvgSpeed { get; set; }           // 4.5
    public decimal AvgValue { get; set; }           // 4.7
    
    // === BADGES ===
    public List<SellerBadge> Badges { get; set; }
    
    // === ÚLTIMA ACTUALIZACIÓN ===
    public DateTime LastReviewAt { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class SellerBadge
{
    public string BadgeType { get; set; }
    public string DisplayName { get; set; }
    public string Icon { get; set; }
}

// Ejemplos de badges:
// ⭐ "Top Rated Seller" - Promedio >= 4.8 con 50+ reviews
// ✓ "Trusted Dealer" - Verificado por OKLA
// 🚀 "Fast Responder" - Responde en < 1 hora
// 💯 "100% Positive" - Sin reviews negativas (< 3 estrellas)
// 🏆 "Best of 2025" - Top 10 del año
```

### Vista del Comprador (Estilo Amazon)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTOMAX DEALER - OPINIONES DE CLIENTES                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ⭐⭐⭐⭐⭐ 4.7 de 5                                                        │
│  156 calificaciones globales                                               │
│                                                                             │
│  ⭐ Top Rated Seller   ✓ Trusted Dealer   🚀 Fast Responder                │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  5 estrellas  ████████████████████████████████████████  77%               │
│  4 estrellas  ████████                                   16%               │
│  3 estrellas  ███                                        5%                │
│  2 estrellas  █                                          1%                │
│  1 estrella   █                                          1%                │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  CALIFICACIONES DETALLADAS                                                  │
│  Comunicación     ⭐⭐⭐⭐⭐  4.8                                           │
│  Exactitud        ⭐⭐⭐⭐⭐  4.6                                           │
│  Rapidez          ⭐⭐⭐⭐⭐  4.5                                           │
│  Valor            ⭐⭐⭐⭐⭐  4.7                                           │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  [Ordenar por: Más recientes ▼]  [Filtrar por: Todas las estrellas ▼]      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Reviews Individuales (Estilo Amazon)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  👤 Juan P.                                                                 │
│  📍 Santo Domingo                                                           │
│                                                                             │
│  ⭐⭐⭐⭐⭐  Excelente experiencia de compra                                │
│  ✓ Compra verificada  |  Toyota Corolla 2020  |  Reseñado el 5 enero 2026   │
│                                                                             │
│  Todo el proceso fue muy profesional. Desde el primer contacto, Roberto     │
│  (el vendedor) fue muy atento y respondió todas mis preguntas. El vehículo  │
│  estaba exactamente como en las fotos, sin sorpresas. El proceso de         │
│  financiamiento fue rápido y transparente.                                  │
│                                                                             │
│  Lo único que mejoraría es el tiempo de entrega, pero entiendo que estaban  │
│  procesando los documentos. Totalmente recomendado.                         │
│                                                                             │
│  📷 [foto1.jpg] [foto2.jpg] [foto3.jpg]                                     │
│                                                                             │
│  ───────────────────────────────────────────────────────────────────────    │
│  💬 Respuesta de AutoMax Dealer (6 enero 2026):                             │
│  "¡Gracias Juan por tu confianza! Nos alegra que hayas tenido una buena    │
│   experiencia. Estamos trabajando en mejorar los tiempos de entrega.       │
│   ¡Disfruta tu nuevo Corolla!"                                             │
│  ───────────────────────────────────────────────────────────────────────    │
│                                                                             │
│  👍 45 personas encontraron útil esta opinión                               │
│                                                                             │
│  [¿Te resultó útil?]  [Sí 👍]  [No 👎]  [Reportar ⚠️]                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Formulario para Dejar Review

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        📝 ESCRIBE TU OPINIÓN                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Compraste: Toyota Corolla 2020                                             │
│  De: AutoMax Dealer                                                         │
│  Fecha de compra: 15 diciembre 2025                                         │
│  ✓ Compra verificada                                                        │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  CALIFICACIÓN GENERAL *                                                     │
│  [☆] [☆] [☆] [☆] [☆]                                                      │
│   1    2    3    4    5                                                     │
│                                                                             │
│  CALIFICACIONES DETALLADAS (opcional)                                       │
│  Comunicación    [☆] [☆] [☆] [☆] [☆]                                      │
│  Exactitud       [☆] [☆] [☆] [☆] [☆]                                      │
│  Rapidez         [☆] [☆] [☆] [☆] [☆]                                      │
│  Valor           [☆] [☆] [☆] [☆] [☆]                                      │
│                                                                             │
│  TÍTULO DE TU OPINIÓN *                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Excelente experiencia...                                            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  TU OPINIÓN *                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Cuéntanos tu experiencia de compra. ¿Cómo fue el trato? ¿El        │   │
│  │ vehículo era como lo describían? ¿Recomendarías a este vendedor?   │   │
│  │                                                                     │   │
│  │                                                                     │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│  Mínimo 50 caracteres                                                       │
│                                                                             │
│  AÑADIR FOTOS (opcional)                                                    │
│  [📷 Subir fotos] Máximo 5 fotos                                            │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  ☑️ Acepto que mi opinión sea publicada en OKLA                             │
│  ☑️ Confirmo que esta opinión refleja mi experiencia real                   │
│                                                                             │
│                    [Cancelar]    [📤 Publicar opinión]                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Entidades Adicionales

```csharp
public class ReviewVote
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public bool IsHelpful { get; set; }             // true = útil, false = no útil
    public DateTime VotedAt { get; set; }
}

public class ReviewReport
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid ReporterId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public DateTime ReportedAt { get; set; }
    public ReportStatus Status { get; set; }
}

public enum ReportReason
{
    Spam,
    FakeReview,
    Inappropriate,
    NotRelevant,
    Offensive,
    CompetitorSabotage,
    Other
}

public class ReviewRequest
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? SentAt { get; set; }           // Cuando se envió el email
    public int RemindersSent { get; set; }          // Cuántos recordatorios
    public ReviewRequestStatus Status { get; set; }
}

public enum ReviewRequestStatus
{
    Pending,            // Esperando enviar
    Sent,               // Email enviado
    Reminded,           // Recordatorio enviado
    Completed,          // Review completada
    Expired,            // Pasó el límite (90 días)
    Declined            // Usuario no quiso dejar review
}
```

### Flujo de Review Automatizado

```
Compra completada
       │
       ▼
[7 días después] ──► Email: "¿Cómo fue tu experiencia con AutoMax?"
       │
       │ (si no responde)
       ▼
[14 días después] ──► Recordatorio: "Tu opinión ayuda a otros compradores"
       │
       │ (si no responde)
       ▼
[30 días después] ──► Último recordatorio
       │
       │ (si no responde)
       ▼
[90 días] ──► Expirado (ya no puede dejar review)
```

### Moderación y Anti-Fraude

```csharp
public class ReviewModerationResult
{
    public Guid ReviewId { get; set; }
    public bool IsApproved { get; set; }
    public List<string> Flags { get; set; }
    public decimal SpamScore { get; set; }          // 0-1, > 0.7 = probable spam
    public decimal FakeScore { get; set; }          // Probabilidad de ser falsa
    public List<string> ModerationNotes { get; set; }
}

// Señales de review falsa/spam:
// - Usuario creó cuenta solo para dejar review
// - IP sospechosa (VPN, datacenter)
// - Texto copiado de otra review
// - Demasiadas reviews en poco tiempo
// - Patrón de lenguaje artificial (IA)
// - Review extremadamente corta o genérica
// - Review de competidor (mismo segmento)
```

### Endpoints

```
# Reviews públicas
GET  /api/reviews/seller/{sellerId}                    - Reviews de un vendedor
GET  /api/reviews/seller/{sellerId}/summary            - Resumen de ratings
GET  /api/reviews/vehicle/{vehicleId}                  - Reviews del vehículo
GET  /api/reviews/{reviewId}                           - Una review específica

# Crear review (requiere auth + compra verificada)
POST /api/reviews                                      - Crear review
PUT  /api/reviews/{reviewId}                           - Editar mi review
DELETE /api/reviews/{reviewId}                         - Eliminar mi review

# Interacción
POST /api/reviews/{reviewId}/vote                      - Votar útil/no útil
POST /api/reviews/{reviewId}/report                    - Reportar review

# Respuesta del vendedor
POST /api/reviews/{reviewId}/response                  - Responder review
PUT  /api/reviews/{reviewId}/response                  - Editar respuesta
DELETE /api/reviews/{reviewId}/response                - Eliminar respuesta

# Para vendedor
GET  /api/reviews/my-reviews                           - Mis reviews recibidas
GET  /api/reviews/my-reviews/pending-response          - Reviews sin responder

# Solicitudes de review
GET  /api/reviews/requests                             - Solicitudes pendientes
POST /api/reviews/requests/{requestId}/send            - Enviar email de solicitud

# Moderación (admin)
GET  /api/reviews/moderation/pending                   - Reviews pendientes
POST /api/reviews/moderation/{reviewId}/approve        - Aprobar
POST /api/reviews/moderation/{reviewId}/reject         - Rechazar
```

### Tecnologías
- **Backend:** .NET 8 con Clean Architecture
- **Base de datos:** PostgreSQL
- **Búsqueda:** Elasticsearch (para buscar en reviews)
- **Moderación:** Azure Content Moderator o AWS Comprehend
- **Anti-spam:** ML model personalizado
- **Notificaciones:** Integración con NotificationService

---

## 📊 RESUMEN DE NUEVOS MICROSERVICIOS (ACTUALIZADO)

| # | Servicio | Puerto | Prioridad | Tecnologías Principales |
|---|----------|--------|-----------|------------------------|
| 1 | EventTrackingService | 5050 | ⭐⭐⭐⭐⭐ CRÍTICO | Kafka, ClickHouse, JS SDK |
| 2 | DataPipelineService | 5051 | ⭐⭐⭐⭐⭐ CRÍTICO | Airflow, Spark, dbt |
| 3 | UserBehaviorService | 5052 | ⭐⭐⭐⭐⭐ CRÍTICO | PostgreSQL, Redis |
| 4 | FeatureStoreService | 5053 | ⭐⭐⭐⭐⭐ CRÍTICO | PostgreSQL, Redis |
| 5 | RecommendationService | 5054 | ⭐⭐⭐⭐⭐ CRÍTICO | Python, TensorFlow/PyTorch |
| 6 | LeadScoringService | 5055 | ⭐⭐⭐⭐⭐ MEGA VALIOSO | Python, scikit-learn |
| 7 | VehicleIntelligenceService | 5056 | ⭐⭐⭐⭐ DIFERENCIADOR | Python, XGBoost |
| 8 | MLTrainingService | 5057 | ⭐⭐⭐ IMPORTANTE | MLflow, Python |
| 9 | ListingAnalyticsService | 5058 | ⭐⭐⭐⭐⭐ ESENCIAL | .NET 8, TimescaleDB, Redis |
| 10 | ReviewService | 5059 | ⭐⭐⭐⭐⭐ ESENCIAL | .NET 8, PostgreSQL, Elasticsearch |
| 11 | **ChatbotService** | 5060 | ⭐⭐⭐⭐⭐ **GAME CHANGER** | .NET 8, OpenAI, Pinecone, SignalR |

> **TOTAL: 11 Microservicios de Data & ML**

---

## 🔄 FLUJO DE DATOS COMPLETO

```
Usuario interactúa con OKLA
         │
         ▼
[EventTrackingService] ──► Kafka/RabbitMQ ──► [ClickHouse]
         │                                         │
         │                    ┌────────────────────┘
         ▼                    ▼
[DataPipelineService] ──► Transformación, ETL
         │
         ├──► [UserBehaviorService] ──► Perfiles de usuario
         │
         └──► [FeatureStoreService] ──► Features centralizados
                    │
         ┌──────────┼──────────┐
         ▼          ▼          ▼
[Recommendation] [LeadScoring] [VehicleIntel]
    Service         Service      Service
         │          │            │
         └──────────┼────────────┘
                    │
         ┌──────────┴──────────┐
         ▼                     ▼
    [Dealers]              [Compradores]
    
    • Leads ordenados      • "Para ti"
    • Insights             • Similar vehicles
    • Pricing sugerido     • Best deals
    • Buyers potenciales   • Alertas
```

---

## 💰 VALOR PARA CADA TIPO DE USUARIO

### Para Compradores
| Feature | Servicio | Beneficio |
|---------|----------|-----------|
| "Vehículos para ti" | RecommendationService | Encontrar más rápido |
| "Usuarios también vieron" | RecommendationService | Descubrir opciones |
| Alertas personalizadas | UserBehaviorService | No perderse listings |
| Precio justo indicator | VehicleIntelligenceService | Evitar sobrepagar |

### Para Vendedores Individuales
| Feature | Servicio | Beneficio |
|---------|----------|-----------|
| Compradores interesados | RecommendationService | Más contactos |
| Precio sugerido | VehicleIntelligenceService | Vender más rápido |
| Tips para mejorar listing | VehicleIntelligenceService | Más visibilidad |
| **Estadísticas de vistas** | **ListingAnalyticsService** | **Ver quién ve sus publicaciones** |

### Para Dealers ⭐ MÁXIMO VALOR
| Feature | Servicio | Beneficio |
|---------|----------|-----------|
| Lead Scoring | LeadScoringService | Priorizar mejor |
| Compradores potenciales | RecommendationService | Outreach proactivo |
| Pricing óptimo | VehicleIntelligenceService | Vender más rápido |
| Demanda del mercado | VehicleIntelligenceService | Comprar mejor inventario |
| Insights de competencia | VehicleIntelligenceService | Ventaja competitiva |
| Predicción de ventas | VehicleIntelligenceService | Planificar mejor |
| Dashboard analytics | FeatureStoreService | Tomar decisiones |
| **Estadísticas detalladas** | **ListingAnalyticsService** | **Métricas completas por vehículo** |
| **Reviews y reputación** | **ReviewService** | **Construir confianza con clientes** |

---

## 📅 ROADMAP DE IMPLEMENTACIÓN

### Fase 1: Fundamentos (Semanas 1-4)
1. ✅ EventTrackingService - Empezar a recopilar datos
2. ✅ JavaScript SDK para frontend
3. ✅ Mobile SDK para Flutter
4. ✅ **ListingAnalyticsService** - Estadísticas básicas para vendedores
5. ✅ **ReviewService** - Sistema de reviews básico

### Fase 2: Procesamiento (Semanas 5-8)
6. ✅ DataPipelineService - ETL básico
7. ✅ UserBehaviorService - Perfiles básicos
8. ✅ FeatureStoreService - Features iniciales
9. ✅ **ListingAnalyticsService** - Dashboard avanzado para dealers
10. ✅ **ReviewService** - Moderación y badges

### Fase 3: ML Básico (Semanas 9-14)
11. ✅ LeadScoringService - V1 con reglas + ML simple
12. ✅ RecommendationService - "Similar vehicles"
13. ✅ VehicleIntelligenceService - Pricing básico

### Fase 4: ML Avanzado (Semanas 15-20)
14. ✅ MLTrainingService - Pipeline de training
15. ✅ Modelos avanzados de recomendación
16. ✅ A/B testing framework

---

**Última Actualización:** Enero 8, 2026
