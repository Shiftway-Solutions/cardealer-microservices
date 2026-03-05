---
title: "Auditoría: Registro de Gastos Operativos - OKLA S.R.L."
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 📊 Auditoría: Registro de Gastos Operativos - OKLA S.R.L.

**Documento de Auditoría #8**  
**Fecha:** Enero 29, 2026  
**Auditor:** Gregory Moreno  
**Versión:** 1.0

---

## 📌 DATOS DE AUDITORÍA

**Documentos de Referencia:**

- [Matriz de Procesos: 11-REGISTRO-GASTOS-OPERATIVOS.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/11-REGISTRO-GASTOS-OPERATIVOS.md)
- [Obligaciones Fiscales DGII](45-obligaciones-fiscales-dgii.md)
- [Procedimiento Fiscal OKLA](../../process-matrix/08-COMPLIANCE-LEGAL-RD/10-PROCEDIMIENTO-FISCAL-OKLA.md)

**Empresa Auditada:**
| Campo | Valor |
|----------------------|------------------------------------|
| Razón Social | OKLA S.R.L. |
| RNC | 1-33-32590-1 |
| Registro Mercantil | 196339PSD |
| Fecha de Fundación | Enero 25, 2026 |
| Actividad Principal | Servicios de publicidad digital |

**Propósito del Sistema:**

- Registrar TODOS los gastos operativos de OKLA
- Generar automáticamente Formato 606 (compras) para DGII
- Calcular retenciones ISR 10% (IR-17)
- Proveer información lista para auditorías
- Optimizar carga tributaria legalmente

---

## 🎯 RESUMEN EJECUTIVO

### Estado de Compliance

| Aspecto                       | Backend | Frontend | Gap  | Estado     |
| ----------------------------- | ------- | -------- | ---- | ---------- |
| **Registro de Gastos**        | 🟡 30%  | 🔴 0%    | -30% | 🔴 CRÍTICO |
| **Clasificación Proveedores** | 🔴 0%   | 🔴 0%    | 0%   | 🔴 BLOCKER |
| **Verificación NCF**          | 🔴 0%   | 🔴 0%    | 0%   | 🔴 CRÍTICO |
| **Cálculo Retenciones**       | 🔴 0%   | 🔴 0%    | 0%   | 🔴 CRÍTICO |
| **Generación Formato 606**    | 🔴 0%   | 🔴 0%    | 0%   | 🔴 BLOCKER |
| **Calendario Fiscal**         | 🔴 0%   | 🔴 0%    | 0%   | 🔴 CRÍTICO |
| **Alertas Automáticas**       | 🔴 0%   | 🔴 0%    | 0%   | 🔴 ALTA    |

**Compliance Global:** 🔴 **5%** (1/20 requisitos)

---

## 🎨 WIREFRAME - REGISTRO DE GASTOS

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  REGISTRO DE GASTOS OPERATIVOS                 [+ Nuevo Gasto]│
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 💰 Gastos◀│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐│
│ │ 📋 606   │  │ 💵 RD$45K   │ │ 📄 23       │ │ ⚠️ 5        │ │ 📊 18%    ││
│ │ 📊 Reportes│  │ Mes actual  │ │ Facturas    │ │ Pendientes  │ │ ITBIS     ││
│ │ 📅 Calendar│  │             │ │ registradas │ │ de NCF      │ │ Deducible ││
│ │          │  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘│
│ │          │                                                                │
│ │          │  ┌─────────────────────────────────────────────────────────┐  │
│ │          │  │ 🔍 Buscar...   [Mes ▼] [Categoría ▼] [Proveedor ▼]       │  │
│ │          │  └─────────────────────────────────────────────────────────┘  │
│ │          │                                                                │
│ │          │  ┌─────────────────────────────────────────────────────────┐  │
│ │          │  │ GASTOS DEL MES                                          │  │
│ │          │  │                                                          │  │
│ │          │  │ │ NCF             │ Proveedor     │ Categoría   │ Monto  │  │
│ │          │  │ │─────────────────┼───────────────┼─────────────┼────────│  │
│ │          │  │ │ B0100012345 ✅ │ Claro RD      │ Telecom     │ RD$5K  │  │
│ │          │  │ │ B0100012346 ✅ │ EDENORTE      │ Servicios   │ RD$8K  │  │
│ │          │  │ │ B0100012347 ⚠️ │ Office Depot  │ Materiales  │ RD$3K  │  │
│ │          │  │ │ B1300000089 ✅ │ Freelancer X  │ Honorarios  │ RD$15K │  │
│ │          │  │ │ (Pendiente) ❌ │ Amazon Web S. │ Tecnología  │ US$150 │  │
│ │          │  │                                                          │  │
│ │          │  │  Mostrando 1-5 de 23        [← Anterior] [1] [2] [→]     │  │
│ │          │  └─────────────────────────────────────────────────────────┘  │
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - NUEVO GASTO

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ← Volver                         REGISTRAR GASTO            [Guardar Gasto] │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ DATOS DEL PROVEEDOR                                                     │ │
│  │                                                                         │ │
│  │  Tipo de Proveedor *                    RNC/Cédula *                    │ │
│  │  ○ Local (RD)    ● Internacional        ┌─────────────────────────────┐ │ │
│  │                                         │ 1-01-23456-7          [🔍]  │ │ │
│  │                                         └─────────────────────────────┘ │ │
│  │  Razón Social                           NCF (si aplica)                 │ │
│  │  ┌─────────────────────────────┐        ┌─────────────────────────────┐ │ │
│  │  │ Claro Dominicana S.A.       │        │ B0100012345                 │ │ │
│  │  └─────────────────────────────┘        └─────────────────────────────┘ │ │
│  │                                                                         │ │
│  │  ⚠️ NCF verificado con DGII: ✅ Válido                                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ DETALLES DEL GASTO                                                      │ │
│  │                                                                         │ │
│  │  Categoría DGII *              Fecha *                                  │ │
│  │  ┌─────────────────────────────┐  ┌─────────────────────────────┐       │ │
│  │  │ 01 - Gastos Personales   ▼  │  │ 2026-01-29             📅  │       │ │
│  │  └─────────────────────────────┘  └─────────────────────────────┘       │ │
│  │                                                                         │ │
│  │  Monto sin ITBIS *             ITBIS (18%)       Retención ISR (10%)    │ │
│  │  ┌─────────────────────────┐   ┌───────────┐     ☑️ Aplicar retención   │ │
│  │  │ RD$ 8,474.58            │   │ RD$ 1,525 │                            │ │
│  │  └─────────────────────────┘   └───────────┘     Monto: RD$ 847.46      │ │
│  │                                                                         │ │
│  │  Total Factura: RD$ 10,000.00                                           │ │
│  │                                                                         │ │
│  │  Descripción                                                            │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │ │
│  │  │ Servicio de telecomunicaciones - Enero 2026                     │    │ │
│  │  └─────────────────────────────────────────────────────────────────┘    │ │
│  │                                                                         │ │
│  │  Comprobante 📎 [factura-claro-ene26.pdf]     [Subir otro archivo]      │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔍 ANÁLISIS DETALLADO

