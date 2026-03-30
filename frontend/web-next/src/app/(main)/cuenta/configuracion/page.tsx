/**
 * Settings Page
 *
 * General app settings and preferences
 * Connects to:
 * - UserService (PUT /api/users/me) for language & currency
 * - PrivacyController (GET/PUT /api/privacy/preferences) for notifications
 * - localStorage for theme (not stored in backend)
 */

'use client';

import * as React from 'react';
import {
  Settings,
  Bell,
  Globe,
  Moon,
  Sun,
  Monitor,
  Mail,
  MessageCircle,
  DollarSign,
  Eye,
  Loader2,
  Check,
  AlertCircle,
  Trash2,
  AlertTriangle,
  ShieldAlert,
  Download,
  Smartphone,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import {
  settingsService,
  type AppSettings,
  type NotificationSettings,
  type Theme,
} from '@/services/settings';
import {
  requestAccountDeletion,
  confirmAccountDeletion,
  cancelAccountDeletion,
  type DeletionReasonString,
  DeletionReasonMap,
} from '@/services/auth';
import { useAuth } from '@/hooks/use-auth';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

type ThemeOption = Theme;

function ThemeButton({
  theme,
  currentTheme,
  onClick,
  icon: Icon,
  label,
}: {
  theme: ThemeOption;
  currentTheme: ThemeOption;
  onClick: () => void;
  icon: React.ElementType;
  label: string;
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'flex flex-col items-center gap-2 rounded-lg border-2 p-4 transition-all',
        currentTheme === theme
          ? 'border-primary bg-primary/5'
          : 'border-border hover:border-border dark:border-gray-700 dark:hover:border-gray-600'
      )}
    >
      <Icon
        className={cn(
          'h-6 w-6',
          currentTheme === theme
            ? 'text-primary'
            : 'text-muted-foreground dark:text-muted-foreground'
        )}
      />
      <span
        className={cn(
          'text-sm font-medium',
          currentTheme === theme ? 'text-primary' : 'text-foreground dark:text-gray-300'
        )}
      >
        {label}
      </span>
    </button>
  );
}

function NotificationToggle({
  label,
  description,
  checked,
  onCheckedChange,
  icon: Icon,
}: {
  label: string;
  description: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  icon: React.ElementType;
}) {
  return (
    <div className="flex items-start gap-4 py-3">
      <div className="bg-muted text-muted-foreground dark:text-muted-foreground flex h-10 w-10 items-center justify-center rounded-lg dark:bg-gray-800">
        <Icon className="h-5 w-5" />
      </div>
      <div className="flex-1">
        <div className="flex items-center justify-between">
          <Label className="cursor-pointer font-medium">{label}</Label>
          <Switch checked={checked} onCheckedChange={onCheckedChange} />
        </div>
        <p className="text-muted-foreground dark:text-muted-foreground mt-0.5 text-sm">
          {description}
        </p>
      </div>
    </div>
  );
}

