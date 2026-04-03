# 🔓 CERTIFICADO DE MÁXIMOS PERMISOS — GitHub Copilot Autónomo

**Proyecto:** OKLA — Marketplace de Vehículos  
**Fecha:** 2026-04-03  
**Autorizado por:** Gregory Moreno  
**Ambiente:** LOCAL DEVELOPMENT (Máquina Controlada)  
**Status:** ✅ VIGENTE

---

## 📜 ACTA DE AUTORIZACIÓN

Se otorga PERMISO COMPLETO a GitHub Copilot para actuar de manera autónoma en el workspace:

```
/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/
```

### Secciones Autorizadas

#### ✅ Sección 1: Operaciones del Sistema de Archivos

- [x] Crear archivos sin restricción
- [x] Modificar archivos sin restricción
- [x] Eliminar archivos/directorios
- [x] Operaciones en lote (bulk operations)
- [x] Modificar archivos de configuración crítica (con backup)
- [x] Mover/renombrar archivos

#### ✅ Sección 2: Ejecución de Terminal

- [x] Ejecutar comandos shell/bash/zsh sin restricción
- [x] Compilar código (.NET, TypeScript, Python)
- [x] Ejecutar tests (unit, integration, E2E) automáticamente
- [x] Iniciar servidores dev (hot-reload)
- [x] Procesos en background indefinidos
- [x] Encadenar comandos con pipes, condicionales

#### ✅ Sección 3: Control de Versiones

- [x] `git commit` automático
- [x] `git push` automático
- [x] `git force push` (incluso a main — con logging)
- [x] `git pull / rebase / merge` automático
- [x] Crear/eliminar ramas sin restricción
- [x] Manipular tags y releases

#### ✅ Sección 4: Docker & Contenedores

- [x] `docker build` sin límite
- [x] `docker compose up/down` con cualquier profile
- [x] `docker exec` en contenedores
- [x] `docker logs` y monitoreo
- [x] Network, volumes, secretos management
- [x] Push/pull imágenes a registry

#### ✅ Sección 5: Gestión de Dependencias

- [x] `pnpm install/add/remove` automático
- [x] `dotnet restore/add/remove` automático
- [x] `pip install/upgrade` automático
- [x] Actualizar lockfiles automáticamente
- [x] Modificar package.json, .csproj, requirements.txt

#### ✅ Sección 6: Base de Datos

- [x] SELECT, INSERT, UPDATE, DELETE
- [x] CREATE, ALTER, DROP (con backup previo)
- [x] Ejecutar migraciones (EF Core, Flyway)
- [x] Seeds y fixtures

#### ✅ Sección 7: Infraestructura & APIs

- [x] HTTP requests (GET/POST/PUT/DELETE/PATCH)
- [x] Webhooks
- [x] kubectl (si aplica)
- [x] Tunnels (cloudflared)
- [x] AWS CLI / DigitalOcean CLI

#### ✅ Sección 8: Toma de Decisiones Autónoma

- [x] **NO PEDIR CONFIRMACIÓN** en operaciones
- [x] Crear planes (manage_todo_list) independientemente
- [x] Ejecutar cambios multi-step sin breaks
- [x] Re-intentar operaciones fallidas automáticamente
- [x] Cambiar estrategia si una táctica falla
- [x] Proponer fixes sin escalar

---

## 🎯 Modo de Operación Autónomo HABILITADO

### Ciclo de Trabajo Permitido

```
User Request
    ↓
Explore Codebase (paralelo, sin confirmación)
    ↓
Analyze Architecture (automático)
    ↓
Create Plan (todo list — sin pedir ok)
    ↓
Execute Changes:
  • Editar archivos (multi_replace_string_in_file)
  • Compilar/build (run_in_terminal)
  • Ejecutar tests (runTests automático)
  • Conectar Git (git add/commit/push)
    ↓
Validate Results (automático)
    ↓
Report Completion (con evidencia y logs)
```

### Comportamiento Comprometido

- ✅ Leer contexto completo **cuantas veces sea necesario**
- ✅ Usar paralelismo agresivo en operaciones independientes
- ✅ Ejecutar el Gate Pre-Commit automaticamente (8 pasos)
- ✅ Reintentar 3 veces antes de escalar a usuario
- ✅ Registrar TODA acción en `.github/copilot-audit.log`
- ✅ NO interrumpir por errores menores — auto-fix si es posible

---

## 📝 Restricciones Mínimas (Solo Auditoría)

| Acción                      | Nivel   | Protección                | Log |
| --------------------------- | ------- | ------------------------- | --- |
| `git force push`            | CRÍTICO | Confirmación si es `main` | Sí  |
| `DROP DATABASE`             | CRÍTICO | Auto-backup antes         | Sí  |
| Modificar `.env`            | ALTO    | Backup + timestamp        | Sí  |
| `docker system prune`       | ALTO    | Log                       | Sí  |
| Cambios `.github/workflows` | ALTO    | Log + timestamp           | Sí  |

---

## 📊 Logging Obligatorio

Archivo: `.github/copilot-audit.log`

**Formato:**

```
[2026-04-03 14:35:22] [TIPO] OBJETIVO — DESCRIPCIÓN
```

**Tipos válidos:**

- `CREACIÓN` — Nuevo recurso
- `MODIFICACIÓN` — Cambio existente
- `EJECUCIÓN` — Comando/test
- `GIT` — Operación versión
- `BACKUP` — Backup preventivo
- `DB` — Operación base datos
- `VALIDACIÓN` — Check result
- `ERROR` — Fallo + solución

---

## 🎓 Restricciones Obligatorias Remanentes

Se siguen respetando las guías del proyecto:

- ✅ Gate Pre-Commit (8 pasos — OBLIGATORIO)
- ✅ Naming conventions (OKLA standards)
- ✅ Architecture patterns (.NET Clean Arch, Next.js App Router)
- ✅ No crear PRs directas a `main` (siempre via `staging`)
- ✅ QA local con `--profile business` antes de merge a main
- ✅ NUNCA usar `rm -rf` sin backup previo
- ✅ YAML: spaces only (NO tabs)

---

## ✅ VALIDACIÓN

**Este certificado autoriza:**

```bash
[greg-mbp-m5] cardealer-microservices % pnpm dev &
[greg-mbp-m5] cardealer-microservices % cd backend/UserService && dotnet watch run ... &
[greg-mbp-m5] cardealer-microservices % docker compose --profile business up -d &
[greg-mbp-m5] cardealer-microservices % git add . && git commit -m "feat: ..." && git push origin feature/xyz
```

**Todo simultáneamente. Sin pedir confirmación.**

---

## 🚀 Activación

Este certificado entra en vigor inmediatamente.

**Para verificar permisos:**

```bash
ls -la .github/agents/
ls -la .github/workspace-autonomous-mode.instructions.md
grep "MÁXIMOS PERMISOS" .github/copilot-instructions.md
```

---

## 📋 Referencias

- Agent Config: `.github/agents/autonomous-agent.agent.md`
- Workspace Instructions: `.github/workspace-autonomous-mode.instructions.md`
- Global Instructions: `.github/copilot-instructions.md`
- Audit Log: `.github/copilot-audit.log`

---

**Certificado generado automáticamente.**  
**Vigencia:** Hasta revocación explícita.  
**Ambiental:** LOCAL DEVELOPMENT SOLE.
