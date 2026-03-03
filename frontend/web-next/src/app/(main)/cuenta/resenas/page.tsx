/**
 * Seller Reviews Management Page
 *
 * Allows individual seller accounts to:
 *  - View all reviews they've received
 *  - See aggregated statistics
 *  - Respond publicly to reviews
 *
 * Route: /cuenta/resenas
 * Access: seller  (dealer accounts are redirected to /dealer by layout.tsx)
 *
 * AUDIT: Created to complete the review flow.
 */

'use client';

import Image from 'next/image';
import * as React from 'react';
import { toast } from 'sonner';
import {
  Star,
  MessageSquare,
  Reply,
  CheckCircle,
  AlertCircle,
  Loader2,
  ChevronDown,
  ChevronUp,
  RefreshCw,
} from 'lucide-react';
import { useAuth } from '@/hooks/use-auth';
import { useSellerByUserId } from '@/hooks/use-seller';
import { useReviewsForTarget, useReviewStats, useRespondToReview } from '@/hooks/use-reviews';
import { ReviewSummaryBar } from '@/components/reviews/review-summary-bar';
import { StarRating } from '@/components/reviews/star-rating';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import type { Review } from '@/services/reviews';

// ────────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────────

function formatDate(date: string) {
  return new Date(date).toLocaleDateString('es-DO', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function getInitials(name: string) {
  return name
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

// ────────────────────────────────────────────────────────────────────────────
// ReviewResponseForm — inline form for responding to a review
// ────────────────────────────────────────────────────────────────────────────

interface ReviewResponseFormProps {
  reviewId: string;
  sellerId: string;
  onClose: () => void;
}

function ReviewResponseForm({ reviewId, sellerId, onClose }: ReviewResponseFormProps) {
  const [text, setText] = React.useState('');
  const respondMutation = useRespondToReview();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = text.trim();
    if (trimmed.length < 10) {
      toast.error('La respuesta debe tener al menos 10 caracteres');
      return;
    }
    try {
      await respondMutation.mutateAsync({ reviewId, content: trimmed, sellerId });
      toast.success('¡Respuesta publicada!');
      onClose();
    } catch (err: unknown) {
      const error = err as { message?: string };
      toast.error('Error al publicar', { description: error?.message ?? 'Inténtalo de nuevo.' });
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mt-3 space-y-2">
      <Textarea
        placeholder="Escribe tu respuesta pública... (mínimo 10 caracteres)"
        value={text}
        onChange={e => setText(e.target.value)}
        rows={3}
        className="resize-none"
        autoFocus
        maxLength={1000}
      />
      <div className="flex items-center justify-between">
        <span className="text-muted-foreground text-xs">{text.length}/1000</span>
        <div className="flex gap-2">
          <Button type="button" variant="outline" size="sm" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" size="sm" disabled={respondMutation.isPending}>
            {respondMutation.isPending ? (
              <>
                <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
                Publicando…
              </>
            ) : (
              <>
                <Reply className="mr-1.5 h-3.5 w-3.5" />
                Publicar
              </>
            )}
          </Button>
        </div>
      </div>
    </form>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// ReviewManagementCard — single review with respond capability
// ────────────────────────────────────────────────────────────────────────────

interface ReviewManagementCardProps {
  review: Review;
  sellerId: string;
}

function ReviewManagementCard({ review, sellerId }: ReviewManagementCardProps) {
  const [respondOpen, setRespondOpen] = React.useState(false);
  const [expanded, setExpanded] = React.useState(false);
  const hasResponse = !!review.response;
  const isLong = review.content.length > 200;

  return (
    <Card
      className={cn(
        'transition-colors',
        hasResponse ? 'border-green-100 bg-green-50/30' : 'border-amber-100 bg-amber-50/20'
      )}
    >
      <CardContent className="pt-5">
        {/* Reviewer header */}
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-start gap-3">
            {/* Avatar */}
            <div className="bg-primary/10 text-primary flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full text-sm font-semibold">
              {review.reviewerAvatarUrl ? (
                <Image
                  src={review.reviewerAvatarUrl}
                  alt=""
                  width={40}
                  height={40}
                  className="h-10 w-10 rounded-full object-cover"
                />
              ) : (
                getInitials(review.reviewerName || 'U')
              )}
            </div>

            {/* Name + date + rating */}
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-medium">{review.reviewerName}</span>
                {review.isVerifiedPurchase && (
                  <Badge variant="secondary" className="gap-1 text-xs">
                    <CheckCircle className="h-3 w-3 text-green-600" />
                    Compra verificada
                  </Badge>
                )}
              </div>
              <div className="mt-0.5 flex items-center gap-2">
                <StarRating value={review.overallRating} readonly size="sm" />
                <span className="text-muted-foreground text-xs">
                  {formatDate(review.createdAt)}
                </span>
              </div>
            </div>
          </div>

          {/* Status badge */}
          {hasResponse ? (
            <Badge variant="outline" className="gap-1 border-green-300 text-green-700">
              <MessageSquare className="h-3 w-3" />
              Respondida
            </Badge>
          ) : (
            <Badge variant="outline" className="gap-1 border-amber-300 text-amber-700">
              <AlertCircle className="h-3 w-3" />
              Sin respuesta
            </Badge>
          )}
        </div>

        {/* Review content */}
        <div className="mt-3 pl-[52px]">
          {review.title && <h4 className="text-foreground mb-1 font-semibold">{review.title}</h4>}
          <p
            className={cn(
              'text-muted-foreground text-sm leading-relaxed',
              !expanded && isLong && 'line-clamp-3'
            )}
          >
            {review.content}
          </p>
          {isLong && (
            <button
              onClick={() => setExpanded(!expanded)}
              className="text-primary mt-1 flex items-center gap-0.5 text-xs hover:underline"
            >
              {expanded ? (
                <>
                  <ChevronUp className="h-3 w-3" /> Ver menos
                </>
              ) : (
                <>
                  <ChevronDown className="h-3 w-3" /> Ver más
                </>
              )}
            </button>
          )}

          {/* Existing response */}
          {hasResponse && review.response && (
            <div className="bg-muted mt-3 rounded-lg p-3">
              <div className="mb-1 flex items-center gap-1.5">
                <Reply className="text-primary h-3.5 w-3.5" />
                <span className="text-xs font-medium">Tu respuesta</span>
                <span className="text-muted-foreground text-xs">
                  · {formatDate(review.response.respondedAt)}
                </span>
              </div>
              <p className="text-muted-foreground text-sm">{review.response.content}</p>
            </div>
          )}

          {/* Respond form or button */}
          {!hasResponse && !respondOpen && (
            <Button
              variant="outline"
              size="sm"
              className="mt-3 gap-1.5"
              onClick={() => setRespondOpen(true)}
              data-testid={`respond-btn-${review.id}`}
            >
              <Reply className="h-3.5 w-3.5" />
              Responder
            </Button>
          )}

          {respondOpen && (
            <ReviewResponseForm
              reviewId={review.id}
              sellerId={sellerId}
              onClose={() => setRespondOpen(false)}
            />
          )}
        </div>
      </CardContent>
    </Card>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────────

type SortOption = 'newest' | 'oldest' | 'highest' | 'lowest';

export default function SellerResenasPage() {
  const { user } = useAuth();
  const { data: sellerProfile, isLoading: sellerLoading } = useSellerByUserId(user?.id);

  const [sortBy, setSortBy] = React.useState<SortOption>('newest');
  const [filterRating, setFilterRating] = React.useState<number | undefined>();
  const [pendingOnly, setPendingOnly] = React.useState(false);

  const sellerId = sellerProfile?.id;

  const {
    data: reviewsData,
    isLoading: reviewsLoading,
    refetch,
    isRefetching,
  } = useReviewsForTarget('seller', sellerId, { sortBy, rating: filterRating, pageSize: 100 });

  const { data: stats, isLoading: statsLoading } = useReviewStats('seller', sellerId);

  const allReviews = reviewsData?.items ?? [];
  const reviews = pendingOnly ? allReviews.filter(r => !r.response) : allReviews;
  const pendingCount = allReviews.filter(r => !r.response).length;
  const isLoading = sellerLoading || reviewsLoading;

  if (!sellerLoading && !sellerId) {
    return (
      <div className="py-12 text-center">
        <Star className="text-muted-foreground mx-auto mb-3 h-12 w-12" />
        <h2 className="text-foreground mb-2 text-xl font-semibold">
          Perfil de vendedor no encontrado
        </h2>
        <p className="text-muted-foreground">
          Activa tu cuenta de vendedor para acceder a las reseñas.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Mis Reseñas</h1>
          <p className="text-muted-foreground mt-1">
            Gestiona y responde las reseñas de los compradores
            {(sellerProfile?.displayName ?? sellerProfile?.fullName)
              ? ` en "${sellerProfile.displayName ?? sellerProfile.fullName}"`
              : ''}
            .
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          disabled={isRefetching}
          className="gap-1.5"
        >
          <RefreshCw className={cn('h-3.5 w-3.5', isRefetching && 'animate-spin')} />
          Actualizar
        </Button>
      </div>

      {sellerLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map(i => (
            <Skeleton key={i} className="h-44 rounded-xl" />
          ))}
        </div>
      ) : (
        <>
          {/* Stats */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Star className="h-4 w-4 text-amber-500" />
                Resumen
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ReviewSummaryBar stats={stats} isLoading={statsLoading} />
            </CardContent>
          </Card>

          {/* Pending alert */}
          {pendingCount > 0 && !pendingOnly && (
            <div
              className="flex cursor-pointer items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
              onClick={() => setPendingOnly(true)}
            >
              <AlertCircle className="h-4 w-4 flex-shrink-0 text-amber-500" />
              <span>
                Tienes <strong>{pendingCount}</strong>{' '}
                {pendingCount === 1 ? 'reseña sin responder' : 'reseñas sin responder'}.{' '}
                <span className="underline">Ver pendientes</span>
              </span>
            </div>
          )}

          {/* Sort & filter */}
          <div className="flex flex-wrap items-center gap-2 rounded-lg border p-3">
            <button
              onClick={() => setPendingOnly(!pendingOnly)}
              className={cn(
                'flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-colors',
                pendingOnly
                  ? 'bg-amber-500 text-white'
                  : 'bg-muted text-muted-foreground hover:bg-muted/80'
              )}
            >
              <AlertCircle className="h-3 w-3" />
              Sin respuesta ({pendingCount})
            </button>
            <div className="bg-border mx-1 h-4 w-px" />
            <span className="text-muted-foreground text-xs">Ordenar:</span>
            {(['newest', 'oldest', 'highest', 'lowest'] as const).map(opt => {
              const labels = {
                newest: 'Recientes',
                oldest: 'Antiguas',
                highest: 'Mayor ★',
                lowest: 'Menor ★',
              };
              return (
                <button
                  key={opt}
                  onClick={() => setSortBy(opt)}
                  className={cn(
                    'rounded-full px-3 py-1 text-xs font-medium transition-colors',
                    sortBy === opt
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted text-muted-foreground hover:bg-muted/80'
                  )}
                >
                  {labels[opt]}
                </button>
              );
            })}
            <div className="bg-border mx-1 h-4 w-px" />
            {[5, 4, 3, 2, 1].map(r => (
              <button
                key={r}
                onClick={() => setFilterRating(filterRating === r ? undefined : r)}
                className={cn(
                  'flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium transition-colors',
                  filterRating === r
                    ? 'bg-amber-500 text-white'
                    : 'bg-muted text-muted-foreground hover:bg-muted/80'
                )}
              >
                <Star className="h-3 w-3" />
                {r}
              </button>
            ))}
            {(filterRating || pendingOnly) && (
              <button
                onClick={() => {
                  setFilterRating(undefined);
                  setPendingOnly(false);
                }}
                className="text-muted-foreground hover:text-foreground ml-auto text-xs underline"
              >
                Limpiar
              </button>
            )}
            <span className="text-muted-foreground ml-auto text-xs">
              {reviews.length} {reviews.length === 1 ? 'reseña' : 'reseñas'}
            </span>
          </div>

          {/* Reviews list */}
          {isLoading ? (
            <div className="space-y-4">
              {[1, 2, 3].map(i => (
                <Skeleton key={i} className="h-44 rounded-xl" />
              ))}
            </div>
          ) : reviews.length === 0 ? (
            <Card>
              <CardContent className="py-14 text-center">
                <Star className="text-muted-foreground/30 mx-auto mb-3 h-14 w-14" />
                <p className="text-muted-foreground font-medium">
                  {pendingOnly
                    ? '¡Todas las reseñas han sido respondidas!'
                    : filterRating
                      ? `No hay reseñas con ${filterRating} ★`
                      : 'Todavía no tienes reseñas'}
                </p>
                {!pendingOnly && !filterRating && (
                  <p className="text-muted-foreground/60 mt-1 text-sm">
                    Aparecerán aquí cuando los compradores compartan su experiencia.
                  </p>
                )}
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-4">
              {reviews.map(review => (
                <ReviewManagementCard key={review.id} review={review} sellerId={sellerId!} />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}
