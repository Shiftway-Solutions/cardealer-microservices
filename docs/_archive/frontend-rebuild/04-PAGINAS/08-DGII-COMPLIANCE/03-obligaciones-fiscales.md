---
title: "45 - Obligaciones Fiscales DGII (Formatos 606/607/608)"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 💰 45 - Obligaciones Fiscales DGII (Formatos 606/607/608)

> **Scope:** Cumplimiento de obligaciones fiscales DGII (ADMIN FISCAL)  
> **Rutas:** `/admin/fiscal/formato-606`, `/admin/fiscal/formato-607`, `/admin/fiscal/formato-608`  
> **Roles:** Admin Fiscal, Super Admin (NO para usuarios normales ni dealers)  
> **Diferencia con doc 33:** Este doc = Reportes obligatorios DGII | Doc 33 = Facturas de usuarios  
> **Facturas de usuarios:** Ver [33-facturacion-dgii.md](33-facturacion-dgii.md)

> **Sprint:** 6 (Fiscal & Compliance)  
> **Prioridad:** P0 - CRÍTICA (Obligaciones Legales)  
> **Proceso Matrix:**
>
> - [08-obligaciones-fiscales-dgii.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/08-obligaciones-fiscales-dgii.md)
> - [10-PROCEDIMIENTO-FISCAL-OKLA.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/10-PROCEDIMIENTO-FISCAL-OKLA.md)  
>   **Backend:** InvoicingService (Puerto 5046) + FiscalService (Puerto TBD)  
>   **Última Auditoría:** Enero 30, 2026

---

## 📌 DATOS FISCALES DE OKLA S.R.L.

| Campo                   | Valor                               |
| ----------------------- | ----------------------------------- |
| **Razón Social**        | OKLA S.R.L.                         |
| **RNC**                 | 1-33-32590-1                        |
| **Registro Mercantil**  | 196339PSD                           |
| **Fecha Constitución**  | Enero 25, 2026                      |
| **Actividad Económica** | Plataforma de Anuncios Clasificados |
| **Modelo de Negocio**   | Servicios de Publicidad + ITBIS     |

### Modelo de Negocio (Relevancia Fiscal)

**OKLA VENDE servicios de publicidad (sujeto a ITBIS 18%):**

- Publicación de anuncios: $29 + ITBIS
- Suscripciones dealers: $49-$299/mes + ITBIS
- Boosts y promociones

**OKLA NO:**

- ❌ Compra ni vende vehículos
- ❌ Cobra comisiones por ventas de vehículos
- ❌ Es intermediario financiero
- ❌ Procesa pagos entre compradores/vendedores

**Implicación Fiscal:** Todas las obligaciones DGII aplican a OKLA como empresa de servicios digitales.

---

## 🔍 AUDITORÍA DE IMPLEMENTACIÓN (Enero 29, 2026)

### 📊 Resumen Ejecutivo

| Categoría                         | Requisitos | Implementado | Pendiente | % Completado |
| --------------------------------- | ---------- | ------------ | --------- | ------------ |
| **NCF - Gestión Secuencias**      | 5          | 1            | 4         | 🔴 20%       |
| **Formato 606 (Compras)**         | 4          | 0            | 4         | 🔴 0%        |
| **Formato 607 (Ventas)**          | 4          | 0            | 4         | 🔴 0%        |
| **Formato 608 (Anulaciones)**     | 3          | 0            | 3         | 🔴 0%        |
| **e-CF (Factura Electrónica)**    | 5          | 0            | 5         | 🔴 0%        |
| **Dashboard Fiscal Admin**        | 4          | 0            | 4         | 🔴 0%        |
| **Alertas y Recordatorios**       | 3          | 0            | 3         | 🔴 0%        |
| **TOTAL**                         | **28**     | **1**        | **27**    | **🔴 4%**    |
| **CUMPLIMIENTO LEY 11-92 (DGII)** | **28**     | **1**        | **27**    | **🔴 4%**    |

### ⚠️ ESTADO GENERAL: 🔴 CRÍTICO - NO CUMPLE

**Conclusión:** OKLA tiene **4% de cumplimiento** en obligaciones fiscales DGII. Sin estos sistemas, la plataforma **NO puede operar legalmente** en República Dominicana.

---

## 🚨 RIESGO LEGAL - LEY 11-92 (Código Tributario) & LEY 253-12 (Comprobantes Fiscales)

### 📋 Marco Legal

| Ley             | Artículo    | Requisito                              | Estado      | Multa                |
| --------------- | ----------- | -------------------------------------- | ----------- | -------------------- |
| **Ley 11-92**   | Art. 50     | Emisión de comprobantes fiscales       | 🔴 Parcial  | RD$10,000-$50,000    |
| **Ley 253-12**  | Art. 7      | NCF secuencial y autorizado            | 🔴 Falta UI | RD$50,000-$500,000   |
| **Ley 11-92**   | Art. 309    | Presentación Formato 607 (día 15)      | 🔴 Falta    | RD$3,000-$15,000/mes |
| **Ley 11-92**   | Art. 310    | Presentación Formato 606 (día 15)      | 🔴 Falta    | RD$3,000-$15,000/mes |
| **Ley 11-92**   | Art. 311    | Presentación Formato 608 (día 15)      | 🔴 Falta    | RD$2,000-$10,000/mes |
| **Norma 06-18** | Art. 15     | Factura Electrónica (e-CF) obligatoria | 🔴 Opcional | Futuro (2027+)       |
| **Ley 11-92**   | Art. 254    | Pago ITBIS (día 20)                    | ✅ Azul     | 10% + 4% interés/mes |
| **Ley 11-92**   | Art. 50-bis | Registro contable completo             | 🔴 Falta    | RD$10,000-$100,000   |

### ⚠️ Sanciones por Incumplimiento

