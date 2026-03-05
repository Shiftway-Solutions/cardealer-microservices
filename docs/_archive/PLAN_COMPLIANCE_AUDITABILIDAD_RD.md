# 📋 Plan de Compliance y Auditabilidad - OKLA

## Cumplimiento de Normativas República Dominicana

**Fecha:** Enero 20, 2026  
**Versión:** 1.0  
**Estado:** 📋 PLAN APROBADO

---

## 📊 RESUMEN EJECUTIVO

Este documento presenta el análisis GAP entre el sistema actual de microservicios OKLA y los requerimientos de la matriz de procesos según las normativas de República Dominicana. Se propone un plan de **12 sprints** para alcanzar compliance completo.

### Normativas Aplicables

| Ley/Norma             | Nombre                               | Aplicación en OKLA                |
| --------------------- | ------------------------------------ | --------------------------------- |
| **Ley 172-13**        | Protección de Datos Personales       | Consentimientos, ARCO, retención  |
| **Ley 155-17**        | Prevención Lavado de Activos (PLD)   | KYC, monitoreo transacciones      |
| **Ley 126-02**        | Comercio Electrónico y Firma Digital | Contratos electrónicos, evidencia |
| **Ley 358-05**        | Protección al Consumidor             | Publicidad veraz, denuncias       |
| **Ley 173-09**        | Defensa de la Competencia            | Precios, prácticas comerciales    |
| **Res. 07-2018 DGII** | Facturación Electrónica (CFE)        | NCF, XML, envío DGII              |
| **ISO 27001**         | Seguridad de la Información          | Logs, accesos, encriptación       |
| **PCI DSS**           | Seguridad en Pagos                   | Tokenización, pasarelas           |
| **ISO 10001/10002**   | Gestión de Quejas                    | Tickets, SLA, satisfacción        |

---

## 🔍 ANÁLISIS GAP: SERVICIOS EXISTENTES VS REQUERIMIENTOS

### 1. SERVICIOS EXISTENTES (Relevantes para Compliance)

| Servicio                    | Estado  | Funcionalidad Actual         | Gap Identificado                                           |
| --------------------------- | ------- | ---------------------------- | ---------------------------------------------------------- |
| **AuditService**            | 🚧 Dev  | Logs de auditoría básicos    | Falta: ARCO, retención legal, compliance específico RD     |
| **AuthService**             | ✅ Prod | Login, JWT, 2FA              | Falta: Consentimientos, verificación cédula, bloqueo legal |
| **UserService**             | ✅ Prod | CRUD usuarios, perfiles      | Falta: Derechos ARCO, anonimización, historial cambios     |
| **DealerManagementService** | 🚧 Dev  | Registro dealers, documentos | Falta: Verificación DGII/RNC, KYC completo                 |
| **InvoicingService**        | 🚧 Dev  | NCF, facturas                | Falta: Integración DGII real, NC/ND, archivo legal         |
| **BillingService**          | ✅ Prod | Stripe/Azul                  | Falta: PCI compliance docs, reconciliación auditable       |
| **ContactService**          | ✅ Prod | Mensajería                   | Falta: Encriptación E2E, archivo 5 años                    |
| **NotificationService**     | ✅ Prod | Email/SMS/Push               | Falta: Templates normativos, tracking legal                |
| **ReviewService**           | 🚧 Dev  | Reviews, moderación          | Falta: Derecho respuesta, apelación documentada            |
| **AdminService**            | 🚧 Dev  | Moderación básica            | Falta: Workflow denuncias, investigación documentada       |
| **VehiclesSaleService**     | ✅ Prod | Publicaciones                | Falta: Validación legal, detección duplicados              |

### 2. SERVICIOS FALTANTES (Nuevos Requeridos)

| Nuevo Servicio              | Prioridad  | Normativa Principal | Justificación                              |
| --------------------------- | ---------- | ------------------- | ------------------------------------------ |
| **ComplianceService**       | 🔴 CRÍTICO | Ley 155-17, 172-13  | Gestión centralizada de compliance         |
| **DataProtectionService**   | 🔴 CRÍTICO | Ley 172-13          | Gestión ARCO, consentimientos              |
| **KYCService**              | 🔴 CRÍTICO | Ley 155-17          | Verificación identidad, listas vigilancia  |
| **ContractService**         | 🟡 ALTO    | Ley 126-02          | Contratos electrónicos, firmas             |
| **EscrowService**           | 🟡 ALTO    | Ley 155-17, 126-02  | Depósitos en garantía                      |
| **DisputeService**          | 🟡 ALTO    | Ley 358-05          | Denuncias, quejas, resolución              |
| **ReportingService**        | 🟡 ALTO    | Múltiples           | Reportes regulatorios automatizados        |
| **RetentionService**        | 🟢 MEDIO   | ISO 27001           | Gestión ciclo de vida datos                |
| **DigitalSignatureService** | 🟢 MEDIO   | Ley 126-02          | Integración firma digital RD               |
| **VerificationService**     | 🟢 MEDIO   | Ley 155-17          | Verificación cédula, RNC con APIs externas |

---

