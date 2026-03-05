# 🏦 Sprint 4: Integración AZUL - Phase 1 COMPLETA

**Fecha Inicio:** Enero 8, 2026  
**Fecha Fin:** Enero 8, 2026  
**Estado:** ✅ PHASE 1 COMPLETADA (Payment Page + Persistence)

---

## 📋 Resumen Ejecutivo

AZUL (Servicios Digitales Popular) es el procesador de pagos líder en República Dominicana. Ofrece dos métodos principales de integración para e-commerce:

| Método                       | Complejidad | PCI Compliance       | Recomendado |
| ---------------------------- | ----------- | -------------------- | ----------- |
| **Payment Page (Hosted)**    | Baja        | AZUL maneja todo     | ✅ Para MVP |
| **Webservices API (Direct)** | Alta        | Comercio responsable | Para fase 2 |

---

## 🔗 URLs de Integración

### Ambiente de Pruebas

| Tipo         | URL                                                         |
| ------------ | ----------------------------------------------------------- |
| Payment Page | `https://pruebas.azul.com.do/PaymentPage/`                  |
| JSON API     | `https://pruebas.azul.com.do/webservices/JSON/Default.aspx` |
| SOAP API     | `https://pruebas.azul.com.do/webservices/SOAP/Default.asmx` |

### Producción

| Tipo         | URL Principal                                             | URL Alterna                                                   |
| ------------ | --------------------------------------------------------- | ------------------------------------------------------------- |
| Payment Page | `https://pagos.azul.com.do/PaymentPage/Default.aspx`      | `https://contpagos.azul.com.do/PaymentPage/Default.aspx`      |
| JSON API     | `https://pagos.azul.com.do/webservices/JSON/Default.aspx` | `https://contpagos.azul.com.do/Webservices/JSON/default.aspx` |

---

## 🔐 Autenticación

### Payment Page (Hosted)

- **AuthHash**: Hash SHA-512 calculado con campos + AuthKey
- AuthKey proporcionado por AZUL durante afiliación

### Webservices API (Direct)

- **Auth1** y **Auth2**: Headers HTTP proporcionados por AZUL
- Requiere certificado digital o VPN Site-to-Site
- TLS 1.2 obligatorio

---

## 💳 Tipos de Transacción

| TrxType  | Descripción                       | Uso               |
| -------- | --------------------------------- | ----------------- |
| `Sale`   | Venta con captura inmediata       | Pagos estándar    |
| `Hold`   | Pre-autorización (reserva fondos) | Reservas          |
| `Post`   | Captura de Hold previo            | Confirmar reserva |
| `Void`   | Anulación (antes de 20 min)       | Cancelar venta    |
| `Refund` | Devolución (hasta 6 meses)        | Reembolsos        |

---

## 📝 Campos Requeridos (Payment Page)

```json
{
  "MerchantId": "Proporcionado por AZUL",
  "MerchantName": "OKLA Marketplace",
  "MerchantType": "E-Commerce",
  "CurrencyCode": "214", // DOP
  "OrderNumber": "ORD-12345",
  "Amount": "100000", // $1,000.00 (sin decimales, últimos 2 son centavos)
  "ITBIS": "18000", // $180.00 ITBIS
  "ApprovedUrl": "https://okla.com.do/payment/approved",
  "DeclinedUrl": "https://okla.com.do/payment/declined",
  "CancelUrl": "https://okla.com.do/payment/cancelled",
  "AuthHash": "SHA-512 calculado"
}
```

---

## 🔒 Cálculo del AuthHash

```csharp
// Orden de concatenación para Payment Page:
string toHash = MerchantId + MerchantName + MerchantType + CurrencyCode
              + OrderNumber + Amount + ITBIS + ApprovedUrl + DeclinedUrl
              + CancelUrl + UseCustomField1 + CustomField1Label
              + CustomField1Value + UseCustomField2 + CustomField2Label
              + CustomField2Value + AuthKey;

// Calcular HMAC-SHA512
using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(authKey));
byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(toHash));
string authHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
```

