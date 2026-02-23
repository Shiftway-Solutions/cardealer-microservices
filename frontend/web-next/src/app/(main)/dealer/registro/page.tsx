/**
 * Dealer Profile Setup Page
 *
 * For authenticated users with AccountType=Dealer who haven't completed
 * their dealer profile (no dealerId). The middleware redirects here.
 *
 * This is different from /registro/dealer (guest registration).
 * This page is for users already logged in as dealers.
 */

'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
import { Progress } from '@/components/ui/progress';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Building2,
  User,
  MapPin,
  ArrowLeft,
  ArrowRight,
  Check,
  Shield,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { useAuth } from '@/hooks/use-auth';
import { getCurrentDealer } from '@/services/dealers';
import {
  sanitizeText,
  sanitizeEmail,
  sanitizePhone,
  sanitizeRNC,
  sanitizeUrl,
} from '@/lib/security/sanitize';

const steps = [
  { id: 1, title: 'Tipo de Dealer', icon: Building2 },
  { id: 2, title: 'Información', icon: User },
  { id: 3, title: 'Ubicación', icon: MapPin },
  { id: 4, title: 'Confirmación', icon: Shield },
];

const provinces = [
  { value: 'distrito-nacional', label: 'Distrito Nacional' },
  { value: 'santo-domingo', label: 'Santo Domingo' },
  { value: 'santiago', label: 'Santiago' },
  { value: 'puerto-plata', label: 'Puerto Plata' },
  { value: 'la-vega', label: 'La Vega' },
  { value: 'san-pedro-de-macoris', label: 'San Pedro de Macorís' },
  { value: 'la-romana', label: 'La Romana' },
  { value: 'san-cristobal', label: 'San Cristóbal' },
  { value: 'duarte', label: 'Duarte' },
  { value: 'espaillat', label: 'Espaillat' },
  { value: 'la-altagracia', label: 'La Altagracia' },
  { value: 'peravia', label: 'Peravia' },
  { value: 'azua', label: 'Azua' },
  { value: 'barahona', label: 'Barahona' },
  { value: 'monsenor-nouel', label: 'Monseñor Nouel' },
  { value: 'monte-plata', label: 'Monte Plata' },
  { value: 'sanchez-ramirez', label: 'Sánchez Ramírez' },
  { value: 'valverde', label: 'Valverde' },
  { value: 'maria-trinidad-sanchez', label: 'María Trinidad Sánchez' },
  { value: 'samana', label: 'Samaná' },
];

