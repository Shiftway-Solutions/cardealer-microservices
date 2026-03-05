---
title: "Dealer - CRM y Leads"
priority: P1
estimated_time: "45 minutos"
dependencies: []
apis: ["ContactService"]
status: complete
last_updated: "2026-01-30"
---

# 📊 Dealer - CRM y Leads

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Dashboard dealer, CRMService
> **Roles:** DLR-ADMIN

---

## 📋 OBJETIVO

Implementar CRM para dealers:

- Pipeline visual de leads
- Lista de leads con filtros
- Detalle de lead
- Seguimiento y notas

---

## 🔧 PASO 1: Página CRM

```typescript
// filepath: src/app/(main)/dealer/crm/page.tsx
import { Metadata } from "next";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { LeadsPipeline } from "@/components/dealer/crm/LeadsPipeline";
import { LeadsList } from "@/components/dealer/crm/LeadsList";

export const metadata: Metadata = {
  title: "CRM | Dealer Dashboard",
};

export default function CRMPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">CRM</h1>
        <p className="text-gray-600">Gestiona tus leads y oportunidades</p>
      </div>

      <Tabs defaultValue="pipeline">
        <TabsList>
          <TabsTrigger value="pipeline">Pipeline</TabsTrigger>
          <TabsTrigger value="list">Lista</TabsTrigger>
        </TabsList>

        <TabsContent value="pipeline" className="mt-6">
          <LeadsPipeline />
        </TabsContent>

        <TabsContent value="list" className="mt-6">
          <LeadsList />
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

---

## 🔧 PASO 2: LeadsPipeline (Kanban)

```typescript
// filepath: src/components/dealer/crm/LeadsPipeline.tsx
"use client";

import { useMemo } from "react";
import { useLeads, useUpdateLeadStatus } from "@/lib/hooks/useLeads";
import { LeadCard } from "./LeadCard";
import type { Lead, LeadStatus } from "@/types";

const PIPELINE_STAGES: { status: LeadStatus; label: string; color: string }[] = [
  { status: "new", label: "Nuevos", color: "bg-blue-500" },
  { status: "contacted", label: "Contactados", color: "bg-yellow-500" },
  { status: "qualified", label: "Calificados", color: "bg-purple-500" },
  { status: "negotiation", label: "Negociación", color: "bg-orange-500" },
  { status: "won", label: "Ganados", color: "bg-green-500" },
  { status: "lost", label: "Perdidos", color: "bg-gray-500" },
];

export function LeadsPipeline() {
  const { data: leads } = useLeads();
  const { mutate: updateStatus } = useUpdateLeadStatus();

  const leadsByStatus = useMemo(() => {
    const grouped: Record<string, Lead[]> = {};
    PIPELINE_STAGES.forEach((s) => (grouped[s.status] = []));
    leads?.forEach((lead) => {
      if (grouped[lead.status]) {
        grouped[lead.status].push(lead);
      }
    });
    return grouped;
  }, [leads]);

  const handleDrop = (leadId: string, newStatus: LeadStatus) => {
    updateStatus({ leadId, status: newStatus });
  };

  return (
    <div className="flex gap-4 overflow-x-auto pb-4">
      {PIPELINE_STAGES.map((stage) => (
        <PipelineColumn
          key={stage.status}
          stage={stage}
          leads={leadsByStatus[stage.status] || []}
          onDrop={handleDrop}
        />
      ))}
    </div>
  );
}

