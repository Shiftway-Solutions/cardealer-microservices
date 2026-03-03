/**
 * Dealer CSV Import Page
 *
 * Bulk import vehicles from CSV files — connected to VehiclesSaleService ImportController
 */

'use client';

import { useCallback, useRef, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  ArrowLeft,
  Upload,
  FileSpreadsheet,
  Download,
  CheckCircle,
  AlertCircle,
  XCircle,
  FileText,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';
import { toast } from 'sonner';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useImportVehicles,
  useImportHistory,
  useDownloadImportTemplate,
} from '@/hooks/use-dealer-analytics';
import { PlanGate } from '@/components/plan/plan-gate';

function DealerImportContent() {
  const [dragActive, setDragActive] = useState(false);
  const [uploadedFile, setUploadedFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  const importMutation = useImportVehicles(dealerId);
  const templateMutation = useDownloadImportTemplate();
  const { data: importHistory, isLoading: isHistoryLoading } = useImportHistory(dealerId);

  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    const file = e.dataTransfer.files?.[0];
    if (file && (file.name.endsWith('.csv') || file.name.endsWith('.xlsx'))) {
      setUploadedFile(file);
    } else {
      toast.error('Solo se aceptan archivos CSV o XLSX');
    }
  }, []);

  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setUploadedFile(file);
    }
  }, []);

  const handleImport = async () => {
    if (!uploadedFile || !dealerId) return;

    try {
      const result = await importMutation.mutateAsync(uploadedFile);
      toast.success(
        `Importación completada: ${result.successful} exitosos, ${result.failed} fallidos`
      );
      setUploadedFile(null);
    } catch {
      toast.error('Error al importar el archivo');
    }
  };

  const handleDownloadTemplate = async () => {
    try {
      await templateMutation.mutateAsync('csv');
      toast.success('Plantilla descargada');
    } catch {
      toast.error('Error al descargar la plantilla');
    }
  };

  const historyItems = Array.isArray(importHistory) ? importHistory : [];

  if (isDealerLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-48 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link href="/dealer/inventario">
          <Button variant="ghost" size="icon">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        </Link>
        <div>
          <h1 className="text-2xl font-bold">Importar Vehículos</h1>
          <p className="text-muted-foreground">Carga masiva desde CSV</p>
        </div>
      </div>

      {/* Instructions */}
      <Card className="border-primary bg-primary/10">
        <CardContent className="p-6">
          <div className="flex items-start gap-4">
            <FileSpreadsheet className="text-primary h-8 w-8 flex-shrink-0" />
            <div>
              <h3 className="text-primary mb-2 font-semibold">¿Primera vez importando?</h3>
              <p className="text-primary mb-3 text-sm">
                Descarga nuestra plantilla para asegurarte de que tu archivo tenga el formato
                correcto. Incluye todas las columnas requeridas y ejemplos de datos.
              </p>
              <Button
                variant="outline"
                size="sm"
                className="border-primary"
                onClick={handleDownloadTemplate}
                disabled={templateMutation.isPending}
              >
                {templateMutation.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Download className="mr-2 h-4 w-4" />
                )}
                Descargar Plantilla CSV
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Upload Area */}
      <Card>
        <CardHeader>
          <CardTitle>Subir Archivo</CardTitle>
          <CardDescription>
            Formato soportado: CSV. Máximo 1000 registros por archivo.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv,.xlsx"
            className="hidden"
            onChange={handleFileSelect}
          />
          <div
            className={`rounded-lg border-2 border-dashed p-8 text-center transition-colors ${
              dragActive ? 'border-primary bg-primary/10' : 'border-border hover:border-border'
            }`}
            onDragEnter={handleDrag}
            onDragLeave={handleDrag}
            onDragOver={handleDrag}
            onDrop={handleDrop}
          >
            <Upload
              className={`mx-auto mb-4 h-12 w-12 ${dragActive ? 'text-primary' : 'text-muted-foreground'}`}
            />

            {uploadedFile ? (
              <div className="space-y-4">
                <div className="bg-muted inline-flex items-center gap-2 rounded-lg px-4 py-2">
                  <FileText className="text-muted-foreground h-5 w-5" />
                  <span className="font-medium">{uploadedFile.name}</span>
                  <span className="text-muted-foreground text-sm">
                    ({(uploadedFile.size / 1024).toFixed(1)} KB)
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 w-6 p-0"
                    onClick={() => setUploadedFile(null)}
                  >
                    <XCircle className="h-4 w-4" />
                  </Button>
                </div>

                {/* Show import result if available */}
                {importMutation.data && (
                  <div className="bg-muted mx-auto max-w-md rounded-lg p-4 text-left">
                    <p className="mb-2 font-medium">Resultado de importación:</p>
                    <div className="flex gap-4 text-sm">
                      <span className="text-primary">
                        ✓ {importMutation.data.successful} exitosos
                      </span>
                      {importMutation.data.failed > 0 && (
                        <span className="text-red-600">
                          ✗ {importMutation.data.failed} fallidos
                        </span>
                      )}
                    </div>
                    {importMutation.data.errors && importMutation.data.errors.length > 0 && (
                      <div className="mt-2 max-h-32 overflow-y-auto text-sm text-red-600">
                        {importMutation.data.errors.slice(0, 5).map((err, i) => (
                          <p key={i}>
                            Fila {err.row}: {err.message}
                          </p>
                        ))}
                      </div>
                    )}
                  </div>
                )}

                <Button
                  className="bg-primary hover:bg-primary/90"
                  disabled={importMutation.isPending}
                  onClick={handleImport}
                >
                  {importMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Importando...
                    </>
                  ) : (
                    'Iniciar Importación'
                  )}
                </Button>
              </div>
            ) : (
              <>
                <p className="text-muted-foreground mb-2">
                  Arrastra tu archivo aquí o haz clic para seleccionar
                </p>
                <p className="text-muted-foreground mb-4 text-sm">CSV (.csv)</p>
                <Button variant="outline" onClick={() => fileInputRef.current?.click()}>
                  <Upload className="mr-2 h-4 w-4" />
                  Seleccionar Archivo
                </Button>
              </>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Required Fields */}
      <Card>
        <CardHeader>
          <CardTitle>Campos Requeridos</CardTitle>
          <CardDescription>Tu archivo debe incluir estas columnas</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            {[
              'marca',
              'modelo',
              'año',
              'precio',
              'kilometraje',
              'transmision',
              'combustible',
              'color',
            ].map(field => (
              <div key={field} className="bg-muted/50 flex items-center gap-2 rounded-lg p-3">
                <CheckCircle className="text-primary h-4 w-4" />
                <span className="text-sm font-medium capitalize">{field}</span>
              </div>
            ))}
          </div>
          <div className="mt-4">
            <p className="text-muted-foreground text-sm">
              Campos opcionales: version, puertas, cilindros, traccion, descripcion, vin
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Import History */}
      <Card>
        <CardHeader>
          <CardTitle>Historial de Importaciones</CardTitle>
        </CardHeader>
        <CardContent>
          {isHistoryLoading ? (
            <div className="space-y-4">
              {[1, 2].map(i => (
                <Skeleton key={i} className="h-20 w-full" />
              ))}
            </div>
          ) : historyItems.length > 0 ? (
            <div className="space-y-4">
              {historyItems.map((item, idx) => (
                <div
                  key={item.id || idx}
                  className="bg-muted/50 flex items-center justify-between rounded-lg p-4"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-muted rounded-lg p-2">
                      <FileSpreadsheet className="text-muted-foreground h-5 w-5" />
                    </div>
                    <div>
                      <p className="font-medium">{item.filename || 'Importación'}</p>
                      <p className="text-muted-foreground text-sm">
                        {item.date ? new Date(item.date).toLocaleDateString('es-DO') : ''} •{' '}
                        {item.totalRecords || 0} registros
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <div className="flex items-center gap-2">
                        <CheckCircle className="text-primary h-4 w-4" />
                        <span className="text-primary text-sm">
                          {item.successful || 0} exitosos
                        </span>
                      </div>
                      {(item.failed || 0) > 0 && (
                        <div className="flex items-center gap-2">
                          <AlertCircle className="h-4 w-4 text-red-500" />
                          <span className="text-sm text-red-600">{item.failed} fallidos</span>
                        </div>
                      )}
                    </div>
                    <Badge className="bg-primary/10 text-primary">Completado</Badge>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-muted-foreground py-8 text-center">
              <FileSpreadsheet className="mx-auto mb-4 h-12 w-12 opacity-50" />
              <p>No hay importaciones anteriores</p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default function DealerImportPage() {
  return (
    <PlanGate feature="bulkUpload">
      <DealerImportContent />
    </PlanGate>
  );
}
