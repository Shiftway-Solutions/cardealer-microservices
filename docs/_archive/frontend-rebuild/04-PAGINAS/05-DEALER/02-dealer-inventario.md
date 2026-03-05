---
title: "Dealer - Inventario"
priority: P1
estimated_time: "50 minutos"
dependencies: []
apis: ["VehiclesSaleService"]
status: complete
last_updated: "2026-01-30"
---

# 📦 Dealer - Inventario

> **Tiempo estimado:** 50 minutos
> **Prerrequisitos:** Dashboard dealer, VehiclesSaleService
> **Roles:** DLR-STAFF, DLR-ADMIN

---

## 📋 OBJETIVO

Implementar gestión de inventario para dealers:

- Lista de vehículos con filtros
- Publicar nuevo vehículo
- Edición rápida inline
- Import masivo CSV/Excel
- Acciones en lote

---

## 🔧 PASO 1: Página de Inventario

```typescript
// filepath: src/app/(main)/dealer/inventario/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { InventoryHeader } from "@/components/dealer/inventory/InventoryHeader";
import { InventoryTable } from "@/components/dealer/inventory/InventoryTable";
import { InventoryFilters } from "@/components/dealer/inventory/InventoryFilters";
import { InventoryTableSkeleton } from "@/components/dealer/inventory/InventoryTableSkeleton";

export const metadata: Metadata = {
  title: "Inventario | Dealer Dashboard",
};

interface Props {
  searchParams: Promise<{
    status?: string;
    search?: string;
    page?: string;
  }>;
}

export default async function InventoryPage({ searchParams }: Props) {
  const params = await searchParams;

  return (
    <div className="space-y-6">
      <InventoryHeader />
      <InventoryFilters />
      <Suspense fallback={<InventoryTableSkeleton />}>
        <InventoryTable
          status={params.status}
          search={params.search}
          page={Number(params.page) || 1}
        />
      </Suspense>
    </div>
  );
}
```

---

## 🔧 PASO 2: InventoryHeader

```typescript
// filepath: src/components/dealer/inventory/InventoryHeader.tsx
"use client";

import Link from "next/link";
import { Plus, Upload, Download } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { useDealer } from "@/lib/hooks/useDealer";

export function InventoryHeader() {
  const { dealer } = useDealer();
  const canPublish = (dealer?.activeListingsCount ?? 0) < (dealer?.maxActiveListings ?? 0);

  return (
    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Inventario</h1>
        <p className="text-gray-600">
          {dealer?.activeListingsCount} / {dealer?.maxActiveListings} vehículos activos
        </p>
      </div>

      <div className="flex items-center gap-3">
        <Button variant="outline" size="sm" asChild>
          <Link href="/dealer/inventario/import">
            <Upload size={16} className="mr-2" />
            Importar
          </Link>
        </Button>

        <Button variant="outline" size="sm">
          <Download size={16} className="mr-2" />
          Exportar
        </Button>

        <Button asChild disabled={!canPublish}>
          <Link href="/dealer/inventario/nuevo">
            <Plus size={16} className="mr-2" />
            Nuevo vehículo
          </Link>
        </Button>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: InventoryTable

```typescript
// filepath: src/components/dealer/inventory/InventoryTable.tsx
"use client";

import { useState } from "react";
import Link from "next/link";
import { MoreHorizontal, Eye, Edit, Pause, Trash2, BarChart2 } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Checkbox } from "@/components/ui/Checkbox";
import { DropdownMenu } from "@/components/ui/DropdownMenu";
import { useDealerInventory } from "@/lib/hooks/useDealerInventory";
import { formatPrice, formatDate } from "@/lib/utils";
import { BulkActions } from "./BulkActions";
import type { Vehicle } from "@/types";

interface Props {
  status?: string;
  search?: string;
  page: number;
}

