---
title: "77 - Páginas Legales y Comunes (Terms, Privacy, About, Contact)"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 77 - Páginas Legales y Comunes (Terms, Privacy, About, Contact)

> **Módulo**: Legal & Common Pages  
> **Ubicación**: `frontend/web/src/pages/common/`  
> **Última actualización**: Enero 2026

---

## 📐 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    COMMON / LEGAL PAGES                                 │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                      TermsPage                                  │    │
│  │  /terms (Terms of Service)                                      │    │
│  │                                                                 │    │
│  │  Sections:                                                      │    │
│  │  1. Introduction                                                │    │
│  │  2. Account Registration                                        │    │
│  │  3. User Conduct                                                │    │
│  │  4. Vehicle Listings                                            │    │
│  │  5. Transactions                                                │    │
│  │  6. Fees and Payments                                           │    │
│  │  7. Intellectual Property                                       │    │
│  │  8. Disclaimers and Limitations                                 │    │
│  │  9. Termination                                                 │    │
│  │  10. Governing Law                                              │    │
│  │  11. Contact Information                                        │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                      PrivacyPage                                │    │
│  │  /privacy (Privacy Policy)                                      │    │
│  │                                                                 │    │
│  │  Sections:                                                      │    │
│  │  1. Introduction                                                │    │
│  │  2. Information We Collect                                      │    │
│  │     2.1 Information You Provide                                 │    │
│  │     2.2 Information Collected Automatically                     │    │
│  │     2.3 Information from Third Parties                          │    │
│  │  3. How We Use Your Information                                 │    │
│  │  4. How We Share Your Information                               │    │
│  │     4.1 With Other Users                                        │    │
│  │     4.2 With Service Providers                                  │    │
│  │  5. Your Rights (ARCO - Ley 172-13)                             │    │
│  │  6. Data Retention                                              │    │
│  │  7. Security                                                    │    │
│  │  8. Contact Information                                         │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                      CookiesPage                                │    │
│  │  /cookies (Cookie Policy)                                       │    │
│  │                                                                 │    │
│  │  ┌───────────────────────────────────────────────────────────┐ │    │
│  │  │ Cookie Preferences Panel                                   │ │    │
│  │  │                                                            │ │    │
│  │  │ 🔒 Necessary Cookies      [ON - disabled]                  │ │    │
│  │  │    Authentication, security, session                       │ │    │
│  │  │                                                            │ │    │
│  │  │ ⚙️ Functional Cookies     [ON/OFF toggle]                  │ │    │
│  │  │    Language, preferences, saved searches                   │ │    │
│  │  │                                                            │ │    │
│  │  │ 📊 Analytics Cookies      [ON/OFF toggle]                  │ │    │
│  │  │    Usage analytics, performance                            │ │    │
│  │  │                                                            │ │    │
│  │  │ 📢 Advertising Cookies    [ON/OFF toggle]                  │ │    │
│  │  │    Targeted ads, marketing                                 │ │    │
│  │  │                                                            │ │    │
│  │  │              [💾 Save Preferences]                         │ │    │
│  │  └───────────────────────────────────────────────────────────┘ │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                       AboutPage                                 │    │
│  │  /about (About Us)                                              │    │
│  │                                                                 │    │
│  │  Sections:                                                      │    │
│  │  - Hero with gradient background                                │    │
│  │  - Mission Statement                                            │    │
│  │  - Values (Trust, Quality, Community, Service)                  │    │
│  │  - Stats (15,000+ Vehicles, 8,500+ Customers, etc.)             │    │
│  │  - Team Section                                                 │    │
│  │  - CTA (Get Started)                                            │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                      ContactPage                                │    │
│  │  /contact (Contact Us)                                          │    │
│  │                                                                 │    │
│  │  ┌─────────────────────────┐  ┌────────────────────────────┐   │    │
│  │  │ Contact Form            │  │ Contact Info               │   │    │
│  │  │ Name: [___________]     │  │ 📧 info@cardealer.com.do   │   │    │
│  │  │ Email: [__________]     │  │ 📞 +1 (809) 555-0123       │   │    │
│  │  │ Subject: [________]     │  │ 📍 Santo Domingo, RD       │   │    │
│  │  │ Message: [________]     │  │ 🕐 Mon-Fri 9AM-6PM         │   │    │
│  │  │         [________]      │  │                            │   │    │
│  │  │  [📤 Send Message]      │  │ Social: FB TW IG LI        │   │    │
│  │  └─────────────────────────┘  └────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                      PricingPage                                │    │
│  │  /pricing (Plans & Pricing)                                     │    │
│  │                                                                 │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │    │
│  │  │   BASIC     │  │  PREMIUM ⭐  │  │   DEALER    │             │    │
│  │  │   FREE      │  │  $49/listing │  │  $199/month │             │    │
│  │  │             │  │  (Popular)   │  │             │             │    │
│  │  │ ✓ 1 listing │  │ ✓ 5 listings │  │ ✓ Unlimited │             │    │
│  │  │ ✓ 30 days   │  │ ✓ 60 days    │  │ ✓ 90 days   │             │    │
│  │  │ ✓ 5 photos  │  │ ✓ 20 photos  │  │ ✓ Unlimited │             │    │
│  │  │ ✗ Featured  │  │ ✓ Featured   │  │ ✓ API access│             │    │
│  │  │             │  │ ✓ Analytics  │  │ ✓ Branding  │             │    │
│  │  │[Get Started]│  │[Start Prem.]│  │[Contact]    │             │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘             │    │
│  │                                                                 │    │
│  │  FAQ Section (expandable)                                       │    │
│  └────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Tipos TypeScript

