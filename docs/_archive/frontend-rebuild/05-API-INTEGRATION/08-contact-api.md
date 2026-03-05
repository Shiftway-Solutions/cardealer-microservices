# 📞 08 - Contact API (ContactService)

**Servicio:** ContactService  
**Puerto:** 8080  
**Base Path:** `/api/contactrequests`, `/api/contactmessages`  
**Autenticación:** ✅ Requerida (JWT Bearer Token)

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)
7. [Validaciones](#validaciones)
8. [Estados y Flujos](#estados-y-flujos)
9. [Notas de Seguridad](#notas-de-seguridad)

---

## 📖 Descripción General

El **ContactService** gestiona las consultas y mensajes entre compradores y vendedores. Permite a los compradores contactar a vendedores sobre vehículos específicos y mantener conversaciones completas.

### Casos de Uso

- 💬 Comprador envía consulta sobre un vehículo
- 📬 Vendedor recibe y responde consultas
- 🔔 Sistema de mensajería bidireccional
- ✅ Marcar mensajes como leídos
- 📊 Contador de mensajes sin leer

### Flujo Típico

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      FLUJO DE CONTACT/INQUIRY                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ COMPRADOR INICIA CONSULTA                                               │
│  ├─> Ve vehículo en VehicleDetailPage                                      │
│  ├─> Click botón "Contactar Vendedor"                                      │
│  ├─> Fill form: Subject, Message, Phone (opcional)                         │
│  └─> POST /api/contactrequests → Crea ContactRequest con Status="Open"     │
│                                                                             │
│  2️⃣ VENDEDOR RECIBE NOTIFICACIÓN                                            │
│  ├─> GET /api/contactrequests/received → Lista sus consultas pendientes    │
│  ├─> Ve badge "5 consultas sin leer"                                       │
│  ├─> Click en consulta específica                                          │
│  └─> GET /api/contactrequests/{id} → Ve mensaje inicial del comprador      │
│                                                                             │
│  3️⃣ VENDEDOR RESPONDE                                                       │
│  ├─> Escribe respuesta en chat                                             │
│  ├─> POST /api/contactrequests/{id}/reply                                  │
│  ├─> Backend actualiza Status = "Responded"                                │
│  └─> Comprador recibe notificación                                         │
│                                                                             │
│  4️⃣ CONVERSACIÓN CONTINÚA                                                   │
│  ├─> Ambos pueden enviar mensajes ilimitados                               │
│  ├─> POST /api/contactmessages/{id}/mark-read                              │
│  ├─> GET /api/contactmessages/unread-count                                 │
│  └─> Status puede cambiar: Open → Responded → InProgress → Closed          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Endpoints Disponibles

### ContactRequestsController

#### 1. POST `/api/contactrequests` - Crear Consulta

Comprador crea nueva consulta sobre un vehículo.

**Auth:** ✅ Required (Buyer)  
**Request Body:**

```json
{
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "sellerId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "subject": "Consulta sobre Toyota Corolla 2022",
  "buyerName": "Juan Pérez",
  "buyerEmail": "juan@example.com",
  "buyerPhone": "+1-809-555-1234",
  "message": "¿El vehículo está disponible para prueba de manejo?"
}
```

**Response 200:**

```json
{
  "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "subject": "Consulta sobre Toyota Corolla 2022",
  "status": "Open",
  "createdAt": "2026-01-30T10:30:00Z"
}
```

---

#### 2. GET `/api/contactrequests/my-inquiries` - Mis Consultas (Comprador)

Obtiene todas las consultas creadas por el usuario actual (buyer perspective).

**Auth:** ✅ Required (Buyer)  
**Query Params:** Ninguno

**Response 200:**

```json
[
  {
    "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "subject": "Consulta sobre Toyota Corolla 2022",
    "status": "Responded",
    "createdAt": "2026-01-30T10:30:00Z",
    "respondedAt": "2026-01-30T11:45:00Z",
    "messageCount": 3,
    "lastMessage": "Sí, disponible para prueba. ¿Cuándo le gustaría?"
  }
]
```

---

#### 3. GET `/api/contactrequests/received` - Consultas Recibidas (Vendedor)

Obtiene todas las consultas recibidas por el usuario actual (seller perspective).

**Auth:** ✅ Required (Seller)  
**Query Params:** Ninguno

**Response 200:**

```json
[
  {
    "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
    "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "subject": "Consulta sobre Toyota Corolla 2022",
    "buyerName": "Juan Pérez",
    "buyerEmail": "juan@example.com",
    "buyerPhone": "+1-809-555-1234",
    "status": "Open",
    "createdAt": "2026-01-30T10:30:00Z",
    "respondedAt": null,
    "messageCount": 1,
    "unreadCount": 1
  }
]
```

---

#### 4. GET `/api/contactrequests/{id}` - Detalle de Consulta con Mensajes

Obtiene consulta específica con todos sus mensajes (conversación completa).

**Auth:** ✅ Required (Buyer or Seller)  
**Path Params:**

- `id` (UUID) - ID de la consulta

**Response 200:**

```json
{
  "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
  "vehicleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "subject": "Consulta sobre Toyota Corolla 2022",
  "buyerName": "Juan Pérez",
  "buyerEmail": "juan@example.com",
  "buyerPhone": "+1-809-555-1234",
  "status": "Responded",
  "createdAt": "2026-01-30T10:30:00Z",
  "messages": [
    {
      "id": "1fa85f64-5717-4562-b3fc-2c963f66afa1",
      "senderId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
      "message": "¿El vehículo está disponible para prueba de manejo?",
      "isFromBuyer": true,
      "isRead": true,
      "sentAt": "2026-01-30T10:30:00Z"
    },
    {
      "id": "2fa85f64-5717-4562-b3fc-2c963f66afa2",
      "senderId": "8fa85f64-5717-4562-b3fc-2c963f66afa8",
      "message": "Sí, disponible. ¿Cuándo le gustaría?",
      "isFromBuyer": false,
      "isRead": false,
      "sentAt": "2026-01-30T11:45:00Z"
    }
  ]
}
```

**Response 404:** Consulta no encontrada  
**Response 403:** Usuario no autorizado (no es buyer ni seller de esta consulta)

---

#### 5. POST `/api/contactrequests/{id}/reply` - Responder Consulta

Envía un mensaje de respuesta en la conversación.

**Auth:** ✅ Required (Buyer or Seller)  
**Path Params:**

- `id` (UUID) - ID de la consulta

**Request Body:**

```json
{
  "message": "Perfecto, podría ser mañana a las 3 PM?"
}
```

**Response 200:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa3",
  "message": "Perfecto, podría ser mañana a las 3 PM?",
  "sentAt": "2026-01-30T12:00:00Z"
}
```

**Comportamiento:**

- Si el vendedor responde por primera vez, el Status cambia de "Open" a "Responded"
- Se registra `RespondedAt` timestamp
- El mensaje se guarda con `isFromBuyer` según quién envía

---

### ContactMessagesController

#### 6. POST `/api/contactmessages/{id}/mark-read` - Marcar Mensaje como Leído

Marca un mensaje específico como leído.

**Auth:** ✅ Required  
**Path Params:**

- `id` (UUID) - ID del mensaje

**Response 200:** `OK`

---

#### 7. GET `/api/contactmessages/unread-count` - Contador de Mensajes Sin Leer

Obtiene el número total de mensajes sin leer para el usuario actual.

**Auth:** ✅ Required  
**Query Params:** Ninguno

**Response 200:**

```json
{
  "count": 5
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// CONTACT REQUEST TYPES
// ============================================================================

export interface ContactRequest {
  id: string;
  vehicleId: string;
  buyerId: string;
  sellerId: string;
  subject: string;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  message: string;
  status: ContactRequestStatus;
  createdAt: string;
  respondedAt?: string;
  messageCount?: number;
  unreadCount?: number;
  lastMessage?: string;
  messages?: ContactMessage[];
}

export type ContactRequestStatus =
  | "Open" // Recién creado, sin respuesta
  | "Responded" // Vendedor respondió al menos una vez
  | "InProgress" // Conversación activa
  | "Closed"; // Finalizado (venta completada o cancelado)

export interface CreateContactRequestDto {
  vehicleId: string;
  sellerId: string;
  subject: string;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  message: string;
}

export interface ReplyToContactRequestDto {
  message: string;
}

// ============================================================================
// CONTACT MESSAGE TYPES
// ============================================================================

export interface ContactMessage {
  id: string;
  contactRequestId: string;
  senderId: string;
  message: string;
  isFromBuyer: boolean;
  isRead: boolean;
  sentAt: string;
}

export interface UnreadCountResponse {
  count: number;
}

// ============================================================================
// LIST RESPONSE TYPES (para buyer/seller perspectives)
// ============================================================================

export interface MyInquiry {
  id: string;
  vehicleId: string;
  subject: string;
  status: ContactRequestStatus;
  createdAt: string;
  respondedAt?: string;
  messageCount: number;
  lastMessage?: string;
}

export interface ReceivedInquiry {
  id: string;
  vehicleId: string;
  subject: string;
  buyerName: string;
  buyerEmail: string;
  buyerPhone?: string;
  status: ContactRequestStatus;
  createdAt: string;
  respondedAt?: string;
  messageCount: number;
  unreadCount: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/contactService.ts
import { apiClient } from "./api-client";
import type {
  ContactRequest,
  CreateContactRequestDto,
  ReplyToContactRequestDto,
  MyInquiry,
  ReceivedInquiry,
  UnreadCountResponse,
} from "@/types/contact";

class ContactService {
  // ============================================================================
  // CONTACT REQUESTS
  // ============================================================================

  /**
   * Crear nueva consulta (buyer crea inquiry sobre vehículo)
   */
  async createContactRequest(
    dto: CreateContactRequestDto,
  ): Promise<ContactRequest> {
    const response = await apiClient.post<ContactRequest>(
      "/api/contactrequests",
      dto,
    );
    return response.data;
  }

  /**
   * Obtener mis consultas (buyer perspective)
   */
  async getMyInquiries(): Promise<MyInquiry[]> {
    const response = await apiClient.get<MyInquiry[]>(
      "/api/contactrequests/my-inquiries",
    );
    return response.data;
  }

  /**
   * Obtener consultas recibidas (seller perspective)
   */
  async getReceivedInquiries(): Promise<ReceivedInquiry[]> {
    const response = await apiClient.get<ReceivedInquiry[]>(
      "/api/contactrequests/received",
    );
    return response.data;
  }

  /**
   * Obtener consulta con mensajes completos
   */
  async getContactRequestById(id: string): Promise<ContactRequest> {
    const response = await apiClient.get<ContactRequest>(
      `/api/contactrequests/${id}`,
    );
    return response.data;
  }

  /**
   * Responder a una consulta (buyer o seller envía mensaje)
   */
  async replyToContactRequest(
    id: string,
    dto: ReplyToContactRequestDto,
  ): Promise<ContactMessage> {
    const response = await apiClient.post<ContactMessage>(
      `/api/contactrequests/${id}/reply`,
      dto,
    );
    return response.data;
  }

  // ============================================================================
  // CONTACT MESSAGES
  // ============================================================================

  /**
   * Marcar mensaje como leído
   */
  async markMessageAsRead(messageId: string): Promise<void> {
    await apiClient.post(`/api/contactmessages/${messageId}/mark-read`);
  }

  /**
   * Obtener contador de mensajes sin leer
   */
  async getUnreadCount(): Promise<number> {
    const response = await apiClient.get<UnreadCountResponse>(
      "/api/contactmessages/unread-count",
    );
    return response.data.count;
  }
}

export const contactService = new ContactService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useContacts.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { contactService } from "@/services/contactService";
import type {
  CreateContactRequestDto,
  ReplyToContactRequestDto,
} from "@/types/contact";

// ============================================================================
// QUERY KEYS
// ============================================================================

export const contactKeys = {
  all: ["contacts"] as const,
  myInquiries: () => [...contactKeys.all, "my-inquiries"] as const,
  receivedInquiries: () => [...contactKeys.all, "received"] as const,
  detail: (id: string) => [...contactKeys.all, "detail", id] as const,
  unreadCount: () => [...contactKeys.all, "unread-count"] as const,
};

// ============================================================================
// QUERIES
// ============================================================================

/**
 * Hook: Obtener mis consultas (buyer perspective)
 */
export function useMyInquiries() {
  return useQuery({
    queryKey: contactKeys.myInquiries(),
    queryFn: () => contactService.getMyInquiries(),
  });
}

/**
 * Hook: Obtener consultas recibidas (seller perspective)
 */
export function useReceivedInquiries() {
  return useQuery({
    queryKey: contactKeys.receivedInquiries(),
    queryFn: () => contactService.getReceivedInquiries(),
  });
}

/**
 * Hook: Obtener consulta con mensajes
 */
export function useContactRequest(id: string) {
  return useQuery({
    queryKey: contactKeys.detail(id),
    queryFn: () => contactService.getContactRequestById(id),
    enabled: !!id,
    refetchInterval: 5000, // Refetch cada 5s para nuevos mensajes
  });
}

/**
 * Hook: Contador de mensajes sin leer
 */
export function useUnreadCount() {
  return useQuery({
    queryKey: contactKeys.unreadCount(),
    queryFn: () => contactService.getUnreadCount(),
    refetchInterval: 10000, // Refetch cada 10s
  });
}

// ============================================================================
// MUTATIONS
// ============================================================================

/**
 * Hook: Crear consulta
 */
export function useCreateContactRequest() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: CreateContactRequestDto) =>
      contactService.createContactRequest(dto),
    onSuccess: () => {
      // Invalidar lista de mis consultas
      queryClient.invalidateQueries({ queryKey: contactKeys.myInquiries() });
    },
  });
}