| Infracción                     | Sanción Base       | Consecuencia Adicional      |
| ------------------------------ | ------------------ | --------------------------- |
| No emitir NCF                  | RD$10,000-$50,000  | Cierre temporal (3-6 meses) |
| No presentar 607 (Ventas)      | RD$3,000-$15,000   | Multa por cada mes          |
| No presentar 606 (Compras)     | RD$3,000-$15,000   | Multa por cada mes          |
| No presentar 608 (Anulaciones) | RD$2,000-$10,000   | Multa por cada mes          |
| Secuencias NCF vencidas        | RD$50,000-$500,000 | Suspensión de operaciones   |
| NCF duplicado o falso          | 2-6 años prisión   | Delito penal + multa        |
| Evasión fiscal (ITBIS)         | 2-6 años prisión   | Embargo + multa             |
| Mora ITBIS                     | 10% + 4% mensual   | Intereses acumulativos      |

**Total multas anuales estimadas si NO se implementa:** **RD$360,000 - RD$1,200,000**  
**Riesgo de cierre:** **ALTO** (sin cumplimiento fiscal la empresa no puede operar)

---

## ✅ IMPLEMENTADO (1/28)

### 1. Generación Básica de NCF (20% ✅)

**Backend:** InvoicingService  
**Endpoint:** `POST /api/fiscal/ncf/generate`

```csharp
// InvoicingService - Generación básica NCF
public async Task<string> GenerateNCF(NCFType type)
{
    var sequence = await _ncfSequenceRepository.GetNext(type);
    var ncf = $"B{(int)type:D2}{sequence:D8}";
    await _ncfSequenceRepository.IncrementUsed(type);
    return ncf;
}
```

**Tipos de NCF Soportados:**

- B01: Factura Crédito Fiscal (empresas con RNC)
- B02: Factura Consumo (consumidor final)
- B04: Nota de Crédito (devoluciones)

**Falta:**

- ❌ UI de administración de secuencias
- ❌ Alertas de agotamiento
- ❌ Solicitud de nuevas secuencias a DGII
- ❌ Validación de NCF en factura

---

## 🔴 FALTANTES CRÍTICOS (27/28)

### 1. Gestión de Secuencias NCF (80% Pendiente) 🔴

**Prioridad:** P0 - CRÍTICA  
**Blocker:** SÍ - Sin esto no se pueden emitir facturas legales  
**Ley:** 253-12 (Art. 7-12)  
**Multa:** RD$50,000-$500,000 + cierre temporal

#### Rutas Faltantes

```
/admin/fiscal/ncf-config         → Configuración de secuencias
/admin/fiscal/ncf/sequences      → Ver secuencias activas
/admin/fiscal/ncf/request        → Solicitar nueva secuencia DGII
/admin/fiscal/ncf/history        → Historial de secuencias
```

#### Archivos Faltantes (5 archivos)

```tsx
// 1. Página principal de gestión NCF
src / pages / admin / fiscal / NCFManagementPage.tsx;

// 2. Formulario de solicitud de secuencia
src / pages / admin / fiscal / NCFRequestPage.tsx;

// 3. Tabla de secuencias
src / components / admin / fiscal / NCFSequenceTable.tsx;

// 4. Alertas de secuencias bajas
src / components / admin / fiscal / NCFAlertBanner.tsx;

// 5. Modal de configuración
src / components / admin / fiscal / NCFConfigModal.tsx;
```

#### UI Propuesta (NCFManagementPage)

```tsx
// NCFManagementPage.tsx - FALTA CREAR
import { useState } from "react";
import { useNCFSequences } from "@/hooks/useNCFSequences";
import { AdminLayout } from "@/layouts/AdminLayout";
import { NCFSequenceTable } from "@/components/admin/fiscal/NCFSequenceTable";
import { NCFAlertBanner } from "@/components/admin/fiscal/NCFAlertBanner";
import { FiAlertTriangle, FiPlus } from "react-icons/fi";

export default function NCFManagementPage() {
  const { data: sequences, isLoading } = useNCFSequences();
  const lowSequences = sequences?.filter((s) => s.remaining < 100);

  return (
    <AdminLayout>
      <div className="max-w-7xl mx-auto py-6 px-4">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">
              ⚙️ Configuración de Secuencias NCF
            </h1>
            <p className="text-gray-600 mt-1">
              Gestión de Números de Comprobante Fiscal (DGII)
            </p>
          </div>
          <Link
            to="/admin/fiscal/ncf/request"
            className="btn-primary flex items-center gap-2"
          >
            <FiPlus /> Solicitar Nueva Secuencia
          </Link>
        </div>

        {/* Alertas críticas */}
        {lowSequences && lowSequences.length > 0 && (
          <NCFAlertBanner sequences={lowSequences} />
        )}

        {/* Tabla de secuencias */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <NCFSequenceTable sequences={sequences} isLoading={isLoading} />
        </div>

        {/* Stats rápidas */}
        <div className="grid grid-cols-3 gap-4 mt-6">
          <div className="bg-blue-50 p-4 rounded-lg">
            <div className="text-2xl font-bold text-blue-600">
              {sequences?.length || 0}
            </div>
            <div className="text-sm text-blue-700">Secuencias Activas</div>
          </div>
          <div className="bg-green-50 p-4 rounded-lg">
            <div className="text-2xl font-bold text-green-600">
              {sequences?.reduce((sum, s) => sum + s.remaining, 0) || 0}
            </div>
            <div className="text-sm text-green-700">NCF Disponibles</div>
          </div>
          <div className="bg-amber-50 p-4 rounded-lg">
            <div className="text-2xl font-bold text-amber-600">
              {lowSequences?.length || 0}
            </div>
            <div className="text-sm text-amber-700">Alertas Activas</div>
          </div>
        </div>

        {/* Información legal */}
        <div className="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex gap-3">
            <FiAlertTriangle className="text-blue-600 text-xl flex-shrink-0 mt-0.5" />
            <div className="text-sm text-blue-800">
              <p className="font-semibold mb-1">
                Obligaciones DGII - Ley 253-12:
              </p>
              <ul className="list-disc list-inside space-y-1">
                <li>
                  Solicitar secuencias ANTES de agotar (mínimo 100 restantes)
                </li>
                <li>NCF debe ser secuencial y sin saltos</li>
                <li>Multa por NCF no autorizado: RD$50,000-$500,000</li>
                <li>Solicitud toma 3-5 días hábiles en Oficina Virtual DGII</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}
```

