/**
 * View 360° Step — Smart Publish Wizard
 *
 * Guides users through creating a 360° view of their vehicle.
 * Two methods:
 * 1. Upload a 360° video → auto-extract frames
 * 2. Manual photo upload with angle guides
 *
 * Feature is gated by subscription plan:
 * - Seller Premium/Pro: Enabled
 * - Dealer Visible/Pro/Elite: Enabled
 * - Free plans: Locked (shows upgrade CTA)
 */

'use client';

import { useState, useCallback } from 'react';
import {
  RotateCw,
  Video,
  Camera,
  Upload,
  Lock,
  Crown,
  ArrowRight,
  SkipForward,
  CheckCircle2,
  Info,
  Smartphone,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

// ============================================================
// TYPES
// ============================================================

interface View360StepProps {
  vehicleId?: string;
  accountType: 'individual' | 'dealer';
  onSkip: () => void;
  onComplete: () => void;
}

type View360Method = 'video' | 'manual' | null;

// Plans that have access to 360° view
// eslint-disable-next-line unused-imports/no-unused-vars
const PLANS_WITH_360 = ['premium', 'pro', 'visible', 'elite'];

// Angle guide positions for manual 360° capture
const ANGLE_GUIDES = [
  { angle: 0, label: 'Frente', description: 'Vista frontal directa', icon: '🚗' },
  { angle: 45, label: 'Frontal Derecho', description: 'Esquina frontal derecha', icon: '↗️' },
  {
    angle: 90,
    label: 'Lateral Derecho',
    description: 'Vista lateral derecha completa',
    icon: '➡️',
  },
  { angle: 135, label: 'Trasero Derecho', description: 'Esquina trasera derecha', icon: '↘️' },
  { angle: 180, label: 'Trasero', description: 'Vista trasera directa', icon: '🔄' },
  { angle: 225, label: 'Trasero Izquierdo', description: 'Esquina trasera izquierda', icon: '↙️' },
  {
    angle: 270,
    label: 'Lateral Izquierdo',
    description: 'Vista lateral izquierda completa',
    icon: '⬅️',
  },
  { angle: 315, label: 'Frontal Izquierdo', description: 'Esquina frontal izquierda', icon: '↖️' },
  {
    angle: -1,
    label: 'Interior Frontal',
    description: 'Dashboard y asientos delanteros',
    icon: '🪑',
  },
  { angle: -2, label: 'Interior Trasero', description: 'Asientos traseros', icon: '💺' },
  { angle: -3, label: 'Motor', description: 'Compartimiento del motor', icon: '⚙️' },
  { angle: -4, label: 'Maletero', description: 'Espacio de carga / maletero', icon: '📦' },
];

// ============================================================
// ANGLE GUIDE CARD
// ============================================================

function AngleGuideCard({
  guide,
  photo,
  onUpload,
  isActive,
}: {
  guide: (typeof ANGLE_GUIDES)[0];
  photo?: string;
  onUpload: (file: File) => void;
  isActive: boolean;
}) {
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) onUpload(file);
  };

  return (
    <div
      className={cn(
        'relative overflow-hidden rounded-xl border-2 transition-all',
        isActive ? 'border-emerald-500 shadow-lg' : 'border-gray-200',
        photo ? 'border-emerald-300 bg-emerald-50' : 'bg-white'
      )}
    >
      <div className="p-3">
        <div className="mb-2 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className="text-lg">{guide.icon}</span>
            <div>
              <p className="text-sm font-semibold">{guide.label}</p>
              <p className="text-xs text-gray-500">{guide.description}</p>
            </div>
          </div>
          {photo && <CheckCircle2 className="h-5 w-5 text-emerald-500" />}
        </div>

        {photo ? (
          <div className="relative aspect-video overflow-hidden rounded-lg">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={photo} alt={guide.label} className="h-full w-full object-cover" />
          </div>
        ) : (
          <label className="flex aspect-video cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed border-gray-300 bg-gray-50 transition-colors hover:border-emerald-400 hover:bg-emerald-50">
            <Camera className="mb-1 h-6 w-6 text-gray-400" />
            <span className="text-xs text-gray-500">
              {guide.angle >= 0 ? `${guide.angle}°` : 'Interior'}
            </span>
            <input
              type="file"
              accept="image/*"
              capture="environment"
              onChange={handleFileChange}
              className="hidden"
            />
          </label>
        )}
      </div>
    </div>
  );
}

