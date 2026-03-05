# 💬 Consultas y Mensajes

> **Tiempo estimado:** 45 minutos  
> **Páginas:** MyInquiriesPage, ReceivedInquiriesPage, MessagesPage

---

## 📋 OBJETIVO

Sistema de comunicación entre compradores y vendedores:

- Lista de consultas enviadas (comprador)
- Lista de consultas recibidas (vendedor/dealer)
- Chat en tiempo real

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ MIS CONSULTAS                                    [+ Nueva]      │
├─────────────────────────────────────────────────────────────────┤
│ [Enviadas] [Recibidas]                                          │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🚗 Toyota Camry 2024            AutoMax RD                  │ │
│ │ "Hola, ¿está disponible?"       Hace 2 horas   ● Sin leer   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🚗 Honda CR-V 2023              CarWorld                    │ │
│ │ "¿Aceptan financiamiento?"      Ayer           ✓ Respondido │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

### Lista de Consultas Enviadas

```typescript
// filepath: src/app/(buyer)/inquiries/page.tsx
'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { inquiryService } from '@/services/api/inquiryService';
import { formatDistanceToNow } from 'date-fns';
import { es } from 'date-fns/locale';
import { MessageSquare, Car, Clock, ChevronRight } from 'lucide-react';

export default function MyInquiriesPage() {
  const [tab, setTab] = useState<'sent' | 'received'>('sent');

  const { data: inquiries, isLoading } = useQuery({
    queryKey: ['inquiries', tab],
    queryFn: () => inquiryService.getMyInquiries(tab),
  });

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Mis Consultas</h1>
      </div>

      <Tabs value={tab} onValueChange={(v) => setTab(v as 'sent' | 'received')}>
        <TabsList className="mb-6">
          <TabsTrigger value="sent">Enviadas</TabsTrigger>
          <TabsTrigger value="received">Recibidas</TabsTrigger>
        </TabsList>

        <TabsContent value={tab}>
          {isLoading ? (
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-24 w-full" />
              ))}
            </div>
          ) : inquiries?.length === 0 ? (
            <Card>
              <CardContent className="py-12 text-center">
                <MessageSquare className="w-12 h-12 mx-auto text-gray-400 mb-4" />
                <p className="text-gray-600">No tienes consultas {tab === 'sent' ? 'enviadas' : 'recibidas'}</p>
                <Button className="mt-4" asChild>
                  <Link href="/search">Buscar vehículos</Link>
                </Button>
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-4">
              {inquiries?.map((inquiry) => (
                <Link key={inquiry.id} href={`/messages/${inquiry.id}`}>
                  <Card className="hover:shadow-md transition-shadow cursor-pointer">
                    <CardContent className="p-4">
                      <div className="flex items-center gap-4">
                        <div className="w-16 h-16 bg-gray-100 rounded-lg flex items-center justify-center">
                          {inquiry.vehicle?.mainImage ? (
                            <img
                              src={inquiry.vehicle.mainImage}
                              alt=""
                              className="w-full h-full object-cover rounded-lg"
                            />
                          ) : (
                            <Car className="w-8 h-8 text-gray-400" />
                          )}
                        </div>

                        <div className="flex-grow min-w-0">
                          <div className="flex items-center gap-2">
                            <h3 className="font-semibold truncate">
                              {inquiry.vehicle?.title || 'Vehículo'}
                            </h3>
                            {!inquiry.isRead && (
                              <Badge variant="destructive" className="text-xs">
                                Nuevo
                              </Badge>
                            )}
                          </div>
                          <p className="text-sm text-gray-600 truncate">
                            {inquiry.lastMessage}
                          </p>
                          <div className="flex items-center gap-2 mt-1 text-xs text-gray-500">
                            <Clock className="w-3 h-3" />
                            {formatDistanceToNow(new Date(inquiry.updatedAt), {
                              addSuffix: true,
                              locale: es
                            })}
                            <span>•</span>
                            <span>{tab === 'sent' ? inquiry.dealer?.name : inquiry.user?.name}</span>
                          </div>
                        </div>

                        <ChevronRight className="w-5 h-5 text-gray-400 flex-shrink-0" />
                      </div>
                    </CardContent>
                  </Card>
                </Link>
              ))}
            </div>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

### Página de Chat/Mensajes

```typescript
// filepath: src/app/(buyer)/messages/[id]/page.tsx
'use client';

