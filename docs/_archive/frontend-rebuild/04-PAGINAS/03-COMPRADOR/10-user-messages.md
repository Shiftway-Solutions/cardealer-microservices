---
title: "71 - User Messages Page"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 💬 71 - User Messages Page

**Objetivo:** Sistema de mensajería en tiempo real entre compradores y vendedores.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** MessagingService, WebSocket, AuthStore

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [Tipos](#-tipos)
3. [MessagesPage](#-messagespage)
4. [Hooks](#-hooks)
5. [Servicios](#-servicios)

---

## 🏗️ ARQUITECTURA

```
pages/user/
└── MessagesPage.tsx              # Inbox de mensajes (300 líneas)

hooks/
├── useMessagesInbox.ts           # Lista de conversaciones
└── useChatWindow.ts              # Chat individual

services/
└── messagingService.ts           # API de mensajería

components/organisms/
├── ConversationList.tsx          # Lista de conversaciones
├── ChatWindow.tsx                # Ventana de chat
├── MessageBubble.tsx             # Burbuja de mensaje
└── ChatInput.tsx                 # Input de mensaje
```

---

## 📊 TIPOS

```typescript
// src/types/messaging.ts

export interface Conversation {
  id: string;
  participantId: string;
  participantName: string;
  participantAvatar?: string;
  participantType: "buyer" | "seller" | "dealer";
  vehicleId?: string;
  vehicleTitle?: string;
  vehicleImage?: string;
  lastMessage: string;
  lastMessageAt: string;
  unreadCount: number;
  isOnline: boolean;
}

export interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  content: string;
  type: MessageType;
  attachments?: Attachment[];
  createdAt: string;
  readAt?: string;
  status: MessageStatus;
}

export enum MessageType {
  Text = "text",
  Image = "image",
  Document = "document",
  VehicleCard = "vehicle_card",
  SystemMessage = "system",
}

export enum MessageStatus {
  Sending = "sending",
  Sent = "sent",
  Delivered = "delivered",
  Read = "read",
  Failed = "failed",
}

export interface Attachment {
  id: string;
  type: "image" | "document";
  url: string;
  name: string;
  size: number;
}

export interface SendMessageDto {
  conversationId: string;
  content: string;
  type?: MessageType;
  attachments?: File[];
}

export interface ConversationsResponse {
  conversations: Conversation[];
  totalCount: number;
  unreadTotal: number;
}
```

---

## 💬 MESSAGESPAGE

**Ruta:** `/messages` o `/inbox`

```typescript
// src/pages/user/MessagesPage.tsx
import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import MainLayout from '@/layouts/MainLayout';
import {
  FiMessageSquare, FiSearch, FiSend,
  FiPaperclip, FiImage, FiMoreVertical,
  FiCheck, FiCheckCircle, FiClock
} from 'react-icons/fi';
import { useMessagesInbox } from '@/hooks/useMessagesInbox';
import { useChatWindow } from '@/hooks/useChatWindow';
import { useAuthStore } from '@/store/authStore';
import type { Conversation, Message, MessageStatus } from '@/types/messaging';

export default function MessagesPage() {
  const { t } = useTranslation('messages');
  const { conversationId } = useParams<{ conversationId?: string }>();
  const navigate = useNavigate();
  const currentUserId = useAuthStore((state) => state.user?.id);

  // Conversation list state
  const [searchQuery, setSearchQuery] = useState('');
  const {
    conversations,
    isLoading: loadingConversations,
    unreadTotal,
    refetch: refetchConversations
  } = useMessagesInbox();

  // Active chat state
  const [selectedConversation, setSelectedConversation] = useState<Conversation | null>(null);
  const {
    messages,
    isLoading: loadingMessages,
    sendMessage,
    markAsRead,
  } = useChatWindow(conversationId || null);

  // Message input
  const [messageText, setMessageText] = useState('');
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Set selected conversation from URL param
  useEffect(() => {
    if (conversationId && conversations) {
      const conv = conversations.find((c) => c.id === conversationId);
      if (conv) {
        setSelectedConversation(conv);
        markAsRead(conversationId);
      }
    }
  }, [conversationId, conversations]);

  // Handle conversation selection
  const handleSelectConversation = (conversation: Conversation) => {
    setSelectedConversation(conversation);
    navigate(`/messages/${conversation.id}`);
  };

  // Handle send message
  const handleSendMessage = async () => {
    if (!messageText.trim() || !selectedConversation) return;

    setSending(true);
    try {
      await sendMessage({
        conversationId: selectedConversation.id,
        content: messageText.trim(),
      });
      setMessageText('');
      refetchConversations();
    } catch (error) {
      console.error('Error sending message:', error);
    } finally {
      setSending(false);
    }
  };

  // Handle key press (Enter to send)
  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  // Filter conversations by search
  const filteredConversations = conversations?.filter((conv) =>
    conv.participantName.toLowerCase().includes(searchQuery.toLowerCase()) ||
    conv.vehicleTitle?.toLowerCase().includes(searchQuery.toLowerCase())
  ) || [];

  // Get status icon
  const getStatusIcon = (status: MessageStatus) => {
    switch (status) {
      case MessageStatus.Sending:
        return <FiClock className="text-gray-400" size={14} />;
      case MessageStatus.Sent:
        return <FiCheck className="text-gray-400" size={14} />;
      case MessageStatus.Delivered:
        return <FiCheckCircle className="text-gray-400" size={14} />;
      case MessageStatus.Read:
        return <FiCheckCircle className="text-blue-500" size={14} />;
      default:
        return null;
    }
  };

  return (
    <MainLayout>
      <div className="h-[calc(100vh-64px)] flex bg-gray-50">
        {/* Sidebar - Conversation List */}
        <aside className="w-full md:w-96 bg-white border-r flex flex-col">
          {/* Header */}
          <div className="p-4 border-b">
            <div className="flex items-center justify-between mb-4">
              <h1 className="text-xl font-bold text-gray-900">
                {t('messages.title', 'Mensajes')}
              </h1>
              {unreadTotal > 0 && (
                <span className="bg-blue-600 text-white text-xs font-bold px-2 py-1 rounded-full">
                  {unreadTotal}
                </span>
              )}
            </div>

            {/* Search */}
            <div className="relative">
              <FiSearch className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder={t('messages.search', 'Buscar conversaciones...')}
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {/* Conversation List */}
          <div className="flex-1 overflow-y-auto">
            {loadingConversations ? (
              <ConversationListSkeleton />
            ) : filteredConversations.length === 0 ? (
              <EmptyConversations />
            ) : (
              filteredConversations.map((conversation) => (
                <ConversationItem
                  key={conversation.id}
                  conversation={conversation}
                  isActive={selectedConversation?.id === conversation.id}
                  onClick={() => handleSelectConversation(conversation)}
                />
              ))
            )}
          </div>
        </aside>

        {/* Main Chat Area */}
        <main className="hidden md:flex flex-col flex-1">
          {selectedConversation ? (
            <>
              {/* Chat Header */}
              <div className="h-16 bg-white border-b px-6 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="relative">
                    <img
                      src={selectedConversation.participantAvatar || '/default-avatar.png'}
                      alt={selectedConversation.participantName}
                      className="w-10 h-10 rounded-full object-cover"
                    />
                    {selectedConversation.isOnline && (
                      <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full" />
                    )}
                  </div>
                  <div>
                    <h2 className="font-semibold text-gray-900">
                      {selectedConversation.participantName}
                    </h2>
                    {selectedConversation.vehicleTitle && (
                      <p className="text-sm text-gray-500">
                        {selectedConversation.vehicleTitle}
                      </p>
                    )}
                  </div>
                </div>
                <button className="p-2 hover:bg-gray-100 rounded-lg">
                  <FiMoreVertical size={20} />
                </button>
              </div>

              {/* Messages */}
              <div className="flex-1 overflow-y-auto p-6 space-y-4">
                {loadingMessages ? (
                  <MessagesSkeleton />
                ) : (
                  messages.map((message) => (
                    <MessageBubble
                      key={message.id}
                      message={message}
                      isOwn={message.senderId === currentUserId}
                      statusIcon={getStatusIcon(message.status)}
                    />
                  ))
                )}
                <div ref={messagesEndRef} />
              </div>

              {/* Message Input */}
              <div className="bg-white border-t p-4">
                <div className="flex items-end gap-3">
                  <button className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg">
                    <FiPaperclip size={20} />
                  </button>
                  <button className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg">
                    <FiImage size={20} />
                  </button>
                  <div className="flex-1">
                    <textarea
                      value={messageText}
                      onChange={(e) => setMessageText(e.target.value)}
                      onKeyPress={handleKeyPress}
                      placeholder={t('messages.placeholder', 'Escribe un mensaje...')}
                      rows={1}
                      className="w-full px-4 py-2 border rounded-lg resize-none focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                  <button
                    onClick={handleSendMessage}
                    disabled={!messageText.trim() || sending}
                    className="p-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <FiSend size={20} />
                  </button>
                </div>
              </div>
            </>
          ) : (
            <EmptyChatPlaceholder />
          )}
        </main>
      </div>
    </MainLayout>
  );
}

// Sub-components

function ConversationItem({ conversation, isActive, onClick }: {
  conversation: Conversation;
  isActive: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`w-full p-4 flex items-start gap-3 hover:bg-gray-50 transition-colors ${
        isActive ? 'bg-blue-50 border-l-4 border-blue-600' : ''
      }`}
    >
      <div className="relative flex-shrink-0">
        <img
          src={conversation.participantAvatar || '/default-avatar.png'}
          alt={conversation.participantName}
          className="w-12 h-12 rounded-full object-cover"
        />
        {conversation.isOnline && (
          <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full" />
        )}
      </div>
      <div className="flex-1 min-w-0 text-left">
        <div className="flex items-center justify-between mb-1">
          <h4 className="font-medium text-gray-900 truncate">
            {conversation.participantName}
          </h4>
          <span className="text-xs text-gray-500">
            {formatTimeAgo(conversation.lastMessageAt)}
          </span>
        </div>
        {conversation.vehicleTitle && (
          <p className="text-xs text-blue-600 truncate mb-1">
            {conversation.vehicleTitle}
          </p>
        )}
        <div className="flex items-center justify-between">
          <p className="text-sm text-gray-500 truncate">
            {conversation.lastMessage}
          </p>
          {conversation.unreadCount > 0 && (
            <span className="ml-2 bg-blue-600 text-white text-xs font-bold px-2 py-0.5 rounded-full">
              {conversation.unreadCount}
            </span>
          )}
        </div>
      </div>
    </button>
  );
}

function MessageBubble({ message, isOwn, statusIcon }: {
  message: Message;
  isOwn: boolean;
  statusIcon?: React.ReactNode;
}) {
  return (
    <div className={`flex ${isOwn ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[70%] rounded-2xl px-4 py-2 ${
          isOwn
            ? 'bg-blue-600 text-white rounded-br-md'
            : 'bg-gray-100 text-gray-900 rounded-bl-md'
        }`}
      >
        <p className="whitespace-pre-wrap">{message.content}</p>
        <div className={`flex items-center justify-end gap-1 mt-1 ${
          isOwn ? 'text-blue-100' : 'text-gray-400'
        }`}>
          <span className="text-xs">
            {new Date(message.createdAt).toLocaleTimeString([], {
              hour: '2-digit',
              minute: '2-digit',
            })}
          </span>
          {isOwn && statusIcon}
        </div>
      </div>
    </div>
  );
}

function EmptyChatPlaceholder() {
  return (
    <div className="flex-1 flex items-center justify-center bg-gray-50">
      <div className="text-center">
        <div className="w-16 h-16 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
          <FiMessageSquare size={32} className="text-gray-400" />
        </div>
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          Selecciona una conversación
        </h3>
        <p className="text-gray-500">
          Elige una conversación del panel izquierdo para comenzar
        </p>
      </div>
    </div>
  );
}

function EmptyConversations() {
  return (
    <div className="flex-1 flex items-center justify-center p-8">
      <div className="text-center">
        <FiMessageSquare size={48} className="text-gray-300 mx-auto mb-4" />
        <h3 className="font-medium text-gray-900 mb-2">Sin conversaciones</h3>
        <p className="text-sm text-gray-500">
          Cuando contactes a un vendedor, la conversación aparecerá aquí
        </p>
      </div>
    </div>
  );
}

// Helper
function formatTimeAgo(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'ahora';
  if (diffMins < 60) return `${diffMins}m`;
  if (diffHours < 24) return `${diffHours}h`;
  if (diffDays < 7) return `${diffDays}d`;
  return date.toLocaleDateString();
}

// Skeletons
function ConversationListSkeleton() {
  return (
    <div className="space-y-4 p-4">
      {[...Array(5)].map((_, i) => (
        <div key={i} className="flex gap-3 animate-pulse">
          <div className="w-12 h-12 bg-gray-200 rounded-full" />
          <div className="flex-1 space-y-2">
            <div className="h-4 bg-gray-200 rounded w-3/4" />
            <div className="h-3 bg-gray-200 rounded w-1/2" />
          </div>
        </div>
      ))}
    </div>
  );
}

function MessagesSkeleton() {
  return (
    <div className="space-y-4">
      {[...Array(6)].map((_, i) => (
        <div key={i} className={`flex ${i % 2 === 0 ? 'justify-start' : 'justify-end'}`}>
          <div className={`h-12 bg-gray-200 rounded-2xl animate-pulse ${
            i % 2 === 0 ? 'w-48' : 'w-64'
          }`} />
        </div>
      ))}
    </div>
  );
}
```

---

## 🪝 HOOKS

### useMessagesInbox

```typescript
// src/hooks/useMessagesInbox.ts
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { messagingService } from "@/services/messagingService";
import type { ConversationsResponse } from "@/types/messaging";

export function useMessagesInbox() {
  const queryClient = useQueryClient();

  const { data, isLoading, error, refetch } = useQuery<ConversationsResponse>({
    queryKey: ["conversations"],
    queryFn: messagingService.getConversations,
    refetchInterval: 30000, // Poll every 30 seconds
  });

  return {
    conversations: data?.conversations || [],
    totalCount: data?.totalCount || 0,
    unreadTotal: data?.unreadTotal || 0,
    isLoading,
    error,
    refetch,
  };
}
```

### useChatWindow

```typescript
// src/hooks/useChatWindow.ts
import { useState, useEffect, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { messagingService } from "@/services/messagingService";
import type { Message, SendMessageDto } from "@/types/messaging";

export function useChatWindow(conversationId: string | null) {
  const queryClient = useQueryClient();

  // Fetch messages
  const {
    data: messages = [],
    isLoading,
    refetch,
  } = useQuery<Message[]>({
    queryKey: ["messages", conversationId],
    queryFn: () => messagingService.getMessages(conversationId!),
    enabled: !!conversationId,
    refetchInterval: 5000, // Poll every 5 seconds
  });

  // Send message mutation
  const sendMutation = useMutation({
    mutationFn: (dto: SendMessageDto) => messagingService.sendMessage(dto),
    onSuccess: (newMessage) => {
      queryClient.setQueryData<Message[]>(
        ["messages", conversationId],
        (old = []) => [...old, newMessage],
      );
      queryClient.invalidateQueries({ queryKey: ["conversations"] });
    },
  });

  // Mark as read
  const markAsRead = useCallback(
    async (convId: string) => {
      try {
        await messagingService.markAsRead(convId);
        queryClient.invalidateQueries({ queryKey: ["conversations"] });
      } catch (error) {
        console.error("Error marking as read:", error);
      }
    },
    [queryClient],
  );

  return {
    messages,
    isLoading,
    sendMessage: sendMutation.mutateAsync,
    markAsRead,
    refetch,
  };
}
```

---

## 🔧 SERVICIOS

```typescript
// src/services/messagingService.ts
import api from "./api";
import type {
  Conversation,
  Message,
  SendMessageDto,
  ConversationsResponse,
} from "@/types/messaging";

export const messagingService = {
  // Get all conversations
  getConversations: async (): Promise<ConversationsResponse> => {
    const response = await api.get("/api/messages/conversations");
    return response.data;
  },

  // Get messages for a conversation
  getMessages: async (conversationId: string): Promise<Message[]> => {
    const response = await api.get(
      `/api/messages/conversations/${conversationId}`,
    );
    return response.data;
  },

  // Send a message
  sendMessage: async (dto: SendMessageDto): Promise<Message> => {
    const response = await api.post("/api/messages/send", dto);
    return response.data;
  },

  // Mark conversation as read
  markAsRead: async (conversationId: string): Promise<void> => {
    await api.post(`/api/messages/conversations/${conversationId}/read`);
  },

  // Start new conversation (contact seller)
  startConversation: async (
    sellerId: string,
    vehicleId?: string,
    message?: string,
  ): Promise<Conversation> => {
    const response = await api.post("/api/messages/conversations", {
      participantId: sellerId,
      vehicleId,
      initialMessage: message,
    });
    return response.data;
  },

  // Delete conversation
  deleteConversation: async (conversationId: string): Promise<void> => {
    await api.delete(`/api/messages/conversations/${conversationId}`);
  },

  // Upload attachment
  uploadAttachment: async (
    conversationId: string,
    file: File,
  ): Promise<string> => {
    const formData = new FormData();
    formData.append("file", file);
    const response = await api.post(
      `/api/messages/conversations/${conversationId}/attachments`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data.url;
  },
};
```

---

## ✅ VALIDACIÓN

### MessagesPage

- [ ] Lista de conversaciones carga
- [ ] Búsqueda filtra conversaciones
- [ ] Click en conversación la selecciona
- [ ] URL cambia a /messages/{id}
- [ ] Indicador online visible
- [ ] Badge de unread count visible
- [ ] Chat header muestra participante
- [ ] Mensajes se cargan correctamente
- [ ] Mensajes propios a la derecha (azul)
- [ ] Mensajes recibidos a la izquierda (gris)
- [ ] Scroll automático al fondo
- [ ] Input de mensaje funciona
- [ ] Enter envía mensaje
- [ ] Botón enviar funciona
- [ ] Status icons (sent, delivered, read)
- [ ] Empty state cuando no hay conversaciones
- [ ] Empty state cuando no hay chat seleccionado
- [ ] Responsive: móvil muestra solo lista o chat

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/user-messages.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("User Messages", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar inbox de mensajes", async ({ page }) => {
    await page.goto("/messages");

    await expect(
      page.getByRole("heading", { name: /mensajes/i }),
    ).toBeVisible();
    await expect(page.getByTestId("conversations-list")).toBeVisible();
  });

  test("debe abrir conversación específica", async ({ page }) => {
    await page.goto("/messages/conv123");

    await expect(page.getByTestId("chat-messages")).toBeVisible();
  });

  test("debe enviar nuevo mensaje", async ({ page }) => {
    await page.goto("/messages/conv123");

    await page.fill(
      'textarea[name="message"]',
      "Hola, me interesa el vehículo",
    );
    await page.click('button[type="submit"]');

    await expect(page.getByText(/Hola, me interesa/)).toBeVisible();
  });

  test("debe mostrar badge de no leídos", async ({ page }) => {
    await page.goto("/messages");

    const unreadBadge = page.getByTestId("unread-badge");
    if (await unreadBadge.first().isVisible()) {
      await expect(unreadBadge.first()).toContainText(/\d+/);
    }
  });

  test("debe marcar como leído al abrir", async ({ page }) => {
    await page.goto("/messages");

    const unreadItem = page.getByTestId("conversation-unread").first();
    if (await unreadItem.isVisible()) {
      await unreadItem.click();
      await page.goBack();
      // El item ya no debería estar marcado como no leído
    }
  });
});
```

---

## 🔗 RUTAS

```typescript
// src/App.tsx
<Route path="/messages" element={<ProtectedRoute><MessagesPage /></ProtectedRoute>} />
<Route path="/messages/:conversationId" element={<ProtectedRoute><MessagesPage /></ProtectedRoute>} />
<Route path="/inbox" element={<Navigate to="/messages" replace />} />
```

---

_Última actualización: Enero 2026_
