# 🐳☸️ Deep Audit: Docker, Kubernetes & CI/CD Deployment Configurations

**Proyecto:** OKLA (CarDealer Microservices)  
**Fecha:** 13 de Febrero, 2026  
**Auditor:** GitHub Copilot — Claude Opus 4.6  
**Scope:** Dockerfiles, Kubernetes manifests (`k8s/`), GitHub Actions CI/CD workflows  
**Servicios auditados:** AuthService, Gateway, ErrorService, MediaService, NotificationService, AdminService, ContactService

---

## 📊 Executive Summary

| Category                     | Score     | Critical | High | Medium | Low |
| ---------------------------- | --------- | -------- | ---- | ------ | --- |
| **Dockerfiles**              | 🟢 8.5/10 | 0        | 1    | 2      | 2   |
| **Kubernetes — Security**    | 🟢 8/10   | 1        | 1    | 2      | 1   |
| **Kubernetes — Reliability** | 🟢 8/10   | 0        | 2    | 2      | 1   |
| **Kubernetes — Data**        | 🔴 4/10   | 3        | 2    | 1      | 0   |
| **CI/CD Workflows**          | 🟡 7/10   | 0        | 2    | 3      | 2   |
| **12-Factor Compliance**     | 🟢 8/10   | 0        | 1    | 2      | 0   |

**Overall: 🟡 7.3/10 — Good foundation with critical data-layer and secrets issues**

---

## 1️⃣ DOCKERFILE AUDIT

### ✅ What's Done Well

| Practice             | Status | Evidence                                                   |
| -------------------- | ------ | ---------------------------------------------------------- |
| Multi-stage builds   | ✅     | All 7 Dockerfiles use `sdk:8.0` → `aspnet:8.0-alpine`      |
| Minimal base images  | ✅     | Alpine variants (`aspnet:8.0-alpine`) across all services  |
| Non-root user        | ✅     | `adduser/addgroup` with `USER appuser` (UID 1000)          |
| Layer caching        | ✅     | `.csproj` COPY → restore → full COPY pattern               |
| HEALTHCHECK          | ✅     | All Dockerfiles include `wget`-based healthcheck           |
| Port 8080            | ✅     | Consistent `EXPOSE 8080` + `ASPNETCORE_URLS=http://+:8080` |
| No secrets in images | ✅     | Secrets directory created at runtime (`/run/secrets`)      |
| `.dockerignore`      | ✅     | Comprehensive at `backend/.dockerignore` (74 lines)        |
| `UseAppHost=false`   | ✅     | All publish stages use this flag for smaller images        |

### 🔴 Findings

---

#### F-D1: Inconsistent `wget` Installation — Some Services Redundantly Install wget

**Severity:** 🟡 Low  
**Affected:** Gateway, MediaService, ContactService Dockerfiles  
**Details:**

The `aspnet:8.0-alpine` base image **does NOT include wget by default**. However, only 3 of 7 Dockerfiles explicitly install it:

| Service             | Installs wget?               | Uses wget in HEALTHCHECK? |
| ------------------- | ---------------------------- | ------------------------- |
| AuthService         | ❌ No explicit install       | ✅ Yes                    |
| Gateway             | ✅ `apk add --no-cache wget` | ✅ Yes                    |
| ErrorService        | ❌ No explicit install       | ✅ Yes                    |
| MediaService        | ✅ `apk add --no-cache wget` | ✅ Yes                    |
| NotificationService | ❌ No explicit install       | ✅ Yes                    |
| AdminService        | ❌ No explicit install       | ✅ Yes                    |
| ContactService      | ✅ `apk add --no-cache wget` | ✅ Yes                    |

**Risk:** Healthchecks may silently fail at runtime for AuthService, ErrorService, NotificationService, AdminService if `wget` is not present.

**Remediation:**

```dockerfile
# Add to ALL Dockerfiles that use wget in HEALTHCHECK (before user creation):
RUN apk add --no-cache wget
```

