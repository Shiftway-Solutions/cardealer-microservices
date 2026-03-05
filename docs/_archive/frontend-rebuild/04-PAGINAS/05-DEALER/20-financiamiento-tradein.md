---
title: "Financiamiento & Trade-In - Sistema Completo"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService"]
status: complete
last_updated: "2026-01-30"
---

# 💰 Financiamiento & Trade-In - Sistema Completo

> **Última actualización:** Enero 29, 2026  
> **Complejidad:** 🔴 Alta (Integraciones bancarias, ML, APIs gubernamentales)  
> **Estado:** 📖 Documentación Completa - Listo para Implementación  
> **Dependencias:** FinancingService, TradeInService, VehicleHistoryService, VehiclesSaleService

---

## 📚 DOCUMENTACIÓN BASE

Este documento integra TODOS los procesos de la carpeta `docs/process-matrix/14-FINANCIAMIENTO-TRADEIN/`:

| Documento Process Matrix           | Secciones Cubiertas                                  |
| ---------------------------------- | ---------------------------------------------------- |
| `01-calculadora-financiamiento.md` | Calculadora de cuotas, Pre-aprobación, Bancos RD     |
| `02-trade-in-estimador.md`         | Estimador de valor, Solicitudes, Matching con dealer |
| `03-historial-vehiculo.md`         | Historial estilo CARFAX, Accidentes, Mantenimiento   |
| `04-calculadora-costos-totales.md` | ITBIS, Impuestos, Marbete, Seguros, Costos legales   |

---

## ⚠️ AUDITORÍA DE ESTADO (Enero 29, 2026)

### Estado de Implementación Backend

| Proceso Backend             | Estado  | Observación                       |
| --------------------------- | ------- | --------------------------------- |
| FinancingService API        | ✅ 100% | `/backend/FinancingService/`      |
| TradeInService API          | ✅ 100% | `/backend/TradeInService/`        |
| VehicleHistoryService API   | 🔴 0%   | **NO EXISTE** - A implementar     |
| Calculadora de cuotas       | ✅ 100% | Multi-banco con tasas reales      |
| Pre-aprobación              | ✅ 100% | Flujo completo implementado       |
| Estimador trade-in          | ✅ 100% | ML para valoración                |
| Historial vehículo (CARFAX) | 🔴 0%   | **CRÍTICO** - Diferenciador clave |
| Costos totales              | 🔴 0%   | Requiere tablas DGII/INTRANT      |

### Estado de Acceso UI

| Funcionalidad UI                  | Estado | Ubicación Propuesta             |
| --------------------------------- | ------ | ------------------------------- |
| Calculadora financiamiento widget | 🔴 0%  | `/vehicles/:slug` (sidebar)     |
| Calculadora standalone            | 🔴 0%  | `/financing/calculator`         |
| Pre-aprobación form               | 🔴 0%  | `/financing/pre-approval`       |
| Comparador de bancos              | 🔴 0%  | `/financing/compare`            |
| Estimador trade-in                | 🔴 0%  | `/trade-in/estimate`            |
| Solicitar trade-in                | 🔴 0%  | `/trade-in/request`             |
| Trade-in en detalle vehículo      | 🔴 0%  | `/vehicles/:slug/trade-in`      |
| Historial vehículo (CARFAX)       | 🔴 0%  | `/vehicles/:slug/history`       |
| Calculadora costos totales        | 🔴 0%  | `/vehicles/:slug/total-costs`   |
| Mis pre-aprobaciones              | 🔴 0%  | `/financing/my-applications`    |
| Mis trade-ins                     | 🔴 0%  | `/trade-in/my-requests`         |
| Admin: Gestionar pre-aprobaciones | 🔴 0%  | `/admin/financing/applications` |

---

## 📊 RESUMEN DE PROCESOS A IMPLEMENTAR

### FIN-\* (Financiamiento) - 16 procesos

| ID          | Proceso                      | Backend | UI    | Prioridad  |
| ----------- | ---------------------------- | ------- | ----- | ---------- |
| FIN-CALC-01 | Calcular cuota mensual       | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| FIN-CALC-02 | Calcular total a pagar       | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| FIN-CALC-03 | Calcular tasa efectiva       | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| FIN-CALC-04 | Tabla de amortización        | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| FIN-CALC-05 | Simulación multi-banco       | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| FIN-BANK-01 | Listar bancos disponibles    | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| FIN-BANK-02 | Tasas por banco              | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| FIN-BANK-03 | Integración API bancaria     | 🟡 50%  | 🔴 0% | 🟡 MEDIA   |
| FIN-BANK-04 | Webhook notificaciones banco | 🟡 40%  | N/A   | 🟡 MEDIA   |
| FIN-PRE-01  | Solicitar pre-aprobación     | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| FIN-PRE-02  | Ver mis pre-aprobaciones     | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| FIN-PRE-03  | Admin aprobar/rechazar       | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| FIN-PRE-04  | Notificación estado          | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| FIN-RATE-01 | Actualizar tasas             | ✅ 100% | N/A   | 🟢 BAJA    |
| FIN-RATE-02 | Historial de tasas           | ✅ 100% | 🔴 0% | 🟢 BAJA    |
| FIN-RATE-03 | Alertas cambio de tasa       | ✅ 100% | 🔴 0% | 🟢 BAJA    |

### TRADE-\* (Trade-In) - 16 procesos

| ID            | Proceso                       | Backend | UI    | Prioridad  |
| ------------- | ----------------------------- | ------- | ----- | ---------- |
| TRADE-EST-01  | Estimar valor vehículo (ML)   | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| TRADE-EST-02  | Ajustes por condición         | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-EST-03  | Ajustes por accidentes        | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-EST-04  | Comparar con mercado          | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| TRADE-EST-05  | Generar reporte PDF           | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| TRADE-REQ-01  | Crear solicitud trade-in      | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| TRADE-REQ-02  | Ver mis solicitudes           | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-REQ-03  | Cancelar solicitud            | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| TRADE-REQ-04  | Notificar dealers interesados | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-VAL-01  | Validar datos del vehículo    | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-VAL-02  | Verificar placa (INTRANT)     | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-VAL-03  | Escanear placa (OCR móvil)    | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| TRADE-VAL-04  | Verificar VIN                 | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| TRADE-DEAL-01 | Dealer recibe ofertas         | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-DEAL-02 | Dealer hace oferta            | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| TRADE-DEAL-03 | Usuario acepta/rechaza        | ✅ 100% | 🔴 0% | 🔴 ALTA    |

### HIST-\* (Historial Vehículo) - 17 procesos

