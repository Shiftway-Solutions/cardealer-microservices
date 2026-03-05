# 🔌 Real-time & WebSockets

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** API configurada con WebSocket support
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Implementar funcionalidades real-time en OKLA:

- **Chat en vivo** entre comprador y vendedor
- **Notificaciones push** en tiempo real
- **Actualizaciones de estado** de vehículos
- **Indicadores de presencia** (usuario en línea)
- **Typing indicators** en chat

---

## 🎯 ARQUITECTURA REAL-TIME

```
┌─────────────────┐     WebSocket     ┌─────────────────┐
│   Frontend      │◄──────────────────►│   Gateway       │
│   (Socket.io)   │                    │   (SignalR/WS)  │
└─────────────────┘                    └────────┬────────┘
                                                │
                    ┌───────────────────────────┼───────────────────────────┐
                    │                           │                           │
              ┌─────▼─────┐             ┌───────▼───────┐           ┌───────▼───────┐
              │   Chat    │             │ Notification  │           │   Presence    │
              │  Service  │             │   Service     │           │   Service     │
              └───────────┘             └───────────────┘           └───────────────┘
```

---

## 🔧 PASO 1: Cliente WebSocket Base

```typescript
// filepath: src/lib/websocket/client.ts
import { io, Socket } from "socket.io-client";
import { env } from "@/lib/env";

type SocketCallback = (...args: any[]) => void;

class WebSocketClient {
  private socket: Socket | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private listeners: Map<string, Set<SocketCallback>> = new Map();

  // Conectar al servidor
  connect(token: string): Promise<void> {
    return new Promise((resolve, reject) => {
      if (this.socket?.connected) {
        resolve();
        return;
      }

      this.socket = io(env.wsUrl || env.apiUrl, {
        auth: { token },
        transports: ["websocket", "polling"],
        reconnection: true,
        reconnectionAttempts: this.maxReconnectAttempts,
        reconnectionDelay: 1000,
        reconnectionDelayMax: 5000,
        timeout: 20000,
      });

      this.socket.on("connect", () => {
        console.log("[WS] Connected:", this.socket?.id);
        this.reconnectAttempts = 0;
        resolve();
      });

      this.socket.on("disconnect", (reason) => {
        console.log("[WS] Disconnected:", reason);
      });

      this.socket.on("connect_error", (error) => {
        console.error("[WS] Connection error:", error.message);
        this.reconnectAttempts++;
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
          reject(new Error("Max reconnection attempts reached"));
        }
      });

      // Re-emitir eventos a los listeners registrados
      this.socket.onAny((event, ...args) => {
        const callbacks = this.listeners.get(event);
        if (callbacks) {
          callbacks.forEach((cb) => cb(...args));
        }
      });
    });
  }

  // Desconectar
  disconnect(): void {
    if (this.socket) {
      this.socket.disconnect();
      this.socket = null;
    }
    this.listeners.clear();
  }

  // Suscribirse a un evento
  on(event: string, callback: SocketCallback): () => void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(callback);

    // Retornar función de cleanup
    return () => {
      this.listeners.get(event)?.delete(callback);
    };
  }

  // Emitir evento
  emit(event: string, data?: any): void {
    if (!this.socket?.connected) {
      console.warn("[WS] Not connected, cannot emit:", event);
      return;
    }
    this.socket.emit(event, data);
  }

  // Emitir con acknowledgment
  emitAsync<T>(event: string, data?: any): Promise<T> {
    return new Promise((resolve, reject) => {
      if (!this.socket?.connected) {
        reject(new Error("Not connected"));
        return;
      }
      this.socket.emit(event, data, (response: T) => {
        resolve(response);
      });
    });
  }

  // Estado de conexión
  get isConnected(): boolean {
    return this.socket?.connected ?? false;
  }
}

// Singleton
export const wsClient = new WebSocketClient();
```

---

## 🔧 PASO 2: Provider de WebSocket

