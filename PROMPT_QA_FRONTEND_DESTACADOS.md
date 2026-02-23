# 🎯 PROMPT QA FRONTEND — Verificación UI de Vehículos Destacados & Premium

**Proyecto:** OKLA Cardealer Microservices  
**Rama:** `main` / `feat/homepage-featured-vehicles-enrichment`  
**URL Homepage:** https://okla.com.do/  
**Fecha:** 2026-02-23  
**Objetivo:** Verificar y corregir la visualización de vehículos con campañas activas en la homepage con badges ⭐ Destacado y 💎 Premium.

---

## 📋 RESUMEN EJECUTIVO

Este prompt es para **QA Frontend / Ingeniería Frontend** que necesita validar que:

1. ✅ La sección "⭐ Vehículos Destacados" se renderiza correctamente
2. ✅ La sección "💎 Vehículos Premium" se renderiza correctamente
3. ✅ Los badges aparecen según `isFeatured` / `isPremium`
4. ✅ Las imágenes cargan correctamente (o muestran placeholder 🚗)
5. ✅ El precio se formatea adecuadamente (ej: RD$900,000)
6. ✅ Las tarjetas son clickeables y enlazan a `/vehiculos/{slug}`
7. ✅ Se registra tracking de impresiones (POST `/api/advertising/tracking/impression`)
8. ✅ No hay errores de consola (React, Network, etc.)
9. ✅ El diseño es responsive (móvil 375px, tablet 768px, desktop 1280px+)
10. ✅ Estados de carga y error se muestran correctamente

**Tiempo estimado:** 2-3 horas (con screenshots y documentación)

---

## 🗂️ ARQUITECTURA FRONTEND ACTUAL

### Componentes clave:

| Archivo                  | Responsabilidad                                | Estado          |
| ------------------------ | ---------------------------------------------- | --------------- |
| `featured-vehicles.tsx`  | Renderiza secciones FeaturedSpot / PremiumSpot | ✅ Implementado |
| `use-advertising.ts`     | Hooks React Query para datos de rotación       | ✅ Implementado |
| `advertising.ts`         | API client (HTTP)                              | ✅ Implementado |
| `advertising.ts` (types) | Interfaces TypeScript                          | ✅ Implementado |

### Stack frontend:

- **Framework:** Next.js 16 (App Router)
- **Queries:** TanStack Query v5
- **UI:** shadcn/ui (Card, Badge, Skeleton)
- **Styling:** Tailwind CSS v4
- **Package manager:** pnpm
- **TypeScript:** Strict mode

### Datos esperados desde backend:

```typescript
interface RotatedVehicle {
  vehicleId: string;
  campaignId: string;
  position: number;
  qualityScore: number;
  title?: string; // Título del vehículo
  slug?: string; // URL slug (ej: "toyota-corolla-2022")
  imageUrl?: string; // URL imagen (200x125, lazy-loaded)
  price?: number; // Precio en DOP/USD
  currency?: string; // "DOP" o "USD"
  location?: string; // Ciudad/Provincia
  isFeatured?: boolean; // true = mostrar ⭐ Destacado
  isPremium?: boolean; // true = mostrar 💎 Premium
}

interface HomepageRotation {
  section: "FeaturedSpot" | "PremiumSpot";
  items: RotatedVehicle[];
  generatedAt: string;
  nextRotationAt: string;
}
```

---

## 🚀 PASOS DE VERIFICACIÓN

### PASO 0️⃣ — Preparación del entorno

#### 0.1 Clonar y actualizar código

```bash
# Asegúrate de estar en main
cd ~/Developer/Web/Backend/cardealer-microservices
git checkout main
git pull origin main

# Entra al frontend
cd frontend/web-next

# Instala dependencias
pnpm install

# Inicia servidor de desarrollo
pnpm dev
```

**Resultado esperado:** El servidor corre en `http://localhost:3000` sin errores.

#### 0.2 Abrir devtools

- Abre Chrome DevTools: `F12` o `Cmd+Option+I` (macOS)
- Vé a la pestaña **Console** → limpia con `⌘K` (Mac) o `Ctrl+Shift+K` (Windows)
- Vé a la pestaña **Network** → filtra por `Fetch/XHR`

---

### PASO 1️⃣ — Verificación visual en homepage

#### 1.1 Navega a http://localhost:3000

**Checklista de observación:**

