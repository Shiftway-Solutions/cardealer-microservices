# 🏷️ Admin Categories

> **Tiempo estimado:** 15 minutos  
> **Página:** AdminCategoriesPage

---

## 📋 OBJETIVO

Gestión de categorías y taxonomía:

- Marcas y modelos
- Tipos de vehículo
- Características

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ CATEGORÍAS                                      [+ Nueva]       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ [Marcas] [Modelos] [Tipos] [Características]                    │
│ ═══════                                                         │
│                                                                 │
│ MARCAS                                    [Buscar...]           │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🚗 Toyota           45 modelos    ✅ Activa    [Editar]     │ │
│ │ 🚗 Honda            38 modelos    ✅ Activa    [Editar]     │ │
│ │ 🚗 Hyundai          32 modelos    ✅ Activa    [Editar]     │ │
│ │ 🚗 Nissan           28 modelos    ✅ Activa    [Editar]     │ │
│ │ 🚗 Ford             25 modelos    ❌ Inactiva  [Editar]     │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(admin)/admin/categories/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { adminService } from '@/services/api/adminService';
import { Plus, Edit, Search, Car, Tag, Settings } from 'lucide-react';
import { toast } from 'sonner';

type CategoryType = 'makes' | 'models' | 'types' | 'features';

export default function AdminCategoriesPage() {
  const [activeTab, setActiveTab] = useState<CategoryType>('makes');
  const [search, setSearch] = useState('');
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [editItem, setEditItem] = useState<any>(null);
  const [formData, setFormData] = useState({ name: '', isActive: true });
  const queryClient = useQueryClient();

  const { data: items } = useQuery({
    queryKey: ['admin-categories', activeTab, search],
    queryFn: () => adminService.getCategories(activeTab, search),
  });

  const saveMutation = useMutation({
    mutationFn: (data: any) =>
      editItem
        ? adminService.updateCategory(activeTab, editItem.id, data)
        : adminService.createCategory(activeTab, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
      setIsAddOpen(false);
      setEditItem(null);
      setFormData({ name: '', isActive: true });
      toast.success(editItem ? 'Actualizado' : 'Creado');
    },
  });

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      adminService.updateCategory(activeTab, id, { isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
    },
  });

  const openEdit = (item: any) => {
    setEditItem(item);
    setFormData({ name: item.name, isActive: item.isActive });
    setIsAddOpen(true);
  };

  const openAdd = () => {
    setEditItem(null);
    setFormData({ name: '', isActive: true });
    setIsAddOpen(true);
  };

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Categorías</h1>
        <Button onClick={openAdd}>
          <Plus className="w-4 h-4 mr-2" />
          Nueva
        </Button>
      </div>

      <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as CategoryType)}>
        <TabsList className="mb-6">
          <TabsTrigger value="makes">Marcas</TabsTrigger>
          <TabsTrigger value="models">Modelos</TabsTrigger>
          <TabsTrigger value="types">Tipos</TabsTrigger>
          <TabsTrigger value="features">Características</TabsTrigger>
        </TabsList>

        <div className="mb-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              placeholder="Buscar..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-10"
            />
          </div>
        </div>

        <TabsContent value={activeTab}>
          <Card>
            <CardContent className="p-0">
              <div className="divide-y">
                {items?.map((item: any) => (
                  <div key={item.id} className="flex items-center justify-between p-4">
                    <div className="flex items-center gap-3">
                      <Car className="w-5 h-5 text-gray-400" />
                      <div>
                        <div className="font-medium">{item.name}</div>
                        {item.count !== undefined && (
                          <div className="text-sm text-gray-600">
                            {item.count} {activeTab === 'makes' ? 'modelos' : 'vehículos'}
                          </div>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="flex items-center gap-2">
                        <Switch
                          checked={item.isActive}
                          onCheckedChange={(checked) =>
                            toggleStatusMutation.mutate({ id: item.id, isActive: checked })
                          }
                        />
                        <span className="text-sm text-gray-600">
                          {item.isActive ? 'Activa' : 'Inactiva'}
                        </span>
                      </div>
                      <Button size="sm" variant="ghost" onClick={() => openEdit(item)}>
                        <Edit className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Add/Edit Dialog */}
      <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editItem ? 'Editar' : 'Nueva'} {activeTab.slice(0, -1)}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <Input
              placeholder="Nombre"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            />
            <div className="flex items-center gap-2">
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
              />
              <span>Activa</span>
            </div>
            <Button className="w-full" onClick={() => saveMutation.mutate(formData)}>
              {editItem ? 'Guardar Cambios' : 'Crear'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                            | Descripción       |
| -------- | ----------------------------------- | ----------------- |
| `GET`    | `/api/admin/categories/{type}`      | Listar categorías |
| `POST`   | `/api/admin/categories/{type}`      | Crear categoría   |
| `PUT`    | `/api/admin/categories/{type}/{id}` | Actualizar        |
| `DELETE` | `/api/admin/categories/{type}/{id}` | Eliminar          |

---

## ✅ CHECKLIST

- [ ] Tabs para tipos de categoría
- [ ] Búsqueda
- [ ] Toggle de estado activo/inactivo
- [ ] Modal de creación/edición
- [ ] Contador de items relacionados

---

_Última actualización: Enero 31, 2026_