export default function DealerProfileSetupPage() {
  const router = useRouter();
  const { user } = useAuth();
  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    dealerType: '' as 'independent' | 'chain' | '',
    entityType: 'company' as const,
    businessName: '',
    rnc: '',
    contactName: user?.fullName || '',
    email: user?.email || '',
    phone: user?.phone || '',
    instagramUrl: '',
    facebookUrl: '',
    whatsappNumber: '',
    description: '',
    address: '',
    city: '',
    province: '',
    agreeTerms: false,
    agreeVerification: false,
  });

  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [checkingDealer, setCheckingDealer] = useState(true);

  // Check if dealer profile already exists → redirect to dashboard
  useEffect(() => {
    const checkExistingDealer = async () => {
      try {
        const dealer = await getCurrentDealer();
        if (dealer) {
          router.replace('/dealer');
          return;
        }
      } catch {
        // No dealer found, show wizard
      } finally {
        setCheckingDealer(false);
      }
    };
    if (user?.id) {
      checkExistingDealer();
    } else {
      setCheckingDealer(false);
    }
  }, [user?.id, router]);

  const handleChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear field error when user edits
    if (fieldErrors[field]) {
      setFieldErrors(prev => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
  };

  /** Validate current step fields. Returns true if valid. */
  const validateStep = (step: number): boolean => {
    const errors: Record<string, string> = {};

    if (step === 1) {
      if (!formData.dealerType) {
        errors.dealerType = 'Selecciona el tipo de dealer';
      }
    }

    if (step === 2) {
      if (!formData.businessName.trim()) {
        errors.businessName = 'El nombre del negocio es obligatorio';
      }
      if (!formData.rnc.trim()) {
        errors.rnc = 'El RNC es obligatorio';
      }
      if (!formData.email.trim()) {
        errors.email = 'El correo electrónico es obligatorio';
      }
      if (!formData.phone.trim()) {
        errors.phone = 'El teléfono es obligatorio';
      }
    }

    if (step === 3) {
      if (!formData.address.trim()) {
        errors.address = 'La dirección es obligatoria';
      }
      if (!formData.city.trim()) {
        errors.city = 'La ciudad es obligatoria';
      }
      if (!formData.province) {
        errors.province = 'La provincia es obligatoria';
      }
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const nextStep = () => {
    if (!validateStep(currentStep)) return;
    if (currentStep < 4) setCurrentStep(currentStep + 1);
  };

  const prevStep = () => {
    setFieldErrors({});
    if (currentStep > 1) setCurrentStep(currentStep - 1);
  };

  const handleSubmit = async () => {
    setError(null);
    setIsSubmitting(true);

    try {
      if (!user?.id) {
        setError('No se pudo identificar tu usuario. Inicia sesión de nuevo.');
        setIsSubmitting(false);
        return;
      }

      // 1. Create dealer profile in DealerManagementService
      const dealerResponse = await apiClient.post('/api/dealers', {
        userId: user.id,
        businessName: sanitizeText(formData.businessName.trim(), { maxLength: 200 }),
        rnc: sanitizeRNC(formData.rnc),
        type: formData.dealerType,
        email: sanitizeEmail(formData.email),
        phone: sanitizePhone(formData.phone),
        facebookUrl: formData.facebookUrl ? sanitizeUrl(formData.facebookUrl) : undefined,
        instagramUrl: formData.instagramUrl ? sanitizeUrl(formData.instagramUrl) : undefined,
        whatsAppNumber: formData.whatsappNumber
          ? sanitizePhone(formData.whatsappNumber)
          : undefined,
        description: formData.description
          ? sanitizeText(formData.description, { maxLength: 2000 })
          : undefined,
        address: sanitizeText(formData.address.trim(), { maxLength: 500 }),
        city: sanitizeText(formData.city.trim(), { maxLength: 100 }),
        province: formData.province,
      });

      const newDealerId = dealerResponse.data?.id;

      // 2. Sync dealerId to AuthService so JWT includes it
      if (newDealerId) {
        await apiClient.post('/api/auth/set-dealer-id', { dealerId: newDealerId });

        // 3. Force token refresh to get updated JWT with dealerId claim
        // HttpOnly cookies are sent automatically via withCredentials — no manual token needed
        try {
          await apiClient.post('/api/auth/refresh-token');
        } catch {
          // Refresh failed silently — the api-client interceptor will handle it on next 401
        }
      }

      // 4. Redirect to dealer dashboard
      router.push('/dealer');
      router.refresh();
    } catch (err: unknown) {
      const apiErr = err as {
        message?: string;
        code?: string;
        errors?: Array<{ message: string }>;
      };
      const detail = apiErr.errors?.[0]?.message || apiErr.message;
      setError(detail || 'Error al crear el perfil de dealer. Intenta de nuevo.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const progress = (currentStep / 4) * 100;
  const canSubmit = formData.agreeTerms && formData.agreeVerification && formData.businessName;

  // Show loading while checking if dealer already exists
  if (checkingDealer) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
          <p className="text-muted-foreground text-sm">Verificando perfil de dealer...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8">
      <div className="mb-8 text-center">
        <h1 className="text-foreground text-3xl font-bold">Completa tu perfil de Dealer</h1>
        <p className="text-muted-foreground mt-2">
          Para acceder al portal de dealer, necesitas completar la información de tu negocio.
        </p>
      </div>

      <Card>
        <CardHeader>
          <div className="mb-4 flex items-center justify-between">
            <CardTitle className="text-lg">Paso {currentStep} de 4</CardTitle>
          </div>
          <Progress value={progress} className="h-2" />

          {/* Steps indicator */}
          <div className="mt-4 flex justify-between">
            {steps.map(step => {
              const Icon = step.icon;
              const isActive = currentStep === step.id;
              const isCompleted = currentStep > step.id;
              return (
                <div key={step.id} className="flex flex-col items-center">
                  <div
                    className={`flex h-10 w-10 items-center justify-center rounded-full transition-colors ${
                      isCompleted
                        ? 'bg-primary text-primary-foreground'
                        : isActive
                          ? 'border-primary bg-primary/10 text-primary border-2'
                          : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    {isCompleted ? <Check className="h-5 w-5" /> : <Icon className="h-5 w-5" />}
                  </div>
                  <span
                    className={`mt-2 hidden text-xs sm:block ${isActive || isCompleted ? 'text-foreground' : 'text-muted-foreground'}`}
                  >
                    {step.title}
                  </span>
                </div>
              );
            })}
          </div>
        </CardHeader>

        <CardContent className="space-y-6">
          {error && (
            <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {error}
            </div>
          )}

          {/* Step 1: Dealer Type */}
          {currentStep === 1 && (
            <div className="space-y-6">
              <h3 className="text-lg font-semibold">¿Qué tipo de dealer eres?</h3>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <button
                  onClick={() => handleChange('dealerType', 'independent')}
                  className={`rounded-xl border-2 p-6 text-left transition-colors ${
                    formData.dealerType === 'independent'
                      ? 'border-primary bg-primary/5'
                      : 'border-border hover:border-primary/50'
                  }`}
                >
                  <Building2 className="text-primary mb-3 h-8 w-8" />
                  <h4 className="font-semibold">Dealer Independiente</h4>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Un solo local, inventario propio
                  </p>
                </button>
                <button
                  onClick={() => handleChange('dealerType', 'chain')}
                  className={`rounded-xl border-2 p-6 text-left transition-colors ${
                    formData.dealerType === 'chain'
                      ? 'border-primary bg-primary/5'
                      : 'border-border hover:border-primary/50'
                  }`}
                >
                  <Building2 className="text-primary mb-3 h-8 w-8" />
                  <h4 className="font-semibold">Cadena / Multi-sucursal</h4>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Múltiples locales o franquicia
                  </p>
                </button>
              </div>
              {fieldErrors.dealerType && (
                <p className="flex items-center gap-1 text-sm text-red-600">
                  <AlertCircle className="h-3.5 w-3.5" />
                  {fieldErrors.dealerType}
                </p>
              )}
            </div>
          )}

          {/* Step 2: Business Information */}
          {currentStep === 2 && (
            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Información del Negocio</h3>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div className="md:col-span-2">
                  <label className="mb-2 block text-sm font-medium">Nombre del Negocio *</label>
                  <Input
                    placeholder="Ej: Auto Premium RD"
                    value={formData.businessName}
                    onChange={e => handleChange('businessName', e.target.value)}
                    className={fieldErrors.businessName ? 'border-red-500' : ''}
                  />
                  {fieldErrors.businessName && (
                    <p className="mt-1 text-xs text-red-600">{fieldErrors.businessName}</p>
                  )}
                </div>

                <div className="md:col-span-2">
                  <label className="mb-2 block text-sm font-medium">RNC *</label>
                  <Input
                    placeholder="000-00000-0"
                    value={formData.rnc}
                    onChange={e => handleChange('rnc', e.target.value)}
                    className={fieldErrors.rnc ? 'border-red-500' : ''}
                  />
                  {fieldErrors.rnc && (
                    <p className="mt-1 text-xs text-red-600">{fieldErrors.rnc}</p>
                  )}
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Nombre del Contacto *</label>
                  <Input
                    placeholder="Tu nombre"
                    value={formData.contactName}
                    onChange={e => handleChange('contactName', e.target.value)}
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Correo Electrónico *</label>
                  <Input
                    type="email"
                    placeholder="email@tudealer.com"
                    value={formData.email}
                    onChange={e => handleChange('email', e.target.value)}
                    className={fieldErrors.email ? 'border-red-500' : ''}
                  />
                  {fieldErrors.email && (
                    <p className="mt-1 text-xs text-red-600">{fieldErrors.email}</p>
                  )}
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Teléfono *</label>
                  <Input
                    type="tel"
                    placeholder="809-555-0123"
                    value={formData.phone}
                    onChange={e => handleChange('phone', e.target.value)}
                    className={fieldErrors.phone ? 'border-red-500' : ''}
                  />
                  {fieldErrors.phone && (
                    <p className="mt-1 text-xs text-red-600">{fieldErrors.phone}</p>
                  )}
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Instagram (opcional)</label>
                  <Input
                    placeholder="@tu_dealer"
                    value={formData.instagramUrl}
                    onChange={e => handleChange('instagramUrl', e.target.value)}
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Facebook (opcional)</label>
                  <Input
                    placeholder="@tu_dealer"
                    value={formData.facebookUrl}
                    onChange={e => handleChange('facebookUrl', e.target.value)}
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">WhatsApp (opcional)</label>
                  <Input
                    type="tel"
                    placeholder="809-555-0123"
                    value={formData.whatsappNumber}
                    onChange={e => handleChange('whatsappNumber', e.target.value)}
                  />
                </div>

                <div className="md:col-span-2">
                  <label className="mb-2 block text-sm font-medium">
                    Descripción del negocio (opcional)
                  </label>
                  <Textarea
                    placeholder="Cuéntanos sobre tu negocio..."
                    value={formData.description}
                    onChange={e => handleChange('description', e.target.value)}
                    rows={3}
                  />
                </div>
              </div>
            </div>
          )}

          {/* Step 3: Location */}
          {currentStep === 3 && (
            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Ubicación del Negocio</h3>

              <div>
                <label className="mb-2 block text-sm font-medium">Dirección *</label>
                <Textarea
                  placeholder="Av. Principal #123, Local 4..."
                  value={formData.address}
                  onChange={e => handleChange('address', e.target.value)}
                  className={fieldErrors.address ? 'border-red-500' : ''}
                />
                {fieldErrors.address && (
                  <p className="mt-1 text-xs text-red-600">{fieldErrors.address}</p>
                )}
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-medium">Ciudad *</label>
                  <Input
                    placeholder="Santo Domingo"
                    value={formData.city}
                    onChange={e => handleChange('city', e.target.value)}
                    className={fieldErrors.city ? 'border-red-500' : ''}
                  />
                  {fieldErrors.city && (
                    <p className="mt-1 text-xs text-red-600">{fieldErrors.city}</p>
                  )}
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Provincia *</label>
                  <Select
                    value={formData.province}
                    onValueChange={v => handleChange('province', v)}
                  >
                    <SelectTrigger className={fieldErrors.province ? 'border-red-500' : ''}>
                      <SelectValue placeholder="Seleccionar provincia" />
                    </SelectTrigger>
                    <SelectContent>
                      {provinces.map(p => (
                        <SelectItem key={p.value} value={p.value}>
                          {p.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {fieldErrors.province && (
                    <p className="mt-1 text-xs text-red-600">{fieldErrors.province}</p>
                  )}
                </div>
              </div>

              <div className="bg-muted/50 rounded-lg p-4">
                <p className="text-muted-foreground flex items-center gap-2 text-sm">
                  <MapPin className="h-4 w-4" />
                  Podrás agregar múltiples ubicaciones después de completar el registro.
                </p>
              </div>
            </div>
          )}

          {/* Step 4: Confirmation */}
          {currentStep === 4 && (
            <div className="space-y-6">
              <h3 className="text-lg font-semibold">Confirmar y Crear Perfil</h3>

              <div className="bg-primary/5 border-primary/20 rounded-xl border p-6">
                <Shield className="text-primary mb-4 h-10 w-10" />
                <h4 className="mb-2 text-lg font-semibold">Verificación de Dealer</h4>
                <p className="text-muted-foreground mb-4 text-sm">
                  Después de crear tu perfil, deberás subir documentos para verificación:
                </p>
                <ul className="text-muted-foreground space-y-2 text-sm">
                  <li className="flex items-center gap-2">
                    <Check className="text-primary h-4 w-4" />
                    Cédula del representante legal
                  </li>
                  <li className="flex items-center gap-2">
                    <Check className="text-primary h-4 w-4" />
                    RNC y documentos de la empresa
                  </li>
                  <li className="flex items-center gap-2">
                    <Check className="text-primary h-4 w-4" />
                    Fotos del local/establecimiento
                  </li>
                </ul>
              </div>

              {/* Summary */}
              <div className="divide-border divide-y rounded-lg border">
                <div className="flex justify-between p-3 text-sm">
                  <span className="text-muted-foreground">Negocio</span>
                  <span className="font-medium">{formData.businessName || '—'}</span>
                </div>
                <div className="flex justify-between p-3 text-sm">
                  <span className="text-muted-foreground">Tipo</span>
                  <span className="font-medium">
                    {formData.dealerType === 'independent'
                      ? 'Independiente'
                      : formData.dealerType === 'chain'
                        ? 'Cadena'
                        : '—'}
                  </span>
                </div>
                <div className="flex justify-between p-3 text-sm">
                  <span className="text-muted-foreground">Ubicación</span>
                  <span className="font-medium">
                    {formData.city && formData.province
                      ? `${formData.city}, ${provinces.find(p => p.value === formData.province)?.label || formData.province}`
                      : '—'}
                  </span>
                </div>
                <div className="flex justify-between p-3 text-sm">
                  <span className="text-muted-foreground">Contacto</span>
                  <span className="font-medium">{formData.email || '—'}</span>
                </div>
              </div>

              <div className="space-y-4">
                <div className="flex items-start gap-3">
                  <Checkbox
                    id="terms"
                    checked={formData.agreeTerms}
                    onCheckedChange={checked => handleChange('agreeTerms', checked as boolean)}
                  />
                  <label htmlFor="terms" className="text-muted-foreground text-sm">
                    Acepto los Términos y Condiciones y la Política de Privacidad de OKLA
                  </label>
                </div>

                <div className="flex items-start gap-3">
                  <Checkbox
                    id="verification"
                    checked={formData.agreeVerification}
                    onCheckedChange={checked =>
                      handleChange('agreeVerification', checked as boolean)
                    }
                  />
                  <label htmlFor="verification" className="text-muted-foreground text-sm">
                    Entiendo que debo completar la verificación de documentos para activar mi cuenta
                    de dealer
                  </label>
                </div>
              </div>
            </div>
          )}

          {/* Navigation */}
          <div className="border-border flex justify-between border-t pt-6">
            {currentStep > 1 ? (
              <Button variant="outline" onClick={prevStep} disabled={isSubmitting}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Anterior
              </Button>
            ) : (
              <Button variant="outline" onClick={() => router.push('/')} disabled={isSubmitting}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Ir al Inicio
              </Button>
            )}

            {currentStep < 4 ? (
              <Button onClick={nextStep}>
                Continuar
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            ) : (
              <Button onClick={handleSubmit} disabled={!canSubmit || isSubmitting}>
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Creando perfil...
                  </>
                ) : (
                  <>
                    Crear Perfil de Dealer
                    <ArrowRight className="ml-2 h-4 w-4" />
                  </>
                )}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
