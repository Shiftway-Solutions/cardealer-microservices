---
title: "🧾 33 - Facturación Electrónica DGII"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🧾 33 - Facturación Electrónica DGII

> **Scope:** Gestión de facturas y NCF (User + Dealer + Admin básico)  
> **Roles cubiertos:**  
> • **USER:** Ver mis facturas (`/billing/invoices`), descargar PDF con NCF  
> • **DEALER:** Validación RNC en registro, ver facturas de suscripción  
> • **ADMIN:** Gestión de secuencias NCF, aprobar/rechazar solicitudes NCF  
> **Reportes DGII (Formatos 606/607/608):** Ver [45-obligaciones-fiscales-dgii.md](45-obligaciones-fiscales-dgii.md)

> **Sprint:** 4 (Pagos y Facturación)  
> **Prioridad:** P0 - Crítica (Compliance Legal)  
> **Proceso Matrix:** [03-dgii-integration.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/03-dgii-integration.md)  
> **Backend:** InvoicingService (Puerto 5046)  
> **Última Auditoría:** Enero 30, 2026

---

## 🔍 AUDITORÍA COMPLETA DE IMPLEMENTACIÓN (Enero 29, 2026)

### 📊 Resumen Ejecutivo

| Categoría            | Requisitos | Implementado | Pendiente | % Completado |
| -------------------- | ---------- | ------------ | --------- | ------------ |
| **Validación RNC**   | 3          | 3            | 0         | ✅ 100%      |
| **Generación NCF**   | 4          | 2            | 2         | 🟡 50%       |
| **Facturas Usuario** | 3          | 3            | 0         | ✅ 100%      |
| **Reportes DGII**    | 6          | 0            | 6         | 🔴 0%        |
| **Admin NCF**        | 3          | 0            | 3         | 🔴 0%        |
| **Admin Reportes**   | 4          | 0            | 4         | 🔴 0%        |
| **TOTAL**            | **23**     | **8**        | **15**    | **35%**      |

### ✅ IMPLEMENTADO CORRECTAMENTE (8/23)

#### 1. Validación de RNC (100% ✅)

**Ruta:** `/dealer/register`  
**Componente:** `DealerRegistrationPage.tsx`

✅ **Funcionalidades:**

- Validación en tiempo real contra API DGII
- Autocompletado de nombre comercial
- Verificación de status (Activo/Inactivo)
- Cache de 24h para optimización

#### 2. Facturas de Usuario (100% ✅)

**Ruta:** `/billing/invoices`  
**Archivo:** `src/pages/billing/InvoicesPage.tsx`

✅ **Funcionalidades:**

- Lista completa de facturas del usuario
- Filtrado por fecha, estado, monto
- Descarga de PDF con NCF
- Vista de detalles completos
- Histórico de pagos

#### 3. Dashboard de Facturación (100% ✅)

**Ruta:** `/billing`  
**Archivo:** `src/pages/billing/BillingDashboardPage.tsx`

✅ **Funcionalidades:**

- Resumen de gastos mensuales
- Facturas recientes (últimas 3)
- Métodos de pago guardados
- Stats de facturación

### 🔴 FALTANTES CRÍTICOS (15/23)

#### 1. Gestión de Secuencias NCF (0% 🔴)

**Prioridad:** P0 (Crítica - Sin esto no se pueden emitir facturas)  
**Riesgo:** Ley 253-12, Multas DGII hasta RD$500,000

❌ **No existe:**

- Página de administración de secuencias NCF
- Solicitud de nuevas secuencias a DGII
- Alertas de agotamiento de secuencias
- Visualización de secuencias activas/vencidas

**Rutas Faltantes:**

```
/admin/fiscal/ncf
/admin/fiscal/ncf/request
/admin/fiscal/ncf/history
```

**Archivos Faltantes:**

```
src/pages/admin/NCFManagementPage.tsx
src/pages/admin/NCFRequestPage.tsx
src/components/admin/NCFSequenceTable.tsx
src/components/admin/NCFAlertsBanner.tsx
```

**Ejemplo de Código Necesario:**

```tsx
// NCFManagementPage.tsx - FALTA CREAR
export default function NCFManagementPage() {
  const { data: sequences } = useNCFSequences();

  return (
    <AdminLayout>
      <div className="max-w-7xl mx-auto py-6">
        <h1 className="text-2xl font-bold mb-6">Gestión de Secuencias NCF</h1>

        {/* Alerta si queda < 100 NCF */}
        {sequences?.some((s) => s.remaining < 100) && (
          <NCFAlertsBanner sequences={sequences} />
        )}

        {/* Tabla de secuencias */}
        <NCFSequenceTable sequences={sequences} />

        {/* Botón solicitar nueva secuencia */}
        <Link to="/admin/fiscal/ncf/request" className="btn-primary mt-4">
          Solicitar Nueva Secuencia a DGII
        </Link>
      </div>
    </AdminLayout>
  );
}
```

**Estructura de Datos:**

```typescript
interface NCFSequence {
  id: string;
  type: "B01" | "B02" | "B04"; // B01: Consumidor, B02: Crédito Fiscal, B04: Nota Crédito
  prefix: string; // 'B01'
  startNumber: number; // 1
  endNumber: number; // 1000
  currentNumber: number; // 150
  remaining: number; // 850
  authorizationDate: string;
  expirationDate: string;
  isActive: boolean;
  isExpiringSoon: boolean; // < 30 días
  isRunningLow: boolean; // < 100 NCF
}
```

#### 2. Reporte 607 DGII - Ventas (0% 🔴)

**Prioridad:** P0 (Crítica - Obligación mensual)  
**Riesgo:** Multa DGII por no presentar: RD$3,000-$15,000 por reporte

❌ **No existe:**

- Generador de reporte 607 (ventas/ingresos)
- Selector de período
- Preview de transacciones
- Generación de archivo .txt
- Historial de reportes enviados

**Ruta Faltante:**

```
/admin/fiscal/reports/607
```

**Archivo Faltante:**

```
src/pages/admin/DGII607Page.tsx
```

**Ejemplo de Código Necesario:**

```tsx
// DGII607Page.tsx - FALTA CREAR
export default function DGII607Page() {
  const [period, setPeriod] = useState({ month: 1, year: 2026 });
  const [preview, setPreview] = useState<Invoice[]>([]);

  const { mutate: generateReport, isLoading } = useGenerate607();

  const handleGenerate = () => {
    generateReport(
      {
        month: period.month,
        year: period.year,
      },
      {
        onSuccess: (data) => {
          // Descargar archivo .txt
          downloadFile(data.fileUrl, `607${period.month}${period.year}.txt`);
        },
      },
    );
  };

  return (
    <AdminLayout>
      <div className="max-w-7xl mx-auto py-6">
        <h1 className="text-2xl font-bold mb-2">Reporte 607 DGII - Ingresos</h1>
        <p className="text-gray-600 mb-6">
          Formato obligatorio mensual - Plazo: día 10 de cada mes
        </p>

        {/* Selector de período */}
        <div className="bg-white p-6 rounded-lg shadow mb-6">
          <h2 className="font-semibold mb-4">Período a reportar</h2>
          <div className="flex gap-4">
            <select
              value={period.month}
              onChange={(e) =>
                setPeriod((prev) => ({
                  ...prev,
                  month: parseInt(e.target.value),
                }))
              }
              className="border rounded px-3 py-2"
            >
              {months.map((m, i) => (
                <option key={i} value={i + 1}>
                  {m}
                </option>
              ))}
            </select>
            <select
              value={period.year}
              onChange={(e) =>
                setPeriod((prev) => ({
                  ...prev,
                  year: parseInt(e.target.value),
                }))
              }
              className="border rounded px-3 py-2"
            >
              {years.map((y) => (
                <option key={y} value={y}>
                  {y}
                </option>
              ))}
            </select>
            <button
              onClick={() => loadPreview(period)}
              className="btn-secondary"
            >
              Previsualizar
            </button>
          </div>
        </div>

        {/* Preview de transacciones */}
        {preview.length > 0 && (
          <div className="bg-white p-6 rounded-lg shadow mb-6">
            <h2 className="font-semibold mb-4">
              Facturas a incluir: {preview.length}
            </h2>
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2">NCF</th>
                  <th className="text-left py-2">RNC/Cédula</th>
                  <th className="text-left py-2">Fecha</th>
                  <th className="text-right py-2">Monto</th>
                  <th className="text-right py-2">ITBIS</th>
                </tr>
              </thead>
              <tbody>
                {preview.map((inv) => (
                  <tr key={inv.id} className="border-b">
                    <td className="py-2">{inv.ncf}</td>
                    <td className="py-2">{inv.customerRNC}</td>
                    <td className="py-2">{formatDate(inv.issueDate)}</td>
                    <td className="text-right py-2">
                      ${inv.subtotal.toFixed(2)}
                    </td>
                    <td className="text-right py-2">${inv.itbis.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="font-bold">
                  <td colSpan={3}>TOTALES</td>
                  <td className="text-right py-2">
                    $
                    {preview
                      .reduce((sum, inv) => sum + inv.subtotal, 0)
                      .toFixed(2)}
                  </td>
                  <td className="text-right py-2">
                    $
                    {preview
                      .reduce((sum, inv) => sum + inv.itbis, 0)
                      .toFixed(2)}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}

        {/* Botón generar */}
        <button
          onClick={handleGenerate}
          disabled={isLoading || preview.length === 0}
          className="btn-primary"
        >
          {isLoading ? "Generando..." : "Generar Archivo 607.txt"}
        </button>

        {/* Historial */}
        <ReportHistory type="607" />
      </div>
    </AdminLayout>
  );
}
```

