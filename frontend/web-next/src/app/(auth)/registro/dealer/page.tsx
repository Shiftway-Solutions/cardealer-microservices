/**
 * Dealer Registration Page
 *
 * Registration form for dealers (guest/pre-auth flow).
 * Registers a new user account with AccountType=Dealer,
 * then redirects to email verification.
 *
 * Connected to real APIs — February 2026
 */

'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
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
  Car,
  Building2,
  User,
  MapPin,
  ArrowLeft,
  ArrowRight,
  Check,
  Upload,
  Shield,
  Star,
  TrendingUp,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import Link from 'next/link';
import { useAuth } from '@/hooks/use-auth';
import { createDealer } from '@/services/dealers';
import type { CreateDealerRequest } from '@/services/dealers';
import {
  sanitizeText,
  sanitizeEmail,
  sanitizePhone,
  sanitizeRNC,
  sanitizeUrl,
} from '@/lib/security/sanitize';
import { RD_PROVINCES } from '@/lib/validations/seller-onboarding';

const steps = [
  { id: 1, title: 'Tipo de Cuenta', icon: Building2 },
  { id: 2, title: 'Información del Negocio', icon: User },
  { id: 3, title: 'Ubicación', icon: MapPin },
  { id: 4, title: 'Verificación', icon: Shield },
];

const benefits = [
  'Panel de control profesional',
  'Publicar múltiples vehículos',
  'Estadísticas avanzadas',
  'Importar inventario CSV',
  'CRM de leads incluido',
  'Badge de verificación',
];

