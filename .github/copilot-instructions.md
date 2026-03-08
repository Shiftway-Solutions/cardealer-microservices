# 🤖 GitHub Copilot Instructions - OKLA Project (v2)

This file provides concise context for GitHub Copilot (Claude) when assisting with the OKLA codebase. Use it to understand the project’s architecture, coding standards, and critical rules.

**Last updated:** 2026-02-22

---

## 1. Project Overview

**OKLA** is a vehicle marketplace in Dominican Republic, built with microservices (.NET 8) and a Next.js 16 frontend, deployed on Digital Ocean Kubernetes (DOKS).

- **Business model**: Freemium v3 — Free buyers, dealers on 4 tiers: Libre ($0), Visible ($29/mo), Pro ($89/mo), Elite ($199/mo).
- **Architecture**: Clean Architecture per service, CQRS with MediatR, Domain Events, RabbitMQ, PostgreSQL, Redis.
- **Frontend**: Next.js 16 (App Router), TypeScript, Tailwind, shadcn/ui, Zustand, TanStack Query, pnpm (⚠️ **NO npm/yarn**).
- **Backend**: .NET 8, Ocelot Gateway, shared libraries (`CarDealer.Shared`, `CarDealer.Contracts`).
- **K8s**: DOKS, all services use port **8080** internally, BFF pattern (`/api/*` rewritten to gateway).

---

## 2. Essential Coding Rules

### 2.1 Naming Conventions

- **C#**: Classes/interfaces: `PascalCase`, methods: `PascalCase`, params/locals: `camelCase`. Private fields: `_camelCase`. Interfaces: `I` prefix.
- **TypeScript**: Components: `PascalCase`, functions/variables: `camelCase`, interfaces: `PascalCase` (no `I` prefix).
- **Files**: Match the main export name (e.g., `VehicleCard.tsx`).
- **Folders**: `kebab-case` (e.g., `auth-service`).

### 2.2 Project Structure

```
backend/
├── ServiceName/
│   ├── ServiceName.Api/          # Controllers, Program.cs
│   ├── ServiceName.Application/   # Commands/Queries, DTOs, Validators
│   ├── ServiceName.Domain/        # Entities, Enums, Events, Interfaces
│   └── ServiceName.Infrastructure/# Persistence, Repos, External Services
frontend/
├── web-next/
│   ├── src/app/                   # App Router (groups: (auth), (main), api)
│   ├── src/components/             # Reusable components (grouped by feature)
│   ├── src/services/               # API clients
│   └── src/lib/                    # Utilities, security, config
k8s/                                 # Manifests (deployments, services, ingress)
docs/                                # Detailed documentation
```

### 2.3 Critical Prohibitions

- ❌ **Never** use `npm` or `yarn` in frontend – use `pnpm`.
- ❌ **Never** expose Gateway to internet – only internal.
- ❌ **Never** use `CreateBootstrapLogger()` if you call `UseStandardSerilog()`.
- ❌ **Never** change RabbitMQ queue arguments without deleting the old queue first.
- ❌ **Never** let `/health` endpoint run external checks (exclude `"external"` tag).
- ❌ **Never** store secrets in code – use K8s secrets or environment variables.

---

## 3. Backend Development Standards

### 3.1 Clean Architecture Layers

Each service must follow the 4-layer structure above. **Shared libraries** in `_Shared/` provide common functionality.

### 3.2 Mandatory Extensions (in Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddMicroserviceSecrets();      // from K8s secrets
builder.UseStandardSerilog();                         // no CreateBootstrapLogger
builder.Services.AddStandardDatabase<TDbContext>(builder.Configuration);
builder.Services.AddStandardRabbitMq(builder.Configuration);
builder.Services.AddPostgreSqlDeadLetterQueue(builder.Configuration);
builder.Services.AddStandardObservability(builder.Configuration, "ServiceName");
builder.Services.AddGlobalErrorHandling(builder.Configuration);
builder.Services.AddAuditMiddleware();
builder.Services.AddMediatR(cfg => ...);
builder.Services.AddValidatorsFromAssembly(...);
```

### 3.3 Health Checks

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("external")   // ⚠️ CRITICAL
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
```

### 3.4 Dependency Injection

