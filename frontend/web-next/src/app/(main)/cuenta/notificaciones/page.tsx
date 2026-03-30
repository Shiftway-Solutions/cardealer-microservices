/**
 * Notifications Page
 *
 * Displays user's notifications with filters and actions
 *
 * Route: /cuenta/notificaciones
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Bell,
  MessageCircle,
  Heart,
  Eye,
  DollarSign,
  AlertCircle,
  Check,
  CheckCheck,
  Trash2,
  Settings,
  Loader2,
  RefreshCw,
  CheckCircle,
  XCircle,
  CreditCard,
  Clock,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import {
  notificationsService,
  type Notification,
  type NotificationType,
} from '@/services/notifications';

// =============================================================================
// ICON MAPPING
// =============================================================================

const iconMap: Record<NotificationType, React.ElementType> = {
  message: MessageCircle,
  favorite: Heart,
  view: Eye,
  price: DollarSign,
  system: AlertCircle,
  inquiry: MessageCircle,
  vehicle_sold: CheckCircle,
  vehicle_approved: CheckCircle,
  vehicle_rejected: XCircle,
  payment_received: CreditCard,
  subscription_expiring: Clock,
};

// =============================================================================
// LOADING STATE
// =============================================================================

function NotificationsLoading() {
  return (
    <Card>
      <div className="divide-y">
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="flex gap-4 p-4">
            <Skeleton className="h-10 w-10 shrink-0 rounded-full" />
            <div className="flex-1">
              <Skeleton className="mb-2 h-4 w-48" />
              <Skeleton className="mb-2 h-3 w-full" />
              <Skeleton className="h-3 w-24" />
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function NotificationsError({ onRetry }: { onRetry: () => void }) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center py-12 text-center">
        <AlertCircle className="mb-4 h-12 w-12 text-red-400" />
        <h3 className="mb-2 font-semibold">Error al cargar notificaciones</h3>
        <p className="text-muted-foreground mb-4 text-sm">
          No se pudieron cargar las notificaciones
        </p>
        <Button variant="outline" onClick={onRetry}>
          <RefreshCw className="mr-2 h-4 w-4" />
          Reintentar
        </Button>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// EMPTY STATE
// =============================================================================

function EmptyState() {
  return (
    <div className="py-16 text-center">
      <div className="bg-muted mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-full">
        <Bell className="text-muted-foreground h-10 w-10" />
      </div>
      <h3 className="text-foreground mb-2 text-lg font-medium">No tienes notificaciones</h3>
      <p className="text-muted-foreground">Las notificaciones sobre tu actividad aparecerán aquí</p>
    </div>
  );
}

// =============================================================================
// NOTIFICATION ITEM
// =============================================================================

interface NotificationItemProps {
  notification: Notification;
  onMarkAsRead: (id: string) => void;
  onDelete: (id: string) => void;
  isDeleting: boolean;
}

function NotificationItem({
  notification,
  onMarkAsRead,
  onDelete,
  isDeleting,
}: NotificationItemProps) {
  const Icon = iconMap[notification.type] || Bell;
  const colorClass = notificationsService.getNotificationColor(notification.type);

  const content = (
    <div
      className={cn(
        'flex gap-4 p-4 transition-all',
        !notification.isRead && 'bg-blue-50/50',
        notification.link && 'hover:bg-muted/50',
        isDeleting && 'opacity-50'
      )}
    >
      {/* Icon */}
      <div
        className={cn(
          'flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full',
          colorClass
        )}
      >
        <Icon className="h-5 w-5" />
      </div>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-2">
          <div>
            <h4
              className={cn(
                notification.isRead
                  ? 'text-muted-foreground font-medium'
                  : 'text-foreground font-semibold'
              )}
            >
              {notification.title}
            </h4>
            <p className="text-muted-foreground mt-0.5 text-sm">{notification.message}</p>
          </div>
          <span className="text-muted-foreground flex-shrink-0 text-xs">
            {notificationsService.formatNotificationTime(notification.createdAt)}
          </span>
        </div>

        {/* Actions */}
        <div className="mt-2 flex items-center gap-3">
          {!notification.isRead && (
            <button
              onClick={e => {
                e.preventDefault();
                e.stopPropagation();
                onMarkAsRead(notification.id);
              }}
              className="text-primary hover:text-primary/80 flex items-center gap-1 text-xs font-medium"
            >
              <Check className="h-3 w-3" />
              Marcar como leída
            </button>
          )}
          <button
            onClick={e => {
              e.preventDefault();
              e.stopPropagation();
              onDelete(notification.id);
            }}
            disabled={isDeleting}
            className="text-muted-foreground flex items-center gap-1 text-xs hover:text-red-600"
          >
            {isDeleting ? (
              <Loader2 className="h-3 w-3 animate-spin" />
            ) : (
              <Trash2 className="h-3 w-3" />
            )}
            Eliminar
          </button>
        </div>
      </div>

      {/* Unread indicator */}
      {!notification.isRead && (
        <div className="bg-primary mt-2 h-2 w-2 flex-shrink-0 rounded-full" />
      )}
    </div>
  );

  if (notification.link) {
    return (
      <Link href={notification.link} className="border-border block border-b last:border-0">
        {content}
      </Link>
    );
  }

  return <div className="border-border border-b last:border-0">{content}</div>;
}

