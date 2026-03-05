# 📋 Admin Reports & Listings

> **Tiempo estimado:** 20 minutos  
> **Página:** AdminReportsPage, AdminListingsPage

---

## 📋 OBJETIVO

Gestión de reportes de usuarios y listados:

- Reportes de contenido inapropiado
- Moderación de listados
- Acciones masivas

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ REPORTES              [Pendientes ▼]  [Tipo ▼]  [Buscar...]    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ⚠️ FRAUDE - Toyota Camry 2024                hace 2 horas   │ │
│ │    Reportado por: juan@email.com                            │ │
│ │    "El precio es demasiado bajo, parece estafa"             │ │
│ │    [Ver Vehículo] [Ignorar] [Suspender] [Eliminar]          │ │
│ ├─────────────────────────────────────────────────────────────┤ │
│ │ 🔞 CONTENIDO - Honda CR-V 2023               hace 5 horas   │ │
│ │    Reportado por: maria@email.com                           │ │
│ │    "Imágenes inapropiadas en la descripción"                │ │
│ │    [Ver Vehículo] [Ignorar] [Suspender] [Eliminar]          │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ LISTADOS PENDIENTES DE REVISIÓN                                 │
│ ┌───┬────────────────────────┬──────────┬─────────┬───────────┐ │
│ │ ☐ │ Vehículo               │ Vendedor │ Fecha   │ Acción    │ │
│ │ ☐ │ BMW X5 2024            │ Dealer A │ Hoy     │ [Aprobar] │ │
│ │ ☐ │ Mercedes C300          │ Juan P.  │ Ayer    │ [Aprobar] │ │
│ └───┴────────────────────────┴──────────┴─────────┴───────────┘ │
│ [Aprobar Seleccionados] [Rechazar Seleccionados]                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(admin)/admin/reports/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { adminService } from '@/services/api/adminService';
import { formatRelativeTime } from '@/lib/format';
import { AlertTriangle, Eye, X, Ban, Trash2, Check, Search } from 'lucide-react';
import { toast } from 'sonner';
import Link from 'next/link';

const reportTypes = [
  { value: 'all', label: 'Todos' },
  { value: 'fraud', label: 'Fraude', icon: '⚠️' },
  { value: 'inappropriate', label: 'Contenido', icon: '🔞' },
  { value: 'duplicate', label: 'Duplicado', icon: '📋' },
  { value: 'other', label: 'Otro', icon: '❓' },
];

