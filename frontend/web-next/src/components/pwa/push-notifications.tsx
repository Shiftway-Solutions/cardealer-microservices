'use client';

import { useCallback, useState } from 'react';

interface PushNotificationConfig {
  /** VAPID public key for web push */
  vapidPublicKey?: string;
  /** API endpoint to register push subscription */
  subscribeEndpoint?: string;
  /** API endpoint to unregister push subscription */
  unsubscribeEndpoint?: string;
}

interface UsePushNotificationsReturn {
  /** Whether push notifications are supported */
  isSupported: boolean;
  /** Whether the user has granted permission */
  isPermissionGranted: boolean;
  /** Whether push is currently subscribed */
  isSubscribed: boolean;
  /** Request notification permission */
  requestPermission: () => Promise<NotificationPermission>;
  /** Subscribe to push notifications */
  subscribe: () => Promise<PushSubscription | null>;
  /** Unsubscribe from push notifications */
  unsubscribe: () => Promise<boolean>;
  /** Current push subscription */
  subscription: PushSubscription | null;
  /** Loading state */
  isLoading: boolean;
  /** Error message */
  error: string | null;
}

/**
 * Hook for managing push notifications
 */
export function usePushNotifications(
  config: PushNotificationConfig = {}
): UsePushNotificationsReturn {
  const {
    vapidPublicKey = process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY,
    subscribeEndpoint = '/api/push/subscribe',
    unsubscribeEndpoint = '/api/push/unsubscribe',
  } = config;

  const [subscription, setSubscription] = useState<PushSubscription | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isSupported =
    typeof window !== 'undefined' &&
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window;

  const isPermissionGranted =
    typeof window !== 'undefined' &&
    'Notification' in window &&
    Notification.permission === 'granted';

  const requestPermission = useCallback(async (): Promise<NotificationPermission> => {
    if (!isSupported) {
      setError('Push notifications are not supported in this browser');
      return 'denied';
    }

    try {
      const permission = await Notification.requestPermission();
      return permission;
    } catch (err) {
      setError('Failed to request notification permission');
      return 'denied';
    }
  }, [isSupported]);

  const subscribe = useCallback(async (): Promise<PushSubscription | null> => {
    if (!isSupported) {
      setError('Push notifications are not supported');
      return null;
    }

    if (!vapidPublicKey) {
      setError('VAPID public key is not configured');
      return null;
    }

    setIsLoading(true);
    setError(null);

    try {
      // Request permission if not granted
      if (Notification.permission !== 'granted') {
        const permission = await requestPermission();
        if (permission !== 'granted') {
          setError('Notification permission denied');
          setIsLoading(false);
          return null;
        }
      }

      // Get service worker registration
      const registration = await navigator.serviceWorker.ready;

      // Check if already subscribed
      const existingSubscription = await registration.pushManager.getSubscription();
      if (existingSubscription) {
        setSubscription(existingSubscription);
        setIsLoading(false);
        return existingSubscription;
      }

      // Convert VAPID key to Uint8Array
      const applicationServerKey = urlBase64ToUint8Array(vapidPublicKey);

      // Subscribe to push
      const newSubscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: applicationServerKey.buffer as ArrayBuffer,
      });

      // Send subscription to server
      await fetch(subscribeEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          subscription: newSubscription.toJSON(),
        }),
      });

      setSubscription(newSubscription);
      setIsLoading(false);
      return newSubscription;
    } catch (err) {
      console.error('[Push] Failed to subscribe:', err);
      setError('Failed to subscribe to push notifications');
      setIsLoading(false);
      return null;
    }
  }, [isSupported, vapidPublicKey, subscribeEndpoint, requestPermission]);

  const unsubscribe = useCallback(async (): Promise<boolean> => {
    if (!subscription) {
      return true;
    }

    setIsLoading(true);
    setError(null);

    try {
      // Unsubscribe from push
      await subscription.unsubscribe();

      // Notify server
      await fetch(unsubscribeEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          endpoint: subscription.endpoint,
        }),
      });

      setSubscription(null);
      setIsLoading(false);
      return true;
    } catch (err) {
      console.error('[Push] Failed to unsubscribe:', err);
      setError('Failed to unsubscribe from push notifications');
      setIsLoading(false);
      return false;
    }
  }, [subscription, unsubscribeEndpoint]);

  return {
    isSupported,
    isPermissionGranted,
    isSubscribed: !!subscription,
    requestPermission,
    subscribe,
    unsubscribe,
    subscription,
    isLoading,
    error,
  };
}

/**
 * Convert a base64 string to Uint8Array (for VAPID key)
 */
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');

  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);

  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }

  return outputArray;
}

/**
 * Push notification toggle component
 */
interface PushNotificationToggleProps {
  className?: string;
}

export function PushNotificationToggle({ className = '' }: PushNotificationToggleProps) {
  const {
    isSupported,
    isPermissionGranted,
    isSubscribed,
    subscribe,
    unsubscribe,
    isLoading,
    error,
  } = usePushNotifications();

  const handleToggle = async () => {
    if (isSubscribed) {
      await unsubscribe();
    } else {
      await subscribe();
    }
  };

  if (!isSupported) {
    return null;
  }

  return (
    <div className={`flex items-center justify-between ${className}`}>
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted">
          <svg
            className={`h-5 w-5 ${isSubscribed ? 'text-green-600' : 'text-muted-foreground'}`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
            />
          </svg>
        </div>
        <div>
          <p className="font-medium text-foreground">Notificaciones</p>
          <p className="text-sm text-muted-foreground">
            {isSubscribed ? 'Recibirás alertas de nuevos vehículos' : 'Activa para recibir alertas'}
          </p>
        </div>
      </div>

      <button
        onClick={handleToggle}
        disabled={isLoading || (!isPermissionGranted && !isSubscribed)}
        className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${isSubscribed ? 'bg-green-600' : 'bg-muted'} ${isLoading ? 'cursor-not-allowed opacity-50' : 'cursor-pointer'} `}
        aria-pressed={isSubscribed}
        role="switch"
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform ${isSubscribed ? 'translate-x-6' : 'translate-x-1'} `}
        />
      </button>

      {error && <p className="mt-1 text-xs text-red-500">{error}</p>}
    </div>
  );
}

export default PushNotificationToggle;
