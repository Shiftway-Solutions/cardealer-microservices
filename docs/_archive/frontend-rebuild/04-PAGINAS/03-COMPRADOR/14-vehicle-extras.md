# 🔧 Extras y Accesorios de Vehículos

> **Tiempo estimado:** 15 minutos  
> **Componente:** VehicleExtrasSection

---

## 📋 OBJETIVO

Mostrar y filtrar extras/accesorios:

- Características del vehículo
- Equipamiento incluido
- Opciones adicionales

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ CARACTERÍSTICAS Y EXTRAS                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ SEGURIDAD                                                       │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ✅ Airbags frontales      ✅ Airbags laterales              │ │
│ │ ✅ ABS                    ✅ Control de estabilidad         │ │
│ │ ✅ Cámara trasera         ✅ Sensores de estacionamiento    │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ CONFORT                                                         │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ✅ Aire acondicionado     ✅ Asientos de cuero              │ │
│ │ ✅ Sunroof               ✅ Asientos calefaccionados        │ │
│ │ ❌ Asientos ventilados   ✅ Memoria de asientos             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ TECNOLOGÍA                                                      │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ✅ Bluetooth             ✅ Apple CarPlay                   │ │
│ │ ✅ Android Auto          ✅ Navegación GPS                  │ │
│ │ ✅ Carga inalámbrica     ❌ Head-up display                 │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/components/vehicles/VehicleExtrasSection.tsx
'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Shield, Armchair, Smartphone, Gauge,
  Check, X
} from 'lucide-react';

interface Extra {
  id: string;
  name: string;
  category: string;
  included: boolean;
}

interface VehicleExtrasSectionProps {
  extras: Extra[];
}

const categoryConfig: Record<string, { label: string; icon: any }> = {
  safety: { label: 'Seguridad', icon: Shield },
  comfort: { label: 'Confort', icon: Armchair },
  technology: { label: 'Tecnología', icon: Smartphone },
  performance: { label: 'Rendimiento', icon: Gauge },
};

export function VehicleExtrasSection({ extras }: VehicleExtrasSectionProps) {
  // Group extras by category
  const grouped = extras.reduce((acc, extra) => {
    if (!acc[extra.category]) acc[extra.category] = [];
    acc[extra.category].push(extra);
    return acc;
  }, {} as Record<string, Extra[]>);

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-bold">Características y Extras</h2>

      {Object.entries(grouped).map(([category, items]) => {
        const config = categoryConfig[category] || { label: category, icon: Check };
        const Icon = config.icon;

        return (
          <Card key={category}>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <Icon className="w-5 h-5 text-primary-600" />
                {config.label}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-3">
                {items.map((extra) => (
                  <div
                    key={extra.id}
                    className={`flex items-center gap-2 ${
                      extra.included ? 'text-gray-900' : 'text-gray-400'
                    }`}
                  >
                    {extra.included ? (
                      <Check className="w-4 h-4 text-green-600 flex-shrink-0" />
                    ) : (
                      <X className="w-4 h-4 text-gray-300 flex-shrink-0" />
                    )}
                    <span className="text-sm">{extra.name}</span>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
```

### Filtro de Extras en Búsqueda

```typescript
// filepath: src/components/search/ExtrasFilter.tsx
'use client';

import { useState } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { ChevronDown, ChevronUp } from 'lucide-react';

interface ExtrasFilterProps {
  availableExtras: { id: string; name: string; count: number }[];
  selectedExtras: string[];
  onChange: (extras: string[]) => void;
}

export function ExtrasFilter({ availableExtras, selectedExtras, onChange }: ExtrasFilterProps) {
  const [isOpen, setIsOpen] = useState(false);
  const visibleCount = 5;

  const toggle = (id: string) => {
    if (selectedExtras.includes(id)) {
      onChange(selectedExtras.filter(e => e !== id));
    } else {
      onChange([...selectedExtras, id]);
    }
  };

  const visibleExtras = isOpen ? availableExtras : availableExtras.slice(0, visibleCount);

  return (
    <div className="space-y-3">
      <h3 className="font-medium">Extras y Características</h3>

      <div className="space-y-2">
        {visibleExtras.map((extra) => (
          <label key={extra.id} className="flex items-center gap-2 cursor-pointer">
            <Checkbox
              checked={selectedExtras.includes(extra.id)}
              onCheckedChange={() => toggle(extra.id)}
            />
            <span className="text-sm flex-1">{extra.name}</span>
            <span className="text-xs text-gray-500">({extra.count})</span>
          </label>
        ))}
      </div>

      {availableExtras.length > visibleCount && (
        <Button
          variant="ghost"
          size="sm"
          className="w-full"
          onClick={() => setIsOpen(!isOpen)}
        >
          {isOpen ? (
            <>
              <ChevronUp className="w-4 h-4 mr-1" />
              Ver menos
            </>
          ) : (
            <>
              <ChevronDown className="w-4 h-4 mr-1" />
              Ver {availableExtras.length - visibleCount} más
            </>
          )}
        </Button>
      )}

      {selectedExtras.length > 0 && (
        <Button
          variant="link"
          size="sm"
          className="text-red-600 p-0"
          onClick={() => onChange([])}
        >
          Limpiar filtros
        </Button>
      )}
    </div>
  );
}
```

### Selector de Extras para Publicación

```typescript
// filepath: src/components/publish/ExtrasSelector.tsx
'use client';

import { useQuery } from '@tanstack/react-query';
import { Checkbox } from '@/components/ui/checkbox';
import { catalogService } from '@/services/api/catalogService';
import { Skeleton } from '@/components/ui/skeleton';

interface ExtrasSelectorProps {
  selectedExtras: string[];
  onChange: (extras: string[]) => void;
}

export function ExtrasSelector({ selectedExtras, onChange }: ExtrasSelectorProps) {
  const { data: extras, isLoading } = useQuery({
    queryKey: ['catalog-extras'],
    queryFn: () => catalogService.getExtras(),
  });

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3, 4, 5].map(i => (
          <Skeleton key={i} className="h-6 w-full" />
        ))}
      </div>
    );
  }

  const toggle = (id: string) => {
    if (selectedExtras.includes(id)) {
      onChange(selectedExtras.filter(e => e !== id));
    } else {
      onChange([...selectedExtras, id]);
    }
  };

  // Group by category
  const grouped = extras?.reduce((acc: any, extra: any) => {
    if (!acc[extra.category]) acc[extra.category] = [];
    acc[extra.category].push(extra);
    return acc;
  }, {});

  return (
    <div className="space-y-6">
      {Object.entries(grouped || {}).map(([category, items]: [string, any]) => (
        <div key={category}>
          <h4 className="font-medium mb-3 capitalize">{category}</h4>
          <div className="grid grid-cols-2 gap-2">
            {items.map((extra: any) => (
              <label key={extra.id} className="flex items-center gap-2 cursor-pointer">
                <Checkbox
                  checked={selectedExtras.includes(extra.id)}
                  onCheckedChange={() => toggle(extra.id)}
                />
                <span className="text-sm">{extra.name}</span>
              </label>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                    | Descripción                 |
| ------ | --------------------------- | --------------------------- |
| `GET`  | `/api/catalog/extras`       | Lista de extras disponibles |
| `GET`  | `/api/vehicles/{id}/extras` | Extras de un vehículo       |

---

## ✅ CHECKLIST

- [ ] Sección de extras en detalle de vehículo
- [ ] Agrupación por categoría
- [ ] Iconos de check/x para incluido/no incluido
- [ ] Filtro de extras en búsqueda
- [ ] Selector de extras en publicación

---

_Última actualización: Enero 31, 2026_