```typescript
// filepath: src/providers/WebSocketProvider.tsx
'use client';

import * as React from 'react';
import { wsClient } from '@/lib/websocket/client';
import { useAuth } from '@/hooks/useAuth';

interface WebSocketContextValue {
  isConnected: boolean;
  connect: () => Promise<void>;
  disconnect: () => void;
}

const WebSocketContext = React.createContext<WebSocketContextValue | null>(null);

export function WebSocketProvider({ children }: { children: React.ReactNode }) {
  const { token, isAuthenticated } = useAuth();
  const [isConnected, setIsConnected] = React.useState(false);

  const connect = React.useCallback(async () => {
    if (!token) return;
    try {
      await wsClient.connect(token);
      setIsConnected(true);
    } catch (error) {
      console.error('Failed to connect WebSocket:', error);
    }
  }, [token]);

  const disconnect = React.useCallback(() => {
    wsClient.disconnect();
    setIsConnected(false);
  }, []);

  // Auto-connect cuando hay autenticación
  React.useEffect(() => {
    if (isAuthenticated && token) {
      connect();
    } else {
      disconnect();
    }

    return () => {
      disconnect();
    };
  }, [isAuthenticated, token, connect, disconnect]);

  // Escuchar cambios de conexión
  React.useEffect(() => {
    const unsubConnect = wsClient.on('connect', () => setIsConnected(true));
    const unsubDisconnect = wsClient.on('disconnect', () => setIsConnected(false));

    return () => {
      unsubConnect();
      unsubDisconnect();
    };
  }, []);

  return (
    <WebSocketContext.Provider value={{ isConnected, connect, disconnect }}>
      {children}
    </WebSocketContext.Provider>
  );
}

export function useWebSocket() {
  const context = React.useContext(WebSocketContext);
  if (!context) {
    throw new Error('useWebSocket must be used within WebSocketProvider');
  }
  return context;
}
```

---

## 🔧 PASO 3: Hook de Chat

```typescript
// filepath: src/hooks/useChat.ts
"use client";

import * as React from "react";
import { wsClient } from "@/lib/websocket/client";
import { useWebSocket } from "@/providers/WebSocketProvider";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { chatService } from "@/services/chatService";

interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  content: string;
  type: "text" | "image" | "file";
  createdAt: string;
  readAt?: string;
}

interface UseChat {
  messages: Message[];
  isLoading: boolean;
  sendMessage: (content: string) => void;
  isSending: boolean;
  isTyping: boolean;
  markAsRead: (messageId: string) => void;
}

export function useChat(conversationId: string): UseChat {
  const { isConnected } = useWebSocket();
  const queryClient = useQueryClient();
  const [isTyping, setIsTyping] = React.useState(false);
  const typingTimeoutRef = React.useRef<NodeJS.Timeout>();

  // Cargar mensajes iniciales
  const { data: messages = [], isLoading } = useQuery({
    queryKey: ["chat", "messages", conversationId],
    queryFn: () => chatService.getMessages(conversationId),
    enabled: !!conversationId,
  });

  // Enviar mensaje
  const sendMutation = useMutation({
    mutationFn: (content: string) =>
      chatService.sendMessage(conversationId, content),
    onSuccess: (newMessage) => {
      // Agregar mensaje al cache
      queryClient.setQueryData<Message[]>(
        ["chat", "messages", conversationId],
        (old = []) => [...old, newMessage],
      );
    },
  });

  // Unirse a la sala de chat
  React.useEffect(() => {
    if (!isConnected || !conversationId) return;

    wsClient.emit("chat:join", { conversationId });

    return () => {
      wsClient.emit("chat:leave", { conversationId });
    };
  }, [isConnected, conversationId]);

  // Escuchar nuevos mensajes
  React.useEffect(() => {
    if (!isConnected) return;

    const unsubNewMessage = wsClient.on("chat:message", (message: Message) => {
      if (message.conversationId === conversationId) {
        queryClient.setQueryData<Message[]>(
          ["chat", "messages", conversationId],
          (old = []) => [...old, message],
        );
      }
    });

    const unsubTyping = wsClient.on(
      "chat:typing",
      ({ conversationId: convId, isTyping: typing }) => {
        if (convId === conversationId) {
          setIsTyping(typing);

          // Auto-clear typing después de 3 segundos
          if (typingTimeoutRef.current) {
            clearTimeout(typingTimeoutRef.current);
          }
          if (typing) {
            typingTimeoutRef.current = setTimeout(
              () => setIsTyping(false),
              3000,
            );
          }
        }
      },
    );

    const unsubRead = wsClient.on("chat:read", ({ messageId }) => {
      queryClient.setQueryData<Message[]>(
        ["chat", "messages", conversationId],
        (old = []) =>
          old.map((msg) =>
            msg.id === messageId
              ? { ...msg, readAt: new Date().toISOString() }
              : msg,
          ),
      );
    });

    return () => {
      unsubNewMessage();
      unsubTyping();
      unsubRead();
    };
  }, [isConnected, conversationId, queryClient]);

  const sendMessage = React.useCallback(
    (content: string) => {
      sendMutation.mutate(content);
      wsClient.emit("chat:message", { conversationId, content });
    },
    [conversationId, sendMutation],
  );

  const markAsRead = React.useCallback(
    (messageId: string) => {
      wsClient.emit("chat:read", { conversationId, messageId });
    },
    [conversationId],
  );

  return {
    messages,
    isLoading,
    sendMessage,
    isSending: sendMutation.isPending,
    isTyping,
    markAsRead,
  };
}
```