## 🏗️ ARQUITECTURA PROPUESTA

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         CAPA DE COMPLIANCE                                    │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐           │
│  │ ComplianceService│  │DataProtectionSvc │  │   KYCService     │           │
│  │  (Orquestador)   │  │  (Ley 172-13)    │  │  (Ley 155-17)    │           │
│  │                  │  │                  │  │                  │           │
│  │ • Políticas      │  │ • ARCO           │  │ • Verificación   │           │
│  │ • Monitoreo      │  │ • Consentimientos│  │ • Listas PEP     │           │
│  │ • Alertas        │  │ • Retención      │  │ • Perfiles riesgo│           │
│  │ • Reportes UFC   │  │ • Anonimización  │  │ • Re-verificación│           │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘           │
│           │                     │                     │                      │
├───────────┴─────────────────────┴─────────────────────┴──────────────────────┤
│                         CAPA DE TRANSACCIONES                                 │
├──────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐           │
│  │  ContractService │  │  EscrowService   │  │  DisputeService  │           │
│  │   (Ley 126-02)   │  │   (PLD/FT)       │  │   (Ley 358-05)   │           │
│  │                  │  │                  │  │                  │           │
│  │ • Contratos      │  │ • Depósitos      │  │ • Denuncias      │           │
│  │ • Firma digital  │  │ • Liberación     │  │ • Investigación  │           │
│  │ • Archivo legal  │  │ • Devoluciones   │  │ • Resolución     │           │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘           │
│           │                     │                     │                      │
├───────────┴─────────────────────┴─────────────────────┴──────────────────────┤
│                         CAPA DE EVIDENCIA                                     │
├──────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐           │
│  │   AuditService   │  │ ReportingService │  │ RetentionService │           │
│  │   (Mejorado)     │  │  (Regulatorio)   │  │  (Ciclo Vida)    │           │
│  │                  │  │                  │  │                  │           │
│  │ • Logs inmutables│  │ • Reportes UFC   │  │ • Políticas      │           │
│  │ • Integridad     │  │ • Reportes DGII  │  │ • Archivado      │           │
│  │ • Trazabilidad   │  │ • Dashboards     │  │ • Purga legal    │           │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘           │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 📅 PLAN DE SPRINTS (12 Sprints - 24 Semanas)

### 🔴 FASE 1: FUNDAMENTOS DE COMPLIANCE (Sprints 1-3)

---

### Sprint C1: DataProtectionService - Ley 172-13

**Duración:** 2 semanas  
**Story Points:** 80  
**Prioridad:** 🔴 CRÍTICA

#### Objetivo

Implementar gestión completa de datos personales según Ley 172-13.

#### Entidades

```csharp
// Consentimiento del usuario
public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ConsentType Type { get; set; } // TermsOfService, PrivacyPolicy, Marketing, DataProcessing
    public string Version { get; set; } // "2.0.1"
    public string DocumentHash { get; set; } // SHA256 del documento aceptado
    public bool Granted { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string? RevokeReason { get; set; }
}

// Solicitud ARCO (Acceso, Rectificación, Cancelación, Oposición)
public class ARCORequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ARCOType Type { get; set; } // Access, Rectification, Cancellation, Opposition
    public string RequestNumber { get; set; } // ARCO-2026-00001
    public ARCOStatus Status { get; set; } // Received, InProgress, Completed, Rejected
    public string Description { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime Deadline { get; set; } // 30 días según Ley
    public DateTime? CompletedAt { get; set; }
    public Guid? ProcessedBy { get; set; }
    public string? Resolution { get; set; }
    public string? RejectionReason { get; set; }
    public List<ARCOAttachment> Attachments { get; set; }
}

// Historial de cambios en datos personales
public class DataChangeLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DataField { get; set; } // "Email", "Phone", "Address"
    public string OldValueHash { get; set; } // Hash del valor anterior
    public string NewValueHash { get; set; } // Hash del nuevo valor
    public string ChangedByType { get; set; } // "User", "Admin", "System"
    public Guid? ChangedById { get; set; }
    public string Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public string IpAddress { get; set; }
}
```

#### Endpoints

| Endpoint                                      | Método | Descripción                         | Auth     |
| --------------------------------------------- | ------ | ----------------------------------- | -------- |
| `/api/data-protection/consents`               | POST   | Registrar nuevo consentimiento      | ✅ User  |
| `/api/data-protection/consents/user/{userId}` | GET    | Obtener consentimientos del usuario | ✅ User  |
| `/api/data-protection/consents/{id}/revoke`   | POST   | Revocar consentimiento              | ✅ User  |
| `/api/data-protection/arco`                   | POST   | Crear solicitud ARCO                | ✅ User  |
| `/api/data-protection/arco/{id}`              | GET    | Estado de solicitud ARCO            | ✅ User  |
| `/api/data-protection/arco`                   | GET    | Listar solicitudes (Admin)          | ✅ Admin |
| `/api/data-protection/arco/{id}/process`      | POST   | Procesar solicitud ARCO             | ✅ Admin |
| `/api/data-protection/export/{userId}`        | POST   | Exportar datos del usuario (Access) | ✅ Admin |
| `/api/data-protection/anonymize/{userId}`     | POST   | Anonimizar usuario (Cancellation)   | ✅ Admin |
| `/api/data-protection/data-changes/{userId}`  | GET    | Historial de cambios                | ✅ Admin |

#### Tareas Backend

- [ ] Crear DataProtectionService con Clean Architecture
- [ ] Implementar entidades UserConsent, ARCORequest, DataChangeLog
- [ ] Configurar DbContext y migraciones
- [ ] Implementar ConsentManagementService
- [ ] Implementar ARCOProcessingService
- [ ] Implementar DataExportService (genera JSON/CSV)
- [ ] Implementar AnonymizationService
- [ ] Crear eventos RabbitMQ: ConsentGranted, ConsentRevoked, ARCORequested, ARCOCompleted
- [ ] Integrar con AuditService para trazabilidad
- [ ] Crear plantillas de notificación para flujo ARCO

#### Tareas Frontend

- [ ] Formulario de aceptación de términos y privacidad
- [ ] Panel de gestión de consentimientos en perfil
- [ ] Formulario de solicitud ARCO
- [ ] Vista de estado de solicitud ARCO
- [ ] Panel admin para gestionar solicitudes ARCO
- [ ] Botón "Descargar mis datos" en perfil

#### Tests

