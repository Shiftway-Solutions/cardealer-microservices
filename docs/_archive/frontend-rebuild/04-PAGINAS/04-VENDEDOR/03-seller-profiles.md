---
title: "30. Seller Profiles Completo"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["UserService", "DealerManagementService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 30. Seller Profiles Completo

> **Objetivo:** Implementar sistema completo de perfiles de vendedores (individuales y dealers) con perfiles públicos, configuración privada, badges/verificación, estadísticas y búsqueda/descubrimiento de sellers.  
> **Tiempo estimado:** 3-4 horas  
> **Prioridad:** P1 (Crítico - Confianza y transparencia)  
> **Complejidad:** 🟡 Media (CRUD, public/private views, verification system)  
> **Dependencias:** UserService (SellerProfileController), DealerManagementService, MediaService (avatars/logos)

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura del Sistema](#arquitectura-del-sistema)
2. [Backend API](#backend-api)
3. [Perfil Público](#perfil-público)
4. [Mi Perfil (Settings)](#mi-perfil-settings)
5. [Badges y Verificación](#badges-y-verificación)
6. [Estadísticas de Seller](#estadísticas-de-seller)
7. [Búsqueda y Descubrimiento](#búsqueda-y-descubrimiento)
8. [Top Sellers](#top-sellers)
9. [Hooks y Servicios](#hooks-y-servicios)
10. [Tipos TypeScript](#tipos-typescript)
11. [Validación](#validación)

---

## 🏗️ ARQUITECTURA DEL SISTEMA

### Seller Profile System Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                    SELLER PROFILE ECOSYSTEM                                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  👤 SELLER TYPES                                                            │
│  ├─ Individual Seller (AccountType = Individual)                           │
│  │   • Vende su vehículo personal                                          │
│  │   • Sin suscripción (pago por listing)                                  │
│  │   • Perfil básico: Nombre, foto, contacto                               │
│  │   • Stats: Vehículos vendidos, tiempo respuesta                         │
│  │                                                                          │
│  └─ Dealer (AccountType = Dealer)                                          │
│      • Vende inventario completo                                           │
│      • Suscripción mensual (Starter/Pro/Enterprise)                        │
│      • Perfil completo: Logo, múltiples sucursales, equipo                 │
│      • Stats avanzadas: Conversión, reviews, badges premium                │
│                                                                             │
│  🌐 PERFIL PÚBLICO (SELLER-001)                                             │
│  /sellers/{username} o /dealers/{slug}                                     │
│  ├─ Header: Avatar/Logo, nombre, badges, ubicación                         │
│  ├─ Bio/Descripción del negocio                                            │
│  ├─ Stats cards: Total vehículos, años experiencia, reviews, respuesta     │
│  ├─ Badges: Verificado, Early Bird, Top Seller, Respuesta Rápida          │
│  ├─ Vehículos activos (grid/list view)                                     │
│  ├─ Reseñas de compradores (si tiene)                                      │
│  ├─ Sucursales (mapa) - Solo dealers                                       │
│  ├─ Horarios de atención                                                   │
│  └─ Botones: Contactar, WhatsApp, Ver inventario completo                  │
│                                                                             │
│  ⚙️ MI PERFIL - SETTINGS (SELLER-002)                                       │
│  /settings/profile (ProtectedRoute)                                        │
│  ├─ GET /api/seller-profiles/me                                            │
│  ├─ PUT /api/seller-profiles/me                                            │
│  │                                                                          │
│  ├─ Sección: Información Básica                                            │
│  │   • Display name                                                        │
│  │   • Username (único, URL-friendly)                                      │
│  │   • Bio (max 500 caracteres)                                            │
│  │   • Avatar/Logo upload                                                  │
│  │   • Phone, Email                                                        │
│  │                                                                          │
│  ├─ Sección: Ubicación (Dealers)                                           │
│  │   • Primary location                                                    │
│  │   • Multiple branches (Pro/Enterprise)                                  │
│  │   • Google Maps integration                                             │
│  │                                                                          │
│  ├─ Sección: Horarios                                                      │
│  │   • Business hours (Lun-Dom)                                            │
│  │   • Holidays/cerrado                                                    │
│  │                                                                          │
│  └─ Sección: Social Media                                                  │
│      • Facebook, Instagram, TikTok                                         │
│      • Website URL                                                         │
│                                                                             │
│  📞 PREFERENCIAS DE CONTACTO (SELLER-003)                                   │
│  /settings/contact-preferences                                             │
│  ├─ GET /api/seller-profiles/me/contact-preferences                        │
│  ├─ PUT /api/seller-profiles/me/contact-preferences                        │
│  │                                                                          │
│  ├─ Notificaciones:                                                        │
│  │   • Email cuando alguien consulta por vehículo                          │
│  │   • SMS para consultas urgentes                                         │
│  │   • Push notifications (app móvil)                                      │
│  │   • WhatsApp Business integration                                       │
│  │                                                                          │
│  ├─ Privacidad:                                                            │
│  │   • Mostrar teléfono en listings (toggle)                               │
│  │   • Mostrar WhatsApp button (toggle)                                    │
│  │   • Permitir mensajes directos (toggle)                                 │
│  │   • Horario de contacto preferido                                       │
│  │                                                                          │
│  └─ Auto-responder:                                                        │
│      • Mensaje automático fuera de horario                                 │
│      • Tiempo estimado de respuesta                                        │
│                                                                             │
│  🏆 BADGES Y VERIFICACIÓN (SELLER-004)                                      │
│  /settings/badges                                                          │
│  ├─ GET /api/seller-profiles/me/badges                                     │
│  │                                                                          │
│  ├─ Badges Automáticos (Sistema otorga):                                   │
│  │   • ✅ Verified Seller (KYC completado)                                 │
│  │   • 🎖️ Early Bird Member (inscrito antes 31/01/2026)                   │
│  │   • ⚡ Fast Responder (<1h tiempo respuesta promedio)                   │
│  │   • 📸 Premium Photos (fotos profesionales en todos los listings)       │
│  │   • 📝 Complete Listings (todos los campos completos)                   │
│  │                                                                          │
│  ├─ Badges por Logros:                                                     │
│  │   • 🏅 Top Seller (Top 10% en ventas)                                   │
│  │   • ⭐ 5-Star Rated (promedio 4.8+ con 10+ reviews)                     │
│  │   • 🚀 Power Seller (50+ vehículos vendidos)                            │
│  │   • 💎 Diamond Dealer (100+ vehículos, 4.9+ rating)                     │
│  │                                                                          │
│  └─ Progreso hacia badges:                                                 │
│      • Progress bars con métricas requeridas                               │
│      • Tips para obtener cada badge                                        │
│                                                                             │
│  📊 ESTADÍSTICAS DEL SELLER (SELLER-005)                                    │
│  /settings/statistics (ProtectedRoute)                                     │
│  ├─ GET /api/seller-profiles/me/statistics                                 │
│  │                                                                          │
│  ├─ Overview Cards:                                                        │
│  │   • Total Listings (Active/Sold/Total)                                  │
│  │   • Total Views (último mes)                                            │
│  │   • Inquiries Received                                                  │
│  │   • Response Time (promedio)                                            │
│  │                                                                          │
│  ├─ Performance Metrics:                                                   │
│  │   • Conversion Rate (views → inquiries)                                 │
│  │   • Lead Quality Score (1-10)                                           │
│  │   • Average Days to Sell                                                │
│  │   • Repeat Customer Rate                                                │
│  │                                                                          │
│  ├─ Charts:                                                                │
│  │   • Views over time (línea)                                             │
│  │   • Inquiries by day of week (barra)                                    │
│  │   • Top performing listings (tabla)                                     │
│  │                                                                          │
│  └─ Public Stats (lo que compradores ven):                                 │
│      • Preview de cómo se ve tu perfil público                             │
│      • Toggle para mostrar/ocultar stats específicas                       │
│                                                                             │
│  🔍 BÚSQUEDA DE SELLERS (PROF-001)                                          │
│  /sellers/search                                                           │
│  ├─ GET /api/seller-profiles/search                                        │
│  │   Query params:                                                         │
│  │   • q (nombre, username)                                                │
│  │   • type (Individual, Dealer, All)                                      │
│  │   • location (ciudad, provincia)                                        │
│  │   • verified (true/false)                                               │
│  │   • minRating (1-5)                                                     │
│  │   • badges (array de badges)                                            │
│  │   • sortBy (rating, reviews, vehicles, responseTime)                    │
│  │                                                                          │
│  ├─ Filtros sidebar:                                                       │
│  │   • Tipo de seller                                                      │
│  │   • Ubicación (autocomplete)                                            │
│  │   • Rating mínimo (slider)                                              │
│  │   • Badges (checkboxes)                                                 │
│  │   • Con vehículos activos (toggle)                                      │
│  │                                                                          │
│  └─ Results grid:                                                          │
│      • Card por seller: avatar, nombre, badges, rating, # vehicles         │
│      • Click → /sellers/{username}                                         │
│                                                                             │
│  🏆 TOP SELLERS (PROF-002)                                                  │
│  /sellers/top                                                              │
│  ├─ GET /api/seller-profiles/top                                           │
│  │   Query: ?period=month|quarter|year&type=Individual|Dealer|All          │
│  │                                                                          │
│  ├─ Leaderboard:                                                           │
│  │   • Ranking #1, #2, #3... con medals                                    │
│  │   • Avatar, nombre, badges                                              │
│  │   • Stats: X vehículos vendidos, Y rating, Z reviews                    │
│  │   • Click → Ver perfil                                                  │
│  │                                                                          │
│  ├─ Filtros:                                                               │
│  │   • Período: Mes actual, Trimestre, Año, All-time                       │
│  │   • Tipo: Individual, Dealer, Todos                                     │
│  │   • Categoría: Más ventas, Mejor rated, Más rápido                      │
│  │                                                                          │
│  └─ CTA:                                                                   │
│      • "¿Quieres aparecer aquí?" → Info de planes                          │
│                                                                             │
│  ✅ VERIFICAR SELLER (PROF-003 - Admin)                                     │
│  POST /api/seller-profiles/{id}/verify (Admin only)                        │
│  ├─ Admin panel: /admin/sellers/verification-queue                         │
│  ├─ Ver documentos KYC                                                     │
│  ├─ Verificar identidad (cédula/RNC)                                       │
│  ├─ Aprobar/Rechazar con razón                                             │
│  ├─ Badge "Verified" se otorga automáticamente                             │
│  └─ Email de confirmación                                                  │
│                                                                             │
│  📝 CREAR PERFIL (PROF-004)                                                 │
│  POST /api/seller-profiles                                                 │
│  ├─ Auto-creado al registrarse como seller                                 │
│  ├─ Wizard inicial (first-time setup):                                     │
│  │   • Step 1: Nombre y foto                                               │
│  │   • Step 2: Bio y ubicación                                             │
│  │   • Step 3: Contacto y horarios                                         │
│  │   • Step 4: Subir documentos KYC (opcional)                             │
│  └─ Redirect a dashboard con onboarding checklist                          │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### SellerProfileController Endpoints (Ya Implementados ✅)

```typescript
// MY PROFILE (SELLER-002)
GET / api / seller - profiles / me;
// Response: {
//   id, userId, username, displayName, bio, avatar,
//   accountType, verificationStatus, badges,
//   phone, email, website, socialMedia,
//   businessHours, locations, createdAt
// }

PUT / api / seller - profiles / me;
// Body: { displayName, username, bio, avatar, phone, website, socialMedia, businessHours }
// Response: { success: true, profile: {...} }

// CONTACT PREFERENCES (SELLER-003)
GET / api / seller - profiles / me / contact - preferences;
// Response: {
//   emailNotifications, smsNotifications, pushNotifications,
//   showPhone, showWhatsApp, allowDirectMessages,
//   preferredContactHours, autoResponderEnabled, autoResponderMessage
// }

PUT / api / seller - profiles / me / contact - preferences;
// Body: { ...preferences }
// Response: { success: true }

// BADGES (SELLER-004)
GET / api / seller - profiles / me / badges;
// Response: {
//   badges: [
//     { id: 'verified', name: 'Verified Seller', icon: '✅', earnedAt, ... },
//     { id: 'early_bird', name: 'Early Bird', icon: '🎖️', earnedAt, ... }
//   ],
//   progress: [
//     { badgeId: 'top_seller', requirement: '50 sales', current: 23, target: 50, ... }
//   ]
// }

// STATISTICS (SELLER-005)
GET / api / seller - profiles / me / statistics;
// Response: {
//   overview: { totalListings, totalViews, totalInquiries, avgResponseTime },
//   performance: { conversionRate, leadQuality, avgDaysToSell, repeatCustomerRate },
//   charts: { viewsOverTime: [], inquiriesByDay: [] },
//   topListings: []
// }

// PUBLIC PROFILE (SELLER-001)
GET / api / seller - profiles / { username };
// Public endpoint (no auth required)
// Response: {
//   id, username, displayName, bio, avatar, accountType,
//   badges, rating, totalReviews,
//   publicStats: { activeListings, yearsExperience, avgResponseTime },
//   locations, businessHours, socialMedia,
//   listings: [] // active listings
// }

// SEARCH SELLERS (PROF-001)
GET / api / seller - profiles / search;
// Query: ?q=&type=&location=&verified=&minRating=&badges=&sortBy=&page=&pageSize=
// Response: {
//   sellers: [{ id, username, displayName, avatar, badges, rating, ... }],
//   pagination: { ... }
// }

// TOP SELLERS (PROF-002)
GET / api / seller - profiles / top;
// Query: ?period=month&type=All&category=sales
// Response: {
//   leaderboard: [
//     { rank: 1, seller: {...}, stats: { salesCount, rating, reviewCount } }
//   ]
// }

// VERIFY SELLER (PROF-003 - Admin)
POST / api / seller - profiles / { id } / verify;
// Headers: Authorization (Admin only)
// Response: { verified: true, badgeAwarded: true }

POST / api / seller - profiles / { id } / reject - verification;
// Body: { reason }
// Headers: Authorization (Admin only)

// CREATE PROFILE (PROF-004)
POST / api / seller - profiles;
// Body: { userId, displayName, username, accountType }
// Response: { profileId, username }
// Note: Auto-triggered on user registration
```

---

## 🎨 PERFIL PÚBLICO

### SELLER-001: SellerPublicProfile Component

```typescript
// filepath: src/app/(public)/sellers/[username]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import {
  MapPin,
  Star,
  Clock,
  Phone,
  Mail,
  Globe,
  MessageCircle,
  Calendar,
  TrendingUp,
  Award
} from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { VehicleGrid } from "@/components/vehicles/VehicleGrid";
import { SellerBadges } from "@/components/seller/SellerBadges";
import { SellerReviews } from "@/components/seller/SellerReviews";
import { SellerLocations } from "@/components/seller/SellerLocations";
import { sellerProfileService } from "@/lib/services/sellerProfileService";

interface Props {
  params: { username: string };
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const seller = await sellerProfileService.getByUsername(params.username);

  if (!seller) {
    return { title: "Seller Not Found" };
  }

  return {
    title: `${seller.displayName} - OKLA`,
    description: seller.bio || `Ver vehículos de ${seller.displayName}`,
  };
}

export default async function SellerPublicProfilePage({ params }: Props) {
  const seller = await sellerProfileService.getByUsername(params.username);

  if (!seller) {
    notFound();
  }

  const stats = seller.publicStats;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b">
        <div className="container max-w-7xl py-8">
          <div className="flex items-start gap-6">
            {/* Avatar/Logo */}
            <div className="relative">
              <img
                src={seller.avatar || "/default-avatar.png"}
                alt={seller.displayName}
                className="w-32 h-32 rounded-full object-cover border-4 border-white shadow-lg"
              />
              {seller.accountType === "Dealer" && (
                <Badge className="absolute -bottom-2 left-1/2 -translate-x-1/2 bg-blue-600">
                  DEALER
                </Badge>
              )}
            </div>

            {/* Info */}
            <div className="flex-1">
              <div className="flex items-start justify-between mb-3">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900 mb-2">
                    {seller.displayName}
                  </h1>
                  <p className="text-gray-600">@{seller.username}</p>
                </div>

                {/* Badges */}
                <SellerBadges badges={seller.badges} size="md" />
              </div>

              {/* Bio */}
              {seller.bio && (
                <p className="text-gray-700 mb-4 max-w-3xl">{seller.bio}</p>
              )}

              {/* Rating */}
              {seller.rating && (
                <div className="flex items-center gap-4 mb-4">
                  <div className="flex items-center gap-1">
                    <Star size={20} className="text-yellow-500 fill-yellow-500" />
                    <span className="text-xl font-semibold">{seller.rating.toFixed(1)}</span>
                  </div>
                  <span className="text-gray-600">
                    {seller.totalReviews} reseñas
                  </span>
                </div>
              )}

              {/* Location */}
              {seller.locations?.[0] && (
                <div className="flex items-center gap-2 text-gray-600 mb-4">
                  <MapPin size={18} />
                  <span>
                    {seller.locations[0].city}, {seller.locations[0].province}
                  </span>
                </div>
              )}

              {/* Contact buttons */}
              <div className="flex gap-3">
                <Button size="lg" className="gap-2">
                  <MessageCircle size={18} />
                  Contactar
                </Button>
                {seller.showWhatsApp && seller.phone && (
                  <Button
                    variant="outline"
                    size="lg"
                    className="gap-2"
                    onClick={() =>
                      window.open(`https://wa.me/${seller.phone.replace(/\D/g, "")}`)
                    }
                  >
                    <Phone size={18} />
                    WhatsApp
                  </Button>
                )}
                {seller.website && (
                  <Button
                    variant="outline"
                    size="lg"
                    onClick={() => window.open(seller.website, "_blank")}
                  >
                    <Globe size={18} />
                  </Button>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Section */}
      <div className="bg-gradient-to-br from-blue-50 to-blue-100 py-12">
        <div className="container max-w-7xl">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            <Card className="p-6 text-center">
              <div className="text-3xl font-bold text-blue-600 mb-2">
                {stats.activeListings}
              </div>
              <p className="text-sm text-gray-600">Vehículos Activos</p>
            </Card>

            <Card className="p-6 text-center">
              <div className="text-3xl font-bold text-green-600 mb-2">
                {stats.yearsExperience}+
              </div>
              <p className="text-sm text-gray-600">Años de Experiencia</p>
            </Card>

            <Card className="p-6 text-center">
              <div className="text-3xl font-bold text-purple-600 mb-2">
                {stats.totalSales || 0}
              </div>
              <p className="text-sm text-gray-600">Vehículos Vendidos</p>
            </Card>

            <Card className="p-6 text-center">
              <div className="text-3xl font-bold text-yellow-600 mb-2">
                {stats.avgResponseTime}
              </div>
              <p className="text-sm text-gray-600">Tiempo Respuesta</p>
            </Card>
          </div>
        </div>
      </div>

      {/* Active Listings */}
      <div className="container max-w-7xl py-12">
        <div className="flex items-center justify-between mb-8">
          <h2 className="text-2xl font-bold text-gray-900">
            Vehículos Disponibles ({seller.listings.length})
          </h2>
          <Button variant="outline">Ver Todos</Button>
        </div>

        <VehicleGrid vehicles={seller.listings} />
      </div>

      {/* Reviews */}
      {seller.totalReviews > 0 && (
        <div className="bg-gray-50 py-12">
          <div className="container max-w-7xl">
            <h2 className="text-2xl font-bold text-gray-900 mb-8">
              Reseñas ({seller.totalReviews})
            </h2>
            <SellerReviews sellerId={seller.id} />
          </div>
        </div>
      )}

      {/* Locations (Dealers only) */}
      {seller.accountType === "Dealer" && seller.locations.length > 1 && (
        <div className="container max-w-7xl py-12">
          <h2 className="text-2xl font-bold text-gray-900 mb-8">
            Sucursales ({seller.locations.length})
          </h2>
          <SellerLocations locations={seller.locations} />
        </div>
      )}

      {/* Business Hours */}
      {seller.businessHours && (
        <div className="bg-gray-50 py-12">
          <div className="container max-w-7xl">
            <Card className="p-8">
              <div className="flex items-center gap-3 mb-6">
                <Clock size={24} className="text-blue-600" />
                <h3 className="text-xl font-semibold">Horarios de Atención</h3>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                {Object.entries(seller.businessHours).map(([day, hours]) => (
                  <div key={day} className="flex justify-between py-2 border-b">
                    <span className="font-medium capitalize">{day}</span>
                    <span className="text-gray-600">
                      {hours.isOpen ? `${hours.open} - ${hours.close}` : "Cerrado"}
                    </span>
                  </div>
                ))}
              </div>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## ⚙️ MI PERFIL (SETTINGS)

### SELLER-002: ProfileSettingsPage

```typescript
// filepath: src/app/(protected)/settings/profile/page.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { useState } from "react";
import { Loader2, Upload, X } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Card } from "@/components/ui/Card";
import { Avatar } from "@/components/ui/Avatar";
import { Label } from "@/components/ui/Label";
import { useMyProfile, useUpdateProfile } from "@/lib/hooks/useSellerProfile";
import { useUploadImage } from "@/lib/hooks/useMedia";
import { toast } from "sonner";

const schema = z.object({
  displayName: z.string().min(3, "Mínimo 3 caracteres"),
  username: z
    .string()
    .min(3, "Mínimo 3 caracteres")
    .regex(/^[a-z0-9-]+$/, "Solo minúsculas, números y guiones"),
  bio: z.string().max(500, "Máximo 500 caracteres").optional(),
  phone: z.string().optional(),
  website: z.string().url("URL inválida").optional().or(z.literal("")),
  facebook: z.string().optional(),
  instagram: z.string().optional(),
  tiktok: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

export default function ProfileSettingsPage() {
  const { data: profile, isLoading } = useMyProfile();
  const { mutate: updateProfile, isPending } = useUpdateProfile();
  const { mutate: uploadImage, isPending: isUploading } = useUploadImage();
  const [avatar, setAvatar] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: profile,
  });

  const handleAvatarSelect = (file: File) => {
    // Validate
    if (!["image/jpeg", "image/png"].includes(file.type)) {
      toast.error("Solo JPG o PNG");
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error("Máximo 5MB");
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    uploadImage(formData, {
      onSuccess: (data) => {
        setAvatar(data.url);
        toast.success("Imagen subida");
      },
    });
  };

  const onSubmit = (data: FormData) => {
    updateProfile(
      { ...data, avatar },
      {
        onSuccess: () => {
          toast.success("Perfil actualizado");
        },
      }
    );
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <Loader2 size={32} className="animate-spin text-blue-600" />
      </div>
    );
  }

  return (
    <div className="container max-w-4xl py-12">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Mi Perfil</h1>
        <p className="text-gray-600">
          Actualiza tu información pública visible para compradores
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">
        {/* Avatar */}
        <Card className="p-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Foto de Perfil
          </h2>

          <div className="flex items-start gap-6">
            <Avatar
              src={avatar || profile?.avatar}
              alt={profile?.displayName}
              size="xl"
            />

            <div>
              <label className="cursor-pointer">
                <input
                  type="file"
                  className="hidden"
                  accept="image/jpeg,image/png"
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    if (file) handleAvatarSelect(file);
                  }}
                  disabled={isUploading}
                />
                <Button
                  type="button"
                  variant="outline"
                  disabled={isUploading}
                  className="gap-2"
                >
                  {isUploading ? (
                    <Loader2 size={16} className="animate-spin" />
                  ) : (
                    <Upload size={16} />
                  )}
                  Cambiar Foto
                </Button>
              </label>
              <p className="text-sm text-gray-600 mt-2">
                JPG o PNG. Máximo 5MB. Recomendado 400x400px.
              </p>
            </div>
          </div>
        </Card>

        {/* Basic Info */}
        <Card className="p-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Información Básica
          </h2>

          <div className="space-y-6">
            <div>
              <Label htmlFor="displayName">Nombre para mostrar *</Label>
              <Input
                id="displayName"
                {...register("displayName")}
                placeholder="Ej: Auto Express RD"
                error={errors.displayName?.message}
              />
            </div>

            <div>
              <Label htmlFor="username">Username (URL) *</Label>
              <div className="flex items-center gap-2">
                <span className="text-gray-600">okla.com.do/sellers/</span>
                <Input
                  id="username"
                  {...register("username")}
                  placeholder="auto-express-rd"
                  error={errors.username?.message}
                  className="flex-1"
                />
              </div>
              <p className="text-xs text-gray-500 mt-1">
                Solo minúsculas, números y guiones
              </p>
            </div>

            <div>
              <Label htmlFor="bio">Biografía</Label>
              <Textarea
                id="bio"
                {...register("bio")}
                placeholder="Cuéntanos sobre ti o tu negocio..."
                rows={4}
                maxLength={500}
                error={errors.bio?.message}
              />
              <p className="text-xs text-gray-500 mt-1">
                {register("bio").name?.length || 0}/500 caracteres
              </p>
            </div>

            <div className="grid md:grid-cols-2 gap-6">
              <div>
                <Label htmlFor="phone">Teléfono</Label>
                <Input
                  id="phone"
                  {...register("phone")}
                  placeholder="(809) 555-1234"
                  error={errors.phone?.message}
                />
              </div>

              <div>
                <Label htmlFor="website">Sitio Web</Label>
                <Input
                  id="website"
                  {...register("website")}
                  placeholder="https://mi-sitio.com"
                  error={errors.website?.message}
                />
              </div>
            </div>
          </div>
        </Card>

        {/* Social Media */}
        <Card className="p-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Redes Sociales
          </h2>

          <div className="space-y-6">
            <div>
              <Label htmlFor="facebook">Facebook</Label>
              <Input
                id="facebook"
                {...register("facebook")}
                placeholder="https://facebook.com/mi-negocio"
              />
            </div>

            <div>
              <Label htmlFor="instagram">Instagram</Label>
              <Input
                id="instagram"
                {...register("instagram")}
                placeholder="@mi_negocio"
              />
            </div>

            <div>
              <Label htmlFor="tiktok">TikTok</Label>
              <Input
                id="tiktok"
                {...register("tiktok")}
                placeholder="@mi_negocio"
              />
            </div>
          </div>
        </Card>

        {/* Submit */}
        <div className="flex justify-end gap-4">
          <Button type="button" variant="outline" onClick={() => window.history.back()}>
            Cancelar
          </Button>
          <Button type="submit" size="lg" disabled={isPending} className="min-w-[200px]">
            {isPending && <Loader2 size={16} className="mr-2 animate-spin" />}
            Guardar Cambios
          </Button>
        </div>
      </form>
    </div>
  );
}
```

---

## 🏆 BADGES Y VERIFICACIÓN

### SELLER-004: BadgesPage Component

```typescript
// filepath: src/app/(protected)/settings/badges/page.tsx
"use client";

import { Progress } from "@/components/ui/Progress";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { CheckCircle, Lock, TrendingUp } from "lucide-react";
import { useMyBadges } from "@/lib/hooks/useSellerProfile";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export default function BadgesPage() {
  const { data: badgesData, isLoading } = useMyBadges();

  if (isLoading) return <div>Cargando...</div>;

  const { badges, progress } = badgesData;
  const earnedBadges = badges.filter((b) => b.earned);
  const lockedBadges = progress.filter((p) => !p.earned);

  return (
    <div className="container max-w-6xl py-12">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Mis Badges y Logros
        </h1>
        <p className="text-gray-600">
          Gana badges completando logros y mejorando tu perfil
        </p>
      </div>

      {/* Earned Badges */}
      <div className="mb-12">
        <h2 className="text-2xl font-semibold text-gray-900 mb-6">
          Badges Obtenidos ({earnedBadges.length})
        </h2>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {earnedBadges.map((badge) => (
            <Card key={badge.id} className="p-6 relative overflow-hidden">
              <div className="absolute top-4 right-4">
                <CheckCircle size={24} className="text-green-600" />
              </div>

              <div className="text-5xl mb-4">{badge.icon}</div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                {badge.name}
              </h3>
              <p className="text-sm text-gray-600 mb-3">{badge.description}</p>

              <p className="text-xs text-gray-500">
                Obtenido {formatDistanceToNow(new Date(badge.earnedAt), { locale: es })}
              </p>
            </Card>
          ))}
        </div>
      </div>

      {/* Progress towards badges */}
      <div>
        <h2 className="text-2xl font-semibold text-gray-900 mb-6">
          En Progreso ({lockedBadges.length})
        </h2>

        <div className="space-y-6">
          {lockedBadges.map((item) => {
            const progressPercent = (item.current / item.target) * 100;

            return (
              <Card key={item.badgeId} className="p-6">
                <div className="flex items-start gap-6">
                  <div className="text-4xl opacity-50">{item.icon}</div>

                  <div className="flex-1">
                    <div className="flex items-center justify-between mb-2">
                      <h3 className="text-lg font-semibold text-gray-900">
                        {item.name}
                      </h3>
                      <Badge variant="secondary">
                        <Lock size={14} className="mr-1" />
                        Bloqueado
                      </Badge>
                    </div>

                    <p className="text-sm text-gray-600 mb-4">
                      {item.description}
                    </p>

                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">{item.requirement}</span>
                        <span className="font-semibold">
                          {item.current} / {item.target}
                        </span>
                      </div>
                      <Progress value={progressPercent} className="h-2" />
                    </div>

                    {item.tips && (
                      <div className="mt-4 p-3 bg-blue-50 rounded-lg">
                        <div className="flex items-start gap-2">
                          <TrendingUp size={16} className="text-blue-600 mt-0.5" />
                          <div className="text-sm text-blue-900">
                            <p className="font-semibold mb-1">Tip:</p>
                            <p>{item.tips}</p>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      </div>
    </div>
  );
}
```

---

## 🔍 BÚSQUEDA Y DESCUBRIMIENTO

### PROF-001: SearchSellersPage

```typescript
// filepath: src/app/(public)/sellers/search/page.tsx
"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { Search, Filter, MapPin, Star } from "lucide-react";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { Checkbox } from "@/components/ui/Checkbox";
import { Slider } from "@/components/ui/Slider";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";
import { SellerCard } from "@/components/seller/SellerCard";
import { useSearchSellers } from "@/lib/hooks/useSellerProfile";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/Sheet";

export default function SearchSellersPage() {
  const searchParams = useSearchParams();
  const [query, setQuery] = useState(searchParams.get("q") || "");
  const [filters, setFilters] = useState({
    type: searchParams.get("type") || "All",
    location: searchParams.get("location") || "",
    verified: searchParams.get("verified") === "true",
    minRating: Number(searchParams.get("minRating")) || 0,
    badges: searchParams.get("badges")?.split(",") || [],
    sortBy: searchParams.get("sortBy") || "rating",
  });

  const { data, isLoading } = useSearchSellers({
    q: query,
    ...filters,
    page: 1,
    pageSize: 20,
  });

  const handleSearch = () => {
    // Trigger search with current filters
  };

  const FiltersSidebar = () => (
    <div className="space-y-6">
      {/* Type */}
      <div>
        <h3 className="font-semibold text-gray-900 mb-3">Tipo de Seller</h3>
        <Select
          value={filters.type}
          onChange={(e) => setFilters({ ...filters, type: e.target.value })}
        >
          <option value="All">Todos</option>
          <option value="Individual">Individual</option>
          <option value="Dealer">Dealer</option>
        </Select>
      </div>

      {/* Location */}
      <div>
        <h3 className="font-semibold text-gray-900 mb-3">Ubicación</h3>
        <Input
          placeholder="Ciudad o provincia"
          value={filters.location}
          onChange={(e) => setFilters({ ...filters, location: e.target.value })}
        />
      </div>

      {/* Rating */}
      <div>
        <h3 className="font-semibold text-gray-900 mb-3">
          Rating Mínimo: {filters.minRating}+
        </h3>
        <Slider
          value={[filters.minRating]}
          onValueChange={([val]) => setFilters({ ...filters, minRating: val })}
          min={0}
          max={5}
          step={0.5}
        />
      </div>

      {/* Verified */}
      <div>
        <label className="flex items-center gap-2 cursor-pointer">
          <Checkbox
            checked={filters.verified}
            onCheckedChange={(checked) =>
              setFilters({ ...filters, verified: checked as boolean })
            }
          />
          <span className="text-sm text-gray-700">Solo verificados</span>
        </label>
      </div>

      {/* Badges */}
      <div>
        <h3 className="font-semibold text-gray-900 mb-3">Badges</h3>
        <div className="space-y-2">
          {["early_bird", "top_seller", "fast_responder", "premium_photos"].map(
            (badge) => (
              <label key={badge} className="flex items-center gap-2 cursor-pointer">
                <Checkbox
                  checked={filters.badges.includes(badge)}
                  onCheckedChange={(checked) => {
                    const newBadges = checked
                      ? [...filters.badges, badge]
                      : filters.badges.filter((b) => b !== badge);
                    setFilters({ ...filters, badges: newBadges });
                  }}
                />
                <span className="text-sm text-gray-700 capitalize">
                  {badge.replace("_", " ")}
                </span>
              </label>
            )
          )}
        </div>
      </div>

      {/* Apply */}
      <Button onClick={handleSearch} className="w-full">
        Aplicar Filtros
      </Button>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container max-w-7xl py-12">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-4">
            Buscar Sellers
          </h1>

          {/* Search bar */}
          <div className="flex gap-4">
            <div className="flex-1 relative">
              <Search
                size={20}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
              />
              <Input
                placeholder="Buscar por nombre o username..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && handleSearch()}
                className="pl-10"
              />
            </div>

            {/* Sort */}
            <Select
              value={filters.sortBy}
              onChange={(e) => setFilters({ ...filters, sortBy: e.target.value })}
              className="w-48"
            >
              <option value="rating">Mejor Rating</option>
              <option value="reviews">Más Reseñas</option>
              <option value="vehicles">Más Vehículos</option>
              <option value="responseTime">Más Rápido</option>
            </Select>

            {/* Mobile filters */}
            <Sheet>
              <SheetTrigger asChild>
                <Button variant="outline" className="md:hidden">
                  <Filter size={20} />
                </Button>
              </SheetTrigger>
              <SheetContent>
                <FiltersSidebar />
              </SheetContent>
            </Sheet>
          </div>
        </div>

        <div className="flex gap-8">
          {/* Filters sidebar - Desktop */}
          <aside className="hidden md:block w-64 flex-shrink-0">
            <Card className="p-6 sticky top-4">
              <FiltersSidebar />
            </Card>
          </aside>

          {/* Results */}
          <div className="flex-1">
            {isLoading ? (
              <div>Cargando...</div>
            ) : (
              <>
                <p className="text-gray-600 mb-6">
                  {data?.sellers.length} sellers encontrados
                </p>

                <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {data?.sellers.map((seller) => (
                    <SellerCard key={seller.id} seller={seller} />
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

## 🏆 TOP SELLERS

### PROF-002: TopSellersPage

```typescript
// filepath: src/app/(public)/sellers/top/page.tsx
"use client";

import { useState } from "react";
import { Trophy, Medal, Award, TrendingUp } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { Avatar } from "@/components/ui/Avatar";
import { SellerBadges } from "@/components/seller/SellerBadges";
import { useTopSellers } from "@/lib/hooks/useSellerProfile";
import Link from "next/link";

export default function TopSellersPage() {
  const [period, setPeriod] = useState<"month" | "quarter" | "year">("month");
  const [type, setType] = useState<"All" | "Individual" | "Dealer">("All");
  const [category, setCategory] = useState<"sales" | "rating" | "responseTime">(
    "sales"
  );

  const { data, isLoading } = useTopSellers({ period, type, category });

  const getMedalIcon = (rank: number) => {
    if (rank === 1) return <Trophy size={32} className="text-yellow-500" />;
    if (rank === 2) return <Medal size={32} className="text-gray-400" />;
    if (rank === 3) return <Award size={32} className="text-amber-600" />;
    return <span className="text-2xl font-bold text-gray-400">#{rank}</span>;
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-yellow-50 via-orange-50 to-red-50">
      <div className="container max-w-6xl py-12">
        {/* Header */}
        <div className="text-center mb-12">
          <div className="flex justify-center mb-4">
            <Trophy size={64} className="text-yellow-500" />
          </div>
          <h1 className="text-4xl font-bold text-gray-900 mb-4">
            Top Sellers de OKLA
          </h1>
          <p className="text-xl text-gray-600">
            Los mejores vendedores de la plataforma
          </p>
        </div>

        {/* Filters */}
        <Card className="p-6 mb-8">
          <div className="grid md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Período
              </label>
              <Select
                value={period}
                onChange={(e) => setPeriod(e.target.value as any)}
              >
                <option value="month">Mes Actual</option>
                <option value="quarter">Trimestre</option>
                <option value="year">Año</option>
              </Select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Tipo
              </label>
              <Select value={type} onChange={(e) => setType(e.target.value as any)}>
                <option value="All">Todos</option>
                <option value="Individual">Individual</option>
                <option value="Dealer">Dealer</option>
              </Select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Categoría
              </label>
              <Select
                value={category}
                onChange={(e) => setCategory(e.target.value as any)}
              >
                <option value="sales">Más Ventas</option>
                <option value="rating">Mejor Rating</option>
                <option value="responseTime">Más Rápido</option>
              </Select>
            </div>
          </div>
        </Card>

        {/* Leaderboard */}
        {isLoading ? (
          <div className="text-center py-12">Cargando...</div>
        ) : (
          <div className="space-y-4">
            {data?.leaderboard.map((entry, index) => (
              <Card
                key={entry.seller.id}
                className={`p-6 hover:shadow-xl transition-shadow ${
                  index < 3 ? "border-2 border-yellow-400" : ""
                }`}
              >
                <div className="flex items-center gap-6">
                  {/* Rank */}
                  <div className="flex-shrink-0 w-16 text-center">
                    {getMedalIcon(entry.rank)}
                  </div>

                  {/* Avatar */}
                  <Link href={`/sellers/${entry.seller.username}`}>
                    <Avatar
                      src={entry.seller.avatar}
                      alt={entry.seller.displayName}
                      size="lg"
                      className="cursor-pointer hover:scale-110 transition-transform"
                    />
                  </Link>

                  {/* Info */}
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <Link href={`/sellers/${entry.seller.username}`}>
                        <h3 className="text-xl font-bold text-gray-900 hover:text-blue-600">
                          {entry.seller.displayName}
                        </h3>
                      </Link>
                      {entry.seller.accountType === "Dealer" && (
                        <Badge variant="primary">DEALER</Badge>
                      )}
                    </div>

                    <SellerBadges badges={entry.seller.badges} size="sm" />
                  </div>

                  {/* Stats */}
                  <div className="text-right">
                    <div className="text-3xl font-bold text-blue-600 mb-1">
                      {entry.stats.salesCount}
                    </div>
                    <p className="text-sm text-gray-600">Ventas</p>

                    <div className="flex items-center gap-1 mt-2 justify-end">
                      <span className="text-yellow-500">★</span>
                      <span className="font-semibold">
                        {entry.stats.rating.toFixed(1)}
                      </span>
                      <span className="text-gray-600 text-sm">
                        ({entry.stats.reviewCount})
                      </span>
                    </div>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}

        {/* CTA */}
        <div className="mt-12 text-center p-8 bg-blue-600 text-white rounded-xl">
          <h3 className="text-2xl font-bold mb-4">
            ¿Quieres aparecer en este ranking?
          </h3>
          <p className="text-blue-100 mb-6">
            Aumenta tus ventas y mejora tu perfil para subir posiciones
          </p>
          <Link href="/dealer/pricing">
            <Button size="lg" variant="secondary">
              Ver Planes para Dealers
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### useSellerProfile Hook

```typescript
// filepath: src/lib/hooks/useSellerProfile.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { sellerProfileService } from "@/lib/services/sellerProfileService";
import { toast } from "sonner";

export const useMyProfile = () => {
  return useQuery({
    queryKey: ["seller-profile", "me"],
    queryFn: () => sellerProfileService.getMe(),
  });
};

export const useUpdateProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) => sellerProfileService.updateMe(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["seller-profile", "me"] });
    },
  });
};

export const useMyBadges = () => {
  return useQuery({
    queryKey: ["seller-profile", "me", "badges"],
    queryFn: () => sellerProfileService.getMyBadges(),
  });
};

export const useMyStatistics = () => {
  return useQuery({
    queryKey: ["seller-profile", "me", "statistics"],
    queryFn: () => sellerProfileService.getMyStatistics(),
  });
};

export const useContactPreferences = () => {
  return useQuery({
    queryKey: ["seller-profile", "me", "contact-preferences"],
    queryFn: () => sellerProfileService.getContactPreferences(),
  });
};

export const useUpdateContactPreferences = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: any) =>
      sellerProfileService.updateContactPreferences(data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["seller-profile", "me", "contact-preferences"],
      });
      toast.success("Preferencias actualizadas");
    },
  });
};

export const useSearchSellers = (params: any) => {
  return useQuery({
    queryKey: ["seller-profiles", "search", params],
    queryFn: () => sellerProfileService.search(params),
  });
};

export const useTopSellers = (params: any) => {
  return useQuery({
    queryKey: ["seller-profiles", "top", params],
    queryFn: () => sellerProfileService.getTop(params),
  });
};

export const useSellerByUsername = (username: string) => {
  return useQuery({
    queryKey: ["seller-profile", username],
    queryFn: () => sellerProfileService.getByUsername(username),
    enabled: !!username,
  });
};
```

---

## 📦 TIPOS TYPESCRIPT

```typescript
// filepath: src/lib/types/sellerProfile.ts
export type AccountType = "Individual" | "Dealer";

export type VerificationStatus =
  | "NotVerified"
  | "Pending"
  | "Verified"
  | "Rejected";

export interface Badge {
  id: string;
  name: string;
  description: string;
  icon: string;
  earned: boolean;
  earnedAt?: string;
  category: "auto" | "achievement" | "premium";
}

export interface BadgeProgress {
  badgeId: string;
  name: string;
  description: string;
  icon: string;
  requirement: string;
  current: number;
  target: number;
  earned: boolean;
  tips?: string;
}

export interface SellerProfile {
  id: string;
  userId: string;
  username: string;
  displayName: string;
  bio?: string;
  avatar?: string;
  accountType: AccountType;
  verificationStatus: VerificationStatus;
  badges: Badge[];
  rating?: number;
  totalReviews: number;
  phone?: string;
  email: string;
  website?: string;
  socialMedia?: {
    facebook?: string;
    instagram?: string;
    tiktok?: string;
  };
  businessHours?: Record<
    string,
    { isOpen: boolean; open: string; close: string }
  >;
  locations: Location[];
  showPhone: boolean;
  showWhatsApp: boolean;
  publicStats: {
    activeListings: number;
    yearsExperience: number;
    totalSales?: number;
    avgResponseTime: string;
  };
  listings: VehicleListing[];
  createdAt: string;
}

export interface ContactPreferences {
  emailNotifications: boolean;
  smsNotifications: boolean;
  pushNotifications: boolean;
  whatsappNotifications: boolean;
  showPhone: boolean;
  showWhatsApp: boolean;
  allowDirectMessages: boolean;
  preferredContactHours?: {
    start: string;
    end: string;
  };
  autoResponderEnabled: boolean;
  autoResponderMessage?: string;
}

export interface SellerStatistics {
  overview: {
    totalListings: number;
    activeListings: number;
    soldListings: number;
    totalViews: number;
    totalInquiries: number;
    avgResponseTime: string;
  };
  performance: {
    conversionRate: number;
    leadQuality: number;
    avgDaysToSell: number;
    repeatCustomerRate: number;
  };
  charts: {
    viewsOverTime: Array<{ date: string; views: number }>;
    inquiriesByDay: Array<{ day: string; count: number }>;
  };
  topListings: Array<{
    vehicleId: string;
    title: string;
    views: number;
    inquiries: number;
  }>;
}

export interface SearchSellersParams {
  q?: string;
  type?: "Individual" | "Dealer" | "All";
  location?: string;
  verified?: boolean;
  minRating?: number;
  badges?: string[];
  sortBy?: "rating" | "reviews" | "vehicles" | "responseTime";
  page?: number;
  pageSize?: number;
}

export interface TopSellersParams {
  period: "month" | "quarter" | "year";
  type: "All" | "Individual" | "Dealer";
  category: "sales" | "rating" | "responseTime";
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Perfil Público:
# - /sellers/{username} muestra perfil completo
# - Avatar/logo visible
# - Badges renderizados
# - Stats cards con datos correctos
# - Vehículos activos en grid
# - Botones de contacto funcionan
# - WhatsApp abre chat
# - Horarios de atención visible

# Verificar Mi Perfil:
# - /settings/profile muestra formulario
# - Cambiar avatar funciona (drag & drop)
# - Username validation (solo minúsculas, números, guiones)
# - Bio contador de caracteres (500 max)
# - Social media URLs validan
# - Submit actualiza perfil
# - Toast de confirmación

# Verificar Badges:
# - /settings/badges lista badges obtenidos
# - Progress bars de badges bloqueados
# - Tips para obtener badges
# - Porcentaje de progreso correcto

# Verificar Estadísticas:
# - /settings/statistics muestra overview cards
# - Charts renderizan (Recharts/Chart.js)
# - Top listings table
# - Performance metrics calculadas correctamente

# Verificar Búsqueda:
# - /sellers/search funciona
# - Filtros sidebar actualiza resultados
# - Sort by funciona
# - Paginación funciona
# - Cards de sellers muestran info correcta
# - Click en seller va a perfil público

# Verificar Top Sellers:
# - /sellers/top muestra leaderboard
# - Medals para top 3
# - Filtros funcionan (período, tipo, categoría)
# - Stats muestran correctamente
# - CTA redirige a /dealer/pricing
```

---

## 🚀 MEJORAS FUTURAS

1. **Seller Verification Video**: Video KYC para verificación premium
2. **Seller Analytics Dashboard**: Dashboard avanzado con gráficos
3. **Seller Reviews System**: Sistema completo de reseñas de compradores
4. **Seller Recommendations**: "Sellers similares" con IA
5. **Seller Comparison**: Comparar hasta 3 sellers lado a lado
6. **Seller Following**: Seguir sellers favoritos, recibir updates
7. **Seller Certifications**: Certificaciones oficiales (Chevrolet Dealer, etc.)
8. **Seller Promotions**: Promociones especiales por tiempo limitado

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/seller-profiles.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Seller Profiles", () => {
  test("debe mostrar perfil público del vendedor", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await expect(page.getByRole("heading")).toBeVisible();
    await expect(page.getByTestId("seller-info")).toBeVisible();
    await expect(page.getByTestId("seller-rating")).toBeVisible();
  });

  test("debe mostrar vehículos del vendedor", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await expect(page.getByTestId("seller-vehicles")).toBeVisible();
    await expect(page.getByTestId("vehicle-card")).toHaveCount({ min: 0 });
  });

  test("debe mostrar badge de verificación", async ({ page }) => {
    await page.goto("/vendedor/dealer-verificado");

    await expect(page.getByTestId("verified-badge")).toBeVisible();
  });

  test("debe mostrar reviews del vendedor", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await expect(page.getByTestId("seller-reviews")).toBeVisible();
  });

  test("debe contactar vendedor desde perfil", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await page.getByRole("button", { name: /contactar/i }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
  });

  test("debe filtrar vehículos del vendedor", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await page.getByRole("combobox", { name: /ordenar/i }).click();
    await page.getByRole("option", { name: /menor precio/i }).click();

    await expect(page).toHaveURL(/sort=price_asc/);
  });
});
```

---

**Documentación Completada**
**Cobertura:** SELLER-001 a SELLER-005 + PROF-001 a PROF-004 (9/9 procesos = 100%)
