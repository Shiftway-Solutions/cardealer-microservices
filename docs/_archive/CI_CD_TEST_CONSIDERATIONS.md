# 🚀 CI/CD Test Considerations - CarDealer Microservices

> **Fecha de Creación:** 6 de Enero 2026  
> **Estado:** ✅ Tests listos para CI/CD  
> **Tests Totales:** ~2,350+ tests en 42 proyectos

---

## 📊 Resumen de Tests

### Estado Actual

| Categoría | Proyectos | Tests | Estado |
|-----------|-----------|-------|--------|
| Unit Tests | 39 | ~2,200+ | ✅ Passing |
| Integration Tests | 4 | ~109 | ✅ Passing |
| **TOTAL** | **42** | **~2,350+** | **✅ 100%** |

### Proyectos de Integration Tests

| Proyecto | Tests | Containers | Timeout Recomendado | Trait |
|----------|-------|------------|---------------------|-------|
| IntegrationTests (Global) | 54 | PostgreSQL, Redis, RabbitMQ | 5 min | Integration |
| ConfigurationService.IntegrationTests | 20 | PostgreSQL | 2 min | Integration |
| MessageBusService.IntegrationTests | 12 | RabbitMQ | 2 min | Integration |
| LoggingService.IntegrationTests | 23 | Seq (datalust/seq:2024) | 3 min | E2E, Integration |

---

## ⚙️ Configuración del Pipeline

### GitHub Actions (Recomendado)

```yaml
name: CI/CD Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore backend/CarDealer.sln
      
      - name: Build
        run: dotnet build backend/CarDealer.sln --no-restore --configuration Release
      
      - name: Run Unit Tests
        run: |
          dotnet test backend/CarDealer.sln \
            --no-build \
            --configuration Release \
            --verbosity minimal \
            --filter "FullyQualifiedName!~IntegrationTests" \
            --logger "trx;LogFileName=test-results.trx" \
            --collect:"XPlat Code Coverage"
        timeout-minutes: 10

  integration-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    needs: unit-tests
    
    services:
      docker:
        image: docker:dind
        options: --privileged
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      # Pre-pull images for faster tests
      - name: Pre-pull Docker images
        run: |
          docker pull postgres:16-alpine &
          docker pull redis:7-alpine &
          docker pull rabbitmq:3-management-alpine &
          docker pull datalust/seq:2024 &
          wait
      
      - name: Build
        run: dotnet build backend/CarDealer.sln --configuration Release
      
      - name: Run Integration Tests
        run: |
          dotnet test backend/CarDealer.sln \
            --no-build \
            --configuration Release \
            --verbosity minimal \
            --filter "FullyQualifiedName~IntegrationTests" \
            --logger "trx;LogFileName=integration-results.trx"
        timeout-minutes: 15
        env:
          TESTCONTAINERS_RYUK_DISABLED: false
          TESTCONTAINERS_REUSE_ENABLED: false
```

---

## 🐳 Consideraciones de Testcontainers

### Imágenes Docker Utilizadas

| Imagen | Versión | Uso |
|--------|---------|-----|
| `postgres:16-alpine` | Fixed | Base de datos |
| `redis:7-alpine` | Fixed | Cache |
| `rabbitmq:3-management-alpine` | Fixed | Message broker |
| `datalust/seq:2024` | Fixed | Log aggregator |

> ⚠️ **IMPORTANTE:** Siempre usar tags fijos, NUNCA `:latest` para reproducibilidad

### Timeouts Configurados

```csharp
// Timeout global para startup de containers
private static readonly TimeSpan ContainerStartTimeout = TimeSpan.FromMinutes(3);

// Uso con CancellationToken
using var cts = new CancellationTokenSource(ContainerStartTimeout);
await container.StartAsync(cts.Token);
```

### Pre-pulling de Imágenes

Para acelerar los tests en CI, pre-pull las imágenes:

```bash
# Script para pre-pull (ejecutar antes de tests)
docker pull postgres:16-alpine
docker pull redis:7-alpine
docker pull rabbitmq:3-management-alpine
docker pull datalust/seq:2024
```

---

## 🔧 Variables de Entorno para CI/CD

### Testcontainers

```yaml
env:
  # Deshabilitar Ryuk si hay problemas de permisos
  TESTCONTAINERS_RYUK_DISABLED: false
  
  # Reutilizar containers (útil para desarrollo local)
  TESTCONTAINERS_REUSE_ENABLED: false
  
  # Host de Docker (para runners self-hosted)
  DOCKER_HOST: unix:///var/run/docker.sock
```

### .NET

```yaml
env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  NUGET_PACKAGES: ${{ github.workspace }}/.nuget/packages
```

---

## 📁 Estructura de Tests

