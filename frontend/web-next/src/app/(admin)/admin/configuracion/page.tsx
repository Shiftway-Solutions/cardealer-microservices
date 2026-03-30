/**
 * Admin Configuration Page
 *
 * Platform-wide settings and configuration management
 * Connected to ConfigurationService backend
 */

'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import {
  Settings,
  Globe,
  DollarSign,
  Mail,
  Bell,
  BellRing,
  Shield,
  Image,
  Save,
  RefreshCw,
  AlertTriangle,
  Check,
  Loader2,
  Car,
  UserCheck,
  Database,
  CreditCard,
  ToggleLeft,
  History,
  AlertCircle,
  Smartphone,
  MessageCircle,
  Lock,
  Eye,
  EyeOff,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import {
  configurationService,
  CONFIG_CATEGORIES,
  SECRET_MASK_PLACEHOLDER,
  isSecretKey,
  type ConfigurationItem,
  type FeatureFlag,
  type ConfigCategory,
  type SecretMaskedItem,
} from '@/services/configuration';

// Icon mapping
const ICON_MAP: Record<string, React.ElementType> = {
  Globe,
  DollarSign,
  Mail,
  Bell,
  BellRing,
  Shield,
  Image,
  Car,
  UserCheck,
  Database,
  CreditCard,
  AlertTriangle,
  Smartphone,
  MessageCircle,
  Lock,
};

// Payment provider definitions for billing card
const PAYMENT_PROVIDERS = [
  { prefix: 'azul', name: 'Azul', description: 'Banco Popular RD', type: 'Bancaria' },
  { prefix: 'cardnet', name: 'CardNET', description: 'Bancaria RD', type: 'Bancaria' },
  { prefix: 'pixelpay', name: 'PixelPay', description: 'Comisiones más bajas', type: 'Fintech' },
  {
    prefix: 'fygaro',
    name: 'Fygaro',
    description: 'Agregador de pagos',
    type: 'Agregador',
  },
  {
    prefix: 'stripe',
    name: 'Stripe',
    description: 'Tarjetas internacionales',
    type: 'Internacional',
  },
  {
    prefix: 'paypal',
    name: 'PayPal',
    description: 'Pagos internacionales',
    type: 'Internacional',
  },
];

type ConfigValues = Record<string, string>;

