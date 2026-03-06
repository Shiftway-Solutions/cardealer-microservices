import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Buscar Vehículos | OKLA - Marketplace de Vehículos en RD',
  description:
    'Busca entre miles de vehículos en República Dominicana. Filtra por marca, modelo, año, precio, ubicación y más. Búsqueda inteligente con IA.',
  keywords: [
    'buscar carros',
    'vehículos en venta RD',
    'autos Santo Domingo',
    'carros usados República Dominicana',
    'buscar Toyota',
    'buscar Honda',
    'SUV en venta',
  ],
  openGraph: {
    title: 'Buscar Vehículos | OKLA',
    description: 'Encuentra tu próximo vehículo entre miles de opciones en República Dominicana.',
    url: 'https://okla.com.do/buscar',
  },
};

export default function BuscarLayout({ children }: { children: React.ReactNode }) {
  return children;
}