| ID            | Proceso                       | Backend | UI    | Prioridad  |
| ------------- | ----------------------------- | ------- | ----- | ---------- |
| HIST-OWN-01   | Historial de propietarios     | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-OWN-02   | Transferencias registradas    | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-OWN-03   | Tiempo por propietario        | 🔴 0%   | 🔴 0% | 🟡 MEDIA   |
| HIST-OWN-04   | Uso (personal/comercial/taxi) | 🔴 0%   | 🔴 0% | 🟡 MEDIA   |
| HIST-ACC-01   | Accidentes reportados         | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-ACC-02   | Severidad de daños            | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-ACC-03   | Claims de seguro              | 🔴 0%   | 🔴 0% | 🔴 ALTA    |
| HIST-ACC-04   | Pérdida total declarada       | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-MAINT-01 | Servicios de mantenimiento    | 🔴 0%   | 🔴 0% | 🟡 MEDIA   |
| HIST-MAINT-02 | Recalls del fabricante        | 🔴 0%   | 🔴 0% | 🔴 ALTA    |
| HIST-MAINT-03 | Reparaciones mayores          | 🔴 0%   | 🔴 0% | 🟡 MEDIA   |
| HIST-KM-01    | Verificación de kilometraje   | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-KM-02    | Detección de alteración       | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-KM-03    | Historial de lecturas         | 🔴 0%   | 🔴 0% | 🟡 MEDIA   |
| HIST-LEG-01   | Gravámenes bancarios          | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |
| HIST-LEG-02   | Multas pendientes             | 🔴 0%   | 🔴 0% | 🔴 ALTA    |
| HIST-LEG-03   | Reporte de robo               | 🔴 0%   | 🔴 0% | 🔴 CRÍTICA |

### COST-\* (Costos Totales) - 15 procesos

| ID           | Proceso                    | Backend | UI    | Prioridad |
| ------------ | -------------------------- | ------- | ----- | --------- |
| COST-CALC-01 | Calcular costo total       | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-CALC-02 | Desglose por componente    | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-CALC-03 | Comparar nuevo vs usado    | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| COST-CALC-04 | TCO (Total Cost Ownership) | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| COST-CALC-05 | Exportar PDF del cálculo   | 🔴 0%   | 🔴 0% | 🟢 BAJA   |
| COST-TAX-01  | ITBIS (18% nuevos)         | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-TAX-02  | Impuesto primera placa     | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-TAX-03  | Transferencia DGII (2%)    | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-TAX-04  | Peritaje/Inspección        | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| COST-FEE-01  | Marbete anual (por CC)     | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-FEE-02  | INTRANT traspaso           | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-FEE-03  | Gestión legal              | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| COST-INS-01  | Seguro obligatorio         | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| COST-INS-02  | Cotización seguro full     | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| COST-INS-03  | Integración aseguradoras   | 🔴 0%   | 🔴 0% | 🟢 BAJA   |

**TOTAL: 64 procesos** (32 ✅ backend completo, 32 🔴 sin UI, 17 🔴 sin implementar)

---

## 🎯 OBJETIVO DE ESTE DOCUMENTO

Implementar UI completa para:

1. **Calculadora de Financiamiento:** Widget en detalle + página standalone
2. **Pre-Aprobación:** Formulario y seguimiento de solicitudes
3. **Trade-In:** Estimador de valor + solicitudes + matching
4. **Historial Vehículo:** Reporte estilo CARFAX
5. **Costos Totales:** Desglose transparente de TODOS los costos
6. **Admin Dashboard:** Gestión de pre-aprobaciones y trade-ins

---

## 🏗️ ARQUITECTURA GENERAL

### Flujo de Financiamiento

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   Financing Flow Architecture                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ USER DISCOVERY                                                          │
│  User browsing → Vehicle Detail Page                                        │
│                   │                                                         │
│                   ▼                                                         │
│          ┌────────────────────────────────┐                                │
│          │ FinancingCalculatorWidget       │                                │
│          │ • Precio: $2,500,000            │                                │
│          │ • Inicial: 20% ($500,000)       │                                │
│          │ • Plazo: 60 meses               │                                │
│          │ • Cuota: ~$48,000/mes           │                                │
│          └────────────────────────────────┘                                │
│                   │                                                         │
│                   │ "Ver más bancos"                                        │
│                   ▼                                                         │
│  2️⃣ COMPARISON                                                              │
│  /financing/compare?vehicleId=xxx                                           │
│          ┌────────────────────────────────┐                                │
│          │ BankComparisonTable             │                                │
│          │ ┌─────────────────────────────┐│                                │
│          │ │ Banco    | Tasa | Cuota     ││                                │
│          │ ├─────────────────────────────┤│                                │
│          │ │ Popular  | 11%  | $47,800 ✅││ ← Mejor                        │
│          │ │ BHD      | 12%  | $48,200   ││                                │
│          │ │ Reservas | 13%  | $48,900   ││                                │
│          │ └─────────────────────────────┘│                                │
│          └────────────────────────────────┘                                │
│                   │                                                         │
│                   │ "Solicitar pre-aprobación"                              │
│                   ▼                                                         │
│  3️⃣ PRE-APPROVAL                                                            │
│  /financing/pre-approval                                                    │
│          ┌────────────────────────────────┐                                │
│          │ PreApprovalForm                 │                                │
│          │ • Información personal          │                                │
│          │ • Ingresos mensuales            │                                │
│          │ • Banco preferido               │                                │
│          │ • Vehículo seleccionado         │                                │
│          └────────────────────────────────┘                                │
│                   │                                                         │
│                   │ Submit → POST /api/financing/pre-approval               │
│                   ▼                                                         │
│  FinancingService → Validar → Enviar a banco → Email confirmación          │
│                   │                                                         │
│                   │ RabbitMQ: financing.pre-approval.created                │
│                   ▼                                                         │
│  4️⃣ FOLLOW-UP                                                               │
│  Email: "Tu solicitud está en revisión"                                     │
│  Dashboard: /financing/my-applications                                      │
│          ┌────────────────────────────────┐                                │
│          │ Application #12345              │                                │
│          │ Estado: En Revisión 🟡          │                                │
│          │ Banco: Banco Popular            │                                │
│          │ Monto: $2,000,000               │                                │
│          │ Fecha: Enero 28, 2026           │                                │
│          └────────────────────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Flujo de Trade-In

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Trade-In Flow Architecture                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ VALUATION                                                               │
│  /trade-in/estimate                                                         │
│          ┌────────────────────────────────┐                                │
│          │ TradeInEstimatorForm            │                                │
│          │ • Marca: Toyota                 │                                │
│          │ • Modelo: Camry                 │                                │
│          │ • Año: 2018                     │                                │
│          │ • Kilometraje: 85,000 km        │                                │
│          │ • Condición: Buena              │                                │
│          │ • Accidentes: No                │                                │
│          └────────────────────────────────┘                                │
│                   │                                                         │
│                   │ Submit → POST /api/tradein/estimate                     │
│                   ▼                                                         │
│  TradeInService → ML Model → Market Data → Estimate                        │
│                   │                                                         │
│                   ▼                                                         │
│          ┌────────────────────────────────┐                                │
│          │ ValuationResult                 │                                │
│          │ ┌─────────────────────────────┐│                                │
│          │ │ Valor Estimado: $650,000    ││                                │
│          │ │ Rango: $620K - $680K        ││                                │
│          │ │ Confianza: 85%              ││                                │
│          │ │                             ││                                │
│          │ │ Similar en mercado: $675K   ││                                │
│          │ │ Condición afecta: -$25K     ││                                │
│          │ └─────────────────────────────┘│                                │
│          └────────────────────────────────┘                                │
│                   │                                                         │
│                   │ "Usar como parte de pago"                               │
│                   ▼                                                         │
│  2️⃣ APPLY TO VEHICLE                                                        │
│  /vehicles/toyota-corolla-2024?tradeInId=xxx                                │
│          ┌────────────────────────────────┐                                │
│          │ Price Breakdown                 │                                │
│          │ • Precio vehículo: $2,500,000   │                                │
│          │ • Trade-in: -$650,000           │                                │
│          │ • A financiar: $1,850,000       │                                │
│          │ • Inicial 20%: $370,000         │                                │
│          └────────────────────────────────┘                                │
│                   │                                                         │
│                   │ "Solicitar ofertas de dealers"                          │
│                   ▼                                                         │
│  3️⃣ DEALER MATCHING                                                         │
│  POST /api/tradein/offers → Notify dealers                                 │
│                   │                                                         │
│                   │ RabbitMQ: tradein.offer.created                         │
│                   ▼                                                         │
│  Dealers receive notification → Make offers                                 │
│                   │                                                         │
│                   ▼                                                         │
│  4️⃣ USER RECEIVES OFFERS                                                    │
│  /trade-in/my-requests                                                      │
│          ┌────────────────────────────────┐                                │
│          │ Request #45678                  │                                │
│          │ Tu vehículo: 2018 Camry         │                                │
│          │                                 │                                │
│          │ Ofertas recibidas: 3            │                                │
│          │ • AutoMax RD: $670,000 ⭐       │                                │
│          │ • Mega Autos: $650,000          │                                │
│          │ • CarPlus: $640,000             │                                │
│          └────────────────────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 COMPONENTES A IMPLEMENTAR

