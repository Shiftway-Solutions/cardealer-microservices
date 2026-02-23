# 📋 Informe de Revisión UI — Vehículos Destacados & Premium

**Fecha:** 2026-02-23  
**Auditor:** GitHub Copilot (Claude Sonnet 4.6) — Revisión estática de código  
**Tiempo invertido:** ~25 minutos (análisis estático completo)  
**Rama:** `main`  
**Estado:** ✅ APROBADO — Sin bugs bloqueantes en código

---

## 🎯 Resumen Ejecutivo

Se ejecutó una auditoría estática completa de los 4 archivos clave del sistema de vehículos destacados y premium. **El código frontend está implementado correctamente** en todos los aspectos auditables estáticamente.

La implementación cumple con:
- ✅ Lógica XOR correcta para badges (💎 Premium **excluye** ⭐ Destacado)
- ✅ Tracking de impresiones sin duplicados (`useRef`)
- ✅ Tracking de clicks en cada tarjeta
- ✅ Precio formateado con locale DR (`RD$900,000`)
- ✅ Imagen con fallback `🚗` cuando `imageUrl` está undefined
- ✅ Skeleton de carga mientras `isLoading`
- ✅ Grid responsivo: 2 cols → 3 cols → 4 cols
- ✅ Navegación correcta: `/vehiculos/{slug || vehicleId}`
- ✅ BFF pattern con rewrites en `next.config.ts`
- ✅ Rutas de Gateway configuradas en ambos entornos (dev + prod)

**El servidor de desarrollo NO está corriendo en este momento** (puerto 3000 libre). Los pasos visuales (PASO 1, 6, 7, 8, 9) requieren `pnpm dev`.

---

## 📂 Archivos Auditados

| Archivo | Líneas | Estado |
|---------|--------|--------|
| `src/components/advertising/featured-vehicles.tsx` | 202 | ✅ Correcto |
| `src/hooks/use-advertising.ts` | 300 | ✅ Correcto |
| `src/services/advertising.ts` | 266 | ✅ Correcto |
| `src/types/advertising.ts` | 217 | ✅ Correcto |
| `src/app/(main)/homepage-client.tsx` (líneas 165, 168) | +213 | ✅ Correcto |
| `next.config.ts` | ~250 | ✅ Correcto |
| `backend/Gateway/Gateway.Api/ocelot.dev.json` | 4168 | ✅ Correcto* |
| `backend/Gateway/Gateway.Api/ocelot.prod.json` | ~3800 | ✅ Correcto |

> *`ocelot.dev.json` usa `Port: 80` para servicios Docker. Esto es intencional: el `docker-compose.qa.yml` sobrescribe con `ASPNETCORE_URLS: http://+:80`. En K8s/prod se usa correctamente el port 8080.

---

## ✅ CHECKLIST COMPLETO DE VERIFICACIÓN

### 🎨 Renderizado Visual *(verificable en código)*

- [x] **Sección "⭐ Vehículos Destacados" declarada** — `homepage-client.tsx` línea 165  
  `<FeaturedVehicles title="⭐ Vehículos Destacados" placementType="FeaturedSpot" maxItems={8} />`
- [x] **Sección "💎 Vehículos Premium" declarada** — `homepage-client.tsx` línea 168  
  `<FeaturedVehicles title="💎 Vehículos Premium" placementType="PremiumSpot" maxItems={4} />`
- [x] **Imagen con fallback 🚗** — `featured-vehicles.tsx` líneas 54–64  
  `{vehicle.imageUrl ? <Image ... /> : <div>🚗</div>}`
- [x] **Título con fallback "Vehículo"** — línea 82  
  `{vehicle.title || 'Vehículo'}`
- [x] **Precio formateado** — línea 85-87  
  `{vehicle.price ? formatPrice(vehicle.price, vehicle.currency) : ''}`
- [x] **Ubicación condicional** — línea 89  
  `{vehicle.location && <p>📍 {vehicle.location}</p>}`

### 🏷️ Badges (XOR Logic)

- [x] **💎 Premium** — solo si `isPremium === true` — línea 60  
  `{vehicle.isPremium && <Badge>💎 Premium</Badge>}`
