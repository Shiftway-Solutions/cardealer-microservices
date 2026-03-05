# 🌐 Internacionalización (i18n) - Configuración

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Proyecto Next.js configurado
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Configurar internacionalización completa para OKLA:

- Español (es-DO) como idioma principal
- Inglés (en-US) como secundario
- Formatos de moneda (DOP/USD)
- Formatos de fecha/hora dominicanos
- Pluralización y género

---

## 🎯 REQUISITOS DE NEGOCIO

| Aspecto               | Configuración                   |
| --------------------- | ------------------------------- |
| **Idioma primario**   | Español (República Dominicana)  |
| **Idioma secundario** | Inglés (USA)                    |
| **Moneda principal**  | DOP (Peso Dominicano)           |
| **Moneda secundaria** | USD (para vehículos importados) |
| **Zona horaria**      | America/Santo_Domingo (AST)     |
| **Formato fecha**     | DD/MM/YYYY                      |
| **Formato número**    | 1,234,567.89                    |

---

## 🔧 PASO 1: Instalar Dependencias

```bash
npm install next-intl
npm install -D @formatjs/cli
```

---

## 🔧 PASO 2: Estructura de Archivos

```
src/
├── i18n/
│   ├── config.ts              # Configuración central
│   ├── request.ts             # Server-side locale
│   └── routing.ts             # Rutas localizadas
├── messages/
│   ├── es-DO.json             # Traducciones español
│   └── en-US.json             # Traducciones inglés
└── middleware.ts              # Detección de idioma
```

---

## 🔧 PASO 3: Configuración Central

```typescript
// filepath: src/i18n/config.ts
export const locales = ["es-DO", "en-US"] as const;
export type Locale = (typeof locales)[number];

export const defaultLocale: Locale = "es-DO";

export const localeNames: Record<Locale, string> = {
  "es-DO": "Español (RD)",
  "en-US": "English (US)",
};

export const localeFlags: Record<Locale, string> = {
  "es-DO": "🇩🇴",
  "en-US": "🇺🇸",
};

// Configuración de formatos por locale
export const localeFormats: Record<
  Locale,
  {
    currency: string;
    currencySymbol: string;
    dateFormat: string;
    numberFormat: Intl.NumberFormatOptions;
  }
> = {
  "es-DO": {
    currency: "DOP",
    currencySymbol: "RD$",
    dateFormat: "dd/MM/yyyy",
    numberFormat: {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    },
  },
  "en-US": {
    currency: "USD",
    currencySymbol: "$",
    dateFormat: "MM/dd/yyyy",
    numberFormat: {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    },
  },
};

// Timezone
export const timezone = "America/Santo_Domingo";
```

---

## 🔧 PASO 4: Configuración de Routing

```typescript
// filepath: src/i18n/routing.ts
import { defineRouting } from "next-intl/routing";
import { createNavigation } from "next-intl/navigation";
import { locales, defaultLocale } from "./config";

export const routing = defineRouting({
  locales,
  defaultLocale,
  localePrefix: "as-needed", // Solo mostrar prefijo para idiomas no-default
});

// Exportar helpers de navegación
export const { Link, redirect, usePathname, useRouter } =
  createNavigation(routing);
```

---

## 🔧 PASO 5: Request Handler (Server)

```typescript
// filepath: src/i18n/request.ts
import { getRequestConfig } from "next-intl/server";
import { routing } from "./routing";
import { Locale } from "./config";

export default getRequestConfig(async ({ requestLocale }) => {
  // Obtener locale del request
  let locale = await requestLocale;

  // Validar que sea un locale soportado
  if (!locale || !routing.locales.includes(locale as Locale)) {
    locale = routing.defaultLocale;
  }

  return {
    locale,
    messages: (await import(`@/messages/${locale}.json`)).default,
    timeZone: "America/Santo_Domingo",
    now: new Date(),
    formats: {
      dateTime: {
        short: {
          day: "numeric",
          month: "short",
          year: "numeric",
        },
        long: {
          day: "numeric",
          month: "long",
          year: "numeric",
          hour: "numeric",
          minute: "numeric",
        },
      },
      number: {
        currency: {
          style: "currency",
          currency: locale === "es-DO" ? "DOP" : "USD",
        },
        percent: {
          style: "percent",
          minimumFractionDigits: 1,
        },
      },
    },
  };
});
```

