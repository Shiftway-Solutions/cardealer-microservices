import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Comparar Vehículos',
  description:
    'Compara hasta 3 vehículos lado a lado. Especificaciones, precios, características y valoración de mercado para tomar la mejor decisión.',
  keywords: [
    'comparar carros',
    'comparar vehículos RD',
    'comparación autos',
    'Toyota vs Honda',
    'SUV comparación',
    'mejor carro República Dominicana',
  ],
  openGraph: {
    title: 'Comparar Vehículos | OKLA',
    description: 'Compara vehículos lado a lado para tomar la mejor decisión de compra.',
    url: 'https://okla.com.do/comparar',
  },
};

export default function CompararLayout({ children }: { children: React.ReactNode }) {
  return children;
}
