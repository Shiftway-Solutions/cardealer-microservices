/**
 * Dealer New Employee Page
 *
 * Add a new staff member to the dealer account
 * Connected to real APIs - P1 Integration
 */

'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { ArrowLeft, Mail, Shield, Building2, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { useCurrentDealer, useDealerLocations } from '@/hooks/use-dealers';
import { useInviteEmployee, useAvailableRoles } from '@/hooks/use-dealer-employees';
import type { DealerRole } from '@/services/dealer-employees';
import { toast } from 'sonner';
import { sanitizeEmail } from '@/lib/security/sanitize';

export default function NewEmployeePage() {
  const router = useRouter();

  // Get dealer data
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  // Get real locations from API
  const { data: locations, isLoading: locationsLoading } = useDealerLocations(dealerId);

  // Get available roles
  const { data: roles, isLoading: rolesLoading } = useAvailableRoles();

  // Invite mutation
  const inviteEmployee = useInviteEmployee(dealerId);

  // Form state
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<DealerRole | ''>('');

  const isLoading = dealerLoading || locationsLoading || rolesLoading;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email.trim() || !role) {
      toast.error('Email y rol son obligatorios');
      return;
    }

    const sanitizedEmail = sanitizeEmail(email);

    if (!sanitizedEmail) {
      toast.error('Email inválido');
      return;
    }

    try {
      await inviteEmployee.mutateAsync({
        email: sanitizedEmail,
        role: role as DealerRole,
      });
      toast.success('Invitación enviada exitosamente');
      router.push('/dealer/empleados');
    } catch {
      toast.error('Error al enviar la invitación');
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-3xl space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10" />
          <div>
            <Skeleton className="mb-2 h-8 w-48" />
            <Skeleton className="h-4 w-64" />
          </div>
        </div>
        {[1, 2].map(i => (
          <Card key={i}>
            <CardContent className="pt-6">
              <Skeleton className="mb-4 h-6 w-40" />
              <div className="grid grid-cols-2 gap-4">
                <Skeleton className="h-10" />
                <Skeleton className="h-10" />
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <div className="max-w-3xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link href="/dealer/empleados">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
        <div>
          <h1 className="text-2xl font-bold">Agregar Empleado</h1>
          <p className="text-muted-foreground">Invita a un nuevo miembro del equipo</p>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Email Invitation */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Mail className="h-5 w-5" />
              Invitación por Email
            </CardTitle>
            <CardDescription>
              Se enviará una invitación al email del nuevo empleado para que se una al equipo
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label htmlFor="email">Email del empleado *</Label>
              <div className="relative mt-2">
                <Mail className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
                <Input
                  id="email"
                  className="pl-10"
                  type="email"
                  placeholder="empleado@ejemplo.com"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  required
                />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Role */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Shield className="h-5 w-5" />
              Rol
            </CardTitle>
            <CardDescription>El rol determina los permisos y accesos del empleado</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label>Rol *</Label>
              <Select value={role} onValueChange={v => setRole(v as DealerRole)}>
                <SelectTrigger className="mt-2">
                  <SelectValue placeholder="Seleccionar rol" />
                </SelectTrigger>
                <SelectContent>
                  {roles && roles.length > 0 ? (
                    roles.map(r => (
                      <SelectItem key={r.id} value={r.name}>
                        <div>
                          <span className="font-medium">{r.name}</span>
                          {r.description && (
                            <span className="text-muted-foreground ml-2 text-xs">
                              - {r.description}
                            </span>
                          )}
                        </div>
                      </SelectItem>
                    ))
                  ) : (
                    <>
                      <SelectItem value="Admin">Administrador</SelectItem>
                      <SelectItem value="SalesManager">Gerente de Ventas</SelectItem>
                      <SelectItem value="Salesperson">Vendedor</SelectItem>
                      <SelectItem value="InventoryManager">Gestor de Inventario</SelectItem>
                      <SelectItem value="Viewer">Solo Lectura</SelectItem>
                    </>
                  )}
                </SelectContent>
              </Select>
            </div>

            {/* Role description cards */}
            <div className="grid gap-3">
              {[
                {
                  role: 'Admin',
                  desc: 'Acceso total a la plataforma, gestión de empleados y configuración',
                },
                {
                  role: 'SalesManager',
                  desc: 'Gestión de leads, citas, inventario y reportes de ventas',
                },
                {
                  role: 'Salesperson',
                  desc: 'Ver inventario, gestionar leads asignados y responder consultas',
                },
                {
                  role: 'InventoryManager',
                  desc: 'Gestión completa del inventario y publicación de vehículos',
                },
                { role: 'Viewer', desc: 'Solo puede ver información, sin permisos de edición' },
              ].map(item => (
                <div
                  key={item.role}
                  className={`cursor-pointer rounded-lg border p-3 transition-colors ${
                    role === item.role
                      ? 'border-primary bg-primary/10'
                      : 'border-border bg-muted/50 hover:bg-muted'
                  }`}
                  onClick={() => setRole(item.role as DealerRole)}
                >
                  <p className="text-sm font-medium">{item.role}</p>
                  <p className="text-muted-foreground text-xs">{item.desc}</p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Location Info (optional) */}
        {locations && locations.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Building2 className="h-5 w-5" />
                Ubicaciones Disponibles
              </CardTitle>
              <CardDescription>
                El empleado tendrá acceso a todas las ubicaciones del dealer
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                {locations.map(loc => (
                  <div key={loc.id} className="bg-muted/50 rounded-lg p-3 text-sm">
                    <p className="font-medium">{loc.name}</p>
                    {loc.address && <p className="text-muted-foreground text-xs">{loc.address}</p>}
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-4">
          <Button type="button" variant="outline" onClick={() => router.back()}>
            Cancelar
          </Button>
          <Button
            type="submit"
            className="bg-primary hover:bg-primary/90"
            disabled={!email.trim() || !role || inviteEmployee.isPending}
          >
            {inviteEmployee.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Enviando...
              </>
            ) : (
              <>
                <Mail className="mr-2 h-4 w-4" />
                Enviar Invitación
              </>
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