#### Componente: NCFSequenceTable

```tsx
// NCFSequenceTable.tsx - FALTA CREAR
interface NCFSequence {
  id: string;
  type: "B01" | "B02" | "B04"; // B01: Consumidor, B02: Crédito Fiscal, B04: Nota Crédito
  typeName: string;
  startNCF: string; // 'B0100000001'
  endNCF: string; // 'B0100001000'
  currentNCF: string; // 'B0100000547'
  remaining: number; // 453
  percentUsed: number; // 54.7
  authorizationDate: string;
  expirationDate: string | null;
  isActive: boolean;
  status: "active" | "low" | "critical" | "expired";
}

export function NCFSequenceTable({ sequences, isLoading }: Props) {
  if (isLoading) return <Spinner />;

  return (
    <table className="w-full">
      <thead className="bg-gray-50 border-b border-gray-200">
        <tr>
          <th className="text-left py-3 px-4">Tipo NCF</th>
          <th className="text-left py-3 px-4">Rango Autorizado</th>
          <th className="text-left py-3 px-4">Último Usado</th>
          <th className="text-center py-3 px-4">Disponibles</th>
          <th className="text-center py-3 px-4">Progreso</th>
          <th className="text-center py-3 px-4">Estado</th>
          <th className="text-center py-3 px-4">Acciones</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-gray-200">
        {sequences?.map((seq) => (
          <tr key={seq.id} className="hover:bg-gray-50">
            <td className="py-4 px-4">
              <div className="font-medium text-gray-900">{seq.type}</div>
              <div className="text-sm text-gray-500">{seq.typeName}</div>
            </td>
            <td className="py-4 px-4">
              <div className="text-sm">
                <div className="font-mono">{seq.startNCF}</div>
                <div className="font-mono text-gray-500">{seq.endNCF}</div>
              </div>
            </td>
            <td className="py-4 px-4">
              <div className="font-mono text-sm">{seq.currentNCF}</div>
            </td>
            <td className="py-4 px-4 text-center">
              <div className="text-lg font-semibold">{seq.remaining}</div>
              <div className="text-xs text-gray-500">
                ({seq.percentUsed.toFixed(1)}% usado)
              </div>
            </td>
            <td className="py-4 px-4">
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className={`h-2 rounded-full ${
                    seq.percentUsed > 90
                      ? "bg-red-500"
                      : seq.percentUsed > 80
                        ? "bg-amber-500"
                        : "bg-green-500"
                  }`}
                  style={{ width: `${seq.percentUsed}%` }}
                />
              </div>
            </td>
            <td className="py-4 px-4 text-center">
              <StatusBadge status={seq.status} />
            </td>
            <td className="py-4 px-4 text-center">
              <button className="btn-sm btn-secondary">Ver Detalles</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
```

#### Hook: useNCFSequences

```typescript
// hooks/useNCFSequences.ts - FALTA CREAR
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { fiscalService } from "@/services/fiscalService";

export function useNCFSequences() {
  return useQuery({
    queryKey: ["ncf-sequences"],
    queryFn: () => fiscalService.getSequences(),
    refetchInterval: 60000, // Refresh cada minuto
  });
}

export function useRequestNCFSequence() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: NCFSequenceRequest) =>
      fiscalService.requestSequence(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ncf-sequences"] });
    },
  });
}
```

#### Backend Endpoints (FiscalService - FALTA CREAR)

```csharp
// FiscalService.Api/Controllers/NCFController.cs - FALTA CREAR
[ApiController]
[Route("api/fiscal/ncf")]
public class NCFController : ControllerBase
{
    private readonly INCFService _ncfService;

    [HttpGet("sequences")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSequences()
    {
        var sequences = await _ncfService.GetActiveSequencesAsync();
        return Ok(sequences);
    }

    [HttpPost("sequences")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSequence([FromBody] AddNCFSequenceRequest request)
    {
        // Validar autorización DGII
        var result = await _ncfService.AddSequenceAsync(request);
        return Ok(result);
    }

    [HttpGet("next/{type}")]
    public async Task<IActionResult> GetNext(NCFType type)
    {
        var ncf = await _ncfService.GenerateNextAsync(type);
        return Ok(new { ncf });
    }

    [HttpGet("validate/{ncf}")]
    public async Task<IActionResult> Validate(string ncf)
    {
        var isValid = await _ncfService.ValidateAsync(ncf);
        return Ok(new { isValid });
    }
}
```

**Story Points:** 13 SP  
**Tiempo:** 4-5 días  
**Prioridad:** P0 (Blocker)

---

### 2. Formato 607 - Reporte de Ventas (100% Pendiente) 🔴

**Prioridad:** P0 - CRÍTICA  
**Blocker:** SÍ - Obligación mensual (día 15)  
**Ley:** 11-92 (Art. 309)  
**Multa:** RD$3,000-$15,000 por mes no presentado

#### Descripción

El **Formato 607** es un reporte mensual obligatorio que detalla TODAS las facturas emitidas (ventas) durante el mes. Debe presentarse antes del día 15 del mes siguiente a través de la Oficina Virtual DGII.

#### Estructura del Formato 607

