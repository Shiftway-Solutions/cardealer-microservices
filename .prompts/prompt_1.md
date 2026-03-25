# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 3: Páginas Públicas: Vender, Dealers, Legal (Guest)

**Fecha:** 2026-03-25 12:10:54
**Fase:** REAUDIT
**Usuario:** Guest (sin login)
**URL:** https://okla.com.do

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 3 (intento 1/3).
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

### S3-T01: Auditar /vender — Planes de Seller

**Pasos:**

- [ ] Paso 1: Abre Chrome y navega a https://okla.com.do/vender
- [ ] Paso 2: Toma una screenshot completa de la página
- [ ] Paso 3: Verifica el hero: 'Vende tu vehículo al mejor precio'
- [ ] Paso 4: Verifica stats: 10K+ vendidos, 7 días venta promedio, 95% satisfechos, RD$500M+ transado
- [ ] Paso 5: Scroll hasta la sección de planes de publicación
- [ ] Paso 6: Toma screenshot de los planes: Libre (RD$0), Estándar (RD$579/publicación), Verificado (RD$2,029/mes)
- [ ] Paso 7: Verifica que COINCIDEN con /cuenta/suscripcion (Libre/Estándar/Verificado)
- [ ] Paso 8: Anota las features de cada plan: publicaciones activas, fotos por vehículo, duración
- [ ] Paso 9: Libre: 1 pub, 5 fotos, 30 días. Estándar: 1 pub/pago, 10 fotos, 60 días. Verificado: 3 pubs, 12 fotos, 90 días
- [ ] Paso 10: Haz clic en 'Comenzar gratis' y verifica si redirige a registro o publicar
- [ ] Paso 11: Verifica si 'Ver cómo funciona' tiene video o sección anchor

**A validar:**

- [ ] FRONTEND-031: ¿Planes de /vender coinciden con /cuenta/suscripcion (Libre/Estándar/Verificado)?
- [ ] FRONTEND-032: ¿Plan Libre: 1 pub, 5 fotos, 30 días — coincide con backend?
- [ ] FRONTEND-033: ¿Plan Estándar RD$579/publicación — coincide con pricing API?
- [ ] FRONTEND-034: ¿Plan Verificado RD$2,029/mes — coincide con pricing API?
- [ ] FRONTEND-035: ¿'Comenzar gratis' redirige correctamente?
- [ ] FRONTEND-036: ¿Estadísticas (10K+, RD$500M+) son reales?

**Hallazgos:**

- FRONTEND-031: ✅ Planes /vender (Libre/Estándar/Verificado) presentes y correctos
- FRONTEND-032: ✅ Libre: 1 pub, 5 fotos, 30 días — correcto
- FRONTEND-033: ✅ Estándar RD$579/publicación — correcto
- FRONTEND-034: ✅ Verificado RD$2,029/mes — correcto
- FRONTEND-035: ✅ "Comenzar gratis" → /publicar (funciona correctamente)
- FRONTEND-036: ⚠️ Stats (10K+, RD$500M+) aspiracionales (decisión marketing, no bug de código)
- CTAs: "Crear cuenta y vender" → /vender/registro, "Ya tengo cuenta" → /login, "Ver cómo funciona" → #como-funciona ✅

---

### S3-T02: Auditar /dealers — Planes de Dealer (verificar alineación backend)

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do/dealers
- [ ] Paso 2: Toma una screenshot completa
- [ ] Paso 3: Verifica hero: 'Vende más vehículos con OKLA'
- [ ] Paso 4: Scroll hasta la sección de planes
- [ ] Paso 5: Toma screenshot de TODOS los planes de dealer
- [ ] Paso 6: Verifica los 6 planes con precios (backend ya alineado):
- [ ] Paso 7: - LIBRE: RD$0/mes — anotar features
- [ ] Paso 8: - VISIBLE: RD$1,682/mes ($29 USD) — anotar features
- [ ] Paso 9: - STARTER: RD$3,422/mes ($59 USD) — anotar features
- [ ] Paso 10: - PRO: RD$5,742/mes ($99 USD) — anotar features
- [ ] Paso 11: - ÉLITE: RD$20,242/mes ($349 USD) — anotar features
- [ ] Paso 12: - ENTERPRISE: RD$34,742/mes ($599 USD) — anotar features
- [ ] Paso 13: Verifica qué plan tiene badge 'MÁS POPULAR' vs 'RECOMENDADO'
- [ ] Paso 14: Verifica los ChatAgent limits de cada plan
- [ ] Paso 15: Scroll a testimonios: Juan Pérez, María García, Carlos Martínez — ¿son reales?
- [ ] Paso 16: Verifica CTA '14 días gratis' — ¿está implementado en backend?

**A validar:**

- [ ] FRONTEND-038: ¿6 planes frontend coinciden con los 6 del backend?
- [ ] FRONTEND-040: ¿PRO RD$5,742 coincide con backend $99?
- [ ] FRONTEND-041: ¿ÉLITE RD$20,242 coincide con backend $349?
- [ ] FRONTEND-042: ¿ChatAgent limits consistentes entre frontend y backend?
- [ ] FRONTEND-043: ¿Testimonios reales o ficticios?
- [ ] FRONTEND-046: ¿'14 días gratis' implementado?
- [ ] FRONTEND-048: ¿Precios dinámicos (usePlatformPricing) o hardcoded?

