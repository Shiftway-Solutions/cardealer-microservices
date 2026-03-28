/**
 * 360° Viewer Layout
 *
 * INDEXATION FIX: noindex for 360° pages — they are interactive client-only
 * experiences with no SEO value. Prevents crawl budget waste on duplicate content.
 */

import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Vista 360°',
  robots: {
    index: false,
    follow: false,
    googleBot: {
      index: false,
      follow: false,
    },
  },
};

export default function Layout360({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}
