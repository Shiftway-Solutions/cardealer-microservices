/**
 * Admin Subscription Plans Management Page
 *
 * UI to configure subscription plan features and build plan tiers.
 * Allows admins to:
 * - Define features/capabilities
 * - Assign features to plans
 * - Set pricing and limits
 * - Configure feature gates
 */

import { Metadata } from 'next';
import { SubscriptionPlanManager } from './plan-manager-client';

export const metadata: Metadata = {
  title: 'Gestión de Planes de Suscripción — Admin OKLA',
  description: 'Administra los planes de suscripción, funcionalidades y precios de la plataforma.',
};

export default function AdminSubscriptionPlansPage() {
  return <SubscriptionPlanManager />;
}