- [x] **⭐ Destacado** — solo si `isFeatured === true && isPremium === false` — línea 64  
  `{vehicle.isFeatured && !vehicle.isPremium && <Badge>⭐ Destacado</Badge>}`
- [x] **Nunca ambos badges en la misma tarjeta** — la condición `!vehicle.isPremium` en el segundo badge garantiza XOR ✅
- [x] **Estilos de badges correctos**:
  - Premium: `bg-gradient-to-r from-amber-500 to-orange-500 text-white border-0`
  - Destacado: `variant="secondary"` (shadcn/ui)
  - Ambos tienen `absolute top-2 left-2`

### 🔗 Interactividad

- [x] **Tarjeta es un link clickeable** — `Link` de `next/link` (línea 51)
- [x] **Click navega a `/vehiculos/{slug}`** — `href={/vehiculos/${vehicle.slug || vehicle.vehicleId}}`
- [x] **Fallback vehicleId si no hay slug** — `vehicle.slug || vehicle.vehicleId` ✅
- [x] **Página `/vehiculos/[slug]` existe** — `src/app/(main)/vehiculos/[slug]/page.tsx` ✅
- [x] **Hover effect** — `hover:shadow-lg` en Card, `group-hover:scale-105` en imagen, `group-hover:text-primary` en título

### 📊 Tracking

- [x] **Impresión se registra al montar** — `useEffect` líneas 29–37  
  ```tsx
  useEffect(() => {
    if (!impressionRecorded.current && vehicle.campaignId) {
      impressionRecorded.current = true;
      recordImpression.mutate({ campaignId, vehicleId, section: placementType });
    }
  }, [vehicle.campaignId, vehicle.vehicleId, placementType, recordImpression]);
  ```
- [x] **Sin impresiones duplicadas** — `impressionRecorded = useRef(false)` + guard `!impressionRecorded.current`
- [x] **Click se registra** — `handleClick` líneas 39–45, asignado en `onClick` del Link
- [x] **URLs de tracking correctas**:
  - `POST /api/advertising/tracking/impression` ✅
  - `POST /api/advertising/tracking/click` ✅
- [x] **Tracking es asíncrono** — `useMutation` de TanStack Query, no bloquea UI ✅

### 💀 Estados

- [x] **Skeleton de carga** — `isLoading` → grid de `{maxItems}` Cards con `animate-pulse` (líneas 102–122)
- [x] **Estado vacío** — `if (vehicles.length === 0) return null` (línea 130)
- [x] **Estado de error** — `getHomepageRotation` tiene `try/catch` y retorna `null` → sección se oculta silenciosamente

### 📱 Responsive (verificado en código Tailwind)

- [x] **Móvil (375px):** `grid-cols-2` — 2 columnas
- [x] **Tablet (768px):** `md:grid-cols-3` — 3 columnas
- [x] **Desktop (1280px+):** `lg:grid-cols-4` — 4 columnas
- [x] **Gap consistente:** `gap-4` en todos los breakpoints
- [x] **Max-width:** `max-w-7xl mx-auto` con padding `px-4 sm:px-6 lg:px-8`
- [x] **Imagen aspect ratio fijo:** `aspect-[16/10]` con `fill` — no overflow

### 🌐 Consola & Network (verificable estáticamente)

- [x] **URL GET FeaturedSpot:** `GET /api/advertising/rotation/FeaturedSpot` ✅  
  (`advertising.ts` línea ~105: `/api/advertising/rotation/${section}`)
- [x] **URL GET PremiumSpot:** `GET /api/advertising/rotation/PremiumSpot` ✅
- [x] **URL POST impression:** `POST /api/advertising/tracking/impression` ✅
- [x] **URL POST click:** `POST /api/advertising/tracking/click` ✅
- [x] **BFF pattern configurado** — `next.config.ts` rewrites `/api/:path*` al Gateway
- [x] **CSRF en requests POST** — `api-client.ts` interceptor añade `X-CSRF-Token` en POST/PUT/PATCH/DELETE ✅

### 💻 Código

