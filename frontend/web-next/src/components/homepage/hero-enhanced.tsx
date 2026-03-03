/**
 * HeroEnhanced Component
 *
 * Professional hero section inspired by CarGurus/AutoTrader/Cars.com
 * Features:
 * - Integrated search bar with make/model/price filters
 * - Animated trust badges
 * - Floating car statistics
 * - Smooth gradient backgrounds with animated shapes
 */

'use client';

import { useState } from 'react';
import { Search, ChevronDown, Shield, Star, CheckCircle2 } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { cn } from '@/lib/utils';

// =============================================================================
// TYPES
// =============================================================================

interface HeroEnhancedProps {
  className?: string;
}

interface TrustBadge {
  icon: typeof Shield;
  text: string;
}

// =============================================================================
// STATIC DATA
// =============================================================================

const TRUST_BADGES: TrustBadge[] = [
  { icon: Shield, text: 'Vendedores Verificados' },
  { icon: CheckCircle2, text: 'Historial Garantizado' },
  { icon: Star, text: 'Precios Transparentes' },
];

const POPULAR_MAKES = ['Toyota', 'Honda', 'Hyundai', 'Kia', 'Nissan', 'Mazda', 'Ford', 'Chevrolet'];

// =============================================================================
// ANIMATED BACKGROUND SHAPES
// =============================================================================

function AnimatedShapes() {
  return (
    <div className="pointer-events-none absolute inset-0 overflow-hidden">
      {/* Gradient Orbs */}
      <div className="from-primary/30 to-primary/5 animate-pulse-orb absolute -top-40 -right-40 h-96 w-96 rounded-full bg-gradient-to-br blur-3xl" />
      <div
        className="animate-pulse-orb absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-gradient-to-tr from-blue-500/20 to-purple-500/10 blur-3xl"
        style={{ animationDelay: '2s' }}
      />

      {/* Grid Pattern */}
      <div
        className="absolute inset-0 opacity-[0.02]"
        style={{
          backgroundImage: `
            linear-gradient(rgba(255,255,255,0.1) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255,255,255,0.1) 1px, transparent 1px)
          `,
          backgroundSize: '60px 60px',
        }}
      />

      {/* Floating Car Silhouettes */}
      <div className="animate-float-y absolute top-1/4 right-20 opacity-5">
        <svg width="200" height="80" viewBox="0 0 200 80" fill="white">
          <path d="M160 50c0-16.569-13.431-30-30-30H70c-16.569 0-30 13.431-30 30v10h120V50z" />
          <circle cx="60" cy="60" r="15" />
          <circle cx="140" cy="60" r="15" />
        </svg>
      </div>
    </div>
  );
}

// =============================================================================
// SEARCH BAR COMPONENT
// =============================================================================

