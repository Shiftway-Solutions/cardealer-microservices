# GitHub Copilot — Instrucciones Globales del Agente

> **Ambiente controlado · Auditoría manual al final del día**
> Este archivo es leído por el agente en cada sesión. Sigue estas reglas sin excepción.

---

## 🚦 GATE OBLIGATORIO PRE-COMMIT Y PRE-PUSH

NUNCA ejecutes `git commit` ni `git push` sin antes haber pasado los **7 pasos** de este gate.
Si cualquier paso falla, detente, corrige el problema y vuelve a correr el gate completo desde el paso 1.
No hay excepciones. Este proceso garantiza que el CI/CD no se rompa.

> ⚠️ **EL CI/CD TIENE DOS PIPELINES QUE DEBEN PASAR:**
>
> - **Pipeline Backend (.NET):** Pasos 1, 2, 4
> - **Pipeline Frontend (Next.js/React):** Pasos 3a, 3b, 3c, 3d ← **El paso 3 tiene 4 sub-pasos que reproducen el CI/CD frontend exacto**

---

### Paso 1 — `dotnet restore` · Detecta paquetes rotos o referencias faltantes

```bash
dotnet restore
```

**Criterio de éxito:** salida sin errores `NU****` codes.

Si falla:

- Revisa que todos los proyectos referenciados en la `.sln` existan en disco.
- Verifica que el `NuGet.Config` apunte a los feeds correctos.
- Elimina la carpeta `obj/` del proyecto afectado y repite: `dotnet restore <proyecto>.csproj`
- Registra en auditoría y no avances hasta resolver.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-1] dotnet restore — OK" >> .github/copilot-audit.log
```

---

### Paso 2 — `dotnet build /p:TreatWarningsAsErrors=true` · Detecta errores y warnings de C#

```bash
dotnet build /p:TreatWarningsAsErrors=true
```

**Criterio de éxito:** `Build succeeded` con `0 errores` y `0 warnings`.

| Código | Significado                                 | Acción                                    |
| ------ | ------------------------------------------- | ----------------------------------------- |
| CS8601 | Posible asignación de referencia nula       | Agrega null check o usa `?.` / `!`        |
| CS8602 | Dereference de referencia posiblemente nula | Valida con `if (x != null)` o usa `??`    |
| CS8618 | Propiedad no-nullable sin inicializar       | Inicializa en constructor o usa `= null!` |
| CS0108 | Miembro oculta miembro heredado sin new     | Agrega keyword `new` o renombra           |
| CS0162 | Código inalcanzable                         | Elimina o comenta el bloque               |
| CS1998 | Método async sin await                      | Agrega `await` o elimina `async`          |
| CS8625 | Literal null no puede convertirse a tipo    | Cambia el tipo a `T?` o elimina el null   |

Nunca uses `/p:TreatWarningsAsErrors=false` ni `#pragma warning disable` para forzar el paso.
Si el warning es de una dependencia externa, usa `<NoWarn>CSXXXX</NoWarn>` en el `.csproj` con comentario XML justificando.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-2] dotnet build TreatWarningsAsErrors — OK" >> .github/copilot-audit.log
```

---

### Paso 3a — `pnpm lint` · Detecta errores de ESLint en el frontend React/Next.js

```bash
cd frontend/web-next && pnpm lint
```

**Criterio de éxito:** `0 errors` (warnings son aceptables si tienen justificación con comentario).

| Regla ESLint                         | Causa común                          | Acción                                         |
| ------------------------------------ | ------------------------------------ | ---------------------------------------------- |
| `no-unused-vars`                     | Variable declarada pero no usada     | Elimínala o prefixa con `_`                    |
| `react-hooks/exhaustive-deps`        | Dependencia faltante en `useEffect`  | Agrega la dependencia al array                 |
| `@next/next/no-img-element`          | `<img>` nativo en lugar de `<Image>` | Reemplaza con `import Image from 'next/image'` |
| `@typescript-eslint/no-explicit-any` | Tipo `any` explícito                 | Define un tipo o interfaz específica           |
| `react/no-unescaped-entities`        | Comillas sin escapar en JSX          | Usa `&apos;` `&quot;`                          |

Nunca deshabilites reglas con `eslint-disable` para forzar el paso.
Si es un false positive justificado, usa `// eslint-disable-next-line <regla> -- motivo` con motivo explícito.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3a] pnpm lint — OK" >> .github/copilot-audit.log
```

---

### Paso 3b — `pnpm typecheck` · TypeScript type check (tsc --noEmit) ← **PASO CRÍTICO NUEVO**

```bash
cd frontend/web-next && pnpm typecheck
```

Este paso reproduce **EXACTAMENTE** el job `🔍 Lint & Type Check` del CI/CD (`_reusable-frontend.yml`).
**Criterio de éxito:** comando termina sin output de error (exit code 0, sin líneas `error TS****`).

> ⚠️ **REGLAS CRÍTICAS DE TYPESCRIPT — Causas reales de fallos en CI/CD:**

| Error TS                                                   | Causa                                                                                                                                            | Acción                                                                                                                               |
| ---------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| **TS2393** Duplicate function implementation               | Scripts `.ts` sin `import`/`export` son globales — si dos archivos declaran `async function main()`, TypeScript ve duplicado en el scope global. | **Agrega `export {};` al inicio del archivo** para hacerlo un módulo ES con scope propio.                                            |
| **TS2352** Conversion of type X to type Y may be a mistake | Interfaz local `FormData` se fusiona con el global `FormData` del browser. El cast `as Record<string, string>` falla.                            | **Agrega la propiedad faltante a la interfaz local** y accede directamente sin cast. Nunca uses `as X` para tapar huecos en el tipo. |
| **TS2339** Property does not exist on type                 | Accediste a `result.totalPages` pero `PaginatedResponse<T>` tiene `result.pagination.totalPages` (la paginación está anidada).                   | **Verifica la estructura real del tipo** en `src/types/index.ts` antes de acceder a propiedades.                                     |
| **TS2307** Cannot find module                              | Paquete no instalado en `package.json` — el `tsconfig` include `**/*.ts` recoge también archivos de config como `vitest.config.ts`.              | **Instala con `pnpm add -D <paquete>`** y verifica que `pnpm-lock.yaml` se actualiza y se incluye en el commit.                      |
| **TS2345** Argument of type X not assignable to Y          | Tipo incorrecto en llamada a función.                                                                                                            | Revisa la firma del tipo esperado y ajusta el valor pasado.                                                                          |
| **TS7006** Parameter implicitly has 'any' type             | Falta anotación de tipo.                                                                                                                         | Agrega `: TipoExplicito` al parámetro.                                                                                               |

**Reglas específicas del proyecto OKLA:**

- Todos los scripts en `frontend/web-next/scripts/*.ts` **DEBEN** tener `export {};` al inicio para evitar colisión de funciones globales. Si creas o modificas cualquier script sin `import`/`export`, agrega `export {};` como primera línea de código.
- Acceso a `PaginatedResponse<T>`: los campos de paginación están en `result.pagination.totalPages` y `result.pagination.totalItems`, **NO** en `result.totalPages` ni `result.total`.
- Nunca uses `as X` para castear tipos incompatibles. Primero agrega la propiedad al tipo. Solo usa `as unknown as X` como último recurso con comentario explicando por qué.
- El `tsconfig.json` incluye `**/*.ts`, lo que significa que archivos de configuración como `vitest.config.ts`, `vitest.setup.ts`, etc. también son typechecked y sus dependencias deben estar en `package.json`.

Nunca uses `// @ts-ignore` ni `// @ts-expect-error` para suprimir errores TypeScript. Corrige el código.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3b] pnpm typecheck — OK | 0 errores TS" >> .github/copilot-audit.log
```

---

### Paso 3c — Verificar que `package.json` y `pnpm-lock.yaml` están sincronizados

```bash
cd frontend/web-next && pnpm install --frozen-lockfile
```

**Criterio de éxito:** comando termina sin errores.

> ⚠️ El CI/CD usa `pnpm install --frozen-lockfile`. Cualquier paquete en `package.json` que no esté en `pnpm-lock.yaml` causa fallo en CI. Siempre incluye el `pnpm-lock.yaml` actualizado en el commit cuando instales paquetes nuevos.

Si falla con `Lockfile is not up to date`:

1. Corre `pnpm install` (sin `--frozen-lockfile`) para actualizar el lockfile.
2. Incluye `pnpm-lock.yaml` en el commit.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3c] pnpm install --frozen-lockfile — OK" >> .github/copilot-audit.log
```