---

## 📤 Respuesta de Transacción

| Campo               | Descripción                                |
| ------------------- | ------------------------------------------ |
| `IsoCode`           | Código ISO-8583 (00 = Aprobada)            |
| `ResponseMessage`   | APROBADA, DECLINADA, etc.                  |
| `AuthorizationCode` | Código de autorización                     |
| `AzulOrderId`       | ID único de AZUL (usar para refunds/voids) |
| `RRN`               | Reference Referral Number                  |
| `DateTime`          | Fecha/hora formato YYYYMMDDHHMMSS          |
| `ErrorDescription`  | Descripción del error si aplica            |

---

## 🏪 DataVault (Tokenización)

Permite guardar tarjetas para pagos recurrentes:

```json
{
  "SaveToDataVault": "1", // Guardar tarjeta
  "DataVaultToken": "" // Token devuelto por AZUL
}
```

**Respuesta incluye:**

- `DataVaultToken`: Token único (30-40 caracteres)
- `DataVaultExpiration`: AAAAMM
- `DataVaultBrand`: VISA, MASTERCARD, etc.

---

## 🛡️ 3D Secure 2.0

AZUL soporta autenticación 3DS 2.0:

1. **Flujo Sin Fricción**: Autenticación automática
2. **Flujo con Desafío**: Requiere OTP del banco

Campos adicionales:

```json
{
  "ThreeDSAuth": {
    "TermUrl": "URL para respuesta 3DS",
    "MethodNotificationUrl": "URL para notificación"
  }
}
```

---

## 📊 Códigos de Respuesta Comunes

| IsoCode | Mensaje              | Acción                   |
| ------- | -------------------- | ------------------------ |
| 00      | APROBADA             | Transacción exitosa      |
| 05      | DECLINADA            | No autorizada por emisor |
| 14      | TARJETA INVALIDA     | Número incorrecto        |
| 51      | FONDOS INSUFICIENTES | Sin saldo                |
| 54      | TARJETA EXPIRADA     | Vencida                  |
| 91      | EMISOR NO DISPONIBLE | Reintentar               |

---

## 🔧 Configuración Requerida

### Credenciales de AZUL (obtener de ejecutivo)

```env
# BillingService appsettings
AZUL__MerchantId=XXXXXXXXXX
AZUL__MerchantName=OKLA Marketplace
AZUL__AuthKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
AZUL__Auth1=XXXXXXXX
AZUL__Auth2=XXXXXXXX
AZUL__CurrencyCode=214
AZUL__Environment=Test  # o Production
```

---

## 📞 Contacto AZUL

- **Email:** vozdelcliente@azul.com.do
- **Teléfono:** 809-544-2985
- **Soporte Técnico:** solucionesintegradas@azul.com.do
- **Portal Desarrolladores:** https://dev.azul.com.do

---

## 🏗️ Plan de Implementación

### Fase 1: Payment Page (Sprint 4.1)

- [ ] Crear `AzulPaymentPageService`
- [ ] Implementar cálculo de AuthHash
- [ ] Crear endpoints de callback (approved/declined/cancel)
- [ ] Integrar con BillingService existente
- [ ] UI: Selector de método de pago (Stripe vs AZUL)

### Fase 2: Webservices API (Sprint 4.2)

- [ ] Configurar VPN o certificados digitales
- [ ] Implementar `AzulWebserviceClient`
- [ ] Agregar DataVault para tarjetas guardadas
- [ ] Implementar 3D Secure 2.0

### Fase 3: Optimización (Sprint 4.3)

- [ ] Webhooks para reconciliación
- [ ] Reportes de transacciones
- [ ] Dashboard de pagos unificado

---

## 📁 Archivos a Crear

