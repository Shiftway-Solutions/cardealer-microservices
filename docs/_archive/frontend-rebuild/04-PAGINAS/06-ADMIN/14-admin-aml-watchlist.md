# 🔒 Admin AML Watchlist

> **Tiempo estimado:** 20 minutos  
> **Página:** AdminAMLWatchlistPage

---

## 📋 OBJETIVO

Panel de cumplimiento AML (Anti-Money Laundering):

- Lista de vigilancia
- Verificación de usuarios
- Alertas de transacciones sospechosas

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ AML COMPLIANCE                      [Exportar]  [+ Agregar]     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ RESUMEN                                                         │
│ ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐    │
│ │ 🔴 Altos   │ │ 🟡 Medios  │ │ 🟢 Bajos   │ │ ✅ Limpios │    │
│ │     5      │ │    12      │ │    28      │ │    1,245   │    │
│ └────────────┘ └────────────┘ └────────────┘ └────────────┘    │
│                                                                 │
│ ALERTAS RECIENTES                                               │
│ ┌───┬────────────────────┬────────────┬───────────┬───────────┐ │
│ │ ! │ Usuario            │ Tipo       │ Monto     │ Estado    │ │
│ │🔴│ Juan Pérez (RNC)   │ Alto valor │ $5.2M     │ [Revisar] │ │
│ │🟡│ María G. (Cédula)  │ Frecuencia │ $1.8M     │ [Revisar] │ │
│ │🟡│ Dealer XYZ         │ Estructura │ $3.1M     │ [Revisar] │ │
│ └───┴────────────────────┴────────────┴───────────┴───────────┘ │
│                                                                 │
│ LISTA DE VIGILANCIA                                             │
│ ┌────────────────────┬────────────┬───────────┬───────────────┐ │
│ │ Nombre/RNC         │ Agregado   │ Motivo    │ Acciones      │ │
│ │ 123456789          │ 15/01/2026 │ PEP       │ [Ver] [Quitar]│ │
│ │ 987654321          │ 10/01/2026 │ Sanción   │ [Ver] [Quitar]│ │
│ └────────────────────┴────────────┴───────────┴───────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(admin)/admin/aml/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { adminService } from '@/services/api/adminService';
import { formatCurrency, formatDate } from '@/lib/format';
import { Shield, AlertTriangle, Plus, Download, Eye, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

const riskLevels = {
  high: { label: 'Alto', color: 'bg-red-100 text-red-700', icon: '🔴' },
  medium: { label: 'Medio', color: 'bg-yellow-100 text-yellow-700', icon: '🟡' },
  low: { label: 'Bajo', color: 'bg-green-100 text-green-700', icon: '🟢' },
};

export default function AdminAMLWatchlistPage() {
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newEntry, setNewEntry] = useState({ identifier: '', reason: '' });
  const queryClient = useQueryClient();

  const { data: stats } = useQuery({
    queryKey: ['admin-aml-stats'],
    queryFn: () => adminService.getAMLStats(),
  });

  const { data: alerts } = useQuery({
    queryKey: ['admin-aml-alerts'],
    queryFn: () => adminService.getAMLAlerts(),
  });

  const { data: watchlist } = useQuery({
    queryKey: ['admin-aml-watchlist'],
    queryFn: () => adminService.getAMLWatchlist(),
  });

  const addToWatchlistMutation = useMutation({
    mutationFn: (data: { identifier: string; reason: string }) =>
      adminService.addToWatchlist(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-aml-watchlist'] });
      setIsAddOpen(false);
      setNewEntry({ identifier: '', reason: '' });
      toast.success('Agregado a lista de vigilancia');
    },
  });

  const removeFromWatchlistMutation = useMutation({
    mutationFn: (id: string) => adminService.removeFromWatchlist(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-aml-watchlist'] });
      toast.success('Removido de lista');
    },
  });

  return (
    <div className="container max-w-5xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold flex items-center gap-2">
          <Shield className="w-6 h-6" />
          AML Compliance
        </h1>
        <div className="flex gap-2">
          <Button variant="outline">
            <Download className="w-4 h-4 mr-2" />
            Exportar
          </Button>
          <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
            <DialogTrigger asChild>
              <Button>
                <Plus className="w-4 h-4 mr-2" />
                Agregar
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Agregar a Lista de Vigilancia</DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <Input
                  placeholder="RNC o Cédula"
                  value={newEntry.identifier}
                  onChange={(e) => setNewEntry({ ...newEntry, identifier: e.target.value })}
                />
                <Input
                  placeholder="Motivo (PEP, Sanción, etc.)"
                  value={newEntry.reason}
                  onChange={(e) => setNewEntry({ ...newEntry, reason: e.target.value })}
                />
                <Button
                  className="w-full"
                  onClick={() => addToWatchlistMutation.mutate(newEntry)}
                >
                  Agregar
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <Card>
          <CardContent className="pt-6 text-center">
            <div className="text-2xl font-bold text-red-600">{stats?.high || 0}</div>
            <div className="text-sm text-gray-600">🔴 Riesgo Alto</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6 text-center">
            <div className="text-2xl font-bold text-yellow-600">{stats?.medium || 0}</div>
            <div className="text-sm text-gray-600">🟡 Riesgo Medio</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6 text-center">
            <div className="text-2xl font-bold text-green-600">{stats?.low || 0}</div>
            <div className="text-sm text-gray-600">🟢 Riesgo Bajo</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6 text-center">
            <div className="text-2xl font-bold text-gray-600">{stats?.clean || 0}</div>
            <div className="text-sm text-gray-600">✅ Limpios</div>
          </CardContent>
        </Card>
      </div>

      {/* Alerts */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-yellow-500" />
            Alertas Recientes
          </CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-3 w-8"></th>
                <th className="pb-3">Usuario</th>
                <th className="pb-3">Tipo de Alerta</th>
                <th className="pb-3 text-right">Monto</th>
                <th className="pb-3">Acción</th>
              </tr>
            </thead>
            <tbody>
              {alerts?.map((alert: any) => (
                <tr key={alert.id} className="border-b">
                  <td className="py-3">{riskLevels[alert.riskLevel as keyof typeof riskLevels]?.icon}</td>
                  <td className="py-3">
                    <div className="font-medium">{alert.userName}</div>
                    <div className="text-sm text-gray-600">{alert.identifier}</div>
                  </td>
                  <td className="py-3">
                    <Badge className={riskLevels[alert.riskLevel as keyof typeof riskLevels]?.color}>
                      {alert.alertType}
                    </Badge>
                  </td>
                  <td className="py-3 text-right font-medium">{formatCurrency(alert.amount)}</td>
                  <td className="py-3">
                    <Button size="sm" variant="outline">
                      <Eye className="w-4 h-4 mr-1" />
                      Revisar
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>

      {/* Watchlist */}
      <Card>
        <CardHeader>
          <CardTitle>Lista de Vigilancia</CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full">
            <thead>
              <tr className="border-b text-left">
                <th className="pb-3">Identificador</th>
                <th className="pb-3">Agregado</th>
                <th className="pb-3">Motivo</th>
                <th className="pb-3">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {watchlist?.map((entry: any) => (
                <tr key={entry.id} className="border-b">
                  <td className="py-3 font-mono">{entry.identifier}</td>
                  <td className="py-3 text-gray-600">{formatDate(entry.createdAt)}</td>
                  <td className="py-3">
                    <Badge variant="secondary">{entry.reason}</Badge>
                  </td>
                  <td className="py-3">
                    <div className="flex gap-2">
                      <Button size="sm" variant="ghost">
                        <Eye className="w-4 h-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-red-600"
                        onClick={() => removeFromWatchlistMutation.mutate(entry.id)}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                        | Descripción         |
| -------- | ------------------------------- | ------------------- |
| `GET`    | `/api/admin/aml/stats`          | Estadísticas AML    |
| `GET`    | `/api/admin/aml/alerts`         | Alertas recientes   |
| `GET`    | `/api/admin/aml/watchlist`      | Lista de vigilancia |
| `POST`   | `/api/admin/aml/watchlist`      | Agregar a lista     |
| `DELETE` | `/api/admin/aml/watchlist/{id}` | Remover de lista    |

---

## ✅ CHECKLIST

- [ ] Cards de estadísticas por riesgo
- [ ] Tabla de alertas recientes
- [ ] Tabla de lista de vigilancia
- [ ] Modal para agregar entradas
- [ ] Acciones de revisión y eliminación

---

_Última actualización: Enero 31, 2026_
