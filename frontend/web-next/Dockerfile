# syntax=docker/dockerfile:1
# ============================================
# OKLA Frontend - Next.js Production Docker Image
# ============================================
# Cross-compilation multi-stage build:
#   deps + builder stages run NATIVELY on the host (ARM64 on macOS runners)
#   runner stage targets linux/amd64 for Kubernetes (DOKS)
# This avoids QEMU emulation for the heavy `next build` step,
# preventing segfaults and cutting build time from 45min+ to ~5min.
# ============================================

# ── ARGs available before FROM ──
ARG NODE_VERSION=20

# ── Stage 1: Install dependencies (native platform) ──
FROM --platform=$BUILDPLATFORM node:${NODE_VERSION}-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

RUN corepack enable && corepack prepare pnpm@9 --activate

COPY package.json pnpm-lock.yaml ./

RUN --mount=type=cache,id=pnpm-store,target=/root/.local/share/pnpm/store \
    pnpm install --frozen-lockfile

# ── Stage 2: Build Next.js (native platform — NO QEMU) ──
FROM --platform=$BUILDPLATFORM node:${NODE_VERSION}-alpine AS builder
WORKDIR /app

RUN corepack enable && corepack prepare pnpm@9 --activate

COPY --from=deps /app/node_modules ./node_modules
COPY . .

ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production

# Limit Node heap to 2GB — prevents OOM when other containers are running
ENV NODE_OPTIONS=--max-old-space-size=2048

# BFF pattern: browser uses relative URLs, Next.js rewrites to gateway
ARG NEXT_PUBLIC_API_URL=
ARG NEXT_PUBLIC_SITE_URL=https://okla.com.do
ARG INTERNAL_API_URL=http://gateway:8080

ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_SITE_URL=$NEXT_PUBLIC_SITE_URL
ENV INTERNAL_API_URL=$INTERNAL_API_URL

RUN pnpm build

# ── Stage 3: Production runner (target platform = linux/amd64 for K8s) ──
# $TARGETPLATFORM is set automatically by Docker Buildx from --platform flag
FROM --platform=$TARGETPLATFORM node:${NODE_VERSION}-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy built assets
# next.js standalone output includes the server code in .next/standalone
# but does NOT include the browser-facing static files (.next/static).
# Those MUST be copied separately, otherwise /_next/static/* returns 404.
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static

# Set ownership
RUN chown -R nextjs:nodejs /app

USER nextjs

# Expose port (8080 for Kubernetes)
EXPOSE 8080

ENV PORT=8080
ENV HOSTNAME="0.0.0.0"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/ || exit 1

# Start the application
CMD ["node", "server.js"]
