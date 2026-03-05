---
title: "27. KYC Verification & Trust"
priority: P0
estimated_time: ""
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 27. KYC Verification & Trust

**Objetivo:** Sistema completo de verificación de identidad (KYC) para cumplimiento regulatorio (Ley 155-17 RD - Prevención Lavado de Activos), verificación de vendedores, badges de confianza, y programa OKLA Certified Pre-Owned.

**Prioridad:** P0 (CRÍTICO - Obligatorio para dealers y compliance)  
**Complejidad:** 🔴 Alta (Document OCR, Biometrics, Compliance, Legal)  
**Dependencias:** KYCService (✅ IMPLEMENTADO), TrustService (✅ IMPLEMENTADO), MediaService  
**Última Auditoría:** Enero 29, 2026

---

## 🔍 AUDITORÍA DE IMPLEMENTACIÓN (Enero 29, 2026)

### Estado de Verificación KYC (Ley 155-17)

| Proceso                             | Backend | Frontend UI | Cobertura | Estado      |
| ----------------------------------- | ------- | ----------- | --------- | ----------- |
| **AML-KYC-001** Verificación Básica | ✅ 100% | ✅ 95%      | **95%**   | ✅ COMPLETO |
| **AML-DDC-001** Due Diligence       | ✅ 100% | 🟡 40%      | **40%**   | 🟡 PARCIAL  |
| **Watchlist Check (PEPs)**          | ✅ 100% | ✅ 95%      | **95%**   | ✅ COMPLETO |
| **STR Reports (ROS)**               | ✅ 100% | ✅ 90%      | **90%**   | ✅ COMPLETO |

### ✅ Páginas KYC Implementadas

| Ruta                          | Componente         | Funcionalidad         | Estado  |
| ----------------------------- | ------------------ | --------------------- | ------- |
| `/verification`               | VerificationPage   | Upload documentos KYC | ✅ 100% |
| `/dashboard`                  | UserDashboardPage  | Banner KYC status     | ✅ 100% |
| `/admin/kyc/queue`            | KYCAdminQueuePage  | Cola de revisión      | ✅ 95%  |
| `/admin/kyc/review/{id}`      | KYCAdminReviewPage | Aprobar/Rechazar      | ✅ 95%  |
| `/admin/compliance/watchlist` | WatchlistAdminPage | Gestión PEPs          | ✅ 95%  |
| `/admin/compliance/str`       | STRReportsPage     | Reportes UAF          | ✅ 90%  |

### 🟡 Funcionalidades Parciales (DDC)

**LO QUE EXISTE:**

- ✅ Upload de cédula/pasaporte
- ✅ Validación de identidad básica
- ✅ Detección de PEP (flag `isPEP`)
- ✅ Cálculo de `riskScore` y `riskLevel`

**LO QUE FALTA (BRECHA):**

```
🔴 NO EXISTE: Dashboard de Due Diligence Reforzada
🔴 NO EXISTE: Wizard para DDC por niveles (Simplificada/Normal/Reforzada)
🔴 NO EXISTE: Formulario de origen de fondos
🔴 NO EXISTE: Upload de comprobantes de dirección
🔴 NO EXISTE: Declaración jurada de origen de fondos (> $500K)
🔴 NO EXISTE: Workflow de aprobación para DDC reforzada
```

### 📋 kycService.ts - Implementación Completa

```typescript
// ✅ IMPLEMENTADO: frontend/web/src/services/kycService.ts

export interface KYCProfile {
  id: string;
  userId: string;
  status: KYCStatus; // Pending, InProgress, UnderReview, Approved, Rejected
  riskLevel: RiskLevel; // Low, Medium, High, Critical
  riskScore: number; // 0-100
  isPEP: boolean;
  // ... 30+ campos implementados
}

export enum KYCStatus {
  Pending = 1,
  InProgress = 2,
  DocumentsRequired = 3,
  UnderReview = 4,
  Approved = 5,
  Rejected = 6,
  Expired = 7,
  Suspended = 8
}

// Funciones implementadas:
✅ kycService.getProfileByUserId(userId)
✅ kycService.submitForReview(profileId)
✅ kycService.approveProfile(profileId)
✅ kycService.rejectProfile(profileId, reason)
✅ kycService.getSTRs({ page, pageSize, status })
✅ kycService.createSTR(data)
✅ kycService.approveSTR(id)
✅ kycService.sendSTRtoUAF(id, uafNumber)
✅ kycService.searchWatchlist(query, type)
✅ kycService.addWatchlistEntry(data)
✅ kycService.checkUserAgainstWatchlist(userId)
```

### 🎯 Plan de Mejora: DDC Reforzada (21 SP)

**Sprint Siguiente:** Due Diligence Completa

1. **DueDiligencePage** (5 SP)
   - Wizard de 3 pasos: Simplificada → Normal → Reforzada
   - Selector automático según monto de transacción
   - Formularios dinámicos por nivel

2. **OriginOfFundsForm** (3 SP)
   - Formulario declaración de fondos
   - Upload de comprobantes (recibos, estados cuenta)
   - Validación de consistencia

