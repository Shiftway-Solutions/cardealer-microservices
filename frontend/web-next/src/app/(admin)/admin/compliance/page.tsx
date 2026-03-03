/**
 * Admin Compliance Page
 *
 * AML, DGII and regulatory compliance management
 */

'use client';

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Shield, AlertTriangle, CheckCircle, FileText, Download, Eye } from 'lucide-react';
import {
  useAmlAlerts,
  useDgiiReports,
  useComplianceStats,
  useSubmitDgiiReport,
} from '@/hooks/use-admin-extended';

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'pending_review':
      return <Badge className="bg-amber-100 text-amber-700">Pendiente Revisión</Badge>;
    case 'under_investigation':
      return <Badge className="bg-blue-100 text-blue-700">En Investigación</Badge>;
    case 'resolved':
      return <Badge className="bg-primary/10 text-primary">Resuelto</Badge>;
    case 'pending':
      return <Badge className="bg-amber-100 text-amber-700">Pendiente</Badge>;
    case 'submitted':
      return <Badge className="bg-primary/10 text-primary">Enviado</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
};

export default function AdminCompliancePage() {
  const { data: amlAlerts = [] } = useAmlAlerts();
  const { data: dgiiReports = [] } = useDgiiReports();
  const { data: stats, isLoading: loadingStats } = useComplianceStats();
  const submitReport = useSubmitDgiiReport();

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold">Compliance</h1>
          <p className="text-muted-foreground">Cumplimiento regulatorio y AML</p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <AlertTriangle className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {loadingStats ? '—' : (stats?.amlAlerts ?? amlAlerts.length)}
                </p>
                <p className="text-muted-foreground text-sm">Alertas AML</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <FileText className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {loadingStats
                    ? '—'
                    : (stats?.pendingReports ??
                      dgiiReports.filter(r => r.status === 'pending').length)}
                </p>
                <p className="text-muted-foreground text-sm">Reportes Pendientes</p>
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
                <p className="text-2xl font-bold">
                  {loadingStats ? '—' : (stats?.complianceRate ?? '—')}
                </p>
                <p className="text-muted-foreground text-sm">Cumplimiento</p>
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
                  {loadingStats ? '—' : (stats?.kycVerifications ?? '—')}
                </p>
                <p className="text-muted-foreground text-sm">Verificaciones KYC</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="aml" className="space-y-6">
        <TabsList>
          <TabsTrigger value="aml">AML / Anti-Lavado</TabsTrigger>
          <TabsTrigger value="dgii">Reportes DGII</TabsTrigger>
          <TabsTrigger value="watchlist">Lista de Vigilancia</TabsTrigger>
        </TabsList>

        {/* AML Tab */}
        <TabsContent value="aml">
          <Card>
            <CardHeader>
              <CardTitle>Alertas AML</CardTitle>
              <CardDescription>Monitoreo de transacciones sospechosas</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {amlAlerts.map(alert => (
                  <div
                    key={alert.id}
                    className="bg-muted/50 flex items-start justify-between rounded-lg p-4"
                  >
                    <div className="flex items-start gap-4">
                      <div
                        className={`rounded-lg p-2 ${
                          alert.status === 'pending_review'
                            ? 'bg-amber-100'
                            : alert.status === 'under_investigation'
                              ? 'bg-blue-100'
                              : 'bg-primary/10'
                        }`}
                      >
                        <AlertTriangle
                          className={`h-5 w-5 ${
                            alert.status === 'pending_review'
                              ? 'text-amber-600'
                              : alert.status === 'under_investigation'
                                ? 'text-blue-600'
                                : 'text-primary'
                          }`}
                        />
                      </div>
                      <div>
                        <div className="mb-1 flex items-center gap-2">
                          <p className="font-medium">{alert.description}</p>
                          {getStatusBadge(alert.status)}
                        </div>
                        <p className="text-muted-foreground text-sm">
                          {alert.amount && `Monto: ${alert.amount}`}
                          {alert.entity && ` • Entidad: ${alert.entity}`}
                          {alert.matchedName && ` • Match: ${alert.matchedName}`}
                          {alert.matchScore && ` • Score: ${alert.matchScore}`}
                        </p>
                        <p className="text-muted-foreground mt-1 text-xs">
                          {new Date(alert.createdAt).toLocaleString('es-DO')}
                        </p>
                      </div>
                    </div>
                    <Button variant="outline" size="sm">
                      <Eye className="mr-1 h-4 w-4" />
                      Revisar
                    </Button>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* DGII Tab */}
        <TabsContent value="dgii">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Reportes DGII</CardTitle>
                  <CardDescription>
                    Formatos 606, 607, 608 y otros reportes fiscales
                  </CardDescription>
                </div>
                <Button className="bg-primary hover:bg-primary/90">
                  <FileText className="mr-2 h-4 w-4" />
                  Generar Reporte
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {dgiiReports.map(report => (
                  <div
                    key={report.id}
                    className="bg-muted/50 flex items-center justify-between rounded-lg p-4"
                  >
                    <div className="flex items-center gap-4">
                      <div className="rounded-lg bg-blue-100 p-2">
                        <FileText className="h-5 w-5 text-blue-600" />
                      </div>
                      <div>
                        <div className="mb-1 flex items-center gap-2">
                          <p className="font-medium">Reporte {report.reportNumber}</p>
                          {getStatusBadge(report.status)}
                        </div>
                        <p className="text-muted-foreground text-sm">
                          Período: {report.period} • {report.transactionCount} registros
                        </p>
                        {report.dueDate && (
                          <p className="mt-1 text-xs text-amber-600">
                            Vence: {new Date(report.dueDate).toLocaleDateString('es-DO')}
                          </p>
                        )}
                        {report.submittedAt && (
                          <p className="text-primary mt-1 text-xs">
                            Enviado: {new Date(report.submittedAt).toLocaleDateString('es-DO')}
                          </p>
                        )}
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Button variant="outline" size="sm">
                        <Download className="h-4 w-4" />
                      </Button>
                      {report.status === 'pending' && (
                        <Button
                          size="sm"
                          className="bg-primary hover:bg-primary/90"
                          disabled={submitReport.isPending}
                          onClick={() => submitReport.mutate(report.id)}
                        >
                          {submitReport.isPending ? 'Enviando...' : 'Enviar'}
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Watchlist Tab */}
        <TabsContent value="watchlist">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Lista de Vigilancia</CardTitle>
                  <CardDescription>Usuarios y entidades bajo monitoreo</CardDescription>
                </div>
                <Button variant="outline">
                  <Download className="mr-2 h-4 w-4" />
                  Actualizar Lista
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-muted-foreground py-8 text-center">
                <Shield className="mx-auto mb-4 h-12 w-12 opacity-50" />
                <p>No hay entradas en la lista de vigilancia activa</p>
                <p className="mt-2 text-sm">
                  Última actualización: {new Date().toLocaleDateString('es-DO')}
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
