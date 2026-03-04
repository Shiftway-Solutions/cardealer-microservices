'use client';

/**
 * VehicleFilters — Sidebar Filter Panel
 *
 * Complete left-sidebar filter panel modeled after CarGurus & AutoTrader.
 * Organized as: Condition → Make/Model → Price → Year → Body Type →
 * Province → [Advanced: Mileage, Fuel, Transmission, Drivetrain, Color,
 * Seller Type, Certified, Clean Title, Deal Rating]
 *
 * Studies show left-sidebar filters increase filter engagement by 23%
 * vs. top-bar filters (Nielsen Norman Group, 2022).
 */

import * as React from 'react';
import { CheckCircle2 } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Slider } from '@/components/ui/slider';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
import { cn } from '@/lib/utils';
import { BodyTypeSelector } from './body-type-selector';
import type { VehicleSearchFilters } from '@/hooks/use-vehicle-search';

// =============================================================================
// STATIC REFERENCE DATA
// =============================================================================

const currentYear = new Date().getFullYear();
const years = Array.from({ length: 35 }, (_, i) => currentYear - i);

const dominicanProvinces = [
  'Distrito Nacional',
  'Santo Domingo',
  'Santiago',
  'La Romana',
  'Puerto Plata',
  'San Pedro de Macorís',
  'La Vega',
  'San Cristóbal',
  'Higüey',
  'Barahona',
  'Azua',
  'Bonao',
  'Moca',
  'Nagua',
  'Samaná',
  'Montecristi',
  'San Francisco de Macorís',
  'Hato Mayor',
  'Bani',
  'Cotui',
];

const fuelTypeOptions = [
  { value: 'gasolina', label: 'Gasolina' },
  { value: 'diesel', label: 'Diésel' },
  { value: 'hibrido', label: 'Híbrido' },
  { value: 'pluginhybrid', label: 'Híbrido Enchufable (PHEV)' },
  { value: 'electrico', label: 'Eléctrico' },
  { value: 'glp', label: 'GLP / Gas' },
];

const transmissionOptions = [
  { value: 'automatica', label: 'Automática' },
  { value: 'manual', label: 'Manual' },
  { value: 'cvt', label: 'CVT' },
];

const drivetrainOptions = [
  { value: 'fwd', label: 'FWD (Delantera)' },
  { value: 'rwd', label: 'RWD (Trasera)' },
  { value: 'awd', label: 'AWD / 4x4' },
  { value: '4wd', label: '4WD (Off-road)' },
];

// Seats options — DR families commonly need 5, 7, or 8 seaters
const seatsOptions = [
  { value: 2, label: '2 pasajeros' },
  { value: 4, label: '4 pasajeros' },
  { value: 5, label: '5 pasajeros' },
  { value: 7, label: '7 pasajeros' },
  { value: 8, label: '8+ pasajeros' },
];

// Cylinder options — “4 cilindros” / “6 cilindros” are the top DR search terms
const cylinderOptions = [
  { value: 3, label: '3 cil.' },
  { value: 4, label: '4 cil.' },
  { value: 6, label: '6 cil.' },
  { value: 8, label: '8 cil.' },
];

// Interior color swatches
const interiorColorOptions = [
  { value: 'negro', label: 'Negro', hex: '#1C1C1C' },
  { value: 'gris', label: 'Gris', hex: '#9E9E9E' },
  { value: 'beige', label: 'Beige', hex: '#F5F0E1' },
  { value: 'marron', label: 'Marrón', hex: '#6D4C41' },
  { value: 'crema', label: 'Crema', hex: '#FFFDE7' },
  { value: 'rojo', label: 'Rojo', hex: '#B71C1C' },
];