function HeroSearchBar() {
  const [condition, setCondition] = useState('');
  const [make, setMake] = useState('');
  const [model, setModel] = useState('');
  const router = useRouter();

  const handleSearch = () => {
    const params = new URLSearchParams();
    if (condition) params.set('condition', condition);
    if (make) params.set('make', make);
    if (model) params.set('model', model);
    const qs = params.toString();
    router.push(`/vehiculos${qs ? `?${qs}` : ''}`);
  };

  // Models depend on selected make
  const modelsByMake: Record<string, string[]> = {
    Toyota: ['Corolla', 'Camry', 'RAV4', 'Hilux', 'Land Cruiser', 'Fortuner', 'Yaris', '4Runner'],
    Honda: ['Civic', 'Accord', 'CR-V', 'HR-V', 'Pilot', 'Odyssey', 'Fit'],
    Hyundai: ['Tucson', 'Santa Fe', 'Elantra', 'Sonata', 'Kona', 'Palisade', 'Accent'],
    Kia: ['Sportage', 'Sorento', 'Seltos', 'K5', 'Carnival', 'Soul', 'Forte'],
    Nissan: ['Sentra', 'Altima', 'Rogue', 'Pathfinder', 'Kicks', 'Frontier', 'Murano'],
    Mazda: ['CX-5', 'CX-30', 'CX-9', 'Mazda3', 'Mazda6', 'MX-5'],
    Ford: ['F-150', 'Explorer', 'Escape', 'Bronco', 'Ranger', 'Mustang', 'Edge'],
    Chevrolet: ['Silverado', 'Equinox', 'Tahoe', 'Traverse', 'Malibu', 'Camaro', 'Colorado'],
  };

  const availableModels = make ? modelsByMake[make] || [] : [];

  return (
    <div className="animate-slide-up mx-auto w-full max-w-4xl" style={{ animationDelay: '300ms' }}>
      {/* Main Search Container */}
      <div className="bg-card rounded-2xl border border-white/20 p-2 shadow-2xl shadow-black/10">
        <div className="flex flex-col gap-2 md:flex-row">
          {/* Condition Select */}
          <div className="group relative flex-1">
            <select
              value={condition}
              onChange={e => setCondition(e.target.value)}
              className="border-border bg-muted text-foreground hover:border-muted-foreground/50 focus:border-primary focus:bg-background focus:ring-primary/20 h-14 w-full cursor-pointer appearance-none rounded-xl border px-4 pr-10 font-medium transition-all focus:ring-2 focus:outline-none"
            >
              <option value="">Estado</option>
              <option value="nuevo">Nuevo</option>
              <option value="recien-importado">Recién Importado</option>
              <option value="usado">Usado</option>
            </select>
            <ChevronDown className="text-muted-foreground pointer-events-none absolute top-1/2 right-3 h-5 w-5 -translate-y-1/2" />
          </div>

          {/* Make Select */}
          <div className="group relative flex-1">
            <select
              value={make}
              onChange={e => {
                setMake(e.target.value);
                setModel(''); // Reset model when make changes
              }}
              className="border-border bg-muted text-foreground hover:border-muted-foreground/50 focus:border-primary focus:bg-background focus:ring-primary/20 h-14 w-full cursor-pointer appearance-none rounded-xl border px-4 pr-10 font-medium transition-all focus:ring-2 focus:outline-none"
            >
              <option value="">Marca</option>
              {POPULAR_MAKES.map(m => (
                <option key={m} value={m}>
                  {m}
                </option>
              ))}
            </select>
            <ChevronDown className="text-muted-foreground pointer-events-none absolute top-1/2 right-3 h-5 w-5 -translate-y-1/2" />
          </div>

          {/* Model Select */}
          <div className="group relative flex-1">
            <select
              value={model}
              onChange={e => setModel(e.target.value)}
              disabled={!make}
              className="border-border bg-muted text-foreground hover:border-muted-foreground/50 focus:border-primary focus:bg-background focus:ring-primary/20 h-14 w-full cursor-pointer appearance-none rounded-xl border px-4 pr-10 font-medium transition-all focus:ring-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
            >
              <option value="">{make ? 'Modelo' : 'Selecciona marca'}</option>
              {availableModels.map(m => (
                <option key={m} value={m}>
                  {m}
                </option>
              ))}
            </select>
            <ChevronDown className="text-muted-foreground pointer-events-none absolute top-1/2 right-3 h-5 w-5 -translate-y-1/2" />
          </div>

          {/* Search Button */}
          <button
            onClick={handleSearch}
            className="bg-primary text-primary-foreground shadow-primary/30 hover:bg-primary/90 hover:shadow-primary/40 flex h-14 cursor-pointer items-center justify-center gap-2 rounded-xl px-10 font-semibold whitespace-nowrap shadow-lg transition-all duration-300 hover:shadow-xl"
          >
            <Search className="h-5 w-5" />
            <span>Buscar</span>
          </button>
        </div>
      </div>

      {/* Quick Filters */}
      <div
        className="animate-fade-in mt-4 flex flex-wrap justify-center gap-2"
        style={{ animationDelay: '600ms' }}
      >
        {['SUV', 'Sedán', 'Camioneta', 'Deportivo', 'Híbrido', 'Eléctrico'].map(filter => (
          <Link
            key={filter}
            href={`/vehiculos?body_type=${encodeURIComponent(filter)}`}
            className="hover:border-primary hover:bg-primary/90 hover:text-primary-foreground hover:shadow-primary/20 rounded-full border border-white/30 bg-white/15 px-5 py-2.5 text-sm font-semibold text-white shadow-sm backdrop-blur-md transition-all duration-200 hover:shadow-md"
          >
            {filter}
          </Link>
        ))}
      </div>
    </div>
  );
}

// =============================================================================
// TRUST BADGES
// =============================================================================

function TrustBadgesBar({ badges }: { badges: TrustBadge[] }) {
  return (
    <div
      className="animate-fade-in flex flex-wrap justify-center gap-4 md:gap-8"
      style={{ animationDelay: '800ms' }}
    >
      {badges.map((badge, index) => (
        <div
          key={badge.text}
          className="animate-slide-right flex items-center gap-2 text-white"
          style={{ animationDelay: `${900 + index * 100}ms` }}
        >
          <badge.icon className="text-primary h-5 w-5" />
          <span className="text-sm font-medium">{badge.text}</span>
        </div>
      ))}
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function HeroEnhanced({ className }: HeroEnhancedProps) {
  return (
    <section
      className={cn(
        'relative flex min-h-[90vh] flex-col items-center justify-center overflow-hidden',
        'bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900',
        className
      )}
    >
      {/* Animated Background */}
      <AnimatedShapes />

      {/* Content Container */}
      <div className="relative z-10 mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 md:py-24 lg:px-8">
        {/* Headline */}
        <div className="animate-slide-down mb-8 text-center md:mb-12">
          <h1 className="mb-4 text-4xl leading-[1.1] font-bold tracking-tight text-white sm:text-5xl md:mb-6 md:text-6xl lg:text-7xl">
            Tu próximo vehículo
            <br />
            está en <span className="text-primary">OKLA</span>
          </h1>
          <p className="mx-auto max-w-2xl text-lg leading-relaxed text-white/90 md:text-xl">
            El nuevo marketplace de vehículos en República Dominicana. Encuentra, compara y compra
            con total confianza.
          </p>
        </div>

        {/* Search Bar */}
        <HeroSearchBar />

        {/* Trust Badges */}
        <div className="mt-12 md:mt-16">
          <TrustBadgesBar badges={TRUST_BADGES} />
        </div>
      </div>

      {/* Scroll Indicator */}
      <div
        className="animate-fade-in absolute bottom-8 left-1/2 -translate-x-1/2"
        style={{ animationDelay: '1200ms' }}
      >
        <div className="animate-bounce-gentle flex flex-col items-center gap-2 text-white/80">
          <span className="text-xs font-medium tracking-widest uppercase">Explorar</span>
          <ChevronDown className="h-5 w-5" />
        </div>
      </div>
    </section>
  );
}

// Named export for convenience
export { HeroEnhanced as default };
