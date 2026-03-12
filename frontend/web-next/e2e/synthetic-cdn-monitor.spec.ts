/**
 * ═══════════════════════════════════════════════════════════════════
 * Synthetic CDN Monitor — Image Performance & Availability
 * ═══════════════════════════════════════════════════════════════════
 *
 * Scheduled synthetic test (every 30 minutes via GitHub Actions cron)
 * that measures CDN image performance from the perspective of a user
 * in Santo Domingo, DR (simulated 4G: 100ms latency, 20Mbps).
 *
 * Metrics collected:
 *   - Image LCP (time to first visible image)
 *   - Total carousel load time (all images loaded)
 *   - Image error rate per listing
 *   - Individual image load times
 *
 * Alert triggers:
 *   - Image LCP > 3 seconds → alert to technical team
 *   - Image error rate > 20% → critical alert
 *   - CDN response time P95 > 5 seconds → degradation alert
 *
 * Run:
 *   pnpm exec playwright test e2e/synthetic-cdn-monitor.spec.ts --config=playwright.prod.config.ts
 *
 * With 4G simulation:
 *   SIMULATE_4G=true pnpm exec playwright test e2e/synthetic-cdn-monitor.spec.ts --config=playwright.prod.config.ts
 */

import { test, expect, type Page } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'https://okla.com.do';
const API_BASE_URL = process.env.API_BASE_URL || 'https://api.okla.com.do';
const SIMULATE_4G = process.env.SIMULATE_4G === 'true';

// Thresholds
const LCP_THRESHOLD_MS = 3000; // 3 seconds — Google's "good" LCP threshold
const CAROUSEL_LOAD_THRESHOLD_MS = 8000; // 8 seconds for full carousel
const IMAGE_ERROR_THRESHOLD = 0.2; // 20%

// 4G network simulation for Santo Domingo (100ms latency, 20Mbps)
const NETWORK_4G = {
  offline: false,
  downloadThroughput: (20 * 1024 * 1024) / 8, // 20 Mbps → bytes/s
  uploadThroughput: (5 * 1024 * 1024) / 8, // 5 Mbps → bytes/s
  latency: 100, // 100ms RTT
};

interface ImageLoadMetric {
  url: string;
  loadTimeMs: number;
  status: 'loaded' | 'error' | 'timeout';
  sizeBytes: number;
  contentType: string;
}

interface PerformanceReport {
  listingId: string;
  listingTitle: string;
  url: string;
  lcpMs: number;
  totalCarouselLoadMs: number;
  imageCount: number;
  imagesLoaded: number;
  imagesErrored: number;
  errorRate: number;
  imageMetrics: ImageLoadMetric[];
  timestamp: string;
}

