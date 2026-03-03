'use client';

/**
 * Google Analytics 4 Integration
 *
 * Provides:
 * - Page view tracking (automatic with route changes)
 * - Custom event tracking
 * - E-commerce tracking
 * - User properties
 */

import Script from 'next/script';
import { usePathname, useSearchParams } from 'next/navigation';
import { useEffect, Suspense } from 'react';

// GA4 Measurement ID from environment
const GA_MEASUREMENT_ID = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID;

// Declare gtag on window
declare global {
  interface Window {
    gtag: (
      command: 'config' | 'event' | 'set' | 'js',
      targetId: string | Date,
      params?: Record<string, unknown>
    ) => void;
    dataLayer: unknown[];
  }
}

// =============================================================================
// GOOGLE ANALYTICS SCRIPT COMPONENT
// =============================================================================

/**
 * Google Analytics Script Tags
 * Add this component to your root layout
 */
export function GoogleAnalytics() {
  if (!GA_MEASUREMENT_ID) {
    return null;
  }

  return (
    <>
      <Script
        src={`https://www.googletagmanager.com/gtag/js?id=${GA_MEASUREMENT_ID}`}
        strategy="afterInteractive"
      />
      <Script id="google-analytics" strategy="afterInteractive">
        {`
          window.dataLayer = window.dataLayer || [];
          function gtag(){dataLayer.push(arguments);}
          gtag('js', new Date());
          gtag('config', '${GA_MEASUREMENT_ID}', {
            page_path: window.location.pathname,
            send_page_view: false
          });
        `}
      </Script>
      <Suspense fallback={null}>
        <AnalyticsPageView />
      </Suspense>
    </>
  );
}

/**
 * Component that tracks page views on route changes
 */
function AnalyticsPageView() {
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    if (!GA_MEASUREMENT_ID) return;

    const url = pathname + (searchParams?.toString() ? `?${searchParams.toString()}` : '');

    // Track page view
    window.gtag?.('config', GA_MEASUREMENT_ID, {
      page_path: url,
      page_title: document.title,
    });
  }, [pathname, searchParams]);

  return null;
}

// =============================================================================
// EVENT TRACKING FUNCTIONS
// =============================================================================

/**
 * Track a custom event in GA4
 */
export function trackEvent(eventName: string, params?: Record<string, unknown>) {
  if (!GA_MEASUREMENT_ID || typeof window === 'undefined') return;

  window.gtag?.('event', eventName, params);
}

/**
 * Track vehicle view
 */
export function trackVehicleView(vehicle: {
  id: string;
  make: string;
  model: string;
  year: number;
  price: number;
  sellerType?: 'dealer' | 'seller';
}) {
  trackEvent('view_item', {
    currency: 'DOP',
    value: vehicle.price,
    items: [
      {
        item_id: vehicle.id,
        item_name: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
        item_brand: vehicle.make,
        item_category: 'Vehicles',
        item_category2: vehicle.model,
        price: vehicle.price,
      },
    ],
  });

  // Custom event for vehicle views
  trackEvent('vehicle_view', {
    vehicle_id: vehicle.id,
    vehicle_make: vehicle.make,
    vehicle_model: vehicle.model,
    vehicle_year: vehicle.year,
    vehicle_price: vehicle.price,
    seller_type: vehicle.sellerType,
  });
}

/**
 * Track search
 */
export function trackSearch(params: {
  searchTerm?: string;
  make?: string;
  model?: string;
  minPrice?: number;
  maxPrice?: number;
  resultsCount?: number;
}) {
  trackEvent('search', {
    search_term: params.searchTerm || `${params.make || ''} ${params.model || ''}`.trim(),
    ...params,
  });
}

/**
 * Track contact seller
 */
export function trackContactSeller(vehicle: {
  id: string;
  make: string;
  model: string;
  price: number;
  contactMethod: 'whatsapp' | 'call' | 'message' | 'email';
}) {
  trackEvent('contact_seller', {
    vehicle_id: vehicle.id,
    vehicle_name: `${vehicle.make} ${vehicle.model}`,
    vehicle_price: vehicle.price,
    contact_method: vehicle.contactMethod,
  });

  // Also track as lead
  trackEvent('generate_lead', {
    currency: 'DOP',
    value: vehicle.price * 0.03, // Estimated lead value
    vehicle_id: vehicle.id,
  });
}

/**
 * Track favorite/save vehicle
 */
export function trackFavorite(vehicle: {
  id: string;
  make: string;
  model: string;
  price: number;
  action: 'add' | 'remove';
}) {
  trackEvent('add_to_wishlist', {
    currency: 'DOP',
    value: vehicle.price,
    items: [
      {
        item_id: vehicle.id,
        item_name: `${vehicle.make} ${vehicle.model}`,
        price: vehicle.price,
      },
    ],
  });

  trackEvent('favorite_vehicle', {
    vehicle_id: vehicle.id,
    action: vehicle.action,
  });
}

/**
 * Track vehicle listing creation
 */