/**
 * Hook: Responder consulta
 */
export function useReplyToContactRequest(contactRequestId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: ReplyToContactRequestDto) =>
      contactService.replyToContactRequest(contactRequestId, dto),
    onSuccess: () => {
      // Invalidar detalle de la consulta para refrescar mensajes
      queryClient.invalidateQueries({
        queryKey: contactKeys.detail(contactRequestId),
      });
      // Invalidar listas (por si cambió status)
      queryClient.invalidateQueries({ queryKey: contactKeys.myInquiries() });
      queryClient.invalidateQueries({
        queryKey: contactKeys.receivedInquiries(),
      });
    },
  });
}

/**
 * Hook: Marcar mensaje como leído
 */
export function useMarkMessageAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (messageId: string) =>
      contactService.markMessageAsRead(messageId),
    onSuccess: () => {
      // Invalidar contador de sin leer
      queryClient.invalidateQueries({ queryKey: contactKeys.unreadCount() });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### 1. ContactForm - Formulario de Contacto (Buyer)

```typescript
// src/components/contact/ContactForm.tsx
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useCreateContactRequest } from "@/hooks/useContacts";
import type { CreateContactRequestDto } from "@/types/contact";

interface ContactFormProps {
  vehicleId: string;
  sellerId: string;
  vehicleTitle: string; // Ej: "Toyota Corolla 2022"
  onSuccess?: () => void;
}

export const ContactForm = ({
  vehicleId,
  sellerId,
  vehicleTitle,
  onSuccess,
}: ContactFormProps) => {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateContactRequestDto>();
  const createMutation = useCreateContactRequest();

  const onSubmit = async (data: CreateContactRequestDto) => {
    try {
      await createMutation.mutateAsync({
        ...data,
        vehicleId,
        sellerId,
        subject: `Consulta sobre ${vehicleTitle}`,
      });

      alert("✅ Consulta enviada exitosamente!");
      onSuccess?.();
    } catch (error) {
      alert("❌ Error al enviar consulta");
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* Nombre */}
      <div>
        <label className="block text-sm font-medium mb-1">
          Nombre Completo *
        </label>
        <input
          type="text"
          {...register("buyerName", { required: "Nombre requerido" })}
          className="w-full px-3 py-2 border rounded-lg"
          placeholder="Juan Pérez"
        />
        {errors.buyerName && (
          <p className="text-red-500 text-sm mt-1">{errors.buyerName.message}</p>
        )}
      </div>

      {/* Email */}
      <div>
        <label className="block text-sm font-medium mb-1">
          Email *
        </label>
        <input
          type="email"
          {...register("buyerEmail", {
            required: "Email requerido",
            pattern: {
              value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
              message: "Email inválido",
            },
          })}
          className="w-full px-3 py-2 border rounded-lg"
          placeholder="juan@example.com"
        />
        {errors.buyerEmail && (
          <p className="text-red-500 text-sm mt-1">{errors.buyerEmail.message}</p>
        )}
      </div>

      {/* Teléfono (opcional) */}
      <div>
        <label className="block text-sm font-medium mb-1">
          Teléfono (Opcional)
        </label>
        <input
          type="tel"
          {...register("buyerPhone")}
          className="w-full px-3 py-2 border rounded-lg"
          placeholder="+1-809-555-1234"
        />
      </div>

      {/* Mensaje */}
      <div>
        <label className="block text-sm font-medium mb-1">
          Mensaje *
        </label>
        <textarea
          {...register("message", {
            required: "Mensaje requerido",
            minLength: { value: 10, message: "Mínimo 10 caracteres" },
            maxLength: { value: 2000, message: "Máximo 2000 caracteres" },
          })}
          rows={4}
          className="w-full px-3 py-2 border rounded-lg"
          placeholder="¿El vehículo está disponible para prueba de manejo?"
        />
        {errors.message && (
          <p className="text-red-500 text-sm mt-1">{errors.message.message}</p>
        )}
      </div>

      {/* Submit Button */}
      <button
        type="submit"
        disabled={createMutation.isPending}
        className="w-full bg-blue-600 text-white py-2 px-4 rounded-lg hover:bg-blue-700 disabled:opacity-50"
      >
        {createMutation.isPending ? "Enviando..." : "📧 Enviar Consulta"}
      </button>
    </form>
  );
};
```

---

### 2. MyInquiriesList - Lista de Mis Consultas (Buyer)

```typescript
// src/components/contact/MyInquiriesList.tsx
import { useMyInquiries } from "@/hooks/useContacts";
import { Link } from "react-router-dom";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export const MyInquiriesList = () => {
  const { data: inquiries, isLoading, error } = useMyInquiries();

  if (isLoading) {
    return <div className="text-center py-8">Cargando consultas...</div>;
  }

  if (error) {
    return (
      <div className="text-center py-8 text-red-600">
        Error al cargar consultas
      </div>
    );
  }

  if (!inquiries || inquiries.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <p className="text-gray-500">No tienes consultas aún</p>
        <Link to="/vehicles" className="text-blue-600 mt-2 inline-block">
          🔍 Explorar vehículos
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {inquiries.map((inquiry) => (
        <Link
          key={inquiry.id}
          to={`/inquiries/${inquiry.id}`}
          className="block bg-white p-4 rounded-lg border hover:shadow-md transition-shadow"
        >
          {/* Header */}
          <div className="flex items-start justify-between mb-2">
            <h3 className="font-semibold text-lg">{inquiry.subject}</h3>
            <StatusBadge status={inquiry.status} />
          </div>

          {/* Last Message Preview */}
          {inquiry.lastMessage && (
            <p className="text-gray-600 text-sm mb-3 line-clamp-2">
              {inquiry.lastMessage}
            </p>
          )}

          {/* Meta */}
          <div className="flex items-center gap-4 text-sm text-gray-500">
            <span>
              💬 {inquiry.messageCount}{" "}
              {inquiry.messageCount === 1 ? "mensaje" : "mensajes"}
            </span>
            <span>
              🕐{" "}
              {formatDistanceToNow(new Date(inquiry.createdAt), {
                addSuffix: true,
                locale: es,
              })}
            </span>
            {inquiry.respondedAt && (
              <span className="text-green-600">✅ Respondido</span>
            )}
          </div>
        </Link>
      ))}
    </div>
  );
};

// Helper component
const StatusBadge = ({ status }: { status: string }) => {
  const colors = {
    Open: "bg-yellow-100 text-yellow-800",
    Responded: "bg-green-100 text-green-800",
    InProgress: "bg-blue-100 text-blue-800",
    Closed: "bg-gray-100 text-gray-800",
  };

  return (
    <span
      className={`px-2 py-1 text-xs font-medium rounded ${colors[status] || ""}`}
    >
      {status}
    </span>
  );
};
```

---

### 3. ContactConversation - Chat Completo

```typescript
// src/components/contact/ContactConversation.tsx
import { useContactRequest, useReplyToContactRequest } from "@/hooks/useContacts";
import { useState } from "react";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

interface ContactConversationProps {
  contactRequestId: string;
  currentUserId: string;
}

export const ContactConversation = ({
  contactRequestId,
  currentUserId,
}: ContactConversationProps) => {
  const { data: contact, isLoading } = useContactRequest(contactRequestId);
  const replyMutation = useReplyToContactRequest(contactRequestId);
  const [replyMessage, setReplyMessage] = useState("");

  const handleSendReply = async () => {
    if (!replyMessage.trim()) return;

    try {
      await replyMutation.mutateAsync({ message: replyMessage });
      setReplyMessage(""); // Clear input
    } catch (error) {
      alert("Error al enviar mensaje");
    }
  };

  if (isLoading) {
    return <div className="text-center py-8">Cargando conversación...</div>;
  }

  if (!contact) {
    return <div className="text-center py-8">Consulta no encontrada</div>;
  }

  const isBuyer = contact.buyerId === currentUserId;

  return (
    <div className="flex flex-col h-[600px]">
      {/* Header */}
      <div className="bg-white p-4 border-b">
        <h2 className="text-lg font-semibold">{contact.subject}</h2>
        <div className="text-sm text-gray-500 mt-1">
          {isBuyer ? (
            <span>Consulta con vendedor</span>
          ) : (
            <span>
              Consulta de: {contact.buyerName} ({contact.buyerEmail})
            </span>
          )}
        </div>
      </div>

      {/* Messages List */}
      <div className="flex-1 overflow-y-auto p-4 space-y-3 bg-gray-50">
        {contact.messages?.map((message) => {
          const isMyMessage = message.senderId === currentUserId;

          return (
            <div
              key={message.id}
              className={`flex ${isMyMessage ? "justify-end" : "justify-start"}`}
            >
              <div
                className={`max-w-[70%] rounded-lg p-3 ${
                  isMyMessage
                    ? "bg-blue-600 text-white"
                    : "bg-white text-gray-900"
                }`}
              >
                <p className="text-sm">{message.message}</p>
                <p
                  className={`text-xs mt-1 ${
                    isMyMessage ? "text-blue-100" : "text-gray-500"
                  }`}
                >
                  {formatDistanceToNow(new Date(message.sentAt), {
                    addSuffix: true,
                    locale: es,
                  })}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Reply Input */}
      <div className="bg-white p-4 border-t">
        <div className="flex gap-2">
          <input
            type="text"
            value={replyMessage}
            onChange={(e) => setReplyMessage(e.target.value)}
            onKeyPress={(e) => e.key === "Enter" && handleSendReply()}
            placeholder="Escribe tu mensaje..."
            className="flex-1 px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            onClick={handleSendReply}
            disabled={!replyMessage.trim() || replyMutation.isPending}
            className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {replyMutation.isPending ? "..." : "Enviar"}
          </button>
        </div>
      </div>
    </div>
  );
};
```

---

### 4. UnreadMessagesBadge - Badge de Mensajes Sin Leer

```typescript
// src/components/contact/UnreadMessagesBadge.tsx
import { useUnreadCount } from "@/hooks/useContacts";

export const UnreadMessagesBadge = () => {
  const { data: count } = useUnreadCount();

  if (!count || count === 0) {
    return null;
  }

  return (
    <span className="absolute -top-1 -right-1 bg-red-600 text-white text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center">
      {count > 9 ? "9+" : count}
    </span>
  );
};

// Uso en Navbar:
// <div className="relative">
//   <Link to="/inquiries">
//     <FiMail className="h-6 w-6" />
//     <UnreadMessagesBadge />
//   </Link>
// </div>
```

---

## ✅ Validaciones

### Backend (C# FluentValidation)

```csharp
// ContactService.Application/Validators/CreateContactRequestValidator.cs
public class CreateContactRequestValidator : AbstractValidator<CreateContactRequestDto>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("VehicleId es requerido");

        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("SellerId es requerido");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject es requerido")
            .MaximumLength(100).WithMessage("Subject máximo 100 caracteres");

        RuleFor(x => x.BuyerName)
            .NotEmpty().WithMessage("Nombre es requerido")
            .MaximumLength(100).WithMessage("Nombre máximo 100 caracteres");

        RuleFor(x => x.BuyerEmail)
            .NotEmpty().WithMessage("Email es requerido")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(100).WithMessage("Email máximo 100 caracteres");

        RuleFor(x => x.BuyerPhone)
            .MaximumLength(20).WithMessage("Teléfono máximo 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.BuyerPhone));

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mensaje es requerido")
            .MinimumLength(10).WithMessage("Mensaje mínimo 10 caracteres")
            .MaximumLength(2000).WithMessage("Mensaje máximo 2000 caracteres");
    }
}
```

### Frontend (React Hook Form + Zod)

```typescript
// src/schemas/contactSchema.ts
import { z } from "zod";

export const createContactRequestSchema = z.object({
  vehicleId: z.string().uuid("VehicleId inválido"),
  sellerId: z.string().uuid("SellerId inválido"),
  subject: z.string().max(100, "Máximo 100 caracteres"),
  buyerName: z
    .string()
    .min(1, "Nombre requerido")
    .max(100, "Máximo 100 caracteres"),
  buyerEmail: z
    .string()
    .email("Email inválido")
    .max(100, "Máximo 100 caracteres"),
  buyerPhone: z.string().max(20, "Máximo 20 caracteres").optional(),
  message: z
    .string()
    .min(10, "Mínimo 10 caracteres")
    .max(2000, "Máximo 2000 caracteres"),
});

export type CreateContactRequestFormData = z.infer<
  typeof createContactRequestSchema
>;
```

---

## 🔄 Estados y Flujos

### Estados de ContactRequest

```typescript
type ContactRequestStatus = "Open" | "Responded" | "InProgress" | "Closed";
```

| Estado       | Descripción                                 | Transición Desde            |
| ------------ | ------------------------------------------- | --------------------------- |
| `Open`       | Recién creado, vendedor aún no responde     | Initial                     |
| `Responded`  | Vendedor respondió al menos una vez         | Open                        |
| `InProgress` | Conversación activa (ambos respondiendo)    | Responded                   |
| `Closed`     | Finalizado (venta completada o sin interés) | Open, Responded, InProgress |

### Diagrama de Flujo

```
┌──────────┐
│   Open   │ ← Buyer crea consulta
└────┬─────┘
     │
     │ Seller responde por primera vez
     ▼
┌──────────┐
│Responded │
└────┬─────┘
     │
     │ Conversación continúa (múltiples mensajes)
     ▼
┌──────────┐
│InProgress│
└────┬─────┘
     │
     │ Manual close o auto-close después de X días
     ▼
┌──────────┐
│  Closed  │
└──────────┘
```

---

## 🔒 Notas de Seguridad

### Autorización

1. **Crear Consulta:** Solo usuarios autenticados (Buyer role)
2. **Ver Consulta:** Solo buyer o seller de esa consulta específica
3. **Responder:** Solo buyer o seller de esa consulta
4. **Marcar como leído:** Solo el destinatario del mensaje

### Validación de Permisos

```csharp
// Backend - ContactRequestsController.cs
[HttpGet("{id}")]
public async Task<IActionResult> GetContactRequest(Guid id)
{
    var contactRequest = await _contactRequestRepository.GetByIdAsync(id);
    if (contactRequest == null) return NotFound();

    var currentUserId = GetCurrentUserId();

    // ✅ Verificar que el usuario es buyer O seller
    if (contactRequest.BuyerId != currentUserId && contactRequest.SellerId != currentUserId)
    {
        return Forbid(); // 403 Forbidden
    }

    // ...
}
```

### Rate Limiting (Recomendado)

```csharp
// Limitar creación de consultas por usuario
// Máximo 10 consultas por hora por usuario
[RateLimit(PermitLimit = 10, Window = 60)] // 10 per hour
public async Task<IActionResult> CreateContactRequest(...)
```

### Sanitización de Inputs

```typescript
// Frontend - Escapar HTML en mensajes
import DOMPurify from "dompurify";

const sanitizedMessage = DOMPurify.sanitize(message);
```

### GDPR Compliance

- ✅ Buyer puede eliminar su cuenta → Anonimizar consultas
- ✅ Exportar datos: Incluir todas las consultas y mensajes
- ✅ Retention policy: Borrar consultas cerradas después de 2 años

---

## 📊 Uso en Componentes Principales

### VehicleDetailPage

```typescript
// src/pages/VehicleDetailPage.tsx
import { ContactForm } from "@/components/contact/ContactForm";
import { useState } from "react";

export const VehicleDetailPage = () => {
  const [showContactForm, setShowContactForm] = useState(false);
  const { data: vehicle } = useVehicle(vehicleId);

  return (
    <div>
      {/* Vehicle details */}

      {/* Contact Button */}
      <button
        onClick={() => setShowContactForm(true)}
        className="bg-blue-600 text-white px-6 py-3 rounded-lg"
      >
        📧 Contactar Vendedor
      </button>

      {/* Contact Form Modal */}
      {showContactForm && vehicle && (
        <Modal onClose={() => setShowContactForm(false)}>
          <ContactForm
            vehicleId={vehicle.id}
            sellerId={vehicle.sellerId}
            vehicleTitle={`${vehicle.make} ${vehicle.model} ${vehicle.year}`}
            onSuccess={() => {
              setShowContactForm(false);
              alert("✅ Consulta enviada!");
            }}
          />
        </Modal>
      )}
    </div>
  );
};
```

### MyInquiriesPage (Buyer Dashboard)

```typescript
// src/pages/MyInquiriesPage.tsx
import { MyInquiriesList } from "@/components/contact/MyInquiriesList";

export const MyInquiriesPage = () => {
  return (
    <div className="container mx-auto py-8">
      <h1 className="text-3xl font-bold mb-6">Mis Consultas</h1>
      <MyInquiriesList />
    </div>
  );
};
```

### ReceivedInquiriesPage (Seller Dashboard)

```typescript
// src/pages/ReceivedInquiriesPage.tsx
import { useReceivedInquiries } from "@/hooks/useContacts";
import { Link } from "react-router-dom";

export const ReceivedInquiriesPage = () => {
  const { data: inquiries, isLoading } = useReceivedInquiries();

  return (
    <div className="container mx-auto py-8">
      <h1 className="text-3xl font-bold mb-6">Consultas Recibidas</h1>

      {/* Lista de consultas */}
      <div className="space-y-4">
        {inquiries?.map((inquiry) => (
          <Link
            key={inquiry.id}
            to={`/inquiries/${inquiry.id}`}
            className="block bg-white p-6 rounded-lg border hover:shadow-lg"
          >
            <div className="flex justify-between items-start">
              <div>
                <h3 className="font-semibold text-lg">{inquiry.subject}</h3>
                <p className="text-gray-600 text-sm mt-1">
                  De: {inquiry.buyerName} ({inquiry.buyerEmail})
                </p>
              </div>

              {inquiry.unreadCount > 0 && (
                <span className="bg-red-600 text-white text-xs font-bold rounded-full px-2 py-1">
                  {inquiry.unreadCount} sin leer
                </span>
              )}
            </div>

            <div className="flex gap-4 mt-4 text-sm text-gray-500">
              <span>💬 {inquiry.messageCount} mensajes</span>
              <span className={inquiry.respondedAt ? "text-green-600" : "text-yellow-600"}>
                {inquiry.respondedAt ? "✅ Respondido" : "⏳ Pendiente"}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **7 Endpoints documentados**  
✅ **TypeScript Types completos** (ContactRequest, ContactMessage, DTOs)  
✅ **Service Layer** con 7 métodos  
✅ **7 React Query Hooks** (queries + mutations)  
✅ **4 Componentes UI** completos y funcionales  
✅ **Validaciones** backend + frontend  
✅ **Seguridad** con autorización por roles  
✅ **Real-time updates** con refetchInterval

---

**🚀 Próximos Pasos:**

- Integrar ContactForm en VehicleDetailPage
- Agregar notificaciones push para nuevos mensajes
- Implementar auto-close de consultas después de 30 días sin actividad
- Dashboard de métricas para sellers (response time, conversion rate)

---

_Última actualización: Enero 30, 2026_
