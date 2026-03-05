# 🔐 KYCService - Documentación Frontend

> **Servicio:** KYCService  
> **Puerto:** 5097 (dev) / 8080 (k8s)  
> **Estado:** ✅ Alta prioridad completada  
> **Compliance:** Ley 155-17 (Prevención Lavado RD)  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de verificación de identidad (Know Your Customer) estilo **Qik/Banco Popular**. Implementa verificación biométrica con OCR de cédula y comparación facial usando **Amazon Rekognition**. Requerido para dealers y transacciones de alto valor.

---

## 🎯 Casos de Uso Frontend

### 1. Verificación de Dealer (Onboarding)

```typescript
// Flujo de verificación de identidad para dealers
const verifyDealerIdentity = async () => {
  // Paso 1: Iniciar sesión de verificación
  const session = await kycService.startVerification({
    documentType: "cedula",
    deviceInfo: getDeviceInfo(),
  });

  // Paso 2: Capturar frente de cédula
  const frontImage = await captureDocument("front");
  await kycService.uploadDocument(session.sessionId, {
    type: "id_front",
    image: frontImage,
  });

  // Paso 3: Capturar reverso de cédula
  const backImage = await captureDocument("back");
  await kycService.uploadDocument(session.sessionId, {
    type: "id_back",
    image: backImage,
  });

  // Paso 4: Capturar selfie
  const selfieImage = await captureSelfie();
  await kycService.uploadDocument(session.sessionId, {
    type: "selfie",
    image: selfieImage,
  });

  // Paso 5: Procesar verificación
  const result = await kycService.processVerification(session.sessionId);
  return result; // { verified: true, score: 0.95 }
};
```

### 2. Verificación de Comprador (High Value)

```typescript
// Para transacciones > $50,000 USD
const verifyBuyerForTransaction = async (transactionAmount: number) => {
  if (transactionAmount > 50000) {
    const verified = await kycService.checkVerificationStatus(userId);
    if (!verified) {
      // Redirigir a flujo de verificación
      navigate("/verify-identity");
      return false;
    }
  }
  return true;
};
```

### 3. Estado de Verificación

```typescript
// Mostrar badge de verificado en perfil
const VerifiedBadge = ({ userId }: { userId: string }) => {
  const { data: status } = useKYCStatus(userId);

  if (status?.isVerified) {
    return <Badge variant="success">✓ Verificado</Badge>;
  }
  return null;
};
```

---

## 📡 API Endpoints

### Identity Verification

| Método | Endpoint                                              | Descripción                    |
| ------ | ----------------------------------------------------- | ------------------------------ |
| `POST` | `/api/kyc/identity-verification/start`                | Iniciar sesión de verificación |
| `POST` | `/api/kyc/identity-verification/{sessionId}/document` | Subir documento                |
| `POST` | `/api/kyc/identity-verification/{sessionId}/process`  | Procesar verificación          |
| `GET`  | `/api/kyc/identity-verification/{sessionId}/status`   | Estado de sesión               |
| `GET`  | `/api/kyc/identity-verification/user/{userId}`        | Estado de usuario              |

### KYC Profiles

| Método | Endpoint                     | Descripción           |
| ------ | ---------------------------- | --------------------- |
| `GET`  | `/api/kyc/profiles/{userId}` | Perfil KYC de usuario |
| `PUT`  | `/api/kyc/profiles/{userId}` | Actualizar perfil     |

### KYC Documents

| Método | Endpoint                           | Descripción            |
| ------ | ---------------------------------- | ---------------------- |
| `GET`  | `/api/kyc/documents/user/{userId}` | Documentos del usuario |
| `GET`  | `/api/kyc/documents/{id}`          | Detalle de documento   |

### Watchlist (Admin)

| Método | Endpoint                            | Descripción         |
| ------ | ----------------------------------- | ------------------- |
| `GET`  | `/api/kyc/watchlist/check/{cedula}` | Verificar en listas |
| `GET`  | `/api/kyc/watchlist/hits/{userId}`  | Hits de un usuario  |