Or switch to the built-in `curl` by using `aspnet:8.0` (Debian-based) instead, or use a .NET-based healthcheck:

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD dotnet --info > /dev/null 2>&1 && wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1
```

---

#### F-D2: AdminService Uses Different HEALTHCHECK Syntax

**Severity:** 🟡 Low  
**Affected:** [AdminService/Dockerfile](../backend/AdminService/Dockerfile)  
**Details:**

All services use `--spider` mode except AdminService:

```dockerfile
# All other services:
CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# AdminService uses:
CMD wget --no-verbose --tries=1 http://localhost:8080/health -qO- > /dev/null || exit 1
```

The `--spider` flag is more efficient as it sends a HEAD request (no body download). The AdminService downloads the full response body and discards it.

**Remediation:** Standardize to `--spider` across all services.

---

#### F-D3: `COPY . .` Copies Entire Backend Context

**Severity:** 🟡 Medium  
**Affected:** All 7 Dockerfiles  
**Details:**

All Dockerfiles use `COPY . .` in the build stage, which copies the entire `backend/` directory. The Docker build context is `./backend` (confirmed in CI workflow). This means every service build includes source code from all other 86 services, inflating the build context by potentially hundreds of MB.

**Remediation:**  
The `.dockerignore` already excludes `bin/`, `obj/`, etc. This is acceptable given the multi-stage build discards it, but consider adding service-specific `.dockerignore` files or restructuring the build context for faster CI builds:

```dockerfile
# More selective copy (only what's needed):
COPY ["AuthService/", "AuthService/"]
COPY ["_Shared/", "_Shared/"]
COPY ["ServiceDiscovery/", "ServiceDiscovery/"]
```

---

#### F-D4: No Explicit Image Pinning to Digest

**Severity:** 🟡 Medium  
**Affected:** All Dockerfiles  
**Details:**

All Dockerfiles use tag-based references (`mcr.microsoft.com/dotnet/sdk:8.0`, `aspnet:8.0-alpine`). These tags are mutable — a future push to the same tag could introduce breaking changes or vulnerabilities.

**Remediation (for production hardening):**

```dockerfile
# Pin to SHA digest:
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine@sha256:<digest> AS final
```

---

#### F-D5: Missing `LABEL` Metadata for Traceability

**Severity:** 🟢 Low  
**Affected:** All Dockerfiles  
**Details:**

No OCI/OpenContainers labels are present. These are important for image provenance, vulnerability tracking, and container orchestration.

**Remediation:**

```dockerfile
# Add after FROM in the final stage:
LABEL org.opencontainers.image.source="https://github.com/gregorymorenoiem/cardealer-microservices"
LABEL org.opencontainers.image.description="OKLA AuthService"
LABEL org.opencontainers.image.version="${BUILD_VERSION}"
LABEL org.opencontainers.image.created="${BUILD_DATE}"
```

---

#### F-D6: MediaService Missing `readOnlyRootFilesystem` in K8s vs Dockerfile

**Severity:** 🟠 High  
**Affected:** MediaService  
**Details:**

MediaService's Dockerfile creates `/app/uploads` for file uploads. In the K8s deployment, the `readOnlyRootFilesystem` is **missing** (unlike all other services), creating an inconsistency:

```yaml
# MediaService K8s (deployments.yaml) — MISSING readOnlyRootFilesystem:
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  allowPrivilegeEscalation: false
  capabilities:
    drop:
      - ALL
  # readOnlyRootFilesystem: true  ← MISSING!
```

This means the MediaService container can write anywhere in its filesystem, which is a security risk.

**Remediation:** Add `readOnlyRootFilesystem: true` and mount writable volumes:

```yaml
securityContext:
  readOnlyRootFilesystem: true # ADD THIS
volumeMounts:
  - name: tmp
    mountPath: /tmp
  - name: uploads
    mountPath: /app/uploads # Already present
