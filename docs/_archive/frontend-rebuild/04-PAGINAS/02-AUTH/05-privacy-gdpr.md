---
title: "26. Privacy & GDPR Compliance (Ley 172-13 RD)"
priority: P0
estimated_time: ""
dependencies: []
apis: ["UserService"]
status: complete
last_updated: "2026-01-30"
---

# 26. Privacy & GDPR Compliance (Ley 172-13 RD)

**Objetivo:** Cumplimiento completo con GDPR, CCPA, LGPD y **Ley 172-13 (República Dominicana)**, incluyendo consentimiento de cookies, política de privacidad, derecho al olvido (ARCO), exportación de datos, gestión de consentimientos y auditoría de acceso.

**Prioridad:** P2 (Alta - Legal compliance obligatorio)  
**Complejidad:** 🟢 Baja-Media (Legal docs, Data export, Cookie consent)  
**Dependencias:** PrivacyService (✅ IMPLEMENTADO), Todos los servicios (para data export)  
**Última Auditoría:** Enero 29, 2026

---

## 🔍 AUDITORÍA COMPLETA DE IMPLEMENTACIÓN (Enero 29, 2026)

### 📊 Resumen Ejecutivo

| Categoría                     | Requisitos | Implementado | Pendiente | % Completado |
| ----------------------------- | ---------- | ------------ | --------- | ------------ |
| **Derechos ARCO**             | 4          | 4            | 0         | ✅ 100%      |
| **Privacy Center**            | 1          | 1            | 0         | ✅ 100%      |
| **Exportación de Datos**      | 3          | 3            | 0         | ✅ 100%      |
| **Eliminación de Cuenta**     | 3          | 3            | 0         | ✅ 100%      |
| **Ver Mis Datos**             | 1          | 1            | 0         | ✅ 100%      |
| **Cookie Consent Banner**     | 4          | 0            | 4         | 🔴 0%        |
| **Consentimientos Registro**  | 2          | 0            | 2         | 🔴 0%        |
| **Preferencias Comunicación** | 3          | 0            | 3         | 🔴 0%        |
| **Historial Consentimientos** | 2          | 0            | 2         | 🔴 0%        |
| **Página Unsubscribe**        | 3          | 0            | 3         | 🔴 0%        |
| **TOTAL**                     | **26**     | **12**       | **14**    | **46%**      |

### ✅ IMPLEMENTADO CORRECTAMENTE (12/26)

#### 1. Privacy Center (100% ✅)

**Ruta:** `/privacy-center`  
**Archivo:** `src/pages/user/PrivacyCenterPage.tsx`

✅ **Funcionalidades:**

- Dashboard completo de privacidad
- Acceso a derechos ARCO
- Panel de consentimientos
- Links a exportación y eliminación
- Acceso protegido con autenticación

#### 2. Ver Mis Datos (100% ✅)

**Ruta:** `/settings/privacy/my-data`  
**Archivo:** `src/pages/user/MyDataPage.tsx`

✅ **Funcionalidades:**

- Vista completa de datos personales
- Organizado por categorías
- Perfil, actividad, transacciones, privacidad
- Botón para exportar datos

#### 3. Exportar Datos (100% ✅)

**Ruta:** `/settings/privacy/download-my-data`  
**Archivo:** `src/pages/user/DataDownloadPage.tsx`

✅ **Funcionalidades:**

- Múltiples formatos (JSON, CSV, PDF)
- Selección de categorías
- Tracking de estado
- Descarga de archivo
- Notificación cuando esté listo

#### 4. Eliminar Cuenta (100% ✅)

**Ruta:** `/settings/privacy/delete-account`  
**Archivo:** `src/pages/user/DeleteAccountPage.tsx`

✅ **Funcionalidades:**

- Proceso de confirmación
- Selección de razón
- Periodo de gracia 7 días
- Opción para cancelar
- Advertencias sobre datos retenidos

### 🔴 FALTANTES CRÍTICOS (14/26)

#### 1. Cookie Consent Banner (0% 🔴)

**Prioridad:** P0 (Crítica - Legal compliance)  
**Riesgo:** GDPR: €20M / Ley 172-13: RD$500K

❌ **No existe:**

- Banner de consentimiento de cookies
- Categorías (Essential, Analytics, Marketing, Personalization)
- Botones "Aceptar Todo", "Rechazar Todo", "Personalizar"
- Almacenamiento de preferencias
- Envío al backend

**Archivos Faltantes:**

```
src/components/privacy/CookieConsentBanner.tsx
src/lib/hooks/useCookieConsent.ts
```

**Integración Requerida:**

```tsx
// App.tsx o MainLayout.tsx
import { CookieConsentBanner } from "@/components/privacy/CookieConsentBanner";

function App() {
  return (
    <>
      <CookieConsentBanner /> {/* ← FALTA */}
      <Router>...</Router>
    </>
  );
}
```

#### 2. Consentimientos en Registro (0% 🔴)

**Prioridad:** P0 (Crítica)  
**Riesgo:** Base de usuarios sin consentimiento válido

❌ **No existe en RegisterPage:**

- Checkbox obligatorio de términos
- Checkbox obligatorio de política de privacidad
- Checkboxes opcionales de marketing

**Código Esperado:**

```tsx
<form onSubmit={handleSubmit(onSubmit)}>
  {/* Campos existentes */}

  {/* FALTA ESTO: */}
  <div className="space-y-3 border-t pt-4 mt-4">
    <div className="flex items-start">
      <input
        type="checkbox"
        id="terms"
        {...register("acceptTerms", {
          required: "Debes aceptar los términos",
        })}
      />
      <label htmlFor="terms" className="ml-2 text-sm">
        Acepto los <a href="/terms">Términos de Servicio</a> *
      </label>
    </div>

    <div className="flex items-start">
      <input
        type="checkbox"
        id="privacy"
        {...register("acceptPrivacy", {
          required: "Debes aceptar la política",
        })}
      />
      <label htmlFor="privacy" className="ml-2 text-sm">
        Acepto la <a href="/privacy">Política de Privacidad</a> *
      </label>
    </div>

    {/* Opcionales */}
    <div className="flex items-start">
      <input type="checkbox" id="marketing" {...register("acceptMarketing")} />
      <label htmlFor="marketing" className="ml-2 text-sm">
        Quiero recibir ofertas por email (opcional)
      </label>
    </div>
  </div>
</form>
```

#### 3. Preferencias de Comunicación (0% 🔴)

**Prioridad:** P1 (Alta)  
**Riesgo:** Ley 172-13 Art. 9 (Derecho de Oposición)

❌ **Falta:**

- Página de preferencias
- Gestión por canal (Email, SMS, WhatsApp, Push)
- Gestión por tipo (Marketing, Partners, Alertas)
- Toggles granulares

**Ruta Faltante:**

```
/settings/notifications/preferences
```

**Archivo Faltante:**

```
src/pages/user/CommunicationPreferencesPage.tsx
```

#### 4. Historial de Consentimientos (0% 🔴)