3. **RiskAssessmentPage** (8 SP)
   - Dashboard de evaluación de riesgos
   - Matriz de factores (monto, cliente, origen, frecuencia, geografía)
   - Score automático y manual override

4. **ComplianceCalendarPage** (5 SP)
   - Calendario de obligaciones regulatorias
   - Alertas de vencimientos (607 DGII, capacitaciones PLD)
   - Tracking de completado

**Referencias:**

- Matriz completa: `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-ley-155-17.md`
- Procesos AML: Sección 2.2 "Detección de PEPs" y 2.3 "Monitoreo de Transacciones"

---

## ✅ INTEGRACIÓN CON SERVICIOS DE CONFIANZA Y SEGURIDAD

Este documento complementa:

- [process-matrix/15-CONFIANZA-SEGURIDAD/01-verificacion-identidad.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/01-verificacion-identidad.md) - **Verificación** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/05-okla-certified.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/05-okla-certified.md) - **OKLA Certified** ⭐

**Estado:** ✅ KYC 100% | ✅ TrustService 100% BE | 🔴 OKLA Certified 0%

### Servicios Involucrados

| Servicio             | Puerto | Función               | Estado              |
| -------------------- | ------ | --------------------- | ------------------- |
| KYCService           | 5025   | Verificación KYC/AML  | ✅ 100%             |
| TrustService         | 5082   | Badges y trust score  | ✅ 100% BE + 40% UI |
| CertificationService | 5092   | OKLA Certified (CPO)  | 🔴 0% (Fase 2)      |
| MediaService         | 5007   | Storage de documentos | ✅ 100%             |

### TrustService - Endpoints

| Método | Endpoint                           | Descripción                    | Auth |
| ------ | ---------------------------------- | ------------------------------ | ---- |
| `POST` | `/api/trust/verify-identity`       | Iniciar verificación identidad | ✅   |
| `GET`  | `/api/trust/verification-status`   | Estado de mi verificación      | ✅   |
| `POST` | `/api/trust/upload-document`       | Subir documento                | ✅   |
| `GET`  | `/api/trust/badges/{userId}`       | Badges de usuario              | ❌   |
| `POST` | `/api/trust/report-fraud`          | Reportar fraude                | ✅   |
| `GET`  | `/api/trust/seller-score/{userId}` | Score de confianza             | ❌   |

### TrustService - Procesos

| Proceso         | Nombre                           | Pasos | Archivo                      |
| --------------- | -------------------------------- | ----- | ---------------------------- |
| TRUST-KYC-001   | Verificar Identidad Individual   | 10    | 01-verificacion-identidad.md |
| TRUST-KYC-002   | Verificar Empresa (RNC)          | 8     | 01-verificacion-identidad.md |
| TRUST-VER-001   | Calcular Trust Score             | 7     | 01-verificacion-identidad.md |
| TRUST-BADGE-001 | Asignar Badge Automático         | 5     | 01-verificacion-identidad.md |
| TRUST-HIST-001  | Generar Historial Verificaciones | 4     | 01-verificacion-identidad.md |

### TrustService - Entidades

```csharp
// TrustService/TrustService.Domain/Entities/
public class IdentityVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public VerificationType Type { get; set; } // Individual, Business
    public DocumentType DocumentType { get; set; } // Cedula, RNC, Passport
    public string DocumentNumber { get; set; }
    public string FullName { get; set; }
    public DateTime? DocumentExpiry { get; set; }

    public string FrontImageUrl { get; set; }
    public string BackImageUrl { get; set; }
    public string SelfieUrl { get; set; }

    public VerificationStatus Status { get; set; } // Pending, Approved, Rejected
    public decimal ConfidenceScore { get; set; } // 0-100
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // Expira en 1 año
}

public class TrustBadge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public BadgeType Type { get; set; }
    // VerifiedSeller, VerifiedDealer, TrustedSeller, TopRatedSeller,
    // FastResponder, FoundingMember, PremiumDealer, SafeTransaction

    public string Name { get; set; }
    public string IconUrl { get; set; }
    public DateTime EarnedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

public class SellerTrustScore
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int TrustScore { get; set; } // 0-100
    public TrustLevel Level { get; set; } // New, Bronze, Silver, Gold, Platinum

    // Componentes del score
    public int IdentityScore { get; set; }
    public int TransactionScore { get; set; }
    public int ResponseScore { get; set; }
    public int ReviewScore { get; set; }
    public int ListingQualityScore { get; set; }

    public int TotalSales { get; set; }
    public decimal AverageRating { get; set; }
    public int FraudReportsConfirmed { get; set; }
}

public class FraudReport
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public Guid? ReportedVehicleId { get; set; }
    public FraudType Type { get; set; }
    public string Description { get; set; }
    public ReportStatus Status { get; set; }
}
```

### 🏆 OKLA Certified Pre-Owned (Planificado)

**Estado:** 🔴 0% - Planificado para Fase 2  
**Inspiración:** AutoTrader CPO, Cars.com Certified

