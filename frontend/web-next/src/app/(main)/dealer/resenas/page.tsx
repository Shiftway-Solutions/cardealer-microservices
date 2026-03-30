/**
 * Dealer Reviews Management Page
 *
 * Allows dealer users and dealer employees to:
 *  - View all reviews received on their dealer profile
 *  - See aggregated statistics (average rating + distribution)
 *  - Respond publicly to any review that doesn't have a response yet
 *
 * Route: /dealer/resenas
 * Access: dealer | dealer_employee  (enforced by /dealer/layout.tsx AuthGuard)
 *
 * AUDIT: Created to complete the review flow — without this page dealers had
 *        no way to see or respond to reviews in their portal.
 */

'use client';

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
import { useCurrentDealer } from '@/hooks/use-dealers';
import { useReviewsForTarget, useReviewStats, useRespondToReview } from '@/hooks/use-reviews';
import { ReviewSummaryBar } from '@/components/reviews/review-summary-bar';
import { StarRating } from '@/components/reviews/star-rating';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import Image from 'next/image';
import { cn, formatDate } from '@/lib/utils';
import { VideoHelpButton } from '@/components/dealer/video-help-button';
import type { Review } from '@/services/reviews';

// ────────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────────

function getInitials(name: string) {
  return name
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

// ────────────────────────────────────────────────────────────────────────────
// ReviewResponseForm — inline response form
// ────────────────────────────────────────────────────────────────────────────

interface ReviewResponseFormProps {
  reviewId: string;
  dealerId: string;
  onClose: () => void;
}

function ReviewResponseForm({ reviewId, dealerId, onClose }: ReviewResponseFormProps) {
  const [text, setText] = React.useState('');
  const respondMutation = useRespondToReview();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = text.trim();
    if (trimmed.length < 10) {
      toast.error('La respuesta debe tener al menos 10 caracteres');
      return;
    }

    try {
      await respondMutation.mutateAsync({
        reviewId,
        content: trimmed,
        sellerId: dealerId,
      });
      toast.success('¡Respuesta publicada!', {
        description: 'Los compradores ya pueden ver tu respuesta.',
      });
      onClose();
    } catch (err: unknown) {
      const error = err as { message?: string };
      toast.error('Error al publicar', {
        description: error?.message ?? 'Inténtalo de nuevo más tarde.',
      });
    }
  };

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
                Publicar respuesta
              </>
            )}
          </Button>
        </div>
      </div>
    </form>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// ReviewManagementCard
// ────────────────────────────────────────────────────────────────────────────

interface ReviewManagementCardProps {
  review: Review;
  dealerId: string;
}

function ReviewManagementCard({ review, dealerId }: ReviewManagementCardProps) {
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
        {/* Header: reviewer info + status */}
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-start gap-3">
            {/* Avatar */}
            <div className="bg-primary/10 text-primary flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full text-sm font-semibold">
              {review.reviewerAvatarUrl ? (
                <Image
                  src={review.reviewerAvatarUrl}
                  alt={review.reviewerName ?? 'Usuario'}
                  width={40}
                  height={40}
                  className="h-10 w-10 rounded-full object-cover"
                />
              ) : (
                getInitials(review.reviewerName || 'U')
              )}
            </div>

            {/* Name, date, stars */}
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-medium">{review.reviewerName ?? 'Usuario'}</span>
                {review.isVerifiedPurchase && (
                  <Badge variant="secondary" className="gap-1 text-xs">
                    <CheckCircle className="h-3 w-3 text-green-600" />
                    Verificado
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

          {/* Pending / Responded badge */}
          {hasResponse ? (
            <Badge variant="outline" className="shrink-0 gap-1 border-green-300 text-green-700">
              <MessageSquare className="h-3 w-3" />
              Respondida
            </Badge>
          ) : (
            <Badge variant="outline" className="shrink-0 gap-1 border-amber-300 text-amber-700">
              <AlertCircle className="h-3 w-3" />
              Sin respuesta
            </Badge>
          )}
        </div>

        {/* Review body */}
        <div className="mt-3 pl-[52px]">
          {review.title && <h4 className="mb-1 font-semibold">{review.title}</h4>}

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
                <span className="text-xs font-medium">Respuesta del concesionario</span>
                <span className="text-muted-foreground text-xs">
                  · {formatDate(review.response.respondedAt)}
                </span>
              </div>
              <p className="text-muted-foreground text-sm">{review.response.content}</p>
            </div>
          )}

          {/* Respond button / form */}
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
              dealerId={dealerId}
              onClose={() => setRespondOpen(false)}
            />
          )}
        </div>
      </CardContent>
    </Card>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Filter Bar
// ────────────────────────────────────────────────────────────────────────────

type SortOption = 'newest' | 'oldest' | 'highest' | 'lowest';

interface FilterBarProps {
  sortBy: SortOption;
  onSortChange: (s: SortOption) => void;
  filterRating: number | undefined;
  onFilterRating: (r: number | undefined) => void;
  pendingOnly: boolean;
  onPendingOnly: (v: boolean) => void;
  total: number;
  pending: number;
}