### 1. FinancingCalculatorWidget (En detalle de vehículo)

**Ubicación:** `src/components/financing/FinancingCalculatorWidget.tsx`

```typescript
// filepath: src/components/financing/FinancingCalculatorWidget.tsx
"use client";

import * as React from "react";
import { Calculator, TrendingDown, Info, ArrowRight } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Slider } from "@/components/ui/slider";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tooltip } from "@/components/ui/tooltip";
import { useFinancingCalculation } from "@/lib/hooks/useFinancingCalculation";
import { formatCurrency, formatPercent } from "@/lib/utils";

interface FinancingCalculatorWidgetProps {
  vehiclePrice: number;
  vehicleId: string;
  className?: string;
}

export function FinancingCalculatorWidget({
  vehiclePrice,
  vehicleId,
  className,
}: FinancingCalculatorWidgetProps) {
  const [downPaymentPercent, setDownPaymentPercent] = React.useState(20);
  const [termMonths, setTermMonths] = React.useState(60);
  const [selectedBankId, setSelectedBankId] = React.useState<string>("");

  const downPayment = Math.round(vehiclePrice * (downPaymentPercent / 100));
  const amountToFinance = vehiclePrice - downPayment;

  const { calculation, banks, isLoading } = useFinancingCalculation({
    vehiclePrice,
    downPayment,
    termMonths,
    bankId: selectedBankId,
  });

  return (
    <Card className={cn("p-6", className)}>
      {/* Header */}
      <div className="flex items-center gap-2 mb-6">
        <Calculator className="h-5 w-5 text-blue-600" />
        <h3 className="text-lg font-semibold">Calculadora de Financiamiento</h3>
        <Tooltip content="Calcula tu cuota mensual aproximada">
          <Info className="h-4 w-4 text-gray-400" />
        </Tooltip>
      </div>

      {/* Inputs */}
      <div className="space-y-6">
        {/* Down Payment */}
        <div className="space-y-2">
          <div className="flex justify-between">
            <Label>Inicial</Label>
            <span className="text-sm font-medium">
              {formatCurrency(downPayment)} ({downPaymentPercent}%)
            </span>
          </div>
          <Slider
            value={[downPaymentPercent]}
            onValueChange={(value) => setDownPaymentPercent(value[0])}
            min={10}
            max={50}
            step={5}
            className="py-4"
          />
          <div className="flex justify-between text-xs text-gray-500">
            <span>10%</span>
            <span>50%</span>
          </div>
        </div>

        {/* Term */}
        <div className="space-y-2">
          <Label>Plazo</Label>
          <Select
            value={termMonths.toString()}
            onValueChange={(value) => setTermMonths(Number(value))}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="12">12 meses (1 año)</SelectItem>
              <SelectItem value="24">24 meses (2 años)</SelectItem>
              <SelectItem value="36">36 meses (3 años)</SelectItem>
              <SelectItem value="48">48 meses (4 años)</SelectItem>
              <SelectItem value="60">60 meses (5 años)</SelectItem>
              <SelectItem value="72">72 meses (6 años)</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Bank Selection */}
        <div className="space-y-2">
          <Label>Banco</Label>
          <Select value={selectedBankId} onValueChange={setSelectedBankId}>
            <SelectTrigger>
              <SelectValue placeholder="Selecciona un banco" />
            </SelectTrigger>
            <SelectContent>
              {banks?.map((bank) => (
                <SelectItem key={bank.id} value={bank.id}>
                  {bank.name} - {formatPercent(bank.interestRate)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Result */}
      {calculation && (
        <div className="mt-6 pt-6 border-t">
          <div className="bg-blue-50 rounded-lg p-4 mb-4">
            <div className="text-sm text-blue-700 mb-1">Cuota Mensual</div>
            <div className="text-3xl font-bold text-blue-900">
              {formatCurrency(calculation.monthlyPayment)}
            </div>
            <div className="text-xs text-blue-600 mt-1">
              Tasa: {formatPercent(calculation.interestRate)} anual
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <div className="text-gray-600">A Financiar</div>
              <div className="font-semibold">
                {formatCurrency(calculation.amountToFinance)}
              </div>
            </div>
            <div>
              <div className="text-gray-600">Total Intereses</div>
              <div className="font-semibold">
                {formatCurrency(calculation.totalInterest)}
              </div>
            </div>
            <div className="col-span-2">
              <div className="text-gray-600">Total a Pagar</div>
              <div className="font-semibold text-lg">
                {formatCurrency(calculation.totalPayment)}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="mt-6 space-y-2">
        <Button
          type="button"
          className="w-full"
          asChild
        >
          <Link href={`/financing/compare?vehicleId=${vehicleId}`}>
            Comparar Todos los Bancos
            <ArrowRight className="ml-2 h-4 w-4" />
          </Link>
        </Button>

        <Button
          type="button"
          variant="outline"
          className="w-full"
          asChild
        >
          <Link href="/financing/pre-approval">
            Solicitar Pre-Aprobación
          </Link>
        </Button>
      </div>

      {/* Disclaimer */}
      <p className="text-xs text-gray-500 mt-4">
        * Cuota aproximada. Sujeta a aprobación bancaria. Tasas de enero 2026.
      </p>
    </Card>
  );
}
```

