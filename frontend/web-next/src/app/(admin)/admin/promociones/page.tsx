/**
 * Admin Promotions Management Page
 *
 * Manage promotional campaigns and featured listings
 */

'use client';

import * as React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Megaphone,
  Plus,
  Search,
  Calendar,
  AlertCircle,
  Eye,
  Pause,
  Play,
  Trash2,
  Edit,
  Target,
} from 'lucide-react';
import { format } from 'date-fns';
import { es } from 'date-fns/locale';

interface Promotion {
  id: string;
  name: string;
  description: string;
  type: 'featured' | 'discount' | 'bundle' | 'early_bird';
  status: 'active' | 'scheduled' | 'paused' | 'expired' | 'draft';
  startDate: string;
  endDate: string;
  targetAudience: string;
  impressions: number;
  clicks: number;
  conversions: number;
  budget?: number;
  spent?: number;
}

const statusColors: Record<string, string> = {
  active: 'bg-green-100 text-green-700',
  scheduled: 'bg-blue-100 text-blue-700',
  paused: 'bg-amber-100 text-amber-700',
  expired: 'bg-gray-100 text-gray-700',
  draft: 'bg-slate-100 text-slate-700',
};

const statusLabels: Record<string, string> = {
  active: 'Activa',
  scheduled: 'Programada',
  paused: 'Pausada',
  expired: 'Expirada',
  draft: 'Borrador',
};

const typeLabels: Record<string, string> = {
  featured: 'Destacado',
  discount: 'Descuento',
  bundle: 'Bundle',
  early_bird: 'Early Bird',
};

export default function PromocionesPage() {
  const [promotions, setPromotions] = React.useState<Promotion[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [search, setSearch] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState<string>('all');

  React.useEffect(() => {
    async function fetchPromotions() {
      try {
        setLoading(true);
        // TODO: Replace with real API call when endpoint is available
        // const data = await adminService.getPromotions({ search, status: statusFilter });
        setPromotions([]);
      } catch (err) {
        if (process.env.NODE_ENV === 'development')
          console.error('Error fetching promotions:', err);
        setError('No se pudieron cargar las promociones.');
      } finally {
        setLoading(false);
      }
    }
    fetchPromotions();
  }, [search, statusFilter]);

  const filteredPromotions = promotions.filter(p => {
    const matchesSearch = !search || p.name.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = statusFilter === 'all' || p.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Promociones</h1>
        <div className="grid gap-4 sm:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="h-16 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-[300px] items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <p className="text-muted-foreground mt-4">{error}</p>
        </div>
      </div>
    );
  }

  const stats = {
    active: promotions.filter(p => p.status === 'active').length,
    totalImpressions: promotions.reduce((s, p) => s + p.impressions, 0),
    totalConversions: promotions.reduce((s, p) => s + p.conversions, 0),
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Promociones</h1>
          <p className="text-muted-foreground">
            Gestiona campañas promocionales y listados destacados
          </p>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Nueva Promoción
        </Button>
      </div>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-green-50 p-3">
              <Megaphone className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Activas</p>
              <p className="text-2xl font-bold">{stats.active}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-blue-50 p-3">
              <Eye className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Impresiones Totales</p>
              <p className="text-2xl font-bold">{stats.totalImpressions.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-purple-50 p-3">
              <Target className="h-6 w-6 text-purple-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Conversiones</p>
              <p className="text-2xl font-bold">{stats.totalConversions.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="flex flex-col gap-4 p-4 sm:flex-row">
          <div className="relative flex-1">
            <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
            <Input
              placeholder="Buscar promociones..."
              className="pl-9"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-full sm:w-[180px]">
              <SelectValue placeholder="Estado" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="active">Activa</SelectItem>
              <SelectItem value="scheduled">Programada</SelectItem>
              <SelectItem value="paused">Pausada</SelectItem>
              <SelectItem value="expired">Expirada</SelectItem>
              <SelectItem value="draft">Borrador</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {/* List */}
      {filteredPromotions.length === 0 ? (
        <div className="flex min-h-[200px] items-center justify-center">
          <div className="text-center">
            <Megaphone className="text-muted-foreground mx-auto h-12 w-12" />
            <p className="text-muted-foreground mt-4">No hay promociones todavía</p>
            <Button className="mt-4" variant="outline">
              <Plus className="mr-2 h-4 w-4" />
              Crear primera promoción
            </Button>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredPromotions.map(promo => (
            <Card key={promo.id} className="transition-shadow hover:shadow-md">
              <CardContent className="p-6">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-semibold">{promo.name}</h3>
                      <Badge className={statusColors[promo.status]}>
                        {statusLabels[promo.status]}
                      </Badge>
                      <Badge variant="outline">{typeLabels[promo.type]}</Badge>
                    </div>
                    <p className="text-muted-foreground mt-1 text-sm">{promo.description}</p>
                    <div className="text-muted-foreground mt-2 flex gap-4 text-xs">
                      <span className="flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        {format(new Date(promo.startDate), 'dd MMM', { locale: es })} -{' '}
                        {format(new Date(promo.endDate), 'dd MMM yyyy', { locale: es })}
                      </span>
                      <span>{promo.impressions.toLocaleString()} impresiones</span>
                      <span>{promo.clicks} clicks</span>
                      <span>{promo.conversions} conversiones</span>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button variant="ghost" size="icon" aria-label="Editar promoción">
                      <Edit className="h-4 w-4" />
                    </Button>
                    {promo.status === 'active' ? (
                      <Button variant="ghost" size="icon" aria-label="Pausar promoción">
                        <Pause className="h-4 w-4" />
                      </Button>
                    ) : promo.status === 'paused' ? (
                      <Button variant="ghost" size="icon" aria-label="Reanudar promoción">
                        <Play className="h-4 w-4" />
                      </Button>
                    ) : null}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-red-500"
                      aria-label="Eliminar promoción"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
