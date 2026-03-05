---
title: "29. Dealer Onboarding Completo"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["UserService", "BillingService"]
status: complete
last_updated: "2026-01-30"
---

# 29. Dealer Onboarding Completo

> **Objetivo:** Implementar flujo completo de onboarding paso a paso para dealers, desde landing page hasta activación final con verificación KYC, setup de pagos y configuración inicial.  
> **Tiempo estimado:** 4-5 horas  
> **Prioridad:** P1 (Crítico - Conversión de dealers)  
> **Complejidad:** 🔴 Alta (Multi-step wizard, KYC, Payments, Email verification)  
> **Dependencias:** UserService (DealerOnboardingV2Controller), BillingService (Subscriptions, EarlyBird), KYCService

---

## ✅ INTEGRACIÓN CON ONBOARDING Y REFERIDOS

Este documento complementa:

- [process-matrix/17-ENGAGEMENT-RETENCION/03-onboarding-comprador.md](../../process-matrix/17-ENGAGEMENT-RETENCION/03-onboarding-comprador.md) - **Onboarding** ⭐
- [process-matrix/17-ENGAGEMENT-RETENCION/02-programa-referidos.md](../../process-matrix/17-ENGAGEMENT-RETENCION/02-programa-referidos.md) - **Referidos** ⭐

**Estado:** 🟡 UserOnboarding 30% BE + 40% UI | 🔴 ReferralService 0%

### Servicios de Activación

| Servicio        | Puerto | Función                    | Estado             |
| --------------- | ------ | -------------------------- | ------------------ |
| UserService     | 5004   | Onboarding extendido       | 🟡 30% BE + 40% UI |
| ReferralService | 5088   | Programa de referidos      | 🔴 0% (Fase 2)     |
| BillingService  | 5010   | Subscriptions + Early Bird | ✅ 100%            |
| KYCService      | 5025   | Verificación KYC/AML       | ✅ 100%            |

### Onboarding de Compradores (Paralelo a Dealers)

**Objetivo:** Activar compradores nuevos con preferencias personalizadas

**Endpoints:**

```typescript
GET / api / users / onboarding / status; // Estado del onboarding
POST / api / users / onboarding / preferences; // Guardar preferencias
POST / api / users / onboarding / step / { step } / complete; // Marcar paso completado
POST / api / users / onboarding / skip; // Saltar onboarding
GET / api / users / onboarding / recommendations; // Recomendaciones personalizadas
```

**Wizard de Comprador (6 pasos):**

1. **Bienvenida** - Video tour 30 seg
2. **Intent** - ¿Qué buscas? (JustBrowsing, BuyingSoon, BuyingNow)
3. **Presupuesto** - Slider de rango de precio
4. **Marcas** - Seleccionar 1-5 marcas favoritas
5. **Tipo de vehículo** - Sedan, SUV, Pickup, etc.
6. **Ubicación** - Ciudad/provincia

**Resultado:**

- Crear alerta de búsqueda automática (AlertService)
- Mostrar recomendaciones personalizadas
- Email de bienvenida con primeros pasos

### 🎁 Programa de Referidos (Planificado Fase 2)

**Estado:** 🔴 0% - Growth hack para adquisición orgánica

**Estructura de Recompensas:**

| Usuario       | Acción del Referido | Recompensa Referidor | Recompensa Referido |
| ------------- | ------------------- | -------------------- | ------------------- |
| **Comprador** | Primera compra      | RD$ 2,500 crédito    | RD$ 1,000 crédito   |
| **Vendedor**  | Primera venta       | RD$ 1,500 crédito    | RD$ 500 descuento   |
| **Dealer**    | Suscripción Pro     | 2 meses gratis       | 1 mes gratis        |

**Endpoints planificados:**

```typescript
GET / api / referrals / code; // Mi código de referido
GET / api / referrals / link; // Mi link de referido
GET / api / referrals / stats; // Mis estadísticas
POST / api / referrals / apply; // Aplicar código
GET / api / referrals / rewards; // Mis recompensas
POST / api / referrals / rewards / { id } / redeem; // Canjear recompensa
```

**Código de Referido:**

- Format: `JUAN-OKLA-2026`
- Short link: `okla.do/r/JUAN`
- Full link: `https://okla.com.do/ref?code=JUAN-OKLA-2026`

**Tracking:**

- Attribution: Source, Campaign, Landing Page
- Status: Pending → Qualified → Rewarded
- Qualify: 30 días para completar acción
- Reward expiry: 90 días desde otorgamiento

**UI Faltante (Fase 2):**

- `/refer` - Programa de referidos
- `/refer/dashboard` - Mis referidos y ganancias
- `/settings/referral-code` - Mi código

### Entidades de Onboarding