### 1. Backend Existente (FinanceService)

#### ✅ LO QUE EXISTE (Básico - 30%)

**Archivo:** `backend/FinanceService/FinanceService.Domain/Entities/Expense.cs`

```csharp
// Entidad básica de gastos (NO cumple DGII)
public class Expense : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid DealerId { get; set; }
    public string ExpenseNumber { get; private set; }

    public ExpenseCategory Category { get; private set; }  // ❌ No son categorías DGII
    public ExpenseStatus Status { get; private set; }

    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "MXN";  // ❌ Debería ser "DOP"

    public DateTime ExpenseDate { get; private set; }
    public string? Vendor { get; private set; }           // ❌ No separa local/internacional
    public string? VendorTaxId { get; private set; }       // ❌ No valida RNC
    public string? InvoiceNumber { get; private set; }     // ❌ No distingue NCF
    public string? ReceiptUrl { get; private set; }

    // ❌ FALTA: Tipo de proveedor (Local/Internacional)
    // ❌ FALTA: Tipo de NCF (B01/B02/B13)
    // ❌ FALTA: ITBIS deducible
    // ❌ FALTA: Retención ISR 10%
    // ❌ FALTA: Tasa de cambio
    // ❌ FALTA: Tipo de gasto DGII (01-11)
    // ❌ FALTA: Validación de NCF con DGII
}
```

**Categorías actuales (NO cumplen DGII):**

```csharp
public enum ExpenseCategory
{
    Utilities,      // ❌ No es categoría DGII
    Rent,           // ❌ No es categoría DGII
    Salaries,       // ❌ No va en 606 (va en TSS)
    Marketing,      // ❌ No es categoría DGII
    Supplies,       // ❌ No es categoría DGII
    // ... 14 categorías más NO compatibles con DGII
}
```

#### ❌ LO QUE FALTA (70%)

**Entidades Nuevas Requeridas:**

1. **ExpenseProvider** (proveedores con clasificación DGII)
2. **ExpenseDocument** (múltiples docs por gasto)
3. **ExpenseNcfValidation** (verificación automática NCF)
4. **ExpenseRetention** (retenciones ISR 10%)
5. **ExpenseExchangeRate** (tasas de cambio diarias)

**Funcionalidades Backend Faltantes:**

- ❌ Integración con API de DGII para verificar NCF
- ❌ Generación automática de NCF B13 (gastos internacionales)
- ❌ Cálculo automático de retenciones ISR 10%
- ❌ Descarga automática de tasa de cambio (Banco Central RD)
- ❌ Generación de Formato 606
- ❌ Validaciones de duplicados de NCF
- ❌ Sistema de alertas (día 3, 8, 13, 18)
- ❌ Conciliación bancaria
- ❌ Calendario fiscal automático

---

### 2. Frontend Existente

#### ❌ NO EXISTE (0%)

**Búsqueda en código:**

```bash
# Búsqueda en frontend
grep -r "expense" frontend/web/src/**/*.tsx
grep -r "gasto" frontend/web/src/**/*.tsx
grep -r "ExpenseManagement" frontend/web/src/**/*.tsx

# Resultado: 0 matches
```

**Rutas que NO existen:**

- `/admin/expenses` → Dashboard de gastos
- `/admin/expenses/register` → Formulario registro de gastos
- `/admin/expenses/approval` → Aprobación de gastos
- `/admin/expenses/providers` → Catálogo de proveedores
- `/admin/expenses/reports` → Reportes de gastos
- `/admin/expenses/606` → Generador de Formato 606
- `/admin/expenses/calendar` → Calendario fiscal

**Componentes que NO existen:**

- `ExpenseRegistrationForm` → Formulario completo con clasificación DGII
- `ExpenseProviderSelector` → Selector local/internacional
- `NcfValidator` → Verificador en tiempo real
- `RetentionCalculator` → Calculadora ISR 10%
- `ExpenseApprovalList` → Lista para contador
- `Expense606Generator` → Generador Formato 606
- `ExpenseDashboard` → Vista general de gastos

---

### 3. Proveedores de OKLA (Catálogo Real)

#### 3.1 Proveedores Internacionales (27 proveedores)

**Categoría: Hosting/Cloud (5 proveedores)**
| Proveedor | País | Uso | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| Digital Ocean | USA | Hosting principal | $100 | B13 |
| AWS | USA | Storage S3 | $50 | B13 |
| Google Cloud | USA | Maps, APIs | $30 | B13 |
| Cloudflare | USA | CDN, DNS, SSL | $20 | B13 |
| Vercel | USA | Frontend hosting | $0 | - |

**Categoría: Desarrollo (6 proveedores)**
| Proveedor | País | Uso | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| GitHub | USA | Repos + Copilot | $21 | B13 |
| JetBrains | Rep. Checa | IntelliJ/Rider | $17 | B13 |
| Postman | USA | API testing | $12 | B13 |
| VS Code | USA | Editor (gratis) | $0 | - |
| Docker Hub | USA | Container registry | $0 | - |
| npm/yarn | USA | Package manager | $0 | - |

**Categoría: Pasarelas de Pago (2 proveedores)**
| Proveedor | País | Comisión | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| Stripe | USA/Irlanda | ~3% transacciones | ~$250 | B13 |
| PayPal | USA | ~3.5% + $0.30 | ~$50 | B13 |

**Categoría: Publicidad Digital (5 proveedores)**
| Proveedor | País | Uso | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| Google Ads | USA/Irlanda | SEM, Display | $500 | B13 |
| Facebook Ads | USA/Irlanda | Facebook + Instagram | $350 | B13 |
| TikTok Ads | Singapur | Video ads | $200 | B13 |
| LinkedIn Ads | USA | B2B marketing | $100 | B13 |
| Twitter Ads | USA | Promociones | $50 | B13 |

**Categoría: Comunicaciones (3 proveedores)**
| Proveedor | País | Uso | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| Twilio | USA | SMS, Verificación | $50 | B13 |
| SendGrid | USA | Email transaccional | $30 | B13 |
| Mailchimp | USA | Email marketing | $20 | B13 |

**Categoría: IA/ML (3 proveedores)**
| Proveedor | País | Uso | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| OpenAI | USA | ChatGPT API | $100 | B13 |
| Anthropic | USA | Claude API | $50 | B13 |
| Google AI | USA | Gemini, ML models | $30 | B13 |

**Categoría: Software/Herramientas (3 proveedores)**
| Proveedor | País | Uso | ~Mensual | NCF |
|-----------------|---------------|----------------------|----------|------|
| Microsoft 365 | USA | Office, OneDrive | $30 | B13 |
| Adobe CC | USA | Photoshop, Illustr. | $55 | B13 |
| Figma | USA | Diseño UI/UX | $15 | B13 |