function FilterBar({
  sortBy,
  onSortChange,
  filterRating,
  onFilterRating,
  pendingOnly,
  onPendingOnly,
  total,
  pending,
}: FilterBarProps) {
  const sortLabels: Record<SortOption, string> = {
    newest: 'Más recientes',
    oldest: 'Más antiguas',
    highest: 'Mayor puntuación',
    lowest: 'Menor puntuación',
  };

  return (
    <div className="flex flex-wrap items-center gap-2 rounded-lg border p-3">
      {/* Pending filter */}
      <button
        onClick={() => onPendingOnly(!pendingOnly)}
        className={cn(
          'flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-colors',
          pendingOnly
            ? 'bg-amber-500 text-white'
            : 'bg-muted text-muted-foreground hover:bg-muted/80'
        )}
        data-testid="filter-pending"
      >
        <AlertCircle className="h-3 w-3" />
        Sin respuesta ({pending})
      </button>

      <div className="bg-border mx-1 h-4 w-px" />

      {/* Sort */}
      <span className="text-muted-foreground text-xs">Ordenar:</span>
      {(['newest', 'oldest', 'highest', 'lowest'] as const).map(opt => (
        <button
          key={opt}
          onClick={() => onSortChange(opt)}
          className={cn(
            'rounded-full px-3 py-1 text-xs font-medium transition-colors',
            sortBy === opt
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground hover:bg-muted/80'
          )}
        >
          {sortLabels[opt]}
        </button>
      ))}

      <div className="bg-border mx-1 h-4 w-px" />

      {/* Star filter */}
      <span className="text-muted-foreground text-xs">Estrellas:</span>
      {[5, 4, 3, 2, 1].map(r => (
        <button
          key={r}
          onClick={() => onFilterRating(filterRating === r ? undefined : r)}
          className={cn(
            'flex items-center gap-1 rounded-full px-3 py-1 text-xs font-medium transition-colors',
            filterRating === r
              ? 'bg-amber-500 text-white'
              : 'bg-muted text-muted-foreground hover:bg-muted/80'
          )}
        >
          <Star className="h-3 w-3" />
          {r}
        </button>
      ))}

      {/* Clear filter */}
      {(filterRating || pendingOnly) && (
        <button
          onClick={() => {
            onFilterRating(undefined);
            onPendingOnly(false);
          }}
          className="text-muted-foreground hover:text-foreground ml-auto text-xs underline"
        >
          Limpiar filtros
        </button>
      )}

      <span className="text-muted-foreground ml-auto text-xs">
        {total} {total === 1 ? 'reseña' : 'reseñas'}
      </span>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────────

export default function DealerResenasPage() {
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();

  const [sortBy, setSortBy] = React.useState<SortOption>('newest');
  const [filterRating, setFilterRating] = React.useState<number | undefined>();
  const [pendingOnly, setPendingOnly] = React.useState(false);

  const dealerId = dealer?.id;

  const {
    data: reviewsData,
    isLoading: reviewsLoading,
    refetch,
    isRefetching,
  } = useReviewsForTarget('seller', dealerId, {
    sortBy,
    rating: filterRating,
    pageSize: 100,
  });

  const { data: stats, isLoading: statsLoading } = useReviewStats('seller', dealerId);

  const allReviews = reviewsData?.items ?? [];

  // Client-side pending filter (doesn't require extra API call)
  const reviews = pendingOnly ? allReviews.filter(r => !r.response) : allReviews;
  const pendingCount = allReviews.filter(r => !r.response).length;

  const isLoading = dealerLoading || reviewsLoading;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-foreground text-2xl font-bold">Reseñas</h1>
            <VideoHelpButton sectionKey="resenas" variant="icon" />
          </div>
          <p className="text-muted-foreground mt-1">
            Gestiona y responde las reseñas de tu concesionario
            {dealer?.businessName ? ` "${dealer.businessName}"` : ''}.
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

      {!dealerId && dealerLoading ? (
        // Loading skeleton
        <div className="space-y-4">
          <Skeleton className="h-32 rounded-xl" />
          <Skeleton className="h-48 rounded-xl" />
          <Skeleton className="h-48 rounded-xl" />
        </div>
      ) : !dealerId ? (
        <Card>
          <CardContent className="py-10 text-center">
            <AlertCircle className="text-muted-foreground/50 mx-auto mb-3 h-10 w-10" />
            <p className="text-muted-foreground">No se encontró el perfil del concesionario.</p>
          </CardContent>
        </Card>
      ) : (
        <>
          {/* Stats card */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <Star className="h-4 w-4 text-amber-500" />
                Resumen de reseñas
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
                {pendingCount === 1 ? 'reseña sin respuesta' : 'reseñas sin respuesta'}.{' '}
                <span className="underline">Ver solo pendientes</span>
              </span>
            </div>
          )}

          {/* Filters */}
          <FilterBar
            sortBy={sortBy}
            onSortChange={setSortBy}
            filterRating={filterRating}
            onFilterRating={setFilterRating}
            pendingOnly={pendingOnly}
            onPendingOnly={setPendingOnly}
            total={reviews.length}
            pending={pendingCount}
          />

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
                      ? `No hay reseñas con ${filterRating} estrellas`
                      : 'Aún no tienes reseñas'}
                </p>
                <p className="text-muted-foreground/60 mt-1 text-sm">
                  {!pendingOnly &&
                    !filterRating &&
                    'Las reseñas aparecerán aquí cuando los compradores compartan su experiencia.'}
                </p>
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-4">
              {reviews.map(review => (
                <ReviewManagementCard key={review.id} review={review} dealerId={dealerId} />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}