---

## 🔧 PASO 6: Middleware

```typescript
// filepath: src/middleware.ts
import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";

export default createMiddleware(routing);

export const config = {
  // Excluir rutas que no necesitan i18n
  matcher: [
    // Todas las rutas excepto:
    "/((?!api|_next|_vercel|.*\\..*).*)",
  ],
};
```

---

## 🔧 PASO 7: Archivos de Traducción

### Español (Principal)

```json
// filepath: src/messages/es-DO.json
{
  "common": {
    "loading": "Cargando...",
    "error": "Ha ocurrido un error",
    "retry": "Reintentar",
    "save": "Guardar",
    "cancel": "Cancelar",
    "delete": "Eliminar",
    "edit": "Editar",
    "search": "Buscar",
    "filter": "Filtrar",
    "sort": "Ordenar",
    "back": "Volver",
    "next": "Siguiente",
    "previous": "Anterior",
    "close": "Cerrar",
    "confirm": "Confirmar",
    "yes": "Sí",
    "no": "No"
  },
  "auth": {
    "login": "Iniciar Sesión",
    "logout": "Cerrar Sesión",
    "register": "Registrarse",
    "email": "Correo electrónico",
    "password": "Contraseña",
    "confirmPassword": "Confirmar contraseña",
    "forgotPassword": "¿Olvidaste tu contraseña?",
    "resetPassword": "Restablecer contraseña",
    "rememberMe": "Recordarme",
    "noAccount": "¿No tienes cuenta?",
    "hasAccount": "¿Ya tienes cuenta?",
    "loginWith": "Iniciar sesión con {provider}",
    "errors": {
      "invalidCredentials": "Correo o contraseña incorrectos",
      "emailRequired": "El correo es requerido",
      "passwordRequired": "La contraseña es requerida",
      "passwordMismatch": "Las contraseñas no coinciden"
    }
  },
  "navigation": {
    "home": "Inicio",
    "search": "Buscar",
    "sell": "Vender",
    "favorites": "Favoritos",
    "messages": "Mensajes",
    "profile": "Mi Perfil",
    "settings": "Configuración",
    "help": "Ayuda",
    "dealers": "Para Dealers"
  },
  "vehicles": {
    "title": "Vehículos",
    "new": "Nuevo",
    "used": "Usado",
    "certified": "Certificado",
    "make": "Marca",
    "model": "Modelo",
    "year": "Año",
    "price": "Precio",
    "mileage": "Kilometraje",
    "transmission": "Transmisión",
    "fuelType": "Combustible",
    "color": "Color",
    "condition": "Condición",
    "bodyType": "Tipo de carrocería",
    "features": "Características",
    "description": "Descripción",
    "location": "Ubicación",
    "publishedDate": "Publicado",
    "views": "{count, plural, =0 {Sin vistas} =1 {1 vista} other {# vistas}}",
    "contactSeller": "Contactar vendedor",
    "addToFavorites": "Agregar a favoritos",
    "removeFromFavorites": "Quitar de favoritos",
    "compare": "Comparar",
    "share": "Compartir",
    "report": "Reportar",
    "filters": {
      "priceRange": "Rango de precio",
      "yearRange": "Rango de año",
      "mileageMax": "Kilometraje máximo",
      "transmission": {
        "automatic": "Automático",
        "manual": "Manual",
        "cvt": "CVT"
      },
      "fuelType": {
        "gasoline": "Gasolina",
        "diesel": "Diésel",
        "electric": "Eléctrico",
        "hybrid": "Híbrido"
      }
    },
    "sort": {
      "newest": "Más recientes",
      "oldest": "Más antiguos",
      "priceLowHigh": "Precio: menor a mayor",
      "priceHighLow": "Precio: mayor a menor",
      "mileageLowHigh": "Kilometraje: menor a mayor",
      "yearNewest": "Año: más reciente"
    }
  },
  "dealer": {
    "title": "Panel de Dealer",
    "dashboard": "Dashboard",
    "inventory": "Inventario",
    "leads": "Leads",
    "analytics": "Estadísticas",
    "settings": "Configuración",
    "plans": {
      "starter": "Inicial",
      "pro": "Profesional",
      "enterprise": "Empresarial"
    },
    "stats": {
      "activeListings": "Publicaciones activas",
      "totalViews": "Vistas totales",
      "newLeads": "Leads nuevos",
      "conversionRate": "Tasa de conversión"
    }
  },
  "forms": {
    "required": "Este campo es requerido",
    "invalid": "Valor inválido",
    "minLength": "Mínimo {min} caracteres",
    "maxLength": "Máximo {max} caracteres",
    "email": "Correo electrónico inválido",
    "phone": "Teléfono inválido",
    "rnc": "RNC inválido"
  },
  "notifications": {
    "title": "Notificaciones",
    "empty": "No tienes notificaciones",
    "markAllRead": "Marcar todas como leídas",
    "types": {
      "newMessage": "Nuevo mensaje",
      "priceAlert": "Alerta de precio",
      "newLead": "Nuevo lead",
      "vehicleSold": "Vehículo vendido"
    }
  },
  "footer": {
    "about": "Sobre OKLA",
    "contact": "Contacto",
    "terms": "Términos y Condiciones",
    "privacy": "Política de Privacidad",
    "help": "Centro de Ayuda",
    "copyright": "© {year} OKLA. Todos los derechos reservados."
  },
  "currency": {
    "dop": "Pesos Dominicanos",
    "usd": "Dólares Americanos",
    "showIn": "Mostrar precios en"
  },
  "time": {
    "now": "Ahora",
    "minutesAgo": "Hace {count} {count, plural, =1 {minuto} other {minutos}}",
    "hoursAgo": "Hace {count} {count, plural, =1 {hora} other {horas}}",
    "daysAgo": "Hace {count} {count, plural, =1 {día} other {días}}",
    "weeksAgo": "Hace {count} {count, plural, =1 {semana} other {semanas}}"
  }
}
```

