/**
 * Broken Images Alert Banner
 *
 * Visible alert shown to buyers when a vehicle listing has images
 * that are currently unreachable from the CDN. This is detected by
 * the publish-time URL verification or the periodic health scan job.
 *
 * Displayed prominently on the vehicle detail page when
 * vehicle.hasBrokenImages === true.
 */

'use client';

import { ImageOff } from 'lucide-react';

export function BrokenImagesAlert() {
  return (
    <div
      className="mb-4 rounded-xl border-2 border-blue-300 bg-blue-50 p-4 dark:border-blue-600 dark:bg-blue-950/30"
      role="status"
      aria-label="Fotos en actualización"
    >
      <div className="flex items-start gap-3">
        <div className="shrink-0 rounded-full bg-blue-200 p-2 dark:bg-blue-800">
          <ImageOff className="h-5 w-5 text-blue-700 dark:text-blue-300" />
        </div>
        <div className="flex-1">
          <h3 className="text-sm font-semibold text-blue-900 dark:text-blue-200">
            📸 Fotos en actualización
          </h3>
          <p className="mt-1 text-sm text-blue-800 dark:text-blue-300">
            Algunas fotos de este vehículo se están actualizando y podrían no mostrarse
            correctamente. El vendedor ha sido notificado y las imágenes se restaurarán pronto.
          </p>
          <p className="mt-2 text-xs text-blue-700 dark:text-blue-400">
            Si estás interesado en este vehículo, puedes contactar al vendedor directamente para
            solicitar fotos actualizadas.
          </p>
        </div>
      </div>
    </div>
  );
}
