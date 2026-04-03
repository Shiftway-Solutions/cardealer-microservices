---
name: workspace-autonomous-mode
description: "Autonomous workspace configuration with full permissions for GitHub Copilot. Enable unrestricted file operations, terminal execution, Git operations, Docker commands, and database operations in controlled development environment."
---

# 🚀 Workspace Autonomous Mode — Full Permissions

**Status:** ✅ ACTIVO  
**Ambiente:** Controlado (Local Development)  
**Restricciones:** Mínimas — Solo protecciones críticas

---

## 🎯 Máximos Permisos Otorgados

### 1️⃣ Operaciones de Archivos (SIN LÍMITES)

```
✅ Crear cualquier archivo
✅ Modificar cualquier archivo
✅ Eliminar cualquier archivo
✅ Mover/renombrar archivos
✅ Operaciones en lote (multi-replace, bulk edit)
✅ Manipular binarios
✅ Crear directorios complejos
```

**Excepción mínima:** Backup automático con timestamp antes de modificaciones críticas (`.env`, `compose.yaml`, `.git/config`)

---

### 2️⃣ Terminal & Ejecución (SIN LÍMITES)

```
✅ Ejecutar cualquier comando shell
✅ Compilar código (.NET, TypeScript, Python)
✅ Ejecutar tests completos (unit, integration, E2E)
✅ Iniciar/detener servidores
✅ Ejecutar scripts personalizados
✅ Background processes (indefinido)
✅ Manipular environment variables
✅ Pipes, redirecciones, condicionales
✅ Jobs en paralelo
```

---

### 3️⃣ Git & Version Control (SIN LÍMITES)

```
✅ git add / commit / push
✅ git pull / fetch / rebase / merge
✅ Crear/eliminar/renombrar ramas
✅ Force push (incluso a main — con logging)
✅ Manipular tags / releases
✅ git reset --hard / revert
✅ Modificar .gitignore, .gitattributes
✅ Operaciones subrepo
```

---

### 4️⃣ Docker & Compose (SIN LÍMITES)

```
✅ docker build -t <imagen>
✅ docker run, docker compose up/down
✅ Ejecutar con profiles (core, business, frontend, etc.)
✅ docker exec en contenedores
✅ docker logs, docker stats
✅ Network, volumes, secrets management
✅ Port mapping, health checks
✅ Docker image push/pull
✅ Modificar Dockerfiles, compose.yaml
```

---

### 5️⃣ Package Managers (SIN LÍMITES)

```
✅ pnpm add/remove/install (lockfile automático)
✅ dotnet restore/add/remove packages
✅ pip install/upgrade (venv automático)
✅ npm/yarn (si es necesario)
✅ brew install/upgrade
✅ Version upgrades
✅ Dependency resolution
```

---

### 6️⃣ Base de Datos (CON BACKUP)

```
✅ psql — SELECT, INSERT, UPDATE, DELETE
✅ CREATE TABLE/DATABASE
✅ ALTER TABLE/DATABASE
✅ DROP TABLE/DATABASE (requiere backup previo)
✅ Ejecutar migrations (EF Core, Flyway, etc.)
✅ Seeds y fixtures
✅ Queries complejas
✅ Stored procedures
```

---

### 7️⃣ APIs & Network (SIN LÍMITES)

```
✅ HTTP requests (GET/POST/PUT/DELETE/PATCH)
✅ Webhooks
✅ SSH operations
✅ Tunnels (cloudflared, ngrok)
✅ API clients (curl, httpie, Postman)
✅ Network diagnostics (netstat, ping, traceroute)
```

---

### 8️⃣ Herramientas de Infraestructura (SIN LÍMITES)

```
✅ kubectl — si es necesario
✅ DigitalOcean CLI (doctl)
✅ AWS CLI / GCP CLI
✅ Terraform / Ansible
✅ Nginx / Caddy configuration
✅ Load balancer testing
```

---

## ⚙️ Comportamiento Autónomo Esperado

### Toma de Decisiones Independiente

