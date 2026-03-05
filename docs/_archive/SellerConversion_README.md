# 🔄 Seller Conversion — Buyer → Seller

**Feature:** Convertir cuentas Buyer a Seller Individual  
**Servicio:** UserService (.NET 8, Clean Architecture)  
**Endpoint:** `POST /api/sellers/convert`  
**Fecha:** Febrero 2026

---

## 📋 Resumen

Permite a usuarios con `AccountType = Buyer` o `Guest` convertirse en vendedores individuales (`AccountType = Seller`). La conversión:

- Crea un `SellerProfile` con datos del usuario existente
- Crea un `SellerConversion` record para auditoría y rollback
- Actualiza `User.AccountType` de Buyer → Seller
- Publica 2 domain events: `SellerConversionRequestedEvent` + `SellerCreatedEvent`
- Soporta idempotencia via `Idempotency-Key` header

### ⛔ Restricciones

| AccountType        | Resultado                                              |
| ------------------ | ------------------------------------------------------ |
| **Buyer**          | ✅ Conversión exitosa                                  |
| **Guest**          | ✅ Conversión exitosa                                  |
| **Seller**         | ⚠️ Retorna perfil existente (idempotente)              |
| **Dealer**         | ❌ **RECHAZADO** — `CONVERSION_NOT_ALLOWED` (HTTP 400) |
| **DealerEmployee** | ❌ **RECHAZADO** — `CONVERSION_NOT_ALLOWED` (HTTP 400) |
| **Admin**          | ✅ Conversión exitosa (admin puede ser seller también) |

> 🔴 **Buyer → Dealer NO está implementado.** Si se intenta, el handler lanza `InvalidOperationException("CONVERSION_NOT_ALLOWED")` que se traduce a un RFC 7807 ProblemDetails con `errorCode: "CONVERSION_NOT_ALLOWED"`.

---

## 🏗️ Arquitectura

### Archivos Creados

| Archivo                                                                                     | Capa           | Descripción                       |
| ------------------------------------------------------------------------------------------- | -------------- | --------------------------------- |
| `Domain/Entities/SellerConversion.cs`                                                       | Domain         | Entidad de tracking de conversión |
| `Domain/Interfaces/ISellerConversionRepository.cs`                                          | Domain         | Interfaz del repositorio          |
| `Application/DTOs/SellerConversionDtos.cs`                                                  | Application    | DTOs de request/response          |
| `Application/UseCases/Sellers/ConvertBuyerToSeller/ConvertBuyerToSellerCommand.cs`          | Application    | Comando MediatR                   |
| `Application/UseCases/Sellers/ConvertBuyerToSeller/ConvertBuyerToSellerCommandHandler.cs`   | Application    | Handler (10 pasos)                |
| `Application/UseCases/Sellers/ConvertBuyerToSeller/ConvertBuyerToSellerCommandValidator.cs` | Application    | Validación FluentValidation       |
| `Infrastructure/Persistence/Configurations/SellerConversionConfiguration.cs`                | Infrastructure | EF Core config                    |
| `Infrastructure/Repositories/SellerConversionRepository.cs`                                 | Infrastructure | Implementación repositorio        |

### Archivos Modificados

| Archivo                                              | Cambio                               |
| ---------------------------------------------------- | ------------------------------------ |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | Agregado `DbSet<SellerConversion>`   |
| `Application/Interfaces/IAuditServiceClient.cs`      | Agregado `LogSellerConversionAsync`  |
| `Infrastructure/Clients/AuditServiceClient.cs`       | Implementación audit logging         |
| `Application/Metrics/UserServiceMetrics.cs`          | 3 contadores de métricas             |
| `Api/Controllers/SellersController.cs`               | Endpoint `POST /api/sellers/convert` |
| `Api/Program.cs`                                     | Registro DI + FluentValidation       |

### Shared Contracts (Eventos)

| Archivo                                                                       | EventType                     |
| ----------------------------------------------------------------------------- | ----------------------------- |
| `_Shared/CarDealer.Contracts/Events/Seller/SellerConversionRequestedEvent.cs` | `seller.conversion.requested` |
| `_Shared/CarDealer.Contracts/Events/Seller/SellerCreatedEvent.cs`             | `seller.created`              |

---

## 📡 API

### `POST /api/sellers/convert`

**Auth:** Bearer JWT requerido  
**Headers opcionales:** `Idempotency-Key: <uuid>`

#### Request Body

