---
title: "51 - Inquiries & Messaging System"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["NotificationService", "ContactService"]
status: complete
last_updated: "2026-01-30"
---

# 💬 51 - Inquiries & Messaging System

**Objetivo:** Sistema completo de consultas y mensajería entre compradores y vendedores.

**Prioridad:** P0 (CRÍTICO - Core comunicación)  
**Complejidad:** 🟡 Media  
**Dependencias:** ContactService, NotificationService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [Páginas](#-páginas)
3. [Hooks](#-hooks)
4. [Servicios](#-servicios)
5. [Tipos TypeScript](#-tipos-typescript)
6. [Flujo de Usuario](#-flujo-de-usuario)

---

## 🏗️ ARQUITECTURA

```
pages/
├── MyInquiriesPage.tsx       # Consultas enviadas (comprador)
├── ReceivedInquiriesPage.tsx # Consultas recibidas (vendedor)
└── MessagesPage.tsx          # Bandeja de mensajes completa

hooks/
├── useMessagesInbox.ts       # Hook para inbox
├── useChatWindow.ts          # Hook para conversación
└── useContactRequests.ts     # Hook para consultas
```

### Arquitectura de Datos

```
ContactRequest (Consulta inicial)
├── id, vehicleId, vehicleName
├── senderId, senderName, senderEmail
├── recipientId, recipientName
├── message (inicial)
├── status: Open | Responded | Closed
└── createdAt, respondedAt

Message (Mensajes de conversación)
├── id, conversationId
├── senderId, senderName
├── content, type (text/image/file)
├── isRead, readAt
└── createdAt
```

---

## 📄 PÁGINAS

### 1. MyInquiriesPage.tsx

**Ruta:** `/my-inquiries` (Comprador: consultas enviadas)

```typescript
// src/pages/MyInquiriesPage.tsx
"use client";

import { useState } from "react";
import { Link } from "react-router-dom";
import {
  MessageSquare,
  Car,
  Clock,
  CheckCircle,
  XCircle,
  Search,
  Filter,
  ChevronRight,
  Loader2,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Card, CardContent } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/Select";
import { useQuery } from "@tanstack/react-query";
import { contactService, InquiryStatus } from "@/services/contactService";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

interface Inquiry {
  id: string;
  vehicleId: string;
  vehicleName: string;
  vehicleImage: string;
  recipientName: string;
  message: string;
  status: InquiryStatus;
  createdAt: string;
  respondedAt?: string;
  lastReplyMessage?: string;
}

export default function MyInquiriesPage() {
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<InquiryStatus | "all">("all");

  const { data, isLoading, error } = useQuery({
    queryKey: ["my-inquiries", { status: statusFilter, search: searchTerm }],
    queryFn: () =>
      contactService.getMyInquiries({
        status: statusFilter !== "all" ? statusFilter : undefined,
        searchTerm: searchTerm || undefined,
      }),
  });

  const inquiries = data?.data || [];

  const getStatusBadge = (status: InquiryStatus) => {
    const styles: Record<InquiryStatus, { variant: string; icon: any; label: string }> = {
      Open: {
        variant: "warning",
        icon: Clock,
        label: "Esperando respuesta",
      },
      Responded: {
        variant: "success",
        icon: CheckCircle,
        label: "Respondida",
      },
      Closed: {
        variant: "secondary",
        icon: XCircle,
        label: "Cerrada",
      },
    };

    const style = styles[status];
    const Icon = style.icon;

    return (
      <Badge variant={style.variant as any} className="flex items-center gap-1">
        <Icon className="w-3 h-3" />
        {style.label}
      </Badge>
    );
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-4xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Mis Consultas
            </h1>
            <p className="text-gray-600">
              Consultas que has enviado a vendedores sobre vehículos
            </p>
          </div>

          {/* Filters */}
          <div className="flex flex-col sm:flex-row gap-4 mb-6">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <Input
                type="text"
                placeholder="Buscar por vehículo o vendedor..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            <Select
              value={statusFilter}
              onValueChange={(value) => setStatusFilter(value as InquiryStatus | "all")}
            >
              <SelectTrigger className="w-full sm:w-48">
                <Filter className="w-4 h-4 mr-2" />
                <SelectValue placeholder="Filtrar por estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todas</SelectItem>
                <SelectItem value="Open">Pendientes</SelectItem>
                <SelectItem value="Responded">Respondidas</SelectItem>
                <SelectItem value="Closed">Cerradas</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Loading */}
          {isLoading && (
            <div className="flex justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
            </div>
          )}

          {/* Error */}
          {error && (
            <div className="text-center py-12">
              <p className="text-red-600">Error al cargar consultas</p>
            </div>
          )}

          {/* Empty State */}
          {!isLoading && inquiries.length === 0 && (
            <Card className="py-12 text-center">
              <MessageSquare className="w-16 h-16 text-gray-300 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                No tienes consultas
              </h3>
              <p className="text-gray-600 mb-6">
                Cuando contactes a un vendedor, tus consultas aparecerán aquí.
              </p>
              <Button asChild>
                <Link to="/vehicles">Explorar Vehículos</Link>
              </Button>
            </Card>
          )}

          {/* Inquiries List */}
          <div className="space-y-4">
            {inquiries.map((inquiry) => (
              <Card
                key={inquiry.id}
                className="hover:shadow-md transition-shadow"
              >
                <CardContent className="p-4">
                  <div className="flex gap-4">
                    {/* Vehicle Image */}
                    <Link to={`/vehicles/${inquiry.vehicleId}`}>
                      <img
                        src={inquiry.vehicleImage || "/img/placeholder-car.jpg"}
                        alt={inquiry.vehicleName}
                        className="w-24 h-24 object-cover rounded-lg flex-shrink-0"
                      />
                    </Link>

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2 mb-2">
                        <div>
                          <Link
                            to={`/vehicles/${inquiry.vehicleId}`}
                            className="font-medium text-gray-900 hover:text-blue-600"
                          >
                            {inquiry.vehicleName}
                          </Link>
                          <p className="text-sm text-gray-500">
                            Vendedor: {inquiry.recipientName}
                          </p>
                        </div>
                        {getStatusBadge(inquiry.status)}
                      </div>

                      {/* Message Preview */}
                      <p className="text-sm text-gray-600 line-clamp-2 mb-2">
                        {inquiry.lastReplyMessage || inquiry.message}
                      </p>

                      {/* Footer */}
                      <div className="flex items-center justify-between">
                        <span className="text-xs text-gray-400">
                          {formatDistanceToNow(new Date(inquiry.createdAt), {
                            addSuffix: true,
                            locale: es,
                          })}
                        </span>
                        <Link
                          to={`/messages/${inquiry.id}`}
                          className="text-sm text-blue-600 hover:text-blue-700 flex items-center"
                        >
                          Ver conversación
                          <ChevronRight className="w-4 h-4" />
                        </Link>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Stats */}
          {!isLoading && inquiries.length > 0 && (
            <div className="mt-8 grid grid-cols-3 gap-4 text-center">
              <div className="bg-white rounded-lg p-4 shadow-sm">
                <p className="text-2xl font-bold text-gray-900">
                  {inquiries.filter((i) => i.status === "Open").length}
                </p>
                <p className="text-sm text-gray-500">Pendientes</p>
              </div>
              <div className="bg-white rounded-lg p-4 shadow-sm">
                <p className="text-2xl font-bold text-green-600">
                  {inquiries.filter((i) => i.status === "Responded").length}
                </p>
                <p className="text-sm text-gray-500">Respondidas</p>
              </div>
              <div className="bg-white rounded-lg p-4 shadow-sm">
                <p className="text-2xl font-bold text-gray-400">
                  {inquiries.filter((i) => i.status === "Closed").length}
                </p>
                <p className="text-sm text-gray-500">Cerradas</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
```

---

### 2. ReceivedInquiriesPage.tsx

**Ruta:** `/received-inquiries` (Vendedor: consultas recibidas)

```typescript
// src/pages/ReceivedInquiriesPage.tsx
"use client";

import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Inbox,
  Clock,
  CheckCircle,
  XCircle,
  Search,
  Filter,
  Reply,
  User,
  Phone,
  Mail,
  Loader2,
  Send,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Badge } from "@/components/ui/Badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/Dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/Select";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { contactService, InquiryStatus } from "@/services/contactService";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import { toast } from "sonner";

interface ReceivedInquiry {
  id: string;
  vehicleId: string;
  vehicleName: string;
  vehicleImage: string;
  senderId: string;
  senderName: string;
  senderEmail: string;
  senderPhone?: string;
  message: string;
  status: InquiryStatus;
  createdAt: string;
  respondedAt?: string;
}

export default function ReceivedInquiriesPage() {
  const queryClient = useQueryClient();

  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<InquiryStatus | "all">("all");
  const [selectedInquiry, setSelectedInquiry] = useState<ReceivedInquiry | null>(null);
  const [replyMessage, setReplyMessage] = useState("");

  const { data, isLoading, error } = useQuery({
    queryKey: ["received-inquiries", { status: statusFilter, search: searchTerm }],
    queryFn: () =>
      contactService.getReceivedInquiries({
        status: statusFilter !== "all" ? statusFilter : undefined,
        searchTerm: searchTerm || undefined,
      }),
  });

  const replyMutation = useMutation({
    mutationFn: ({ inquiryId, message }: { inquiryId: string; message: string }) =>
      contactService.replyToInquiry(inquiryId, message),
    onSuccess: () => {
      toast.success("Respuesta enviada");
      queryClient.invalidateQueries({ queryKey: ["received-inquiries"] });
      setSelectedInquiry(null);
      setReplyMessage("");
    },
    onError: () => {
      toast.error("Error al enviar respuesta");
    },
  });

  const closeMutation = useMutation({
    mutationFn: (inquiryId: string) => contactService.closeInquiry(inquiryId),
    onSuccess: () => {
      toast.success("Consulta cerrada");
      queryClient.invalidateQueries({ queryKey: ["received-inquiries"] });
    },
  });

  const inquiries = data?.data || [];

  const getStatusBadge = (status: InquiryStatus) => {
    const styles: Record<InquiryStatus, { variant: string; icon: any; label: string }> = {
      Open: {
        variant: "warning",
        icon: Clock,
        label: "Nueva",
      },
      Responded: {
        variant: "success",
        icon: CheckCircle,
        label: "Respondida",
      },
      Closed: {
        variant: "secondary",
        icon: XCircle,
        label: "Cerrada",
      },
    };

    const style = styles[status];
    const Icon = style.icon;

    return (
      <Badge variant={style.variant as any} className="flex items-center gap-1">
        <Icon className="w-3 h-3" />
        {style.label}
      </Badge>
    );
  };

  const handleReply = () => {
    if (!selectedInquiry || !replyMessage.trim()) return;
    replyMutation.mutate({
      inquiryId: selectedInquiry.id,
      message: replyMessage,
    });
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-4xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Consultas Recibidas
            </h1>
            <p className="text-gray-600">
              Consultas de compradores interesados en tus vehículos
            </p>
          </div>

          {/* Stats Bar */}
          <div className="grid grid-cols-3 gap-4 mb-6">
            <Card className="p-4 text-center bg-yellow-50 border-yellow-200">
              <p className="text-2xl font-bold text-yellow-600">
                {inquiries.filter((i) => i.status === "Open").length}
              </p>
              <p className="text-sm text-yellow-700">Sin responder</p>
            </Card>
            <Card className="p-4 text-center bg-green-50 border-green-200">
              <p className="text-2xl font-bold text-green-600">
                {inquiries.filter((i) => i.status === "Responded").length}
              </p>
              <p className="text-sm text-green-700">Respondidas</p>
            </Card>
            <Card className="p-4 text-center">
              <p className="text-2xl font-bold text-gray-600">{inquiries.length}</p>
              <p className="text-sm text-gray-500">Total</p>
            </Card>
          </div>

          {/* Filters */}
          <div className="flex flex-col sm:flex-row gap-4 mb-6">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <Input
                type="text"
                placeholder="Buscar por comprador o vehículo..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            <Select
              value={statusFilter}
              onValueChange={(value) => setStatusFilter(value as InquiryStatus | "all")}
            >
              <SelectTrigger className="w-full sm:w-48">
                <Filter className="w-4 h-4 mr-2" />
                <SelectValue placeholder="Filtrar" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todas</SelectItem>
                <SelectItem value="Open">Sin responder</SelectItem>
                <SelectItem value="Responded">Respondidas</SelectItem>
                <SelectItem value="Closed">Cerradas</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Loading */}
          {isLoading && (
            <div className="flex justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
            </div>
          )}

          {/* Empty State */}
          {!isLoading && inquiries.length === 0 && (
            <Card className="py-12 text-center">
              <Inbox className="w-16 h-16 text-gray-300 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                No tienes consultas
              </h3>
              <p className="text-gray-600">
                Cuando alguien te contacte, las consultas aparecerán aquí.
              </p>
            </Card>
          )}

          {/* Inquiries List */}
          <div className="space-y-4">
            {inquiries.map((inquiry) => (
              <Card
                key={inquiry.id}
                className={`hover:shadow-md transition-shadow ${
                  inquiry.status === "Open" ? "border-l-4 border-l-yellow-400" : ""
                }`}
              >
                <CardContent className="p-4">
                  <div className="flex gap-4">
                    {/* Vehicle Image */}
                    <Link to={`/vehicles/${inquiry.vehicleId}`}>
                      <img
                        src={inquiry.vehicleImage || "/img/placeholder-car.jpg"}
                        alt={inquiry.vehicleName}
                        className="w-20 h-20 object-cover rounded-lg flex-shrink-0"
                      />
                    </Link>

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2 mb-2">
                        <div>
                          <p className="font-medium text-gray-900">
                            {inquiry.senderName}
                          </p>
                          <Link
                            to={`/vehicles/${inquiry.vehicleId}`}
                            className="text-sm text-blue-600 hover:underline"
                          >
                            {inquiry.vehicleName}
                          </Link>
                        </div>
                        {getStatusBadge(inquiry.status)}
                      </div>

                      {/* Message */}
                      <p className="text-sm text-gray-600 line-clamp-2 mb-3">
                        {inquiry.message}
                      </p>

                      {/* Footer */}
                      <div className="flex items-center justify-between">
                        <span className="text-xs text-gray-400">
                          {formatDistanceToNow(new Date(inquiry.createdAt), {
                            addSuffix: true,
                            locale: es,
                          })}
                        </span>

                        <div className="flex gap-2">
                          {inquiry.status === "Open" && (
                            <Button
                              size="sm"
                              onClick={() => setSelectedInquiry(inquiry)}
                            >
                              <Reply className="w-4 h-4 mr-1" />
                              Responder
                            </Button>
                          )}
                          {inquiry.status !== "Closed" && (
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => closeMutation.mutate(inquiry.id)}
                            >
                              <XCircle className="w-4 h-4 mr-1" />
                              Cerrar
                            </Button>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </div>

      {/* Reply Dialog */}
      <Dialog
        open={!!selectedInquiry}
        onOpenChange={() => setSelectedInquiry(null)}
      >
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Responder Consulta</DialogTitle>
          </DialogHeader>

          {selectedInquiry && (
            <div className="space-y-4">
              {/* Sender Info */}
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="flex items-center gap-3 mb-3">
                  <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
                    <User className="w-5 h-5 text-blue-600" />
                  </div>
                  <div>
                    <p className="font-medium">{selectedInquiry.senderName}</p>
                    <p className="text-sm text-gray-500">
                      Interesado en: {selectedInquiry.vehicleName}
                    </p>
                  </div>
                </div>
                <div className="flex gap-4 text-sm">
                  <a
                    href={`mailto:${selectedInquiry.senderEmail}`}
                    className="flex items-center gap-1 text-blue-600 hover:underline"
                  >
                    <Mail className="w-4 h-4" />
                    {selectedInquiry.senderEmail}
                  </a>
                  {selectedInquiry.senderPhone && (
                    <a
                      href={`tel:${selectedInquiry.senderPhone}`}
                      className="flex items-center gap-1 text-blue-600 hover:underline"
                    >
                      <Phone className="w-4 h-4" />
                      {selectedInquiry.senderPhone}
                    </a>
                  )}
                </div>
              </div>

              {/* Original Message */}
              <div>
                <p className="text-sm font-medium text-gray-700 mb-1">
                  Mensaje original:
                </p>
                <p className="text-gray-600 bg-gray-50 p-3 rounded-lg">
                  {selectedInquiry.message}
                </p>
              </div>

              {/* Reply Textarea */}
              <div>
                <p className="text-sm font-medium text-gray-700 mb-1">
                  Tu respuesta:
                </p>
                <Textarea
                  value={replyMessage}
                  onChange={(e) => setReplyMessage(e.target.value)}
                  placeholder="Escribe tu respuesta..."
                  rows={4}
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setSelectedInquiry(null)}>
              Cancelar
            </Button>
            <Button
              onClick={handleReply}
              disabled={!replyMessage.trim() || replyMutation.isPending}
            >
              {replyMutation.isPending ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin mr-2" />
                  Enviando...
                </>
              ) : (
                <>
                  <Send className="w-4 h-4 mr-2" />
                  Enviar Respuesta
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </MainLayout>
  );
}
```

---

### 3. MessagesPage.tsx

**Ruta:** `/messages` (Bandeja de mensajes completa)

```typescript
// src/pages/MessagesPage.tsx
"use client";

import { useState, useEffect, useRef } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import {
  Search,
  Send,
  Paperclip,
  Phone,
  MoreVertical,
  ArrowLeft,
  Check,
  CheckCheck,
  Loader2,
  MessageSquare,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { useMessagesInbox, useChatWindow } from "@/hooks/useMessages";
import { formatDistanceToNow, format, isToday, isYesterday } from "date-fns";
import { es } from "date-fns/locale";
import { cn } from "@/lib/utils";
import { useAuth } from "@/hooks/useAuth";

export default function MessagesPage() {
  const { conversationId } = useParams();
  const { user } = useAuth();

  const [searchTerm, setSearchTerm] = useState("");
  const [newMessage, setNewMessage] = useState("");
  const [selectedConversationId, setSelectedConversationId] = useState<string | null>(
    conversationId || null
  );

  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Hooks
  const { conversations, isLoading: inboxLoading } = useMessagesInbox({
    searchTerm,
  });

  const {
    messages,
    isLoading: chatLoading,
    sendMessage,
    isSending,
  } = useChatWindow(selectedConversationId || "");

  // Auto-scroll
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Formatear fecha de mensaje
  const formatMessageTime = (date: string) => {
    const d = new Date(date);
    if (isToday(d)) {
      return format(d, "h:mm a");
    }
    if (isYesterday(d)) {
      return `Ayer ${format(d, "h:mm a")}`;
    }
    return format(d, "d MMM, h:mm a", { locale: es });
  };

  const handleSend = () => {
    if (!newMessage.trim() || !selectedConversationId) return;
    sendMessage(newMessage);
    setNewMessage("");
  };

  const selectedConversation = conversations.find(
    (c) => c.id === selectedConversationId
  );

  return (
    <MainLayout hideFooter>
      <div className="h-[calc(100vh-64px)] flex">
        {/* Sidebar - Conversations List */}
        <div
          className={cn(
            "w-full md:w-80 border-r bg-white flex flex-col",
            selectedConversationId && "hidden md:flex"
          )}
        >
          {/* Search */}
          <div className="p-4 border-b">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <Input
                type="text"
                placeholder="Buscar conversaciones..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {/* Conversations List */}
          <div className="flex-1 overflow-y-auto">
            {inboxLoading ? (
              <div className="flex justify-center py-8">
                <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
              </div>
            ) : conversations.length === 0 ? (
              <div className="p-8 text-center">
                <MessageSquare className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                <p className="text-gray-500">No tienes conversaciones</p>
              </div>
            ) : (
              conversations.map((conversation) => (
                <button
                  key={conversation.id}
                  onClick={() => setSelectedConversationId(conversation.id)}
                  className={cn(
                    "w-full p-4 flex gap-3 hover:bg-gray-50 transition-colors text-left",
                    selectedConversationId === conversation.id && "bg-blue-50"
                  )}
                >
                  <Avatar className="w-12 h-12 flex-shrink-0">
                    <AvatarImage src={conversation.participantAvatar} />
                    <AvatarFallback>
                      {conversation.participantName[0]}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-medium text-gray-900 truncate">
                        {conversation.participantName}
                      </span>
                      <span className="text-xs text-gray-400">
                        {formatDistanceToNow(
                          new Date(conversation.lastMessageAt),
                          { addSuffix: false, locale: es }
                        )}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <p className="text-sm text-gray-500 truncate">
                        {conversation.lastMessage}
                      </p>
                      {conversation.unreadCount > 0 && (
                        <Badge variant="destructive" className="ml-2">
                          {conversation.unreadCount}
                        </Badge>
                      )}
                    </div>
                    <p className="text-xs text-gray-400 truncate mt-1">
                      {conversation.vehicleName}
                    </p>
                  </div>
                </button>
              ))
            )}
          </div>
        </div>

        {/* Chat Area */}
        <div
          className={cn(
            "flex-1 flex flex-col bg-gray-50",
            !selectedConversationId && "hidden md:flex"
          )}
        >
          {!selectedConversationId ? (
            // Empty State
            <div className="flex-1 flex items-center justify-center">
              <div className="text-center">
                <MessageSquare className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">
                  Selecciona una conversación
                </h3>
                <p className="text-gray-500">
                  Elige una conversación de la lista para ver los mensajes
                </p>
              </div>
            </div>
          ) : (
            <>
              {/* Chat Header */}
              <div className="bg-white border-b p-4 flex items-center gap-3">
                <button
                  onClick={() => setSelectedConversationId(null)}
                  className="md:hidden p-2 hover:bg-gray-100 rounded-lg"
                >
                  <ArrowLeft className="w-5 h-5" />
                </button>

                {selectedConversation && (
                  <>
                    <Avatar className="w-10 h-10">
                      <AvatarImage src={selectedConversation.participantAvatar} />
                      <AvatarFallback>
                        {selectedConversation.participantName[0]}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex-1">
                      <p className="font-medium">
                        {selectedConversation.participantName}
                      </p>
                      <p className="text-xs text-gray-500">
                        {selectedConversation.vehicleName}
                      </p>
                    </div>
                    <Button variant="ghost" size="icon">
                      <Phone className="w-5 h-5" />
                    </Button>
                    <Button variant="ghost" size="icon">
                      <MoreVertical className="w-5 h-5" />
                    </Button>
                  </>
                )}
              </div>

              {/* Messages */}
              <div className="flex-1 overflow-y-auto p-4 space-y-4">
                {chatLoading ? (
                  <div className="flex justify-center py-8">
                    <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
                  </div>
                ) : (
                  messages.map((message) => {
                    const isOwn = message.senderId === user?.id;
                    return (
                      <div
                        key={message.id}
                        className={cn(
                          "flex",
                          isOwn ? "justify-end" : "justify-start"
                        )}
                      >
                        <div
                          className={cn(
                            "max-w-[70%] rounded-lg px-4 py-2",
                            isOwn
                              ? "bg-blue-600 text-white"
                              : "bg-white text-gray-900 shadow-sm"
                          )}
                        >
                          <p>{message.content}</p>
                          <div
                            className={cn(
                              "flex items-center justify-end gap-1 mt-1 text-xs",
                              isOwn ? "text-blue-200" : "text-gray-400"
                            )}
                          >
                            <span>{formatMessageTime(message.createdAt)}</span>
                            {isOwn && (
                              message.isRead ? (
                                <CheckCheck className="w-4 h-4 text-blue-200" />
                              ) : (
                                <Check className="w-4 h-4" />
                              )
                            )}
                          </div>
                        </div>
                      </div>
                    );
                  })
                )}
                <div ref={messagesEndRef} />
              </div>

              {/* Message Input */}
              <div className="bg-white border-t p-4">
                <div className="flex gap-2">
                  <Button variant="ghost" size="icon">
                    <Paperclip className="w-5 h-5" />
                  </Button>
                  <Input
                    type="text"
                    placeholder="Escribe un mensaje..."
                    value={newMessage}
                    onChange={(e) => setNewMessage(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" && !e.shiftKey) {
                        e.preventDefault();
                        handleSend();
                      }
                    }}
                    className="flex-1"
                  />
                  <Button
                    onClick={handleSend}
                    disabled={!newMessage.trim() || isSending}
                  >
                    {isSending ? (
                      <Loader2 className="w-5 h-5 animate-spin" />
                    ) : (
                      <Send className="w-5 h-5" />
                    )}
                  </Button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 🪝 HOOKS

### useMessages.ts

```typescript
// src/hooks/useMessages.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { messagingService } from "@/services/messagingService";

export interface Conversation {
  id: string;
  participantId: string;
  participantName: string;
  participantAvatar?: string;
  vehicleId: string;
  vehicleName: string;
  lastMessage: string;
  lastMessageAt: string;
  unreadCount: number;
}

export interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  senderName: string;
  content: string;
  type: "text" | "image" | "file";
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export function useMessagesInbox(options?: { searchTerm?: string }) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["inbox", options?.searchTerm],
    queryFn: () => messagingService.getInbox(options?.searchTerm),
    refetchInterval: 30000, // Refetch cada 30 segundos
  });

  return {
    conversations: (data || []) as Conversation[],
    isLoading,
    error,
  };
}

export function useChatWindow(conversationId: string) {
  const queryClient = useQueryClient();

  const { data: messages = [], isLoading } = useQuery({
    queryKey: ["messages", conversationId],
    queryFn: () => messagingService.getMessages(conversationId),
    enabled: !!conversationId,
    refetchInterval: 5000, // Refetch cada 5 segundos
  });

  const sendMutation = useMutation({
    mutationFn: (content: string) =>
      messagingService.sendMessage(conversationId, content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["messages", conversationId] });
      queryClient.invalidateQueries({ queryKey: ["inbox"] });
    },
  });

  const markAsReadMutation = useMutation({
    mutationFn: () => messagingService.markAsRead(conversationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["inbox"] });
    },
  });

  return {
    messages: messages as Message[],
    isLoading,
    sendMessage: sendMutation.mutate,
    isSending: sendMutation.isPending,
    markAsRead: markAsReadMutation.mutate,
  };
}
```

---

## 🔧 SERVICIOS

### contactService.ts

```typescript
// src/services/contactService.ts
import { apiClient } from "./api-client";

