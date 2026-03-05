# 🤖 25 - Chatbot API

**Servicio:** ChatbotService  
**Puerto:** 8080  
**Base Path:** `/api/chat`, `/api/leads`, `/api/configuration`  
**Autenticación:** ✅ Parcial

---

## 📖 Descripción

Sistema de chatbot IA para atención automatizada al cliente. Incluye:

- Sesiones de chat con usuarios
- Generación de leads automática
- Transferencia a agentes humanos
- Configuración por dealer
- Integración con WhatsApp

---

## 🎯 Endpoints Disponibles

### ChatController

| #   | Método | Endpoint                          | Auth | Descripción              |
| --- | ------ | --------------------------------- | ---- | ------------------------ |
| 1   | `POST` | `/api/chat/start`                 | ❌   | Iniciar sesión de chat   |
| 2   | `POST` | `/api/chat/message`               | ❌   | Enviar mensaje           |
| 3   | `POST` | `/api/chat/end`                   | ❌   | Finalizar sesión         |
| 4   | `POST` | `/api/chat/transfer`              | ❌   | Transferir a agente      |
| 5   | `GET`  | `/api/chat/session`               | ❌   | Obtener sesión por token |
| 6   | `GET`  | `/api/chat/session/{id}/messages` | ❌   | Mensajes de sesión       |

### LeadsController

| #   | Método | Endpoint                            | Auth            | Descripción       |
| --- | ------ | ----------------------------------- | --------------- | ----------------- |
| 7   | `GET`  | `/api/leads/{id}`                   | ✅ Admin/Dealer | Obtener lead      |
| 8   | `GET`  | `/api/leads/status/{status}`        | ✅              | Leads por estado  |
| 9   | `GET`  | `/api/leads/unassigned`             | ✅              | Leads sin asignar |
| 10  | `GET`  | `/api/leads/session/{sessionId}`    | ✅              | Lead por sesión   |
| 11  | `PUT`  | `/api/leads/{id}/status`            | ✅              | Actualizar estado |
| 12  | `PUT`  | `/api/leads/{id}/assign`            | ✅              | Asignar lead      |
| 13  | `GET`  | `/api/leads/count/today/{configId}` | ✅              | Leads de hoy      |

### ConfigurationController

| #   | Método | Endpoint                        | Auth | Descripción         |
| --- | ------ | ------------------------------- | ---- | ------------------- |
| 14  | `GET`  | `/api/configuration/{dealerId}` | ✅   | Config de dealer    |
| 15  | `POST` | `/api/configuration`            | ✅   | Crear configuración |
| 16  | `PUT`  | `/api/configuration/{id}`       | ✅   | Actualizar config   |

---

## 📝 Detalle de Endpoints

### 1. POST `/api/chat/start` - Iniciar Sesión

**Request:**

```json
{
  "userId": "user-123",
  "userName": "Juan Pérez",
  "userEmail": "juan@example.com",
  "userPhone": "+1 809-555-0100",
  "sessionType": "VehicleInquiry",
  "channel": "Web",
  "language": "es",
  "dealerId": "dealer-001"
}
```

**Session Types:** `VehicleInquiry`, `GeneralSupport`, `TestDrive`, `Financing`, `TradeIn`

**Channels:** `Web`, `WhatsApp`, `Messenger`, `Mobile`

**Response 200:**

```json
{
  "sessionId": "session-789",
  "sessionToken": "tok_abc123xyz",
  "welcomeMessage": "¡Hola Juan! Soy el asistente virtual de OKLA. ¿En qué puedo ayudarte hoy?",
  "quickReplies": [
    "Buscar vehículo",
    "Agendar test drive",
    "Consultar financiamiento",
    "Hablar con un asesor"
  ]
}
```

---

### 2. POST `/api/chat/message` - Enviar Mensaje

**Request:**

```json
{
  "sessionToken": "tok_abc123xyz",
  "message": "Busco un SUV Toyota 2024",
  "type": "Text",
  "mediaUrl": null
}
```

**Message Types:** `Text`, `Image`, `Audio`, `Document`, `Location`

**Response 200:**

