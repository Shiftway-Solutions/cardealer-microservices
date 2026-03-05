/**
 * Dealer Portal Client — Premium Branded Storefront
 *
 * An elegant, shareable public portal for each dealer:
 * - Hero banner with dealer branding
 * - Full vehicle inventory with filters
 * - AI Chatbot for inventory questions
 * - Contact info, locations, reviews
 * - Social sharing
 * - Responsive & optimized for slow connections
 */

'use client';

import { useState, useMemo, useCallback } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import {
  Search,
  MapPin,
  Phone,
  Mail,
  Globe,
  Star,
  Share2,
  MessageCircle,
  Clock,
  CheckCircle2,
  Grid3X3,
  List,
  Car,
  Shield,
  Award,
  TrendingUp,
  Copy,
  Facebook,
  Instagram,
  X,
  Bot,
  Send,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';
import type { DealerDto } from '@/services/dealers';

// ============================================================
// TYPES
// ============================================================

interface PortalVehicle {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency: string;
  mileage: number;
  transmission: string;
  fuelType: string;
  condition: string;
  imageUrl?: string;
  images?: string[];
  has360View?: boolean;
  isFeatured?: boolean;
  slug?: string;
  province?: string;
  city?: string;
  createdAt?: string;
}

interface DealerPortalClientProps {
  slug: string;
  initialDealer: DealerDto;
}

// ============================================================
// CHATBOT COMPONENT
// ============================================================

function DealerChatbot({
  dealerName,
  dealerId,
  dealerEmail: _dealerEmail,
}: {
  dealerName: string;
  dealerId: string;
  dealerEmail?: string;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<Array<{ role: 'user' | 'bot'; text: string }>>([
    {
      role: 'bot',
      text: `¡Hola! 👋 Soy el asistente virtual de ${dealerName}. Puedo ayudarte con información sobre nuestro inventario, precios, financiamiento y más. ¿En qué te puedo ayudar?`,
    },
  ]);
  const [input, setInput] = useState('');
  const [isTyping, setIsTyping] = useState(false);

  const sendMessage = useCallback(async () => {
    if (!input.trim()) return;
    const userMsg = input.trim();
    setInput('');
    setMessages(prev => [...prev, { role: 'user', text: userMsg }]);
    setIsTyping(true);

    try {
      // Use apiClient for proper auth/CSRF handling
      const {
        default: { post },
      } = await import('@/lib/api-client').then(m => ({ default: m.apiClient }));
      const res = await post('/api/chatbot/message', {
        message: userMsg,
        dealerId,
        context: 'dealer_portal',
      });

      const data = res.data;
      const responseText =
        data?.data?.response || data?.response || 'Lo siento, no pude procesar tu pregunta.';
      setMessages(prev => [...prev, { role: 'bot', text: responseText }]);
    } catch {
      setMessages(prev => [
        ...prev,
        { role: 'bot', text: 'Error de conexión. Verifica tu internet e intenta de nuevo.' },
      ]);
    } finally {
      setIsTyping(false);
    }
  }, [input, dealerId]);

  if (!isOpen) {
    return (
      <button
        onClick={() => setIsOpen(true)}
        className="fixed right-6 bottom-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-gradient-to-br from-emerald-500 to-teal-600 text-white shadow-2xl transition-transform hover:scale-110 active:scale-95"
        aria-label="Abrir chat"
      >
        <Bot className="h-7 w-7" />
      </button>
    );
  }

  return (
    <div className="fixed right-4 bottom-4 z-50 flex h-[500px] w-[380px] flex-col rounded-2xl border bg-white shadow-2xl sm:right-6 sm:bottom-6">
      {/* Header */}
      <div className="flex items-center justify-between rounded-t-2xl bg-gradient-to-r from-emerald-600 to-teal-600 px-4 py-3 text-white">
        <div className="flex items-center gap-2">
          <Bot className="h-5 w-5" />
          <div>
            <p className="text-sm font-semibold">{dealerName}</p>
            <p className="text-xs text-emerald-100">Asistente Virtual</p>
          </div>
        </div>
        <button onClick={() => setIsOpen(false)} className="rounded-full p-1 hover:bg-white/20">
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Messages */}
      <div className="flex-1 space-y-3 overflow-y-auto p-4">
        {messages.map((msg, i) => (
          <div
            key={i}
            className={cn('flex', msg.role === 'user' ? 'justify-end' : 'justify-start')}
          >
            <div
              className={cn(
                'max-w-[80%] rounded-2xl px-4 py-2 text-sm',
                msg.role === 'user' ? 'bg-emerald-600 text-white' : 'bg-gray-100 text-gray-800'
              )}
            >
              {msg.text}
            </div>
          </div>
        ))}
        {isTyping && (
          <div className="flex justify-start">
            <div className="flex gap-1 rounded-2xl bg-gray-100 px-4 py-3">
              <span
                className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                style={{ animationDelay: '0ms' }}
              />
              <span
                className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                style={{ animationDelay: '150ms' }}
              />
              <span
                className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                style={{ animationDelay: '300ms' }}
              />
            </div>
          </div>
        )}
      </div>

      {/* Input */}
      <div className="border-t p-3">
        <div className="flex gap-2">
          <Input
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && sendMessage()}
            placeholder="Escribe tu pregunta..."
            className="flex-1"
          />
          <Button size="icon" onClick={sendMessage} className="bg-emerald-600 hover:bg-emerald-700">
            <Send className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}

// ============================================================
// SHARE DIALOG
// ============================================================

function ShareDialog({ dealerName, slug }: { dealerName: string; slug: string }) {
  const portalUrl = `${typeof window !== 'undefined' ? window.location.origin : 'https://okla.com.do'}/portal/${slug}`;

  const copyLink = () => {
    navigator.clipboard.writeText(portalUrl);
    toast.success('¡Enlace copiado!');
  };

  const shareWhatsApp = () => {
    window.open(
      `https://wa.me/?text=${encodeURIComponent(`Mira el inventario de ${dealerName} en OKLA: ${portalUrl}`)}`,
      '_blank'
    );
  };

  const shareFacebook = () => {
    window.open(
      `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(portalUrl)}`,
      '_blank'
    );
  };

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2">
          <Share2 className="h-4 w-4" />
          Compartir Portal
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Compartir Portal de {dealerName}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <Input value={portalUrl} readOnly className="flex-1 text-sm" />
            <Button size="sm" onClick={copyLink} variant="outline">
              <Copy className="h-4 w-4" />
            </Button>
          </div>
          <div className="flex gap-3">
            <Button
              onClick={shareWhatsApp}
              className="flex-1 gap-2 bg-green-600 hover:bg-green-700"
            >
              <MessageCircle className="h-4 w-4" />
              WhatsApp
            </Button>
            <Button onClick={shareFacebook} className="flex-1 gap-2 bg-blue-600 hover:bg-blue-700">
              <Facebook className="h-4 w-4" />
              Facebook
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ============================================================
// VEHICLE CARD
// ============================================================

function PortalVehicleCard({ vehicle }: { vehicle: PortalVehicle }) {
  const formatPrice = (price: number, currency: string) => {
    if (currency === 'USD') return `US$${price.toLocaleString()}`;
    return `RD$${price.toLocaleString()}`;
  };

  return (
    <Link href={`/vehiculos/${vehicle.slug || vehicle.id}`}>
      <Card className="group h-full overflow-hidden transition-all duration-300 hover:-translate-y-1 hover:shadow-xl">
        <div className="relative aspect-[4/3] overflow-hidden bg-gray-100">
          {vehicle.imageUrl ? (
            <Image
              src={vehicle.imageUrl}
              alt={vehicle.title}
              fill
              sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
              className="object-cover transition-transform duration-500 group-hover:scale-105"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full items-center justify-center">
              <Car className="h-12 w-12 text-gray-300" />
            </div>
          )}
          {vehicle.has360View && (
            <Badge className="absolute top-2 left-2 bg-purple-600 text-xs">🔄 360°</Badge>
          )}
          {vehicle.isFeatured && (
            <Badge className="absolute top-2 right-2 bg-amber-500 text-xs">⭐ Destacado</Badge>
          )}
        </div>
        <CardContent className="p-4">
          <h3 className="truncate text-sm font-semibold text-gray-900 group-hover:text-emerald-600">
            {vehicle.year} {vehicle.make} {vehicle.model}
          </h3>
          <p className="mt-1 text-lg font-bold text-emerald-600">
            {formatPrice(vehicle.price, vehicle.currency)}
          </p>
          <div className="mt-2 flex flex-wrap gap-2 text-xs text-gray-500">
            <span>{vehicle.mileage?.toLocaleString()} km</span>
            <span>•</span>
            <span>{vehicle.transmission}</span>
            <span>•</span>
            <span>{vehicle.fuelType}</span>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}

// ============================================================
// MAIN PORTAL COMPONENT
// ============================================================

export function DealerPortalClient({ slug, initialDealer }: DealerPortalClientProps) {
  const dealer = initialDealer;
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState('newest');
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [filterCondition, setFilterCondition] = useState('all');
  const [filterTransmission, setFilterTransmission] = useState('all');
  const [vehicles, setVehicles] = useState<PortalVehicle[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [totalVehicles, setTotalVehicles] = useState(0);

  // Fetch dealer vehicles
  useState(() => {
    async function fetchVehicles() {
      try {
        const res = await fetch(
          `/api/vehicles?sellerId=${dealer.id}&status=Active&pageSize=50&sortBy=createdAt&sortDirection=desc`
        );
        if (res.ok) {
          const data = await res.json();
          const items = data.data?.items || data.items || [];
          setVehicles(
            items.map((v: Record<string, unknown>) => ({
              id: v.id as string,
              title: `${v.year} ${v.make} ${v.model}`,
              make: v.make as string,
              model: v.model as string,
              year: v.year as number,
              price: v.price as number,
              currency: (v.currency as string) || 'DOP',
              mileage: v.mileage as number,
              transmission: v.transmission as string,
              fuelType: v.fuelType as string,
              condition: v.condition as string,
              imageUrl: (v.primaryImageUrl as string) || (v.imageUrl as string),
              images: v.images as string[],
              has360View: v.has360View as boolean,
              isFeatured: v.isFeatured as boolean,
              slug: v.slug as string,
              province: v.province as string,
              city: v.city as string,
              createdAt: v.createdAt as string,
            }))
          );
          setTotalVehicles(data.data?.pagination?.totalItems || items.length);
        }
      } catch {
        // silently fail, show empty
      } finally {
        setIsLoading(false);
      }
    }
    fetchVehicles();
  });

  // Filtered and sorted vehicles
  const filteredVehicles = useMemo(() => {
    let result = [...vehicles];

    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        v =>
          v.title.toLowerCase().includes(q) ||
          v.make.toLowerCase().includes(q) ||
          v.model.toLowerCase().includes(q)
      );
    }

    if (filterCondition !== 'all') {
      result = result.filter(v => v.condition?.toLowerCase() === filterCondition);
    }

    if (filterTransmission !== 'all') {
      result = result.filter(v => v.transmission?.toLowerCase() === filterTransmission);
    }

    switch (sortBy) {
      case 'price-low':
        result.sort((a, b) => a.price - b.price);
        break;
      case 'price-high':
        result.sort((a, b) => b.price - a.price);
        break;
      case 'year-new':
        result.sort((a, b) => b.year - a.year);
        break;
      case 'mileage':
        result.sort((a, b) => a.mileage - b.mileage);
        break;
      default:
        // newest first
        break;
    }

    return result;
  }, [vehicles, searchQuery, sortBy, filterCondition, filterTransmission]);

  const planBadge = () => {
    switch (dealer.currentPlan) {
      case 'elite':
        return { label: 'Élite', color: 'bg-gradient-to-r from-amber-500 to-yellow-500' };
      case 'pro':
        return { label: 'Pro', color: 'bg-gradient-to-r from-purple-500 to-indigo-500' };
      case 'visible':
        return { label: 'Visible', color: 'bg-gradient-to-r from-blue-500 to-cyan-500' };
      default:
        return { label: 'Dealer', color: 'bg-gray-500' };
    }
  };

  const badge = planBadge();

  return (
    <div className="min-h-screen bg-gray-50">
      {/* ===== HERO BANNER ===== */}
      <section className="relative overflow-hidden">
        {/* Banner Background */}
        <div className="h-64 bg-gradient-to-br from-gray-900 via-emerald-900 to-teal-900 sm:h-80 lg:h-96">
          {dealer.bannerUrl && (
            <Image
              src={dealer.bannerUrl}
              alt={`Banner de ${dealer.businessName}`}
              fill
              className="object-cover opacity-40"
              priority
            />
          )}
          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
        </div>

        {/* Dealer Info Overlay */}
        <div className="absolute inset-x-0 bottom-0 px-4 pb-6 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-7xl">
            <div className="flex flex-col items-start gap-4 sm:flex-row sm:items-end sm:gap-6">
              {/* Logo */}
              <div className="relative -mb-12 h-24 w-24 overflow-hidden rounded-2xl border-4 border-white bg-white shadow-xl sm:-mb-16 sm:h-32 sm:w-32">
                {dealer.logoUrl ? (
                  <Image
                    src={dealer.logoUrl}
                    alt={dealer.businessName}
                    fill
                    className="object-contain p-2"
                    priority
                  />
                ) : (
                  <div className="flex h-full items-center justify-center bg-gradient-to-br from-emerald-100 to-teal-100">
                    <span className="text-3xl font-bold text-emerald-700">
                      {dealer.businessName?.substring(0, 2).toUpperCase()}
                    </span>
                  </div>
                )}
              </div>

              {/* Name & Meta */}
              <div className="flex-1 text-white">
                <div className="flex flex-wrap items-center gap-2">
                  <h1 className="text-2xl font-bold sm:text-3xl lg:text-4xl">
                    {dealer.businessName}
                  </h1>
                  <Badge className={cn('text-xs text-white', badge.color)}>{badge.label}</Badge>
                  {dealer.verificationStatus === 'verified' && (
                    <Badge className="gap-1 bg-emerald-500 text-xs">
                      <CheckCircle2 className="h-3 w-3" />
                      Verificado
                    </Badge>
                  )}
                </div>
                <div className="mt-2 flex flex-wrap items-center gap-4 text-sm text-gray-200">
                  {(dealer.city || dealer.province) && (
                    <span className="flex items-center gap-1">
                      <MapPin className="h-4 w-4" />
                      {[dealer.city, dealer.province].filter(Boolean).join(', ')}
                    </span>
                  )}
                  {dealer.rating && (
                    <span className="flex items-center gap-1">
                      <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
                      {dealer.rating.toFixed(1)} ({dealer.reviewCount} reseñas)
                    </span>
                  )}
                  {dealer.avgResponseTimeMinutes && (
                    <span className="flex items-center gap-1">
                      <Clock className="h-4 w-4" />
                      Responde en{' '}
                      {dealer.avgResponseTimeMinutes < 60
                        ? `${dealer.avgResponseTimeMinutes} min`
                        : `${Math.round(dealer.avgResponseTimeMinutes / 60)}h`}
                    </span>
                  )}
                </div>
              </div>

              {/* Actions */}
              <div className="flex gap-2">
                <ShareDialog dealerName={dealer.businessName} slug={slug} />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ===== STATS BAR ===== */}
      <section className="border-b bg-white pt-16 pb-4 sm:pt-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex flex-wrap items-center justify-between gap-4">
            {/* Contact Row */}
            <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600">
              {dealer.phone && (
                <a
                  href={`tel:${dealer.phone}`}
                  className="flex items-center gap-1 hover:text-emerald-600"
                >
                  <Phone className="h-4 w-4" />
                  {dealer.phone}
                </a>
              )}
              {dealer.email && (
                <a
                  href={`mailto:${dealer.email}`}
                  className="flex items-center gap-1 hover:text-emerald-600"
                >
                  <Mail className="h-4 w-4" />
                  {dealer.email}
                </a>
              )}
              {dealer.website && (
                <a
                  href={dealer.website}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-1 hover:text-emerald-600"
                >
                  <Globe className="h-4 w-4" />
                  Sitio Web
                </a>
              )}
              {dealer.whatsAppNumber && (
                <a
                  href={`https://wa.me/${dealer.whatsAppNumber.replace(/[^0-9]/g, '')}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-1 text-green-600 hover:text-green-700"
                >
                  <MessageCircle className="h-4 w-4" />
                  WhatsApp
                </a>
              )}
            </div>

            {/* Stats */}
            <div className="flex gap-6">
              <div className="text-center">
                <p className="text-xl font-bold text-gray-900">{totalVehicles}</p>
                <p className="text-xs text-gray-500">Vehículos</p>
              </div>
              {dealer.responseRate && (
                <div className="text-center">
                  <p className="text-xl font-bold text-emerald-600">{dealer.responseRate}%</p>
                  <p className="text-xs text-gray-500">Respuesta</p>
                </div>
              )}
              {dealer.reviewCount && (
                <div className="text-center">
                  <p className="text-xl font-bold text-amber-500">{dealer.reviewCount}</p>
                  <p className="text-xs text-gray-500">Reseñas</p>
                </div>
              )}
            </div>
          </div>

          {/* Description */}
          {dealer.description && (
            <p className="mt-4 max-w-3xl text-sm leading-relaxed text-gray-600">
              {dealer.description}
            </p>
          )}

          {/* Social Links */}
          <div className="mt-3 flex gap-3">
            {dealer.facebookUrl && (
              <a
                href={dealer.facebookUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-gray-400 hover:text-blue-600"
              >
                <Facebook className="h-5 w-5" />
              </a>
            )}
            {dealer.instagramUrl && (
              <a
                href={dealer.instagramUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-gray-400 hover:text-pink-500"
              >
                <Instagram className="h-5 w-5" />
              </a>
            )}
          </div>
        </div>
      </section>

      {/* ===== INVENTORY SECTION ===== */}
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          {/* Toolbar */}
          <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-1 items-center gap-3">
              <div className="relative flex-1 sm:max-w-md">
                <Search className="absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400" />
                <Input
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  placeholder="Buscar vehículos..."
                  className="pl-10"
                />
              </div>
              <Select value={sortBy} onValueChange={setSortBy}>
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Ordenar por" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="newest">Más Recientes</SelectItem>
                  <SelectItem value="price-low">Menor Precio</SelectItem>
                  <SelectItem value="price-high">Mayor Precio</SelectItem>
                  <SelectItem value="year-new">Más Nuevos</SelectItem>
                  <SelectItem value="mileage">Menor Kilometraje</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-2">
              {/* Filters */}
              <Select value={filterCondition} onValueChange={setFilterCondition}>
                <SelectTrigger className="w-[140px]">
                  <SelectValue placeholder="Condición" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todas</SelectItem>
                  <SelectItem value="new">Nuevo</SelectItem>
                  <SelectItem value="used">Usado</SelectItem>
                  <SelectItem value="certified">Certificado</SelectItem>
                </SelectContent>
              </Select>

              <Select value={filterTransmission} onValueChange={setFilterTransmission}>
                <SelectTrigger className="w-[140px]">
                  <SelectValue placeholder="Transmisión" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todas</SelectItem>
                  <SelectItem value="automatic">Automática</SelectItem>
                  <SelectItem value="manual">Manual</SelectItem>
                </SelectContent>
              </Select>

              {/* View mode */}
              <div className="hidden gap-1 rounded-lg border p-1 sm:flex">
                <button
                  onClick={() => setViewMode('grid')}
                  className={cn(
                    'rounded-md p-1.5',
                    viewMode === 'grid' ? 'bg-emerald-100 text-emerald-600' : 'text-gray-400'
                  )}
                >
                  <Grid3X3 className="h-4 w-4" />
                </button>
                <button
                  onClick={() => setViewMode('list')}
                  className={cn(
                    'rounded-md p-1.5',
                    viewMode === 'list' ? 'bg-emerald-100 text-emerald-600' : 'text-gray-400'
                  )}
                >
                  <List className="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>

          {/* Results count */}
          <p className="mb-4 text-sm text-gray-500">
            {filteredVehicles.length} vehículo{filteredVehicles.length !== 1 ? 's' : ''} encontrado
            {filteredVehicles.length !== 1 ? 's' : ''}
          </p>

          {/* Vehicle Grid */}
          {isLoading ? (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="animate-pulse rounded-xl border bg-white">
                  <div className="aspect-[4/3] rounded-t-xl bg-gray-200" />
                  <div className="space-y-2 p-4">
                    <div className="h-4 w-3/4 rounded bg-gray-200" />
                    <div className="h-5 w-1/2 rounded bg-gray-200" />
                    <div className="h-3 w-full rounded bg-gray-200" />
                  </div>
                </div>
              ))}
            </div>
          ) : filteredVehicles.length > 0 ? (
            <div
              className={cn(
                viewMode === 'grid'
                  ? 'grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4'
                  : 'space-y-4'
              )}
            >
              {filteredVehicles.map(vehicle => (
                <PortalVehicleCard key={vehicle.id} vehicle={vehicle} />
              ))}
            </div>
          ) : (
            <div className="rounded-2xl border-2 border-dashed py-20 text-center">
              <Car className="mx-auto h-12 w-12 text-gray-300" />
              <h3 className="mt-4 text-lg font-semibold text-gray-600">
                No se encontraron vehículos
              </h3>
              <p className="mt-1 text-sm text-gray-400">
                {searchQuery
                  ? 'Intenta con otros términos de búsqueda.'
                  : 'Este dealer no tiene vehículos publicados aún.'}
              </p>
            </div>
          )}
        </div>
      </section>

      {/* ===== WHY TRUST SECTION ===== */}
      <section className="border-t bg-white py-12">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <h2 className="mb-8 text-center text-2xl font-bold text-gray-900">
            ¿Por qué comprar en {dealer.businessName}?
          </h2>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            <div className="text-center">
              <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-emerald-100">
                <Shield className="h-6 w-6 text-emerald-600" />
              </div>
              <h3 className="font-semibold text-gray-900">Dealer Verificado</h3>
              <p className="mt-1 text-sm text-gray-500">
                Identidad y documentos verificados por OKLA
              </p>
            </div>
            <div className="text-center">
              <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-blue-100">
                <Award className="h-6 w-6 text-blue-600" />
              </div>
              <h3 className="font-semibold text-gray-900">Calidad Garantizada</h3>
              <p className="mt-1 text-sm text-gray-500">Vehículos inspeccionados y con historial</p>
            </div>
            <div className="text-center">
              <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-purple-100">
                <TrendingUp className="h-6 w-6 text-purple-600" />
              </div>
              <h3 className="font-semibold text-gray-900">Precios Competitivos</h3>
              <p className="mt-1 text-sm text-gray-500">
                Precios justos verificados por inteligencia de mercado
              </p>
            </div>
            <div className="text-center">
              <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-amber-100">
                <MessageCircle className="h-6 w-6 text-amber-600" />
              </div>
              <h3 className="font-semibold text-gray-900">Atención Rápida</h3>
              <p className="mt-1 text-sm text-gray-500">
                Respuesta garantizada vía chat, WhatsApp o llamada
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* ===== POWERED BY OKLA ===== */}
      <section className="border-t bg-gradient-to-br from-gray-900 to-emerald-900 py-8">
        <div className="mx-auto max-w-7xl px-4 text-center sm:px-6 lg:px-8">
          <p className="text-sm text-gray-300">
            Portal potenciado por{' '}
            <Link href="/" className="font-semibold text-emerald-400 hover:text-emerald-300">
              OKLA
            </Link>{' '}
            — El marketplace de vehículos #1 en República Dominicana
          </p>
          <div className="mt-3 flex justify-center gap-4">
            <Link href="/dealers" className="text-sm text-gray-400 hover:text-white">
              ¿Eres dealer? Únete gratis
            </Link>
            <span className="text-gray-600">•</span>
            <Link href="/vehiculos" className="text-sm text-gray-400 hover:text-white">
              Buscar vehículos
            </Link>
          </div>
        </div>
      </section>

      {/* Chatbot FAB */}
      <DealerChatbot
        dealerName={dealer.businessName}
        dealerId={dealer.id}
        dealerEmail={dealer.email}
      />
    </div>
  );
}