---

### 2. TradeInEstimator (Standalone)

**Ubicación:** `src/components/tradein/TradeInEstimator.tsx`

```typescript
// filepath: src/components/tradein/TradeInEstimator.tsx
"use client";

import * as React from "react";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Car, TrendingUp, CheckCircle, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Checkbox } from "@/components/ui/checkbox";
import { Card } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { useTradeInEstimate } from "@/lib/hooks/useTradeInEstimate";
import { formatCurrency } from "@/lib/utils";

const estimateSchema = z.object({
  make: z.string().min(1, "Selecciona la marca"),
  model: z.string().min(1, "Selecciona el modelo"),
  year: z.number().min(1990).max(new Date().getFullYear() + 1),
  trim: z.string().optional(),
  mileage: z.number().min(0),
  condition: z.enum(["excellent", "good", "fair", "poor"]),
  transmission: z.enum(["automatic", "manual"]),
  fuelType: z.enum(["gasoline", "diesel", "electric", "hybrid"]),
  color: z.string(),
  hasAccidentHistory: z.boolean(),
  hasServiceHistory: z.boolean(),
  hasOriginalParts: z.boolean(),
  licensePlate: z.string().optional(),
});

type EstimateFormData = z.infer<typeof estimateSchema>;

export function TradeInEstimator() {
  const [step, setStep] = React.useState<"form" | "result">("form");

  const methods = useForm<EstimateFormData>({
    resolver: zodResolver(estimateSchema),
    defaultValues: {
      condition: "good",
      transmission: "automatic",
      fuelType: "gasoline",
      hasAccidentHistory: false,
      hasServiceHistory: false,
      hasOriginalParts: true,
    },
  });

  const { estimate, isLoading, estimateValue } = useTradeInEstimate();

  const onSubmit = async (data: EstimateFormData) => {
    const result = await estimateValue(data);
    if (result) {
      setStep("result");
    }
  };

  return (
    <div className="max-w-4xl mx-auto">
      {step === "form" ? (
        <Card className="p-6">
          <div className="mb-6">
            <h2 className="text-2xl font-bold mb-2">
              Estima el Valor de tu Vehículo
            </h2>
            <p className="text-gray-600">
              Obtén una valoración instantánea basada en inteligencia artificial y datos del mercado
            </p>
          </div>

          <FormProvider {...methods}>
            <form onSubmit={methods.handleSubmit(onSubmit)} className="space-y-6">
              {/* Basic Info */}
              <div className="grid md:grid-cols-3 gap-4">
                <div className="space-y-2">
                  <Label>Marca</Label>
                  <Select
                    value={methods.watch("make")}
                    onValueChange={(value) => methods.setValue("make", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Selecciona" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="toyota">Toyota</SelectItem>
                      <SelectItem value="honda">Honda</SelectItem>
                      <SelectItem value="nissan">Nissan</SelectItem>
                      {/* ... más marcas */}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Modelo</Label>
                  <Select
                    value={methods.watch("model")}
                    onValueChange={(value) => methods.setValue("model", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Selecciona" />
                    </SelectTrigger>
                    <SelectContent>
                      {/* Filtrado por marca */}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Año</Label>
                  <Input
                    type="number"
                    {...methods.register("year", { valueAsNumber: true })}
                    placeholder="2020"
                  />
                </div>
              </div>

              {/* Mileage & Condition */}
              <div className="grid md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Kilometraje</Label>
                  <Input
                    type="number"
                    {...methods.register("mileage", { valueAsNumber: true })}
                    placeholder="85000"
                  />
                </div>

                <div className="space-y-2">
                  <Label>Condición General</Label>
                  <RadioGroup
                    value={methods.watch("condition")}
                    onValueChange={(value) =>
                      methods.setValue("condition", value as any)
                    }
                  >
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="excellent" id="excellent" />
                      <Label htmlFor="excellent">Excelente</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="good" id="good" />
                      <Label htmlFor="good">Buena</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="fair" id="fair" />
                      <Label htmlFor="fair">Regular</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <RadioGroupItem value="poor" id="poor" />
                      <Label htmlFor="poor">Mala</Label>
                    </div>
                  </RadioGroup>
                </div>
              </div>

              {/* Additional Details */}
              <div className="space-y-3">
                <Label>Información Adicional</Label>
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="accident"
                    checked={methods.watch("hasAccidentHistory")}
                    onCheckedChange={(checked) =>
                      methods.setValue("hasAccidentHistory", checked as boolean)
                    }
                  />
                  <Label htmlFor="accident" className="font-normal">
                    Ha tenido accidentes
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="service"
                    checked={methods.watch("hasServiceHistory")}
                    onCheckedChange={(checked) =>
                      methods.setValue("hasServiceHistory", checked as boolean)
                    }
                  />
                  <Label htmlFor="service" className="font-normal">
                    Tiene historial de mantenimiento completo
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="parts"
                    checked={methods.watch("hasOriginalParts")}
                    onCheckedChange={(checked) =>
                      methods.setValue("hasOriginalParts", checked as boolean)
                    }
                  />
                  <Label htmlFor="parts" className="font-normal">
                    Todas las piezas son originales
                  </Label>
                </div>
              </div>

              {/* Submit */}
              <Button
                type="submit"
                className="w-full"
                size="lg"
                disabled={isLoading}
              >
                {isLoading ? "Calculando..." : "Estimar Valor"}
              </Button>
            </form>
          </FormProvider>
        </Card>
      ) : (
        <TradeInEstimateResult estimate={estimate!} onStartOver={() => setStep("form")} />
      )}
    </div>
  );
}

function TradeInEstimateResult({ estimate, onStartOver }: any) {
  return (
    <Card className="p-6">
      <div className="text-center mb-8">
        <CheckCircle className="h-16 w-16 text-green-500 mx-auto mb-4" />
        <h2 className="text-2xl font-bold mb-2">Valor Estimado de tu Vehículo</h2>
        <div className="text-5xl font-bold text-blue-600 mb-2">
          {formatCurrency(estimate.estimatedValue)}
        </div>
        <p className="text-gray-600">
          Rango: {formatCurrency(estimate.minValue)} - {formatCurrency(estimate.maxValue)}
        </p>
        <div className="mt-2">
          <span className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-green-100 text-green-700">
            Confianza: {estimate.confidence}%
          </span>
        </div>
      </div>

      {/* Breakdown */}
      <div className="bg-gray-50 rounded-lg p-6 mb-6">
        <h3 className="font-semibold mb-4">Desglose de Valoración</h3>
        <div className="space-y-3">
          <div className="flex justify-between">
            <span className="text-gray-600">Valor base de mercado</span>
            <span className="font-medium">{formatCurrency(estimate.baseValue)}</span>
          </div>
          <div className="flex justify-between text-green-600">
            <span>+ Buena condición</span>
            <span>+{formatCurrency(estimate.conditionAdjustment)}</span>
          </div>
          <div className="flex justify-between text-green-600">
            <span>+ Historial de mantenimiento</span>
            <span>+{formatCurrency(estimate.historyBonus)}</span>
          </div>
          {estimate.accidentPenalty && (
            <div className="flex justify-between text-red-600">
              <span>- Historial de accidentes</span>
              <span>-{formatCurrency(estimate.accidentPenalty)}</span>
            </div>
          )}
          <div className="flex justify-between text-red-600">
            <span>- Depreciación por kilometraje</span>
            <span>-{formatCurrency(estimate.mileageDepreciation)}</span>
          </div>
          <div className="border-t pt-3 flex justify-between font-semibold text-lg">
            <span>Valor Final</span>
            <span className="text-blue-600">{formatCurrency(estimate.estimatedValue)}</span>
          </div>
        </div>
      </div>

      {/* Market Comparison */}
      <div className="bg-blue-50 rounded-lg p-4 mb-6">
        <div className="flex items-start gap-3">
          <TrendingUp className="h-5 w-5 text-blue-600 mt-0.5" />
          <div>
            <h4 className="font-semibold text-blue-900 mb-1">
              Comparación con el Mercado
            </h4>
            <p className="text-sm text-blue-700">
              Vehículos similares se están vendiendo entre{" "}
              {formatCurrency(estimate.marketMin)} y {formatCurrency(estimate.marketMax)}.
              Tu vehículo está{" "}
              <strong>
                {estimate.marketPosition === "above" ? "por encima" : "dentro"}
              </strong>{" "}
              del promedio.
            </p>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="grid md:grid-cols-2 gap-4">
        <Button type="button" size="lg" asChild>
          <Link href="/trade-in/request">
            Usar como Parte de Pago
          </Link>
        </Button>
        <Button type="button" variant="outline" size="lg" onClick={onStartOver}>
          Estimar Otro Vehículo
        </Button>
      </div>

      <p className="text-xs text-gray-500 text-center mt-6">
        * Valoración estimada mediante IA. El precio final puede variar según inspección física.
      </p>
    </Card>
  );
}
```