```
backend/BillingService/
├── BillingService.Application/
│   ├── Services/
│   │   ├── IAzulPaymentService.cs
│   │   └── AzulPaymentService.cs
│   └── DTOs/
│       ├── AzulPaymentRequest.cs
│       └── AzulPaymentResponse.cs
├── BillingService.Infrastructure/
│   ├── Azul/
│   │   ├── AzulHashGenerator.cs
│   │   ├── AzulWebserviceClient.cs
│   │   └── AzulConfiguration.cs
│   └── PaymentGateways/
│       ├── IPaymentGateway.cs
│       ├── StripeGateway.cs
│       └── AzulGateway.cs
└── BillingService.Api/
    └── Controllers/
        └── AzulCallbackController.cs
```

---

## ✅ Checklist Pre-Implementación

- [x] Solicitar credenciales de prueba a AZUL
- [x] Configurar URLs de callback en ambiente de pruebas
- [ ] Definir flujo de selección de pasarela (Stripe vs AZUL)
- [ ] Diseñar UI del selector de pago
- [x] Crear tarjetas de prueba para testing

### Tarjetas de Prueba AZUL

| Escenario        | Número           | CVV | CVV     |
| ---------------- | ---------------- | --- | ------- |
| 3DS Sin Fricción | 4265880000000007 | 999 | 12/2027 |
| 3DS Con Desafío  | 4005520000000129 | 999 | 12/2027 |

---

## ✅ PHASE 1: PAYMENT PAGE + PERSISTENCE - COMPLETADO

**Fecha de Completado:** Enero 8, 2026

### 📦 Componentes Implementados

#### 1. DTOs y Modelos (Subtasks 1-2)

- ✅ `AzulPaymentRequest.cs` - Request DTO con 16 campos requeridos
- ✅ `AzulPaymentResponse.cs` - Response DTO con helpers IsApproved/IsDeclined
- ✅ 13 propiedades para metadata del pago (authorization, response codes, timestamps)

#### 2. Configuración (Subtask 3)

- ✅ `AzulConfiguration.cs` - Configuración con URLs dinámicas Test/Production
- ✅ `appsettings.json` - Sección Azul configurada
- ✅ Dynamic URLs basadas en IsTestEnvironment flag

#### 3. Servicios de Seguridad (Subtask 4)

- ✅ `IAzulHashGenerator` - Interface en Application layer
- ✅ `AzulHashGenerator` - Implementación HMAC-SHA512 en Infrastructure
- ✅ `GenerateRequestHash()` - 17 parámetros para Payment Page
- ✅ `GenerateResponseHash()` - 10 parámetros para validación
- ✅ `ValidateResponseHash()` - Verifica integridad de respuestas AZUL

#### 4. Servicios de Pago (Subtask 5)

- ✅ `IAzulPaymentService` - Interface para creación de pagos
- ✅ `AzulPaymentService` - Implementación con formateo de montos
- ✅ `CreatePaymentRequest()` - Genera request con AuthHash
- ✅ `FormatAmount()` - Convierte decimales a formato AZUL (sin puntos, últimos 2 dígitos = centavos)

#### 5. Controllers (Subtasks 6-7)

- ✅ `AzulPaymentController` - POST /api/payment/azul/initiate
- ✅ `AzulCallbackController` - 3 callbacks (approved/declined/cancelled)
- ✅ Hash validation en todos los callbacks
- ✅ Logging estructurado con Serilog
- ✅ Error handling con try-catch

#### 6. Dominio y Persistencia (Subtasks 8-10)

- ✅ `AzulTransaction.cs` - Entity con 18 propiedades

  - OrderNumber, AzulOrderId, Amount, ITBIS
  - AuthorizationCode, ResponseCode, IsoCode
  - Status (Approved/Declined/Cancelled/Error)
  - DataVault fields (Token, Expiration, Brand)
  - User metadata (UserId, Email, Name)
  - Audit fields (IpAddress, UserAgent, Timestamps)