export default function DealerRegistrationPage() {
  const router = useRouter();
  const { register, login } = useAuth();
  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [accountType, setAccountType] = useState<'individual' | 'company'>('company');
  const [formData, setFormData] = useState({
    // Step 1
    dealerType: '',
    // Step 2
    businessName: '',
    rnc: '',
    contactName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
    website: '',
    // Step 3
    address: '',
    city: '',
    province: '',
    // Step 4
    agreeTerms: false,
    agreeVerification: false,
  });

  const handleChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (error) setError(null);
  };

  const nextStep = () => {
    if (currentStep < 4) setCurrentStep(currentStep + 1);
  };

  const prevStep = () => {
    if (currentStep > 1) setCurrentStep(currentStep - 1);
  };

  const handleSubmit = async () => {
    // Validate password match
    if (formData.password !== formData.confirmPassword) {
      setError('Las contraseñas no coinciden.');
      return;
    }
    if (formData.password.length < 8) {
      setError('La contraseña debe tener al menos 8 caracteres.');
      return;
    }
    if (!/[A-Z]/.test(formData.password)) {
      setError('La contraseña debe tener al menos una letra mayúscula.');
      return;
    }
    if (!/[a-z]/.test(formData.password)) {
      setError('La contraseña debe tener al menos una letra minúscula.');
      return;
    }
    if (!/\d/.test(formData.password)) {
      setError('La contraseña debe tener al menos un número.');
      return;
    }
    if (!/[^a-zA-Z0-9]/.test(formData.password)) {
      setError('La contraseña debe tener al menos un carácter especial (ej: !@#$%).');
      return;
    }
    if (!formData.businessName.trim()) {
      setError('El nombre del negocio es requerido.');
      return;
    }
    if (!formData.address.trim() || !formData.city.trim() || !formData.province) {
      setError('La dirección completa es requerida.');
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      // Sanitize all inputs before sending
      const sanitizedEmail = sanitizeEmail(formData.email);
      const nameParts = sanitizeText(formData.contactName.trim(), { maxLength: 100 }).split(' ');
      const firstName = nameParts[0] || '';
      const lastName = nameParts.slice(1).join(' ') || firstName;
      const sanitizedPhone = formData.phone ? sanitizePhone(formData.phone) : undefined;

      // Build the dealer profile payload (to be sent after auth)
      const dealerPayload: CreateDealerRequest = {
        businessName: sanitizeText(formData.businessName, { maxLength: 200 }),
        businessRegistrationNumber: formData.rnc ? sanitizeRNC(formData.rnc) : undefined,
        dealerType: formData.dealerType === 'chain' ? 'Chain' : 'Independent',
        email: sanitizedEmail,
        phone: formData.phone,
        website: formData.website ? sanitizeUrl(formData.website) || undefined : undefined,
        address: sanitizeText(formData.address, { maxLength: 300 }),
        city: sanitizeText(formData.city, { maxLength: 100 }),
        state: formData.province,
        country: 'DO',
      };

      // Step 1: Register the user account as a dealer
      await register({
        firstName,
        lastName,
        email: sanitizedEmail,
        phone: sanitizedPhone,
        password: formData.password,
        acceptTerms: formData.agreeTerms,
        accountType: 'dealer',
      });

      // Step 2: Auto-login so we can create the dealer profile immediately
      try {
        await login({
          email: sanitizedEmail,
          password: formData.password,
        });

        // Step 3: Create the dealer profile (user is now authenticated)
        await createDealer(dealerPayload);

        // Success — go to dealer dashboard
        router.push('/mis-vehiculos');
      } catch {
        // Auto-login failed (most likely email verification required).
        // Save the dealer data so it can be created after the user verifies and logs in.
        if (typeof window !== 'undefined') {
          localStorage.setItem('pending-dealer-registration', JSON.stringify(dealerPayload));
        }
        // Redirect to email verification page
        router.push(`/verificar-email?email=${encodeURIComponent(sanitizedEmail)}&dealer=true`);
      }
    } catch (err) {
      const apiError = err as { message?: string };
      setError(apiError.message || 'Error al crear la cuenta. Intenta de nuevo.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const progress = (currentStep / 4) * 100;
  const canSubmit =
    formData.agreeTerms &&
    formData.agreeVerification &&
    formData.businessName &&
    formData.email &&
    formData.password &&
    formData.contactName &&
    !isSubmitting;

  return (
    <div className="from-primary min-h-screen bg-gradient-to-br to-teal-700">
      {/* Header */}
      <div className="bg-white/10 backdrop-blur-sm">
        <div className="mx-auto max-w-6xl px-4 py-4">
          <Link href="/" className="inline-flex items-center gap-2 text-white">
            <div className="bg-card flex h-10 w-10 items-center justify-center rounded-lg">
              <Car className="text-primary h-6 w-6" />
            </div>
            <span className="text-2xl font-bold">OKLA</span>
          </Link>
        </div>
      </div>

      <div className="mx-auto max-w-6xl px-4 py-12">
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
          {/* Left: Benefits */}
          <div className="hidden lg:block">
            <div className="sticky top-8">
              <h2 className="mb-6 text-2xl font-bold text-white">Únete como Dealer</h2>
              <p className="text-primary-foreground mb-8">
                Accede a herramientas profesionales para vender más vehículos.
              </p>

              <div className="space-y-4">
                {benefits.map((benefit, i) => (
                  <div key={i} className="flex items-center gap-3 text-white">
                    <div className="bg-primary/100 flex h-6 w-6 items-center justify-center rounded-full">
                      <Check className="h-4 w-4" />
                    </div>
                    <span>{benefit}</span>
                  </div>
                ))}
              </div>

              {/* Testimonial */}
              <div className="mt-12 rounded-xl bg-white/10 p-6 backdrop-blur-sm">
                <div className="mb-3 flex items-center gap-1">
                  {[1, 2, 3, 4, 5].map(i => (
                    <Star key={i} className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                  ))}
                </div>
                <p className="mb-4 text-white italic">
                  "Desde que me uní a OKLA, mis ventas aumentaron un 40%. El panel es muy fácil de
                  usar."
                </p>
                <p className="text-primary/40 font-medium">— Carlos, Auto Premium RD</p>
              </div>
            </div>
          </div>

          {/* Right: Form */}
          <div className="lg:col-span-2">
            <Card className="shadow-xl">
              <CardHeader>
                <div className="mb-6 flex items-center justify-between">
                  <CardTitle>Registro de Dealer</CardTitle>
                  <span className="text-muted-foreground text-sm">Paso {currentStep} de 4</span>
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
                          className={`flex h-10 w-10 items-center justify-center rounded-full ${
                            isCompleted
                              ? 'bg-primary text-white'
                              : isActive
                                ? 'border-primary bg-primary/10 text-primary border-2'
                                : 'bg-muted text-muted-foreground'
                          }`}
                        >
                          {isCompleted ? (
                            <Check className="h-5 w-5" />
                          ) : (
                            <Icon className="h-5 w-5" />
                          )}
                        </div>
                        <span
                          className={`mt-2 text-xs ${isActive || isCompleted ? 'text-foreground' : 'text-muted-foreground'}`}
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

                {currentStep === 1 && (
                  <div className="space-y-6">
                    <div>
                      <h3 className="mb-4 text-lg font-semibold">¿Qué tipo de dealer eres?</h3>
                      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                        <button
                          onClick={() => handleChange('dealerType', 'independent')}
                          className={`rounded-xl border-2 p-6 text-left transition-colors ${
                            formData.dealerType === 'independent'
                              ? 'border-primary bg-primary/10'
                              : 'border-border hover:border-border'
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
                              ? 'border-primary bg-primary/10'
                              : 'border-border hover:border-border'
                          }`}
                        >
                          <TrendingUp className="text-primary mb-3 h-8 w-8" />
                          <h4 className="font-semibold">Cadena / Multi-sucursal</h4>
                          <p className="text-muted-foreground mt-1 text-sm">
                            Múltiples locales o franquicia
                          </p>
                        </button>
                      </div>
                    </div>

                    <div className="pt-4">
                      <h4 className="mb-3 font-medium">¿Eres persona física o jurídica?</h4>
                      <div className="flex gap-4">
                        <button
                          onClick={() => setAccountType('individual')}
                          className={`flex-1 rounded-lg border-2 px-4 py-3 ${
                            accountType === 'individual'
                              ? 'border-primary bg-primary/10'
                              : 'border-border'
                          }`}
                        >
                          Persona Física
                        </button>
                        <button
                          onClick={() => setAccountType('company')}
                          className={`flex-1 rounded-lg border-2 px-4 py-3 ${
                            accountType === 'company'
                              ? 'border-primary bg-primary/10'
                              : 'border-border'
                          }`}
                        >
                          Empresa (RNC)
                        </button>
                      </div>
                    </div>
                  </div>
                )}

                {currentStep === 2 && (
                  <div className="space-y-4">
                    <h3 className="text-lg font-semibold">Información del Negocio</h3>

                    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                      <div className="md:col-span-2">
                        <label className="mb-2 block text-sm font-medium">
                          Nombre del Negocio *
                        </label>
                        <Input
                          placeholder="Ej: Auto Premium RD"
                          value={formData.businessName}
                          onChange={e => handleChange('businessName', e.target.value)}
                        />
                      </div>

                      {accountType === 'company' && (
                        <div className="md:col-span-2">
                          <label className="mb-2 block text-sm font-medium">RNC *</label>
                          <Input
                            placeholder="000-00000-0"
                            value={formData.rnc}
                            onChange={e => handleChange('rnc', e.target.value)}
                          />
                        </div>
                      )}

                      <div>
                        <label className="mb-2 block text-sm font-medium">
                          Nombre del Contacto *
                        </label>
                        <Input
                          placeholder="Tu nombre completo"
                          value={formData.contactName}
                          onChange={e => handleChange('contactName', e.target.value)}
                        />
                      </div>

                      <div>
                        <label className="mb-2 block text-sm font-medium">
                          Correo Electrónico *
                        </label>
                        <Input
                          type="email"
                          placeholder="email@tudealer.com"
                          value={formData.email}
                          onChange={e => handleChange('email', e.target.value)}
                        />
                      </div>

                      <div>
                        <label className="mb-2 block text-sm font-medium">Contraseña *</label>
                        <Input
                          type="password"
                          placeholder="Mínimo 8 caracteres"
                          value={formData.password}
                          onChange={e => handleChange('password', e.target.value)}
                        />
                      </div>

                      <div>
                        <label className="mb-2 block text-sm font-medium">
                          Confirmar Contraseña *
                        </label>
                        <Input
                          type="password"
                          placeholder="Repetir contraseña"
                          value={formData.confirmPassword}
                          onChange={e => handleChange('confirmPassword', e.target.value)}
                        />
                      </div>

                      <div>
                        <label className="mb-2 block text-sm font-medium">Teléfono *</label>
                        <Input
                          type="tel"
                          placeholder="809-555-0123"
                          value={formData.phone}
                          onChange={e => handleChange('phone', e.target.value)}
                        />
                      </div>

                      <div>
                        <label className="mb-2 block text-sm font-medium">
                          Sitio Web (opcional)
                        </label>
                        <Input
                          placeholder="https://tudealer.com"
                          value={formData.website}
                          onChange={e => handleChange('website', e.target.value)}
                        />
                      </div>
                    </div>
                  </div>
                )}

                {currentStep === 3 && (
                  <div className="space-y-4">
                    <h3 className="text-lg font-semibold">Ubicación del Negocio</h3>

                    <div>
                      <label className="mb-2 block text-sm font-medium">Dirección *</label>
                      <Textarea
                        placeholder="Av. Principal #123, Local 4..."
                        value={formData.address}
                        onChange={e => handleChange('address', e.target.value)}
                      />
                    </div>

                    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                      <div>
                        <label className="mb-2 block text-sm font-medium">Ciudad *</label>
                        <Input
                          placeholder="Santo Domingo"
                          value={formData.city}
                          onChange={e => handleChange('city', e.target.value)}
                        />
                      </div>

                      <div>
                        <label className="mb-2 block text-sm font-medium">Provincia *</label>
                        <Select
                          value={formData.province}
                          onValueChange={v => handleChange('province', v)}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Seleccionar provincia" />
                          </SelectTrigger>
                          <SelectContent>
                            {RD_PROVINCES.map(p => (
                              <SelectItem key={p} value={p}>
                                {p}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
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

                {currentStep === 4 && (
                  <div className="space-y-6">
                    <h3 className="text-lg font-semibold">Verificación</h3>

                    <div className="border-primary bg-primary/10 rounded-xl border p-6">
                      <Shield className="text-primary mb-4 h-10 w-10" />
                      <h4 className="mb-2 text-lg font-semibold">Verificación de Dealer</h4>
                      <p className="text-muted-foreground mb-4 text-sm">
                        Para garantizar la confianza de los compradores, verificamos todos los
                        dealers. Después de registrarte, deberás subir:
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

                    <div className="space-y-4">
                      <div className="flex items-start gap-3">
                        <Checkbox
                          id="terms"
                          checked={formData.agreeTerms as boolean}
                          onCheckedChange={checked =>
                            handleChange('agreeTerms', checked as boolean)
                          }
                        />
                        <label htmlFor="terms" className="text-muted-foreground text-sm">
                          Acepto los{' '}
                          <Link href="/terminos" className="text-primary hover:underline">
                            Términos y Condiciones
                          </Link>{' '}
                          y la{' '}
                          <Link href="/privacidad" className="text-primary hover:underline">
                            Política de Privacidad
                          </Link>
                        </label>
                      </div>

                      <div className="flex items-start gap-3">
                        <Checkbox
                          id="verification"
                          checked={formData.agreeVerification as boolean}
                          onCheckedChange={checked =>
                            handleChange('agreeVerification', checked as boolean)
                          }
                        />
                        <label htmlFor="verification" className="text-muted-foreground text-sm">
                          Entiendo que debo completar la verificación de documentos para activar mi
                          cuenta
                        </label>
                      </div>
                    </div>
                  </div>
                )}

                {/* Navigation */}
                <div className="border-border flex justify-between border-t pt-6">
                  {currentStep > 1 ? (
                    <Button variant="outline" onClick={prevStep}>
                      <ArrowLeft className="mr-2 h-4 w-4" />
                      Anterior
                    </Button>
                  ) : (
                    <Link href="/registro">
                      <Button variant="outline">
                        <ArrowLeft className="mr-2 h-4 w-4" />
                        Volver
                      </Button>
                    </Link>
                  )}

                  {currentStep < 4 ? (
                    <Button className="bg-primary hover:bg-primary/90" onClick={nextStep}>
                      Continuar
                      <ArrowRight className="ml-2 h-4 w-4" />
                    </Button>
                  ) : (
                    <Button
                      className="bg-primary hover:bg-primary/90"
                      disabled={!canSubmit}
                      onClick={handleSubmit}
                    >
                      {isSubmitting ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Creando cuenta...
                        </>
                      ) : (
                        <>
                          Crear Cuenta de Dealer
                          <ArrowRight className="ml-2 h-4 w-4" />
                        </>
                      )}
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>

            <p className="text-primary-foreground mt-6 text-center text-sm">
              ¿Ya tienes cuenta de dealer?{' '}
              <Link href="/login" className="font-medium text-white hover:underline">
                Inicia sesión
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
