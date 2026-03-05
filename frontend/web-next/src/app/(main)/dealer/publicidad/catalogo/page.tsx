/**
 * Advertising Product Catalog — /dealer/publicidad/catalogo
 *
 * Shows all 7 advertising products from the OKLA catalog.
 * Only accessible to authenticated dealers.
 * Prices displayed in USD and OKLA Coins.
 */

'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth } from '@/hooks/use-auth';
import { apiClient } from '@/lib/api-client';
import {
  ArrowLeft,
  Star,
  Search,
  Megaphone,
  BarChart3,
  Eye,
  Zap,
  Crown,
  Coins,
  Loader2,
  ShieldAlert,
  Gift,
} from 'lucide-react';

// =============================================================================
// TYPES
// =============================================================================

interface AdProduct {
  id: string;
  slug: string;
  name: string;
  description: string;
  category: string;
  pricePerDay: number | null;
  pricePerWeek: number | null;
  pricePerMonth: number | null;
  coinsPricePerDay: number | null;
  coinsPricePerWeek: number | null;
  coinsPricePerMonth: number | null;
  maxSimultaneous: number | null;
  scope: string;
  isBundle: boolean;
  bundleSavings: number | null;
  displayOrder: number;
}

interface CatalogResponse {
  products: AdProduct[];
  currency: string;
  coinConversion: string;
  totalProducts: number;
}

// =============================================================================
// CONSTANTS
// =============================================================================

const CATEGORY_ICONS: Record<string, React.ElementType> = {
  Destacados: Star,
  Busquedas: Search,
  Homepage: Megaphone,
  Bundles: Gift,
};

const CATEGORY_COLORS: Record<string, string> = {
  Destacados: 'text-amber-600 bg-amber-50',
  Busquedas: 'text-blue-600 bg-blue-50',
  Homepage: 'text-purple-600 bg-purple-50',
  Bundles: 'text-emerald-600 bg-emerald-50',
};

// =============================================================================
// HELPERS
// =============================================================================

function formatUSD(amount: number | null | undefined) {
  if (amount == null) return '—';
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(amount);
}

function formatCoins(amount: number | null | undefined) {
  if (amount == null) return '—';
  return `${amount.toLocaleString()} 🪙`;
}

// =============================================================================
// CATALOG PAGE
// =============================================================================