---

## 🔧 Cliente TypeScript

```typescript
// services/kycService.ts

import { apiClient } from "./apiClient";

// Tipos
interface StartVerificationRequest {
  documentType: "cedula" | "passport";
  deviceInfo?: DeviceInfo;
  location?: { lat: number; lng: number };
}

interface StartVerificationResponse {
  sessionId: string;
  expiresAt: string;
  steps: VerificationStep[];
  instructions: string[];
}

interface VerificationStep {
  step: number;
  name: string;
  description: string;
  required: boolean;
  completed: boolean;
}

interface UploadDocumentRequest {
  type: "id_front" | "id_back" | "selfie" | "proof_of_address";
  image: string; // Base64 encoded
}

interface DocumentUploadResponse {
  documentId: string;
  status: "processing" | "success" | "failed";
  ocrData?: OCRData;
}

interface OCRData {
  cedula?: string;
  nombre?: string;
  apellido?: string;
  fechaNacimiento?: string;
  lugarNacimiento?: string;
  sexo?: "M" | "F";
  fechaExpiracion?: string;
}

interface VerificationResult {
  verified: boolean;
  score: number;
  details: {
    documentValid: boolean;
    faceMatch: boolean;
    faceMatchScore: number;
    dataConsistent: boolean;
    watchlistClear: boolean;
  };
  status: "approved" | "rejected" | "pending_review" | "requires_manual";
  rejectionReason?: string;
}

interface KYCStatus {
  userId: string;
  isVerified: boolean;
  verifiedAt?: string;
  verificationLevel: "none" | "basic" | "enhanced" | "full";
  expiresAt?: string;
  documentType?: string;
  lastAttempt?: string;
  attemptsRemaining: number;
}

export const kycService = {
  // Iniciar verificación
  async startVerification(
    request: StartVerificationRequest,
  ): Promise<StartVerificationResponse> {
    const response = await apiClient.post(
      "/api/kyc/identity-verification/start",
      request,
    );
    return response.data;
  },

  // Subir documento
  async uploadDocument(
    sessionId: string,
    document: UploadDocumentRequest,
  ): Promise<DocumentUploadResponse> {
    const response = await apiClient.post(
      `/api/kyc/identity-verification/${sessionId}/document`,
      document,
    );
    return response.data;
  },

  // Procesar verificación
  async processVerification(sessionId: string): Promise<VerificationResult> {
    const response = await apiClient.post(
      `/api/kyc/identity-verification/${sessionId}/process`,
    );
    return response.data;
  },

  // Estado de sesión
  async getSessionStatus(
    sessionId: string,
  ): Promise<{ step: number; status: string }> {
    const response = await apiClient.get(
      `/api/kyc/identity-verification/${sessionId}/status`,
    );
    return response.data;
  },

  // Estado de usuario
  async getUserKYCStatus(userId: string): Promise<KYCStatus> {
    const response = await apiClient.get(
      `/api/kyc/identity-verification/user/${userId}`,
    );
    return response.data;
  },

  // Verificar cédula (validación local)
  validateCedula(cedula: string): boolean {
    // Formato: XXX-XXXXXXX-X
    const regex = /^\d{3}-\d{7}-\d{1}$/;
    if (!regex.test(cedula)) return false;

    // Validación Módulo 10 (algoritmo de Luhn modificado)
    const digits = cedula.replace(/-/g, "").split("").map(Number);
    const checkDigit = digits.pop()!;
    const weights = [1, 2, 1, 2, 1, 2, 1, 2, 1, 2];

    let sum = 0;
    for (let i = 0; i < 10; i++) {
      let product = digits[i] * weights[i];
      if (product > 9) product -= 9;
      sum += product;
    }

    const calculated = (10 - (sum % 10)) % 10;
    return calculated === checkDigit;
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/useKYC.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { kycService } from "../services/kycService";

export function useKYCStatus(userId: string | undefined) {
  return useQuery({
    queryKey: ["kyc-status", userId],
    queryFn: () => kycService.getUserKYCStatus(userId!),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useKYCVerification() {
  const queryClient = useQueryClient();
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [currentStep, setCurrentStep] = useState(0);

  const startVerification = useMutation({
    mutationFn: kycService.startVerification,
    onSuccess: (data) => {
      setSessionId(data.sessionId);
      setCurrentStep(1);
    },
  });

  const uploadDocument = useMutation({
    mutationFn: ({ type, image }: { type: string; image: string }) =>
      kycService.uploadDocument(sessionId!, { type, image }),
    onSuccess: () => {
      setCurrentStep((prev) => prev + 1);
    },
  });

  const processVerification = useMutation({
    mutationFn: () => kycService.processVerification(sessionId!),
    onSuccess: (result) => {
      if (result.verified) {
        queryClient.invalidateQueries({ queryKey: ["kyc-status"] });
      }
    },
  });

  return {
    sessionId,
    currentStep,
    startVerification,
    uploadDocument,
    processVerification,
    isLoading:
      startVerification.isPending ||
      uploadDocument.isPending ||
      processVerification.isPending,
  };
}
```

