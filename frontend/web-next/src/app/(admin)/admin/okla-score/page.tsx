'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Slider } from '@/components/ui/slider';
import { Separator } from '@/components/ui/separator';
import {
  ShieldCheck,
  Rocket,
  Crown,
  Lock,
  CheckCircle,
  Circle,
  ArrowRight,
  Settings,
  Zap,
  Eye,
  Star,
  AlertTriangle,
  Globe,
  Database,
  BarChart3,
  Shield,
} from 'lucide-react';

// =============================================================================
// Admin: OKLA Score™ Phase Configuration
// =============================================================================
// Lets admins select which phase of OKLA Score implementation is active.
// Phase 1: Tierra Fértil — VIN decode, basic scoring, free APIs
// Phase 2: El Espejo — Full 7-dimension scoring, paid APIs
// Phase 3: El Sello — Badge system, premium reports, mechanic verification
// Phase 4: El Estándar — Mandatory score, API for third parties, auto-blocking
// =============================================================================

interface PhaseConfig {
  id: number;
  name: string;
  nameEs: string;
  icon: React.ElementType;
  color: string;
  bgColor: string;
  borderColor: string;
  subtitle: string;
  description: string;
  features: { label: string; enabled: boolean }[];
  apis: { name: string; type: 'free' | 'paid'; enabled: boolean }[];
  dimensions: string[];
  estimatedCost: string;
}