export default function AdminConfigurationPage() {
  const [configValues, setConfigValues] = useState<ConfigValues>({});
  const [originalValues, setOriginalValues] = useState<ConfigValues>({});
  const [configItems, setConfigItems] = useState<ConfigurationItem[]>([]);
  const [featureFlags, setFeatureFlags] = useState<FeatureFlag[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // === Secret field state ===
  // secretMasked: the masked values from backend (e.g. "re_B••••••••")
  const [secretMasked, setSecretMasked] = useState<Record<string, string>>({});
  // secretEdits: new plaintext values the user has typed (only for changed fields)
  const [secretEdits, setSecretEdits] = useState<Record<string, string>>({});
  // editingSecrets: which secret fields the user has clicked "edit" on
  const [editingSecrets, setEditingSecrets] = useState<Set<string>>(new Set());
  // visibleSecrets: which secret fields are showing the typed value (eye toggle)
  const [visibleSecrets, setVisibleSecrets] = useState<Set<string>>(new Set());
  // Payment provider expansion state
  const [expandedProviders, setExpandedProviders] = useState<Set<string>>(new Set());
  const initialExpandDone = useRef(false);

  // Load all configurations
  const loadConfigurations = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [configs, flags, secrets] = await Promise.all([
        configurationService.getAll(),
        configurationService.getFeatureFlags(),
        configurationService.getSecrets(),
      ]);

      setConfigItems(configs);
      setFeatureFlags(flags);

      // Build values map from configs (excluding secret keys)
      const values: ConfigValues = {};
      configs.forEach(config => {
        if (!isSecretKey(config.key)) {
          values[config.key] = config.value;
        }
      });

      setConfigValues(values);
      setOriginalValues({ ...values });
      setHasChanges(false);

      // Build masked values from secrets endpoint
      const masked: Record<string, string> = {};
      secrets.forEach((s: SecretMaskedItem) => {
        masked[s.key] = s.hasValue ? s.maskedValue || SECRET_MASK_PLACEHOLDER : '';
      });
      setSecretMasked(masked);
      setSecretEdits({});
      setEditingSecrets(new Set());
      setVisibleSecrets(new Set());
    } catch (err: unknown) {
      const error = err as { message?: string; response?: { status?: number } };
      if (error.response?.status === 401) {
        setError('No autorizado. Inicia sesión como administrador.');
      } else {
        setError(
          'Error al cargar la configuración. Verifica que el ConfigurationService esté disponible.'
        );
      }
      if (process.env.NODE_ENV === 'development')
        console.error('Error loading configurations:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadConfigurations();
  }, [loadConfigurations]);

  // Track changes (configs + secrets)
  useEffect(() => {
    const configChanged = Object.keys(configValues).some(
      key => configValues[key] !== originalValues[key]
    );
    const secretChanged = Object.keys(secretEdits).length > 0;
    setHasChanges(configChanged || secretChanged);
  }, [configValues, originalValues, secretEdits]);

  // Auto-expand enabled payment providers on initial load
  useEffect(() => {
    if (!loading && !initialExpandDone.current && Object.keys(originalValues).length > 0) {
      initialExpandDone.current = true;
      const enabled = PAYMENT_PROVIDERS.filter(
        p => originalValues[`billing.${p.prefix}_enabled`] === 'true'
      ).map(p => p.prefix);
      if (enabled.length > 0) {
        setExpandedProviders(new Set(enabled));
      }
    }
  }, [loading, originalValues]);

  // Update a config value
  const updateValue = (key: string, value: string) => {
    setConfigValues(prev => ({ ...prev, [key]: value }));
    // Auto-expand provider when enabling, auto-collapse when disabling
    const match = key.match(/^billing\.(\w+)_enabled$/);
    if (match && PAYMENT_PROVIDERS.some(p => p.prefix === match[1])) {
      setExpandedProviders(prev => {
        const next = new Set(prev);
        if (value === 'true') next.add(match[1]);
        else next.delete(match[1]);
        return next;
      });
    }
  };

  // Save all changes (configs via plain endpoint, secrets via encrypted endpoint)
  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      // 1. Save regular config changes (never send secret keys here)
      const changedItems = Object.entries(configValues)
        .filter(([key, value]) => value !== originalValues[key])
        .filter(([key]) => !isSecretKey(key))
        .map(([key, value]) => ({ key, value }));

      if (changedItems.length > 0) {
        await configurationService.bulkUpsert({
          environment: 'Development',
          updatedBy: 'admin',
          items: changedItems,
          changeReason: 'Updated from admin configuration panel',
        });
      }

      // 2. Save secret changes via encrypted endpoint (separate request)
      const secretItems = Object.entries(secretEdits)
        .filter(([, value]) => value && !value.split('').every(c => c === '\u2022'))
        .map(([key, value]) => ({ key, value }));

      if (secretItems.length > 0) {
        await configurationService.bulkUpsertSecrets({
          environment: 'Development',
          updatedBy: 'admin',
          items: secretItems,
        });
      }

      // Reload to get fresh data
      await loadConfigurations();
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (err) {
      setError('Error al guardar la configuración.');
      if (process.env.NODE_ENV === 'development')
        console.error('Error saving configurations:', err);
    } finally {
      setSaving(false);
    }
  };

  // Reset to original values
  const handleReset = () => {
    setConfigValues({ ...originalValues });
    setSecretEdits({});
    setEditingSecrets(new Set());
    setVisibleSecrets(new Set());
  };

  // Toggle a feature flag
  const handleToggleFeatureFlag = async (flag: FeatureFlag) => {
    try {
      await configurationService.updateFeatureFlag(flag.id, {
        id: flag.id,
        isEnabled: !flag.isEnabled,
        rolloutPercentage: flag.rolloutPercentage,
      });
      // Reload flags
      const flags = await configurationService.getFeatureFlags();
      setFeatureFlags(flags);
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error toggling feature flag:', err);
      setError('Error al actualizar feature flag.');
    }
  };

  // Render a config field based on type
  const renderField = (fieldDef: {
    key: string;
    label: string;
    type: string;
    hint?: string;
    options?: Array<{ value: string; label: string }>;
  }) => {
    const value = configValues[fieldDef.key] ?? '';
    const isChanged = value !== (originalValues[fieldDef.key] ?? '');

    switch (fieldDef.type) {
      case 'secret': {
        const isEditing = editingSecrets.has(fieldDef.key);
        const isVisible = visibleSecrets.has(fieldDef.key);
        const maskedVal = secretMasked[fieldDef.key] || '';
        const editVal = secretEdits[fieldDef.key] ?? '';
        const hasStoredValue = !!maskedVal && maskedVal !== '';
        const hasNewValue = editVal.length > 0;

        return (
          <div key={fieldDef.key}>
            <label className="flex items-center gap-2 text-sm font-medium">
              <Lock className="h-3.5 w-3.5 text-amber-600" />
              {fieldDef.label}
              {hasNewValue && (
                <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
              )}
              <span className="text-muted-foreground text-[10px] font-normal">(cifrado)</span>
            </label>
            {!isEditing ? (
              // Show masked value with "edit" button
              <div className="mt-1 flex items-center gap-2">
                <div className="border-input bg-muted/50 flex-1 rounded-md border px-3 py-2 font-mono text-sm tracking-wider">
                  {hasStoredValue ? (
                    maskedVal
                  ) : (
                    <span className="text-muted-foreground italic">Sin configurar</span>
                  )}
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setEditingSecrets(prev => new Set(prev).add(fieldDef.key));
                  }}
                >
                  Editar
                </Button>
              </div>
            ) : (
              // Show editable input with visibility toggle
              <div className="mt-1 flex items-center gap-2">
                <div className="relative flex-1">
                  <Input
                    type={isVisible ? 'text' : 'password'}
                    value={editVal}
                    onChange={e => {
                      setSecretEdits(prev => ({ ...prev, [fieldDef.key]: e.target.value }));
                    }}
                    className="border-amber-400 pr-10 font-mono"
                    placeholder={
                      hasStoredValue
                        ? 'Nuevo valor (deja vacío para mantener)'
                        : 'Ingresa el valor...'
                    }
                    autoComplete="off"
                    data-1p-ignore="true"
                    data-lpignore="true"
                  />
                  <button
                    type="button"
                    className="text-muted-foreground hover:text-foreground absolute top-1/2 right-3 -translate-y-1/2"
                    onClick={() => {
                      setVisibleSecrets(prev => {
                        const next = new Set(prev);
                        if (next.has(fieldDef.key)) next.delete(fieldDef.key);
                        else next.add(fieldDef.key);
                        return next;
                      });
                    }}
                    title={isVisible ? 'Ocultar' : 'Mostrar'}
                  >
                    {isVisible ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setEditingSecrets(prev => {
                      const next = new Set(prev);
                      next.delete(fieldDef.key);
                      return next;
                    });
                    setSecretEdits(prev => {
                      const next = { ...prev };
                      delete next[fieldDef.key];
                      return next;
                    });
                    setVisibleSecrets(prev => {
                      const next = new Set(prev);
                      next.delete(fieldDef.key);
                      return next;
                    });
                  }}
                >
                  Cancelar
                </Button>
              </div>
            )}
            {fieldDef.hint && <p className="text-muted-foreground mt-1 text-xs">{fieldDef.hint}</p>}
          </div>
        );
      }

      case 'toggle':
        return (
          <div key={fieldDef.key}>
            <label className="hover:bg-muted/50 flex cursor-pointer items-center justify-between rounded-lg p-3">
              <div className="flex items-center gap-2">
                <span className="font-medium">{fieldDef.label}</span>
                {isChanged && (
                  <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
                )}
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={value === 'true'}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  value === 'true' ? 'bg-primary/100' : 'bg-gray-300'
                }`}
                onClick={() => updateValue(fieldDef.key, value === 'true' ? 'false' : 'true')}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                    value === 'true' ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </label>
            {fieldDef.hint && (
              <p className="text-muted-foreground mt-0 px-3 pb-2 text-xs">{fieldDef.hint}</p>
            )}
          </div>
        );

      case 'textarea':
        return (
          <div key={fieldDef.key}>
            <label className="flex items-center gap-2 text-sm font-medium">
              {fieldDef.label}
              {isChanged && (
                <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
              )}
            </label>
            <Textarea
              value={value}
              onChange={e => updateValue(fieldDef.key, e.target.value)}
              className={`mt-1 ${isChanged ? 'border-amber-400' : ''}`}
            />
          </div>
        );

      case 'currency':
        return (
          <div key={fieldDef.key}>
            <label className="flex items-center gap-2 text-sm font-medium">
              {fieldDef.label}
              {isChanged && (
                <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
              )}
            </label>
            <div className="relative mt-1">
              <span className="text-muted-foreground absolute top-1/2 left-3 -translate-y-1/2">
                RD$
              </span>
              <Input
                value={value}
                onChange={e => updateValue(fieldDef.key, e.target.value)}
                className={`pl-12 ${isChanged ? 'border-amber-400' : ''}`}
                type="number"
              />
            </div>
            {fieldDef.hint && <p className="text-muted-foreground mt-1 text-xs">{fieldDef.hint}</p>}
          </div>
        );

      case 'number':
        return (
          <div key={fieldDef.key}>
            <label className="flex items-center gap-2 text-sm font-medium">
              {fieldDef.label}
              {isChanged && (
                <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
              )}
            </label>
            <Input
              value={value}
              onChange={e => updateValue(fieldDef.key, e.target.value)}
              className={`mt-1 ${isChanged ? 'border-amber-400' : ''}`}
              type="number"
            />
            {fieldDef.hint && <p className="text-muted-foreground mt-1 text-xs">{fieldDef.hint}</p>}
          </div>
        );

      case 'select':
        return (
          <div key={fieldDef.key}>
            <label className="flex items-center gap-2 text-sm font-medium">
              {fieldDef.label}
              {isChanged && (
                <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
              )}
            </label>
            <select
              value={value}
              onChange={e => updateValue(fieldDef.key, e.target.value)}
              className={`border-input bg-background mt-1 w-full rounded-lg border px-3 py-2 ${isChanged ? 'border-amber-400' : ''}`}
            >
              {fieldDef.options?.map(opt => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        );

      default: // text
        return (
          <div key={fieldDef.key}>
            <label className="flex items-center gap-2 text-sm font-medium">
              {fieldDef.label}
              {isChanged && (
                <span className="h-2 w-2 rounded-full bg-amber-500" title="Modificado" />
              )}
            </label>
            <Input
              value={value}
              onChange={e => updateValue(fieldDef.key, e.target.value)}
              className={`mt-1 ${isChanged ? 'border-amber-400' : ''}`}
            />
            {fieldDef.hint && <p className="text-muted-foreground mt-1 text-xs">{fieldDef.hint}</p>}
          </div>
        );
    }
  };

  // Render billing card with grouped payment provider sections
  const renderBillingCard = () => {
    const category = CONFIG_CATEGORIES['billing'];
    const fields = [...category.keys];

    // Group fields by provider prefix
    const providerFieldsMap: Record<
      string,
      Array<{
        key: string;
        label: string;
        type: string;
        hint?: string;
        options?: Array<{ value: string; label: string }>;
      }>
    > = {};
    const generalFields: Array<{
      key: string;
      label: string;
      type: string;
      hint?: string;
      options?: Array<{ value: string; label: string }>;
    }> = [];

    for (const field of fields) {
      const provider = PAYMENT_PROVIDERS.find(p => field.key.startsWith(`billing.${p.prefix}_`));
      if (provider) {
        if (!providerFieldsMap[provider.prefix]) providerFieldsMap[provider.prefix] = [];
        providerFieldsMap[provider.prefix].push(
          field as {
            key: string;
            label: string;
            type: string;
            hint?: string;
            options?: Array<{ value: string; label: string }>;
          }
        );
      } else {
        generalFields.push(
          field as {
            key: string;
            label: string;
            type: string;
            hint?: string;
            options?: Array<{ value: string; label: string }>;
          }
        );
      }
    }

    // Count changes in this category
    const configChangedCount = fields.filter(
      f => f.type !== 'secret' && (configValues[f.key] ?? '') !== (originalValues[f.key] ?? '')
    ).length;
    const secretChangedCount = fields.filter(
      f => f.type === 'secret' && secretEdits[f.key] !== undefined
    ).length;
    const changedCount = configChangedCount + secretChangedCount;

    // Count active providers
    const activeCount = PAYMENT_PROVIDERS.filter(
      p => configValues[`billing.${p.prefix}_enabled`] === 'true'
    ).length;

    return (
      <Card key="billing">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CreditCard className="h-5 w-5" />
            {category.label}
            <Badge variant="outline" className="ml-1 text-xs">
              {activeCount} activa{activeCount !== 1 ? 's' : ''}
            </Badge>
            {changedCount > 0 && (
              <Badge variant="outline" className="ml-2 border-amber-400 text-amber-600">
                {changedCount} cambio{changedCount > 1 ? 's' : ''}
              </Badge>
            )}
          </CardTitle>
          <CardDescription>
            Habilitar, deshabilitar y configurar las pasarelas de pago de la plataforma
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* General billing settings (default provider, trial days, invoice config) */}
          {generalFields.length > 0 &&
            (() => {
              const genToggles = generalFields.filter(f => f.type === 'toggle');
              const genOther = generalFields.filter(f => f.type !== 'toggle');
              return (
                <>
                  {genOther.length > 0 && (
                    <div
                      className={`grid gap-4 ${genOther.length <= 2 ? 'md:grid-cols-2' : 'md:grid-cols-2 lg:grid-cols-3'}`}
                    >
                      {genOther.map(field => renderField(field))}
                    </div>
                  )}
                  {genToggles.length > 0 && (
                    <div
                      className={
                        genOther.length > 0 ? 'border-border space-y-1 border-t pt-4' : 'space-y-1'
                      }
                    >
                      {genToggles.map(field => renderField(field))}
                    </div>
                  )}
                </>
              );
            })()}

          {/* Payment providers grouped sections */}
          <div className="space-y-3 pt-2">
            <h3 className="text-muted-foreground text-xs font-semibold tracking-wide uppercase">
              Proveedores de Pago
            </h3>
            {PAYMENT_PROVIDERS.map(provider => {
              const providerFields = providerFieldsMap[provider.prefix] || [];
              const enabledKey = `billing.${provider.prefix}_enabled`;
              const isEnabled = configValues[enabledKey] === 'true';
              const isExpanded = expandedProviders.has(provider.prefix);

              // Separate enabled toggle from other config fields
              const configFields = providerFields.filter(f => f.key !== enabledKey);
              const innerToggles = configFields.filter(f => f.type === 'toggle');
              const innerOther = configFields.filter(f => f.type !== 'toggle');

              return (
                <div
                  key={provider.prefix}
                  className={`rounded-lg border transition-colors ${
                    isEnabled
                      ? 'border-primary bg-primary/10/30 dark:border-primary dark:bg-primary/95/20'
                      : 'border-border bg-muted/20'
                  }`}
                >
                  {/* Provider header with toggle */}
                  <div
                    className="flex cursor-pointer items-center justify-between p-4 select-none"
                    onClick={() => {
                      setExpandedProviders(prev => {
                        const next = new Set(prev);
                        if (next.has(provider.prefix)) next.delete(provider.prefix);
                        else next.add(provider.prefix);
                        return next;
                      });
                    }}
                  >
                    <div className="flex items-center gap-3">
                      {isExpanded ? (
                        <ChevronDown className="text-muted-foreground h-4 w-4" />
                      ) : (
                        <ChevronRight className="text-muted-foreground h-4 w-4" />
                      )}
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold">{provider.name}</span>
                          <Badge variant="outline" className="text-xs">
                            {provider.type}
                          </Badge>
                          {isEnabled ? (
                            <Badge className="bg-primary/10 text-primary dark:bg-primary/95 dark:text-primary/60 text-xs">
                              Activo
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="text-xs">
                              Inactivo
                            </Badge>
                          )}
                        </div>
                        <p className="text-muted-foreground mt-0.5 text-xs">
                          {provider.description}
                        </p>
                      </div>
                    </div>
                    {/* Enable/disable toggle */}
                    <button
                      type="button"
                      role="switch"
                      aria-checked={isEnabled}
                      className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors ${
                        isEnabled ? 'bg-primary/100' : 'bg-gray-300'
                      }`}
                      onClick={e => {
                        e.stopPropagation();
                        updateValue(enabledKey, isEnabled ? 'false' : 'true');
                      }}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          isEnabled ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>

                  {/* Provider config fields (collapsible) */}
                  {isExpanded && configFields.length > 0 && (
                    <div className="border-border space-y-4 border-t px-4 pt-3 pb-4">
                      {innerOther.length > 0 && (
                        <div
                          className={`grid gap-4 ${innerOther.length <= 2 ? 'md:grid-cols-2' : 'md:grid-cols-2 lg:grid-cols-3'}`}
                        >
                          {innerOther.map(field => renderField(field))}
                        </div>
                      )}
                      {innerToggles.length > 0 && (
                        <div
                          className={
                            innerOther.length > 0
                              ? 'border-border space-y-1 border-t pt-3'
                              : 'space-y-1'
                          }
                        >
                          {innerToggles.map(field => renderField(field))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </CardContent>
      </Card>
    );
  };

  // Render a category card
  const renderCategoryCard = (categoryKey: ConfigCategory) => {
    const category = CONFIG_CATEGORIES[categoryKey];
    const IconComponent = ICON_MAP[category.icon] || Settings;
    const fields = [...category.keys];

    // Separate toggle fields from other fields
    const toggleFields = fields.filter(f => f.type === 'toggle');
    const otherFields = fields.filter(f => f.type !== 'toggle');

    // Count changes in this category (configs + secrets)
    const configChangedCount = fields.filter(
      f => f.type !== 'secret' && (configValues[f.key] ?? '') !== (originalValues[f.key] ?? '')
    ).length;
    const secretChangedCount = fields.filter(
      f => f.type === 'secret' && secretEdits[f.key] !== undefined
    ).length;
    const changedCount = configChangedCount + secretChangedCount;

    return (
      <Card key={categoryKey}>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <IconComponent className="h-5 w-5" />
            {category.label}
            {changedCount > 0 && (
              <Badge variant="outline" className="ml-2 border-amber-400 text-amber-600">
                {changedCount} cambio{changedCount > 1 ? 's' : ''}
              </Badge>
            )}
          </CardTitle>
          <CardDescription>
            {categoryKey === 'general' && 'Configuración básica del sitio'}
            {categoryKey === 'pricing' && 'Configuración de precios de la plataforma'}
            {categoryKey === 'email' &&
              'Proveedor de email (Resend/SendGrid/SMTP) y configuración de correo'}
            {categoryKey === 'sms' && 'Configuración de Twilio para envío de SMS'}
            {categoryKey === 'push' && 'Firebase Cloud Messaging para notificaciones push'}
            {categoryKey === 'whatsapp' && 'WhatsApp Business API para mensajería'}
            {categoryKey === 'notifications' &&
              'Alertas del sistema y canales de notificación admin'}
            {categoryKey === 'security' &&
              'Configuración de seguridad, autenticación y limitación de peticiones'}
            {categoryKey === 'vehicles' &&
              'Configuración de publicaciones de vehículos y requisitos de verificación para vendedores'}
            {categoryKey === 'kyc' &&
              'Configuración de verificación de identidad y bypass por tipo de cuenta'}
            {categoryKey === 'media' && 'Configuración de archivos y almacenamiento'}
            {categoryKey === 'cache' && 'Configuración de caché y rendimiento'}
            {categoryKey === 'billing' && 'Configuración de pagos y facturación'}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Regular fields in grid */}
          {otherFields.length > 0 && (
            <div
              className={`grid gap-4 ${otherFields.length === 1 ? 'md:grid-cols-1' : otherFields.length === 2 ? 'md:grid-cols-2' : 'md:grid-cols-2 lg:grid-cols-3'}`}
            >
              {otherFields.map(field =>
                renderField(
                  field as {
                    key: string;
                    label: string;
                    type: string;
                    hint?: string;
                    options?: Array<{ value: string; label: string }>;
                  }
                )
              )}
            </div>
          )}

          {/* Toggle fields */}
          {toggleFields.length > 0 && (
            <div
              className={
                otherFields.length > 0 ? 'border-border space-y-1 border-t pt-4' : 'space-y-1'
              }
            >
              {toggleFields.map(field =>
                renderField(field as { key: string; label: string; type: string })
              )}
            </div>
          )}
        </CardContent>
      </Card>
    );
  };

  // Loading state
  if (loading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="text-center">
          <Loader2 className="text-muted-foreground mx-auto h-8 w-8 animate-spin" />
          <p className="text-muted-foreground mt-4">Cargando configuración...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Configuración</h1>
          <p className="text-muted-foreground">Configuración general de la plataforma</p>
          {configItems.length > 0 && (
            <p className="text-muted-foreground mt-1 text-xs">
              {configItems.length} configuraciones • {featureFlags.length} feature flags
            </p>
          )}
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={handleReset} disabled={!hasChanges || saving}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Restablecer
          </Button>
          <Button
            className={saved ? 'bg-green-600 hover:bg-green-700' : 'bg-primary hover:bg-primary/90'}
            onClick={handleSave}
            disabled={!hasChanges || saving}
          >
            {saving ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Guardando...
              </>
            ) : saved ? (
              <>
                <Check className="mr-2 h-4 w-4" />
                Guardado
              </>
            ) : (
              <>
                <Save className="mr-2 h-4 w-4" />
                Guardar Cambios
                {hasChanges && (
                  <Badge variant="secondary" className="ml-2 bg-white/20">
                    {Object.keys(configValues).filter(k => configValues[k] !== originalValues[k])
                      .length + Object.keys(secretEdits).length}
                  </Badge>
                )}
              </>
            )}
          </Button>
        </div>
      </div>

      {/* Error Alert */}
      {error && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex items-center gap-3 py-4">
            <AlertCircle className="h-5 w-5 shrink-0 text-red-600" />
            <div className="flex-1">
              <p className="font-medium text-red-800">{error}</p>
              <p className="text-sm text-red-600">
                Asegúrate de que el ConfigurationService esté disponible a través del Gateway
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={loadConfigurations} className="shrink-0">
              Reintentar
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Configuration Categories */}
      {!error && (
        <>
          {(Object.keys(CONFIG_CATEGORIES) as ConfigCategory[]).map(categoryKey =>
            categoryKey === 'billing' ? renderBillingCard() : renderCategoryCard(categoryKey)
          )}

          {/* Feature Flags Section */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ToggleLeft className="h-5 w-5" />
                Feature Flags
              </CardTitle>
              <CardDescription>
                Habilitar o deshabilitar funcionalidades de la plataforma
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-1">
                {featureFlags.map(flag => (
                  <div
                    key={flag.id}
                    className="hover:bg-muted/50 flex items-center justify-between rounded-lg p-3"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{flag.name}</span>
                        {flag.rolloutPercentage < 100 && (
                          <Badge variant="outline" className="text-xs">
                            {flag.rolloutPercentage}% rollout
                          </Badge>
                        )}
                      </div>
                      {flag.description && (
                        <p className="text-muted-foreground text-sm">{flag.description}</p>
                      )}
                    </div>
                    <button
                      type="button"
                      role="switch"
                      aria-checked={flag.isEnabled}
                      className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors ${
                        flag.isEnabled ? 'bg-primary/100' : 'bg-gray-300'
                      }`}
                      onClick={() => handleToggleFeatureFlag(flag)}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          flag.isEnabled ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>
                ))}
                {featureFlags.length === 0 && (
                  <p className="text-muted-foreground py-4 text-center text-sm">
                    No hay feature flags configurados
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Footer */}
          <div className="text-muted-foreground flex items-center justify-center gap-2 pb-8 text-sm">
            <History className="h-4 w-4" />
            <span>
              Todos los cambios de configuración se registran en el{' '}
              <Link href="/admin/logs" className="text-primary font-medium hover:underline">
                historial de auditoría
              </Link>
            </span>
          </div>
        </>
      )}
    </div>
  );
}