### CookiesPage Types

```typescript
// Cookie preferences state
interface CookiePreferences {
  necessary: boolean; // Always true, disabled toggle
  functional: boolean; // Language, preferences
  analytics: boolean; // Usage analytics
  advertising: boolean; // Targeted ads
}

// Default preferences
const defaultPreferences: CookiePreferences = {
  necessary: true,
  functional: true,
  analytics: true,
  advertising: false,
};
```

### ContactPage Types

```typescript
// Zod schema for contact form
const contactSchema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters"),
  email: z.string().email("Invalid email address"),
  subject: z.string().min(5, "Subject must be at least 5 characters"),
  message: z.string().min(20, "Message must be at least 20 characters"),
});

type ContactFormData = z.infer<typeof contactSchema>;
```

### PricingPage Types

```typescript
interface PricingPlan {
  name: string;
  price: number;
  period: string;           // 'Free', 'per listing', 'per month'
  description: string;
  features: string[];       // ✓ Included features
  notIncluded: string[];    // ✗ Not included
  cta: string;              // Button text
  popular: boolean;         // Show "Popular" badge
}

const plans: PricingPlan[] = [
  {
    name: 'Basic',
    price: 0,
    period: 'Free',
    description: 'Perfect for individual sellers',
    features: ['1 active listing', '30 days duration', 'Up to 5 photos', ...],
    notIncluded: ['Featured badge', 'Priority placement', ...],
    cta: 'Get Started',
    popular: false,
  },
  // ... Premium, Dealer plans
];
```

---

## 🧩 Componentes Principales

### TermsPage

```
frontend/web/src/pages/common/TermsPage.tsx (223 líneas)
│
├── Layout: MainLayout
│
├── Hero Section
│   ├── Gradient background (primary → secondary)
│   ├── Title: "Terms of Service"
│   └── Last updated date (dynamic)
│
├── Content (prose styling)
│   ├── 1. Introduction
│   ├── 2. Account Registration
│   │   └── Requirements, age 18+, responsibilities
│   ├── 3. User Conduct
│   │   └── Prohibited activities list
│   ├── 4. Vehicle Listings
│   │   └── Seller obligations, accuracy, legality
│   ├── 5. Transactions
│   │   └── Platform is intermediary, not party
│   ├── 6. Fees and Payments
│   ├── 7. Intellectual Property
│   ├── 8. Disclaimers and Limitations
│   ├── 9. Termination
│   ├── 10. Governing Law (República Dominicana)
│   └── 11. Contact Information
│
└── Footer links to related pages
```

### PrivacyPage

```
frontend/web/src/pages/common/PrivacyPage.tsx (269 líneas)
│
├── Layout: MainLayout
│
├── Hero Section
│   ├── Gradient background
│   ├── Title: "Privacy Policy"
│   └── Last updated date
│
├── Content (prose styling)
│   ├── 1. Introduction
│   ├── 2. Information We Collect
│   │   ├── 2.1 Information You Provide
│   │   │   └── Account, profile, listings, messages, payments
│   │   ├── 2.2 Information Collected Automatically
│   │   │   └── Device, usage, location, cookies
│   │   └── 2.3 Information from Third Parties
│   │       └── Social login, verification, payment processors
│   ├── 3. How We Use Your Information
│   │   └── Service provision, communications, security, analytics
│   ├── 4. How We Share Your Information
│   │   ├── 4.1 With Other Users
│   │   └── 4.2 With Service Providers
│   ├── 5. Your Rights (ARCO - Ley 172-13 RD)
│   │   ├── Acceso - Right to access your data
│   │   ├── Rectificación - Right to correct data
│   │   ├── Cancelación - Right to delete data
│   │   └── Oposición - Right to object to processing
│   ├── 6. Data Retention
│   ├── 7. Security
│   └── 8. Contact Information
│
└── Link to Privacy Center (/privacy-center)
```

