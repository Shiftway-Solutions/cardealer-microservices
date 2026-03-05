# 🏆 OKLA Score — APIs y Cálculo de Reputación

**Fecha:** 2025-07-14  
**Autor:** GitHub Copilot (Claude)

---

## 1. ¿Qué es el OKLA Score?

El **OKLA Score** es un puntaje de reputación (0–100) asignado a dealers y vendedores en la plataforma OKLA. Mide la confiabilidad del vendedor basado en múltiples factores, incluyendo las **estrellas de reseñas** de los compradores.

---

## 2. Fórmula del OKLA Score

```
OKLA Score = (W1 × Review Score) + (W2 × Response Score) + (W3 × Transaction Score)
           + (W4 × Listing Quality) + (W5 × Verification Score) + (W6 × Activity Score)
```

### Pesos (Weights)

| Factor                 | Peso    | Descripción                                                  |
| ---------------------- | ------- | ------------------------------------------------------------ |
| **Review Score**       | **35%** | Estrellas promedio de reseñas (★) — el factor más importante |
| **Response Score**     | 20%     | Velocidad y tasa de respuesta a contactos                    |
| **Transaction Score**  | 15%     | Ventas completadas exitosamente                              |
| **Listing Quality**    | 15%     | Calidad de las publicaciones (fotos, descripción, precio)    |
| **Verification Score** | 10%     | Nivel de verificación KYC del vendedor                       |
| **Activity Score**     | 5%      | Actividad reciente en la plataforma                          |

### Cálculo del Review Score (★ → Puntos)

```
Review Score = (Average Rating / 5) × 100 × Review Weight Factor

Review Weight Factor = min(1.0, Total Reviews / 10)
```

| Estrellas Promedio | Reviews | Review Score (de 100)             |
| ------------------ | ------- | --------------------------------- |
| 5.0 ★              | 10+     | 100                               |
| 4.5 ★              | 10+     | 90                                |
| 4.0 ★              | 10+     | 80                                |
| 3.5 ★              | 10+     | 70                                |
| 3.0 ★              | 5       | 30 (penalizado por pocas reseñas) |
| 1.0 ★              | 3       | 6 (muy baja reputación)           |

**Sin reseñas:** El vendedor comienza con Review Score = 50 (neutro).

### Cálculo de los Demás Factores

| Factor             | Fórmula                                                                                                                               | Rango |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| Response Score     | `(Contacts Responded / Total Contacts) × 100` × `min(1, Avg Response Time < 24h ? 1.0 : 0.5)`                                         | 0–100 |
| Transaction Score  | `(Completed Sales / Total Listings) × 100` × `min(1, Total Sales / 5)`                                                                | 0–100 |
| Listing Quality    | `(Photos > 5 ? 25 : Photos×5) + (Description > 100 chars ? 25 : 10) + (Price in market range ? 25 : 10) + (Complete specs ? 25 : 10)` | 0–100 |
| Verification Score | `Unverified=0, Basic KYC=50, Full KYC=80, KYC + Business License=100`                                                                 | 0–100 |
| Activity Score     | `Last active < 7 days ? 100 : < 30 days ? 75 : < 90 days ? 25 : 0`                                                                    | 0–100 |

---

## 3. APIs Necesarias para el Cálculo

### 3.1 APIs Internas (Ya Existentes en OKLA)

| API                     | Endpoint                                     | Datos que Provee                               | Estado       |
| ----------------------- | -------------------------------------------- | ---------------------------------------------- | ------------ |
| **ReviewService**       | `GET /api/reviews/seller/{sellerId}/summary` | Rating promedio, total reseñas, distribución ★ | ✅ Existente |
| **ContactService**      | `GET /api/contactrequests?sellerId={id}`     | Total contactos, respondidos, tiempo respuesta | ✅ Existente |
| **VehiclesSaleService** | `GET /api/vehicles?sellerId={id}`            | Listados activos, vendidos, calidad            | ✅ Existente |
| **KYCService**          | `GET /api/kyc/status/{userId}`               | Nivel de verificación                          | ✅ Existente |
| **AuthService**         | `GET /api/users/{id}`                        | Última actividad, fecha registro               | ✅ Existente |
| **AppointmentService**  | `GET /api/appointments?sellerId={id}`        | Citas cumplidas vs canceladas                  | ✅ Existente |

### 3.2 APIs Externas Recomendadas (Para Enriquecer el Score)

| API                         | Proveedor        | Costo                       | Uso                            | Prioridad  |
| --------------------------- | ---------------- | --------------------------- | ------------------------------ | ---------- |
| **DGII RNC Validation**     | DGII (Gov DR)    | Gratis                      | Validar RNC/cédula del dealer  | 🟢 Alta    |
| **Google Business Profile** | Google           | Gratis (Places API: $17/1K) | Cross-reference reseñas Google | 🟡 Media   |
| **Facebook Marketplace**    | Meta Graph API   | Gratis                      | Cross-reference perfil dealer  | 🟡 Media   |
| **INTRANT Vehicle History** | INTRANT (Gov DR) | Por negociar                | Historial de vehículos         | 🟢 Alta    |
| **AML/PEP Check**           | ComplyAdvantage  | $0.10/check                 | Anti-lavado de dinero          | 🔴 Fase 2+ |
| **Phone Verification**      | Twilio Verify    | $0.05/verify                | Verificar teléfono del seller  | 🟡 Media   |
| **Email Verification**      | ZeroBounce       | $0.008/verify               | Verificar email válido         | 🟢 Alta    |

### 3.3 APIs Específicas de República Dominicana

