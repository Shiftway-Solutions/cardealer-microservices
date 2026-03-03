/**
 * Lazy-loaded Components
 *
 * Heavy components that should be loaded on demand to improve initial load time.
 * Uses Next.js dynamic import with SSR disabled for client-only components.
 *
 * NOTE: Components from external libraries (like recharts) are loaded lazily.
 * Internal components that don't exist yet are commented out as placeholders.
 */

import dynamic from 'next/dynamic';
import { Skeleton } from '@/components/ui/skeleton';
import { ComponentType } from 'react';

// =============================================================================
// LOADING FALLBACKS (SKELETONS)
// =============================================================================

export const ChartSkeleton = () => (
  <div className="h-64 w-full animate-pulse">
    <Skeleton className="h-full w-full rounded-lg" />
  </div>
);

export const MapSkeleton = () => (
  <div className="h-80 w-full animate-pulse">
    <Skeleton className="h-full w-full rounded-lg" />
    <p className="text-muted-foreground mt-2 text-center text-sm">Cargando mapa...</p>
  </div>
);

export const GallerySkeleton = () => (
  <div className="aspect-video w-full animate-pulse">
    <Skeleton className="h-full w-full rounded-lg" />
  </div>
);

export const EditorSkeleton = () => (
  <div className="h-48 w-full animate-pulse">
    <Skeleton className="h-full w-full rounded-lg" />
  </div>
);

export const TableSkeleton = () => (
  <div className="w-full animate-pulse space-y-2">
    <Skeleton className="h-10 w-full rounded" />
    <Skeleton className="h-10 w-full rounded" />
    <Skeleton className="h-10 w-full rounded" />
    <Skeleton className="h-10 w-full rounded" />
    <Skeleton className="h-10 w-full rounded" />
  </div>
);

export const ModalSkeleton = () => (
  <div className="bg-background/80 fixed inset-0 flex items-center justify-center">
    <Skeleton className="h-64 w-96 rounded-lg" />
  </div>
);

export const CardSkeleton = () => (
  <div className="w-full animate-pulse">
    <Skeleton className="h-48 w-full rounded-lg" />
    <Skeleton className="mt-2 h-4 w-3/4 rounded" />
    <Skeleton className="mt-2 h-4 w-1/2 rounded" />
  </div>
);

// =============================================================================
// LAZY COMPONENTS - Charts (Recharts Library)
// =============================================================================

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnyComponent = ComponentType<any>;

/**
 * Recharts components - Heavy charting library (~200kb)
 * Only loaded when charts are actually needed
 */
export const LazyLineChart = dynamic<AnyComponent>(
  () => import('recharts').then(mod => mod.LineChart as unknown as AnyComponent),
  { loading: () => <ChartSkeleton />, ssr: false }
);

export const LazyBarChart = dynamic<AnyComponent>(
  () => import('recharts').then(mod => mod.BarChart as unknown as AnyComponent),
  { loading: () => <ChartSkeleton />, ssr: false }
);

export const LazyPieChart = dynamic<AnyComponent>(
  () => import('recharts').then(mod => mod.PieChart as unknown as AnyComponent),
  { loading: () => <ChartSkeleton />, ssr: false }
);

export const LazyAreaChart = dynamic<AnyComponent>(
  () => import('recharts').then(mod => mod.AreaChart as unknown as AnyComponent),
  { loading: () => <ChartSkeleton />, ssr: false }
);

// Re-export recharts ResponsiveContainer for convenience
export const LazyResponsiveContainer = dynamic(
  () => import('recharts').then(mod => mod.ResponsiveContainer as unknown as AnyComponent),
  { ssr: false }
);

// =============================================================================
// LAZY COMPONENTS - Vehicle Detail (Existing Components)
// =============================================================================

/**
 * Vehicle gallery with image carousel
 */
export const LazyVehicleGallery = dynamic(
  () => import('@/components/vehicle-detail/vehicle-gallery'),
  { loading: () => <GallerySkeleton />, ssr: false }
);

/**
 * Vehicle tabs (details, specs, features)
 */
export const LazyVehicleTabs = dynamic(() => import('@/components/vehicle-detail/vehicle-tabs'), {
  loading: () => <TableSkeleton />,
  ssr: true,
});

/**
 * Similar vehicles section
 */