**Criterios de Certificación:**

- Año: 2019 o más nuevo (máximo 7 años)
- Kilometraje: < 100,000 km
- Título limpio: Sin salvage, rebuilt, flood
- Sin accidentes estructurales
- Inspección de 150+ puntos completada
- Score mínimo: 85/100

**Beneficios:**

- ✅ Garantía OKLA: 6 meses o 10,000 km
- ✅ Reporte de historial completo
- ✅ 7 días de devolución
- ✅ Asistencia en carretera 24/7 (3 meses)
- ✅ Badge "OKLA Certified" en listing

**Endpoints Planificados:**

```typescript
GET    /api/certification/eligibility/{vehicleId}  # Verificar elegibilidad
POST   /api/certification/apply/{vehicleId}        # Solicitar certificación
GET    /api/certification/{vehicleId}              # Ver estado
GET    /api/certification/vehicles                 # Listar certificados
POST   /api/certification/{id}/inspection          # Subir inspección
POST   /api/certification/{id}/approve             # Aprobar (Admin)
```

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes Usuario](#componentes-usuario)
4. [Componentes Admin](#componentes-admin)
5. [Páginas](#páginas)
6. [Hooks y Servicios](#hooks-y-servicios)
7. [Tipos TypeScript](#tipos-typescript)
8. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### Flujo KYC Completo

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          KYC VERIFICATION FLOW                             │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1️⃣ INICIO DEL PROCESO                                                     │
│  Usuario accede a /verificacion                                           │
│  ├─ Si es Dealer: KYC obligatorio antes de publicar                       │
│  ├─ Si es Individual: Opcional (aumenta trust score)                      │
│  └─ Badge "Verificado" ✓ al completar                                     │
│                                                                            │
│  2️⃣ CREAR PERFIL KYC                                                       │
│  POST /api/kyc/profiles                                                    │
│  ├─ Datos personales: Nombre, Cédula, Fecha nacimiento                    │
│  ├─ Dirección completa                                                    │
│  ├─ Ocupación y fuente de ingresos                                        │
│  └─ Status: Pending → DB                                                  │
│                                                                            │
│  3️⃣ UPLOAD DOCUMENTOS (Multi-step)                                        │
│  POST /api/kyc/documents/upload                                           │
│  ├─ Step 1: Cédula frontal (JPG/PNG max 10MB)                            │
│  │   └─ Azure OCR extrae: nombre, cédula, fecha exp                      │
│  ├─ Step 2: Cédula reverso                                               │
│  │   └─ Verificar firma, código barras                                   │
│  ├─ Step 3: Comprobante domicilio (recibo luz/agua)                      │
│  │   └─ Fecha < 3 meses                                                  │
│  └─ Optional: Business License (solo dealers)                             │
│                                                                            │
│  4️⃣ VERIFICACIÓN BIOMÉTRICA (Liveness)                                    │
│  POST /api/kyc/identity-verification/start                                │
│  ├─ Tomar selfie del usuario                                             │
│  ├─ Liveness challenges:                                                  │
│  │   • "Sonríe" → detectar sonrisa                                       │
│  │   • "Gira a la izquierda" → detectar movimiento                       │
│  │   • "Parpadea 2 veces" → detectar parpadeo                            │
│  ├─ Azure Face API: comparar selfie vs cédula                            │
│  │   • Confidence score > 0.7 = Match ✓                                  │
│  │   • < 0.7 = Manual review                                             │
│  └─ POST /api/kyc/identity-verification/process-selfie                    │
│                                                                            │
│  5️⃣ WATCHLIST SCREENING (Automático)                                      │
│  POST /api/kyc/watchlist/screen                                           │
│  ├─ Check contra listas:                                                  │
│  │   • PEP (Personas Expuestas Políticamente)                            │
│  │   • OFAC Sanctions (USA)                                               │
│  │   • UN Sanctions                                                       │
│  │   • Interpol Red Notices                                               │
│  ├─ Match found? → Flag para manual review                               │
│  └─ No match? → Auto-approve (si docs OK)                                │
│                                                                            │
│  6️⃣ ADMIN REVIEW (Manual)                                                 │
│  GET /api/kyc/profiles?status=UnderReview                                 │
│  ├─ Admin Dashboard: /admin/kyc/queue                                     │
│  ├─ Ver documentos, selfie, scores                                        │
│  ├─ Acciones:                                                             │
│  │   • Approve → Status: Verified                                        │
│  │   • Reject → Status: Rejected + reason                                │
│  │   • Request More Info → Email al usuario                              │
│  └─ POST /api/kyc/profiles/{id}/approve                                   │
│                                                                            │
│  7️⃣ RESULTADO FINAL                                                        │
│  ├─ ✅ Verified → Badge "Verificado" en perfil                            │
│  ├─ ❌ Rejected → Email con razón + opción de re-aplicar                  │
│  └─ ⏳ Pending → "Tu verificación está en proceso"                        │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

### Regulación Cumplida

**Ley 155-17 (República Dominicana):**

- Prevención del Lavado de Activos
- Financiamiento del Terrorismo
- Proliferación de Armas de Destrucción Masiva

**Requisitos:**

- Identificación positiva de clientes
- Verificación de identidad con documentos oficiales
- Screening contra listas de sanciones
- Reportes de Transacciones Sospechosas (STR)

---

## 🔌 BACKEND API

### KYCService Endpoints (Ya Implementados ✅)

```typescript
// KYC Profiles
POST   /api/kyc/profiles                      # Crear perfil KYC
GET    /api/kyc/profiles/{id}                 # Obtener perfil
GET    /api/kyc/profiles?userId={id}          # Perfil por usuario
GET    /api/kyc/profiles?status=Pending       # Filtrar por status
PUT    /api/kyc/profiles/{id}                 # Actualizar perfil
POST   /api/kyc/profiles/{id}/approve         # Aprobar (admin)
POST   /api/kyc/profiles/{id}/reject          # Rechazar (admin)

// Documents
POST   /api/kyc/documents/upload              # Subir documento
GET    /api/kyc/documents?profileId={id}      # Docs por perfil
GET    /api/kyc/documents/{id}                # Obtener documento
DELETE /api/kyc/documents/{id}                # Eliminar documento
POST   /api/kyc/documents/{id}/verify         # Verificar (admin)

// Identity Verification (Biometric)
POST   /api/kyc/identity-verification/start                 # Iniciar sesión
POST   /api/kyc/identity-verification/process-document      # Procesar doc
POST   /api/kyc/identity-verification/process-selfie        # Procesar selfie
GET    /api/kyc/identity-verification/session/{id}          # Estado sesión
GET    /api/kyc/identity-verification/history?userId={id}   # Historial

// Watchlist & Compliance
POST   /api/kyc/watchlist/screen              # Screening PEP/Sanctions
GET    /api/kyc/watchlist/matches?profileId={id} # Ver matches
POST   /api/kyc/watchlist/resolve             # Resolver false positive

// Suspicious Transaction Reports (STR)
POST   /api/kyc/str/create                    # Crear reporte STR
GET    /api/kyc/str?status=Pending            # Listar reportes
POST   /api/kyc/str/{id}/submit               # Enviar a UAF (autoridad)
```

---

## 🎨 COMPONENTES USUARIO

### PASO 1: VerificationStatusBanner - Banner de Estado

```typescript
// filepath: src/components/kyc/VerificationStatusBanner.tsx
"use client";

import { CheckCircle, AlertTriangle, Clock, XCircle } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useKYCProfile } from "@/lib/hooks/useKYC";

export function VerificationStatusBanner() {
  const { data: profile, isLoading } = useKYCProfile();

  if (isLoading || !profile) return null;

  const statusConfig = {
    NotStarted: {
      icon: AlertTriangle,
      color: "yellow",
      title: "Verificación pendiente",
      message: "Verifica tu identidad para aumentar tu confianza en la plataforma",
      action: "Iniciar verificación",
      href: "/verificacion",
    },
    Pending: {
      icon: Clock,
      color: "blue",
      title: "Verificación en proceso",
      message: "Hemos recibido tus documentos. Revisión en 24-48 horas.",
      action: null,
      href: null,
    },
    UnderReview: {
      icon: Clock,
      color: "blue",
      title: "En revisión manual",
      message: "Nuestro equipo está revisando tu información",
      action: null,
      href: null,
    },
    Verified: {
      icon: CheckCircle,
      color: "green",
      title: "¡Cuenta verificada!",
      message: "Tu identidad ha sido verificada exitosamente",
      action: null,
      href: null,
    },
    Rejected: {
      icon: XCircle,
      color: "red",
      title: "Verificación rechazada",
      message: profile.rejectionReason || "No pudimos verificar tu identidad",
      action: "Intentar de nuevo",
      href: "/verificacion/retry",
    },
  };

  const config = statusConfig[profile.status];
  const Icon = config.icon;

  if (profile.status === "Verified") {
    // No mostrar banner si ya está verificado
    return null;
  }

  return (
    <div
      className={`bg-${config.color}-50 border border-${config.color}-200 rounded-lg p-4 mb-6`}
    >
      <div className="flex items-start gap-3">
        <Icon size={24} className={`text-${config.color}-600 flex-shrink-0`} />
        <div className="flex-1">
          <h3 className={`font-semibold text-${config.color}-900`}>
            {config.title}
          </h3>
          <p className={`text-sm text-${config.color}-700 mt-1`}>
            {config.message}
          </p>
        </div>
        {config.action && (
          <Button
            href={config.href}
            className={`bg-${config.color}-600 hover:bg-${config.color}-700`}
          >
            {config.action}
          </Button>
        )}
      </div>
    </div>
  );
}
```

---

### PASO 2: DocumentUploadCard - Subir Documento

```typescript
// filepath: src/components/kyc/DocumentUploadCard.tsx
"use client";

import { useState, useRef } from "react";
import { Upload, CheckCircle, AlertCircle, X } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useUploadDocument } from "@/lib/hooks/useKYC";

interface DocumentUploadCardProps {
  documentType: "IdentityCardFront" | "IdentityCardBack" | "ProofOfAddress" | "BusinessLicense";
  title: string;
  description: string;
  required: boolean;
  profileId: string;
}

export function DocumentUploadCard({
  documentType,
  title,
  description,
  required,
  profileId,
}: DocumentUploadCardProps) {
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { mutate: uploadDoc, isPending, isSuccess } = useUploadDocument();

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) return;

    // Validar tipo
    if (!["image/jpeg", "image/png", "application/pdf"].includes(selectedFile.type)) {
      alert("Solo se permiten JPG, PNG o PDF");
      return;
    }

    // Validar tamaño (max 10MB)
    if (selectedFile.size > 10 * 1024 * 1024) {
      alert("El archivo debe ser menor a 10MB");
      return;
    }

    setFile(selectedFile);

    // Preview
    if (selectedFile.type.startsWith("image/")) {
      const reader = new FileReader();
      reader.onloadend = () => setPreview(reader.result as string);
      reader.readAsDataURL(selectedFile);
    }
  };

  const handleUpload = () => {
    if (!file) return;

    const formData = new FormData();
    formData.append("file", file);
    formData.append("profileId", profileId);
    formData.append("documentType", documentType);

    uploadDoc(formData, {
      onSuccess: () => {
        // Keep file for preview but mark as uploaded
      },
    });
  };

  const handleRemove = () => {
    setFile(null);
    setPreview(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  return (
    <div className="bg-white rounded-lg border p-6">
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="font-semibold text-gray-900">{title}</h3>
          <p className="text-sm text-gray-600 mt-1">{description}</p>
        </div>
        {required && <Badge variant="primary">Requerido</Badge>}
        {isSuccess && <Badge variant="success">Subido ✓</Badge>}
      </div>

      {/* Upload area */}
      {!file && !isSuccess && (
        <div
          onClick={() => fileInputRef.current?.click()}
          className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center cursor-pointer hover:border-primary-500 transition"
        >
          <Upload size={48} className="mx-auto text-gray-400 mb-4" />
          <p className="text-gray-600 mb-2">
            Click para subir o arrastra el archivo aquí
          </p>
          <p className="text-sm text-gray-500">JPG, PNG o PDF (max 10MB)</p>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,application/pdf"
            onChange={handleFileSelect}
            className="hidden"
          />
        </div>
      )}

      {/* Preview */}
      {(file || isSuccess) && (
        <div className="space-y-4">
          {preview && (
            <img
              src={preview}
              alt="Preview"
              className="w-full h-48 object-cover rounded-lg"
            />
          )}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              {isSuccess ? (
                <CheckCircle size={20} className="text-green-600" />
              ) : (
                <AlertCircle size={20} className="text-gray-600" />
              )}
              <span className="text-sm text-gray-700">
                {file?.name || "Archivo subido"}
              </span>
            </div>
            <div className="flex items-center gap-2">
              {!isSuccess && (
                <>
                  <Button
                    size="sm"
                    onClick={handleUpload}
                    disabled={isPending}
                  >
                    {isPending ? "Subiendo..." : "Subir"}
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={handleRemove}
                  >
                    <X size={16} />
                  </Button>
                </>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Info adicional */}
      {documentType === "IdentityCardFront" && (
        <div className="mt-4 bg-blue-50 rounded-lg p-3">
          <p className="text-xs text-blue-700">
            💡 <strong>Tip:</strong> Asegúrate de que la cédula esté bien iluminada,
            enfocada y sin reflejos. Los datos deben ser legibles.
          </p>
        </div>
      )}
    </div>
  );
}
```

---

### PASO 3: BiometricVerification - Selfie con Liveness

```typescript
// filepath: src/components/kyc/BiometricVerification.tsx
"use client";

import { useState, useRef, useEffect } from "react";
import { Camera, CheckCircle, AlertCircle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useStartVerification, useProcessSelfie } from "@/lib/hooks/useKYC";

export function BiometricVerification({ profileId }: { profileId: string }) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [challenge, setChallenge] = useState<string | null>(null);
  const [capturedImage, setCapturedImage] = useState<string | null>(null);

  const { mutate: startVerification, data: session } = useStartVerification();
  const { mutate: processSelfie, isPending, isSuccess } = useProcessSelfie();

  const challenges = [
    "Sonríe 😊",
    "Gira tu cabeza a la izquierda ⬅️",
    "Parpadea 2 veces 👁️",
  ];

  useEffect(() => {
    // Iniciar sesión de verificación
    startVerification(profileId);
  }, [profileId]);

  const startCamera = async () => {
    try {
      const mediaStream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: "user", width: 1280, height: 720 },
      });
      setStream(mediaStream);
      if (videoRef.current) {
        videoRef.current.srcObject = mediaStream;
      }
      // Random challenge
      setChallenge(challenges[Math.floor(Math.random() * challenges.length)]);
    } catch (error) {
      console.error("Error accessing camera:", error);
      alert("No se pudo acceder a la cámara. Verifica los permisos.");
    }
  };

  const capturePhoto = () => {
    if (!videoRef.current || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const video = videoRef.current;

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    ctx.drawImage(video, 0, 0);
    const imageData = canvas.toDataURL("image/jpeg", 0.8);
    setCapturedImage(imageData);

    // Stop camera
    stream?.getTracks().forEach((track) => track.stop());
    setStream(null);
  };

  const retake = () => {
    setCapturedImage(null);
    startCamera();
  };

  const submitSelfie = () => {
    if (!capturedImage || !session) return;

    // Convert base64 to blob
    fetch(capturedImage)
      .then((res) => res.blob())
      .then((blob) => {
        const formData = new FormData();
        formData.append("selfie", blob, "selfie.jpg");
        formData.append("sessionId", session.id);

        processSelfie(formData);
      });
  };

  return (
    <div className="bg-white rounded-lg border p-6">
      <h2 className="text-xl font-semibold text-gray-900 mb-4">
        Verificación Biométrica
      </h2>

      {/* Instructions */}
      {!stream && !capturedImage && (
        <div className="text-center py-8">
          <Camera size={64} className="mx-auto text-gray-400 mb-4" />
          <h3 className="font-semibold text-gray-900 mb-2">
            Vamos a verificar tu identidad
          </h3>
          <p className="text-gray-600 mb-6">
            Tomaremos una selfie para compararla con tu cédula
          </p>
          <Button onClick={startCamera}>
            <Camera size={18} className="mr-2" />
            Iniciar cámara
          </Button>
        </div>
      )}

      {/* Camera view */}
      {stream && !capturedImage && (
        <div className="space-y-4">
          {/* Challenge badge */}
          <Badge variant="primary" size="lg" className="mb-4">
            {challenge}
          </Badge>

          <div className="relative rounded-lg overflow-hidden bg-black">
            <video
              ref={videoRef}
              autoPlay
              playsInline
              className="w-full"
            />
            {/* Oval guide */}
            <div className="absolute inset-0 flex items-center justify-center">
              <div className="w-64 h-80 border-4 border-white rounded-full opacity-50" />
            </div>
          </div>

          <Button onClick={capturePhoto} className="w-full">
            Capturar foto
          </Button>
        </div>
      )}

      {/* Preview & Submit */}
      {capturedImage && !isSuccess && (
        <div className="space-y-4">
          <img
            src={capturedImage}
            alt="Selfie"
            className="w-full rounded-lg"
          />
          <div className="flex gap-3">
            <Button
              variant="outline"
              onClick={retake}
              className="flex-1"
            >
              <RefreshCw size={16} className="mr-2" />
              Tomar otra
            </Button>
            <Button
              onClick={submitSelfie}
              disabled={isPending}
              className="flex-1"
            >
              {isPending ? "Verificando..." : "Enviar selfie"}
            </Button>
          </div>
        </div>
      )}

      {/* Success */}
      {isSuccess && (
        <div className="text-center py-8">
          <CheckCircle size={64} className="mx-auto text-green-600 mb-4" />
          <h3 className="font-semibold text-gray-900 mb-2">
            ¡Verificación completada!
          </h3>
          <p className="text-gray-600">
            Tu selfie ha sido procesada exitosamente
          </p>
        </div>
      )}

      <canvas ref={canvasRef} className="hidden" />
    </div>
  );
}
```

---

## 👨‍💼 COMPONENTES ADMIN

### PASO 4: KYCQueueTable - Cola de Revisión Admin

```typescript
// filepath: src/components/admin/kyc/KYCQueueTable.tsx
"use client";

import { useState } from "react";
import { Eye, CheckCircle, XCircle, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useKYCQueue, useApproveProfile, useRejectProfile } from "@/lib/hooks/useKYC";
import { ReviewModal } from "./ReviewModal";

export function KYCQueueTable() {
  const [selectedProfile, setSelectedProfile] = useState<string | null>(null);
  const { data: queue, isLoading } = useKYCQueue({ status: "UnderReview" });
  const { mutate: approve } = useApproveProfile();
  const { mutate: reject } = useRejectProfile();

  if (isLoading) {
    return <div>Cargando cola...</div>;
  }

  const statusColors = {
    Pending: "yellow",
    UnderReview: "blue",
    Verified: "green",
    Rejected: "red",
  };

  return (
    <>
      <div className="bg-white rounded-lg border overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Usuario
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Cédula
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Docs
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Fecha
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Acciones
              </th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {queue?.items.map((profile) => (
              <tr key={profile.id} className="hover:bg-gray-50">
                <td className="px-6 py-4">
                  <div>
                    <p className="font-medium text-gray-900">
                      {profile.fullName}
                    </p>
                    <p className="text-sm text-gray-500">{profile.email}</p>
                  </div>
                </td>
                <td className="px-6 py-4 text-sm text-gray-900">
                  {profile.identityNumber}
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center gap-2">
                    {profile.documentsCount > 0 ? (
                      <CheckCircle size={16} className="text-green-600" />
                    ) : (
                      <AlertTriangle size={16} className="text-yellow-600" />
                    )}
                    <span className="text-sm">{profile.documentsCount}/3</span>
                  </div>
                </td>
                <td className="px-6 py-4">
                  <Badge variant={statusColors[profile.status]}>
                    {profile.status}
                  </Badge>
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">
                  {new Date(profile.submittedAt).toLocaleDateString("es-DO")}
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setSelectedProfile(profile.id)}
                    >
                      <Eye size={14} className="mr-1" />
                      Revisar
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Review Modal */}
      {selectedProfile && (
        <ReviewModal
          profileId={selectedProfile}
          onClose={() => setSelectedProfile(null)}
          onApprove={(id) => {
            approve(id);
            setSelectedProfile(null);
          }}
          onReject={(id, reason) => {
            reject({ id, reason });
            setSelectedProfile(null);
          }}
        />
      )}
    </>
  );
}
```

---

## 📄 PÁGINAS

### PASO 5: Página de Verificación Usuario

```typescript
// filepath: src/app/(main)/verificacion/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Shield, FileText, Camera, CheckCircle } from "lucide-react";
import { auth } from "@/lib/auth";
import { VerificationStepper } from "@/components/kyc/VerificationStepper";

export const metadata: Metadata = {
  title: "Verificación de Identidad | OKLA",
};

export default async function VerificationPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/verificacion");
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="text-center mb-12">
        <Shield size={64} className="mx-auto text-primary-600 mb-4" />
        <h1 className="text-3xl font-bold text-gray-900">
          Verificación de Identidad
        </h1>
        <p className="text-gray-600 mt-2">
          Verifica tu identidad para aumentar la confianza en la plataforma
        </p>
      </div>

      {/* Steps overview */}
      <div className="grid grid-cols-3 gap-6 mb-12">
        <div className="text-center">
          <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-3">
            <FileText size={28} className="text-primary-600" />
          </div>
          <h3 className="font-semibold text-gray-900">1. Documentos</h3>
          <p className="text-sm text-gray-600 mt-1">Sube tu cédula</p>
        </div>
        <div className="text-center">
          <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-3">
            <Camera size={28} className="text-primary-600" />
          </div>
          <h3 className="font-semibold text-gray-900">2. Selfie</h3>
          <p className="text-sm text-gray-600 mt-1">Verificación facial</p>
        </div>
        <div className="text-center">
          <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-3">
            <CheckCircle size={28} className="text-primary-600" />
          </div>
          <h3 className="font-semibold text-gray-900">3. Listo</h3>
          <p className="text-sm text-gray-600 mt-1">Revisión 24-48h</p>
        </div>
      </div>

      {/* Stepper component */}
      <VerificationStepper userId={session.user.id} />
    </div>
  );
}
```

---

### PASO 6: Dashboard Admin KYC

```typescript
// filepath: src/app/(admin)/admin/kyc/queue/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Shield } from "lucide-react";
import { auth } from "@/lib/auth";
import { KYCQueueTable } from "@/components/admin/kyc/KYCQueueTable";
import { KYCStats } from "@/components/admin/kyc/KYCStats";

export const metadata: Metadata = {
  title: "Cola de Verificación KYC | Admin OKLA",
};

export default async function KYCQueuePage() {
  const session = await auth();

  if (!session?.user || session.user.role !== "Admin") {
    redirect("/login");
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-8">
        <Shield size={32} className="text-primary-600" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Cola de Verificación KYC
          </h1>
          <p className="text-gray-600">
            Revisión manual de perfiles pendientes
          </p>
        </div>
      </div>

      {/* Stats cards */}
      <KYCStats />

      {/* Queue table */}
      <div className="mt-8">
        <KYCQueueTable />
      </div>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 7: KYC Hooks

```typescript
// filepath: src/lib/hooks/useKYC.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { kycService } from "@/lib/services/kycService";
import { toast } from "sonner";

export function useKYCProfile() {
  return useQuery({
    queryKey: ["kycProfile"],
    queryFn: () => kycService.getCurrentUserProfile(),
  });
}

export function useCreateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) => kycService.createProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["kycProfile"] });
      toast.success("Perfil KYC creado");
    },
  });
}

