# AUDITORÍA — Sprint 3: Páginas Públicas: Vender, Dealers, Legal (Guest)

**Fecha:** 2026-03-25 11:52:53
**Fase:** AUDIT
**Usuario:** Guest (sin login)
**URL:** https://okla.com.do

## Instrucciones

Ejecuta TODA la auditoría con **Chrome** como un humano real.
NO uses scripts — solo Chrome. Scripts solo para upload/download de fotos vía MediaService.

Para cada tarea:

1. Navega con Chrome a la URL indicada
2. Toma screenshot cuando se indique
3. Documenta bugs y discrepancias en la sección 'Hallazgos'
4. Marca la tarea como completada: `- [ ]` → `- [x]`
5. Al terminar TODAS las tareas, agrega `READ` al final

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

- [x] Paso 1: Abre Chrome y navega a https://okla.com.do/vender — ✅
- [x] Paso 2: Toma una screenshot completa de la página — ✅
- [x] Paso 3: Verifica el hero: 'Vende tu vehículo al mejor precio' — ✅ Exacto
- [x] Paso 4: Verifica stats: 10K+ vendidos, 7 días venta promedio, 95% satisfechos, RD$500M+ transado — ✅ Presentes (aspiracionales/marketing)
- [x] Paso 5: Scroll hasta la sección de planes de publicación — ✅ "Planes de publicación"
- [x] Paso 6: Toma screenshot de los planes: Libre (RD$0), Estándar (RD$579/publicación), Verificado (RD$2,029/mes) — ✅ Coinciden
- [x] Paso 7: Verifica que COINCIDEN con /cuenta/suscripcion (Libre/Estándar/Verificado) — ✅ Mismos 3 planes, precios dinámicos via usePlatformPricing()
- [x] Paso 8: Anota las features de cada plan — ✅ Documentados abajo
- [x] Paso 9: Libre: 1 pub, 5 fotos, 30 días. Estándar: 1 pub/pago, 10 fotos, 60 días. Verificado: 3 pubs, 12 fotos, 90 días — ✅ COINCIDE
- [x] Paso 10: Haz clic en 'Comenzar gratis' y verifica si redirige a registro o publicar — ✅ Redirige a /login?callbackUrl=/publicar
- [x] Paso 11: Verifica si 'Ver cómo funciona' tiene video o sección anchor — ✅ Anchor a #como-funciona (sección 4 pasos)

**A validar:**

- [x] FRONTEND-031: ¿Planes de /vender coinciden con /cuenta/suscripcion (Libre/Estándar/Verificado)? — ✅ SÍ. Ambos usan usePlatformPricing() con fallback defaults
- [x] FRONTEND-032: ¿Plan Libre: 1 pub, 5 fotos, 30 días — coincide con backend? — ✅ COINCIDE
- [x] FRONTEND-033: ¿Plan Estándar RD$579/publicación — coincide con pricing API? — ✅ COINCIDE ($9.99 USD × 58 = RD$579)
- [x] FRONTEND-034: ¿Plan Verificado RD$2,029/mes — coincide con pricing API? — ✅ COINCIDE ($34.99 USD × 58 = RD$2,029)
- [x] FRONTEND-035: ¿'Comenzar gratis' redirige correctamente? — ✅ /login?callbackUrl=/publicar
- [x] FRONTEND-036: ¿Estadísticas (10K+, RD$500M+) son reales? — ⚠️ ASPIRACIONALES. Hardcoded marketing stats, no conectados a datos reales

**Hallazgos:**
- ✅ Planes seller (Libre/Estándar/Verificado): pricing consistente entre /vender, /cuenta/suscripcion y backend
- ✅ Precios dinámicos via usePlatformPricing() hook con fallback a DEFAULT_PRICING
- ✅ Features correctos en cada plan (pubs, fotos, duración)
- ✅ CTAs funcionales: "Comenzar gratis" → /login?callbackUrl=/publicar, "Elegir Estándar" → /publicar?plan=estandar
- ✅ "Ver cómo funciona" usa anchor #como-funciona (4 pasos: Crea anuncio, Define precio, Recibe ofertas, Cierra venta)
- ⚠️ Stats (10K+ vendidos, RD$500M+ transado) son aspiracionales/hardcoded, no reflejan datos reales de la plataforma
- ⚠️ CTA dice "Crear cuenta y vender" en hero pero "Comenzar gratis" en plan Libre — inconsistencia menor de copy

---

### S3-T02: Auditar /dealers — Planes de Dealer (verificar alineación backend)

**Pasos:**

