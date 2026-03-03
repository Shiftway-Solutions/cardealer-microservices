/**
 * Admin User Detail Page
 *
 * View and manage individual user details
 */

'use client';

import { useState } from 'react';
import { useParams } from 'next/navigation';
import { formatDateTime } from '@/lib/utils';
import { useAdminUser, useUpdateUserStatus, useVerifyUser } from '@/hooks/use-admin';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  ArrowLeft,
  Mail,
  Phone,
  Calendar,
  MapPin,
  Shield,
  Car,
  Heart,
  MessageSquare,
  Ban,
  CheckCircle,
  AlertTriangle,
  Clock,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';

export default function UserDetailPage() {
  const params = useParams();
  const userId = params.id as string;
  const [activeTab, setActiveTab] = useState('overview');

  const { data: userData, isLoading, isError, error } = useAdminUser(userId);
  const updateStatus = useUpdateUserStatus();
  const verifyUserMutation = useVerifyUser();

  const formatDate = formatDateTime;

  if (isLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <span className="ml-3 text-muted-foreground">Cargando usuario...</span>
      </div>
    );
  }

  if (isError || !userData) {
    return (
      <div className="space-y-4">
        <Link href="/admin/usuarios">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
        <Card className="border-destructive">
          <CardContent className="py-8 text-center">
            <AlertTriangle className="mx-auto mb-4 h-10 w-10 text-destructive" />
            <p className="text-lg font-medium">No se pudo cargar el usuario</p>
            <p className="text-muted-foreground text-sm">
              {error instanceof Error ? error.message : 'El usuario no existe o hubo un error de conexión.'}
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const displayName = userData.name || 'Sin nombre';
  const location = [userData.city, userData.province].filter(Boolean).join(', ') || userData.country || 'No especificada';

  const typeLabel: Record<string, string> = {
    buyer: 'Comprador',
    seller: 'Vendedor',
    dealer: 'Dealer',
  };

  const typeBadgeColor: Record<string, string> = {
    buyer: 'bg-blue-600',
    seller: 'bg-purple-600',
    dealer: 'bg-amber-600',
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/admin/usuarios">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Detalle de Usuario</h1>
            <p className="text-muted-foreground text-xs">ID: {userData.id}</p>
          </div>
        </div>
        <div className="flex gap-2">
          {!userData.verified && (
            <Button
              variant="outline"
              className="border-primary text-primary"
              onClick={() => verifyUserMutation.mutate(userData.id)}
              disabled={verifyUserMutation.isPending}
            >
              <CheckCircle className="mr-2 h-4 w-4" />
              Verificar
            </Button>
          )}
          {userData.status === 'active' ? (
            <Button
              variant="outline"
              className="border-red-600 text-red-600"
              onClick={() => updateStatus.mutate({ id: userData.id, status: 'suspended' })}
              disabled={updateStatus.isPending}
            >
              <Ban className="mr-2 h-4 w-4" />
              Suspender
            </Button>
          ) : (
            <Button
              variant="outline"
              className="border-primary text-primary"
              onClick={() => updateStatus.mutate({ id: userData.id, status: 'active' })}
              disabled={updateStatus.isPending}
            >
              <CheckCircle className="mr-2 h-4 w-4" />
              Activar
            </Button>
          )}
        </div>
      </div>

      {/* User Card */}
      <Card>
        <CardContent className="py-6">
          <div className="flex items-start gap-6">
            <Avatar className="h-20 w-20">
              <AvatarImage src={userData.avatar || undefined} />
              <AvatarFallback className="bg-primary text-2xl">
                {displayName
                  .split(' ')
                  .map(n => n[0])
                  .join('')
                  .slice(0, 2)}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1">
              <div className="mb-2 flex items-center gap-3">
                <h2 className="text-xl font-bold">{displayName}</h2>
                {userData.verified && (
                  <Badge className="bg-primary">
                    <CheckCircle className="mr-1 h-3 w-3" />
                    Verificado
                  </Badge>
                )}
                <Badge
                  className={
                    userData.status === 'active'
                      ? 'bg-primary/10 text-primary'
                      : userData.status === 'suspended'
                        ? 'bg-red-100 text-red-700'
                        : 'bg-yellow-100 text-yellow-700'
                  }
                >
                  {userData.status === 'active' ? 'Activo' : userData.status === 'suspended' ? 'Suspendido' : userData.status}
                </Badge>
                <Badge className={typeBadgeColor[userData.type] || 'bg-gray-600'}>
                  {typeLabel[userData.type] || userData.type}
                </Badge>
              </div>
              <div className="grid grid-cols-2 gap-4 text-sm md:grid-cols-4">
                <div className="text-muted-foreground flex items-center gap-2">
                  <Mail className="h-4 w-4" />
                  <span>{userData.email}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Phone className="h-4 w-4" />
                  <span>{userData.phone || 'No registrado'}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  <span>{location}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  <span>Desde {new Date(userData.createdAt).toLocaleDateString('es-DO')}</span>
                </div>
              </div>
            </div>
            <div className="text-right">
              <p className="text-muted-foreground text-xs">Último acceso</p>
              <p className="text-sm text-muted-foreground">
                {userData.lastActiveAt ? formatDate(userData.lastActiveAt) : 'Sin datos'}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Car className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Vehículos</p>
                <p className="text-2xl font-bold">{userData.vehiclesCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-pink-100 p-2">
                <Heart className="h-5 w-5 text-pink-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Favoritos</p>
                <p className="text-2xl font-bold">{userData.favoritesCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <MessageSquare className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Transacciones</p>
                <p className="text-2xl font-bold">{userData.dealsCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-100 p-2">
                <AlertTriangle className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Reportes</p>
                <p className="text-2xl font-bold">{userData.reports?.length ?? 0}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="overview">Resumen</TabsTrigger>
          <TabsTrigger value="activity">Actividad</TabsTrigger>
          <TabsTrigger value="vehicles">Vehículos</TabsTrigger>
          <TabsTrigger value="security">Seguridad</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="mt-6">
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Información Personal</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex justify-between border-b border-border py-2">
                  <span className="text-muted-foreground">Nombre completo</span>
                  <span className="font-medium">{displayName}</span>
                </div>
                <div className="flex justify-between border-b border-border py-2">
                  <span className="text-muted-foreground">Email</span>
                  <span className="font-medium">{userData.email}</span>
                </div>
                <div className="flex justify-between border-b border-border py-2">
                  <span className="text-muted-foreground">Teléfono</span>
                  <span className="font-medium">{userData.phone || 'No registrado'}</span>
                </div>
                <div className="flex justify-between border-b border-border py-2">
                  <span className="text-muted-foreground">Ubicación</span>
                  <span className="font-medium">{location}</span>
                </div>
                <div className="flex justify-between py-2">
                  <span className="text-muted-foreground">Tipo</span>
                  <Badge className={typeBadgeColor[userData.type] || 'bg-gray-600'}>
                    {typeLabel[userData.type] || userData.type}
                  </Badge>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Actividad Reciente</CardTitle>
              </CardHeader>
              <CardContent>
                {userData.recentActivity && userData.recentActivity.length > 0 ? (
                  <div className="space-y-4">
                    {userData.recentActivity.slice(0, 4).map(activity => (
                      <div key={activity.id} className="flex items-start gap-3">
                        <div className="rounded-full bg-muted p-1.5">
                          <Clock className="text-muted-foreground h-3 w-3" />
                        </div>
                        <div className="flex-1">
                          <p className="text-sm font-medium">{activity.action}</p>
                          {activity.target && (
                            <p className="text-muted-foreground text-xs">{activity.target}</p>
                          )}
                          <p className="text-muted-foreground text-xs">{formatDate(activity.timestamp)}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-muted-foreground text-sm">Sin actividad reciente registrada.</p>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="activity" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Historial de Actividad</CardTitle>
            </CardHeader>
            <CardContent>
              {userData.recentActivity && userData.recentActivity.length > 0 ? (
                <div className="space-y-4">
                  {userData.recentActivity.map(activity => (
                    <div
                      key={activity.id}
                      className="flex items-center justify-between rounded-lg bg-muted/50 p-4"
                    >
                      <div className="flex items-center gap-4">
                        <Clock className="text-muted-foreground h-5 w-5" />
                        <div>
                          <p className="font-medium">{activity.action}</p>
                          {activity.target && (
                            <p className="text-muted-foreground text-sm">{activity.target}</p>
                          )}
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="text-sm text-muted-foreground">{formatDate(activity.timestamp)}</p>
                        {activity.ipAddress && (
                          <p className="text-muted-foreground text-xs">IP: {activity.ipAddress}</p>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-muted-foreground text-sm">Sin actividad registrada.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="vehicles" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Vehículos del Usuario</CardTitle>
            </CardHeader>
            <CardContent>
              {userData.vehicles && userData.vehicles.length > 0 ? (
                <div className="space-y-4">
                  {userData.vehicles.map(vehicle => (
                    <div
                      key={vehicle.id}
                      className="flex items-center justify-between rounded-lg bg-muted/50 p-4"
                    >
                      <div className="flex items-center gap-4">
                        <div className="rounded-lg bg-muted p-3">
                          <Car className="h-5 w-5 text-muted-foreground" />
                        </div>
                        <div>
                          <p className="font-medium">{vehicle.title}</p>
                          <p className="text-muted-foreground text-sm">
                            Estado: {vehicle.status} · Creado: {new Date(vehicle.createdAt).toLocaleDateString('es-DO')}
                          </p>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="font-bold text-primary">
                          RD$ {vehicle.price.toLocaleString()}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-muted-foreground text-sm">Este usuario no tiene vehículos publicados.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="security" className="mt-6">
          <Card>
            <CardHeader>
              <CardTitle>Seguridad de la Cuenta</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between rounded-lg bg-muted/50 p-4">
                <div className="flex items-center gap-3">
                  <Shield className={`h-5 w-5 ${userData.emailVerified ? 'text-primary' : 'text-yellow-600'}`} />
                  <div>
                    <p className="font-medium">Email Verificado</p>
                    <p className="text-muted-foreground text-sm">{userData.email}</p>
                  </div>
                </div>
                <Badge className={userData.emailVerified ? 'bg-primary/10 text-primary' : 'bg-yellow-100 text-yellow-700'}>
                  {userData.emailVerified ? 'Verificado' : 'Pendiente'}
                </Badge>
              </div>
              <div className="flex items-center justify-between rounded-lg bg-muted/50 p-4">
                <div className="flex items-center gap-3">
                  <Phone className={`h-5 w-5 ${userData.phone ? 'text-primary' : 'text-yellow-600'}`} />
                  <div>
                    <p className="font-medium">Teléfono</p>
                    <p className="text-muted-foreground text-sm">{userData.phone || 'No registrado'}</p>
                  </div>
                </div>
                <Badge className={userData.phone ? 'bg-primary/10 text-primary' : 'bg-yellow-100 text-yellow-700'}>
                  {userData.phone ? 'Registrado' : 'Pendiente'}
                </Badge>
              </div>
              <div className="flex items-center justify-between rounded-lg bg-muted/50 p-4">
                <div className="flex items-center gap-3">
                  <Shield className={`h-5 w-5 ${userData.twoFactorEnabled ? 'text-primary' : 'text-yellow-600'}`} />
                  <div>
                    <p className="font-medium">Autenticación 2FA</p>
                    <p className="text-muted-foreground text-sm">
                      {userData.twoFactorEnabled ? 'Configurada' : 'No configurada'}
                    </p>
                  </div>
                </div>
                <Badge className={userData.twoFactorEnabled ? 'bg-primary/10 text-primary' : 'bg-yellow-100 text-yellow-700'}>
                  {userData.twoFactorEnabled ? 'Activo' : 'Pendiente'}
                </Badge>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
