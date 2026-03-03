/**
 * Dealer Appointments Page
 *
 * Manage test drive appointments and meetings
 * Connected to real APIs - January 2026
 */

'use client';

import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Calendar,
  Car,
  Phone,
  Mail,
  Check,
  X,
  ChevronLeft,
  ChevronRight,
  Plus,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useDealerAppointments,
  useConfirmAppointment,
  useCancelAppointment,
  useCompleteAppointment,
} from '@/hooks/use-appointments';
import {
  getStatusColor,
  getStatusLabel,
  getAppointmentTypeLabel,
  getAppointmentTypeIcon,
  formatAppointmentTime,
  calculateAppointmentStats,
  type AppointmentStatus,
  type AppointmentType,
} from '@/services/appointments';
import { toast } from 'sonner';

// ============================================================================
// Skeleton Components
// ============================================================================

function StatsSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {[1, 2, 3, 4].map(i => (
        <Card key={i}>
          <CardContent className="p-4 text-center">
            <Skeleton className="mx-auto h-8 w-12" />
            <Skeleton className="mx-auto mt-2 h-4 w-24" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function AppointmentsSkeleton() {
  return (
    <div className="space-y-4">
      {[1, 2, 3].map(i => (
        <Card key={i}>
          <CardContent className="p-4">
            <div className="flex gap-4">
              <Skeleton className="h-12 w-16" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-40" />
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-4 w-48" />
              </div>
              <Skeleton className="h-8 w-24" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

// ============================================================================
// Helper Components
// ============================================================================

const getStatusBadge = (status: AppointmentStatus) => {
  const color = getStatusColor(status);
  const label = getStatusLabel(status);
  return <Badge className={color}>{label}</Badge>;
};

const getTypeBadge = (type: AppointmentType) => {
  const label = getAppointmentTypeLabel(type);
  const icon = getAppointmentTypeIcon(type);

  const colorClass =
    {
      TestDrive: 'border-primary text-primary',
      PropertyTour: 'border-blue-500 text-blue-700',
      Consultation: 'border-purple-500 text-purple-700',
      Inspection: 'border-amber-500 text-amber-700',
      Meeting: 'border-gray-500 text-foreground',
    }[type] || '';

  return (
    <Badge variant="outline" className={colorClass}>
      {icon} {label}
    </Badge>
  );
};

// ============================================================================
// Calendar Helpers
// ============================================================================

const calendarDays = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];

function getMonthName(date: Date): string {
  return date.toLocaleDateString('es-DO', { month: 'long', year: 'numeric' });
}

function getDaysInMonth(year: number, month: number): number {
  return new Date(year, month + 1, 0).getDate();
}

function getFirstDayOfMonth(year: number, month: number): number {
  return new Date(year, month, 1).getDay();
}

// ============================================================================
// Main Component
// ============================================================================

export default function DealerAppointmentsPage() {
  const today = new Date();
  const [currentMonth, setCurrentMonth] = React.useState(today.getMonth());
  const [currentYear, setCurrentYear] = React.useState(today.getFullYear());
  const [selectedDate, setSelectedDate] = React.useState(today.toISOString().split('T')[0]);

  // Get current dealer
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();

  // Get all dealer appointments for stats
  const {
    data: allAppointments,
    isLoading: isAppointmentsLoading,
    error: appointmentsError,
    refetch,
  } = useDealerAppointments(dealer?.id || '');

  // Mutations
  const confirmMutation = useConfirmAppointment();
  const cancelMutation = useCancelAppointment();
  const completeMutation = useCompleteAppointment();

  // Filter appointments for selected date
  const selectedDateAppointments = React.useMemo(() => {
    if (!allAppointments) return [];
    return allAppointments.filter(a => a.scheduledDate === selectedDate);
  }, [allAppointments, selectedDate]);

  // Calculate stats
  const stats = React.useMemo(() => {
    if (!allAppointments) return { today: 0, thisWeek: 0, pending: 0, completedThisMonth: 0 };
    return calculateAppointmentStats(allAppointments);
  }, [allAppointments]);

  // Get dates with appointments for calendar dots
  const datesWithAppointments = React.useMemo(() => {
    if (!allAppointments) return new Set<string>();
    return new Set(allAppointments.map(a => a.scheduledDate));
  }, [allAppointments]);

  // Calendar navigation
  const goToPrevMonth = () => {
    if (currentMonth === 0) {
      setCurrentMonth(11);
      setCurrentYear(currentYear - 1);
    } else {
      setCurrentMonth(currentMonth - 1);
    }
  };

  const goToNextMonth = () => {
    if (currentMonth === 11) {
      setCurrentMonth(0);
      setCurrentYear(currentYear + 1);
    } else {
      setCurrentMonth(currentMonth + 1);
    }
  };

  // Appointment actions
  const handleConfirm = async (id: string) => {
    try {
      await confirmMutation.mutateAsync(id);
      toast.success('Cita confirmada');
      refetch();
    } catch {
      toast.error('Error al confirmar la cita');
    }
  };

  const handleCancel = async (id: string) => {
    if (!confirm('¿Estás seguro de cancelar esta cita?')) return;
    try {
      await cancelMutation.mutateAsync({ id, reason: 'Cancelada por el dealer' });
      toast.success('Cita cancelada');
      refetch();
    } catch {
      toast.error('Error al cancelar la cita');
    }
  };

  const handleComplete = async (id: string) => {
    try {
      await completeMutation.mutateAsync({ id });
      toast.success('Cita marcada como completada');
      refetch();
    } catch {
      toast.error('Error al completar la cita');
    }
  };

  // Calendar rendering
  const daysInMonth = getDaysInMonth(currentYear, currentMonth);
  const firstDayOfMonth = getFirstDayOfMonth(currentYear, currentMonth);
  const monthName = getMonthName(new Date(currentYear, currentMonth));

  // Loading state
  if (isDealerLoading || isAppointmentsLoading) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Citas</h1>
            <p className="text-muted-foreground">Gestiona tus citas y test drives</p>
          </div>
        </div>
        <StatsSkeleton />
        <div className="grid gap-6 lg:grid-cols-3">
          <Card>
            <CardContent className="p-4">
              <Skeleton className="h-64 w-full" />
            </CardContent>
          </Card>
          <div className="lg:col-span-2">
            <AppointmentsSkeleton />
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (appointmentsError) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Citas</h1>
          <p className="text-muted-foreground">Gestiona tus citas y test drives</p>
        </div>
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex flex-col items-center justify-center gap-4 p-8">
            <AlertCircle className="h-12 w-12 text-red-500" />
            <p className="text-center text-red-700">
              Error al cargar las citas. Por favor intenta de nuevo.
            </p>
            <Button variant="outline" onClick={() => refetch()}>
              <RefreshCw className="mr-2 h-4 w-4" />
              Reintentar
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const statsCards = [
    { label: 'Hoy', value: stats.today },
    { label: 'Esta Semana', value: stats.thisWeek },
    { label: 'Pendientes', value: stats.pending },
    { label: 'Completadas (Mes)', value: stats.completedThisMonth },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div className="flex items-center gap-3">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Citas</h1>
            <p className="text-muted-foreground">Gestiona tus citas y test drives</p>
          </div>
          <Button variant="ghost" size="icon" onClick={() => refetch()} title="Actualizar">
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
        <Button className="bg-primary hover:bg-primary/90">
          <Plus className="mr-2 h-4 w-4" />
          Nueva Cita
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {statsCards.map(stat => (
          <Card key={stat.label}>
            <CardContent className="p-4 text-center">
              <p className="text-3xl font-bold text-primary">{stat.value}</p>
              <p className="text-sm text-muted-foreground">{stat.label}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Calendar */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-lg capitalize">{monthName}</CardTitle>
            <div className="flex gap-1">
              <Button variant="ghost" size="icon" onClick={goToPrevMonth}>
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="ghost" size="icon" onClick={goToNextMonth}>
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {/* Calendar Header */}
            <div className="mb-2 grid grid-cols-7 gap-1">
              {calendarDays.map(day => (
                <div key={day} className="py-1 text-center text-xs text-muted-foreground">
                  {day}
                </div>
              ))}
            </div>
            {/* Calendar Days */}
            <div className="grid grid-cols-7 gap-1">
              {/* Empty cells for days before first of month */}
              {[...Array(firstDayOfMonth)].map((_, i) => (
                <div key={`empty-${i}`} className="aspect-square" />
              ))}
              {/* Actual days */}
              {[...Array(daysInMonth)].map((_, i) => {
                const day = i + 1;
                const dateStr = `${currentYear}-${String(currentMonth + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
                const isSelected = dateStr === selectedDate;
                const hasAppointments = datesWithAppointments.has(dateStr);
                const isToday = dateStr === today.toISOString().split('T')[0];

                return (
                  <button
                    key={day}
                    onClick={() => setSelectedDate(dateStr)}
                    className={`relative flex aspect-square flex-col items-center justify-center rounded-lg text-sm ${
                      isSelected
                        ? 'bg-primary text-white'
                        : isToday
                          ? 'bg-primary/10 font-semibold text-primary'
                          : 'hover:bg-muted'
                    }`}
                  >
                    {day}
                    {hasAppointments && !isSelected && (
                      <div className="absolute bottom-1 h-1 w-1 rounded-full bg-primary" />
                    )}
                  </button>
                );
              })}
            </div>
          </CardContent>
        </Card>

        {/* Appointments List */}
        <div className="space-y-4 lg:col-span-2">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">
              Citas del{' '}
              {new Date(selectedDate + 'T12:00:00').toLocaleDateString('es-DO', {
                weekday: 'long',
                day: 'numeric',
                month: 'long',
              })}
            </h2>
            <Badge variant="secondary">{selectedDateAppointments.length} citas</Badge>
          </div>

          {selectedDateAppointments.length > 0 ? (
            <div className="space-y-4">
              {selectedDateAppointments.map(appointment => (
                <Card key={appointment.id}>
                  <CardContent className="p-4">
                    <div className="flex items-start justify-between">
                      <div className="flex gap-4">
                        {/* Time */}
                        <div className="text-center">
                          <p className="text-lg font-bold">
                            {formatAppointmentTime(
                              appointment.scheduledDate,
                              appointment.scheduledTime
                            )}
                          </p>
                          <div className="mt-1 flex gap-1">{getTypeBadge(appointment.type)}</div>
                        </div>

                        {/* Details */}
                        <div>
                          <div className="mb-2 flex items-center gap-2">
                            <h3 className="font-semibold">{appointment.clientName}</h3>
                            {getStatusBadge(appointment.status)}
                          </div>

                          <div className="space-y-1 text-sm text-muted-foreground">
                            {appointment.relatedEntityDescription && (
                              <p className="flex items-center gap-2">
                                <Car className="h-4 w-4" />
                                {appointment.relatedEntityDescription}
                              </p>
                            )}
                            {appointment.clientPhone && (
                              <p className="flex items-center gap-2">
                                <Phone className="h-4 w-4" />
                                <a
                                  href={`tel:${appointment.clientPhone}`}
                                  className="hover:underline"
                                >
                                  {appointment.clientPhone}
                                </a>
                              </p>
                            )}
                            <p className="flex items-center gap-2">
                              <Mail className="h-4 w-4" />
                              <a
                                href={`mailto:${appointment.clientEmail}`}
                                className="hover:underline"
                              >
                                {appointment.clientEmail}
                              </a>
                            </p>
                          </div>

                          {appointment.clientNotes && (
                            <p className="mt-2 text-sm text-muted-foreground italic">
                              &ldquo;{appointment.clientNotes}&rdquo;
                            </p>
                          )}
                        </div>
                      </div>

                      {/* Actions */}
                      <div className="flex flex-col gap-2">
                        {appointment.status === 'Scheduled' && (
                          <>
                            <Button
                              size="sm"
                              className="bg-primary hover:bg-primary/90"
                              onClick={() => handleConfirm(appointment.id)}
                              disabled={confirmMutation.isPending}
                            >
                              <Check className="mr-1 h-4 w-4" />
                              Confirmar
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              className="text-red-600"
                              onClick={() => handleCancel(appointment.id)}
                              disabled={cancelMutation.isPending}
                            >
                              <X className="mr-1 h-4 w-4" />
                              Cancelar
                            </Button>
                          </>
                        )}
                        {appointment.status === 'Confirmed' && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleComplete(appointment.id)}
                            disabled={completeMutation.isPending}
                          >
                            Completar
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <Card>
              <CardContent className="flex flex-col items-center justify-center py-12 text-center">
                <Calendar className="mb-4 h-12 w-12 text-gray-300" />
                <p className="text-muted-foreground">No hay citas para este día</p>
                <Button variant="link" className="mt-2">
                  Programar una cita
                </Button>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
