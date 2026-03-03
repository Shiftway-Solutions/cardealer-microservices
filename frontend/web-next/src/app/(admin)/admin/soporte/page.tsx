/**
 * Admin Support Tickets Page
 *
 * View and manage support tickets
 */

'use client';

import { useState, useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import {
  Search,
  Ticket,
  Clock,
  CheckCircle,
  MessageSquare,
  AlertCircle,
  ChevronRight,
  Send,
} from 'lucide-react';
import {
  useSupportTickets,
  useTicketStats,
  useReplySupportTicket,
  useUpdateTicketStatus,
} from '@/hooks/use-admin-extended';
import type { SupportTicket } from '@/services/admin-extended';

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'open':
      return <Badge className="bg-blue-100 text-blue-700">Abierto</Badge>;
    case 'in_progress':
      return <Badge className="bg-amber-100 text-amber-700">En Progreso</Badge>;
    case 'resolved':
      return <Badge className="bg-primary/10 text-primary">Resuelto</Badge>;
    case 'closed':
      return <Badge variant="outline">Cerrado</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
};

const getCategoryBadge = (category: string) => {
  switch (category) {
    case 'technical':
      return <Badge variant="outline">Técnico</Badge>;
    case 'billing':
      return <Badge variant="outline">Facturación</Badge>;
    case 'account':
      return <Badge variant="outline">Cuenta</Badge>;
    case 'sales':
      return <Badge variant="outline">Ventas</Badge>;
    default:
      return <Badge variant="outline">{category}</Badge>;
  }
};

export default function AdminSupportPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [selectedTicket, setSelectedTicket] = useState<SupportTicket | null>(null);

  const { data: ticketsData } = useSupportTickets();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const allTickets = ticketsData?.items ?? [];
  const { data: ticketStats } = useTicketStats();

  const filteredTickets = useMemo(
    () =>
      allTickets.filter(ticket => {
        if (statusFilter !== 'all' && ticket.status !== statusFilter) return false;
        if (searchQuery) {
          const query = searchQuery.toLowerCase();
          return (
            ticket.subject?.toLowerCase().includes(query) ||
            ticket.userName?.toLowerCase().includes(query) ||
            ticket.ticketNumber?.toLowerCase().includes(query)
          );
        }
        return true;
      }),
    [allTickets, statusFilter, searchQuery]
  );

  const openCount = ticketStats?.open ?? allTickets.filter(t => t.status === 'open').length;
  const inProgressCount =
    ticketStats?.inProgress ?? allTickets.filter(t => t.status === 'in_progress').length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold">Soporte</h1>
          <p className="text-muted-foreground">Gestión de tickets de soporte</p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Ticket className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{openCount}</p>
                <p className="text-muted-foreground text-sm">Abiertos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <Clock className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{inProgressCount}</p>
                <p className="text-muted-foreground text-sm">En Progreso</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-100 p-2">
                <AlertCircle className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {allTickets.filter(t => t.priority === 'high' && t.status !== 'resolved').length}
                </p>
                <p className="text-muted-foreground text-sm">Alta Prioridad</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 rounded-lg p-2">
                <CheckCircle className="text-primary h-5 w-5" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {ticketStats?.resolved ?? allTickets.filter(t => t.status === 'resolved').length}
                </p>
                <p className="text-muted-foreground text-sm">Resueltos Hoy</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex gap-4">
        <div className="relative flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
          <Input
            placeholder="Buscar tickets..."
            className="pl-10"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
          />
        </div>
        <div className="flex gap-2">
          {['all', 'open', 'in_progress', 'resolved'].map(status => (
            <Button
              key={status}
              variant={statusFilter === status ? 'default' : 'outline'}
              size="sm"
              onClick={() => setStatusFilter(status)}
              className={statusFilter === status ? 'bg-primary' : ''}
            >
              {status === 'all'
                ? 'Todos'
                : status === 'open'
                  ? 'Abiertos'
                  : status === 'in_progress'
                    ? 'En Progreso'
                    : 'Resueltos'}
            </Button>
          ))}
        </div>
      </div>

      {/* Tickets List */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* List */}
        <div className="lg:col-span-2">
          <Card>
            <CardContent className="p-0">
              <div className="divide-y">
                {filteredTickets.map(ticket => (
                  <div
                    key={ticket.id}
                    className={`hover:bg-muted/50 cursor-pointer p-4 ${selectedTicket?.id === ticket.id ? 'bg-muted/50' : ''}`}
                    onClick={() => setSelectedTicket(ticket)}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="mb-1 flex items-center gap-2">
                          <span className="text-muted-foreground text-xs">
                            {ticket.ticketNumber}
                          </span>
                          {ticket.priority === 'high' && (
                            <Badge className="bg-red-100 text-xs text-red-700">Urgente</Badge>
                          )}
                        </div>
                        <h4 className="mb-1 font-medium">{ticket.subject}</h4>
                        <p className="text-muted-foreground text-sm">
                          {ticket.userName} • {ticket.userEmail}
                        </p>
                        <div className="mt-2 flex items-center gap-3">
                          {getStatusBadge(ticket.status)}
                          {getCategoryBadge(ticket.category)}
                          <span className="text-muted-foreground text-xs">
                            {ticket.messagesCount} mensajes
                          </span>
                        </div>
                      </div>
                      <ChevronRight className="text-muted-foreground h-5 w-5" />
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Detail */}
        <div className="lg:col-span-1">
          {selectedTicket ? (
            <Card className="sticky top-4">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg">{selectedTicket.ticketNumber}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <p className="text-muted-foreground text-sm">Asunto</p>
                  <p className="font-medium">{selectedTicket.subject}</p>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-muted-foreground text-sm">De</p>
                    <p className="font-medium">{selectedTicket.userName}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground text-sm">Estado</p>
                    {getStatusBadge(selectedTicket.status)}
                  </div>
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Última actualización</p>
                  <p className="text-sm">
                    {new Date(selectedTicket.updatedAt).toLocaleString('es-DO')}
                  </p>
                </div>

                <div className="border-border border-t pt-4">
                  <Textarea placeholder="Escribir respuesta..." className="mb-3" />
                  <Button className="bg-primary hover:bg-primary/90 w-full">
                    <Send className="mr-2 h-4 w-4" />
                    Enviar Respuesta
                  </Button>
                </div>

                <div className="flex gap-2">
                  {selectedTicket.status !== 'resolved' && (
                    <Button variant="outline" size="sm" className="flex-1">
                      <CheckCircle className="mr-1 h-4 w-4" />
                      Resolver
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="text-muted-foreground p-8 text-center">
                <MessageSquare className="mx-auto mb-4 h-12 w-12 opacity-50" />
                <p>Selecciona un ticket para ver detalles</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
