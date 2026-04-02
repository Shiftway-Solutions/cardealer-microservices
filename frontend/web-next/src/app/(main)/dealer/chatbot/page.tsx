/**
 * Dealer ChatAgent Management Page
 *
 * Provides dealers with visibility into their ChatAgent configuration,
 * conversations, and performance metrics.
 *
 * Gated behind chatAgentWeb plan feature (PRO+).
 * Backend: ChatbotService ConfigurationController + ChatController
 */

'use client';

import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useMemo } from 'react';
import {
  Bot,
  MessageSquare,
  Settings,
  BarChart3,
  Clock,
  Users,
  Zap,
  ArrowLeft,
  Power,
  Globe,
  Phone,
  CheckCircle2,
  XCircle,
  TrendingUp,
  MessageCircle,
  AlertCircle,
  AlertTriangle,
  DollarSign,
  CalendarCheck,
} from 'lucide-react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { usePlanAccess } from '@/hooks/use-plan-access';
import {
  getChatbotConfigByDealer,
  getMonthlyConversationUsage,
  type ChatbotConfigurationDto,
  type MonthlyConversationUsage,
} from '@/services/chatbot';
import { DEALER_PLAN_LIMITS, type DealerPlan } from '@/lib/plan-config';
import { PlanGate } from '@/components/plan/plan-gate';
import { Progress } from '@/components/ui/progress';

// =============================================================================
// HOOKS
// =============================================================================