### Inglés (Secundario)

```json
// filepath: src/messages/en-US.json
{
  "common": {
    "loading": "Loading...",
    "error": "An error occurred",
    "retry": "Retry",
    "save": "Save",
    "cancel": "Cancel",
    "delete": "Delete",
    "edit": "Edit",
    "search": "Search",
    "filter": "Filter",
    "sort": "Sort",
    "back": "Back",
    "next": "Next",
    "previous": "Previous",
    "close": "Close",
    "confirm": "Confirm",
    "yes": "Yes",
    "no": "No"
  },
  "auth": {
    "login": "Login",
    "logout": "Logout",
    "register": "Register",
    "email": "Email",
    "password": "Password",
    "confirmPassword": "Confirm password",
    "forgotPassword": "Forgot your password?",
    "resetPassword": "Reset password",
    "rememberMe": "Remember me",
    "noAccount": "Don't have an account?",
    "hasAccount": "Already have an account?",
    "loginWith": "Login with {provider}",
    "errors": {
      "invalidCredentials": "Invalid email or password",
      "emailRequired": "Email is required",
      "passwordRequired": "Password is required",
      "passwordMismatch": "Passwords don't match"
    }
  },
  "navigation": {
    "home": "Home",
    "search": "Search",
    "sell": "Sell",
    "favorites": "Favorites",
    "messages": "Messages",
    "profile": "My Profile",
    "settings": "Settings",
    "help": "Help",
    "dealers": "For Dealers"
  },
  "vehicles": {
    "title": "Vehicles",
    "new": "New",
    "used": "Used",
    "certified": "Certified",
    "make": "Make",
    "model": "Model",
    "year": "Year",
    "price": "Price",
    "mileage": "Mileage",
    "transmission": "Transmission",
    "fuelType": "Fuel Type",
    "color": "Color",
    "condition": "Condition",
    "bodyType": "Body Type",
    "features": "Features",
    "description": "Description",
    "location": "Location",
    "publishedDate": "Published",
    "views": "{count, plural, =0 {No views} =1 {1 view} other {# views}}",
    "contactSeller": "Contact Seller",
    "addToFavorites": "Add to favorites",
    "removeFromFavorites": "Remove from favorites",
    "compare": "Compare",
    "share": "Share",
    "report": "Report",
    "filters": {
      "priceRange": "Price Range",
      "yearRange": "Year Range",
      "mileageMax": "Maximum Mileage",
      "transmission": {
        "automatic": "Automatic",
        "manual": "Manual",
        "cvt": "CVT"
      },
      "fuelType": {
        "gasoline": "Gasoline",
        "diesel": "Diesel",
        "electric": "Electric",
        "hybrid": "Hybrid"
      }
    },
    "sort": {
      "newest": "Newest First",
      "oldest": "Oldest First",
      "priceLowHigh": "Price: Low to High",
      "priceHighLow": "Price: High to Low",
      "mileageLowHigh": "Mileage: Low to High",
      "yearNewest": "Year: Newest"
    }
  },
  "dealer": {
    "title": "Dealer Portal",
    "dashboard": "Dashboard",
    "inventory": "Inventory",
    "leads": "Leads",
    "analytics": "Analytics",
    "settings": "Settings",
    "plans": {
      "starter": "Starter",
      "pro": "Professional",
      "enterprise": "Enterprise"
    },
    "stats": {
      "activeListings": "Active Listings",
      "totalViews": "Total Views",
      "newLeads": "New Leads",
      "conversionRate": "Conversion Rate"
    }
  },
  "forms": {
    "required": "This field is required",
    "invalid": "Invalid value",
    "minLength": "Minimum {min} characters",
    "maxLength": "Maximum {max} characters",
    "email": "Invalid email address",
    "phone": "Invalid phone number",
    "rnc": "Invalid RNC"
  },
  "notifications": {
    "title": "Notifications",
    "empty": "You have no notifications",
    "markAllRead": "Mark all as read",
    "types": {
      "newMessage": "New message",
      "priceAlert": "Price alert",
      "newLead": "New lead",
      "vehicleSold": "Vehicle sold"
    }
  },
  "footer": {
    "about": "About OKLA",
    "contact": "Contact",
    "terms": "Terms and Conditions",
    "privacy": "Privacy Policy",
    "help": "Help Center",
    "copyright": "© {year} OKLA. All rights reserved."
  },
  "currency": {
    "dop": "Dominican Pesos",
    "usd": "US Dollars",
    "showIn": "Show prices in"
  },
  "time": {
    "now": "Now",
    "minutesAgo": "{count} {count, plural, =1 {minute} other {minutes}} ago",
    "hoursAgo": "{count} {count, plural, =1 {hour} other {hours}} ago",
    "daysAgo": "{count} {count, plural, =1 {day} other {days}} ago",
    "weeksAgo": "{count} {count, plural, =1 {week} other {weeks}} ago"
  }
}
```