```

---

## 2️⃣ KUBERNETES AUDIT

### 2.1 Security Context & Pod Security

#### ✅ What's Done Well

| Practice                              | Status                                                  |
| ------------------------------------- | ------------------------------------------------------- |
| `runAsNonRoot: true`                  | ✅ All deployments                                      |
| `runAsUser: 1000`                     | ✅ All deployments                                      |
| `allowPrivilegeEscalation: false`     | ✅ All deployments                                      |
| `capabilities.drop: [ALL]`            | ✅ All deployments                                      |
| `readOnlyRootFilesystem: true`        | ✅ 10/11 deployments (MediaService missing)             |
| `automountServiceAccountToken: false` | ✅ All deployments                                      |
| Dedicated ServiceAccounts             | ✅ `okla-frontend`, `okla-backend`, `okla-gateway`      |
| RBAC Roles with least privilege       | ✅ Frontend has empty rules; backend has limited access |
| Network Policies                      | ✅ Default deny + granular allow rules                  |
| `/tmp` emptyDir mounts                | ✅ All services mount writable `/tmp`                   |

#### 🔴 Findings

---

#### F-K1: 🔴 CRITICAL — Hardcoded Plaintext Passwords in db-secrets.yaml (Committed to Git)

**Severity:** 🔴 Critical  
**Affected:** [k8s/db-secrets.yaml](../k8s/db-secrets.yaml)  
**Details:**

The file `db-secrets.yaml` contains **real production passwords in plaintext** committed to the git repository:

```yaml
# db-secrets.yaml — COMMITTED TO GIT:
Database__Password: "OklaDB2025!"
Jwt__Key: "OklaSecretKey2025VeryLongAndSecure123456789!"
ConnectionStrings__DefaultConnection: "Host=postgres;...Password=OklaDB2025!;..."
```

**This is the single most critical security finding in this audit.** Anyone with read access to this repository has:

- Database superuser password (`postgres` / `OklaDB2025!`)
- JWT signing key (`OklaSecretKey2025VeryLongAndSecure123456789!`)
- Redis connection strings
- Full connection strings for all 8+ databases

**Remediation (URGENT):**

1. **Immediately rotate ALL secrets** — they must be considered compromised
2. **Delete `db-secrets.yaml`** from the repository and git history:
   ```bash
   git filter-repo --path k8s/db-secrets.yaml --invert-paths
   ```
3. **Use the template pattern** already established in `secrets.template.yaml`:
   ```bash
   # Deploy secrets using envsubst (secrets from CI/CD):
   envsubst < k8s/secrets.template.yaml | kubectl apply -f -
   ```
4. **Add to `.gitignore`:**
   ```
   k8s/db-secrets.yaml
   k8s/secrets.yaml
   ```
5. **Long-term:** Adopt External Secrets Operator or Sealed Secrets (already mentioned in `secrets.template.yaml` comments)

---

#### F-K2: 🔴 CRITICAL — RabbitMQ Uses Default guest/guest Credentials

**Severity:** 🔴 Critical  
**Affected:** [k8s/infrastructure.yaml](../k8s/infrastructure.yaml#L250-L270)  
**Details:**

```yaml
# infrastructure.yaml — RabbitMQ:
env:
  - name: RABBITMQ_DEFAULT_USER
    value: "guest"
  - name: RABBITMQ_DEFAULT_PASS
    value: "guest"
```

The default `guest/guest` credentials are well-known and provide full admin access to the RabbitMQ management console (port 15672). Combined with the network policy allowing backend services to reach port 15672, any compromised backend pod could manage all queues.

**Remediation:**

```yaml
env:
  - name: RABBITMQ_DEFAULT_USER
    valueFrom:
      secretKeyRef:
        name: rabbitmq-secrets
        key: RabbitMQ__Username
  - name: RABBITMQ_DEFAULT_PASS
    valueFrom:
      secretKeyRef:
        name: rabbitmq-secrets
        key: RabbitMQ__Password
```

---

#### F-K3: 🔴 CRITICAL — PostgreSQL Password Hardcoded in infrastructure.yaml

**Severity:** 🔴 Critical  
**Affected:** [k8s/infrastructure.yaml](../k8s/infrastructure.yaml#L15-L20)  
**Details:**

```yaml
# infrastructure.yaml:
apiVersion: v1
kind: Secret
metadata:
  name: postgres-master-secret
stringData:
  POSTGRES_PASSWORD: "OklaDB2025!"
  password: "OklaDB2025!"
```

This is the same password found in `db-secrets.yaml`, but it's **also** hardcoded in the infrastructure template that creates the PostgreSQL StatefulSet. Even if `db-secrets.yaml` is removed, this file still exposes the master password.

**Remediation:**

```yaml
# Replace with template variable:
stringData:
  POSTGRES_PASSWORD: "${POSTGRES_MASTER_PASSWORD}"
  password: "${POSTGRES_MASTER_PASSWORD}"
```

---

#### F-K4: Redis Has No Password Authentication

**Severity:** 🟠 High  
**Affected:** [k8s/infrastructure.yaml](../k8s/infrastructure.yaml#L170-L185) and [k8s/db-secrets.yaml](../k8s/db-secrets.yaml)  
**Details:**

Redis is started without `--requirepass`:

```yaml
command:
  [
    "redis-server",
    "--appendonly",
    "yes",
    "--maxmemory",
    "128mb",
    "--maxmemory-policy",
    "allkeys-lru",
  ]
# No --requirepass flag!
```

And the secret merely contains `ConnectionStrings__Redis: "redis:6379"` with no password.

**Remediation:**

```yaml
command:
  - redis-server
  - "--appendonly"
  - "yes"
  - "--maxmemory"
  - "128mb"
  - "--maxmemory-policy"
  - "allkeys-lru"
  - "--requirepass"
  - "$(REDIS_PASSWORD)"
