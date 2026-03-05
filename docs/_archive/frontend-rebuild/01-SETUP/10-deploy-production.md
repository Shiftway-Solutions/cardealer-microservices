# 🚀 Deploy a Producción - Frontend Next.js

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Proyecto Next.js configurado, Docker instalado
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Configurar deploy de producción completo:

- Dockerfile multi-stage optimizado
- Nginx configuración para SPA/SSR
- Variables de entorno runtime vs build-time
- Health checks y monitoreo
- Kubernetes manifests
- CI/CD con GitHub Actions

---

## 🎯 ARQUITECTURA DE DEPLOY

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEPLOY ARCHITECTURE                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────────────────┐    │
│  │   GitHub     │────▶│   GitHub     │────▶│   GitHub Container       │    │
│  │   Push       │     │   Actions    │     │   Registry (ghcr.io)     │    │
│  └──────────────┘     └──────────────┘     └──────────────────────────┘    │
│                                                     │                       │
│                                                     ▼                       │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Digital Ocean Kubernetes (DOKS)                    │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐         │  │
│  │  │ frontend-web   │  │ frontend-web   │  │ frontend-web   │         │  │
│  │  │ Pod (replica)  │  │ Pod (replica)  │  │ Pod (replica)  │         │  │
│  │  └────────────────┘  └────────────────┘  └────────────────┘         │  │
│  │           ▲                  ▲                  ▲                    │  │
│  │           └──────────────────┼──────────────────┘                    │  │
│  │                              │                                       │  │
│  │                    ┌─────────┴─────────┐                             │  │
│  │                    │   Load Balancer   │                             │  │
│  │                    │  (DO Managed LB)  │                             │  │
│  │                    └─────────┬─────────┘                             │  │
│  └──────────────────────────────┼───────────────────────────────────────┘  │
│                                 │                                          │
│                                 ▼                                          │
│                    ┌───────────────────────┐                               │
│                    │    okla.com.do        │                               │
│                    │ (Let's Encrypt TLS)   │                               │
│                    └───────────────────────┘                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Dockerfile Multi-Stage (Next.js)

```dockerfile
# filepath: frontend/web-next/Dockerfile
# =============================================================================
# DOCKERFILE - OKLA Frontend (Next.js)
# =============================================================================
# Multi-stage build for optimized production image
#
# Build: docker build -t okla-frontend .
# Run:   docker run -p 3000:3000 okla-frontend
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Dependencies
# -----------------------------------------------------------------------------
FROM node:20-alpine AS deps

WORKDIR /app

# Install dependencies only
COPY package.json pnpm-lock.yaml* ./
RUN corepack enable pnpm && pnpm install --frozen-lockfile

# -----------------------------------------------------------------------------
# Stage 2: Builder
# -----------------------------------------------------------------------------
FROM node:20-alpine AS builder

WORKDIR /app

# Copy dependencies from deps stage
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Build-time environment variables
ARG NEXT_PUBLIC_API_URL=https://api.okla.com.do
ARG NEXT_PUBLIC_APP_NAME=OKLA
ARG NEXT_PUBLIC_APP_VERSION=1.0.0
ARG NEXT_PUBLIC_SITE_URL=https://okla.com.do
ARG NEXT_PUBLIC_SENTRY_DSN=
ARG NEXT_PUBLIC_GA_TRACKING_ID=
ARG NEXT_PUBLIC_GOOGLE_MAPS_KEY=
ARG NEXT_PUBLIC_RECAPTCHA_SITE_KEY=

# Set environment for build
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}
ENV NEXT_PUBLIC_APP_NAME=${NEXT_PUBLIC_APP_NAME}
ENV NEXT_PUBLIC_APP_VERSION=${NEXT_PUBLIC_APP_VERSION}
ENV NEXT_PUBLIC_SITE_URL=${NEXT_PUBLIC_SITE_URL}
ENV NEXT_PUBLIC_SENTRY_DSN=${NEXT_PUBLIC_SENTRY_DSN}
ENV NEXT_PUBLIC_GA_TRACKING_ID=${NEXT_PUBLIC_GA_TRACKING_ID}
ENV NEXT_PUBLIC_GOOGLE_MAPS_KEY=${NEXT_PUBLIC_GOOGLE_MAPS_KEY}
ENV NEXT_PUBLIC_RECAPTCHA_SITE_KEY=${NEXT_PUBLIC_RECAPTCHA_SITE_KEY}

# Disable telemetry during build
ENV NEXT_TELEMETRY_DISABLED=1

# Build the application
RUN corepack enable pnpm && pnpm build

# -----------------------------------------------------------------------------
# Stage 3: Runner (Production)
# -----------------------------------------------------------------------------
FROM node:20-alpine AS runner

WORKDIR /app

# Set production environment
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy only necessary files from builder
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static

# Set correct permissions
RUN chown -R nextjs:nodejs /app

# Switch to non-root user
USER nextjs

# Expose port
EXPOSE 3000

# Set hostname for container
ENV HOSTNAME="0.0.0.0"
ENV PORT=3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget -qO- http://localhost:3000/api/health || exit 1

# Start the application
CMD ["node", "server.js"]
```

---

## 🔧 PASO 2: Configuración Next.js para Standalone

```typescript
// filepath: frontend/web-next/next.config.ts
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Enable standalone output for Docker
  output: "standalone",

  // Compress responses
  compress: true,

  // Optimize images
  images: {
    domains: [
      "api.okla.com.do",
      "cdn.okla.com.do",
      "s3.amazonaws.com",
      "localhost",
    ],
    formats: ["image/avif", "image/webp"],
    minimumCacheTTL: 60 * 60 * 24, // 24 hours
  },

  // Environment variables available at runtime
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL,
    NEXT_PUBLIC_APP_NAME: process.env.NEXT_PUBLIC_APP_NAME || "OKLA",
    NEXT_PUBLIC_APP_VERSION: process.env.NEXT_PUBLIC_APP_VERSION || "1.0.0",
  },

  // Headers for security
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          { key: "X-Frame-Options", value: "SAMEORIGIN" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-XSS-Protection", value: "1; mode=block" },
          {
            key: "Referrer-Policy",
            value: "strict-origin-when-cross-origin",
          },
          {
            key: "Permissions-Policy",
            value: "camera=(), microphone=(), geolocation=(self)",
          },
        ],
      },
      {
        // Cache static assets
        source: "/_next/static/(.*)",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=31536000, immutable",
          },
        ],
      },
      {
        // Cache images
        source: "/images/(.*)",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=86400, s-maxage=86400",
          },
        ],
      },
    ];
  },

  // Redirects
  async redirects() {
    return [
      {
        source: "/home",
        destination: "/",
        permanent: true,
      },
    ];
  },

  // Rewrites for API proxy (optional, for development)
  async rewrites() {
    return process.env.NODE_ENV === "development"
      ? [
          {
            source: "/api/proxy/:path*",
            destination: `${process.env.NEXT_PUBLIC_API_URL}/:path*`,
          },
        ]
      : [];
  },

  // Experimental features
  experimental: {
    // Enable server actions
    serverActions: {
      bodySizeLimit: "2mb",
    },
  },

  // Webpack configuration
  webpack: (config, { isServer }) => {
    // Optimize bundles
    if (!isServer) {
      config.optimization.splitChunks = {
        chunks: "all",
        cacheGroups: {
          default: false,
          vendors: false,
          // Vendor chunk
          vendor: {
            name: "vendor",
            chunks: "all",
            test: /node_modules/,
            priority: 20,
          },
          // Common chunk
          common: {
            name: "common",
            minChunks: 2,
            chunks: "all",
            priority: 10,
            reuseExistingChunk: true,
            enforce: true,
          },
        },
      };
    }
    return config;
  },
};

export default nextConfig;
```

---

## 🔧 PASO 3: Health Check API Route

```typescript
// filepath: frontend/web-next/src/app/api/health/route.ts
import { NextResponse } from "next/server";

export async function GET() {
  const healthCheck = {
    status: "healthy",
    timestamp: new Date().toISOString(),
    version: process.env.NEXT_PUBLIC_APP_VERSION || "1.0.0",
    environment: process.env.NODE_ENV,
    uptime: process.uptime(),
    memory: process.memoryUsage(),
  };

  try {
    // Optional: Check API connectivity
    const apiUrl = process.env.NEXT_PUBLIC_API_URL;
    if (apiUrl) {
      const response = await fetch(`${apiUrl}/health`, {
        method: "GET",
        signal: AbortSignal.timeout(5000),
      });
      healthCheck.api = {
        url: apiUrl,
        status: response.ok ? "connected" : "error",
        statusCode: response.status,
      };
    }
  } catch (error) {
    healthCheck.api = {
      status: "unreachable",
      error: error instanceof Error ? error.message : "Unknown error",
    };
  }

  return NextResponse.json(healthCheck, {
    status: 200,
    headers: {
      "Cache-Control": "no-store, max-age=0",
    },
  });
}
```

---

## 🔧 PASO 4: Variables de Entorno

### Build-time vs Runtime

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ENVIRONMENT VARIABLES                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  BUILD-TIME (NEXT_PUBLIC_*)           │  RUNTIME (server-only)             │
│  ══════════════════════════           │  ═══════════════════════           │
│  • Embebidas en el bundle JS          │  • Solo disponibles en server      │
│  • Accesibles en cliente              │  • NO expuestas al cliente         │
│  • Requieren rebuild para cambiar     │  • Cambiables sin rebuild          │
│                                       │                                     │
│  NEXT_PUBLIC_API_URL                  │  DATABASE_URL                      │
│  NEXT_PUBLIC_GA_TRACKING_ID           │  JWT_SECRET                        │
│  NEXT_PUBLIC_SENTRY_DSN               │  API_INTERNAL_URL                  │
│  NEXT_PUBLIC_GOOGLE_MAPS_KEY          │  REDIS_URL                         │
│  NEXT_PUBLIC_RECAPTCHA_SITE_KEY       │  SMTP_PASSWORD                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Archivo .env.production

```bash
# filepath: frontend/web-next/.env.production
# =============================================================================
# PRODUCTION ENVIRONMENT VARIABLES
# =============================================================================

# API Configuration
NEXT_PUBLIC_API_URL=https://api.okla.com.do
NEXT_PUBLIC_SITE_URL=https://okla.com.do

# App Info
NEXT_PUBLIC_APP_NAME=OKLA
NEXT_PUBLIC_APP_VERSION=1.0.0

# Analytics & Monitoring
NEXT_PUBLIC_GA_TRACKING_ID=G-XXXXXXXXXX
NEXT_PUBLIC_SENTRY_DSN=https://xxxx@sentry.io/xxxxx

# External Services
NEXT_PUBLIC_GOOGLE_MAPS_KEY=AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
NEXT_PUBLIC_RECAPTCHA_SITE_KEY=6LcXXXXXXXXXXXXXXXXXXXXXXXXXX

# Feature Flags
NEXT_PUBLIC_ENABLE_ANALYTICS=true
NEXT_PUBLIC_ENABLE_CHAT=true
NEXT_PUBLIC_ENABLE_NOTIFICATIONS=true
```

---

## 🔧 PASO 5: Kubernetes Deployment

```yaml
# filepath: k8s/frontend-web.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: frontend-web
  namespace: okla
  labels:
    app: frontend-web
    tier: frontend
spec:
  replicas: 3
  selector:
    matchLabels:
      app: frontend-web
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: frontend-web
    spec:
      containers:
        - name: frontend-web
          image: ghcr.io/gregorymorenoiem/okla-frontend:latest
          imagePullPolicy: Always
          ports:
            - containerPort: 3000
              protocol: TCP
          env:
            - name: NODE_ENV
              value: "production"
            - name: NEXT_PUBLIC_API_URL
              valueFrom:
                configMapKeyRef:
                  name: frontend-config
                  key: API_URL
          resources:
            requests:
              memory: "256Mi"
              cpu: "100m"
            limits:
              memory: "512Mi"
              cpu: "500m"
          livenessProbe:
            httpGet:
              path: /api/health
              port: 3000
            initialDelaySeconds: 10
            periodSeconds: 30
            timeoutSeconds: 5
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /api/health
              port: 3000
            initialDelaySeconds: 5
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 3
      imagePullSecrets:
        - name: ghcr-secret

---
apiVersion: v1
kind: Service
metadata:
  name: frontend-web
  namespace: okla
spec:
  selector:
    app: frontend-web
  ports:
    - port: 80
      targetPort: 3000
      protocol: TCP
  type: ClusterIP

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: frontend-config
  namespace: okla
data:
  API_URL: "https://api.okla.com.do"
  SITE_URL: "https://okla.com.do"

---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: frontend-web-ingress
  namespace: okla
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/proxy-body-size: "50m"
spec:
  tls:
    - hosts:
        - okla.com.do
        - www.okla.com.do
      secretName: okla-tls
  rules:
    - host: okla.com.do
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: frontend-web
                port:
                  number: 80
    - host: www.okla.com.do
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: frontend-web
                port:
                  number: 80
```

---

## 🔧 PASO 6: GitHub Actions CI/CD

```yaml
# filepath: .github/workflows/frontend-deploy.yml
name: Frontend Deploy

on:
  push:
    branches: [main]
    paths:
      - "frontend/web-next/**"
      - ".github/workflows/frontend-deploy.yml"
  workflow_dispatch:

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: gregorymorenoiem/okla-frontend

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=sha,prefix=
            type=raw,value=latest,enable={{is_default_branch}}
            type=raw,value={{date 'YYYYMMDD-HHmmss'}}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: ./frontend/web-next
          file: ./frontend/web-next/Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
          build-args: |
            NEXT_PUBLIC_API_URL=${{ secrets.NEXT_PUBLIC_API_URL }}
            NEXT_PUBLIC_SENTRY_DSN=${{ secrets.NEXT_PUBLIC_SENTRY_DSN }}
            NEXT_PUBLIC_GA_TRACKING_ID=${{ secrets.NEXT_PUBLIC_GA_TRACKING_ID }}
            NEXT_PUBLIC_GOOGLE_MAPS_KEY=${{ secrets.NEXT_PUBLIC_GOOGLE_MAPS_KEY }}

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Install doctl
        uses: digitalocean/action-doctl@v2
        with:
          token: ${{ secrets.DIGITALOCEAN_ACCESS_TOKEN }}

      - name: Configure kubectl
        run: doctl kubernetes cluster kubeconfig save okla-cluster

      - name: Deploy to Kubernetes
        run: |
          kubectl set image deployment/frontend-web \
            frontend-web=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }} \
            -n okla

      - name: Wait for rollout
        run: kubectl rollout status deployment/frontend-web -n okla --timeout=300s

      - name: Verify deployment
        run: |
          kubectl get pods -n okla -l app=frontend-web
          curl -sf https://okla.com.do/api/health || exit 1
```

---

## 🔧 PASO 7: Scripts de Deploy Local

```bash
# filepath: frontend/web-next/scripts/deploy.sh
#!/bin/bash
# =============================================================================
# Deploy Script - OKLA Frontend
# =============================================================================

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configuration
IMAGE_NAME="ghcr.io/gregorymorenoiem/okla-frontend"
TAG="${1:-latest}"

echo -e "${YELLOW}🚀 Starting deployment...${NC}"

# Build
echo -e "${GREEN}📦 Building Docker image...${NC}"
docker build \
  --build-arg NEXT_PUBLIC_API_URL=https://api.okla.com.do \
  --build-arg NEXT_PUBLIC_APP_VERSION=$(git describe --tags --always) \
  -t ${IMAGE_NAME}:${TAG} \
  .

# Push
echo -e "${GREEN}📤 Pushing to registry...${NC}"
docker push ${IMAGE_NAME}:${TAG}

# Deploy (if kubectl configured)
if command -v kubectl &> /dev/null; then
  echo -e "${GREEN}☸️  Deploying to Kubernetes...${NC}"
  kubectl set image deployment/frontend-web \
    frontend-web=${IMAGE_NAME}:${TAG} \
    -n okla

  kubectl rollout status deployment/frontend-web -n okla --timeout=300s
fi

echo -e "${GREEN}✅ Deployment complete!${NC}"
```

---

## 📊 MÉTRICAS DE VALIDACIÓN

### Verificar Build

```bash
# Build local
docker build -t okla-frontend:test .

# Verificar tamaño de imagen
docker images okla-frontend:test
# Esperado: < 200MB

# Ejecutar localmente
docker run -p 3000:3000 okla-frontend:test

# Verificar health check
curl http://localhost:3000/api/health
# Esperado: {"status":"healthy",...}
```

### Verificar en Producción

```bash
# Verificar pods
kubectl get pods -n okla -l app=frontend-web
# Esperado: 3/3 Running

# Verificar logs
kubectl logs -f deployment/frontend-web -n okla

# Verificar health
curl https://okla.com.do/api/health

# Lighthouse audit
npx lighthouse https://okla.com.do --view
# Esperado: Performance > 90
```

---

## 🚨 TROUBLESHOOTING

### Error: "Cannot find module 'sharp'"

```dockerfile
# Agregar en Stage 3 (runner)
RUN apk add --no-cache libc6-compat
COPY --from=builder /app/node_modules/.pnpm/sharp*/node_modules/sharp ./node_modules/sharp
```

### Error: "EACCES permission denied"

```dockerfile
# Verificar permisos en Dockerfile
RUN chown -R nextjs:nodejs /app
USER nextjs
```

### Error: "health check failing"

```bash
# Verificar que la ruta existe
curl -v http://localhost:3000/api/health

# Verificar logs del pod
kubectl logs deployment/frontend-web -n okla --previous
```

### Pods en CrashLoopBackOff

```bash
# Ver eventos
kubectl describe pod -n okla -l app=frontend-web

# Ver logs del pod anterior
kubectl logs -n okla -l app=frontend-web --previous

# Posibles causas:
# 1. Variables de entorno faltantes
# 2. Puerto incorrecto
# 3. Health check fallando
```

---

## 🔗 REFERENCIAS

- [Next.js Deployment](https://nextjs.org/docs/deployment)
- [Docker Multi-stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [Kubernetes Deployments](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [GitHub Actions](https://docs.github.com/en/actions)

---

## ✅ CHECKLIST DE DEPLOY

- [ ] Dockerfile creado y probado localmente
- [ ] next.config.ts con `output: "standalone"`
- [ ] Health check endpoint funcionando
- [ ] Variables de entorno configuradas
- [ ] Kubernetes manifests creados
- [ ] GitHub Actions workflow configurado
- [ ] Secrets configurados en GitHub
- [ ] Imagen publicada en ghcr.io
- [ ] Deploy exitoso a producción
- [ ] Health check pasando en producción
- [ ] Lighthouse score > 90

---

## 🧪 TESTS E2E POST-DEPLOY

```typescript
// filepath: e2e/deploy-verification.spec.ts
import { test, expect } from "@playwright/test";

const PROD_URL = "https://okla.com.do";

test.describe("Production Verification", () => {
  test("health check returns 200", async ({ request }) => {
    const response = await request.get(`${PROD_URL}/api/health`);
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.status).toBe("healthy");
  });

  test("homepage loads correctly", async ({ page }) => {
    await page.goto(PROD_URL);
    await expect(page).toHaveTitle(/OKLA/);
    await expect(page.locator("header")).toBeVisible();
    await expect(page.locator("footer")).toBeVisible();
  });

  test("no console errors on homepage", async ({ page }) => {
    const errors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") {
        errors.push(msg.text());
      }
    });

    await page.goto(PROD_URL);
    await page.waitForLoadState("networkidle");

    expect(errors).toHaveLength(0);
  });

  test("API connectivity", async ({ request }) => {
    const response = await request.get(`${PROD_URL}/api/health`);
    const body = await response.json();

    expect(body.api?.status).toBe("connected");
  });
});
```

---

_Última actualización: Enero 31, 2026_
