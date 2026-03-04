/**
 * Admin Banners Management Page
 *
 * Full CRUD for promotional banners displayed on the OKLA platform.
 * Changes made here are immediately reflected on /vehiculos (configurable ad slots).
 */

'use client';

import * as React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import {
  Image as ImageIcon,
  Plus,
  AlertCircle,
  Edit,
  Trash2,
  Eye,
  ExternalLink,
  BarChart2,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  useBanners,
  useCreateBanner,
  useUpdateBanner,
  useDeleteBanner,
} from '@/hooks/use-admin-extended';
import type { Banner } from '@/services/admin-extended';

// ─────────────────────────────────────────────
// Constants
// ─────────────────────────────────────────────
const PLACEMENT_LABELS: Record<string, string> = {
  search_leaderboard: '/vehiculos — Leaderboard',
  'homepage-hero': 'Homepage — Hero',
  'homepage-secondary': 'Homepage — Secundario',
  sidebar: 'Barra Lateral',
  listing_inline: 'Listado — Inline',
};

const STATUS_LABELS: Record<Banner['status'], string> = {
  active: 'Activo',
  scheduled: 'Programado',
  inactive: 'Inactivo',
};

const STATUS_VARIANTS: Record<Banner['status'], 'default' | 'secondary' | 'outline'> = {
  active: 'default',
  scheduled: 'outline',
  inactive: 'secondary',
};

// ─────────────────────────────────────────────
// Banner Form
// ─────────────────────────────────────────────
const EMPTY_FORM: Partial<Banner> = {
  title: '',
  subtitle: '',
  image: '',
  link: '',
  ctaText: '',
  placement: 'search_leaderboard',
  status: 'active',
  startDate: new Date().toISOString().slice(0, 10),
  endDate: new Date(Date.now() + 30 * 86_400_000).toISOString().slice(0, 10),
  displayOrder: 0,
};

interface BannerFormDialogProps {
  open: boolean;
  initialData?: Partial<Banner>;
  onClose: () => void;
  onSave: (data: Partial<Banner>) => Promise<void>;
  loading: boolean;
}