---

## 🔧 PASO 4: Hook de Notificaciones Real-time

```typescript
// filepath: src/hooks/useNotifications.ts
"use client";

import * as React from "react";
import { wsClient } from "@/lib/websocket/client";
import { useWebSocket } from "@/providers/WebSocketProvider";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

interface Notification {
  id: string;
  type: "info" | "success" | "warning" | "error";
  title: string;
  message: string;
  actionUrl?: string;
  createdAt: string;
  readAt?: string;
}

export function useNotifications() {
  const { isConnected } = useWebSocket();
  const queryClient = useQueryClient();
  const [unreadCount, setUnreadCount] = React.useState(0);

  React.useEffect(() => {
    if (!isConnected) return;

    // Suscribirse a notificaciones
    wsClient.emit("notifications:subscribe");

    // Escuchar nuevas notificaciones
    const unsubNew = wsClient.on(
      "notification:new",
      (notification: Notification) => {
        // Actualizar cache de notificaciones
        queryClient.setQueryData<Notification[]>(
          ["notifications"],
          (old = []) => [notification, ...old],
        );

        // Incrementar contador
        setUnreadCount((prev) => prev + 1);

        // Mostrar toast
        showNotificationToast(notification);

        // Reproducir sonido (si está habilitado)
        playNotificationSound();
      },
    );

    // Escuchar contador actualizado
    const unsubCount = wsClient.on("notification:count", (count: number) => {
      setUnreadCount(count);
    });

    return () => {
      unsubNew();
      unsubCount();
    };
  }, [isConnected, queryClient]);

  const markAsRead = React.useCallback((notificationId: string) => {
    wsClient.emit("notification:read", { notificationId });
    setUnreadCount((prev) => Math.max(0, prev - 1));
  }, []);

  const markAllAsRead = React.useCallback(() => {
    wsClient.emit("notification:read-all");
    setUnreadCount(0);
  }, []);

  return {
    unreadCount,
    markAsRead,
    markAllAsRead,
  };
}

function showNotificationToast(notification: Notification) {
  const toastFn = {
    info: toast.info,
    success: toast.success,
    warning: toast.warning,
    error: toast.error,
  }[notification.type];

  toastFn(notification.title, {
    description: notification.message,
    action: notification.actionUrl
      ? {
          label: "Ver",
          onClick: () => (window.location.href = notification.actionUrl!),
        }
      : undefined,
  });
}

function playNotificationSound() {
  try {
    const audio = new Audio("/sounds/notification.mp3");
    audio.volume = 0.5;
    audio.play().catch(() => {
      // Ignorar errores de autoplay
    });
  } catch {
    // Ignorar si el navegador no soporta audio
  }
}
```