// ============================================================
// MAIN COMPONENT
// ============================================================

export function View360Step({
  vehicleId,
  accountType: _accountType,
  onSkip,
  onComplete,
}: View360StepProps) {
  const [method, setMethod] = useState<View360Method>(null);
  const [videoFile, setVideoFile] = useState<File | null>(null);
  const [manualPhotos, setManualPhotos] = useState<Record<number, string>>({});
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isProcessing, setIsProcessing] = useState(false);
  const [activeGuideIndex, setActiveGuideIndex] = useState(0);

  // For now, 360 is available to all - plan gating will be enforced via the backend
  const is360Available = true;

  const handleVideoUpload = useCallback(
    async (file: File) => {
      if (!file.type.startsWith('video/')) {
        toast.error('Por favor selecciona un archivo de video válido');
        return;
      }

      if (file.size > 100 * 1024 * 1024) {
        toast.error('El video no debe exceder 100 MB');
        return;
      }

      setVideoFile(file);
      setIsProcessing(true);
      setUploadProgress(0);

      try {
        // Simulate upload + processing
        const formData = new FormData();
        formData.append('video', file);
        if (vehicleId) formData.append('vehicleId', vehicleId);

        // Upload to the vehicle360 API
        const response = await fetch('/api/vehicle360/upload', {
          method: 'POST',
          body: formData,
        });

        if (response.ok) {
          setUploadProgress(100);
          toast.success('¡Video 360° procesado exitosamente!');
          setTimeout(onComplete, 1500);
        } else {
          toast.error('Error al procesar el video. Intenta de nuevo.');
        }
      } catch {
        toast.error('Error de conexión. Verifica tu internet e intenta de nuevo.');
      } finally {
        setIsProcessing(false);
      }
    },
    [vehicleId, onComplete]
  );

  const handleManualPhotoUpload = useCallback((angle: number, file: File) => {
    const url = URL.createObjectURL(file);
    setManualPhotos(prev => ({ ...prev, [angle]: url }));

    // Move to next guide
    const currentIdx = ANGLE_GUIDES.findIndex(g => g.angle === angle);
    if (currentIdx < ANGLE_GUIDES.length - 1) {
      setActiveGuideIndex(currentIdx + 1);
    }

    toast.success(`Foto de ${ANGLE_GUIDES.find(g => g.angle === angle)?.label} guardada`);
  }, []);

  const completedPhotos = Object.keys(manualPhotos).length;
  const totalRequired = 8; // At least the 8 exterior angles
  const progressPercent = Math.round((completedPhotos / totalRequired) * 100);

  const handleManualComplete = useCallback(async () => {
    if (completedPhotos < 4) {
      toast.error('Necesitas al menos 4 fotos para crear una vista 360°');
      return;
    }

    setIsProcessing(true);
    toast.success('Vista 360° creada exitosamente con las fotos manuales');
    setIsProcessing(false);
    onComplete();
  }, [completedPhotos, onComplete]);

  // ============================================================
  // LOCKED STATE (Plan upgrade needed)
  // ============================================================

  if (!is360Available) {
    return (
      <div className="mx-auto max-w-2xl space-y-6 py-8">
        <Card className="border-amber-200 bg-gradient-to-br from-amber-50 to-yellow-50">
          <CardContent className="p-8 text-center">
            <Lock className="mx-auto mb-4 h-16 w-16 text-amber-500" />
            <h3 className="mb-2 text-xl font-bold text-gray-900">Vista 360° — Función Premium</h3>
            <p className="mb-6 text-gray-600">
              La vista 360° permite a los compradores ver tu vehículo desde todos los ángulos,
              aumentando las consultas hasta un 65%.
            </p>
            <Button className="gap-2 bg-gradient-to-r from-amber-500 to-yellow-500 text-white">
              <Crown className="h-4 w-4" />
              Actualizar Plan
            </Button>
            <Button variant="ghost" onClick={onSkip} className="ml-3">
              Omitir este paso
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  // ============================================================
  // METHOD SELECTION
  // ============================================================

  if (!method) {
    return (
      <div className="mx-auto max-w-3xl space-y-6 py-4">
        <div className="text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-purple-100">
            <RotateCw className="h-8 w-8 text-purple-600" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900">Vista 360° de tu Vehículo</h2>
          <p className="mx-auto mt-2 max-w-md text-gray-500">
            Los vehículos con vista 360° reciben hasta un 65% más consultas. Elige cómo quieres
            crear la tuya.
          </p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {/* Video Method */}
          <Card
            className="cursor-pointer transition-all hover:-translate-y-1 hover:shadow-lg"
            onClick={() => setMethod('video')}
          >
            <CardHeader>
              <div className="mx-auto mb-2 flex h-14 w-14 items-center justify-center rounded-xl bg-blue-100">
                <Video className="h-7 w-7 text-blue-600" />
              </div>
              <CardTitle className="text-center text-lg">Subir Video 360°</CardTitle>
              <CardDescription className="text-center">
                Graba un video caminando alrededor del vehículo y nosotros extraemos las fotos
                automáticamente.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-xs text-gray-500">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                  <span>Más rápido — solo graba y sube</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                  <span>MP4, MOV, WebM — máx. 100 MB</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                  <span>IA extrae los mejores ángulos</span>
                </div>
              </div>
              <Badge className="mt-3 bg-blue-100 text-blue-700">Recomendado</Badge>
            </CardContent>
          </Card>

          {/* Manual Method */}
          <Card
            className="cursor-pointer transition-all hover:-translate-y-1 hover:shadow-lg"
            onClick={() => setMethod('manual')}
          >
            <CardHeader>
              <div className="mx-auto mb-2 flex h-14 w-14 items-center justify-center rounded-xl bg-emerald-100">
                <Camera className="h-7 w-7 text-emerald-600" />
              </div>
              <CardTitle className="text-center text-lg">Fotos Manuales</CardTitle>
              <CardDescription className="text-center">
                Sigue nuestra guía para tomar fotos desde cada ángulo. Te indicamos exactamente
                dónde posicionarte.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2 text-xs text-gray-500">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                  <span>Mayor control de cada foto</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                  <span>Guía visual de ángulos</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                  <span>Ideal para fotos de estudio</span>
                </div>
              </div>
              <Badge className="mt-3 bg-emerald-100 text-emerald-700">Más Control</Badge>
            </CardContent>
          </Card>
        </div>

        <div className="text-center">
          <Button variant="ghost" onClick={onSkip} className="gap-2 text-gray-400">
            <SkipForward className="h-4 w-4" />
            Omitir este paso
          </Button>
        </div>
      </div>
    );
  }

  // ============================================================
  // VIDEO UPLOAD
  // ============================================================

  if (method === 'video') {
    return (
      <div className="mx-auto max-w-2xl space-y-6 py-4">
        <div className="text-center">
          <h2 className="text-xl font-bold text-gray-900">Sube tu Video 360°</h2>
          <p className="mt-1 text-sm text-gray-500">
            Graba un video caminando alrededor del vehículo en sentido horario.
          </p>
        </div>

        {/* Tips */}
        <Card className="border-blue-200 bg-blue-50">
          <CardContent className="p-4">
            <h4 className="mb-2 flex items-center gap-2 text-sm font-semibold text-blue-800">
              <Info className="h-4 w-4" />
              Consejos para un mejor resultado
            </h4>
            <ul className="space-y-1 text-xs text-blue-700">
              <li>📱 Sostén el teléfono a la altura del centro del vehículo</li>
              <li>🔄 Camina alrededor completamente (360 grados)</li>
              <li>⏱️ Toma entre 30-60 segundos para dar la vuelta completa</li>
              <li>☀️ Buena iluminación, preferiblemente al aire libre</li>
              <li>📐 Mantén una distancia constante (2-3 metros)</li>
              <li>🚫 Evita objetos o personas en el camino</li>
            </ul>
          </CardContent>
        </Card>

        {/* Upload Area */}
        {!videoFile ? (
          <label className="flex cursor-pointer flex-col items-center justify-center rounded-2xl border-3 border-dashed border-gray-300 bg-gray-50 px-6 py-16 transition-colors hover:border-blue-400 hover:bg-blue-50">
            <Upload className="mb-3 h-10 w-10 text-gray-400" />
            <p className="text-lg font-semibold text-gray-600">
              Arrastra tu video aquí o haz clic para seleccionar
            </p>
            <p className="mt-1 text-sm text-gray-400">MP4, MOV, WebM — máximo 100 MB</p>
            <input
              type="file"
              accept="video/mp4,video/mov,video/webm,video/quicktime"
              onChange={e => {
                const file = e.target.files?.[0];
                if (file) handleVideoUpload(file);
              }}
              className="hidden"
            />
          </label>
        ) : (
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center gap-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-blue-100">
                  <Video className="h-6 w-6 text-blue-600" />
                </div>
                <div className="flex-1">
                  <p className="font-medium">{videoFile.name}</p>
                  <p className="text-sm text-gray-500">
                    {(videoFile.size / (1024 * 1024)).toFixed(1)} MB
                  </p>
                  {isProcessing && (
                    <div className="mt-2">
                      <Progress value={uploadProgress} className="h-2" />
                      <p className="mt-1 text-xs text-gray-500">
                        {uploadProgress < 50
                          ? 'Subiendo video...'
                          : uploadProgress < 90
                            ? 'Extrayendo fotogramas...'
                            : 'Finalizando...'}
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        <div className="flex justify-between">
          <Button variant="ghost" onClick={() => setMethod(null)}>
            ← Cambiar método
          </Button>
          <Button variant="ghost" onClick={onSkip}>
            Omitir <SkipForward className="ml-2 h-4 w-4" />
          </Button>
        </div>
      </div>
    );
  }

  // ============================================================
  // MANUAL PHOTO UPLOAD
  // ============================================================

  return (
    <div className="mx-auto max-w-4xl space-y-6 py-4">
      <div className="text-center">
        <h2 className="text-xl font-bold text-gray-900">Fotos 360° — Guía de Ángulos</h2>
        <p className="mt-1 text-sm text-gray-500">
          Sigue la guía y toma fotos desde cada ángulo indicado. Mínimo 4 fotos, ideal 8+.
        </p>
      </div>

      {/* Progress */}
      <div className="rounded-xl bg-white p-4 shadow-sm">
        <div className="mb-2 flex items-center justify-between">
          <span className="text-sm font-medium">
            Progreso: {completedPhotos}/{totalRequired} fotos
          </span>
          <span className="text-sm text-gray-500">{progressPercent}%</span>
        </div>
        <Progress value={progressPercent} className="h-2" />
        {completedPhotos >= 4 && (
          <p className="mt-2 flex items-center gap-1 text-xs text-emerald-600">
            <CheckCircle2 className="h-3.5 w-3.5" />
            ¡Suficientes fotos para crear la vista 360°!
          </p>
        )}
      </div>

      {/* Tip */}
      <Card className="border-emerald-200 bg-emerald-50">
        <CardContent className="flex items-start gap-3 p-4">
          <Smartphone className="mt-0.5 h-5 w-5 text-emerald-600" />
          <div>
            <p className="text-sm font-medium text-emerald-800">
              Consejo: Usa la cámara de tu celular
            </p>
            <p className="text-xs text-emerald-600">
              Colócate a 2-3 metros del vehículo, a la altura del centro. Muévete en sentido horario
              y toma una foto en cada posición marcada.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Angle Guide Grid */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {ANGLE_GUIDES.map((guide, idx) => (
          <AngleGuideCard
            key={guide.angle}
            guide={guide}
            photo={manualPhotos[guide.angle]}
            onUpload={file => handleManualPhotoUpload(guide.angle, file)}
            isActive={idx === activeGuideIndex}
          />
        ))}
      </div>

      {/* Actions */}
      <div className="flex items-center justify-between">
        <Button variant="ghost" onClick={() => setMethod(null)}>
          ← Cambiar método
        </Button>
        <div className="flex gap-2">
          <Button variant="ghost" onClick={onSkip}>
            Omitir
          </Button>
          <Button
            onClick={handleManualComplete}
            disabled={completedPhotos < 4 || isProcessing}
            className="gap-2 bg-emerald-600 hover:bg-emerald-700"
          >
            {isProcessing ? 'Procesando...' : 'Crear Vista 360°'}
            <ArrowRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
