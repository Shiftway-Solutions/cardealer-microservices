'use client';

import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/hooks/use-toast';
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
  Save,
  Loader2,
  RefreshCw,
} from 'lucide-react';

// =============================================================================
// Admin: OKLA Score™ Phase Configuration (with persistence)
// =============================================================================
// Configuration is persisted via ConfigurationService.
// Phase changes and feature toggles are saved to the backend.
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
  features: { label: string; key: string; enabled: boolean }[];
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
      { label: 'Decodificación VIN (NHTSA vPIC)', key: 'vin_decode', enabled: true },
      { label: 'Score simplificado (0-1000)', key: 'basic_score', enabled: true },
      { label: 'Recalls activos (NHTSA)', key: 'recalls', enabled: true },
      { label: 'Calificación seguridad NHTSA', key: 'safety_ratings', enabled: true },
      { label: 'Semáforo básico (verde/amarillo/rojo)', key: 'traffic_light', enabled: true },
      { label: 'Quejas NHTSA por componente', key: 'complaints', enabled: true },
      { label: 'Historial VIN completo', key: 'vin_history', enabled: false },
      { label: 'Precio vs. mercado (fórmula completa)', key: 'price_vs_market', enabled: false },
      { label: 'Badge OKLA en listings', key: 'badge_listings', enabled: false },
      { label: 'API para terceros', key: 'public_api', enabled: false },
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
      { label: 'Decodificación VIN (NHTSA vPIC)', key: 'vin_decode', enabled: true },
      { label: 'Score completo 7 dimensiones', key: 'full_score', enabled: true },
      { label: 'Historial VIN completo (VinAudit)', key: 'vin_history', enabled: true },
      { label: 'Detección fraude odómetro', key: 'odometer_fraud', enabled: true },
      { label: 'Precio vs. mercado (fórmula RD)', key: 'price_vs_market', enabled: true },
      { label: 'Tasa de cambio BCRD en vivo', key: 'bcrd_rate', enabled: true },
      { label: 'Factor_Ajuste_RD (importación)', key: 'factor_ajuste_rd', enabled: true },
      { label: 'Reputación del vendedor (D7)', key: 'seller_reputation', enabled: true },
      { label: 'Badge OKLA en listings', key: 'badge_listings', enabled: false },
      { label: 'Reportes premium', key: 'premium_reports', enabled: false },
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
      { label: 'Todo de Fase 2', key: 'phase_2_all', enabled: true },
      { label: 'Badge OKLA en listings', key: 'badge_listings', enabled: true },
      { label: 'OKLA Certified Excellence (≥850)', key: 'certified_excellence', enabled: true },
      { label: 'Reportes PDF premium', key: 'pdf_reports', enabled: true },
      { label: 'Verificación mecánica en taller', key: 'mechanic_verification', enabled: true },
      { label: 'Comparación con KBB/MarketCheck', key: 'market_comparison', enabled: true },
      { label: 'Historial de Score por VIN', key: 'score_history', enabled: true },
      { label: 'Dashboard analytics para dealers', key: 'dealer_analytics', enabled: true },
      { label: 'Auto-bloqueo de VINs fraudulentos', key: 'auto_block', enabled: false },
      { label: 'API pública para terceros', key: 'public_api', enabled: false },
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
      { label: 'Todo de Fase 3', key: 'phase_3_all', enabled: true },
      { label: 'Score obligatorio para publicar', key: 'mandatory_score', enabled: true },
      { label: 'Auto-bloqueo VINs fraudulentos', key: 'auto_block', enabled: true },
      { label: 'API pública para terceros', key: 'public_api', enabled: true },
      { label: 'Score en app móvil (scan VIN)', key: 'mobile_scan', enabled: true },
      { label: 'Integración con financieras', key: 'finance_integration', enabled: true },
      { label: 'Integración con aseguradoras', key: 'insurance_integration', enabled: true },
      { label: 'Auditoría mecánica obligatoria', key: 'mandatory_audit', enabled: true },
      { label: 'Certificación OKLA para dealers', key: 'dealer_certification', enabled: true },
      { label: 'Penalizaciones por datos falsos', key: 'fraud_penalties', enabled: true },
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

const CONFIG_KEY_PHASE = 'okla_score_phase';
const CONFIG_KEY_TOGGLES = 'okla_score_toggles';

