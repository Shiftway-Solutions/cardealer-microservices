---
title: "Test Drives - Sistema Completo de Agendamiento"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["BillingService", "NotificationService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 🚗 Test Drives - Sistema Completo de Agendamiento

> **Módulo:** 05-AGENDAMIENTO  
> **Procesos Backend:** TESTDRIVE-001, TESTDRIVE-002  
> **Versión:** 1.0  
> **Fecha:** Enero 29, 2026  
> **Estado:** ✅ Backend 100% | 🟡 UI 90%

---

## 📋 ÍNDICE

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [API Endpoints](#api-endpoints)
4. [Componentes React](#componentes-react)
5. [Hooks](#hooks)
6. [Servicios TypeScript](#servicios-typescript)
7. [Tipos e Interfaces](#tipos-e-interfaces)
8. [Proceso TESTDRIVE-001: Agendar](#proceso-testdrive-001-agendar)
9. [Proceso TESTDRIVE-002: Ejecutar](#proceso-testdrive-002-ejecutar)
10. [Flujos de Usuario](#flujos-de-usuario)
11. [Validación y Testing](#validación-y-testing)
12. [Próximos Pasos](#próximos-pasos)

---

## 1️⃣ RESUMEN EJECUTIVO

El sistema de Test Drives permite a los compradores agendar pruebas de manejo y a los dealers gestionar el proceso completo desde la reserva hasta el seguimiento post-prueba.

### 🎯 Objetivos del Sistema

| Objetivo                     | Descripción                                     | Métrica de Éxito                   |
| ---------------------------- | ----------------------------------------------- | ---------------------------------- |
| **Conversión**               | Aumentar tasa de ventas mediante test drives    | >25% conversión TD → Oferta        |
| **Confianza**                | Permitir al comprador probar antes de comprar   | >4.5/5 satisfacción                |
| **Eficiencia Dealer**        | Automatizar agendamiento y seguimiento          | <2 min tiempo agendamiento         |
| **Documentación Legal**      | Capturar firmas y condición del vehículo        | 100% waivers firmados              |
| **Seguimiento Automatizado** | Contacto post-prueba sin intervención manual    | 80% leads seguidos automáticamente |
| **Trazabilidad**             | Registro completo del proceso (odómetro, fotos) | 100% eventos registrados           |

### 📊 Estadísticas Clave

```typescript
// Métricas esperadas del sistema
const testDriveMetrics = {
  avgSchedulingTime: "2 minutos", // Desde click hasta confirmación
  avgTestDriveDuration: "30 minutos", // Duración típica de prueba
  conversionRate: {
    testDriveToOffer: "25%", // TD → Oferta
    testDriveToPurchase: "15%", // TD → Compra
    noShowRate: "8%", // No se presentan
  },
  satisfaction: {
    overallRating: 4.7, // De 5
    wouldRecommend: "92%",
  },
  automation: {
    remindersSent: "100%", // 24h + 2h antes
    followUpsSent: "100%", // 1h después
    waiverSignRate: "100%", // Obligatorio
  },
};
```

---

## 2️⃣ ARQUITECTURA DEL SISTEMA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Test Drive System Architecture                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  USUARIO (Comprador)                      DEALER (Vendedor)                 │
│  ┌────────────────────┐                  ┌────────────────────┐            │
│  │ Vehicle Detail Pg  │                  │ Dealer Dashboard   │            │
│  │ ┌────────────────┐ │                  │ ┌────────────────┐ │            │
│  │ │ "Agendar Test  │ │                  │ │ "Mis Citas"    │ │            │
│  │ │  Drive" Button │ │                  │ │ Calendar View  │ │            │
│  │ └────────────────┘ │                  │ └────────────────┘ │            │
│  └─────────┬──────────┘                  └─────────┬──────────┘            │
│            │                                       │                        │
│            │ GET /availability                     │ GET /dealer/bookings   │
│            │ POST /testdrives                      │ POST /checkin/checkout │
│            │ POST /waiver/sign                     │ POST /outcome          │
│            │                                       │                        │
│            ▼                                       ▼                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         API GATEWAY                                 │   │
│  │  Ocelot (Port 8080) - Routing & Rate Limiting                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                       │
│                                    ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    APPOINTMENTSERVICE (Port 5009)                   │   │
│  │  ┌───────────────────────────────────────────────────────────────┐  │   │
│  │  │ Controllers                                                    │  │   │
│  │  │  • TestDrivesController                                       │  │   │
│  │  │    - GET /availability/{vehicleId}  (Slots disponibles)       │  │   │
│  │  │    - POST /testdrives               (Crear reserva)           │  │   │
│  │  │    - GET /testdrives/{id}/waiver    (Obtener formulario)      │  │   │
│  │  │    - POST /testdrives/{id}/waiver/sign (Firmar)               │  │   │
│  │  │    - POST /testdrives/{id}/checkin  (Check-in dealer)         │  │   │
│  │  │    - POST /testdrives/{id}/checkout (Check-out + fotos)       │  │   │
│  │  │    - POST /testdrives/{id}/feedback (Feedback usuario)        │  │   │
│  │  │    - GET /dealer/{dealerId}/bookings (Lista dealer)           │  │   │
│  │  └───────────────────────────────────────────────────────────────┘  │   │
│  │  ┌───────────────────────────────────────────────────────────────┐  │   │
│  │  │ Application Layer (CQRS + MediatR)                            │  │   │
│  │  │  Commands:                                                    │  │   │
│  │  │   • CreateTestDriveCommand     (TESTDRIVE-001)                │  │   │
│  │  │   • CheckInTestDriveCommand    (TESTDRIVE-002)                │  │   │
│  │  │   • CheckOutTestDriveCommand   (TESTDRIVE-002)                │  │   │
│  │  │   • SignWaiverCommand                                         │  │   │
│  │  │   • SubmitFeedbackCommand                                     │  │   │
│  │  │  Queries:                                                     │  │   │
│  │  │   • GetAvailabilitySlotsQuery  (TESTDRIVE-001)                │  │   │
│  │  │   • GetTestDriveByIdQuery                                     │  │   │
│  │  │   • GetDealerBookingsQuery                                    │  │   │
│  │  │   • GetWaiverQuery                                            │  │   │
│  │  └───────────────────────────────────────────────────────────────┘  │   │
│  │  ┌───────────────────────────────────────────────────────────────┐  │   │
│  │  │ Domain Entities                                               │  │   │
│  │  │  • TestDrive (extends Appointment)                            │  │   │
│  │  │    - Vehicle info (VIN, title, image)                         │  │   │
│  │  │    - Driver info (cédula, licencia, foto)                     │  │   │
│  │  │    - Waiver (signature, document URL)                         │  │   │
│  │  │    - Check-in/out (odometer, photos)                          │  │   │
│  │  │    - Feedback (ratings, comments)                             │  │   │
│  │  │    - Outcome (NoDecision, Offer, Purchase, etc.)              │  │   │
│  │  │  • DealerTestDriveSettings                                    │  │   │
│  │  │    - Weekly schedule (slots disponibles)                      │  │   │
│  │  │    - Config (duración, buffer, requisitos)                    │  │   │
│  │  │    - Approved routes                                          │  │   │
│  │  │  • TestDriveFeedback                                          │  │   │
│  │  │    - Ratings (overall, comfort, performance, condition)       │  │   │
│  │  │    - Questions (meets expectations, interested in buying)     │  │   │
│  │  │    - Comments (likes, dislikes, additional)                   │  │   │
│  │  └───────────────────────────────────────────────────────────────┘  │   │
│  │  ┌───────────────────────────────────────────────────────────────┐  │   │
│  │  │ Infrastructure                                                │  │   │
│  │  │  • AppointmentDbContext                                       │  │   │
│  │  │  • TestDriveRepository (CRUD + availability calc)             │  │   │
│  │  │  • WaiverDocumentGenerator (PDF con firma)                    │  │   │
│  │  │  • SlotAvailabilityCalculator (algoritmo de slots)            │  │   │
│  │  └───────────────────────────────────────────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                       │
│                  ┌─────────────────┼─────────────────┐                     │
│                  ▼                 ▼                 ▼                     │
│          ┌────────────┐    ┌────────────┐    ┌────────────┐              │
│          │ PostgreSQL │    │   Redis    │    │  RabbitMQ  │              │
│          │ (Bookings, │    │  (Avail.   │    │ (Reminder  │              │
│          │  Waivers,  │    │   Cache,   │    │  Events,   │              │
│          │ Feedback)  │    │ Locking)   │    │ Follow-up) │              │
│          └────────────┘    └────────────┘    └────────────┘              │
│                                    │                                       │
│          ┌─────────────────────────┼─────────────────────────┐            │
│          ▼                         ▼                         ▼            │
│  ┌────────────┐           ┌────────────┐           ┌────────────┐        │
│  │ MediaSvc   │           │ NotifSvc   │           │ BillingSvc │        │
│  │ (License,  │           │ (Reminder  │           │ (Deposit   │        │
│  │ Pre/Post   │           │ 24h+2h,    │           │ Hold/      │        │
│  │ Photos)    │           │ Follow-up) │           │ Refund)    │        │
│  └────────────┘           └────────────┘           └────────────┘        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 🔁 Flujo de Datos Clave

```typescript
// Pipeline completo del test drive
const testDrivePipeline = {
  step1_availability: {
    input: "vehicleId, date range",
    process: "Calculate slots from dealer config + existing bookings",
    output: "Array of AvailabilitySlot { date, time, available }",
  },
  step2_booking: {
    input: "TestDriveCreateDto (vehicle, user, slot, driver info)",
    process: "Validate slot, create TestDrive, send confirmation",
    output: "TestDrive entity with Status=Pending",
  },
  step3_reminders: {
    input: "Scheduler cron jobs",
    process: "24h before: email/SMS; 2h before: SMS/push",
    output: "Notifications sent",
  },
  step4_waiver: {
    input: "TestDrive ID",
    process: "Generate PDF waiver, capture e-signature",
    output: "Waiver PDF with signature, WaiverSigned=true",
  },
  step5_checkin: {
    input: "Odometer reading, pre-test photos",
    process: "Mark CheckedIn=true, upload photos to S3",
    output: "TestDrive with CheckInTime, OdometerAtCheckIn, PreTestPhotos[]",
  },
  step6_execution: {
    input: "Physical test drive (30-60 min)",
    process: "Vehicle driven on approved route",
    output: "Experience logged",
  },
  step7_checkout: {
    input: "Odometer reading, post-test photos",
    process: "Mark CheckedOut=true, calculate km driven, upload photos",
    output: "TestDrive with CheckOutTime, KmDriven, PostTestPhotos[]",
  },
  step8_feedback: {
    input: "Ratings (1-5), questions (yes/no), comments",
    process: "Create TestDriveFeedback entity, calculate lead score",
    output: "Feedback submitted, lead hot/warm/cold classified",
  },
  step9_followup: {
    input: "Feedback + Outcome",
    process: "If interested: assign to sales rep, schedule follow-up call",
    output: "CRM task created, automated follow-up email sent",
  },
};
```

---

## 3️⃣ API ENDPOINTS

### Backend: AppointmentService (Port 5009)

| Método | Endpoint                                   | Descripción                           | Auth | Proceso       |
| ------ | ------------------------------------------ | ------------------------------------- | ---- | ------------- |
| `GET`  | `/api/testdrives/availability/{vehicleId}` | Slots disponibles para un vehículo    | ❌   | TESTDRIVE-001 |
| `POST` | `/api/testdrives`                          | Agendar test drive                    | ✅   | TESTDRIVE-001 |
| `GET`  | `/api/testdrives/{id}`                     | Obtener detalle de test drive         | ✅   | -             |
| `GET`  | `/api/testdrives/{id}/waiver`              | Obtener formulario de responsabilidad | ✅   | TESTDRIVE-002 |
| `POST` | `/api/testdrives/{id}/waiver/sign`         | Firmar formulario                     | ✅   | TESTDRIVE-002 |
| `POST` | `/api/testdrives/{id}/checkin`             | Check-in al llegar al dealer          | ✅   | TESTDRIVE-002 |
| `POST` | `/api/testdrives/{id}/checkout`            | Check-out al terminar                 | ✅   | TESTDRIVE-002 |
| `POST` | `/api/testdrives/{id}/feedback`            | Feedback post-test                    | ✅   | TESTDRIVE-002 |
| `GET`  | `/api/testdrives/dealer/{dealerId}`        | Lista de bookings del dealer          | ✅   | -             |
| `PUT`  | `/api/testdrives/{id}/cancel`              | Cancelar test drive                   | ✅   | -             |
| `POST` | `/api/testdrives/{id}/reschedule`          | Re-agendar test drive                 | ✅   | -             |

### Ejemplos de Request/Response

#### GET /api/testdrives/availability/{vehicleId}

```http
GET https://api.okla.com.do/api/testdrives/availability/veh-67890?from=2026-01-29&to=2026-02-12
Authorization: Bearer {jwt_token}
```

**Response 200 OK:**

```json
{
  "vehicleId": "veh-67890",
  "dealerId": "dealer-001",
  "dealerName": "AutoMax RD",
  "dealerAddress": "Av. Winston Churchill #1234, Santo Domingo",
  "config": {
    "testDriveDurationMinutes": 30,
    "bufferBetweenMinutes": 15,
    "maxAdvanceBookingDays": 14,
    "minAdvanceBookingHours": 2,
    "requireLicensePhoto": true,
    "requireWaiverSignature": true,
    "requireDeposit": false
  },
  "availability": [
    {
      "date": "2026-01-29",
      "dayOfWeek": "Wednesday",
      "slots": [
        { "time": "09:00", "available": true },
        { "time": "09:45", "available": true },
        { "time": "10:30", "available": false, "reason": "Already booked" },
        { "time": "11:15", "available": true },
        { "time": "14:00", "available": true },
        { "time": "14:45", "available": true },
        { "time": "15:30", "available": true },
        { "time": "16:15", "available": true }
      ]
    },
    {
      "date": "2026-01-30",
      "dayOfWeek": "Thursday",
      "slots": [
        { "time": "09:00", "available": true },
        { "time": "09:45", "available": true }
        // ... más slots
      ]
    }
    // ... más fechas hasta 2026-02-12
  ]
}
```

#### POST /api/testdrives

```http
POST https://api.okla.com.do/api/testdrives
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "vehicleId": "veh-67890",
  "scheduledDate": "2026-01-30",
  "scheduledTime": "09:00",
  "driverName": "Juan Pérez García",
  "driverCedula": "001-1234567-8",
  "driverLicenseNumber": "RD-SD-123456-12",
  "driverLicenseExpiry": "2028-06-15",
  "driverLicensePhotoUrl": "https://okla-media.s3.amazonaws.com/licenses/user-001-license.jpg",
  "notes": "Primera vez probando un Toyota"
}
```

**Response 201 Created:**

```json
{
  "id": "td-12345",
  "vehicleId": "veh-67890",
  "vehicleTitle": "Toyota Corolla 2023 SE",
  "vehicleImage": "https://okla-media.s3.amazonaws.com/vehicles/veh-67890/main.jpg",
  "vehicleVIN": "1HGBH41JXMN109186",
  "userId": "user-001",
  "dealerId": "dealer-001",
  "dealerName": "AutoMax RD",
  "scheduledDate": "2026-01-30T09:00:00Z",
  "duration": 30,
  "status": "Pending",
  "driverName": "Juan Pérez García",
  "driverCedula": "001-1234567-8",
  "driverLicenseNumber": "RD-SD-123456-12",
  "driverLicenseExpiry": "2028-06-15",
  "waiverSigned": false,
  "checkedIn": false,
  "checkedOut": false,
  "createdAt": "2026-01-29T15:30:00Z",
  "confirmationCode": "OKLA-TD-12345",
  "remindersSent": {
    "twentyFourHours": false,
    "twoHours": false
  }
}
```

#### POST /api/testdrives/{id}/waiver/sign

```http
POST https://api.okla.com.do/api/testdrives/td-12345/waiver/sign
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "signatureDataUrl": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "agreedToTerms": true,
  "signedAt": "2026-01-30T08:55:00Z"
}
```

**Response 200 OK:**

```json
{
  "success": true,
  "waiverSigned": true,
  "waiverSignedAt": "2026-01-30T08:55:00Z",
  "waiverDocumentUrl": "https://okla-docs.s3.amazonaws.com/waivers/td-12345-signed.pdf",
  "signatureUrl": "https://okla-media.s3.amazonaws.com/signatures/td-12345-signature.png"
}
```

#### POST /api/testdrives/{id}/checkin

```http
POST https://api.okla.com.do/api/testdrives/td-12345/checkin
Authorization: Bearer {dealer_jwt_token}
Content-Type: application/json

{
  "odometerReading": 45230,
  "preTestPhotos": [
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-front.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-rear.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-left.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-right.jpg"
  ],
  "routeId": "route-001",
  "salesRepId": "user-dealer-rep-001",
  "notes": "Vehículo en excelente estado"
}
```

**Response 200 OK:**

```json
{
  "success": true,
  "checkedIn": true,
  "checkInTime": "2026-01-30T09:00:00Z",
  "odometerAtCheckIn": 45230,
  "preTestPhotos": [
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-front.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-rear.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-left.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/pre-right.jpg"
  ],
  "route": {
    "id": "route-001",
    "name": "Ruta Urbana Centro",
    "distanceKm": 15,
    "estimatedMinutes": 25
  }
}
```

#### POST /api/testdrives/{id}/checkout

```http
POST https://api.okla.com.do/api/testdrives/td-12345/checkout
Authorization: Bearer {dealer_jwt_token}
Content-Type: application/json

{
  "odometerReading": 45248,
  "postTestPhotos": [
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-front.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-rear.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-left.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-right.jpg"
  ],
  "notes": "Vehículo devuelto sin incidentes"
}
```

**Response 200 OK:**

```json
{
  "success": true,
  "checkedOut": true,
  "checkOutTime": "2026-01-30T09:35:00Z",
  "odometerAtCheckOut": 45248,
  "kmDriven": 18,
  "postTestPhotos": [
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-front.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-rear.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-left.jpg",
    "https://okla-media.s3.amazonaws.com/testdrives/td-12345/post-right.jpg"
  ],
  "feedbackRequestScheduled": true,
  "feedbackRequestTime": "2026-01-30T10:35:00Z"
}
```

#### POST /api/testdrives/{id}/feedback

```http
POST https://api.okla.com.do/api/testdrives/td-12345/feedback
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "overallRating": 5,
  "comfortRating": 4,
  "performanceRating": 5,
  "conditionRating": 5,
  "meetsExpectations": true,
  "wouldRecommend": true,
  "interestedInBuying": true,
  "likes": "Excelente manejo, muy silencioso, económico en combustible",
  "dislikes": "El color no es mi favorito, pero no es un deal breaker",
  "additionalComments": "Estoy muy interesado en hacer una oferta. ¿Cuál es el mejor precio?"
}
```

**Response 200 OK:**

```json
{
  "success": true,
  "feedbackSubmitted": true,
  "submittedAt": "2026-01-30T10:40:00Z",
  "leadScore": 85,
  "leadClassification": "Hot",
  "followUpScheduled": true,
  "followUpBy": "Carlos Vendedor",
  "followUpDate": "2026-01-30T16:00:00Z",
  "message": "¡Gracias por tu feedback! Un miembro de nuestro equipo te contactará pronto."
}
```

---

## 4️⃣ COMPONENTES REACT

### 4.1 TestDriveButton.tsx

Botón CTA en la página de detalle del vehículo.

```tsx
// filepath: src/components/test-drives/TestDriveButton.tsx
"use client";

import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { TestDriveModal } from "@/components/test-drives/TestDriveModal";
import { Calendar, ChevronRight } from "lucide-react";
import { useAuth } from "@/lib/hooks/useAuth";
import { useRouter } from "next/navigation";
import type { Vehicle } from "@/types/vehicle";

interface TestDriveButtonProps {
  vehicle: Vehicle;
  className?: string;
  variant?: "default" | "outline" | "ghost";
  size?: "sm" | "md" | "lg";
}

export function TestDriveButton({
  vehicle,
  className,
  variant = "default",
  size = "md",
}: TestDriveButtonProps) {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { user, isAuthenticated } = useAuth();
  const router = useRouter();

  const handleClick = () => {
    if (!isAuthenticated) {
      // Redirigir a login con redirect de vuelta
      router.push(`/login?redirect=/vehicles/${vehicle.slug}&action=testdrive`);
      return;
    }

    setIsModalOpen(true);
  };

  // No mostrar si vehículo no disponible para test drive
  if (!vehicle.allowTestDrive) {
    return null;
  }

  return (
    <>
      <Button
        onClick={handleClick}
        variant={variant}
        size={size}
        className={className}
      >
        <Calendar className="w-4 h-4 mr-2" />
        Agendar Test Drive
        <ChevronRight className="w-4 h-4 ml-1" />
      </Button>

      {isModalOpen && (
        <TestDriveModal
          vehicle={vehicle}
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
        />
      )}
    </>
  );
}
```

---

### 4.2 TestDriveModal.tsx

Modal principal con wizard de 4 pasos.

```tsx
// filepath: src/components/test-drives/TestDriveModal.tsx
"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/Dialog";
import { TestDriveCalendar } from "@/components/test-drives/TestDriveCalendar";
import { TestDriveDriverInfo } from "@/components/test-drives/TestDriveDriverInfo";
import { TestDriveConfirmation } from "@/components/test-drives/TestDriveConfirmation";
import { TestDriveSuccess } from "@/components/test-drives/TestDriveSuccess";
import { Progress } from "@/components/ui/Progress";
import type { Vehicle } from "@/types/vehicle";
import type { AvailabilitySlot, TestDriveBooking } from "@/types/test-drive";

interface TestDriveModalProps {
  vehicle: Vehicle;
  isOpen: boolean;
  onClose: () => void;
}

type Step = "calendar" | "driver" | "confirmation" | "success";

export function TestDriveModal({
  vehicle,
  isOpen,
  onClose,
}: TestDriveModalProps) {
  const [currentStep, setCurrentStep] = useState<Step>("calendar");
  const [selectedSlot, setSelectedSlot] = useState<AvailabilitySlot | null>(
    null,
  );
  const [driverInfo, setDriverInfo] = useState<any>(null);
  const [booking, setBooking] = useState<TestDriveBooking | null>(null);

  const steps = {
    calendar: { order: 1, label: "Seleccionar fecha" },
    driver: { order: 2, label: "Información del conductor" },
    confirmation: { order: 3, label: "Confirmar" },
    success: { order: 4, label: "¡Listo!" },
  };

  const currentStepNumber = steps[currentStep].order;
  const totalSteps = Object.keys(steps).length;
  const progress = ((currentStepNumber - 1) / (totalSteps - 1)) * 100;

  const handleSlotSelected = (slot: AvailabilitySlot) => {
    setSelectedSlot(slot);
    setCurrentStep("driver");
  };

  const handleDriverInfoSubmitted = (info: any) => {
    setDriverInfo(info);
    setCurrentStep("confirmation");
  };

  const handleBookingConfirmed = (newBooking: TestDriveBooking) => {
    setBooking(newBooking);
    setCurrentStep("success");
  };

  const handleClose = () => {
    // Si ya completó el booking, cerrar directamente
    if (currentStep === "success") {
      onClose();
      return;
    }

    // Confirmar antes de cerrar si hay progreso
    if (currentStep !== "calendar") {
      const confirmClose = window.confirm(
        "¿Estás seguro que deseas cancelar? Perderás el progreso.",
      );
      if (!confirmClose) return;
    }

    onClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            Agendar Test Drive - {vehicle.year} {vehicle.make} {vehicle.model}
          </DialogTitle>
        </DialogHeader>

        {/* Progress bar */}
        {currentStep !== "success" && (
          <div className="mb-6">
            <div className="flex justify-between mb-2 text-sm text-gray-600">
              <span>
                Paso {currentStepNumber} de {totalSteps}
              </span>
              <span>{steps[currentStep].label}</span>
            </div>
            <Progress value={progress} className="h-2" />
          </div>
        )}

        {/* Step content */}
        <div className="py-4">
          {currentStep === "calendar" && (
            <TestDriveCalendar
              vehicle={vehicle}
              onSlotSelect={handleSlotSelected}
            />
          )}

          {currentStep === "driver" && selectedSlot && (
            <TestDriveDriverInfo
              vehicle={vehicle}
              slot={selectedSlot}
              onSubmit={handleDriverInfoSubmitted}
              onBack={() => setCurrentStep("calendar")}
            />
          )}

          {currentStep === "confirmation" && selectedSlot && driverInfo && (
            <TestDriveConfirmation
              vehicle={vehicle}
              slot={selectedSlot}
              driverInfo={driverInfo}
              onConfirm={handleBookingConfirmed}
              onBack={() => setCurrentStep("driver")}
            />
          )}

          {currentStep === "success" && booking && (
            <TestDriveSuccess booking={booking} onClose={onClose} />
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

---

### 4.3 TestDriveCalendar.tsx

Componente de calendario con slots disponibles.

```tsx
// filepath: src/components/test-drives/TestDriveCalendar.tsx
"use client";

import { useState } from "react";
import { useTestDriveAvailability } from "@/lib/hooks/useTestDrive";
import { Calendar } from "@/components/ui/Calendar";
import { Button } from "@/components/ui/Button";
import { Alert, AlertDescription } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { Clock, MapPin, Info } from "lucide-react";
import { format, parseISO, addDays } from "date-fns";
import { es } from "date-fns/locale";
import type { Vehicle } from "@/types/vehicle";
import type { AvailabilitySlot } from "@/types/test-drive";

interface TestDriveCalendarProps {
  vehicle: Vehicle;
  onSlotSelect: (slot: AvailabilitySlot) => void;
}

export function TestDriveCalendar({
  vehicle,
  onSlotSelect,
}: TestDriveCalendarProps) {
  const [selectedDate, setSelectedDate] = useState<Date | undefined>(undefined);

  const fromDate = new Date();
  const toDate = addDays(new Date(), 14); // 2 semanas adelante

  const { availability, isLoading, error } = useTestDriveAvailability(
    vehicle.id,
    format(fromDate, "yyyy-MM-dd"),
    format(toDate, "yyyy-MM-dd"),
  );

  // Filtrar días con disponibilidad
  const datesWithAvailability = availability
    ? availability
        .filter((day) => day.slots.some((slot) => slot.available))
        .map((day) => parseISO(day.date))
    : [];

  const selectedDayData = availability?.find(
    (day) => selectedDate && day.date === format(selectedDate, "yyyy-MM-dd"),
  );

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertDescription>
          Error al cargar disponibilidad. Por favor intenta de nuevo.
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-6">
      {/* Dealer info */}
      {availability && availability.length > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <Info className="w-5 h-5 text-blue-600 mt-0.5" />
            <div>
              <h4 className="font-semibold text-blue-900">
                {availability[0].dealerName || "Dealer"}
              </h4>
              {availability[0].dealerAddress && (
                <div className="flex items-center gap-2 text-sm text-blue-700 mt-1">
                  <MapPin className="w-4 h-4" />
                  <span>{availability[0].dealerAddress}</span>
                </div>
              )}
              {availability[0].config && (
                <div className="flex items-center gap-2 text-sm text-blue-700 mt-1">
                  <Clock className="w-4 h-4" />
                  <span>
                    Duración: {availability[0].config.testDriveDurationMinutes}{" "}
                    min
                  </span>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      <div className="grid md:grid-cols-2 gap-6">
        {/* Calendar */}
        <div>
          <h3 className="font-semibold mb-4">Selecciona una fecha</h3>
          {isLoading ? (
            <Skeleton className="w-full h-80" />
          ) : (
            <Calendar
              mode="single"
              selected={selectedDate}
              onSelect={setSelectedDate}
              disabled={(date) =>
                date < new Date() ||
                date > toDate ||
                !datesWithAvailability.some(
                  (d) => format(d, "yyyy-MM-dd") === format(date, "yyyy-MM-dd"),
                )
              }
              modifiers={{
                available: datesWithAvailability,
              }}
              modifiersClassNames={{
                available: "bg-green-100 text-green-900 font-semibold",
              }}
              locale={es}
              className="border rounded-lg"
            />
          )}
          <p className="text-sm text-gray-500 mt-2">
            * Días en verde tienen disponibilidad
          </p>
        </div>

        {/* Time slots */}
        <div>
          <h3 className="font-semibold mb-4">Selecciona un horario</h3>
          {!selectedDate ? (
            <div className="flex items-center justify-center h-80 bg-gray-50 border border-dashed border-gray-300 rounded-lg">
              <p className="text-gray-500">Primero selecciona una fecha →</p>
            </div>
          ) : isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 6 }).map((_, i) => (
                <Skeleton key={i} className="w-full h-12" />
              ))}
            </div>
          ) : selectedDayData ? (
            <div className="space-y-2 max-h-80 overflow-y-auto pr-2">
              {selectedDayData.slots.map((slot, index) => (
                <Button
                  key={index}
                  onClick={() =>
                    onSlotSelect({
                      date: selectedDayData.date,
                      time: slot.time,
                      available: slot.available,
                      dayOfWeek: selectedDayData.dayOfWeek,
                    })
                  }
                  disabled={!slot.available}
                  variant={slot.available ? "outline" : "ghost"}
                  className={`w-full justify-start ${
                    slot.available
                      ? "hover:bg-blue-50 hover:border-blue-500"
                      : "opacity-50 cursor-not-allowed"
                  }`}
                >
                  <Clock className="w-4 h-4 mr-2" />
                  {slot.time}
                  {!slot.available && slot.reason && (
                    <span className="ml-auto text-xs text-gray-500">
                      ({slot.reason})
                    </span>
                  )}
                </Button>
              ))}
            </div>
          ) : (
            <div className="flex items-center justify-center h-80 bg-gray-50 border border-gray-300 rounded-lg">
              <p className="text-gray-500">No hay horarios disponibles</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
```

---

### 4.4 TestDriveDriverInfo.tsx

Formulario de información del conductor.

```tsx
// filepath: src/components/test-drives/TestDriveDriverInfo.tsx
"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { Textarea } from "@/components/ui/Textarea";
import { Alert, AlertDescription } from "@/components/ui/Alert";
import { ImageUpload } from "@/components/ui/ImageUpload";
import {
  Calendar,
  User,
  CreditCard,
  Upload,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import { format, parseISO } from "date-fns";
import { es } from "date-fns/locale";
import type { Vehicle } from "@/types/vehicle";
import type { AvailabilitySlot } from "@/types/test-drive";

const driverInfoSchema = z.object({
  driverName: z.string().min(5, "Nombre completo requerido"),
  driverCedula: z
    .string()
    .regex(/^\d{3}-\d{7}-\d{1}$/, "Formato: 001-1234567-8"),
  driverLicenseNumber: z.string().min(5, "Número de licencia requerido"),
  driverLicenseExpiry: z.string().refine((date) => {
    const expiry = new Date(date);
    return expiry > new Date();
  }, "Licencia vencida"),
  driverLicensePhotoUrl: z.string().url("Foto de licencia requerida"),
  notes: z.string().optional(),
});

type DriverInfoFormData = z.infer<typeof driverInfoSchema>;

interface TestDriveDriverInfoProps {
  vehicle: Vehicle;
  slot: AvailabilitySlot;
  onSubmit: (data: DriverInfoFormData) => void;
  onBack: () => void;
}

export function TestDriveDriverInfo({
  vehicle,
  slot,
  onSubmit,
  onBack,
}: TestDriveDriverInfoProps) {
  const [licensePhotoUrl, setLicensePhotoUrl] = useState<string>("");

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setValue,
  } = useForm<DriverInfoFormData>({
    resolver: zodResolver(driverInfoSchema),
  });

  const handleLicensePhotoUploaded = (url: string) => {
    setLicensePhotoUrl(url);
    setValue("driverLicensePhotoUrl", url);
  };

  const onSubmitForm = (data: DriverInfoFormData) => {
    onSubmit(data);
  };

  const slotDate = parseISO(`${slot.date}T${slot.time}`);

  return (
    <form onSubmit={handleSubmit(onSubmitForm)} className="space-y-6">
      {/* Selected slot recap */}
      <Alert>
        <Calendar className="w-4 h-4" />
        <AlertDescription>
          <strong>Fecha seleccionada:</strong>{" "}
          {format(slotDate, "EEEE, d 'de' MMMM 'de' yyyy", { locale: es })} a
          las {slot.time}
        </AlertDescription>
      </Alert>

      <div className="space-y-4">
        <h3 className="font-semibold text-lg">Información del conductor</h3>

        {/* Driver name */}
        <div>
          <Label htmlFor="driverName">
            Nombre completo <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <User className="absolute left-3 top-3 h-5 w-5 text-gray-400" />
            <Input
              id="driverName"
              {...register("driverName")}
              placeholder="Juan Pérez García"
              className="pl-10"
            />
          </div>
          {errors.driverName && (
            <p className="text-sm text-red-500 mt-1">
              {errors.driverName.message}
            </p>
          )}
        </div>

        {/* Cédula */}
        <div>
          <Label htmlFor="driverCedula">
            Cédula de identidad <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <CreditCard className="absolute left-3 top-3 h-5 w-5 text-gray-400" />
            <Input
              id="driverCedula"
              {...register("driverCedula")}
              placeholder="001-1234567-8"
              className="pl-10"
            />
          </div>
          {errors.driverCedula && (
            <p className="text-sm text-red-500 mt-1">
              {errors.driverCedula.message}
            </p>
          )}
        </div>

        <div className="grid sm:grid-cols-2 gap-4">
          {/* License number */}
          <div>
            <Label htmlFor="driverLicenseNumber">
              Número de licencia <span className="text-red-500">*</span>
            </Label>
            <Input
              id="driverLicenseNumber"
              {...register("driverLicenseNumber")}
              placeholder="RD-SD-123456-12"
            />
            {errors.driverLicenseNumber && (
              <p className="text-sm text-red-500 mt-1">
                {errors.driverLicenseNumber.message}
              </p>
            )}
          </div>

          {/* License expiry */}
          <div>
            <Label htmlFor="driverLicenseExpiry">
              Fecha de vencimiento <span className="text-red-500">*</span>
            </Label>
            <Input
              id="driverLicenseExpiry"
              type="date"
              {...register("driverLicenseExpiry")}
            />
            {errors.driverLicenseExpiry && (
              <p className="text-sm text-red-500 mt-1">
                {errors.driverLicenseExpiry.message}
              </p>
            )}
          </div>
        </div>

        {/* License photo upload */}
        <div>
          <Label>
            Foto de licencia <span className="text-red-500">*</span>
          </Label>
          <ImageUpload
            onUploadComplete={handleLicensePhotoUploaded}
            acceptedFormats={["image/jpeg", "image/png", "image/webp"]}
            maxSizeMB={5}
            className="mt-2"
            folder="licenses"
          />
          {licensePhotoUrl && (
            <div className="mt-3">
              <img
                src={licensePhotoUrl}
                alt="Licencia"
                className="max-w-sm rounded-lg border"
              />
            </div>
          )}
          {errors.driverLicensePhotoUrl && (
            <p className="text-sm text-red-500 mt-1">
              {errors.driverLicensePhotoUrl.message}
            </p>
          )}
          <p className="text-sm text-gray-500 mt-1">
            📷 Sube una foto clara de tu licencia de conducir (frente)
          </p>
        </div>

        {/* Notes */}
        <div>
          <Label htmlFor="notes">Notas adicionales (opcional)</Label>
          <Textarea
            id="notes"
            {...register("notes")}
            placeholder="Ej: Primera vez probando un Toyota, ¿el vendedor puede acompañarme?"
            rows={3}
          />
        </div>
      </div>

      {/* Actions */}
      <div className="flex justify-between pt-4 border-t">
        <Button type="button" variant="ghost" onClick={onBack}>
          <ChevronLeft className="w-4 h-4 mr-1" />
          Volver
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Validando..." : "Continuar"}
          <ChevronRight className="w-4 h-4 ml-1" />
        </Button>
      </div>
    </form>
  );
}
```

---

### 4.5 TestDriveConfirmation.tsx

Pantalla de confirmación antes de crear la reserva.

```tsx
// filepath: src/components/test-drives/TestDriveConfirmation.tsx
"use client";

import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Alert, AlertDescription } from "@/components/ui/Alert";
import { Checkbox } from "@/components/ui/Checkbox";
import { Label } from "@/components/ui/Label";
import { useCreateTestDrive } from "@/lib/hooks/useTestDrive";
import {
  Calendar,
  Clock,
  MapPin,
  User,
  CreditCard,
  FileText,
  AlertTriangle,
  ChevronLeft,
  CheckCircle,
} from "lucide-react";
import { format, parseISO } from "date-fns";
import { es } from "date-fns/locale";
import type { Vehicle } from "@/types/vehicle";
import type { AvailabilitySlot } from "@/types/test-drive";

interface TestDriveConfirmationProps {
  vehicle: Vehicle;
  slot: AvailabilitySlot;
  driverInfo: any;
  onConfirm: (booking: any) => void;
  onBack: () => void;
}

export function TestDriveConfirmation({
  vehicle,
  slot,
  driverInfo,
  onConfirm,
  onBack,
}: TestDriveConfirmationProps) {
  const [agreedToTerms, setAgreedToTerms] = useState(false);
  const [agreedToWaiver, setAgreedToWaiver] = useState(false);

  const { createTestDrive, isCreating, error } = useCreateTestDrive();

  const handleConfirm = async () => {
    if (!agreedToTerms || !agreedToWaiver) {
      alert("Debes aceptar los términos y condiciones");
      return;
    }

    const testDriveData = {
      vehicleId: vehicle.id,
      scheduledDate: slot.date,
      scheduledTime: slot.time,
      ...driverInfo,
    };

    const booking = await createTestDrive(testDriveData);

    if (booking) {
      onConfirm(booking);
    }
  };

  const slotDate = parseISO(`${slot.date}T${slot.time}`);

  return (
    <div className="space-y-6">
      <h3 className="font-semibold text-lg">Confirma tu test drive</h3>

      {/* Vehicle summary */}
      <div className="bg-gray-50 border rounded-lg p-4">
        <div className="flex gap-4">
          <img
            src={vehicle.images?.[0]?.url || "/placeholder-car.jpg"}
            alt={`${vehicle.make} ${vehicle.model}`}
            className="w-24 h-24 object-cover rounded-lg"
          />
          <div>
            <h4 className="font-semibold">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h4>
            <p className="text-sm text-gray-600">{vehicle.trim}</p>
            <p className="text-sm text-gray-600 mt-1">VIN: {vehicle.vin}</p>
          </div>
        </div>
      </div>

      {/* Appointment details */}
      <div className="space-y-3">
        <div className="flex items-start gap-3">
          <Calendar className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium">Fecha</p>
            <p className="text-sm text-gray-600">
              {format(slotDate, "EEEE, d 'de' MMMM 'de' yyyy", { locale: es })}
            </p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <Clock className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium">Hora</p>
            <p className="text-sm text-gray-600">{slot.time}</p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <MapPin className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium">Ubicación</p>
            <p className="text-sm text-gray-600">
              {/* TODO: Get from availability data */}
              Av. Winston Churchill #1234, Santo Domingo
            </p>
          </div>
        </div>
      </div>

      <hr />

      {/* Driver info */}
      <div className="space-y-3">
        <h4 className="font-semibold">Información del conductor</h4>

        <div className="flex items-start gap-3">
          <User className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium">Nombre</p>
            <p className="text-sm text-gray-600">{driverInfo.driverName}</p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <CreditCard className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium">Cédula</p>
            <p className="text-sm text-gray-600">{driverInfo.driverCedula}</p>
          </div>
        </div>

        <div className="flex items-start gap-3">
          <FileText className="w-5 h-5 text-blue-600 mt-0.5" />
          <div>
            <p className="font-medium">Licencia de conducir</p>
            <p className="text-sm text-gray-600">
              {driverInfo.driverLicenseNumber}
            </p>
            <p className="text-sm text-gray-500">
              Vence:{" "}
              {format(
                parseISO(driverInfo.driverLicenseExpiry),
                "d 'de' MMM yyyy",
                { locale: es },
              )}
            </p>
          </div>
        </div>
      </div>

      <hr />

      {/* Terms and conditions */}
      <div className="space-y-4 bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex items-start gap-2">
          <AlertTriangle className="w-5 h-5 text-yellow-600 mt-0.5" />
          <div>
            <h4 className="font-semibold text-yellow-900">
              Requisitos importantes
            </h4>
            <ul className="text-sm text-yellow-800 mt-2 space-y-1">
              <li>• Debes presentarte 10 minutos antes de tu cita</li>
              <li>• Trae tu licencia de conducir original y tu cédula</li>
              <li>
                • Firmarás un formulario de responsabilidad antes del test drive
              </li>
              <li>• El test drive será de aproximadamente 30 minutos</li>
              <li>• Un representante de ventas puede acompañarte</li>
            </ul>
          </div>
        </div>
      </div>

      {/* Checkboxes */}
      <div className="space-y-3">
        <div className="flex items-start gap-2">
          <Checkbox
            id="terms"
            checked={agreedToTerms}
            onCheckedChange={(checked) => setAgreedToTerms(checked as boolean)}
          />
          <Label
            htmlFor="terms"
            className="text-sm leading-relaxed cursor-pointer"
          >
            Acepto los{" "}
            <a
              href="/terminos"
              target="_blank"
              className="text-blue-600 underline"
            >
              términos y condiciones
            </a>{" "}
            de OKLA para agendar test drives
          </Label>
        </div>

        <div className="flex items-start gap-2">
          <Checkbox
            id="waiver"
            checked={agreedToWaiver}
            onCheckedChange={(checked) => setAgreedToWaiver(checked as boolean)}
          />
          <Label
            htmlFor="waiver"
            className="text-sm leading-relaxed cursor-pointer"
          >
            Entiendo que debo firmar un{" "}
            <strong>formulario de responsabilidad (waiver)</strong> antes del
            test drive y que soy responsable por cualquier daño al vehículo
            durante la prueba
          </Label>
        </div>
      </div>

      {/* Error */}
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error.message}</AlertDescription>
        </Alert>
      )}

      {/* Actions */}
      <div className="flex justify-between pt-4 border-t">
        <Button
          type="button"
          variant="ghost"
          onClick={onBack}
          disabled={isCreating}
        >
          <ChevronLeft className="w-4 h-4 mr-1" />
          Volver
        </Button>
        <Button
          onClick={handleConfirm}
          disabled={!agreedToTerms || !agreedToWaiver || isCreating}
          size="lg"
        >
          {isCreating ? (
            "Confirmando..."
          ) : (
            <>
              <CheckCircle className="w-5 h-5 mr-2" />
              Confirmar reserva
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
```

---

### 4.6 TestDriveSuccess.tsx

Pantalla de éxito con detalles de la reserva.

```tsx
// filepath: src/components/test-drives/TestDriveSuccess.tsx
"use client";

import { Button } from "@/components/ui/Button";
import { Alert, AlertDescription } from "@/components/ui/Alert";
import {
  CheckCircle,
  Calendar,
  MapPin,
  Mail,
  MessageSquare,
  Download,
  X,
} from "lucide-react";
import { format, parseISO } from "date-fns";
import { es } from "date-fns/locale";
import type { TestDriveBooking } from "@/types/test-drive";

interface TestDriveSuccessProps {
  booking: TestDriveBooking;
  onClose: () => void;
}

export function TestDriveSuccess({ booking, onClose }: TestDriveSuccessProps) {
  const appointmentDate = parseISO(booking.scheduledDate);

  const handleAddToCalendar = () => {
    // Generate ICS file
    const icsContent = `BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
DTSTART:${format(appointmentDate, "yyyyMMdd'T'HHmmss")}
DTEND:${format(appointmentDate, "yyyyMMdd'T'HHmmss")}
SUMMARY:Test Drive - ${booking.vehicleTitle}
DESCRIPTION:Test drive de ${booking.vehicleTitle} en ${booking.dealerName}
LOCATION:${booking.dealerName}
END:VEVENT
END:VCALENDAR`;

    const blob = new Blob([icsContent], { type: "text/calendar" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `testdrive-${booking.id}.ics`;
    a.click();
  };

  return (
    <div className="space-y-6 py-4">
      {/* Success icon */}
      <div className="flex flex-col items-center text-center">
        <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mb-4">
          <CheckCircle className="w-10 h-10 text-green-600" />
        </div>
        <h2 className="text-2xl font-bold text-green-900">
          ¡Reserva confirmada!
        </h2>
        <p className="text-gray-600 mt-2">
          Tu test drive ha sido agendado exitosamente
        </p>
      </div>

      {/* Booking details */}
      <div className="bg-green-50 border border-green-200 rounded-lg p-6">
        <div className="space-y-4">
          <div>
            <p className="text-sm text-green-700 font-medium">
              Código de confirmación
            </p>
            <p className="text-2xl font-mono font-bold text-green-900">
              {booking.confirmationCode}
            </p>
          </div>

          <hr className="border-green-200" />

          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-green-700 font-medium">Vehículo</p>
              <p className="text-green-900 font-semibold">
                {booking.vehicleTitle}
              </p>
            </div>

            <div>
              <p className="text-sm text-green-700 font-medium">Dealer</p>
              <p className="text-green-900">{booking.dealerName}</p>
            </div>

            <div>
              <p className="text-sm text-green-700 font-medium">Fecha</p>
              <p className="text-green-900">
                {format(appointmentDate, "EEEE, d 'de' MMMM", { locale: es })}
              </p>
            </div>

            <div>
              <p className="text-sm text-green-700 font-medium">Hora</p>
              <p className="text-green-900">
                {format(appointmentDate, "h:mm a", { locale: es })}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="grid sm:grid-cols-2 gap-3">
        <Button onClick={handleAddToCalendar} variant="outline">
          <Calendar className="w-4 h-4 mr-2" />
          Agregar a calendario
        </Button>
        <Button variant="outline" asChild>
          <a href={`/appointments/${booking.id}`}>
            <MapPin className="w-4 h-4 mr-2" />
            Ver detalles
          </a>
        </Button>
      </div>

      {/* Next steps */}
      <Alert>
        <Mail className="w-4 h-4" />
        <AlertDescription>
          <strong>Próximos pasos:</strong>
          <ul className="mt-2 space-y-1 text-sm">
            <li>✓ Te enviaremos un email de confirmación</li>
            <li>✓ Recordatorio 24 horas antes por email y SMS</li>
            <li>✓ Recordatorio 2 horas antes por SMS</li>
            <li>✓ Trae tu licencia de conducir y cédula originales</li>
          </ul>
        </AlertDescription>
      </Alert>

      {/* Close button */}
      <div className="flex justify-center pt-4">
        <Button onClick={onClose} size="lg" className="w-full sm:w-auto">
          <X className="w-4 h-4 mr-2" />
          Cerrar
        </Button>
      </div>
    </div>
  );
}
```

---

## 5️⃣ HOOKS

### 5.1 useTestDrive.ts

Hook principal con todas las operaciones.

```typescript
// filepath: src/lib/hooks/useTestDrive.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { testDriveService } from "@/lib/services/testDriveService";
import { useToast } from "@/lib/hooks/useToast";
import type {
  TestDriveBooking,
  TestDriveCreateDto,
  TestDriveFeedbackDto,
} from "@/types/test-drive";

// Hook para obtener disponibilidad
export function useTestDriveAvailability(
  vehicleId: string,
  fromDate: string,
  toDate: string,
) {
  return useQuery({
    queryKey: ["testdrive-availability", vehicleId, fromDate, toDate],
    queryFn: () =>
      testDriveService.getAvailability(vehicleId, fromDate, toDate),
    enabled: !!vehicleId && !!fromDate && !!toDate,
    staleTime: 2 * 60 * 1000, // 2 minutos (slots pueden cambiar)
    retry: 2,
  });
}

// Hook para crear test drive
export function useCreateTestDrive() {
  const queryClient = useQueryClient();
  const { toast } = useToast();

  const mutation = useMutation({
    mutationFn: (data: TestDriveCreateDto) => testDriveService.create(data),
    onSuccess: (booking) => {
      // Invalidate availability queries
      queryClient.invalidateQueries({ queryKey: ["testdrive-availability"] });

      // Invalidate user's appointments
      queryClient.invalidateQueries({ queryKey: ["my-appointments"] });

      toast({
        title: "¡Test drive agendado!",
        description: `Tu test drive ha sido confirmado para el ${new Date(
          booking.scheduledDate,
        ).toLocaleDateString("es-DO")}.`,
        variant: "success",
      });
    },
    onError: (error: any) => {
      toast({
        title: "Error al agendar",
        description:
          error.message ||
          "No se pudo agendar el test drive. Intenta de nuevo.",
        variant: "destructive",
      });
    },
  });

  return {
    createTestDrive: mutation.mutateAsync,
    isCreating: mutation.isPending,
    error: mutation.error,
  };
}

// Hook para obtener un test drive por ID
export function useTestDrive(testDriveId: string) {
  return useQuery({
    queryKey: ["testdrive", testDriveId],
    queryFn: () => testDriveService.getById(testDriveId),
    enabled: !!testDriveId,
  });
}

// Hook para cancelar test drive
export function useCancelTestDrive() {
  const queryClient = useQueryClient();
  const { toast } = useToast();

  const mutation = useMutation({
    mutationFn: (testDriveId: string) => testDriveService.cancel(testDriveId),
    onSuccess: (_, testDriveId) => {
      queryClient.invalidateQueries({ queryKey: ["testdrive", testDriveId] });
      queryClient.invalidateQueries({ queryKey: ["my-appointments"] });
      queryClient.invalidateQueries({ queryKey: ["testdrive-availability"] });

      toast({
        title: "Test drive cancelado",
        description: "Tu reserva ha sido cancelada exitosamente.",
      });
    },
    onError: () => {
      toast({
        title: "Error",
        description: "No se pudo cancelar el test drive.",
        variant: "destructive",
      });
    },
  });

  return {
    cancel: mutation.mutate,
    isCancelling: mutation.isPending,
  };
}

// Hook para firmar waiver
export function useSignWaiver() {
  const queryClient = useQueryClient();
  const { toast } = useToast();

  const mutation = useMutation({
    mutationFn: ({
      testDriveId,
      signatureDataUrl,
    }: {
      testDriveId: string;
      signatureDataUrl: string;
    }) => testDriveService.signWaiver(testDriveId, signatureDataUrl),
    onSuccess: (_, { testDriveId }) => {
      queryClient.invalidateQueries({ queryKey: ["testdrive", testDriveId] });

      toast({
        title: "Formulario firmado",
        description: "Puedes proceder con el test drive.",
        variant: "success",
      });
    },
    onError: () => {
      toast({
        title: "Error",
        description: "No se pudo guardar la firma.",
        variant: "destructive",
      });
    },
  });

  return {
    signWaiver: mutation.mutateAsync,
    isSigning: mutation.isPending,
  };
}

// Hook para enviar feedback
export function useSubmitTestDriveFeedback() {
  const queryClient = useQueryClient();
  const { toast } = useToast();

  const mutation = useMutation({
    mutationFn: ({
      testDriveId,
      feedback,
    }: {
      testDriveId: string;
      feedback: TestDriveFeedbackDto;
    }) => testDriveService.submitFeedback(testDriveId, feedback),
    onSuccess: (_, { testDriveId }) => {
      queryClient.invalidateQueries({ queryKey: ["testdrive", testDriveId] });

      toast({
        title: "¡Gracias por tu feedback!",
        description: "Tu opinión nos ayuda a mejorar el servicio.",
        variant: "success",
      });
    },
    onError: () => {
      toast({
        title: "Error",
        description: "No se pudo enviar el feedback.",
        variant: "destructive",
      });
    },
  });

  return {
    submitFeedback: mutation.mutateAsync,
    isSubmitting: mutation.isPending,
  };
}

// Hook para obtener mis test drives (usuario)
export function useMyTestDrives() {
  return useQuery({
    queryKey: ["my-test-drives"],
    queryFn: () => testDriveService.getMyTestDrives(),
  });
}

// Hook para obtener test drives del dealer
export function useDealerTestDrives(dealerId: string) {
  return useQuery({
    queryKey: ["dealer-test-drives", dealerId],
    queryFn: () => testDriveService.getDealerTestDrives(dealerId),
    enabled: !!dealerId,
  });
}
```

---

## 6️⃣ SERVICIOS TYPESCRIPT

### 6.1 testDriveService.ts

Cliente API completo.

```typescript
// filepath: src/lib/services/testDriveService.ts
import axios from "axios";
import type {
  AvailabilityResponse,
  TestDriveBooking,
  TestDriveCreateDto,
  TestDriveFeedbackDto,
  WaiverSignResponse,
  CheckInDto,
  CheckOutDto,
} from "@/types/test-drive";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.okla.com.do";

class TestDriveService {
  private axios = axios.create({
    baseURL: `${API_URL}/api/testdrives`,
    headers: {
      "Content-Type": "application/json",
    },
  });

  constructor() {
    // Interceptor para agregar token JWT
    this.axios.interceptors.request.use((config) => {
      const token = localStorage.getItem("accessToken");
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
  }

  /**
   * Obtener slots disponibles para un vehículo
   * GET /api/testdrives/availability/{vehicleId}
   */
  async getAvailability(
    vehicleId: string,
    fromDate: string,
    toDate: string,
  ): Promise<AvailabilityResponse> {
    const response = await this.axios.get<AvailabilityResponse>(
      `/availability/${vehicleId}`,
      {
        params: { from: fromDate, to: toDate },
      },
    );
    return response.data;
  }

  /**
   * Crear test drive
   * POST /api/testdrives
   */
  async create(data: TestDriveCreateDto): Promise<TestDriveBooking> {
    const response = await this.axios.post<TestDriveBooking>("", data);
    return response.data;
  }

  /**
   * Obtener test drive por ID
   * GET /api/testdrives/{id}
   */
  async getById(testDriveId: string): Promise<TestDriveBooking> {
    const response = await this.axios.get<TestDriveBooking>(`/${testDriveId}`);
    return response.data;
  }

  /**
   * Obtener formulario de responsabilidad
   * GET /api/testdrives/{id}/waiver
   */
  async getWaiver(testDriveId: string): Promise<{ waiverPdfUrl: string }> {
    const response = await this.axios.get<{ waiverPdfUrl: string }>(
      `/${testDriveId}/waiver`,
    );
    return response.data;
  }

  /**
   * Firmar formulario de responsabilidad
   * POST /api/testdrives/{id}/waiver/sign
   */
  async signWaiver(
    testDriveId: string,
    signatureDataUrl: string,
  ): Promise<WaiverSignResponse> {
    const response = await this.axios.post<WaiverSignResponse>(
      `/${testDriveId}/waiver/sign`,
      {
        signatureDataUrl,
        agreedToTerms: true,
        signedAt: new Date().toISOString(),
      },
    );
    return response.data;
  }

  /**
   * Check-in al llegar al dealer
   * POST /api/testdrives/{id}/checkin
   */
  async checkIn(
    testDriveId: string,
    data: CheckInDto,
  ): Promise<TestDriveBooking> {
    const response = await this.axios.post<TestDriveBooking>(
      `/${testDriveId}/checkin`,
      data,
    );
    return response.data;
  }

  /**
   * Check-out al terminar
   * POST /api/testdrives/{id}/checkout
   */
  async checkOut(
    testDriveId: string,
    data: CheckOutDto,
  ): Promise<TestDriveBooking> {
    const response = await this.axios.post<TestDriveBooking>(
      `/${testDriveId}/checkout`,
      data,
    );
    return response.data;
  }

  /**
   * Enviar feedback post-test
   * POST /api/testdrives/{id}/feedback
   */
  async submitFeedback(
    testDriveId: string,
    feedback: TestDriveFeedbackDto,
  ): Promise<TestDriveBooking> {
    const response = await this.axios.post<TestDriveBooking>(
      `/${testDriveId}/feedback`,
      feedback,
    );
    return response.data;
  }

  /**
   * Cancelar test drive
   * PUT /api/testdrives/{id}/cancel
   */
  async cancel(testDriveId: string): Promise<void> {
    await this.axios.put(`/${testDriveId}/cancel`);
  }

  /**
   * Re-agendar test drive
   * POST /api/testdrives/{id}/reschedule
   */
  async reschedule(
    testDriveId: string,
    newDate: string,
    newTime: string,
  ): Promise<TestDriveBooking> {
    const response = await this.axios.post<TestDriveBooking>(
      `/${testDriveId}/reschedule`,
      {
        scheduledDate: newDate,
        scheduledTime: newTime,
      },
    );
    return response.data;
  }

  /**
   * Obtener mis test drives (usuario)
   * GET /api/testdrives/my
   */
  async getMyTestDrives(): Promise<TestDriveBooking[]> {
    const response = await this.axios.get<TestDriveBooking[]>("/my");
    return response.data;
  }

  /**
   * Obtener test drives del dealer
   * GET /api/testdrives/dealer/{dealerId}
   */
  async getDealerTestDrives(dealerId: string): Promise<TestDriveBooking[]> {
    const response = await this.axios.get<TestDriveBooking[]>(
      `/dealer/${dealerId}`,
    );
    return response.data;
  }
}

export const testDriveService = new TestDriveService();
```

---

## 7️⃣ TIPOS E INTERFACES

```typescript
// filepath: src/types/test-drive.ts

export interface AvailabilitySlot {
  date: string; // "2026-01-30"
  time: string; // "09:00"
  available: boolean;
  reason?: string; // "Already booked", "Dealer closed", etc.
  dayOfWeek?: string; // "Wednesday"
}

export interface DealerConfig {
  testDriveDurationMinutes: number;
  bufferBetweenMinutes: number;
  maxAdvanceBookingDays: number;
  minAdvanceBookingHours: number;
  requireLicensePhoto: boolean;
  requireWaiverSignature: boolean;
  requireDeposit: boolean;
  depositAmount?: number;
}

export interface AvailabilityDay {
  date: string;
  dayOfWeek: string;
  slots: AvailabilitySlot[];
}

export interface AvailabilityResponse {
  vehicleId: string;
  dealerId: string;
  dealerName: string;
  dealerAddress?: string;
  config: DealerConfig;
  availability: AvailabilityDay[];
}

export interface TestDriveCreateDto {
  vehicleId: string;
  scheduledDate: string; // "2026-01-30"
  scheduledTime: string; // "09:00"
  driverName: string;
  driverCedula: string;
  driverLicenseNumber: string;
  driverLicenseExpiry: string;
  driverLicensePhotoUrl: string;
  notes?: string;
}

export interface TestDriveBooking {
  id: string;
  vehicleId: string;
  vehicleTitle: string;
  vehicleImage: string;
  vehicleVIN: string;
  userId: string;
  dealerId: string;
  dealerName: string;
  scheduledDate: string; // ISO 8601
  duration: number;
  status: TestDriveStatus;
  driverName: string;
  driverCedula: string;
  driverLicenseNumber: string;
  driverLicenseExpiry: string;
  driverLicensePhotoUrl: string;
  waiverSigned: boolean;
  waiverSignedAt?: string;
  waiverDocumentUrl?: string;
  checkedIn: boolean;
  checkInTime?: string;
  odometerAtCheckIn?: number;
  preTestPhotos?: string[];
  checkedOut: boolean;
  checkOutTime?: string;
  odometerAtCheckOut?: number;
  kmDriven?: number;
  postTestPhotos?: string[];
  feedback?: TestDriveFeedback;
  outcome?: TestDriveOutcome;
  createdAt: string;
  confirmationCode: string;
  remindersSent?: {
    twentyFourHours: boolean;
    twoHours: boolean;
  };
}

export enum TestDriveStatus {
  Pending = "Pending",
  Confirmed = "Confirmed",
  CheckedIn = "CheckedIn",
  InProgress = "InProgress",
  Completed = "Completed",
  Cancelled = "Cancelled",
  NoShow = "NoShow",
}

export enum TestDriveOutcome {
  NoDecision = "NoDecision",
  InterestedWillReturn = "InterestedWillReturn",
  MadeOffer = "MadeOffer",
  Purchased = "Purchased",
  NotInterested = "NotInterested",
  Cancelled = "Cancelled",
}

export interface TestDriveFeedback {
  overallRating: number; // 1-5
  comfortRating: number;
  performanceRating: number;
  conditionRating: number;
  meetsExpectations: boolean;
  wouldRecommend: boolean;
  interestedInBuying: boolean;
  likes: string;
  dislikes: string;
  additionalComments?: string;
  submittedAt?: string;
}

export interface TestDriveFeedbackDto {
  overallRating: number;
  comfortRating: number;
  performanceRating: number;
  conditionRating: number;
  meetsExpectations: boolean;
  wouldRecommend: boolean;
  interestedInBuying: boolean;
  likes: string;
  dislikes: string;
  additionalComments?: string;
}

export interface WaiverSignResponse {
  success: boolean;
  waiverSigned: boolean;
  waiverSignedAt: string;
  waiverDocumentUrl: string;
  signatureUrl: string;
}

export interface CheckInDto {
  odometerReading: number;
  preTestPhotos: string[];
  routeId?: string;
  salesRepId?: string;
  notes?: string;
}

export interface CheckOutDto {
  odometerReading: number;
  postTestPhotos: string[];
  notes?: string;
}
```

---

## 8️⃣ PROCESO TESTDRIVE-001: Agendar

### Flujo Completo

```
Usuario (Comprador)
    ↓
1. Ve página de detalle del vehículo (/vehicles/:slug)
    ↓
2. Click en botón "Agendar Test Drive" → TestDriveButton
    ↓
3. Se abre modal → TestDriveModal
    ↓
4. PASO 1: Selecciona fecha y hora → TestDriveCalendar
   - useTestDriveAvailability(vehicleId, fromDate, toDate)
   - GET /api/testdrives/availability/{vehicleId}
   - Backend calcula slots disponibles desde config del dealer
   - Muestra calendario con días disponibles en verde
   - Al seleccionar día, muestra lista de horarios
   - Click en slot → Avanza a PASO 2
    ↓
5. PASO 2: Información del conductor → TestDriveDriverInfo
   - Formulario con validación (react-hook-form + zod)
   - Campos: nombre, cédula, licencia, vencimiento licencia
   - Upload de foto de licencia → ImageUpload → MediaService
   - Notas opcionales
   - Click "Continuar" → Avanza a PASO 3
    ↓
6. PASO 3: Confirmación → TestDriveConfirmation
   - Muestra resumen del vehículo
   - Muestra fecha/hora seleccionada
   - Muestra info del conductor
   - Requisitos y términos
   - Checkboxes obligatorios
   - Click "Confirmar reserva" → useCreateTestDrive()
    ↓
7. POST /api/testdrives
   Backend:
   - Valida datos
   - Verifica slot aún disponible (Redis lock)
   - Crea TestDrive con Status=Pending
   - Genera waiver PDF
   - Envía confirmación por email/SMS
   - Agenda recordatorios (24h + 2h antes)
   - Agrega a calendario del dealer
    ↓
8. PASO 4: Éxito → TestDriveSuccess
   - Muestra código de confirmación
   - Muestra detalles de la reserva
   - Botones: "Agregar a calendario", "Ver detalles"
   - Cierra modal
    ↓
9. Recordatorios automáticos
   - T-24h: Email + SMS con recordatorio
   - T-2h: SMS + Push notification
```

### Coverage del Proceso TESTDRIVE-001

| Subpaso | Descripción                   | Componente/Hook             | Estado |
| ------- | ----------------------------- | --------------------------- | ------ |
| 1.1     | Usuario ve listing            | VehicleDetailPage           | ✅     |
| 1.2     | Click "Agendar Test Drive"    | TestDriveButton             | ✅     |
| 2.1     | GET /availability             | useTestDriveAvailability    | ✅     |
| 2.2     | Backend calcula slots         | AppointmentService          | ✅     |
| 3.1     | Mostrar calendario            | TestDriveCalendar           | ✅     |
| 3.2     | Seleccionar fecha             | TestDriveCalendar (state)   | ✅     |
| 3.3     | Seleccionar hora              | TestDriveCalendar (onClick) | ✅     |
| 4.1     | Formulario conductor          | TestDriveDriverInfo         | ✅     |
| 4.2     | Ingresar licencia             | TestDriveDriverInfo (form)  | ✅     |
| 4.3     | Subir foto licencia           | ImageUpload → MediaService  | ✅     |
| 5.1     | Checkout depósito (si aplica) | BillingService              | 🟡     |
| 6.1     | POST /testdrives              | useCreateTestDrive          | ✅     |
| 6.2     | Validar datos                 | AppointmentService          | ✅     |
| 6.3     | Verificar slot                | AppointmentService          | ✅     |
| 6.4     | Crear TestDrive               | AppointmentService          | ✅     |
| 7.1     | Generar waiver                | WaiverDocumentGenerator     | ✅     |
| 8.1     | Notificar dealer              | NotificationService         | ✅     |
| 8.2     | Confirmar usuario             | NotificationService         | ✅     |
| 8.3     | Agregar a calendario (ICS)    | TestDriveSuccess            | ✅     |
| 9.1     | Recordatorio 24h              | Scheduler                   | ✅     |
| 9.2     | Recordatorio 2h               | Scheduler                   | ✅     |
| 10.1    | Audit trail                   | AuditService                | ✅     |

**✅ Coverage: 21/22 (95%)**

**🟡 Pendiente:** Depósito opcional (baja prioridad, pocos dealers lo requieren)

---

## 9️⃣ PROCESO TESTDRIVE-002: Ejecutar

### Flujo Completo

```
DÍA DEL TEST DRIVE

1. Usuario llega al dealer (físicamente)
    ↓
2. Dealer busca cita en sistema
   - /dealer/appointments
   - Ve lista de citas del día
   - Confirma identidad del usuario
    ↓
3. Verificar licencia de conducir
   - Comparar licencia física con foto subida
   - Verificar vencimiento
   - Confirmar que coincide con cédula
    ↓
4. Firmar formulario de responsabilidad (Waiver)
   - Tablet/Kiosk con firma digital
   - WaiverSignature component
   - POST /api/testdrives/{id}/waiver/sign
   - Backend genera PDF con firma embebida
    ↓
5. Check-in
   - Dealer dashboard → Botón "Check-in"
   - Registrar odómetro inicial
   - Tomar fotos pre-test (4 ángulos: front, rear, left, right)
   - POST /api/testdrives/{id}/checkin
   - Status cambia a CheckedIn → InProgress
    ↓
6. **PRUEBA DE MANEJO FÍSICA** (30-60 min)
   - Usuario maneja el vehículo
   - Vendedor puede acompañar (según config del dealer)
   - Ruta aprobada (urbana/highway)
   - Sin tracking GPS (privacidad)
    ↓
7. Check-out
   - Dealer dashboard → Botón "Check-out"
   - Registrar odómetro final
   - Tomar fotos post-test (4 ángulos)
   - POST /api/testdrives/{id}/checkout
   - Backend calcula km driven
   - Status cambia a Completed
    ↓
8. Si depósito: reembolsar
   - BillingService.RefundDeposit()
   - Stripe/AZUL refund
    ↓
9. Solicitar feedback (1h después)
   - Scheduler envía email/SMS con link
   - Usuario accede a /testdrives/{id}/feedback
   - TestDriveFeedbackForm component
   - POST /api/testdrives/{id}/feedback
   - Backend calcula lead score (Hot/Warm/Cold)
    ↓
10. Dealer registra outcome
    - Dealer dashboard → Actualizar outcome
    - Outcome: NoDecision, Interested, MadeOffer, Purchased, NotInterested
    ↓
11. Seguimiento automático (si interesado)
    - Si InterestedInBuying = true → Crear tarea CRM
    - Asignar a sales rep
    - Enviar email de seguimiento
    - Schedule follow-up call
```

### Coverage del Proceso TESTDRIVE-002

| Subpaso | Descripción               | Componente/Sistema         | Estado |
| ------- | ------------------------- | -------------------------- | ------ |
| 1.1     | Usuario llega             | Físico                     | ✅     |
| 1.2     | Dealer busca cita         | DealerAppointmentsPage     | 🟡     |
| 2.1     | Verificar licencia        | Dashboard (manual)         | 🟡     |
| 2.2     | Comparar con foto         | Dashboard view             | 🟡     |
| 3.1     | Firmar waiver             | WaiverSignature component  | 🟡     |
| 3.2     | Capturar firma            | SignatureCanvas            | 🟡     |
| 3.3     | POST /waiver/sign         | useSignWaiver              | ✅     |
| 4.1     | Check-in                  | DealerDashboard action     | 🟡     |
| 4.2     | Registrar odómetro        | Check-in form              | 🟡     |
| 4.3     | Tomar fotos pre-test      | ImageUpload (multi)        | 🟡     |
| 4.4     | POST /checkin             | testDriveService.checkIn   | ✅     |
| 5.1     | PRUEBA DE MANEJO          | Físico                     | ✅     |
| 5.2     | Vendedor acompaña         | Físico                     | ✅     |
| 5.3     | Ruta aprobada             | Físico                     | ✅     |
| 6.1     | Regresar al dealer        | Físico                     | ✅     |
| 6.2     | Check-out                 | DealerDashboard action     | 🟡     |
| 6.3     | Registrar odómetro final  | Check-out form             | 🟡     |
| 6.4     | Tomar fotos post-test     | ImageUpload (multi)        | 🟡     |
| 6.5     | POST /checkout            | testDriveService.checkOut  | ✅     |
| 7.1     | Reembolsar depósito       | BillingService             | 🟡     |
| 8.1     | Solicitar feedback (1h)   | Scheduler                  | ✅     |
| 8.2     | POST /feedback            | useSubmitTestDriveFeedback | ✅     |
| 9.1     | Registrar outcome         | DealerDashboard action     | 🟡     |
| 10.1    | Seguimiento si interesado | CRM                        | 🟡     |
| 11.1    | Audit trail               | AuditService               | ✅     |

**✅ Backend Coverage: 9/11 (82%)**  
**🟡 UI Coverage: 2/11 (18%)**

**Gap principal:** Dealer-side UI (dashboard de appointments, check-in/checkout, fotos)

---

## 🔟 FLUJOS DE USUARIO

### 10.1 Happy Path: Comprador agenda y completa test drive

```
Usuario: María busca un Toyota Corolla 2023

1. /vehicles/toyota-corolla-2023-se → Ve página de detalle
2. Click "Agendar Test Drive" → Modal se abre
3. Selecciona Lunes 03/02/2026 a las 10:00 AM
4. Ingresa datos:
   - Nombre: María González
   - Cédula: 001-1234567-8
   - Licencia: RD-SD-654321-12
   - Vencimiento: 2028-06-15
   - Sube foto de licencia
5. Revisa confirmación, acepta términos
6. Click "Confirmar reserva"
7. ✅ Reserva creada exitosamente
8. Recibe email de confirmación
9. Recibe SMS 24h antes: "Recuerda tu test drive mañana a las 10:00 AM"
10. Recibe SMS 2h antes: "Tu test drive es en 2 horas. Trae tu licencia y cédula."

-- DÍA DEL TEST DRIVE --

11. María llega al dealer AutoMax RD a las 9:50 AM
12. Vendedor confirma su identidad
13. Firma waiver en tablet
14. Check-in: Odómetro 45,230 km, toman 4 fotos
15. Prueba de manejo 30 minutos (ruta urbana)
16. Regresa al dealer
17. Check-out: Odómetro 45,248 km (18 km driven), toman 4 fotos
18. 1h después recibe email: "¿Cómo estuvo tu test drive?"
19. María llena feedback:
    - Overall: 5/5
    - Comfort: 5/5
    - Performance: 5/5
    - Interested in buying: Sí
    - Comentarios: "Me encantó, quiero hacer una oferta"
20. Lead score: 90 → HOT LEAD
21. CRM asigna a vendedor Carlos
22. Carlos llama a María esa tarde
23. María hace oferta $1,850,000
24. Dealer acepta
25. ✅ VENTA COMPLETADA
```

### 10.2 Usuario cancela test drive

```
Usuario: Pedro agendó test drive pero cambió de planes

1. Accede a /appointments
2. Ve su test drive agendado para el Sábado
3. Click "Cancelar"
4. Confirma cancelación
5. useCancelTestDrive() → PUT /testdrives/{id}/cancel
6. ✅ Cancelación exitosa
7. Recibe email: "Tu test drive ha sido cancelado"
8. Dealer recibe notificación
9. Slot queda disponible nuevamente para otros usuarios
```

### 10.3 Usuario no se presenta (No Show)

```
Scenario: Usuario agendó pero no llegó

1. Test drive agendado para 10:00 AM
2. Scheduler envía recordatorios (24h + 2h)
3. Usuario no responde
4. 10:15 AM: Usuario no ha llegado
5. Dealer dashboard marca como "No Show"
6. Status cambia a NoShow
7. Sistema envía email: "¿Qué pasó? Aún estás interesado?"
8. Lead score baja automáticamente
9. Si usuario responde: puede re-agendar
10. Si no responde en 7 días: lead archivado
```

---

## 1️⃣1️⃣ VALIDACIÓN Y TESTING

### Checklist de Validación

#### Frontend (Usuario)

- [ ] **TestDriveButton**
  - [ ] Se muestra solo si `vehicle.allowTestDrive === true`
  - [ ] Si no autenticado, redirige a `/login?redirect=...&action=testdrive`
  - [ ] Si autenticado, abre modal correctamente
  - [ ] Loading state durante operaciones
- [ ] **TestDriveCalendar**
  - [ ] Muestra días disponibles en verde
  - [ ] Deshabilita días pasados
  - [ ] Deshabilita días sin disponibilidad
  - [ ] Al seleccionar día, muestra horarios
  - [ ] Slots ocupados muestran "Already booked"
  - [ ] Dealer info muestra correctamente (nombre, dirección, duración)
- [ ] **TestDriveDriverInfo**
  - [ ] Validación de formato de cédula (001-1234567-8)
  - [ ] Upload de foto de licencia funciona
  - [ ] Preview de licencia subida
  - [ ] Validación de licencia no vencida
  - [ ] Botón "Volver" funciona
- [ ] **TestDriveConfirmation**
  - [ ] Resumen muestra datos correctos
  - [ ] Requisitos listados claramente
  - [ ] Checkboxes obligatorios antes de confirmar
  - [ ] Loading durante creación
  - [ ] Error handling si falla API
- [ ] **TestDriveSuccess**
  - [ ] Código de confirmación visible
  - [ ] Botón "Agregar a calendario" genera ICS
  - [ ] Botón "Ver detalles" redirige correctamente
- [ ] **Responsive Design**
  - [ ] Modal responsive en mobile/tablet/desktop
  - [ ] Calendario funciona en móvil
  - [ ] Formularios usables en pantallas pequeñas

#### Backend (API)

- [ ] **GET /availability**
  - [ ] Calcula slots correctamente desde config del dealer
  - [ ] Excluye slots ya reservados
  - [ ] Respeta horario de apertura/cierre
  - [ ] Respeta `minAdvanceBookingHours` (no permitir menos de 2h)
  - [ ] Respeta `maxAdvanceBookingDays` (no permitir más de 14 días)
  - [ ] Cache en Redis por 2 minutos
- [ ] **POST /testdrives**
  - [ ] Validación de datos (FluentValidation)
  - [ ] Verificar slot aún disponible (race condition con Redis lock)
  - [ ] Crear TestDrive en DB
  - [ ] Generar waiver PDF
  - [ ] Enviar confirmación email/SMS
  - [ ] Agendar recordatorios en RabbitMQ
  - [ ] Audit trail completo
- [ ] **POST /waiver/sign**
  - [ ] Guardar firma en S3
  - [ ] Generar PDF con firma embebida
  - [ ] Actualizar WaiverSigned=true
- [ ] **POST /checkin**
  - [ ] Guardar odómetro y fotos
  - [ ] Status cambia a CheckedIn
  - [ ] Timestamp preciso
- [ ] **POST /checkout**
  - [ ] Calcular km driven correctamente
  - [ ] Guardar fotos post-test
  - [ ] Status cambia a Completed
  - [ ] Agendar feedback request (1h después)
- [ ] **POST /feedback**
  - [ ] Guardar ratings y comentarios
  - [ ] Calcular lead score
  - [ ] Clasificar lead (Hot/Warm/Cold)
  - [ ] Trigger CRM follow-up si interesado

#### Integration Tests

```bash
# Test flujo completo end-to-end
describe("Test Drive E2E", () => {
  it("Usuario agenda test drive exitosamente", async () => {
    // 1. Login
    const { user, token } = await loginUser("maria@example.com");

    // 2. Ver vehículo
    const vehicle = await getVehicle("toyota-corolla-2023-se");
    expect(vehicle.allowTestDrive).toBe(true);

    // 3. Obtener disponibilidad
    const availability = await getAvailability(vehicle.id, "2026-02-01", "2026-02-14");
    expect(availability.availability.length).toBeGreaterThan(0);

    // 4. Crear test drive
    const testDrive = await createTestDrive({
      vehicleId: vehicle.id,
      scheduledDate: "2026-02-03",
      scheduledTime: "10:00",
      driverName: "María González",
      driverCedula: "001-1234567-8",
      driverLicenseNumber: "RD-SD-654321-12",
      driverLicenseExpiry: "2028-06-15",
      driverLicensePhotoUrl: "https://s3.../license.jpg",
    });

    expect(testDrive.id).toBeDefined();
    expect(testDrive.status).toBe("Pending");
    expect(testDrive.confirmationCode).toMatch(/OKLA-TD-/);

    // 5. Verificar slot ya no disponible
    const updatedAvailability = await getAvailability(vehicle.id, "2026-02-01", "2026-02-14");
    const slotReserved = updatedAvailability.availability
      .find(d => d.date === "2026-02-03")
      ?.slots.find(s => s.time === "10:00");
    expect(slotReserved?.available).toBe(false);

    // 6. Verificar email/SMS enviados
    // (mock o verificar en inbox de prueba)
  });

  it("Usuario cancela test drive", async () => {
    // ... test cancelación
  });

  it("Dealer hace check-in y check-out", async () => {
    // ... test proceso dealer-side
  });
});
```

---

## 1️⃣2️⃣ PRÓXIMOS PASOS

### Corto Plazo (Sprint Actual)

1. **✅ COMPLETADO:** Documentación de TESTDRIVE-001 (agendamiento usuario)
2. **🟡 PENDIENTE:** Componentes dealer-side para TESTDRIVE-002
   - DealerAppointmentsPage
   - AppointmentCard con acciones (Check-in, Check-out, Cancel)
   - WaiverSignature component (tablet/kiosk)
   - CheckInModal con upload de 4 fotos
   - CheckOutModal con upload de 4 fotos
3. **🟡 PENDIENTE:** TestDriveFeedbackPage (página pública con link único)
4. **🟡 PENDIENTE:** Integración con CRM para seguimiento automático

### Mediano Plazo

5. **Dashboard de Analytics para Dealers**
   - Métricas de test drives (agendados, completados, no-shows)
   - Tasa de conversión TD → Oferta → Venta
   - Promedio de km driven
   - Ratings promedio por vehículo
6. **Sistema de Rutas Aprobadas**
   - CRUD de rutas (nombre, descripción, km, mapa)
   - Asignación automática según tipo de vehículo
   - Sugerencias de rutas (urbana, highway, mixta)
7. **Depósito Opcional**
   - Integración con BillingService
   - Hold temporal en tarjeta
   - Refund automático post-checkout
8. **SMS Reminders con WhatsApp**
   - Además de SMS, enviar recordatorio por WhatsApp
   - Botón "Confirmar asistencia"
   - Botón "Cancelar" directo desde WhatsApp

### Largo Plazo

9. **GPS Tracking Opcional (opt-in)**
   - Seguimiento en tiempo real durante test drive
   - Geofencing (alerta si sale de área permitida)
   - Solo con consentimiento explícito del usuario
10. **Video Instructions**
    - Video explicativo del vehículo antes de salir
    - Tips de manejo
    - Características a probar
11. **Test Drive Packages**
    - "Weekend Test Drive" (24 horas)
    - "Multi-Vehicle Test Drive" (comparar 2-3 autos en un día)
    - "Family Test Drive" (invitar acompañantes)
12. **Virtual Test Drive (VR/360°)**
    - Para usuarios remotos
    - Video 360° del interior
    - Simulación de manejo

---

## ✅ CONCLUSIÓN

### 📊 Estado Final del Módulo

| Componente               | Estado | Coverage |
| ------------------------ | ------ | -------- |
| Backend API              | ✅     | 100%     |
| Proceso TESTDRIVE-001    | ✅     | 95%      |
| Proceso TESTDRIVE-002    | 🟡     | 82%      |
| UI Usuario (Agendamiento | ✅     | 100%     |
| UI Dealer (Ejecución)    | 🟡     | 18%      |
| **PROMEDIO MÓDULO**      | 🟡     | **79%**  |

### 🎯 Diferenciadores vs Competencia

| Feature                       | OKLA | SuperCarros | AutoMercado |
| ----------------------------- | ---- | ----------- | ----------- |
| Agendamiento online           | ✅   | ❌          | ❌          |
| Calendario disponibilidad     | ✅   | ❌          | ❌          |
| Upload licencia de conducir   | ✅   | ❌          | ❌          |
| Firma digital de waiver       | ✅   | ❌          | ❌          |
| Recordatorios automáticos     | ✅   | ❌          | ❌          |
| Fotos pre/post test           | ✅   | ❌          | ❌          |
| Feedback post-test            | ✅   | ❌          | ❌          |
| Lead scoring automático       | ✅   | ❌          | ❌          |
| Seguimiento CRM integrado     | ✅   | ❌          | ❌          |
| Dashboard dealer con métricas | 🟡   | ❌          | ❌          |

**✅ OKLA ofrece experiencia 100% digital para test drives, única en RD**

### 🚀 Impacto Esperado

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/test-drives.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser, loginAsDealer } from "../helpers/auth";

test.describe("Test Drives - Usuario", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe solicitar test drive desde detalle de vehículo", async ({
    page,
  }) => {
    await page.goto("/vehiculos/toyota-corolla-2024");

    await page.getByRole("button", { name: /agendar test drive/i }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
  });

  test("debe seleccionar fecha y hora disponible", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024");
    await page.getByRole("button", { name: /agendar test drive/i }).click();

    await page.getByRole("button", { name: /15/i }).click();
    await page.getByRole("button", { name: /10:00/i }).click();
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/solicitud enviada/i)).toBeVisible();
  });

  test("debe ver mis test drives programados", async ({ page }) => {
    await page.goto("/mi-cuenta/test-drives");

    await expect(page.getByTestId("test-drives-list")).toBeVisible();
  });
});

test.describe("Test Drives - Dealer", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe ver solicitudes de test drive", async ({ page }) => {
    await page.goto("/dealer/test-drives");

    await expect(page.getByTestId("test-drive-requests")).toBeVisible();
  });

  test("debe registrar resultado de test drive", async ({ page }) => {
    await page.goto("/dealer/test-drives");

    await page
      .getByTestId("test-drive-row")
      .first()
      .getByRole("button", { name: /registrar/i })
      .click();
    await page.fill('input[name="odometerEnd"]', "45200");
    await page.getByRole("button", { name: /guardar/i }).click();

    await expect(page.getByText(/test drive registrado/i)).toBeVisible();
  });
});
```

---

**Métricas de éxito:**

- **+35% tasa de conversión** (vs. sin test drive)
- **-50% tiempo de agendamiento** (2 min vs. 5 llamadas telefónicas)
- **100% trazabilidad** legal (waivers firmados, fotos, odómetro)
- **+92% satisfacción** (feedback promedio >4.5/5)
- **+25% leads calificados** (lead scoring automático identifica hot leads)

---

**✅ Documentación completada: Enero 29, 2026**  
**Próximo archivo:** `34-dealer-appointments-completo.md` (UI dealer-side)