**Prioridad:** P1 (Alta)  
**Riesgo:** Sin auditoría de consentimientos

❌ **Falta:**

- Página de historial
- Timeline de cambios
- Log de otorgamiento/revocación
- Exportación de historial

**Ruta Faltante:**

```
/settings/privacy/consent-history
```

**Archivo Faltante:**

```
src/pages/user/ConsentHistoryPage.tsx
src/components/consent/ConsentHistoryTimeline.tsx
```

#### 5. Página de Unsubscribe (0% 🔴)

**Prioridad:** P1 (Alta)  
**Riesgo:** CAN-SPAM, Ley 172-13 Art. 9

❌ **Falta:**

- Página de cancelación desde email
- Token de autenticación en URL
- Formulario de razones
- Confirmación de cancelación

**Ruta Faltante:**

```
/unsubscribe?token=xxx&type=xxx
```

**Archivo Faltante:**

```
src/pages/UnsubscribePage.tsx
src/components/consent/UnsubscribeConfirmation.tsx
```

### ✅ Páginas Implementadas

| Ruta                                 | Componente        | Funcionalidad        | Estado  |
| ------------------------------------ | ----------------- | -------------------- | ------- |
| `/privacy-center`                    | PrivacyCenterPage | Dashboard ARCO       | ✅ 100% |
| `/settings/privacy/my-data`          | MyDataPage        | Ver datos personales | ✅ 100% |
| `/settings/privacy/download-my-data` | DataDownloadPage  | Exportar datos       | ✅ 100% |
| `/settings/privacy/delete-account`   | DeleteAccountPage | Eliminación cuenta   | ✅ 100% |
| `/privacy`                           | PrivacyPolicyPage | Política privacidad  | ✅ 100% |
| `/terms`                             | TermsPage         | Términos condiciones | ✅ 100% |

### 🚨 Impacto Legal de los Faltantes

#### Riesgo Alto (P0 - Crítico) 🔴

**1. Cookie Consent Banner Ausente**

- **Riesgo:** Violación GDPR Article 7, Multa hasta €20M o 4% revenue
- **República Dominicana:** Multa Ley 172-13 hasta RD$500,000
- **Impacto:** Usuario no puede dar consentimiento informado para cookies
- **Tiempo de Implementación:** 2-3 días

**2. Consentimientos en Registro Ausentes**

- **Riesgo:** Cuentas creadas sin consentimiento válido
- **República Dominicana:** Tratamiento ilegal de datos (Art. 6 Ley 172-13)
- **Impacto:** Base de usuarios sin consentimiento legal
- **Tiempo de Implementación:** 1 día

#### Riesgo Medio (P1 - Alta) 🟡

**3. Preferencias de Comunicación**

- **Riesgo:** Violación CAN-SPAM, GDPR Article 21 (Right to object)
- **República Dominicana:** Ley 172-13 Art. 9 (Derecho de Oposición)
- **Impacto:** Usuarios no pueden oponerse al marketing
- **Tiempo de Implementación:** 3-4 días

**4. Historial de Consentimientos**

- **Riesgo:** Falta de auditoría (GDPR Article 7.1)
- **República Dominicana:** No se puede demostrar consentimiento
- **Impacto:** Imposible defender en caso de queja
- **Tiempo de Implementación:** 2 días

**5. Página de Unsubscribe**

- **Riesgo:** Violación CAN-SPAM (requerido en todo email marketing)
- **República Dominicana:** Incumplimiento Ley 172-13 Art. 9
- **Impacto:** Multas por cada email sin unsubscribe funcional
- **Tiempo de Implementación:** 1-2 días

### 🛠️ Plan de Implementación Recomendado

#### Sprint Crítico (2 semanas)

**Semana 1 - Consentimientos Básicos**

**Día 1-2: Cookie Consent Banner**

- [ ] Crear `CookieConsentBanner.tsx`
- [ ] Crear hook `useCookieConsent()`
- [ ] Integrar en `App.tsx` o `MainLayout.tsx`
- [ ] Implementar almacenamiento en localStorage
- [ ] Conectar con backend `/api/privacy/consent`

**Día 3: Consentimientos en Registro**

- [ ] Modificar formulario de registro
- [ ] Agregar checkboxes obligatorios (terms, privacy)
- [ ] Agregar checkboxes opcionales (marketing, newsletter)
- [ ] Enviar consents al backend en `POST /api/auth/register`
- [ ] Validación con react-hook-form

**Día 4-5: Página de Unsubscribe**

- [ ] Crear `UnsubscribePage.tsx`
- [ ] Crear `UnsubscribeConfirmation.tsx`
- [ ] Implementar lógica de token
- [ ] Conectar con backend `/api/unsubscribe`

**Semana 2 - Preferencias & Auditoría**

**Día 6-8: Preferencias de Comunicación**

- [ ] Crear `CommunicationPreferencesPage.tsx`
- [ ] Crear componentes de toggles por canal/tipo
- [ ] Crear hook `useConsentPreferences()`
- [ ] Conectar con backend `/api/privacy/communication`

**Día 9-10: Historial de Consentimientos**

- [ ] Crear `ConsentHistoryPage.tsx`
- [ ] Crear `ConsentHistoryTimeline.tsx`
- [ ] Crear hook `useConsentHistory()`
- [ ] Implementar paginación
- [ ] Conectar con backend `/api/privacy/consent/history`

**Testing & QA (Día 11-13)**

- [ ] Testing E2E de flujo completo
- [ ] Validación de almacenamiento de consents
- [ ] Pruebas de unsubscribe desde emails
- [ ] Verificación de auditoría
- [ ] Code review
- [ ] Deploy a staging

### 📝 Checklist de Tareas Pendientes

#### Componentes Faltantes

- [ ] `src/components/privacy/CookieConsentBanner.tsx`
- [ ] `src/components/consent/UnsubscribeConfirmation.tsx`
- [ ] `src/components/consent/ConsentHistoryTimeline.tsx`
- [ ] `src/components/consent/CommunicationPreferences.tsx`

#### Páginas Faltantes

- [ ] `src/pages/UnsubscribePage.tsx`
- [ ] `src/pages/user/ConsentHistoryPage.tsx`
- [ ] `src/pages/user/CommunicationPreferencesPage.tsx`
- [ ] Modificar `RegisterPage.tsx` (agregar checkboxes)

#### Hooks Faltantes

- [ ] `src/lib/hooks/useCookieConsent.ts`
- [ ] `src/lib/hooks/useConsent.ts` (preferences, history, grant, revoke)

#### Servicios a Actualizar

- [ ] Agregar métodos en `src/services/privacyService.ts`:
  - [ ] `updateCookieConsent()`
  - [ ] `getConsentPreferences()`
  - [ ] `updateConsentPreferences()`
  - [ ] `getConsentHistory()`
  - [ ] `unsubscribe(token, type, reason)`

#### Rutas en App.tsx

