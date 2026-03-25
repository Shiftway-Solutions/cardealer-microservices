# RE-AUDITORÍA (Verificación de fixes, intento 2/3) — Sprint 2: Búsqueda & Filtros de Vehículos (Guest)

**Fecha:** 2026-03-25 11:19:23
**Fase:** REAUDIT
**Usuario:** Guest (sin login)
**URL:** https://okla.com.do

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 2 (intento 2/3).
Re-ejecuta las mismas tareas de auditoría con Chrome para verificar que los fixes funcionan.

- Si TODOS los bugs están corregidos → agrega `READ` al final
- Si ALGÚN bug persiste → documenta cuáles persisten en 'Hallazgos'
  y agrega `READ` igualmente. El script enviará otra ronda de fixes.

IMPORTANTE: Usa Chrome como un humano. NO scripts.

## Credenciales

| Rol                 | Email                  | Password       |
| ------------------- | ---------------------- | -------------- |
| Admin               | admin@okla.local       | Admin123!@#    |
| Buyer               | buyer002@okla-test.com | BuyerTest2026! |
| Dealer              | nmateo@okla.com.do     | Dealer2026!@#  |
| Vendedor Particular | gmoreno@okla.com.do    | $Gregory1      |

---

## TAREAS

### S2-T01: Auditar listado y filtros de /vehiculos

**Pasos:**

- [x] Paso 1: Abre Chrome y navega a https://okla.com.do/vehiculos
- [x] Paso 2: Toma una screenshot de la página completa
- [x] Paso 3: Verifica que dice '149 vehículos encontrados' (o el conteo actual) → **149 vehículos ✅**
- [x] Paso 4: Verifica la trust bar → **Vendedores verificados · +2,400 vehículos activos · Contacto directo · Alertas gratis ✅**
- [x] Paso 5: Filtros laterales → **Condición, Marca y Modelo, Precio, Año, Carrocería (10 tipos), Ubicación, Filtros avanzados ✅**
- [x] Paso 6: Filtro precio '< 1M' → **21 vehículos, tag "Hasta RD$1M ×" ✅**
- [x] Paso 7: Resultados bajo RD$1M → **Nissan Sentra RD$950K, Chevrolet Spin RD$900K, Suzuki Ertiga RD$1M ✅**
- [x] Paso 8: Filtro SUV → **38 vehículos filtrados ✅**
- [x] Paso 9: Solo SUVs → **Bentley Bentayga, Mercedes G63, Porsche Cayenne ✅**
- [x] Paso 10: Vehicle cards → **imagen, badge (PARTICULAR/DEALER/VERIFICADO), año, km, combustible, ubicación, precio RD$ + ≈USD ✅**

**A validar:**

- [x] FRONTEND-018: ✅ **CORREGIDO** — Combustible muestra "Gasolina" (no "gasoline") incluso con sort=price_asc aplicado. Verificado en listado SUV, precio y ordenamiento.
- [x] FRONTEND-019: ✅ **CORREGIDO** — Filtro "< 1M" muestra 21 vehículos bajo RD$1M.
- [x] FRONTEND-020: ✅ **CORREGIDO** — RD$16,800,000 ≈ $277,686 (tasa ~60.5).
- [x] FRONTEND-026: ✅ **CORREGIDO** — Ordenamiento "Precio: Menor a mayor" funciona (1,150K→1,200K→1,250K).
- [x] FRONTEND-029: ⚠️ **PARCIAL** — Listing cards NO muestran "Nuevo" para vehículos con >1000 km ✅. PERO la página de detalle del vehículo (ej. 2024 Toyota Corolla con 32,404 km) aún muestra badge "Nuevo" (alcance diferente, no cubierto por fix original).

**Hallazgos:**

- FRONTEND-018: **CORREGIDO** — Fuel type muestra "Gasolina" en todos los escenarios probados (sin filtro, con filtro SUV, con sort=price_asc, en página 2).
- FRONTEND-029: **PARCIAL** — El fix en `transformToCardData` funciona para listing cards, pero la página de detalle del vehículo usa datos distintos y aún muestra "Nuevo" + "Destacado" para vehículos con 32,404 km. Se necesita fix adicional en la vista detalle.
- FRONTEND-020: Conversión RD$/USD verificada: 16,800,000/60.5 ≈ 277,686 ✅, 13,200,000/60.5 ≈ 218,182 ✅

---

### S2-T02: Auditar paginación y vehículos patrocinados

**Pasos:**

