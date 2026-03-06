/**
 * Admin Content Page
 *
 * Manage static content, banners, and CMS
 */

'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  FileText,
  Image as ImageIcon,
  Plus,
  Edit,
  Trash2,
  Eye,
  Globe,
  Megaphone,
} from 'lucide-react';
import {
  useBanners,
  useStaticPages,
  useBlogPosts,
  useContentOverview,
  useDeleteBanner,
} from '@/hooks/use-admin-extended';

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'active':
    case 'published':
      return <Badge className="bg-primary/10 text-primary">Activo</Badge>;
    case 'scheduled':
      return <Badge className="bg-blue-100 text-blue-700">Programado</Badge>;
    case 'draft':
      return <Badge className="bg-muted text-foreground">Borrador</Badge>;
    case 'inactive':
      return <Badge variant="outline">Inactivo</Badge>;
    default:
      return <Badge variant="outline">{status}</Badge>;
  }
};

export default function AdminContentPage() {
  const [activeTab, setActiveTab] = useState<'banners' | 'pages' | 'blog'>('banners');
  const { data: banners = [] } = useBanners();
  const { data: pages = [] } = useStaticPages();
  const { data: blogPosts = [] } = useBlogPosts();
  const { data: overview } = useContentOverview();
  const deleteBanner = useDeleteBanner();

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold">Contenido</h1>
          <p className="text-muted-foreground">Gestión de banners, páginas y blog</p>
        </div>
        <Button className="bg-primary hover:bg-primary/90">
          <Plus className="mr-2 h-4 w-4" />
          Nuevo Contenido
        </Button>
      </div>

      {/* Tabs */}
      <div className="border-border flex gap-2 border-b pb-2">
        <Button
          variant={activeTab === 'banners' ? 'default' : 'ghost'}
          onClick={() => setActiveTab('banners')}
          className={activeTab === 'banners' ? 'bg-primary' : ''}
        >
          <Megaphone className="mr-2 h-4 w-4" />
          Banners
        </Button>
        <Button
          variant={activeTab === 'pages' ? 'default' : 'ghost'}
          onClick={() => setActiveTab('pages')}
          className={activeTab === 'pages' ? 'bg-primary' : ''}
        >
          <FileText className="mr-2 h-4 w-4" />
          Páginas
        </Button>
        <Button
          variant={activeTab === 'blog' ? 'default' : 'ghost'}
          onClick={() => setActiveTab('blog')}
          className={activeTab === 'blog' ? 'bg-primary' : ''}
        >
          <Globe className="mr-2 h-4 w-4" />
          Blog
        </Button>
      </div>

      {/* Banners Tab */}
      {activeTab === 'banners' && (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Banners Publicitarios</CardTitle>
              <CardDescription>Gestiona los banners en el sitio</CardDescription>
            </div>
            <Button className="bg-primary hover:bg-primary/90">
              <Plus className="mr-2 h-4 w-4" />
              Nuevo Banner
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {banners.map(banner => (
                <div
                  key={banner.id}
                  className="flex items-center justify-between rounded-lg border p-4"
                >
                  <div className="flex items-center gap-4">
                    <div className="flex h-16 w-32 items-center justify-center rounded bg-gradient-to-r from-gray-200 to-gray-300">
                      <ImageIcon className="text-muted-foreground h-6 w-6" />
                      <p className="text-muted-foreground text-sm">
                        Ubicación: {banner.placement.replace('_', ' ')}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p className="font-medium">{banner.views.toLocaleString()} vistas</p>
                      {getStatusBadge(banner.status)}
                    </div>
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon" aria-label="Ver banner">
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" aria-label="Editar banner">
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-red-500"
                        aria-label="Eliminar banner"
                        onClick={() => deleteBanner.mutate(banner.id)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Pages Tab */}
      {activeTab === 'pages' && (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Páginas Estáticas</CardTitle>
              <CardDescription>Contenido legal y páginas informativas</CardDescription>
            </div>
            <Button className="bg-primary hover:bg-primary/90">
              <Plus className="mr-2 h-4 w-4" />
              Nueva Página
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {pages.map(page => (
                <div
                  key={page.id}
                  className="hover:bg-muted/50 flex items-center justify-between rounded-lg border p-4"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-muted rounded-lg p-2">
                      <FileText className="text-muted-foreground h-5 w-5" />
                    </div>
                    <div>
                      <h4 className="font-medium">{page.title}</h4>
                      <p className="text-muted-foreground text-sm">{page.slug}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p className="text-muted-foreground text-sm">
                        Actualizado: {new Date(page.lastModified).toLocaleDateString('es-DO')}
                      </p>
                      {getStatusBadge(page.status)}
                    </div>
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon" aria-label="Ver página">
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" aria-label="Editar página">
                        <Edit className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Blog Tab */}
      {activeTab === 'blog' && (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Artículos del Blog</CardTitle>
              <CardDescription>Contenido editorial y guías</CardDescription>
            </div>
            <Button className="bg-primary hover:bg-primary/90">
              <Plus className="mr-2 h-4 w-4" />
              Nuevo Artículo
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {blogPosts.map(post => (
                <div
                  key={post.id}
                  className="hover:bg-muted/50 flex items-center justify-between rounded-lg border p-4"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-muted flex h-14 w-20 items-center justify-center rounded">
                      <ImageIcon className="text-muted-foreground h-5 w-5" />
                    </div>
                    <div>
                      <h4 className="font-medium">{post.title}</h4>
                      <p className="text-muted-foreground text-sm">
                        Por {post.author}{' '}
                        {post.publishedAt &&
                          `• ${new Date(post.publishedAt).toLocaleDateString('es-DO')}`}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    {getStatusBadge(post.status)}
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon" aria-label="Ver artículo">
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" aria-label="Editar artículo">
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" className="text-red-500" aria-label="Eliminar artículo">
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Content Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Megaphone className="h-8 w-8 text-purple-500" />
              <div>
                <p className="text-2xl font-bold">{banners.length}</p>
                <p className="text-muted-foreground text-sm">Banners</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <FileText className="h-8 w-8 text-blue-500" />
              <div>
                <p className="text-2xl font-bold">{pages.length}</p>
                <p className="text-muted-foreground text-sm">Páginas</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Globe className="text-primary h-8 w-8" />
              <div>
                <p className="text-2xl font-bold">{blogPosts.length}</p>
                <p className="text-muted-foreground text-sm">Artículos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Eye className="h-8 w-8 text-amber-500" />
              <div>
                <p className="text-2xl font-bold">
                  {overview?.totalViews?.toLocaleString() ?? '—'}
                </p>
                <p className="text-muted-foreground text-sm">Vistas Totales</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
