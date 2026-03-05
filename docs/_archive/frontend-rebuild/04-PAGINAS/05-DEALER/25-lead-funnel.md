# 🎯 Embudo de Leads

> **Tiempo estimado:** 25 minutos  
> **Página:** DealerLeadFunnelPage

---

## 📋 OBJETIVO

Gestión de leads y consultas:

- Pipeline de leads
- Seguimiento de conversiones
- Acciones rápidas

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ LEADS                    [Filtrar ▼]  [Exportar]  [+ Nuevo Lead]│
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐ │
│ │ 🔵 NUEVOS    │ │ 🟡 CONTACTADO│ │ 🟠 NEGOCIANDO│ │ 🟢 CERRADO│ │
│ │     12       │ │      8       │ │      5       │ │     3    │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘ │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🔵 Juan Pérez                           hace 2 horas        │ │
│ │    Toyota Camry 2024 • $1,850,000                           │ │
│ │    📱 809-555-1234  📧 juan@email.com                       │ │
│ │    "Me interesa, ¿tienen financiamiento?"                   │ │
│ │    [Llamar] [WhatsApp] [Email] [Mover a ▼]                  │ │
│ ├─────────────────────────────────────────────────────────────┤ │
│ │ 🔵 María González                       hace 5 horas        │ │
│ │    Honda CR-V 2023 • $2,100,000                             │ │
│ │    📱 809-555-5678                                          │ │
│ │    "¿Está disponible para verlo mañana?"                    │ │
│ │    [Llamar] [WhatsApp] [Email] [Mover a ▼]                  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(dealer)/dealer/leads/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { dealerService } from '@/services/api/dealerService';
import { formatCurrency, formatRelativeTime } from '@/lib/format';
import { Phone, MessageCircle, Mail, ChevronDown, Plus, Download, Filter } from 'lucide-react';
import { toast } from 'sonner';

type LeadStatus = 'new' | 'contacted' | 'negotiating' | 'closed' | 'lost';

const statusConfig: Record<LeadStatus, { label: string; color: string; bg: string }> = {
  new: { label: 'Nuevo', color: 'text-blue-700', bg: 'bg-blue-100' },
  contacted: { label: 'Contactado', color: 'text-yellow-700', bg: 'bg-yellow-100' },
  negotiating: { label: 'Negociando', color: 'text-orange-700', bg: 'bg-orange-100' },
  closed: { label: 'Cerrado', color: 'text-green-700', bg: 'bg-green-100' },
  lost: { label: 'Perdido', color: 'text-gray-700', bg: 'bg-gray-100' },
};