**Total Gastos Internacionales:** ~$2,100 USD/mes (~$126,000 DOP/mes)

---

#### 3.2 Proveedores Locales (12 proveedores)

**Categoría: Pasarelas Locales (1 proveedor)**
| Proveedor | RNC | Uso | ~Mensual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| AZUL Banco Popular | 101-00001-1 | Comisiones ~2.5% | $8,000 | B01 | No |

**Categoría: Bancos (3 proveedores)**
| Proveedor | RNC | Uso | ~Mensual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| Banco Popular | 101-00001-1 | Cuenta empresarial| $1,500 | B01 | No |
| Banreservas | 401-000-000 | Cuenta secundaria | $800 | B01 | No |
| BHD León | 101-XXXXXXX | Cuenta USD | $500 | B01 | No |

**Categoría: Telecomunicaciones (2 proveedores)**
| Proveedor | RNC | Uso | ~Mensual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| Claro | 101-XXXXXXX | Internet fibra | $3,500 | B01 | No |
| Altice | 101-XXXXXXX | Móvil empresarial | $2,000 | B01 | No |

**Categoría: Dominio Local (1 proveedor)**
| Proveedor | RNC | Uso | Anual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| NIC.do | 130529842 | okla.com.do | $2,500 | B01 | No |

**Categoría: Electricidad (1 proveedor)**
| Proveedor | RNC | Uso | ~Mensual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| Edenorte/Edesur | 430-XXXXXXX | Electricidad | $2,500 | B01 | No (exento)|

**Categoría: Profesionales (2 proveedores)**
| Proveedor | RNC/Cédula | Uso | ~Mensual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| Contador | Cédula | Contabilidad | $15,000 | B01 | **10%** |
| Abogado | Cédula | Legal | $10,000 | B01 | **10%** |

**Categoría: Alquiler (1 proveedor)**
| Proveedor | RNC/Cédula | Uso | ~Mensual | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| Dueño Oficina | Cédula | Alquiler oficina | $25,000 | B02 | **10%** |

**Categoría: Suministros (1 proveedor)**
| Proveedor | RNC | Uso | Variable | NCF | Retención |
|---------------------|-------------|-------------------|----------|------|-----------|
| Office Depot | 101-XXXXXXX | Suministros | $2,000 | B01 | No |

**Total Gastos Locales:** ~$73,300 DOP/mes

**Total Gastos OKLA:** ~$199,300 DOP/mes (~$2.4M DOP/año)

---

## 📋 REQUISITOS DE DGII PARA REGISTRO DE GASTOS

### Requisito 1: Clasificación de Proveedores

**Proceso Requerido:**

1. **Tipo de Proveedor:**
   - Local (con RNC)
   - Internacional (sin RNC)
   - Persona Física (con cédula)

2. **Datos del Proveedor Local:**
   - RNC válido (verificado en DGII)
   - Nombre comercial
   - Tipo de entidad (SRL, SA, EIRL, Persona Física)

3. **Datos del Proveedor Internacional:**
   - Nombre completo
   - País
   - Invoice number

**Backend Necesario:** 13 SP  
**Frontend Necesario:** 8 SP  
**Total:** 21 SP

---

### Requisito 2: Registro de Gasto con NCF

**Datos Obligatorios para Gastos Locales:**

```typescript
interface ExpenseLocal {
  provider: {
    rnc: string; // RNC verificado
    name: string;
    type: "EMPRESA" | "PERSONA_FISICA";
  };
  ncf: {
    number: string; // B0100000789
    type: "B01" | "B02" | "B04";
    date: Date;
    verified: boolean; // Verificado en DGII
  };
  amounts: {
    subtotal: number; // Base imponible
    itbis: number; // ITBIS 18%
    total: number;
    isrWithheld: number; // Retención ISR 10% (si aplica)
    netPayable: number; // Total - Retención
  };
  dgiiCategory: string; // "02" = Servicios, "03" = Alquiler, etc.
  paymentMethod: string; // "02" = Transferencia, "03" = Tarjeta, etc.
  documentUrl: string; // S3 URL del NCF escaneado
}
```

**Datos Obligatorios para Gastos Internacionales:**

```typescript
interface ExpenseInternational {
  provider: {
    name: string;
    country: string;
    hasRNC: false;
  };
  invoice: {
    number: string;
    date: Date;
    amountUSD: number;
    exchangeRate: number; // Del Banco Central RD
    amountDOP: number;
  };
  ncfB13: string; // Generado por sistema (B1300000001)
  dgiiCategory: string; // "02" = Servicios, "07" = Financieros
  paymentMethod: string; // "03" = Tarjeta
  documentUrl: string; // S3 URL del invoice PDF
}
```

**Backend Necesario:** 21 SP  
**Frontend Necesario:** 13 SP  
**Total:** 34 SP

---

### Requisito 3: Verificación de NCF (Integración DGII)

**API de DGII para Verificar NCF:**

```typescript
// Endpoint DGII (ficticio - usar el real)
GET https://dgii.gov.do/api/ncf/verify/{ncf}

Response: {
  ncf: "B0100000123",
  valid: true,
  rnc: "101234567",
  businessName: "Proveedor XYZ SRL",
  status: "ACTIVE",
  issueDate: "2026-01-25",
  expirationDate: "2026-12-31"
}
```

**Flujo de Verificación:**

1. Usuario ingresa NCF en formulario
2. Sistema hace llamada a DGII API
3. Si válido → Permitir guardar
4. Si inválido → Mostrar error, no permitir guardar

**Backend Necesario:** 8 SP  
**Frontend Necesario:** 3 SP  
**Total:** 11 SP

---

### Requisito 4: Cálculo de Retenciones ISR 10%

**Cuándo Aplicar Retención ISR 10%:**

| Tipo de Gasto             | Retención | Base Legal         |
| ------------------------- | --------- | ------------------ |
| Servicios profesionales   | 10%       | Art. 309 Ley 11-92 |
| Alquiler a persona física | 10%       | Art. 309           |
| Servicios técnicos        | 10%       | Art. 309           |
| Publicidad (>$50K)        | 10%       | Art. 309           |

**Cuándo NO Retener:**

- ❌ Empresas (SRL, SA) - No aplica retención
- ❌ Servicios públicos (luz, agua) - Exentos
- ❌ Compra de bienes - Solo servicios retienen
- ❌ Gastos internacionales - Otro régimen (Art. 305)

**Ejemplo de Cálculo:**

```typescript
// Contador (Persona Física) factura $15,000 + ITBIS
const subtotal = 15000;
const itbis = subtotal * 0.18; // $2,700
const total = subtotal + itbis; // $17,700
const isrRetention = subtotal * 0.1; // $1,500 (10% sobre base)
const netPayable = total - isrRetention; // $16,200 (pago al proveedor)

// Los $1,500 retenidos se declaran en IR-17 día 10
```

**Backend Necesario:** 5 SP  
**Frontend Necesario:** 5 SP  
**Total:** 10 SP

---

