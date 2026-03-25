# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 1: Homepage & Navegación Pública (Guest)

**Fecha:** 2026-03-25 05:59:48
**Fase:** REAUDIT
**Usuario:** Guest (sin login)
**URL:** https://okla.com.do

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 1 (intento 1/3).
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

### S1-T01: Auditar Homepage completa

**Pasos:**

- [ ] Paso 1: Abre Chrome y navega a https://okla.com.do
- [ ] Paso 2: Toma una screenshot de la página actual y dime qué ves
- [ ] Paso 3: Verifica que el Hero dice 'Tu próximo vehículo está en OKLA'
- [ ] Paso 4: Verifica que la barra de búsqueda tiene placeholder 'Busca tu vehículo ideal'
- [ ] Paso 5: Verifica que aparecen las categorías rápidas: SUV, Sedán, Camioneta, Deportivo, Híbrido, Eléctrico
- [ ] Paso 6: Verifica los trust badges: Vendedores Verificados, Historial Garantizado, Precios Transparentes
- [ ] Paso 7: Verifica las estadísticas: 10,000+ Vehículos, 50,000+ Usuarios, 500+ Dealers, 95% Satisfacción
- [ ] Paso 8: Scroll hacia abajo y toma una screenshot de la sección de vehículos destacados
- [ ] Paso 9: Verifica que los vehículos destacados tienen el tag 'Publicidad'
- [ ] Paso 10: Busca si hay un vehículo E2E de prueba visible (Toyota Corolla 2022 — E2E mm8mioxc) — si lo ves, reporta como BUG CRÍTICO

**A validar:**

- [ ] FRONTEND-001: ¿Las imágenes de vehículos cargan (no 403 S3)?
- [ ] FRONTEND-002: ¿Los precios muestran formato RD$ con separadores de miles?
- [ ] FRONTEND-003: ¿El carrusel funciona (swipe/arrows)?
- [ ] FRONTEND-008: ¿Vehículo E2E test visible? → Debe ocultarse
- [ ] FRONTEND-015: ¿Las estadísticas son reales o hardcoded?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

### S1-T02: Auditar Navbar y Footer

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do
- [ ] Paso 2: Toma una screenshot del navbar y verifica que contiene: Inicio, Comprar, Vender, Dealers, ¿Por qué OKLA?, Ingresar, Registrarse
- [ ] Paso 3: Scroll hasta el final de la página y toma screenshot del footer
- [ ] Paso 4: Haz clic en cada link del footer y verifica que NO da 404. Links esperados: Marketplace, Compañía, Legal, Soporte, Configurar cookies
- [ ] Paso 5: Verifica que aparece el disclaimer legal: Ley 358-05, ITBIS, Pro-Consumidor, INDOTEL

**A validar:**

- [ ] FRONTEND-004: ¿Los links del footer apuntan a páginas reales?
- [ ] FRONTEND-010: ¿El disclaimer de Ley 358-05 es legalmente completo?
- [ ] FRONTEND-014: ¿SEO: meta title, description, og:image configurados?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

### S1-T03: Auditar sección de Concesionarios y Carruseles

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do
- [ ] Paso 2: Scroll hasta la sección 'Concesionarios en OKLA' y toma screenshot
- [ ] Paso 3: Verifica que muestra dealers verificados con su conteo de inventario
- [ ] Paso 4: Haz clic en 'Ver inventario' del primer dealer y verifica que lleva a su página real
- [ ] Paso 5: Regresa a https://okla.com.do
- [ ] Paso 6: Scroll hasta la sección 'SUVs — Los más solicitados' y toma screenshot
- [ ] Paso 7: Scroll hasta 'Sedanes — Comodidad y eficiencia' y verifica si el Maserati Ghibli aparece duplicado (BUG conocido)
- [ ] Paso 8: Verifica que el tipo de combustible dice 'Gasolina' y NO 'gasoline' en inglés
- [ ] Paso 9: Verifica que la ubicación dice 'Santo Domingo Norte' (con espacio) y NO 'Santo DomingoNorte'

**A validar:**

- [ ] FRONTEND-009: ¿Vehículos duplicados en carruseles?
- [ ] FRONTEND-011: ¿Los dealers muestran conteo real de vehículos?
- [ ] FRONTEND-012: ¿'Ver inventario' lleva a página real?
- [ ] FRONTEND-016: ¿'Santo DomingoNorte' vs 'Santo Domingo Norte'?
- [ ] FRONTEND-017: ¿Combustible en inglés 'gasoline' vs 'Gasolina'?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

### S1-T04: Auditar responsive mobile

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do
- [ ] Paso 2: Redimensiona el browser a 375px de ancho (mobile)
- [ ] Paso 3: Toma una screenshot y verifica que el hero, búsqueda y categorías se ven bien en mobile
- [ ] Paso 4: Verifica que los carruseles son scrolleables en mobile
- [ ] Paso 5: Verifica que el navbar se convierte en hamburger menu
- [ ] Paso 6: Redimensiona a 768px (tablet) y toma otra screenshot
- [ ] Paso 7: Redimensiona de vuelta a 1920px (desktop)

**A validar:**

- [ ] FRONTEND-013: ¿Responsive: hero, carruseles, grid funcionan en mobile (375px)?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

## Resultado

- Sprint: 1 — Homepage & Navegación Pública (Guest)
- Fase: REAUDIT
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines, agrega la palabra READ al final de este archivo._