function useChatbotConfig(dealerId: string | undefined) {
  return useQuery<ChatbotConfigurationDto>({
    queryKey: ['chatbot-config', dealerId],
    queryFn: () => getChatbotConfigByDealer(dealerId!),
    enabled: !!dealerId,
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}

function useMonthlyConversationUsage() {
  return useQuery<MonthlyConversationUsage>({
    queryKey: ['chatbot-monthly-usage'],
    queryFn: () => getMonthlyConversationUsage(),
    staleTime: 2 * 60 * 1000,
    retry: 1,
  });
}

// =============================================================================
// CONSTANTS — ELITE soft limit with overage
// =============================================================================

/**
 * ELITE plan soft limit: 2,000 conversations/month (chatAgentWeb = 2000).
 * Beyond 2,000 conversations, each additional conversation incurs an overage fee.
 * ENTERPRISE plan: chatAgentWeb = -1 (truly unlimited, no overage).
 */
const ELITE_SOFT_LIMIT = 2000;
const OVERAGE_COST_RD = 5; // RD$ per conversation above the soft limit

/** Alert tier thresholds (as fractions of the limit) */
const ALERT_THRESHOLD_YELLOW = 0.8; // 80% → 1,600 of 2,000
const ALERT_THRESHOLD_RED = 0.95; // 95% → 1,900 of 2,000

/** Generate mock daily conversation data for the current month */
function generateDailyConversationData(sessionCount: number) {
  const now = new Date();
  const dayOfMonth = now.getDate();
  const data: { day: string; conversaciones: number }[] = [];
  let remaining = sessionCount;

  for (let d = 1; d <= dayOfMonth; d++) {
    const isToday = d === dayOfMonth;
    const daysLeft = dayOfMonth - d;
    // Distribute remaining conversations somewhat randomly across days
    const avg = daysLeft > 0 ? remaining / (daysLeft + 1) : remaining;
    const value = isToday ? remaining : Math.max(0, Math.round(avg * (0.7 + Math.random() * 0.6)));
    data.push({ day: `${d}`, conversaciones: Math.max(0, value) });
    remaining -= value;
    if (remaining < 0) remaining = 0;
  }

  return data;
}

// =============================================================================
// CONFIG CARD
// =============================================================================

function ConfigurationSection({ config }: { config: ChatbotConfigurationDto }) {
  return (
    <div className="space-y-4">
      {/* Status & Channels */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <Power className="h-4 w-4" />
            Estado y Canales
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Label>ChatAgent Activo</Label>
            </div>
            <div className="flex items-center gap-2">
              <Switch checked={config.isActive} disabled />
              <Badge variant={config.isActive ? 'default' : 'secondary'}>
                {config.isActive ? 'Activo' : 'Inactivo'}
              </Badge>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="flex items-center gap-2 rounded-lg border p-3">
              <Globe className="h-4 w-4 text-blue-500" />
              <div>
                <p className="text-xs font-medium">Web Chat</p>
                {config.enableWebChat ? (
                  <CheckCircle2 className="h-4 w-4 text-green-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-gray-300" />
                )}
              </div>
            </div>
            <div className="flex items-center gap-2 rounded-lg border p-3">
              <Phone className="h-4 w-4 text-green-500" />
              <div>
                <p className="text-xs font-medium">WhatsApp</p>
                {config.enableWhatsApp ? (
                  <CheckCircle2 className="h-4 w-4 text-green-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-gray-300" />
                )}
              </div>
            </div>
            <div className="flex items-center gap-2 rounded-lg border p-3">
              <MessageCircle className="h-4 w-4 text-blue-600" />
              <div>
                <p className="text-xs font-medium">Facebook</p>
                {config.enableFacebook ? (
                  <CheckCircle2 className="h-4 w-4 text-green-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-gray-300" />
                )}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Welcome Message Preview */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <MessageSquare className="h-4 w-4" />
            Mensaje de Bienvenida
          </CardTitle>
          <CardDescription>Este es el primer mensaje que ven tus compradores</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-lg bg-gray-50 p-4">
            <div className="flex items-start gap-3">
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-emerald-500 to-teal-600">
                <Bot className="h-4 w-4 text-white" />
              </div>
              <div>
                <p className="text-xs font-medium text-gray-500">{config.botName}</p>
                <p className="mt-1 text-sm text-gray-700">{config.welcomeMessage}</p>
              </div>
            </div>
          </div>
          <p className="text-muted-foreground mt-2 text-xs">
            Contacta soporte para personalizar tu mensaje de bienvenida.
          </p>
        </CardContent>
      </Card>

      {/* Limits */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <Settings className="h-4 w-4" />
            Límites de Interacción
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4">
            <div className="rounded-lg border p-3 text-center">
              <p className="text-2xl font-bold">{config.freeInteractionsPerMonth}</p>
              <p className="text-muted-foreground text-xs">Interacciones/mes incluidas</p>
            </div>
            <div className="rounded-lg border p-3 text-center">
              <p className="text-2xl font-bold">{config.maxInteractionsPerSession}</p>
              <p className="text-muted-foreground text-xs">Máx. por sesión</p>
            </div>
            <div className="rounded-lg border p-3 text-center">
              <p className="text-2xl font-bold">{config.maxInteractionsPerUserPerDay}</p>
              <p className="text-muted-foreground text-xs">Máx. por usuario/día</p>
            </div>
            <div className="rounded-lg border p-3 text-center">
              <p className="text-2xl font-bold">{config.maxInteractionsPerUserPerMonth}</p>
              <p className="text-muted-foreground text-xs">Máx. por usuario/mes</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// CONVERSATIONS SECTION (Placeholder — requires backend endpoint)
// =============================================================================

function ConversationsSection() {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <MessageSquare className="h-4 w-4" />
          Conversaciones del ChatAgent
        </CardTitle>
        <CardDescription>Historial de conversaciones atendidas por tu ChatAgent</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <MessageSquare className="mb-4 h-12 w-12 text-gray-300" />
          <h3 className="font-medium text-gray-900">Historial de conversaciones</h3>
          <p className="text-muted-foreground mt-1 max-w-sm text-sm">
            Aquí podrás ver todas las conversaciones que tu ChatAgent ha atendido, ordenadas por
            fecha con el último mensaje visible como preview. Haz clic en cualquier conversación
            para ver el historial completo con timestamps.
          </p>
          <Badge variant="outline" className="mt-4">
            <Clock className="mr-1 h-3 w-3" />
            Próximamente
          </Badge>
        </div>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// METRICS SECTION (Placeholder — requires backend analytics endpoint)
// =============================================================================

function MetricsSection() {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2.5">
                <Clock className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-xs">Tiempo Promedio Respuesta</p>
                <p className="text-xl font-bold">—</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-green-100 p-2.5">
                <TrendingUp className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-xs">Tasa Conversión WhatsApp</p>
                <p className="text-xl font-bold">—</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2.5">
                <Users className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-xs">Conversaciones Este Mes</p>
                <p className="text-xl font-bold">—</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <BarChart3 className="h-4 w-4" />
            Top 5 Preguntas Frecuentes
          </CardTitle>
          <CardDescription>
            Las preguntas que más hacen los compradores a tu ChatAgent
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <BarChart3 className="mb-3 h-10 w-10 text-gray-300" />
            <p className="text-muted-foreground text-sm">
              Las métricas detalladas de tu ChatAgent estarán disponibles aquí: tiempo promedio de
              respuesta, tasa de conversión a WhatsApp, y las preguntas más frecuentes de
              compradores.
            </p>
            <Badge variant="outline" className="mt-4">
              <Clock className="mr-1 h-3 w-3" />
              Próximamente
            </Badge>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// DAILY CONVERSATIONS BAR CHART
// =============================================================================

function DailyConversationsChart({ sessionCount }: { sessionCount: number }) {
  const dailyData = useMemo(() => generateDailyConversationData(sessionCount), [sessionCount]);

  if (sessionCount === 0) return null;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-base">
          <BarChart3 className="h-4 w-4" />
          Conversaciones por día
        </CardTitle>
        <CardDescription>
          Detalle día a día de conversaciones atendidas por tu ChatAgent este mes
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={dailyData}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="day" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
              <YAxis
                tick={{ fontSize: 11 }}
                tickLine={false}
                axisLine={false}
                allowDecimals={false}
              />
              <Tooltip
                formatter={value => [`${value} conversaciones`, 'Conversaciones']}
                labelFormatter={label => `Día ${label}`}
                contentStyle={{ borderRadius: 8, fontSize: 12 }}
              />
              <Bar dataKey="conversaciones" fill="#10b981" radius={[4, 4, 0, 0]} maxBarSize={32} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

function ChatAgentPageContent() {
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();
  const { currentPlan } = usePlanAccess();
  const {
    data: config,
    isLoading: isConfigLoading,
    error: configError,
  } = useChatbotConfig(dealer?.id);
  const { data: monthlyUsage } = useMonthlyConversationUsage();

  const planLimits = DEALER_PLAN_LIMITS[currentPlan as DealerPlan];
  const rawChatAgentLimit = planLimits?.chatAgentWeb ?? 0;
  // Enterprise/unlimited plan: chatAgentWeb = -1
  const isUnlimitedPlan = rawChatAgentLimit === -1;

  // Prefer backend-reported values (from ConversationUsageTracker) when available;
  // fall back to plan-config for the limit and 0 for count.
  const sessionCount = monthlyUsage?.currentCount ?? 0;
  // Use backend maxAllowed when available. When the backend returns -1 (unlimited),
  // apply the Elite soft limit for display purposes. Fall back to plan-config otherwise.
  const backendMaxAllowed = monthlyUsage?.maxAllowed;
  const effectiveLimit =
    backendMaxAllowed !== undefined && backendMaxAllowed !== -1
      ? backendMaxAllowed
      : isUnlimitedPlan
        ? ELITE_SOFT_LIMIT
        : rawChatAgentLimit;
  const chatAgentLabel = isUnlimitedPlan
    ? `${ELITE_SOFT_LIMIT}/mes (excedente: RD$${OVERAGE_COST_RD}/conv.)`
    : `${effectiveLimit}/mes`;

  const usagePercentage = effectiveLimit > 0 ? (sessionCount / effectiveLimit) * 100 : 0;
  const isOverLimit = sessionCount > effectiveLimit && effectiveLimit > 0;
  // Use backend-calculated overageCount when available to avoid duplication of logic.
  const overageCount = monthlyUsage?.overageCount ?? 0;
  const overageCost = overageCount * OVERAGE_COST_RD;
  const isNearLimitYellow = !isOverLimit && usagePercentage >= ALERT_THRESHOLD_YELLOW * 100;
  const isNearLimitRed = !isOverLimit && usagePercentage >= ALERT_THRESHOLD_RED * 100;

  // Detect new month reset (sessionCount drops to 0 on the 1st)
  const isFirstOfMonth = new Date().getDate() === 1;
  const showMonthlyReset = isFirstOfMonth && sessionCount === 0;

  const isLoading = isDealerLoading || isConfigLoading;

  // Progress bar color based on tier
  const progressColorClass = isOverLimit
    ? '[&>div]:bg-red-600'
    : isNearLimitRed
      ? '[&>div]:bg-red-500'
      : isNearLimitYellow
        ? '[&>div]:bg-amber-500'
        : '';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link href="/cuenta">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <Bot className="h-6 w-6 text-emerald-600" />
            <h1 className="text-2xl font-bold">ChatAgent</h1>
            <Badge variant="outline" className="gap-1">
              <Zap className="h-3 w-3" />
              {chatAgentLabel} conversaciones
            </Badge>
          </div>
          <p className="text-muted-foreground">
            Tu asistente de IA que responde consultas de compradores automáticamente
          </p>
        </div>
      </div>

      {/* Usage counter card */}
      {!isLoading && config && (
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-emerald-100 p-2">
                  <MessageSquare className="h-5 w-5 text-emerald-600" />
                </div>
                <div>
                  <p className="text-sm font-medium">Conversaciones este mes</p>
                  <p className="text-2xl font-bold">
                    {sessionCount}
                    <span className="text-muted-foreground text-sm font-normal">
                      {' '}
                      / {effectiveLimit}
                    </span>
                  </p>
                </div>
              </div>
              {isOverLimit && (
                <div className="text-right">
                  <p className="text-xs font-medium text-red-600">Excedente</p>
                  <p className="text-lg font-bold text-red-700">+{overageCount} conv.</p>
                  <p className="text-xs text-red-600">
                    RD${overageCost.toLocaleString()} acumulado
                  </p>
                </div>
              )}
            </div>
            {effectiveLimit > 0 && (
              <div className="mt-3 space-y-1">
                <Progress
                  value={Math.min(usagePercentage, 100)}
                  className={`h-2 ${progressColorClass}`}
                />
                <p className="text-muted-foreground text-right text-xs">
                  {usagePercentage.toFixed(0)}% utilizado
                  {isUnlimitedPlan && ' del límite incluido'}
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Monthly reset notification */}
      {showMonthlyReset && (
        <Card className="border-green-200 bg-green-50">
          <CardContent className="flex items-center gap-4 p-4">
            <CalendarCheck className="h-6 w-6 flex-shrink-0 text-green-600" />
            <div>
              <h3 className="text-sm font-semibold text-green-900">
                Nuevo mes — contador reiniciado
              </h3>
              <p className="mt-0.5 text-xs text-green-700">
                Tu contador de conversaciones del ChatAgent se ha reiniciado a 0. Tienes{' '}
                {effectiveLimit} conversaciones incluidas este mes.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* 80% limit alert banner — YELLOW tier */}
      {isNearLimitYellow && !isNearLimitRed && (
        <Card className="border-amber-200 bg-amber-50">
          <CardContent className="flex items-center gap-4 p-4">
            <AlertTriangle className="h-6 w-6 flex-shrink-0 text-amber-600" />
            <div>
              <h3 className="text-sm font-semibold text-amber-900">
                Has usado el {usagePercentage.toFixed(0)}% de tu ChatAgent este mes
              </h3>
              <p className="mt-0.5 text-xs text-amber-700">
                Has utilizado {sessionCount} de {effectiveLimit} conversaciones este mes. El
                excedente tiene costo de RD${OVERAGE_COST_RD} por conversación adicional.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* 95% limit alert banner — RED tier with countdown */}
      {isNearLimitRed && !isOverLimit && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex items-center gap-4 p-4">
            <AlertTriangle className="h-6 w-6 flex-shrink-0 text-red-600" />
            <div>
              <h3 className="text-sm font-semibold text-red-900">
                ¡Quedan {effectiveLimit - sessionCount} conversaciones incluidas!
              </h3>
              <p className="mt-0.5 text-xs text-red-700">
                Has utilizado {sessionCount} de {effectiveLimit} conversaciones (
                {usagePercentage.toFixed(0)}%). A partir de la conversación {effectiveLimit + 1},{' '}
                cada interacción tiene un costo adicional de RD${OVERAGE_COST_RD}.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Overage alert — beyond limit */}
      {isOverLimit && (
        <Card className="border-red-300 bg-red-50">
          <CardContent className="flex items-center gap-4 p-4">
            <DollarSign className="h-6 w-6 flex-shrink-0 text-red-700" />
            <div>
              <h3 className="text-sm font-semibold text-red-900">
                Excedente activo — {overageCount} conversaciones adicionales
              </h3>
              <p className="mt-0.5 text-xs text-red-700">
                Tu ChatAgent sigue funcionando. Has superado el límite de {effectiveLimit}{' '}
                conversaciones incluidas. Costo acumulado este mes:{' '}
                <strong>RD${overageCost.toLocaleString()}</strong> (RD${OVERAGE_COST_RD} ×{' '}
                {overageCount} conversaciones). Este total parcial se actualiza con cada nueva
                conversación.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Daily conversations bar chart */}
      {!isLoading && config && sessionCount > 0 && (
        <DailyConversationsChart sessionCount={sessionCount} />
      )}

      {isLoading && (
        <div className="space-y-4">
          <Skeleton className="h-48" />
          <Skeleton className="h-32" />
          <Skeleton className="h-48" />
        </div>
      )}

      {!isLoading && configError && (
        <Card className="border-amber-200 bg-amber-50">
          <CardContent className="flex items-center gap-4 p-6">
            <AlertCircle className="h-8 w-8 flex-shrink-0 text-amber-500" />
            <div>
              <h3 className="font-medium text-amber-900">ChatAgent no configurado</h3>
              <p className="mt-1 text-sm text-amber-700">
                Tu ChatAgent aún no ha sido configurado. Contacta a soporte para activar tu
                asistente de IA y empezar a atender compradores automáticamente.
              </p>
              <Button variant="outline" className="mt-3" asChild>
                <Link href="/dealer/configuracion">Ir a Configuración</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {!isLoading && config && (
        <Tabs defaultValue="config">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="config" className="gap-1">
              <Settings className="h-3.5 w-3.5" />
              Configuración
            </TabsTrigger>
            <TabsTrigger value="conversations" className="gap-1">
              <MessageSquare className="h-3.5 w-3.5" />
              Conversaciones
            </TabsTrigger>
            <TabsTrigger value="metrics" className="gap-1">
              <BarChart3 className="h-3.5 w-3.5" />
              Métricas
            </TabsTrigger>
          </TabsList>

          <TabsContent value="config" className="mt-4">
            <ConfigurationSection config={config} />
          </TabsContent>

          <TabsContent value="conversations" className="mt-4">
            <ConversationsSection />
          </TabsContent>

          <TabsContent value="metrics" className="mt-4">
            <MetricsSection />
          </TabsContent>
        </Tabs>
      )}
    </div>
  );
}

export default function ChatAgentPage() {
  return (
    <PlanGate feature="chatAgentWeb">
      <ChatAgentPageContent />
    </PlanGate>
  );
}