export type InquiryStatus = "Open" | "Responded" | "Closed";

export interface Inquiry {
  id: string;
  vehicleId: string;
  vehicleName: string;
  vehicleImage?: string;
  senderId: string;
  senderName: string;
  senderEmail: string;
  senderPhone?: string;
  recipientId: string;
  recipientName: string;
  message: string;
  status: InquiryStatus;
  createdAt: string;
  respondedAt?: string;
  lastReplyMessage?: string;
}

class ContactService {
  async getMyInquiries(params?: {
    status?: InquiryStatus;
    searchTerm?: string;
  }) {
    const response = await apiClient.get<{ data: Inquiry[]; total: number }>(
      "/api/contactrequests/my-inquiries",
      { params },
    );
    return response.data;
  }

  async getReceivedInquiries(params?: {
    status?: InquiryStatus;
    searchTerm?: string;
  }) {
    const response = await apiClient.get<{ data: Inquiry[]; total: number }>(
      "/api/contactrequests/received",
      { params },
    );
    return response.data;
  }

  async sendInquiry(data: {
    vehicleId: string;
    message: string;
    name?: string;
    email?: string;
    phone?: string;
  }) {
    const response = await apiClient.post("/api/contactrequests", data);
    return response.data;
  }

  async replyToInquiry(inquiryId: string, message: string) {
    const response = await apiClient.post(
      `/api/contactrequests/${inquiryId}/reply`,
      { message },
    );
    return response.data;
  }