env:
  - name: REDIS_PASSWORD
    valueFrom:
      secretKeyRef:
        name: redis-secrets
        key: REDIS_PASSWORD
```

---

#### F-K5: `Include Error Detail=true` in Connection Strings

**Severity:** 🟡 Medium  
**Affected:** All connection strings in [k8s/db-secrets.yaml](../k8s/db-secrets.yaml)  
**Details:**

```
ConnectionStrings__DefaultConnection: "Host=postgres;...Include Error Detail=true"
```

The `Include Error Detail=true` flag causes PostgreSQL to return detailed error messages including table names, column names, constraint names, and potentially query fragments. This should be `false` in production.

**Remediation:** Remove `Include Error Detail=true` from all production connection strings, or replace with environment-specific configuration.

---

### 2.2 Probes & Reliability

#### ✅ What's Done Well

| Practice                  | Status                                               |
| ------------------------- | ---------------------------------------------------- |
| Startup probes            | ✅ All deployments (generous `failureThreshold: 24`) |
| Liveness probes           | ✅ All deployments (`/health` or `/health/live`)     |
| Readiness probes          | ✅ All deployments (`/health` or `/health/ready`)    |
| PodDisruptionBudgets      | ✅ All core services (9 PDBs)                        |
| HorizontalPodAutoscalers  | ✅ All services (10 HPAs with scaling behavior)      |
| Resource requests/limits  | ✅ All pods including infrastructure                 |
| Velero backup schedule    | ✅ Daily at 2:00 AM UTC                              |
| PostgreSQL backup CronJob | ✅ Daily at 3:00 AM UTC                              |

#### 🔴 Findings

---

#### F-K6: Conflicting Database Architecture — Dual PostgreSQL Patterns

**Severity:** 🟠 High  
**Affected:** [k8s/infrastructure.yaml](../k8s/infrastructure.yaml) vs [k8s/databases.yaml](../k8s/databases.yaml)  
**Details:**

Two mutually exclusive database architectures exist:

1. **`infrastructure.yaml`** — Single PostgreSQL StatefulSet with init script creating per-service databases
2. **`databases.yaml`** — Multiple individual PostgreSQL StatefulSets (one per service: `authservice-db`, `userservice-db`, etc.)

Both reference different secrets (`postgres-master-secret` vs `postgres-password`). It's unclear which is the active production configuration.

**Impact:** Applying both files creates duplicate PostgreSQL instances, wasting resources and potentially splitting data.

**Remediation:** Choose ONE pattern and delete the other:

- **Recommended:** Keep `infrastructure.yaml` (single instance) for cost savings on DOKS
- Archive `databases.yaml` as `databases.yaml.archive` or delete it
- Document the chosen pattern in [k8s/README.md](../k8s/README.md)

---

#### F-K7: Redis Data Uses `emptyDir` — Data Lost on Pod Restart

**Severity:** 🟠 High  
**Affected:** [k8s/infrastructure.yaml](../k8s/infrastructure.yaml#L180-L190)  
**Details:**

```yaml
# Redis volume:
volumes:
  - name: redis-data
    emptyDir: {}
```

Redis is configured with `--appendonly yes` (AOF persistence) but the volume is `emptyDir`. On pod restart/reschedule, all cached data and AOF files are lost. While Redis is used as a cache, losing all sessions, rate-limit counters, and cached data simultaneously on restart impacts availability.

**Remediation:**

```yaml
# Option A: PVC for persistence (recommended for session data):
volumeClaimTemplates:
  - metadata:
      name: redis-data
    spec:
      accessModes: ["ReadWriteOnce"]
      storageClassName: do-block-storage
      resources:
        requests:
          storage: 1Gi

# Option B: Accept data loss, remove --appendonly yes (pure cache):
command:
  ["redis-server", "--maxmemory", "128mb", "--maxmemory-policy", "allkeys-lru"]
```

---

#### F-K8: No Pod Anti-Affinity Rules

**Severity:** 🟡 Medium  
**Affected:** All deployments  
**Details:**

No anti-affinity rules exist to spread pods across nodes. If the HPA scales AuthService to 6 replicas, all 6 could land on the same node. A single node failure would take down all replicas.

**Remediation (for critical services):**

```yaml
spec:
  template:
    spec:
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - weight: 100
              podAffinityTerm:
                labelSelector:
                  matchExpressions:
                    - key: app
                      operator: In
                      values:
                        - authservice
                topologyKey: kubernetes.io/hostname
```

---

#### F-K9: AuthService HPA `minReplicas: 2` but Deployment `replicas: 1`

**Severity:** 🟡 Medium  
**Affected:** AuthService  
**Details:**

```yaml
# deployments.yaml:
replicas: 1 # Initial deployment

