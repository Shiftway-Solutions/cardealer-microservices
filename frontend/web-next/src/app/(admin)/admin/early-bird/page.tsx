/**
 * Admin Early Bird Program Page
 *
 * Manage the early bird pricing program for dealers
 */

'use client';

import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import { Progress } from '@/components/ui/progress';
import {
  Sunrise,
  Users,
  DollarSign,
  Calendar,
  AlertCircle,
  CheckCircle2,
  Search,
  Settings,
  UserPlus,
} from 'lucide-react';

interface EarlyBirdConfig {
  isActive: boolean;
  maxSlots: number;
  usedSlots: number;
  discountPercentage: number;
  lockInMonths: number;
  startDate: string;
  endDate?: string;
}

interface EarlyBirdMember {
  id: string;
  dealerName: string;
  email: string;
  plan: string;
  joinedAt: string;
  discountApplied: number;
  status: 'active' | 'expired' | 'cancelled';
  lockedUntil: string;
  monthlySavings: number;
}

export default function EarlyBirdPage() {
  const [config, setConfig] = React.useState<EarlyBirdConfig | null>(null);
  const [members, setMembers] = React.useState<EarlyBirdMember[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [search, setSearch] = React.useState('');

  React.useEffect(() => {
    async function fetchData() {
      try {
        setLoading(true);
        // TODO: Replace with real API calls
        // const [configData, membersData] = await Promise.all([
        //   adminService.getEarlyBirdConfig(),
        //   adminService.getEarlyBirdMembers(),
        // ]);
        setConfig({
          isActive: true,
          maxSlots: 100,
          usedSlots: 0,
          discountPercentage: 40,
          lockInMonths: 12,
          startDate: new Date().toISOString(),
        });
        setMembers([]);
      } catch (err) {
        if (process.env.NODE_ENV === 'development')
          console.error('Error fetching early bird data:', err);
        setError('No se pudieron cargar los datos del programa Early Bird.');
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  const filteredMembers = members.filter(
    m =>
      !search ||
      m.dealerName.toLowerCase().includes(search.toLowerCase()) ||
      m.email.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Early Bird</h1>
        <div className="grid gap-4 sm:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="h-20 w-full" />
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

  const slotsPercentage = config ? (config.usedSlots / config.maxSlots) * 100 : 0;
  const activeMembersCount = members.filter(m => m.status === 'active').length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground flex items-center gap-2 text-2xl font-bold">
            <Sunrise className="h-7 w-7 text-amber-500" />
            Programa Early Bird
          </h1>
          <p className="text-muted-foreground">
            Gestiona el programa de precios preferenciales para dealers tempranos
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <Settings className="mr-2 h-4 w-4" />
            Configurar
          </Button>
          <Badge variant={config?.isActive ? 'default' : 'secondary'} className="h-9 px-4 text-sm">
            {config?.isActive ? '🟢 Activo' : '⚫ Inactivo'}
          </Badge>
        </div>
      </div>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center gap-4">
              <div className="rounded-lg bg-amber-50 p-3">
                <Users className="h-6 w-6 text-amber-600" />
              </div>
              <div className="flex-1">
                <p className="text-muted-foreground text-sm">Espacios Usados</p>
                <p className="text-2xl font-bold">
                  {config?.usedSlots ?? 0} / {config?.maxSlots ?? 0}
                </p>
                <Progress value={slotsPercentage} className="mt-2 h-1.5" />
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-green-50 p-3">
              <CheckCircle2 className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Miembros Activos</p>
              <p className="text-2xl font-bold">{activeMembersCount}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-blue-50 p-3">
              <DollarSign className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Descuento</p>
              <p className="text-2xl font-bold">{config?.discountPercentage ?? 0}%</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-purple-50 p-3">
              <Calendar className="h-6 w-6 text-purple-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Lock-in</p>
              <p className="text-2xl font-bold">{config?.lockInMonths ?? 0} meses</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Members Section */}
      <Card>
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle>Miembros del Programa</CardTitle>
            <div className="relative w-full sm:w-64">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <Input
                placeholder="Buscar miembro..."
                className="pl-9"
                value={search}
                onChange={e => setSearch(e.target.value)}
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {filteredMembers.length === 0 ? (
            <div className="flex min-h-[200px] items-center justify-center">
              <div className="text-center">
                <UserPlus className="text-muted-foreground mx-auto h-12 w-12" />
                <p className="text-muted-foreground mt-4">
                  No hay miembros en el programa Early Bird todavía
                </p>
              </div>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/50 border-b">
                    <th className="px-4 py-3 text-left font-medium">Dealer</th>
                    <th className="px-4 py-3 text-left font-medium">Plan</th>
                    <th className="px-4 py-3 text-left font-medium">Fecha Ingreso</th>
                    <th className="px-4 py-3 text-right font-medium">Descuento</th>
                    <th className="px-4 py-3 text-right font-medium">Ahorro/Mes</th>
                    <th className="px-4 py-3 text-center font-medium">Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredMembers.map(member => (
                    <tr key={member.id} className="border-b last:border-0">
                      <td className="px-4 py-3">
                        <p className="font-medium">{member.dealerName}</p>
                        <p className="text-muted-foreground text-xs">{member.email}</p>
                      </td>
                      <td className="px-4 py-3">{member.plan}</td>
                      <td className="px-4 py-3">
                        {new Date(member.joinedAt).toLocaleDateString('es-DO')}
                      </td>
                      <td className="px-4 py-3 text-right">{member.discountApplied}%</td>
                      <td className="px-4 py-3 text-right font-medium text-green-600">
                        RD${member.monthlySavings.toLocaleString()}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Badge variant={member.status === 'active' ? 'default' : 'secondary'}>
                          {member.status === 'active'
                            ? 'Activo'
                            : member.status === 'expired'
                              ? 'Expirado'
                              : 'Cancelado'}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