const PHASES: PhaseConfig[] = [
  {
    id: 1,
    name: 'Tierra Fértil',
    nameEs: 'Fase 1 — Tierra Fértil',
    icon: Rocket,
    color: 'text-emerald-700',
    bgColor: 'bg-emerald-50',
    borderColor: 'border-emerald-300',
    subtitle: 'Sé útil primero',
    description:
      'MVP del OKLA Score. Decodificación VIN gratuita vía NHTSA, scoring simplificado con 4 dimensiones. Objetivo: demostrar valor sin costo para el dealer.',
    features: [
      { label: 'Decodificación VIN (NHTSA vPIC)', enabled: true },
      { label: 'Score simplificado (0-1000)', enabled: true },
      { label: 'Recalls activos (NHTSA)', enabled: true },
      { label: 'Calificación seguridad NHTSA', enabled: true },
      { label: 'Semáforo básico (verde/amarillo/rojo)', enabled: true },
      { label: 'Quejas NHTSA por componente', enabled: true },
      { label: 'Historial VIN completo', enabled: false },
      { label: 'Precio vs. mercado (fórmula completa)', enabled: false },
      { label: 'Badge OKLA en listings', enabled: false },
      { label: 'API para terceros', enabled: false },
    ],
    apis: [
      { name: 'NHTSA vPIC (VIN decode)', type: 'free', enabled: true },
      { name: 'NHTSA Recalls', type: 'free', enabled: true },
      { name: 'NHTSA Safety Ratings', type: 'free', enabled: true },
      { name: 'NHTSA Complaints', type: 'free', enabled: true },
      { name: 'VinAudit', type: 'paid', enabled: false },
      { name: 'MarketCheck', type: 'paid', enabled: false },
      { name: 'BCRD Exchange Rate', type: 'free', enabled: false },
    ],
    dimensions: ['D1 (básico)', 'D3', 'D5', 'D6'],
    estimatedCost: 'RD$0/mes',
  },
  {
    id: 2,
    name: 'El Espejo',
    nameEs: 'Fase 2 — El Espejo',
    icon: Eye,
    color: 'text-blue-700',
    bgColor: 'bg-blue-50',
    borderColor: 'border-blue-300',
    subtitle: 'Sé indispensable',
    description:
      'Score completo con 7 dimensiones. Integración VinAudit para historial real, fórmula de precio vs. mercado con Factor_Ajuste_RD, tasa BCRD en vivo.',
    features: [
      { label: 'Decodificación VIN (NHTSA vPIC)', enabled: true },
      { label: 'Score completo 7 dimensiones', enabled: true },
      { label: 'Historial VIN completo (VinAudit)', enabled: true },
      { label: 'Detección fraude odómetro', enabled: true },
      { label: 'Precio vs. mercado (fórmula RD)', enabled: true },
      { label: 'Tasa de cambio BCRD en vivo', enabled: true },
      { label: 'Factor_Ajuste_RD (importación)', enabled: true },
      { label: 'Reputación del vendedor (D7)', enabled: true },
      { label: 'Badge OKLA en listings', enabled: false },
      { label: 'Reportes premium', enabled: false },
    ],
    apis: [
      { name: 'NHTSA vPIC (VIN decode)', type: 'free', enabled: true },
      { name: 'NHTSA Recalls', type: 'free', enabled: true },
      { name: 'NHTSA Safety Ratings', type: 'free', enabled: true },
      { name: 'NHTSA Complaints', type: 'free', enabled: true },
      { name: 'VinAudit', type: 'paid', enabled: true },
      { name: 'BCRD Exchange Rate', type: 'free', enabled: true },
      { name: 'MarketCheck', type: 'paid', enabled: false },
    ],
    dimensions: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'],
    estimatedCost: '~RD$15,000/mes',
  },
  {
    id: 3,
    name: 'El Sello',
    nameEs: 'Fase 3 — El Sello',
    icon: Crown,
    color: 'text-purple-700',
    bgColor: 'bg-purple-50',
    borderColor: 'border-purple-300',
    subtitle: 'Sé aspiracional',
    description:
      'Badge OKLA Certified Excellence visible en listings. Reportes PDF premium para dealers. Verificación mecánica en asociación con talleres autorizados.',
    features: [
      { label: 'Todo de Fase 2', enabled: true },
      { label: 'Badge OKLA en listings', enabled: true },
      { label: 'OKLA Certified Excellence (≥850)', enabled: true },
      { label: 'Reportes PDF premium', enabled: true },
      { label: 'Verificación mecánica en taller', enabled: true },
      { label: 'Comparación con KBB/MarketCheck', enabled: true },
      { label: 'Historial de Score por VIN', enabled: true },
      { label: 'Dashboard analytics para dealers', enabled: true },
      { label: 'Auto-bloqueo de VINs fraudulentos', enabled: false },
      { label: 'API pública para terceros', enabled: false },
    ],
    apis: [
      { name: 'NHTSA vPIC (VIN decode)', type: 'free', enabled: true },
      { name: 'NHTSA Recalls', type: 'free', enabled: true },
      { name: 'NHTSA Safety Ratings', type: 'free', enabled: true },
      { name: 'NHTSA Complaints', type: 'free', enabled: true },
      { name: 'VinAudit', type: 'paid', enabled: true },
      { name: 'MarketCheck', type: 'paid', enabled: true },
      { name: 'BCRD Exchange Rate', type: 'free', enabled: true },
      { name: 'KBB (B2B)', type: 'paid', enabled: true },
    ],
    dimensions: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'],
    estimatedCost: '~RD$55,000/mes',
  },
  {
    id: 4,
    name: 'El Estándar',
    nameEs: 'Fase 4 — El Estándar',
    icon: Lock,
    color: 'text-amber-700',
    bgColor: 'bg-amber-50',
    borderColor: 'border-amber-300',
    subtitle: 'Sé el estándar',
    description:
      'OKLA Score obligatorio para todas las publicaciones. Auto-bloqueo de VINs con título Salvage/Flood. API pública para financieras, aseguradoras y concesionarios.',
    features: [
      { label: 'Todo de Fase 3', enabled: true },
      { label: 'Score obligatorio para publicar', enabled: true },
      { label: 'Auto-bloqueo VINs fraudulentos', enabled: true },
      { label: 'API pública para terceros', enabled: true },
      { label: 'Score en app móvil (scan VIN)', enabled: true },
      { label: 'Integración con financieras', enabled: true },
      { label: 'Integración con aseguradoras', enabled: true },
      { label: 'Auditoría mecánica obligatoria', enabled: true },
      { label: 'Certificación OKLA para dealers', enabled: true },
      { label: 'Penalizaciones por datos falsos', enabled: true },
    ],
    apis: [
      { name: 'NHTSA vPIC (VIN decode)', type: 'free', enabled: true },
      { name: 'NHTSA Recalls', type: 'free', enabled: true },
      { name: 'NHTSA Safety Ratings', type: 'free', enabled: true },
      { name: 'NHTSA Complaints', type: 'free', enabled: true },
      { name: 'VinAudit', type: 'paid', enabled: true },
      { name: 'MarketCheck', type: 'paid', enabled: true },
      { name: 'BCRD Exchange Rate', type: 'free', enabled: true },
      { name: 'KBB (B2B)', type: 'paid', enabled: true },
      { name: 'CARFAX', type: 'paid', enabled: true },
    ],
    dimensions: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'],
    estimatedCost: '~RD$120,000/mes',
  },
];