- [ ] ¿Aparece una sección con título "Vehículos Destacados" o similar?
- [ ] ¿Aparece una sección con título "Vehículos Premium" o similar?
- [ ] ¿Hay tarjetas de vehículos visibles? (si no, ¿hay esqueleto de carga?)
- [ ] ¿Cada tarjeta tiene: imagen, título, precio, ubicación?
- [ ] ¿Se ve el badge **⭐ Destacado** en algunas tarjetas?
- [ ] ¿Se ve el badge **💎 Premium** en otras tarjetas?
- [ ] ¿Al pasar el mouse sobre una tarjeta, cambia la sombra (hover effect)?
- [ ] ¿La imagen tiene efecto zoom al hover?

**Captura de pantalla:** Toma una screenshot de la homepage visible. Guárdala como `01_homepage_overview.png`.

#### 1.2 Inspecciona la consola de navegador

En **Console**, ejecuta:

```javascript
// Ver si hay errores
console.log("Checking for errors...");
// Deberías ver "No errors" o mensajes informativos normales
```

**Esperas:**

- ❌ No debe haber `Uncaught Error` o `Uncaught TypeError`
- ✅ Pueden haber warnings normales de React o librerías

**Captura de pantalla:** `02_console_clean.png`

#### 1.3 Pestaña Network — Verifica llamadas API

En **Network**, filtra por `Fetch/XHR`:

1. Recarga la página (`Cmd+R` / `Ctrl+R`)
2. Busca peticiones a `/api/advertising/rotation/FeaturedSpot`
3. Busca peticiones a `/api/advertising/rotation/PremiumSpot`
4. Busca peticiones a `/api/advertising/tracking/impression`

**Esperas por cada petición:**

- **Status:** `200 OK` ✅
- **Response Type:** `json`
- **Size:** > 0 bytes
- **Time:** < 2000ms

**Ejemplo de response esperado:**

```json
{
  "success": true,
  "data": {
    "section": "FeaturedSpot",
    "items": [
      {
        "vehicleId": "550e8400-e29b-41d4-a716-446655440000",
        "campaignId": "123e4567-e89b-12d3-a456-426614174000",
        "position": 1,
        "qualityScore": 0.95,
        "title": "Toyota Corolla 2022",
        "slug": "toyota-corolla-2022",
        "imageUrl": "https://bucket.okla.com.do/img/corolla.jpg",
        "price": 900000,
        "currency": "DOP",
        "location": "Santo Domingo",
        "isFeatured": true,
        "isPremium": false
      }
      // ... más vehículos
    ],
    "generatedAt": "2026-02-23T14:30:00Z",
    "nextRotationAt": "2026-02-23T15:30:00Z"
  }
}
```

**Captura de pantalla:** `03_network_rotation_calls.png`

---

### PASO 2️⃣ — Análisis del componente featured-vehicles.tsx

#### 2.1 Abre el archivo y revisa la estructura

**Ubicación:** `frontend/web-next/src/components/advertising/featured-vehicles.tsx`

**Checklist de verificación del código:**

- [ ] ¿El componente importa `useHomepageRotation` y usa `placementType` como prop?
- [ ] ¿Maneja los estados: `isLoading`, `isError`, `data`?
- [ ] ¿Muestra un skeleton de carga mientras `isLoading === true`?
- [ ] ¿Si no hay datos, retorna `null` o muestra mensaje "No hay vehículos"?
- [ ] ¿El componente `FeaturedVehicleCard` recibe `vehicle` y `placementType`?
- [ ] ¿`isFeatured` y `isPremium` se usan para mostrar badges condicionalmente?
- [ ] ¿El precio se formatea con `formatPrice()`?
- [ ] ¿Usa `next/link` para navegación interna?
- [ ] ¿La imagen usa `next/image` con `Image` component?
- [ ] ¿Se llama `recordImpression` en un `useEffect`?
- [ ] ¿Se llama `recordClick` en el `onClick` del link?

**Ejemplo de estructura esperada:**

```tsx
function FeaturedVehicleCard({ vehicle, placementType }) {
  const recordImpression = useRecordImpression();

  useEffect(() => {
    // Registrar impresión una sola vez
    recordImpression.mutate({
      campaignId: vehicle.campaignId,
      vehicleId: vehicle.vehicleId,
      section: placementType,
    });
  }, [vehicle.campaignId, vehicle.vehicleId]);

  return (
    <Link href={`/vehiculos/${vehicle.slug}`}>
      <Card>
        <Image src={vehicle.imageUrl} alt={vehicle.title} />
        {vehicle.isPremium && <Badge>💎 Premium</Badge>}
        {vehicle.isFeatured && <Badge>⭐ Destacado</Badge>}
        <h3>{vehicle.title}</h3>
        <p>{formatPrice(vehicle.price, vehicle.currency)}</p>
        {vehicle.location && <p>📍 {vehicle.location}</p>}
      </Card>
    </Link>
  );
}
```