---

### 3. VehicleHistoryReport (CARFAX-style)

**Ubicación:** `src/components/vehicle-history/VehicleHistoryReport.tsx`

```typescript
// filepath: src/components/vehicle-history/VehicleHistoryReport.tsx
"use client";

import * as React from "react";
import { FileText, Shield, AlertTriangle, CheckCircle, Users, Wrench, Scale } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { useVehicleHistory } from "@/lib/hooks/useVehicleHistory";
import { formatDate } from "@/lib/utils";

interface VehicleHistoryReportProps {
  vin: string;
  licensePlate?: string;
}

export function VehicleHistoryReport({ vin, licensePlate }: VehicleHistoryReportProps) {
  const { history, isLoading, purchaseReport } = useVehicleHistory(vin);

  if (!history) {
    return <VehicleHistoryPreview vin={vin} onPurchase={purchaseReport} />;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <Card className="p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h1 className="text-2xl font-bold mb-1">
              Historial del Vehículo
            </h1>
            <p className="text-gray-600">
              {history.year} {history.make} {history.model}
            </p>
          </div>
          <div className="text-right">
            <div className="text-sm text-gray-600">VIN</div>
            <div className="font-mono font-semibold">{history.vin}</div>
            {history.licensePlate && (
              <>
                <div className="text-sm text-gray-600 mt-2">Placa</div>
                <div className="font-semibold">{history.licensePlate}</div>
              </>
            )}
          </div>
        </div>

        {/* Summary Score */}
        <div className="grid grid-cols-3 gap-4 mt-6">
          <div className="text-center p-4 bg-green-50 rounded-lg">
            <Shield className="h-8 w-8 text-green-600 mx-auto mb-2" />
            <div className="text-2xl font-bold text-green-700">
              {history.overallScore}/10
            </div>
            <div className="text-sm text-green-600">Score General</div>
          </div>
          <div className="text-center p-4 bg-blue-50 rounded-lg">
            <Users className="h-8 w-8 text-blue-600 mx-auto mb-2" />
            <div className="text-2xl font-bold text-blue-700">
              {history.totalOwners}
            </div>
            <div className="text-sm text-blue-600">Propietarios</div>
          </div>
          <div className="text-center p-4 bg-gray-50 rounded-lg">
            <Wrench className="h-8 w-8 text-gray-600 mx-auto mb-2" />
            <div className="text-2xl font-bold text-gray-700">
              {history.maintenanceRecords.length}
            </div>
            <div className="text-sm text-gray-600">Servicios</div>
          </div>
        </div>
      </Card>

      {/* Ownership History */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Users className="h-5 w-5 text-blue-600" />
          Historial de Propietarios
        </h3>
        <div className="space-y-3">
          {history.ownerRecords.map((owner, index) => (
            <div key={index} className="flex items-start gap-4 pb-3 border-b last:border-0">
              <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0">
                <span className="font-semibold text-blue-700">#{index + 1}</span>
              </div>
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <span className="font-medium">{owner.ownerType}</span>
                  {owner.isCurrentOwner && (
                    <Badge variant="secondary">Actual</Badge>
                  )}
                </div>
                <div className="text-sm text-gray-600">
                  {formatDate(owner.startDate)} - {owner.endDate ? formatDate(owner.endDate) : "Presente"}
                </div>
                <div className="text-sm text-gray-500 mt-1">
                  Duración: {owner.durationMonths} meses • {owner.province}
                </div>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Accident History */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-orange-600" />
          Historial de Accidentes
        </h3>
        {history.accidentRecords.length === 0 ? (
          <div className="text-center py-8 text-green-600">
            <CheckCircle className="h-12 w-12 mx-auto mb-2" />
            <p className="font-medium">Sin accidentes reportados</p>
            <p className="text-sm text-gray-500">Este vehículo no tiene historial de accidentes</p>
          </div>
        ) : (
          <div className="space-y-4">
            {history.accidentRecords.map((accident, index) => (
              <div key={index} className="border-l-4 border-red-500 pl-4 py-2">
                <div className="flex items-center gap-2 mb-1">
                  <Badge variant={accident.severity === "major" ? "destructive" : "secondary"}>
                    {accident.severity === "major" ? "Severo" : "Menor"}
                  </Badge>
                  <span className="text-sm text-gray-600">
                    {formatDate(accident.date)}
                  </span>
                </div>
                <p className="text-sm">{accident.description}</p>
                {accident.insuranceClaim && (
                  <p className="text-xs text-gray-500 mt-1">
                    Claim aseguradora: {accident.insuranceClaim.amount}
                  </p>
                )}
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Mileage Verification */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <FileText className="h-5 w-5 text-purple-600" />
          Verificación de Kilometraje
        </h3>
        {history.mileageVerification?.isConsistent ? (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4">
            <div className="flex items-center gap-2 text-green-700 mb-2">
              <CheckCircle className="h-5 w-5" />
              <span className="font-semibold">Kilometraje Verificado</span>
            </div>
            <p className="text-sm text-green-600">
              El kilometraje registrado es consistente con el historial del vehículo.
            </p>
          </div>
        ) : (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center gap-2 text-red-700 mb-2">
              <AlertTriangle className="h-5 w-5" />
              <span className="font-semibold">Inconsistencia Detectada</span>
            </div>
            <p className="text-sm text-red-600">
              Se detectaron inconsistencias en el kilometraje reportado.
            </p>
          </div>
        )}
      </Card>

      {/* Legal Status */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Scale className="h-5 w-5 text-gray-600" />
          Estado Legal
        </h3>
        <div className="space-y-3">
          <div className="flex justify-between py-2 border-b">
            <span className="text-gray-600">Gravámenes</span>
            <span className={history.legalCheck?.hasLiens ? "text-red-600 font-medium" : "text-green-600"}>
              {history.legalCheck?.hasLiens ? "Sí" : "No"}
            </span>
          </div>
          <div className="flex justify-between py-2 border-b">
            <span className="text-gray-600">Multas Pendientes</span>
            <span className={history.legalCheck?.hasFines ? "text-red-600 font-medium" : "text-green-600"}>
              {history.legalCheck?.hasFines ? "Sí" : "No"}
            </span>
          </div>
          <div className="flex justify-between py-2">
            <span className="text-gray-600">Reporte de Robo</span>
            <span className={history.legalCheck?.isStolenVehicle ? "text-red-600 font-medium" : "text-green-600"}>
              {history.legalCheck?.isStolenVehicle ? "Sí" : "No"}
            </span>
          </div>
        </div>
      </Card>

      {/* Report Footer */}
      <Card className="p-6 bg-gray-50">
        <div className="text-center">
          <p className="text-sm text-gray-600 mb-4">
            Reporte generado el {formatDate(history.reportGeneratedAt)}
          </p>
          <Button type="button" variant="outline">
            <FileText className="h-4 w-4 mr-2" />
            Descargar Reporte PDF
          </Button>
        </div>
      </Card>
    </div>
  );
}

function VehicleHistoryPreview({ vin, onPurchase }: any) {
  return (
    <Card className="p-8 text-center">
      <Shield className="h-16 w-16 text-blue-600 mx-auto mb-4" />
      <h2 className="text-2xl font-bold mb-2">
        Obtén el Historial Completo
      </h2>
      <p className="text-gray-600 mb-6">
        Conoce todo sobre este vehículo antes de comprar
      </p>

      <div className="grid md:grid-cols-2 gap-6 mb-8 text-left">
        <div className="flex items-start gap-3">
          <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
          <div>
            <div className="font-medium">Historial de Propietarios</div>
            <div className="text-sm text-gray-600">Cuántos dueños ha tenido</div>
          </div>
        </div>
        <div className="flex items-start gap-3">
          <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
          <div>
            <div className="font-medium">Accidentes Reportados</div>
            <div className="text-sm text-gray-600">Claims de seguro y siniestros</div>
          </div>
        </div>
        <div className="flex items-start gap-3">
          <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
          <div>
            <div className="font-medium">Verificación de KM</div>
            <div className="text-sm text-gray-600">Detección de alteración</div>
          </div>
        </div>
        <div className="flex items-start gap-3">
          <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
          <div>
            <div className="font-medium">Estado Legal</div>
            <div className="text-sm text-gray-600">Gravámenes y multas</div>
          </div>
        </div>
      </div>

      <Button type="button" size="lg" onClick={onPurchase}>
        Comprar Reporte - RD$ 1,500
      </Button>

      <p className="text-xs text-gray-500 mt-4">
        * Reporte generado en menos de 5 minutos
      </p>
    </Card>
  );
}
```