---

## 🔧 PASO 8: Hooks y Utilidades

### Hook useLocale

```typescript
// filepath: src/hooks/useLocale.ts
"use client";

import { useLocale as useNextIntlLocale, useTranslations } from "next-intl";
import { useRouter, usePathname } from "@/i18n/routing";
import { locales, localeNames, localeFlags, Locale } from "@/i18n/config";

export function useLocale() {
  const locale = useNextIntlLocale() as Locale;
  const router = useRouter();
  const pathname = usePathname();
  const t = useTranslations();

  const switchLocale = (newLocale: Locale) => {
    router.replace(pathname, { locale: newLocale });
  };

  return {
    locale,
    locales,
    localeName: localeNames[locale],
    localeFlag: localeFlags[locale],
    switchLocale,
    t,
  };
}
```

### Componente Language Switcher

```typescript
// filepath: src/components/ui/LanguageSwitcher.tsx
'use client';

import * as React from 'react';
import { Button } from '@/components/ui/Button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/DropdownMenu';
import { Globe } from 'lucide-react';
import { useLocale } from '@/hooks/useLocale';
import { locales, localeNames, localeFlags, Locale } from '@/i18n/config';

export function LanguageSwitcher() {
  const { locale, switchLocale, localeFlag } = useLocale();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-2">
          <span className="text-lg">{localeFlag}</span>
          <Globe className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {locales.map((loc) => (
          <DropdownMenuItem
            key={loc}
            onClick={() => switchLocale(loc)}
            className={locale === loc ? 'bg-primary-50' : ''}
          >
            <span className="mr-2 text-lg">{localeFlags[loc]}</span>
            {localeNames[loc]}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
```