export function trackListingCreated(listing: {
  id: string;
  make: string;
  model: string;
  year: number;
  price: number;
  listingType: 'free' | 'premium' | 'boost';
}) {
  trackEvent('listing_created', {
    listing_id: listing.id,
    vehicle_make: listing.make,
    vehicle_model: listing.model,
    vehicle_year: listing.year,
    listing_price: listing.price,
    listing_type: listing.listingType,
  });
}

/**
 * Track checkout/purchase events
 */
export function trackPurchase(transaction: {
  transactionId: string;
  value: number;
  currency?: string;
  items: Array<{
    id: string;
    name: string;
    price: number;
    quantity?: number;
  }>;
}) {
  trackEvent('purchase', {
    transaction_id: transaction.transactionId,
    currency: transaction.currency || 'DOP',
    value: transaction.value,
    items: transaction.items.map(item => ({
      item_id: item.id,
      item_name: item.name,
      price: item.price,
      quantity: item.quantity || 1,
    })),
  });
}

/**
 * Track dealer subscription
 */
export function trackDealerSubscription(subscription: {
  dealerId: string;
  plan: 'libre' | 'visible' | 'pro' | 'elite';
  value: number;
  isEarlyBird?: boolean;
}) {
  trackEvent('dealer_subscription', {
    dealer_id: subscription.dealerId,
    plan: subscription.plan,
    value: subscription.value,
    is_early_bird: subscription.isEarlyBird,
  });

  trackEvent('purchase', {
    transaction_id: `sub_${subscription.dealerId}_${Date.now()}`,
    currency: 'USD',
    value: subscription.value,
    items: [
      {
        item_id: `plan_${subscription.plan}`,
        item_name: `Dealer Plan ${subscription.plan}`,
        price: subscription.value,
      },
    ],
  });
}

/**
 * Track seller subscription
 */
export function trackSellerSubscription(subscription: {
  sellerId: string;
  plan: 'gratis' | 'premium' | 'pro';
  value: number;
}) {
  trackEvent('seller_subscription', {
    seller_id: subscription.sellerId,
    plan: subscription.plan,
    value: subscription.value,
  });

  if (subscription.value > 0) {
    trackEvent('purchase', {
      transaction_id: `seller_sub_${subscription.sellerId}_${Date.now()}`,
      currency: 'DOP',
      value: subscription.value,
      items: [
        {
          item_id: `seller_plan_${subscription.plan}`,
          item_name: `Seller Plan ${subscription.plan}`,
          price: subscription.value,
        },
      ],
    });
  }
}

/**
 * Track user sign up
 */
export function trackSignUp(method: 'email' | 'google' | 'facebook') {
  trackEvent('sign_up', {
    method,
  });
}

/**
 * Track user login
 */
export function trackLogin(method: 'email' | 'google' | 'facebook') {
  trackEvent('login', {
    method,
  });
}

/**
 * Track share
 */
export function trackShare(content: {
  contentType: 'vehicle' | 'dealer' | 'article';
  contentId: string;
  method: 'whatsapp' | 'facebook' | 'twitter' | 'copy_link' | 'email';
}) {
  trackEvent('share', {
    content_type: content.contentType,
    item_id: content.contentId,
    method: content.method,
  });
}

/**
 * Track dealer page view
 */
export function trackDealerView(dealer: { id: string; name: string; vehicleCount?: number }) {
  trackEvent('dealer_view', {
    dealer_id: dealer.id,
    dealer_name: dealer.name,
    vehicle_count: dealer.vehicleCount,
  });
}

/**
 * Track comparison
 */
export function trackComparison(vehicles: Array<{ id: string; make: string; model: string }>) {
  trackEvent('vehicle_comparison', {
    vehicle_count: vehicles.length,
    vehicles: vehicles.map(v => `${v.make} ${v.model}`).join(', '),
    vehicle_ids: vehicles.map(v => v.id).join(','),
  });
}

/**
 * Set user properties
 */
export function setUserProperties(properties: {
  userId?: string;
  userType?:
    | 'buyer'
    | 'seller'
    | 'dealer'
    | 'dealer_employee'
    | 'admin'
    | 'platform_employee'
    | 'guest';
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
  isVerified?: boolean;
  signUpDate?: string;
}) {
  if (!GA_MEASUREMENT_ID || typeof window === 'undefined') return;

  window.gtag?.('set', 'user_properties', properties);

  if (properties.userId) {
    window.gtag?.('config', GA_MEASUREMENT_ID, {
      user_id: properties.userId,
    });
  }
}

// =============================================================================
// HOOK FOR EASY ACCESS
// =============================================================================

/**
 * Hook to access analytics functions
 */
export function useAnalytics() {
  return {
    trackEvent,
    trackVehicleView,
    trackSearch,
    trackContactSeller,
    trackFavorite,
    trackListingCreated,
    trackPurchase,
    trackDealerSubscription,
    trackSellerSubscription,
    trackSignUp,
    trackLogin,
    trackShare,
    trackDealerView,
    trackComparison,
    setUserProperties,
  };
}

export default GoogleAnalytics;
