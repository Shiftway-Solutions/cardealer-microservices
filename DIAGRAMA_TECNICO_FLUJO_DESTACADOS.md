# 🔗 DIAGRAMA TÉCNICO: Flujo de Vehículos Destacados/Premium

## Nivel 1: Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          🌐 OKLA.COM.DO - FRONTEND                              │
│                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │                          HOMEPAGE (/)                                    │  │
│  │                                                                          │  │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │  │
│  │  │ Hero Section (Featured Carousel)                                  │ │  │
│  │  │ - 5-6 vehículos principales rotados                              │ │  │
│  │  │ - Llamadas a: useHomepageSections()                             │ │  │
│  │  └────────────────────────────────────────────────────────────────────┘ │  │
│  │                                                                          │  │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │  │
│  │  │ ⭐ Vehículos Destacados (Featured Section)                         │ │  │
│  │  │ - 8-12 vehículos en grid                                         │ │  │
│  │  │ - Llamadas a: useHomepageRotation('FeaturedSpot')               │ │  │
│  │  │ - Tracking: impresión al cargar, click al seleccionar          │ │  │
│  │  └────────────────────────────────────────────────────────────────────┘ │  │
│  │                                                                          │  │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │  │
│  │  │ 💎 Vehículos Premium (Premium Section)                            │ │  │
│  │  │ - 4-6 vehículos destacados (PAGADO)                            │ │  │
│  │  │ - Llamadas a: useHomepageRotation('PremiumSpot')               │ │  │
│  │  │ - Tracking: impresión al cargar, click al seleccionar          │ │  │
│  │  │ - Badges: 💎, color dorado, prioridad alta                    │ │  │
│  │  └────────────────────────────────────────────────────────────────────┘ │  │
│  │                                                                          │  │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │  │
│  │  │ Categorías (Sedanes, SUVs, Camionetas, etc.)                      │ │  │
│  │  │ - Secciones dinámicas configurables por admin                   │ │  │
│  │  │ - Llamadas a: useHomepageSections()                            │ │  │
│  │  └────────────────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┼─────────────────┐
                    │                 │                 │
                    ▼                 ▼                 ▼
          ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
          │ VEHICLESSALE     │ │ ADVERTISING      │ │ PAYMENT          │
          │ SERVICE          │ │ SERVICE          │ │ SERVICE          │
          └──────────────────┘ └──────────────────┘ └──────────────────┘
```

---

## Nivel 2: Flujo de Publicación

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                  DEALER/SELLER PUBLICA UN VEHÍCULO                              │
└─────────────────────────────────────────────────────────────────────────────────┘

                           FRONTEND: Sell Your Car Page
                           ↓
                           POST /api/vehicles
                           {
                             make: "Toyota",
                             model: "Camry",
                             year: 2024,
                             price: 450000,
                             dealerId: "dealer-uuid-001",
                             images: [...],
                             description: "Excelente estado"
                           }
                           ↓
          ┌────────────────────────────────────────────────────────┐
          │     VehiclesSaleService / VehiclesController          │
          │                                                        │
          │  Command: CreateVehicleCommand                        │
          │  Handler: CreateVehicleCommandHandler                 │
          │  ↓                                                    │
          │  1. Valida datos (NoSqlInjection, NoXss)             │
          │  2. Crea entidad Vehicle                             │
          │  3. Status = Active (default)                        │
          │  4. IsFeatured = false (por defecto)                 │
          │  5. IsPremium = false (por defecto)                  │
          │  6. Agrega ImagenEs                                  │
          │  7. Guarda en DB: vehiclessaleservice_db.Vehicles   │
          │  8. Emite: VehicleCreatedEvent (RabbitMQ)          │
          └────────────────────────────────────────────────────────┘
                           ↓
                  ✅ HTTP 201 Created
                  {
                    id: "vehicle-uuid-123",
                    status: "Active",
                    isFeatured: false,
                    isPremium: false
                  }
                           ↓
        IMPORTANTE: En este punto, el vehículo:
        - ✅ Existe en la base de datos
        - ✅ Es buscable por nombre/marca/modelo
        - ❌ NO aparece en la página principal
        - ❌ NO tiene publicidad pagada
        - ❌ NO está en ninguna sección del homepage

        Próximo paso: Dealer decide si quiere destacarlo
```

---