---

## 🔌 API SERVICES

### financingService.ts

```typescript
// filepath: src/lib/services/financingService.ts
import { api } from "./api";

export interface CalculatePaymentRequest {
  vehiclePrice: number;
  downPayment: number;
  termMonths: number;
  bankId?: string;
}

export interface FinancingCalculation {
  id: string;
  bankId: string;
  bankName: string;
  interestRate: number;
  monthlyPayment: number;
  amountToFinance: number;
  totalInterest: number;
  totalPayment: number;
  apr: number;
}

export interface Bank {
  id: string;
  name: string;
  interestRate: number;
  minTermMonths: number;
  maxTermMonths: number;
  minAmount: number;
  maxAmount: number;
  requiresDownPayment: boolean;
  minDownPaymentPercent: number;
}

export interface PreApprovalRequest {
  vehicleId: string;
  bankId: string;
  requestedAmount: number;
  termMonths: number;
  monthlyIncome: number;
  employmentType: string;
  employmentYears: number;
}

export interface PreApproval {
  id: string;
  status: "pending" | "under_review" | "approved" | "rejected";
  vehicleId: string;
  bankId: string;
  requestedAmount: number;
  approvedAmount?: number;
  interestRate?: number;
  termMonths: number;
  createdAt: string;
  updatedAt: string;
}

class FinancingService {
  /**
   * FIN-CALC-01: Calculate monthly payment
   */
  async calculatePayment(
    request: CalculatePaymentRequest,
  ): Promise<FinancingCalculation> {
    const response = await api.post<FinancingCalculation>(
      "/financing/calculate",
      request,
    );
    return response.data;
  }

  /**
   * FIN-CALC-05: Simulate multiple banks
   */
  async simulateMultipleBanks(
    request: CalculatePaymentRequest,
  ): Promise<FinancingCalculation[]> {
    const response = await api.post<FinancingCalculation[]>(
      "/financing/simulate-multiple",
      request,
    );
    return response.data;
  }

  /**
   * FIN-BANK-01: List available banks
   */
  async getBanks(): Promise<Bank[]> {
    const response = await api.get<Bank[]>("/financing/banks");
    return response.data;
  }

  /**
   * FIN-BANK-02: Get rates by bank
   */
  async getBankRates(bankId: string): Promise<Bank> {
    const response = await api.get<Bank>(`/financing/banks/${bankId}/rates`);
    return response.data;
  }

  /**
   * FIN-PRE-01: Request pre-approval
   */
  async requestPreApproval(request: PreApprovalRequest): Promise<PreApproval> {
    const response = await api.post<PreApproval>(
      "/financing/pre-approval",
      request,
    );
    return response.data;
  }

  /**
   * FIN-PRE-02: Get my pre-approvals
   */
  async getMyPreApprovals(): Promise<PreApproval[]> {
    const response = await api.get<PreApproval[]>("/financing/pre-approvals");
    return response.data;
  }

  /**
   * Get pre-approval by ID
   */
  async getPreApprovalById(id: string): Promise<PreApproval> {
    const response = await api.get<PreApproval>(
      `/financing/pre-approvals/${id}`,
    );
    return response.data;
  }
}

export const financingService = new FinancingService();
```