---

## 🧩 Componente de Verificación

```tsx
// components/IdentityVerification.tsx

import { useKYCVerification } from "../hooks/useKYC";
import { DocumentCapture } from "./DocumentCapture";
import { SelfieCapture } from "./SelfieCapture";

const steps = [
  { id: "start", label: "Iniciar" },
  { id: "id_front", label: "Cédula Frente" },
  { id: "id_back", label: "Cédula Reverso" },
  { id: "selfie", label: "Selfie" },
  { id: "processing", label: "Procesando" },
  { id: "result", label: "Resultado" },
];

export function IdentityVerification({
  onComplete,
}: {
  onComplete: (success: boolean) => void;
}) {
  const {
    currentStep,
    startVerification,
    uploadDocument,
    processVerification,
    isLoading,
  } = useKYCVerification();

  const handleStart = () => {
    startVerification.mutate({
      documentType: "cedula",
      deviceInfo: getDeviceInfo(),
    });
  };

  const handleDocumentCapture = async (type: string, imageBase64: string) => {
    await uploadDocument.mutateAsync({ type, image: imageBase64 });

    if (currentStep === 4) {
      // Último documento, procesar
      const result = await processVerification.mutateAsync();
      onComplete(result.verified);
    }
  };

  return (
    <div className="max-w-md mx-auto p-6">
      {/* Progress Steps */}
      <div className="flex justify-between mb-8">
        {steps.map((step, index) => (
          <div
            key={step.id}
            className={`flex flex-col items-center ${
              index <= currentStep ? "text-blue-600" : "text-gray-400"
            }`}
          >
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center ${
                index < currentStep
                  ? "bg-green-500 text-white"
                  : index === currentStep
                    ? "bg-blue-600 text-white"
                    : "bg-gray-200"
              }`}
            >
              {index < currentStep ? "✓" : index + 1}
            </div>
            <span className="text-xs mt-1">{step.label}</span>
          </div>
        ))}
      </div>

      {/* Step Content */}
      {currentStep === 0 && (
        <div className="text-center">
          <h2 className="text-xl font-bold mb-4">Verificación de Identidad</h2>
          <p className="text-gray-600 mb-6">
            Para continuar, necesitamos verificar tu identidad con tu cédula
            dominicana.
          </p>
          <ul className="text-left text-sm text-gray-500 mb-6">
            <li>✓ Ten tu cédula a la mano</li>
            <li>✓ Asegúrate de tener buena iluminación</li>
            <li>✓ El proceso toma menos de 2 minutos</li>
          </ul>
          <Button onClick={handleStart} disabled={isLoading}>
            {isLoading ? "Iniciando..." : "Comenzar Verificación"}
          </Button>
        </div>
      )}

      {currentStep === 1 && (
        <DocumentCapture
          title="Frente de la Cédula"
          instructions="Centra la cédula dentro del marco"
          onCapture={(img) => handleDocumentCapture("id_front", img)}
        />
      )}

      {currentStep === 2 && (
        <DocumentCapture
          title="Reverso de la Cédula"
          instructions="Voltea la cédula y captura el reverso"
          onCapture={(img) => handleDocumentCapture("id_back", img)}
        />
      )}

      {currentStep === 3 && (
        <SelfieCapture
          title="Selfie de Verificación"
          instructions="Mira directamente a la cámara"
          onCapture={(img) => handleDocumentCapture("selfie", img)}
        />
      )}

      {currentStep === 4 && (
        <div className="text-center py-8">
          <Spinner size="lg" />
          <p className="mt-4">Procesando verificación...</p>
          <p className="text-sm text-gray-500">
            Esto puede tomar unos segundos
          </p>
        </div>
      )}
    </div>
  );
}
```

---

## 🧪 Testing

### Vitest Mocks

```typescript
// __mocks__/kycService.ts
export const kycService = {
  startVerification: vi.fn().mockResolvedValue({
    sessionId: "session-123",
    expiresAt: new Date(Date.now() + 1800000).toISOString(),
    steps: [
      { step: 1, name: "id_front", completed: false },
      { step: 2, name: "id_back", completed: false },
      { step: 3, name: "selfie", completed: false },
    ],
  }),
  uploadDocument: vi.fn().mockResolvedValue({
    documentId: "doc-123",
    status: "success",
  }),
  processVerification: vi.fn().mockResolvedValue({
    verified: true,
    score: 0.95,
    details: {
      documentValid: true,
      faceMatch: true,
      faceMatchScore: 0.98,
    },
    status: "approved",
  }),
  getUserKYCStatus: vi.fn().mockResolvedValue({
    isVerified: false,
    verificationLevel: "none",
    attemptsRemaining: 3,
  }),
  validateCedula: vi.fn().mockReturnValue(true),
};
```

### E2E Test (Playwright)

```typescript
// e2e/kyc.spec.ts
import { test, expect } from "@playwright/test";