---

### PASO 3️⃣ — Inspección de hooks y servicios

#### 3.1 Verifica `use-advertising.ts`

**Ubicación:** `frontend/web-next/src/hooks/use-advertising.ts`

Busca la función `useHomepageRotation`:

```typescript
export function useHomepageRotation(section: AdPlacementType) {
  return useQuery({
    queryKey: advertisingKeys.rotationSection(section),
    queryFn: () => getHomepageRotation(section),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}
```

**Checklist:**

- [ ] ¿Existe la función `useHomepageRotation`?
- [ ] ¿Recibe `section` como parámetro?
- [ ] ¿Usa `useQuery` de TanStack Query?
- [ ] ¿Tiene `staleTime` definido?
- [ ] ¿Llama a `getHomepageRotation(section)` desde services?

#### 3.2 Verifica `advertising.ts` (services)

**Ubicación:** `frontend/web-next/src/services/advertising.ts`

Busca:

```typescript
export async function getHomepageRotation(section: AdPlacementType) {
  const response = await apiClient.get<{
    success: boolean;
    data: HomepageRotation;
  }>(`/api/advertising/rotation/${section}`);
  return response.data.data;
}

export async function recordImpression(data: RecordImpressionRequest) {
  await apiClient.post("/api/advertising/tracking/impression", data);
}
```

**Checklist:**

- [ ] ¿La función construye la URL correctamente: `/api/advertising/rotation/${section}`?
- [ ] ¿Retorna `response.data.data` (desenvuelve la respuesta)?
- [ ] ¿`recordImpression` y `recordClick` existen?
- [ ] ¿Usan la URL correcta: `/api/advertising/tracking/impression` y `/api/advertising/tracking/click`?

---

### PASO 4️⃣ — Test visual con datos MOCK (offline)

Cuando necesites probar sin depender del backend en vivo, puedes crear un mock local:

#### 4.1 Crear archivo mock

**Ubicación:** `frontend/web-next/src/hooks/use-advertising.mock.ts`

```typescript
export const MOCK_FEATURED_ROTATION = {
  section: "FeaturedSpot",
  items: [
    {
      vehicleId: "1",
      campaignId: "camp-1",
      position: 1,
      qualityScore: 0.95,
      title: "🚗 Toyota Corolla 2022 - Automático",
      slug: "toyota-corolla-2022",
      imageUrl:
        "https://images.unsplash.com/photo-1552820728-8ac41f1ce891?w=400",
      price: 900000,
      currency: "DOP",
      location: "Santo Domingo",
      isFeatured: true,
      isPremium: false,
    },
    {
      vehicleId: "2",
      campaignId: "camp-2",
      position: 2,
      qualityScore: 0.92,
      title: "Honda Civic 2021 - Deportivo",
      slug: "honda-civic-2021",
      imageUrl:
        "https://images.unsplash.com/photo-1544636331-e26879cd4d9b?w=400",
      price: 750000,
      currency: "DOP",
      location: "Santiago",
      isFeatured: true,
      isPremium: false,
    },
  ],
  generatedAt: new Date().toISOString(),
  nextRotationAt: new Date(Date.now() + 3600000).toISOString(),
};

export const MOCK_PREMIUM_ROTATION = {
  section: "PremiumSpot",
  items: [
    {
      vehicleId: "3",
      campaignId: "camp-3",
      position: 1,
      qualityScore: 0.98,
      title: "💎 Mercedes-Benz C-Class 2023 Luxury",
      slug: "mercedes-c-class-2023",
      imageUrl:
        "https://images.unsplash.com/photo-1568605114967-8130f3a36994?w=400",
      price: 2500000,
      currency: "DOP",
      location: "Punta Cana",
      isFeatured: false,
      isPremium: true,
    },
  ],
  generatedAt: new Date().toISOString(),
  nextRotationAt: new Date(Date.now() + 3600000).toISOString(),
};
```

#### 4.2 Usar el mock en desarrollo

En `use-advertising.ts`, temporalmente:

