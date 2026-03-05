---
title: "AUDITORÍA: Automatización de Reportes DGII - OKLA S.R.L."
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["BillingService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 🤖 AUDITORÍA: Automatización de Reportes DGII - OKLA S.R.L.

**Empresa:** OKLA S.R.L.  
**RNC:** 1-33-32590-1  
**Fecha de Auditoría:** Enero 29, 2026  
**Documento Base:** `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/12-AUTOMATIZACION-REPORTES-DGII.md`  
**Auditor:** AI Development Team

---

## 📊 RESUMEN EJECUTIVO

### Estado General

| Aspecto                | Estado     | Cobertura | Diagnóstico                                 |
| ---------------------- | ---------- | --------- | ------------------------------------------- |
| **Backend Automation** | 🟡 PARCIAL | **15%**   | SchedulerService existe, NO tiene jobs DGII |
| **Frontend Dashboard** | 🔴 CRÍTICO | **5%**    | Hooks existen, UI NO EXISTE                 |
| **Jobs Automatizados** | 🔴 CRÍTICO | **0%**    | NINGÚN job DGII implementado                |
| **DGII Integration**   | 🔴 CRÍTICO | **0%**    | NO existe DGIIService                       |
| **Overall Compliance** | 🔴 CRÍTICO | **8%**    | **SISTEMA DE AUTOMATIZACIÓN NO FUNCIONAL**  |

### Impacto del Problema

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   SIN AUTOMATIZACIÓN DGII                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🔴 PROBLEMAS ACTUALES:                                                 │
│  ════════════════════════                                               │
│                                                                         │
│  • ⏰ 8-12 HORAS/MES de trabajo manual del contador                     │
│  • 💸 $1,200 USD/año en costos de contador (tiempo desperdiciado)      │
│  • ⚠️ ALTO RIESGO de errores humanos en Excel                          │
│  • 📉 NO hay visibilidad del estado fiscal en tiempo real               │
│  • 🚨 RIESGO de olvidar deadlines → multas RD$3K-$15K/mes               │
│  • 📊 NO hay dashboard para administradores                             │
│  • 🔄 NO hay recordatorios automáticos de vencimientos                  │
│  • 🧮 Cálculo manual de ITBIS → posible sobre/sub pago                  │
│                                                                         │
│  🟢 CON AUTOMATIZACIÓN (Propuesta):                                     │
│  ═══════════════════════════════                                        │
│                                                                         │
│  • ⏱️ 30 MINUTOS/MES de revisión (reducción 95%)                        │
│  • 📧 Recordatorios automáticos 3 días antes de deadlines               │
│  • 🤖 Generación automática de Formatos 606/607/608                     │
│  • 📊 Dashboard en tiempo real con estado fiscal                        │
│  • ✅ Validaciones automáticas antes de envío                           │
│  • 🔔 Alertas de secuencias NCF agotándose                              │
│  • 💾 Backups automáticos de datos fiscales                             │
│  • 📈 ROI: $1,200/año ahorrados + reducción de riesgo                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Riesgo Financiero

| Concepto               | Sin Automatización               | Con Automatización          | Ahorro Anual       |
| ---------------------- | -------------------------------- | --------------------------- | ------------------ |
| **Tiempo contador**    | 12h/mes × $100/h = $1,200/año    | 0.5h/mes × $100/h = $50/año | **$1,150**         |
| **Multas por olvido**  | ~$500/año (riesgo promedio)      | $0 (recordatorios auto)     | **$500**           |
| **Errores de cálculo** | ~$300/año (sobre/sub pago ITBIS) | $0 (cálculo automático)     | **$300**           |
| **Tiempo admin**       | 3h/mes × $50/h = $150/año        | 0h/mes = $0                 | **$150**           |
| **TOTAL AHORRO**       | -                                | -                           | **$2,100 USD/año** |

### Inversión Requerida

| Fase       | Descripción           | Story Points | Tiempo Estimado | Costo       |
| ---------- | --------------------- | ------------ | --------------- | ----------- |
| **Fase 1** | DGIIService Backend   | 34 SP        | 2-3 semanas     | $4,760      |
| **Fase 2** | Jobs Automatizados    | 21 SP        | 1-2 semanas     | $2,940      |
| **Fase 3** | Dashboard Frontend    | 26 SP        | 1-2 semanas     | $3,640      |
| **Fase 4** | Integration & Testing | 13 SP        | 1 semana        | $1,820      |
| **TOTAL**  | **Sistema Completo**  | **94 SP**    | **6-8 semanas** | **$13,160** |

**ROI:** 6.2 años (inversión $13,160 / ahorro $2,100/año)

**NOTA:** ROI es largo pero **el valor principal es reducción de riesgo de multas (hasta $15K/mes) y mejora operativa (95% menos tiempo manual).**

---

## 🔍 ANÁLISIS DETALLADO DEL BACKEND

### 1. SchedulerService (EXISTE - 15% Funcional)

**Ubicación:** `backend/SchedulerService/`

#### ✅ Lo que SÍ Existe

```
SchedulerService/
├── SchedulerService.Infrastructure/
│   ├── DependencyInjection.cs ✅ (Hangfire configurado)
│   ├── Jobs/
│   │   ├── IScheduledJob.cs ✅ (Interface base)
│   │   ├── CleanupOldExecutionsJob.cs ✅
│   │   ├── DailyStatsReportJob.cs ✅
│   │   └── HealthCheckJob.cs ✅
│   ├── Services/
│   │   ├── HangfireJobScheduler.cs ✅
│   │   └── JobExecutionEngine.cs ✅
│   └── Repositories/ ✅
│       ├── JobRepository.cs
│       └── JobExecutionRepository.cs
```

**Análisis:**

- ✅ Hangfire está configurado y funcionando
- ✅ Infrastructure completa para jobs recurrentes
- ✅ 3 jobs de ejemplo implementados
- ❌ **PERO: NINGÚN job relacionado con DGII**
- ❌ **NO existe integración con reportes fiscales**

#### ❌ Lo que NO Existe (CRÍTICO)

```
❌ FALTANTE COMPLETO:

Jobs DGII (Todos necesarios):
├── ❌ IR17ReminderJob.cs (día 8 de cada mes)
├── ❌ Format606PreviewJob.cs (día 10 de cada mes)
├── ❌ FormatsReminderJob.cs (día 13 de cada mes)
├── ❌ IT1ReminderJob.cs (día 18 de cada mes)
├── ❌ NCFSequenceCheckJob.cs (diario 7:00 AM)
└── ❌ FiscalDataBackupJob.cs (semanal, domingos 2:00 AM)

Servicios DGII:
├── ❌ Format606GeneratorService.cs
├── ❌ Format607GeneratorService.cs
├── ❌ Format608GeneratorService.cs
├── ❌ IR17GeneratorService.cs
├── ❌ IT1CalculatorService.cs
└── ❌ NCFGeneratorService.cs
```

### 2. ReportingService (EXISTE - 10% Funcional)

**Ubicación:** `backend/ReportingService/`

#### ✅ Lo que SÍ Existe

```csharp
// ReportingService.Domain/Entities/ReportingEntities.cs
public enum ReportType
{
    // DGII Reports
    DGII_606 = 10,  // Formato 606 (Compras)
    DGII_607 = 11,  // Formato 607 (Ventas)
    DGII_608 = 12,  // Formato 608 (Anulaciones)
    // ...otros
}

// ReportingService.Domain/Entities/DGIISubmission.cs
public class DGIISubmission
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string SubmissionType { get; set; }  // 606, 607, 608, IR17, IT1
    public string Period { get; set; }          // YYYYMM
    public DateTime SubmittedAt { get; set; }
    // ...más propiedades básicas
}

// ReportingService.Api/Controllers/DGIIController.cs (parcial)
[ApiController]
[Route("api/dgii")]
public class DGIIController : ControllerBase
{
    // Endpoint básico para obtener submissions
    [HttpGet("submissions/{period}")]
    public async Task<ActionResult> GetSubmissionsByPeriod(string period)
    {
        var result = await _mediator.Send(new GetDGIISubmissionsByPeriodQuery(period, null));
        return Ok(result);
    }
}
```

**Análisis:**

- ✅ Entidad `DGIISubmission` existe (tracking de envíos)
- ✅ Enum `ReportType` incluye 606/607/608
- ✅ Controller básico con 1 endpoint
- ✅ Repository con queries básicas
- ❌ **NO genera archivos de formatos**
- ❌ **NO calcula reportes automáticamente**
- ❌ **NO tiene lógica de negocio DGII**

#### ❌ Lo que NO Existe (CRÍTICO)

```
GENERADORES DE REPORTES (0%):
├── ❌ Format606GeneratorService.cs
│   └── NO genera archivo TXT con estructura DGII
│   └── NO mapea gastos a líneas de 606
│   └── NO aplica códigos de tipo de gasto (01-11)
│   └── NO formatea RNC con ceros a la izquierda
│
├── ❌ Format607GeneratorService.cs
│   └── NO genera archivo TXT de ventas
│   └── NO mapea invoices a líneas de 607
│   └── NO calcula totales por tipo de ingreso
│
├── ❌ Format608GeneratorService.cs
│   └── NO genera archivo TXT de anulaciones
│   └── NO incluye notas de crédito
│
├── ❌ IR17GeneratorService.cs
│   └── NO calcula retenciones ISR del mes
│   └── NO genera archivo para DGII
│
└── ❌ IT1CalculatorService.cs
    └── NO calcula ITBIS cobrado - ITBIS pagado
    └── NO pre-llena formulario IT-1

VALIDADORES (0%):
├── ❌ DGIIFormatValidator.cs
│   └── NO valida estructura de archivos 606/607/608
│   └── NO verifica NCF format (11 caracteres)
│   └── NO valida RNC format
│
└── ❌ NCFVerificationService.cs
    └── NO verifica NCF con DGII
    └── NO hace scraping de página DGII
```

### 3. DGIIService (NO EXISTE - 0%)

**Estado:** **SERVICIO COMPLETO FALTANTE**

```
❌ DGIIService/ (TODO POR CREAR)
├── DGIIService.Api/
│   ├── Controllers/
│   │   ├── ❌ Format606Controller.cs
│   │   ├── ❌ Format607Controller.cs
│   │   ├── ❌ Format608Controller.cs
│   │   ├── ❌ IR17Controller.cs
│   │   ├── ❌ IT1Controller.cs
│   │   ├── ❌ NCFController.cs
│   │   └── ❌ ReportsController.cs
│   ├── ❌ Program.cs
│   └── ❌ Dockerfile
│
├── DGIIService.Application/
│   ├── Features/
│   │   ├── Format606/
│   │   │   ├── ❌ GenerateFormat606Command.cs
│   │   │   ├── ❌ ValidateFormat606Query.cs
│   │   │   └── ❌ Format606Dto.cs
│   │   ├── ❌ Format607/ (misma estructura)
│   │   ├── ❌ Format608/ (misma estructura)
│   │   ├── ❌ IR17/
│   │   └── ❌ NCF/
│   │
│   ├── Services/
│   │   ├── ❌ NCFGeneratorService.cs
│   │   ├── ❌ Format606GeneratorService.cs
│   │   ├── ❌ Format607GeneratorService.cs
│   │   ├── ❌ ITBISCalculatorService.cs
│   │   └── ❌ DGIIValidatorService.cs
│   │
│   └── DTOs/ (❌ Todos faltantes)
│
├── DGIIService.Domain/
│   ├── Entities/
│   │   ├── ❌ NCFSequence.cs
│   │   ├── ❌ NCFIssued.cs
│   │   ├── ❌ NCFReceived.cs
│   │   ├── ❌ DGIIFormat.cs
│   │   └── ❌ FiscalPeriod.cs
│   │
│   ├── Enums/
│   │   ├── ❌ NCFType.cs
│   │   ├── ❌ ExpenseType.cs (códigos 01-11 DGII)
│   │   └── ❌ FormatStatus.cs
│   │
│   └── Interfaces/ (❌ Todos faltantes)
│
└── DGIIService.Infrastructure/
    ├── Persistence/
    │   ├── ❌ DGIIDbContext.cs
    │   └── Repositories/ (❌ Todos faltantes)
    │
    ├── External/
    │   └── ❌ DGIIApiClient.cs (scraping NCF)
    │
    └── FileGenerators/
        ├── ❌ Format606FileGenerator.cs
        ├── ❌ Format607FileGenerator.cs
        └── ❌ Format608FileGenerator.cs
```

**Impacto:**

- 🔴 **SIN este servicio, NO se puede automatizar nada**
- 🔴 Contador debe seguir usando Excel manualmente
- 🔴 Riesgo alto de errores humanos
- 🔴 NO hay trazabilidad de operaciones fiscales

---

## 🔍 ANÁLISIS DETALLADO DEL FRONTEND

### Estado Actual: 5% Cobertura

**Ubicación:** `frontend/web/src/`

#### ✅ Lo que SÍ Existe (5%)

```typescript
// 1. Hook genérico para reportes DGII (frontend/web/src/hooks/useInvoices.ts)
export function useDGIIReports(year?: number, month?: number) {
  return useQuery({
    queryKey: invoiceKeys.dgiiReports({ year, month }),
    queryFn: () => invoicingService.getDGIIReports({ year, month }),
  });
}

// 2. Servicio para fetch de reportes (frontend/web/src/services/invoicingService.ts)
export async function getDGIIReports(params?: {
  year?: number;
  month?: number;
}): Promise<DGIIReport[]> {
  const response = await apiClient.get("/api/dgii/reports", { params });
  return response.data;
}
```

**Análisis:**

- ✅ Hook `useDGIIReports()` existe
- ✅ Servicio `getDGIIReports()` existe
- ✅ TanStack Query configurado correctamente
- ❌ **PERO: NO hay UI que use estos hooks**
- ❌ **NO hay páginas de dashboard fiscal**
- ❌ **NO hay componentes de visualización**

#### ❌ Lo que NO Existe (95% Faltante)

**PÁGINAS PRINCIPALES (0%):**

```
❌ NO EXISTE:

/admin/fiscal/dashboard ← Dashboard fiscal principal
    └── src/pages/admin/FiscalDashboard.tsx ❌

/admin/fiscal/calendar ← Calendario de obligaciones
    └── src/pages/admin/FiscalCalendar.tsx ❌

/admin/fiscal/606 ← Generador Formato 606
    └── src/pages/admin/Format606Page.tsx ❌

/admin/fiscal/607 ← Generador Formato 607
    └── src/pages/admin/Format607Page.tsx ❌

/admin/fiscal/608 ← Generador Formato 608
    └── src/pages/admin/Format608Page.tsx ❌

/admin/fiscal/ir17 ← Reporte IR-17
    └── src/pages/admin/IR17Page.tsx ❌

/admin/fiscal/it1 ← Cálculo IT-1 (ITBIS)
    └── src/pages/admin/IT1Page.tsx ❌

/admin/fiscal/ncf ← Monitor secuencias NCF
    └── src/pages/admin/NCFMonitor.tsx ❌

/admin/fiscal/scheduler ← Configuración de jobs
    └── src/pages/admin/ReportSchedulerPage.tsx ❌
```

**COMPONENTES (0%):**

```typescript
// ❌ TODOS FALTANTES:

// Tarjetas de estado
❌ src/components/admin/fiscal/ReportStatusCard.tsx
❌ src/components/admin/fiscal/NCFSequenceCard.tsx
❌ src/components/admin/fiscal/DeadlineAlertCard.tsx

// Generadores de reportes
❌ src/components/admin/fiscal/Format606Generator.tsx
❌ src/components/admin/fiscal/Format607Generator.tsx
❌ src/components/admin/fiscal/Format608Generator.tsx

// Visualización de datos
❌ src/components/admin/fiscal/FiscalCalendar.tsx
❌ src/components/admin/fiscal/MonthlyStatsChart.tsx
❌ src/components/admin/fiscal/ITBISCalculator.tsx

// Utilidades
❌ src/components/admin/fiscal/FormatPreviewModal.tsx
❌ src/components/admin/fiscal/DownloadFormatButton.tsx
❌ src/components/admin/fiscal/NCFVerificationInput.tsx
```

**SERVICIOS (95% Faltante):**

```typescript
// ❌ MÉTODOS FALTANTES en dgiiService.ts:

// Generación de reportes
❌ generateFormat606(year: number, month: number)
❌ generateFormat607(year: number, month: number)
❌ generateFormat608(year: number, month: number)
❌ generateIR17(year: number, month: number)
❌ calculateIT1(year: number, month: number)

// Preview de reportes
❌ previewFormat606(year: number, month: number)
❌ previewFormat607(year: number, month: number)

// Descarga de archivos
❌ downloadFormat(formatId: string, formatType: string)

// Estado de reportes
❌ getMonthStatus(year: number, month: number)
❌ getUpcomingDeadlines()

// NCF Management
❌ getNCFSequenceStatus()
❌ verifyNCF(ncf: string, rnc: string)
❌ generateNCF(type: string)

// Scheduler
❌ getJobStatus(jobName: string)
❌ triggerJob(jobName: string)
❌ getJobHistory(jobName: string)
```

---

## 📋 REQUISITOS DGII NO IMPLEMENTADOS

### 1. Generación Automática de Formato 606 (Compras)

**Prioridad:** 🔴 CRÍTICA  
**Complejidad:** 21 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Sistema debe:
1. Obtener todos los gastos aprobados del período (año/mes)
2. Filtrar gastos por tipo (locales, internacionales, excluir nómina)
3. Transformar cada gasto a línea 606 con:
   - RNC/Cédula proveedor (con ceros a la izquierda, 11 caracteres)
   - Tipo de gasto (códigos DGII 01-11)
   - NCF recibido (11 caracteres)
   - Fechas (YYYYMMDD)
   - Montos: Servicios, Bienes, Total, ITBIS, Retenciones
   - Forma de pago
4. Generar archivo TXT con formato DGII:
   - Línea encabezado: 606|RNC|YYYYMM|RecordCount
   - Líneas de detalle separadas por pipe (|)
   - Encoding: UTF-8 sin BOM
5. Validar formato antes de subir
6. Subir a S3: /dgii-reports/606/YYYY/MM/606_RNC_YYYYMM.txt
7. Guardar registro en dgii_formats table
8. Marcar gastos como included_in_606 = true
```

**Backend Faltante:**

```csharp
// ❌ DGIIService.Application/Services/Format606GeneratorService.cs (500 líneas)

public class Format606GeneratorService : IFormat606GeneratorService
{
    // ❌ Task<Format606Result> GenerateAsync(int year, int month)
    // ❌ Task<Format606Preview> GetPreviewAsync(int year, int month)
    // ❌ Format606Line MapToFormat606Line(Expense, ExpenseProvider)
    // ❌ string GenerateFileContent(header, lines)
    // ❌ ValidationResult ValidateFormat(fileContent)
    // ❌ string FormatRNC(string rnc) // Ceros a la izquierda
    // ❌ string FormatAmount(decimal amount) // F2 format
}

// ❌ DGIIService.Api/Controllers/Format606Controller.cs

[ApiController]
[Route("api/dgii/606")]
public class Format606Controller : ControllerBase
{
    // ❌ [HttpGet("preview")] GetPreview(year, month)
    // ❌ [HttpPost("generate")] Generate(request)
    // ❌ [HttpGet("{formatId}/download")] Download(formatId)
    // ❌ [HttpGet("history")] GetHistory(year?)
}
```

**Frontend Faltante:**

```typescript
// ❌ src/pages/admin/Format606Page.tsx (400 líneas)

export const Format606Page = () => {
  // Estados:
  // - year/month selector
  // - preview data
  // - generated formats history
  // - loading states
  // Funciones:
  // - handlePreview() → GET /api/dgii/606/preview
  // - handleGenerate() → POST /api/dgii/606/generate
  // - handleDownload() → GET /api/dgii/606/{id}/download
  // UI:
  // - DateRangePicker (año/mes)
  // - PreviewTable con líneas de 606
  // - Stats cards (total gastos, total ITBIS, etc.)
  // - GenerateButton
  // - HistoryTable con formatos generados
  // - DownloadButton por cada formato
};
```

### 2. Generación Automática de Formato 607 (Ventas)

**Prioridad:** 🔴 CRÍTICA  
**Complejidad:** 13 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Sistema debe:
1. Obtener todas las facturas emitidas del período con NCF válido
2. Transformar cada invoice a línea 607 con:
   - RNC/Cédula cliente
   - NCF emitido (E31, E32, E34)
   - Tipo de ingreso (02 para servicios de OKLA)
   - Monto facturado, ITBIS, retenciones
   - Formas de pago (efectivo, tarjeta, crédito, etc.)
3. Generar archivo TXT con formato DGII
4. Validar y subir a S3
5. Guardar registro en dgii_formats
```

**Backend Faltante:**

```csharp
// ❌ DGIIService.Application/Services/Format607GeneratorService.cs (400 líneas)

public class Format607GeneratorService : IFormat607GeneratorService
{
    // ❌ Task<Format607Result> GenerateAsync(int year, int month)
    // ❌ Format607Line MapToFormat607Line(Invoice invoice)
    // ❌ string GenerateFileContent(year, month, lines)
}
```

**Frontend Faltante:**

```typescript
// ❌ src/pages/admin/Format607Page.tsx (350 líneas)
// Similar a Format606Page pero para ventas
```

### 3. Generación Automática de Formato 608 (Anulaciones)

**Prioridad:** 🟡 MEDIA  
**Complejidad:** 8 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Sistema debe:
1. Obtener notas de crédito (NCF E34) del período
2. Incluir NCF original anulado + NCF de la nota de crédito
3. Generar archivo TXT con formato DGII
4. Validar y subir a S3
```

**Backend Faltante:**

```csharp
// ❌ DGIIService.Application/Services/Format608GeneratorService.cs (250 líneas)
```

**Frontend Faltante:**

```typescript
// ❌ src/pages/admin/Format608Page.tsx (250 líneas)
```

### 4. Cálculo y Reporte IR-17 (Retenciones ISR)

**Prioridad:** 🟡 MEDIA  
**Complejidad:** 13 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Sistema debe:
1. Obtener gastos del mes con retención ISR (10%)
2. Calcular total retenido por proveedor
3. Generar archivo para envío a DGII
4. Vence: día 10 de cada mes
```

**Backend Faltante:**

```csharp
// ❌ DGIIService.Application/Services/IR17GeneratorService.cs (300 líneas)
```

**Frontend Faltante:**

```typescript
// ❌ src/pages/admin/IR17Page.tsx (300 líneas)
```

### 5. Cálculo IT-1 (ITBIS Mensual)

**Prioridad:** 🟡 MEDIA  
**Complejidad:** 13 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Sistema debe:
1. Calcular ITBIS cobrado (de facturas emitidas)
2. Calcular ITBIS pagado (de gastos)
3. Calcular balance: Cobrado - Pagado
4. Pre-llenar formulario IT-1 para DGII
5. Vence: día 20 de cada mes
```

**Backend Faltante:**

```csharp
// ❌ DGIIService.Application/Services/ITBISCalculatorService.cs (350 líneas)

public class ITBISCalculatorService
{
    // ❌ Task<ITBISCalculation> CalculateAsync(int year, int month)
    // ❌ decimal CalculateITBISCobrado() // De facturas
    // ❌ decimal CalculateITBISPagado() // De gastos
    // ❌ decimal CalculateBalance()
    // ❌ IT1Form PreFillIT1()
}
```

**Frontend Faltante:**

```typescript
// ❌ src/pages/admin/IT1Page.tsx (400 líneas)

export const IT1Page = () => {
  // UI:
  // - ITBISCobrado card (monto + breakdown por tipo de venta)
  // - ITBISPagado card (monto + breakdown por tipo de gasto)
  // - Balance card (a pagar o crédito fiscal)
  // - PreFilledFormPreview (simulación de IT-1)
  // - ExportButton (PDF o copiar para DGII)
};
```

### 6. Dashboard Fiscal con Calendario

**Prioridad:** 🔴 CRÍTICA  
**Complejidad:** 26 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Dashboard debe mostrar:
1. Grid de estado de reportes del mes (606, 607, 608, IR-17, IT-1)
2. Alertas de próximos vencimientos
3. Estado de secuencias NCF (disponibles, porcentaje, alertas)
4. Calendario fiscal con eventos del mes:
   - Día 3: Cierre de mes anterior
   - Día 5: TSS
   - Día 10: IR-17
   - Día 15: 606/607/608
   - Día 20: IT-1
5. Stats cards: Total gastos, total ITBIS pagado, retenciones, etc.
```

**Frontend Faltante:**

```typescript
// ❌ src/pages/admin/FiscalDashboard.tsx (600 líneas)

export const FiscalDashboard = () => {
  // Queries:
  const { data: reportStatus } = useQuery(["fiscal-status", year, month]);
  const { data: alerts } = useQuery(["fiscal-alerts"]);
  const { data: ncfStatus } = useQuery(["ncf-sequences"]);

  // UI Components:
  // - DeadlineAlerts (próximos vencimientos con countdown)
  // - ReportStatusGrid (4 cards: 606, 607, IR-17, IT-1)
  // - NCFSequenceMonitor (disponibles, % usado, alertas)
  // - FiscalCalendar (calendario con eventos del mes)
  // - QuickActions (botones para generar reportes)
};

// ❌ src/components/admin/fiscal/ReportStatusCard.tsx (150 líneas)
// ❌ src/components/admin/fiscal/NCFSequenceCard.tsx (120 líneas)
// ❌ src/components/admin/fiscal/DeadlineAlertCard.tsx (100 líneas)
// ❌ src/components/admin/fiscal/FiscalCalendar.tsx (350 líneas)
```

### 7. Jobs Automatizados (Recordatorios)

**Prioridad:** 🔴 CRÍTICA  
**Complejidad:** 21 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Jobs necesarios:

1. IR17ReminderJob (día 8 cada mes, 9:00 AM)
   → Enviar email a admin@okla.com.do y contador@okla.com.do
   → "⏰ IR-17 vence en 2 días (día 10)"

2. Format606PreviewJob (día 10 cada mes, 8:00 AM)
   → Generar preview automático de 606
   → Enviar email con stats: X gastos, RD$Y ITBIS

3. FormatsReminderJob (día 13 cada mes, 9:00 AM)
   → Recordar envío de 606/607/608
   → "📊 Formatos DGII vencen en 2 días (día 15)"

4. IT1ReminderJob (día 18 cada mes, 9:00 AM)
   → Recordar IT-1
   → "💰 IT-1 vence en 2 días (día 20)"

5. NCFSequenceCheckJob (diario 7:00 AM)
   → Verificar secuencias NCF
   → Si quedan < 100 → Alertar admin

6. FiscalDataBackupJob (domingos 2:00 AM)
   → Backup de datos fiscales a S3
   → /backups/fiscal/YYYY-MM-DD.sql.gz
```

**Backend Faltante:**

```csharp
// ❌ SchedulerService/Jobs/DGIIJobs/ (TODO POR CREAR)

// ❌ IR17ReminderJob.cs (100 líneas)
public class IR17ReminderJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // 1. Calcular días hasta deadline (día 10)
        // 2. Generar preview de IR-17
        // 3. Enviar email con:
        //    - Total retenciones del mes
        //    - Número de proveedores con retención
        //    - Link a dashboard: /admin/fiscal/ir17
    }
}