### Requisito 5: Generación de Formato 606

**Especificación del Formato 606:**

```
# Estructura de línea en archivo 606.txt
| RNC/Cédula | Tipo ID | Tipo Gasto | NCF | NCF Modificado | Fecha Pago | Fecha Retención | Monto Facturado | ITBIS Facturado | ITBIS Retenido | ... | Forma Pago |

# Ejemplo real de OKLA:
# Contador - Servicios contables $15,000 + ITBIS, retención 10%
102345678|1|02|B0100000789||20260125|20260125|15000.00|2700.00|0.00|0.00|0.00|0.00|0.00|0.00|1500.00|02|

# Digital Ocean - Hosting $100 USD = $6,000 DOP (sin ITBIS)
|0|3|02|B1300000001||20260115|20260115|6000.00|0.00|6000.00|0.00|0.00|0.00|0.00|0.00|0.00|03|
```

**Proceso de Generación:**

1. **Día 1-5:** Cierre de mes, registrar últimos gastos
2. **Día 6-9:** Generar preview del 606, revisar totales
3. **Día 10:** Enviar IR-17 (retenciones)
4. **Día 11-14:** Validar 606 con herramienta DGII
5. **Día 15:** Enviar 606 a DGII

**Backend Necesario:** 13 SP  
**Frontend Necesario:** 8 SP  
**Total:** 21 SP

---

### Requisito 6: Calendario Fiscal Automático

**Alertas Requeridas:**

| Día | Alerta               | Acción                        | Prioridad  |
| --- | -------------------- | ----------------------------- | ---------- |
| 3   | Cierre de mes        | Registrar últimos gastos      | 🔴 ALTA    |
| 5   | TSS (nómina)         | Enviar SUIR + pagar           | 🔴 ALTA    |
| 8   | IR-17 próximo        | Revisar retenciones           | 🔴 ALTA    |
| 10  | Enviar IR-17         | Presentar y pagar retenciones | 🔴 CRÍTICA |
| 13  | 606/607/608 próximos | Validar formatos              | 🔴 ALTA    |
| 15  | Enviar formatos      | Presentar 606/607/608 a DGII  | 🔴 CRÍTICA |
| 18  | IT-1 próximo         | Calcular ITBIS a pagar        | 🔴 ALTA    |
| 20  | Enviar IT-1          | Presentar y pagar ITBIS       | 🔴 CRÍTICA |

**Backend Necesario:** 5 SP  
**Frontend Necesario:** 5 SP  
**Total:** 10 SP

---

## 💻 PROPUESTAS DE UI

### 1. Dashboard de Gastos

```typescript
// /admin/expenses

import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';

export const ExpensesDashboard = () => {
  const { data: summary } = useExpensesSummary('2026-01');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Gastos Operativos</h1>
        <Link to="/admin/expenses/register">
          <Button>
            <Plus className="mr-2 h-4 w-4" />
            Registrar Gasto
          </Button>
        </Link>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader>
            <h3 className="text-sm text-gray-500">Total Este Mes</h3>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">RD$ {summary.totalMonth.toLocaleString()}</p>
            <p className="text-sm text-gray-500 mt-1">
              {summary.countMonth} gastos registrados
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm text-gray-500">Gastos Locales</h3>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">RD$ {summary.localExpenses.toLocaleString()}</p>
            <p className="text-sm text-green-600 mt-1">
              ITBIS deducible: RD$ {summary.itbisDeductible.toLocaleString()}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm text-gray-500">Gastos Internacionales</h3>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">${summary.internationalUSD.toLocaleString()}</p>
            <p className="text-sm text-gray-500 mt-1">
              ≈ RD$ {summary.internationalDOP.toLocaleString()}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm text-gray-500">Retenciones ISR</h3>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">RD$ {summary.retentions.toLocaleString()}</p>
            <p className="text-sm text-orange-600 mt-1">
              Declarar en IR-17 (día 10)
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Calendario Fiscal */}
      <Card>
        <CardHeader>
          <h3 className="font-semibold">Próximas Obligaciones Fiscales</h3>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center justify-between p-3 bg-red-50 rounded-lg">
              <div>
                <p className="font-medium text-red-800">IR-17 - Retenciones</p>
                <p className="text-sm text-red-600">Declaración de retenciones ISR 10%</p>
              </div>
              <Badge variant="destructive">Día 10</Badge>
            </div>
            <div className="flex items-center justify-between p-3 bg-orange-50 rounded-lg">
              <div>
                <p className="font-medium text-orange-800">Formatos 606/607/608</p>
                <p className="text-sm text-orange-600">Reporte de compras y ventas</p>
              </div>
              <Badge variant="warning">Día 15</Badge>
            </div>
            <div className="flex items-center justify-between p-3 bg-yellow-50 rounded-lg">
              <div>
                <p className="font-medium text-yellow-800">IT-1 - ITBIS</p>
                <p className="text-sm text-yellow-600">Declaración mensual de ITBIS</p>
              </div>
              <Badge variant="secondary">Día 20</Badge>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Gráfico de gastos por categoría */}
      <Card>
        <CardHeader>
          <h3 className="font-semibold">Gastos por Categoría (Enero 2026)</h3>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {summary.byCategory.map((cat) => (
              <div key={cat.code}>
                <div className="flex justify-between mb-1">
                  <span className="text-sm font-medium">{cat.name}</span>
                  <span className="text-sm text-gray-600">
                    RD$ {cat.amount.toLocaleString()} ({cat.percentage}%)
                  </span>
                </div>
                <Progress value={cat.percentage} className="h-2" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Últimos gastos registrados */}
      <Card>
        <CardHeader>
          <h3 className="font-semibold">Últimos Gastos Registrados</h3>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Fecha</TableHead>
                <TableHead>Proveedor</TableHead>
                <TableHead>Categoría</TableHead>
                <TableHead>NCF/Invoice</TableHead>
                <TableHead className="text-right">Monto</TableHead>
                <TableHead>Estado</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {summary.recentExpenses.map((expense) => (
                <TableRow key={expense.id}>
                  <TableCell>{format(expense.date, 'dd/MM/yyyy')}</TableCell>
                  <TableCell>
                    {expense.provider.name}
                    {expense.type === 'INTERNATIONAL' && (
                      <Badge variant="outline" className="ml-2">🌍</Badge>
                    )}
                  </TableCell>
                  <TableCell>{expense.categoryName}</TableCell>
                  <TableCell className="font-mono text-sm">
                    {expense.ncf || expense.invoiceNumber}
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {expense.type === 'LOCAL'
                      ? `RD$ ${expense.amount.toLocaleString()}`
                      : `$${expense.amountUSD.toFixed(2)}`
                    }
                  </TableCell>
                  <TableCell>
                    <Badge variant={
                      expense.status === 'APPROVED' ? 'success' :
                      expense.status === 'PENDING' ? 'warning' : 'secondary'
                    }>
                      {expense.statusText}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
};
```

**Story Points:** 13 SP

---

