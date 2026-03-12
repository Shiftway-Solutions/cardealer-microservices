/**
 * ═══════════════════════════════════════════════════════════════════
 * OKLA Marketplace — Smoke Test (k6)
 * ═══════════════════════════════════════════════════════════════════
 *
 * Quick 2-minute test with 10 VUs to verify all endpoints are
 * reachable and responding correctly. Run BEFORE the full load test.
 *
 * Run:
 *   k6 run tests/load/k6-smoke-test.js
 */

import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Counter } from "k6/metrics";

const errorRate = new Rate("errors");
const imageFailures = new Counter("image_failures");
const imageChecks = new Counter("image_checks");

const BASE_URL = __ENV.BASE_URL || "https://api.okla.do";
const IMAGE_FAILURE_THRESHOLD = 0.2; // 20% — if more images fail, deploy is bad
const MIN_IMAGE_SIZE = 1024; // 1KB

export const options = {
  vus: 10,
  duration: "2m",
  thresholds: {
    http_req_duration: ["p(95)<5000"],
    errors: ["rate<0.1"],
    http_req_failed: ["rate<0.05"],
  },
};

const ENDPOINTS = [
  { method: "GET", path: "/health", name: "Health" },
  { method: "GET", path: "/health/ready", name: "Ready" },
  { method: "GET", path: "/health/live", name: "Live" },
  {
    method: "GET",
    path: "/api/vehicles?page=1&pageSize=5",
    name: "Vehicles List",
  },
  { method: "GET", path: "/api/vehicles/featured", name: "Featured" },
  { method: "GET", path: "/api/vehicles/makes", name: "Makes" },
];

export default function () {
  for (const ep of ENDPOINTS) {
    const res =
      ep.method === "GET"
        ? http.get(`${BASE_URL}${ep.path}`, { tags: { name: ep.name } })
        : http.post(`${BASE_URL}${ep.path}`, "{}", {
            headers: { "Content-Type": "application/json" },
            tags: { name: ep.name },
          });

    const ok = check(res, {
      [`${ep.name}: status OK`]: (r) => r.status >= 200 && r.status < 500,
      [`${ep.name}: response time < 5s`]: (r) => r.timings.duration < 5000,
    });

    errorRate.add(!ok);
    sleep(1);
  }

  // ═══════════════════════════════════════════════════════════════════
  // IMAGE INTEGRITY VALIDATION — Post-Deploy Step
  // ═══════════════════════════════════════════════════════════════════
  // Fetch 5 most recent listings, extract image URLs, validate each one
  // checks: HTTP 200, Content-Type image/*, size > 1KB
  // If >20% of images fail → deploy should be rolled back
  // ═══════════════════════════════════════════════════════════════════

  const vehiclesRes = http.get(
    `${BASE_URL}/api/vehicles?page=1&pageSize=5&sortBy=createdAt&sortOrder=desc`,
    { tags: { name: "Image Smoke: Fetch Listings" } },
  );

  const listingsOk = check(vehiclesRes, {
    "Listings: status 200": (r) => r.status === 200,
  });

  if (listingsOk && vehiclesRes.json()) {
    const data = vehiclesRes.json();
    const items =
      data.items || data.data || data.results || data.vehicles || [];

    let totalImages = 0;
    let failedImages = 0;

    for (const item of items.slice(0, 5)) {
      // Try to get images from vehicle detail
      const detailRes = http.get(`${BASE_URL}/api/vehicles/${item.id}`, {
        tags: { name: "Image Smoke: Vehicle Detail" },
      });

      if (detailRes.status === 200 && detailRes.json()) {
        const detail = detailRes.json();
        const photos = detail.photos || detail.images || detail.media || [];

        for (const photo of photos) {
          const imageUrl =
            photo.cdnUrl || photo.url || photo.imageUrl || photo.src || "";
          if (!imageUrl || !imageUrl.startsWith("http")) continue;

          totalImages++;
          imageChecks.add(1);

          // Validate the image URL with a GET request
          const imgRes = http.get(imageUrl, {
            tags: { name: "Image Smoke: CDN Image" },
            timeout: "15s",
          });

          const imgOk = check(imgRes, {
            "Image: status 200": (r) => r.status === 200,
            "Image: Content-Type is image/webp": (r) =>
              (r.headers["Content-Type"] || "").includes("image/webp"),
            "Image: size > 1KB": (r) => (r.body || "").length > MIN_IMAGE_SIZE,
          });

          if (!imgOk) {
            failedImages++;
            imageFailures.add(1);
            console.warn(
              `❌ Image failed: ${imageUrl} (status: ${imgRes.status}, ` +
                `type: ${imgRes.headers["Content-Type"]}, size: ${(imgRes.body || "").length}B)`,
            );
          }
        }
      }
      sleep(0.5);
    }

    // Final image failure rate check
    if (totalImages > 0) {
      const failureRate = failedImages / totalImages;
      check(null, {
        [`Image failure rate ${(failureRate * 100).toFixed(1)}% <= ${IMAGE_FAILURE_THRESHOLD * 100}%`]:
          () => failureRate <= IMAGE_FAILURE_THRESHOLD,
      });

      if (failureRate > IMAGE_FAILURE_THRESHOLD) {
        console.error(
          `🚨 IMAGE SMOKE FAILED: ${failedImages}/${totalImages} images broken ` +
            `(${(failureRate * 100).toFixed(1)}% > ${IMAGE_FAILURE_THRESHOLD * 100}% threshold). ` +
            `DEPLOY SHOULD BE ROLLED BACK.`,
        );
        errorRate.add(true);
      } else {
        console.log(
          `✅ Image smoke passed: ${totalImages - failedImages}/${totalImages} images OK ` +
            `(${(failureRate * 100).toFixed(1)}% failure rate)`,
        );
      }
    }
  }
}
