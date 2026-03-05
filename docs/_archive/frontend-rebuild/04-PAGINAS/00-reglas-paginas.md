# 📄 Reglas para Desarrollo de Páginas

> **Principio:** Las páginas son orquestadores. No tienen estilos propios, solo componen componentes.

---

## 📋 Índice

1. [Regla Principal](#regla-principal)
2. [Estructura de una Página](#estructura-de-una-página)
3. [Lo que SÍ debe tener una página](#lo-que-sí-debe-tener-una-página)
4. [Lo que NO debe tener una página](#lo-que-no-debe-tener-una-página)
5. [Template de Página](#template-de-página)
6. [Ejemplos por Tipo de Página](#ejemplos-por-tipo-de-página)

---

## 🎯 Regla Principal

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│   UNA PÁGINA ES UN ORQUESTADOR, NO UN DISEÑADOR                │
│                                                                  │
│   ✅ Importa componentes                                        │
│   ✅ Obtiene datos (hooks, server components)                   │
│   ✅ Transforma datos para componentes                          │
│   ✅ Maneja estados (loading, error)                            │
│   ✅ Compone componentes en orden lógico                        │
│                                                                  │
│   ❌ NO define estilos visuales                                 │
│   ❌ NO tiene clases de Tailwind extensivas                     │
│   ❌ NO duplica lógica de componentes                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📁 Estructura de una Página

### Archivo Típico: `app/[route]/page.tsx`

```typescript
/**
 * [Nombre de la Página]
 *
 * Descripción breve de qué hace esta página.
 */

'use client'; // Solo si necesita interactividad

// 1. Imports de React/Next
import { useMemo } from 'react';

// 2. Imports de componentes (desde barrels)
import { Component1, Component2, Component3 } from '@/components/domain';

// 3. Imports de hooks
import { useData } from '@/hooks/use-data';

// 4. Imports de servicios/utils
import { transformData } from '@/services/data-service';

// 5. Datos estáticos (si aplica)
const STATIC_DATA = [...];

// 6. Helpers de transformación (si son específicos de esta página)
const transformForComponent = (data) => {...};

// 7. Componente de página
export default function PageName() {
  // a. Obtener datos
  const { data, isLoading, error } = useData();

  // b. Transformar datos
  const transformedData = useMemo(() => {
    return transformForComponent(data);
  }, [data]);

  // c. Renderizar composición de componentes
  return (
    <>
      <Component1 />
      <Component2 data={transformedData} />
      {isLoading && <LoadingState />}
      {error && <ErrorState message={error} />}
      <Component3 />
    </>
  );
}
```

---

## ✅ Lo que SÍ debe tener una página

### 1. Imports organizados

```typescript
// ✅ CORRECTO - Imports limpios desde barrels
import {
  HeroStatic,
  SectionContainer,
  FeaturesGrid,
  CTASection,
} from "@/components/homepage";
```

### 2. Obtención de datos

```typescript
// ✅ CORRECTO - Hook para datos
const { vehicles, isLoading, error } = useVehicles();
```

### 3. Transformación de datos

```typescript
// ✅ CORRECTO - Transformar para el componente
const gridVehicles = useMemo(() => {
  return vehicles.map(transformToGridFormat);
}, [vehicles]);
```

### 4. Manejo de estados

```typescript
// ✅ CORRECTO - Estados delegados a componentes
{isLoading && <LoadingSection />}
{error && <ErrorSection message={error} />}
```

### 5. Composición declarativa

```typescript
// ✅ CORRECTO - Solo composición
return (
  <>
    <HeroStatic />
    <SectionContainer title="Vehículos">
      <VehicleGrid vehicles={vehicles} />
    </SectionContainer>
    <CTASection title="¿Listo?" primaryButton={{...}} />
  </>
);
```

---

## ❌ Lo que NO debe tener una página

### 1. Estilos directos extensivos

```typescript
// ❌ INCORRECTO - Estilos en la página
<section className="relative h-[calc(100vh-4rem)] overflow-hidden bg-gradient-to-br from-gray-900 to-gray-800">
  <div className="absolute inset-0 bg-[url('/pattern.svg')] opacity-10" />
  <div className="relative mx-auto flex h-full max-w-7xl items-center">
    <h1 className="text-4xl font-bold tracking-tight text-white">...</h1>
  </div>
</section>

// ✅ CORRECTO - Componente con estilos encapsulados
<HeroStatic title="..." />
```

### 2. Lógica de UI duplicada

```typescript
// ❌ INCORRECTO - Lógica de renderizado repetida
{items.map((item, index) => (
  <div key={index} className="rounded-2xl bg-gray-50 p-4 text-center">
    <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-xl bg-primary/10">
      <item.icon className="h-6 w-6 text-primary" />
    </div>
    <h3 className="mb-1 text-lg font-semibold">{item.title}</h3>
    <p className="text-sm text-gray-600">{item.description}</p>
  </div>
))}

// ✅ CORRECTO - Delegado a componente
<FeaturesGrid features={FEATURES} />
```

### 3. Botones/Links con estilos inline

```typescript
// ❌ INCORRECTO - Estilos de botón en la página
<Link
  href="/vehiculos"
  className="inline-flex h-14 items-center justify-center gap-2 rounded-lg bg-[#00A870] px-8 text-lg font-semibold text-white shadow-lg transition-all hover:bg-[#009663]"
>
  Explorar
</Link>

// ✅ CORRECTO - Componente Button o dentro de otro componente
<Button size="xl" asChild>
  <Link href="/vehiculos">Explorar</Link>
</Button>

// ✅ O mejor aún, dentro del componente Hero
<HeroStatic
  primaryCTA={{ label: 'Explorar', href: '/vehiculos' }}
/>
```

---

## 📝 Template de Página

Usa este template para crear nuevas páginas:

```typescript
/**
 * [NombrePágina]
 *
 * [Descripción de la página]
 *
 * Ruta: /[ruta]
 * Tipo: [Pública | Protegida | Admin]
 */

'use client';

import { useMemo } from 'react';

// Componentes
import {
  // Importar solo lo necesario desde barrels
} from '@/components/[dominio]';

// Hooks
import { useData } from '@/hooks/use-data';

// Tipos y servicios
import { transformData, type DataType } from '@/services/data-service';

// =============================================
// DATOS ESTÁTICOS
// =============================================

const STATIC_CONFIG = {
  // Configuración estática de la página
};

// =============================================
// HELPERS
// =============================================

const transformForDisplay = (data: DataType) => {
  // Transformaciones específicas de esta página
};

// =============================================
// COMPONENTE
// =============================================

export default function NombrePagina() {
  // 1. Obtener datos
  const { data, isLoading, error } = useData();

  // 2. Transformar datos
  const displayData = useMemo(() => {
    if (!data) return [];
    return data.map(transformForDisplay);
  }, [data]);

  // 3. Renderizar
  return (
    <>
      {/* Sección 1 */}
      <ComponenteHero />

      {/* Sección 2 */}
      <SectionContainer title="Título" background="gradient">
        {isLoading ? (
          <LoadingSection />
        ) : error ? (
          <ErrorSection message={error} />
        ) : (
          <DataGrid items={displayData} />
        )}
      </SectionContainer>

      {/* Sección 3 */}
      <CTASection
        title="Call to Action"
        primaryButton={{ label: 'Acción', href: '/accion' }}
      />
    </>
  );
}
```

---

## 📚 Ejemplos por Tipo de Página

### Página Pública (Homepage)

```typescript
// app/page.tsx
import { HeroStatic, SectionContainer, FeaturesGrid, CTASection } from '@/components/homepage';
import { useHomepageSections } from '@/hooks/use-homepage-sections';

export default function HomePage() {
  const { sections, isLoading } = useHomepageSections();

  return (
    <>
      <HeroStatic />
      <SectionContainer title="Destacados" background="gradient">
        <FeaturedGrid vehicles={sections.featured} />
      </SectionContainer>
      <FeaturesGrid features={FEATURES} />
      <CTASection title="¿Listo?" primaryButton={{...}} />
    </>
  );
}
```

### Página de Listado (Vehículos)

```typescript
// app/vehiculos/page.tsx
import { SearchHeader, VehicleGrid, Pagination } from '@/components/vehicles';
import { useVehicleSearch } from '@/hooks/use-vehicle-search';

export default function VehiculosPage() {
  const { vehicles, filters, pagination, isLoading } = useVehicleSearch();

  return (
    <>
      <SearchHeader filters={filters} />
      <VehicleGrid vehicles={vehicles} isLoading={isLoading} />
      <Pagination {...pagination} />
    </>
  );
}
```

### Página de Detalle (Vehículo)

```typescript
// app/vehiculos/[slug]/page.tsx
import { VehicleGallery, VehicleInfo, ContactSeller, SimilarVehicles } from '@/components/vehicles';
import { useVehicle } from '@/hooks/use-vehicle';

export default function VehicleDetailPage({ params }: { params: { slug: string } }) {
  const { vehicle, isLoading } = useVehicle(params.slug);

  if (isLoading) return <LoadingSection />;
  if (!vehicle) return <NotFoundSection />;

  return (
    <>
      <VehicleGallery images={vehicle.images} />
      <VehicleInfo vehicle={vehicle} />
      <ContactSeller seller={vehicle.seller} />
      <SimilarVehicles vehicleId={vehicle.id} />
    </>
  );
}
```

### Página de Dashboard (Dealer)

```typescript
// app/dealer/dashboard/page.tsx
import { DashboardHeader, StatsGrid, RecentActivity, QuickActions } from '@/components/dealer';
import { useDealerDashboard } from '@/hooks/use-dealer-dashboard';

export default function DealerDashboardPage() {
  const { stats, activity, isLoading } = useDealerDashboard();

  return (
    <>
      <DashboardHeader />
      <StatsGrid stats={stats} isLoading={isLoading} />
      <RecentActivity items={activity} />
      <QuickActions />
    </>
  );
}
```

---

## 🔗 Documentos Relacionados

- [03-COMPONENTES/00-metodologia-componentes.md](../03-COMPONENTES/00-metodologia-componentes.md) - Metodología de componentes
- [03-COMPONENTES/07-homepage-components.md](../03-COMPONENTES/07-homepage-components.md) - Componentes del homepage
- [01-PUBLICO/01-home-implementado.md](01-PUBLICO/01-home-implementado.md) - Implementación del homepage

---

**Última actualización:** Enero 2026