## Nivel 3: Opción A - Destacado MANUAL por Admin

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                     ADMIN PANEL: /admin/homepage                                │
│                                                                                  │
│  1. Admin ve lista de secciones:                                               │
│     - "Vehículos Destacados"                                                   │
│     - "Vehículos Premium"                                                       │
│     - "Sedanes"                                                                 │
│     - "SUVs"                                                                    │
│     - etc.                                                                      │
│                                                                                  │
│  2. Admin clica en "Vehículos Destacados"                                      │
│                                                                                  │
│  3. Admin ve botón "+ Agregar Vehículo"                                        │
│                                                                                  │
│  4. Admin busca: "2024 Toyota Camry"                                           │
│     Llamada: GET /api/vehicles/search?q=2024%20Toyota                         │
│     Resultado: [vehicle-uuid-123, vehicle-uuid-456, ...]                      │
│                                                                                  │
│  5. Admin selecciona: "2024 Toyota Camry 450k RD$"                            │
│                                                                                  │
│  6. Admin clica: "Agregar a sección"                                           │
│     Llamada: POST /api/homepagesections/destacados/vehicles                   │
└─────────────────────────────────────────────────────────────────────────────────┘
                           ↓
          ┌────────────────────────────────────────────────────────┐
          │ VehiclesSaleService / HomepageSectionsController      │
          │                                                        │
          │  Command: AssignVehicleToSectionCommand              │
          │  Handler: AssignVehicleToSectionHandler              │
          │  ↓                                                    │
          │  1. Verifica que section existe: "destacados"        │
          │  2. Verifica que vehicle existe: "vehicle-uuid-123" │
          │  3. Crea: VehicleHomepageAssignment                 │
          │     {                                                 │
          │       SectionId: "destacados",                       │
          │       VehicleId: "vehicle-uuid-123",                │
          │       DisplayOrder: 5 (al final),                    │
          │       AssignedAt: "2024-02-23T10:00:00Z"           │
          │     }                                                 │
          │  4. Opcionalmente UPDATE Vehicle                     │
          │     {                                                 │
          │       IsFeatured: true,                              │
          │       FeaturedPriority: 0,                           │
          │       FeaturedUntil: null (indefinido)              │
          │     }                                                 │
          │  5. Emite: VehicleAssignedToSectionEvent           │
          │  6. Guarda en DB                                     │
          └────────────────────────────────────────────────────────┘
                           ↓
                  ✅ HTTP 200 OK
                  {
                    message: "Vehículo asignado",
                    vehicleId: "vehicle-uuid-123",
                    section: "destacados",
                    displayOrder: 5
                  }
                           ↓
        RESULTADO: El vehículo aparece en "Vehículos Destacados"
        - ✅ Se muestra en https://okla.com.do/ sección "⭐ Destacados"
        - ✅ Sin publicidad pagada
        - ✅ Badge: ⭐ Destacado
        - ❌ Sin tracking de clicks/impresiones
