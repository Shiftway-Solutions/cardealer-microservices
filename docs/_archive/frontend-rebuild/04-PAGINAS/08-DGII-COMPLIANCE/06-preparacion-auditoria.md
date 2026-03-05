---
title: "Auditoría #10: Sistema de Preparación para Auditoría DGII - OKLA S.R.L."
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 🔍 Auditoría #10: Sistema de Preparación para Auditoría DGII - OKLA S.R.L.

**Fecha de Auditoría:** Enero 29, 2026  
**Empresa:** OKLA S.R.L.  
**RNC:** 1-33-32590-1  
**Auditor:** Gregory Moreno  
**Documento Base:** `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/13-PREPARACION-AUDITORIA-DGII.md`

---

## 📊 RESUMEN EJECUTIVO

### Compliance General

| Métrica          | Backend    | Frontend  | Overall        | Estado      |
| ---------------- | ---------- | --------- | -------------- | ----------- |
| **Cobertura**    | 🔴 **12%** | 🔴 **0%** | 🔴 **6%**      | **CRÍTICO** |
| **Story Points** | 55 SP      | 60 SP     | **115 SP**     | **$16,100** |
| **Prioridad**    | 🔴 P0      | 🔴 P0     | **🔴 BLOCKER** | **URGENTE** |

### Hallazgos Críticos

#### 🚨 Problemas Principales

**Sin sistema de preparación:**

- ⏰ **3-7 días** para reunir documentación manualmente si DGII audita
- 📂 **Documentos dispersos** entre computadoras, emails, S3 sin organización
- ❌ **Alto riesgo** de no poder responder en plazo legal (5-15 días hábiles)
- 💰 **Multas por no responder:** RD$10K-$50K por requerimiento no atendido
- 🔍 **Investigación ampliada** si DGII sospecha que ocultas información

**Backend actual (12% funcional):**

- ✅ **AuditService EXISTE** (tracking de auditorías internas) - 12%
- ✅ **MediaService con S3** (documentos archivados) - 95%
- ❌ **AuditPackageService NO EXISTE** (generador de paquetes) - 0%
- ❌ **ComplianceReportService NO EXISTE** (score mensual) - 0%
- ❌ **DocumentIndexService NO EXISTE** (índice automático) - 0%

**Frontend completamente ausente (0%):**

- ❌ **0 páginas** de preparación para auditoría DGII
- ❌ **0 componentes** de generación de paquetes
- ❌ **0 dashboards** de compliance score
- ❌ **0 checklist** de documentos requeridos

### Estado por Área

| Área                        | Backend | Frontend | Gap  | Descripción               |
| --------------------------- | ------- | -------- | ---- | ------------------------- |
| **Generación de Paquetes**  | 🔴 0%   | 🔴 0%    | 0%   | Sistema 1-click NO EXISTE |
| **Compliance Score**        | 🔴 0%   | 🔴 0%    | 0%   | Score mensual NO EXISTE   |
| **Índice de Documentos**    | 🔴 0%   | 🔴 0%    | 0%   | Generador NO EXISTE       |
| **Checklist Pre-Auditoría** | 🔴 0%   | 🔴 0%    | 0%   | Sistema NO EXISTE         |
| **Response Templates**      | 🔴 0%   | 🔴 0%    | 0%   | Cartas DGII NO EXISTEN    |
| **Audit Dashboard**         | 🟡 12%  | 🔴 0%    | -12% | AuditService básico       |
| **S3 Document Storage**     | 🟢 95%  | 🟡 30%   | -65% | MediaService OK           |

**Compliance Overall:** 🔴 **6% CRÍTICO** (Backend 12%, Frontend 0%)

---

## 🔍 ANÁLISIS DE BACKEND

### 1. AuditService (12% funcional - NO ES PARA DGII)

**Propósito actual:** Tracking de auditorías internas de eventos de sistema

**Estado:**

```
✅ AuditService.Api/Controllers/AuditController.cs (203 líneas)
   - GetAuditLogs() - Paginación y filtros
   - GetAuditLogById() - Detalle de log
   - CreateAudit() - Registro de evento
✅ AuditService.Domain/Entities/AuditLog.cs
   - Entidad para logs de eventos
✅ AuditService.Application/Features/Audit/*
   - Commands: CreateAudit
   - Queries: GetAuditLogs, GetAuditLogById, GetAuditStats
```

**❌ PROBLEMA:** AuditService NO ES PARA DGII

- Solo registra eventos de sistema (logins, cambios, errores)
- NO tiene funcionalidad de preparación de auditoría fiscal
- NO genera paquetes de documentos
- NO calcula compliance score
- NO tiene checklist de documentos DGII

**Funcionalidad DGII faltante (0%):**

- ❌ AuditPackageController NO EXISTE
- ❌ AuditPackageService NO EXISTE (generación ZIP)
- ❌ ComplianceReportService NO EXISTE (score mensual)
- ❌ DGIIDocumentIndexService NO EXISTE
- ❌ ResponseTemplateService NO EXISTE

### 2. MediaService con S3 (95% funcional)

**Estado:**

```
✅ MediaService.Infrastructure/Services/Storage/S3StorageService.cs
   - UploadFileAsync() ✅
   - DownloadFileAsync() ✅
   - DeleteFileAsync() ✅
   - GetFileUrlAsync() ✅
   - ListFilesAsync() ✅
✅ Configuración AWS S3:
   - Bucket: okla-media
   - Región: us-east-1
   - Permisos configurados
```

**✅ BUENO:** Documentos se archivan en S3

- Facturas emitidas en: s3://okla-media/invoices/emitted/YYYY/MM/
- Facturas recibidas en: s3://okla-media/invoices/received/YYYY/MM/
- Formatos DGII en: s3://okla-media/dgii-reports/606/YYYY/MM/

**❌ PROBLEMA:** No hay organización para auditoría

- Archivos SIN estructura específica para paquetes DGII
- NO hay carpeta s3://okla-media/audit/packages/
- NO hay generación automática de índices
- NO hay descarga masiva por período

### 3. Servicios Faltantes para DGII (0% - NO EXISTEN)

#### a) AuditPackageService (0%)

```csharp
// ❌ NO EXISTE: DGIIService.Application/Services/AuditPackageService.cs

public class AuditPackageService : IAuditPackageService
{
    // ❌ FALTA: Generar paquete completo para auditoría
    public async Task<AuditPackageResult> GenerateAsync(
        DateTime startDate,
        DateTime endDate,
        List<AuditCategory> categories)
    {
        // 1. Información general empresa (legal/)
        // 2. Formatos DGII del período (dgii-reports/)
        // 3. Facturas emitidas (invoices/emitted/)
        // 4. Facturas recibidas (invoices/received/)
        // 5. Estados de cuenta bancarios (banking/statements/)
        // 6. Nómina y TSS (payroll/)
        // 7. Libros contables (accounting/)
        // 8. Generar índice Excel
        // 9. Crear ZIP
        // 10. Subir a s3://audit/packages/
    }

    // ❌ FALTA: Descargar paquete generado
    public async Task<Stream> DownloadPackageAsync(Guid packageId);

    // ❌ FALTA: Listar paquetes históricos
    public async Task<List<AuditPackage>> GetPackagesAsync(int year);
}
```

**Funcionalidad esperada:**

- Parámetros: startDate, endDate, categories[]
- Descarga archivos de S3 por categoría
- Organiza en carpetas: 1-empresa/, 2-dgii/, 3-ventas/, etc.
- Genera INDICE-DOCUMENTOS.xlsx
- Crea ZIP (típicamente 50-500 MB)
- Upload a S3 y registro en BD

**Tiempo de generación estimado:**

- 1 mes de datos: 2-5 minutos
- 1 año de datos: 10-20 minutos
- 3 años de datos: 30-60 minutos

#### b) ComplianceReportService (0%)

```csharp
// ❌ NO EXISTE: DGIIService.Application/Services/ComplianceReportService.cs

public class ComplianceReportService : IComplianceReportService
{
    // ❌ FALTA: Generar reporte mensual de cumplimiento
    public async Task<ComplianceReport> GenerateMonthlyReportAsync(int year, int month)
    {
        // Verificar:
        // 1. Estado de formatos DGII (606/607/608/IT-1/IR-17)
        // 2. Facturas emitidas con PDF (%)
        // 3. Gastos con documentos (%)
        // 4. NCF verificados (%)
        // 5. Estados bancarios descargados
        // 6. Conciliaciones realizadas
        // 7. Nómina archivada
        // 8. Calcular score 0-100
        // 9. Generar alertas
    }

    // ❌ FALTA: Calcular score de cumplimiento
    private decimal CalculateScore(ComplianceReport report)
    {
        // Formatos DGII: 40%
        // Documentación: 40%
        // NCF: 20%
        // Total: 0-100
    }
}
```

**Score de cumplimiento:**

```
90-100 = 🟢 EXCELENTE (listo para auditoría)
70-89  = 🟡 BUENO (pocos ajustes)
50-69  = 🟠 REGULAR (trabajo pendiente)
0-49   = 🔴 CRÍTICO (bloqueadores)
```

**Alertas generadas:**

- ❌ "Formato 606 de Enero 2026 NO ENVIADO"
- ❌ "15 facturas sin PDF (20% del total)"
- ❌ "30 gastos sin documento (40% del total)"
- ❌ "10 NCF no verificados"
- ⚠️ "Estado de cuenta Popular Diciembre faltante"

#### c) DocumentIndexService (0%)

```csharp
// ❌ NO EXISTE: DGIIService.Application/Services/DocumentIndexService.cs

public class DocumentIndexService : IDocumentIndexService
{
    // ❌ FALTA: Generar índice Excel de documentos
    public async Task<Stream> GenerateIndexAsync(string packagePath)
    {
        // Recorrer carpetas del paquete
        // Contar documentos por tipo
        // Crear Excel con:
        // - Hoja 1: Resumen (carpeta, descripción, cantidad)
        // - Hoja 2: Detalle por mes
        // - Hoja 3: Documentos faltantes (alertas)
    }
}
```

**Índice generado:**

