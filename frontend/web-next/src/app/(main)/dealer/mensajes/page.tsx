/**
 * Dealer Messages Page
 *
 * Manage customer conversations
 */

'use client';

import * as React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Search,
  Send,
  MoreVertical,
  Phone,
  Car,
  User,
  Clock,
  Check,
  CheckCheck,
  RefreshCw,
  AlertCircle,
  MessageSquare,
} from 'lucide-react';
import {
  useReceivedInquiries,
  useConversation,
  useReplyToContactRequest,
  useUnreadCount,
} from '@/hooks/use-contact';
import { sanitizeText } from '@/lib/security/sanitize';
import {
  formatRelativeTime,
  getStatusColor,
  getStatusLabel,
} from '@/services/contact';
import { toast } from 'sonner';

// Skeleton for conversations list
function ConversationsSkeleton() {
  return (
    <div className="flex-1 overflow-y-auto">
      {[1, 2, 3, 4, 5].map(i => (
        <div key={i} className="border-border border-b p-4">
          <div className="flex items-start gap-3">
            <Skeleton className="h-10 w-10 rounded-full" />
            <div className="min-w-0 flex-1 space-y-2">
              <div className="flex items-center justify-between">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-3 w-12" />
              </div>
              <Skeleton className="h-3 w-full" />
              <Skeleton className="h-3 w-32" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

// Skeleton for messages
function MessagesSkeleton() {
  return (
    <div className="flex-1 space-y-4 overflow-y-auto p-4">
      {[1, 2, 3, 4, 5].map(i => (
        <div key={i} className={`flex ${i % 2 === 0 ? 'justify-end' : 'justify-start'}`}>
          <Skeleton
            className={`h-16 w-64 rounded-2xl ${i % 2 === 0 ? 'rounded-br-sm' : 'rounded-bl-sm'}`}
          />
        </div>
      ))}
    </div>
  );
}

export default function DealerMessagesPage() {
  const [selectedConversation, setSelectedConversation] = React.useState<string | null>(null);
  const [newMessage, setNewMessage] = React.useState('');
  const [searchQuery, setSearchQuery] = React.useState('');

  // API hooks
  const {
    data: conversations,
    isLoading: conversationsLoading,
    error: conversationsError,
    refetch: refetchConversations,
  } = useReceivedInquiries();

  const { data: _unreadCount } = useUnreadCount();

  const { data: conversationDetail, isLoading: messagesLoading } = useConversation(
    selectedConversation || ''
  );

  const { mutate: reply, isPending: replying } = useReplyToContactRequest();

  // Set first conversation as selected when data loads
  React.useEffect(() => {
    if (conversations && conversations.length > 0 && !selectedConversation) {
      setSelectedConversation(conversations[0].id);
    }
  }, [conversations, selectedConversation]);

  // Filter conversations by search
  const filteredConversations = React.useMemo(() => {
    if (!conversations) return [];
    if (!searchQuery) return conversations;

    const query = searchQuery.toLowerCase();
    return conversations.filter(
      c =>
        c.buyerName.toLowerCase().includes(query) ||
        c.subject.toLowerCase().includes(query) ||
        c.buyerEmail.toLowerCase().includes(query)
    );
  }, [conversations, searchQuery]);

  const selectedConvo = conversations?.find(c => c.id === selectedConversation);

  const handleSendMessage = () => {
    if (newMessage.trim() && selectedConversation) {
      // Sanitize message content before sending
      const sanitizedMessage = sanitizeText(newMessage.trim(), { maxLength: 5000 });
      reply(
        { id: selectedConversation, message: sanitizedMessage },
        {
          onSuccess: () => {
            setNewMessage('');
            toast.success('Mensaje enviado');
          },
          onError: () => {
            toast.error('Error al enviar el mensaje');
          },
        }
      );
    }
  };

  // Count totals
  const totalConversations = conversations?.length || 0;
  const totalUnread = conversations?.reduce((sum, c) => sum + c.unreadCount, 0) || 0;

  // Error state
  if (conversationsError) {
    return (
      <div className="h-[calc(100vh-12rem)]">
        <div className="mb-6">
          <h1 className="text-foreground text-2xl font-bold">Mensajes</h1>
          <p className="text-muted-foreground">Conversaciones con clientes interesados</p>
        </div>
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex flex-col items-center justify-center gap-4 p-8">
            <AlertCircle className="h-12 w-12 text-red-500" />
            <p className="text-center text-red-700">
              Error al cargar las conversaciones. Por favor intenta de nuevo.
            </p>
            <Button variant="outline" onClick={() => refetchConversations()}>
              <RefreshCw className="mr-2 h-4 w-4" />
              Reintentar
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="h-[calc(100vh-12rem)]">
      <div className="mb-6 flex flex-col justify-between gap-4 sm:flex-row">
        <div className="flex items-center gap-3">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Mensajes</h1>
            <p className="text-muted-foreground">Conversaciones con clientes interesados</p>
          </div>
          <Button
            variant="ghost"
            size="icon"
            onClick={() => refetchConversations()}
            title="Actualizar"
          >
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="secondary">{totalConversations} conversaciones</Badge>
          {totalUnread > 0 && (
            <Badge className="bg-amber-100 text-amber-700">{totalUnread} sin leer</Badge>
          )}
        </div>
      </div>

      <div className="bg-card flex h-[calc(100%-4rem)] overflow-hidden rounded-lg border">
        {/* Conversations List */}
        <div className="flex w-80 flex-col border-r">
          {/* Search */}
          <div className="border-border border-b p-3">
            <div className="relative">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <Input
                placeholder="Buscar conversaciones..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>
          </div>

          {/* List */}
          <div className="flex-1 overflow-y-auto">
            {conversationsLoading ? (
              <ConversationsSkeleton />
            ) : filteredConversations.length === 0 ? (
              <div className="text-muted-foreground flex flex-col items-center justify-center gap-2 p-8 text-center">
                <MessageSquare className="h-10 w-10 text-gray-300" />
                <p>No hay conversaciones</p>
              </div>
            ) : (
              filteredConversations.map(convo => (
                <button
                  key={convo.id}
                  onClick={() => setSelectedConversation(convo.id)}
                  className={`border-border hover:bg-muted/50 w-full border-b p-4 text-left transition-colors ${
                    selectedConversation === convo.id
                      ? 'border-l-2 border-l-primary bg-primary/10'
                      : ''
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <div className="relative">
                      <div className="bg-muted flex h-10 w-10 items-center justify-center rounded-full">
                        <User className="text-muted-foreground h-5 w-5" />
                      </div>
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center justify-between">
                        <p className="truncate font-medium">{convo.buyerName}</p>
                        <span className="text-muted-foreground text-xs">
                          {formatRelativeTime(convo.respondedAt || convo.createdAt)}
                        </span>
                      </div>
                      <p className="text-muted-foreground truncate text-sm">{convo.subject}</p>
                      <div className="mt-1 flex items-center justify-between">
                        <p className="text-muted-foreground flex items-center gap-1 text-xs">
                          <Car className="h-3 w-3" />
                          {convo.vehicleTitle || 'Vehículo'}
                        </p>
                        <Badge
                          variant="outline"
                          className={`text-xs ${getStatusColor(convo.status)}`}
                        >
                          {getStatusLabel(convo.status)}
                        </Badge>
                      </div>
                    </div>
                    {convo.unreadCount > 0 && (
                      <Badge className="bg-primary">{convo.unreadCount}</Badge>
                    )}
                  </div>
                </button>
              ))
            )}
          </div>
        </div>

        {/* Chat Area */}
        {selectedConvo ? (
          <div className="flex flex-1 flex-col">
            {/* Header */}
            <div className="border-border flex items-center justify-between border-b p-4">
              <div className="flex items-center gap-3">
                <div className="relative">
                  <div className="bg-muted flex h-10 w-10 items-center justify-center rounded-full">
                    <User className="text-muted-foreground h-5 w-5" />
                  </div>
                </div>
                <div>
                  <p className="font-medium">{selectedConvo.buyerName}</p>
                  <p className="text-muted-foreground text-xs">{selectedConvo.buyerEmail}</p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Badge variant="outline" className="flex items-center gap-1">
                  <Car className="h-3 w-3" />
                  {selectedConvo.vehicleTitle}
                </Badge>
                {selectedConvo.buyerPhone && (
                  <Button variant="ghost" size="icon" asChild>
                    <a href={`tel:${selectedConvo.buyerPhone}`}>
                      <Phone className="h-4 w-4" />
                    </a>
                  </Button>
                )}
                <Button variant="ghost" size="icon">
                  <MoreVertical className="h-4 w-4" />
                </Button>
              </div>
            </div>

            {/* Messages */}
            <div className="flex-1 space-y-4 overflow-y-auto p-4">
              {messagesLoading ? (
                <MessagesSkeleton />
              ) : conversationDetail?.messages && conversationDetail.messages.length > 0 ? (
                conversationDetail.messages.map(message => (
                  <div
                    key={message.id}
                    className={`flex ${!message.isFromBuyer ? 'justify-end' : 'justify-start'}`}
                  >
                    <div
                      className={`max-w-[70%] rounded-2xl px-4 py-2 ${
                        !message.isFromBuyer
                          ? 'rounded-br-sm bg-primary text-white'
                          : 'bg-muted text-foreground rounded-bl-sm'
                      }`}
                    >
                      <p>{message.message}</p>
                      <div
                        className={`mt-1 flex items-center gap-1 text-xs ${
                          !message.isFromBuyer
                            ? 'justify-end text-primary-foreground'
                            : 'text-muted-foreground'
                        }`}
                      >
                        <Clock className="h-3 w-3" />
                        {formatRelativeTime(message.sentAt)}
                        {!message.isFromBuyer &&
                          (message.isRead ? (
                            <CheckCheck className="ml-1 h-3 w-3" />
                          ) : (
                            <Check className="ml-1 h-3 w-3" />
                          ))}
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-muted-foreground flex flex-col items-center justify-center gap-2 py-12">
                  <MessageSquare className="h-10 w-10" />
                  <p>No hay mensajes aún</p>
                </div>
              )}
            </div>

            {/* Input */}
            <div className="border-border border-t p-4">
              <div className="flex gap-2">
                <Input
                  placeholder="Escribe un mensaje..."
                  value={newMessage}
                  onChange={e => setNewMessage(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && !replying && handleSendMessage()}
                  className="flex-1"
                  disabled={replying}
                />
                <Button
                  onClick={handleSendMessage}
                  className="bg-primary hover:bg-primary/90"
                  disabled={!newMessage.trim() || replying}
                >
                  <Send className="h-4 w-4" />
                </Button>
              </div>
              <div className="mt-2 flex gap-2">
                <Button variant="outline" size="sm" className="text-xs">
                  📍 Enviar ubicación
                </Button>
                <Button variant="outline" size="sm" className="text-xs">
                  📅 Agendar cita
                </Button>
                <Button variant="outline" size="sm" className="text-xs">
                  💳 Enviar cotización
                </Button>
              </div>
            </div>
          </div>
        ) : (
          <div className="text-muted-foreground flex flex-1 items-center justify-center">
            Selecciona una conversación para comenzar
          </div>
        )}
      </div>
    </div>
  );
}