### 2. Formulario de Registro de Gastos

```typescript
// /admin/expenses/register

export const ExpenseRegistrationForm = () => {
  const [expenseType, setExpenseType] = useState<'LOCAL' | 'INTERNATIONAL'>('LOCAL');
  const { data: providers } = useProviders();

  return (
    <Card className="max-w-4xl mx-auto">
      <CardHeader>
        <h2 className="text-2xl font-bold">Registrar Nuevo Gasto</h2>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Tipo de Gasto */}
          <div>
            <Label>Tipo de Gasto</Label>
            <div className="flex gap-4 mt-2">
              <Button
                type="button"
                variant={expenseType === 'LOCAL' ? 'default' : 'outline'}
                onClick={() => setExpenseType('LOCAL')}
                className="flex-1"
              >
                🇩🇴 Gasto Local (con RNC)
              </Button>
              <Button
                type="button"
                variant={expenseType === 'INTERNATIONAL' ? 'default' : 'outline'}
                onClick={() => setExpenseType('INTERNATIONAL')}
                className="flex-1"
              >
                🌍 Gasto Internacional (sin RNC)
              </Button>
            </div>
          </div>

          {expenseType === 'LOCAL' ? (
            <>
              {/* Proveedor Local */}
              <div>
                <Label>Proveedor</Label>
                <Select onValueChange={handleProviderChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Seleccionar proveedor..." />
                  </SelectTrigger>
                  <SelectContent>
                    {providers?.local.map((p) => (
                      <SelectItem key={p.id} value={p.id}>
                        {p.name} - RNC {p.rnc}
                      </SelectItem>
                    ))}
                    <SelectItem value="NEW">+ Agregar nuevo proveedor</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* NCF */}
              <div>
                <Label>Número de Comprobante Fiscal (NCF)</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder="B0100000123"
                    value={ncf}
                    onChange={(e) => setNcf(e.target.value.toUpperCase())}
                  />
                  <Button type="button" onClick={verifyNcf} disabled={verifying}>
                    {verifying ? <Loader2 className="animate-spin" /> : <CheckCircle />}
                    Verificar
                  </Button>
                </div>
                {ncfVerified && (
                  <Alert className="mt-2">
                    <CheckCircle className="h-4 w-4 text-green-600" />
                    <AlertDescription>
                      NCF válido - RNC {ncfData.rnc} - {ncfData.businessName}
                    </AlertDescription>
                  </Alert>
                )}
              </div>

              {/* Montos */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label>Subtotal (Base imponible)</Label>
                  <Input
                    type="number"
                    step="0.01"
                    value={subtotal}
                    onChange={(e) => setSubtotal(parseFloat(e.target.value))}
                  />
                </div>
                <div>
                  <Label>ITBIS 18%</Label>
                  <Input
                    type="number"
                    step="0.01"
                    value={itbis}
                    readOnly
                    className="bg-gray-50"
                  />
                </div>
              </div>

              {/* Retención ISR */}
              {shouldWithhold && (
                <Alert>
                  <AlertTriangle className="h-4 w-4" />
                  <AlertTitle>Retención ISR 10%</AlertTitle>
                  <AlertDescription>
                    Este proveedor es persona física con servicios profesionales.
                    Debe retener RD$ {retention.toLocaleString()} (10% del subtotal).
                    Declarar en IR-17 día 10.
                  </AlertDescription>
                </Alert>
              )}

              {/* Total */}
              <div className="border-t pt-4">
                <div className="flex justify-between text-lg font-bold">
                  <span>Total a Pagar al Proveedor:</span>
                  <span className="text-2xl text-blue-600">
                    RD$ {netPayable.toLocaleString()}
                  </span>
                </div>
                {shouldWithhold && (
                  <p className="text-sm text-gray-600 mt-1">
                    (Total RD$ {total.toLocaleString()} - Retención RD$ {retention.toLocaleString()})
                  </p>
                )}
              </div>
            </>
          ) : (
            <>
              {/* Proveedor Internacional */}
              <div>
                <Label>Proveedor Internacional</Label>
                <Select onValueChange={handleProviderChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Seleccionar proveedor..." />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      <SelectLabel>Hosting/Cloud</SelectLabel>
                      <SelectItem value="digital-ocean">Digital Ocean (USA)</SelectItem>
                      <SelectItem value="aws">AWS (USA)</SelectItem>
                      <SelectItem value="google-cloud">Google Cloud (USA)</SelectItem>
                    </SelectGroup>
                    <SelectGroup>
                      <SelectLabel>Desarrollo</SelectLabel>
                      <SelectItem value="github">GitHub (USA)</SelectItem>
                      <SelectItem value="jetbrains">JetBrains (Rep. Checa)</SelectItem>
                    </SelectGroup>
                    <SelectGroup>
                      <SelectLabel>Publicidad</SelectLabel>
                      <SelectItem value="google-ads">Google Ads (USA)</SelectItem>
                      <SelectItem value="facebook-ads">Facebook Ads (USA)</SelectItem>
                    </SelectGroup>
                    <SelectItem value="NEW">+ Agregar nuevo proveedor</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Invoice Number */}
              <div>
                <Label>Número de Invoice</Label>
                <Input placeholder="INV-2026-001234" />
              </div>

              {/* Montos en USD */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label>Monto en USD</Label>
                  <Input
                    type="number"
                    step="0.01"
                    placeholder="100.00"
                    value={amountUSD}
                    onChange={(e) => setAmountUSD(parseFloat(e.target.value))}
                  />
                </div>
                <div>
                  <Label>Tasa de Cambio (Banco Central)</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      step="0.01"
                      value={exchangeRate}
                      readOnly
                      className="bg-gray-50"
                    />
                    <Button type="button" size="sm" onClick={fetchExchangeRate}>
                      Actualizar
                    </Button>
                  </div>
                </div>
              </div>

              {/* Total en DOP */}
              <div className="border-t pt-4">
                <div className="flex justify-between text-lg font-bold">
                  <span>Total en RD$:</span>
                  <span className="text-2xl text-blue-600">
                    RD$ {amountDOP.toLocaleString()}
                  </span>
                </div>
                <p className="text-sm text-gray-600 mt-1">
                  ${amountUSD.toFixed(2)} USD × {exchangeRate.toFixed(2)} = RD$ {amountDOP.toLocaleString()}
                </p>
              </div>

              {/* NCF B13 Automático */}
              <Alert>
                <Info className="h-4 w-4" />
                <AlertDescription>
                  El sistema generará automáticamente un NCF B13 (gastos al exterior) para este gasto.
                  Sin ITBIS deducible (gasto internacional).
                </AlertDescription>
              </Alert>
            </>
          )}

          {/* Categoría DGII */}
          <div>
            <Label>Categoría de Gasto (DGII)</Label>
            <Select>
              <SelectTrigger>
                <SelectValue placeholder="Seleccionar categoría..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="01">01 - Gastos de Personal</SelectItem>
                <SelectItem value="02">02 - Gastos por Trabajos, Suministros y Servicios</SelectItem>
                <SelectItem value="03">03 - Arrendamientos</SelectItem>
                <SelectItem value="04">04 - Gastos de Activos Fijos</SelectItem>
                <SelectItem value="05">05 - Gastos de Representación</SelectItem>
                <SelectItem value="06">06 - Otras Deducciones Admitidas</SelectItem>
                <SelectItem value="07">07 - Gastos Financieros</SelectItem>
                <SelectItem value="08">08 - Gastos Extraordinarios</SelectItem>
                <SelectItem value="09">09 - Compras y Gastos que forman parte del Costo de Venta</SelectItem>
                <SelectItem value="10">10 - Adquisiciones de Activos</SelectItem>
                <SelectItem value="11">11 - Gastos de Seguros</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Upload de Documento */}
          <div>
            <Label>Documento de Soporte</Label>
            <div className="border-2 border-dashed rounded-lg p-6 text-center">
              <Upload className="mx-auto h-12 w-12 text-gray-400" />
              <p className="mt-2 text-sm text-gray-600">
                {expenseType === 'LOCAL'
                  ? 'Subir factura con NCF (PDF o imagen)'
                  : 'Subir invoice/receipt (PDF o imagen)'
                }
              </p>
              <input
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                className="hidden"
                id="document-upload"
                onChange={handleFileUpload}
              />
              <label htmlFor="document-upload">
                <Button type="button" variant="outline" className="mt-2">
                  Seleccionar Archivo
                </Button>
              </label>
              {file && (
                <p className="mt-2 text-sm text-green-600">
                  ✓ {file.name} ({(file.size / 1024).toFixed(2)} KB)
                </p>
              )}
            </div>
          </div>

          {/* Notas */}
          <div>
            <Label>Notas Adicionales</Label>
            <Textarea
              placeholder="Descripción del gasto, propósito, etc."
              rows={3}
            />
          </div>

          {/* Actions */}
          <div className="flex justify-between pt-4">
            <Link to="/admin/expenses">
              <Button type="button" variant="outline">
                Cancelar
              </Button>
            </Link>
            <Button type="submit" disabled={!isValid}>
              Registrar Gasto
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
};
```