```
INDICE-DOCUMENTOS.xlsx
├── Resumen
│   ├── 1-informacion-empresa (7 docs)
│   ├── 2-formatos-dgii (36 docs: 606x12, 607x12, IT-1x12)
│   ├── 3-facturas-emitidas (248 docs)
│   ├── 4-facturas-recibidas (127 docs)
│   ├── 5-estados-cuenta (24 docs: 2 bancos × 12 meses)
│   └── 6-nomina (12 docs)
└── Detalle por Mes
    └── Enero 2026:
        ├── 606: ✅ Enviado
        ├── 607: ✅ Enviado
        ├── IT-1: ✅ Pagado
        ├── Facturas: 18 PDFs
        └── Gastos: 15 documentos
```

#### d) ResponseTemplateService (0%)

```csharp
// ❌ NO EXISTE: DGIIService.Application/Services/ResponseTemplateService.cs

public class ResponseTemplateService : IResponseTemplateService
{
    // ❌ FALTA: Generar carta de respuesta a DGII
    public async Task<string> GenerateResponseLetterAsync(
        string requirementNumber,
        DateTime requirementDate,
        DateTime responseDate,
        List<DocumentCategory> categories,
        int totalDocuments)
    {
        // Plantilla:
        // - Encabezado DGII
        // - Referencia al requerimiento
        // - Lista de documentos entregados
        // - Total de documentos
        // - Firma
    }
}
```

### 4. Base de Datos (0% - Tablas NO EXISTEN)

```sql
-- ❌ NO EXISTE: audit_packages table

CREATE TABLE audit_packages (
    id UUID PRIMARY KEY,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    categories TEXT[] NOT NULL, -- ['CompanyInfo', 'DGIIFormats', 'SalesInvoices', ...]
    zip_file_url TEXT NOT NULL, -- s3://audit/packages/{id}/auditoria-okla-202601-202612.zip
    file_size_bytes BIGINT NOT NULL,
    document_count INTEGER NOT NULL,
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    generated_by UUID NOT NULL REFERENCES users(id),
    downloaded_at TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'Generated', -- Generated, Downloaded, Submitted
    notes TEXT,
    -- Metadata
    company_info_count INTEGER DEFAULT 0,
    dgii_formats_count INTEGER DEFAULT 0,
    sales_invoices_count INTEGER DEFAULT 0,
    expense_invoices_count INTEGER DEFAULT 0,
    bank_statements_count INTEGER DEFAULT 0,
    payroll_count INTEGER DEFAULT 0,
    INDEX idx_dates (start_date, end_date),
    INDEX idx_status (status),
    INDEX idx_generated_at (generated_at DESC)
);

-- ❌ NO EXISTE: compliance_reports table

CREATE TABLE compliance_reports (
    id UUID PRIMARY KEY,
    year INTEGER NOT NULL,
    month INTEGER NOT NULL,
    compliance_score DECIMAL(5,2) NOT NULL, -- 0-100
    -- Formatos DGII
    format_606_submitted BOOLEAN DEFAULT FALSE,
    format_607_submitted BOOLEAN DEFAULT FALSE,
    format_608_submitted BOOLEAN DEFAULT FALSE,
    it1_submitted BOOLEAN DEFAULT FALSE,
    ir17_submitted BOOLEAN DEFAULT FALSE,
    -- Documentación
    sales_invoices_count INTEGER DEFAULT 0,
    sales_invoices_with_pdf INTEGER DEFAULT 0,
    expenses_count INTEGER DEFAULT 0,
    expenses_with_documents INTEGER DEFAULT 0,
    expenses_with_ncf_verified INTEGER DEFAULT 0,
    -- NCF
    ncf_sequences_active BOOLEAN DEFAULT FALSE,
    ncf_minimum_remaining INTEGER DEFAULT 0,
    -- Alertas
    alerts TEXT[], -- ['Formato 606 NO enviado', ...]
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (year, month),
    INDEX idx_score (compliance_score DESC),
    INDEX idx_period (year, month)
);

-- ❌ NO EXISTE: dgii_responses table

CREATE TABLE dgii_responses (
    id UUID PRIMARY KEY,
    requirement_number VARCHAR(50) NOT NULL UNIQUE,
    requirement_date DATE NOT NULL,
    response_date DATE,
    response_deadline DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Responded, Closed
    audit_package_id UUID REFERENCES audit_packages(id),
    response_letter_url TEXT, -- PDF de carta de respuesta
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    INDEX idx_status (status),
    INDEX idx_deadline (response_deadline)
);
```

---

## 🎨 ANÁLISIS DE FRONTEND

### Estado Actual: 0% Implementado

**❌ PÁGINAS COMPLETAMENTE FALTANTES:**

| Página                        | Ruta                            | Funcionalidad           | Backend | UI    | SP    |
| ----------------------------- | ------------------------------- | ----------------------- | ------- | ----- | ----- |
| **AuditPreparationDashboard** | `/admin/audit/preparation`      | Dashboard principal     | 🔴 0%   | 🔴 0% | 13 SP |
| **GenerateAuditPackagePage**  | `/admin/audit/generate-package` | Generador 1-click       | 🔴 0%   | 🔴 0% | 21 SP |
| **ComplianceScorePage**       | `/admin/audit/compliance-score` | Score mensual           | 🔴 0%   | 🔴 0% | 13 SP |
| **DocumentChecklistPage**     | `/admin/audit/checklist`        | Checklist pre-auditoría | 🔴 0%   | 🔴 0% | 8 SP  |
| **AuditPackagesHistoryPage**  | `/admin/audit/packages`         | Histórico de paquetes   | 🔴 0%   | 🔴 0% | 5 SP  |

**❌ COMPONENTES COMPLETAMENTE FALTANTES:**

1. ComplianceScoreCircle (8 SP) - Gráfico circular 0-100
2. AuditCategorySelector (5 SP) - Selector de categorías
3. DocumentCountCard (3 SP) - Card con contador
4. GeneratePackageButton (5 SP) - Botón con progress bar
5. AlertsList (5 SP) - Lista de alertas con íconos
6. PackageDownloadButton (3 SP) - Descarga directa

**❌ SERVICIOS COMPLETAMENTE FALTANTES:**

```typescript
// ❌ NO EXISTE: src/services/auditPreparationService.ts

export class AuditPreparationService {
  // ❌ FALTA: Generar paquete de auditoría
  async generatePackage(request: GeneratePackageRequest): Promise<AuditPackage>;

  // ❌ FALTA: Obtener compliance score mensual
  async getComplianceReport(
    year: number,
    month: number,
  ): Promise<ComplianceReport>;

  // ❌ FALTA: Descargar paquete generado
  async downloadPackage(packageId: string): Promise<Blob>;

  // ❌ FALTA: Listar paquetes históricos
  async getPackages(year?: number): Promise<AuditPackage[]>;
}
```

---

## 📦 REQUISITOS FALTANTES DETALLADOS

### 1. Generación de Paquete de Auditoría (1 Click) - 34 SP

**Objetivo:** Admin click botón → ZIP listo en 5-20 minutos

**Backend (21 SP):**

```csharp
// DGIIService.Api/Controllers/AuditPackageController.cs
[ApiController]
[Route("api/audit-packages")]
[Authorize(Roles = "Admin")]
public class AuditPackageController : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<AuditPackageResult>> Generate(
        [FromBody] GeneratePackageRequest request)
    {
        // request.StartDate = 2026-01-01
        // request.EndDate = 2026-12-31
        // request.Categories = [CompanyInfo, DGIIFormats, SalesInvoices, ...]

        var result = await _packageService.GenerateAsync(
            request.StartDate,
            request.EndDate,
            request.Categories);

        // result.PackageId = guid
        // result.DownloadUrl = s3://...
        // result.FileSizeMB = 125.5
        // result.DocumentCount = 387

        return Ok(result);
    }

    [HttpGet("{packageId}")]
    public async Task<IActionResult> Download(Guid packageId)
    {
        var package = await _packageService.GetByIdAsync(packageId);
        return Redirect(package.ZipFileUrl); // S3 presigned URL
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditPackage>>> GetAll(
        [FromQuery] int? year = null)
    {
        var packages = await _packageService.GetPackagesAsync(year);
        return Ok(packages);
    }
}
```

**AuditPackageService.GenerateAsync() - Proceso:**

```
1️⃣ CREAR DIRECTORIO TEMPORAL
   └── /tmp/{packageId}/

2️⃣ DESCARGAR CATEGORÍA: Información Empresa (si seleccionada)
   └── 1-informacion-empresa/
       ├── registro-mercantil.pdf
       ├── acta-constitutiva.pdf
       ├── estatutos.pdf
       └── rnc.pdf

3️⃣ DESCARGAR CATEGORÍA: Formatos DGII (si seleccionada)
   └── 2-formatos-dgii/
       ├── 2026-01/
       │   ├── 606_133325901_202601.txt
       │   ├── 606_133325901_202601_acuse.pdf
       │   ├── 607_133325901_202601.txt
       │   ├── 607_133325901_202601_acuse.pdf
       │   ├── it1_202601.pdf
       │   └── ir17_202601.pdf
       ├── 2026-02/
       └── ...

4️⃣ DESCARGAR CATEGORÍA: Facturas Emitidas (si seleccionada)
   └── 3-facturas-emitidas/
       ├── 2026-01/
       │   ├── B0100000001.pdf
       │   ├── B0100000002.pdf
       │   └── ...
       └── libro-ventas-2026.xlsx (generado)

5️⃣ DESCARGAR CATEGORÍA: Facturas Recibidas (si seleccionada)
   └── 4-facturas-recibidas/
       ├── locales/
       │   └── 2026-01/
       │       ├── contador-b0100000789.pdf
       │       └── ...
       ├── internacionales/
       │   └── 2026-01/
       │       ├── digitalocean-inv-001234.pdf
       │       └── ...
       └── libro-compras-2026.xlsx (generado)

6️⃣ DESCARGAR CATEGORÍA: Estados de Cuenta (si seleccionada)
   └── 5-estados-cuenta/
       ├── popular/
       │   ├── 2026-01.pdf
       │   └── ...
       └── bhd/

7️⃣ DESCARGAR CATEGORÍA: Nómina (si seleccionada)
   └── 6-nomina/
       ├── 2026-01/
       │   ├── nomina-enero-2026.xlsx
       │   ├── tss-enero-2026.pdf
       │   └── ir3-enero-2026.pdf
       └── ...

8️⃣ GENERAR ÍNDICE
   └── INDICE-DOCUMENTOS.xlsx
       ├── Hoja 1: Resumen (carpeta, descripción, cantidad)
       ├── Hoja 2: Detalle por mes
       └── Hoja 3: Alertas

9️⃣ CREAR ZIP
   └── auditoria-okla-202601-202612.zip (125 MB)

🔟 UPLOAD A S3
   └── s3://okla-media/audit/packages/{packageId}/auditoria-okla-202601-202612.zip

1️⃣1️⃣ REGISTRAR EN BD
   └── INSERT INTO audit_packages (...)
```