  async closeInquiry(inquiryId: string) {
    const response = await apiClient.patch(
      `/api/contactrequests/${inquiryId}/close`,
    );
    return response.data;
  }
}

export const contactService = new ContactService();
```

### messagingService.ts

```typescript
// src/services/messagingService.ts
import { apiClient } from "./api-client";
import type { Conversation, Message } from "@/hooks/useMessages";

class MessagingService {
  async getInbox(searchTerm?: string) {
    const response = await apiClient.get<Conversation[]>(
      "/api/messages/inbox",
      {
        params: { searchTerm },
      },
    );
    return response.data;
  }

  async getMessages(conversationId: string) {
    const response = await apiClient.get<Message[]>(
      `/api/messages/conversations/${conversationId}`,
    );
    return response.data;
  }

  async sendMessage(conversationId: string, content: string) {
    const response = await apiClient.post<Message>(
      `/api/messages/conversations/${conversationId}`,
      { content, type: "text" },
    );
    return response.data;
  }

  async markAsRead(conversationId: string) {
    await apiClient.patch(`/api/messages/conversations/${conversationId}/read`);
  }

  async getUnreadCount() {
    const response = await apiClient.get<{ count: number }>(
      "/api/messages/unread-count",
    );
    return response.data.count;
  }
}

export const messagingService = new MessagingService();
```

---

## 🛣️ RUTAS

```typescript
// src/App.tsx
import MyInquiriesPage from "./pages/MyInquiriesPage";
import ReceivedInquiriesPage from "./pages/ReceivedInquiriesPage";
import MessagesPage from "./pages/MessagesPage";