#### 3. Reporte 606 DGII - Compras (0% 🔴)

**Prioridad:** P1 (Alta - Obligación mensual)  
**Riesgo:** Multa DGII: RD$3,000-$15,000

❌ **Falta:**

- Generador de reporte 606 (compras)
- Registro de facturas recibidas
- Proveedores con RNC
- Generación archivo .txt

**Ruta Faltante:**

```
/admin/fiscal/reports/606
```

**Archivo Faltante:**

```
src/pages/admin/DGII606Page.tsx
```

#### 4. Reporte 608 DGII - Anulaciones (0% 🔴)

**Prioridad:** P1 (Alta)  
**Riesgo:** Multa DGII

❌ **Falta:**

- Generador de reporte 608 (NCF anulados)
- Registro de razones de anulación

**Ruta Faltante:**

```
/admin/fiscal/reports/608
```

#### 5. Dashboard Fiscal Admin (0% 🔴)

**Prioridad:** P1 (Alta)

❌ **Falta:**

- Dashboard centralizado de compliance fiscal
- Calendario de obligaciones DGII
- Alertas de vencimientos
- Resumen de reportes pendientes

**Ruta Faltante:**

```
/admin/fiscal/dashboard
```

### 🚨 Impacto Legal de los Faltantes

#### Riesgo Alto (P0 - Crítico) 🔴

**1. Gestión de Secuencias NCF Ausente**

- **Riesgo:** No se pueden emitir facturas válidas sin NCF
- **Ley 253-12:** Multa hasta RD$500,000
- **Impacto:** Operación ilegal, cierre de negocio
- **Tiempo de Implementación:** 3-4 días

**2. Reporte 607 DGII (Ventas)**

- **Riesgo:** Incumplimiento obligación mensual
- **Multa:** RD$3,000-$15,000 por mes no presentado
- **Plazo:** Día 10 de cada mes
- **Impacto:** Multas acumulativas + recargos
- **Tiempo de Implementación:** 2-3 días

#### Riesgo Medio (P1 - Alta) 🟡

**3. Reporte 606 DGII (Compras)**

- **Riesgo:** Incumplimiento obligación mensual
- **Multa:** RD$3,000-$15,000
- **Tiempo de Implementación:** 2 días

**4. Reporte 608 DGII (Anulaciones)**

- **Riesgo:** No reportar NCF anulados
- **Multa:** RD$1,000-$5,000
- **Tiempo de Implementación:** 1 día

### 🛠️ Plan de Implementación Recomendado

#### Sprint Crítico (2 semanas)

**Semana 1 - Secuencias NCF**

**Día 1-2: NCF Management**

- [ ] Crear `NCFManagementPage.tsx`
- [ ] Crear `NCFSequenceTable.tsx`
- [ ] Crear `NCFAlertsBanner.tsx`
- [ ] Implementar hook `useNCFSequences()`
- [ ] Conectar con backend `/api/dgii/ncf/sequences`

**Día 3-4: NCF Request**

- [ ] Crear `NCFRequestPage.tsx`
- [ ] Formulario de solicitud a DGII
- [ ] Implementar `useRequestNCF()` mutation
- [ ] Conectar con backend `/api/dgii/ncf/request`

**Semana 2 - Reportes DGII**

**Día 5-7: Reporte 607**

- [ ] Crear `DGII607Page.tsx`
- [ ] Selector de período (mes/año)
- [ ] Preview de transacciones
- [ ] Implementar hook `useGenerate607()`
- [ ] Download de archivo .txt
- [ ] Historial de reportes

**Día 8-9: Reporte 606**

- [ ] Crear `DGII606Page.tsx`
- [ ] Similar a 607 pero para compras
- [ ] Implementar hook `useGenerate606()`

**Día 10: Reporte 608**

- [ ] Crear `DGII608Page.tsx`
- [ ] Listado de NCF anulados
- [ ] Implementar hook `useGenerate608()`

**Testing & QA (Día 11-13)**

- [ ] Testing E2E de flujos fiscales
- [ ] Validación de formatos .txt contra DGII
- [ ] Pruebas de generación de reportes
- [ ] Verificación de límites de secuencias NCF
- [ ] Code review
- [ ] Deploy a staging

### 📝 Checklist de Tareas Pendientes

#### Componentes Faltantes

- [ ] `src/components/admin/NCFSequenceTable.tsx`
- [ ] `src/components/admin/NCFAlertsBanner.tsx`
- [ ] `src/components/admin/ReportHistory.tsx`
- [ ] `src/components/admin/FiscalCalendar.tsx`

#### Páginas Faltantes

- [ ] `src/pages/admin/NCFManagementPage.tsx`
- [ ] `src/pages/admin/NCFRequestPage.tsx`
- [ ] `src/pages/admin/DGII607Page.tsx`
- [ ] `src/pages/admin/DGII606Page.tsx`
- [ ] `src/pages/admin/DGII608Page.tsx`
- [ ] `src/pages/admin/FiscalDashboardPage.tsx`

#### Hooks Faltantes

- [ ] `src/lib/hooks/useNCFSequences.ts`
- [ ] `src/lib/hooks/useRequestNCF.ts`
- [ ] `src/lib/hooks/useGenerate607.ts`
- [ ] `src/lib/hooks/useGenerate606.ts`
- [ ] `src/lib/hooks/useGenerate608.ts`

#### Servicios a Crear

- [ ] `src/services/dgiiService.ts`:
  - [ ] `getNCFSequences()`
  - [ ] `requestNCFSequence(type, quantity)`
  - [ ] `generate607Report(month, year)`
  - [ ] `generate606Report(month, year)`
  - [ ] `generate608Report(month, year)`
  - [ ] `getReportHistory(type)`
  - [ ] `downloadReport(reportId)`

#### Rutas en App.tsx

- [ ] `/admin/fiscal/ncf` → `NCFManagementPage`
- [ ] `/admin/fiscal/ncf/request` → `NCFRequestPage`
- [ ] `/admin/fiscal/reports/607` → `DGII607Page`
- [ ] `/admin/fiscal/reports/606` → `DGII606Page`
- [ ] `/admin/fiscal/reports/608` → `DGII608Page`
- [ ] `/admin/fiscal/dashboard` → `FiscalDashboardPage`

#### Layouts

- [ ] Agregar menú "Fiscal" en AdminLayout
- [ ] Submenu: NCF, Reportes 606/607/608, Dashboard

---

## 🔴 Estado Crítico - Ley 11-92 (Código Tributario DGII)

