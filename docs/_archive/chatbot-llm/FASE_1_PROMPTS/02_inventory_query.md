# 🔍 Prompt 02 — Consulta de Inventario (Dual-Mode v2.0)

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 17, 2026  
> **Versión:** 2.0 — Diferenciación SingleVehicle vs DealerInventory

---

## 1. Objetivo

Definir cómo el chatbot gestiona consultas sobre vehículos según el **modo** de conversación:

| Modo                     | Comportamiento                                        | Búsqueda                        |
| ------------------------ | ----------------------------------------------------- | ------------------------------- |
| **SingleVehicle (SV)**   | Responde SOLO sobre 1 vehículo inyectado en el prompt | ❌ No busca inventario          |
| **DealerInventory (DI)** | Busca en inventario del dealer vía RAG (pgvector)     | ✅ Búsqueda semántica + filtros |

---

## 2. SingleVehicle — Consultas de Inventario

### Contexto

En modo SV, el LLM recibe los datos de UN vehículo en el system prompt. No tiene acceso a otros vehículos.

### Flujo de respuesta

```
Usuario pregunta sobre ESTE vehículo
       │
       ├── Datos disponibles en contexto → Responder con datos reales
       │   ├── Precio → "RD${price}* (sujeto a confirmación)"
       │   ├── Especificaciones → Datos del system prompt
       │   └── Disponibilidad → "Para confirmar disponibilidad, contacta al dealer"
       │
       └── Datos NO disponibles en contexto → "No tengo esa información"
           ├── Historial mecánico → "Te recomiendo preguntar directamente al dealer"
           ├── VIN → "Esa información está disponible con el equipo de ventas"
           └── Garantía extendida → "El dealer te puede dar detalles"
```

### Boundary: Cuando preguntan por OTRO vehículo

```
Usuario: "¿Tienen un Honda Civic?"
Bot:     intent=VehicleNotInInventory

Respuesta tipo:
"Solo puedo ayudarte con el {make} {model} {year} que estás viendo.
Para explorar más opciones, te invito a visitar el perfil de {dealerName}
donde podrás ver todo su inventario. 🚗"

quickReplies: ["Ver más del {model}", "Perfil del dealer", "Gracias"]
```

### ⚠️ Lo que SV NUNCA debe hacer

| Prohibido                            | Por qué                         |
| ------------------------------------ | ------------------------------- |
| Inventar otros vehículos             | Solo conoce 1                   |
| Sugerir modelos alternativos         | No tiene acceso a inventario    |
| Comparar con otros vehículos         | No tiene datos para comparar    |
| Decir "no tenemos X pero tenemos Y"  | No sabe qué más tiene el dealer |
| Mencionar precios de otros vehículos | No tiene esa información        |

---

## 3. DealerInventory — Consultas de Inventario

### Contexto

En modo DI, el LLM tiene acceso a los resultados de RAG (pgvector) y puede invocar `search_inventory` como function call.

### Flujo de búsqueda

```
Usuario: "Busco una yipeta automática de menos de 2 millones"
         │
         ▼
┌─────────────────────────┐
│  1. Interpretar query   │ ← Slang mapping + NLU
│     yipeta → SUV        │
│     2 millones → ≤2M    │
│     automática → Auto   │
└────────┬────────────────┘
         ▼
┌─────────────────────────┐
│  2. Buscar inventario   │ ← search_inventory function call
│     body_type=SUV       │
│     max_price=2,300,000 │   (±15% buffer for slang)
│     transmission=Auto   │
└────────┬────────────────┘
         ▼
┌─────────────────────────┐
│  3. Presentar (max 3-4) │ ← Formateado con emojis
│     Precio + destaque   │
│     Quick replies       │
└─────────────────────────┘
```

### 3.1 — Mapeo de Slang Dominicano (NLU)

El chatbot debe interpretar expresiones coloquiales dominicanas para convertirlas en filtros de búsqueda:

#### Tipos de vehículo

| Expresión usuario   | Filtro aplicado                | Confianza |
| ------------------- | ------------------------------ | --------- |
| "yipeta"            | `bodyType: SUV`                | Alta      |
| "guagua"            | `bodyType: Van, Minivan`       | Alta      |
| "carro"             | _(genérico — no filtrar tipo)_ | —         |
| "camioneta"         | `bodyType: Pickup, Truck`      | Alta      |
| "maquinón"          | `segment: Luxury`              | Media     |
| "carrito económico" | `maxPrice: 800000`             | Media     |

#### Precios coloquiales

| Expresión          | Interpretación                     | Rango aplicado (±15%)       |
| ------------------ | ---------------------------------- | --------------------------- |
| "como 2 palos"     | ~RD$2,000,000                      | RD$1,700,000 – RD$2,300,000 |
| "medio palo"       | ~RD$500,000                        | RD$425,000 – RD$575,000     |
| "un palo"          | ~RD$1,000,000                      | RD$850,000 – RD$1,150,000   |
| "3 millones"       | RD$3,000,000                       | RD$2,550,000 – RD$3,450,000 |
| "barato"           | Sin precio máximo, ordenar ASC     | —                           |
| "caro" / "premium" | Sin precio mínimo, segmento luxury | —                           |
| "como 500 mil"     | ~RD$500,000                        | RD$425,000 – RD$575,000     |