**Story Points:** 21 SP

---

### 3. Generador de Formato 606

```typescript
// /admin/expenses/606

export const Format606Generator = () => {
  const [period, setPeriod] = useState({ month: 1, year: 2026 });
  const { data: preview, isLoading } = useFormat606Preview(period);

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex justify-between items-center">
            <div>
              <h2 className="text-2xl font-bold">Formato 606 - Compras</h2>
              <p className="text-gray-600">Reporte mensual de compras para DGII</p>
            </div>
            <Badge variant="destructive">Vencimiento: Día 15</Badge>
          </div>
        </CardHeader>
        <CardContent>
          {/* Selector de Período */}
          <div className="flex gap-4 mb-6">
            <div>
              <Label>Mes</Label>
              <Select value={period.month.toString()} onValueChange={(v) => setPeriod({...period, month: parseInt(v)})}>
                <SelectTrigger className="w-[180px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {[1,2,3,4,5,6,7,8,9,10,11,12].map((m) => (
                    <SelectItem key={m} value={m.toString()}>
                      {new Date(2026, m-1).toLocaleString('es-DO', { month: 'long' })}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>Año</Label>
              <Select value={period.year.toString()} onValueChange={(v) => setPeriod({...period, year: parseInt(v)})}>
                <SelectTrigger className="w-[120px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="2026">2026</SelectItem>
                  <SelectItem value="2027">2027</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Resumen */}
          {preview && (
            <>
              <div className="grid grid-cols-4 gap-4 mb-6">
                <Card>
                  <CardContent className="pt-6">
                    <p className="text-sm text-gray-500">Total Compras</p>
                    <p className="text-2xl font-bold">RD$ {preview.totalPurchases.toLocaleString()}</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-6">
                    <p className="text-sm text-gray-500">ITBIS Deducible</p>
                    <p className="text-2xl font-bold text-green-600">
                      RD$ {preview.itbisDeductible.toLocaleString()}
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-6">
                    <p className="text-sm text-gray-500">Retenciones ISR</p>
                    <p className="text-2xl font-bold text-orange-600">
                      RD$ {preview.retentions.toLocaleString()}
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-6">
                    <p className="text-sm text-gray-500">Total Líneas</p>
                    <p className="text-2xl font-bold">{preview.totalLines}</p>
                  </CardContent>
                </Card>
              </div>

              {/* Preview de Líneas */}
              <div className="mb-6">
                <h3 className="font-semibold mb-3">Preview del Archivo 606.txt</h3>
                <div className="bg-gray-900 text-gray-100 p-4 rounded-lg font-mono text-xs overflow-x-auto">
                  <pre>{preview.fileContent.slice(0, 1000)}...</pre>
                </div>
                <p className="text-sm text-gray-500 mt-2">
                  Mostrando primeras 10 líneas de {preview.totalLines}
                </p>
              </div>

              {/* Validación */}
              <Alert variant={preview.validationErrors.length === 0 ? 'success' : 'destructive'}>
                {preview.validationErrors.length === 0 ? (
                  <>
                    <CheckCircle className="h-4 w-4" />
                    <AlertTitle>Validación Exitosa ✓</AlertTitle>
                    <AlertDescription>
                      El archivo cumple con las especificaciones de DGII.
                      Listo para enviar.
                    </AlertDescription>
                  </>
                ) : (
                  <>
                    <AlertTriangle className="h-4 w-4" />
                    <AlertTitle>Errores de Validación ({preview.validationErrors.length})</AlertTitle>
                    <AlertDescription>
                      <ul className="list-disc list-inside mt-2">
                        {preview.validationErrors.slice(0, 5).map((error, i) => (
                          <li key={i}>{error}</li>
                        ))}
                      </ul>
                    </AlertDescription>
                  </>
                )}
              </Alert>

              {/* Acciones */}
              <div className="flex justify-between mt-6">
                <Button variant="outline" onClick={handleRefresh}>
                  <RefreshCcw className="mr-2 h-4 w-4" />
                  Actualizar Preview
                </Button>
                <div className="flex gap-2">
                  <Button variant="outline" onClick={handleDownload}>
                    <Download className="mr-2 h-4 w-4" />
                    Descargar 606.txt
                  </Button>
                  <Button
                    onClick={handleSendToDGII}
                    disabled={preview.validationErrors.length > 0}
                  >
                    <Send className="mr-2 h-4 w-4" />
                    Enviar a DGII
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      {/* Desglose por Tipo de Gasto */}
      <Card>
        <CardHeader>
          <h3 className="font-semibold">Desglose por Tipo de Gasto (DGII)</h3>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Código</TableHead>
                <TableHead>Tipo de Gasto</TableHead>
                <TableHead className="text-right">Cantidad</TableHead>
                <TableHead className="text-right">Subtotal</TableHead>
                <TableHead className="text-right">ITBIS</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {preview?.byCategory.map((cat) => (
                <TableRow key={cat.code}>
                  <TableCell className="font-mono">{cat.code}</TableCell>
                  <TableCell>{cat.name}</TableCell>
                  <TableCell className="text-right">{cat.count}</TableCell>
                  <TableCell className="text-right">
                    RD$ {cat.subtotal.toLocaleString()}
                  </TableCell>
                  <TableCell className="text-right">
                    RD$ {cat.itbis.toLocaleString()}
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    RD$ {cat.total.toLocaleString()}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
};
```