import { useState, useEffect, useRef } from 'react';
import { useParams } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { inquiryService } from '@/services/api/inquiryService';
import { useAuth } from '@/hooks/useAuth';
import { formatDistanceToNow } from 'date-fns';
import { es } from 'date-fns/locale';
import { Send, ArrowLeft, Phone, Car } from 'lucide-react';
import Link from 'next/link';

export default function MessagesPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [message, setMessage] = useState('');

  const { data: inquiry } = useQuery({
    queryKey: ['inquiry', id],
    queryFn: () => inquiryService.getById(id),
  });

  const { data: messages } = useQuery({
    queryKey: ['messages', id],
    queryFn: () => inquiryService.getMessages(id),
    refetchInterval: 10000, // Polling cada 10s
  });

  const sendMutation = useMutation({
    mutationFn: (text: string) => inquiryService.sendMessage(id, text),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['messages', id] });
      setMessage('');
    },
  });

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = (e: React.FormEvent) => {
    e.preventDefault();
    if (message.trim()) {
      sendMutation.mutate(message.trim());
    }
  };

  const otherParty = inquiry?.dealer || inquiry?.user;

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b px-4 py-3 flex items-center gap-4">
        <Link href="/inquiries" className="md:hidden">
          <ArrowLeft className="w-5 h-5" />
        </Link>

        <Avatar>
          <AvatarImage src={otherParty?.avatar} />
          <AvatarFallback>{otherParty?.name?.charAt(0)}</AvatarFallback>
        </Avatar>

        <div className="flex-grow">
          <h2 className="font-semibold">{otherParty?.name}</h2>
          <p className="text-sm text-gray-600">{inquiry?.vehicle?.title}</p>
        </div>

        <div className="flex gap-2">
          {otherParty?.phone && (
            <Button variant="outline" size="icon" asChild>
              <a href={`tel:${otherParty.phone}`}>
                <Phone className="w-4 h-4" />
              </a>
            </Button>
          )}
          <Button variant="outline" size="icon" asChild>
            <Link href={`/vehicles/${inquiry?.vehicle?.slug}`}>
              <Car className="w-4 h-4" />
            </Link>
          </Button>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-grow overflow-y-auto p-4 space-y-4">
        {messages?.map((msg) => {
          const isOwn = msg.senderId === user?.id;
          return (
            <div
              key={msg.id}
              className={`flex ${isOwn ? 'justify-end' : 'justify-start'}`}
            >
              <div
                className={`max-w-[75%] rounded-lg px-4 py-2 ${
                  isOwn
                    ? 'bg-primary-600 text-white'
                    : 'bg-white border shadow-sm'
                }`}
              >
                <p>{msg.text}</p>
                <p className={`text-xs mt-1 ${isOwn ? 'text-white/70' : 'text-gray-500'}`}>
                  {formatDistanceToNow(new Date(msg.createdAt), {
                    addSuffix: true,
                    locale: es
                  })}
                </p>
              </div>
            </div>
          );
        })}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSend} className="bg-white border-t p-4">
        <div className="flex gap-2">
          <Input
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Escribe un mensaje..."
            className="flex-grow"
          />
          <Button type="submit" disabled={!message.trim() || sendMutation.isPending}>
            <Send className="w-4 h-4" />
          </Button>
        </div>
      </form>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint                             | Descripción          |
| ------ | ------------------------------------ | -------------------- |
| `GET`  | `/api/inquiries?type=sent\|received` | Listar consultas     |
| `GET`  | `/api/inquiries/{id}`                | Detalle de consulta  |
| `GET`  | `/api/inquiries/{id}/messages`       | Mensajes de consulta |
| `POST` | `/api/inquiries/{id}/messages`       | Enviar mensaje       |

---

## 🧪 TEST E2E

```typescript
// filepath: e2e/inquiries.spec.ts
import { test, expect } from "@playwright/test";

test("can view and send messages", async ({ page }) => {
  await page.goto("/inquiries");
  await expect(page.getByText("Mis Consultas")).toBeVisible();

  // Click on inquiry
  await page.click('[data-testid="inquiry-item"]');

  // Send message
  await page.fill('input[placeholder*="mensaje"]', "Hola, me interesa");
  await page.click('button[type="submit"]');

  await expect(page.getByText("Hola, me interesa")).toBeVisible();
});
```

---

## ✅ CHECKLIST

- [ ] Lista de consultas enviadas
- [ ] Lista de consultas recibidas
- [ ] Chat con mensajes
- [ ] Polling para nuevos mensajes
- [ ] Indicadores de no leído
- [ ] Mobile responsive

---

_Última actualización: Enero 30, 2026_