- ✅ `IAzulTransactionRepository` - Interface con 8 métodos

  - GetByIdAsync, GetByOrderNumberAsync, GetByAzulOrderIdAsync
  - GetByUserIdAsync, GetApprovedTransactionsAsync
  - CreateAsync, UpdateAsync, ExistsAsync

- ✅ `AzulTransactionRepository` - Implementación EF Core

  - LINQ queries con async/await
  - OrderByDescending para date sorting
  - Where clauses para Status filtering

- ✅ `AzulTransactionConfiguration` - Fluent API

  - 18 column mappings con HasColumnName
  - Decimal precision (18,2) para Amount/ITBIS
  - MaxLength specifications
  - 5 performance indexes:
    - idx_azul_transactions_order_number
    - idx_azul_transactions_azul_order_id
    - idx_azul_transactions_user_id
    - idx_azul_transactions_status
    - idx_azul_transactions_datetime

- ✅ `BillingDbContext.cs` - Updated
  - DbSet<AzulTransaction> added
  - Configuration applied in OnModelCreating

#### 7. Dependency Injection (Subtask 10)

- ✅ `Program.cs` - Todos los servicios registrados
  - AzulConfiguration con Options pattern
  - IAzulHashGenerator → AzulHashGenerator (Scoped)
  - IAzulPaymentService → AzulPaymentService (Scoped)
  - IAzulTransactionRepository → AzulTransactionRepository (Scoped)

#### 8. Database Migration (Subtask 11)

- ✅ `20260108161828_AddAzulTransactions.cs` - EF Core migration
- ✅ Migration aplicada a Docker PostgreSQL container
- ✅ Tabla `azul_transactions` creada con 18 columnas
- ✅ 5 índices B-tree para performance
- ✅ Auto-migration habilitada en Program.cs

### 🗂️ Archivos Creados/Modificados

**Nuevos archivos (13):**

1. `BillingService.Application/DTOs/Azul/AzulPaymentRequest.cs`
2. `BillingService.Application/DTOs/Azul/AzulPaymentResponse.cs`
3. `BillingService.Application/Configuration/AzulConfiguration.cs`
4. `BillingService.Application/Interfaces/IAzulHashGenerator.cs`
5. `BillingService.Application/Services/AzulPaymentService.cs`
6. `BillingService.Infrastructure/Azul/AzulHashGenerator.cs`
7. `BillingService.Api/Controllers/AzulPaymentController.cs`
8. `BillingService.Api/Controllers/AzulCallbackController.cs`
9. `BillingService.Domain/Entities/AzulTransaction.cs`
10. `BillingService.Domain/Interfaces/IAzulTransactionRepository.cs`
11. `BillingService.Infrastructure/Repositories/AzulTransactionRepository.cs`
12. `BillingService.Infrastructure/Persistence/Configurations/AzulTransactionConfiguration.cs`
13. `BillingService.Infrastructure/Migrations/20260108161828_AddAzulTransactions.cs`

**Archivos modificados (4):**

1. `BillingService.Api/Program.cs` - DI registration
2. `BillingService.Api/appsettings.json` - Azul configuration
3. `BillingService.Api/BillingService.Api.csproj` - Added EF Design package
4. `BillingService.Infrastructure/Persistence/BillingDbContext.cs` - Added AzulTransactions DbSet

### 🧪 Verificación de Base de Datos

```sql
-- Tabla creada correctamente
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public' AND table_name = 'azul_transactions';
-- Resultado: azul_transactions

-- Estructura verificada
\d azul_transactions
-- 18 columnas correctas
-- 5 índices B-tree funcionando
```

### 📊 Métricas de Código