export function useUploadDocument() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (formData: FormData) => kycService.uploadDocument(formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["kycProfile"] });
      toast.success("Documento subido correctamente");
    },
  });
}

export function useStartVerification() {
  return useMutation({
    mutationFn: (profileId: string) =>
      kycService.startVerificationSession(profileId),
  });
}

export function useProcessSelfie() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (formData: FormData) => kycService.processSelfie(formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["kycProfile"] });
      toast.success("Selfie procesada correctamente");
    },
  });
}

// Admin hooks
export function useKYCQueue(params?: { status?: string }) {
  return useQuery({
    queryKey: ["kycQueue", params],
    queryFn: () => kycService.getQueue(params),
  });
}

export function useApproveProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (profileId: string) => kycService.approveProfile(profileId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["kycQueue"] });
      toast.success("Perfil aprobado");
    },
  });
}

export function useRejectProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      kycService.rejectProfile(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["kycQueue"] });
      toast.success("Perfil rechazado");
    },
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 8: KYC Types

```typescript
// filepath: src/types/kyc.ts
export type KYCStatus =
  | "NotStarted"
  | "Pending"
  | "UnderReview"
  | "Verified"
  | "Rejected";

export type DocumentType =
  | "IdentityCardFront"
  | "IdentityCardBack"
  | "ProofOfAddress"
  | "BusinessLicense"
  | "TaxRegistration";

export interface KYCProfile {
  id: string;
  userId: string;
  fullName: string;
  identityNumber: string; // Cédula
  dateOfBirth: string;
  address: string;
  city: string;
  province: string;
  occupation: string;
  status: KYCStatus;
  submittedAt?: string;
  reviewedAt?: string;
  reviewedBy?: string;
  rejectionReason?: string;
  documentsCount: number;
  biometricVerified: boolean;
  watchlistScreened: boolean;
}

export interface KYCDocument {
  id: string;
  profileId: string;
  documentType: DocumentType;
  fileUrl: string;
  fileName: string;
  uploadedAt: string;
  verificationStatus: "Pending" | "Approved" | "Rejected";
  ocrData?: {
    fullName?: string;
    identityNumber?: string;
    dateOfBirth?: string;
    expirationDate?: string;
  };
}

export interface VerificationSession {
  id: string;
  profileId: string;
  status: "Active" | "Completed" | "Failed";
  startedAt: string;
  completedAt?: string;
  faceMatchScore?: number;
  livenessScore?: number;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Flujo Usuario:
# - /verificacion muestra stepper con 3 pasos
# - Subir cédula frontal/reverso funciona (max 10MB)
# - Preview de imagen antes de enviar
# - Capturar selfie con cámara funciona
# - Liveness challenges aparecen random
# - Estado "En revisión" se muestra después

# Verificar Admin Dashboard:
# - /admin/kyc/queue lista perfiles pendientes
# - Filtrar por status funciona
# - Abrir modal review muestra documentos + selfie
# - Aprobar/Rechazar funciona con razón
# - Stats cards actualizan en tiempo real
```

