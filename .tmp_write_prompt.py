import sys

content = """# AUDITORÍA — Sprint 13: Calidad de Datos — Lo que el Usuario Ve Mal
**Fecha:** 2026-04-01 11:40:38
**Fase:** AUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://apparently-bride-achieved-pdt.trycloudflare.com)
**Usuario:** Guest
**URL Base:** https://apparently-bride-achieved-pdt.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://apparently-bride-achieved-pdt.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

---

## TAREAS

### S13-T01: Buscar anomalías visibles en los listados

**Pasos:**
- [x] Paso 1: Navega a {BASE_URL}/vehiculos sin filtros
- [x] Paso 2: Scroll por TODAS las páginas disponibles (mín 5 páginas)
- [x] Paso 3: BUSCAR: palabras en inglés — 'gasoline', 'diesel', 'electric', 'automatic', 'manual'
- [x] Paso 4: BUSCAR: ubicaciones mal formateadas — 'Santo DomingoNorte', 'Santiago De Los Caballeros' sin tilde
- [x] Paso 5: BUSCAR: vehículos de prueba — 'E2E', 'test', 'mm8mioxc' en título
- [x] Paso 6: BUSCAR: precios sospechosos — RD$0, RD$1, precios negativos
- [x] Paso 7: BUSCAR: vehículos sin foto
- [x] Paso 8: BUSCAR: vehículos duplicados (mismo carro 2 veces)
- [x] Paso 9: Toma screenshot de CADA anomalía encontrada
- [x] Paso 10: Regresa a la homepage
- [x] Paso 11: Verifica estadísticas: '10,000+ Vehículos' — ¿cuántos hay realmente en /vehiculos?
- [x] Paso 12: Verifica: '500+ Dealers' — ¿cuántos hay en /dealers?
- [x] Paso 13: Verifica: '50,000+ Usuarios' — ¿parece real o inflado?
- [x] Paso 14: ¿Los testimonios del homepage son de personas reales?
- [x] Paso 15: Agrega READ al final de este archivo y luego ejecuta el prompt AGENT_LOOP_PROMPT.md

**A validar:**
- [x] UF-085: ¿No hay texto en inglés mezclado en los listados? — PASS (UI traduce correctamente)
- [x] UF-086: ¿Las ubicaciones están bien formateadas en español? — PASS
- [x] UF-087: ¿No hay vehículos E2E/test visibles al público? — PASS
- [x] UF-088: ¿Las estadísticas del homepage reflejan datos reales? — FAIL (ver hallazgos)
- [x] UF-089: ¿No hay precios sospechosos (RD$0, negativos)? — PASS

**Hallazgos:**

BUG-1 (CRITICO — UF-088): Estadísticas del homepage NO reflejan datos reales
- Homepage muestra: "10,000+ Vehículos publicados" — Real: 5 vehículos en DB
- Homepage muestra: "500+ Dealers registrados" — Real: 1 dealer (Auto Mateo RD) en DB
- Homepage /dealers: "Únete a los +500 dealers" — Real: 1 dealer
- Dealers showcase: muestra 4x "Tu marca aquí / Espacio disponible" (ningún dealer real featured)
- Mitigación actual: asterisco "*Cifras proyectadas basadas en el mercado automotriz dominicano"
- Riesgo: confianza del usuario — stats infladas vs 5 vehículos en listado

BUG-2 (MENOR): Typos en descripción de seed data
- 2020 Nissan Sentra: "un solo dueno" — debe ser "un solo dueño"
- 2020 Nissan Sentra: "Mantenimiento al dia" — debe ser "Mantenimiento al día"

BUG-3 (INFO): Datos semilla con baja diversidad
- 5/5 vehículos con fuelType: Gasolina (mismo combustible)
- 5/5 vehículos con transmission: Manual (misma transmisión)
- 5/5 vehículos con seller: Auto Mateo RD (mismo dealer)

UF-085 PASS: UI traduce correctamente todos los campos del backend
- "Gasoline" (API) — "Gasolina" (UI)
- "CertifiedPreOwned" (API) — "Certificado" (UI)
- Sin texto en inglés visible en listados

UF-086 PASS: Ubicaciones correctamente formateadas
- "Santo Domingo, Distrito Nacional" sin concatenaciones malformadas

UF-087 PASS: Sin vehículos de test/E2E visibles al público

UF-089 PASS: Todos los precios son realistas (RD$850K a RD$1.68M)

Paso 14 — Testimonios: No existe sección de testimonios en el homepage actual. Feature pendiente.

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega READ al final de este archivo y luego ejecuta el prompt AGENT_LOOP_PROMPT.md

---

## Resultado
- Sprint: 13 — Calidad de Datos — Lo que el Usuario Ve Mal
- Fase: AUDIT
- URL: https://apparently-bride-achieved-pdt.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados:
  - BUG-1 (CRITICO): Stats del homepage infladas (10K vehículos vs 5 reales, 500 dealers vs 1 real)
  - BUG-2 (MENOR): Typos en seed data — "dueno" falta ñ, "dia" falta tilde
  - BUG-3 (INFO): Baja diversidad en datos semilla (5 vehículos misma config)
  - UF-085/086/087/089: PASS — sin problemas visibles

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: AGENT_LOOP_PROMPT.md_
READ
"""

with open('/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/prompt_1.md', 'w') as f:
    f.write(content)
print("OK - archivo escrito")