**Frontend (13 SP):**

```tsx
// frontend/web/src/pages/admin/GenerateAuditPackagePage.tsx

export const GenerateAuditPackagePage = () => {
  const [startDate, setStartDate] = useState<Date>(new Date(2026, 0, 1));
  const [endDate, setEndDate] = useState<Date>(new Date(2026, 11, 31));
  const [selectedCategories, setSelectedCategories] = useState<AuditCategory[]>(
    [
      "CompanyInfo",
      "DGIIFormats",
      "SalesInvoices",
      "ExpenseInvoices",
      "BankStatements",
      "Payroll",
    ],
  );
  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [result, setResult] = useState<AuditPackageResult | null>(null);

  const generatePackage = useMutation({
    mutationFn: (request: GeneratePackageRequest) =>
      auditService.generatePackage(request),
    onMutate: () => {
      setIsGenerating(true);
      setProgress(0);
      // Simular progreso mientras genera
      const interval = setInterval(() => {
        setProgress((p) => Math.min(p + 10, 90));
      }, 2000);
      return { interval };
    },
    onSuccess: (data, _, context) => {
      clearInterval(context.interval);
      setProgress(100);
      setResult(data);
      toast.success(`Paquete generado: ${data.documentCount} documentos`);
    },
    onError: (error, _, context) => {
      clearInterval(context.interval);
      setIsGenerating(false);
      toast.error("Error generando paquete");
    },
  });

  const handleGenerate = () => {
    generatePackage.mutate({
      startDate,
      endDate,
      categories: selectedCategories,
    });
  };

  return (
    <MainLayout>
      <div className="p-6 space-y-6">
        <h1 className="text-3xl font-bold">Generar Paquete de Auditoría</h1>

        {/* Selector de Período */}
        <Card>
          <CardHeader>
            <CardTitle>1. Seleccionar Período</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Fecha Inicio</Label>
                <Input
                  type="date"
                  value={format(startDate, "yyyy-MM-dd")}
                  onChange={(e) => setStartDate(new Date(e.target.value))}
                />
              </div>
              <div>
                <Label>Fecha Fin</Label>
                <Input
                  type="date"
                  value={format(endDate, "yyyy-MM-dd")}
                  onChange={(e) => setEndDate(new Date(e.target.value))}
                />
              </div>
            </div>
            <p className="text-sm text-gray-500 mt-2">
              Período: {differenceInMonths(endDate, startDate) + 1} meses
            </p>
          </CardContent>
        </Card>

        {/* Selector de Categorías */}
        <Card>
          <CardHeader>
            <CardTitle>2. Seleccionar Categorías</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {AUDIT_CATEGORIES.map((category) => (
                <div key={category.value} className="flex items-center gap-2">
                  <Checkbox
                    id={category.value}
                    checked={selectedCategories.includes(category.value)}
                    onCheckedChange={(checked) => {
                      if (checked) {
                        setSelectedCategories([
                          ...selectedCategories,
                          category.value,
                        ]);
                      } else {
                        setSelectedCategories(
                          selectedCategories.filter(
                            (c) => c !== category.value,
                          ),
                        );
                      }
                    }}
                  />
                  <Label
                    htmlFor={category.value}
                    className="flex items-center gap-2"
                  >
                    <category.icon className="h-5 w-5" />
                    <div>
                      <div className="font-medium">{category.label}</div>
                      <div className="text-sm text-gray-500">
                        {category.description}
                      </div>
                    </div>
                  </Label>
                </div>
              ))}
            </div>
            <p className="text-sm text-gray-500 mt-4">
              {selectedCategories.length} de {AUDIT_CATEGORIES.length}{" "}
              categorías seleccionadas
            </p>
          </CardContent>
        </Card>

        {/* Botón Generar */}
        <Card>
          <CardHeader>
            <CardTitle>3. Generar Paquete</CardTitle>
          </CardHeader>
          <CardContent>
            {!isGenerating && !result && (
              <Button
                onClick={handleGenerate}
                size="lg"
                disabled={selectedCategories.length === 0}
                className="w-full"
              >
                <Package className="mr-2" />
                Generar Paquete de Auditoría
              </Button>
            )}

            {isGenerating && (
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <Loader2 className="h-6 w-6 animate-spin text-blue-500" />
                  <span>Generando paquete...</span>
                </div>
                <Progress value={progress} />
                <p className="text-sm text-gray-500">
                  {progress < 30 && "Descargando documentos de S3..."}
                  {progress >= 30 && progress < 60 && "Organizando archivos..."}
                  {progress >= 60 &&
                    progress < 90 &&
                    "Generando índice Excel..."}
                  {progress >= 90 && progress < 100 && "Creando archivo ZIP..."}
                  {progress === 100 && "¡Listo!"}
                </p>
              </div>
            )}

            {result && (
              <div className="space-y-4">
                <Alert>
                  <CheckCircle className="h-4 w-4" />
                  <AlertTitle>Paquete Generado Exitosamente</AlertTitle>
                  <AlertDescription>
                    {result.documentCount} documentos |{" "}
                    {result.fileSizeMB.toFixed(1)} MB
                  </AlertDescription>
                </Alert>

                <div className="grid grid-cols-3 gap-4">
                  <div className="text-center p-4 bg-gray-50 rounded">
                    <FileText className="h-8 w-8 mx-auto text-blue-500" />
                    <div className="mt-2 font-semibold">
                      {result.documentCount}
                    </div>
                    <div className="text-sm text-gray-500">Documentos</div>
                  </div>
                  <div className="text-center p-4 bg-gray-50 rounded">
                    <HardDrive className="h-8 w-8 mx-auto text-green-500" />
                    <div className="mt-2 font-semibold">
                      {result.fileSizeMB.toFixed(1)} MB
                    </div>
                    <div className="text-sm text-gray-500">Tamaño</div>
                  </div>
                  <div className="text-center p-4 bg-gray-50 rounded">
                    <Clock className="h-8 w-8 mx-auto text-orange-500" />
                    <div className="mt-2 font-semibold">
                      {result.durationSeconds}s
                    </div>
                    <div className="text-sm text-gray-500">Tiempo</div>
                  </div>
                </div>

                <div className="flex gap-3">
                  <Button
                    onClick={() => window.open(result.downloadUrl, "_blank")}
                    className="flex-1"
                  >
                    <Download className="mr-2" />
                    Descargar ZIP
                  </Button>
                  <Button
                    onClick={() => {
                      setResult(null);
                      setProgress(0);
                      setIsGenerating(false);
                    }}
                    variant="outline"
                  >
                    Generar Otro
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </MainLayout>
  );
};

const AUDIT_CATEGORIES = [
  {
    value: "CompanyInfo",
    label: "Información de la Empresa",
    description: "Registro Mercantil, Actas, RNC, Estatutos",
    icon: Building,
  },
  {
    value: "DGIIFormats",
    label: "Formatos DGII",
    description: "606, 607, 608, IT-1, IR-17 con acuses",
    icon: FileText,
  },
  {
    value: "SalesInvoices",
    label: "Facturas Emitidas",
    description: "Todas las facturas B01/B02 con NCF",
    icon: Receipt,
  },
  {
    value: "ExpenseInvoices",
    label: "Facturas Recibidas",
    description: "Facturas locales + invoices internacionales",
    icon: ShoppingCart,
  },
  {
    value: "BankStatements",
    label: "Estados de Cuenta",
    description: "Estados bancarios + conciliaciones",
    icon: CreditCard,
  },
  {
    value: "Payroll",
    label: "Nómina y TSS",
    description: "Nóminas, TSS, IR-3",
    icon: Users,
  },
];
```

**Valor:**

- Admin puede generar paquete en **1 click**
- Paquete listo en **5-20 minutos** (vs 3-7 días manual)
- **100% completo** sin olvidar documentos
- **ZIP organizado** con índice profesional
- **Listo para entregar** a DGII inmediatamente

### 2. Compliance Score Mensual - 26 SP

**Objetivo:** Dashboard con score 0-100 + alertas

**Backend (13 SP):**