- [x] Paso 1: Navega a https://okla.com.do/vehiculos → **149 vehículos, 7 páginas ✅**
- [x] Paso 2: Scroll al final → **Paginación visible: "Anterior 1 2 ... 7 Siguiente" ✅**
- [x] Paso 3: Screenshot paginación → **"Mostrando página 1 de 7 (149 vehículos)" ✅** (7 páginas, no 15)
- [x] Paso 4: Click Página 2 → **Carga nuevos vehículos (Honda CR-V 2026, Honda Civic 2018, Dodge Grand Caravan 2023) ✅**
- [x] Paso 5: Regresa a página 1 ✅
- [x] Paso 6: Bloques "Vehículos Patrocinados" → **5 bloques (1 sidebar "Recomendados" + 4 inline) ✅**
- [x] Paso 7: Screenshot patrocinados → **CR-V Touring, Tucson SEL, Corolla LE, Sportage EX ✅**
- [x] Paso 8: ❌ **Patrocinados REPITEN** — CR-V y Tucson aparecen en bloques 1, 2 y 4. Fix (commit 9d3103e0) aún NO desplegado.
- [x] Paso 9: Badge visual diferente → **DEALER+VERIFICADO badges, borde verde, "Publicidad" label, Est. mensual, rating ⭐ ✅**

**A validar:**

- [x] FRONTEND-021: ✅ **CORREGIDO** — Patrocinados tienen borde verde, header "Vehículos Patrocinados / Publicidad", badges DEALER+VERIFICADO, precio estimado mensual, rating.
- [x] FRONTEND-024: ✅ **CORREGIDO** — Paginación funciona, página 2 carga datos diferentes, total consistente (149).
- [x] FRONTEND-025: ❌ **PERSISTE EN PRODUCCIÓN** — CR-V y Tucson repiten en bloques. Fix committeado (9d3103e0) pero NO desplegado. Código verificado correcto via Gate (576 tests).

**Hallazgos:**

- FRONTEND-025: **PENDIENTE DEPLOY** — Fix cambia shuffle a hash determinístico + deduplicación. Código correcto, producción usa versión anterior. Bloques: B1(CR-V,Tucson), B2(CR-V,Tucson REPETIDO), B3(Sportage), B4(CR-V,Tucson,Corolla).
- 7 páginas de paginación (no 15 como esperaba el task)

---

### S2-T03: Auditar búsqueda y alertas sin auth

**Pasos:**

- [x] Paso 1: Navega a https://okla.com.do/vehiculos ✅
- [x] Paso 2: Escribe 'Toyota Corolla' → **IA interpretó: "Toyota Corolla - todos los años, precios y condiciones" — 95% confianza, 790ms, caché ✅**
- [x] Paso 3: Screenshot resultados → **7 vehículos encontrados, filtros Toyota + Corolla + sedan ✅**
- [x] Paso 4: Solo Toyota Corolla → **3 Corollas visibles: 2024 (32,404km RD$1.2M), 2022 (15,000km RD$1.25M), 2024 (24,992km RD$950K) ✅**
- [x] Paso 5: 'Guardar búsqueda' → **Redirige a /login?redirect=/vehiculos ✅**
- [x] Paso 6: 'Activar alertas' → **Redirige a /login?callbackUrl=%2Fcuenta%2Falertas ✅**
- [x] Paso 7: 'Contactar vendedor' (Chat en vivo en detalle) → **Popup inline: "Inicia sesión para chatear" con botones "Iniciar sesión" y "Crear cuenta gratis" ✅**

**A validar:**

- [x] FRONTEND-005: ✅ **CORREGIDO** — Búsqueda IA funciona (95% confianza, filtros automáticos Toyota+Corolla+sedan).
- [x] FRONTEND-022: ✅ **CORREGIDO** — 'Guardar búsqueda' redirige a login.
- [x] FRONTEND-023: ✅ **CORREGIDO** — 'Activar alertas' redirige a login.
- [x] FRONTEND-030: ✅ **CORREGIDO** — 'Contactar vendedor' muestra popup inline de login (no permite contacto anónimo).

**Hallazgos:**
- Búsqueda IA excelente: interpreta "Toyota Corolla" y aplica filtros automáticos (marca+modelo+carrocería).
- 'Contactar vendedor' en listing card navega al detalle del vehículo. 'Chat en vivo' en detalle muestra popup de login inline, correcto y user-friendly.

---

## Resultado

- Sprint: 2 — Búsqueda & Filtros de Vehículos (Guest)
- Fase: REAUDIT (intento 2/3)
- Estado: COMPLETADO
- Bugs encontrados: 1 persiste (FRONTEND-025 pendiente deploy), 1 parcial (FRONTEND-029 detalle)
- Resumen:
  - ✅ FRONTEND-005: Búsqueda IA funciona
  - ✅ FRONTEND-018: Combustible en español ("Gasolina") incluso con sort params
  - ✅ FRONTEND-019: Filtros de precio funcionan
  - ✅ FRONTEND-020: Conversión RD$/USD correcta
  - ✅ FRONTEND-021: Patrocinados visualmente distintos
  - ✅ FRONTEND-022: 'Guardar búsqueda' pide login
  - ✅ FRONTEND-023: 'Activar alertas' pide login
  - ✅ FRONTEND-024: Paginación funciona
  - ❌ FRONTEND-025: Patrocinados repiten (fix code OK, pendiente deploy)
  - ✅ FRONTEND-026: Ordenamiento funciona
  - ⚠️ FRONTEND-029: Listing cards OK, detalle aún muestra "Nuevo" para 32K km
  - ✅ FRONTEND-030: 'Contactar vendedor' muestra login popup

---

_Cuando termines, agrega la palabra READ al final de este archivo._