| Campo             | Descripción         | Ejemplo     |
| ----------------- | ------------------- | ----------- |
| RNC/Cédula        | Cliente             | 101234567   |
| Tipo ID           | 1=RNC, 2=Cédula     | 1           |
| NCF               | Comprobante emitido | B0200000001 |
| NCF Modificado    | Si aplica           |             |
| Tipo Ingreso      | 01=Operaciones      | 01          |
| Fecha Comprobante | Fecha factura       | 20260115    |
| Monto Facturado   | Total sin ITBIS     | 2500.00     |
| ITBIS Facturado   | ITBIS cobrado (18%) | 450.00      |
| Forma Pago        | 04=Tarjeta          | 04          |

**Archivo de salida:** `607RNCEMP012026.txt` (delimitado por pipes `|`)

#### Rutas Faltantes

```
/admin/fiscal/607                  → Generador de 607
/admin/fiscal/607/preview          → Vista previa antes de generar
/admin/fiscal/607/history          → Historial de reportes generados
/admin/fiscal/607/download         → Descargar archivo .txt
```

#### Archivos Faltantes (4 archivos)

```tsx
src / pages / admin / fiscal / DGII607Page.tsx;
src / components / admin / fiscal / Format607Generator.tsx;
src / components / admin / fiscal / Format607Preview.tsx;
src / components / admin / fiscal / ReportHistory.tsx;
```

#### UI Propuesta (DGII607Page)

```tsx
// DGII607Page.tsx - FALTA CREAR
export default function DGII607Page() {
  const [period, setPeriod] = useState({ month: 1, year: 2026 });
  const { data: preview, refetch } = useFormat607Preview(period);
  const generateMutation = useGenerateFormat607();

  const handleGenerate = async () => {
    const result = await generateMutation.mutateAsync(period);
    // Descargar archivo .txt
    downloadFile(result.fileContent, `607${period.month}${period.year}.txt`);
  };

  return (
    <AdminLayout>
      <div className="max-w-7xl mx-auto py-6">
        <h1 className="text-2xl font-bold mb-6">
          📊 Formato 607 - Reporte de Ventas DGII
        </h1>

        {/* Información legal */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
          <p className="font-semibold text-blue-900">
            Obligación Mensual - Día 15
          </p>
          <p className="text-sm text-blue-700 mt-1">
            Reporta TODAS las facturas emitidas (B01, B02, B04) del mes. Multa
            por no presentar: RD$3,000-$15,000 por mes.
          </p>
        </div>

        {/* Selector de período */}
        <div className="bg-white p-6 rounded-lg shadow mb-6">
          <h2 className="font-semibold mb-4">Seleccionar Período</h2>
          <div className="flex gap-4 items-end">
            <div>
              <label className="block text-sm mb-1">Mes</label>
              <select
                value={period.month}
                onChange={(e) =>
                  setPeriod({ ...period, month: +e.target.value })
                }
                className="form-select"
              >
                {months.map((m, i) => (
                  <option key={i} value={i + 1}>
                    {m}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm mb-1">Año</label>
              <select
                value={period.year}
                onChange={(e) =>
                  setPeriod({ ...period, year: +e.target.value })
                }
                className="form-select"
              >
                <option value={2024}>2024</option>
                <option value={2025}>2025</option>
                <option value={2026}>2026</option>
              </select>
            </div>
            <button onClick={() => refetch()} className="btn-secondary">
              Previsualizar
            </button>
          </div>
        </div>

        {/* Preview de transacciones */}
        {preview && <Format607Preview data={preview} />}

        {/* Botón generar */}
        <button
          onClick={handleGenerate}
          disabled={!preview || generateMutation.isPending}
          className="btn-primary mt-6"
        >
          {generateMutation.isPending
            ? "Generando..."
            : "Generar Archivo 607.txt"}
        </button>

        {/* Historial */}
        <div className="mt-8">
          <h2 className="text-xl font-bold mb-4">Historial de Reportes</h2>
          <ReportHistory type="607" />
        </div>
      </div>
    </AdminLayout>
  );
}
```

**Story Points:** 8 SP  
**Tiempo:** 3 días  
**Prioridad:** P0 (Blocker)

---

### 3. Formato 606 - Reporte de Compras (100% Pendiente) 🔴

**Prioridad:** P1 - ALTA  
**Blocker:** NO (opcional si no hay compras)  
**Ley:** 11-92 (Art. 310)  
**Multa:** RD$3,000-$15,000 por mes

#### Descripción

El **Formato 606** reporta todas las compras realizadas por OKLA (gastos operativos, servicios, proveedores). Obligatorio solo si hay compras con NCF.

#### Rutas Faltantes

```
/admin/fiscal/606                  → Generador de 606
```

**Story Points:** 8 SP  
**Tiempo:** 3 días  
**Prioridad:** P1

---

### 4. Formato 608 - Anulaciones (100% Pendiente) 🔴

**Prioridad:** P1 - ALTA  
**Ley:** 11-92 (Art. 311)  
**Multa:** RD$2,000-$10,000

#### Descripción

Reporta NCF anulados (facturas canceladas o errores). Obligatorio si hubo anulaciones.

#### Rutas Faltantes

```
/admin/fiscal/608                  → Registro de anulaciones
```

**Story Points:** 5 SP  
**Tiempo:** 2 días  
**Prioridad:** P1

---

### 5. Dashboard Fiscal Admin (100% Pendiente) 🔴

**Prioridad:** P0 - CRÍTICA  
**Descripción:** Dashboard centralizado de compliance fiscal

#### Ruta Faltante

```
/admin/fiscal/dashboard            → Dashboard fiscal
```

#### UI Propuesta