**Story Points:** 13 SP

---

## 📊 PLAN DE IMPLEMENTACIÓN

### Fase 1: Base de Registro de Gastos (2 semanas - 34 SP)

**Backend (21 SP):**

- Extender entidad Expense con campos DGII
- Crear entidad ExpenseProvider
- Crear entidad ExpenseDocument
- API CRUD de proveedores
- API CRUD de gastos con validaciones DGII
- Upload de documentos a S3/MediaService

**Frontend (13 SP):**

- Dashboard de gastos básico
- Formulario de registro (local e internacional)
- Lista de gastos con filtros

---

### Fase 2: Verificación NCF y Retenciones (1-2 semanas - 21 SP)

**Backend (13 SP):**

- Integración con API de DGII para verificar NCF
- Generación automática de NCF B13
- Cálculo automático de retenciones ISR 10%
- Descarga automática de tasa de cambio (Banco Central)

**Frontend (8 SP):**

- Verificador de NCF en tiempo real
- Calculadora de retenciones visual
- Indicadores de tipo de proveedor

---

### Fase 3: Formato 606 y Calendario Fiscal (1-2 semanas - 26 SP)

**Backend (16 SP):**

- Generación de Formato 606 según especificación DGII
- Validador de formato 606
- Sistema de alertas automáticas (día 3, 8, 13, 18)
- API para consultar calendario fiscal

**Frontend (10 SP):**

- Generador de Formato 606 con preview
- Dashboard de calendario fiscal
- Sistema de alertas visuales

---

### Fase 4: Reportes y Analytics (1 semana - 13 SP)

**Backend (5 SP):**

- Reportes de gastos por categoría
- Reportes de gastos por proveedor
- Export a Excel/CSV

**Frontend (8 SP):**

- Dashboard analytics con gráficos
- Comparación mes vs mes
- Exportador de reportes

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Backend

- [ ] **Extender FinanceService.Domain.Entities.Expense** (5 SP)
  - Agregar campos DGII (TipoGasto, TipoNCF, ITBISDeducible, RetencionISR, TasaCambio)
  - Agregar enum ExpenseType (LOCAL, INTERNATIONAL)
  - Separar amounts (Subtotal, ITBIS, Total, Retención, NetPayable)
  - Cambiar Currency default de "MXN" a "DOP"

- [ ] **Crear ExpenseProvider entity** (3 SP)
  - RNC/Cédula
  - Tipo (EMPRESA, PERSONA_FISICA, INTERNACIONAL)
  - País
  - Indicador de retención

- [ ] **Crear ExpenseDocument entity** (2 SP)
  - S3 URL
  - Tipo (NCF, INVOICE, STATEMENT, EXCHANGE_RATE, EVIDENCE)
  - ExpenseId foreign key

- [ ] **Crear ExpenseNcfValidation entity** (3 SP)
  - NCF verificado
  - Fecha de verificación
  - Resultado de DGII API
  - ExpenseId foreign key

- [ ] **API CRUD de proveedores** (3 SP)
  - POST /api/expenses/providers
  - GET /api/expenses/providers
  - PUT /api/expenses/providers/{id}
  - DELETE /api/expenses/providers/{id}

- [ ] **API CRUD de gastos** (5 SP)
  - POST /api/expenses (con validaciones DGII)
  - GET /api/expenses (con filtros: tipo, período, proveedor, categoría)
  - GET /api/expenses/{id}
  - PUT /api/expenses/{id}
  - DELETE /api/expenses/{id}

- [ ] **API de documentos** (3 SP)
  - POST /api/expenses/{id}/documents/upload (S3)
  - GET /api/expenses/{id}/documents
  - DELETE /api/expenses/{id}/documents/{docId}

- [ ] **Integración DGII para verificar NCF** (8 SP)
  - POST /api/dgii/verify-ncf
  - Almacenar resultado en ExpenseNcfValidation
  - Cache de verificaciones (válido por 30 días)

- [ ] **Generación automática de NCF B13** (3 SP)
  - GET /api/expenses/generate-ncf-b13
  - Secuencia automática (B1300000001, B1300000002, etc.)
  - Almacenar en tabla de secuencias

- [ ] **Generación de Formato 606** (13 SP)
  - POST /api/expenses/reports/606
  - Query params: month, year
  - Generar archivo 606.txt según especificación DGII
  - Validar formato
  - Almacenar en S3
  - Retornar URL de descarga

- [ ] **Cálculo automático de retenciones** (5 SP)
  - Lógica en dominio: shouldWithhold()
  - Calcular 10% sobre base imponible
  - Registrar en ExpenseRetention entity

- [ ] **Sistema de alertas** (5 SP)
  - Calendario fiscal automático
  - Enviar emails día 3, 8, 13, 18
  - Dashboard de próximas obligaciones

---

### Frontend

- [ ] **ExpensesDashboard.tsx** (8 SP)
  - `/admin/expenses`
  - Stats cards (total mes, locales, internacionales, retenciones)
  - Gráfico de gastos por categoría
  - Calendario fiscal
  - Últimos gastos registrados

- [ ] **ExpenseRegistrationForm.tsx** (13 SP)
  - `/admin/expenses/register`
  - Toggle LOCAL/INTERNATIONAL
  - Formulario completo con validaciones
  - Verificador de NCF en tiempo real
  - Calculadora de retenciones visual
  - Upload de documentos (drag & drop)
  - Preview de totales

- [ ] **ExpenseListPage.tsx** (5 SP)
  - `/admin/expenses/list`
  - Tabla con filtros (tipo, período, proveedor, categoría)
  - Paginación
  - Export a Excel

- [ ] **ExpenseApprovalPage.tsx** (8 SP)
  - `/admin/expenses/approval`
  - Lista de gastos pendientes de aprobación
  - Preview de documentos
  - Aprobar/Rechazar con razón
  - Notificar a quien registró

- [ ] **Format606GeneratorPage.tsx** (8 SP)
  - `/admin/expenses/606`
  - Selector de período
  - Preview del archivo 606.txt
  - Validación automática
  - Descarga de archivo
  - Envío a DGII (futuro)

- [ ] **ExpenseProvidersPage.tsx** (5 SP)
  - `/admin/expenses/providers`
  - CRUD de proveedores
  - Clasificación LOCAL/INTERNATIONAL
  - Indicador de retención