# hpa.yaml:
minReplicas: 2 # HPA minimum
```

The HPA will scale to 2 shortly after deployment, but there's a window where only 1 pod exists. Also, PDB sets `minAvailable: 1` which is correct for the HPA min of 2.

**Remediation:** Set `replicas: 2` in the deployment to match HPA min:

```yaml
spec:
  replicas: 2 # Match HPA minReplicas
```

---

#### F-K10: No Graceful Shutdown Configuration

**Severity:** 🟢 Low  
**Affected:** All deployments  
**Details:**

No `terminationGracePeriodSeconds` is explicitly set (defaults to 30s). For services processing long-running requests (MediaService uploads, BillingService payments), 30s may not be enough.

**Remediation:**

```yaml
spec:
  template:
    spec:
      terminationGracePeriodSeconds: 60 # For MediaService, BillingService
```

---

### 2.3 Network Policies

#### ✅ What's Done Well

- Default deny ingress in `okla` namespace
- Gateway is the only entry point from ingress-nginx
- Backend services only accept from gateway
- Infrastructure (PostgreSQL, Redis, RabbitMQ) only accepts from backend tier
- DNS egress allowed for all pods
- External egress allowed (for Stripe, S3, etc.) with private IP exclusion

#### 🔴 Finding

---

#### F-K11: Duplicate Network Policies in rbac.yaml vs network-policies.yaml

**Severity:** 🟡 Medium  
**Affected:** [k8s/rbac.yaml](../k8s/rbac.yaml) and [k8s/network-policies.yaml](../k8s/network-policies.yaml)  
**Details:**

`rbac.yaml` contains NetworkPolicy resources (`deny-all-ingress`, `allow-ingress-to-frontend`, `allow-ingress-to-gateway`) that duplicate policies already defined in `network-policies.yaml` (`default-deny-ingress`, `allow-frontend-ingress`, `allow-gateway-ingress`).

Since Kubernetes NetworkPolicies are additive, duplicates don't break anything but cause confusion about which file is authoritative.

**Remediation:** Remove all NetworkPolicy resources from `rbac.yaml` — keep network policies exclusively in `network-policies.yaml`.

---

### 2.4 Ingress

#### ✅ What's Done Well

- TLS termination with Let's Encrypt via cert-manager
- Separate hosts for frontend (`okla.com.do`) and API (`api.okla.com.do`)
- Proxy body size limit (`50m`)
- Read timeout configured (`60s`)

#### 🔴 Findings

---

#### F-K12: Missing Security Headers in Ingress Annotations

**Severity:** 🟡 Medium  
**Affected:** [k8s/ingress.yaml](../k8s/ingress.yaml)  
**Details:**

The Ingress is missing important security header annotations:

**Remediation:**

```yaml
annotations:
  # Existing:
  nginx.ingress.kubernetes.io/proxy-body-size: "50m"
  nginx.ingress.kubernetes.io/proxy-read-timeout: "60"
  # ADD:
  nginx.ingress.kubernetes.io/configuration-snippet: |
    more_set_headers "X-Frame-Options: SAMEORIGIN";
    more_set_headers "X-Content-Type-Options: nosniff";
    more_set_headers "X-XSS-Protection: 1; mode=block";
    more_set_headers "Referrer-Policy: strict-origin-when-cross-origin";
    more_set_headers "Permissions-Policy: camera=(), microphone=(), geolocation=()";
  nginx.ingress.kubernetes.io/ssl-redirect: "true"
  nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
  # Rate limiting at ingress level:
  nginx.ingress.kubernetes.io/limit-rps: "50"
  nginx.ingress.kubernetes.io/limit-connections: "20"
