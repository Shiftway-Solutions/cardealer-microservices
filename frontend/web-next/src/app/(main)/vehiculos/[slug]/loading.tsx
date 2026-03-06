import { Skeleton } from '@/components/ui/skeleton';

export default function VehicleDetailLoading() {
  return (
    <div className="min-h-screen">
      {/* Breadcrumb skeleton */}
      <div className="border-border bg-muted/30 border-b py-3">
        <div className="container mx-auto flex items-center gap-2 px-4">
          <Skeleton className="h-4 w-12" />
          <Skeleton className="h-4 w-20" />
          <Skeleton className="h-4 w-40" />
        </div>
      </div>

      <div className="container mx-auto px-4 py-6">
        <div className="grid gap-8 lg:grid-cols-[1fr_380px]">
          {/* Main content */}
          <div className="space-y-6">
            {/* Gallery skeleton */}
            <Skeleton className="aspect-[16/10] w-full rounded-xl" />
            <div className="flex gap-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-16 w-20 rounded-lg" />
              ))}
            </div>

            {/* Title & price */}
            <div className="space-y-3">
              <Skeleton className="h-8 w-3/4" />
              <div className="flex items-center gap-4">
                <Skeleton className="h-6 w-32" />
                <Skeleton className="h-5 w-24" />
              </div>
              <Skeleton className="h-10 w-48" />
            </div>

            {/* Specs grid */}
            <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="border-border rounded-lg border p-3">
                  <Skeleton className="mb-2 h-4 w-16" />
                  <Skeleton className="h-5 w-24" />
                </div>
              ))}
            </div>

            {/* Description */}
            <div className="space-y-3">
              <Skeleton className="h-6 w-32" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
            </div>
          </div>

          {/* Sidebar — Seller card */}
          <div className="space-y-4">
            <div className="border-border rounded-xl border p-6">
              <div className="flex items-center gap-3">
                <Skeleton className="h-12 w-12 rounded-full" />
                <div className="space-y-2">
                  <Skeleton className="h-5 w-32" />
                  <Skeleton className="h-4 w-20" />
                </div>
              </div>
              <div className="mt-4 space-y-3">
                <Skeleton className="h-10 w-full rounded-lg" />
                <Skeleton className="h-10 w-full rounded-lg" />
                <Skeleton className="h-10 w-full rounded-lg" />
              </div>
            </div>

            {/* Monthly payment skeleton */}
            <div className="border-border rounded-xl border p-4">
              <Skeleton className="mb-2 h-5 w-40" />
              <Skeleton className="h-8 w-32" />
              <Skeleton className="mt-2 h-4 w-full" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
