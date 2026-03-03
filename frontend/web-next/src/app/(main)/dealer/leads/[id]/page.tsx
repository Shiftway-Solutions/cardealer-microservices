/**
 * Dealer Lead Detail Page
 *
 * View and manage individual lead/inquiry details
 * Connected to real APIs - P1 Integration
 */

'use client';

import { useState } from 'react';
import { useParams } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Skeleton } from '@/components/ui/skeleton';
import {
  ArrowLeft,
  User,
  Mail,
  Phone,
  Car,
  Calendar,
  Clock,
  MessageSquare,
  Send,
  CheckCircle,
  Star,
  Archive,
  MoreVertical,
  AlertCircle,
  RefreshCw,
  Briefcase,
  Target,
  Tag,
} from 'lucide-react';
import Link from 'next/link';
import { useLead, useUpdateLead } from '@/hooks/use-crm';
import type { LeadDto, LeadStatus } from '@/services/crm';
import { PlanGate } from '@/components/plan/plan-gate';
import { toast } from 'sonner';
import { sanitizeText } from '@/lib/security/sanitize';

// ============================================================================
// Skeleton Components
// ============================================================================

function LeadDetailSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Skeleton className="h-10 w-10" />
        <div>
          <Skeleton className="mb-2 h-8 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardContent className="pt-6">
              <Skeleton className="mb-4 h-6 w-40" />
              <Skeleton className="mb-2 h-20 w-full" />
              <Skeleton className="h-20 w-full" />
            </CardContent>
          </Card>
        </div>
        <div className="space-y-6">
          {[1, 2, 3].map(i => (
            <Card key={i}>
              <CardContent className="pt-6">
                <Skeleton className="mb-3 h-5 w-32" />
                <Skeleton className="mb-2 h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Helper Functions
// ============================================================================

function getStatusBadge(status: string) {
  const styles: Record<string, string> = {
    New: 'bg-blue-100 text-blue-700',
    Contacted: 'bg-yellow-100 text-yellow-700',
    Qualified: 'bg-purple-100 text-purple-700',
    Proposal: 'bg-indigo-100 text-indigo-700',
    Negotiating: 'bg-orange-100 text-orange-700',
    Won: 'bg-primary/10 text-primary',
    Lost: 'bg-red-100 text-red-700',
  };
  return <Badge className={styles[status] || 'bg-gray-100 text-gray-700'}>{status}</Badge>;
}

function getScoreBadge(score: number) {
  if (score >= 80) return <Badge className="bg-primary/10 text-primary">Alto ({score})</Badge>;
  if (score >= 50) return <Badge className="bg-yellow-100 text-yellow-700">Medio ({score})</Badge>;
  return <Badge className="bg-red-100 text-red-700">Bajo ({score})</Badge>;
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString('es-DO', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// ============================================================================
// Main Page Component
// ============================================================================

function LeadDetailContent() {
  const params = useParams();
  const leadId = params.id as string;
  const [notes, setNotes] = useState('');

  // Get lead data from API
  const { data: lead, isLoading, error, refetch } = useLead(leadId);
  const updateLead = useUpdateLead();

  const handleUpdateStatus = async (newStatus: LeadStatus) => {
    if (!lead) return;
    try {
      await updateLead.mutateAsync({
        id: lead.id,
        request: {
          firstName: lead.firstName,
          lastName: lead.lastName,
          email: lead.email,
          phone: lead.phone,
          company: lead.company,
          jobTitle: lead.jobTitle,
          status: newStatus,
          score: lead.score,
          estimatedValue: lead.estimatedValue,
        },
      });
      toast.success(`Lead marcado como ${newStatus}`);
    } catch {
      toast.error('Error al actualizar el lead');
    }
  };

  const handleSaveNotes = async () => {
    if (!lead || !notes.trim()) return;
    const sanitized = sanitizeText(notes.trim(), { maxLength: 1000 });
    try {
      await updateLead.mutateAsync({
        id: lead.id,
        request: {
          firstName: lead.firstName,
          lastName: lead.lastName,
          email: lead.email,
          phone: lead.phone,
          company: lead.company,
          jobTitle: lead.jobTitle,
          status: lead.status,
          score: lead.score,
          estimatedValue: lead.estimatedValue,
        },
      });
      toast.success('Notas guardadas');
      setNotes('');
    } catch {
      toast.error('Error al guardar las notas');
    }
  };

  // Error state
  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Link href="/dealer/leads">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <h1 className="text-2xl font-bold">Detalle del Lead</h1>
        </div>
        <Card>
          <CardContent className="py-12 text-center">
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-red-400" />
            <p className="mb-2 font-medium text-red-600">Error al cargar el lead</p>
            <p className="text-muted-foreground mb-4 text-sm">
              No se pudo obtener la información del lead.
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

  if (isLoading || !lead) {
    return <LeadDetailSkeleton />;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/dealer/leads">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Detalle del Lead</h1>
            <p className="text-muted-foreground">{lead.fullName}</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Actualizar
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Main Content */}
        <div className="space-y-6 lg:col-span-2">
          {/* Notes */}
          {lead.notes && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5" />
                  Mensaje / Notas
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="bg-muted/50 rounded-lg p-4">
                  <p className="text-sm whitespace-pre-wrap">{lead.notes}</p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Add Notes */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Send className="h-5 w-5" />
                Agregar Nota
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Textarea
                placeholder="Escribe una nota sobre este lead..."
                value={notes}
                onChange={e => setNotes(e.target.value)}
                rows={3}
              />
              <div className="mt-2 flex justify-end">
                <Button
                  className="bg-primary hover:bg-primary/90"
                  onClick={handleSaveNotes}
                  disabled={!notes.trim() || updateLead.isPending}
                >
                  <Send className="mr-2 h-4 w-4" />
                  {updateLead.isPending ? 'Guardando...' : 'Guardar Nota'}
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Tags */}
          {lead.tags && lead.tags.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-base">
                  <Tag className="h-5 w-5" />
                  Etiquetas
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  {lead.tags.map((tag, i) => (
                    <Badge key={i} variant="outline">
                      {tag}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Contact Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <User className="h-5 w-5" />
                Contacto
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-3">
                <Avatar className="h-12 w-12">
                  <AvatarFallback>
                    {lead.firstName?.[0]}
                    {lead.lastName?.[0]}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium">{lead.fullName}</p>
                  {lead.jobTitle && (
                    <p className="text-muted-foreground text-xs">{lead.jobTitle}</p>
                  )}
                </div>
              </div>
              <div className="space-y-2 text-sm">
                <div className="text-muted-foreground flex items-center gap-2">
                  <Mail className="h-4 w-4" />
                  <a href={`mailto:${lead.email}`} className="hover:text-primary">
                    {lead.email}
                  </a>
                </div>
                {lead.phone && (
                  <div className="text-muted-foreground flex items-center gap-2">
                    <Phone className="h-4 w-4" />
                    <a href={`tel:${lead.phone}`} className="hover:text-primary">
                      {lead.phone}
                    </a>
                  </div>
                )}
                {lead.company && (
                  <div className="text-muted-foreground flex items-center gap-2">
                    <Briefcase className="h-4 w-4" />
                    <span>{lead.company}</span>
                  </div>
                )}
              </div>
              <div className="flex gap-2">
                {lead.phone && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="flex-1"
                    onClick={() => window.open(`tel:${lead.phone}`)}
                  >
                    <Phone className="mr-1 h-4 w-4" />
                    Llamar
                  </Button>
                )}
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1"
                  onClick={() => window.open(`mailto:${lead.email}`)}
                >
                  <Mail className="mr-1 h-4 w-4" />
                  Email
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Lead Info */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Información del Lead</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Estado</span>
                {getStatusBadge(lead.status)}
              </div>
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Puntuación</span>
                {getScoreBadge(lead.score)}
              </div>
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Fuente</span>
                <span className="font-medium">{lead.source}</span>
              </div>
              {lead.estimatedValue && (
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">Valor Estimado</span>
                  <span className="font-medium text-primary">
                    RD$ {lead.estimatedValue.toLocaleString()}
                  </span>
                </div>
              )}
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Creado</span>
                <span className="font-medium">{formatDate(lead.createdAt)}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Actualizado</span>
                <span className="font-medium">{formatDate(lead.updatedAt)}</span>
              </div>
              {lead.convertedAt && (
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">Convertido</span>
                  <span className="font-medium text-primary">
                    {formatDate(lead.convertedAt)}
                  </span>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Actions */}
          <Card>
            <CardContent className="space-y-2 p-4">
              {lead.status !== 'Won' && lead.status !== 'Lost' && (
                <>
                  {lead.status === 'New' && (
                    <Button
                      className="w-full bg-primary hover:bg-primary/90"
                      onClick={() => handleUpdateStatus('Contacted')}
                      disabled={updateLead.isPending}
                    >
                      <CheckCircle className="mr-2 h-4 w-4" />
                      Marcar como Contactado
                    </Button>
                  )}
                  {lead.status === 'Contacted' && (
                    <Button
                      className="w-full bg-purple-600 hover:bg-purple-700"
                      onClick={() => handleUpdateStatus('Qualified')}
                      disabled={updateLead.isPending}
                    >
                      <Target className="mr-2 h-4 w-4" />
                      Marcar como Calificado
                    </Button>
                  )}
                  {(lead.status === 'Qualified' || lead.status === 'Proposal') && (
                    <Button
                      className="w-full bg-orange-600 hover:bg-orange-700"
                      onClick={() => handleUpdateStatus('Negotiating')}
                      disabled={updateLead.isPending}
                    >
                      <Star className="mr-2 h-4 w-4" />
                      Marcar como Negociando
                    </Button>
                  )}
                  {lead.status === 'Negotiating' && (
                    <Button
                      className="w-full bg-primary hover:bg-primary/90"
                      onClick={() => handleUpdateStatus('Won')}
                      disabled={updateLead.isPending}
                    >
                      <CheckCircle className="mr-2 h-4 w-4" />
                      Marcar como Ganado
                    </Button>
                  )}
                  <Button
                    variant="outline"
                    className="w-full text-red-600 hover:text-red-700"
                    onClick={() => handleUpdateStatus('Lost')}
                    disabled={updateLead.isPending}
                  >
                    <Archive className="mr-2 h-4 w-4" />
                    Marcar como Perdido
                  </Button>
                </>
              )}
              <Link href="/dealer/citas" className="block">
                <Button variant="outline" className="w-full">
                  <Calendar className="mr-2 h-4 w-4" />
                  Agendar Cita
                </Button>
              </Link>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

export default function LeadDetailPage() {
  return (
    <PlanGate feature="leadManagement">
      <LeadDetailContent />
    </PlanGate>
  );
}
