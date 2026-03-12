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
import { Rate } from "k6/metrics";

const errorRate = new Rate("errors");

const BASE_URL = __ENV.BASE_URL || "https://api.okla.do";

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
}
