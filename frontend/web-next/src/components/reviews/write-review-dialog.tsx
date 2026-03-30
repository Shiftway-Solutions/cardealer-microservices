/**
 * Write Review Dialog Component
 * Modal form for submitting a review with star rating
 */

'use client';

import * as React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { StarRating } from './star-rating';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { useCreateReview } from '@/hooks/use-reviews';

const reviewSchema = z.object({
  overallRating: z.number().min(1, 'Selecciona una calificación').max(5),
  title: z
    .string()
    .min(5, 'El título debe tener al menos 5 caracteres')
    .max(100, 'El título no puede exceder 100 caracteres')
    .optional()
    .or(z.literal('')),
  content: z
    .string()
    .min(20, 'La reseña debe tener al menos 20 caracteres')
    .max(1000, 'La reseña no puede exceder 1000 caracteres'),
});

type ReviewFormData = z.infer<typeof reviewSchema>;

interface WriteReviewDialogProps {
  targetId: string;
  targetType: 'seller' | 'dealer';
  vehicleId?: string;
  vehicleTitle?: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess?: () => void;
}

export function WriteReviewDialog({
  targetId,
  targetType,
  // vehicleId reserved for future per-vehicle reviews
  vehicleTitle,
  open,
  onOpenChange,
  onSuccess,
}: WriteReviewDialogProps) {
  const createReview = useCreateReview();

  const {
    register,
    handleSubmit,
    setValue,
    reset,
    formState: { errors },
  } = useForm<ReviewFormData>({
    resolver: zodResolver(reviewSchema),
    defaultValues: {
      overallRating: 0,
      title: '',
      content: '',
    },
  });

  const [ratingValue, setRatingValue] = React.useState(0);

  const onSubmit = async (data: ReviewFormData) => {
    try {
      await createReview.mutateAsync({
        targetType,
        targetId,
        overallRating: data.overallRating,
        title: data.title || undefined,
        content: data.content,
      });

      toast.success('¡Gracias por tu reseña!', {
        description: 'Tu opinión ayuda a otros compradores.',
      });

      reset();
      setRatingValue(0);
      onOpenChange(false);
      onSuccess?.();
    } catch (error: unknown) {
      const err = error as { message?: string; response?: { data?: { error?: string } } };
      const message = err.response?.data?.error || err.message || 'No se pudo enviar la reseña';
      toast.error('Error al enviar reseña', { description: message });
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50"
        onClick={() => onOpenChange(false)}
        aria-hidden="true"
      />

      {/* Dialog */}
      <div className="bg-card relative z-10 mx-4 w-full max-w-lg rounded-xl p-6 shadow-xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-foreground text-lg font-semibold">Escribir reseña</h2>
            {vehicleTitle && <p className="text-muted-foreground mt-0.5 text-sm">{vehicleTitle}</p>}
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 w-8 p-0"
            onClick={() => onOpenChange(false)}
            aria-label="Cerrar diálogo"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          {/* Star Rating */}
          <div>
            <Label className="mb-2 block">Calificación *</Label>
            <StarRating
              value={ratingValue}
              onChange={val => {
                setValue('overallRating', val, { shouldValidate: true });
                setRatingValue(val);
              }}
              size="lg"
            />
            {errors.overallRating && (
              <p className="mt-1 text-xs text-red-500">{errors.overallRating.message}</p>
            )}
          </div>

          {/* Title */}
          <div>
            <Label htmlFor="review-title">Título (opcional)</Label>
            <Input
              id="review-title"
              placeholder="Resumen breve de tu experiencia"
              {...register('title')}
              className="mt-1.5"
            />
            {errors.title && <p className="mt-1 text-xs text-red-500">{errors.title.message}</p>}
          </div>

          {/* Content */}
          <div>
            <Label htmlFor="review-content">Tu reseña *</Label>
            <Textarea
              id="review-content"
              placeholder="Comparte tu experiencia con este vendedor. ¿Cómo fue el proceso de compra? ¿Recomendarías este vendedor?"
              rows={4}
              {...register('content')}
              className="mt-1.5 resize-none"
            />
            {errors.content && (
              <p className="mt-1 text-xs text-red-500">{errors.content.message}</p>
            )}
            <p className="text-muted-foreground mt-1 text-xs">Mínimo 20 caracteres</p>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={createReview.isPending}>
              {createReview.isPending ? 'Enviando...' : 'Enviar reseña'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default WriteReviewDialog;
