import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

// =============================================================================
// Deal Rating Badge - Sistema de calificación de precios estilo marketplace
// =============================================================================
// great      = Precio excelente (muy por debajo del mercado)
// good       = Buen precio (por debajo del mercado)
// fair       = Precio justo (en línea con el mercado)
// high       = Precio alto (por encima del mercado)
// overpriced = Sobrepreciado (>15% por encima del mercado, alerta >20%)
// uncertain  = Sin datos suficientes para calificar

const dealRatingVariants = cva(
  'inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-semibold uppercase tracking-wide',
  {
    variants: {
      rating: {
        great: 'bg-primary text-primary-foreground',
        good: 'bg-green-500 text-white dark:bg-green-600',
        fair: 'bg-amber-500 text-white dark:bg-amber-600',
        high: 'bg-orange-500 text-white dark:bg-orange-600',
        overpriced: 'bg-red-700 text-white dark:bg-red-800 animate-pulse',
        uncertain: 'bg-muted text-muted-foreground',
      },
      size: {
        sm: 'px-2 py-0.5 text-[10px]',
        default: 'px-3 py-1 text-xs',
        lg: 'px-4 py-1.5 text-sm',
      },
    },
    defaultVariants: {
      rating: 'uncertain',
      size: 'default',
    },
  }
);

export type DealRating = 'great' | 'good' | 'fair' | 'high' | 'overpriced' | 'uncertain';

export interface DealRatingBadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>, VariantProps<typeof dealRatingVariants> {
  rating: DealRating;
  showIcon?: boolean;
  showLabel?: boolean;
}

const ratingConfig: Record<
  DealRating,
  { label: string; labelEs: string; icon: React.ReactNode; description: string }
> = {
  great: {
    label: 'Great Deal',
    labelEs: 'Excelente',
    icon: (
      <svg className="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" />
      </svg>
    ),
    description: 'Muy por debajo del valor de mercado',
  },
  good: {
    label: 'Good Deal',
    labelEs: 'Buen Precio',
    icon: (
      <svg className="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-11.25a.75.75 0 00-1.5 0v4.59L7.3 9.24a.75.75 0 00-1.1 1.02l3.25 3.5a.75.75 0 001.1 0l3.25-3.5a.75.75 0 10-1.1-1.02l-1.95 2.1V6.75z" />
      </svg>
    ),
    description: 'Por debajo del valor de mercado',
  },
  fair: {
    label: 'Fair Price',
    labelEs: 'Precio Justo',
    icon: (
      <svg className="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M4.25 10a.75.75 0 01.75-.75h10a.75.75 0 010 1.5H5a.75.75 0 01-.75-.75z" />
      </svg>
    ),
    description: 'En línea con el valor de mercado',
  },
  high: {
    label: 'High Price',
    labelEs: 'Precio Alto',
    icon: (
      <svg className="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-13a.75.75 0 00-1.5 0v4c0 .414.336.75.75.75h4a.75.75 0 000-1.5h-3.25V5z" />
      </svg>
    ),
    description: 'Por encima del valor de mercado',
  },
  overpriced: {
    label: 'Overpriced',
    labelEs: 'Sobrepreciado',
    icon: (
      <svg className="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path
          fillRule="evenodd"
          d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z"
          clipRule="evenodd"
        />
      </svg>
    ),
    description: 'Significativamente por encima del valor de mercado (>15%)',
  },
  uncertain: {
    label: 'No Rating',
    labelEs: 'Sin Calificar',
    icon: (
      <svg className="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" />
      </svg>
    ),
    description: 'Datos insuficientes para calificar',
  },
};

const DealRatingBadge = React.forwardRef<HTMLSpanElement, DealRatingBadgeProps>(
  ({ className, rating, size, showIcon = true, showLabel = true, ...props }, ref) => {
    const config = ratingConfig[rating];

    return (
      <span
        ref={ref}
        className={cn(dealRatingVariants({ rating, size, className }))}
        title={config.description}
        role="status"
        aria-label={`Calificación de precio: ${config.labelEs}`}
        {...props}
      >
        {showIcon && config.icon}
        {showLabel && config.labelEs}
      </span>
    );
  }
);
DealRatingBadge.displayName = 'DealRatingBadge';

// Helper function to calculate deal rating based on price difference
// 5-tier system aligned with backend PricingEngine.ClassifyPricePosition
export function calculateDealRating(currentPrice: number, marketPrice: number | null): DealRating {
  if (!marketPrice || marketPrice <= 0) {
    return 'uncertain';
  }

  const priceDifference = ((currentPrice - marketPrice) / marketPrice) * 100;

  if (priceDifference <= -15) {
    return 'great'; // 15%+ below market → Excelente
  } else if (priceDifference <= -5) {
    return 'good'; // 5-15% below market → Buen Precio
  } else if (priceDifference <= 5) {
    return 'fair'; // Within ±5% of market → Precio Justo
  } else if (priceDifference <= 15) {
    return 'high'; // 5-15% above market → Precio Alto
  } else {
    return 'overpriced'; // >15% above market → Sobrepreciado
  }
}

/**
 * Returns true if the vehicle is severely overvalued (>20% above market).
 * Used to trigger special alerts and recommendations for the seller.
 */
export function isOvervalued(currentPrice: number, marketPrice: number | null): boolean {
  if (!marketPrice || marketPrice <= 0) return false;
  const priceDifference = ((currentPrice - marketPrice) / marketPrice) * 100;
  return priceDifference > 20;
}

export { DealRatingBadge, dealRatingVariants, ratingConfig };
