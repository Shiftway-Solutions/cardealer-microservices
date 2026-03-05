# 📊 Reporte: Estrategia de Reporte de Ventas Cerradas

**Fecha:** 2026-03-05  
**Autor:** Equipo OKLA  
**Versión:** 1.0

---

## 1. Resumen Ejecutivo

Este documento analiza la implementación de un sistema de **reporte de ventas cerradas** en OKLA, donde dealers, vendedores y compradores reportan cuando una venta se ha completado exitosamente. Esto permite medir la eficiencia publicitaria, fortalecer el OKLA Score y obtener ventajas competitivas únicas en el mercado dominicano.

---

## 2. Estado Actual del Sistema

### 2.1 Lo que existe hoy

| Componente | Estado | Detalle |
|-----------|--------|---------|
| `VehicleStatus.Sold` | ✅ Implementado | Enum con 7 estados (Draft → Sold) |
| Endpoint `POST /api/vehicles/{id}/sold` | ✅ Implementado | Marca vehículo como vendido, acepta `SalePrice` y `Notes` |
| `VehicleSoldEvent` | ❌ No publicado | El controller NO emite evento a RabbitMQ |
| Tracking de comprador | ❌ No existe | No hay `BuyerId` en la entidad Sale |
| Transacción/Orden | ❌ No existe | No hay entidad que vincule comprador ↔ vendedor |
| OKLA Score (dealer) | ❌ Solo documentación | Score 0–100 con 6 factores diseñado pero no implementado |
| Dealer.CompletedSales | ✅ Campo existe | `int` en entidad Dealer pero no se actualiza automáticamente |

### 2.2 Flujo Actual

```
Dealer → POST /api/vehicles/{id}/sold → Status=Sold, SoldAt=now → FIN
```

No hay confirmación del comprador, no hay tracking, no hay evento publicado.

---

## 3. Análisis de Marketing: ¿Por qué medir ventas cerradas?

### 3.1 Métricas que se pueden obtener

| Métrica | Fórmula | Valor de negocio |
|---------|---------|-------------------|
| **Tasa de conversión** | Ventas / Publicaciones × 100 | Eficiencia de la plataforma |
| **Tiempo medio de venta** | Promedio(SoldAt - PublishedAt) | Velocidad del marketplace |
| **Precio vs. Listado** | SalePrice / ListedPrice × 100 | Poder de negociación |
| **ROI publicitario** | Ventas de ads premium / Costo ads | Justificación de planes pagos |
| **Volumen por categoría** | Ventas por marca/modelo/año | Tendencias del mercado RD |
| **Eficiencia por plan** | Ventas/Dealer por tier de plan | Upsell a planes superiores |

### 3.2 Ventajas Competitivas

1. **Datos de mercado reales**: OKLA sería la ÚNICA plataforma en RD con datos de transacciones reales (precio final, tiempo de venta). Esto es oro para dealers y compradores.

2. **Índice de precios OKLA**: Con suficientes transacciones, podemos crear un índice de precios de vehículos usados en RD — algo que NO existe actualmente.

3. **Transparencia de mercado**: Mostrar "Este modelo se vende en promedio en X días por $Y" genera confianza en compradores y justifica precios de venta.

4. **Reportes para dealers**: Dashboard con métricas de rendimiento vs. mercado — razón para pagar planes Pro/Elite.

5. **Data para OKLA Score**: Historial de transacciones exitosas fortalece la reputación verificable del dealer.

---

## 4. Impacto en el OKLA Score

### 4.1 Factor de Transacciones (15% del Score actual)

El OKLA Score del dealer/vendedor tiene 6 factores. El **Transaction Score** (15%) se beneficia directamente:

```
Transaction Score = (completedSales / totalListings) × 0.4    // Tasa conversión
                  + min(completedSales / 10, 1.0) × 0.3       // Volumen
                  + buyerConfirmation × 0.2                     // Confirmación
                  + avgDaysToSell_score × 0.1                   // Velocidad
```

### 4.2 Bonificaciones por venta confirmada

| Evento | Impacto en Score |
|--------|-----------------|
| Dealer reporta venta | +2 pts base |
| Comprador confirma venta | +3 pts adicionales |
| Comprador deja reseña post-venta | +5 pts |
| Venta completada en < 30 días | +1 pt bonus |
| 10+ ventas confirmadas | Badge "Vendedor Activo" |
| 50+ ventas confirmadas | Badge "Vendedor Estrella" |

### 4.3 Penalizaciones

| Evento | Impacto |
|--------|---------|
| Venta reportada pero comprador la niega | -5 pts |
| Patrón de ventas sospechoso detectado | -10 pts + revisión manual |
| Publicación marcada como vendida sin actividad previa | Flag para auditoría |

---