> **Regla del ±15%:** Cuando el usuario usa expresiones imprecisas ("como", "alrededor de"), aplicar un buffer de ±15% en el rango de búsqueda. Para valores exactos ("exactamente 2 millones"), usar ±5%.

#### Características

| Expresión              | Filtro                                       |
| ---------------------- | -------------------------------------------- |
| "full extras" / "full" | `features: [leather, sunroof, camera, etc.]` |
| "prende rápido"        | `engineType: V6+` o `horsepower: >=200`      |
| "no gasta" / "rinde"   | `fuelEfficiency: High` o `fuelType: Hybrid`  |
| "poca milla"           | `maxMileage: 30000`                          |
| "de paquete" / "nueva" | `condition: New`                             |
| "usadita"              | `condition: Used, maxMileage: 50000`         |
| "para mudar"           | `bodyType: Pickup, Van, capacity: Large`     |

### 3.2 — Formato de Presentación de Resultados

#### Con resultados (1-4 vehículos)

```
🚗 **{year} {make} {model} {trim}** — RD${price:N0}*
   {fuelType} | {transmission} | {mileage:N0} km

🚗 **{year} {make} {model} {trim}** — RD${price:N0}*
   {fuelType} | {transmission} | {mileage:N0} km

*Precios sujetos a confirmación.

¿Quieres más detalles de alguno o comparamos?
```

**Reglas de presentación:**

- Máximo **3-4 vehículos** por respuesta
- Si hay más, indicar: "Tenemos {n} opciones más. ¿Quieres que filtremos?"
- Ordenar por **relevancia semántica** (RAG score)
- Destacar **oferta** si `saleTag` presente: 🏷️

#### Sin resultados

```
No encontré vehículos que coincidan exactamente con lo que buscas. 😕

Pero tenemos opciones similares:
🚗 **{sugerencia_1}** — RD${price}*
🚗 **{sugerencia_2}** — RD${price}*

¿Ajustamos la búsqueda? Puedo filtrar por precio, tipo o marca.
```

**Regla de fallback:** Si RAG retorna 0 resultados con los filtros exactos, relajar filtros (quitar 1 restricción) y reintentar. Nunca decir simplemente "no tenemos nada".

### 3.3 — Refinamiento Iterativo

El usuario puede refinar su búsqueda en múltiples turnos:

```
Turno 1: "Busco yipeta automática"
         → search_inventory(bodyType=SUV, transmission=Auto)
         → Muestra 3 resultados

Turno 2: "Algo más nuevo, de este año"
         → search_inventory(bodyType=SUV, transmission=Auto, minYear=2024)
         → Refina resultados

Turno 3: "En blanco o gris"
         → search_inventory(...prev, color=[White, Gray])
         → Filtra más

Turno 4: "¿Cuánto cuesta la primera?"
         → get_vehicle_details(vehicleId=xxx)
         → Muestra detalles
```

El LLM debe **acumular** filtros del contexto conversacional, no empezar de cero en cada turno.

### 3.4 — Boundary: Cross-Dealer

```
Usuario: "En Caribe Motors vi una igual más barata"
Bot:     intent=CrossDealerRefusal

Respuesta tipo:
"Entiendo que estés comparando, es lo mejor! 👍 Solo puedo ayudarte
con el inventario de {dealerName}. Nuestros vehículos incluyen
revisión mecánica certificada y documentación al día.

¿Te gustaría una cotización personalizada? Puedo conectarte
con un asesor. 🤝"

quickReplies: ["Cotización", "Hablar con asesor", "Seguir viendo"]
```

---

## 4. Comparación Rápida: SV vs DI

| Aspecto                         | SingleVehicle                      | DealerInventory                          |
| ------------------------------- | ---------------------------------- | ---------------------------------------- |
| **Datos disponibles**           | 1 vehículo (system prompt)         | RAG top-5 + search                       |
| **Puede buscar**                | ❌ No                              | ✅ Sí (function call)                    |
| **Puede comparar**              | ❌ No                              | ✅ Sí (2-3, mismo dealer)                |
| **Slang mapping**               | Solo para entender, no para buscar | Para convertir en filtros                |
| **Boundary ante otro vehículo** | "Solo puedo hablar de este"        | "No lo tenemos / estas son opciones"     |
| **Boundary ante otro dealer**   | N/A (no aplica)                    | "Solo manejo inventario de {dealerName}" |
| **Refinamiento multi-turno**    | Sobre el MISMO vehículo            | Filtros acumulativos                     |

---

## 5. Validaciones de Seguridad

Antes de presentar cualquier resultado:

| Check                  | Acción                                                      |
| ---------------------- | ----------------------------------------------------------- |
| Precio > 0             | Si precio = 0 o null, omitir precio y decir "consultar"     |
| Vehículo existe en DB  | No mostrar vehículos eliminados o inactivos                 |
| Pertenece al dealer    | No filtrar resultados de OTROS dealers                      |
| Datos PII              | Nunca mostrar datos del vendedor individual en la respuesta |
| Precios con disclaimer | Siempre agregar "sujeto a confirmación"                     |

---

_Documento actualizado para arquitectura Dual-Mode v2.0 — Febrero 2026_
