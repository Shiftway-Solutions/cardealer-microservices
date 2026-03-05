# 🚗 Portal Dealer & Seller — Implementación Completa

**Fecha:** Febrero 2026  
**Servicios modificados:** PaymentService, VehiclesSaleService, Gateway, Frontend (web-next)  
**Estado:** Implementación backend + frontend completa, pendiente deploy

---

## 📋 Resumen de Cambios

### 🔧 Bug Fixes (3 servicios)

Se corrigieron errores de sintaxis MediatR en 3 servicios que impedían compilación:

| Servicio                        | Archivo      | Error                                                         | Fix                                                        |
| ------------------------------- | ------------ | ------------------------------------------------------------- | ---------------------------------------------------------- |
| **Vehicle360ProcessingService** | `Program.cs` | Comentario+código inyectado dentro del lambda de `AddMediatR` | Movido `AddTransient<ValidationBehavior>` fuera del lambda |
| **BackgroundRemovalService**    | `Program.cs` | Mismo error MediatR                                           | Mismo fix                                                  |
| **PaymentService**              | `Program.cs` | Mismo error MediatR                                           | Mismo fix + registro de `IInvoiceRepository`               |

### 💰 Sistema de Facturación (PaymentService)

Nuevas entidades, repositorios y endpoints para generación de facturas con cumplimiento DGII:

| Archivo                                            | Tipo         | Descripción                                                  |
| -------------------------------------------------- | ------------ | ------------------------------------------------------------ |
| `Domain/Entities/Invoice.cs`                       | **NEW**      | Entidad con campos NCF, DGII, buyer/seller info, PDF storage |
| `Domain/Interfaces/IInvoiceRepository.cs`          | **NEW**      | Interface con 8 métodos (CRUD, paginación, auto-numbering)   |
| `Infrastructure/Repositories/InvoiceRepository.cs` | **NEW**      | Implementación completa con formato `OKLA-{year}-{seq}`      |
| `Infrastructure/Persistence/AzulDbContext.cs`      | **MODIFIED** | +DbSet Invoices, +DbSet PaymentTransactions, +entity configs |
| `Api/Controllers/InvoicesController.cs`            | **NEW**      | 4 endpoints: GET by ID, GET my, GET dealer, GET download     |

**Endpoints:**

- `GET /api/invoices/{id}` — Obtener factura por ID
- `GET /api/invoices/my?page=1&pageSize=10` — Mis facturas (paginado)
- `GET /api/invoices/dealer/{dealerId}?page=1&pageSize=10` — Facturas de dealer
- `GET /api/invoices/{id}/download` — Redirect a URL de PDF

### 📨 Sistema de Leads/Mensajería (VehiclesSaleService)

Sistema completo de contacto comprador→vendedor con thread de mensajes:

| Archivo                                              | Tipo         | Descripción                                                                  |
| ---------------------------------------------------- | ------------ | ---------------------------------------------------------------------------- |
| `Domain/Entities/Lead.cs`                            | **NEW**      | Lead + LeadMessage entities, enums (LeadStatus, LeadSource)                  |
| `Api/Controllers/LeadsController.cs`                 | **NEW**      | 8 endpoints: create, get, list seller/dealer, reply, messages, status, stats |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | **MODIFIED** | +DbSet Leads/LeadMessages, +entity configs con indexes                       |

**Endpoints:**

- `POST /api/leads` — Crear lead (anónimo, sin auth)
- `GET /api/leads/{id}` — Detalle del lead (auth, seller only)
- `GET /api/leads/seller/{sellerId}?page=1&pageSize=10` — Leads del vendedor
- `GET /api/leads/dealer/{dealerId}?page=1&pageSize=10` — Leads del dealer
- `POST /api/leads/{id}/reply` — Responder a lead (auth)
- `GET /api/leads/{id}/messages` — Thread de mensajes
- `PATCH /api/leads/{id}/status` — Cambiar estado
- `GET /api/leads/seller/{sellerId}/stats` — Estadísticas de leads

### 📡 Domain Events (CarDealer.Contracts)

| Archivo                                              | EventType                    | Trigger              |
| ---------------------------------------------------- | ---------------------------- | -------------------- |
| `Events/Billing/InvoiceGeneratedEvent.cs`            | `billing.invoice.generated`  | Al generar factura   |
| `Events/Billing/PublicationCreditsPurchasedEvent.cs` | `billing.credits.purchased`  | Al comprar créditos  |
| `Events/Vehicle/VehiclePublishedEvent.cs`            | `vehicles.vehicle.published` | Al publicar vehículo |
| `Events/Vehicle/LeadCreatedEvent.cs`                 | `vehicles.lead.created`      | Al crear lead        |

### 🖥️ Frontend (Next.js 16)

| Ruta                | Archivo      | Descripción                                                               |
| ------------------- | ------------ | ------------------------------------------------------------------------- |
| `/vender/dashboard` | **NEW**      | Dashboard vendedor: stats, leads recientes, vehículos, quick actions      |
| `/vender/leads`     | **NEW**      | Gestión de leads: listado, filtros, detalle, respuestas, cambio de estado |
| `/mis-vehiculos`    | **MODIFIED** | Conectado a API real (antes: datos hardcoded mock)                        |

### 🔗 Gateway Routes

Rutas agregadas a `ocelot.prod.json` y `ocelot.dev.json`:

