/**
 * Create New Ad Campaign — /dealer/publicidad/nueva
 *
 * Step-by-step campaign creation flow for dealers.
 * Covers: objective, targeting, budget, vehicle selection, and launch.
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  ArrowLeft,
  ArrowRight,
  Check,
  Sparkles,
  Target,
  DollarSign,
  Rocket,
  Eye,
  MousePointerClick,
  Users,
  TrendingUp,
  MapPin,
  Calendar,
  Search,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// =============================================================================
// TYPES
// =============================================================================

type CampaignObjective = 'traffic' | 'leads' | 'awareness' | 'conversions';
type AdSlotChoice =
  | 'search_top'
  | 'homepage_featured_grid'
  | 'homepage_recommended'
  | 'search_inline';

interface CampaignDraft {
  name: string;
  objective: CampaignObjective;
  slots: AdSlotChoice[];
  dailyBudget: number;
  maxCpc: number;
  keywords: string[];
  regions: string[];
  startDate: string;
  endDate: string;
}

// =============================================================================
// CONSTANTS
// =============================================================================

const OBJECTIVES = [
  {
    id: 'traffic' as CampaignObjective,
    label: 'Tráfico',
    description: 'Más visitas a tus listados',
    icon: Eye,
    recommended: true,
  },
  {
    id: 'leads' as CampaignObjective,
    label: 'Leads',
    description: 'Generar contactos y WhatsApps',
    icon: Users,
    recommended: false,
  },
  {
    id: 'awareness' as CampaignObjective,
    label: 'Visibilidad',
    description: 'Más impresiones y reconocimiento',
    icon: Eye,
    recommended: false,
  },
  {
    id: 'conversions' as CampaignObjective,
    label: 'Ventas',
    description: 'Optimizar para ventas cerradas',
    icon: TrendingUp,
    recommended: false,
  },
];

const AD_PLACEMENTS = [
  {
    id: 'search_top' as AdSlotChoice,
    label: 'Búsqueda Patrocinada',
    description: 'Primeras posiciones en resultados de búsqueda',
    cpcRange: 'RD$80–350/clic',
    icon: Search,
    popular: true,
  },
  {
    id: 'homepage_featured_grid' as AdSlotChoice,
    label: 'Homepage Destacado',
    description: 'Aparece en la grilla de vehículos destacados',
    cpcRange: 'RD$60–200/clic',
    icon: Sparkles,
    popular: true,
  },
  {
    id: 'homepage_recommended' as AdSlotChoice,
    label: 'Recomendados',
    description: 'Sección "Recomendados para Ti" en homepage',
    cpcRange: 'RD$60–180/clic',
    icon: Target,
    popular: false,
  },
  {
    id: 'search_inline' as AdSlotChoice,
    label: 'Resultados Inline',
    description: 'Intercalado entre resultados orgánicos',
    cpcRange: 'RD$50–150/clic',
    icon: MousePointerClick,
    popular: false,
  },
];

const REGIONS = [
  'Santo Domingo',
  'Santiago',
  'Santo Domingo Este',
  'Santo Domingo Norte',
  'Santo Domingo Oeste',
  'Distrito Nacional',
  'La Romana',
  'Punta Cana',
  'San Pedro de Macorís',
  'La Vega',
  'San Cristóbal',
  'Puerto Plata',
];

// =============================================================================
// STEP COMPONENTS
// =============================================================================

function StepIndicator({ current, total }: { current: number; total: number }) {
  return (
    <div className="flex items-center gap-2">
      {Array.from({ length: total }).map((_, i) => (
        <div key={i} className="flex items-center gap-2">
          <div
            className={cn(
              'flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold transition-colors',
              i < current
                ? 'bg-emerald-600 text-white'
                : i === current
                  ? 'bg-emerald-100 text-emerald-700 ring-2 ring-emerald-600'
                  : 'bg-slate-100 text-slate-400'
            )}
          >
            {i < current ? <Check className="h-4 w-4" /> : i + 1}
          </div>
          {i < total - 1 && (
            <div className={cn('h-0.5 w-8', i < current ? 'bg-emerald-600' : 'bg-slate-200')} />
          )}
        </div>
      ))}
    </div>
  );
}

// =============================================================================
// MAIN
// =============================================================================

export default function NewCampaignPage() {
  const [step, setStep] = useState(0);
  const [draft, setDraft] = useState<CampaignDraft>({
    name: '',
    objective: 'traffic',
    slots: ['search_top'],
    dailyBudget: 500,
    maxCpc: 120,
    keywords: [],
    regions: [],
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
  });
  const [keywordInput, setKeywordInput] = useState('');

  const STEPS = ['Objetivo', 'Ubicación', 'Presupuesto', 'Detalles', 'Revisar'];

  const addKeyword = () => {
    if (keywordInput.trim() && !draft.keywords.includes(keywordInput.trim())) {
      setDraft(d => ({ ...d, keywords: [...d.keywords, keywordInput.trim()] }));
      setKeywordInput('');
    }
  };

  const toggleRegion = (region: string) => {
    setDraft(d => ({
      ...d,
      regions: d.regions.includes(region)
        ? d.regions.filter(r => r !== region)
        : [...d.regions, region],
    }));
  };

  const toggleSlot = (slot: AdSlotChoice) => {
    setDraft(d => ({
      ...d,
      slots: d.slots.includes(slot) ? d.slots.filter(s => s !== slot) : [...d.slots, slot],
    }));
  };

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(amount);

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      {/* Header */}
      <div className="border-b bg-white">
        <div className="mx-auto max-w-3xl px-4 py-6 sm:px-6">
          <div className="mb-6 flex items-center gap-3">
            <Link
              href="/dealer/publicidad"
              className="flex items-center gap-1 text-sm text-slate-500 transition-colors hover:text-slate-700"
            >
              <ArrowLeft className="h-4 w-4" />
              Publicidad
            </Link>
          </div>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-100">
                <Rocket className="h-5 w-5 text-emerald-600" />
              </div>
              <div>
                <h1 className="text-xl font-bold text-slate-900">Nueva Campaña</h1>
                <p className="text-xs text-slate-500">{STEPS[step]}</p>
              </div>
            </div>
            <StepIndicator current={step} total={STEPS.length} />
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        {/* Step 0: Objective */}
        {step === 0 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-slate-900">¿Cuál es tu objetivo?</h2>
            <p className="text-sm text-slate-500">
              Selecciona el objetivo principal de tu campaña.
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              {OBJECTIVES.map(obj => (
                <button
                  key={obj.id}
                  onClick={() => setDraft(d => ({ ...d, objective: obj.id }))}
                  className={cn(
                    'relative rounded-xl border-2 p-5 text-left transition-all',
                    draft.objective === obj.id
                      ? 'border-emerald-600 bg-emerald-50 shadow-sm'
                      : 'border-slate-200 bg-white hover:border-slate-300'
                  )}
                >
                  {obj.recommended && (
                    <Badge className="absolute top-3 right-3 bg-emerald-600 text-[10px] text-white">
                      Recomendado
                    </Badge>
                  )}
                  <obj.icon
                    className={cn(
                      'mb-2 h-6 w-6',
                      draft.objective === obj.id ? 'text-emerald-600' : 'text-slate-400'
                    )}
                  />
                  <p className="font-semibold text-slate-900">{obj.label}</p>
                  <p className="mt-1 text-xs text-slate-500">{obj.description}</p>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Step 1: Placement */}
        {step === 1 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-slate-900">¿Dónde quieres aparecer?</h2>
            <p className="text-sm text-slate-500">Selecciona las ubicaciones de tus anuncios.</p>
            <div className="space-y-3">
              {AD_PLACEMENTS.map(pl => (
                <button
                  key={pl.id}
                  onClick={() => toggleSlot(pl.id)}
                  className={cn(
                    'flex w-full items-center gap-4 rounded-xl border-2 p-4 text-left transition-all',
                    draft.slots.includes(pl.id)
                      ? 'border-emerald-600 bg-emerald-50'
                      : 'border-slate-200 bg-white hover:border-slate-300'
                  )}
                >
                  <div
                    className={cn(
                      'flex h-10 w-10 items-center justify-center rounded-lg',
                      draft.slots.includes(pl.id)
                        ? 'bg-emerald-600 text-white'
                        : 'bg-slate-100 text-slate-400'
                    )}
                  >
                    <pl.icon className="h-5 w-5" />
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <p className="font-semibold text-slate-900">{pl.label}</p>
                      {pl.popular && (
                        <Badge variant="secondary" className="text-[10px]">
                          Popular
                        </Badge>
                      )}
                    </div>
                    <p className="text-xs text-slate-500">{pl.description}</p>
                    <p className="mt-0.5 text-xs font-medium text-emerald-600">{pl.cpcRange}</p>
                  </div>
                  <div
                    className={cn(
                      'flex h-5 w-5 items-center justify-center rounded-full border-2',
                      draft.slots.includes(pl.id)
                        ? 'border-emerald-600 bg-emerald-600'
                        : 'border-slate-300'
                    )}
                  >
                    {draft.slots.includes(pl.id) && <Check className="h-3 w-3 text-white" />}
                  </div>
                </button>
              ))}
            </div>

            {/* Regions */}
            <div className="pt-4">
              <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold text-slate-900">
                <MapPin className="h-4 w-4 text-emerald-600" />
                Regiones objetivo (opcional)
              </h3>
              <div className="flex flex-wrap gap-2">
                {REGIONS.map(region => (
                  <button
                    key={region}
                    onClick={() => toggleRegion(region)}
                    className={cn(
                      'rounded-full px-3 py-1.5 text-xs font-medium transition-colors',
                      draft.regions.includes(region)
                        ? 'bg-emerald-600 text-white'
                        : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                    )}
                  >
                    {region}
                  </button>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Step 2: Budget */}
        {step === 2 && (
          <div className="space-y-6">
            <h2 className="text-lg font-semibold text-slate-900">Presupuesto y puja</h2>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-sm">
                  <DollarSign className="h-4 w-4 text-emerald-600" />
                  Presupuesto diario
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="text-3xl font-bold text-emerald-600">
                  {formatCurrency(draft.dailyBudget)}
                  <span className="text-base font-normal text-slate-400">/día</span>
                </div>
                <input
                  type="range"
                  min={100}
                  max={10000}
                  step={100}
                  value={draft.dailyBudget}
                  onChange={e => setDraft(d => ({ ...d, dailyBudget: Number(e.target.value) }))}
                  className="w-full accent-emerald-600"
                />
                <p className="text-xs text-slate-400">
                  Presupuesto mensual estimado: {formatCurrency(draft.dailyBudget * 30)}
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-sm">
                  <MousePointerClick className="h-4 w-4 text-blue-600" />
                  Puja máxima (CPC)
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="text-2xl font-bold text-blue-600">
                  {formatCurrency(draft.maxCpc)}
                  <span className="text-base font-normal text-slate-400">/clic</span>
                </div>
                <input
                  type="range"
                  min={50}
                  max={500}
                  step={10}
                  value={draft.maxCpc}
                  onChange={e => setDraft(d => ({ ...d, maxCpc: Number(e.target.value) }))}
                  className="w-full accent-blue-600"
                />
                <p className="text-xs text-slate-400">
                  El sistema de subasta de segundo precio te cobra el mínimo necesario para ganar la
                  posición. Nunca pagas más que tu puja máxima.
                </p>
              </CardContent>
            </Card>
          </div>
        )}

        {/* Step 3: Details */}
        {step === 3 && (
          <div className="space-y-6">
            <h2 className="text-lg font-semibold text-slate-900">Detalles de la campaña</h2>

            <Card>
              <CardContent className="space-y-4 pt-6">
                <div>
                  <label className="mb-1.5 block text-sm font-medium text-slate-700">
                    Nombre de la campaña
                  </label>
                  <Input
                    value={draft.name}
                    onChange={e => setDraft(d => ({ ...d, name: e.target.value }))}
                    placeholder="Ej: Campaña SUVs Febrero 2026"
                  />
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-slate-700">
                      <Calendar className="h-3.5 w-3.5" />
                      Fecha de inicio
                    </label>
                    <Input
                      type="date"
                      value={draft.startDate}
                      onChange={e => setDraft(d => ({ ...d, startDate: e.target.value }))}
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-slate-700">
                      <Calendar className="h-3.5 w-3.5" />
                      Fecha de fin (opcional)
                    </label>
                    <Input
                      type="date"
                      value={draft.endDate}
                      onChange={e => setDraft(d => ({ ...d, endDate: e.target.value }))}
                    />
                  </div>
                </div>

                {/* Keywords */}
                {draft.slots.includes('search_top') && (
                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-slate-700">
                      Palabras clave para búsqueda patrocinada
                    </label>
                    <div className="flex gap-2">
                      <Input
                        value={keywordInput}
                        onChange={e => setKeywordInput(e.target.value)}
                        placeholder="Ej: Toyota RAV4 2023"
                        onKeyDown={e => e.key === 'Enter' && (e.preventDefault(), addKeyword())}
                      />
                      <Button type="button" onClick={addKeyword} variant="outline" size="sm">
                        Agregar
                      </Button>
                    </div>
                    {draft.keywords.length > 0 && (
                      <div className="mt-2 flex flex-wrap gap-1.5">
                        {draft.keywords.map(kw => (
                          <Badge
                            key={kw}
                            variant="secondary"
                            className="cursor-pointer hover:bg-red-100"
                            onClick={() =>
                              setDraft(d => ({ ...d, keywords: d.keywords.filter(k => k !== kw) }))
                            }
                          >
                            {kw} ×
                          </Badge>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        )}

        {/* Step 4: Review */}
        {step === 4 && (
          <div className="space-y-6">
            <h2 className="text-lg font-semibold text-slate-900">Revisar y lanzar</h2>

            <Card>
              <CardContent className="space-y-4 pt-6">
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="text-xs text-slate-500">Nombre</p>
                    <p className="font-semibold text-slate-900">{draft.name || '(sin nombre)'}</p>
                  </div>
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="text-xs text-slate-500">Objetivo</p>
                    <p className="font-semibold text-slate-900">
                      {OBJECTIVES.find(o => o.id === draft.objective)?.label}
                    </p>
                  </div>
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="text-xs text-slate-500">Presupuesto diario</p>
                    <p className="font-semibold text-emerald-600">
                      {formatCurrency(draft.dailyBudget)}
                    </p>
                  </div>
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="text-xs text-slate-500">CPC máximo</p>
                    <p className="font-semibold text-blue-600">{formatCurrency(draft.maxCpc)}</p>
                  </div>
                </div>

                <div className="rounded-lg bg-slate-50 p-3">
                  <p className="mb-1 text-xs text-slate-500">Ubicaciones</p>
                  <div className="flex flex-wrap gap-1.5">
                    {draft.slots.map(s => (
                      <Badge key={s} variant="secondary">
                        {AD_PLACEMENTS.find(p => p.id === s)?.label}
                      </Badge>
                    ))}
                  </div>
                </div>

                {draft.regions.length > 0 && (
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="mb-1 text-xs text-slate-500">Regiones</p>
                    <div className="flex flex-wrap gap-1.5">
                      {draft.regions.map(r => (
                        <Badge key={r} variant="outline">
                          {r}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

                {draft.keywords.length > 0 && (
                  <div className="rounded-lg bg-slate-50 p-3">
                    <p className="mb-1 text-xs text-slate-500">Palabras clave</p>
                    <div className="flex flex-wrap gap-1.5">
                      {draft.keywords.map(kw => (
                        <Badge key={kw} variant="secondary">
                          {kw}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

                <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
                  <p className="text-sm font-semibold text-emerald-900">
                    Presupuesto mensual estimado: {formatCurrency(draft.dailyBudget * 30)}
                  </p>
                  <p className="mt-1 text-xs text-emerald-700">
                    Solo pagas por clics reales. El sistema de subasta garantiza el precio más
                    justo.
                  </p>
                </div>
              </CardContent>
            </Card>
          </div>
        )}

        {/* Navigation */}
        <div className="mt-8 flex items-center justify-between border-t border-slate-200 pt-6">
          <Button
            variant="outline"
            onClick={() => setStep(s => Math.max(0, s - 1))}
            disabled={step === 0}
          >
            <ArrowLeft className="mr-1 h-4 w-4" />
            Anterior
          </Button>

          {step < STEPS.length - 1 ? (
            <Button
              className="bg-emerald-600 text-white hover:bg-emerald-700"
              onClick={() => setStep(s => Math.min(STEPS.length - 1, s + 1))}
              disabled={step === 1 && draft.slots.length === 0}
            >
              Siguiente
              <ArrowRight className="ml-1 h-4 w-4" />
            </Button>
          ) : (
            <Button
              className="bg-emerald-600 px-8 text-white hover:bg-emerald-700"
              onClick={() => {
                // In production: POST to /api/advertising/campaigns
                alert('¡Campaña creada! Tu anuncio estará activo en las próximas horas.');
              }}
            >
              <Rocket className="mr-2 h-4 w-4" />
              Lanzar campaña
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
