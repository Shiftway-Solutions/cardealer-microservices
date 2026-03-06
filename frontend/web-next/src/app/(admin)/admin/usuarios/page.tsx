/**
 * Admin Users Page
 *
 * User management page for administrators
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
  Users,
  Search,
  Mail,
  Phone,
  ChevronLeft,
  ChevronRight,
  UserCheck,
  UserX,
  Shield,
  Ban,
  Eye,
  Loader2,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  useAdminUsers,
  useUserStats,
  useUpdateUserStatus,
  useVerifyUser,
  useDeleteUser,
  type UserFilters,
} from '@/hooks/use-admin';

// =============================================================================
// SKELETON
// =============================================================================

function UsersSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-32" />
      </div>
      <div className="grid gap-4 md:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Skeleton key={i} className="h-24" />
        ))}
      </div>
      <Card>
        <CardContent className="p-0">
          {[1, 2, 3, 4, 5].map(i => (
            <div key={i} className="border-border flex items-center gap-4 border-b p-4">
              <Skeleton className="h-12 w-12 rounded-full" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-40" />
                <Skeleton className="h-3 w-60" />
              </div>
              <Skeleton className="h-8 w-24" />
            </div>
          ))}
        </CardContent>
      </Card>
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
    case 'suspended':
      return <Badge className="bg-amber-100 text-amber-700">Suspendido</Badge>;
    case 'pending':
      return <Badge className="bg-blue-100 text-blue-700">Pendiente</Badge>;
    case 'banned':
      return <Badge className="bg-red-100 text-red-700">Baneado</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
};

const getTypeBadge = (type: string, userIntent?: string) => {
  switch (type) {
    case 'seller':
      return (
        <Badge variant="outline" className="border-purple-300 text-purple-700">
          Vendedor Individual
        </Badge>
      );
    case 'buyer':
      return (
        <Badge variant="outline" className="border-gray-300 text-gray-600">
          {userIntent === 'buy' ? 'Comprador' : 'Comprador'}
        </Badge>
      );
    case 'dealer':
      return (
        <Badge variant="outline" className="border-blue-300 text-blue-700">
          Dealer
        </Badge>
      );
    default:
      return <Badge variant="outline">{type}</Badge>;
  }
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AdminUsersPage() {
  const [filters, setFilters] = useState<UserFilters>({
    page: 1,
    pageSize: 20,
  });
  const [searchTerm, setSearchTerm] = useState('');

  const { data: usersData, isLoading } = useAdminUsers(filters);
  const { data: stats } = useUserStats();
  const updateStatusMutation = useUpdateUserStatus();
  const verifyMutation = useVerifyUser();
  const deleteMutation = useDeleteUser();

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

  const handleStatusChange = async (userId: string, status: 'active' | 'suspended' | 'banned') => {
    try {
      await updateStatusMutation.mutateAsync({ id: userId, status });
      toast.success(
        `Usuario ${status === 'active' ? 'activado' : status === 'suspended' ? 'suspendido' : 'baneado'}`
      );
    } catch (error) {
      console.error('Error al actualizar estado de usuario:', error);
      toast.error('Error al actualizar estado');
    }
  };

  const handleVerify = async (userId: string) => {
    try {
      await verifyMutation.mutateAsync(userId);
      toast.success('Usuario verificado');
    } catch (error) {
      console.error('Error al verificar usuario:', error);
      toast.error('Error al verificar usuario');
    }
  };

  const handleDelete = async (userId: string) => {
    try {
      await deleteMutation.mutateAsync(userId);
      toast.success('Usuario eliminado');
    } catch (error) {
      console.error('Error al eliminar usuario:', error);
      toast.error('Error al eliminar usuario');
    }
  };

  if (isLoading) {
    return <UsersSkeleton />;
  }

  const users = usersData?.items || [];
  const totalPages = usersData?.totalPages || 1;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-foreground text-3xl font-bold">Usuarios</h1>
        <p className="text-muted-foreground">Gestiona los usuarios de la plataforma</p>
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Users className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.total.toLocaleString() || 0}</p>
                <p className="text-muted-foreground text-sm">Total</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 rounded-lg p-2">
                <UserCheck className="text-primary h-5 w-5" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.active.toLocaleString() || 0}</p>
                <p className="text-muted-foreground text-sm">Activos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <UserX className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.suspended || 0}</p>
                <p className="text-muted-foreground text-sm">Suspendidos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <Shield className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">+{stats?.newThisMonth || 0}</p>
                <p className="text-muted-foreground text-sm">Este mes</p>
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
                placeholder="Buscar por nombre, email..."
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
              value={filters.type || 'all'}
              onValueChange={v => handleFilterChange('type', v)}
            >
              <SelectTrigger className="w-40">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="buyer">Individuales</SelectItem>
                <SelectItem value="seller">Vendedores</SelectItem>
                <SelectItem value="dealer">Dealers</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.status || 'all'}
              onValueChange={v => handleFilterChange('status', v)}
            >
              <SelectTrigger className="w-40">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="active">Activos</SelectItem>
                <SelectItem value="pending">Pendientes</SelectItem>
                <SelectItem value="suspended">Suspendidos</SelectItem>
                <SelectItem value="banned">Baneados</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Users Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="border-border bg-muted/50 border-b">
                <tr>
                  <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                    Usuario
                  </th>
                  <th className="text-muted-foreground p-4 text-left text-sm font-medium">Tipo</th>
                  <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                    Estado
                  </th>
                  <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                    Verificado
                  </th>
                  <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                    Última Actividad
                  </th>
                  <th className="text-muted-foreground p-4 text-right text-sm font-medium">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody>
                {users.map(user => (
                  <tr key={user.id} className="hover:bg-muted/50 border-b">
                    <td className="p-4">
                      <div className="flex items-center gap-3">
                        <div className="bg-muted text-muted-foreground flex h-10 w-10 items-center justify-center rounded-full font-medium">
                          {user.name.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <p className="text-foreground font-medium">{user.name}</p>
                          <div className="text-muted-foreground flex items-center gap-3 text-sm">
                            <span className="flex items-center gap-1">
                              <Mail className="h-3 w-3" />
                              {user.email}
                            </span>
                            {user.phone && (
                              <span className="flex items-center gap-1">
                                <Phone className="h-3 w-3" />
                                {user.phone}
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="p-4">{getTypeBadge(user.type, user.userIntent)}</td>
                    <td className="p-4">{getStatusBadge(user.status)}</td>
                    <td className="p-4">
                      {user.verified ? (
                        <Badge className="bg-primary/10 text-primary">
                          <UserCheck className="mr-1 h-3 w-3" />
                          Verificado
                        </Badge>
                      ) : (
                        <Badge variant="outline" className="text-muted-foreground">
                          No verificado
                        </Badge>
                      )}
                    </td>
                    <td className="text-muted-foreground p-4 text-sm">
                      {user.lastActiveAt
                        ? new Date(user.lastActiveAt).toLocaleDateString('es-DO')
                        : 'Nunca'}
                    </td>
                    <td className="p-4">
                      <div className="flex justify-end gap-2">
                        <Link href={`/admin/usuarios/${user.id}`}>
                          <Button variant="ghost" size="sm">
                            <Eye className="h-4 w-4" />
                          </Button>
                        </Link>
                        {!user.verified && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleVerify(user.id)}
                            disabled={verifyMutation.isPending}
                          >
                            {verifyMutation.isPending ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <UserCheck className="text-primary h-4 w-4" />
                            )}
                          </Button>
                        )}
                        {user.status === 'active' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleStatusChange(user.id, 'suspended')}
                            disabled={updateStatusMutation.isPending}
                          >
                            <UserX className="h-4 w-4 text-amber-600" />
                          </Button>
                        )}
                        {user.status === 'suspended' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleStatusChange(user.id, 'active')}
                            disabled={updateStatusMutation.isPending}
                          >
                            <UserCheck className="text-primary h-4 w-4" />
                          </Button>
                        )}
                        <AlertDialog>
                          <AlertDialogTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <Ban className="h-4 w-4 text-red-600" />
                            </Button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>¿Eliminar usuario?</AlertDialogTitle>
                              <AlertDialogDescription>
                                Esta acción no se puede deshacer. Se eliminará permanentemente el
                                usuario {user.name} y todos sus datos.
                              </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                              <AlertDialogCancel>Cancelar</AlertDialogCancel>
                              <AlertDialogAction
                                onClick={() => handleDelete(user.id)}
                                className="bg-red-600 hover:bg-red-700"
                              >
                                Eliminar
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {users.length === 0 && (
            <div className="text-muted-foreground py-12 text-center">
              <Users className="mx-auto mb-2 h-8 w-8" />
              <p>No se encontraron usuarios</p>
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="border-border flex items-center justify-between border-t p-4">
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
        </CardContent>
      </Card>
    </div>
  );
}