---

## 🔧 PASO 5: Hook de Presencia

```typescript
// filepath: src/hooks/usePresence.ts
"use client";

import * as React from "react";
import { wsClient } from "@/lib/websocket/client";
import { useWebSocket } from "@/providers/WebSocketProvider";

interface PresenceState {
  userId: string;
  status: "online" | "away" | "offline";
  lastSeen?: string;
}

export function usePresence(userIds: string[]) {
  const { isConnected } = useWebSocket();
  const [presenceMap, setPresenceMap] = React.useState<
    Map<string, PresenceState>
  >(new Map());

  React.useEffect(() => {
    if (!isConnected || userIds.length === 0) return;

    // Suscribirse a presencia de usuarios
    wsClient.emit("presence:subscribe", { userIds });

    // Escuchar actualizaciones
    const unsubPresence = wsClient.on(
      "presence:update",
      (state: PresenceState) => {
        setPresenceMap((prev) => {
          const next = new Map(prev);
          next.set(state.userId, state);
          return next;
        });
      },
    );

    // Escuchar estado inicial
    const unsubInit = wsClient.on(
      "presence:init",
      (states: PresenceState[]) => {
        setPresenceMap((prev) => {
          const next = new Map(prev);
          states.forEach((state) => next.set(state.userId, state));
          return next;
        });
      },
    );

    return () => {
      unsubPresence();
      unsubInit();
      wsClient.emit("presence:unsubscribe", { userIds });
    };
  }, [isConnected, userIds.join(",")]);

  const getStatus = React.useCallback(
    (userId: string): PresenceState["status"] => {
      return presenceMap.get(userId)?.status ?? "offline";
    },
    [presenceMap],
  );

  const isOnline = React.useCallback(
    (userId: string): boolean => {
      return getStatus(userId) === "online";
    },
    [getStatus],
  );

  return { presenceMap, getStatus, isOnline };
}
```

---

## 🔧 PASO 6: Componente de Chat UI

```typescript
// filepath: src/components/chat/ChatWindow.tsx
'use client';

import * as React from 'react';
import { useChat } from '@/hooks/useChat';
import { useAuth } from '@/hooks/useAuth';
import { formatRelativeTime } from '@/lib/formatters/date';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Send, Loader2 } from 'lucide-react';

interface ChatWindowProps {
  conversationId: string;
  recipientName: string;
  recipientAvatar?: string;
}

export function ChatWindow({
  conversationId,
  recipientName,
  recipientAvatar,
}: ChatWindowProps) {
  const { user } = useAuth();
  const { messages, isLoading, sendMessage, isSending, isTyping, markAsRead } =
    useChat(conversationId);
  const [input, setInput] = React.useState('');
  const messagesEndRef = React.useRef<HTMLDivElement>(null);
  const inputRef = React.useRef<HTMLInputElement>(null);

  // Scroll al último mensaje
  React.useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Marcar mensajes como leídos al visualizar
  React.useEffect(() => {
    const unreadMessages = messages.filter(
      (m) => m.senderId !== user?.id && !m.readAt
    );
    unreadMessages.forEach((m) => markAsRead(m.id));
  }, [messages, user?.id, markAsRead]);

  const handleSend = (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isSending) return;
    sendMessage(input.trim());
    setInput('');
    inputRef.current?.focus();
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="w-8 h-8 animate-spin text-gray-400" />
      </div>
    );
  }

  return (
    <div className="flex flex-col h-[500px] border rounded-lg overflow-hidden">
      {/* Header */}
      <div className="flex items-center gap-3 p-4 border-b bg-gray-50">
        <Avatar>
          <AvatarImage src={recipientAvatar} alt={recipientName} />
          <AvatarFallback>
            {recipientName.charAt(0).toUpperCase()}
          </AvatarFallback>
        </Avatar>
        <div>
          <p className="font-medium">{recipientName}</p>
          {isTyping && (
            <p className="text-sm text-gray-500 animate-pulse">
              Escribiendo...
            </p>
          )}
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.map((message) => (
          <MessageBubble
            key={message.id}
            message={message}
            isOwn={message.senderId === user?.id}
          />
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSend} className="p-4 border-t bg-gray-50">
        <div className="flex gap-2">
          <Input
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Escribe un mensaje..."
            disabled={isSending}
            className="flex-1"
          />
          <Button type="submit" disabled={!input.trim() || isSending}>
            {isSending ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Send className="w-4 h-4" />
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}

function MessageBubble({
  message,
  isOwn,
}: {
  message: { content: string; createdAt: string };
  isOwn: boolean;
}) {
  return (
    <div className={cn('flex', isOwn ? 'justify-end' : 'justify-start')}>
      <div
        className={cn(
          'max-w-[70%] rounded-lg px-4 py-2',
          isOwn
            ? 'bg-primary-600 text-white rounded-br-none'
            : 'bg-gray-100 text-gray-900 rounded-bl-none'
        )}
      >
        <p className="break-words">{message.content}</p>
        <p
          className={cn(
            'text-xs mt-1',
            isOwn ? 'text-primary-200' : 'text-gray-500'
          )}
        >
          {formatRelativeTime(message.createdAt)}
        </p>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 7: Indicador de Conexión

```typescript
// filepath: src/components/ui/ConnectionIndicator.tsx
'use client';

