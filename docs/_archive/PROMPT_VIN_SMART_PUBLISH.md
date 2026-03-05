# 🚗 PROMPT: Smart Vehicle Publishing con VIN — OKLA Platform

> **Objetivo:** Implementar un sistema de publicación de vehículos inteligente basado en VIN que auto-rellene formularios, valide datos, estime precios, y ofrezca múltiples caminos de publicación (VIN, manual, importación CSV para dealers). Debe funcionar tanto para vendedores individuales como para dealers.

---

## 📋 CONTEXTO DEL PROYECTO

OKLA es un marketplace de compra/venta de vehículos en **República Dominicana**. Arquitectura de **microservicios .NET 8** con **Clean Architecture**, frontend **Next.js 14 App Router**, desplegado en **Digital Ocean Kubernetes**.

### Stack Relevante

- **Backend:** .NET 8, PostgreSQL 16, RabbitMQ 3.12, Redis 7, Ocelot Gateway
- **Frontend:** Next.js 14 + TypeScript + App Router, pnpm, shadcn/ui, React Query (TanStack Query)
- **Patrones:** CQRS (MediatR en algunos servicios), Repository Pattern, Result Pattern, Domain Events via RabbitMQ
- **Seguridad:** JWT Bearer, FluentValidation con `.NoSqlInjection()` y `.NoXss()`, CSRF protection, input sanitization

### Servicios Existentes Relevantes

| Servicio                       | Puerto Dev | Responsabilidad                                                  | Estado                   |
| ------------------------------ | ---------- | ---------------------------------------------------------------- | ------------------------ |
| **VehiclesSaleService**        | 15104      | CRUD vehículos, catálogo makes/models, VIN decode básico (NHTSA) | ✅ En producción         |
| **VehicleIntelligenceService** | 5056       | Pricing IA, análisis de precios, predicción de demanda           | ✅ Existe (MediatR/CQRS) |
| **InventoryManagementService** | 5040       | Inventario dealers, import/export, batch editing                 | ✅ Existe                |
| **SpyneIntegrationService**    | -          | Background removal, 360° spins, image enhancement (API Spyne AI) | ✅ Existe                |
| **MediaService**               | 15105      | Upload imágenes a S3, thumbnails, CDN                            | ✅ En producción         |
| **DealerManagementService**    | 5039       | Perfiles dealers, sucursales, verificación                       | ✅ Existe                |
| **DealerAnalyticsService**     | 5041       | Métricas, dashboard, conversiones                                | ✅ Existe                |
| **KYCService**                 | 15180      | Verificación de identidad                                        | ✅ En producción         |
| **NotificationService**        | 15105      | Email (Resend), SMS (Twilio), Push (Firebase)                    | ✅ En producción         |
| **AuditService**               | 15112      | Auditoría centralizada                                           | ✅ En producción         |
| **IdempotencyService**         | 15136      | Control de operaciones duplicadas                                | ✅ En producción         |

### APIs Externas Disponibles

- **NHTSA VPIC API** (gratuita, sin API key) — Ya integrada parcialmente en CatalogController
- **Spyne AI** — Configurada con API key para procesamiento de imágenes
- **Stripe / Azul** — Pasarelas de pago configuradas
- **AWS S3** — Almacenamiento de imágenes (bucket: okla-images-2026, region: us-east-2)
- **Google Maps** — API key configurada
- **Resend** — Email transaccional configurado

---

## 🏗️ ESTADO ACTUAL DEL CÓDIGO (Lo que ya existe)

### Backend — VehiclesSaleService

#### Entidad Vehicle (~60 propiedades)

```
Vehicle.cs tiene: VIN (string?, max 17, unique index), StockNumber, Make, MakeId, Model, ModelId, Year,
Trim, TrimId, VehicleType (enum), BodyStyle (enum), Doors, EngineSize, Cylinders, Horsepower,
FuelType (enum), TransmissionType (enum), DriveType (enum), NumberOfSpeeds, Mileage, MileageUnit,
Condition (enum), ExteriorColor, InteriorColor, HasAccidentHistory, NumberOfOwners,
HasCleanTitle, IsCarfaxAvailable, Features (jsonb), SafetyFeatures (jsonb), etc.
```

#### CatalogController — VIN Decode Existente

- Endpoint: `GET /api/catalog/vin/{vin}/decode`
- Llama a NHTSA VPIC: `https://vpic.nhtsa.dot.gov/api/vehicles/decodevinvalues/{vin}?format=json`
- Mapea campos NHTSA → enums locales (FuelType, Transmission, DriveType, BodyStyle, VehicleType)
- Retorna `VinDecodeResult` con `FormAutoFillData` para auto-rellenar formularios
- **Validación:** 17 caracteres, sin I/O/Q

#### VehiclesController — Create Vehicle