## 5. Sistema Anti-Fraude: Prevención de Ventas Simuladas

### 5.1 Amenaza identificada

Dealers podrían crear cuentas falsas de compradores para simular ventas y subir su OKLA Score artificialmente.

### 5.2 Técnicas de detección

#### A. Análisis de IP y dispositivo
```
Regla: Si buyer_ip == seller_ip → FLAG
Regla: Si device_fingerprint coincide → FLAG
Regla: Si mismo IP confirma compras de múltiples dealers → FLAG
```

#### B. Análisis de patrones temporales
```
Regla: Si cuenta del comprador fue creada < 24h antes de la compra → SOSPECHOSO
Regla: Si comprador solo ha "comprado" de un dealer → SOSPECHOSO
Regla: Si > 3 ventas confirmadas en 24h del mismo dealer → REVISAR
Regla: Si tiempo entre publicación y venta < 2 horas → FLAG
```

#### C. Análisis de cuenta del comprador
```
Regla: Si comprador no tiene KYC verificado → Confirmación no cuenta para Score
Regla: Si comprador tiene email temporal/desechable → FLAG
Regla: Si comprador nunca navegó otros vehículos → SOSPECHOSO
Regla: Si comprador no tiene historial de contacto previo con dealer → SOSPECHOSO
```

#### D. Análisis de red social
```
Regla: Si múltiples compradores comparten datos (teléfono, dirección) → RED
Regla: Si comprador y vendedor tienen misma dirección → FLAG
Regla: Si patrón de confirmaciones es demasiado regular (cada X horas) → FLAG
```

### 5.3 Niveles de confianza en la confirmación

| Nivel | Condiciones | Peso en Score |
|-------|------------|---------------|
| **Alto** | Comprador KYC verificado + IP diferente + historial de navegación + contacto previo | 100% |
| **Medio** | Comprador registrado > 7 días + IP diferente + algún historial | 60% |
| **Bajo** | Comprador nuevo + sin KYC + sin historial | 20% |
| **Rechazado** | Flags de fraude detectados | 0% + penalización |

### 5.4 Sistema de puntuación de confianza (Fraud Score)

```
fraudScore = 100  // Empieza con confianza total

// Restar por señales de riesgo
if (sameIP) fraudScore -= 40
if (newAccount < 24h) fraudScore -= 20
if (noKYC) fraudScore -= 15
if (noContactHistory) fraudScore -= 10
if (noBrowsingHistory) fraudScore -= 10
if (sameDeviceFingerprint) fraudScore -= 50
if (temporaryEmail) fraudScore -= 25

// Resultado
if (fraudScore >= 70) → APROBADO (confirmación válida)
if (fraudScore >= 40) → PENDIENTE (revisión manual)
if (fraudScore < 40) → RECHAZADO (no cuenta para Score)
```

---

## 6. Diseño de Implementación

### 6.1 Flujo completo propuesto

```
1. Dealer marca vehículo como vendido
   POST /api/vehicles/{id}/sold { salePrice, buyerEmail? }
   
2. Sistema publica VehicleSoldEvent a RabbitMQ
   → DealerManagementService actualiza CompletedSales
   → NotificationService envía solicitud de confirmación al comprador (si email)
   → ReviewService genera ReviewRequest (solicitud de reseña)

3. Comprador recibe email: "¿Compraste este vehículo?"
   → Click "Sí, confirmo" → POST /api/sales/{id}/confirm
   → Click "No, no compré" → POST /api/sales/{id}/deny

4. Sistema valida anti-fraude
   → Calcula fraudScore
   → Si aprobado: actualiza OKLA Score del dealer
   → Si rechazado: penaliza y flaggea

5. Después de 7 días, si comprador confirmó:
   → ReviewRequest se activa
   → Comprador recibe email para dejar reseña
```

### 6.2 Entidades nuevas necesarias

```csharp
public class SaleTransaction
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid SellerId { get; set; }
    public Guid? BuyerId { get; set; }
    public string? BuyerEmail { get; set; }
    public decimal ListedPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public SaleStatus Status { get; set; }  // Reported, Confirmed, Denied, Expired
    public string ConfirmationToken { get; set; }
    public int FraudScore { get; set; }
    public string? FraudFlags { get; set; }  // JSON array
    public ConfidenceLevel ConfidenceLevel { get; set; }
}

public enum SaleStatus { Reported, Confirmed, Denied, Expired }
public enum ConfidenceLevel { High, Medium, Low, Rejected }
```

### 6.3 Eventos de dominio

| Evento | Publicado por | Consumido por |
|--------|--------------|---------------|
| `vehicles.vehicle.sold` | VehiclesSaleService | DealerMgmt, Notification, Review |
| `sales.transaction.confirmed` | VehiclesSaleService | DealerMgmt (Score update) |
| `sales.transaction.denied` | VehiclesSaleService | DealerMgmt (penalización) |
| `sales.fraud.detected` | VehiclesSaleService | AdminService (alerta) |