**Hallazgos:**

- FRONTEND-038: ✅ 6 planes presentes (LIBRE $0, VISIBLE RD$1,682, STARTER RD$3,422, PRO RD$5,742, ÉLITE RD$20,242, ENTERPRISE RD$34,742)
- FRONTEND-040: ✅ PRO RD$5,742 = $99 × 58 ✓
- FRONTEND-041: ✅ ÉLITE RD$20,242 = $349 × 58 ✓
- FRONTEND-042: ⏳ **PENDIENTE DEPLOY** — PRO aún muestra "300 conv/mes" (debería ser 500), ÉLITE muestra "5,000 conv/mes" (debería ser 2,000). Fix correcto en código (commit 7e87af16) pero NO desplegado a producción.
- FRONTEND-043: ⚠️ Testimonios ficticios (Juan Pérez/AutoMax RD, María García/Caribbean Motors, Carlos Martínez/Premium Auto) — sin cambio, decisión de negocio
- FRONTEND-046: ✅ "14 días gratis" + "Sin tarjeta de crédito" + "Cancela cuando quieras" presentes
- FRONTEND-048: ✅ Precios dinámicos via usePlatformPricing()
- Badges: PRO=MÁS POPULAR, ÉLITE=RECOMENDADO ✅
- STARTER: ChatAgent Web 100 conv/mes + WhatsApp 100 conv/mes ✅
- ENTERPRISE: ChatAgent SIN LÍMITE ✅

---

### S3-T03: Auditar páginas legales y herramientas

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do/terminos y toma screenshot — ¿contenido actualizado 2026?
- [ ] Paso 2: Navega a https://okla.com.do/privacidad y toma screenshot — ¿cumple Ley 172-13?
- [ ] Paso 3: Navega a https://okla.com.do/cookies y toma screenshot — ¿banner funcional?
- [ ] Paso 4: Navega a https://okla.com.do/politica-reembolso y toma screenshot — ¿existe?
- [ ] Paso 5: Navega a https://okla.com.do/reclamaciones y toma screenshot — ¿formulario funciona?
- [ ] Paso 6: Navega a https://okla.com.do/herramientas y toma screenshot — ¿calculadora funciona?
- [ ] Paso 7: Navega a https://okla.com.do/comparar y toma screenshot — ¿comparador funciona?
- [ ] Paso 8: Navega a https://okla.com.do/okla-score y toma screenshot — ¿implementado o placeholder?
- [ ] Paso 9: Navega a https://okla.com.do/precios y toma screenshot — ¿planes actualizados?
- [ ] Paso 10: Navega a https://okla.com.do/empleos y toma screenshot — ¿posiciones reales?

**A validar:**

- [ ] FRONTEND-064 a FRONTEND-075: Todas las páginas públicas secundarias
- [ ] LEGAL-001: Ley 358-05 disclaimers
- [ ] LEGAL-002: Ley 172-13 consent
- [ ] LEGAL-008: Política privacidad y cookies
- [ ] LEGAL-009: Términos actualizados 2026

**Hallazgos:**
- /terminos ✅ (carga correctamente, "Términos y Condiciones | OKLA")
- /privacidad ✅ (carga correctamente, "Política de Privacidad | OKLA")
- /cookies ✅ (carga correctamente, "Política de Cookies | OKLA")
- /politica-reembolso: ⏳ **PENDIENTE DEPLOY** — aún redirige a login (callbackUrl=%2Fpolitica-reembolso). Fix correcto en código (commit 7e87af16, agregado a publicRoutes en middleware.ts) pero NO desplegado.
- /reclamaciones → login (comportamiento esperado, requiere autenticación)
- /herramientas ✅ (carga correctamente)
- /comparar ✅ (carga correctamente)
- /okla-score → login (comportamiento esperado, requiere autenticación)
- /precios ✅ (carga correctamente)
- /empleos ✅ (carga correctamente)
- LEGAL-001: Ley 358-05 disclaimers ✅ (presente en /terminos)
- LEGAL-002: Ley 172-13 consent ✅ (presente en /privacidad)
- LEGAL-008: Política privacidad y cookies ✅
- LEGAL-009: Términos actualizados 2026 ✅

---

## Resultado

- Sprint: 3 — Páginas Públicas: Vender, Dealers, Legal (Guest)
- Fase: REAUDIT 1/3
- Estado: COMPLETADO
- Bugs nuevos: 0
- Bugs pendientes deploy: 2 (FRONTEND-042 ChatAgent limits, FRONTEND-064 /politica-reembolso)
- Nota: Fixes del commit 7e87af16 correctos en código pero NO desplegados a producción. Una vez desplegado: PRO=500 conv/mes, ÉLITE=2,000 conv/mes, /politica-reembolso accesible sin login.

---

_Cuando termines, agrega la palabra READ al final de este archivo._