```tsx
// FiscalDashboardPage.tsx - FALTA CREAR
export default function FiscalDashboardPage() {
  const { data: summary } = useFiscalSummary();
  const { data: deadlines } = useUpcomingDeadlines();

  return (
    <AdminLayout>
      <div className="max-w-7xl mx-auto py-6">
        <h1 className="text-3xl font-bold mb-6">
          📊 Dashboard Fiscal - {format(new Date(), "MMMM yyyy")}
        </h1>

        {/* Próximos vencimientos */}
        <div className="bg-amber-50 border border-amber-300 rounded-lg p-4 mb-6">
          <h2 className="font-semibold text-amber-900 mb-3">
            ⏰ Próximos Vencimientos DGII
          </h2>
          <div className="space-y-2">
            {deadlines?.map((d) => (
              <div key={d.id} className="flex justify-between">
                <span className="text-sm">{d.name}</span>
                <span
                  className={`text-sm font-semibold ${
                    d.daysRemaining <= 3 ? "text-red-600" : "text-amber-700"
                  }`}
                >
                  Día {d.day} ({d.daysRemaining} días)
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Resumen del mes */}
        <div className="grid grid-cols-3 gap-6 mb-6">
          <StatCard
            title="Ventas del Mes"
            value={formatCurrency(summary?.totalSales)}
            subtitle={`${summary?.invoiceCount} facturas`}
            icon="💰"
          />
          <StatCard
            title="ITBIS a Pagar"
            value={formatCurrency(summary?.itbisToPay)}
            subtitle="Ventas - Compras"
            icon="💳"
          />
          <StatCard
            title="Compras del Mes"
            value={formatCurrency(summary?.totalPurchases)}
            subtitle={`${summary?.purchaseCount} facturas`}
            icon="🛒"
          />
        </div>

        {/* Comprobantes emitidos */}
        <div className="bg-white rounded-lg shadow p-6 mb-6">
          <h2 className="text-xl font-bold mb-4">📄 Comprobantes Emitidos</h2>
          <div className="grid grid-cols-4 gap-4">
            <div>
              <div className="text-3xl font-bold text-blue-600">
                {summary?.ncfCounts.B01}
              </div>
              <div className="text-sm text-gray-600">B01 - Consumo</div>
            </div>
            <div>
              <div className="text-3xl font-bold text-green-600">
                {summary?.ncfCounts.B02}
              </div>
              <div className="text-sm text-gray-600">B02 - Crédito Fiscal</div>
            </div>
            <div>
              <div className="text-3xl font-bold text-amber-600">
                {summary?.ncfCounts.B04}
              </div>
              <div className="text-sm text-gray-600">B04 - Notas Crédito</div>
            </div>
            <div>
              <div className="text-3xl font-bold text-red-600">
                {summary?.ncfCounts.voided}
              </div>
              <div className="text-sm text-gray-600">Anulados</div>
            </div>
          </div>
        </div>

        {/* Acciones rápidas */}
        <div className="grid grid-cols-3 gap-4">
          <QuickActionCard
            title="Generar 606"
            description="Reporte de compras"
            href="/admin/fiscal/606"
            icon="📥"
          />
          <QuickActionCard
            title="Generar 607"
            description="Reporte de ventas"
            href="/admin/fiscal/607"
            icon="📤"
          />
          <QuickActionCard
            title="Ver NCF"
            description="Secuencias activas"
            href="/admin/fiscal/ncf"
            icon="🔢"
          />
        </div>
      </div>
    </AdminLayout>
  );
}
```

**Story Points:** 8 SP  
**Tiempo:** 3 días  
**Prioridad:** P0

---

### 6. Factura Electrónica (e-CF) (100% Pendiente) 🟡

**Prioridad:** P2 - MEDIA (opcional hasta 2027)  
**Ley:** Norma 06-2018  
**Estado:** Voluntario (obligatorio para empresas >RD$100M/año en 2027+)

#### Descripción

Sistema de **Factura Electrónica** certificada por DGII. Requiere:

- Certificado digital autorizado
- Integración con WebService DGII
- Firma electrónica de documentos
- Almacenamiento XML por 10 años

**Story Points:** 34 SP (13 backend + 13 frontend + 8 integración)  
**Tiempo:** 2-3 semanas  
**Prioridad:** P2 (futuro, no blocker)

---

### 7. Alertas y Recordatorios Automáticos (100% Pendiente) 🔴

**Prioridad:** P1 - ALTA  
**Descripción:** Sistema de notificaciones automáticas para vencimientos DGII

#### Funcionalidades Faltantes

```
- Email día 8: Recordatorio IR-17 (retenciones)
- Email día 12: Recordatorio 606/607/608 + borrador
- Email día 17: Recordatorio ITBIS
- Alert urgente: Día del vencimiento
- Alerta NCF: Cuando quedan < 100 secuencias
```

**Story Points:** 5 SP  
**Tiempo:** 2 días  
**Prioridad:** P1

---

## 📊 PLAN DE IMPLEMENTACIÓN

### Fase 1: Fundamentos (CRÍTICO) - 2 semanas

**Objetivo:** Cumplir mínimo legal para operar

| Task                      | SP  | Días | Prioridad |
| ------------------------- | --- | ---- | --------- |
| Gestión de Secuencias NCF | 13  | 4-5  | P0        |
| Dashboard Fiscal Admin    | 8   | 3    | P0        |
| Generador Formato 607     | 8   | 3    | P0        |
| Alertas y Recordatorios   | 5   | 2    | P1        |
| **TOTAL FASE 1**          | 34  | 12   | **P0**    |

### Fase 2: Compliance Completo - 1 semana

| Task                  | SP  | Días | Prioridad |
| --------------------- | --- | ---- | --------- |
| Generador Formato 606 | 8   | 3    | P1        |
| Generador Formato 608 | 5   | 2    | P1        |
| **TOTAL FASE 2**      | 13  | 5    | **P1**    |