```

---

## Nivel 3: Opción B - Premium PAGADO por Dealer

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                  DEALER DASHBOARD: Publicar con Publicidad                      │
│                                                                                  │
│  1. Dealer publica vehículo (Paso previo)                                       │
│                                                                                  │
│  2. Dealer ve opción: "¿Quieres destacar este vehículo?"                       │
│     [  ] No, publicar gratis                                                    │
│     [✓] Sí, quiero un plan de publicidad                                        │
│                                                                                  │
│  3. Dealer elige plan:                                                          │
│     [✓] Premium - $29/mes - Aparece en "💎 Vehículos Premium"                │
│     [ ] Featured - $9/mes - Aparece en "⭐ Vehículos Destacados"            │
│     [ ] Custom - $X/mes - Configuración personalizada                          │
│                                                                                  │
│  4. Dealer clica: "Comprar plan"                                                │
└─────────────────────────────────────────────────────────────────────────────────┘
                           ↓
              PaymentService (procesamiento de pago)
                           ↓
         ┌────────────────────────────────────────────────────────┐
         │      AdvertisingService / CampaignsController         │
         │                                                        │
         │  Command: CreateCampaignCommand                       │
         │  Handler: CreateCampaignCommandHandler                │
         │  ↓                                                    │
         │  1. Verifica pago exitoso                            │
         │  2. Crea entidad Campaign                            │
         │     {                                                 │
         │       Id: "campaign-uuid-001",                       │
         │       DealerId: "dealer-uuid-001",                  │
         │       VehicleId: "vehicle-uuid-123",                │
         │       PlacementType: "PremiumSpot",                 │
         │       Status: "Active",                              │
         │       Budget: 29.99,                                 │
         │       StartDate: "2024-02-23",                      │
         │       EndDate: "2024-03-23",                        │
         │       CreatedAt: "2024-02-23T10:05:00Z"            │
         │     }                                                 │
         │  3. Guarda en DB: advertisingservice_db.Campaigns   │
         │  4. Emite: CampaignCreatedEvent (RabbitMQ)         │
         │  5. Emite: VehiclePromotedEvent (RabbitMQ)         │
         └────────────────────────────────────────────────────────┘
                           ↓
         ┌────────────────────────────────────────────────────────┐
         │  VehiclesSaleService / Event Consumer (RabbitMQ)     │
         │                                                        │
         │  Listener: VehiclePromotedEventHandler               │
         │  ↓                                                    │
         │  On VehiclePromotedEvent:                            │
         │  - Busca: Vehicle { Id = vehicle-uuid-123 }         │
         │  - Actualiza:                                        │
         │    {                                                  │
         │      IsPremium: true,                                │
         │      LinkedCampaignId: "campaign-uuid-001",         │
         │      FeaturedUntil: "2024-03-23",                  │
         │      FeaturedPriority: 100  (mayor = más visible)  │
         │    }                                                  │
         │  - Guarda cambios                                    │
         │  - Emite: VehicleStatusChangedEvent                │
         └────────────────────────────────────────────────────────┘
                           ↓
                  ✅ HTTP 200 OK
                  {
                    campaignId: "campaign-uuid-001",
                    status: "Active",
                    vehicleId: "vehicle-uuid-123",
                    placementType: "PremiumSpot",
                    endDate: "2024-03-23",
                    budget: 29.99
                  }
                           ↓
        RESULTADO: El vehículo aparece como PREMIUM
        - ✅ Se muestra en https://okla.com.do/ sección "💎 Premium"
        - ✅ Con publicidad pagada
        - ✅ Badge: 💎 Premium (oro/dorado)
        - ✅ Prioridad alta (se muestra primero)
        - ✅ Tracking de clicks/impresiones activo
        - ✅ Dashboard del dealer muestra métricas
```

---

