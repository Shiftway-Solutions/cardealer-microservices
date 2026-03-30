/**
 * Appointment Calendar Component
 * Public-facing booking calendar for dealer test drives / visits
 *
 * Features:
 * - Month-view calendar for date selection
 * - Available time slots for selected date
 * - Booking form with react-hook-form + Zod validation
 * - Uses useAvailability + useCreateAppointment hooks
 */

'use client';

import * as React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import {
  Calendar as CalendarIcon,
  Clock,
  ChevronLeft,
  ChevronRight,
  Check,
  Loader2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAvailability, useCreateAppointment } from '@/hooks/use-appointments';
import { useAuth } from '@/hooks/use-auth';

// =============================================================================
// SCHEMA
// =============================================================================

const bookingSchema = z.object({
  clientName: z.string().min(2, 'Nombre requerido').max(100),
  clientEmail: z.string().email('Email inválido'),
  clientPhone: z.string().optional(),
  clientNotes: z.string().max(500).optional(),
});

type BookingFormData = z.infer<typeof bookingSchema>;

// =============================================================================
// TYPES
// =============================================================================

interface AppointmentCalendarProps {
  providerId: string;
  providerName?: string;
  vehicleId?: string;
  vehicleTitle?: string;
  location?: string;
  className?: string;
}

// =============================================================================
// HELPERS
// =============================================================================

const DAYS_ES = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
const MONTHS_ES = [
  'Enero',
  'Febrero',
  'Marzo',
  'Abril',
  'Mayo',
  'Junio',
  'Julio',
  'Agosto',
  'Septiembre',
  'Octubre',
  'Noviembre',
  'Diciembre',
];

function getDaysInMonth(year: number, month: number): number {
  return new Date(year, month + 1, 0).getDate();
}

function getFirstDayOfMonth(year: number, month: number): number {
  return new Date(year, month, 1).getDay();
}

