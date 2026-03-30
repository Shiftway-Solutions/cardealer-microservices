/**
 * DealerBotPanel — AI chatbot panel for dealer conversations
 *
 * Extracted from mensajes/page.tsx for lazy-loading.
 * Includes AppointmentScheduler sub-component.
 *
 * Usage: dynamic(() => import('@/components/messaging/DealerBotPanel'), { ssr: false })
 */

'use client';

import * as React from 'react';
import {
  Send,
  Loader2,
  ArrowLeft,
  RefreshCw,
  AlertCircle,
  Bot,
  Sparkles,
  CalendarCheck,
  X,
} from 'lucide-react';
import { useChatbot } from '@/hooks/useChatbot';
import { BotMessageContent } from '@/components/chat/BotMessageContent';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { sanitizeText } from '@/lib/security/sanitize';
import { csrfFetch } from '@/lib/security/csrf';

// =============================================================================
// APPOINTMENT SCHEDULER — Real calendar with month view
// =============================================================================

function AppointmentScheduler({
  dealerName,
  dealerId: _dealerId,
  dealerEmail: _dealerEmail,
  vehicleTitle: _vehicleTitle,
  onSchedule,
  onClose,
}: {
  dealerName: string;
  dealerId?: string;
  dealerEmail?: string;
  vehicleTitle?: string;
  onSchedule: (message: string, date: string, time: string) => void;
  onClose: () => void;
}) {
  const [step, setStep] = React.useState<'date' | 'time'>('date');
  const [selectedDate, setSelectedDate] = React.useState<Date | null>(null);
  const today = React.useMemo(() => new Date(), []);
  const [calendarMonth, setCalendarMonth] = React.useState<Date>(
    () => new Date(today.getFullYear(), today.getMonth(), 1)
  );

  const dayNames = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
  const monthNames = [
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
  const timeSlots = ['09:00 AM', '10:00 AM', '11:00 AM', '02:00 PM', '03:00 PM', '04:00 PM'];

  // Build grid for the current calendar month (Mon-Sun layout starting Sun)
  const calendarDays = React.useMemo(() => {
    const year = calendarMonth.getFullYear();
    const month = calendarMonth.getMonth();
    const firstDay = new Date(year, month, 1).getDay(); // 0=Sun
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const grid: (Date | null)[] = Array(firstDay).fill(null);
    for (let d = 1; d <= daysInMonth; d++) grid.push(new Date(year, month, d));
    while (grid.length % 7 !== 0) grid.push(null);
    return grid;
  }, [calendarMonth]);

  const isAvailable = (d: Date) => {
    if (!d) return false;
    const diff = Math.floor((d.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diff >= 1 && diff <= 45 && d.getDay() !== 0; // Not Sunday, within 45 days
  };

  const isPast = (d: Date) => {
    const diff = Math.floor((d.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return diff < 1;
  };

  const canGoPrev =
    calendarMonth.getFullYear() > today.getFullYear() ||
    calendarMonth.getMonth() > today.getMonth();

  const formatDateStr = (d: Date) =>
    `${dayNames[d.getDay()]} ${d.getDate()} de ${monthNames[d.getMonth()]}`;

  return (
    <div className="border-primary/20 ring-primary/10 mx-3 mb-3 rounded-2xl border bg-white p-4 shadow-lg ring-1">
      {/* Header */}
      <div className="mb-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="bg-primary/10 flex h-7 w-7 items-center justify-center rounded-full">
            <CalendarCheck className="text-primary h-4 w-4" />
          </div>
          <div>
            <h4 className="text-sm font-semibold text-gray-800">Agendar cita con {dealerName}</h4>
            <p className="text-xs text-gray-400">
              {step === 'date' ? 'Paso 1/2 — Selecciona un día' : 'Paso 2/2 — Selecciona una hora'}
            </p>
          </div>
        </div>
        <button
          onClick={onClose}
          className="text-gray-400 transition-colors hover:text-gray-600"
          aria-label="Cerrar"
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Progress bar */}
      <div className="mb-4 flex gap-1">
        {(['date', 'time'] as const).map((s, i) => (
          <div
            key={s}
            className={cn(
              'h-1 flex-1 rounded-full transition-all',
              (step === 'date' && i === 0) || step === 'time' ? 'bg-primary' : 'bg-gray-100'
            )}
          />
        ))}
      </div>

      {/* Step 1: Month calendar */}
      {step === 'date' && (
        <>
          {/* Month navigation */}
          <div className="mb-3 flex items-center justify-between">
            <button
              onClick={() =>
                setCalendarMonth(
                  new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() - 1, 1)
                )
              }
              disabled={!canGoPrev}
              className="flex h-7 w-7 items-center justify-center rounded-full text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700 disabled:cursor-not-allowed disabled:opacity-30"
              aria-label="Mes anterior"
            >
              ‹
            </button>
            <span className="text-sm font-semibold text-gray-700">
              {monthNames[calendarMonth.getMonth()]} {calendarMonth.getFullYear()}
            </span>
            <button
              onClick={() =>
                setCalendarMonth(
                  new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() + 1, 1)
                )
              }
              className="flex h-7 w-7 items-center justify-center rounded-full text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700"
              aria-label="Mes siguiente"
            >
              ›
            </button>
          </div>

          {/* Day-of-week headers */}
          <div className="mb-1 grid grid-cols-7 gap-0.5">
            {dayNames.map(d => (
              <div
                key={d}
                className="py-1 text-center text-[10px] font-semibold tracking-wide text-gray-400 uppercase"
              >
                {d}
              </div>
            ))}
          </div>

          {/* Calendar grid */}
          <div className="grid grid-cols-7 gap-0.5">
            {calendarDays.map((d, idx) => {
              if (!d) return <div key={`empty-${idx}`} />;
              const available = isAvailable(d);
              const past = isPast(d);
              const isSelected = selectedDate?.toDateString() === d.toDateString();
              const isToday = d.toDateString() === today.toDateString();
              return (
                <button
                  key={d.toISOString()}
                  onClick={() => {
                    if (!available) return;
                    setSelectedDate(d);
                    setStep('time');
                  }}
                  disabled={!available}
                  className={cn(
                    'relative flex h-8 w-full items-center justify-center rounded-lg text-sm transition-all',
                    isSelected && 'bg-primary font-bold text-white shadow-md',
                    !isSelected &&
                      available &&
                      'hover:bg-primary/10 hover:text-primary font-medium text-gray-700',
                    !isSelected && past && 'cursor-not-allowed text-gray-200',
                    !isSelected &&
                      !past &&
                      !available &&
                      'cursor-not-allowed text-gray-300 line-through',
                    isToday && !isSelected && 'ring-primary/40 ring-1'
                  )}
                >
                  {d.getDate()}
                  {isToday && !isSelected && (
                    <span className="bg-primary absolute bottom-0.5 left-1/2 h-1 w-1 -translate-x-1/2 rounded-full" />
                  )}
                </button>
              );
            })}
          </div>
          <p className="mt-3 text-center text-xs text-gray-400">
            Domingos no disponibles · Horario: lun–sáb
          </p>
        </>
      )}

      {/* Step 2: Time slots */}
      {step === 'time' && selectedDate && (
        <>
          <button
            onClick={() => setStep('date')}
            className="text-primary mb-3 flex items-center gap-1 text-xs hover:underline"
          >
            ← Cambiar día
          </button>
          <p className="mb-3 text-sm text-gray-500">
            <span className="font-semibold text-gray-800">{formatDateStr(selectedDate)}</span> — ¿A
            qué hora te viene bien?
          </p>
          <div className="grid grid-cols-3 gap-2">
            {timeSlots.map(t => (
              <button
                key={t}
                onClick={() => {
                  const dateStr = formatDateStr(selectedDate);
                  onSchedule(
                    `Quisiera agendar una cita para ver el vehículo el ${dateStr} a las ${t}.`,
                    dateStr,
                    t
                  );
                }}
                className="hover:border-primary hover:bg-primary/5 hover:text-primary rounded-xl border border-gray-100 px-2 py-2.5 text-sm font-medium text-gray-700 transition-all active:scale-95"
              >
                {t}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

// =============================================================================
// DEALER BOT PANEL — self-contained, one instance per conversation
// =============================================================================

/** Props for the DealerBotPanel component */
export interface DealerBotPanelProps {
  dealerId?: string;
  dealerName: string;
  dealerEmail?: string;
  vehicleTitle?: string;
  onBack: () => void;
}

/**
 * DealerBotPanel
 *
 * Initialises its own ChatbotService session scoped to the dealer's account.
 * Mount with key={conversationId} so the session resets when the user
 * navigates to a different conversation.
 */
export default function DealerBotPanel({
  dealerId,
  dealerName,
  dealerEmail,
  vehicleTitle,
  onBack,
}: DealerBotPanelProps) {
  const chat = useChatbot({ dealerId, dealerName, autoStart: true, maxRetries: 2 });
  const botEndRef = React.useRef<HTMLDivElement>(null);
  const [inputValue, setInputValue] = React.useState('');
  const [showAppointmentScheduler, setShowAppointmentScheduler] = React.useState(false);
  const [isBookingAppointment, setIsBookingAppointment] = React.useState(false);
  // Track which bot message last triggered the scheduler, so we don't re-show after dismiss
  const [lastSchedulerMsgId, setLastSchedulerMsgId] = React.useState<string | null>(null);
  // Session-level flag: once calendar has been shown (auto-triggered), don't auto-show again.
  // User can still manually re-open it via the calendar button in the input area.
  const [calendarAutoShown, setCalendarAutoShown] = React.useState(false);
  // Track if the user has already booked an appointment in this session
  const [appointmentBooked, setAppointmentBooked] = React.useState(false);

  // ── Human-feel typing: delay revealing bot responses ──────────
  // Track which bot message IDs have been revealed to the user.
  const [revealedIds, setRevealedIds] = React.useState<Set<string>>(new Set());
  // Timestamp when the typing indicator last started (used to compute remaining delay).
  const typingStartRef = React.useRef<number | null>(null);

  React.useEffect(() => {
    // Track when typing indicator starts
    const hasTyping = chat.messages.some(m => m.isFromBot && m.isLoading);
    if (hasTyping && typingStartRef.current === null) {
      typingStartRef.current = Date.now();
    }

    // For each completed bot message that hasn't been revealed yet, schedule a reveal
    chat.messages.forEach(msg => {
      if (!msg.isFromBot || msg.isLoading || revealedIds.has(msg.id)) return;

      const elapsed = typingStartRef.current ? Date.now() - typingStartRef.current : 0;
      const wordCount = msg.content.split(/\s+/).length;
      // Minimum typing time: ~35ms per word, clamped between 700ms and 2400ms
      const minTypingMs = Math.min(Math.max(wordCount * 35, 700), 2400);
      const remaining = Math.max(0, minTypingMs - elapsed);

      setTimeout(() => {
        setRevealedIds(prev => new Set([...prev, msg.id]));
        typingStartRef.current = null;
      }, remaining);
    });
  }, [chat.messages, revealedIds]);

  // Messages to render: show user messages immediately; bot messages only after reveal delay
  const messagesToShow = React.useMemo(
    () => chat.messages.filter(m => !m.isFromBot || m.isLoading || revealedIds.has(m.id)),
    [chat.messages, revealedIds]
  );

  // Show typing indicator whenever there's a loading message OR unrevealed completed bot messages
  const showTypingIndicator = chat.messages.some(
    m => m.isFromBot && (m.isLoading || (!m.isLoading && !revealedIds.has(m.id)))
  );

  React.useEffect(() => {
    botEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messagesToShow, showTypingIndicator]);

  // Auto-show appointment date picker when bot's last message mentions scheduling
  // Only auto-triggers ONCE per session. After that, user can manually re-open via button.
  React.useEffect(() => {
    // Don't auto-show if already shown once or appointment already booked
    if (calendarAutoShown || appointmentBooked) return;
    const lastBotMsg = [...chat.messages]
      .reverse()
      .find(m => m.isFromBot && !m.isLoading && !m.isError);
    if (!lastBotMsg || lastBotMsg.id === lastSchedulerMsgId) return;
    const text = lastBotMsg.content.toLowerCase();
    const keywords = ['agendar', 'cita', 'visita', 'disponib', 'coordinar'];
    if (keywords.some(kw => text.includes(kw))) {
      setLastSchedulerMsgId(lastBotMsg.id);
      setShowAppointmentScheduler(true);
      setCalendarAutoShown(true); // Mark as auto-shown — won't auto-trigger again
    }
  }, [chat.messages, calendarAutoShown, appointmentBooked, lastSchedulerMsgId]);

  const handleSend = () => {
    const text = sanitizeText(inputValue.trim(), { maxLength: 2000 });
    if (!text || chat.isLoading) return;
    setInputValue('');
    setShowAppointmentScheduler(false);
    chat.sendMessage(text);
  };

  // Handle appointment scheduling: send message to bot + call booking API
  const handleScheduleAppointment = async (message: string, date: string, time: string) => {
    setShowAppointmentScheduler(false);
    setIsBookingAppointment(true);
    setAppointmentBooked(true); // Prevent calendar from auto-showing again
    // Send user message to chatbot
    chat.sendMessage(message);
    // Call appointment booking API (fire-and-forget, non-blocking)
    try {
      await csrfFetch('/api/appointments/book', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          dealerId: sanitizeText(dealerId || '', { maxLength: 100 }),
          dealerName: sanitizeText(dealerName, { maxLength: 200 }),
          dealerEmail: dealerEmail || '',
          vehicleTitle: sanitizeText(vehicleTitle || dealerName, { maxLength: 300 }),
          date,
          time,
        }),
      });
    } catch {
      // Non-blocking — email failure doesn't disrupt the chat
    } finally {
      setIsBookingAppointment(false);
    }
  };

  // "Asistente [DealerName]" — use botName returned by the service if available
  const displayName = chat.botName || `Asistente de ${dealerName}`;

  return (
    <>
      {/* Header */}
      <div className="border-border bg-card flex items-center gap-3 border-b p-4 shadow-sm">
        <Button
          variant="ghost"
          size="icon"
          className="md:hidden"
          onClick={onBack}
          aria-label="Volver a chats"
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>

        <div className="relative">
          <div className="from-primary flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br to-emerald-600 shadow-md">
            <Bot className="h-5 w-5 text-white" />
          </div>
          <span className="absolute -right-0.5 -bottom-0.5 h-3 w-3 rounded-full border-2 border-white bg-green-400" />
        </div>

        <div className="min-w-0 flex-1">
          <h3 className="text-foreground truncate font-semibold">{displayName}</h3>
          <p className="text-primary text-xs">● En línea · Responde al instante</p>
        </div>

        <div className="flex items-center gap-2">
          <Badge className="bg-primary/10 text-primary shrink-0 border-0">
            <Sparkles className="mr-1 h-3 w-3" />
            IA
          </Badge>
          <Button
            variant="ghost"
            size="icon"
            onClick={onBack}
            title="Volver a mensajes"
            className="hidden md:inline-flex"
          >
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto bg-gradient-to-b from-gray-50/30 to-white p-4">
        {!chat.isConnected && chat.isLoading && messagesToShow.length === 0 && (
          <div className="flex flex-col items-center justify-center py-12">
            <Loader2 className="text-primary h-8 w-8 animate-spin" />
            <p className="text-muted-foreground mt-3 text-sm">
              Conectando con el asistente de {dealerName}…
            </p>
          </div>
        )}

        {!chat.isConnected && !chat.isLoading && chat.error && (
          <div className="flex flex-col items-center justify-center py-12">
            <AlertCircle className="h-8 w-8 text-red-400" />
            <p className="text-muted-foreground mt-3 text-center text-sm">{chat.error}</p>
            <Button
              variant="outline"
              size="sm"
              className="mt-4"
              onClick={() => chat.startSession()}
            >
              <RefreshCw className="mr-2 h-4 w-4" />
              Reintentar conexión
            </Button>
          </div>
        )}

        <div className="space-y-4">
          {messagesToShow.map(message => (
            <div
              key={message.id}
              className={cn(
                'flex gap-2.5',
                message.isFromBot ? 'justify-start' : 'justify-end',
                // Smooth fade-in for newly revealed bot messages
                message.isFromBot &&
                  !message.isLoading &&
                  'animate-in fade-in slide-in-from-bottom-1 duration-300'
              )}
            >
              {message.isFromBot && (
                <div className="from-primary mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br to-emerald-600 shadow-sm">
                  <Bot className="h-3.5 w-3.5 text-white" />
                </div>
              )}
              <div
                className={cn(
                  'max-w-[78%] rounded-2xl px-4 py-2.5 shadow-sm',
                  message.isFromBot
                    ? 'rounded-tl-sm bg-white text-gray-800 ring-1 ring-gray-100'
                    : 'from-primary rounded-tr-sm bg-gradient-to-br to-emerald-600 text-white'
                )}
              >
                {message.isLoading ? (
                  <div className="flex gap-1.5 py-1">
                    <span
                      className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                      style={{ animationDelay: '0ms' }}
                    />
                    <span
                      className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                      style={{ animationDelay: '150ms' }}
                    />
                    <span
                      className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                      style={{ animationDelay: '300ms' }}
                    />
                  </div>
                ) : message.isFromBot ? (
                  <>
                    <BotMessageContent content={message.content} />
                    <p className="mt-1.5 text-xs text-gray-400">
                      {message.timestamp.toLocaleTimeString('es-DO', {
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </p>
                  </>
                ) : (
                  <>
                    <p className="text-sm whitespace-pre-wrap">{message.content}</p>
                    <p className="mt-1.5 text-right text-xs text-white/70">
                      {message.timestamp.toLocaleTimeString('es-DO', {
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </p>
                  </>
                )}
              </div>
            </div>
          ))}

          {/* Typing indicator — shown while waiting for reveal */}
          {showTypingIndicator && (
            <div className="animate-in fade-in flex gap-2.5">
              <div className="from-primary mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br to-emerald-600 shadow-sm">
                <Bot className="h-3.5 w-3.5 text-white" />
              </div>
              <div className="max-w-[78%] rounded-2xl rounded-tl-sm bg-white px-4 py-3 shadow-sm ring-1 ring-gray-100">
                <div className="flex gap-1.5">
                  <span
                    className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                    style={{ animationDelay: '0ms' }}
                  />
                  <span
                    className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                    style={{ animationDelay: '150ms' }}
                  />
                  <span
                    className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                    style={{ animationDelay: '300ms' }}
                  />
                </div>
              </div>
            </div>
          )}
        </div>

        {chat.quickReplies.length > 0 && !chat.isLoading && (
          <div className="mt-4 flex flex-wrap gap-2">
            {chat.quickReplies.map(reply => (
              <button
                key={reply.payload ?? reply.text}
                onClick={() => chat.selectQuickReply(reply)}
                disabled={chat.isLoading}
                className="border-primary/40 text-primary hover:border-primary hover:bg-primary/5 rounded-full border bg-white px-3.5 py-1.5 text-sm font-medium shadow-sm transition-colors disabled:opacity-50"
              >
                {reply.text}
              </button>
            ))}
          </div>
        )}

        <div ref={botEndRef} />
      </div>

      {/* Appointment Scheduler — inline card above input */}
      {showAppointmentScheduler && (
        <AppointmentScheduler
          dealerName={dealerName}
          dealerId={dealerId}
          dealerEmail={dealerEmail}
          vehicleTitle={vehicleTitle}
          onSchedule={handleScheduleAppointment}
          onClose={() => setShowAppointmentScheduler(false)}
        />
      )}

      {/* Booking progress indicator */}
      {isBookingAppointment && (
        <div className="bg-primary/5 text-primary mx-3 mb-2 flex items-center gap-2 rounded-xl px-4 py-2 text-sm">
          <Loader2 className="h-4 w-4 animate-spin" />
          Confirmando cita y enviando correos…
        </div>
      )}

      {/* Input */}
      <div className="border-border border-t bg-gradient-to-t from-gray-50/50 to-white p-4">
        {chat.isLimitReached ? (
          <div className="flex items-center justify-between rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-700">
            <span>Has alcanzado el límite de mensajes.</span>
            <Button
              variant="link"
              size="sm"
              className="h-auto p-0 text-amber-700"
              onClick={chat.resetChat}
            >
              Reiniciar chat
            </Button>
          </div>
        ) : (
          <div className="flex gap-3">
            {/* Calendar re-open button — allows user to manually show scheduler */}
            {calendarAutoShown && !showAppointmentScheduler && !isBookingAppointment && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setShowAppointmentScheduler(true)}
                title="Abrir calendario de citas"
                className="border-primary/30 text-primary hover:bg-primary/10 h-12 w-12 shrink-0 rounded-xl border"
                aria-label="Abrir calendario de citas"
              >
                <CalendarCheck className="h-5 w-5" />
              </Button>
            )}
            <Input
              type="text"
              placeholder={`Pregunta al asistente de ${dealerName}…`}
              value={inputValue}
              onChange={e => setInputValue(e.target.value)}
              onKeyDown={e => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  handleSend();
                }
              }}
              disabled={chat.isLoading || !chat.isConnected}
              className="border-border bg-card focus:border-primary focus:ring-primary/20 h-12 flex-1 rounded-xl shadow-sm transition-all focus:ring-2"
              aria-label="Mensaje al asistente del dealer"
            />
            <Button
              onClick={handleSend}
              disabled={!inputValue.trim() || chat.isLoading || !chat.isConnected}
              className="from-primary shadow-primary/25 h-12 w-12 rounded-xl bg-gradient-to-r to-emerald-600 shadow-lg transition-all hover:shadow-xl disabled:opacity-50 disabled:shadow-none"
              aria-label="Enviar mensaje"
            >
              {chat.isLoading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <Send className="h-5 w-5" />
              )}
            </Button>
          </div>
        )}
        {!chat.isLimitReached &&
          chat.remainingInteractions > 0 &&
          chat.remainingInteractions <= 5 && (
            <p className="text-muted-foreground mt-2 text-center text-xs">
              {chat.remainingInteractions} mensajes restantes
            </p>
          )}
      </div>
    </>
  );
}
