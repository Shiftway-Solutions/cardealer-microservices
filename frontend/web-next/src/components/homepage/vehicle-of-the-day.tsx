'use client';

import { useMemo, useState, useEffect, memo } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Trophy, Calendar, Gauge, MapPin, Clock, ArrowRight } from 'lucide-react';
import { formatPrice } from '@/lib/format';

interface Vehicle {
  id: string;
  slug?: string;
  title: string;
  price: number;
  year?: number;
  mileage?: number;
  mileageUnit?: string;
  location?: string;
  imageUrl?: string;
  thumbnailUrl?: string;
  make?: string;
  model?: string;
  fuelType?: string;
  transmission?: string;
  photoCount?: number;
  sellerVerified?: boolean;
}

interface VehicleOfTheDayProps {
  vehicles: Vehicle[];
}

function formatMileage(km: number): string {
  return new Intl.NumberFormat('es-DO').format(km);
}

// Deterministic seed based on date (changes daily at midnight DR time UTC-4)
function getDailySeed(): number {
  const now = new Date();
  // Adjust to DR timezone (UTC-4)
  const drOffset = -4 * 60;
  const utcOffset = now.getTimezoneOffset();
  const drTime = new Date(now.getTime() + (utcOffset + drOffset) * 60000);

  const year = drTime.getFullYear();
  const month = drTime.getMonth() + 1;
  const day = drTime.getDate();
  return year * 10000 + month * 100 + day; // e.g. 20260306
}

// Simple seeded PRNG (mulberry32)
function mulberry32(seed: number): () => number {
  let a = seed | 0;
  return function () {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function getTimeUntilMidnightDR(): {
  hours: number;
  minutes: number;
  seconds: number;
} {
  const now = new Date();
  const drOffset = -4 * 60;
  const utcOffset = now.getTimezoneOffset();
  const drTime = new Date(now.getTime() + (utcOffset + drOffset) * 60000);

  const endOfDay = new Date(drTime);
  endOfDay.setHours(23, 59, 59, 999);

  const diff = endOfDay.getTime() - drTime.getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { hours, minutes, seconds };
}

/**
 * CWV FIX: Isolated countdown component — re-renders every second WITHOUT
 * triggering a re-render of the parent VehicleOfTheDay component.
 * This prevents ~60 unnecessary re-renders per minute of the entire card.
 */
const CountdownTimer = memo(function CountdownTimer() {
  const [countdown, setCountdown] = useState(getTimeUntilMidnightDR());

  useEffect(() => {
    const timer = setInterval(() => {
      setCountdown(getTimeUntilMidnightDR());
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  return (
    <div className="text-muted-foreground mt-4 flex items-center gap-2 text-sm">
      <Clock className="h-4 w-4" />
      <span>
        Nuevo vehículo en{' '}
        <strong>
          {countdown.hours}h {countdown.minutes}m {countdown.seconds}s
        </strong>
      </span>
    </div>
  );
});

export function VehicleOfTheDay({ vehicles }: VehicleOfTheDayProps) {
  // Select vehicle of the day deterministically
  const selectedVehicle = useMemo(() => {
    if (!vehicles || vehicles.length === 0) return null;

    // Filter eligible vehicles
    const eligible = vehicles.filter(
      v => v.price > 0 && (v.photoCount === undefined || v.photoCount >= 3) && v.imageUrl
    );

    if (eligible.length === 0) return vehicles[0] || null;

    const seed = getDailySeed();
    const rng = mulberry32(seed);
    const index = Math.floor(rng() * eligible.length);
    return eligible[index];
  }, [vehicles]);

  if (!selectedVehicle) return null;

  const imageUrl =
    selectedVehicle.imageUrl || selectedVehicle.thumbnailUrl || '/placeholder-vehicle.jpg';

  const vehicleLink = selectedVehicle.slug
    ? `/vehiculos/${selectedVehicle.slug}`
    : `/vehiculos/${selectedVehicle.id}`;

  return (
    <section className="container mx-auto px-4 py-8">
      <Card className="overflow-hidden border-2 border-amber-200 bg-gradient-to-r from-amber-50 to-orange-50 dark:from-amber-950/20 dark:to-orange-950/20">
        <CardContent className="p-0">
          <div className="grid md:grid-cols-2">
            {/* Image */}
            <div className="relative aspect-[16/10] md:aspect-auto">
              <Image
                src={imageUrl}
                alt={selectedVehicle.title}
                fill
                className="object-cover"
                sizes="(max-width: 768px) 100vw, 50vw"
                priority
              />
              {/* Badge overlay */}
              <div className="absolute top-4 left-4">
                <Badge className="bg-amber-500 px-3 py-1.5 text-sm font-bold text-white shadow-lg">
                  <Trophy className="mr-1.5 h-4 w-4" />
                  Vehículo del Día
                </Badge>
              </div>
            </div>

            {/* Info */}
            <div className="flex flex-col justify-center p-6 md:p-8">
              <div className="mb-2">
                <Badge variant="outline" className="border-amber-300 text-amber-700">
                  <Trophy className="mr-1 h-3 w-3" />
                  Selección OKLA
                </Badge>
              </div>

              <h3 className="text-foreground mb-2 text-2xl font-bold md:text-3xl">
                {selectedVehicle.title}
              </h3>

              <p className="text-primary mb-4 text-3xl font-bold md:text-4xl">
                {formatPrice(selectedVehicle.price)}
              </p>

              {/* Specs */}
              <div className="text-muted-foreground mb-6 flex flex-wrap gap-3 text-sm">
                {selectedVehicle.year && (
                  <span className="flex items-center gap-1">
                    <Calendar className="h-4 w-4" />
                    {selectedVehicle.year}
                  </span>
                )}
                {selectedVehicle.mileage !== undefined && (
                  <span className="flex items-center gap-1">
                    <Gauge className="h-4 w-4" />
                    {formatMileage(selectedVehicle.mileage)}{' '}
                    {'km'}
                  </span>
                )}
                {selectedVehicle.location && (
                  <span className="flex items-center gap-1">
                    <MapPin className="h-4 w-4" />
                    {selectedVehicle.location}
                  </span>
                )}
                {selectedVehicle.transmission && (
                  <span className="capitalize">{selectedVehicle.transmission}</span>
                )}
                {selectedVehicle.fuelType && (
                  <span className="capitalize">{selectedVehicle.fuelType}</span>
                )}
              </div>

              {/* CTA */}
              <Link href={vehicleLink}>
                <Button size="lg" className="w-full sm:w-auto">
                  Ver detalle
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Button>
              </Link>

              {/* Countdown — isolated component to avoid re-rendering the entire card */}
              <CountdownTimer />
            </div>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}