### CookiesPage

```
frontend/web/src/pages/common/CookiesPage.tsx (349 líneas)
│
├── Layout: MainLayout
│
├── State
│   └── cookiePreferences (CookiePreferences)
│
├── Handlers
│   ├── handleToggle(category) - Toggle preference
│   └── handleSavePreferences() - Save to localStorage
│
├── Hero Section
│   ├── Gradient background
│   ├── Title: "Cookie Policy"
│   └── Last updated date
│
├── Content
│   ├── 1. What Are Cookies?
│   ├── 2. Types of Cookies We Use
│   │   ├── 2.1 Necessary Cookies
│   │   │   └── Auth, security, session, CSRF
│   │   ├── 2.2 Functional Cookies
│   │   │   └── Language, preferences, saved searches
│   │   ├── 2.3 Analytics Cookies
│   │   │   └── Usage, performance, Google Analytics
│   │   └── 2.4 Advertising Cookies
│   │       └── Targeted ads, remarketing
│   ├── 3. How to Manage Cookies
│   ├── 4. Third-Party Cookies
│   └── 5. Contact Information
│
└── Cookie Preferences Panel
    ├── Toggle for each category
    ├── Necessary = always ON (disabled)
    └── Save Preferences button
```

### AboutPage

```
frontend/web/src/pages/common/AboutPage.tsx (168 líneas)
│
├── Layout: MainLayout
│
├── Hero Section
│   ├── Gradient background
│   ├── Title: "About CarDealer"
│   └── Tagline
│
├── Mission Section
│   └── Vision and mission statement
│
├── Values Section (4 cards)
│   ├── 🛡️ Trust - Verified listings
│   ├── ✓ Quality - High standards
│   ├── 👥 Community - Relationships
│   └── ❤️ Service - Dedicated support
│
├── Stats Section (4 stats)
│   ├── 15,000+ Vehicles Listed
│   ├── 8,500+ Happy Customers
│   ├── 500+ Verified Dealers
│   └── 98% Satisfaction Rate
│
├── Team Section (optional)
│
└── CTA Section
    └── Link to /sell or /register
```

### ContactPage

```
frontend/web/src/pages/common/ContactPage.tsx (281 líneas)
│
├── Layout: MainLayout
│
├── State
│   ├── isSubmitting
│   └── isSuccess
│
├── Form (react-hook-form + Zod)
│   ├── Name (min 2 chars)
│   ├── Email (valid email)
│   ├── Subject (min 5 chars)
│   └── Message (min 20 chars)
│
├── Hero Section
│   ├── Gradient background
│   ├── Title: "Contact Us"
│   └── Subtitle
│
├── 2-Column Layout
│   ├── Left (2/3) - Contact Form
│   │   ├── Success message (if submitted)
│   │   └── Form fields with validation
│   │
│   └── Right (1/3) - Contact Info
│       ├── 📧 Email: info@cardealer.com.do
│       ├── 📞 Phone: +1 (809) 555-0123
│       ├── 📍 Address: Santo Domingo, RD
│       ├── 🕐 Hours: Mon-Fri 9AM-6PM
│       └── Social Links (FB, TW, IG, LI)
│
└── Map Section (optional)
```

### PricingPage

```
frontend/web/src/pages/common/PricingPage.tsx (320 líneas)
│
├── Layout: MainLayout
│
├── Hero Section
│   ├── Gradient background
│   ├── Title: "Simple, Transparent Pricing"
│   └── Subtitle
│
├── Pricing Cards (3 plans)
│   ├── Basic (Free)
│   │   ├── Price: $0
│   │   ├── Period: Free
│   │   ├── Features: 1 listing, 30 days, 5 photos
│   │   └── CTA: Get Started → /register
│   │
│   ├── Premium ($49/listing) ⭐ Popular
│   │   ├── Price: $49
│   │   ├── Period: per listing
│   │   ├── Features: 5 listings, 60 days, 20 photos, featured
│   │   └── CTA: Start Premium → /checkout?plan=premium
│   │
│   └── Dealer ($199/month)
│       ├── Price: $199
│       ├── Period: per month
│       ├── Features: Unlimited, 90 days, API, branding
│       └── CTA: Contact Sales → /contact
│
├── Feature Comparison Table (expandable)
│
└── FAQ Section
    ├── Can I upgrade later?
    ├── What payment methods?
    ├── Is there a trial?
    └── Refund policy?
```