- [ ] Tests unitarios (ConsentService, ARCOService)
- [ ] Tests de integración con base de datos
- [ ] Tests E2E del flujo ARCO completo

#### Evidencias Generadas

- ✅ Log de consentimientos con timestamp e IP
- ✅ Historial de solicitudes ARCO
- ✅ Comprobante de procesamiento
- ✅ Archivo de datos exportados
- ✅ Certificado de anonimización

---

### Sprint C2: KYCService - Ley 155-17 (PLD/FT)

**Duración:** 2 semanas  
**Story Points:** 90  
**Prioridad:** 🔴 CRÍTICA

#### Objetivo

Implementar proceso Know Your Customer completo para prevención de lavado de activos.

#### Entidades

```csharp
// Perfil KYC del usuario
public class KYCProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public KYCLevel Level { get; set; } // Basic, Standard, Enhanced
    public KYCStatus Status { get; set; } // Pending, InReview, Verified, Rejected, Expired
    public RiskLevel RiskLevel { get; set; } // Low, Medium, High, Critical

    // Datos de identidad verificados
    public string FullName { get; set; }
    public string DocumentType { get; set; } // Cedula, Passport, RNC
    public string DocumentNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Nationality { get; set; }
    public string? Occupation { get; set; }
    public string? SourceOfFunds { get; set; }

    // Verificación
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? ExpiresAt { get; set; } // Re-verificación periódica

    // PEP (Persona Expuesta Políticamente)
    public bool IsPEP { get; set; }
    public string? PEPDetails { get; set; }

    // Listas de vigilancia
    public bool IsOnWatchlist { get; set; }
    public string? WatchlistMatch { get; set; }
    public DateTime? WatchlistCheckDate { get; set; }

    // Documentos
    public List<KYCDocument> Documents { get; set; }
    public List<KYCVerificationHistory> VerificationHistory { get; set; }
}

// Documento de verificación KYC
public class KYCDocument
{
    public Guid Id { get; set; }
    public Guid KYCProfileId { get; set; }
    public KYCDocumentType Type { get; set; } // IDFront, IDBack, ProofOfAddress, Selfie, RNC
    public string FileName { get; set; }
    public string FileUrl { get; set; }
    public string FileHash { get; set; } // Integridad
    public DocumentStatus Status { get; set; } // Pending, Approved, Rejected
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
}

// Monitoreo de transacciones
public class TransactionMonitoring
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TransactionId { get; set; }
    public string TransactionType { get; set; } // VehicleSale, ServicePayment
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public AlertLevel AlertLevel { get; set; } // None, Low, Medium, High, Critical
    public string? AlertReason { get; set; }
    public bool RequiresReview { get; set; }
    public bool ReportedToUFC { get; set; } // Unidad Financiera de Investigaciones
    public DateTime TransactionDate { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}

// Reporte a UFC (Unidad Financiera de Investigaciones)
public class UFCReport
{
    public Guid Id { get; set; }
    public string ReportNumber { get; set; } // UFC-2026-00001
    public UFCReportType Type { get; set; } // ROS (Reporte Operación Sospechosa), RTE (Reporte Transacción Efectivo)
    public Guid UserId { get; set; }
    public List<Guid> TransactionIds { get; set; }
    public string Description { get; set; }
    public decimal TotalAmount { get; set; }
    public UFCReportStatus Status { get; set; } // Draft, Submitted, Acknowledged
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? UFCAcknowledgmentNumber { get; set; }
}
```

#### Endpoints

| Endpoint                           | Método | Descripción              | Auth          |
| ---------------------------------- | ------ | ------------------------ | ------------- |
| `/api/kyc/profile`                 | POST   | Iniciar proceso KYC      | ✅ User       |
| `/api/kyc/profile/{userId}`        | GET    | Obtener perfil KYC       | ✅ User/Admin |
| `/api/kyc/documents`               | POST   | Subir documento KYC      | ✅ User       |
| `/api/kyc/documents/{id}/review`   | POST   | Revisar documento        | ✅ Compliance |
| `/api/kyc/verify/{userId}`         | POST   | Aprobar/Rechazar KYC     | ✅ Compliance |
| `/api/kyc/watchlist/check`         | POST   | Verificar contra listas  | ✅ System     |
| `/api/kyc/pep/check`               | POST   | Verificar PEP            | ✅ System     |
| `/api/kyc/monitoring/alerts`       | GET    | Alertas de transacciones | ✅ Compliance |
| `/api/kyc/monitoring/{id}/review`  | POST   | Revisar alerta           | ✅ Compliance |
| `/api/kyc/ufc/reports`             | GET    | Listar reportes UFC      | ✅ Compliance |
| `/api/kyc/ufc/reports`             | POST   | Crear reporte UFC        | ✅ Compliance |
| `/api/kyc/ufc/reports/{id}/submit` | POST   | Enviar a UFC             | ✅ Compliance |
| `/api/kyc/risk-profile/{userId}`   | GET    | Perfil de riesgo         | ✅ Compliance |

#### Reglas de Monitoreo (Ley 155-17)

```csharp
public class TransactionRules
{
    // Transacciones que requieren reporte automático
    public static decimal ReportThresholdDOP = 500000; // RD$500,000 en efectivo
    public static decimal ReportThresholdUSD = 10000;  // US$10,000 en efectivo

    // Alertas automáticas
    public static List<AlertRule> Rules = new()
    {
        new AlertRule("MULTIPLE_HIGH_VALUE", "3+ transacciones >RD$200K en 30 días", AlertLevel.High),
        new AlertRule("STRUCTURING", "Múltiples transacciones justo bajo umbral", AlertLevel.Critical),
        new AlertRule("UNUSUAL_PATTERN", "Patrón inusual para perfil del usuario", AlertLevel.Medium),
        new AlertRule("NEW_USER_HIGH_VALUE", "Usuario <30 días con transacción >RD$500K", AlertLevel.High),
        new AlertRule("PEP_TRANSACTION", "Transacción de persona políticamente expuesta", AlertLevel.High),
        new AlertRule("WATCHLIST_MATCH", "Match con lista de vigilancia", AlertLevel.Critical),
    };
}
```

