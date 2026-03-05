---
title: "🎧 Admin - Soporte"
priority: P2
estimated_time: "45 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🎧 Admin - Soporte

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Admin layout, SupportService
> **Roles:** ADM-SUPPORT, ADM-ADMIN, ADM-SUPER

---

## 📋 OBJETIVO

Implementar panel de soporte:

- Cola de tickets
- Detalle de ticket
- Chat interno con usuarios
- Base de conocimiento (admin)

---

## 🎨 WIREFRAME - DASHBOARD DE SOPORTE

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  SOPORTE                                     🔔 8 nuevos      │
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 👥 Users │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐│
│ │ 🛡️ Mod   │  │ 📨 45       │ │ ⏳ 12       │ │ 🔴 3        │ │ ✅ 28     ││
│ │ 🎧 Supp ◀│  │ Abiertos    │ │ En espera   │ │ Urgentes    │ │ Resueltos ││
│ │ ⚙️ System│  │             │ │             │ │             │ │ hoy       ││
│ │          │  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘│
│ │          │                                                                │
│ │          │  ┌─────────────────────────────────────────────────────────┐  │
│ │          │  │ 🔍 Buscar ticket...    [Estado ▼] [Prioridad ▼] [Agente ▼]│ │
│ │          │  └─────────────────────────────────────────────────────────┘  │
│ │          │                                                                │
│ │          │  ┌─────────────────────────────────────────────────────────┐  │
│ │          │  │ COLA DE TICKETS                                          │  │
│ │          │  │                                                          │  │
│ │          │  │  ┌──────────────────────────────────────────────────────┐│  │
│ │          │  │  │ 🔴 #1234 No puedo publicar mi vehículo               ││  │
│ │          │  │  │ Juan Pérez • Hace 5 min • 🏷️ Publicación            ││  │
│ │          │  │  │ Asignado a: María García                             ││  │
│ │          │  │  └──────────────────────────────────────────────────────┘│  │
│ │          │  │                                                          │  │
│ │          │  │  ┌──────────────────────────────────────────────────────┐│  │
│ │          │  │  │ 🟡 #1233 Problema con el pago                        ││  │
│ │          │  │  │ Ana Martínez • Hace 1 hora • 🏷️ Pagos               ││  │
│ │          │  │  │ Sin asignar                    [Tomar ticket]        ││  │
│ │          │  │  └──────────────────────────────────────────────────────┘│  │
│ │          │  │                                                          │  │
│ │          │  │  ┌──────────────────────────────────────────────────────┐│  │
│ │          │  │  │ 🟢 #1232 ¿Cómo verifico mi cuenta?                   ││  │
│ │          │  │  │ Pedro López • Hace 3 horas • 🏷️ Verificación        ││  │
│ │          │  │  │ Asignado a: Carlos Ruiz                              ││  │
│ │          │  │  └──────────────────────────────────────────────────────┘│  │
│ │          │  │                                                          │  │
│ │          │  │  [← Anterior] 1 2 3 ... 10 [Siguiente →]                │  │
│ │          │  └─────────────────────────────────────────────────────────┘  │
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - DETALLE DE TICKET

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ← Volver a tickets                          [⏸️ En espera] [✅ Resolver]    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────┐  ┌────────────────────────────────┐ │
│  │ TICKET #1234                       │  │ INFORMACIÓN                    │ │
│  │ 🔴 URGENTE                         │  │                                │ │
│  │                                    │  │ 👤 Juan Pérez                  │ │
│  │ No puedo publicar mi vehículo      │  │    juan@email.com             │ │
│  │                                    │  │    +1 809-555-1234            │ │
│  │ Cada vez que intento subir las     │  │                                │ │
│  │ fotos, el sistema me da error...   │  │ 📊 Datos del ticket           │ │
│  │                                    │  │    Estado: Abierto            │ │
│  │ Adjuntos: 📎 screenshot.png        │  │    Prioridad: Urgente         │ │
│  │                                    │  │    Categoría: Publicación     │ │
│  │ ──────────────────────────         │  │    Creado: Hace 5 min         │ │
│  │                                    │  │    Asignado: María García     │ │
│  │ 💬 CONVERSACIÓN                    │  │                                │ │
│  │                                    │  │ 🏷️ Etiquetas                  │ │
│  │ 👤 Juan: Error al subir fotos      │  │    [bug] [publicación] [fotos]│ │
│  │    5 min ago                       │  │                                │ │
│  │                                    │  │ 📝 Notas internas             │ │
│  │ 👨‍💼 María: Hola Juan, voy a        │  │    Revisar logs de upload     │ │
│  │    revisar el problema...          │  │    Posible issue con S3       │ │
│  │    2 min ago                       │  │                                │ │
│  │                                    │  └────────────────────────────────┘ │
│  │ ┌────────────────────────────────┐ │                                     │
│  │ │ Escribir respuesta...          │ │                                     │
│  │ │                                │ │                                     │
│  │ │              [📎] [Enviar →]   │ │                                     │
│  │ └────────────────────────────────┘ │                                     │
│  └────────────────────────────────────┘                                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Dashboard de Soporte