function formatDateISO(year: number, month: number, day: number): string {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

function isToday(year: number, month: number, day: number): boolean {
  const today = new Date();
  return today.getFullYear() === year && today.getMonth() === month && today.getDate() === day;
}

function isPast(year: number, month: number, day: number): boolean {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const date = new Date(year, month, day);
  return date < today;
}

// =============================================================================
// COMPONENT
// =============================================================================

export function AppointmentCalendar({
  providerId,
  providerName,
  vehicleId,
  vehicleTitle,
  location = '',
  className = '',
}: AppointmentCalendarProps) {
  const { user } = useAuth();
  const createAppointment = useCreateAppointment();

  // Calendar state
  const today = new Date();
  const [currentMonth, setCurrentMonth] = React.useState(today.getMonth());
  const [currentYear, setCurrentYear] = React.useState(today.getFullYear());
  const [selectedDate, setSelectedDate] = React.useState<string | null>(null);
  const [selectedTime, setSelectedTime] = React.useState<string | null>(null);
  const [step, setStep] = React.useState<'date' | 'time' | 'form' | 'success'>('date');

  // Availability query
  const { data: availability, isLoading: loadingSlots } = useAvailability(
    providerId,
    selectedDate || ''
  );

  const availableSlots = availability?.availableSlots?.filter(s => s.available) ?? [];

  // Booking form
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BookingFormData>({
    resolver: zodResolver(bookingSchema),
    defaultValues: {
      clientName: user ? `${user.firstName || ''} ${user.lastName || ''}`.trim() : '',
      clientEmail: user?.email || '',
      clientPhone: user?.phone || '',
      clientNotes: '',
    },
  });

  // Navigation
  const prevMonth = () => {
    if (currentMonth === 0) {
      setCurrentMonth(11);
      setCurrentYear(y => y - 1);
    } else {
      setCurrentMonth(m => m - 1);
    }
  };

  const nextMonth = () => {
    if (currentMonth === 11) {
      setCurrentMonth(0);
      setCurrentYear(y => y + 1);
    } else {
      setCurrentMonth(m => m + 1);
    }
  };

  // Date selection
  const handleDateClick = (day: number) => {
    if (isPast(currentYear, currentMonth, day)) return;
    const dateStr = formatDateISO(currentYear, currentMonth, day);
    setSelectedDate(dateStr);
    setSelectedTime(null);
    setStep('time');
  };

  // Time selection
  const handleTimeClick = (time: string) => {
    setSelectedTime(time);
    setStep('form');
  };

  // Submit booking
  const onSubmit = async (data: BookingFormData) => {
    if (!selectedDate || !selectedTime) return;

    try {
      await createAppointment.mutateAsync({
        type: 'TestDrive',
        relatedEntityId: vehicleId,
        relatedEntityDescription: vehicleTitle,
        agentOrSellerId: providerId,
        clientName: data.clientName,
        clientEmail: data.clientEmail,
        clientPhone: data.clientPhone,
        scheduledDate: selectedDate,
        scheduledTime: selectedTime,
        durationMinutes: 30,
        location: location || 'Por definir',
        clientNotes: data.clientNotes,
      });

      setStep('success');
      toast.success('¡Cita agendada!', {
        description: `Tu visita está programada para el ${new Date(selectedDate).toLocaleDateString('es-DO', { weekday: 'long', month: 'long', day: 'numeric' })} a las ${selectedTime}.`,
      });
    } catch (error: unknown) {
      const err = error as { message?: string };
      toast.error('Error al agendar', {
        description: err.message || 'No se pudo completar la reservación.',
      });
    }
  };

  // Reset
  const handleReset = () => {
    setSelectedDate(null);
    setSelectedTime(null);
    setStep('date');
    reset();
  };

  // Render calendar grid
  const daysInMonth = getDaysInMonth(currentYear, currentMonth);
  const firstDay = getFirstDayOfMonth(currentYear, currentMonth);
  const canGoPrev =
    currentYear > today.getFullYear() ||
    (currentYear === today.getFullYear() && currentMonth > today.getMonth());

  return (
    <Card className={className}>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-lg">
          <CalendarIcon className="text-primary h-5 w-5" />
          Agendar Visita
        </CardTitle>
        {providerName && <p className="text-muted-foreground text-sm">{providerName}</p>}
      </CardHeader>

      <CardContent>
        {/* ── Step 1: Date Selection ── */}
        {step === 'date' && (
          <div>
            {/* Month navigation */}
            <div className="mb-4 flex items-center justify-between">
              <Button
                variant="ghost"
                size="sm"
                onClick={prevMonth}
                disabled={!canGoPrev}
                className="h-8 w-8 p-0"
                aria-label="Mes anterior"
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <span className="text-sm font-semibold">
                {MONTHS_ES[currentMonth]} {currentYear}
              </span>
              <Button
                variant="ghost"
                size="sm"
                onClick={nextMonth}
                className="h-8 w-8 p-0"
                aria-label="Mes siguiente"
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>

            {/* Day headers */}
            <div className="mb-1 grid grid-cols-7 gap-1">
              {DAYS_ES.map(d => (
                <div key={d} className="text-muted-foreground text-center text-xs font-medium">
                  {d}
                </div>
              ))}
            </div>

            {/* Calendar cells */}
            <div className="grid grid-cols-7 gap-1">
              {/* Empty cells before first day */}
              {Array.from({ length: firstDay }).map((_, i) => (
                <div key={`empty-${i}`} className="h-9" />
              ))}

              {/* Day cells */}
              {Array.from({ length: daysInMonth }).map((_, i) => {
                const day = i + 1;
                const past = isPast(currentYear, currentMonth, day);
                const todayMark = isToday(currentYear, currentMonth, day);
                const dateStr = formatDateISO(currentYear, currentMonth, day);
                const isSelected = selectedDate === dateStr;

                return (
                  <button
                    key={day}
                    onClick={() => handleDateClick(day)}
                    disabled={past}
                    className={`flex h-9 items-center justify-center rounded-md text-sm transition-colors ${
                      past
                        ? 'text-muted-foreground/40 cursor-not-allowed'
                        : isSelected
                          ? 'bg-primary text-primary-foreground font-semibold'
                          : todayMark
                            ? 'bg-primary/10 text-primary hover:bg-primary/20 font-semibold'
                            : 'hover:bg-muted'
                    }`}
                  >
                    {day}
                  </button>
                );
              })}
            </div>

            <p className="text-muted-foreground mt-3 text-center text-xs">
              Selecciona una fecha para ver horarios disponibles
            </p>
          </div>
        )}

        {/* ── Step 2: Time Selection ── */}
        {step === 'time' && (
          <div>
            <button
              onClick={() => setStep('date')}
              className="text-primary mb-3 flex items-center gap-1 text-sm hover:underline"
            >
              <ChevronLeft className="h-3 w-3" />
              Cambiar fecha
            </button>

            <p className="mb-3 text-sm font-medium">
              📅{' '}
              {selectedDate &&
                new Date(selectedDate + 'T12:00:00').toLocaleDateString('es-DO', {
                  weekday: 'long',
                  month: 'long',
                  day: 'numeric',
                })}
            </p>

            {loadingSlots ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="text-primary h-6 w-6 animate-spin" />
                <span className="text-muted-foreground ml-2 text-sm">Cargando horarios...</span>
              </div>
            ) : availableSlots.length > 0 ? (
              <div className="grid grid-cols-3 gap-2">
                {availableSlots.map(slot => (
                  <button
                    key={slot.time}
                    onClick={() => handleTimeClick(slot.time)}
                    className={`flex items-center justify-center gap-1 rounded-lg border px-2 py-2 text-sm transition-colors ${
                      selectedTime === slot.time
                        ? 'border-primary bg-primary/10 text-primary font-semibold'
                        : 'border-border hover:border-primary/50 hover:bg-muted'
                    }`}
                  >
                    <Clock className="h-3 w-3" />
                    {slot.time}
                  </button>
                ))}
              </div>
            ) : (
              <div className="py-8 text-center">
                <Clock className="text-muted-foreground/40 mx-auto mb-2 h-8 w-8" />
                <p className="text-muted-foreground text-sm">
                  No hay horarios disponibles para esta fecha.
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setStep('date')}
                  className="mt-3"
                >
                  Seleccionar otra fecha
                </Button>
              </div>
            )}
          </div>
        )}

        {/* ── Step 3: Booking Form ── */}
        {step === 'form' && (
          <div>
            <button
              onClick={() => setStep('time')}
              className="text-primary mb-3 flex items-center gap-1 text-sm hover:underline"
            >
              <ChevronLeft className="h-3 w-3" />
              Cambiar horario
            </button>

            <div className="bg-muted/50 mb-4 rounded-lg p-3 text-sm">
              <p className="font-medium">
                📅{' '}
                {selectedDate &&
                  new Date(selectedDate + 'T12:00:00').toLocaleDateString('es-DO', {
                    weekday: 'long',
                    month: 'long',
                    day: 'numeric',
                  })}
              </p>
              <p className="text-muted-foreground">🕐 {selectedTime}</p>
              {vehicleTitle && <p className="text-muted-foreground mt-1">🚗 {vehicleTitle}</p>}
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
              <div>
                <Label htmlFor="booking-name" className="text-xs">
                  Nombre completo *
                </Label>
                <Input
                  id="booking-name"
                  {...register('clientName')}
                  className="mt-1"
                  placeholder="Tu nombre"
                />
                {errors.clientName && (
                  <p className="mt-0.5 text-xs text-red-500">{errors.clientName.message}</p>
                )}
              </div>

              <div>
                <Label htmlFor="booking-email" className="text-xs">
                  Email *
                </Label>
                <Input
                  id="booking-email"
                  type="email"
                  {...register('clientEmail')}
                  className="mt-1"
                  placeholder="tu@email.com"
                />
                {errors.clientEmail && (
                  <p className="mt-0.5 text-xs text-red-500">{errors.clientEmail.message}</p>
                )}
              </div>

              <div>
                <Label htmlFor="booking-phone" className="text-xs">
                  Teléfono (opcional)
                </Label>
                <Input
                  id="booking-phone"
                  {...register('clientPhone')}
                  className="mt-1"
                  placeholder="809-555-1234"
                />
              </div>

              <div>
                <Label htmlFor="booking-notes" className="text-xs">
                  Notas (opcional)
                </Label>
                <Textarea
                  id="booking-notes"
                  {...register('clientNotes')}
                  className="mt-1 resize-none"
                  rows={2}
                  placeholder="¿Algo que debamos saber?"
                />
              </div>

              <div className="flex gap-2 pt-1">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleReset}
                  className="flex-1"
                >
                  Cancelar
                </Button>
                <Button
                  type="submit"
                  size="sm"
                  disabled={createAppointment.isPending}
                  className="flex-1"
                >
                  {createAppointment.isPending ? (
                    <>
                      <Loader2 className="mr-1 h-3 w-3 animate-spin" />
                      Agendando...
                    </>
                  ) : (
                    'Confirmar cita'
                  )}
                </Button>
              </div>
            </form>
          </div>
        )}

        {/* ── Step 4: Success ── */}
        {step === 'success' && (
          <div className="py-6 text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-green-100">
              <Check className="h-6 w-6 text-green-600" />
            </div>
            <h3 className="text-foreground mb-1 font-semibold">¡Cita agendada!</h3>
            <p className="text-muted-foreground mb-1 text-sm">
              {selectedDate &&
                new Date(selectedDate + 'T12:00:00').toLocaleDateString('es-DO', {
                  weekday: 'long',
                  month: 'long',
                  day: 'numeric',
                })}{' '}
              a las {selectedTime}
            </p>
            <p className="text-muted-foreground text-xs">
              Te enviaremos un correo de confirmación.
            </p>
            <Button variant="outline" size="sm" onClick={handleReset} className="mt-4">
              Agendar otra cita
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default AppointmentCalendar;