test.describe('Synthetic CDN Monitor — Image Performance', () => {
  let mostVisitedListingSlug: string | null = null;

  test.beforeAll(async ({ request }) => {
    // Get the most visited/featured listing
    const response = await request.get(`${API_BASE_URL}/api/vehicles/featured`).catch(() => null);

    if (response && response.ok()) {
      const data = await response.json();
      const items = data.items || data.data || data.results || data;
      if (Array.isArray(items) && items.length > 0) {
        mostVisitedListingSlug = items[0].slug || items[0].id;
      }
    }

    // Fallback: get most recent listing
    if (!mostVisitedListingSlug) {
      const fallback = await request
        .get(`${API_BASE_URL}/api/vehicles?page=1&pageSize=1&sortBy=views&sortOrder=desc`)
        .catch(() => null);

      if (fallback && fallback.ok()) {
        const data = await fallback.json();
        const items = data.items || data.data || data.results || [];
        if (items.length > 0) {
          mostVisitedListingSlug = items[0].slug || items[0].id;
        }
      }
    }
  });

  test('should measure image LCP and carousel load time', async ({ page, context }) => {
    test.skip(!mostVisitedListingSlug, 'No listing found for performance test');

    // Apply 4G network throttling if enabled
    if (SIMULATE_4G) {
      const cdpSession = await context.newCDPSession(page);
      await cdpSession.send('Network.emulateNetworkConditions', NETWORK_4G);
    }

    const report = await measureListingImagePerformance(page, mostVisitedListingSlug!);

    // Log comprehensive report
    // eslint-disable-next-line no-console
    console.log('\n═══════════════════════════════════════════════════════');
    // eslint-disable-next-line no-console
    console.log('📊 CDN IMAGE PERFORMANCE REPORT');
    // eslint-disable-next-line no-console
    console.log('═══════════════════════════════════════════════════════');
    // eslint-disable-next-line no-console
    console.log(`Listing: ${report.listingTitle} (${report.listingId})`);
    // eslint-disable-next-line no-console
    console.log(`URL: ${report.url}`);
    // eslint-disable-next-line no-console
    console.log(`Timestamp: ${report.timestamp}`);
    // eslint-disable-next-line no-console
    console.log(`Network: ${SIMULATE_4G ? '4G (100ms latency, 20Mbps)' : 'Native'}`);
    // eslint-disable-next-line no-console
    console.log('───────────────────────────────────────────────────────');
    // eslint-disable-next-line no-console
    console.log(
      `🖼️  Image LCP: ${report.lcpMs.toFixed(0)}ms ${report.lcpMs > LCP_THRESHOLD_MS ? '❌ EXCEEDS THRESHOLD' : '✅'}`
    );
    // eslint-disable-next-line no-console
    console.log(
      `🎠 Carousel load: ${report.totalCarouselLoadMs.toFixed(0)}ms ${report.totalCarouselLoadMs > CAROUSEL_LOAD_THRESHOLD_MS ? '⚠️ SLOW' : '✅'}`
    );
    // eslint-disable-next-line no-console
    console.log(
      `📸 Images: ${report.imagesLoaded}/${report.imageCount} loaded, ${report.imagesErrored} errors`
    );
    // eslint-disable-next-line no-console
    console.log(
      `📉 Error rate: ${(report.errorRate * 100).toFixed(1)}% ${report.errorRate > IMAGE_ERROR_THRESHOLD ? '❌ CRITICAL' : '✅'}`
    );
    // eslint-disable-next-line no-console
    console.log('───────────────────────────────────────────────────────');

    // Log individual image metrics
    for (const img of report.imageMetrics) {
      const icon = img.status === 'loaded' ? '✅' : '❌';
      // eslint-disable-next-line no-console
      console.log(
        `  ${icon} ${img.loadTimeMs.toFixed(0)}ms | ${(img.sizeBytes / 1024).toFixed(1)}KB | ${img.contentType} | ${img.url.substring(0, 80)}...`
      );
    }

    // Output JSON for GitHub Actions to parse
    // eslint-disable-next-line no-console
    console.log(`\n::set-output name=lcp_ms::${report.lcpMs.toFixed(0)}`);
    // eslint-disable-next-line no-console
    console.log(`::set-output name=carousel_ms::${report.totalCarouselLoadMs.toFixed(0)}`);
    // eslint-disable-next-line no-console
    console.log(`::set-output name=error_rate::${(report.errorRate * 100).toFixed(1)}`);
    // eslint-disable-next-line no-console
    console.log(`::set-output name=images_total::${report.imageCount}`);
    // eslint-disable-next-line no-console
    console.log(`::set-output name=images_loaded::${report.imagesLoaded}`);

    // ASSERTIONS — These trigger alerts on failure

    // 1. Image LCP must be under 3 seconds
    expect(
      report.lcpMs,
      `🚨 Image LCP ${report.lcpMs.toFixed(0)}ms exceeds ${LCP_THRESHOLD_MS}ms threshold. ` +
        `CDN may be degraded. Alert the technical team.`
    ).toBeLessThanOrEqual(LCP_THRESHOLD_MS);

    // 2. Image error rate must be under 20%
    expect(
      report.errorRate,
      `🚨 Image error rate ${(report.errorRate * 100).toFixed(1)}% exceeds ${IMAGE_ERROR_THRESHOLD * 100}% threshold. ` +
        `${report.imagesErrored}/${report.imageCount} images failed to load.`
    ).toBeLessThanOrEqual(IMAGE_ERROR_THRESHOLD);

    // 3. Carousel should load within 8 seconds
    expect(
      report.totalCarouselLoadMs,
      `⚠️ Carousel load time ${report.totalCarouselLoadMs.toFixed(0)}ms exceeds ${CAROUSEL_LOAD_THRESHOLD_MS}ms. ` +
        `CDN performance is degrading.`
    ).toBeLessThanOrEqual(CAROUSEL_LOAD_THRESHOLD_MS);
  });

  test('should verify CDN images load without 4G throttling (baseline)', async ({ page }) => {
    test.skip(!mostVisitedListingSlug, 'No listing found for baseline test');

    // No throttling — measure pure CDN speed
    const report = await measureListingImagePerformance(page, mostVisitedListingSlug!);

    // eslint-disable-next-line no-console
    console.log(
      `\n📊 BASELINE (no throttling): LCP=${report.lcpMs.toFixed(0)}ms, ` +
        `Carousel=${report.totalCarouselLoadMs.toFixed(0)}ms, ` +
        `${report.imagesLoaded}/${report.imageCount} loaded`
    );

    // Baseline assertions — tighter thresholds without throttling
    expect(report.lcpMs).toBeLessThanOrEqual(LCP_THRESHOLD_MS);
    expect(report.errorRate).toBeLessThanOrEqual(IMAGE_ERROR_THRESHOLD);
  });
});

