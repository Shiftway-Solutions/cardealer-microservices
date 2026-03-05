# 📄 Documentos del Dealer

> **Tiempo estimado:** 25 minutos  
> **Páginas:** DealerDocumentsPage

---

## 📋 OBJETIVO

Gestión de documentos legales del dealer:

- Upload de documentos requeridos
- Estado de verificación
- Historial de documentos

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ DOCUMENTOS                                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ DOCUMENTOS REQUERIDOS                                           │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📄 RNC                           ✓ Verificado               │ │
│ │    Registro Nacional del Contribuyente                       │ │
│ │    Subido: 15 Ene 2024           [Ver] [Reemplazar]         │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📄 Licencia Comercial            ⏳ En revisión             │ │
│ │    Patente municipal                                         │ │
│ │    Subido: 20 Ene 2024           [Ver]                      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📄 Cédula del Representante      ❌ Pendiente               │ │
│ │    Documento de identidad                                    │ │
│ │                                  [Subir Documento]           │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(dealer)/dealer/documents/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { dealerService, type DealerDocument } from '@/services/api/dealerService';
import { mediaService } from '@/services/api/mediaService';
import { useToast } from '@/hooks/use-toast';
import { format } from 'date-fns';
import { es } from 'date-fns/locale';
import {
  FileText, Upload, Eye, RefreshCw, CheckCircle,
  Clock, XCircle, AlertTriangle, Loader2
} from 'lucide-react';

const requiredDocs = [
  { type: 'rnc', name: 'RNC', description: 'Registro Nacional del Contribuyente' },
  { type: 'business_license', name: 'Licencia Comercial', description: 'Patente municipal o licencia de operación' },
  { type: 'representative_id', name: 'Cédula del Representante', description: 'Documento de identidad del representante legal' },
  { type: 'address_proof', name: 'Comprobante de Domicilio', description: 'Factura de servicios o contrato de arrendamiento' },
];

const statusConfig = {
  verified: { icon: CheckCircle, color: 'text-green-500', label: 'Verificado', badge: 'default' },
  pending: { icon: Clock, color: 'text-yellow-500', label: 'En revisión', badge: 'secondary' },
  rejected: { icon: XCircle, color: 'text-red-500', label: 'Rechazado', badge: 'destructive' },
  missing: { icon: AlertTriangle, color: 'text-gray-400', label: 'Pendiente', badge: 'outline' },
};

