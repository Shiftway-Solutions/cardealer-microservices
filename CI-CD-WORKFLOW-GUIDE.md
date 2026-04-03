# 🚀 CI/CD Workflow Guide — OKLA

## Flujo de Ramas

El workflow `smart-cicd.yml` está configurado para 3 tipos de ramas con comportamientos distintos:

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 1️⃣ **`feature/*` — Desarrollo Local**

**Trigger:** `git push origin feature/nombre-del-feature`

### Qué sucede:

- ✅ Build backend (.NET)
- ✅ Build/test frontend (Next.js)
- ✅ Unit tests
- ❌ **NO** Docker push
- ❌ **NO** Deploy

### Use case:

- Iteración local con hot-reload
- PR a `staging` desde feature branch
- Testing local con http://okla.local

### Comando recomendado:

```bash
# 1. Crear feature desde staging
git checkout staging && git pull origin staging
git checkout -b feature/mi-feature

# 2. Iterar localmente
docker compose up -d
cd backend/AuthService
dotnet watch run --project AuthService.Api/AuthService.Api.csproj

# 3. Commit + push
git push origin feature/mi-feature
# → Workflow corre: Build + Test (sin Docker push)

# 4. Abrir PR: feature/mi-feature → staging
```

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 2️⃣ **`staging` — QA Local Testing**

**Trigger:** `git push origin staging` o PR merge a staging

### Qué sucede:

- ✅ Build backend
- ✅ Build/test frontend
- ✅ Unit tests
- ✅ **Docker push** a GHCR (ghcr.io)
- ✅ Integration tests (local Docker)
- ❌ **NO** Deploy a DigitalOcean

### Use case:

- Testing local con full stack Docker Compose
- QA team verifica cambios antes de producción
- Staging branch es el "gate" para `main`

### Comando recomendado:

```bash
# 1. Verificar que feature está OK
# (ya hizo PR a staging y se mergeó)

# 2. QA local con stack completo
docker compose --profile business up -d
# → Deploy local: Postgres + Redis + RabbitMQ + todos los servicios

# 3. Ejecutar smoke tests / Playwright E2E
pnpm exec playwright test

# 4. Si todo OK → PR: staging → main
# (NUNCA hagas PR directamente feature → main)
```

**Docker images in GHCR (staging):**

```
ghcr.io/shiftway-solutions/authservice:latest
ghcr.io/shiftway-solutions/gateway:latest
... (todos los servicios)
```

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 3️⃣ **`main` — Producción (DigitalOcean)**

**Trigger:** `git push origin main` o PR merge a main

### Qué sucede:

- ✅ Build backend
- ✅ Build/test frontend
- ✅ Unit tests
- ✅ **Docker push** a GHCR
- ✅ Integration tests
- 🚀 **Deploy a DigitalOcean DOKS** (Kubernetes)

### Use case:

- **PRODUCCIÓN EN VIVO**
- Cambios llegan automáticamente a https://okla.com.do
- No esperes a este branch si estás en desarrollo local

### Comando recomendado:

```bash
# 1. Solo mergear a main si:
#    - Feature pasó PR review
#    - QA local verificó en staging
#    - Todos los tests pasaron

# 2. Merge a main automáticamente dispara:
git merge origin/staging  # (ya tenía los cambios QA-aprobados)

# 3. Workflow automáticamente:
#    - Build + Test
#    - Push Docker a GHCR
#    - Deploy a DOKS
#    - Restart pods necesarios

# 4. Verificar deployment
kubectl get pods -n okla
```

**Docker images in GHCR (production):**

```
ghcr.io/shiftway-solutions/authservice:a2d2d282  (git SHA)
ghcr.io/shiftway-solutions/authservice:latest
```

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 🔄 Flujo Completo Recomendado

```
┌─────────────────────────────────────────────────┐
│ 1. feature/auth-refactor                        │
│    └─ Local dev + hot-reload                    │
│    └─ CI: Build + Test                          │
│    └─ PR → staging                              │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│ 2. staging (branch)                             │
│    └─ Merge feature/auth-refactor               │
│    └─ CI: Build + Test + Docker push            │
│    └─ Manual QA: docker compose --profile biz   │
│    └─ Smoke tests / E2E                         │
│    └─ PR → main                                 │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│ 3. main (PRODUCCIÓN)                            │
│    └─ Merge staging                             │
│    └─ CI: Build + Test + Docker push            │
│    └─ ✅ DEPLOY a DigitalOcean DOKS             │
│    └─ 🔄 Kubernetes restarts pods modified      │
│    └─ 🌐 Live on https://okla.com.do            │
└─────────────────────────────────────────────────┘
```

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## ⚠️ Reglas Críticas

| Regla                                                     | Razón                                 |
| --------------------------------------------------------- | ------------------------------------- |
| **Nunca pushear directamente a `main`**                   | Solo via PR desde `staging`           |
| **Hacer QA local en `staging` ANTES de mergear a `main`** | `main` es sagrado, no es para testing |
| **Feature branches usan `docker compose up -d` local**    | Sin Docker push a GHCR                |
| **Staging branches SÍ pushean Docker a GHCR**             | Pero NO despliegan a Kubernetes       |
| **Solo `main` despliega a DigitalOcean**                  | Producción automática                 |

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 🔍 Verificar Status del Workflow

### Ver último run:

```bash
gh run list --workflow=smart-cicd.yml --limit=1
```

### Ver detalles de un run específico:

```bash
gh run view <RUN_ID> --log
```

### Ver qué Branch disparó el workflow:

```bash
gh run list --workflow=smart-cicd.yml --json status,headBranch -L5
```

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 📊 Resumen de Comportamiento

| Acción                      | Branch     | Build | Test | Docker Push | Deploy DOKS |
| --------------------------- | ---------- | ----- | ---- | ----------- | ----------- |
| `git push origin feature/*` | feature/\* | ✅    | ✅   | ❌          | ❌          |
| Merge to staging            | staging    | ✅    | ✅   | ✅          | ❌          |
| Merge to main               | main       | ✅    | ✅   | ✅          | ✅          |

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

## 💡 Pro Tips

1. **Local dev speed**: Usa `dotnet watch run`, NO rebuildar Docker
2. **Staging is QA**: Levanta `docker compose --profile business` antes de mergear a main
3. **Main is prod**: Asume que cualquier merge a main se despliega en 2-3 minutos
4. **Accidental deploys**: Si commiteas a main por error, los cambios ya están en DOKS
5. **Rollback**: `git revert` + push a main → nuevo deploy automático

―――――――――――――――――――――――――――――――――――――――――――――――――――――――

Última actualización: 2026-04-03
Workflow file: [.github/workflows/smart-cicd.yml](.github/workflows/smart-cicd.yml)
