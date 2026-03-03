/**
 * Conversation Detail Page
 *
 * Individual chat/conversation view
 */

'use client';

import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import {
  ArrowLeft,
  Send,
  Phone,
  MoreVertical,
  Car,
  Calendar,
  MapPin,
  AlertCircle,
  CheckCheck,
  Check,
  Image as ImageIcon,
  Paperclip,
  Smile,
} from 'lucide-react';
import Link from 'next/link';

// Mock conversation data
const conversation = {
  id: 'conv-1',
  contact: {
    name: 'Juan Pérez',
    phone: '809-555-1234',
    email: 'juan@email.com',
    initials: 'JP',
    isOnline: true,
  },
  vehicle: {
    id: 'v-123',
    title: 'Toyota Camry 2023',
    price: 2150000,
    image: null,
    slug: 'toyota-camry-2023',
  },
  messages: [
    {
      id: 1,
      sender: 'them',
      content: 'Hola, me interesa el Toyota Camry que tienes publicado. ¿Está disponible todavía?',
      timestamp: '10:30 AM',
      status: 'read',
    },
    {
      id: 2,
      sender: 'me',
      content:
        '¡Hola Juan! Sí, el vehículo está disponible. ¿Te gustaría agendar una cita para verlo?',
      timestamp: '10:32 AM',
      status: 'read',
    },
    {
      id: 3,
      sender: 'them',
      content:
        'Sí, me gustaría verlo este fin de semana si es posible. ¿Tienen disponibilidad el sábado en la mañana?',
      timestamp: '10:35 AM',
      status: 'read',
    },
    {
      id: 4,
      sender: 'me',
      content:
        'Claro que sí. Tenemos disponibilidad el sábado de 9am a 1pm. ¿A qué hora le conviene?',
      timestamp: '10:36 AM',
      status: 'read',
    },
    {
      id: 5,
      sender: 'them',
      content: 'Perfecto, ¿podría ser a las 10am?',
      timestamp: '10:38 AM',
      status: 'read',
    },
    {
      id: 6,
      sender: 'me',
      content:
        'Excelente, quedamos para el sábado a las 10am. Nuestra dirección es Av. Winston Churchill #123, Plaza Ágora. Te esperamos.',
      timestamp: '10:40 AM',
      status: 'delivered',
    },
  ],
};