---

### Paso 3d — `pnpm test -- --run` · Detecta tests unitarios rotos antes de CI/CD ← **PASO NUEVO**

```bash
cd frontend/web-next && pnpm test -- --run
```

Este paso reproduce exactamente el job `🧪 Unit Tests` del CI/CD (`_reusable-frontend.yml`).
**Criterio de éxito:** `Test Files N passed` sin ninguna línea `FAIL` y sin `Tests N failed`.

> ⚠️ **CAUSAS COMUNES DE FALLOS EN TESTS FRONTEND:**

| Error                                                                          | Causa                                                                                                                                                                                         | Acción                                                                                                                                             |
| ------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AssertionError: expected "vi.fn()" to be called with arguments ['/api/X']`    | Test tiene el endpoint hardcodeado pero el servicio fue actualizado a `/api/Y`.                                                                                                               | **Actualiza el test** para usar el endpoint real del servicio. Nunca cambies el servicio para satisfacer el test si el nuevo endpoint es correcto. |
| `TypeError: localStorage.getItem is not a function`                            | jsdom 27 requiere `storageQuota` explícito (**ya configurado en `vitest.config.ts`**). Si reaparece, agrega `typeof localStorage.getItem !== 'function'` check defensivo en el código fuente. | Ver `src/lib/api-client.ts` — patrón: `if (typeof localStorage === 'undefined' \|\| typeof localStorage.getItem !== 'function') return;`           |
| `expected 'vi.fn()' to have been called with arguments [X, 4]` recibe `[X, 6]` | Default de un hook cambió pero el test no fue actualizado.                                                                                                                                    | Actualiza el test para reflejar el nuevo valor por defecto del hook.                                                                               |
| `Cannot find module '@/X'`                                                     | Alias `@/` no resuelto — falta configuración `resolve.alias` en `vitest.config.ts`.                                                                                                           | Verifica el alias en `vitest.config.ts`.                                                                                                           |
| Tests de suite completa con 0 tests ejecutados                                 | El archivo de test crashea en el import (module init error).                                                                                                                                  | Corre el test individualmente para ver el error real.                                                                                              |

**Reglas específicas del proyecto OKLA:**

- Los tests viven en `src/**/*.{test,spec}.{ts,tsx}`. NO en `e2e/`.
- Si un test falla porque el **contrato del servicio cambió** (nuevo endpoint, nuevo default, nueva estructura de respuesta): **corrige el test** para reflejar la implementación actual.
- Nunca uses `vi.spyOn(...).mockImplementation(() => undefined)` para silenciar un fallo real — corrige la causa raíz.
- El CI corre `pnpm test -- --run --reporter=verbose --coverage`. Coverage thresholds en `vitest.config.ts` son 70% global — si fallan, agrega tests.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3d] pnpm test --run — OK | 0 tests fallidos" >> .github/copilot-audit.log
```