- **Always** register all dependencies, especially for `HostedService`. A common missing registration is `IDeadLetterQueue`:
  ```csharp
  builder.Services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();
  builder.Services.AddHostedService<DeadLetterQueueProcessor>();
  ```
- **Test DI startup** with `WebApplicationFactory<Program>`.

### 3.5 CQRS with MediatR

- Commands/Queries go in `Application/Features/`.
- Handlers should be simple: validate (auto via `ValidationBehavior`), call domain, persist, publish events.
- **Do not** call validators manually – `ValidationBehavior` runs them automatically.

### 3.6 Result Pattern & Error Handling

- Use `Result<T>` from `CarDealer.Shared` for operation outcomes.
- Global middleware converts exceptions to **RFC 7807 ProblemDetails**.
- Controllers return `ApiResponse<T>` (from shared lib) for successful responses.

### 3.7 Events

- All domain events inherit `EventBase` (in `CarDealer.Contracts`).
- Event type format: `"{domain}.{entity}.{action}"` (e.g., `auth.user.registered`).
- Publish via RabbitMQ.

---

## 4. Frontend Development Standards (Next.js)

### 4.1 Stack

- **Framework**: Next.js 16 App Router.
- **State**: Zustand (client) + TanStack Query (server state).
- **Forms**: `react-hook-form` + `zod` + `@hookform/resolvers`.
- **UI**: shadcn/ui components (do **not** create custom basic components).
- **Styling**: Tailwind CSS v4.
- **Testing**: Vitest (⚠️ **not Jest**) + Testing Library + Playwright (E2E).
- **Package manager**: pnpm (⚠️ **not npm/yarn**).

### 4.2 Folder Conventions

```
src/app/
  (auth)/         # login, registro, etc. (no navbar)
  (main)/         # authenticated pages with navbar
  api/            # route handlers (BFF)
src/components/
  ui/             # shadcn/ui components
  kyc/            # KYC-specific components
  vehicles/       # vehicle-related components
src/services/     # API clients (e.g., kycService, authService)
src/hooks/        # custom hooks
src/lib/          # utilities (api-client, security, etc.)
```

### 4.3 Data Fetching

- Use **TanStack Query** for all server-state.
- Use **Zustand** for client-only state (UI, auth status, etc.).
- API calls go through service modules (e.g., `kycService.createProfile`).

### 4.4 Forms

```typescript
const schema = z.object({ email: z.string().email() });
const form = useForm({ resolver: zodResolver(schema) });
<form onSubmit={form.handleSubmit(onSubmit)}>...</form>
```

### 4.5 API Response Handling

Both `ApiResponse<T>` and `ProblemDetails` may be returned. Use a wrapper:

```typescript
async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const error = await res.json();
    if (error.type) throw new ApiError(error.title, error.status, error.errors);
    if (error.success === false) throw new ApiError(error.error, res.status);
    throw new Error("Unknown error");
  }
  const data = await res.json();
  return data.data ?? data; // unwrap ApiResponse
}
```

### 4.6 Security in Frontend

- **CSRF**: Use `csrfFetch()` or `<CsrfInput>` from `@/lib/security/csrf` for all state-changing requests.
- **XSS prevention**: Always use `escapeHtml()` or `sanitizeText()` from `@/lib/security/sanitize` when rendering user input.
- **URL sanitization**: Use `sanitizeUrl()` before using in `href` or `src`.

---

## 5. Critical Infrastructure Rules

### 5.1 Kubernetes (DOKS)

- All services listen on port **8080** (NO 80). Ocelot downstream ports must be 8080.
- BFF pattern: frontend rewrites `/api/*` to internal gateway service. No external Ingress for gateway.
- Secrets: `registry-credentials` must use a long-lived PAT (not `ghs_*`). Refresh if `ImagePullBackOff`.
- ConfigMap for Gateway: update with `ocelot.prod.json` and restart deployment.

### 5.2 CI/CD (GitHub Actions)

Note: GitHub Actions builds and publishes the production Docker images used in deployments. Do NOT build and push production images locally from macOS for the cluster deployment; use the repository CI instead.