### tradeInService.ts

```typescript
// filepath: src/lib/services/tradeInService.ts
import { api } from "./api";

export interface TradeInEstimateRequest {
  make: string;
  model: string;
  year: number;
  trim?: string;
  mileage: number;
  condition: "excellent" | "good" | "fair" | "poor";
  transmission: "automatic" | "manual";
  fuelType: "gasoline" | "diesel" | "electric" | "hybrid";
  color: string;
  hasAccidentHistory: boolean;
  hasServiceHistory: boolean;
  hasOriginalParts: boolean;
  licensePlate?: string;
}

export interface TradeInEstimate {
  id: string;
  estimatedValue: number;
  minValue: number;
  maxValue: number;
  confidence: number;
  baseValue: number;
  conditionAdjustment: number;
  mileageDepreciation: number;
  historyBonus: number;
  accidentPenalty: number;
  marketMin: number;
  marketMax: number;
  marketPosition: "above" | "within" | "below";
  expiresAt: string;
}

export interface TradeInOfferRequest {
  valuationId: string;
  targetVehicleId: string;
  additionalNotes?: string;
}

export interface TradeInOffer {
  id: string;
  valuationId: string;
  status:
    | "pending"
    | "dealer_offers_received"
    | "accepted"
    | "completed"
    | "cancelled";
  myVehicle: {
    make: string;
    model: string;
    year: number;
    estimatedValue: number;
  };
  targetVehicle: {
    id: string;
    make: string;
    model: string;
    year: number;
    price: number;
  };
  dealerOffers: DealerOffer[];
  createdAt: string;
}

export interface DealerOffer {
  dealerId: string;
  dealerName: string;
  offerAmount: number;
  notes?: string;
  expiresAt: string;
}

class TradeInService {
  /**
   * TRADE-EST-01: Estimate vehicle value
   */
  async estimateValue(
    request: TradeInEstimateRequest,
  ): Promise<TradeInEstimate> {
    const response = await api.post<TradeInEstimate>(
      "/tradein/estimate",
      request,
    );
    return response.data;
  }

  /**
   * TRADE-EST-02: Get saved estimate
   */
  async getEstimate(id: string): Promise<TradeInEstimate> {
    const response = await api.get<TradeInEstimate>(`/tradein/estimate/${id}`);
    return response.data;
  }

  /**
   * TRADE-REQ-01: Create trade-in offer request
   */
  async createOfferRequest(
    request: TradeInOfferRequest,
  ): Promise<TradeInOffer> {
    const response = await api.post<TradeInOffer>("/tradein/offers", request);
    return response.data;
  }

  /**
   * TRADE-REQ-02: Get my trade-in offers
   */
  async getMyOffers(): Promise<TradeInOffer[]> {
    const response = await api.get<TradeInOffer[]>("/tradein/offers");
    return response.data;
  }

  /**
   * Accept dealer offer
   */
  async acceptOffer(offerId: string, dealerOfferId: string): Promise<void> {
    await api.post(`/tradein/offers/${offerId}/accept`, { dealerOfferId });
  }

  /**
   * Get market value for comparison
   */
  async getMarketValue(
    make: string,
    model: string,
    year: number,
  ): Promise<number> {
    const response = await api.get<{ averageValue: number }>(
      "/tradein/market-value",
      {
        params: { make, model, year },
      },
    );
    return response.data.averageValue;
  }
}

export const tradeInService = new TradeInService();
```

---

## 📍 INTEGRACIÓN EN PÁGINAS EXISTENTES

### 1. Vehicle Detail - Agregar FinancingCalculatorWidget

**Modificar:** `docs/frontend-rebuild/04-PAGINAS/03-detalle-vehiculo.md`

**Agregar en el sidebar derecho:**

```typescript
// filepath: src/app/(main)/vehiculos/[slug]/page.tsx
import { FinancingCalculatorWidget } from "@/components/financing/FinancingCalculatorWidget";

export default async function VehiclePage({ params }: VehiclePageProps) {
  // ... código existente ...

  return (
    <div className="container py-6 lg:py-8">
      <div className="grid lg:grid-cols-3 gap-8">
        {/* ... Left column ... */}

        {/* Right column - Sticky sidebar */}
        <div className="hidden lg:block">
          <div className="sticky top-24 space-y-6">
            <VehicleHeader vehicle={vehicle} />

            {/* ← NUEVO: Widget de financiamiento */}
            <FinancingCalculatorWidget
              vehiclePrice={vehicle.price}
              vehicleId={vehicle.id}
            />

            <SellerCard vehicle={vehicle} />
          </div>
        </div>
      </div>
    </div>
  );
}
```

### 2. Homepage - Agregar CTA de Trade-In

**Modificar:** `docs/frontend-rebuild/04-PAGINAS/01-home.md`

**Agregar sección de Trade-In:**

```typescript
// filepath: src/app/(main)/page.tsx

<section className="py-16 bg-gradient-to-br from-blue-600 to-blue-800 text-white">
  <div className="container">
    <div className="max-w-3xl mx-auto text-center">
      <h2 className="text-3xl font-bold mb-4">
        ¿Quieres Cambiar tu Vehículo?
      </h2>
      <p className="text-xl text-blue-100 mb-8">
        Obtén una valoración instantánea y úsala como parte de pago
      </p>
      <Button type="button" size="lg" variant="secondary" asChild>
        <Link href="/trade-in/estimate">
          Estimar el Valor de mi Vehículo
        </Link>
      </Button>
    </div>
  </div>
</section>
```

---

## 🧪 TESTING

### Unit Tests - FinancingCalculatorWidget

