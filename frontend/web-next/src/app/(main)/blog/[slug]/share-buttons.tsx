'use client';

import * as React from 'react';
import { Share2, Link2, Check, MessageCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';

interface ShareButtonsProps {
  title: string;
  slug: string;
}

export function ShareButtons({ title, slug }: ShareButtonsProps) {
  const [copied, setCopied] = React.useState(false);

  const fullUrl =
    typeof window !== 'undefined'
      ? `${window.location.origin}/blog/${slug}`
      : `https://okla.com.do/blog/${slug}`;

  const handleCopyLink = async () => {
    try {
      await navigator.clipboard.writeText(fullUrl);
      setCopied(true);
      toast.success('¡Link copiado al portapapeles!');
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast.error('No se pudo copiar el link');
    }
  };

  const handleWhatsApp = () => {
    const text = encodeURIComponent(`${title}\n\n${fullUrl}`);
    window.open(`https://wa.me/?text=${text}`, '_blank', 'noopener,noreferrer');
  };

  return (
    <div className="flex flex-wrap items-center gap-3">
      <span className="text-muted-foreground flex items-center gap-1.5 text-sm font-medium">
        <Share2 className="h-4 w-4" />
        Compartir:
      </span>
      <Button variant="outline" size="sm" onClick={handleCopyLink} className="gap-1.5">
        {copied ? (
          <Check className="h-3.5 w-3.5 text-green-600" />
        ) : (
          <Link2 className="h-3.5 w-3.5" />
        )}
        {copied ? 'Copiado' : 'Copiar Link'}
      </Button>
      <Button
        variant="outline"
        size="sm"
        onClick={handleWhatsApp}
        className="gap-1.5 text-green-600 hover:bg-green-50 hover:text-green-700"
      >
        <MessageCircle className="h-3.5 w-3.5" />
        WhatsApp
      </Button>
    </div>
  );
}