#### Integraciones Externas

| Servicio                          | Propósito              | Estado         |
| --------------------------------- | ---------------------- | -------------- |
| **JCE (Junta Central Electoral)** | Validar cédulas RD     | 🔄 Planificado |
| **DGII**                          | Validar RNC            | 🔄 Planificado |
| **World-Check / Refinitiv**       | Listas PEP y sanciones | 🔄 Planificado |
| **UFC RD**                        | Envío de reportes      | 🔄 Planificado |

#### Tareas

- [ ] Crear KYCService con Clean Architecture
- [ ] Implementar entidades KYC
- [ ] Implementar DocumentVerificationService
- [ ] Implementar RiskScoringEngine
- [ ] Implementar TransactionMonitoringService
- [ ] Implementar WatchlistCheckService (mock inicial)
- [ ] Implementar UFCReportingService
- [ ] Crear dashboard de compliance officer
- [ ] Integrar con MediaService para documentos
- [ ] Crear eventos: KYCVerified, AlertGenerated, UFCReportSubmitted

---

### Sprint C3: Mejoras AuditService + ComplianceService

**Duración:** 2 semanas  
**Story Points:** 70  
**Prioridad:** 🔴 CRÍTICA

#### Objetivo

Mejorar AuditService existente y crear ComplianceService orquestador.

#### Mejoras a AuditService

```csharp
// Nuevas entidades para compliance

// Evento de compliance
public class ComplianceEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } // "KYC_VERIFICATION", "ARCO_REQUEST", "PLD_ALERT"
    public string Regulation { get; set; } // "LEY_172_13", "LEY_155_17", "RES_07_2018_DGII"
    public Guid? UserId { get; set; }
    public Guid? EntityId { get; set; }
    public string EntityType { get; set; }
    public string Description { get; set; }
    public bool IsCompliant { get; set; }
    public string? NonComplianceReason { get; set; }
    public DateTime EventDate { get; set; }
    public string? RemediationAction { get; set; }
    public DateTime? RemediationDeadline { get; set; }
    public Guid? AssignedTo { get; set; }
}

// Política de retención
public class RetentionPolicy
{
    public Guid Id { get; set; }
    public string DataType { get; set; } // "AuditLogs", "Contracts", "Invoices"
    public string Regulation { get; set; }
    public int RetentionYears { get; set; }
    public bool ArchiveBeforeDelete { get; set; }
    public string ArchiveLocation { get; set; } // "S3_COLD_STORAGE"
    public bool IsActive { get; set; }
}

// Certificado de integridad de logs
public class LogIntegrityCertificate
{
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalRecords { get; set; }
    public string MerkleRoot { get; set; } // Hash raíz del árbol Merkle
    public string PreviousCertificateHash { get; set; } // Cadena de integridad
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } // "SYSTEM"
}
```

#### ComplianceService (Nuevo)

```csharp
// Orquestador central de compliance
public class ComplianceService
{
    // Monitoreo en tiempo real
    public async Task<ComplianceStatus> GetSystemComplianceStatus();
    public async Task<List<ComplianceAlert>> GetActiveAlerts();

    // Políticas
    public async Task<List<CompliancePolicy>> GetActivePolicies();
    public async Task ApplyPolicy(Guid policyId, PolicyScope scope);

    // Reportes regulatorios
    public async Task<Report> GenerateUFCReport(DateRange period);
    public async Task<Report> GenerateDGIIReport(DateRange period);
    public async Task<Report> GenerateDataProtectionReport(DateRange period);

    // Dashboard
    public async Task<ComplianceDashboard> GetDashboard();
}

// Estado de compliance del sistema
public class ComplianceStatus
{
    public OverallStatus Status { get; set; } // Compliant, PartiallyCompliant, NonCompliant
    public List<RegulationStatus> ByRegulation { get; set; }
    public int PendingActions { get; set; }
    public int OverdueActions { get; set; }
    public DateTime LastAssessment { get; set; }
}
```

#### Tareas

- [ ] Agregar entidades de compliance a AuditService
- [ ] Implementar sistema de integridad de logs (Merkle tree)
- [ ] Implementar RetentionPolicyService
- [ ] Crear ComplianceService como orquestador
- [ ] Implementar dashboard de compliance
- [ ] Crear jobs de verificación de integridad
- [ ] Crear jobs de archivado automático
- [ ] Integrar alertas con NotificationService

---

### 🟡 FASE 2: TRANSACCIONES SEGURAS (Sprints 4-6)

---

### Sprint C4: ContractService - Ley 126-02

**Duración:** 2 semanas  
**Story Points:** 85  
**Prioridad:** 🟡 ALTA

#### Objetivo

Implementar sistema de contratos electrónicos con firma digital.

#### Entidades

