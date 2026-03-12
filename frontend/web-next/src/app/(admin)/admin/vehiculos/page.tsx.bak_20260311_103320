/**
 * Admin Vehicles Page (Unified)
 *
 * Combines vehicle management + moderation queue in a single tabbed view.
 * - Tab "Todos": Grid view of all vehicles with filters, search, pagination
 * - Tab "Moderación": One-at-a-time review queue for pending vehicles
 *
 * Connected to real APIs - February 2026
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { formatPrice as _fp } from '@/lib/format';
import { formatDateTime } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import {
  Car,
  Search,
  ChevronLeft,
  ChevronRight,
  Eye,
  CheckCircle,
  XCircle,
  Flag,
  Clock,
  Star,
  Trash2,
  Loader2,
  AlertTriangle,
  User,
  Calendar,
  ImageIcon,
  FileText,
  Inbox,
  RefreshCw,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  useAdminVehicles,
  useVehicleStats,
  useApproveVehicle,
  useRejectVehicle,
  useToggleFeatured,
  useDeleteVehicle,
  useModerationQueue,
  useModerationStats,
  useApproveModerationItem,
  useRejectModerationItem,
  type VehicleFilters,
} from '@/hooks/use-admin';
import type { ModerationItem } from '@/services/admin';

// =============================================================================
// HELPERS
// =============================================================================

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'active':
      return <Badge className="bg-primary/10 text-primary">Activo</Badge>;
    case 'pending':
      return <Badge className="bg-amber-100 text-amber-700">Pendiente</Badge>;
    case 'rejected':
      return <Badge className="bg-red-100 text-red-700">Rechazado</Badge>;
    case 'sold':
      return <Badge className="bg-blue-100 text-blue-700">Vendido</Badge>;
    case 'paused':
      return <Badge className="bg-muted text-foreground">Pausado</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
};

const formatPrice = (price: number, currency?: string) => {
  if (!price) return 'N/A';
  return _fp(price, currency || 'DOP');
};

const rejectionReasons = [
  'Fotos de baja calidad o incompletas',
  'Información del vehículo incorrecta o incompleta',
  'Precio fuera del rango de mercado',
  'Contenido inapropiado o engañoso',
  'Vehículo ya publicado anteriormente',
  'Documentos sospechosos o fraudulentos',
  'Otro (especificar)',
];

// =============================================================================
// SKELETONS
// =============================================================================

function VehiclesSkeleton() {
  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {[1, 2, 3, 4, 5, 6].map(i => (
          <Skeleton key={i} className="h-64" />
        ))}
      </div>
    </div>
  );
}

function ModerationSkeleton() {
  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <CardContent className="p-6">
          <Skeleton className="aspect-video w-full rounded-lg" />
        </CardContent>
      </Card>
      <Card>
        <CardContent className="space-y-4 p-6">
          <Skeleton className="h-8 w-3/4" />
          <Skeleton className="h-6 w-1/2" />
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-16 w-full" />
          <div className="flex gap-3">
            <Skeleton className="h-10 flex-1" />
            <Skeleton className="h-10 flex-1" />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// MODERATION EMPTY STATE
// =============================================================================

function EmptyModerationState() {
  return (
    <Card className="py-16">
      <CardContent className="flex flex-col items-center justify-center text-center">
        <div className="bg-primary/10 mb-6 rounded-full p-6">
          <Inbox className="text-primary h-12 w-12" />
        </div>
        <h2 className="text-foreground mb-2 text-2xl font-bold">¡Cola de moderación vacía!</h2>
        <p className="text-muted-foreground mb-6 max-w-md">
          No hay publicaciones pendientes de revisión. Cuando los usuarios publiquen vehículos u
          otro contenido, aparecerán aquí automáticamente.
        </p>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// MODERATION ITEM CARD (review one-at-a-time)
// =============================================================================

function ModerationItemCard({
  item,
  onApprove,
  onReject,
  onSkip,
  isApproving,
  isRejecting,
}: {
  item: ModerationItem;
  onApprove: () => void;
  onReject: (reason: string) => void;
  onSkip: () => void;
  isApproving: boolean;
  isRejecting: boolean;
}) {
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [selectedReason, setSelectedReason] = useState('');
  const [customReason, setCustomReason] = useState('');

  const handleRejectClick = () => {
    setShowRejectModal(true);
  };

  const confirmReject = () => {
    const reason = selectedReason === 'Otro (especificar)' ? customReason : selectedReason;
    onReject(reason);
    setShowRejectModal(false);
    setSelectedReason('');
    setCustomReason('');
  };

  const mainImage = item.images?.[0] || '/placeholder-vehicle.jpg';

  return (
    <>
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Images Panel */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="flex items-center gap-2 text-lg">
              <ImageIcon className="h-5 w-5" />
              Imágenes ({item.images?.length || 0})
            </CardTitle>
            <Badge
              className={
                item.priority === 'high' || item.priority === 'urgent'
                  ? 'bg-red-100 text-red-700'
                  : ''
              }
            >
              {item.priority === 'high'
                ? 'Alta Prioridad'
                : item.priority === 'urgent'
                  ? 'Urgente'
                  : 'Normal'}
            </Badge>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {/* Main Image */}
              <div className="bg-muted relative aspect-video overflow-hidden rounded-lg">
                {item.images && item.images.length > 0 ? (
                  <Image
                    src={mainImage}
                    alt={item.title}
                    fill
                    className="object-cover"
                    sizes="(max-width: 1024px) 100vw, 50vw"
                  />
                ) : (
                  <div className="flex h-full items-center justify-center">
                    <ImageIcon className="text-muted-foreground h-16 w-16" />
                  </div>
                )}
              </div>

              {/* Thumbnails */}
              {item.images && item.images.length > 1 && (
                <div className="flex gap-2 overflow-x-auto">
                  {item.images.map((img, idx) => (
                    <div
                      key={idx}
                      className="bg-muted ring-primary relative h-16 w-20 flex-shrink-0 cursor-pointer overflow-hidden rounded-lg hover:ring-2"
                    >
                      <Image
                        src={img}
                        alt={`${item.title} ${idx + 1}`}
                        fill
                        className="object-cover"
                      />
                    </div>
                  ))}
                </div>
              )}

              {/* Alert if flagged */}
              {item.flagReason && (
                <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3">
                  <Flag className="mt-0.5 h-5 w-5 flex-shrink-0 text-amber-600" />
                  <div>
                    <p className="font-medium text-amber-800">Marcado para revisión</p>
                    <p className="text-sm text-amber-700">{item.flagReason}</p>
                  </div>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Details Panel */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <FileText className="h-5 w-5" />
              Detalles del Listado
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Title and Price */}
            <div>
              <h2 className="text-xl font-bold">{item.title}</h2>
              <p className="text-primary text-2xl font-bold">{formatPrice(item.price || 0)}</p>
            </div>

            {/* Description */}
            <div>
              <p className="text-muted-foreground mb-1 text-sm font-medium">Descripción</p>
              <p className="text-foreground">{item.description || 'Sin descripción'}</p>
            </div>

            {/* Seller Info */}
            <div className="bg-muted/50 rounded-lg p-4">
              <div className="flex items-center gap-3">
                <div className="bg-muted flex h-10 w-10 items-center justify-center rounded-full">
                  <User className="text-muted-foreground h-5 w-5" />
                </div>
                <div>
                  <p className="font-medium">{item.sellerName}</p>
                  <p className="text-muted-foreground text-sm capitalize">
                    {item.sellerType === 'dealer' ? 'Dealer' : 'Vendedor'}
                  </p>
                </div>
              </div>
            </div>

            {/* Metadata */}
            <div className="text-muted-foreground flex items-center gap-4 text-sm">
              <span className="flex items-center gap-1">
                <Calendar className="h-4 w-4" />
                {formatDateTime(item.submittedAt)}
              </span>
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3 pt-4">
              <Button
                className="bg-primary hover:bg-primary/90 flex-1"
                onClick={onApprove}
                disabled={isApproving || isRejecting}
              >
                {isApproving ? (
                  <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <CheckCircle className="mr-2 h-4 w-4" />
                )}
                Aprobar
              </Button>
              <Button
                variant="destructive"
                className="flex-1"
                onClick={handleRejectClick}
                disabled={isApproving || isRejecting}
              >
                {isRejecting ? (
                  <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <XCircle className="mr-2 h-4 w-4" />
                )}
                Rechazar
              </Button>
            </div>

            {/* Skip Button */}
            <Button
              variant="ghost"
              className="w-full"
              onClick={onSkip}
              disabled={isApproving || isRejecting}
            >
              Saltar (revisar después)
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Reject Modal */}
      {showRejectModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <Card className="w-full max-w-md">
            <CardHeader>
              <CardTitle>Razón del Rechazo</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                {rejectionReasons.map(reason => (
                  <label
                    key={reason}
                    className="hover:bg-muted/50 flex cursor-pointer items-center gap-2 rounded-lg p-2"
                  >
                    <input
                      type="radio"
                      name="reason"
                      value={reason}
                      checked={selectedReason === reason}
                      onChange={e => setSelectedReason(e.target.value)}
                      className="rounded-full"
                    />
                    <span className="text-sm">{reason}</span>
                  </label>
                ))}
              </div>

              {selectedReason === 'Otro (especificar)' && (
                <Textarea
                  placeholder="Describe la razón del rechazo..."
                  value={customReason}
                  onChange={e => setCustomReason(e.target.value)}
                />
              )}

              <div className="flex gap-3">
                <Button
                  variant="outline"
                  className="flex-1"
                  onClick={() => setShowRejectModal(false)}
                >
                  Cancelar
                </Button>
                <Button
                  variant="destructive"
                  className="flex-1"
                  onClick={confirmReject}
                  disabled={
                    !selectedReason || (selectedReason === 'Otro (especificar)' && !customReason)
                  }
                >
                  Confirmar Rechazo
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}