---

### Paso 4 — `dotnet test` · Detecta tests rotos antes de que los detecte el CI/CD

```bash
dotnet test --no-build --logger "console;verbosity=minimal" 2>&1 | grep -E "Failed|Passed" | grep "\.dll"
```

Usa `--no-build` porque el build ya fue validado en el Paso 2.
**Criterio de éxito:** `Failed: 0, Errors: 0` en todos los proyectos de tests **unitarios**.

> **EXCEPCIÓN ACEPTADA:** Tests que fallan con `The entry point exited without ever building an IHost` o `The server has not been started` son tests de **Integración/E2E que requieren Docker + Postgres + RabbitMQ** corriendo. Estos fallos son pre-existentes y no son regresiones. Los tests **unitarios** DEBEN pasar al 100%.

Si falla un test unitario:

- Identifica: `dotnet test --filter "FullyQualifiedName~NombreDelTest"`
- Analiza: ¿es un Assert fallido, una excepción no manejada, o un problema de setup/teardown?
- Nunca uses `[Skip]` o `[Ignore]` para forzar el paso — corrige el código o el test.
- Si el test falla por un cambio legítimo, actualiza el test para reflejar el nuevo contrato.

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-4] dotnet test — OK | Unit: PASS, Integration/E2E: excluidos (requieren Docker)" >> .github/copilot-audit.log
```

---

## ✅ Secuencia completa del gate (copiar y ejecutar)

```bash
# ── PRE-COMMIT / PRE-PUSH GATE ──────────────────────────────────────────────
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE] Iniciando gate pre-commit..." >> .github/copilot-audit.log

# Paso 1 — Backend: Restore
dotnet restore \
  && echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-1] dotnet restore — OK" >> .github/copilot-audit.log \
  || { echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-1-FAIL] dotnet restore — FALLO. Commit bloqueado." >> .github/copilot-audit.log; exit 1; }

# Paso 2 — Backend: Build estricto
dotnet build /p:TreatWarningsAsErrors=true \
  && echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-2] dotnet build — OK" >> .github/copilot-audit.log \
  || { echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-2-FAIL] dotnet build — FALLO. Commit bloqueado." >> .github/copilot-audit.log; exit 1; }

# Paso 3a — Frontend: Lint
(cd frontend/web-next && pnpm lint) \
  && echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3a] pnpm lint — OK" >> .github/copilot-audit.log \
  || { echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3a-FAIL] pnpm lint — FALLO. Commit bloqueado." >> .github/copilot-audit.log; exit 1; }