// =============================================================================
// MAIN PAGE
// =============================================================================

type FilterType = 'all' | 'unread';

export default function NotificationsPage() {
  const queryClient = useQueryClient();

  const [filter, setFilter] = React.useState<FilterType>('all');
  const [deletingId, setDeletingId] = React.useState<string | null>(null);

  // Fetch notifications
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['notifications', filter === 'unread'],
    queryFn: () =>
      notificationsService.getNotifications({
        unreadOnly: filter === 'unread',
      }),
    staleTime: 1000 * 30, // 30 seconds
  });

  // Mark as read mutation
  const markAsReadMutation = useMutation({
    mutationFn: notificationsService.markAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
    onError: () => {
      toast.error('Error al marcar como leída');
    },
  });

  // Mark all as read mutation
  const markAllAsReadMutation = useMutation({
    mutationFn: notificationsService.markAllAsRead,
    onSuccess: () => {
      toast.success('Todas marcadas como leídas');
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
    onError: () => {
      toast.error('Error al marcar todas');
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      setDeletingId(id);
      return notificationsService.deleteNotification(id);
    },
    onSuccess: () => {
      toast.success('Notificación eliminada');
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
    onError: () => {
      toast.error('Error al eliminar');
    },
    onSettled: () => {
      setDeletingId(null);
    },
  });

  // Delete all mutation
  const deleteAllMutation = useMutation({
    mutationFn: notificationsService.deleteAllNotifications,
    onSuccess: () => {
      toast.success('Todas las notificaciones eliminadas');
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
    onError: () => {
      toast.error('Error al eliminar todas');
    },
  });

  const notifications = data?.notifications || [];
  const unreadCount = data?.unreadCount || 0;

  // Loading state
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="mb-2 h-8 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
        <NotificationsLoading />
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Notificaciones</h1>
        </div>
        <NotificationsError onRetry={refetch} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Notificaciones</h1>
          <p className="text-muted-foreground">
            {unreadCount > 0 ? `${unreadCount} sin leer` : 'Estás al día'}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link href="/cuenta/configuracion#notificaciones">
              <Settings className="mr-2 h-4 w-4" />
              Configurar
            </Link>
          </Button>
        </div>
      </div>

      {/* Filters & Actions */}
      {notifications.length > 0 && (
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          {/* Tabs */}
          <div className="flex gap-2">
            <Button
              variant={filter === 'all' ? 'default' : 'outline'}
              size="sm"
              onClick={() => setFilter('all')}
            >
              Todas
            </Button>
            <Button
              variant={filter === 'unread' ? 'default' : 'outline'}
              size="sm"
              onClick={() => setFilter('unread')}
            >
              Sin leer
              {unreadCount > 0 && (
                <span
                  className={cn(
                    'ml-1.5 rounded-full px-1.5 py-0.5 text-xs',
                    filter === 'unread' ? 'bg-white/20' : 'bg-primary/10 text-primary'
                  )}
                >
                  {unreadCount}
                </span>
              )}
            </Button>
          </div>

          {/* Bulk Actions */}
          <div className="flex gap-2">
            {unreadCount > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => markAllAsReadMutation.mutate()}
                disabled={markAllAsReadMutation.isPending}
              >
                {markAllAsReadMutation.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <CheckCheck className="mr-2 h-4 w-4" />
                )}
                Marcar todas como leídas
              </Button>
            )}
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-red-600 hover:bg-red-50 hover:text-red-700"
                  disabled={deleteAllMutation.isPending}
                >
                  {deleteAllMutation.isPending ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Trash2 className="mr-2 h-4 w-4" />
                  )}
                  Eliminar todas
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>¿Eliminar todas las notificaciones?</AlertDialogTitle>
                  <AlertDialogDescription>
                    Esta acción no se puede deshacer. Se eliminarán todas tus notificaciones.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancelar</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={() => deleteAllMutation.mutate()}
                    className="bg-red-600 hover:bg-red-700"
                  >
                    Eliminar todas
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </div>
        </div>
      )}

      {/* Notifications List */}
      {notifications.length > 0 ? (
        <Card className="overflow-hidden">
          <div>
            {notifications.map(notification => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onMarkAsRead={id => markAsReadMutation.mutate(id)}
                onDelete={id => deleteMutation.mutate(id)}
                isDeleting={deletingId === notification.id}
              />
            ))}
          </div>
        </Card>
      ) : filter === 'unread' ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Check className="mx-auto mb-3 h-12 w-12 text-green-500" />
            <h3 className="text-foreground mb-1 font-medium">Todo al día</h3>
            <p className="text-muted-foreground">No tienes notificaciones sin leer</p>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent>
            <EmptyState />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