```typescript
// filepath: src/app/(admin)/admin/soporte/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { SupportStats } from "@/components/admin/support/SupportStats";
import { TicketsQueue } from "@/components/admin/support/TicketsQueue";
import { LoadingCard } from "@/components/ui/LoadingCard";

export const metadata: Metadata = {
  title: "Soporte | Admin OKLA",
};

interface Props {
  searchParams: Promise<{
    status?: string;
    priority?: string;
    assignee?: string;
  }>;
}

export default async function SupportPage({ searchParams }: Props) {
  const params = await searchParams;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Soporte</h1>
        <p className="text-gray-600">Gestiona tickets y ayuda a usuarios</p>
      </div>

      {/* Stats */}
      <Suspense fallback={<LoadingCard className="h-24" />}>
        <SupportStats />
      </Suspense>

      {/* Tickets */}
      <Suspense fallback={<LoadingCard className="h-96" />}>
        <TicketsQueue
          status={params.status}
          priority={params.priority}
          assignee={params.assignee}
        />
      </Suspense>
    </div>
  );
}
```

---

## 🔧 PASO 2: SupportStats

```typescript
// filepath: src/components/admin/support/SupportStats.tsx
import { Ticket, Clock, CheckCircle, AlertCircle } from "lucide-react";
import { supportService } from "@/lib/services/supportService";

export async function SupportStats() {
  const stats = await supportService.getAdminStats();

  const items = [
    { icon: Ticket, label: "Abiertos", value: stats.open, color: "text-blue-600 bg-blue-50" },
    { icon: Clock, label: "En espera", value: stats.pending, color: "text-amber-600 bg-amber-50" },
    { icon: AlertCircle, label: "Urgentes", value: stats.urgent, color: "text-red-600 bg-red-50" },
    { icon: CheckCircle, label: "Resueltos hoy", value: stats.resolvedToday, color: "text-green-600 bg-green-50" },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      {items.map((item) => (
        <div key={item.label} className="bg-white rounded-xl border p-4">
          <div className="flex items-center gap-3">
            <div className={`p-2 rounded-lg ${item.color}`}>
              <item.icon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{item.value}</p>
              <p className="text-sm text-gray-500">{item.label}</p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 3: TicketsQueue

```typescript
// filepath: src/components/admin/support/TicketsQueue.tsx
import Link from "next/link";
import { Clock, User } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Avatar } from "@/components/ui/Avatar";
import { TicketsFilters } from "./TicketsFilters";
import { supportService } from "@/lib/services/supportService";
import { formatRelativeDate } from "@/lib/utils";

const priorityColors = {
  low: "default",
  medium: "warning",
  high: "danger",
  urgent: "danger",
} as const;

const statusColors = {
  open: "info",
  pending: "warning",
  in_progress: "purple",
  resolved: "success",
  closed: "default",
} as const;

interface Props {
  status?: string;
  priority?: string;
  assignee?: string;
}

