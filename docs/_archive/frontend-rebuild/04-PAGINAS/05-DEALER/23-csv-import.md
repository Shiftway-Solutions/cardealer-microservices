# 📤 Importación CSV de Vehículos

> **Tiempo estimado:** 25 minutos  
> **Página:** DealerCsvImportPage

---

## 📋 OBJETIVO

Importación masiva de vehículos desde archivo CSV/Excel:

- Upload de archivo
- Mapeo de columnas
- Validación y preview
- Importación con progreso

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ IMPORTAR VEHÍCULOS                           [Descargar Template]│
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ PASO 1: Subir Archivo                                           │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │                                                             │ │
│ │        📄 Arrastra tu archivo CSV o Excel aquí              │ │
│ │              o [Seleccionar archivo]                        │ │
│ │                                                             │ │
│ │           Formatos: .csv, .xlsx (máx 10MB)                  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ─────────────────────────────────────────────────────────────── │
│                                                                 │
│ PASO 2: Mapear Columnas                                         │
│ ┌────────────────────────┬────────────────────────────────────┐ │
│ │ Columna en archivo     │ Campo en sistema                   │ │
│ ├────────────────────────┼────────────────────────────────────┤ │
│ │ marca                  │ [Marca ▼]                          │ │
│ │ modelo                 │ [Modelo ▼]                         │ │
│ │ año                    │ [Año ▼]                            │ │
│ │ precio                 │ [Precio ▼]                         │ │
│ │ km                     │ [Kilometraje ▼]                    │ │
│ └────────────────────────┴────────────────────────────────────┘ │
│                                                                 │
│ PASO 3: Vista Previa                                            │
│ ┌───┬────────────┬──────────┬──────┬────────────┬─────────────┐ │
│ │   │ Marca      │ Modelo   │ Año  │ Precio     │ Estado      │ │
│ │ ✓ │ Toyota     │ Camry    │ 2024 │ $1,850,000 │ ✅ Válido   │ │
│ │ ✓ │ Honda      │ CR-V     │ 2023 │ $2,100,000 │ ✅ Válido   │ │
│ │ ✗ │ Hyundai    │          │ 2024 │ $1,500,000 │ ⚠️ Sin modelo│ │
│ └───┴────────────┴──────────┴──────┴────────────┴─────────────┘ │
│                                                                 │
│ ✅ 48 válidos  ⚠️ 2 con errores                                 │
│                                                                 │
│                              [Cancelar]  [Importar 48 vehículos]│
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(dealer)/dealer/vehicles/import/page.tsx
'use client';

import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { useMutation } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { dealerService } from '@/services/api/dealerService';
import { Upload, FileSpreadsheet, Download, CheckCircle, AlertCircle } from 'lucide-react';
import { toast } from 'sonner';

type Step = 'upload' | 'mapping' | 'preview' | 'importing' | 'complete';

const systemFields = [
  { value: 'make', label: 'Marca' },
  { value: 'model', label: 'Modelo' },
  { value: 'year', label: 'Año' },
  { value: 'price', label: 'Precio' },
  { value: 'mileage', label: 'Kilometraje' },
  { value: 'transmission', label: 'Transmisión' },
  { value: 'fuelType', label: 'Combustible' },
  { value: 'color', label: 'Color' },
  { value: 'vin', label: 'VIN' },
  { value: 'description', label: 'Descripción' },
  { value: 'ignore', label: '-- Ignorar --' },
];