```json
{
  "acceptTerms": true,
  "preferredContactMethod": "whatsapp",
  "acceptsOffers": true,
  "showPhone": true,
  "showLocation": true,
  "bio": "Vendedor de confianza en Santo Domingo"
}
```

#### Response 201 (Created) / 200 (Already Seller)

```json
{
  "success": true,
  "data": {
    "conversionId": "uuid",
    "userId": "uuid",
    "sellerProfileId": "uuid",
    "source": "conversion",
    "status": "Approved",
    "previousAccountType": "Buyer",
    "newAccountType": "Seller",
    "pendingVerification": true,
    "requestedAt": "2026-02-18T...",
    "completedAt": "2026-02-18T...",
    "sellerProfile": {
      "id": "uuid",
      "fullName": "Test Buyer",
      "email": "buyer@test.com",
      "verificationStatus": "PendingReview",
      "maxActiveListings": 3,
      "canSellHighValue": false
    }
  }
}
```

#### Response 400 (Dealer Rejected)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Conversion not allowed",
  "status": 400,
  "detail": "Dealer accounts cannot be converted to individual seller. Use the dealer management portal.",
  "errorCode": "CONVERSION_NOT_ALLOWED"
}
```

---

## 🗄️ Database Schema

### Table: `seller_conversions`

```sql
CREATE TABLE seller_conversions (
    "Id"                  UUID PRIMARY KEY,
    "UserId"              UUID NOT NULL,
    "SellerProfileId"     UUID NOT NULL,
    "Source"              VARCHAR(50) NOT NULL,
    "PreviousAccountType" VARCHAR(50) NOT NULL,
    "NewAccountType"      VARCHAR(50) NOT NULL,
    "Status"              VARCHAR(30) NOT NULL DEFAULT 'Pending',
    "KycProfileId"        UUID NULL,
    "IdempotencyKey"      VARCHAR(100) NULL,
    "CorrelationId"       VARCHAR(100) NULL,
    "IpAddress"           VARCHAR(45) NULL,
    "UserAgent"           VARCHAR(500) NULL,
    "Notes"               VARCHAR(1000) NULL,
    "RequestedAt"         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CompletedAt"         TIMESTAMPTZ NULL,
    "RevertedAt"          TIMESTAMPTZ NULL
);

-- Indexes
CREATE UNIQUE INDEX IX_SellerConversions_UserId ON seller_conversions ("UserId");
CREATE UNIQUE INDEX IX_SellerConversions_IdempotencyKey
    ON seller_conversions ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
CREATE INDEX IX_SellerConversions_Status ON seller_conversions ("Status");
CREATE INDEX IX_SellerConversions_SellerProfileId ON seller_conversions ("SellerProfileId");
```

---

## 🔄 EF Core Migration

### Generar

```bash
cd backend/UserService

dotnet ef migrations add AddSellerConversions \
  --project UserService.Infrastructure \
  --startup-project UserService.Api
```

### Aplicar

```bash
# Desarrollo local
dotnet ef database update \
  --project UserService.Infrastructure \
  --startup-project UserService.Api

# Producción — generar SQL script
dotnet ef migrations script --idempotent \
  --project UserService.Infrastructure \
  --startup-project UserService.Api \
  --output migration-seller-conversions.sql
```

### Auto-migration

Si `EnableAutoMigration: true` en appsettings, la migration se aplica automáticamente al iniciar el pod.

---

## 🧪 Tests

### Unit Tests (8 tests)

```
UserService.Tests/Application/ConvertBuyerToSellerTests.cs
```

| Test                                                          | Escenario                | Resultado Esperado                                               |
| ------------------------------------------------------------- | ------------------------ | ---------------------------------------------------------------- |
| `Handle_BuyerToSeller_Success_CreatesProfileAndReturnsResult` | Buyer convierte          | 201, SellerProfile + SellerConversion creados, events publicados |
| `Handle_AlreadySeller_ReturnsExistingProfile_Idempotent`      | Ya es seller             | 200, retorna perfil existente, sin duplicados                    |
| `Handle_DealerToSeller_ThrowsConversionNotAllowed`            | Dealer intenta           | InvalidOperationException("CONVERSION_NOT_ALLOWED")              |
| `Handle_DealerEmployeeToSeller_ThrowsConversionNotAllowed`    | DealerEmployee intenta   | InvalidOperationException("CONVERSION_NOT_ALLOWED")              |
| `Handle_DuplicateIdempotencyKey_ReturnsSameResponse`          | Idempotency-Key repetido | Retorna misma respuesta sin crear nuevos records                 |
| `Handle_UserNotFound_ThrowsKeyNotFoundException`              | UserId no existe         | KeyNotFoundException                                             |
| `Handle_GuestToSeller_Success`                                | Guest convierte          | 201, conversión exitosa                                          |
| `Handle_EventPublishFails_ConversionStillSucceeds`            | RabbitMQ caído           | Conversión exitosa (events best-effort)                          |

### Ejecutar

```bash
cd backend/UserService
dotnet test UserService.Tests --filter "ConvertBuyerToSellerTests" --verbosity normal
```

---

## 🔙 Rollback

### Revertir una conversión (SQL manual)

```sql
-- 1. Obtener datos de la conversión
SELECT * FROM seller_conversions WHERE "UserId" = '<user-id>';

