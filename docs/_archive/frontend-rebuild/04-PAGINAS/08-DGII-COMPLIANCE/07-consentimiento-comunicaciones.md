---
title: "37. Consentimiento de Comunicaciones (Ley 172-13 RD)"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["AuthService", "UserService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 37. Consentimiento de Comunicaciones (Ley 172-13 RD)

**Objetivo:** Implementación completa del sistema de consentimiento de comunicaciones cumpliendo con la Ley 172-13 de Protección de Datos Personales de República Dominicana, incluyendo opt-in/opt-out por canal, registro de auditoría, historial de cambios y cumplimiento CAN-SPAM/GDPR.

**Prioridad:** P1 (Alta - Legal Compliance)  
**Complejidad:** 🟡 Media (Legal compliance, Audit logs, Multiple channels)  
**Dependencias:** NotificationService (✅), AuthService (✅), UserService (✅)  
**Última Auditoría:** Enero 29, 2026

---

## 🔍 AUDITORÍA DE IMPLEMENTACIÓN (Enero 29, 2026)

### 📊 Estado de Cumplimiento

| Componente                       | Especificación | Frontend UI | Estado   |
| -------------------------------- | -------------- | ----------- | -------- |
| **Consentimientos en Registro**  | ✅             | 🔴 0%       | 🔴 FALTA |
| **Preferencias de Comunicación** | ✅             | 🔴 0%       | 🔴 FALTA |
| **Historial de Consentimientos** | ✅             | 🔴 0%       | 🔴 FALTA |
| **Página Unsubscribe**           | ✅             | 🔴 0%       | 🔴 FALTA |
| **Backend API Consents**         | ✅             | -           | ✅ IMPL. |

### 🔴 Gaps Críticos Identificados

#### 1. Consentimientos en Registro (P0)

**Estado:** 🔴 NO IMPLEMENTADO  
**Impacto:** Usuarios se registran sin consentimiento válido

❌ **Falta:**

- Checkboxes en formulario de registro
- Validación de términos obligatorios
- Envío de consents al backend

**Código Necesario:**

```tsx
// RegisterPage.tsx - FALTA AGREGAR
<div className="space-y-3 border-t pt-4 mt-4">
  <div className="flex items-start">
    <input
      type="checkbox"
      id="terms"
      {...register("acceptTerms", { required: true })}
    />
    <label htmlFor="terms" className="ml-2 text-sm">
      Acepto los <a href="/terms">Términos de Servicio</a> *
    </label>
  </div>

  <div className="flex items-start">
    <input type="checkbox" id="marketing" {...register("acceptMarketing")} />
    <label htmlFor="marketing" className="ml-2 text-sm">
      Quiero recibir ofertas por email (opcional)
    </label>
  </div>
</div>
```

#### 2. Preferencias de Comunicación (P1)

**Estado:** 🔴 NO IMPLEMENTADO  
**Impacto:** No se puede cumplir derecho de oposición (Art. 9 Ley 172-13)

❌ **Falta:**

- Página `/settings/notifications/preferences`
- Toggles por canal y tipo
- Integración con backend

#### 3. Historial de Consentimientos (P1)

**Estado:** 🔴 NO IMPLEMENTADO  
**Impacto:** Sin auditoría de consentimientos

❌ **Falta:**

- Página `/settings/privacy/consent-history`
- Timeline de cambios
- Log de IP y fechas

#### 4. Unsubscribe (P1)

**Estado:** 🔴 NO IMPLEMENTADO  
**Impacto:** Violación CAN-SPAM Act

❌ **Falta:**

---

## 🎨 WIREFRAME - PREFERENCIAS DE COMUNICACIÓN

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER (Navbar)                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────┐                                                    │
│  │ SIDEBAR             │   PREFERENCIAS DE COMUNICACIÓN                     │
│  │ 👤 Mi Perfil        │   ─────────────────────────────                    │
│  │ 🔒 Seguridad        │                                                    │
│  │ 📧 Comunicaciones ◀ │   Controla cómo te contactamos. Puedes cambiar    │
│  │ 🔔 Notificaciones   │   tus preferencias en cualquier momento.          │
│  │ 🔐 Privacidad       │                                                    │
│  │ 🗑️ Eliminar Cuenta  │   ┌──────────────────────────────────────────────┐│
│  └─────────────────────┘   │ EMAIL                                         ││
│                            │                                               ││
│                            │ ┌─────────────────────────────┐ ┌──────────┐  ││
│                            │ │ Actualizaciones de cuenta   │ │ ✅ ON    │  ││
│                            │ │ Cambios en tu cuenta...     │ │          │  ││
│                            │ └─────────────────────────────┘ └──────────┘  ││
│                            │                                               ││
│                            │ ┌─────────────────────────────┐ ┌──────────┐  ││
│                            │ │ Ofertas y promociones       │ │ ⬜ OFF   │  ││
│                            │ │ Descuentos y ofertas...     │ │          │  ││
│                            │ └─────────────────────────────┘ └──────────┘  ││
│                            │                                               ││
│                            │ ┌─────────────────────────────┐ ┌──────────┐  ││
│                            │ │ Boletín semanal             │ │ ✅ ON    │  ││
│                            │ │ Resumen de nuevos vehículos │ │          │  ││
│                            │ └─────────────────────────────┘ └──────────┘  ││
│                            └──────────────────────────────────────────────┘│
│                                                                              │
│                            ┌──────────────────────────────────────────────┐│
│                            │ SMS / WHATSAPP                                ││
│                            │                                               ││
│                            │ ┌─────────────────────────────┐ ┌──────────┐  ││
│                            │ │ Alertas de seguridad        │ │ ✅ ON 🔒 │  ││
│                            │ │ Códigos de verificación     │ │ Requerido│  ││
│                            │ └─────────────────────────────┘ └──────────┘  ││
│                            │                                               ││
│                            │ ┌─────────────────────────────┐ ┌──────────┐  ││
│                            │ │ Recordatorios               │ │ ✅ ON    │  ││
│                            │ │ Pagos y vencimientos        │ │          │  ││
│                            │ └─────────────────────────────┘ └──────────┘  ││
│                            └──────────────────────────────────────────────┘│
│                                                                              │
│                            ┌──────────────────────────────────────────────┐│
│                            │ HISTORIAL DE CAMBIOS                          ││
│                            │                                               ││
│                            │ 📅 Ene 29, 2026 - Desactivaste "Ofertas"      ││
│                            │ 📅 Ene 15, 2026 - Activaste "Boletín"         ││
│                            │ 📅 Dic 20, 2025 - Registro inicial             ││
│                            │                                               ││
│                            │ [Ver historial completo →]                    ││
│                            └──────────────────────────────────────────────┘│
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│ FOOTER                                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - PÁGINA UNSUBSCRIBE

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                              🚫 CANCELAR SUSCRIPCIÓN                        │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                                                                         │ │
│  │  Hola Juan,                                                             │ │
│  │                                                                         │ │
│  │  ¿Estás seguro que deseas dejar de recibir nuestros emails?            │ │
│  │                                                                         │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │ │
│  │  │ ○ Cancelar SOLO emails de marketing                             │    │ │
│  │  │   (Seguirás recibiendo actualizaciones de cuenta)              │    │ │
│  │  │                                                                 │    │ │
│  │  │ ○ Cancelar TODOS los emails                                     │    │ │
│  │  │   (Solo recibirás emails transaccionales obligatorios)         │    │ │
│  │  └─────────────────────────────────────────────────────────────────┘    │ │
│  │                                                                         │ │
│  │  ¿Por qué te vas? (opcional)                                            │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │ │
│  │  │ Recibo demasiados emails                                     ▼  │    │ │
│  │  └─────────────────────────────────────────────────────────────────┘    │ │
│  │                                                                         │ │
│  │               [Cancelar]    [Confirmar Cancelación]                     │ │
│  │                                                                         │ │
│  │  ───────────────────────────────────────────────────────────────────    │ │
│  │  💡 ¿Prefieres recibir menos emails? Ajusta la frecuencia en tus       │ │
│  │     [Preferencias de comunicación →]                                    │ │
│  │                                                                         │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

- Página `/unsubscribe?token=xxx`
- Formulario de confirmación
- Procesamiento de token

### 📋 Plan de Implementación

Ver documento principal: [26-privacy-gdpr.md](../02-AUTH/05-privacy-gdpr.md) sección "Plan de Implementación Recomendado"

**Tiempo estimado total:** 10-13 días  
**Prioridad:** ALTA (Legal compliance obligatorio)

---

## 📋 TABLA DE CONTENIDOS

1. [Marco Legal](#marco-legal)
2. [Procesos de Implementación](#procesos-de-implementación)
3. [Backend API](#backend-api)
4. [Componentes](#componentes)
5. [Páginas](#páginas)
6. [Hooks y Servicios](#hooks-y-servicios)
7. [Tipos TypeScript](#tipos-typescript)
8. [Auditoría y Cumplimiento](#auditoría-y-cumplimiento)

---

## ⚖️ MARCO LEGAL

### Ley 172-13 - Protección de Datos Personales (RD)

| Requisito Legal           | Descripción                                       | Estado Backend | Estado Frontend |
| ------------------------- | ------------------------------------------------- | -------------- | --------------- |
| **Consentimiento previo** | Usuario debe autorizar antes de recibir marketing | ✅ Impl.       | 🔴 Pendiente    |
| **Claro e inequívoco**    | No pre-marcado, acción afirmativa del usuario     | ✅ Impl.       | 🔴 Pendiente    |
| **Específico por tipo**   | Separar OKLA vs Partners vs Surveys               | ✅ Impl.       | 🔴 Pendiente    |
| **Revocable**             | Poder darse de baja en cualquier momento          | ✅ Impl.       | 🔴 Pendiente    |
| **Documentado**           | Registrar fecha, hora, IP del consentimiento      | ✅ Impl.       | 🔴 Pendiente    |
| **Derecho de acceso**     | Usuario puede ver historial de consentimientos    | ✅ Impl.       | 🔴 Pendiente    |

### Estado de Implementación por Proceso

| Proceso           | Backend API                         | Frontend Ruta                          | Frontend Componente             | Estado |
| ----------------- | ----------------------------------- | -------------------------------------- | ------------------------------- | ------ |
| CONSENT-REG-001   | ✅ POST /api/auth/register          | ❌ /register                           | ❌ RegisterForm checkboxes      | 🔴 0%  |
| CONSENT-PREF-001  | ✅ GET/PUT /api/consent/preferences | ❌ /settings/notifications/preferences | ❌ CommunicationPreferencesPage | 🔴 0%  |
| CONSENT-AUDIT-001 | ✅ GET /api/consent/history         | ❌ /settings/privacy/consent-history   | ❌ ConsentHistoryPage           | 🔴 0%  |
| CONSENT-UNSUB-001 | ✅ POST /api/unsubscribe            | ❌ /unsubscribe                        | ❌ UnsubscribePage              | 🔴 0%  |

### Tipos de Comunicación

| Tipo              | Descripción                         | Consentimiento | Desactivable |
| ----------------- | ----------------------------------- | -------------- | ------------ |
| **Transaccional** | Confirmaciones, facturas, seguridad | ❌ No          | ❌ No        |
| **Servicio**      | Alertas de cuenta, cambios de TOS   | ❌ No          | ❌ No        |
| **Marketing**     | Promociones, ofertas, newsletter    | ✅ Sí          | ✅ Sí        |
| **Partners**      | Ofertas de terceros                 | ✅ Sí          | ✅ Sí        |
| **Investigación** | Encuestas, feedback                 | ✅ Sí          | ✅ Sí        |

---

## 🏗️ PROCESOS DE IMPLEMENTACIÓN

### CONSENT-REG: Consentimiento en Registro

#### CONSENT-REG-001: Checkboxes de Registro

**Estado:** 🔴 NO IMPLEMENTADO  
**Prioridad:** P0 (Crítica)  
**Tiempo Estimado:** 1 día

**Lo que FALTA implementar:**

```typescript
// RegisterPage.tsx - Código necesario que NO existe actualmente

// 1. Agregar checkboxes obligatorios
<div className="space-y-3 border-t pt-4 mt-4">
  <p className="text-sm font-medium">Términos obligatorios:</p>

  <FormField>
    <label className="flex items-start gap-2 text-sm">
      <input
        {...register("acceptTerms", {
          required: "Debes aceptar los términos de servicio"
        })}
        type="checkbox"
        className="mt-1 rounded border-gray-300"
      />
      <span className="text-gray-700">
        Acepto los <a href="/terms" target="_blank" className="text-blue-600 underline">
          Términos de Servicio
        </a> de OKLA *
      </span>
    </label>
    {errors.acceptTerms && (
      <p className="text-red-500 text-xs mt-1">{errors.acceptTerms.message}</p>
    )}
  </FormField>

  <FormField>
    <label className="flex items-start gap-2 text-sm">
      <input
        {...register("acceptPrivacy", {
          required: "Debes aceptar la política de privacidad"
        })}
        type="checkbox"
        className="mt-1 rounded border-gray-300"
      />
      <span className="text-gray-700">
        Acepto la <a href="/privacy" target="_blank" className="text-blue-600 underline">
          Política de Privacidad
        </a> y el tratamiento de mis datos según Ley 172-13 *
      </span>
    </label>
    {errors.acceptPrivacy && (
      <p className="text-red-500 text-xs mt-1">{errors.acceptPrivacy.message}</p>
    )}
  </FormField>
</div>

// 2. Agregar checkboxes opcionales de marketing
<div className="space-y-3 border-t pt-4 mt-4">
  <p className="text-sm font-medium">Preferencias de comunicación (opcional):</p>

  <FormField>
    <label className="flex items-start gap-2 text-sm">
      <input
        {...register("marketingConsent")}
        type="checkbox"
        className="mt-1 rounded border-gray-300"
      />
      <span className="text-gray-600">
        Deseo recibir ofertas, promociones y novedades de OKLA por email
      </span>
    </label>
  </FormField>

  <FormField>
    <label className="flex items-start gap-2 text-sm">
      <input
        {...register("partnersConsent")}
        type="checkbox"
        className="mt-1 rounded border-gray-300"
      />
      <span className="text-gray-600">
        Acepto recibir ofertas de partners seleccionados de OKLA
      </span>
    </label>
  </FormField>

  <FormField>
    <label className="flex items-start gap-2 text-sm">
      <input
        {...register("newsletterConsent")}
        type="checkbox"
        className="mt-1 rounded border-gray-300"
      />
      <span className="text-gray-600">
        Quiero recibir el newsletter semanal con consejos y novedades
      </span>
    </label>
  </FormField>
</div>
```

**Requisitos Legales:**

- ✅ Checkboxes NO pre-marcados (por defecto: false)
- ✅ Términos obligatorios separados de marketing opcional
- ✅ Partners separado de OKLA
- ✅ Texto claro y visible
- ✅ Links a documentos legales abriendo en nueva pestaña

#### CONSENT-REG-002: Registro del Consentimiento Backend

**Estado:** ✅ Backend listo (DataProtectionService)  
**Frontend:** 🔴 NO implementado - Falta enviar datos

**Payload que FALTA enviar al Backend:**

```typescript
  "name": "Juan Pérez",
  "email": "juan@example.com",
  "password": "hashed",
  "consents": {
    "terms": true,              // Obligatorio
    "privacy": true,            // Obligatorio
    "marketingEmail": false,    // Opcional (NO marcado por defecto)
    "partnersEmail": false,     // Opcional (NO marcado por defecto)
    // Metadata para auditoría (Ley 172-13)
    "timestamp": "2026-01-29T10:30:00Z",
    "ipAddress": "192.168.1.100",
    "userAgent": "Mozilla/5.0...",
    "source": "registration_form",
    "version": "1.0"  // Versión del texto de consentimiento
  }
}
```

---

### CONSENT-PREF: Preferencias de Comunicación

#### CONSENT-PREF-001: Centro de Preferencias

**Estado:** 🔴 Pendiente  
**Ruta:** `/settings/notifications/preferences`

---

## 🔌 BACKEND API

### ConsentController (NotificationService)

```typescript
// Base URL: /api/consent

// Obtener preferencias actuales del usuario
GET    /api/consent/preferences
Response: {
  "email": {
    "transactional": true,     // No desactivable
    "security": true,           // No desactivable
    "marketing_okla": false,
    "partners": false,
    "vehicle_alerts": true,
    "surveys": false
  },
  "sms": {
    "security": true,           // 2FA - No desactivable
    "price_alerts": false,
    "marketing": false
  },
  "push": {
    "messages": true,
    "updates": true,
    "recommendations": false
  },
  "whatsapp": {
    "marketing": false,
    "alerts": false
  },
  "lastUpdated": "2026-01-25T14:35:00Z"
}

// Actualizar preferencias
PUT    /api/consent/preferences
Body: {
  "email": {
    "marketing_okla": true,
    "partners": false,
    "vehicle_alerts": true
  },
  "sms": {
    "price_alerts": true
  }
}
Response: {
  "success": true,
  "message": "Preferencias actualizadas",
  "updatedAt": "2026-01-29T11:00:00Z"
}

// Obtener historial de cambios (CONSENT-AUDIT-002)
GET    /api/consent/history?page=1&pageSize=20
Response: {
  "items": [
    {
      "id": "consent_123",
      "type": "marketing_okla",
      "channel": "email",
      "granted": false,
      "timestamp": "2026-01-25T14:35:00Z",
      "source": "preferences_page",
      "ipAddress": "192.168.xxx.xxx"
    },
    {
      "id": "consent_124",
      "type": "vehicle_alerts",
      "channel": "sms",
      "granted": true,
      "timestamp": "2026-01-20T10:15:00Z",
      "source": "preferences_page",
      "ipAddress": "192.168.xxx.xxx"
    }
  ],
  "totalCount": 12
}

// Otorgar consentimiento específico
POST   /api/consent/grant
Body: {
  "type": "marketing_okla",
  "channel": "email"
}

// Revocar consentimiento específico
POST   /api/consent/revoke
Body: {
  "type": "marketing_okla",
  "channel": "email",
  "reason": "user_request"  // Opcional
}
```

### UnsubscribeController (Público - No Auth)

```typescript
// Base URL: /api/unsubscribe

// Validar token de unsubscribe (desde link en email)
GET    /api/unsubscribe/validate?token=abc123&type=marketing
Response: {
  "valid": true,
  "email": "juan@example.com",
  "type": "marketing_okla",
  "expiresAt": "2026-02-05T23:59:59Z"
}

// Confirmar baja
POST   /api/unsubscribe/confirm
Body: {
  "token": "abc123",
  "type": "marketing",
  "option": "unsubscribe_all" | "unsubscribe_type" | "reduce_frequency"
}
Response: {
  "success": true,
  "message": "Te has dado de baja exitosamente"
}

// Opciones de baja (para página de confirmación)
GET    /api/unsubscribe/options?token=abc123
Response: {
  "currentSubscriptions": [
    { "type": "marketing_okla", "label": "Promociones de OKLA" },
    { "type": "newsletter", "label": "Newsletter semanal" }
  ],
  "options": [
    {
      "value": "unsubscribe_type",
      "label": "Solo cancelar 'Promociones de OKLA'"
    },
    {
      "value": "unsubscribe_all",
      "label": "Cancelar TODAS las comunicaciones de marketing"
    },
    {
      "value": "reduce_frequency",
      "label": "Reducir frecuencia (mensual en vez de semanal)"
    }
  ]
}
```

---

## 🎨 COMPONENTES

### PASO 1: ConsentPreferencesForm

**Componente principal para gestionar preferencias de consentimiento.**

```typescript
// filepath: src/components/consent/ConsentPreferencesForm.tsx
"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Switch } from "@/components/ui/Switch";
import { showToast } from "@/lib/toast";
import { useConsentPreferences } from "@/lib/hooks/useConsent";
import { AlertCircle, Mail, MessageSquare, Bell, Phone } from "lucide-react";

const preferencesSchema = z.object({
  email: z.object({
    marketing_okla: z.boolean(),
    partners: z.boolean(),
    vehicle_alerts: z.boolean(),
    surveys: z.boolean(),
  }),
  sms: z.object({
    price_alerts: z.boolean(),
    marketing: z.boolean(),
  }),
  push: z.object({
    messages: z.boolean(),
    updates: z.boolean(),
    recommendations: z.boolean(),
  }),
  whatsapp: z.object({
    marketing: z.boolean(),
    alerts: z.boolean(),
  }),
});

type PreferencesFormData = z.infer<typeof preferencesSchema>;

export function ConsentPreferencesForm() {
  const { data: currentPreferences, isLoading } = useConsentPreferences();
  const [isSaving, setIsSaving] = useState(false);

  const { register, handleSubmit, watch } = useForm<PreferencesFormData>({
    resolver: zodResolver(preferencesSchema),
    defaultValues: currentPreferences,
  });

  const onSubmit = async (data: PreferencesFormData) => {
    setIsSaving(true);
    try {
      await fetch("/api/consent/preferences", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });

      showToast.success("Preferencias actualizadas");
    } catch {
      showToast.error("Error al guardar preferencias");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) return <div>Cargando preferencias...</div>;

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Comunicaciones Obligatorias */}
      <Card className="p-6">
        <div className="flex items-start gap-3 mb-4">
          <AlertCircle className="text-blue-600 mt-1" size={20} />
          <div>
            <h3 className="font-semibold text-gray-900">
              Comunicaciones Obligatorias
            </h3>
            <p className="text-sm text-gray-600 mt-1">
              Estas notificaciones son necesarias para el funcionamiento de tu cuenta
              y no pueden ser desactivadas.
            </p>
          </div>
        </div>
        <ul className="space-y-2 text-sm text-gray-700">
          <li>✓ Confirmaciones de cuenta y seguridad</li>
          <li>✓ Transacciones y pagos</li>
          <li>✓ Cambios en términos de servicio</li>
        </ul>
      </Card>

      {/* Email */}
      <Card className="p-6">
        <div className="flex items-center gap-3 mb-4">
          <Mail className="text-gray-700" size={24} />
          <h3 className="font-semibold text-gray-900">Email</h3>
        </div>
        <div className="space-y-4">
          <SwitchField
            label="Newsletter semanal - Novedades del mercado automotriz"
            description="Recibe las últimas tendencias y ofertas destacadas"
            {...register("email.marketing_okla")}
          />
          <SwitchField
            label="Ofertas y promociones de OKLA"
            description="Promociones exclusivas en vehículos seleccionados"
            {...register("email.marketing_okla")}
          />
          <SwitchField
            label="Ofertas de partners seleccionados"
            description="Financiamiento, seguros y servicios relacionados"
            {...register("email.partners")}
          />
          <SwitchField
            label="Alertas de nuevos vehículos (según tus búsquedas)"
            description="Te avisamos cuando hay nuevos vehículos que te interesan"
            {...register("email.vehicle_alerts")}
          />
          <SwitchField
            label="Encuestas y feedback"
            description="Ayúdanos a mejorar con tu opinión"
            {...register("email.surveys")}
          />
        </div>
      </Card>

      {/* SMS */}
      <Card className="p-6">
        <div className="flex items-center gap-3 mb-4">
          <MessageSquare className="text-gray-700" size={24} />
          <h3 className="font-semibold text-gray-900">SMS</h3>
        </div>
        <div className="space-y-4">
          <div className="text-sm text-gray-600 mb-3">
            ✓ Códigos de verificación (obligatorio - no desactivable)
          </div>
          <SwitchField
            label="Alertas de precio en favoritos"
            description="Te avisamos cuando baja el precio de tus vehículos favoritos"
            {...register("sms.price_alerts")}
          />
          <SwitchField
            label="Ofertas y promociones"
            description="Promociones urgentes por SMS"
            {...register("sms.marketing")}
          />
        </div>
      </Card>

      {/* Push Notifications */}
      <Card className="p-6">
        <div className="flex items-center gap-3 mb-4">
          <Bell className="text-gray-700" size={24} />
          <h3 className="font-semibold text-gray-900">Push Notifications</h3>
        </div>
        <div className="space-y-4">
          <SwitchField
            label="Mensajes nuevos"
            description="Cuando recibes mensajes de otros usuarios"
            {...register("push.messages")}
          />
          <SwitchField
            label="Actualizaciones en mis anuncios"
            description="Nuevas consultas, favoritos y vistas en tus vehículos"
            {...register("push.updates")}
          />
          <SwitchField
            label="Recomendaciones personalizadas"
            description="Vehículos que podrían interesarte"
            {...register("push.recommendations")}
          />
        </div>
      </Card>

      {/* WhatsApp (Opcional) */}
      <Card className="p-6">
        <div className="flex items-center gap-3 mb-4">
          <Phone className="text-gray-700" size={24} />
          <h3 className="font-semibold text-gray-900">WhatsApp</h3>
        </div>
        <div className="space-y-4">
          <SwitchField
            label="Marketing y promociones"
            description="Recibe ofertas exclusivas por WhatsApp"
            {...register("whatsapp.marketing")}
          />
          <SwitchField
            label="Alertas de vehículos"
            description="Notificaciones de nuevos vehículos"
            {...register("whatsapp.alerts")}
          />
        </div>
      </Card>

      {/* Footer */}
      <div className="bg-gray-50 rounded-lg p-4">
        <p className="text-sm text-gray-600 mb-2">
          ℹ️ <strong>Tu privacidad es importante.</strong> Estas preferencias
          cumplen con la Ley 172-13 de Protección de Datos Personales de RD.
        </p>
        <p className="text-xs text-gray-500">
          Último cambio: {currentPreferences?.lastUpdated
            ? new Date(currentPreferences.lastUpdated).toLocaleString("es-DO")
            : "N/A"}
        </p>
        <Button
          variant="link"
          size="sm"
          className="mt-2 px-0"
          onClick={() => window.location.href = "/settings/privacy/consent-history"}
        >
          Ver historial de cambios →
        </Button>
      </div>

      {/* Submit Button */}
      <div className="flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={() => window.history.back()}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSaving}>
          {isSaving ? "Guardando..." : "Guardar Preferencias"}
        </Button>
      </div>
    </form>
  );
}

// Helper component
function SwitchField({ label, description, ...props }) {
  return (
    <div className="flex items-start justify-between gap-4">
      <div className="flex-1">
        <label className="text-sm font-medium text-gray-900">{label}</label>
        {description && (
          <p className="text-xs text-gray-500 mt-1">{description}</p>
        )}
      </div>
      <Switch {...props} />
    </div>
  );
}
```

---

### PASO 2: UnsubscribeConfirmation

**Página de confirmación cuando usuario hace clic en link de baja desde email.**

```typescript
// filepath: src/components/consent/UnsubscribeConfirmation.tsx
"use client";

import { useState, useEffect } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { showToast } from "@/lib/toast";
import { CheckCircle, XCircle } from "lucide-react";

export function UnsubscribeConfirmation() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token");
  const type = searchParams.get("type");

  const [isValidating, setIsValidating] = useState(true);
  const [isValid, setIsValid] = useState(false);
  const [email, setEmail] = useState("");
  const [options, setOptions] = useState([]);
  const [selectedOption, setSelectedOption] = useState("unsubscribe_type");
  const [isUnsubscribing, setIsUnsubscribing] = useState(false);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    validateToken();
  }, [token]);

  const validateToken = async () => {
    try {
      const res = await fetch(
        `/api/unsubscribe/validate?token=${token}&type=${type}`
      );
      const data = await res.json();

      if (data.valid) {
        setIsValid(true);
        setEmail(data.email);

        // Obtener opciones
        const optRes = await fetch(`/api/unsubscribe/options?token=${token}`);
        const optData = await optRes.json();
        setOptions(optData.options);
      }
    } catch {
      setIsValid(false);
    } finally {
      setIsValidating(false);
    }
  };

  const handleUnsubscribe = async () => {
    setIsUnsubscribing(true);
    try {
      await fetch("/api/unsubscribe/confirm", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          token,
          type,
          option: selectedOption,
        }),
      });

      setSuccess(true);
      showToast.success("Te has dado de baja exitosamente");

      // Redirect después de 3 segundos
      setTimeout(() => router.push("/"), 3000);
    } catch {
      showToast.error("Error al procesar solicitud");
    } finally {
      setIsUnsubscribing(false);
    }
  };

  if (isValidating) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p>Validando...</p>
      </div>
    );
  }

  if (!isValid) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <Card className="max-w-md p-6 text-center">
          <XCircle className="text-red-500 mx-auto mb-4" size={48} />
          <h1 className="text-xl font-bold text-gray-900 mb-2">
            Link Inválido o Expirado
          </h1>
          <p className="text-gray-600 mb-4">
            El link de baja no es válido o ha expirado.
          </p>
          <Button onClick={() => router.push("/settings/notifications")}>
            Ir a Preferencias
          </Button>
        </Card>
      </div>
    );
  }

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <Card className="max-w-md p-6 text-center">
          <CheckCircle className="text-green-500 mx-auto mb-4" size={48} />
          <h1 className="text-xl font-bold text-gray-900 mb-2">
            ¡Listo!
          </h1>
          <p className="text-gray-600 mb-2">
            Te has dado de baja exitosamente.
          </p>
          <p className="text-sm text-gray-500">
            Redirigiendo...
          </p>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <Card className="max-w-2xl p-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          📧 Cancelar Suscripción
        </h1>
        <p className="text-gray-600 mb-6">
          Email: <strong>{email}</strong>
        </p>

        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6">
          <h3 className="font-semibold text-gray-900 mb-2">
            ¿Seguro que deseas darte de baja?
          </h3>
          <p className="text-sm text-gray-700 mb-2">
            Dejarás de recibir: <strong>Ofertas y promociones de OKLA</strong>
          </p>
          <p className="text-sm text-gray-600">
            Seguirás recibiendo:
          </p>
          <ul className="text-sm text-gray-600 list-disc list-inside mt-1">
            <li>Confirmaciones de transacciones</li>
            <li>Alertas de seguridad</li>
            <li>Notificaciones de tu cuenta</li>
          </ul>
        </div>

        <div className="space-y-3 mb-6">
          <p className="font-medium text-gray-900">Elige una opción:</p>
          {options.map((opt) => (
            <label
              key={opt.value}
              className="flex items-start gap-3 p-3 border rounded-lg cursor-pointer hover:bg-gray-50"
            >
              <input
                type="radio"
                name="unsubscribe-option"
                value={opt.value}
                checked={selectedOption === opt.value}
                onChange={(e) => setSelectedOption(e.target.value)}
                className="mt-1"
              />
              <span className="text-sm text-gray-700">{opt.label}</span>
            </label>
          ))}
        </div>

        <div className="flex gap-3 justify-end">
          <Button
            variant="outline"
            onClick={() => router.push("/")}
          >
            Mantener Suscripción
          </Button>
          <Button
            variant="destructive"
            onClick={handleUnsubscribe}
            disabled={isUnsubscribing}
          >
            {isUnsubscribing ? "Procesando..." : "Confirmar Cancelación"}
          </Button>
        </div>

        <p className="text-sm text-gray-500 text-center mt-6">
          ¿Prefieres ajustar tus preferencias?{" "}
          <a
            href="/settings/notifications/preferences"
            className="text-primary-600 hover:underline"
          >
            Ir a Configuración
          </a>
        </p>
      </Card>
    </div>
  );
}
```

---

### PASO 3: ConsentHistoryTimeline

**Historial completo de consentimientos del usuario (CONSENT-AUDIT-002).**

```typescript
// filepath: src/components/consent/ConsentHistoryTimeline.tsx
"use client";

import { useState } from "react";
import { useConsentHistory } from "@/lib/hooks/useConsent";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { CheckCircle, XCircle, Download } from "lucide-react";
import { format } from "date-fns";
import { es } from "date-fns/locale";

export function ConsentHistoryTimeline() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useConsentHistory(page);

  const handleExport = async () => {
    // Descargar historial completo como JSON
    const res = await fetch("/api/consent/history/export");
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `consent-history-${Date.now()}.json`;
    a.click();
  };

  if (isLoading) return <div>Cargando historial...</div>;

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <p className="text-gray-600">
          Este es el registro completo de tus consentimientos de comunicación
          según la Ley 172-13 de Protección de Datos Personales.
        </p>
        <Button variant="outline" size="sm" onClick={handleExport}>
          <Download size={16} className="mr-2" />
          Descargar Historial
        </Button>
      </div>

      <div className="space-y-4">
        {data?.items.map((record) => (
          <Card key={record.id} className="p-5">
            <div className="flex items-start justify-between gap-4">
              <div className="flex items-start gap-4 flex-1">
                {record.granted ? (
                  <CheckCircle className="text-green-500 mt-1" size={20} />
                ) : (
                  <XCircle className="text-red-500 mt-1" size={20} />
                )}

                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-semibold text-gray-900">
                      {getConsentLabel(record.type)}
                    </span>
                    <Badge variant={record.granted ? "success" : "default"}>
                      {record.granted ? "Activado" : "Desactivado"}
                    </Badge>
                    <Badge variant="outline">{record.channel}</Badge>
                  </div>

                  <p className="text-sm text-gray-600 mb-2">
                    Fuente: {getSourceLabel(record.source)}
                  </p>

                  <div className="flex items-center gap-4 text-xs text-gray-500">
                    <span>
                      {format(
                        new Date(record.timestamp),
                        "dd 'de' MMMM 'de' yyyy 'a las' HH:mm",
                        { locale: es }
                      )}
                    </span>
                    <span>IP: {maskIP(record.ipAddress)}</span>
                  </div>
                </div>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Pagination */}
      {data && data.totalCount > 20 && (
        <div className="flex justify-center gap-2 mt-6">
          <Button
            variant="outline"
            disabled={page === 1}
            onClick={() => setPage(page - 1)}
          >
            Anterior
          </Button>
          <span className="px-4 py-2 text-sm text-gray-600">
            Página {page} de {Math.ceil(data.totalCount / 20)}
          </span>
          <Button
            variant="outline"
            disabled={page * 20 >= data.totalCount}
            onClick={() => setPage(page + 1)}
          >
            Siguiente
          </Button>
        </div>
      )}
    </div>
  );
}

// Helper functions
function getConsentLabel(type: string): string {
  const labels = {
    marketing_okla: "Marketing OKLA",
    partners: "Ofertas de Partners",
    vehicle_alerts: "Alertas de Vehículos",
    price_alerts: "Alertas de Precio",
    newsletter: "Newsletter Semanal",
    surveys: "Encuestas y Feedback",
  };
  return labels[type] || type;
}

function getSourceLabel(source: string): string {
  const labels = {
    registration_form: "Registro de cuenta",
    preferences_page: "Página de preferencias",
    unsubscribe: "Link de baja en email",
    admin: "Acción administrativa",
  };
  return labels[source] || source;
}

function maskIP(ip: string): string {
  // Enmascarar últimos 2 octetos por privacidad
  const parts = ip.split(".");
  return `${parts[0]}.${parts[1]}.xxx.xxx`;
}
```

---

## 📄 PÁGINAS

### Página: Preferencias de Consentimiento

```typescript
// filepath: src/app/settings/notifications/preferences/page.tsx
import { Metadata } from "next";
import { ConsentPreferencesForm } from "@/components/consent/ConsentPreferencesForm";

export const metadata: Metadata = {
  title: "Preferencias de Comunicación | OKLA",
  description: "Gestiona tus preferencias de comunicación",
};

export default function ConsentPreferencesPage() {
  return (
    <div className="max-w-3xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          📧 Preferencias de Comunicación
        </h1>
        <p className="text-gray-600">
          Gestiona cómo y cuándo quieres recibir comunicaciones de OKLA.
          Según Ley 172-13 de Protección de Datos Personales de RD.
        </p>
      </div>

      <ConsentPreferencesForm />
    </div>
  );
}
```

### Página: Historial de Consentimientos

```typescript
// filepath: src/app/settings/privacy/consent-history/page.tsx
import { Metadata } from "next";
import { ConsentHistoryTimeline } from "@/components/consent/ConsentHistoryTimeline";

export const metadata: Metadata = {
  title: "Historial de Consentimientos | OKLA",
  description: "Registro de tus consentimientos de comunicación",
};

export default function ConsentHistoryPage() {
  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">
          📋 Historial de Consentimientos
        </h1>
        <p className="text-gray-600">
          Registro completo de tus consentimientos según Ley 172-13.
        </p>
      </div>

      <ConsentHistoryTimeline />
    </div>
  );
}
```

### Página: Darse de Baja (Unsubscribe)

```typescript
// filepath: src/app/unsubscribe/page.tsx
import { Metadata } from "next";
import { UnsubscribeConfirmation } from "@/components/consent/UnsubscribeConfirmation";

export const metadata: Metadata = {
  title: "Cancelar Suscripción | OKLA",
  description: "Gestiona tus suscripciones de email",
};

export default function UnsubscribePage() {
  return <UnsubscribeConfirmation />;
}
```

---

## 🪝 HOOKS Y SERVICIOS

### useConsent Hook

```typescript
// filepath: src/lib/hooks/useConsent.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { consentService } from "@/lib/services/consentService";

export function useConsentPreferences() {
  return useQuery({
    queryKey: ["consent", "preferences"],
    queryFn: () => consentService.getPreferences(),
  });
}

export function useUpdateConsentPreferences() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) => consentService.updatePreferences(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["consent", "preferences"] });
    },
  });
}

export function useConsentHistory(page: number = 1) {
  return useQuery({
    queryKey: ["consent", "history", page],
    queryFn: () => consentService.getHistory(page),
  });
}

export function useGrantConsent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { type: string; channel: string }) =>
      consentService.grant(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["consent"] });
    },
  });
}

export function useRevokeConsent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: { type: string; channel: string; reason?: string }) =>
      consentService.revoke(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["consent"] });
    },
  });
}
```

### Consent Service

```typescript
// filepath: src/lib/services/consentService.ts
import { apiClient } from "@/lib/api";

export const consentService = {
  async getPreferences() {
    const { data } = await apiClient.get("/api/consent/preferences");
    return data;
  },

  async updatePreferences(preferences: any) {
    const { data } = await apiClient.put(
      "/api/consent/preferences",
      preferences,
    );
    return data;
  },

  async getHistory(page: number = 1, pageSize: number = 20) {
    const { data } = await apiClient.get("/api/consent/history", {
      params: { page, pageSize },
    });
    return data;
  },

  async grant(payload: { type: string; channel: string }) {
    const { data } = await apiClient.post("/api/consent/grant", payload);
    return data;
  },

  async revoke(payload: { type: string; channel: string; reason?: string }) {
    const { data } = await apiClient.post("/api/consent/revoke", payload);
    return data;
  },

  // Unsubscribe (público)
  async validateUnsubscribeToken(token: string, type: string) {
    const { data } = await apiClient.get("/api/unsubscribe/validate", {
      params: { token, type },
    });
    return data;
  },

  async confirmUnsubscribe(payload: {
    token: string;
    type: string;
    option: string;
  }) {
    const { data } = await apiClient.post("/api/unsubscribe/confirm", payload);
    return data;
  },

  async getUnsubscribeOptions(token: string) {
    const { data } = await apiClient.get("/api/unsubscribe/options", {
      params: { token },
    });
    return data;
  },

  // Export
  async exportHistory() {
    return apiClient.get("/api/consent/history/export", {
      responseType: "blob",
    });
  },
};
```

---

## 📐 TIPOS TYPESCRIPT

```typescript
// filepath: src/types/consent.ts

export enum ConsentType {
  TERMS_OF_SERVICE = "terms",
  PRIVACY_POLICY = "privacy",
  MARKETING_OKLA = "marketing_okla",
  MARKETING_PARTNERS = "partners",
  VEHICLE_ALERTS = "vehicle_alerts",
  PRICE_DROP_ALERTS = "price_alerts",
  NEWSLETTER = "newsletter",
  SURVEYS = "surveys",
}

export enum ConsentChannel {
  EMAIL = "email",
  SMS = "sms",
  PUSH = "push",
  WHATSAPP = "whatsapp",
}

export enum ConsentSource {
  REGISTRATION = "registration_form",
  PREFERENCES = "preferences_page",
  UNSUBSCRIBE = "unsubscribe",
  ADMIN = "admin",
}

export interface ConsentRecord {
  id: string;
  userId: string;
  type: ConsentType;
  channel: ConsentChannel;
  granted: boolean;
  timestamp: string;
  ipAddress: string;
  userAgent: string;
  source: ConsentSource;
  version: string; // Versión del texto de consentimiento
  revokedAt?: string;
  revokedReason?: string;
}

export interface ConsentPreferences {
  email: {
    transactional: true; // No desactivable
    security: true; // No desactivable
    marketing_okla: boolean;
    partners: boolean;
    vehicle_alerts: boolean;
    surveys: boolean;
  };
  sms: {
    security: true; // 2FA - No desactivable
    price_alerts: boolean;
    marketing: boolean;
  };
  push: {
    messages: boolean;
    updates: boolean;
    recommendations: boolean;
  };
  whatsapp: {
    marketing: boolean;
    alerts: boolean;
  };
  lastUpdated: string;
}

export interface UnsubscribeToken {
  token: string;
  email: string;
  type: string;
  expiresAt: string;
}

export interface UnsubscribeOption {
  value: "unsubscribe_type" | "unsubscribe_all" | "reduce_frequency";
  label: string;
}
```

---

## 🔒 AUDITORÍA Y CUMPLIMIENTO

### Registro de Auditoría (Backend)

```csharp
// ConsentRecord Entity
public class ConsentRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Tipo y Canal
    public ConsentType Type { get; set; }
    public string Channel { get; set; } // email, sms, push, whatsapp

    // Estado
    public bool Granted { get; set; }
    public DateTime Timestamp { get; set; }

    // Contexto para auditoría (Ley 172-13)
    public string Source { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }

    // Versión del texto legal
    public string ConsentTextVersion { get; set; }
    public string ConsentTextHash { get; set; }

    // Revocación
    public DateTime? RevokedAt { get; set; }
    public string RevokedReason { get; set; }
}
```

### Lista de Supresión

```csharp
// SuppressionList Entity
public class SuppressionList
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public SuppressionType Type { get; set; } // marketing, all
    public DateTime AddedAt { get; set; }
    public string Reason { get; set; }
}

public enum SuppressionType
{
    Marketing,      // Solo marketing
    All            // Todas las comunicaciones opcionales
}
```

### Verificación Antes de Enviar

```csharp
// NotificationService - Verificar consentimiento antes de enviar
public async Task<bool> SendMarketingEmail(Guid userId, string templateId)
{
    // 1. Verificar consentimiento
    var hasConsent = await _consentService.HasConsent(
        userId,
        ConsentType.MarketingOkla,
        "email"
    );

    if (!hasConsent)
    {
        _logger.LogInformation(
            "User {UserId} has not consented to marketing emails",
            userId
        );
        return false;
    }

    // 2. Verificar lista de supresión
    var isSuppressed = await _suppressionService.IsEmailSuppressed(email);
    if (isSuppressed)
    {
        _logger.LogWarning(
            "Email {Email} is in suppression list",
            email
        );
        return false;
    }

    // 3. Enviar email
    await _emailService.Send(userId, templateId);
    return true;
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Fase 1: Base (Q1 2026)

- [x] Checkboxes en registro (CONSENT-REG-001)
- [ ] Registro de consentimientos con metadata (CONSENT-REG-002)
- [ ] Página de preferencias básica (CONSENT-PREF-001)
- [ ] Link de unsubscribe en emails (CONSENT-UNSUB-001)

### Fase 2: Mejoras (Q2 2026)

- [ ] Granularidad por canal (CONSENT-PREF-002)
- [ ] Página de confirmación de baja mejorada (CONSENT-UNSUB-002)
- [ ] Historial de consentimientos para usuario (CONSENT-AUDIT-002)
- [ ] Exportación de historial

### Fase 3: Completo (Q2 2026)

- [ ] Dashboard admin de consentimientos
- [ ] Integración con lista de supresión
- [ ] Auditoría completa con reportes
- [ ] Tests E2E de flujos completos

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Consentimiento de Comunicaciones", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar preferencias de comunicación en settings", async ({
    page,
  }) => {
    await page.goto("/settings/notifications");
    await expect(page.getByTestId("communication-preferences")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /preferencias de comunicación/i }),
    ).toBeVisible();
  });

  test("debe mostrar canales de comunicación con toggles", async ({ page }) => {
    await page.goto("/settings/notifications");
    await expect(page.getByTestId("channel-email-toggle")).toBeVisible();
    await expect(page.getByTestId("channel-sms-toggle")).toBeVisible();
    await expect(page.getByTestId("channel-push-toggle")).toBeVisible();
    await expect(page.getByTestId("channel-whatsapp-toggle")).toBeVisible();
  });

  test("debe actualizar consentimiento de email marketing", async ({
    page,
  }) => {
    await page.goto("/settings/notifications");
    const toggle = page.getByTestId("consent-marketing-email");
    const initialState = await toggle.isChecked();
    await toggle.click();
    await expect(page.getByText(/preferencias actualizadas/i)).toBeVisible();
    await page.reload();
    expect(await toggle.isChecked()).toBe(!initialState);
  });

  test("debe mostrar historial de consentimientos", async ({ page }) => {
    await page.goto("/settings/notifications/history");
    await expect(page.getByTestId("consent-history-list")).toBeVisible();
    await expect(page.getByTestId("consent-entry").first()).toBeVisible();
    await expect(page.getByTestId("consent-date").first()).toBeVisible();
    await expect(page.getByTestId("consent-action").first()).toBeVisible();
  });

  test("debe funcionar unsubscribe desde link de email", async ({ page }) => {
    await page.goto("/unsubscribe?token=test-token&email=test@example.com");
    await expect(page.getByTestId("unsubscribe-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /cancelar suscripción/i }),
    ).toBeVisible();
    await page.getByRole("button", { name: /confirmar cancelación/i }).click();
    await expect(page.getByText(/suscripción cancelada/i)).toBeVisible();
  });

  test("debe mostrar checkboxes de consentimiento en registro", async ({
    page,
  }) => {
    await page.goto("/register");
    await expect(page.getByTestId("consent-terms")).toBeVisible();
    await expect(page.getByTestId("consent-privacy")).toBeVisible();
    await expect(page.getByTestId("consent-marketing")).toBeVisible();
  });
});
```

---

## 📚 REFERENCIAS

| Documento                         | Ubicación                                                     |
| --------------------------------- | ------------------------------------------------------------- |
| **Proceso completo**              | `docs/process-matrix/09-NOTIFICACIONES/05-consentimiento-...` |
| **Ley 172-13**                    | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/02-ley-172-13.md` |
| **Registro con consentimiento**   | `docs/frontend-rebuild/04-PAGINAS/07-auth.md`                 |
| **Centro de notificaciones**      | `docs/frontend-rebuild/04-PAGINAS/25-notificaciones.md`       |
| **Privacy & GDPR**                | `docs/frontend-rebuild/04-PAGINAS/26-privacy-gdpr.md`         |
| **NotificationService (Backend)** | `backend/NotificationService/`                                |

---

**Última actualización:** Enero 29, 2026  
**Estado:** 🟡 50% Backend | 🟡 40% UI  
**Responsable:** Equipo de Desarrollo OKLA  
**Prioridad:** 🟡 MEDIA (Funcionalidad básica existe, mejoras pendientes)
