/**
 * Admin Dealer Detail Page
 *
 * View and manage individual dealer details
 */

'use client';

import { useState } from 'react';
import { useParams } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ArrowLeft,
  Mail,
  Phone,
  Calendar,
  MapPin,
  Car,
  Users,
  DollarSign,
  Star,
  Ban,
  CheckCircle,
  AlertTriangle,
  FileText,
  Eye,
  TrendingUp,
  Loader2,
  RefreshCw,
  MessageSquare,
  HeadphonesIcon,
} from 'lucide-react';
import Link from 'next/link';
import { toast } from 'sonner';
import {
  useAdminDealerDetail,
  useDealerVehiclesAdmin,
  useDealerDocumentsAdmin,
  useDealerBillingAdmin,
} from '@/hooks/use-admin-extended';
import { adminChangeDealerPlan } from '@/services/admin';

export default function DealerDetailPage() {
  const params = useParams();
  const dealerId = params?.id as string;
  const [activeTab, setActiveTab] = useState('overview');
  const [changePlanOpen, setChangePlanOpen] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState('');
  const [planJustification, setPlanJustification] = useState('');
  const [changePlanLoading, setChangePlanLoading] = useState(false);

  const { data: dealerData, isLoading } = useAdminDealerDetail(dealerId);
  const { data: vehicles = [] } = useDealerVehiclesAdmin(dealerId);
  const { data: documents = [] } = useDealerDocumentsAdmin(dealerId);
  const { data: billingHistory = [] } = useDealerBillingAdmin(dealerId);

  if (isLoading || !dealerData) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/admin/dealers">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Detalle de Dealer</h1>
            <p className="text-muted-foreground">ID: {dealerData.id}</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Dialog open={changePlanOpen} onOpenChange={setChangePlanOpen}>
            <DialogTrigger asChild>
              <Button variant="outline">
                <RefreshCw className="mr-2 h-4 w-4" />
                Cambiar Plan
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Cambiar plan de {dealerData.name}</DialogTitle>
                <DialogDescription>
                  Plan actual: <strong>{dealerData.plan.toUpperCase()}</strong>. Selecciona el nuevo
                  plan e indica el motivo del cambio.
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-2">
                <div className="space-y-2">
                  <Label>Nuevo plan</Label>
                  <Select value={selectedPlan} onValueChange={setSelectedPlan}>
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar plan" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="libre">LIBRE</SelectItem>
                      <SelectItem value="visible">VISIBLE</SelectItem>
                      <SelectItem value="pro">PRO</SelectItem>
                      <SelectItem value="elite">ÉLITE</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Justificación (obligatoria)</Label>
                  <Textarea
                    placeholder="Motivo del cambio de plan..."
                    value={planJustification}
                    onChange={e => setPlanJustification(e.target.value)}
                    className="min-h-[80px]"
                  />
                </div>
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => {
                    setChangePlanOpen(false);
                    setSelectedPlan('');
                    setPlanJustification('');
                  }}
                >
                  Cancelar
                </Button>
                <Button
                  disabled={
                    !selectedPlan ||
                    !planJustification.trim() ||
                    selectedPlan === dealerData.plan ||
                    changePlanLoading
                  }
                  onClick={async () => {
                    setChangePlanLoading(true);
                    try {
                      await adminChangeDealerPlan(dealerId, selectedPlan, planJustification);
                      toast.success(`Plan cambiado a ${selectedPlan.toUpperCase()} exitosamente`);
                      setChangePlanOpen(false);
                      setSelectedPlan('');
                      setPlanJustification('');
                    } catch {
                      toast.error(
                        'Error al cambiar el plan. El endpoint puede no estar disponible aún.'
                      );
                    } finally {
                      setChangePlanLoading(false);
                    }
                  }}
                >
                  {changePlanLoading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                  Confirmar cambio
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
          <Button
            variant="outline"
            className="border-yellow-600 text-yellow-600"
            onClick={() =>
              toast.info(
                'Funcionalidad de advertencia pendiente de integración con NotificationService'
              )
            }
          >
            <AlertTriangle className="mr-2 h-4 w-4" />
            Advertir
          </Button>
          <Button variant="outline" className="border-red-600 text-red-600">
            <Ban className="mr-2 h-4 w-4" />
            Suspender
          </Button>
        </div>
      </div>

      {/* Dealer Card */}
      <Card className="border-slate-700 bg-slate-800">
        <CardContent className="py-6">
          <div className="flex items-start gap-6">
            <Avatar className="h-20 w-20">
              <AvatarImage src={dealerData.avatar || undefined} />
              <AvatarFallback className="bg-primary text-2xl">
                {dealerData.name
                  .split(' ')
                  .map(n => n[0])
                  .join('')}
              </AvatarFallback>
            </Avatar>
            <div className="flex-1">
              <div className="mb-2 flex items-center gap-3">
                <h2 className="text-xl font-bold text-white">{dealerData.name}</h2>
                {dealerData.verified && (
                  <Badge className="bg-primary">
                    <CheckCircle className="mr-1 h-3 w-3" />
                    Verificado
                  </Badge>
                )}
                <Badge className="bg-purple-600">{dealerData.plan}</Badge>
                <Badge
                  className={
                    dealerData.status === 'active'
                      ? 'bg-primary/10 text-primary'
                      : 'bg-red-100 text-red-700'
                  }
                >
                  {dealerData.status === 'active' ? 'Activo' : 'Suspendido'}
                </Badge>
              </div>
              <p className="text-muted-foreground mb-2 text-sm">
                {dealerData.legalName} • RNC: {dealerData.rnc}
              </p>
              <div className="grid grid-cols-2 gap-4 text-sm md:grid-cols-4">
                <div className="text-muted-foreground flex items-center gap-2">
                  <Mail className="h-4 w-4" />
                  <span>{dealerData.email}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Phone className="h-4 w-4" />
                  <span>{dealerData.phone}</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  <span>2 ubicaciones</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  <span>Desde {new Date(dealerData.createdAt).toLocaleDateString('es-DO')}</span>
                </div>
              </div>
            </div>
            <div className="text-right">
              <div className="mb-1 flex items-center gap-1 text-yellow-400">
                <Star className="h-5 w-5 fill-current" />
                <span className="text-xl font-bold">{dealerData.stats.rating}</span>
              </div>
              <p className="text-muted-foreground text-sm">
                {dealerData.stats.reviewCount} reviews
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="bg-primary/95 rounded-lg p-2">
                <Car className="text-primary/80 h-5 w-5" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Vehículos Activos</p>
                <p className="text-2xl font-bold text-white">{dealerData.stats.activeListings}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-900 p-2">
                <Eye className="h-5 w-5 text-blue-400" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Vistas Totales</p>
                <p className="text-2xl font-bold text-white">
                  {dealerData.stats.totalVehicles.toLocaleString()}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-900 p-2">
                <Users className="h-5 w-5 text-purple-400" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Leads Totales</p>
                <p className="text-2xl font-bold text-white">{dealerData.stats.totalSales}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-slate-700 bg-slate-800">
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-orange-900 p-2">
                <TrendingUp className="h-5 w-5 text-orange-400" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Conversión</p>
                <p className="text-2xl font-bold text-white">
                  RD$ {dealerData.stats.monthlyRevenue.toLocaleString()}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="border-slate-700 bg-slate-800">
          <TabsTrigger value="overview">Resumen</TabsTrigger>
          <TabsTrigger value="vehicles">Vehículos</TabsTrigger>
          <TabsTrigger value="documents">Documentos</TabsTrigger>
          <TabsTrigger value="billing">Facturación</TabsTrigger>
          <TabsTrigger value="messages">
            <MessageSquare className="mr-1 h-3.5 w-3.5" />
            ChatAgent
          </TabsTrigger>
          <TabsTrigger value="support">
            <HeadphonesIcon className="mr-1 h-3.5 w-3.5" />
            Soporte
          </TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="mt-6">
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="text-white">Información del Negocio</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex justify-between border-b border-slate-700 py-2">
                  <span className="text-muted-foreground">Nombre Comercial</span>
                  <span className="text-white">{dealerData.name}</span>
                </div>
                <div className="flex justify-between border-b border-slate-700 py-2">
                  <span className="text-muted-foreground">Razón Social</span>
                  <span className="text-white">{dealerData.legalName}</span>
                </div>
                <div className="flex justify-between border-b border-slate-700 py-2">
                  <span className="text-muted-foreground">RNC</span>
                  <span className="text-white">{dealerData.rnc}</span>
                </div>
                <div className="flex justify-between border-b border-slate-700 py-2">
                  <span className="text-muted-foreground">Representante</span>
                  <span className="text-white">{dealerData.contactPerson}</span>
                </div>
                <div className="flex justify-between border-b border-slate-700 py-2">
                  <span className="text-muted-foreground">Ventas Totales</span>
                  <span className="text-white">{dealerData.stats.totalSales}</span>
                </div>
                <div className="flex justify-between py-2">
                  <span className="text-muted-foreground">Listados Activos</span>
                  <span className="text-white">{dealerData.stats.activeListings}</span>
                </div>
              </CardContent>
            </Card>

            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="text-white">Suscripción</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="rounded-lg bg-slate-700/50 p-4">
                  <div className="mb-2 flex items-center justify-between">
                    <span className="font-medium text-white">Plan {dealerData.plan}</span>
                    <Badge className="bg-primary">Activo</Badge>
                  </div>
                  <p className="text-primary/80 text-2xl font-bold">
                    RD$ {dealerData.stats.monthlyRevenue.toLocaleString()}/mes
                  </p>
                </div>
                <div className="flex justify-between border-b border-slate-700 py-2">
                  <span className="text-muted-foreground">Miembro desde</span>
                  <span className="text-white">
                    {new Date(dealerData.createdAt).toLocaleDateString('es-DO')}
                  </span>
                </div>
                <div className="flex justify-between py-2">
                  <span className="text-muted-foreground">Método de pago</span>
                  <span className="text-white">Visa •••• 4242</span>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="vehicles" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-white">Vehículos Publicados</CardTitle>
              <CardDescription>{vehicles.length} vehículos activos</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {vehicles.map(vehicle => (
                  <div
                    key={vehicle.id}
                    className="flex items-center justify-between rounded-lg bg-slate-700/50 p-4"
                  >
                    <div className="flex items-center gap-4">
                      <div className="rounded-lg bg-slate-600 p-3">
                        <Car className="h-5 w-5 text-gray-300" />
                      </div>
                      <div>
                        <p className="font-medium text-white">{vehicle.title}</p>
                        <p className="text-primary/80 text-sm">
                          RD$ {vehicle.price.toLocaleString()}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-6 text-sm">
                      <div className="text-center">
                        <p className="font-medium text-white">{vehicle.views}</p>
                        <p className="text-muted-foreground">vistas</p>
                      </div>
                      <div className="text-center">
                        <p className="font-medium text-white">{vehicle.leads}</p>
                        <p className="text-muted-foreground">leads</p>
                      </div>
                      <Badge className="bg-primary">Activo</Badge>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="documents" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-white">Documentos de Verificación</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {documents.map((doc, idx) => (
                  <div
                    key={idx}
                    className="flex items-center justify-between rounded-lg bg-slate-700/50 p-4"
                  >
                    <div className="flex items-center gap-4">
                      <div className="rounded-lg bg-slate-600 p-3">
                        <FileText className="h-5 w-5 text-gray-300" />
                      </div>
                      <div>
                        <p className="font-medium text-white">{doc.name}</p>
                        <p className="text-muted-foreground text-sm">
                          Subido: {new Date(doc.uploadedAt).toLocaleDateString('es-DO')}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge className="bg-primary">
                        <CheckCircle className="mr-1 h-3 w-3" />
                        Verificado
                      </Badge>
                      <Button variant="outline" size="sm">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="billing" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="text-white">Historial de Facturación</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {billingHistory.map(record => (
                  <div
                    key={record.id}
                    className="flex items-center justify-between rounded-lg bg-slate-700/50 p-4"
                  >
                    <div className="flex items-center gap-4">
                      <div className="rounded-lg bg-slate-600 p-3">
                        <DollarSign className="h-5 w-5 text-gray-300" />
                      </div>
                      <div>
                        <p className="font-medium text-white">
                          {record.description ?? `Plan ${dealerData.plan}`}
                        </p>
                        <p className="text-muted-foreground text-sm">{record.id}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <p className="font-bold text-white">
                        RD$ {(record.amount ?? 0).toLocaleString()}
                      </p>
                      <Badge className="bg-primary">
                        {record.status === 'paid' ? 'Pagada' : record.status}
                      </Badge>
                    </div>
                  </div>
                ))}
                {billingHistory.length === 0 && (
                  <p className="text-muted-foreground py-4 text-center">
                    Sin registros de facturación
                  </p>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="messages" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-white">
                <MessageSquare className="h-5 w-5" />
                Conversaciones del ChatAgent
              </CardTitle>
              <CardDescription>
                Historial de conversaciones del ChatAgent de este dealer en el mes actual
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <MessageSquare className="text-muted-foreground mb-3 h-10 w-10" />
                <p className="text-muted-foreground font-medium">
                  Integración pendiente con ContactService
                </p>
                <p className="text-muted-foreground mt-1 text-sm">
                  Las métricas de conversaciones del ChatAgent estarán disponibles cuando se
                  complete la integración con el endpoint de sesiones por dealer.
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="support" className="mt-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-white">
                <HeadphonesIcon className="h-5 w-5" />
                Tickets de Soporte
              </CardTitle>
              <CardDescription>
                Historial de tickets de soporte abiertos por este dealer
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <HeadphonesIcon className="text-muted-foreground mb-3 h-10 w-10" />
                <p className="text-muted-foreground font-medium">
                  Sistema de tickets no implementado aún
                </p>
                <p className="text-muted-foreground mt-1 text-sm">
                  El módulo de soporte se integrará en una fase posterior del roadmap.
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
