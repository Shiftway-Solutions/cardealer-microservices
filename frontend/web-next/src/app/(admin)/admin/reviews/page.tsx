/**
 * Admin Reviews Moderation Page
 *
 * Moderate dealer and seller reviews
 */

'use client';

import { useState } from 'react';
import { formatDateTime } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  Search,
  Star,
  Flag,
  CheckCircle,
  XCircle,
  Clock,
  MessageSquare,
  AlertTriangle,
  Eye,
  Trash2,
} from 'lucide-react';
import {
  useAdminReviews,
  useReportedReviews,
  useAdminReviewStats,
  useApproveReview,
  useRejectReview,
} from '@/hooks/use-admin-extended';
import type { AdminReview } from '@/services/admin-extended';

export default function ReviewsPage() {
  const [selectedReview, setSelectedReview] = useState<AdminReview | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [activeTab, setActiveTab] = useState('pending');

  const { data: pendingData } = useAdminReviews();
  const pendingReviews = pendingData?.items ?? [];
  const { data: reportedReviews = [] } = useReportedReviews();
  const { data: stats } = useAdminReviewStats();
  const approveReview = useApproveReview();
  const rejectReview = useRejectReview();

  // Use centralized formatDateTime from @/lib/utils
  const formatDate = formatDateTime;

  const renderStars = (rating: number) => {
    return (
      <div className="flex">
        {[1, 2, 3, 4, 5].map(star => (
          <Star
            key={star}
            className={`h-4 w-4 ${
              star <= rating ? 'fill-current text-yellow-400' : 'text-muted-foreground'
            }`}
          />
        ))}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold">Moderación de Reviews</h1>
        <p className="text-muted-foreground">Aprueba, rechaza o elimina reviews de usuarios</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-yellow-900 p-2">
                <Clock className="h-5 w-5 text-yellow-400" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Pendientes</p>
                <p className="text-2xl font-bold text-white">
                  {stats?.pendingReviews ?? pendingReviews.length}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-900 p-2">
                <Flag className="h-5 w-5 text-red-400" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Reportados</p>
                <p className="text-2xl font-bold text-white">
                  {stats?.reportedReviews ?? reportedReviews.length}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="bg-primary/95 rounded-lg p-2">
                <CheckCircle className="text-primary/80 h-5 w-5" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Aprobados Hoy</p>
                <p className="text-2xl font-bold text-white">{stats?.approvedReviews ?? '—'}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-900 p-2">
                <Trash2 className="h-5 w-5 text-purple-400" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Total Reviews</p>
                <p className="text-2xl font-bold text-white">{stats?.totalReviews ?? '—'}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card className="border-slate-700 bg-slate-800">
        <CardContent className="py-4">
          <div className="flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <Input
                placeholder="Buscar por contenido, autor o destinatario..."
                className="border-slate-600 bg-slate-700 pl-10"
              />
            </div>
            <Select defaultValue="all">
              <SelectTrigger className="w-40 border-slate-600 bg-slate-700">
                <SelectValue placeholder="Rating" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="5">5 estrellas</SelectItem>
                <SelectItem value="4">4 estrellas</SelectItem>
                <SelectItem value="3">3 estrellas</SelectItem>
                <SelectItem value="2">2 estrellas</SelectItem>
                <SelectItem value="1">1 estrella</SelectItem>
              </SelectContent>
            </Select>
            <Select defaultValue="all">
              <SelectTrigger className="w-40 border-slate-600 bg-slate-700">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="dealer">Dealer</SelectItem>
                <SelectItem value="seller">Vendedor</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="border-slate-700 bg-slate-800">
          <TabsTrigger value="pending">
            Pendientes
            <Badge className="ml-2 bg-yellow-600">
              {stats?.pendingReviews ?? pendingReviews.length}
            </Badge>
          </TabsTrigger>
          <TabsTrigger value="reported">
            Reportados
            <Badge className="ml-2 bg-red-600">
              {stats?.reportedReviews ?? reportedReviews.length}
            </Badge>
          </TabsTrigger>
        </TabsList>

        <TabsContent value="pending" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-white">Reviews Pendientes</CardTitle>
              <CardDescription>Reviews esperando moderación</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {pendingReviews.map(review => (
                  <div key={review.id} className="rounded-lg bg-slate-700/50 p-4">
                    <div className="mb-3 flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <Avatar className="h-10 w-10">
                          <AvatarImage src={review.authorAvatar || undefined} />
                          <AvatarFallback className="bg-slate-600">
                            {review.authorName
                              .split(' ')
                              .map(n => n[0])
                              .join('')}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-medium text-white">{review.authorName}</p>
                          <p className="text-muted-foreground text-sm">
                            Review para:{' '}
                            <span className="text-primary/80">{review.targetName}</span>
                            <Badge variant="outline" className="ml-2 text-xs">
                              {review.targetType === 'dealer' ? 'Dealer' : 'Vendedor'}
                            </Badge>
                          </p>
                        </div>
                      </div>
                      <div className="text-right">
                        {renderStars(review.rating)}
                        <p className="text-muted-foreground mt-1 text-xs">
                          {formatDate(review.createdAt)}
                        </p>
                      </div>
                    </div>

                    <div className="mb-3">
                      <p className="mb-1 font-medium text-white">{review.title}</p>
                      <p className="text-sm text-gray-300">{review.comment}</p>
                    </div>

                    {review.reports.includes('reported') && (
                      <div className="mb-3 rounded-lg border border-red-600/30 bg-red-900/20 p-2">
                        <p className="flex items-center gap-2 text-sm text-red-400">
                          <AlertTriangle className="h-4 w-4" />
                          Este review ha sido reportado
                        </p>
                      </div>
                    )}

                    <div className="flex justify-end gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setSelectedReview(review);
                          setDialogOpen(true);
                        }}
                      >
                        <Eye className="mr-1 h-4 w-4" />
                        Ver Más
                      </Button>
                      <Button
                        size="sm"
                        className="bg-primary hover:bg-primary/90"
                        onClick={() => approveReview.mutate(review.id)}
                        disabled={approveReview.isPending}
                      >
                        <CheckCircle className="mr-1 h-4 w-4" />
                        Aprobar
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="border-red-600 text-red-400"
                        onClick={() => rejectReview.mutate({ id: review.id })}
                        disabled={rejectReview.isPending}
                      >
                        <XCircle className="mr-1 h-4 w-4" />
                        Rechazar
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="reported" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-white">Reviews Reportados</CardTitle>
              <CardDescription>Reviews marcados como inapropiados</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {reportedReviews.map(review => (
                  <div
                    key={review.id}
                    className="rounded-lg border border-red-600/30 bg-slate-700/50 p-4"
                  >
                    <div className="mb-3 flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <Avatar className="h-10 w-10">
                          <AvatarImage src={review.authorAvatar || undefined} />
                          <AvatarFallback className="bg-slate-600">
                            {review.authorName
                              .split(' ')
                              .map(n => n[0])
                              .join('')}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-medium text-white">{review.authorName}</p>
                          <p className="text-muted-foreground text-sm">
                            Review para:{' '}
                            <span className="text-primary/80">{review.targetName}</span>
                          </p>
                        </div>
                      </div>
                      <div className="text-right">
                        {renderStars(review.rating)}
                        <p className="text-muted-foreground mt-1 text-xs">
                          {formatDate(review.createdAt)}
                        </p>
                      </div>
                    </div>

                    <div className="mb-3">
                      <p className="mb-1 font-medium text-white">{review.title}</p>
                      <p className="text-sm text-gray-300">{review.comment}</p>
                    </div>

                    <div className="mb-3 rounded-lg border border-red-600/30 bg-red-900/20 p-3">
                      <p className="mb-1 text-sm font-medium text-red-400">
                        <Flag className="mr-1 inline h-4 w-4" />
                        {review.reportCount} reportes
                      </p>
                      <p className="text-muted-foreground text-sm">
                        Razón: {review.reportReasons.join(', ')}
                      </p>
                      <p className="text-muted-foreground mt-1 text-xs">
                        {formatDate(review.lastReportedAt)}
                      </p>
                    </div>

                    <div className="flex justify-end gap-2">
                      <Button variant="outline" size="sm">
                        <MessageSquare className="mr-1 h-4 w-4" />
                        Contactar
                      </Button>
                      <Button size="sm" className="bg-primary hover:bg-primary/90">
                        <CheckCircle className="mr-1 h-4 w-4" />
                        Mantener
                      </Button>
                      <Button variant="outline" size="sm" className="border-red-600 text-red-400">
                        <Trash2 className="mr-1 h-4 w-4" />
                        Eliminar
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Review Detail Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-2xl border-slate-700 bg-slate-800">
          <DialogHeader>
            <DialogTitle className="text-white">Detalle del Review</DialogTitle>
          </DialogHeader>

          {selectedReview && (
            <div className="space-y-4">
              <div className="flex items-center gap-3 border-b border-slate-700 pb-4">
                <Avatar className="h-12 w-12">
                  <AvatarImage src={selectedReview.authorAvatar || undefined} />
                  <AvatarFallback className="bg-slate-600">
                    {selectedReview.authorName
                      .split(' ')
                      .map(n => n[0])
                      .join('')}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-white">{selectedReview.authorName}</p>
                  <div className="flex items-center gap-2">
                    {renderStars(selectedReview.rating)}
                    <span className="text-muted-foreground text-sm">
                      {formatDate(selectedReview.createdAt)}
                    </span>
                  </div>
                </div>
              </div>

              <div>
                <p className="text-muted-foreground mb-1 text-sm">Review para</p>
                <p className="font-medium text-white">{selectedReview.targetName}</p>
              </div>

              <div>
                <p className="text-muted-foreground mb-1 text-sm">Título</p>
                <p className="font-medium text-white">{selectedReview.title}</p>
              </div>

              <div>
                <p className="text-muted-foreground mb-1 text-sm">Contenido</p>
                <p className="text-gray-300">{selectedReview.comment}</p>
              </div>

              <div>
                <p className="text-muted-foreground mb-2 text-sm">Nota de moderación (opcional)</p>
                <Textarea
                  placeholder="Agregar nota interna..."
                  className="border-slate-600 bg-slate-700"
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Cancelar
            </Button>
            <Button variant="outline" className="border-red-600 text-red-400">
              <Trash2 className="mr-2 h-4 w-4" />
              Eliminar
            </Button>
            <Button className="bg-primary hover:bg-primary/90">
              <CheckCircle className="mr-2 h-4 w-4" />
              Aprobar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