---

## 🛣️ Rutas

```typescript
// App.tsx
<Route path="/terms" element={<TermsPage />} />
<Route path="/privacy" element={<PrivacyPage />} />
<Route path="/cookies" element={<CookiesPage />} />
<Route path="/about" element={<AboutPage />} />
<Route path="/contact" element={<ContactPage />} />
<Route path="/pricing" element={<PricingPage />} />
```

---

## 🌐 API Services

### ContactPage - Submit Form

```typescript
// services/contactService.ts

interface ContactFormDto {
  name: string;
  email: string;
  subject: string;
  message: string;
}

// POST /api/contact
export const submitContactForm = async (
  data: ContactFormDto,
): Promise<void> => {
  await api.post("/contact", data);
};
```

### CookiesPage - Save Preferences

```typescript
// utils/cookieConsent.ts

const COOKIE_CONSENT_KEY = "cookie-consent";

export const getCookiePreferences = (): CookiePreferences => {
  const saved = localStorage.getItem(COOKIE_CONSENT_KEY);
  if (saved) {
    return JSON.parse(saved);
  }
  return defaultPreferences;
};

export const saveCookiePreferences = (prefs: CookiePreferences): void => {
  localStorage.setItem(COOKIE_CONSENT_KEY, JSON.stringify(prefs));

  // Apply preferences
  if (!prefs.analytics) {
    // Disable Google Analytics
    window["ga-disable-GA_MEASUREMENT_ID"] = true;
  }

  if (!prefs.advertising) {
    // Disable ad tracking
  }
};
```

---

## 📦 Dependencias

```json
{
  "react-hook-form": "^7.x",
  "@hookform/resolvers": "^3.x",
  "zod": "^3.x",
  "react-icons": "^4.x"
}
```

---

## 🌍 Internacionalización

```json
// locales/es/legal.json
{
  "terms": {
    "title": "Términos de Servicio",
    "lastUpdated": "Última actualización"
  },
  "privacy": {
    "title": "Política de Privacidad",
    "arcoRights": "Derechos ARCO (Ley 172-13)",
    "access": "Acceso - Derecho a acceder a tus datos",
    "rectification": "Rectificación - Derecho a corregir datos",
    "cancellation": "Cancelación - Derecho a eliminar datos",
    "opposition": "Oposición - Derecho a oponerte al procesamiento"
  },
  "cookies": {
    "title": "Política de Cookies",
    "necessary": "Cookies Necesarias",
    "functional": "Cookies Funcionales",
    "analytics": "Cookies de Analítica",
    "advertising": "Cookies Publicitarias",
    "savePreferences": "Guardar Preferencias"
  },
  "about": {
    "title": "Acerca de Nosotros",
    "mission": "Nuestra Misión",
    "values": "Nuestros Valores",
    "trust": "Confianza",
    "quality": "Calidad",
    "community": "Comunidad",
    "service": "Servicio"
  },
  "contact": {
    "title": "Contáctanos",
    "sendMessage": "Enviar Mensaje",
    "success": "¡Gracias! Te responderemos en 24 horas.",
    "email": "Correo Electrónico",
    "phone": "Teléfono",
    "address": "Dirección",
    "hours": "Horario de Atención"
  },
  "pricing": {
    "title": "Planes y Precios",
    "basic": "Básico",
    "premium": "Premium",
    "dealer": "Dealer",
    "popular": "Popular",
    "getStarted": "Comenzar",
    "contactSales": "Contactar Ventas"
  }
}
```

---

## 🔗 Footer Links

Estas páginas se enlazan desde el Footer de la aplicación:

```tsx
// components/organisms/Footer.tsx

const legalLinks = [
  { href: "/terms", label: "Terms of Service" },
  { href: "/privacy", label: "Privacy Policy" },
  { href: "/cookies", label: "Cookie Policy" },
];

const companyLinks = [
  { href: "/about", label: "About Us" },
  { href: "/contact", label: "Contact" },
  { href: "/pricing", label: "Pricing" },
  { href: "/help", label: "Help Center" },
];
```

---

## ✅ Checklist de Validación

### TermsPage