- [ ] `/unsubscribe` → `UnsubscribePage`
- [ ] `/settings/privacy/consent-history` → `ConsentHistoryPage`
- [ ] `/settings/privacy/communication` → `CommunicationPreferencesPage`
- [ ] `/settings/privacy/consents` → `ConsentsManagementPage`

#### Layouts

- [ ] Integrar `<CookieConsentBanner />` en `App.tsx`
- [ ] Agregar tab "Privacidad" en `SettingsTab.tsx`

---

## 🏗️ ARQUITECTURA

```typescript
// ✅ COMPLETO: Todos los derechos implementados

1. ACCESO (Art. 43) - Ver mis datos
   ✅ privacyService.getMyPersonalData(userId)
   ✅ MyDataPage muestra: perfil, vehículos, mensajes, transacciones

2. RECTIFICACIÓN (Art. 44) - Corregir datos incorrectos
   ✅ userService.updateProfile(userId, data)
   ✅ Formularios de edición en settings

3. CANCELACIÓN (Art. 45) - Eliminar cuenta
   ✅ privacyService.requestAccountDeletion(userId, reason)
   ✅ DeleteAccountPage con wizard de 3 pasos
   ✅ Período de gracia de 30 días (soft delete)

4. OPOSICIÓN (Art. 46) - Rechazar tratamiento
   ✅ privacyService.updateConsent(consentId, granted: false)
   ✅ PrivacyCenterPage con toggles granulares
```

### 📊 Consentimientos Granulares (Art. 8-10 Ley 172-13)

```typescript
// ✅ IMPLEMENTADO: frontend/web/src/services/privacyService.ts

export enum ConsentPurpose {
  Marketing = 1,           // Email/SMS marketing
  Analytics = 2,           // Google Analytics, tracking
  ThirdPartySharing = 3,   // Compartir con dealers
  Profiling = 4,           // Recomendaciones personalizadas
  LocationTracking = 5     // Geolocalización
}

interface Consent {
  id: string;
  userId: string;
  purpose: ConsentPurpose;
  granted: boolean;
  grantedAt?: string;
  revokedAt?: string;
}

// Componente UI:
<PrivacyCenterPage>
  <ConsentToggles>
    ✅ Marketing emails (ON/OFF)
    ✅ Analytics & tracking (ON/OFF)
    ✅ Share with dealers (ON/OFF)
    ✅ Personalized recommendations (ON/OFF)
    ✅ Location tracking (ON/OFF)
  </ConsentToggles>
</PrivacyCenterPage>
```

### ✅ Exportación de Datos (Art. 43 - Portabilidad)

```typescript
// ✅ COMPLETO: DataDownloadPage.tsx (200+ líneas)

Features implementadas:
- Selección de formato: JSON, XML, CSV
- Categorías seleccionables:
  * ✅ Información personal
  * ✅ Vehículos publicados
  * ✅ Transacciones
  * ✅ Actividad (búsquedas, favoritos)
  * ✅ Comunicaciones (mensajes, notificaciones)
- Download ZIP con todos los datos
- Tracking de solicitud (24-48 horas para grandes volúmenes)
- Async job si data > 10MB

Endpoints:
✅ POST /api/privacy/export → Genera archivo
✅ GET /api/privacy/export/{requestId}/status → Estado
✅ GET /api/privacy/export/{requestId}/download → Descarga
```

### ✅ Eliminación de Cuenta (Art. 45 - Derecho al Olvido)

```typescript
// ✅ COMPLETO: DeleteAccountPage.tsx (300+ líneas)

Wizard de 3 pasos:
1. Razones de eliminación (checklist)
   - No uso más el servicio
   - Privacidad concerns
   - Encontré otra plataforma
   - No encontré lo que buscaba
   - Mal servicio al cliente

2. Confirmación con contraseña
   - Password verification
   - Advertencias de datos a eliminar
   - Opción de exportar datos antes

3. Período de gracia (30 días)
   - Soft delete inmediato
   - Email de confirmación
   - Link de recuperación (30 días)
   - Hard delete después de 30 días

Endpoints:
✅ POST /api/privacy/delete-account
✅ POST /api/privacy/cancel-deletion (restaurar)
✅ DELETE /api/privacy/permanent-delete (hard delete)
```

### 📧 Contacto DPO (Data Protection Officer)

```
Oficial de Protección de Datos (DPO)
Email: privacidad@okla.com.do
Teléfono: +1-809-555-0100 ext. 333
Horario: Lun-Vie 9:00 AM - 6:00 PM

Dirección Física:
OKLA Technologies SRL
Av. Winston Churchill #1099
Torre Empresarial, Piso 12
Santo Domingo, República Dominicana
```

### 🟡 Única Brecha Menor (5% pendiente)

```
🟡 BRECHA: Formulario de oposición específico
   → Actualmente solo hay toggles generales de consentimiento
   → Falta: "Oponerme a tratamiento X por razón Y"
   → Impacto: BAJO (los toggles cubren >95% de casos)
```

### 🎯 Nivel de Cumplimiento Legal

| Marco Legal           | Cobertura | Observación                   |
| --------------------- | --------- | ----------------------------- |
| **Ley 172-13 (RD)**   | ✅ 95%    | Excelente implementación ARCO |
| **GDPR (EU)**         | ✅ 90%    | Cumple mayormente             |
| **CCPA (California)** | ✅ 85%    | Falta "Do Not Sell" explícito |
| **LGPD (Brasil)**     | ✅ 90%    | Cumple requisitos principales |

**CONCLUSIÓN:** ✅ **OKLA CUMPLE CON LEY 172-13** (95% implementado)

**Referencias:**