// ❌ Format606PreviewJob.cs (150 líneas)
// ❌ FormatsReminderJob.cs (120 líneas)
// ❌ IT1ReminderJob.cs (130 líneas)
// ❌ NCFSequenceCheckJob.cs (140 líneas)
// ❌ FiscalDataBackupJob.cs (80 líneas)
```

**Configuración Faltante:**

```csharp
// ❌ SchedulerService/Configuration/DGIIJobsConfiguration.cs

public static class DGIIJobsConfiguration
{
    public static void ConfigureJobs(IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            // Job: IR-17 Reminder (día 8, 9:00 AM)
            q.AddJob<IR17ReminderJob>(opts => opts.WithIdentity("ir17-reminder"));
            q.AddTrigger(opts => opts
                .ForJob("ir17-reminder")
                .WithCronSchedule("0 0 9 8 * ?"));

            // Job: 606 Preview (día 10, 8:00 AM)
            q.AddJob<Format606PreviewJob>(opts => opts.WithIdentity("606-preview"));
            q.AddTrigger(opts => opts
                .ForJob("606-preview")
                .WithCronSchedule("0 0 8 10 * ?"));

            // Job: Formats Reminder (día 13, 9:00 AM)
            q.AddJob<FormatsReminderJob>(opts => opts.WithIdentity("formats-reminder"));
            q.AddTrigger(opts => opts
                .ForJob("formats-reminder")
                .WithCronSchedule("0 0 9 13 * ?"));

            // Job: IT-1 Reminder (día 18, 9:00 AM)
            q.AddJob<IT1ReminderJob>(opts => opts.WithIdentity("it1-reminder"));
            q.AddTrigger(opts => opts
                .ForJob("it1-reminder")
                .WithCronSchedule("0 0 9 18 * ?"));

            // Job: NCF Check (diario 7:00 AM)
            q.AddJob<NCFSequenceCheckJob>(opts => opts.WithIdentity("ncf-check"));
            q.AddTrigger(opts => opts
                .ForJob("ncf-check")
                .WithCronSchedule("0 0 7 * * ?"));

            // Job: Backup (domingos 2:00 AM)
            q.AddJob<FiscalDataBackupJob>(opts => opts.WithIdentity("fiscal-backup"));
            q.AddTrigger(opts => opts
                .ForJob("fiscal-backup")
                .WithCronSchedule("0 0 2 ? * SUN"));
        });
    }
}
```

**Frontend para Scheduler:**

```typescript
// ❌ src/pages/admin/ReportSchedulerPage.tsx (450 líneas)

