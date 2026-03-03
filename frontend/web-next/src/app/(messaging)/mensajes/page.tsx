/**
 * Messages Page - Full Screen Layout
 *
 * Displays user's conversations (inquiries and received messages)
 * Uses dedicated messaging layout without account sidebar (WhatsApp/Gmail pattern)
 *
 * Route: /mensajes
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  MessageCircle,
  Search,
  Send,
  MoreVertical,
  Trash2,
  Archive,
  Check,
  CheckCheck,
  Loader2,
  ArrowLeft,
  Car,
  User,
  RefreshCw,
  AlertCircle,
  Inbox,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import {
  messagingService,
  type Conversation,
  type Message,
} from '@/services/messaging';

// =============================================================================
// LOADING STATE
// =============================================================================

function ConversationsLoading() {
  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-border p-4">
        <Skeleton className="h-10 w-full" />
      </div>
      <div className="flex-1">
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="flex gap-3 border-b border-border p-4">
            <Skeleton className="h-12 w-12 rounded-full" />
            <div className="flex-1">
              <Skeleton className="mb-2 h-4 w-32" />
              <Skeleton className="mb-2 h-3 w-48" />
              <Skeleton className="h-3 w-full" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function MessagesLoading() {
  return (
    <div className="flex h-full items-center justify-center">
      <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function ConversationsError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="flex h-full flex-col items-center justify-center p-6 text-center">
      <AlertCircle className="mb-4 h-12 w-12 text-red-400" />
      <h3 className="mb-2 font-semibold">Error al cargar</h3>
      <p className="mb-4 text-sm text-muted-foreground">No se pudieron cargar las conversaciones</p>
      <Button variant="outline" onClick={onRetry}>
        <RefreshCw className="mr-2 h-4 w-4" />
        Reintentar
      </Button>
    </div>
  );
}

// =============================================================================
// EMPTY STATE
// =============================================================================

function EmptyState() {
  return (
    <div className="flex h-full flex-col items-center justify-center p-8 text-center">
      {/* Animated gradient circle */}
      <div className="relative mb-6">
        <div className="absolute inset-0 animate-pulse rounded-full bg-gradient-to-br from-[#00A870]/20 to-primary/20 blur-xl" />
        <div className="relative flex h-24 w-24 items-center justify-center rounded-full bg-gradient-to-br from-[#00A870] to-primary/80 shadow-lg shadow-[#00A870]/25">
          <Inbox className="h-12 w-12 text-white" />
        </div>
      </div>
      <h3 className="mb-2 text-xl font-bold text-foreground">¡Tu bandeja está lista!</h3>
      <p className="mb-8 max-w-sm text-muted-foreground">
        Explora vehículos y contacta vendedores para iniciar conversaciones.
      </p>
      <Button
        asChild
        className="group relative overflow-hidden bg-gradient-to-r from-[#00A870] to-primary/80 px-8 py-3 text-base font-semibold shadow-lg shadow-[#00A870]/25 transition-all hover:shadow-xl hover:shadow-[#00A870]/30"
      >
        <Link href="/vehiculos" className="flex items-center gap-2">
          <Car className="h-5 w-5" />
          Explorar vehículos
          <span className="transition-transform group-hover:translate-x-1">→</span>
        </Link>
      </Button>
      <p className="mt-6 text-xs text-muted-foreground">💡 Tip: Los mensajes se guardan automáticamente</p>
    </div>
  );
}

// =============================================================================
// CONVERSATION ITEM
// =============================================================================

