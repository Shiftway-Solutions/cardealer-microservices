/**
 * Dealer Profile Page
 *
 * Features:
 * - Dealer hero with logo, name, badges
 * - Contact info and location
 * - Inventory listing
 * - Reviews section
 * - About section
 *
 * Route: /dealers/[slug]
 */

'use client';

import * as React from 'react';
import { useSearchParams } from 'next/navigation';
import Image from 'next/image';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { VehicleCard } from '@/components/ui/vehicle-card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  MapPin,
  Phone,
  Mail,
  Clock,
  Star,
  Shield,
  CheckCircle,
  MessageSquare,
  Car,
  ExternalLink,
  Share2,
  AlertCircle,
} from 'lucide-react';
import { useDealerBySlug, useDealerLocations } from '@/hooks/use-dealers';
import { useReviewStats } from '@/hooks/use-reviews';
import { useVehiclesByDealer } from '@/hooks/use-vehicles';
import { ReviewsSection } from '@/components/reviews';
import { AppointmentCalendar } from '@/components/appointments';
import { ChatWidget } from '@/components/chat/ChatWidget';

// =============================================================================
// LOADING SKELETON
// =============================================================================

function DealerProfileSkeleton() {
  return (
    <div className="bg-muted/50 min-h-screen">
      {/* Cover Image Skeleton */}
      <Skeleton className="h-48 w-full md:h-64 lg:h-80" />

      {/* Header Skeleton */}
      <div className="relative z-10 container mx-auto -mt-16 px-4">
        <div className="bg-card rounded-xl p-6 shadow-lg">
          <div className="flex flex-col gap-6 md:flex-row">
            <Skeleton className="-mt-16 h-24 w-24 rounded-xl md:-mt-20 md:h-32 md:w-32" />
            <div className="flex-1 space-y-3">
              <Skeleton className="h-8 w-64" />
              <Skeleton className="h-4 w-96" />
              <div className="flex gap-4">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-24" />
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Content Skeleton */}
      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
          <div className="space-y-4 lg:col-span-2">
            <Skeleton className="h-10 w-64" />
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              {[1, 2, 3, 4].map(i => (
                <Skeleton key={i} className="h-64 w-full" />
              ))}
            </div>
          </div>
          <div className="space-y-6">
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-32 w-full" />
          </div>
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function DealerNotFound() {
  return (
    <div className="bg-muted/50 flex min-h-screen items-center justify-center">
      <Card className="mx-4 max-w-md">
        <CardContent className="p-8 text-center">
          <AlertCircle className="text-muted-foreground mx-auto mb-4 h-16 w-16" />
          <h1 className="text-foreground mb-2 text-2xl font-bold">Dealer no encontrado</h1>
          <p className="text-muted-foreground mb-6">
            El concesionario que buscas no existe o ya no está disponible.
          </p>
          <Button asChild className="bg-primary hover:bg-primary/90">
            <Link href="/dealers">Ver todos los dealers</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

import type { DayHours } from '@/services/dealers';
import type { Dealer } from '@/types';

function formatHours(hours?: DayHours): string {
  if (!hours || hours.isClosed) return 'Cerrado';
  return `${hours.open} - ${hours.close}`;
}

function getDealerBadges(dealer: Dealer): string[] {
  const badges: string[] = [];

  if (dealer.verificationStatus === 'verified') {
    badges.push('Verificado');
  }

  if (dealer.plan === 'visible') {
    badges.push('VISIBLE');
  } else if (dealer.plan === 'pro') {
    badges.push('PRO');
  } else if (dealer.plan === 'elite') {
    badges.push('ÉLITE');
  }

  if (dealer.responseRate && dealer.responseRate >= 90) {
    badges.push('Respuesta Rápida');
  }

  if (dealer.reviewCount && dealer.reviewCount >= 50 && dealer.rating && dealer.rating >= 4.5) {
    badges.push('Top Seller');
  }

  return badges;
}

// =============================================================================
// PAGE COMPONENT
// =============================================================================

interface PageProps {
  params: Promise<{ slug: string }>;
}

export default function DealerProfileClient({ params }: PageProps) {
  const { slug } = React.use(params);
  const searchParams = useSearchParams();
  const chatAutoOpen = searchParams.get('chat') === 'open';

  // Fetch dealer data
  const { data: dealerData, isLoading: dealerLoading, error: dealerError } = useDealerBySlug(slug);
  const { data: locations } = useDealerLocations(dealerData?.id || '');
  const { data: reviewStats } = useReviewStats('dealer', dealerData?.id || '');
  const { data: vehiclesData, isLoading: vehiclesLoading } = useVehiclesByDealer(
    dealerData?.id || ''
  );

  // Loading state
  if (dealerLoading) {
    return <DealerProfileSkeleton />;
  }

  // Error state
  if (dealerError || !dealerData) {
    return <DealerNotFound />;
  }

  // Transform dealer data to expected format
  const primaryLocation = locations?.find(l => l.isPrimary) || locations?.[0];
  const dealer = {
    id: dealerData.id,
    slug: slug,
    name: dealerData.businessName,
    logo: dealerData.logoUrl || '/placeholder-dealer.png',
    coverImage: dealerData.bannerUrl || '/placeholder-dealer-banner.jpg',
    description:
      dealerData.description ||
      `${dealerData.businessName} es un concesionario en ${dealerData.city}, ${dealerData.province}.`,
    shortDescription:
      dealerData.description?.slice(0, 100) || `Concesionario en ${dealerData.city}`,
    isVerified: dealerData.verificationStatus === 'verified',
    isPremium: dealerData.plan === 'pro' || dealerData.plan === 'elite',
    memberSince: dealerData.createdAt
      ? new Date(dealerData.createdAt).getFullYear().toString()
      : 'N/A',
    rating: dealerData.rating || reviewStats?.averageRating || 0,
    totalReviews: dealerData.reviewCount || reviewStats?.totalReviews || 0,
    totalVehicles: dealerData.currentActiveListings || 0,
    totalSold: 0, // Will be added when we have sales data
    responseTime: dealerData.avgResponseTimeMinutes
      ? dealerData.avgResponseTimeMinutes < 60
        ? `< ${dealerData.avgResponseTimeMinutes} min`
        : `< ${Math.round(dealerData.avgResponseTimeMinutes / 60)} hora${Math.round(dealerData.avgResponseTimeMinutes / 60) > 1 ? 's' : ''}`
      : 'N/A',
    location: {
      address: primaryLocation?.address || dealerData.address,
      city: primaryLocation?.city || dealerData.city,
      province: primaryLocation?.province || dealerData.province,
      coordinates:
        primaryLocation?.latitude && primaryLocation?.longitude
          ? { lat: primaryLocation.latitude, lng: primaryLocation.longitude }
          : undefined,
    },
    contact: {
      phone: dealerData.phone,
      whatsapp: dealerData.mobilePhone,
      email: dealerData.email,
      instagramUrl: dealerData.instagramUrl,
      facebookUrl: dealerData.facebookUrl,
      whatsAppNumber: dealerData.whatsAppNumber,
    },
    hours: primaryLocation?.businessHours
      ? {
          weekdays: formatHours(primaryLocation.businessHours.monday),
          saturday: formatHours(primaryLocation.businessHours.saturday),
          sunday: formatHours(primaryLocation.businessHours.sunday),
        }
      : {
          weekdays: '8:00 AM - 6:00 PM',
          saturday: '9:00 AM - 5:00 PM',
          sunday: 'Cerrado',
        },
    badges: getDealerBadges(dealerData),
    specialties: [], // Will be fetched from dealer specialties when available
  };

  // Get data from hooks
  const vehicles = vehiclesData?.items || [];

  return (
    <div className="bg-muted/50 min-h-screen">
      {/* Cover Image */}
      <div className="bg-muted-foreground/30 relative h-48 md:h-64 lg:h-80">
        <Image
          src={dealer.coverImage}
          alt={`${dealer.name} cover`}
          fill
          className="object-cover"
          priority
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent" />
      </div>

      {/* Dealer Header */}
      <div className="relative z-10 container mx-auto -mt-16 px-4">
        <div className="bg-card rounded-xl p-6 shadow-lg">
          <div className="flex flex-col gap-6 md:flex-row">
            {/* Logo */}
            <div className="relative -mt-16 h-24 w-24 overflow-hidden rounded-xl border-4 border-white bg-white shadow-lg md:-mt-20 md:h-32 md:w-32">
              <Image src={dealer.logo} alt={dealer.name} fill className="object-cover" />
            </div>

            {/* Info */}
            <div className="flex-1">
              <div className="mb-2 flex flex-wrap items-center gap-2">
                <h1 className="text-2xl font-bold md:text-3xl">{dealer.name}</h1>
                {dealer.isVerified && (
                  <Badge className="bg-primary/10 text-primary gap-1">
                    <CheckCircle className="h-3 w-3" />
                    Verificado
                  </Badge>
                )}
                {dealer.isPremium && <Badge className="bg-amber-100 text-amber-700">Premium</Badge>}
              </div>

              <p className="text-muted-foreground mb-4">{dealer.shortDescription}</p>

              {/* Quick Stats */}
              <div className="flex flex-wrap gap-4 text-sm">
                <div className="flex items-center gap-1.5">
                  <Star className="h-4 w-4 fill-amber-500 text-amber-500" />
                  <span className="font-semibold">{dealer.rating}</span>
                  <span className="text-muted-foreground">({dealer.totalReviews} reseñas)</span>
                </div>
                <div className="text-muted-foreground flex items-center gap-1.5">
                  <Car className="h-4 w-4" />
                  {dealer.totalVehicles} vehículos
                </div>
                <div className="text-muted-foreground flex items-center gap-1.5">
                  <MapPin className="h-4 w-4" />
                  {dealer.location.city}
                </div>
                <div className="text-muted-foreground flex items-center gap-1.5">
                  <Clock className="h-4 w-4" />
                  Responde {dealer.responseTime}
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="flex flex-col gap-2 md:min-w-[160px]">
              <Button className="bg-primary hover:bg-primary/90">
                <MessageSquare className="mr-2 h-4 w-4" />
                Contactar
              </Button>
              <Button variant="outline">
                <Phone className="mr-2 h-4 w-4" />
                Llamar
              </Button>
              <Button variant="ghost" size="sm">
                <Share2 className="mr-2 h-4 w-4" />
                Compartir
              </Button>
            </div>
          </div>

          {/* Badges */}
          {dealer.badges.length > 0 && (
            <div className="border-border mt-4 flex flex-wrap gap-2 border-t pt-4">
              {dealer.badges.map(badge => (
                <Badge key={badge} variant="outline" className="gap-1">
                  <Shield className="h-3 w-3" />
                  {badge}
                </Badge>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
          {/* Left Column - Main Content */}
          <div className="lg:col-span-2">
            <Tabs defaultValue="inventory" className="w-full">
              <TabsList className="w-full justify-start">
                <TabsTrigger value="inventory">Inventario ({dealer.totalVehicles})</TabsTrigger>
                <TabsTrigger value="reviews">Reseñas ({dealer.totalReviews})</TabsTrigger>
                <TabsTrigger value="about">Acerca de</TabsTrigger>
              </TabsList>

              {/* Inventory Tab */}
              <TabsContent value="inventory" className="mt-6">
                {vehiclesLoading ? (
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    {[1, 2, 3, 4].map(i => (
                      <Skeleton key={i} className="h-64 w-full" />
                    ))}
                  </div>
                ) : vehicles.length === 0 ? (
                  <Card>
                    <CardContent className="py-12 text-center">
                      <Car className="text-muted-foreground mx-auto mb-4 h-12 w-12" />
                      <p className="text-muted-foreground">
                        Este dealer no tiene vehículos disponibles actualmente.
                      </p>
                    </CardContent>
                  </Card>
                ) : (
                  <>
                    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                      {vehicles.slice(0, 4).map(vehicle => (
                        <VehicleCard key={vehicle.id} vehicle={vehicle} />
                      ))}
                    </div>

                    {vehicles.length > 4 && (
                      <div className="mt-6 text-center">
                        <Button variant="outline" size="lg" asChild>
                          <Link href={`/buscar?dealer=${dealer.id}`}>
                            Ver todos los vehículos ({vehicles.length})
                          </Link>
                        </Button>
                      </div>
                    )}
                  </>
                )}
              </TabsContent>

              {/* Reviews Tab */}
              <TabsContent value="reviews" className="mt-6">
                <ReviewsSection
                  targetId={dealerData.id}
                  targetType="dealer"
                  targetName={dealer.name}
                />
              </TabsContent>

              {/* About Tab */}
              <TabsContent value="about" className="mt-6">
                <Card>
                  <CardHeader>
                    <CardTitle>Acerca de {dealer.name}</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <p className="text-foreground whitespace-pre-line">{dealer.description}</p>

                    {dealer.specialties.length > 0 && (
                      <div>
                        <h4 className="mb-2 font-semibold">Especialidades</h4>
                        <div className="flex flex-wrap gap-2">
                          {dealer.specialties.map(specialty => (
                            <Badge key={specialty} variant="secondary">
                              {specialty}
                            </Badge>
                          ))}
                        </div>
                      </div>
                    )}

                    <div className="border-border grid grid-cols-2 gap-4 border-t pt-4">
                      <div>
                        <span className="text-muted-foreground">Miembro desde</span>
                        <p className="font-semibold">{dealer.memberSince}</p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">Vehículos vendidos</span>
                        <p className="font-semibold">{dealer.totalSold}+</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </TabsContent>
            </Tabs>
          </div>

          {/* Right Column - Contact Info */}
          <div className="space-y-6">
            {/* Contact Card */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Información de Contacto</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-start gap-3">
                  <MapPin className="text-muted-foreground mt-0.5 h-5 w-5" />
                  <div>
                    <p className="font-medium">{dealer.location.address}</p>
                    <p className="text-muted-foreground">
                      {dealer.location.city}, {dealer.location.province}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <Phone className="text-muted-foreground h-5 w-5" />
                  <a href={`tel:${dealer.contact.phone}`} className="hover:text-primary">
                    {dealer.contact.phone}
                  </a>
                </div>

                <div className="flex items-center gap-3">
                  <Mail className="text-muted-foreground h-5 w-5" />
                  <a href={`mailto:${dealer.contact.email}`} className="hover:text-primary">
                    {dealer.contact.email}
                  </a>
                </div>

                {dealer.contact.instagramUrl && (
                  <div className="flex items-center gap-3">
                    <svg
                      className="text-muted-foreground h-5 w-5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    >
                      <rect width="20" height="20" x="2" y="2" rx="5" ry="5" />
                      <path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z" />
                      <line x1="17.5" x2="17.51" y1="6.5" y2="6.5" />
                    </svg>
                    <a
                      href={`https://instagram.com/${dealer.contact.instagramUrl.replace(/^@/, '')}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:text-primary flex items-center gap-1"
                    >
                      {dealer.contact.instagramUrl}
                      <ExternalLink className="h-3 w-3" />
                    </a>
                  </div>
                )}

                {dealer.contact.facebookUrl && (
                  <div className="flex items-center gap-3">
                    <svg
                      className="text-muted-foreground h-5 w-5"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    >
                      <path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z" />
                    </svg>
                    <a
                      href={`https://facebook.com/${dealer.contact.facebookUrl.replace(/^@/, '')}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:text-primary flex items-center gap-1"
                    >
                      {dealer.contact.facebookUrl}
                      <ExternalLink className="h-3 w-3" />
                    </a>
                  </div>
                )}

                {dealer.contact.whatsAppNumber && (
                  <div className="flex items-center gap-3">
                    <Phone className="text-muted-foreground h-5 w-5" />
                    <a
                      href={`https://wa.me/${dealer.contact.whatsAppNumber.replace(/[^0-9]/g, '')}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="hover:text-primary flex items-center gap-1"
                    >
                      WhatsApp: {dealer.contact.whatsAppNumber}
                      <ExternalLink className="h-3 w-3" />
                    </a>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Hours Card */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Horario de Atención</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Lunes - Viernes</span>
                  <span className="font-medium">{dealer.hours.weekdays}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Sábado</span>
                  <span className="font-medium">{dealer.hours.saturday}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Domingo</span>
                  <span className="font-medium">{dealer.hours.sunday}</span>
                </div>
              </CardContent>
            </Card>

            {/* Schedule Visit - Interactive Calendar */}
            <AppointmentCalendar
              providerId={dealerData.id}
              providerName={dealer.name}
              location={`${dealer.location.address}, ${dealer.location.city}`}
            />
          </div>
        </div>
      </div>

      {/* Dealer-scoped Chatbot — auto-opens when ?chat=open */}
      {chatAutoOpen && <ChatWidget dealerId={dealerData.id} />}
    </div>
  );
}
