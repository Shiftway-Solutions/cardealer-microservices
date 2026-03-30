'use client';

import { useState, useMemo } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Calculator,
  TrendingUp,
  Shield,
  ChevronDown,
  ChevronUp,
  DollarSign,
  Calendar,
  Percent,
} from 'lucide-react';

const PLAZOS = [
  { value: '12', label: '12 meses (1 año)' },
  { value: '24', label: '24 meses (2 años)' },
  { value: '36', label: '36 meses (3 años)' },
  { value: '48', label: '48 meses (4 años)' },
  { value: '60', label: '60 meses (5 años)' },
  { value: '72', label: '72 meses (6 años)' },
];

function formatRD(value: number): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('es-DO').format(Math.round(value));
}

interface AmortizationRow {
  month: number;
  payment: number;
  principal: number;
  interest: number;
  insurance: number;
  balance: number;
}

function calculateAmortization(
  loanAmount: number,
  annualRate: number,
  months: number,
  includeInsurance: boolean,
  vehiclePrice: number
): {
  monthlyPayment: number;
  totalPayment: number;
  totalInterest: number;
  totalInsurance: number;
  monthlyInsurance: number;
  schedule: AmortizationRow[];
} {
  const monthlyRate = annualRate / 100 / 12;
  const monthlyInsurance = includeInsurance ? (vehiclePrice * 0.03) / 12 : 0;
  const totalInsurance = monthlyInsurance * months;

  if (monthlyRate === 0) {
    const monthlyPayment = loanAmount / months;
    const schedule: AmortizationRow[] = [];
    let balance = loanAmount;
    for (let i = 1; i <= months; i++) {
      balance -= monthlyPayment;
      schedule.push({
        month: i,
        payment: monthlyPayment + monthlyInsurance,
        principal: monthlyPayment,
        interest: 0,
        insurance: monthlyInsurance,
        balance: Math.max(0, balance),
      });
    }
    return {
      monthlyPayment: monthlyPayment + monthlyInsurance,
      totalPayment: loanAmount + totalInsurance,
      totalInterest: 0,
      totalInsurance,
      monthlyInsurance,
      schedule,
    };
  }

  // French amortization formula: P × [r(1+r)^n] / [(1+r)^n – 1]
  const factor = Math.pow(1 + monthlyRate, months);
  const baseMonthlyPayment = loanAmount * ((monthlyRate * factor) / (factor - 1));

  const schedule: AmortizationRow[] = [];
  let balance = loanAmount;

  for (let i = 1; i <= months; i++) {
    const interestPayment = balance * monthlyRate;
    const principalPayment = baseMonthlyPayment - interestPayment;
    balance -= principalPayment;

    schedule.push({
      month: i,
      payment: baseMonthlyPayment + monthlyInsurance,
      principal: principalPayment,
      interest: interestPayment,
      insurance: monthlyInsurance,
      balance: Math.max(0, balance),
    });
  }

  const totalInterest = schedule.reduce((sum, row) => sum + row.interest, 0);

  return {
    monthlyPayment: baseMonthlyPayment + monthlyInsurance,
    totalPayment: baseMonthlyPayment * months + totalInsurance,
    totalInterest,
    totalInsurance,
    monthlyInsurance,
    schedule,
  };
}