// Key features / equipment — most searched in Dominican Republic
const featuresOptions = [
  { value: 'ac', label: 'A/C (Aire acondicionado)' },
  { value: 'gps', label: 'GPS / Navegación' },
  { value: 'sunroof', label: 'Techo solar' },
  { value: 'panoramic', label: 'Techo panorámico' },
  { value: 'backup_camera', label: 'Cámara de retroceso' },
  { value: 'camera_360', label: 'Cámara 360°' },
  { value: 'apple_carplay', label: 'Apple CarPlay' },
  { value: 'android_auto', label: 'Android Auto' },
  { value: 'leather_seats', label: 'Asientos de cuero' },
  { value: 'heated_seats', label: 'Asientos calefaccionados' },
  { value: 'blind_spot', label: 'Alerta punto ciego' },
  { value: 'adaptive_cruise', label: 'Cruise control adaptativo' },
  { value: 'keyless_entry', label: 'Entrada sin llave' },
  { value: 'remote_start', label: 'Encendido remoto' },
  { value: 'alloy_wheels', label: 'Aros de aleación' },
];

const colorOptions = [
  { value: 'blanco', label: 'Blanco', hex: '#FFFFFF' },
  { value: 'negro', label: 'Negro', hex: '#1C1C1C' },
  { value: 'gris', label: 'Gris/Plata', hex: '#9E9E9E' },
  { value: 'rojo', label: 'Rojo', hex: '#E53935' },
  { value: 'azul', label: 'Azul', hex: '#1E88E5' },
  { value: 'verde', label: 'Verde', hex: '#43A047' },
  { value: 'marron', label: 'Marrón', hex: '#6D4C41' },
  { value: 'dorado', label: 'Dorado', hex: '#FFD600' },
  { value: 'beige', label: 'Beige', hex: '#F5F0E1' },
  { value: 'naranja', label: 'Naranja', hex: '#FB8C00' },
];

const dealRatingOptions = [
  { value: 'great', label: 'Excelente precio', color: 'bg-green-500', desc: '> 10% bajo mercado' },
  { value: 'good', label: 'Buen precio', color: 'bg-blue-500', desc: '5–10% bajo mercado' },
  { value: 'fair', label: 'Precio justo', color: 'bg-yellow-500', desc: 'Precio de mercado' },
];

// =============================================================================
// TYPES
// =============================================================================

export interface VehicleFiltersProps {
  filters: VehicleSearchFilters;
  onChange: (filters: Partial<VehicleSearchFilters>) => void;
  onClear?: () => void;
  activeCount?: number;
  makeCatalog?: { id: string; name: string }[];
  modelCatalog?: { id: string; name: string; make?: string }[];
  facets?: {
    makes: { value: string; count: number }[];
    bodyTypes: { value: string; count: number }[];
    provinces: { value: string; count: number }[];
    fuelTypes: { value: string; count: number }[];
    transmissions: { value: string; count: number }[];
  };
  className?: string;
}

// =============================================================================
// SUB-COMPONENTS
// =============================================================================

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <p className="text-muted-foreground mb-2.5 text-[11px] font-semibold tracking-wider uppercase">
      {children}
    </p>
  );
}