export const ReportSchedulerPage = () => {
  // Query para estado de jobs
  const { data: jobs } = useQuery(["scheduler-jobs"]);

  // UI:
  // - Tabla de jobs con:
  //   * Nombre del job
  //   * Schedule (cron expression en texto legible)
  //   * Última ejecución (timestamp + resultado)
  //   * Próxima ejecución (timestamp + countdown)
  //   * Estado (enabled/disabled)
  //   * Acciones (trigger manual, ver historial)
  // - JobHistoryModal (últimas 20 ejecuciones)
  // - EnableDisableToggle
  // - TriggerManuallyButton
};
```

### 8. Verificación de NCF con DGII

**Prioridad:** 🟡 MEDIA  
**Complejidad:** 8 SP  
**Estado:** 0% - NO EXISTE

**Requisito:**

```
Sistema debe:
1. Verificar NCF recibidos con página de DGII
2. Extraer: Razón Social, RNC, Estado (Vigente/Anulado)
3. Guardar resultado de verificación
4. Usar para validar gastos antes de incluir en 606
```

**Backend Faltante:**

```csharp
// ❌ DGIIService.Infrastructure/External/DGIIVerificationService.cs (200 líneas)

public class DGIIVerificationService : IDGIIVerificationService
{
    // Usando Playwright para scraping
    public async Task<NCFVerificationResult> VerifyAsync(string ncf, string rnc)
    {
        // 1. Navegar a: https://dgii.gov.do/herramientas/consultas/Paginas/ncf.aspx
        // 2. Llenar formulario con RNC y NCF
        // 3. Submit y esperar resultado
        // 4. Parsear HTML de respuesta
        // 5. Retornar: IsValid, BusinessName, Status
    }
}
```

**Frontend Faltante:**

```typescript
// ❌ src/components/admin/fiscal/NCFVerificationInput.tsx (180 líneas)