export const LazySimilarVehicles = dynamic(
  () => import('@/components/vehicle-detail/similar-vehicles'),
  { loading: () => <CardSkeleton />, ssr: true }
);

/**
 * Seller card component
 */
export const LazySellerCard = dynamic(() => import('@/components/vehicle-detail/seller-card'), {
  loading: () => <CardSkeleton />,
  ssr: true,
});

// =============================================================================
// HELPER: Create placeholder for future components
// =============================================================================

/**
 * Creates a placeholder component for modules that don't exist yet.
 * Useful during development to plan lazy loading structure.
 */
function createPlaceholder(name: string): ComponentType {
  const Placeholder = () => (
    <div className="border-muted bg-muted/20 flex h-32 w-full items-center justify-center rounded-lg border-2 border-dashed">
      <span className="text-muted-foreground text-sm">
        Componente &quot;{name}&quot; próximamente
      </span>
    </div>
  );
  Placeholder.displayName = `Placeholder(${name})`;
  return Placeholder;
}

// =============================================================================
// LAZY COMPONENTS - Future Components (Placeholders)
// These will be replaced with actual imports when components are created
// =============================================================================

/**
 * Google Maps or Leaflet - Heavy mapping components
 * TODO: Create @/components/maps/Map when needed
 */
export const LazyMap = createPlaceholder('Map');

/**
 * Dealer location map
 * TODO: Create @/components/maps/DealerMap when needed
 */
export const LazyDealerMap = createPlaceholder('DealerMap');

/**
 * 360° Vehicle viewer — real component from @/components/vehicles/viewer-360
 */
export const LazyVehicle360Viewer = dynamic(
  () =>
    import('@/components/vehicles/viewer-360').then(
      mod => mod.Viewer360 as unknown as AnyComponent
    ),
  { loading: () => <GallerySkeleton />, ssr: false }
);

/**
 * Video player component
 * TODO: Create @/components/media/VideoPlayer when needed
 */
export const LazyVideoPlayer = createPlaceholder('VideoPlayer');

/**
 * Rich text editor - Heavy with formatting toolbar
 * TODO: Create @/components/forms/RichTextEditor when needed
 */
export const LazyRichTextEditor = createPlaceholder('RichTextEditor');

/**
 * Advanced data table with sorting, filtering, pagination
 * TODO: Create @/components/tables/DataTable when needed
 */
export const LazyDataTable = createPlaceholder('DataTable');

/**
 * Dealer inventory table
 * TODO: Create @/components/dealer/InventoryTable when needed
 */
export const LazyInventoryTable = createPlaceholder('InventoryTable');

/**
 * Contact seller modal
 * TODO: Create @/components/modals/ContactModal when needed
 */
export const LazyContactModal = createPlaceholder('ContactModal');

/**
 * Share vehicle modal
 * TODO: Create @/components/modals/ShareModal when needed
 */
export const LazyShareModal = createPlaceholder('ShareModal');

/**
 * Image lightbox modal
 * TODO: Create @/components/modals/Lightbox when needed
 */
export const LazyLightbox = createPlaceholder('Lightbox');

/**
 * Comparison modal
 * TODO: Create @/components/modals/ComparisonModal when needed
 */
export const LazyComparisonModal = createPlaceholder('ComparisonModal');

/**
 * Stripe Elements - Payment form
 * TODO: Create @/components/checkout/StripePaymentForm when needed
 */
export const LazyStripePaymentForm = createPlaceholder('StripePaymentForm');

/**
 * AZUL Payment form
 * TODO: Create @/components/checkout/AzulPaymentForm when needed
 */
export const LazyAzulPaymentForm = createPlaceholder('AzulPaymentForm');

/**
 * Analytics dashboard - Heavy with multiple charts
 * TODO: Create @/components/dealer/AnalyticsDashboard when needed
 */
export const LazyAnalyticsDashboard = createPlaceholder('AnalyticsDashboard');

/**
 * Admin dashboard stats
 * TODO: Create @/components/admin/AdminDashboard when needed
 */
export const LazyAdminDashboard = createPlaceholder('AdminDashboard');

/**
 * Chat/Messaging component — real component from @/components/chat/ChatPanel
 */
export const LazyMessaging = dynamic(
  () => import('@/components/chat/ChatPanel').then(mod => mod.ChatPanel as unknown as AnyComponent),
  { loading: () => <CardSkeleton />, ssr: false }
);