- [ ] **FiscalCalendarPage.tsx** (5 SP)
  - `/admin/expenses/calendar`
  - Vista mensual de obligaciones
  - Alertas de vencimientos
  - Checklist de tareas

---

### Integration

- [ ] **Agregar rutas en AdminSidebar.tsx** (1 SP)

  ```typescript
  {
    section: 'Finanzas',
    items: [
      { href: '/admin/expenses', label: 'Gastos Operativos', icon: FiDollarSign },
      { href: '/admin/expenses/register', label: 'Registrar Gasto', icon: FiPlus },
      { href: '/admin/expenses/approval', label: 'Aprobar Gastos', icon: FiCheckCircle },
      { href: '/admin/expenses/606', label: 'Formato 606', icon: FiFileText },
      { href: '/admin/expenses/calendar', label: 'Calendario Fiscal', icon: FiCalendar },
    ]
  }
  ```

- [ ] **Conectar frontend con backend APIs** (2 SP)
  - Crear expenseService.ts
  - Implementar métodos para todos los endpoints
  - Agregar tipos TypeScript

- [ ] **Testing** (8 SP)
  - Tests unitarios backend (Expense, ExpenseProvider, Format606Generator)
  - Tests de integración (API endpoints)
  - Tests E2E frontend (registro de gasto flow completo)

---

## 🎯 RESUMEN DE STORY POINTS

| Fase                          | Backend   | Frontend  | Total      |
| ----------------------------- | --------- | --------- | ---------- |
| **Fase 1: Base de Registro**  | 21 SP     | 13 SP     | 34 SP      |
| **Fase 2: NCF y Retenciones** | 13 SP     | 8 SP      | 21 SP      |
| **Fase 3: Formato 606**       | 16 SP     | 10 SP     | 26 SP      |
| **Fase 4: Reportes**          | 5 SP      | 8 SP      | 13 SP      |
| **Integration & Testing**     | -         | -         | 11 SP      |
| **TOTAL**                     | **55 SP** | **39 SP** | **105 SP** |

---

## 🔴 RIESGO LEGAL Y FINANCIERO

### Sin Sistema de Registro de Gastos:

**Riesgos Operacionales:**

- ❌ No se puede generar Formato 606 (obligatorio día 15 cada mes)
- ❌ No se puede deducir ITBIS de gastos operativos
- ❌ No se puede calcular correctamente IT-1
- ❌ No se pueden presentar retenciones ISR en IR-17
- ❌ No hay evidencia documental para auditorías

**Multas DGII:**

- No presentar 606: **RD$3,000-$15,000/mes** (multa acumulativa)
- No retener ISR 10%: **RD$5,000-$50,000** + intereses
- No tener documentos de soporte: **RD$1,000-$10,000** por gasto
- **Total multas anuales estimadas:** RD$144,000-$600,000

**Impacto Financiero:**

- Sin deducción de ITBIS: Pérdida de ~$13,000-$20,000 DOP/mes (~$240,000 DOP/año)
- Sin evidencia de gastos: DGII puede desconocer hasta 50% de gastos en auditoría
- Riesgo de ajuste fiscal: +30% ISR sobre utilidades

**Impacto Operacional:**

- Contador debe registrar gastos manualmente (20-30 horas/mes)
- Riesgo de errores humanos en clasificación DGII
- Imposible generar reportes en tiempo real
- No hay trazabilidad de documentos

---

## 🏆 CONCLUSIÓN

**Estado Actual:** 🔴 **5% de compliance** (1/20 requisitos)

**Riesgo Legal:** 🔴 **CRÍTICO** - Sin Formato 606, no se cumple con obligaciones DGII

**Blocker:** ✅ **SÍ** - Sin Fase 1, no se puede:

- Deducir ITBIS de gastos operativos
- Presentar Formato 606 (obligatorio día 15)
- Calcular correctamente IT-1
- Presentar retenciones ISR en IR-17
- Tener documentos listos para auditorías

**Inversión Requerida:**

- Fase 1 (CRÍTICA): 34 SP = $4,800 USD (~2 semanas)
- Fase 2: 21 SP = $3,000 USD (~1-2 semanas)
- Fase 3: 26 SP = $3,700 USD (~1-2 semanas)
- Fase 4: 13 SP = $1,800 USD (~1 semana)
- **Total:** 105 SP = **$14,700 USD** (~6-8 semanas)

**Ahorro Anual:**

- Multas evitadas: RD$144K-$600K (~$2,400-$10,000 USD)
- ITBIS recuperado: RD$240K (~$4,000 USD)
- Tiempo contador ahorrado: 300 horas/año × $30/hora = $9,000 USD
- **Total ahorro anual:** $15,400-$23,000 USD

**ROI:** Recuperación de inversión en 8-12 meses

---

## 🚨 RECOMENDACIÓN FINAL

**Implementar URGENTEMENTE Fase 1 + 2 (55 SP) en próximas 3-4 semanas:**

1. **Semana 1-2:** Fase 1 - Base de registro de gastos (34 SP)
2. **Semana 3-4:** Fase 2 - Verificación NCF y retenciones (21 SP)
3. **Semana 5-6:** Fase 3 - Formato 606 y calendario fiscal (26 SP)

Sin la Fase 1, OKLA no puede:

- Cumplir con obligaciones mensuales de DGII
- Deducir ITBIS de ~$200K DOP/mes de gastos
- Evitar multas de ~$12K-$50K USD/mes
- Tener información lista para auditorías

**Prioridad:** 🔴 **P0 - BLOCKER OPERACIONAL Y LEGAL**

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/registro-gastos.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Registro de Gastos", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar lista de gastos", async ({ page }) => {
    await page.goto("/admin/gastos");

    await expect(page.getByTestId("gastos-list")).toBeVisible();
  });

  test("debe registrar nuevo gasto", async ({ page }) => {
    await page.goto("/admin/gastos/nuevo");

    await page.fill('input[name="description"]', "Hosting Digital Ocean");
    await page.fill('input[name="amount"]', "5000");
    await page.getByRole("combobox", { name: /categoría/i }).click();
    await page.getByRole("option", { name: /tecnología/i }).click();
    await page.getByRole("button", { name: /guardar/i }).click();

    await expect(page.getByText(/gasto registrado/i)).toBeVisible();
  });

  test("debe subir factura de proveedor", async ({ page }) => {
    await page.goto("/admin/gastos/nuevo");

    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles("./fixtures/factura.pdf");

    await expect(page.getByTestId("file-preview")).toBeVisible();
  });

  test("debe ver reporte de gastos por categoría", async ({ page }) => {
    await page.goto("/admin/gastos/reporte");

    await expect(page.getByTestId("gastos-by-category")).toBeVisible();
  });
});
```

---

**Documento creado:** Enero 29, 2026  
**Próxima revisión:** Febrero 15, 2026 (post-implementación Fase 1)  
**Responsable:** Equipo de Desarrollo + Contador  
**Aprobado por:** Gregory Moreno (CEO OKLA S.R.L.)
