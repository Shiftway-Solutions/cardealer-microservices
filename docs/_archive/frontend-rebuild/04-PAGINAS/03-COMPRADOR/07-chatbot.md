---
title: "22. Chatbot con IA y Calificación de Leads"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 22. Chatbot con IA y Calificación de Leads

**Objetivo:** Implementar chatbot inteligente con IA (OpenAI/Claude) para asistencia 24/7, calificación automática de leads (hot/warm/cold), integración con WhatsApp y CRM.

**Prioridad:** P2 (Baja - Mejora de conversión y soporte)  
**Complejidad:** 🔴 Muy Alta (IA, NLP, Integraciones múltiples)  
**Dependencias:** ChatbotService (backend), CRMService, WhatsAppService, NotificationService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura del Chatbot](#arquitectura-del-chatbot)
2. [Sistema de Calificación de Leads](#sistema-de-calificación-de-leads)
3. [Backend API](#backend-api)
4. [Componentes Frontend](#componentes-frontend)
5. [Integración WhatsApp](#integración-whatsapp)
6. [Hooks y Servicios](#hooks-y-servicios)
7. [Tipos TypeScript](#tipos-typescript)
8. [Validación](#validación)

---

## 🤖 ARQUITECTURA DEL CHATBOT

### Flujo de Conversación

```
Usuario envía mensaje
    ↓
WebSocket → ChatbotService
    ↓
Intent Classification (IA)
├─ Pregunta sobre vehículo → VehicleIntent
├─ Consulta de precio → PricingIntent
├─ Agendar test drive → TestDriveIntent
├─ Información de contacto → ContactIntent
├─ Financiamiento → FinancingIntent
└─ General → GeneralIntent
    ↓
Context Manager (mantiene historial)
    ↓
Response Generator (IA + Templates)
├─ Si respuesta directa → Enviar respuesta
├─ Si requiere datos → Query a DB/API
├─ Si requiere humano → Transfer to agent
└─ Si intent crítico → Crear lead en CRM
    ↓
Lead Scoring Engine
├─ Calcula score basado en:
│  - Intent urgency
│  - User behavior
│  - Message sentiment
│  - Timing
│  - Budget indicators
└─ Clasifica: HOT / WARM / COLD
    ↓
Notification System
├─ HOT lead → Notificar dealer inmediatamente (SMS/WhatsApp)
├─ WARM lead → Email + Push notification
└─ COLD lead → Agregar a funnel de nurturing
    ↓
Response enviada al usuario
```

---

## 🔥 SISTEMA DE CALIFICACIÓN DE LEADS

### Lead Scoring Algorithm

```typescript
// filepath: docs/algorithms/lead-scoring.ts

interface LeadScoringFactors {
  // Behavioral (40%)
  pageViews: number; // 0-10+
  timeOnSite: number; // minutes
  vehiclesViewed: number; // 0-20+
  favorited: boolean;
  comparedVehicles: boolean;

  // Intent Signals (35%)
  priceInquiry: boolean;
  testDriveRequest: boolean;
  financingQuestion: boolean;
  contactInfoProvided: boolean;
  urgencyKeywords: string[]; // "hoy", "ahora", "urgente"

  // User Profile (15%)
  isReturningUser: boolean;
  hasAccount: boolean;
  previousPurchases: number;

  // Timing (10%)
  timeOfDay: "business" | "evening" | "late_night";
  dayOfWeek: "weekday" | "weekend";
  responseTime: number; // seconds
}

function calculateLeadScore(factors: LeadScoringFactors): {
  score: number; // 0-100
  classification: "HOT" | "WARM" | "COLD";
  confidence: number; // 0-1
} {
  let score = 0;

  // Behavioral Score (40 points max)
  score += Math.min(factors.pageViews * 2, 10);
  score += Math.min(factors.timeOnSite / 3, 10);
  score += Math.min(factors.vehiclesViewed * 2, 10);
  if (factors.favorited) score += 5;
  if (factors.comparedVehicles) score += 5;

  // Intent Signals (35 points max)
  if (factors.priceInquiry) score += 8;
  if (factors.testDriveRequest) score += 12;
  if (factors.financingQuestion) score += 7;
  if (factors.contactInfoProvided) score += 8;

  // Urgency keywords
  const urgencyScore = factors.urgencyKeywords.length * 3;
  score += Math.min(urgencyScore, 10);

  // User Profile (15 points max)
  if (factors.isReturningUser) score += 5;
  if (factors.hasAccount) score += 5;
  score += Math.min(factors.previousPurchases * 5, 5);

  // Timing (10 points max)
  if (factors.timeOfDay === "business") score += 5;
  if (factors.dayOfWeek === "weekday") score += 3;
  if (factors.responseTime < 60) score += 2;

  // Classification
  let classification: "HOT" | "WARM" | "COLD";
  if (score >= 70) classification = "HOT";
  else if (score >= 40) classification = "WARM";
  else classification = "COLD";

  // Confidence based on data completeness
  const dataPoints = Object.keys(factors).length;
  const confidence = Math.min(dataPoints / 15, 1);

  return { score, classification, confidence };
}
```

### Urgency Detection (NLP)

```typescript
const urgencyKeywords = {
  high: [
    "hoy",
    "ahora",
    "ya",
    "urgente",
    "inmediato",
    "cuanto antes",
    "lo antes posible",
    "esta semana",
  ],
  medium: [
    "pronto",
    "próximamente",
    "próximo mes",
    "en breve",
    "dentro de poco",
  ],
  low: [
    "algún día",
    "eventualmente",
    "tal vez",
    "en el futuro",
    "cuando pueda",
  ],
};

function detectUrgency(message: string): "high" | "medium" | "low" {
  const lowerMessage = message.toLowerCase();

  for (const keyword of urgencyKeywords.high) {
    if (lowerMessage.includes(keyword)) return "high";
  }

  for (const keyword of urgencyKeywords.medium) {
    if (lowerMessage.includes(keyword)) return "medium";
  }

  return "low";
}
```

---

## 🔌 BACKEND API

### ChatbotService Endpoints

```typescript
// filepath: docs/backend/ChatbotService-API.md

POST   /api/chatbot/message              # Enviar mensaje (WebSocket alternativa)
GET    /api/chatbot/conversations        # Historial de conversaciones
GET    /api/chatbot/conversations/{id}   # Conversación específica
DELETE /api/chatbot/conversations/{id}   # Eliminar conversación

POST   /api/chatbot/transfer-to-agent    # Transferir a agente humano
POST   /api/chatbot/lead                 # Crear lead desde chatbot
GET    /api/chatbot/lead-score/{id}      # Score del lead

POST   /api/chatbot/whatsapp/webhook     # Webhook de WhatsApp
GET    /api/chatbot/whatsapp/status      # Estado de integración

GET    /api/chatbot/intents              # Intents disponibles
POST   /api/chatbot/train                # Re-entrenar modelo (admin)
GET    /api/chatbot/stats                # Estadísticas (admin)
```

### WebSocket Events

```typescript
// Client → Server
{
  "event": "message",
  "data": {
    "conversationId": "uuid",
    "content": "¿Cuál es el precio?",
    "metadata": {
      "vehicleId": "uuid",
      "source": "widget" | "whatsapp" | "page"
    }
  }
}

// Server → Client
{
  "event": "response",
  "data": {
    "messageId": "uuid",
    "content": "El precio de este Toyota RAV4 2023 es $35,000 USD.",
    "type": "text" | "quick_replies" | "card" | "carousel",
    "quickReplies": ["Ver fotos", "Agendar test drive", "Hablar con vendedor"],
    "confidence": 0.95
  }
}

{
  "event": "typing",
  "data": {
    "isTyping": true
  }
}

{
  "event": "lead_created",
  "data": {
    "leadId": "uuid",
    "score": 85,
    "classification": "HOT"
  }
}
```

---

## 🎨 COMPONENTES FRONTEND

### PASO 1: ChatWidget - Widget Flotante

```typescript
// filepath: src/components/chatbot/ChatWidget.tsx
"use client";

import { useState, useEffect } from "react";
import { MessageCircle, X, Minimize2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { ChatWindow } from "./ChatWindow";
import { Badge } from "@/components/ui/Badge";
import { useChatbot } from "@/lib/hooks/useChatbot";

export function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [isMinimized, setIsMinimized] = useState(false);
  const { unreadCount } = useChatbot();

  useEffect(() => {
    // Auto-open after 30 seconds if user hasn't interacted
    const timer = setTimeout(() => {
      if (!isOpen && !localStorage.getItem("chatbot_dismissed")) {
        setIsOpen(true);
      }
    }, 30000);

    return () => clearTimeout(timer);
  }, [isOpen]);

  if (isMinimized) {
    return (
      <Button
        onClick={() => setIsMinimized(false)}
        className="fixed bottom-6 right-6 z-50 rounded-full shadow-lg"
        size="icon"
      >
        <MessageCircle size={24} />
        {unreadCount > 0 && (
          <Badge className="absolute -top-1 -right-1 bg-red-500 text-white">
            {unreadCount}
          </Badge>
        )}
      </Button>
    );
  }

  return (
    <>
      {/* Chat Button */}
      {!isOpen && (
        <Button
          onClick={() => setIsOpen(true)}
          className="fixed bottom-6 right-6 z-50 rounded-full shadow-lg"
          size="lg"
        >
          <MessageCircle size={24} className="mr-2" />
          ¿Necesitas ayuda?
          {unreadCount > 0 && (
            <Badge className="ml-2 bg-white text-primary-600">
              {unreadCount}
            </Badge>
          )}
        </Button>
      )}

      {/* Chat Window */}
      {isOpen && (
        <div className="fixed bottom-6 right-6 z-50 w-96 h-[600px] bg-white rounded-xl shadow-2xl flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between p-4 border-b bg-gradient-to-r from-primary-600 to-primary-700 text-white rounded-t-xl">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center">
                <MessageCircle size={20} />
              </div>
              <div>
                <h3 className="font-semibold">Asistente OKLA</h3>
                <p className="text-xs text-white/80">
                  <span className="inline-block w-2 h-2 bg-green-400 rounded-full mr-1" />
                  En línea
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setIsMinimized(true)}
                className="text-white hover:bg-white/10"
              >
                <Minimize2 size={18} />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => {
                  setIsOpen(false);
                  localStorage.setItem("chatbot_dismissed", "true");
                }}
                className="text-white hover:bg-white/10"
              >
                <X size={18} />
              </Button>
            </div>
          </div>

          {/* Chat Content */}
          <ChatWindow />
        </div>
      )}
    </>
  );
}
```

---

### PASO 2: ChatWindow - Ventana de Conversación

```typescript
// filepath: src/components/chatbot/ChatWindow.tsx
"use client";

import { useState, useRef, useEffect } from "react";
import { Send, Paperclip } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { ChatMessage } from "./ChatMessage";
import { QuickReplies } from "./QuickReplies";
import { TypingIndicator } from "./TypingIndicator";
import { useChatbot } from "@/lib/hooks/useChatbot";

export function ChatWindow() {
  const [input, setInput] = useState("");
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const {
    messages,
    isTyping,
    quickReplies,
    sendMessage,
    sendQuickReply,
  } = useChatbot();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isTyping]);

  const handleSend = () => {
    if (!input.trim()) return;
    sendMessage(input);
    setInput("");
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <>
      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Welcome Message */}
        {messages.length === 0 && (
          <div className="text-center py-8 text-gray-500">
            <p className="text-lg font-medium mb-2">¡Hola! 👋</p>
            <p className="text-sm">
              Soy tu asistente virtual. ¿En qué puedo ayudarte hoy?
            </p>
          </div>
        )}

        {/* Messages */}
        {messages.map((message) => (
          <ChatMessage key={message.id} message={message} />
        ))}

        {/* Typing Indicator */}
        {isTyping && <TypingIndicator />}

        <div ref={messagesEndRef} />
      </div>

      {/* Quick Replies */}
      {quickReplies && quickReplies.length > 0 && (
        <div className="px-4 pb-2">
          <QuickReplies
            replies={quickReplies}
            onSelect={(reply) => sendQuickReply(reply)}
          />
        </div>
      )}

      {/* Input */}
      <div className="p-4 border-t">
        <div className="flex items-end gap-2">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="Escribe tu mensaje..."
            className="flex-1"
            maxLength={500}
          />
          <Button
            onClick={handleSend}
            disabled={!input.trim()}
            size="icon"
          >
            <Send size={18} />
          </Button>
        </div>
        <p className="text-xs text-gray-500 mt-1">
          Presiona Enter para enviar
        </p>
      </div>
    </>
  );
}
```

---

### PASO 3: ChatMessage - Mensaje Individual

```typescript
// filepath: src/components/chatbot/ChatMessage.tsx
"use client";

import { format } from "date-fns";
import { es } from "date-fns/locale";
import { User, Bot, CheckCheck } from "lucide-react";
import { cn } from "@/lib/utils";
import type { ChatMessage as ChatMessageType } from "@/types/chatbot";

interface ChatMessageProps {
  message: ChatMessageType;
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isBot = message.sender === "bot";

  return (
    <div className={cn("flex gap-3", isBot ? "justify-start" : "justify-end")}>
      {/* Avatar (bot only) */}
      {isBot && (
        <div className="w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center flex-shrink-0">
          <Bot size={16} className="text-primary-600" />
        </div>
      )}

      {/* Message Content */}
      <div className={cn("max-w-[80%]", !isBot && "order-first")}>
        <div
          className={cn(
            "rounded-2xl px-4 py-2",
            isBot
              ? "bg-gray-100 text-gray-900"
              : "bg-primary-600 text-white"
          )}
        >
          {message.type === "text" && <p className="text-sm">{message.content}</p>}

          {message.type === "card" && message.card && (
            <VehicleCard vehicle={message.card} />
          )}

          {message.type === "carousel" && message.carousel && (
            <VehicleCarousel vehicles={message.carousel} />
          )}
        </div>

        {/* Timestamp */}
        <div className={cn("flex items-center gap-1 mt-1 px-2", !isBot && "justify-end")}>
          <time className="text-xs text-gray-500">
            {format(new Date(message.timestamp), "HH:mm", { locale: es })}
          </time>
          {!isBot && message.status === "read" && (
            <CheckCheck size={14} className="text-blue-500" />
          )}
        </div>
      </div>

      {/* Avatar (user only) */}
      {!isBot && (
        <div className="w-8 h-8 bg-primary-600 rounded-full flex items-center justify-center flex-shrink-0">
          <User size={16} className="text-white" />
        </div>
      )}
    </div>
  );
}

function VehicleCard({ vehicle }: any) {
  return (
    <div className="bg-white rounded-lg overflow-hidden shadow-sm">
      <img
        src={vehicle.image}
        alt={vehicle.name}
        className="w-full h-32 object-cover"
      />
      <div className="p-3">
        <h4 className="font-semibold text-sm">{vehicle.name}</h4>
        <p className="text-primary-600 font-bold text-lg">{vehicle.price}</p>
        <Button size="sm" className="w-full mt-2">
          Ver detalles
        </Button>
      </div>
    </div>
  );
}

function VehicleCarousel({ vehicles }: any) {
  return (
    <div className="flex gap-2 overflow-x-auto">
      {vehicles.map((vehicle: any) => (
        <VehicleCard key={vehicle.id} vehicle={vehicle} />
      ))}
    </div>
  );
}
```

---

### PASO 4: QuickReplies - Respuestas Rápidas

```typescript
// filepath: src/components/chatbot/QuickReplies.tsx
"use client";

interface QuickRepliesProps {
  replies: string[];
  onSelect: (reply: string) => void;
}

export function QuickReplies({ replies, onSelect }: QuickRepliesProps) {
  return (
    <div className="flex flex-wrap gap-2">
      {replies.map((reply, index) => (
        <button
          key={index}
          onClick={() => onSelect(reply)}
          className="px-3 py-1.5 bg-white border-2 border-primary-600 text-primary-600 rounded-full text-sm font-medium hover:bg-primary-50 transition"
        >
          {reply}
        </button>
      ))}
    </div>
  );
}
```

---

### PASO 5: LeadScoreIndicator - Indicador de Score

```typescript
// filepath: src/components/chatbot/LeadScoreIndicator.tsx
"use client";

import { Flame, Zap, Snowflake } from "lucide-react";
import { cn } from "@/lib/utils";
import type { LeadClassification } from "@/types/chatbot";

interface LeadScoreIndicatorProps {
  classification: LeadClassification;
  score: number;
  confidence: number;
}

export function LeadScoreIndicator({
  classification,
  score,
  confidence,
}: LeadScoreIndicatorProps) {
  const config = {
    HOT: {
      icon: Flame,
      label: "Lead Caliente",
      color: "text-red-600 bg-red-100",
      description: "Alta probabilidad de conversión",
    },
    WARM: {
      icon: Zap,
      label: "Lead Tibio",
      color: "text-orange-600 bg-orange-100",
      description: "Interés moderado",
    },
    COLD: {
      icon: Snowflake,
      label: "Lead Frío",
      color: "text-blue-600 bg-blue-100",
      description: "Necesita nurturing",
    },
  };

  const { icon: Icon, label, color, description } = config[classification];

  return (
    <div className={cn("inline-flex items-center gap-2 px-3 py-2 rounded-lg", color)}>
      <Icon size={20} />
      <div>
        <div className="font-semibold text-sm">{label}</div>
        <div className="text-xs opacity-75">
          Score: {score}/100 | Confianza: {(confidence * 100).toFixed(0)}%
        </div>
      </div>
    </div>
  );
}
```

---

## 📱 INTEGRACIÓN WHATSAPP

### PASO 6: WhatsApp Business API

```typescript
// filepath: docs/integrations/whatsapp-business-api.ts

/**
 * Integración con WhatsApp Business API
 * Proveedor: Twilio WhatsApp Business
 */

interface WhatsAppMessage {
  from: string; // +18095551234
  to: string; // +18095559999
  body: string;
  mediaUrl?: string;
}

// Webhook handler
async function handleWhatsAppWebhook(req: Request) {
  const { From, Body, MediaUrl0, MessageSid } = await req.formData();

  // Crear conversación si no existe
  let conversation = await chatbotService.getConversationByPhone(From);
  if (!conversation) {
    conversation = await chatbotService.createConversation({
      userId: null,
      source: "whatsapp",
      metadata: { phone: From },
    });
  }

  // Procesar mensaje
  const response = await chatbotService.processMessage({
    conversationId: conversation.id,
    content: Body,
    source: "whatsapp",
    metadata: {
      messageId: MessageSid,
      phone: From,
      mediaUrl: MediaUrl0,
    },
  });

  // Enviar respuesta por WhatsApp
  await twilioClient.messages.create({
    from: "whatsapp:+18095559999",
    to: `whatsapp:${From}`,
    body: response.content,
  });

  return new Response("OK", { status: 200 });
}

// Enviar mensaje proactivo
async function sendWhatsAppMessage(to: string, message: string) {
  return twilioClient.messages.create({
    from: "whatsapp:+18095559999",
    to: `whatsapp:${to}`,
    body: message,
  });
}

// Notificar dealer de HOT lead
async function notifyDealerHotLead(lead: Lead, dealer: Dealer) {
  const message = `
🔥 NUEVO LEAD CALIENTE

Cliente: ${lead.name}
Teléfono: ${lead.phone}
Interés: ${lead.vehicleName}
Score: ${lead.score}/100

Responde RÁPIDO para maximizar conversión.
Link: ${process.env.APP_URL}/crm/leads/${lead.id}
  `.trim();

  await sendWhatsAppMessage(dealer.phone, message);
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 7: useChatbot Hook

```typescript
// filepath: src/lib/hooks/useChatbot.ts
import { useState, useEffect, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { chatbotService } from "@/lib/services/chatbotService";
import type { ChatMessage, QuickReply } from "@/types/chatbot";

export function useChatbot() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const [quickReplies, setQuickReplies] = useState<string[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);

  const wsRef = useRef<WebSocket | null>(null);
  const conversationIdRef = useRef<string | null>(null);

  useEffect(() => {
    // Initialize WebSocket
    const ws = new WebSocket(process.env.NEXT_PUBLIC_WS_URL + "/chatbot");
    wsRef.current = ws;

    ws.onopen = () => {
      console.log("Chatbot WebSocket connected");
    };

    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);

      switch (data.event) {
        case "response":
          setMessages((prev) => [
            ...prev,
            {
              id: data.data.messageId,
              sender: "bot",
              content: data.data.content,
              type: data.data.type,
              timestamp: new Date().toISOString(),
              card: data.data.card,
              carousel: data.data.carousel,
            },
          ]);
          setIsTyping(false);
          if (data.data.quickReplies) {
            setQuickReplies(data.data.quickReplies);
          }
          break;

        case "typing":
          setIsTyping(data.data.isTyping);
          break;

        case "lead_created":
          // Handle lead creation
          console.log("Lead created:", data.data);
          break;
      }
    };

    ws.onerror = (error) => {
      console.error("WebSocket error:", error);
    };

    ws.onclose = () => {
      console.log("Chatbot WebSocket disconnected");
    };

    return () => {
      ws.close();
    };
  }, []);

  const sendMessage = (content: string) => {
    if (!wsRef.current) return;

    const message: ChatMessage = {
      id: Date.now().toString(),
      sender: "user",
      content,
      type: "text",
      timestamp: new Date().toISOString(),
      status: "sent",
    };

    setMessages((prev) => [...prev, message]);
    setQuickReplies([]);

    wsRef.current.send(
      JSON.stringify({
        event: "message",
        data: {
          conversationId: conversationIdRef.current,
          content,
          metadata: {
            source: "widget",
          },
        },
      }),
    );

    setIsTyping(true);
  };

  const sendQuickReply = (reply: string) => {
    sendMessage(reply);
  };

  return {
    messages,
    isTyping,
    quickReplies,
    unreadCount,
    sendMessage,
    sendQuickReply,
  };
}
```

---

### PASO 8: chatbotService API Client

```typescript
// filepath: src/lib/services/chatbotService.ts
import { apiClient } from "./apiClient";
import type {
  Conversation,
  ChatMessage,
  LeadScore,
  ChatbotStats,
} from "@/types/chatbot";

export const chatbotService = {
  async getConversations() {
    const { data } = await apiClient.get<Conversation[]>(
      "/chatbot/conversations",
    );
    return data;
  },

  async getConversation(id: string) {
    const { data } = await apiClient.get<Conversation>(
      `/chatbot/conversations/${id}`,
    );
    return data;
  },

  async deleteConversation(id: string) {
    await apiClient.delete(`/chatbot/conversations/${id}`);
  },

  async sendMessage(
    conversationId: string | null,
    content: string,
    metadata?: any,
  ) {
    const { data } = await apiClient.post("/chatbot/message", {
      conversationId,
      content,
      metadata,
    });
    return data;
  },

  async transferToAgent(conversationId: string, reason: string) {
    await apiClient.post("/chatbot/transfer-to-agent", {
      conversationId,
      reason,
    });
  },

  async createLead(conversationId: string) {
    const { data } = await apiClient.post("/chatbot/lead", {
      conversationId,
    });
    return data;
  },

  async getLeadScore(conversationId: string) {
    const { data } = await apiClient.get<LeadScore>(
      `/chatbot/lead-score/${conversationId}`,
    );
    return data;
  },

  async getStats() {
    const { data } = await apiClient.get<ChatbotStats>("/chatbot/stats");
    return data;
  },
};
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 9: Tipos de Chatbot

```typescript
// filepath: src/types/chatbot.ts
export type MessageSender = "user" | "bot" | "agent";
export type MessageType = "text" | "card" | "carousel" | "quick_replies";
export type MessageStatus = "sent" | "delivered" | "read";
export type LeadClassification = "HOT" | "WARM" | "COLD";

export interface ChatMessage {
  id: string;
  sender: MessageSender;
  content: string;
  type: MessageType;
  timestamp: string;
  status?: MessageStatus;
  card?: VehicleCard;
  carousel?: VehicleCard[];
  quickReplies?: string[];
}

export interface VehicleCard {
  id: string;
  name: string;
  image: string;
  price: string;
  url: string;
}

export interface Conversation {
  id: string;
  userId: string | null;
  source: "widget" | "whatsapp" | "page";
  messages: ChatMessage[];
  leadScore?: LeadScore;
  status: "active" | "closed" | "transferred";
  createdAt: string;
  updatedAt: string;
}

export interface LeadScore {
  conversationId: string;
  score: number; // 0-100
  classification: LeadClassification;
  confidence: number; // 0-1
  factors: {
    behavioral: number;
    intent: number;
    profile: number;
    timing: number;
  };
  urgency: "high" | "medium" | "low";
  recommendations: string[];
}

export interface ChatbotStats {
  totalConversations: number;
  activeConversations: number;
  averageResponseTime: number; // seconds
  satisfactionRate: number; // 0-1
  leadConversionRate: number; // 0-1
  topIntents: Array<{
    intent: string;
    count: number;
  }>;
  leadDistribution: {
    hot: number;
    warm: number;
    cold: number;
  };
}

export interface QuickReply {
  text: string;
  payload?: string;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - Widget flotante aparece en bottom-right
# - Auto-open después de 30 segundos
# - WebSocket conecta correctamente
# - Mensajes se envían y reciben en tiempo real
# - Typing indicator aparece cuando bot está escribiendo
# - Quick replies aparecen y funcionan
# - Cards de vehículos se renderizan correctamente
# - Carousel de vehículos funciona (scroll horizontal)
# - Lead scoring calcula correctamente (HOT/WARM/COLD)
# - WhatsApp webhook procesa mensajes
# - Notificación de HOT lead se envía por WhatsApp
# - Transfer to agent funciona
# - Historial de conversaciones persiste
# - Minimize/maximize widget funciona
# - Mobile responsive
```

---

## 🎯 INTENTS Y RESPUESTAS

### Intents Predefinidos

```typescript
const intents = {
  GREETING: {
    patterns: ["hola", "buenos días", "buenas tardes", "hey"],
    responses: [
      "¡Hola! ¿En qué puedo ayudarte hoy?",
      "¡Buenas! Estoy aquí para ayudarte a encontrar tu vehículo ideal.",
    ],
  },

  VEHICLE_INQUIRY: {
    patterns: ["precio", "cuánto cuesta", "información sobre"],
    action: "fetch_vehicle_details",
    requiresContext: ["vehicleId"],
  },

  TEST_DRIVE: {
    patterns: ["test drive", "probar", "manejar"],
    action: "schedule_test_drive",
    leadScore: +15,
  },

  FINANCING: {
    patterns: ["financiamiento", "crédito", "banco", "mensualidad"],
    action: "show_financing_options",
    leadScore: +10,
  },

  CONTACT_DEALER: {
    patterns: ["contactar", "hablar con", "vendedor"],
    action: "transfer_to_agent",
    leadScore: +20,
  },
};
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/chatbot.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Chatbot", () => {
  test("debe mostrar widget de chatbot en páginas públicas", async ({
    page,
  }) => {
    await page.goto("/");

    await expect(page.getByTestId("chatbot-widget")).toBeVisible();
  });

  test("debe abrir chat al hacer click en widget", async ({ page }) => {
    await page.goto("/");

    await page.getByTestId("chatbot-widget").click();
    await expect(page.getByTestId("chatbot-window")).toBeVisible();
    await expect(page.getByText(/¿cómo puedo ayudarte/i)).toBeVisible();
  });

  test("debe enviar mensaje y recibir respuesta", async ({ page }) => {
    await page.goto("/");

    await page.getByTestId("chatbot-widget").click();
    await page.fill('input[name="message"]', "Busco un Toyota Camry");
    await page.keyboard.press("Enter");

    await expect(page.getByText(/toyota camry/i)).toBeVisible();
    // Esperar respuesta del bot
    await expect(page.getByTestId("bot-message")).toHaveCount({ min: 1 });
  });

  test("debe mostrar sugerencias rápidas", async ({ page }) => {
    await page.goto("/");

    await page.getByTestId("chatbot-widget").click();
    await expect(page.getByTestId("quick-suggestions")).toBeVisible();
  });

  test("debe transferir a agente humano", async ({ page }) => {
    await page.goto("/");

    await page.getByTestId("chatbot-widget").click();
    await page.fill('input[name="message"]', "Quiero hablar con una persona");
    await page.keyboard.press("Enter");

    await expect(page.getByText(/conectando con un agente/i)).toBeVisible();
  });

  test("debe minimizar y restaurar chat", async ({ page }) => {
    await page.goto("/");

    await page.getByTestId("chatbot-widget").click();
    await page.getByTestId("minimize-chat").click();
    await expect(page.getByTestId("chatbot-window")).not.toBeVisible();

    await page.getByTestId("chatbot-widget").click();
    await expect(page.getByTestId("chatbot-window")).toBeVisible();
  });
});
```

---

## 🚀 MEJORAS FUTURAS

1. **Sentiment Analysis**: Detectar frustración del usuario → Transfer to agent
2. **Voice Support**: Integración con Whisper API para voice messages
3. **Multi-language**: Soporte para inglés, creole
4. **Proactive Messaging**: Mensajes automáticos basados en comportamiento
5. **A/B Testing**: Diferentes personalidades del bot
6. **Analytics Dashboard**: Métricas detalladas de conversaciones

---

**Siguiente documento:** `23-comparador.md` - Comparador de vehículos lado a lado