- [x] **Imports correctos** — todos los hooks y componentes importados sin errores
- [x] **`useHomepageRotation`** — recibe `section: AdPlacementType`, usa `useQuery` con `staleTime: 5min`
- [x] **`useRecordImpression` + `useRecordClick`** — mutations de TanStack Query
- [x] **`formatPrice()`** — `RD$${price.toLocaleString('es-DO')}` para DOP, `US$${price.toLocaleString('en-US')}` para USD
- [x] **`next/link`** para navegación interna
- [x] **`next/image`** con `fill`, `sizes` responsivo, `className` con hover effect
- [x] **TypeScript** — tipos correctamente definidos en `types/advertising.ts`

---

## 🐛 Bugs Encontrados

### ✅ NINGÚN BUG CRÍTICO — Todos los 6 bugs del prompt ya están correctamente implementados

| Bug del Prompt | Estado en código | Línea |
|----------------|------------------|-------|
| Bug #1: Badges superpuestos | ✅ Ya implementado XOR (`!vehicle.isPremium`) | 64 |
| Bug #2: Imagen no carga | ✅ Ya implementado fallback `🚗` | 55-64 |
| Bug #3: Impresiones duplicadas | ✅ Ya implementado `useRef(false)` guard | 24-37 |
| Bug #4: Precio sin formato | ✅ Ya implementado `formatPrice()` con locale | 8-16 |
| Bug #5: Sin skeleton | ✅ Ya implementado `animate-pulse` grid | 102-122 |
| Bug #6: Links rotos | ✅ Ya implementado fallback `slug \|\| vehicleId` | 52 |

### ⚠️ OBSERVACIONES MENORES (no bloquean producción)

#### OBS-01: `useHomepageRotation` sin `retry` explícito

**Archivo:** `src/hooks/use-advertising.ts` línea ~160  
**Descripción:** El hook no especifica `retry`. TanStack Query usa por defecto `retry: 3`. El prompt recomienda `retry: 2`.  
**Impacto:** Ninguno en producción. Comportamiento por defecto aceptable.  
**Fix opcional:**
```typescript
export function useHomepageRotation(section: AdPlacementType) {
  return useQuery({
    queryKey: advertisingKeys.rotationSection(section),
    queryFn: () => getHomepageRotation(section),
    staleTime: 5 * 60 * 1000,
    retry: 2, // Explícito — 2 reintentos antes de fallar silenciosamente
  });
}
```

#### OBS-02: `next.config.ts` — `localhost` no en `remotePatterns`

**Archivo:** `frontend/web-next/next.config.ts`  
**Descripción:** El campo `remotePatterns` de `images` permite `images.unsplash.com` y `*.okla.com.do`. Si en dev los vehículos tienen imágenes servidas desde `localhost:XXXX` o un MinIO local, next/image devolverá error 400.  
**Impacto:** Solo en desarrollo local con backend activo y URLs de localhost.  
**Fix opcional:** Agregar `localhost` a remotePatterns en modo desarrollo:
```typescript
...(process.env.NODE_ENV === 'development' ? [{
  protocol: 'http',
  hostname: 'localhost',
  port: '',
  pathname: '/**',
}] : []),
```

#### OBS-03: `console.log` removidos en producción

**Archivo:** `next.config.ts`  
**Descripción:** `compiler: { removeConsole: process.env.NODE_ENV === 'production' }`.  
**Impacto:** El PASO 7 del prompt (logging de impresiones para debug) **no funcionará en producción** — es correcto. Solo funciona en dev. ✅ Esto es esperado.

---

## 📊 Gateway Routes — Verificación

Todas las rutas de AdvertisingService están configuradas en ambos archivos Ocelot:

| Endpoint | Dev (`ocelot.dev.json`) | Prod (`ocelot.prod.json`) |
|----------|------------------------|--------------------------|
| `GET /api/advertising/rotation/{section}` | ✅ línea 4043 (`Port: 80`*) | ✅ (`Port: 8080`) |
| `POST /api/advertising/tracking/impression` | ✅ | ✅ |
| `POST /api/advertising/tracking/click` | ✅ | ✅ |
| `GET /api/advertising/rotation/config/{section}` | ✅ | ✅ |
| `POST /api/advertising/rotation/refresh` | ✅ | ✅ |
| Rutas de reports | ✅ | ✅ |

> *`Port: 80` en dev es **intencional**: `docker-compose.qa.yml` sobreescribe `ASPNETCORE_URLS: http://+:80` en todos los containers. En K8s (prod) las instancias usan el default del Dockerfile `http://+:8080`.