```csharp
// DGIIService.Application/Services/ComplianceReportService.cs

public async Task<ComplianceReport> GenerateMonthlyReportAsync(int year, int month)
{
    var report = new ComplianceReport
    {
        Year = year,
        Month = month,
        GeneratedAt = DateTime.UtcNow
    };

    // 1. Verificar formatos DGII (40% del score)
    report.Format606Submitted = await _dgiiRepo.IsFormatSubmittedAsync("606", year, month);
    report.Format607Submitted = await _dgiiRepo.IsFormatSubmittedAsync("607", year, month);
    report.Format608Submitted = await _dgiiRepo.IsFormatSubmittedAsync("608", year, month);
    report.IT1Submitted = await _dgiiRepo.IsFormatSubmittedAsync("IT1", year, month);
    report.IR17Submitted = await _dgiiRepo.IsFormatSubmittedAsync("IR17", year, month);

    // 2. Verificar documentación (40% del score)
    var period = new DateTime(year, month, 1);
    report.SalesInvoicesCount = await _invoiceRepo.CountByPeriodAsync(period);
    report.SalesInvoicesWithPDF = await _invoiceRepo.CountWithPDFAsync(period);

    report.ExpensesCount = await _expenseRepo.CountByPeriodAsync(period);
    report.ExpensesWithDocuments = await _expenseRepo.CountWithDocumentsAsync(period);
    report.ExpensesWithNCFVerified = await _expenseRepo.CountWithNCFVerifiedAsync(period);

    // 3. Verificar NCF (20% del score)
    var ncfStatus = await _ncfService.GetSequencesStatusAsync();
    report.NCFSequencesActive = ncfStatus.AllActive;
    report.NCFMinimumRemaining = ncfStatus.MinRemaining;

    // 4. Calcular score
    report.ComplianceScore = CalculateScore(report);

    // 5. Generar alertas
    report.Alerts = GenerateAlerts(report);

    // 6. Guardar en BD
    await _reportRepo.SaveAsync(report);

    return report;
}

private decimal CalculateScore(ComplianceReport report)
{
    var scores = new List<decimal>();

    // Formatos DGII (40% - 8 puntos cada uno)
    if (report.Format606Submitted) scores.Add(8);
    if (report.Format607Submitted) scores.Add(8);
    if (report.Format608Submitted) scores.Add(8);
    if (report.IT1Submitted) scores.Add(8);
    if (report.IR17Submitted) scores.Add(8);

    // Documentación (40% - 20 puntos cada categoría)
    var salesDocScore = (report.SalesInvoicesWithPDF * 1.0m / report.SalesInvoicesCount) * 20;
    scores.Add(salesDocScore);

    var expenseDocScore = (report.ExpensesWithDocuments * 1.0m / report.ExpensesCount) * 20;
    scores.Add(expenseDocScore);

    // NCF (20% - 10 puntos cada criterio)
    if (report.NCFSequencesActive) scores.Add(10);
    if (report.NCFMinimumRemaining > 100) scores.Add(10);

    return scores.Sum();
}

private List<string> GenerateAlerts(ComplianceReport report)
{
    var alerts = new List<string>();

    if (!report.Format606Submitted)
        alerts.Add($"❌ Formato 606 de {GetMonthName(report.Month)} {report.Year} NO ENVIADO");

    if (!report.Format607Submitted)
        alerts.Add($"❌ Formato 607 de {GetMonthName(report.Month)} {report.Year} NO ENVIADO");

    if (!report.IT1Submitted)
        alerts.Add($"❌ IT-1 de {GetMonthName(report.Month)} {report.Year} NO PRESENTADO");

    if (!report.IR17Submitted)
        alerts.Add($"❌ IR-17 de {GetMonthName(report.Month)} {report.Year} NO PRESENTADO");

    var salesWithoutPDF = report.SalesInvoicesCount - report.SalesInvoicesWithPDF;
    if (salesWithoutPDF > 0)
    {
        var percentage = (salesWithoutPDF * 100.0m / report.SalesInvoicesCount);
        alerts.Add($"⚠️ {salesWithoutPDF} facturas sin PDF ({percentage:0.0}% del total)");
    }

    var expensesWithoutDoc = report.ExpensesCount - report.ExpensesWithDocuments;
    if (expensesWithoutDoc > 0)
    {
        var percentage = (expensesWithoutDoc * 100.0m / report.ExpensesCount);
        alerts.Add($"⚠️ {expensesWithoutDoc} gastos sin documento ({percentage:0.0}% del total)");
    }

    var ncfNotVerified = report.ExpensesWithDocuments - report.ExpensesWithNCFVerified;
    if (ncfNotVerified > 0)
        alerts.Add($"⚠️ {ncfNotVerified} NCF sin verificar");

    if (!report.NCFSequencesActive)
        alerts.Add($"❌ Secuencias NCF NO ACTIVAS");

    if (report.NCFMinimumRemaining < 100)
        alerts.Add($"⚠️ Solo {report.NCFMinimumRemaining} NCF restantes - Solicitar nuevos");

    return alerts;
}
```

**Frontend (13 SP):**

```tsx
// frontend/web/src/pages/admin/ComplianceScorePage.tsx

export const ComplianceScorePage = () => {
  const [selectedYear, setSelectedYear] = useState(2026);
  const [selectedMonth, setSelectedMonth] = useState(1);

  const { data: report, isLoading } = useQuery({
    queryKey: ["compliance-report", selectedYear, selectedMonth],
    queryFn: () => complianceService.getReport(selectedYear, selectedMonth),
  });

  const getScoreColor = (score: number) => {
    if (score >= 90) return "text-green-500";
    if (score >= 70) return "text-yellow-500";
    if (score >= 50) return "text-orange-500";
    return "text-red-500";
  };

  const getScoreLabel = (score: number) => {
    if (score >= 90) return "🟢 EXCELENTE - Listo para auditoría";
    if (score >= 70) return "🟡 BUENO - Pocos ajustes pendientes";
    if (score >= 50) return "🟠 REGULAR - Trabajo pendiente";
    return "🔴 CRÍTICO - Bloqueadores importantes";
  };

  return (
    <MainLayout>
      <div className="p-6 space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-3xl font-bold">Score de Cumplimiento Fiscal</h1>
          <div className="flex gap-2">
            <Select
              value={selectedMonth.toString()}
              onValueChange={(v) => setSelectedMonth(parseInt(v))}
            >
              <SelectTrigger className="w-[150px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {MONTHS.map((month, i) => (
                  <SelectItem key={i} value={(i + 1).toString()}>
                    {month}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select
              value={selectedYear.toString()}
              onValueChange={(v) => setSelectedYear(parseInt(v))}
            >
              <SelectTrigger className="w-[100px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="2024">2024</SelectItem>
                <SelectItem value="2025">2025</SelectItem>
                <SelectItem value="2026">2026</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {isLoading && <div>Cargando...</div>}

        {report && (
          <>
            {/* Score Principal */}
            <Card>
              <CardHeader>
                <CardTitle>Score de Cumplimiento</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-center">
                  <div className="relative">
                    <svg className="w-64 h-64">
                      <circle
                        cx="128"
                        cy="128"
                        r="100"
                        fill="none"
                        stroke="#e5e7eb"
                        strokeWidth="20"
                      />
                      <circle
                        cx="128"
                        cy="128"
                        r="100"
                        fill="none"
                        stroke={getScoreColorHex(report.complianceScore)}
                        strokeWidth="20"
                        strokeDasharray={`${(report.complianceScore / 100) * 628} 628`}
                        strokeDashoffset="0"
                        transform="rotate(-90 128 128)"
                        strokeLinecap="round"
                      />
                    </svg>
                    <div className="absolute inset-0 flex flex-col items-center justify-center">
                      <div
                        className={`text-5xl font-bold ${getScoreColor(report.complianceScore)}`}
                      >
                        {report.complianceScore}
                      </div>
                      <div className="text-sm text-gray-500 mt-1">de 100</div>
                    </div>
                  </div>
                </div>
                <p className="text-center mt-6 text-lg">
                  {getScoreLabel(report.complianceScore)}
                </p>
              </CardContent>
            </Card>

            {/* Alertas */}
            {report.alerts && report.alerts.length > 0 && (
              <Card className="border-red-200">
                <CardHeader>
                  <CardTitle className="text-red-600 flex items-center gap-2">
                    <AlertTriangle className="h-5 w-5" />
                    Alertas de Cumplimiento
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-2">
                    {report.alerts.map((alert, i) => (
                      <li key={i} className="flex items-start gap-2">
                        <div className="mt-0.5">
                          {alert.startsWith("❌") ? (
                            <XCircle className="h-5 w-5 text-red-500" />
                          ) : (
                            <AlertTriangle className="h-5 w-5 text-orange-500" />
                          )}
                        </div>
                        <span>{alert.replace(/^[❌⚠️]\s/, "")}</span>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            )}

            {/* Estado de Formatos DGII */}
            <Card>
              <CardHeader>
                <CardTitle>Estado de Formatos DGII (40% del score)</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-5 gap-4">
                  <FormatStatusCard
                    name="606"
                    submitted={report.format606Submitted}
                    label="Formato 606"
                    description="Compras"
                  />
                  <FormatStatusCard
                    name="607"
                    submitted={report.format607Submitted}
                    label="Formato 607"
                    description="Ventas"
                  />
                  <FormatStatusCard
                    name="608"
                    submitted={report.format608Submitted}
                    label="Formato 608"
                    description="Anulaciones"
                  />
                  <FormatStatusCard
                    name="IT-1"
                    submitted={report.it1Submitted}
                    label="IT-1"
                    description="ITBIS"
                  />
                  <FormatStatusCard
                    name="IR-17"
                    submitted={report.ir17Submitted}
                    label="IR-17"
                    description="Retenciones"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Estado de Documentación */}
            <Card>
              <CardHeader>
                <CardTitle>Estado de Documentación (40% del score)</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <ProgressBar
                    label="Facturas emitidas con PDF"
                    current={report.salesInvoicesWithPDF}
                    total={report.salesInvoicesCount}
                    color="blue"
                  />
                  <ProgressBar
                    label="Gastos con documentos"
                    current={report.expensesWithDocuments}
                    total={report.expensesCount}
                    color="green"
                  />
                  <ProgressBar
                    label="NCF verificados"
                    current={report.expensesWithNCFVerified}
                    total={report.expensesWithDocuments}
                    color="purple"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Estado de NCF */}
            <Card>
              <CardHeader>
                <CardTitle>Estado de NCF (20% del score)</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 gap-4">
                  <div className="p-4 bg-gray-50 rounded">
                    <div className="flex items-center gap-2 mb-2">
                      {report.ncfSequencesActive ? (
                        <CheckCircle className="h-5 w-5 text-green-500" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-500" />
                      )}
                      <span className="font-medium">Secuencias Activas</span>
                    </div>
                    <p className="text-sm text-gray-500">
                      {report.ncfSequencesActive
                        ? "Todas activas"
                        : "Inactivas"}
                    </p>
                  </div>
                  <div className="p-4 bg-gray-50 rounded">
                    <div className="flex items-center gap-2 mb-2">
                      {report.ncfMinimumRemaining > 100 ? (
                        <CheckCircle className="h-5 w-5 text-green-500" />
                      ) : (
                        <AlertTriangle className="h-5 w-5 text-orange-500" />
                      )}
                      <span className="font-medium">NCF Restantes</span>
                    </div>
                    <p className="text-sm text-gray-500">
                      {report.ncfMinimumRemaining} números restantes
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </>
        )}
      </div>
    </MainLayout>
  );
};

// Helper component
const FormatStatusCard = ({ name, submitted, label, description }) => (
  <div className="text-center p-4 bg-gray-50 rounded">
    {submitted ? (
      <CheckCircle className="h-8 w-8 mx-auto text-green-500" />
    ) : (
      <XCircle className="h-8 w-8 mx-auto text-red-500" />
    )}
    <div className="mt-2 font-semibold">{label}</div>
    <div className="text-sm text-gray-500">{description}</div>
    <Badge className="mt-2" variant={submitted ? "default" : "destructive"}>
      {submitted ? "Enviado" : "Pendiente"}
    </Badge>
  </div>
);

const ProgressBar = ({ label, current, total, color }) => {
  const percentage = (current / total) * 100;
  return (
    <div>
      <div className="flex justify-between mb-1">
        <span className="text-sm font-medium">{label}</span>
        <span className="text-sm text-gray-500">
          {current} / {total} ({percentage.toFixed(0)}%)
        </span>
      </div>
      <div className="w-full bg-gray-200 rounded-full h-2.5">
        <div
          className={`bg-${color}-600 h-2.5 rounded-full`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
};
```

