# ============================================
# OKLA Frontend - Next.js 14 Docker Image
# ============================================
# Multi-stage build for optimized production image

# Stage 1: Dependencies
FROM node:20-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

# Install pnpm
RUN corepack enable && corepack prepare pnpm@9 --activate

# Copy package files
COPY package.json pnpm-lock.yaml ./

# Install dependencies
RUN pnpm install --frozen-lockfile

# Stage 2: Builder
FROM node:20-alpine AS builder
WORKDIR /app

# Install pnpm
RUN corepack enable && corepack prepare pnpm@9 --activate

# Copy dependencies from deps stage
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Set environment variables for build
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production

# Production: BFF pattern — browser uses relative URLs, Next.js rewrites to gateway
ARG NEXT_PUBLIC_API_URL=
ARG NEXT_PUBLIC_SITE_URL=https://okla.com.do
# INTERNAL_API_URL used by rewrites() at build time to set the proxy destination
ARG INTERNAL_API_URL=http://gateway:8080

# Set as ENV so Next.js can read them during build
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_SITE_URL=$NEXT_PUBLIC_SITE_URL
ENV INTERNAL_API_URL=$INTERNAL_API_URL

# Build the application
RUN pnpm build

# Stage 3: Runner (Production)
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create non-root user for security
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy built assets
# Note: `standalone` output mode creates .next/standalone with everything
# including .next/static bundled inside. Just copy the whole standalone dir
# and the public folder. The double-copy of .next/static is unnecessary.
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./

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