```typescript
import {
  MOCK_FEATURED_ROTATION,
  MOCK_PREMIUM_ROTATION,
} from "./use-advertising.mock";

export function useHomepageRotation(section: AdPlacementType) {
  // COMENTAR EN PRODUCCIÓN — SOLO PARA DESARROLLO LOCAL
  const mockData =
    section === "FeaturedSpot" ? MOCK_FEATURED_ROTATION : MOCK_PREMIUM_ROTATION;

  return useQuery({
    queryKey: advertisingKeys.rotationSection(section),
    queryFn: async () => {
      // Simular delay de red
      await new Promise((r) => setTimeout(r, 500));
      return mockData;
    },
    staleTime: 5 * 60 * 1000,
  });
}
```

**NO COMMIT ESTOS CAMBIOS.** Usa solo para pruebas locales.

---

### PASO 5️⃣ — Validación de casos límite

Prueba cada caso límite:

#### 5.1 Caso: Vehículo sin imagen

**Esperado:** Mostrar emoji 🚗 como placeholder

**Verifica:**

```tsx
{vehicle.imageUrl ? (
  <Image src={vehicle.imageUrl} ... />
) : (
  <div>🚗</div>
)}
```

#### 5.2 Caso: Vehículo sin precio

**Esperado:** No mostrar nada (o "Consultar precio")

**Verifica:**

```tsx
{
  vehicle.price ? formatPrice(vehicle.price, vehicle.currency) : "Consultar";
}
```

#### 5.3 Caso: Vehículo sin ubicación

**Esperado:** No mostrar línea de ubicación

**Verifica:**

```tsx
{
  vehicle.location && <p>📍 {vehicle.location}</p>;
}
```

#### 5.4 Caso: Sin datos (lista vacía)

**Esperado:** No renderizar nada (o mostrar "No hay vehículos destacados")

**Verifica:**

```tsx
if (vehicles.length === 0) return null;
// O:
if (vehicles.length === 0) return <p>No hay vehículos destacados</p>;
```

#### 5.5 Caso: Error en la carga

**Esperado:** Mostrar mensaje de error amigable

**Verifica en use-advertising.ts:**

```typescript
export function useHomepageRotation(section: AdPlacementType) {
  return useQuery({
    queryKey: advertisingKeys.rotationSection(section),
    queryFn: () => getHomepageRotation(section),
    retry: 2, // Reintentar 2 veces
    staleTime: 5 * 60 * 1000,
  });
}
```

---

### PASO 6️⃣ — Validación responsive

Prueba en diferentes tamaños de pantalla:

#### 6.1 Móvil (375px)

```bash
# En DevTools:
1. Cmd+Shift+M (Mac) o Ctrl+Shift+M (Windows)
2. Selecciona "iPhone SE" o "Pixel 5"
3. Zoom 100%
```

**Checklist:**

- [ ] Las tarjetas caben en 2 columnas?
- [ ] El texto no se desborda?
- [ ] Las imágenes cargan correctamente?
- [ ] Los badges están visibles?
- [ ] El precio es legible?

**Captura:** `04_mobile_375px.png`

#### 6.2 Tablet (768px)

```bash
# DevTools: iPad o iPad Pro
```

**Checklist:**

- [ ] 3 columnas?
- [ ] Espaciado simétrico?

**Captura:** `05_tablet_768px.png`

#### 6.3 Desktop (1280px+)

```bash
# DevTools: Laptop L o sin DevTools
```

**Checklist:**

- [ ] 4 columnas?
- [ ] Máx-width respetado?
- [ ] Hover effects funcionan?

**Captura:** `06_desktop_1280px.png`

---

### PASO 7️⃣ — Validación de tracking

#### 7.1 Verifica que se registra impresión

1. Abre DevTools → **Console**
2. En `featured-vehicles.tsx`, agrega un log temporal:

```typescript
useEffect(() => {
  if (!impressionRecorded.current && vehicle.campaignId) {
    console.log('📊 Registrando impresión:', {
      campaignId: vehicle.campaignId,
      vehicleId: vehicle.vehicleId,
      section: placementType,
    });
    impressionRecorded.current = true;
    recordImpression.mutate({ ... });
  }
}, [vehicle.campaignId, vehicle.vehicleId, placementType, recordImpression]);
```

3. Recarga la página
4. En **Console**, deberías ver:
   ```
   📊 Registrando impresión: { campaignId: "...", vehicleId: "...", section: "FeaturedSpot" }
   ```