- [ ] Hero con gradient y fecha dinámica
- [ ] 11 secciones legales completas
- [ ] Prose styling para legibilidad
- [ ] Links a páginas relacionadas
- [ ] Responsive design

### PrivacyPage

- [ ] Todas las secciones de privacidad
- [ ] Derechos ARCO (Ley 172-13 RD)
- [ ] Link a Privacy Center
- [ ] Información de contacto

### CookiesPage

- [ ] Panel de preferencias funcional
- [ ] Toggle para cada categoría
- [ ] Necessary cookies siempre ON
- [ ] Save preferences guarda en localStorage
- [ ] Explicación de cada tipo de cookie

### AboutPage

- [ ] Hero atractivo
- [ ] Mission statement claro
- [ ] 4 valores con iconos
- [ ] Estadísticas de impacto
- [ ] CTA al final

### ContactPage

- [ ] Formulario con validación Zod
- [ ] Success message después de envío
- [ ] Info de contacto visible
- [ ] Social links funcionan
- [ ] Error handling

### PricingPage

- [ ] 3 planes claramente diferenciados
- [ ] Popular badge en Premium
- [ ] Features vs Not included
- [ ] CTAs llevan a rutas correctas
- [ ] FAQ expandible

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";

test.describe("Legal & Common Pages", () => {
  test("debe mostrar página de términos y condiciones", async ({ page }) => {
    await page.goto("/terms");
    await expect(page.getByTestId("terms-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /términos y condiciones/i }),
    ).toBeVisible();
    await expect(page.getByTestId("terms-section").first()).toBeVisible();
    await expect(page.getByTestId("toc-navigation")).toBeVisible();
  });

  test("debe mostrar página de política de privacidad", async ({ page }) => {
    await page.goto("/privacy");
    await expect(page.getByTestId("privacy-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /política de privacidad/i }),
    ).toBeVisible();
    await expect(page.getByTestId("privacy-section").first()).toBeVisible();
  });

  test("debe navegar entre secciones de términos con TOC", async ({ page }) => {
    await page.goto("/terms");
    await page.getByTestId("toc-link").first().click();
    await expect(page.locator(":target")).toBeVisible();
  });

  test("debe mostrar página About Us con info de empresa", async ({ page }) => {
    await page.goto("/about");
    await expect(page.getByTestId("about-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /sobre okla/i }),
    ).toBeVisible();
    await expect(page.getByTestId("company-info")).toBeVisible();
    await expect(page.getByTestId("team-section")).toBeVisible();
  });

  test("debe mostrar página de contacto con formulario", async ({ page }) => {
    await page.goto("/contact");
    await expect(page.getByTestId("contact-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /contáctanos/i }),
    ).toBeVisible();
    await expect(page.getByTestId("contact-form")).toBeVisible();
    await expect(page.getByTestId("contact-info")).toBeVisible();
  });

  test("debe enviar formulario de contacto correctamente", async ({ page }) => {
    await page.goto("/contact");
    await page.getByTestId("contact-name").fill("Juan Pérez");
    await page.getByTestId("contact-email").fill("juan@example.com");
    await page.getByTestId("contact-subject").fill("Consulta general");
    await page
      .getByTestId("contact-message")
      .fill("Mensaje de prueba para el equipo de OKLA.");
    await page.getByRole("button", { name: /enviar mensaje/i }).click();
    await expect(
      page.getByText(/mensaje enviado correctamente/i),
    ).toBeVisible();
  });

  test("debe mostrar página de pricing con planes", async ({ page }) => {
    await page.goto("/pricing");
    await expect(page.getByTestId("pricing-page")).toBeVisible();
    await expect(page.getByTestId("plan-starter")).toBeVisible();
    await expect(page.getByTestId("plan-premium")).toBeVisible();
    await expect(page.getByTestId("plan-enterprise")).toBeVisible();
    await expect(page.getByTestId("popular-badge")).toBeVisible();
  });

  test("debe mostrar FAQ expandible en pricing", async ({ page }) => {
    await page.goto("/pricing");
    await expect(page.getByTestId("faq-section")).toBeVisible();
    await page.getByTestId("faq-item").first().click();
    await expect(page.getByTestId("faq-answer").first()).toBeVisible();
  });
});
```

---

## 📚 Documentación Relacionada

- [70-user-security-privacy.md](../02-AUTH/06-user-security-privacy.md) - Privacy Center (ARCO)
- [73-common-static-pages.md](../09-COMPONENTES-COMUNES/03-static-pages.md) - FAQ, How It Works
- [68-common-components.md](../09-COMPONENTES-COMUNES/01-common-components.md) - Footer component
