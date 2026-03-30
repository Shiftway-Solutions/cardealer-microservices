/**
 * Dealer Settings Page
 *
 * Configure dealer account settings
 */

'use client';

import Image from 'next/image';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Settings,
  Bell,
  Shield,
  Users,
  Mail,
  MessageSquare,
  Smartphone,
  Globe,
  Lock,
  Check,
  Loader2,
  CreditCard,
} from 'lucide-react';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { useDealerEmployees } from '@/hooks/use-dealer-employees';
import {
  useDealerSettings,
  useUpdateNotificationSettings,
  useUpdateSecuritySettings,
  usePaymentGatewaySettings,
  useUpdatePaymentGatewaySettings,
} from '@/hooks/use-dealer-settings';
import { defaultNotificationSettings, type NotificationSettings } from '@/services/dealer-settings';
import { toast } from 'sonner';
import { VideoHelpButton } from '@/components/dealer/video-help-button';

// ============================================================================
// Loading Skeleton
// ============================================================================

function SettingsSkeleton() {
  return (
    <div className="space-y-6">
      <div>
        <Skeleton className="mb-2 h-8 w-40" />
        <Skeleton className="h-4 w-60" />
      </div>
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent className="space-y-6">
              {[1, 2, 3, 4].map(i => (
                <div key={i} className="flex items-center justify-between">
                  <div>
                    <Skeleton className="mb-1 h-4 w-32" />
                    <Skeleton className="h-3 w-48" />
                  </div>
                  <Skeleton className="h-6 w-10" />
                </div>
              ))}
            </CardContent>
          </Card>
        </div>
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-28" />
            </CardHeader>
            <CardContent className="space-y-2">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

export default function DealerSettingsPage() {
  // Get current dealer
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  // Get settings
  const { data: settings, isLoading: settingsLoading } = useDealerSettings(dealerId);

  // Get employees for team section
  const { data: employees } = useDealerEmployees(dealerId);

  // Mutations
  const updateNotifications = useUpdateNotificationSettings(dealerId);
  const updateSecurity = useUpdateSecuritySettings(dealerId);

  // Payment gateway preferences
  const { data: gatewayData, isLoading: gatewaysLoading } = usePaymentGatewaySettings();
  const updateGateways = useUpdatePaymentGatewaySettings();

  // Local state for form
  const [notifications, setNotifications] = useState<NotificationSettings>(
    defaultNotificationSettings
  );
  const [sessionTimeout, setSessionTimeout] = useState(30);
  const [hasChanges, setHasChanges] = useState(false);

  // Sync settings when loaded
  useEffect(() => {
    if (settings) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setNotifications(settings.notifications);
      setSessionTimeout(settings.security.sessionTimeoutMinutes);
    }
  }, [settings]);

  // Track changes
  useEffect(() => {
    if (settings) {
      const notifChanged = JSON.stringify(notifications) !== JSON.stringify(settings.notifications);
      const securityChanged = sessionTimeout !== settings.security.sessionTimeoutMinutes;
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setHasChanges(notifChanged || securityChanged);
    }
  }, [notifications, sessionTimeout, settings]);

  const isLoading = dealerLoading || settingsLoading;

  // Handle save
  const handleSave = async () => {
    try {
      await Promise.all([
        updateNotifications.mutateAsync(notifications),
        updateSecurity.mutateAsync({ sessionTimeoutMinutes: sessionTimeout }),
      ]);
      toast.success('Configuración guardada');
      setHasChanges(false);
    } catch {
      toast.error('Error al guardar la configuración');
    }
  };

  if (isLoading) {
    return <SettingsSkeleton />;
  }

  const isSaving = updateNotifications.isPending || updateSecurity.isPending;
  const activeEmployees = employees?.filter(e => e.status === 'Active') || [];
  const maxEmployees = Math.floor((dealer?.maxActiveListings || 15) / 3);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-foreground text-2xl font-bold">Configuración</h1>
            <VideoHelpButton sectionKey="configuracion" variant="icon" />
          </div>
          <p className="text-muted-foreground">Personaliza tu experiencia en OKLA</p>
        </div>
        {hasChanges && (
          <Button
            onClick={handleSave}
            disabled={isSaving}
            className="bg-primary hover:bg-primary/90"
          >
            {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Guardar Cambios
          </Button>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main Settings */}
        <div className="space-y-6 lg:col-span-2">
          {/* Notifications */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Bell className="h-5 w-5" />
                Notificaciones
              </CardTitle>
              <CardDescription>Configura cómo quieres recibir alertas</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Email */}
              <div>
                <h4 className="mb-4 flex items-center gap-2 font-medium">
                  <Mail className="h-4 w-4" />
                  Email
                </h4>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Nuevos leads</p>
                      <p className="text-muted-foreground text-sm">
                        Recibe un email cuando alguien contacte
                      </p>
                    </div>
                    <Switch
                      checked={notifications.emailNewLead}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, emailNewLead: checked }))
                      }
                    />
                  </div>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Mensajes</p>
                      <p className="text-muted-foreground text-sm">
                        Notificación de nuevos mensajes
                      </p>
                    </div>
                    <Switch
                      checked={notifications.emailMessages}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, emailMessages: checked }))
                      }
                    />
                  </div>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Citas programadas</p>
                      <p className="text-muted-foreground text-sm">Recordatorios de test drives</p>
                    </div>
                    <Switch
                      checked={notifications.emailAppointments}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, emailAppointments: checked }))
                      }
                    />
                  </div>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Reporte semanal</p>
                      <p className="text-muted-foreground text-sm">
                        Resumen de actividad cada lunes
                      </p>
                    </div>
                    <Switch
                      checked={notifications.emailWeeklyReport}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, emailWeeklyReport: checked }))
                      }
                    />
                  </div>
                </div>
              </div>

              <hr />

              {/* SMS */}
              <div>
                <h4 className="mb-4 flex items-center gap-2 font-medium">
                  <Smartphone className="h-4 w-4" />
                  SMS
                </h4>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Nuevos leads urgentes</p>
                      <p className="text-muted-foreground text-sm">Solo leads de alta prioridad</p>
                    </div>
                    <Switch
                      checked={notifications.smsNewLead}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, smsNewLead: checked }))
                      }
                    />
                  </div>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Recordatorio de citas</p>
                      <p className="text-muted-foreground text-sm">1 hora antes de cada cita</p>
                    </div>
                    <Switch
                      checked={notifications.smsAppointments}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, smsAppointments: checked }))
                      }
                    />
                  </div>
                </div>
              </div>

              <hr />

              {/* Push */}
              <div>
                <h4 className="mb-4 flex items-center gap-2 font-medium">
                  <MessageSquare className="h-4 w-4" />
                  Notificaciones Push
                </h4>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Mensajes</p>
                      <p className="text-muted-foreground text-sm">Notificación instantánea</p>
                    </div>
                    <Switch
                      checked={notifications.pushMessages}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, pushMessages: checked }))
                      }
                    />
                  </div>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Nuevos leads</p>
                      <p className="text-muted-foreground text-sm">Alerta inmediata de contactos</p>
                    </div>
                    <Switch
                      checked={notifications.pushLeads}
                      onCheckedChange={checked =>
                        setNotifications(prev => ({ ...prev, pushLeads: checked }))
                      }
                    />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Security — Managed in /cuenta/seguridad */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                Seguridad
              </CardTitle>
              <CardDescription>Protege tu cuenta</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground mb-4 text-sm">
                Gestiona tu contraseña, autenticación de dos factores y sesiones activas desde la
                configuración de seguridad de tu cuenta.
              </p>
              <Button variant="outline" className="w-full" asChild>
                <Link href="/cuenta/seguridad">
                  <Shield className="mr-2 h-4 w-4" />
                  Ir a Seguridad de la Cuenta
                </Link>
              </Button>
            </CardContent>
          </Card>

          {/* Payment Gateways */}
          <Card>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <CreditCard className="h-5 w-5" />
                    Pasarelas de Pago
                  </CardTitle>
                  <CardDescription>
                    Activa las pasarelas que aceptarás para pagos de tu suscripción OKLA
                  </CardDescription>
                </div>
                {updateGateways.isPending && (
                  <Loader2 className="text-primary mt-1 h-4 w-4 animate-spin" />
                )}
              </div>
            </CardHeader>
            <CardContent>
              {gatewaysLoading ? (
                <div className="space-y-3">
                  {[1, 2, 3].map(i => (
                    <div
                      key={i}
                      className="flex items-center justify-between rounded-xl border p-4"
                    >
                      <div className="flex items-center gap-3">
                        <div className="bg-muted h-10 w-10 animate-pulse rounded-lg" />
                        <div>
                          <div className="bg-muted mb-1 h-4 w-24 animate-pulse rounded" />
                          <div className="bg-muted h-3 w-36 animate-pulse rounded" />
                        </div>
                      </div>
                      <div className="bg-muted h-6 w-10 animate-pulse rounded-full" />
                    </div>
                  ))}
                </div>
              ) : (
                <div className="space-y-3">
                  {(gatewayData ?? []).map(gateway => (
                    <div
                      key={gateway.id}
                      className="flex items-center justify-between rounded-xl border p-4 transition-colors hover:bg-gray-50/50"
                    >
                      <div className="flex items-center gap-3">
                        <div
                          className={[
                            'flex h-10 w-10 items-center justify-center rounded-lg text-lg',
                            gateway.isEnabled ? 'bg-blue-50' : 'bg-muted',
                          ].join(' ')}
                        >
                          {gateway.isLocal ? '🇷🇩' : '🌐'}
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <span
                              className={[
                                'font-medium',
                                !gateway.isEnabled && 'text-muted-foreground',
                              ]
                                .filter(Boolean)
                                .join(' ')}
                            >
                              {gateway.name}
                            </span>
                            <span
                              className={[
                                'rounded-full px-2 py-0.5 text-xs font-medium',
                                gateway.isLocal
                                  ? 'bg-blue-50 text-blue-700'
                                  : 'bg-purple-50 text-purple-700',
                              ].join(' ')}
                            >
                              {gateway.isLocal ? 'Rep. Dom.' : 'Internacional'}
                            </span>
                            {gateway.recommended && gateway.isEnabled && (
                              <span className="rounded-full bg-green-50 px-2 py-0.5 text-xs font-medium text-green-700">
                                Recomendado
                              </span>
                            )}
                          </div>
                          <p className="text-muted-foreground mt-0.5 truncate text-sm">
                            {gateway.description}
                          </p>
                        </div>
                      </div>
                      <Switch
                        checked={gateway.isEnabled}
                        disabled={updateGateways.isPending}
                        onCheckedChange={async checked => {
                          const current = gatewayData ?? [];
                          const next = current.map(g =>
                            g.id === gateway.id ? { ...g, isEnabled: checked } : g
                          );
                          const enabledIds = next.filter(g => g.isEnabled).map(g => g.id);
                          if (enabledIds.length === 0) {
                            toast.error('Debes tener al menos una pasarela habilitada.');
                            return;
                          }
                          try {
                            await updateGateways.mutateAsync(enabledIds);
                            toast.success(
                              `${gateway.name} ${checked ? 'habilitada' : 'deshabilitada'} correctamente`
                            );
                          } catch {
                            toast.error('No se pudo actualizar la pasarela de pago.');
                          }
                        }}
                      />
                    </div>
                  ))}
                </div>
              )}
              <p className="text-muted-foreground mt-4 text-xs">
                Las pasarelas habilitadas aparecerán como opciones cuando agregues un método de
                pago.
              </p>
            </CardContent>
          </Card>

          {/* Team */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Equipo
              </CardTitle>
              <CardDescription>Gestiona accesos de empleados</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {activeEmployees.slice(0, 3).map(employee => (
                  <div
                    key={employee.id}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div className="flex items-center gap-3">
                      {employee.avatarUrl ? (
                        <Image
                          src={employee.avatarUrl}
                          alt={employee.name}
                          width={40}
                          height={40}
                          className="h-10 w-10 rounded-full object-cover"
                        />
                      ) : (
                        <div className="bg-primary/10 text-primary flex h-10 w-10 items-center justify-center rounded-full font-medium">
                          {employee.name
                            .split(' ')
                            .map(n => n[0])
                            .join('')
                            .slice(0, 2)}
                        </div>
                      )}
                      <div>
                        <p className="font-medium">{employee.name}</p>
                        <p className="text-muted-foreground text-sm">{employee.email}</p>
                      </div>
                    </div>
                    <Badge variant={employee.role === 'Owner' ? 'default' : 'outline'}>
                      {employee.role}
                    </Badge>
                  </div>
                ))}
                {activeEmployees.length === 0 && (
                  <p className="text-muted-foreground py-4 text-center text-sm">
                    No hay empleados registrados
                  </p>
                )}
              </div>
              <Button variant="outline" className="mt-4 w-full">
                <Users className="mr-2 h-4 w-4" />
                Invitar Empleado
              </Button>
              <p className="text-muted-foreground mt-2 text-center text-xs">
                Plan {dealer?.plan || 'Pro'}: {activeEmployees.length} de {maxEmployees} empleados
                utilizados
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Links */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Acceso Rápido</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="ghost" className="w-full justify-start">
                <Globe className="mr-2 h-4 w-4" />
                Ver Perfil Público
              </Button>
              <Button variant="ghost" className="w-full justify-start">
                <Settings className="mr-2 h-4 w-4" />
                Editar Perfil
              </Button>
              <Button variant="ghost" className="w-full justify-start">
                <Lock className="mr-2 h-4 w-4" />
                Privacidad
              </Button>
            </CardContent>
          </Card>

          {/* Account Status */}
          {dealer && dealer.verificationStatus === 'verified' && (
            <Card className="border-primary bg-primary/10">
              <CardContent className="p-4">
                <div className="mb-3 flex items-center gap-3">
                  <div className="bg-primary/10 rounded-full p-2">
                    <Check className="text-primary h-5 w-5" />
                  </div>
                  <div>
                    <p className="text-primary font-medium">Cuenta Verificada</p>
                    <p className="text-primary text-sm">Dealer oficial</p>
                  </div>
                </div>
                <div className="text-primary space-y-1 text-sm">
                  <p>✓ Documentos verificados</p>
                  <p>✓ Identidad confirmada</p>
                  {dealer.rnc && <p>✓ RNC validado</p>}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Danger Zone */}
          <Card className="border-red-200">
            <CardHeader>
              <CardTitle className="text-lg text-red-600">Zona de Peligro</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button
                variant="outline"
                className="w-full border-red-200 text-red-600 hover:bg-red-50"
              >
                Pausar Cuenta
              </Button>
              <Button
                variant="outline"
                className="w-full border-red-200 text-red-600 hover:bg-red-50"
              >
                Eliminar Cuenta
              </Button>
              <p className="text-muted-foreground text-xs">
                Eliminar tu cuenta es permanente y no se puede deshacer.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