**Captura:** `07_tracking_impression_logs.png`

#### 7.2 Verifica llamada POST en Network

1. **Network** → Filtra `Fetch/XHR`
2. Haz clic en una tarjeta
3. Deberías ver:
   - Petición `POST /api/advertising/tracking/impression`
   - Status: `200 OK` o `204 No Content`

**Captura:** `08_tracking_click_network.png`

---

### PASO 8️⃣ — Verificación de accesibilidad (opcional)

Si tienes la extensión **axe DevTools**:

1. Abre DevTools → Pestaña **axe DevTools**
2. Haz clic en **Scan THIS PAGE**
3. Busca issues:
   - **Critical:** Color contrast bajo
   - **Serious:** Missing `alt` text en imágenes
   - **Moderate:** Missing labels

**Checklist:**

- [ ] Todas las imágenes tienen `alt` text? ✅ (Next.js Image component lo requiere)
- [ ] Colores con contraste suficiente (WCAG AA)?
- [ ] Elementos interactivos son focusables (tab)?

---

### PASO 9️⃣ — Validación E2E manual (flujo completo)

Simula el flujo de un usuario:

#### 9.1 Usuario visita homepage

```
1. Navega a http://localhost:3000
2. Observa las secciones de destacados y premium
3. Ve el skeleton cargando
4. Los datos aparecen
5. ✅ Esperado: Secciones visibles, datos completos
```

#### 9.2 Usuario hace clic en una tarjeta

```
1. Haz clic en una tarjeta (ej: "Toyota Corolla 2022")
2. Se registra el click (POST /api/advertising/tracking/click)
3. Navega a /vehiculos/toyota-corolla-2022 (o 404 si no existe la página)
4. ✅ Esperado: Navegación exitosa, no hay errores 404 innecesarios
```

#### 9.3 Usuario recarga la página

```
1. Recarga con F5 / Cmd+R
2. El componente se remonta
3. Impresiones se registran solo UNA VEZ (no duplicadas)
4. ✅ Esperado: No hay duplicados, impresión única
```

---

## 🐛 POSIBLES BUGS A BUSCAR

### Bug #1: Badges superpuestos

**Síntoma:** Ambos badges (⭐ Destacado y 💎 Premium) aparecen en la misma tarjeta.

**Causa probable:** Lógica `AND` en lugar de `XOR`:

```tsx
{
  vehicle.isFeatured && <Badge>⭐</Badge>;
}
{
  vehicle.isPremium && <Badge>💎</Badge>;
}
```

**Corrección esperada:**

```tsx
{
  vehicle.isPremium && <Badge>💎 Premium</Badge>;
}
{
  vehicle.isFeatured && !vehicle.isPremium && <Badge>⭐ Destacado</Badge>;
}
```

✅ **Ya está implementado correctamente en el código actual.**

---

### Bug #2: Imagen no carga

**Síntoma:** Las imágenes muestran "imagen rota" en lugar de placeholder 🚗.

**Causa probable:** `imageUrl` es `undefined` o URL inválida.

**Verificación:**

```javascript
// En Console:
console.log(rotationData.items[0].imageUrl);
```

**Corrección esperada:**

```tsx
{vehicle.imageUrl ? (
  <Image src={vehicle.imageUrl} ... />
) : (
  <div className="text-muted-foreground flex h-full items-center justify-center">
    🚗
  </div>
)}
```

✅ **Ya está implementado correctamente.**

---

### Bug #3: Impresiones duplicadas

**Síntoma:** Se registran múltiples impresiones para la misma tarjeta (en la misma carga).

**Causa probable:** `recordImpression.mutate()` se llama varias veces por deps incorrectos.

**Verificación:**

```typescript
const impressionRecorded = useRef(false);

useEffect(() => {
  if (!impressionRecorded.current && vehicle.campaignId) {
    impressionRecorded.current = true;
    recordImpression.mutate({ ... });
  }
}, [vehicle.campaignId, vehicle.vehicleId, placementType, recordImpression]);
```

✅ **Ya está implementado correctamente con `useRef`.**

---

### Bug #4: Precio sin formato

**Síntoma:** Precio muestra como `900000` en lugar de `RD$900,000`.

**Causa probable:** `formatPrice()` no se llama o está incorrecto.

**Verificación:**

```typescript
function formatPrice(price: number, currency: string = "DOP") {
  if (currency === "DOP") {
    return `RD$${price.toLocaleString("es-DO")}`;
  }
  return `US$${price.toLocaleString("en-US")}`;
}
```

