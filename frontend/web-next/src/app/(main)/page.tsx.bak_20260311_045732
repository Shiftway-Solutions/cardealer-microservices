/**
 * OKLA Homepage — Server Component
 *
 * Fetches homepage data server-side for fast First Contentful Paint.
 * Interactive components are rendered as client islands via HomepageClient.
 *
 * Performance: SSR sends fully-rendered HTML — critical for users in
 * Dominican Republic on mobile connections (~10-15 Mbps).
 */

import { Suspense } from 'react';
import Image from 'next/image';
import HomepageClient from './homepage-client';
import { LoadingSection } from '@/components/homepage';

// =============================================
// CWV P0-1 FIX: Hero preload is now homepage-only (moved from root layout)
// CWV P0-2 FIX: SSR hero section so Googlebot and browsers see the
// <img> tag in the initial HTML, drastically improving LCP.
// =============================================

export default function HomePage() {
  return (
    <>
      {/* CWV P0-2: SSR the hero background image so it's in the initial HTML.
          This fixes LCP: browser discovers the image during HTML parse, not after JS hydration.
          The HomepageClient hero will overlay this with interactive search. */}
      <div className="relative -mt-16 h-[340px] w-full overflow-hidden sm:h-[380px] md:h-[420px]">
        <Image
          src="/hero-bg.jpg"
          alt="OKLA — Marketplace de vehículos #1 en República Dominicana"
          fill
          priority
          fetchPriority="high"
          quality={75}
          sizes="100vw"
          className="object-cover"
          placeholder="blur"
          blurDataURL="data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBAUEBAYFBQUGBgYHCQ4JCQgICRINDQoOFRIWFhUSFBQXGiEcFxgfGRQUHScdHyIjJSUlFhwpLCgkKyEkJST/2wBDAQYGBgkICREJCREkGBQYJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCT/wAARCAAIABADAREAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAABgcI/8QAIxAAAQMEAgIDAAAAAAAAAAAAAQIDEQAEBQYSIUETMVFh/8QAFQEBAQAAAAAAAAAAAAAAAAAABAX/xAAeEQACAgEFAQAAAAAAAAAAAAABAgADERIhMUFRYf/aAAwDAQACEQMRAD8AL12+z/I5m8tW9pxWPtUlpXjYlKipJBmZmAD9e6Wkfn7K8gfXOMB0txyF//Z"
        />
        {/* SSR heading for LCP text fallback */}
        <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 px-4 text-center">
          <h1 className="text-3xl font-bold tracking-tight text-white sm:text-4xl md:text-5xl">
            Encuentra tu próximo vehículo en OKLA
          </h1>
          <p className="mt-3 max-w-2xl text-base text-white/90 sm:text-lg">
            El marketplace de vehículos #1 en República Dominicana
          </p>
        </div>
      </div>

      <Suspense fallback={<LoadingSection />}>
        <HomepageClient />
      </Suspense>
    </>
  );
}
