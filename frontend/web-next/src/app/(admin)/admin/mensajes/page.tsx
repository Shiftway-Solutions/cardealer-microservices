/**
 * Admin Messages Page
 *
 * View and manage platform messages/support tickets
 */

'use client';

import { useState, useMemo } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { TicketStatusBadge, PriorityBadge } from '@/components/dashboard';
import { Search, Mail, MailOpen, Clock, AlertCircle, CheckCircle } from 'lucide-react';
import { useAdminMessages } from '@/hooks/use-admin-extended';
import type { AdminMessage } from '@/services/admin-extended';

// Status and Priority badges now use shared components from @/components/dashboard

export default function AdminMessagesPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const { data: messagesData, isLoading } = useAdminMessages();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const allMessages = messagesData?.items ?? [];

  const [_selectedTicket, setSelectedTicket] = useState<AdminMessage | null>(null);

  const filteredTickets = useMemo(
    () =>
      allMessages.filter(t => {
        if (statusFilter !== 'all' && t.status !== statusFilter) return false;
        if (searchQuery) {
          const query = searchQuery.toLowerCase();
          return (
            t.subject.toLowerCase().includes(query) ||
            t.senderName.toLowerCase().includes(query) ||
            (t.senderEmail?.toLowerCase().includes(query) ?? false)
          );
        }
        return true;
      }),
    [allMessages, statusFilter, searchQuery]
  );

  const openCount = allMessages.filter(t => t.status === 'new').length;
  const pendingCount = allMessages.filter(t => t.status === 'read').length;
  const highPriorityCount = allMessages.filter(
    t => t.priority === 'high' && t.status !== 'resolved'
  ).length;
  const resolvedCount = allMessages.filter(t => t.status === 'resolved').length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold">Mensajes</h1>
          <p className="text-muted-foreground">Soporte y comunicación con usuarios</p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Mail className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{isLoading ? '—' : openCount}</p>
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
                <p className="text-2xl font-bold">{isLoading ? '—' : pendingCount}</p>
                <p className="text-muted-foreground text-sm">Pendientes</p>
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
                <p className="text-2xl font-bold">{isLoading ? '—' : highPriorityCount}</p>
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
                <p className="text-2xl font-bold">{isLoading ? '—' : resolvedCount}</p>
                <p className="text-muted-foreground text-sm">Resueltos</p>
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
            placeholder="Buscar por asunto, usuario, email..."
            className="pl-10"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
          />
        </div>
        <div className="flex gap-2">
          {['all', 'open', 'pending', 'resolved'].map(status => (
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
                  : status === 'pending'
                    ? 'Pendientes'
                    : 'Resueltos'}
            </Button>
          ))}
        </div>
      </div>

      {/* Tickets List */}
      <Card>
        <CardContent className="p-0">
          <div className="divide-y">
            {filteredTickets.map(ticket => (
              <div
                key={ticket.id}
                className={`hover:bg-muted/50 cursor-pointer p-4 transition-colors ${
                  ticket.status === 'new' ? 'bg-blue-50/50' : ''
                }`}
                onClick={() => setSelectedTicket(ticket)}
              >
                <div className="flex items-start gap-4">
                  <div className="flex-shrink-0 pt-1">
                    {ticket.status === 'new' ? (
                      <Mail className="h-5 w-5 text-blue-600" />
                    ) : (
                      <MailOpen className="text-muted-foreground h-5 w-5" />
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="mb-1 flex items-center gap-2">
                      <h4
                        className={`font-medium ${ticket.status === 'new' ? 'text-foreground' : 'text-muted-foreground'}`}
                      >
                        {ticket.subject}
                      </h4>
                      <PriorityBadge priority={ticket.priority} />
                    </div>
                    <p className="text-muted-foreground mb-2 text-sm">
                      <span className="font-medium">{ticket.senderName}</span>
                      <span className="mx-2">•</span>
                      {ticket.senderEmail}
                      <span className="mx-2">•</span>
                      <Badge variant="outline" className="text-xs">
                        {ticket.senderType === 'dealer' ? 'Dealer' : 'Usuario'}
                      </Badge>
                    </p>
                    <p className="text-muted-foreground truncate text-sm">{ticket.preview}</p>
                  </div>
                  <div className="flex flex-col items-end gap-2">
                    <TicketStatusBadge status={ticket.status} />
                    <span className="text-muted-foreground text-xs">
                      {new Date(ticket.createdAt).toLocaleDateString('es-DO')}
                    </span>
                    <span className="text-muted-foreground text-xs">
                      {ticket.messagesCount} mensajes
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Quick Reply Modal would go here */}
    </div>
  );
}