// Rutas protegidas
<Route
  path="/my-inquiries"
  element={
    <ProtectedRoute>
      <MyInquiriesPage />
    </ProtectedRoute>
  }
/>
<Route
  path="/received-inquiries"
  element={
    <ProtectedRoute>
      <ReceivedInquiriesPage />
    </ProtectedRoute>
  }
/>
<Route
  path="/messages"
  element={
    <ProtectedRoute>
      <MessagesPage />
    </ProtectedRoute>
  }
/>
<Route
  path="/messages/:conversationId"
  element={
    <ProtectedRoute>
      <MessagesPage />
    </ProtectedRoute>
  }
/>
```

---

## 🔄 FLUJO DE USUARIO

```
┌─────────────────────────────────────────────────────────────────┐
│                 FLUJO DE MENSAJERÍA                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  COMPRADOR:                                                     │
│  1. Ve vehículo interesante → /vehicles/{id}                   │
│  2. Click "Contactar Vendedor"                                 │
│  3. Envía mensaje inicial (consulta)                           │
│  4. Puede ver en /my-inquiries                                 │
│  5. Recibe notificación cuando vendedor responde               │
│  6. Continúa conversación en /messages                         │
│                                                                 │
│  VENDEDOR:                                                      │
│  1. Recibe notificación de nueva consulta                      │
│  2. Ve en /received-inquiries (badge "Nueva")                  │
│  3. Click "Responder" → Modal con info del comprador           │
│  4. Envía respuesta                                            │
│  5. Continúa en /messages                                      │
│  6. Puede cerrar consulta cuando se resuelve                   │
│                                                                 │
│  AMBOS:                                                         │
│  - /messages para conversación en tiempo real                  │
│  - Notificaciones de nuevos mensajes                           │
│  - Historial de conversaciones persistente                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ VALIDACIÓN

