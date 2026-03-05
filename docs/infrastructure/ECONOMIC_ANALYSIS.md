# 💰 Análisis Económico — OKLA Platform

**Fecha:** 2025-07-14  
**Autor:** GitHub Copilot (Claude)

---

## 1. Análisis de Costos de Modelos Claude (AI/LLM)

### Modelos en Uso

| Servicio                            | Modelo            | Precio Input | Precio Output | Uso Estimado |
| ----------------------------------- | ----------------- | ------------ | ------------- | ------------ |
| ChatbotService (Dealer Sales Agent) | Claude Sonnet 4.5 | $3.00/MTok   | $15.00/MTok   | Alto         |
| SupportAgent (Soporte)              | Claude Haiku 4.5  | $0.25/MTok   | $1.25/MTok    | Medio        |
| SearchAgent (Búsqueda AI)           | Claude Haiku 4.5  | $0.25/MTok   | $1.25/MTok    | Medio        |

> **MTok** = millón de tokens. Precios de Anthropic a julio 2025.

### Estimación de Costos por Fase de Usuarios

#### Fase 0: MVP (0–500 usuarios, ~50 dealers)

| Servicio       | Interacciones/día | Tokens/interacción | Costo/día | Costo/mes      |
| -------------- | ----------------- | ------------------ | --------- | -------------- |
| ChatbotService | 20                | ~2,000 tokens      | $0.07     | ~$2.10         |
| SupportAgent   | 30                | ~1,500 tokens      | $0.01     | ~$0.45         |
| SearchAgent    | 50                | ~800 tokens        | $0.01     | ~$0.40         |
| **Total AI**   |                   |                    |           | **~$2.95/mes** |

#### Fase 1: Crecimiento (500–2,000 usuarios, ~200 dealers)

| Servicio       | Interacciones/día | Costo/mes       |
| -------------- | ----------------- | --------------- |
| ChatbotService | 100               | ~$10.50         |
| SupportAgent   | 150               | ~$2.25          |
| SearchAgent    | 300               | ~$2.40          |
| **Total AI**   |                   | **~$15.15/mes** |

#### Fase 2: Medio (2,000–10,000 usuarios)

| Servicio       | Interacciones/día | Costo/mes       |
| -------------- | ----------------- | --------------- |
| ChatbotService | 500               | ~$52.50         |
| SupportAgent   | 500               | ~$7.50          |
| SearchAgent    | 1,500             | ~$12.00         |
| **Total AI**   |                   | **~$72.00/mes** |

#### Fase 3: Alta escala (10,000–50,000 usuarios)

| Servicio       | Interacciones/día | Costo/mes     |
| -------------- | ----------------- | ------------- |
| ChatbotService | 2,000             | ~$210         |
| SupportAgent   | 2,000             | ~$30          |
| SearchAgent    | 5,000             | ~$40          |
| **Total AI**   |                   | **~$280/mes** |

### Optimizaciones Implementadas ✅

| Optimización                                               | Ahorro Estimado              | Estado    |
| ---------------------------------------------------------- | ---------------------------- | --------- |
| **Prompt Caching** (Anthropic beta)                        | ~90% en system prompt tokens | ✅ Activo |
| **FAQ Response Cache** (SupportAgent)                      | ~70% menos llamadas a Claude | ✅ Activo |
| **SHA256 Response Cache** (SearchAgent)                    | ~50% menos llamadas a Claude | ✅ Activo |
| **Session interaction limits** (10/sesión, 50/usuario/día) | Previene abuso               | ✅ Activo |
| **MaxTokens limitado** (400-512 en producción)             | ~30% menos tokens output     | ✅ Activo |

### Recomendaciones para Optimizar Costos AI

| #   | Recomendación                                                                        | Impacto            | Esfuerzo |
| --- | ------------------------------------------------------------------------------------ | ------------------ | -------- |
| 1   | **Migrar ChatbotService a Haiku 4.5** para consultas simples                         | -80% costo chatbot | Bajo     |
| 2   | **Routing inteligente**: Haiku para preguntas FAQ, Sonnet solo para ventas complejas | -60% costo chatbot | Medio    |
| 3   | **Batch API** de Anthropic para procesamiento no-real-time                           | -50% costo batch   | Medio    |
| 4   | **Caché semántico**: cachear respuestas similares (no solo idénticas)                | -40% llamadas      | Alto     |
| 5   | **Pre-computar respuestas** para las 50 preguntas más frecuentes                     | -30% llamadas      | Bajo     |

### Modelo Híbrido Recomendado

```
Consulta del usuario
    │
    ├── FAQ Match (caché local) → Respuesta inmediata ($0)
    │
    ├── Consulta simple → Claude Haiku 4.5 ($0.25/MTok)
    │       - "¿Cuánto cuesta el plan Pro?"
    │       - "¿Cómo publico un vehículo?"
    │
    └── Consulta compleja → Claude Sonnet 4.5 ($3/MTok)
            - Negociación de venta
            - Análisis comparativo de vehículos
            - Recomendaciones personalizadas
```

