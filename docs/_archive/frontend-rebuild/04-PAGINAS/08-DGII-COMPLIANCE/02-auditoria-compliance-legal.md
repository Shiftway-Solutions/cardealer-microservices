---
title: "43. Auditoría Compliance & Legal - Frontend"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["BillingService", "NotificationService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 🔍 43. Auditoría Compliance & Legal - Frontend

> **Fecha:** Enero 29, 2026  
> **Alcance:** Validación de implementación de procesos legales (Ley 155-17, Ley 172-13, Ley 11-92) + **Compliance Reports**  
> **Referencias:** `process-matrix/08-COMPLIANCE-LEGAL-RD/`  
> **Última Actualización:** Enero 29, 2026 - Auditoría Reportes de Compliance

---

## 📊 RESUMEN EJECUTIVO

| Marco Legal                           | Backend | Frontend UI | Gap      | Estado                        |
| ------------------------------------- | ------- | ----------- | -------- | ----------------------------- |
| **Ley 155-17 (AML/PLD)**              | ✅ 80%  | 🟡 40%      | **-40%** | 🟡 PARCIAL (44 SP)            |
| **Ley 172-13 (Privacidad)**           | ✅ 90%  | ✅ 95%      | **+5%**  | ✅ COMPLETO (4 SP)            |
| **Ley 11-92 (DGII Tax)**              | 🟡 60%  | 🔴 0%       | **-60%** | 🔴 CRÍTICO (21 SP)            |
| **Ley 11-92 (DGII Formatos)** 🆕      | 🟡 50%  | 🔴 **4%**   | **-46%** | 🔴 **CRÍTICO (94 SP)**        |
| **Ley 11-92 (Libros Contables)** 🆕   | 🔴 0%   | 🔴 0%       | **0%**   | 🔴 **MÁXIMO RIESGO (283 SP)** |
| **Norma 06-2018 (e-CF Electrónicos)** | 🔴 0%   | 🔴 0%       | **0%**   | 🔴 **MÁXIMO RIESGO (155 SP)** |
| **Ley 126-02 (Comercio Elec.)** 🆕    | ✅ 70%  | ✅ 80%      | **+10%** | ✅ BUENO (37 SP)              |
| **Ley 358-05 (Pro Consumidor)** 🆕    | 🟡 40%  | 🔴 35%      | **-5%**  | 🔴 CRÍTICO (66 SP)            |
| **Registro Gastos Operativos** 🆕     | 🟡 30%  | 🔴 0%       | **-30%** | 🔴 **BLOCKER (105 SP)**       |
| **Automatización Reportes DGII** 🆕   | 🟡 15%  | 🔴 5%       | **-10%** | 🔴 **CRÍTICO (94 SP)**        |
| **Preparación Auditoría DGII** 🆕     | 🟡 12%  | 🔴 0%       | **-12%** | 🔴 **BLOCKER (115 SP)**       |
| **ComplianceService (Reportes)** 🆕   | 🟡 40%  | 🔴 0%       | **-40%** | 🔴 CRÍTICO                    |
| **Ley 63-17 (INTRANT Vehicular)**     | 🟡 50%  | 🔴 0%       | **-50%** | 🔴 CRÍTICO (60 SP)            |
| **Sistema Auditoría (Folder 25)** 🆕  | 🟡 75%  | 🔴 **0%**   | **-75%** | 🔴 **CRÍTICO (309 SP)**       |

**🚨 ALERTAS CRÍTICAS:**

- **Sistema Auditoría (Folder 25):** **0% frontend** - FiscalReportingService NO EXISTE + 12 páginas UI faltantes = **CRÍTICO: Sin dashboard compliance (309 SP)**
- **Ley 11-92 (Libros Contables):** **0% cobertura** - Sistema COMPLETO no existe = **MÁXIMO RIESGO: Base para TODO compliance DGII** (283 SP)
- **Norma 06-2018 (e-CF Electrónicos):** **0% cobertura** - Sistema COMPLETO no existe = **MÁXIMO RIESGO LEGAL: $540K/año** (155 SP)
- **Registro de Gastos Operativos:** **5% cobertura** - Sistema NO EXISTE = **BLOCKER FORMATO 606 (105 SP)**
- **Automatización Reportes DGII:** **8% cobertura** - Jobs automáticos NO EXISTEN + Dashboard faltante = **BLOCKER CRÍTICO (94 SP)**
- **Preparación Auditoría DGII:** **6% cobertura** - Sistema paquetes auditoría NO EXISTE = **BLOCKER CRÍTICO: 1 click → ZIP completo en <24h (115 SP)**
- **Ley 11-92 (DGII Formatos):** **4% cobertura** - Gestión NCF + Formatos 606/607/608 faltantes = **BLOCKER LEGAL (94 SP)**
- **Ley 358-05 (Pro Consumidor):** **35% cobertura** - Sistema de quejas faltante = **COMPLIANCE BLOCKER (66 SP)**
- **ComplianceService Reportes:** **0% UI** - Generadores de reportes automáticos NO EXISTEN en frontend
- **HALLAZGO UAF:** OKLA probablemente NO es Sujeto Obligado UAF (plataforma de clasificados, no dealer)

**✅ CUMPLIMIENTO EXCELENTE:**

- Ley 172-13 (Privacidad): **95% cobertura** - Implementación completa ARCO (4 SP)
- Ley 126-02 (Comercio Electrónico): **80% cobertura** - Requisitos básicos completos (37 SP)

---

## 📊 TOTAL STORY POINTS

**Story Points Totales:** **1,411 SP** (44 + 4 + 21 + 94 + **283** + **155** + 37 + 66 + 105 + **94** + **115** + 60 + **309** + otros)

**Bloqueadores Máximos (8):** **1,221 SP**

1. **Sistema Auditoría (Folder 25):** 309 SP (P0 - FiscalReportingService + Frontend Dashboard completo)
2. **Libros Contables (Accounting Books):** 283 SP (P0 - Sistema BASE para TODO compliance DGII)
3. **e-CF Comprobantes Fiscales Electrónicos:** 155 SP (P0 - Riesgo $540K/año + cierre de negocio)
4. **Preparación Auditoría DGII:** 115 SP (P0 - Responder a DGII en <24h vs 7 días manual)
5. **Registro de Gastos Operativos:** 105 SP (P0 - Sin esto, no hay Formato 606)
6. **DGII Formatos (606/607/608):** 94 SP (P0 - Obligación legal mensual)
7. **Automatización Reportes DGII:** 94 SP (P0 - Jobs automáticos + dashboard faltantes)
8. **Ley 358-05 (Pro Consumidor):** 66 SP (P1 - Sistema de quejas completo)

**Inversión Total Estimada (Bloqueadores):** $170,940 USD (1,221 SP × $140/SP)  
**Ahorro en Multas Anuales:** RD$3.1M-$5.3M ($52,000-$88,000 USD) + Multas auditoría ($170-$850)  
**ROI:** 8-16 meses

**⚠️ e-CF es el MAYOR RIESGO:** Si OKLA supera RD$50M sin e-CF, TODAS las facturas son inválidas y DGII puede cerrar el negocio.

---

## 🆕 AUDITORÍA: SISTEMA DE REPORTES DE COMPLIANCE

> **Referencia:** `process-matrix/08-COMPLIANCE-LEGAL-RD/05-compliance-reports.md`  
> **ComplianceService:** Puerto 5027  
> **Estado Backend:** 🟡 40% Implementado

### 📋 Resumen de Reportes Obligatorios

| Reporte                 | Destino        | Frecuencia | Backend | Frontend | Gap       | Prioridad  |
| ----------------------- | -------------- | ---------- | ------- | -------- | --------- | ---------- |
| **607 DGII**            | DGII           | Mensual    | 🟡 60%  | 🔴 0%    | **-60%**  | 🔴 CRÍTICA |
| **606 DGII**            | DGII           | Mensual    | 🟡 60%  | 🔴 0%    | **-60%**  | 🔴 CRÍTICA |
| **NCF Summary**         | DGII           | Mensual    | 🟡 50%  | 🔴 0%    | **-50%**  | 🔴 ALTA    |
| **AML Report**          | SB             | Trimestral | ✅ 80%  | 🔴 0%    | **-80%**  | 🔴 CRÍTICA |
| **Consumer Complaints** | Pro Consumidor | Mensual    | 🔴 20%  | 🔴 0%    | **-20%**  | 🔴 ALTA    |
| **Data Privacy**        | INDOTEL        | Anual      | ✅ 90%  | 🔴 0%    | **-90%**  | 🟡 MEDIA   |
| **Audit Trail**         | Interno        | On-demand  | ✅ 100% | 🔴 0%    | **-100%** | 🟡 MEDIA   |
| **Transaction Report**  | Interno        | Diario     | 🟡 40%  | 🔴 0%    | **-40%**  | 🟡 MEDIA   |

### 🔴 PÁGINAS CRÍTICAS FALTANTES (Sistema de Reportes)

| Ruta Propuesta                        | Funcionalidad             | Backend | UI    | Prioridad  |
| ------------------------------------- | ------------------------- | ------- | ----- | ---------- |
| `/admin/compliance/reports`           | Dashboard de reportes     | 🟡 40%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/reports/607`       | Generador 607 DGII        | 🟡 60%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/reports/606`       | Generador 606 DGII        | 🟡 60%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/reports/ncf`       | Resumen NCF               | 🟡 50%  | 🔴 0% | 🔴 ALTA    |
| `/admin/compliance/reports/aml`       | Reporte AML trimestral    | ✅ 80%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/reports/schedule`  | Calendario programaciones | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| `/admin/compliance/reports/history`   | Historial reportes        | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| `/admin/compliance/reports/generator` | Generador universal       | 🔴 0%   | 🔴 0% | 🟡 MEDIA   |

### ❌ BACKEND ENDPOINTS SIN UI (ComplianceService)

**Estado Actual:** 0 de 12 endpoints tienen interfaz de usuario

```typescript
// ✅ BACKEND IMPLEMENTADO, 🔴 SIN UI

// Generación de Reportes
POST / api / compliance / reports / generate; // 🔴 No usado
GET / api / compliance / reports; // 🔴 No usado
GET / api / compliance / reports / { id }; // 🔴 No usado
GET / api / compliance / reports / { id } / download; // 🔴 No usado

// Reportes Específicos
POST / api / compliance / reports / 606; // 🔴 No usado
POST / api / compliance / reports / 607; // 🔴 No usado
POST / api / compliance / reports / ncf - summary; // 🔴 No usado
POST / api / compliance / reports / aml; // 🔴 No usado
POST / api / compliance / reports / audit - trail; // 🔴 No usado

// Programación
GET / api / compliance / schedules; // 🔴 No usado
POST / api / compliance / schedules; // 🔴 No usado
DELETE / api / compliance / schedules / { id }; // 🔴 No usado
```

**Cobertura:** 0 de 12 endpoints (0%) = **CRÍTICO**

### 📦 Entidades Backend Disponibles (Sin UI)

```typescript
// ComplianceReport
{
  id: UUID,
  type: ReportType, // DGII_606, DGII_607, AML_Report, etc.
  name: string,
  periodStart: DateTime,
  periodEnd: DateTime,
  status: ReportStatus, // Pending, Generating, Completed, Failed
  fileUrl: string,
  s3Key: string,
  fileFormat: string, // PDF, Excel, XML, TXT
  fileSizeBytes: long,
  totalRecords: int,
  totalAmount: decimal,
  summary: Dictionary<string, object>,
  generatedById: UUID,
  createdAt: DateTime,
  completedAt: DateTime,
  generationDuration: TimeSpan
}

// ReportSchedule
{
  id: UUID,
  reportType: ReportType,
  frequency: ScheduleFrequency, // Daily, Monthly, Quarterly, Yearly
  dayOfMonth: int, // 1-28 para mensual
  timeOfDay: TimeSpan,
  notifyEmails: List<string>,
  isActive: bool,
  nextRunAt: DateTime,
  lastRunAt: DateTime
}
```

### 🚨 Impacto Legal de Faltantes

#### Reportes DGII (607/606) - 🔴 CRÍTICO

**Plazo:** Día 10 de cada mes  
**Multa por Incumplimiento:** RD$3,000 - RD$15,000 **por mes**  
**Riesgo:** Acumulación de multas + recargos + auditoría DGII

**Sin UI, el administrador NO PUEDE:**

- Generar archivo 607.txt (ventas del mes)
- Generar archivo 606.txt (compras del mes)
- Validar formato antes de enviar a DGII
- Ver historial de reportes enviados
- Programar generación automática día 8 de cada mes

#### Reporte AML - 🔴 CRÍTICO

**Plazo:** Trimestral (día 10 del mes siguiente al trimestre)  
**Destino:** Superintendencia de Bancos (SB)  
**Multa:** RD$100,000 - RD$1,000,000 + suspensión operaciones

**Sin UI, el Compliance Officer NO PUEDE:**

- Revisar transacciones > $10,000 USD
- Identificar PICs (Personas Políticamente Expuestas)
- Agregar por cliente con risk scoring
- Generar reporte formato SB
- Enviar a UAF (Unidad de Análisis Financiero)

#### Consumer Complaints - 🔴 ALTA

**Plazo:** Mensual  
**Destino:** Pro Consumidor  
**Multa:** Según Ley 358-05

**Falta Completamente:**

- Sistema de recepción de quejas
- Clasificación por tipo (ItemNotReceived, NotAsDescribed, etc.)
- Seguimiento de resolución
- Generador de reporte mensual

### 📅 Calendario de Obligaciones Regulatorias

```yaml
# Reportes Automáticos Requeridos
schedules:
  - type: DGII_607
    frequency: Monthly
    dayOfMonth: 10
    description: "Ventas del mes anterior"
    penalty: "RD$3,000 - RD$15,000"

  - type: DGII_606
    frequency: Monthly
    dayOfMonth: 10
    description: "Compras del mes anterior"
    penalty: "RD$3,000 - RD$15,000"

  - type: AML_Report
    frequency: Quarterly
    dayOfMonth: 10
    description: "Transacciones > $10K USD"
    penalty: "RD$100K - RD$1M + suspensión"

  - type: NCF_Summary
    frequency: Monthly
    dayOfMonth: 15
    description: "Resumen NCF emitidos"
    penalty: "RD$1,000 - RD$5,000"

  - type: DataPrivacy
    frequency: Yearly
    month: March
    dayOfMonth: 31
    description: "Cumplimiento Ley 172-13"
    penalty: "RD$500,000"

  - type: TransactionDaily
    frequency: Daily
    timeOfDay: "06:00"
    description: "Transacciones del día anterior"
    internal: true
```

### 🛠️ Plan de Implementación (42 SP)

#### Sprint 1 - Dashboard y Generadores DGII (2 semanas)

**Día 1-3: ReportsHubPage** (8 SP)

```
Ruta: /admin/compliance/reports
Features:
- Dashboard principal con métricas
- Próximos vencimientos (calendario)
- Reportes generados (últimos 10)
- Quick actions: Generar 607, 606, AML
- Stats: Total reportes generados, tamaño archivos, errores
- Timeline de actividad
```

**Día 4-6: DGII607ReportPage** (8 SP)

```
Ruta: /admin/compliance/reports/607
Features:
- Selector de período (mes/año)
- Preview de transacciones incluidas
- Validación formato DGII
- Generación archivo 607.txt
- Download automático
- Historial de reportes 607
- Notificación a emails configurados
```

**Día 7-9: DGII606ReportPage** (8 SP)

```
Ruta: /admin/compliance/reports/606
Features:
- Selector de período
- Preview de compras
- Validación proveedores con RNC
- Generación archivo 606.txt
- Download
- Historial
```

**Día 10: Testing & QA** (2 SP)

#### Sprint 2 - AML y Programación (1.5 semanas)

**Día 11-13: AMLReportPage** (8 SP)

```
Ruta: /admin/compliance/reports/aml
Features:
- Selector trimestre
- Transacciones > $10,000 USD
- Risk scoring por cliente
- Identificación PICs
- Generación reporte formato SB
- Download PDF/Excel
```

**Día 14-15: ReportSchedulerPage** (5 SP)

```
Ruta: /admin/compliance/reports/schedule
Features:
- Lista de reportes programados
- Crear nueva programación
- Editar frecuencia, día, hora
- Configurar emails de notificación
- Activar/desactivar schedules
- Próxima ejecución (countdown)
```

**Día 16: Testing & Deploy** (3 SP)

### 📝 Checklist de Tareas Pendientes

#### Páginas Faltantes

- [ ] `src/pages/admin/compliance/ReportsHubPage.tsx` ⚠️ CRÍTICO
- [ ] `src/pages/admin/compliance/DGII607ReportPage.tsx` ⚠️ CRÍTICO
- [ ] `src/pages/admin/compliance/DGII606ReportPage.tsx` ⚠️ CRÍTICO
- [ ] `src/pages/admin/compliance/AMLReportPage.tsx` ⚠️ CRÍTICO
- [ ] `src/pages/admin/compliance/ReportSchedulerPage.tsx`
- [ ] `src/pages/admin/compliance/ReportHistoryPage.tsx`
- [ ] `src/pages/admin/compliance/NCFSummaryReportPage.tsx`

#### Componentes Reutilizables

- [ ] `src/components/admin/compliance/ReportCard.tsx`
- [ ] `src/components/admin/compliance/ReportStatusBadge.tsx`
- [ ] `src/components/admin/compliance/PeriodSelector.tsx`
- [ ] `src/components/admin/compliance/TransactionPreview.tsx`
- [ ] `src/components/admin/compliance/DGII607Generator.tsx`
- [ ] `src/components/admin/compliance/DGII606Generator.tsx`
- [ ] `src/components/admin/compliance/RegulatoryCalendar.tsx`
- [ ] `src/components/admin/compliance/DeadlineAlert.tsx`

#### Servicios TypeScript

- [ ] `src/services/complianceReportsService.ts`:
  - [ ] `generateReport(type, period)` - Generar reporte
  - [ ] `getReports(filters)` - Listar reportes
  - [ ] `getReportById(id)` - Detalle
  - [ ] `downloadReport(id)` - Descargar archivo
  - [ ] `getSchedules()` - Listar programados
  - [ ] `createSchedule(schedule)` - Crear programación
  - [ ] `deleteSchedule(id)` - Eliminar programación
  - [ ] `getUpcomingDeadlines()` - Próximos vencimientos
  - [ ] `generate607Report(month, year)` - Específico 607
  - [ ] `generate606Report(month, year)` - Específico 606
  - [ ] `generateAMLReport(quarter, year)` - Específico AML

#### Hooks Faltantes

- [ ] `src/lib/hooks/useGenerateReport.ts`
- [ ] `src/lib/hooks/useReportsList.ts`
- [ ] `src/lib/hooks/useReportSchedules.ts`
- [ ] `src/lib/hooks/useUpcomingDeadlines.ts`

#### Rutas en App.tsx

- [ ] `/admin/compliance/reports` → `ReportsHubPage`
- [ ] `/admin/compliance/reports/607` → `DGII607ReportPage`
- [ ] `/admin/compliance/reports/606` → `DGII606ReportPage`
- [ ] `/admin/compliance/reports/aml` → `AMLReportPage`
- [ ] `/admin/compliance/reports/schedule` → `ReportSchedulerPage`
- [ ] `/admin/compliance/reports/history` → `ReportHistoryPage`

#### Configuración AdminSidebar.tsx

```tsx
{
  id: 'compliance-reports',
  label: 'Reportes de Compliance',
  icon: FileText,
  allowedRoles: [PlatformRole.SUPER_ADMIN, PlatformRole.COMPLIANCE_OFFICER],
  children: [
    {
      id: 'reports-hub',
      label: 'Dashboard',
      path: '/admin/compliance/reports',
    },
    {
      id: 'reports-607',
      label: 'Reporte 607 (DGII)',
      path: '/admin/compliance/reports/607',
    },
    {
      id: 'reports-606',
      label: 'Reporte 606 (DGII)',
      path: '/admin/compliance/reports/606',
    },
    {
      id: 'reports-aml',
      label: 'Reporte AML',
      path: '/admin/compliance/reports/aml',
    },
    {
      id: 'reports-schedule',
      label: 'Programación',
      path: '/admin/compliance/reports/schedule',
    },
    {
      id: 'reports-history',
      label: 'Historial',
      path: '/admin/compliance/reports/history',
    },
  ],
}
```

### 🎯 Ejemplo de Interfaz: DGII607ReportPage

```tsx
// DGII607ReportPage.tsx - FALTA CREAR
export default function DGII607ReportPage() {
  const [period, setPeriod] = useState({ month: 1, year: 2026 });
  const [preview, setPreview] = useState<Invoice[]>([]);

  const { mutate: generateReport, isLoading } = useGenerate607Report();
  const { data: history } = useReportHistory("DGII_607");

  const handleGenerate = () => {
    generateReport(
      {
        month: period.month,
        year: period.year,
      },
      {
        onSuccess: (data) => {
          // Auto-download archivo .txt
          downloadFile(data.fileUrl, `607${period.month}${period.year}.txt`);

          // Notificar
          toast.success("Reporte 607 generado correctamente");
        },
        onError: () => {
          toast.error("Error generando reporte 607");
        },
      },
    );
  };

  return (
    <div className="max-w-7xl mx-auto py-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold mb-2">
          Reporte 607 DGII - Ingresos/Ventas
        </h1>
        <p className="text-gray-600">
          Formato obligatorio mensual -{" "}
          <strong>Plazo: día 10 de cada mes</strong>
        </p>
        <DeadlineAlert deadline={getNextDeadline()} type="607" />
      </div>

      {/* Selector de período */}
      <div className="bg-white p-6 rounded-lg shadow mb-6">
        <h2 className="font-semibold mb-4">Período a reportar</h2>
        <PeriodSelector
          value={period}
          onChange={setPeriod}
          onLoadPreview={(p) => loadPreview(p)}
        />
      </div>

      {/* Preview de transacciones */}
      {preview.length > 0 && (
        <TransactionPreview
          transactions={preview}
          type="607"
          onValidate={() => {
            /* validar formato */
          }}
        />
      )}

      {/* Botón generar */}
      <button
        onClick={handleGenerate}
        disabled={isLoading || preview.length === 0}
        className="btn-primary flex items-center gap-2"
      >
        {isLoading ? (
          <>
            <FiLoader className="animate-spin" />
            Generando archivo...
          </>
        ) : (
          <>
            <FiDownload />
            Generar Archivo 607.txt
          </>
        )}
      </button>

      {/* Historial */}
      <div className="mt-8">
        <h3 className="font-semibold mb-4">Reportes 607 Generados</h3>
        <ReportHistory reports={history} type="607" />
      </div>
    </div>
  );
}
```

### 📊 Métricas de Éxito

**KPIs a Monitorear:**

1. **Generación de Reportes:**
   - Reportes generados por mes
   - Tiempo promedio de generación
   - Tasa de error en generación

2. **Cumplimiento:**
   - % de reportes enviados a tiempo (meta: 100%)
   - Días de adelanto promedio vs deadline
   - Reportes pendientes por enviar

3. **Automatización:**
   - % de reportes generados automáticamente (meta: 80%)
   - Schedules activos
   - Notificaciones enviadas

4. **Calidad:**
   - % de reportes sin errores de validación (meta: 100%)
   - Reportes rechazados por DGII/SB (meta: 0%)

---

### ✅ Implementado (40%)

| Proceso                          | UI  | Archivos                 | Estado |
| -------------------------------- | --- | ------------------------ | ------ |
| **AML-KYC-001** Verificación KYC | ✅  | `27-kyc-verificacion.md` | ✅ 95% |
| **WL-001** Watchlist/PEPs        | ✅  | WatchlistAdminPage       | ✅ 95% |
| **AML-ROS-001** Reportes UAF     | ✅  | STRReportsPage           | ✅ 90% |

### 🔴 Faltante (60%)

| Proceso                                   | Brecha                               | Prioridad  |
| ----------------------------------------- | ------------------------------------ | ---------- |
| **AML-DDC-001** Due Diligence Reforzada   | Sin wizard DDC, sin origen de fondos | 🔴 ALTA    |
| **AML-UMBRAL-001** Monitoreo > $500K      | Sin dashboard transacciones          | 🔴 CRÍTICA |
| **COMP-001** Dashboard Compliance Officer | ADM-COMP sin homepage                | 🔴 CRÍTICA |

### 📋 Plan de Acción (26 SP)

**Sprint Inmediato:**

1. **ComplianceDashboardPage** (8 SP)
   - Ruta: `/admin/compliance/dashboard`
   - Métricas: STRs, alertas, vencimientos, capacitación
   - Quick actions

2. **TransactionMonitoringPage** (8 SP)
   - Ruta: `/admin/compliance/transactions`
   - Tabla transacciones > $100K
   - Risk score y alertas

3. **AlertsDashboardPage** (5 SP)
   - Ruta: `/admin/compliance/alerts`
   - Tipos: SINGLE_LARGE, MULTIPLE_24H, STRUCTURING
   - Priorización

4. **DueDiligencePage** (5 SP)
   - Ruta: `/admin/compliance/ddc/{userId}`
   - Wizard: Simplificada → Normal → Reforzada
   - Origen de fondos

---

## 2. LEY 172-13 (Protección de Datos)

### ✅ COMPLETO (95%) - EXCELENTE

| Derecho ARCO      | UI                | Estado  |
| ----------------- | ----------------- | ------- |
| **Acceso**        | MyDataPage        | ✅ 100% |
| **Rectificación** | Settings          | ✅ 100% |
| **Cancelación**   | DeleteAccountPage | ✅ 95%  |
| **Oposición**     | PrivacyCenterPage | ✅ 100% |

**Páginas Implementadas:**

- ✅ `/privacy-center` - Dashboard ARCO
- ✅ `/my-data` - Ver datos personales
- ✅ `/data-download` - Exportar (JSON/XML/CSV)
- ✅ `/delete-account` - Wizard de eliminación

**Consentimientos Granulares:**

```typescript
✅ Marketing emails
✅ Analytics & tracking
✅ Share with dealers
✅ Personalized recommendations
✅ Location tracking
```

**Contacto DPO:**

- Email: privacidad@okla.com.do
- Teléfono: +1-809-555-0100 ext. 333

**Documento:** `26-privacy-gdpr.md`

---

## 3. LEY 11-92 (Código Tributario DGII)

### 🔴 CRÍTICO - Sin UI (0%)

| Componente      | Backend | Frontend | Gap      |
| --------------- | ------- | -------- | -------- |
| **Reporte 607** | ✅ 80%  | 🔴 0%    | **-80%** |
| **Reporte 606** | ✅ 80%  | 🔴 0%    | **-80%** |
| Generación NCF  | ✅ 90%  | ✅ 100%  | +10%     |
| Facturas PDF    | ✅ 100% | ✅ 100%  | 0%       |

### 📋 Plan de Acción (10 SP)

**Sprint Inmediato:**

1. **DGII607Page** (5 SP)

   ```
   Ruta: /admin/compliance/dgii/607
   Features:
   - Selector período (mes/año)
   - Preview transacciones
   - Validación formato
   - Download .txt
   - Historial reportes
   ```

2. **DGII606Page** (5 SP)
   ```
   Ruta: /admin/compliance/dgii/606
   Features:
   - Compras del período
   - Validación proveedores
   - Generación archivo
   - Download .txt
   ```

**Formato 607 DGII:**

```
RNC/Cédula|Tipo|NCF|NCF Modificado|Fecha|Monto|ITBIS|...
00112345678|01|B0100000001||15/01/2026|50000.00|9000.00|...
```

**Calendario DGII:**

- 607/606: Día 10 de cada mes
- IT-1: Trimestral
- Declaración Jurada: Marzo (anual)

**Documento:** `33-facturacion-dgii.md`

---

## 4. LEY 358-05 (Pro Consumidor - Protección al Consumidor) 🆕

### 🔴 CRÍTICO (35%) - Sistema de Quejas Faltante

| Proceso                       | Backend | Frontend | Estado      | Prioridad |
| ----------------------------- | ------- | -------- | ----------- | --------- |
| **CONS-INFO-001** Info básica | ✅ 100% | ✅ 90%   | ✅ Completo | ✅ BAJA   |
| **CONS-QUEJA-001** Quejas     | 🟡 40%  | 🔴 0%    | 🔴 CRÍTICO  | 🔴 ALTA   |
| **CONS-GAR-001** Garantías    | 🟡 40%  | 🟡 30%   | 🟡 Parcial  | 🟡 MEDIA  |
| **CONS-DEV-001** Retracto     | 🔴 0%   | 🔴 0%    | 🔴 CRÍTICO  | 🔴 ALTA   |
| **PC-002/003** Mediación      | 🔴 0%   | 🔴 0%    | 🔴 CRÍTICO  | 🔴 ALTA   |

**Cobertura Global:** 🔴 **35% CRÍTICO**

### ✅ Implementado (35%)

**Información al Consumidor:**

- ✅ Dealers muestran RNC, dirección, teléfono, email (DealerProfilePage)
- ✅ Vehículos muestran VIN, specs completas, historial (VehicleDetailPage)
- ✅ Badge "Warranty" en dealers que ofrecen garantía (DealerCard)

**Documentos:** `11-help-center.md` + [04-proconsumidor.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/04-proconsumidor.md)

### 🔴 Faltante (65%) - COMPLIANCE BLOCKER

| Proceso                         | Brecha                                   | Prioridad  |
| ------------------------------- | ---------------------------------------- | ---------- |
| **Sistema de Quejas**           | NO existe `/complaints`, formulario, API | 🔴 CRÍTICA |
| **Mediación de Disputas**       | NO existe workflow, timer, dashboard     | 🔴 CRÍTICA |
| **Escalamiento Pro Consumidor** | NO existe expediente PDF, integración    | 🔴 ALTA    |
| **Derecho de Retracto (48h)**   | NO existe endpoint, UI, validación       | 🔴 ALTA    |
| **Reclamos de Garantía**        | NO existe entidad `Warranty`, proceso    | 🟡 MEDIA   |

### 📋 Plan de Acción (66 SP)

**Sprint Inmediato (Crítico - 21 SP):**

1. **ConsumerProtectionController** (8 SP)
   - Endpoints: `POST /api/consumer/complaints`, `GET /api/consumer/complaints/my`
   - Entidades: `Complaint`, `ComplaintType`, `ComplaintStatus`, `ComplaintResolution`
   - Notificaciones a vendedor (48h respuesta)
   - Número de caso: QJ-2026-XXXXX

2. **ComplaintsPage + NewComplaintPage** (13 SP)
   - Ruta: `/complaints` - Lista de quejas del usuario
   - Ruta: `/complaints/new` - Formulario con 10 tipos de queja
   - Upload evidencia (MediaService)
   - Status tracking (New, InProgress, Resolved, Escalated)

**Sprint Siguiente (Alta - 29 SP):**

3. **Sistema de Mediación Backend** (13 SP)
   - Timer de 15 días (SchedulerService)
   - Workflow: Asignar mediador → Proponer solución
   - Generación de expediente PDF
   - Email a `quejas@proconsumidor.gob.do`

4. **MediationDashboard + Escalation** (8 SP)
   - Ruta: `/admin/mediation` - Panel para mediadores
   - Timeline de resolución
   - Upload documentos adicionales
   - Botón "Escalar a Pro Consumidor"

5. **Derecho de Retracto** (8 SP)
   - Endpoint: `POST /api/consumer/retraction`
   - Validación: 48h desde compra (solo servicios OKLA)
   - RetractionRequestPage
   - Botón en confirmación de suscripción

**Sprint Final (Media - 16 SP):**

6. **Sistema de Garantías Completo** (11 SP)
   - Entidad `Warranty` (tipo, duración, cobertura, exclusiones)
   - WarrantyClaimPage - Formulario de reclamo
   - WarrantyTermsPage - Términos legales (12 meses/20K km nuevos, 3 meses/5K km usados)
   - Mostrar garantía mínima en VehicleDetailPage

7. **Información al Consumidor** (5 SP)
   - ConsumerRightsPage - Derechos del consumidor
   - Categoría "Protección al Consumidor" en HelpCenter
   - Políticas de devolución por dealer

### ⚠️ Riesgos Legales Pro Consumidor

| Artículo       | Requisito                       | Estado Actual | Sanción Potencial          |
| -------------- | ------------------------------- | ------------- | -------------------------- |
| **Art. 48**    | Sistema de atención de quejas   | 🔴 NO         | Multa 10-100 salarios      |
| **Art. 56**    | Plazo de respuesta 5 días       | 🔴 NO         | Multa + cierre temporal    |
| **Art. 62**    | Información veraz y suficiente  | ✅ SÍ (80%)   | N/A                        |
| **Art. 51**    | Derecho de retracto (servicios) | 🔴 NO         | Multa + reembolso forzado  |
| **Art. 45-47** | Garantía legal mínima           | 🟡 PARCIAL    | Multa + daños y perjuicios |

**Recomendación:**  
⚠️ **Implementar Sistema de Quejas antes del lanzamiento público** - BLOCKER

---

## 5. ComplianceService - Dashboard General

### 🔴 CRÍTICO - ADM-COMP sin Homepage

**Páginas Faltantes:**

| Ruta                             | Funcionalidad       | Backend | UI    | Prioridad  |
| -------------------------------- | ------------------- | ------- | ----- | ---------- |
| `/admin/compliance/dashboard`    | Dashboard principal | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/dgii/607`     | Formato 607         | ✅ 80%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/transactions` | Monitoreo           | 🟡 60%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/alerts`       | Alertas umbral      | 🟡 60%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/risks`        | Risk Assessment     | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| `/admin/compliance/calendar`     | Calendario          | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| `/admin/compliance/training`     | Capacitaciones      | ✅ 100% | 🔴 0% | 🟡 MEDIA   |

**Páginas Existentes:**

- ✅ `/admin/compliance/watchlist` - WatchlistAdminPage (95%)
- ✅ `/admin/compliance/str` - STRReportsPage (90%)

**Documento:** `15-admin-compliance.md`

---

## 6. Endpoints Backend Disponibles

### ComplianceService (NO USADOS)

```typescript
// ✅ BACKEND IMPLEMENTADO, 🔴 SIN UI

GET / api / compliance / frameworks; // 🔴 No usado
GET / api / compliance / requirements; // 🔴 No usado
GET / api / compliance / controls; // 🔴 No usado
GET / api / compliance / assessments; // 🔴 No usado
GET / api / compliance / findings; // 🔴 No usado
GET / api / compliance / calendar / upcoming; // 🔴 No usado
GET / api / compliance / training; // 🔴 No usado
GET / api / compliance / dashboard; // 🔴 No usado
```

**Cobertura:** 0 de 10 endpoints tienen UI (0%)

### KYCService (BIEN USADOS)

```typescript
// ✅ BACKEND IMPLEMENTADO, ✅ UI COMPLETA

GET / api / kyc / kycprofiles / { id }; // ✅ UserDashboardPage
POST / api / kyc / kycprofiles; // ✅ VerificationPage
POST / api / kyc / submit - review; // ✅ VerificationPage
POST / api / kyc / approve; // ✅ KYCAdminReviewPage
GET / api / kyc / strs; // ✅ STRReportsPage
POST / api / kyc / strs; // ✅ STRReportsPage
GET / api / kyc / watchlist; // ✅ WatchlistAdminPage
POST / api / kyc / watchlist; // ✅ WatchlistAdminPage
```

**Cobertura:** 10 de 10 endpoints tienen UI (100%) ✅

### PrivacyService (EXCELENTE)

```typescript
// ✅ BACKEND IMPLEMENTADO, ✅ UI COMPLETA

GET /api/privacy/my-data              // ✅ MyDataPage
POST /api/privacy/export              // ✅ DataDownloadPage
POST /api/privacy/delete-account      // ✅ DeleteAccountPage
GET /api/privacy/consents             // ✅ PrivacyCenterPage
PUT /api/privacy/consents/{id}        // ✅ PrivacyCenterPage
```

**Cobertura:** 6 de 6 endpoints tienen UI (100%) ✅

---

## 7. Matriz de Priorización

### 🔴 Críticas (Sprint Actual - 57 SP)

| #   | Tarea                         | SP  | Impacto                       |
| --- | ----------------------------- | --- | ----------------------------- |
| 1   | **ConsumerProtection API** 🆕 | 8   | Ley 358-05 compliance blocker |
| 2   | **ComplaintsPage** 🆕         | 13  | Sistema de quejas (Art. 48)   |
| 3   | ComplianceDashboardPage       | 8   | ADM-COMP sin homepage         |
| 4   | TransactionMonitoringPage     | 8   | Ley 155-17 umbral $500K       |
| 5   | AlertsDashboardPage           | 5   | Alertas tiempo real           |
| 6   | DGII607Page                   | 5   | Compliance Ley 11-92          |
| 7   | DGII606Page                   | 5   | Compliance Ley 11-92          |
| 8   | DueDiligencePage              | 5   | DDC reforzada                 |

### 🟡 Altas (Sprint Siguiente - 47 SP)

| #   | Tarea                  | SP  | Impacto                  |
| --- | ---------------------- | --- | ------------------------ |
| 7   | RiskAssessmentPage     | 8   | Evaluación riesgos       |
| 8   | OriginOfFundsForm      | 3   | Formulario origen fondos |
| 9   | ComplianceCalendarPage | 5   | Calendario obligaciones  |
| 10  | FrameworksPage         | 2   | Gestión marcos           |

### 🟢 Medias (Backlog - 21 SP)

| #   | Tarea                  | SP  | Impacto               |
| --- | ---------------------- | --- | --------------------- |
| 11  | TrainingManagementPage | 8   | Capacitaciones PLD    |
| 12  | ControlsTestingPage    | 5   | Pruebas de controles  |
| 13  | AuditReportsPage       | 5   | Reportes consolidados |
| 14  | FindingsPage           | 3   | Hallazgos compliance  |

---

## 7. Arquitectura Frontend Propuesta

### Estructura de Carpetas

```
frontend/web/src/
├── pages/
│   └── admin/
│       └── compliance/
│           ├── ComplianceDashboardPage.tsx        🔴 CREAR
│           ├── TransactionMonitoringPage.tsx      🔴 CREAR
│           ├── AlertsDashboardPage.tsx            🔴 CREAR
│           ├── DueDiligencePage.tsx               🔴 CREAR
│           ├── RiskAssessmentPage.tsx             🔴 CREAR
│           ├── ComplianceCalendarPage.tsx         🔴 CREAR
│           ├── TrainingManagementPage.tsx         🔴 CREAR
│           ├── DGII607Page.tsx                    🔴 CREAR
│           ├── DGII606Page.tsx                    🔴 CREAR
│           ├── WatchlistAdminPage.tsx             ✅ EXISTE
│           └── STRReportsPage.tsx                 ✅ EXISTE
│
├── services/
│   ├── complianceService.ts                       🔴 CREAR
│   ├── transactionMonitoringService.ts            🔴 CREAR
│   ├── kycService.ts                              ✅ EXISTE
│   └── privacyService.ts                          ✅ EXISTE
│
└── components/
    └── admin/
        └── compliance/
            ├── ComplianceMetricCard.tsx           🔴 CREAR
            ├── RegulatoryAlert.tsx                🔴 CREAR
            ├── RiskScoreGauge.tsx                 🔴 CREAR
            ├── TransactionList.tsx                🔴 CREAR
            ├── DGII607Generator.tsx               🔴 CREAR
            └── TrainingProgress.tsx               🔴 CREAR
```

---

## 8. Componentes Reutilizables Necesarios

```tsx
// Dashboard Compliance
<ComplianceMetricCard />
<RegulatoryAlert />
<RiskScoreGauge />
<DeadlineCountdown />
<ComplianceTimeline />
<STRQuickCreate />

// Transacciones
<TransactionList />
<TransactionDetail />
<TransactionAlertBadge />
<ThresholdProgressBar />

// DGII
<DGII607Generator />
<DGII606Generator />
<NCFValidator />
<TaxReportPreview />

// Capacitaciones
<TrainingCard />
<TrainingProgress />
<CertificateViewer />
```

---

## 9. Integraciones Externas Pendientes

| Integración             | Estado   | Prioridad | Notas                |
| ----------------------- | -------- | --------- | -------------------- |
| **DGII API**            | 🔴 FALTA | ALTA      | Envío 607/606        |
| **UAF Portal**          | 🔴 FALTA | ALTA      | Envío ROS            |
| **Refinitiv/Dow Jones** | 🔴 FALTA | MEDIA     | PEPs internacionales |
| **OFAC API**            | 🔴 FALTA | MEDIA     | Sanciones USA        |
| **UN Sanctions**        | 🔴 FALTA | MEDIA     | Sanciones ONU        |

---

## 10. Checklist de Validación

### Ley 155-17 (AML/PLD)

- [x] KYC básico funcional
- [x] Watchlist de PEPs
- [x] Reportes ROS a UAF
- [ ] Dashboard compliance officer
- [ ] Monitoreo transacciones > $500K
- [ ] Alertas de umbral
- [ ] Due Diligence reforzada
- [ ] Risk Assessment

### Ley 172-13 (Privacidad)

- [x] Derechos ARCO completos
- [x] Consentimientos granulares
- [x] Exportación de datos
- [x] Right to be forgotten
- [x] Cookie consent
- [x] Privacy policy
- [x] DPO contact info

### Ley 11-92 (DGII)

- [x] Generación NCF
- [x] Facturas PDF
- [x] Notas de crédito
- [ ] Reporte 607
- [ ] Reporte 606
- [x] Validación RNC

### Ley 126-02 (Comercio Electrónico) 🆕

- [x] Términos y condiciones publicados
- [x] Política de privacidad publicada
- [x] Aceptación de términos en registro
- [x] Confirmación automática de transacciones
- [ ] Información legal completa en footer (falta RNC, razón social)
- [ ] Firma digital de contratos
- [ ] Verificación de documentos

---

## 11. Referencias Documentales

### Documentos Proceso Matrix

- `08-COMPLIANCE-LEGAL-RD/01-compliance-service.md`
- `08-COMPLIANCE-LEGAL-RD/01-ley-155-17.md`
- `08-COMPLIANCE-LEGAL-RD/02-ley-172-13.md`
- `08-COMPLIANCE-LEGAL-RD/03-dgii-integration.md` 🆕
- `08-COMPLIANCE-LEGAL-RD/04-proconsumidor.md` 🆕
- `08-COMPLIANCE-LEGAL-RD/05-compliance-reports.md` 🆕
- `08-COMPLIANCE-LEGAL-RD/06-ley-126-02-comercio-electronico.md` 🆕
- `05-PAGOS-FACTURACION/04-invoicing-service.md`

### Documentos Frontend

- `04-PAGINAS/15-admin-compliance.md`
- `04-PAGINAS/26-privacy-gdpr.md`
- `04-PAGINAS/27-kyc-verificacion.md`
- `04-PAGINAS/33-facturacion-dgii.md`
- `04-PAGINAS/11-help-center.md` 🆕

---

## 12. Contactos Reguladores

| Regulador             | Contacto                            | Competencia |
| --------------------- | ----------------------------------- | ----------- |
| **UAF**               | uaf@uaf.gob.do / 809-540-8787       | Ley 155-17  |
| **INDOTEL**           | datospersonales@indotel.gob.do      | Ley 172-13  |
| **DGII**              | 809-689-3444 / consulta@dgii.gov.do | Ley 11-92   |
| **Pro Consumidor** 🆕 | quejas@proconsumidor.gob.do / \*462 | Ley 358-05  |
| **INTRANT** 🆕        | 809-920-2020 / info@intrant.gob.do  | Ley 63-17   |

---

## 15. Ley 63-17 - INTRANT - Registro Vehicular 🆕

> **Referencia:** `process-matrix/08-COMPLIANCE-LEGAL-RD/07-ley-63-17-intrant.md`  
> **VehicleRegistryService:** Puerto 5XXX (TBD)  
> **Estado Backend:** 🟡 50% Implementado  
> **Estado Frontend:** 🔴 0% Implementado

### 📋 Resumen de Cumplimiento

| Proceso                            | Backend | Frontend UI | Gap       | Prioridad  |
| ---------------------------------- | ------- | ----------- | --------- | ---------- |
| **Verificación de placa**          | ✅ 100% | 🔴 0%       | **-100%** | 🔴 CRÍTICA |
| **Historial de propietarios**      | ✅ 100% | 🔴 0%       | **-100%** | 🔴 ALTA    |
| **Multas pendientes**              | 🟡 50%  | 🔴 0%       | **-50%**  | 🔴 CRÍTICA |
| **Revisión técnica**               | 🟡 40%  | 🔴 0%       | **-40%**  | 🟡 MEDIA   |
| **Transferencia de propiedad**     | 🟡 60%  | 🔴 0%       | **-60%**  | 🟡 MEDIA   |
| **Validación de VIN**              | ✅ 100% | 🔴 0%       | **-100%** | 🟡 MEDIA   |
| **Verificación de embargos/liens** | ✅ 100% | 🔴 0%       | **-100%** | 🔴 ALTA    |

**Cobertura Global:** Backend 50% | Frontend 0% | Gap -50% = **🔴 CRÍTICO**

### ✅ BACKEND IMPLEMENTADO (VehicleRegistryService)

**Entidades Completadas (7/7):**

```typescript
// 1. VehicleRegistration
{
  id: UUID,
  vehicleId: UUID,
  plateNumber: string,
  vin: string,
  registrationDate: DateTime,
  expirationDate: DateTime,
  status: RegistrationStatus, // Active, Suspended, Expired, Cancelled
  ownerIdentification: string, // Cédula/RNC
  ownerName: string,
  ownerType: OwnerType, // Individual, Company
  vehicleType: VehicleType, // Sedan, SUV, Motorcycle, etc.
  province: string,
  municipality: string
}

// 2. OwnershipTransfer
{
  id: UUID,
  vehicleRegistrationId: UUID,
  fromOwnerIdentification: string,
  toOwnerIdentification: string,
  transferDate: DateTime,
  status: TransferStatus, // Pending, Completed, Rejected, Cancelled
  transferPrice: decimal,
  paymentReference: string,
  notaryPublicName: string,
  contractNumber: string
}

// 3. LienRecord (Gravámenes/Embargos)
{
  id: UUID,
  vehicleRegistrationId: UUID,
  lienType: LienType, // Mortgage, Judicial, Tax, Customs
  creditorName: string,
  amount: decimal,
  recordedDate: DateTime,
  releasedDate: DateTime?,
  status: LienStatus // Active, Released
}

// 4. VinValidation
{
  id: UUID,
  vin: string,
  make: string,
  model: string,
  year: int,
  isValid: bool,
  validationDate: DateTime,
  validationSource: string // WMI Database, NICB, etc.
}
```

**Controladores Disponibles (4):**

```csharp
// ✅ IMPLEMENTADOS - SIN UI

// 1. RegistrationsController
GET    /api/registrations/plate/{plateNumber}         // ✅ OK - 🔴 Sin UI
GET    /api/registrations/vin/{vin}                   // ✅ OK - 🔴 Sin UI
GET    /api/registrations/owner/{identification}      // ✅ OK - 🔴 Sin UI
GET    /api/registrations/expired                     // ✅ OK - 🔴 Sin UI
POST   /api/registrations                             // ✅ OK - 🔴 Sin UI
POST   /api/registrations/{id}/renew                  // ✅ OK - 🔴 Sin UI
POST   /api/registrations/{id}/suspend                // ✅ OK - 🔴 Sin UI

// 2. TransfersController
GET    /api/transfers/vehicle/{vehicleId}             // ✅ OK - 🔴 Sin UI
GET    /api/transfers/pending                         // ✅ OK - 🔴 Sin UI
POST   /api/transfers                                 // ✅ OK - 🔴 Sin UI
POST   /api/transfers/{id}/complete                   // ✅ OK - 🔴 Sin UI
POST   /api/transfers/{id}/reject                     // ✅ OK - 🔴 Sin UI

// 3. LiensController
GET    /api/liens/vehicle/{vehicleId}                 // ✅ OK - 🔴 Sin UI
GET    /api/liens/vehicle/{vehicleId}/check           // ✅ OK - 🔴 Sin UI
POST   /api/liens                                     // ✅ OK - 🔴 Sin UI
POST   /api/liens/{id}/release                        // ✅ OK - 🔴 Sin UI

// 4. VinValidationController
POST   /api/vinvalidation/validate                    // ✅ OK - 🔴 Sin UI
GET    /api/vinvalidation/{vin}                       // ✅ OK - 🔴 Sin UI
```

**Cobertura Backend:** 21 endpoints implementados, 0 con UI = **0%**

### 🔴 PÁGINAS CRÍTICAS FALTANTES (Frontend)

| Ruta Propuesta                 | Funcionalidad                       | Backend | UI    | Prioridad  | Story Points |
| ------------------------------ | ----------------------------------- | ------- | ----- | ---------- | ------------ |
| `/verify/vehicle`              | Verificador público de placa        | ✅ 100% | 🔴 0% | 🔴 CRÍTICA | 8 SP         |
| `/vehicles/:id/intrant`        | Badge INTRANT en detalle vehículo   | ✅ 100% | 🔴 0% | 🔴 CRÍTICA | 5 SP         |
| `/vehicles/:id/history`        | Historial propietarios (autorizado) | ✅ 100% | 🔴 0% | 🔴 ALTA    | 8 SP         |
| `/admin/intrant/registrations` | Gestión de registros vehiculares    | ✅ 100% | 🔴 0% | 🟡 MEDIA   | 13 SP        |
| `/admin/intrant/transfers`     | Gestión de transferencias           | ✅ 100% | 🔴 0% | 🟡 MEDIA   | 13 SP        |
| `/admin/intrant/liens`         | Gestión de gravámenes               | ✅ 100% | 🔴 0% | 🟡 MEDIA   | 8 SP         |
| `/seller/intrant/verify`       | Verificar vehículo antes publicar   | ✅ 100% | 🔴 0% | 🔴 ALTA    | 5 SP         |

**Total:** 60 Story Points

### 🚨 Componentes UI Críticos Faltantes

#### 1. IntrantBadge Component (5 SP)

```tsx
// IntrantBadge.tsx - Mostrar en VehicleDetailPage

interface IntrantBadgeProps {
  status: "verified" | "pending" | "issues" | "not-verified";
  finesCount?: number;
  inspectionExpired?: boolean;
  hasLiens?: boolean;
}

// Estados del badge:
// ✅ "Verificado INTRANT" - Todo en orden
// ⚠️ "Multas Pendientes" - Tiene multas (alerta amarilla)
// 🔴 "Con Gravámenes" - Tiene embargos (alerta roja)
// ⚠️ "Revisión Vencida" - Inspección expirada
// ❌ "No Verificado" - No se pudo consultar
```

#### 2. VehicleVerifierPage (8 SP)

```tsx
// /verify/vehicle?plate=A123456
// Página pública para cualquiera verificar un vehículo

Secciones:
- Input de búsqueda por placa
- Información básica: Marca, modelo, año, color, VIN
- Estado legal: Multas, embargos, revisión técnica
- Número de propietarios (sin nombres)
- Badge "Verificado" o alertas
- CTA: "¿Quieres comprar este vehículo? Ver publicación en OKLA"
```

#### 3. IntrantSection en VehicleDetailPage (3 SP)

```tsx
// Agregar a VehicleDetailPage.tsx

<section className="mt-8 bg-gray-50 p-6 rounded-lg">
  <h3 className="text-xl font-bold mb-4">Verificación INTRANT</h3>
  <IntrantBadge {...badgeProps} />

  {/* Info del registro */}
  <div className="mt-4 grid grid-cols-2 gap-4">
    <div>Placa: {vehicle.plateNumber}</div>
    <div>VIN: {vehicle.vin}</div>
    <div>Propietarios: {vehicle.ownersCount}</div>
    <div>Última transferencia: {vehicle.lastTransferDate}</div>
  </div>

  {/* Alertas */}
  {vehicle.hasFines && (
    <Alert variant="warning">
      Este vehículo tiene {vehicle.finesCount} multas pendientes por RD${" "}
      {vehicle.finesTotal}. Deben ser pagadas antes de la transferencia.
    </Alert>
  )}

  {vehicle.hasLiens && (
    <Alert variant="error">
      🔴 ATENCIÓN: Este vehículo tiene gravámenes activos. No puede ser
      transferido hasta liberarlos.
    </Alert>
  )}
</section>
```

#### 4. VehicleHistoryPage (8 SP)

```tsx
// /vehicles/:id/history
// Solo para compradores verificados con autorización del vendedor

Secciones:
- Timeline de propietarios (fechas, provincias, tipo)
- Número de transferencias totales
- Tiempo promedio de propiedad
- Cambios de provincia (indica movilidad)
- Solicitar autorización si no tiene acceso
```

### ⚙️ Integración Técnica Requerida

#### Opciones de Integración con INTRANT

| Opción                  | Descripción                    | Viabilidad    | Costo      | Recomendación  |
| ----------------------- | ------------------------------ | ------------- | ---------- | -------------- |
| **API oficial INTRANT** | Solicitar acceso institucional | 🟡 En trámite | Gratis     | 🟡 Esperar     |
| **Proveedor tercero**   | Usar servicio intermediario    | ✅ Inmediato  | $300-500/m | ✅ RECOMENDADO |
| **Web scraping**        | Extraer del portal INTRANT     | ⚠️ Riesgoso   | Dev time   | ❌ NO          |
| **Verificación manual** | Empleado OKLA verifica         | 🟡 Temporal   | Staff      | 🟡 Temporal    |

**Proveedor Recomendado:** ConsultData.do o DataRD (servicios de data dominicana)

#### Flujo de Verificación Propuesto

```
┌─────────────────────────────────────────────────────────────────────────┐
│              VERIFICACIÓN AUTOMÁTICA DE VEHÍCULO                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1️⃣ VENDEDOR PUBLICA VEHÍCULO                                           │
│  └── Ingresa placa + VIN en formulario de publicación                   │
│                                                                         │
│  2️⃣ OKLA CONSULTA INTRANT (Backend)                                     │
│  ├── POST /api/vinvalidation/validate                                  │
│  ├── GET /api/registrations/plate/{plateNumber}                        │
│  ├── GET /api/liens/vehicle/{vehicleId}/check                          │
│  └── Valida datos automáticamente                                      │
│                                                                         │
│  3️⃣ VERIFICACIÓN AUTOMÁTICA                                             │
│  ├── ✅ Placa válida → Continuar publicación                            │
│  ├── ✅ VIN coincide con placa → OK                                     │
│  ├── ⚠️ Multas pendientes → Alertar al vendedor                         │
│  ├── ⚠️ Revisión vencida → Notificar pero permitir                      │
│  ├── 🔴 Con gravámenes → BLOQUEAR publicación                           │
│  └── 🔴 Reportado robado → BLOQUEAR + Notificar autoridades            │
│                                                                         │
│  4️⃣ BADGE VISIBLE EN PUBLICACIÓN                                        │
│  ├── ✅ "Verificado INTRANT" → Si todo OK                               │
│  ├── ⚠️ "Multas pendientes (3)" → Alerta visible para compradores      │
│  ├── 🔴 "No disponible" → Si tiene gravámenes                           │
│  └── ❌ "No verificado" → Si no se pudo consultar (RARO)                │
│                                                                         │
│  5️⃣ COMPRADOR ACCEDE A DETALLE                                          │
│  ├── Ve IntrantBadge prominente                                        │
│  ├── Puede hacer clic en "Ver historial" (si autorizado)               │
│  ├── Ve número de propietarios anteriores                              │
│  └── Alerta si hay multas o gravámenes                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 🚨 Impacto Legal de Faltantes

#### Sin Verificación INTRANT

**Riesgos Operativos:**

- ❌ Compradores no saben si el vehículo tiene multas
- ❌ Vendedores falsifican propiedad (fraude)
- ❌ Vehículos robados se publican en la plataforma
- ❌ Gravámenes bancarios desconocidos
- ❌ Historial de accidentes oculto

**Consecuencias Legales:**

- Demandas por fraude (Ley 126-02)
- Responsabilidad solidaria por vehículos robados
- Multas por facilitar comercio ilegal
- Daño reputacional masivo

**Riesgo Financiero:**

- RD$500K - $2M en demandas por vehículo fraudulento
- Pérdida de confianza del marketplace
- Bloqueo de operaciones por autoridades

#### Multas y Sanciones INTRANT

| Incumplimiento                          | Sanción                    |
| --------------------------------------- | -------------------------- |
| Facilitar venta de vehículo robado      | Responsabilidad criminal   |
| No verificar propiedad legal            | RD$50K - $200K (Art. 203)  |
| Ocultar gravámenes a compradores        | RD$100K - $500K (Art. 215) |
| Permitir publicación con placa inválida | RD$20K - $100K (Art. 192)  |

### 📊 Plan de Implementación

#### Fase 1: Verificación Básica (21 SP) - Q2 2026

**Sprint 1 (13 SP):**

- IntrantBadge component (5 SP)
- VehicleVerifierPage (8 SP)

**Sprint 2 (8 SP):**

- IntrantSection en VehicleDetailPage (3 SP)
- Integración con proveedor de datos (5 SP)

#### Fase 2: Historial y Admin (39 SP) - Q3 2026

**Sprint 3 (16 SP):**

- VehicleHistoryPage (8 SP)
- SellerVerifyPage (5 SP)
- API service completo (3 SP)

**Sprint 4 (23 SP):**

- AdminIntrantRegistrationsPage (13 SP)
- AdminIntrantTransfersPage (10 SP)

#### Fase 3: Gravámenes y Reportes (0 SP) - Q4 2026

- AdminIntrantLiensPage (8 SP)
- Reportes de verificaciones (5 SP)

**Total:** 60 Story Points (~4 sprints)

### 🎯 Métricas de Éxito

| Métrica                      | Objetivo     | Impacto                  |
| ---------------------------- | ------------ | ------------------------ |
| % de vehículos verificados   | 95%          | Confianza del comprador  |
| Tiempo de verificación       | < 5 segundos | UX fluida                |
| Vehículos robados bloqueados | 100%         | Compliance legal         |
| Reducción de fraudes         | 70%          | Menos disputas           |
| Satisfacción del comprador   | +20 NPS      | Transparencia            |
| Vehículos con gravámenes     | < 5%         | Menos problemas de venta |

### 📚 Referencias

- **Documento matriz:** `process-matrix/08-COMPLIANCE-LEGAL-RD/07-ley-63-17-intrant.md`
- **Backend:** `VehicleRegistryService/` (21 endpoints)
- **Portal INTRANT:** intrant.gob.do
- **Consulta multas:** consultamultas.intrant.gob.do
- **Ley 63-17:** congreso.gob.do/leyes/63-17

### ⚠️ ALERTA DE PRIORIDAD

**Ley 63-17 (INTRANT) es ahora el segundo compliance blocker más crítico** después de Pro Consumidor.

**Recomendación:**

- 🔴 **Implementar Fase 1 (21 SP) ANTES del lanzamiento público**
- 🔴 Badge INTRANT debe ser visible en TODOS los vehículos
- 🔴 Bloquear publicación de vehículos robados o con gravámenes
- 🟡 Fase 2 y 3 pueden ser post-lanzamiento (2-3 meses)
- ✅ Backend ya está 50% completo - Solo falta UI

---

## 18. DGII - Automatización de Reportes e-CF 🆕

> **Referencia:** `process-matrix/08-COMPLIANCE-LEGAL-RD/12-AUTOMATIZACION-REPORTES-DGII.md`  
> **DGIIService / ExpenseService:** NO EXISTEN  
> **Estado Backend:** 🔴 0% Implementado  
> **Estado Frontend:** 🔴 0% Implementado

### 📋 Resumen de Cumplimiento

| Proceso                               | Backend | Frontend UI | Gap    | Prioridad  |
| ------------------------------------- | ------- | ----------- | ------ | ---------- |
| **Registro de Gastos/Proveedores**    | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Generación Formato 606 (Compras)**  | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Generación Formato 607 (Ventas)**   | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Generación Formato 608 (Anulados)** | 🔴 0%   | 🔴 0%       | **0%** | 🟡 MEDIA   |
| **Cálculo IT-1 (ITBIS Mensual)**      | 🔴 0%   | 🔴 0%       | **0%** | 🔴 ALTA    |
| **Cálculo IR-17 (Retenciones)**       | 🔴 0%   | 🔴 0%       | **0%** | 🟡 MEDIA   |
| **Gestión de Secuencias NCF**         | 🔴 0%   | 🔴 0%       | **0%** | 🔴 ALTA    |
| **Facturación Electrónica (e-CF)**    | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |

**Cobertura Global:** Backend 0% | Frontend 0% | Gap 0% = **🔴 CRÍTICO**

### 🚨 SISTEMA COMPLETO NO EXISTE

**Hallazgo Principal:** A pesar de que el documento de proceso existe (1,223 líneas) y hay referencias en el AdminSidebar, **NO existe ninguna implementación real**:

- ❌ DGIIService NO EXISTE
- ❌ ExpenseService NO EXISTE
- ❌ NCFGeneratorService NO EXISTE
- ❌ Format606GeneratorService NO EXISTE
- ❌ Format607GeneratorService NO EXISTE
- ❌ Format608GeneratorService NO EXISTE
- ❌ ITBISCalculatorService NO EXISTE

**Referencias en AdminSidebar (sin páginas):**

```tsx
// ✅ Links en sidebar - 🔴 PÁGINAS NO EXISTEN
{
  id: 'fiscal',
  label: 'Contabilidad & NCF (DGII)',
  children: [
    { path: '/admin/fiscal/invoices/new' },        // ❌ NO EXISTE
    { path: '/admin/fiscal/invoices' },            // ❌ NO EXISTE
    { path: '/admin/fiscal/credit-notes' },        // ❌ NO EXISTE
    { path: '/admin/fiscal/void' },                // ❌ NO EXISTE
    { path: '/admin/fiscal/ncf-sequences' },       // ❌ NO EXISTE
    { path: '/admin/fiscal/dgii/607' },            // ❌ NO EXISTE
    { path: '/admin/fiscal/dgii/608' },            // ❌ NO EXISTE
    { path: '/admin/fiscal/settings' },            // ❌ NO EXISTE
  ]
}
```

**Resultado:** Clicking en cualquier link del sidebar → **404 Page Not Found**

### � E-CF: Comprobantes Fiscales Electrónicos (Sistema Crítico) 🆕

> **Referencia Adicional:** `process-matrix/08-COMPLIANCE-LEGAL-RD/14-E-CF-COMPROBANTES-ELECTRONICOS.md`  
> **Norma Legal:** Norma General 06-2018 DGII  
> **Obligatorio desde:** Enero 1, 2025 (para empresas con RD$50M+ facturación anual)  
> **Estado Actual:** 🔴 **0% IMPLEMENTADO - CRÍTICO**

#### ¿Qué es e-CF?

El **Comprobante Fiscal Electrónico (e-CF)** es un documento tributario emitido y transmitido **en tiempo real** a la DGII, con validez legal equivalente a un NCF tradicional pero **obligatorio** para empresas que superen RD$50 millones en facturación anual.

**Características clave:**

- ✅ Firmado digitalmente con certificado INDOTEL
- ✅ Transmitido en < 5 segundos a DGII Web Services
- ✅ Validado y autorizado por DGII en tiempo real
- ✅ Código QR para verificación pública
- ✅ Almacenamiento electrónico (S3 + PostgreSQL)
- ✅ Generación automática de Formatos 606/607/608

**Tipos de e-CF que OKLA necesita:**

| Código | Tipo de e-CF              | Uso en OKLA                        | Frecuencia |
| ------ | ------------------------- | ---------------------------------- | ---------- |
| E31    | Factura de Crédito Fiscal | Dealers con RNC (suscripciones)    | Alta       |
| E32    | Factura de Consumo        | Individuos sin RNC (listings)      | Alta       |
| E34    | Nota de Crédito           | Reembolsos, cancelaciones          | Media      |
| E41    | Comprobante de Compras    | Proveedores locales sin NCF (raro) | Baja       |
| E47    | Compras del Exterior      | Digital Ocean, Stripe, Google Ads  | Alta       |

#### Flujo Técnico de e-CF

```
┌──────────────────────────────────────────────────────────────────────┐
│              FLUJO DE FACTURACIÓN ELECTRÓNICA                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1️⃣ Cliente paga suscripción ($129/mes) → Stripe/AZUL confirma      │
│  2️⃣ BillingService dispara PaymentCompletedEvent                    │
│  3️⃣ ECFService recibe evento y genera XML según Norma 06-2018       │
│  4️⃣ Firma digital con certificado INDOTEL (.pfx)                    │
│  5️⃣ Transmite a https://ecf.dgii.gov.do/ecf/RecepcionECF            │
│  6️⃣ DGII responde en < 3 segundos con TrackingNumber                │
│  7️⃣ Si aprobado: Genera PDF con QR, guarda en S3, envía email       │
│  8️⃣ Si rechazado: Alerta admin, corrige y reintenta                 │
│  9️⃣ e-CF se incluye automáticamente en Formato 607 del mes          │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

#### Arquitectura ECFService (0% implementado)

```
ECFService/
├── Domain/
│   ├── Entities/
│   │   ├── ElectronicInvoice.cs          // ❌ NO EXISTE
│   │   ├── ElectronicInvoiceItem.cs      // ❌ NO EXISTE
│   │   └── ECFSequence.cs                // ❌ NO EXISTE
│   ├── Enums/
│   │   ├── ECFType.cs (E31, E32, E34...) // ❌ NO EXISTE
│   │   └── ECFStatus.cs (Draft, Signed...) // ❌ NO EXISTE
│   └── Interfaces/
│       ├── IECFRepository.cs             // ❌ NO EXISTE
│       └── IECFGeneratorService.cs       // ❌ NO EXISTE
├── Application/
│   ├── Services/
│   │   ├── ECFGeneratorService.cs        // ❌ NO EXISTE - Genera XML
│   │   ├── DigitalSignatureService.cs    // ❌ NO EXISTE - Firma con .pfx
│   │   ├── DGIIWebService.cs             // ❌ NO EXISTE - Transmite a DGII
│   │   └── ECFPdfGenerator.cs            // ❌ NO EXISTE - PDF con QR
│   └── Commands/
│       ├── GenerateECFCommand.cs         // ❌ NO EXISTE
│       ├── TransmitECFCommand.cs         // ❌ NO EXISTE
│       └── CancelECFCommand.cs           // ❌ NO EXISTE
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ECFDbContext.cs               // ❌ NO EXISTE
│   │   └── ECFRepository.cs              // ❌ NO EXISTE
│   ├── External/
│   │   └── DGIIApiClient.cs              // ❌ NO EXISTE - SOAP/REST client
│   └── Security/
│       └── CertificateManager.cs         // ❌ NO EXISTE - Gestión de .pfx
└── Api/
    ├── Controllers/
    │   ├── ECFController.cs              // ❌ NO EXISTE
    │   ├── ECFTransmissionController.cs  // ❌ NO EXISTE
    │   └── ECFVerificationController.cs  // ❌ NO EXISTE
    └── Jobs/
        └── MonthlyReportSubmissionJob.cs // ❌ NO EXISTE - Día 10 de cada mes
```

#### Páginas Frontend Requeridas (0% implementado)

| Ruta                               | Funcionalidad                        | Backend | UI    | Story Points |
| ---------------------------------- | ------------------------------------ | ------- | ----- | ------------ |
| `/admin/fiscal/ecf/dashboard`      | Dashboard e-CF con stats del día     | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/ecf/list`           | Lista de todos los e-CF emitidos     | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/ecf/generate`       | Generar e-CF manual (backup)         | 🔴 0%   | 🔴 0% | 21 SP        |
| `/admin/fiscal/ecf/view/:id`       | Ver e-CF (XML, PDF, status DGII)     | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/ecf/rejected`       | e-CF rechazados por DGII             | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/ecf/transmit-batch` | Transmisión masiva (reenviar)        | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/ecf/settings`       | Config: certificado, URLs DGII, test | 🔴 0%   | 🔴 0% | 8 SP         |

**Total Fase e-CF:** 79 Story Points (~5 sprints)

#### Base de Datos e-CF (NO EXISTE)

```sql
-- Tabla principal
CREATE TABLE electronic_invoices (
    id UUID PRIMARY KEY,
    ecf_number VARCHAR(20) NOT NULL UNIQUE,  -- E3100000001
    ecf_type INTEGER NOT NULL,                -- 31, 32, 34, 41, 47
    status VARCHAR(20) NOT NULL,              -- Draft, Signed, Submitted, Approved, Rejected

    -- Emisor (OKLA)
    issuer_rnc VARCHAR(15) DEFAULT '133325901',
    issuer_name VARCHAR(200) DEFAULT 'OKLA S.R.L.',

    -- Receptor (Cliente)
    receiver_rnc VARCHAR(15),
    receiver_name VARCHAR(200) NOT NULL,
    receiver_email VARCHAR(200),

    -- Montos
    subtotal DECIMAL(18,2) NOT NULL,
    tax_rate DECIMAL(5,2) DEFAULT 18.00,     -- ITBIS 18%
    tax_amount DECIMAL(18,2) NOT NULL,
    total DECIMAL(18,2) NOT NULL,

    -- DGII Response
    tracking_number VARCHAR(50),              -- DGII tracking
    approval_code VARCHAR(50),                -- Código de autorización
    dgii_message TEXT,                        -- Mensaje de error si rechazado

    -- Documentos
    signed_xml_url TEXT,                      -- S3 URL del XML firmado
    pdf_url TEXT,                             -- S3 URL del PDF con QR
    qr_code TEXT,                             -- Código QR para verificación

    -- Referencias
    original_ecf_number VARCHAR(20),          -- Para notas de crédito E34
    payment_id UUID,                          -- FK a payments

    -- Fechas
    issue_date TIMESTAMP NOT NULL,
    dgii_submit_date TIMESTAMP,
    dgii_approval_date TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Items del e-CF
CREATE TABLE electronic_invoice_items (
    id UUID PRIMARY KEY,
    electronic_invoice_id UUID NOT NULL REFERENCES electronic_invoices(id),
    line_number INTEGER NOT NULL,
    description VARCHAR(500) NOT NULL,
    quantity DECIMAL(18,4) DEFAULT 1,
    unit_price DECIMAL(18,2) NOT NULL,
    tax_amount DECIMAL(18,2) NOT NULL,
    total DECIMAL(18,2) NOT NULL
);

-- Secuencias por tipo (E31, E32, E34, E47)
CREATE TABLE ecf_sequences (
    id UUID PRIMARY KEY,
    ecf_type INTEGER NOT NULL UNIQUE,
    current_number INTEGER NOT NULL DEFAULT 0,
    prefix VARCHAR(10) NOT NULL,              -- 'E31', 'E32', etc.
    updated_at TIMESTAMP DEFAULT NOW()
);
```

**Estado:** ❌ **Ninguna tabla existe** - Base de datos limpia

#### 🔥 Impacto Legal de NO tener e-CF

**Requisito Legal:** Norma General 06-2018, Resolución 13-2019

**Consecuencias si OKLA supera RD$50M sin e-CF:**

1. **Todas las facturas son INVÁLIDAS** ⚠️
   - DGII NO reconoce NCF tradicionales después del deadline
   - Clientes NO pueden deducir ITBIS de sus compras
   - Dealers pueden pedir reembolso por "factura inválida"

2. **DGII puede cerrar el negocio** 🔴
   - Suspensión de RNC por 30-90 días
   - Imposibilidad de facturar durante suspensión
   - Pérdida de ingresos completa

3. **Multas acumulativas** 💰
   - Primera infracción: RD$100,000 - $200,000
   - Reincidencia: RD$300,000 - $500,000
   - Multa por factura inválida: RD$5,000 por factura

4. **Auditoría DGII inmediata**
   - Revisión de todos los años fiscales
   - Recálculo de impuestos
   - Intereses moratorios retroactivos

**Ejemplo de Riesgo:**

Si OKLA emite 500 facturas/mes sin e-CF después del deadline:

- 500 facturas × RD$5,000 = **RD$2,500,000 en multas/mes**
- - RD$200,000 multa por no usar e-CF
- = **RD$2.7M por mes** = **RD$32.4M por año** ($540K USD)

#### 📅 Requisitos Pre-implementación

**1. Certificado Digital (OBLIGATORIO)**

- Proveedor: INDOTEL, Cámara de Comercio, CertiSign
- Costo: RD$5,000 - $15,000 por año
- Formato: .pfx (PKCS#12)
- Tiempo de emisión: 5-10 días hábiles

**2. Acceso a Ambiente de Pruebas DGII**

- URL: https://ecf.dgii.gov.do/testecf/
- Solicitar en oficina DGII
- Ejecutar mínimo 20 e-CF de cada tipo (E31, E32, E34)
- Tiempo de certificación: 30-60 días

**3. Credenciales Web Services**

- Autenticación con certificado digital
- Endpoints SOAP/REST para:
  - RecepcionECF (enviar e-CF)
  - ConsultaECF (verificar estado)
  - AnulacionECF (cancelar e-CF)

#### 💼 Automatización de Reportes Fiscales

**Ventaja Principal de e-CF:** Reportes fiscales se generan **automáticamente**

**Sin e-CF (Actual - Esfuerzo manual):**

- Formato 607: 2-4 horas/mes copiando datos de facturas
- Formato 608: 1 hora/mes registrando anulaciones
- IT-1 ITBIS: 1-2 horas/mes calculando ITBIS cobrado vs pagado
- **Total: 10-15 horas/mes**

**Con e-CF (Automático):**

- Formato 607: **5 minutos** (solo verificar en DGII Oficina Virtual)
- Formato 608: **0 minutos** (DGII consolida notas de crédito E34)
- IT-1 ITBIS: **5 minutos** (pre-llenado desde e-CF)
- **Total: 10-15 minutos/mes**

**Ahorro:** 95% del tiempo fiscal + 0% errores humanos

#### 🎯 Story Points Específicos e-CF

**Backend ECFService:** 55 SP

- Domain entities + enums: 8 SP
- ECFGeneratorService (XML): 13 SP
- DigitalSignatureService: 8 SP
- DGIIWebService (integración): 13 SP
- ECFPdfGenerator (QR): 8 SP
- Event handlers: 5 SP

**Frontend Admin Pages:** 79 SP

- Dashboard e-CF: 13 SP
- Lista de e-CF: 8 SP
- Generación manual: 21 SP
- Visor de e-CF: 8 SP
- Gestión de rechazados: 8 SP
- Transmisión batch: 13 SP
- Configuración: 8 SP

**Pruebas & Certificación:** 21 SP

- Pruebas en ambiente test DGII: 8 SP
- Certificación DGII: 8 SP
- Documentación: 5 SP

**Total e-CF:** 155 Story Points (~10 sprints = 2.5 meses)

#### 📊 Comparación con Otros Sistemas Fiscales

| Sistema             | Story Points | Prioridad  | Riesgo Legal        | Estado |
| ------------------- | ------------ | ---------- | ------------------- | ------ |
| **e-CF**            | **155 SP**   | 🔴 CRÍTICA | $540K/año en multas | 🔴 0%  |
| Formato 606/607/608 | 94 SP        | 🔴 CRÍTICA | $300K/año en multas | 🔴 0%  |
| NCF Management      | 34 SP        | 🔴 ALTA    | $100K/año en multas | 🔴 0%  |
| Sistema de Gastos   | 105 SP       | 🔴 CRÍTICA | $200K/año en multas | 🔴 0%  |

**e-CF es el sistema MÁS CRÍTICO** porque:

1. Sin e-CF, TODAS las facturas son inválidas
2. DGII puede cerrar el negocio inmediatamente
3. Clientes pueden pedir reembolsos masivos
4. Multa: $540K/año (la más alta de todos los sistemas)

#### ⚠️ ALERTA LEGAL MÁXIMA

**SI OKLA SUPERA RD$50M EN FACTURACIÓN SIN e-CF:**

- ❌ NO puede facturar legalmente
- ❌ Todas las facturas emitidas son NULAS
- ❌ DGII puede cerrar el negocio en 24-48 horas
- ❌ Multas: RD$2.7M por mes ($45K USD/mes)
- ❌ Dealers pueden demandar por facturación inválida

**RECOMENDACIÓN:** Implementar e-CF ANTES de alcanzar RD$50M en ventas anuales. Si proyección indica que se superará el umbral en 2026, **COMENZAR DESARROLLO AHORA** (febrero 2026) para tener sistema listo en Q2 2026.

**Tiempo Mínimo de Implementación:** 3-4 meses

- Desarrollo: 2.5 meses (155 SP)
- Certificación DGII: 1-1.5 meses
- Pruebas en producción: 0.5 meses

**Inversión:** $21,700 USD (155 SP × $140/SP)  
**Ahorro anual en multas:** $540,000 USD  
**ROI:** Menos de 1 mes ⚡

---

### �📊 Arquitectura Requerida (Documento)

El documento especifica 3 microservicios necesarios:

#### 1. DGIIService (0% implementado)

```
DGIIService/
├── Api/Controllers/
│   ├── Format606Controller.cs       // ❌ NO EXISTE
│   ├── Format607Controller.cs       // ❌ NO EXISTE
│   ├── Format608Controller.cs       // ❌ NO EXISTE
│   ├── IR17Controller.cs            // ❌ NO EXISTE
│   ├── IT1Controller.cs             // ❌ NO EXISTE
│   ├── NCFController.cs             // ❌ NO EXISTE
│   └── ReportsController.cs         // ❌ NO EXISTE
├── Application/Services/
│   ├── NCFGeneratorService.cs       // ❌ NO EXISTE
│   ├── Format606GeneratorService.cs // ❌ NO EXISTE
│   ├── Format607GeneratorService.cs // ❌ NO EXISTE
│   ├── ITBISCalculatorService.cs    // ❌ NO EXISTE
│   └── DGIIValidatorService.cs      // ❌ NO EXISTE
├── Domain/Entities/
│   ├── NCFSequence.cs               // ❌ NO EXISTE
│   ├── NCFIssued.cs                 // ❌ NO EXISTE
│   ├── NCFReceived.cs               // ❌ NO EXISTE
│   ├── DGIIFormat.cs                // ❌ NO EXISTE
│   └── FiscalPeriod.cs              // ❌ NO EXISTE
└── Infrastructure/
    └── FileGenerators/              // ❌ NO EXISTE
```

#### 2. ExpenseService (0% implementado)

```
ExpenseService/
├── Api/Controllers/
│   ├── ExpensesController.cs       // ❌ NO EXISTE
│   ├── ProvidersController.cs      // ❌ NO EXISTE
│   └── ExpenseDocumentsController  // ❌ NO EXISTE
├── Domain/Entities/
│   ├── Expense.cs                  // ❌ NO EXISTE
│   ├── ExpenseProvider.cs          // ❌ NO EXISTE
│   ├── ExpenseDocument.cs          // ❌ NO EXISTE
│   └── ExpenseApproval.cs          // ❌ NO EXISTE
└── Infrastructure/
    └── Persistence/                // ❌ NO EXISTE
```

#### 3. ECFService (Facturación Electrónica) (0% implementado)

```
ECFService/
├── Api/Controllers/
│   ├── ECFController.cs            // ❌ NO EXISTE
│   └── ECFTransmissionController   // ❌ NO EXISTE
├── Application/Services/
│   ├── ECFGeneratorService.cs      // ❌ NO EXISTE
│   ├── ECFSigningService.cs        // ❌ NO EXISTE
│   └── DGIITransmissionService.cs  // ❌ NO EXISTE
└── Infrastructure/
    └── External/
        └── DGIIApiClient.cs        // ❌ NO EXISTE
```

### 🔴 PÁGINAS CRÍTICAS FALTANTES (Frontend)

#### Fase 1: Gestión de Gastos (34 SP)

| Ruta Propuesta                   | Funcionalidad                | Backend | UI    | Story Points |
| -------------------------------- | ---------------------------- | ------- | ----- | ------------ |
| `/admin/fiscal/expenses`         | Lista de gastos con filtros  | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/expenses/new`     | Formulario registro de gasto | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/expenses/:id`     | Detalle del gasto + docs     | 🔴 0%   | 🔴 0% | 5 SP         |
| `/admin/fiscal/expenses/approve` | Aprobación de gastos         | 🔴 0%   | 🔴 0% | 8 SP         |

#### Fase 2: Proveedores (21 SP)

| Ruta Propuesta                | Funcionalidad         | Backend | UI    | Story Points |
| ----------------------------- | --------------------- | ------- | ----- | ------------ |
| `/admin/fiscal/providers`     | Lista de proveedores  | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/providers/new` | Agregar proveedor     | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/providers/:id` | Detalle del proveedor | 🔴 0%   | 🔴 0% | 5 SP         |

#### Fase 3: Generación de Formatos DGII (42 SP)

| Ruta Propuesta                 | Funcionalidad            | Backend | UI    | Story Points |
| ------------------------------ | ------------------------ | ------- | ----- | ------------ |
| `/admin/fiscal/dgii/dashboard` | Dashboard fiscal mensual | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/dgii/606`       | Generador Formato 606    | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/dgii/607`       | Generador Formato 607    | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/dgii/608`       | Generador Formato 608    | 🔴 0%   | 🔴 0% | 5 SP         |
| `/admin/fiscal/dgii/it1`       | Cálculo IT-1 (ITBIS)     | 🔴 0%   | 🔴 0% | 8 SP         |

#### Fase 4: NCF Management (34 SP)

| Ruta Propuesta                | Funcionalidad             | Backend | UI    | Story Points |
| ----------------------------- | ------------------------- | ------- | ----- | ------------ |
| `/admin/fiscal/ncf-sequences` | Gestión de secuencias NCF | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/invoices`      | Facturas emitidas         | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/invoices/new`  | Emitir nueva factura      | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/fiscal/credit-notes`  | Notas de crédito/débito   | 🔴 0%   | 🔴 0% | 5 SP         |

#### Fase 5: Facturación Electrónica e-CF (55 SP)

| Ruta Propuesta                | Funcionalidad              | Backend | UI    | Story Points |
| ----------------------------- | -------------------------- | ------- | ----- | ------------ |
| `/admin/fiscal/ecf/dashboard` | Dashboard e-CF             | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/ecf/generate`  | Generar e-CF               | 🔴 0%   | 🔴 0% | 21 SP        |
| `/admin/fiscal/ecf/transmit`  | Transmitir a DGII          | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/fiscal/ecf/history`   | Historial de transmisiones | 🔴 0%   | 🔴 0% | 8 SP         |

**Total:** 186 Story Points (~13 sprints)

### 🔥 Impacto Legal de Faltantes

#### Sin Sistema de Gastos

**Consecuencia:** NO se puede generar Formato 606

- ❌ DGII NO recibe reporte de compras
- ❌ NO se puede deducir ITBIS pagado
- ❌ NO se puede deducir ISR sobre gastos
- ❌ OKLA paga impuestos como si no tuviera gastos

**Multa:** RD$3,000 - $15,000 **por mes** + recargos

#### Sin Formato 607

**Consecuencia:** NO se reportan ventas a DGII

- ❌ DGII NO sabe cuánto vendió OKLA
- ❌ ITBIS cobrado no se reporta
- ❌ Auditoría DGII inmediata
- ❌ Posible cierre de operaciones

**Multa:** RD$5,000 - $20,000 **por mes** + auditoría forzada

#### Sin e-CF (Facturación Electrónica)

**Obligatorio desde:** Enero 1, 2025 (RD$50M+ facturación anual)

**Consecuencia:**

- ❌ Todas las facturas son INVÁLIDAS
- ❌ NO se puede facturar legalmente
- ❌ DGII puede cerrar el negocio
- ❌ Clientes no pueden deducir compras

**Multa:** RD$100,000 - $500,000 + cierre temporal

#### Riesgo Financiero Acumulado

| Período | Multas 606 | Multas 607 | e-CF  | Total Mensual |
| ------- | ---------- | ---------- | ----- | ------------- |
| Mes 1   | $10K       | $15K       | $0    | $25K          |
| Mes 2   | $10K       | $15K       | $0    | $25K          |
| Mes 3   | $10K       | $15K       | $100K | $125K         |
| Q1      | $30K       | $45K       | $100K | $175K         |
| Año     | $120K      | $180K      | $500K | **$800K**     |

**Riesgo Total 1er Año:** RD$800,000 en multas + posible cierre

### 📅 Calendario Fiscal DGII

| Reporte | Frecuencia  | Plazo          | Sin Sistema                    |
| ------- | ----------- | -------------- | ------------------------------ |
| 606     | Mensual     | Día 10 del mes | ❌ IMPOSIBLE GENERAR           |
| 607     | Mensual     | Día 10 del mes | ❌ IMPOSIBLE GENERAR           |
| 608     | Mensual     | Día 10 del mes | ❌ IMPOSIBLE GENERAR           |
| IT-1    | Mensual     | Día 10 del mes | ❌ MANUAL (propenso a errores) |
| IR-17   | Mensual     | Día 10 del mes | ❌ IMPOSIBLE GENERAR           |
| e-CF    | Tiempo real | < 5 segundos   | ❌ NO IMPLEMENTADO             |

**Resultado:** OKLA NO PUEDE cumplir con ningún plazo fiscal

### 🎯 Plan de Implementación Urgente

#### Sprint 1-2: Sistema de Gastos (34 SP) 🔴 CRÍTICO

**Backend:**

- ExpenseService completo (CRUD)
- ExpenseProvider entity + repository
- ExpenseDocument entity + S3 upload
- Approval workflow

**Frontend:**

- ExpensesListPage (tabla + filtros)
- ExpenseFormPage (formulario completo)
- ExpenseDetailPage (detalle + docs)
- ApprovalPage (aprobación de gastos)

**Entregable:** Contador puede registrar gastos del mes

#### Sprint 3-4: Generación 606 (21 SP) 🔴 CRÍTICA

**Backend:**

- Format606GeneratorService
- Validador de estructura DGII
- Export a TXT

**Frontend:**

- DGII606Page (preview + generate + download)
- Dashboard fiscal mensual

**Entregable:** Archivo 606 generado automáticamente

#### Sprint 5-6: Generación 607/608 (21 SP) 🔴 CRÍTICA

**Backend:**

- Format607GeneratorService (desde BillingService)
- Format608GeneratorService (anulaciones)

**Frontend:**

- DGII607Page + DGII608Page

**Entregable:** Archivos 607/608 listos para DGII

#### Sprint 7-9: NCF Management (34 SP) 🔴 ALTA

**Backend:**

- NCFSequence entity + generator
- NCFIssued tracking
- NCFReceived validation

**Frontend:**

- NCFSequencesPage
- InvoicesListPage
- InvoiceFormPage
- CreditNotesPage

**Entregable:** Sistema completo de NCF tradicional

#### Sprint 10-13: e-CF (Facturación Electrónica) (55 SP) 🔴 CRÍTICA

**Backend:**

- ECFService completo
- Firmado digital (certificado DGII)
- Transmisión a ecf.dgii.gov.do
- Webhook de respuestas DGII

**Frontend:**

- ECFDashboardPage
- ECFGeneratePage
- ECFTransmitPage
- ECFHistoryPage

**Entregable:** Facturación electrónica funcional

#### Sprint 14: IT-1 + IR-17 (21 SP) 🟡 MEDIA

**Backend:**

- ITBISCalculatorService
- IR17GeneratorService

**Frontend:**

- IT1CalculatorPage
- IR17Page

**Entregable:** Cálculos fiscales automáticos

**Total:** 186 SP = ~14 sprints = **3.5 meses** con 1 dev full-time

### ⚠️ ALERTA CRÍTICA

**DGII Automatización es el COMPLIANCE BLOCKER MÁS GRANDE del proyecto.**

**Sin este sistema:**

- ❌ OKLA NO PUEDE operar legalmente en República Dominicana
- ❌ OKLA NO PUEDE facturar a clientes corporativos (necesitan NCF válido)
- ❌ OKLA NO PUEDE cumplir con obligaciones fiscales mensuales
- ❌ OKLA acumula multas de RD$25K - $50K **POR MES**
- ❌ Riesgo de cierre por DGII después de 3 meses

**Recomendación:**

- 🔴 **NO LANZAR sin sistema de gastos + 606 + 607** (Sprints 1-6)
- 🔴 **Implementar e-CF INMEDIATAMENTE** si facturación > RD$50M anual
- 🔴 **Contratar contador familiarizado con DGII** durante desarrollo
- 🔴 **Obtener certificado digital DGII** antes de Sprint 10
- 🔴 **Plan B temporal:** Contador externo con software DGII mientras se desarrolla
- 🟡 Fase 1-4 son OBLIGATORIAS para operación legal
- 🟡 Fase 5 (e-CF) obligatoria si facturación anual > RD$50M

### 📊 Comparación con Otros Blockers

| Blocker                         | Backend | Frontend | Total SP | Impacto Legal      | Prioridad  |
| ------------------------------- | ------- | -------- | -------- | ------------------ | ---------- |
| **DGII Automatización** 🆕      | 0%      | 0%       | 186 SP   | Cierre por DGII    | 🔴 MAX     |
| **INTRANT Vehicular**           | 50%     | 0%       | 60 SP    | Vehículos robados  | 🔴 CRÍTICA |
| **Pro Consumidor**              | 40%     | 35%      | 66 SP    | Cierre temporal    | 🔴 CRÍTICA |
| **AML/PLD**                     | 80%     | 40%      | 44 SP    | Multa + Reputación | 🟡 ALTA    |
| **Ley 126-02 (Comercio Elec.)** | 70%     | 80%      | 37 SP    | Multa menor        | ✅ BUENO   |

**DGII Automatización es 3x más grande que el segundo blocker.**

### 📚 Referencias

- **Documento matriz:** `process-matrix/08-COMPLIANCE-LEGAL-RD/12-AUTOMATIZACION-REPORTES-DGII.md`
- **Portal DGII:** dgii.gov.do
- **e-CF DGII:** ecf.dgii.gov.do
- **Oficina Virtual:** oficinavirtual.dgii.gov.do
- **Formatos fiscales:** dgii.gov.do/serviciosEnLinea/formatos
- **Calendario fiscal:** dgii.gov.do/calendarioContribuyente
- **RNC OKLA:** 1-33-32590-1

---

## 19. Libros Contables y Automatización para Auditoría 🆕

> **Referencia:** `process-matrix/08-COMPLIANCE-LEGAL-RD/15-LIBROS-CONTABLES-AUTOMATIZACION.md`  
> **AccountingService:** NO EXISTE  
> **Estado Backend:** 🔴 0% Implementado  
> **Estado Frontend:** 🔴 0% Implementado  
> **Requisito Legal:** Código Tributario (Ley 11-92 Art. 294-300)

### 📋 Resumen de Cumplimiento

| Libro Contable                 | Backend | Frontend UI | Gap    | Prioridad  |
| ------------------------------ | ------- | ----------- | ------ | ---------- |
| **Libro Diario**               | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Libro Mayor**                | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Libro de Inventarios**       | 🔴 0%   | 🔴 0%       | **0%** | 🟡 ALTA    |
| **Libro de Compras**           | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Libro de Ventas**            | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Libro de Retenciones**       | 🔴 0%   | 🔴 0%       | **0%** | 🔴 ALTA    |
| **Libro de Banco**             | 🔴 0%   | 🔴 0%       | **0%** | 🟡 ALTA    |
| **Balance de Comprobación**    | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Estado de Resultados**       | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Balance General**            | 🔴 0%   | 🔴 0%       | **0%** | 🔴 CRÍTICA |
| **Paquete Auditoría (1 clic)** | 🔴 0%   | 🔴 0%       | **0%** | 🔴 MÁXIMA  |

**Cobertura Global:** Backend 0% | Frontend 0% | Gap 0% = **🔴 CRÍTICO**

### 🚨 SISTEMA COMPLETO NO EXISTE

**Hallazgo Principal:** A pesar de la especificación detallada (1,635 líneas), **NO existe ninguna implementación**:

- ❌ **AccountingService** NO EXISTE (puerto 5028)
- ❌ **Tablas de base de datos** NO EXISTEN (0 de 10 tablas)
- ❌ **Plan de cuentas** NO EXISTE
- ❌ **Generadores de reportes** NO EXISTEN
- ❌ **Asientos contables automáticos** NO EXISTEN
- ❌ **Integración con e-CF** NO EXISTE

**Consecuencia:** OKLA NO PUEDE responder a una auditoría DGII

### 📊 Arquitectura Requerida (0% implementado)

#### 1. Base de Datos Contable (0 de 10 tablas)

```sql
-- ❌ NINGUNA TABLA EXISTE

chart_of_accounts            -- ❌ NO EXISTE (Plan de cuentas DGII)
journal_entries              -- ❌ NO EXISTE (Libro Diario)
journal_entry_lines          -- ❌ NO EXISTE (Líneas de asientos)
general_ledger               -- ❌ NO EXISTE (Libro Mayor - Vista materializada)
purchase_ledger              -- ❌ NO EXISTE (Libro de Compras → F606)
sales_ledger                 -- ❌ NO EXISTE (Libro de Ventas → F607)
withholding_ledger           -- ❌ NO EXISTE (Libro de Retenciones → IR-17)
bank_ledger                  -- ❌ NO EXISTE (Libro de Banco)
accounting_periods           -- ❌ NO EXISTE (Períodos contables)
trial_balance                -- ❌ NO EXISTE (Balance de Comprobación)
```

**Estado:** Base de datos 100% limpia - Ningún libro contable existe

#### 2. AccountingService Backend (0% implementado)

```
AccountingService/
├── Domain/
│   ├── Entities/
│   │   ├── JournalEntry.cs              // ❌ NO EXISTE
│   │   ├── JournalEntryLine.cs          // ❌ NO EXISTE
│   │   ├── ChartOfAccount.cs            // ❌ NO EXISTE
│   │   ├── PurchaseLedger.cs            // ❌ NO EXISTE
│   │   ├── SalesLedger.cs               // ❌ NO EXISTE
│   │   ├── WithholdingLedger.cs         // ❌ NO EXISTE
│   │   └── BankLedger.cs                // ❌ NO EXISTE
│   └── Enums/
│       ├── AccountType.cs               // ❌ NO EXISTE (Asset, Liability, etc.)
│       └── TransactionType.cs           // ❌ NO EXISTE
├── Application/
│   ├── Services/
│   │   ├── JournalEntryService.cs       // ❌ NO EXISTE - Crear asientos
│   │   ├── LedgerService.cs             // ❌ NO EXISTE - Libros auxiliares
│   │   ├── ReportGeneratorService.cs    // ❌ NO EXISTE - Excel/PDF
│   │   ├── AuditPackageService.cs       // ❌ NO EXISTE - Paquete 1 clic
│   │   └── TrialBalanceService.cs       // ❌ NO EXISTE - Balance de comprobación
│   └── EventHandlers/
│       ├── PaymentCompletedHandler.cs   // ❌ NO EXISTE - e-CF → asiento
│       ├── ExpenseRegisteredHandler.cs  // ❌ NO EXISTE - Gasto → asiento
│       └── PayrollProcessedHandler.cs   // ❌ NO EXISTE - Nómina → asiento
├── Infrastructure/
│   ├── Persistence/
│   │   ├── AccountingDbContext.cs       // ❌ NO EXISTE
│   │   └── Repositories/                // ❌ NO EXISTE
│   └── ReportGenerators/
│       ├── ExcelReportGenerator.cs      // ❌ NO EXISTE - EPPlus
│       └── PdfReportGenerator.cs        // ❌ NO EXISTE - iTextSharp
└── Api/
    ├── Controllers/
    │   ├── JournalEntryController.cs    // ❌ NO EXISTE
    │   ├── AccountingReportsController.cs // ❌ NO EXISTE
    │   └── AuditPackageController.cs    // ❌ NO EXISTE
    └── Jobs/
        └── PeriodCloseJob.cs            // ❌ NO EXISTE - Cierre mensual
```

#### 3. Frontend Admin Pages (0% implementado)

| Ruta Propuesta                     | Funcionalidad                   | Backend | UI    | Story Points |
| ---------------------------------- | ------------------------------- | ------- | ----- | ------------ |
| `/admin/accounting/dashboard`      | Dashboard contable principal    | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/accounting/journal`        | Libro Diario (ver/crear)        | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/accounting/ledger`         | Libro Mayor por cuenta          | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/accounting/trial-balance`  | Balance de Comprobación         | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/accounting/chart-accounts` | Plan de cuentas (CRUD)          | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/accounting/purchases`      | Libro de Compras                | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/accounting/sales`          | Libro de Ventas                 | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/accounting/bank`           | Libro de Banco + Conciliación   | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/accounting/reports`        | Generador de reportes           | 🔴 0%   | 🔴 0% | 13 SP        |
| `/admin/accounting/audit-package`  | **Paquete Auditoría (1 clic)**  | 🔴 0%   | 🔴 0% | **21 SP**    |
| `/admin/accounting/income`         | Estado de Resultados            | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/accounting/balance-sheet`  | Balance General                 | 🔴 0%   | 🔴 0% | 8 SP         |
| `/admin/accounting/periods`        | Períodos contables (open/close) | 🔴 0%   | 🔴 0% | 5 SP         |

**Total:** 139 Story Points (~10 sprints = 2.5 meses)

### 🔥 Impacto Legal de Faltantes

#### Escenario: Auditoría DGII

**Situación típica:**

```
DGII: "Necesitamos revisar sus libros contables de 2026."
OKLA (sin sistema): "Tenemos que buscar facturas, copiar a Excel,
                     calcular totales manualmente..."
DGII: "Tienen 5 días hábiles para entregar."
OKLA: "😰 ¡Es imposible!"
```

**Tiempo actual para responder a auditoría:**

- Manual (sin sistema): **40-80 horas** (1-2 semanas de trabajo completo)
  - Buscar todas las facturas emitidas y recibidas
  - Copiar manualmente a Excel
  - Calcular totales
  - Cuadrar débitos vs créditos
  - Generar cada libro (7 libros × 8 horas = 56 horas)
  - Alto riesgo de errores matemáticos
  - Formato no cumple estándares DGII

**Con sistema automatizado:**

- **< 1 hora** (tiempo real: 5-10 minutos)
  - Admin accede a `/admin/accounting/audit-package`
  - Selecciona período (Ene-Dic 2026)
  - Click "Generar Paquete"
  - Descarga ZIP con 10 libros contables en Excel + PDF
  - Todos los reportes cuadrados y validados
  - Formato estándar DGII

**Ahorro:** 95-98% del tiempo

#### Sin Libros Contables

**Multas:**

- Libros no legalizados: RD$10,000 - $50,000
- No presentar libros en auditoría: RD$50,000 - $200,000
- Información incompleta: RD$20,000 - $100,000
- Desacato a requerimiento: RD$100,000 - $500,000 + cierre temporal

**Consecuencias:**

1. **DGII descalifica información fiscal**
   - No acepta deducciones de gastos
   - No acepta créditos fiscales ITBIS
   - Recalcula impuestos a pagar

2. **Ajuste fiscal retroactivo**
   - DGII impone valores fiscales arbitrarios
   - +30%-50% más impuestos
   - Intereses moratorios desde 2026

3. **Cierre temporal del negocio**
   - Suspensión de RNC por 30-90 días
   - No se puede facturar durante suspensión
   - Pérdida total de ingresos

**Riesgo Total 1er Año:** RD$400,000 - $1,200,000 ($6,600 - $20,000 USD)

### 🎯 Integración con Otros Sistemas

**Flujo de Datos Automático:**

```
┌─────────────────────────────────────────────────────────────────────────┐
│          INTEGRACIÓN: TRANSACCIÓN → LIBRO CONTABLE                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1️⃣ VENTA (BillingService)                                              │
│     └── PaymentCompletedEvent → AccountingService                      │
│         └── Asiento automático:                                        │
│             Débito: Bancos $34.22                                       │
│             Crédito: Ingresos $29.00                                    │
│             Crédito: ITBIS por Pagar $5.22                             │
│         └── Registra en Libro Diario + Libro de Ventas                │
│                                                                         │
│  2️⃣ GASTO (ExpenseService)                                              │
│     └── ExpenseRegisteredEvent → AccountingService                     │
│         └── Asiento automático:                                        │
│             Débito: Gastos Hosting $100                                 │
│             Débito: ITBIS Pagado $18                                    │
│             Crédito: Bancos $118                                        │
│         └── Registra en Libro Diario + Libro de Compras               │
│                                                                         │
│  3️⃣ e-CF EMITIDO (ECFService)                                           │
│     └── ECFApprovedEvent → AccountingService                           │
│         └── Registra en Libro de Ventas con e-CF number               │
│         └── Actualiza sales_ledger (para F607 automático)             │
│                                                                         │
│  4️⃣ FIN DE MES (Job Automático)                                         │
│     └── PeriodCloseJob ejecuta:                                        │
│         ├── Genera Balance de Comprobación                            │
│         ├── Genera Estado de Resultados                               │
│         ├── Genera Balance General                                    │
│         └── Cierra período contable (status = CLOSED)                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 📊 Plan de Cuentas DGII (NO IMPLEMENTADO)

El documento especifica un catálogo completo con 60+ cuentas:

**Estructura:**

- **1. ACTIVOS** (1.1 Corrientes, 1.2 No Corrientes)
  - 1.1.01 Caja
  - 1.1.02 Bancos (Banco Popular, BHD León)
  - 1.1.04 ITBIS Pagado (Crédito Fiscal)
  - 1.2.01 Equipos de Computación
  - 1.2.04 Activos Intangibles

- **2. PASIVOS** (2.1 Corrientes, 2.2 No Corrientes)
  - 2.1.02 ITBIS por Pagar
  - 2.1.03 ISR por Pagar (Retenciones)
  - 2.1.07 Ingresos Diferidos (Suscripciones)

- **3. PATRIMONIO**
  - 3.1.01 Capital Social
  - 3.1.04 Utilidad del Ejercicio

- **4. INGRESOS**
  - 4.1.01 Ingresos por Listings
  - 4.1.02 Ingresos por Suscripciones Dealers
  - 4.1.03 Ingresos por Boosts

- **5. COSTOS Y GASTOS**
  - 5.1.01 Gastos de Personal
  - 5.1.02 Gastos de Hosting/Servidores
  - 5.1.03 Gastos de Publicidad
  - 5.1.09 Comisiones Pasarelas de Pago

**Estado:** ❌ Catálogo NO EXISTE en base de datos

### 🎯 Story Points Específicos

**Backend AccountingService:** 89 SP

- Domain entities + enums: 13 SP
- JournalEntryService (asientos automáticos): 21 SP
- LedgerService (libros auxiliares): 13 SP
- ReportGeneratorService (Excel/PDF): 21 SP
- AuditPackageService (ZIP con todo): 13 SP
- Event handlers (Payment, Expense, Payroll): 8 SP

**Base de Datos:** 21 SP

- Schema design (10 tablas): 8 SP
- Migraciones: 5 SP
- Stored procedures/functions: 5 SP
- Datos iniciales (plan de cuentas): 3 SP

**Frontend Admin Pages:** 139 SP

- Dashboard contable: 13 SP
- Libro Diario (view + create): 13 SP
- Libro Mayor: 13 SP
- Balance de Comprobación: 8 SP
- Plan de cuentas (CRUD): 8 SP
- Libros auxiliares (3 páginas): 24 SP (8×3)
- Reportes financieros (3 páginas): 24 SP (8×3)
- **Paquete Auditoría (1 clic):** 21 SP ⭐
- Generador de reportes: 13 SP
- Períodos contables: 5 SP

**Integración:** 13 SP

- Event handlers BillingService: 3 SP
- Event handlers ExpenseService: 3 SP
- Event handlers ECFService: 3 SP
- Job de cierre mensual: 4 SP

**Testing:** 21 SP

- Unit tests (asientos, cuadre): 8 SP
- Integration tests: 8 SP
- E2E tests (flujo completo): 5 SP

**Total Libros Contables:** **283 Story Points** (~20 sprints = 5 meses)

### 📋 Funcionalidad Estrella: Paquete Auditoría (1 Clic)

**El feature más valioso del sistema:**

```
Usuario: Admin contador
Acción: Click "📦 Generar Paquete Auditoría"
Input: Período (Ene 2026 - Dic 2026)
Output: ZIP con 50+ archivos Excel/PDF
Tiempo: < 5 minutos
Valor: Ahorra 40-80 horas de trabajo manual
```

**Contenido del ZIP:**

- 12 × Libro Diario (uno por mes)
- 12 × Libro Mayor (uno por mes)
- 12 × Libro de Compras (uno por mes)
- 12 × Libro de Ventas (uno por mes)
- 12 × Balance de Comprobación (uno por mes)
- 1 × Estado de Resultados anual
- 1 × Balance General al cierre
- 1 × Índice de documentos
- 1 × Carta de entrega (template Word)

**Total:** ~49 archivos en 1 ZIP

**Diferenciador competitivo:**

- Competidores: 1-2 semanas para preparar auditoría
- OKLA: < 1 hora con 1 clic

### 💡 Comparación con Otros Sistemas

| Sistema                     | Story Points | Prioridad  | Riesgo Legal           | Estado |
| --------------------------- | ------------ | ---------- | ---------------------- | ------ |
| **Libros Contables**        | **283 SP**   | 🔴 MÁXIMA  | $20K/año en auditorías | 🔴 0%  |
| e-CF Electrónicos           | 155 SP       | 🔴 CRÍTICA | $540K/año si >RD$50M   | 🔴 0%  |
| Gastos Operativos           | 105 SP       | 🔴 CRÍTICA | $200K/año sin F606     | 🔴 5%  |
| DGII Formatos (606/607/608) | 94 SP        | 🔴 CRÍTICA | $300K/año sin formatos | 🔴 4%  |
| Pro Consumidor              | 66 SP        | 🔴 CRÍTICA | $100K/año + cierre     | 🔴 35% |
| INTRANT Vehicular           | 60 SP        | 🔴 CRÍTICA | $200K/año veh. robados | 🟡 50% |

**Libros Contables es el sistema MÁS GRANDE de todos los compliance systems.**

**Por qué:**

1. **Alcance más amplio** - Registra TODAS las transacciones
2. **Integración compleja** - Conecta con 5+ microservicios
3. **Auditoría DGII** - Requerimiento inmediato (5 días)
4. **Base para otros sistemas** - F606, F607, IT-1, IR-17 dependen de los libros

### ⚠️ ALERTA MÁXIMA

**OKLA NO PUEDE operar sin libros contables porque:**

1. **Obligación legal** (Código Tributario Art. 294-300)
   - Toda empresa debe llevar libros contables
   - Multa: RD$10K-$50K si no están legalizados
   - Cierre temporal si no se presentan en auditoría

2. **Respuesta a DGII imposible**
   - Auditoría requiere libros en 5 días hábiles
   - Manual: 40-80 horas de trabajo
   - Sin sistema: Alto riesgo de errores + multas

3. **Dependencia de otros sistemas**
   - Sin Libro de Compras → No se puede generar F606
   - Sin Libro de Ventas → No se puede generar F607
   - Sin asientos → No se puede calcular IT-1 (ITBIS)
   - Sin retenciones → No se puede generar IR-17

4. **Toma de decisiones**
   - Sin Estado de Resultados → No se sabe si hay ganancia/pérdida
   - Sin Balance General → No se conoce la situación financiera
   - Sin Libro Mayor → No se puede rastrear gastos

**Prioridad:** Implementar Fase 1 (Base + Asientos) ANTES de lanzamiento

### 📚 Referencias

- **Documento matriz:** `process-matrix/08-COMPLIANCE-LEGAL-RD/15-LIBROS-CONTABLES-AUTOMATIZACION.md` (1,635 líneas)
- **Código Tributario:** Ley 11-92, Art. 294-300 (Libros Contables)
- **DGII Procedimiento:** Circular 06-2015 (Legalización de Libros)

---

## 20. Conclusión

### Estado Global

| Aspecto                            | Cobertura | Observación                               |
| ---------------------------------- | --------- | ----------------------------------------- |
| **Ley 172-13 (Privacidad)**        | ✅ 95%    | EXCELENTE - Implementación completa ARCO  |
| **Ley 126-02 (Comercio Elec.)** 🆕 | ✅ 80%    | BUENO - Solo falta info legal en footer   |
| **Ley 155-17 (AML/PLD)**           | 🟡 40%    | PARCIAL - Falta monitoreo y DDC reforzada |
| **Ley 63-17 (INTRANT Vehicular)**  | 🟡 50%    | CRÍTICO - Backend OK, 0% UI (60 SP)       |
| **DGII Automatización e-CF** 🆕    | 🔴 0%     | CRÍTICO - Sistema NO EXISTE (186 SP)      |
| **Ley 11-92 (DGII)**               | 🔴 30%    | CRÍTICO - Sin generadores 607/606         |
| **Ley 358-05 (Pro Consumidor)** 🆕 | 🔴 35%    | CRÍTICO - Sistema de quejas faltante      |
| **ComplianceService**              | 🔴 5%     | CRÍTICO - ADM-COMP sin dashboard          |

### Recomendación Final

brechas críticas (94 SP):\*\*

**INTRANT Vehicular (BLOCKER PRE-LANZAMIENTO - 21 SP):**

1. 🔴 IntrantBadge component (5 SP)
2. 🔴 VehicleVerifierPage pública (8 SP)
3. 🔴 IntrantSection en VehicleDetailPage (3 SP)
4. 🔴 Integración con proveedor de datos (5 SP)

**Pro Consumidor (BLOCKER - 21 SP):**

9. ✅ ConsumerProtectionController backend (8 SP)
10. ✅ ComplaintsPage + NewComplaintPage (13 SP)

**AML/PLD (Post-lanzamiento - 26 SP):**

11. 🟡 ComplianceDashboardPage (8 SP)
12. 🟡 TransactionMonitoringPage (8 SP)
13. 🟡 AlertsDashboardPage (5 SP)
14. 🟡 DueDiligencePage (5 SP)

**DGII Automatización** es ahora el **MÁXIMO compliance blocker** del proyecto, seguido de INTRANT (Ley 63-17) y Pro Consumidor (Ley 358-05).

**Por qué DGII Automatización es MÁXIMO BLOCKER:**

- ❌ Sin sistema de gastos = NO se puede generar Formato 606
- ❌ Sin Formato 606 = NO se deducen gastos = OKLA paga impuestos como si no tuviera gastos
- ❌ Sin Formato 607 = DGII NO sabe ventas de OKLA = Auditoría inmediata
- ❌ Sin e-CF = Facturas INVÁLIDAS = NO se puede facturar legalmente
- ❌ Multas acumuladas: RD$25K-$50K por mes = RD$800K al año
- ❌ Riesgo de cierre por DGII después de 3 meses

**Por qué INTRANT es BLOCKER pre-lanzamiento:**

- ❌ Sin verificación INTRANT = Vehículos robados pueden publicarse
- ❌ Sin badge INTRANT = Compradores NO confían en la plataforma
- ❌ Sin validación de gravámenes = Ventas fraudulentas
- ❌ Sin consulta de multas = Compradores heredan deudas

**Pro Consumidor (Ley 358-05)** sigue siendo **compliance blocker crítico**.

**Sanciones por incumplimiento:**

**Ley 358-05:**

- Multa 10-100 salarios mínimos (Art. 48)
- Cierre temporal del negocio (Art. 56)
- Reembolso forzado + daños y perjuicios (Art. 45-47)

**DGII (Ley 11-92):**

- No reportar Formato 606: RD$3K - $15K por mes
- No reportar Formato 607: RD$5K - $20K por mes + auditoría
- No usar e-CF (>RD$50M anual): RD$100K - $500K + cierre
- Acumulado anual: RD$800K en multas

**Ley 63-17:**

- Facilitar venta vehículo robado: Responsabilidad criminal
- No verificar propiedad: RD$50K - $200K (Art. 203)
- Ocultar gravámenes: RD$100K - $500K (Art. 215)

**Recomendación de Lanzamiento:**

- 🔴 NO LANZAR sin sistema de quejas implementado (Ley 358-05)
- 🔴 NO LANZAR sin verificación INTRANT básica (Badge + VehicleVerifierPage)
- 🔴 Implementar Fase 1 INTRANT (21 SP) en paralelo con Pro Consumidor
- 🔴 Capacitar equipo de soporte en ambas leyes ANTES del lanzamiento
- 🔴 Firmar convenio con Pro Consumidor (opcional pero recomendado)
- 🔴 Contratar proveedor de datos INTRANT (ConsultData.do o similar

**Esfuerzo:** ~6 semanas con 1 desarrollador full-time (5 sprints)  
**Esfuerzo INTRANT (Fase 1):** 21 SP = 1.5 sprints = **2 semanas**
**Esfuerzo:** ~5 semanas con 1 desarrollador full-time (4 sprints)

### ⚠️ ALERTA LEGAL

**Pro Consumidor (Ley 358-05)** es ahora el **compliance blocker** más crítico del proyecto.

**Sanciones por incumplimiento:**

- Multa 10-100 salarios mínimos (Art. 48)
- Cierre temporal del negocio (Art. 56)
- Reembolso forzado + daños y perjuicios (Art. 45-47)

**Recomendación:**

- 🔴 NO LANZAR sin sistema de quejas implementado
- 🔴 Capacitar equipo de soporte en Ley 358-05 ANTES del lanzamiento
- 🔴 Firmar convenio con Pro Consumidor (opcional pero recomendado)
- ✅ Ley 126-02 (Comercio Electrónico) tiene cumplimiento EXCELENTE (80%)
- ✅ Firma digital NO es blocker para operaciones actuales
- 🔴 **Ley 11-92 (DGII Formatos):** 81 SP CRÍTICO - Gestión de NCF y reportes 606/607/608 obligatorios

---

## 15. Ley 11-92 - Obligaciones Fiscales DGII (Formatos 606/607/608) 🆕

### 🔴 CUMPLIMIENTO CRÍTICO (4%)

**Implementación Crítica:**

- ✅ Generación básica de NCF (20% del sistema)
- 🔴 **Gestión de secuencias NCF** (0% UI - BLOCKER)
- 🔴 **Formato 607 (Ventas)** (0% - Obligatorio día 15 cada mes)
- 🔴 **Formato 606 (Compras)** (0% - Obligatorio día 15 cada mes)
- 🔴 **Formato 608 (Anulaciones)** (0% - Obligatorio día 15 cada mes)
- 🔴 **Dashboard Fiscal Admin** (0% - Sin visibilidad de obligaciones)
- 🔴 **Alertas automáticas** (0% - Sin recordatorios de vencimientos)

**Riesgo Legal CRÍTICO:**

- Sin gestión de NCF: **RD$50,000-$500,000** + cierre temporal
- No presentar 607: **RD$3,000-$15,000/mes** (multa acumulativa)
- No presentar 606: **RD$3,000-$15,000/mes** (multa acumulativa)
- No presentar 608: **RD$2,000-$10,000/mes** (multa acumulativa)
- **Total multas anuales estimadas:** RD$360,000-$1,200,000

**Rutas Faltantes (CRÍTICAS):**

- `/admin/fiscal/ncf-config` → Gestión de secuencias NCF (13 SP)
- `/admin/fiscal/607` → Generador reporte 607 DGII (8 SP)
- `/admin/fiscal/606` → Generador reporte 606 DGII (8 SP)
- `/admin/fiscal/608` → Registro de anulaciones (5 SP)
- `/admin/fiscal/dashboard` → Dashboard fiscal consolidado (8 SP)

**Plan de Implementación URGENTE (Fase 1 - 2 semanas):**

- ✅ Gestión de Secuencias NCF (13 SP) - P0 BLOCKER
- ✅ Dashboard Fiscal Admin (8 SP) - P0 CRÍTICO
- ✅ Generador Formato 607 (8 SP) - P0 CRÍTICO
- ✅ Alertas automáticas (5 SP) - P1 ALTA
- **TOTAL:** 34 SP (2 semanas) - **Mínimo para operar legalmente**

**Conclusión:**

- **BLOCKER:** Sin gestión de NCF, no se pueden emitir facturas legales
- **Obligatorio:** Reportes 606/607/608 son obligación mensual (día 15)
- **Urgencia:** Implementar Fase 1 (34 SP) en próximos 15 días

Ver documentación completa: [45-obligaciones-fiscales-dgii.md](45-obligaciones-fiscales-dgii.md)

---

## 16. Sistema de Registro de Gastos Operativos 🆕

### 🔴 CUMPLIMIENTO CRÍTICO (5%)

**Estado Actual:**

- Backend: FinanceService tiene entidad `Expense` básica (30% funcional)
- Frontend: **0% - NO EXISTE**
- Compliance: **5%** - Solo estructura básica, NO cumple DGII

**Implementación Crítica:**

- ✅ Entidad Expense básica (30% - NO cumple DGII)
- 🔴 **Clasificación de proveedores** (0% - Local vs Internacional)
- 🔴 **Registro con NCF** (0% - No distingue B01/B02/B13)
- 🔴 **Verificación NCF con DGII** (0% - BLOCKER)
- 🔴 **Cálculo retenciones ISR 10%** (0% - CRÍTICO)
- 🔴 **Generación Formato 606** (0% - **BLOCKER FORMATO 606**)
- 🔴 **Calendario fiscal** (0% - Sin alertas día 3, 8, 13, 18)
- 🔴 **Dashboard de gastos** (0% UI)

**Riesgo Legal CRÍTICO:**

- Sin Formato 606: **RD$3,000-$15,000/mes** (multa acumulativa)
- No retener ISR 10%: **RD$5,000-$50,000** + intereses
- No tener documentos: **RD$1,000-$10,000** por gasto sin soporte
- **Total multas anuales estimadas:** RD$144,000-$600,000

**Impacto Financiero:**

- Sin deducción de ITBIS: Pérdida de ~$13,000-$20,000 DOP/mes (~$240,000 DOP/año)
- Sin evidencia de gastos: DGII puede desconocer hasta 50% de gastos en auditoría
- Riesgo de ajuste fiscal: +30% ISR sobre utilidades

**Proveedores de OKLA (Catálogo Real):**

**Internacionales (27 proveedores):**

- Hosting/Cloud: Digital Ocean, AWS, Google Cloud, Cloudflare ($200/mes)
- Desarrollo: GitHub, JetBrains, Postman ($50/mes)
- Pasarelas: Stripe, PayPal (~$300/mes comisiones)
- Publicidad: Google Ads, Facebook Ads, TikTok Ads ($1,200/mes)
- IA/ML: OpenAI, Anthropic, Google AI ($180/mes)
- Software: Microsoft 365, Adobe CC, Figma ($100/mes)
- **Total Internacional:** ~$2,100 USD/mes (~$126,000 DOP/mes)

**Locales (12 proveedores):**

- AZUL Banco Popular: ~$8,000 DOP/mes
- Claro/Altice: ~$5,500 DOP/mes
- Contador: $15,000 DOP/mes (retención 10%)
- Abogado: $10,000 DOP/mes (retención 10%)
- Alquiler oficina: $25,000 DOP/mes (retención 10%)
- Edenorte/Edesur: $2,500 DOP/mes (exento ITBIS)
- **Total Local:** ~$73,300 DOP/mes

**Total Gastos OKLA:** ~$199,300 DOP/mes (~$2.4M DOP/año)

**Rutas Faltantes (CRÍTICAS):**

- `/admin/expenses` → Dashboard de gastos (13 SP)
- `/admin/expenses/register` → Formulario registro (21 SP)
- `/admin/expenses/approval` → Aprobación contador (8 SP)
- `/admin/expenses/providers` → Catálogo proveedores (5 SP)
- `/admin/expenses/606` → Generador Formato 606 (13 SP)
- `/admin/expenses/calendar` → Calendario fiscal (5 SP)

**Plan de Implementación URGENTE (Fase 1-3 - 6-8 semanas):**

- ✅ Fase 1: Base de registro (34 SP) - P0 BLOCKER
- ✅ Fase 2: Verificación NCF + Retenciones (21 SP) - P0 CRÍTICO
- ✅ Fase 3: Formato 606 + Calendario (26 SP) - P0 CRÍTICO
- ✅ Fase 4: Reportes y Analytics (13 SP) - P1 ALTA
- ✅ Integration & Testing (11 SP)
- **TOTAL:** 105 SP (6-8 semanas) - **$14,700 USD**

**Conclusión:**

- **BLOCKER:** Sin registro de gastos, no se puede generar Formato 606
- **Obligatorio:** Formato 606 es obligación mensual DGII (día 15)
- **Crítico:** Sin deducción ITBIS, pérdida de $240K DOP/año
- **Urgencia:** Implementar Fase 1-3 (81 SP) en próximas 6 semanas

Ver documentación completa: [46-registro-gastos-operativos.md](46-registro-gastos-operativos.md)

---

## 14. Ley 126-02 - Comercio Electrónico 🆕

### ✅ CUMPLIMIENTO EXCELENTE (80%)

**Implementación Completa:**

- ✅ Información del proveedor en footer (OklaFooter.tsx - 346 líneas)
- ✅ Términos y condiciones completos (TermsPage.tsx - 223 líneas)
- ✅ Política de privacidad (PrivacyPage.tsx - integrada con Ley 172-13)
- ✅ Confirmación de transacciones por email (NotificationService)
- ✅ Aceptación de términos en registro (checkbox obligatorio)
- ✅ Recibo digital PDF con NCF (InvoicingService)

**Gaps Menores (3 SP):**

- 🟡 Agregar RNC visible en footer (1 SP)
- 🟡 Dirección física completa en footer (1 SP)
- 🟡 Registro mercantil en AboutPage (1 SP)

**Firma Digital (26 SP - Prioridad MEDIA):**

- 🟡 Backend: DocumentSigningController (13 SP)
- 🟡 Frontend: ContractSigningPage + DocumentVerifyPage (13 SP)
- ℹ️ **NO es blocker** - Click-wrap de términos es válido legalmente
- ℹ️ **RECOMENDADO** para contratos de alto valor (>$50K)

**Total:** 37 Story Points (29 SP opcionales)

Ver documentación completa: [44-comercio-electronico.md](44-comercio-electronico.md)

---

_Auditoría realizada por Gregory Moreno_  
_Fecha inicial: Enero 8, 2026 (Ley 155-17, 172-13, 11-92, 358-05)_  
_Última actualización: Enero 29, 2026 (agregado Norma 06-2018 e-CF + Ley 126-02 + Ley 63-17 INTRANT + UI operacional DGII + Registro Gastos Operativos + Automatización Reportes DGII)_  
_Total Story Points: **704 SP** (44 AML + 4 Privacidad + 21 DGII Tax + 94 DGII Formatos + **155 e-CF** + 37 Comercio Elec + 66 Pro Consumidor + 105 Gastos Operativos + **94 Automatización DGII** + 60 INTRANT + otros)_

---

## 17. 🤖 Sistema de Automatización de Reportes DGII

**Documento:** `47-automatizacion-reportes-dgii.md`  
**Ley Base:** Ley 11-92 (Código Tributario)  
**Normas DGII:** Norma 01-07 (Formato 606), Norma 02-05 (Formato 607)  
**Obligaciones:** Formatos 606/607/608 mensuales, IR-17, IT-1

### 📊 Análisis de Cobertura

| Área                            | Backend | Frontend | Brecha  | Prioridad              |
| ------------------------------- | ------- | -------- | ------- | ---------------------- |
| **SchedulerService (Jobs)**     | 🟡 15%  | 🔴 0%    | -15%    | 🔴 **CRÍTICO**         |
| **ReportingService (Formatos)** | 🟡 10%  | 🔴 5%    | -5%     | 🔴 **CRÍTICO**         |
| **DGIIService**                 | 🔴 0%   | 🔴 0%    | 0%      | 🔴 **BLOCKER**         |
| **Dashboard Fiscal**            | 🔴 0%   | 🔴 0%    | 0%      | 🔴 **CRÍTICO**         |
| **Verificación NCF**            | 🔴 0%   | 🔴 0%    | 0%      | 🟠 **ALTO**            |
| **PROMEDIO TOTAL**              | **8%**  | **2%**   | **-6%** | **🔴 CRÍTICO (94 SP)** |

### 🚨 Problemas Críticos

**Sin automatización:**

- ⏰ **12 horas/mes** del contador en Excel manual
- 📅 **Alto riesgo** de perder deadlines (día 10, 15, 20)
- 💰 **Multas potenciales:** RD$3K-$15K por mes si reportes tarde/missing
- ❌ **Errores manuales:** $300/año promedio en correcciones

**Backend faltante (0-15%):**

- ❌ **DGIIService NO EXISTE** (microservicio completo - 0%)
- ❌ **0 generadores** de formatos (606/607/608/IR-17/IT-1)
- ❌ **6 jobs automáticos** NO EXISTEN en SchedulerService:
  - IR-17 reminder (día 8 @ 9:00 AM)
  - Formato 606 preview (día 10 @ 8:00 AM)
  - Formats reminder (día 13 @ 9:00 AM)
  - IT-1 reminder (día 18 @ 9:00 AM)
  - NCF sequence check (diario @ 7:00 AM)
  - Fiscal backup (domingos @ 2:00 AM)
- ✅ Hangfire configurado con PostgreSQL (infrastructure OK 15%)
- ✅ 3 jobs básicos existen: CleanupOldExecutionsJob, DailyStatsReportJob, HealthCheckJob
- ❌ ReportingService tiene solo tracking (DGIISubmission entity), NO generation (10%)

**Frontend faltante (0-5%):**

- ✅ Hook `useDGIIReports()` existe en useInvoices.ts (5%)
- ✅ Service `getDGIIReports()` existe en invoicingService.ts
- ❌ **9 páginas** completamente FALTANTES (0%):
  - FiscalDashboard.tsx - Dashboard con alertas
  - Format606Page.tsx - Generador 606
  - Format607Page.tsx - Generador 607
  - Format608Page.tsx - Anulaciones
  - IR17Page.tsx - Retenciones ISR
  - IT1Page.tsx - Calculadora ITBIS
  - NCFMonitor.tsx - Monitor de secuencias
  - FiscalCalendar.tsx - Calendario obligaciones
  - ReportSchedulerPage.tsx - Gestión de jobs
- ❌ **6 componentes** FALTANTES (0%)
- ❌ **95% métodos** de service FALTANTES

### 🎯 8 Requisitos Faltantes

| #                             | Requisito                   | Backend   | Frontend  | Total SP       | Prioridad |
| ----------------------------- | --------------------------- | --------- | --------- | -------------- | --------- |
| 1                             | **Formato 606 Generation**  | 13 SP     | 8 SP      | **21 SP**      | 🔴 P0     |
| 2                             | **Formato 607 Generation**  | 8 SP      | 5 SP      | **13 SP**      | 🔴 P0     |
| 3                             | **Formato 608 Generation**  | 5 SP      | 3 SP      | **8 SP**       | 🟠 P1     |
| 4                             | **IR-17 Report**            | 8 SP      | 5 SP      | **13 SP**      | 🔴 P0     |
| 5                             | **IT-1 Calculation**        | 8 SP      | 5 SP      | **13 SP**      | 🔴 P0     |
| 6                             | **Fiscal Dashboard**        | 13 SP     | 13 SP     | **26 SP**      | 🔴 P0     |
| 7                             | **Automated Jobs (6 jobs)** | 13 SP     | 8 SP      | **21 SP**      | 🔴 P0     |
| 8                             | **NCF Verification**        | 5 SP      | 3 SP      | **8 SP**       | 🟠 P1     |
| **TOTAL AUTOMATIZACIÓN DGII** | **73 SP**                   | **21 SP** | **94 SP** | **🔴 CRÍTICO** |

### 💡 Análisis de Valor

**Sin automatización:**

- 📊 **12h/mes** contador en Excel → Costo: $1,200/año
- 🚨 **Alto riesgo** multas RD$3K-$15K/mes → Costo potencial: $180K/año
- ❌ **Errores manuales** → Correcciones: $300/año
- **Costo Total:** $1,500/año + riesgo multas

**Con automatización:**

- ⚡ **30min/mes** revisar reportes → Costo: $50/año
- ✅ **0% riesgo** multas (recordatorios automáticos)
- ✅ **0% errores** (validación automatizada)
- **Costo Total:** $50/año

**Ahorro:** $2,100/año + eliminación de riesgo de multas

**Inversión:** 94 SP × $140/SP = **$13,160 USD**

**ROI Monetario:** $13,160 / $2,100 = **6.2 años**

**PERO valor principal es:**

- ✅ **Eliminación de riesgo** de multas ($3K-$15K/mes)
- ✅ **95% reducción** de tiempo contador (12h → 0.5h/mes)
- ✅ **0 errores** en formatos
- ✅ **Paz mental** con recordatorios automáticos 6 días antes de cada deadline
- ✅ **Contador puede enfocarse en estrategia** en lugar de Excel manual

### 📅 Plan de Implementación (4 Fases)

**Fase 1: DGIIService Backend (34 SP, 2-3 semanas)**

- Crear microservicio DGIIService completo
- Implementar generadores 606/607/608/IR17/IT1
- Validación de formatos según normas DGII
- Upload de archivos a S3
- Controllers REST API

**Fase 2: Jobs Automatizados (21 SP, 1-2 semanas)**

- 6 jobs con cron schedules en SchedulerService
- Notificaciones por email/SMS
- Logs de ejecución

**Fase 3: Dashboard Frontend (26 SP, 1-2 semanas)**

- FiscalDashboard (alertas + stats + countdown)
- Format606/607Page (preview + generate + validation)
- ReportSchedulerPage (jobs management)
- NCF Monitor con progress bars

**Fase 4: Integration & Testing (13 SP, 1 semana)**

- Unit tests (70% coverage)
- Integration tests
- E2E tests
- Deployment a DOKS

**Total:** 94 SP = **$13,160 USD** | **6-8 semanas** | **2 developers**

### 📊 Métricas de Éxito

**KPIs a monitorear:**

- ✅ **0 deadlines perdidos** (day 10, 15, 20)
- ✅ **< 5 minutos** para generar cualquier formato
- ✅ **100% validación** antes de enviar
- ✅ **0 multas** por reportes tarde
- ✅ **95% reducción** tiempo contador (12h → 0.5h/mes)
- ✅ **6 recordatorios** automáticos funcionando (IR-17 día 8, 606 preview día 10, formats día 13, IT-1 día 18, NCF diario, backup semanal)

**Dependencias críticas:**

- ⚠️ **REQUIERE:** Sistema de Registro de Gastos Operativos (105 SP) - Sin esto, no hay Formato 606
- ⚠️ **REQUIERE:** Generadores Formato 606/607/608 (94 SP DGII Formatos) - Base legal

**Documentación completa:** `docs/frontend-rebuild/04-PAGINAS/47-automatizacion-reportes-dgii.md`

---

## 10. 🔍 SISTEMA DE PREPARACIÓN PARA AUDITORÍA DGII

> **Auditoría #10 - Completada:** Enero 29, 2026  
> **Referencia:** `process-matrix/08-COMPLIANCE-LEGAL-RD/13-PREPARACION-AUDITORIA-DGII.md`  
> **Estado Backend:** 🟡 12% Implementado (MediaService S3 existe, pero no sistema de paquetes)  
> **Estado Frontend:** 🔴 0% Implementado

### 🎯 Objetivo del Sistema

**Responder a fiscalización DGII en menos de 24 horas con 1 solo click.**

Cuando DGII envía un requerimiento de auditoría, OKLA debe entregar:

- ✅ **100% de documentación** organizada y completa
- ✅ **En menos de 24 horas** (vs 3-7 días manual)
- ✅ **Formato profesional** con índice + carta de respuesta
- ✅ **7 categorías** de documentos en ZIP organizado

### 🚨 Compliance Actual: 6% (CRÍTICO)

| Componente                  | Backend | Frontend | Descripción                              | SP    |
| --------------------------- | ------- | -------- | ---------------------------------------- | ----- |
| **AuditPackageService**     | 🔴 0%   | 🔴 0%    | Generación de paquetes ZIP NO EXISTE     | 34 SP |
| **ComplianceReportService** | 🔴 0%   | 🔴 0%    | Score mensual 0-100 NO EXISTE            | 26 SP |
| **ChecklistService**        | 🔴 0%   | 🔴 0%    | 25 checkpoints mensuales NO EXISTE       | 21 SP |
| **ResponseTemplateService** | 🔴 0%   | 🔴 0%    | Cartas formales DGII NO EXISTEN          | 13 SP |
| **MediaService S3**         | ✅ 95%  | 🟡 30%   | Infraestructura OK, falta organización   | -     |
| **AuditService**            | 🟡 12%  | 🔴 0%    | Tracking eventos (NO es para DGII audit) | -     |
| **ComplianceService**       | 🟡 15%  | 🔴 0%    | Dashboard general (NO audit packages)    | -     |

**Backend Overall:** 🟡 **12%** (solo infraestructura S3, NO sistema de auditoría)  
**Frontend Overall:** 🔴 **0%** (6 páginas + 6 componentes + 2 services faltantes)

### 📦 4 Tipos de Inspecciones DGII

| Tipo                       | Duración    | Alcance              | Riesgo      | Frecuencia |
| -------------------------- | ----------- | -------------------- | ----------- | ---------- |
| **Verificación de Oficio** | 1-3 días    | Verificación puntual | 🟢 Bajo     | Común      |
| **Fiscalización Parcial**  | 1-4 semanas | Período específico   | 🟡 Medio    | Ocasional  |
| **Fiscalización Integral** | 1-6 meses   | 3-5 años completos   | 🔴 Alto     | Rara       |
| **Investigación Especial** | Variable    | Por denuncia         | 🔴 Muy Alto | Muy rara   |

**Plazos legales para responder:** 5-15 días hábiles (típicamente)  
**Meta OKLA:** **< 24 horas** con sistema automatizado

### 📁 7 Categorías de Documentos Requeridos

**1. Información General de la Empresa**

- Registro Mercantil (196339PSD)
- Acta Constitutiva
- RNC (1-33-32590-1)
- Estatutos sociales
- Poderes notariales

**2. Declaraciones DGII**

- Formatos 606/607/608 con acuses (12 meses)
- IT-1 (ITBIS mensual) con comprobantes de pago
- IR-17 (Retenciones) con comprobantes
- IR-2 (Anual) si aplica

**3. Facturas Emitidas**

- Todas las facturas B01/B02/B04 con NCF (PDFs)
- Libro de ventas (Excel/PDF)
- Conciliación con Formato 607

**4. Facturas Recibidas**

- Facturas locales con NCF verificados
- Invoices internacionales (DigitalOcean, AWS, Stripe, etc.)
- Libro de compras
- Conciliación con Formato 606

**5. Documentos Bancarios**

- Estados de cuenta (todos los bancos)
- Conciliaciones bancarias mensuales
- Comprobantes de transferencias
- Cheques emitidos

**6. Nómina y TSS**

- Nóminas mensuales
- Comprobantes TSS pagados
- IR-3 (Retenciones empleados)
- Contratos laborales

**7. Libros Contables**

- Libro mayor general
- Balance de comprobación
- Estados financieros
- Conciliaciones contables

### ❌ PROBLEMAS SIN SISTEMA AUTOMATIZADO

**Tiempo de respuesta manual: 3-7 días**

```
Día 1-2: Buscar documentos (16 horas)
   ├── Computadoras locales (4h)
   ├── Emails (3h)
   ├── S3 sin estructura (5h)
   └── Pedir al contador (4h)

Día 3-5: Organizar (20 horas)
   ├── Crear carpetas (2h)
   ├── Renombrar archivos (4h)
   ├── Verificar completitud (8h)
   └── Índice Excel manual (6h)

Día 6-7: Preparar respuesta (8 horas)
   ├── Redactar carta (3h)
   ├── Imprimir/copiar USB (2h)
   ├── Revisar con abogado (2h)
   └── Entregar DGII (1h)

TOTAL: 44 horas × $50/h = $2,200/auditoría
```

**Riesgos:**

- ❌ **Perder plazo legal** → Multas RD$10K-$50K ($170-$850)
- ❌ **Documentos incompletos** → Extensión de auditoría
- ❌ **Mala impresión DGII** → Mayor escrutinio futuro
- ❌ **Desorganización** → Parecer poco profesional

### ✅ SISTEMA AUTOMATIZADO (1 Click)

**Tiempo de respuesta: < 24 horas**

```
Día 1 Mañana: Generar paquete (1 hora)
   ├── Admin selecciona período (5 min)
   ├── Sistema genera ZIP (10-20 min automático)
   ├── Descargar y revisar (15 min)
   └── Carta con template (10 min)

Día 1 Tarde: Entregar DGII (1 hora)
   ├── Imprimir carta (15 min)
   ├── Copiar USB (15 min)
   └── Ir a DGII (30 min)

TOTAL: 2 horas × $50/h = $100/auditoría
```

**Ahorro por auditoría:** $2,200 - $100 = **$2,100**

**Beneficios:**

- ✅ **100% completo** - Sin olvidar documentos
- ✅ **24 horas** vs 7 días manual (21x más rápido)
- ✅ **Profesional** - Índice + carta formal
- ✅ **Paz mental** - Siempre listo para auditoría
- ✅ **Mejor relación DGII** - Respuestas rápidas y completas

### 🏗️ Arquitectura del Sistema

**Backend (55 SP):**

1. **AuditPackageService (34 SP)** - ❌ NO EXISTE
   - GenerateAsync(startDate, endDate, categories[]) → ZIP
   - Descarga archivos de S3 por categoría
   - Organiza en 7 carpetas numeradas
   - Genera índice Excel con resumen
   - Crea ZIP (50-500 MB típico)
   - Upload a s3://okla-media/audit/packages/
   - Registro en BD (audit_packages table)

2. **ComplianceReportService (26 SP)** - ❌ NO EXISTE
   - GenerateMonthlyReportAsync(year, month)
   - Calcula score 0-100:
     - 40% Formatos DGII (606/607/608/IT1/IR17)
     - 40% Documentación (PDFs, NCF verificados)
     - 20% NCF (secuencias activas, mínimo 100 restantes)
   - Genera alertas accionables
   - Guarda en compliance_reports table

3. **ChecklistService (21 SP)** - ❌ NO EXISTE
   - 25 checkpoints mensuales
   - 6 categorías: Documentos, Facturas, Gastos, Bancos, Nómina, Verificación
   - Progress 0-100%
   - Items pendientes con prioridades

4. **ResponseTemplateService (13 SP)** - ❌ NO EXISTE
   - Genera carta formal a DGII
   - Template con datos de empresa
   - Lista de documentos entregados
   - Export a PDF

**Frontend (60 SP):**

1. **AuditPreparationDashboard (21 SP)** - ❌ NO EXISTE
   - `/admin/audit/preparation`
   - 4 cards: Score, Documentos, Paquetes, Alertas
   - Acciones rápidas (3 botones)
   - Paquetes recientes (tabla)

2. **GenerateAuditPackagePage (13 SP)** - ❌ NO EXISTE
   - `/admin/audit/generate-package`
   - Selector de período (startDate, endDate)
   - Selector de categorías (7 checkboxes)
   - Botón "Generar" con progress bar
   - Resultado: documentos, tamaño MB, descarga

3. **ComplianceScorePage (13 SP)** - ❌ NO EXISTE
   - `/admin/audit/compliance-score`
   - Gráfico circular score 0-100
   - Estado formatos DGII (5 cards)
   - Progreso documentación (3 bars)
   - Estado NCF (2 indicators)
   - Alertas con íconos

4. **DocumentChecklistPage (13 SP)** - ❌ NO EXISTE
   - `/admin/audit/checklist`
   - Progress bar general
   - 6 secciones con checkboxes
   - Toggle items (persist en BD)
   - Histórico de compliance

**Componentes (29 SP):**

- ComplianceScoreCircle (8 SP)
- AuditCategorySelector (5 SP)
- GeneratePackageButton (5 SP)
- AlertsList (5 SP)
- DocumentCountCard (3 SP)
- PackageDownloadButton (3 SP)

### 📊 Estructura del Paquete Generado

```
auditoria-okla-202601-202612.zip (125 MB típico)
├── INDICE-DOCUMENTOS.xlsx
├── CARTA-RESPUESTA-DGII.pdf
├── 1-informacion-empresa/
│   ├── registro-mercantil.pdf
│   ├── acta-constitutiva.pdf
│   ├── rnc.pdf
│   └── estatutos.pdf
├── 2-formatos-dgii/
│   ├── 2026-01/
│   │   ├── 606_133325901_202601.txt
│   │   ├── 606_133325901_202601_acuse.pdf
│   │   ├── 607_133325901_202601.txt
│   │   ├── 607_133325901_202601_acuse.pdf
│   │   ├── it1_202601.pdf
│   │   └── ir17_202601.pdf
│   └── ... (12 meses)
├── 3-facturas-emitidas/
│   ├── 2026-01/
│   │   ├── B0100000001.pdf
│   │   └── ... (18 facturas/mes)
│   ├── libro-ventas-2026.xlsx
│   └── conciliacion-607.xlsx
├── 4-facturas-recibidas/
│   ├── locales/
│   │   └── 2026-01/ (15 gastos/mes)
│   ├── internacionales/
│   │   └── 2026-01/ (5 invoices/mes)
│   ├── libro-compras-2026.xlsx
│   └── conciliacion-606.xlsx
├── 5-estados-cuenta/
│   ├── popular/ (12 estados)
│   └── bhd/ (12 estados)
├── 6-nomina/
│   ├── 2026-01/
│   │   ├── nomina-enero.xlsx
│   │   ├── tss-enero.pdf
│   │   └── ir3-enero.pdf
│   └── ... (12 meses)
└── 7-libros-contables/
    ├── libro-mayor-2026.xlsx
    ├── balance-comprobacion-2026.xlsx
    └── estados-financieros-2026.pdf
```

### 💰 Análisis Financiero

**Inversión:**

- Backend: 55 SP × $140 = $7,700
- Frontend: 60 SP × $140 = $8,400
- **Total: 115 SP = $16,100 USD**

**ROI:**

- Ahorro/auditoría: $2,100
- Auditorías/año: 1-2 (típico)
- Ahorro anual: $2,100-$4,200
- Multas evitadas: $170-$850/año
- **Total ahorro: $2,270-$5,050/año**
- **ROI: 3-7 años**

**PERO valor principal es operacional:**

- ✅ Paz mental (siempre listo)
- ✅ Compliance score mensual
- ✅ Prevención de multas
- ✅ Profesionalismo ante DGII
- ✅ Eliminación de riesgo de perder plazo

### 📅 Plan de Implementación (8 Semanas)

**Fase 1: Backend Core (34 SP, 2-3 semanas)**

- AuditPackageService completo
- ComplianceReportService
- Tablas BD (audit_packages, compliance_reports)
- Tests unitarios

**Fase 2: Frontend Dashboard (21 SP, 1-2 semanas)**

- AuditPreparationDashboard
- GenerateAuditPackagePage
- Rutas + servicios TypeScript

**Fase 3: Score & Checklist (39 SP, 2-3 semanas)**

- ComplianceScorePage
- DocumentChecklistPage
- ResponseLetterPage
- Componentes reutilizables

**Fase 4: Testing & Deploy (21 SP, 1-2 semanas)**

- Unit tests (15 tests mínimo)
- Integration tests
- E2E tests
- Deployment DOKS

### ⚠️ DEPENDENCIAS CRÍTICAS

Este sistema **REQUIERE** que estén implementados primero:

1. ✅ **MediaService con S3** (95% OK) - Archivos ya en S3
2. ❌ **Sistema de Gastos Operativos** (0%) - Sin esto, no hay facturas recibidas organizadas
3. ❌ **Generadores Formato 606/607** (0%) - Sin esto, no hay formatos DGII archivados
4. ❌ **Automatización DGII** (0%) - Sin esto, no hay jobs de archivado

**Secuencia recomendada:**

```
1. Gastos Operativos (105 SP)
   ↓
2. Formato 606/607/608 (94 SP)
   ↓
3. Automatización Jobs (94 SP)
   ↓
4. Preparación Auditoría (115 SP) ⭐ ENTONCES SÍ
```

### 📊 Métricas de Éxito

**KPIs a monitorear:**

- ⏱️ Tiempo generación paquete < 20 minutos
- ⏱️ Tiempo respuesta DGII < 24 horas (vs 7 días)
- ✅ 100% documentos incluidos (0 olvidados)
- 📊 Compliance score > 80 promedio mensual
- 📊 12 checklists mensuales completados/año
- 💰 $0 en multas por respuestas tardías

**Documentación completa:** `docs/frontend-rebuild/04-PAGINAS/48-preparacion-auditoria-dgii.md` (4,350 líneas)

---

---

## 🆕 AUDITORÍA: SISTEMA INTEGRAL AUDITORÍA Y CUMPLIMIENTO (FOLDER 25)

> **Referencia:** `process-matrix/25-AUDITORIA-CUMPLIMIENTO/` (12 documentos, ~4,700 líneas)  
> **Fecha de Auditoría:** Enero 29, 2026  
> **Servicios Evaluados:** AuditService, ComplianceService, DataProtectionService, FiscalReportingService

### 📋 Resumen del Marco Documental

El folder `25-AUDITORIA-CUMPLIMIENTO` define el **sistema integral de auditoría y cumplimiento** de OKLA, incluyendo:

| Documento                           | Líneas | Propósito                                     |
| ----------------------------------- | ------ | --------------------------------------------- |
| `00-DATOS-EMPRESA-OKLA.md`          | 193    | Datos de registro mercantil y fiscal de OKLA  |
| `01-RESUMEN-EJECUTIVO.md`           | 301    | Executive summary para auditores              |
| `02-MATRIZ-OBLIGACIONES-LEGALES.md` | 286    | 39 obligaciones legales identificadas         |
| `03-CALENDARIO-FISCAL-REPORTES.md`  | 318    | Calendario fiscal (IR-17, 606/607/608, ITBIS) |
| `04-AUDITORIA-DGII.md`              | 429    | Checklist completo auditoría DGII             |
| `05-AUDITORIA-UAF.md`               | 704    | Análisis UAF/AML (Ley 155-17)                 |
| `06-AUDITORIA-PROTECCION-DATOS.md`  | 388    | Auditoría Ley 172-13 (ARCO)                   |
| `07-AUDITORIA-PROCONSUMIDOR.md`     | 431    | Auditoría Pro Consumidor (Ley 358-05)         |
| `08-REPORTES-AUTOMATIZADOS.md`      | 494    | FiscalReportingService specifications         |
| `09-EVIDENCIAS-CONTROLES.md`        | 400    | Registro de evidencias y controles            |
| `10-MICROSERVICIOS-AUDITORIA.md`    | 684    | Arquitectura 4 microservicios de auditoría    |
| `11-DASHBOARD-AUDITORIA-UI.md`      | 504    | UI specifications para dashboard              |
| **TOTAL**                           | ~4,700 | **12 documentos de especificación**           |

### 🏢 Datos de OKLA S.R.L.

| Campo              | Valor                                                 |
| ------------------ | ----------------------------------------------------- |
| **Nombre Legal**   | OKLA S.R.L.                                           |
| **RNC**            | 1-33-32590-1                                          |
| **Registro Merc.** | 196339PSD                                             |
| **Gerente**        | Nicauris Mateo Alcántara                              |
| **Capital**        | RD$100,000.00                                         |
| **Domicilio**      | Calle Respaldo Anacaona No. 32, Sabana Perdida        |
| **Modelo**         | Marketplace clasificados vehículos (como SuperCarros) |

### 🎯 HALLAZGO CRÍTICO: OKLA Probablemente NO es Sujeto Obligado UAF

El documento `05-AUDITORIA-UAF.md` analiza en detalle que:

> **OKLA es una plataforma de clasificados, NO un dealer de vehículos.**
>
> - OKLA **NO compra ni vende vehículos**
> - OKLA **NO procesa pagos de transacciones vehiculares**
> - OKLA solo cobra por **publicidad y suscripciones**
> - Los **Dealers que usan OKLA SÍ son sujetos obligados**, pero OKLA probablemente NO

**Recomendación:** Confirmar con abogado especializado en LA/FT.

### 📊 Estado de Implementación Backend

| Servicio Especificado      | Puerto | Archivos | Estado           | Cobertura |
| -------------------------- | ------ | -------- | ---------------- | --------- |
| **AuditService**           | 5070   | 91       | ✅ EXISTE        | 🟡 60%    |
| **ComplianceService**      | 5071   | 27       | ✅ EXISTE        | 🟡 55%    |
| **DataProtectionService**  | 5073   | 38       | ✅ EXISTE        | 🟡 65%    |
| **FiscalReportingService** | 5072   | 0        | ❌ **NO EXISTE** | 🔴 **0%** |

**Backend Total:** 3 de 4 servicios existen (**75%**), 1 crítico faltante

#### ✅ AuditService - Implementado (91 archivos)

**Entidades implementadas:**

- `AuditEvent.cs` - Eventos consumidos de RabbitMQ
- `AuditLog.cs` - Logs de auditoría

**Estructura Clean Architecture:**

```
AuditService/
├── AuditService.Api/ (Controller, Program.cs)
├── AuditService.Application/ (Features, DTOs)
├── AuditService.Domain/ (Entities: AuditEvent, AuditLog)
├── AuditService.Infrastructure/ (Persistence, RabbitMQ)
├── AuditService.Tests/
├── Dockerfile
└── prometheus-alerts.yml
```

**Gap:** Falta UI para visualizar eventos, dashboard de auditoría

#### ✅ ComplianceService - Implementado (27 archivos)

**Entidades implementadas (14 entidades):**

```csharp
// Enums
ComplianceStatus, RegulationType, CriticalityLevel, TaskStatus,
FindingType, FindingStatus, RegulatoryReportType, ReportStatus,
ControlType, EvaluationFrequency

// Entities
RegulatoryFramework   // Marcos regulatorios (Ley 155-17, 172-13, etc.)
ComplianceRequirement // Requerimientos específicos
ComplianceControl     // Controles implementados
ControlTest           // Pruebas de control
ComplianceAssessment  // Evaluaciones de cumplimiento
ComplianceFinding     // Hallazgos de auditoría
RemediationAction     // Acciones correctivas
```

**Gap:** Falta UI para gestión de compliance, dashboard de controles

#### ✅ DataProtectionService - Implementado (38 archivos)

**Entidades implementadas:**

```csharp
ARCORequest      // Solicitudes ARCO (Access, Rectification, Cancellation, Opposition)
ARCOAttachment   // Adjuntos de solicitudes
UserConsent      // Consentimientos de usuario
DataChangeLog    // Log de cambios en datos personales
```

**Gap:** Falta UI para gestión ARCO, panel de consentimientos

#### ❌ FiscalReportingService - NO EXISTE (0 archivos)

**Especificado en 08-REPORTES-AUTOMATIZADOS.md (494 líneas):**

| Reporte Requerido | Frecuencia | Destino | Implementación |
| ----------------- | ---------- | ------- | -------------- |
| Formato 606       | Mensual    | DGII    | ❌ 0%          |
| Formato 607       | Mensual    | DGII    | ❌ 0%          |
| Formato 608       | Mensual    | DGII    | ❌ 0%          |
| IT-1 + ITBIS      | Mensual    | DGII    | ❌ 0%          |
| Reportes UAF      | Trimestral | SB      | ❌ 0%          |
| Reportes Privacy  | Anual      | INDOTEL | ❌ 0%          |

**Este servicio es CRÍTICO:** Sin él, no hay generación automática de formatos DGII

### 📊 Estado de Implementación Frontend

| Página Especificada              | Backend | UI    | Estado       |
| -------------------------------- | ------- | ----- | ------------ |
| `/admin/audit/dashboard`         | ✅ 60%  | 🔴 0% | ❌ No existe |
| `/admin/audit/obligations`       | ✅ 55%  | 🔴 0% | ❌ No existe |
| `/admin/audit/evidences`         | ✅ 55%  | 🔴 0% | ❌ No existe |
| `/admin/audit/reports/dgii`      | 🔴 0%   | 🔴 0% | ❌ No existe |
| `/admin/audit/compliance/alerts` | ✅ 55%  | 🔴 0% | ❌ No existe |
| `/admin/audit/compliance/kyc`    | ✅ 55%  | 🔴 0% | ❌ No existe |
| `/admin/audit/compliance/ros`    | ✅ 55%  | 🔴 0% | ❌ No existe |
| `/admin/audit/privacy/arco`      | ✅ 65%  | 🔴 0% | ❌ No existe |
| `/admin/audit/privacy/consents`  | ✅ 65%  | 🔴 0% | ❌ No existe |
| `/admin/audit/calendar`          | 🔴 0%   | 🔴 0% | ❌ No existe |
| `/admin/audit/packages`          | 🔴 0%   | 🔴 0% | ❌ No existe |
| `/admin/audit/score`             | 🔴 0%   | 🔴 0% | ❌ No existe |

**Frontend Total:** 0 de 12 páginas existen (**0%**)

### 📦 Estado de Evidencias (Documento 09)

| Categoría           | Requeridas | Disponibles | Parciales | Faltantes | % Completo |
| ------------------- | ---------- | ----------- | --------- | --------- | ---------- |
| Contables/Fiscales  | 18         | 2           | 5         | 11        | 11%        |
| Legales/Societarias | 12         | 5           | 3         | 4         | 42%        |
| Operacionales       | 15         | 2           | 4         | 9         | 13%        |
| Tecnológicas        | 12         | 2           | 4         | 6         | 17%        |
| RRHH                | 8          | 1           | 2         | 5         | 13%        |
| Compliance          | 7          | 0           | 1         | 6         | 0%         |
| **TOTAL**           | **72**     | **12**      | **19**    | **41**    | **17%**    |

### 📅 Calendario Fiscal (Documento 03)

| Día | Obligación           | Automatizado | Estado    |
| --- | -------------------- | ------------ | --------- |
| 10  | IR-17 Retenciones    | ❌ No        | 🔴 Manual |
| 15  | Formatos 606/607/608 | ❌ No        | 🔴 Manual |
| 20  | IT-1 + ITBIS         | ❌ No        | 🔴 Manual |

**Sistema de alertas especificado pero NO implementado:**

- 15 días antes: Preparación
- 7 días antes: Recordatorio
- 3 días antes: Urgente
- 1 día antes: Crítico

### 🚨 Análisis de Brechas y Story Points

#### ❌ FiscalReportingService - CRÍTICO (96 SP)

**Servicio completo desde cero:**

```
Domain:        12 SP (Entidades, Enums)
Application:   25 SP (Commands, Queries, Handlers, DTOs)
Infrastructure: 20 SP (DbContext, Repositories, DGII Integration)
API:           15 SP (Controllers, HealthChecks, Swagger)
Tests:         10 SP (Unit tests, Integration tests)
Jobs:          14 SP (CRON jobs para generación automática)
```

#### ❌ Frontend Audit Dashboard - CRÍTICO (145 SP)

**12 páginas especificadas en documento 11:**

```
Dashboard Principal:           15 SP
Calendario Obligaciones:       12 SP
Registro Evidencias:           18 SP
Centro Reportes DGII:          20 SP
Panel Alertas Compliance:      15 SP
Gestión KYC/DDC:               18 SP
Reportes Sospechosos (ROS):    12 SP
Solicitudes ARCO:              15 SP
Gestión Consentimientos:       10 SP
Paquetes Auditoría:            15 SP
Compliance Score:              10 SP
Componentes Reutilizables:     5 SP
```

#### 🟡 Completar Servicios Existentes (68 SP)

**AuditService gaps:**

- Handlers para consultas avanzadas: 12 SP
- Dashboard metrics endpoint: 8 SP
- Export to PDF/Excel: 10 SP

**ComplianceService gaps:**

- Calendario integrado: 10 SP
- Scoring automático: 12 SP
- Notificaciones: 8 SP

**DataProtectionService gaps:**

- Workflow ARCO completo: 8 SP

### 💰 Resumen de Story Points - Folder 25

| Componente                           | Story Points | Prioridad |
| ------------------------------------ | ------------ | --------- |
| **FiscalReportingService (nuevo)**   | 96 SP        | 🔴 P0     |
| **Frontend Audit Dashboard (nuevo)** | 145 SP       | 🔴 P0     |
| **Completar AuditService**           | 30 SP        | 🟡 P1     |
| **Completar ComplianceService**      | 30 SP        | 🟡 P1     |
| **Completar DataProtectionService**  | 8 SP         | 🟢 P2     |
| **TOTAL FOLDER 25**                  | **309 SP**   | -         |

### 📊 Impacto Legal de No Implementar

| Riesgo                            | Multa Estimada      | Frecuencia |
| --------------------------------- | ------------------- | ---------- |
| Formatos 606/607/608 tardíos      | RD$3,000-15,000/mes | Mensual    |
| IT-1/ITBIS tardíos                | RD$5,000-25,000/mes | Mensual    |
| Respuesta ARCO > 30 días          | RD$100,000-500,000  | Por caso   |
| Auditoría DGII sin respuesta <24h | RD$50,000-200,000   | Por evento |
| **Total riesgo anual**            | **~RD$500,000+**    | -          |

### 🎯 Plan de Implementación Recomendado

**Fase 1: FiscalReportingService (4 semanas, 96 SP)**

- Semana 1-2: Domain + Application + Infrastructure
- Semana 3: API + Jobs automáticos
- Semana 4: Tests + Deploy

**Fase 2: Frontend Dashboard Core (4 semanas, 85 SP)**

- Semana 5: Dashboard + Calendario + Evidencias
- Semana 6: Centro Reportes DGII + Alertas
- Semana 7: KYC + ROS
- Semana 8: ARCO + Consentimientos

**Fase 3: Completar Servicios (2 semanas, 68 SP)**

- Semana 9: AuditService + ComplianceService
- Semana 10: DataProtectionService + Testing

**Fase 4: UI Avanzada (2 semanas, 60 SP)**

- Semana 11: Paquetes Auditoría + Score
- Semana 12: Testing E2E + Deploy

---

## 📚 REFERENCIAS DOCUMENTALES

| Documento                                 | Sección | Story Points |
| ----------------------------------------- | ------- | ------------ |
| `02-ley-172-13.md`                        | #3      | 4 SP         |
| `03-dgii-integration.md`                  | #4      | 21 SP        |
| `05-compliance-reports.md`                | #5      | -            |
| `06-ley-126-02-comercio-electronico.md`   | #14     | 37 SP        |
| `07-ley-63-17-intrant.md`                 | #15     | 60 SP        |
| `12-AUTOMATIZACION-REPORTES-DGII.md`      | #9      | 94 SP        |
| `13-PREPARACION-AUDITORIA-DGII.md` 🆕     | #10     | **115 SP**   |
| `14-E-CF-COMPROBANTES-ELECTRONICOS.md` 🆕 | #8      | **155 SP**   |
| `46-registro-gastos-operativos.md`        | #13     | 105 SP       |
| `25-AUDITORIA-CUMPLIMIENTO/` 🆕           | #11     | **309 SP**   |

**Documentos de e-CF:**

- Norma General 06-2018 DGII (Facturación Electrónica)
- Resolución 13-2019 (Especificaciones Técnicas e-CF)
- Ley 11-92 Código Tributario (Art. 50-56)

**Contactos Críticos:**

- **DGII Oficina Virtual:** https://dgii.gov.do/oficinavirtual
- **DGII Soporte e-CF:** ecf@dgii.gov.do | Tel: 809-689-3444
- **Certificados Digitales:** INDOTEL | Cámara de Comercio | CertiSign
- **Ambiente de Pruebas:** https://ecf.dgii.gov.do/testecf/

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/auditoria-compliance.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Auditoría y Compliance Legal", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de compliance", async ({ page }) => {
    await page.goto("/admin/compliance");

    await expect(page.getByTestId("compliance-dashboard")).toBeVisible();
  });

  test("debe ver checklist de obligaciones", async ({ page }) => {
    await page.goto("/admin/compliance/checklist");

    await expect(page.getByTestId("obligations-checklist")).toBeVisible();
  });

  test("debe ver estado de certificados", async ({ page }) => {
    await page.goto("/admin/compliance/certificates");

    await expect(page.getByTestId("certificates-status")).toBeVisible();
  });

  test("debe generar reporte de cumplimiento", async ({ page }) => {
    await page.goto("/admin/compliance/reports");

    await page.getByRole("button", { name: /generar reporte/i }).click();
    await expect(page.getByTestId("compliance-report")).toBeVisible();
  });
});
```

---

_Próxima revisión: Febrero 15, 2026 (post-implementación: Gastos Operativos + DGII Formatos + Automatización + Preparación Auditoría)_