- CI-built images are authoritative: only images produced by GitHub Actions should be deployed to the cluster. Local builds from macOS can produce platform-incompatible images (arm64 vs amd64) and cause ImagePull or runtime failures.
- Image names **must** match exactly between CI/CD and `k8s/deployments.yaml` (e.g., `ghcr.io/gregorymorenoiem/frontend-web:latest`). Prefer immutable tags (for example, use commit SHA tags) for production releases.
- How to trigger a build:
  - Push commits to `main` (or the branch configured to build/pack images).
  - Use the GitHub Actions `workflow_dispatch` UI to manually trigger the build workflow when needed.
  - If you need to force a rebuild, create an empty commit: `git commit --allow-empty -m "rebuild: trigger CI image build" && git push`.
- How to verify the build and published image:
  - Open the repository Actions page and inspect the latest workflow run for the build-and-push job. Ensure the job succeeded and the image digest was printed.
  - Confirm the image exists in GitHub Container Registry (GHCR) or your configured registry.
  - After the image is published, restart the deployment (if your deployment does not auto-roll) and check pod events.
- Quick verification commands (CI and deployment):

```bash
# check actions run (open in browser): https://github.com/<owner>/<repo>/actions
# restart deployment to pull latest image
kubectl rollout restart deployment/kycservice -n okla
kubectl rollout status deployment/kycservice -n okla
kubectl get pods -n okla -l app=kycservice -o wide
kubectl logs -n okla -l app=kycservice --follow
```

- If a build doesn't reflect code changes, clear Docker buildx cache used by CI (example):

```bash
gh cache list --key "Linux-buildx-{service}" | awk '{print $1}' | xargs -I{} gh cache delete {}
```

- Local development guidance (if you must build locally):
  - Use an ephemeral Linux runner or an M1-aware workflow: prefer testing in a Linux CI runner before pushing to GHCR.
  - When building locally, use `docker buildx build --platform linux/amd64` and verify the image in a Linux environment before pushing.

- Recommended enforcement (process changes):
  - Protect `main` and require passing CI checks before merge.
  - Use immutable image tags (commit SHA) in deployment manifests instead of `:latest` for production.
  - Add the build status badge to the README for quick visibility.

### 5.3 RabbitMQ

- Queue arguments are **immutable**. If you change DLX, TTL, etc., delete the queue first:
  ```bash
  kubectl exec deployment/rabbitmq -n okla -- rabbitmqctl delete_queue {queue-name}
  ```
- Dead Letter Queues: Use `ISharedDeadLetterQueue` from shared lib, but register `IDeadLetterQueue` locally for processor.

### 5.4 Databases

- Two PostgreSQL instances: DO Managed (production) and in-cluster (staging). Connection strings via secrets.
- Database-per-service pattern. Use `AddStandardDatabase<T>` which handles migrations if `EnableAutoMigration: true`.

---

## 6. Security Validators (Backend)

**All string inputs must be validated against SQL injection and XSS.**

Copy `SecurityValidators.cs` from AuthService to new services and apply:

```csharp
RuleFor(x => x.Email).NotEmpty().EmailAddress().NoSqlInjection().NoXss();
RuleFor(x => x.Password)
    .NotEmpty().MinimumLength(8).MaximumLength(128)
    .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]")
    .NoSqlInjection().NoXss();  // Password can contain special chars, but still validate patterns
```

---

## 7. Execution of Long-Running Commands

To avoid terminal disconnections when running commands that take a long time (e.g., database migrations, large builds, deployment scripts), **do not paste the command directly into the terminal**. Instead:

1. **Create a temporary shell script** with the command(s), execute it, and then remove it.
2. **Use `nohup`** or **`screen`/`tmux`** if the command must survive a session disconnect.

Example for creating a temporary script:

```bash
# Create a temporary script
cat > /tmp/okla-cmd-$$.sh << 'EOF'
#!/bin/bash
set -e
# your long-running commands here
kubectl rollout restart deployment/vehiclessaleservice -n okla
pnpm run build
EOF

# Make it executable
chmod +x /tmp/okla-cmd-$$.sh

# Run it (optionally with nohup)
nohup /tmp/okla-cmd-$$.sh > /tmp/okla-cmd-$$.log 2>&1 &

# Optionally remove after completion (if running in foreground, remove at end)
# rm /tmp/okla-cmd-$$.sh
```

