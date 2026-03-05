# 🏢 Editor de Perfil de Dealer

> **Tiempo estimado:** 60 minutos  
> **Prerrequisitos:** Dashboard de dealer, componentes de formulario  
> **Páginas cubiertas:** DealerProfileEditorPage, PublicDealerProfilePage

---

## 📋 OBJETIVO

Implementar las páginas de perfil del dealer:

- Editor de perfil con todos los campos
- Vista pública del perfil del dealer
- Gestión de logo y galería
- Horarios de atención
- Redes sociales y contacto

---

## 🎨 ESTRUCTURA VISUAL

### Editor de Perfil

```
┌─────────────────────────────────────────────────────────────────┐
│ HEADER: "Configurar Perfil del Dealer"                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ TABS: [Información] [Ubicaciones] [Horarios] [Redes] [Galería]  │
│                                                                 │
│ TAB: INFORMACIÓN BÁSICA                                         │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Logo                    Nombre del Dealer                   │ │
│ │ ┌────────────┐         ┌─────────────────────────────────┐  │ │
│ │ │            │         │ AutoMax RD                      │  │ │
│ │ │   [Logo]   │         └─────────────────────────────────┘  │ │
│ │ │            │                                              │ │
│ │ └────────────┘         Slug URL                             │ │
│ │  [Cambiar]             ┌─────────────────────────────────┐  │ │
│ │                        │ okla.com.do/dealers/automax-rd  │  │ │
│ │                        └─────────────────────────────────┘  │ │
│ │                                                              │ │
│ │ Descripción                                                  │ │
│ │ ┌─────────────────────────────────────────────────────────┐ │ │
│ │ │ Somos un concesionario con más de 20 años de           │ │ │
│ │ │ experiencia en el mercado dominicano...                 │ │ │
│ │ └─────────────────────────────────────────────────────────┘ │ │
│ │                                                              │ │
│ │ Teléfono              WhatsApp              Email           │ │
│ │ ┌────────────┐        ┌────────────┐        ┌────────────┐  │ │
│ │ │809-555-1234│        │809-555-5678│        │info@automax│  │ │
│ │ └────────────┘        └────────────┘        └────────────┘  │ │
│ │                                                              │ │
│ │ Año de fundación      RNC                                   │ │
│ │ ┌────────────┐        ┌────────────────────┐                │ │
│ │ │ 2004       │        │ 1-01-12345-6       │                │ │
│ │ └────────────┘        └────────────────────┘                │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│                                    [Cancelar]  [Guardar Cambios]│
└─────────────────────────────────────────────────────────────────┘
```

### Perfil Público del Dealer

```
┌─────────────────────────────────────────────────────────────────┐
│ HEADER                                                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ COVER IMAGE                                                  │ │
│ │                                                              │ │
│ │         ┌─────────┐                                         │ │
│ │         │  LOGO   │  AutoMax RD                             │ │
│ │         └─────────┘  ⭐ 4.8 (120 reviews) | 📍 Santo Domingo│ │
│ │                      ✓ Verificado | Desde 2004              │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ [Inventario: 45]  [Reviews]  [Ubicaciones]  [Contacto]          │
│                                                                 │
│ INVENTARIO DESTACADO                                            │
│ ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐        │
│ │ Toyota    │ │ Honda     │ │ Hyundai   │ │ BMW       │        │
│ │ Camry '24 │ │ CR-V '23  │ │ Tucson'23 │ │ X3 '22    │        │
│ │ $1.8M     │ │ $2.1M     │ │ $1.6M     │ │ $2.8M     │        │
│ └───────────┘ └───────────┘ └───────────┘ └───────────┘        │
│                                                                 │
│                    [Ver todo el inventario →]                   │
│                                                                 │
│ SOBRE NOSOTROS                                                  │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Con más de 20 años de experiencia, AutoMax RD se ha        │ │
│ │ convertido en uno de los concesionarios más confiables...  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ HORARIOS                          CONTACTO                      │
│ ┌──────────────────────┐          ┌──────────────────────────┐  │
│ │ Lun-Vie: 8am - 6pm   │          │ 📞 809-555-1234          │  │
│ │ Sábado:  9am - 3pm   │          │ 💬 WhatsApp              │  │
│ │ Domingo: Cerrado     │          │ 📧 info@automax.com.do   │  │
│ └──────────────────────┘          └──────────────────────────┘  │
│                                                                 │
│ UBICACIÓN                                                       │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │                    [MAPA DE GOOGLE]                         │ │
│ │                                                              │ │
│ │  Av. Winston Churchill #45, Santo Domingo                   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

### 1. Editor de Perfil de Dealer

```typescript
// filepath: src/app/(dealer)/dealer/profile/edit/page.tsx
'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Label } from '@/components/ui/label';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { useToast } from '@/hooks/use-toast';
import { dealerService, type DealerProfile } from '@/services/api/dealerService';
import { mediaService } from '@/services/api/mediaService';
import {
  Building2, MapPin, Clock, Share2, Image as ImageIcon,
  Upload, Save, Eye, Loader2, Plus, Trash2
} from 'lucide-react';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';