### Fase 3: Automatización (Futuro) - 3 semanas

| Task                       | SP  | Días | Prioridad |
| -------------------------- | --- | ---- | --------- |
| e-CF (Factura Electrónica) | 34  | 15   | P2        |
| **TOTAL FASE 3**           | 34  | 15   | **P2**    |

**Total Story Points:** 81 SP  
**Tiempo Total:** 8-10 semanas (Fase 1 + 2 + 3)  
**Mínimo para operar legalmente:** Fase 1 (34 SP, 2 semanas)

---

## 🎯 RECOMENDACIONES

### 🚨 Urgente (Próximos 15 días)

1. **Implementar Gestión de Secuencias NCF** (13 SP)
   - Sin esto, NO se pueden emitir facturas legales
   - Multa: RD$50,000-$500,000 + cierre temporal

2. **Implementar Dashboard Fiscal** (8 SP)
   - Visibilidad de obligaciones y vencimientos
   - Prevenir multas por olvido de presentación

3. **Implementar Formato 607** (8 SP)
   - Obligatorio mensual (día 15)
   - Multa acumulativa: RD$3,000-$15,000/mes

### ⚡ Importante (Próximos 30 días)

4. **Alertas Automáticas** (5 SP)
   - Email 3 días antes de cada vencimiento
   - Reducir riesgo de multas

5. **Formato 606 y 608** (13 SP)
   - Completar compliance fiscal 100%

### 🔮 Futuro (3-6 meses)

6. **e-CF (Factura Electrónica)** (34 SP)
   - Prepararse para obligatoriedad futura (2027+)
   - Mejora de imagen corporativa

---

## 💰 CÁLCULO DE RIESGO

### Escenario 1: Sin Implementación (Status Quo)

| Multa                     | Frecuencia | Monto/mes         | Monto/año      |
| ------------------------- | ---------- | ----------------- | -------------- |
| No presentar 607          | Mensual    | RD$3,000          | RD$36,000      |
| No presentar 606          | Mensual    | RD$3,000          | RD$36,000      |
| No presentar 608          | Mensual    | RD$2,000          | RD$24,000      |
| NCF no autorizado         | Una vez    | RD$50,000         | RD$50,000      |
| **TOTAL ANUAL**           |            |                   | **RD$146,000** |
| **Plus riesgo de cierre** |            | **Pérdida total** | **$$$**        |

### Escenario 2: Con Implementación (Fase 1 + 2)

| Inversión                 | SP  | Costo Estimado  |
| ------------------------- | --- | --------------- |
| Desarrollo (47 SP x $200) | 47  | $9,400 USD      |
| QA y Testing              | -   | $2,000 USD      |
| **TOTAL**                 | 47  | **$11,400 USD** |

**ROI:** En 1 año se recupera la inversión evitando multas (RD$146,000 ≈ $2,500 USD/mes)  
**Riesgo evitado:** Cierre de operaciones

---

## 📚 REFERENCIAS

| Recurso                  | URL                                |
| ------------------------ | ---------------------------------- |
| **Oficina Virtual DGII** | https://oficinavirtual.dgii.gov.do |
| **Manual Formatos**      | https://dgii.gov.do/formatosEnvio  |
| **Ley 11-92**            | Código Tributario Dominicano       |
| **Ley 253-12**           | Comprobantes Fiscales y NCF        |
| **Norma 06-2018**        | Facturación Electrónica (e-CF)     |
| **Portal NCF**           | https://dgii.gov.do/ncf            |
| **DGII Teléfono**        | (809) 689-3444                     |

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Backend 🟡

- [x] Generación básica de NCF (20%)
- [ ] FiscalService completo
- [ ] NCFController (secuencias)
- [ ] FormatsController (606/607/608)
- [ ] TaxController (ITBIS, dashboard)
- [ ] Repositorio NCFSequence
- [ ] Repositorio DGIIReport
- [ ] Tests unitarios (20 tests)

### Frontend 🔴

#### Admin Fiscal

- [ ] NCFManagementPage
- [ ] NCFRequestPage
- [ ] DGII607Page
- [ ] DGII606Page
- [ ] DGII608Page
- [ ] FiscalDashboardPage

#### Componentes

- [ ] NCFSequenceTable
- [ ] NCFAlertBanner
- [ ] Format607Generator
- [ ] Format607Preview
- [ ] ReportHistory
- [ ] DeadlineCalendar
- [ ] FiscalStats

#### Hooks

- [ ] useNCFSequences
- [ ] useFormat607
- [ ] useFormat606
- [ ] useFormat608
- [ ] useFiscalSummary
- [ ] useUpcomingDeadlines

#### Services

- [ ] fiscalService.ts (API calls)
- [ ] ncfService.ts (lógica NCF)
- [ ] dgiiService.ts (formatos)

### Integración 🔴

- [ ] Rutas en App.tsx
- [ ] Links en AdminSidebar (ya existen ✅)
- [ ] Permisos (solo Admin/Super Admin)
- [ ] Notificaciones email (día 8, 12, 17)
- [ ] Alertas en dashboard

---

## 💼 CASOS DE USO REALES DE OKLA (Operacional)

> **Referencia:** [10-PROCEDIMIENTO-FISCAL-OKLA.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/10-PROCEDIMIENTO-FISCAL-OKLA.md)

Esta sección muestra cómo OKLA S.R.L. debe usar el sistema fiscal en el día a día.

### Tipos de NCF que OKLA Emite

| NCF     | Cuándo Usar                            | Ejemplo Cliente                          |
| ------- | -------------------------------------- | ---------------------------------------- |
| **B01** | Dealer/empresa **con RNC**             | Suscripción Pro a "AutoMax S.R.L."       |
| **B02** | Usuario individual **sin RNC**         | Publicación individual a José Pérez      |
| **B04** | Devolución/corrección                  | Usuario cancela suscripción (reembolso)  |
| **B15** | Venta a gobierno                       | Ministerio contrata publicidad (raro)    |
| **B13** | Compras exterior (OKLA como comprador) | Digital Ocean, Stripe (para formato 606) |