**Costo estimado con modelo híbrido:** ~40% menos que usar Sonnet para todo.

---

## 2. Análisis Económico de Infraestructura

### Costos en Etapa de Desarrollo (Actual)

| Componente           | Servicio                         | Costo/mes        |
| -------------------- | -------------------------------- | ---------------- |
| Kubernetes Cluster   | 2× s-4vcpu-8gb (DOKS)            | $96.00           |
| PostgreSQL           | DO Managed Basic                 | $15.00           |
| Load Balancer        | DO LB                            | $12.00           |
| Container Registry   | GHCR (GitHub gratuito)           | $0.00            |
| DNS                  | Cloudflare Free                  | $0.00            |
| Claude API           | Anthropic (desarrollo, bajo uso) | ~$3.00           |
| **Total Desarrollo** |                                  | **~$126.00/mes** |

### Costos en Producción por Fase

#### Fase 0: Lanzamiento (0–500 usuarios)

| Componente                       | Costo/mes        |
| -------------------------------- | ---------------- |
| DOKS (2× s-4vcpu-8gb)            | $96.00           |
| PostgreSQL Managed               | $15.00           |
| Load Balancer                    | $12.00           |
| DO Spaces (media storage, 250GB) | $5.00            |
| Claude API                       | ~$3.00           |
| Dominio + SSL                    | ~$2.00           |
| **Total**                        | **~$133.00/mes** |

#### Fase 1: Crecimiento (500–2,000 usuarios)

| Componente                  | Costo/mes        |
| --------------------------- | ---------------- |
| DOKS (2× s-4vcpu-8gb)       | $96.00           |
| PostgreSQL Managed Standard | $30.00           |
| Load Balancer               | $12.00           |
| DO Spaces (500GB + CDN)     | $10.00           |
| Claude API                  | ~$15.00          |
| Monitoring (DO Monitoring)  | $0.00 (incluido) |
| **Total**                   | **~$163.00/mes** |

#### Fase 2: Medio (2,000–10,000 usuarios)

| Componente            | Costo/mes        |
| --------------------- | ---------------- |
| DOKS (3× s-4vcpu-8gb) | $144.00          |
| PostgreSQL Managed HA | $60.00           |
| DO Managed Redis      | $15.00           |
| Load Balancer         | $12.00           |
| DO Spaces (1TB + CDN) | $15.00           |
| Claude API            | ~$72.00          |
| Cloudflare Pro        | $20.00           |
| **Total**             | **~$338.00/mes** |

#### Fase 3: Alta Escala (10,000–50,000 usuarios)

| Componente                    | Costo/mes          |
| ----------------------------- | ------------------ |
| DOKS (4-6× s-4vcpu-8gb)       | $192-$288          |
| PostgreSQL HA + Read Replicas | $120.00            |
| DO Managed Redis HA           | $30.00             |
| Load Balancer ×2              | $24.00             |
| DO Spaces (5TB + CDN)         | $30.00             |
| Claude API                    | ~$280.00           |
| Cloudflare Pro                | $20.00             |
| Monitoring Stack              | $30.00             |
| **Total**                     | **~$726–$822/mes** |

---

## 3. Análisis de Media Storage — DigitalOcean Spaces

### Situación Actual

- MediaService usa **AWS S3 SDK** (compatible con DO Spaces)
- Configurado con `ServiceURL` en appsettings
- CDN domain: `cdn.okla.com.do`

### Migración a DO Spaces (Recomendado)

| Aspecto         | AWS S3                 | DO Spaces     |
| --------------- | ---------------------- | ------------- |
| Storage (250GB) | $5.75/mes              | $5.00/mes     |
| Transfer (1TB)  | $90/mes                | **Incluido**  |
| CDN             | CloudFront ($0.085/GB) | **Incluido**  |
| Latencia a DR   | ~50ms (Virginia)       | ~30ms (NYC)   |
| Compatibilidad  | Nativo                 | S3-compatible |

**Ahorro estimado con DO Spaces:** $85+/mes en transfer/CDN cuando escale.

### Configuración para DO Spaces

```json
{
  "S3Storage": {
    "ServiceURL": "https://nyc3.digitaloceanspaces.com",
    "BucketName": "okla-media",
    "Region": "nyc3",
    "CDNDomain": "cdn.okla.com.do",
    "AccessKey": "[from K8s secret]",
    "SecretKey": "[from K8s secret]"
  }
}
```

**Acciones recomendadas:**

1. Crear Space `okla-media` en DO Console (región nyc3)
2. Habilitar CDN con custom domain `cdn.okla.com.do`
3. Actualizar K8s secret con DO Spaces credentials
4. El código existente funciona sin cambios (S3-compatible)