| Ruta                                 | Servicio            | Auth | Método |
| ------------------------------------ | ------------------- | ---- | ------ |
| `/api/leads`                         | vehiclessaleservice | No   | POST   |
| `/api/leads/seller/{sellerId}`       | vehiclessaleservice | Sí   | GET    |
| `/api/leads/seller/{sellerId}/stats` | vehiclessaleservice | Sí   | GET    |
| `/api/leads/dealer/{dealerId}`       | vehiclessaleservice | Sí   | GET    |
| `/api/leads/{id}`                    | vehiclessaleservice | Sí   | GET    |
| `/api/leads/{id}/reply`              | vehiclessaleservice | Sí   | POST   |
| `/api/leads/{id}/messages`           | vehiclessaleservice | Sí   | GET    |
| `/api/leads/{id}/status`             | vehiclessaleservice | Sí   | PATCH  |
| `/api/invoices/{id}`                 | paymentservice      | Sí   | GET    |
| `/api/invoices/my`                   | paymentservice      | Sí   | GET    |
| `/api/invoices/dealer/{dealerId}`    | paymentservice      | Sí   | GET    |
| `/api/invoices/{id}/download`        | paymentservice      | Sí   | GET    |

### ☸️ Kubernetes

| Archivo                                   | Descripción                                                     |
| ----------------------------------------- | --------------------------------------------------------------- |
| `k8s/hpa-portal-services.yaml`            | **NEW** — HPA para vehiclessaleservice, paymentservice, gateway |
| `scripts/scale-dealer-seller-services.sh` | **NEW** — Script para habilitar servicios del portal            |

---

## 🚀 Pasos de Deploy

### 1. Build y Push de Imágenes

Las imágenes se construyen automáticamente via CI/CD (`smart-cicd.yml`) al hacer push a `main`:

```bash
git add -A
git commit -m "feat(portal): implement dealer & seller portal complete"
git push origin main
```

### 2. Verificar Imágenes en GHCR

```bash
# Verificar que las imágenes existen
docker pull ghcr.io/gregorymorenoiem/vehiclessaleservice:latest
docker pull ghcr.io/gregorymorenoiem/paymentservice:latest
docker pull ghcr.io/gregorymorenoiem/gateway:latest
```

### 3. Actualizar ConfigMap del Gateway

```bash
kubectl delete configmap gateway-config -n okla
kubectl create configmap gateway-config \
  --from-file=ocelot.json=backend/Gateway/Gateway.Api/ocelot.prod.json -n okla
kubectl rollout restart deployment/gateway -n okla
```

### 4. Escalar Servicios

```bash
# Opción A: Script automático
chmod +x scripts/scale-dealer-seller-services.sh
./scripts/scale-dealer-seller-services.sh

# Opción B: Manual
kubectl scale deployment paymentservice --replicas=1 -n okla
kubectl scale deployment dealermanagementservice --replicas=1 -n okla
```

### 5. Aplicar HPA

```bash
kubectl apply -f k8s/hpa-portal-services.yaml
```

### 6. Crear Bases de Datos (si no existen)

```bash
# Port-forward a PostgreSQL managed
kubectl port-forward svc/postgres 5432:5432 -n okla

# Crear DBs
psql -h localhost -U okla_admin -c "CREATE DATABASE paymentservice_db;"
```

### 7. Ejecutar Migraciones

Las migraciones se aplican automáticamente al iniciar los pods si `EnableAutoMigration: true`.

### 8. Verificar

```bash
# Pods running
kubectl get pods -n okla | grep -E 'payment|vehicle|gateway'

# Health checks
curl -s https://okla.com.do/api/health | jq .

# Test leads endpoint
curl -s https://okla.com.do/api/leads \
  -H "Content-Type: application/json" \
  -d '{"vehicleId": "...", "buyerName": "Test", "buyerEmail": "test@test.com", "message": "Interesado"}'
```

---

## 📊 Arquitectura de Datos

### Lead Entity

```
Lead
├── Id (UUID)
├── VehicleId → Vehicle
├── SellerId (UUID)
├── DealerId? (UUID, nullable)
├── BuyerName, BuyerEmail, BuyerPhone?
├── Message (initial message)
├── VehicleTitle, VehiclePrice, VehicleImageUrl (denormalized)
├── Status (New|Contacted|Negotiating|Closed|Lost|Spam)
├── Source (Website|API|WhatsApp|Phone|Email|ChatBot)
├── Messages[] → LeadMessage
├── IpAddress, UserAgent
└── CreatedAt, UpdatedAt
```

### Invoice Entity

```
Invoice
├── Id (UUID)
├── PaymentTransactionId → PaymentTransaction
├── UserId, DealerId?
├── InvoiceNumber (OKLA-2026-000001)
├── Ncf? (NCF DGII)
├── Subtotal, TaxRate, TaxAmount, TotalAmount
├── Currency, ExchangeRate, AmountInDop
├── Description, LineItemsJson (JSONB)
├── Buyer: Name, Email, Rnc?, Address?, Phone?
├── Seller: Name, Rnc, Address
├── PdfUrl, PdfStorageKey
├── Status (Draft|Issued|Sent|Paid|Cancelled|Voided)
└── IssuedAt, CreatedAt, UpdatedAt
```

---

_Documento generado — Febrero 2026_