```

---

### 2.5 Resource Quotas & Limits

#### ✅ What's Done Well

- Namespace-level ResourceQuota (50 pods, 4 CPU request / 8 CPU limit, 8Gi / 16Gi memory)
- LimitRange with sensible defaults (200m CPU, 256Mi memory default limit)
- Per-container min/max boundaries
- Pod-level max limits

No findings for this section — well configured.

---

## 3️⃣ CI/CD AUDIT

### ✅ What's Done Well

| Practice                     | Status                                                      |
| ---------------------------- | ----------------------------------------------------------- |
| Smart change detection       | ✅ `dorny/paths-filter` per service                         |
| Reusable workflows           | ✅ `_reusable-dotnet-service.yml`, `_reusable-frontend.yml` |
| Concurrency control          | ✅ `cancel-in-progress: true`                               |
| NuGet caching                | ✅ `actions/cache@v4` with `.csproj` hash                   |
| Docker layer caching         | ✅ BuildX local cache with cache rotation                   |
| Image vulnerability scanning | ✅ Trivy CRITICAL/HIGH scan with SARIF upload               |
| NuGet vulnerability audit    | ✅ `dotnet list package --vulnerable`                       |
| Rollback mechanism           | ✅ `kubectl rollout undo` on failure                        |
| Test results upload          | ✅ Artifact retention 7 days                                |
| Docker push only on main     | ✅ Conditional `run-docker-push`                            |
| PR quality checks            | ✅ Size check, commit message validation                    |
| Environment protection       | ✅ `environment: staging/production`                        |

### 🔴 Findings

---

#### F-CI1: Legacy `test.yml` Uses npm Instead of pnpm

**Severity:** 🟠 High  
**Affected:** [.github/workflows/test.yml](.github/workflows/test.yml)  
**Details:**

The `test.yml` workflow uses `npm ci` and `npm test` for frontend tests:

```yaml
cache: "npm"
cache-dependency-path: frontend/web/package-lock.json
# ...
run: npm ci
run: npm test -- --run --reporter=verbose
```

The project uses **pnpm exclusively** (per copilot-instructions.md). This workflow will fail or produce incorrect results.

**Remediation:** Either:

1. Delete `test.yml` (replaced by `smart-cicd.yml` + reusable workflows)
2. Or update to pnpm:

```yaml
- uses: pnpm/action-setup@v4
  with:
    version: 9
- run: pnpm install --frozen-lockfile
- run: pnpm test -- --run --reporter=verbose
```

---

#### F-CI2: `:latest` Tag Pushed to Registry

**Severity:** 🟠 High  
**Affected:** [.github/workflows/\_reusable-dotnet-service.yml](../.github/workflows/_reusable-dotnet-service.yml#L210-L215)  
**Details:**

```yaml
tags: |
  ${{ inputs.docker-registry }}/${{ github.repository_owner }}/${{ inputs.service-name }}:${{ needs.build-and-test.outputs.version }}
  ${{ inputs.docker-registry }}/${{ github.repository_owner }}/${{ inputs.service-name }}:latest
```

Pushing `:latest` to GHCR is problematic:

- K8s deployments using `:latest` get unpredictable behavior
- No way to know which version `:latest` points to
- Rollbacks become unreliable

**Remediation:** Remove the `:latest` tag push. Use only versioned tags:

```yaml
tags: |
  ${{ inputs.docker-registry }}/${{ github.repository_owner }}/${{ inputs.service-name }}:${{ needs.build-and-test.outputs.version }}
  ${{ inputs.docker-registry }}/${{ github.repository_owner }}/${{ inputs.service-name }}:${{ github.sha }}
```

---

#### F-CI3: No Integration Tests in CI Pipeline

**Severity:** 🟡 Medium  
**Affected:** All CI workflows  
**Details:**

The CI pipeline only runs unit tests (`dotnet test`). There are no integration tests that validate:

- Database migrations work
- API endpoints respond correctly
- Inter-service communication functions
- RabbitMQ message handling

**Remediation:** Add a service-level integration test job using Docker Compose:

```yaml
integration-tests:
  name: 🧪 Integration Tests
  needs: build-and-test
  runs-on: ubuntu-latest
  services:
    postgres:
      image: postgres:16-alpine
      env:
        POSTGRES_PASSWORD: test
      ports: ["5432:5432"]
    redis:
      image: redis:7-alpine
      ports: ["6379:6379"]
  steps:
    - uses: actions/checkout@v4
    - run: dotnet test --filter "Category=Integration"
