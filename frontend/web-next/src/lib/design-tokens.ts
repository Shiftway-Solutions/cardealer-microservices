/**
 * @module design-tokens
 * @description OKLA Design System — Centralized design tokens
 *
 * This module is the single source of truth for all design values used across
 * the OKLA vehicle marketplace frontend. It maps directly to the CSS custom
 * properties defined in `globals.css` and the Tailwind theme.
 *
 * ## Usage
 *
 * ```tsx
 * import { colors, typography, spacing } from '@/lib/design-tokens';
 *
 * // Use in inline styles (rare — prefer Tailwind classes)
 * <div style={{ color: colors.primary[500] }} />
 *
 * // Use in JS logic (e.g., chart colors, canvas rendering)
 * const chartColor = colors.deal.great;
 *
 * // Prefer Tailwind equivalents:
 * // colors.primary[500] → className="text-primary"
 * // colors.background.secondary → className="bg-secondary"
 * ```
 *
 * ## CSS Variable Mapping
 *
 * | Token                  | CSS Variable        | Tailwind Class     |
 * |------------------------|---------------------|--------------------|
 * | `colors.primary[500]`  | `--primary`         | `text-primary`     |
 * | `colors.text.primary`  | `--foreground`      | `text-foreground`  |
 * | `colors.text.secondary`| `--muted-foreground`| `text-muted-foreground` |
 * | `colors.border.default`| `--border`          | `border-border`    |
 * | `colors.background.*`  | `--background`      | `bg-background`    |
 *
 * @see {@link file://./../../app/globals.css} for CSS custom property definitions
 */

// =============================================================================
// OKLA Design System - Design Tokens
// =============================================================================
// Sistema de diseño centralizado para toda la aplicación
// Colores, tipografía, espaciado, sombras, animaciones

/**
 * Color palette for the OKLA brand and UI.
 *
 * - **primary**: Emerald green `#00A870` — brand color used for CTAs, links, active states
 * - **secondary**: Navy blue — used for secondary actions and informational elements
 * - **neutral**: Gray scale for text, borders, backgrounds
 * - **semantic**: success/warning/error/info for feedback states
 * - **deal**: Price rating system colors (great → high)
 */
export const colors = {
  // Brand Colors - Verde Esmeralda OKLA
  primary: {
    50: '#E6F7F0',
    100: '#B3E8D4',
    200: '#80D9B8',
    300: '#4DCA9C',
    400: '#26BE85',
    500: '#00A870', // Color principal
    600: '#009663',
    700: '#008456',
    800: '#006B46',
    900: '#005236',
  },

  // Secondary - Azul Marino
  secondary: {
    50: '#E8EEF4',
    100: '#C5D4E4',
    200: '#9EB8D2',
    300: '#779BC0',
    400: '#5987B2',
    500: '#3B73A4',
    600: '#2F5C83',
    700: '#234562',
    800: '#172E41',
    900: '#0B1720',
  },

  // Neutral - Grises
  neutral: {
    50: '#FAFAFA',
    100: '#F5F5F5',
    200: '#E5E5E5',
    300: '#D4D4D4',
    400: '#A3A3A3',
    500: '#737373',
    600: '#525252',
    700: '#404040',
    800: '#262626',
    900: '#171717',
  },

  // Semantic Colors
  success: {
    light: '#D1FAE5',
    main: '#10B981',
    dark: '#047857',
  },
  warning: {
    light: '#FEF3C7',
    main: '#F59E0B',
    dark: '#B45309',
  },
  error: {
    light: '#FEE2E2',
    main: '#EF4444',
    dark: '#B91C1C',
  },
  info: {
    light: '#DBEAFE',
    main: '#3B82F6',
    dark: '#1D4ED8',
  },

  // Deal Rating Colors (Sistema de calificación de precios)
  deal: {
    great: '#00A870', // Verde OKLA - Excelente precio
    good: '#22C55E', // Verde claro - Buen precio
    fair: '#F59E0B', // Amarillo - Precio justo
    high: '#EF4444', // Rojo - Precio alto
    uncertain: '#9CA3AF', // Gris - Sin datos suficientes
  },

  // Background Colors
  background: {
    primary: '#FFFFFF',
    secondary: '#F9FAFB',
    tertiary: '#F3F4F6',
  },

  // Text Colors
  text: {
    primary: '#111827',
    secondary: '#4B5563',
    tertiary: '#9CA3AF',
    inverse: '#FFFFFF',
    link: '#00A870',
  },

  // Border Colors
  border: {
    light: '#E5E7EB',
    default: '#D1D5DB',
    dark: '#9CA3AF',
    focus: '#00A870',
  },
} as const;

