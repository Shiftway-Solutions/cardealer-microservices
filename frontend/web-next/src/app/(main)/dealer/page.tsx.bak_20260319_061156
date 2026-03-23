'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

/**
 * /dealer root → redirects to /dealer/dashboard
 * The canonical dealer dashboard lives at /dealer/dashboard.
 */
export default function DealerRootPage() {
  const router = useRouter();

  useEffect(() => {
    router.replace('/dealer/dashboard');
  }, [router]);

  return null;
}