| API                 | Entidad                                 | Función                                  | Integración             |
| ------------------- | --------------------------------------- | ---------------------------------------- | ----------------------- |
| **DGII e-Consulta** | Dirección General de Impuestos Internos | Validar RNC empresarial, estado fiscal   | REST API o web scraping |
| **TSS**             | Tesorería de la Seguridad Social        | Verificar empresa legalmente constituida | Por convenio            |
| **INTRANT**         | Instituto Nacional de Tránsito          | Verificar placa, historial de vehículo   | Por convenio            |
| **JCE Cedula**      | Junta Central Electoral                 | Validar cédula del vendedor              | Por convenio            |
| **Proconsumidor**   | Pro Consumidor                          | Verificar quejas/reclamaciones           | Público                 |

---

## 4. Implementación Recomendada

### Fase 0 (MVP) — Solo APIs Internas

```
OKLA Score =
  0.35 × ReviewScore(ReviewService) +
  0.20 × ResponseScore(ContactService) +
  0.15 × TransactionScore(VehiclesSaleService) +
  0.15 × ListingQuality(VehiclesSaleService) +
  0.10 × VerificationScore(KYCService) +
  0.05 × ActivityScore(AuthService)
```

**Costo adicional:** $0 (todas APIs internas)

### Fase 1 — Agregar Validaciones DR

```
+ Bonus: DGII RNC válido → +5 puntos
+ Bonus: Email verificado → +2 puntos
+ Bonus: Teléfono verificado → +3 puntos
```

**Costo adicional:** ~$10-20/mes (verificaciones)

### Fase 2 — Cross-Reference External

```
+ Google Business reviews cross-reference
+ INTRANT vehicle history verification
+ AML/PEP screening for dealers
```

**Costo adicional:** ~$50-100/mes

---

## 5. Impacto de las Reseñas en el Score

### Escenarios

| Dealer               | Rating | Reviews | Review Score | OKLA Score Est. |
| -------------------- | ------ | ------- | ------------ | --------------- |
| Dealer A (excelente) | 4.8 ★  | 25      | 96           | ~85             |
| Dealer B (bueno)     | 4.2 ★  | 15      | 84           | ~75             |
| Dealer C (regular)   | 3.5 ★  | 8       | 56           | ~55             |
| Dealer D (malo)      | 2.0 ★  | 12      | 40           | ~40             |
| Dealer E (nuevo)     | N/A    | 0       | 50           | ~50             |

### Reglas de Protección

1. **Mínimo de reseñas**: Score de Review se pondera por `min(1, reviews/10)` — necesita 10+ reseñas para peso completo
2. **Anti-bomba de reseñas**: Máx 1 reseña por comprador por dealer por mes
3. **Reseñas verificadas**: Solo compradores que contactaron al dealer pueden dejar reseña
4. **Decaimiento temporal**: Reseñas > 1 año tienen 50% del peso
5. **Respuesta del dealer**: Si el dealer responde a una reseña negativa, penalización se reduce 20%

---

## 6. Arquitectura del OKLA Score Service

```
┌─────────────────┐
│  OKLA Score      │
│  Calculator      │
│  (Scheduled Job) │
└───────┬─────────┘
        │ Cada 6 horas
        ├── GET ReviewService → rating, count
        ├── GET ContactService → response rate
        ├── GET VehiclesSaleService → sales, quality
        ├── GET KYCService → verification level
        ├── GET AuthService → last activity
        └── Calcular Score → Guardar en DB
                │
                └── Publicar evento: seller.score.updated
                    │
                    └── SearchAgent indexa nuevo score
```

### Dónde Implementar

**Opción A (Recomendada):** Dentro de `ReviewService` como feature adicional

- Ya tiene acceso a reseñas (35% del peso)
- Agrega endpoints: `GET /api/okla-score/{sellerId}`, `GET /api/okla-score/{sellerId}/breakdown`
- Scheduled job cada 6 horas recalcula scores

**Opción B:** Nuevo microservicio `ScoreService`

- Más aislado pero agrega overhead
- Solo justificado si la lógica crece significativamente

---

## 7. Endpoints Propuestos

```
GET  /api/okla-score/{sellerId}
     → { score: 82, tier: "gold", lastUpdated: "2025-07-14T..." }

GET  /api/okla-score/{sellerId}/breakdown
     → {
          score: 82,
          tier: "gold",
          factors: {
            reviews: { score: 90, weight: 0.35, rating: 4.5, count: 18 },
            response: { score: 85, weight: 0.20, rate: 0.92, avgTime: "2h" },
            transactions: { score: 75, weight: 0.15, completed: 12, listed: 20 },
            listingQuality: { score: 80, weight: 0.15, avgPhotos: 8, avgDesc: 250 },
            verification: { score: 80, weight: 0.10, level: "full_kyc" },
            activity: { score: 100, weight: 0.05, lastActive: "2025-07-14" }
          }
        }

GET  /api/okla-score/leaderboard?limit=10
     → Top 10 dealers por OKLA Score
```

### Tiers

| OKLA Score | Tier        | Badge           | Beneficio                        |
| ---------- | ----------- | --------------- | -------------------------------- |
| 90–100     | 🏆 Platinum | Estrella dorada | Top de búsqueda, badge destacado |
| 75–89      | 🥇 Gold     | Estrella plata  | Prioridad en búsqueda            |
| 50–74      | 🥈 Silver   | Sin badge       | Normal                           |
| 25–49      | 🥉 Bronze   | ⚠️ Advertencia  | Menor visibilidad                |
| 0–24       | ❌ Red Flag | 🚫 Alerta       | Revisión manual requerida        |

---

_Este documento sirve como especificación para implementar el OKLA Score. Priorizar Fase 0 (APIs internas) para MVP._