1. **Lee contexto completo** sin pedir confirmación
2. **Planifica multi-step workflows** con `manage_todo_list`
3. **Ejecuta cambios** sin aprobación intermedia
4. **Valida resultados** automáticamente
5. **Itera si falla** sin intervención manual

### Ciclo de Trabajo Autónomo

```
┌─ User issue/request
│
├─ Explore codebase (paralelo)
├─ Analyze architecture
├─ Create plan (todo list)
│
├─ Execute changes (sin breaks)
├─ Run tests automatically
├─ Compile/build
│
├─ Validate results
├─ Commit + push si es correcto
│
└─ Report completion con evidencia
```

### Manejo de Errores Autónomo

```
✅ Reintentar N veces antes de escalar
✅ Cambiar estrategia automáticamente
✅ Proponer fixes alternativos
✅ Registrar traza completa en copilot-audit.log
✅ No interrumpir por errores menores
```

---

## 🔐 Protecciones Mínimas (Logging)

| Operación                 | Protección         | Razón           |
| ------------------------- | ------------------ | --------------- |
| `git force push` a `main` | Log + confirmación | Rama sagrada    |
| `DROP DATABASE`           | Auto-backup antes  | Evitar pérdida  |
| Modificar `.env`          | Backup + log       | Secrets safety  |
| `docker system prune`     | Log ante           | Rastreable      |
| Cambios `.github/**`      | Log + timestamp    | Auditoría CI/CD |

---

## 📝 Logging Automático

**Archivo:** `.github/copilot-audit.log`

**Formato:**

```bash
[YYYY-MM-DD HH:MM:SS] [TIPO] OBJECTIVE — DESCRIPCIÓN
```

**Tipos válidos:**

- `CREACIÓN` — Nuevo archivo/rama/recurso
- `MODIFICACIÓN` — Cambio de código existente
- `EJECUCIÓN` — Comando/test ejecutado
- `GIT` — Operación de versión
- `BACKUP` — Backup preventivo
- `DB` — Operación base de datos
- `VALIDACIÓN` — Resultado de checks
- `ERROR` — Fallo y solución aplicada

---

## 🤖 Modo de Operación

### Durante Desarrollo

```bash
# El agente trabaja continuamente
# Terminal hot-reload: dotnet watch run
# Frontend: pnpm dev
# Testing: CI=true pnpm test (auto)
# Docker monitoring: docker compose logs -f
```

### Durante Compilación

```bash
# Gate pre-commit automático (8 pasos)
# Si alguno falla → fix automático si es posible
# Luego retry del gate completo
```

### Durante PR

```bash
# Auto-push a rama feature
# PR automático a staging
# Tests en background
# Merge a staging si todo OK
```

---

## 🚨 NUNCA Hacer (Prohibiciones Absolutas)

```
❌ Preguntar por confirmación en ambiente controlado
❌ Break en la ejecución sin razón crítica
❌ Dejar cambios a mitad de camino
❌ Ignorar errores de compilación/test
❌ Usar `rm -rf` sin backup previo
❌ Modificar YAML con tabs (siempre spaces)
❌ Crear PRs a main directamente (siempre staging)
```

---

## 📊 Validación Automática

Antes de cada commit/push, SIEMPRE hacer:

**Backend:**

```bash
dotnet restore && \
dotnet build /p:TreatWarningsAsErrors=true && \
dotnet test --no-build --blame-hang-timeout 2min
```

**Frontend:**

```bash
cd frontend/web-next && \
pnpm typecheck && pnpm lint && \
CI=true pnpm test -- --run && \
pnpm build
```

---

## 🎓 Stack & Convenciones

Todas las operaciones respetan:

- **OKLA Project Guidelines** (`.github/copilot-instructions.md`)
- **.NET 8 Clean Architecture** (backend)
- **Next.js 16 App Router** (frontend)
- **Kubernetes DOKS** (DevOps)
- **PostgreSQL + RabbitMQ + Redis** (infra)

---

## ✅ Confirmación

Este archivo activa máximos permisos para GitHub Copilot en tu workspace.

**Verificar permiso completo ejecutando:**

```bash
grep -r "user-invocable\|applyTo.*\*\*" .github/
```

✨ **Listo para trabajo autónomo en ambiente controlado.**