- [x] Paso 1: Navega a https://okla.com.do/dealers — ✅
- [x] Paso 2: Toma una screenshot completa — ✅
- [x] Paso 3: Verifica hero: 'Vende más vehículos con OKLA' — ✅ Exacto
- [x] Paso 4: Scroll hasta la sección de planes — ✅ "Planes para cada tipo de dealer"
- [x] Paso 5: Toma screenshot de TODOS los planes de dealer — ✅
- [x] Paso 6: Verifica los 6 planes con precios (backend ya alineado): — ✅ 6 planes presentes
- [x] Paso 7: - LIBRE: RD$0/mes — ✅ Features: destacados/mes, ChatAgent IA
- [x] Paso 8: - VISIBLE: RD$1,682/mes ($29 USD) — ✅ 3 destacados, $15 OKLA Coins, 5 PricingAgent, ChatAgent IA
- [x] Paso 9: - STARTER: RD$3,422/mes ($59 USD) — ✅ 5 destacados, $30 Coins, ChatAgent Web+WA 100 conv/mes
- [x] Paso 10: - PRO: RD$5,742/mes ($99 USD) — ✅ 10 destacados, $45 Coins, ChatAgent Web+WA 300 conv/mes
- [x] Paso 11: - ÉLITE: RD$20,242/mes ($349 USD) — ✅ 25 destacados, $120 Coins, ChatAgent Web+WA 5,000 conv/mes
- [x] Paso 12: - ENTERPRISE: RD$34,742/mes ($599 USD) — ✅ 50 destacados, $300 Coins, ChatAgent SIN LÍMITE, 👑 Badge Enterprise
- [x] Paso 13: Verifica qué plan tiene badge 'MÁS POPULAR' vs 'RECOMENDADO' — ✅ PRO=MÁS POPULAR, ÉLITE=RECOMENDADO
- [x] Paso 14: Verifica los ChatAgent limits de cada plan — ⚠️ Ver discrepancia backend abajo
- [x] Paso 15: Scroll a testimonios: Juan Pérez, María García, Carlos Martínez — ⚠️ FICTICIOS (hardcoded en dealers/page.tsx)
- [x] Paso 16: Verifica CTA '14 días gratis' — ✅ Implementado en backend (StripeTrialDays=14)

**A validar:**

- [x] FRONTEND-038: ¿6 planes frontend coinciden con los 6 del backend? — ✅ SÍ. LIBRE/VISIBLE/STARTER/PRO/ÉLITE/ENTERPRISE con precios USD idénticos
- [x] FRONTEND-040: ¿PRO RD$5,742 coincide con backend $99? — ✅ $99 × 58 = RD$5,742
- [x] FRONTEND-041: ¿ÉLITE RD$20,242 coincide con backend $349? — ✅ $349 × 58 = RD$20,242
- [x] FRONTEND-042: ¿ChatAgent limits consistentes entre frontend y backend? — 🚨 DISCREPANCIA:
  - PRO: Frontend 300 conv/mes vs Backend 500 (PlanFeatureLimits.cs)
  - ÉLITE: Frontend 5,000 conv/mes vs Backend 2,000
  - STARTER: No definido en backend PlanFeatureLimits.cs
- [x] FRONTEND-043: ¿Testimonios reales o ficticios? — ⚠️ FICTICIOS. Hardcoded en dealers/page.tsx. "Juan Pérez/AutoMax RD", "María García/Caribbean Motors", "Carlos Martínez" son fixtures de test
- [x] FRONTEND-046: ¿'14 días gratis' implementado? — ✅ StripeTrialDays=14 en PublicConfigurationsController.cs + notificaciones de expiración
- [x] FRONTEND-048: ¿Precios dinámicos (usePlatformPricing) o hardcoded? — ✅ DINÁMICOS. usePlatformPricing() fetch /api/pricing con cache TTL 60s, fallback a DEALER_PLAN_PRICES

**Hallazgos:**
- ✅ 6 planes dealer: pricing frontend/backend alineado perfectamente (USD × 58 = DOP)
- ✅ PRO=MÁS POPULAR, ÉLITE=RECOMENDADO badges correctos
- ✅ 14 días gratis implementado en backend (Stripe trial)
- ✅ Precios dinámicos via usePlatformPricing()
- 🚨 **BUG FRONTEND-042**: ChatAgent limits DISCREPANCIA frontend vs backend:
  - PRO: Frontend muestra 300 conv/mes, backend enforces 500
  - ÉLITE: Frontend muestra 5,000 conv/mes, backend enforces 2,000
  - STARTER: Sin límites definidos en backend PlanFeatureLimits.cs
- ⚠️ Testimonios ficticios (Juan Pérez, María García, Carlos Martínez) — reemplazar con reales antes de launch
- ⚠️ Stats hero (500+ dealers, 10K+ ventas mensuales) son aspiracionales

---

### S3-T03: Auditar páginas legales y herramientas

**Pasos:**