function ConversationItem({
  conversation,
  isSelected,
  onClick,
}: {
  conversation: Conversation;
  isSelected: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'group w-full border-b border-gray-50 p-4 text-left transition-all duration-200',
        isSelected
          ? 'border-l-4 border-l-[#00A870] bg-gradient-to-r from-[#00A870]/10 to-primary/5'
          : 'hover:bg-muted/50/80',
        conversation.unreadCount > 0 && !isSelected && 'bg-blue-50/30'
      )}
    >
      <div className="flex gap-3">
        {/* Avatar */}
        <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center overflow-hidden rounded-full bg-muted">
          {conversation.otherUser.avatarUrl ? (
            <Image
              src={conversation.otherUser.avatarUrl}
              alt={conversation.otherUser.name}
              width={48}
              height={48}
              className="h-full w-full object-cover"
            />
          ) : (
            <User className="h-6 w-6 text-muted-foreground" />
          )}
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-2">
            <span
              className={cn(
                'truncate font-medium',
                conversation.unreadCount > 0 ? 'text-foreground' : 'text-foreground'
              )}
            >
              {conversation.otherUser.name}
            </span>
            <span className="flex-shrink-0 text-xs text-muted-foreground">
              {conversation.lastMessage
                ? messagingService.formatConversationTime(conversation.lastMessage.sentAt)
                : messagingService.formatConversationTime(conversation.createdAt)}
            </span>
          </div>

          <div className="mt-0.5 flex items-center gap-2">
            <p className="truncate text-sm text-muted-foreground">{conversation.vehicle.title}</p>
            <Badge
              variant="outline"
              className={cn(
                'shrink-0 border-0 text-xs font-medium',
                conversation.type === 'inquiry'
                  ? 'bg-[#00A870]/10 text-[#00A870]'
                  : 'bg-blue-50 text-blue-600'
              )}
            >
              {conversation.type === 'inquiry' ? 'Enviado' : 'Recibido'}
            </Badge>
          </div>

          <div className="mt-1 flex items-center justify-between gap-2">
            <p
              className={cn(
                'truncate text-sm',
                conversation.unreadCount > 0 ? 'font-medium text-foreground' : 'text-muted-foreground'
              )}
            >
              {conversation.lastMessage ? (
                <>
                  {conversation.lastMessage.isFromMe && (
                    <span className="mr-1 text-muted-foreground">Tú:</span>
                  )}
                  {conversation.lastMessage.content}
                </>
              ) : (
                'Sin mensajes'
              )}
            </p>
            {conversation.unreadCount > 0 && (
              <span className="bg-primary flex h-5 min-w-[20px] flex-shrink-0 items-center justify-center rounded-full px-1.5 text-xs font-medium text-white">
                {conversation.unreadCount > 99 ? '99+' : conversation.unreadCount}
              </span>
            )}
          </div>
        </div>
      </div>
    </button>
  );
}

// =============================================================================
// MESSAGE BUBBLE
// =============================================================================