```csharp
// Contrato electrónico
public class ElectronicContract
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } // CTR-2026-00001
    public ContractType Type { get; set; } // VehicleSale, ServiceAgreement, TermsOfService
    public ContractStatus Status { get; set; } // Draft, PendingSignatures, Signed, Cancelled, Expired

    // Partes
    public Guid SellerId { get; set; }
    public string SellerName { get; set; }
    public string? SellerRNC { get; set; }

    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; }
    public string? BuyerCedula { get; set; }

    // Contenido
    public string TemplateId { get; set; }
    public string Content { get; set; } // HTML renderizado
    public string ContentHash { get; set; } // SHA256 del contenido

    // Objeto del contrato
    public Guid? VehicleId { get; set; }
    public string? VehicleDescription { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }

    // Firmas
    public List<ContractSignature> Signatures { get; set; }

    // Archivos
    public string? PdfUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAt { get; set; } // Cuando todas las partes firmaron
    public DateTime? ExpiresAt { get; set; }
}

// Firma en contrato
public class ContractSignature
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid SignerId { get; set; }
    public SignerRole Role { get; set; } // Seller, Buyer, Witness, Notary
    public SignatureType Type { get; set; } // Simple, Advanced, Qualified

    // Datos de firma
    public string SignatureData { get; set; } // Base64 de firma manuscrita o hash
    public string? CertificateSerial { get; set; } // Para firma digital certificada
    public string? CertificateIssuer { get; set; }

    // Contexto
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string? Geolocation { get; set; }
    public DateTime SignedAt { get; set; }

    // Verificación
    public bool IsVerified { get; set; }
    public string VerificationHash { get; set; }
}

// Plantilla de contrato
public class ContractTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ContractType Type { get; set; }
    public string Version { get; set; }
    public string HtmlContent { get; set; } // Con placeholders {{variable}}
    public List<TemplateVariable> Variables { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresLegalReview { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
}
```

#### Endpoints

| Endpoint                       | Método | Descripción           | Auth      |
| ------------------------------ | ------ | --------------------- | --------- |
| `/api/contracts`               | POST   | Crear contrato        | ✅ User   |
| `/api/contracts/{id}`          | GET    | Obtener contrato      | ✅ User   |
| `/api/contracts/{id}/sign`     | POST   | Firmar contrato       | ✅ User   |
| `/api/contracts/{id}/pdf`      | GET    | Descargar PDF         | ✅ User   |
| `/api/contracts/{id}/verify`   | GET    | Verificar integridad  | ✅ Public |
| `/api/contracts/templates`     | GET    | Listar plantillas     | ✅ Admin  |
| `/api/contracts/templates`     | POST   | Crear plantilla       | ✅ Admin  |
| `/api/contracts/user/{userId}` | GET    | Contratos del usuario | ✅ User   |

#### Tareas

- [ ] Crear ContractService con Clean Architecture
- [ ] Implementar entidades de contratos
- [ ] Crear sistema de plantillas con variables
- [ ] Implementar generación de PDF con iText/QuestPDF
- [ ] Implementar sistema de firmas (simple primero)
- [ ] Crear verificación de integridad (hash chain)
- [ ] Integrar con MediaService para almacenamiento
- [ ] Crear eventos: ContractCreated, ContractSigned, ContractCancelled

---

### Sprint C5: EscrowService - Depósitos en Garantía

**Duración:** 2 semanas  
**Story Points:** 75  
**Prioridad:** 🟡 ALTA

#### Objetivo

Implementar sistema de depósitos en garantía para transacciones seguras.

#### Entidades

```csharp
// Cuenta de depósito en garantía
public class EscrowAccount
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } // ESC-2026-00001
    public Guid TransactionId { get; set; }
    public TransactionType TransactionType { get; set; }

    // Partes
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }

    // Montos
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public decimal Fee { get; set; } // Comisión OKLA
    public decimal NetAmount { get; set; } // Monto a liberar al vendedor

    // Estado
    public EscrowStatus Status { get; set; } // Created, Funded, InDispute, Released, Refunded, Cancelled
    public EscrowMilestone CurrentMilestone { get; set; }

    // Fechas
    public DateTime CreatedAt { get; set; }
    public DateTime? FundedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Condiciones de liberación
    public List<EscrowCondition> Conditions { get; set; }
    public List<EscrowMovement> Movements { get; set; }
}

// Condición para liberación
public class EscrowCondition
{
    public Guid Id { get; set; }
    public Guid EscrowAccountId { get; set; }
    public string Description { get; set; }
    public ConditionType Type { get; set; } // VehicleDelivery, DocumentTransfer, BuyerConfirmation
    public bool IsMet { get; set; }
    public DateTime? MetAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public string? Evidence { get; set; } // URL a foto/documento
}

// Movimiento de fondos
public class EscrowMovement
{
    public Guid Id { get; set; }
    public Guid EscrowAccountId { get; set; }
    public MovementType Type { get; set; } // Deposit, Release, Refund, Fee
    public decimal Amount { get; set; }
    public string PaymentReference { get; set; }
    public string PaymentMethod { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? Notes { get; set; }
}
```

#### Flujo de Escrow

```
1. COMPRADOR crea escrow → Estado: Created
2. COMPRADOR deposita fondos → Estado: Funded
3. VENDEDOR entrega vehículo → Condición: VehicleDelivery ✓
4. VENDEDOR transfiere documentos → Condición: DocumentTransfer ✓
5. COMPRADOR confirma recepción → Condición: BuyerConfirmation ✓
6. SISTEMA libera fondos → Estado: Released
   (automático si todas las condiciones se cumplen)

Alternativas:
- DISPUTA → Estado: InDispute → DisputeService maneja
- TIMEOUT → Estado: Refunded (si vendedor no cumple)
- CANCELACIÓN mutua → Estado: Refunded
```

#### Tareas

- [ ] Crear EscrowService con Clean Architecture
- [ ] Implementar flujo de estados
- [ ] Integrar con BillingService (Stripe/Azul)
- [ ] Implementar sistema de condiciones
- [ ] Crear timeline de escrow para usuarios
- [ ] Integrar con DisputeService para conflictos
- [ ] Crear eventos y notificaciones

---

### Sprint C6: DisputeService - Ley 358-05

**Duración:** 2 semanas  
**Story Points:** 80  
**Prioridad:** 🟡 ALTA

#### Objetivo

Implementar sistema de denuncias y resolución de disputas.

#### Entidades