export async function TicketsQueue({ status, priority, assignee }: Props) {
  const tickets = await supportService.getTickets({ status, priority, assignee });

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b">
        <TicketsFilters />
      </div>

      <div className="divide-y">
        {tickets.map((ticket) => (
          <Link
            key={ticket.id}
            href={`/admin/soporte/tickets/${ticket.id}`}
            className="block p-4 hover:bg-gray-50 transition-colors"
          >
            <div className="flex items-start gap-4">
              {/* User Avatar */}
              <Avatar src={ticket.user.avatar} name={ticket.user.name} size="sm" />

              {/* Content */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <h4 className="font-medium text-gray-900 truncate">
                    {ticket.subject}
                  </h4>
                  <Badge variant={priorityColors[ticket.priority as keyof typeof priorityColors]}>
                    {ticket.priority}
                  </Badge>
                </div>

                <p className="text-sm text-gray-600 line-clamp-1 mb-2">
                  {ticket.lastMessage}
                </p>

                <div className="flex items-center gap-4 text-xs text-gray-500">
                  <span className="flex items-center gap-1">
                    <User size={12} />
                    {ticket.user.name}
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock size={12} />
                    {formatRelativeDate(ticket.updatedAt)}
                  </span>
                  <span>#{ticket.ticketNumber}</span>
                </div>
              </div>

              {/* Status & Assignee */}
              <div className="flex flex-col items-end gap-2">
                <Badge variant={statusColors[ticket.status as keyof typeof statusColors]}>
                  {ticket.status}
                </Badge>
                {ticket.assignee && (
                  <Avatar
                    src={ticket.assignee.avatar}
                    name={ticket.assignee.name}
                    size="xs"
                  />
                )}
              </div>
            </div>
          </Link>
        ))}

        {tickets.length === 0 && (
          <div className="p-12 text-center text-gray-500">
            No hay tickets que coincidan con los filtros
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: Detalle de Ticket

```typescript
// filepath: src/app/(admin)/admin/soporte/tickets/[id]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { TicketHeader } from "@/components/admin/support/TicketHeader";
import { TicketConversation } from "@/components/admin/support/TicketConversation";
import { TicketSidebar } from "@/components/admin/support/TicketSidebar";
import { supportService } from "@/lib/services/supportService";

interface Props {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const ticket = await supportService.getTicketById(id);
  return { title: ticket ? `#${ticket.ticketNumber} | Soporte` : "Ticket no encontrado" };
}

export default async function TicketDetailPage({ params }: Props) {
  const { id } = await params;
  const ticket = await supportService.getTicketById(id);

  if (!ticket) notFound();

  return (
    <div className="h-[calc(100vh-8rem)]">
      <TicketHeader ticket={ticket} />

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6 mt-4 h-[calc(100%-4rem)]">
        {/* Conversation - Takes most space */}
        <div className="lg:col-span-3 h-full">
          <TicketConversation ticket={ticket} />
        </div>

        {/* Sidebar */}
        <div className="h-full overflow-y-auto">
          <TicketSidebar ticket={ticket} />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: TicketConversation

```typescript
// filepath: src/components/admin/support/TicketConversation.tsx
"use client";

import { useState, useRef, useEffect } from "react";
import { Send, Paperclip } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { Button } from "@/components/ui/Button";
import { Textarea } from "@/components/ui/Textarea";
import { useTicketMessages, useSendTicketMessage } from "@/lib/hooks/useSupport";
import { formatDate } from "@/lib/utils";
import type { Ticket } from "@/types";

interface Props {
  ticket: Ticket;
}

export function TicketConversation({ ticket }: Props) {
  const [message, setMessage] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { data: messages } = useTicketMessages(ticket.id);
  const { send, isSending } = useSendTicketMessage(ticket.id);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim()) return;

    await send({ content: message, isInternal: false });
    setMessage("");
  };

  return (
    <div className="bg-white rounded-xl border h-full flex flex-col">
      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages?.map((msg) => (
          <div
            key={msg.id}
            className={`flex gap-3 ${msg.isStaff ? "flex-row-reverse" : ""}`}
          >
            <Avatar
              src={msg.sender.avatar}
              name={msg.sender.name}
              size="sm"
            />

            <div className={`max-w-[70%] ${msg.isStaff ? "text-right" : ""}`}>
              <div className="flex items-center gap-2 mb-1">
                <span className="text-sm font-medium text-gray-900">
                  {msg.sender.name}
                </span>
                {msg.isInternal && (
                  <span className="text-xs bg-yellow-100 text-yellow-700 px-2 py-0.5 rounded">
                    Nota interna
                  </span>
                )}
              </div>

              <div
                className={`rounded-xl p-3 ${
                  msg.isStaff
                    ? "bg-primary-600 text-white"
                    : msg.isInternal
                    ? "bg-yellow-50 border border-yellow-200"
                    : "bg-gray-100"
                }`}
              >
                <p className="text-sm whitespace-pre-wrap">{msg.content}</p>
              </div>

              <p className="text-xs text-gray-400 mt-1">
                {formatDate(msg.createdAt)}
              </p>
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSubmit} className="p-4 border-t">
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <Textarea
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="Escribe tu respuesta..."
              rows={3}
              className="resize-none"
            />
          </div>

          <div className="flex flex-col gap-2">
            <Button type="button" variant="outline" size="sm">
              <Paperclip size={16} />
            </Button>
            <Button type="submit" disabled={!message.trim() || isSending}>
              <Send size={16} />
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
```

---

## 🔧 PASO 6: TicketSidebar

```typescript
// filepath: src/components/admin/support/TicketSidebar.tsx
"use client";

import { useState } from "react";
import { User, Tag, Clock, UserCheck } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Select } from "@/components/ui/Select";
import { Button } from "@/components/ui/Button";
import { useUpdateTicket, useSupportAgents } from "@/lib/hooks/useSupport";
import { formatDate } from "@/lib/utils";
import type { Ticket } from "@/types";

interface Props {
  ticket: Ticket;
}

export function TicketSidebar({ ticket }: Props) {
  const { update, isUpdating } = useUpdateTicket(ticket.id);
  const { data: agents } = useSupportAgents();
  const [status, setStatus] = useState(ticket.status);
  const [priority, setPriority] = useState(ticket.priority);
  const [assigneeId, setAssigneeId] = useState(ticket.assignee?.id || "");

  const handleUpdate = () => {
    update({ status, priority, assigneeId: assigneeId || null });
  };

  return (
    <div className="space-y-4">
      {/* User Info */}
      <div className="bg-white rounded-xl border p-4">
        <h3 className="font-semibold text-gray-900 mb-3 flex items-center gap-2">
          <User size={16} />
          Usuario
        </h3>
        <div className="space-y-2 text-sm">
          <p className="font-medium">{ticket.user.name}</p>
          <p className="text-gray-500">{ticket.user.email}</p>
          {ticket.user.phone && (
            <p className="text-gray-500">{ticket.user.phone}</p>
          )}
        </div>
      </div>

      {/* Ticket Details */}
      <div className="bg-white rounded-xl border p-4">
        <h3 className="font-semibold text-gray-900 mb-3 flex items-center gap-2">
          <Tag size={16} />
          Detalles
        </h3>

        <div className="space-y-4">
          <div>
            <label className="text-sm text-gray-500 block mb-1">Estado</label>
            <Select value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="open">Abierto</option>
              <option value="pending">En espera</option>
              <option value="in_progress">En progreso</option>
              <option value="resolved">Resuelto</option>
              <option value="closed">Cerrado</option>
            </Select>
          </div>

          <div>
            <label className="text-sm text-gray-500 block mb-1">Prioridad</label>
            <Select value={priority} onChange={(e) => setPriority(e.target.value)}>
              <option value="low">Baja</option>
              <option value="medium">Media</option>
              <option value="high">Alta</option>
              <option value="urgent">Urgente</option>
            </Select>
          </div>

          <div>
            <label className="text-sm text-gray-500 block mb-1">Asignado a</label>
            <Select value={assigneeId} onChange={(e) => setAssigneeId(e.target.value)}>
              <option value="">Sin asignar</option>
              {agents?.map((agent) => (
                <option key={agent.id} value={agent.id}>
                  {agent.name}
                </option>
              ))}
            </Select>
          </div>

          <Button
            className="w-full"
            onClick={handleUpdate}
            disabled={isUpdating}
          >
            Guardar cambios
          </Button>
        </div>
      </div>

      {/* Timeline */}
      <div className="bg-white rounded-xl border p-4">
        <h3 className="font-semibold text-gray-900 mb-3 flex items-center gap-2">
          <Clock size={16} />
          Historial
        </h3>
        <div className="space-y-2 text-sm text-gray-500">
          <p>Creado: {formatDate(ticket.createdAt)}</p>
          <p>Actualizado: {formatDate(ticket.updatedAt)}</p>
          {ticket.resolvedAt && (
            <p>Resuelto: {formatDate(ticket.resolvedAt)}</p>
          )}
        </div>
      </div>
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /admin/soporte muestra stats
# - Lista de tickets carga
# - Detalle de ticket funciona
# - Conversación envía mensajes
```

---

## 🔧 PASO 7: Servicio de Soporte

```typescript
// filepath: src/lib/services/supportService.ts
import { apiClient } from "@/lib/apiClient";
import type { PaginatedResponse } from "@/types";

export type TicketStatus =
  | "open"
  | "pending"
  | "in_progress"
  | "waiting_user"
  | "resolved"
  | "closed";
export type TicketPriority = "low" | "medium" | "high" | "urgent";
export type TicketCategory =
  | "technical"
  | "billing"
  | "account"
  | "listing"
  | "dealer"
  | "other";

export interface Ticket {
  id: string;
  ticketNumber: string;
  subject: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  category: TicketCategory;
  user: {
    id: string;
    name: string;
    email: string;
    avatarUrl?: string;
  };
  assignee?: {
    id: string;
    name: string;
  };
  createdAt: string;
  updatedAt: string;
  firstResponseAt?: string;
  resolvedAt?: string;
  messages: TicketMessage[];
  tags: string[];
  relatedTickets?: string[];
}

export interface TicketMessage {
  id: string;
  content: string;
  sender: {
    id: string;
    name: string;
    type: "user" | "agent" | "system";
    avatarUrl?: string;
  };
  createdAt: string;
  attachments?: {
    id: string;
    name: string;
    url: string;
    type: string;
  }[];
  isInternal: boolean;
}

export interface SupportStats {
  open: number;
  pending: number;
  urgent: number;
  resolvedToday: number;
  avgFirstResponseTime: number; // minutes
  avgResolutionTime: number; // hours
  satisfactionScore: number; // 0-5
}

export interface SupportAgent {
  id: string;
  name: string;
  email: string;
  avatarUrl?: string;
  status: "online" | "away" | "offline";
  openTickets: number;
}

interface GetTicketsParams {
  page?: number;
  pageSize?: number;
  status?: TicketStatus;
  priority?: TicketPriority;
  category?: TicketCategory;
  assigneeId?: string;
  search?: string;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

interface CreateMessageDto {
  content: string;
  isInternal: boolean;
  attachmentIds?: string[];
}

interface UpdateTicketDto {
  status?: TicketStatus;
  priority?: TicketPriority;
  assigneeId?: string;
  tags?: string[];
}

export const supportService = {
  // Estadísticas admin
  async getAdminStats(): Promise<SupportStats> {
    return apiClient.get("/admin/support/stats");
  },

  // Listar tickets
  async getTickets(
    params: GetTicketsParams = {},
  ): Promise<PaginatedResponse<Ticket>> {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) searchParams.set(key, value.toString());
    });

    return apiClient.get(`/admin/support/tickets?${searchParams.toString()}`);
  },

  // Obtener ticket por ID
  async getTicketById(id: string): Promise<Ticket> {
    return apiClient.get(`/admin/support/tickets/${id}`);
  },

  // Actualizar ticket
  async updateTicket(id: string, data: UpdateTicketDto): Promise<Ticket> {
    return apiClient.put(`/admin/support/tickets/${id}`, data);
  },

  // Agregar mensaje
  async addMessage(
    ticketId: string,
    data: CreateMessageDto,
  ): Promise<TicketMessage> {
    return apiClient.post(`/admin/support/tickets/${ticketId}/messages`, data);
  },

  // Resolver ticket
  async resolveTicket(id: string, resolutionNotes: string): Promise<Ticket> {
    return apiClient.post(`/admin/support/tickets/${id}/resolve`, {
      resolutionNotes,
    });
  },

  // Reabrir ticket
  async reopenTicket(id: string, reason: string): Promise<Ticket> {
    return apiClient.post(`/admin/support/tickets/${id}/reopen`, { reason });
  },

  // Cerrar ticket
  async closeTicket(id: string): Promise<Ticket> {
    return apiClient.post(`/admin/support/tickets/${id}/close`);
  },

  // Asignar ticket
  async assignTicket(id: string, agentId: string): Promise<Ticket> {
    return apiClient.post(`/admin/support/tickets/${id}/assign`, { agentId });
  },

  // Obtener agentes disponibles
  async getAgents(): Promise<SupportAgent[]> {
    return apiClient.get("/admin/support/agents");
  },

  // Merge tickets
  async mergeTickets(
    primaryId: string,
    secondaryIds: string[],
  ): Promise<Ticket> {
    return apiClient.post(`/admin/support/tickets/${primaryId}/merge`, {
      secondaryIds,
    });
  },

  // Obtener respuestas predefinidas
  async getCannedResponses(): Promise<
    Array<{ id: string; title: string; content: string }>
  > {
    return apiClient.get("/admin/support/canned-responses");
  },

  // Exportar tickets
  async exportTickets(params: GetTicketsParams): Promise<Blob> {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) searchParams.set(key, value.toString());
    });

    return apiClient.get(
      `/admin/support/tickets/export?${searchParams.toString()}`,
      {
        responseType: "blob",
      },
    );
  },

  // Upload attachment
  async uploadAttachment(
    ticketId: string,
    file: File,
  ): Promise<{ id: string; url: string }> {
    const formData = new FormData();
    formData.append("file", file);

    return apiClient.post(
      `/admin/support/tickets/${ticketId}/attachments`,
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );
  },
};
```

---

## 🎨 Estados de UI

### Loading State

```typescript
export function TicketsQueueSkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="bg-white rounded-xl border p-4">
          <div className="flex justify-between items-start">
            <div className="space-y-2 flex-1">
              <div className="flex items-center gap-2">
                <Skeleton className="h-5 w-24" />
                <Skeleton className="h-5 w-16 rounded-full" />
              </div>
              <Skeleton className="h-5 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
            </div>
            <div className="text-right space-y-2">
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-16" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

### Empty State

```typescript
export function TicketsEmptyState({ status }: { status?: string }) {
  return (
    <div className="text-center py-12 bg-white rounded-xl border">
      <Inbox size={48} className="mx-auto text-gray-300 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        No hay tickets {status ? `${status}s` : ""}
      </h3>
      <p className="text-gray-500 max-w-sm mx-auto">
        {status === "urgent"
          ? "No hay tickets urgentes pendientes. ¡Bien!"
          : "No se encontraron tickets con los filtros seleccionados."}
      </p>
    </div>
  );
}
```

### Error State

```typescript
export function TicketsErrorState({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="text-center py-12 bg-white rounded-xl border">
      <AlertCircle size={48} className="mx-auto text-red-400 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        Error al cargar tickets
      </h3>
      <p className="text-gray-500 mb-4">
        No se pudo obtener la lista de tickets de soporte.
      </p>
      <Button variant="outline" onClick={onRetry}>
        <RefreshCw size={16} className="mr-2" />
        Reintentar
      </Button>
    </div>
  );
}
```

---

## 🧪 Testing E2E

```typescript
// filepath: e2e/admin/support.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Support", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page, "support");
  });

  test("should display support stats", async ({ page }) => {
    await page.goto("/admin/soporte");

    await expect(page.getByText("Abiertos")).toBeVisible();
    await expect(page.getByText("En espera")).toBeVisible();
    await expect(page.getByText("Urgentes")).toBeVisible();
    await expect(page.getByText("Resueltos hoy")).toBeVisible();
  });

  test("should display tickets queue", async ({ page }) => {
    await page.goto("/admin/soporte");

    await expect(page.getByTestId("tickets-queue")).toBeVisible();
  });

  test("should filter tickets by status", async ({ page }) => {
    await page.goto("/admin/soporte");

    await page.getByRole("combobox", { name: "Estado" }).click();
    await page.getByRole("option", { name: "Urgente" }).click();

    await expect(page).toHaveURL(/priority=urgent/);
  });

  test("should filter tickets by assignee", async ({ page }) => {
    await page.goto("/admin/soporte");

    await page.getByRole("combobox", { name: "Asignado a" }).click();
    await page.getByRole("option", { name: "Sin asignar" }).click();

    await expect(page).toHaveURL(/assignee=/);
  });

  test("should open ticket detail", async ({ page }) => {
    await page.goto("/admin/soporte");

    await page.getByTestId("ticket-item").first().click();

    await expect(page.getByTestId("ticket-detail")).toBeVisible();
    await expect(page.getByTestId("ticket-conversation")).toBeVisible();
  });

  test("should send reply to ticket", async ({ page }) => {
    await page.goto("/admin/soporte");
    await page.getByTestId("ticket-item").first().click();

    await page
      .getByPlaceholder("Escribe tu respuesta")
      .fill("Gracias por contactarnos. Estamos revisando tu caso.");
    await page.getByRole("button", { name: "Enviar" }).click();

    await expect(page.getByText("Mensaje enviado")).toBeVisible();
  });

  test("should send internal note", async ({ page }) => {
    await page.goto("/admin/soporte");
    await page.getByTestId("ticket-item").first().click();

    await page.getByRole("tab", { name: "Nota interna" }).click();
    await page
      .getByPlaceholder("Escribe una nota")
      .fill("Escalado a desarrollo");
    await page.getByRole("button", { name: "Agregar nota" }).click();

    await expect(page.getByText("Nota agregada")).toBeVisible();
  });

  test("should change ticket priority", async ({ page }) => {
    await page.goto("/admin/soporte");
    await page.getByTestId("ticket-item").first().click();

    await page.getByLabel("Prioridad").click();
    await page.getByRole("option", { name: "Alta" }).click();
    await page.getByRole("button", { name: "Guardar cambios" }).click();

    await expect(page.getByText("Ticket actualizado")).toBeVisible();
  });

  test("should assign ticket to agent", async ({ page }) => {
    await page.goto("/admin/soporte");
    await page.getByTestId("ticket-item").first().click();

    await page.getByLabel("Asignado a").click();
    await page.getByRole("option").first().click();
    await page.getByRole("button", { name: "Guardar cambios" }).click();

    await expect(page.getByText("Ticket asignado")).toBeVisible();
  });

  test("should resolve ticket", async ({ page }) => {
    await page.goto("/admin/soporte");
    await page.getByTestId("ticket-item").first().click();

    await page.getByRole("button", { name: "Resolver" }).click();
    await page
      .getByLabel("Notas de resolución")
      .fill("Problema resuelto. Usuario confirmó.");
    await page.getByRole("button", { name: "Confirmar resolución" }).click();

    await expect(page.getByText("Ticket resuelto")).toBeVisible();
  });

  test("should use canned response", async ({ page }) => {
    await page.goto("/admin/soporte");
    await page.getByTestId("ticket-item").first().click();

    await page.getByRole("button", { name: "Respuestas rápidas" }).click();
    await page.getByRole("option", { name: /Saludo inicial/ }).click();

    const textarea = page.getByPlaceholder("Escribe tu respuesta");
    await expect(textarea).not.toBeEmpty();
  });
});
```

---

## 📊 Métricas y Analytics

```typescript
// filepath: src/lib/analytics/supportEvents.ts
import { analytics } from "@/lib/analytics";

export const supportEvents = {
  // Visualización
  viewQueue: (filters: Record<string, string>) => {
    analytics.track("admin_support_queue_viewed", filters);
  },

  viewTicket: (ticketId: string, priority: string) => {
    analytics.track("admin_support_ticket_viewed", { ticketId, priority });
  },

  // Acciones
  sendReply: (ticketId: string, isFirstResponse: boolean) => {
    analytics.track("admin_support_reply_sent", { ticketId, isFirstResponse });
  },

  addInternalNote: (ticketId: string) => {
    analytics.track("admin_support_note_added", { ticketId });
  },

  changePriority: (ticketId: string, from: string, to: string) => {
    analytics.track("admin_support_priority_changed", { ticketId, from, to });
  },

  assignTicket: (ticketId: string, agentId: string) => {
    analytics.track("admin_support_ticket_assigned", { ticketId, agentId });
  },

  resolveTicket: (ticketId: string, timeToResolve: number) => {
    analytics.track("admin_support_ticket_resolved", {
      ticketId,
      timeToResolve,
    });
  },

  closeTicket: (ticketId: string) => {
    analytics.track("admin_support_ticket_closed", { ticketId });
  },

  reopenTicket: (ticketId: string) => {
    analytics.track("admin_support_ticket_reopened", { ticketId });
  },

  // Eficiencia
  useCannedResponse: (responseId: string) => {
    analytics.track("admin_support_canned_response_used", { responseId });
  },

  mergeTickets: (primaryId: string, count: number) => {
    analytics.track("admin_support_tickets_merged", { primaryId, count });
  },
};
```

---

## 🔐 Permisos y Roles

| Acción                      | ADM-SUPER | ADM-ADMIN | ADM-MOD | ADM-SUPPORT |
| --------------------------- | --------- | --------- | ------- | ----------- |
| Ver lista de tickets        | ✅        | ✅        | ❌      | ✅          |
| Ver detalle de ticket       | ✅        | ✅        | ❌      | ✅          |
| Responder ticket            | ✅        | ✅        | ❌      | ✅          |
| Agregar nota interna        | ✅        | ✅        | ❌      | ✅          |
| Cambiar prioridad           | ✅        | ✅        | ❌      | ✅          |
| Asignar ticket              | ✅        | ✅        | ❌      | ✅          |
| Resolver ticket             | ✅        | ✅        | ❌      | ✅          |
| Cerrar ticket               | ✅        | ✅        | ❌      | ✅          |
| Reabrir ticket              | ✅        | ✅        | ❌      | ❌          |
| Merge tickets               | ✅        | ✅        | ❌      | ❌          |
| Ver métricas de soporte     | ✅        | ✅        | ❌      | ✅          |
| Configurar canned responses | ✅        | ✅        | ❌      | ❌          |
| Exportar tickets            | ✅        | ✅        | ❌      | ❌          |

---

## ✅ Checklist de Implementación

### Backend (SupportService)

- [ ] Endpoint `GET /api/admin/support/stats` estadísticas
- [ ] Endpoint `GET /api/admin/support/tickets` listar tickets
- [ ] Endpoint `GET /api/admin/support/tickets/{id}` detalle
- [ ] Endpoint `PUT /api/admin/support/tickets/{id}` actualizar
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/messages` agregar mensaje
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/resolve` resolver
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/reopen` reabrir
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/close` cerrar
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/assign` asignar
- [ ] Endpoint `GET /api/admin/support/agents` listar agentes
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/merge` merge
- [ ] Endpoint `GET /api/admin/support/canned-responses` respuestas
- [ ] Endpoint `GET /api/admin/support/tickets/export` exportar
- [ ] Endpoint `POST /api/admin/support/tickets/{id}/attachments` adjuntos
- [ ] WebSocket para mensajes en tiempo real

### Frontend

- [ ] Página `/admin/soporte` con stats y lista
- [ ] Componente `SupportStats` cards de estadísticas
- [ ] Componente `TicketsQueue` lista de tickets
- [ ] Componente `TicketDetail` panel de detalle
- [ ] Componente `TicketConversation` chat de mensajes
- [ ] Componente `TicketSidebar` acciones y metadata
- [ ] Componente `CannedResponsePicker` selector de respuestas
- [ ] Estados: Loading, Empty, Error
- [ ] Servicio `supportService`
- [ ] Real-time updates vía WebSocket
- [ ] Tests E2E completos
- [ ] Analytics tracking

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-support.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Support", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar cola de tickets", async ({ page }) => {
    await page.goto("/admin/support");

    await expect(page.getByTestId("tickets-queue")).toBeVisible();
  });

  test("debe ver detalle de ticket", async ({ page }) => {
    await page.goto("/admin/support");

    await page.getByTestId("ticket-row").first().click();
    await expect(page.getByTestId("ticket-detail")).toBeVisible();
  });

  test("debe responder a ticket", async ({ page }) => {
    await page.goto("/admin/support");
    await page.getByTestId("ticket-row").first().click();

    await page.fill(
      'textarea[name="response"]',
      "Gracias por contactarnos. Hemos revisado su caso...",
    );
    await page.getByRole("button", { name: /enviar respuesta/i }).click();

    await expect(page.getByText(/respuesta enviada/i)).toBeVisible();
  });

  test("debe cerrar ticket", async ({ page }) => {
    await page.goto("/admin/support");
    await page.getByTestId("ticket-row").first().click();

    await page.getByRole("button", { name: /cerrar ticket/i }).click();
    await expect(page.getByText(/ticket cerrado/i)).toBeVisible();
  });

  test("debe filtrar por prioridad", async ({ page }) => {
    await page.goto("/admin/support");

    await page.getByRole("combobox", { name: /prioridad/i }).click();
    await page.getByRole("option", { name: /alta/i }).click();

    await expect(page).toHaveURL(/priority=high/);
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: [06-admin-analytics.md](./01-admin-dashboard.md)
