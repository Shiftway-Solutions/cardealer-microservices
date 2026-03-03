/**
 * Security Settings Page
 *
 * Manage password, 2FA, and active sessions - API Integrated
 */

'use client';

import * as React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Shield,
  Key,
  Smartphone,
  Laptop,
  Tablet,
  MapPin,
  Clock,
  LogOut,
  AlertTriangle,
  Check,
  Loader2,
  Eye,
  EyeOff,
  Copy,
  Link as LinkIcon,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  authService,
  type Session,
  type TwoFactorSetupResponse,
  type DeletionReasonString,
} from '@/services/auth';
import { useAuth } from '@/hooks/use-auth';
import Image from 'next/image';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';

// ============================================================
// SESSION ITEM COMPONENT
// ============================================================

function SessionItem({
  session,
  onRevoke,
  isRevoking,
}: {
  session: Session;
  onRevoke: () => void;
  isRevoking: boolean;
}) {
  const formatTime = (date: string) => {
    const d = new Date(date);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Activo ahora';
    if (diffMins < 60) return `Hace ${diffMins} minutos`;
    if (diffHours < 24) return `Hace ${diffHours} horas`;
    return `Hace ${diffDays} días`;
  };

  const DeviceIcon = {
    desktop: Laptop,
    mobile: Smartphone,
    tablet: Tablet,
    unknown: Laptop,
  }[session.deviceType];

  return (
    <div className="border-border flex items-start gap-4 rounded-lg border p-4">
      <div
        className={cn(
          'flex h-10 w-10 items-center justify-center rounded-lg',
          session.isCurrent ? 'bg-primary/10 text-primary' : 'bg-muted text-muted-foreground'
        )}
      >
        <DeviceIcon className="h-5 w-5" />
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <h4 className="text-foreground font-medium">{session.deviceName}</h4>
          {session.isCurrent && (
            <span className="bg-primary/10 text-primary rounded-full px-2 py-0.5 text-xs font-medium">
              Este dispositivo
            </span>
          )}
        </div>
        <p className="text-muted-foreground mt-0.5 text-sm">
          {session.deviceType === 'mobile'
            ? 'Dispositivo móvil'
            : session.deviceType === 'tablet'
              ? 'Tablet'
              : 'Computadora'}{' '}
          {session.ipAddress && `• ${session.ipAddress}`}
        </p>
        <div className="text-muted-foreground mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs">
          {session.location && (
            <div className="flex items-center gap-1">
              <MapPin className="h-3 w-3" />
              {session.location}
            </div>
          )}
          <div className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            {formatTime(session.lastActiveAt)}
          </div>
        </div>
      </div>

      {!session.isCurrent && (
        <Button
          variant="ghost"
          size="sm"
          onClick={onRevoke}
          disabled={isRevoking}
          className="text-red-600 hover:bg-red-50 hover:text-red-700"
        >
          {isRevoking ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <LogOut className="h-4 w-4" />
          )}
        </Button>
      )}
    </div>
  );
}

// ============================================================
// 2FA SETUP DIALOG
// ============================================================