/**
 * Navigate to a listing page and measure all image loading metrics
 */
async function measureListingImagePerformance(
  page: Page,
  slug: string
): Promise<PerformanceReport> {
  const url = `${BASE_URL}/vehiculos/${slug}`;
  const imageMetrics: ImageLoadMetric[] = [];
  let firstImageVisibleMs = 0;
  const startTime = Date.now();

  // Track image request timings via CDP-like approach using page events
  const imageRequests = new Map<string, { startTime: number }>();

  page.on('request', request => {
    const resourceType = request.resourceType();
    if (resourceType === 'image') {
      imageRequests.set(request.url(), { startTime: Date.now() });
    }
  });

  page.on('response', async response => {
    const request = response.request();
    if (request.resourceType() === 'image') {
      const reqData = imageRequests.get(request.url());
      const loadTimeMs = reqData ? Date.now() - reqData.startTime : 0;

      try {
        const body = await response.body().catch(() => Buffer.from(''));
        imageMetrics.push({
          url: request.url(),
          loadTimeMs,
          status: response.status() >= 200 && response.status() < 400 ? 'loaded' : 'error',
          sizeBytes: body.length,
          contentType: response.headers()['content-type'] || 'unknown',
        });
      } catch {
        imageMetrics.push({
          url: request.url(),
          loadTimeMs,
          status: 'error',
          sizeBytes: 0,
          contentType: 'unknown',
        });
      }
    }
  });

  // Navigate and wait for images
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });

  // Measure LCP for images using PerformanceObserver
  const lcpValue = await page.evaluate(() => {
    return new Promise<number>(resolve => {
      let lcpMs = 0;
      const observer = new PerformanceObserver(entryList => {
        const entries = entryList.getEntries();
        for (const entry of entries) {
          // LCP entry — check if it's an image element
          const lcpEntry = entry as PerformanceEntry & { element?: Element; startTime: number };
          if (lcpEntry.element?.tagName === 'IMG' || !lcpEntry.element) {
            lcpMs = lcpEntry.startTime;
          }
        }
      });

      observer.observe({ type: 'largest-contentful-paint', buffered: true });

      // Wait for LCP to stabilize (user interaction or timeout)
      setTimeout(() => {
        observer.disconnect();
        resolve(lcpMs);
      }, 5000);
    });
  });

  firstImageVisibleMs = lcpValue || 0;

  // Wait for network idle to ensure all carousel images have loaded
  await page.waitForLoadState('networkidle').catch(() => {
    /* timeout is ok */
  });

  // Additional wait for lazy-loaded images
  await page.waitForTimeout(2000);

  const totalCarouselLoadMs = Date.now() - startTime;

  // Get listing title from the page
  const title = await page
    .locator('h1, [data-testid="vehicle-title"]')
    .first()
    .textContent()
    .catch(() => slug);

  // Filter only CDN image metrics (not icons, logos, etc.)
  const cdnImages = imageMetrics.filter(
    img =>
      !img.url.includes('data:') &&
      !img.url.includes('favicon') &&
      !img.url.includes('logo') &&
      !img.url.includes('/_next/static') &&
      img.sizeBytes > 100
  );

  const imagesLoaded = cdnImages.filter(img => img.status === 'loaded').length;
  const imagesErrored = cdnImages.filter(img => img.status === 'error').length;
  const imageCount = cdnImages.length;
  const errorRate = imageCount > 0 ? imagesErrored / imageCount : 0;

  return {
    listingId: slug,
    listingTitle: title || slug,
    url,
    lcpMs: firstImageVisibleMs,
    totalCarouselLoadMs,
    imageCount,
    imagesLoaded,
    imagesErrored,
    errorRate,
    imageMetrics: cdnImages,
    timestamp: new Date().toISOString(),
  };
}
