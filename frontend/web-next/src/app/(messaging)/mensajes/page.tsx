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
  Bot,
  Sparkles,
} from 'lucide-react';
import { useChatbot, type UseChatbotReturn } from '@/hooks/useChatbot';
import { BotMessageContent } from '@/components/chat/BotMessageContent';
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
// AI BOT PANEL (inline, full-height live chat)
// =============================================================================

/** Sentinel ID used to select the AI assistant "conversation" */
const BOT_CONVERSATION_ID = '__ai_assistant__';

function AiBotPanel({
  chat,
  onBack,
}: {
  chat: UseChatbotReturn;
  onBack: () => void;
}) {
  const messagesEndRef = React.useRef<HTMLDivElement>(null);
  const [inputValue, setInputValue] = React.useState('');

  React.useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chat.messages]);

  const handleSend = () => {
    const text = inputValue.trim();
    if (!text || chat.isLoading) return;
    setInputValue('');
    chat.sendMessage(text);
  };

  return (
    <>
      {/* Header */}
      <div className="flex items-center gap-3 border-b border-border bg-card p-4 shadow-sm">
        <Button variant="ghost" size="icon" className="md:hidden" onClick={onBack}>
          <ArrowLeft className="h-5 w-5" />
        </Button>

        <div className="relative">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-[#00A870] to-emerald-600 shadow-md">
            <Bot className="h-5 w-5 text-white" />
          </div>
          <span className="absolute -right-0.5 -bottom-0.5 h-3 w-3 rounded-full border-2 border-white bg-green-400" />
        </div>

        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-foreground truncate">
            {chat.botName || 'Asistente OKLA'}
          </h3>
          <p className="text-xs text-[#00A870]">● En línea · Responde al instante</p>
        </div>

        <Badge className="shrink-0 border-0 bg-[#00A870]/10 text-[#00A870]">
          <Sparkles className="mr-1 h-3 w-3" />
          IA
        </Badge>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto bg-gradient-to-b from-gray-50/30 to-white p-4">
        {/* Connecting state */}
        {!chat.isConnected && chat.isLoading && (
          <div className="flex flex-col items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-[#00A870]" />
            <p className="mt-3 text-sm text-muted-foreground">Conectando con el asistente...</p>
          </div>
        )}

        {/* Connection error */}
        {!chat.isConnected && !chat.isLoading && chat.error && (
          <div className="flex flex-col items-center justify-center py-12">
            <AlertCircle className="h-8 w-8 text-red-400" />
            <p className="mt-3 text-sm text-center text-muted-foreground">{chat.error}</p>
            <Button
              variant="outline"
              size="sm"
              className="mt-4"
              onClick={() => chat.startSession()}
            >
              <RefreshCw className="mr-2 h-4 w-4" />
              Reintentar conexión
            </Button>
          </div>
        )}

        {/* Message bubbles */}
        <div className="space-y-4">
          {chat.messages.map(message => (
            <div
              key={message.id}
              className={cn('flex gap-2.5', message.isFromBot ? 'justify-start' : 'justify-end')}
            >
              {message.isFromBot && (
                <div className="mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-[#00A870] to-emerald-600 shadow-sm">
                  <Bot className="h-3.5 w-3.5 text-white" />
                </div>
              )}
              <div
                className={cn(
                  'max-w-[78%] rounded-2xl px-4 py-2.5 shadow-sm',
                  message.isFromBot
                    ? 'rounded-tl-sm bg-white ring-1 ring-gray-100 text-gray-800'
                    : 'rounded-tr-sm bg-gradient-to-br from-[#00A870] to-emerald-600 text-white'
                )}
              >
                {message.isLoading ? (
                  <div className="flex gap-1.5 py-1">
                    <span
                      className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                      style={{ animationDelay: '0ms' }}
                    />
                    <span
                      className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                      style={{ animationDelay: '150ms' }}
                    />
                    <span
                      className="h-2 w-2 animate-bounce rounded-full bg-gray-400"
                      style={{ animationDelay: '300ms' }}
                    />
                  </div>
                ) : message.isFromBot ? (
                  <>
                    <BotMessageContent content={message.content} />
                    <p className="mt-1.5 text-xs text-gray-400">
                      {message.timestamp.toLocaleTimeString('es-DO', {
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </p>
                  </>
                ) : (
                  <>
                    <p className="text-sm whitespace-pre-wrap">{message.content}</p>
                    <p className="mt-1.5 text-right text-xs text-white/70">
                      {message.timestamp.toLocaleTimeString('es-DO', {
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </p>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Quick Replies */}
        {chat.quickReplies.length > 0 && !chat.isLoading && (
          <div className="mt-4 flex flex-wrap gap-2">
            {chat.quickReplies.map(reply => (
              <button
                key={reply.payload ?? reply.text}
                onClick={() => chat.selectQuickReply(reply)}
                disabled={chat.isLoading}
                className="rounded-full border border-[#00A870]/40 bg-white px-3.5 py-1.5 text-sm font-medium text-[#00A870] shadow-sm transition-colors hover:border-[#00A870] hover:bg-[#00A870]/5 disabled:opacity-50"
              >
                {reply.text}
              </button>
            ))}
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input bar */}
      <div className="border-t border-border bg-gradient-to-t from-gray-50/50 to-white p-4">
        {chat.isLimitReached ? (
          <div className="flex items-center justify-between rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-700">
            <span>Has alcanzado el límite de mensajes.</span>
            <Button
              variant="link"
              size="sm"
              className="h-auto p-0 text-amber-700"
              onClick={chat.resetChat}
            >
              Reiniciar chat
            </Button>
          </div>
        ) : (
          <div className="flex gap-3">
            <Input
              type="text"
              placeholder="Escribe tu mensaje al asistente..."
              value={inputValue}
              onChange={e => setInputValue(e.target.value)}
              onKeyDown={e => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  handleSend();
                }
              }}
              disabled={chat.isLoading || !chat.isConnected}
              className="h-12 flex-1 rounded-xl border-border bg-card shadow-sm transition-all focus:border-[#00A870] focus:ring-2 focus:ring-[#00A870]/20"
              aria-label="Mensaje al asistente"
            />
            <Button
              onClick={handleSend}
              disabled={!inputValue.trim() || chat.isLoading || !chat.isConnected}
              className="h-12 w-12 rounded-xl bg-gradient-to-r from-[#00A870] to-emerald-600 shadow-lg shadow-[#00A870]/25 transition-all hover:shadow-xl disabled:opacity-50 disabled:shadow-none"
              aria-label="Enviar mensaje"
            >
              {chat.isLoading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <Send className="h-5 w-5" />
              )}
            </Button>
          </div>
        )}
        {!chat.isLimitReached &&
          chat.remainingInteractions > 0 &&
          chat.remainingInteractions <= 5 && (
            <p className="mt-2 text-center text-xs text-muted-foreground">
              {chat.remainingInteractions} mensajes restantes
            </p>
          )}
      </div>
    </>
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

  // ── AI Chatbot session ──────────────────────────────────────
  const chat = useChatbot({ autoStart: false, maxRetries: 2 });

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
              {/* ── Pinned AI Bot Conversation ── */}
              <button
                onClick={() => {
                  setSelectedConversationId(BOT_CONVERSATION_ID);
                  if (!chat.isConnected && !chat.isLoading) {
                    chat.startSession();
                  }
                }}
                className={cn(
                  'group w-full border-b border-gray-100 p-4 text-left transition-all duration-200',
                  selectedConversationId === BOT_CONVERSATION_ID
                    ? 'border-l-4 border-l-[#00A870] bg-gradient-to-r from-[#00A870]/10 to-emerald-50/50'
                    : 'bg-gradient-to-r from-[#00A870]/3 to-transparent hover:from-[#00A870]/8 hover:to-emerald-50/30'
                )}
              >
                <div className="flex gap-3">
                  <div className="relative shrink-0">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-gradient-to-br from-[#00A870] to-emerald-600 shadow-md ring-2 ring-[#00A870]/20">
                      <Bot className="h-6 w-6 text-white" />
                    </div>
                    <span className="absolute -right-0.5 -bottom-0.5 flex h-3.5 w-3.5 items-center justify-center rounded-full border-2 border-white bg-green-400" />
                  </div>

                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-2">
                      <div className="flex items-center gap-1.5">
                        <span className="font-semibold text-foreground">Asistente OKLA</span>
                        <Badge className="border-0 bg-[#00A870]/10 px-1.5 py-0 text-xs text-[#00A870]">
                          <Sparkles className="mr-0.5 h-2.5 w-2.5" />
                          IA
                        </Badge>
                      </div>
                      <span className="text-xs font-medium text-[#00A870]">En línea</span>
                    </div>
                    <p className="mt-0.5 text-xs text-muted-foreground">
                      Inteligencia Artificial · 24/7
                    </p>
                    <p className="mt-0.5 truncate text-sm text-muted-foreground">
                      {chat.messages.length > 0
                        ? `${chat.messages[chat.messages.length - 1].isFromBot ? '🤖' : 'Tú'}: ${chat.messages[chat.messages.length - 1].content.slice(0, 38)}...`
                        : 'Pregúntame sobre vehículos, precios y más'}
                    </p>
                  </div>
                </div>
              </button>
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
          <div
            className={cn(
              'flex flex-1 flex-col',
              !selectedConversationId && 'hidden md:flex'
            )}
          >
            {selectedConversationId === BOT_CONVERSATION_ID ? (
              <AiBotPanel chat={chat} onBack={() => setSelectedConversationId(null)} />
            ) : selectedConversation ? (
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

                  {/* WhatsApp button – shown for received conversations when buyer phone is available */}
                  {selectedConversation.type === 'received' &&
                    selectedConversation.otherUser.phone && (
                      <a
                        href={`https://wa.me/${selectedConversation.otherUser.phone.replace(/\D/g, '')}?text=${encodeURIComponent(
                          `Hola ${selectedConversation.otherUser.name}, te contacto desde OKLA por el ${selectedConversation.vehicle.title}.`
                        )}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        title="Contactar por WhatsApp"
                        className="inline-flex h-9 w-9 items-center justify-center rounded-md text-[#25D366] transition-colors hover:bg-[#25D366]/10"
                      >
                        <svg
                          viewBox="0 0 24 24"
                          className="h-5 w-5 fill-current"
                          aria-hidden="true"
                        >
                          <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
                        </svg>
                        <span className="sr-only">WhatsApp</span>
                      </a>
                    )}

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
