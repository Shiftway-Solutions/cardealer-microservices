/**
 * Admin — Advertising Spaces Management
 *
 * Audit & configure all advertising spaces on the OKLA platform.
 * Each space has configurable points value and display duration.
 */

import { Metadata } from 'next';
import { AdSpacesManager } from './ad-spaces-client';

export const metadata: Metadata = {
  title: 'Espacios Publicitarios — Admin OKLA',
  description: 'Gestiona y audita todos los espacios publicitarios de la plataforma OKLA.',
};

export default function AdminAdSpacesPage() {
  return <AdSpacesManager />;
}