function BannerFormDialog({ open, initialData, onClose, onSave, loading }: BannerFormDialogProps) {
  const [form, setForm] = React.useState<Partial<Banner>>(initialData ?? EMPTY_FORM);
  const isEdit = Boolean(initialData?.id);

  React.useEffect(() => {
    setForm(initialData ?? EMPTY_FORM);
  }, [initialData, open]);

  const field =
    (key: keyof Banner) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm(prev => ({ ...prev, [key]: e.target.value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.title?.trim() || !form.link?.trim()) {
      toast.error('Título y URL son obligatorios');
      return;
    }
    await onSave(form);
  };

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Editar Banner' : 'Nuevo Banner'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Title */}
          <div className="space-y-1">
            <Label htmlFor="b-title">Título *</Label>
            <Input
              id="b-title"
              value={form.title ?? ''}
              onChange={field('title')}
              placeholder="ej. Financiamiento para tu vehículo"
              required
            />
          </div>

          {/* Subtitle */}
          <div className="space-y-1">
            <Label htmlFor="b-subtitle">Subtítulo</Label>
            <Input
              id="b-subtitle"
              value={form.subtitle ?? ''}
              onChange={field('subtitle')}
              placeholder="Descripción breve del anuncio"
            />
          </div>

          {/* CTA */}
          <div className="space-y-1">
            <Label htmlFor="b-cta">Texto del botón CTA</Label>
            <Input
              id="b-cta"
              value={form.ctaText ?? ''}
              onChange={field('ctaText')}
              placeholder="ej. Solicitar ahora"
            />
          </div>

          {/* Link */}
          <div className="space-y-1">
            <Label htmlFor="b-link">URL destino *</Label>
            <Input
              id="b-link"
              value={form.link ?? ''}
              onChange={field('link')}
              placeholder="https://... o /ruta-interna"
              required
            />
          </div>

          {/* Image URL */}
          <div className="space-y-1">
            <Label htmlFor="b-image">URL de imagen</Label>
            <Input
              id="b-image"
              value={form.image ?? ''}
              onChange={field('image')}
              placeholder="/images/banners/mi-banner.jpg"
            />
          </div>

          {/* Placement + Status */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Ubicación</Label>
              <Select
                value={form.placement ?? 'search_leaderboard'}
                onValueChange={v => setForm(p => ({ ...p, placement: v }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(PLACEMENT_LABELS).map(([v, l]) => (
                    <SelectItem key={v} value={v}>
                      {l}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1">
              <Label>Estado</Label>
              <Select
                value={form.status ?? 'active'}
                onValueChange={v => setForm(p => ({ ...p, status: v as Banner['status'] }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="active">Activo</SelectItem>
                  <SelectItem value="scheduled">Programado</SelectItem>
                  <SelectItem value="inactive">Inactivo</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Dates */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="b-start">Fecha inicio</Label>
              <Input
                id="b-start"
                type="date"
                value={form.startDate ?? ''}
                onChange={field('startDate')}
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="b-end">Fecha fin</Label>
              <Input
                id="b-end"
                type="date"
                value={form.endDate ?? ''}
                onChange={field('endDate')}
              />
            </div>
          </div>

          {/* Order */}
          <div className="space-y-1">
            <Label htmlFor="b-order">Orden de visualización</Label>
            <Input
              id="b-order"
              type="number"
              min={0}
              value={form.displayOrder ?? 0}
              onChange={e => setForm(p => ({ ...p, displayOrder: Number(e.target.value) }))}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={loading}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear banner'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ─────────────────────────────────────────────
// Main Page
// ─────────────────────────────────────────────
export default function BannersPage() {
  const { data: banners, isLoading, isError } = useBanners();
  const createBanner = useCreateBanner();
  const updateBanner = useUpdateBanner();
  const deleteBannerMutation = useDeleteBanner();

  const [formOpen, setFormOpen] = React.useState(false);
  const [editTarget, setEditTarget] = React.useState<Banner | null>(null);
  const [deleteTarget, setDeleteTarget] = React.useState<Banner | null>(null);

  const handleCreate = async (data: Partial<Banner>) => {
    await createBanner.mutateAsync(data, {
      onSuccess: () => {
        toast.success('Banner creado');
        setFormOpen(false);
      },
      onError: () => toast.error('Error al crear el banner'),
    });
  };

  const handleUpdate = async (data: Partial<Banner>) => {
    if (!editTarget) return;
    await updateBanner.mutateAsync(
      { id: editTarget.id, data },
      {
        onSuccess: () => {
          toast.success('Banner actualizado');
          setEditTarget(null);
        },
        onError: () => toast.error('Error al actualizar el banner'),
      }
    );
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    await deleteBannerMutation.mutateAsync(deleteTarget.id, {
      onSuccess: () => {
        toast.success('Banner eliminado');
        setDeleteTarget(null);
      },
      onError: () => toast.error('Error al eliminar el banner'),
    });
  };

  const activeCount = banners?.filter(b => b.status === 'active').length ?? 0;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-9 w-36" />
        </div>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="mb-3 h-32 w-full" />
                <Skeleton className="mb-2 h-5 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex min-h-[300px] items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <p className="text-muted-foreground mt-4">No se pudieron cargar los banners</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Banners Publicitarios</h1>
          <p className="text-muted-foreground text-sm">
            {activeCount} activo{activeCount !== 1 ? 's' : ''} · Los cambios se reflejan en el sitio
            en &lt;60&nbsp;segundos
          </p>
        </div>
        <Button
          onClick={() => {
            setEditTarget(null);
            setFormOpen(true);
          }}
        >
          <Plus className="mr-2 h-4 w-4" />
          Nuevo Banner
        </Button>
      </div>

      {/* Placement info callout */}
      <div className="rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-300">
        <strong>Ubicación &quot;/vehiculos — Leaderboard&quot;</strong>: aparece entre los
        resultados de búsqueda. Los anuncios con estado <em>Activo</em> y fecha vigente se muestran
        automáticamente.
      </div>

      {/* Banner Grid */}
      {!banners || banners.length === 0 ? (
        <div className="flex min-h-[300px] items-center justify-center rounded-2xl border-2 border-dashed">
          <div className="text-center">
            <ImageIcon className="text-muted-foreground mx-auto h-12 w-12" />
            <p className="text-muted-foreground mt-4 font-medium">No hay banners configurados</p>
            <p className="text-muted-foreground text-sm">
              Crea el primer banner para que aparezca en /vehiculos
            </p>
            <Button
              className="mt-4"
              onClick={() => {
                setEditTarget(null);
                setFormOpen(true);
              }}
            >
              <Plus className="mr-2 h-4 w-4" />
              Crear primer banner
            </Button>
          </div>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {banners.map(banner => (
            <Card key={banner.id} className="overflow-hidden transition-shadow hover:shadow-md">
              {/* Preview area */}
              <div className="bg-muted relative aspect-video">
                {banner.image ? (
                  /* eslint-disable-next-line @next/next/no-img-element */
                  <img
                    src={banner.image}
                    alt={banner.title}
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <div className="flex h-full items-center justify-center">
                    <ImageIcon className="text-muted-foreground h-10 w-10" />
                  </div>
                )}
                <div className="absolute top-2 right-2 flex gap-1">
                  <Badge variant={STATUS_VARIANTS[banner.status]}>
                    {STATUS_LABELS[banner.status]}
                  </Badge>
                </div>
                <div className="absolute bottom-2 left-2">
                  <Badge variant="outline" className="bg-background/80 text-xs backdrop-blur-sm">
                    {PLACEMENT_LABELS[banner.placement] ?? banner.placement}
                  </Badge>
                </div>
              </div>

              <CardContent className="space-y-3 p-4">
                <div>
                  <h3 className="truncate font-semibold">{banner.title}</h3>
                  {banner.subtitle && (
                    <p className="text-muted-foreground truncate text-sm">{banner.subtitle}</p>
                  )}
                </div>

                {/* Date range */}
                <p className="text-muted-foreground text-xs">
                  {banner.startDate} → {banner.endDate}
                </p>

                {/* Stats */}
                <div className="text-muted-foreground flex gap-4 text-xs">
                  <span className="flex items-center gap-1">
                    <Eye className="h-3 w-3" />
                    {(banner.views ?? 0).toLocaleString()} vistas
                  </span>
                  <span className="flex items-center gap-1">
                    <ExternalLink className="h-3 w-3" />
                    {banner.clicks ?? 0} clicks
                  </span>
                  {(banner.views ?? 0) > 0 && (
                    <span className="flex items-center gap-1">
                      <BarChart2 className="h-3 w-3" />
                      CTR {(((banner.clicks ?? 0) / (banner.views ?? 1)) * 100).toFixed(1)}%
                    </span>
                  )}
                </div>

                {/* Actions */}
                <div className="flex gap-2 border-t pt-2">
                  <Button
                    variant="outline"
                    size="sm"
                    className="flex-1"
                    onClick={() => {
                      setEditTarget(banner);
                      setFormOpen(true);
                    }}
                  >
                    <Edit className="mr-1 h-3 w-3" />
                    Editar
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    className="text-red-500 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950"
                    onClick={() => setDeleteTarget(banner)}
                  >
                    <Trash2 className="h-3 w-3" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create / Edit Dialog */}
      <BannerFormDialog
        open={formOpen}
        initialData={editTarget ?? undefined}
        onClose={() => {
          setFormOpen(false);
          setEditTarget(null);
        }}
        onSave={editTarget ? handleUpdate : handleCreate}
        loading={createBanner.isPending || updateBanner.isPending}
      />

      {/* Delete Confirm Dialog */}
      <AlertDialog open={Boolean(deleteTarget)} onOpenChange={v => !v && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Eliminar banner?</AlertDialogTitle>
            <AlertDialogDescription>
              Se eliminará <strong>&quot;{deleteTarget?.title}&quot;</strong>. Esta acción no se
              puede deshacer.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-red-600 hover:bg-red-700">
              Eliminar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