async function loadSavedConfig(): Promise<{
  phase: number;
  toggles: Record<string, boolean>;
} | null> {
  try {
    const res = await fetch('/api/configurations/category/general');
    if (!res.ok) return null;
    const data = await res.json();
    const configs = Array.isArray(data?.data) ? data.data : Array.isArray(data) ? data : [];

    let phase = 1;
    let toggles: Record<string, boolean> = {};

    for (const cfg of configs) {
      if (cfg.key === CONFIG_KEY_PHASE) {
        phase = parseInt(cfg.value, 10) || 1;
      }
      if (cfg.key === CONFIG_KEY_TOGGLES) {
        try {
          toggles = JSON.parse(cfg.value);
        } catch {
          toggles = {};
        }
      }
    }

    return { phase, toggles };
  } catch {
    return null;
  }
}

async function saveConfig(phase: number, toggles: Record<string, boolean>): Promise<boolean> {
  try {
    const configs = [
      {
        key: CONFIG_KEY_PHASE,
        value: String(phase),
        environment: 'Prod',
        description: 'OKLA Score active phase (1-4)',
      },
      {
        key: CONFIG_KEY_TOGGLES,
        value: JSON.stringify(toggles),
        environment: 'Prod',
        description: 'OKLA Score feature toggles per phase',
      },
    ];

    const res = await fetch('/api/configurations/bulk', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(configs),
    });

    return res.ok;
  } catch {
    return false;
  }
}

