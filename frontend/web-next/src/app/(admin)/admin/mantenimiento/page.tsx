/**
 * Admin Maintenance Mode Page
 *
 * Full management of maintenance windows connected to MaintenanceService backend.
 * - Toggle immediate maintenance mode on/off
 * - Schedule future maintenance windows
 * - View active, upcoming, and completed maintenance history
 * - Cancel or complete in-progress maintenance
 */

'use client';

import { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import {
  AlertTriangle,
  Calendar,
  Clock,
  Settings,
  CheckCircle,
  Loader2,
  Play,
  Square,
  XCircle,
  Trash2,
  Wrench,
  Server,
  AlertCircle,
} from 'lucide-react';
import {
  maintenanceService,
  MaintenanceType,
  MaintenanceStatus,
  type MaintenanceStatusResponse,
  type MaintenanceWindowDto,
} from '@/services/maintenance';
import { formatDateTime } from '@/lib/utils';

// =============================================================================
// HELPERS
// =============================================================================

function formatDuration(start: string, end: string): string {
  const diff = new Date(end).getTime() - new Date(start).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${minutes}m`;
}

function getStatusBadge(status: string) {
  switch (status) {
    case 'InProgress':
      return <Badge className="border-red-200 bg-red-100 text-red-700">En Progreso</Badge>;
    case 'Scheduled':
      return <Badge className="border-blue-200 bg-blue-100 text-blue-700">Programado</Badge>;
    case 'Completed':
      return <Badge className="border-primary bg-primary/10 text-primary">Completado</Badge>;
    case 'Cancelled':
      return <Badge className="border-gray-200 bg-gray-100 text-gray-600">Cancelado</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
}

function getTypeBadge(type: string) {
  switch (type) {
    case 'Emergency':
      return (
        <Badge variant="outline" className="border-red-300 text-red-600">
          Emergencia
        </Badge>
      );
    case 'Scheduled':
      return (
        <Badge variant="outline" className="border-blue-300 text-blue-600">
          Programado
        </Badge>
      );
    case 'Database':
      return (
        <Badge variant="outline" className="border-purple-300 text-purple-600">
          Base de Datos
        </Badge>
      );
    case 'Deployment':
      return (
        <Badge variant="outline" className="border-orange-300 text-orange-600">
          Despliegue
        </Badge>
      );
    case 'Infrastructure':
      return (
        <Badge variant="outline" className="border-cyan-300 text-cyan-600">
          Infraestructura
        </Badge>
      );
    default:
      return <Badge variant="outline">{type}</Badge>;
  }
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AdminMaintenancePage() {
  // --- State ---
  const [status, setStatus] = useState<MaintenanceStatusResponse | null>(null);
  const [allWindows, setAllWindows] = useState<MaintenanceWindowDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Immediate maintenance form
  const [immediateMessage, setImmediateMessage] = useState(
    'Estamos realizando mejoras en la plataforma. Volvemos pronto...'
  );
  const [immediateDuration, setImmediateDuration] = useState('60');
  const [notifyUsers, setNotifyUsers] = useState(true);

  // Schedule form
  const [schedTitle, setSchedTitle] = useState('');
  const [schedDescription, setSchedDescription] = useState('');
  const [schedType, setSchedType] = useState<number>(MaintenanceType.Scheduled);
  const [schedStartDate, setSchedStartDate] = useState('');
  const [schedStartTime, setSchedStartTime] = useState('');
  const [schedEndDate, setSchedEndDate] = useState('');
  const [schedEndTime, setSchedEndTime] = useState('');
  const [schedNotify, setSchedNotify] = useState(true);
  const [schedNotifyMinutes, setSchedNotifyMinutes] = useState('30');

  // Cancel dialog
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [cancelReason, setCancelReason] = useState('');

  // --- Data loading ---
  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      // 1. Always fetch public status (no auth required)
      const statusRes = await maintenanceService.getStatus();
      setStatus(statusRes);

      // 2. Try to fetch all windows (admin-only, requires auth)
      try {
        const windowsRes = await maintenanceService.getAll();
        setAllWindows(windowsRes);
      } catch (adminErr: unknown) {
        const axiosErr = adminErr as { response?: { status?: number } };
        if (axiosErr?.response?.status === 401) {
          if (process.env.NODE_ENV === 'development')
            console.warn(
              'No autorizado para listar ventanas de mantenimiento. Verifica tu sesión.'
            );
          setError(
            'Sesión expirada o no autorizado. Inicia sesión como administrador para gestionar mantenimientos.'
          );
        } else {
          if (process.env.NODE_ENV === 'development')
            console.warn('Error al cargar ventanas de mantenimiento:', adminErr);
        }
        setAllWindows([]);
      }
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error loading maintenance data:', err);
      setError('No se pudo conectar con el MaintenanceService. Verifica que esté ejecutándose.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
    // Auto-refresh every 15 seconds
    const interval = setInterval(loadData, 15000);
    return () => clearInterval(interval);
  }, [loadData]);

  // Auto-clear success message
  useEffect(() => {
    if (success) {
      const timer = setTimeout(() => setSuccess(null), 4000);
      return () => clearTimeout(timer);
    }
  }, [success]);

  // --- Actions ---
  const handleActivateImmediate = async () => {
    setActionLoading('activate');
    setError(null);
    try {
      await maintenanceService.activateImmediate(immediateMessage);
      setSuccess('Modo mantenimiento activado exitosamente');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error activating maintenance:', err);
      setError('Error al activar el modo mantenimiento. Verifica tu autenticación.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleDeactivate = async () => {
    setActionLoading('deactivate');
    setError(null);
    try {
      await maintenanceService.deactivateImmediate();
      setSuccess('Modo mantenimiento desactivado');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error deactivating maintenance:', err);
      setError('Error al desactivar el modo mantenimiento.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSchedule = async () => {
    if (!schedTitle || !schedStartDate || !schedStartTime || !schedEndDate || !schedEndTime) {
      setError('Completa todos los campos requeridos para programar el mantenimiento.');
      return;
    }

    const startISO = new Date(`${schedStartDate}T${schedStartTime}`).toISOString();
    const endISO = new Date(`${schedEndDate}T${schedEndTime}`).toISOString();

    if (new Date(endISO) <= new Date(startISO)) {
      setError('La fecha de fin debe ser posterior a la fecha de inicio.');
      return;
    }

    setActionLoading('schedule');
    setError(null);
    try {
      await maintenanceService.create({
        title: schedTitle,
        description: schedDescription,
        type: schedType,
        scheduledStart: startISO,
        scheduledEnd: endISO,
        notifyUsers: schedNotify,
        notifyMinutesBefore: parseInt(schedNotifyMinutes) || 30,
        affectedServices: ['all'],
      });
      setSuccess('Mantenimiento programado exitosamente');
      setSchedTitle('');
      setSchedDescription('');
      setSchedStartDate('');
      setSchedStartTime('');
      setSchedEndDate('');
      setSchedEndTime('');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error scheduling maintenance:', err);
      setError('Error al programar el mantenimiento.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleStart = async (id: string) => {
    setActionLoading(`start-${id}`);
    try {
      await maintenanceService.start(id);
      setSuccess('Mantenimiento iniciado');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development') console.error('Error starting maintenance:', err);
      setError('Error al iniciar el mantenimiento.');
    } finally {
      setActionLoading(null);
    }
  };

  const _handleComplete = async (id: string) => {
    setActionLoading(`complete-${id}`);
    try {
      await maintenanceService.complete(id);
      setSuccess('Mantenimiento completado');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error completing maintenance:', err);
      setError('Error al completar el mantenimiento.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleCancel = async (id: string) => {
    if (!cancelReason.trim()) {
      setError('Proporciona una razón para cancelar.');
      return;
    }
    setActionLoading(`cancel-${id}`);
    try {
      await maintenanceService.cancel(id, cancelReason);
      setSuccess('Mantenimiento cancelado');
      setCancellingId(null);
      setCancelReason('');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development')
        console.error('Error cancelling maintenance:', err);
      setError('Error al cancelar el mantenimiento.');
    } finally {
      setActionLoading(null);
    }
  };

  const handleDelete = async (id: string) => {
    setActionLoading(`delete-${id}`);
    try {
      await maintenanceService.delete(id);
      setSuccess('Ventana de mantenimiento eliminada');
      await loadData();
    } catch (err) {
      if (process.env.NODE_ENV === 'development') console.error('Error deleting maintenance:', err);
      setError('Error al eliminar el mantenimiento.');
    } finally {
      setActionLoading(null);
    }
  };

  // --- Derived data ---
  const isMaintenanceActive = status?.isMaintenanceMode ?? false;
  const activeWindow = status?.maintenanceWindow ?? null;
  const scheduledWindows = allWindows.filter(w => w.status === MaintenanceStatus.Scheduled);
  const historyWindows = allWindows
    .filter(
      w => w.status === MaintenanceStatus.Completed || w.status === MaintenanceStatus.Cancelled
    )
    .sort(
      (a, b) =>
        new Date(b.actualEnd || b.updatedAt || b.createdAt).getTime() -
        new Date(a.actualEnd || a.updatedAt || a.createdAt).getTime()
    )
    .slice(0, 10);

  // --- Loading state ---
  if (loading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
        <span className="text-muted-foreground ml-3">Cargando estado de mantenimiento...</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold">Modo Mantenimiento</h1>
          <p className="text-muted-foreground">
            Gestiona el modo de mantenimiento de la plataforma
          </p>
        </div>
      </div>

      {/* Alerts */}
      {error && (
        <div className="flex items-center gap-3 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm">{error}</p>
          <button
            onClick={() => setError(null)}
            className="ml-auto text-red-600 hover:text-red-800"
          >
            ✕
          </button>
        </div>
      )}
      {success && (
        <div className="border-primary bg-primary/10 text-primary flex items-center gap-3 rounded-lg border p-4">
          <CheckCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm">{success}</p>
        </div>
      )}

      {/* Current Status Banner */}
      <Card
        className={
          isMaintenanceActive ? 'border-red-300 bg-red-50' : 'border-primary bg-primary/10'
        }
      >
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div
                className={`rounded-full p-3 ${isMaintenanceActive ? 'bg-red-100' : 'bg-primary/10'}`}
              >
                {isMaintenanceActive ? (
                  <AlertTriangle className="h-8 w-8 text-red-600" />
                ) : (
                  <CheckCircle className="text-primary h-8 w-8" />
                )}
              </div>
              <div>
                <h2
                  className={`text-xl font-bold ${isMaintenanceActive ? 'text-red-900' : 'text-primary'}`}
                >
                  {isMaintenanceActive ? 'Modo Mantenimiento Activo' : 'Plataforma Operativa'}
                </h2>
                <p className={isMaintenanceActive ? 'text-red-700' : 'text-primary'}>
                  {isMaintenanceActive
                    ? 'Los usuarios ven la página de mantenimiento'
                    : 'Todo funciona con normalidad'}
                </p>
                {activeWindow && (
                  <p className="mt-1 text-sm text-red-600">
                    Desde {formatDateTime(activeWindow.actualStart || activeWindow.scheduledStart)}
                    {' · '}
                    {activeWindow.description}
                  </p>
                )}
              </div>
            </div>
            {isMaintenanceActive ? (
              <Button
                variant="outline"
                className="border-red-300 text-red-700 hover:bg-red-100"
                onClick={handleDeactivate}
                disabled={actionLoading === 'deactivate'}
              >
                {actionLoading === 'deactivate' ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Square className="mr-2 h-4 w-4" />
                )}
                Desactivar
              </Button>
            ) : (
              <div className="text-primary flex items-center gap-2 text-sm">
                <Server className="h-4 w-4" />
                Online
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Immediate Maintenance */}
        <Card className={isMaintenanceActive ? 'opacity-60' : ''}>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-600" />
              Mantenimiento Inmediato
            </CardTitle>
            <CardDescription>Activa el modo mantenimiento ahora mismo</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label>Mensaje para usuarios</Label>
              <Textarea
                placeholder="Estamos realizando mejoras en la plataforma. Volvemos pronto..."
                className="mt-2"
                value={immediateMessage}
                onChange={e => setImmediateMessage(e.target.value)}
                disabled={isMaintenanceActive}
              />
            </div>
            <div>
              <Label>Duración estimada (minutos)</Label>
              <Input
                type="number"
                placeholder="60"
                className="mt-2"
                value={immediateDuration}
                onChange={e => setImmediateDuration(e.target.value)}
                disabled={isMaintenanceActive}
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Switch
                  checked={notifyUsers}
                  onCheckedChange={setNotifyUsers}
                  disabled={isMaintenanceActive}
                />
                <Label>Notificar a usuarios activos</Label>
              </div>
            </div>
            <Button
              className={isMaintenanceActive ? '' : 'bg-amber-600 hover:bg-amber-700'}
              disabled={isMaintenanceActive || actionLoading === 'activate'}
              onClick={handleActivateImmediate}
            >
              {actionLoading === 'activate' ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <AlertTriangle className="mr-2 h-4 w-4" />
              )}
              {isMaintenanceActive ? 'Ya está activo' : 'Activar Mantenimiento'}
            </Button>
          </CardContent>
        </Card>

        {/* Scheduled Maintenance */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Calendar className="h-5 w-5 text-blue-600" />
              Programar Mantenimiento
            </CardTitle>
            <CardDescription>Programa una ventana de mantenimiento futura</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label>Título *</Label>
              <Input
                placeholder="Actualización de base de datos"
                className="mt-2"
                value={schedTitle}
                onChange={e => setSchedTitle(e.target.value)}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Fecha inicio *</Label>
                <Input
                  type="date"
                  className="mt-2"
                  value={schedStartDate}
                  onChange={e => setSchedStartDate(e.target.value)}
                />
              </div>
              <div>
                <Label>Hora inicio *</Label>
                <Input
                  type="time"
                  className="mt-2"
                  value={schedStartTime}
                  onChange={e => setSchedStartTime(e.target.value)}
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Fecha fin *</Label>
                <Input
                  type="date"
                  className="mt-2"
                  value={schedEndDate}
                  onChange={e => setSchedEndDate(e.target.value)}
                />
              </div>
              <div>
                <Label>Hora fin *</Label>
                <Input
                  type="time"
                  className="mt-2"
                  value={schedEndTime}
                  onChange={e => setSchedEndTime(e.target.value)}
                />
              </div>
            </div>
            <div>
              <Label>Tipo</Label>
              <select
                className="border-input bg-background mt-2 w-full rounded-md border px-3 py-2 text-sm"
                value={schedType}
                onChange={e => setSchedType(parseInt(e.target.value))}
              >
                <option value={MaintenanceType.Scheduled}>Programado</option>
                <option value={MaintenanceType.Database}>Base de Datos</option>
                <option value={MaintenanceType.Deployment}>Despliegue</option>
                <option value={MaintenanceType.Infrastructure}>Infraestructura</option>
                <option value={MaintenanceType.Other}>Otro</option>
              </select>
            </div>
            <div>
              <Label>Descripción</Label>
              <Textarea
                placeholder="Descripción del mantenimiento programado..."
                className="mt-2"
                value={schedDescription}
                onChange={e => setSchedDescription(e.target.value)}
              />
            </div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <Switch checked={schedNotify} onCheckedChange={setSchedNotify} />
                <Label>Notificar usuarios</Label>
              </div>
              {schedNotify && (
                <div className="flex items-center gap-2">
                  <Input
                    type="number"
                    className="w-20"
                    value={schedNotifyMinutes}
                    onChange={e => setSchedNotifyMinutes(e.target.value)}
                  />
                  <span className="text-muted-foreground text-sm">min antes</span>
                </div>
              )}
            </div>
            <Button
              variant="outline"
              onClick={handleSchedule}
              disabled={actionLoading === 'schedule'}
            >
              {actionLoading === 'schedule' ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Calendar className="mr-2 h-4 w-4" />
              )}
              Programar
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Scheduled Maintenance List */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="h-5 w-5" />
            Mantenimientos Programados
            {scheduledWindows.length > 0 && (
              <Badge variant="outline" className="ml-2">
                {scheduledWindows.length}
              </Badge>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {scheduledWindows.length === 0 ? (
            <div className="text-muted-foreground py-8 text-center">
              <Calendar className="mx-auto mb-4 h-12 w-12 opacity-50" />
              <p>No hay mantenimientos programados</p>
            </div>
          ) : (
            <div className="space-y-3">
              {scheduledWindows.map(window => (
                <div
                  key={window.id}
                  className="flex items-center justify-between rounded-lg border bg-blue-50/50 p-4"
                >
                  <div className="flex items-center gap-3">
                    <div className="rounded-lg bg-blue-100 p-2">
                      <Clock className="h-5 w-5 text-blue-600" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="font-medium">{window.title}</p>
                        {getTypeBadge(window.type)}
                      </div>
                      <p className="text-muted-foreground text-sm">
                        {formatDateTime(window.scheduledStart)} —{' '}
                        {formatDateTime(window.scheduledEnd)}
                        <span className="ml-2 text-xs">
                          ({formatDuration(window.scheduledStart, window.scheduledEnd)})
                        </span>
                      </p>
                      {window.description && (
                        <p className="text-muted-foreground mt-1 text-sm">{window.description}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      className="border-primary text-primary hover:bg-primary/10"
                      onClick={() => handleStart(window.id)}
                      disabled={actionLoading === `start-${window.id}`}
                      title="Iniciar ahora"
                    >
                      {actionLoading === `start-${window.id}` ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Play className="h-4 w-4" />
                      )}
                    </Button>
                    {cancellingId === window.id ? (
                      <div className="flex items-center gap-2">
                        <Input
                          placeholder="Razón de cancelación"
                          className="h-8 w-48 text-sm"
                          value={cancelReason}
                          onChange={e => setCancelReason(e.target.value)}
                        />
                        <Button
                          size="sm"
                          variant="destructive"
                          onClick={() => handleCancel(window.id)}
                          disabled={actionLoading === `cancel-${window.id}`}
                        >
                          {actionLoading === `cancel-${window.id}` ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            'Confirmar'
                          )}
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => {
                            setCancellingId(null);
                            setCancelReason('');
                          }}
                        >
                          ✕
                        </Button>
                      </div>
                    ) : (
                      <Button
                        size="sm"
                        variant="outline"
                        className="border-red-300 text-red-600 hover:bg-red-50"
                        onClick={() => setCancellingId(window.id)}
                        title="Cancelar"
                      >
                        <XCircle className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* History */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Wrench className="h-5 w-5" />
            Historial de Mantenimientos
          </CardTitle>
        </CardHeader>
        <CardContent>
          {historyWindows.length === 0 ? (
            <div className="text-muted-foreground py-8 text-center">
              <Wrench className="mx-auto mb-4 h-12 w-12 opacity-50" />
              <p>No hay mantenimientos anteriores</p>
            </div>
          ) : (
            <div className="space-y-3">
              {historyWindows.map(window => (
                <div
                  key={window.id}
                  className="bg-muted/50 flex items-center justify-between rounded-lg p-4"
                >
                  <div className="flex items-center gap-3">
                    <div
                      className={`rounded-lg p-2 ${
                        window.status === MaintenanceStatus.Completed
                          ? 'bg-primary/10'
                          : 'bg-gray-100'
                      }`}
                    >
                      {window.status === MaintenanceStatus.Completed ? (
                        <CheckCircle className="text-primary h-5 w-5" />
                      ) : (
                        <XCircle className="h-5 w-5 text-gray-500" />
                      )}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="font-medium">{window.title}</p>
                        {getStatusBadge(window.status)}
                        {getTypeBadge(window.type)}
                      </div>
                      <p className="text-muted-foreground text-sm">
                        {formatDateTime(window.actualStart || window.scheduledStart)}
                        {window.actualEnd && (
                          <>
                            {' — '}
                            {formatDateTime(window.actualEnd)}
                            <span className="ml-2 text-xs">
                              (
                              {formatDuration(
                                window.actualStart || window.scheduledStart,
                                window.actualEnd
                              )}
                              )
                            </span>
                          </>
                        )}
                      </p>
                      {window.notes && (
                        <p className="text-muted-foreground mt-1 text-sm italic">{window.notes}</p>
                      )}
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    className="text-muted-foreground hover:text-red-600"
                    onClick={() => handleDelete(window.id)}
                    disabled={actionLoading === `delete-${window.id}`}
                    title="Eliminar"
                  >
                    {actionLoading === `delete-${window.id}` ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <Trash2 className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            Configuración
          </CardTitle>
          <CardDescription>
            Opciones automáticas cuando el modo mantenimiento está activo
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Permitir acceso a admins durante mantenimiento</p>
              <p className="text-muted-foreground text-sm">
                Los administradores pueden acceder normalmente
              </p>
            </div>
            <Switch defaultChecked disabled />
          </div>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Redirección automática</p>
              <p className="text-muted-foreground text-sm">
                Redirige a los usuarios a la página de mantenimiento vía middleware
              </p>
            </div>
            <Switch defaultChecked disabled />
          </div>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Fail-open si el servicio no responde</p>
              <p className="text-muted-foreground text-sm">
                Si MaintenanceService no responde, permite el acceso normal
              </p>
            </div>
            <Switch defaultChecked disabled />
          </div>
          <p className="text-muted-foreground text-xs italic">
            Estas configuraciones están definidas en el middleware del frontend y no se pueden
            cambiar desde aquí.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