- [ ] Consultas enviadas visibles para comprador
- [ ] Consultas recibidas visibles para vendedor
- [ ] Filtros por estado funcionando
- [ ] Búsqueda funcionando
- [ ] Modal de respuesta con info del contacto
- [ ] Mensajes en tiempo real (polling 5s)
- [ ] Indicadores de leído (✓✓)
- [ ] Badge de mensajes no leídos
- [ ] Responsive: lista/chat separados en mobile

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/inquiries-messaging.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Consultas y Mensajes", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test.describe("Envío de Consultas", () => {
    test("debe enviar consulta desde detalle de vehículo", async ({ page }) => {
      await page.goto("/vehiculos/toyota-camry-2023");

      await page.getByRole("button", { name: /contactar/i }).click();
      await page.fill(
        'textarea[name="message"]',
        "¿Está disponible para verlo hoy?",
      );
      await page.click('button[type="submit"]');

      await expect(page.getByText(/mensaje enviado/i)).toBeVisible();
    });
  });

  test.describe("Inbox de Mensajes", () => {
    test("debe mostrar lista de conversaciones", async ({ page }) => {
      await page.goto("/mensajes");

      await expect(page.getByTestId("conversations-list")).toBeVisible();
    });

    test("debe filtrar por estado", async ({ page }) => {
      await page.goto("/mensajes");

      await page.getByRole("combobox", { name: /estado/i }).click();
      await page.getByRole("option", { name: /sin responder/i }).click();

      await expect(page).toHaveURL(/status=pending/);
    });

    test("debe buscar en mensajes", async ({ page }) => {
      await page.goto("/mensajes");

      await page.fill('input[placeholder*="buscar"]', "Toyota");
      await page.keyboard.press("Enter");

      await expect(page.getByTestId("conversation-item")).toHaveCount({
        min: 0,
      });
    });

    test("debe abrir chat y responder", async ({ page }) => {
      await page.goto("/mensajes");

      await page.getByTestId("conversation-item").first().click();
      await expect(page.getByTestId("chat-window")).toBeVisible();

      await page.fill('textarea[name="reply"]', "Sí, todavía está disponible.");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/Sí, todavía/)).toBeVisible();
    });

    test("debe mostrar indicador de leído", async ({ page }) => {
      await page.goto("/mensajes");

      await page.getByTestId("conversation-item").first().click();
      await expect(page.locator('[data-read="true"]')).toBeVisible();
    });
  });
});
```

---

_Última actualización: Enero 2026_