| Componente           | Backend | Frontend UI | Brecha   | Prioridad   |
| -------------------- | ------- | ----------- | -------- | ----------- |
| **Generación NCF**   | ✅ 90%  | ✅ 100%     | +10%     | ✅ COMPLETO |
| **Facturas PDF**     | ✅ 100% | ✅ 100%     | 0%       | ✅ COMPLETO |
| **Notas de Crédito** | ✅ 100% | ✅ 95%      | -5%      | ✅ COMPLETO |
| **Reporte 607 DGII** | ✅ 80%  | 🔴 0%       | **-80%** | 🔴 CRÍTICO  |
| **Reporte 606 DGII** | ✅ 80%  | 🔴 0%       | **-80%** | 🔴 CRÍTICO  |
| **Validación RNC**   | ✅ 100% | ✅ 100%     | 0%       | ✅ COMPLETO |

### 🔴 BRECHA CRÍTICA: Generación de Reportes DGII

**Proceso COMP-001 (Reporte 607 DGII):** SIN UI

```
Backend Implementado:
✅ InvoicingService tiene endpoints para generar 607/606
✅ Modelos de NCF y transacciones listos
✅ Lógica de formato .txt según DGII

Frontend Faltante:
🔴 NO EXISTE: /admin/dgii/607 (generador de formato 607)
🔴 NO EXISTE: /admin/dgii/606 (generador de formato 606)
🔴 NO EXISTE: DGII607Page.tsx
🔴 NO EXISTE: DGII606Page.tsx
```

### 📋 Plan de Cierre de Brecha (10 SP)

**Sprint Inmediato:** Implementar generadores DGII

1. **DGII607Page** (5 SP)

   ```tsx
   Ruta: /admin/compliance/dgii/607
   Componente: DGII607Page.tsx

   Features:
   - Selector de período (mes/año)
   - Preview de transacciones incluidas
   - Validación de formato RNC/NCF
   - Generación archivo .txt
   - Download directo
   - Historial de reportes generados
   ```

2. **DGII606Page** (5 SP)

   ```tsx
   Ruta: /admin/compliance/dgii/606
   Componente: DGII606Page.tsx

   Features:
   - Selector de período
   - Compras (facturas recibidas)
   - Validación de proveedores
   - Generación archivo .txt
   - Download directo
   ```

### 📊 Formato 607 DGII (Comprobantes Emitidos)

```
Estructura del archivo .txt:
RNC/Cédula|Tipo|NCF|NCF Modificado|Fecha|Monto Facturado|ITBIS|...

Ejemplo:
00112345678|01|B0100000001||2026-01-15|50000.00|9000.00|0.00|0.00|59000.00|0.00|0.00|59000.00
```

| Campo           | Descripción                           | Ejemplo     |
| --------------- | ------------------------------------- | ----------- |
| RNC/Cédula      | Identificación del cliente            | 00112345678 |
| Tipo            | Cédula (01), RNC (02), Pasaporte (03) | 01          |
| NCF             | Número Comprobante Fiscal             | B0100000001 |
| NCF Modificado  | Si es nota de crédito                 | (vacío)     |
| Fecha           | DD/MM/YYYY                            | 15/01/2026  |
| Monto Facturado | Sin ITBIS                             | 50000.00    |
| ITBIS Facturado | 18%                                   | 9000.00     |
| Monto Total     | Con ITBIS                             | 59000.00    |

### 📊 Formato 606 DGII (Comprobantes Recibidos)

```
Estructura similar al 607 pero para COMPRAS:
RNC Proveedor|Tipo|NCF|Fecha|Monto Facturado|ITBIS Facturado|...
```

### 🎯 Endpoints Backend Disponibles

```typescript
// ✅ IMPLEMENTADO en InvoicingService

POST /api/invoicing/dgii/generate-607
{
  "startDate": "2026-01-01",
  "endDate": "2026-01-31"
}
→ Response: { fileUrl: string, recordCount: number }

POST /api/invoicing/dgii/generate-606
{
  "startDate": "2026-01-01",
  "endDate": "2026-01-31"
}
→ Response: { fileUrl: string, recordCount: number }

GET /api/invoicing/dgii/607/history
→ Response: Array<{ period: string, fileUrl: string, generatedAt: string }>

GET /api/invoicing/dgii/606/history
→ Response: Array<{ period: string, fileUrl: string, generatedAt: string }>
```

### 📅 Calendario DGII (Obligaciones)

| Reporte                | Plazo              | Frecuencia   | Estado UI |
| ---------------------- | ------------------ | ------------ | --------- |
| **607**                | Día 10 de cada mes | Mensual      | 🔴 SIN UI |
| **606**                | Día 10 de cada mes | Mensual      | 🔴 SIN UI |
| **IT-1**               | Trimestral         | Cada 3 meses | 🔴 SIN UI |
| **Declaración Jurada** | Marzo              | Anual        | 🔴 SIN UI |

**RECOMENDACIÓN:** Integrar con `/admin/compliance/calendar` (también pendiente)

### ✅ Funcionalidades Completas (No requieren cambios)

1. **Generación de NCF automática** ✅
2. **Facturas PDF descargables** ✅
3. **Notas de crédito** ✅
4. **Validación RNC contra API DGII** ✅
5. **Lista de facturas por usuario** ✅
6. **Dashboard de facturación** ✅

**Referencias:**

- Matriz de procesos: `docs/process-matrix/05-PAGOS-FACTURACION/04-invoicing-service.md`
- Compliance: `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-compliance-service.md`
- Proceso COMP-001: Sección "Reporte 607 DGII"

---

## 📑 Tabla de Contenidos

