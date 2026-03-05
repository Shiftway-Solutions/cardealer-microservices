# 🏪 Componentes de Dealers

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Componentes base, tipos de Dealer

---

## 📋 OBJETIVO

Implementar componentes para dealers:

- DealerCard
- DealerProfile
- DealerStats
- DealerInventory

---

## 🔧 PASO 1: Tipos de Dealer

```typescript
// filepath: src/types/dealer.ts
export interface Dealer {
  id: string;
  slug: string;
  name: string;
  logo?: string;
  description?: string;
  phone: string;
  email: string;
  website?: string;
  address: string;
  city: string;
  province: string;
  rating: number;
  reviewCount: number;
  vehicleCount: number;
  isVerified: boolean;
  isPremium: boolean;
  responseTime?: string;
  memberSince: string;
  workingHours?: WorkingHours;
}

export interface WorkingHours {
  monday: string;
  tuesday: string;
  wednesday: string;
  thursday: string;
  friday: string;
  saturday: string;
  sunday: string;
}
```

---

## 🔧 PASO 2: DealerCard

```typescript
// filepath: src/components/dealers/DealerCard.tsx
"use client";

import Image from "next/image";
import Link from "next/link";
import { MapPin, Star, Car, Check, Clock } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { cn } from "@/lib/utils";
import type { Dealer } from "@/types";

interface DealerCardProps {
  dealer: Dealer;
  className?: string;
}

export function DealerCard({ dealer, className }: DealerCardProps) {
  return (
    <Link
      href={`/dealers/${dealer.slug}`}
      className={cn(
        "block bg-white rounded-xl border border-gray-200",
        "hover:shadow-lg hover:border-primary-200 transition-all",
        className
      )}
    >
      <div className="p-6">
        {/* Header */}
        <div className="flex items-start gap-4">
          {/* Logo */}
          <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
            {dealer.logo ? (
              <Image
                src={dealer.logo}
                alt={dealer.name}
                fill
                sizes="64px"
                className="object-cover"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-2xl font-bold text-gray-400">
                {dealer.name[0]}
              </div>
            )}
          </div>

          {/* Info */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h3 className="font-semibold text-gray-900 truncate">
                {dealer.name}
              </h3>
              {dealer.isVerified && (
                <Check size={16} className="text-primary-600 flex-shrink-0" />
              )}
            </div>

            {/* Location */}
            <div className="flex items-center gap-1 mt-1 text-sm text-gray-500">
              <MapPin size={14} />
              <span>{dealer.city}</span>
            </div>

            {/* Rating */}
            <div className="flex items-center gap-2 mt-2">
              <div className="flex items-center gap-1">
                <Star size={14} className="fill-yellow-400 text-yellow-400" />
                <span className="font-medium text-sm">{dealer.rating.toFixed(1)}</span>
              </div>
              <span className="text-sm text-gray-500">
                ({dealer.reviewCount} opiniones)
              </span>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="flex items-center gap-4 mt-4 pt-4 border-t">
          <div className="flex items-center gap-1 text-sm">
            <Car size={16} className="text-gray-400" />
            <span className="font-medium">{dealer.vehicleCount}</span>
            <span className="text-gray-500">vehículos</span>
          </div>

          {dealer.responseTime && (
            <div className="flex items-center gap-1 text-sm text-gray-500">
              <Clock size={14} />
              <span>Responde en {dealer.responseTime}</span>
            </div>
          )}
        </div>

        {/* Badges */}
        {(dealer.isVerified || dealer.isPremium) && (
          <div className="flex gap-2 mt-3">
            {dealer.isVerified && (
              <Badge variant="success" size="sm">Verificado</Badge>
            )}
            {dealer.isPremium && (
              <Badge variant="premium" size="sm">Premium</Badge>
            )}
          </div>
        )}
      </div>
    </Link>
  );
}
```

---

## 🔧 PASO 3: DealerProfile Header