/** Typography scale — font families, sizes, weights, and line heights. */
export const typography = {
  fontFamily: {
    sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
    mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
  },
  fontSize: {
    xs: ['0.75rem', { lineHeight: '1rem' }], // 12px
    sm: ['0.875rem', { lineHeight: '1.25rem' }], // 14px
    base: ['1rem', { lineHeight: '1.5rem' }], // 16px
    lg: ['1.125rem', { lineHeight: '1.75rem' }], // 18px
    xl: ['1.25rem', { lineHeight: '1.75rem' }], // 20px
    '2xl': ['1.5rem', { lineHeight: '2rem' }], // 24px
    '3xl': ['1.875rem', { lineHeight: '2.25rem' }], // 30px
    '4xl': ['2.25rem', { lineHeight: '2.5rem' }], // 36px
    '5xl': ['3rem', { lineHeight: '1' }], // 48px
    '6xl': ['3.75rem', { lineHeight: '1' }], // 60px
  },
  fontWeight: {
    normal: '400',
    medium: '500',
    semibold: '600',
    bold: '700',
  },
} as const;

/** Spacing scale (rem-based) for consistent padding, margins, and gaps. */
export const spacing = {
  0: '0',
  0.5: '0.125rem', // 2px
  1: '0.25rem', // 4px
  1.5: '0.375rem', // 6px
  2: '0.5rem', // 8px
  2.5: '0.625rem', // 10px
  3: '0.75rem', // 12px
  3.5: '0.875rem', // 14px
  4: '1rem', // 16px
  5: '1.25rem', // 20px
  6: '1.5rem', // 24px
  7: '1.75rem', // 28px
  8: '2rem', // 32px
  9: '2.25rem', // 36px
  10: '2.5rem', // 40px
  11: '2.75rem', // 44px
  12: '3rem', // 48px
  14: '3.5rem', // 56px
  16: '4rem', // 64px
  20: '5rem', // 80px
  24: '6rem', // 96px
  28: '7rem', // 112px
  32: '8rem', // 128px
} as const;

/** Border radius values for consistent rounded corners across the UI. */
export const borderRadius = {
  none: '0',
  sm: '0.125rem', // 2px
  default: '0.25rem', // 4px
  md: '0.375rem', // 6px
  lg: '0.5rem', // 8px
  xl: '0.75rem', // 12px
  '2xl': '1rem', // 16px
  '3xl': '1.5rem', // 24px
  full: '9999px',
} as const;

/** Shadow tokens for elevation — from subtle card shadows to prominent modals. */
export const shadows = {
  none: 'none',
  sm: '0 1px 2px 0 rgb(0 0 0 / 0.05)',
  default: '0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)',
  md: '0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)',
  lg: '0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)',
  xl: '0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)',
  '2xl': '0 25px 50px -12px rgb(0 0 0 / 0.25)',
  inner: 'inset 0 2px 4px 0 rgb(0 0 0 / 0.05)',
  // Custom shadows para cards de vehículos
  card: '0 2px 8px rgba(0, 0, 0, 0.08)',
  cardHover: '0 8px 24px rgba(0, 0, 0, 0.12)',
  cardActive: '0 4px 12px rgba(0, 168, 112, 0.15)',
} as const;

/** Transition presets for consistent animation timing and easing. */
export const transitions = {
  duration: {
    fast: '150ms',
    normal: '200ms',
    slow: '300ms',
    slower: '500ms',
  },
  timing: {
    default: 'cubic-bezier(0.4, 0, 0.2, 1)',
    linear: 'linear',
    in: 'cubic-bezier(0.4, 0, 1, 1)',
    out: 'cubic-bezier(0, 0, 0.2, 1)',
    inOut: 'cubic-bezier(0.4, 0, 0.2, 1)',
    bounce: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
  },
} as const;

/** Responsive breakpoints — matches Tailwind's default breakpoint system. */
export const breakpoints = {
  sm: '640px',
  md: '768px',
  lg: '1024px',
  xl: '1280px',
  '2xl': '1536px',
} as const;

/** Z-index scale for layering — dropdown < sticky < modal < toast. */
export const zIndex = {
  base: 0,
  dropdown: 1000,
  sticky: 1020,
  fixed: 1030,
  modalBackdrop: 1040,
  modal: 1050,
  popover: 1060,
  tooltip: 1070,
  toast: 1080,
} as const;

// Export all tokens as a single object for convenience
/** Combined design token object — import individual tokens for tree-shaking. */
export const designTokens = {
  colors,
  typography,
  spacing,
  borderRadius,
  shadows,
  transitions,
  breakpoints,
  zIndex,
} as const;

export type Colors = typeof colors;
export type Typography = typeof typography;
export type Spacing = typeof spacing;
export type BorderRadius = typeof borderRadius;
export type Shadows = typeof shadows;
export type Transitions = typeof transitions;
export type Breakpoints = typeof breakpoints;
export type ZIndex = typeof zIndex;