-- 2. Restaurar AccountType original
UPDATE users
SET "AccountType" = 'Buyer', "UserIntent" = 'Buy', "UpdatedAt" = NOW()
WHERE "Id" = '<user-id>';

-- 3. Marcar conversión como revertida
UPDATE seller_conversions
SET "Status" = 'Reverted', "RevertedAt" = NOW()
WHERE "UserId" = '<user-id>';

-- 4. (Opcional) Desactivar seller profile
UPDATE seller_profiles
SET "IsActive" = false, "UpdatedAt" = NOW()
WHERE "UserId" = '<user-id>';
```

### Rollback completo de la feature (migration)

```bash
# Revertir migration
dotnet ef migrations remove \
  --project UserService.Infrastructure \
  --startup-project UserService.Api

# O revertir a una migration específica
dotnet ef database update <PreviousMigrationName> \
  --project UserService.Infrastructure \
  --startup-project UserService.Api
```

---

## 📊 Métricas (Prometheus/Grafana)

| Métrica                                   | Tipo    | Descripción                        |
| ----------------------------------------- | ------- | ---------------------------------- |
| `seller.conversions.requested_total`      | Counter | Total de solicitudes de conversión |
| `seller.conversions.approved_total`       | Counter | Conversiones exitosas              |
| `seller.conversions.failed_total{reason}` | Counter | Conversiones fallidas con razón    |

### Alertas sugeridas

```yaml
- alert: HighSellerConversionFailureRate
  expr: rate(seller_conversions_failed_total[5m]) / rate(seller_conversions_requested_total[5m]) > 0.3
  for: 10m
  labels:
    severity: warning
  annotations:
    summary: "Alta tasa de fallos en conversión de sellers"
```

---

## 🔐 Seguridad

- ✅ `NoSqlInjection()` en PreferredContactMethod, Bio, IdempotencyKey
- ✅ `NoXss()` en PreferredContactMethod, Bio
- ✅ JWT `[Authorize]` obligatorio
- ✅ UserId extraído del JWT claim (no del body)
- ✅ Audit logging via AuditServiceClient
- ✅ Idempotency key support para prevenir duplicados
- ✅ Events NO contienen PII (solo IDs y metadata)

---

## 📡 Domain Events

### SellerConversionRequestedEvent

```json
{
  "eventId": "uuid",
  "eventType": "seller.conversion.requested",
  "occurredAt": "2026-02-18T...",
  "schemaVersion": 1,
  "correlationId": null,
  "userId": "uuid",
  "conversionId": "uuid",
  "sellerProfileId": "uuid",
  "source": "conversion",
  "previousAccountType": "Buyer",
  "kycProfileId": null,
  "requestedAt": "2026-02-18T..."
}
```

### SellerCreatedEvent

```json
{
  "eventId": "uuid",
  "eventType": "seller.created",
  "occurredAt": "2026-02-18T...",
  "schemaVersion": 1,
  "userId": "uuid",
  "sellerProfileId": "uuid",
  "source": "conversion",
  "accountType": "Seller",
  "createdAt": "2026-02-18T..."
}
```

---

## ✅ Acceptance Criteria

- [x] `POST /api/sellers/convert` creates SellerProfile + SellerConversion
- [x] Buyer → Seller succeeds with 201
- [x] Dealer → Seller rejected with 400 + CONVERSION_NOT_ALLOWED
- [x] Already-seller returns existing profile (idempotent, 200)
- [x] Idempotency-Key header prevents duplicates
- [x] Events published (best-effort, non-blocking)
- [x] Audit log recorded
- [x] 3 Prometheus metrics tracked
- [x] 8 unit tests passing
- [x] SecurityValidators applied (NoSqlInjection, NoXss)
- [x] Build succeeds with 0 errors, 0 warnings
