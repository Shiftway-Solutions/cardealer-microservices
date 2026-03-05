'use client';

/**
 * Marketplace Import Page
 *
 * Allows dealers/sellers to import vehicle listings from Facebook Marketplace
 * or other platforms by pasting the listing text. Uses AI (Claude) to extract
 * structured vehicle data and pre-fill the creation form.
 */

import { useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import {
  Upload,
  Sparkles,
  AlertCircle,
  CheckCircle2,
  Loader2,
  Facebook,
  Globe,
  ClipboardPaste,
  ArrowRight,
  Car,
  Edit3,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';

interface ExtractedVehicle {
  make: string;
  model: string;
  year: number;
  price: number;
  currency: string;
  mileage: number | null;
  transmission: string | null;
  fuelType: string | null;
  bodyType: string | null;
  condition: string;
  color: string | null;
  description: string;
  location: string | null;
  province: string | null;
  features: string[];
  engineSize: string | null;
  doors: number | null;
  driveType: string | null;
  confidence: number;
}

export default function ImportarPage() {
  const router = useRouter();
  const [listingText, setListingText] = useState('');
  const [listingUrl, setListingUrl] = useState('');
  const [isExtracting, setIsExtracting] = useState(false);
  const [extractedData, setExtractedData] = useState<ExtractedVehicle | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hint, setHint] = useState<string | null>(null);

  const handleExtract = useCallback(
    async (mode: 'text' | 'url') => {
      setIsExtracting(true);
      setError(null);
      setHint(null);
      setExtractedData(null);

      try {
        const token = document.cookie
          .split(';')
          .find(c => c.trim().startsWith('token='))
          ?.split('=')[1];

        const response = await fetch('/api/import/marketplace', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify(
            mode === 'text' ? { text: listingText } : { url: listingUrl }
          ),
        });

        const result = await response.json();

        if (result.success) {
          setExtractedData(result.data);
        } else {
          setError(result.error || 'Error al procesar el anuncio');
          if (result.hint) setHint(result.hint);
        }
      } catch {
        setError('Error de conexión. Intenta de nuevo.');
      } finally {
        setIsExtracting(false);
      }
    },
    [listingText, listingUrl]
  );

  const handlePublish = useCallback(() => {
    if (!extractedData) return;
    // Store extracted data in sessionStorage and navigate to publish page
    sessionStorage.setItem('importedVehicle', JSON.stringify(extractedData));
    router.push('/vender/publicar?from=import');
  }, [extractedData, router]);

  const formatPrice = (price: number, currency: string) => {
    if (currency === 'USD') return `US$${price.toLocaleString('en-US')}`;
    return `RD$${price.toLocaleString('es-DO')}`;
  };

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      {/* Header */}
      <div className="mb-8 text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-blue-500 to-purple-600">
          <Sparkles className="h-8 w-8 text-white" />
        </div>
        <h1 className="text-3xl font-bold tracking-tight">Importar desde Marketplace</h1>
        <p className="text-muted-foreground mt-2 text-lg">
          Usa inteligencia artificial para importar tu inventario de Facebook Marketplace u otra
          plataforma
        </p>
      </div>

      {/* How it works */}
      <div className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-900 dark:bg-blue-950/30">
          <CardContent className="pt-6 text-center">
            <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-blue-100 dark:bg-blue-900">
              <ClipboardPaste className="h-5 w-5 text-blue-600" />
            </div>
            <h3 className="font-semibold">1. Copia el anuncio</h3>
            <p className="text-muted-foreground mt-1 text-sm">
              Copia el texto de tu anuncio en Facebook Marketplace
            </p>
          </CardContent>
        </Card>
        <Card className="border-purple-200 bg-purple-50/50 dark:border-purple-900 dark:bg-purple-950/30">
          <CardContent className="pt-6 text-center">
            <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-purple-100 dark:bg-purple-900">
              <Sparkles className="h-5 w-5 text-purple-600" />
            </div>
            <h3 className="font-semibold">2. IA extrae los datos</h3>
            <p className="text-muted-foreground mt-1 text-sm">
              Claude analiza el texto y extrae marca, modelo, precio y más
            </p>
          </CardContent>
        </Card>
        <Card className="border-green-200 bg-green-50/50 dark:border-green-900 dark:bg-green-950/30">
          <CardContent className="pt-6 text-center">
            <div className="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-green-100 dark:bg-green-900">
              <Car className="h-5 w-5 text-green-600" />
            </div>
            <h3 className="font-semibold">3. Publica en OKLA</h3>
            <p className="text-muted-foreground mt-1 text-sm">
              Revisa los datos, ajusta si es necesario y publica
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Input area */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Upload className="h-5 w-5" />
            Pega tu anuncio
          </CardTitle>
          <CardDescription>
            Copia el texto completo de tu anuncio de Facebook Marketplace y pégalo aquí
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Tabs defaultValue="text">
            <TabsList className="mb-4">
              <TabsTrigger value="text" className="gap-2">
                <ClipboardPaste className="h-4 w-4" />
                Pegar texto
              </TabsTrigger>
              <TabsTrigger value="url" className="gap-2">
                <Globe className="h-4 w-4" />
                URL del anuncio
              </TabsTrigger>
            </TabsList>

            <TabsContent value="text">
              <div className="space-y-4">
                <Textarea
                  placeholder={`Ejemplo:\n\nToyota Corolla 2020\nRD$ 850,000\n45,000 km\nAutomático, gasolina\nColor blanco, interior negro\nCámara de reversa, GPS, aros deportivos\nSanto Domingo Este\n\nExcelente condición, único dueño...`}
                  value={listingText}
                  onChange={e => setListingText(e.target.value)}
                  className="min-h-[200px] resize-y"
                />
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground text-sm">
                    {listingText.length > 0
                      ? `${listingText.length} caracteres`
                      : 'Pega el texto del anuncio'}
                  </span>
                  <Button
                    onClick={() => handleExtract('text')}
                    disabled={isExtracting || listingText.length < 10}
                    className="gap-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white hover:from-blue-700 hover:to-purple-700"
                  >
                    {isExtracting ? (
                      <>
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Analizando con IA...
                      </>
                    ) : (
                      <>
                        <Sparkles className="h-4 w-4" />
                        Extraer datos con IA
                      </>
                    )}
                  </Button>
                </div>
              </div>
            </TabsContent>

            <TabsContent value="url">
              <div className="space-y-4">
                <div>
                  <Label htmlFor="marketplace-url">URL del anuncio</Label>
                  <div className="mt-1.5 flex gap-2">
                    <div className="relative flex-1">
                      <Facebook className="text-muted-foreground absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2" />
                      <Input
                        id="marketplace-url"
                        placeholder="https://www.facebook.com/marketplace/item/..."
                        value={listingUrl}
                        onChange={e => setListingUrl(e.target.value)}
                        className="pl-10"
                      />
                    </div>
                    <Button
                      onClick={() => handleExtract('url')}
                      disabled={isExtracting || !listingUrl.startsWith('http')}
                      className="gap-2"
                      variant="outline"
                    >
                      {isExtracting ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Sparkles className="h-4 w-4" />
                      )}
                      Extraer
                    </Button>
                  </div>
                </div>
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertTitle>Nota sobre URLs</AlertTitle>
                  <AlertDescription>
                    Facebook Marketplace puede bloquear el acceso externo a los anuncios. Si la URL
                    no funciona, copia y pega el texto del anuncio directamente en la pestaña
                    &quot;Pegar texto&quot;.
                  </AlertDescription>
                </Alert>
              </div>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      {/* Error state */}
      {error && (
        <Alert variant="destructive" className="mb-8">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>
            {error}
            {hint && <p className="mt-1 font-medium">{hint}</p>}
          </AlertDescription>
        </Alert>
      )}

      {/* Extracted data preview */}
      {extractedData && (
        <Card className="mb-8 border-green-200 dark:border-green-900">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-5 w-5 text-green-600" />
                <CardTitle>Datos extraídos</CardTitle>
              </div>
              <Badge
                variant={extractedData.confidence >= 70 ? 'default' : 'secondary'}
                className={
                  extractedData.confidence >= 70
                    ? 'bg-green-100 text-green-700'
                    : 'bg-yellow-100 text-yellow-700'
                }
              >
                {extractedData.confidence}% confianza
              </Badge>
            </div>
            <CardDescription>
              Revisa los datos extraídos y ajústalos antes de publicar
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              {/* Left column: Vehicle details */}
              <div className="space-y-4">
                <h3 className="flex items-center gap-2 font-semibold">
                  <Car className="h-4 w-4" />
                  Detalles del Vehículo
                </h3>

                <div className="grid grid-cols-2 gap-3">
                  <DataField label="Marca" value={extractedData.make} />
                  <DataField label="Modelo" value={extractedData.model} />
                  <DataField label="Año" value={extractedData.year?.toString()} />
                  <DataField
                    label="Precio"
                    value={
                      extractedData.price
                        ? formatPrice(extractedData.price, extractedData.currency)
                        : null
                    }
                    highlight
                  />
                  <DataField
                    label="Kilometraje"
                    value={extractedData.mileage ? `${extractedData.mileage.toLocaleString()} km` : null}
                  />
                  <DataField label="Transmisión" value={extractedData.transmission} />
                  <DataField label="Combustible" value={extractedData.fuelType} />
                  <DataField label="Carrocería" value={extractedData.bodyType} />
                  <DataField label="Color" value={extractedData.color} />
                  <DataField label="Condición" value={extractedData.condition} />
                  <DataField label="Motor" value={extractedData.engineSize} />
                  <DataField label="Tracción" value={extractedData.driveType} />
                </div>
              </div>

              {/* Right column: Location, description, features */}
              <div className="space-y-4">
                <div>
                  <h3 className="mb-2 font-semibold">📍 Ubicación</h3>
                  <p className="text-sm">
                    {extractedData.location || extractedData.province || 'No especificada'}
                  </p>
                </div>

                {extractedData.features.length > 0 && (
                  <div>
                    <h3 className="mb-2 font-semibold">✨ Características</h3>
                    <div className="flex flex-wrap gap-1.5">
                      {extractedData.features.map((feature, i) => (
                        <Badge key={i} variant="secondary" className="text-xs">
                          {feature}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}

                {extractedData.description && (
                  <div>
                    <h3 className="mb-2 font-semibold">📝 Descripción</h3>
                    <p className="text-muted-foreground line-clamp-4 text-sm">
                      {extractedData.description}
                    </p>
                  </div>
                )}
              </div>
            </div>

            {/* Actions */}
            <div className="mt-6 flex flex-col gap-3 border-t pt-6 sm:flex-row sm:justify-end">
              <Button
                variant="outline"
                onClick={() => setExtractedData(null)}
                className="gap-2"
              >
                <Edit3 className="h-4 w-4" />
                Modificar texto y re-extraer
              </Button>
              <Button
                onClick={handlePublish}
                className="gap-2 bg-gradient-to-r from-emerald-600 to-teal-600 text-white hover:from-emerald-700 hover:to-teal-700"
              >
                Continuar a publicar
                <ArrowRight className="h-4 w-4" />
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Supported platforms info */}
      <Card className="border-dashed">
        <CardContent className="pt-6">
          <h3 className="mb-3 text-center font-semibold">Plataformas soportadas</h3>
          <div className="flex flex-wrap justify-center gap-4">
            {[
              { name: 'Facebook Marketplace', icon: Facebook, color: 'text-blue-600' },
              { name: 'Corotos', icon: Globe, color: 'text-orange-600' },
              { name: 'SuperCarros', icon: Car, color: 'text-red-600' },
              { name: 'Otras plataformas', icon: Globe, color: 'text-gray-600' },
            ].map(platform => (
              <div
                key={platform.name}
                className="flex items-center gap-2 rounded-full border px-4 py-2 text-sm"
              >
                <platform.icon className={`h-4 w-4 ${platform.color}`} />
                {platform.name}
              </div>
            ))}
          </div>
          <p className="text-muted-foreground mt-3 text-center text-xs">
            Funciona con cualquier texto de anuncio de vehículos en español
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

function DataField({
  label,
  value,
  highlight,
}: {
  label: string;
  value: string | null | undefined;
  highlight?: boolean;
}) {
  return (
    <div>
      <span className="text-muted-foreground text-xs">{label}</span>
      <p className={`text-sm font-medium ${highlight ? 'text-emerald-600' : ''} ${!value ? 'text-muted-foreground italic' : ''}`}>
        {value || 'No detectado'}
      </p>
    </div>
  );
}