export default function AdminOklaScorePage() {
  const [activePhase, setActivePhase] = useState(1);
  const [showConfirm, setShowConfirm] = useState(false);
  const [pendingPhase, setPendingPhase] = useState<number | null>(null);
  const [customToggles, setCustomToggles] = useState<Record<string, boolean>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [hasChanges, setHasChanges] = useState(false);
  const { toast } = useToast();

  const currentPhase = PHASES.find(p => p.id === activePhase)!;

  useEffect(() => {
    loadSavedConfig().then(saved => {
      if (saved) {
        setActivePhase(saved.phase);
        setCustomToggles(saved.toggles);
      }
      setIsLoading(false);
    });
  }, []);

  const handlePhaseSelect = (phaseId: number) => {
    if (phaseId === activePhase) return;
    setPendingPhase(phaseId);
    setShowConfirm(true);
  };

  const confirmPhaseChange = () => {
    if (pendingPhase !== null) {
      setActivePhase(pendingPhase);
      setCustomToggles({});
      setHasChanges(true);
    }
    setShowConfirm(false);
    setPendingPhase(null);
  };

  const handleToggle = useCallback((toggleKey: string, checked: boolean) => {
    setCustomToggles(prev => ({ ...prev, [toggleKey]: checked }));
    setHasChanges(true);
  }, []);

  const handleSave = async () => {
    setIsSaving(true);
    const success = await saveConfig(activePhase, customToggles);
    setIsSaving(false);

    if (success) {
      setHasChanges(false);
      toast({
        title: 'Configuración guardada',
        description: `OKLA Score configurado en Fase ${activePhase} — ${currentPhase.name}`,
      });
    } else {
      toast({
        title: 'Error al guardar',
        description: 'No se pudo guardar la configuración. Verifique la conexión con el backend.',
        variant: 'destructive',
      });
    }
  };

  const handleReload = async () => {
    setIsLoading(true);
    const saved = await loadSavedConfig();
    if (saved) {
      setActivePhase(saved.phase);
      setCustomToggles(saved.toggles);
      setHasChanges(false);
    }
    setIsLoading(false);
  };

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-emerald-600" />
        <span className="text-muted-foreground ml-3 text-lg">Cargando configuración...</span>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Header with save/reload */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <ShieldCheck className="h-8 w-8 text-emerald-600" />
            OKLA Score™ — Configuración
          </h1>
          <p className="text-muted-foreground mt-2">
            Selecciona la etapa de implementación del OKLA Score. Cada fase activa nuevas
            capacidades, APIs y dimensiones de evaluación.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={handleReload} disabled={isLoading}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Recargar
          </Button>
          <Button
            size="sm"
            onClick={handleSave}
            disabled={!hasChanges || isSaving}
            className={hasChanges ? 'bg-emerald-600 hover:bg-emerald-700' : ''}
          >
            {isSaving ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}
            {isSaving ? 'Guardando...' : 'Guardar Cambios'}
          </Button>
        </div>
      </div>

      {/* Unsaved changes banner */}
      {hasChanges && (
        <Card className="border-amber-300 bg-amber-50">
          <CardContent className="flex items-center gap-3 p-3">
            <AlertTriangle className="h-4 w-4 text-amber-600" />
            <span className="text-sm font-medium text-amber-800">
              Tienes cambios sin guardar. Haz clic en &quot;Guardar Cambios&quot; para persistir la
              configuración.
            </span>
          </CardContent>
        </Card>
      )}

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
                className={`flex items-center gap-3 rounded-xl border-2 px-5 py-4 transition-all ${
                  isActive
                    ? `${phase.borderColor} ${phase.bgColor} shadow-md ring-2 ring-offset-2`
                    : isPast
                      ? 'border-gray-200 bg-gray-50 opacity-60'
                      : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                }`}
              >
                <div
                  className={`flex h-10 w-10 items-center justify-center rounded-full ${isActive ? phase.bgColor : 'bg-gray-100'}`}
                >
                  {isPast ? (
                    <CheckCircle className="h-5 w-5 text-emerald-500" />
                  ) : (
                    <Icon className={`h-5 w-5 ${isActive ? phase.color : 'text-gray-400'}`} />
                  )}
                </div>
                <div className="text-left">
                  <p className={`text-sm font-bold ${isActive ? phase.color : ''}`}>
                    Fase {phase.id}
                  </p>
                  <p className="text-muted-foreground text-xs">{phase.name}</p>
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
              <AlertTriangle className="mt-0.5 h-5 w-5 text-amber-600" />
              <div className="flex-1">
                <p className="font-semibold text-amber-800">
                  ¿Cambiar a {PHASES[pendingPhase - 1].nameEs}?
                </p>
                <p className="mt-1 text-sm text-amber-700">
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
        <div className="space-y-6 lg:col-span-2">
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
              <p className="text-muted-foreground mb-6 text-sm">{currentPhase.description}</p>
              <h4 className="mb-3 flex items-center gap-2 font-semibold">
                <Settings className="h-4 w-4" />
                Funcionalidades
              </h4>
              <div className="space-y-3">
                {currentPhase.features.map(feature => {
                  const toggleKey = `${activePhase}-${feature.key}`;
                  const isEnabled = customToggles[toggleKey] ?? feature.enabled;
                  return (
                    <div
                      key={feature.key}
                      className="flex items-center justify-between rounded-lg border px-4 py-3"
                    >
                      <div className="flex items-center gap-3">
                        {isEnabled ? (
                          <CheckCircle className="h-4 w-4 text-emerald-500" />
                        ) : (
                          <Circle className="h-4 w-4 text-gray-300" />
                        )}
                        <span
                          className={`text-sm ${isEnabled ? 'font-medium' : 'text-muted-foreground'}`}
                        >
                          {feature.label}
                        </span>
                      </div>
                      <Switch
                        checked={isEnabled}
                        onCheckedChange={checked => handleToggle(toggleKey, checked)}
                      />
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Dimensions */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <BarChart3 className="h-4 w-4" />
                Dimensiones Activas
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                {['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'].map(dim => {
                  const isActive =
                    currentPhase.dimensions.includes(dim) ||
                    currentPhase.dimensions.includes(`${dim} (básico)`);
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
                      <p className="text-muted-foreground font-mono text-xs">{dim}</p>
                      <p className="text-sm font-semibold">{labels[dim]}</p>
                      <p className="text-muted-foreground text-xs">{weights[dim]}</p>
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

        {/* Sidebar */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Globe className="h-4 w-4" />
                APIs Integradas
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {currentPhase.apis.map(api => (
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

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Database className="h-4 w-4" />
                Costo Estimado
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-emerald-600">{currentPhase.estimatedCost}</p>
              <p className="text-muted-foreground mt-1 text-xs">
                Costo mensual estimado para APIs de pago
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Star className="h-4 w-4" />
                Resumen por Fase
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {PHASES.map(phase => {
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
                      <p className={`text-sm ${isActive ? 'font-bold ' + phase.color : ''}`}>
                        Fase {phase.id}
                      </p>
                      <p className="text-muted-foreground text-[10px]">
                        {phase.dimensions.length} dims · {phase.apis.filter(a => a.enabled).length}{' '}
                        APIs
                      </p>
                    </div>
                    {isActive && <Badge className="bg-emerald-600 text-[10px]">ACTIVA</Badge>}
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