## Nivel 4: Frontend - Obtención de Datos

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      HOMEPAGE CLIENT COMPONENT                                  │
│         (frontend/web-next/src/app/(main)/homepage-client.tsx)                 │
│                                                                                  │
│  export default function HomepageClient({ sections }) {                        │
│                                                                                  │
│    // 1️⃣ OPCIÓN A: Obtener vehículos destacados (ADMIN)                       │
│    const { data: homepageSections } = useHomepageSections();                  │
│    // Llamada: GET /api/homepagesections/homepage                             │
│    // Intervalo: 10 minutos (staleTime)                                       │
│    // Retorna: Array de HomepageSection con vehículos asignados              │
│                                                                                  │
│    // 2️⃣ OPCIÓN B: Obtener vehículos premium (PAGADO)                        │
│    const { data: featuredRotation } = useHomepageRotation('FeaturedSpot');   │
│    // Llamada: GET /api/advertising/rotation?placementType=FeaturedSpot      │
│    // Intervalo: 5 minutos (staleTime)                                       │
│    // Retorna: Array de vehículos destacados pagados                         │
│                                                                                  │
│    const { data: premiumRotation } = useHomepageRotation('PremiumSpot');     │
│    // Llamada: GET /api/advertising/rotation?placementType=PremiumSpot       │
│    // Intervalo: 5 minutos (staleTime)                                       │
│    // Retorna: Array de vehículos premium pagados                            │
│                                                                                  │
│    return (                                                                     │
│      <>                                                                         │
│        {/* Hero Carousel - Secciones o Destacados */}                         │
│        <HeroCompact vehicles={heroVehicles} />                                │
│                                                                                  │
│        {/* ⭐ Vehículos Destacados (Admin) */}                                │
│        <FeaturedVehicles                                                       │
│          title="⭐ Vehículos Destacados"                                       │
│          placementType="FeaturedSpot"                                          │
│          maxItems={8}                                                           │
│        />                                                                       │
│                                                                                  │
│        {/* 💎 Vehículos Premium (Pagado) */}                                  │
│        <FeaturedVehicles                                                       │
│          title="💎 Vehículos Premium"                                          │
│          placementType="PremiumSpot"                                           │
│          maxItems={4}                                                           │
│        />                                                                       │
│                                                                                  │
│        {/* Secciones dinámicas (Sedanes, SUVs, etc.) */}                      │
│        {homepageSections?.map(section => (                                    │
│          <FeaturedSection                                                      │
│            key={section.id}                                                    │
│            title={section.name}                                                │
│            listings={transformVehicles(section.vehicles)}                      │
│          />                                                                    │
│        ))}                                                                      │
│      </>                                                                        │
│    );                                                                           │
│  }                                                                              │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Nivel 5: Frontend - Renderización en Pantalla

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           HOMEPAGE RENDERIZADA                                  │
│                        https://okla.com.do/                                     │
│                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ HERO CAROUSEL                                                            │ │
│  │ ┌────────────┐ ┌────────────┐ ┌────────────┐                          │ │
│  │ │ 2024 BMW X7│ │ 2024 Merc. │ │ 2024 Jeep  │  ◄─ Rotación automática │ │
│  │ │ $850,000   │ │ $920,000   │ │ $650,000   │     cada 5 segundos    │ │
│  │ └────────────┘ └────────────┘ └────────────┘                          │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                                │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ ⭐ VEHÍCULOS DESTACADOS (Admin - Gratis)                               │ │
│  │                                                                          │ │
│  │ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │ │
│  │ │⭐ Badge │ │⭐ Badge │ │⭐ Badge │ │⭐ Badge │ │⭐ Badge │          │ │
│  │ │ 2024    │ │ 2023    │ │ 2024    │ │ 2022    │ │ 2024    │          │ │
│  │ │Toyota   │ │ Honda   │ │ Mazda   │ │ Ford    │ │ Kia     │          │ │
│  │ │Camry    │ │ Accord  │ │CX-5     │ │Mustang  │ │Sportage │          │ │
│  │ │450k RD$ │ │520k RD$ │ │480k RD$ │ │550k RD$ │ │420k RD$ │          │ │
│  │ └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │ │
│  │                           Ver todos →                                  │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                                │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ 💎 VEHÍCULOS PREMIUM (Pagado - Publicidad)                             │ │
│  │                                                                          │ │
│  │ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐                       │ │
│  │ │💎 Badge │ │💎 Badge │ │💎 Badge │ │💎 Badge │                       │ │
│  │ │(Oro)    │ │(Oro)    │ │(Oro)    │ │(Oro)    │                       │ │
│  │ │ 2024    │ │ 2024    │ │ 2023    │ │ 2024    │                       │ │
│  │ │Audi     │ │Porsche  │ │Range    │ │Mercedes │                       │ │
│  │ │A6       │ │911      │ │Rover    │ │G-Class  │                       │ │
│  │ │750k RD$ │ │1.2M RD$ │ │980k RD$ │ │1.1M RD$ │                       │ │
│  │ └─────────┘ └─────────┘ └─────────┘ └─────────┘                       │ │
│  │  "Editar" campaign en dealer dashboard  ◄── Solo si es del dealer     │ │
│  │  Visto: 1,234 veces | Clicks: 45                                       │ │
│  │                                                                          │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                                │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ 🚗 SEDANES (Sección dinámica del admin)                                │ │
│  │                                                                          │ │
│  │ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │ │
│  │ │ 2024    │ │ 2023    │ │ 2024    │ │ 2022    │ │ 2024    │          │ │
│  │ │Toyota   │ │ Honda   │ │ Mazda   │ │ Nissan  │ │ Subaru  │          │ │
│  │ │Corolla  │ │Civic    │ │3        │ │Altima   │ │Legacy   │          │ │
│  │ │380k RD$ │ │400k RD$ │ │350k RD$ │ │420k RD$ │ │480k RD$ │          │ │
│  │ └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │ │
│  │                                                                          │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                                │
│  ... más secciones dinámicas configurables por admin                         │
│                                                                                │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Nivel 6: Tracking & Métricas

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                       TRACKING DE PUBLICIDAD (Premium)                          │
│                                                                                  │
│  USUARIO VE EL VEHÍCULO PREMIUM                                                │
│  ↓                                                                               │
│  useEffect(() => {                                                             │
│    recordImpression.mutate({                                                   │
│      campaignId: "campaign-uuid-001",                                          │
│      vehicleId: "vehicle-uuid-123",                                            │
│      section: "PremiumSpot"                                                    │
│    });                                                                          │
│  }, [vehicle.campaignId]);                                                     │
│  ↓                                                                               │
│  POST /api/advertising/tracking/impression                                     │
│  ↓                                                                               │
│  AdvertisingService / TrackingController                                       │
│  ├─ Crea Impression record                                                     │
│  ├─ campaignId: "campaign-uuid-001"                                           │
│  ├─ vehicleId: "vehicle-uuid-123"                                             │
│  ├─ timestamp: "2024-02-23T15:30:45Z"                                         │
│  ├─ section: "PremiumSpot"                                                    │
│  └─ Guarda en DB: advertisingservice_db.Impressions                           │
│                                                                                  │
│                                                                                  │
│  USUARIO CLICA EN VEHÍCULO PREMIUM                                             │
│  ↓                                                                               │
│  <Link                                                                          │
│    href={`/vehiculos/${vehicle.slug}`}                                        │
│    onClick={() => {                                                            │
│      recordClick.mutate({                                                      │
│        campaignId: "campaign-uuid-001",                                        │
│        vehicleId: "vehicle-uuid-123",                                          │
│        section: "PremiumSpot"                                                  │
│      });                                                                        │
│    }}                                                                           │
│  >                                                                              │
│  ↓                                                                               │
│  POST /api/advertising/tracking/click                                          │
│  ↓                                                                               │
│  AdvertisingService / TrackingController                                       │
│  ├─ Crea Click record                                                          │
│  ├─ campaignId: "campaign-uuid-001"                                           │
│  ├─ vehicleId: "vehicle-uuid-123"                                             │
│  ├─ timestamp: "2024-02-23T15:30:50Z"                                         │
│  ├─ section: "PremiumSpot"                                                    │
│  └─ Guarda en DB: advertisingservice_db.Clicks                                │
│  ├─ Actualiza Campaign.Clicks += 1                                            │
│  └─ Si umbral de budget alcanzado, pausa campaña (opcional)                   │
│                                                                                  │
│                                                                                  │
│  DEALER VE DASHBOARD                                                           │
│  ↓                                                                               │
│  /dealer/campaigns/campaign-uuid-001                                           │
│  ↓                                                                               │
│  GET /api/advertising/campaigns/campaign-uuid-001/stats                        │
│  ↓                                                                               │
│  Response:                                                                      │
│  {                                                                              │
│    campaignId: "campaign-uuid-001",                                           │
│    vehicleTitle: "2024 Audi A6",                                              │
│    vehicleId: "vehicle-uuid-123",                                             │
│    status: "Active",                                                           │
│    startDate: "2024-02-23",                                                   │
│    endDate: "2024-03-23",                                                     │
│    budget: 29.99,                                                             │
│    spent: 14.99,                                                              │
│    remaining: 15.00,                                                          │
│    impressions: 1234,                                                         │
│    clicks: 45,                                                                 │
│    ctr: 3.65,  // Click-Through Rate                                         │
│    costPerClick: 0.33,                                                        │
│    lastUpdated: "2024-02-23T16:00:00Z"                                        │
│  }                                                                              │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Nivel 7: Base de Datos - Schema