---

## 🔧 PASO 9: Formateo de Moneda

```typescript
// filepath: src/lib/formatters/currency.ts
import { Locale, localeFormats } from "@/i18n/config";

export interface CurrencyFormatOptions {
  locale: Locale;
  currency?: "DOP" | "USD";
  showSymbol?: boolean;
  compact?: boolean;
}

export function formatCurrency(
  amount: number,
  options: CurrencyFormatOptions,
): string {
  const { locale, currency, showSymbol = true, compact = false } = options;
  const config = localeFormats[locale];
  const targetCurrency = currency || config.currency;

  const formatter = new Intl.NumberFormat(locale, {
    style: showSymbol ? "currency" : "decimal",
    currency: targetCurrency,
    notation: compact ? "compact" : "standard",
    minimumFractionDigits: compact ? 0 : 2,
    maximumFractionDigits: compact ? 1 : 2,
  });

  return formatter.format(amount);
}

// Convertir DOP a USD (tasa aproximada)
export function convertCurrency(
  amount: number,
  from: "DOP" | "USD",
  to: "DOP" | "USD",
  exchangeRate: number = 58.5, // Tasa ejemplo
): number {
  if (from === to) return amount;
  return from === "DOP" ? amount / exchangeRate : amount * exchangeRate;
}

// Hook para usar en componentes
export function useCurrencyFormatter() {
  const { locale } = useLocale();

  return {
    format: (amount: number, currency?: "DOP" | "USD") =>
      formatCurrency(amount, { locale, currency }),
    formatCompact: (amount: number, currency?: "DOP" | "USD") =>
      formatCurrency(amount, { locale, currency, compact: true }),
  };
}
```

### Componente PriceDisplay Internacionalizado

```typescript
// filepath: src/components/ui/PriceDisplay.tsx
'use client';

import * as React from 'react';
import { useCurrencyFormatter } from '@/lib/formatters/currency';
import { cn } from '@/lib/utils';

interface PriceDisplayProps {
  amount: number;
  currency?: 'DOP' | 'USD';
  originalAmount?: number; // Para mostrar precio tachado
  compact?: boolean;
  className?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

export function PriceDisplay({
  amount,
  currency = 'DOP',
  originalAmount,
  compact = false,
  className,
  size = 'md',
}: PriceDisplayProps) {
  const { format, formatCompact } = useCurrencyFormatter();
  const formatter = compact ? formatCompact : format;

  const sizeClasses = {
    sm: 'text-sm',
    md: 'text-base',
    lg: 'text-lg',
    xl: 'text-2xl',
  };

  return (
    <div className={cn('flex items-baseline gap-2', className)}>
      <span className={cn('font-bold text-gray-900', sizeClasses[size])}>
        {formatter(amount, currency)}
      </span>
      {originalAmount && originalAmount > amount && (
        <span className="text-sm text-gray-500 line-through">
          {formatter(originalAmount, currency)}
        </span>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 10: Formateo de Fechas

```typescript
// filepath: src/lib/formatters/date.ts
import { formatDistanceToNow, format, parseISO } from "date-fns";
import { es, enUS } from "date-fns/locale";
import { Locale, timezone } from "@/i18n/config";

const localeMap = {
  "es-DO": es,
  "en-US": enUS,
};

export function formatDate(
  date: string | Date,
  locale: Locale,
  formatStr: string = "dd/MM/yyyy",
): string {
  const dateObj = typeof date === "string" ? parseISO(date) : date;
  return format(dateObj, formatStr, { locale: localeMap[locale] });
}

export function formatRelativeTime(
  date: string | Date,
  locale: Locale,
): string {
  const dateObj = typeof date === "string" ? parseISO(date) : date;
  return formatDistanceToNow(dateObj, {
    addSuffix: true,
    locale: localeMap[locale],
  });
}

// Hook para usar en componentes
export function useDateFormatter() {
  const { locale } = useLocale();

  return {
    format: (date: string | Date, formatStr?: string) =>
      formatDate(date, locale, formatStr),
    relative: (date: string | Date) => formatRelativeTime(date, locale),
  };
}
```

---

## 🔧 PASO 11: Layout con Provider

```typescript
// filepath: src/app/[locale]/layout.tsx
import { NextIntlClientProvider } from 'next-intl';
import { getMessages, getTranslations } from 'next-intl/server';
import { notFound } from 'next/navigation';
import { routing } from '@/i18n/routing';
import { Locale } from '@/i18n/config';