```csharp
// UserService/Domain/Entities/
public class UserOnboarding
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public OnboardingStatus Status { get; set; }        // NotStarted, InProgress, Completed, Skipped
    public int CurrentStep { get; set; }
    public decimal CompletionPercent { get; set; }
    public List<OnboardingStep> Steps { get; set; }
    public UserPreferences Preferences { get; set; }
}

public class UserPreferences
{
    public UserIntent Intent { get; set; }              // JustBrowsing, BuyingSoon, BuyingNow
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public List<string> PreferredMakes { get; set; }
    public List<string> PreferredBodyTypes { get; set; }
    public bool InterestedInFinancing { get; set; }
}

// ReferralService/Domain/Entities/ (Planificado)
public class ReferralCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Code { get; set; }                    // JUAN-OKLA-2026
    public string ShortLink { get; set; }               // okla.do/r/JUAN
    public int TotalReferrals { get; set; }
    public decimal TotalEarnings { get; set; }
}

public class Referral
{
    public Guid ReferrerId { get; set; }
    public Guid ReferredUserId { get; set; }
    public ReferralStatus Status { get; set; }          // Pending, Qualified, Rewarded, Expired
    public string QualifyingAction { get; set; }        // FIRST_PURCHASE, SUBSCRIPTION
}
```

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura del Flujo](#arquitectura-del-flujo)
2. [Backend API](#backend-api)
3. [Landing y Pricing](#landing-y-pricing)
4. [Wizard de Onboarding](#wizard-de-onboarding)
5. [Verificación y Activación](#verificación-y-activación)
6. [Admin Approval](#admin-approval)
7. [Hooks y Servicios](#hooks-y-servicios)
8. [Tipos TypeScript](#tipos-typescript)
9. [Validación](#validación)

---

## 🏗️ ARQUITECTURA DEL FLUJO

### Complete Onboarding Journey

```
┌────────────────────────────────────────────────────────────────────────────┐
│                      DEALER ONBOARDING FLOW                                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  🌐 PASO 1: LANDING PAGE (ONBOARD-001)                                     │
│  /dealer/landing                                                           │
│  ├─ Hero section: "Aumenta tus ventas 3x"                                 │
│  ├─ Beneficios: Badge verificado, múltiples sucursales, estadísticas      │
│  ├─ Testimonios de dealers exitosos                                       │
│  ├─ Stats: 10K+ visitantes, 500+ vehículos, 50+ dealers                   │
│  └─ CTA: "Ver Planes y Precios" → /dealer/pricing                         │
│                                                                             │
│  💳 PASO 2: PRICING PAGE (ONBOARD-002.1)                                   │
│  /dealer/pricing                                                           │
│  ├─ Early Bird Banner: 3 MESES GRATIS + 20% OFF + Badge Fundador          │
│  ├─ 3 planes:                                                              │
│  │   • Starter: $49/mes → $39 (15 vehículos)                              │
│  │   • Pro: $129/mes → $103 (50 vehículos) ⭐ RECOMENDADO                 │
│  │   • Enterprise: $299/mes → $239 (ILIMITADO)                            │
│  ├─ Comparación de features                                               │
│  ├─ FAQ (5 preguntas)                                                      │
│  └─ CTA: "Empezar Ahora" → /dealer/onboarding/v2?plan=Pro                 │
│                                                                             │
│  📝 PASO 3: WIZARD MULTI-STEP (ONBOARD-002.2)                              │
│  /dealer/onboarding/v2                                                     │
│  │                                                                          │
│  ├─ Step 1/5: Información del Negocio                                     │
│  │   POST /api/dealer-onboarding/register                                 │
│  │   • Business Name                                                       │
│  │   • RNC (9-11 dígitos)                                                  │
│  │   • Legal Name                                                          │
│  │   • Dealer Type (Independent, Chain, Multiple, Franchise)              │
│  │   • Address, City, Province                                            │
│  │   → Crea Dealer con Status = Pending                                   │
│  │                                                                          │
│  ├─ Step 2/5: Información de Contacto                                     │
│  │   PUT /api/dealer-onboarding/{dealerId}/contact                        │
│  │   • Email (verificación requerida)                                     │
│  │   • Phone, Mobile                                                       │
│  │   • Website (opcional)                                                  │
│  │   • Primary Contact Name                                                │
│  │   → Envía email de verificación                                        │
│  │                                                                          │
│  ├─ Step 3/5: Suscripción                                                 │
│  │   POST /api/billing/subscriptions                                      │
│  │   • Plan seleccionado (Starter/Pro/Enterprise)                         │
│  │   • Payment method (Stripe o AZUL)                                     │
│  │   • Billing details                                                     │
│  │   • Apply Early Bird discount                                          │
│  │   → 3 meses gratis, primer cargo en 90 días                            │
│  │   → Dealer.CurrentPlan = Pro, MaxActiveListings = 50                   │
│  │                                                                          │
│  ├─ Step 4/5: Documentos KYC                                              │
│  │   POST /api/kyc/documents/upload                                       │
│  │   • RNC Certificate (PDF/JPG)                                          │
│  │   • Business License (PDF/JPG)                                         │
│  │   • ID del propietario (Cédula frente/reverso)                         │
│  │   • Proof of address (< 3 meses)                                       │
│  │   → KYCProfile.Status = Pending                                        │
│  │   → Dealer.VerificationStatus = DocumentsUploaded                      │
│  │                                                                          │
│  └─ Step 5/5: Configuración Inicial                                       │
│      PUT /api/dealer-onboarding/{dealerId}/preferences                    │
│      • Business hours                                                      │
│      • Notification preferences                                            │
│      • Logo upload (opcional)                                              │
│      • Bio/Description                                                     │
│      → Dealer.OnboardingStatus = Completed                                │
│      → Redirige a /dealer/onboarding/status (pending approval)            │
│                                                                             │
│  ✉️ PASO 4: EMAIL VERIFICATION (ONBOARD-003)                               │
│  /dealer/onboarding/verify-email?token=xxx                                │
│  ├─ Link en email de verificación                                         │
│  ├─ POST /api/dealer-onboarding/verify-email                              │
│  ├─ Dealer.EmailVerified = true                                           │
│  └─ Redirige a wizard (continuar donde quedó)                             │
│                                                                             │
│  📄 PASO 5: KYC DOCUMENTS (ONBOARD-004)                                    │
│  /dealer/onboarding/documents (parte del wizard step 4)                   │
│  ├─ Drag & drop upload                                                     │
│  ├─ Preview de documentos                                                  │
│  ├─ Validación de tipo y tamaño                                           │
│  ├─ Progress bar de subida                                                │
│  └─ Status por documento: Pending, Uploaded, Verified                     │
│                                                                             │
│  💳 PASO 6: PAYMENT SETUP (ONBOARD-005)                                    │
│  /dealer/onboarding/payment-setup (parte del wizard step 3)               │
│  ├─ Stripe Checkout                                                        │
│  │   • Elementos: Card, Expiry, CVC                                       │
│  │   • 3D Secure support                                                   │
│  │   • Save card for future payments                                      │
│  ├─ AZUL Gateway (República Dominicana)                                   │
│  │   • Redirect to AZUL                                                   │
│  │   • Return URL con status                                              │
│  │   • Webhook para confirmar pago                                        │
│  └─ Early Bird: Primer cargo en 90 días                                   │
│                                                                             │
│  ⏳ PASO 7: ONBOARDING STATUS (ONBOARD-006)                                │
│  /dealer/onboarding/status                                                │
│  ├─ Stepper visual: 5 pasos con checkmarks                                │
│  ├─ GET /api/dealer-onboarding/{dealerId}/status                          │
│  ├─ Estados:                                                               │
│  │   • BusinessInfoCompleted ✅                                           │
│  │   • ContactInfoCompleted ✅                                            │
│  │   • EmailVerified ✅                                                   │
│  │   • SubscriptionActive ✅                                              │
│  │   • DocumentsUploaded ✅                                               │
│  │   • PreferencesSet ✅                                                  │
│  │   • AdminApprovalPending ⏳                                            │
│  ├─ Estimated time: "24-48 horas para revisión"                           │
│  └─ Email cuando aprobado: "¡Tu cuenta está activa!"                      │
│                                                                             │
│  ✅ PASO 8: ACTIVACIÓN (ONBOARD-007)                                       │
│  POST /api/dealer-onboarding/{dealerId}/activate (interno, por admin)     │
│  ├─ Admin aprueba dealer                                                   │
│  ├─ Dealer.Status = Active                                                │
│  ├─ Dealer.VerificationStatus = Verified                                  │
│  ├─ Envía email de bienvenida                                             │
│  └─ Redirige a /dealer/dashboard                                          │
│                                                                             │
│  👨‍💼 ADMIN: APPROVAL (ADMIN-001, ADMIN-002)                               │
│  /admin/dealers/pending                                                    │
│  ├─ Lista de dealers pendientes                                           │
│  ├─ Ver todos los documentos subidos                                      │
│  ├─ Ver info de suscripción                                               │
│  ├─ Verificar RNC en DGII (República Dominicana)                          │
│  ├─ Acciones:                                                              │
│  │   • POST /api/dealer-onboarding/{dealerId}/approve                     │
│  │   • POST /api/dealer-onboarding/{dealerId}/reject                      │
│  │   • Reason for rejection (required)                                    │
│  └─ Notificar por email (aprobado o rechazado)                            │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### DealerOnboardingV2Controller Endpoints (Ya Implementados ✅)

```typescript
// REGISTRATION (ONBOARD-002)
POST / api / dealer - onboarding / register;
// Body: { businessName, rnc, legalName, dealerType, address, city, province,
//        email, phone, mobilePhone, website, contactName, selectedPlan }
// Response: { dealerId, status: 'Pending', onboardingToken }

// CONTACT INFO (Step 2)
PUT / api / dealer - onboarding / { dealerId } / contact;
// Body: { email, phone, mobilePhone, website, primaryContactName }
// Response: { success: true, emailVerificationSent: true }

// EMAIL VERIFICATION (ONBOARD-003)
POST / api / dealer - onboarding / verify - email;
// Body: { token }
// Response: { success: true, dealerId, emailVerified: true }

GET / api / dealer - onboarding / resend - verification;
// Query: ?dealerId={id}
// Response: { success: true, message: "Email sent" }

// DOCUMENTS (ONBOARD-004)
PUT / api / dealer - onboarding / { dealerId } / documents;
// Body: FormData with files
// Response: { documentsUploaded: true, pendingReview: true }

GET / api / dealer - onboarding / { dealerId } / documents;
// Response: { documents: [{ id, type, url, status, uploadedAt }] }

// PREFERENCES (ONBOARD-005)
PUT / api / dealer - onboarding / { dealerId } / preferences;
// Body: { businessHours, notificationPreferences, bio, logo }
// Response: { preferencesSet: true }

// STATUS (ONBOARD-006)
GET / api / dealer - onboarding / { dealerId } / status;
// Response: {
//   dealerId,
//   currentStep: 3,
//   totalSteps: 5,
//   steps: [
//     { name: 'Business Info', completed: true },
//     { name: 'Contact', completed: true },
//     { name: 'Email Verification', completed: false },
//     // ...
//   ],
//   overallStatus: 'InProgress' | 'PendingApproval' | 'Approved' | 'Rejected',
//   estimatedApprovalTime: '24-48 hours',
//   nextAction: 'Verify your email'
// }

// ADMIN APPROVAL (ADMIN-001)
POST / api / dealer - onboarding / { dealerId } / approve;
// Headers: Authorization (Admin only)
// Response: { approved: true, dealerActivated: true, emailSent: true }

// ADMIN REJECTION (ADMIN-002)
POST / api / dealer - onboarding / { dealerId } / reject;
// Body: { reason: string }
// Headers: Authorization (Admin only)
// Response: { rejected: true, emailSent: true }

GET / api / dealer - onboarding / pending - approvals;
// Headers: Authorization (Admin only)
// Query: ?page=1&pageSize=20
// Response: {
//   dealers: [{ id, businessName, submittedAt, documents, subscription }],
//   pagination: {}
// }
```

### BillingService Endpoints (Subscriptions)

```typescript
// SUBSCRIPTIONS (ONBOARD-005)
POST / api / billing / subscriptions;
// Body: {
//   dealerId,
//   planId: 'starter'|'pro'|'enterprise',
//   paymentMethodId,
//   applyEarlyBird: true
// }
// Response: { subscriptionId, firstChargeDate, discountApplied: true }

GET / api / billing / earlybird / status;
// Response: {
//   active: true,
//   daysRemaining: 23,
//   deadline: '2026-01-31T23:59:59',
//   benefits: ['3_months_free', '20_percent_lifetime', 'founder_badge']
// }

POST / api / billing / earlybird / enroll;
// Body: { dealerId, subscriptionId }
// Response: { enrolled: true, benefits: [] }
```

---

## 🎨 LANDING Y PRICING

### PASO 1: DealerLandingPage

```typescript
// filepath: src/app/(public)/dealer/landing/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import {
  TrendingUp,
  BarChart3,
  Users,
  Shield,
  Zap,
  Star,
  ArrowRight
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { EarlyBirdCountdown } from "@/components/dealer/EarlyBirdCountdown";

export const metadata: Metadata = {
  title: "OKLA para Dealers - Aumenta tus ventas 3x | OKLA",
  description: "Únete a los dealers líderes en República Dominicana",
};

export default function DealerLandingPage() {
  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-blue-900 text-white py-20">
        <div className="container max-w-6xl">
          <div className="text-center mb-8">
            <Badge className="bg-yellow-500 text-black mb-4">
              🎉 Oferta de Lanzamiento
            </Badge>
            <h1 className="text-5xl font-bold mb-6">
              Aumenta tus ventas de vehículos hasta 3x
            </h1>
            <p className="text-xl text-blue-100 mb-8 max-w-3xl mx-auto">
              Únete a la plataforma líder de compra-venta de vehículos en
              República Dominicana. Panel profesional, leads calificados y
              herramientas de gestión incluidas.
            </p>
            <Link href="/dealer/pricing">
              <Button size="lg" variant="secondary" className="gap-2">
                Ver Planes y Precios
                <ArrowRight size={20} />
              </Button>
            </Link>
          </div>

          {/* Early Bird Banner */}
          <EarlyBirdCountdown variant="hero" />
        </div>
      </section>

      {/* Benefits Grid */}
      <section className="py-16 bg-gray-50">
        <div className="container max-w-6xl">
          <h2 className="text-3xl font-bold text-center mb-12">
            ¿Por qué elegir OKLA?
          </h2>

          <div className="grid md:grid-cols-3 gap-8">
            <Card className="p-6 text-center hover:shadow-lg transition-shadow">
              <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <TrendingUp size={32} className="text-green-600" />
              </div>
              <h3 className="text-xl font-semibold mb-3">Aumenta Ventas</h3>
              <p className="text-gray-600">
                10,000+ visitantes mensuales buscando vehículos.
                Tus listados vistos por compradores reales.
              </p>
            </Card>

            <Card className="p-6 text-center hover:shadow-lg transition-shadow">
              <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <BarChart3 size={32} className="text-blue-600" />
              </div>
              <h3 className="text-xl font-semibold mb-3">Panel Profesional</h3>
              <p className="text-gray-600">
                Gestiona todo tu inventario desde un solo lugar.
                Estadísticas en tiempo real y reportes.
              </p>
            </Card>

            <Card className="p-6 text-center hover:shadow-lg transition-shadow">
              <div className="w-16 h-16 bg-purple-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <Zap size={32} className="text-purple-600" />
              </div>
              <h3 className="text-xl font-semibold mb-3">Importación Masiva</h3>
              <p className="text-gray-600">
                Sube hasta 50 vehículos en minutos con nuestro
                importador CSV/Excel.
              </p>
            </Card>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-16">
        <div className="container max-w-6xl">
          <div className="grid md:grid-cols-4 gap-8 text-center">
            <div>
              <p className="text-4xl font-bold text-blue-600 mb-2">10K+</p>
              <p className="text-gray-600">Visitantes/Mes</p>
            </div>
            <div>
              <p className="text-4xl font-bold text-green-600 mb-2">500+</p>
              <p className="text-gray-600">Vehículos Publicados</p>
            </div>
            <div>
              <p className="text-4xl font-bold text-purple-600 mb-2">50+</p>
              <p className="text-gray-600">Dealers Activos</p>
            </div>
            <div>
              <p className="text-4xl font-bold text-yellow-600 mb-2">95%</p>
              <p className="text-gray-600">Satisfacción</p>
            </div>
          </div>
        </div>
      </section>

      {/* Features Premium */}
      <section className="py-16 bg-gradient-to-br from-gray-50 to-blue-50">
        <div className="container max-w-6xl">
          <h2 className="text-3xl font-bold text-center mb-12">
            Features Premium Incluidos
          </h2>

          <div className="grid md:grid-cols-2 gap-6">
            {[
              {
                icon: Shield,
                title: "Badge Verificado",
                desc: "Aumenta confianza con sello oficial"
              },
              {
                icon: Users,
                title: "Múltiples Sucursales",
                desc: "Gestiona todas tus locations"
              },
              {
                icon: BarChart3,
                title: "Estadísticas Avanzadas",
                desc: "Vistas, consultas, conversión"
              },
              {
                icon: Star,
                title: "Prioridad en Búsquedas",
                desc: "Aparece primero en resultados"
              },
            ].map((feature) => (
              <div key={feature.title} className="flex items-start gap-4 p-4">
                <div className="p-3 bg-blue-100 rounded-lg">
                  <feature.icon size={24} className="text-blue-600" />
                </div>
                <div>
                  <h3 className="font-semibold text-lg mb-1">{feature.title}</h3>
                  <p className="text-gray-600">{feature.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Final */}
      <section className="py-20 bg-blue-600 text-white">
        <div className="container max-w-4xl text-center">
          <h2 className="text-4xl font-bold mb-6">
            ¿Listo para aumentar tus ventas?
          </h2>
          <p className="text-xl mb-8 text-blue-100">
            Únete hoy y obtén 3 meses gratis + 20% de descuento de por vida
          </p>
          <div className="flex gap-4 justify-center">
            <Link href="/dealer/pricing">
              <Button size="lg" variant="secondary">
                Ver Planes y Precios
              </Button>
            </Link>
            <Link href="/dealer/register">
              <Button size="lg" variant="outline" className="text-white border-white hover:bg-white/10">
                Registrarme Ahora
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
```

---

### PASO 2: DealerPricingPage

```typescript
// filepath: src/app/(public)/dealer/pricing/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { Check, X, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { EarlyBirdCountdown } from "@/components/dealer/EarlyBirdCountdown";
import { PricingFAQ } from "@/components/dealer/PricingFAQ";

export const metadata: Metadata = {
  title: "Planes y Precios - OKLA Dealers",
};

const plans = [
  {
    id: "starter",
    name: "Starter",
    regularPrice: 49,
    earlyBirdPrice: 39,
    maxListings: 15,
    features: [
      "15 vehículos activos",
      "Fotos ilimitadas",
      "Badge verificado",
      "Estadísticas básicas",
      "Soporte por email",
      "1 sucursal",
    ],
    notIncluded: [
      "Múltiples sucursales",
      "Importación CSV",
      "Analytics avanzados",
    ],
  },
  {
    id: "pro",
    name: "Pro",
    regularPrice: 129,
    earlyBirdPrice: 103,
    maxListings: 50,
    recommended: true,
    features: [
      "50 vehículos activos",
      "Fotos ilimitadas",
      "Badge verificado",
      "Estadísticas avanzadas",
      "Soporte prioritario",
      "Hasta 3 sucursales",
      "Importación CSV",
      "Prioridad en búsquedas",
    ],
    notIncluded: ["API access"],
  },
  {
    id: "enterprise",
    name: "Enterprise",
    regularPrice: 299,
    earlyBirdPrice: 239,
    maxListings: "ILIMITADO",
    features: [
      "Vehículos ILIMITADOS",
      "Todo de Pro +",
      "Sucursales ilimitadas",
      "API access",
      "Account manager dedicado",
      "Integración personalizada",
      "Reportes personalizados",
      "Branding personalizado",
      "SLA garantizado",
    ],
    notIncluded: [],
  },
];

export default function DealerPricingPage() {
  return (
    <div className="min-h-screen bg-gray-50 py-12">
      <div className="container max-w-7xl">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">
            Planes y Precios
          </h1>
          <p className="text-xl text-gray-600 max-w-2xl mx-auto">
            Elige el plan perfecto para tu negocio. Todos incluyen
            3 meses gratis con Early Bird.
          </p>
        </div>

        {/* Early Bird Banner */}
        <div className="mb-12">
          <EarlyBirdCountdown variant="pricing" />
        </div>

        {/* Plans Grid */}
        <div className="grid md:grid-cols-3 gap-8 mb-16">
          {plans.map((plan) => (
            <Card
              key={plan.id}
              className={`p-8 relative ${
                plan.recommended ? "ring-2 ring-blue-600 shadow-xl" : ""
              }`}
            >
              {plan.recommended && (
                <Badge className="absolute -top-3 left-1/2 -translate-x-1/2 bg-blue-600">
                  ⭐ RECOMENDADO
                </Badge>
              )}

              {/* Plan name */}
              <h3 className="text-2xl font-bold text-gray-900 mb-2">
                {plan.name}
              </h3>

              {/* Pricing */}
              <div className="mb-6">
                <div className="flex items-baseline gap-2">
                  <span className="text-4xl font-bold text-gray-900">
                    ${plan.earlyBirdPrice}
                  </span>
                  <span className="text-gray-600">/mes</span>
                </div>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-gray-400 line-through">
                    ${plan.regularPrice}
                  </span>
                  <Badge variant="success">
                    Ahorras ${plan.regularPrice - plan.earlyBirdPrice}/mes
                  </Badge>
                </div>
                <p className="text-sm text-gray-600 mt-2">
                  {typeof plan.maxListings === "number"
                    ? `Hasta ${plan.maxListings} vehículos`
                    : plan.maxListings}
                </p>
              </div>

              {/* Features */}
              <ul className="space-y-3 mb-8">
                {plan.features.map((feature) => (
                  <li key={feature} className="flex items-start gap-2">
                    <Check size={20} className="text-green-600 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-700">{feature}</span>
                  </li>
                ))}
                {plan.notIncluded.map((feature) => (
                  <li key={feature} className="flex items-start gap-2 opacity-50">
                    <X size={20} className="text-gray-400 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-500">{feature}</span>
                  </li>
                ))}
              </ul>

              {/* CTA */}
              <Link href={`/dealer/onboarding/v2?plan=${plan.id}`}>
                <Button
                  className="w-full"
                  variant={plan.recommended ? "default" : "outline"}
                  size="lg"
                >
                  {plan.recommended && <Sparkles size={16} className="mr-2" />}
                  ¡Aprovechar Oferta!
                </Button>
              </Link>
            </Card>
          ))}
        </div>

        {/* FAQ */}
        <PricingFAQ />

        {/* Contact */}
        <div className="text-center mt-12 p-8 bg-blue-50 rounded-xl">
          <h3 className="text-xl font-semibold mb-2">¿Tienes preguntas?</h3>
          <p className="text-gray-600 mb-4">
            Nuestro equipo está listo para ayudarte
          </p>
          <div className="flex gap-4 justify-center text-sm">
            <div>
              📞 <a href="tel:+18095551234" className="text-blue-600 hover:underline">
                (809) 555-1234
              </a>
            </div>
            <div>
              ✉️ <a href="mailto:dealers@okla.com.do" className="text-blue-600 hover:underline">
                dealers@okla.com.do
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

## 🎨 WIZARD DE ONBOARDING

### PASO 3: DealerOnboardingWizard - Main Component

```typescript
// filepath: src/components/dealer/onboarding/DealerOnboardingWizard.tsx
"use client";

import { useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Progress } from "@/components/ui/Progress";
import { OnboardingStepIndicator } from "./OnboardingStepIndicator";
import { BusinessInfoStep } from "./steps/BusinessInfoStep";
import { ContactInfoStep } from "./steps/ContactInfoStep";
import { SubscriptionStep } from "./steps/SubscriptionStep";
import { DocumentsStep } from "./steps/DocumentsStep";
import { PreferencesStep } from "./steps/PreferencesStep";
import {
  useCreateDealer,
  useUpdateDealerContact,
  useUploadDocuments,
  useUpdatePreferences,
} from "@/lib/hooks/useDealerOnboarding";
import { toast } from "sonner";

const steps = [
  { id: 1, name: "Información del Negocio", component: BusinessInfoStep },
  { id: 2, name: "Contacto", component: ContactInfoStep },
  { id: 3, name: "Suscripción", component: SubscriptionStep },
  { id: 4, name: "Documentos KYC", component: DocumentsStep },
  { id: 5, name: "Configuración", component: PreferencesStep },
];

export function DealerOnboardingWizard() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const selectedPlan = searchParams.get("plan") || "pro";

  const [currentStep, setCurrentStep] = useState(1);
  const [dealerId, setDealerId] = useState<string | null>(null);
  const [formData, setFormData] = useState<any>({});

  const { mutate: createDealer, isPending: isCreating } = useCreateDealer();
  const { mutate: updateContact, isPending: isUpdatingContact } = useUpdateDealerContact();
  const { mutate: uploadDocs, isPending: isUploadingDocs } = useUploadDocuments();
  const { mutate: updatePrefs, isPending: isUpdatingPrefs } = useUpdatePreferences();

  const progress = (currentStep / steps.length) * 100;
  const CurrentStepComponent = steps[currentStep - 1].component;

  const handleNext = async (stepData: any) => {
    const newData = { ...formData, ...stepData };
    setFormData(newData);

    if (currentStep === 1) {
      // Step 1: Create dealer
      createDealer(
        { ...stepData, selectedPlan },
        {
          onSuccess: (data) => {
            setDealerId(data.dealerId);
            setCurrentStep(2);
            toast.success("Información guardada");
          },
        }
      );
    } else if (currentStep === 2 && dealerId) {
      // Step 2: Update contact
      updateContact(
        { dealerId, ...stepData },
        {
          onSuccess: () => {
            setCurrentStep(3);
            toast.success("Email de verificación enviado");
          },
        }
      );
    } else if (currentStep === 3) {
      // Step 3: Subscription handled by SubscriptionStep
      setCurrentStep(4);
    } else if (currentStep === 4 && dealerId) {
      // Step 4: Documents handled by DocumentsStep
      setCurrentStep(5);
    } else if (currentStep === 5 && dealerId) {
      // Step 5: Final preferences
      updatePrefs(
        { dealerId, ...stepData },
        {
          onSuccess: () => {
            toast.success("¡Onboarding completado!");
            router.push(`/dealer/onboarding/status?dealerId=${dealerId}`);
          },
        }
      );
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const isPending =
    isCreating || isUpdatingContact || isUploadingDocs || isUpdatingPrefs;

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Progress bar */}
      <div className="mb-8">
        <Progress value={progress} className="h-2 mb-2" />
        <p className="text-sm text-gray-600 text-center">
          Paso {currentStep} de {steps.length}
        </p>
      </div>

      {/* Step indicator */}
      <OnboardingStepIndicator
        steps={steps.map((s) => s.name)}
        currentStep={currentStep}
        className="mb-12"
      />

      {/* Current step content */}
      <div className="bg-white rounded-xl border p-8 mb-6">
        <CurrentStepComponent
          data={formData}
          dealerId={dealerId}
          selectedPlan={selectedPlan}
          onNext={handleNext}
          onBack={handleBack}
          isPending={isPending}
        />
      </div>

      {/* Help text */}
      <div className="text-center text-sm text-gray-600">
        ¿Necesitas ayuda?{" "}
        <a href="mailto:soporte@okla.com.do" className="text-blue-600 hover:underline">
          Contáctanos
        </a>
      </div>
    </div>
  );
}
```

---

### PASO 4: BusinessInfoStep (Step 1/5)

```typescript
// filepath: src/components/dealer/onboarding/steps/BusinessInfoStep.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { Loader2 } from "lucide-react";

const schema = z.object({
  businessName: z.string().min(3, "Mínimo 3 caracteres"),
  rnc: z.string().regex(/^\d{9,11}$/, "RNC debe tener 9-11 dígitos"),
  legalName: z.string().min(3, "Mínimo 3 caracteres"),
  dealerType: z.enum(["Independent", "Chain", "MultipleStore", "Franchise"]),
  address: z.string().min(10, "Dirección completa requerida"),
  city: z.string().min(2, "Ciudad requerida"),
  province: z.string().min(2, "Provincia requerida"),
});

type FormData = z.infer<typeof schema>;

interface BusinessInfoStepProps {
  data: any;
  onNext: (data: FormData) => void;
  isPending: boolean;
}

export function BusinessInfoStep({ data, onNext, isPending }: BusinessInfoStepProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: data,
  });

  return (
    <form onSubmit={handleSubmit(onNext)} className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">
          Información del Negocio
        </h2>
        <p className="text-gray-600">
          Cuéntanos sobre tu negocio de venta de vehículos
        </p>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Nombre del Negocio *
          </label>
          <Input
            {...register("businessName")}
            placeholder="Ej: Auto Express RD"
            error={errors.businessName?.message}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            RNC *
          </label>
          <Input
            {...register("rnc")}
            placeholder="123456789"
            maxLength={11}
            error={errors.rnc?.message}
          />
          <p className="text-xs text-gray-500 mt-1">9-11 dígitos</p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Razón Social *
          </label>
          <Input
            {...register("legalName")}
            placeholder="Auto Express SRL"
            error={errors.legalName?.message}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Tipo de Dealer *
          </label>
          <Select {...register("dealerType")} error={errors.dealerType?.message}>
            <option value="">Seleccionar...</option>
            <option value="Independent">Independiente</option>
            <option value="Chain">Cadena</option>
            <option value="MultipleStore">Multi-sucursal</option>
            <option value="Franchise">Franquicia</option>
          </Select>
        </div>

        <div className="md:col-span-2">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Dirección Completa *
          </label>
          <Input
            {...register("address")}
            placeholder="Av. Abraham Lincoln #123, Piantini"
            error={errors.address?.message}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Ciudad *
          </label>
          <Input
            {...register("city")}
            placeholder="Santo Domingo"
            error={errors.city?.message}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Provincia *
          </label>
          <Select {...register("province")} error={errors.province?.message}>
            <option value="">Seleccionar...</option>
            <option value="Distrito Nacional">Distrito Nacional</option>
            <option value="Santo Domingo">Santo Domingo</option>
            <option value="Santiago">Santiago</option>
            <option value="La Vega">La Vega</option>
            {/* Add more provinces */}
          </Select>
        </div>
      </div>

      <div className="flex justify-end gap-4">
        <Button
          type="submit"
          size="lg"
          disabled={isPending}
          className="min-w-[200px]"
        >
          {isPending && <Loader2 size={16} className="mr-2 animate-spin" />}
          Continuar
        </Button>
      </div>
    </form>
  );
}
```

---

### PASO 5: SubscriptionStep (Step 3/5)

```typescript
// filepath: src/components/dealer/onboarding/steps/SubscriptionStep.tsx
"use client";

import { useState } from "react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, CardElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Check, Loader2 } from "lucide-react";
import { useCreateSubscription } from "@/lib/hooks/useDealerOnboarding";
import { toast } from "sonner";

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_KEY!);

const planDetails = {
  starter: {
    name: "Starter",
    price: 39,
    originalPrice: 49,
    features: ["15 vehículos", "Badge verificado", "Stats básicas"],
  },
  pro: {
    name: "Pro",
    price: 103,
    originalPrice: 129,
    features: ["50 vehículos", "Analytics avanzados", "CSV import"],
  },
  enterprise: {
    name: "Enterprise",
    price: 239,
    originalPrice: 299,
    features: ["Ilimitado", "API access", "Account manager"],
  },
};

interface SubscriptionStepProps {
  dealerId: string | null;
  selectedPlan: string;
  onNext: () => void;
  onBack: () => void;
}

function SubscriptionForm({ dealerId, selectedPlan, onNext }: SubscriptionStepProps) {
  const stripe = useStripe();
  const elements = useElements();
  const [isProcessing, setIsProcessing] = useState(false);

  const { mutate: createSubscription } = useCreateSubscription();
  const plan = planDetails[selectedPlan as keyof typeof planDetails];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe || !elements || !dealerId) return;

    setIsProcessing(true);

    try {
      // Create payment method
      const cardElement = elements.getElement(CardElement);
      if (!cardElement) throw new Error("Card element not found");

      const { error, paymentMethod } = await stripe.createPaymentMethod({
        type: "card",
        card: cardElement,
      });

      if (error) {
        toast.error(error.message);
        return;
      }

      // Create subscription
      createSubscription(
        {
          dealerId,
          planId: selectedPlan,
          paymentMethodId: paymentMethod.id,
          applyEarlyBird: true,
        },
        {
          onSuccess: () => {
            toast.success("Suscripción activada!");
            onNext();
          },
          onError: (err: any) => {
            toast.error(err.message || "Error al procesar pago");
          },
        }
      );
    } catch (error: any) {
      toast.error(error.message);
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">
          Configurar Suscripción
        </h2>
        <p className="text-gray-600">
          Configura tu método de pago. Primer cargo en 90 días.
        </p>
      </div>

      {/* Plan summary */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">{plan.name}</h3>
            <p className="text-sm text-gray-600 mt-1">
              {plan.features.join(" • ")}
            </p>
          </div>
          <Badge className="bg-green-500">Early Bird</Badge>
        </div>

        <div className="flex items-baseline gap-2">
          <span className="text-3xl font-bold text-gray-900">${plan.price}</span>
          <span className="text-gray-600">/mes</span>
          <span className="text-gray-400 line-through ml-2">${plan.originalPrice}</span>
        </div>

        <div className="mt-4 space-y-2 text-sm">
          <div className="flex items-center gap-2">
            <Check size={16} className="text-green-600" />
            <span>3 meses gratis (primer cargo en Abril 2026)</span>
          </div>
          <div className="flex items-center gap-2">
            <Check size={16} className="text-green-600" />
            <span>20% descuento de por vida</span>
          </div>
          <div className="flex items-center gap-2">
            <Check size={16} className="text-green-600" />
            <span>Badge "Miembro Fundador" permanente</span>
          </div>
        </div>
      </div>

      {/* Card input */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Tarjeta de Crédito/Débito
        </label>
        <div className="p-4 border border-gray-300 rounded-lg">
          <CardElement
            options={{
              style: {
                base: {
                  fontSize: "16px",
                  color: "#424770",
                  "::placeholder": { color: "#aab7c4" },
                },
              },
            }}
          />
        </div>
        <p className="text-xs text-gray-500 mt-2">
          💳 Aceptamos Visa, Mastercard, AMEX
        </p>
      </div>

      {/* Security note */}
      <div className="bg-gray-50 rounded-lg p-4 text-sm text-gray-600">
        🔒 Tus datos están protegidos con encriptación SSL. No almacenamos
        información de tarjeta en nuestros servidores.
      </div>

      <div className="flex justify-between gap-4">
        <Button type="button" variant="outline" onClick={onNext}>
          Volver
        </Button>
        <Button
          type="submit"
          size="lg"
          disabled={!stripe || isProcessing}
          className="min-w-[200px]"
        >
          {isProcessing && <Loader2 size={16} className="mr-2 animate-spin" />}
          Activar Suscripción
        </Button>
      </div>
    </form>
  );
}

export function SubscriptionStep(props: SubscriptionStepProps) {
  return (
    <Elements stripe={stripePromise}>
      <SubscriptionForm {...props} />
    </Elements>
  );
}
```

---

### PASO 6: DocumentsStep (Step 4/5)

```typescript
// filepath: src/components/dealer/onboarding/steps/DocumentsStep.tsx
"use client";

import { useState } from "react";
import { Upload, FileText, CheckCircle, AlertCircle, X } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useUploadDocument } from "@/lib/hooks/useDealerOnboarding";
import { toast } from "sonner";

const requiredDocuments = [
  {
    type: "RNC",
    title: "Certificado RNC",
    description: "Certificado de registro mercantil (DGII)",
    required: true,
  },
  {
    type: "BusinessLicense",
    title: "Licencia Comercial",
    description: "Licencia municipal de operación",
    required: true,
  },
  {
    type: "IdentityCard",
    title: "Cédula del Propietario",
    description: "Frente y reverso (ambos lados)",
    required: true,
  },
  {
    type: "ProofOfAddress",
    title: "Comprobante de Domicilio",
    description: "Recibo luz/agua/teléfono (< 3 meses)",
    required: true,
  },
];

interface DocumentsStepProps {
  dealerId: string | null;
  onNext: () => void;
  onBack: () => void;
}

export function DocumentsStep({ dealerId, onNext, onBack }: DocumentsStepProps) {
  const [uploadedDocs, setUploadedDocs] = useState<Record<string, boolean>>({});
  const { mutate: uploadDoc, isPending } = useUploadDocument();

  const handleFileSelect = (type: string, file: File) => {
    if (!dealerId) return;

    // Validate
    if (!["image/jpeg", "image/png", "application/pdf"].includes(file.type)) {
      toast.error("Solo se permiten JPG, PNG o PDF");
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      toast.error("El archivo debe ser menor a 10MB");
      return;
    }

    const formData = new FormData();
    formData.append("file", file);
    formData.append("dealerId", dealerId);
    formData.append("documentType", type);

    uploadDoc(formData, {
      onSuccess: () => {
        setUploadedDocs((prev) => ({ ...prev, [type]: true }));
        toast.success("Documento subido correctamente");
      },
    });
  };

  const allDocsUploaded = requiredDocuments.every((doc) => uploadedDocs[doc.type]);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">
          Documentos de Verificación (KYC)
        </h2>
        <p className="text-gray-600">
          Sube los documentos requeridos para verificar tu negocio
        </p>
      </div>

      {/* Documents list */}
      <div className="space-y-4">
        {requiredDocuments.map((doc) => {
          const isUploaded = uploadedDocs[doc.type];

          return (
            <div
              key={doc.type}
              className="border rounded-lg p-6 hover:border-blue-300 transition-colors"
            >
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-4">
                  <div className="p-3 bg-blue-50 rounded-lg">
                    <FileText size={24} className="text-blue-600" />
                  </div>
                  <div>
                    <h3 className="font-semibold text-gray-900 mb-1">
                      {doc.title}
                      {doc.required && (
                        <Badge variant="error" className="ml-2">
                          Requerido
                        </Badge>
                      )}
                    </h3>
                    <p className="text-sm text-gray-600 mb-3">{doc.description}</p>

                    {!isUploaded ? (
                      <label className="cursor-pointer">
                        <input
                          type="file"
                          className="hidden"
                          accept="image/jpeg,image/png,application/pdf"
                          onChange={(e) => {
                            const file = e.target.files?.[0];
                            if (file) handleFileSelect(doc.type, file);
                          }}
                          disabled={isPending}
                        />
                        <Button type="button" variant="outline" size="sm">
                          <Upload size={16} className="mr-2" />
                          Subir Archivo
                        </Button>
                      </label>
                    ) : (
                      <div className="flex items-center gap-2 text-green-600">
                        <CheckCircle size={16} />
                        <span className="text-sm font-medium">Subido ✓</span>
                      </div>
                    )}
                  </div>
                </div>

                {isUploaded && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() =>
                      setUploadedDocs((prev) => ({ ...prev, [doc.type]: false }))
                    }
                  >
                    <X size={16} />
                  </Button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Info box */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex gap-3">
          <AlertCircle size={20} className="text-yellow-600 flex-shrink-0 mt-0.5" />
          <div className="text-sm text-yellow-800">
            <p className="font-semibold mb-1">Información importante:</p>
            <ul className="list-disc list-inside space-y-1">
              <li>Los documentos serán revisados en 24-48 horas</li>
              <li>Asegúrate de que las imágenes sean claras y legibles</li>
              <li>Formatos aceptados: JPG, PNG, PDF (máx. 10MB)</li>
            </ul>
          </div>
        </div>
      </div>

      <div className="flex justify-between gap-4">
        <Button type="button" variant="outline" onClick={onBack}>
          Volver
        </Button>
        <Button
          type="button"
          size="lg"
          disabled={!allDocsUploaded || isPending}
          onClick={onNext}
          className="min-w-[200px]"
        >
          Continuar
        </Button>
      </div>
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Landing:
# - /dealer/landing muestra hero con gradient
# - Benefits grid con 3 cards
# - Stats section con 4 métricas
# - Features premium con iconos
# - CTA final con 2 botones

# Verificar Pricing:
# - /dealer/pricing muestra 3 planes
# - Early Bird banner con countdown
# - Plan Pro marcado como recomendado
# - Precios tachados y descuentos
# - FAQ section
# - Botón de cada plan redirige a /dealer/onboarding/v2?plan=X

# Verificar Wizard:
# - /dealer/onboarding/v2?plan=pro muestra step 1/5
# - Progress bar funciona
# - Step indicator actualiza
# - Validación de formularios funciona
# - RNC valida 9-11 dígitos
# - Email verification se envía
# - Stripe checkout funciona
# - Upload de documentos funciona (max 10MB)
# - Completion redirige a /dealer/onboarding/status

# Verificar Status:
# - /dealer/onboarding/status muestra stepper
# - Estados de pasos (completado/pendiente)
# - Mensaje "En revisión 24-48h"

# Verificar Admin Approval:
# - /admin/dealers/pending lista dealers
# - Ver documentos funciona
# - Aprobar funciona
# - Rechazar pide razón
# - Email de notificación se envía
```

---

## � DOCUMENTACIÓN CONSOLIDADA

> **NOTA:** Este documento consolida toda la documentación de Dealer Onboarding previamente distribuida en múltiples archivos.

### Páginas Incluidas en este Documento

| Página                          | Ruta                    | LOC  | Descripción                         |
| ------------------------------- | ----------------------- | ---- | ----------------------------------- |
| **DealerLandingPage**           | `/dealer/landing`       | ~200 | Landing con beneficios y Early Bird |
| **DealerPricingPage**           | `/dealer/pricing`       | ~250 | Planes: Starter, Pro, Enterprise    |
| **DealerRegistrationPage**      | `/dealer/register`      | ~350 | Formulario de registro inicial      |
| **DealerOnboardingPage**        | `/dealer/onboarding`    | 448  | Wizard multi-paso                   |
| **DealerDocumentsPage**         | `/dealer/documents`     | 352  | Subida de documentos KYC            |
| **DealerEmailVerificationPage** | `/dealer/verify-email`  | ~100 | Verificación de email               |
| **DealerPaymentSetupPage**      | `/dealer/payment-setup` | ~200 | Configurar método de pago           |
| **DealerSubscribePage**         | `/dealer/subscribe`     | ~300 | Checkout de suscripción             |

### Flujo Completo de Conversión

```
/dealer/landing → /dealer/pricing → /dealer/register → /dealer/onboarding → /dealer/dashboard
     ↓                  ↓                  ↓                   ↓
  Beneficios        Comparar          Formulario          Wizard 6 pasos
  Early Bird        Planes            básico              + Documentos KYC
                    3 tiers                               + Verificación
```

### Documentos Requeridos (KYC - Ley 155-17)

| Documento                 | Obligatorio    | Formato        |
| ------------------------- | -------------- | -------------- |
| RNC (DGII)                | ✅             | PDF/Imagen     |
| Registro Mercantil        | ✅             | PDF            |
| Cédula del Representante  | ✅             | Imagen         |
| Contrato de Arrendamiento | ✅             | PDF            |
| Fotos del Local           | ✅             | Imágenes (3-5) |
| Patente Municipal         | ⚡ Recomendado | PDF            |

### Referencias Relacionadas

- **Dashboard:** [06-dealer-dashboard.md](06-dealer-dashboard.md)
- **Analytics:** [28-dealer-analytics-completo.md](28-dealer-analytics-completo.md)
- **Inventario:** [09-dealer-inventario.md](09-dealer-inventario.md)
- **KYC Completo:** [27-kyc-verificacion.md](27-kyc-verificacion.md)

---

## �🚀 MEJORAS FUTURAS

1. **Live Chat Support**: Chat en vivo durante onboarding
2. **Progressive Saving**: Auto-save del wizard cada paso
3. **Resume Onboarding**: Continuar donde quedó si abandonó
4. **Video KYC**: Verificación por video llamada (Zoom/Meet)
5. **OCR Automation**: Extraer datos de documentos automáticamente
6. **WhatsApp Integration**: Updates vía WhatsApp
7. **Multi-language**: Soporte en inglés y francés (Haití)

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-onboarding.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Dealer Onboarding", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar landing de dealers", async ({ page }) => {
    await page.goto("/dealer");

    await expect(
      page.getByRole("heading", { name: /conviértete en dealer/i }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /comenzar/i })).toBeVisible();
  });

  test("debe iniciar wizard de onboarding", async ({ page }) => {
    await page.goto("/dealer/onboarding");

    await expect(page.getByTestId("onboarding-stepper")).toBeVisible();
    await expect(page.getByText(/información del negocio/i)).toBeVisible();
  });

  test("debe completar paso 1 - Info del negocio", async ({ page }) => {
    await page.goto("/dealer/onboarding");

    await page.fill('input[name="businessName"]', "Auto Dealer RD");
    await page.fill('input[name="rnc"]', "123456789");
    await page.fill('input[name="phone"]', "8091234567");
    await page.getByRole("button", { name: /siguiente/i }).click();

    await expect(page.getByText(/paso 2/i)).toBeVisible();
  });

  test("debe subir documentos requeridos", async ({ page }) => {
    await page.goto("/dealer/onboarding?step=2");

    const fileInput = page.locator('input[type="file"]').first();
    await fileInput.setInputFiles("./fixtures/rnc-document.pdf");

    await expect(page.getByTestId("document-preview")).toBeVisible();
  });

  test("debe seleccionar plan de suscripción", async ({ page }) => {
    await page.goto("/dealer/onboarding?step=3");

    await expect(page.getByTestId("plan-starter")).toBeVisible();
    await expect(page.getByTestId("plan-pro")).toBeVisible();
    await expect(page.getByTestId("plan-enterprise")).toBeVisible();

    await page.getByTestId("plan-pro").click();
    await expect(page.getByTestId("plan-pro")).toHaveAttribute(
      "data-selected",
      "true",
    );
  });

  test("debe mostrar resumen antes de enviar", async ({ page }) => {
    await page.goto("/dealer/onboarding?step=4");

    await expect(page.getByTestId("onboarding-summary")).toBeVisible();
    await expect(
      page.getByRole("button", { name: /enviar solicitud/i }),
    ).toBeVisible();
  });
});
```

---

**Siguiente documento:** `30-seller-profiles-completo.md` - Perfiles completos de vendedores
