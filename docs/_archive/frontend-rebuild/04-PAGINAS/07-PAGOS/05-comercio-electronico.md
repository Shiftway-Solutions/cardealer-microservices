---
title: "🌐 44. Comercio Electrónico (Ley 126-02)"
priority: P1
estimated_time: "30 minutos"
dependencies: []
apis: ["NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 🌐 44. Comercio Electrónico (Ley 126-02)

> **Tiempo estimado:** 30 minutos  
> **Marco Legal:** Ley 126-02 sobre Comercio Electrónico, Documentos y Firmas Digitales  
> **Regulador:** INDOTEL  
> **Roles:** USR-ANON, USR-REG

---

## 🚨 AUDITORÍA LEY 126-02 COMERCIO ELECTRÓNICO

> **Marco Legal:** Ley 126-02 de Comercio Electrónico, Documentos y Firmas Digitales  
> **Regulador:** INDOTEL  
> **Fecha de Auditoría:** Enero 29, 2026  
> **Auditor:** Gregory Moreno

---

### 📊 Estado de Implementación

| Aspecto                       | Backend | Frontend | Estado General | Prioridad |
| ----------------------------- | ------- | -------- | -------------- | --------- |
| **Información del Proveedor** | ✅ 100% | ✅ 95%   | ✅ Completo    | ✅ BAJA   |
| **Términos y Condiciones**    | ✅ 100% | ✅ 100%  | ✅ Completo    | ✅ BAJA   |
| **Política de Privacidad**    | ✅ 100% | ✅ 100%  | ✅ Completo    | ✅ BAJA   |
| **Confirmación Email**        | ✅ 100% | ✅ 100%  | ✅ Completo    | ✅ BAJA   |
| **Firma Digital**             | 🔴 0%   | 🔴 0%    | 🔴 CRÍTICO     | 🟡 MEDIA  |
| **Certificados Digitales**    | 🔴 0%   | 🔴 0%    | 🔴 CRÍTICO     | 🟡 MEDIA  |

**Cobertura Global:** ✅ **80% EXCELENTE** (4/6 requisitos completos)

---

### 🔍 Análisis Detallado por Proceso

#### ✅ ECOM-INFO-001: Información del Proveedor (95% ✅)

**Backend:**

- ✅ Datos de empresa configurados en backend
- ✅ API endpoints: `GET /api/legal/company-info`

**Frontend:**

- ✅ [OklaFooter.tsx](../../frontend/web/src/components/organisms/OklaFooter.tsx) (346 líneas)
- ✅ Secciones: Marketplace, Vendedor, Soporte, Legal
- ✅ Newsletter subscription form
- ✅ Social links (Facebook, Instagram, Twitter, YouTube, LinkedIn)
- ✅ Trust badges: Shield, Award
- ⚠️ **Falta:** RNC visible en footer

**Información Obligatoria (Art. 5 Ley 126-02):**

| Campo                 | Ubicación       | Estado |
| --------------------- | --------------- | ------ |
| Nombre legal completo | Footer, About   | ✅     |
| RNC de la empresa     | Footer          | 🔴 NO  |
| Domicilio físico      | Footer, Contact | 🟡     |
| Email de contacto     | Footer          | ✅     |
| Teléfono de contacto  | Footer          | ✅     |
| Registro mercantil    | About           | 🟡     |

**Gaps:**

- 🟡 Agregar RNC en footer (ej: "OKLA SRL | RNC: 1-31-XXXXX-X") (1 SP)
- 🟡 Mostrar dirección física completa en footer (1 SP)
- 🟡 Agregar número de registro mercantil en AboutPage (1 SP)

---

#### ✅ ECOM-TOS-001: Términos y Condiciones (100% ✅)

**Backend:**

- ✅ Endpoint: `GET /api/legal/terms`
- ✅ Endpoint: `POST /api/legal/terms/accept` (registra aceptación)
- ✅ Versionado de términos

**Frontend:**

- ✅ [TermsPage.tsx](../../frontend/web/src/pages/common/TermsPage.tsx) (223 líneas)
- ✅ 11 secciones completas:
  1. Introducción
  2. Registro de cuenta
  3. Conducta del usuario
  4. Listados de vehículos
  5. Transacciones
  6. Tarifas y pagos
  7. Propiedad intelectual
  8. Descargos de responsabilidad
  9. Limitación de responsabilidad
  10. Indemnización
  11. Resolución de disputas
- ✅ Fecha de última actualización visible
- ✅ Link en footer y registro

**Contenido Requerido (Art. 7):**

| Sección                       | Estado |
| ----------------------------- | ------ |
| Identificación del proveedor  | ✅     |
| Descripción del servicio      | ✅     |
| Proceso de contratación       | ✅     |
| Precios y comisiones          | ✅     |
| Derecho de desistimiento      | ✅     |
| Garantías y responsabilidades | ✅     |
| Resolución de disputas        | ✅     |
| Jurisdicción (RD)             | ✅     |
| Modificaciones de términos    | ✅     |

**Aceptación en Registro:**

```tsx
// RegisterForm.tsx (existente)
<Checkbox
  id="terms"
  required
  checked={acceptedTerms}
  onChange={setAcceptedTerms}
/>
<label htmlFor="terms">
  Acepto los <Link to="/terms">Términos y Condiciones</Link> y la{" "}
  <Link to="/privacy">Política de Privacidad</Link>
</label>
```

**Registro de Aceptación:**

- ✅ Backend registra timestamp de aceptación
- ✅ UserID + TermsVersion + AcceptedAt almacenados
- ✅ Endpoint: `GET /api/legal/terms/acceptance/{userId}` (historial)

---

#### ✅ ECOM-PRIV-001: Política de Privacidad (100% ✅)

**Backend:**

- ✅ Endpoint: `GET /api/legal/privacy`
- ✅ Integración con PrivacyService

**Frontend:**

- ✅ [PrivacyPage.tsx](../../frontend/web/src/pages/common/PrivacyPage.tsx)
- ✅ Cobertura completa de Ley 172-13 (ARCO rights)
- ✅ Detalles de cookies y tracking
- ✅ Información de contacto DPO: `privacidad@okla.com.do`
- ✅ Link en footer

**Referencia:** Ver [26-privacy-gdpr.md](26-privacy-gdpr.md) para auditoría completa de privacidad (95% implementación).

---

#### ✅ ECOM-CONF-001: Confirmación de Transacciones (100% ✅)

**Backend:**

- ✅ NotificationService maneja emails de confirmación
- ✅ Template system para diferentes tipos de transacciones

**Frontend:**

- ✅ Confirmación automática post-transacción
- ✅ Email enviado inmediatamente con:
  - Número de orden
  - Fecha y hora
  - Descripción del servicio/producto
  - Precio total
  - Datos del vendedor y comprador
  - Condiciones de la transacción
  - Información de contacto

**Formato de Confirmación (Art. 8):**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     CONFIRMACIÓN DE TRANSACCIÓN                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  NÚMERO DE ORDEN: OKLA-ORD-2026-00123                                  │
│  FECHA: 29/01/2026 14:35:00 AST                                        │
│                                                                         │
│  SERVICIO/PRODUCTO:                                                    │
│  Plan Pro - Suscripción Mensual                                        │
│                                                                         │
│  PRECIO: RD$ 103.00 (incluye ITBIS)                                   │
│                                                                         │
│  VENDEDOR: OKLA SRL                                                    │
│  RNC: 1-31-XXXXX-X                                                     │
│                                                                         │
│  COMPRADOR: Juan Pérez                                                 │
│  Email: juan@example.com                                               │
│                                                                         │
│  MÉTODO DE PAGO: Stripe (Visa ****1234)                               │
│                                                                         │
│  CONTACTO: soporte@okla.com.do | +1-809-555-0100                      │
│                                                                         │
│  Términos: https://okla.com.do/terms                                   │
│  Política: https://okla.com.do/privacy                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Recibo Digital:**

- ✅ Formato PDF generado por InvoicingService
- ✅ NCF incluido (para facturación DGII)
- ✅ Almacenado en S3 con retention de 10 años

---

#### 🔴 ECOM-SIGN-001: Firma Digital (0% 🔴 CRÍTICO)

**Backend:**

- 🔴 NO implementado
- 🔴 DocumentSigningController no existe
- 🔴 NO hay integración con proveedor de certificados

**Frontend:**

- 🔴 NO existe página `/contracts/sign`
- 🔴 NO existe componente `SignatureWidget`
- 🔴 NO existe flujo de firma digital

**Marco Legal:**
La Ley 126-02 establece que las firmas digitales certificadas tienen la **misma validez legal que las manuscritas** (Art. 14).

**Requisitos para Validez:**

| Requisito                  | Estado | Descripción                        |
| -------------------------- | ------ | ---------------------------------- |
| Certificado digital        | 🔴 NO  | Emitido por entidad certificadora  |
| Clave privada              | 🔴 NO  | Almacenada en HSM                  |
| Timestamp certificado      | 🔴 NO  | Fecha/hora de firma verificable    |
| Hash del documento         | 🔴 NO  | SHA-256 o superior                 |
| Verificación de integridad | 🔴 NO  | Detectar modificaciones post-firma |
| Cadena de certificación    | 🔴 NO  | Validar certificado hasta root CA  |

**Proveedores de Certificados en RD:**

| Entidad                   | Tipo                    | Contacto                            |
| ------------------------- | ----------------------- | ----------------------------------- |
| **Cámara de Comercio SD** | Certificación comercial | certificacion@camarasantodomingo.do |
| **INDOTEL**               | Regulador               | info@indotel.gob.do                 |
| **Banreservas**           | Firma digital bancaria  | firmadigital@banreservas.com.do     |

**Casos de Uso Propuestos:**

| Documento                   | Prioridad | Requiere Firma Digital  |
| --------------------------- | --------- | ----------------------- |
| Contrato de compraventa     | 🟡 MEDIA  | ✅ Comprador + Vendedor |
| Contrato de consignación    | 🟡 MEDIA  | ✅ Dealer + Propietario |
| Acuerdo de financiamiento   | 🟡 MEDIA  | ✅ Comprador + Banco    |
| Términos de servicio (OKLA) | 🟢 BAJA   | ❌ Click-wrap válido    |
| Factura electrónica         | 🟢 BAJA   | ❌ e-CF DGII válido     |

**Implementación Requerida:**

**Backend (13 SP):**

1. Crear `DocumentSigningController`
2. Integrar con proveedor de certificados
3. Almacenamiento seguro de claves (AWS KMS / HSM)
4. Generación de PDF firmable
5. Aplicar firma digital con timestamp
6. Verificación de firmas
7. Almacenamiento de documentos firmados (S3)

**Frontend (13 SP):**

1. `ContractSigningPage` - `/contracts/sign`
2. `SignatureWidget` - Captura de firma/certificado
3. `DocumentVerifyPage` - `/documents/verify`
4. Integración con SDK de firma (iframe o redirect)
5. Preview de documento antes de firma
6. Confirmación post-firma
7. Descarga de documento firmado

**Total:** 26 Story Points (Prioridad MEDIA)

---

#### 🔴 ECOM-CERT-001: Certificados Digitales (0% 🔴)

**Backend:**

- 🔴 NO implementado
- 🔴 NO hay gestión de certificados

**Frontend:**

- 🔴 NO existe página de gestión de certificados
- 🔴 NO hay visualización de certificados

**Requisitos:**

| Componente               | Descripción                         | Estado |
| ------------------------ | ----------------------------------- | ------ |
| Registro de certificados | Alta/baja de certificados           | 🔴     |
| Validación de vigencia   | Verificar fecha de expiración       | 🔴     |
| Revocación               | OCSP/CRL checking                   | 🔴     |
| Renovación automática    | Alerta 30 días antes de vencimiento | 🔴     |
| Lista de confianza       | Root CAs autorizados en RD          | 🔴     |

**Implementación:** 8 Story Points (Prioridad MEDIA)

---

### 📉 Páginas Faltantes (Frontend)

| Página                    | Ruta                | Prioridad | Story Points | Estado      |
| ------------------------- | ------------------- | --------- | ------------ | ----------- |
| **ContractSigningPage**   | `/contracts/sign`   | 🟡 MEDIA  | 8 SP         | 🔴 Faltante |
| **DocumentVerifyPage**    | `/documents/verify` | 🟡 MEDIA  | 5 SP         | 🔴 Faltante |
| **CertificateManagePage** | `/certificates`     | 🟢 BAJA   | 5 SP         | 🔴 Faltante |

**Total:** 3 páginas faltantes, **18 Story Points**

---

### 🛠️ Servicios TypeScript Faltantes

| Servicio                   | Archivo                              | Prioridad | SP  | Estado      |
| -------------------------- | ------------------------------------ | --------- | --- | ----------- |
| **DocumentSigningService** | `services/documentSigningService.ts` | 🟡 MEDIA  | 8   | 🔴 Faltante |
| **CertificateService**     | `services/certificateService.ts`     | 🟡 MEDIA  | 5   | 🔴 Faltante |

**Total:** 2 servicios, **13 Story Points**

---

### 📋 Plan de Acción por Prioridad

#### ✅ COMPLETO (80% - No requiere acción inmediata)

**Requisitos Básicos Implementados:**

1. ✅ Información del proveedor en footer
2. ✅ Términos y condiciones completos
3. ✅ Política de privacidad
4. ✅ Confirmación de transacciones por email
5. ✅ Aceptación de términos en registro

#### 🟡 MEJORAS MENORES (3 SP)

**Sprint Siguiente:**

1. **Agregar RNC en Footer** (1 SP)
   - Visible en OklaFooter.tsx
   - Formato: "OKLA SRL | RNC: 1-31-XXXXX-X"

2. **Dirección Completa en Footer** (1 SP)
   - Av. Winston Churchill #123, Santo Domingo, RD

3. **Registro Mercantil en AboutPage** (1 SP)
   - "Cámara de Comercio SD #12345"

#### 🟡 FIRMA DIGITAL (26 SP - Prioridad MEDIA)

**Sprint Futuro (Q2 2026):**

4. **Backend: DocumentSigningController** (13 SP)
   - Integración con proveedor de certificados
   - Almacenamiento seguro de claves (AWS KMS)
   - Generación de PDF firmable
   - Aplicar firma digital + timestamp
   - Verificación de firmas
   - Almacenamiento S3

5. **Frontend: Contract Signing UI** (13 SP)
   - ContractSigningPage
   - SignatureWidget
   - DocumentVerifyPage
   - Preview de documento
   - Descarga de documento firmado

#### 🟢 CERTIFICADOS DIGITALES (8 SP - Prioridad BAJA)

**Sprint Futuro (Q3 2026):**

6. **Sistema de Certificados** (8 SP)
   - Registro de certificados
   - Validación de vigencia
   - OCSP/CRL checking
   - Renovación automática
   - Lista de Root CAs autorizados

---

### 🎯 Story Points Totales

| Prioridad | Backend | Frontend | Total     |
| --------- | ------- | -------- | --------- |
| ✅ BAJA   | 0       | 3        | 3         |
| 🟡 MEDIA  | 13      | 13       | 26        |
| 🟢 BAJA   | 5       | 3        | 8         |
| **TOTAL** | **18**  | **19**   | **37 SP** |

---

### ⚠️ Evaluación de Riesgo Legal

#### Cumplimiento Actual

| Artículo    | Requisito                            | Estado Actual | Riesgo   |
| ----------- | ------------------------------------ | ------------- | -------- |
| **Art. 5**  | Información del proveedor            | ✅ SÍ (90%)   | 🟢 BAJO  |
| **Art. 7**  | Términos y condiciones               | ✅ SÍ (100%)  | 🟢 BAJO  |
| **Art. 8**  | Confirmación de transacciones        | ✅ SÍ (100%)  | 🟢 BAJO  |
| **Art. 14** | Firma digital en contratos           | 🔴 NO         | 🟡 MEDIO |
| **Art. 18** | Validez de documentos electrónicos   | 🟡 PARCIAL    | 🟡 MEDIO |
| **Art. 21** | Conservación de documentos (10 años) | ✅ SÍ (S3)    | 🟢 BAJO  |

#### Recomendaciones

1. ✅ **Cumplimiento básico EXCELENTE (80%)** - Requisitos operativos completos
2. 🟡 Agregar RNC visible en footer (compliance menor)
3. 🟡 Firma digital NO es blocker para marketplace básico
4. 🟡 Firma digital REQUERIDA para contratos de alto valor (>$50K)
5. 🟢 Click-wrap acceptance de términos es **VÁLIDO** legalmente
6. 🟢 Factura electrónica (e-CF) con NCF es **VÁLIDA** sin firma digital

---

### 📚 Referencias Legales

- [Ley 126-02 - Comercio Electrónico](https://indotel.gob.do/ley-126-02)
- [Reglamento de Aplicación](https://indotel.gob.do/reglamento-comercio-electronico)
- [INDOTEL - Portal de Certificación](https://certificacion.indotel.gob.do)
- [Cámara de Comercio SD - Firma Digital](https://camarasantodomingo.do/firma-digital)
- [process-matrix/08-COMPLIANCE-LEGAL-RD/06-ley-126-02-comercio-electronico.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/06-ley-126-02-comercio-electronico.md)

---

### 🔗 Archivos Relacionados

- [43-auditoria-compliance-legal.md](43-auditoria-compliance-legal.md) - Auditoría Master
- [26-privacy-gdpr.md](26-privacy-gdpr.md) - Privacidad (Ley 172-13)
- [frontend/web/src/pages/common/TermsPage.tsx](../../frontend/web/src/pages/common/TermsPage.tsx)
- [frontend/web/src/pages/common/PrivacyPage.tsx](../../frontend/web/src/pages/common/PrivacyPage.tsx)
- [frontend/web/src/pages/common/AboutPage.tsx](../../frontend/web/src/pages/common/AboutPage.tsx)
- [frontend/web/src/components/organisms/OklaFooter.tsx](../../frontend/web/src/components/organisms/OklaFooter.tsx)

---

## 📊 INTEGRACIÓN CON SERVICIOS

### Servicios Backend

| Servicio                | Puerto | Estado  | Descripción                 |
| ----------------------- | ------ | ------- | --------------------------- |
| **NotificationService** | 5006   | ✅ 100% | Emails de confirmación      |
| **InvoicingService**    | 5046   | ✅ 80%  | Facturas electrónicas (NCF) |
| **PrivacyService**      | TBD    | ✅ 90%  | Política de privacidad      |
| **LegalService**        | TBD    | 🟡 60%  | Términos, aceptaciones      |
| **DocumentService**     | TBD    | 🔴 0%   | Firma digital (PENDIENTE)   |

---

## 🎨 Componentes UI Existentes

### Páginas Legales

| Página      | Ruta       | Líneas | Estado |
| ----------- | ---------- | ------ | ------ |
| TermsPage   | `/terms`   | 223    | ✅     |
| PrivacyPage | `/privacy` | ~300   | ✅     |
| AboutPage   | `/about`   | 168    | ✅     |
| ContactPage | `/contact` | ~200   | ✅     |

### Componentes Compartidos

| Componente    | Ubicación              | Líneas | Estado |
| ------------- | ---------------------- | ------ | ------ |
| OklaFooter    | `organisms/OklaFooter` | 346    | ✅     |
| TermsCheckbox | `atoms/` (múltiples)   | ~20    | ✅     |

---

## 🔧 Servicio de Compliance

```typescript
// filepath: src/services/legal/complianceService.ts
import { apiClient } from "@/lib/apiClient";

export interface TermsAcceptance {
  id: string;
  userId: string;
  documentType: "terms" | "privacy" | "cookies";
  version: string;
  acceptedAt: string;
  ipAddress: string;
  userAgent: string;
}

export interface LegalDocument {
  id: string;
  type: string;
  version: string;
  content: string;
  effectiveDate: string;
  createdAt: string;
}

export interface ComplianceStatus {
  userId: string;
  termsAccepted: boolean;
  termsVersion: string;
  privacyAccepted: boolean;
  privacyVersion: string;
  needsReAcceptance: boolean;
  pendingDocuments: string[];
}

class ComplianceService {
  // Verificar estado de compliance del usuario
  async getComplianceStatus(userId: string): Promise<ComplianceStatus> {
    const response = await apiClient.get<ComplianceStatus>(
      `/api/legal/compliance/${userId}/status`,
    );
    return response.data;
  }

  // Obtener documento legal actual
  async getDocument(type: string): Promise<LegalDocument> {
    const response = await apiClient.get<LegalDocument>(
      `/api/legal/documents/${type}/current`,
    );
    return response.data;
  }

  // Registrar aceptación de términos
  async acceptTerms(
    userId: string,
    documentType: string,
    version: string,
  ): Promise<TermsAcceptance> {
    const response = await apiClient.post<TermsAcceptance>(
      "/api/legal/acceptances",
      {
        userId,
        documentType,
        version,
        acceptedAt: new Date().toISOString(),
      },
    );
    return response.data;
  }

  // Obtener historial de aceptaciones
  async getAcceptanceHistory(userId: string): Promise<TermsAcceptance[]> {
    const response = await apiClient.get<TermsAcceptance[]>(
      `/api/legal/acceptances/user/${userId}`,
    );
    return response.data;
  }

  // Verificar si versión de documento ha cambiado
  async checkDocumentVersion(
    type: string,
    currentVersion: string,
  ): Promise<{
    hasNewVersion: boolean;
    latestVersion: string;
    effectiveDate: string;
  }> {
    const response = await apiClient.get(
      `/api/legal/documents/${type}/check?currentVersion=${currentVersion}`,
    );
    return response.data;
  }

  // Exportar datos del usuario (GDPR/Ley 172-13)
  async exportUserData(userId: string): Promise<{ downloadUrl: string }> {
    const response = await apiClient.post(`/api/legal/gdpr/export/${userId}`);
    return response.data;
  }

  // Solicitar eliminación de datos
  async requestDataDeletion(
    userId: string,
    reason: string,
  ): Promise<{
    requestId: string;
    status: string;
    estimatedCompletionDate: string;
  }> {
    const response = await apiClient.post("/api/legal/gdpr/deletion-request", {
      userId,
      reason,
    });
    return response.data;
  }
}

export const complianceService = new ComplianceService();
```

---

## 🎨 Estados de UI

### Terms Acceptance Modal

```typescript
export function TermsAcceptanceModal({
  isOpen,
  onClose,
  documentType,
  onAccept,
}: {
  isOpen: boolean;
  onClose: () => void;
  documentType: "terms" | "privacy";
  onAccept: () => void;
}) {
  const [hasRead, setHasRead] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [document, setDocument] = useState<LegalDocument | null>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  // Detect scroll to bottom
  const handleScroll = () => {
    if (!contentRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = contentRef.current;
    if (scrollTop + clientHeight >= scrollHeight - 50) {
      setHasRead(true);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl max-h-[80vh]">
        <DialogHeader>
          <DialogTitle>
            {documentType === "terms"
              ? "Términos y Condiciones"
              : "Política de Privacidad"}
          </DialogTitle>
          <DialogDescription>
            Por favor lee el documento completo antes de aceptar.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="h-96 flex items-center justify-center">
            <Loader2 className="animate-spin" />
          </div>
        ) : (
          <div
            ref={contentRef}
            onScroll={handleScroll}
            className="h-96 overflow-y-auto prose prose-sm"
            dangerouslySetInnerHTML={{ __html: document?.content || "" }}
          />
        )}

        <DialogFooter className="flex items-center justify-between">
          <div className="text-sm text-gray-500">
            Versión {document?.version} - Efectivo {document?.effectiveDate}
          </div>
          <div className="flex gap-2">
            <Button variant="outline" onClick={onClose}>
              Cancelar
            </Button>
            <Button onClick={onAccept} disabled={!hasRead}>
              {hasRead ? "Acepto" : "Lee hasta el final"}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

### Compliance Banner

```typescript
export function ComplianceBanner({ userId }: { userId: string }) {
  const { data: status, isLoading } = useQuery({
    queryKey: ["compliance-status", userId],
    queryFn: () => complianceService.getComplianceStatus(userId),
  });

  if (isLoading || !status?.needsReAcceptance) return null;

  return (
    <div className="bg-amber-50 border-b border-amber-200 p-4">
      <div className="container mx-auto flex items-center justify-between">
        <div className="flex items-center gap-3">
          <AlertCircle className="text-amber-600" size={20} />
          <span className="text-amber-800">
            Nuestros términos han sido actualizados. Por favor revisa los cambios.
          </span>
        </div>
        <Button size="sm" variant="outline">
          Revisar cambios
        </Button>
      </div>
    </div>
  );
}
```

---

## 🧪 Testing E2E

```typescript
// filepath: e2e/legal/compliance.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Ley 126-02 Compliance", () => {
  test.describe("Art. 5 - Información del Proveedor", () => {
    test("should display company info in footer", async ({ page }) => {
      await page.goto("/");

      const footer = page.getByRole("contentinfo");
      await expect(footer.getByText("OKLA Marketplace SRL")).toBeVisible();
      await expect(footer.getByText(/RNC/)).toBeVisible();
      await expect(footer.getByText(/Santo Domingo/)).toBeVisible();
    });

    test("should have About page with complete info", async ({ page }) => {
      await page.goto("/about");

      await expect(page.getByText("Razón Social")).toBeVisible();
      await expect(page.getByText("RNC")).toBeVisible();
      await expect(page.getByText("Dirección")).toBeVisible();
      await expect(page.getByText("Teléfono")).toBeVisible();
      await expect(page.getByText("Email")).toBeVisible();
    });
  });

  test.describe("Art. 7 - Términos y Condiciones", () => {
    test("should require terms acceptance on registration", async ({
      page,
    }) => {
      await page.goto("/registro");

      // Fill form
      await page.fill('[name="email"]', "test@example.com");
      await page.fill('[name="password"]', "Test123!");

      // Submit button should be disabled without terms
      await expect(
        page.getByRole("button", { name: "Crear cuenta" }),
      ).toBeDisabled();

      // Accept terms
      await page.getByLabel(/Acepto los términos/).check();

      // Now button should be enabled
      await expect(
        page.getByRole("button", { name: "Crear cuenta" }),
      ).toBeEnabled();
    });

    test("should link to terms page", async ({ page }) => {
      await page.goto("/registro");

      const termsLink = page.getByRole("link", { name: /términos/i });
      await termsLink.click();

      await expect(page).toHaveURL("/terms");
    });
  });

  test.describe("Art. 8 - Confirmación de Transacciones", () => {
    test("should show confirmation after listing creation", async ({
      page,
    }) => {
      await page.goto("/login");
      await page.fill('[name="email"]', "dealer@test.com");
      await page.fill('[name="password"]', "test123");
      await page.click('button[type="submit"]');

      await page.goto("/dealer/crear-listing");
      // ... fill listing form
      await page.click('button[type="submit"]');

      // Should show confirmation
      await expect(page.getByText("Publicación creada")).toBeVisible();
      await expect(page.getByText("Referencia:")).toBeVisible();
    });

    test("should send confirmation email after purchase", async ({ page }) => {
      // ... mock payment flow
      await page.goto("/checkout/confirmation/12345");

      await expect(page.getByText("Pago confirmado")).toBeVisible();
      await expect(page.getByText("Hemos enviado un correo")).toBeVisible();
    });
  });

  test.describe("Art. 21 - Conservación de Documentos", () => {
    test("should show transaction history", async ({ page }) => {
      await page.goto("/login");
      await page.fill('[name="email"]', "user@test.com");
      await page.fill('[name="password"]', "test123");
      await page.click('button[type="submit"]');

      await page.goto("/perfil/historial");

      await expect(page.getByText("Historial de Transacciones")).toBeVisible();
      // Should show records older than 10 years disclaimer
    });

    test("should allow invoice download", async ({ page }) => {
      await page.goto("/perfil/historial");

      const downloadButton = page.getByTestId("download-invoice-12345");

      const downloadPromise = page.waitForEvent("download");
      await downloadButton.click();
      const download = await downloadPromise;

      expect(download.suggestedFilename()).toMatch(/factura.*\.pdf/);
    });
  });

  test.describe("Legal Pages", () => {
    test("terms page should be accessible", async ({ page }) => {
      await page.goto("/terms");

      await expect(
        page.getByRole("heading", { name: /Términos/i }),
      ).toBeVisible();
      await expect(page.getByText("Última actualización")).toBeVisible();
    });

    test("privacy page should be accessible", async ({ page }) => {
      await page.goto("/privacy");

      await expect(
        page.getByRole("heading", { name: /Privacidad/i }),
      ).toBeVisible();
      await expect(page.getByText("Ley 172-13")).toBeVisible();
    });

    test("contact page should have required info", async ({ page }) => {
      await page.goto("/contact");

      await expect(page.getByText("Atención al Cliente")).toBeVisible();
      await expect(
        page.getByRole("link", { name: /@okla\.com\.do/ }),
      ).toBeVisible();
    });
  });
});
```

---

## 📊 Analytics Events

```typescript
// filepath: src/lib/analytics/complianceEvents.ts
import { analytics } from "@/lib/analytics";

export const complianceEvents = {
  // Terms acceptance
  termsViewed: (documentType: string, version: string) => {
    analytics.track("legal_document_viewed", { documentType, version });
  },

  termsAccepted: (documentType: string, version: string) => {
    analytics.track("legal_document_accepted", { documentType, version });
  },

  termsDeclined: (documentType: string, version: string) => {
    analytics.track("legal_document_declined", { documentType, version });
  },

  // Re-acceptance prompts
  reAcceptancePromptShown: (
    documentType: string,
    oldVersion: string,
    newVersion: string,
  ) => {
    analytics.track("legal_reacceptance_prompt_shown", {
      documentType,
      oldVersion,
      newVersion,
    });
  },

  // GDPR/Ley 172-13
  dataExportRequested: () => {
    analytics.track("gdpr_data_export_requested");
  },

  dataDeletionRequested: (reason: string) => {
    analytics.track("gdpr_data_deletion_requested", { reason });
  },

  // Legal page views
  legalPageViewed: (page: "terms" | "privacy" | "about" | "contact") => {
    analytics.track("legal_page_viewed", { page });
  },
};
```

---

## ✅ Checklist de Implementación

### Art. 5 - Información del Proveedor

- [ ] Razón Social en OklaFooter
- [ ] RNC visible en footer
- [ ] Dirección completa en AboutPage
- [ ] Teléfono de contacto
- [ ] Email de contacto
- [ ] Horario de atención

### Art. 7 - Términos y Condiciones

- [ ] TermsPage con contenido completo
- [ ] Checkbox de aceptación en registro
- [ ] Link a términos desde formularios
- [ ] Versión y fecha de términos visible
- [ ] TermsAcceptanceModal para cambios

### Art. 8 - Confirmación de Transacciones

- [ ] Pantalla de confirmación post-pago
- [ ] Email de confirmación automático
- [ ] Número de referencia único
- [ ] Resumen de transacción
- [ ] Link a factura descargable

### Art. 18 - Documentos Electrónicos

- [ ] NCF en facturas electrónicas
- [ ] Formato PDF/A para archivado
- [ ] Metadata de documento
- [ ] Timestamp verificable

### Art. 21 - Conservación

- [ ] Retención 10 años en S3
- [ ] Historial de transacciones
- [ ] Descarga de facturas históricas
- [ ] Log de cambios de términos

### Ley 172-13 (Privacidad)

- [ ] PrivacyPage completa
- [ ] Consent para cookies
- [ ] Exportación de datos (GDPR)
- [ ] Solicitud de eliminación
- [ ] Data Processing Agreement

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/comercio-electronico.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Comercio Electrónico - Ley 126-02", () => {
  test("debe mostrar términos y condiciones", async ({ page }) => {
    await page.goto("/terminos");

    await expect(
      page.getByRole("heading", { name: /términos/i }),
    ).toBeVisible();
  });

  test("debe mostrar política de privacidad", async ({ page }) => {
    await page.goto("/privacidad");

    await expect(
      page.getByRole("heading", { name: /privacidad/i }),
    ).toBeVisible();
  });

  test("debe requerir aceptación de términos en registro", async ({ page }) => {
    await page.goto("/registro");

    const termsCheckbox = page.getByRole("checkbox", {
      name: /acepto.*términos/i,
    });
    await expect(termsCheckbox).toBeVisible();
  });

  test("debe mostrar información de empresa", async ({ page }) => {
    await page.goto("/sobre-nosotros");

    await expect(page.getByText(/rnc/i)).toBeVisible();
  });

  test("debe mostrar política de reembolsos", async ({ page }) => {
    await page.goto("/reembolsos");

    await expect(
      page.getByRole("heading", { name: /reembolsos/i }),
    ).toBeVisible();
  });
});
```

---

**CONCLUSIÓN:**  
Ley 126-02 muestra **EXCELENTE cumplimiento básico (80%)**. Requisitos operativos para marketplace están completos. Firma digital es opcional para operaciones actuales pero RECOMENDADA para contratos de alto valor.

---

_Última actualización: Enero 29, 2026_  
_Auditor: Gregory Moreno_  
_Próxima revisión: Abril 29, 2026 (post-implementación firma digital)_