const profileSchema = z.object({
  name: z.string().min(2, 'El nombre debe tener al menos 2 caracteres'),
  slug: z.string().min(2).regex(/^[a-z0-9-]+$/, 'Solo letras minúsculas, números y guiones'),
  description: z.string().max(1000, 'Máximo 1000 caracteres').optional(),
  phone: z.string().min(10, 'Teléfono inválido'),
  whatsapp: z.string().optional(),
  email: z.string().email('Email inválido'),
  website: z.string().url('URL inválida').optional().or(z.literal('')),
  foundedYear: z.number().min(1900).max(new Date().getFullYear()).optional(),
  rnc: z.string().optional(),
  // Social
  facebook: z.string().optional(),
  instagram: z.string().optional(),
  twitter: z.string().optional(),
  youtube: z.string().optional(),
});

type ProfileFormData = z.infer<typeof profileSchema>;

interface BusinessHours {
  day: string;
  open: string;
  close: string;
  closed: boolean;
}

export default function DealerProfileEditorPage() {
  const router = useRouter();
  const { toast } = useToast();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [profile, setProfile] = useState<DealerProfile | null>(null);
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<string | null>(null);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [coverPreview, setCoverPreview] = useState<string | null>(null);
  const [hours, setHours] = useState<BusinessHours[]>([
    { day: 'Lunes', open: '08:00', close: '18:00', closed: false },
    { day: 'Martes', open: '08:00', close: '18:00', closed: false },
    { day: 'Miércoles', open: '08:00', close: '18:00', closed: false },
    { day: 'Jueves', open: '08:00', close: '18:00', closed: false },
    { day: 'Viernes', open: '08:00', close: '18:00', closed: false },
    { day: 'Sábado', open: '09:00', close: '15:00', closed: false },
    { day: 'Domingo', open: '', close: '', closed: true },
  ]);

  const form = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      name: '',
      slug: '',
      description: '',
      phone: '',
      whatsapp: '',
      email: '',
      website: '',
      facebook: '',
      instagram: '',
      twitter: '',
      youtube: '',
    },
  });

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    try {
      const data = await dealerService.getMyProfile();
      setProfile(data);

      form.reset({
        name: data.name,
        slug: data.slug,
        description: data.description || '',
        phone: data.phone,
        whatsapp: data.whatsapp || '',
        email: data.email,
        website: data.website || '',
        foundedYear: data.foundedYear,
        rnc: data.rnc || '',
        facebook: data.socialLinks?.facebook || '',
        instagram: data.socialLinks?.instagram || '',
        twitter: data.socialLinks?.twitter || '',
        youtube: data.socialLinks?.youtube || '',
      });

      if (data.logo) {
        setLogoPreview(data.logo);
      }
      if (data.coverImage) {
        setCoverPreview(data.coverImage);
      }
      if (data.businessHours) {
        setHours(data.businessHours);
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'No pudimos cargar tu perfil',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setLogoFile(file);
      setLogoPreview(URL.createObjectURL(file));
    }
  };

  const handleCoverChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setCoverFile(file);
      setCoverPreview(URL.createObjectURL(file));
    }
  };

  const onSubmit = async (data: ProfileFormData) => {
    setIsSaving(true);
    try {
      // Upload images if changed
      let logoUrl = profile?.logo;
      let coverUrl = profile?.coverImage;

      if (logoFile) {
        const uploadResult = await mediaService.uploadImage(logoFile, 'dealer-logos');
        logoUrl = uploadResult.url;
      }

      if (coverFile) {
        const uploadResult = await mediaService.uploadImage(coverFile, 'dealer-covers');
        coverUrl = uploadResult.url;
      }

      // Update profile
      await dealerService.updateProfile({
        ...data,
        logo: logoUrl,
        coverImage: coverUrl,
        businessHours: hours,
        socialLinks: {
          facebook: data.facebook,
          instagram: data.instagram,
          twitter: data.twitter,
          youtube: data.youtube,
        },
      });

      toast({
        title: 'Perfil actualizado',
        description: 'Los cambios se han guardado correctamente',
      });
    } catch (error) {
      toast({
        title: 'Error',
        description: 'No pudimos guardar los cambios',
        variant: 'destructive',
      });
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">Configurar Perfil</h1>
          <p className="text-gray-600">Personaliza cómo se ve tu dealer en OKLA</p>
        </div>
        <Button variant="outline" asChild>
          <a href={`/dealers/${profile?.slug}`} target="_blank" rel="noopener noreferrer">
            <Eye className="w-4 h-4 mr-2" />
            Ver perfil público
          </a>
        </Button>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)}>
          <Tabs defaultValue="info" className="space-y-6">
            <TabsList className="grid w-full grid-cols-5">
              <TabsTrigger value="info">
                <Building2 className="w-4 h-4 mr-2" />
                Información
              </TabsTrigger>
              <TabsTrigger value="hours">
                <Clock className="w-4 h-4 mr-2" />
                Horarios
              </TabsTrigger>
              <TabsTrigger value="social">
                <Share2 className="w-4 h-4 mr-2" />
                Redes
              </TabsTrigger>
              <TabsTrigger value="gallery">
                <ImageIcon className="w-4 h-4 mr-2" />
                Galería
              </TabsTrigger>
              <TabsTrigger value="locations">
                <MapPin className="w-4 h-4 mr-2" />
                Ubicaciones
              </TabsTrigger>
            </TabsList>

            {/* Tab: Información Básica */}
            <TabsContent value="info">
              <Card>
                <CardHeader>
                  <CardTitle>Información del Dealer</CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Logo y Cover */}
                  <div className="grid md:grid-cols-2 gap-6">
                    <div>
                      <Label>Logo del Dealer</Label>
                      <div className="mt-2 flex items-center gap-4">
                        <Avatar className="w-24 h-24">
                          <AvatarImage src={logoPreview || ''} />
                          <AvatarFallback>{profile?.name?.charAt(0) || 'D'}</AvatarFallback>
                        </Avatar>
                        <div>
                          <input
                            type="file"
                            accept="image/*"
                            onChange={handleLogoChange}
                            className="hidden"
                            id="logo-upload"
                          />
                          <Button type="button" variant="outline" size="sm" asChild>
                            <label htmlFor="logo-upload" className="cursor-pointer">
                              <Upload className="w-4 h-4 mr-2" />
                              Cambiar logo
                            </label>
                          </Button>
                          <p className="text-xs text-gray-500 mt-1">
                            JPG, PNG. Máximo 2MB
                          </p>
                        </div>
                      </div>
                    </div>

                    <div>
                      <Label>Imagen de portada</Label>
                      <div className="mt-2">
                        <div
                          className="h-24 bg-gray-100 rounded-lg bg-cover bg-center"
                          style={{ backgroundImage: coverPreview ? `url(${coverPreview})` : undefined }}
                        />
                        <input
                          type="file"
                          accept="image/*"
                          onChange={handleCoverChange}
                          className="hidden"
                          id="cover-upload"
                        />
                        <Button type="button" variant="outline" size="sm" className="mt-2" asChild>
                          <label htmlFor="cover-upload" className="cursor-pointer">
                            <Upload className="w-4 h-4 mr-2" />
                            Cambiar portada
                          </label>
                        </Button>
                      </div>
                    </div>
                  </div>

                  {/* Nombre y Slug */}
                  <div className="grid md:grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="name"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Nombre del Dealer</FormLabel>
                          <FormControl>
                            <Input placeholder="AutoMax RD" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="slug"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>URL del perfil</FormLabel>
                          <FormControl>
                            <div className="flex">
                              <span className="inline-flex items-center px-3 bg-gray-100 border border-r-0 rounded-l-md text-sm text-gray-500">
                                okla.com.do/dealers/
                              </span>
                              <Input
                                className="rounded-l-none"
                                placeholder="automax-rd"
                                {...field}
                              />
                            </div>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  {/* Descripción */}
                  <FormField
                    control={form.control}
                    name="description"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Descripción</FormLabel>
                        <FormControl>
                          <Textarea
                            placeholder="Cuéntale a los compradores sobre tu negocio..."
                            className="min-h-[100px]"
                            {...field}
                          />
                        </FormControl>
                        <FormDescription>
                          {field.value?.length || 0}/1000 caracteres
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  {/* Contacto */}
                  <div className="grid md:grid-cols-3 gap-4">
                    <FormField
                      control={form.control}
                      name="phone"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Teléfono</FormLabel>
                          <FormControl>
                            <Input placeholder="809-555-1234" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="whatsapp"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>WhatsApp</FormLabel>
                          <FormControl>
                            <Input placeholder="809-555-5678" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="email"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Email</FormLabel>
                          <FormControl>
                            <Input placeholder="info@tudealer.com" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  {/* Más info */}
                  <div className="grid md:grid-cols-3 gap-4">
                    <FormField
                      control={form.control}
                      name="website"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Sitio web</FormLabel>
                          <FormControl>
                            <Input placeholder="https://tudealer.com" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="foundedYear"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Año de fundación</FormLabel>
                          <FormControl>
                            <Input
                              type="number"
                              placeholder="2004"
                              {...field}
                              onChange={(e) => field.onChange(parseInt(e.target.value))}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="rnc"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>RNC</FormLabel>
                          <FormControl>
                            <Input placeholder="1-01-12345-6" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Tab: Horarios */}
            <TabsContent value="hours">
              <Card>
                <CardHeader>
                  <CardTitle>Horarios de Atención</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {hours.map((hour, index) => (
                      <div key={hour.day} className="flex items-center gap-4">
                        <div className="w-28 font-medium">{hour.day}</div>
                        <label className="flex items-center gap-2">
                          <input
                            type="checkbox"
                            checked={hour.closed}
                            onChange={(e) => {
                              const newHours = [...hours];
                              newHours[index].closed = e.target.checked;
                              setHours(newHours);
                            }}
                            className="rounded"
                          />
                          Cerrado
                        </label>
                        {!hour.closed && (
                          <>
                            <Input
                              type="time"
                              value={hour.open}
                              onChange={(e) => {
                                const newHours = [...hours];
                                newHours[index].open = e.target.value;
                                setHours(newHours);
                              }}
                              className="w-32"
                            />
                            <span>a</span>
                            <Input
                              type="time"
                              value={hour.close}
                              onChange={(e) => {
                                const newHours = [...hours];
                                newHours[index].close = e.target.value;
                                setHours(newHours);
                              }}
                              className="w-32"
                            />
                          </>
                        )}
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Tab: Redes Sociales */}
            <TabsContent value="social">
              <Card>
                <CardHeader>
                  <CardTitle>Redes Sociales</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="facebook"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Facebook</FormLabel>
                        <FormControl>
                          <Input placeholder="https://facebook.com/tudealer" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="instagram"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Instagram</FormLabel>
                        <FormControl>
                          <Input placeholder="https://instagram.com/tudealer" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="twitter"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Twitter/X</FormLabel>
                        <FormControl>
                          <Input placeholder="https://twitter.com/tudealer" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="youtube"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>YouTube</FormLabel>
                        <FormControl>
                          <Input placeholder="https://youtube.com/@tudealer" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>
            </TabsContent>

            {/* Tab: Galería */}
            <TabsContent value="gallery">
              <Card>
                <CardHeader>
                  <CardTitle>Galería de Fotos</CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-gray-600 mb-4">
                    Agrega fotos de tus instalaciones para generar más confianza
                  </p>
                  {/* Gallery component here */}
                  <div className="grid grid-cols-3 gap-4">
                    {profile?.galleryImages?.map((img, idx) => (
                      <div key={idx} className="relative aspect-video bg-gray-100 rounded-lg overflow-hidden">
                        <img src={img} alt="" className="w-full h-full object-cover" />
                        <Button
                          type="button"
                          variant="destructive"
                          size="icon"
                          className="absolute top-2 right-2"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    ))}
                    <div className="aspect-video border-2 border-dashed rounded-lg flex items-center justify-center cursor-pointer hover:bg-gray-50">
                      <Plus className="w-8 h-8 text-gray-400" />
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Tab: Ubicaciones */}
            <TabsContent value="locations">
              <Card>
                <CardHeader className="flex flex-row items-center justify-between">
                  <CardTitle>Ubicaciones / Sucursales</CardTitle>
                  <Button type="button" variant="outline" size="sm">
                    <Plus className="w-4 h-4 mr-2" />
                    Agregar ubicación
                  </Button>
                </CardHeader>
                <CardContent>
                  <p className="text-gray-600">
                    Gestiona tus ubicaciones en la sección de
                    <a href="/dealer/locations" className="text-primary ml-1">Ubicaciones</a>
                  </p>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>

          {/* Actions */}
          <div className="flex justify-end gap-4 mt-8">
            <Button type="button" variant="outline" onClick={() => router.back()}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSaving}>
              {isSaving ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Guardando...
                </>
              ) : (
                <>
                  <Save className="w-4 h-4 mr-2" />
                  Guardar cambios
                </>
              )}
            </Button>
          </div>
        </form>
      </Form>
    </div>
  );
}
```

### 2. Perfil Público del Dealer

```typescript
// filepath: src/app/(public)/dealers/[slug]/page.tsx
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Image from 'next/image';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { dealerService } from '@/services/api/dealerService';
import { VehicleCard } from '@/components/vehicles/VehicleCard';
import { ReviewsList } from '@/components/reviews/ReviewsList';
import { GoogleMap } from '@/components/maps/GoogleMap';
import {
  Star, MapPin, Phone, MessageSquare, Mail, Globe, Calendar,
  Clock, Shield, Facebook, Instagram, Twitter, Youtube
} from 'lucide-react';