---

## 4. Servicios de DO vs Microservicios Propios

### Servicios que se pueden reemplazar con DO Managed

| Microservicio Actual  | Servicio DO      | Costo DO   | Ahorro                           |
| --------------------- | ---------------- | ---------- | -------------------------------- |
| Redis (deployment)    | DO Managed Redis | $15/mes    | Menos mantenimiento, HA incluido |
| RabbitMQ (deployment) | —                | N/A        | No hay equivalente en DO         |
| PostgreSQL (ya en DO) | ✅ Ya migrado    | $15-60/mes | —                                |

### Servicios que NO vale la pena migrar a DO

- **RabbitMQ**: No tiene equivalente managed en DO. Mantener in-cluster.
- **LLM Server**: Eliminado (usamos API de Anthropic directamente).
- **Redis**: Considerar DO Managed Redis en Fase 2+ por HA automático.

---

## 5. Revenue vs Costos — Break-Even Analysis

### Fuentes de Revenue

| Producto                      | Precio           | Revenue/unidad |
| ----------------------------- | ---------------- | -------------- |
| Dealer Plan Libre             | $0/mes           | $0             |
| Dealer Plan Visible           | $29/mes          | $29            |
| Dealer Plan Pro               | $89/mes          | $89            |
| Dealer Plan Elite             | $199/mes         | $199           |
| Listado individual (seller)   | $29/publicación  | $29            |
| OKLA Coins Starter (50)       | $4.99            | $4.99          |
| OKLA Coins Popular (120)      | $9.99            | $9.99          |
| OKLA Coins Pro (300)          | $19.99           | $19.99         |
| OKLA Coins Business (800)     | $39.99           | $39.99         |
| Publicidad (varios productos) | $15-199/producto | Variable       |

### Escenarios de Revenue

#### Escenario Conservador (Fase 0)

| Fuente                | Cantidad   | Revenue/mes  |
| --------------------- | ---------- | ------------ |
| Dealers Plan Visible  | 5          | $145         |
| Dealers Plan Pro      | 2          | $178         |
| Listados individuales | 10         | $290         |
| OKLA Coins            | 5 paquetes | $50          |
| **Total**             |            | **$663/mes** |

**Costo infraestructura:** $133/mes → **Margen: $530/mes (80%)** ✅

#### Escenario Moderado (Fase 1)

| Fuente                | Cantidad    | Revenue/mes    |
| --------------------- | ----------- | -------------- |
| Dealers Plan Visible  | 20          | $580           |
| Dealers Plan Pro      | 10          | $890           |
| Dealers Plan Elite    | 2           | $398           |
| Listados individuales | 50          | $1,450         |
| OKLA Coins            | 30 paquetes | $300           |
| Publicidad            | 5 productos | $250           |
| **Total**             |             | **$3,868/mes** |

**Costo infraestructura:** $163/mes → **Margen: $3,705/mes (96%)** ✅

#### Escenario Optimista (Fase 2)

| Fuente                | Cantidad         | Revenue/mes      |
| --------------------- | ---------------- | ---------------- |
| Dealers activos       | 200 (mix planes) | ~$8,000          |
| Listados individuales | 200              | $5,800           |
| OKLA Coins            | 100 paquetes     | $1,000           |
| Publicidad            | 30 productos     | $2,000           |
| **Total**             |                  | **~$16,800/mes** |

**Costo infraestructura:** $338/mes → **Margen: $16,462/mes (98%)** ✅

---

## 6. Resumen Ejecutivo

### Costos Totales por Fase (Infra + AI)

| Fase       | Usuarios | Infra/mes | AI/mes | Total/mes  | Revenue est. | Margen |
| ---------- | -------- | --------- | ------ | ---------- | ------------ | ------ |
| Desarrollo | 0        | $126      | $3     | **$129**   | $0           | -$129  |
| 0 (MVP)    | 0-500    | $133      | $3     | **$136**   | $663         | 79%    |
| 1          | 500-2K   | $163      | $15    | **$178**   | $3,868       | 95%    |
| 2          | 2K-10K   | $338      | $72    | **$410**   | $16,800      | 98%    |
| 3          | 10K-50K  | $750      | $280   | **$1,030** | $50,000+     | 98%+   |

### Decisiones Clave

1. **DO Spaces para media** — Ahorrar $85+/mes en bandwidth cuando escale
2. **Modelo híbrido Claude** — Haiku para FAQ/simple, Sonnet solo para ventas complejas
3. **Mantener in-cluster**: Redis y RabbitMQ (migrar a managed en Fase 2+)
4. **LLM Server eliminado** — API directa a Anthropic es más económico que self-hosted
5. **Break-even**: Con solo **2 dealers Pro + 5 listados** = cubrir infraestructura

---

_Documento actualizado: 2025-07-14_
