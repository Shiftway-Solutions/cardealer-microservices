/**
 * Dealer Leads Page
 *
 * CRM for managing leads and customer inquiries
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { PlanGate } from '@/components/plan/plan-gate';
import {
  Users,
  Search,
  Filter,
  Phone,
  Mail,
  MessageSquare,
  Clock,
  Star,
  TrendingUp,
  ChevronRight,
  Calendar,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import { useLeadStats, useDeleteLead, useUpdateLead } from '@/hooks/use-crm';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  getLeadStatusColor,
  getLeadScoreColor,
  formatLeadName,
  type LeadDto,
} from '@/services/crm';

// Loading skeleton for stats
function StatsSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {[1, 2, 3, 4].map(i => (
        <Card key={i}>
          <CardContent className="flex items-center gap-3 p-4">
            <Skeleton className="h-10 w-10 rounded-lg" />
            <div className="space-y-2">
              <Skeleton className="h-6 w-12" />
              <Skeleton className="h-3 w-20" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

// Loading skeleton for leads list
function LeadsListSkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2, 3, 4, 5].map(i => (
        <Card key={i}>
          <CardContent className="p-4">
            <div className="flex items-center gap-4">
              <Skeleton className="h-12 w-12 rounded-full" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="h-4 w-64" />
                <Skeleton className="h-3 w-40" />
              </div>
              <div className="flex gap-2">
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-8" />
                <Skeleton className="h-8 w-16" />
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

const getStatusBadge = (status: string) => {
  const statusConfig: Record<string, { label: string; className: string }> = {
    New: { label: 'Nuevo', className: 'bg-primary/10 text-primary' },
    Contacted: { label: 'Contactado', className: 'bg-blue-100 text-blue-700' },
    Qualified: { label: 'Calificado', className: 'bg-purple-100 text-purple-700' },
    Proposal: { label: 'Propuesta', className: 'bg-indigo-100 text-indigo-700' },
    Negotiating: { label: 'Negociando', className: 'bg-amber-100 text-amber-700' },
    Won: { label: 'Ganado', className: 'bg-green-100 text-green-700' },
    Lost: { label: 'Perdido', className: 'bg-red-100 text-red-700' },
  };
  const config = statusConfig[status] || { label: status, className: 'bg-muted text-foreground' };
  return <Badge className={config.className}>{config.label}</Badge>;
};

const getScoreColor = (score: number) => {
  if (score >= 80) return 'text-primary';
  if (score >= 60) return 'text-amber-600';
  return 'text-red-600';
};

const formatTime = (date: string) => {
  const d = new Date(date);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMins / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffMins < 60) return `Hace ${diffMins} min`;
  if (diffHours < 24) return `Hace ${diffHours}h`;
  return `Hace ${diffDays} día(s)`;
};

function LeadsPageContent() {
  const [search, setSearch] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState('all');

  // API hooks
  const { data: _dealer } = useCurrentDealer();
  const {
    data: stats,
    leads,
    isLoading: isStatsLoading,
    error: statsError,
    refetch,
  } = useLeadStats();
  const { mutate: _deleteLead } = useDeleteLead();
  const { mutate: _updateLead } = useUpdateLead();

  const isLoading = isStatsLoading;

  // Filter leads locally
  const filteredLeads = React.useMemo(() => {
    if (!leads) return [];

    return leads.filter(lead => {
      const matchesSearch =
        lead.fullName.toLowerCase().includes(search.toLowerCase()) ||
        lead.email.toLowerCase().includes(search.toLowerCase()) ||
        (lead.phone && lead.phone.includes(search));

      const matchesStatus = statusFilter === 'all' || lead.status === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [leads, search, statusFilter]);

  // Stats cards data
  const statsCards = React.useMemo(() => {
    if (!stats) return [];

    return [
      { label: 'Total Leads', value: stats.total, icon: Users, color: 'text-blue-600' },
      { label: 'Nuevos', value: stats.new, icon: Star, color: 'text-primary' },
      {
        label: 'Tasa Conversión',
        value: `${stats.conversionRate}%`,
        icon: TrendingUp,
        color: 'text-purple-600',
      },
      { label: 'Score Promedio', value: stats.avgScore, icon: Clock, color: 'text-amber-600' },
    ];
  }, [stats]);

  // Count leads by status category
  const leadCounts = React.useMemo(() => {
    if (!leads) return { all: 0, new: 0, active: 0, closed: 0 };

    return {
      all: leads.length,
      new: leads.filter(l => l.status === 'New').length,
      active: leads.filter(l =>
        ['Contacted', 'Qualified', 'Proposal', 'Negotiating'].includes(l.status)
      ).length,
      closed: leads.filter(l => ['Won', 'Lost'].includes(l.status)).length,
    };
  }, [leads]);

  // Error state
  if (statsError) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Leads</h1>
            <p className="text-muted-foreground">Gestiona tus oportunidades de venta</p>
          </div>
        </div>
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex flex-col items-center justify-center gap-4 p-8">
            <AlertCircle className="h-12 w-12 text-red-500" />
            <p className="text-center text-red-700">
              Error al cargar los leads. Por favor intenta de nuevo.
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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Leads</h1>
          <p className="text-muted-foreground">Gestiona tus oportunidades de venta</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Actualizar
          </Button>
          <Button variant="outline">Exportar CSV</Button>
        </div>
      </div>

      {/* Stats */}
      {isLoading ? (
        <StatsSkeleton />
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {statsCards.map(stat => {
            const Icon = stat.icon;
            return (
              <Card key={stat.label}>
                <CardContent className="flex items-center gap-3 p-4">
                  <div className={`bg-muted rounded-lg p-2 ${stat.color}`}>
                    <Icon className="h-5 w-5" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold">{stat.value}</p>
                    <p className="text-muted-foreground text-xs">{stat.label}</p>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Tabs */}
      <Tabs defaultValue="all" className="space-y-4">
        <TabsList>
          <TabsTrigger value="all">Todos ({leadCounts.all})</TabsTrigger>
          <TabsTrigger value="new">Nuevos ({leadCounts.new})</TabsTrigger>
          <TabsTrigger value="active">Activos ({leadCounts.active})</TabsTrigger>
          <TabsTrigger value="closed">Cerrados ({leadCounts.closed})</TabsTrigger>
        </TabsList>

        <TabsContent value="all" className="space-y-4">
          {/* Filters */}
          <Card>
            <CardContent className="p-4">
              <div className="flex flex-col gap-4 sm:flex-row">
                <div className="relative flex-1">
                  <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                  <Input
                    placeholder="Buscar por nombre, email o teléfono..."
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                    className="pl-9"
                  />
                </div>
                <Select value={statusFilter} onValueChange={setStatusFilter}>
                  <SelectTrigger className="w-40">
                    <SelectValue placeholder="Estado" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos</SelectItem>
                    <SelectItem value="New">Nuevos</SelectItem>
                    <SelectItem value="Contacted">Contactados</SelectItem>
                    <SelectItem value="Qualified">Calificados</SelectItem>
                    <SelectItem value="Negotiating">Negociando</SelectItem>
                    <SelectItem value="Won">Ganados</SelectItem>
                    <SelectItem value="Lost">Perdidos</SelectItem>
                  </SelectContent>
                </Select>
                <Button variant="outline">
                  <Filter className="mr-2 h-4 w-4" />
                  Más Filtros
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Leads List */}
          {isLoading ? (
            <LeadsListSkeleton />
          ) : (
            <div className="space-y-3">
              {filteredLeads.map(lead => (
                <Card key={lead.id} className="transition-shadow hover:shadow-md">
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between gap-4">
                      <div className="flex flex-1 items-center gap-4">
                        <div className="bg-muted flex h-12 w-12 shrink-0 items-center justify-center rounded-full">
                          <span className="text-muted-foreground text-lg font-medium">
                            {lead.firstName.charAt(0)}
                          </span>
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-2">
                            <Link
                              href={`/dealer/leads/${lead.id}`}
                              className="hover:text-primary font-semibold transition-colors"
                            >
                              {formatLeadName(lead)}
                            </Link>
                            {getStatusBadge(lead.status)}
                            <span className={`text-sm font-medium ${getScoreColor(lead.score)}`}>
                              {lead.score}%
                            </span>
                          </div>
                          <p className="text-muted-foreground truncate text-sm">{lead.email}</p>
                          <div className="text-muted-foreground mt-1 flex items-center gap-4 text-xs">
                            <span>{lead.source}</span>
                            <span>•</span>
                            <span>{formatTime(lead.createdAt)}</span>
                            {lead.updatedAt !== lead.createdAt && (
                              <>
                                <span>•</span>
                                <span>Actualizado: {formatTime(lead.updatedAt)}</span>
                              </>
                            )}
                          </div>
                        </div>
                      </div>

                      <div className="flex shrink-0 items-center gap-2">
                        {lead.phone && (
                          <Button variant="ghost" size="icon" title="Llamar" asChild>
                            <a href={`tel:${lead.phone}`}>
                              <Phone className="h-4 w-4" />
                            </a>
                          </Button>
                        )}
                        <Button variant="ghost" size="icon" title="Email" asChild>
                          <a href={`mailto:${lead.email}`}>
                            <Mail className="h-4 w-4" />
                          </a>
                        </Button>
                        {lead.phone && (
                          <Button variant="ghost" size="icon" title="WhatsApp" asChild>
                            <a
                              href={`https://wa.me/${lead.phone.replace(/[^0-9]/g, '')}`}
                              target="_blank"
                            >
                              <MessageSquare className="h-4 w-4" />
                            </a>
                          </Button>
                        )}
                        <Button variant="ghost" size="icon" title="Agendar Cita">
                          <Calendar className="h-4 w-4" />
                        </Button>
                        <Button variant="outline" size="sm" asChild>
                          <Link href={`/dealer/leads/${lead.id}`}>
                            Ver <ChevronRight className="ml-1 h-4 w-4" />
                          </Link>
                        </Button>
                      </div>
                    </div>

                    {lead.notes && (
                      <div className="border-border mt-3 border-t pt-3">
                        <p className="text-muted-foreground text-sm">
                          <span className="font-medium">Nota:</span> {lead.notes}
                        </p>
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          {!isLoading && filteredLeads.length === 0 && (
            <Card>
              <CardContent className="p-12 text-center">
                <Users className="mx-auto mb-4 h-12 w-12 text-gray-300" />
                <h3 className="text-foreground mb-2 text-lg font-medium">No hay leads</h3>
                <p className="text-muted-foreground">
                  {search || statusFilter !== 'all'
                    ? 'No se encontraron leads con los filtros aplicados'
                    : 'Aún no tienes leads. Los leads aparecerán aquí cuando los clientes te contacten.'}
                </p>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="new">
          <LeadsTabContent
            leads={leads?.filter(l => l.status === 'New') || []}
            isLoading={isLoading}
            emptyMessage="No hay leads nuevos"
          />
        </TabsContent>

        <TabsContent value="active">
          <LeadsTabContent
            leads={
              leads?.filter(l =>
                ['Contacted', 'Qualified', 'Proposal', 'Negotiating'].includes(l.status)
              ) || []
            }
            isLoading={isLoading}
            emptyMessage="No hay leads activos"
          />
        </TabsContent>

        <TabsContent value="closed">
          <LeadsTabContent
            leads={leads?.filter(l => ['Won', 'Lost'].includes(l.status)) || []}
            isLoading={isLoading}
            emptyMessage="No hay leads cerrados"
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}

// Helper component for tab content
function LeadsTabContent({
  leads,
  isLoading,
  emptyMessage,
}: {
  leads: LeadDto[];
  isLoading: boolean;
  emptyMessage: string;
}) {
  if (isLoading) {
    return <LeadsListSkeleton />;
  }

  if (leads.length === 0) {
    return (
      <Card>
        <CardContent className="p-12 text-center">
          <Users className="mx-auto mb-4 h-12 w-12 text-gray-300" />
          <h3 className="text-foreground mb-2 text-lg font-medium">{emptyMessage}</h3>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-3">
      {leads.map(lead => (
        <Card key={lead.id} className="transition-shadow hover:shadow-md">
          <CardContent className="p-4">
            <div className="flex items-center justify-between gap-4">
              <div className="flex flex-1 items-center gap-4">
                <div className="bg-muted flex h-12 w-12 shrink-0 items-center justify-center rounded-full">
                  <span className="text-muted-foreground text-lg font-medium">
                    {lead.firstName.charAt(0)}
                  </span>
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <Link
                      href={`/dealer/leads/${lead.id}`}
                      className="hover:text-primary font-semibold transition-colors"
                    >
                      {formatLeadName(lead)}
                    </Link>
                    <Badge className={getLeadStatusColor(lead.status)}>{lead.status}</Badge>
                    <span className={`text-sm font-medium ${getLeadScoreColor(lead.score)}`}>
                      {lead.score}%
                    </span>
                  </div>
                  <p className="text-muted-foreground truncate text-sm">{lead.email}</p>
                  <p className="text-muted-foreground text-xs">{lead.source}</p>
                </div>
              </div>
              <Button variant="outline" size="sm" asChild>
                <Link href={`/dealer/leads/${lead.id}`}>
                  Ver <ChevronRight className="ml-1 h-4 w-4" />
                </Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

export default function LeadsPage() {
  return (
    <PlanGate feature="leadManagement">
      <LeadsPageContent />
    </PlanGate>
  );
}