```typescript
// filepath: src/components/dealers/DealerProfileHeader.tsx
"use client";

import Image from "next/image";
import { MapPin, Phone, Globe, Mail, Star, Check, Clock, Calendar } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { formatPhoneNumber } from "@/lib/utils";
import type { Dealer } from "@/types";

interface DealerProfileHeaderProps {
  dealer: Dealer;
}

export function DealerProfileHeader({ dealer }: DealerProfileHeaderProps) {
  const handleCall = () => {
    window.location.href = `tel:${dealer.phone}`;
  };

  const handleWhatsApp = () => {
    const phone = dealer.phone.replace(/\D/g, "");
    window.open(`https://wa.me/1${phone}`, "_blank");
  };

  return (
    <div className="bg-white rounded-xl border p-6">
      <div className="flex flex-col md:flex-row gap-6">
        {/* Logo */}
        <div className="relative w-24 h-24 md:w-32 md:h-32 rounded-xl overflow-hidden bg-gray-100 flex-shrink-0">
          {dealer.logo ? (
            <Image
              src={dealer.logo}
              alt={dealer.name}
              fill
              sizes="128px"
              className="object-cover"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-4xl font-bold text-gray-400">
              {dealer.name[0]}
            </div>
          )}
        </div>

        {/* Info */}
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">{dealer.name}</h1>
            {dealer.isVerified && (
              <Badge variant="success">
                <Check size={12} className="mr-1" />
                Verificado
              </Badge>
            )}
            {dealer.isPremium && (
              <Badge variant="premium">Premium</Badge>
            )}
          </div>

          {/* Description */}
          {dealer.description && (
            <p className="mt-2 text-gray-600 line-clamp-2">{dealer.description}</p>
          )}

          {/* Stats row */}
          <div className="flex flex-wrap items-center gap-4 mt-4 text-sm">
            <div className="flex items-center gap-1">
              <Star size={16} className="fill-yellow-400 text-yellow-400" />
              <span className="font-semibold">{dealer.rating.toFixed(1)}</span>
              <span className="text-gray-500">({dealer.reviewCount} opiniones)</span>
            </div>

            <div className="flex items-center gap-1 text-gray-600">
              <MapPin size={16} className="text-gray-400" />
              <span>{dealer.city}, {dealer.province}</span>
            </div>

            {dealer.responseTime && (
              <div className="flex items-center gap-1 text-gray-600">
                <Clock size={16} className="text-gray-400" />
                <span>Responde en {dealer.responseTime}</span>
              </div>
            )}

            <div className="flex items-center gap-1 text-gray-600">
              <Calendar size={16} className="text-gray-400" />
              <span>Miembro desde {dealer.memberSince}</span>
            </div>
          </div>

          {/* Contact buttons */}
          <div className="flex flex-wrap gap-3 mt-6">
            <Button onClick={handleCall}>
              <Phone size={16} />
              Llamar
            </Button>
            <Button variant="success" onClick={handleWhatsApp}>
              WhatsApp
            </Button>
            {dealer.website && (
              <Button variant="outline" asChild>
                <a href={dealer.website} target="_blank" rel="noopener noreferrer">
                  <Globe size={16} />
                  Sitio web
                </a>
              </Button>
            )}
            <Button variant="outline" asChild>
              <a href={`mailto:${dealer.email}`}>
                <Mail size={16} />
                Email
              </a>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: DealerStats

```typescript
// filepath: src/components/dealers/DealerStats.tsx
import { Car, Eye, MessageCircle, TrendingUp } from "lucide-react";

interface DealerStatsProps {
  stats: {
    totalVehicles: number;
    activeListings: number;
    totalViews: number;
    totalInquiries: number;
    soldThisMonth: number;
  };
}

export function DealerStats({ stats }: DealerStatsProps) {
  const items = [
    {
      label: "Vehículos activos",
      value: stats.activeListings,
      icon: Car,
      color: "text-blue-600 bg-blue-50",
    },
    {
      label: "Vistas este mes",
      value: stats.totalViews.toLocaleString(),
      icon: Eye,
      color: "text-green-600 bg-green-50",
    },
    {
      label: "Consultas",
      value: stats.totalInquiries,
      icon: MessageCircle,
      color: "text-purple-600 bg-purple-50",
    },
    {
      label: "Vendidos este mes",
      value: stats.soldThisMonth,
      icon: TrendingUp,
      color: "text-orange-600 bg-orange-50",
    },
  ];

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {items.map((item) => (
        <div key={item.label} className="bg-white rounded-xl border p-4">
          <div className={`inline-flex p-2 rounded-lg ${item.color}`}>
            <item.icon size={20} />
          </div>
          <p className="mt-3 text-2xl font-bold text-gray-900">{item.value}</p>
          <p className="text-sm text-gray-500">{item.label}</p>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 5: DealerInventory

```typescript
// filepath: src/components/dealers/DealerInventory.tsx
"use client";

import * as React from "react";
import { VehicleGrid } from "@/components/vehicles/VehicleGrid";
import { EmptyState } from "@/components/ui/EmptyState";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { useVehicles } from "@/lib/hooks/useVehicles";
import type { Dealer } from "@/types";

interface DealerInventoryProps {
  dealer: Dealer;
}

export function DealerInventory({ dealer }: DealerInventoryProps) {
  const { data, isLoading } = useVehicles({ dealerId: dealer.id });

  const vehicles = data?.items ?? [];
  const newVehicles = vehicles.filter((v) => v.condition === "new");
  const usedVehicles = vehicles.filter((v) => v.condition === "used");

  if (!isLoading && vehicles.length === 0) {
    return (
      <EmptyState
        title="Sin vehículos"
        description="Este dealer no tiene vehículos disponibles"
      />
    );
  }

  return (
    <Tabs defaultValue="all">
      <TabsList>
        <TabsTrigger value="all">
          Todos ({vehicles.length})
        </TabsTrigger>
        <TabsTrigger value="new">
          Nuevos ({newVehicles.length})
        </TabsTrigger>
        <TabsTrigger value="used">
          Usados ({usedVehicles.length})
        </TabsTrigger>
      </TabsList>

      <TabsContent value="all" className="mt-6">
        <VehicleGrid vehicles={vehicles} isLoading={isLoading} />
      </TabsContent>

      <TabsContent value="new" className="mt-6">
        <VehicleGrid vehicles={newVehicles} isLoading={isLoading} />
      </TabsContent>

      <TabsContent value="used" className="mt-6">
        <VehicleGrid vehicles={usedVehicles} isLoading={isLoading} />
      </TabsContent>
    </Tabs>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar en http://localhost:3000/dealers:
# - DealerCard muestra info correcta
# - Links funcionan
# - Badges se muestran
# - Responsive design
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/03-COMPONENTES/05-usuarios.md`