**Test:**

```javascript
// En Console:
formatPrice(900000, "DOP"); // Debería retornar "RD$900,000"
```

✅ **Ya está implementado correctamente.**

---

### Bug #5: No hay skeleton de carga

**Síntoma:** Cuando se carga por primera vez, la sección desaparece (en blanco) en lugar de mostrar skeleton.

**Causa probable:** `isLoading` no se maneja o el skeleton no se renderiza.

**Verificación:**

```tsx
if (isLoading) {
  return (
    <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
      {Array.from({ length: maxItems }).map((_, i) => (
        <Card key={i} className="animate-pulse">
          <div className="bg-muted aspect-[16/10]" />
          <CardContent className="space-y-2 p-4">
            <div className="bg-muted h-4 w-3/4 rounded" />
            <div className="bg-muted h-5 w-1/2 rounded" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
```

✅ **Ya está implementado correctamente.**

---

### Bug #6: Links rotos a vehículos

**Síntoma:** Haces clic en una tarjeta y ves 404 o página vacía.

**Causa probable:** `slug` es `undefined` o la ruta es incorrecta.

**Verificación:**

```typescript
// En Console:
console.log(rotationData.items[0].slug); // Debería ser ej: "toyota-corolla-2022"
```

**Verificar ruta:**

```tsx
<Link href={`/vehiculos/${vehicle.slug || vehicle.vehicleId}`}>
```

⚠️ **A validar:** La página `/vehiculos/[slug]` debe existir en el frontend.

---

## ✅ CHECKLIST FINAL DE VERIFICACIÓN

Marca cada punto **antes** de dar por completada la auditoría:

### Renderizado visual

- [ ] Sección "⭐ Vehículos Destacados" visible
- [ ] Sección "💎 Vehículos Premium" visible
- [ ] Cada tarjeta tiene imagen (o placeholder 🚗)
- [ ] Cada tarjeta tiene título
- [ ] Cada tarjeta tiene precio formateado (ej: RD$900,000)
- [ ] Cada tarjeta tiene ubicación (📍 Santo Domingo)

### Badges

- [ ] Badge ⭐ Destacado aparece SOLO cuando `isFeatured = true`
- [ ] Badge 💎 Premium aparece SOLO cuando `isPremium = true`
- [ ] Ambos badges NO aparecen en la misma tarjeta
- [ ] Badges tienen estilos correctos (colores, fuente)

### Interactividad

- [ ] Tarjeta es un link clickeable
- [ ] Click navega a `/vehiculos/{slug}`
- [ ] Hover muestra efectos visuales (sombra, zoom)
- [ ] No hay errores 404 al navegar

### Tracking

- [ ] Impresión se registra al cargar (POST `/api/advertising/tracking/impression`)
- [ ] Click se registra al hacer clic (POST `/api/advertising/tracking/click`)
- [ ] NO hay impresiones duplicadas
- [ ] Tracking no genera errores de consola

### Estados

- [ ] Skeleton de carga aparece mientras `isLoading`
- [ ] Datos aparecen cuando se cargan
- [ ] Si no hay datos, retorna `null` o muestra mensaje adecuado
- [ ] Si hay error, se muestra mensaje de error (u oculta silenciosamente)

### Responsive

- [ ] Móvil (375px): 2 columnas, sin overflow
- [ ] Tablet (768px): 3 columnas, espaciado simétrico
- [ ] Desktop (1280px+): 4 columnas, máx-width respetado
- [ ] No hay text overflow en ningún tamaño

### Consola y Network

- [ ] No hay `Uncaught Error` o `Uncaught TypeError`
- [ ] Petición GET `/api/advertising/rotation/FeaturedSpot`: `200 OK`
- [ ] Petición GET `/api/advertising/rotation/PremiumSpot`: `200 OK`
- [ ] Petición POST `/api/advertising/tracking/impression`: `200 OK` o `204 No Content`
- [ ] Petición POST `/api/advertising/tracking/click`: `200 OK` o `204 No Content`

### Código

- [ ] `featured-vehicles.tsx` importa todos los hooks necesarios
- [ ] `useHomepageRotation` se usa correctamente
- [ ] `useRecordImpression` y `useRecordClick` se usan correctamente
- [ ] `formatPrice()` formatea correctamente
- [ ] `next/link` se usa para navegación interna
- [ ] `next/image` se usa para imágenes