```
backend/
├── _Tests/
│   └── IntegrationTests/          # Tests E2E globales
│       ├── Fixtures/
│       │   ├── PostgresFixture.cs
│       │   ├── RedisFixture.cs
│       │   ├── RabbitMQFixture.cs
│       │   └── InfrastructureFixture.cs
│       ├── Gateway/
│       └── E2E/
├── ConfigurationService/
│   └── ConfigurationService.IntegrationTests/
├── MessageBusService/
│   └── MessageBusService.IntegrationTests/
├── LoggingService/
│   └── LoggingService.IntegrationTests/
└── [40+ Unit Test Projects]/
    └── *.Tests/
```

---

## ⚡ Optimizaciones para CI/CD

### 1. Paralelización de Tests

```yaml
# Los tests unitarios pueden correr en paralelo
- name: Run Tests (Parallel)
  run: |
    dotnet test backend/CarDealer.sln \
      --no-build \
      --configuration Release \
      --parallel \
      -- RunConfiguration.MaxCpuCount=4
```

### 2. Caching de NuGet

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

### 3. Separar Unit e Integration Tests

```yaml
# Filtrar por categoría
--filter "Category!=Integration"  # Solo unit tests
--filter "Category=Integration"   # Solo integration tests
--filter "Category!=E2E"          # Excluir tests E2E pesados (Seq)
--filter "Category=E2E"           # Solo tests E2E
```

### 4. Estrategia de CI/CD Recomendada

```yaml
# Pipeline rápido (PRs y commits frecuentes)
- name: Fast Tests (Unit + Light Integration)
  run: |
    dotnet test backend/CarDealer.sln \
      --no-build --configuration Release \
      --filter "Category!=E2E" \
      --verbosity minimal
  timeout-minutes: 10

# Pipeline completo (nightly o antes de release)
- name: Full Tests (Incluyendo E2E)
  run: |
    dotnet test backend/CarDealer.sln \
      --no-build --configuration Release \
      --verbosity minimal
  timeout-minutes: 25
```

### 5. Fail Fast

```yaml
# Detener en el primer fallo para feedback rápido
dotnet test --blame-hang-timeout 60s --blame-crash
```

---

## 🔴 Problemas Conocidos y Soluciones

### 1. Timeout de Testcontainers

**Problema:** Los containers tardan mucho en iniciar.

**Solución:**
- Pre-pull imágenes Docker
- Usar tags fijos en lugar de `:latest`
- Configurar timeout explícito con `CancellationToken`

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
await container.StartAsync(cts.Token);
```

### 2. Fire-and-Forget Tasks en Tests

**Problema:** Tasks async no se completan antes de las verificaciones.

**Solución:**
- Agregar `await Task.Delay(200)` después del handler
- Usar `Times.AtMostOnce()` en lugar de `Times.Once()`

```csharp
// Ejemplo corregido
var result = await handler.Handle(command, CancellationToken.None);
await Task.Delay(200);  // Esperar fire-and-forget

auditMock.Verify(x => x.LogAsync(...), Times.AtMostOnce);
```

### 3. Puertos Dinámicos

**Problema:** Conflictos de puertos en tests paralelos.

**Solución:** Ya implementado - todos los fixtures usan `WithPortBinding(port, true)` para puertos aleatorios.

---

## 📊 Reporte de Cobertura

### Configuración de Coverlet

```yaml
- name: Run Tests with Coverage
  run: |
    dotnet test backend/CarDealer.sln \
      --no-build \
      --configuration Release \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage

- name: Upload Coverage to Codecov
  uses: codecov/codecov-action@v4
  with:
    directory: ./coverage
    files: "**/coverage.cobertura.xml"
    fail_ci_if_error: false
```

---

## ✅ Checklist Pre-Merge

- [ ] Todos los unit tests pasan (42+ proyectos)
- [ ] Todos los integration tests pasan (4 proyectos)
- [ ] No hay warnings de compilación críticos
- [ ] Cobertura de código >= 80% en código nuevo
- [ ] Las imágenes Docker usan tags fijos
- [ ] Los timeouts están configurados apropiadamente

---

## 📈 Métricas de Rendimiento

| Métrica | Valor Actual | Objetivo |
|---------|--------------|----------|
| Tiempo Unit Tests | ~2-3 min | < 5 min |
| Tiempo Integration Tests | ~5-8 min | < 10 min |
| Tiempo Total CI | ~10-15 min | < 20 min |
| Cobertura Global | ~70% | >= 80% |

---

## 🔗 Referencias

- [Testcontainers .NET](https://dotnet.testcontainers.org/)
- [GitHub Actions Docker](https://docs.github.com/en/actions/using-containerized-services)
- [.NET Test Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/best-practices)
- [xUnit Documentation](https://xunit.net/)

---

*Documento generado el 6 de Enero 2026 - CarDealer Microservices v1.0*