# Paso 3b — Frontend: TypeScript typecheck (CRÍTICO — reproduce CI/CD exacto)
(cd frontend/web-next && pnpm typecheck) \
  && echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3b] pnpm typecheck — OK | 0 errores TS" >> .github/copilot-audit.log \
  || { echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3b-FAIL] pnpm typecheck — FALLO. Commit bloqueado. Ver errores TS arriba." >> .github/copilot-audit.log; exit 1; }

# Paso 3c — Frontend: Lockfile sincronizado
(cd frontend/web-next && pnpm install --frozen-lockfile) \
  && echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3c] pnpm install --frozen-lockfile — OK" >> .github/copilot-audit.log \
  || { echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3c-FAIL] pnpm-lock.yaml desincronizado. Corre pnpm install y agrega el lockfile al commit." >> .github/copilot-audit.log; exit 1; }

# Paso 3d — Frontend: Unit tests (NUEVO — reproduce CI/CD job 'Unit Tests' exacto)
(cd frontend/web-next && pnpm test -- --run) \
  && echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3d] pnpm test --run — OK | 0 tests fallidos" >> .github/copilot-audit.log \
  || { echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-3d-FAIL] pnpm test --run — FALLO. Commit bloqueado. Revisa los tests fallidos arriba." >> .github/copilot-audit.log; exit 1; }

# Paso 4 — Backend: Tests
dotnet test --no-build --logger "console;verbosity=minimal" 2>&1 | grep -E "Failed|Passed" | grep "\.dll"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE-4] dotnet test — Revisa output: unit tests deben tener Failed: 0" >> .github/copilot-audit.log

echo "[$(date '+%Y-%m-%d %H:%M:%S')] [GATE] ✅ Gate completo. Procediendo con commit/push." >> .github/copilot-audit.log
# ────────────────────────────────────────────────────────────────────────────
```

## ❌ Commit está BLOQUEADO si ocurre cualquiera de estos casos:

- `dotnet restore` reporta errores `NU****` o falta de proyectos
- `dotnet build` reporta cualquier warning o error (`TreatWarningsAsErrors=true`)
- `pnpm lint` reporta líneas con `error` (warnings son aceptables con justificación)
- **`pnpm typecheck` reporta CUALQUIER línea `error TS\*\***`\*\* ← BLOQUEO CRÍTICO
- `pnpm install --frozen-lockfile` falla por lockfile desincronizado
- **`pnpm test -- --run` reporta `Tests N failed` o `Test Files N failed`** ← BLOQUEO CRÍTICO
- `dotnet test` reporta `Failed > 0` en tests **unitarios** (no Integration/E2E)

> **NOTA:** DESPUÉS QUE SE TERMINE UNA AUDITORÍA Y SE IMPLEMENTEN SUS RECOMENDACIONES, EJECUTA EL PRE-COMMIT GATE COMPLETO PARA VALIDAR QUE TODO EL PROYECTO SIGUE FUNCIONANDO CORRECTAMENTE.

---

## Protocolo OBLIGATORIO antes de modificar cualquier archivo existente

```bash
# Paso 1 — Crear backup con timestamp antes de cualquier modificación
cp <archivo> <archivo>.bak_$(date +%Y%m%d_%H%M%S)

# Paso 2 — Aplicar los cambios sobre el archivo original