```csharp
// Denuncia/Disputa
public class Dispute
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } // DIS-2026-00001
    public DisputeType Type { get; set; } // Fraud, Misrepresentation, NonDelivery, QualityIssue, Other
    public DisputeStatus Status { get; set; } // Open, UnderInvestigation, PendingResponse, Mediation, Resolved, Escalated
    public DisputePriority Priority { get; set; } // Low, Medium, High, Critical

    // Partes
    public Guid ComplainantId { get; set; } // Quien denuncia
    public string ComplainantName { get; set; }
    public Guid RespondentId { get; set; } // Denunciado
    public string RespondentName { get; set; }

    // Contexto
    public Guid? TransactionId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? EscrowAccountId { get; set; }

    // Contenido
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal? ClaimedAmount { get; set; }

    // Proceso
    public Guid? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ResponseDeadline { get; set; } // 5 días hábiles
    public DateTime ResolutionDeadline { get; set; } // 15 días hábiles
    public DateTime? ResolvedAt { get; set; }

    // Resolución
    public DisputeResolution? Resolution { get; set; } // InFavorComplainant, InFavorRespondent, MutualAgreement, Dismissed
    public string? ResolutionDetails { get; set; }
    public decimal? AwardedAmount { get; set; }

    // Evidencias
    public List<DisputeEvidence> Evidences { get; set; }
    public List<DisputeMessage> Messages { get; set; }
    public List<DisputeAction> ActionHistory { get; set; }
}

// Evidencia de disputa
public class DisputeEvidence
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public Guid SubmittedBy { get; set; }
    public EvidenceType Type { get; set; } // Document, Photo, Screenshot, Video, ChatLog
    public string Title { get; set; }
    public string Description { get; set; }
    public string FileUrl { get; set; }
    public DateTime SubmittedAt { get; set; }
}

// Acción en disputa
public class DisputeAction
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public ActionType Type { get; set; } // Created, Assigned, ResponseReceived, EvidenceAdded, StatusChanged, Resolved
    public Guid PerformedBy { get; set; }
    public string Description { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime PerformedAt { get; set; }
}
```

#### Endpoints

| Endpoint                      | Método | Descripción              | Auth     |
| ----------------------------- | ------ | ------------------------ | -------- |
| `/api/disputes`               | POST   | Crear denuncia           | ✅ User  |
| `/api/disputes/{id}`          | GET    | Ver denuncia             | ✅ User  |
| `/api/disputes/{id}/respond`  | POST   | Responder a denuncia     | ✅ User  |
| `/api/disputes/{id}/evidence` | POST   | Agregar evidencia        | ✅ User  |
| `/api/disputes/{id}/messages` | POST   | Enviar mensaje           | ✅ User  |
| `/api/disputes`               | GET    | Listar denuncias (Admin) | ✅ Admin |
| `/api/disputes/{id}/assign`   | POST   | Asignar investigador     | ✅ Admin |
| `/api/disputes/{id}/resolve`  | POST   | Resolver denuncia        | ✅ Admin |
| `/api/disputes/{id}/appeal`   | POST   | Apelar resolución        | ✅ User  |
| `/api/disputes/stats`         | GET    | Estadísticas             | ✅ Admin |

#### Tareas

- [ ] Crear DisputeService con Clean Architecture
- [ ] Implementar workflow de estados
- [ ] Crear sistema de evidencias
- [ ] Implementar canal de comunicación
- [ ] Integrar con EscrowService
- [ ] Crear panel de investigador
- [ ] Implementar sistema de apelación
- [ ] Generar reportes para PROCONSUMIDOR

---

### 🟢 FASE 3: FACTURACIÓN Y REPORTES (Sprints 7-9)

---

### Sprint C7: Mejoras InvoicingService - DGII

**Duración:** 2 semanas  
**Story Points:** 85  
**Prioridad:** 🟡 ALTA

#### Objetivo

Completar integración con DGII para facturación electrónica.

#### Nuevas Funcionalidades

```csharp
// Configuración CFDI/NCF
public class CFDIConfiguration
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }

    // Datos fiscales
    public string RNC { get; set; }
    public string BusinessName { get; set; }
    public string LegalName { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }

    // Certificado digital DGII
    public byte[] Certificate { get; set; }
    public string CertificateSerial { get; set; }
    public DateTime CertificateValidFrom { get; set; }
    public DateTime CertificateValidTo { get; set; }

    // Secuencias NCF autorizadas
    public List<NCFSequence> NCFSequences { get; set; }
}

// XML para DGII (formato oficial)
public class DGIIInvoiceXML
{
    // Genera XML según especificación DGII
    public string GenerateXML(Invoice invoice);
    public string SignXML(string xml, byte[] certificate);
    public bool ValidateXML(string xml);
}

// Libro de ventas
public class SalesLedger
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<SalesLedgerEntry> Entries { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? DGIISubmissionId { get; set; }
}
```

#### Integración DGII

| Proceso          | Endpoint DGII                          | Estado         |
| ---------------- | -------------------------------------- | -------------- |
| Validar RNC      | `https://dgii.gov.do/api/rnc/validate` | 🔄 Planificado |
| Enviar CFE       | `https://dgii.gov.do/api/cfe/submit`   | 🔄 Planificado |
| Consultar estado | `https://dgii.gov.do/api/cfe/status`   | 🔄 Planificado |
| Anular CFE       | `https://dgii.gov.do/api/cfe/cancel`   | 🔄 Planificado |

#### Tareas

- [ ] Implementar generación de XML según formato DGII
- [ ] Implementar firma digital de XML
- [ ] Crear NCFSequenceManager (control de numeración)
- [ ] Implementar libro de ventas automático
- [ ] Crear servicio de envío a DGII (mock inicial)
- [ ] Implementar notas de crédito/débito
- [ ] Crear reportes 606, 607, 608

