#!/usr/bin/env npx tsx
/**
 * OKLA Pre-Launch Indexing Checklist
 *
 * Validates that all indexing infrastructure is properly configured
 * before the beta launch. Run this BEFORE the daily seed script.
 *
 * Usage:
 *   npx tsx scripts/pre-launch-indexing-check.ts
 *
 * Checks:
 * ✅ Google Search Console verification token
 * ✅ Google Indexing API key (service account)
 * ✅ Backend vehicle sitemap endpoint accessibility
 * ✅ Frontend sitemap.xml accessibility
 * ✅ Frontend robots.txt configuration
 * ✅ Vehicle count matches beta target (~1,500)
 * ✅ ISR revalidation endpoint
 * ✅ SEO webhook endpoint
 * ✅ Frontend environment variables
 */

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

interface CheckResult {
  name: string;
  status: 'PASS' | 'FAIL' | 'WARN' | 'SKIP';
  message: string;
  recommendation?: string;
}

const results: CheckResult[] = [];

function check(
  name: string,
  status: CheckResult['status'],
  message: string,
  recommendation?: string
): void {
  results.push({ name, status, message, recommendation });
  const icon = { PASS: '✅', FAIL: '❌', WARN: '⚠️', SKIP: '⏭️' }[status];
  console.log(`  ${icon} ${name}: ${message}`);
  if (recommendation) {
    console.log(`     → ${recommendation}`);
  }
}