1. [Resumen](#-resumen)
2. [Marco Legal](#-marco-legal)
3. [Páginas y Rutas](#-páginas-y-rutas)
4. [Componentes UI](#-componentes-ui)
5. [Flujos de Usuario](#-flujos-de-usuario)
6. [Integración API](#-integración-api)
7. [Tipos de NCF](#-tipos-de-ncf)
8. [Testing](#-testing)
9. [Checklist](#-checklist-de-implementación)

---

## 📋 Resumen

Sistema de facturación electrónica conforme a las regulaciones de la DGII (Dirección General de Impuestos Internos) de República Dominicana.

| Funcionalidad        | Descripción                          |
| -------------------- | ------------------------------------ |
| **Generación NCF**   | Número Comprobante Fiscal automático |
| **Facturas PDF**     | Generación y descarga de facturas    |
| **Notas de Crédito** | Anulación parcial/total de facturas  |
| **Reportes DGII**    | Formatos 606/607 para declaración    |
| **Validación RNC**   | Verificación contra API DGII         |

### Modelo de Negocio (Contexto)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                    QUÉ FACTURA OKLA S.R.L.                                │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  ✅ SERVICIOS QUE OKLA FACTURA (con NCF + ITBIS 18%):                      │
│                                                                            │
│  📝 Publicación Individual          $29 + ITBIS ($5.22) = $34.22          │
│  📦 Suscripción Starter             $49/mes + ITBIS = $57.82/mes          │
│  📦 Suscripción Pro                 $129/mes + ITBIS = $152.22/mes        │
│  📦 Suscripción Enterprise          $299/mes + ITBIS = $352.82/mes        │
│  ⭐ Promociones/Destacados          $10-$40 + ITBIS                        │
│                                                                            │
│  ❌ OKLA NO FACTURA:                                                       │
│  └─ Transacciones de vehículos (ocurren directamente dealer ↔ comprador)  │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚖️ Marco Legal

### Normativas Aplicables

| Ley/Norma         | Descripción                     |
| ----------------- | ------------------------------- |
| **Ley 253-12**    | Ley sobre Comprobantes Fiscales |
| **Norma 06-2018** | Factura Electrónica             |
| **Norma 08-2019** | Secuencias de NCF               |
| **ITBIS**         | 18% sobre servicios digitales   |

### RNC de OKLA

```
OKLA S.R.L.
RNC: 1-33-32590-1
Dirección: Av. Winston Churchill #123, Santo Domingo
```

---

## 🛣️ Páginas y Rutas

### Estructura de Navegación

```
/invoices (User/Dealer)
├── /invoices                          → Lista de facturas del usuario
├── /invoices/:id                      → Detalle de factura + PDF
└── /invoices/:id/pdf                  → Descarga directa PDF

/billing (Dealer)
├── /billing/invoices                  → Facturas del dealer
└── /billing/credit-notes              → Notas de crédito

/admin/invoicing (Admin)
├── /admin/invoices                    → Gestión de todas las facturas
├── /admin/invoices/:id                → Detalle admin con acciones
├── /admin/ncf-sequences               → Gestión de secuencias NCF
├── /admin/dgii/reports                → Generación formatos 606/607
└── /admin/dgii/validation             → Validación RNC/NCF
```

---

## 🧩 Componentes UI

### 1. InvoicesListPage

Lista de facturas del usuario con filtros.

```tsx
// src/pages/invoices/InvoicesListPage.tsx

import { useState } from "react";
import { useInvoices } from "@/hooks/useInvoices";
import { InvoiceCard } from "@/components/invoices/InvoiceCard";
import { InvoiceFilters } from "@/components/invoices/InvoiceFilters";
import { Pagination } from "@/components/ui/Pagination";
import { EmptyState } from "@/components/ui/EmptyState";
import { FiFileText } from "react-icons/fi";

export function InvoicesListPage() {
  const [filters, setFilters] = useState({
    page: 1,
    pageSize: 10,
    status: undefined,
    startDate: undefined,
    endDate: undefined,
  });

  const { data, isLoading, error } = useInvoices(filters);

  if (isLoading) return <InvoicesListSkeleton />;
  if (error) return <ErrorMessage error={error} />;

  return (
    <MainLayout>
      <div className="max-w-4xl mx-auto px-4 py-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold">Mis Facturas</h1>
          <span className="text-gray-500">{data?.total || 0} facturas</span>
        </div>

        <InvoiceFilters filters={filters} onChange={setFilters} />

        {data?.data.length === 0 ? (
          <EmptyState
            icon={FiFileText}
            title="No tienes facturas"
            description="Las facturas aparecerán aquí cuando realices pagos"
          />
        ) : (
          <>
            <div className="space-y-4">
              {data?.data.map((invoice) => (
                <InvoiceCard key={invoice.id} invoice={invoice} />
              ))}
            </div>

            <Pagination
              currentPage={filters.page}
              totalPages={data?.totalPages || 1}
              onPageChange={(page) => setFilters({ ...filters, page })}
            />
          </>
        )}
      </div>
    </MainLayout>
  );
}
```

### 2. InvoiceDetailPage

Detalle de factura con visor PDF y acciones.

```tsx
// src/pages/invoices/InvoiceDetailPage.tsx

import { useParams } from "react-router-dom";
import {
  useInvoice,
  useDownloadInvoicePDF,
  useSendInvoiceByEmail,
} from "@/hooks/useInvoices";
import { NCFBadge } from "@/components/invoices/NCFBadge";
import { InvoiceStatusBadge } from "@/components/invoices/InvoiceStatusBadge";
import { InvoicePDFViewer } from "@/components/invoices/InvoicePDFViewer";
import { Button } from "@/components/ui/Button";
import { FiDownload, FiMail, FiPrinter } from "react-icons/fi";

export function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: invoice, isLoading, error } = useInvoice(id!);
  const downloadPDF = useDownloadInvoicePDF();
  const sendEmail = useSendInvoiceByEmail();

  if (isLoading) return <InvoiceDetailSkeleton />;
  if (error) return <ErrorMessage error={error} />;
  if (!invoice) return <NotFound />;

  return (
    <MainLayout>
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="flex justify-between items-start mb-6">
          <div>
            <h1 className="text-2xl font-bold mb-2">
              Factura {invoice.invoiceNumber}
            </h1>
            <div className="flex items-center gap-3">
              <NCFBadge ncf={invoice.ncf} />
              <InvoiceStatusBadge status={invoice.status} />
            </div>
          </div>

          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => downloadPDF.mutate(invoice.id)}
              loading={downloadPDF.isPending}
            >
              <FiDownload className="mr-2" />
              Descargar PDF
            </Button>
            <Button
              variant="outline"
              onClick={() => sendEmail.mutate({ id: invoice.id })}
              loading={sendEmail.isPending}
            >
              <FiMail className="mr-2" />
              Enviar por Email
            </Button>
            <Button variant="outline" onClick={() => window.print()}>
              <FiPrinter className="mr-2" />
              Imprimir
            </Button>
          </div>
        </div>

        {/* Invoice Info */}
        <div className="grid md:grid-cols-2 gap-6 mb-8">
          {/* Emisor */}
          <div className="bg-gray-50 rounded-lg p-4">
            <h3 className="font-semibold mb-2">Emisor</h3>
            <p className="text-sm text-gray-600">
              {invoice.issuerName}
              <br />
              RNC: {invoice.issuerRnc}
              <br />
              {invoice.issuerAddress}
            </p>
          </div>

          {/* Receptor */}
          <div className="bg-gray-50 rounded-lg p-4">
            <h3 className="font-semibold mb-2">Cliente</h3>
            <p className="text-sm text-gray-600">
              {invoice.customerName}
              <br />
              {invoice.customerRnc && `RNC: ${invoice.customerRnc}`}
              <br />
              {invoice.customerEmail}
            </p>
          </div>
        </div>

        {/* Items */}
        <div className="bg-white border rounded-lg overflow-hidden mb-6">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left p-4">Descripción</th>
                <th className="text-right p-4">Cantidad</th>
                <th className="text-right p-4">Precio</th>
                <th className="text-right p-4">ITBIS</th>
                <th className="text-right p-4">Total</th>
              </tr>
            </thead>
            <tbody>
              {invoice.items.map((item) => (
                <tr key={item.id} className="border-t">
                  <td className="p-4">{item.description}</td>
                  <td className="p-4 text-right">{item.quantity}</td>
                  <td className="p-4 text-right">
                    {formatCurrency(item.unitPrice, invoice.currency)}
                  </td>
                  <td className="p-4 text-right">
                    {formatCurrency(item.taxAmount, invoice.currency)}
                  </td>
                  <td className="p-4 text-right font-medium">
                    {formatCurrency(item.total, invoice.currency)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Totals */}
        <div className="flex justify-end">
          <div className="w-64 space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-600">Subtotal</span>
              <span>{formatCurrency(invoice.subtotal, invoice.currency)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">ITBIS (18%)</span>
              <span>{formatCurrency(invoice.taxAmount, invoice.currency)}</span>
            </div>
            {invoice.discountAmount > 0 && (
              <div className="flex justify-between text-green-600">
                <span>Descuento</span>
                <span>
                  -{formatCurrency(invoice.discountAmount, invoice.currency)}
                </span>
              </div>
            )}
            <div className="flex justify-between font-bold text-lg border-t pt-2">
              <span>Total</span>
              <span>{formatCurrency(invoice.total, invoice.currency)}</span>
            </div>
          </div>
        </div>

        {/* PDF Viewer (optional inline) */}
        {invoice.pdfUrl && (
          <div className="mt-8">
            <h3 className="font-semibold mb-4">Vista Previa</h3>
            <InvoicePDFViewer url={invoice.pdfUrl} />
          </div>
        )}
      </div>
    </MainLayout>
  );
}
```

### 3. InvoiceCard

Card de factura para la lista.

```tsx
// src/components/invoices/InvoiceCard.tsx

import { Link } from "react-router-dom";
import { Invoice } from "@/services/invoicingService";
import { NCFBadge } from "./NCFBadge";
import { InvoiceStatusBadge } from "./InvoiceStatusBadge";
import { formatCurrency, formatDate } from "@/lib/utils";
import { FiChevronRight, FiDownload } from "react-icons/fi";

interface InvoiceCardProps {
  invoice: Invoice;
}

export function InvoiceCard({ invoice }: InvoiceCardProps) {
  return (
    <Link
      to={`/invoices/${invoice.id}`}
      className="block bg-white border rounded-lg p-4 hover:shadow-md transition-shadow"
    >
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-2">
            <span className="font-semibold">{invoice.invoiceNumber}</span>
            <NCFBadge ncf={invoice.ncf} size="sm" />
            <InvoiceStatusBadge status={invoice.status} size="sm" />
          </div>
          <div className="text-sm text-gray-500">
            <span>{formatDate(invoice.issueDate)}</span>
            {invoice.items[0] && (
              <span className="ml-3">{invoice.items[0].description}</span>
            )}
          </div>
        </div>

        <div className="flex items-center gap-4">
          <div className="text-right">
            <div className="font-bold text-lg">
              {formatCurrency(invoice.total, invoice.currency)}
            </div>
            <div className="text-xs text-gray-500">
              ITBIS: {formatCurrency(invoice.taxAmount, invoice.currency)}
            </div>
          </div>
          <FiChevronRight className="text-gray-400" />
        </div>
      </div>
    </Link>
  );
}
```

### 4. NCFBadge

Badge que muestra el NCF formateado.

```tsx
// src/components/invoices/NCFBadge.tsx

import { formatNCF, getNCFPrefix } from "@/services/invoicingService";
import { cn } from "@/lib/utils";

interface NCFBadgeProps {
  ncf: string;
  size?: "sm" | "md" | "lg";
  showPrefix?: boolean;
}

export function NCFBadge({
  ncf,
  size = "md",
  showPrefix = true,
}: NCFBadgeProps) {
  const formatted = formatNCF(ncf);
  const prefix = ncf.slice(0, 3);

  const prefixColors: Record<string, string> = {
    B01: "bg-blue-100 text-blue-800", // Consumidor Final
    B02: "bg-green-100 text-green-800", // Crédito Fiscal
    B03: "bg-yellow-100 text-yellow-800", // Nota de Débito
    B04: "bg-red-100 text-red-800", // Nota de Crédito
    B14: "bg-purple-100 text-purple-800", // Gubernamental
    B15: "bg-indigo-100 text-indigo-800", // Régimen Especial
  };

  const sizeClasses = {
    sm: "text-xs px-2 py-0.5",
    md: "text-sm px-2.5 py-1",
    lg: "text-base px-3 py-1.5",
  };

  return (
    <span
      className={cn(
        "inline-flex items-center font-mono rounded-md",
        prefixColors[prefix] || "bg-gray-100 text-gray-800",
        sizeClasses[size],
      )}
    >
      {showPrefix && <span className="font-bold mr-1">{prefix}</span>}
      <span>{formatted.slice(4)}</span>
    </span>
  );
}
```

### 5. InvoiceStatusBadge

Badge de estado de factura.

```tsx
// src/components/invoices/InvoiceStatusBadge.tsx

import {
  InvoiceStatus,
  getInvoiceStatusLabel,
} from "@/services/invoicingService";
import { cn } from "@/lib/utils";

interface InvoiceStatusBadgeProps {
  status: InvoiceStatus;
  size?: "sm" | "md";
}

export function InvoiceStatusBadge({
  status,
  size = "md",
}: InvoiceStatusBadgeProps) {
  const statusConfig: Record<InvoiceStatus, { color: string; icon: string }> = {
    [InvoiceStatus.Draft]: { color: "bg-gray-100 text-gray-800", icon: "📝" },
    [InvoiceStatus.Issued]: { color: "bg-blue-100 text-blue-800", icon: "📄" },
    [InvoiceStatus.Sent]: {
      color: "bg-indigo-100 text-indigo-800",
      icon: "📧",
    },
    [InvoiceStatus.Paid]: { color: "bg-green-100 text-green-800", icon: "✅" },
    [InvoiceStatus.PartiallyPaid]: {
      color: "bg-yellow-100 text-yellow-800",
      icon: "⏳",
    },
    [InvoiceStatus.Overdue]: { color: "bg-red-100 text-red-800", icon: "⚠️" },
    [InvoiceStatus.Voided]: { color: "bg-gray-200 text-gray-600", icon: "🚫" },
    [InvoiceStatus.Cancelled]: { color: "bg-red-200 text-red-800", icon: "❌" },
  };

  const config = statusConfig[status];
  const label = getInvoiceStatusLabel(status);

  const sizeClasses = {
    sm: "text-xs px-2 py-0.5",
    md: "text-sm px-2.5 py-1",
  };

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full font-medium",
        config.color,
        sizeClasses[size],
      )}
    >
      <span className="mr-1">{config.icon}</span>
      {label}
    </span>
  );
}
```

### 6. InvoiceFilters

Filtros para la lista de facturas.

```tsx
// src/components/invoices/InvoiceFilters.tsx

import { InvoiceStatus } from "@/services/invoicingService";
import { Select } from "@/components/ui/Select";
import { DateRangePicker } from "@/components/ui/DateRangePicker";

interface InvoiceFiltersProps {
  filters: {
    status?: InvoiceStatus;
    startDate?: string;
    endDate?: string;
  };
  onChange: (filters: any) => void;
}

export function InvoiceFilters({ filters, onChange }: InvoiceFiltersProps) {
  return (
    <div className="flex flex-wrap gap-4 mb-6">
      <Select
        label="Estado"
        value={filters.status || ""}
        onChange={(e) =>
          onChange({ ...filters, status: e.target.value || undefined })
        }
        options={[
          { value: "", label: "Todos" },
          { value: InvoiceStatus.Paid, label: "Pagadas" },
          { value: InvoiceStatus.Issued, label: "Emitidas" },
          { value: InvoiceStatus.Sent, label: "Enviadas" },
          { value: InvoiceStatus.Voided, label: "Anuladas" },
        ]}
      />

      <DateRangePicker
        startDate={filters.startDate}
        endDate={filters.endDate}
        onChange={(start, end) =>
          onChange({ ...filters, startDate: start, endDate: end })
        }
      />
    </div>
  );
}
```

---

## 🔐 Admin: Gestión NCF y Reportes DGII

### AdminInvoicesPage

Gestión de todas las facturas del sistema.

```tsx
// src/pages/admin/invoices/AdminInvoicesPage.tsx

import { useState } from "react";
import { useInvoices, useVoidInvoice } from "@/hooks/useInvoices";
import { AdminLayout } from "@/layouts/AdminLayout";
import { DataTable } from "@/components/admin/DataTable";
import { VoidInvoiceModal } from "@/components/admin/invoices/VoidInvoiceModal";

export function AdminInvoicesPage() {
  const [filters, setFilters] = useState({ page: 1, pageSize: 20 });
  const { data, isLoading } = useInvoices(filters);
  const voidInvoice = useVoidInvoice();

  const columns = [
    { header: "Número", accessor: "invoiceNumber" },
    { header: "NCF", accessor: "ncf", render: (v) => <NCFBadge ncf={v} /> },
    { header: "Cliente", accessor: "customerName" },
    {
      header: "Total",
      accessor: "total",
      render: (v, row) => formatCurrency(v, row.currency),
    },
    {
      header: "Estado",
      accessor: "status",
      render: (v) => <InvoiceStatusBadge status={v} />,
    },
    { header: "Fecha", accessor: "issueDate", render: (v) => formatDate(v) },
    {
      header: "Acciones",
      render: (_, row) => (
        <div className="flex gap-2">
          <Button size="sm" variant="outline" asChild>
            <Link to={`/admin/invoices/${row.id}`}>Ver</Link>
          </Button>
          {row.status !== InvoiceStatus.Voided && (
            <Button
              size="sm"
              variant="destructive"
              onClick={() => openVoidModal(row)}
            >
              Anular
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <AdminLayout title="Facturas">
      <div className="space-y-6">
        <AdminInvoiceFilters filters={filters} onChange={setFilters} />
        <DataTable
          columns={columns}
          data={data?.data || []}
          loading={isLoading}
        />
        <Pagination
          {...data}
          onPageChange={(page) => setFilters({ ...filters, page })}
        />
      </div>
    </AdminLayout>
  );
}
```

### AdminNCFSequencesPage

Gestión de secuencias NCF autorizadas por DGII.

```tsx
// src/pages/admin/invoices/AdminNCFSequencesPage.tsx

import {
  useNCFSequences,
  useCreateNCFSequence,
  useActivateNCFSequence,
} from "@/hooks/useInvoices";
import { AdminLayout } from "@/layouts/AdminLayout";
import { Progress } from "@/components/ui/Progress";

export function AdminNCFSequencesPage() {
  const { data: sequences, isLoading } = useNCFSequences();
  const createSequence = useCreateNCFSequence();
  const activateSequence = useActivateNCFSequence();

  return (
    <AdminLayout title="Secuencias NCF">
      <div className="space-y-6">
        {/* Alert si alguna secuencia está por agotarse */}
        {sequences?.some((s) => s.usagePercentage > 80) && (
          <Alert variant="warning">
            ⚠️ Algunas secuencias NCF están por agotarse. Solicite nuevas a la
            DGII.
          </Alert>
        )}

        <div className="grid gap-4">
          {sequences?.map((seq) => (
            <div key={seq.id} className="bg-white border rounded-lg p-4">
              <div className="flex justify-between items-start mb-4">
                <div>
                  <h3 className="font-bold text-lg">
                    {seq.prefix} - {getNCFTypeName(seq.type)}
                  </h3>
                  <p className="text-sm text-gray-500">
                    Autorizado: {formatDate(seq.authorizationDate)} - Vence:{" "}
                    {formatDate(seq.expirationDate)}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  {seq.isActive ? (
                    <Badge color="green">Activa</Badge>
                  ) : (
                    <Button
                      size="sm"
                      onClick={() => activateSequence.mutate(seq.id)}
                    >
                      Activar
                    </Button>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span>Usados: {seq.currentNumber - seq.startNumber}</span>
                  <span>Disponibles: {seq.endNumber - seq.currentNumber}</span>
                </div>
                <Progress
                  value={seq.usagePercentage}
                  className={
                    seq.usagePercentage > 80 ? "bg-red-500" : "bg-blue-500"
                  }
                />
                <p className="text-xs text-gray-500">
                  Rango: {seq.startNumber} - {seq.endNumber}
                </p>
              </div>
            </div>
          ))}
        </div>

        <Button onClick={() => openCreateModal()}>
          + Agregar Nueva Secuencia
        </Button>
      </div>
    </AdminLayout>
  );
}
```

### AdminDGIIReportsPage

Generación de reportes 606/607 para DGII.

```tsx
// src/pages/admin/dgii/AdminDGIIReportsPage.tsx

import { useState } from "react";
import {
  useDGIIReports,
  useGenerateDGIIReport,
  useDownloadDGIIReport,
} from "@/hooks/useInvoices";
import { AdminLayout } from "@/layouts/AdminLayout";

export function AdminDGIIReportsPage() {
  const [year, setYear] = useState(new Date().getFullYear());
  const [month, setMonth] = useState(new Date().getMonth() + 1);

  const { data: reports, isLoading } = useDGIIReports(year, month);
  const generateReport = useGenerateDGIIReport();
  const downloadReport = useDownloadDGIIReport();

  return (
    <AdminLayout title="Reportes DGII">
      <div className="space-y-6">
        {/* Selector de período */}
        <div className="flex gap-4 items-end">
          <Select
            label="Año"
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            options={[2024, 2025, 2026].map((y) => ({
              value: y,
              label: String(y),
            }))}
          />
          <Select
            label="Mes"
            value={month}
            onChange={(e) => setMonth(Number(e.target.value))}
            options={MONTHS.map((m, i) => ({ value: i + 1, label: m }))}
          />
        </div>

        {/* Botones de generación */}
        <div className="flex gap-4">
          <Button
            onClick={() => generateReport.mutate({ type: "606", year, month })}
            loading={generateReport.isPending}
          >
            Generar Formato 606 (Compras)
          </Button>
          <Button
            onClick={() => generateReport.mutate({ type: "607", year, month })}
            loading={generateReport.isPending}
          >
            Generar Formato 607 (Ventas)
          </Button>
        </div>

        {/* Lista de reportes generados */}
        <div className="space-y-4">
          {reports?.map((report) => (
            <div
              key={report.id}
              className="bg-white border rounded-lg p-4 flex justify-between items-center"
            >
              <div>
                <h3 className="font-semibold">
                  Formato {report.type} - {report.period}
                </h3>
                <p className="text-sm text-gray-500">
                  {report.recordCount} registros · Total:{" "}
                  {formatCurrency(report.totalAmount, "DOP")} · ITBIS:{" "}
                  {formatCurrency(report.totalTax, "DOP")}
                </p>
                {report.generatedAt && (
                  <p className="text-xs text-gray-400">
                    Generado: {formatDateTime(report.generatedAt)}
                  </p>
                )}
              </div>

              <div className="flex gap-2">
                <DGIIReportStatusBadge status={report.status} />
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => downloadReport.mutate(report.id)}
                >
                  <FiDownload className="mr-1" />
                  Descargar TXT
                </Button>
              </div>
            </div>
          ))}
        </div>

        {/* Info sobre formatos */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h4 className="font-semibold text-blue-800 mb-2">
            📋 Sobre los Formatos DGII
          </h4>
          <ul className="text-sm text-blue-700 space-y-1">
            <li>
              <strong>606:</strong> Compras de bienes y servicios (para OKLA
              generalmente vacío)
            </li>
            <li>
              <strong>607:</strong> Ventas de bienes y servicios (facturas
              emitidas a clientes)
            </li>
            <li>Fecha límite de envío: Día 20 del mes siguiente</li>
          </ul>
        </div>
      </div>
    </AdminLayout>
  );
}
```

---

## � Notas de Crédito (B04)

### CreditNotesListPage

Lista de notas de crédito emitidas.

```tsx
// src/pages/billing/CreditNotesListPage.tsx

import { useCreditNotes } from "@/hooks/useInvoices";
import { CreditNoteCard } from "@/components/invoices/CreditNoteCard";

export function CreditNotesListPage() {
  const { data, isLoading } = useCreditNotes();

  return (
    <MainLayout>
      <div className="max-w-4xl mx-auto px-4 py-8">
        <h1 className="text-2xl font-bold mb-6">Notas de Crédito</h1>

        {data?.data.length === 0 ? (
          <EmptyState
            icon={FiFileText}
            title="No hay notas de crédito"
            description="Las notas de crédito aparecerán aquí cuando se procesen reembolsos"
          />
        ) : (
          <div className="space-y-4">
            {data?.data.map((cn) => (
              <CreditNoteCard key={cn.id} creditNote={cn} />
            ))}
          </div>
        )}
      </div>
    </MainLayout>
  );
}
```

### CreditNoteCard

Card para mostrar una nota de crédito.

```tsx
// src/components/invoices/CreditNoteCard.tsx

interface CreditNoteCardProps {
  creditNote: CreditNote;
}

export function CreditNoteCard({ creditNote }: CreditNoteCardProps) {
  return (
    <div className="bg-white border rounded-lg p-4">
      <div className="flex justify-between items-start">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <NCFBadge ncf={creditNote.ncf} />
            <span className="font-mono text-sm text-gray-500">
              Ref: {creditNote.originalInvoiceNcf}
            </span>
          </div>
          <p className="text-gray-600">{creditNote.reason}</p>
        </div>

        <div className="text-right">
          <div className="text-lg font-bold text-red-600">
            -{formatCurrency(creditNote.amount, creditNote.currency)}
          </div>
          <p className="text-xs text-gray-500">
            {formatDate(creditNote.issueDate)}
          </p>
        </div>
      </div>

      <div className="flex gap-2 mt-4">
        <Button variant="outline" size="sm" asChild>
          <Link to={`/billing/credit-notes/${creditNote.id}`}>
            Ver Detalles
          </Link>
        </Button>
        <Button variant="outline" size="sm">
          <FiDownload className="mr-1" /> PDF
        </Button>
      </div>
    </div>
  );
}
```

### IssueCreditNoteModal (Admin)

Modal para emitir nota de crédito desde admin.

```tsx
// src/components/admin/invoices/IssueCreditNoteModal.tsx

interface IssueCreditNoteModalProps {
  invoice: Invoice;
  isOpen: boolean;
  onClose: () => void;
}

export function IssueCreditNoteModal({
  invoice,
  isOpen,
  onClose,
}: IssueCreditNoteModalProps) {
  const [reason, setReason] = useState<CreditNoteReason>(
    CreditNoteReason.Refund,
  );
  const [amount, setAmount] = useState(invoice.total);
  const [description, setDescription] = useState("");

  const issueCreditNote = useIssueCreditNote();

  const handleSubmit = async () => {
    await issueCreditNote.mutateAsync({
      originalInvoiceId: invoice.id,
      reason,
      amount,
      description,
    });
    onClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Emitir Nota de Crédito</DialogTitle>
          <DialogDescription>
            Se emitirá NCF B04 referenciando la factura {invoice.ncf}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Factura original */}
          <div className="bg-gray-50 p-3 rounded">
            <p className="text-sm">
              <strong>Factura original:</strong> {invoice.ncf}
            </p>
            <p className="text-sm">
              <strong>Monto original:</strong>{" "}
              {formatCurrency(invoice.total, invoice.currency)}
            </p>
          </div>

          {/* Razón */}
          <Select
            label="Motivo de la Nota de Crédito"
            value={reason}
            onChange={(e) => setReason(e.target.value as CreditNoteReason)}
            options={[
              { value: CreditNoteReason.Refund, label: "Reembolso" },
              {
                value: CreditNoteReason.Correction,
                label: "Corrección de datos",
              },
              {
                value: CreditNoteReason.PartialRefund,
                label: "Reembolso parcial",
              },
              { value: CreditNoteReason.Discount, label: "Descuento aplicado" },
            ]}
          />

          {/* Monto */}
          <div>
            <label className="block text-sm font-medium mb-1">Monto</label>
            <CurrencyInput
              value={amount}
              onChange={setAmount}
              max={invoice.total}
              currency={invoice.currency}
            />
            {amount < invoice.total && (
              <p className="text-xs text-yellow-600 mt-1">
                Nota de crédito parcial por{" "}
                {formatCurrency(amount, invoice.currency)}
              </p>
            )}
          </div>

          {/* Descripción */}
          <Textarea
            label="Descripción (aparecerá en la nota)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Ej: Reembolso por cancelación de suscripción"
            required
          />
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancelar
          </Button>
          <Button
            variant="destructive"
            onClick={handleSubmit}
            loading={issueCreditNote.isPending}
          >
            Emitir Nota de Crédito B04
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

---

## 📊 Admin Dashboard Fiscal

Dashboard consolidado para finanzas.

```tsx
// src/pages/admin/fiscal/FiscalDashboardPage.tsx

import {
  useFiscalSummary,
  useNCFSequences,
  useUpcomingObligations,
} from "@/hooks/useInvoices";

export function FiscalDashboardPage() {
  const { data: summary } = useFiscalSummary();
  const { data: sequences } = useNCFSequences();
  const { data: obligations } = useUpcomingObligations();

  const lowSequences = sequences?.filter((s) => s.usagePercentage > 80) || [];

  return (
    <AdminLayout title="Dashboard Fiscal">
      <div className="space-y-6">
        {/* Alertas de secuencias */}
        {lowSequences.length > 0 && (
          <Alert variant="warning">
            <FiAlertTriangle className="mr-2" />
            <strong>Secuencias NCF por agotarse:</strong>
            {lowSequences.map((s) => (
              <span key={s.id} className="ml-2">
                {s.prefix} ({100 - s.usagePercentage}% disponible)
              </span>
            ))}
            <Link to="/admin/ncf-sequences" className="ml-4 underline">
              Gestionar
            </Link>
          </Alert>
        )}

        {/* Stats del mes */}
        <div className="grid md:grid-cols-4 gap-4">
          <StatCard
            title="Facturas Emitidas"
            value={summary?.invoicesCount || 0}
            subtitle={`Total: ${formatCurrency(summary?.invoicesTotal, "DOP")}`}
            icon={FiFileText}
          />
          <StatCard
            title="ITBIS Cobrado"
            value={formatCurrency(summary?.itbisCollected, "DOP")}
            subtitle={`+${summary?.itbisGrowth}% vs mes anterior`}
            trend="up"
            icon={FiDollarSign}
          />
          <StatCard
            title="Notas de Crédito"
            value={summary?.creditNotesCount || 0}
            subtitle={`Total: ${formatCurrency(summary?.creditNotesTotal, "DOP")}`}
            icon={FiMinusCircle}
          />
          <StatCard
            title="NCF B02 (Crédito Fiscal)"
            value={summary?.b02Count || 0}
            subtitle={`${summary?.b02Percentage}% del total`}
            icon={FiBuilding}
          />
        </div>

        {/* Secuencias NCF */}
        <div className="bg-white border rounded-lg p-6">
          <div className="flex justify-between items-center mb-4">
            <h3 className="font-semibold text-lg">Secuencias NCF</h3>
            <Button variant="outline" size="sm" asChild>
              <Link to="/admin/ncf-sequences">Ver Todas</Link>
            </Button>
          </div>

          <div className="space-y-4">
            {sequences?.slice(0, 3).map((seq) => (
              <div key={seq.id} className="flex items-center gap-4">
                <NCFBadge ncf={`${seq.prefix}00000001`} size="lg" />
                <div className="flex-1">
                  <Progress
                    value={seq.usagePercentage}
                    className={seq.usagePercentage > 80 ? "bg-red-500" : ""}
                  />
                </div>
                <span className="text-sm text-gray-500 w-20 text-right">
                  {seq.endNumber - seq.currentNumber} disponibles
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Obligaciones próximas */}
        <div className="bg-white border rounded-lg p-6">
          <h3 className="font-semibold text-lg mb-4">
            📅 Próximas Obligaciones
          </h3>
          <div className="space-y-3">
            {obligations?.map((ob) => (
              <div
                key={ob.id}
                className="flex items-center justify-between p-3 bg-gray-50 rounded"
              >
                <div className="flex items-center gap-3">
                  <div
                    className={`w-2 h-2 rounded-full ${
                      ob.daysUntil <= 5
                        ? "bg-red-500"
                        : ob.daysUntil <= 10
                          ? "bg-yellow-500"
                          : "bg-green-500"
                    }`}
                  />
                  <div>
                    <p className="font-medium">{ob.name}</p>
                    <p className="text-sm text-gray-500">
                      {formatDate(ob.dueDate)}
                    </p>
                  </div>
                </div>
                <span className="text-sm">
                  {ob.daysUntil <= 0 ? "⚠️ Vencido" : `En ${ob.daysUntil} días`}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Botones de acción */}
        <div className="flex gap-4">
          <Button asChild>
            <Link to="/admin/dgii/reports">📥 Generar Reportes DGII</Link>
          </Button>
          <Button variant="outline" asChild>
            <Link to="/admin/invoices">Ver Todas las Facturas</Link>
          </Button>
        </div>
      </div>
    </AdminLayout>
  );
}
```

---

## ⚠️ NCF Sequence Alerts Component

Componente para alertas de secuencias que se muestran en el admin.

```tsx
// src/components/admin/NCFSequenceAlerts.tsx

import { useNCFSequences } from "@/hooks/useInvoices";
import { Alert } from "@/components/ui/Alert";

export function NCFSequenceAlerts() {
  const { data: sequences } = useNCFSequences();

  const alerts = sequences?.filter((s) => s.usagePercentage >= 80) || [];

  if (alerts.length === 0) return null;

  return (
    <div className="space-y-2">
      {alerts.map((seq) => {
        const level =
          seq.usagePercentage >= 95
            ? "critical"
            : seq.usagePercentage >= 90
              ? "warning"
              : "info";

        const variant =
          level === "critical"
            ? "destructive"
            : level === "warning"
              ? "warning"
              : "default";

        return (
          <Alert key={seq.id} variant={variant}>
            <div className="flex items-center justify-between">
              <div>
                <strong>
                  {level === "critical"
                    ? "🚨 CRÍTICO: "
                    : level === "warning"
                      ? "⚠️ Advertencia: "
                      : "ℹ️ Info: "}
                </strong>
                Secuencia NCF {seq.prefix} al {seq.usagePercentage}% de uso
                <span className="ml-2 text-sm">
                  ({seq.endNumber - seq.currentNumber} disponibles)
                </span>
              </div>
              <Button variant="outline" size="sm" asChild>
                <Link to="/admin/ncf-sequences">Gestionar</Link>
              </Button>
            </div>
          </Alert>
        );
      })}
    </div>
  );
}
```

---

## 🔌 Integración API

### Endpoints Principales

| Método | Endpoint                       | Descripción           | Auth | Roles        |
| ------ | ------------------------------ | --------------------- | ---- | ------------ |
| `GET`  | `/api/invoices`                | Listar facturas       | ✅   | User, Dealer |
| `GET`  | `/api/invoices/{id}`           | Obtener factura       | ✅   | Owner        |
| `GET`  | `/api/invoices/{id}/pdf`       | Descargar PDF         | ✅   | Owner        |
| `POST` | `/api/invoices/{id}/send`      | Enviar por email      | ✅   | Owner        |
| `POST` | `/api/invoices/{id}/void`      | Anular factura        | ✅   | Admin        |
| `GET`  | `/api/credit-notes`            | Listar notas crédito  | ✅   | User, Dealer |
| `POST` | `/api/credit-notes`            | Emitir nota crédito   | ✅   | Admin        |
| `GET`  | `/api/invoices/ncf-sequences`  | Ver secuencias NCF    | ✅   | Admin        |
| `POST` | `/api/invoices/ncf-sequences`  | Crear secuencia       | ✅   | Admin        |
| `GET`  | `/api/dgii/validate-rnc/{rnc}` | Validar RNC           | ✅   | User         |
| `POST` | `/api/dgii/report/606`         | Generar reporte       | ✅   | Admin        |
| `POST` | `/api/dgii/report/607`         | Generar reporte       | ✅   | Admin        |
| `GET`  | `/api/fiscal/summary`          | Dashboard fiscal      | ✅   | Admin        |
| `GET`  | `/api/fiscal/obligations`      | Obligaciones próximas | ✅   | Admin        |

### Hooks React Query

```typescript
// Hooks disponibles en @/hooks/useInvoices.ts

// Facturas
useInvoices(params); // Lista con filtros
useInvoice(id); // Detalle
useDownloadInvoicePDF(); // Descargar PDF
useSendInvoiceByEmail(); // Enviar por email
useVoidInvoice(); // Anular (Admin)

// NCF
useNCFSequences(); // Lista de secuencias
useCreateNCFSequence(); // Crear secuencia
useActivateNCFSequence(); // Activar secuencia

// DGII
useDGIIReports(year, month); // Lista de reportes
useGenerateDGIIReport(); // Generar 606/607
useDownloadDGIIReport(); // Descargar TXT
useValidateRNC(rnc); // Validar RNC
useValidateNCF(ncf); // Validar NCF
```

---

## 📊 Tipos de NCF

### Prefijos Soportados

| Prefijo | Tipo             | Descripción                                    |
| ------- | ---------------- | ---------------------------------------------- |
| **B01** | Consumidor Final | Clientes sin RNC (la mayoría)                  |
| **B02** | Crédito Fiscal   | Empresas con RNC (pueden deducir ITBIS)        |
| **B04** | Nota de Crédito  | Anulación parcial o total de factura           |
| **B14** | Gubernamental    | Entidades gubernamentales                      |
| **B15** | Régimen Especial | Contribuyentes con régimen tributario especial |

### Lógica de Asignación

```typescript
function determineNCFType(customer: Customer): NCFPrefix {
  // Si tiene RNC válido → B02 (Crédito Fiscal)
  if (customer.rnc && isValidRNC(customer.rnc)) {
    // Verificar si es gubernamental
    if (isGovernmentEntity(customer.rnc)) {
      return "B14"; // Gubernamental
    }
    // Verificar régimen especial
    if (isSpecialRegime(customer.rnc)) {
      return "B15"; // Régimen Especial
    }
    return "B02"; // Crédito Fiscal estándar
  }

  // Sin RNC → B01 (Consumidor Final)
  return "B01";
}
```

---

## 🧪 Testing

### Test Cards y Escenarios

| Escenario             | Datos de Prueba             |
| --------------------- | --------------------------- |
| Factura B01           | Sin RNC, cualquier email    |
| Factura B02           | RNC: 1-31-00001-1           |
| RNC Inválido          | RNC: 1-11-11111-1           |
| Secuencia NCF agotada | Simular endNumber alcanzado |

---

## ✅ Checklist de Implementación

### Backend ✅

- [x] InvoicingService con generación NCF
- [x] Entidades Invoice, CreditNote, NCFSequence
- [x] API REST completa
- [x] Generación de PDF
- [x] Integración con DGII para validación RNC
- [x] Reportes 606/607
- [x] Notas de crédito B04

### Frontend 🚧

#### Páginas Usuario

- [ ] InvoicesListPage - Lista de facturas usuario
- [ ] InvoiceDetailPage - Detalle con PDF
- [ ] CreditNotesListPage - Lista notas de crédito

#### Componentes Facturas

- [ ] InvoiceCard component
- [ ] NCFBadge component
- [ ] InvoiceStatusBadge component
- [ ] InvoiceFilters component
- [ ] InvoicePDFViewer component

#### Componentes Notas de Crédito

- [ ] CreditNoteCard component
- [ ] IssueCreditNoteModal (Admin)

#### Admin Pages

- [ ] AdminInvoicesPage - Gestión admin
- [ ] AdminNCFSequencesPage - Secuencias NCF
- [ ] AdminDGIIReportsPage - Formatos 606/607
- [ ] FiscalDashboardPage - Dashboard consolidado
- [ ] VoidInvoiceModal
- [ ] CreateNCFSequenceModal

#### Alertas NCF

- [ ] NCFSequenceAlerts component
- [ ] Alert al 80%, 90%, 95% de uso

#### Hooks React Query

- [ ] useInvoices.ts (facturas)
- [ ] useCreditNotes (notas crédito)
- [ ] useNCFSequences (secuencias)
- [ ] useFiscalSummary (dashboard)
- [ ] useUpcomingObligations (obligaciones)
- [ ] useIssueCreditNote (emitir B04)

### Integración 🚧

- [ ] Rutas en App.tsx
- [ ] Links en Navbar/Dashboard
- [ ] Protección de rutas admin
- [ ] Error handling

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/facturacion-dgii.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Facturación DGII", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de facturación", async ({ page }) => {
    await page.goto("/admin/dgii");

    await expect(page.getByTestId("dgii-dashboard")).toBeVisible();
  });

  test("debe ver secuencias de NCF", async ({ page }) => {
    await page.goto("/admin/dgii/secuencias");

    await expect(page.getByTestId("ncf-sequences")).toBeVisible();
  });

  test("debe generar comprobante fiscal", async ({ page }) => {
    await page.goto("/admin/dgii/comprobantes/nuevo");

    await page.getByRole("combobox", { name: /tipo/i }).click();
    await page.getByRole("option", { name: /b01/i }).click();
    await page.getByRole("button", { name: /generar/i }).click();

    await expect(page.getByTestId("ncf-generated")).toBeVisible();
  });

  test("debe ver histórico de comprobantes", async ({ page }) => {
    await page.goto("/admin/dgii/comprobantes");

    await expect(page.getByTestId("comprobantes-list")).toBeVisible();
  });

  test("debe enviar reporte 607 a DGII", async ({ page }) => {
    await page.goto("/admin/dgii/reportes");

    await page.getByRole("button", { name: /generar 607/i }).click();
    await expect(page.getByText(/reporte generado/i)).toBeVisible();
  });
});
```

---

**Última actualización:** Enero 29, 2026  
**Autor:** OKLA Team  
**Versión:** 1.1.0
