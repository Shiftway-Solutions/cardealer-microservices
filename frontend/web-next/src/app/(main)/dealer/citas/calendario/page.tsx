/**
 * Dealer Calendar View Page
 *
 * Calendar view for appointments and test drives
 * Connected to real APIs - P1 Integration
 */

'use client';

import { useState, useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Plus,
  Calendar,
  Clock,
  User,
  Car,
  Phone,
  MapPin,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import Link from 'next/link';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useDealerAppointments,
  useConfirmAppointment,
  useCancelAppointment,
} from '@/hooks/use-appointments';
import {
  getStatusColor,
  getStatusLabel,
  getAppointmentTypeLabel,
  type Appointment,
  type AppointmentType,
  type AppointmentStatus,
} from '@/services/appointments';
import { toast } from 'sonner';
import { PlanGate } from '@/components/plan/plan-gate';

// ============================================================================
// Skeleton Components
// ============================================================================

function CalendarSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-32" />
      </div>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardContent className="pt-6">
            <div className="grid grid-cols-7 gap-2">
              {Array.from({ length: 35 }).map((_, i) => (
                <Skeleton key={i} className="aspect-square rounded-lg" />
              ))}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="space-y-4">
              {[1, 2, 3].map(i => (
                <Skeleton key={i} className="h-32 rounded-lg" />
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// ============================================================================
// Calendar Helpers
// ============================================================================

const weekDays = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];

function getCalendarDays(year: number, month: number) {
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const daysInMonth = lastDay.getDate();

  // Monday = 0, Sunday = 6 (ISO week)
  let startDow = firstDay.getDay() - 1;
  if (startDow < 0) startDow = 6;

  const days: Array<{ day: number | null; date: string | null }> = [];

  for (let i = 0; i < startDow; i++) {
    days.push({ day: null, date: null });
  }

  for (let d = 1; d <= daysInMonth; d++) {
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
    days.push({ day: d, date: dateStr });
  }

  return days;
}

function getMonthLabel(year: number, month: number): string {
  const date = new Date(year, month, 1);
  return date.toLocaleDateString('es-DO', { month: 'long', year: 'numeric' });
}

// ============================================================================
// Main Page Component
// ============================================================================

export default function CalendarPage() {
  const today = new Date();
  const [currentYear, setCurrentYear] = useState(today.getFullYear());
  const [currentMonth, setCurrentMonth] = useState(today.getMonth());
  const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
  const [selectedDate, setSelectedDate] = useState(todayStr);

  // Get dealer data
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  // Get ALL appointments for this month range
  const monthStart = `${currentYear}-${String(currentMonth + 1).padStart(2, '0')}-01`;
  const monthEnd = `${currentYear}-${String(currentMonth + 1).padStart(2, '0')}-${new Date(currentYear, currentMonth + 1, 0).getDate()}`;

  const {
    data: appointments,
    isLoading: appointmentsLoading,
    error: appointmentsError,
    refetch,
  } = useDealerAppointments(dealerId, {
    fromDate: monthStart,
    toDate: monthEnd,
  });

  // Mutations
  const confirmAppointment = useConfirmAppointment();
  const cancelAppointment = useCancelAppointment();

  const isLoading = dealerLoading || appointmentsLoading;

  // Calendar days for current month
  const calendarDays = useMemo(
    () => getCalendarDays(currentYear, currentMonth),
    [currentYear, currentMonth]
  );

  // Map dates to appointment counts
  const appointmentsByDate = useMemo(() => {
    const map = new Map<string, Appointment[]>();
    appointments?.forEach(apt => {
      const dateKey = apt.scheduledDate;
      if (!map.has(dateKey)) map.set(dateKey, []);
      map.get(dateKey)!.push(apt);
    });
    return map;
  }, [appointments]);

  // Appointments for selected date
  const selectedAppointments = useMemo(
    () => appointmentsByDate.get(selectedDate) || [],
    [appointmentsByDate, selectedDate]
  );

  // Navigation
  const goToPrevMonth = () => {
    if (currentMonth === 0) {
      setCurrentMonth(11);
      setCurrentYear(y => y - 1);
    } else {
      setCurrentMonth(m => m - 1);
    }
  };

  const goToNextMonth = () => {
    if (currentMonth === 11) {
      setCurrentMonth(0);
      setCurrentYear(y => y + 1);
    } else {
      setCurrentMonth(m => m + 1);
    }
  };

  const goToToday = () => {
    setCurrentYear(today.getFullYear());
    setCurrentMonth(today.getMonth());
    setSelectedDate(todayStr);
  };

  // Actions
  const handleConfirm = async (id: string) => {
    try {
      await confirmAppointment.mutateAsync(id);
      toast.success('Cita confirmada');
    } catch {
      toast.error('Error al confirmar la cita');
    }
  };

  const handleCancel = async (id: string) => {
    try {
      await cancelAppointment.mutateAsync({ id });
      toast.success('Cita cancelada');
    } catch {
      toast.error('Error al cancelar la cita');
    }
  };

  const getTypeBadge = (type: string) => {
    const label = getAppointmentTypeLabel(type as AppointmentType);
    switch (type) {
      case 'TestDrive':
        return <Badge className="bg-blue-100 text-blue-700">{label}</Badge>;
      case 'PropertyTour':
      case 'Consultation':
        return <Badge className="bg-purple-100 text-purple-700">{label}</Badge>;
      case 'Inspection':
        return <Badge className="bg-orange-100 text-orange-700">{label}</Badge>;
      case 'Meeting':
        return <Badge className="bg-indigo-100 text-indigo-700">{label}</Badge>;
      default:
        return <Badge variant="outline">{label}</Badge>;
    }
  };

  const getStatusBadge = (status: string) => {
    const label = getStatusLabel(status as AppointmentStatus);
    const color = getStatusColor(status as AppointmentStatus);
    return <Badge className={color}>{label}</Badge>;
  };

  // Error state
  if (appointmentsError) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Link href="/dealer/citas">
            <Button variant="ghost" size="icon" aria-label="Volver">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <h1 className="text-2xl font-bold">Calendario</h1>
        </div>
        <Card>
          <CardContent className="py-12 text-center">
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-red-400" />
            <p className="mb-2 font-medium text-red-600">Error al cargar las citas</p>
            <p className="text-muted-foreground mb-4 text-sm">
              No se pudieron obtener las citas del calendario.
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

  if (isLoading) {
    return <CalendarSkeleton />;
  }

  return (
    <PlanGate feature="autoScheduling">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/dealer/citas">
              <Button variant="ghost" size="icon" aria-label="Volver">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <div>
              <h1 className="text-2xl font-bold">Calendario</h1>
              <p className="text-muted-foreground">Vista de calendario para citas</p>
            </div>
          </div>
          <Button className="bg-primary hover:bg-primary/90">
            <Plus className="mr-2 h-4 w-4" />
            Nueva Cita
          </Button>
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Calendar */}
          <Card className="lg:col-span-2">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="flex items-center gap-2 capitalize">
                <Calendar className="h-5 w-5" />
                {getMonthLabel(currentYear, currentMonth)}
              </CardTitle>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  onClick={goToPrevMonth}
                  aria-label="Mes anterior"
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button variant="outline" size="sm" onClick={goToToday}>
                  Hoy
                </Button>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={goToNextMonth}
                  aria-label="Mes siguiente"
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {/* Week Days Header */}
              <div className="mb-2 grid grid-cols-7 gap-1">
                {weekDays.map(day => (
                  <div
                    key={day}
                    className="text-muted-foreground py-2 text-center text-sm font-medium"
                  >
                    {day}
                  </div>
                ))}
              </div>

              {/* Calendar Grid */}
              <div className="grid grid-cols-7 gap-1">
                {calendarDays.map((item, index) => {
                  const aptCount = item.date ? appointmentsByDate.get(item.date)?.length || 0 : 0;
                  const isToday = item.date === todayStr;
                  const isSelected = item.date === selectedDate;

                  return (
                    <button
                      key={index}
                      className={`relative aspect-square rounded-lg p-2 text-sm transition-colors ${
                        item.day ? 'hover:bg-muted' : 'text-gray-300'
                      } ${isToday ? 'bg-primary/10 font-bold' : ''} ${
                        isSelected ? 'ring-primary ring-2' : ''
                      }`}
                      onClick={() => item.date && setSelectedDate(item.date)}
                      disabled={!item.day}
                    >
                      {item.day}
                      {aptCount > 0 && (
                        <span className="absolute bottom-1 left-1/2 flex -translate-x-1/2 items-center gap-0.5">
                          {aptCount <= 3 ? (
                            Array.from({ length: aptCount }).map((_, i) => (
                              <span key={i} className="bg-primary/100 h-1.5 w-1.5 rounded-full" />
                            ))
                          ) : (
                            <span className="text-primary text-xs font-medium">{aptCount}</span>
                          )}
                        </span>
                      )}
                    </button>
                  );
                })}
              </div>

              {/* Legend */}
              <div className="text-muted-foreground mt-4 flex items-center gap-4 text-xs">
                <div className="flex items-center gap-1">
                  <span className="bg-primary/100 h-2 w-2 rounded-full" />
                  Citas programadas
                </div>
                <div className="flex items-center gap-1">
                  <span className="bg-primary/10 h-4 w-4 rounded" />
                  Hoy
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Day Details */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base capitalize">
                {new Date(selectedDate + 'T12:00:00').toLocaleDateString('es-DO', {
                  weekday: 'long',
                  day: 'numeric',
                  month: 'long',
                })}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {selectedAppointments.length > 0 ? (
                <div className="space-y-4">
                  {selectedAppointments.map(apt => (
                    <div key={apt.id} className="border-border bg-muted/50 rounded-lg border p-4">
                      <div className="mb-3 flex items-start justify-between">
                        <div className="flex items-center gap-2">
                          <Clock className="text-muted-foreground h-4 w-4" />
                          <span className="font-medium">{apt.scheduledTime}</span>
                        </div>
                        {getStatusBadge(apt.status)}
                      </div>

                      <div className="space-y-2 text-sm">
                        <div className="flex items-center gap-2">
                          <User className="text-muted-foreground h-4 w-4" />
                          <span>{apt.clientName}</span>
                        </div>
                        {apt.clientPhone && (
                          <div className="flex items-center gap-2">
                            <Phone className="text-muted-foreground h-4 w-4" />
                            <span>{apt.clientPhone}</span>
                          </div>
                        )}
                        {apt.relatedEntityDescription && (
                          <div className="flex items-center gap-2">
                            <Car className="text-muted-foreground h-4 w-4" />
                            <span>{apt.relatedEntityDescription}</span>
                          </div>
                        )}
                        {apt.location && (
                          <div className="flex items-center gap-2">
                            <MapPin className="text-muted-foreground h-4 w-4" />
                            <span>{apt.location}</span>
                          </div>
                        )}
                      </div>

                      <div className="border-border mt-3 flex items-center justify-between border-t pt-3">
                        {getTypeBadge(apt.type)}
                        <div className="flex gap-2">
                          {apt.status === 'Scheduled' && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleConfirm(apt.id)}
                              disabled={confirmAppointment.isPending}
                            >
                              Confirmar
                            </Button>
                          )}
                          {(apt.status === 'Scheduled' || apt.status === 'Confirmed') && (
                            <Button
                              variant="outline"
                              size="sm"
                              className="text-red-600 hover:text-red-700"
                              onClick={() => handleCancel(apt.id)}
                              disabled={cancelAppointment.isPending}
                            >
                              Cancelar
                            </Button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-muted-foreground py-8 text-center">
                  <Calendar className="mx-auto mb-4 h-12 w-12 opacity-50" />
                  <p className="font-medium">Sin citas programadas</p>
                  <p className="text-sm">Selecciona otro día o crea una nueva cita</p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Upcoming Appointments */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Próximas Citas</CardTitle>
              <Button variant="ghost" size="sm" onClick={() => refetch()}>
                <RefreshCw className="mr-2 h-4 w-4" />
                Actualizar
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {appointments && appointments.length > 0 ? (
              <div className="space-y-4">
                {appointments
                  .filter(apt => {
                    const aptDate = new Date(`${apt.scheduledDate}T${apt.scheduledTime}`);
                    return (
                      aptDate >= today && (apt.status === 'Scheduled' || apt.status === 'Confirmed')
                    );
                  })
                  .sort((a, b) => {
                    const dateA = new Date(`${a.scheduledDate}T${a.scheduledTime}`);
                    const dateB = new Date(`${b.scheduledDate}T${b.scheduledTime}`);
                    return dateA.getTime() - dateB.getTime();
                  })
                  .slice(0, 10)
                  .map(apt => (
                    <div
                      key={apt.id}
                      className="bg-muted/50 hover:bg-muted flex cursor-pointer items-center justify-between rounded-lg p-4 transition-colors"
                      onClick={() => setSelectedDate(apt.scheduledDate)}
                    >
                      <div className="flex items-center gap-4">
                        <div className="text-center">
                          <p className="text-2xl font-bold">
                            {new Date(apt.scheduledDate + 'T12:00:00').getDate()}
                          </p>
                          <p className="text-muted-foreground text-xs">
                            {new Date(apt.scheduledDate + 'T12:00:00').toLocaleDateString('es-DO', {
                              month: 'short',
                            })}
                          </p>
                        </div>
                        <div>
                          <div className="flex items-center gap-2">
                            <p className="font-medium">{apt.clientName}</p>
                            {getTypeBadge(apt.type)}
                          </div>
                          {apt.relatedEntityDescription && (
                            <p className="text-muted-foreground text-sm">
                              {apt.relatedEntityDescription}
                            </p>
                          )}
                          <p className="text-muted-foreground text-xs">
                            {apt.scheduledTime} • {apt.location}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">{getStatusBadge(apt.status)}</div>
                    </div>
                  ))}
              </div>
            ) : (
              <div className="text-muted-foreground py-8 text-center">
                <Calendar className="mx-auto mb-4 h-12 w-12 opacity-50" />
                <p className="font-medium">Sin citas próximas</p>
                <p className="text-sm">No hay citas programadas para este mes</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </PlanGate>
  );
}