**Valor:**

- **Score visual 0-100** con gráfico circular
- **Identificación inmediata** de problemas
- **Alertas accionables** con prioridades
- **Seguimiento mensual** del cumplimiento
- **Prevención de multas** con alertas tempranas

### 3. Checklist Pre-Auditoría Mensual - 21 SP

**Objetivo:** Checklist automatizado que admin revisa cada mes

**Backend (13 SP):**

```csharp
// DGIIService.Application/Services/ChecklistService.cs

public async Task<MonthlyChecklist> GenerateChecklistAsync(int year, int month)
{
    var checklist = new MonthlyChecklist
    {
        Year = year,
        Month = month,
        GeneratedAt = DateTime.UtcNow
    };

    // 1. Documentos del mes
    checklist.Format606Generated = await _dgiiRepo.IsFormatSubmittedAsync("606", year, month);
    checklist.Format607Generated = await _dgiiRepo.IsFormatSubmittedAsync("607", year, month);
    checklist.Format608Generated = await _dgiiRepo.IsFormatSubmittedAsync("608", year, month);
    checklist.IT1Submitted = await _dgiiRepo.IsFormatSubmittedAsync("IT1", year, month);
    checklist.IR17Submitted = await _dgiiRepo.IsFormatSubmittedAsync("IR17", year, month);
    checklist.DGIIAcusesArchived = await CheckAcusesArchivedAsync(year, month);

    // 2. Facturas emitidas
    var period = new DateTime(year, month, 1);
    checklist.AllInvoicesHaveNCF = await _invoiceRepo.AllHaveNCFAsync(period);
    checklist.InvoicePDFsArchived = await _invoiceRepo.AllHavePDFAsync(period);
    checklist.SalesBookUpdated = await CheckSalesBookAsync(year, month);
    checklist.Reconciliation607Done = await CheckReconciliation607Async(year, month);

    // 3. Facturas recibidas
    checklist.AllLocalExpensesHaveNCF = await _expenseRepo.AllLocalHaveNCFAsync(period);
    checklist.NCFVerified = await _expenseRepo.AllNCFVerifiedAsync(period);
    checklist.InternationalInvoicesArchived = await CheckIntlInvoicesAsync(year, month);
    checklist.PurchasesBookUpdated = await CheckPurchasesBookAsync(year, month);
    checklist.Reconciliation606Done = await CheckReconciliation606Async(year, month);

    // 4. Bancos
    checklist.BankStatementsDownloaded = await CheckBankStatementsAsync(year, month);
    checklist.BankReconciliationDone = await CheckBankReconciliationAsync(year, month);
    checklist.TransferVouchersArchived = await CheckTransferVouchersAsync(year, month);

    // 5. Nómina
    checklist.PayrollArchived = await CheckPayrollAsync(year, month);
    checklist.TSSPaidAndArchived = await CheckTSSAsync(year, month);
    checklist.IR3IncludedInIR17 = await CheckIR3Async(year, month);

    // 6. Verificación
    checklist.Format606TotalsMatch = await Verify606TotalsAsync(year, month);
    checklist.Format607TotalsMatch = await Verify607TotalsAsync(year, month);
    checklist.ITBISCalculationMatches = await VerifyITBISAsync(year, month);
    checklist.IR17RetentionsMatch = await VerifyIR17Async(year, month);

    // 7. Calcular progreso
    checklist.CompletionPercentage = CalculateCompletion(checklist);
    checklist.PendingItems = GetPendingItems(checklist);

    return checklist;
}

private decimal CalculateCompletion(MonthlyChecklist checklist)
{
    var totalItems = 25; // Total de checkpoints
    var completedItems = 0;

    // Contar items completados (todos los bool properties = true)
    var properties = checklist.GetType().GetProperties()
        .Where(p => p.PropertyType == typeof(bool));

    foreach (var prop in properties)
    {
        if ((bool)prop.GetValue(checklist))
            completedItems++;
    }

    return (completedItems * 100.0m) / totalItems;
}
```

**Frontend (8 SP):**

```tsx
// frontend/web/src/pages/admin/DocumentChecklistPage.tsx

export const DocumentChecklistPage = () => {
  const [selectedYear, setSelectedYear] = useState(2026);
  const [selectedMonth, setSelectedMonth] = useState(1);

  const { data: checklist, isLoading } = useQuery({
    queryKey: ["checklist", selectedYear, selectedMonth],
    queryFn: () => complianceService.getChecklist(selectedYear, selectedMonth),
  });

  const updateItem = useMutation({
    mutationFn: ({ itemKey, value }: { itemKey: string; value: boolean }) =>
      complianceService.updateChecklistItem(
        selectedYear,
        selectedMonth,
        itemKey,
        value,
      ),
    onSuccess: () => {
      queryClient.invalidateQueries(["checklist"]);
      toast.success("Checklist actualizado");
    },
  });

  return (
    <MainLayout>
      <div className="p-6 space-y-6">
        <h1 className="text-3xl font-bold">Checklist de Documentos</h1>

        {/* Progress Bar */}
        {checklist && (
          <Card>
            <CardHeader>
              <CardTitle>Progreso del Mes</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <Progress value={checklist.completionPercentage} />
                </div>
                <div className="text-2xl font-bold">
                  {checklist.completionPercentage.toFixed(0)}%
                </div>
              </div>
              <p className="text-sm text-gray-500 mt-2">
                {checklist.pendingItems.length} items pendientes
              </p>
            </CardContent>
          </Card>
        )}

        {checklist && (
          <>
            {/* Documentos del Mes */}
            <ChecklistSection
              title="Documentos del Mes"
              items={[
                {
                  key: "format606Generated",
                  label: "Formato 606 generado y enviado",
                  checked: checklist.format606Generated,
                },
                {
                  key: "format607Generated",
                  label: "Formato 607 generado y enviado",
                  checked: checklist.format607Generated,
                },
                {
                  key: "format608Generated",
                  label: "Formato 608 generado y enviado (si hay anulaciones)",
                  checked: checklist.format608Generated,
                },
                {
                  key: "it1Submitted",
                  label: "IT-1 presentado y pagado",
                  checked: checklist.it1Submitted,
                },
                {
                  key: "ir17Submitted",
                  label: "IR-17 presentado y pagado",
                  checked: checklist.ir17Submitted,
                },
                {
                  key: "dgiiAcusesArchived",
                  label: "Acuses de DGII archivados en S3",
                  checked: checklist.dgiiAcusesArchived,
                },
              ]}
              onToggle={(key, value) =>
                updateItem.mutate({ itemKey: key, value })
              }
            />

            {/* Facturas Emitidas */}
            <ChecklistSection
              title="Facturas Emitidas"
              items={[
                {
                  key: "allInvoicesHaveNCF",
                  label: "Todas las facturas tienen NCF",
                  checked: checklist.allInvoicesHaveNCF,
                },
                {
                  key: "invoicePDFsArchived",
                  label: "PDFs archivados en S3",
                  checked: checklist.invoicePDFsArchived,
                },
                {
                  key: "salesBookUpdated",
                  label: "Libro de ventas actualizado",
                  checked: checklist.salesBookUpdated,
                },
                {
                  key: "reconciliation607Done",
                  label: "Conciliación con 607 realizada",
                  checked: checklist.reconciliation607Done,
                },
              ]}
              onToggle={(key, value) =>
                updateItem.mutate({ itemKey: key, value })
              }
            />

            {/* Facturas Recibidas */}
            <ChecklistSection
              title="Facturas Recibidas"
              items={[
                {
                  key: "allLocalExpensesHaveNCF",
                  label: "Todas las facturas locales tienen NCF",
                  checked: checklist.allLocalExpensesHaveNCF,
                },
                {
                  key: "ncfVerified",
                  label: "NCF verificados en DGII",
                  checked: checklist.ncfVerified,
                },
                {
                  key: "internationalInvoicesArchived",
                  label: "Invoices internacionales archivados",
                  checked: checklist.internationalInvoicesArchived,
                },
                {
                  key: "purchasesBookUpdated",
                  label: "Libro de compras actualizado",
                  checked: checklist.purchasesBookUpdated,
                },
                {
                  key: "reconciliation606Done",
                  label: "Conciliación con 606 realizada",
                  checked: checklist.reconciliation606Done,
                },
              ]}
              onToggle={(key, value) =>
                updateItem.mutate({ itemKey: key, value })
              }
            />

            {/* Bancos */}
            <ChecklistSection
              title="Bancos"
              items={[
                {
                  key: "bankStatementsDownloaded",
                  label: "Estados de cuenta descargados",
                  checked: checklist.bankStatementsDownloaded,
                },
                {
                  key: "bankReconciliationDone",
                  label: "Conciliación bancaria realizada",
                  checked: checklist.bankReconciliationDone,
                },
                {
                  key: "transferVouchersArchived",
                  label: "Comprobantes de transferencia archivados",
                  checked: checklist.transferVouchersArchived,
                },
              ]}
              onToggle={(key, value) =>
                updateItem.mutate({ itemKey: key, value })
              }
            />

            {/* Nómina */}
            <ChecklistSection
              title="Nómina"
              items={[
                {
                  key: "payrollArchived",
                  label: "Nómina del mes archivada",
                  checked: checklist.payrollArchived,
                },
                {
                  key: "tssPaidAndArchived",
                  label: "TSS pagado y comprobante archivado",
                  checked: checklist.tssPaidAndArchived,
                },
                {
                  key: "ir3IncludedInIR17",
                  label: "IR-3 incluido en IR-17",
                  checked: checklist.ir3IncludedInIR17,
                },
              ]}
              onToggle={(key, value) =>
                updateItem.mutate({ itemKey: key, value })
              }
            />

            {/* Verificación */}
            <ChecklistSection
              title="Verificación"
              items={[
                {
                  key: "format606TotalsMatch",
                  label: "Totales de 606 = Libro de compras",
                  checked: checklist.format606TotalsMatch,
                },
                {
                  key: "format607TotalsMatch",
                  label: "Totales de 607 = Libro de ventas",
                  checked: checklist.format607TotalsMatch,
                },
                {
                  key: "itbisCalculationMatches",
                  label: "ITBIS calculado coincide con IT-1",
                  checked: checklist.itbisCalculationMatches,
                },
                {
                  key: "ir17RetentionsMatch",
                  label: "Retenciones IR-17 = Suma de retenciones",
                  checked: checklist.ir17RetentionsMatch,
                },
              ]}
              onToggle={(key, value) =>
                updateItem.mutate({ itemKey: key, value })
              }
            />
          </>
        )}
      </div>
    </MainLayout>
  );
};

const ChecklistSection = ({ title, items, onToggle }) => (
  <Card>
    <CardHeader>
      <CardTitle>{title}</CardTitle>
    </CardHeader>
    <CardContent>
      <div className="space-y-3">
        {items.map((item) => (
          <div key={item.key} className="flex items-center gap-3">
            <Checkbox
              id={item.key}
              checked={item.checked}
              onCheckedChange={(checked) => onToggle(item.key, checked)}
            />
            <Label htmlFor={item.key} className="flex-1 cursor-pointer">
              {item.label}
            </Label>
            {item.checked && <CheckCircle className="h-5 w-5 text-green-500" />}
          </div>
        ))}
      </div>
    </CardContent>
  </Card>
);
```