```

---

#### F-CI4: Deploy Workflow Applies Hardcoded db-secrets.yaml

**Severity:** 🟡 Medium  
**Affected:** [.github/workflows/smart-cicd.yml](../.github/workflows/smart-cicd.yml) (deploy-staging step)  
**Details:**

The staging deployment step applies `db-secrets.yaml` which contains hardcoded passwords:

```yaml
kubectl apply -f k8s/db-secrets.yaml
```

Meanwhile, the dedicated `deploy-digitalocean.yml` correctly creates secrets from GitHub secrets. This creates two conflicting secret management approaches.

**Remediation:** Remove `kubectl apply -f k8s/db-secrets.yaml` from `smart-cicd.yml`. Use the same GitHub-secrets-based approach as `deploy-digitalocean.yml`.

---

#### F-CI5: `${IMAGE_TAG}` Variable in deployments.yaml Never Substituted

**Severity:** 🟡 Medium  
**Affected:** [k8s/deployments.yaml](../k8s/deployments.yaml) + deploy workflows  
**Details:**

All deployment images use `${IMAGE_TAG}`:

```yaml
image: ghcr.io/gregorymorenoiem/authservice:${IMAGE_TAG}
```

But neither deploy workflow performs `envsubst` or `sed` to replace this variable. `kubectl apply` applies it literally as `${IMAGE_TAG}`.

The `deploy-digitalocean.yml` works around this by restarting deployments after apply, which pulls the latest image. This is fragile.

**Remediation:**

```bash
# In deploy workflow, before kubectl apply:
export IMAGE_TAG="${{ needs.prepare.outputs.version }}"
envsubst < k8s/deployments.yaml | kubectl apply -f -
```

---

#### F-CI6: Trivy Scan Uses `continue-on-error: true`

**Severity:** 🟡 Medium  
**Affected:** [.github/workflows/\_reusable-dotnet-service.yml](../.github/workflows/_reusable-dotnet-service.yml#L220-L230)  
**Details:**

```yaml
- name: 🔒 Run Trivy vulnerability scanner
  continue-on-error: true # ← Scan failures are silently ignored
```

Critical vulnerabilities don't block the pipeline. An image with known CVEs can be pushed to production.

**Remediation:** Remove `continue-on-error` and add exit-code control:

```yaml
- name: 🔒 Run Trivy vulnerability scanner
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: "..."
    format: "sarif"
    output: "trivy-results.sarif"
    severity: "CRITICAL"
    exit-code: "1" # Fail pipeline on critical vulnerabilities
```

---

#### F-CI7: No SBOM Generation

**Severity:** 🟢 Low  
**Affected:** All CI workflows  
**Details:**

No Software Bill of Materials (SBOM) is generated during the build process. SBOMs are increasingly required for compliance and vulnerability tracking.

**Remediation:**

```yaml
- name: 📋 Generate SBOM
  uses: anchore/sbom-action@v0
  with:
    image: "${{ inputs.docker-registry }}/${{ github.repository_owner }}/${{ inputs.service-name }}:${{ needs.build-and-test.outputs.version }}"
    output-file: sbom-${{ inputs.service-name }}.spdx.json