export default function DealerLeadFunnelPage() {
  const [statusFilter, setStatusFilter] = useState<LeadStatus | 'all'>('all');
  const queryClient = useQueryClient();

  const { data: leads, isLoading } = useQuery({
    queryKey: ['dealer-leads', statusFilter],
    queryFn: () => dealerService.getLeads(statusFilter === 'all' ? undefined : statusFilter),
  });

  const { data: stats } = useQuery({
    queryKey: ['dealer-leads-stats'],
    queryFn: () => dealerService.getLeadStats(),
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ leadId, status }: { leadId: string; status: LeadStatus }) =>
      dealerService.updateLeadStatus(leadId, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-leads'] });
      queryClient.invalidateQueries({ queryKey: ['dealer-leads-stats'] });
      toast.success('Estado actualizado');
    },
  });

  const handleCall = (phone: string) => {
    window.location.href = `tel:${phone}`;
  };

  const handleWhatsApp = (phone: string, message?: string) => {
    const text = encodeURIComponent(message || 'Hola, te contacto desde OKLA');
    window.open(`https://wa.me/1${phone.replace(/\D/g, '')}?text=${text}`, '_blank');
  };

  const handleEmail = (email: string) => {
    window.location.href = `mailto:${email}`;
  };

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Leads</h1>
        <div className="flex gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline">
                <Filter className="w-4 h-4 mr-2" />
                {statusFilter === 'all' ? 'Todos' : statusConfig[statusFilter].label}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem onClick={() => setStatusFilter('all')}>
                Todos
              </DropdownMenuItem>
              {Object.entries(statusConfig).map(([key, val]) => (
                <DropdownMenuItem key={key} onClick={() => setStatusFilter(key as LeadStatus)}>
                  {val.label}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
          <Button variant="outline">
            <Download className="w-4 h-4 mr-2" />
            Exportar
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3 mb-6">
        {Object.entries(statusConfig).map(([key, val]) => (
          <Card
            key={key}
            className={`cursor-pointer transition-all ${statusFilter === key ? 'ring-2 ring-primary-500' : ''}`}
            onClick={() => setStatusFilter(key as LeadStatus)}
          >
            <CardContent className="p-4 text-center">
              <div className={`text-2xl font-bold ${val.color}`}>
                {stats?.[key] || 0}
              </div>
              <div className="text-sm text-gray-600">{val.label}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Leads List */}
      <div className="space-y-4">
        {isLoading ? (
          <div className="text-center py-8 text-gray-500">Cargando leads...</div>
        ) : leads?.length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-gray-500">No hay leads en esta categoría</p>
            </CardContent>
          </Card>
        ) : (
          leads?.map((lead: any) => (
            <Card key={lead.id} className="overflow-hidden">
              <CardContent className="p-4">
                {/* Header */}
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <Badge className={statusConfig[lead.status as LeadStatus].bg}>
                      {statusConfig[lead.status as LeadStatus].label}
                    </Badge>
                    <span className="font-semibold">{lead.name}</span>
                  </div>
                  <span className="text-sm text-gray-500">
                    {formatRelativeTime(lead.createdAt)}
                  </span>
                </div>

                {/* Vehicle */}
                <div className="mb-3">
                  <span className="font-medium">{lead.vehicleTitle}</span>
                  <span className="text-gray-600 ml-2">
                    • {formatCurrency(lead.vehiclePrice)}
                  </span>
                </div>

                {/* Contact Info */}
                <div className="flex gap-4 text-sm text-gray-600 mb-3">
                  {lead.phone && (
                    <span className="flex items-center gap-1">
                      📱 {lead.phone}
                    </span>
                  )}
                  {lead.email && (
                    <span className="flex items-center gap-1">
                      📧 {lead.email}
                    </span>
                  )}
                </div>

                {/* Message */}
                {lead.message && (
                  <div className="bg-gray-50 rounded-lg p-3 mb-4 text-sm italic">
                    "{lead.message}"
                  </div>
                )}

                {/* Actions */}
                <div className="flex flex-wrap gap-2">
                  {lead.phone && (
                    <>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleCall(lead.phone)}
                      >
                        <Phone className="w-4 h-4 mr-1" />
                        Llamar
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="text-green-600 border-green-600"
                        onClick={() => handleWhatsApp(lead.phone)}
                      >
                        <MessageCircle className="w-4 h-4 mr-1" />
                        WhatsApp
                      </Button>
                    </>
                  )}
                  {lead.email && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleEmail(lead.email)}
                    >
                      <Mail className="w-4 h-4 mr-1" />
                      Email
                    </Button>
                  )}

                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button size="sm" variant="outline" className="ml-auto">
                        Mover a
                        <ChevronDown className="w-4 h-4 ml-1" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      {Object.entries(statusConfig)
                        .filter(([key]) => key !== lead.status)
                        .map(([key, val]) => (
                          <DropdownMenuItem
                            key={key}
                            onClick={() => updateStatusMutation.mutate({
                              leadId: lead.id,
                              status: key as LeadStatus
                            })}
                          >
                            {val.label}
                          </DropdownMenuItem>
                        ))}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método  | Endpoint                         | Descripción           |
| ------- | -------------------------------- | --------------------- |
| `GET`   | `/api/dealers/leads?status=new`  | Listar leads          |
| `GET`   | `/api/dealers/leads/stats`       | Contadores por estado |
| `PATCH` | `/api/dealers/leads/{id}/status` | Cambiar estado        |
| `POST`  | `/api/dealers/leads`             | Crear lead manual     |
| `GET`   | `/api/dealers/leads/export`      | Exportar CSV          |

---

## ✅ CHECKLIST

- [ ] Contadores por estado (funnel)
- [ ] Lista de leads con filtro
- [ ] Acciones rápidas (llamar, WhatsApp, email)
- [ ] Cambio de estado con dropdown
- [ ] Mensaje del cliente visible
- [ ] Tiempo relativo

---

_Última actualización: Enero 31, 2026_