function PipelineColumn({
  stage,
  leads,
  onDrop,
}: {
  stage: (typeof PIPELINE_STAGES)[0];
  leads: Lead[];
  onDrop: (leadId: string, status: LeadStatus) => void;
}) {
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const leadId = e.dataTransfer.getData("leadId");
    if (leadId) {
      onDrop(leadId, stage.status);
    }
  };

  return (
    <div
      className="flex-shrink-0 w-72 bg-gray-50 rounded-xl p-4"
      onDragOver={handleDragOver}
      onDrop={handleDrop}
    >
      {/* Header */}
      <div className="flex items-center gap-2 mb-4">
        <div className={`w-3 h-3 rounded-full ${stage.color}`} />
        <h3 className="font-medium text-gray-900">{stage.label}</h3>
        <span className="text-sm text-gray-500 ml-auto">{leads.length}</span>
      </div>

      {/* Cards */}
      <div className="space-y-3">
        {leads.map((lead) => (
          <LeadCard key={lead.id} lead={lead} draggable />
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: LeadCard

```typescript
// filepath: src/components/dealer/crm/LeadCard.tsx
"use client";

import Link from "next/link";
import { Phone, Mail, Car, Clock } from "lucide-react";
import { formatRelativeDate } from "@/lib/utils";
import type { Lead } from "@/types";

interface LeadCardProps {
  lead: Lead;
  draggable?: boolean;
}

export function LeadCard({ lead, draggable }: LeadCardProps) {
  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData("leadId", lead.id);
  };

  return (
    <Link
      href={`/dealer/crm/leads/${lead.id}`}
      draggable={draggable}
      onDragStart={handleDragStart}
      className="block bg-white rounded-lg border p-4 hover:shadow-md transition-shadow cursor-grab active:cursor-grabbing"
    >
      {/* Name & Score */}
      <div className="flex items-start justify-between mb-2">
        <h4 className="font-medium text-gray-900">{lead.name}</h4>
        {lead.score && (
          <span
            className={`text-xs font-medium px-2 py-0.5 rounded ${
              lead.score >= 80
                ? "bg-green-100 text-green-700"
                : lead.score >= 50
                ? "bg-yellow-100 text-yellow-700"
                : "bg-gray-100 text-gray-700"
            }`}
          >
            {lead.score}%
          </span>
        )}
      </div>

      {/* Vehicle Interest */}
      {lead.vehicleInterest && (
        <div className="flex items-center gap-1 text-sm text-gray-600 mb-2">
          <Car size={14} />
          <span className="truncate">{lead.vehicleInterest}</span>
        </div>
      )}

      {/* Contact Info */}
      <div className="flex items-center gap-3 text-xs text-gray-500">
        {lead.phone && (
          <span className="flex items-center gap-1">
            <Phone size={12} />
            {lead.phone}
          </span>
        )}
        {lead.email && (
          <span className="flex items-center gap-1">
            <Mail size={12} />
          </span>
        )}
      </div>

      {/* Time */}
      <div className="flex items-center gap-1 text-xs text-gray-400 mt-2">
        <Clock size={12} />
        {formatRelativeDate(lead.createdAt)}
      </div>
    </Link>
  );
}
```

---

## 🔧 PASO 4: Lead Detail

```typescript
// filepath: src/app/(main)/dealer/crm/leads/[id]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { LeadHeader } from "@/components/dealer/crm/LeadHeader";
import { LeadInfo } from "@/components/dealer/crm/LeadInfo";
import { LeadTimeline } from "@/components/dealer/crm/LeadTimeline";
import { LeadNotes } from "@/components/dealer/crm/LeadNotes";
import { crmService } from "@/lib/services/crmService";

interface Props {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const lead = await crmService.getLeadById(id);
  return { title: lead ? `${lead.name} | CRM` : "Lead no encontrado" };
}

export default async function LeadDetailPage({ params }: Props) {
  const { id } = await params;
  const lead = await crmService.getLeadById(id);

  if (!lead) notFound();

  return (
    <div className="space-y-6">
      <LeadHeader lead={lead} />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          <LeadInfo lead={lead} />
          <LeadTimeline leadId={lead.id} />
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          <LeadNotes leadId={lead.id} />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: LeadTimeline

```typescript
// filepath: src/components/dealer/crm/LeadTimeline.tsx
"use client";

import { Phone, Mail, MessageCircle, Calendar, Check } from "lucide-react";
import { useLeadActivity } from "@/lib/hooks/useLeadActivity";
import { formatRelativeDate } from "@/lib/utils";

const activityIcons = {
  call: Phone,
  email: Mail,
  message: MessageCircle,
  appointment: Calendar,
  status_change: Check,
};

interface Props {
  leadId: string;
}

export function LeadTimeline({ leadId }: Props) {
  const { data: activities } = useLeadActivity(leadId);

  return (
    <div className="bg-white rounded-xl border p-6">
      <h3 className="font-semibold text-gray-900 mb-4">Actividad</h3>

      <div className="space-y-4">
        {activities?.map((activity, index) => {
          const Icon = activityIcons[activity.type as keyof typeof activityIcons] || Check;

          return (
            <div key={activity.id} className="flex gap-3">
              {/* Icon */}
              <div className="flex flex-col items-center">
                <div className="w-8 h-8 rounded-full bg-primary-100 flex items-center justify-center">
                  <Icon size={14} className="text-primary-600" />
                </div>
                {index < (activities?.length ?? 0) - 1 && (
                  <div className="w-0.5 h-full bg-gray-200 mt-2" />
                )}
              </div>

              {/* Content */}
              <div className="flex-1 pb-4">
                <p className="text-sm text-gray-900">{activity.description}</p>
                <p className="text-xs text-gray-500 mt-1">
                  {activity.createdBy} • {formatRelativeDate(activity.createdAt)}
                </p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: LeadsList (Vista de Lista)

```typescript
// filepath: src/components/dealer/crm/LeadsList.tsx
"use client";

import { useState } from "react";
import { useLeads } from "@/lib/hooks/useLeads";
import { DataTable } from "@/components/ui/DataTable";
import { LeadFilters } from "./LeadFilters";
import { LeadActions } from "./LeadActions";
import { formatDate, formatCurrency } from "@/lib/utils";
import type { Lead, LeadStatus } from "@/types";

const statusColors = {
  new: "bg-blue-100 text-blue-700",
  contacted: "bg-yellow-100 text-yellow-700",
  qualified: "bg-purple-100 text-purple-700",
  negotiation: "bg-orange-100 text-orange-700",
  won: "bg-green-100 text-green-700",
  lost: "bg-gray-100 text-gray-700",
};

export function LeadsList() {
  const [filters, setFilters] = useState({
    status: "" as LeadStatus | "",
    search: "",
    dateFrom: "",
    dateTo: "",
  });

  const { data, isLoading } = useLeads(filters);

  const columns = [
    {
      header: "Lead",
      accessorKey: "name",
      cell: (row: Lead) => (
        <div>
          <p className="font-medium">{row.name}</p>
          <p className="text-sm text-gray-500">{row.email}</p>
        </div>
      ),
    },
    {
      header: "Vehículo",
      accessorKey: "vehicleInterest",
      cell: (row: Lead) => (
        <div className="max-w-xs truncate">
          {row.vehicleInterest || "—"}
        </div>
      ),
    },
    {
      header: "Estado",
      accessorKey: "status",
      cell: (row: Lead) => (
        <span className={`px-2 py-1 text-xs font-medium rounded ${statusColors[row.status]}`}>
          {row.status}
        </span>
      ),
    },
    {
      header: "Score",
      accessorKey: "score",
      cell: (row: Lead) => (
        <div className="flex items-center gap-2">
          <div className="w-16 bg-gray-200 rounded-full h-2">
            <div
              className="bg-primary-600 h-2 rounded-full"
              style={{ width: `${row.score}%` }}
            />
          </div>
          <span className="text-sm">{row.score}%</span>
        </div>
      ),
    },
    {
      header: "Origen",
      accessorKey: "source",
      cell: (row: Lead) => row.source || "Web",
    },
    {
      header: "Fecha",
      accessorKey: "createdAt",
      cell: (row: Lead) => formatDate(row.createdAt, "dd MMM yyyy"),
    },
    {
      header: "Acciones",
      cell: (row: Lead) => <LeadActions lead={row} />,
    },
  ];

  return (
    <div className="space-y-4">
      <LeadFilters filters={filters} onChange={setFilters} />
      <DataTable
        columns={columns}
        data={data?.items || []}
        isLoading={isLoading}
        emptyMessage="No hay leads que coincidan con los filtros"
      />
    </div>
  );
}
```

---

## 🔧 PASO 7: LeadFilters

```typescript
// filepath: src/components/dealer/crm/LeadFilters.tsx
"use client";

import { Search, Filter, Download } from "lucide-react";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { Button } from "@/components/ui/Button";
import { useExportLeads } from "@/lib/hooks/useLeads";

interface LeadFiltersProps {
  filters: {
    status: string;
    search: string;
    dateFrom: string;
    dateTo: string;
  };
  onChange: (filters: any) => void;
}

export function LeadFilters({ filters, onChange }: LeadFiltersProps) {
  const { mutate: exportLeads, isPending: isExporting } = useExportLeads();

  return (
    <div className="bg-white rounded-lg border p-4">
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Search */}
        <div className="relative">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <Input
            value={filters.search}
            onChange={(e) => onChange({ ...filters, search: e.target.value })}
            placeholder="Buscar por nombre, email..."
            className="pl-9"
          />
        </div>

        {/* Status */}
        <Select
          value={filters.status}
          onChange={(e) => onChange({ ...filters, status: e.target.value })}
        >
          <option value="">Todos los estados</option>
          <option value="new">Nuevos</option>
          <option value="contacted">Contactados</option>
          <option value="qualified">Calificados</option>
          <option value="negotiation">Negociación</option>
          <option value="won">Ganados</option>
          <option value="lost">Perdidos</option>
        </Select>

        {/* Date Range */}
        <Input
          type="date"
          value={filters.dateFrom}
          onChange={(e) => onChange({ ...filters, dateFrom: e.target.value })}
          placeholder="Desde"
        />
        <Input
          type="date"
          value={filters.dateTo}
          onChange={(e) => onChange({ ...filters, dateTo: e.target.value })}
          placeholder="Hasta"
        />
      </div>

      {/* Export Button */}
      <div className="mt-4 flex justify-end">
        <Button
          variant="outline"
          size="sm"
          onClick={() => exportLeads(filters)}
          disabled={isExporting}
        >
          <Download size={14} className="mr-2" />
          {isExporting ? "Exportando..." : "Exportar CSV"}
        </Button>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 8: Lead Scoring

```typescript
// filepath: src/components/dealer/crm/LeadScoreBreakdown.tsx
"use client";

import { TrendingUp, User, Car, Clock, DollarSign } from "lucide-react";
import type { Lead } from "@/types";

interface LeadScoreBreakdownProps {
  lead: Lead;
}

export function LeadScoreBreakdown({ lead }: LeadScoreBreakdownProps) {
  const scoreFactors = [
    {
      icon: User,
      label: "Información completa",
      score: lead.hasCompleteInfo ? 20 : 0,
      max: 20,
      color: "text-blue-600",
    },
    {
      icon: Car,
      label: "Interés específico",
      score: lead.hasVehicleInterest ? 25 : 0,
      max: 25,
      color: "text-purple-600",
    },
    {
      icon: Clock,
      label: "Respuesta rápida",
      score: lead.responseTime < 24 ? 15 : lead.responseTime < 72 ? 10 : 5,
      max: 15,
      color: "text-green-600",
    },
    {
      icon: DollarSign,
      label: "Presupuesto definido",
      score: lead.hasBudget ? 20 : 0,
      max: 20,
      color: "text-amber-600",
    },
    {
      icon: TrendingUp,
      label: "Engagement",
      score: Math.min(lead.interactionCount * 5, 20),
      max: 20,
      color: "text-pink-600",
    },
  ];

  return (
    <div className="bg-white rounded-xl border p-6">
      <div className="flex items-center justify-between mb-6">
        <h3 className="font-semibold text-gray-900">Lead Score</h3>
        <div className="text-3xl font-bold text-primary-600">
          {lead.score}%
        </div>
      </div>

      <div className="space-y-4">
        {scoreFactors.map((factor) => (
          <div key={factor.label}>
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <factor.icon size={16} className={factor.color} />
                <span className="text-sm text-gray-700">{factor.label}</span>
              </div>
              <span className="text-sm font-medium">
                {factor.score}/{factor.max}
              </span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className={`${factor.color.replace("text", "bg")} h-2 rounded-full`}
                style={{ width: `${(factor.score / factor.max) * 100}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 9: Bulk Actions

```typescript
// filepath: src/components/dealer/crm/BulkLeadActions.tsx
"use client";

import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { useBulkUpdateLeads } from "@/lib/hooks/useLeads";
import { Mail, Tag, Trash2 } from "lucide-react";

interface BulkLeadActionsProps {
  selectedLeads: string[];
  onComplete: () => void;
}

export function BulkLeadActions({ selectedLeads, onComplete }: BulkLeadActionsProps) {
  const [action, setAction] = useState("");
  const { mutate: bulkUpdate, isPending } = useBulkUpdateLeads();

  const handleExecute = () => {
    if (!action || selectedLeads.length === 0) return;

    const [actionType, value] = action.split(":");

    bulkUpdate(
      {
        leadIds: selectedLeads,
        action: actionType,
        value,
      },
      {
        onSuccess: () => {
          onComplete();
          setAction("");
        },
      }
    );
  };

  return (
    <div className="flex items-center gap-3 bg-blue-50 border border-blue-200 rounded-lg p-4">
      <span className="text-sm font-medium text-blue-700">
        {selectedLeads.length} seleccionado(s)
      </span>

      <Select
        value={action}
        onChange={(e) => setAction(e.target.value)}
        className="min-w-[200px]"
      >
        <option value="">Acción masiva...</option>
        <optgroup label="Cambiar estado">
          <option value="status:contacted">Marcar como contactado</option>
          <option value="status:qualified">Marcar como calificado</option>
          <option value="status:lost">Marcar como perdido</option>
        </optgroup>
        <optgroup label="Otras acciones">
          <option value="tag:hot">Etiquetar como "Hot"</option>
          <option value="tag:cold">Etiquetar como "Cold"</option>
          <option value="email:followup">Enviar email de seguimiento</option>
          <option value="delete">Eliminar</option>
        </optgroup>
      </Select>

      <Button
        onClick={handleExecute}
        disabled={!action || isPending}
        size="sm"
      >
        {isPending ? "Procesando..." : "Ejecutar"}
      </Button>
    </div>
  );
}
```

---

## 🔧 PASO 10: Integraciones CRM

```typescript
// filepath: src/lib/services/crmService.ts
import { api } from "@/lib/api";
import type { Lead, LeadActivity, LeadNote, LeadFilters } from "@/types";

class CRMService {
  private baseUrl = "/api/crm";

  // Leads
  async getLeads(filters?: LeadFilters) {
    const { data } = await api.get(`${this.baseUrl}/leads`, {
      params: filters,
    });
    return data;
  }

  async getLeadById(id: string) {
    const { data } = await api.get(`${this.baseUrl}/leads/${id}`);
    return data;
  }

  async createLead(lead: Partial<Lead>) {
    const { data } = await api.post(`${this.baseUrl}/leads`, lead);
    return data;
  }

  async updateLeadStatus(leadId: string, status: string) {
    const { data } = await api.patch(`${this.baseUrl}/leads/${leadId}/status`, {
      status,
    });
    return data;
  }

  async bulkUpdateLeads(leadIds: string[], action: string, value?: any) {
    const { data } = await api.post(`${this.baseUrl}/leads/bulk`, {
      leadIds,
      action,
      value,
    });
    return data;
  }

  // Activity
  async getLeadActivity(leadId: string) {
    const { data } = await api.get(`${this.baseUrl}/leads/${leadId}/activity`);
    return data;
  }

  async addActivity(leadId: string, activity: Partial<LeadActivity>) {
    const { data } = await api.post(
      `${this.baseUrl}/leads/${leadId}/activity`,
      activity,
    );
    return data;
  }

  // Notes
  async getNotes(leadId: string) {
    const { data } = await api.get(`${this.baseUrl}/leads/${leadId}/notes`);
    return data;
  }

  async createNote(leadId: string, content: string) {
    const { data } = await api.post(`${this.baseUrl}/leads/${leadId}/notes`, {
      content,
    });
    return data;
  }

  async updateNote(noteId: string, content: string) {
    const { data } = await api.patch(`${this.baseUrl}/notes/${noteId}`, {
      content,
    });
    return data;
  }

  async deleteNote(noteId: string) {
    await api.delete(`${this.baseUrl}/notes/${noteId}`);
  }

  // Export
  async exportLeads(filters?: LeadFilters) {
    const response = await api.get(`${this.baseUrl}/leads/export`, {
      params: filters,
      responseType: "blob",
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `leads-${Date.now()}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  }
}

export const crmService = new CRMService();
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /dealer/crm muestra pipeline y lista
# - Drag & drop funciona en pipeline
# - Filtros funcionan en lista
# - Detalle de lead carga con score
# - Timeline muestra actividad
# - Bulk actions funcionan
# - Export CSV funciona
```

---

## 📊 TIPOS TYPESCRIPT

```typescript
// filepath: src/types/crm.types.ts

export type LeadStatus =
  | "new"
  | "contacted"
  | "qualified"
  | "negotiation"
  | "won"
  | "lost";

export type LeadSource =
  | "web"
  | "phone"
  | "email"
  | "referral"
  | "social"
  | "other";

export interface Lead {
  id: string;
  name: string;
  email: string;
  phone?: string;
  status: LeadStatus;
  score: number;
  source: LeadSource;
  vehicleInterest?: string;
  vehicleId?: string;
  budget?: number;
  notes?: string;
  tags: string[];

  // Scoring factors
  hasCompleteInfo: boolean;
  hasVehicleInterest: boolean;
  hasBudget: boolean;
  responseTime: number; // hours
  interactionCount: number;

  assignedTo?: string;
  lastContactedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface LeadActivity {
  id: string;
  leadId: string;
  type: "call" | "email" | "message" | "appointment" | "status_change";
  description: string;
  createdBy: string;
  createdAt: string;
}

export interface LeadNote {
  id: string;
  leadId: string;
  content: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface LeadFilters {
  status?: LeadStatus;
  search?: string;
  dateFrom?: string;
  dateTo?: string;
  source?: LeadSource;
  minScore?: number;
  maxScore?: number;
  assignedTo?: string;
}
```

---

## 📚 DOCUMENTACIÓN CONSOLIDADA

> **Archivos fusionados en este documento:**
>
> - `10-dealer-crm.md` (CRM completo con pipeline Kanban)
> - `10-dealer-crm.md` (Dashboard y detalle de leads)
> - `10-dealer-crm.md` (Páginas de Lead Scoring)

### 🏗️ Arquitectura CRM Suite Completa

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           CRM & LEAD SCORING SUITE                              │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  SERVICIOS BACKEND                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   CRMService    │  │ ContactService  │  │LeadScoringService│                │
│  │    :5085        │  │    :5075        │  │     :5055        │                │
│  │  ─────────────  │  │  ─────────────  │  │  ─────────────   │                │
│  │  • Leads CRUD   │  │  • Mensajería   │  │  • ML Scoring    │                │
│  │  • Deals        │  │  • Inquiries    │  │  • Hot/Warm/Cold │                │
│  │  • Activities   │  │  • Threads      │  │  • Score 0-100   │                │
│  │  • Pipeline     │  │  • Templates    │  │  • Probability   │                │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘                 │
│           │                    │                    │                           │
│           └────────────────────┼────────────────────┘                           │
│                                ▼                                                │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │                        FRONTEND PAGES                                     │  │
│  ├──────────────────────────────────────────────────────────────────────────┤  │
│  │                                                                           │  │
│  │  ┌─────────────────────────────────────────────────────────────────────┐ │  │
│  │  │                  LeadsDashboard.tsx (428 LOC)                       │ │  │
│  │  │  ┌──────────────────────────────────────────────────────────────┐  │ │  │
│  │  │  │ Statistics: Total | Hot 🔥 | Avg Score | Conversion Rate     │  │ │  │
│  │  │  ├──────────────────────────────────────────────────────────────┤  │ │  │
│  │  │  │ Filters: 🔍 Search | 🌡️ Temperature | 📊 Status              │  │ │  │
│  │  │  ├──────────────────────────────────────────────────────────────┤  │ │  │
│  │  │  │ Table: User | Vehicle | Score Bar | Temp | Status | Actions  │  │ │  │
│  │  │  └──────────────────────────────────────────────────────────────┘  │ │  │
│  │  └─────────────────────────────────────────────────────────────────────┘ │  │
│  │                                    │                                      │  │
│  │                                    ▼                                      │  │
│  │  ┌─────────────────────────────────────────────────────────────────────┐ │  │
│  │  │                    LeadDetail.tsx (439 LOC)                         │ │  │
│  │  │  ┌────────────────────┐  ┌────────────────────────────────────────┐│ │  │
│  │  │  │  Score Overview    │  │  Sidebar                               ││ │  │
│  │  │  │  • Total Score     │  │  • Quick Stats (views, contacts)      ││ │  │
│  │  │  │  • Temperature 🔥  │  │  • Status Selector                    ││ │  │
│  │  │  │  • Probability %   │  │  • Notes (editable textarea)          ││ │  │
│  │  │  │  • Score Breakdown │  │  • Activity Timeline                  ││ │  │
│  │  │  ├────────────────────┤  └────────────────────────────────────────┘│ │  │
│  │  │  │  Vehicle Card      │                                            │ │  │
│  │  │  │  Activity Log      │                                            │ │  │
│  │  │  └────────────────────┘                                            │ │  │
│  │  └─────────────────────────────────────────────────────────────────────┘ │  │
│  │                                                                           │  │
│  │  ┌─────────────────────────────────────────────────────────────────────┐ │  │
│  │  │                LeadsPipelinePage.tsx (Kanban)                       │ │  │
│  │  │  ┌────────┐ ┌──────────┐ ┌───────────┐ ┌────────────┐ ┌─────────┐  │ │  │
│  │  │  │ NUEVOS │ │CONTACTADO│ │CALIFICADO │ │NEGOCIACIÓN │ │ GANADOS │  │ │  │
│  │  │  │  (12)  │→│   (8)    │→│    (5)    │→│    (3)     │→│   (2)   │  │ │  │
│  │  │  │ ┌────┐ │ │ ┌────┐   │ │ ┌────┐    │ │ ┌────┐     │ │ ┌────┐  │  │ │  │
│  │  │  │ │Lead│ │ │ │Lead│   │ │ │Lead│    │ │ │Lead│     │ │ │Lead│  │  │ │  │
│  │  │  │ └────┘ │ │ └────┘   │ │ └────┘    │ │ └────┘     │ │ └────┘  │  │ │  │
│  │  │  └────────┘ └──────────┘ └───────────┘ └────────────┘ └─────────┘  │ │  │
│  │  │              ← DRAG & DROP with @dnd-kit/core →                    │ │  │
│  │  └─────────────────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
│  LEAD SCORING MODEL                                                            │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │  Score = Engagement (40%) + Recency (30%) + Intent (30%)                 │  │
│  ├──────────────────────────────────────────────────────────────────────────┤  │
│  │  Temperature:  ❄️ COLD (0-30)  🟡 WARM (31-60)  🔥 HOT (61-80)  ✅ READY (81-100) │  │
│  ├──────────────────────────────────────────────────────────────────────────┤  │
│  │  Engagement Factors:                                                      │  │
│  │   • viewCount × 2        • favoriteCount × 5     • shareCount × 3        │  │
│  │   • contactCount × 10    • comparisonCount × 4   • testDrive × 15        │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 📊 Tipos TypeScript Consolidados

```typescript
// src/types/leads.ts - CONSOLIDADO

export type LeadTemperature = "Hot" | "Warm" | "Cold";

export type LeadStatus =
  | "New"
  | "Contacted"
  | "Qualified"
  | "Nurturing"
  | "Negotiating"
  | "Converted"
  | "Lost";

export type LeadSource =
  | "website"
  | "whatsapp"
  | "phone"
  | "email"
  | "referral"
  | "social_media";

export interface LeadDto {
  id: string;
  dealerId: string;
  userId: string;
  vehicleId: string;

  // User Info
  userFullName: string;
  userEmail: string;
  userPhone?: string;

  // Vehicle Info
  vehicleTitle: string;
  vehiclePrice: number;

  // Scoring (de LeadScoringService)
  score: number; // 0-100
  temperature: LeadTemperature;
  conversionProbability: number;
  engagementScore: number; // 0-40
  recencyScore: number; // 0-30
  intentScore: number; // 0-30

  // Status (CRMService)
  status: LeadStatus;
  source: LeadSource;
  dealerNotes?: string;
  assignedTo?: string;

  // Activity Metrics
  viewCount: number;
  contactCount: number;
  favoriteCount: number;
  shareCount: number;
  comparisonCount: number;
  hasScheduledTestDrive: boolean;
  hasRequestedFinancing: boolean;

  // Timeline
  firstInteractionAt: string;
  lastInteractionAt: string;
  lastContactedAt?: string;
  nextFollowUpAt?: string;
  convertedAt?: string;

  // Recent Actions
  recentActions: LeadAction[];
}

export interface LeadStatisticsDto {
  totalLeads: number;
  newLeadsThisWeek: number;
  hotLeads: number;
  warmLeads: number;
  coldLeads: number;
  averageScore: number;
  conversionRate: number;
  convertedLeads: number;
  avgResponseTime: string; // e.g., "2h 15m"
}

export interface LeadAction {
  id: string;
  actionType: "call" | "email" | "message" | "view" | "favorite" | "testdrive";
  description: string;
  occurredAt: string;
  scoreImpact: number;
}
```

### 🔧 leadScoringService.ts

```typescript
// src/services/leadScoringService.ts

import api from "./api";

export const leadScoringService = {
  // CRUD
  getLeads: (filters: LeadFilters, page: number, pageSize: number) =>
    api.get("/api/leads", { params: { ...filters, page, pageSize } }),

  getLeadById: (id: string) => api.get(`/api/leads/${id}`),

  getStatistics: (dealerId: string) =>
    api.get(`/api/leads/statistics/${dealerId}`),

  updateLeadStatus: (id: string, dto: UpdateLeadStatusDto) =>
    api.put(`/api/leads/${id}/status`, dto),

  // Helpers
  getTemperatureEmoji: (temp: LeadTemperature): string => {
    const emojis = { Hot: "🔥", Warm: "🟡", Cold: "❄️" };
    return emojis[temp] || "";
  },

  getScoreBarClass: (score: number): string => {
    if (score >= 80) return "bg-green-500";
    if (score >= 60) return "bg-red-500";
    if (score >= 30) return "bg-orange-500";
    return "bg-blue-500";
  },

  getRecommendedAction: (lead: LeadDto): string => {
    if (lead.temperature === "Hot" && lead.status === "New")
      return "¡Llamar ahora! Lead caliente sin contactar";
    if (!lead.lastContactedAt) return "Primer contacto pendiente";
    if (lead.hasScheduledTestDrive) return "Confirmar asistencia a test drive";
    return "Enviar seguimiento por email";
  },
};
```

### 📋 Flujo Completo CRM

```
1️⃣ LEAD CREATION
   └─> Usuario ve vehículo → Click "Contactar" → ContactService → CRMService crea Lead

2️⃣ LEAD SCORING (Automático)
   └─> LeadScoringService analiza: Engagement (40%) + Recency (30%) + Intent (30%)
   └─> Categoriza: ❄️ COLD | 🟡 WARM | 🔥 HOT | ✅ READY

3️⃣ CRM PIPELINE (Kanban)
   └─> New → Contacted → Qualified → Negotiation → Won/Lost
   └─> Drag & drop con @dnd-kit/core

4️⃣ COMUNICACIÓN
   └─> Chat bidireccional buyer ↔ dealer
   └─> Estado: Open → Responded → In Progress → Closed

5️⃣ AGENDAMIENTO
   └─> AppointmentService → Slots disponibles → Confirmación
   └─> Recordatorios automáticos (24h, 2h antes)

6️⃣ CONVERSIÓN
   └─> Dealer marca "Won" → Lead = "Converted" → Métricas actualizadas
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-crm.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer CRM", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe mostrar pipeline de leads", async ({ page }) => {
    await page.goto("/dealer/crm");

    await expect(
      page.getByRole("heading", { name: /leads|crm/i }),
    ).toBeVisible();
    await expect(page.getByTestId("leads-pipeline")).toBeVisible();
  });

  test("debe filtrar leads por estado", async ({ page }) => {
    await page.goto("/dealer/crm");

    await page.getByRole("tab", { name: /hot/i }).click();
    await expect(page.getByTestId("lead-card")).toHaveCount({ min: 0 });
  });

  test("debe ver detalle de lead", async ({ page }) => {
    await page.goto("/dealer/crm");

    await page.getByTestId("lead-card").first().click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.getByText(/historial de contacto/i)).toBeVisible();
  });

  test("debe agregar nota a lead", async ({ page }) => {
    await page.goto("/dealer/crm");
    await page.getByTestId("lead-card").first().click();

    await page.fill('textarea[name="note"]', "Llamar mañana por la tarde");
    await page.getByRole("button", { name: /agregar nota/i }).click();

    await expect(page.getByText(/nota agregada/i)).toBeVisible();
  });

  test("debe cambiar estado de lead", async ({ page }) => {
    await page.goto("/dealer/crm");
    await page.getByTestId("lead-card").first().click();

    await page.getByRole("combobox", { name: /estado/i }).click();
    await page.getByRole("option", { name: /contactado/i }).click();

    await expect(page.getByText(/estado actualizado/i)).toBeVisible();
  });

  test("debe marcar lead como convertido", async ({ page }) => {
    await page.goto("/dealer/crm");
    await page.getByTestId("lead-card").first().click();

    await page.getByRole("button", { name: /marcar como venta/i }).click();
    await expect(page.getByText(/lead convertido/i)).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/11-help-center.md`