- Endpoint: `POST /api/vehicles`
- Acepta `CreateVehicleRequest` con todos los campos del vehículo
- Publica `VehicleCreatedEvent` a RabbitMQ
- **NO usa MediatR/CQRS** — lógica directa en controller

#### Catálogo de Datos

- Makes, Models, Trims con relaciones en BD
- Años, body types, fuel types, transmissions, drive types, colores como enums/endpoints estáticos
- Provincias de RD hardcoded
- Endpoint de seed: `POST /api/catalog/seed`

### Backend — VehicleIntelligenceService (Usa MediatR/CQRS)

- `POST /api/pricing/analyze` → `AnalyzePriceCommand` → `PriceAnalysisResult`
- `GET /api/pricing/vehicle/{vehicleId}/latest` → última análisis
- Tiene entidades: `PriceAnalysis`, `MarketDataSnapshot`, `PricingModel`

### Backend — InventoryManagementService (Usa MediatR/CQRS)

- CRUD de `InventoryItem` con CostPrice, ListPrice, TargetPrice, MinAcceptablePrice
- Bulk operations, featured items, hot items, overdue items
- Filtros por dealer, status, búsqueda

### Frontend — 3 Flujos de Publicación SEPARADOS (Problema actual)

#### Flujo 1: `/publicar/` — Wizard 4 pasos (vendedor individual)

- **NO tiene campo VIN**
- Steps: Info Básica → Fotos → Precio/Ubicación → Revisión
- Usa hooks de catálogo, sanitización aplicada
- 953 líneas

#### Flujo 2: `/vender/publicar/` — Wizard 5 pasos (con VIN stub)

- Tiene input de VIN y botón "Decodificar" **SIN funcionalidad** (no tiene onClick handler)
- Steps: Info + VIN → Fotos → Características → Precio → Revisión
- Requiere KYC (VerificationGate)
- Auto-save en localStorage
- 1024 líneas

#### Flujo 3: `/dealer/publicar/` — Formulario single-page (dealer)

- Tiene campo VIN pero **sin decodificación**
- Hasta 20 fotos
- **Sin sanitización** de inputs
- 424 líneas

### Frontend — Services y Hooks Existentes

#### `services/vehicles.ts` (809 líneas)

```typescript
// Funciones de catálogo existentes (con fallback estático):
(getMakes(),
  getModelsByMake(),
  getYears(),
  getBodyTypes(),
  getFuelTypes(),
  getTransmissions(),
  getDriveTypes(),
  getColors(),
  getProvinces());

// CRUD existente:
(createVehicle(),
  updateVehicle(),
  deleteVehicle(),
  getVehicles(),
  getVehicleById(),
  getVehicleBySlug());

// ❌ NO EXISTE: decodeVin() en el frontend service
```

#### `hooks/use-vehicles.ts` (302 líneas)

```typescript
// Hooks existentes:
(useVehicles(),
  useVehicle(),
  useVehicleBySlug(),
  useMyVehicles(),
  useDealerVehicles(),
  useCreateVehicle(),
  useUpdateVehicle());

// Catalog hooks (24h staleTime):
(useMakes(),
  useModels(),
  useYears(),
  useBodyTypes(),
  useFuelTypes(),
  useTransmissions(),
  useDriveTypes(),
  useColors());

// ❌ NO EXISTE: useDecodeVin() hook
```

#### `hooks/use-media.ts`

```typescript
(useUploadImage(),
  useUploadMultipleImages(),
  useUploadFile(),
  useDeleteMedia());
```

#### `hooks/use-dealers.ts` (20+ hooks)

```typescript
useCurrentDealer(), useDealerById(), useDealerDashboardData(),
useCreateDealer(), useUpdateDealer(), etc.
```

### Gateway — Rutas Existentes

```
/api/vehicles/* → vehiclessaleservice
/api/catalog/* → vehiclessaleservice
/api/pricing/* → vehicleintelligenceservice
/api/inventory/* → inventorymanagementservice
/api/media/* → mediaservice
/api/spyne/* → spyneintegrationservice
/api/dealers/* → dealermanagementservice
```

---

## 🎯 REQUERIMIENTOS DE IMPLEMENTACIÓN

### Objetivo Principal

Crear un **sistema de publicación unificado e inteligente** que:

1. Use el VIN como **método principal** para auto-rellenar el 80%+ del formulario
2. Ofrezca **alternativas** cuando el VIN no está disponible o falla
3. Funcione tanto para **vendedores individuales** como para **dealers**
4. Se integre con los servicios existentes (Pricing, Spyne, Media, etc.)
5. Sea **más rápido y eficiente** que cualquier competidor en RD

### Flujos de Publicación Requeridos

#### Flujo A: Smart VIN Publish (Individual + Dealer)