**Valor:**

- **25 checkpoints** automatizados por mes
- **Progreso visual** 0-100%
- **Identificación de gaps** antes de auditoría
- **Prevención de olvidos** con checklist mensual
- **Histórico** de compliance por mes

### 4. Dashboard de Preparación - 21 SP

**Objetivo:** Vista general del estado de preparación

**Frontend (21 SP):**

```tsx
// frontend/web/src/pages/admin/AuditPreparationDashboard.tsx

export const AuditPreparationDashboard = () => {
  const { data: overview } = useQuery({
    queryKey: ["audit-overview"],
    queryFn: () => auditService.getOverview(),
  });

  const { data: recentPackages } = useQuery({
    queryKey: ["recent-packages"],
    queryFn: () => auditService.getRecentPackages(5),
  });

  return (
    <MainLayout>
      <div className="p-6 space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-3xl font-bold">
            Preparación para Auditoría DGII
          </h1>
          <Button asChild>
            <Link to="/admin/audit/generate-package">
              <Package className="mr-2" />
              Generar Paquete
            </Link>
          </Button>
        </div>

        {/* Cards de Estado */}
        <div className="grid grid-cols-4 gap-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">
                Score Cumplimiento
              </CardTitle>
              <Activity className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {overview?.currentScore || 0}
              </div>
              <p className="text-xs text-muted-foreground">
                {overview?.scoreTrend > 0 &&
                  `+${overview.scoreTrend}% vs mes anterior`}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">
                Documentos Totales
              </CardTitle>
              <FileText className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {overview?.totalDocuments || 0}
              </div>
              <p className="text-xs text-muted-foreground">Archivados en S3</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">
                Paquetes Generados
              </CardTitle>
              <Package className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {overview?.packagesGenerated || 0}
              </div>
              <p className="text-xs text-muted-foreground">
                En los últimos 12 meses
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">
                Alertas Activas
              </CardTitle>
              <AlertTriangle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-red-500">
                {overview?.activeAlerts || 0}
              </div>
              <p className="text-xs text-muted-foreground">
                Requieren atención
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Acciones Rápidas */}
        <Card>
          <CardHeader>
            <CardTitle>Acciones Rápidas</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-3 gap-4">
              <Button asChild variant="outline" className="h-20 flex-col">
                <Link to="/admin/audit/generate-package">
                  <Package className="h-8 w-8 mb-2" />
                  <span>Generar Paquete</span>
                </Link>
              </Button>
              <Button asChild variant="outline" className="h-20 flex-col">
                <Link to="/admin/audit/compliance-score">
                  <Activity className="h-8 w-8 mb-2" />
                  <span>Ver Score</span>
                </Link>
              </Button>
              <Button asChild variant="outline" className="h-20 flex-col">
                <Link to="/admin/audit/checklist">
                  <CheckSquare className="h-8 w-8 mb-2" />
                  <span>Checklist</span>
                </Link>
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Paquetes Recientes */}
        <Card>
          <CardHeader>
            <CardTitle>Paquetes Generados Recientemente</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Período</TableHead>
                  <TableHead>Documentos</TableHead>
                  <TableHead>Tamaño</TableHead>
                  <TableHead>Fecha</TableHead>
                  <TableHead>Acciones</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recentPackages?.map((pkg) => (
                  <TableRow key={pkg.id}>
                    <TableCell>
                      {format(pkg.startDate, "MMM yyyy")} -{" "}
                      {format(pkg.endDate, "MMM yyyy")}
                    </TableCell>
                    <TableCell>{pkg.documentCount}</TableCell>
                    <TableCell>{pkg.fileSizeMB.toFixed(1)} MB</TableCell>
                    <TableCell>
                      {formatDistanceToNow(pkg.generatedAt, {
                        addSuffix: true,
                      })}
                    </TableCell>
                    <TableCell>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => window.open(pkg.downloadUrl, "_blank")}
                      >
                        <Download className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </MainLayout>
  );
};
```

### 5. Response Templates - 13 SP

**Objetivo:** Plantillas de cartas de respuesta a DGII

**Backend (8 SP):**

```csharp
// DGIIService.Application/Services/ResponseTemplateService.cs

public async Task<ResponseLetter> GenerateResponseLetterAsync(
    string requirementNumber,
    DateTime requirementDate,
    DateTime responseDate,
    List<DocumentCategory> categories,
    int totalDocuments)
{
    var template = $@"
Santo Domingo, {responseDate:dd/MM/yyyy}

Señores
Dirección General de Impuestos Internos
Departamento de Fiscalización
Ciudad.-

REF: Respuesta a Requerimiento No. {requirementNumber}
     Expediente: [NÚMERO EXPEDIENTE]
     Contribuyente: OKLA S.R.L.
     RNC: 1-33-32590-1

Distinguidos señores:

En atención a su requerimiento de fecha {requirementDate:dd/MM/yyyy},
recibido en nuestras oficinas el día {requirementDate.AddDays(2):dd/MM/yyyy}, mediante
el cual solicitan documentación correspondiente al período especificado,
procedemos a entregar los siguientes documentos:

{GenerateDocumentsList(categories)}

TOTAL DE DOCUMENTOS ENTREGADOS: {totalDocuments}

Los documentos se entregan en formato digital (USB) organizados en carpetas
por categoría, con un índice en Excel que detalla cada documento.

Quedamos a su disposición para cualquier aclaración adicional que requieran.

Atentamente,

________________________
Nicauris Mateo Alcántara
Gerente General
OKLA S.R.L.
RNC: 1-33-32590-1
Tel: 809-XXX-XXXX
Email: legal@okla.com.do
";

    return new ResponseLetter
    {
        Content = template,
        RequirementNumber = requirementNumber,
        GeneratedAt = DateTime.UtcNow
    };
}

private string GenerateDocumentsList(List<DocumentCategory> categories)
{
    var sb = new StringBuilder();
    var counter = 1;

    if (categories.Contains(DocumentCategory.CompanyInfo))
    {
        sb.AppendLine($"{counter++}. INFORMACIÓN GENERAL");
        sb.AppendLine("   - Registro Mercantil (1 documento)");
        sb.AppendLine("   - Acta Constitutiva (1 documento)");
        sb.AppendLine();
    }

    if (categories.Contains(DocumentCategory.DGIIFormats))
    {
        sb.AppendLine($"{counter++}. DECLARACIONES DGII");
        sb.AppendLine("   - Formatos 606 del período (X documentos)");
        sb.AppendLine("   - Formatos 607 del período (X documentos)");
        sb.AppendLine("   - IT-1 del período (X documentos)");
        sb.AppendLine("   - IR-17 del período (X documentos)");
        sb.AppendLine();
    }

    // ... más categorías

    return sb.ToString();
}
```

**Frontend (5 SP):**

```tsx
// frontend/web/src/pages/admin/ResponseLetterPage.tsx

export const ResponseLetterPage = () => {
  const [requirementNumber, setRequirementNumber] = useState("");
  const [requirementDate, setRequirementDate] = useState<Date>(new Date());
  const [responseDate, setResponseDate] = useState<Date>(new Date());
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);

  const generateLetter = useMutation({
    mutationFn: (data) => auditService.generateResponseLetter(data),
    onSuccess: (data) => {
      // Abrir PDF generado
      window.open(data.pdfUrl, "_blank");
    },
  });

  return (
    <MainLayout>
      <div className="p-6 space-y-6">
        <h1 className="text-3xl font-bold">Generar Carta de Respuesta DGII</h1>

        <Card>
          <CardHeader>
            <CardTitle>Información del Requerimiento</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label>Número de Requerimiento</Label>
              <Input
                value={requirementNumber}
                onChange={(e) => setRequirementNumber(e.target.value)}
                placeholder="Ej: DGII-2026-12345"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Fecha de Requerimiento</Label>
                <Input
                  type="date"
                  value={format(requirementDate, "yyyy-MM-dd")}
                  onChange={(e) => setRequirementDate(new Date(e.target.value))}
                />
              </div>
              <div>
                <Label>Fecha de Respuesta</Label>
                <Input
                  type="date"
                  value={format(responseDate, "yyyy-MM-dd")}
                  onChange={(e) => setResponseDate(new Date(e.target.value))}
                />
              </div>
            </div>

            <div>
              <Label>Categorías de Documentos Entregados</Label>
              <div className="mt-2 space-y-2">
                {DOCUMENT_CATEGORIES.map((cat) => (
                  <div key={cat.value} className="flex items-center gap-2">
                    <Checkbox
                      id={cat.value}
                      checked={selectedCategories.includes(cat.value)}
                      onCheckedChange={(checked) => {
                        if (checked) {
                          setSelectedCategories([
                            ...selectedCategories,
                            cat.value,
                          ]);
                        } else {
                          setSelectedCategories(
                            selectedCategories.filter((c) => c !== cat.value),
                          );
                        }
                      }}
                    />
                    <Label htmlFor={cat.value}>{cat.label}</Label>
                  </div>
                ))}
              </div>
            </div>

            <Button
              onClick={() =>
                generateLetter.mutate({
                  requirementNumber,
                  requirementDate,
                  responseDate,
                  categories: selectedCategories,
                })
              }
              disabled={!requirementNumber || selectedCategories.length === 0}
              className="w-full"
            >
              <FileText className="mr-2" />
              Generar Carta (PDF)
            </Button>
          </CardContent>
        </Card>
      </div>
    </MainLayout>
  );
};
```