### Ejemplos de Ventas (Formato 607)

```
# ════════════════════════════════════════════════════════════════════════
# ENERO 2026 - VENTAS TÍPICAS DE OKLA
# ════════════════════════════════════════════════════════════════════════

# 1. Suscripción Plan Pro a dealer con RNC (tarjeta de crédito)
# Cliente: AutoMax S.R.L., RNC 131-32590-1
# Factura B01: B0100000789
# Monto: $129 + $23.22 ITBIS = $152.22
131325901|1|B0100000789||02|20260115||129.00|23.22|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|152.22|0.00|0.00|0.00|0.00|

# 2. Publicación individual a persona sin RNC (tarjeta)
# Cliente: José Pérez, Cédula 001-1234567-8
# Factura B02: B0200001234
# Monto: $29 + $5.22 ITBIS = $34.22
0|2|B0200001234||02|20260118||29.00|5.22|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|34.22|0.00|0.00|0.00|0.00|

# 3. Suscripción Plan Starter a dealer con RNC
# Cliente: Carros RD S.R.L., RNC 130-11111-1
# Factura B01: B0100000790
# Monto: $49 + $8.82 ITBIS = $57.82
130111111|1|B0100000790||02|20260120||49.00|8.82|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|57.82|0.00|0.00|0.00|0.00|

# 4. Nota de crédito por cancelación de suscripción
# Cliente: AutoMax S.R.L., RNC 131-32590-1
# NCF Anulado: B0100000789 (factura original)
# Nota Crédito B04: B0400000012
# Monto: $129 + $23.22 ITBIS = $152.22 (negativo)
131325901|1|B0400000012|B0100000789|02|20260125||-129.00|-23.22|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|0.00|-152.22|0.00|0.00|0.00|0.00|
```

### Ejemplos de Compras (Formato 606)

```
# ════════════════════════════════════════════════════════════════════════
# ENERO 2026 - GASTOS TÍPICOS DE OKLA
# ════════════════════════════════════════════════════════════════════════

# GASTOS INTERNACIONALES (B13 - Sin ITBIS)
# ─────────────────────────────────────────────────────────────────────────

# 1. Digital Ocean - Hosting ($100 USD ≈ RD$6,000)
|0|3|02|B1300000001||20260115|20260115|6000.00|0.00|6000.00|0.00|0.00|0.00|0.00|0.00|0.00|03|

# 2. GitHub - Repositorio ($21 USD ≈ RD$1,260)
|0|3|02|B1300000002||20260115|20260115|1260.00|0.00|1260.00|0.00|0.00|0.00|0.00|0.00|0.00|03|

# 3. Stripe - Comisiones del mes (~RD$15,000)
|0|3|07|B1300000003||20260131|20260131|15000.00|0.00|15000.00|0.00|0.00|0.00|0.00|0.00|0.00|03|

# 4. Google Ads - Publicidad (~RD$30,000)
|0|3|02|B1300000004||20260131|20260131|30000.00|0.00|30000.00|0.00|0.00|0.00|0.00|0.00|0.00|03|

# GASTOS LOCALES (Con NCF y ITBIS)
# ─────────────────────────────────────────────────────────────────────────

# 5. NIC.do - Dominio okla.com.do (~RD$2,500 + ITBIS)
130529842|1|02|B0100012345||20260110|20260110|2500.00|0.00|2500.00|450.00|0.00|0.00|0.00|0.00|0.00|03|

# 6. AZUL Banco Popular - Comisiones (~RD$8,000 + ITBIS)
101234567|1|07|B0100000543||20260131|20260131|8000.00|0.00|8000.00|1440.00|0.00|0.00|0.00|0.00|0.00|03|

# 7. Contador - Honorarios (~RD$15,000 + ITBIS, retención ISR 10%)
102345678|1|02|B0100000789||20260125|20260125|15000.00|0.00|15000.00|2700.00|0.00|0.00|0.00|0.00|1500.00|02|

# 8. Abogado - Servicios legales (~RD$25,000 + ITBIS, retención ISR 10%)
103456789|1|02|B0100000321||20260120|20260125|25000.00|0.00|25000.00|4500.00|0.00|0.00|0.00|0.00|2500.00|02|

# 9. Claro - Internet oficina (~RD$3,500 + ITBIS)
101654321|1|02|B0100098765||20260115|20260115|3500.00|0.00|3500.00|630.00|0.00|0.00|0.00|0.00|0.00|03|

# 10. Alquiler oficina a persona física (~RD$20,000 + ITBIS, retención 10%)
00112345678|2|03|B0200005432||20260105|20260105|20000.00|0.00|20000.00|3600.00|0.00|0.00|0.00|0.00|2000.00|02|
```

### Cálculo de ITBIS Mensual (Ejemplo Real)