export default function DealerCsvImportPage() {
  const [step, setStep] = useState<Step>('upload');
  const [file, setFile] = useState<File | null>(null);
  const [columns, setColumns] = useState<string[]>([]);
  const [mapping, setMapping] = useState<Record<string, string>>({});
  const [preview, setPreview] = useState<any[]>([]);
  const [progress, setProgress] = useState(0);

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    const f = acceptedFiles[0];
    if (!f) return;

    setFile(f);

    // Parse headers
    const formData = new FormData();
    formData.append('file', f);

    const result = await dealerService.parseImportFile(formData);
    setColumns(result.columns);
    setMapping(result.suggestedMapping);
    setStep('mapping');
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'text/csv': ['.csv'],
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
    },
    maxSize: 10 * 1024 * 1024, // 10MB
    multiple: false,
  });

  const validateMutation = useMutation({
    mutationFn: () => {
      const formData = new FormData();
      formData.append('file', file!);
      formData.append('mapping', JSON.stringify(mapping));
      return dealerService.validateImport(formData);
    },
    onSuccess: (data) => {
      setPreview(data.rows);
      setStep('preview');
    },
  });

  const importMutation = useMutation({
    mutationFn: () => {
      const formData = new FormData();
      formData.append('file', file!);
      formData.append('mapping', JSON.stringify(mapping));
      return dealerService.importVehicles(formData, (p) => setProgress(p));
    },
    onSuccess: (data) => {
      setStep('complete');
      toast.success(`${data.imported} vehículos importados`);
    },
  });

  const validCount = preview.filter(r => r.isValid).length;
  const errorCount = preview.filter(r => !r.isValid).length;

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Importar Vehículos</h1>
        <Button variant="outline" asChild>
          <a href="/templates/vehicles-template.xlsx" download>
            <Download className="w-4 h-4 mr-2" />
            Descargar Template
          </a>
        </Button>
      </div>

      {/* Progress Steps */}
      <div className="flex items-center mb-8">
        {['Subir', 'Mapear', 'Revisar', 'Importar'].map((s, i) => (
          <div key={s} className="flex items-center">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium ${
              i <= ['upload', 'mapping', 'preview', 'importing'].indexOf(step)
                ? 'bg-primary-600 text-white'
                : 'bg-gray-200 text-gray-600'
            }`}>
              {i + 1}
            </div>
            <span className="ml-2 text-sm hidden sm:inline">{s}</span>
            {i < 3 && <div className="w-12 h-0.5 bg-gray-200 mx-2" />}
          </div>
        ))}
      </div>

      {/* Step: Upload */}
      {step === 'upload' && (
        <Card>
          <CardContent className="pt-6">
            <div
              {...getRootProps()}
              className={`border-2 border-dashed rounded-lg p-12 text-center cursor-pointer transition-colors ${
                isDragActive ? 'border-primary-500 bg-primary-50' : 'border-gray-300 hover:border-gray-400'
              }`}
            >
              <input {...getInputProps()} />
              <FileSpreadsheet className="w-12 h-12 mx-auto text-gray-400 mb-4" />
              <p className="text-lg font-medium mb-2">
                {isDragActive ? 'Suelta el archivo aquí' : 'Arrastra tu archivo CSV o Excel'}
              </p>
              <p className="text-gray-600 mb-4">o</p>
              <Button type="button">Seleccionar archivo</Button>
              <p className="text-sm text-gray-500 mt-4">
                Formatos: .csv, .xlsx (máximo 10MB)
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step: Mapping */}
      {step === 'mapping' && (
        <Card>
          <CardHeader>
            <CardTitle>Mapear Columnas</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {columns.map((col) => (
                <div key={col} className="flex items-center gap-4">
                  <div className="w-1/3 font-mono text-sm bg-gray-100 px-3 py-2 rounded">
                    {col}
                  </div>
                  <span className="text-gray-400">→</span>
                  <Select
                    value={mapping[col] || 'ignore'}
                    onValueChange={(v) => setMapping({ ...mapping, [col]: v })}
                  >
                    <SelectTrigger className="w-1/2">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {systemFields.map((f) => (
                        <SelectItem key={f.value} value={f.value}>
                          {f.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ))}
            </div>
            <div className="flex justify-end gap-2 mt-6">
              <Button variant="outline" onClick={() => setStep('upload')}>
                Atrás
              </Button>
              <Button onClick={() => validateMutation.mutate()}>
                Validar y Continuar
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step: Preview */}
      {step === 'preview' && (
        <Card>
          <CardHeader>
            <CardTitle>Vista Previa</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="mb-4 flex gap-4">
              <Badge variant="success">✅ {validCount} válidos</Badge>
              {errorCount > 0 && (
                <Badge variant="destructive">⚠️ {errorCount} con errores</Badge>
              )}
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2">Marca</th>
                    <th className="text-left py-2">Modelo</th>
                    <th className="text-left py-2">Año</th>
                    <th className="text-right py-2">Precio</th>
                    <th className="text-center py-2">Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {preview.slice(0, 10).map((row, i) => (
                    <tr key={i} className="border-b">
                      <td className="py-2">{row.make}</td>
                      <td className="py-2">{row.model}</td>
                      <td className="py-2">{row.year}</td>
                      <td className="py-2 text-right">${row.price?.toLocaleString()}</td>
                      <td className="py-2 text-center">
                        {row.isValid ? (
                          <CheckCircle className="w-4 h-4 text-green-600 inline" />
                        ) : (
                          <span className="text-red-600 text-xs">{row.error}</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex justify-end gap-2 mt-6">
              <Button variant="outline" onClick={() => setStep('mapping')}>
                Atrás
              </Button>
              <Button onClick={() => { setStep('importing'); importMutation.mutate(); }}>
                Importar {validCount} vehículos
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step: Importing */}
      {step === 'importing' && (
        <Card>
          <CardContent className="pt-6 text-center">
            <Upload className="w-12 h-12 mx-auto text-primary-600 mb-4 animate-pulse" />
            <p className="text-lg font-medium mb-4">Importando vehículos...</p>
            <Progress value={progress} className="max-w-md mx-auto" />
            <p className="text-sm text-gray-600 mt-2">{progress}% completado</p>
          </CardContent>
        </Card>
      )}

      {/* Step: Complete */}
      {step === 'complete' && (
        <Card>
          <CardContent className="pt-6 text-center">
            <CheckCircle className="w-16 h-16 mx-auto text-green-600 mb-4" />
            <p className="text-xl font-bold mb-2">¡Importación Completa!</p>
            <p className="text-gray-600 mb-6">Se importaron {validCount} vehículos</p>
            <Button asChild>
              <a href="/dealer/vehicles">Ver Inventario</a>
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                                | Descripción                        |
| ------ | --------------------------------------- | ---------------------------------- |
| `POST` | `/api/dealers/vehicles/parse`           | Parsear archivo y obtener columnas |
| `POST` | `/api/dealers/vehicles/validate-import` | Validar datos mapeados             |
| `POST` | `/api/dealers/vehicles/import`          | Ejecutar importación               |
| `GET`  | `/api/dealers/vehicles/import-template` | Descargar template                 |

---

## ✅ CHECKLIST

- [ ] Dropzone para upload
- [ ] Soporte CSV y Excel
- [ ] Mapeo de columnas automático
- [ ] Validación con preview
- [ ] Barra de progreso
- [ ] Manejo de errores por fila
- [ ] Template descargable

---

_Última actualización: Enero 31, 2026_