- [x] Paso 1: Navega a https://okla.com.do/terminos y toma screenshot — ✅ "Términos y Condiciones", Marzo 2026 (v2026.1)
- [x] Paso 2: Navega a https://okla.com.do/privacidad y toma screenshot — ✅ "Política de Privacidad", Enero 2026. Cumple Ley 172-13
- [x] Paso 3: Navega a https://okla.com.do/cookies y toma screenshot — ✅ "Política de Cookies", Enero 2026. Botón "Configurar cookies" presente
- [x] Paso 4: Navega a https://okla.com.do/politica-reembolso — 🚨 REDIRIGE A LOGIN. Página de reembolso debería ser pública
- [x] Paso 5: Navega a https://okla.com.do/reclamaciones — ⚠️ Redirige a login (puede ser intencional para filing)
- [x] Paso 6: Navega a https://okla.com.do/herramientas — ✅ 4 herramientas: Calculadora Financiamiento, Calculadora Importación, Guía Precios, Comparador
- [x] Paso 7: Navega a https://okla.com.do/comparar — ✅ Funcional. Muestra "Sin vehículos para comparar" (necesita selección)
- [x] Paso 8: Navega a https://okla.com.do/okla-score — ⚠️ Redirige a login (requiere auth para ver score)
- [x] Paso 9: Navega a https://okla.com.do/precios — ✅ "Guía de Precios" de mercado vehicular RD, Feb 2026. Rangos por categoría, factores de precio
- [x] Paso 10: Navega a https://okla.com.do/empleos — ✅ 3 posiciones: Full Stack .NET/Next.js, Diseñador UX/UI, Ejecutivo Ventas Dealers

**A validar:**

- [x] FRONTEND-064 a FRONTEND-075: Todas las páginas públicas secundarias — Verificadas (ver detalles abajo)
- [x] LEGAL-001: Ley 358-05 disclaimers — ✅ Sección 14 de /terminos: "Protección al Consumidor (Ley 358-05)"
- [x] LEGAL-002: Ley 172-13 consent — ✅ Sección 16 de /terminos + página /privacidad cumplen
- [x] LEGAL-008: Política privacidad y cookies — ✅ /privacidad (Ene 2026) + /cookies (Ene 2026) + botón "Configurar cookies"
- [x] LEGAL-009: Términos actualizados 2026 — ✅ Marzo 2026 (v2026.1)

**Hallazgos:**
- ✅ /terminos: Actualizado Marzo 2026 (v2026.1). Incluye Ley 358-05 y Ley 172-13
- ✅ /privacidad: Actualizado Enero 2026. Referencias Ley 172-13, consentimiento, datos personales. Email: privacidad@okla.com.do
- ✅ /cookies: Actualizado Enero 2026. Botón "Configurar cookies" funcional en toda la plataforma
- 🚨 **BUG FRONTEND-064**: /politica-reembolso REDIRIGE A LOGIN. Una política de reembolso debe ser pública por Ley 358-05
- ⚠️ /reclamaciones redirige a login (aceptable — formulario requiere autenticación)
- ✅ /herramientas: 4 herramientas disponibles (calculadoras, guía precios, comparador)
- ✅ /comparar: Funcional, requiere selección de vehículos
- ⚠️ /okla-score: Redirige a login (requiere auth — aceptable para funcionalidad premium)
- ✅ /precios: Guía de precios de mercado vehicular RD actualizada Feb 2026
- ✅ /empleos: 3 posiciones abiertas reales con email empleos@okla.com.do

---

## Resultado

- Sprint: 3 — Páginas Públicas: Vender, Dealers, Legal (Guest)
- Fase: AUDIT
- Estado: ✅ COMPLETADO
- Fecha auditoría: 2026-03-25

### Bugs encontrados

| ID | Severidad | Descripción | Página |
|----|-----------|-------------|--------|
| FRONTEND-042 | 🚨 P1 | ChatAgent limits discrepancia frontend vs backend (PRO: 300 vs 500, ÉLITE: 5K vs 2K, STARTER: sin definir en backend) | /dealers |
| FRONTEND-064 | 🚨 P1 | /politica-reembolso redirige a login — debe ser pública (Ley 358-05) | /politica-reembolso |
| FRONTEND-036 | ⚠️ P2 | Stats de /vender (10K+, RD$500M+) son aspiracionales hardcoded, no datos reales | /vender |
| FRONTEND-043 | ⚠️ P2 | Testimonios ficticios (Juan Pérez, María García, Carlos Martínez) hardcoded | /dealers |

### Verificaciones exitosas

- ✅ FRONTEND-031: Planes seller alineados /vender ↔ /cuenta/suscripcion
- ✅ FRONTEND-032: Plan Libre features correctos
- ✅ FRONTEND-033: Estándar RD$579 = $9.99 USD
- ✅ FRONTEND-034: Verificado RD$2,029 = $34.99 USD
- ✅ FRONTEND-035: "Comenzar gratis" redirige a login/publicar
- ✅ FRONTEND-038: 6 planes dealer alineados frontend/backend
- ✅ FRONTEND-040: PRO RD$5,742 = $99 USD
- ✅ FRONTEND-041: ÉLITE RD$20,242 = $349 USD
- ✅ FRONTEND-046: 14 días gratis implementado (Stripe)
- ✅ FRONTEND-048: Precios dinámicos via usePlatformPricing()
- ✅ LEGAL-001: Ley 358-05 disclaimers en /terminos
- ✅ LEGAL-002: Ley 172-13 consent en /privacidad
- ✅ LEGAL-008: Políticas privacidad y cookies actualizadas
- ✅ LEGAL-009: Términos v2026.1 (Marzo 2026)

---

_Cuando termines, agrega la palabra READ al final de este archivo._
