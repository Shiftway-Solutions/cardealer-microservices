'use client';

import { ListingQualityScore } from './listing-quality-score';
import type { VehicleFormData } from './smart-publish-wizard';
import { escapeHtml } from '@/lib/security/sanitize';
import {
  Car,
  Calendar,
  Gauge,
  Fuel,
  Settings2,
  MapPin,
  Phone,
  Mail,
  Check,
  ArrowLeft,
  Send,
  Save,
  Star,
  Barcode,
  ArrowLeftRight,
  MessageCircle,
} from 'lucide-react';

// ============================================================
// Component
// ============================================================

interface ReviewStepProps {
  data: VehicleFormData;
  onPublish: () => void;
  onSaveDraft: () => void;
  onBack: () => void;
  isPublishing: boolean;
}

function formatPrice(amount: number, currency: 'DOP' | 'USD'): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Car;
  label: string;
  value?: string | number;
}) {
  if (!value) return null;
  return (
    <div className="flex items-center gap-3 py-2">
      <Icon className="h-4 w-4 flex-shrink-0 text-gray-400" />
      <span className="w-28 flex-shrink-0 text-sm text-gray-500">{label}</span>
      <span className="text-sm font-medium text-gray-900">{value}</span>
    </div>
  );
}

export function ReviewStep({
  data,
  onPublish,
  onSaveDraft,
  onBack,
  isPublishing,
}: ReviewStepProps) {
  const primaryImage = data.images.find(img => img.isPrimary) || data.images[0];

  // Validation checks
  const errors: string[] = [];
  if (!data.make) errors.push('Marca es requerida');
  if (!data.model) errors.push('Modelo es requerido');
  if (!data.year) errors.push('Año es requerido');
  if (!data.price) errors.push('Precio es requerido');
  if (data.images.length < 3) errors.push('Mínimo 3 fotos requeridas');
  if (!data.province) errors.push('Provincia es requerida');
  if (!data.condition) errors.push('Condición es requerida');
  if (!data.transmission) errors.push('Transmisión es requerida');
  if (!data.fuelType) errors.push('Tipo de combustible es requerido');

  const canPublish = errors.length === 0;

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h2 className="text-xl font-bold text-gray-900">Revisión Final</h2>
        <p className="mt-1 text-sm text-gray-500">
          Revisa que toda la información sea correcta antes de publicar
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* ── Main Content ── */}
        <div className="space-y-6 lg:col-span-2">
          {/* Vehicle Preview Card */}
          <div className="overflow-hidden rounded-xl border border-gray-200 bg-white">
            {/* Image Gallery Preview */}
            {data.images.length > 0 && (
              <div className="relative aspect-video bg-gray-100">
                {primaryImage && (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={primaryImage.url}
                    alt={`${data.year} ${data.make} ${data.model}`}
                    className="h-full w-full object-cover"
                  />
                )}
                <div className="absolute right-3 bottom-3 rounded-full bg-black/60 px-3 py-1 text-xs font-medium text-white">
                  {data.images.length} foto{data.images.length !== 1 ? 's' : ''}
                </div>
              </div>
            )}

            {/* Vehicle Info */}
            <div className="p-5">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="text-xl font-bold text-gray-900">
                    {data.year} {escapeHtml(data.make)} {escapeHtml(data.model)}
                  </h3>
                  {data.trim && <p className="text-sm text-gray-500">{escapeHtml(data.trim)}</p>}
                </div>
                <div className="text-right">
                  <p className="text-xl font-bold text-emerald-600">
                    {formatPrice(data.price, data.currency)}
                  </p>
                  <div className="mt-1 flex items-center gap-2">
                    {data.isNegotiable && <span className="text-xs text-gray-500">Negociable</span>}
                    {data.acceptsTrades && (
                      <span className="inline-flex items-center gap-0.5 text-xs text-gray-500">
                        <ArrowLeftRight className="h-3 w-3" /> Acepta cambios
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* VIN Badge */}
              {data.vin && (
                <div className="mt-3 flex items-center gap-1.5 rounded-lg bg-gray-50 px-3 py-2">
                  <Barcode className="h-4 w-4 text-gray-400" />
                  <span className="font-mono text-xs tracking-wider text-gray-600">{data.vin}</span>
                  <Check className="ml-auto h-3.5 w-3.5 text-emerald-500" />
                </div>
              )}

              {/* Specs Grid */}
              <div className="mt-4 divide-y divide-gray-100">
                <InfoRow
                  icon={Gauge}
                  label="Kilometraje"
                  value={
                    data.mileage
                      ? `${data.mileage.toLocaleString('es-DO')} ${data.mileageUnit}`
                      : undefined
                  }
                />
                <InfoRow icon={Settings2} label="Transmisión" value={data.transmission} />
                <InfoRow icon={Fuel} label="Combustible" value={data.fuelType} />
                <InfoRow icon={Car} label="Carrocería" value={data.bodyStyle} />
                <InfoRow icon={Car} label="Tracción" value={data.driveType} />
                <InfoRow icon={Car} label="Motor" value={data.engineSize} />
                <InfoRow icon={Calendar} label="Condición" value={data.condition} />
                <InfoRow icon={Car} label="Color Ext." value={data.exteriorColor} />
                <InfoRow icon={Car} label="Color Int." value={data.interiorColor} />
                <InfoRow
                  icon={MapPin}
                  label="Ubicación"
                  value={`${data.city ? data.city + ', ' : ''}${data.province}`}
                />
              </div>

              {/* Features */}
              {data.features.length > 0 && (
                <div className="mt-4">
                  <p className="mb-2 text-xs font-semibold tracking-wider text-gray-500 uppercase">
                    Características ({data.features.length})
                  </p>
                  <div className="flex flex-wrap gap-1.5">
                    {data.features.map(feature => (
                      <span
                        key={feature}
                        className="inline-flex items-center gap-1 rounded-full bg-gray-100 px-2.5 py-1 text-xs text-gray-700"
                      >
                        <Check className="h-3 w-3 text-emerald-500" />
                        {escapeHtml(feature)}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Description */}
              {data.description && (
                <div className="mt-4">
                  <p className="mb-2 text-xs font-semibold tracking-wider text-gray-500 uppercase">
                    Descripción
                  </p>
                  <p className="text-sm leading-relaxed whitespace-pre-line text-gray-700">
                    {escapeHtml(data.description)}
                  </p>
                </div>
              )}

              {/* Contact Info */}
              {(data.sellerName || data.sellerPhone || data.sellerEmail) && (
                <div className="mt-4 rounded-lg bg-gray-50 p-3">
                  <p className="mb-2 text-xs font-semibold tracking-wider text-gray-500 uppercase">
                    Contacto
                  </p>
                  <div className="space-y-1.5">
                    {data.sellerName && (
                      <p className="flex items-center gap-2 text-sm text-gray-700">
                        <Star className="h-3.5 w-3.5 text-gray-400" />
                        {escapeHtml(data.sellerName)}
                      </p>
                    )}
                    {data.sellerPhone && (
                      <p className="flex items-center gap-2 text-sm text-gray-700">
                        <Phone className="h-3.5 w-3.5 text-gray-400" />
                        {data.sellerPhone}
                      </p>
                    )}
                    {data.sellerEmail && (
                      <p className="flex items-center gap-2 text-sm text-gray-700">
                        <Mail className="h-3.5 w-3.5 text-gray-400" />
                        {data.sellerEmail}
                      </p>
                    )}
                    {data.sellerWhatsApp && (
                      <p className="flex items-center gap-2 text-sm text-gray-700">
                        <MessageCircle className="h-3.5 w-3.5 text-gray-400" />
                        {data.sellerWhatsApp}
                      </p>
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ── Sidebar ── */}
        <div className="space-y-4">
          {/* Quality Score */}
          <ListingQualityScore data={data} />

          {/* Validation Errors */}
          {errors.length > 0 && (
            <div className="rounded-xl border border-red-200 bg-red-50 p-4">
              <p className="mb-2 text-sm font-semibold text-red-800">
                Campos requeridos faltantes:
              </p>
              <ul className="space-y-1">
                {errors.map(err => (
                  <li key={err} className="flex items-center gap-2 text-sm text-red-700">
                    <span className="h-1 w-1 rounded-full bg-red-500" />
                    {err}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Actions */}
          <div className="space-y-3">
            <div className="rounded-lg border border-blue-200 bg-blue-50 p-3 text-xs text-blue-700">
              <p className="font-medium">📋 Revisión antes de publicar</p>
              <p className="mt-1">
                Tu anuncio será revisado por nuestro equipo antes de ser visible al público.
                Recibirás una notificación cuando sea aprobado (generalmente en menos de 24 horas).
              </p>
            </div>
            <button
              onClick={onPublish}
              disabled={!canPublish || isPublishing}
              className="flex w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-3.5 text-sm font-semibold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isPublishing ? (
                <>
                  <div className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Enviando a revisión...
                </>
              ) : (
                <>
                  <Send className="h-4 w-4" />
                  Enviar a Revisión
                </>
              )}
            </button>
            <button
              onClick={onSaveDraft}
              disabled={isPublishing}
              className="flex w-full items-center justify-center gap-2 rounded-xl border-2 border-gray-300 bg-white px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              <Save className="h-4 w-4" />
              Guardar como Borrador
            </button>
            <button
              onClick={onBack}
              disabled={isPublishing}
              className="flex w-full items-center justify-center gap-2 text-sm text-gray-500 hover:text-gray-700"
            >
              <ArrowLeft className="h-4 w-4" />
              Volver a editar
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
