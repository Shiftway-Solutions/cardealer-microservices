/**
 * Vehicle Tabs Component
 * Tabs for Description, Specifications, Features
 */

'use client';

import * as React from 'react';
import {
  Fuel,
  Gauge,
  Settings,
  Calendar,
  Palette,
  Users,
  DoorOpen,
  Cog,
  Check,
} from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn, formatNumber } from '@/lib/utils';
import type { Vehicle } from '@/types';

interface VehicleTabsProps {
  vehicle: Vehicle;
  className?: string;
}

export function VehicleTabs({ vehicle, className }: VehicleTabsProps) {
  return (
    <div
      className={cn(
        'border-border overflow-hidden rounded-2xl border bg-white shadow-sm dark:bg-slate-900',
        className
      )}
    >
      <Tabs defaultValue="description" className="w-full">
        <TabsList className="border-border h-auto w-full justify-start rounded-none border-b bg-transparent p-0">
          <TabsTrigger
            value="description"
            className="rounded-none border-b-2 border-transparent px-6 py-3 data-[state=active]:border-[#00A870] data-[state=active]:bg-transparent data-[state=active]:text-[#00A870] data-[state=active]:shadow-none"
          >
            Descripción
          </TabsTrigger>
          <TabsTrigger
            value="specs"
            className="rounded-none border-b-2 border-transparent px-6 py-3 data-[state=active]:border-[#00A870] data-[state=active]:bg-transparent data-[state=active]:text-[#00A870] data-[state=active]:shadow-none"
          >
            Especificaciones
          </TabsTrigger>
          <TabsTrigger
            value="features"
            className="rounded-none border-b-2 border-transparent px-6 py-3 data-[state=active]:border-[#00A870] data-[state=active]:bg-transparent data-[state=active]:text-[#00A870] data-[state=active]:shadow-none"
          >
            Características
          </TabsTrigger>
        </TabsList>

        {/* Description Tab */}
        <TabsContent value="description" className="m-0 p-6">
          <DescriptionTab vehicle={vehicle} />
        </TabsContent>

        {/* Specifications Tab */}
        <TabsContent value="specs" className="m-0 p-6">
          <SpecificationsTab vehicle={vehicle} />
        </TabsContent>

        {/* Features Tab */}
        <TabsContent value="features" className="m-0 p-6">
          <FeaturesTab vehicle={vehicle} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

// Description Tab Content
function DescriptionTab({ vehicle }: { vehicle: Vehicle }) {
  // Mock description if not provided
  const description =
    (vehicle as { description?: string }).description ||
    `
    Este ${vehicle.year} ${vehicle.make} ${vehicle.model} ${vehicle.trim || ''} está en excelentes condiciones.
    
    Con ${formatNumber(vehicle.mileage)} kilómetros recorridos, este vehículo ha sido bien mantenido y está listo para un nuevo dueño.
    
    Cuenta con transmisión ${vehicle.transmission === 'automatic' ? 'automática' : 'manual'} y motor de ${vehicle.fuelType === 'gasoline' ? 'gasolina' : vehicle.fuelType === 'diesel' ? 'diesel' : vehicle.fuelType === 'hybrid' ? 'híbrido' : 'eléctrico'}.
    
    El vehículo se encuentra ubicado en ${vehicle.location.city}, ${vehicle.location.province}.
  `;

  return (
    <div className="prose prose-gray max-w-none">
      <h3 className="mb-4 text-lg font-semibold">Acerca de este vehículo</h3>
      <div className="text-muted-foreground whitespace-pre-line">{description.trim()}</div>
    </div>
  );
}

// Specifications Tab Content
function SpecificationsTab({ vehicle }: { vehicle: Vehicle }) {
  const specs = [
    {
      icon: Calendar,
      label: 'Año',
      value: vehicle.year.toString(),
    },
    {
      icon: Gauge,
      label: 'Kilometraje',
      value: `${formatNumber(vehicle.mileage)} km`,
    },
    {
      icon: Settings,
      label: 'Transmisión',
      value:
        vehicle.transmission === 'automatic'
          ? 'Automática'
          : vehicle.transmission === 'manual'
            ? 'Manual'
            : 'CVT',
    },
    {
      icon: Fuel,
      label: 'Combustible',
      value:
        vehicle.fuelType === 'gasoline'
          ? 'Gasolina'
          : vehicle.fuelType === 'diesel'
            ? 'Diesel'
            : vehicle.fuelType === 'hybrid'
              ? 'Híbrido'
              : 'Eléctrico',
    },
    {
      icon: Cog,
      label: 'Tracción',
      value: vehicle.drivetrain === '4wd' ? '4x4' : vehicle.drivetrain === 'awd' ? 'AWD' : '2WD',
    },
    {
      icon: Palette,
      label: 'Color exterior',
      value: vehicle.exteriorColor || 'No especificado',
    },
    {
      icon: Palette,
      label: 'Color interior',
      value: vehicle.interiorColor || 'No especificado',
    },
    {
      icon: DoorOpen,
      label: 'Puertas',
      value: vehicle.doors?.toString() || '4',
    },
    {
      icon: Users,
      label: 'Asientos',
      value: vehicle.seats?.toString() || '5',
    },
  ];

  // Filter out specs with "No especificado" values if desired
  const validSpecs = specs.filter(spec => spec.value && spec.value !== 'No especificado');

  return (
    <div>
      <h3 className="mb-4 text-lg font-semibold">Especificaciones técnicas</h3>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {validSpecs.map((spec, index) => (
          <div
            key={index}
            className="flex items-center gap-3 rounded-xl bg-slate-50 p-3 dark:bg-slate-800"
          >
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-white shadow-sm dark:bg-slate-700">
              <spec.icon className="h-5 w-5 text-[#00A870]" />
            </div>
            <div>
              <p className="text-muted-foreground text-xs">{spec.label}</p>
              <p className="text-foreground font-medium">{spec.value}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Engine details if available */}
      {(vehicle.engineSize || vehicle.horsepower) && (
        <div className="border-border mt-6 border-t pt-6">
          <h4 className="text-foreground mb-3 font-medium">Motorización</h4>
          <div className="flex flex-wrap gap-4">
            {vehicle.engineSize && (
              <div className="bg-muted/50 rounded-lg px-4 py-2">
                <span className="text-muted-foreground text-sm">Motor: </span>
                <span className="font-medium">{vehicle.engineSize}</span>
              </div>
            )}
            {vehicle.horsepower && (
              <div className="bg-muted/50 rounded-lg px-4 py-2">
                <span className="text-muted-foreground text-sm">Potencia: </span>
                <span className="font-medium">{vehicle.horsepower} HP</span>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// Features Tab Content
function FeaturesTab({ vehicle }: { vehicle: Vehicle }) {
  // Use features from vehicle or provide mock data
  const features =
    vehicle.features && vehicle.features.length > 0
      ? vehicle.features
      : [
          'Aire acondicionado',
          'Dirección asistida',
          'Vidrios eléctricos',
          'Cierre centralizado',
          'Bluetooth',
          'Cámara de reversa',
          'Sensores de estacionamiento',
          'Airbags',
          'ABS',
          'Control de crucero',
        ];

  // Group features into categories (simple grouping)
  const categories = [
    {
      name: 'Comodidad',
      features: features.filter((f: string) =>
        ['aire', 'asientos', 'climatiz', 'techo', 'cuero'].some(k => f.toLowerCase().includes(k))
      ),
    },
    {
      name: 'Seguridad',
      features: features.filter((f: string) =>
        ['airbag', 'abs', 'sensor', 'cámara', 'freno', 'alarma'].some(k =>
          f.toLowerCase().includes(k)
        )
      ),
    },
    {
      name: 'Tecnología',
      features: features.filter((f: string) =>
        ['bluetooth', 'pantalla', 'usb', 'navegación', 'android', 'apple', 'wifi'].some(k =>
          f.toLowerCase().includes(k)
        )
      ),
    },
    {
      name: 'Otras características',
      features: features.filter((f: string) => {
        const allKeywords = [
          'aire',
          'asientos',
          'climatiz',
          'techo',
          'cuero',
          'airbag',
          'abs',
          'sensor',
          'cámara',
          'freno',
          'alarma',
          'bluetooth',
          'pantalla',
          'usb',
          'navegación',
          'android',
          'apple',
          'wifi',
        ];
        return !allKeywords.some(k => f.toLowerCase().includes(k));
      }),
    },
  ].filter(cat => cat.features.length > 0);

  // If no categories match, show all features in one list
  const showSimpleList =
    categories.length === 0 || categories.every(c => c.name === 'Otras características');

  return (
    <div>
      <h3 className="mb-4 text-lg font-semibold">Características y equipamiento</h3>

      {showSimpleList ? (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {features.map((feature: string, index: number) => (
            <div key={index} className="text-foreground flex items-center gap-2">
              <Check className="h-4 w-4 flex-shrink-0 text-[#00A870]" />
              <span>{feature}</span>
            </div>
          ))}
        </div>
      ) : (
        <div className="space-y-6">
          {categories.map((category, catIndex) => (
            <div key={catIndex}>
              <h4 className="text-foreground mb-3 font-medium">{category.name}</h4>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {category.features.map((feature: string, index: number) => (
                  <div key={index} className="text-foreground flex items-center gap-2">
                    <Check className="h-4 w-4 flex-shrink-0 text-[#00A870]" />
                    <span>{feature}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Empty state */}
      {features.length === 0 && (
        <p className="text-muted-foreground py-8 text-center">
          No hay características listadas para este vehículo.
        </p>
      )}
    </div>
  );
}

export default VehicleTabs;