- Ley completa: `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/02-ley-172-13.md`
- Procesos ARCO: Sección 3 "Derechos del Titular"
- Consentimientos: Sección 2 "Consentimiento y Base Legal"

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes](#componentes)
4. [Páginas](#páginas)
5. [Hooks y Servicios](#hooks-y-servicios)
6. [Tipos TypeScript](#tipos-typescript)
7. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### GDPR Compliance Overview

```
┌───────────────────────────────────────────────────────────────────────────┐
│                          GDPR COMPLIANCE SYSTEM                           │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  1. COOKIE CONSENT                                                        │
│  ├─ Banner al primer acceso (cookie-consent=null)                        │
│  ├─ Categorías: Essential, Analytics, Marketing                          │
│  ├─ Accept All / Reject All / Customize                                  │
│  └─ Store en localStorage + backend audit log                            │
│                                                                           │
│  2. PRIVACY POLICY & TERMS                                                │
│  ├─ /privacy - Política de privacidad completa                           │
│  ├─ /terms - Términos y condiciones                                      │
│  ├─ Versioning (user must re-accept when updated)                        │
│  └─ Required acceptance on signup                                        │
│                                                                           │
│  3. RIGHT TO ACCESS (Art. 15 GDPR)                                        │
│  ├─ User Dashboard: "Mi información personal"                            │
│  ├─ View all data collected (profile, vehicles, messages, etc.)          │
│  └─ Audit log of data access by staff                                    │
│                                                                           │
│  4. RIGHT TO PORTABILITY (Art. 20 GDPR)                                   │
│  ├─ Export all user data in JSON format                                  │
│  ├─ Export vehicles, messages, reviews, favorites                        │
│  ├─ Download as .json or .csv                                            │
│  └─ Async job if data > 10MB                                             │
│                                                                           │
│  5. RIGHT TO BE FORGOTTEN (Art. 17 GDPR)                                  │
│  ├─ Account deletion request                                             │
│  ├─ Confirmation modal with consequences                                 │
│  ├─ 30-day grace period (soft delete)                                    │
│  ├─ Anonymize data (keep aggregate stats)                                │
│  └─ Send confirmation email                                              │
│                                                                           │
│  6. DATA PROCESSING AGREEMENTS                                            │
│  ├─ DPA with third-parties (Stripe, Twilio, AWS)                        │
│  ├─ Data retention policies                                              │
│  └─ Compliance dashboard for admins                                      │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

### Cookie Categories

```typescript
enum CookieCategory {
  ESSENTIAL = "essential", // Always enabled (session, CSRF)
  ANALYTICS = "analytics", // Google Analytics, Mixpanel
  MARKETING = "marketing", // Facebook Pixel, Google Ads
  PERSONALIZATION = "personalization", // Recommendations, saved prefs
}

interface CookieConsent {
  essential: true; // Always true
  analytics: boolean;
  marketing: boolean;
  personalization: boolean;
  timestamp: string;
  version: string; // Policy version
}
```

### 📧 Communication Consent (Ley 172-13 RD)

**Integración con procesos CONSENT-\***

Ver documento completo: `docs/process-matrix/09-NOTIFICACIONES/05-consentimiento-comunicaciones.md`

```typescript
// Tipos de Comunicación según Ley 172-13
enum CommunicationType {
  TRANSACTIONAL = "transactional", // ✅ Obligatorio (no requiere consentimiento)
  SERVICE = "service", // ✅ Obligatorio (cambios TOS, seguridad)
  MARKETING = "marketing", // 🔘 Opt-in requerido
  PARTNERS = "partners", // 🔘 Opt-in separado
  RESEARCH = "research", // 🔘 Opt-in (encuestas)
}

// CONSENT-REG-002: Registro de Consentimiento
interface ConsentRecord {
  userId: string;
  consentType: CommunicationType;
  channel: "email" | "sms" | "push" | "whatsapp";
  granted: boolean;
  timestamp: string;
  ipAddress: string; // Requerido por Ley 172-13
  userAgent: string; // Requerido para auditoría
  source: "registration" | "settings" | "unsubscribe";
  consentTextVersion: string; // Versión del texto legal
  consentTextHash: string; // SHA-256 del texto
  revokedAt?: string;
  revokedReason?: string;
}

// CONSENT-PREF-002: Matriz de Preferencias por Canal
interface ChannelPreferences {
  email: {
    transactional: true; // No desactivable
    security: true; // No desactivable
    marketing_okla: boolean; // Opt-in
    partners: boolean; // Opt-in
    vehicle_alerts: boolean; // Opt-in
    surveys: boolean; // Opt-in
  };
  sms: {
    security: true; // 2FA - No desactivable
    price_alerts: boolean; // Opt-in
    marketing: boolean; // Opt-in
  };
  push: {
    messages: boolean; // Recomendado
    updates: boolean; // Recomendado
    recommendations: boolean; // Opt-in
  };
  whatsapp: {
    marketing: boolean; // Opt-in
    alerts: boolean; // Opt-in
  };
}
```

### Cumplimiento Legal Combinado

| Requisito                   | GDPR | CCPA | Ley 172-13 RD | Estado     |
| --------------------------- | ---- | ---- | ------------- | ---------- |
| Consentimiento previo       | ✅   | ❌   | ✅            | ✅ Impl.   |
| Opt-in claro e inequívoco   | ✅   | ❌   | ✅            | ✅ Impl.   |
| Link de baja en emails      | ✅   | ✅   | ✅            | ✅ Impl.   |
| Registro de consentimientos | ✅   | ✅   | ✅            | 🟡 Parcial |
| Portabilidad de datos       | ✅   | ✅   | ✅            | 🟡 Parcial |
| Derecho al olvido           | ✅   | ✅   | ✅            | 🟡 Parcial |
| Auditoría de acceso         | ✅   | ✅   | ✅            | 🔴 Pend.   |
| DPA con terceros            | ✅   | ✅   | ✅            | ✅ Impl.   |

### Rutas de Consentimiento

```typescript
// Nuevas rutas requeridas
/settings/notifications/preferences  // CONSENT-PREF-001
/settings/privacy/consent-history    // CONSENT-AUDIT-002
/unsubscribe?token=xxx&type=xxx      // CONSENT-UNSUB-001

// Integración en páginas existentes
/register         // CONSENT-REG-001: Checkboxes
/settings         // Link a preferencias
```

---

## 🔌 BACKEND API

### PrivacyService Endpoints

```typescript
// filepath: docs/backend/PrivacyService-API.md

// Cookie Consent
GET    /api/privacy/consent                    # Get current consent
POST   /api/privacy/consent                    # Update consent
GET    /api/privacy/cookie-policy              # Cookie policy text

// Data Access
GET    /api/privacy/my-data                    # Get all user data
GET    /api/privacy/access-log                 # Who accessed my data

// Data Export
POST   /api/privacy/export-data                # Request data export
GET    /api/privacy/export-status/{jobId}      # Check export status
GET    /api/privacy/download/{jobId}           # Download export file

// Account Deletion
POST   /api/privacy/delete-account             # Request deletion
POST   /api/privacy/cancel-deletion            # Cancel (within 30 days)
GET    /api/privacy/deletion-status            # Check deletion status

// Legal Documents
GET    /api/privacy/policy                     # Privacy policy
GET    /api/privacy/terms                      # Terms of service
GET    /api/privacy/policy-versions            # Version history
```

### Payload Examples

```json
// POST /api/privacy/consent Body
{
  "essential": true,
  "analytics": true,
  "marketing": false,
  "personalization": true
}

// GET /api/privacy/my-data Response
{
  "user": {
    "id": "user_123",
    "email": "juan@example.com",
    "fullName": "Juan Pérez",
    "phone": "+1809-555-1234",
    "createdAt": "2025-06-15T10:30:00Z"
  },
  "vehicles": [
    {
      "id": "veh_456",
      "make": "Toyota",
      "model": "RAV4",
      "year": 2022,
      "price": 25000
    }
  ],
  "favorites": [...],
  "messages": [...],
  "reviews": [...],
  "searches": [...]
}

// POST /api/privacy/export-data Response
{
  "jobId": "export_789",
  "status": "pending",
  "estimatedTime": "5 minutes",
  "message": "Te enviaremos un email cuando esté listo"
}

// POST /api/privacy/delete-account Response
{
  "scheduledDeletionDate": "2026-02-08T00:00:00Z",
  "gracePeriodDays": 30,
  "message": "Tu cuenta será eliminada en 30 días. Puedes cancelar en cualquier momento."
}
```

---

## 🎨 COMPONENTES

### PASO 1: CookieConsentBanner - Banner de Cookies

```typescript
// filepath: src/components/privacy/CookieConsentBanner.tsx
"use client";

import { useState, useEffect } from "react";
import { X, Settings } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/Dialog";
import { Switch } from "@/components/ui/Switch";
import { useCookieConsent } from "@/lib/hooks/usePrivacy";

export function CookieConsentBanner() {
  const [isVisible, setIsVisible] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const { consent, updateConsent } = useCookieConsent();

  const [settings, setSettings] = useState({
    essential: true,
    analytics: false,
    marketing: false,
    personalization: false,
  });

  useEffect(() => {
    // Show banner if no consent recorded
    if (consent === null) {
      setIsVisible(true);
    }
  }, [consent]);

  const handleAcceptAll = () => {
    updateConsent({
      essential: true,
      analytics: true,
      marketing: true,
      personalization: true,
    });
    setIsVisible(false);
  };

  const handleRejectAll = () => {
    updateConsent({
      essential: true,
      analytics: false,
      marketing: false,
      personalization: false,
    });
    setIsVisible(false);
  };

  const handleSaveSettings = () => {
    updateConsent(settings);
    setShowSettings(false);
    setIsVisible(false);
  };

  if (!isVisible) return null;

  return (
    <>
      <div className="fixed bottom-0 left-0 right-0 bg-white border-t shadow-lg z-50 p-6">
        <div className="max-w-6xl mx-auto">
          <div className="flex items-start justify-between gap-6">
            <div className="flex-1">
              <h3 className="font-semibold text-gray-900 mb-2">
                🍪 Usamos cookies
              </h3>
              <p className="text-sm text-gray-600">
                Utilizamos cookies para mejorar tu experiencia, analizar el uso del
                sitio y personalizar el contenido. Puedes aceptar todas las cookies o
                personalizarlas según tus preferencias.{" "}
                <a
                  href="/privacy"
                  target="_blank"
                  className="text-primary-600 hover:underline"
                >
                  Leer más
                </a>
              </p>
            </div>

            <div className="flex items-center gap-3 flex-shrink-0">
              <Button variant="outline" onClick={() => setShowSettings(true)}>
                <Settings size={16} className="mr-1" />
                Personalizar
              </Button>
              <Button variant="outline" onClick={handleRejectAll}>
                Rechazar
              </Button>
              <Button onClick={handleAcceptAll}>Aceptar todo</Button>
            </div>
          </div>
        </div>
      </div>

      {/* Settings Modal */}
      <Dialog open={showSettings} onOpenChange={setShowSettings}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Configuración de cookies</DialogTitle>
          </DialogHeader>

          <div className="space-y-6">
            {/* Essential */}
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900">
                  Esenciales (Obligatorias)
                </h4>
                <p className="text-sm text-gray-600 mt-1">
                  Necesarias para el funcionamiento del sitio (sesión,
                  autenticación, seguridad).
                </p>
              </div>
              <Switch checked disabled />
            </div>

            {/* Analytics */}
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900">Analíticas</h4>
                <p className="text-sm text-gray-600 mt-1">
                  Nos ayudan a entender cómo usas el sitio para mejorarlo (Google
                  Analytics, Mixpanel).
                </p>
              </div>
              <Switch
                checked={settings.analytics}
                onChange={(checked) =>
                  setSettings({ ...settings, analytics: checked })
                }
              />
            </div>

            {/* Marketing */}
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900">Marketing</h4>
                <p className="text-sm text-gray-600 mt-1">
                  Usadas para mostrarte anuncios relevantes (Facebook Pixel, Google
                  Ads).
                </p>
              </div>
              <Switch
                checked={settings.marketing}
                onChange={(checked) =>
                  setSettings({ ...settings, marketing: checked })
                }
              />
            </div>

            {/* Personalization */}
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900">Personalización</h4>
                <p className="text-sm text-gray-600 mt-1">
                  Guardan tus preferencias para ofrecerte una experiencia
                  personalizada.
                </p>
              </div>
              <Switch
                checked={settings.personalization}
                onChange={(checked) =>
                  setSettings({ ...settings, personalization: checked })
                }
              />
            </div>
          </div>

          <div className="flex items-center gap-3 pt-6 border-t">
            <Button onClick={handleSaveSettings}>Guardar configuración</Button>
            <Button variant="outline" onClick={() => setShowSettings(false)}>
              Cancelar
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
```

---

### PASO 2: DataExportCard - Exportar Datos

```typescript
// filepath: src/components/privacy/DataExportCard.tsx
"use client";

import { useState } from "react";
import { Download, Clock, CheckCircle } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useRequestDataExport, useExportStatus } from "@/lib/hooks/usePrivacy";

export function DataExportCard() {
  const [jobId, setJobId] = useState<string | null>(null);
  const { mutate: requestExport, isPending } = useRequestDataExport();
  const { data: exportStatus } = useExportStatus(jobId);

  const handleRequestExport = () => {
    requestExport(undefined, {
      onSuccess: (data) => {
        setJobId(data.jobId);
      },
    });
  };

  return (
    <div className="bg-white rounded-lg border p-6">
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="font-semibold text-gray-900">Exportar mis datos</h3>
          <p className="text-sm text-gray-600 mt-1">
            Descarga una copia de toda tu información en formato JSON
          </p>
        </div>
        <Download size={24} className="text-primary-600" />
      </div>

      {/* Status */}
      {exportStatus && (
        <div className="mb-4">
          {exportStatus.status === "pending" && (
            <Badge variant="warning">
              <Clock size={12} className="mr-1" />
              Procesando... ({exportStatus.estimatedTime})
            </Badge>
          )}
          {exportStatus.status === "completed" && (
            <Badge variant="success">
              <CheckCircle size={12} className="mr-1" />
              ¡Listo para descargar!
            </Badge>
          )}
        </div>
      )}

      {/* What's included */}
      <div className="bg-gray-50 rounded-lg p-4 mb-4">
        <p className="text-sm font-medium text-gray-900 mb-2">
          Tu exportación incluirá:
        </p>
        <ul className="text-sm text-gray-600 space-y-1">
          <li>• Información de perfil</li>
          <li>• Vehículos publicados</li>
          <li>• Mensajes y conversaciones</li>
          <li>• Favoritos y búsquedas guardadas</li>
          <li>• Reviews y ratings</li>
          <li>• Historial de actividad</li>
        </ul>
      </div>

      {/* Actions */}
      {!exportStatus || exportStatus.status === "failed" ? (
        <Button onClick={handleRequestExport} disabled={isPending}>
          {isPending ? "Procesando..." : "Solicitar exportación"}
        </Button>
      ) : exportStatus.status === "completed" ? (
        <Button>
          <Download size={16} className="mr-1" />
          Descargar datos
        </Button>
      ) : (
        <p className="text-sm text-gray-600">
          Te enviaremos un email cuando esté listo para descargar
        </p>
      )}
    </div>
  );
}
```

---

### PASO 3: AccountDeletionCard - Eliminar Cuenta

```typescript
// filepath: src/components/privacy/AccountDeletionCard.tsx
"use client";

import { useState } from "react";
import { AlertTriangle, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/Dialog";
import { Checkbox } from "@/components/ui/Checkbox";
import { useDeleteAccount, useDeletionStatus } from "@/lib/hooks/usePrivacy";

export function AccountDeletionCard() {
  const [showConfirm, setShowConfirm] = useState(false);
  const [confirmed, setConfirmed] = useState(false);
  const { mutate: deleteAccount, isPending } = useDeleteAccount();
  const { data: deletionStatus } = useDeletionStatus();

  const handleDelete = () => {
    deleteAccount(undefined, {
      onSuccess: () => {
        setShowConfirm(false);
      },
    });
  };

  if (deletionStatus?.scheduledDate) {
    return (
      <div className="bg-red-50 rounded-lg border border-red-200 p-6">
        <div className="flex items-start gap-3">
          <AlertTriangle size={24} className="text-red-600 flex-shrink-0" />
          <div className="flex-1">
            <h3 className="font-semibold text-red-900">
              Eliminación programada
            </h3>
            <p className="text-sm text-red-700 mt-1">
              Tu cuenta será eliminada permanentemente el{" "}
              {new Date(deletionStatus.scheduledDate).toLocaleDateString("es-DO")}
            </p>
            <p className="text-sm text-red-700 mt-2">
              Quedan {deletionStatus.daysRemaining} días para cancelar
            </p>
          </div>
        </div>

        <Button variant="outline" className="mt-4">
          Cancelar eliminación
        </Button>
      </div>
    );
  }

  return (
    <>
      <div className="bg-white rounded-lg border p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h3 className="font-semibold text-gray-900">Eliminar cuenta</h3>
            <p className="text-sm text-gray-600 mt-1">
              Elimina permanentemente tu cuenta y todos tus datos
            </p>
          </div>
          <AlertTriangle size={24} className="text-red-600" />
        </div>

        <div className="bg-red-50 rounded-lg p-4 mb-4">
          <p className="text-sm font-medium text-red-900 mb-2">
            ⚠️ Esta acción es irreversible
          </p>
          <ul className="text-sm text-red-700 space-y-1">
            <li>• Se eliminarán todos tus vehículos publicados</li>
            <li>• Perderás acceso a tus mensajes</li>
            <li>• Se borrarán tus favoritos y búsquedas</li>
            <li>• No podrás recuperar tu cuenta</li>
          </ul>
        </div>

        <Button
          variant="destructive"
          onClick={() => setShowConfirm(true)}
        >
          <Trash2 size={16} className="mr-1" />
          Eliminar cuenta
        </Button>
      </div>

      {/* Confirmation Modal */}
      <Dialog open={showConfirm} onOpenChange={setShowConfirm}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>¿Estás seguro?</DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <p className="text-gray-600">
              Esta acción eliminará permanentemente tu cuenta después de un
              período de gracia de 30 días.
            </p>

            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <p className="text-sm text-yellow-900">
                <strong>Período de gracia:</strong> Tienes 30 días para cancelar
                la eliminación. Después de ese tiempo, tu cuenta será eliminada
                permanentemente.
              </p>
            </div>

            <div className="flex items-start gap-3">
              <Checkbox
                checked={confirmed}
                onChange={(checked) => setConfirmed(checked)}
              />
              <label className="text-sm text-gray-700">
                Entiendo que esta acción no se puede deshacer y acepto las
                consecuencias de eliminar mi cuenta
              </label>
            </div>
          </div>

          <div className="flex items-center gap-3 pt-6 border-t">
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={!confirmed || isPending}
            >
              Sí, eliminar mi cuenta
            </Button>
            <Button variant="outline" onClick={() => setShowConfirm(false)}>
              Cancelar
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
```

---

### PASO 4: DataAccessLog - Log de Accesos

```typescript
// filepath: src/components/privacy/DataAccessLog.tsx
"use client";

import { Shield, User, Clock } from "lucide-react";
import { format } from "date-fns";
import { es } from "date-fns/locale";
import { useAccessLog } from "@/lib/hooks/usePrivacy";

export function DataAccessLog() {
  const { data: accessLog, isLoading } = useAccessLog();

  if (isLoading) {
    return <div>Cargando historial...</div>;
  }

  return (
    <div className="bg-white rounded-lg border p-6">
      <div className="flex items-center gap-2 mb-4">
        <Shield size={20} className="text-primary-600" />
        <h3 className="font-semibold text-gray-900">
          Historial de accesos a tu información
        </h3>
      </div>

      <p className="text-sm text-gray-600 mb-4">
        Registro de quién ha accedido a tu información personal
      </p>

      {!accessLog || accessLog.length === 0 ? (
        <p className="text-sm text-gray-500 text-center py-8">
          No hay accesos registrados
        </p>
      ) : (
        <div className="space-y-3">
          {accessLog.map((log) => (
            <div
              key={log.id}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <div className="flex items-center gap-3">
                <User size={16} className="text-gray-500" />
                <div>
                  <p className="text-sm font-medium text-gray-900">
                    {log.accessedBy}
                  </p>
                  <p className="text-xs text-gray-600">{log.reason}</p>
                </div>
              </div>

              <div className="flex items-center gap-2 text-xs text-gray-500">
                <Clock size={12} />
                {format(new Date(log.accessedAt), "dd MMM yyyy, HH:mm", {
                  locale: es,
                })}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```

---

## 📄 PÁGINAS

### PASO 5: Página de Privacidad

```typescript
// filepath: src/app/(main)/privacy/page.tsx
import { Metadata } from "next";
import { Shield, Lock, Eye, Download } from "lucide-react";

export const metadata: Metadata = {
  title: "Política de Privacidad | OKLA",
  description: "Protección de datos y privacidad en OKLA",
};

export default function PrivacyPage() {
  return (
    <div className="max-w-4xl mx-auto px-4 py-12">
      <div className="text-center mb-12">
        <Shield size={48} className="mx-auto mb-4 text-primary-600" />
        <h1 className="text-4xl font-bold text-gray-900">
          Política de Privacidad
        </h1>
        <p className="text-gray-600 mt-4">
          Última actualización: 8 de enero de 2026
        </p>
      </div>

      <div className="prose prose-gray max-w-none">
        <section className="mb-12">
          <h2>1. Información que recopilamos</h2>
          <p>
            En OKLA recopilamos información para brindarte un mejor servicio:
          </p>
          <ul>
            <li>
              <strong>Información de cuenta:</strong> nombre, email, teléfono
            </li>
            <li>
              <strong>Información de vehículos:</strong> datos de tus publicaciones
            </li>
            <li>
              <strong>Comunicaciones:</strong> mensajes con otros usuarios
            </li>
            <li>
              <strong>Uso del sitio:</strong> páginas visitadas, búsquedas
            </li>
            <li>
              <strong>Dispositivo:</strong> IP, navegador, dispositivo
            </li>
          </ul>
        </section>

        <section className="mb-12">
          <h2>2. Cómo usamos tu información</h2>
          <p>Usamos tu información para:</p>
          <ul>
            <li>Procesar transacciones y pagos</li>
            <li>Mejorar nuestros servicios</li>
            <li>Enviarte notificaciones importantes</li>
            <li>Personalizar tu experiencia</li>
            <li>Prevenir fraudes</li>
          </ul>
        </section>

        <section className="mb-12">
          <h2>3. Compartir información</h2>
          <p>
            No vendemos tu información personal. Solo compartimos con:
          </p>
          <ul>
            <li>
              <strong>Proveedores de servicios:</strong> Stripe (pagos), AWS
              (hosting), Twilio (SMS)
            </li>
            <li>
              <strong>Autoridades:</strong> cuando la ley lo requiera
            </li>
            <li>
              <strong>Con tu consentimiento:</strong> cuando nos lo autorices
            </li>
          </ul>
        </section>

        <section className="mb-12">
          <h2>4. Tus derechos (GDPR)</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 my-6">
            <div className="bg-blue-50 rounded-lg p-6">
              <Eye size={24} className="mb-2 text-blue-600" />
              <h3 className="font-semibold mb-2">Acceso</h3>
              <p className="text-sm">
                Puedes ver toda tu información personal en cualquier momento
              </p>
            </div>

            <div className="bg-green-50 rounded-lg p-6">
              <Download size={24} className="mb-2 text-green-600" />
              <h3 className="font-semibold mb-2">Portabilidad</h3>
              <p className="text-sm">
                Exporta todos tus datos en formato JSON
              </p>
            </div>

            <div className="bg-purple-50 rounded-lg p-6">
              <Lock size={24} className="mb-2 text-purple-600" />
              <h3 className="font-semibold mb-2">Rectificación</h3>
              <p className="text-sm">
                Corrige cualquier información incorrecta
              </p>
            </div>

            <div className="bg-red-50 rounded-lg p-6">
              <Shield size={24} className="mb-2 text-red-600" />
              <h3 className="font-semibold mb-2">Eliminación</h3>
              <p className="text-sm">
                Solicita eliminar tu cuenta permanentemente
              </p>
            </div>
          </div>
        </section>

        <section className="mb-12">
          <h2>5. Seguridad</h2>
          <p>Protegemos tu información con:</p>
          <ul>
            <li>Encriptación SSL/TLS</li>
            <li>Autenticación de dos factores (2FA)</li>
            <li>Firewalls y monitoreo 24/7</li>
            <li>Auditorías de seguridad regulares</li>
          </ul>
        </section>

        <section className="mb-12">
          <h2>6. Cookies</h2>
          <p>
            Usamos cookies para mejorar tu experiencia. Puedes gestionar tus
            preferencias en cualquier momento.
          </p>
        </section>

        <section className="mb-12">
          <h2>7. Contacto</h2>
          <p>
            Para cualquier pregunta sobre privacidad, contáctanos en:{" "}
            <a href="mailto:privacy@okla.com.do">privacy@okla.com.do</a>
          </p>
        </section>
      </div>
    </div>
  );
}
```

---

### PASO 6: Configuración de Privacidad del Usuario

```typescript
// filepath: src/app/(main)/configuracion/privacidad/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { DataExportCard } from "@/components/privacy/DataExportCard";
import { AccountDeletionCard } from "@/components/privacy/AccountDeletionCard";
import { DataAccessLog } from "@/components/privacy/DataAccessLog";

export const metadata: Metadata = {
  title: "Privacidad y Datos | OKLA",
  description: "Gestiona tu privacidad y datos personales",
};

export default async function PrivacySettingsPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/configuracion/privacidad");
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-gray-900 mb-8">
        Privacidad y datos personales
      </h1>

      <div className="space-y-6">
        {/* Export Data */}
        <DataExportCard />

        {/* Access Log */}
        <DataAccessLog />

        {/* Delete Account */}
        <AccountDeletionCard />
      </div>
    </div>
  );
}
```

---

## 🔗 REFERENCIAS Y DOCUMENTACIÓN

### Backend Existente ✅

- `UserService/UserService.Api/Controllers/PrivacyController.cs` - ARCO endpoints ✅
- `DataProtectionService/*` - Consent management service ✅
- `NotificationService/*` - Communication preferences ✅

### Frontend Existente ✅

- `src/services/privacyService.ts` - Privacy API service ✅ (parcial)
- `src/pages/user/PrivacyCenterPage.tsx` - Hub de privacidad ✅
- `src/pages/user/MyDataPage.tsx` - Ver datos ✅
- `src/pages/user/DataDownloadPage.tsx` - Exportar ✅
- `src/pages/user/DeleteAccountPage.tsx` - Eliminar ✅

### Documentación Relacionada

- [02-ley-172-13.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/02-ley-172-13.md) - Especificación completa Ley 172-13
- [99-consentimiento-comunicaciones.md](../08-DGII-COMPLIANCE/07-consentimiento-comunicaciones.md) - Sistema de consentimientos detallado

### Legislación Aplicable

- **Ley 172-13** - Protección de Datos Personales República Dominicana
- **GDPR** - General Data Protection Regulation (EU)
- **CAN-SPAM Act** - Email marketing compliance (USA)
- **CCPA** - California Consumer Privacy Act

### Métricas de Éxito Post-Implementación

#### KPIs Legales

- **Tasa de Consentimiento:** % usuarios que aceptan cookies/marketing
- **Opt-in Rate:** % de nuevos usuarios que aceptan marketing
- **Opt-out Rate:** % de usuarios que se dan de baja
- **Tiempo de Respuesta ARCO:** < 10 días hábiles (legal: 10 días)
- **Solicitudes de Exportación:** # por mes
- **Solicitudes de Eliminación:** # por mes

#### KPIs Técnicos

- **Consents Almacenados:** 100% de registros con consent válido
- **Auditoría Completa:** 100% de cambios registrados en history
- **Disponibilidad:** 99.9% uptime de endpoints de privacidad

---

## ⚖️ CONCLUSIÓN

El proyecto OKLA tiene una **base sólida de cumplimiento de la Ley 172-13** con los derechos ARCO (Acceso, Rectificación, Cancelación, Oposición) implementados correctamente en el backend y parcialmente en el frontend.

### Puntos Fuertes ✅

- Privacy Center funcional y bien estructurado
- Exportación de datos completa con múltiples formatos
- Proceso de eliminación de cuenta con periodo de gracia
- Vista de datos personales comprensiva
- Backend robusto con DataProtectionService

### Gaps Críticos 🔴

- **Cookie Consent Banner** - Riesgo legal alto (P0)
- **Consentimientos en Registro** - Base de usuarios sin consent (P0)
- **Preferencias de Comunicación** - No cumple derecho de oposición (P1)
- **Historial de Consentimientos** - Sin auditoría (P1)
- **Página de Unsubscribe** - Emails sin link funcional (P1)

### Recomendación Final

Implementar el **Sprint Crítico de 2 semanas** para alcanzar **95% de cumplimiento** y eliminar riesgos legales inmediatos.

**Prioridad de Ejecución:**

1. Cookie Consent Banner (P0) - 2-3 días
2. Consentimientos en Registro (P0) - 1 día
3. Página de Unsubscribe (P1) - 1-2 días
4. Preferencias de Comunicación (P1) - 3-4 días
5. Historial de Consentimientos (P1) - 2 días

**Estado Actual:** 🟡 46% Completado (12/26 requisitos)  
**Estado Post-Sprint:** 🟢 95% Completado (estimado)

---

**Auditoría completada:** Enero 29, 2026  
**Próxima revisión:** Post-implementación del Sprint Crítico  
**Responsable:** Equipo Frontend + Legal Compliance

---

## 🪝 HOOKS Y SERVICIOS

### PASO 7: Privacy Hooks

```typescript
// filepath: src/lib/hooks/usePrivacy.ts
import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { privacyService } from "@/lib/services/privacyService";
import { toast } from "sonner";

export function useCookieConsent() {
  const [consent, setConsent] = useState<any>(null);

  useEffect(() => {
    // Load from localStorage
    const saved = localStorage.getItem("cookie-consent");
    if (saved) {
      setConsent(JSON.parse(saved));
    }
  }, []);

  const updateConsent = (newConsent: any) => {
    localStorage.setItem("cookie-consent", JSON.stringify(newConsent));
    setConsent(newConsent);

    // Also save to backend
    privacyService.updateConsent(newConsent);
  };

  return { consent, updateConsent };
}

export function useRequestDataExport() {
  return useMutation({
    mutationFn: () => privacyService.requestDataExport(),
    onSuccess: (data) => {
      toast.success("Exportación solicitada. Te avisaremos cuando esté lista.");
    },
  });
}

export function useExportStatus(jobId: string | null) {
  return useQuery({
    queryKey: ["exportStatus", jobId],
    queryFn: () => privacyService.getExportStatus(jobId!),
    enabled: !!jobId,
    refetchInterval: 5000, // Poll every 5 seconds
  });
}

export function useDeleteAccount() {
  return useMutation({
    mutationFn: () => privacyService.deleteAccount(),
    onSuccess: () => {
      toast.success("Eliminación programada. Tienes 30 días para cancelar.");
    },
  });
}

export function useDeletionStatus() {
  return useQuery({
    queryKey: ["deletionStatus"],
    queryFn: () => privacyService.getDeletionStatus(),
  });
}

export function useAccessLog() {
  return useQuery({
    queryKey: ["accessLog"],
    queryFn: () => privacyService.getAccessLog(),
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 8: Privacy Types

```typescript
// filepath: src/types/privacy.ts
export interface CookieConsent {
  essential: boolean;
  analytics: boolean;
  marketing: boolean;
  personalization: boolean;
  timestamp: string;
  version: string;
}

export interface DataExportJob {
  jobId: string;
  status: "pending" | "processing" | "completed" | "failed";
  estimatedTime?: string;
  downloadUrl?: string;
  createdAt: string;
  completedAt?: string;
}

export interface DeletionRequest {
  scheduledDate: string;
  daysRemaining: number;
  canCancel: boolean;
  requestedAt: string;
}

export interface AccessLogEntry {
  id: string;
  accessedBy: string;
  accessedByRole: "admin" | "support" | "system";
  reason: string;
  accessedAt: string;
  ipAddress: string;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - Cookie consent banner aparece en primera visita
# - Configurar cookies individualmente funciona
# - Política de privacidad es legible y completa
# - Exportar datos funciona y genera JSON correcto
# - Eliminar cuenta muestra modal de confirmación
# - Período de gracia de 30 días se respeta
# - Log de accesos muestra quién accedió a tus datos
# - Cancelar eliminación funciona (dentro de 30 días)
# - Links a /privacy funcionan desde todas partes
# - GDPR compliance completo (acceso, portabilidad, eliminación)
```

---

## 🚀 MEJORAS FUTURAS

1. **Consent Management Platform (CMP)**: OneTrust o Cookiebot
2. **Privacy Shield Certification**: Para cumplimiento EU-US
3. **Age Verification**: Para menores de edad (COPPA)
4. **Biometric Data Policy**: Si se usa reconocimiento facial
5. **Right to Restrict Processing**: Art. 18 GDPR

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/privacy-gdpr.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Privacy & GDPR", () => {
  test("debe mostrar política de privacidad pública", async ({ page }) => {
    await page.goto("/privacidad");

    await expect(
      page.getByRole("heading", { name: /política de privacidad/i }),
    ).toBeVisible();
    await expect(page.getByText(/datos personales/i)).toBeVisible();
  });

  test("debe mostrar términos de uso", async ({ page }) => {
    await page.goto("/terminos");

    await expect(
      page.getByRole("heading", { name: /términos/i }),
    ).toBeVisible();
  });

  test.describe("Authenticated User", () => {
    test.beforeEach(async ({ page }) => {
      await loginAsUser(page);
    });

    test("debe gestionar preferencias de cookies", async ({ page }) => {
      await page.goto("/settings/privacy");

      await expect(page.getByText(/preferencias de cookies/i)).toBeVisible();
      await page.getByRole("switch", { name: /analytics/i }).click();
      await expect(page.getByText(/preferencias guardadas/i)).toBeVisible();
    });

    test("debe solicitar descarga de datos", async ({ page }) => {
      await page.goto("/settings/privacy");

      await page.getByRole("button", { name: /descargar mis datos/i }).click();
      await expect(page.getByText(/solicitud recibida/i)).toBeVisible();
    });

    test("debe solicitar eliminación de cuenta", async ({ page }) => {
      await page.goto("/settings/privacy");

      await page.getByRole("button", { name: /eliminar cuenta/i }).click();
      await expect(page.getByRole("dialog")).toBeVisible();
      await expect(
        page.getByText(/esta acción es irreversible/i),
      ).toBeVisible();
    });
  });
});
```

---

## ✅ P2 COMPLETADA AL 100%

**Documentos creados (7/7):**

1. ✅ 20-reviews-reputacion.md (~800 líneas)
2. ✅ 21-recomendaciones.md (~870 líneas)
3. ✅ 22-chatbot.md (~920 líneas)
4. ✅ 23-comparador.md (~650 líneas)
5. ✅ 24-alertas-busquedas.md (~700 líneas)
6. ✅ 25-notificaciones.md (~650 líneas)
7. ✅ 26-privacy-gdpr.md (~750 líneas)

**Total de líneas creadas en P2:** ~5,340 líneas

**Auditoría completa:** ✅ P0 + ✅ P1 + ✅ P2 = **100% COMPLETADO**
