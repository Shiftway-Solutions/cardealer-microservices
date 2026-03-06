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
import dynamic from 'next/dynamic';
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
import { sanitizeText } from '@/lib/security/sanitize';
import { csrfFetch } from '@/lib/security/csrf';
import { toast } from 'sonner';
import { messagingService, type Conversation, type Message } from '@/services/messaging';

// Lazy-load DealerBotPanel + AppointmentScheduler (~475 lines) to reduce initial bundle
const DealerBotPanel = dynamic(() => import('@/components/messaging/DealerBotPanel'), {
  ssr: false,
  loading: () => (
    <div className="flex flex-1 items-center justify-center">
      <Loader2 className="text-primary h-8 w-8 animate-spin" />
    </div>
  ),
});

// =============================================================================
// LOADING STATE
// =============================================================================

function ConversationsLoading() {
  return (
    <div className="flex h-full flex-col">
      <div className="border-border border-b p-4">
        <Skeleton className="h-10 w-full" />
      </div>
      <div className="flex-1">
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="border-border flex gap-3 border-b p-4">
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
      <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
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
      <h2 className="mb-2 font-semibold">Error al cargar</h2>
      <p className="text-muted-foreground mb-4 text-sm">No se pudieron cargar las conversaciones</p>
      <Button variant="outline" onClick={onRetry}>
        <RefreshCw className="mr-2 h-4 w-4" />
        Reintentar
      </Button>
    </div>
  );
}

// =============================================================================
// BOT SESSIONS (from localStorage — persists across page reloads)
// =============================================================================

interface BotSession {
  dealerId: string;
  dealerName: string;
  lastMessage: string | null;
  lastTimestamp: Date | null;
}

function parseBotSessions(): BotSession[] {
  try {
    const found: BotSession[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (!key?.startsWith('okla_chat_session_')) continue;
      const suffix = key.replace('okla_chat_session_', '');
      if (suffix === 'default') continue; // skip global OKLA support
      const dealerId = suffix;
      const dealerName = localStorage.getItem(`okla_chat_dealername_${dealerId}`) || 'Asistente IA';
      let lastMessage: string | null = null;
      let lastTimestamp: Date | null = null;
      const msgsJson = localStorage.getItem(`okla_chat_messages_${dealerId}`);
      if (msgsJson) {
        const msgs = JSON.parse(msgsJson) as Array<{
          content: string;
          timestamp: string;
          isLoading?: boolean;
        }>;
        const real = msgs.filter(m => !m.isLoading);
        if (real.length > 0) {
          lastMessage = real[real.length - 1].content;
          lastTimestamp = new Date(real[real.length - 1].timestamp);
        }
      }
      found.push({ dealerId, dealerName, lastMessage, lastTimestamp });
    }
    found.sort((a, b) => (b.lastTimestamp?.getTime() ?? 0) - (a.lastTimestamp?.getTime() ?? 0));
    return found;
  } catch {
    return [];
  }
}

function useBotSessions(): BotSession[] {
  const [sessions, setSessions] = React.useState<BotSession[]>([]);

  React.useEffect(() => {
    setSessions(parseBotSessions());

    // Refresh sidebar when a new message is saved to localStorage (storage event)
    const handleStorage = (e: StorageEvent) => {
      if (e.key?.startsWith('okla_chat_')) {
        setSessions(parseBotSessions());
      }
    };
    window.addEventListener('storage', handleStorage);

    // Poll every 15 seconds to catch same-tab updates (storage events fire only cross-tab)
    const interval = setInterval(() => {
      setSessions(parseBotSessions());
    }, 15_000);

    return () => {
      window.removeEventListener('storage', handleStorage);
      clearInterval(interval);
    };
  }, []);

  return sessions;
}

// =============================================================================
// EMPTY STATE
// =============================================================================