```json
{
  "responseId": "resp-456",
  "message": "¡Excelente elección! Tenemos 5 SUVs Toyota 2024 disponibles. ¿Tienes preferencia de modelo?",
  "messageType": "Text",
  "suggestions": [
    { "text": "RAV4", "action": "search", "params": { "model": "RAV4" } },
    {
      "text": "Fortuner",
      "action": "search",
      "params": { "model": "Fortuner" }
    },
    { "text": "4Runner", "action": "search", "params": { "model": "4Runner" } }
  ],
  "vehicles": [
    {
      "id": "v1",
      "title": "Toyota RAV4 2024",
      "price": 2200000,
      "imageUrl": "..."
    },
    {
      "id": "v2",
      "title": "Toyota Fortuner 2024",
      "price": 2800000,
      "imageUrl": "..."
    }
  ],
  "leadGenerated": false,
  "requiresHumanAgent": false
}
```

---

### 4. POST `/api/chat/transfer` - Transferir a Agente

**Request:**

```json
{
  "sessionToken": "tok_abc123xyz",
  "transferReason": "Customer requested human agent"
}
```

**Response 200:**

```json
{
  "success": true,
  "agentName": "María García",
  "estimatedWaitTimeMinutes": 3,
  "message": "Te estamos conectando con María García. Tiempo estimado: 3 minutos."
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// CHAT SESSION
// ============================================================================

export type SessionType =
  | "VehicleInquiry"
  | "GeneralSupport"
  | "TestDrive"
  | "Financing"
  | "TradeIn";

export type ChatChannel = "Web" | "WhatsApp" | "Messenger" | "Mobile";

export type SessionStatus = "Active" | "Waiting" | "Transferred" | "Ended";

export interface StartSessionRequest {
  userId?: string;
  userName?: string;
  userEmail?: string;
  userPhone?: string;
  sessionType: SessionType;
  channel: ChatChannel;
  language?: string;
  dealerId?: string;
}

export interface StartSessionResponse {
  sessionId: string;
  sessionToken: string;
  welcomeMessage: string;
  quickReplies: string[];
}

export interface ChatSession {
  id: string;
  sessionToken: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  sessionType: SessionType;
  channel: ChatChannel;
  status: SessionStatus;
  messageCount: number;
  interactionCount: number;
  maxInteractionsPerSession: number;
  interactionLimitReached: boolean;
  createdAt: string;
  lastActivityAt: string;
}

// ============================================================================
// MESSAGES
// ============================================================================

export type MessageType = "Text" | "Image" | "Audio" | "Document" | "Location";

export interface SendMessageRequest {
  sessionToken: string;
  message: string;
  type: MessageType;
  mediaUrl?: string;
}

export interface ChatbotResponse {
  responseId: string;
  message: string;
  messageType: MessageType;
  suggestions?: ChatSuggestion[];
  vehicles?: VehicleSuggestion[];
  leadGenerated: boolean;
  requiresHumanAgent: boolean;
}

export interface ChatSuggestion {
  text: string;
  action: string;
  params?: Record<string, string>;
}

export interface VehicleSuggestion {
  id: string;
  title: string;
  price: number;
  imageUrl: string;
}

// ============================================================================
// LEADS
// ============================================================================

export type LeadStatus =
  | "New"
  | "InProgress"
  | "Qualified"
  | "Contacted"
  | "Converted"
  | "Lost";

export interface ChatLead {
  id: string;
  sessionId: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  userPhone?: string;
  status: LeadStatus;
  score: number;
  interestedVehicleIds: string[];
  notes?: string;
  assignedToUserId?: string;
  createdAt: string;
  updatedAt: string;
}

// ============================================================================
// CONFIGURATION
// ============================================================================

export interface ChatbotConfiguration {
  id: string;
  dealerId: string;
  isEnabled: boolean;
  welcomeMessage: string;
  workingHoursStart: string;
  workingHoursEnd: string;
  maxInteractionsPerSession: number;
  autoTransferAfterMinutes: number;
  quickReplies: string[];
  createdAt: string;
  updatedAt: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/chatbotService.ts
import { apiClient } from "./api-client";
import type {
  StartSessionRequest,
  StartSessionResponse,
  SendMessageRequest,
  ChatbotResponse,
  ChatSession,
  ChatLead,
  ChatbotConfiguration,
} from "@/types/chatbot";

class ChatbotService {
  // ============================================================================
  // CHAT
  // ============================================================================

  async startSession(
    request: StartSessionRequest,
  ): Promise<StartSessionResponse> {
    const response = await apiClient.post<StartSessionResponse>(
      "/api/chat/start",
      request,
    );
    return response.data;
  }

  async sendMessage(request: SendMessageRequest): Promise<ChatbotResponse> {
    const response = await apiClient.post<ChatbotResponse>(
      "/api/chat/message",
      request,
    );
    return response.data;
  }

  async endSession(sessionToken: string, endReason?: string): Promise<void> {
    await apiClient.post("/api/chat/end", { sessionToken, endReason });
  }

  async transferToAgent(
    sessionToken: string,
    reason: string,
  ): Promise<{ agentName: string; estimatedWaitTimeMinutes: number }> {
    const response = await apiClient.post("/api/chat/transfer", {
      sessionToken,
      transferReason: reason,
    });
    return response.data;
  }

  async getSession(token: string): Promise<ChatSession> {
    const response = await apiClient.get<ChatSession>("/api/chat/session", {
      params: { token },
    });
    return response.data;
  }

  // ============================================================================
  // LEADS
  // ============================================================================

  async getLead(id: string): Promise<ChatLead> {
    const response = await apiClient.get<ChatLead>(`/api/leads/${id}`);
    return response.data;
  }

  async getLeadsByStatus(
    status: string,
    page = 1,
    pageSize = 20,
  ): Promise<ChatLead[]> {
    const response = await apiClient.get<ChatLead[]>(
      `/api/leads/status/${status}`,
      {
        params: { page, pageSize },
      },
    );
    return response.data;
  }

  async getUnassignedLeads(): Promise<ChatLead[]> {
    const response = await apiClient.get<ChatLead[]>("/api/leads/unassigned");
    return response.data;
  }

  async updateLeadStatus(id: string, status: string): Promise<void> {
    await apiClient.put(`/api/leads/${id}/status`, { status });
  }

  async assignLead(id: string, userId: string): Promise<void> {
    await apiClient.put(`/api/leads/${id}/assign`, { userId });
  }

  // ============================================================================
  // CONFIGURATION
  // ============================================================================

  async getConfiguration(dealerId: string): Promise<ChatbotConfiguration> {
    const response = await apiClient.get<ChatbotConfiguration>(
      `/api/configuration/${dealerId}`,
    );
    return response.data;
  }

  async updateConfiguration(
    id: string,
    config: Partial<ChatbotConfiguration>,
  ): Promise<ChatbotConfiguration> {
    const response = await apiClient.put<ChatbotConfiguration>(
      `/api/configuration/${id}`,
      config,
    );
    return response.data;
  }
}

export const chatbotService = new ChatbotService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useChatbot.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { chatbotService } from "@/services/chatbotService";
import type { StartSessionRequest, SendMessageRequest } from "@/types/chatbot";

export const chatbotKeys = {
  all: ["chatbot"] as const,
  session: (token: string) => [...chatbotKeys.all, "session", token] as const,
  leads: () => [...chatbotKeys.all, "leads"] as const,
  leadsByStatus: (status: string) => [...chatbotKeys.leads(), status] as const,
  config: (dealerId: string) =>
    [...chatbotKeys.all, "config", dealerId] as const,
};

export function useStartChatSession() {
  return useMutation({
    mutationFn: (request: StartSessionRequest) =>
      chatbotService.startSession(request),
  });
}

export function useSendMessage() {
  return useMutation({
    mutationFn: (request: SendMessageRequest) =>
      chatbotService.sendMessage(request),
  });
}

export function useEndSession() {
  return useMutation({
    mutationFn: ({ token, reason }: { token: string; reason?: string }) =>
      chatbotService.endSession(token, reason),
  });
}

export function useTransferToAgent() {
  return useMutation({
    mutationFn: ({ token, reason }: { token: string; reason: string }) =>
      chatbotService.transferToAgent(token, reason),
  });
}

export function useChatSession(token: string) {
  return useQuery({
    queryKey: chatbotKeys.session(token),
    queryFn: () => chatbotService.getSession(token),
    enabled: !!token,
  });
}

export function useLeadsByStatus(status: string) {
  return useQuery({
    queryKey: chatbotKeys.leadsByStatus(status),
    queryFn: () => chatbotService.getLeadsByStatus(status),
  });
}

export function useChatbotConfig(dealerId: string) {
  return useQuery({
    queryKey: chatbotKeys.config(dealerId),
    queryFn: () => chatbotService.getConfiguration(dealerId),
    enabled: !!dealerId,
  });
}
```

---

## 🎉 Resumen

✅ **16 Endpoints documentados**  
✅ **TypeScript Types** (Session, Messages, Leads, Config)  
✅ **Service Layer** (12 métodos)  
✅ **React Query Hooks** (7 hooks)

---

_Última actualización: Enero 30, 2026_