import * as React from 'react';
import { useWebSocket } from '@/providers/WebSocketProvider';
import { Wifi, WifiOff } from 'lucide-react';
import { cn } from '@/lib/utils';

export function ConnectionIndicator() {
  const { isConnected } = useWebSocket();
  const [showReconnecting, setShowReconnecting] = React.useState(false);

  React.useEffect(() => {
    if (!isConnected) {
      const timeout = setTimeout(() => setShowReconnecting(true), 2000);
      return () => clearTimeout(timeout);
    } else {
      setShowReconnecting(false);
    }
  }, [isConnected]);

  if (isConnected) {
    return null; // No mostrar nada si está conectado
  }

  if (!showReconnecting) {
    return null;
  }

  return (
    <div className="fixed bottom-4 left-4 z-50 flex items-center gap-2 bg-yellow-500 text-white px-4 py-2 rounded-lg shadow-lg animate-pulse">
      <WifiOff className="w-4 h-4" />
      <span className="text-sm font-medium">Reconectando...</span>
    </div>
  );
}
```

---

## 🔧 PASO 8: Typing Indicator Hook

```typescript
// filepath: src/hooks/useTypingIndicator.ts
"use client";

import * as React from "react";
import { wsClient } from "@/lib/websocket/client";
import { useWebSocket } from "@/providers/WebSocketProvider";
import { useDebouncedCallback } from "use-debounce";

export function useTypingIndicator(conversationId: string) {
  const { isConnected } = useWebSocket();
  const [isTyping, setIsTyping] = React.useState(false);
  const typingTimeoutRef = React.useRef<NodeJS.Timeout>();

  // Emitir typing con debounce
  const emitTyping = useDebouncedCallback(() => {
    if (!isConnected) return;
    wsClient.emit("chat:typing", { conversationId, isTyping: true });
    setIsTyping(true);

    // Auto-stop después de 2 segundos sin escribir
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }
    typingTimeoutRef.current = setTimeout(() => {
      wsClient.emit("chat:typing", { conversationId, isTyping: false });
      setIsTyping(false);
    }, 2000);
  }, 300);

  const handleInputChange = React.useCallback(() => {
    emitTyping();
  }, [emitTyping]);

  // Cleanup
  React.useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, []);

  return { isTyping, handleInputChange };
}
```

---

## 🔧 PASO 9: Actualizaciones de Vehículos en Tiempo Real

```typescript
// filepath: src/hooks/useVehicleUpdates.ts
"use client";

import * as React from "react";
import { wsClient } from "@/lib/websocket/client";
import { useWebSocket } from "@/providers/WebSocketProvider";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