export const NCFVerificationInput = ({ onVerified }: Props) => {
  // Input de NCF con botón "Verificar"
  // Loader mientras verifica
  // Badge de resultado: ✅ Válido | ❌ Inválido
  // Mostrar razón social si válido
};
```

---

## 💻 PROPUESTAS DE IMPLEMENTACIÓN

### Propuesta 1: Dashboard Fiscal Principal

**Descripción:** Dashboard central para visualizar estado fiscal del mes y próximos vencimientos.

**Complejidad:** 13 SP

**Código TypeScript/React:**

```typescript
// frontend/web/src/pages/admin/FiscalDashboard.tsx

import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import {
  Calendar,
  FileText,
  AlertTriangle,
  CheckCircle,
  Clock,
  TrendingUp,
  TrendingDown,
  Download
} from 'lucide-react';
import { dgiiService } from '@/services/dgiiService';

interface ReportStatus {
  format606: {
    status: 'NOT_STARTED' | 'PREVIEW' | 'GENERATED' | 'SUBMITTED';
    recordCount: number;
    totalAmount: number;
    totalITBIS: number;
    generatedAt?: string;
  };
  format607: {
    status: 'NOT_STARTED' | 'PREVIEW' | 'GENERATED' | 'SUBMITTED';
    recordCount: number;
    totalInvoiced: number;
    totalITBIS: number;
    generatedAt?: string;
  };
  ir17: {
    status: 'NOT_STARTED' | 'CALCULATED' | 'SUBMITTED';
    totalRetentions: number;
    providerCount: number;
  };
  it1: {
    status: 'NOT_STARTED' | 'CALCULATED' | 'SUBMITTED';
    itbisCobrado: number;
    itbisPagado: number;
    balance: number;
  };
}

interface DeadlineAlert {
  name: string;
  deadline: string;
  daysRemaining: number;
  status: 'OK' | 'WARNING' | 'CRITICAL';
}

interface NCFSequence {
  type: string;
  prefix: string;
  remaining: number;
  total: number;
  percentage: number;
}

export const FiscalDashboard = () => {
  const currentDate = new Date();
  const [selectedYear] = useState(currentDate.getFullYear());
  const [selectedMonth] = useState(currentDate.getMonth() + 1);

  // Query: Estado de reportes del mes
  const { data: reportStatus, isLoading: loadingStatus } = useQuery<ReportStatus>({
    queryKey: ['fiscal-status', selectedYear, selectedMonth],
    queryFn: () => dgiiService.getMonthStatus(selectedYear, selectedMonth),
  });

  // Query: Alertas de vencimiento
  const { data: alerts, isLoading: loadingAlerts } = useQuery<DeadlineAlert[]>({
    queryKey: ['fiscal-alerts'],
    queryFn: () => dgiiService.getUpcomingDeadlines(),
  });

  // Query: Secuencias NCF
  const { data: ncfSequences, isLoading: loadingNCF } = useQuery<NCFSequence[]>({
    queryKey: ['ncf-sequences'],
    queryFn: () => dgiiService.getNCFSequenceStatus(),
  });

  const getStatusBadge = (status: string) => {
    const variants = {
      NOT_STARTED: { variant: 'secondary', label: 'Pendiente', icon: Clock },
      PREVIEW: { variant: 'default', label: 'Preview', icon: FileText },
      GENERATED: { variant: 'default', label: 'Generado', icon: CheckCircle },
      SUBMITTED: { variant: 'success', label: 'Enviado', icon: CheckCircle },
      CALCULATED: { variant: 'default', label: 'Calculado', icon: CheckCircle },
    };
    const config = variants[status as keyof typeof variants] || variants.NOT_STARTED;
    const Icon = config.icon;

    return (
      <Badge variant={config.variant as any} className="flex items-center gap-1">
        <Icon className="h-3 w-3" />
        {config.label}
      </Badge>
    );
  };

  const getAlertVariant = (daysRemaining: number) => {
    if (daysRemaining <= 1) return 'destructive';
    if (daysRemaining <= 3) return 'warning';
    return 'default';
  };

  if (loadingStatus || loadingAlerts || loadingNCF) {
    return <div className="p-6">Cargando dashboard fiscal...</div>;
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Dashboard Fiscal DGII</h1>
          <p className="text-gray-500">
            {getMonthName(selectedMonth)} {selectedYear}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => window.location.href = '/admin/fiscal/calendar'}>
            <Calendar className="h-4 w-4 mr-2" />
            Ver Calendario
          </Button>
        </div>
      </div>

      {/* Alertas de Vencimiento */}
      {alerts && alerts.length > 0 && (
        <div className="space-y-2">
          {alerts.map((alert, i) => (
            <Alert key={i} variant={getAlertVariant(alert.daysRemaining)}>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription className="flex justify-between items-center">
                <div>
                  <strong>{alert.name}</strong>
                  <span className="ml-2">vence el {alert.deadline}</span>
                </div>
                <Badge variant={alert.daysRemaining <= 1 ? 'destructive' : 'warning'}>
                  {alert.daysRemaining} {alert.daysRemaining === 1 ? 'día' : 'días'}
                </Badge>
              </AlertDescription>
            </Alert>
          ))}
        </div>
      )}

      {/* Grid de Estado de Reportes */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">

        {/* Formato 606 */}
        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="pb-2">
            <div className="flex justify-between items-start">
              <CardTitle className="text-sm font-medium">Formato 606</CardTitle>
              {getStatusBadge(reportStatus?.format606?.status || 'NOT_STARTED')}
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <div>
              <p className="text-2xl font-bold">{reportStatus?.format606?.recordCount || 0}</p>
              <p className="text-xs text-gray-500">gastos registrados</p>
            </div>
            {reportStatus?.format606?.totalAmount > 0 && (
              <div className="space-y-1 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600">Total:</span>
                  <span className="font-semibold">RD${reportStatus.format606.totalAmount.toLocaleString()}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">ITBIS:</span>
                  <span className="font-semibold">RD${reportStatus.format606.totalITBIS.toLocaleString()}</span>
                </div>
              </div>
            )}
            <div className="flex gap-2 pt-2">
              <Button
                size="sm"
                variant="outline"
                className="flex-1"
                onClick={() => window.location.href = '/admin/fiscal/606?action=preview'}
              >
                Preview
              </Button>
              <Button
                size="sm"
                className="flex-1"
                onClick={() => window.location.href = '/admin/fiscal/606?action=generate'}
                disabled={reportStatus?.format606?.status === 'SUBMITTED'}
              >
                Generar
              </Button>
            </div>
            {reportStatus?.format606?.status === 'GENERATED' && (
              <Button size="sm" variant="outline" className="w-full">
                <Download className="h-3 w-3 mr-1" />
                Descargar TXT
              </Button>
            )}
            <p className="text-xs text-gray-400 text-center">
              Vence: 15 de {getMonthName(selectedMonth)}
            </p>
          </CardContent>
        </Card>

        {/* Formato 607 */}
        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="pb-2">
            <div className="flex justify-between items-start">
              <CardTitle className="text-sm font-medium">Formato 607</CardTitle>
              {getStatusBadge(reportStatus?.format607?.status || 'NOT_STARTED')}
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <div>
              <p className="text-2xl font-bold">{reportStatus?.format607?.recordCount || 0}</p>
              <p className="text-xs text-gray-500">facturas emitidas</p>
            </div>
            {reportStatus?.format607?.totalInvoiced > 0 && (
              <div className="space-y-1 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600">Total:</span>
                  <span className="font-semibold">RD${reportStatus.format607.totalInvoiced.toLocaleString()}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">ITBIS:</span>
                  <span className="font-semibold">RD${reportStatus.format607.totalITBIS.toLocaleString()}</span>
                </div>
              </div>
            )}
            <div className="flex gap-2 pt-2">
              <Button size="sm" variant="outline" className="flex-1">Preview</Button>
              <Button size="sm" className="flex-1">Generar</Button>
            </div>
            <p className="text-xs text-gray-400 text-center">
              Vence: 15 de {getMonthName(selectedMonth)}
            </p>
          </CardContent>
        </Card>

        {/* IR-17 (Retenciones) */}
        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="pb-2">
            <div className="flex justify-between items-start">
              <CardTitle className="text-sm font-medium">IR-17 (Retenciones)</CardTitle>
              {getStatusBadge(reportStatus?.ir17?.status || 'NOT_STARTED')}
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <div>
              <p className="text-2xl font-bold">
                RD${reportStatus?.ir17?.totalRetentions?.toLocaleString() || 0}
              </p>
              <p className="text-xs text-gray-500">total retenido</p>
            </div>
            {reportStatus?.ir17?.providerCount > 0 && (
              <div className="text-sm">
                <span className="text-gray-600">{reportStatus.ir17.providerCount} proveedores con retención</span>
              </div>
            )}
            <Button
              size="sm"
              className="w-full"
              onClick={() => window.location.href = '/admin/fiscal/ir17'}
            >
              Ver Detalle
            </Button>
            <p className="text-xs text-gray-400 text-center">
              Vence: 10 de {getMonthName(selectedMonth)}
            </p>
          </CardContent>
        </Card>

        {/* IT-1 (ITBIS) */}
        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="pb-2">
            <div className="flex justify-between items-start">
              <CardTitle className="text-sm font-medium">IT-1 (ITBIS)</CardTitle>
              {getStatusBadge(reportStatus?.it1?.status || 'NOT_STARTED')}
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <div>
              <p className={`text-2xl font-bold ${
                reportStatus?.it1?.balance >= 0 ? 'text-red-600' : 'text-green-600'
              }`}>
                {reportStatus?.it1?.balance >= 0
                  ? `RD$${reportStatus.it1.balance.toLocaleString()}`
                  : `(RD$${Math.abs(reportStatus.it1.balance).toLocaleString()})`}
              </p>
              <p className="text-xs text-gray-500">
                {reportStatus?.it1?.balance >= 0 ? 'a pagar' : 'crédito fiscal'}
              </p>
            </div>
            {reportStatus?.it1 && (
              <div className="space-y-1 text-xs">
                <div className="flex justify-between">
                  <span className="text-gray-600">ITBIS Cobrado:</span>
                  <span>RD${reportStatus.it1.itbisCobrado.toLocaleString()}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">ITBIS Pagado:</span>
                  <span>RD${reportStatus.it1.itbisPagado.toLocaleString()}</span>
                </div>
              </div>
            )}
            <Button
              size="sm"
              className="w-full"
              onClick={() => window.location.href = '/admin/fiscal/it1'}
            >
              Calcular IT-1
            </Button>
            <p className="text-xs text-gray-400 text-center">
              Vence: 20 de {getMonthName(selectedMonth)}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Monitor de Secuencias NCF */}
      <Card>
        <CardHeader>
          <CardTitle>Secuencias NCF</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {ncfSequences?.map((seq) => (
              <div key={seq.type} className="p-4 border rounded-lg">
                <div className="flex justify-between items-center mb-2">
                  <span className="font-medium">{seq.type}</span>
                  {seq.remaining < 100 && (
                    <Badge variant="warning">
                      <AlertTriangle className="h-3 w-3 mr-1" />
                      Bajo
                    </Badge>
                  )}
                </div>
                <p className="text-2xl font-bold">{seq.remaining.toLocaleString()}</p>
                <p className="text-xs text-gray-500 mb-2">
                  disponibles de {seq.total.toLocaleString()}
                </p>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className={`h-2 rounded-full transition-all ${
                      seq.percentage > 20 ? 'bg-green-500' : 'bg-yellow-500'
                    }`}
                    style={{ width: `${seq.percentage}%` }}
                  />
                </div>
                <p className="text-xs text-gray-400 mt-1 text-center">
                  {seq.percentage}% restante
                </p>
              </div>
            ))}
          </div>
          <div className="mt-4 text-center">
            <Button variant="outline" onClick={() => window.location.href = '/admin/fiscal/ncf'}>
              Ver Detalles Completos
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Calendario Fiscal Resumen */}
      <Card>
        <CardHeader>
          <CardTitle>Calendario Fiscal - {getMonthName(selectedMonth)} {selectedYear}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
            <DeadlineCard
              day="3"
              title="Cierre Mes Anterior"
              description="Validar todos los gastos e ingresos"
              status="pending"
            />
            <DeadlineCard
              day="5"
              title="TSS"
              description="Enviar reporte de nómina"
              status="pending"
            />
            <DeadlineCard
              day="10"
              title="IR-17"
              description="Retenciones ISR"
              status={reportStatus?.ir17?.status === 'SUBMITTED' ? 'completed' : 'pending'}
            />
            <DeadlineCard
              day="15"
              title="606/607/608"
              description="Formatos DGII"
              status={reportStatus?.format606?.status === 'SUBMITTED' ? 'completed' : 'pending'}
            />
            <DeadlineCard
              day="20"
              title="IT-1"
              description="Declaración ITBIS"
              status={reportStatus?.it1?.status === 'SUBMITTED' ? 'completed' : 'pending'}
            />
          </div>
          <div className="mt-4 text-center">
            <Button onClick={() => window.location.href = '/admin/fiscal/calendar'}>
              <Calendar className="h-4 w-4 mr-2" />
              Ver Calendario Completo
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