```sql
-- VehiclesSaleService Database
CREATE TABLE public."Vehicles" (
  "Id" uuid PRIMARY KEY,
  "Make" varchar,
  "Model" varchar,
  "Year" int,
  "Price" numeric,
  "Mileage" int,
  "FuelType" varchar,
  "Transmission" varchar,
  "Description" text,
  "Status" int,                    -- 0=Draft, 1=Active, 2=Sold, etc.

  -- ⚠️ CAMPOS FALTANTES:
  "IsFeatured" boolean DEFAULT false,
  "IsPremium" boolean DEFAULT false,
  "LinkedCampaignId" uuid,         -- FK a AdvertisingService.Campaigns
  "FeaturedUntil" timestamp,
  "FeaturedPriority" int DEFAULT 0,

  "DealerId" uuid,
  "SellerId" uuid,
  "CreatedAt" timestamp,
  "UpdatedAt" timestamp,
  "DeletedAt" timestamp
);

CREATE TABLE public."VehicleHomepageAssignments" (
  "Id" uuid PRIMARY KEY,
  "SectionId" varchar,              -- "destacados", "premium", "sedanes", etc.
  "VehicleId" uuid NOT NULL,        -- FK to Vehicles
  "DisplayOrder" int,
  "AssignedAt" timestamp,

  FOREIGN KEY ("VehicleId") REFERENCES "Vehicles"("Id")
);

-- AdvertisingService Database
CREATE TABLE public."Campaigns" (
  "Id" uuid PRIMARY KEY,
  "DealerId" uuid NOT NULL,
  "VehicleId" uuid NOT NULL,
  "PlacementType" varchar,          -- "FeaturedSpot", "PremiumSpot"
  "Status" int,                     -- 0=Draft, 1=Active, 2=Paused, 3=Ended
  "Budget" numeric,
  "Spent" numeric DEFAULT 0,
  "StartDate" timestamp,
  "EndDate" timestamp,
  "CreatedAt" timestamp,
  "UpdatedAt" timestamp
);

CREATE TABLE public."Impressions" (
  "Id" uuid PRIMARY KEY,
  "CampaignId" uuid NOT NULL,
  "VehicleId" uuid NOT NULL,
  "Section" varchar,
  "Timestamp" timestamp,

  FOREIGN KEY ("CampaignId") REFERENCES "Campaigns"("Id")
);

CREATE TABLE public."Clicks" (
  "Id" uuid PRIMARY KEY,
  "CampaignId" uuid NOT NULL,
  "VehicleId" uuid NOT NULL,
  "Section" varchar,
  "Timestamp" timestamp,

  FOREIGN KEY ("CampaignId") REFERENCES "Campaigns"("Id")
);

-- Índices para performance
CREATE INDEX idx_vehicles_isfeatured ON "Vehicles"("IsFeatured");
CREATE INDEX idx_vehicles_ispremium ON "Vehicles"("IsPremium");
CREATE INDEX idx_vehicles_linkedcampaign ON "Vehicles"("LinkedCampaignId");
CREATE INDEX idx_homepage_sectionid ON "VehicleHomepageAssignments"("SectionId");
CREATE INDEX idx_campaigns_dealerid ON "Campaigns"("DealerId");
CREATE INDEX idx_campaigns_vehicleid ON "Campaigns"("VehicleId");
CREATE INDEX idx_campaigns_status ON "Campaigns"("Status");
CREATE INDEX idx_impressions_campaignid ON "Impressions"("CampaignId");
CREATE INDEX idx_clicks_campaignid ON "Clicks"("CampaignId");
```