// =============================================================================
// TAB: MODERATION QUEUE
// =============================================================================

function ModerationTab() {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [page] = useState(1);

  const {
    data: queueData,
    isLoading: isLoadingQueue,
    refetch: refetchQueue,
  } = useModerationQueue({ page, pageSize: 10, status: 'pending' });

  const { data: modStats, isLoading: isLoadingStats } = useModerationStats();

  const approveMutation = useApproveModerationItem();
  const rejectMutation = useRejectModerationItem();

  const items = queueData?.items || [];
  const currentItem = items[currentIndex];
  const totalItems = queueData?.total || 0;

  const handleApprove = async () => {
    if (!currentItem) return;
    try {
      await approveMutation.mutateAsync(currentItem.id);
      toast.success('Vehículo aprobado');
      if (currentIndex < items.length - 1) {
        setCurrentIndex(currentIndex + 1);
      } else {
        refetchQueue();
        setCurrentIndex(0);
      }
    } catch (error) {
      console.error('Error al aprobar vehículo:', error);
      toast.error('Error al aprobar');
    }
  };

  const handleReject = async (reason: string = 'Rechazado por moderador') => {
    if (!currentItem) return;
    try {
      await rejectMutation.mutateAsync({ id: currentItem.id, reason });
      toast.success('Vehículo rechazado');
      if (currentIndex < items.length - 1) {
        setCurrentIndex(currentIndex + 1);
      } else {
        refetchQueue();
        setCurrentIndex(0);
      }
    } catch (error) {
      console.error('Error al rechazar vehículo:', error);
      toast.error('Error al rechazar');
    }
  };

  const handleNext = () => {
    if (currentIndex < items.length - 1) {
      setCurrentIndex(currentIndex + 1);
    }
  };

  const handlePrev = () => {
    if (currentIndex > 0) {
      setCurrentIndex(currentIndex - 1);
    }
  };

  const statsConfig = [
    { label: 'En Cola', value: modStats?.inQueue ?? 0, icon: Clock },
    { label: 'Alta Prioridad', value: modStats?.highPriority ?? 0, icon: AlertTriangle },
    { label: 'Hoy Revisados', value: modStats?.reviewedToday ?? 0, icon: CheckCircle },
    { label: 'Rechazados Hoy', value: modStats?.rejectedToday ?? 0, icon: XCircle },
  ];

  return (
    <div className="space-y-6">
      {/* Moderation Stats */}
      {isLoadingStats ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[1, 2, 3, 4].map(i => (
            <Card key={i}>
              <CardContent className="p-4">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-9 w-9 rounded-lg" />
                  <div className="space-y-2">
                    <Skeleton className="h-6 w-12" />
                    <Skeleton className="h-3 w-20" />
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {statsConfig.map(stat => {
            const Icon = stat.icon;
            return (
              <Card key={stat.label}>
                <CardContent className="p-4">
                  <div className="flex items-center gap-3">
                    <div className="rounded-lg bg-slate-100 p-2">
                      <Icon className="h-5 w-5 text-slate-600" />
                    </div>
                    <div>
                      <p className="text-2xl font-bold">{stat.value}</p>
                      <p className="text-muted-foreground text-xs">{stat.label}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Queue Content */}
      {isLoadingQueue ? (
        <ModerationSkeleton />
      ) : items.length === 0 ? (
        <EmptyModerationState />
      ) : currentItem ? (
        <>
          <ModerationItemCard
            item={currentItem}
            onApprove={handleApprove}
            onReject={(reason: string) => handleReject(reason)}
            onSkip={handleNext}
            isApproving={approveMutation.isPending}
            isRejecting={rejectMutation.isPending}
          />

          {/* Navigation */}
          <div className="flex items-center justify-center gap-4">
            <Button variant="outline" onClick={handlePrev} disabled={currentIndex === 0}>
              <ChevronLeft className="mr-1 h-4 w-4" />
              Anterior
            </Button>
            <span className="text-muted-foreground text-sm">
              {currentIndex + 1} de {items.length}
              {totalItems > items.length && ` (${totalItems} total)`}
            </span>
            <Button
              variant="outline"
              onClick={handleNext}
              disabled={currentIndex === items.length - 1}
            >
              Siguiente
              <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          </div>
        </>
      ) : null}
    </div>
  );
}

// =============================================================================
// TAB: ALL VEHICLES (Grid view)
// =============================================================================

function VehiclesGridTab() {
  const [filters, setFilters] = useState<VehicleFilters>({
    page: 1,
    pageSize: 12,
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [rejectReason, setRejectReason] = useState('');
  const [rejectingId, setRejectingId] = useState<string | null>(null);

  const { data: vehiclesData, isLoading } = useAdminVehicles(filters);
  const approveMutation = useApproveVehicle();
  const rejectMutation = useRejectVehicle();
  const toggleFeaturedMutation = useToggleFeatured();
  const deleteMutation = useDeleteVehicle();

  const handleSearch = () => {
    setFilters(prev => ({ ...prev, search: searchTerm, page: 1 }));
  };

  const handleFilterChange = (key: string, value: string) => {
    setFilters(prev => ({
      ...prev,
      [key]: value === 'all' ? undefined : value,
      page: 1,
    }));
  };

  const handleApprove = async (id: string) => {
    try {
      await approveMutation.mutateAsync(id);
      toast.success('Vehículo aprobado');
    } catch (error) {
      console.error('Error al aprobar vehículo:', error);
      toast.error('Error al aprobar vehículo');
    }
  };

  const handleReject = async () => {
    if (!rejectingId || !rejectReason) return;
    try {
      await rejectMutation.mutateAsync({ id: rejectingId, reason: rejectReason });
      toast.success('Vehículo rechazado');
      setRejectingId(null);
      setRejectReason('');
    } catch (error) {
      console.error('Error al rechazar vehículo:', error);
      toast.error('Error al rechazar vehículo');
    }
  };

  const handleToggleFeatured = async (id: string, featured: boolean) => {
    try {
      await toggleFeaturedMutation.mutateAsync({ id, featured: !featured });
      toast.success(featured ? 'Quitado de destacados' : 'Agregado a destacados');
    } catch (error) {
      console.error('Error al actualizar destacado:', error);
      toast.error('Error al actualizar');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await deleteMutation.mutateAsync(id);
      toast.success('Vehículo eliminado');
    } catch (error) {
      console.error('Error al eliminar vehículo:', error);
      toast.error('Error al eliminar vehículo');
    }
  };

  if (isLoading) {
    return <VehiclesSkeleton />;
  }

  const vehicles = vehiclesData?.items || [];
  const totalPages = vehiclesData?.totalPages || 1;

  return (
    <div className="space-y-6">
      {/* Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-wrap gap-4">
            <div className="flex flex-1 gap-2">
              <Input
                placeholder="Buscar por marca, modelo, vendedor..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleSearch()}
                className="max-w-sm"
              />
              <Button onClick={handleSearch}>
                <Search className="h-4 w-4" />
              </Button>
            </div>
            <Select
              value={filters.status || 'all'}
              onValueChange={v => handleFilterChange('status', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="active">Activos</SelectItem>
                <SelectItem value="pending">Pendientes</SelectItem>
                <SelectItem value="rejected">Rechazados</SelectItem>
                <SelectItem value="sold">Vendidos</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.sellerType || 'all'}
              onValueChange={v => handleFilterChange('sellerType', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Vendedor" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="seller">Vendedor</SelectItem>
                <SelectItem value="dealer">Dealer</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Vehicles Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {vehicles.map(vehicle => (
          <Card key={vehicle.id} className="overflow-hidden">
            <div className="relative aspect-[16/10]">
              <Image
                src={vehicle.image || '/placeholder-car.jpg'}
                alt={vehicle.title}
                fill
                className="object-cover"
              />
              <div className="absolute top-2 left-2 flex gap-2">
                {getStatusBadge(vehicle.status)}
                {vehicle.featured && (
                  <Badge className="bg-purple-600 text-white">
                    <Star className="mr-1 h-3 w-3" />
                    Destacado
                  </Badge>
                )}
              </div>
              {vehicle.reportsCount > 0 && (
                <div className="absolute top-2 right-2">
                  <Badge className="bg-red-600 text-white">
                    <Flag className="mr-1 h-3 w-3" />
                    {vehicle.reportsCount} reportes
                  </Badge>
                </div>
              )}
            </div>
            <CardContent className="p-4">
              <h3 className="text-foreground mb-1 font-semibold">{vehicle.title}</h3>
              <p className="text-primary mb-2 text-lg font-bold">
                {formatPrice(vehicle.price, vehicle.currency)}
              </p>
              <div className="text-muted-foreground mb-3 flex items-center gap-2 text-sm">
                <span>{vehicle.sellerName}</span>
                <Badge variant="outline" className="text-xs">
                  {vehicle.sellerType === 'dealer' ? 'Dealer' : 'Vendedor'}
                </Badge>
              </div>
              <div className="text-muted-foreground mb-3 flex items-center gap-4 text-sm">
                <span className="flex items-center gap-1">
                  <Eye className="h-4 w-4" />
                  {vehicle.views}
                </span>
                <span>{new Date(vehicle.createdAt).toLocaleDateString('es-DO')}</span>
              </div>
              <div className="flex flex-wrap gap-2">
                <Link
                  href={`/vehiculos/${vehicle.slug || vehicle.id}`}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  <Button variant="outline" size="sm">
                    <Eye className="mr-1 h-4 w-4" />
                    Ver
                  </Button>
                </Link>
                {vehicle.status === 'pending' && (
                  <>
                    <Button
                      size="sm"
                      className="bg-primary hover:bg-primary/90"
                      onClick={() => handleApprove(vehicle.id)}
                      disabled={approveMutation.isPending}
                    >
                      {approveMutation.isPending ? (
                        <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                      ) : (
                        <CheckCircle className="mr-1 h-4 w-4" />
                      )}
                      Aprobar
                    </Button>
                    <AlertDialog
                      open={rejectingId === vehicle.id}
                      onOpenChange={open => !open && setRejectingId(null)}
                    >
                      <AlertDialogTrigger asChild>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-red-600"
                          onClick={() => setRejectingId(vehicle.id)}
                        >
                          <XCircle className="mr-1 h-4 w-4" />
                          Rechazar
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Rechazar vehículo</AlertDialogTitle>
                          <AlertDialogDescription>
                            Ingresa el motivo del rechazo. El vendedor será notificado.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <Textarea
                          placeholder="Motivo del rechazo..."
                          value={rejectReason}
                          onChange={e => setRejectReason(e.target.value)}
                          rows={3}
                        />
                        <AlertDialogFooter>
                          <AlertDialogCancel onClick={() => setRejectReason('')}>
                            Cancelar
                          </AlertDialogCancel>
                          <AlertDialogAction
                            onClick={handleReject}
                            disabled={!rejectReason || rejectMutation.isPending}
                            className="bg-red-600 hover:bg-red-700"
                          >
                            {rejectMutation.isPending && (
                              <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                            )}
                            Rechazar
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </>
                )}
                {vehicle.status === 'active' && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleToggleFeatured(vehicle.id, vehicle.featured)}
                    disabled={toggleFeaturedMutation.isPending}
                  >
                    <Star
                      className={`mr-1 h-4 w-4 ${vehicle.featured ? 'fill-amber-500 text-amber-500' : ''}`}
                    />
                    {vehicle.featured ? 'Quitar' : 'Destacar'}
                  </Button>
                )}
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button variant="ghost" size="sm" className="text-red-600">
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>¿Eliminar vehículo?</AlertDialogTitle>
                      <AlertDialogDescription>
                        Esta acción no se puede deshacer. Se eliminará permanentemente este
                        vehículo.
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Cancelar</AlertDialogCancel>
                      <AlertDialogAction
                        onClick={() => handleDelete(vehicle.id)}
                        className="bg-red-600 hover:bg-red-700"
                      >
                        Eliminar
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {vehicles.length === 0 && (
        <Card>
          <CardContent className="text-muted-foreground py-12 text-center">
            <Car className="mx-auto mb-2 h-8 w-8" />
            <p>No se encontraron vehículos</p>
          </CardContent>
        </Card>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-muted-foreground text-sm">
            Página {filters.page} de {totalPages}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setFilters(prev => ({ ...prev, page: (prev.page || 1) - 1 }))}
              disabled={(filters.page || 1) <= 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setFilters(prev => ({ ...prev, page: (prev.page || 1) + 1 }))}
              disabled={(filters.page || 1) >= totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

// =============================================================================
// MAIN PAGE COMPONENT
// =============================================================================

export default function AdminVehiclesPage() {
  const { data: stats } = useVehicleStats();
  const { data: modStats } = useModerationStats();

  const pendingCount = modStats?.inQueue ?? stats?.pending ?? 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-foreground text-3xl font-bold">Vehículos</h1>
        <p className="text-muted-foreground">Gestiona y modera los vehículos de la plataforma</p>
      </div>

      {/* Stats (shared across tabs) */}
      <div className="grid gap-4 md:grid-cols-5">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Car className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.total?.toLocaleString() || 0}</p>
                <p className="text-muted-foreground text-xs">Total</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 rounded-lg p-2">
                <CheckCircle className="text-primary h-5 w-5" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.active?.toLocaleString() || 0}</p>
                <p className="text-muted-foreground text-xs">Activos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <Clock className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.pending || 0}</p>
                <p className="text-muted-foreground text-xs">Pendientes</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <Star className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.featured || 0}</p>
                <p className="text-muted-foreground text-xs">Destacados</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-100 p-2">
                <Flag className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.withReports || 0}</p>
                <p className="text-muted-foreground text-xs">Con reportes</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabbed Content */}
      <Tabs defaultValue="todos" className="space-y-6">
        <TabsList>
          <TabsTrigger value="todos" className="gap-2">
            <Car className="h-4 w-4" />
            Todos los Vehículos
          </TabsTrigger>
          <TabsTrigger value="moderacion" className="gap-2">
            <AlertTriangle className="h-4 w-4" />
            Moderación
            {pendingCount > 0 && (
              <Badge className="ml-1 bg-amber-500 px-1.5 py-0 text-xs text-white">
                {pendingCount}
              </Badge>
            )}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="todos">
          <VehiclesGridTab />
        </TabsContent>

        <TabsContent value="moderacion">
          <ModerationTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
