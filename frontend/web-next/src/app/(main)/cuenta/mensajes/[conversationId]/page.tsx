/**
 * Conversation Detail Redirect
 *
 * Redirects from legacy route to the unified messaging layout.
 * Old: /cuenta/mensajes/[conversationId] → New: /mensajes?conversation=[conversationId]
 */

'use client';

import { useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { Loader2 } from 'lucide-react';

export default function ConversationRedirect() {
  const router = useRouter();
  const params = useParams();
  const conversationId = params.conversationId as string;

  useEffect(() => {
    router.replace(`/mensajes?conversation=${encodeURIComponent(conversationId)}`);
  }, [router, conversationId]);

  return (
    <div className="flex min-h-[400px] items-center justify-center">
      <div className="text-center">
        <Loader2 className="mx-auto h-8 w-8 animate-spin text-blue-600" />
        <p className="text-muted-foreground mt-4">Redirigiendo a la conversación...</p>
      </div>
    </div>
  );
}
