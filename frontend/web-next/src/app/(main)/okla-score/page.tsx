'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ScoreReport } from '@/components/okla-score/score-report';
import { ScoreGauge } from '@/components/okla-score/score-gauge';
import { useCalculateScore, useVinDecode } from '@/hooks/use-okla-score';
import type { OklaScoreReport } from '@/types/okla-score';
import {
  Search,
  ShieldCheck,
  AlertTriangle,
  TrendingDown,
  DollarSign,
  Loader2,
  Info,
  Car,
} from 'lucide-react';

// =============================================================================
// OKLA Score™ — VIN Lookup Page
// =============================================================================
// Public page where buyers enter a VIN + optional price to get a full score.
// =============================================================================

export default function OklaScorePage() {
  const [vin, setVin] = useState('');
  const [price, setPrice] = useState('');
  const [mileage, setMileage] = useState('');
  const [report, setReport] = useState<OklaScoreReport | null>(null);

  const vinDecode = useVinDecode(vin.length === 17 ? vin : null);
  const calculateScore = useCalculateScore();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (vin.length !== 17) return;

    calculateScore.mutate(
      {
        vin,
        listedPriceDOP: price ? parseInt(price.replace(/\D/g, ''), 10) : undefined,
        declaredMileage: mileage ? parseInt(mileage.replace(/\D/g, ''), 10) : undefined,
        mileageUnit: 'km',
        sellerType: 'individual',
      },
      {
        onSuccess: data => setReport(data),
      }
    );
  };

  const isLoading = calculateScore.isPending;
  const isVinValid = vin.length === 17;

  return (
    <div className="min-h-screen bg-gradient-to-b from-emerald-50/50 to-white dark:from-gray-950 dark:to-gray-900">
      {/* Hero Section */}
      <section className="relative overflow-hidden border-b bg-gradient-to-br from-emerald-600 via-emerald-700 to-teal-800 text-white">
        <div className="absolute inset-0 bg-[url('/grid.svg')] opacity-10" />
        <div className="relative container mx-auto px-4 py-16 md:py-24">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-2 text-sm font-medium backdrop-blur-sm">
              <ShieldCheck className="h-4 w-4" />
              Powered by NHTSA · 100% Gratis
            </div>
            <h1 className="text-4xl font-extrabold tracking-tight md:text-5xl lg:text-6xl">
              OKLA Score™
            </h1>
            <p className="mt-4 text-xl text-emerald-100 md:text-2xl">
              La primera evaluación científica de vehículos usados en RD
            </p>
            <p className="mx-auto mt-2 max-w-xl text-emerald-200">
              Analiza cualquier vehículo con su número VIN. Detecta fraudes, recalls, daños ocultos
              y compara precios con el mercado de EE.UU. y RD.
            </p>
          </div>
        </div>
      </section>

      {/* Search Form */}
      <section className="container mx-auto -mt-8 max-w-3xl px-4">
        <Card className="border-emerald-200 shadow-xl">
          <CardContent className="p-6">
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <Label htmlFor="vin" className="text-base font-semibold">
                  Número VIN del Vehículo
                </Label>
                <div className="relative mt-2">
                  <Search className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
                  <Input
                    id="vin"
                    placeholder="Ej: 1HGBH41JXMN109186"
                    value={vin}
                    onChange={e =>
                      setVin(
                        e.target.value
                          .toUpperCase()
                          .replace(/[^A-HJ-NPR-Z0-9]/g, '')
                          .slice(0, 17)
                      )
                    }
                    className="h-12 pl-10 font-mono text-lg tracking-wider"
                    maxLength={17}
                    autoComplete="off"
                    spellCheck={false}
                  />
                </div>
                <p className="text-muted-foreground mt-1 text-xs">
                  {vin.length}/17 caracteres
                  {isVinValid && vinDecode.data && (
                    <span className="ml-2 font-medium text-emerald-600">
                      ✓ {vinDecode.data.year} {vinDecode.data.make} {vinDecode.data.model}
                    </span>
                  )}
                  {isVinValid && vinDecode.isLoading && (
                    <span className="text-muted-foreground ml-2">Decodificando...</span>
                  )}
                </p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="price">Precio Listado (RD$) — Opcional</Label>
                  <div className="relative mt-1">
                    <DollarSign className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                    <Input
                      id="price"
                      placeholder="Ej: 950000"
                      value={price}
                      onChange={e => setPrice(e.target.value.replace(/\D/g, ''))}
                      className="pl-9"
                      type="text"
                      inputMode="numeric"
                    />
                  </div>
                </div>
                <div>
                  <Label htmlFor="mileage">Kilometraje Declarado — Opcional</Label>
                  <div className="relative mt-1">
                    <Car className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                    <Input
                      id="mileage"
                      placeholder="Ej: 45000"
                      value={mileage}
                      onChange={e => setMileage(e.target.value.replace(/\D/g, ''))}
                      className="pl-9"
                      type="text"
                      inputMode="numeric"
                    />
                  </div>
                </div>
              </div>

              <Button
                type="submit"
                size="lg"
                className="h-12 w-full bg-emerald-600 text-base text-white hover:bg-emerald-700"
                disabled={!isVinValid || isLoading}
              >
                {isLoading ? (
                  <>
                    <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                    Calculando OKLA Score...
                  </>
                ) : (
                  <>
                    <ShieldCheck className="mr-2 h-5 w-5" />
                    Calcular OKLA Score™
                  </>
                )}
              </Button>

              {calculateScore.isError && (
                <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  <AlertTriangle className="h-4 w-4" />
                  {calculateScore.error?.message || 'Error al calcular el score'}
                </div>
              )}
            </form>
          </CardContent>
        </Card>
      </section>

      {/* Score Report */}
      {report && (
        <section className="container mx-auto mt-8 max-w-4xl px-4 pb-16">
          <ScoreReport report={report} layout="full" />
        </section>
      )}

      {/* How it Works */}
      {!report && (
        <section className="container mx-auto mt-16 max-w-5xl px-4 pb-16">
          <h2 className="mb-8 text-center text-2xl font-bold">¿Cómo Funciona?</h2>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            <HowItWorksCard
              step={1}
              icon="🔍"
              title="Ingresa el VIN"
              description="Ingresa los 17 caracteres del número VIN del vehículo que estás evaluando."
            />
            <HowItWorksCard
              step={2}
              icon="📡"
              title="Consultamos APIs"
              description="Verificamos historial con NHTSA, recalls activos y calificaciones de seguridad."
            />
            <HowItWorksCard
              step={3}
              icon="🧮"
              title="Algoritmo de 7 Dimensiones"
              description="Evaluamos VIN History, Mecánica, Kilometraje, Precio, Seguridad, Depreciación y Vendedor."
            />
            <HowItWorksCard
              step={4}
              icon="🛡️"
              title="Reporte Completo"
              description="Recibes un score de 0 a 1,000 con alertas y comparación de precios en RD."
            />
          </div>

          {/* Score levels */}
          <div className="mt-16">
            <h3 className="mb-6 text-center text-xl font-bold">Niveles del OKLA Score™</h3>
            <div className="flex flex-wrap justify-center gap-4">
              <ScoreLevelCard
                emoji="🟢"
                label="Excelente"
                range="850 — 1,000"
                color="bg-emerald-50 border-emerald-200 text-emerald-700"
              />
              <ScoreLevelCard
                emoji="🔵"
                label="Bueno"
                range="700 — 849"
                color="bg-blue-50 border-blue-200 text-blue-700"
              />
              <ScoreLevelCard
                emoji="🟡"
                label="Regular"
                range="550 — 699"
                color="bg-amber-50 border-amber-200 text-amber-700"
              />
              <ScoreLevelCard
                emoji="🔴"
                label="Deficiente"
                range="400 — 549"
                color="bg-red-50 border-red-200 text-red-700"
              />
              <ScoreLevelCard
                emoji="⚫"
                label="Crítico"
                range="0 — 399"
                color="bg-slate-100 border-slate-200 text-slate-700"
              />
            </div>
          </div>

          {/* Trust signals */}
          <div className="mt-16 text-center">
            <div className="inline-flex items-center gap-2 rounded-full border border-emerald-200 bg-emerald-50 px-6 py-3 text-sm font-medium text-emerald-700">
              <Info className="h-4 w-4" />
              Datos proporcionados por NHTSA (National Highway Traffic Safety Administration) — 100%
              gratuito
            </div>
          </div>
        </section>
      )}
    </div>
  );
}

// --- Internal Components ---

function HowItWorksCard({
  step,
  icon,
  title,
  description,
}: {
  step: number;
  icon: string;
  title: string;
  description: string;
}) {
  return (
    <Card className="relative overflow-hidden">
      <div className="absolute top-2 right-2 flex h-6 w-6 items-center justify-center rounded-full bg-emerald-100 text-xs font-bold text-emerald-700">
        {step}
      </div>
      <CardContent className="p-5">
        <div className="mb-3 text-3xl">{icon}</div>
        <h3 className="font-semibold">{title}</h3>
        <p className="text-muted-foreground mt-1 text-sm">{description}</p>
      </CardContent>
    </Card>
  );
}

function ScoreLevelCard({
  emoji,
  label,
  range,
  color,
}: {
  emoji: string;
  label: string;
  range: string;
  color: string;
}) {
  return (
    <div className={`flex items-center gap-3 rounded-xl border px-5 py-3 ${color}`}>
      <span className="text-xl">{emoji}</span>
      <div>
        <p className="font-semibold">{label}</p>
        <p className="text-xs opacity-70">{range}</p>
      </div>
    </div>
  );
}
