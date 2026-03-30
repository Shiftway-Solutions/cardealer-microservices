/**
 * Admin Support Ticket Detail Page
 *
 * Individual support ticket management
 */

'use client';

import { useState } from 'react';
import { useParams } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ArrowLeft,
  User,
  Mail,
  MessageSquare,
  Send,
  CheckCircle,
  AlertTriangle,
  Paperclip,
  Phone,
  Tag,
  ArrowUpRight,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';
import {
  useSupportTicket,
  useReplySupportTicket,
  useUpdateTicketStatus,
} from '@/hooks/use-admin-extended';

export default function SupportTicketDetailPage() {
  const params = useParams();
  const ticketId = params?.id as string;
  const [reply, setReply] = useState('');

  const { data: ticket, isLoading } = useSupportTicket(ticketId);
  const replyMutation = useReplySupportTicket();
  const updateStatus = useUpdateTicketStatus();

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('es-DO', {
      day: 'numeric',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'open':
        return <Badge className="bg-blue-600">Abierto</Badge>;
      case 'in-progress':
        return <Badge className="bg-yellow-600">En Progreso</Badge>;
      case 'waiting':
        return <Badge className="bg-purple-600">Esperando Usuario</Badge>;
      case 'resolved':
        return <Badge className="bg-primary">Resuelto</Badge>;
      case 'closed':
        return <Badge className="bg-muted-foreground">Cerrado</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority) {
      case 'high':
        return <Badge className="bg-red-600">Alta</Badge>;
      case 'medium':
        return <Badge className="bg-yellow-600">Media</Badge>;
      case 'low':
        return <Badge className="bg-muted-foreground">Baja</Badge>;
      default:
        return <Badge variant="outline">{priority}</Badge>;
    }
  };

  const handleSendReply = () => {
    if (!reply.trim() || !ticketId) return;
    replyMutation.mutate({ id: ticketId, message: reply }, { onSuccess: () => setReply('') });
  };

  if (isLoading || !ticket) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/admin/soporte">
            <Button variant="ghost" size="icon" aria-label="Volver a soporte">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold">{ticket.id}</h1>
              {getStatusBadge(ticket.status)}
              {getPriorityBadge(ticket.priority)}
            </div>
            <p className="text-muted-foreground">{ticket.subject}</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <ArrowUpRight className="mr-2 h-4 w-4" />
            Escalar
          </Button>
          <Button
            className="bg-primary hover:bg-primary/90"
            onClick={() => updateStatus.mutate({ id: ticketId, status: 'resolved' })}
            disabled={updateStatus.isPending}
          >
            <CheckCircle className="mr-2 h-4 w-4" />
            {updateStatus.isPending ? 'Guardando...' : 'Marcar Resuelto'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Conversation */}
        <div className="space-y-6 lg:col-span-2">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-white">
                <MessageSquare className="h-5 w-5" />
                Conversación
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="mb-4 max-h-[500px] space-y-4 overflow-y-auto">
                {ticket.messages.map(msg => {
                  if (msg.senderType === 'system') {
                    return (
                      <div key={msg.id} className="text-center">
                        <span className="text-muted-foreground rounded-full bg-slate-700 px-3 py-1 text-xs">
                          {msg.message} • {formatDate(msg.timestamp)}
                        </span>
                      </div>
                    );
                  }

                  const isAgent = msg.senderType === 'agent';

                  return (
                    <div key={msg.id} className={`flex gap-3 ${isAgent ? 'flex-row-reverse' : ''}`}>
                      <Avatar className="h-8 w-8 flex-shrink-0">
                        <AvatarFallback className={isAgent ? 'bg-primary' : 'bg-slate-600'}>
                          {isAgent
                            ? 'S'
                            : ticket.customer.name
                                .split(' ')
                                .map(n => n[0])
                                .join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div className={`max-w-md flex-1 ${isAgent ? 'text-right' : ''}`}>
                        <div className="mb-1 flex items-center gap-2">
                          {isAgent ? (
                            <>
                              <span className="text-muted-foreground text-xs">
                                {formatDate(msg.timestamp)}
                              </span>
                              <span className="text-primary/80 text-sm font-medium">
                                {msg.sender}
                              </span>
                            </>
                          ) : (
                            <>
                              <span className="text-sm font-medium text-gray-300">
                                {ticket.customer.name}
                              </span>
                              <span className="text-muted-foreground text-xs">
                                {formatDate(msg.timestamp)}
                              </span>
                            </>
                          )}
                        </div>
                        <div
                          className={`rounded-lg p-3 ${
                            isAgent
                              ? 'bg-primary/95/30 text-gray-200'
                              : 'bg-slate-700 text-gray-200'
                          }`}
                        >
                          <p className="text-sm">{msg.message}</p>
                          {msg.attachments && msg.attachments.length > 0 && (
                            <div className="mt-2 border-t border-slate-600 pt-2">
                              {msg.attachments.map((att, i) => (
                                <div
                                  key={i}
                                  className="text-muted-foreground flex items-center gap-2 text-xs"
                                >
                                  <Paperclip className="h-3 w-3" />
                                  <span>{att.name}</span>
                                  <span>({att.size})</span>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Reply Input */}
              <div className="border-t border-slate-700 pt-4">
                <Textarea
                  placeholder="Escribe tu respuesta..."
                  className="mb-2 border-slate-600 bg-slate-700"
                  value={reply}
                  onChange={e => setReply(e.target.value)}
                  rows={3}
                />
                <div className="flex items-center justify-between">
                  <Button variant="outline" size="sm">
                    <Paperclip className="mr-2 h-4 w-4" />
                    Adjuntar
                  </Button>
                  <div className="flex gap-2">
                    <Select defaultValue="public">
                      <SelectTrigger className="w-32 border-slate-600 bg-slate-700">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="public">Pública</SelectItem>
                        <SelectItem value="internal">Nota Interna</SelectItem>
                      </SelectContent>
                    </Select>
                    <Button
                      className="bg-primary hover:bg-primary/90"
                      onClick={handleSendReply}
                      disabled={!reply.trim() || replyMutation.isPending}
                    >
                      <Send className="mr-2 h-4 w-4" />
                      {replyMutation.isPending ? 'Enviando...' : 'Enviar'}
                    </Button>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* User Info */}
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base text-white">
                <User className="h-5 w-5" />
                Usuario
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-3">
                <Avatar className="h-12 w-12">
                  <AvatarFallback className="bg-slate-600">
                    {ticket.customer.name
                      .split(' ')
                      .map(n => n[0])
                      .join('')}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-white">{ticket.customer.name}</p>
                  <Badge variant="outline" className="text-xs">
                    {ticket.customer.accountType === 'seller' ? 'Vendedor' : 'Comprador'}
                  </Badge>
                </div>
              </div>
              <div className="space-y-2 text-sm">
                <div className="text-muted-foreground flex items-center gap-2">
                  <Mail className="h-4 w-4" />
                  <span>{ticket.customer.email}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Phone className="h-4 w-4" />
                  <span>{ticket.customer.phone}</span>
                </div>
              </div>
              <Link href={`/admin/usuarios/${ticket.id}`}>
                <Button variant="outline" size="sm" className="w-full">
                  Ver Perfil Completo
                </Button>
              </Link>
            </CardContent>
          </Card>

          {/* Ticket Details */}
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-base text-white">Detalles del Ticket</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between border-b border-slate-700 py-2">
                <span className="text-muted-foreground">Estado</span>
                <Select defaultValue={ticket.status}>
                  <SelectTrigger className="h-8 w-32 border-slate-600 bg-slate-700">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="open">Abierto</SelectItem>
                    <SelectItem value="in-progress">En Progreso</SelectItem>
                    <SelectItem value="waiting">Esperando</SelectItem>
                    <SelectItem value="resolved">Resuelto</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex justify-between border-b border-slate-700 py-2">
                <span className="text-muted-foreground">Prioridad</span>
                <Select defaultValue={ticket.priority}>
                  <SelectTrigger className="h-8 w-32 border-slate-600 bg-slate-700">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="low">Baja</SelectItem>
                    <SelectItem value="medium">Media</SelectItem>
                    <SelectItem value="high">Alta</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex justify-between border-b border-slate-700 py-2">
                <span className="text-muted-foreground">Categoría</span>
                <Badge variant="outline">Técnico</Badge>
              </div>
              <div className="flex justify-between border-b border-slate-700 py-2">
                <span className="text-muted-foreground">Creado</span>
                <span className="text-sm text-white">{formatDate(ticket.createdAt)}</span>
              </div>
              <div className="flex justify-between py-2">
                <span className="text-muted-foreground">Última actualización</span>
                <span className="text-sm text-white">{formatDate(ticket.updatedAt)}</span>
              </div>
            </CardContent>
          </Card>

          {/* Quick Actions */}
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-base text-white">Acciones Rápidas</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start">
                <Tag className="mr-2 h-4 w-4" />
                Agregar Etiqueta
              </Button>
              <Button variant="outline" className="w-full justify-start">
                <User className="mr-2 h-4 w-4" />
                Asignar Agente
              </Button>
              <Button variant="outline" className="w-full justify-start text-yellow-400">
                <AlertTriangle className="mr-2 h-4 w-4" />
                Reportar Problema
              </Button>
            </CardContent>
          </Card>

          {/* Related Tickets */}
          {ticket.relatedTickets.length > 0 && (
            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="text-base text-white">Tickets Relacionados</CardTitle>
              </CardHeader>
              <CardContent>
                {ticket.relatedTickets.map(related => (
                  <Link key={related.id} href={`/admin/soporte/${related.id}`}>
                    <div className="rounded-lg bg-slate-700/50 p-3 transition-colors hover:bg-slate-700">
                      <p className="text-sm font-medium text-white">{related.id}</p>
                      <p className="text-muted-foreground truncate text-xs">{related.subject}</p>
                      <Badge className="bg-primary mt-1 text-xs">Resuelto</Badge>
                    </div>
                  </Link>
                ))}
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