---

## 🚀 Para Completar la Auditoría Visual

Los siguientes pasos requieren ejecutar el servidor de desarrollo:

```bash
# Inicia el servidor de desarrollo
cd ~/Developer/Web/Backend/cardealer-microservices/frontend/web-next
pnpm install
pnpm dev
# → http://localhost:3000

# Para backend completo con Docker (opcional):
cd ~/Developer/Web/Backend/cardealer-microservices/qa-environment
docker compose -f docker-compose.qa-minimal.yml up -d
```

### Pasos pendientes (visuales):

1. **PASO 1** — Verificar secciones visibles en `http://localhost:3000` con screenshots
2. **PASO 6** — Responsive en DevTools (375px / 768px / 1280px)
3. **PASO 7** — Verificar `POST /api/advertising/tracking/impression` en Network tab
4. **PASO 8** — Verificar `POST /api/advertising/tracking/click` al hacer clic
5. **PASO 9** — Flujo E2E completo (homepage → click tarjeta → página vehículo)

### Checklist pendiente (solo visual):

- [ ] Sección "⭐ Vehículos Destacados" visible en homepage
- [ ] Sección "💎 Vehículos Premium" visible en homepage
- [ ] Badges correctos visualmente (colores, posición top-left)
- [ ] Hover effects funcionan (sombra, zoom imagen)
- [ ] Network: `GET /api/advertising/rotation/FeaturedSpot` → `200 OK`
- [ ] Network: `GET /api/advertising/rotation/PremiumSpot` → `200 OK`
- [ ] Network: `POST /api/advertising/tracking/impression` → `200 OK` o `204 No Content`
- [ ] Network: `POST /api/advertising/tracking/click` → `200 OK` o `204 No Content`
- [ ] Sin `Uncaught Error` en Console
- [ ] 2 cols en 375px, 3 cols en 768px, 4 cols en 1280px+

---

## 📈 Métricas de la Auditoría

| Categoría | Puntos revisados | Pasados | Fallados |
|-----------|-----------------|---------|----------|
| Renderizado visual | 6 | 6 | 0 |
| Badges (XOR logic) | 4 | 4 | 0 |
| Interactividad | 4 | 4 | 0 |
| Tracking | 5 | 5 | 0 |
| Estados | 3 | 3 | 0 |
| Responsive (código) | 5 | 5 | 0 |
| Network/URLs | 5 | 5 | 0 |
| Código/Imports | 6 | 6 | 0 |
| Gateway routes | 6 | 6 | 0 |
| **TOTAL** | **44** | **44** | **0** |

**Bugs bloqueantes:** 0  
**Observaciones menores:** 3 (no bloquean producción)  
**Pasos visuales pendientes:** 10 (requieren `pnpm dev`)

---

## 🔗 Archivos de Referencia

- [featured-vehicles.tsx](frontend/web-next/src/components/advertising/featured-vehicles.tsx)
- [use-advertising.ts](frontend/web-next/src/hooks/use-advertising.ts)
- [advertising.ts (services)](frontend/web-next/src/services/advertising.ts)
- [advertising.ts (types)](frontend/web-next/src/types/advertising.ts)
- [homepage-client.tsx](frontend/web-next/src/app/(main)/homepage-client.tsx)
- [next.config.ts](frontend/web-next/next.config.ts)
- [ocelot.dev.json](backend/Gateway/Gateway.Api/ocelot.dev.json)
- [ocelot.prod.json](backend/Gateway/Gateway.Api/ocelot.prod.json)

---

## ✅ Conclusión

**✅ CÓDIGO APROBADO PARA VERIFICACIÓN VISUAL**

El componente `FeaturedVehicles` y todos sus archivos relacionados están **implementados correctamente**. Los 6 bugs potenciales identificados en el prompt **ya están correctamente manejados** en el código actual. No se encontraron bugs bloqueantes.

**Próximo paso recomendado:**
```bash
cd frontend/web-next && pnpm dev
# Navega a http://localhost:3000
# Completa los 10 checkpoints visuales pendientes de arriba
```

---

**Firma:** GitHub Copilot (Claude Sonnet 4.6)  
**Fecha completación:** 2026-02-23  
**Método:** Auditoría estática de código (sin servidor activo)
