# 🎨 Design Tokens - Sistema de Diseño OKLA

> **Tiempo estimado:** 20 minutos
> **Prerrequisitos:** Proyecto Next.js creado, Tailwind instalado
> **Tema visual:** Inspirado en CarGurus.com (USA) - Verde esmeralda + UI limpia

---

## 📋 OBJETIVO

Establecer tokens de diseño consistentes para:

- **Colores de marca:** Verde primario (#00A870) estilo CarGurus
- **Sistema de Deal Rating:** Badges de calificación de precio
- Tipografía con escala predefinida (Inter/Roboto)
- Espaciado consistente
- Sombras sutiles y bordes
- Breakpoints responsive

> 📖 **Referencia:** Ver [00-TEMA-CARGURUS-AUDITORIA.md](./00-TEMA-CARGURUS-AUDITORIA.md) para detalles del tema

---

## 🔧 PASO 1: Configurar Tailwind CSS

### Código completo

Reemplazar `tailwind.config.ts`:

```typescript
// filepath: tailwind.config.ts
import type { Config } from "tailwindcss";
import tailwindAnimate from "tailwindcss-animate";

const config: Config = {
  darkMode: ["class"],
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    container: {
      center: true,
      padding: "1rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      // ===================
      // COLORES DE MARCA - TEMA CARGURUS
      // ===================
      colors: {
        // Color primario - Verde CarGurus (Esmeralda)
        primary: {
          50: "#E6F7F0", // Background sutil
          100: "#B3E8D4", // Highlight muy claro
          200: "#80D9B8", // Hover backgrounds
          300: "#4DCA9C", // Active states
          400: "#26BD85", // Lighter primary
          500: "#00A870", // ⭐ PRINCIPAL - Verde CarGurus
          600: "#008C5A", // Hover
          700: "#007048", // Active/Pressed
          800: "#005436", // Dark variant
          900: "#003824", // Darkest
          950: "#001C12", // Near black
          DEFAULT: "#00A870",
          foreground: "#ffffff",
        },

        // Secundario - Navy (texto y elementos oscuros)
        secondary: {
          50: "#F5F5F8",
          100: "#E8E8EE",
          200: "#D1D1DD",
          300: "#ABABC2",
          400: "#7A7A9C",
          500: "#4A4A68", // Texto secundario
          600: "#2D2D44", // Texto principal
          700: "#1A1A2E", // ⭐ Navy oscuro - Headlines
          800: "#12121F",
          900: "#0A0A14",
          950: "#05050A",
          DEFAULT: "#1A1A2E",
          foreground: "#ffffff",
        },

        // Success (verde más claro que primary)
        success: {
          50: "#f0fdf4",
          100: "#dcfce7",
          200: "#bbf7d0",
          300: "#86efac",
          400: "#4ade80",
          500: "#22c55e",
          600: "#16a34a",
          700: "#15803d",
          800: "#166534",
          900: "#14532d",
          DEFAULT: "#22c55e",
          foreground: "#ffffff",
        },

        // Warning (naranja - Fair Deal)
        warning: {
          50: "#FFF8E1",
          100: "#FFECB3",
          200: "#FFE082",
          300: "#FFD54F",
          400: "#FFCA28",
          500: "#FFA726", // ⭐ Fair Deal
          600: "#FB8C00",
          700: "#F57C00",
          800: "#EF6C00",
          900: "#E65100",
          DEFAULT: "#FFA726",
          foreground: "#000000",
        },

        // Error/Danger (rojo - High Price)
        danger: {
          50: "#FFEBEE",
          100: "#FFCDD2",
          200: "#EF9A9A",
          300: "#E57373",
          400: "#EF5350", // ⭐ High Price
          500: "#F44336",
          600: "#E53935",
          700: "#D32F2F",
          800: "#C62828",
          900: "#B71C1C", // ⭐ Overpriced
          DEFAULT: "#EF5350",
          foreground: "#ffffff",
        },

        // Info (azul - links e información)
        info: {
          50: "#E3F2FD",
          100: "#BBDEFB",
          200: "#90CAF9",
          300: "#64B5F6",
          400: "#42A5F5",
          500: "#2196F3", // ⭐ Info principal
          600: "#1E88E5",
          700: "#1976D2",
          800: "#1565C0",
          900: "#0D47A1",
          DEFAULT: "#2196F3",
          foreground: "#ffffff",
        },

        // Deal Rating Colors - Sistema CarGurus
        deal: {
          great: "#00A870", // Verde - Excelente Precio
          good: "#7CB342", // Verde lima - Buen Precio
          fair: "#FFA726", // Naranja - Precio Justo
          high: "#EF5350", // Rojo claro - Precio Alto
          overpriced: "#B71C1C", // Rojo oscuro - Sobreprecio
          none: "#9E9E9E", // Gris - Sin análisis
        },

        // Grises para UI (más cálidos, estilo CarGurus)
        gray: {
          50: "#FAFAFA", // Page background
          100: "#F5F5F5", // Card background, inputs
          200: "#EEEEEE", // Borders sutiles
          300: "#E0E0E0", // Borders
          400: "#BDBDBD", // Placeholder, disabled icons
          500: "#9E9E9E", // Icons secondary
          600: "#757575", // Text muted
          700: "#616161", // Text secondary
          800: "#424242", // Text primary light
          900: "#212121", // Text primary
        },

        // Colores semánticos para UI
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",

        // Colores específicos de OKLA - Tema CarGurus
        okla: {
          green: "#00A870", // ⭐ Principal (antes era blue)
          "green-dark": "#008C5A",
          "green-light": "#00C785",
          navy: "#1A1A2E", // Texto headlines
          "navy-light": "#2D2D44",
          orange: "#FFA726", // Fair Deal
          gold: "#FFC107", // Badges premium
        },
      },

      // ===================
      // TIPOGRAFÍA
      // ===================
      fontFamily: {
        sans: [
          "Inter",
          "-apple-system",
          "BlinkMacSystemFont",
          "Segoe UI",
          "Roboto",
          "Helvetica Neue",
          "Arial",
          "sans-serif",
        ],
        display: ["Inter", "-apple-system", "BlinkMacSystemFont", "sans-serif"],
        mono: [
          "JetBrains Mono",
          "Fira Code",
          "Consolas",
          "Monaco",
          "monospace",
        ],
      },

      fontSize: {
        // Display sizes (headings grandes)
        "display-2xl": [
          "4.5rem",
          { lineHeight: "1.1", letterSpacing: "-0.02em" },
        ],
        "display-xl": [
          "3.75rem",
          { lineHeight: "1.1", letterSpacing: "-0.02em" },
        ],
        "display-lg": ["3rem", { lineHeight: "1.2", letterSpacing: "-0.02em" }],
        "display-md": [
          "2.25rem",
          { lineHeight: "1.2", letterSpacing: "-0.01em" },
        ],
        "display-sm": [
          "1.875rem",
          { lineHeight: "1.3", letterSpacing: "-0.01em" },
        ],
        "display-xs": ["1.5rem", { lineHeight: "1.4" }],

        // Body sizes
        "body-xl": ["1.25rem", { lineHeight: "1.75" }],
        "body-lg": ["1.125rem", { lineHeight: "1.75" }],
        "body-md": ["1rem", { lineHeight: "1.6" }],
        "body-sm": ["0.875rem", { lineHeight: "1.5" }],
        "body-xs": ["0.75rem", { lineHeight: "1.5" }],

        // Labels
        "label-lg": ["0.875rem", { lineHeight: "1.25", fontWeight: "500" }],
        "label-md": ["0.8125rem", { lineHeight: "1.25", fontWeight: "500" }],
        "label-sm": ["0.75rem", { lineHeight: "1.25", fontWeight: "500" }],

        // Price display
        "price-xl": ["2.5rem", { lineHeight: "1", fontWeight: "700" }],
        "price-lg": ["2rem", { lineHeight: "1", fontWeight: "700" }],
        "price-md": ["1.5rem", { lineHeight: "1", fontWeight: "600" }],
        "price-sm": ["1.25rem", { lineHeight: "1", fontWeight: "600" }],
      },

      // ===================
      // ESPACIADO ADICIONAL
      // ===================
      spacing: {
        "4.5": "1.125rem",
        "5.5": "1.375rem",
        "6.5": "1.625rem",
        "7.5": "1.875rem",
        "8.5": "2.125rem",
        "9.5": "2.375rem",
        "13": "3.25rem",
        "15": "3.75rem",
        "17": "4.25rem",
        "18": "4.5rem",
        "19": "4.75rem",
        "21": "5.25rem",
        "22": "5.5rem",
        "26": "6.5rem",
        "30": "7.5rem",
      },

      // ===================
      // BORDES
      // ===================
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
        "4xl": "2rem",
        "5xl": "2.5rem",
      },

      // ===================
      // SOMBRAS - Estilo CarGurus (sutiles)
      // ===================
      boxShadow: {
        // Sombras muy sutiles para UI limpia
        xs: "0 1px 2px rgba(0, 0, 0, 0.04)",
        "card-sm": "0 2px 4px rgba(0, 0, 0, 0.06)",
        card: "0 2px 8px rgba(0, 0, 0, 0.08)",
        "card-md": "0 4px 12px rgba(0, 0, 0, 0.08)",
        "card-lg": "0 8px 24px rgba(0, 0, 0, 0.12)",

        // Sombra hover para cards (distintivo CarGurus)
        "card-hover": "0 8px 24px rgba(0, 0, 0, 0.12)",

        // Sombras para botones - Verde primario
        "button-primary": "0 4px 12px rgba(0, 168, 112, 0.25)",
        "button-danger": "0 4px 12px rgba(239, 83, 80, 0.25)",
        "button-success": "0 4px 12px rgba(34, 197, 94, 0.25)",

        // Sombras para modales/dropdowns
        modal: "0 16px 48px rgba(0, 0, 0, 0.20)",
        dropdown: "0 4px 16px rgba(0, 0, 0, 0.12)",

        // Glow effects - Verde CarGurus
        "glow-primary": "0 0 20px rgba(0, 168, 112, 0.25)",
        "glow-success": "0 0 20px rgba(34, 197, 94, 0.25)",

        // Navbar shadow
        navbar: "0 2px 4px rgba(0, 0, 0, 0.04)",
      },

      // ===================
      // ANIMACIONES
      // ===================
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" },
        },
        "fade-in": {
          from: { opacity: "0" },
          to: { opacity: "1" },
        },
        "fade-out": {
          from: { opacity: "1" },
          to: { opacity: "0" },
        },
        "slide-in-from-top": {
          from: { transform: "translateY(-10px)", opacity: "0" },
          to: { transform: "translateY(0)", opacity: "1" },
        },
        "slide-in-from-bottom": {
          from: { transform: "translateY(10px)", opacity: "0" },
          to: { transform: "translateY(0)", opacity: "1" },
        },
        "slide-in-from-left": {
          from: { transform: "translateX(-10px)", opacity: "0" },
          to: { transform: "translateX(0)", opacity: "1" },
        },
        "slide-in-from-right": {
          from: { transform: "translateX(10px)", opacity: "0" },
          to: { transform: "translateX(0)", opacity: "1" },
        },
        shimmer: {
          "0%": { backgroundPosition: "-200% 0" },
          "100%": { backgroundPosition: "200% 0" },
        },
        pulse: {
          "0%, 100%": { opacity: "1" },
          "50%": { opacity: "0.5" },
        },
        bounce: {
          "0%, 100%": { transform: "translateY(-5%)" },
          "50%": { transform: "translateY(0)" },
        },
        spin: {
          from: { transform: "rotate(0deg)" },
          to: { transform: "rotate(360deg)" },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
        "fade-in": "fade-in 0.2s ease-out",
        "fade-out": "fade-out 0.2s ease-out",
        "slide-in-top": "slide-in-from-top 0.3s ease-out",
        "slide-in-bottom": "slide-in-from-bottom 0.3s ease-out",
        "slide-in-left": "slide-in-from-left 0.3s ease-out",
        "slide-in-right": "slide-in-from-right 0.3s ease-out",
        shimmer: "shimmer 2s infinite linear",
        pulse: "pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite",
        bounce: "bounce 1s infinite",
        spin: "spin 1s linear infinite",
      },

      // ===================
      // Z-INDEX SCALE
      // ===================
      zIndex: {
        dropdown: "1000",
        sticky: "1020",
        fixed: "1030",
        "modal-backdrop": "1040",
        modal: "1050",
        popover: "1060",
        tooltip: "1070",
        toast: "1080",
      },

      // ===================
      // ASPECT RATIOS
      // ===================
      aspectRatio: {
        vehicle: "4 / 3",
        "vehicle-wide": "16 / 9",
        "vehicle-square": "1 / 1",
        card: "3 / 4",
      },
    },
  },
  plugins: [tailwindAnimate],
};

export default config;
```

---

## 🔧 PASO 2: Configurar CSS Variables

### Código a crear/actualizar

```css
/* filepath: src/styles/globals.css */

@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    /* Background & Foreground */
    --background: 0 0% 100%;
    --foreground: 240 10% 10%;

    /* Card */
    --card: 0 0% 100%;
    --card-foreground: 240 10% 10%;

    /* Popover */
    --popover: 0 0% 100%;
    --popover-foreground: 240 10% 10%;

    /* Primary - Verde CarGurus (#00A870) */
    --primary: 158 100% 33%;
    --primary-foreground: 0 0% 100%;

    /* Secondary - Navy (#1A1A2E) */
    --secondary: 240 30% 14%;
    --secondary-foreground: 0 0% 100%;

    /* Muted */
    --muted: 0 0% 96%;
    --muted-foreground: 240 5% 46%;

    /* Accent - Verde claro */
    --accent: 158 80% 95%;
    --accent-foreground: 158 100% 25%;

    /* Destructive */
    --destructive: 4 80% 63%;
    --destructive-foreground: 0 0% 100%;

    /* Border & Input - Grises sutiles */
    --border: 0 0% 90%;
    --input: 0 0% 90%;
    --ring: 158 100% 33%;

    /* Radius */
    --radius: 0.5rem;

    /* Chart colors - Paleta CarGurus */
    --chart-1: 158 100% 33%;
    --chart-2: 84 55% 47%;
    --chart-3: 36 100% 57%;
    --chart-4: 4 80% 63%;
    --chart-5: 207 90% 54%;
  }

  .dark {
    --background: 240 10% 6%;
    --foreground: 0 0% 98%;

    --card: 240 10% 8%;
    --card-foreground: 0 0% 98%;

    --popover: 240 10% 8%;
    --popover-foreground: 0 0% 98%;

    --primary: 158 80% 45%;
    --primary-foreground: 240 10% 6%;

    --secondary: 240 20% 20%;
    --secondary-foreground: 0 0% 98%;

    --muted: 240 10% 15%;
    --muted-foreground: 240 5% 65%;

    --accent: 158 50% 15%;
    --accent-foreground: 158 80% 70%;

    --destructive: 0 63% 31%;
    --destructive-foreground: 0 0% 98%;

    --border: 240 10% 20%;
    --input: 240 10% 20%;
    --ring: 158 80% 45%;
  }
}

@layer base {
  * {
    @apply border-border;
  }

  body {
    @apply bg-background text-foreground antialiased;
    font-feature-settings:
      "rlig" 1,
      "calt" 1;
  }

  /* Smooth scrolling */
  html {
    scroll-behavior: smooth;
  }

  /* Focus styles accesibles */
  *:focus-visible {
    @apply outline-none ring-2 ring-primary ring-offset-2 ring-offset-background;
  }

  /* Remove autofill background */
  input:-webkit-autofill,
  input:-webkit-autofill:hover,
  input:-webkit-autofill:focus {
    -webkit-box-shadow: 0 0 0px 1000px var(--background) inset !important;
    box-shadow: 0 0 0px 1000px var(--background) inset !important;
    -webkit-text-fill-color: inherit !important;
  }
}

@layer components {
  /* Container responsive */
  .container-page {
    @apply mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8;
  }

  .container-narrow {
    @apply mx-auto w-full max-w-4xl px-4 sm:px-6 lg:px-8;
  }

  .container-wide {
    @apply mx-auto w-full max-w-[1400px] px-4 sm:px-6 lg:px-8;
  }

  /* Gradient backgrounds - Verde CarGurus */
  .gradient-primary {
    @apply bg-gradient-to-r from-primary-600 to-primary-500;
  }

  .gradient-success {
    @apply bg-gradient-to-r from-success-600 to-success-500;
  }

  .gradient-hero {
    @apply bg-gradient-to-br from-gray-50 via-white to-gray-100;
  }

  .gradient-hero-green {
    @apply bg-gradient-to-br from-primary-50 via-white to-primary-100;
  }

  /* Text gradients */
  .text-gradient {
    @apply bg-clip-text text-transparent;
  }

  .text-gradient-primary {
    @apply bg-gradient-to-r from-primary-600 to-primary-400 bg-clip-text text-transparent;
  }

  /* Glass effect */
  .glass {
    @apply bg-white/80 backdrop-blur-md dark:bg-gray-900/80;
  }

  .glass-strong {
    @apply bg-white/95 backdrop-blur-lg dark:bg-gray-900/95;
  }

  /* Card hover effects - Estilo CarGurus */
  .card-hover {
    @apply transition-all duration-300 hover:-translate-y-1 hover:shadow-card-hover;
  }

  /* Skeleton loading */
  .skeleton {
    @apply animate-shimmer bg-gradient-to-r from-gray-200 via-gray-100 to-gray-200 bg-[length:400%_100%];
  }

  /* Hide scrollbar but keep functionality */
  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

  .scrollbar-hide::-webkit-scrollbar {
    display: none;
  }

  /* Price formatting */
  .price-display {
    @apply font-bold tabular-nums tracking-tight;
  }

  .price-currency {
    @apply text-[0.6em] font-semibold;
  }

  /* Badge styles */
  .badge-verified {
    @apply inline-flex items-center gap-1 rounded-full bg-success-100 px-2.5 py-0.5 text-xs font-medium text-success-700;
  }

  .badge-featured {
    @apply inline-flex items-center gap-1 rounded-full bg-warning-100 px-2.5 py-0.5 text-xs font-medium text-warning-700;
  }

  .badge-new {
    @apply inline-flex items-center gap-1 rounded-full bg-primary-100 px-2.5 py-0.5 text-xs font-medium text-primary-700;
  }
}

@layer utilities {
  /* Line clamp utilities */
  .line-clamp-1 {
    display: -webkit-box;
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  /* Safe area padding for mobile */
  .safe-bottom {
    padding-bottom: env(safe-area-inset-bottom);
  }

  .safe-top {
    padding-top: env(safe-area-inset-top);
  }

  /* Touch action for mobile */
  .touch-pan-x {
    touch-action: pan-x;
  }

  .touch-pan-y {
    touch-action: pan-y;
  }
}
```

---

## 🔧 PASO 3: Instalar fuentes

### Código a actualizar

```tsx
// filepath: src/app/layout.tsx
import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "@/styles/globals.css";

const inter = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: {
    default: "OKLA - Marketplace de Vehículos en República Dominicana",
    template: "%s | OKLA",
  },
  description:
    "Encuentra y compra vehículos nuevos y usados en República Dominicana. Miles de autos, SUVs, camionetas y más.",
  keywords: [
    "carros",
    "vehículos",
    "autos",
    "República Dominicana",
    "comprar carro",
    "vender carro",
    "marketplace",
  ],
  authors: [{ name: "OKLA" }],
  creator: "OKLA",
  metadataBase: new URL("https://okla.com.do"),
  openGraph: {
    type: "website",
    locale: "es_DO",
    url: "https://okla.com.do",
    siteName: "OKLA",
    title: "OKLA - Marketplace de Vehículos en República Dominicana",
    description:
      "Encuentra y compra vehículos nuevos y usados en República Dominicana.",
    images: [
      {
        url: "/og-image.jpg",
        width: 1200,
        height: 630,
        alt: "OKLA Marketplace",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "OKLA - Marketplace de Vehículos",
    description: "Encuentra tu próximo vehículo en República Dominicana",
    images: ["/og-image.jpg"],
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es" className={inter.variable}>
      <body className="min-h-screen bg-background font-sans antialiased">
        {children}
      </body>
    </html>
  );
}
```

---

## 🔧 PASO 4: Crear constantes de diseño

### Código a crear

```typescript
// filepath: src/lib/design-tokens.ts

/**
 * Design Tokens exportados como constantes TypeScript
 * Para usar en componentes que necesitan valores programáticos
 *
 * TEMA: CarGurus USA - Verde esmeralda como primario
 */

// ⭐ COLORES PRINCIPALES - Tema CarGurus
export const COLORS = {
  // Verde CarGurus - Color primario
  primary: {
    50: "#E6F7F0",
    100: "#B3E8D4",
    200: "#80D9B8",
    300: "#4DCA9C",
    400: "#26BD85",
    500: "#00A870", // ⭐ Principal
    600: "#008C5A",
    700: "#007048",
    800: "#005436",
    900: "#003824",
  },
  // Navy para texto
  secondary: {
    500: "#4A4A68",
    600: "#2D2D44",
    700: "#1A1A2E", // ⭐ Headlines
  },
  // Grises UI
  gray: {
    50: "#FAFAFA",
    100: "#F5F5F5",
    200: "#EEEEEE",
    300: "#E0E0E0",
    400: "#BDBDBD",
    500: "#9E9E9E",
  },
  success: {
    500: "#22c55e",
    600: "#16a34a",
  },
  warning: {
    500: "#FFA726",
    600: "#FB8C00",
  },
  danger: {
    400: "#EF5350",
    500: "#F44336",
    900: "#B71C1C",
  },
  info: {
    500: "#2196F3",
    600: "#1E88E5",
  },
} as const;

// ⭐ DEAL RATING - Sistema distintivo de CarGurus
export const DEAL_RATING = {
  great: {
    label: "Excelente Precio",
    labelEn: "Great Deal",
    color: "#FFFFFF",
    bgColor: "#00A870",
    icon: "🔥",
  },
  good: {
    label: "Buen Precio",
    labelEn: "Good Deal",
    color: "#FFFFFF",
    bgColor: "#7CB342",
    icon: "👍",
  },
  fair: {
    label: "Precio Justo",
    labelEn: "Fair Deal",
    color: "#000000",
    bgColor: "#FFA726",
    icon: "➖",
  },
  high: {
    label: "Precio Alto",
    labelEn: "High Price",
    color: "#FFFFFF",
    bgColor: "#EF5350",
    icon: "⚠️",
  },
  overpriced: {
    label: "Sobreprecio",
    labelEn: "Overpriced",
    color: "#FFFFFF",
    bgColor: "#B71C1C",
    icon: "❌",
  },
  none: {
    label: "Sin Análisis",
    labelEn: "No Analysis",
    color: "#FFFFFF",
    bgColor: "#9E9E9E",
    icon: "❓",
  },
} as const;

export type DealRatingType = keyof typeof DEAL_RATING;

export const BREAKPOINTS = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  "2xl": 1400,
} as const;

export const SPACING = {
  xs: "0.25rem", // 4px
  sm: "0.5rem", // 8px
  md: "1rem", // 16px
  lg: "1.5rem", // 24px
  xl: "2rem", // 32px
  "2xl": "3rem", // 48px
  "3xl": "4rem", // 64px
} as const;

export const FONT_SIZES = {
  xs: "0.75rem",
  sm: "0.875rem",
  base: "1rem",
  lg: "1.125rem",
  xl: "1.25rem",
  "2xl": "1.5rem",
  "3xl": "1.875rem",
  "4xl": "2.25rem",
} as const;

export const Z_INDEX = {
  dropdown: 1000,
  sticky: 1020,
  fixed: 1030,
  modalBackdrop: 1040,
  modal: 1050,
  popover: 1060,
  tooltip: 1070,
  toast: 1080,
} as const;

export const TRANSITIONS = {
  fast: "150ms ease-in-out",
  normal: "200ms ease-in-out",
  slow: "300ms ease-in-out",
} as const;

export const SHADOWS = {
  sm: "0 1px 2px 0 rgb(0 0 0 / 0.05)",
  md: "0 4px 6px -1px rgb(0 0 0 / 0.1)",
  lg: "0 10px 15px -3px rgb(0 0 0 / 0.1)",
  xl: "0 25px 50px -12px rgb(0 0 0 / 0.25)",
} as const;

// Vehicle image aspect ratios
export const ASPECT_RATIOS = {
  vehicleCard: "4/3",
  vehicleGallery: "16/9",
  vehicleThumb: "1/1",
} as const;

// Animation durations
export const ANIMATION = {
  fast: 150,
  normal: 200,
  slow: 300,
  skeleton: 2000,
} as const;

// Form field sizes
export const FIELD_SIZES = {
  sm: {
    height: "2rem",
    padding: "0.5rem 0.75rem",
    fontSize: "0.875rem",
  },
  md: {
    height: "2.5rem",
    padding: "0.625rem 1rem",
    fontSize: "1rem",
  },
  lg: {
    height: "3rem",
    padding: "0.75rem 1.25rem",
    fontSize: "1.125rem",
  },
} as const;

// Price formatting options for RD
export const PRICE_FORMAT = {
  locale: "es-DO",
  currency: "DOP",
  currencyDisplay: "symbol",
} as const;

// Media query helpers
export const mediaQuery = {
  sm: `(min-width: ${BREAKPOINTS.sm}px)`,
  md: `(min-width: ${BREAKPOINTS.md}px)`,
  lg: `(min-width: ${BREAKPOINTS.lg}px)`,
  xl: `(min-width: ${BREAKPOINTS.xl}px)`,
  "2xl": `(min-width: ${BREAKPOINTS["2xl"]}px)`,
} as const;
```

---

## ✅ VALIDACIÓN

### Verificar compilación

```bash
# Verificar que todo compila
pnpm build

# Output esperado:
# ✓ Compiled successfully
```

### Verificar estilos en dev

```bash
# Iniciar servidor de desarrollo
pnpm dev

# Abrir http://localhost:3000
# Los estilos base deben estar aplicados
```

### Test visual rápido

Crear página temporal:

```tsx
// filepath: src/app/design-test/page.tsx (borrar después)
export default function DesignTestPage() {
  return (
    <div className="container-page py-12">
      <h1 className="text-display-lg text-primary-600">Título Principal</h1>
      <p className="text-body-lg text-muted-foreground">Texto de párrafo</p>

      <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div className="rounded-lg border bg-card p-4 shadow-card card-hover">
          <p className="text-price-lg price-display">RD$1,850,000</p>
        </div>
        <div className="rounded-lg border bg-card p-4 shadow-card card-hover">
          <span className="badge-verified">Verificado</span>
        </div>
        <div className="rounded-lg border bg-card p-4 shadow-card card-hover">
          <span className="badge-featured">Destacado</span>
        </div>
      </div>

      <div className="mt-8 flex gap-4">
        <button className="rounded-lg bg-primary px-4 py-2 text-white shadow-button-primary transition hover:bg-primary-600">
          Botón Primary
        </button>
        <button className="rounded-lg bg-success px-4 py-2 text-white shadow-button-success transition hover:bg-success-600">
          Botón Success
        </button>
        <button className="rounded-lg bg-danger px-4 py-2 text-white shadow-button-danger transition hover:bg-danger-600">
          Botón Danger
        </button>
      </div>
    </div>
  );
}
```

Visitar `http://localhost:3000/design-test` y verificar que:

- ✅ Colores se aplican correctamente
- ✅ Tipografía usa Inter
- ✅ Sombras son visibles
- ✅ Hover states funcionan
- ✅ Responsive funciona (probar resize)

```bash
# Borrar página de prueba
rm src/app/design-test/page.tsx
```

---

## 📊 RESUMEN

| Archivo                    | Propósito                                |
| -------------------------- | ---------------------------------------- |
| `tailwind.config.ts`       | Configuración completa de Tailwind       |
| `src/styles/globals.css`   | Variables CSS y utilidades               |
| `src/app/layout.tsx`       | Layout raíz con fuentes y metadata       |
| `src/lib/design-tokens.ts` | Tokens exportables para uso programático |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/03-componentes-base.md`