---

## 📸 CAPTURAS REQUERIDAS

Documenta todo con screenshots:

1. `01_homepage_overview.png` — Homepage completa con ambas secciones
2. `02_console_clean.png` — Console sin errores
3. `03_network_rotation_calls.png` — Llamadas a `/api/advertising/rotation/*`
4. `04_mobile_375px.png` — Vista móvil
5. `05_tablet_768px.png` — Vista tablet
6. `06_desktop_1280px.png` — Vista desktop
7. `07_tracking_impression_logs.png` — Console logs de tracking
8. `08_tracking_click_network.png` — Network tab con POST tracking

**Carpeta:** Guarda todo en `frontend/web-next/QA_SCREENSHOTS/` (crear carpeta)

---

## 💾 COMMITS & PULL REQUEST

Si encuentras bugs y los corriges:

```bash
# 1. Crea rama
git checkout -b fix/homepage-featured-ui

# 2. Haz cambios en:
#    - featured-vehicles.tsx
#    - use-advertising.ts
#    - advertising.ts
#    - etc.

# 3. Commit con conventional commits
git add frontend/web-next/src/components/advertising/featured-vehicles.tsx
git commit -m "fix(homepage): add missing fallback for empty vehicle lists"

git add frontend/web-next/src/hooks/use-advertising.ts
git commit -m "fix(homepage): handle tracking errors silently"

# 4. Push
git push origin fix/homepage-featured-ui

# 5. Abre PR en GitHub
# Descripción: Incluye checklist completado + screenshots
```

**Template de PR:**

```markdown
## 🎯 Título

Fix: Correcciones UI en secciones de vehículos destacados y premium

## 📋 Cambios

- [ ] Implementado validación de campos nulos (imagen, precio, ubicación)
- [ ] Corregida lógica de badges (⭐ XOR 💎)
- [ ] Mejorado skeleton de carga
- [ ] Validado tracking de impresiones y clicks

## 🖼️ Screenshots

### Antes

[antes_screenshot.png]

### Después

[despues_screenshot.png]

## ✅ Checklist

- [x] Verificado en móvil 375px
- [x] Verificado en tablet 768px
- [x] Verificado en desktop 1280px+
- [x] Sin errores en console
- [x] Network calls exitosas
- [x] Tracking registrado correctamente

## 🔗 Relacionado

Cierra #4 (PR original de featured vehicles)
```

---

## ⏱️ TIEMPO ESTIMADO

| Fase                        | Tiempo         |
| --------------------------- | -------------- |
| Preparación (Paso 0-1)      | 15 min         |
| Análisis código (Paso 2-3)  | 20 min         |
| Testing con mock (Paso 4-5) | 30 min         |
| Responsive (Paso 6)         | 25 min         |
| Tracking (Paso 7)           | 20 min         |
| Bugs & fixes (Paso 8-9)     | 40 min         |
| **TOTAL**                   | **~2.5 horas** |

---

## 🚀 PRÓXIMOS PASOS

1. ✅ **Completar todos los pasos** de verificación
2. ✅ **Tomar screenshots** de cada fase
3. ✅ **Documentar bugs** encontrados
4. ✅ **Corregir bugs** (si existen)
5. ✅ **Crear PR** con cambios
6. ✅ **Generar informe** final (ver siguiente sección)

---

## 📊 PLANTILLA DE INFORME FINAL

Crea archivo: `REVISION_FRONTEND_DESTACADOS_20260223.md`