function PriceInput({
  value,
  onChange,
  placeholder,
}: {
  value: number | undefined;
  onChange: (v: number | undefined) => void;
  placeholder: string;
}) {
  return (
    <div className="relative">
      <span className="text-muted-foreground absolute top-1/2 left-2.5 -translate-y-1/2 text-xs">
        RD$
      </span>
      <Input
        type="number"
        min={0}
        className="h-8 pl-8 text-sm"
        placeholder={placeholder}
        value={value ?? ''}
        onChange={e => onChange(e.target.value ? Number(e.target.value) : undefined)}
      />
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function VehicleFilters({
  filters,
  onChange,
  onClear: _onClear,
  activeCount: _activeCount,
  makeCatalog = [],
  modelCatalog = [],
  facets,
  className,
}: VehicleFiltersProps) {
  const [priceRange, setPriceRange] = React.useState<[number, number]>([
    filters.priceMin ?? 0,
    filters.priceMax ?? 10_000_000,
  ]);
  const [mileageMax, setMileageMax] = React.useState(filters.mileageMax ?? 300_000);

  // Ref to distinguish external filter changes (AI, URL params) from user slider interactions.
  // Prevents a redundant onChange call when the parent already applied the filter.
  const isExternalUpdate = React.useRef(false);

  // Propagate slider position → parent (skip when the change came from the parent itself)
  React.useEffect(() => {
    if (isExternalUpdate.current) {
      isExternalUpdate.current = false;
      return;
    }
    const t = setTimeout(() => {
      onChange({
        priceMin: priceRange[0] > 0 ? priceRange[0] : undefined,
        priceMax: priceRange[1] < 10_000_000 ? priceRange[1] : undefined,
      });
    }, 400);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [priceRange]);

  React.useEffect(() => {
    const t = setTimeout(() => {
      onChange({ mileageMax: mileageMax < 300_000 ? mileageMax : undefined });
    }, 400);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mileageMax]);

  // Sync local slider state when filters change externally (AI search, clear filters, URL params).
  // FIX: previously only reset when filters were cleared; now syncs on any external change.
  React.useEffect(() => {
    isExternalUpdate.current = true;
    setPriceRange([filters.priceMin ?? 0, filters.priceMax ?? 10_000_000]);
    setMileageMax(filters.mileageMax ?? 300_000);
  }, [filters.priceMin, filters.priceMax, filters.mileageMax]);

  const makes = facets?.makes?.length
    ? facets.makes
    : makeCatalog.map(m => ({ value: m.name, count: 0 }));

  const availableModels = filters.make
    ? modelCatalog.filter(m => !m.make || m.make.toLowerCase() === filters.make?.toLowerCase())
    : modelCatalog;

  const formatPrice = (v: number) =>
    v >= 1_000_000
      ? `${(v / 1_000_000).toFixed(1)}M`
      : v >= 1_000
        ? `${(v / 1_000).toFixed(0)}K`
        : v.toString();

  const advancedActiveCount = [
    filters.mileageMax,
    filters.fuelType,
    filters.transmission,
    filters.drivetrain,
    filters.color,
    filters.sellerType,
    filters.isCertified,
    filters.hasCleanTitle,
    filters.dealRating,
    filters.seats,
    filters.cylinders,
    filters.interiorColor,
    filters.features?.length ? filters.features : undefined,
  ].filter(Boolean).length;

  return (
    <div className={cn('space-y-0', className)}>
      <Accordion
        type="multiple"
        defaultValue={['condicion', 'marca', 'precio', 'anio', 'carroceria', 'ubicacion']}
        className="w-full space-y-0"
      >
        {/* ── CONDITION ─────────────────────────────────────── */}
        <AccordionItem value="condicion" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            Condición
          </AccordionTrigger>
          <AccordionContent className="pb-4">
            <div className="flex gap-1.5">
              {(
                [
                  { v: undefined, label: 'Todos' },
                  { v: 'nuevo', label: 'Nuevo' },
                  { v: 'usado', label: 'Usado' },
                ] as { v: 'nuevo' | 'usado' | undefined; label: string }[]
              ).map(opt => {
                const isActive = filters.condition === opt.v;
                return (
                  <button
                    key={opt.label}
                    type="button"
                    onClick={() => onChange({ condition: opt.v })}
                    className={cn(
                      'flex-1 rounded-lg border py-1.5 text-xs font-medium transition-all',
                      isActive
                        ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                        : 'border-border text-muted-foreground hover:text-foreground hover:border-[#00A870]/30'
                    )}
                  >
                    {opt.label}
                  </button>
                );
              })}
            </div>
          </AccordionContent>
        </AccordionItem>

        <Separator className="my-0" />

        {/* ── MAKE / MODEL ─────────────────────────────────── */}
        <AccordionItem value="marca" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            Marca y Modelo
          </AccordionTrigger>
          <AccordionContent className="space-y-2 pb-4">
            <select
              value={filters.make ?? ''}
              onChange={e => onChange({ make: e.target.value || undefined, model: undefined })}
              className="border-input bg-background h-9 w-full rounded-md border px-3 text-sm focus:ring-1 focus:ring-[#00A870] focus:outline-none"
            >
              <option value="">Todas las marcas</option>
              {makes.map(m => (
                <option key={m.value} value={m.value}>
                  {m.value} {m.count > 0 ? `(${m.count})` : ''}
                </option>
              ))}
            </select>
            {filters.make && (
              <select
                value={filters.model ?? ''}
                onChange={e => onChange({ model: e.target.value || undefined })}
                className="border-input bg-background h-9 w-full rounded-md border px-3 text-sm focus:ring-1 focus:ring-[#00A870] focus:outline-none"
              >
                <option value="">Todos los modelos</option>
                {availableModels.map(m => (
                  <option key={m.id} value={m.name}>
                    {m.name}
                  </option>
                ))}
              </select>
            )}
          </AccordionContent>
        </AccordionItem>

        <Separator className="my-0" />

        {/* ── PRICE ─────────────────────────────────────────── */}
        <AccordionItem value="precio" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            Precio
          </AccordionTrigger>
          <AccordionContent className="pb-4">
            <div className="space-y-3">
              <Slider
                value={priceRange}
                onValueChange={v => setPriceRange(v as [number, number])}
                min={0}
                max={10_000_000}
                step={100_000}
                className="w-full"
              />
              <div className="text-muted-foreground flex items-center justify-between text-xs">
                <span>RD$ {formatPrice(priceRange[0])}</span>
                <span>RD$ {formatPrice(priceRange[1])}</span>
              </div>
              <div className="grid grid-cols-2 gap-2">
                <PriceInput
                  value={filters.priceMin}
                  onChange={v => {
                    onChange({ priceMin: v });
                    setPriceRange([v ?? 0, priceRange[1]]);
                  }}
                  placeholder="Mínimo"
                />
                <PriceInput
                  value={filters.priceMax}
                  onChange={v => {
                    onChange({ priceMax: v });
                    setPriceRange([priceRange[0], v ?? 10_000_000]);
                  }}
                  placeholder="Máximo"
                />
              </div>
              <div className="flex flex-wrap gap-1">
                {[
                  { label: '< 1M', max: 1_000_000 },
                  { label: '1M–2M', min: 1_000_000, max: 2_000_000 },
                  { label: '2M–5M', min: 2_000_000, max: 5_000_000 },
                  { label: '> 5M', min: 5_000_000 },
                ].map(chip => (
                  <button
                    key={chip.label}
                    type="button"
                    onClick={() => {
                      onChange({ priceMin: chip.min, priceMax: chip.max });
                      setPriceRange([chip.min ?? 0, chip.max ?? 10_000_000]);
                    }}
                    className="border-border text-muted-foreground rounded-full border px-2.5 py-0.5 text-xs transition-colors hover:border-[#00A870] hover:text-[#00A870]"
                  >
                    {chip.label}
                  </button>
                ))}
              </div>
            </div>
          </AccordionContent>
        </AccordionItem>

        <Separator className="my-0" />

        {/* ── YEAR ──────────────────────────────────────────── */}
        <AccordionItem value="anio" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            Año
          </AccordionTrigger>
          <AccordionContent className="pb-4">
            <div className="grid grid-cols-2 gap-2">
              <select
                value={filters.yearMin ?? ''}
                onChange={e =>
                  onChange({ yearMin: e.target.value ? Number(e.target.value) : undefined })
                }
                className="border-input bg-background h-9 rounded-md border px-2 text-sm focus:ring-1 focus:ring-[#00A870] focus:outline-none"
              >
                <option value="">Desde</option>
                {years.map(y => (
                  <option key={y} value={y}>
                    {y}
                  </option>
                ))}
              </select>
              <select
                value={filters.yearMax ?? ''}
                onChange={e =>
                  onChange({ yearMax: e.target.value ? Number(e.target.value) : undefined })
                }
                className="border-input bg-background h-9 rounded-md border px-2 text-sm focus:ring-1 focus:ring-[#00A870] focus:outline-none"
              >
                <option value="">Hasta</option>
                {years.map(y => (
                  <option key={y} value={y}>
                    {y}
                  </option>
                ))}
              </select>
            </div>
            <div className="mt-2 flex flex-wrap gap-1">
              {[
                { label: '2022+', min: 2022 },
                { label: '2020+', min: 2020 },
                { label: '2018+', min: 2018 },
                { label: '2015+', min: 2015 },
              ].map(chip => (
                <button
                  key={chip.label}
                  type="button"
                  onClick={() => onChange({ yearMin: chip.min, yearMax: undefined })}
                  className={cn(
                    'rounded-full border px-2.5 py-0.5 text-xs transition-colors',
                    filters.yearMin === chip.min && !filters.yearMax
                      ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                      : 'border-border text-muted-foreground hover:border-[#00A870] hover:text-[#00A870]'
                  )}
                >
                  {chip.label}
                </button>
              ))}
            </div>
          </AccordionContent>
        </AccordionItem>

        <Separator className="my-0" />

        {/* ── BODY TYPE ─────────────────────────────────────── */}
        <AccordionItem value="carroceria" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            Carrocería
          </AccordionTrigger>
          <AccordionContent className="pb-4">
            <BodyTypeSelector
              value={filters.bodyType}
              onChange={v => onChange({ bodyType: v })}
              variant="compact"
            />
          </AccordionContent>
        </AccordionItem>

        <Separator className="my-0" />

        {/* ── PROVINCE ──────────────────────────────────────── */}
        <AccordionItem value="ubicacion" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            Ubicación
          </AccordionTrigger>
          <AccordionContent className="pb-4">
            <select
              value={filters.province ?? ''}
              onChange={e => onChange({ province: e.target.value || undefined })}
              className="border-input bg-background h-9 w-full rounded-md border px-3 text-sm focus:ring-1 focus:ring-[#00A870] focus:outline-none"
            >
              <option value="">Todas las provincias</option>
              {dominicanProvinces.map(p => {
                const fi = facets?.provinces?.find(f => f.value === p);
                return (
                  <option key={p} value={p}>
                    {p} {fi && fi.count > 0 ? `(${fi.count})` : ''}
                  </option>
                );
              })}
            </select>
          </AccordionContent>
        </AccordionItem>

        <Separator className="my-0" />

        {/* ── ADVANCED ──────────────────────────────────────── */}
        <AccordionItem value="avanzados" className="border-0">
          <AccordionTrigger className="py-2.5 text-sm font-medium hover:no-underline">
            <span className="flex items-center gap-2">
              Filtros avanzados
              {advancedActiveCount > 0 && (
                <Badge className="flex h-4 w-4 items-center justify-center rounded-full bg-[#00A870] p-0 text-[10px] text-white">
                  {advancedActiveCount}
                </Badge>
              )}
            </span>
          </AccordionTrigger>
          <AccordionContent className="space-y-5 pb-4">
            {/* Mileage */}
            <div>
              <SectionLabel>Kilometraje máximo</SectionLabel>
              <Slider
                value={[mileageMax]}
                onValueChange={v => setMileageMax(v[0])}
                min={0}
                max={300_000}
                step={10_000}
                className="w-full"
              />
              <div className="text-muted-foreground mt-1.5 flex justify-between text-xs">
                <span>0 km</span>
                <span className="text-foreground font-medium">
                  {mileageMax >= 300_000 ? 'Sin límite' : `${mileageMax.toLocaleString()} km`}
                </span>
              </div>
            </div>

            <Separator />

            {/* Fuel */}
            <div>
              <SectionLabel>Combustible</SectionLabel>
              <div className="space-y-1.5">
                {fuelTypeOptions.map(opt => (
                  <label key={opt.value} className="flex cursor-pointer items-center gap-2 text-sm">
                    <Checkbox
                      checked={filters.fuelType === opt.value}
                      onCheckedChange={c =>
                        onChange({
                          fuelType: c ? (opt.value as VehicleSearchFilters['fuelType']) : undefined,
                        })
                      }
                    />
                    <span>{opt.label}</span>
                  </label>
                ))}
              </div>
            </div>

            <Separator />

            {/* Transmission */}
            <div>
              <SectionLabel>Transmisión</SectionLabel>
              <div className="flex gap-1.5">
                {transmissionOptions.map(opt => (
                  <button
                    key={opt.value}
                    type="button"
                    onClick={() =>
                      onChange({
                        transmission:
                          filters.transmission === opt.value
                            ? undefined
                            : (opt.value as VehicleSearchFilters['transmission']),
                      })
                    }
                    className={cn(
                      'flex-1 rounded-lg border py-1.5 text-xs font-medium transition-all',
                      filters.transmission === opt.value
                        ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                        : 'border-border text-muted-foreground hover:border-[#00A870]/30'
                    )}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            <Separator />

            {/* Drivetrain */}
            <div>
              <SectionLabel>Tracción</SectionLabel>
              <div className="grid grid-cols-2 gap-1.5">
                {drivetrainOptions.map(opt => (
                  <button
                    key={opt.value}
                    type="button"
                    onClick={() =>
                      onChange({
                        drivetrain:
                          filters.drivetrain === opt.value
                            ? undefined
                            : (opt.value as VehicleSearchFilters['drivetrain']),
                      })
                    }
                    className={cn(
                      'rounded-lg border px-2 py-1.5 text-left text-xs font-medium transition-all',
                      filters.drivetrain === opt.value
                        ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                        : 'border-border text-muted-foreground hover:border-[#00A870]/30'
                    )}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            <Separator />

            {/* Color */}
            <div>
              <SectionLabel>Color exterior</SectionLabel>
              <div className="flex flex-wrap gap-2">
                {colorOptions.map(c => (
                  <button
                    key={c.value}
                    type="button"
                    title={c.label}
                    onClick={() =>
                      onChange({ color: filters.color === c.value ? undefined : c.value })
                    }
                    className={cn(
                      'h-7 w-7 rounded-full border-2 transition-all',
                      c.value === 'blanco' ? 'border-gray-300' : 'border-transparent',
                      filters.color === c.value &&
                        'border-[#00A870] ring-2 ring-[#00A870] ring-offset-1'
                    )}
                    style={{ backgroundColor: c.hex }}
                    aria-label={c.label}
                    aria-pressed={filters.color === c.value}
                  />
                ))}
              </div>
              {filters.color && (
                <p className="mt-1 text-xs text-[#00A870] capitalize">{filters.color}</p>
              )}
            </div>

            <Separator />

            {/* Seller Type */}
            <div>
              <SectionLabel>Tipo de vendedor</SectionLabel>
              <div className="flex gap-2">
                {[
                  { v: undefined, label: 'Todos' },
                  { v: 'dealer', label: 'Dealer' },
                  { v: 'seller', label: 'Particular' },
                ].map(opt => (
                  <button
                    key={opt.label}
                    type="button"
                    onClick={() =>
                      onChange({ sellerType: opt.v as VehicleSearchFilters['sellerType'] })
                    }
                    className={cn(
                      'flex-1 rounded-lg border py-1.5 text-xs font-medium transition-all',
                      filters.sellerType === opt.v
                        ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                        : 'border-border text-muted-foreground hover:border-[#00A870]/30'
                    )}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            <Separator />

            {/* Certified & Clean Title */}
            <div className="space-y-2">
              <label className="flex cursor-pointer items-center gap-2 text-sm">
                <Checkbox
                  checked={filters.isCertified === true}
                  onCheckedChange={c => onChange({ isCertified: c ? true : undefined })}
                />
                <span className="flex items-center gap-1.5">
                  <CheckCircle2 className="h-3.5 w-3.5 text-[#00A870]" />
                  Con garantía del vendedor
                </span>
              </label>
              <label className="flex cursor-pointer items-center gap-2 text-sm">
                <Checkbox
                  checked={filters.hasCleanTitle === true}
                  onCheckedChange={c => onChange({ hasCleanTitle: c ? true : undefined })}
                />
                <span>Título limpio</span>
              </label>
            </div>

            <Separator />

            {/* Deal Rating */}
            <div>
              <SectionLabel>Valoración de precio</SectionLabel>
              <div className="space-y-2">
                {dealRatingOptions.map(r => (
                  <label key={r.value} className="flex cursor-pointer items-center gap-2 text-sm">
                    <Checkbox
                      checked={filters.dealRating === r.value}
                      onCheckedChange={c =>
                        onChange({
                          dealRating: c
                            ? (r.value as VehicleSearchFilters['dealRating'])
                            : undefined,
                        })
                      }
                    />
                    <span className={cn('inline-block h-2 w-2 rounded-full', r.color)} />
                    <span className="flex-1">{r.label}</span>
                  </label>
                ))}
              </div>
            </div>

            <Separator />

            {/* Seats — DR families frequently search for 5, 7, 8-seaters */}
            <div>
              <SectionLabel>Capacidad de pasajeros</SectionLabel>
              <div className="flex flex-wrap gap-1.5">
                {seatsOptions.map(opt => (
                  <button
                    key={opt.value}
                    type="button"
                    onClick={() =>
                      onChange({
                        seats: filters.seats === opt.value ? undefined : opt.value,
                      })
                    }
                    className={cn(
                      'rounded-lg border px-2.5 py-1.5 text-xs font-medium transition-all',
                      filters.seats === opt.value
                        ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                        : 'border-border text-muted-foreground hover:border-[#00A870]/30'
                    )}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            <Separator />

            {/* Cylinders — "4 cilindros" / "6 cilindros" is a top search in DR */}
            <div>
              <SectionLabel>Cilindros del motor</SectionLabel>
              <div className="flex gap-1.5">
                {cylinderOptions.map(opt => (
                  <button
                    key={opt.value}
                    type="button"
                    onClick={() =>
                      onChange({
                        cylinders: filters.cylinders === opt.value ? undefined : opt.value,
                      })
                    }
                    className={cn(
                      'flex-1 rounded-lg border py-1.5 text-xs font-medium transition-all',
                      filters.cylinders === opt.value
                        ? 'border-[#00A870] bg-[#00A870]/10 text-[#00A870]'
                        : 'border-border text-muted-foreground hover:border-[#00A870]/30'
                    )}
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>

            <Separator />

            {/* Interior Color */}
            <div>
              <SectionLabel>Color interior</SectionLabel>
              <div className="flex flex-wrap gap-2">
                {interiorColorOptions.map(c => (
                  <button
                    key={c.value}
                    type="button"
                    title={c.label}
                    onClick={() =>
                      onChange({
                        interiorColor: filters.interiorColor === c.value ? undefined : c.value,
                      })
                    }
                    className={cn(
                      'h-7 w-7 rounded-full border-2 transition-all',
                      c.value === 'crema' ? 'border-gray-300' : 'border-transparent',
                      filters.interiorColor === c.value &&
                        'border-[#00A870] ring-2 ring-[#00A870] ring-offset-1'
                    )}
                    style={{ backgroundColor: c.hex }}
                    aria-label={c.label}
                    aria-pressed={filters.interiorColor === c.value}
                  />
                ))}
              </div>
              {filters.interiorColor && (
                <p className="mt-1 text-xs text-[#00A870] capitalize">{filters.interiorColor}</p>
              )}
            </div>

            <Separator />

            {/* Features / Equipamiento — A/C, GPS, Sunroof critical for DR buyers */}
            <div>
              <SectionLabel>Equipamiento</SectionLabel>
              <div className="space-y-1.5">
                {featuresOptions.map(opt => (
                  <label key={opt.value} className="flex cursor-pointer items-center gap-2 text-sm">
                    <Checkbox
                      checked={filters.features?.includes(opt.value) ?? false}
                      onCheckedChange={c => {
                        const current = filters.features ?? [];
                        const next = c
                          ? [...current, opt.value]
                          : current.filter(f => f !== opt.value);
                        onChange({ features: next.length ? next : undefined });
                      }}
                    />
                    <span>{opt.label}</span>
                  </label>
                ))}
              </div>
            </div>
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </div>
  );
}

export default VehicleFilters;