```
┌─────────────────────────────────────────────────────────────────────────┐
│           CÁLCULO ITBIS - ENERO 2026 (Ejemplo OKLA)                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  VENTAS (ITBIS Cobrado):                                                │
│  ──────────────────────────                                             │
│  50 suscripciones x $129       = $6,450.00                              │
│  ITBIS 18%                     = $1,161.00                              │
│                                                                         │
│  100 publicaciones x $29       = $2,900.00                              │
│  ITBIS 18%                     = $  522.00                              │
│                                                                         │
│  TOTAL ITBIS COBRADO           = $1,683.00                              │
│                                                                         │
│  COMPRAS LOCALES (ITBIS Pagado Deducible):                              │
│  ──────────────────────────────────────                                 │
│  Contador $15,000 x 18%        = $2,700.00                              │
│  Abogado $25,000 x 18%         = $4,500.00                              │
│  Internet $3,500 x 18%         = $  630.00                              │
│  Dominio NIC.do $2,500 x 18%   = $  450.00                              │
│  Alquiler $20,000 x 18%        = $3,600.00                              │
│  AZUL comisiones $8,000 x 18%  = $1,440.00                              │
│                                                                         │
│  TOTAL ITBIS DEDUCIBLE         = $13,320.00                             │
│                                                                         │
│  GASTOS INTERNACIONALES (Sin ITBIS):                                    │
│  ─────────────────────────────────────                                  │
│  Digital Ocean, GitHub, Stripe, Google Ads = $52,260 (NO deducible)     │
│                                                                         │
│  RESULTADO:                                                             │
│  ──────────────────────────                                             │
│  ITBIS Cobrado                   $1,683.00                              │
│  ITBIS Deducible               -$13,320.00                              │
│  ═════════════════════════════════════════                              │
│  CRÉDITO FISCAL                 ($11,637.00)                            │
│                                                                         │
│  ➡️ No hay pago este mes. Crédito se arrastra al siguiente.             │
│  ➡️ En meses futuros, crédito acumulado reduce pago de ITBIS.           │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Retenciones (IR-17) - Ejemplos OKLA

**Cuándo OKLA debe retener ISR 10%:**

| Tipo de Gasto             | Retención | Base Legal         | Ejemplo                 |
| ------------------------- | --------- | ------------------ | ----------------------- |
| Servicios profesionales   | 10%       | Art. 309 Ley 11-92 | Contador, Abogado       |
| Alquiler a persona física | 10%       | Art. 309           | Alquiler de oficina     |
| Servicios técnicos        | 10%       | Art. 309           | Desarrollador freelance |
| Publicidad (>RD$50K)      | 10%       | Art. 309           | Influencer, diseñador   |

**Cuándo NO retener:**

- ❌ Empresas (SRL, SA) - No aplica retención
- ❌ Servicios públicos (luz, agua) - Exentos
- ❌ Compra de bienes - Solo servicios retienen
- ❌ Gastos internacionales - Otro régimen (Art. 305)

### UI Requerida para Estos Casos de Uso

Para manejar estos escenarios, el sistema necesita:

1. **Selector automático de NCF:**
   - Input: ¿Cliente tiene RNC?
   - Output: Sugerir B01 (con RNC) o B02 (sin RNC)

2. **Calculadora de ITBIS:**
   - Input: Monto base
   - Output: ITBIS 18% + Total

3. **Registro de retenciones:**
   - Checkbox: "¿Aplica retención ISR 10%?"
   - Calcular automáticamente

4. **Clasificación de gastos:**
   - Dropdown: Tipo de gasto (02 Servicios, 03 Arrendamiento, etc.)
   - Auto-llenar en formato 606

5. **Dashboard de ITBIS:**
   - Cobrado vs Pagado en tiempo real
   - Crédito fiscal acumulado
   - Alerta si hay ITBIS a pagar (>RD$1,000)

**Story Points para UI Operacional:** 13 SP adicionales

---

## 🏆 CONCLUSIÓN

**OKLA S.R.L. tiene 4% de compliance con las obligaciones fiscales de DGII (Ley 11-92).**

**Story Points Totales:** **94 SP** (81 SP formatos + 13 SP UI operacional)

**Estado Actual:** 🔴 **4% de cumplimiento** (1/28 requisitos)  
**Riesgo Legal:** 🔴 **CRÍTICO** - NO cumple Ley 11-92 (DGII)  
**Blocker:** ✅ **SÍ** - Sin Fase 1, NO se puede operar legalmente  
**Inversión Requerida:** $13,200 USD (Fase 1 + 2 + UI operacional)  
**Tiempo Mínimo:** 2-3 semanas (Fase 1 crítica)  
**Multas Anuales Evitadas:** RD$360,000-$1,200,000 ($6,000-$20,000 USD)

### Recomendación Final

🚨 **IMPLEMENTAR URGENTEMENTE:**

1. **Gestión de Secuencias NCF** (13 SP) → Sin esto, facturas no son legales
2. **Dashboard Fiscal** (8 SP) → Visibilidad de obligaciones
3. **Formato 607** (8 SP) → Cumplir obligación mensual
4. **UI Operacional** (13 SP) → Casos de uso reales de OKLA

**Total Mínimo Viable:** 42 SP (14-18 días) para evitar cierre de operaciones.

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/obligaciones-fiscales.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Obligaciones Fiscales", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar calendario fiscal", async ({ page }) => {
    await page.goto("/admin/fiscal/calendario");

    await expect(page.getByTestId("fiscal-calendar")).toBeVisible();
  });

  test("debe ver obligaciones pendientes", async ({ page }) => {
    await page.goto("/admin/fiscal/obligaciones");

    await expect(page.getByTestId("pending-obligations")).toBeVisible();
  });

  test("debe ver reporte ITBIS", async ({ page }) => {
    await page.goto("/admin/fiscal/itbis");

    await expect(page.getByTestId("itbis-report")).toBeVisible();
  });

  test("debe generar formato 606", async ({ page }) => {
    await page.goto("/admin/fiscal/formatos");

    await page.getByRole("button", { name: /generar 606/i }).click();
    await expect(page.getByText(/formato generado/i)).toBeVisible();
  });

  test("debe ver alertas de vencimiento", async ({ page }) => {
    await page.goto("/admin/fiscal");

    await expect(page.getByTestId("deadline-alerts")).toBeVisible();
  });
});
```

---

**Última auditoría:** Enero 29, 2026  
**Auditor:** Gregory Moreno  
**Próxima revisión:** Febrero 15, 2026  
**Estado:** 🔴 CRÍTICO - Requiere acción inmediata