interface VehicleUpdate {
  vehicleId: string;
  type: "price_change" | "sold" | "status_change";
  data: {
    oldPrice?: number;
    newPrice?: number;
    newStatus?: string;
  };
}

export function useVehicleUpdates(vehicleIds: string[]) {
  const { isConnected } = useWebSocket();
  const queryClient = useQueryClient();

  React.useEffect(() => {
    if (!isConnected || vehicleIds.length === 0) return;

    // Suscribirse a actualizaciones
    wsClient.emit("vehicles:subscribe", { vehicleIds });

    // Escuchar actualizaciones
    const unsub = wsClient.on("vehicle:update", (update: VehicleUpdate) => {
      // Invalidar cache del vehículo
      queryClient.invalidateQueries({
        queryKey: ["vehicle", update.vehicleId],
      });

      // Mostrar notificación según tipo
      switch (update.type) {
        case "price_change":
          toast.info("Precio actualizado", {
            description: `El precio ha cambiado de $${update.data.oldPrice?.toLocaleString()} a $${update.data.newPrice?.toLocaleString()}`,
          });
          break;
        case "sold":
          toast.warning("Vehículo vendido", {
            description: "Este vehículo ya no está disponible.",
          });
          break;
        case "status_change":
          toast.info("Estado actualizado", {
            description: `Nuevo estado: ${update.data.newStatus}`,
          });
          break;
      }
    });

    return () => {
      unsub();
      wsClient.emit("vehicles:unsubscribe", { vehicleIds });
    };
  }, [isConnected, vehicleIds.join(","), queryClient]);
}
```

---

## 🧪 Testing WebSocket

```typescript
// filepath: src/__tests__/websocket.test.ts
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { wsClient } from "@/lib/websocket/client";
import { io } from "socket.io-client";

vi.mock("socket.io-client");

describe("WebSocket Client", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    wsClient.disconnect();
  });

  it("should connect with token", async () => {
    const mockSocket = {
      on: vi.fn((event, callback) => {
        if (event === "connect") {
          setTimeout(callback, 0);
        }
        return mockSocket;
      }),
      onAny: vi.fn(),
      connected: true,
      id: "test-socket-id",
    };

    (io as any).mockReturnValue(mockSocket);

    await wsClient.connect("test-token");

    expect(io).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        auth: { token: "test-token" },
      }),
    );
  });

  it("should emit events when connected", async () => {
    const mockSocket = {
      on: vi.fn((event, callback) => {
        if (event === "connect") {
          setTimeout(callback, 0);
        }
        return mockSocket;
      }),
      onAny: vi.fn(),
      emit: vi.fn(),
      connected: true,
      id: "test-socket-id",
    };

    (io as any).mockReturnValue(mockSocket);

    await wsClient.connect("test-token");
    wsClient.emit("test:event", { data: "test" });

    expect(mockSocket.emit).toHaveBeenCalledWith("test:event", {
      data: "test",
    });
  });
});
```

---

## ✅ Checklist Real-time

### Conexión

- [ ] Implementar cliente WebSocket singleton
- [ ] Crear WebSocketProvider
- [ ] Manejar reconexiones automáticas
- [ ] Mostrar indicador de conexión

### Chat

- [ ] Hook useChat con mensajes y envío
- [ ] Typing indicators
- [ ] Marcar mensajes como leídos
- [ ] UI de chat con bubbles

### Notificaciones

- [ ] Hook useNotifications
- [ ] Toast para nuevas notificaciones
- [ ] Contador de no leídas
- [ ] Sonido de notificación

### Presencia

- [ ] Hook usePresence
- [ ] Indicador online/offline
- [ ] Last seen

### Vehículos

- [ ] Suscripción a cambios
- [ ] Invalidar cache automáticamente
- [ ] Notificar cambios de precio

---

## 🔗 Referencias

- [Socket.io Client](https://socket.io/docs/v4/client-api/)
- [React Query con WebSockets](https://tanstack.com/query/latest/docs/react/guides/websockets)

---

_Las funcionalidades real-time aumentan significativamente el engagement del usuario._
