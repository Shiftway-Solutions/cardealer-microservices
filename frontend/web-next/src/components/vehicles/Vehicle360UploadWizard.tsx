'use client';

/**
 * Vehicle360UploadWizard
 *
 * A multi-step wizard that lets dealers upload a 360° video, track processing,
 * and preview the resulting 360° frames.
 *
 * Steps:
 *  1. Select — drag-and-drop / file picker for the video
 *  2. Configure — choose frame count, quality, format
 *  3. Upload + Process — shows upload progress then processing progress
 *  4. Done — preview of extracted frames with bg-removed result
 */

import { useState, useRef, useCallback } from 'react';
import Image from 'next/image';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Upload,
  Video,
  CheckCircle,
  XCircle,
  RefreshCw,
  RotateCcw,
  Loader2,
  Layers,
  Zap,
  Eye,
} from 'lucide-react';
import { toast } from 'sonner';
import { useUpload360Video, use360JobStatus } from '@/hooks/use-vehicle360';
import type { ProcessingJob, Vehicle360Status } from '@/services/vehicle360';

// ── Types ──────────────────────────────────────────────────────────────────────

interface Props {
  vehicleId: string;
  onComplete?: (job: ProcessingJob) => void;
  onCancel?: () => void;
}

type WizardStep = 'select' | 'configure' | 'processing' | 'done' | 'error';

interface ProcessingConfig {
  frameCount: number;
  imageFormat: 'Jpeg' | 'Png' | 'WebP';
  quality: 'Low' | 'Medium' | 'High' | 'Ultra';
}

// ── Helpers ────────────────────────────────────────────────────────────────────

const ALLOWED_VIDEO_TYPES = ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/webm'];
const MAX_FILE_SIZE_MB = 100;

const STATUS_LABEL: Record<Vehicle360Status, string> = {
  Pending: 'Pendiente…',
  Uploading: 'Subiendo al proveedor…',
  Processing: 'Extrayendo frames…',
  Downloading: 'Descargando resultados…',
  Completed: 'Completado ✓',
  Failed: 'Error',
  Cancelled: 'Cancelado',
  Retrying: 'Reintentando…',
};

const STATUS_PROGRESS: Record<Vehicle360Status, number> = {
  Pending: 10,
  Uploading: 25,
  Processing: 55,
  Downloading: 80,
  Completed: 100,
  Failed: 100,
  Cancelled: 0,
  Retrying: 20,
};

// ── Component ─────────────────────────────────────────────────────────────────