function MessageBubble({ message }: { message: Message }) {
  return (
    <div className={cn('flex', message.isFromMe ? 'justify-end' : 'justify-start')}>
      <div
        className={cn(
          'max-w-[75%] rounded-2xl px-4 py-2',
          message.isFromMe
            ? 'bg-primary rounded-br-md text-white'
            : 'rounded-bl-md bg-muted text-foreground'
        )}
      >
        <p className="whitespace-pre-wrap">{message.content}</p>
        <div
          className={cn(
            'mt-1 flex items-center justify-end gap-1 text-xs',
            message.isFromMe ? 'text-white/70' : 'text-muted-foreground'
          )}
        >
          <span>{messagingService.formatMessageTime(message.sentAt)}</span>
          {message.isFromMe &&
            (message.isRead ? <CheckCheck className="h-4 w-4" /> : <Check className="h-4 w-4" />)}
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function MessagesPage() {
  const queryClient = useQueryClient();

  const [selectedConversationId, setSelectedConversationId] = React.useState<string | null>(null);
  const [selectedType, setSelectedType] = React.useState<'inquiry' | 'received'>('inquiry');
  const [newMessage, setNewMessage] = React.useState('');
  const [searchQuery, setSearchQuery] = React.useState('');
  const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);

  const messagesEndRef = React.useRef<HTMLDivElement>(null);

  // Fetch conversations
  const {
    data: conversations = [],
    isLoading: conversationsLoading,
    error: conversationsError,
    refetch: refetchConversations,
  } = useQuery({
    queryKey: ['conversations'],
    queryFn: messagingService.getConversations,
    staleTime: 1000 * 30, // 30 seconds
  });

  // Fetch conversation detail when selected
  const { data: conversationDetail, isLoading: detailLoading } = useQuery({
    queryKey: ['conversation', selectedConversationId, selectedType],
    queryFn: () =>
      selectedConversationId
        ? messagingService.getConversationDetail(selectedConversationId, selectedType)
        : null,
    enabled: !!selectedConversationId,
  });

  // Send message mutation
  const sendMutation = useMutation({
    mutationFn: ({ conversationId, content }: { conversationId: string; content: string }) =>
      messagingService.sendMessage(conversationId, content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversation', selectedConversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
      setNewMessage('');
      toast.success('Mensaje enviado');
    },
    onError: () => {
      toast.error('Error al enviar el mensaje');
    },
  });

  // Archive mutation
  const archiveMutation = useMutation({
    mutationFn: messagingService.archiveConversation,
    onSuccess: () => {
      toast.success('Conversación archivada');
      setSelectedConversationId(null);
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
    onError: () => {
      toast.error('Error al archivar');
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: messagingService.deleteConversation,
    onSuccess: () => {
      toast.success('Conversación eliminada');
      setSelectedConversationId(null);
      setDeleteDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
    onError: () => {
      toast.error('Error al eliminar');
    },
  });

  // Get selected conversation
  const selectedConversation = React.useMemo(
    () => conversations.find(c => c.id === selectedConversationId),
    [conversations, selectedConversationId]
  );

  // Scroll to bottom on new messages
  React.useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [conversationDetail?.messages]);

  // Filter conversations
  const filteredConversations = React.useMemo(() => {
    if (!searchQuery) return conversations;
    const search = searchQuery.toLowerCase();
    return conversations.filter(
      c =>
        c.otherUser.name.toLowerCase().includes(search) ||
        c.vehicle.title.toLowerCase().includes(search)
    );
  }, [conversations, searchQuery]);

  // Handle send
  const handleSend = () => {
    if (!newMessage.trim() || !selectedConversationId) return;
    sendMutation.mutate({ conversationId: selectedConversationId, content: newMessage.trim() });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // Handle select conversation
  const handleSelectConversation = (conversation: Conversation) => {
    setSelectedConversationId(conversation.id);
    setSelectedType(conversation.type);
  };

  // Get messages
  const messages = conversationDetail?.messages || [];

  // Count stats
  const _unreadCount = conversations.reduce((sum, c) => sum + c.unreadCount, 0);

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      {/* Messages Container - Full height WhatsApp style */}
      <Card className="flex flex-1 flex-col overflow-hidden rounded-xl border-0 bg-card shadow-xl ring-1 shadow-border/50 ring-border">
        <div className="flex flex-1 overflow-hidden">
          {/* Conversations List - Left panel */}
          <div
            className={cn(
              'flex w-full flex-col border-r border-border bg-card md:w-80 lg:w-96',
              selectedConversationId && 'hidden md:flex'
            )}
          >
            {/* Header with search */}
            <div className="border-b border-border bg-gradient-to-b from-gray-50/80 to-white p-4">
              <h1 className="mb-3 text-xl font-bold text-foreground">Mensajes</h1>
              <div className="relative">
                <label htmlFor="search-conversations" className="sr-only">
                  Buscar conversaciones
                </label>
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <Search className="h-4 w-4 text-muted-foreground" />
                </div>
                <Input
                  id="search-conversations"
                  type="text"
                  placeholder="Buscar..."
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  className="h-12 rounded-xl border-border bg-card pl-12 shadow-sm transition-all focus:border-[#00A870] focus:bg-white focus:ring-2 focus:ring-[#00A870]/20"
                  aria-label="Buscar conversaciones"
                />
              </div>
            </div>

            {/* List */}
            <div className="flex-1 overflow-y-auto">
              {conversationsLoading ? (
                <ConversationsLoading />
              ) : conversationsError ? (
                <ConversationsError onRetry={refetchConversations} />
              ) : filteredConversations.length > 0 ? (
                filteredConversations.map(conv => (
                  <ConversationItem
                    key={conv.id}
                    conversation={conv}
                    isSelected={selectedConversationId === conv.id}
                    onClick={() => handleSelectConversation(conv)}
                  />
                ))
              ) : conversations.length === 0 ? (
                <EmptyState />
              ) : (
                <div className="flex flex-col items-center justify-center p-6 text-center">
                  <p className="text-muted-foreground">No se encontraron conversaciones</p>
                  <Button variant="link" onClick={() => setSearchQuery('')} className="mt-2">
                    Limpiar búsqueda
                  </Button>
                </div>
              )}
            </div>
          </div>

          {/* Chat Area */}
          <div className={cn('flex flex-1 flex-col', !selectedConversationId && 'hidden md:flex')}>
            {selectedConversation ? (
              <>
                {/* Chat Header - Premium */}
                <div className="flex items-center gap-3 border-b border-border bg-card p-4 shadow-sm">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="md:hidden"
                    onClick={() => setSelectedConversationId(null)}
                  >
                    <ArrowLeft className="h-5 w-5" />
                  </Button>

                  <div className="flex h-10 w-10 items-center justify-center overflow-hidden rounded-full bg-muted">
                    {selectedConversation.otherUser.avatarUrl ? (
                      <Image
                        src={selectedConversation.otherUser.avatarUrl}
                        alt={selectedConversation.otherUser.name}
                        width={40}
                        height={40}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <User className="h-5 w-5 text-muted-foreground" />
                    )}
                  </div>

                  <div className="min-w-0 flex-1">
                    <h3 className="truncate font-medium text-foreground">
                      {selectedConversation.otherUser.name}
                    </h3>
                    {selectedConversation.vehicle.slug ? (
                      <Link
                        href={`/vehiculos/${selectedConversation.vehicle.slug}`}
                        className="hover:text-primary block truncate text-sm text-muted-foreground"
                      >
                        {selectedConversation.vehicle.title}
                      </Link>
                    ) : (
                      <p className="truncate text-sm text-muted-foreground">
                        {selectedConversation.vehicle.title}
                      </p>
                    )}
                  </div>

                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon">
                        <MoreVertical className="h-5 w-5" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem
                        onClick={() => archiveMutation.mutate(selectedConversationId!)}
                        disabled={archiveMutation.isPending}
                      >
                        <Archive className="mr-2 h-4 w-4" />
                        Archivar
                      </DropdownMenuItem>
                      <DropdownMenuItem
                        className="text-red-600"
                        onClick={() => setDeleteDialogOpen(true)}
                      >
                        <Trash2 className="mr-2 h-4 w-4" />
                        Eliminar
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>

                {/* Messages */}
                <div className="flex-1 space-y-4 overflow-y-auto bg-gradient-to-b from-gray-50/30 to-white p-6">
                  {detailLoading ? (
                    <MessagesLoading />
                  ) : (
                    <>
                      {/* Vehicle Card - Premium */}
                      <Card className="overflow-hidden border-0 bg-gradient-to-r from-[#00A870]/5 to-primary/5 shadow-sm ring-1 ring-[#00A870]/10">
                        <CardContent className="p-4">
                          <Link
                            href={
                              selectedConversation.vehicle.slug
                                ? `/vehiculos/${selectedConversation.vehicle.slug}`
                                : '#'
                            }
                            className={cn(
                              'flex gap-3 transition-opacity',
                              selectedConversation.vehicle.slug && 'hover:opacity-90'
                            )}
                          >
                            <div className="flex h-12 w-16 items-center justify-center overflow-hidden rounded bg-muted">
                              {selectedConversation.vehicle.imageUrl ? (
                                <Image
                                  src={selectedConversation.vehicle.imageUrl}
                                  alt={selectedConversation.vehicle.title}
                                  width={64}
                                  height={48}
                                  className="h-full w-full object-cover"
                                />
                              ) : (
                                <Car className="h-6 w-6 text-muted-foreground" />
                              )}
                            </div>
                            <div>
                              <p className="text-sm font-medium text-foreground">
                                {selectedConversation.vehicle.title}
                              </p>
                              {selectedConversation.vehicle.price > 0 && (
                                <p className="text-primary text-sm font-semibold">
                                  {new Intl.NumberFormat('es-DO', {
                                    style: 'currency',
                                    currency: 'DOP',
                                    maximumFractionDigits: 0,
                                  }).format(selectedConversation.vehicle.price)}
                                </p>
                              )}
                            </div>
                          </Link>
                        </CardContent>
                      </Card>

                      {/* Messages */}
                      {messages.length > 0 ? (
                        messages.map(message => (
                          <MessageBubble key={message.id} message={message} />
                        ))
                      ) : (
                        <div className="py-8 text-center text-sm text-muted-foreground">
                          No hay mensajes en esta conversación
                        </div>
                      )}
                      <div ref={messagesEndRef} />
                    </>
                  )}
                </div>

                {/* Input - Premium style */}
                <div className="border-t border-border bg-gradient-to-t from-gray-50/50 to-white p-4">
                  <div className="flex gap-3">
                    <Input
                      type="text"
                      placeholder="Escribe un mensaje..."
                      value={newMessage}
                      onChange={e => setNewMessage(e.target.value)}
                      onKeyDown={handleKeyDown}
                      disabled={sendMutation.isPending}
                      className="h-12 flex-1 rounded-xl border-border bg-card shadow-sm transition-all focus:border-[#00A870] focus:ring-2 focus:ring-[#00A870]/20"
                    />
                    <Button
                      onClick={handleSend}
                      disabled={!newMessage.trim() || sendMutation.isPending}
                      className="h-12 w-12 rounded-xl bg-gradient-to-r from-[#00A870] to-primary/80 shadow-lg shadow-[#00A870]/25 transition-all hover:shadow-xl hover:shadow-[#00A870]/30 disabled:opacity-50 disabled:shadow-none"
                    >
                      {sendMutation.isPending ? (
                        <Loader2 className="h-5 w-5 animate-spin" />
                      ) : (
                        <Send className="h-5 w-5" />
                      )}
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <div className="flex flex-1 items-center justify-center bg-gradient-to-br from-gray-50 to-white p-8">
                <div className="text-center">
                  {/* Elegant illustration */}
                  <div className="relative mx-auto mb-6">
                    <div className="absolute inset-0 animate-pulse rounded-full bg-[#00A870]/10 blur-2xl" />
                    <div className="relative flex h-20 w-20 items-center justify-center rounded-2xl bg-gradient-to-br from-[#00A870]/10 to-primary/10 shadow-inner">
                      <MessageCircle className="h-10 w-10 text-[#00A870]" />
                    </div>
                  </div>
                  <h3 className="mb-2 text-lg font-semibold text-foreground">
                    Selecciona una conversación
                  </h3>
                  <p className="mb-4 max-w-xs text-sm text-muted-foreground">
                    Elige una conversación de la lista para ver los mensajes
                  </p>
                  <div className="inline-flex items-center gap-2 rounded-full bg-[#00A870]/5 px-4 py-2 text-xs text-[#00A870]">
                    <span>💬</span>
                    <span>Responde rápido para mejores resultados</span>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </Card>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Eliminar conversación?</AlertDialogTitle>
            <AlertDialogDescription>
              Esta acción no se puede deshacer. Se eliminará la conversación y todos los mensajes.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              onClick={() =>
                selectedConversationId && deleteMutation.mutate(selectedConversationId)
              }
              className="bg-red-600 hover:bg-red-700"
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              Eliminar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