export function InventoryTable({ status, search, page }: Props) {
  const [selected, setSelected] = useState<string[]>([]);
  const { data, isLoading } = useDealerInventory({ status, search, page });

  const vehicles = data?.items ?? [];
  const allSelected = vehicles.length > 0 && selected.length === vehicles.length;

  const toggleAll = () => {
    setSelected(allSelected ? [] : vehicles.map((v) => v.id));
  };

  const toggleOne = (id: string) => {
    setSelected((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    );
  };

  return (
    <div className="bg-white rounded-xl border">
      {/* Bulk Actions */}
      {selected.length > 0 && (
        <BulkActions
          selectedIds={selected}
          onClear={() => setSelected([])}
        />
      )}

      {/* Table */}
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="p-4 w-12">
                <Checkbox checked={allSelected} onCheckedChange={toggleAll} />
              </th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">
                Vehículo
              </th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">
                Precio
              </th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">
                Estado
              </th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">
                Vistas
              </th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">
                Leads
              </th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">
                Publicado
              </th>
              <th className="p-4 w-12"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {vehicles.map((vehicle) => (
              <VehicleRow
                key={vehicle.id}
                vehicle={vehicle}
                selected={selected.includes(vehicle.id)}
                onToggle={() => toggleOne(vehicle.id)}
              />
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {data && (
        <div className="p-4 border-t flex items-center justify-between">
          <p className="text-sm text-gray-500">
            {data.totalCount} vehículos en total
          </p>
          {/* Pagination component */}
        </div>
      )}
    </div>
  );
}

function VehicleRow({
  vehicle,
  selected,
  onToggle,
}: {
  vehicle: Vehicle;
  selected: boolean;
  onToggle: () => void;
}) {
  const statusColors = {
    Active: "success",
    Draft: "default",
    Paused: "warning",
    Sold: "info",
  } as const;

  return (
    <tr className="hover:bg-gray-50">
      <td className="p-4">
        <Checkbox checked={selected} onCheckedChange={onToggle} />
      </td>
      <td className="p-4">
        <div className="flex items-center gap-3">
          <img
            src={vehicle.primaryImage || "/placeholder-car.jpg"}
            alt={vehicle.title}
            className="w-16 h-12 rounded-lg object-cover"
          />
          <div>
            <Link
              href={`/dealer/inventario/${vehicle.id}`}
              className="font-medium text-gray-900 hover:text-primary-600"
            >
              {vehicle.year} {vehicle.make} {vehicle.model}
            </Link>
            <p className="text-sm text-gray-500">{vehicle.trim}</p>
          </div>
        </div>
      </td>
      <td className="p-4 font-medium">{formatPrice(vehicle.price)}</td>
      <td className="p-4">
        <Badge variant={statusColors[vehicle.status as keyof typeof statusColors]}>
          {vehicle.status}
        </Badge>
      </td>
      <td className="p-4 text-gray-600">{vehicle.viewCount}</td>
      <td className="p-4 text-gray-600">{vehicle.leadCount}</td>
      <td className="p-4 text-sm text-gray-500">
        {formatDate(vehicle.createdAt)}
      </td>
      <td className="p-4">
        <DropdownMenu
          trigger={
            <button className="p-2 hover:bg-gray-100 rounded-lg">
              <MoreHorizontal size={16} />
            </button>
          }
          items={[
            { icon: Eye, label: "Ver", href: `/vehiculos/${vehicle.slug}` },
            { icon: Edit, label: "Editar", href: `/dealer/inventario/${vehicle.id}/editar` },
            { icon: BarChart2, label: "Estadísticas", href: `/dealer/inventario/${vehicle.id}/stats` },
            { icon: Pause, label: "Pausar", onClick: () => {} },
            { icon: Trash2, label: "Eliminar", onClick: () => {}, variant: "danger" },
          ]}
        />
      </td>
    </tr>
  );
}
```

---

## 🔧 PASO 4: Import Masivo

```typescript
// filepath: src/app/(main)/dealer/inventario/import/page.tsx
import { Metadata } from "next";
import { ImportWizard } from "@/components/dealer/inventory/ImportWizard";

export const metadata: Metadata = {
  title: "Importar Inventario | Dealer",
};

export default function ImportPage() {
  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Importar Inventario</h1>
        <p className="text-gray-600 mt-1">
          Sube un archivo CSV o Excel con tus vehículos
        </p>
      </div>

      <ImportWizard />
    </div>
  );
}
```

```typescript
// filepath: src/components/dealer/inventory/ImportWizard.tsx
"use client";

import { useState } from "react";
import { Upload, FileSpreadsheet, Check, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { useImportInventory } from "@/lib/hooks/useImportInventory";

type Step = "upload" | "mapping" | "preview" | "importing" | "done";

export function ImportWizard() {
  const [step, setStep] = useState<Step>("upload");
  const [file, setFile] = useState<File | null>(null);
  const { importFile, progress, errors, isImporting } = useImportInventory();

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (selected) {
      setFile(selected);
      setStep("mapping");
    }
  };

  return (
    <div className="bg-white rounded-xl border p-6">
      {step === "upload" && (
        <div className="text-center py-12">
          <FileSpreadsheet size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            Sube tu archivo
          </h3>
          <p className="text-gray-500 mb-6">CSV o Excel (.xlsx)</p>

          <input
            type="file"
            accept=".csv,.xlsx,.xls"
            onChange={handleFileSelect}
            className="hidden"
            id="file-upload"
          />
          <label htmlFor="file-upload">
            <Button asChild>
              <span>
                <Upload size={16} className="mr-2" />
                Seleccionar archivo
              </span>
            </Button>
          </label>

          <div className="mt-8 text-left">
            <a
              href="/templates/inventory-template.xlsx"
              className="text-primary-600 hover:underline text-sm"
            >
              Descargar plantilla de ejemplo
            </a>
          </div>
        </div>
      )}

      {step === "mapping" && file && (
        <div>
          <h3 className="font-medium mb-4">Archivo: {file.name}</h3>
          {/* Column mapping UI */}
          <Button onClick={() => setStep("preview")}>
            Continuar a preview
          </Button>
        </div>
      )}

      {step === "importing" && (
        <div className="text-center py-12">
          <div className="w-full bg-gray-200 rounded-full h-2 mb-4">
            <div
              className="bg-primary-600 h-2 rounded-full transition-all"
              style={{ width: `${progress}%` }}
            />
          </div>
          <p className="text-gray-600">Importando... {progress}%</p>
        </div>
      )}

      {step === "done" && (
        <div className="text-center py-12">
          <Check size={48} className="mx-auto text-green-500 mb-4" />
          <h3 className="text-lg font-medium text-gray-900">
            Importación completada
          </h3>
          {errors.length > 0 && (
            <p className="text-amber-600 mt-2">
              {errors.length} errores encontrados
            </p>
          )}
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 10: Upload 360° UX Mejorado (P1 - Feature Premium) ⭐

```typescript
// filepath: src/components/dealer/inventory/Video360UploadSection.tsx
"use client";

import * as React from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  Video,
  RotateCcw,
  Upload,
  X,
  CheckCircle,
  AlertCircle,
  Loader2,
  Info,
  Sparkles,
  Eye,
  Download,
  Trash2,
  RefreshCw,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Progress } from "@/components/ui/Progress";
import { Badge } from "@/components/ui/Badge";
import { useVehicle360Upload, useVehicle360Jobs } from "@/lib/hooks/useVehicle360";
import { formatFileSize, formatDate, cn } from "@/lib/utils";

interface Video360UploadSectionProps {
  vehicleId: string;
  onComplete?: () => void;
}

const ACCEPTED_FORMATS = ["video/mp4", "video/quicktime", "video/avi", "video/webm"];
const MAX_FILE_SIZE = 500 * 1024 * 1024; // 500MB
const MAX_DURATION = 60; // 60 segundos

export function Video360UploadSection({
  vehicleId,
  onComplete,
}: Video360UploadSectionProps) {
  const [selectedFile, setSelectedFile] = React.useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [isDragOver, setIsDragOver] = React.useState(false);
  const [validationError, setValidationError] = React.useState<string | null>(null);
  const inputRef = React.useRef<HTMLInputElement>(null);

  // Upload mutation
  const {
    mutate: uploadVideo,
    isLoading: isUploading,
    progress: uploadProgress,
    error: uploadError,
  } = useVehicle360Upload();

  // Jobs query (para tracking de procesamiento)
  const { data: jobs, refetch: refetchJobs } = useVehicle360Jobs(vehicleId);
  const currentJob = jobs?.[0]; // Último job

  // Validar archivo
  const validateFile = (file: File): string | null => {
    if (!ACCEPTED_FORMATS.includes(file.type)) {
      return "Formato no válido. Usa MP4, MOV, AVI o WebM.";
    }

    if (file.size > MAX_FILE_SIZE) {
      return `Archivo muy grande. Máximo ${formatFileSize(MAX_FILE_SIZE)}.`;
    }

    return null;
  };

  // Handle file selection
  const handleFileSelect = (file: File) => {
    const error = validateFile(file);
    if (error) {
      setValidationError(error);
      return;
    }

    setValidationError(null);
    setSelectedFile(file);

    // Create preview URL
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFileSelect(file);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);

    const file = e.dataTransfer.files[0];
    if (file) handleFileSelect(file);
  };

  const handleClear = () => {
    setSelectedFile(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(null);
    setValidationError(null);
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  };

  const handleUpload = () => {
    if (!selectedFile) return;

    uploadVideo(
      { vehicleId, file: selectedFile },
      {
        onSuccess: () => {
          refetchJobs();
          if (onComplete) onComplete();
        },
      }
    );
  };

  // Cleanup preview URL on unmount
  React.useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  // Auto-refresh jobs mientras está procesando
  React.useEffect(() => {
    if (currentJob?.status === "processing") {
      const interval = setInterval(() => {
        refetchJobs();
      }, 5000); // Poll cada 5 segundos
      return () => clearInterval(interval);
    }
  }, [currentJob?.status, refetchJobs]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
            <RotateCcw className="h-5 w-5 text-primary-600" />
            Vista 360° Interactiva
          </h3>
          <p className="text-sm text-gray-600 mt-1">
            Sube un video girando alrededor del vehículo y crearemos una vista 360° profesional
          </p>
        </div>

        {currentJob?.status === "completed" && (
          <Badge variant="success" className="flex items-center gap-1">
            <CheckCircle className="h-3 w-3" />
            Vista 360° activa
          </Badge>
        )}
      </div>

      {/* Current job status */}
      {currentJob && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className={cn(
            "rounded-xl p-4 border",
            currentJob.status === "pending" && "bg-yellow-50 border-yellow-200",
            currentJob.status === "processing" && "bg-blue-50 border-blue-200",
            currentJob.status === "completed" && "bg-green-50 border-green-200",
            currentJob.status === "failed" && "bg-red-50 border-red-200"
          )}
        >
          <div className="flex items-start justify-between">
            <div className="flex-1">
              {/* Status header */}
              <div className="flex items-center gap-2 mb-2">
                {currentJob.status === "pending" && (
                  <>
                    <Loader2 className="h-4 w-4 text-yellow-600 animate-spin" />
                    <span className="font-medium text-yellow-900">En cola...</span>
                  </>
                )}
                {currentJob.status === "processing" && (
                  <>
                    <Loader2 className="h-4 w-4 text-blue-600 animate-spin" />
                    <span className="font-medium text-blue-900">Procesando video...</span>
                  </>
                )}
                {currentJob.status === "completed" && (
                  <>
                    <CheckCircle className="h-4 w-4 text-green-600" />
                    <span className="font-medium text-green-900">Vista 360° lista</span>
                  </>
                )}
                {currentJob.status === "failed" && (
                  <>
                    <AlertCircle className="h-4 w-4 text-red-600" />
                    <span className="font-medium text-red-900">Error en procesamiento</span>
                  </>
                )}
              </div>

              {/* Progress bar for processing */}
              {currentJob.status === "processing" && (
                <div className="space-y-2">
                  <Progress value={currentJob.progress || 0} className="h-2" />
                  <p className="text-sm text-blue-700">
                    {currentJob.currentStep || "Extrayendo frames..."} ({currentJob.progress || 0}%)
                  </p>
                  <p className="text-xs text-blue-600">
                    Tiempo estimado: {Math.round((100 - (currentJob.progress || 0)) * 1.5)} segundos
                  </p>
                </div>
              )}

              {/* Completed info */}
              {currentJob.status === "completed" && (
                <div className="space-y-2">
                  <p className="text-sm text-green-700">
                    ✓ {currentJob.framesExtracted} frames extraídos
                  </p>
                  <p className="text-sm text-green-700">
                    ✓ Fondo removido automáticamente
                  </p>
                  <p className="text-xs text-green-600">
                    Completado en {currentJob.processingTimeSeconds}s • {formatDate(currentJob.completedAt!)}
                  </p>
                </div>
              )}

              {/* Error info */}
              {currentJob.status === "failed" && (
                <div className="space-y-2">
                  <p className="text-sm text-red-700">
                    {currentJob.errorMessage || "Ocurrió un error inesperado"}
                  </p>
                  <p className="text-xs text-red-600">
                    Intenta con un video diferente o contacta soporte
                  </p>
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2 ml-4">
              {currentJob.status === "completed" && (
                <>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      window.open(`/vehicles/${vehicleId}/360`, "_blank");
                    }}
                  >
                    <Eye className="h-4 w-4 mr-1" />
                    Ver
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={handleClear}
                    className="text-gray-600"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </>
              )}
              {currentJob.status === "failed" && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => {
                    // Retry job
                    // TODO: Implement retry logic
                    refetchJobs();
                  }}
                >
                  <RefreshCw className="h-4 w-4 mr-1" />
                  Reintentar
                </Button>
              )}
            </div>
          </div>
        </motion.div>
      )}

      {/* Upload zone - Solo mostrar si no hay job en progreso */}
      {!currentJob || currentJob.status === "failed" ? (
        <>
          {!selectedFile ? (
            <div
              className={cn(
                "border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-colors",
                isDragOver && "border-primary-400 bg-primary-50",
                !isDragOver && "border-gray-300 hover:border-primary-400 hover:bg-gray-50",
                validationError && "border-red-300 bg-red-50"
              )}
              onDrop={handleDrop}
              onDragOver={(e) => {
                e.preventDefault();
                setIsDragOver(true);
              }}
              onDragLeave={() => setIsDragOver(false)}
              onClick={() => inputRef.current?.click()}
            >
              <input
                ref={inputRef}
                type="file"
                accept="video/*"
                className="hidden"
                onChange={handleInputChange}
              />

              <Video className="h-12 w-12 mx-auto text-gray-400 mb-4" />

              <h3 className="font-semibold text-gray-900 mb-1">
                Subir Video 360°
              </h3>

              <p className="text-sm text-gray-500 mb-4">
                Arrastra un video aquí o haz clic para seleccionar
              </p>

              <p className="text-xs text-gray-400">
                MP4, MOV, AVI, WebM • Máx. 500MB • Máx. 60 segundos
              </p>

              {validationError && (
                <div className="mt-4 flex items-center justify-center gap-2 text-red-600">
                  <AlertCircle className="h-4 w-4" />
                  <span className="text-sm">{validationError}</span>
                </div>
              )}
            </div>
          ) : (
            /* Preview del video seleccionado */
            <motion.div
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="space-y-4"
            >
              <div className="relative rounded-xl overflow-hidden bg-black">
                <video
                  src={previewUrl!}
                  controls
                  className="w-full max-h-[300px] object-contain"
                />

                <button
                  type="button"
                  className="absolute top-2 right-2 bg-black/60 text-white p-1.5 rounded-full hover:bg-black/80"
                  onClick={handleClear}
                >
                  <X className="h-4 w-4" />
                </button>
              </div>

              <div className="flex items-center justify-between bg-gray-50 rounded-lg p-4">
                <div className="flex items-center gap-3">
                  <Video className="h-5 w-5 text-gray-400" />
                  <div>
                    <p className="font-medium text-sm text-gray-900">
                      {selectedFile.name}
                    </p>
                    <p className="text-xs text-gray-500">
                      {formatFileSize(selectedFile.size)}
                    </p>
                  </div>
                </div>

                <Button onClick={handleUpload} disabled={isUploading}>
                  {isUploading ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Subiendo... {uploadProgress}%
                    </>
                  ) : (
                    <>
                      <Upload className="h-4 w-4 mr-2" />
                      Procesar Video
                    </>
                  )}
                </Button>
              </div>

              {/* Upload progress */}
              {isUploading && (
                <div className="space-y-2">
                  <Progress value={uploadProgress} className="h-2" />
                  <p className="text-sm text-center text-gray-600">
                    Subiendo archivo... {uploadProgress}%
                  </p>
                </div>
              )}

              {/* Upload error */}
              {uploadError && (
                <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
                  <AlertCircle className="h-5 w-5 text-red-500 flex-shrink-0" />
                  <div>
                    <p className="font-medium text-red-900">Error en la subida</p>
                    <p className="text-sm text-red-700">{uploadError.message}</p>
                  </div>
                </div>
              )}

              {/* Explicación de qué pasará */}
              <div className="bg-gradient-to-br from-primary-50 to-blue-50 border border-primary-200 rounded-xl p-4">
                <h4 className="font-medium text-gray-900 flex items-center gap-2 mb-3">
                  <Sparkles className="h-4 w-4 text-primary-600" />
                  Procesamiento automático con IA
                </h4>
                <ul className="space-y-2">
                  <li className="text-sm text-gray-700 flex items-start gap-2">
                    <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0 mt-0.5" />
                    <span>
                      <strong>Extracción inteligente:</strong> Extraemos 6 frames del video (cada 60°) para crear la vista 360°
                    </span>
                  </li>
                  <li className="text-sm text-gray-700 flex items-start gap-2">
                    <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0 mt-0.5" />
                    <span>
                      <strong>Remoción de fondo:</strong> IA remueve el fondo automáticamente para foco total en el vehículo
                    </span>
                  </li>
                  <li className="text-sm text-gray-700 flex items-start gap-2">
                    <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0 mt-0.5" />
                    <span>
                      <strong>Vista interactiva:</strong> Los compradores podrán rotar el vehículo 360° con mouse o touch
                    </span>
                  </li>
                  <li className="text-sm text-gray-600 flex items-start gap-2">
                    <Info className="h-4 w-4 text-blue-600 flex-shrink-0 mt-0.5" />
                    <span>
                      <strong>Tiempo estimado:</strong> 2-5 minutos (te notificaremos cuando esté listo)
                    </span>
                  </li>
                </ul>
              </div>
            </motion.div>
          )}

          {/* Tips para grabar */}
          <div className="bg-gray-50 rounded-xl p-4">
            <h4 className="font-medium text-gray-900 mb-3 flex items-center gap-2">
              <Info className="h-4 w-4 text-primary-600" />
              💡 Tips para un video 360° perfecto
            </h4>
            <ul className="grid grid-cols-1 md:grid-cols-2 gap-2 text-sm text-gray-600">
              <li className="flex items-start gap-2">
                <span className="text-primary-600">•</span>
                <span>Graba con el vehículo sobre una plataforma giratoria (recomendado)</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-primary-600">•</span>
                <span>O camina alrededor del vehículo manteniéndote a la misma distancia</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-primary-600">•</span>
                <span>Usa buena iluminación, preferiblemente luz natural exterior</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-primary-600">•</span>
                <span>Mantén la cámara estable a la altura media del vehículo</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-primary-600">•</span>
                <span>Una vuelta completa y lenta (15-30 segundos óptimo)</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-primary-600">•</span>
                <span>Fondo limpio y sin obstrucciones para mejor resultado</span>
              </li>
            </ul>
          </div>

          {/* Benefits highlight */}
          <div className="bg-gradient-to-r from-green-50 to-emerald-50 border border-green-200 rounded-xl p-4">
            <h4 className="font-medium text-green-900 mb-2 flex items-center gap-2">
              <Sparkles className="h-4 w-4" />
              ¿Por qué agregar vista 360°?
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-3">
              <div>
                <p className="text-2xl font-bold text-green-600">+45%</p>
                <p className="text-xs text-green-700">Más vistas</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-green-600">+60%</p>
                <p className="text-xs text-green-700">Más consultas</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-green-600">2.5x</p>
                <p className="text-xs text-green-700">Más rápido vende</p>
              </div>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}
```

**Hooks requeridos:**

```typescript
// filepath: src/lib/hooks/useVehicle360.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { vehicle360ProcessingService } from "@/lib/services/vehicle360ProcessingService";
import { showToast } from "@/lib/toast";

export function useVehicle360Upload() {
  const queryClient = useQueryClient();
  const [progress, setProgress] = React.useState(0);

  const mutation = useMutation({
    mutationFn: ({ vehicleId, file }: { vehicleId: string; file: File }) => {
      return vehicle360ProcessingService.uploadAndProcess(
        vehicleId,
        file,
        (p) => {
          setProgress(p);
        },
      );
    },
    onSuccess: (_, { vehicleId }) => {
      queryClient.invalidateQueries(["vehicle-360-jobs", vehicleId]);
      showToast.success("Video subido", "Estamos procesando tu vista 360°");
      setProgress(0);
    },
    onError: () => {
      showToast.error("Error al subir", "Intenta de nuevo");
      setProgress(0);
    },
  });

  return {
    ...mutation,
    progress,
  };
}

export function useVehicle360Jobs(vehicleId: string) {
  return useQuery({
    queryKey: ["vehicle-360-jobs", vehicleId],
    queryFn: () => vehicle360ProcessingService.getJobsByVehicle(vehicleId),
    refetchInterval: (data) => {
      // Auto-refresh mientras haya jobs procesando
      const hasProcessing = data?.some((job) => job.status === "processing");
      return hasProcessing ? 5000 : false;
    },
  });
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /dealer/inventario muestra tabla ✅
# - Filtros funcionan ✅
# - Selección múltiple funciona ✅
# - Import wizard carga ✅
# - **Upload 360° con drag & drop funciona** ✅ NUEVO
# - **Preview de video antes de procesar** ✅ NUEVO
# - **Progress tracking en tiempo real** ✅ NUEVO
# - **Status cards con colores** ✅ NUEVO
# - **Auto-refresh mientras procesa** ✅ NUEVO
```

---

## 🔧 PASO 11: Processing Dashboard 360° (P2 - Monitoreo Avanzado) 📊

```typescript
// filepath: src/components/dealer/inventory/Processing360Dashboard.tsx
"use client";

import * as React from "react";
import { motion } from "framer-motion";
import {
  Video,
  CheckCircle,
  AlertCircle,
  Clock,
  RefreshCw,
  Trash2,
  Eye,
  BarChart3,
  TrendingUp,
  Loader2,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Progress } from "@/components/ui/Progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { useProcessingJobs, useRetryJob, useDeleteJob } from "@/lib/hooks/useVehicle360";
import { formatDate, formatDuration, cn } from "@/lib/utils";

interface Processing360DashboardProps {
  dealerId: string;
}

export function Processing360Dashboard({ dealerId }: Processing360DashboardProps) {
  const { data: jobs, isLoading, refetch } = useProcessingJobs(dealerId);
  const { mutate: retryJob } = useRetryJob();
  const { mutate: deleteJob } = useDeleteJob();

  const [filter, setFilter] = React.useState<"all" | "processing" | "completed" | "failed">("all");

  const filteredJobs = React.useMemo(() => {
    if (!jobs) return [];
    if (filter === "all") return jobs;
    return jobs.filter((job) => job.status === filter);
  }, [jobs, filter]);

  const stats = React.useMemo(() => {
    if (!jobs) return { total: 0, processing: 0, completed: 0, failed: 0, avgTime: 0, successRate: 0 };

    const total = jobs.length;
    const processing = jobs.filter((j) => j.status === "processing").length;
    const completed = jobs.filter((j) => j.status === "completed").length;
    const failed = jobs.filter((j) => j.status === "failed").length;

    const completedJobs = jobs.filter((j) => j.status === "completed" && j.processingTimeSeconds);
    const avgTime = completedJobs.length > 0
      ? completedJobs.reduce((sum, j) => sum + (j.processingTimeSeconds || 0), 0) / completedJobs.length
      : 0;

    const successRate = total > 0 ? (completed / (completed + failed)) * 100 : 0;

    return { total, processing, completed, failed, avgTime, successRate };
  }, [jobs]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Dashboard de Procesamiento 360°</h2>
          <p className="text-gray-600 mt-1">
            Monitorea el estado de todos tus videos 360° en tiempo real
          </p>
        </div>
        <Button variant="outline" onClick={() => refetch()}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Actualizar
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white rounded-xl p-4 shadow-sm border">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Total Jobs</p>
              <p className="text-2xl font-bold text-gray-900 mt-1">{stats.total}</p>
            </div>
            <Video className="h-8 w-8 text-gray-400" />
          </div>
        </div>

        <div className="bg-blue-50 rounded-xl p-4 shadow-sm border border-blue-200">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-blue-700">Procesando</p>
              <p className="text-2xl font-bold text-blue-900 mt-1">{stats.processing}</p>
            </div>
            <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
          </div>
        </div>

        <div className="bg-green-50 rounded-xl p-4 shadow-sm border border-green-200">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-green-700">Completados</p>
              <p className="text-2xl font-bold text-green-900 mt-1">{stats.completed}</p>
            </div>
            <CheckCircle className="h-8 w-8 text-green-500" />
          </div>
        </div>

        <div className="bg-red-50 rounded-xl p-4 shadow-sm border border-red-200">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-red-700">Fallidos</p>
              <p className="text-2xl font-bold text-red-900 mt-1">{stats.failed}</p>
            </div>
            <AlertCircle className="h-8 w-8 text-red-500" />
          </div>
        </div>
      </div>

      {/* Performance Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-white rounded-xl p-4 shadow-sm border">
          <div className="flex items-center gap-2 mb-3">
            <Clock className="h-5 w-5 text-gray-600" />
            <h3 className="font-semibold text-gray-900">Tiempo Promedio</h3>
          </div>
          <p className="text-3xl font-bold text-primary-600">
            {formatDuration(Math.round(stats.avgTime))}
          </p>
          <p className="text-sm text-gray-600 mt-1">Por video procesado</p>
        </div>

        <div className="bg-white rounded-xl p-4 shadow-sm border">
          <div className="flex items-center gap-2 mb-3">
            <TrendingUp className="h-5 w-5 text-gray-600" />
            <h3 className="font-semibold text-gray-900">Tasa de Éxito</h3>
          </div>
          <p className="text-3xl font-bold text-green-600">
            {stats.successRate.toFixed(1)}%
          </p>
          <p className="text-sm text-gray-600 mt-1">
            {stats.completed} de {stats.completed + stats.failed} exitosos
          </p>
        </div>
      </div>

      {/* Filters */}
      <Tabs value={filter} onValueChange={(v) => setFilter(v as any)}>
        <TabsList>
          <TabsTrigger value="all">Todos ({stats.total})</TabsTrigger>
          <TabsTrigger value="processing">Procesando ({stats.processing})</TabsTrigger>
          <TabsTrigger value="completed">Completados ({stats.completed})</TabsTrigger>
          <TabsTrigger value="failed">Fallidos ({stats.failed})</TabsTrigger>
        </TabsList>

        <TabsContent value={filter} className="mt-4">
          {/* Jobs List */}
          <div className="space-y-4">
            {filteredJobs.length === 0 ? (
              <div className="text-center py-12 text-gray-500">
                <Video className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <p>No hay jobs en esta categoría</p>
              </div>
            ) : (
              filteredJobs.map((job) => (
                <motion.div
                  key={job.id}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  className={cn(
                    "bg-white rounded-xl p-4 shadow-sm border",
                    job.status === "processing" && "border-blue-300",
                    job.status === "completed" && "border-green-300",
                    job.status === "failed" && "border-red-300"
                  )}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      {/* Status badge */}
                      <div className="flex items-center gap-3 mb-2">
                        {job.status === "processing" && (
                          <>
                            <Loader2 className="h-5 w-5 text-blue-600 animate-spin" />
                            <Badge variant="default" className="bg-blue-100 text-blue-700">
                              Procesando
                            </Badge>
                          </>
                        )}
                        {job.status === "completed" && (
                          <>
                            <CheckCircle className="h-5 w-5 text-green-600" />
                            <Badge variant="success">Completado</Badge>
                          </>
                        )}
                        {job.status === "failed" && (
                          <>
                            <AlertCircle className="h-5 w-5 text-red-600" />
                            <Badge variant="destructive">Fallido</Badge>
                          </>
                        )}
                        <span className="text-sm text-gray-600">
                          {formatDate(job.createdAt)}
                        </span>
                      </div>

                      {/* Vehicle info */}
                      <h4 className="font-semibold text-gray-900">{job.vehicleTitle}</h4>

                      {/* Progress or details */}
                      {job.status === "processing" && (
                        <div className="mt-3 space-y-2">
                          <Progress value={job.progress || 0} className="h-2" />
                          <p className="text-sm text-gray-600">
                            {job.currentStep || "Procesando..."} ({job.progress || 0}%)
                          </p>
                        </div>
                      )}

                      {job.status === "completed" && (
                        <div className="mt-2 text-sm text-gray-600">
                          <p>✓ {job.framesExtracted} frames extraídos</p>
                          <p>⏱️ Procesado en {formatDuration(job.processingTimeSeconds!)}</p>
                        </div>
                      )}

                      {job.status === "failed" && job.errorMessage && (
                        <div className="mt-2 text-sm text-red-600 bg-red-50 p-2 rounded">
                          {job.errorMessage}
                        </div>
                      )}

                      {/* Logs (for failed jobs) */}
                      {job.status === "failed" && job.logs && (
                        <details className="mt-3">
                          <summary className="text-sm text-gray-600 cursor-pointer hover:text-gray-900">
                            Ver logs técnicos
                          </summary>
                          <pre className="mt-2 text-xs bg-gray-900 text-green-400 p-3 rounded overflow-x-auto">
                            {job.logs}
                          </pre>
                        </details>
                      )}
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-2 ml-4">
                      {job.status === "completed" && (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => window.open(`/vehicles/${job.vehicleId}/360`, "_blank")}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      )}
                      {job.status === "failed" && (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => retryJob(job.id)}
                        >
                          <RefreshCw className="h-4 w-4" />
                        </Button>
                      )}
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {
                          if (confirm("¿Eliminar este job?")) {
                            deleteJob(job.id);
                          }
                        }}
                      >
                        <Trash2 className="h-4 w-4 text-red-600" />
                      </Button>
                    </div>
                  </div>
                </motion.div>
              ))
            )}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

**Hooks requeridos:**

```typescript
// filepath: src/lib/hooks/useVehicle360.ts
export function useProcessingJobs(dealerId: string) {
  return useQuery({
    queryKey: ["processing-jobs", dealerId],
    queryFn: () => vehicle360ProcessingService.getJobsByDealer(dealerId),
    refetchInterval: 5000, // Auto-refresh cada 5s
  });
}

export function useRetryJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId: string) => vehicle360ProcessingService.retryJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries(["processing-jobs"]);
      showToast.success("Job reenviado", "Procesando de nuevo...");
    },
  });
}

export function useDeleteJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId: string) => vehicle360ProcessingService.deleteJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries(["processing-jobs"]);
      showToast.success("Job eliminado");
    },
  });
}
```

---

## 📚 DOCUMENTACIÓN CONSOLIDADA

> **NOTA:** Este documento consolida toda la documentación de Dealer Inventory previamente distribuida en múltiples archivos.

### Páginas Incluidas en este Documento

| Página                    | Ruta                           | LOC  | Descripción                        |
| ------------------------- | ------------------------------ | ---- | ---------------------------------- |
| **DealerInventoryPage**   | `/dealer/inventory`            | 603  | Lista de inventario con grid/tabla |
| **DealerAddVehiclePage**  | `/dealer/add-vehicle`          | 455  | Wizard 5 pasos para publicar       |
| **DealerVehicleEditPage** | `/dealer/inventory/:id/edit`   | 933  | Editar con tabs                    |
| **DealerImportPage**      | `/dealer/inventory/import`     | ~300 | Importar CSV/Excel/JSON            |
| **CSVImportPage**         | `/dealer/inventory/import/csv` | ~200 | Importación masiva                 |

### Arquitectura Completa de Inventario

```
pages/dealer/
├── DealerInventoryPage.tsx       # Lista de inventario (603 líneas)
├── DealerAddVehiclePage.tsx      # Publicar vehículo (455 líneas)
├── DealerVehicleEditPage.tsx     # Editar vehículo (933 líneas)
├── DealerImportPage.tsx          # Importar CSV/Excel
└── components/
    ├── VehicleGridCard.tsx       # Card en vista grid
    ├── VehicleTableRow.tsx       # Fila en vista tabla
    ├── VehicleFilters.tsx        # Filtros de búsqueda
    ├── BulkActions.tsx           # Acciones masivas
    └── forms/
        ├── VehicleInfoStep.tsx   # Paso 1: Información
        ├── PhotosStep.tsx        # Paso 2: Fotos (Drag & Drop)
        ├── FeaturesStep.tsx      # Paso 3: Características
        ├── PricingStep.tsx       # Paso 4: Precios
        └── ReviewStep.tsx        # Paso 5: Revisión
