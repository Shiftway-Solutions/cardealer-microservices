# 🏛️ Arquitectura de Microservicios de Compliance - OKLA

## República Dominicana - Marco Regulatorio Completo

**Fecha:** Enero 20, 2026  
**Versión:** 1.0  
**Estado:** ✅ 895 Tests Passing

---

## 📋 Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Diagrama de Arquitectura](#diagrama-de-arquitectura)
3. [Diagrama de Comunicaciones](#diagrama-de-comunicaciones)
4. [Microservicios de Compliance](#microservicios-de-compliance)
5. [Integraciones Externas](#integraciones-externas)
6. [Flujos de Datos](#flujos-de-datos)
7. [Seguridad y Cumplimiento](#seguridad-y-cumplimiento)

---

## 🎯 Resumen Ejecutivo

OKLA implementa **12 microservicios de compliance** para cumplir con las regulaciones dominicanas en el sector automotriz. Cada microservicio está diseñado con **Clean Architecture** y cubre una ley o normativa específica.

### Stack Tecnológico

| Componente     | Tecnología               |
| -------------- | ------------------------ |
| Backend        | .NET 8.0 LTS             |
| Base de Datos  | PostgreSQL 16+           |
| Message Broker | RabbitMQ 3.12+           |
| Cache          | Redis 7+                 |
| API Gateway    | Ocelot                   |
| Testing        | xUnit + FluentAssertions |

### Cobertura de Tests

| Total Tests | Passing        | Coverage                |
| ----------- | -------------- | ----------------------- |
| **895**     | **895 (100%)** | Domain + Business Logic |

---

## 🏗️ Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    OKLA PLATFORM                                         │
│                         Marketplace de Vehículos - República Dominicana                  │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    API GATEWAY                                           │
│                                     (Ocelot)                                             │
│                              https://api.okla.com.do                                     │
└─────────────────────────────────────────────────────────────────────────────────────────┘
                                           │
           ┌───────────────────────────────┼───────────────────────────────┐
           │                               │                               │
           ▼                               ▼                               ▼
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│   CORE SERVICES     │     │  COMPLIANCE LAYER   │     │   INFRASTRUCTURE    │
│                     │     │                     │     │                     │
│ • AuthService       │     │ • C1-C12 Services   │     │ • PostgreSQL        │
│ • UserService       │     │ • 12 Microservicios │     │ • Redis             │
│ • VehiclesSaleServ  │     │ • 895 Tests         │     │ • RabbitMQ          │
│ • MediaService      │     │                     │     │ • S3 Storage        │
│ • BillingService    │     │                     │     │                     │
└─────────────────────┘     └─────────────────────┘     └─────────────────────┘
```

---

## 🔄 Diagrama de Comunicaciones

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                        COMPLIANCE MICROSERVICES COMMUNICATION MAP                        │
└─────────────────────────────────────────────────────────────────────────────────────────┘

                              ┌─────────────────────┐
                              │   API GATEWAY       │
                              │     (Ocelot)        │
                              └──────────┬──────────┘
                                         │
         ┌───────────────────────────────┼───────────────────────────────┐
         │                               │                               │
         ▼                               ▼                               ▼
┌─────────────────┐           ┌─────────────────┐           ┌─────────────────┐
│  TRIBUTARIO     │           │   PROTECCIÓN    │           │   OPERACIONES   │
│                 │           │                 │           │                 │
│ ┌─────────────┐ │           │ ┌─────────────┐ │           │ ┌─────────────┐ │
│ │C1 Tax      ◄┼─┼───────────┼─┼►C2 DataProt │ │           │ │C6 Contract  │ │
│ │Compliance   │ │           │ └──────┬──────┘ │           │ └──────┬──────┘ │
│ └──────┬──────┘ │           │        │        │           │        │        │
│        │        │           │ ┌──────▼──────┐ │           │ ┌──────▼──────┐ │
│        │        │           │ │C4 Consumer  │ │           │ │C7 Escrow    │ │
│        │        │           │ │Protection   │ │           │ └──────┬──────┘ │
│        │        │           │ └──────┬──────┘ │           │        │        │
└────────┼────────┘           └────────┼────────┘           │ ┌──────▼──────┐ │
         │                             │                    │ │C8 Dispute   │ │
         │                             │                    │ └─────────────┘ │
         │                             │                    └─────────────────┘
         │                             │
         ▼                             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MESSAGE BUS (RabbitMQ)                             │
│                                                                              │
│  Exchanges:                                                                  │
│  • compliance.events    • tax.declarations    • consumer.complaints          │
│  • aml.alerts          • contract.signatures  • audit.logs                   │
└──────────────────────────────────┬───────────────────────────────────────────┘
                                   │
         ┌─────────────────────────┼─────────────────────────┐
         │                         │                         │
         ▼                         ▼                         ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   SEGURIDAD     │     │   AUDITORÍA     │     │  INTEGRACIÓN    │
│                 │     │                 │     │                 │
│ ┌─────────────┐ │     │ ┌─────────────┐ │     │ ┌─────────────┐ │
│ │C3 AML       │ │     │ │C9 Audit     │ │     │ │C12 Compli-  │ │
│ │(PLD)        │ │     │ │Service      │ │     │ │ance Integr. │ │
│ └──────┬──────┘ │     │ └──────┬──────┘ │     │ └──────┬──────┘ │
│        │        │     │        │        │     │        │        │
│ ┌──────▼──────┐ │     │ ┌──────▼──────┐ │     │        │        │
│ │C5 Compli-   │ │     │ │C10 Legal    │ │     │        ▼        │
│ │ance Service │ │     │ │Document     │ │     │ ┌─────────────┐ │
│ └─────────────┘ │     │ └──────┬──────┘ │     │ │  EXTERNAL   │ │
│                 │     │        │        │     │ │  SYSTEMS    │ │
└─────────────────┘     │ ┌──────▼──────┐ │     │ │             │ │
                        │ │C11 Regulat. │ │     │ │ • DGII      │ │
                        │ │Alert        │ │     │ │ • JCE       │ │
                        │ └─────────────┘ │     │ │ • UAF       │ │
                        └─────────────────┘     │ │ • INDOTEL   │ │
                                                │ └─────────────┘ │
                                                └─────────────────┘
```

---

## 📦 Microservicios de Compliance

### C1: TaxComplianceService (Ley 11-92)

**Propósito:** Cumplimiento tributario según Código Tributario de RD

```
┌─────────────────────────────────────────────────────────────────┐
│                    TaxComplianceService                          │
│                       Ley 11-92                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── DeclarationType             ├── TaxDeclaration              │
│  │   • ITBIS (18%)               │   • Id, TaxpayerId, Rnc       │
│  │   • ISR                       │   • DeclarationType, Period   │
│  │   • Reporte606               │   • GrossAmount, TaxAmount    │
│  │   • Reporte607               │   • Status, DueDate           │
│  │   • Reporte608               │                                │
│  │   • Reporte609               ├── Taxpayer                     │
│  │   • IR17, IT1, IR1           │   • Rnc, BusinessName          │
│  │                               │   • TaxpayerType               │
│  ├── DeclarationStatus           │                                │
│  │   • Draft, Pending            ├── TaxPayment                   │
│  │   • Submitted, Accepted       │   • Amount, Status             │
│  │   • Rejected, Paid            │   • BankReference              │
│  │                               │                                │
│  ├── TaxpayerType                ├── NcfSequence                  │
│  │   • Individual (9 dígitos)    │   • Serie, CurrentNumber       │
│  │   • Company (11 dígitos)      │   • ExpirationDate             │
│  │   • LargeContributor          │                                │
│  │                               ├── Reporte606Item               │
│  ├── NcfType                     │   • RncCedula, Ncf             │
│  │   • B01-B16, E31-E32          │   • MontoFacturado, Itbis      │
│  │                               │                                │
│  ├── PaymentStatus               ├── Reporte607Item               │
│  │   • Pending, Paid, Failed     │   • TipoIngreso, Ncf           │
│  │                               │   • MontoFacturado             │
│  └── WithholdingType             │                                │
│      • ISR10, ISR27              └── TaxWithholding               │
│      • ITBIS30, ITBIS100             • SupplierRnc, Rate          │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 35 ✅                    INTEGRACIÓN: DGII API           │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/tax/declarations` - Crear declaración
- `GET /api/tax/declarations/{period}` - Obtener por período
- `POST /api/tax/declarations/{id}/submit` - Enviar a DGII
- `GET /api/tax/ncf/next/{type}` - Obtener siguiente NCF
- `POST /api/tax/withholdings` - Registrar retención

---

### C2: DataProtectionService (Ley 172-13)

**Propósito:** Protección de datos personales según Ley 172-13

```
┌─────────────────────────────────────────────────────────────────┐
│                   DataProtectionService                          │
│                       Ley 172-13                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── ConsentType                 ├── DataSubject                 │
│  │   • Marketing                 │   • FullName, Email           │
│  │   • Analytics                 │   • IdentificationNumber      │
│  │   • ThirdPartySharing         │   • ConsentHistory            │
│  │   • DataProcessing            │                                │
│  │                               ├── Consent                      │
│  ├── ConsentStatus               │   • ConsentType, Status        │
│  │   • Active, Revoked           │   • GrantedAt, RevokedAt       │
│  │   • Expired                   │   • IpAddress, UserAgent       │
│  │                               │                                │
│  ├── DataRequestType             ├── DataRequest                  │
│  │   • Access (ARCO)             │   • RequestType, Status        │
│  │   • Rectification             │   • ResponseDueDate            │
│  │   • Cancellation              │                                │
│  │   • Opposition                ├── DataProcessingActivity       │
│  │                               │   • Purpose, LegalBasis        │
│  ├── DataRequestStatus           │   • DataCategories             │
│  │   • Pending, InProgress       │                                │
│  │   • Completed, Rejected       ├── DataBreach                   │
│  │                               │   • Description, Severity      │
│  └── BreachSeverity              │   • AffectedSubjects           │
│      • Low, Medium               │   • NotifiedToAuthority        │
│      • High, Critical            │                                │
│                                  └── RetentionPolicy              │
│                                      • DataType, RetentionDays    │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 68 ✅                    INTEGRACIÓN: INDOTEL            │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/data-protection/consent` - Registrar consentimiento
- `DELETE /api/data-protection/consent/{id}` - Revocar consentimiento
- `POST /api/data-protection/requests` - Solicitud ARCO
- `GET /api/data-protection/requests/{id}/status` - Estado solicitud
- `POST /api/data-protection/breaches` - Reportar brecha

---

### C3: AntiMoneyLaunderingService (Ley 155-17)

**Propósito:** Prevención de Lavado de Activos (PLD/AML)

```
┌─────────────────────────────────────────────────────────────────┐
│               AntiMoneyLaunderingService                         │
│                       Ley 155-17                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── IdentificationType          ├── Customer                    │
│  │   • Cedula, Passport          │   • FullName, Identification  │
│  │   • Rnc, ForeignId            │   • RiskLevel, KycStatus      │
│  │                               │   • IsPep, PepCategory        │
│  ├── RiskLevel                   │   • IsOnSanctionsList         │
│  │   • Low, Medium               │                                │
│  │   • High, Prohibited          ├── Transaction                  │
│  │                               │   • Amount, Currency           │
│  ├── KycStatus                   │   • IsAboveThreshold          │
│  │   • Pending, InProgress       │   • IsSuspicious              │
│  │   • Verified, Rejected        │                                │
│  │   • Expired                   ├── SuspiciousActivityReport    │
│  │                               │   • ReportNumber, ReportType   │
│  ├── RosReportType               │   • SuspicionIndicators       │
│  │   • SuspiciousTransaction     │   • Status, SubmittedToUafAt  │
│  │   • UnusualPattern            │                                │
│  │   • StructuredTransaction     ├── AmlAlert                     │
│  │   • PepRelated                │   • AlertType, Status          │
│  │   • TerrorismFinancing        │   • RiskScore                  │
│  │                               │                                │
│  ├── RosStatus                   ├── KycDocument                  │
│  │   • Draft, Submitted          │   • DocumentType, IsVerified   │
│  │   • Acknowledged              │                                │
│  │                               ├── SanctionsList                │
│  ├── PepCategory                 │   • ListName, Source           │
│  │   • NationalGovernment        │   • (OFAC, UN, EU)             │
│  │   • LocalGovernment           │                                │
│  │   • Judicial, Military        └── SanctionedEntity             │
│  │   • StateOwned                    • Name, AlternateNames       │
│  │   • InternationalOrg              • SanctionReason             │
│  │   • PoliticalParty                                             │
│  │                                                                │
│  ├── AlertType                   UMBRALES:                       │
│  │   • ThresholdExceeded         • USD 10,000 → Reporte           │
│  │   • StructuringDetected       • Pitufeo → Alertas múltiples    │
│  │   • SanctionsMatch            • PEP → Riesgo Alto automático   │
│  │   • PepIdentified             • 15 días hábiles → UAF          │
│  │                                                                │
│  └── AlertStatus                                                  │
│      • New, UnderInvestigation                                    │
│      • Escalated, Confirmed                                       │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 37 ✅                    INTEGRACIÓN: UAF (Unidad de     │
│                                  Análisis Financiero)            │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/aml/customers` - Registrar cliente con KYC
- `POST /api/aml/transactions` - Registrar transacción
- `GET /api/aml/customers/{id}/risk` - Evaluar riesgo
- `POST /api/aml/ros` - Crear ROS
- `POST /api/aml/ros/{id}/submit` - Enviar a UAF
- `GET /api/aml/sanctions/check/{name}` - Verificar sanciones
- `GET /api/aml/pep/check/{id}` - Verificar PEP

---

### C4: ConsumerProtectionService (Ley 358-05)

**Propósito:** Protección de derechos del consumidor

```
┌─────────────────────────────────────────────────────────────────┐
│                ConsumerProtectionService                         │
│                       Ley 358-05                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── WarrantyType                ├── Warranty                    │
│  │   • Legal (6 meses mín)       │   • ProductId, WarrantyType   │
│  │   • Extended                  │   • StartDate, EndDate        │
│  │   • Manufacturer              │   • Status                    │
│  │   • Distributor               │                                │
│  │   • Commercial                ├── WarrantyClaim                │
│  │                               │   • ClaimNumber, Status        │
│  ├── WarrantyStatus              │   • Resolution                 │
│  │   • Active, Expired           │                                │
│  │   • Claimed, Voided           ├── Complaint                    │
│  │   • Fulfilled                 │   • ComplaintNumber, Type      │
│  │                               │   • Description, Priority      │
│  ├── ComplaintType               │   • ResponseDueDate (15 días)  │
│  │   • DefectiveProduct          │   • IsEscalatedToProConsumidor │
│  │   • WarrantyIssue             │                                │
│  │   • MisleadingAdvertising     ├── ComplaintEvidence            │
│  │   • PriceDispute              │   • FileName, FilePath         │
│  │   • RefundRequest             │                                │
│  │   • ServiceQuality            ├── Mediation                    │
│  │   • ContractBreach            │   • MediationNumber, Status    │
│  │                               │   • ScheduledDate              │
│  ├── ComplaintStatus             │   • AgreementSummary           │
│  │   • Received, UnderReview     │                                │
│  │   • InMediation, Resolved     ├── Consumer                     │
│  │   • Escalated, Closed         │   • FullName, Email            │
│  │                               │   • IdentificationNumber       │
│  ├── ComplaintPriority           │                                │
│  │   • Low, Medium               └── ProductDisclosure            │
│  │   • High, Urgent                  • ProductName, Price         │
│  │                                   • PriceIncludesITBIS         │
│  ├── MediationStatus                 • WarrantyMonths             │
│  │   • Scheduled, InProgress         • MeetsDisclosureRequirements│
│  │   • Agreement, NoAgreement                                     │
│  │   • Cancelled                 PLAZOS LEGALES:                  │
│  │                               • 7 días → Derecho de retracto   │
│  └── ResolutionType              • 15 días → Respuesta obligatoria│
│      • FullRefund, PartialRefund • 6 meses → Garantía mínima      │
│      • ProductReplacement                                         │
│      • Repair, Compensation                                       │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 39 ✅                    INTEGRACIÓN: Pro-Consumidor     │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/consumer/warranties` - Registrar garantía
- `POST /api/consumer/warranties/{id}/claim` - Reclamar garantía
- `POST /api/consumer/complaints` - Crear reclamación
- `POST /api/consumer/complaints/{id}/mediate` - Solicitar mediación
- `POST /api/consumer/complaints/{id}/escalate` - Escalar a Pro-Consumidor
- `GET /api/consumer/disclosure/{productId}` - Verificar disclosure

---

### C5: ComplianceService (Marco General)

**Propósito:** Orquestación y gestión general de cumplimiento

```
┌─────────────────────────────────────────────────────────────────┐
│                     ComplianceService                            │
│                    Marco General de Cumplimiento                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── ComplianceStatus            ├── ComplianceCheck             │
│  │   • Compliant                 │   • EntityId, EntityType      │
│  │   • NonCompliant              │   • CheckType, Status         │
│  │   • PendingReview             │   • Score, Findings           │
│  │   • RequiresAction            │                                │
│  │                               ├── ComplianceRule              │
│  ├── ComplianceCheckType         │   • RuleName, Description     │
│  │   • Tax, AML, DataProtection  │   • Regulation, Severity      │
│  │   • Consumer, Contract        │   • IsActive                  │
│  │   • Environmental             │                                │
│  │                               ├── ComplianceViolation         │
│  ├── ViolationSeverity           │   • RuleId, ViolationType     │
│  │   • Info, Warning             │   • Description, Severity     │
│  │   • Minor, Major              │   • RemediationDeadline       │
│  │   • Critical                  │                                │
│  │                               ├── RemediationPlan             │
│  ├── RemediationStatus           │   • ViolationId, Steps        │
│  │   • NotStarted, InProgress    │   • Status, DueDate           │
│  │   • Completed, Overdue        │                                │
│  │                               ├── ComplianceReport            │
│  └── ReportFrequency             │   • Period, OverallScore      │
│      • Daily, Weekly             │   • Findings, Recommendations │
│      • Monthly, Quarterly        │                                │
│      • Annual                    └── ComplianceOfficer           │
│                                      • Name, Email, Certifications│
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 103 ✅                   ORQUESTA: Todos los C1-C12      │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/compliance/check` - Ejecutar verificación
- `GET /api/compliance/status/{entityId}` - Estado de cumplimiento
- `GET /api/compliance/violations` - Listar violaciones
- `POST /api/compliance/remediation` - Crear plan de remediación
- `GET /api/compliance/reports/{period}` - Generar reporte

---

### C6: ContractService (Ley 126-02)

**Propósito:** Gestión de contratos y firma digital

```
┌─────────────────────────────────────────────────────────────────┐
│                      ContractService                             │
│                       Ley 126-02                                 │
│                 (Comercio Electrónico y Firma Digital)           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── ContractType (13)           ├── ContractTemplate            │
│  │   • Sale, Lease               │   • Name, Description         │
│  │   • Consignment, Financing    │   • Version, Content          │
│  │   • Service, Warranty         │   • Clauses[]                 │
│  │   • TradeIn, Rental           │                                │
│  │   • Insurance, Guarantee      ├── Contract                     │
│  │   • PowerOfAttorney           │   • ContractNumber, Type       │
│  │   • TermsOfService            │   • Status, EffectiveDate     │
│  │   • PrivacyPolicy             │   • ExpirationDate            │
│  │                               │   • Parties[], Signatures[]   │
│  ├── ContractStatus (11)         │                                │
│  │   • Draft, PendingReview      ├── ContractParty               │
│  │   • PendingSignatures         │   • PartyType, PartyRole       │
│  │   • PartiallyExecuted         │   • Name, Identification      │
│  │   • FullyExecuted, Active     │   • SignatureStatus           │
│  │   • Expired, Terminated       │                                │
│  │   • Suspended, Cancelled      ├── ContractSignature           │
│  │   • Renewed                   │   • SignatureType, Hash        │
│  │                               │   • CertificateId             │
│  ├── PartyType (7)               │   • VerificationStatus        │
│  │   • Individual, Company       │   • SignedAt, IpAddress       │
│  │   • Dealer, Platform          │                                │
│  │   • Guarantor, Witness        ├── ContractClause              │
│  │   • Agent                     │   • ClauseType, Title          │
│  │                               │   • Content, IsRequired       │
│  ├── PartyRole (10)              │                                │
│  │   • Seller, Buyer             ├── ContractVersion             │
│  │   • Lessor, Lessee            │   • VersionNumber, Changes    │
│  │   • ServiceProvider, Client   │   • Status, CreatedAt         │
│  │   • Financier, Borrower       │                                │
│  │   • Guarantor, Witness        ├── ContractDocument            │
│  │                               │   • FileName, FileHash         │
│  ├── SignatureType (4)           │                                │
│  │   • Electronic, Digital       ├── ContractAuditLog            │
│  │   • Biometric, Holographic    │   • EventType, Description    │
│  │                               │   • UserId, Timestamp         │
│  ├── SignatureStatus (6)         │                                │
│  │   • Pending, Requested        └── CertificationAuthority      │
│  │   • Signed, Rejected              • Name, IsAccredited         │
│  │   • Expired, Revoked              • PublicKey, ValidUntil      │
│  │                                                                │
│  ├── SignatureVerificationStatus (4)                             │
│  │   • NotVerified, Verified                                      │
│  │   • Failed, Revoked                                            │
│  │                                                                │
│  └── ClauseType (10)                                              │
│      • General, Payment, Delivery                                 │
│      • Warranty, Liability, Termination                           │
│      • Confidentiality, DisputeResolution                         │
│      • ForceMajeure, Miscellaneous                                │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 141 ✅                   INTEGRACIÓN: INDOTEL (PKI)      │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/contracts` - Crear contrato
- `POST /api/contracts/{id}/parties` - Agregar parte
- `POST /api/contracts/{id}/sign` - Firmar contrato
- `GET /api/contracts/{id}/verify` - Verificar firmas
- `GET /api/contracts/{id}/pdf` - Generar PDF
- `POST /api/contracts/{id}/terminate` - Terminar contrato

---

### C7: EscrowService (Código Civil)

**Propósito:** Gestión de depósitos y garantías

```
┌─────────────────────────────────────────────────────────────────┐
│                       EscrowService                              │
│                      Código Civil RD                             │
│                  (Depósitos en Garantía)                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── EscrowTransactionType (9)   ├── EscrowAccount               │
│  │   • VehicleSale, VehicleLease │   • AccountNumber, Status      │
│  │   • ServiceDeposit            │   • BuyerId, SellerId          │
│  │   • SecurityDeposit           │   • Amount, Currency           │
│  │   • ReservationDeposit        │   • ReleaseConditions[]       │
│  │   • TradeInHold               │                                │
│  │   • FinancingDeposit          ├── ReleaseCondition            │
│  │   • InsuranceHold             │   • ConditionType, Description │
│  │   • DisputeHold               │   • Status, VerifiedAt        │
│  │                               │                                │
│  ├── EscrowStatus (11)           ├── FundMovement                 │
│  │   • Created, Funded           │   • MovementType, Amount       │
│  │   • PartiallyFunded           │   • PaymentMethod              │
│  │   • ConditionsSet             │   • Reference, Status          │
│  │   • PendingVerification       │                                │
│  │   • ConditionsMet             ├── EscrowDocument               │
│  │   • ReleaseAuthorized         │   • FileName, DocumentType     │
│  │   • Released, Refunded        │                                │
│  │   • Disputed, Cancelled       ├── EscrowDispute                │
│  │                               │   • DisputeNumber, Reason       │
│  ├── ReleaseConditionType (12)   │   • Status, ResolutionNotes    │
│  │   • DocumentVerification      │                                │
│  │   • VehicleInspection         ├── EscrowAuditLog               │
│  │   • TitleTransfer             │   • EventType, Description     │
│  │   • PaymentConfirmation       │   • Timestamp                  │
│  │   • InsuranceVerification     │                                │
│  │   • FinancingApproval         └── EscrowFeeConfiguration      │
│  │   • LegalClearance                • TransactionType, FeePercent│
│  │   • BuyerApproval                 • MinFee, MaxFee             │
│  │   • SellerApproval                                             │
│  │   • ThirdPartyVerification                                     │
│  │   • TimeElapsed                                                │
│  │   • MutualAgreement                                            │
│  │                                                                │
│  ├── ConditionStatus (6)                                          │
│  │   • Pending, InProgress                                        │
│  │   • Met, NotMet, Waived, Expired                               │
│  │                                                                │
│  ├── FundMovementType (10)                                        │
│  │   • Deposit, PartialDeposit                                    │
│  │   • Release, PartialRelease                                    │
│  │   • Refund, PartialRefund                                      │
│  │   • FeeDeduction, Adjustment                                   │
│  │   • DisputeHold, DisputeRelease                                │
│  │                                                                │
│  └── PaymentMethod (7)                                            │
│      • BankTransfer, CreditCard                                   │
│      • DebitCard, Cash                                            │
│      • Check, Financing, Crypto                                   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 135 ✅                   INTEGRACIÓN: Bancos RD          │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/escrow/accounts` - Crear cuenta escrow
- `POST /api/escrow/accounts/{id}/fund` - Depositar fondos
- `POST /api/escrow/accounts/{id}/conditions` - Agregar condiciones
- `POST /api/escrow/accounts/{id}/verify-condition` - Verificar condición
- `POST /api/escrow/accounts/{id}/release` - Liberar fondos
- `POST /api/escrow/accounts/{id}/dispute` - Iniciar disputa

---

### C8: DisputeService (Pro-Consumidor)

**Propósito:** Resolución de disputas y conflictos

```
┌─────────────────────────────────────────────────────────────────┐
│                       DisputeService                             │
│                      Pro-Consumidor RD                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ENUMS:                          ENTITIES:                       │
│  ├── DisputeType (9)             ├── Dispute                      │
│  │   • ContractBreach            │   • DisputeNumber, Type        │
│  │   • PaymentDispute            │   • Status, Priority           │
│  │   • QualityIssue              │   • ClaimantId, RespondentId   │
│  │   • DeliveryProblem           │   • Amount, Description        │
│  │   • WarrantyClaim             │   • ResponseDueDate            │
│  │   • RefundRequest             │                                │
│  │   • FraudAllegation           ├── DisputeEvidence              │
│  │   • ServiceComplaint          │   • EvidenceType, FileName     │
│  │   • PricingDispute            │   • Status, SubmittedBy        │
│  │                               │                                │
│  ├── DisputeStatus (9)           ├── DisputeComment               │
│  │   • Opened, UnderReview       │   • Content, IsInternal        │
│  │   • AwaitingResponse          │   • AuthorId                   │
│  │   • InMediation               │                                │
│  │   • InArbitration             ├── DisputeTimelineEvent         │
│  │   • Resolved, Closed          │   • EventType, Description     │
│  │   • Escalated, Cancelled      │   • Timestamp                  │
│  │                               │                                │
│  ├── DisputePriority (4)         ├── MediationSession             │
│  │   • Low, Medium               │   • SessionNumber, Status      │
│  │   • High, Critical            │   • ScheduledAt, MediatorId    │
│  │                               │   • Outcome, Notes             │
│  ├── ResolutionType (7)          │                                │
│  │   • FullRefund                ├── DisputeParticipant           │
│  │   • PartialRefund             │   • ParticipantRole, Name      │
│  │   • Replacement               │   • Email, Phone               │
│  │   • Repair                    │                                │
│  │   • ServiceCredit             ├── ResolutionTemplate           │
│  │   • Compensation              │   • DisputeType, Name          │
│  │   • Dismissal                 │   • Steps, EstimatedDays       │
│  │                               │                                │
│  ├── ParticipantRole (5)         └── DisputeSlaConfiguration     │
│  │   • Claimant, Respondent          • DisputeType, Priority      │
│  │   • Mediator, Arbitrator          • ResponseDeadline           │
│  │   • Witness                       • ResolutionDeadline         │
│  │                                   • EscalationThreshold        │
│  ├── EvidenceStatus (4)                                           │
│  │   • Pending, Accepted                                          │
│  │   • Rejected, UnderReview                                      │
│  │                                                                │
│  └── CommunicationChannel (4)                                     │
│      • Email, Phone, InApp, Letter                                │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 117 ✅                   INTEGRACIÓN: Pro-Consumidor     │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/disputes` - Crear disputa
- `POST /api/disputes/{id}/evidence` - Agregar evidencia
- `POST /api/disputes/{id}/respond` - Responder a disputa
- `POST /api/disputes/{id}/mediation` - Solicitar mediación
- `POST /api/disputes/{id}/resolve` - Resolver disputa
- `GET /api/disputes/{id}/timeline` - Obtener timeline

---

### C9: AuditService (Normativas de Auditoría)

**Propósito:** Registro y trazabilidad de auditoría

```
┌─────────────────────────────────────────────────────────────────┐
│                        AuditService                              │
│                   Normativas de Auditoría                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  FUNCIONALIDADES:                ENTITIES:                       │
│  ├── Event Logging               ├── AuditLog                    │
│  │   • User actions              │   • EventId, EventType        │
│  │   • System events             │   • EntityType, EntityId      │
│  │   • Data changes              │   • UserId, Timestamp         │
│  │   • Security events           │   • OldValue, NewValue        │
│  │                               │   • IpAddress, UserAgent      │
│  ├── Trail Management            │                                │
│  │   • Immutable logs            ├── AuditTrail                   │
│  │   • Tamper detection          │   • TrailId, StartDate        │
│  │   • Chain verification        │   • EndDate, Hash              │
│  │                               │   • PreviousHash              │
│  ├── Retention Policies          │                                │
│  │   • 5 años mínimo             ├── AuditRetentionPolicy        │
│  │   • Archiving rules           │   • EntityType, RetentionDays │
│  │   • Purge schedules           │   • ArchiveAfterDays          │
│  │                               │                                │
│  └── Reporting                   └── AuditReport                 │
│      • Compliance reports            • Period, GeneratedAt        │
│      • Activity summaries            • Findings, Anomalies        │
│      • Anomaly detection                                          │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 88 ✅                    INMUTABILIDAD: Blockchain-like  │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/audit/log` - Registrar evento
- `GET /api/audit/logs` - Consultar logs (con filtros)
- `GET /api/audit/entity/{type}/{id}` - Historia de entidad
- `GET /api/audit/user/{id}/activity` - Actividad de usuario
- `GET /api/audit/reports/{period}` - Generar reporte
- `POST /api/audit/verify` - Verificar integridad

---

### C10: LegalDocumentService (Requisitos Legales)

**Propósito:** Gestión de documentos legales

```
┌─────────────────────────────────────────────────────────────────┐
│                   LegalDocumentService                           │
│                    Requisitos Legales RD                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  TIPOS DE DOCUMENTOS:            ENTITIES:                       │
│  ├── Vehículos                   ├── LegalDocument               │
│  │   • Título de propiedad       │   • DocumentType, Status       │
│  │   • Matrícula                 │   • Content, Version          │
│  │   • Marbete                   │   • ValidFrom, ValidUntil     │
│  │   • Seguro obligatorio        │                                │
│  │   • Inspección INTRANT        ├── DocumentRequirement         │
│  │                               │   • TransactionType            │
│  │   • Poder especial            │   • RequiredDocuments[]       │
│  │                               │   • IsOptional                │
│  │                               │                                │
│  ├── Fiscales                    ├── DocumentValidation          │
│  │   • NCF                       │   • ValidationRules            │
│  │   • Factura                   │   • Status, ValidatedAt       │
│  │   • Comprobantes              │                                │
│  │                               └── DocumentTemplate            │
│  ├── Identidad                       • TemplateName, Content      │
│  │   • Cédula                        • Variables, Format          │
│  │   • Pasaporte                                                  │
│  │   • RNC                                                        │
│  │                                                                │
│  └── Contratos                                                    │
│      • Compraventa                                                │
│      • Arrendamiento                                              │
│      • Poder                                                      │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 53 ✅                    INTEGRACIÓN: DGII, INTRANT      │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/legal-documents` - Crear documento
- `GET /api/legal-documents/{id}` - Obtener documento
- `POST /api/legal-documents/{id}/validate` - Validar documento
- `GET /api/legal-documents/requirements/{transactionType}` - Requisitos
- `POST /api/legal-documents/generate` - Generar desde template

---

### C11: RegulatoryAlertService (Alertas Regulatorias)

**Propósito:** Monitoreo de cambios regulatorios

```
┌─────────────────────────────────────────────────────────────────┐
│                  RegulatoryAlertService                          │
│                   Alertas Regulatorias                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  FUENTES MONITOREADAS:           ENTITIES:                       │
│  ├── DGII                        ├── RegulatoryAlert             │
│  │   • Nuevas normativas         │   • AlertType, Source          │
│  │   • Cambios de tasas          │   • Title, Description         │
│  │   • Fechas límite             │   • EffectiveDate              │
│  │                               │   • Priority, Status           │
│  ├── Pro-Consumidor              │                                │
│  │   • Resoluciones              ├── AlertSubscription           │
│  │   • Nuevas regulaciones       │   • UserId, AlertTypes[]       │
│  │                               │   • NotificationChannel        │
│  ├── INDOTEL                     │                                │
│  │   • Normativas digitales      ├── AlertNotification           │
│  │   • Requisitos PKI            │   • AlertId, UserId            │
│  │                               │   • SentAt, ReadAt             │
│  ├── Banco Central               │                                │
│  │   • Tasas de cambio           └── ComplianceDeadline          │
│  │   • Regulaciones financieras      • RegulationId, Deadline     │
│  │                                   • Status, ReminderSent       │
│  └── Congreso Nacional                                            │
│      • Nuevas leyes                                               │
│      • Modificaciones                                             │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 35 ✅                    MONITOREO: Tiempo real          │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `GET /api/regulatory-alerts` - Listar alertas activas
- `POST /api/regulatory-alerts/subscribe` - Suscribirse a alertas
- `GET /api/regulatory-alerts/deadlines` - Próximos vencimientos
- `POST /api/regulatory-alerts/acknowledge/{id}` - Confirmar lectura

---

### C12: ComplianceIntegrationService (Integración Externa)

**Propósito:** Integración con entidades gubernamentales

```
┌─────────────────────────────────────────────────────────────────┐
│               ComplianceIntegrationService                       │
│                  Integración Gubernamental                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  INTEGRACIONES:                  ENTITIES:                       │
│                                                                  │
│  ┌─────────────────────┐         ├── IntegrationConnection      │
│  │        DGII         │         │   • SystemId, Name            │
│  │  • Validar RNC      │         │   • BaseUrl, ApiKey           │
│  │  • Enviar reportes  │         │   • Status, LastSync          │
│  │  • Obtener NCF      │         │                                │
│  │  • e-CF             │         ├── IntegrationLog              │
│  └─────────────────────┘         │   • ConnectionId, Request     │
│                                  │   • Response, StatusCode      │
│  ┌─────────────────────┐         │   • Timestamp                 │
│  │        JCE          │         │                                │
│  │  • Validar cédula   │         ├── SyncJob                     │
│  │  • Datos ciudadano  │         │   • JobType, Schedule         │
│  └─────────────────────┘         │   • LastRun, NextRun          │
│                                  │   • Status                    │
│  ┌─────────────────────┐         │                                │
│  │        UAF          │         └── IntegrationError            │
│  │  • Enviar ROS       │             • ConnectionId, ErrorCode   │
│  │  • Consultar estado │             • ErrorMessage, Timestamp   │
│  └─────────────────────┘                                          │
│                                                                  │
│  ┌─────────────────────┐                                          │
│  │      INTRANT        │                                          │
│  │  • Validar placa    │                                          │
│  │  • Estado vehículo  │                                          │
│  │  • Multas           │                                          │
│  └─────────────────────┘                                          │
│                                                                  │
│  ┌─────────────────────┐                                          │
│  │      INDOTEL        │                                          │
│  │  • PKI/Firmas       │                                          │
│  │  • Certificados     │                                          │
│  └─────────────────────┘                                          │
│                                                                  │
│  ┌─────────────────────┐                                          │
│  │   BANCO CENTRAL     │                                          │
│  │  • Tasas de cambio  │                                          │
│  │  • USD/DOP          │                                          │
│  └─────────────────────┘                                          │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  TESTS: 44 ✅                    FORMATO: REST + SOAP            │
└─────────────────────────────────────────────────────────────────┘
```

**Endpoints:**

- `POST /api/integration/dgii/validate-rnc` - Validar RNC
- `POST /api/integration/jce/validate-cedula` - Validar cédula
- `POST /api/integration/uaf/submit-ros` - Enviar ROS
- `GET /api/integration/intrant/vehicle/{plate}` - Consultar vehículo
- `GET /api/integration/bancentral/exchange-rate` - Tasa de cambio
- `GET /api/integration/status` - Estado de conexiones

---

## 🔗 Integraciones Externas

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              INTEGRACIONES GUBERNAMENTALES                               │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│     DGII     │     │     JCE      │     │     UAF      │     │   INTRANT    │
│              │     │              │     │              │     │              │
│ • RNC        │     │ • Cédulas    │     │ • ROS        │     │ • Placas     │
│ • NCF        │     │ • Ciudadanos │     │ • PLD/AML    │     │ • Multas     │
│ • Reportes   │     │ • Validación │     │ • Alertas    │     │ • Inspección │
│ • e-CF       │     │              │     │              │     │              │
└──────┬───────┘     └──────┬───────┘     └──────┬───────┘     └──────┬───────┘
       │                    │                    │                    │
       └──────────────────┬─┴────────────────────┴─┬──────────────────┘
                          │                        │
                          ▼                        ▼
              ┌─────────────────────────────────────────────┐
              │         ComplianceIntegrationService         │
              │                    (C12)                     │
              └─────────────────────────────────────────────┘
                                   │
       ┌───────────────────────────┼───────────────────────────┐
       │                           │                           │
       ▼                           ▼                           ▼
┌──────────────┐           ┌──────────────┐           ┌──────────────┐
│   INDOTEL    │           │ BANCO CENTRAL│           │PRO-CONSUMIDOR│
│              │           │              │           │              │
│ • PKI        │           │ • Tasas USD  │           │ • Disputas   │
│ • Firmas     │           │ • Políticas  │           │ • Mediación  │
│ • Certs      │           │   monetarias │           │ • Sanciones  │
└──────────────┘           └──────────────┘           └──────────────┘
```

---

## 📊 Flujos de Datos

### Flujo 1: Venta de Vehículo con Cumplimiento Completo

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                         FLUJO: VENTA DE VEHÍCULO CON COMPLIANCE                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘

  COMPRADOR                PLATAFORMA                    VENDEDOR
      │                        │                            │
      │  1. Inicia compra      │                            │
      ├───────────────────────►│                            │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │  C3 AML │  2. KYC/AML Check     │
      │                   │ Service │◄──────────────────────┤
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C1 Tax   │  3. Validar RNC       │
      │                   │Service  │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C6 Con-  │  4. Generar Contrato  │
      │                   │tract    │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │◄───────────────────────┤  5. Solicitar Firma       │
      │  6. Firma Digital      │                            │
      ├───────────────────────►│                            │
      │                        ├───────────────────────────►│
      │                        │  7. Solicitar Firma        │
      │                        │◄───────────────────────────┤
      │                        │  8. Firma Digital          │
      │                   ┌────┴────┐                       │
      │                   │C7 Escrow│  9. Depositar fondos  │
      │                   │Service  │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C10 Legal│  10. Generar docs     │
      │                   │Document │  (Traspaso, NCF)      │
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C12 Integ│  11. Validar INTRANT  │
      │                   │ration   │  12. Reportar DGII    │
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C9 Audit │  13. Log completo     │
      │                   │Service  │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │◄───────────────────────┤  14. Confirmar venta      │
      │                        ├───────────────────────────►│
      │  ✅ VENTA COMPLETADA   │                            │
      │                        │                            │
```

### Flujo 2: Reclamación de Consumidor

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO: RECLAMACIÓN DE CONSUMIDOR                               │
└─────────────────────────────────────────────────────────────────────────────────────────┘

  CONSUMIDOR               PLATAFORMA                    VENDEDOR
      │                        │                            │
      │  1. Crear reclamación  │                            │
      ├───────────────────────►│                            │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C4 Consu-│  2. Registrar caso    │
      │                   │mer Prot.│                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C2 Data  │  3. Verificar consent │
      │                   │Protect. │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │                        ├───────────────────────────►│
      │                        │  4. Notificar (15 días)    │
      │                        │                            │
      │                        │◄───────────────────────────┤
      │                        │  5. Respuesta              │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C8 Dispu-│  6. Evaluar respuesta │
      │                   │te Serv. │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │       [SI NO HAY ACUERDO]                           │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C8 Media-│  7. Agendar mediación │
      │                   │tion     │                       │
      │                   └────┬────┘                       │
      │                        │                            │
      │◄───────────────────────┤  8. Invitar a mediación   │
      │                        ├───────────────────────────►│
      │                        │                            │
      │       [SI NO HAY ACUERDO EN MEDIACIÓN]              │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C12 Integ│  9. Escalar a         │
      │                   │Pro-Cons.│  Pro-Consumidor       │
      │                   └────┬────┘                       │
      │                        │                            │
      │                   ┌────┴────┐                       │
      │                   │C9 Audit │  10. Log completo     │
      │                   │Service  │                       │
      │                   └────┬────┘                       │
      │                        │                            │
```

---

## 🔐 Seguridad y Cumplimiento

### Matriz de Seguridad

| Servicio          | Autenticación | Autorización | Encriptación | Auditoría |
| ----------------- | ------------- | ------------ | ------------ | --------- |
| C1 Tax            | JWT           | RBAC         | TLS 1.3      | ✅        |
| C2 DataProtection | JWT           | ABAC         | AES-256      | ✅        |
| C3 AML            | JWT + 2FA     | RBAC         | TLS 1.3      | ✅        |
| C4 Consumer       | JWT           | RBAC         | TLS 1.3      | ✅        |
| C5 Compliance     | JWT           | RBAC         | TLS 1.3      | ✅        |
| C6 Contract       | JWT + Firma   | RBAC         | PKI          | ✅        |
| C7 Escrow         | JWT + 2FA     | RBAC         | TLS 1.3      | ✅        |
| C8 Dispute        | JWT           | RBAC         | TLS 1.3      | ✅        |
| C9 Audit          | JWT           | Admin Only   | Immutable    | ✅        |
| C10 LegalDoc      | JWT           | RBAC         | TLS 1.3      | ✅        |
| C11 Alerts        | JWT           | RBAC         | TLS 1.3      | ✅        |
| C12 Integration   | API Keys      | IP Whitelist | mTLS         | ✅        |

### Retención de Datos

| Tipo de Dato           | Retención        | Base Legal     |
| ---------------------- | ---------------- | -------------- |
| Transacciones fiscales | 10 años          | Ley 11-92      |
| Datos personales       | Hasta revocación | Ley 172-13     |
| Registros AML          | 5 años           | Ley 155-17     |
| Contratos              | 10 años          | Código Civil   |
| Auditoría              | 5 años           | Normativas     |
| Disputas               | 3 años           | Pro-Consumidor |

---

## 📈 Métricas y Monitoreo

### Health Checks

Cada microservicio expone:

- `GET /health` - Estado general
- `GET /health/ready` - Listo para recibir tráfico
- `GET /health/live` - Proceso vivo

### Métricas Prometheus

- `compliance_checks_total` - Total de verificaciones
- `compliance_violations_total` - Total de violaciones
- `integration_requests_total` - Llamadas a sistemas externos
- `integration_latency_seconds` - Latencia de integraciones

---

## 📚 Referencias Legales

| Ley          | Nombre                               | Servicio              |
| ------------ | ------------------------------------ | --------------------- |
| Ley 11-92    | Código Tributario                    | C1 TaxCompliance      |
| Ley 172-13   | Protección de Datos                  | C2 DataProtection     |
| Ley 155-17   | Prevención Lavado de Activos         | C3 AML                |
| Ley 358-05   | Derechos del Consumidor              | C4 ConsumerProtection |
| Ley 126-02   | Comercio Electrónico y Firma Digital | C6 Contract           |
| Código Civil | Obligaciones y Contratos             | C7 Escrow, C8 Dispute |

---

**Documento generado:** Enero 20, 2026  
**Total Tests:** 895 ✅  
**Cobertura:** 12 Microservicios de Compliance