export default function DealerDocumentsPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [uploadingType, setUploadingType] = useState<string | null>(null);
  const [previewDoc, setPreviewDoc] = useState<DealerDocument | null>(null);

  const { data: documents, isLoading } = useQuery({
    queryKey: ['dealer-documents'],
    queryFn: () => dealerService.getDocuments(),
  });

  const uploadMutation = useMutation({
    mutationFn: async ({ type, file }: { type: string; file: File }) => {
      const uploadResult = await mediaService.uploadDocument(file, 'dealer-documents');
      return dealerService.submitDocument(type, uploadResult.url, uploadResult.fileName);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-documents'] });
      setUploadingType(null);
      toast({ title: 'Documento subido exitosamente' });
    },
    onError: () => {
      toast({ title: 'Error al subir documento', variant: 'destructive' });
    },
  });

  const handleFileSelect = (type: string, e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setUploadingType(type);
      uploadMutation.mutate({ type, file });
    }
  };

  const getDocStatus = (type: string) => {
    const doc = documents?.find((d: DealerDocument) => d.type === type);
    if (!doc) return { status: 'missing', doc: null };
    return { status: doc.status, doc };
  };

  const verifiedCount = documents?.filter((d: DealerDocument) => d.status === 'verified').length || 0;
  const progress = (verifiedCount / requiredDocs.length) * 100;

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Documentos</h1>
        <p className="text-gray-600">Sube los documentos requeridos para verificar tu dealer</p>
      </div>

      {/* Progress */}
      <Card className="mb-6">
        <CardContent className="py-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium">Progreso de verificación</span>
            <span className="text-sm text-gray-600">{verifiedCount} de {requiredDocs.length}</span>
          </div>
          <Progress value={progress} className="h-2" />
        </CardContent>
      </Card>

      {/* Documents List */}
      <div className="space-y-4">
        {requiredDocs.map((reqDoc) => {
          const { status, doc } = getDocStatus(reqDoc.type);
          const config = statusConfig[status as keyof typeof statusConfig];
          const StatusIcon = config.icon;

          return (
            <Card key={reqDoc.type}>
              <CardContent className="p-4">
                <div className="flex items-start gap-4">
                  <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center">
                    <FileText className="w-5 h-5 text-gray-600" />
                  </div>

                  <div className="flex-grow">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium">{reqDoc.name}</h3>
                      <Badge variant={config.badge as any}>
                        <StatusIcon className={`w-3 h-3 mr-1 ${config.color}`} />
                        {config.label}
                      </Badge>
                    </div>
                    <p className="text-sm text-gray-600">{reqDoc.description}</p>

                    {doc && (
                      <p className="text-xs text-gray-500 mt-1">
                        Subido: {format(new Date(doc.uploadedAt), 'dd MMM yyyy', { locale: es })}
                      </p>
                    )}

                    {status === 'rejected' && doc?.rejectionReason && (
                      <p className="text-sm text-red-600 mt-2">
                        Motivo: {doc.rejectionReason}
                      </p>
                    )}
                  </div>

                  <div className="flex gap-2">
                    {doc && (
                      <Button variant="outline" size="sm" onClick={() => setPreviewDoc(doc)}>
                        <Eye className="w-4 h-4 mr-1" />
                        Ver
                      </Button>
                    )}

                    {(status === 'missing' || status === 'rejected') && (
                      <div>
                        <input
                          type="file"
                          accept=".pdf,.jpg,.jpeg,.png"
                          onChange={(e) => handleFileSelect(reqDoc.type, e)}
                          className="hidden"
                          id={`upload-${reqDoc.type}`}
                        />
                        <Button size="sm" asChild disabled={uploadingType === reqDoc.type}>
                          <label htmlFor={`upload-${reqDoc.type}`} className="cursor-pointer">
                            {uploadingType === reqDoc.type ? (
                              <Loader2 className="w-4 h-4 animate-spin" />
                            ) : (
                              <>
                                <Upload className="w-4 h-4 mr-1" />
                                Subir
                              </>
                            )}
                          </label>
                        </Button>
                      </div>
                    )}

                    {status === 'verified' && (
                      <div>
                        <input
                          type="file"
                          accept=".pdf,.jpg,.jpeg,.png"
                          onChange={(e) => handleFileSelect(reqDoc.type, e)}
                          className="hidden"
                          id={`replace-${reqDoc.type}`}
                        />
                        <Button variant="ghost" size="sm" asChild>
                          <label htmlFor={`replace-${reqDoc.type}`} className="cursor-pointer">
                            <RefreshCw className="w-4 h-4" />
                          </label>
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Preview Dialog */}
      <Dialog open={!!previewDoc} onOpenChange={() => setPreviewDoc(null)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>{previewDoc?.fileName}</DialogTitle>
          </DialogHeader>
          <div className="h-[500px] bg-gray-100 rounded-lg">
            {previewDoc?.url.endsWith('.pdf') ? (
              <iframe src={previewDoc.url} className="w-full h-full" />
            ) : (
              <img src={previewDoc?.url} alt="" className="w-full h-full object-contain" />
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                      | Descripción         |
| -------- | ----------------------------- | ------------------- |
| `GET`    | `/api/dealers/documents`      | Lista de documentos |
| `POST`   | `/api/dealers/documents`      | Subir documento     |
| `DELETE` | `/api/dealers/documents/{id}` | Eliminar documento  |

---

## ✅ CHECKLIST

- [ ] Lista de documentos requeridos
- [ ] Upload con preview
- [ ] Estados de verificación
- [ ] Progreso de verificación
- [ ] Razones de rechazo

---

_Última actualización: Enero 30, 2026_
