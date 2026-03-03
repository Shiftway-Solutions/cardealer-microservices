/**
 * Dealer Employees Page
 *
 * Manage dealer staff and access permissions
 * Connected to real APIs - January 2026
 */

'use client';

import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Users,
  Plus,
  Search,
  Mail,
  Phone,
  Shield,
  Calendar,
  Star,
  TrendingUp,
  Edit,
  Trash2,
  Key,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useDealerEmployees,
  useComputedEmployeeStats,
  useRemoveEmployee,
} from '@/hooks/use-dealer-employees';
import {
  getRoleLabel,
  getInitials,
  formatJoinDate,
  type DealerRole,
} from '@/services/dealer-employees';
import { toast } from 'sonner';
import Image from 'next/image';

// ============================================================================
// Skeleton Components
// ============================================================================

function StatsSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
      {[1, 2, 3, 4].map(i => (
        <Card key={i}>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Skeleton className="h-10 w-10 rounded-lg" />
              <div className="space-y-2">
                <Skeleton className="h-6 w-12" />
                <Skeleton className="h-4 w-20" />
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function EmployeesListSkeleton() {
  return (
    <div className="space-y-4">
      {[1, 2, 3].map(i => (
        <Card key={i}>
          <CardContent className="p-6">
            <div className="flex flex-col gap-4 sm:flex-row">
              <div className="flex flex-1 items-start gap-4">
                <Skeleton className="h-12 w-12 rounded-full" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-5 w-40" />
                  <Skeleton className="h-4 w-64" />
                </div>
              </div>
              <div className="flex gap-6">
                {[1, 2, 3].map(j => (
                  <div key={j} className="space-y-1 text-center">
                    <Skeleton className="mx-auto h-6 w-8" />
                    <Skeleton className="h-3 w-12" />
                  </div>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

// ============================================================================
// Helper Components
// ============================================================================

const getRoleBadge = (role: DealerRole | string) => {
  const normalizedRole = role as DealerRole;
  switch (normalizedRole) {
    case 'Owner':
    case 'Admin':
      return (
        <Badge className="bg-purple-100 text-purple-700">{getRoleLabel(normalizedRole)}</Badge>
      );
    case 'SalesManager':
    case 'Salesperson':
      return <Badge className="bg-blue-100 text-blue-700">{getRoleLabel(normalizedRole)}</Badge>;
    case 'InventoryManager':
      return <Badge className="bg-green-100 text-green-700">{getRoleLabel(normalizedRole)}</Badge>;
    case 'Viewer':
      return <Badge variant="outline">{getRoleLabel(normalizedRole)}</Badge>;
    default:
      return <Badge variant="outline">{role}</Badge>;
  }
};

// ============================================================================
// Main Component
// ============================================================================

export default function DealerEmployeesPage() {
  const [searchQuery, setSearchQuery] = React.useState('');

  // Fetch dealer data
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();

  // Fetch employees
  const {
    data: employees,
    isLoading: employeesLoading,
    error,
    refetch,
  } = useDealerEmployees(dealer?.id || '');

  // Compute stats from employees
  const stats = useComputedEmployeeStats(employees);

  // Remove employee mutation
  const removeEmployee = useRemoveEmployee(dealer?.id || '');

  const isLoading = dealerLoading || employeesLoading;

  // Filter employees by search
  const filteredEmployees = React.useMemo(() => {
    if (!employees) return [];
    if (!searchQuery) return employees;
    const query = searchQuery.toLowerCase();
    return employees.filter(
      e =>
        e.name.toLowerCase().includes(query) ||
        e.email.toLowerCase().includes(query) ||
        getRoleLabel(e.role).toLowerCase().includes(query)
    );
  }, [employees, searchQuery]);

  // Handle remove employee
  const handleRemoveEmployee = (employeeId: string, employeeName: string) => {
    if (confirm(`¿Estás seguro de que deseas eliminar a ${employeeName}?`)) {
      removeEmployee.mutate(employeeId, {
        onSuccess: () => {
          toast.success(`${employeeName} ha sido eliminado del equipo`);
        },
        onError: () => {
          toast.error('Error al eliminar empleado');
        },
      });
    }
  };

  // ============================================================================
  // Render
  // ============================================================================

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <Skeleton className="h-8 w-40" />
            <Skeleton className="mt-2 h-4 w-60" />
          </div>
          <Skeleton className="h-10 w-40" />
        </div>
        <StatsSkeleton />
        <EmployeesListSkeleton />
      </div>
    );
  }

  if (!dealer) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <AlertCircle className="mb-4 h-12 w-12 text-muted-foreground" />
        <p className="text-lg font-medium text-muted-foreground">
          Necesitas una cuenta de dealer para acceder a esta función
        </p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <AlertCircle className="mb-4 h-12 w-12 text-red-400" />
        <p className="mb-4 text-lg font-medium text-muted-foreground">Error al cargar empleados</p>
        <Button onClick={() => refetch()} variant="outline">
          <RefreshCw className="mr-2 h-4 w-4" />
          Reintentar
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Empleados</h1>
          <p className="text-muted-foreground">Gestiona tu equipo y permisos</p>
        </div>
        <Button className="bg-primary hover:bg-primary/90">
          <Plus className="mr-2 h-4 w-4" />
          Invitar Empleado
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-primary/10 p-2">
                <Users className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats.totalEmployees}</p>
                <p className="text-sm text-muted-foreground">Empleados</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <TrendingUp className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats.totalLeads}</p>
                <p className="text-sm text-muted-foreground">Leads Totales</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <Star className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats.avgRating || '-'}</p>
                <p className="text-sm text-muted-foreground">Rating Promedio</p>
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
                <p className="text-2xl font-bold">
                  {dealer.maxActiveListings ? Math.ceil(dealer.maxActiveListings / 3) : 5}
                </p>
                <p className="text-sm text-muted-foreground">Límite Plan</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Search */}
      <div className="flex gap-4">
        <div className="relative flex-1">
          <Search className="absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar empleados..."
            className="pl-10"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
          />
        </div>
      </div>

      {/* Employees List */}
      <div className="space-y-4">
        {filteredEmployees.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <Users className="mb-4 h-12 w-12 text-muted-foreground" />
              <p className="text-lg font-medium text-muted-foreground">
                {searchQuery ? 'No se encontraron empleados' : 'No tienes empleados registrados'}
              </p>
              {!searchQuery && (
                <Button className="mt-4 bg-primary hover:bg-primary/90">
                  <Plus className="mr-2 h-4 w-4" />
                  Invitar Primer Empleado
                </Button>
              )}
            </CardContent>
          </Card>
        ) : (
          filteredEmployees.map(employee => (
            <Card key={employee.id}>
              <CardContent className="p-6">
                <div className="flex flex-col gap-4 sm:flex-row">
                  {/* Avatar and Info */}
                  <div className="flex flex-1 items-start gap-4">
                    <div
                      className={`flex h-12 w-12 items-center justify-center rounded-full text-lg font-medium ${
                        employee.role === 'Owner' || employee.role === 'Admin'
                          ? 'bg-purple-100 text-purple-700'
                          : 'bg-blue-100 text-blue-700'
                      }`}
                    >
                      {employee.avatarUrl ? (
                        <Image
                          src={employee.avatarUrl}
                          alt={employee.name}
                          width={48}
                          height={48}
                          className="h-12 w-12 rounded-full object-cover"
                        />
                      ) : (
                        getInitials(employee.name)
                      )}
                    </div>
                    <div className="flex-1">
                      <div className="mb-1 flex items-center gap-2">
                        <h3 className="font-semibold">{employee.name}</h3>
                        {getRoleBadge(employee.role)}
                      </div>
                      <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Mail className="h-3 w-3" />
                          {employee.email}
                        </span>
                        {employee.phone && (
                          <span className="flex items-center gap-1">
                            <Phone className="h-3 w-3" />
                            {employee.phone}
                          </span>
                        )}
                        <span className="flex items-center gap-1">
                          <Calendar className="h-3 w-3" />
                          Desde {formatJoinDate(employee.activationDate || employee.invitationDate)}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Stats */}
                  <div className="flex gap-6 sm:gap-8">
                    <div className="text-center">
                      <p className="text-xl font-bold">{employee.leadsCount || 0}</p>
                      <p className="text-xs text-muted-foreground">Leads</p>
                    </div>
                    <div className="text-center">
                      <p className="text-xl font-bold">{employee.salesCount || 0}</p>
                      <p className="text-xs text-muted-foreground">Ventas</p>
                    </div>
                    <div className="text-center">
                      <p className="flex items-center justify-center text-xl font-bold">
                        {employee.rating || '-'}
                        {employee.rating && <Star className="ml-1 h-3 w-3 text-amber-500" />}
                      </p>
                      <p className="text-xs text-muted-foreground">Rating</p>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex gap-2 sm:ml-4">
                    <Button variant="outline" size="sm">
                      <Edit className="mr-1 h-4 w-4" />
                      Editar
                    </Button>
                    <Button variant="outline" size="sm">
                      <Key className="mr-1 h-4 w-4" />
                      Permisos
                    </Button>
                    {employee.role !== 'Owner' && (
                      <Button
                        variant="outline"
                        size="sm"
                        className="text-red-600 hover:bg-red-50"
                        onClick={() => handleRemoveEmployee(employee.id, employee.name)}
                        disabled={removeEmployee.isPending}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>

      {/* Role Info */}
      <Card>
        <CardHeader>
          <CardTitle>Roles y Permisos</CardTitle>
          <CardDescription>Información sobre los niveles de acceso</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="rounded-lg border p-4">
              <div className="mb-2 flex items-center gap-2">
                <Shield className="h-5 w-5 text-purple-600" />
                <h4 className="font-medium">Administrador</h4>
              </div>
              <ul className="space-y-1 text-sm text-muted-foreground">
                <li>• Acceso completo al panel</li>
                <li>• Gestión de empleados</li>
                <li>• Configuración de cuenta</li>
                <li>• Facturación y suscripción</li>
              </ul>
            </div>
            <div className="rounded-lg border p-4">
              <div className="mb-2 flex items-center gap-2">
                <TrendingUp className="h-5 w-5 text-blue-600" />
                <h4 className="font-medium">Ventas</h4>
              </div>
              <ul className="space-y-1 text-sm text-muted-foreground">
                <li>• Gestión de inventario</li>
                <li>• Atención de leads</li>
                <li>• Mensajes y citas</li>
                <li>• Estadísticas propias</li>
              </ul>
            </div>
            <div className="rounded-lg border p-4">
              <div className="mb-2 flex items-center gap-2">
                <Users className="h-5 w-5 text-green-600" />
                <h4 className="font-medium">Soporte</h4>
              </div>
              <ul className="space-y-1 text-sm text-muted-foreground">
                <li>• Ver inventario</li>
                <li>• Responder mensajes</li>
                <li>• Gestión de citas</li>
                <li>• Solo lectura de stats</li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
