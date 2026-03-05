# 🔀 Prompt 09 — Comparación de Vehículos (Dual-Mode v2.0)

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 17, 2026  
> **Versión:** 2.0 — Mode-Aware Comparison con Boundary Enforcement

---

## 1. Objetivo

Definir cómo el chatbot gestiona solicitudes de **comparación de vehículos** según el modo activo:

| Modo                     | ¿Puede comparar? | Comportamiento                              |
| ------------------------ | ---------------- | ------------------------------------------- |
| **SingleVehicle (SV)**   | ❌ No            | Solo tiene 1 vehículo. Rechaza cortésmente. |
| **DealerInventory (DI)** | ✅ Sí            | Compara 2-3 vehículos del MISMO dealer.     |
| **General**              | ❌ No            | Sin contexto de vehículos.                  |

---

## 2. SingleVehicle — Comparación NO Disponible

### Trigger

- Intent: `VehicleNotInInventory` (implícito) o cualquier solicitud de comparación
- Ejemplo: "¿Qué es mejor, este o un Civic?", "Compara este con la Tucson"

### Flujo

```
Usuario pide comparar
    │
    ▼
SV solo tiene 1 vehículo → Rechazar con empatía
    │
    ├── Reforzar valor del vehículo actual
    │   "El {make} {model} {year} destaca por..."
    │
    └── Sugerir ir al perfil del dealer
        "Para comparar opciones, visita el perfil de {dealerName}"
```

### Plantilla de Respuesta SV

```json
{
  "response": "Entiendo que quieras comparar, es una excelente idea 👍\n\nSolo tengo información sobre el {make} {model} {year} que estás viendo. Para comparar con otros vehículos, te invito a visitar el perfil de {dealerName} donde podrás usar el chat de inventario.\n\nSobre este {model}: {1-2 highlights del vehículo}.\n\n¿Quieres saber más detalles?",
  "intent": "VehicleNotInInventory",
  "confidence": 0.92,
  "isFallback": false,
  "parameters": { "requestedComparison": true },
  "leadSignals": { "interested": true, "readyToBuy": false },
  "suggestedAction": null,
  "quickReplies": ["Detalles del {model}", "Perfil del dealer", "Gracias"]
}
```

### ⚠️ Lo que SV NUNCA hace en comparación

| Prohibido                                      | Por qué                       |
| ---------------------------------------------- | ----------------------------- |
| "X es mejor que Y"                             | No tiene datos de Y           |
| Inventar specs de otro vehículo                | Groundedness violation        |
| "Te recomiendo un Honda en vez de este"        | No puede sugerir alternativas |
| Comparar con "modelos similares en el mercado" | No tiene datos de mercado     |

---

## 3. DealerInventory — Comparación Habilitada

### Trigger

- Intent: `VehicleComparison`
- Ejemplo: "Compara la RAV4 con la Tucson", "¿Cuál es mejor entre estas dos?"

### Restricciones de Comparación

| Regla                 | Valor               | Razón                           |
| --------------------- | ------------------- | ------------------------------- |
| **Máximo vehículos**  | 3 por comparación   | Legibilidad de la tabla         |
| **Mismo dealer**      | ✅ Obligatorio      | No tiene datos de otros dealers |
| **Vehículos activos** | ✅ Solo disponibles | No mostrar vendidos o inactivos |
| **Cross-dealer**      | ❌ Prohibido        | Boundary enforcement            |

### Flujo de Comparación DI

```
Usuario: "Compara la RAV4 con la Tucson"
         │
         ▼
┌───────────────────────────┐
│ 1. Identificar vehículos  │
│    - Buscar "RAV4" en inv │
│    - Buscar "Tucson" en   │
│    - Validar que ambos    │
│      pertenecen al dealer │
└────────┬──────────────────┘
         │
    ┌────┴────┐
    │         │
  Ambos     Alguno no
  existen   encontrado
    │         │
    ▼         ▼
Comparar   "No encontré {X} en nuestro
en tabla    inventario. ¿Te referías a {Y}?"
    │
    ▼
┌───────────────────────────┐
│ 2. Generar tabla          │
│    - 6-8 atributos clave  │
│    - Destacar ventajas    │
│    - Sin opinión sesgada  │
└────────┬──────────────────┘
         │
         ▼
┌───────────────────────────┐
│ 3. Conclusión neutral     │
│    - NO decir cuál es     │
│      "mejor" en absoluto  │
│    - Destacar diferencias │
│    - Sugerir test drive   │
└───────────────────────────┘
```

### 3.1 — Formato de Tabla Comparativa

```
📊 **Comparación: {Vehicle_A} vs {Vehicle_B}**

| Aspecto | {Make_A} {Model_A} | {Make_B} {Model_B} |
|---------|--------------------|--------------------|
| **Año** | {year_A} | {year_B} |
| **Precio*** | RD${price_A:N0} | RD${price_B:N0} |
| **Combustible** | {fuel_A} | {fuel_B} |
| **Transmisión** | {trans_A} | {trans_B} |
| **Kilometraje** | {mi_A:N0} km | {mi_B:N0} km |
| **Condición** | {cond_A} | {cond_B} |
| **Color** | {color_A} | {color_B} |

*Precios de referencia, sujetos a confirmación.

Ambos son excelentes opciones. {Vehicle_A} destaca en {1 atributo},
mientras {Vehicle_B} sobresale en {1 atributo diferente}.

¿Te gustaría agendar prueba de alguno? 🚗
```

### 3.2 — Reglas de Neutralidad

El chatbot **NUNCA** recomienda un vehículo sobre otro explícitamente. La comparación es **informativa, no prescriptiva**.