---

### Sprint C8: ReportingService - Reportes Regulatorios

**Duración:** 2 semanas  
**Story Points:** 70  
**Prioridad:** 🟡 ALTA

#### Objetivo

Centralizar generación de reportes para autoridades.

#### Tipos de Reportes

```csharp
public enum ReportType
{
    // DGII
    Report_606_Purchases,      // Compras
    Report_607_Sales,          // Ventas
    Report_608_Cancellations,  // Anulaciones
    Report_IT1,                // ITBIS mensual

    // UFC (Ley 155-17)
    Report_ROS,                // Reporte Operación Sospechosa
    Report_RTE,                // Reporte Transacción Efectivo
    Report_Monthly_PLD,        // Monitoreo mensual

    // Datos Personales (Ley 172-13)
    Report_ARCO_Summary,       // Resumen solicitudes ARCO
    Report_Data_Breaches,      // Incidentes de seguridad
    Report_Consents,           // Estado de consentimientos

    // PROCONSUMIDOR (Ley 358-05)
    Report_Complaints,         // Denuncias recibidas
    Report_Resolutions,        // Resoluciones

    // Internos
    Report_Compliance_Status,  // Estado general compliance
    Report_KYC_Summary,        // Resumen verificaciones
    Report_Transactions,       // Transacciones del período
}

public class ScheduledReport
{
    public Guid Id { get; set; }
    public ReportType Type { get; set; }
    public string Name { get; set; }
    public ReportFrequency Frequency { get; set; } // Daily, Weekly, Monthly, Quarterly
    public string CronExpression { get; set; }
    public List<string> Recipients { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public DateTime? NextScheduledAt { get; set; }
}
```

#### Tareas

- [ ] Crear ReportingService con Clean Architecture
- [ ] Implementar generadores para cada tipo de reporte
- [ ] Crear sistema de programación de reportes
- [ ] Implementar exportación PDF/Excel/XML
- [ ] Crear dashboard de reportes
- [ ] Integrar con NotificationService para envío
- [ ] Implementar almacenamiento histórico

---

### Sprint C9: RetentionService + DigitalSignatureService

**Duración:** 2 semanas  
**Story Points:** 65  
**Prioridad:** 🟢 MEDIO

#### Objetivo

Implementar gestión de ciclo de vida de datos y firma digital.

#### RetentionService

```csharp
// Políticas de retención por tipo de dato
public static class RetentionPolicies
{
    public static readonly Dictionary<string, int> Years = new()
    {
        ["AuditLogs"] = 5,           // ISO 27001
        ["Contracts"] = 10,          // Ley 126-02
        ["Invoices"] = 10,           // DGII
        ["UserData"] = 5,            // Ley 172-13 (después de cierre cuenta)
        ["Messages"] = 5,            // Ley 126-02
        ["KYCDocuments"] = 10,       // Ley 155-17
        ["UFCReports"] = 10,         // Ley 155-17
        ["Disputes"] = 5,            // Ley 358-05
        ["Transactions"] = 10,       // General
    };
}

public class RetentionJob
{
    public async Task ProcessRetention()
    {
        foreach (var policy in ActivePolicies)
        {
            var cutoffDate = DateTime.UtcNow.AddYears(-policy.RetentionYears);

            if (policy.ArchiveBeforeDelete)
            {
                await ArchiveRecords(policy.DataType, cutoffDate);
            }

            await PurgeRecords(policy.DataType, cutoffDate);
        }
    }
}
```

#### DigitalSignatureService

```csharp
// Integración con proveedores de firma digital RD
public interface IDigitalSignatureProvider
{
    Task<SignatureResult> SignDocument(byte[] document, SignerInfo signer);
    Task<VerificationResult> VerifySignature(byte[] signedDocument);
    Task<CertificateInfo> ValidateCertificate(string certificateSerial);
}

// Proveedores soportados
public enum SignatureProvider
{
    INDOTEL,       // Certificados de INDOTEL
    OGTIC,         // Gobierno digital
    PrivateCA,     // Autoridades privadas autorizadas
    Simple         // Firma simple (captura + timestamp)
}
```

---

### 🔵 FASE 4: INTEGRACIÓN Y TESTING (Sprints 10-12)

---

### Sprint C10: Mejoras a Servicios Existentes

**Duración:** 2 semanas  
**Story Points:** 75  
**Prioridad:** 🟢 MEDIO

#### Mejoras por Servicio

**AuthService:**

- [ ] Agregar logging de intentos de acceso (Ley 172-13)
- [ ] Implementar bloqueo por orden judicial
- [ ] Agregar aceptación de términos en registro
- [ ] Implementar verificación de cédula básica

**UserService:**

- [ ] Agregar campo de historial de cambios
- [ ] Implementar soft delete con anonimización
- [ ] Agregar campos de verificación de identidad
- [ ] Crear endpoint de exportación de datos

**ContactService:**

- [ ] Implementar encriptación E2E de mensajes
- [ ] Agregar metadatos de retención
- [ ] Crear archivo automático después de 5 años

**ReviewService:**

- [ ] Agregar sistema de derecho de respuesta
- [ ] Implementar proceso de apelación documentado
- [ ] Crear log de moderación

**VehiclesSaleService:**

- [ ] Agregar validación de precios anómalos
- [ ] Implementar detección de duplicados
- [ ] Crear log de cambios en publicaciones

---

### Sprint C11: Verificación Externa + Integraciones

**Duración:** 2 semanas  
**Story Points:** 70  
**Prioridad:** 🟢 MEDIO

#### VerificationService