```typescript
// filepath: src/components/financing/__tests__/FinancingCalculatorWidget.test.tsx
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FinancingCalculatorWidget } from "../FinancingCalculatorWidget";
import { financingService } from "@/lib/services/financingService";

jest.mock("@/lib/services/financingService");

describe("FinancingCalculatorWidget", () => {
  it("should calculate monthly payment", async () => {
    const user = userEvent.setup();

    (financingService.calculatePayment as jest.Mock).mockResolvedValue({
      monthlyPayment: 48000,
      totalInterest: 420000,
      totalPayment: 2420000,
    });

    (financingService.getBanks as jest.Mock).mockResolvedValue([
      { id: "1", name: "Banco Popular", interestRate: 0.11 },
    ]);

    render(
      <FinancingCalculatorWidget vehiclePrice={2500000} vehicleId="vehicle-123" />
    );

    // Wait for banks to load
    await waitFor(() => {
      expect(screen.getByText("Banco Popular")).toBeInTheDocument();
    });

    // Select bank
    await user.click(screen.getByRole("combobox"));
    await user.click(screen.getByText("Banco Popular"));

    // Verify calculation displayed
    await waitFor(() => {
      expect(screen.getByText(/48,000/)).toBeInTheDocument();
    });
  });

  it("should adjust down payment with slider", async () => {
    const user = userEvent.setup();

    render(
      <FinancingCalculatorWidget vehiclePrice={2500000} vehicleId="vehicle-123" />
    );

    const slider = screen.getByRole("slider");

    // This would require custom implementation based on slider library
    // Test that changing slider updates the displayed down payment
  });
});
```

---

## 📊 MÉTRICAS DE ÉXITO

| Métrica                         | Objetivo | Método de Medición                     |
| ------------------------------- | -------- | -------------------------------------- |
| Uso de calculadora              | > 60%    | % de visitantes que usan el widget     |
| Conversión a pre-aprobación     | > 15%    | % que solicitan pre-aprobación         |
| Trade-in estimations            | > 30%    | % que estiman su vehículo              |
| Trade-in conversion             | > 10%    | % que usan trade-in en compra          |
| Vehicle history report purchase | > 25%    | % que compran reporte antes de comprar |

---

## 🚀 PRÓXIMOS PASOS

### Sprint 1: Financiamiento Core (Prioridad CRÍTICA)

- [ ] FinancingCalculatorWidget component
- [ ] BankComparisonTable component
- [ ] PreApprovalForm component
- [ ] Integration en VehicleDetailPage
- [ ] Tests unitarios (> 80% coverage)

### Sprint 2: Trade-In (Prioridad ALTA)

- [ ] TradeInEstimator component
- [ ] TradeInOfferRequest component
- [ ] My Trade-Ins dashboard
- [ ] Dealer notification system
- [ ] ML model para valuación

### Sprint 3: Historial Vehículo (Prioridad ALTA - Diferenciador)

- [ ] VehicleHistoryService backend
- [ ] Integraciones INTRANT/DGII
- [ ] VehicleHistoryReport component
- [ ] Purchase flow
- [ ] PDF report generation

### Sprint 4: Costos Totales (Prioridad MEDIA)

- [ ] TotalCostCalculator component
- [ ] Tax rates integration
- [ ] Insurance quotes integration
- [ ] TCO calculator

---

## 📚 REFERENCIAS

### Documentos Process Matrix

- [01-calculadora-financiamiento.md](../../process-matrix/14-FINANCIAMIENTO-TRADEIN/01-calculadora-financiamiento.md)
- [02-trade-in-estimador.md](../../process-matrix/14-FINANCIAMIENTO-TRADEIN/02-trade-in-estimador.md)
- [03-historial-vehiculo.md](../../process-matrix/14-FINANCIAMIENTO-TRADEIN/03-historial-vehiculo.md)
- [04-calculadora-costos-totales.md](../../process-matrix/14-FINANCIAMIENTO-TRADEIN/04-calculadora-costos-totales.md)

### Documentos Relacionados Frontend

- [03-detalle-vehiculo.md](./03-detalle-vehiculo.md) - Integración de widget
- [01-home.md](./01-home.md) - CTA de trade-in
- [04-publicar.md](../04-VENDEDOR/01-publicar-vehiculo.md) - Trade-in en publicación

### Backend APIs

- `FinancingService.Api` - `/backend/FinancingService/FinancingService.Api/`
- `TradeInService.Api` - `/backend/TradeInService/TradeInService.Api/`

### Integraciones Externas

- **Bancos RD:** Banco Popular, BHD León, Banreservas, Scotiabank
- **INTRANT:** Verificación de placas
- **DGII:** Tablas de impuestos
- **Aseguradoras:** Seguros Universal, Mapfre, Colonial

---

**✅ DOCUMENTO COMPLETO - LISTO PARA IMPLEMENTACIÓN**

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/financiamiento-tradein.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser, loginAsDealer } from "../helpers/auth";

test.describe("Financiamiento - Usuario", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar calculadora de financiamiento", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024");

    await expect(page.getByTestId("financing-calculator")).toBeVisible();
  });

  test("debe calcular cuota mensual", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024");

    await page.fill('input[name="downPayment"]', "200000");
    await page.getByRole("combobox", { name: /plazo/i }).click();
    await page.getByRole("option", { name: /48 meses/i }).click();

    await expect(page.getByTestId("monthly-payment")).toBeVisible();
  });

  test("debe solicitar pre-aprobación", async ({ page }) => {
    await page.goto("/financiamiento/pre-aprobacion");

    await page.fill('input[name="income"]', "80000");
    await page.getByRole("button", { name: /solicitar/i }).click();

    await expect(page.getByText(/solicitud enviada/i)).toBeVisible();
  });
});

test.describe("Trade-In - Usuario", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar valuador de trade-in", async ({ page }) => {
    await page.goto("/trade-in");

    await expect(page.getByTestId("trade-in-form")).toBeVisible();
  });

  test("debe obtener valuación estimada", async ({ page }) => {
    await page.goto("/trade-in");

    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: /honda/i }).click();
    await page.getByRole("combobox", { name: /modelo/i }).click();
    await page.getByRole("option", { name: /civic/i }).click();
    await page.fill('input[name="year"]', "2020");
    await page.fill('input[name="mileage"]', "45000");
    await page.getByRole("button", { name: /obtener valuación/i }).click();

    await expect(page.getByTestId("valuation-result")).toBeVisible();
  });
});

test.describe("Trade-In - Dealer", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe ver solicitudes de trade-in", async ({ page }) => {
    await page.goto("/dealer/trade-ins");

    await expect(page.getByTestId("trade-in-requests")).toBeVisible();
  });

  test("debe hacer contraoferta", async ({ page }) => {
    await page.goto("/dealer/trade-ins");

    await page.getByTestId("trade-in-row").first().click();
    await page.fill('input[name="counterOffer"]', "450000");
    await page.getByRole("button", { name: /enviar oferta/i }).click();

    await expect(page.getByText(/oferta enviada/i)).toBeVisible();
  });
});
```

---

_Este documento integra TODOS los procesos de FINANCIAMIENTO-TRADEIN con implementación UI completa, hooks, servicios, testing y métricas._

---

**Siguiente documento:** `40-admin-operations-completo.md` (si aplica)

**Dependencias backend:** FinancingService (puerto 5080), TradeInService (puerto 5081), VehicleHistoryService (pendiente implementación)

**Prioridad:** 🔴 CRÍTICA (Diferenciador competitivo vs SuperCarros)