---

## Nivel 8: Eventos RabbitMQ

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    EVENTOS PUBLICADOS EN RABBITMQ                               │
└─────────────────────────────────────────────────────────────────────────────────┘

EVENTO: vehicle.created
───────────────────────
Publicado por: VehiclesSaleService
Consumido por: AdvertisingService (opcional)
Payload:
{
  eventId: "uuid",
  eventType: "vehicle.created",
  aggregateId: "vehicle-uuid-123",
  timestamp: "2024-02-23T10:00:00Z",
  data: {
    vehicleId: "vehicle-uuid-123",
    make: "Toyota",
    model: "Camry",
    year: 2024,
    price: 450000,
    dealerId: "dealer-uuid-001"
  }
}


EVENTO: campaign.created
────────────────────────
Publicado por: AdvertisingService
Consumido por: VehiclesSaleService
Payload:
{
  eventId: "uuid",
  eventType: "campaign.created",
  aggregateId: "campaign-uuid-001",
  timestamp: "2024-02-23T10:05:00Z",
  data: {
    campaignId: "campaign-uuid-001",
    vehicleId: "vehicle-uuid-123",
    dealerId: "dealer-uuid-001",
    placementType: "PremiumSpot",
    budget: 29.99,
    endDate: "2024-03-23"
  }
}