**Valor:**

- **Carta profesional** generada automáticamente
- **Formato estándar** DGII
- **Lista de documentos** organizada
- **PDF listo** para imprimir y entregar
- **Ahorra 30-60 minutos** de redacción manual

---

## 💰 ANÁLISIS FINANCIERO

### Costo Sin Sistema de Preparación

**Respuesta manual a auditoría:**

```
Días 1-2: Reunir documentos dispersos (16 horas)
   ├── Buscar en computadoras (4h)
   ├── Buscar en emails (3h)
   ├── Descargar de S3 sin estructura (5h)
   └── Pedir documentos al contador (4h)

Días 3-5: Organizar documentos (20 horas)
   ├── Crear carpetas manualmente (2h)
   ├── Renombrar archivos (4h)
   ├── Verificar que esté completo (8h)
   └── Crear índice Excel (6h)

Días 6-7: Preparar respuesta (8 horas)
   ├── Redactar carta formal (3h)
   ├── Imprimir documentos (2h)
   ├── Revisar con abogado (2h)
   └── Entregar a DGII (1h)

TOTAL: 44 horas × $50/hora = $2,200 por auditoría
```

**Riesgo de multas:**

- No responder en plazo (5-15 días): RD$10K-$50K ($170-$850 USD)
- Documentos incompletos: Extensión de auditoría
- Mala impresión a DGII: Mayor escrutinio futuro

**Costo anual:**

- 1-2 auditorías/año (promedio)
- Costo manual: $2,200-$4,400/año
- Multas evitadas: $500-$2,000/año
- **Total: $2,700-$6,400/año**

### Costo Con Sistema Automatizado

**Respuesta automática:**

```
Día 1: Generar paquete (1 hora)
   ├── Admin selecciona período (5 min)
   ├── Sistema genera ZIP (10-20 min automatic)
   ├── Descargar y revisar (15 min)
   └── Preparar carta con template (10 min)

Día 1: Entregar a DGII (1 hora)
   ├── Imprimir carta (15 min)
   ├── Copiar USB (15 min)
   ├── Ir a DGII (30 min)

TOTAL: 2 horas × $50/hora = $100 por auditoría
```

**Ahorro por auditoría:** $2,200 - $100 = **$2,100**

**Inversión del sistema:**

- Backend: 55 SP × $140 = $7,700
- Frontend: 60 SP × $140 = $8,400
- **Total: 115 SP = $16,100 USD**

### ROI Calculation

```
Inversión: $16,100
Ahorro anual: $2,700-$6,400
ROI: 2.5 - 6 años

PERO valor principal:
✅ Paz mental: Siempre listo para auditoría
✅ Compliance: Score mensual de cumplimiento
✅ Prevención: Alertas tempranas de problemas
✅ Profesionalismo: Respuesta organizada a DGII
✅ Eliminación de riesgo: No perder plazos
```

### Comparación de Escenarios

| Escenario                    | Tiempo       | Costo      | Riesgo                     | Calidad                         |
| ---------------------------- | ------------ | ---------- | -------------------------- | ------------------------------- |
| **Manual (actual)**          | 44h (7 días) | $2,200     | 🔴 Alto (perder plazo)     | 🟡 Regular (pueden faltar docs) |
| **Automatizado (propuesto)** | 2h (1 día)   | $100       | 🟢 Bajo (siempre a tiempo) | 🟢 Excelente (100% completo)    |
| **Ahorro**                   | **42 horas** | **$2,100** | **Eliminación de riesgo**  | **Garantía de calidad**         |

---

## 📅 PLAN DE IMPLEMENTACIÓN

### Fase 1: Backend Core (34 SP, 2-3 semanas)

**Prioridad:** 🔴 P0 - CRÍTICO

**Tareas:**

1. **Crear DGIIService (si no existe como microservicio separado)**
   - Estructura Clean Architecture
   - Entity: AuditPackage, ComplianceReport, MonthlyChecklist
   - Repositories: AuditPackageRepository, ComplianceReportRepository
   - DbContext con EF Core
   - 3 tablas: audit_packages, compliance_reports, monthly_checklists

2. **Implementar AuditPackageService (21 SP)**
   - GenerateAsync() - Generación completa de paquete
   - DownloadPackageAsync() - Descarga de ZIP
   - GetPackagesAsync() - Histórico
   - Integración con MediaService/S3
   - Generación de índice Excel (EPPlus/ClosedXML)
   - Creación de ZIP (System.IO.Compression)

3. **Implementar ComplianceReportService (13 SP)**
   - GenerateMonthlyReportAsync() - Reporte mensual
   - CalculateScore() - Lógica de score 0-100
   - GenerateAlerts() - Alertas accionables
   - Verificación de formatos DGII
   - Verificación de documentación
   - Verificación de NCF

**Entregables:**

- ✅ 3 controllers: AuditPackage, ComplianceReport, Checklist
- ✅ 3 services completos
- ✅ 3 tablas en PostgreSQL
- ✅ Tests unitarios (70% coverage)
- ✅ Documentación API (Swagger)

### Fase 2: Frontend Dashboard (21 SP, 1-2 semanas)

**Prioridad:** 🔴 P0 - CRÍTICO

**Tareas:**

1. **Dashboard Principal (8 SP)**
   - AuditPreparationDashboard.tsx
   - Cards de estado
   - Acciones rápidas
   - Paquetes recientes

2. **Generador de Paquetes (13 SP)**
   - GenerateAuditPackagePage.tsx
   - Selector de período
   - Selector de categorías
   - Progress bar durante generación
   - Resultados con descarga

**Entregables:**

- ✅ 2 páginas principales
- ✅ 6 componentes reutilizables
- ✅ 1 service TypeScript
- ✅ Rutas en App.tsx
- ✅ Links en sidebar

### Fase 3: Compliance Score & Checklist (39 SP, 2-3 semanas)

**Prioridad:** 🟠 P1 - ALTA

**Tareas:**

1. **Compliance Score Page (13 SP)**
   - ComplianceScorePage.tsx
   - Gráfico circular de score
   - Estado de formatos DGII
   - Estado de documentación
   - Estado de NCF
   - Alertas con prioridades

2. **Checklist Page (13 SP)**
   - DocumentChecklistPage.tsx
   - 25 checkpoints por mes
   - Progress bar
   - Secciones por categoría
   - Toggle de items

3. **Response Templates (13 SP)**
   - ResponseLetterPage.tsx
   - Generador de cartas
   - Templates configurables
   - Export a PDF

**Entregables:**

- ✅ 3 páginas completas
- ✅ 8 componentes
- ✅ PDF generator (jsPDF/pdfmake)
- ✅ Documentación de uso

### Fase 4: Integration & Testing (21 SP, 1-2 semanas)

**Prioridad:** 🟡 P2 - MEDIA

**Tareas:**

1. **Testing Completo**
   - Unit tests backend (15 tests mínimo)
   - Integration tests (generación de paquetes)
   - E2E tests (flujo completo)
   - Load testing (paquetes grandes)

2. **Deployment**
   - Docker images
   - Kubernetes manifests
   - CI/CD pipeline
   - Documentación deployment

3. **Capacitación**
   - Manual de usuario
   - Video tutorial (5-10 min)
   - Checklist de uso mensual

**Entregables:**

- ✅ 15 tests unitarios mínimo
- ✅ 5 tests de integración
- ✅ 3 tests E2E
- ✅ Manual de usuario (10 páginas)
- ✅ Video tutorial

---

## 📊 RESUMEN DE INVERSIÓN

### Distribución de Story Points

| Fase                           | Backend   | Frontend  | Testing  | Total      | Costo       |
| ------------------------------ | --------- | --------- | -------- | ---------- | ----------- |
| **Fase 1: Backend Core**       | 34 SP     | 0 SP      | 0 SP     | 34 SP      | $4,760      |
| **Fase 2: Frontend Dashboard** | 0 SP      | 21 SP     | 0 SP     | 21 SP      | $2,940      |
| **Fase 3: Score & Checklist**  | 21 SP     | 26 SP     | 0 SP     | 47 SP      | $6,580      |
| **Fase 4: Testing & Deploy**   | 5 SP      | 5 SP      | 3 SP     | 13 SP      | $1,820      |
| **TOTAL**                      | **60 SP** | **52 SP** | **3 SP** | **115 SP** | **$16,100** |

### Cronograma

```
Semana 1-2: Fase 1 - Backend Core (34 SP)
   ├── AuditPackageService
   ├── ComplianceReportService
   └── Database tables

Semana 3-4: Fase 2 - Frontend Dashboard (21 SP)
   ├── Dashboard principal
   └── Generador de paquetes

Semana 5-7: Fase 3 - Score & Checklist (47 SP)
   ├── Compliance Score
   ├── Checklist mensual
   └── Response templates

Semana 8: Fase 4 - Testing & Deploy (13 SP)
   ├── Unit tests
   ├── Integration tests
   └── Deployment

TOTAL: 8 semanas (~2 meses)
```

### Equipo Requerido