```csharp
// Integración con APIs gubernamentales RD
public interface IVerificationProvider
{
    // JCE - Cédulas
    Task<CedulaVerificationResult> VerifyCedula(string cedula);

    // DGII - RNC
    Task<RNCVerificationResult> VerifyRNC(string rnc);

    // TSS - Seguridad Social
    Task<TSSVerificationResult> VerifyTSS(string cedula);
}

// Resultado de verificación
public class CedulaVerificationResult
{
    public bool IsValid { get; set; }
    public string FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Status { get; set; } // Vigente, Vencida, Fallecido
    public DateTime VerifiedAt { get; set; }
}
```

#### Tareas

- [ ] Investigar APIs disponibles de JCE
- [ ] Investigar API DGII para RNC
- [ ] Crear mocks para desarrollo
- [ ] Implementar caché de verificaciones
- [ ] Crear fallback manual si APIs no disponibles

---

### Sprint C12: Testing de Compliance + Documentación

**Duración:** 2 semanas  
**Story Points:** 60  
**Prioridad:** 🟢 MEDIO

#### Testing

- [ ] Tests de flujo ARCO completo
- [ ] Tests de proceso KYC
- [ ] Tests de generación de reportes
- [ ] Tests de integridad de logs
- [ ] Tests de retención de datos
- [ ] Tests de contratos electrónicos
- [ ] Tests de escrow
- [ ] Tests de disputas

#### Documentación

- [ ] Manual de cumplimiento por normativa
- [ ] Procedimientos operativos estándar
- [ ] Guía de respuesta a auditorías
- [ ] Documentación de APIs
- [ ] Guía de configuración inicial

---

## 📊 RESUMEN DE SPRINTS

| Sprint  | Nombre                              | Duración | SP  | Servicios        |
| ------- | ----------------------------------- | -------- | --- | ---------------- |
| **C1**  | DataProtectionService               | 2 sem    | 80  | Nuevo            |
| **C2**  | KYCService                          | 2 sem    | 90  | Nuevo            |
| **C3**  | AuditService + ComplianceService    | 2 sem    | 70  | Mejorado + Nuevo |
| **C4**  | ContractService                     | 2 sem    | 85  | Nuevo            |
| **C5**  | EscrowService                       | 2 sem    | 75  | Nuevo            |
| **C6**  | DisputeService                      | 2 sem    | 80  | Nuevo            |
| **C7**  | InvoicingService DGII               | 2 sem    | 85  | Mejorado         |
| **C8**  | ReportingService                    | 2 sem    | 70  | Nuevo            |
| **C9**  | RetentionService + DigitalSignature | 2 sem    | 65  | Nuevo            |
| **C10** | Mejoras Servicios Existentes        | 2 sem    | 75  | Mejorados        |
| **C11** | VerificationService                 | 2 sem    | 70  | Nuevo            |
| **C12** | Testing + Documentación             | 2 sem    | 60  | Transversal      |

**Total:** 24 semanas (~6 meses)  
**Story Points Total:** 905 SP

---

## 📈 MATRIZ DE TRAZABILIDAD

| Proceso de Matriz           | Sprint | Servicio                 | Estado |
| --------------------------- | ------ | ------------------------ | ------ |
| 1.1 Registro Usuario        | C1, C2 | DataProtection, KYC      | 🔄     |
| 1.1 Registro Dealer         | C2     | KYC                      | 🔄     |
| 1.2 Actualización Datos     | C1     | DataProtection           | 🔄     |
| 1.2 Ejercicio ARCO          | C1     | DataProtection           | 🔄     |
| 2.1 Publicación Vehículos   | C10    | VehiclesSaleService      | 🔄     |
| 2.1 Validación Anuncios     | C10    | AdminService             | 🔄     |
| 3.1 Sistema Mensajería      | C10    | ContactService           | 🔄     |
| 3.2 Generación Oferta       | C4, C5 | Contract, Escrow         | 🔄     |
| 3.2 Contrato Electrónico    | C4     | ContractService          | 🔄     |
| 3.2 Depósito Garantía       | C5     | EscrowService            | 🔄     |
| 4.1 Sistema Pagos           | C7     | InvoicingService         | 🔄     |
| 4.2 Facturación CFE         | C7     | InvoicingService         | 🔄     |
| 4.2 Gestión NC/ND           | C7     | InvoicingService         | 🔄     |
| 5.1 Calificaciones          | C10    | ReviewService            | 🔄     |
| 5.2 Verificación Vendedores | C2     | KYCService               | 🔄     |
| 5.2 Sistema Denuncias       | C6     | DisputeService           | 🔄     |
| 6.1 Sistema Tickets         | ✅     | SupportService existente | ✅     |
| 7.1 Monitoreo Seguridad     | C3     | AuditService             | 🔄     |
| 7.2 Cumplimiento PLD/FT     | C2, C3 | KYC, Compliance          | 🔄     |
| 8.1 Reportes Usuarios       | C8     | ReportingService         | 🔄     |
| 8.2 Reportes Regulatorios   | C7, C8 | Invoicing, Reporting     | 🔄     |

---

## 🚀 PRÓXIMOS PASOS

1. **Semana 1:** Revisar y aprobar este plan
2. **Semana 2:** Iniciar Sprint C1 (DataProtectionService)
3. **Mensual:** Revisión de progreso con equipo legal
4. **Trimestral:** Auditoría interna de compliance

---

## 📞 CONTACTOS

| Rol                    | Responsable    | Contacto               |
| ---------------------- | -------------- | ---------------------- |
| **Product Owner**      | [Nombre]       | email@okla.com.do      |
| **Tech Lead**          | Gregory Moreno | gmoreno@okla.com.do    |
| **Legal**              | [Abogado]      | legal@okla.com.do      |
| **Compliance Officer** | [Nombre]       | compliance@okla.com.do |

---

_Documento generado: Enero 20, 2026_  
_Próxima revisión: Febrero 20, 2026_