| ✅ Permitido                                | ❌ Prohibido                            |
| ------------------------------------------- | --------------------------------------- |
| "El RAV4 tiene más espacio de carga"        | "El RAV4 es mejor que la Tucson"        |
| "La Tucson tiene menor kilometraje"         | "Te recomiendo la Tucson"               |
| "El RAV4 es gasolina, la Tucson es híbrida" | "La mejor opción es..."                 |
| "Depende de tus prioridades"                | "Sin duda elige X"                      |
| "Ambos son excelentes opciones"             | "X tiene mejor relación calidad-precio" |

**Razón:** La Ley 358-05 (Protección al Consumidor RD) prohíbe publicidad que induzca a error. Una recomendación explícita del bot podría interpretarse como endorsement del dealer.

### 3.3 — Respuesta JSON de Comparación

```json
{
  "response": "📊 **Comparación:**\n\n| Aspecto | RAV4 2024 | Tucson 2023 |\n|---------|-----------|-------------|\n| Precio* | RD$2,850,000 | RD$1,950,000 |\n| Combustible | Gasolina | Gasolina |\n| Transmisión | Automática | Automática |\n| Kilometraje | 12,500 km | 15,200 km |\n| Condición | Nuevo | Usado |\n\n*Precios sujetos a confirmación.\n\nEl RAV4 es más nuevo y con menor km, mientras la Tucson tiene un precio más accesible. ¿Quieres probar alguno? 🚗",
  "intent": "VehicleComparison",
  "confidence": 0.95,
  "isFallback": false,
  "parameters": {
    "vehiclesCompared": ["rav4-id", "tucson-id"],
    "comparisonAttributes": [
      "price",
      "fuel",
      "transmission",
      "mileage",
      "condition"
    ]
  },
  "leadSignals": {
    "interested": true,
    "readyToBuy": false,
    "wantsTestDrive": false,
    "mentionedBudget": false,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggestedAction": "compare_vehicles",
  "quickReplies": [
    "Detalles del RAV4",
    "Detalles de la Tucson",
    "Agendar prueba",
    "Ver más opciones"
  ]
}
```

---

## 4. Boundary: Cross-Dealer Comparison

### Trigger

- Intent: `CrossDealerRefusal`
- Ejemplo: "En otro dealer el Civic está más barato", "Compara con lo que tiene X Motors"

### Respuesta

```json
{
  "response": "Entiendo que estés comparando, ¡es lo mejor antes de decidir! 👍\n\nSolo puedo comparar vehículos del inventario de {dealerName}. No tengo acceso a información de otros concesionarios.\n\nNuestros vehículos incluyen revisión mecánica certificada y documentación al día. ¿Te gustaría una cotización personalizada? 🤝",
  "intent": "CrossDealerRefusal",
  "confidence": 0.93,
  "isFallback": false,
  "parameters": { "mentionedCompetitor": true },
  "leadSignals": { "interested": true },
  "suggestedAction": null,
  "quickReplies": [
    "Cotización personalizada",
    "Hablar con asesor",
    "Seguir viendo"
  ]
}
```

### ⚠️ Lo que NUNCA se dice ante cross-dealer

| Prohibido                            | Por qué                                    |
| ------------------------------------ | ------------------------------------------ |
| "Somos más baratos que X"            | Publicidad comparativa ilegal (Ley 358-05) |
| "X Motors no tiene la misma calidad" | Denigración de competidor                  |
| "Te garantizo mejor precio"          | Promesa vinculante                         |
| Cualquier dato sobre otro dealer     | Groundedness violation                     |
| "Ese dealer no es confiable"         | Difamación                                 |

---

## 5. Casos Edge

### 5.1 — Comparar más de 3 vehículos

```
Usuario: "Compara estos 5 que me mostraste"
Bot: "Para una comparación clara, puedo comparar hasta 3 vehículos
      a la vez. ¿Cuáles te interesan más?"
```

### 5.2 — Comparar vehículos de categorías muy diferentes

```
Usuario: "Compara la yipeta con el sedancito"
Bot: "Son categorías diferentes, pero aquí van las diferencias:
     [tabla comparativa normal]
     Como son tipos distintos, depende más de lo que necesites.
     ¿Es para ciudad o para carretera?"
```

### 5.3 — Uno de los vehículos no existe en inventario

```
Usuario: "Compara la RAV4 con el BMW X3"
Bot: intent=VehicleSearch (para BMW X3)
     → Si no encuentra: "No tenemos BMW X3 en inventario.
       Tenemos {alternativa similar}. ¿Comparo con esa?"
```

### 5.4 — Pide comparar en modo General

```
Usuario: "¿Qué es mejor, un Toyota o un Honda?"
Bot: "Para comparar vehículos específicos, te invito a visitar
     el perfil de un dealer en okla.com.do. Ahí podrás comparar
     modelos concretos con precios y disponibilidad real. 🚗"
```

---

## 6. Resumen de Reglas por Modo

| Regla                 | SV  | DI                     | General |
| --------------------- | --- | ---------------------- | ------- |
| Comparar vehículos    | ❌  | ✅ (2-3, mismo dealer) | ❌      |
| Tabla comparativa     | ❌  | ✅                     | ❌      |
| Sugerir alternativas  | ❌  | ✅ (del inventario)    | ❌      |
| Recomendar "el mejor" | ❌  | ❌ (neutral siempre)   | ❌      |
| Cross-dealer          | N/A | ❌ (refusal)           | ❌      |
| Datos inventados      | ❌  | ❌                     | ❌      |

---

_Documento actualizado para arquitectura Dual-Mode v2.0 — Febrero 2026_