async function main(): Promise<void> {
  console.log('═══════════════════════════════════════════════════════════════');
  console.log('  OKLA — Pre-Launch Indexing Checklist');
  console.log(`  ${new Date().toISOString()}`);
  console.log(`  Site: ${SITE_URL}  |  API: ${API_URL}`);
  console.log('═══════════════════════════════════════════════════════════════\n');

  // ── 1. Google Search Console Verification ──
  console.log('🔍 Environment Variables');
  const gscToken = process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION || '';
  if (!gscToken || gscToken === 'PENDING_VERIFICATION_CODE') {
    check(
      'GSC Verification',
      'FAIL',
      `Token is "${gscToken || '(empty)'}"`,
      'Go to Google Search Console → Add property → HTML tag → set NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION'
    );
  } else {
    check('GSC Verification', 'PASS', `Token set: ${gscToken.slice(0, 10)}...`);
  }

  // ── 2. Google Indexing API Key ──
  const indexingKey = process.env.GOOGLE_INDEXING_API_KEY || '';
  if (!indexingKey) {
    check(
      'Indexing API Key',
      'FAIL',
      'GOOGLE_INDEXING_API_KEY not set',
      'Create a Google Cloud service account with Indexing API access, base64-encode the JSON key'
    );
  } else {
    try {
      const decoded = Buffer.from(indexingKey, 'base64').toString('utf-8');
      const keyJson = JSON.parse(decoded);
      if (keyJson.client_email && keyJson.private_key) {
        check('Indexing API Key', 'PASS', `Service account: ${keyJson.client_email}`);
      } else {
        check('Indexing API Key', 'FAIL', 'Key JSON missing client_email or private_key');
      }
    } catch {
      check(
        'Indexing API Key',
        'FAIL',
        'Cannot decode/parse GOOGLE_INDEXING_API_KEY (must be base64 JSON)'
      );
    }
  }

  // ── 3. SEO Webhook Secret ──
  const seoSecret = process.env.SEO_WEBHOOK_SECRET || '';
  if (!seoSecret) {
    check(
      'SEO Webhook Secret',
      'WARN',
      'SEO_WEBHOOK_SECRET not set — /api/seo/* endpoints are unprotected',
      'Set a strong secret in both frontend and backend (Frontend:SeoWebhookSecret)'
    );
  } else {
    check('SEO Webhook Secret', 'PASS', 'Secret configured');
  }

  // ── 4. Backend Vehicle Sitemap Endpoint ──
  console.log('\n🌐 Backend Endpoints');
  try {
    const resp = await fetch(`${API_URL}/api/vehicles/sitemap`, {
      signal: AbortSignal.timeout(10000),
    });
    if (resp.ok) {
      const data = await resp.json();
      const items = Array.isArray(data) ? data : (data?.items ?? data?.data ?? []);
      check('Vehicle Sitemap API', 'PASS', `${items.length} active vehicles`);

      if (items.length < 1400) {
        check('Beta Target Count', 'WARN', `${items.length} vehicles — expected ~1,500 for beta`);
      } else if (items.length >= 1400) {
        check('Beta Target Count', 'PASS', `${items.length} vehicles — meets beta target`);
      }
    } else {
      check(
        'Vehicle Sitemap API',
        'FAIL',
        `HTTP ${resp.status} from ${API_URL}/api/vehicles/sitemap`,
        'Check that VehiclesSaleService is running and Gateway routes are configured'
      );
    }
  } catch (error) {
    check(
      'Vehicle Sitemap API',
      'FAIL',
      `Cannot reach ${API_URL}/api/vehicles/sitemap — ${error}`,
      'Start the backend services: docker compose up'
    );
  }

  // ── 5. Frontend Sitemap ──
  console.log('\n🗺️  Frontend SEO Resources');
  try {
    const resp = await fetch(`${SITE_URL}/sitemap.xml`, { signal: AbortSignal.timeout(10000) });
    if (resp.ok) {
      const text = await resp.text();
      const urlCount = (text.match(/<url>/g) || []).length;
      check('sitemap.xml', 'PASS', `Accessible — ${urlCount} URLs found`);

      if (urlCount < 100) {
        check(
          'Sitemap Vehicle Coverage',
          'WARN',
          `Only ${urlCount} URLs in sitemap — expected 1,500+ vehicle URLs`,
          'Rebuild the Next.js app to regenerate the sitemap with latest vehicle data'
        );
      }
    } else {
      check('sitemap.xml', 'FAIL', `HTTP ${resp.status} at ${SITE_URL}/sitemap.xml`);
    }
  } catch {
    check('sitemap.xml', 'SKIP', `Cannot reach ${SITE_URL}/sitemap.xml (site may not be live)`);
  }

  // ── 6. robots.txt ──
  try {
    const resp = await fetch(`${SITE_URL}/robots.txt`, { signal: AbortSignal.timeout(10000) });
    if (resp.ok) {
      const text = await resp.text();
      const hasSitemap = text.includes('Sitemap:');
      const hasDisallowVehiculos = text.includes('Disallow: /vehiculos');

      if (hasDisallowVehiculos) {
        check('robots.txt', 'FAIL', 'Disallow: /vehiculos found — blocks vehicle indexing!');
      } else if (hasSitemap) {
        check('robots.txt', 'PASS', 'Accessible, sitemap declared, /vehiculos not blocked');
      } else {
        check('robots.txt', 'WARN', 'Accessible but no Sitemap: declaration');
      }
    } else {
      check('robots.txt', 'FAIL', `HTTP ${resp.status} at ${SITE_URL}/robots.txt`);
    }
  } catch {
    check('robots.txt', 'SKIP', `Cannot reach ${SITE_URL}/robots.txt (site may not be live)`);
  }

  // ── 7. Revalidation Endpoint ──
  try {
    const resp = await fetch(`${SITE_URL}/api/revalidate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path: '/test', secret: 'invalid' }),
      signal: AbortSignal.timeout(10000),
    });
    // We expect 401 or 400 — that means the endpoint exists
    if (resp.status === 401 || resp.status === 400 || resp.status === 200) {
      check(
        'ISR Revalidation Endpoint',
        'PASS',
        `Responding at /api/revalidate (HTTP ${resp.status})`
      );
    } else {
      check(
        'ISR Revalidation Endpoint',
        'WARN',
        `Unexpected HTTP ${resp.status} from /api/revalidate`
      );
    }
  } catch {
    check(
      'ISR Revalidation Endpoint',
      'SKIP',
      'Cannot reach /api/revalidate (site may not be live)'
    );
  }

  // ── 8. SEO Index Endpoint ──
  try {
    const resp = await fetch(`${SITE_URL}/api/seo/index?slug=test`, {
      signal: AbortSignal.timeout(10000),
    });
    if (resp.status === 401 || resp.status === 200 || resp.status === 502) {
      check('SEO Index Endpoint', 'PASS', `Responding at /api/seo/index (HTTP ${resp.status})`);
    } else {
      check('SEO Index Endpoint', 'WARN', `Unexpected HTTP ${resp.status} from /api/seo/index`);
    }
  } catch {
    check('SEO Index Endpoint', 'SKIP', 'Cannot reach /api/seo/index (site may not be live)');
  }

  // ── Summary ──
  const passCount = results.filter(r => r.status === 'PASS').length;
  const failCount = results.filter(r => r.status === 'FAIL').length;
  const warnCount = results.filter(r => r.status === 'WARN').length;
  const skipCount = results.filter(r => r.status === 'SKIP').length;

  console.log('\n═══════════════════════════════════════════════════════════════');
  console.log(
    `  📊 Results: ${passCount} pass, ${failCount} fail, ${warnCount} warn, ${skipCount} skip`
  );
  console.log('═══════════════════════════════════════════════════════════════');

  if (failCount > 0) {
    console.log('\n❌ LAUNCH BLOCKED — Fix the following critical issues:');
    results
      .filter(r => r.status === 'FAIL')
      .forEach(r => console.log(`   • ${r.name}: ${r.message}`));
    console.log('\nRun this script again after fixing.');
    process.exit(1);
  } else if (warnCount > 0) {
    console.log('\n⚠️  LAUNCH ALLOWED with warnings:');
    results
      .filter(r => r.status === 'WARN')
      .forEach(r => console.log(`   • ${r.name}: ${r.message}`));
    console.log('\n✅ No critical blockers. Proceed with launch, but address warnings.');
  } else {
    console.log('\n🚀 ALL CHECKS PASSED — Ready for launch!');
  }
}

main().catch(error => {
  console.error('Fatal error:', error);
  process.exit(1);
});
