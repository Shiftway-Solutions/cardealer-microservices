import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Mantenimiento',
  description: 'Estamos realizando mejoras en el sitio. Volveremos pronto.',
  robots: 'noindex, nofollow',
};

export default function MantenimientoLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="via-primary min-h-screen bg-gradient-to-br from-slate-900 to-slate-900">
      {children}
    </div>
  );
}