// Helper component
interface DeadlineCardProps {
  day: string;
  title: string;
  description: string;
  status: 'pending' | 'completed';
}

const DeadlineCard: React.FC<DeadlineCardProps> = ({ day, title, description, status }) => {
  const today = new Date().getDate();
  const dayNum = parseInt(day);
  const isToday = today === dayNum;
  const isPast = today > dayNum;
  const isUpcoming = today < dayNum && (dayNum - today) <= 3;

  return (
    <div className={`p-4 border-2 rounded-lg ${
      isToday ? 'border-blue-500 bg-blue-50' :
      status === 'completed' ? 'border-green-500 bg-green-50' :
      isUpcoming ? 'border-yellow-500 bg-yellow-50' :
      'border-gray-200'
    }`}>
      <div className="flex justify-between items-start mb-2">
        <span className="text-3xl font-bold text-gray-700">Día {day}</span>
        {status === 'completed' && (
          <CheckCircle className="h-5 w-5 text-green-600" />
        )}
      </div>
      <h4 className="font-semibold text-sm mb-1">{title}</h4>
      <p className="text-xs text-gray-600">{description}</p>
    </div>
  );
};

// Utility function
const getMonthName = (month: number): string => {
  const months = [
    'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
    'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
  ];
  return months[month - 1];
};

export default FiscalDashboard;
```

**Backend API necesaria:**

```csharp
// DGIIService.Api/Controllers/DashboardController.cs

[ApiController]
[Route("api/dgii/dashboard")]
[Authorize(Roles = "Admin,Accountant")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    [HttpGet("status/{year}/{month}")]
    public async Task<ActionResult<MonthStatusDto>> GetMonthStatus(int year, int month)
    {
        var status = await _dashboard.GetMonthStatusAsync(year, month);
        return Ok(status);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<List<DeadlineAlertDto>>> GetUpcomingDeadlines()
    {
        var alerts = await _dashboard.GetUpcomingDeadlinesAsync();
        return Ok(alerts);
    }

    [HttpGet("ncf/sequences")]
    public async Task<ActionResult<List<NCFSequenceDto>>> GetNCFSequenceStatus()
    {
        var sequences = await _dashboard.GetNCFSequenceStatusAsync();
        return Ok(sequences);
    }
}
```

---

### Propuesta 2: Generador Formato 606

**Descripción:** Página para generar el Formato 606 (Compras y Gastos) con preview, validación y descarga.

**Complejidad:** 21 SP

**Código TypeScript/React:**

```typescript
// frontend/web/src/pages/admin/Format606Page.tsx

import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Download,
  Eye,
  FileText,
  AlertTriangle,
  CheckCircle,
  RefreshCcw
} from 'lucide-react';
import { format606Service } from '@/services/dgiiService';

interface Format606Preview {
  year: number;
  month: number;
  recordCount: number;
  totalServices: number;
  totalGoods: number;
  totalAmount: number;
  totalITBIS: number;
  totalISRWithheld: number;
  lines: Format606Line[];
  validations: ValidationResult[];
}

interface Format606Line {
  expenseId: string;
  providerName: string;
  providerRNC: string;
  ncf: string;
  invoiceDate: string;
  expenseType: string;
  servicesAmount: number;
  goodsAmount: number;
  totalAmount: number;
  itbisAmount: number;
  isrWithheld: number;
  paymentMethod: string;
}

interface ValidationResult {
  level: 'INFO' | 'WARNING' | 'ERROR';
  message: string;
  lineNumber?: number;
}

interface GenerateResult {
  success: boolean;
  fileUrl: string;
  recordCount: number;
  totalAmount: number;
  totalITBIS: number;
  message?: string;
}