export default function ConversationPage() {
  const [newMessage, setNewMessage] = useState('');
  const [isSending, setIsSending] = useState(false);

  const handleSend = () => {
    if (!newMessage.trim()) return;
    setIsSending(true);
    setTimeout(() => {
      setIsSending(false);
      setNewMessage('');
    }, 500);
  };

  return (
    <div className="bg-muted/50 flex min-h-screen flex-col">
      {/* Header */}
      <div className="border-border bg-card sticky top-0 z-10 border-b">
        <div className="mx-auto max-w-4xl px-4 py-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Link href="/cuenta/mensajes">
                <Button variant="ghost" size="icon">
                  <ArrowLeft className="h-5 w-5" />
                </Button>
              </Link>

              <div className="flex items-center gap-3">
                <div className="relative">
                  <Avatar className="h-10 w-10">
                    <AvatarFallback className="bg-primary/10 text-primary">
                      {conversation.contact.initials}
                    </AvatarFallback>
                  </Avatar>
                  {conversation.contact.isOnline && (
                    <div className="absolute right-0 bottom-0 h-3 w-3 rounded-full border-2 border-white bg-primary/100" />
                  )}
                </div>
                <div>
                  <h2 className="font-semibold">{conversation.contact.name}</h2>
                  <p className="text-muted-foreground text-xs">
                    {conversation.contact.isOnline ? 'En línea' : 'Última vez hace 2h'}
                  </p>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Button variant="ghost" size="icon" className="text-primary">
                <Phone className="h-5 w-5" />
              </Button>
              <Button variant="ghost" size="icon">
                <MoreVertical className="h-5 w-5" />
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Vehicle Context */}
      <div className="border-border bg-card border-b">
        <div className="mx-auto max-w-4xl px-4 py-3">
          <Link href={`/vehiculos/${conversation.vehicle.slug}`}>
            <div className="bg-muted/50 hover:bg-muted flex items-center gap-3 rounded-lg p-3 transition-colors">
              <div className="bg-muted flex h-12 w-16 items-center justify-center rounded">
                <Car className="text-muted-foreground h-6 w-6" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium">{conversation.vehicle.title}</p>
                <p className="text-sm font-semibold text-primary">
                  RD$ {conversation.vehicle.price.toLocaleString()}
                </p>
              </div>
              <Badge variant="secondary" className="shrink-0">
                Conversación sobre
              </Badge>
            </div>
          </Link>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto">
        <div className="mx-auto max-w-4xl space-y-4 px-4 py-6">
          {/* Date separator */}
          <div className="flex items-center gap-4">
            <div className="bg-muted h-px flex-1" />
            <span className="text-muted-foreground text-xs">Hoy</span>
            <div className="bg-muted h-px flex-1" />
          </div>

          {conversation.messages.map(message => (
            <div
              key={message.id}
              className={`flex ${message.sender === 'me' ? 'justify-end' : 'justify-start'}`}
            >
              <div
                className={`max-w-[75%] rounded-2xl px-4 py-2 ${
                  message.sender === 'me'
                    ? 'rounded-br-md bg-primary text-white'
                    : 'bg-card rounded-bl-md border'
                }`}
              >
                <p className="text-sm">{message.content}</p>
                <div
                  className={`mt-1 flex items-center justify-end gap-1 ${
                    message.sender === 'me' ? 'text-primary-foreground' : 'text-muted-foreground'
                  }`}
                >
                  <span className="text-xs">{message.timestamp}</span>
                  {message.sender === 'me' &&
                    (message.status === 'read' ? (
                      <CheckCheck className="h-3 w-3 text-blue-300" />
                    ) : (
                      <Check className="h-3 w-3" />
                    ))}
                </div>
              </div>
            </div>
          ))}

          {/* Appointment suggestion */}
          <Card className="border-primary bg-primary/10">
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <Calendar className="mt-0.5 h-5 w-5 text-primary" />
                <div>
                  <p className="text-foreground font-medium">Cita Programada</p>
                  <p className="text-muted-foreground text-sm">Sábado, 10:00 AM</p>
                  <p className="text-muted-foreground mt-1 flex items-center gap-1 text-xs">
                    <MapPin className="h-3 w-3" />
                    Av. Winston Churchill #123, Plaza Ágora
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Message Input */}
      <div className="border-border bg-card sticky bottom-0 border-t">
        <div className="mx-auto max-w-4xl px-4 py-3">
          {/* Quick actions */}
          <div className="mb-3 flex items-center gap-2">
            <Button variant="outline" size="sm" className="text-xs">
              <Calendar className="mr-1 h-3 w-3" />
              Agendar cita
            </Button>
            <Button variant="outline" size="sm" className="text-xs">
              <MapPin className="mr-1 h-3 w-3" />
              Enviar ubicación
            </Button>
            <Button variant="outline" size="sm" className="text-xs">
              <Phone className="mr-1 h-3 w-3" />
              Compartir teléfono
            </Button>
          </div>

          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" className="shrink-0">
              <Paperclip className="text-muted-foreground h-5 w-5" />
            </Button>
            <Button variant="ghost" size="icon" className="shrink-0">
              <ImageIcon className="text-muted-foreground h-5 w-5" />
            </Button>

            <Input
              placeholder="Escribe un mensaje..."
              value={newMessage}
              onChange={e => setNewMessage(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSend()}
              className="flex-1"
            />

            <Button variant="ghost" size="icon" className="shrink-0">
              <Smile className="text-muted-foreground h-5 w-5" />
            </Button>

            <Button
              size="icon"
              className="shrink-0 bg-primary hover:bg-primary/90"
              onClick={handleSend}
              disabled={!newMessage.trim() || isSending}
            >
              <Send className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Safety Notice */}
      <div className="border-t border-yellow-200 bg-yellow-50">
        <div className="mx-auto max-w-4xl px-4 py-2">
          <p className="flex items-center gap-2 text-xs text-yellow-800">
            <AlertCircle className="h-3 w-3" />
            Por tu seguridad, realiza las transacciones en persona y verifica el vehículo antes de
            pagar.
          </p>
        </div>
      </div>
    </div>
  );
}