---

## 🚀 MEJORAS FUTURAS

1. **OCR Automático**: Extraer datos de cédula con Azure Computer Vision
2. **Liveness Detection Avanzado**: Multiple challenges + 3D face mapping
3. **Blockchain Verification**: Hash de documentos en blockchain
4. **International IDs**: Pasaportes, licencias de otros países
5. **Watchlist Real-Time API**: Integración con Dow Jones RiskCenter

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/kyc-verification.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser, loginAsAdmin } from "../helpers/auth";

test.describe("KYC Verification", () => {
  test.describe("User Flow", () => {
    test.beforeEach(async ({ page }) => {
      await loginAsUser(page);
    });

    test("debe mostrar página de verificación de identidad", async ({
      page,
    }) => {
      await page.goto("/verificacion/identidad");

      await expect(
        page.getByRole("heading", { name: /verificar identidad/i }),
      ).toBeVisible();
      await expect(page.getByText(/cédula|pasaporte/i)).toBeVisible();
    });

    test("debe subir documento de identidad", async ({ page }) => {
      await page.goto("/verificacion/identidad");

      const fileInput = page.locator('input[type="file"]');
      await fileInput.setInputFiles("./fixtures/cedula-front.jpg");

      await expect(page.getByTestId("document-preview")).toBeVisible();
    });

    test("debe completar liveness check", async ({ page }) => {
      await page.goto("/verificacion/selfie");

      await expect(page.getByTestId("camera-preview")).toBeVisible();
      await page.getByRole("button", { name: /tomar foto/i }).click();

      await expect(page.getByText(/verificando/i)).toBeVisible();
    });
  });

  test.describe("Admin Queue", () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test("debe mostrar cola de KYC pendientes", async ({ page }) => {
      await page.goto("/admin/kyc/queue");

      await expect(page.getByTestId("kyc-queue")).toBeVisible();
      await expect(page.getByTestId("kyc-item")).toHaveCount({ min: 0 });
    });

    test("debe aprobar solicitud KYC", async ({ page }) => {
      await page.goto("/admin/kyc/queue");

      await page.getByTestId("kyc-item").first().click();
      await expect(page.getByRole("dialog")).toBeVisible();

      await page.getByRole("button", { name: /aprobar/i }).click();
      await expect(page.getByText(/aprobado exitosamente/i)).toBeVisible();
    });

    test("debe rechazar con razón", async ({ page }) => {
      await page.goto("/admin/kyc/queue");

      await page.getByTestId("kyc-item").first().click();
      await page.getByRole("button", { name: /rechazar/i }).click();

      await page.fill('textarea[name="reason"]', "Documento ilegible");
      await page.getByRole("button", { name: /confirmar rechazo/i }).click();

      await expect(page.getByText(/rechazado/i)).toBeVisible();
    });
  });
});
```

---

**Siguiente documento:** `94-oauth-management.md` - Gestión de proveedores OAuth