- 1 Backend Developer (.NET/C#)
- 1 Frontend Developer (React/TypeScript)
- 1 QA Tester (part-time, semana 8)
- 1 DevOps Engineer (part-time, semana 8)

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Backend

- [ ] **Microservicio DGIIService (si separado)**
  - [ ] Estructura Clean Architecture
  - [ ] 3 entidades: AuditPackage, ComplianceReport, MonthlyChecklist
  - [ ] 3 repositories
  - [ ] DbContext con EF Core

- [ ] **AuditPackageService**
  - [ ] GenerateAsync() - Generación de paquetes
  - [ ] Descarga de archivos de S3
  - [ ] Organización en carpetas
  - [ ] Generación de índice Excel
  - [ ] Creación de ZIP
  - [ ] Upload a S3
  - [ ] Registro en BD

- [ ] **ComplianceReportService**
  - [ ] GenerateMonthlyReportAsync()
  - [ ] CalculateScore() - Lógica 0-100
  - [ ] GenerateAlerts() - Alertas
  - [ ] Verificación formatos DGII
  - [ ] Verificación documentación
  - [ ] Verificación NCF

- [ ] **ChecklistService**
  - [ ] GenerateChecklistAsync() - 25 checkpoints
  - [ ] CalculateCompletion()
  - [ ] GetPendingItems()
  - [ ] UpdateItemAsync()

- [ ] **ResponseTemplateService**
  - [ ] GenerateResponseLetterAsync()
  - [ ] Templates configurables
  - [ ] Export a PDF

- [ ] **Controllers**
  - [ ] AuditPackageController (3 endpoints)
  - [ ] ComplianceReportController (2 endpoints)
  - [ ] ChecklistController (2 endpoints)
  - [ ] ResponseTemplateController (1 endpoint)

- [ ] **Base de Datos**
  - [ ] audit_packages table
  - [ ] compliance_reports table
  - [ ] monthly_checklists table
  - [ ] dgii_responses table
  - [ ] Migrations

- [ ] **Tests**
  - [ ] 15 unit tests mínimo
  - [ ] 5 integration tests
  - [ ] Mocking de S3
  - [ ] Test data seeds

### Frontend

- [ ] **Servicios TypeScript**
  - [ ] auditPreparationService.ts
  - [ ] generatePackage()
  - [ ] getComplianceReport()
  - [ ] downloadPackage()
  - [ ] getPackages()
  - [ ] getChecklist()
  - [ ] updateChecklistItem()

- [ ] **Páginas**
  - [ ] AuditPreparationDashboard.tsx (21 SP)
  - [ ] GenerateAuditPackagePage.tsx (13 SP)
  - [ ] ComplianceScorePage.tsx (13 SP)
  - [ ] DocumentChecklistPage.tsx (13 SP)
  - [ ] AuditPackagesHistoryPage.tsx (5 SP)
  - [ ] ResponseLetterPage.tsx (5 SP)

- [ ] **Componentes**
  - [ ] ComplianceScoreCircle (8 SP)
  - [ ] AuditCategorySelector (5 SP)
  - [ ] DocumentCountCard (3 SP)
  - [ ] GeneratePackageButton (5 SP)
  - [ ] AlertsList (5 SP)
  - [ ] PackageDownloadButton (3 SP)
  - [ ] ChecklistSection (3 SP)
  - [ ] ProgressBar (2 SP)

- [ ] **Rutas**
  - [ ] /admin/audit/preparation
  - [ ] /admin/audit/generate-package
  - [ ] /admin/audit/compliance-score
  - [ ] /admin/audit/checklist
  - [ ] /admin/audit/packages
  - [ ] /admin/audit/response-letter

- [ ] **Sidebar**
  - [ ] Link "Preparación Auditoría" en Admin menu

- [ ] **Tests**
  - [ ] Component tests (Jest/Vitest)
  - [ ] Integration tests (Playwright)
  - [ ] E2E tests (Cypress)

### DevOps

- [ ] **Docker**
  - [ ] Dockerfile para DGIIService (si nuevo)
  - [ ] docker-compose.yml actualizado

- [ ] **Kubernetes**
  - [ ] Deployment manifest
  - [ ] Service manifest
  - [ ] ConfigMap con S3 config
  - [ ] Secrets para AWS credentials

- [ ] **CI/CD**
  - [ ] GitHub Actions workflow
  - [ ] Build + Test + Deploy pipeline
  - [ ] Automated tests en CI

- [ ] **Documentación**
  - [ ] README del servicio
  - [ ] API documentation (Swagger)
  - [ ] Manual de usuario
  - [ ] Video tutorial

---

## 📈 MÉTRICAS DE ÉXITO

### KPIs a Monitorear

**Tiempo de Respuesta:**

- ⏱️ **Meta:** Generar paquete en < 20 minutos
- ⏱️ **Meta:** Responder a auditoría en < 24 horas (vs 7 días manual)

**Calidad:**

- ✅ **Meta:** 100% de documentos incluidos (0 olvidados)
- ✅ **Meta:** Compliance score > 80 promedio mensual

**Uso:**

- 📊 **Meta:** Generar checklist mensual (12/año)
- 📊 **Meta:** Generar paquete anual pre-emptivo (1/año mínimo)
- 📊 **Meta:** Monitorear score mensualmente

**Financiero:**

- 💰 **Meta:** $0 en multas por respuestas tardías
- 💰 **Meta:** Ahorro de $2,100 por auditoría
- 💰 **Meta:** ROI en 3-6 años

---

## 🎯 PRIORIZACIÓN FINAL

### Orden de Implementación Recomendado

**Ahora (Enero-Febrero 2026):**

1. ✅ Sistema de Registro de Gastos (105 SP) - **BLOCKER**
2. ✅ Generadores Formato 606/607/608 (94 SP) - **BLOCKER**
3. ✅ Automatización Reportes DGII (94 SP) - **CRÍTICO**

**Después (Marzo-Abril 2026):** 4. 🔴 **Preparación para Auditoría (115 SP)** - **CRÍTICO** ⭐ ESTE 5. Pro Consumidor Sistema de Quejas (66 SP) - ALTA

**Razón de prioridad #4:**

- **Depende** de sistemas anteriores (Gastos, Formatos, Automatización)
- **Complementa** la infraestructura fiscal completa
- **Previene** multas y problemas con DGII
- **Ahorra** 42 horas por auditoría
- **Garantiza** respuesta profesional y completa

**Sin este sistema:**

- ❌ Documentos dispersos
- ❌ 7 días para responder (vs 1 día)
- ❌ Riesgo de perder plazo → Multas $170-$850
- ❌ Mala impresión a DGII

**Con este sistema:**

- ✅ 1 click → Paquete completo
- ✅ 1 día para responder
- ✅ 0 riesgo de perder plazo
- ✅ Profesionalismo garantizado

---

## 📋 DEPENDENCIAS CRÍTICAS

Este sistema **DEPENDE** de:

1. ✅ **MediaService con S3** (95% OK)
   - Archivos ya en S3
   - Solo necesita organización para auditoría

2. ❌ **Sistema de Gastos Operativos** (0% - BLOCKER)
   - Sin esto: NO hay facturas recibidas organizadas
   - Implementar PRIMERO (105 SP)

3. ❌ **Generadores Formato 606/607** (0% - BLOCKER)
   - Sin esto: NO hay formatos DGII archivados
   - Implementar SEGUNDO (94 SP)

4. ❌ **Automatización DGII** (0% - CRÍTICO)
   - Sin esto: NO hay recordatorios ni jobs
   - Implementar TERCERO (94 SP)

**Secuencia recomendada:**

```
1. Gastos Operativos (105 SP)
   ↓
2. Formato 606/607/608 (94 SP)
   ↓
3. Automatización Jobs (94 SP)
   ↓
4. Preparación Auditoría (115 SP) ⭐ AHORA ESTAMOS AQUÍ
```

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Preparación Auditoría DGII", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de preparación de auditoría", async ({
    page,
  }) => {
    await page.goto("/admin/dgii/audit-preparation");
    await expect(page.getByTestId("audit-preparation-dashboard")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /preparación auditoría/i }),
    ).toBeVisible();
    await expect(page.getByTestId("compliance-score")).toBeVisible();
  });

  test("debe listar documentos requeridos con status", async ({ page }) => {
    await page.goto("/admin/dgii/audit-preparation");
    await expect(page.getByTestId("required-documents-list")).toBeVisible();
    await expect(page.getByTestId("document-status-rnc")).toBeVisible();
    await expect(page.getByTestId("document-status-ncf")).toBeVisible();
  });

  test("debe generar paquete de auditoría completo", async ({ page }) => {
    await page.goto("/admin/dgii/audit-preparation");
    await page.getByRole("button", { name: /generar paquete/i }).click();
    await expect(page.getByTestId("package-generation-modal")).toBeVisible();
    await page.getByTestId("date-range-start").fill("2025-01-01");
    await page.getByTestId("date-range-end").fill("2025-12-31");
    await page.getByRole("button", { name: /confirmar/i }).click();
    await expect(page.getByText(/paquete generándose/i)).toBeVisible();
  });

  test("debe mostrar checklist de preparación pre-auditoría", async ({
    page,
  }) => {
    await page.goto("/admin/dgii/audit-preparation/checklist");
    await expect(page.getByTestId("pre-audit-checklist")).toBeVisible();
    await expect(page.getByTestId("checklist-item").first()).toBeVisible();
    await page.getByTestId("checklist-item").first().click();
    await expect(page.getByTestId("checklist-item").first()).toHaveAttribute(
      "data-completed",
      "true",
    );
  });

  test("debe descargar paquete de auditoría ZIP", async ({ page }) => {
    await page.goto("/admin/dgii/audit-preparation/packages");
    const downloadPromise = page.waitForEvent("download");
    await page.getByTestId("download-package-button").first().click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/audit-package.*\.zip$/i);
  });

  test("debe mostrar timeline de próximos vencimientos fiscales", async ({
    page,
  }) => {
    await page.goto("/admin/dgii/audit-preparation");
    await expect(page.getByTestId("fiscal-deadlines-timeline")).toBeVisible();
    await expect(page.getByTestId("deadline-item").first()).toBeVisible();
  });
});
```

---

**✅ AUDITORÍA #10 COMPLETADA**

_Sistema de Preparación para Auditoría DGII documentado completamente con roadmap de implementación, análisis financiero, y priorización._

---

**Próxima Auditoría:** Sistema de e-CF (Comprobantes Fiscales Electrónicos) - 155 SP

**Documento:** Enero 29, 2026  
**Responsable:** Gregory Moreno  
**Revisor:** Nicauris Mateo Alcántara
