/**
 * Review Card Component
 * Displays a single review with rating, content, and actions
 */

'use client';

import * as React from 'react';
import { CheckCircle, ThumbsUp, Flag, MessageSquare } from 'lucide-react';
import { StarRating } from './star-rating';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { cn, formatDate } from '@/lib/utils';
import { useVoteReview } from '@/hooks/use-reviews';
import Image from 'next/image';
import type { Review } from '@/services/reviews';

interface ReviewCardProps {
  review: Review;
  className?: string;
}

function getInitials(name: string): string {
  return name
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

export function ReviewCard({ review, className }: ReviewCardProps) {
  const voteMutation = useVoteReview();

  const handleVote = (vote: 'helpful' | 'notHelpful') => {
    if (review.hasVoted) return;
    voteMutation.mutate({ reviewId: review.id, vote });
  };

  return (
    <div className={cn('border-border border-b pb-6 last:border-0', className)}>
      {/* Header: Avatar + Name + Date + Rating */}
      <div className="flex items-start gap-3">
        {/* Avatar */}
        <div className="bg-primary/10 text-primary flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full text-sm font-semibold">
          {review.reviewerAvatarUrl ? (
            <Image
              src={review.reviewerAvatarUrl}
              alt={review.reviewerName}
              width={40}
              height={40}
              sizes="40px"
              className="h-10 w-10 rounded-full object-cover"
            />
          ) : (
            getInitials(review.reviewerName)
          )}
        </div>

        {/* Name + date */}
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-foreground font-medium">{review.reviewerName}</span>
            {review.isVerifiedPurchase && (
              <Badge variant="secondary" className="gap-1 text-xs">
                <CheckCircle className="h-3 w-3 text-green-600" />
                Compra Verificada
              </Badge>
            )}
          </div>
          <div className="mt-0.5 flex items-center gap-2">
            <StarRating value={review.overallRating} readonly size="sm" />
            <span className="text-muted-foreground text-xs">{formatDate(review.createdAt)}</span>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="mt-3 pl-[52px]">
        {review.title && <h4 className="text-foreground mb-1 font-semibold">{review.title}</h4>}
        <p className="text-muted-foreground text-sm leading-relaxed">{review.content}</p>

        {/* Pros/Cons */}
        {review.pros && review.pros.length > 0 && (
          <div className="mt-2">
            <span className="text-xs font-medium text-green-700">Pros: </span>
            <span className="text-muted-foreground text-xs">{review.pros.join(', ')}</span>
          </div>
        )}
        {review.cons && review.cons.length > 0 && (
          <div className="mt-1">
            <span className="text-xs font-medium text-red-700">Contras: </span>
            <span className="text-muted-foreground text-xs">{review.cons.join(', ')}</span>
          </div>
        )}

        {/* Seller/Dealer Response */}
        {review.response && (
          <div className="bg-muted mt-3 rounded-lg p-3">
            <div className="mb-1 flex items-center gap-1.5">
              <MessageSquare className="text-primary h-3.5 w-3.5" />
              <span className="text-xs font-medium">Respuesta del vendedor</span>
              <span className="text-muted-foreground text-xs">
                · {formatDate(review.response.respondedAt)}
              </span>
            </div>
            <p className="text-muted-foreground text-sm">{review.response.content}</p>
          </div>
        )}

        {/* Actions */}
        <div className="mt-3 flex items-center gap-4">
          <Button
            variant="ghost"
            size="sm"
            className="text-muted-foreground h-auto gap-1.5 px-2 py-1 text-xs"
            onClick={() => handleVote('helpful')}
            disabled={review.hasVoted || voteMutation.isPending}
          >
            <ThumbsUp
              className={cn(
                'h-3.5 w-3.5',
                review.userVote === 'helpful' && 'text-primary fill-current'
              )}
            />
            Útil ({review.helpfulCount})
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="text-muted-foreground h-auto gap-1.5 px-2 py-1 text-xs"
          >
            <Flag className="h-3.5 w-3.5" />
            Reportar
          </Button>
        </div>
      </div>
    </div>
  );
}

export default ReviewCard;