function TwoFactorSetupDialog({
  open,
  onOpenChange,
  setupData,
  onVerify,
  isVerifying,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  setupData: TwoFactorSetupResponse | null;
  onVerify: (code: string) => void;
  isVerifying: boolean;
}) {
  const [code, setCode] = React.useState('');
  const [copiedSecret, setCopiedSecret] = React.useState(false);
  const [copiedBackupCodes, setCopiedBackupCodes] = React.useState(false);

  const handleCopySecret = () => {
    if (setupData?.secret) {
      navigator.clipboard.writeText(setupData.secret);
      setCopiedSecret(true);
      setTimeout(() => setCopiedSecret(false), 2000);
    }
  };

  const handleCopyBackupCodes = () => {
    if (setupData?.backupCodes) {
      navigator.clipboard.writeText(setupData.backupCodes.join('\n'));
      setCopiedBackupCodes(true);
      setTimeout(() => setCopiedBackupCodes(false), 2000);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (code.length === 6) {
      onVerify(code);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Configurar autenticación de dos factores</DialogTitle>
          <DialogDescription>
            Escanea el código QR con tu aplicación de autenticación (Google Authenticator, Authy,
            etc.)
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* QR Code - SEGURIDAD: Solo permitir data URIs para prevenir SSRF */}
          {setupData?.qrCodeUrl && setupData.qrCodeUrl.startsWith('data:image/') && (
            <div className="flex justify-center">
              <div className="bg-card rounded-lg border p-3">
                <Image
                  src={setupData.qrCodeUrl}
                  alt="QR Code para 2FA"
                  width={160}
                  height={160}
                  unoptimized
                  className="h-40 w-40 sm:h-48 sm:w-48"
                />
              </div>
            </div>
          )}

          {/* Manual Entry */}
          {setupData?.secret && (
            <div className="space-y-2">
              <Label className="text-muted-foreground text-sm">
                ¿No puedes escanear? Ingresa este código manualmente:
              </Label>
              <div className="flex items-center gap-2">
                <code className="bg-muted flex-1 overflow-x-auto rounded px-3 py-2 font-mono text-xs sm:text-sm">
                  {setupData.secret}
                </code>
                <Button variant="outline" size="sm" onClick={handleCopySecret}>
                  {copiedSecret ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
            </div>
          )}

          {/* Verification Code */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="verification-code">Código de verificación</Label>
              <Input
                id="verification-code"
                type="text"
                inputMode="numeric"
                pattern="[0-9]*"
                maxLength={6}
                placeholder="000000"
                value={code}
                onChange={e => setCode(e.target.value.replace(/\D/g, ''))}
                className="text-center text-lg tracking-widest"
              />
              <p className="text-muted-foreground text-xs">
                Ingresa el código de 6 dígitos de tu aplicación
              </p>
            </div>

            <DialogFooter className="flex-col gap-2 sm:flex-row">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                className="w-full sm:w-auto"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={code.length !== 6 || isVerifying}
                className="w-full sm:w-auto"
              >
                {isVerifying ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Verificando...
                  </>
                ) : (
                  'Verificar y activar'
                )}
              </Button>
            </DialogFooter>
          </form>

          {/* Backup Codes - Important: Show but remind user to save AFTER verifying */}
          {setupData?.backupCodes && setupData.backupCodes.length > 0 && (
            <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 sm:p-4">
              <div className="flex items-start justify-between gap-2">
                <p className="text-sm font-medium text-yellow-800">
                  Guarda estos códigos de respaldo en un lugar seguro:
                </p>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleCopyBackupCodes}
                  className="h-8 shrink-0 text-yellow-700 hover:bg-yellow-100 hover:text-yellow-900"
                >
                  {copiedBackupCodes ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                  <span className="ml-1 hidden sm:inline">
                    {copiedBackupCodes ? 'Copiados' : 'Copiar'}
                  </span>
                </Button>
              </div>
              <div className="mt-2 grid grid-cols-2 gap-1.5 sm:gap-2">
                {setupData.backupCodes.map((backupCode, i) => (
                  <code
                    key={i}
                    className="bg-card rounded px-2 py-1 text-center font-mono text-xs sm:text-sm"
                  >
                    {backupCode}
                  </code>
                ))}
              </div>
              <p className="mt-2 text-xs text-yellow-700">
                ⚠️ Estos códigos solo se mostrarán una vez. Guárdalos ahora.
              </p>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ============================================================
// DISABLE 2FA DIALOG
// ============================================================

function Disable2FADialog({
  open,
  onOpenChange,
  onDisable,
  isDisabling,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onDisable: (password: string) => void;
  isDisabling: boolean;
}) {
  const [password, setPassword] = React.useState('');
  const [showPassword, setShowPassword] = React.useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (password.length >= 1) {
      onDisable(password);
    }
  };

  // Reset form when dialog closes
  React.useEffect(() => {
    if (!open) {
      setPassword('');
      setShowPassword(false);
    }
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Desactivar autenticación de dos factores</DialogTitle>
          <DialogDescription>
            Ingresa tu contraseña para confirmar la desactivación de la autenticación de dos
            factores
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="disable-password">Tu contraseña</Label>
            <div className="relative">
              <Input
                id="disable-password"
                type={showPassword ? 'text' : 'password'}
                placeholder="Ingresa tu contraseña"
                value={password}
                onChange={e => setPassword(e.target.value)}
                className="pr-10"
              />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="absolute top-0 right-0 h-full px-3 hover:bg-transparent"
                onClick={() => setShowPassword(!showPassword)}
              >
                {showPassword ? (
                  <EyeOff className="text-muted-foreground h-4 w-4" />
                ) : (
                  <Eye className="text-muted-foreground h-4 w-4" />
                )}
              </Button>
            </div>
          </div>

          <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            <AlertTriangle className="mb-1 inline h-4 w-4" /> Tu cuenta será menos segura sin 2FA
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" variant="destructive" disabled={!password || isDisabling}>
              {isDisabling ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Desactivando...
                </>
              ) : (
                'Desactivar 2FA'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ============================================================
// DELETE ACCOUNT DIALOG (2-step ARCO compliance flow)
// ============================================================

type DeletionStep = 'reason' | 'confirm';

const DELETION_REASONS: { value: DeletionReasonString; label: string }[] = [
  { value: 'NoLongerNeeded', label: 'Ya no necesito la cuenta' },
  { value: 'FoundAlternative', label: 'Encontré una alternativa' },
  { value: 'BadExperience', label: 'Mala experiencia' },
  { value: 'TooManyEmails', label: 'Demasiados correos' },
  { value: 'PrivacyConcerns', label: 'Preocupaciones de privacidad' },
  { value: 'Other', label: 'Otro' },
];

function DeleteAccountDialog({
  open,
  onOpenChange,
  userEmail,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  userEmail?: string;
}) {
  const router = useRouter();
  const [step, setStep] = React.useState<DeletionStep>('reason');
  const [reason, setReason] = React.useState<DeletionReasonString>('NoLongerNeeded');
  const [otherReason, setOtherReason] = React.useState('');
  const [feedback, setFeedback] = React.useState('');
  const [confirmationCode, setConfirmationCode] = React.useState('');
  const [password, setPassword] = React.useState('');
  const [confirmation, setConfirmation] = React.useState('');

  // Step 1: Request deletion
  const requestDeletionMutation = useMutation({
    mutationFn: authService.requestAccountDeletion,
    onSuccess: data => {
      toast.success(`Código de confirmación enviado a ${data.confirmationEmailSentTo}`);
      setStep('confirm');
    },
    onError: () => {
      toast.error('Error al solicitar la eliminación. Inténtalo de nuevo.');
    },
  });

  // Step 2: Confirm deletion
  const confirmDeletionMutation = useMutation({
    mutationFn: authService.confirmAccountDeletion,
    onSuccess: () => {
      toast.success('Tu cuenta ha sido programada para eliminación');
      onOpenChange(false);
      router.push('/');
    },
    onError: () => {
      toast.error('Código o contraseña incorrectos. Inténtalo de nuevo.');
    },
  });

  const handleRequestDeletion = (e: React.FormEvent) => {
    e.preventDefault();
    requestDeletionMutation.mutate({
      reason,
      otherReason: reason === 'Other' ? otherReason : undefined,
      feedback: feedback || undefined,
    });
  };

  const handleConfirmDeletion = (e: React.FormEvent) => {
    e.preventDefault();
    if (confirmation === 'ELIMINAR' && password && confirmationCode) {
      confirmDeletionMutation.mutate({
        confirmationCode,
        password,
      });
    }
  };

  const resetDialog = () => {
    setStep('reason');
    setReason('NoLongerNeeded');
    setOtherReason('');
    setFeedback('');
    setConfirmationCode('');
    setPassword('');
    setConfirmation('');
  };

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      resetDialog();
    }
    onOpenChange(newOpen);
  };

  const isLoading = requestDeletionMutation.isPending || confirmDeletionMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-red-600">Eliminar cuenta permanentemente</DialogTitle>
          <DialogDescription>
            {step === 'reason'
              ? 'Esta acción eliminará todos tus datos, vehículos publicados, mensajes y historial.'
              : `Ingresa el código enviado a ${userEmail || 'tu correo'} y tu contraseña para confirmar.`}
          </DialogDescription>
        </DialogHeader>

        {step === 'reason' ? (
          <form onSubmit={handleRequestDeletion} className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="deletion-reason">¿Por qué deseas eliminar tu cuenta?</Label>
              <select
                id="deletion-reason"
                value={reason}
                onChange={e => setReason(e.target.value as DeletionReasonString)}
                className="border-input bg-background w-full rounded-md border px-3 py-2 text-sm"
              >
                {DELETION_REASONS.map(r => (
                  <option key={r.value} value={r.value}>
                    {r.label}
                  </option>
                ))}
              </select>
            </div>

            {reason === 'Other' && (
              <div className="space-y-2">
                <Label htmlFor="other-reason">Especifica el motivo</Label>
                <Input
                  id="other-reason"
                  value={otherReason}
                  onChange={e => setOtherReason(e.target.value)}
                  placeholder="Tu motivo..."
                  required
                />
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="feedback">Comentarios adicionales (opcional)</Label>
              <Input
                id="feedback"
                value={feedback}
                onChange={e => setFeedback(e.target.value)}
                placeholder="¿Cómo podemos mejorar?"
              />
            </div>

            <div className="rounded-md bg-yellow-50 p-3 text-sm text-yellow-800">
              <AlertTriangle className="mr-2 inline h-4 w-4" />
              Se enviará un código de confirmación a tu correo electrónico.
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
                Cancelar
              </Button>
              <Button
                type="submit"
                variant="destructive"
                disabled={isLoading || (reason === 'Other' && !otherReason)}
              >
                {requestDeletionMutation.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Enviando código...
                  </>
                ) : (
                  'Continuar'
                )}
              </Button>
            </DialogFooter>
          </form>
        ) : (
          <form onSubmit={handleConfirmDeletion} className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="confirmation-code">Código de confirmación</Label>
              <Input
                id="confirmation-code"
                type="text"
                value={confirmationCode}
                onChange={e => setConfirmationCode(e.target.value)}
                placeholder="Ingresa el código de 6 dígitos"
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="delete-password">Contraseña</Label>
              <Input
                id="delete-password"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="delete-confirmation">
                Escribe <strong>ELIMINAR</strong> para confirmar
              </Label>
              <Input
                id="delete-confirmation"
                type="text"
                value={confirmation}
                onChange={e => setConfirmation(e.target.value.toUpperCase())}
                placeholder="ELIMINAR"
                required
              />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setStep('reason')}>
                Volver
              </Button>
              <Button
                type="submit"
                variant="destructive"
                disabled={
                  confirmation !== 'ELIMINAR' || !password || !confirmationCode || isLoading
                }
              >
                {confirmDeletionMutation.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Eliminando...
                  </>
                ) : (
                  'Eliminar mi cuenta'
                )}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}

// ============================================================
// MAIN COMPONENT
// ============================================================

export default function SecurityPage() {
  const _router = useRouter();
  const queryClient = useQueryClient();
  const { user } = useAuth();

  // Password change state
  const [isChangingPassword, setIsChangingPassword] = React.useState(false);
  const [passwordForm, setPasswordForm] = React.useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [showPasswords, setShowPasswords] = React.useState({
    current: false,
    new: false,
    confirm: false,
  });
  const [passwordError, setPasswordError] = React.useState<string | null>(null);

  // 2FA dialogs
  const [showSetup2FA, setShowSetup2FA] = React.useState(false);
  const [showDisable2FA, setShowDisable2FA] = React.useState(false);
  const [setup2FAData, setSetup2FAData] = React.useState<TwoFactorSetupResponse | null>(null);

  // Delete account dialog
  const [showDeleteAccount, setShowDeleteAccount] = React.useState(false);

  // Revoking session ID for loading state
  const [revokingSessionId, setRevokingSessionId] = React.useState<string | null>(null);

  // ============================================================
  // QUERIES
  // ============================================================

  const { data: sessions = [], isLoading: sessionsLoading } = useQuery({
    queryKey: ['sessions'],
    queryFn: authService.getSessions,
  });

  const { data: twoFactorStatus, isLoading: twoFactorLoading } = useQuery({
    queryKey: ['2fa-status'],
    queryFn: authService.get2FAStatus,
  });

  // Get complete security settings (includes hasPassword and linkedProviders)
  const { data: securitySettings, isLoading: securityLoading } = useQuery({
    queryKey: ['security-settings'],
    queryFn: authService.getSecuritySettings,
  });

  // ============================================================
  // MUTATIONS
  // ============================================================

  const changePasswordMutation = useMutation({
    mutationFn: ({
      currentPassword,
      newPassword,
    }: {
      currentPassword: string;
      newPassword: string;
    }) => authService.changePassword(currentPassword, newPassword),
    onSuccess: () => {
      toast.success('Contraseña actualizada correctamente');
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      setIsChangingPassword(false);
      // Refresh security settings to update hasPassword status
      queryClient.invalidateQueries({ queryKey: ['security-settings'] });
    },
    onError: (error: Error) => {
      setPasswordError(error.message || 'Error al cambiar la contraseña');
    },
  });

  // Mutation for OAuth users setting password for the first time
  const setPasswordMutation = useMutation({
    mutationFn: (password: string) => authService.setPassword(password),
    onSuccess: () => {
      toast.success('Contraseña establecida correctamente');
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      setIsChangingPassword(false);
      // Refresh security settings to update hasPassword status
      queryClient.invalidateQueries({ queryKey: ['security-settings'] });
    },
    onError: (error: Error) => {
      setPasswordError(error.message || 'Error al establecer la contraseña');
    },
  });

  const revokeSessionMutation = useMutation({
    mutationFn: authService.revokeSession,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
      toast.success('Sesión cerrada correctamente');
      setRevokingSessionId(null);
    },
    onError: () => {
      toast.error('Error al cerrar la sesión');
      setRevokingSessionId(null);
    },
  });

  const revokeAllSessionsMutation = useMutation({
    mutationFn: authService.revokeAllSessions,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
      toast.success('Todas las otras sesiones han sido cerradas');
    },
    onError: () => {
      toast.error('Error al cerrar las sesiones');
    },
  });

  const setup2FAMutation = useMutation({
    mutationFn: authService.setup2FA,
    onSuccess: data => {
      setSetup2FAData(data);
      setShowSetup2FA(true);
    },
    onError: () => {
      toast.error('Error al configurar 2FA');
    },
  });

  const enable2FAMutation = useMutation({
    mutationFn: authService.enable2FA,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['2fa-status'] });
      setShowSetup2FA(false);
      setSetup2FAData(null);
      toast.success('Autenticación de dos factores activada');
    },
    onError: () => {
      toast.error('Código inválido. Inténtalo de nuevo.');
    },
  });

  const disable2FAMutation = useMutation({
    mutationFn: authService.disable2FA,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['2fa-status'] });
      setShowDisable2FA(false);
      toast.success('Autenticación de dos factores desactivada');
    },
    onError: () => {
      toast.error('Contraseña incorrecta. Inténtalo de nuevo.');
    },
  });

  // ============================================================
  // HANDLERS
  // ============================================================

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError(null);

    if (passwordForm.newPassword.length < 8) {
      setPasswordError('La contraseña debe tener al menos 8 caracteres');
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordError('Las contraseñas no coinciden');
      return;
    }

    changePasswordMutation.mutate({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    });
  };

  // Handler for OAuth users setting password for the first time
  const handleSetPasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError(null);

    if (passwordForm.newPassword.length < 8) {
      setPasswordError('La contraseña debe tener al menos 8 caracteres');
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordError('Las contraseñas no coinciden');
      return;
    }

    setPasswordMutation.mutate(passwordForm.newPassword);
  };

  const handleToggle2FA = () => {
    if (twoFactorStatus?.isEnabled) {
      setShowDisable2FA(true);
    } else {
      setup2FAMutation.mutate();
    }
  };

  const handleRevokeSession = (sessionId: string) => {
    setRevokingSessionId(sessionId);
    revokeSessionMutation.mutate(sessionId);
  };

  // Password strength
  const getPasswordStrength = (password: string) => {
    let strength = 0;
    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;
    if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++;
    if (/\d/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;
    return strength;
  };

  const passwordStrength = getPasswordStrength(passwordForm.newPassword);
  const strengthLabels = ['Muy débil', 'Débil', 'Regular', 'Fuerte', 'Muy fuerte'];
  const strengthColors = [
    'bg-red-500',
    'bg-orange-500',
    'bg-yellow-500',
    'bg-green-500',
    'bg-green-600',
  ];

  const otherSessions = sessions.filter(s => !s.isCurrent);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-foreground text-2xl font-bold">Seguridad</h1>
        <p className="text-muted-foreground">Gestiona tu contraseña y la seguridad de tu cuenta</p>
      </div>

      {/* Password Section */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Key className="h-5 w-5" />
            Contraseña
          </CardTitle>
          <CardDescription>
            {securitySettings?.hasPassword
              ? 'Cambia tu contraseña periódicamente para mantener tu cuenta segura'
              : 'Establece una contraseña para poder acceder también con email y contraseña'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {securityLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-10 w-32" />
            </div>
          ) : !securitySettings?.hasPassword ? (
            // OAuth user without password - show "Set Password" option
            <div className="space-y-4">
              {/* Linked accounts info */}
              {securitySettings?.linkedProviders && securitySettings.linkedProviders.length > 0 && (
                <div className="rounded-lg border border-blue-100 bg-blue-50 p-4">
                  <div className="flex items-start gap-3">
                    <LinkIcon className="mt-0.5 h-5 w-5 text-blue-600" />
                    <div className="flex-1">
                      <p className="font-medium text-blue-900">Cuentas vinculadas</p>
                      <p className="mt-1 text-sm text-blue-700">
                        Iniciaste sesión con {securitySettings.linkedProviders[0].provider}. No
                        tienes una contraseña establecida.
                      </p>
                      <div className="mt-3 flex items-center gap-2">
                        {securitySettings.linkedProviders[0].provider === 'Google' && (
                          <svg className="h-5 w-5" viewBox="0 0 24 24">
                            <path
                              fill="#4285F4"
                              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                            />
                            <path
                              fill="#34A853"
                              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                            />
                            <path
                              fill="#FBBC05"
                              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                            />
                            <path
                              fill="#EA4335"
                              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                            />
                          </svg>
                        )}
                        {securitySettings.linkedProviders[0].provider === 'Apple' && (
                          <svg className="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.81-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z" />
                          </svg>
                        )}
                        <span className="text-sm font-medium text-blue-800">
                          {securitySettings.linkedProviders[0].email}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              <div className="flex items-center justify-between">
                <div>
                  <p className="text-muted-foreground text-sm">
                    Puedes establecer una contraseña para también iniciar sesión con tu email
                  </p>
                </div>
                <Button variant="outline" onClick={() => setIsChangingPassword(true)}>
                  Establecer contraseña
                </Button>
              </div>

              {/* Set password form (simplified - no current password needed) */}
              {isChangingPassword && (
                <form
                  onSubmit={handleSetPasswordSubmit}
                  className="border-border mt-4 space-y-4 border-t pt-4"
                >
                  {passwordError && (
                    <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                      <AlertTriangle className="h-4 w-4 flex-shrink-0" />
                      {passwordError}
                    </div>
                  )}

                  <div className="space-y-2">
                    <Label htmlFor="newPassword">Nueva contraseña</Label>
                    <div className="relative">
                      <Input
                        id="newPassword"
                        type={showPasswords.new ? 'text' : 'password'}
                        value={passwordForm.newPassword}
                        onChange={e =>
                          setPasswordForm({ ...passwordForm, newPassword: e.target.value })
                        }
                        placeholder="Mínimo 8 caracteres"
                        required
                      />
                      <button
                        type="button"
                        onClick={() =>
                          setShowPasswords({ ...showPasswords, new: !showPasswords.new })
                        }
                        className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
                      >
                        {showPasswords.new ? (
                          <EyeOff className="h-4 w-4" />
                        ) : (
                          <Eye className="h-4 w-4" />
                        )}
                      </button>
                    </div>
                    {passwordForm.newPassword && (
                      <div className="space-y-1">
                        <div className="flex gap-1">
                          {[...Array(5)].map((_, i) => (
                            <div
                              key={i}
                              className={cn(
                                'h-1 flex-1 rounded-full transition-colors',
                                i < passwordStrength
                                  ? strengthColors[passwordStrength - 1]
                                  : 'bg-muted'
                              )}
                            />
                          ))}
                        </div>
                        <p className="text-muted-foreground text-xs">
                          Fortaleza: {strengthLabels[passwordStrength - 1] || 'Muy débil'}
                        </p>
                      </div>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="confirmPassword">Confirmar contraseña</Label>
                    <div className="relative">
                      <Input
                        id="confirmPassword"
                        type={showPasswords.confirm ? 'text' : 'password'}
                        value={passwordForm.confirmPassword}
                        onChange={e =>
                          setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })
                        }
                        required
                      />
                      <button
                        type="button"
                        onClick={() =>
                          setShowPasswords({ ...showPasswords, confirm: !showPasswords.confirm })
                        }
                        className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
                      >
                        {showPasswords.confirm ? (
                          <EyeOff className="h-4 w-4" />
                        ) : (
                          <Eye className="h-4 w-4" />
                        )}
                      </button>
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button type="submit" disabled={setPasswordMutation.isPending}>
                      {setPasswordMutation.isPending ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Guardando...
                        </>
                      ) : (
                        'Establecer contraseña'
                      )}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => {
                        setIsChangingPassword(false);
                        setPasswordForm({
                          currentPassword: '',
                          newPassword: '',
                          confirmPassword: '',
                        });
                        setPasswordError(null);
                      }}
                    >
                      Cancelar
                    </Button>
                  </div>
                </form>
              )}
            </div>
          ) : isChangingPassword ? (
            <form onSubmit={handlePasswordSubmit} className="space-y-4">
              {passwordError && (
                <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  <AlertTriangle className="h-4 w-4 flex-shrink-0" />
                  {passwordError}
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="currentPassword">Contraseña actual</Label>
                <div className="relative">
                  <Input
                    id="currentPassword"
                    type={showPasswords.current ? 'text' : 'password'}
                    value={passwordForm.currentPassword}
                    onChange={e =>
                      setPasswordForm({ ...passwordForm, currentPassword: e.target.value })
                    }
                    required
                  />
                  <button
                    type="button"
                    onClick={() =>
                      setShowPasswords({ ...showPasswords, current: !showPasswords.current })
                    }
                    className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
                  >
                    {showPasswords.current ? (
                      <EyeOff className="h-4 w-4" />
                    ) : (
                      <Eye className="h-4 w-4" />
                    )}
                  </button>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="newPassword">Nueva contraseña</Label>
                <div className="relative">
                  <Input
                    id="newPassword"
                    type={showPasswords.new ? 'text' : 'password'}
                    value={passwordForm.newPassword}
                    onChange={e =>
                      setPasswordForm({ ...passwordForm, newPassword: e.target.value })
                    }
                    required
                  />
                  <button
                    type="button"
                    onClick={() => setShowPasswords({ ...showPasswords, new: !showPasswords.new })}
                    className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
                  >
                    {showPasswords.new ? (
                      <EyeOff className="h-4 w-4" />
                    ) : (
                      <Eye className="h-4 w-4" />
                    )}
                  </button>
                </div>
                {passwordForm.newPassword && (
                  <div className="space-y-1">
                    <div className="flex gap-1">
                      {[...Array(5)].map((_, i) => (
                        <div
                          key={i}
                          className={cn(
                            'h-1 flex-1 rounded-full transition-colors',
                            i < passwordStrength ? strengthColors[passwordStrength - 1] : 'bg-muted'
                          )}
                        />
                      ))}
                    </div>
                    <p className="text-muted-foreground text-xs">
                      Fortaleza: {strengthLabels[passwordStrength - 1] || 'Muy débil'}
                    </p>
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="confirmPassword">Confirmar nueva contraseña</Label>
                <div className="relative">
                  <Input
                    id="confirmPassword"
                    type={showPasswords.confirm ? 'text' : 'password'}
                    value={passwordForm.confirmPassword}
                    onChange={e =>
                      setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })
                    }
                    required
                  />
                  <button
                    type="button"
                    onClick={() =>
                      setShowPasswords({ ...showPasswords, confirm: !showPasswords.confirm })
                    }
                    className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
                  >
                    {showPasswords.confirm ? (
                      <EyeOff className="h-4 w-4" />
                    ) : (
                      <Eye className="h-4 w-4" />
                    )}
                  </button>
                </div>
              </div>

              <div className="flex gap-2">
                <Button type="submit" disabled={changePasswordMutation.isPending}>
                  {changePasswordMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Guardando...
                    </>
                  ) : (
                    'Cambiar contraseña'
                  )}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setIsChangingPassword(false);
                    setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
                    setPasswordError(null);
                  }}
                >
                  Cancelar
                </Button>
              </div>
            </form>
          ) : (
            <div className="flex items-center justify-between">
              <div>
                <p className="text-muted-foreground text-sm">
                  Usa una contraseña fuerte que no uses en otros sitios
                </p>
              </div>
              <Button variant="outline" onClick={() => setIsChangingPassword(true)}>
                Cambiar contraseña
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* 2FA Section */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Smartphone className="h-5 w-5" />
            Autenticación de dos factores (2FA)
          </CardTitle>
          <CardDescription>Añade una capa extra de seguridad a tu cuenta</CardDescription>
        </CardHeader>
        <CardContent>
          {twoFactorLoading ? (
            <div className="flex items-center justify-between">
              <div className="space-y-2">
                <Skeleton className="h-5 w-24" />
                <Skeleton className="h-4 w-48" />
              </div>
              <Skeleton className="h-6 w-11 rounded-full" />
            </div>
          ) : (
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <p className="font-medium">
                  {twoFactorStatus?.isEnabled ? 'Habilitado' : 'Deshabilitado'}
                </p>
                <p className="text-muted-foreground text-sm">
                  {twoFactorStatus?.isEnabled
                    ? `Tu cuenta está protegida con 2FA. ${twoFactorStatus.backupCodesRemaining} códigos de respaldo restantes.`
                    : 'Recomendamos activar 2FA para mayor seguridad'}
                </p>
              </div>
              <Switch
                checked={twoFactorStatus?.isEnabled ?? false}
                onCheckedChange={handleToggle2FA}
                disabled={setup2FAMutation.isPending}
                className="data-[state=unchecked]:bg-muted"
              />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Active Sessions */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                Sesiones activas
              </CardTitle>
              <CardDescription>Dispositivos donde has iniciado sesión</CardDescription>
            </div>
            {otherSessions.length > 0 && (
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-red-600 hover:bg-red-50 hover:text-red-700"
                  >
                    Cerrar otras sesiones
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>¿Cerrar todas las otras sesiones?</AlertDialogTitle>
                    <AlertDialogDescription>
                      Tendrás que volver a iniciar sesión en esos dispositivos.
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>Cancelar</AlertDialogCancel>
                    <AlertDialogAction
                      onClick={() => revokeAllSessionsMutation.mutate()}
                      className="bg-red-600 hover:bg-red-700"
                    >
                      {revokeAllSessionsMutation.isPending ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Cerrando...
                        </>
                      ) : (
                        'Cerrar sesiones'
                      )}
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {sessionsLoading ? (
            <>
              <Skeleton className="h-24 w-full rounded-lg" />
              <Skeleton className="h-24 w-full rounded-lg" />
            </>
          ) : sessions.length === 0 ? (
            <p className="text-muted-foreground py-4 text-center text-sm">
              No hay sesiones activas
            </p>
          ) : (
            sessions.map(session => (
              <SessionItem
                key={session.id}
                session={session}
                onRevoke={() => handleRevokeSession(session.id)}
                isRevoking={revokingSessionId === session.id}
              />
            ))
          )}
        </CardContent>
      </Card>

      {/* Danger Zone */}
      <Card className="border-red-200">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-red-600">
            <AlertTriangle className="h-5 w-5" />
            Zona de peligro
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-foreground font-medium">Eliminar cuenta</p>
              <p className="text-muted-foreground text-sm">
                Esta acción es permanente y no se puede deshacer
              </p>
            </div>
            <Button variant="destructive" size="sm" onClick={() => setShowDeleteAccount(true)}>
              Eliminar cuenta
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* 2FA Setup Dialog */}
      <TwoFactorSetupDialog
        open={showSetup2FA}
        onOpenChange={setShowSetup2FA}
        setupData={setup2FAData}
        onVerify={code => enable2FAMutation.mutate(code)}
        isVerifying={enable2FAMutation.isPending}
      />

      {/* Disable 2FA Dialog */}
      <Disable2FADialog
        open={showDisable2FA}
        onOpenChange={setShowDisable2FA}
        onDisable={code => disable2FAMutation.mutate(code)}
        isDisabling={disable2FAMutation.isPending}
      />

      {/* Delete Account Dialog */}
      <DeleteAccountDialog
        open={showDeleteAccount}
        onOpenChange={setShowDeleteAccount}
        userEmail={user?.email}
      />
    </div>
  );
}