Handler en VehiclesSaleService:
─────────────────────────────────
public class VehiclePromotedEventHandler : IEventHandler<CampaignCreatedEvent>
{
    public async Task Handle(CampaignCreatedEvent @event)
    {
        var vehicle = await _vehicleRepo.GetById(@event.Data.VehicleId);
        if (vehicle != null)
        {
            vehicle.SetPremium(
                true,
                @event.Data.CampaignId,
                @event.Data.EndDate,
                priority: 100
            );
            await _vehicleRepo.UpdateAsync(vehicle);
        }
    }
}


EVENTO: campaign.expired
────────────────────────
Publicado por: AdvertisingService (en scheduled job)
Consumido por: VehiclesSaleService
Payload:
{
  eventId: "uuid",
  eventType: "campaign.expired",
  aggregateId: "campaign-uuid-001",
  timestamp: "2024-03-23T23:59:59Z",
  data: {
    campaignId: "campaign-uuid-001",
    vehicleId: "vehicle-uuid-123",
    dealerId: "dealer-uuid-001"
  }
}

Handler en VehiclesSaleService:
─────────────────────────────────
public class VehicleUnpromotedEventHandler : IEventHandler<CampaignExpiredEvent>
{
    public async Task Handle(CampaignExpiredEvent @event)
    {
        var vehicle = await _vehicleRepo.GetById(@event.Data.VehicleId);
        if (vehicle != null)
        {
            vehicle.SetPremium(false, null, null, priority: 0);
            await _vehicleRepo.UpdateAsync(vehicle);
        }
    }
}
```

---

## Mapa de URLs Backend

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    GATEWAY ROUTING (port 8080)                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

VEHICLESALESERVICE ENDPOINTS (via /api/vehicles/*)
──────────────────────────────────────────────────
GET   /api/vehicles                          → List all vehicles
POST  /api/vehicles                          → Create vehicle
GET   /api/vehicles/{id}                     → Get vehicle detail
PUT   /api/vehicles/{id}                     → Update vehicle
DELETE /api/vehicles/{id}                    → Delete vehicle
POST  /api/vehicles/{id}/images              → Add images
GET   /api/vehicles/search?q=...             → Search vehicles

CATALOGCONTROLLER ENDPOINTS
────────────────────────────
GET   /api/catalog/makes                     → List all makes
GET   /api/catalog/makes/popular             → Popular makes
GET   /api/catalog/makes/{makeSlug}/models   → Models for make
GET   /api/catalog/models/{modelId}/years    → Years available
GET   /api/catalog/models/{modelId}/years/{year}/trims → Trims

FAVORITESCONTROLLER ENDPOINTS
──────────────────────────────
GET   /api/favorites                         → List my favorites
GET   /api/favorites/count                   → Count favorites
POST  /api/favorites/{vehicleId}             → Add to favorites
DELETE /api/favorites/{vehicleId}            → Remove from favorites

HOMEPAGESECTIONSCONTROLLER ENDPOINTS
────────────────────────────────────
GET   /api/homepagesections                  → List all sections
GET   /api/homepagesections/homepage         → Get active sections
GET   /api/homepagesections/{slug}           → Get section detail
POST  /api/homepagesections                  → Create section (admin)
PUT   /api/homepagesections/{slug}           → Update section (admin)
DELETE /api/homepagesections/{slug}          → Delete section (admin)
POST  /api/homepagesections/{slug}/vehicles  → Assign vehicle
DELETE /api/homepagesections/{slug}/vehicles/{vehicleId} → Remove vehicle


ADVERTISINGSERVICE ENDPOINTS (via /api/advertising/*)
──────────────────────────────────────────────────────
GET   /api/advertising/campaigns             → List my campaigns
POST  /api/advertising/campaigns             → Create campaign (PAGO)
GET   /api/advertising/campaigns/{id}        → Get campaign detail
PUT   /api/advertising/campaigns/{id}        → Update campaign
DELETE /api/advertising/campaigns/{id}       → Cancel campaign
POST  /api/advertising/campaigns/{id}/pause  → Pause campaign
POST  /api/advertising/campaigns/{id}/resume → Resume campaign
GET   /api/advertising/rotation?placementType=FeaturedSpot  → Get rotation
POST  /api/advertising/tracking/impression   → Track impression
POST  /api/advertising/tracking/click        → Track click
GET   /api/advertising/campaigns/{id}/stats  → Get campaign stats
```

---

_Documento generado: 2026-02-23_  
_Versión: 2.0 TÉCNICA_  
_Estado: COMPLETO_