interface LocaleLayoutProps {
  children: React.ReactNode;
  params: { locale: string };
}

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({ params: { locale } }: LocaleLayoutProps) {
  const t = await getTranslations({ locale, namespace: 'common' });

  return {
    title: 'OKLA - Marketplace de Vehículos',
    description: t('siteDescription'),
  };
}

export default async function LocaleLayout({
  children,
  params: { locale },
}: LocaleLayoutProps) {
  // Validar locale
  if (!routing.locales.includes(locale as Locale)) {
    notFound();
  }

  // Obtener mensajes
  const messages = await getMessages();

  return (
    <html lang={locale}>
      <body>
        <NextIntlClientProvider messages={messages}>
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
```

---

## 🧪 Testing

### Vitest

```typescript
// __tests__/i18n/formatters.test.ts
import { describe, it, expect } from "vitest";
import { formatCurrency, convertCurrency } from "@/lib/formatters/currency";
import { formatDate, formatRelativeTime } from "@/lib/formatters/date";

describe("Currency Formatter", () => {
  it("should format DOP correctly", () => {
    const result = formatCurrency(1500000, {
      locale: "es-DO",
      currency: "DOP",
    });
    expect(result).toContain("1,500,000");
    expect(result).toContain("RD$");
  });

  it("should format USD correctly", () => {
    const result = formatCurrency(25000, { locale: "en-US", currency: "USD" });
    expect(result).toContain("25,000");
    expect(result).toContain("$");
  });

  it("should convert DOP to USD", () => {
    const result = convertCurrency(585000, "DOP", "USD", 58.5);
    expect(result).toBe(10000);
  });
});

describe("Date Formatter", () => {
  it("should format date in Spanish", () => {
    const result = formatDate("2026-01-15", "es-DO");
    expect(result).toBe("15/01/2026");
  });

  it("should format date in English", () => {
    const result = formatDate("2026-01-15", "en-US", "MM/dd/yyyy");
    expect(result).toBe("01/15/2026");
  });
});
```

### Playwright E2E

```typescript
// e2e/i18n.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Internationalization", () => {
  test("should display content in Spanish by default", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("link", { name: "Buscar" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Vender" })).toBeVisible();
  });

  test("should switch to English", async ({ page }) => {
    await page.goto("/");
    await page.getByRole("button", { name: /globe/i }).click();
    await page.getByText("English (US)").click();

    await expect(page).toHaveURL(/\/en-US/);
    await expect(page.getByRole("link", { name: "Search" })).toBeVisible();
  });

  test("should display prices in DOP", async ({ page }) => {
    await page.goto("/vehiculos");
    const priceElement = page.locator('[data-testid="vehicle-price"]').first();
    await expect(priceElement).toContainText("RD$");
  });

  test("should format dates in Spanish", async ({ page }) => {
    await page.goto("/vehiculos/toyota-camry-2024");
    const dateElement = page.locator('[data-testid="published-date"]');
    // Formato: "Hace X días" o fecha en DD/MM/YYYY
    await expect(dateElement).toContainText(/Hace|\/\d{2}\/\d{4}/);
  });
});
```

---

## ✅ Checklist de Implementación

- [ ] Instalar next-intl
- [ ] Crear estructura de archivos i18n
- [ ] Configurar middleware
- [ ] Crear archivos de traducción (es-DO, en-US)
- [ ] Implementar Language Switcher
- [ ] Configurar formateo de moneda
- [ ] Configurar formateo de fechas
- [ ] Crear layout con provider
- [ ] Escribir tests unitarios
- [ ] Escribir tests E2E

---

## 🔗 Referencias

- [next-intl Documentation](https://next-intl-docs.vercel.app/)
- [ICU Message Format](https://unicode-org.github.io/icu/userguide/format_parse/messages/)
- [date-fns](https://date-fns.org/)

---

_La internacionalización es crítica para mercados como RD donde los usuarios esperan contenido en español con formatos locales._
