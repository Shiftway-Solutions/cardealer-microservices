'use client';

import { useState, useEffect, useCallback } from 'react';
import Link from 'next/link';
import { csrfFetch } from '@/lib/security/csrf';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  MessageSquare,
  Mail,
  Clock,
  Phone,
  ArrowLeft,
  Search,
  Filter,
  ChevronLeft,
  ChevronRight,
  Send,
  CheckCircle,
  AlertCircle,
  User,
} from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { PlanGate } from '@/components/plan/plan-gate';

interface Lead {
  id: string;
  vehicleId: string;
  vehicleTitle: string;
  vehiclePrice: number;
  vehicleImageUrl?: string;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  message: string;
  status: string;
  source: string;
  createdAt: string;
  messagesCount?: number;
}

interface LeadMessage {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: string;
  content: string;
  isRead: boolean;
  createdAt: string;
}

interface LeadStats {
  total: number;
  new: number;
  contacted: number;
  negotiating: number;
  closed: number;
  lost: number;
}

function SellerLeadsContent() {
  const [leads, setLeads] = useState<Lead[]>([]);
  const [stats, setStats] = useState<LeadStats>({
    total: 0,
    new: 0,
    contacted: 0,
    negotiating: 0,
    closed: 0,
    lost: 0,
  });
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');

  // Lead detail dialog
  const [selectedLead, setSelectedLead] = useState<Lead | null>(null);
  const [leadMessages, setLeadMessages] = useState<LeadMessage[]>([]);
  const [replyText, setReplyText] = useState('');
  const [replying, setReplying] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);

  const loadLeads = useCallback(async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: '10',
      });
      if (statusFilter !== 'all') params.set('status', statusFilter);
      if (searchQuery) params.set('search', searchQuery);

      const res = await fetch(`/api/leads/seller/me?${params}`);
      if (res.ok) {
        const data = await res.json();
        const result = data.data ?? data;
        setLeads(result.items ?? []);
        setTotalPages(result.totalPages ?? 1);
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') console.error('Error loading leads:', error);
    } finally {
      setLoading(false);
    }
  }, [page, statusFilter, searchQuery]);

  const loadStats = useCallback(async () => {
    try {
      const res = await fetch('/api/leads/seller/me/stats');
      if (res.ok) {
        const data = await res.json();
        setStats(data.data ?? data);
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') console.error('Error loading stats:', error);
    }
  }, []);

  useEffect(() => {
    loadLeads();
    loadStats();
  }, [loadLeads, loadStats]);

  async function openLeadDetail(lead: Lead) {
    setSelectedLead(lead);
    setDialogOpen(true);
    setReplyText('');
    try {
      const res = await fetch(`/api/leads/${lead.id}/messages`);
      if (res.ok) {
        const data = await res.json();
        setLeadMessages(data.data ?? data ?? []);
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') console.error('Error loading messages:', error);
    }
  }

  async function sendReply() {
    if (!selectedLead || !replyText.trim()) return;
    setReplying(true);
    try {
      const res = await csrfFetch(`/api/leads/${selectedLead.id}/reply`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: replyText.trim() }),
      });
      if (res.ok) {
        setReplyText('');
        // Reload messages
        const msgRes = await fetch(`/api/leads/${selectedLead.id}/messages`);
        if (msgRes.ok) {
          const data = await msgRes.json();
          setLeadMessages(data.data ?? data ?? []);
        }
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') console.error('Error sending reply:', error);
    } finally {
      setReplying(false);
    }
  }

  async function updateLeadStatus(leadId: string, newStatus: string) {
    try {
      await csrfFetch(`/api/leads/${leadId}/status`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: newStatus }),
      });
      loadLeads();
      loadStats();
    } catch (error) {
      if (process.env.NODE_ENV === 'development') console.error('Error updating status:', error);
    }
  }

  const getStatusBadge = (status: string) => {
    const config: Record<
      string,
      {
        variant: 'default' | 'success' | 'secondary' | 'outline' | 'warning' | 'primary' | 'danger';
        label: string;
        icon: typeof CheckCircle;
      }
    > = {
      New: { variant: 'primary', label: 'Nuevo', icon: AlertCircle },
      Contacted: { variant: 'secondary', label: 'Contactado', icon: Phone },
      Negotiating: { variant: 'outline', label: 'Negociando', icon: MessageSquare },
      Closed: { variant: 'success', label: 'Cerrado', icon: CheckCircle },
      Lost: { variant: 'danger', label: 'Perdido', icon: AlertCircle },
      Spam: { variant: 'danger', label: 'Spam', icon: AlertCircle },
    };
    const c = config[status] || { variant: 'outline' as const, label: status, icon: AlertCircle };
    return <Badge variant={c.variant}>{c.label}</Badge>;
  };

  const statItems = [
    { label: 'Total', value: stats.total, color: 'text-gray-900' },
    { label: 'Nuevos', value: stats.new, color: 'text-blue-600' },
    { label: 'Contactados', value: stats.contacted, color: 'text-yellow-600' },
    { label: 'Negociando', value: stats.negotiating, color: 'text-purple-600' },
    { label: 'Cerrados', value: stats.closed, color: 'text-green-600' },
    { label: 'Perdidos', value: stats.lost, color: 'text-red-600' },
  ];

  return (
    <div className="container mx-auto space-y-6 px-4 py-8">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild aria-label="Volver">
          <Link href="/vender/dashboard">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">Leads</h1>
          <p className="text-muted-foreground">Gestiona las consultas de compradores interesados</p>
        </div>
      </div>

      {/* Stats Row */}
      <div className="grid grid-cols-3 gap-3 md:grid-cols-6">
        {statItems.map(item => (
          <Card key={item.label}>
            <CardContent className="p-4 text-center">
              <p className={`text-2xl font-bold ${item.color}`}>{item.value}</p>
              <p className="text-muted-foreground text-xs">{item.label}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
          <Input
            placeholder="Buscar por nombre, email o vehículo..."
            className="pl-10"
            value={searchQuery}
            onChange={e => {
              setSearchQuery(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <Select
          value={statusFilter}
          onValueChange={val => {
            setStatusFilter(val);
            setPage(1);
          }}
        >
          <SelectTrigger className="w-full sm:w-[180px]">
            <Filter className="mr-2 h-4 w-4" />
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos</SelectItem>
            <SelectItem value="New">Nuevos</SelectItem>
            <SelectItem value="Contacted">Contactados</SelectItem>
            <SelectItem value="Negotiating">Negociando</SelectItem>
            <SelectItem value="Closed">Cerrados</SelectItem>
            <SelectItem value="Lost">Perdidos</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Leads List */}
      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="space-y-4 p-8">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex animate-pulse gap-4 p-4">
                  <div className="h-12 w-12 rounded-full bg-gray-200" />
                  <div className="flex-1 space-y-2">
                    <div className="h-4 w-1/3 rounded bg-gray-200" />
                    <div className="h-3 w-2/3 rounded bg-gray-200" />
                  </div>
                </div>
              ))}
            </div>
          ) : leads.length === 0 ? (
            <div className="py-16 text-center">
              <MessageSquare className="text-muted-foreground/40 mx-auto h-14 w-14" />
              <h3 className="mt-4 text-lg font-medium">Sin leads</h3>
              <p className="text-muted-foreground mt-1">
                {statusFilter !== 'all'
                  ? 'No hay leads con este filtro'
                  : 'Los compradores interesados aparecerán aquí'}
              </p>
            </div>
          ) : (
            <div className="divide-y">
              {leads.map(lead => (
                <button
                  key={lead.id}
                  onClick={() => openLeadDetail(lead)}
                  className="hover:bg-accent/50 flex w-full items-start gap-4 p-4 text-left transition-colors"
                >
                  <div className="bg-primary/10 text-primary mt-0.5 shrink-0 rounded-full p-2.5">
                    <User className="h-5 w-5" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium">{lead.buyerName}</span>
                      {getStatusBadge(lead.status)}
                      {lead.status === 'New' && (
                        <span className="h-2 w-2 animate-pulse rounded-full bg-blue-500" />
                      )}
                    </div>
                    <p className="text-primary/80 mt-0.5 truncate text-sm font-medium">
                      {lead.vehicleTitle}
                    </p>
                    <p className="text-muted-foreground mt-0.5 line-clamp-2 text-sm">
                      {lead.message}
                    </p>
                    <div className="text-muted-foreground mt-2 flex items-center gap-3 text-xs">
                      <span className="flex items-center gap-1">
                        <Mail className="h-3 w-3" />
                        {lead.buyerEmail}
                      </span>
                      {lead.buyerPhone && (
                        <span className="flex items-center gap-1">
                          <Phone className="h-3 w-3" />
                          {lead.buyerPhone}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="text-muted-foreground shrink-0 text-xs whitespace-nowrap">
                    <Clock className="mr-1 inline h-3 w-3" />
                    {new Date(lead.createdAt).toLocaleDateString('es-DO', {
                      day: 'numeric',
                      month: 'short',
                      year: 'numeric',
                    })}
                  </div>
                </button>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage(p => Math.max(1, p - 1))}
            aria-label="Página anterior"
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <span className="text-muted-foreground text-sm">
            Página {page} de {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage(p => p + 1)}
            aria-label="Página siguiente"
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      )}

      {/* Lead Detail Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[80vh] overflow-y-auto sm:max-w-[600px]">
          {selectedLead && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2">
                  Lead de {selectedLead.buyerName}
                  {getStatusBadge(selectedLead.status)}
                </DialogTitle>
                <DialogDescription>
                  Interesado en: {selectedLead.vehicleTitle} — RD$
                  {selectedLead.vehiclePrice?.toLocaleString('es-DO')}
                </DialogDescription>
              </DialogHeader>

              {/* Contact Info */}
              <div className="bg-muted/50 space-y-2 rounded-lg p-4">
                <div className="flex items-center gap-2 text-sm">
                  <Mail className="text-muted-foreground h-4 w-4" />
                  <a
                    href={`mailto:${selectedLead.buyerEmail}`}
                    className="text-primary hover:underline"
                  >
                    {selectedLead.buyerEmail}
                  </a>
                </div>
                {selectedLead.buyerPhone && (
                  <div className="flex items-center gap-2 text-sm">
                    <Phone className="text-muted-foreground h-4 w-4" />
                    <a
                      href={`tel:${selectedLead.buyerPhone}`}
                      className="text-primary hover:underline"
                    >
                      {selectedLead.buyerPhone}
                    </a>
                  </div>
                )}
                <div className="text-muted-foreground flex items-center gap-2 text-sm">
                  <Clock className="h-4 w-4" />
                  {new Date(selectedLead.createdAt).toLocaleString('es-DO')}
                </div>
              </div>

              {/* Status Update */}
              <div className="flex items-center gap-2">
                <span className="text-muted-foreground text-sm">Cambiar estado:</span>
                <Select
                  value={selectedLead.status}
                  onValueChange={val => {
                    updateLeadStatus(selectedLead.id, val);
                    setSelectedLead({ ...selectedLead, status: val });
                  }}
                >
                  <SelectTrigger className="w-[160px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="New">Nuevo</SelectItem>
                    <SelectItem value="Contacted">Contactado</SelectItem>
                    <SelectItem value="Negotiating">Negociando</SelectItem>
                    <SelectItem value="Closed">Cerrado</SelectItem>
                    <SelectItem value="Lost">Perdido</SelectItem>
                    <SelectItem value="Spam">Spam</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Message Thread */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium">Conversación</h4>

                {/* Initial Message */}
                <div className="rounded-lg bg-blue-50 p-3">
                  <div className="mb-1 flex items-center gap-2">
                    <span className="text-sm font-medium">{selectedLead.buyerName}</span>
                    <span className="text-muted-foreground text-xs">
                      {new Date(selectedLead.createdAt).toLocaleString('es-DO')}
                    </span>
                  </div>
                  <p className="text-sm">{selectedLead.message}</p>
                </div>

                {/* Replies */}
                {leadMessages.map(msg => (
                  <div
                    key={msg.id}
                    className={`rounded-lg p-3 ${
                      msg.senderRole === 'Seller' ? 'ml-6 bg-green-50' : 'bg-blue-50'
                    }`}
                  >
                    <div className="mb-1 flex items-center gap-2">
                      <span className="text-sm font-medium">{msg.senderName}</span>
                      <Badge variant="outline" className="text-xs">
                        {msg.senderRole === 'Seller' ? 'Tú' : 'Comprador'}
                      </Badge>
                      <span className="text-muted-foreground text-xs">
                        {new Date(msg.createdAt).toLocaleString('es-DO')}
                      </span>
                    </div>
                    <p className="text-sm">{msg.content}</p>
                  </div>
                ))}
              </div>

              {/* Reply Input */}
              <div className="flex gap-2">
                <Textarea
                  placeholder="Escribe tu respuesta..."
                  value={replyText}
                  onChange={e => setReplyText(e.target.value)}
                  rows={2}
                  className="resize-none"
                />
                <Button
                  size="icon"
                  onClick={sendReply}
                  disabled={!replyText.trim() || replying}
                  className="shrink-0 self-end"
                  aria-label="Enviar respuesta"
                >
                  <Send className="h-4 w-4" />
                </Button>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default function SellerLeadsPage() {
  return (
    <PlanGate feature="leadManagement">
      <SellerLeadsContent />
    </PlanGate>
  );
}
