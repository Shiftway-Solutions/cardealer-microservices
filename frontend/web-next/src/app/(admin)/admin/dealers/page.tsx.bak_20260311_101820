/**
 * Admin Dealers Page
 *
 * Dealer management page for administrators
 * Connected to real APIs - February 2026
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Building2,
  Search,
  ChevronLeft,
  ChevronRight,
  Eye,
  CheckCircle,
  Clock,
  Star,
  Car,
  DollarSign,
  Loader2,
  Ban,
  Play,
  Mail,
  Phone,
  MapPin,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  useAdminDealers,
  useDealerStatsAdmin,
  useVerifyDealer,
  useSuspendDealer,
  useReactivateDealer,
  type DealerFilters,
} from '@/hooks/use-admin';

// =============================================================================
// SKELETON
// =============================================================================

function DealersSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-32" />
      </div>
      <div className="grid gap-4 md:grid-cols-5">
        {[1, 2, 3, 4, 5].map(i => (
          <Skeleton key={i} className="h-24" />
        ))}
      </div>
      <Skeleton className="h-96" />
    </div>
  );
}

// =============================================================================
// HELPERS
// =============================================================================

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'active':
      return <Badge className="bg-primary/10 text-primary">Activo</Badge>;
    case 'pending':
      return <Badge className="bg-amber-100 text-amber-700">Pendiente</Badge>;
    case 'suspended':
      return <Badge className="bg-red-100 text-red-700">Suspendido</Badge>;
    case 'rejected':
      return <Badge className="bg-muted text-foreground">Rechazado</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
};

const getPlanBadge = (plan: string) => {
  switch (plan) {
    case 'elite':
      return <Badge className="bg-amber-100 text-amber-700">ÉLITE</Badge>;
    case 'pro':
      return <Badge className="bg-purple-100 text-purple-700">PRO</Badge>;
    case 'visible':
      return <Badge className="bg-blue-100 text-blue-700">VISIBLE</Badge>;
    case 'libre':
      return <Badge className="bg-muted text-foreground">LIBRE</Badge>;
    default:
      return <Badge variant="outline">{plan}</Badge>;
  }
};

const formatMoney = (amount: number) => {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    minimumFractionDigits: 0,
  }).format(amount);
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AdminDealersPage() {
  const [filters, setFilters] = useState<DealerFilters>({
    page: 1,
    pageSize: 10,
  });
  const [searchTerm, setSearchTerm] = useState('');

  const { data: dealersData, isLoading } = useAdminDealers(filters);
  const { data: stats } = useDealerStatsAdmin();
  const verifyMutation = useVerifyDealer();
  const suspendMutation = useSuspendDealer();
  const reactivateMutation = useReactivateDealer();

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

  const handleVerify = async (id: string) => {
    try {
      await verifyMutation.mutateAsync(id);
      toast.success('Dealer verificado exitosamente');
    } catch (error) {
      console.error('Error al verificar dealer:', error);
      toast.error('Error al verificar dealer');
    }
  };

  const handleSuspend = async (id: string, reason: string) => {
    try {
      await suspendMutation.mutateAsync({ id, reason });
      toast.success('Dealer suspendido');
    } catch (error) {
      console.error('Error al suspender dealer:', error);
      toast.error('Error al suspender dealer');
    }
  };

  const handleReactivate = async (id: string) => {
    try {
      await reactivateMutation.mutateAsync(id);
      toast.success('Dealer reactivado');
    } catch (error) {
      console.error('Error al reactivar dealer:', error);
      toast.error('Error al reactivar dealer');
    }
  };

  if (isLoading) {
    return <DealersSkeleton />;
  }

  const dealers = dealersData?.items || [];
  const totalPages = dealersData?.totalPages || 1;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-3xl font-bold">Dealers</h1>
          <p className="text-muted-foreground">Gestiona los dealers de la plataforma</p>
        </div>
        <Button variant="outline" onClick={() => handleFilterChange('status', 'pending')}>
          <Clock className="mr-2 h-4 w-4" />
          Pendientes ({stats?.pending || 0})
        </Button>
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-5">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Building2 className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.total || 0}</p>
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
                <p className="text-xl font-bold">{stats?.active || 0}</p>
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
                <Car className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.byPlan?.elite || 0}</p>
                <p className="text-muted-foreground text-xs">ÉLITE</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-green-100 p-2">
                <DollarSign className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{formatMoney(stats?.totalMrr || 0)}</p>
                <p className="text-muted-foreground text-xs">MRR</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-wrap gap-4">
            <div className="flex flex-1 gap-2">
              <Input
                placeholder="Buscar por nombre, email, RNC..."
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
                <SelectItem value="suspended">Suspendidos</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.plan || 'all'}
              onValueChange={v => handleFilterChange('plan', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Plan" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="libre">LIBRE</SelectItem>
                <SelectItem value="visible">VISIBLE</SelectItem>
                <SelectItem value="pro">PRO</SelectItem>
                <SelectItem value="elite">ÉLITE</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.verified?.toString() || 'all'}
              onValueChange={v => handleFilterChange('verified', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Verificado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="true">Verificados</SelectItem>
                <SelectItem value="false">Sin verificar</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Dealers Table */}
      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Dealer</TableHead>
              <TableHead>Contacto</TableHead>
              <TableHead>Plan</TableHead>
              <TableHead>Vehículos</TableHead>
              <TableHead>Rating</TableHead>
              <TableHead>MRR</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead>Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {dealers.map(dealer => (
              <TableRow key={dealer.id}>
                <TableCell>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="font-medium">{dealer.name}</p>
                      {dealer.verified && <CheckCircle className="h-4 w-4 text-blue-500" />}
                    </div>
                    <p className="text-muted-foreground flex items-center gap-1 text-sm">
                      <MapPin className="h-3 w-3" />
                      {dealer.location}
                    </p>
                  </div>
                </TableCell>
                <TableCell>
                  <div className="space-y-1">
                    <p className="flex items-center gap-1 text-sm">
                      <Mail className="h-3 w-3" />
                      {dealer.email}
                    </p>
                    <p className="text-muted-foreground flex items-center gap-1 text-sm">
                      <Phone className="h-3 w-3" />
                      {dealer.phone}
                    </p>
                  </div>
                </TableCell>
                <TableCell>{getPlanBadge(dealer.plan)}</TableCell>
                <TableCell>
                  <div className="flex items-center gap-1">
                    <Car className="text-muted-foreground h-4 w-4" />
                    {dealer.vehiclesCount}
                  </div>
                </TableCell>
                <TableCell>
                  {dealer.rating > 0 ? (
                    <div className="flex items-center gap-1">
                      <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
                      {dealer.rating.toFixed(1)}
                    </div>
                  ) : (
                    <span className="text-muted-foreground">-</span>
                  )}
                </TableCell>
                <TableCell>{formatMoney(dealer.mrr)}</TableCell>
                <TableCell>{getStatusBadge(dealer.status)}</TableCell>
                <TableCell>
                  <div className="flex gap-1">
                    <Link href={`/admin/dealers/${dealer.id}`}>
                      <Button variant="ghost" size="sm">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </Link>
                    {dealer.status === 'pending' && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-primary"
                        onClick={() => handleVerify(dealer.id)}
                        disabled={verifyMutation.isPending}
                      >
                        {verifyMutation.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <CheckCircle className="h-4 w-4" />
                        )}
                      </Button>
                    )}
                    {dealer.status === 'active' && (
                      <AlertDialog>
                        <AlertDialogTrigger asChild>
                          <Button variant="ghost" size="sm" className="text-red-600">
                            <Ban className="h-4 w-4" />
                          </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                          <AlertDialogHeader>
                            <AlertDialogTitle>¿Suspender dealer?</AlertDialogTitle>
                            <AlertDialogDescription>
                              El dealer no podrá acceder a su cuenta ni gestionar vehículos.
                            </AlertDialogDescription>
                          </AlertDialogHeader>
                          <AlertDialogFooter>
                            <AlertDialogCancel>Cancelar</AlertDialogCancel>
                            <AlertDialogAction
                              onClick={() => handleSuspend(dealer.id, 'Violación de términos')}
                              className="bg-red-600 hover:bg-red-700"
                            >
                              Suspender
                            </AlertDialogAction>
                          </AlertDialogFooter>
                        </AlertDialogContent>
                      </AlertDialog>
                    )}
                    {dealer.status === 'suspended' && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-primary"
                        onClick={() => handleReactivate(dealer.id)}
                        disabled={reactivateMutation.isPending}
                      >
                        {reactivateMutation.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Play className="h-4 w-4" />
                        )}
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {dealers.length === 0 && (
              <TableRow>
                <TableCell colSpan={8} className="text-muted-foreground py-12 text-center">
                  <Building2 className="mx-auto mb-2 h-8 w-8" />
                  <p>No se encontraron dealers</p>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Card>

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
