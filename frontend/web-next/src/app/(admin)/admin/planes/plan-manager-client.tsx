/**
 * Subscription Plan Manager — Admin UI
 *
 * Full-featured admin interface to build and manage subscription plans:
 * - Feature registry: define all platform capabilities
 * - Plan builder: drag features into plans
 * - Pricing configuration
 * - Live preview of plan comparison
 */

'use client';

import { useState, useCallback } from 'react';
import {
  Plus,
  Pencil,
  Trash2,
  Save,
  X,
  Eye,
  Settings2,
  Package,
  Layers,
  CheckCircle2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

// ============================================================
// TYPES
// ============================================================

interface PlanFeature {
  id: string;
  key: string;
  name: string;
  description: string;
  category: FeatureCategory;
  valueType: 'boolean' | 'number' | 'text';
  icon?: string;
  isActive: boolean;
  sortOrder: number;
}

type FeatureCategory =
  | 'listings'
  | 'media'
  | 'analytics'
  | 'marketing'
  | 'communication'
  | 'support'
  | 'advanced'
  | 'portal';

interface PlanFeatureAssignment {
  featureId: string;
  value: string | number | boolean;
  isIncluded: boolean;
}

interface SubscriptionPlan {
  id: string;
  key: string;
  name: string;
  description: string;
  monthlyPrice: number;
  annualPrice: number;
  currency: string;
  targetAudience: 'seller' | 'dealer';
  tier: number;
  isPopular: boolean;
  isActive: boolean;
  features: PlanFeatureAssignment[];
  color: string;
  icon: string;
}

const FEATURE_CATEGORIES: Record<FeatureCategory, { label: string; icon: string }> = {
  listings: { label: 'Publicaciones', icon: '📋' },
  media: { label: 'Medios y Fotos', icon: '📸' },
  analytics: { label: 'Analíticas', icon: '📊' },
  marketing: { label: 'Marketing y Promoción', icon: '📣' },
  communication: { label: 'Comunicación', icon: '💬' },
  support: { label: 'Soporte', icon: '🛟' },
  advanced: { label: 'Avanzado', icon: '⚡' },
  portal: { label: 'Portal Público', icon: '🏪' },
};

// ============================================================
// DEFAULT FEATURES
// ============================================================

const DEFAULT_FEATURES: PlanFeature[] = [
  {
    id: '1',
    key: 'max_listings',
    name: 'Máximo de Publicaciones',
    description: 'Número máximo de vehículos activos',
    category: 'listings',
    valueType: 'number',
    isActive: true,
    sortOrder: 1,
  },
  {
    id: '2',
    key: 'max_photos',
    name: 'Fotos por Vehículo',
    description: 'Máximo de fotos por publicación',
    category: 'media',
    valueType: 'number',
    isActive: true,
    sortOrder: 2,
  },
  {
    id: '3',
    key: 'listing_duration',
    name: 'Duración de Publicación',
    description: 'Días que la publicación permanece activa',
    category: 'listings',
    valueType: 'number',
    isActive: true,
    sortOrder: 3,
  },
  {
    id: '4',
    key: 'view_360',
    name: 'Vista 360°',
    description: 'Permite vista 360 de vehículos',
    category: 'media',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 4,
  },
  {
    id: '5',
    key: 'featured_listings',
    name: 'Publicaciones Destacadas',
    description: 'Cantidad de publicaciones destacadas por mes',
    category: 'marketing',
    valueType: 'number',
    isActive: true,
    sortOrder: 5,
  },
  {
    id: '6',
    key: 'analytics_basic',
    name: 'Analíticas Básicas',
    description: 'Vistas, clics y estadísticas básicas',
    category: 'analytics',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 6,
  },
  {
    id: '7',
    key: 'analytics_advanced',
    name: 'Analíticas Avanzadas',
    description: 'Análisis de mercado, tendencias y reportes',
    category: 'analytics',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 7,
  },
  {
    id: '8',
    key: 'chatbot_ai',
    name: 'ChatBot IA',
    description: 'Asistente virtual con inteligencia artificial',
    category: 'communication',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 8,
  },
  {
    id: '9',
    key: 'whatsapp_integration',
    name: 'Integración WhatsApp',
    description: 'Recibe consultas directamente por WhatsApp',
    category: 'communication',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 9,
  },
  {
    id: '10',
    key: 'crm_leads',
    name: 'CRM de Leads',
    description: 'Gestión completa de prospectos',
    category: 'advanced',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 10,
  },
  {
    id: '11',
    key: 'csv_import',
    name: 'Importación CSV',
    description: 'Importación masiva de inventario',
    category: 'listings',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 11,
  },
  {
    id: '12',
    key: 'boosts_monthly',
    name: 'Boosts Mensuales',
    description: 'Cantidad de boosts incluidos por mes',
    category: 'marketing',
    valueType: 'number',
    isActive: true,
    sortOrder: 12,
  },
  {
    id: '13',
    key: 'public_portal',
    name: 'Portal Público',
    description: 'Página pública con inventario y marca',
    category: 'portal',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 13,
  },
  {
    id: '14',
    key: 'portal_chatbot',
    name: 'ChatBot en Portal',
    description: 'Chatbot IA en el portal público',
    category: 'portal',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 14,
  },
  {
    id: '15',
    key: 'portal_custom_branding',
    name: 'Branding Personalizado',
    description: 'Colores y logo personalizados en portal',
    category: 'portal',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 15,
  },
  {
    id: '16',
    key: 'multi_locations',
    name: 'Múltiples Ubicaciones',
    description: 'Permite gestionar varias sucursales',
    category: 'advanced',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 16,
  },
  {
    id: '17',
    key: 'employees',
    name: 'Empleados',
    description: 'Número de empleados permitidos',
    category: 'advanced',
    valueType: 'number',
    isActive: true,
    sortOrder: 17,
  },
  {
    id: '18',
    key: 'api_access',
    name: 'Acceso API',
    description: 'Acceso programático a la plataforma',
    category: 'advanced',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 18,
  },
  {
    id: '19',
    key: 'priority_support',
    name: 'Soporte Prioritario',
    description: 'Acceso a soporte prioritario',
    category: 'support',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 19,
  },
  {
    id: '20',
    key: 'dedicated_manager',
    name: 'Manager Dedicado',
    description: 'Ejecutivo de cuenta asignado',
    category: 'support',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 20,
  },
  {
    id: '21',
    key: 'video_upload',
    name: 'Video de Vehículo',
    description: 'Permite subir videos del vehículo',
    category: 'media',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 21,
  },
  {
    id: '22',
    key: 'background_removal',
    name: 'Remoción de Fondo',
    description: 'IA que remueve el fondo de las fotos',
    category: 'media',
    valueType: 'boolean',
    isActive: true,
    sortOrder: 22,
  },
];

// ============================================================
// DEFAULT PLANS
// ============================================================

const DEFAULT_SELLER_PLANS: SubscriptionPlan[] = [
  {
    id: 's1',
    key: 'gratis',
    name: 'Gratis',
    description: 'Para vendedores ocasionales',
    monthlyPrice: 0,
    annualPrice: 0,
    currency: 'DOP',
    targetAudience: 'seller',
    tier: 0,
    isPopular: false,
    isActive: true,
    color: 'gray',
    icon: 'Shield',
    features: [
      { featureId: '1', value: 1, isIncluded: true },
      { featureId: '2', value: 10, isIncluded: true },
      { featureId: '3', value: 30, isIncluded: true },
      { featureId: '4', value: false, isIncluded: false },
      { featureId: '6', value: true, isIncluded: true },
    ],
  },
  {
    id: 's2',
    key: 'premium',
    name: 'Premium',
    description: 'Para vendedores frecuentes',
    monthlyPrice: 499,
    annualPrice: 4990,
    currency: 'DOP',
    targetAudience: 'seller',
    tier: 1,
    isPopular: true,
    isActive: true,
    color: 'blue',
    icon: 'Star',
    features: [
      { featureId: '1', value: 5, isIncluded: true },
      { featureId: '2', value: 30, isIncluded: true },
      { featureId: '3', value: 0, isIncluded: true }, // 0 = unlimited
      { featureId: '4', value: true, isIncluded: true },
      { featureId: '5', value: 2, isIncluded: true },
      { featureId: '6', value: true, isIncluded: true },
      { featureId: '7', value: true, isIncluded: true },
      { featureId: '21', value: true, isIncluded: true },
    ],
  },
  {
    id: 's3',
    key: 'pro',
    name: 'Pro',
    description: 'Para vendedores profesionales',
    monthlyPrice: 999,
    annualPrice: 9990,
    currency: 'DOP',
    targetAudience: 'seller',
    tier: 2,
    isPopular: false,
    isActive: true,
    color: 'purple',
    icon: 'Crown',
    features: [
      { featureId: '1', value: 15, isIncluded: true },
      { featureId: '2', value: 50, isIncluded: true },
      { featureId: '3', value: 0, isIncluded: true },
      { featureId: '4', value: true, isIncluded: true },
      { featureId: '5', value: 5, isIncluded: true },
      { featureId: '6', value: true, isIncluded: true },
      { featureId: '7', value: true, isIncluded: true },
      { featureId: '8', value: true, isIncluded: true },
      { featureId: '9', value: true, isIncluded: true },
      { featureId: '12', value: 3, isIncluded: true },
      { featureId: '21', value: true, isIncluded: true },
      { featureId: '22', value: true, isIncluded: true },
    ],
  },
];

const DEFAULT_DEALER_PLANS: SubscriptionPlan[] = [
  {
    id: 'd1',
    key: 'libre',
    name: 'Libre',
    description: 'Para empezar en la plataforma',
    monthlyPrice: 0,
    annualPrice: 0,
    currency: 'DOP',
    targetAudience: 'dealer',
    tier: 0,
    isPopular: false,
    isActive: true,
    color: 'gray',
    icon: 'Shield',
    features: [
      { featureId: '1', value: -1, isIncluded: true },
      { featureId: '2', value: 5, isIncluded: true },
      { featureId: '6', value: true, isIncluded: true },
      { featureId: '13', value: true, isIncluded: true },
    ],
  },
  {
    id: 'd2',
    key: 'visible',
    name: 'Visible',
    description: 'Para dealers que quieren destacar',
    monthlyPrice: 1699,
    annualPrice: 16990,
    currency: 'DOP',
    targetAudience: 'dealer',
    tier: 1,
    isPopular: false,
    isActive: true,
    color: 'blue',
    icon: 'Star',
    features: [
      { featureId: '1', value: -1, isIncluded: true },
      { featureId: '2', value: 10, isIncluded: true },
      { featureId: '4', value: true, isIncluded: true },
      { featureId: '5', value: 3, isIncluded: true },
      { featureId: '6', value: true, isIncluded: true },
      { featureId: '7', value: true, isIncluded: true },
      { featureId: '13', value: true, isIncluded: true },
      { featureId: '14', value: true, isIncluded: true },
      { featureId: '19', value: true, isIncluded: true },
      { featureId: '21', value: true, isIncluded: true },
    ],
  },
  {
    id: 'd3',
    key: 'pro',
    name: 'Pro',
    description: 'Para dealers profesionales',
    monthlyPrice: 5799,
    annualPrice: 57990,
    currency: 'DOP',
    targetAudience: 'dealer',
    tier: 2,
    isPopular: true,
    isActive: true,
    color: 'purple',
    icon: 'Zap',
    features: [
      { featureId: '1', value: -1, isIncluded: true },
      { featureId: '2', value: 15, isIncluded: true },
      { featureId: '4', value: true, isIncluded: true },
      { featureId: '5', value: 10, isIncluded: true },
      { featureId: '6', value: true, isIncluded: true },
      { featureId: '7', value: true, isIncluded: true },
      { featureId: '8', value: true, isIncluded: true },
      { featureId: '9', value: true, isIncluded: true },
      { featureId: '10', value: true, isIncluded: true },
      { featureId: '11', value: true, isIncluded: true },
      { featureId: '12', value: 5, isIncluded: true },
      { featureId: '13', value: true, isIncluded: true },
      { featureId: '14', value: true, isIncluded: true },
      { featureId: '15', value: true, isIncluded: true },
      { featureId: '19', value: true, isIncluded: true },
      { featureId: '21', value: true, isIncluded: true },
      { featureId: '22', value: true, isIncluded: true },
    ],
  },
  {
    id: 'd4',
    key: 'elite',
    name: 'Élite',
    description: 'Todo incluido para los mejores dealers',
    monthlyPrice: 14599,
    annualPrice: 145990,
    currency: 'DOP',
    targetAudience: 'dealer',
    tier: 3,
    isPopular: false,
    isActive: true,
    color: 'amber',
    icon: 'Crown',
    features: DEFAULT_FEATURES.map(f => ({
      featureId: f.id,
      value: f.valueType === 'boolean' ? true : f.valueType === 'number' ? -1 : 'unlimited',
      isIncluded: true,
    })),
  },
];

// ============================================================
// FEATURE EDITOR DIALOG
// ============================================================

function FeatureEditorDialog({
  feature,
  onSave,
  onClose,
}: {
  feature?: PlanFeature;
  onSave: (feature: PlanFeature) => void;
  onClose: () => void;
}) {
  const [idCounter] = useState(() => `f${Math.random().toString(36).slice(2, 10)}`);
  const [form, setForm] = useState<PlanFeature>(
    feature || {
      id: idCounter,
      key: '',
      name: '',
      description: '',
      category: 'listings',
      valueType: 'boolean',
      isActive: true,
      sortOrder: 99,
    }
  );

  return (
    <DialogContent className="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>{feature ? 'Editar Funcionalidad' : 'Nueva Funcionalidad'}</DialogTitle>
      </DialogHeader>
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label>Nombre</Label>
            <Input
              value={form.name}
              onChange={e => setForm({ ...form, name: e.target.value })}
              placeholder="Ej: Vista 360°"
            />
          </div>
          <div>
            <Label>Clave (key)</Label>
            <Input
              value={form.key}
              onChange={e => setForm({ ...form, key: e.target.value })}
              placeholder="Ej: view_360"
            />
          </div>
        </div>
        <div>
          <Label>Descripción</Label>
          <Textarea
            value={form.description}
            onChange={e => setForm({ ...form, description: e.target.value })}
            placeholder="Describe la funcionalidad..."
            rows={2}
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label>Categoría</Label>
            <Select
              value={form.category}
              onValueChange={v => setForm({ ...form, category: v as FeatureCategory })}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(FEATURE_CATEGORIES).map(([key, { label, icon }]) => (
                  <SelectItem key={key} value={key}>
                    {icon} {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label>Tipo de Valor</Label>
            <Select
              value={form.valueType}
              onValueChange={v =>
                setForm({ ...form, valueType: v as 'boolean' | 'number' | 'text' })
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="boolean">Sí/No</SelectItem>
                <SelectItem value="number">Número</SelectItem>
                <SelectItem value="text">Texto</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Switch
            checked={form.isActive}
            onCheckedChange={checked => setForm({ ...form, isActive: checked })}
          />
          <Label>Activa</Label>
        </div>
      </div>
      <DialogFooter>
        <Button variant="outline" onClick={onClose}>
          Cancelar
        </Button>
        <Button
          onClick={() => {
            if (!form.name || !form.key) {
              toast.error('Nombre y clave son obligatorios');
              return;
            }
            onSave(form);
            onClose();
          }}
        >
          <Save className="mr-2 h-4 w-4" />
          Guardar
        </Button>
      </DialogFooter>
    </DialogContent>
  );
}

// ============================================================
// PLAN COMPARISON PREVIEW
// ============================================================

function PlanComparisonPreview({
  plans,
  features,
  audience,
}: {
  plans: SubscriptionPlan[];
  features: PlanFeature[];
  audience: 'seller' | 'dealer';
}) {
  const filteredPlans = plans.filter(p => p.targetAudience === audience && p.isActive);
  const categories = Object.entries(FEATURE_CATEGORIES);

  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-[250px]">Funcionalidad</TableHead>
            {filteredPlans.map(plan => (
              <TableHead key={plan.id} className="text-center">
                <div>
                  <p className="font-bold">{plan.name}</p>
                  <p className="text-muted-foreground text-xs">
                    {plan.monthlyPrice === 0
                      ? 'Gratis'
                      : `RD$${plan.monthlyPrice.toLocaleString()}/mes`}
                  </p>
                  {plan.isPopular && <Badge className="mt-1 bg-emerald-500 text-xs">Popular</Badge>}
                </div>
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {categories.map(([catKey, { label, icon }]) => {
            const catFeatures = features.filter(f => f.category === catKey && f.isActive);
            if (catFeatures.length === 0) return null;

            return (
              <>
                <TableRow key={catKey} className="bg-muted/50">
                  <TableCell colSpan={filteredPlans.length + 1} className="font-semibold">
                    {icon} {label}
                  </TableCell>
                </TableRow>
                {catFeatures.map(feature => (
                  <TableRow key={feature.id}>
                    <TableCell>
                      <div>
                        <p className="text-sm font-medium">{feature.name}</p>
                        <p className="text-muted-foreground text-xs">{feature.description}</p>
                      </div>
                    </TableCell>
                    {filteredPlans.map(plan => {
                      const assignment = plan.features.find(f => f.featureId === feature.id);
                      return (
                        <TableCell key={plan.id} className="text-center">
                          {!assignment || !assignment.isIncluded ? (
                            <X className="mx-auto h-4 w-4 text-gray-300" />
                          ) : feature.valueType === 'boolean' ? (
                            <CheckCircle2 className="mx-auto h-5 w-5 text-emerald-500" />
                          ) : (
                            <span className="font-semibold text-emerald-600">
                              {assignment.value === -1 || assignment.value === 0
                                ? '∞'
                                : String(assignment.value)}
                            </span>
                          )}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                ))}
              </>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

// ============================================================
// MAIN COMPONENT
// ============================================================

export function SubscriptionPlanManager() {
  const [features, setFeatures] = useState<PlanFeature[]>(DEFAULT_FEATURES);
  const [sellerPlans, setSellerPlans] = useState<SubscriptionPlan[]>(DEFAULT_SELLER_PLANS);
  const [dealerPlans, setDealerPlans] = useState<SubscriptionPlan[]>(DEFAULT_DEALER_PLANS);
  const [activeTab, setActiveTab] = useState('features');
  const [editingFeature, setEditingFeature] = useState<PlanFeature | undefined>();
  const [isFeatureDialogOpen, setIsFeatureDialogOpen] = useState(false);
  const [previewAudience, setPreviewAudience] = useState<'seller' | 'dealer'>('dealer');

  const handleSaveFeature = useCallback((feature: PlanFeature) => {
    setFeatures(prev => {
      const idx = prev.findIndex(f => f.id === feature.id);
      if (idx >= 0) {
        const updated = [...prev];
        updated[idx] = feature;
        return updated;
      }
      return [...prev, feature];
    });
    toast.success(`Funcionalidad "${feature.name}" guardada`);
  }, []);

  const handleDeleteFeature = useCallback((id: string) => {
    setFeatures(prev => prev.filter(f => f.id !== id));
    toast.success('Funcionalidad eliminada');
  }, []);

  const handleTogglePlanFeature = useCallback(
    (
      planId: string,
      featureId: string,
      value: string | number | boolean,
      audience: 'seller' | 'dealer'
    ) => {
      const setter = audience === 'seller' ? setSellerPlans : setDealerPlans;
      setter(prev =>
        prev.map(plan => {
          if (plan.id !== planId) return plan;
          const existing = plan.features.find(f => f.featureId === featureId);
          if (existing) {
            return {
              ...plan,
              features: plan.features.map(f =>
                f.featureId === featureId ? { ...f, isIncluded: !f.isIncluded, value } : f
              ),
            };
          }
          return {
            ...plan,
            features: [...plan.features, { featureId, value, isIncluded: true }],
          };
        })
      );
    },
    []
  );

  const handleUpdatePlanPrice = useCallback(
    (
      planId: string,
      field: 'monthlyPrice' | 'annualPrice',
      value: number,
      audience: 'seller' | 'dealer'
    ) => {
      const setter = audience === 'seller' ? setSellerPlans : setDealerPlans;
      setter(prev => prev.map(plan => (plan.id === planId ? { ...plan, [field]: value } : plan)));
    },
    []
  );

  const handleSaveAll = useCallback(() => {
    // In a production implementation, this would POST to the backend
    toast.success('Configuración de planes guardada exitosamente');
  }, []);

  const allPlans = [...sellerPlans, ...dealerPlans];

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Gestión de Planes de Suscripción</h1>
          <p className="mt-1 text-sm text-gray-500">
            Define funcionalidades, configura planes y establece precios.
          </p>
        </div>
        <Button onClick={handleSaveAll} className="gap-2 bg-emerald-600 hover:bg-emerald-700">
          <Save className="h-4 w-4" />
          Guardar Todo
        </Button>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="features" className="gap-2">
            <Package className="h-4 w-4" />
            Funcionalidades
          </TabsTrigger>
          <TabsTrigger value="seller-plans" className="gap-2">
            <Settings2 className="h-4 w-4" />
            Planes Sellers
          </TabsTrigger>
          <TabsTrigger value="dealer-plans" className="gap-2">
            <Layers className="h-4 w-4" />
            Planes Dealers
          </TabsTrigger>
          <TabsTrigger value="preview" className="gap-2">
            <Eye className="h-4 w-4" />
            Vista Previa
          </TabsTrigger>
        </TabsList>

        {/* ========== FEATURES TAB ========== */}
        <TabsContent value="features" className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-500">{features.length} funcionalidades definidas</p>
            <Dialog open={isFeatureDialogOpen} onOpenChange={setIsFeatureDialogOpen}>
              <DialogTrigger asChild>
                <Button
                  size="sm"
                  className="gap-2"
                  onClick={() => {
                    setEditingFeature(undefined);
                    setIsFeatureDialogOpen(true);
                  }}
                >
                  <Plus className="h-4 w-4" />
                  Nueva Funcionalidad
                </Button>
              </DialogTrigger>
              {isFeatureDialogOpen && (
                <FeatureEditorDialog
                  feature={editingFeature}
                  onSave={handleSaveFeature}
                  onClose={() => setIsFeatureDialogOpen(false)}
                />
              )}
            </Dialog>
          </div>

          {Object.entries(FEATURE_CATEGORIES).map(([catKey, { label, icon }]) => {
            const catFeatures = features.filter(f => f.category === catKey);
            if (catFeatures.length === 0) return null;

            return (
              <Card key={catKey}>
                <CardHeader className="py-3">
                  <CardTitle className="text-base">
                    {icon} {label}
                  </CardTitle>
                </CardHeader>
                <CardContent className="p-0">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Nombre</TableHead>
                        <TableHead>Clave</TableHead>
                        <TableHead>Tipo</TableHead>
                        <TableHead>Estado</TableHead>
                        <TableHead className="w-[100px]">Acciones</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {catFeatures.map(feature => (
                        <TableRow key={feature.id}>
                          <TableCell>
                            <div>
                              <p className="font-medium">{feature.name}</p>
                              <p className="text-muted-foreground text-xs">{feature.description}</p>
                            </div>
                          </TableCell>
                          <TableCell>
                            <code className="bg-muted rounded px-2 py-0.5 text-xs">
                              {feature.key}
                            </code>
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline" className="text-xs">
                              {feature.valueType === 'boolean'
                                ? 'Sí/No'
                                : feature.valueType === 'number'
                                  ? 'Número'
                                  : 'Texto'}
                            </Badge>
                          </TableCell>
                          <TableCell>
                            {feature.isActive ? (
                              <Badge className="bg-emerald-100 text-emerald-700">Activa</Badge>
                            ) : (
                              <Badge variant="secondary">Inactiva</Badge>
                            )}
                          </TableCell>
                          <TableCell>
                            <div className="flex gap-1">
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8"
                                onClick={() => {
                                  setEditingFeature(feature);
                                  setIsFeatureDialogOpen(true);
                                }}
                              >
                                <Pencil className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-red-500 hover:text-red-700"
                                onClick={() => handleDeleteFeature(feature.id)}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </CardContent>
              </Card>
            );
          })}
        </TabsContent>

        {/* ========== PLAN EDITOR TABS ========== */}
        {(['seller-plans', 'dealer-plans'] as const).map(tabKey => {
          const audience = tabKey === 'seller-plans' ? 'seller' : 'dealer';
          const plans = audience === 'seller' ? sellerPlans : dealerPlans;

          return (
            <TabsContent key={tabKey} value={tabKey} className="space-y-4">
              <div className="grid gap-4 lg:grid-cols-2 xl:grid-cols-4">
                {plans.map(plan => (
                  <Card
                    key={plan.id}
                    className={cn('relative', plan.isPopular && 'ring-2 ring-emerald-500')}
                  >
                    {plan.isPopular && (
                      <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                        <Badge className="bg-emerald-500">Más Popular</Badge>
                      </div>
                    )}
                    <CardHeader className="pb-3">
                      <CardTitle className="flex items-center justify-between">
                        <span>{plan.name}</span>
                        <Switch
                          checked={plan.isActive}
                          onCheckedChange={checked => {
                            const setter = audience === 'seller' ? setSellerPlans : setDealerPlans;
                            setter(prev =>
                              prev.map(p => (p.id === plan.id ? { ...p, isActive: checked } : p))
                            );
                          }}
                        />
                      </CardTitle>
                      <CardDescription>{plan.description}</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      {/* Pricing */}
                      <div className="space-y-2">
                        <div>
                          <Label className="text-xs">Precio Mensual (DOP)</Label>
                          <Input
                            type="number"
                            value={plan.monthlyPrice}
                            onChange={e =>
                              handleUpdatePlanPrice(
                                plan.id,
                                'monthlyPrice',
                                Number(e.target.value),
                                audience
                              )
                            }
                            className="h-8"
                          />
                        </div>
                        <div>
                          <Label className="text-xs">Precio Anual (DOP)</Label>
                          <Input
                            type="number"
                            value={plan.annualPrice}
                            onChange={e =>
                              handleUpdatePlanPrice(
                                plan.id,
                                'annualPrice',
                                Number(e.target.value),
                                audience
                              )
                            }
                            className="h-8"
                          />
                        </div>
                      </div>

                      <Separator />

                      {/* Features Toggle */}
                      <div className="max-h-[300px] space-y-2 overflow-y-auto">
                        <p className="text-xs font-semibold text-gray-500">Funcionalidades:</p>
                        {features
                          .filter(f => f.isActive)
                          .map(feature => {
                            const assignment = plan.features.find(f => f.featureId === feature.id);
                            const isIncluded = assignment?.isIncluded ?? false;

                            return (
                              <div
                                key={feature.id}
                                className="flex items-center justify-between text-sm"
                              >
                                <span className={cn('text-xs', !isIncluded && 'text-gray-400')}>
                                  {feature.name}
                                </span>
                                <Switch
                                  checked={isIncluded}
                                  onCheckedChange={() =>
                                    handleTogglePlanFeature(
                                      plan.id,
                                      feature.id,
                                      feature.valueType === 'boolean' ? true : -1,
                                      audience
                                    )
                                  }
                                  className="scale-75"
                                />
                              </div>
                            );
                          })}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </TabsContent>
          );
        })}

        {/* ========== PREVIEW TAB ========== */}
        <TabsContent value="preview" className="space-y-4">
          <div className="flex items-center gap-4">
            <Label>Vista previa de planes:</Label>
            <Select
              value={previewAudience}
              onValueChange={v => setPreviewAudience(v as 'seller' | 'dealer')}
            >
              <SelectTrigger className="w-[200px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="seller">Vendedores (Sellers)</SelectItem>
                <SelectItem value="dealer">Dealers</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <Card>
            <CardContent className="p-0">
              <PlanComparisonPreview
                plans={allPlans}
                features={features}
                audience={previewAudience}
              />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