### 6.4 Endpoints nuevos

```
POST   /api/vehicles/{id}/sold              — Dealer reporta venta
POST   /api/sales/{id}/confirm              — Comprador confirma (via token)
POST   /api/sales/{id}/deny                 — Comprador niega
GET    /api/sales/dealer/{dealerId}/stats    — Estadísticas de ventas
GET    /api/sales/market/trends              — Tendencias de mercado (público)
GET    /api/admin/sales/suspicious           — Admin: transacciones sospechosas
```

---

## 7. Análisis Económico de Implementación

### 7.1 Costo de desarrollo

| Componente | Esfuerzo | Prioridad |
|-----------|----------|-----------|
| Publicar `VehicleSoldEvent` en controller existente | 2 horas | P0 - Inmediata |
| Entidad `SaleTransaction` + migrations | 4 horas | P1 - Corto plazo |
| Consumer de evento en DealerManagementService | 3 horas | P1 |
| Sistema anti-fraude básico (IP check) | 4 horas | P1 |
| Endpoint de confirmación de comprador | 3 horas | P1 |
| Email de confirmación (NotificationService) | 2 horas | P2 |
| Dashboard de métricas de ventas | 6 horas | P2 |
| Anti-fraude avanzado (fingerprinting) | 8 horas | P3 |
| Índice de precios de mercado | 12 horas | P3 |
| **Total** | **~44 horas** | |

### 7.2 Costo de infraestructura adicional

**$0 adicional** — Todo se ejecuta dentro de los microservicios existentes:
- `SaleTransaction` se guarda en la DB de VehiclesSaleService (ya tiene PostgreSQL)
- Eventos van por RabbitMQ existente
- Emails van por NotificationService existente

---

## 8. Roadmap de Implementación

### Fase 1 — MVP (Semana 1)
- [ ] Publicar `VehicleSoldEvent` cuando dealer marca venta
- [ ] Consumer en DealerManagementService para actualizar `CompletedSales`
- [ ] Métrica básica: tasa de conversión por dealer

### Fase 2 — Confirmación (Semana 2-3)
- [ ] Entidad `SaleTransaction`
- [ ] Endpoint de confirmación por comprador (con token)
- [ ] Email de solicitud de confirmación
- [ ] Anti-fraude básico (IP, edad de cuenta)

### Fase 3 — Score Integration (Semana 4)
- [ ] Integrar ventas confirmadas en OKLA Score
- [ ] Badge system (Vendedor Activo, Estrella)
- [ ] Dashboard de métricas para dealers

### Fase 4 — Market Intelligence (Mes 2)
- [ ] Índice de precios por marca/modelo
- [ ] Tendencias de mercado público
- [ ] Anti-fraude avanzado (device fingerprinting)
- [ ] Reportes para dealers Pro/Elite

---

## 9. Ventajas Competitivas Clave

| # | Ventaja | Impacto |
|---|---------|---------|
| 1 | **Único marketplace en RD con datos de transacciones reales** | Diferenciación total |
| 2 | **Índice de precios de mercado** | Los compradores vienen por los datos |
| 3 | **Score verificable con ventas reales** | Confianza > competencia |
| 4 | **Métricas de ROI para dealers** | Justifica planes pagos ($49–$299) |
| 5 | **Anti-fraude transparente** | Integridad de la plataforma |
| 6 | **Datos para decisiones de negocio** | Segmentación, pricing, upsell |

---

## 10. Implementación Actual (MVP)

Se implementó la Fase 1 del MVP:

1. ✅ **Evento `VehicleSoldEvent`** — Se publica cuando un dealer marca un vehículo como vendido
2. ✅ **Tracking de `BuyerEmail`** — El endpoint ahora acepta email del comprador
3. ✅ **Campo `CompletedSales`** en Dealer se actualiza via evento
4. ✅ **Validación anti-fraude básica** — Score de confianza en el reporte

---

## 11. Conclusión

El sistema de reporte de ventas cerradas es una **inversión estratégica de alto impacto y bajo costo**. Con $0 de infraestructura adicional y ~44 horas de desarrollo total, OKLA obtiene:

- **Datos únicos en el mercado dominicano** de transacciones vehiculares
- **OKLA Score fortalecido** con métricas de ventas reales
- **Anti-fraude robusto** que protege la integridad del Score
- **Ventajas competitivas** que ningún competidor puede replicar sin esta data
- **Justificación de planes pagos** con métricas de ROI para dealers

La recomendación es implementar las Fases 1-2 inmediatamente (costo: ~20 horas) y las Fases 3-4 después del lanzamiento.
