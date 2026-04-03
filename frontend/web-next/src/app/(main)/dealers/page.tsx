import { Metadata } from 'next';
import { DealersPageClient } from './dealers-page-client';

export const metadata: Metadata = {
  title: 'Para Dealers - Plataforma de Venta de Vehículos',
  description:
    'Herramientas profesionales para concesionarios en República Dominicana. Dashboard, CRM, analytics y más para crecer con OKLA.',
  openGraph: {
    title: 'OKLA para Dealers - Vende más vehículos',
    description: 'Herramientas profesionales para concesionarios',
  },
};

export default function DealersPage() {
  return <DealersPageClient />;
}
