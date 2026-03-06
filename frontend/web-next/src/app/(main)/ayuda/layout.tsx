import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Centro de Ayuda | OKLA - Marketplace de Vehículos en RD',
  description:
    'Centro de ayuda de OKLA. Encuentra respuestas a tus preguntas sobre compra, venta, pagos, seguridad y más.',
  keywords: [
    'ayuda OKLA',
    'preguntas frecuentes',
    'soporte marketplace',
    'cómo vender carro',
    'cómo comprar carro RD',
    'ayuda pagos',
  ],
  openGraph: {
    title: 'Centro de Ayuda | OKLA',
    description: 'Encuentra respuestas a todas tus preguntas sobre OKLA.',
    url: 'https://okla.com.do/ayuda',
  },
};

export default function AyudaLayout({ children }: { children: React.ReactNode }) {
  return children;
}