export default function AdminOklaScorePage() {
  const [activePhase, setActivePhase] = useState(1);
  const [showConfirm, setShowConfirm] = useState(false);
  const [pendingPhase, setPendingPhase] = useState<number | null>(null);

  // Feature toggles for current phase
  const [customToggles, setCustomToggles] = useState<Record<string, boolean>>({});

  const currentPhase = PHASES.find((p) => p.id === activePhase)!;

  const handlePhaseSelect = (phaseId: number) => {
    if (phaseId === activePhase) return;
    setPendingPhase(phaseId);
    setShowConfirm(true);
  };

  const confirmPhaseChange = () => {
    if (pendingPhase !== null) {
      setActivePhase(pendingPhase);
      setCustomToggles({}); // Reset custom toggles
    }
    setShowConfirm(false);
    setPendingPhase(null);
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-3">
          <ShieldCheck className="h-8 w-8 text-emerald-600" />
          OKLA Score™ — Configuración
        </h1>
        <p className="mt-2 text-muted-foreground">
          Selecciona la etapa de implementación del OKLA Score. Cada fase activa
          nuevas capacidades, APIs y dimensiones de evaluación.
        </p>
      </div>

      {/* Phase Timeline */}
      <div className="flex items-center gap-2 overflow-x-auto pb-2">
        {PHASES.map((phase, idx) => {
          const Icon = phase.icon;
          const isActive = phase.id === activePhase;
          const isPast = phase.id < activePhase;

          return (
            <div key={phase.id} className="flex items-center">
              <button
                onClick={() => handlePhaseSelect(phase.id)}
                className={`
                  flex items-center gap-3 rounded-xl border-2 px-5 py-4 transition-all
                  ${isActive
                    ? `${phase.borderColor} ${phase.bgColor} shadow-md ring-2 ring-offset-2 ring-${phase.color.replace('text-', '')}`
                    : isPast
                      ? 'border-gray-200 bg-gray-50 opacity-60'
                      : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                  }
                `}
              >
                <div className={`flex h-10 w-10 items-center justify-center rounded-full ${isActive ? phase.bgColor : 'bg-gray-100'}`}>
                  {isPast ? (
                    <CheckCircle className="h-5 w-5 text-emerald-500" />
                  ) : (
                    <Icon className={`h-5 w-5 ${isActive ? phase.color : 'text-gray-400'}`} />
                  )}
                </div>
                <div className="text-left">
                  <p className={`text-sm font-bold ${isActive ? phase.color : ''}`}>Fase {phase.id}</p>
                  <p className="text-xs text-muted-foreground">{phase.name}</p>
                </div>
              </button>
              {idx < PHASES.length - 1 && (
                <ArrowRight className="mx-1 h-4 w-4 flex-shrink-0 text-gray-300" />
              )}
            </div>
          );
        })}
      </div>

      {/* Confirm Dialog */}
      {showConfirm && pendingPhase !== null && (
        <Card className="border-amber-300 bg-amber-50">
          <CardContent className="p-4">
            <div className="flex items-start gap-3">
              <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5" />
              <div className="flex-1">
                <p className="font-semibold text-amber-800">
                  ¿Cambiar a {PHASES[pendingPhase - 1].nameEs}?
                </p>
                <p className="text-sm text-amber-700 mt-1">
                  {pendingPhase > activePhase
                    ? 'Esto activará nuevas funcionalidades y puede requerir configuración de APIs adicionales.'
                    : 'Esto desactivará funcionalidades. Los datos de scoring existentes no se perderán.'}
                </p>
                <div className="mt-3 flex gap-2">
                  <Button size="sm" onClick={confirmPhaseChange}>
                    Confirmar cambio
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setShowConfirm(false)}>
                    Cancelar
                  </Button>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Active Phase Detail */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main: Features */}
        <div className="lg:col-span-2 space-y-6">
          <Card className={`${currentPhase.borderColor} border-2`}>
            <CardHeader className={currentPhase.bgColor}>
              <div className="flex items-center gap-3">
                <currentPhase.icon className={`h-6 w-6 ${currentPhase.color}`} />
                <div>
                  <CardTitle className={currentPhase.color}>{currentPhase.nameEs}</CardTitle>
                  <CardDescription className="font-medium italic">
                    &ldquo;{currentPhase.subtitle}&rdquo;
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6">
              <p className="text-sm text-muted-foreground mb-6">{currentPhase.description}</p>

              <h4 className="font-semibold mb-3 flex items-center gap-2">
                <Settings className="h-4 w-4" />
                Funcionalidades
              </h4>
              <div className="space-y-3">
                {currentPhase.features.map((feature) => {
                  const toggleKey = `${activePhase}-${feature.label}`;
                  const isEnabled = customToggles[toggleKey] ?? feature.enabled;

                  return (
                    <div
                      key={feature.label}
                      className="flex items-center justify-between rounded-lg border px-4 py-3"
                    >
                      <div className="flex items-center gap-3">
                        {isEnabled ? (
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                        ) : (
                          <Circle className="h-4 w-4 text-gray-300" />
                        )}
                        <span className={`text-sm ${isEnabled ? 'font-medium' : 'text-muted-foreground'}`}>
                          {feature.label}
                        </span>
                      </div>
                      <Switch
                        checked={isEnabled}
                        onCheckedChange={(checked) =>
                          setCustomToggles((prev) => ({ ...prev, [toggleKey]: checked }))
                        }
                      />
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Dimensions Active */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <BarChart3 className="h-4 w-4" />
                Dimensiones Activas
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                {['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'].map((dim) => {
                  const isActive = currentPhase.dimensions.includes(dim) || currentPhase.dimensions.includes(`${dim} (básico)`);
                  const isBasic = currentPhase.dimensions.includes(`${dim} (básico)`);
                  const labels: Record<string, string> = {
                    D1: 'Historial VIN',
                    D2: 'Mecánica',
                    D3: 'Kilometraje',
                    D4: 'Precio',
                    D5: 'Seguridad',
                    D6: 'Depreciación',
                    D7: 'Vendedor',
                  };
                  const weights: Record<string, string> = {
                    D1: '25%',
                    D2: '20%',
                    D3: '18%',
                    D4: '17%',
                    D5: '10%',
                    D6: '6%',
                    D7: '4%',
                  };

                  return (
                    <div
                      key={dim}
                      className={`rounded-lg border p-3 text-center transition-all ${
                        isActive
                          ? 'border-emerald-200 bg-emerald-50'
                          : 'border-gray-100 bg-gray-50 opacity-40'
                      }`}
                    >
                      <p className="text-xs font-mono text-muted-foreground">{dim}</p>
                      <p className="text-sm font-semibold">{labels[dim]}</p>
                      <p className="text-xs text-muted-foreground">{weights[dim]}</p>
                      {isBasic && (
                        <Badge variant="outline" className="mt-1 text-[10px]">
                          Básico
                        </Badge>
                      )}
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar: APIs + Cost */}
        <div className="space-y-6">
          {/* APIs */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Globe className="h-4 w-4" />
                APIs Integradas
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {currentPhase.apis.map((api) => (
                <div
                  key={api.name}
                  className={`flex items-center justify-between rounded-lg border px-3 py-2 text-sm ${
                    api.enabled ? 'border-emerald-200 bg-emerald-50' : 'border-gray-100 bg-gray-50'
                  }`}
                >
                  <div className="flex items-center gap-2">
                    {api.enabled ? (
                      <Zap className="h-3.5 w-3.5 text-emerald-500" />
                    ) : (
                      <Circle className="h-3.5 w-3.5 text-gray-300" />
                    )}
                    <span className={api.enabled ? 'font-medium' : 'text-muted-foreground'}>
                      {api.name}
                    </span>
                  </div>
                  <Badge
                    variant={api.type === 'free' ? 'default' : 'secondary'}
                    className={`text-[10px] ${api.type === 'free' ? 'bg-emerald-100 text-emerald-700' : ''}`}
                  >
                    {api.type === 'free' ? 'GRATIS' : 'PAGO'}
                  </Badge>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Cost Estimate */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Database className="h-4 w-4" />
                Costo Estimado
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-emerald-600">{currentPhase.estimatedCost}</p>
              <p className="text-xs text-muted-foreground mt-1">
                Costo mensual estimado para APIs de pago
              </p>
            </CardContent>
          </Card>

          {/* Phase Comparison */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Star className="h-4 w-4" />
                Resumen por Fase
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {PHASES.map((phase) => {
                const Icon = phase.icon;
                const isActive = phase.id === activePhase;
                return (
                  <div
                    key={phase.id}
                    className={`flex items-center gap-3 rounded-lg border px-3 py-2 ${
                      isActive ? `${phase.borderColor} ${phase.bgColor}` : ''
                    }`}
                  >
                    <Icon className={`h-4 w-4 ${isActive ? phase.color : 'text-gray-400'}`} />
                    <div className="flex-1">
                      <p className={`text-sm ${isActive ? 'font-bold' + ' ' + phase.color : ''}`}>
                        Fase {phase.id}
                      </p>
                      <p className="text-[10px] text-muted-foreground">
                        {phase.dimensions.length} dims · {phase.apis.filter((a) => a.enabled).length} APIs
                      </p>
                    </div>
                    {isActive && (
                      <Badge className="bg-emerald-600 text-[10px]">ACTIVA</Badge>
                    )}
                  </div>
                );
              })}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