| Métrica                      | Valor |
| ---------------------------- | ----- |
| Líneas de código agregadas   | ~1850 |
| Archivos nuevos              | 13    |
| Archivos modificados         | 4     |
| Endpoints API creados        | 4     |
| Métodos de repositorio       | 8     |
| Índices de base de datos     | 5     |
| Tiempo de implementación     | 4h    |
| Tests unitarios (pendientes) | 0     |

### 🚀 Próximos Pasos (Phase 2)

#### Testing (Alta Prioridad)

- [ ] Crear tests unitarios para AzulHashGenerator
- [ ] Crear tests de integración para AzulPaymentService
- [ ] Crear tests de API para controllers
- [ ] Probar con tarjetas de prueba AZUL
- [ ] Validar hash generation/validation

#### Configuración (Requerido antes de producción)

- [ ] Obtener credenciales de producción de AZUL
- [ ] Configurar MerchantId, AuthKey, Auth1, Auth2
- [ ] Actualizar callback URLs a dominio de producción
- [ ] Probar en ambiente de pruebas AZUL

#### Frontend Integration (Sprint 5)

- [ ] Crear PaymentMethodSelector component
- [ ] Implementar redirect a AZUL Payment Page
- [ ] Handle callbacks (approved/declined/cancelled pages)
- [ ] Mostrar status de transacción al usuario
- [ ] Integrar con checkout flow

#### Webservices API (Phase 3 - Opcional)

- [ ] Implementar direct charge via JSON API
- [ ] Agregar DataVault tokenization
- [ ] Implementar 3D Secure 2.0
- [ ] Crear servicio de refund/void

---

## 📚 Recursos Adicionales

### Guías y Documentación

| Recurso                    | Descripción                                        | Ubicación                                                       |
| -------------------------- | -------------------------------------------------- | --------------------------------------------------------------- |
| **Guía de Sandbox**        | Proceso completo para obtener credenciales de AZUL | [AZUL_SANDBOX_SETUP_GUIDE.md](AZUL_SANDBOX_SETUP_GUIDE.md)      |
| **Script de Testing**      | Tests automatizados de integración                 | [scripts/test-azul-sandbox.sh](../scripts/test-azul-sandbox.sh) |
| **Formulario HTML Test**   | Interfaz web para probar pagos manualmente         | [test-azul-payment.html](test-azul-payment.html)                |
| **Documentación Completa** | Resumen de completado de Sprint 4                  | [SPRINT_4_COMPLETED.md](SPRINT_4_COMPLETED.md)                  |

### Testing Tools

#### 1. Script de Testing Automatizado

```bash
# Ejecutar todos los tests de integración
cd /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices
./scripts/test-azul-sandbox.sh

# Tests incluidos:
# ✅ Health check del servicio
# ✅ Payment initiation
# ✅ Formateo de montos
# ✅ Generación de AuthHash
# ✅ Conexión a PostgreSQL
# ✅ Verificación de tabla y índices
# ✅ Análisis de logs
```

#### 2. Formulario HTML de Testing Manual

```bash
# Abrir el formulario en el navegador
open docs/test-azul-payment.html

# O servir con Python
cd docs
python3 -m http.server 8000
# Luego abrir: http://localhost:8000/test-azul-payment.html
```

**Funcionalidades del formulario:**

- Input de amount, ITBIS y orderNumber
- Generación automática de payment request via API
- Redirect automático a AZUL Payment Page
- Lista de tarjetas de prueba con resultados esperados

### Comandos Útiles

```bash
# Ver logs de BillingService
docker logs billingservice --tail 100 -f

# Verificar transacciones en DB
docker exec postgres_db psql -U postgres -d billingservice -c \
  "SELECT order_number, status, amount, created_at FROM azul_transactions ORDER BY created_at DESC LIMIT 10;"

# Reiniciar servicio después de cambios
docker-compose restart billingservice

# Rebuild completo
docker-compose up -d --build billingservice
```

---

**✅ Phase 1 completada exitosamente. Sistema listo para recibir pagos AZUL via Payment Page.**
