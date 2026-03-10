#!/usr/bin/env tsx
/**
 * Weekly Brand/Model SEO Content Refresh Script
 *
 * Purpose: Revalidate all /marcas/* pages via ISR on-demand revalidation
 * and log real inventory stats per brand/model combination.
 *
 * Designed to run as a cron job or GitHub Action every Monday at 06:00 AST.
 *
 * Usage:
 *   pnpm tsx scripts/weekly-seo-refresh.ts
 *
 * Environment:
 *   NEXT_PUBLIC_API_URL — Backend API base URL (default: http://localhost:8080)
 *   REVALIDATION_SECRET — Secret for /api/revalidate endpoint
 *   SITE_URL — Production site URL (default: https://okla.com.do)
 */

const BASE_API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
const BASE_SITE_URL = process.env.SITE_URL || 'https://okla.com.do';
const REVALIDATION_SECRET = process.env.REVALIDATION_SECRET || '';

// ── All brand/model combinations (synced with page.tsx + sitemap.ts) ────────
const POPULAR_COMBINATIONS: Array<{ make: string; model: string }> = [
  // Toyota (14)
  { make: 'toyota', model: 'corolla' },
  { make: 'toyota', model: 'camry' },
  { make: 'toyota', model: 'rav4' },
  { make: 'toyota', model: 'hilux' },
  { make: 'toyota', model: 'fortuner' },
  { make: 'toyota', model: 'land-cruiser' },
  { make: 'toyota', model: 'prado' },
  { make: 'toyota', model: 'yaris' },
  { make: 'toyota', model: 'c-hr' },
  { make: 'toyota', model: 'highlander' },
  { make: 'toyota', model: 'tacoma' },
  { make: 'toyota', model: '4runner' },
  { make: 'toyota', model: 'tundra' },
  { make: 'toyota', model: 'sequoia' },
  // Honda (8)
  { make: 'honda', model: 'civic' },
  { make: 'honda', model: 'accord' },
  { make: 'honda', model: 'cr-v' },
  { make: 'honda', model: 'hr-v' },
  { make: 'honda', model: 'pilot' },
  { make: 'honda', model: 'odyssey' },
  { make: 'honda', model: 'fit' },
  { make: 'honda', model: 'ridgeline' },
  // Hyundai (8)
  { make: 'hyundai', model: 'elantra' },
  { make: 'hyundai', model: 'sonata' },
  { make: 'hyundai', model: 'tucson' },
  { make: 'hyundai', model: 'santa-fe' },
  { make: 'hyundai', model: 'accent' },
  { make: 'hyundai', model: 'kona' },
  { make: 'hyundai', model: 'palisade' },
  { make: 'hyundai', model: 'creta' },
  // Kia (7)
  { make: 'kia', model: 'sportage' },
  { make: 'kia', model: 'sorento' },
  { make: 'kia', model: 'forte' },
  { make: 'kia', model: 'seltos' },
  { make: 'kia', model: 'rio' },
  { make: 'kia', model: 'soul' },
  { make: 'kia', model: 'carnival' },
  // Nissan (8)
  { make: 'nissan', model: 'sentra' },
  { make: 'nissan', model: 'altima' },
  { make: 'nissan', model: 'pathfinder' },
  { make: 'nissan', model: 'frontier' },
  { make: 'nissan', model: 'kicks' },
  { make: 'nissan', model: 'rogue' },
  { make: 'nissan', model: 'versa' },
  { make: 'nissan', model: 'x-trail' },
  // Ford (6)
  { make: 'ford', model: 'explorer' },
  { make: 'ford', model: 'escape' },
  { make: 'ford', model: 'ranger' },
  { make: 'ford', model: 'f-150' },
  { make: 'ford', model: 'bronco' },
  { make: 'ford', model: 'expedition' },
  // Chevrolet (6)
  { make: 'chevrolet', model: 'silverado' },
  { make: 'chevrolet', model: 'equinox' },
  { make: 'chevrolet', model: 'tahoe' },
  { make: 'chevrolet', model: 'trailblazer' },
  { make: 'chevrolet', model: 'traverse' },
  { make: 'chevrolet', model: 'colorado' },
  // Jeep (5)
  { make: 'jeep', model: 'wrangler' },
  { make: 'jeep', model: 'grand-cherokee' },
  { make: 'jeep', model: 'compass' },
  { make: 'jeep', model: 'renegade' },
  { make: 'jeep', model: 'gladiator' },
  // Mitsubishi (5)
  { make: 'mitsubishi', model: 'outlander' },
  { make: 'mitsubishi', model: 'l200' },
  { make: 'mitsubishi', model: 'asx' },
  { make: 'mitsubishi', model: 'montero' },
  { make: 'mitsubishi', model: 'eclipse-cross' },
  // Suzuki (4)
  { make: 'suzuki', model: 'vitara' },
  { make: 'suzuki', model: 'swift' },
  { make: 'suzuki', model: 'jimny' },
  { make: 'suzuki', model: 'grand-vitara' },
  // Mazda (4)
  { make: 'mazda', model: 'cx-5' },
  { make: 'mazda', model: 'cx-30' },
  { make: 'mazda', model: 'mazda3' },
  { make: 'mazda', model: 'cx-9' },
  // BMW (3)
  { make: 'bmw', model: 'x3' },
  { make: 'bmw', model: 'x5' },
  { make: 'bmw', model: 'serie-3' },
  // Mercedes-Benz (3)
  { make: 'mercedes-benz', model: 'clase-c' },
  { make: 'mercedes-benz', model: 'gle' },
  { make: 'mercedes-benz', model: 'glc' },
];