export const Format606Page = () => {
  const queryClient = useQueryClient();
  const currentDate = new Date();

  const [selectedYear, setSelectedYear] = useState(currentDate.getFullYear());
  const [selectedMonth, setSelectedMonth] = useState(
    currentDate.getMonth() === 0 ? 12 : currentDate.getMonth()
  );
  const [showPreview, setShowPreview] = useState(false);

  // Query: Preview del 606
  const {
    data: preview,
    isLoading: loadingPreview,
    refetch: refetchPreview
  } = useQuery<Format606Preview>({
    queryKey: ['format-606-preview', selectedYear, selectedMonth],
    queryFn: () => format606Service.getPreview(selectedYear, selectedMonth),
    enabled: showPreview,
  });

  // Query: Historial de formatos generados
  const { data: history, isLoading: loadingHistory } = useQuery({
    queryKey: ['format-606-history', selectedYear],
    queryFn: () => format606Service.getHistory(selectedYear),
  });

  // Mutation: Generar formato 606
  const generateMutation = useMutation({
    mutationFn: () => format606Service.generate(selectedYear, selectedMonth),
    onSuccess: (result: GenerateResult) => {
      if (result.success) {
        alert(`✅ Formato 606 generado exitosamente!\n\n` +
              `📄 Registros: ${result.recordCount}\n` +
              `💰 Total: RD$${result.totalAmount.toLocaleString()}\n` +
              `💸 ITBIS: RD$${result.totalITBIS.toLocaleString()}`);
        queryClient.invalidateQueries({ queryKey: ['format-606-history'] });
        setShowPreview(false);
      } else {
        alert(`❌ Error al generar 606: ${result.message}`);
      }
    },
    onError: (error: any) => {
      alert(`❌ Error: ${error.message}`);
    },
  });

  const handlePreview = () => {
    setShowPreview(true);
    refetchPreview();
  };

  const handleGenerate = () => {
    if (!preview) return;

    const errors = preview.validations.filter(v => v.level === 'ERROR');
    if (errors.length > 0) {
      alert(`❌ No se puede generar el formato. Hay ${errors.length} errores:\n\n` +
            errors.map(e => `• ${e.message}`).join('\n'));
      return;
    }

    const warnings = preview.validations.filter(v => v.level === 'WARNING');
    if (warnings.length > 0) {
      const confirmed = confirm(
        `⚠️ Hay ${warnings.length} advertencias:\n\n` +
        warnings.map(w => `• ${w.message}`).join('\n') +
        `\n\n¿Desea continuar?`
      );
      if (!confirmed) return;
    }

    generateMutation.mutate();
  };

  const handleDownload = (fileUrl: string, formatId: string) => {
    window.open(fileUrl, '_blank');
  };

  const getValidationBadge = (level: string) => {
    const configs = {
      INFO: { variant: 'default', label: 'Info' },
      WARNING: { variant: 'warning', label: 'Advertencia' },
      ERROR: { variant: 'destructive', label: 'Error' },
    };
    const config = configs[level as keyof typeof configs] || configs.INFO;
    return <Badge variant={config.variant as any}>{config.label}</Badge>;
  };

  const years = Array.from({ length: 5 }, (_, i) => currentDate.getFullYear() - i);
  const months = [
    { value: 1, label: 'Enero' },
    { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' },
    { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' },
    { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' },
    { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' },
    { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' },
    { value: 12, label: 'Diciembre' },
  ];

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Formato 606 - Compras y Gastos</h1>
          <p className="text-gray-500">
            Generación de reporte mensual para DGII
          </p>
        </div>
        <Button variant="outline" onClick={() => window.location.href = '/admin/fiscal/dashboard'}>
          ← Volver al Dashboard
        </Button>
      </div>

      {/* Selector de Período */}
      <Card>
        <CardHeader>
          <CardTitle>Seleccionar Período</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4 items-end">
            <div className="flex-1">
              <label className="text-sm font-medium mb-2 block">Año</label>
              <Select value={selectedYear.toString()} onValueChange={(v) => setSelectedYear(parseInt(v))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {years.map(y => (
                    <SelectItem key={y} value={y.toString()}>{y}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex-1">
              <label className="text-sm font-medium mb-2 block">Mes</label>
              <Select value={selectedMonth.toString()} onValueChange={(v) => setSelectedMonth(parseInt(v))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {months.map(m => (
                    <SelectItem key={m.value} value={m.value.toString()}>{m.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <Button onClick={handlePreview} disabled={loadingPreview}>
              <Eye className="h-4 w-4 mr-2" />
              {loadingPreview ? 'Cargando...' : 'Ver Preview'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Preview de Gastos */}
      {showPreview && preview && (
        <>
          {/* Stats Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-gray-500">Total Gastos</p>
                <p className="text-2xl font-bold">{preview.recordCount}</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-gray-500">Total Monto</p>
                <p className="text-2xl font-bold">RD${preview.totalAmount.toLocaleString()}</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-gray-500">Total ITBIS</p>
                <p className="text-2xl font-bold text-green-600">
                  RD${preview.totalITBIS.toLocaleString()}
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-gray-500">Retenciones ISR</p>
                <p className="text-2xl font-bold text-blue-600">
                  RD${preview.totalISRWithheld.toLocaleString()}
                </p>
              </CardContent>
            </Card>
          </div>

          {/* Validaciones */}
          {preview.validations.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Validaciones</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {preview.validations.map((validation, i) => (
                    <Alert key={i} variant={validation.level === 'ERROR' ? 'destructive' : 'default'}>
                      <div className="flex items-start gap-3">
                        {getValidationBadge(validation.level)}
                        <div className="flex-1">
                          <AlertDescription>
                            {validation.message}
                            {validation.lineNumber && (
                              <span className="ml-2 text-gray-500">
                                (Línea {validation.lineNumber})
                              </span>
                            )}
                          </AlertDescription>
                        </div>
                      </div>
                    </Alert>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Tabla de Gastos */}
          <Card>
            <CardHeader>
              <CardTitle>Gastos a Incluir ({preview.recordCount})</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Proveedor</TableHead>
                      <TableHead>RNC</TableHead>
                      <TableHead>NCF</TableHead>
                      <TableHead>Fecha</TableHead>
                      <TableHead>Tipo</TableHead>
                      <TableHead className="text-right">Monto</TableHead>
                      <TableHead className="text-right">ITBIS</TableHead>
                      <TableHead className="text-right">Retención</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {preview.lines.map((line, i) => (
                      <TableRow key={i}>
                        <TableCell>{line.providerName}</TableCell>
                        <TableCell className="font-mono text-sm">{line.providerRNC}</TableCell>
                        <TableCell className="font-mono text-sm">{line.ncf}</TableCell>
                        <TableCell>{line.invoiceDate}</TableCell>
                        <TableCell>
                          <Badge variant="outline">{line.expenseType}</Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          RD${line.totalAmount.toLocaleString()}
                        </TableCell>
                        <TableCell className="text-right text-green-600">
                          RD${line.itbisAmount.toLocaleString()}
                        </TableCell>
                        <TableCell className="text-right text-blue-600">
                          {line.isrWithheld > 0 ? `RD$${line.isrWithheld.toLocaleString()}` : '-'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>

          {/* Botones de Acción */}
          <div className="flex justify-end gap-4">
            <Button variant="outline" onClick={() => setShowPreview(false)}>
              Cancelar
            </Button>
            <Button
              onClick={handleGenerate}
              disabled={generateMutation.isPending || preview.validations.some(v => v.level === 'ERROR')}
              className="min-w-[200px]"
            >
              {generateMutation.isPending ? (
                <>
                  <RefreshCcw className="h-4 w-4 mr-2 animate-spin" />
                  Generando...
                </>
              ) : (
                <>
                  <FileText className="h-4 w-4 mr-2" />
                  Generar Formato 606
                </>
              )}
            </Button>
          </div>
        </>
      )}

      {/* Historial de Formatos Generados */}
      <Card>
        <CardHeader>
          <CardTitle>Historial de Formatos Generados</CardTitle>
        </CardHeader>
        <CardContent>
          {loadingHistory ? (
            <p className="text-gray-500 text-center py-8">Cargando historial...</p>
          ) : history && history.length > 0 ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Período</TableHead>
                  <TableHead>Fecha Generado</TableHead>
                  <TableHead>Registros</TableHead>
                  <TableHead className="text-right">Total Monto</TableHead>
                  <TableHead className="text-right">Total ITBIS</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead className="text-center">Acciones</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {history.map((format: any) => (
                  <TableRow key={format.id}>
                    <TableCell className="font-medium">
                      {format.periodYear}-{format.periodMonth.toString().padStart(2, '0')}
                    </TableCell>
                    <TableCell>{new Date(format.generatedAt).toLocaleDateString()}</TableCell>
                    <TableCell>{format.recordCount}</TableCell>
                    <TableCell className="text-right">RD${format.totalAmount.toLocaleString()}</TableCell>
                    <TableCell className="text-right text-green-600">
                      RD${format.totalITBIS.toLocaleString()}
                    </TableCell>
                    <TableCell>
                      <Badge variant={format.status === 'SUBMITTED' ? 'success' : 'default'}>
                        {format.status === 'SUBMITTED' ? 'Enviado' : 'Generado'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-center">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleDownload(format.fileUrl, format.id)}
                      >
                        <Download className="h-3 w-3 mr-1" />
                        Descargar
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <p className="text-gray-500 text-center py-8">
              No hay formatos generados para {selectedYear}
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

export default Format606Page;
```

**Backend API necesaria:**

```csharp
// DGIIService.Api/Controllers/Format606Controller.cs

[ApiController]
[Route("api/dgii/606")]
[Authorize(Roles = "Admin,Accountant")]
public class Format606Controller : ControllerBase
{
    private readonly IFormat606GeneratorService _generator;
    private readonly IFormat606Repository _repository;

    [HttpGet("preview")]
    public async Task<ActionResult<Format606PreviewDto>> GetPreview(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var preview = await _generator.GetPreviewAsync(year, month);
        return Ok(preview);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<Format606Result>> Generate(
        [FromBody] GenerateFormat606Request request)
    {
        var result = await _generator.GenerateAsync(request.Year, request.Month);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{formatId}/download")]
    public async Task<IActionResult> Download(Guid formatId)
    {
        var format = await _repository.GetByIdAsync(formatId);
        if (format == null) return NotFound();

        var bytes = Encoding.UTF8.GetBytes(format.FileContent);
        var fileName = $"606_{format.PeriodYear}{format.PeriodMonth:D2}.txt";
        return File(bytes, "text/plain", fileName);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<DGIIFormatDto>>> GetHistory([FromQuery] int? year)
    {
        var formats = await _repository.GetHistoryAsync("606", year);
        return Ok(formats);
    }
}
```

---

### Propuesta 3: Scheduler de Jobs DGII

**Descripción:** Página para ver y gestionar jobs automatizados de recordatorios y generación de reportes.

**Complejidad:** 8 SP

**Código TypeScript/React:**

```typescript
// frontend/web/src/pages/admin/ReportSchedulerPage.tsx

import React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import {
  Clock,
  Play,
  Pause,
  History,
  CheckCircle,
  XCircle,
  AlertTriangle
} from 'lucide-react';
import { schedulerService } from '@/services/dgiiService';

interface ScheduledJob {
  name: string;
  description: string;
  cronExpression: string;
  cronReadable: string;
  enabled: boolean;
  lastRun?: {
    timestamp: string;
    status: 'SUCCESS' | 'FAILED' | 'RUNNING';
    duration: number;
    message?: string;
  };
  nextRun: {
    timestamp: string;
    daysRemaining: number;
    hoursRemaining: number;
  };
}

interface JobExecution {
  id: string;
  jobName: string;
  startedAt: string;
  completedAt?: string;
  status: 'SUCCESS' | 'FAILED' | 'RUNNING';
  duration: number;
  errorMessage?: string;
}

export const ReportSchedulerPage = () => {
  const queryClient = useQueryClient();

  // Query: Lista de jobs configurados
  const { data: jobs, isLoading } = useQuery<ScheduledJob[]>({
    queryKey: ['scheduler-jobs'],
    queryFn: () => schedulerService.getJobs(),
    refetchInterval: 30000, // Refresh cada 30 segundos
  });

  // Mutation: Trigger manual de job
  const triggerMutation = useMutation({
    mutationFn: (jobName: string) => schedulerService.triggerJob(jobName),
    onSuccess: (_, jobName) => {
      alert(`✅ Job "${jobName}" ejecutado manualmente`);
      queryClient.invalidateQueries({ queryKey: ['scheduler-jobs'] });
    },
    onError: (error: any, jobName) => {
      alert(`❌ Error al ejecutar "${jobName}": ${error.message}`);
    },
  });

  // Mutation: Enable/Disable job
  const toggleMutation = useMutation({
    mutationFn: ({ jobName, enabled }: { jobName: string; enabled: boolean }) =>
      schedulerService.toggleJob(jobName, enabled),
    onSuccess: (_, { jobName, enabled }) => {
      alert(`✅ Job "${jobName}" ${enabled ? 'activado' : 'desactivado'}`);
      queryClient.invalidateQueries({ queryKey: ['scheduler-jobs'] });
    },
  });

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'SUCCESS':
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 'FAILED':
        return <XCircle className="h-4 w-4 text-red-600" />;
      case 'RUNNING':
        return <Clock className="h-4 w-4 text-blue-600 animate-spin" />;
      default:
        return null;
    }
  };

  const getStatusBadge = (status: string) => {
    const configs = {
      SUCCESS: { variant: 'success', label: 'Exitoso' },
      FAILED: { variant: 'destructive', label: 'Error' },
      RUNNING: { variant: 'default', label: 'Ejecutando' },
    };
    const config = configs[status as keyof typeof configs] || configs.SUCCESS;
    return <Badge variant={config.variant as any}>{config.label}</Badge>;
  };

  if (isLoading) {
    return <div className="p-6">Cargando scheduler...</div>;
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Scheduler de Reportes DGII</h1>
          <p className="text-gray-500">
            Gestión de jobs automatizados y recordatorios fiscales
          </p>
        </div>
        <Button variant="outline" onClick={() => window.location.href = '/admin/fiscal/dashboard'}>
          ← Volver al Dashboard
        </Button>
      </div>

      {/* Info Card */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-blue-600 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium mb-1">
                Jobs Automáticos Configurados
              </p>
              <p className="text-sm text-gray-600">
                Los jobs se ejecutan automáticamente según su schedule configurado.
                Puedes ejecutarlos manualmente en cualquier momento o
                desactivarlos temporalmente si es necesario.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tabla de Jobs */}
      <Card>
        <CardHeader>
          <CardTitle>Jobs Configurados</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Job</TableHead>
                <TableHead>Schedule</TableHead>
                <TableHead>Última Ejecución</TableHead>
                <TableHead>Próxima Ejecución</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead className="text-center">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {jobs?.map((job) => (
                <TableRow key={job.name}>
                  <TableCell>
                    <div>
                      <p className="font-medium">{job.name}</p>
                      <p className="text-sm text-gray-500">{job.description}</p>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className="font-mono text-xs">
                      {job.cronReadable}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {job.lastRun ? (
                      <div className="flex items-center gap-2">
                        {getStatusIcon(job.lastRun.status)}
                        <div>
                          <p className="text-sm">
                            {new Date(job.lastRun.timestamp).toLocaleString()}
                          </p>
                          <p className="text-xs text-gray-500">
                            {job.lastRun.duration}ms
                          </p>
                        </div>
                      </div>
                    ) : (
                      <span className="text-gray-400">Nunca</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <div>
                      <p className="text-sm">
                        {new Date(job.nextRun.timestamp).toLocaleString()}
                      </p>
                      {job.nextRun.daysRemaining === 0 ? (
                        <p className="text-xs text-orange-600 font-medium">
                          En {job.nextRun.hoursRemaining}h
                        </p>
                      ) : (
                        <p className="text-xs text-gray-500">
                          En {job.nextRun.daysRemaining} días
                        </p>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant={job.enabled ? 'success' : 'secondary'}>
                      {job.enabled ? 'Activo' : 'Desactivado'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2 justify-center">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => triggerMutation.mutate(job.name)}
                        disabled={triggerMutation.isPending}
                      >
                        <Play className="h-3 w-3" />
                      </Button>
                      <Button
                        size="sm"
                        variant={job.enabled ? 'destructive' : 'default'}
                        onClick={() => toggleMutation.mutate({
                          jobName: job.name,
                          enabled: !job.enabled
                        })}
                      >
                        {job.enabled ? (
                          <Pause className="h-3 w-3" />
                        ) : (
                          <Play className="h-3 w-3" />
                        )}
                      </Button>
                      <JobHistoryDialog jobName={job.name} />
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Descripción de Jobs */}
      <Card>
        <CardHeader>
          <CardTitle>Descripción de Jobs</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <JobDescription
              name="IR-17 Reminder"
              schedule="Día 8 de cada mes a las 9:00 AM"
              description="Envía recordatorio por email sobre vencimiento de IR-17 (día 10). Incluye preview de retenciones del mes."
            />
            <JobDescription
              name="Format 606 Preview"
              schedule="Día 10 de cada mes a las 8:00 AM"
              description="Genera preview automático del Formato 606 y envía resumen por email con estadísticas del mes."
            />
            <JobDescription
              name="Formats Reminder"
              schedule="Día 13 de cada mes a las 9:00 AM"
              description="Recordatorio de envío de formatos 606/607/608 a DGII (vence día 15)."
            />
            <JobDescription
              name="IT-1 Reminder"
              schedule="Día 18 de cada mes a las 9:00 AM"
              description="Recordatorio de declaración IT-1 (ITBIS mensual). Vence día 20."
            />
            <JobDescription
              name="NCF Sequence Check"
              schedule="Diariamente a las 7:00 AM"
              description="Verifica secuencias NCF disponibles. Alerta si quedan menos de 100."
            />
            <JobDescription
              name="Fiscal Data Backup"
              schedule="Domingos a las 2:00 AM"
              description="Backup semanal de todos los datos fiscales a S3."
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

// Helper Components

interface JobDescriptionProps {
  name: string;
  schedule: string;
  description: string;
}

const JobDescription: React.FC<JobDescriptionProps> = ({ name, schedule, description }) => {
  return (
    <div className="p-4 border rounded-lg">
      <div className="flex items-start gap-3">
        <Clock className="h-5 w-5 text-blue-600 mt-0.5" />
        <div className="flex-1">
          <h4 className="font-semibold text-sm mb-1">{name}</h4>
          <p className="text-sm text-gray-600 mb-2">{description}</p>
          <Badge variant="outline" className="text-xs">
            {schedule}
          </Badge>
        </div>
      </div>
    </div>
  );
};

interface JobHistoryDialogProps {
  jobName: string;
}

const JobHistoryDialog: React.FC<JobHistoryDialogProps> = ({ jobName }) => {
  const { data: history, isLoading } = useQuery<JobExecution[]>({
    queryKey: ['job-history', jobName],
    queryFn: () => schedulerService.getJobHistory(jobName),
    enabled: false, // Solo cargar cuando se abra el dialog
  });

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button size="sm" variant="outline">
          <History className="h-3 w-3" />
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle>Historial: {jobName}</DialogTitle>
        </DialogHeader>
        <div className="max-h-[60vh] overflow-y-auto">
          {isLoading ? (
            <p className="text-center py-8">Cargando historial...</p>
          ) : history && history.length > 0 ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Fecha/Hora</TableHead>
                  <TableHead>Duración</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead>Mensaje</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {history.map((exec) => (
                  <TableRow key={exec.id}>
                    <TableCell className="text-sm">
                      {new Date(exec.startedAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="text-sm">
                      {exec.duration}ms
                    </TableCell>
                    <TableCell>
                      <Badge variant={exec.status === 'SUCCESS' ? 'success' : 'destructive'}>
                        {exec.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-gray-600">
                      {exec.errorMessage || '-'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <p className="text-center py-8 text-gray-500">
              No hay historial disponible
            </p>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default ReportSchedulerPage;
```

---

## 📊 PLAN DE IMPLEMENTACIÓN

### Fase 1: DGIIService Backend (34 SP - 2-3 semanas)

**Sprint 1.1: Estructura Base (8 SP)**

- [ ] Crear proyecto DGIIService con Clean Architecture
- [ ] Configurar DbContext y migraciones
- [ ] Crear entidades: DGIIFormat, NCFSequence, NCFIssued, FiscalPeriod
- [ ] Crear enums: NCFType, ExpenseType (01-11), FormatStatus
- [ ] Configurar Swagger, CORS, Health Checks

**Sprint 1.2: Formato 606 Generator (13 SP)**

- [ ] Implementar Format606GeneratorService
- [ ] Crear Format606Controller con 4 endpoints
- [ ] Implementar validación de formato
- [ ] Integración con ExpenseService (obtener gastos)
- [ ] Generación de archivo TXT con formato DGII
- [ ] Upload a S3 y registro en BD

**Sprint 1.3: Formato 607 y 608 (13 SP)**

- [ ] Implementar Format607GeneratorService
- [ ] Implementar Format608GeneratorService
- [ ] Crear controllers respectivos
- [ ] Integración con BillingService (facturas)
- [ ] Generación de archivos TXT

### Fase 2: Jobs Automatizados (21 SP - 1-2 semanas)

**Sprint 2.1: Jobs de Recordatorios (13 SP)**

- [ ] Crear IR17ReminderJob
- [ ] Crear Format606PreviewJob
- [ ] Crear FormatsReminderJob
- [ ] Crear IT1ReminderJob
- [ ] Configurar cron schedules en Hangfire
- [ ] Integración con NotificationService (emails)

**Sprint 2.2: Jobs de Mantenimiento (8 SP)**

- [ ] Crear NCFSequenceCheckJob
- [ ] Crear FiscalDataBackupJob
- [ ] Configurar alertas por secuencias bajas
- [ ] Backup automático a S3

### Fase 3: Dashboard Frontend (26 SP - 1-2 semanas)

**Sprint 3.1: Dashboard Principal (13 SP)**

- [ ] Crear FiscalDashboard.tsx
- [ ] Implementar ReportStatusCard componentes
- [ ] Implementar NCFSequenceCard
- [ ] Implementar DeadlineAlertCard
- [ ] Calendario fiscal básico
- [ ] Integración con APIs backend

**Sprint 3.2: Generadores de Reportes (13 SP)**

- [ ] Crear Format606Page.tsx
- [ ] Crear Format607Page.tsx
- [ ] Crear IT1Page.tsx
- [ ] Implementar preview de reportes
- [ ] Implementar descarga de archivos
- [ ] Validaciones del lado cliente

### Fase 4: Integration & Testing (13 SP - 1 semana)

**Sprint 4.1: Testing (8 SP)**

- [ ] Unit tests DGIIService (50 tests)
- [ ] Integration tests de jobs
- [ ] E2E tests del dashboard
- [ ] Tests de generación de formatos

**Sprint 4.2: Deployment (5 SP)**

- [ ] Dockerizar DGIIService
- [ ] Actualizar k8s manifests
- [ ] Configurar jobs en producción
- [ ] Documentación de usuario
- [ ] Capacitación al contador

---

## 💰 ANÁLISIS DE COSTOS

### Inversión Inicial

| Fase                          | SP        | Costo por SP | Total       |
| ----------------------------- | --------- | ------------ | ----------- |
| Fase 1: DGIIService Backend   | 34        | $140         | $4,760      |
| Fase 2: Jobs Automatizados    | 21        | $140         | $2,940      |
| Fase 3: Dashboard Frontend    | 26        | $140         | $3,640      |
| Fase 4: Integration & Testing | 13        | $140         | $1,820      |
| **TOTAL**                     | **94 SP** | **$140**     | **$13,160** |

### Ahorros Anuales

| Concepto                       | Sin Automatización          | Con Automatización        | Ahorro         |
| ------------------------------ | --------------------------- | ------------------------- | -------------- |
| Tiempo contador (Excel manual) | 12h/mes × $100 = $1,200/año | 0.5h/mes × $100 = $50/año | **$1,150**     |
| Multas por olvido              | ~$500/año (promedio)        | $0 (alertas automáticas)  | **$500**       |
| Errores de cálculo             | ~$300/año (sobre/sub pago)  | $0 (cálculo automático)   | **$300**       |
| Tiempo administrador           | 3h/mes × $50 = $150/año     | 0h = $0                   | **$150**       |
| **TOTAL AHORRO ANUAL**         | **$2,100 USD**              |                           | **$2,100 USD** |

### ROI

- **Inversión:** $13,160 USD
- **Ahorro anual:** $2,100 USD
- **ROI:** 6.2 años

**NOTA IMPORTANTE:**  
El ROI financiero es largo, PERO el valor principal NO es el ahorro monetario sino:

1. **Eliminación de riesgo de multas** → Hasta $15,000/mes si no se envía 606/607
2. **Reducción de trabajo manual en 95%** → Contador puede enfocarse en análisis estratégico
3. **Visibilidad en tiempo real** → Dashboard fiscal para tomar decisiones informadas
4. **Trazabilidad completa** → Auditoría automática de todas las operaciones
5. **Escalabilidad** → A medida que OKLA crezca, el sistema escala sin aumentar carga manual

---

## 🚨 RIESGOS Y MITIGACIONES

### Riesgo 1: Sin Automatización → Multas DGII

**Probabilidad:** Alta (70%)  
**Impacto:** $3,000-$15,000/mes en multas

**Mitigación:**

- Implementar Fase 2 (Jobs) como prioridad
- Configurar recordatorios críticos primero (606/607/IR17)
- Alertas redundantes (email + SMS)

### Riesgo 2: Errores en Generación de Archivos

**Probabilidad:** Media (40%)  
**Impacto:** Rechazo de DGII → Re-trabajo + retraso

**Mitigación:**

- Validación exhaustiva antes de generar archivo final
- Preview obligatorio antes de generar
- Tests automatizados de formato
- Validación contra ejemplos oficiales de DGII

### Riesgo 3: Jobs No Se Ejecutan

**Probabilidad:** Baja (20%)  
**Impacto:** Sin recordatorios → riesgo de olvido

**Mitigación:**

- Monitoring de Hangfire en dashboard
- Alertas si job falla 2 veces consecutivas
- Logs centralizados en ErrorService
- Health checks de scheduler

### Riesgo 4: DGII Cambia Formato

**Probabilidad:** Baja (15%)  
**Impacto:** Sistema genera archivos obsoletos

**Mitigación:**

- Documentación de formato DGII versionada
- Configuración flexible de campos
- Tests de compatibilidad
- Monitoreo de cambios en sitio DGII

---

## 📋 CHECKLIST DE COMPLETADO

### Backend (0%)

#### DGIIService (0%)

- [ ] Proyecto creado con Clean Architecture
- [ ] DbContext y migraciones
- [ ] Entidades (DGIIFormat, NCFSequence, FiscalPeriod)
- [ ] Format606GeneratorService (500 líneas)
- [ ] Format607GeneratorService (400 líneas)
- [ ] Format608GeneratorService (250 líneas)
- [ ] IR17GeneratorService (300 líneas)
- [ ] ITBISCalculatorService (350 líneas)
- [ ] Controllers (6 archivos)
- [ ] Dockerfile

#### SchedulerService - DGII Jobs (0%)

- [ ] IR17ReminderJob.cs
- [ ] Format606PreviewJob.cs
- [ ] FormatsReminderJob.cs
- [ ] IT1ReminderJob.cs
- [ ] NCFSequenceCheckJob.cs
- [ ] FiscalDataBackupJob.cs
- [ ] DGIIJobsConfiguration.cs (cron schedules)

### Frontend (0%)

#### Páginas (0%)

- [ ] FiscalDashboard.tsx (600 líneas)
- [ ] Format606Page.tsx (400 líneas)
- [ ] Format607Page.tsx (350 líneas)
- [ ] Format608Page.tsx (250 líneas)
- [ ] IR17Page.tsx (300 líneas)
- [ ] IT1Page.tsx (400 líneas)
- [ ] NCFMonitor.tsx (300 líneas)
- [ ] FiscalCalendar.tsx (350 líneas)
- [ ] ReportSchedulerPage.tsx (450 líneas)

#### Componentes (0%)

- [ ] ReportStatusCard.tsx
- [ ] NCFSequenceCard.tsx
- [ ] DeadlineAlertCard.tsx
- [ ] FormatPreviewModal.tsx
- [ ] DownloadFormatButton.tsx
- [ ] NCFVerificationInput.tsx

#### Servicios (0%)

- [ ] dgiiService.ts (15 métodos)
- [ ] format606Service.ts
- [ ] format607Service.ts
- [ ] schedulerService.ts

#### Rutas en App.tsx (0%)

- [ ] /admin/fiscal/dashboard
- [ ] /admin/fiscal/606
- [ ] /admin/fiscal/607
- [ ] /admin/fiscal/608
- [ ] /admin/fiscal/ir17
- [ ] /admin/fiscal/it1
- [ ] /admin/fiscal/ncf
- [ ] /admin/fiscal/calendar
- [ ] /admin/fiscal/scheduler

#### Navegación en AdminSidebar.tsx (0%)

- [ ] Sección "Fiscal DGII" con 9 links

### Testing (0%)

- [ ] DGIIService.Tests (50 tests unitarios)
- [ ] SchedulerService DGII Jobs tests (30 tests)
- [ ] E2E tests de dashboard fiscal
- [ ] Integration tests de generación de formatos

### Deployment (0%)

- [ ] Dockerfile de DGIIService
- [ ] k8s/deployments.yaml actualizado
- [ ] k8s/services.yaml actualizado
- [ ] Gateway routes configuradas
- [ ] CI/CD pipeline actualizado
- [ ] Documentación de usuario

---

## 🎯 RECOMENDACIONES

### Prioridad P0 (Implementar YA)

1. **Fase 2: Jobs Automatizados** (21 SP - 1-2 semanas)
   - Implementar recordatorios de vencimientos
   - Evitar multas por olvido ($3K-$15K/mes)
   - ROI inmediato en reducción de riesgo

2. **Dashboard Básico** (13 SP - 1 semana)
   - Visibilidad de estado fiscal
   - Alertas de próximos vencimientos
   - Monitor de secuencias NCF

### Prioridad P1 (Implementar en Q1 2026)

3. **Generadores de Formatos** (34 SP - 2-3 semanas)
   - Automatizar 606/607/608
   - Eliminar Excel manual
   - Reducir 95% del trabajo del contador

4. **Cálculo IT-1 e IR-17** (13 SP - 1 semana)
   - Cálculo automático de ITBIS
   - Reporte de retenciones
   - Evitar errores de cálculo

### Prioridad P2 (Implementar en Q2 2026)

5. **Verificación NCF con DGII** (8 SP - 1 semana)
   - Validar NCF recibidos
   - Evitar gastos no deducibles
   - Scraping de página DGII

6. **Backups Automáticos** (5 SP - 3 días)
   - Backup semanal de datos fiscales
   - Subir a S3 automáticamente
   - Retention policy de 1 año

---

## 📞 PRÓXIMOS PASOS

### Inmediato (Esta Semana)

1. ✅ **Revisar este documento con el contador**
   - Validar requisitos de formatos 606/607/608
   - Confirmar códigos de tipo de gasto (01-11)
   - Validar calendario fiscal

2. ✅ **Aprobar inversión de Fase 1 + Fase 2**
   - $7,700 USD (55 SP)
   - 3-4 semanas de desarrollo
   - ROI en reducción de riesgo inmediato

### Semana 1-2 (Sprint Planning)

3. **Iniciar Fase 1: DGIIService Backend**
   - Crear estructura del servicio
   - Implementar Format606GeneratorService
   - Tests unitarios básicos

4. **Configurar SchedulerService para DGII**
   - Crear jobs de recordatorios
   - Configurar cron schedules
   - Integración con NotificationService

### Semana 3-4 (Development)

5. **Completar Fase 1**
   - Format607 y Format608
   - IR17 y IT1 calculators
   - Integration tests

6. **Completar Fase 2**
   - Todos los jobs funcionando
   - Alertas configuradas
   - Monitoreo en Hangfire

### Semana 5-6 (Frontend)

7. **Iniciar Fase 3: Dashboard Frontend**
   - FiscalDashboard.tsx
   - Format606Page.tsx
   - Integración con backend

### Semana 7-8 (Testing & Deploy)

8. **Testing Completo**
   - Unit tests (backend)
   - E2E tests (frontend)
   - User acceptance testing con contador

9. **Deploy a Producción**
   - DGIIService a DOKS
   - Jobs en Hangfire
   - Dashboard accesible
   - Capacitación al contador

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Automatización Reportes DGII", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de automatización con jobs DGII", async ({
    page,
  }) => {
    await page.goto("/admin/dgii/automation");
    await expect(page.getByTestId("dgii-automation-dashboard")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /automatización dgii/i }),
    ).toBeVisible();
    await expect(page.getByTestId("scheduled-jobs-list")).toBeVisible();
  });

  test("debe listar jobs programados con próxima ejecución", async ({
    page,
  }) => {
    await page.goto("/admin/dgii/automation");
    await expect(page.getByTestId("job-606-ventas")).toBeVisible();
    await expect(page.getByTestId("job-607-compras")).toBeVisible();
    await expect(page.getByTestId("job-608-anulaciones")).toBeVisible();
    await expect(page.getByTestId("next-execution").first()).toBeVisible();
  });

  test("debe ejecutar job manualmente desde dashboard", async ({ page }) => {
    await page.goto("/admin/dgii/automation");
    await page.getByTestId("run-job-button").first().click();
    await expect(page.getByTestId("job-running-indicator")).toBeVisible();
    await expect(page.getByText(/job iniciado correctamente/i)).toBeVisible();
  });

  test("debe configurar frecuencia de jobs", async ({ page }) => {
    await page.goto("/admin/dgii/automation/settings");
    await expect(page.getByTestId("job-frequency-form")).toBeVisible();
    await page.getByTestId("frequency-select").selectOption("monthly");
    await page.getByTestId("day-of-month").fill("15");
    await page.getByRole("button", { name: /guardar configuración/i }).click();
    await expect(page.getByText(/configuración actualizada/i)).toBeVisible();
  });

  test("debe mostrar historial de ejecuciones con resultados", async ({
    page,
  }) => {
    await page.goto("/admin/dgii/automation/history");
    await expect(page.getByTestId("execution-history-table")).toBeVisible();
    await expect(page.getByTestId("execution-status").first()).toBeVisible();
    await expect(page.getByTestId("execution-date").first()).toBeVisible();
  });

  test("debe descargar reporte generado automáticamente", async ({ page }) => {
    await page.goto("/admin/dgii/automation/history");
    const downloadPromise = page.waitForEvent("download");
    await page.getByTestId("download-report-button").first().click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/606.*\.txt$/i);
  });
});
```

---

**Documento creado:** Enero 29, 2026  
**Próxima revisión:** Después de implementación Fase 1  
**Responsable:** AI Development Team + Contador OKLA

---

_Este documento es parte de la serie de auditorías de compliance legal para OKLA S.R.L._  
_Ver documento maestro: [43-auditoria-compliance-legal.md](43-auditoria-compliance-legal.md)_
