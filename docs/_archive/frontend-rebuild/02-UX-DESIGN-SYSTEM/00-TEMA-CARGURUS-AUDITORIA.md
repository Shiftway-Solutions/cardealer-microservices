# 🎨 AUDITORÍA DE TEMA - Estilo CarGurus USA

> **Fecha:** Enero 31, 2026
> **Objetivo:** Alinear el Design System de OKLA con el tema visual de CarGurus.com (USA)
> **Nota:** Este documento define el TEMA VISUAL, no el contenido. El contenido sigue siendo OKLA para República Dominicana.

---

## 📋 RESUMEN EJECUTIVO

### ❌ ANTES (Tema Actual OKLA)

- Color primario: **Azul** (#3b82f6)
- Estilo: Genérico, similar a Tailwind default
- Botones: Azules
- No hay sistema de "Deal Rating"

### ✅ DESPUÉS (Tema CarGurus)

- Color primario: **Verde Esmeralda** (#00A870)
- Estilo: Limpio, profesional, confiable
- Botones: Verdes para CTA, outline para secundarios
- Sistema de Deal Rating con colores semánticos

---

## 🎨 ANÁLISIS DEL TEMA CARGURUS

### 1. PALETA DE COLORES

#### Color Primario - Verde CarGurus

```
Verde Principal:  #00A870 (Esmeralda)
Verde Oscuro:     #008C5A (Hover)
Verde Claro:      #00C785 (Active)
Verde Suave:      #E6F7F0 (Background sutil)
```

#### Color Secundario - Navy/Dark

```
Navy Oscuro:      #1A1A2E (Texto principal)
Navy Medio:       #2D2D44 (Texto secundario)
Navy Claro:       #4A4A68 (Texto muted)
```

#### Grises (UI)

```
Gris 50:          #FAFAFA (Background page)
Gris 100:         #F5F5F5 (Background cards)
Gris 200:         #EEEEEE (Borders sutiles)
Gris 300:         #E0E0E0 (Borders)
Gris 400:         #BDBDBD (Placeholder text)
Gris 500:         #9E9E9E (Icons disabled)
```

#### Deal Rating Colors (DISTINTIVO DE CARGURUS)

```
Great Deal:       #00A870 (Verde)    - Excelente precio
Good Deal:        #7CB342 (Verde Lima) - Buen precio
Fair Deal:        #FFA726 (Naranja)  - Precio justo
High Price:       #EF5350 (Rojo)     - Precio alto
Overpriced:       #B71C1C (Rojo oscuro) - Sobreprecio
No Analysis:      #9E9E9E (Gris)     - Sin análisis
```

#### Colores de Acento

```
Azul Info:        #2196F3 (Links, info)
Amarillo Warning: #FFC107 (Alertas)
Rojo Error:       #F44336 (Errores)
```

### 2. TIPOGRAFÍA

#### Font Family

```css
/* CarGurus usa una fuente sans-serif muy limpia */
font-family:
  "Roboto",
  -apple-system,
  BlinkMacSystemFont,
  "Segoe UI",
  sans-serif;

/* Alternativa con Inter (más moderna) */
font-family:
  "Inter",
  -apple-system,
  BlinkMacSystemFont,
  "Segoe UI",
  sans-serif;
```

#### Escala Tipográfica

```
Hero Title:       48px / 56px / 700 (Bold)
Page Title:       32px / 40px / 600 (Semibold)
Section Title:    24px / 32px / 600 (Semibold)
Card Title:       18px / 24px / 600 (Semibold)
Body Large:       16px / 24px / 400 (Regular)
Body:             14px / 20px / 400 (Regular)
Small:            12px / 16px / 400 (Regular)
Caption:          11px / 14px / 400 (Regular)

Precio Display:   28px / 32px / 700 (Bold) - Verde o Navy
Precio Tachado:   16px / 20px / 400 - Gris con line-through
```

### 3. COMPONENTES UI

#### Botones

```
Primary (CTA):
  - Background: #00A870 (Verde)
  - Text: #FFFFFF
  - Hover: #008C5A
  - Border-radius: 8px
  - Padding: 12px 24px
  - Font-weight: 600
  - Shadow: 0 2px 8px rgba(0, 168, 112, 0.3)

Secondary:
  - Background: transparent
  - Border: 1px solid #E0E0E0
  - Text: #1A1A2E
  - Hover: #F5F5F5
  - Border-radius: 8px

Tertiary (Ghost):
  - Background: transparent
  - Text: #00A870
  - Hover: #E6F7F0
```

#### Cards de Vehículos

```
Container:
  - Background: #FFFFFF
  - Border: 1px solid #EEEEEE
  - Border-radius: 12px
  - Shadow: 0 2px 8px rgba(0, 0, 0, 0.08)
  - Hover shadow: 0 8px 24px rgba(0, 0, 0, 0.12)

Image:
  - Aspect-ratio: 4:3
  - Border-radius: 12px 12px 0 0
  - Object-fit: cover

Deal Badge (esquina superior):
  - Posición: absolute top-3 left-3
  - Padding: 4px 8px
  - Border-radius: 4px
  - Font-size: 11px
  - Font-weight: 600
  - Uppercase

Content:
  - Padding: 16px
  - Gap: 8px
```

#### Navegación

```
Navbar:
  - Background: #FFFFFF
  - Border-bottom: 1px solid #EEEEEE
  - Height: 64px
  - Shadow: 0 2px 4px rgba(0, 0, 0, 0.04)

Logo:
  - Color primario: #00A870

Nav Links:
  - Color: #1A1A2E
  - Hover: #00A870
  - Font-weight: 500
  - Font-size: 14px

Search Bar:
  - Background: #F5F5F5
  - Border: 1px solid transparent
  - Focus border: #00A870
  - Border-radius: 8px
  - Height: 44px
```

#### Filtros

```
Filter Container:
  - Background: #FFFFFF
  - Border: 1px solid #EEEEEE
  - Border-radius: 8px
  - Padding: 16px

Filter Chips:
  - Background: #F5F5F5
  - Active: #E6F7F0 con border #00A870
  - Border-radius: 20px
  - Padding: 8px 16px

Range Slider:
  - Track: #EEEEEE
  - Active: #00A870
  - Thumb: #FFFFFF con border #00A870
```

### 4. ESPACIADO Y LAYOUT

```
Container max-width:    1280px
Container padding:      16px (mobile) / 24px (tablet) / 32px (desktop)

Grid gap:               16px (mobile) / 24px (desktop)
Card padding:           16px
Section margin:         48px (mobile) / 64px (desktop)

Border-radius:
  - xs: 4px  (badges, tags)
  - sm: 6px  (inputs, small buttons)
  - md: 8px  (buttons, cards pequeñas)
  - lg: 12px (cards, modals)
  - xl: 16px (hero sections)
  - full: 9999px (avatars, pills)
```

### 5. SOMBRAS

```css
/* Sombras sutiles - CarGurus usa sombras muy suaves */
--shadow-xs: 0 1px 2px rgba(0, 0, 0, 0.04);
--shadow-sm: 0 2px 4px rgba(0, 0, 0, 0.06);
--shadow-md: 0 4px 8px rgba(0, 0, 0, 0.08);
--shadow-lg: 0 8px 16px rgba(0, 0, 0, 0.1);
--shadow-xl: 0 16px 32px rgba(0, 0, 0, 0.12);

/* Sombra para cards hover */
--shadow-card-hover: 0 8px 24px rgba(0, 0, 0, 0.12);

/* Sombra verde para CTAs */
--shadow-primary: 0 4px 12px rgba(0, 168, 112, 0.25);
```

---

## 🔄 MAPEO DE CAMBIOS REQUERIDOS

### Archivos a Actualizar

| Archivo                             | Cambios                      |
| ----------------------------------- | ---------------------------- |
| `02-design-tokens.md`               | Colores, tipografía, sombras |
| `03-componentes-base.md`            | Buttons, Cards, Inputs       |
| `01-principios-ux.md`               | Referencias de colores       |
| `04-patrones-ux.md`                 | Skeletons, estados           |
| `../03-COMPONENTES/03-vehiculos.md` | VehicleCard con Deal Rating  |

### Nuevos Componentes Requeridos

1. **DealRatingBadge** - Badge de calificación de precio
2. **PriceDisplay** - Precio con formato y comparación
3. **SearchBar** - Barra de búsqueda estilo CarGurus
4. **FilterChip** - Chips de filtro activos

---

## 📊 SISTEMA DE DEAL RATING

### Lógica del Badge

```typescript
type DealRating =
  | "great" // Verde - Mejor que mercado >10%
  | "good" // Verde lima - Mejor que mercado 5-10%
  | "fair" // Naranja - Precio de mercado ±5%
  | "high" // Rojo claro - Mayor que mercado 5-15%
  | "overpriced" // Rojo oscuro - Mayor que mercado >15%
  | "none"; // Gris - Sin datos suficientes

interface DealRatingConfig {
  label: string;
  labelEs: string; // Para RD
  color: string;
  bgColor: string;
  icon: string;
}

const dealRatings: Record<DealRating, DealRatingConfig> = {
  great: {
    label: "Great Deal",
    labelEs: "Excelente Precio",
    color: "#FFFFFF",
    bgColor: "#00A870",
    icon: "🔥",
  },
  good: {
    label: "Good Deal",
    labelEs: "Buen Precio",
    color: "#FFFFFF",
    bgColor: "#7CB342",
    icon: "👍",
  },
  fair: {
    label: "Fair Deal",
    labelEs: "Precio Justo",
    color: "#000000",
    bgColor: "#FFA726",
    icon: "➖",
  },
  high: {
    label: "High Price",
    labelEs: "Precio Alto",
    color: "#FFFFFF",
    bgColor: "#EF5350",
    icon: "⚠️",
  },
  overpriced: {
    label: "Overpriced",
    labelEs: "Sobreprecio",
    color: "#FFFFFF",
    bgColor: "#B71C1C",
    icon: "❌",
  },
  none: {
    label: "No Price Analysis",
    labelEs: "Sin Análisis",
    color: "#FFFFFF",
    bgColor: "#9E9E9E",
    icon: "❓",
  },
};
```

### Componente DealRatingBadge

```tsx
// filepath: src/components/vehicles/DealRatingBadge.tsx
interface DealRatingBadgeProps {
  rating: DealRating;
  size?: "sm" | "md" | "lg";
  showIcon?: boolean;
  className?: string;
}

export function DealRatingBadge({
  rating,
  size = "md",
  showIcon = true,
  className,
}: DealRatingBadgeProps) {
  const config = dealRatings[rating];

  const sizeClasses = {
    sm: "text-[10px] px-1.5 py-0.5",
    md: "text-xs px-2 py-1",
    lg: "text-sm px-3 py-1.5",
  };

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 font-semibold uppercase rounded",
        sizeClasses[size],
        className,
      )}
      style={{
        backgroundColor: config.bgColor,
        color: config.color,
      }}
    >
      {showIcon && <span>{config.icon}</span>}
      {config.labelEs}
    </span>
  );
}
```

---

## 🖼️ MOCKUPS DE REFERENCIA

### VehicleCard - Estructura Visual

```
┌─────────────────────────────────────┐
│  ┌───────────────────────────────┐  │
│  │                               │  │
│  │        IMAGEN VEHÍCULO        │  │
│  │         (aspect 4:3)          │  │
│  │                               │  │
│  │ ┌────────────────┐            │  │
│  │ │ EXCELENTE     │            │  │
│  │ │ PRECIO 🔥     │            │  │
│  │ └────────────────┘            │  │
│  │                    ♡  (fav)   │  │
│  └───────────────────────────────┘  │
│                                     │
│  2024 Toyota Corolla LE             │  ← Título (18px, semibold)
│  32,450 km • Santo Domingo          │  ← Specs (14px, gray)
│                                     │
│  RD$ 1,250,000                      │  ← Precio (24px, bold, verde)
│  ↓ RD$ 50,000 bajo mercado          │  ← Comparación (12px, verde)
│                                     │
│  [  Ver Detalles  ]                 │  ← Botón outline
└─────────────────────────────────────┘
```

### Navbar - Estructura Visual

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  🚗 OKLA    │ Comprar ▼ │ Vender │ Dealers │   🔍 Buscar marca, modelo...   │ [Entrar]
│             │           │        │         │                                │
└─────────────────────────────────────────────────────────────────────────────┘
     ↑              ↑                                    ↑                        ↑
   Logo         Nav links                          Search bar               CTA verde
   verde         #1A1A2E                          #F5F5F5 bg              #00A870 bg
```

### Hero Section - Estructura Visual

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│        Encuentra tu próximo vehículo en República Dominicana               │
│                     (48px, bold, #1A1A2E)                                  │
│                                                                             │
│           Miles de vehículos con precios transparentes                     │
│                     (18px, regular, #4A4A68)                               │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  🔍  │  Marca  ▼  │  Modelo  ▼  │  Precio  ▼  │  [ Buscar ]        │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│           🏆 +50,000 vehículos    💰 Precios verificados    ⭐ 4.8/5       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
          Background: Gradiente sutil #FAFAFA → #F0F0F0
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Fase 1: Design Tokens (Prioridad Alta)

- [ ] Actualizar colores primarios a verde (#00A870)
- [ ] Agregar paleta de Deal Rating
- [ ] Actualizar tipografía a Roboto/Inter
- [ ] Actualizar sombras a estilo más sutil
- [ ] Actualizar border-radius scale

### Fase 2: Componentes Base

- [ ] Actualizar Button con variante verde
- [ ] Crear DealRatingBadge
- [ ] Actualizar Card styles
- [ ] Actualizar Input/SearchBar
- [ ] Actualizar FilterChip

### Fase 3: Componentes de Vehículos

- [ ] Actualizar VehicleCard con Deal Rating
- [ ] Agregar PriceDisplay con comparación
- [ ] Actualizar VehicleGrid
- [ ] Actualizar filtros

### Fase 4: Layout

- [ ] Actualizar Navbar con tema verde
- [ ] Actualizar Footer
- [ ] Actualizar Hero sections

---

## 📝 NOTAS IMPORTANTES

1. **Mantener contenido en español** - Solo cambiamos el tema visual, no el idioma
2. **Moneda RD$** - Seguimos usando pesos dominicanos
3. **Localización** - Ciudades y provincias de RD, no USA
4. **Logo OKLA** - Actualizar a verde para consistencia
5. **Accesibilidad** - El verde #00A870 cumple con WCAG AA sobre blanco

---

## 🎯 RESULTADO ESPERADO

Después de aplicar estos cambios, OKLA tendrá:

1. ✅ **Identidad visual profesional** similar a CarGurus
2. ✅ **Color verde distintivo** que transmite confianza y ahorro
3. ✅ **Sistema de Deal Rating** que ayuda a tomar decisiones
4. ✅ **UI limpia y moderna** con sombras sutiles
5. ✅ **Experiencia consistente** en todos los componentes

---

_Documento creado: Enero 31, 2026_
_Próxima revisión: Al completar implementación_
