import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Buscar Vehículos',
  description:
    'Busca entre miles de vehículos en República Dominicana. Filtra por marca, modelo, año, precio, ubicación y más. ¡Encuentra tu carro ideal hoy en OKLA!',
  alternates: {
    canonical: 'https://okla.com.do/buscar',
  },
  robots: {
    index: true,
    follow: true,
    // Prevent indexing of filtered/paginated search results
    // Crawlers should only index the base /buscar page
    googleBot: {
      index: true,
      follow: true,
      'max-snippet': -1,
      'max-image-preview': 'large',
    },
  },
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
    description:
      'Encuentra tu próximo vehículo entre miles de opciones en República Dominicana. ¡Contáctanos hoy!',
    url: 'https://okla.com.do/buscar',
    siteName: 'OKLA',
    locale: 'es_DO',
  },
};

export default function BuscarLayout({ children }: { children: React.ReactNode }) {
  return children;
}