const TOP_BRANDS = [
  'toyota',
  'honda',
  'hyundai',
  'kia',
  'nissan',
  'mitsubishi',
  'suzuki',
  'chevrolet',
  'ford',
  'jeep',
  'mazda',
  'bmw',
  'mercedes-benz',
  'audi',
  'volkswagen',
  'lexus',
  'subaru',
  'dodge',
];

// ── Helpers ──────────────────────────────────────────────────────────────────

async function revalidatePath(path: string): Promise<boolean> {
  try {
    const res = await fetch(`${BASE_SITE_URL}/api/revalidate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path, secret: REVALIDATION_SECRET }),
    });
    return res.ok;
  } catch {
    return false;
  }
}

function sleep(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

// ── Main ─────────────────────────────────────────────────────────────────────

async function main() {
  console.log('🔄 OKLA Weekly SEO Refresh — Starting...');
  console.log(`   API: ${BASE_API_URL}`);
  console.log(`   Site: ${BASE_SITE_URL}`);
  console.log(`   Brands: ${TOP_BRANDS.length}`);
  console.log(`   Models: ${POPULAR_COMBINATIONS.length}`);
  console.log('');

  let revalidated = 0;
  let failed = 0;

  // 1. Revalidate /marcas index
  console.log('📋 Revalidating /marcas index...');
  if (await revalidatePath('/marcas')) {
    revalidated++;
    console.log('   ✅ /marcas');
  } else {
    failed++;
    console.log('   ❌ /marcas — revalidation failed');
  }

  // 2. Revalidate all brand pages
  console.log('\n📋 Revalidating brand pages...');
  for (const brand of TOP_BRANDS) {
    const path = `/marcas/${brand}`;
    if (await revalidatePath(path)) {
      revalidated++;
      process.stdout.write('.');
    } else {
      failed++;
      console.log(`\n   ❌ ${path}`);
    }
    await sleep(100); // Rate limit: 10 req/s
  }
  console.log('');

  // 3. Revalidate all model pages
  console.log('\n📋 Revalidating model pages...');
  for (const combo of POPULAR_COMBINATIONS) {
    const path = `/marcas/${combo.make}/${combo.model}`;
    if (await revalidatePath(path)) {
      revalidated++;
      process.stdout.write('.');
    } else {
      failed++;
      console.log(`\n   ❌ ${path}`);
    }
    await sleep(100); // Rate limit: 10 req/s
  }
  console.log('');

  // 4. Ping search engines for sitemap
  console.log('\n🔔 Pinging search engines...');
  const sitemapUrl = `${BASE_SITE_URL}/sitemap.xml`;
  try {
    await fetch(`https://www.google.com/ping?sitemap=${encodeURIComponent(sitemapUrl)}`);
    console.log('   ✅ Google pinged');
  } catch {
    console.log('   ⚠️ Google ping failed (non-blocking)');
  }
  try {
    await fetch(`https://www.bing.com/ping?sitemap=${encodeURIComponent(sitemapUrl)}`);
    console.log('   ✅ Bing pinged');
  } catch {
    console.log('   ⚠️ Bing ping failed (non-blocking)');
  }

  // Summary
  const total = 1 + TOP_BRANDS.length + POPULAR_COMBINATIONS.length;
  console.log('\n────────────────────────────────────');
  console.log(`✅ Revalidated: ${revalidated}/${total}`);
  console.log(`❌ Failed:      ${failed}/${total}`);
  console.log(`📊 ${new Date().toISOString()}`);
  console.log('────────────────────────────────────');

  if (failed > 0) {
    process.exit(1);
  }
}

main().catch(err => {
  console.error('💥 Fatal error:', err);
  process.exit(1);
});