export default function AdminReportsPage() {
  const [statusFilter, setStatusFilter] = useState('pending');
  const [typeFilter, setTypeFilter] = useState('all');
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const queryClient = useQueryClient();

  const { data: reports } = useQuery({
    queryKey: ['admin-reports', statusFilter, typeFilter, search],
    queryFn: () => adminService.getReports({ status: statusFilter, type: typeFilter, search }),
  });

  const { data: pendingListings } = useQuery({
    queryKey: ['admin-pending-listings'],
    queryFn: () => adminService.getPendingListings(),
  });

  const handleReportAction = useMutation({
    mutationFn: ({ reportId, action }: { reportId: string; action: string }) =>
      adminService.handleReport(reportId, action),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-reports'] });
      toast.success('Acción aplicada');
    },
  });

  const bulkApproveMutation = useMutation({
    mutationFn: (ids: string[]) => adminService.bulkApproveListings(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-pending-listings'] });
      setSelected([]);
      toast.success('Listados aprobados');
    },
  });

  const toggleSelect = (id: string) => {
    setSelected(prev =>
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    );
  };

  return (
    <div className="container max-w-5xl mx-auto py-8 px-4">
      <h1 className="text-2xl font-bold mb-6">Reportes y Listados</h1>

      {/* Reports Section */}
      <Card className="mb-8">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Reportes de Usuarios</CardTitle>
            <div className="flex gap-2">
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="pending">Pendientes</SelectItem>
                  <SelectItem value="resolved">Resueltos</SelectItem>
                  <SelectItem value="all">Todos</SelectItem>
                </SelectContent>
              </Select>
              <Select value={typeFilter} onValueChange={setTypeFilter}>
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {reportTypes.map(t => (
                    <SelectItem key={t.value} value={t.value}>
                      {t.icon} {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {reports?.map((report: any) => (
              <div key={report.id} className="border rounded-lg p-4">
                <div className="flex items-start justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <Badge variant={report.type === 'fraud' ? 'destructive' : 'warning'}>
                      {reportTypes.find(t => t.value === report.type)?.icon} {report.type.toUpperCase()}
                    </Badge>
                    <span className="font-medium">{report.vehicleTitle}</span>
                  </div>
                  <span className="text-sm text-gray-500">{formatRelativeTime(report.createdAt)}</span>
                </div>
                <p className="text-sm text-gray-600 mb-2">Reportado por: {report.reporterEmail}</p>
                <p className="bg-gray-50 p-2 rounded text-sm mb-3">"{report.reason}"</p>
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" asChild>
                    <Link href={`/admin/vehicles/${report.vehicleId}`}>
                      <Eye className="w-4 h-4 mr-1" />
                      Ver
                    </Link>
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => handleReportAction.mutate({ reportId: report.id, action: 'ignore' })}>
                    <X className="w-4 h-4 mr-1" />
                    Ignorar
                  </Button>
                  <Button size="sm" variant="outline" className="text-yellow-600" onClick={() => handleReportAction.mutate({ reportId: report.id, action: 'suspend' })}>
                    <Ban className="w-4 h-4 mr-1" />
                    Suspender
                  </Button>
                  <Button size="sm" variant="destructive" onClick={() => handleReportAction.mutate({ reportId: report.id, action: 'delete' })}>
                    <Trash2 className="w-4 h-4 mr-1" />
                    Eliminar
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Pending Listings */}
      <Card>
        <CardHeader>
          <CardTitle>Listados Pendientes de Revisión</CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-3 w-8">
                  <Checkbox
                    checked={selected.length === pendingListings?.length}
                    onCheckedChange={(checked) => {
                      if (checked) setSelected(pendingListings?.map((l: any) => l.id) || []);
                      else setSelected([]);
                    }}
                  />
                </th>
                <th className="pb-3">Vehículo</th>
                <th className="pb-3">Vendedor</th>
                <th className="pb-3">Fecha</th>
                <th className="pb-3">Acción</th>
              </tr>
            </thead>
            <tbody>
              {pendingListings?.map((listing: any) => (
                <tr key={listing.id} className="border-b">
                  <td className="py-3">
                    <Checkbox
                      checked={selected.includes(listing.id)}
                      onCheckedChange={() => toggleSelect(listing.id)}
                    />
                  </td>
                  <td className="py-3 font-medium">{listing.title}</td>
                  <td className="py-3 text-gray-600">{listing.sellerName}</td>
                  <td className="py-3 text-gray-600">{formatRelativeTime(listing.createdAt)}</td>
                  <td className="py-3">
                    <Button size="sm" variant="outline" className="text-green-600">
                      <Check className="w-4 h-4 mr-1" />
                      Aprobar
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {selected.length > 0 && (
            <div className="flex gap-2 mt-4 pt-4 border-t">
              <Button onClick={() => bulkApproveMutation.mutate(selected)}>
                Aprobar {selected.length} seleccionados
              </Button>
              <Button variant="destructive">
                Rechazar {selected.length} seleccionados
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                           | Descripción          |
| ------ | ---------------------------------- | -------------------- |
| `GET`  | `/api/admin/reports`               | Listar reportes      |
| `POST` | `/api/admin/reports/{id}/action`   | Acción sobre reporte |
| `GET`  | `/api/admin/listings/pending`      | Listados pendientes  |
| `POST` | `/api/admin/listings/bulk-approve` | Aprobar masivo       |

---

## ✅ CHECKLIST

- [ ] Lista de reportes con filtros
- [ ] Acciones por reporte (ignorar, suspender, eliminar)
- [ ] Tabla de listados pendientes
- [ ] Selección múltiple
- [ ] Acciones masivas

---

_Última actualización: Enero 31, 2026_