export function Vehicle360UploadWizard({ vehicleId, onComplete, onCancel }: Props) {
  const [step, setStep] = useState<WizardStep>('select');
  const [file, setFile] = useState<File | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [jobId, setJobId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [config, setConfig] = useState<ProcessingConfig>({
    frameCount: 36,
    imageFormat: 'Jpeg',
    quality: 'High',
  });
  const fileInputRef = useRef<HTMLInputElement>(null);

  const uploadMutation = useUpload360Video();

  // Poll job status while in processing step
  const { data: jobStatus } = use360JobStatus(step === 'processing' ? jobId : null);

  // React to job status changes
  if (
    step === 'processing' &&
    jobStatus &&
    (jobStatus.status === 'Completed' || jobStatus.status === 'Failed' || jobStatus.status === 'Cancelled')
  ) {
    if (jobStatus.status === 'Completed') {
      setStep('done');
      onComplete?.(jobStatus);
    } else {
      setStep('error');
      setErrorMessage(jobStatus.errorMessage ?? 'El procesamiento falló. Intenta de nuevo.');
    }
  }

  // ── File handling ────────────────────────────────────────────────────────────

  const validateAndSetFile = useCallback((f: File) => {
    if (!ALLOWED_VIDEO_TYPES.includes(f.type)) {
      toast.error('Formato no soportado. Usa MP4, MOV, AVI o WebM.');
      return false;
    }
    const sizeMb = f.size / (1024 * 1024);
    if (sizeMb > MAX_FILE_SIZE_MB) {
      toast.error(`El video es demasiado grande (${sizeMb.toFixed(0)} MB). Máximo ${MAX_FILE_SIZE_MB} MB.`);
      return false;
    }
    setFile(f);
    return true;
  }, []);

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setIsDragging(false);
      const dropped = e.dataTransfer.files[0];
      if (dropped) validateAndSetFile(dropped);
    },
    [validateAndSetFile]
  );

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const picked = e.target.files?.[0];
    if (picked) validateAndSetFile(picked);
  };

  // ── Upload + process ─────────────────────────────────────────────────────────

  const handleStartProcessing = async () => {
    if (!file) return;
    setStep('processing');
    setUploadProgress(0);
    setErrorMessage(null);

    try {
      const job = await uploadMutation.mutateAsync({
        file,
        vehicleId,
        frameCount: config.frameCount,
        imageFormat: config.imageFormat,
        quality: config.quality,
        onProgress: p => setUploadProgress(p),
      });
      setJobId(job.jobId);
      toast.success('Video recibido. Procesando frames…');
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Error al subir el video.';
      setStep('error');
      setErrorMessage(message);
      toast.error(message);
    }
  };

  const handleReset = () => {
    setStep('select');
    setFile(null);
    setUploadProgress(0);
    setJobId(null);
    setErrorMessage(null);
    setConfig({ frameCount: 36, imageFormat: 'Jpeg', quality: 'High' });
  };

  // ── Render: step = select ────────────────────────────────────────────────────

  if (step === 'select') {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Video className="h-5 w-5 text-primary" />
            Vista 360° — Subir Video
          </CardTitle>
          <CardDescription>
            Sube un video girando alrededor del vehículo. El sistema extraerá los frames
            automáticamente y les quitará el fondo.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Drop zone */}
          <div
            onDragOver={e => { e.preventDefault(); setIsDragging(true); }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className={`cursor-pointer rounded-xl border-2 border-dashed p-10 text-center transition-colors ${
              isDragging
                ? 'border-primary bg-primary/5'
                : file
                ? 'border-green-500 bg-green-500/5'
                : 'border-border bg-muted/30 hover:border-primary hover:bg-primary/5'
            }`}
          >
            {file ? (
              <div className="flex flex-col items-center gap-2">
                <CheckCircle className="h-12 w-12 text-green-500" />
                <p className="font-medium text-green-700 dark:text-green-400">{file.name}</p>
                <p className="text-muted-foreground text-sm">
                  {(file.size / (1024 * 1024)).toFixed(1)} MB · {file.type}
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={e => { e.stopPropagation(); setFile(null); }}
                  className="mt-2"
                >
                  Cambiar video
                </Button>
              </div>
            ) : (
              <div className="flex flex-col items-center gap-3">
                <Upload className="text-muted-foreground h-12 w-12" />
                <div>
                  <p className="font-medium">Arrastra tu video aquí</p>
                  <p className="text-muted-foreground text-sm">o haz clic para seleccionar</p>
                </div>
                <p className="text-muted-foreground text-xs">MP4, MOV, AVI, WebM · máx. 100 MB</p>
              </div>
            )}
          </div>
          <input
            ref={fileInputRef}
            type="file"
            accept="video/mp4,video/quicktime,video/x-msvideo,video/webm"
            className="hidden"
            onChange={handleFileChange}
          />

          {/* Tips */}
          <div className="bg-muted/50 rounded-lg p-4 text-sm space-y-1">
            <p className="font-medium">💡 Tips para un mejor resultado:</p>
            <ul className="text-muted-foreground list-disc list-inside space-y-0.5">
              <li>Gira 360° lentamente alrededor del vehículo</li>
              <li>Fondo liso (pared, piso limpio) mejora la remoción automática</li>
              <li>Buena iluminación (luz natural o de estudio)</li>
              <li>Duración ideal: 10–30 segundos</li>
            </ul>
          </div>

          <div className="flex justify-end gap-3">
            {onCancel && (
              <Button variant="outline" onClick={onCancel}>
                Cancelar
              </Button>
            )}
            <Button
              disabled={!file}
              onClick={() => setStep('configure')}
            >
              Siguiente
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  // ── Render: step = configure ─────────────────────────────────────────────────

  if (step === 'configure') {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Layers className="h-5 w-5 text-primary" />
            Configurar Extracción
          </CardTitle>
          <CardDescription>
            Ajusta cómo se extraerán los frames del video.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            {/* Frame count */}
            <div>
              <label className="mb-1.5 block text-sm font-medium">Cantidad de frames</label>
              <Select
                value={String(config.frameCount)}
                onValueChange={v => setConfig(c => ({ ...c, frameCount: Number(v) }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="12">12 frames (rápido)</SelectItem>
                  <SelectItem value="24">24 frames (estándar)</SelectItem>
                  <SelectItem value="36">36 frames (suave)</SelectItem>
                  <SelectItem value="48">48 frames (muy suave)</SelectItem>
                  <SelectItem value="72">72 frames (premium)</SelectItem>
                </SelectContent>
              </Select>
              <p className="text-muted-foreground mt-1 text-xs">
                Más frames = rotación más fluida pero más tiempo
              </p>
            </div>

            {/* Format */}
            <div>
              <label className="mb-1.5 block text-sm font-medium">Formato de imagen</label>
              <Select
                value={config.imageFormat}
                onValueChange={v => setConfig(c => ({ ...c, imageFormat: v as ProcessingConfig['imageFormat'] }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Jpeg">JPEG (más ligero)</SelectItem>
                  <SelectItem value="Png">PNG (transparencia)</SelectItem>
                  <SelectItem value="WebP">WebP (moderno)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Quality */}
            <div>
              <label className="mb-1.5 block text-sm font-medium">Calidad</label>
              <Select
                value={config.quality}
                onValueChange={v => setConfig(c => ({ ...c, quality: v as ProcessingConfig['quality'] }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Low">Baja (más rápido)</SelectItem>
                  <SelectItem value="Medium">Media</SelectItem>
                  <SelectItem value="High">Alta (recomendado)</SelectItem>
                  <SelectItem value="Ultra">Ultra (lento)</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Summary badge */}
          <div className="flex items-center gap-2 text-sm">
            <Zap className="h-4 w-4 text-yellow-500" />
            <span className="text-muted-foreground">
              Se extraerán <strong>{config.frameCount} frames</strong> en formato{' '}
              <strong>{config.imageFormat}</strong>, calidad <strong>{config.quality}</strong>.
              {config.imageFormat === 'Png' && ' El fondo se removerá automáticamente.'}
            </span>
          </div>

          <div className="flex justify-between gap-3">
            <Button variant="outline" onClick={() => setStep('select')}>
              Atrás
            </Button>
            <Button onClick={handleStartProcessing}>
              <Upload className="mr-2 h-4 w-4" />
              Subir y Procesar
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  // ── Render: step = processing ────────────────────────────────────────────────

  if (step === 'processing') {
    const isUploading = uploadProgress < 100;
    const processingStatus = jobStatus?.status ?? 'Pending';
    const processingProgress = isUploading
      ? Math.round(uploadProgress * 0.3)  // upload = first 30% of total bar
      : STATUS_PROGRESS[processingStatus as Vehicle360Status] ?? 30;
    const statusLabel = isUploading
      ? `Subiendo video… ${uploadProgress}%`
      : STATUS_LABEL[processingStatus as Vehicle360Status] ?? 'Procesando…';

    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Loader2 className="h-5 w-5 animate-spin text-primary" />
            Procesando Vista 360°
          </CardTitle>
          <CardDescription>
            No cierres esta página. El proceso puede tardar 1–3 minutos.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">{statusLabel}</span>
              <span className="font-medium">{processingProgress}%</span>
            </div>
            <Progress value={processingProgress} className="h-3" />
          </div>

          {/* Processing stages visualization */}
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            {(
              [
                { label: 'Video recibido', doneStatuses: ['Uploading', 'Processing', 'Downloading', 'Completed'], activeStatus: 'Uploading' },
                { label: 'Extrayendo frames', doneStatuses: ['Processing', 'Downloading', 'Completed'], activeStatus: 'Processing' },
                { label: 'Descargando', doneStatuses: ['Downloading', 'Completed'], activeStatus: 'Downloading' },
                { label: 'Completado', doneStatuses: ['Completed'], activeStatus: 'Completed' },
              ] as const
            ).map(stage => {
              const isDone = !isUploading && (stage.doneStatuses as readonly string[]).includes(processingStatus);
              const isActive = !isUploading && stage.activeStatus === processingStatus;
              return (
                <div
                  key={stage.label}
                  className={`rounded-lg border p-3 text-center text-xs transition-colors ${
                    isDone
                      ? 'border-green-500/50 bg-green-500/10 text-green-700 dark:text-green-400'
                      : isActive
                      ? 'border-primary/50 bg-primary/10 text-primary'
                      : 'border-border bg-muted/30 text-muted-foreground'
                  }`}
                >
                  {isDone ? '✓ ' : isActive ? '⏳ ' : '○ '}
                  {stage.label}
                </div>
              );
            })}
          </div>

          <p className="text-muted-foreground text-center text-xs">
            Job ID: <code className="font-mono">{jobId ?? '—'}</code>
          </p>
        </CardContent>
      </Card>
    );
  }

  // ── Render: step = done ──────────────────────────────────────────────────────

  if (step === 'done' && jobStatus) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-green-600 dark:text-green-400">
            <CheckCircle className="h-5 w-5" />
            Vista 360° lista
          </CardTitle>
          <CardDescription>
            Se extrajeron {jobStatus.frameCount ?? config.frameCount} frames correctamente.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Frame preview grid */}
          {jobStatus.frameUrls && jobStatus.frameUrls.length > 0 ? (
            <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
              {jobStatus.frameUrls.slice(0, 12).map((url, i) => (
                <div
                  key={i}
                  className="bg-checkerboard aspect-square overflow-hidden rounded-lg border"
                >
                  <Image
                    src={url}
                    alt={`Frame ${i + 1}`}
                    width={120}
                    height={120}
                    className="h-full w-full object-cover"
                  />
                </div>
              ))}
              {(jobStatus.frameUrls?.length ?? 0) > 12 && (
                <div className="bg-muted text-muted-foreground flex aspect-square items-center justify-center rounded-lg border text-xs">
                  +{(jobStatus.frameUrls?.length ?? 0) - 12} más
                </div>
              )}
            </div>
          ) : (
            <div className="bg-muted flex items-center justify-center rounded-lg p-8 text-center">
              <p className="text-muted-foreground text-sm">
                Los frames se están guardando. Visita la vista 360° del vehículo para verlos.
              </p>
            </div>
          )}

          <div className="flex items-center justify-between">
            <Badge variant="secondary" className="gap-1">
              <Eye className="h-3 w-3" />
              {jobStatus.frameCount ?? config.frameCount} frames procesados
            </Badge>
            <div className="flex gap-2">
              <Button variant="outline" onClick={handleReset}>
                <RotateCcw className="mr-2 h-4 w-4" />
                Subir otro video
              </Button>
              <Button asChild>
                <a href={`/vehiculos/${vehicleId}/360`} target="_blank" rel="noopener noreferrer">
                  <Eye className="mr-2 h-4 w-4" />
                  Ver Vista 360°
                </a>
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  // ── Render: step = error ─────────────────────────────────────────────────────

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-destructive">
          <XCircle className="h-5 w-5" />
          Error en el procesamiento
        </CardTitle>
        <CardDescription>{errorMessage ?? 'Ocurrió un error inesperado.'}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex gap-3">
          <Button variant="outline" onClick={onCancel}>
            Cancelar
          </Button>
          <Button onClick={handleReset}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Intentar de nuevo
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

export default Vehicle360UploadWizard;