```markdown
# 📋 Informe de Revisión UI — Vehículos Destacados & Premium

**Fecha:** 2026-02-23  
**Auditor:** [Tu nombre]  
**Tiempo invertido:** 2h 30min  
**Estado:** ✅ COMPLETADO / ⚠️ CON OBSERVACIONES

## Resumen ejecutivo

Se verificó la visualización de secciones "⭐ Vehículos Destacados" y "💎 Vehículos Premium" en la homepage de https://okla.com.do/.

**Hallazgos:** [OK / X bugs encontrados]

---

## Checklist de verificación

### Renderizado

- [x] Sección Destacados visible
- [x] Sección Premium visible
- [ ] BUG: Badges superpuestos

### Badges

- [x] ⭐ Destacado correcto
- [x] 💎 Premium correcto

### Interactividad

- [x] Links funcionales
- [x] Hover effects

### Tracking

- [x] Impresiones registradas
- [x] Clicks registrados
- [ ] BUG: Impresiones duplicadas

### Responsive

- [x] Móvil (375px)
- [x] Tablet (768px)
- [x] Desktop (1280px+)

### Consola & Network

- [x] Sin errores críticos
- [x] Todas las peticiones 200 OK

## Bugs encontrados

### [🔴 CRÍTICA] Bug: Badges superpuestos

**Descripción:** Ambos badges aparecen en la misma tarjeta.  
**Reproducción:** Carga homepage con vehículo que tiene isFeatured=true Y isPremium=true.  
**Causa:** Lógica OR en lugar de XOR.  
**Fix:** Cambiar a `{vehicle.isFeatured && !vehicle.isPremium && ...}`.  
**Archivo:** `featured-vehicles.tsx` línea XX.

---

### [🟡 MEDIA] Bug: Imagen no carga

**Descripción:** Para algunos vehículos, la imagen muestra broken link.  
**Reproducción:** Recargar página varias veces.  
**Causa:** `imageUrl` no siempre presente en respuesta del backend.  
**Fix:** Ya manejado con placeholder 🚗 (OK).  
**Archivo:** `featured-vehicles.tsx` línea XX.

---

## Commits realizados

- `abc1234` — fix(homepage): correct badge logic (XOR instead of OR)
- `def5678` — fix(homepage): add retry logic for failed requests
- `ghi9012` — test(homepage): add mock data for offline testing

## Screenshots

1. [01_homepage_overview.png](./QA_SCREENSHOTS/01_homepage_overview.png)
2. [02_console_clean.png](./QA_SCREENSHOTS/02_console_clean.png)
3. ... (8 total)

## PR asociado

[#7 - Fix: Homepage featured vehicles UI corrections](https://github.com/gregorymorenoiem/cardealer-microservices/pull/7)

## Conclusión

✅ **APROBADO PARA PRODUCCIÓN** — Todos los bugs críticos fueron solucionados. El componente es responsive, accesible y rastreable.

---

**Firma digital:** GitHub user @[tu-usuario]  
**Fecha completación:** 2026-02-23 16:30 UTC
```

---

## 🎓 REFERENCIAS Y RECURSOS

| Recurso                        | Link                                             |
| ------------------------------ | ------------------------------------------------ |
| Next.js Image Optimization     | https://nextjs.org/docs/api-reference/next/image |
| TanStack Query (React Query)   | https://tanstack.com/query/latest                |
| shadcn/ui Components           | https://ui.shadcn.com/                           |
| Tailwind CSS Responsive Design | https://tailwindcss.com/docs/responsive-design   |
| Conventional Commits           | https://www.conventionalcommits.org/             |
| axe DevTools Accesibilidad     | https://www.deque.com/axe/devtools/              |

---

## ❓ PREGUNTAS FRECUENTES

### P: ¿Necesito tocar el backend?

**R:** No. Este prompt es **solo frontend**. Si hay un bug de datos del backend, repórtalo como issue, pero la solución la hace el equipo backend.

### P: ¿Qué pasa si el endpoint retorna 500?

**R:** Documentá el error con screenshot de Network. Repórtalo en GitHub Issues con etiqueta `backend`.

### P: ¿Puedo usar datos reales vs mock?

**R:** Sí. Usa ambos:

- **Mock** para pruebas de casos límite
- **Real** para validación final en staging/producción

### P: ¿Los badges pueden tener ambos emojis?

**R:** No. Debe ser SOLO uno:

- ⭐ Destacado (si `isFeatured=true` Y `isPremium=false`)
- 💎 Premium (si `isPremium=true`)

### P: ¿El tracking debe ser síncrono?

**R:** No. Usa `useMutation` de TanStack Query para asincronía. Los errores de tracking no deben romper la UI.

---

## 🎯 ENTREGA FINAL

Cuando termines, entrega:

1. ✅ Informe markdown completo (REVISION_FRONTEND_DESTACADOS_YYYYMMDD.md)
2. ✅ Carpeta con 8+ screenshots (QA_SCREENSHOTS/)
3. ✅ PR abierto hacia `main` con commits descriptivos
4. ✅ Checklist de verificación 100% completado
5. ✅ Tiempo total invertido documentado

---

**¡Gracias por auditar el frontend! 🚀**

Cualquier pregunta → abre GitHub Issue o comenta en el PR.

---

_Actualizado: 2026-02-23 | Versión: 2.0 | Adaptado a contexto real OKLA_
