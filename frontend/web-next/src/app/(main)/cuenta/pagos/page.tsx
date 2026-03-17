/**
 * Payments Page
 *
 * User payments history, Early Bird status, and saved payment methods
 */

'use client';

import * as React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  CreditCard,
  Receipt,
  Star,
  CheckCircle,
  AlertCircle,
  Loader2,
  ChevronRight,
  Gift,
  Sparkles,
  Calendar,
  Plus,
  Trash2,
  Check,
  Shield,
  ExternalLink,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  userBillingService,
  availableGateways,
  type EarlyBirdStatus,
  type PaymentMethodInfo,
  type PaymentGateway,
  type TokenizationInitResponse,
} from '@/services/user-billing';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

// ============================================================================
// STAT CARD COMPONENT
// ============================================================================

function StatCard({
  title,
  value,
  description,
  icon: Icon,
  className,
}: {
  title: string;
  value: string | number;
  description?: string;
  icon: React.ElementType;
  className?: string;
}) {
  return (
    <Card className={className}>
      <CardContent className="pt-6">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-muted-foreground text-sm font-medium">{title}</p>
            <p className="mt-1 text-2xl font-semibold">{value}</p>
            {description && <p className="text-muted-foreground mt-1 text-sm">{description}</p>}
          </div>
          <div className="rounded-lg bg-blue-50 p-3">
            <Icon className="h-5 w-5 text-blue-600" />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// ============================================================================
// EARLY BIRD CARD COMPONENT
// ============================================================================

function EarlyBirdCard({
  status,
  onEnroll,
  isEnrolling,
}: {
  status: EarlyBirdStatus | null;
  onEnroll: () => void;
  isEnrolling: boolean;
}) {
  if (!status) {
    return (
      <Card className="border-amber-200 bg-gradient-to-br from-amber-50 to-orange-50">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Gift className="h-5 w-5 text-amber-600" />
            <CardTitle className="text-lg">Programa Early Bird</CardTitle>
          </div>
          <CardDescription>Cargando información...</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  if (status.isEnrolled) {
    return (
      <Card
        className={cn(
          'border-2',
          status.isInFreePeriod
            ? 'to-primary/5 border-green-200 bg-gradient-to-br from-green-50'
            : 'border-amber-200 bg-gradient-to-br from-amber-50 to-orange-50'
        )}
      >
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              {status.hasFounderBadge && <Sparkles className="h-5 w-5 text-amber-500" />}
              <CardTitle className="text-lg">Programa Early Bird</CardTitle>
            </div>
            {status.hasFounderBadge && (
              <Badge variant="secondary" className="bg-amber-100 text-amber-700">
                <Star className="mr-1 h-3 w-3 fill-current" />
                Miembro Fundador
              </Badge>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {status.isInFreePeriod ? (
            <>
              <div className="flex items-center gap-3 rounded-lg bg-green-100/50 p-4">
                <div className="rounded-full bg-green-500 p-2">
                  <CheckCircle className="h-5 w-5 text-white" />
                </div>
                <div>
                  <p className="font-medium text-green-800">Período gratuito activo</p>
                  <p className="text-sm text-green-600">{status.message}</p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-card/50 rounded-lg p-3">
                  <p className="text-muted-foreground text-xs">Días restantes</p>
                  <p className="text-2xl font-bold text-green-600">{status.remainingFreeDays}</p>
                </div>
                <div className="bg-card/50 rounded-lg p-3">
                  <p className="text-muted-foreground text-xs">Gratis hasta</p>
                  <p className="text-sm font-medium">
                    {status.freeUntil ? userBillingService.formatDate(status.freeUntil) : 'N/A'}
                  </p>
                </div>
              </div>
            </>
          ) : (
            <div className="flex items-center gap-3 rounded-lg bg-amber-100/50 p-4">
              <div className="rounded-full bg-amber-500 p-2">
                <Star className="h-5 w-5 text-white" />
              </div>
              <div>
                <p className="font-medium text-amber-800">Período gratuito finalizado</p>
                <p className="text-sm text-amber-600">{status.message}</p>
              </div>
            </div>
          )}
          {status.enrolledAt && (
            <div className="text-muted-foreground flex items-center gap-2 text-sm">
              <Calendar className="h-4 w-4" />
              <span>Inscrito el {userBillingService.formatDate(status.enrolledAt)}</span>
            </div>
          )}
        </CardContent>
      </Card>
    );
  }

  // Not enrolled - show enrollment option
  return (
    <Card className="border-2 border-dashed border-amber-300 bg-gradient-to-br from-amber-50 to-orange-50">
      <CardHeader>
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 text-amber-600" />
          <CardTitle className="text-lg">Programa Early Bird</CardTitle>
        </div>
        <CardDescription>¡Inscríbete ahora y obtén beneficios exclusivos!</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="bg-card/50 rounded-lg p-4">
          <h4 className="font-medium text-amber-800">Beneficios incluidos:</h4>
          <ul className="mt-2 space-y-2 text-sm text-amber-700">
            <li className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4 text-green-500" />3 meses de servicio gratis
            </li>
            <li className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4 text-green-500" />
              Badge exclusivo de Miembro Fundador
            </li>
            <li className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4 text-green-500" />
              Acceso prioritario a nuevas funciones
            </li>
          </ul>
        </div>
        <Button
          onClick={onEnroll}
          disabled={isEnrolling}
          className="w-full bg-amber-600 hover:bg-amber-700"
        >
          {isEnrolling ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Inscribiendo...
            </>
          ) : (
            <>
              <Sparkles className="mr-2 h-4 w-4" />
              Inscribirme ahora
            </>
          )}
        </Button>
      </CardContent>
    </Card>
  );
}

// ============================================================================
// PAYMENT METHOD CARD COMPONENT
// ============================================================================

function PaymentMethodCard({
  method,
  onSetDefault,
  onDelete,
  isSettingDefault,
}: {
  method: PaymentMethodInfo;
  onSetDefault: () => void;
  onDelete: () => void;
  isSettingDefault: boolean;
}) {
  const [showDeleteConfirm, setShowDeleteConfirm] = React.useState(false);

  return (
    <>
      <div
        className={cn(
          'relative rounded-xl border-2 p-4 transition-all',
          method.isExpired
            ? 'border-red-300 bg-red-50/50'
            : method.expiresSoon
              ? 'border-amber-300 bg-amber-50/50'
              : method.isDefault
                ? 'border-blue-500 bg-blue-50/50'
                : 'border-border hover:border-border'
        )}
      >
        {/* Status badges */}
        <div className="absolute -top-2 right-4 flex gap-2">
          {method.isExpired && <Badge className="bg-red-600">Expirada</Badge>}
          {method.expiresSoon && !method.isExpired && (
            <Badge className="bg-amber-500">Expira pronto</Badge>
          )}
          {method.isDefault && !method.isExpired && (
            <Badge className="bg-blue-600">Predeterminada</Badge>
          )}
        </div>

        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div
              className={cn(
                'rounded-lg p-2.5',
                method.isExpired
                  ? 'bg-red-100'
                  : method.expiresSoon
                    ? 'bg-amber-100'
                    : method.isDefault
                      ? 'bg-blue-100'
                      : 'bg-muted'
              )}
            >
              <CreditCard
                className={cn(
                  'h-5 w-5',
                  method.isExpired
                    ? 'text-red-600'
                    : method.expiresSoon
                      ? 'text-amber-600'
                      : method.isDefault
                        ? 'text-blue-600'
                        : 'text-muted-foreground'
                )}
              />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-semibold">{method.card?.brand || 'Card'}</span>
                <span className="text-muted-foreground">•••• {method.card?.last4}</span>
                <Badge variant="outline" className="text-xs">
                  {method.gateway}
                </Badge>
              </div>
              <p
                className={cn(
                  'text-sm',
                  method.isExpired ? 'font-medium text-red-600' : 'text-muted-foreground'
                )}
              >
                {method.isExpired ? 'Expiró' : 'Vence'}{' '}
                {method.card?.expMonth.toString().padStart(2, '0')}/
                {method.card?.expYear.toString().slice(-2)}
              </p>
              {method.usageCount > 0 && (
                <p className="text-muted-foreground text-xs">
                  Usada {method.usageCount} {method.usageCount === 1 ? 'vez' : 'veces'}
                </p>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            {!method.isDefault && !method.isExpired && (
              <Button variant="ghost" size="sm" onClick={onSetDefault} disabled={isSettingDefault}>
                {isSettingDefault ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  'Hacer predeterminada'
                )}
              </Button>
            )}
            <Button
              variant="ghost"
              size="icon"
              className="text-red-500 hover:bg-red-50 hover:text-red-600"
              onClick={() => setShowDeleteConfirm(true)}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Eliminar método de pago?</AlertDialogTitle>
            <AlertDialogDescription>
              Se eliminará la tarjeta {method.card?.brand} terminada en {method.card?.last4}. Esta
              acción no se puede deshacer.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-red-600 hover:bg-red-700"
              onClick={() => {
                onDelete();
                setShowDeleteConfirm(false);
              }}
            >
              Eliminar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

// ============================================================================
// ADD PAYMENT METHOD DIALOG COMPONENT
// Handles provider-specific integrations: redirect, iframe, or SDK
// ============================================================================

type IntegrationStep = 'select' | 'loading' | 'iframe' | 'sdk';

function AddPaymentMethodDialog({
  open,
  onOpenChange,
  onSuccess,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}) {
  const [selectedGateway, setSelectedGateway] = React.useState<PaymentGateway>('Azul');
  const [setAsDefault, setSetAsDefault] = React.useState(true);
  const [, setIsLoading] = React.useState(false);
  const [error, setError] = React.useState('');
  const [step, setStep] = React.useState<IntegrationStep>('select');
  const [tokenizationResponse, setTokenizationResponse] =
    React.useState<TokenizationInitResponse | null>(null);
  const iframeRef = React.useRef<HTMLIFrameElement>(null);

  // Gateway info with integration types
  const gatewayInfo: Record<
    PaymentGateway,
    {
      name: string;
      description: string;
      note: string;
      integrationType: 'redirect' | 'iframe' | 'sdk';
      icon: string;
    }
  > = {
    Azul: {
      name: 'Azul (Banco Popular)',
      description: 'Página segura de Azul WebPay con Cybersource.',
      note: 'Acepta Visa, Mastercard, Discover y tarjetas dominicanas.',
      integrationType: 'redirect',
      icon: '🏦',
    },
    CardNET: {
      name: 'CardNET',
      description: 'Página segura de CardNET para tarjetas dominicanas.',
      note: 'Red de pagos Visa/MasterCard local.',
      integrationType: 'redirect',
      icon: '💳',
    },
    PixelPay: {
      name: 'PixelPay',
      description: 'Checkout embebido seguro de PixelPay.',
      note: 'Fintech regional con soporte para RD.',
      integrationType: 'iframe',
      icon: '📱',
    },
    Fygaro: {
      name: 'Fygaro',
      description: 'Hosted checkout de Fygaro.',
      note: 'Acepta crédito y débito local.',
      integrationType: 'redirect',
      icon: '🔐',
    },
    PayPal: {
      name: 'PayPal / Braintree',
      description: 'Vault API con Braintree SDK.',
      note: 'Tarjetas internacionales y cuenta PayPal.',
      integrationType: 'sdk',
      icon: '🌐',
    },
    Stripe: {
      name: 'Stripe',
      description: 'Checkout seguro con Stripe Elements.',
      note: 'Apple Pay, Google Pay y tarjetas internacionales.',
      integrationType: 'sdk',
      icon: '⚡',
    },
  };

  // Reset state when dialog closes
  React.useEffect(() => {
    if (!open) {
      setStep('select');
      setTokenizationResponse(null);
      setError('');
      setIsLoading(false);
    }
  }, [open]);

  // Listen for iframe messages (for PixelPay and similar)
  React.useEffect(() => {
    // Orígenes permitidos por proveedor de pago
    const ALLOWED_ORIGINS: Record<PaymentGateway, string[]> = {
      Azul: ['https://secure.azul.com.do', 'https://pruebas.azul.com.do'],
      CardNET: ['https://cardnet.com.do', 'https://sandbox.cardnet.com.do'],
      PixelPay: ['https://pixel-pay.com', 'https://sandbox.pixel-pay.com'],
      Fygaro: ['https://fygaro.com', 'https://sandbox.fygaro.com'],
      PayPal: ['https://www.paypal.com', 'https://www.sandbox.paypal.com'],
      Stripe: ['https://js.stripe.com', 'https://m.stripe.com'],
    };

    const handleMessage = async (event: MessageEvent) => {
      // SEGURIDAD: Validar origen del mensaje
      const allowedOrigins = ALLOWED_ORIGINS[selectedGateway] || [];
      // En desarrollo permitir localhost, en producción solo orígenes de proveedores
      const isDev = process.env.NODE_ENV === 'development';
      const isLocalhost = event.origin.includes('localhost') || event.origin.includes('127.0.0.1');

      if (!isDev && !isLocalhost && !allowedOrigins.includes(event.origin)) {
        console.warn(`[Security] Blocked postMessage from unauthorized origin: ${event.origin}`);
        return;
      }

      if (event.data?.type === 'TOKENIZATION_COMPLETE') {
        const { token, cardBrand, cardLast4, expMonth, expYear, cardHolderName } = event.data;

        if (tokenizationResponse?.sessionId) {
          try {
            await userBillingService.completeTokenization({
              sessionId: tokenizationResponse.sessionId,
              providerToken: token,
              gateway: selectedGateway,
              setAsDefault,
              providerResponse: {
                cardBrand,
                cardLast4,
                expMonth: String(expMonth),
                expYear: String(expYear),
                cardHolderName,
              },
            });

            toast.success('Tarjeta agregada exitosamente');
            onSuccess?.();
            onOpenChange(false);
          } catch (err) {
            console.error('Error completing tokenization:', err);
            setError('Error al guardar la tarjeta');
            setStep('select');
          }
        }
      } else if (event.data?.type === 'TOKENIZATION_CANCELLED') {
        setStep('select');
        setError('Operación cancelada');
      } else if (event.data?.type === 'TOKENIZATION_ERROR') {
        setStep('select');
        setError(event.data.message || 'Error en el proceso de tokenización');
      }
    };

    window.addEventListener('message', handleMessage);
    return () => window.removeEventListener('message', handleMessage);
  }, [tokenizationResponse, selectedGateway, setAsDefault, onSuccess, onOpenChange]);

  const handleContinue = async () => {
    setError('');
    setIsLoading(true);
    setStep('loading');

    try {
      const response = await userBillingService.initiateTokenization({
        gateway: selectedGateway,
        returnUrl: `${window.location.origin}/cuenta/pagos/callback`,
        cancelUrl: `${window.location.origin}/cuenta/pagos`,
        setAsDefault,
      });

      setTokenizationResponse(response);

      // Handle based on integration type
      switch (response.integrationType) {
        case 'redirect':
          // Redirect to provider's page
          if (response.tokenizationUrl) {
            // SEGURIDAD: Usar sessionStorage en lugar de localStorage
            // sessionStorage se borra al cerrar la pestaña, reduciendo riesgo de XSS
            sessionStorage.setItem(
              'tokenization_session',
              JSON.stringify({
                sessionId: response.sessionId,
                gateway: selectedGateway,
                setAsDefault,
              })
            );
            window.location.href = response.tokenizationUrl;
          } else {
            throw new Error('No se recibió URL de tokenización');
          }
          break;

        case 'iframe':
          // Show iframe
          setStep('iframe');
          setIsLoading(false);
          break;

        case 'sdk':
          // Load SDK
          setStep('sdk');
          setIsLoading(false);
          // SDK loading will be handled by the SDK component
          break;

        default:
          throw new Error(`Tipo de integración no soportado: ${response.integrationType}`);
      }
    } catch (err: unknown) {
      const error = err as Error & { response?: { data?: { message?: string } } };
      const message =
        error?.response?.data?.message || error.message || 'Error al iniciar el proceso';
      setError(message);
      toast.error(message);
      setStep('select');
      setIsLoading(false);
    }
  };

  // Render gateway selection
  const renderGatewaySelection = () => (
    <>
      {/* Security Notice */}
      <div className="flex items-start gap-3 rounded-lg border border-blue-200 bg-blue-50 p-4">
        <Shield className="mt-0.5 h-5 w-5 flex-shrink-0 text-blue-600" />
        <div className="text-sm">
          <p className="font-medium text-blue-900">Proceso 100% Seguro (PCI DSS)</p>
          <p className="text-blue-700">
            Tus datos de tarjeta son procesados directamente por la pasarela que selecciones. Nunca
            pasan por nuestros servidores.
          </p>
        </div>
      </div>

      {/* Gateway Selection */}
      <div className="space-y-3">
        <Label>Selecciona tu pasarela de pago</Label>
        <div className="max-h-[300px] space-y-2 overflow-y-auto">
          {availableGateways.map(gateway => {
            const info = gatewayInfo[gateway.id];
            return (
              <label
                key={gateway.id}
                className={cn(
                  'relative flex cursor-pointer items-center rounded-xl border-2 p-4 transition-all',
                  selectedGateway === gateway.id
                    ? 'border-blue-500 bg-blue-50'
                    : 'border-border hover:border-border'
                )}
              >
                <input
                  type="radio"
                  name="gateway"
                  value={gateway.id}
                  checked={selectedGateway === gateway.id}
                  onChange={e => setSelectedGateway(e.target.value as PaymentGateway)}
                  className="sr-only"
                />
                <div className="flex flex-1 items-center gap-3">
                  <div
                    className={cn(
                      'flex h-12 w-12 items-center justify-center rounded-lg text-2xl',
                      selectedGateway === gateway.id ? 'bg-blue-500' : 'bg-muted'
                    )}
                  >
                    {info.icon}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium">{info.name}</span>
                      {gateway.recommended && (
                        <Badge className="bg-green-100 text-xs text-green-800 hover:bg-green-100">
                          Recomendado
                        </Badge>
                      )}
                      <Badge variant="outline" className="text-xs">
                        {info.integrationType === 'redirect' && 'Página segura'}
                        {info.integrationType === 'iframe' && 'Checkout embebido'}
                        {info.integrationType === 'sdk' && 'SDK integrado'}
                      </Badge>
                    </div>
                    <span className="text-muted-foreground block truncate text-sm">
                      {info.description}
                    </span>
                    <span className="text-muted-foreground text-xs">{info.note}</span>
                  </div>
                </div>
                {selectedGateway === gateway.id && (
                  <Check className="h-5 w-5 flex-shrink-0 text-blue-500" />
                )}
              </label>
            );
          })}
        </div>
      </div>

      {/* Set as Default Option */}
      <div className="flex items-center space-x-2">
        <Checkbox
          id="setAsDefault"
          checked={setAsDefault}
          onCheckedChange={checked => setSetAsDefault(checked === true)}
        />
        <label htmlFor="setAsDefault" className="text-sm leading-none font-medium">
          Establecer como método de pago predeterminado
        </label>
      </div>

      <DialogFooter className="flex-col gap-3 sm:flex-row">
        <Button
          type="button"
          variant="outline"
          className="w-full sm:w-auto"
          onClick={() => onOpenChange(false)}
        >
          Cancelar
        </Button>
        <Button type="button" className="w-full sm:w-auto" onClick={handleContinue}>
          <ExternalLink className="mr-2 h-4 w-4" />
          Continuar a {gatewayInfo[selectedGateway].name}
        </Button>
      </DialogFooter>
    </>
  );

  // Render iframe integration (for PixelPay)
  const renderIframe = () => (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-medium">Ingresa los datos de tu tarjeta</h3>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            setStep('select');
            setTokenizationResponse(null);
          }}
        >
          ← Volver
        </Button>
      </div>

      <div className="bg-card rounded-lg border p-1">
        <iframe
          ref={iframeRef}
          src={tokenizationResponse?.iframeUrl}
          className="h-[400px] w-full border-0"
          title="Checkout seguro"
          sandbox="allow-scripts allow-forms allow-same-origin allow-popups"
        />
      </div>

      <p className="text-muted-foreground text-center text-xs">
        <Shield className="mr-1 inline h-3 w-3" />
        Conexión segura con {gatewayInfo[selectedGateway].name}
      </p>
    </div>
  );

  // Render SDK integration (for PayPal/Braintree)
  const renderSdk = () => (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-medium">PayPal / Tarjeta de crédito</h3>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            setStep('select');
            setTokenizationResponse(null);
          }}
        >
          ← Volver
        </Button>
      </div>

      {/* PayPal Buttons Container */}
      <div className="space-y-4">
        <div className="bg-muted/50 rounded-lg border p-4">
          <p className="text-muted-foreground mb-4 text-sm">
            Puedes vincular tu cuenta de PayPal o agregar una tarjeta de crédito/débito.
          </p>

          {/* PayPal SDK Container */}
          <div
            id="paypal-button-container"
            className="flex min-h-[150px] items-center justify-center"
          >
            {tokenizationResponse?.sdkConfig ? (
              <div className="text-center">
                <div className="mb-4 animate-pulse rounded-lg bg-blue-100 p-6">
                  <p className="text-sm text-blue-700">Cargando opciones de pago de PayPal...</p>
                </div>
                <p className="text-muted-foreground text-xs">
                  Ambiente: {tokenizationResponse.sdkConfig.environment}
                </p>
              </div>
            ) : (
              <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
            )}
          </div>
        </div>

        {/* Manual Card Entry Option */}
        <div className="relative">
          <div className="absolute inset-0 flex items-center">
            <span className="border-border w-full border-t" />
          </div>
          <div className="relative flex justify-center text-xs uppercase">
            <span className="text-muted-foreground bg-white px-2">O ingresa tu tarjeta</span>
          </div>
        </div>

        {/* Braintree Hosted Fields Container */}
        <div id="braintree-hosted-fields" className="space-y-3">
          <div className="bg-card rounded-lg border p-3">
            <Label className="text-muted-foreground text-xs">Número de tarjeta</Label>
            <div id="card-number" className="border-border h-10 border-b" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="bg-card rounded-lg border p-3">
              <Label className="text-muted-foreground text-xs">Fecha de vencimiento</Label>
              <div id="expiration-date" className="border-border h-10 border-b" />
            </div>
            <div className="bg-card rounded-lg border p-3">
              <Label className="text-muted-foreground text-xs">CVV</Label>
              <div id="cvv" className="border-border h-10 border-b" />
            </div>
          </div>
        </div>

        <Button className="w-full" disabled>
          <CreditCard className="mr-2 h-4 w-4" />
          Guardar tarjeta
        </Button>

        <p className="text-muted-foreground text-center text-xs">
          <Shield className="mr-1 inline h-3 w-3" />
          Procesado de forma segura por PayPal/Braintree
        </p>
      </div>
    </div>
  );

  // Render loading state
  const renderLoading = () => (
    <div className="flex flex-col items-center justify-center py-12">
      <Loader2 className="mb-4 h-12 w-12 animate-spin text-blue-500" />
      <p className="text-lg font-medium">Conectando con {gatewayInfo[selectedGateway].name}...</p>
      <p className="text-muted-foreground mt-2 text-sm">
        {gatewayInfo[selectedGateway].integrationType === 'redirect'
          ? 'Serás redirigido en un momento'
          : 'Cargando formulario seguro'}
      </p>
    </div>
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={cn('max-w-lg', step === 'iframe' && 'max-w-xl', step === 'sdk' && 'max-w-md')}
      >
        {step === 'select' && (
          <>
            <DialogHeader>
              <DialogTitle>Agregar Método de Pago</DialogTitle>
              <DialogDescription>
                Selecciona cómo deseas agregar tu tarjeta. El proceso es 100% seguro.
              </DialogDescription>
            </DialogHeader>

            {error && (
              <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                <AlertCircle className="h-4 w-4 flex-shrink-0" />
                {error}
              </div>
            )}

            {renderGatewaySelection()}
          </>
        )}

        {step === 'loading' && renderLoading()}

        {step === 'iframe' && renderIframe()}

        {step === 'sdk' && renderSdk()}
      </DialogContent>
    </Dialog>
  );
}

// ============================================================================
// MAIN PAGE COMPONENT
// ============================================================================

export default function PaymentsPage() {
  const queryClient = useQueryClient();
  const [showAddPaymentMethod, setShowAddPaymentMethod] = React.useState(false);
  const [settingDefaultId, setSettingDefaultId] = React.useState<string | null>(null);

  // Fetch billing summary (used for Early Bird status check)
  const {
    data: summary,
    isLoading: isLoadingSummary,
    error: summaryError,
  } = useQuery({
    queryKey: ['user-billing-summary'],
    queryFn: () => userBillingService.getBillingSummary(),
  });

  // Fetch Early Bird status
  const { data: earlyBirdStatus } = useQuery({
    queryKey: ['early-bird-status'],
    queryFn: () => userBillingService.getEarlyBirdStatus(),
  });

  // Fetch payment methods
  const {
    data: paymentMethods,
    isLoading: isLoadingPaymentMethods,
    refetch: refetchPaymentMethods,
  } = useQuery({
    queryKey: ['payment-methods'],
    queryFn: () => userBillingService.getPaymentMethods(),
  });

  // Enroll in Early Bird mutation
  const enrollMutation = useMutation({
    mutationFn: () => userBillingService.enrollEarlyBird(3),
    onSuccess: () => {
      toast.success('¡Te has inscrito exitosamente en el programa Early Bird!');
      queryClient.invalidateQueries({ queryKey: ['early-bird-status'] });
      queryClient.invalidateQueries({ queryKey: ['user-billing-summary'] });
    },
    onError: (error: Error & { response?: { data?: { error?: string } } }) => {
      toast.error(error?.response?.data?.error || 'Error al inscribirse en Early Bird');
    },
  });

  // Set default payment method mutation
  const setDefaultMutation = useMutation({
    mutationFn: (paymentMethodId: string) =>
      userBillingService.setDefaultPaymentMethod(paymentMethodId),
    onSuccess: () => {
      toast.success('Método de pago predeterminado actualizado');
      refetchPaymentMethods();
      setSettingDefaultId(null);
    },
    onError: (error: Error & { response?: { data?: { error?: string } } }) => {
      toast.error(error?.response?.data?.error || 'Error al actualizar método de pago');
      setSettingDefaultId(null);
    },
  });

  // Remove payment method mutation
  const removeMutation = useMutation({
    mutationFn: (paymentMethodId: string) =>
      userBillingService.removePaymentMethod(paymentMethodId),
    onSuccess: () => {
      toast.success('Método de pago eliminado');
      refetchPaymentMethods();
    },
    onError: (error: Error & { response?: { data?: { error?: string } } }) => {
      toast.error(error?.response?.data?.error || 'Error al eliminar método de pago');
    },
  });

  // Loading state
  if (isLoadingSummary) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Pagos</h1>
          <p className="text-muted-foreground">
            Administra tus métodos de pago y programa Early Bird
          </p>
        </div>
        <Card>
          <CardContent className="pt-6">
            <Skeleton className="h-64 w-full" />
          </CardContent>
        </Card>
      </div>
    );
  }

  // Error state
  if (summaryError) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <h2 className="mt-4 text-lg font-semibold">Error al cargar los datos</h2>
          <p className="text-muted-foreground mt-2">
            No pudimos cargar tu información de pagos. Por favor, intenta de nuevo.
          </p>
          <Button onClick={() => window.location.reload()} className="mt-4">
            Reintentar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold">Pagos</h1>
        <p className="text-muted-foreground">
          Administra tus métodos de pago y programa Early Bird
        </p>
      </div>

      {/* Early Bird Section */}
      <EarlyBirdCard
        status={earlyBirdStatus ?? null}
        onEnroll={() => enrollMutation.mutate()}
        isEnrolling={enrollMutation.isPending}
      />

      {/* Payment Methods Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                Métodos de Pago
              </CardTitle>
              <CardDescription>Administra tus tarjetas guardadas</CardDescription>
            </div>
            <Button onClick={() => setShowAddPaymentMethod(true)}>
              <Plus className="mr-2 h-4 w-4" />
              Agregar Tarjeta
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoadingPaymentMethods ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
            </div>
          ) : paymentMethods && paymentMethods.length > 0 ? (
            <div className="space-y-3">
              {paymentMethods.map(method => (
                <PaymentMethodCard
                  key={method.id}
                  method={method}
                  onSetDefault={() => {
                    setSettingDefaultId(method.id);
                    setDefaultMutation.mutate(method.id);
                  }}
                  onDelete={() => removeMutation.mutate(method.id)}
                  isSettingDefault={settingDefaultId === method.id && setDefaultMutation.isPending}
                />
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <CreditCard className="h-12 w-12 text-gray-300" />
              <h3 className="text-foreground mt-4 text-lg font-medium">
                No tienes métodos de pago guardados
              </h3>
              <p className="text-muted-foreground mt-2 text-sm">
                Agrega una tarjeta para agilizar tus compras futuras.
              </p>
              <Button className="mt-4" onClick={() => setShowAddPaymentMethod(true)}>
                <Plus className="mr-2 h-4 w-4" />
                Agregar Tarjeta
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Add Payment Method Dialog */}
      <AddPaymentMethodDialog
        open={showAddPaymentMethod}
        onOpenChange={setShowAddPaymentMethod}
        onSuccess={() => refetchPaymentMethods()}
      />

      {/* Link to transaction history */}
      <Card className="border-dashed">
        <CardContent className="flex items-center justify-between py-4">
          <div className="flex items-center gap-3">
            <Receipt className="h-5 w-5 text-blue-600" />
            <div>
              <p className="font-medium">Ver historial de transacciones</p>
              <p className="text-muted-foreground text-sm">
                Consulta todas tus compras y pagos realizados
              </p>
            </div>
          </div>
          <Button variant="outline" size="sm" asChild>
            <a href="/cuenta/historial">
              Ver historial
              <ChevronRight className="ml-1 h-4 w-4" />
            </a>
          </Button>
        </CardContent>
      </Card>

      {/* Help Section */}
      <Card className="border-blue-100 bg-blue-50/50">
        <CardContent className="flex items-center justify-between py-4">
          <div className="flex items-center gap-3">
            <AlertCircle className="h-5 w-5 text-blue-600" />
            <div>
              <p className="font-medium text-blue-900">¿Necesitas ayuda con un pago?</p>
              <p className="text-sm text-blue-700">
                Nuestro equipo de soporte está disponible para ayudarte.
              </p>
            </div>
          </div>
          <Button variant="outline" size="sm" className="border-blue-300 text-blue-700">
            Contactar soporte
            <ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
