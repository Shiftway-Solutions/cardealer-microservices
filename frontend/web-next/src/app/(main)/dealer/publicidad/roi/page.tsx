/**
 * Dealer Publicidad ROI Calculator — /dealer/publicidad/roi
 *
 * Interactive ROI calculator that shows dealers exactly how much return
 * they can expect from their advertising investment.
 * Based on Section 6.2 of the OKLA Advertising Study.
 */

'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  ArrowLeft,
  Calculator,
  TrendingUp,
  DollarSign,
  Target,
  Zap,
  ChevronRight,
  BarChart3,
  Phone,
  Eye,
  MousePointerClick,
} from 'lucide-react';
import { calculateRoi } from '@/lib/ad-engine';

// =============================================================================
// HELPERS
// =============================================================================

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(amount);
}

// =============================================================================
// COMPONENT
// =============================================================================

export default function RoiCalculatorPage() {
  const [budget, setBudget] = useState(5000);
  const [ctr, setCtr] = useState(5);
  const [leadRate, setLeadRate] = useState(12);
  const [convRate, setConvRate] = useState(35);
  const [margin, setMargin] = useState(50000);

  const roi = useMemo(() => calculateRoi({
    monthlyBudget: budget,
    estimatedCtr: ctr / 100,
    estimatedLeadRate: leadRate / 100,
    estimatedConvRate: convRate / 100,
    averageMargin: margin,
  }), [budget, ctr, leadRate, convRate, margin]);

  const BUDGET_PRESETS = [2500, 5000, 10000, 25000, 50000];

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      {/* Header */}
      <div className="border-b bg-white">
        <div className="mx-auto max-w-5xl px-4 py-6 sm:px-6">
          <div className="flex items-center gap-3 mb-4">
            <Link
              href="/dealer/publicidad"
              className="flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700 transition-colors"
            >
              <ArrowLeft className="h-4 w-4" />
              Publicidad
            </Link>
          </div>
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-100">
              <Calculator className="h-6 w-6 text-emerald-600" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-900">
                Calculadora de ROI Publicitario
              </h1>
              <p className="text-sm text-slate-500 mt-0.5">
                Estima el retorno de inversión de tu publicidad en OKLA
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
        <div className="grid gap-6 lg:grid-cols-5">
          {/* Input Panel */}
          <div className="lg:col-span-2 space-y-6">
            {/* Budget */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-semibold flex items-center gap-2">
                  <DollarSign className="h-4 w-4 text-emerald-600" />
                  Presupuesto Mensual
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="text-3xl font-bold text-emerald-600">
                  {formatCurrency(budget)}
                </div>
                <input
                  type="range"
                  min={1000}
                  max={100000}
                  step={500}
                  value={budget}
                  onChange={(e) => setBudget(Number(e.target.value))}
                  className="w-full accent-emerald-600"
                />
                <div className="flex flex-wrap gap-2">
                  {BUDGET_PRESETS.map((preset) => (
                    <button
                      key={preset}
                      onClick={() => setBudget(preset)}
                      className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                        budget === preset
                          ? 'bg-emerald-600 text-white'
                          : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                      }`}
                    >
                      {formatCurrency(preset)}
                    </button>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Advanced Settings */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-semibold flex items-center gap-2">
                  <Target className="h-4 w-4 text-blue-600" />
                  Parámetros (ajustables)
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="text-xs text-slate-500 mb-1 block">
                    Tasa de clics (CTR): {ctr}%
                  </label>
                  <input
                    type="range"
                    min={1}
                    max={15}
                    step={0.5}
                    value={ctr}
                    onChange={(e) => setCtr(Number(e.target.value))}
                    className="w-full accent-blue-600"
                  />
                  <div className="flex justify-between text-[10px] text-slate-400">
                    <span>Bajo: 1%</span>
                    <span>Promedio: 5%</span>
                    <span>Alto: 15%</span>
                  </div>
                </div>

                <div>
                  <label className="text-xs text-slate-500 mb-1 block">
                    Tasa de leads: {leadRate}%
                  </label>
                  <input
                    type="range"
                    min={3}
                    max={25}
                    step={1}
                    value={leadRate}
                    onChange={(e) => setLeadRate(Number(e.target.value))}
                    className="w-full accent-blue-600"
                  />
                </div>

                <div>
                  <label className="text-xs text-slate-500 mb-1 block">
                    Tasa de conversión: {convRate}%
                  </label>
                  <input
                    type="range"
                    min={10}
                    max={60}
                    step={5}
                    value={convRate}
                    onChange={(e) => setConvRate(Number(e.target.value))}
                    className="w-full accent-blue-600"
                  />
                </div>

                <div>
                  <label className="text-xs text-slate-500 mb-1 block">
                    Margen por venta: {formatCurrency(margin)}
                  </label>
                  <input
                    type="range"
                    min={15000}
                    max={150000}
                    step={5000}
                    value={margin}
                    onChange={(e) => setMargin(Number(e.target.value))}
                    className="w-full accent-blue-600"
                  />
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Results Panel */}
          <div className="lg:col-span-3 space-y-6">
            {/* ROI Hero */}
            <Card className="overflow-hidden">
              <div className="bg-gradient-to-r from-emerald-600 to-teal-700 p-6 text-white">
                <p className="text-sm font-medium text-white/80">Retorno de inversión estimado</p>
                <div className="flex items-end gap-3 mt-2">
                  <span className="text-5xl font-bold">{roi.roi.toLocaleString()}%</span>
                  <span className="text-lg text-white/80 mb-1">ROI</span>
                </div>
                <p className="mt-2 text-sm text-white/70">
                  Por cada RD$1 invertido, recibes{' '}
                  <span className="font-bold text-white">RD${roi.returnPerDollar}</span> de retorno
                </p>
              </div>
            </Card>

            {/* Funnel Metrics */}
            <div className="grid gap-3 grid-cols-2">
              <Card className="p-4">
                <div className="flex items-center gap-2 text-slate-500 mb-2">
                  <Eye className="h-4 w-4" />
                  <span className="text-xs font-medium">Impresiones</span>
                </div>
                <p className="text-2xl font-bold text-slate-900">
                  {roi.estimatedImpressions.toLocaleString()}
                </p>
              </Card>
              <Card className="p-4">
                <div className="flex items-center gap-2 text-slate-500 mb-2">
                  <MousePointerClick className="h-4 w-4" />
                  <span className="text-xs font-medium">Clics</span>
                </div>
                <p className="text-2xl font-bold text-slate-900">
                  {roi.estimatedClicks.toLocaleString()}
                </p>
                <p className="text-xs text-slate-400">
                  CPC: {formatCurrency(roi.estimatedCpc)}
                </p>
              </Card>
              <Card className="p-4">
                <div className="flex items-center gap-2 text-slate-500 mb-2">
                  <Phone className="h-4 w-4" />
                  <span className="text-xs font-medium">Leads</span>
                </div>
                <p className="text-2xl font-bold text-slate-900">
                  {roi.estimatedLeads.toLocaleString()}
                </p>
                <p className="text-xs text-slate-400">
                  CPL: {formatCurrency(roi.estimatedCpl)}
                </p>
              </Card>
              <Card className="p-4">
                <div className="flex items-center gap-2 text-emerald-600 mb-2">
                  <TrendingUp className="h-4 w-4" />
                  <span className="text-xs font-medium">Ventas Estimadas</span>
                </div>
                <p className="text-2xl font-bold text-emerald-600">
                  {roi.estimatedSales}
                </p>
                <p className="text-xs text-slate-400">
                  CAC: {formatCurrency(roi.estimatedCac)}
                </p>
              </Card>
            </div>

            {/* Revenue Summary */}
            <Card className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-semibold text-slate-900 flex items-center gap-2">
                  <BarChart3 className="h-5 w-5 text-emerald-600" />
                  Resumen de Revenue
                </h3>
              </div>
              <div className="space-y-3">
                <div className="flex items-center justify-between py-2 border-b border-slate-100">
                  <span className="text-sm text-slate-600">Inversión publicitaria</span>
                  <span className="font-semibold text-red-600">
                    -{formatCurrency(roi.monthlyBudget)}
                  </span>
                </div>
                <div className="flex items-center justify-between py-2 border-b border-slate-100">
                  <span className="text-sm text-slate-600">Revenue por ventas ({roi.estimatedSales} ventas)</span>
                  <span className="font-semibold text-emerald-600">
                    +{formatCurrency(roi.totalRevenue)}
                  </span>
                </div>
                <div className="flex items-center justify-between py-2 bg-emerald-50 -mx-6 px-6 rounded-lg">
                  <span className="text-sm font-bold text-emerald-900">Ganancia neta estimada</span>
                  <span className="text-xl font-bold text-emerald-600">
                    {formatCurrency(roi.totalRevenue - roi.monthlyBudget)}
                  </span>
                </div>
              </div>
            </Card>

            {/* CTA */}
            <div className="flex items-center gap-3">
              <Button
                className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white py-6 text-base font-semibold rounded-xl"
                asChild
              >
                <Link href="/dealer/publicidad">
                  <Zap className="h-5 w-5 mr-2" />
                  Crear mi primera campaña
                  <ChevronRight className="h-4 w-4 ml-1" />
                </Link>
              </Button>
            </div>

            {/* Trust Note */}
            <p className="text-center text-xs text-slate-400">
              * Estimaciones basadas en métricas promedio del mercado automotriz de RD.
              Los resultados pueden variar según el inventario, la calidad del anuncio y la competencia.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