export function FinancingCalculator() {
  const [vehiclePrice, setVehiclePrice] = useState<string>('1500000');
  const [downPayment, setDownPayment] = useState<string>('300000');
  const [plazo, setPlazo] = useState<string>('48');
  const [interestRate, setInterestRate] = useState<string>('12');
  const [includeInsurance, setIncludeInsurance] = useState(true);
  const [showAmortization, setShowAmortization] = useState(false);

  const price = parseFloat(vehiclePrice.replace(/[^0-9.]/g, '')) || 0;
  const down = parseFloat(downPayment.replace(/[^0-9.]/g, '')) || 0;
  const months = parseInt(plazo) || 48;
  const rate = parseFloat(interestRate) || 12;
  const loanAmount = Math.max(0, price - down);
  const downPaymentPercent = price > 0 ? (down / price) * 100 : 0;

  const result = useMemo(() => {
    if (loanAmount <= 0) return null;
    return calculateAmortization(loanAmount, rate, months, includeInsurance, price);
  }, [loanAmount, rate, months, includeInsurance, price]);

  const isValidDown = downPaymentPercent >= 20;

  return (
    <div className="mx-auto max-w-5xl">
      <div className="grid gap-8 lg:grid-cols-5">
        {/* Input Form */}
        <Card className="lg:col-span-3">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Calculator className="text-primary h-5 w-5" />
              Datos del financiamiento
            </CardTitle>
            <CardDescription>
              Ingresa los datos de tu vehículo y las condiciones del préstamo
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Precio del vehículo */}
            <div className="space-y-2">
              <Label htmlFor="price" className="flex items-center gap-2">
                <DollarSign className="h-4 w-4" />
                Precio del vehículo (RD$)
              </Label>
              <Input
                id="price"
                type="text"
                inputMode="numeric"
                value={vehiclePrice}
                onChange={e => {
                  const raw = e.target.value.replace(/[^0-9]/g, '');
                  setVehiclePrice(raw);
                }}
                placeholder="1,500,000"
              />
              {price > 0 && <p className="text-muted-foreground text-sm">{formatRD(price)}</p>}
            </div>

            {/* Inicial */}
            <div className="space-y-2">
              <Label htmlFor="down" className="flex items-center gap-2">
                <DollarSign className="h-4 w-4" />
                Inicial (RD$)
              </Label>
              <Input
                id="down"
                type="text"
                inputMode="numeric"
                value={downPayment}
                onChange={e => {
                  const raw = e.target.value.replace(/[^0-9]/g, '');
                  setDownPayment(raw);
                }}
                placeholder="300,000"
              />
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">
                  {formatRD(down)} ({downPaymentPercent.toFixed(1)}% del precio)
                </span>
                {!isValidDown && price > 0 && down > 0 && (
                  <span className="text-destructive">⚠️ Mínimo recomendado: 20%</span>
                )}
              </div>
              {/* Quick percentage buttons */}
              <div className="flex gap-2">
                {[20, 30, 40, 50].map(pct => (
                  <Button
                    key={pct}
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setDownPayment(Math.round(price * (pct / 100)).toString())}
                    className={
                      Math.abs(downPaymentPercent - pct) < 1 ? 'border-primary bg-primary/10' : ''
                    }
                  >
                    {pct}%
                  </Button>
                ))}
              </div>
            </div>

            {/* Plazo */}
            <div className="space-y-2">
              <Label htmlFor="plazo" className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                Plazo
              </Label>
              <Select value={plazo} onValueChange={setPlazo}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecciona plazo" />
                </SelectTrigger>
                <SelectContent>
                  {PLAZOS.map(p => (
                    <SelectItem key={p.value} value={p.value}>
                      {p.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Tasa de interés */}
            <div className="space-y-2">
              <Label htmlFor="rate" className="flex items-center gap-2">
                <Percent className="h-4 w-4" />
                Tasa de interés anual (%)
              </Label>
              <Input
                id="rate"
                type="number"
                step="0.5"
                min="1"
                max="30"
                value={interestRate}
                onChange={e => setInterestRate(e.target.value)}
              />
              <p className="text-muted-foreground text-xs">
                Promedio del mercado DR: 10-14% anual (fuente: SIB 2026)
              </p>
            </div>

            {/* Incluir seguro */}
            <div className="flex items-center justify-between rounded-lg border p-4">
              <div className="space-y-0.5">
                <Label htmlFor="insurance" className="flex items-center gap-2 text-base">
                  <Shield className="h-4 w-4" />
                  Incluir seguro todo riesgo
                </Label>
                <p className="text-muted-foreground text-sm">
                  Estimado: ~3% del valor anual ({formatRD(price * 0.03)}/año)
                </p>
              </div>
              <Switch
                id="insurance"
                checked={includeInsurance}
                onCheckedChange={setIncludeInsurance}
              />
            </div>
          </CardContent>
        </Card>

        {/* Results */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="text-primary h-5 w-5" />
              Resultado
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            {result ? (
              <>
                {/* Cuota mensual */}
                <div className="from-primary to-primary/80 rounded-xl bg-gradient-to-br p-6 text-center text-white">
                  <p className="mb-1 text-sm text-white/80">Tu cuota mensual</p>
                  <p className="text-4xl font-bold">{formatRD(result.monthlyPayment)}</p>
                  <p className="mt-1 text-sm text-white/70">por {months} meses</p>
                </div>

                <Separator />

                {/* Desglose */}
                <div className="space-y-3">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Monto financiado</span>
                    <span className="font-medium">{formatRD(loanAmount)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Total de intereses</span>
                    <span className="font-medium text-amber-600">
                      {formatRD(result.totalInterest)}
                    </span>
                  </div>
                  {includeInsurance && (
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Total seguro</span>
                      <span className="font-medium text-blue-600">
                        {formatRD(result.totalInsurance)}
                      </span>
                    </div>
                  )}
                  <Separator />
                  <div className="flex justify-between font-semibold">
                    <span>Total a pagar</span>
                    <span>{formatRD(result.totalPayment + down)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Relación interés/precio</span>
                    <span className="font-medium">
                      {((result.totalInterest / price) * 100).toFixed(1)}%
                    </span>
                  </div>
                </div>

                <Separator />

                {/* Desglose de cuota */}
                <div className="space-y-2">
                  <p className="text-sm font-medium">Desglose de cuota mensual:</p>
                  <div className="space-y-1.5">
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Capital + Interés</span>
                      <span>{formatRD(result.monthlyPayment - result.monthlyInsurance)}</span>
                    </div>
                    {includeInsurance && (
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Seguro</span>
                        <span>{formatRD(result.monthlyInsurance)}</span>
                      </div>
                    )}
                  </div>
                </div>
              </>
            ) : (
              <div className="text-muted-foreground py-8 text-center">
                <Calculator className="mx-auto mb-3 h-12 w-12 opacity-30" />
                <p>Ingresa los datos del vehículo para ver el resultado del financiamiento</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Amortization Table */}
      {result && (
        <Card className="mt-8">
          <CardHeader>
            <button
              onClick={() => setShowAmortization(!showAmortization)}
              className="flex w-full items-center justify-between"
            >
              <CardTitle className="text-lg">Tabla de Amortización</CardTitle>
              {showAmortization ? (
                <ChevronUp className="h-5 w-5" />
              ) : (
                <ChevronDown className="h-5 w-5" />
              )}
            </button>
            <CardDescription>Desglose mensual de capital, interés y balance</CardDescription>
          </CardHeader>
          {showAmortization && (
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/50 border-b text-left">
                      <th className="px-3 py-2 font-medium">Mes</th>
                      <th className="px-3 py-2 text-right font-medium">Cuota</th>
                      <th className="px-3 py-2 text-right font-medium">Capital</th>
                      <th className="px-3 py-2 text-right font-medium">Interés</th>
                      {includeInsurance && (
                        <th className="px-3 py-2 text-right font-medium">Seguro</th>
                      )}
                      <th className="px-3 py-2 text-right font-medium">Balance</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.schedule.map(row => (
                      <tr key={row.month} className="hover:bg-muted/30 border-b last:border-0">
                        <td className="px-3 py-2">{row.month}</td>
                        <td className="px-3 py-2 text-right">{formatNumber(row.payment)}</td>
                        <td className="px-3 py-2 text-right text-emerald-600">
                          {formatNumber(row.principal)}
                        </td>
                        <td className="px-3 py-2 text-right text-amber-600">
                          {formatNumber(row.interest)}
                        </td>
                        {includeInsurance && (
                          <td className="px-3 py-2 text-right text-blue-600">
                            {formatNumber(row.insurance)}
                          </td>
                        )}
                        <td className="px-3 py-2 text-right font-medium">
                          {formatNumber(row.balance)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          )}
        </Card>
      )}
    </div>
  );
}
