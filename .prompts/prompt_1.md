# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 2: Búsqueda & Filtros de Vehículos (Guest)

**Fecha:** 2026-03-25 10:21:52
**Fase:** REAUDIT
**Usuario:** Guest (sin login)
**URL:** https://okla.com.do

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 2 (intento 1/3).
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

- [ ] Paso 1: Abre Chrome y navega a https://okla.com.do/vehiculos
- [ ] Paso 2: Toma una screenshot de la página completa
- [ ] Paso 3: Verifica que dice '149 vehículos encontrados' (o el conteo actual)
- [ ] Paso 4: Verifica la trust bar: 'Vendedores verificados · +2,400 vehículos activos'
- [ ] Paso 5: Verifica que los filtros laterales existen: Condición (Nuevo/Usado), Marca, Modelo, Precio, Año, Carrocería, Ubicación
- [ ] Paso 6: Haz clic en el filtro de precio '< 1M' y toma screenshot de los resultados
- [ ] Paso 7: Verifica que los resultados se actualizan con vehículos bajo RD$1,000,000
- [ ] Paso 8: Limpia los filtros y haz clic en 'SUV' en carrocería
- [ ] Paso 9: Toma screenshot y verifica que solo muestra SUVs
- [ ] Paso 10: Verifica que cada vehicle card muestra: imagen, badge, año, km, combustible, ubicación, precio RD$ + ≈USD

**A validar:**

- [ ] FRONTEND-018: ¿Combustible en inglés en algunos vehículos?
- [ ] FRONTEND-019: ¿Filtros de precio actualizan resultados?
- [ ] FRONTEND-020: ¿Conversión RD$/USD correcta (tasa ≈60.5)?
- [ ] FRONTEND-026: ¿Ordenamiento funciona?
- [ ] FRONTEND-029: ¿Vehicle card muestra '0 km' para nuevos?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

### S2-T02: Auditar paginación y vehículos patrocinados

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do/vehiculos
- [ ] Paso 2: Scroll hasta el final de la primera página de resultados
- [ ] Paso 3: Toma screenshot de la paginación (debe tener ~15 páginas)
- [ ] Paso 4: Haz clic en 'Página 2' y verifica que carga nuevos vehículos manteniendo los filtros
- [ ] Paso 5: Regresa a página 1
- [ ] Paso 6: Busca los bloques de 'Vehículos Patrocinados (Publicidad)' intercalados en los resultados
- [ ] Paso 7: Toma screenshot de un bloque de patrocinados
- [ ] Paso 8: Verifica si los vehículos patrocinados repiten los mismos 3 (RAV4, CR-V, Tucson) — BUG conocido P0-010
- [ ] Paso 9: Verifica que los patrocinados tienen badge visual diferente a los orgánicos

**A validar:**

- [ ] FRONTEND-021: ¿Patrocinados se diferencian visualmente?
- [ ] FRONTEND-024: ¿Paginación mantiene filtros?
- [ ] FRONTEND-025: ¿Patrocinados repiten los mismos 3?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

### S2-T03: Auditar búsqueda y alertas sin auth

**Pasos:**

- [ ] Paso 1: Navega a https://okla.com.do/vehiculos
- [ ] Paso 2: Escribe 'Toyota Corolla' en la barra de búsqueda y presiona Enter
- [ ] Paso 3: Toma screenshot de los resultados filtrados
- [ ] Paso 4: Verifica que muestra solo Toyota Corolla
- [ ] Paso 5: Haz clic en 'Guardar búsqueda' y verifica si pide login o permite guardar anónimamente
- [ ] Paso 6: Haz clic en 'Activar alertas' y verifica si pide login
- [ ] Paso 7: Haz clic en 'Contactar vendedor' en el primer vehículo y verifica si abre modal de login o permite contacto anónimo

**A validar:**

- [ ] FRONTEND-005: ¿Búsqueda rápida funciona?
- [ ] FRONTEND-022: ¿'Guardar búsqueda' pide login?
- [ ] FRONTEND-023: ¿'Activar alertas' pide login?
- [ ] FRONTEND-030: ¿'Contactar vendedor' sin auth?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

## Resultado

- Sprint: 2 — Búsqueda & Filtros de Vehículos (Guest)
- Fase: REAUDIT
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines, agrega la palabra READ al final de este archivo._