export default function SettingsPage() {
  const { isAuthenticated, isLoading: authLoading, logout } = useAuth();
  const router = useRouter();
  const [isLoading, setIsLoading] = React.useState(true);
  const [isSaving, setIsSaving] = React.useState(false);
  const [saveSuccess, setSaveSuccess] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [hasChanges, setHasChanges] = React.useState(false);

  const [appSettings, setAppSettings] = React.useState<AppSettings>(
    settingsService.DEFAULT_APP_SETTINGS
  );

  const [notifications, setNotifications] = React.useState<NotificationSettings>(
    settingsService.DEFAULT_NOTIFICATIONS
  );

  // Store original values to detect changes
  const [originalApp, setOriginalApp] = React.useState<AppSettings | null>(null);
  const [originalNotifications, setOriginalNotifications] =
    React.useState<NotificationSettings | null>(null);

  // ── Account Deletion State ──────────────────────────────────────────────────
  type DeletionStep = 'idle' | 'selecting' | 'confirming' | 'success';
  const [deletionStep, setDeletionStep] = React.useState<DeletionStep>('idle');
  const [deletionReason, setDeletionReason] =
    React.useState<DeletionReasonString>('NoLongerNeeded');
  const [deletionFeedback, setDeletionFeedback] = React.useState('');
  const [deletionOtherReason, setDeletionOtherReason] = React.useState('');
  const [confirmationCode, setConfirmationCode] = React.useState('');
  const [deletionPassword, setDeletionPassword] = React.useState('');
  const [_deletionRequestId, setDeletionRequestId] = React.useState('');
  const [gracePeriodEndsAt, setGracePeriodEndsAt] = React.useState('');
  const [isDeletionLoading, setIsDeletionLoading] = React.useState(false);
  const [deletionError, setDeletionError] = React.useState<string | null>(null);
  const [isCancellingDeletion, setIsCancellingDeletion] = React.useState(false);

  // Load settings on mount
  React.useEffect(() => {
    async function loadSettings() {
      setIsLoading(true);
      setError(null);
      try {
        const settings = await settingsService.getAllSettings();
        setAppSettings(settings.app);
        setNotifications(settings.notifications);
        setOriginalApp(settings.app);
        setOriginalNotifications(settings.notifications);
      } catch (err) {
        if (process.env.NODE_ENV === 'development') console.error('Error loading settings:', err);
        setError('No se pudieron cargar las configuraciones. Intenta de nuevo.');
      } finally {
        setIsLoading(false);
      }
    }

    // Apply saved theme immediately
    settingsService.applyTheme(settingsService.getSavedTheme());

    if (!authLoading) {
      loadSettings();
    }
  }, [authLoading]);

  // Detect changes
  React.useEffect(() => {
    if (!originalApp || !originalNotifications) return;
    const appChanged = JSON.stringify(appSettings) !== JSON.stringify(originalApp);
    const notificationsChanged =
      JSON.stringify(notifications) !== JSON.stringify(originalNotifications);
    setHasChanges(appChanged || notificationsChanged);
  }, [appSettings, notifications, originalApp, originalNotifications]);

  const handleSave = async () => {
    setIsSaving(true);
    setError(null);
    try {
      const updated = await settingsService.saveAllSettings(appSettings, notifications);
      setAppSettings(updated.app);
      setNotifications(updated.notifications);
      setOriginalApp(updated.app);
      setOriginalNotifications(updated.notifications);
      setHasChanges(false);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err) {
      if (process.env.NODE_ENV === 'development') console.error('Error saving settings:', err);
      setError('No se pudieron guardar los cambios. Intenta de nuevo.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleThemeChange = (theme: ThemeOption) => {
    setAppSettings(prev => ({ ...prev, theme }));
    // Save theme to localStorage AND apply it immediately for visual feedback
    settingsService.saveTheme(theme);
  };

  const updateEmailNotification = (key: keyof NotificationSettings['email'], value: boolean) => {
    setNotifications(prev => ({
      ...prev,
      email: { ...prev.email, [key]: value },
    }));
  };

  const updatePushNotification = (key: keyof NotificationSettings['push'], value: boolean) => {
    setNotifications(prev => ({
      ...prev,
      push: { ...prev.push, [key]: value },
    }));
  };

  const updateWhatsAppNotification = (
    key: keyof NotificationSettings['whatsapp'],
    value: boolean
  ) => {
    setNotifications(prev => ({
      ...prev,
      whatsapp: { ...prev.whatsapp, [key]: value },
    }));
  };

  // ── Account Deletion Handlers ───────────────────────────────────────────────

  const handleRequestDeletion = async () => {
    setIsDeletionLoading(true);
    setDeletionError(null);
    try {
      const response = await requestAccountDeletion({
        reason: deletionReason,
        otherReason: deletionReason === 'Other' ? deletionOtherReason : undefined,
        feedback: deletionFeedback || undefined,
      });
      setDeletionRequestId(response.requestId);
      setGracePeriodEndsAt(response.gracePeriodEndsAt);
      setDeletionStep('confirming');
    } catch (err) {
      const e = err as { message?: string };
      setDeletionError(e.message || 'Error al solicitar eliminación. Intenta de nuevo.');
    } finally {
      setIsDeletionLoading(false);
    }
  };

  const handleConfirmDeletion = async () => {
    if (!confirmationCode || !deletionPassword) {
      setDeletionError('Ingresa el código de confirmación y tu contraseña.');
      return;
    }
    setIsDeletionLoading(true);
    setDeletionError(null);
    try {
      await confirmAccountDeletion({ confirmationCode, password: deletionPassword });
      setDeletionStep('success');
      // Log user out after confirming deletion
      setTimeout(async () => {
        try {
          await logout();
        } catch {
          /* ignore */
        }
        router.push('/');
      }, 5000);
    } catch (err) {
      const e = err as { message?: string };
      setDeletionError(e.message || 'Código inválido o contraseña incorrecta. Intenta de nuevo.');
    } finally {
      setIsDeletionLoading(false);
    }
  };

  const handleCancelDeletion = async () => {
    setIsCancellingDeletion(true);
    try {
      await cancelAccountDeletion();
      setDeletionStep('idle');
      setDeletionRequestId('');
      setGracePeriodEndsAt('');
      setConfirmationCode('');
      setDeletionPassword('');
      setDeletionError(null);
    } catch (err) {
      const e = err as { message?: string };
      setDeletionError(e.message || 'Error al cancelar. Intenta de nuevo.');
    } finally {
      setIsCancellingDeletion(false);
    }
  };

  // Show loading skeleton
  if (authLoading || isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <div className="bg-muted h-8 w-40 animate-pulse rounded dark:bg-gray-700" />
            <div className="bg-muted mt-2 h-4 w-60 animate-pulse rounded dark:bg-gray-700" />
          </div>
        </div>
        <Card>
          <CardContent className="p-6">
            <div className="space-y-4">
              <div className="bg-muted h-6 w-32 animate-pulse rounded dark:bg-gray-700" />
              <div className="grid grid-cols-3 gap-3">
                {[1, 2, 3].map(i => (
                  <div
                    key={i}
                    className="bg-muted h-24 animate-pulse rounded-lg dark:bg-gray-800"
                  />
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold dark:text-gray-100">Configuración</h1>
          <p className="text-muted-foreground dark:text-muted-foreground">
            Personaliza tu experiencia en OKLA
          </p>
        </div>
        <Button onClick={handleSave} disabled={isSaving || !hasChanges}>
          {isSaving ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Guardando...
            </>
          ) : saveSuccess ? (
            <>
              <Check className="mr-2 h-4 w-4" />
              Guardado
            </>
          ) : (
            'Guardar cambios'
          )}
        </Button>
      </div>

      {/* Error Alert */}
      {error && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* Not authenticated notice */}
      {!isAuthenticated && (
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            Inicia sesión para sincronizar tus preferencias en todos tus dispositivos. El tema se
            guardará localmente.
          </AlertDescription>
        </Alert>
      )}

      {/* Appearance */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            Apariencia
          </CardTitle>
          <CardDescription>Personaliza cómo se ve la aplicación</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Theme */}
          <div>
            <Label className="mb-3 block text-sm font-medium">Tema</Label>
            <div className="grid grid-cols-3 gap-3">
              <ThemeButton
                theme="light"
                currentTheme={appSettings.theme}
                onClick={() => handleThemeChange('light')}
                icon={Sun}
                label="Claro"
              />
              <ThemeButton
                theme="dark"
                currentTheme={appSettings.theme}
                onClick={() => handleThemeChange('dark')}
                icon={Moon}
                label="Oscuro"
              />
              <ThemeButton
                theme="system"
                currentTheme={appSettings.theme}
                onClick={() => handleThemeChange('system')}
                icon={Monitor}
                label="Sistema"
              />
            </div>
            <p className="text-muted-foreground dark:text-muted-foreground mt-2 text-xs">
              El tema se guarda localmente en tu navegador.
            </p>
          </div>

          <Separator />

          {/* Language */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="bg-muted text-muted-foreground dark:text-muted-foreground flex h-10 w-10 items-center justify-center rounded-lg dark:bg-gray-800">
                <Globe className="h-5 w-5" />
              </div>
              <div>
                <Label className="font-medium">Idioma</Label>
                <p className="text-muted-foreground dark:text-muted-foreground text-sm">
                  Selecciona tu idioma preferido
                </p>
              </div>
            </div>
            <select
              value={appSettings.language}
              onChange={e =>
                setAppSettings({ ...appSettings, language: e.target.value as 'es' | 'en' })
              }
              className="focus:ring-primary/20 focus:border-primary border-border rounded-lg border px-3 py-2 focus:ring-2 focus:outline-none"
            >
              <option value="es">Español</option>
              <option value="en">English</option>
            </select>
          </div>

          <Separator />

          {/* Currency */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="bg-muted text-muted-foreground dark:text-muted-foreground flex h-10 w-10 items-center justify-center rounded-lg dark:bg-gray-800">
                <DollarSign className="h-5 w-5" />
              </div>
              <div>
                <Label className="font-medium">Moneda</Label>
                <p className="text-muted-foreground dark:text-muted-foreground text-sm">
                  Moneda para mostrar precios
                </p>
              </div>
            </div>
            <select
              value={appSettings.currency}
              onChange={e =>
                setAppSettings({ ...appSettings, currency: e.target.value as 'DOP' | 'USD' })
              }
              className="focus:ring-primary/20 focus:border-primary bg-background text-foreground border-border rounded-lg border px-3 py-2 focus:ring-2 focus:outline-none dark:border-gray-600"
            >
              <option value="DOP">RD$ (Pesos Dominicanos)</option>
              <option value="USD">US$ (Dólares)</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Email Notifications */}
      <Card id="notificaciones">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            Notificaciones por Email
          </CardTitle>
          <CardDescription>Elige qué emails quieres recibir</CardDescription>
        </CardHeader>
        <CardContent className="divide-y divide-gray-100 dark:divide-gray-800">
          <NotificationToggle
            label="Mensajes"
            description="Recibe un email cuando alguien te envíe un mensaje"
            checked={notifications.email.messages}
            onCheckedChange={v => updateEmailNotification('messages', v)}
            icon={MessageCircle}
          />
          <NotificationToggle
            label="Consultas sobre tus vehículos"
            description="Cuando alguien esté interesado en tus publicaciones"
            checked={notifications.email.inquiries}
            onCheckedChange={v => updateEmailNotification('inquiries', v)}
            icon={Eye}
          />
          <NotificationToggle
            label="Alertas de precio"
            description="Cuando un vehículo en favoritos baje de precio"
            checked={notifications.email.priceAlerts}
            onCheckedChange={v => updateEmailNotification('priceAlerts', v)}
            icon={DollarSign}
          />
          <NotificationToggle
            label="Nuevas publicaciones"
            description="Basado en tus búsquedas guardadas"
            checked={notifications.email.newListings}
            onCheckedChange={v => updateEmailNotification('newListings', v)}
            icon={Bell}
          />
          <NotificationToggle
            label="Ofertas y promociones"
            description="Información sobre ofertas exclusivas de OKLA"
            checked={notifications.email.marketing}
            onCheckedChange={v => updateEmailNotification('marketing', v)}
            icon={Mail}
          />
        </CardContent>
      </Card>

      {/* Push Notifications */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bell className="h-5 w-5" />
            Notificaciones Push
          </CardTitle>
          <CardDescription>Notificaciones en tiempo real en tu dispositivo</CardDescription>
        </CardHeader>
        <CardContent className="divide-y divide-gray-100 dark:divide-gray-800">
          <NotificationToggle
            label="Mensajes"
            description="Notificación inmediata de nuevos mensajes"
            checked={notifications.push.messages}
            onCheckedChange={v => updatePushNotification('messages', v)}
            icon={MessageCircle}
          />
          <NotificationToggle
            label="Cambios de precio en favoritos"
            description="Cuando un vehículo guardado baje de precio"
            checked={notifications.push.priceChanges}
            onCheckedChange={v => updatePushNotification('priceChanges', v)}
            icon={DollarSign}
          />
          <NotificationToggle
            label="Recomendaciones personalizadas"
            description="Vehículos que podrían interesarte según tus búsquedas"
            checked={notifications.push.recommendations}
            onCheckedChange={v => updatePushNotification('recommendations', v)}
            icon={Eye}
          />
        </CardContent>
      </Card>

      {/* WhatsApp Notifications */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Smartphone className="h-5 w-5" />
            Notificaciones por WhatsApp
          </CardTitle>
          <CardDescription>
            Controla qué mensajes recibes por WhatsApp. Las notificaciones transaccionales
            (confirmaciones, leads) siempre están activas.
          </CardDescription>
        </CardHeader>
        <CardContent className="divide-y divide-gray-100 dark:divide-gray-800">
          <div className="flex items-start gap-4 py-3">
            <div className="bg-muted text-muted-foreground dark:text-muted-foreground flex h-10 w-10 items-center justify-center rounded-lg dark:bg-gray-800">
              <MessageCircle className="h-5 w-5" />
            </div>
            <div className="flex-1">
              <div className="flex items-center justify-between">
                <Label className="font-medium">Transaccionales</Label>
                <Switch checked={true} disabled />
              </div>
              <p className="text-muted-foreground dark:text-muted-foreground mt-0.5 text-sm">
                Confirmaciones de leads, estado de publicaciones y verificaciones (obligatorio)
              </p>
            </div>
          </div>
          <NotificationToggle
            label="Ofertas y promociones"
            description="Información sobre ofertas exclusivas de OKLA por WhatsApp"
            checked={notifications.whatsapp.marketing}
            onCheckedChange={v => updateWhatsAppNotification('marketing', v)}
            icon={Mail}
          />
          <NotificationToggle
            label="Alertas de precio"
            description="Notificaciones de cambios de precio en vehículos favoritos"
            checked={notifications.whatsapp.priceAlerts}
            onCheckedChange={v => updateWhatsAppNotification('priceAlerts', v)}
            icon={DollarSign}
          />
        </CardContent>
        <div className="border-t px-6 py-3">
          <p className="text-muted-foreground text-xs">
            Según Ley 172-13 Art. 27 y Política de WhatsApp Business, solo enviaremos mensajes de
            marketing si das tu consentimiento explícito. Puedes cambiarlo en cualquier momento.
          </p>
        </div>
      </Card>

      {/* Data & Privacy */}
      <Card>
        <CardHeader>
          <CardTitle>Datos y Privacidad</CardTitle>
          <CardDescription>Gestiona tus datos personales y el acceso a tu cuenta</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Download data */}
          <div className="flex items-center justify-between py-2">
            <div>
              <p className="text-foreground font-medium dark:text-gray-100">Descargar mis datos</p>
              <p className="text-muted-foreground dark:text-muted-foreground text-sm">
                Obtén una copia de toda tu información (ARCO)
              </p>
            </div>
            <Button variant="outline" size="sm" className="gap-1.5">
              <Download className="h-4 w-4" />
              Solicitar
            </Button>
          </div>
          <Separator />
          <div className="flex items-center justify-between py-2">
            <div>
              <p className="text-foreground font-medium dark:text-gray-100">
                Política de privacidad
              </p>
              <p className="text-muted-foreground dark:text-muted-foreground text-sm">
                Lee cómo protegemos tus datos
              </p>
            </div>
            <Button variant="link" size="sm" asChild>
              <Link href="/privacidad" target="_blank" rel="noopener noreferrer">
                Ver
              </Link>
            </Button>
          </div>
          <Separator />
          <div className="flex items-center justify-between py-2">
            <div>
              <p className="text-foreground font-medium dark:text-gray-100">Términos de servicio</p>
              <p className="text-muted-foreground dark:text-muted-foreground text-sm">
                Condiciones de uso de OKLA
              </p>
            </div>
            <Button variant="link" size="sm" asChild>
              <Link href="/terminos" target="_blank" rel="noopener noreferrer">
                Ver
              </Link>
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Delete Account */}
      <Card className="border-destructive/30">
        <CardHeader>
          <CardTitle className="text-destructive flex items-center gap-2">
            <ShieldAlert className="h-5 w-5" />
            Zona de Peligro
          </CardTitle>
          <CardDescription>
            Acciones irreversibles que afectan permanentemente tu cuenta
          </CardDescription>
        </CardHeader>
        <CardContent>
          {/* STEP: idle */}
          {deletionStep === 'idle' && (
            <div className="border-destructive/20 bg-destructive/5 flex items-start justify-between rounded-lg border p-4">
              <div>
                <p className="text-foreground font-medium dark:text-gray-100">Eliminar cuenta</p>
                <p className="text-muted-foreground dark:text-muted-foreground mt-1 text-sm">
                  Elimina permanentemente tu cuenta y todos tus datos. Tendrás 15 días para cancelar
                  esta acción.
                </p>
              </div>
              <Button
                variant="destructive"
                size="sm"
                className="ml-4 shrink-0 gap-1.5"
                onClick={() => setDeletionStep('selecting')}
              >
                <Trash2 className="h-4 w-4" />
                Eliminar
              </Button>
            </div>
          )}

          {/* STEP: selecting reason */}
          {deletionStep === 'selecting' && (
            <div className="space-y-4">
              <Alert variant="destructive" className="border-destructive/40">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>
                  <strong>¿Estás seguro?</strong> Esta acción eliminará permanentemente tu cuenta,
                  publicaciones, historial y todos tus datos. Tendrás 15 días para cancelarla.
                </AlertDescription>
              </Alert>

              <div className="space-y-2">
                <Label className="text-sm font-medium">¿Por qué quieres eliminar tu cuenta?</Label>
                <div className="space-y-2">
                  {(Object.keys(DeletionReasonMap) as DeletionReasonString[]).map(reason => (
                    <label
                      key={reason}
                      className={cn(
                        'flex cursor-pointer items-center gap-3 rounded-lg border p-3 transition-colors',
                        deletionReason === reason
                          ? 'border-destructive bg-destructive/5'
                          : 'border-border hover:bg-muted/50 dark:border-gray-700'
                      )}
                    >
                      <input
                        type="radio"
                        name="deletionReason"
                        value={reason}
                        checked={deletionReason === reason}
                        onChange={() => setDeletionReason(reason)}
                        className="accent-destructive"
                      />
                      <span className="text-sm">
                        {
                          {
                            PrivacyConcerns: 'Preocupaciones de privacidad',
                            NoLongerNeeded: 'Ya no necesito la cuenta',
                            FoundAlternative: 'Encontré una alternativa',
                            BadExperience: 'Mala experiencia con el servicio',
                            TooManyEmails: 'Recibo demasiados correos',
                            Other: 'Otro motivo',
                          }[reason]
                        }
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              {deletionReason === 'Other' && (
                <div className="space-y-1">
                  <Label className="text-sm">Especifica el motivo</Label>
                  <Input
                    value={deletionOtherReason}
                    onChange={e => setDeletionOtherReason(e.target.value)}
                    placeholder="Escribe tu motivo..."
                    maxLength={200}
                  />
                </div>
              )}

              <div className="space-y-1">
                <Label className="text-sm">Comentarios adicionales (opcional)</Label>
                <Textarea
                  value={deletionFeedback}
                  onChange={e => setDeletionFeedback(e.target.value)}
                  placeholder="Ayúdanos a mejorar..."
                  rows={3}
                  maxLength={500}
                />
              </div>

              {deletionError && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{deletionError}</AlertDescription>
                </Alert>
              )}

              <div className="flex gap-3">
                <Button
                  variant="outline"
                  onClick={() => {
                    setDeletionStep('idle');
                    setDeletionError(null);
                  }}
                  disabled={isDeletionLoading}
                  className="flex-1"
                >
                  Cancelar
                </Button>
                <Button
                  variant="destructive"
                  onClick={handleRequestDeletion}
                  disabled={
                    isDeletionLoading || (deletionReason === 'Other' && !deletionOtherReason)
                  }
                  className="flex-1 gap-1.5"
                >
                  {isDeletionLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Enviando...
                    </>
                  ) : (
                    <>
                      <Trash2 className="h-4 w-4" />
                      Continuar
                    </>
                  )}
                </Button>
              </div>
            </div>
          )}

          {/* STEP: confirming (code + password) */}
          {deletionStep === 'confirming' && (
            <div className="space-y-4">
              <Alert className="border-amber-200 bg-amber-50 dark:border-amber-900/40 dark:bg-amber-950/20">
                <Mail className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                <AlertDescription className="text-amber-800 dark:text-amber-300">
                  Hemos enviado un código de 6 dígitos a tu correo electrónico. Ingresa el código
                  para confirmar la eliminación. Tu cuenta será eliminada el{' '}
                  <strong>
                    {gracePeriodEndsAt
                      ? new Date(gracePeriodEndsAt).toLocaleDateString('es-DO', {
                          day: 'numeric',
                          month: 'long',
                          year: 'numeric',
                        })
                      : 'en 15 días'}
                  </strong>{' '}
                  si no cancelas.
                </AlertDescription>
              </Alert>

              <div className="space-y-1">
                <Label className="text-sm font-medium">Código de confirmación</Label>
                <Input
                  value={confirmationCode}
                  onChange={e => setConfirmationCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  placeholder="000000"
                  maxLength={6}
                  inputMode="numeric"
                  className="tracking-widest"
                />
              </div>

              <div className="space-y-1">
                <Label className="text-sm font-medium">Contraseña actual</Label>
                <Input
                  type="password"
                  value={deletionPassword}
                  onChange={e => setDeletionPassword(e.target.value)}
                  placeholder="Ingresa tu contraseña para confirmar"
                />
              </div>

              {deletionError && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{deletionError}</AlertDescription>
                </Alert>
              )}

              <div className="flex gap-3">
                <Button
                  variant="outline"
                  onClick={handleCancelDeletion}
                  disabled={isDeletionLoading || isCancellingDeletion}
                  className="flex-1"
                >
                  {isCancellingDeletion ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    'Cancelar solicitud'
                  )}
                </Button>
                <Button
                  variant="destructive"
                  onClick={handleConfirmDeletion}
                  disabled={isDeletionLoading || confirmationCode.length < 6 || !deletionPassword}
                  className="flex-1 gap-1.5"
                >
                  {isDeletionLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Confirmando...
                    </>
                  ) : (
                    <>
                      <Trash2 className="h-4 w-4" />
                      Confirmar eliminación
                    </>
                  )}
                </Button>
              </div>
            </div>
          )}

          {/* STEP: success */}
          {deletionStep === 'success' && (
            <div className="space-y-3">
              <Alert className="border-green-200 bg-green-50 dark:border-green-900/40 dark:bg-green-950/20">
                <Check className="h-4 w-4 text-green-600 dark:text-green-400" />
                <AlertDescription className="text-green-800 dark:text-green-300">
                  <strong>Solicitud confirmada.</strong> Tu cuenta ha sido marcada para eliminación.
                  Serás desconectado automáticamente en unos segundos.
                </AlertDescription>
              </Alert>
              <p className="text-muted-foreground text-sm">
                Si cambias de opinión, puedes cancelar la eliminación iniciando sesión antes de la
                fecha límite. Recibirás un recordatorio por email.
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