```

### Tabs del Editor (Edit Page)

```
[Basic] [Details] [Images] [Media 360°] [Pricing]
   │        │         │          │           │
   │        │         │          │           └── Price, Location
   │        │         │          └── Video Upload, Processing Status
   │        │         └── Gallery Management, Reorder
   │        └── Fuel, Transmission, Body, Features
   └── Title, Make, Model, Year
```

### Estados de Vehículos

```
Draft → PendingReview → Active → Sold
                    ↓      ↓
              Rejected   Reserved
                           ↓
                        Archived
```

---

## 📚 REFERENCIAS

### Documentos Process Matrix

- [01-vehicles-service.md](../../process-matrix/02-VEHICULOS-VENTA/01-vehicles-service.md) - CRUD vehículos
- [02-inventory-management.md](../../process-matrix/02-VEHICULOS-VENTA/02-inventory-management.md) - Gestión inventario

### Documentos Frontend

- **[100-media-multimedia-completo.md](../04-VENDEDOR/05-media-multimedia.md)** - 🆕 Media Library, bulk upload
- [06-dealer-dashboard.md](./01-dealer-dashboard.md) - Dashboard principal
- [04-publicar.md](../04-VENDEDOR/01-publicar-vehiculo.md) - Formulario de publicación individual

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-inventario.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer Inventario", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe mostrar lista de inventario", async ({ page }) => {
    await page.goto("/dealer/inventario");

    await expect(
      page.getByRole("heading", { name: /inventario/i }),
    ).toBeVisible();
    await expect(page.getByTestId("inventory-table")).toBeVisible();
  });

  test("debe filtrar por estado", async ({ page }) => {
    await page.goto("/dealer/inventario");

    await page.getByRole("combobox", { name: /estado/i }).click();
    await page.getByRole("option", { name: /activo/i }).click();

    await expect(page).toHaveURL(/status=active/);
  });

  test("debe agregar nuevo vehículo", async ({ page }) => {
    await page.goto("/dealer/inventario");

    await page.getByRole("button", { name: /agregar vehículo/i }).click();
    await expect(page).toHaveURL("/dealer/inventario/nuevo");
  });

  test("debe editar vehículo existente", async ({ page }) => {
    await page.goto("/dealer/inventario");

    await page
      .getByTestId("vehicle-row")
      .first()
      .getByRole("button", { name: /editar/i })
      .click();
    await expect(page).toHaveURL(/\/dealer\/inventario\/editar\//);
  });

  test("debe pausar/activar vehículo", async ({ page }) => {
    await page.goto("/dealer/inventario");

    await page
      .getByTestId("vehicle-row")
      .first()
      .getByRole("button", { name: /pausar/i })
      .click();
    await expect(page.getByText(/vehículo pausado/i)).toBeVisible();
  });

  test("debe importar CSV", async ({ page }) => {
    await page.goto("/dealer/inventario");

    await page.getByRole("button", { name: /importar/i }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/10-dealer-crm.md`