export default function AdCatalogPage() {
  const { user, isAuthenticated } = useAuth();
  const [products, setProducts] = useState<AdProduct[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeCategory, setActiveCategory] = useState('all');

  useEffect(() => {
    async function fetchCatalog() {
      try {
        setIsLoading(true);
        const res = await apiClient.get<{ success: boolean; data: CatalogResponse }>(
          '/api/advertising/catalog'
        );
        setProducts(res.data.data?.products ?? res.data.data ?? []);
      } catch (err: unknown) {
        setError('No se pudo cargar el catálogo de publicidad.');
      } finally {
        setIsLoading(false);
      }
    }
    fetchCatalog();
  }, []);

  // --- Auth gate ---
  if (!isAuthenticated) {
    return (
      <div className="container mx-auto max-w-3xl py-16 text-center">
        <ShieldAlert className="mx-auto mb-4 h-16 w-16 text-amber-500" />
        <h1 className="mb-2 text-2xl font-bold">Acceso Restringido</h1>
        <p className="text-muted-foreground mb-6">
          El catálogo de publicidad está disponible solo para dealers registrados.
        </p>
        <Button asChild>
          <Link href="/dealers/registro">Registrarme como Dealer</Link>
        </Button>
      </div>
    );
  }

  // --- Categories derived from products ---
  const categories = [...new Set(products.map(p => p.category))];

  const filteredProducts =
    activeCategory === 'all'
      ? products
      : products.filter(p => p.category === activeCategory);

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8">
      {/* Header */}
      <div className="mb-8 flex items-center justify-between">
        <div>
          <Link
            href="/dealer/publicidad"
            className="text-muted-foreground hover:text-foreground mb-2 inline-flex items-center gap-1 text-sm"
          >
            <ArrowLeft className="h-4 w-4" /> Volver a Publicidad
          </Link>
          <h1 className="text-3xl font-bold tracking-tight">Catálogo de Publicidad</h1>
          <p className="text-muted-foreground mt-1">
            7 productos diseñados para maximizar la visibilidad de tus vehículos.
            Paga con USD o OKLA Coins.
          </p>
        </div>
        <Badge variant="secondary" className="hidden text-sm sm:inline-flex">
          {products.length} productos disponibles
        </Badge>
      </div>

      {/* Category Tabs */}
      <Tabs
        value={activeCategory}
        onValueChange={setActiveCategory}
        className="mb-8"
      >
        <TabsList>
          <TabsTrigger value="all">Todos</TabsTrigger>
          {categories.map(cat => (
            <TabsTrigger key={cat} value={cat}>
              {cat}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 className="mr-2 h-6 w-6 animate-spin" />
          <span className="text-muted-foreground">Cargando catálogo...</span>
        </div>
      )}

      {/* Error */}
      {error && !isLoading && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-center">
          <p className="text-red-700">{error}</p>
          <Button variant="outline" className="mt-3" onClick={() => window.location.reload()}>
            Reintentar
          </Button>
        </div>
      )}

      {/* Products Grid */}
      {!isLoading && !error && (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {filteredProducts
            .sort((a, b) => a.displayOrder - b.displayOrder)
            .map(product => {
              const Icon = CATEGORY_ICONS[product.category] ?? Zap;
              const colorClass = CATEGORY_COLORS[product.category] ?? 'text-gray-600 bg-gray-50';

              return (
                <Card
                  key={product.id}
                  className={`relative flex flex-col transition-shadow hover:shadow-lg ${
                    product.isBundle ? 'border-2 border-emerald-400' : ''
                  }`}
                >
                  {product.isBundle && (
                    <div className="absolute -top-3 right-4">
                      <Badge className="bg-emerald-500 text-white">
                        Ahorra {product.bundleSavings ? `$${product.bundleSavings}` : ''}
                      </Badge>
                    </div>
                  )}
                  <CardHeader className="pb-3">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <div className={`rounded-lg p-2 ${colorClass}`}>
                          <Icon className="h-5 w-5" />
                        </div>
                        <div>
                          <CardTitle className="text-lg">{product.name}</CardTitle>
                          <Badge variant="outline" className="mt-1 text-xs">
                            {product.category}
                          </Badge>
                        </div>
                      </div>
                      {product.scope === 'PerVehicle' && (
                        <Badge variant="secondary" className="text-xs">
                          Por vehículo
                        </Badge>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent className="flex flex-1 flex-col">
                    <p className="text-muted-foreground mb-4 text-sm">{product.description}</p>

                    {/* Pricing Table */}
                    <div className="mb-4 rounded-lg border bg-muted/30 p-4">
                      <h4 className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                        Precios
                      </h4>
                      <div className="space-y-2">
                        {product.pricePerDay != null && (
                          <div className="flex items-center justify-between text-sm">
                            <span>Por día</span>
                            <div className="flex items-center gap-3">
                              <span className="font-semibold">
                                {formatUSD(product.pricePerDay)}
                              </span>
                              {product.coinsPricePerDay != null && (
                                <span className="text-muted-foreground text-xs">
                                  ó {formatCoins(product.coinsPricePerDay)}
                                </span>
                              )}
                            </div>
                          </div>
                        )}
                        {product.pricePerWeek != null && (
                          <div className="flex items-center justify-between text-sm">
                            <span>Por semana</span>
                            <div className="flex items-center gap-3">
                              <span className="font-semibold">
                                {formatUSD(product.pricePerWeek)}
                              </span>
                              {product.coinsPricePerWeek != null && (
                                <span className="text-muted-foreground text-xs">
                                  ó {formatCoins(product.coinsPricePerWeek)}
                                </span>
                              )}
                            </div>
                          </div>
                        )}
                        {product.pricePerMonth != null && (
                          <div className="flex items-center justify-between text-sm">
                            <span>Por mes</span>
                            <div className="flex items-center gap-3">
                              <span className="font-semibold">
                                {formatUSD(product.pricePerMonth)}
                              </span>
                              {product.coinsPricePerMonth != null && (
                                <span className="text-muted-foreground text-xs">
                                  ó {formatCoins(product.coinsPricePerMonth)}
                                </span>
                              )}
                            </div>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Max simultaneous */}
                    {product.maxSimultaneous != null && (
                      <p className="text-muted-foreground mb-4 text-xs">
                        Máximo simultáneo: {product.maxSimultaneous} vehículo(s)
                      </p>
                    )}

                    {/* CTA */}
                    <div className="mt-auto">
                      <Button className="w-full bg-[#00A870] hover:bg-[#009663] text-white" asChild>
                        <Link href={`/dealer/publicidad/nueva?product=${product.slug}`}>
                          Contratar
                        </Link>
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
        </div>
      )}

      {/* OKLA Coins CTA */}
      {!isLoading && !error && (
        <div className="mt-12 rounded-xl border bg-gradient-to-r from-amber-50 to-orange-50 p-8 text-center">
          <Coins className="mx-auto mb-3 h-10 w-10 text-amber-600" />
          <h2 className="mb-2 text-xl font-bold">¿No tienes OKLA Coins?</h2>
          <p className="text-muted-foreground mb-4 max-w-lg mx-auto">
            Compra paquetes de OKLA Coins con hasta 67% de bonificación y paga tus anuncios sin
            tarjeta de crédito.
          </p>
          <Button variant="outline" asChild>
            <Link href="/dealer/coins">Ver Paquetes de Coins</Link>
          </Button>
        </div>
      )}
    </div>
  );
}
