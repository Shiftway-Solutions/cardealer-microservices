import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Contacto | OKLA - Marketplace de Vehículos en RD',
  description:
    'Contáctanos para cualquier consulta sobre compra o venta de vehículos en República Dominicana. Soporte por email, teléfono y chat.',
  keywords: [
    'contacto OKLA',
    'soporte vehículos',
    'ayuda compra carros',
    'contacto marketplace autos RD',
  ],
  openGraph: {
    title: 'Contacto | OKLA',
    description:
      'Estamos aquí para ayudarte. Contáctanos para cualquier consulta sobre vehículos en RD.',
    url: 'https://okla.com.do/contacto',
  },
};

export default function ContactoLayout({ children }: { children: React.ReactNode }) {
  return children;
}