```
┌─────────────────────────────────────────────────────────────────┐
│  PASO 0: Elección de Método                                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────────┐   │
│  │ 📷 Foto  │  │ 🔢 VIN   │  │ ✍️ Manual │  │ 📄 CSV/Excel  │   │
│  │ del VIN  │  │ Teclado  │  │ Paso a   │  │ (Solo Dealer) │   │
│  │ (cámara) │  │ (tipear) │  │ Paso     │  │ Import masivo │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └──────┬────────┘   │
│       │              │             │                │            │
│       ▼              ▼             │                │            │
│  OCR/decode    NHTSA decode        │                │            │
│       │              │             │                │            │
│       └──────┬───────┘             │                │            │
│              ▼                     │                │            │
│   Auto-fill formulario             │                │            │
│   + Verificar duplicado            │                │            │
│   + Estimación de precio           │                │            │
│              │                     │                │            │
│              ▼                     ▼                ▼            │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  PASO 1: Verificar/Completar Info del Vehículo          │    │
│  │  (campos pre-llenados editables + campos faltantes)     │    │
│  └─────────────────────────────────────────────────────────┘    │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  PASO 2: Fotos del Vehículo                             │    │
│  │  - Upload múltiple con drag & drop                       │    │
│  │  - Auto-enhance con Spyne AI (opcional)                  │    │
│  │  - Background removal (dealers con suscripción)          │    │
│  │  - Guía visual de qué fotos tomar                        │    │
│  └─────────────────────────────────────────────────────────┘    │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  PASO 3: Precio y Detalles                               │    │
│  │  - Sugerencia de precio (VehicleIntelligenceService)     │    │
│  │  - Rango de mercado visual                                │    │
│  │  - Descripción (auto-generada como template)             │    │
│  │  - Ubicación (provincia/ciudad RD)                        │    │
│  │  - Contacto del vendedor                                  │    │
│  └─────────────────────────────────────────────────────────┘    │
│              │                                                   │
│              ▼                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  PASO 4: Revisión y Publicación                          │    │
│  │  - Preview tipo listing real                              │    │
│  │  - Checklist de completitud (score de calidad)            │    │
│  │  - "Listing Quality Score" con tips para mejorar          │    │
│  │  - Publicar / Guardar borrador                            │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

#### Flujo B: CSV/Excel Import (Solo Dealers)

```
Upload CSV/Excel → Validación por fila → Preview tabla editable →
VIN decode masivo (batch) → Confirmación → Creación masiva con progreso
```

---

## 📐 ESPECIFICACIONES TÉCNICAS DETALLADAS

### 1. Backend — Mejorar VIN Decode en VehiclesSaleService

#### 1.1 Nuevo endpoint enriquecido de VIN decode

El endpoint actual `GET /api/catalog/vin/{vin}/decode` ya llama a NHTSA. Se necesita **enriquecer** con:

```
GET /api/catalog/vin/{vin}/decode-smart
```

**Lógica:**

1. Validar formato VIN (17 chars, sin I/O/Q, checksum dígito 9 válido)
2. Verificar si el VIN ya existe en la BD → Si existe, retornar warning con link al listado existente
3. Llamar a NHTSA VPIC API para datos base
4. Hacer match automático contra el catálogo local de Makes/Models/Trims:
   - Buscar Make por nombre (fuzzy match: "TOYOTA" → make.Name == "Toyota")
   - Buscar Model por nombre dentro del Make encontrado
   - Buscar Trim por nombre dentro del Model
   - Si hay match, retornar los IDs del catálogo para pre-seleccionar dropdowns
5. Calcular año del modelo a partir del VIN (posición 10 = year code)
6. Retornar datos enriquecidos incluyendo:
   - Datos decodificados del vehículo
   - IDs del catálogo local (makeId, modelId, trimId) si hay match
   - Flag de VIN duplicado
   - Datos de confianza por campo (qué tan seguro es cada dato)
   - Template de descripción auto-generada

**Response DTO:**

```csharp
public record SmartVinDecodeResult
{
    // Datos del vehículo decodificado
    public string VIN { get; init; }
    public string Make { get; init; }
    public string Model { get; init; }
    public int? Year { get; init; }
    public string? Trim { get; init; }
    public string? BodyStyle { get; init; }
    public string? VehicleType { get; init; }
    public string? EngineSize { get; init; }
    public int? Cylinders { get; init; }
    public int? Horsepower { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? DriveType { get; init; }
    public int? Doors { get; init; }
    public string? ManufacturedIn { get; init; }
    public string? PlantCountry { get; init; }

    // Match con catálogo local
    public int? CatalogMakeId { get; init; }
    public int? CatalogModelId { get; init; }
    public int? CatalogTrimId { get; init; }
    public bool HasCatalogMatch { get; init; }

    // Duplicado
    public bool IsDuplicate { get; init; }
    public Guid? ExistingVehicleId { get; init; }
    public string? ExistingVehicleSlug { get; init; }

    // Calidad
    public Dictionary<string, FieldConfidence> FieldConfidences { get; init; }
    public string? SuggestedDescription { get; init; }

    // Auto-fill optimizado para el frontend
    public FormAutoFillData AutoFill { get; init; }
}

public record FieldConfidence(string Value, string Source, double Confidence); // 0.0 - 1.0
```

#### 1.2 VIN Duplicate Check endpoint

```
GET /api/vehicles/vin/{vin}/exists
```

Retorna `{ exists: bool, vehicleId?: Guid, slug?: string, status?: string }`.
Rápido, sin hacer decode. Para validación en tiempo real mientras el usuario tipea.

#### 1.3 Batch VIN Decode (para dealers)

```
POST /api/catalog/vin/decode-batch
Body: { "vins": ["VIN1", "VIN2", ...], "maxItems": 50 }
```

- Máximo 50 VINs por request
- Procesa en paralelo con rate limiting a NHTSA (max 5 concurrent)
- Retorna array de `SmartVinDecodeResult` con errores por VIN individual
- Usar IdempotencyService para evitar decode duplicado del mismo VIN

#### 1.4 Sugerencia de Precio vía VehicleIntelligenceService

Cuando el VIN es decodificado, automáticamente solicitar una estimación de precio:

```
POST /api/pricing/estimate-by-specs
Body: { make, model, year, trim, mileage, condition, fuelType }
```

Retorna: `{ suggestedPrice: decimal, priceRange: { min, max }, marketPosition: string, confidence: double }`

Si VehicleIntelligenceService no tiene datos suficientes, calcular un estimado básico basado en vehículos similares en la BD (misma marca/modelo/año ± 1 año).

#### 1.5 Auto-generación de Descripción

Crear un endpoint o lógica interna que genere una descripción template:

```
"[Year] [Make] [Model] [Trim] en [condición]. Motor [EngineSize]L [Cylinders] cilindros,
[Horsepower] HP. Transmisión [TransmissionType], tracción [DriveType]. [Mileage] km recorridos.
[FuelType]. Ubicado en [Province], República Dominicana."
```

### 2. Frontend — Componente Unificado de Publicación

#### 2.1 Unificar los 3 flujos en uno solo

Actualmente existen 3 páginas de publicación separadas (`/publicar/`, `/vender/publicar/`, `/dealer/publicar/`). **Unificarlas** en un solo componente inteligente:

**Ruta principal:** `/publicar` (redirige a `/vender/publicar` si no está autenticado)
**Ruta dealer:** `/dealer/publicar` (wrapper que usa el mismo componente con `mode="dealer"`)

#### 2.2 Componente `SmartPublishWizard`

Crear en `src/components/vehicles/smart-publish/`:

```
src/components/vehicles/smart-publish/
├── smart-publish-wizard.tsx       # Componente principal (wizard container)
├── method-selector.tsx            # Paso 0: Elegir método (VIN foto, VIN teclado, Manual, CSV)
├── vin-scanner.tsx                # Captura VIN por cámara (OCR)
├── vin-input.tsx                  # Input VIN con validación en tiempo real
├── vin-decode-results.tsx         # Resultados del decode con preview
├── vehicle-info-form.tsx          # Paso 1: Formulario info vehículo
├── photo-upload-step.tsx          # Paso 2: Upload de fotos
├── photo-guide.tsx                # Guía visual de fotos recomendadas
├── pricing-step.tsx               # Paso 3: Precio con sugerencia
├── price-suggestion-card.tsx      # Card de sugerencia de precio
├── review-step.tsx                # Paso 4: Revisión final
├── listing-quality-score.tsx      # Score de calidad del listing
├── csv-import-wizard.tsx          # Flujo CSV para dealers
├── csv-preview-table.tsx          # Tabla preview del CSV
└── index.ts                       # Exports
```

#### 2.3 `vin-input.tsx` — Input inteligente de VIN

- Input con `maxLength={17}`, `font-mono`, auto-uppercase
- Validación en tiempo real:
  - Formato: Solo A-HJ-NPR-Z0-9 (excluye I, O, Q)
  - Longitud: Muestra progreso "12/17 caracteres"
  - Checksum: Validar dígito de verificación (posición 9) cuando tiene 17 chars
  - Duplicado: Debounce 500ms → `GET /api/vehicles/vin/{vin}/exists`
- Estados visuales:
  - ⚪ Vacío/incompleto
  - 🟡 Formato inválido
  - 🔴 VIN duplicado (con link al vehículo existente)
  - 🟢 VIN válido y disponible
  - ⏳ Verificando...
- Botón "Decodificar" que llama a `GET /api/catalog/vin/{vin}/decode-smart`
- Auto-decode cuando alcanza 17 caracteres válidos (sin necesidad de presionar botón)

#### 2.4 `vin-scanner.tsx` — Escaneo por Cámara

- Usar la cámara del dispositivo para capturar foto de la placa VIN
- OCR client-side usando **Tesseract.js** (librería JavaScript para OCR)
- Flujo:
  1. Abrir cámara con guía de enfoque (rectángulo donde posicionar el VIN)
  2. Capturar imagen
  3. Procesar OCR → extraer texto
  4. Limpiar texto: remover espacios, solo chars válidos VIN
  5. Si detecta 17 chars válidos → auto-fill el `vin-input`
  6. Si no detecta → mostrar mensaje "No se pudo leer, intenta de nuevo o escríbelo manualmente"
- Usar `react-webcam` (ya instalado en el proyecto para KYC)
- Fallback: Input file para subir foto del VIN

#### 2.5 `method-selector.tsx` — Selector de Método

Pantalla inicial atractiva con 3-4 cards grandes:

```
┌─────────────────────────────────────────────────────────────┐
│           ¿Cómo quieres publicar tu vehículo?               │
│                                                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ 📷          │  │ ⌨️          │  │ ✍️                   │  │
│  │ Escanear    │  │ Escribir    │  │ Llenar              │  │
│  │ VIN         │  │ VIN         │  │ Manualmente         │  │
│  │             │  │             │  │                     │  │
│  │ Más rápido  │  │ Rápido      │  │ Sin VIN disponible  │  │
│  │ ~2 min      │  │ ~3 min      │  │ ~5-8 min            │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│                                                              │
│  ┌─────────────────────────────────────────┐ (Solo Dealers) │
│  │ 📄 Importar inventario desde CSV/Excel  │                │
│  │ Publica hasta 50 vehículos a la vez     │                │
│  └─────────────────────────────────────────┘                │
│                                                              │
│  ℹ️ ¿Dónde encuentro el VIN?                                │
│  [Imagen mostrando ubicaciones comunes del VIN en un auto]  │
└─────────────────────────────────────────────────────────────┘
```

#### 2.6 `vin-decode-results.tsx` — Resultados del Decode

Después de decodificar, mostrar una **card de preview** con los datos encontrados:

- Imagen genérica del vehículo (por make/model/year si disponible)
- Título: "2024 Toyota Camry SE"
- Lista de specs decodificados con indicador de confianza (✅ confirmado, ⚠️ aproximado)
- Botón "Continuar con estos datos" y "Editar datos"
- Si es duplicado: Warning prominente con link

#### 2.7 `vehicle-info-form.tsx` — Formulario Inteligente

- Si viene de VIN decode: campos pre-llenados con highlight amarillo "Auto-completado"
- Si viene manual: campos vacíos
- Campos dependientes dinámicos:
  - Make → carga Models
  - Make + Model → carga Years disponibles
  - Make + Model + Year → carga Trims
  - Trim seleccionado → auto-fill engine, transmission, drivetrain
- Sección de Features/Equipamiento con checkboxes agrupados:
  - Seguridad: ABS, Airbags, Control estabilidad, Cámara reversa, Sensores
  - Confort: A/C, Asientos cuero, Sunroof, Asientos calefactados
  - Tecnología: Bluetooth, CarPlay/Android Auto, Navegación, Pantalla táctil
  - Rendimiento: Turbo, Modo sport, Paddle shifters
- Campo de condición con selector visual (New, Certified Pre-Owned, Used, Salvage)
- Kilometraje con toggle km/millas
- Colores: exterior e interior con color swatches visuales

#### 2.8 `photo-upload-step.tsx` — Upload Inteligente de Fotos

- Guía visual: Mostrar 8 ángulos recomendados (frente, trasera, lateral izq/der, interior, tablero, motor, llanta)
- Drag & drop zone + botón de cámara (móvil)
- Progreso individual por foto
- Reordernar con drag
- Seleccionar foto principal
- Mínimo: 3 fotos (individual), 5 fotos (dealer)
- Máximo: 10 fotos (individual), 20 fotos (dealer)
- Integración con **Spyne AI** (opcional):
  - Botón "✨ Mejorar fotos" → background removal + enhancement
  - Preview before/after
  - Disponible para dealers con suscripción activa
- Preview en grid con thumbnails
- Validación de tamaño (max 10MB por foto) y formato (jpg, png, webp)

#### 2.9 `pricing-step.tsx` — Precio con Inteligencia

- Si hay datos suficientes (make, model, year, mileage, condition):
  - Llamar a `POST /api/pricing/estimate-by-specs`
  - Mostrar `price-suggestion-card.tsx`:
    ```
    ┌────────────────────────────────────────────┐
    │  💡 Precio Sugerido                         │
    │                                             │
    │  RD$ 1,250,000                              │
    │  ├────────────┼────────────┤                │
    │  Min: 1.1M    │           Max: 1.4M         │
    │               ▲ Tu precio                   │
    │                                             │
    │  📊 Basado en 23 vehículos similares        │
    │  ⚡ Precio competitivo = venta más rápida   │
    │                                             │
    │  [Usar precio sugerido]                     │
    └────────────────────────────────────────────┘
    ```
  - Slider visual del rango de mercado
  - Indicador de posición del precio del usuario vs mercado
- Campo de precio con formato moneda (RD$ o US$)
- Toggle "Negociable"
- Toggle "Acepta trades"
- Descripción con template auto-generado (editable)
  - Botón "📝 Generar descripción automática" que crea un texto atractivo
- Ubicación: Provincia + Ciudad (dropdown con provincias de RD)
- Información de contacto del vendedor

#### 2.10 `review-step.tsx` — Revisión Final

- Preview que se ve exactamente como aparecerá el listado público
- `listing-quality-score.tsx`:
  ```
  ┌────────────────────────────────────────────┐
  │  📊 Calidad de tu Publicación: 85/100      │
  │  ████████████████████░░░░                  │
  │                                             │
  │  ✅ Fotos (8/8 ángulos recomendados)        │
  │  ✅ Descripción completa (+150 caracteres)  │
  │  ✅ Precio en rango de mercado              │
  │  ⚠️ Falta: VIN para más confianza          │
  │  ⚠️ Falta: Historial de accidentes         │
  │                                             │
  │  💡 Publicaciones con score >80 reciben     │
  │     3x más vistas en promedio               │
  └────────────────────────────────────────────┘
  ```
- Checklist de campos obligatorios (título, precio, marca, modelo, año, fotos)
- Botones: "Guardar Borrador" | "Publicar"
- Si es vendedor individual: mostrar costo ($29/listing) y redirigir a checkout
- Si es dealer: publicar directo (incluido en suscripción)

#### 2.11 `csv-import-wizard.tsx` — Import Masivo (Solo Dealers)

- Step 1: Descargar template CSV/Excel con columnas esperadas
- Step 2: Upload del archivo con validación inmediata
- Step 3: Preview en tabla editable:
  - Cada fila = un vehículo
  - Columnas con VIN → botón "Decodificar todos" (batch)
  - Celdas con errores en rojo
  - Edición inline
- Step 4: Confirmación y progreso de creación
  - Barra de progreso global
  - Status por vehículo (✅ creado, ❌ error, ⏳ procesando)
  - Resumen final: "45/50 vehículos creados, 5 errores"

### 3. Frontend — Hooks y Services Nuevos

#### 3.1 `services/vehicles.ts` — Agregar funciones

```typescript
// Agregar a vehicles.ts
decodeVin(vin: string): Promise<SmartVinDecodeResult>
decodeVinBatch(vins: string[]): Promise<SmartVinDecodeResult[]>
checkVinExists(vin: string): Promise<{ exists: boolean; vehicleId?: string; slug?: string }>
estimatePrice(specs: PriceEstimateRequest): Promise<PriceSuggestion>
generateDescription(specs: VehicleSpecs): string // client-side template
importFromCsv(dealerId: string, vehicles: CreateVehicleRequest[]): Promise<BulkImportResult>
```

#### 3.2 `hooks/use-vehicles.ts` — Agregar hooks

```typescript
// Agregar hooks
useDecodeVin(vin: string, options?: { enabled: boolean })
useDecodeVinBatch()  // mutation
useCheckVinExists(vin: string, options?: { enabled: boolean })
useEstimatePrice(specs: PriceEstimateRequest, options?: { enabled: boolean })
useBulkImport()  // mutation
```

### 4. Auto-save y Recuperación de Borradores

- Guardar progreso del wizard en `localStorage` cada vez que el usuario cambia de paso
- Key: `okla_draft_vehicle_{userId}` (individual) o `okla_draft_vehicle_{dealerId}` (dealer)
- Al entrar al wizard, verificar si hay un borrador → preguntar "¿Continuar donde lo dejaste?"
- Guardar también en BD como borrador (`status: Draft`) cuando el usuario explícitamente da "Guardar borrador"
- Listar borradores en `/mis-vehiculos` con opción de continuar edición

### 5. Tracking y Analytics

Integrar con EventTrackingService para medir:

- `publish_method_selected` — Qué método eligió (VIN scan, VIN keyboard, manual, CSV)
- `vin_decode_success` / `vin_decode_failure` — Tasa de éxito del decode
- `vin_scan_success` / `vin_scan_failure` — Tasa de éxito del OCR
- `publish_step_completed` — Cada paso completado (con tiempo)
- `publish_step_abandoned` — En qué paso abandonan
- `publish_completed` — Publicación exitosa (con método y tiempo total)
- `listing_quality_score` — Score promedio de las publicaciones
- `price_suggestion_used` — Si aceptaron el precio sugerido
- `spyne_enhancement_used` — Si usaron mejora de fotos

### 6. Notificaciones

Usar NotificationService para enviar:

- **Email al publicar:** "Tu [Year] [Make] [Model] ya está publicado en OKLA" con link
- **Push notification:** Cuando un vehículo publicado recibe su primera vista
- **Email semanal:** Resumen de vistas, favoritos, y contactos recibidos (dealers)

---

## 🔒 SEGURIDAD — Obligatorio

### Backend

- Aplicar `.NoSqlInjection()` y `.NoXss()` a TODOS los campos string en validadores
- Validar VIN con regex: `^[A-HJ-NPR-Z0-9]{17}$`
- Rate limiting en decode endpoint: Max 10 requests/minuto por IP
- Rate limiting en batch decode: Max 5 requests/hora por dealer
- Sanitizar toda respuesta de NHTSA antes de guardar en BD
- Usar IdempotencyService para prevenir publicaciones duplicadas
- Audit logging: Registrar cada publicación, decode, y import vía AuditService

### Frontend

- Usar `sanitizeVIN()` de `lib/security/sanitize.ts` antes de enviar al backend
- Aplicar `sanitizeText()` a descripción, `sanitizePrice()` a precio, `sanitizeYear()` a año
- Usar `csrfFetch()` para todas las mutaciones (POST, PUT, DELETE)
- Escapar HTML en cualquier dato del VIN decode antes de renderizar (`escapeHtml()`)
- Validar tamaño y tipo de archivos de foto client-side antes de upload

---

## 📱 RESPONSIVE & UX

- **Mobile-first:** El wizard debe funcionar perfectamente en móvil
- **El scanner de VIN (cámara)** es especialmente útil en móvil
- **Touch-friendly:** Botones grandes, áreas de tap generosas
- **Feedback instantáneo:** Skeleton loaders, spinners, progress bars
- **Animaciones suaves:** Transiciones entre pasos del wizard
- **Accesibilidad:** ARIA labels, keyboard navigation, focus management
- **Idioma:** Todo en español (la plataforma es para República Dominicana)
- **Ayuda contextual:** Tooltips e info icons que explican cada campo

---

## 📁 ARCHIVOS A CREAR/MODIFICAR

### Nuevos Archivos — Backend

```
backend/VehiclesSaleService/VehiclesSaleService.Api/Controllers/
  └── (Modificar CatalogController.cs — agregar decode-smart y decode-batch)
  └── (Modificar VehiclesController.cs — agregar vin/exists endpoint)

backend/VehicleIntelligenceService/VehicleIntelligenceService.Api/Controllers/
  └── (Modificar PricingController.cs — agregar estimate-by-specs)
```

### Nuevos Archivos — Frontend

```
frontend/web-next/src/components/vehicles/smart-publish/
  ├── smart-publish-wizard.tsx
  ├── method-selector.tsx
  ├── vin-scanner.tsx
  ├── vin-input.tsx
  ├── vin-decode-results.tsx
  ├── vehicle-info-form.tsx
  ├── photo-upload-step.tsx
  ├── photo-guide.tsx
  ├── pricing-step.tsx
  ├── price-suggestion-card.tsx
  ├── review-step.tsx
  ├── listing-quality-score.tsx
  ├── csv-import-wizard.tsx
  ├── csv-preview-table.tsx
  └── index.ts

frontend/web-next/src/app/(main)/publicar/
  └── page.tsx  (Reescribir — usar SmartPublishWizard)

frontend/web-next/src/app/(main)/dealer/publicar/
  └── page.tsx  (Reescribir — usar SmartPublishWizard con mode="dealer")

frontend/web-next/src/app/(main)/vender/publicar/
  └── page.tsx  (Redirigir a /publicar o eliminar)

frontend/web-next/src/services/vehicles.ts  (Agregar funciones VIN)
frontend/web-next/src/hooks/use-vehicles.ts  (Agregar hooks VIN)
```

### Paquetes npm a Instalar (pnpm add)

```bash
pnpm add tesseract.js        # OCR para escaneo de VIN por cámara
pnpm add papaparse            # Parseo de CSV para import masivo
pnpm add @types/papaparse -D  # Types para papaparse
pnpm add xlsx                  # Parseo de Excel para import masivo
```

> Nota: `react-webcam` ya está instalado (se usa en KYC).

### Gateway — Rutas a Agregar/Verificar

- `/api/catalog/vin/{vin}/decode-smart` → vehiclessaleservice (ya cubierto por `/api/catalog/*`)
- `/api/catalog/vin/decode-batch` → vehiclessaleservice (ya cubierto)
- `/api/vehicles/vin/{vin}/exists` → vehiclessaleservice (ya cubierto por `/api/vehicles/*`)
- `/api/pricing/estimate-by-specs` → vehicleintelligenceservice (ya cubierto por `/api/pricing/*`)

---

## ✅ CRITERIOS DE ACEPTACIÓN

### Funcionales

- [ ] Un vendedor puede publicar un vehículo en menos de 3 minutos usando VIN
- [ ] El VIN decode auto-rellena marca, modelo, año, trim, motor, transmisión, tracción, tipo de carrocería
- [ ] Si el VIN ya está publicado, se muestra una advertencia clara
- [ ] El scanner OCR de VIN funciona con la cámara del celular
- [ ] El flujo manual (sin VIN) sigue disponible y funcional
- [ ] Los dealers pueden importar inventario desde CSV/Excel
- [ ] La sugerencia de precio se muestra cuando hay datos suficientes
- [ ] La descripción auto-generada es coherente y editable
- [ ] El listing quality score calcula correctamente y da tips útiles
- [ ] Los borradores se guardan y recuperan correctamente
- [ ] Las fotos se suben con progreso y se pueden reordenar
- [ ] La integración con Spyne AI funciona para enhancement de fotos

### No Funcionales

- [ ] El VIN decode responde en menos de 2 segundos
- [ ] El formulario funciona correctamente en móvil
- [ ] Todos los inputs están sanitizados (SQL injection, XSS)
- [ ] CSRF protection en todas las mutaciones
- [ ] Audit logging de todas las acciones de publicación
- [ ] Los textos están en español
- [ ] Accesibilidad WCAG 2.1 AA

### Testing

- [ ] Tests unitarios para validación de VIN (formato, checksum)
- [ ] Tests unitarios para matching VIN → catálogo local
- [ ] Tests de integración para el flujo completo de publicación
- [ ] Tests de componentes React para cada step del wizard

---

## 🚀 ORDEN DE IMPLEMENTACIÓN SUGERIDO

### Fase 1 — Backend Core (Prioridad Alta)

1. Endpoint `decode-smart` en CatalogController
2. Endpoint `vin/exists` en VehiclesController
3. Matching VIN → catálogo local (fuzzy match)
4. Auto-generación de descripción

### Fase 2 — Frontend Core (Prioridad Alta)

5. Componente `vin-input.tsx` con validación
6. Componente `vin-decode-results.tsx`
7. Services y hooks de VIN (`decodeVin`, `useDecodeVin`, `checkVinExists`)
8. Componente `method-selector.tsx`
9. Reescribir `/publicar/page.tsx` con `SmartPublishWizard`

### Fase 3 — Pricing & Quality (Prioridad Media)

10. Endpoint `estimate-by-specs` en VehicleIntelligenceService
11. Componente `price-suggestion-card.tsx`
12. Componente `listing-quality-score.tsx`

### Fase 4 — Fotos & Spyne (Prioridad Media)

13. Componente `photo-upload-step.tsx` con guía visual
14. Integración Spyne AI para enhancement

### Fase 5 — Dealer Features (Prioridad Media)

15. VIN Scanner (OCR con Tesseract.js)
16. Batch decode endpoint
17. CSV/Excel import wizard
18. Reescribir `/dealer/publicar/page.tsx`

### Fase 6 — Polish & Analytics (Prioridad Baja)

19. Event tracking integration
20. Notificaciones de publicación
21. Auto-save borradores mejorado
22. Tests completos

---

## ⚠️ NOTAS IMPORTANTES

1. **Package manager:** Usar SIEMPRE `pnpm` (NO npm, NO yarn)
2. **Puerto K8s:** Todos los servicios usan puerto 8080 en Kubernetes
3. **BFF Pattern:** Frontend accede a la API vía rewrites internos, NO directamente al Gateway
4. **Idioma:** Toda la UI debe estar en **español** (RD)
5. **NHTSA API es gratuita** y no requiere API key, pero tiene rate limiting implícito
6. **VehiclesSaleService NO usa MediatR/CQRS** — la lógica va directa en controllers
7. **VehicleIntelligenceService SÍ usa MediatR/CQRS** — usar Commands/Queries
8. **InventoryManagementService SÍ usa MediatR/CQRS** — usar Commands/Queries
9. **Verificar PROBLEMS** (Ctrl+Shift+M) después de cada cambio de código
10. **Sanitización:** Aplicar `sanitizeVIN()`, `sanitizeText()`, `sanitizePrice()` en frontend
11. **SecurityValidators:** Aplicar `.NoSqlInjection()`, `.NoXss()` en backend
12. **react-webcam** ya está instalado — reutilizar para scanner VIN
13. **shadcn/ui** es el sistema de componentes UI — usar sus componentes (Button, Input, Card, etc.)
14. **React Query** (TanStack Query) para toda la gestión de estado del servidor
15. Después de implementar, siempre verificar con `get_errors` que no hay errores de compilación