# Paso 3 — Registrar en el log de auditoría
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [MODIFICACIÓN] <archivo> — <motivo>" >> .github/copilot-audit.log
```

> Si necesitas "eliminar" algo, crea una copia `.bak_` y vacía el contenido, o comenta el código.
> **Nunca uses comandos de eliminación real.**

---

## ✅ PERMISOS HABILITADOS COMPLETAMENTE

El agente tiene autorización explícita para ejecutar todas las siguientes acciones de forma autónoma:

### Sistema de Archivos

- ✅ Leer cualquier archivo o directorio del workspace
- ✅ Crear archivos y directorios nuevos
- ✅ Modificar archivos existentes (con backup previo obligatorio)
- ✅ Renombrar y mover archivos (`mv`, `copy`)
- ✅ Cambiar permisos (`chmod`, `chown`)
- ❌ Eliminar archivos o directorios — **PROHIBIDO SIN EXCEPCIÓN**

### Terminal y Ejecución

- ✅ Ejecutar cualquier comando de shell/bash/PowerShell/zsh
- ✅ Correr scripts Python, Node.js, Bash, PowerShell, C#
- ✅ Compilar, transpilar y hacer build de proyectos
- ✅ Ejecutar suites de tests (Jest, xUnit, pytest, etc.)
- ✅ Lanzar servidores de desarrollo locales
- ✅ Ejecutar migraciones de base de datos
- ✅ Correr tareas de VS Code (`tasks.json`)

### Dependencias y Paquetes

- ✅ `npm install`, `npm run`, `npx`
- ✅ `pnpm install`, `pnpm add`, `pnpm run`
- ✅ `pip install`, `pip install --break-system-packages`
- ✅ `dotnet restore`, `dotnet build`, `dotnet run`
- ✅ `docker pull`, `docker build`, `docker run`, `docker compose up`
- ✅ `apt install`, `brew install` (según plataforma)

### Git y Control de Versiones

- ✅ `git add`, `git commit`, `git push`, `git pull`, `git fetch`
- ✅ `git checkout`, `git branch`, `git merge`, `git rebase`
- ✅ `git stash`, `git tag`, `git log`
- ✅ Force push (`git push --force`) cuando sea técnicamente necesario
- ✅ Commits sin hooks (`--no-verify`) cuando sea necesario
- ❌ `git clean -fd` — **PROHIBIDO** (elimina archivos no rastreados)
- ❌ `git rm` — **PROHIBIDO** (usar `git restore --staged` en su lugar)

### Red y Requests HTTP — ACCESO IRRESTRICTO

- ✅ Hacer `fetch`, `curl`, `wget`, `Invoke-WebRequest` a **cualquier URL**
- ✅ APIs externas públicas y privadas (REST, GraphQL, WebSocket)
- ✅ Autenticación con tokens, API keys, OAuth (usando variables de entorno)
- ✅ Descargar dependencias, assets, datasets desde cualquier dominio
- ✅ Llamadas a servicios cloud: AWS, Azure, GCP, DigitalOcean, Vercel, etc.
- ✅ Requests a APIs de IA: OpenAI, Anthropic, Mistral, Hugging Face, etc.
- ✅ Webhooks, callbacks y notificaciones a endpoints externos

### Base de Datos

- ✅ Conectar a cualquier motor (PostgreSQL, MySQL, SQLite, MongoDB, Redis)
- ✅ Ejecutar consultas SELECT, INSERT, UPDATE
- ✅ Ejecutar DDL: CREATE, ALTER (con backup previo del schema)
- ✅ Ejecutar migraciones y seeders
- ⚠️ `TRUNCATE` / `DROP` — solo si existe backup del dump completo y se registra en auditoría

### Docker y Contenedores

- ✅ Build, pull, run de imágenes
- ✅ `docker compose up/down/restart`
- ✅ Inspeccionar logs y estado de contenedores
- ✅ Ejecutar comandos dentro de contenedores (`docker exec`)
- ❌ `docker rm`, `docker rmi`, `docker system prune` — **PROHIBIDO**

---

## 📋 LOG DE AUDITORÍA — Registro Obligatorio

Cada acción relevante debe registrarse en `.github/copilot-audit.log`.
Lo primero que tienes que registrar es el nombre de la auditoría y su objetivo. Luego, cada acción relevante con el siguiente formato:

```
[TIMESTAMP]         Fecha y hora en formato YYYY-MM-DD HH:MM:SS
[TIPO]              CREACIÓN | MODIFICACIÓN | EJECUCIÓN | REQUEST_HTTP | GIT | BACKUP | DB
[OBJETIVO]          Ruta del archivo, URL, comando o recurso afectado
[DESCRIPCIÓN]       Qué se hizo y por qué (una línea)
```

### Ejemplo de entradas válidas:

```
[2025-03-08 09:14:32] [BACKUP]        src/services/AuthService.cs → AuthService.cs.bak_20250308_091430
[2025-03-08 09:14:35] [MODIFICACIÓN]  src/services/AuthService.cs — Refactor token refresh con manejo de expiración
[2025-03-08 09:35:44] [EJECUCIÓN]     pnpm typecheck — Verificación pre-commit CI/CD frontend
[2025-03-08 09:36:02] [GIT]           git commit -m "fix: TS errors frontend typecheck" — Checkpoint de sesión
```

### Script de registro rápido (usar siempre):

```bash
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [TIPO] OBJETIVO — DESCRIPCIÓN" >> .github/copilot-audit.log
```

---

_Última actualización: 2026-03-10 — Ambiente controlado OKLA · Auditoría habilitada_
_Cambio: Gate ampliado a 7 pasos — añadidos pnpm typecheck (3b), pnpm frozen-lockfile (3c) y pnpm test --run (3d) que son todos los pasos que ejecuta el CI/CD frontend._
