# 📚 Tutorial: Sistema de Publicidad de OKLA

**Versión**: 1.0  
**Fecha**: 5 de marzo de 2026  
**Audiencia**: Equipo técnico, operaciones, soporte

---

## 📋 Tabla de Contenidos

1. [Arquitectura General](#1-arquitectura-general)
2. [Catálogo de Productos Publicitarios](#2-catálogo-de-productos-publicitarios)
3. [Ciclo de Vida de una Campaña](#3-ciclo-de-vida-de-una-campaña)
4. [Tracking de Impresiones y Clics](#4-tracking-de-impresiones-y-clics)
5. [Sistema de Rotación de Anuncios](#5-sistema-de-rotación-de-anuncios)
6. [Reportes de Publicidad](#6-reportes-de-publicidad)
7. [Reportes Mensuales Automáticos](#7-reportes-mensuales-automáticos)
8. [Homepage: Secciones, Marcas y Categorías](#8-homepage-secciones-marcas-y-categorías)
9. [Monitoreo de Actividad de Usuarios](#9-monitoreo-de-actividad-de-usuarios)
10. [Retargeting en Redes Sociales](#10-retargeting-en-redes-sociales)
11. [API Endpoints](#11-api-endpoints)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. Arquitectura General

### Servicios Involucrados

```
┌─────────────────────┐     ┌──────────────────────┐     ┌─────────────────────┐
│   Frontend (Next.js) │────▶│    Gateway (Ocelot)   │────▶│  AdvertisingService │
│                      │     │                       │     │  (Puerto 8080)      │
└─────────────────────┘     └──────────────────────┘     └──────┬──────────────┘
                                                                │
                                    ┌───────────────────────────┼──────────────────┐
                                    │                           │                  │
                              ┌─────▼──────┐           ┌───────▼────────┐  ┌──────▼───────┐
                              │  PostgreSQL │           │ NotificationSvc │  │  UserService  │
                              │  (DB)       │           │ (Emails)        │  │ (Perfiles)    │
                              └────────────┘           └────────────────┘  └──────────────┘
```

### Entidades Principales en la Base de Datos

| Entidad          | Descripción                                          |
| ---------------- | ---------------------------------------------------- |
| `AdCampaign`     | Campaña publicitaria con presupuesto, fechas, estado |
| `AdImpression`   | Cada vez que un anuncio se muestra a un usuario      |
| `AdClick`        | Cada vez que un usuario hace clic en un anuncio      |
| `RotationConfig` | Configuración de cómo rotan los anuncios por sección |
| `CategoryConfig` | Categorías del homepage (SUV, Sedán, etc.)           |
| `BrandConfig`    | Marcas del homepage (Toyota, Honda, etc.)            |

---

## 2. Catálogo de Productos Publicitarios

Los productos publicitarios disponibles se consultan en:

```
GET /api/advertising/catalog
```

### Productos Disponibles

| Producto              | Slug                 | Precio/Día | Precio/Mes | Alcance      |
| --------------------- | -------------------- | ---------- | ---------- | ------------ |
| Listing Destacado     | `listing-destacado`  | $0.50      | $6.00      | Por vehículo |
| Top 3 en Búsquedas    | `top-3-busquedas`    | $1.50      | $20.00     | Por vehículo |
| Oferta del Día        | `oferta-del-dia`     | $3.00      | $45.00     | Por vehículo |
| Banner Homepage       | `banner-homepage`    | $5.00      | $80.00     | Global       |
| Showcase de Dealer    | `showcase-dealer`    | $4.00      | $60.00     | Por dealer   |
| Email Alerts          | `email-alerts`       | $2.00      | $30.00     | Por vehículo |
| Bundle de Visibilidad | `bundle-visibilidad` | $8.00      | $120.00    | Bundle       |

### Tipos de Precios

| Modelo           | Descripción                                                       |
| ---------------- | ----------------------------------------------------------------- |
| **PerView**      | Se cobra por cada impresión. Se compran "vistas" (viewsPurchased) |
| **FixedDaily**   | Precio fijo por día                                               |
| **FixedWeekly**  | Precio fijo por semana                                            |
| **FixedMonthly** | Precio fijo por mes                                               |

---

## 3. Ciclo de Vida de una Campaña

```
  PendingPayment ──▶ Active ──▶ Completed
       │                │           ▲
       │                ▼           │
       │             Paused ────────┘
       │                │
       │                ▼
       └──────────▶ Cancelled
                        │
                     Expired ◀── (automático cuando pasa la fecha de fin)
```

### Crear una Campaña

```bash
POST /api/advertising/campaigns
Authorization: Bearer {dealer_token}

{
    "vehicleId": "guid-del-vehiculo",
    "ownerId": "guid-del-dealer",
    "ownerType": "Dealer",
    "campaignType": "FeaturedSpot",
    "pricingModel": "FixedMonthly",
    "fixedPriceAmount": 6.00,
    "totalBudget": 6.00,
    "startDate": "2026-03-01",
    "endDate": "2026-03-31"
}
```

### Acciones sobre Campañas

| Acción   | Endpoint                                      | Descripción              |
| -------- | --------------------------------------------- | ------------------------ |
| Pausar   | `POST /api/advertising/campaigns/{id}/pause`  | Detiene temporalmente    |
| Reanudar | `POST /api/advertising/campaigns/{id}/resume` | Reactiva campaña pausada |
| Cancelar | `POST /api/advertising/campaigns/{id}/cancel` | Cancela definitivamente  |

### Expiración Automática

El sistema tiene dos jobs que verifican expiración:

1. **CampaignExpirationJob** — Se ejecuta cada hora, verifica:
   - Si `EndDate < DateTime.UtcNow` → marca como `Expired`
   - Si las vistas compradas se agotaron → marca como `Completed`

2. **DailyAdReportJob** — También verifica expiración como parte del reporte diario

---

## 4. Tracking de Impresiones y Clics

### Registrar una Impresión

```bash
POST /api/advertising/tracking/impression
# No requiere autenticación — rate limit: 100 req/min/IP

{
    "campaignId": "guid-de-la-campaña",
    "sessionId": "sesion-del-usuario",
    "section": "homepage"
}
```

**Protección anti-duplicados**: Redis con clave `impression:{campaignId}:{sessionId}` y TTL de 24 horas. Si la misma sesión ya vio el anuncio hoy, no se registra otra impresión.

### Registrar un Clic

```bash
POST /api/advertising/tracking/click
# No requiere autenticación — rate limit: 100 req/min/IP

{
    "campaignId": "guid-de-la-campaña",
    "redirectUrl": "https://okla.com.do/vehiculo/123"
}
```

### Métricas Calculadas

| Métrica                      | Fórmula                                  |
| ---------------------------- | ---------------------------------------- |
| **CTR** (Click-Through Rate) | `clicks / impressions × 100`             |
| **Gasto**                    | Para PerView: `viewsUsed × pricePerView` |
| **Vistas restantes**         | `viewsPurchased - viewsUsed`             |

---

## 5. Sistema de Rotación de Anuncios

### Cómo funciona

El sistema rota los anuncios en diferentes secciones del sitio usando un algoritmo ponderado:

```
GET /api/advertising/rotation/{section}
# Devuelve las campañas activas para esa sección, ordenadas por quality score
```

### Quality Score

Cada campaña tiene un `qualityScore` (0 a 1.00) que determina su prioridad en la rotación:

- Campañas nuevas: `0.50` (neutral)
- Campañas con buen CTR: score más alto
- Campañas con bajo engagement: score decrece

### Refresh automático

El **RotationRefreshJob** ejecuta cada hora y actualiza el caché Redis de las rotaciones.

### Configurar la rotación

```bash
GET /api/advertising/rotation/config/{section}    # Ver configuración
PUT /api/advertising/rotation/config              # Actualizar configuración
POST /api/advertising/rotation/refresh            # Forzar refresh del caché
```

---

## 6. Reportes de Publicidad

### Para Dealers/Vendedores (Owner Report)

```bash
GET /api/advertising/reports/owner/{ownerId}?ownerType=Dealer&daysBack=30
Authorization: Bearer {token}
```

**Respuesta:**

```json
{
    "success": true,
    "data": {
        "ownerId": "...",
        "ownerType": "Dealer",
        "activeCampaigns": 3,
        "totalCampaigns": 12,
        "totalImpressions": 45230,
        "totalClicks": 1890,
        "overallCtr": 0.0418,
        "totalSpent": 156.50,
        "dailyImpressions": [...],
        "dailyClicks": [...]
    }
}
```

### Reporte de Campaña Individual

```bash
GET /api/advertising/reports/campaign/{campaignId}?daysBack=30
Authorization: Bearer {token}
```

### Reporte de Plataforma (Solo Admin)

```bash
GET /api/advertising/reports/platform?daysBack=30
Authorization: Bearer {admin_token}
```

**Datos incluidos:**

- Total de campañas activas en la plataforma
- Impresiones y clics totales
- CTR promedio
- Revenue total
- Número de anunciantes activos

### Reporte de Precios

```bash
GET /api/advertising/reports/pricing/{placementType}
# Sin autenticación — información pública de precios
```

---

## 7. Reportes Mensuales Automáticos

### ¿Qué son?

Cada **1ro del mes a las 6:00 AM (hora RD)**, el sistema genera automáticamente un reporte de rendimiento publicitario y lo envía por email a todos los dealers y vendedores que tienen (o tuvieron) campañas activas.

### Flujo del Reporte Mensual

```
1ro del mes, 6:00 AM RD
         │
         ▼
   MonthlyAdReportJob
         │
         ├── Obtiene stats de plataforma del mes anterior
         │
         ├── Lista todos los owners con campañas
         │
         └── Para cada owner:
              ├── Obtiene email del UserService
              ├── Genera reporte de rendimiento del mes
              ├── Renderiza HTML del email
              └── Envía via NotificationService (/api/notifications/email)
```

### Contenido del Email

El email mensual incluye:

- **📊 Impresiones del mes** — cuántas veces se mostraron sus anuncios
- **🖱️ Clics del mes** — cuántos usuarios hicieron clic
- **📈 CTR** — tasa de clics (Click-Through Rate)
- **💰 Inversión** — cuánto gastó en publicidad ese mes
- **📋 Campañas activas** vs total de campañas
- **💡 Consejos** — tips para mejorar el rendimiento

### Reporte Diario (complementario)

También existe un reporte diario que se envía a las **8:00 AM (hora RD)** con el resumen del día anterior.

### Configuración

Los jobs están hardcodeados en el código:

- **MonthlyAdReportJob**: `new TimeSpan(10, 0, 0)` UTC = 6:00 AM RD
- **DailyAdReportJob**: `new TimeSpan(12, 0, 0)` UTC = 8:00 AM RD

### Archivos clave

| Archivo                                                                  | Descripción                  |
| ------------------------------------------------------------------------ | ---------------------------- |
| `AdvertisingService.Infrastructure/BackgroundJobs/MonthlyAdReportJob.cs` | Job mensual                  |
| `AdvertisingService.Infrastructure/BackgroundJobs/DailyAdReportJob.cs`   | Job diario                   |
| `AdvertisingService.Application/Clients/NotificationServiceClient.cs`    | Cliente de notificaciones    |
| `AdvertisingService.Application/Clients/UserServiceClient.cs`            | Cliente para resolver emails |

---

## 8. Homepage: Secciones, Marcas y Categorías

### Secciones del Homepage

```bash
GET /api/homepagesections/homepage
# Sin autenticación — devuelve las secciones del homepage con los vehículos
```

Cada sección puede contener hasta 10 vehículos destacados.

### Categorías

```bash
GET /api/advertising/homepage/categories?activeOnly=true
# Devuelve: SUV, Sedán, Camioneta, Deportivo, Eléctrico, Pickup
```

### Marcas

```bash
GET /api/advertising/homepage/brands?activeOnly=true
# Devuelve: Toyota, Honda, Hyundai, Kia, Nissan, Chevrolet, etc.
```

### Administrar (Solo Admin)

```bash
PUT /api/advertising/homepage/categories   # Actualizar categorías
PUT /api/advertising/homepage/brands       # Actualizar marcas
```

---

## 9. Monitoreo de Actividad de Usuarios

### Estado Actual

El **EventTrackingService** está desarrollado pero **NO está desplegado** en el cluster de Kubernetes.

### Capacidades del EventTrackingService (cuando se despliegue)

| Evento             | Descripción                                              |
| ------------------ | -------------------------------------------------------- |
| `PageViewEvent`    | Cada página que visita el usuario                        |
| `SearchEvent`      | Cada búsqueda de vehículos (marca, modelo, precio, etc.) |
| `VehicleViewEvent` | Cada vez que un usuario ve los detalles de un vehículo   |
| `FilterEvent`      | Cada vez que aplica un filtro de búsqueda                |

### Datos que se Capturan

- **sessionId** — ID de la sesión del usuario
- **userId** — Si está autenticado
- **deviceType** — móvil, desktop, tablet
- **location** — geolocalización aproximada
- **timestamp** — fecha y hora exacta
- **metadata** — datos adicionales del evento

### Servicios Relacionados No Desplegados

| Servicio                 | Estado           | Descripción                                    |
| ------------------------ | ---------------- | ---------------------------------------------- |
| `EventTrackingService`   | ❌ No desplegado | Tracking de actividad de usuarios              |
| `DealerAnalyticsService` | ❌ No desplegado | Dashboards para dealers, funnels de conversión |
| `ReportsService`         | ❌ No desplegado | Generación y scheduling de reportes            |
| `MarketingService`       | ❌ No desplegado | Campañas de marketing (email, SMS, WhatsApp)   |
| `SchedulerService`       | ❌ No desplegado | Programación de tareas cron                    |

### Cómo Monitorear Ahora (con lo desplegado)

Con los servicios actuales, el monitoreo se hace a través de:

1. **AdvertisingService** — impresiones y clics de campañas
2. **AuditService** — registra acciones administrativas (400+ tipos de acciones)
3. **Frontend analytics** — se puede integrar con herramientas externas (ver sección 10)

---

## 10. Retargeting en Redes Sociales

### ¿Qué es el Retargeting?

Cuando un usuario busca "Toyota Corolla 2024" en OKLA, queremos mostrarle anuncios de Toyota Corolla en Facebook, Instagram y Google cuando navegue otras páginas.

### Estado Actual

**⚠️ No implementado actualmente.**

### Cómo Implementarlo

#### Paso 1: Integrar Facebook Pixel

Agregar en el `<head>` del layout principal de Next.js:

```html
<!-- Facebook Pixel Code -->
<script>
  !function(f,b,e,v,n,t,s){...}(window,document,'script',
  'https://connect.facebook.net/en_US/fbevents.js');
  fbq('init', 'TU_PIXEL_ID');
  fbq('track', 'PageView');
</script>
```

#### Paso 2: Enviar Eventos de Búsqueda

Cuando el usuario busca un vehículo:

```javascript
fbq("track", "Search", {
  search_string: "Toyota Corolla 2024",
  content_category: "Vehicles",
  content_type: "vehicle",
  currency: "DOP",
  value: 850000,
});
```

#### Paso 3: Enviar Eventos de Vista de Vehículo

```javascript
fbq("track", "ViewContent", {
  content_name: "Toyota Corolla 2024",
  content_category: "SUV",
  content_ids: ["vehicle-123"],
  content_type: "vehicle",
  value: 850000,
  currency: "DOP",
});
```

#### Paso 4: Google Ads Remarketing

```html
<!-- Google tag (gtag.js) -->
<script
  async
  src="https://www.googletagmanager.com/gtag/js?id=AW-CONVERSION_ID"
></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag() {
    dataLayer.push(arguments);
  }
  gtag("js", new Date());
  gtag("config", "AW-CONVERSION_ID");
</script>
```

#### Paso 5: Configurar Audiencias en Plataformas

- **Facebook Ads Manager**: Crear audiencias personalizadas basadas en los eventos de búsqueda/vista
- **Google Ads**: Crear listas de remarketing con Google Tag Manager
- **Instagram**: Automático vía Facebook (mismo Pixel)

### Consideraciones de Privacidad

- Requiere **banner de cookies** para cumplir con normativas
- Los usuarios deben poder **opt-out** del tracking
- Documentar en la **política de privacidad** qué datos se recolectan

---

## 11. API Endpoints — Referencia Completa

### Campañas

| Método | Endpoint                                 | Auth   | Descripción               |
| ------ | ---------------------------------------- | ------ | ------------------------- |
| POST   | `/api/advertising/campaigns`             | Bearer | Crear campaña             |
| GET    | `/api/advertising/campaigns?ownerId=X`   | Bearer | Listar campañas del owner |
| GET    | `/api/advertising/campaigns/{id}`        | Bearer | Detalle de campaña        |
| POST   | `/api/advertising/campaigns/{id}/pause`  | Bearer | Pausar                    |
| POST   | `/api/advertising/campaigns/{id}/resume` | Bearer | Reanudar                  |
| POST   | `/api/advertising/campaigns/{id}/cancel` | Bearer | Cancelar                  |

### Tracking

| Método | Endpoint                               | Auth | Rate Limit |
| ------ | -------------------------------------- | ---- | ---------- |
| POST   | `/api/advertising/tracking/impression` | No   | 100/min/IP |
| POST   | `/api/advertising/tracking/click`      | No   | 100/min/IP |

### Reportes

| Método | Endpoint                                                           | Auth           | Descripción           |
| ------ | ------------------------------------------------------------------ | -------------- | --------------------- |
| GET    | `/api/advertising/reports/campaign/{id}?daysBack=30`               | Bearer         | Reporte de campaña    |
| GET    | `/api/advertising/reports/owner/{ownerId}?ownerType=X&daysBack=30` | Bearer         | Reporte de owner      |
| GET    | `/api/advertising/reports/platform?daysBack=30`                    | Bearer (Admin) | Reporte de plataforma |
| GET    | `/api/advertising/reports/pricing/{type}`                          | No             | Precios               |

### Catálogo y Homepage

| Método | Endpoint                                               | Auth | Descripción             |
| ------ | ------------------------------------------------------ | ---- | ----------------------- |
| GET    | `/api/advertising/catalog`                             | No   | Productos publicitarios |
| GET    | `/api/advertising/homepage/categories?activeOnly=true` | No   | Categorías              |
| GET    | `/api/advertising/homepage/brands?activeOnly=true`     | No   | Marcas                  |
| GET    | `/api/homepagesections/homepage`                       | No   | Secciones del homepage  |

### Rotación

| Método | Endpoint                                     | Auth   | Descripción                   |
| ------ | -------------------------------------------- | ------ | ----------------------------- |
| GET    | `/api/advertising/rotation/{section}`        | No     | Obtener anuncios para sección |
| GET    | `/api/advertising/rotation/config/{section}` | Bearer | Configuración de sección      |
| PUT    | `/api/advertising/rotation/config`           | Bearer | Actualizar configuración      |
| POST   | `/api/advertising/rotation/refresh`          | Bearer | Forzar refresh de caché       |

---

## 12. Troubleshooting

### Problema: Las campañas no muestran impresiones

1. Verificar que la campaña está en estado `Active`
2. Verificar que no han expirado las fechas
3. Verificar que hay vistas disponibles (si es PerView)
4. Revisar Redis para deduplicación de impresiones

```bash
kubectl exec -n okla deployment/redis -- redis-cli keys "impression:*" | head -10
```

### Problema: Los reportes mensuales no se envían

1. Verificar que el pod de AdvertisingService está corriendo:

```bash
kubectl logs -n okla -l app=advertisingservice --tail=50 | grep -i "monthly"
```

2. Verificar que NotificationService acepta emails:

```bash
kubectl logs -n okla -l app=notificationservice --tail=20
```

3. Verificar que UserService devuelve emails:

```bash
kubectl exec -n okla deployment/gateway -- wget -qO- http://userservice:8080/api/users/{userId}
```

### Problema: El reporte de owner devuelve 404

- Verificar que la ruta está en `ocelot.prod.json`
- Verificar que el ConfigMap `gateway-ocelot` está actualizado
- Reiniciar el gateway: `kubectl rollout restart deployment/gateway -n okla`

### Problema: CTR siempre muestra 0%

- Verificar que hay impresiones registradas (si impressions = 0, CTR = 0)
- Verificar la fórmula: CTR = clicks / impressions × 100
- Revisar si Redis está bloqueando las impresiones por dedup

---

## 📝 Glosario

| Término           | Definición                                                         |
| ----------------- | ------------------------------------------------------------------ |
| **CTR**           | Click-Through Rate — porcentaje de clics vs impresiones            |
| **Impresión**     | Una vez que un anuncio se muestra a un usuario                     |
| **Clic**          | Cuando un usuario hace clic en un anuncio                          |
| **Quality Score** | Puntuación de calidad de una campaña (0-1.00)                      |
| **PerView**       | Modelo de precio donde se paga por cada impresión                  |
| **Retargeting**   | Mostrar anuncios en redes sociales a usuarios que visitaron OKLA   |
| **Owner**         | El dueño de una campaña (puede ser Individual o Dealer)            |
| **Rotación**      | El sistema que decide qué anuncios mostrar en cada sección         |
| **DLQ**           | Dead Letter Queue — cola para mensajes que no se pudieron procesar |

---

_Este tutorial fue generado automáticamente como parte de la auditoría del sistema publicitario de OKLA._