If you need to run the command in the foreground and ensure it doesn't get interrupted, use `screen`:

```bash
screen -S okla-cmd
# run your command
# Ctrl+A D to detach, screen -r okla-cmd to reattach
```

Always redirect output to a log file so you can check progress later.

**Important**: When suggesting commands to the user, prefer the temporary script approach over a single long command.

---

## 8. Troubleshooting Quick Reference

| Symptom                       | Likely Cause                                   | Solution                                                                       |
| ----------------------------- | ---------------------------------------------- | ------------------------------------------------------------------------------ |
| 404 in Gateway                | Route missing or incorrect port                | Check `ocelot.*.json`; ensure downstream host:port = servicename:8080          |
| 503 Service Unavailable       | Downstream service not ready or wrong port     | Verify pod is running and port is 8080                                         |
| ImagePullBackOff              | Registry secret expired or image name mismatch | Refresh secret; check deployment image name matches CI/CD                      |
| CrashLoopBackOff (DI failure) | Missing registration for injected service      | Check `Program.cs`; add `builder.Services.AddSingleton<IDeadLetterQueue, ...>` |
| CrashLoopBackOff (RabbitMQ)   | Queue arg mismatch                             | Delete queue manually before deploying code with new args                      |
| Health check timeout          | External checks included in `/health`          | Exclude checks with tag `"external"` from `/health` endpoint                   |
| Frontend shows old version    | Wrong image name in deployment or build cache  | Verify deployment uses `frontend-web:latest`; clear Docker build cache         |

---

## 9. References to Detailed Documentation

For deep dives, consult these files (accessible via `#file:docs/...`):

- `docs/ARCHITECTURE.md` – Full microservice list, patterns, event catalog.
- `docs/KUBERNETES.md` – K8s commands, ingress, secrets management.
- `docs/SECURITY.md` – Complete security validators, CSRF, JWT, rate limiting.
- `docs/CHATBOT_ARCHITECTURE.md` – LLM integration, RAG, Spanish slang.
- `docs/TROUBLESHOOTING.md` – Extended incident database.
- `prompts/` – Pre-made Copilot prompt templates (use with `#file:`).

---

## 10. Example Prompts & Expected Responses

**Prompt:**

> "Create a new command `ApproveKycCommand` in KYCService with validator and handler."

**Expected response:**

- Generate the command record, handler, and validator following CQRS pattern.
- Use `Result<T>` return type.
- Include `NoSqlInjection()` and `NoXss()` on string fields.
- Reference `AuditServiceClient` to log the action.
- Place files in correct folders (`Application/Features/Kyc/Commands`).
- Add the necessary DI registrations if any new service is used.

**Prompt:**

> "I need a React component to display a vehicle card with image, title, price, and a favorite button. Use shadcn/ui."

**Expected response:**

- Import Card, Button, etc., from `@/components/ui`.
- Use Next.js `Image` for optimization.
- Use Zustand for favorite state (client-side) or TanStack Query if synced with backend.
- Include TypeScript interface for props.
- Ensure responsive design.

**Prompt:**

> "How do I restart all services in the cluster after a config change?"

**Expected response:**

- Provide a command, but suggest using a temporary script if there are many services to avoid terminal timeout.
- Example: create a script that loops through deployments and restarts them, with logging.

---

## 11. Final Checklist Before Answering

- [ ] Does the code follow naming conventions and folder structure?
- [ ] Are all strings validated for SQLi/XSS (backend) or sanitized (frontend)?
- [ ] Are all dependencies registered correctly (backend DI)?
- [ ] Is the correct package manager used (pnpm)?
- [ ] Does the code handle both `ApiResponse` and `ProblemDetails` formats (frontend)?
- [ ] Are all state-changing requests protected with CSRF (frontend)?
- [ ] If a new service is added, are Health Checks correctly configured?
- [ ] If RabbitMQ queues are involved, are arguments immutable? If changed, queue deletion needed?
- [ ] If modifying an existing service, is there a test for DI startup?
- [ ] For long-running commands, is the user advised to use a temporary script or `screen` to avoid disconnection?
