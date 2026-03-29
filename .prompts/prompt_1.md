# CORRECCIÓN (Intento 1/3) — Sprint 6: Seller — Publicar Mi Primer Vehículo
**Fecha:** 2026-03-29 10:11:06
**Fase:** FIX
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
**Usuario:** Seller (gmoreno@okla.com.do / $Gregory1)
**URL Base:** https://ought-feed-shipping-wright.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://ought-feed-shipping-wright.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://ought-feed-shipping-wright.trycloudflare.com |
| API (tunnel) | https://ought-feed-shipping-wright.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — FASE DE CORRECCIÓN
En la auditoría anterior se encontraron bugs. Tu trabajo ahora es:

1. Lee la sección 'BUGS A CORREGIR' abajo
2. Corrige cada bug en el código fuente
3. Ejecuta el Gate Pre-Commit (8 pasos) para validar
4. Marca cada fix como completado: `- [ ]` → `- [x]`
5. Al terminar, agrega `READ` al final

⚠️ NO hagas commit aún — primero el sprint debe pasar RE-AUDITORÍA

## BUGS A CORREGIR
_(El agente que hizo la auditoría documentó los hallazgos aquí.)_
_(Lee el archivo de reporte del sprint anterior para ver los bugs.)_

Revisa el último reporte en `audit-reports/` o los hallazgos del prompt anterior.
Corrige todos los bugs encontrados:

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

---

## TAREAS

- [x] Fix bugs de S6-T01: Wizard de publicación paso a paso
  - [x] BUG-S6-01: Banner bienvenida actualizado de "3 pasos" a "6 pasos" con todos los pasos reales
  - [x] BUG-S6-02: Validación por paso implementada en wizard (info: make/model/year/bodyStyle/fuelType/transmission/mileage; photos: mín 1 foto; pricing: precio+provincia)
  - [x] BUG-S6-03: Hint fotos ahora es plan-aware — muestra `maxPhotos` dinámicamente si < 8
  - [x] BUG-S6-04: Condición traducida al español en Paso 6 (Used → Usado, New → Nuevo, CertifiedPreOwned → Certificado)

- [x] Fix bugs de S6-T02: Dashboard del vendedor
  - [x] BUG-S6-05: Medidor "Destacadas este mes" oculto cuando featuredPerMonth === 0 (plan Libre)
  - [x] BUG-S6-06: "Renovación de listing: $6.99" corregido a "RD$579" en plan Estándar
  - [x] BUG-S6-07: Upgrade banner corregido de "50 fotos" a "12 fotos" (máx plan Verificado)

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
  - dotnet restore: ✅ 0 errores
  - dotnet build /p:TreatWarningsAsErrors=true: ✅ 0 errores, 0 warnings
  - pnpm lint: ✅ 0 errores (18 warnings pre-existentes)
  - pnpm typecheck: ✅ 0 errores
  - pnpm test --run: ✅ 576 passed, 0 failed
  - pnpm build: ✅ Compiled successfully
  - dotnet test (unit): ✅ Failed: 0 (integration/E2E pre-existentes excluidos)

## Resultado
- Sprint: 6 — Seller — Publicar Mi Primer Vehículo
- Fase: FIX
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
- URL: https://ought-feed-shipping-wright.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines, agrega la palabra READ al final de este archivo._