function EmptyState() {
  return (
    <div className="flex h-full flex-col items-center justify-center p-8 text-center">
      {/* Animated gradient circle */}
      <div className="relative mb-6">
        <div className="to-primary/20 from-primary/20 absolute inset-0 animate-pulse rounded-full bg-gradient-to-br blur-xl" />
        <div className="to-primary/80 from-primary shadow-primary/25 relative flex h-24 w-24 items-center justify-center rounded-full bg-gradient-to-br shadow-lg">
          <Inbox className="h-12 w-12 text-white" />
        </div>
      </div>
      <h2 className="text-foreground mb-2 text-xl font-bold">Sin mensajes aún</h2>
      <p className="text-muted-foreground mb-2 max-w-sm text-sm">
        Aquí aparecen tus conversaciones con vendedores y dealers — cuando contactas a alguien desde
        una publicación de vehículo.
      </p>
      <p className="text-muted-foreground mb-8 max-w-sm text-xs">
        💬 Para chatear con un Asistente IA de un dealer, usa la pestaña{' '}
        <strong>Asistentes IA</strong>.
      </p>
      <Button
        asChild
        className="group to-primary/80 from-primary shadow-primary/25 hover:shadow-primary/30 relative overflow-hidden bg-gradient-to-r px-8 py-3 text-base font-semibold shadow-lg transition-all hover:shadow-xl"
      >
        <Link href="/vehiculos" className="flex items-center gap-2">
          <Car className="h-5 w-5" />
          Explorar vehículos
          <span className="transition-transform group-hover:translate-x-1">→</span>
        </Link>
      </Button>
      <p className="text-muted-foreground mt-6 text-xs">
        💡 Tip: Haz clic en &quot;Contactar vendedor&quot; en cualquier vehículo para iniciar una
        conversación
      </p>
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
          ? 'to-primary/5 border-l-primary from-primary/10 border-l-4 bg-gradient-to-r'
          : 'hover:bg-muted/50/80',
        conversation.unreadCount > 0 && !isSelected && 'bg-blue-50/30'
      )}
    >
      <div className="flex gap-3">
        {/* Avatar */}
        <div className="bg-muted flex h-12 w-12 flex-shrink-0 items-center justify-center overflow-hidden rounded-full">
          {conversation.otherUser.avatarUrl ? (
            <Image
              src={conversation.otherUser.avatarUrl}
              alt={conversation.otherUser.name}
              width={48}
              height={48}
              className="h-full w-full object-cover"
            />
          ) : (
            <User className="text-muted-foreground h-6 w-6" />
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
            <span className="text-muted-foreground flex-shrink-0 text-xs">
              {conversation.lastMessage
                ? messagingService.formatConversationTime(conversation.lastMessage.sentAt)
                : messagingService.formatConversationTime(conversation.createdAt)}
            </span>
          </div>

          <div className="mt-0.5 flex items-center gap-2">
            <p className="text-muted-foreground truncate text-sm">{conversation.vehicle.title}</p>
            <Badge
              variant="outline"
              className={cn(
                'shrink-0 border-0 text-xs font-medium',
                conversation.type === 'inquiry'
                  ? 'bg-primary/10 text-primary'
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
                conversation.unreadCount > 0
                  ? 'text-foreground font-medium'
                  : 'text-muted-foreground'
              )}
            >
              {conversation.lastMessage ? (
                <>
                  {conversation.lastMessage.isFromMe && (
                    <span className="text-muted-foreground mr-1">Tú:</span>
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
            : 'bg-muted text-foreground rounded-bl-md'
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

  // ── AI Bot panel toggle (dealer-scoped) ─────────────────────
  const [showBotPanel, setShowBotPanel] = React.useState(false);

  // ── Sidebar tab: 'mensajes' | 'asistentes' ──────────────────
  const [sidebarTab, setSidebarTab] = React.useState<'mensajes' | 'asistentes'>('mensajes');
  const botSessions = useBotSessions();

  // ── URL-triggered bot (from vehicle detail "Chat Live" button) ─
  // Populated when the page is loaded with ?sellerId=...&vehicleTitle=...
  const [urlBotConfig, setUrlBotConfig] = React.useState<{
    dealerId: string;
    dealerName: string;
    dealerEmail?: string;
    vehicleTitle?: string;
  } | null>(null);

  React.useEffect(() => {
    // Read URL params via native browser API (avoids Next.js Suspense requirement)
    if (typeof window === 'undefined') return;
    const params = new URLSearchParams(window.location.search);
    const sellerId = params.get('sellerId');
    const vehicleTitle = params.get('vehicleTitle');
    const sellerEmail = params.get('sellerEmail');
    if (sellerId) {
      setUrlBotConfig({
        dealerId: sellerId,
        dealerName: vehicleTitle ? decodeURIComponent(vehicleTitle) : 'Vendedor',
        dealerEmail: sellerEmail ? decodeURIComponent(sellerEmail) : undefined,
        vehicleTitle: vehicleTitle ? decodeURIComponent(vehicleTitle) : undefined,
      });
      // Clean URL so it doesn't re-trigger on refresh
      window.history.replaceState({}, '', '/mensajes');
    }
  }, []); // intentionally run only on mount

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

  // Reset bot panel when user switches conversation
  React.useEffect(() => {
    setShowBotPanel(false);
  }, [selectedConversationId]);

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
    sendMutation.mutate({
      conversationId: selectedConversationId,
      content: sanitizeText(newMessage.trim(), { maxLength: 5000 }),
    });
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
      <Card className="bg-card shadow-border/50 ring-border flex flex-1 flex-col overflow-hidden rounded-xl border-0 shadow-xl ring-1">
        <div className="flex flex-1 overflow-hidden">
          {/* Conversations List - Left panel */}
          <div
            className={cn(
              'border-border bg-card flex w-full flex-col border-r md:w-80 lg:w-96',
              selectedConversationId && 'hidden md:flex'
            )}
          >
            {/* Header with search */}
            <div className="border-border border-b bg-gradient-to-b from-gray-50/80 to-white p-4">
              <h1 className="text-foreground mb-3 text-xl font-bold">Mensajes</h1>

              {/* Tabs */}
              <div className="mb-3 flex rounded-xl border border-gray-100 bg-gray-50 p-0.5">
                <button
                  onClick={() => setSidebarTab('mensajes')}
                  className={cn(
                    'flex flex-1 items-center justify-center gap-1.5 rounded-lg py-2 text-xs font-medium transition-all',
                    sidebarTab === 'mensajes'
                      ? 'text-primary bg-white shadow-sm'
                      : 'text-gray-500 hover:text-gray-700'
                  )}
                >
                  <MessageCircle className="h-3.5 w-3.5" />
                  Mensajes
                </button>
                <button
                  onClick={() => setSidebarTab('asistentes')}
                  className={cn(
                    'relative flex flex-1 items-center justify-center gap-1.5 rounded-lg py-2 text-xs font-medium transition-all',
                    sidebarTab === 'asistentes'
                      ? 'text-primary bg-white shadow-sm'
                      : 'text-gray-500 hover:text-gray-700'
                  )}
                >
                  <Bot className="h-3.5 w-3.5" />
                  Asistentes IA
                  {botSessions.length > 0 && (
                    <span className="bg-primary flex h-4 min-w-[16px] items-center justify-center rounded-full px-1 text-[10px] font-semibold text-white">
                      {botSessions.length}
                    </span>
                  )}
                </button>
              </div>

              {sidebarTab === 'mensajes' && (
                <div className="relative">
                  <label htmlFor="search-conversations" className="sr-only">
                    Buscar conversaciones
                  </label>
                  <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                    <Search className="text-muted-foreground h-4 w-4" />
                  </div>
                  <Input
                    id="search-conversations"
                    type="text"
                    placeholder="Buscar..."
                    value={searchQuery}
                    onChange={e => setSearchQuery(e.target.value)}
                    className="border-border bg-card focus:border-primary focus:ring-primary/20 h-12 rounded-xl pl-12 shadow-sm transition-all focus:bg-white focus:ring-2"
                    aria-label="Buscar conversaciones"
                  />
                </div>
              )}
            </div>

            {/* List */}
            <div className="flex-1 overflow-y-auto">
              {/* ── Asistentes IA tab ──────────────────────── */}
              {sidebarTab === 'asistentes' ? (
                botSessions.length === 0 ? (
                  <div className="flex h-full flex-col items-center justify-center p-6 text-center">
                    <div className="bg-primary/10 mb-4 flex h-16 w-16 items-center justify-center rounded-2xl">
                      <Bot className="text-primary h-8 w-8" />
                    </div>
                    <h2 className="mb-1 font-semibold text-gray-700">Sin conversaciones IA</h2>
                    <p className="text-muted-foreground mb-4 text-sm">
                      Chatea con el asistente de un dealer desde la p&aacute;gina de su
                      veh&iacute;culo
                    </p>
                    <Button asChild variant="outline" size="sm">
                      <Link href="/vehiculos">Ver veh&iacute;culos</Link>
                    </Button>
                  </div>
                ) : (
                  <div>
                    {botSessions.map(session => (
                      <button
                        key={session.dealerId}
                        onClick={() => {
                          setUrlBotConfig({
                            dealerId: session.dealerId,
                            dealerName: session.dealerName,
                          });
                          setSelectedConversationId(null);
                        }}
                        className={cn(
                          'group hover:bg-muted/50 w-full border-b border-gray-50 p-4 text-left transition-all duration-200',
                          urlBotConfig?.dealerId === session.dealerId &&
                            'border-l-primary from-primary/10 to-primary/5 border-l-4 bg-gradient-to-r'
                        )}
                      >
                        <div className="flex gap-3">
                          <div className="from-primary flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br to-emerald-600 shadow-sm">
                            <Bot className="h-6 w-6 text-white" />
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center justify-between gap-2">
                              <span className="truncate font-medium text-gray-800">
                                {session.dealerName}
                              </span>
                              {session.lastTimestamp && (
                                <span className="text-muted-foreground flex-shrink-0 text-xs">
                                  {messagingService.formatConversationTime(
                                    session.lastTimestamp.toISOString()
                                  )}
                                </span>
                              )}
                            </div>
                            <div className="mt-0.5 flex items-center gap-1">
                              <Badge
                                className="bg-primary/10 text-primary shrink-0 border-0"
                                variant="outline"
                              >
                                <Sparkles className="mr-1 h-2.5 w-2.5" />
                                Asistente IA
                              </Badge>
                            </div>
                            {session.lastMessage && (
                              <p className="text-muted-foreground mt-1 truncate text-sm">
                                {session.lastMessage}
                              </p>
                            )}
                          </div>
                        </div>
                      </button>
                    ))}
                  </div>
                )
              ) : /* ── Mensajes tab (original list) ──────── */
              conversationsLoading ? (
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
                    Limpiar b&uacute;squeda
                  </Button>
                </div>
              )}
            </div>
          </div>

          {/* Chat Area */}
          <div
            className={cn(
              'flex flex-1 flex-col',
              !selectedConversationId && !urlBotConfig && 'hidden md:flex'
            )}
          >
            {/* Case 1: URL-param triggered bot (from vehicle detail "Chat Live") */}
            {urlBotConfig && !selectedConversation ? (
              <DealerBotPanel
                key={`url-${urlBotConfig.dealerId}`}
                dealerId={urlBotConfig.dealerId}
                dealerName={urlBotConfig.dealerName}
                dealerEmail={urlBotConfig.dealerEmail}
                vehicleTitle={urlBotConfig.vehicleTitle}
                onBack={() => setUrlBotConfig(null)}
              />
            ) : !selectedConversation ? (
              <div className="flex flex-1 items-center justify-center bg-gradient-to-br from-gray-50 to-white p-8">
                <div className="text-center">
                  <div className="relative mx-auto mb-6">
                    <div className="bg-primary/10 absolute inset-0 animate-pulse rounded-full blur-2xl" />
                    <div className="to-primary/10 from-primary/10 relative flex h-20 w-20 items-center justify-center rounded-2xl bg-gradient-to-br shadow-inner">
                      <MessageCircle className="text-primary h-10 w-10" />
                    </div>
                  </div>
                  <h2 className="text-foreground mb-2 text-lg font-semibold">
                    Selecciona una conversación
                  </h2>
                  <p className="text-muted-foreground mb-4 max-w-xs text-sm">
                    Elige una conversación de la lista para ver los mensajes
                  </p>
                  <div className="bg-primary/5 text-primary inline-flex items-center gap-2 rounded-full px-4 py-2 text-xs">
                    <span>💬</span>
                    <span>Responde rápido para mejores resultados</span>
                  </div>
                </div>
              </div>
            ) : showBotPanel ? (
              <DealerBotPanel
                key={selectedConversationId!}
                dealerId={selectedConversation.otherUser.id || undefined}
                dealerName={selectedConversation.otherUser.name}
                vehicleTitle={selectedConversation.vehicle.title}
                onBack={() => setShowBotPanel(false)}
              />
            ) : (
              <>
                {/* Chat Header - Premium */}
                <div className="border-border bg-card flex items-center gap-3 border-b p-4 shadow-sm">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="md:hidden"
                    onClick={() => setSelectedConversationId(null)}
                  >
                    <ArrowLeft className="h-5 w-5" />
                  </Button>

                  <div className="bg-muted flex h-10 w-10 items-center justify-center overflow-hidden rounded-full">
                    {selectedConversation.otherUser.avatarUrl ? (
                      <Image
                        src={selectedConversation.otherUser.avatarUrl}
                        alt={selectedConversation.otherUser.name}
                        width={40}
                        height={40}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <User className="text-muted-foreground h-5 w-5" />
                    )}
                  </div>

                  <div className="min-w-0 flex-1">
                    <h3 className="text-foreground truncate font-medium">
                      {selectedConversation.otherUser.name}
                    </h3>
                    {selectedConversation.vehicle.slug ? (
                      <Link
                        href={`/vehiculos/${selectedConversation.vehicle.slug}`}
                        className="hover:text-primary text-muted-foreground block truncate text-sm"
                      >
                        {selectedConversation.vehicle.title}
                      </Link>
                    ) : (
                      <p className="text-muted-foreground truncate text-sm">
                        {selectedConversation.vehicle.title}
                      </p>
                    )}
                  </div>

                  {/* AI Bot button — buyers can query the seller's AI assistant */}
                  {selectedConversation.type === 'inquiry' && (
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => setShowBotPanel(true)}
                      title="Preguntar al Asistente IA del vendedor"
                      className="text-primary hover:bg-primary/10 hover:text-primary"
                    >
                      <Bot className="h-5 w-5" />
                    </Button>
                  )}

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
                      <Card className="to-primary/5 from-primary/5 ring-primary/10 overflow-hidden border-0 bg-gradient-to-r shadow-sm ring-1">
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
                            <div className="bg-muted flex h-12 w-16 items-center justify-center overflow-hidden rounded">
                              {selectedConversation.vehicle.imageUrl ? (
                                <Image
                                  src={selectedConversation.vehicle.imageUrl}
                                  alt={selectedConversation.vehicle.title}
                                  width={64}
                                  height={48}
                                  className="h-full w-full object-cover"
                                />
                              ) : (
                                <Car className="text-muted-foreground h-6 w-6" />
                              )}
                            </div>
                            <div>
                              <p className="text-foreground text-sm font-medium">
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
                        <div className="text-muted-foreground py-8 text-center text-sm">
                          No hay mensajes en esta conversación
                        </div>
                      )}
                      <div ref={messagesEndRef} />
                    </>
                  )}
                </div>

                {/* Input - Premium style */}
                <div className="border-border border-t bg-gradient-to-t from-gray-50/50 to-white p-4">
                  <div className="flex gap-3">
                    <Input
                      type="text"
                      placeholder="Escribe un mensaje..."
                      value={newMessage}
                      onChange={e => setNewMessage(e.target.value)}
                      onKeyDown={handleKeyDown}
                      disabled={sendMutation.isPending}
                      className="border-border bg-card focus:border-primary focus:ring-primary/20 h-12 flex-1 rounded-xl shadow-sm transition-all focus:ring-2"
                    />
                    <Button
                      onClick={handleSend}
                      disabled={!newMessage.trim() || sendMutation.isPending}
                      className="to-primary/80 from-primary shadow-primary/25 hover:shadow-primary/30 h-12 w-12 rounded-xl bg-gradient-to-r shadow-lg transition-all hover:shadow-xl disabled:opacity-50 disabled:shadow-none"
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