```

---

## 4️⃣ 12-FACTOR APP COMPLIANCE

| Factor               | Status     | Evidence                                                       |
| -------------------- | ---------- | -------------------------------------------------------------- |
| I. Codebase          | ✅         | Single monorepo in Git                                         |
| II. Dependencies     | ✅         | NuGet `.csproj` + pnpm `package.json`                          |
| III. Config          | 🟡 Partial | ConfigMaps ✅, but hardcoded secrets ❌                        |
| IV. Backing services | ✅         | PostgreSQL, Redis, RabbitMQ via K8s services                   |
| V. Build/release/run | ✅         | Multi-stage Docker + CI/CD pipeline                            |
| VI. Processes        | ✅         | Stateless containers + external state stores                   |
| VII. Port binding    | ✅         | All services self-host on port 8080                            |
| VIII. Concurrency    | ✅         | HPA for horizontal scaling                                     |
| IX. Disposability    | ✅         | Health probes + startup probes + PDB                           |
| X. Dev/prod parity   | 🟡 Partial | Docker for dev, K8s for prod; `ASPNETCORE_ENVIRONMENT` differs |
| XI. Logs             | ✅         | Serilog with structured logging to stdout                      |
| XII. Admin processes | ✅         | DB migrations via `AutoMigrate`, backup CronJob                |

---

## 5️⃣ PRIORITIZED REMEDIATION PLAN

### 🔴 P0 — Do Immediately (Security)

| #   | Finding | Effort | Action                                                                             |
| --- | ------- | ------ | ---------------------------------------------------------------------------------- |
| 1   | F-K1    | 2h     | Rotate ALL secrets, remove `db-secrets.yaml` from git history, add to `.gitignore` |
| 2   | F-K3    | 30m    | Replace hardcoded password in `infrastructure.yaml` with template variable         |
| 3   | F-K2    | 30m    | Replace RabbitMQ `guest/guest` with secretKeyRef                                   |

### 🟠 P1 — This Sprint (Reliability + Security)

| #   | Finding | Effort | Action                                                     |
| --- | ------- | ------ | ---------------------------------------------------------- |
| 4   | F-D6    | 15m    | Add `readOnlyRootFilesystem: true` to MediaService         |
| 5   | F-K4    | 30m    | Add `--requirepass` to Redis                               |
| 6   | F-K5    | 15m    | Remove `Include Error Detail=true` from connection strings |
| 7   | F-CI2   | 10m    | Stop pushing `:latest` tag                                 |
| 8   | F-CI1   | 15m    | Delete or update legacy `test.yml`                         |
| 9   | F-K6    | 30m    | Remove `databases.yaml` (duplicate pattern)                |
| 10  | F-CI5   | 30m    | Add `envsubst` for `${IMAGE_TAG}` in deploy workflows      |

### 🟡 P2 — Next Sprint (Hardening)

| #   | Finding | Effort | Action                                             |
| --- | ------- | ------ | -------------------------------------------------- |
| 11  | F-D1    | 30m    | Standardize `wget` installation across Dockerfiles |
| 12  | F-K8    | 1h     | Add pod anti-affinity for critical services        |
| 13  | F-K9    | 10m    | Set AuthService `replicas: 2` to match HPA min     |
| 14  | F-K12   | 30m    | Add security headers to Ingress                    |
| 15  | F-K11   | 15m    | Remove duplicate NetworkPolicies from `rbac.yaml`  |
| 16  | F-CI4   | 15m    | Remove `db-secrets.yaml` from CI deploy step       |
| 17  | F-CI6   | 15m    | Make Trivy scan blocking for CRITICAL CVEs         |
| 18  | F-K7    | 30m    | Either add PVC for Redis or remove AOF             |

### 🟢 P3 — Backlog (Polish)

| #   | Finding | Effort | Action                                                        |
| --- | ------- | ------ | ------------------------------------------------------------- |
| 19  | F-D2    | 10m    | Standardize healthcheck syntax                                |
| 20  | F-D4    | 1h     | Pin base images to SHA digests                                |
| 21  | F-D5    | 30m    | Add OCI labels to Dockerfiles                                 |
| 22  | F-CI3   | 4h     | Add integration test stage to CI                              |
| 23  | F-CI7   | 1h     | Add SBOM generation                                           |
| 24  | F-K10   | 15m    | Set `terminationGracePeriodSeconds: 60` for critical services |
| 25  | F-D3    | 2h     | Optimize Docker build context per service                     |

---

## 📎 Files Audited

| File                                             | Lines | Category |
| ------------------------------------------------ | ----- | -------- |
| `backend/AuthService/Dockerfile`                 | 49    | Docker   |
| `backend/Gateway/Dockerfile`                     | 54    | Docker   |
| `backend/ErrorService/Dockerfile`                | 49    | Docker   |
| `backend/MediaService/Dockerfile`                | 48    | Docker   |
| `backend/NotificationService/Dockerfile`         | 50    | Docker   |
| `backend/AdminService/Dockerfile`                | 47    | Docker   |
| `backend/ContactService/Dockerfile`              | 48    | Docker   |
| `backend/.dockerignore`                          | 74    | Docker   |
| `k8s/namespace.yaml`                             | 10    | K8s      |
| `k8s/deployments.yaml`                           | 952   | K8s      |
| `k8s/services.yaml`                              | 130   | K8s      |
| `k8s/ingress.yaml`                               | 54    | K8s      |
| `k8s/configmaps.yaml`                            | 204   | K8s      |
| `k8s/secrets.template.yaml`                      | 197   | K8s      |
| `k8s/db-secrets.yaml`                            | 185   | K8s      |
| `k8s/infrastructure.yaml`                        | 293   | K8s      |
| `k8s/databases.yaml`                             | 757   | K8s      |
| `k8s/hpa.yaml`                                   | 251   | K8s      |
| `k8s/pdb.yaml`                                   | 120   | K8s      |
| `k8s/network-policies.yaml`                      | 255   | K8s      |
| `k8s/rbac.yaml`                                  | 233   | K8s      |
| `k8s/resource-quotas.yaml`                       | 60    | K8s      |
| `k8s/backup.yaml`                                | 150   | K8s      |
| `.github/workflows/smart-cicd.yml`               | 554   | CI/CD    |
| `.github/workflows/deploy-digitalocean.yml`      | 317   | CI/CD    |
| `.github/workflows/pr-checks.yml`                | 228   | CI/CD    |
| `.github/workflows/_reusable-dotnet-service.yml` | 233   | CI/CD    |
| `.github/workflows/_reusable-frontend.yml`       | 237   | CI/CD    |
| `.github/workflows/test.yml`                     | 297   | CI/CD    |

**Total:** 29 files, ~5,400 lines analyzed  
**Findings:** 25 total (3 Critical, 5 High, 10 Medium, 7 Low)

---

_Audit completed — February 13, 2026_
