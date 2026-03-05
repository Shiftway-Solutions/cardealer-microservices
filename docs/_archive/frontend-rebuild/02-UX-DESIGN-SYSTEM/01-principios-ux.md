# 🎨 Principios de UX - OKLA

> **Objetivo:** Definir los principios de experiencia de usuario que guiarán todo el desarrollo
> **Tema visual:** CarGurus USA - Verde esmeralda (#00A870) + UI limpia y profesional
> **Prioridad:** 🔴 CRÍTICO - Leer antes de implementar cualquier componente

---

## 🎯 OBJETIVO DE UX

> **"Que encontrar y comprar un vehículo en República Dominicana sea tan fácil y transparente como en CarGurus"**

### Diferenciadores clave (estilo CarGurus):

- ✅ **Precio transparente** - Sistema de Deal Rating que califica cada precio
- ✅ **UI verde profesional** - Color primario que transmite confianza y ahorro
- ✅ **Sombras sutiles** - Diseño limpio sin ruido visual
- ✅ **Focus en el vehículo** - Cards claras con información esencial

---

## 🎨 PALETA DE COLORES PRINCIPAL

| Rol            | Color          | Hex       | Uso                           |
| -------------- | -------------- | --------- | ----------------------------- |
| **Primary**    | Verde CarGurus | `#00A870` | CTAs, links, badges positivos |
| **Secondary**  | Navy           | `#1A1A2E` | Headlines, texto importante   |
| **Text**       | Gray 700       | `#616161` | Texto normal                  |
| **Muted**      | Gray 500       | `#9E9E9E` | Texto secundario              |
| **Background** | Gray 50        | `#FAFAFA` | Fondo de página               |
| **Card**       | White          | `#FFFFFF` | Fondo de cards                |

> 📖 Ver guía completa: [00-TEMA-CARGURUS-AUDITORIA.md](./00-TEMA-CARGURUS-AUDITORIA.md)

---

## 📋 LOS 10 PRINCIPIOS DE UX OKLA

### 1. 🚀 VELOCIDAD PERCIBIDA

**Regla:** El usuario nunca debe esperar más de 300ms sin feedback visual.

```typescript
// ✅ CORRECTO: Feedback inmediato
const handleClick = async () => {
  setIsLoading(true); // Inmediato
  try {
    await api.submit(data);
    toast.success("¡Guardado!");
  } finally {
    setIsLoading(false);
  }
};

// ❌ INCORRECTO: Sin feedback
const handleClick = async () => {
  await api.submit(data); // Usuario no sabe qué pasa
};
```

**Implementar:**

- Skeleton loaders para contenido
- Spinners para acciones
- Progress bars para procesos largos
- Optimistic updates donde sea seguro

```tsx
// Skeleton mientras carga
{
  isLoading ? (
    <VehicleCardSkeleton count={6} />
  ) : (
    <VehicleGrid vehicles={vehicles} />
  );
}
```

---

### 2. 🎯 CLARIDAD SOBRE CREATIVIDAD

**Regla:** Cada elemento debe tener un propósito obvio. Si necesitas explicarlo, rediseñalo.

```tsx
// ✅ CORRECTO: Obvio qué hace - Botones verdes para CTA
<Button>Contactar Vendedor</Button>
<Button variant="outline">Guardar en Favoritos</Button>

// ❌ INCORRECTO: Ambiguo
<Button>Enviar</Button>
<Button>+</Button>
```

**Labels de botones:**
| Acción | Label Correcto | Label Incorrecto |
|--------|----------------|------------------|
| Buscar | "Buscar Vehículos" | "Buscar" |
| Guardar | "Guardar Cambios" | "OK" |
| Eliminar | "Eliminar Vehículo" | "Eliminar" |
| Contactar | "Contactar Vendedor" | "Contactar" |
| Ver detalles | "Ver Vehículo" | "Ver más" |

---

### 3. 📱 MOBILE-FIRST REAL

**Regla:** Diseñar primero para móvil, luego expandir. 70% del tráfico será móvil.

```tsx
// ✅ CORRECTO: Mobile-first
<div className="
  grid
  grid-cols-1           /* Mobile: 1 columna */
  sm:grid-cols-2        /* Tablet: 2 columnas */
  lg:grid-cols-3        /* Desktop: 3 columnas */
  xl:grid-cols-4        /* Wide: 4 columnas */
  gap-4
">
```

**Breakpoints:**

```typescript
const breakpoints = {
  sm: "640px", // Móvil grande
  md: "768px", // Tablet
  lg: "1024px", // Desktop
  xl: "1280px", // Desktop grande
  "2xl": "1536px", // Ultra wide
};
```

**Touch targets:**

- Mínimo 44x44px para elementos clickeables
- Espaciado mínimo de 8px entre targets

---

### 4. 🔍 BÚSQUEDA ES REY

**Regla:** El usuario debe poder buscar desde cualquier página en menos de 2 taps/clicks.

```tsx
// Navbar siempre visible con búsqueda
<Navbar>
  <SearchBar
    placeholder="Buscar por marca, modelo o precio..."
    suggestions={recentSearches}
    onSearch={handleSearch}
  />
</Navbar>
```

**Filtros inteligentes:**

- Mostrar filtros más usados primero
- Recordar filtros del usuario
- Mostrar contador de resultados en tiempo real

```tsx
// Contador dinámico
<div className="text-sm text-muted-foreground">
  {isFiltering ? (
    <Skeleton className="w-20 h-4" />
  ) : (
    `${count.toLocaleString()} vehículos encontrados`
  )}
</div>
```

---

### 5. 💰 PRECIO PROMINENTE

**Regla:** El precio es lo primero que busca el usuario. Siempre visible, siempre formateado.

```tsx
// Componente de precio
export function Price({ amount, currency = "DOP" }: PriceProps) {
  const formatted = new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);

  return <span className="text-2xl font-bold text-gray-900">{formatted}</span>;
}

// Uso
<Price amount={1850000} />; // Muestra: RD$ 1,850,000
```

**Deal Badge siempre visible:**

```tsx
<div className="flex items-center gap-2">
  <Price amount={price} />
  <DealBadge rating={dealRating} />
</div>
```

---

### 6. 📸 IMÁGENES DE CALIDAD

**Regla:** Las imágenes venden. Optimizar para carga rápida pero alta calidad.

```tsx
// Usar Next.js Image con blur placeholder
import Image from "next/image";

<Image
  src={vehicle.images[0].url}
  alt={vehicle.title}
  fill
  className="object-cover"
  sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
  placeholder="blur"
  blurDataURL={vehicle.images[0].blurHash}
  priority={index < 6} // Primeras 6 imágenes con prioridad
/>;
```

**Galería interactiva:**

- Swipe en móvil
- Thumbnails en desktop
- Zoom al hacer click
- Contador de imágenes

---

### 7. ⚡ ACCIONES CONTEXTUALES

**Regla:** Las acciones deben aparecer donde el usuario las necesita, no en un menú escondido.

```tsx
// ✅ CORRECTO: Acciones visibles en contexto
<VehicleCard>
  <div className="absolute top-2 right-2 flex gap-2">
    <FavoriteButton vehicleId={id} />
    <ShareButton vehicleId={id} />
  </div>
  {/* ... contenido ... */}
  <div className="flex gap-2 mt-4">
    <Button className="flex-1">Ver Detalles</Button>
    <Button variant="outline" className="flex-1">Contactar</Button>
  </div>
</VehicleCard>

// ❌ INCORRECTO: Acciones en menú
<VehicleCard>
  <DropdownMenu>
    <DropdownMenuTrigger>...</DropdownMenuTrigger>
    <DropdownMenuContent>
      <DropdownMenuItem>Ver</DropdownMenuItem>
      <DropdownMenuItem>Favorito</DropdownMenuItem>
      <DropdownMenuItem>Compartir</DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
</VehicleCard>
```

---

### 8. 🔔 FEEDBACK CONSTANTE

**Regla:** Cada acción del usuario debe tener una respuesta visible.

```typescript
// Sistema de toasts
import { toast } from "sonner";

// Éxito
toast.success("Vehículo guardado en favoritos");

// Error
toast.error("No se pudo enviar el mensaje. Intenta de nuevo.");

// Loading
toast.promise(submitForm(), {
  loading: "Enviando mensaje...",
  success: "¡Mensaje enviado! El vendedor te responderá pronto.",
  error: "Error al enviar. Verifica tu conexión.",
});

// Con acción
toast("Vehículo eliminado", {
  action: {
    label: "Deshacer",
    onClick: () => restoreVehicle(id),
  },
});
```

**Estados visuales:**

```tsx
// Botón con estados
<Button disabled={isSubmitting} className="relative">
  {isSubmitting ? (
    <>
      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
      Enviando...
    </>
  ) : (
    "Enviar Mensaje"
  )}
</Button>
```

---

### 9. 🛡️ CONFIANZA Y SEGURIDAD

**Regla:** Transmitir seguridad en cada interacción, especialmente en pagos y datos personales.

```tsx
// Badges de confianza
<div className="flex items-center gap-4 text-sm text-muted-foreground">
  <span className="flex items-center gap-1">
    <Shield className="h-4 w-4 text-green-600" />
    Pago Seguro
  </span>
  <span className="flex items-center gap-1">
    <Lock className="h-4 w-4 text-green-600" />
    Datos Protegidos
  </span>
  <span className="flex items-center gap-1">
    <CheckCircle className="h-4 w-4 text-blue-600" />
    Dealer Verificado
  </span>
</div>
```

**Dealer verificado:**

```tsx
<div className="flex items-center gap-2">
  <Avatar src={dealer.logo} alt={dealer.name} />
  <div>
    <div className="flex items-center gap-1">
      <span className="font-semibold">{dealer.name}</span>
      {dealer.isVerified && <BadgeCheck className="h-5 w-5 text-blue-500" />}
    </div>
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
      <span>{dealer.rating}</span>
      <span>·</span>
      <span>{dealer.reviewCount} reseñas</span>
    </div>
  </div>
</div>
```

---

### 10. 🎨 CONSISTENCIA VISUAL

**Regla:** Mismos patrones para mismas acciones en toda la app.

**Colores de acción:**

```typescript
const actionColors = {
  primary: "bg-blue-600 hover:bg-blue-700", // Acción principal
  secondary: "bg-gray-100 hover:bg-gray-200", // Acción secundaria
  success: "bg-green-600 hover:bg-green-700", // Confirmación
  danger: "bg-red-600 hover:bg-red-700", // Eliminar/Peligro
  warning: "bg-yellow-500 hover:bg-yellow-600", // Atención
};
```

**Iconografía consistente:**

```typescript
// Usar siempre los mismos iconos para las mismas acciones
import {
  Heart, // Favoritos
  Share2, // Compartir
  Search, // Buscar
  Filter, // Filtrar
  MapPin, // Ubicación
  Phone, // Llamar
  Mail, // Email
  MessageCircle, // Chat/Mensaje
  Calendar, // Agendar
  Download, // Descargar
  Upload, // Subir
  Trash2, // Eliminar
  Edit, // Editar
  Eye, // Ver
  EyeOff, // Ocultar
  Check, // Éxito/Seleccionar
  X, // Cerrar/Cancelar
  ChevronRight, // Navegar
  ArrowLeft, // Volver
} from "lucide-react";
```

---

## 📊 MÉTRICAS DE UX

### Qué medir:

| Métrica              | Target       | Cómo medir                                     |
| -------------------- | ------------ | ---------------------------------------------- |
| Time to First Search | < 5 segundos | Analytics: tiempo desde landing hasta búsqueda |
| Bounce Rate          | < 40%        | Google Analytics                               |
| Task Completion Rate | > 80%        | Flujos E2E exitosos / intentos                 |
| Error Rate           | < 1%         | Sentry errors / sessions                       |
| Mobile Usability     | 100/100      | Google Mobile-Friendly Test                    |
| Accessibility Score  | > 95         | Lighthouse                                     |

### Cómo implementar tracking:

```typescript
// hooks/useAnalytics.ts
export function useAnalytics() {
  const trackEvent = useCallback((event: string, properties?: object) => {
    // OKLA Analytics SDK
    window.oklaAnalytics?.track(event, properties);

    // Google Analytics
    gtag("event", event, properties);
  }, []);

  const trackPageView = useCallback((page: string) => {
    window.oklaAnalytics?.page(page);
    gtag("event", "page_view", { page_path: page });
  }, []);

  return { trackEvent, trackPageView };
}

// Uso
const { trackEvent } = useAnalytics();

// En búsqueda
trackEvent("search", {
  query,
  filters,
  resultCount,
});

// En favorito
trackEvent("favorite_added", {
  vehicleId,
  vehiclePrice,
  source: "vehicle_card",
});
```

---

## ✅ CHECKLIST DE UX POR COMPONENTE

Antes de marcar un componente como completo, verificar:

```markdown
□ Tiene estado de loading con skeleton/spinner
□ Tiene estado de error con mensaje claro y acción de retry
□ Tiene estado vacío con ilustración y CTA
□ Funciona con teclado (Tab, Enter, Escape)
□ Tiene aria-labels para screen readers
□ Contraste de colores pasa WCAG AA
□ Touch targets son >= 44x44px
□ Animaciones respetan prefers-reduced-motion
□ Funciona offline/slow connection (muestra estado)
□ Tiene tests de accesibilidad
```

---

## 🎬 ANIMACIONES ESTÁNDAR

```typescript
// lib/animations.ts
import { Variants } from "framer-motion";

// Fade in suave
export const fadeIn: Variants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { duration: 0.2 } },
};

// Slide up (para modales, toasts)
export const slideUp: Variants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.2 } },
};

// Scale (para botones, cards hover)
export const scale: Variants = {
  initial: { scale: 1 },
  hover: { scale: 1.02, transition: { duration: 0.15 } },
  tap: { scale: 0.98 },
};

// Stagger children (para listas)
export const staggerContainer: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.05,
    },
  },
};

export const staggerItem: Variants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0 },
};
```

**Uso:**

```tsx
<motion.div variants={staggerContainer} initial="hidden" animate="visible">
  {vehicles.map((vehicle) => (
    <motion.div key={vehicle.id} variants={staggerItem}>
      <VehicleCard vehicle={vehicle} />
    </motion.div>
  ))}
</motion.div>
```

---

## 🚫 ANTI-PATRONES (NO HACER)

| ❌ No hacer                      | ✅ Hacer en su lugar                  |
| -------------------------------- | ------------------------------------- |
| Pop-ups intrusivos               | Banners dismissibles                  |
| Auto-play de video con sonido    | Muted con control visible             |
| Infinite scroll sin indicador    | Load more button + contador           |
| Formularios largos en una página | Multi-step con progress               |
| Errores técnicos ("Error 500")   | Mensajes humanos ("Algo salió mal")   |
| Redirects inesperados            | Confirmación antes de salir           |
| Campos requeridos sin indicar    | Asterisco + texto "Requerido"         |
| Botones sin estado disabled      | Disabled + tooltip explicando por qué |

---

**Siguiente documento:** `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/02-design-tokens.md`