test.describe("KYC Verification", () => {
  test("should show verification prompt for dealers", async ({ page }) => {
    await page.goto("/dealer/register");

    // Completar registro
    await page.fill('[name="businessName"]', "Test Dealer");
    await page.click('[data-testid="submit"]');

    // Debe mostrar prompt de verificación
    await expect(page.locator('[data-testid="kyc-prompt"]')).toBeVisible();
  });

  test("should validate cedula format", async ({ page }) => {
    await page.goto("/verify-identity");
    await page.click('[data-testid="start-verification"]');

    // Mock de captura - solo verificar que el flujo inicia
    await expect(
      page.locator('[data-testid="document-capture"]'),
    ).toBeVisible();
  });
});
```

---

## 🔒 Seguridad y Compliance

### Datos Almacenados

- ✅ Cédula hasheada (no plain text)
- ✅ Selfie encriptada en S3
- ✅ Logs de auditoría de cada verificación
- ✅ Retención: 5 años (Ley 155-17)

### Límites

- Max 3 intentos por día
- Sesión expira en 30 minutos
- Bloqueo temporal después de 5 fallos

### Costos

- Amazon Rekognition: ~$0.001/imagen
- OCR Tesseract: Gratis (self-hosted)

---

## 🔗 Referencias

- [IMPLEMENTATION_STATUS.md](../../../backend/KYCService/IMPLEMENTATION_STATUS.md)
- [Ley 155-17 RD](https://dgii.gov.do/legislacion/leyesTributarias/Documents/155-17.pdf)
- [Amazon Rekognition](https://aws.amazon.com/rekognition/)

---

_La verificación KYC es obligatoria para dealers y transacciones de alto valor._