interface Props {
  params: { slug: string };
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const dealer = await dealerService.getBySlug(params.slug);
  if (!dealer) return { title: 'Dealer no encontrado' };

  return {
    title: `${dealer.name} - Vehículos en OKLA`,
    description: dealer.description || `Explora el inventario de ${dealer.name}`,
    openGraph: {
      title: dealer.name,
      description: dealer.description,
      images: [dealer.logo || '/images/default-dealer.png'],
    },
  };
}

export default async function PublicDealerProfilePage({ params }: Props) {
  const dealer = await dealerService.getBySlug(params.slug);
  if (!dealer) notFound();

  const vehicles = await dealerService.getVehicles(dealer.id, { limit: 8 });
  const reviews = await dealerService.getReviews(dealer.id, { limit: 5 });

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Cover & Profile Header */}
      <div className="relative h-48 md:h-64 bg-gradient-to-r from-primary-600 to-primary-800">
        {dealer.coverImage && (
          <Image
            src={dealer.coverImage}
            alt=""
            fill
            className="object-cover opacity-50"
          />
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
      </div>

      <div className="container mx-auto px-4">
        <div className="relative -mt-20 mb-8">
          <div className="bg-white rounded-lg shadow-lg p-6">
            <div className="flex flex-col md:flex-row gap-6">
              {/* Logo */}
              <div className="flex-shrink-0">
                <div className="w-32 h-32 rounded-xl overflow-hidden border-4 border-white shadow-lg bg-white">
                  {dealer.logo ? (
                    <Image
                      src={dealer.logo}
                      alt={dealer.name}
                      width={128}
                      height={128}
                      className="object-contain"
                    />
                  ) : (
                    <div className="w-full h-full bg-gray-200 flex items-center justify-center text-4xl font-bold text-gray-400">
                      {dealer.name.charAt(0)}
                    </div>
                  )}
                </div>
              </div>

              {/* Info */}
              <div className="flex-grow">
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <h1 className="text-2xl md:text-3xl font-bold">{dealer.name}</h1>
                    <div className="flex flex-wrap items-center gap-3 mt-2 text-sm text-gray-600">
                      <div className="flex items-center gap-1">
                        <Star className="w-4 h-4 fill-yellow-400 text-yellow-400" />
                        <span className="font-medium">{dealer.rating.toFixed(1)}</span>
                        <span>({dealer.reviewCount} reviews)</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <MapPin className="w-4 h-4" />
                        <span>{dealer.city}</span>
                      </div>
                      {dealer.isVerified && (
                        <Badge variant="secondary" className="bg-green-100 text-green-700">
                          <Shield className="w-3 h-3 mr-1" />
                          Verificado
                        </Badge>
                      )}
                      {dealer.foundedYear && (
                        <div className="flex items-center gap-1">
                          <Calendar className="w-4 h-4" />
                          <span>Desde {dealer.foundedYear}</span>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Contact Buttons */}
                  <div className="flex gap-2">
                    <Button asChild>
                      <a href={`tel:${dealer.phone}`}>
                        <Phone className="w-4 h-4 mr-2" />
                        Llamar
                      </a>
                    </Button>
                    {dealer.whatsapp && (
                      <Button variant="outline" className="bg-green-50 border-green-200 text-green-700 hover:bg-green-100" asChild>
                        <a href={`https://wa.me/${dealer.whatsapp.replace(/\D/g, '')}`} target="_blank">
                          <MessageSquare className="w-4 h-4 mr-2" />
                          WhatsApp
                        </a>
                      </Button>
                    )}
                  </div>
                </div>

                {/* Stats */}
                <div className="grid grid-cols-3 gap-4 mt-6 pt-6 border-t">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-primary-600">{dealer.vehicleCount}</div>
                    <div className="text-sm text-gray-600">Vehículos</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-primary-600">{dealer.soldCount}</div>
                    <div className="text-sm text-gray-600">Vendidos</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-primary-600">{dealer.responseTime}</div>
                    <div className="text-sm text-gray-600">Respuesta</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Content Tabs */}
        <Tabs defaultValue="inventory" className="mb-12">
          <TabsList className="mb-6">
            <TabsTrigger value="inventory">
              Inventario ({dealer.vehicleCount})
            </TabsTrigger>
            <TabsTrigger value="about">Sobre Nosotros</TabsTrigger>
            <TabsTrigger value="reviews">
              Reviews ({dealer.reviewCount})
            </TabsTrigger>
            <TabsTrigger value="locations">Ubicaciones</TabsTrigger>
          </TabsList>

          {/* Inventory Tab */}
          <TabsContent value="inventory">
            <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
              {vehicles.data.map((vehicle) => (
                <VehicleCard key={vehicle.id} vehicle={vehicle} />
              ))}
            </div>
            {vehicles.total > 8 && (
              <div className="text-center mt-8">
                <Button variant="outline" asChild>
                  <Link href={`/search?dealer=${dealer.slug}`}>
                    Ver todo el inventario ({vehicles.total})
                  </Link>
                </Button>
              </div>
            )}
          </TabsContent>

          {/* About Tab */}
          <TabsContent value="about">
            <div className="grid md:grid-cols-3 gap-8">
              <div className="md:col-span-2">
                <Card>
                  <CardContent className="p-6">
                    <h3 className="font-semibold mb-4">Sobre {dealer.name}</h3>
                    <p className="text-gray-700 whitespace-pre-line">
                      {dealer.description || 'Sin descripción disponible'}
                    </p>
                  </CardContent>
                </Card>
              </div>

              <div className="space-y-6">
                {/* Hours */}
                <Card>
                  <CardContent className="p-6">
                    <h3 className="font-semibold mb-4 flex items-center gap-2">
                      <Clock className="w-4 h-4" />
                      Horarios
                    </h3>
                    <ul className="space-y-2 text-sm">
                      {dealer.businessHours?.map((hour) => (
                        <li key={hour.day} className="flex justify-between">
                          <span>{hour.day}</span>
                          <span className={hour.closed ? 'text-gray-400' : ''}>
                            {hour.closed ? 'Cerrado' : `${hour.open} - ${hour.close}`}
                          </span>
                        </li>
                      ))}
                    </ul>
                  </CardContent>
                </Card>

                {/* Contact */}
                <Card>
                  <CardContent className="p-6">
                    <h3 className="font-semibold mb-4">Contacto</h3>
                    <ul className="space-y-3">
                      <li>
                        <a href={`tel:${dealer.phone}`} className="flex items-center gap-2 text-gray-700 hover:text-primary">
                          <Phone className="w-4 h-4" />
                          {dealer.phone}
                        </a>
                      </li>
                      <li>
                        <a href={`mailto:${dealer.email}`} className="flex items-center gap-2 text-gray-700 hover:text-primary">
                          <Mail className="w-4 h-4" />
                          {dealer.email}
                        </a>
                      </li>
                      {dealer.website && (
                        <li>
                          <a href={dealer.website} target="_blank" className="flex items-center gap-2 text-gray-700 hover:text-primary">
                            <Globe className="w-4 h-4" />
                            Sitio web
                          </a>
                        </li>
                      )}
                    </ul>

                    {/* Social */}
                    {dealer.socialLinks && (
                      <div className="flex gap-3 mt-4 pt-4 border-t">
                        {dealer.socialLinks.facebook && (
                          <a href={dealer.socialLinks.facebook} target="_blank" className="text-gray-400 hover:text-blue-600">
                            <Facebook className="w-5 h-5" />
                          </a>
                        )}
                        {dealer.socialLinks.instagram && (
                          <a href={dealer.socialLinks.instagram} target="_blank" className="text-gray-400 hover:text-pink-600">
                            <Instagram className="w-5 h-5" />
                          </a>
                        )}
                        {dealer.socialLinks.twitter && (
                          <a href={dealer.socialLinks.twitter} target="_blank" className="text-gray-400 hover:text-sky-500">
                            <Twitter className="w-5 h-5" />
                          </a>
                        )}
                        {dealer.socialLinks.youtube && (
                          <a href={dealer.socialLinks.youtube} target="_blank" className="text-gray-400 hover:text-red-600">
                            <Youtube className="w-5 h-5" />
                          </a>
                        )}
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>
            </div>
          </TabsContent>

          {/* Reviews Tab */}
          <TabsContent value="reviews">
            <ReviewsList
              reviews={reviews.data}
              total={reviews.total}
              rating={dealer.rating}
              entityType="dealer"
              entityId={dealer.id}
            />
          </TabsContent>

          {/* Locations Tab */}
          <TabsContent value="locations">
            <div className="grid md:grid-cols-2 gap-6">
              <Card>
                <CardContent className="p-0 h-[400px]">
                  <GoogleMap
                    locations={dealer.locations}
                    center={dealer.locations[0]?.coordinates}
                  />
                </CardContent>
              </Card>
              <div className="space-y-4">
                {dealer.locations?.map((location) => (
                  <Card key={location.id}>
                    <CardContent className="p-4">
                      <h4 className="font-semibold">{location.name}</h4>
                      <p className="text-sm text-gray-600 mt-1">{location.address}</p>
                      <p className="text-sm text-gray-600">{location.city}</p>
                      {location.phone && (
                        <a href={`tel:${location.phone}`} className="text-sm text-primary hover:underline block mt-2">
                          {location.phone}
                        </a>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
```

---

## 📡 ENDPOINTS DE API

| Método | Endpoint                     | Descripción             | Servicio                |
| ------ | ---------------------------- | ----------------------- | ----------------------- |
| `GET`  | `/api/dealers/profile`       | Obtener perfil propio   | DealerManagementService |
| `PUT`  | `/api/dealers/profile`       | Actualizar perfil       | DealerManagementService |
| `GET`  | `/api/dealers/{slug}`        | Perfil público por slug | DealerManagementService |
| `GET`  | `/api/dealers/{id}/vehicles` | Vehículos del dealer    | VehiclesSaleService     |
| `GET`  | `/api/dealers/{id}/reviews`  | Reviews del dealer      | ReviewService           |

---

## 🧪 TESTS E2E

```typescript
// filepath: e2e/dealer-profile.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "./helpers/auth";

test.describe("Dealer Profile", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("can edit dealer profile", async ({ page }) => {
    await page.goto("/dealer/profile/edit");

    await page.fill('input[name="name"]', "Test Dealer Updated");
    await page.fill(
      'textarea[name="description"]',
      "Nueva descripción del dealer",
    );

    await page.click('button:has-text("Guardar")');

    await expect(page.getByText("Perfil actualizado")).toBeVisible();
  });

  test("public profile shows dealer info", async ({ page }) => {
    await page.goto("/dealers/automax-rd");

    await expect(
      page.getByRole("heading", { name: "AutoMax RD" }),
    ).toBeVisible();
    await expect(page.getByText("Verificado")).toBeVisible();
    await expect(page.getByRole("tab", { name: /inventario/i })).toBeVisible();
  });
});
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

- [ ] Editor de perfil con tabs (info, horarios, redes, galería, ubicaciones)
- [ ] Upload de logo y cover image
- [ ] Formulario con validación Zod
- [ ] Horarios de atención configurables
- [ ] Redes sociales editables
- [ ] Vista pública del perfil
- [ ] Tabs de inventario, about, reviews, ubicaciones
- [ ] Integración con Google Maps
- [ ] SEO con metadata dinámica
- [ ] Tests E2E

---

## 🔗 DOCUMENTOS RELACIONADOS

- [04-dealer-dashboard.md](04-dealer-dashboard.md) - Dashboard principal
- [11-dealer-employees-locations.md](11-dealer-employees-locations.md) - Gestión de ubicaciones
- [12-dealer-management-api.md](../../05-API-INTEGRATION/12-dealer-management-api.md) - API

---

_Última actualización: Enero 30, 2026_
