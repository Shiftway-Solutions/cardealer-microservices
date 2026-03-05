# 📊 Priorización de Servicios en Desarrollo - OKLA Marketplace

**Fecha:** Enero 7, 2026  
**Total de Servicios en Desarrollo:** 29  
**Objetivo:** Marketplace de venta de vehículos

---

## 🎯 PRIORIDAD CRÍTICA (Top 5)
> Funcionalidades esenciales que impactan directamente conversión y experiencia del usuario

### 1. SearchService ⭐⭐⭐⭐⭐
- **Puerto:** 5027
- **Importancia:** CRÍTICA
- **Tiempo estimado:** 2-3 semanas
- **Dependencias:** Elasticsearch, VehiclesSaleService
- **¿Por qué primero?**
  - Búsqueda avanzada con filtros es esencial para marketplace
  - Los usuarios necesitan encontrar vehículos por marca, modelo, precio, año, kilometraje
  - Sin búsqueda efectiva, navegar 100+ vehículos es imposible
  - Autocomplete y búsqueda semántica mejoran UX dramáticamente
- **Funcionalidades clave:**
  - Full-text search con Elasticsearch
  - Filtros facetados (make, model, year, price range, body type)
  - Autocomplete
  - Búsqueda por ubicación (propiedades futuras)
  - Ordenamiento múltiple (precio, año, relevancia)

---

### 2. ContactService ⭐⭐⭐⭐⭐
- **Puerto:** 5026
- **Importancia:** CRÍTICA
- **Tiempo estimado:** 2 semanas
- **Dependencias:** UserService, NotificationService
- **¿Por qué?**
  - Comunicación comprador-vendedor es esencial para conversión
  - Sistema de mensajería interno genera confianza
  - Actualmente solo hay formulario básico sin seguimiento
- **Funcionalidades clave:**
  - Formulario de inquiry inicial
  - Sistema de chat/mensajería
  - Historial de conversaciones
  - Notificaciones push/email cuando llega mensaje
  - Protección de datos de contacto (no exponer email/teléfono inmediato)

---

### 3. AppointmentService ⭐⭐⭐⭐⭐
- **Puerto:** 5025
- **Importancia:** CRÍTICA
- **Tiempo estimado:** 2 semanas
- **Dependencias:** NotificationService, UserService
- **¿Por qué?**
  - Agendar test drives es paso clave en proceso de compra
  - Organiza visitas y reduce no-shows
  - Mejora experiencia profesional del marketplace
- **Funcionalidades clave:**
  - Calendario de disponibilidad
  - Reserva de test drives
  - Recordatorios automáticos (24h y 2h antes)
  - Check-in digital
  - Tours de propiedades (futuro)

---

### 4. FinanceService ⭐⭐⭐⭐⭐
- **Puerto:** 5024
- **Importancia:** CRÍTICA
- **Tiempo estimado:** 3 semanas
- **Dependencias:** BillingService, UserService
- **¿Por qué?**
  - 70% de compradores de vehículos necesitan financiamiento
  - Calculadora de cuotas aumenta engagement
  - Pre-aprobación acelera proceso de compra
- **Funcionalidades clave:**
  - Calculadora de préstamos (cuota mensual, intereses)
  - Solicitud de financiamiento
  - Integración con entidades financieras
  - Pre-aprobación automática
  - Comparador de ofertas de préstamos

---

### 5. InvoicingService ⭐⭐⭐⭐⭐
- **Puerto:** 5028
- **Importancia:** CRÍTICA
- **Tiempo estimado:** 2-3 semanas
- **Dependencias:** BillingService
- **¿Por qué?**
  - Facturación con NCF (DGII) es **obligatorio** en República Dominicana
  - Compliance legal para transacciones formales
  - Profesionalismo y tracking de ventas
- **Funcionalidades clave:**
  - Generación de facturas con NCF (B01, B02, B14, B15)
  - Cálculo automático de ITBIS (18%)
  - PDFs con código QR
  - Validación de RNC
  - Reportes 606/607 para DGII

---

## 🚀 PRIORIDAD ALTA (5)
> Funcionalidades que mejoran operaciones y aceleran crecimiento

### 6. CRMService ⭐⭐⭐⭐
- **Puerto:** 5030
- **Tiempo estimado:** 2 semanas
- **Dependencias:** ContactService, UserService
- **Funcionalidades:**
  - Gestión de leads (captura desde formularios)
  - Pipeline de ventas (New → Contacted → Qualified → Converted)
  - Lead scoring automático
  - Registro de actividades (llamadas, emails, reuniones)
  - Follow-up automático

---

### 7. AdminService ⭐⭐⭐⭐
- **Puerto:** 5029
- **Tiempo estimado:** 2-3 semanas
- **Dependencias:** UserService, VehiclesSaleService
- **Funcionalidades:**
  - Dashboard administrativo centralizado
  - Gestión de usuarios (suspender, activar, verificar)
  - Moderación de contenido (aprobar/rechazar listings)
  - Configuración del sistema
  - Estadísticas en tiempo real

---

### 8. IntegrationService ⭐⭐⭐⭐
- **Puerto:** 5037
- **Tiempo estimado:** 3-4 semanas
- **Dependencias:** VehiclesSaleService
- **Funcionalidades:**
  - **Carfax/AutoCheck:** Reportes de historial de vehículos
  - **Facebook Marketplace:** Publicación automática
  - **Google Maps:** Geocoding
  - **Zapier:** Automatizaciones
  - **HubSpot:** Sincronización CRM

---

### 9. MarketingService ⭐⭐⭐⭐
- **Puerto:** 5034
- **Tiempo estimado:** 2 semanas
- **Dependencias:** NotificationService, UserService
- **Funcionalidades:**
  - Campañas de email marketing
  - Cupones y promociones
  - Landing pages
  - A/B testing
  - Segmentación de audiencias

---

### 10. ReportsService ⭐⭐⭐⭐
- **Puerto:** 5031
- **Tiempo estimado:** 2 semanas
- **Dependencias:** VehiclesSaleService, UserService, BillingService
- **Funcionalidades:**
  - Reportes de ventas
  - Inventario y rotación
  - KPIs del negocio
  - Dashboards personalizados
  - Reportes programados (diarios/semanales)

---

## 📊 PRIORIDAD MEDIA (9)
> Optimización, experiencia mejorada y eficiencia operacional

### 11. RateLimitingService ⭐⭐⭐
- **Puerto:** 5006 (ya existe pero mejorar)
- **Tiempo estimado:** 1 semana
- **Funcionalidades:** Protección contra abuso de API, scraping, DDoS

### 12. FileStorageService ⭐⭐⭐
- **Puerto:** 5035
- **Tiempo estimado:** 2 semanas
- **Funcionalidades:** Gestión avanzada de archivos con versionado, virus scanning

### 13. SchedulerService ⭐⭐⭐
- **Puerto:** 5038
- **Tiempo estimado:** 1-2 semanas
- **Funcionalidades:** Cron jobs automáticos (cleanup, backups, reportes)

### 14. FeatureToggleService ⭐⭐⭐
- **Puerto:** 5008 (ya existe pero mejorar)
- **Tiempo estimado:** 1 semana
- **Funcionalidades:** Feature flags, deploy incremental, A/B testing

### 15. CacheService ⭐⭐⭐
- **Puerto:** 5001 (ya existe en desarrollo)
- **Tiempo estimado:** 1 semana
- **Funcionalidades:** Abstracción de Redis para caching distribuido

### 16. AuditService ⭐⭐⭐
- **Puerto:** 5032
- **Tiempo estimado:** 2 semanas
- **Funcionalidades:** Audit trail, compliance, seguridad, GDPR

### 17. ApiDocsService ⭐⭐⭐
- **Puerto:** 5033
- **Tiempo estimado:** 1-2 semanas
- **Funcionalidades:** Portal de desarrolladores, Swagger consolidado

### 18. BackupDRService ⭐⭐⭐
- **Puerto:** 5036
- **Tiempo estimado:** 2 semanas
- **Funcionalidades:** Backups automáticos, disaster recovery, RPO/RTO

### 19. IdempotencyService ⭐⭐
- **Puerto:** 5009 (ya existe pero mejorar)
- **Tiempo estimado:** 1 semana
- **Funcionalidades:** Prevención de operaciones duplicadas (pagos, recursos)

---

## 🔧 PRIORIDAD BAJA (6)
> Infraestructura de soporte - Abstracciones adicionales sobre lo que ya funciona

### 20. LoggingService
- **Razón baja prioridad:** Serilog con Elasticsearch ya funciona en producción

### 21. MessageBusService
- **Razón baja prioridad:** RabbitMQ directo ya funciona en todos los servicios

### 22. HealthCheckService
- **Razón baja prioridad:** Health checks `/health` ya existen en cada servicio

### 23. TracingService
- **Razón baja prioridad:** OpenTelemetry ya implementado con Jaeger

### 24. ConfigurationService
- **Razón baja prioridad:** ConfigMaps y Secrets de Kubernetes ya funcionan

### 25. ServiceDiscovery
- **Razón baja prioridad:** Kubernetes DNS ya resuelve service discovery

---

## ❌ PRIORIDAD MUY BAJA (4)
> Fuera del scope actual - Expansión futura

### 26. VehiclesRentService
- **Razón:** Renta de vehículos es modelo de negocio diferente (no venta)

### 27. PropertiesSaleService
- **Razón:** Venta de propiedades es vertical completamente diferente

### 28. PropertiesRentService
- **Razón:** Alquiler de propiedades fuera del foco actual

### 29. RealEstateService
- **Razón:** Agregador de real estate para expansión a largo plazo

---

## 📅 ROADMAP RECOMENDADO

### 🏃 Sprint 1-2 (4-6 semanas) - CORE MARKETPLACE
**Objetivo:** Funcionalidad básica de búsqueda y contacto

1. **SearchService** ← **EMPIEZA AQUÍ**
2. **ContactService**
3. **AppointmentService**

**Resultado:** Los usuarios pueden buscar vehículos efectivamente, contactar vendedores y agendar test drives.

---

### 🏃 Sprint 3-4 (4-6 semanas) - CONVERSIÓN Y LEGAL
**Objetivo:** Facilitar compra y cumplir regulaciones

4. **FinanceService**
5. **InvoicingService** (Obligatorio para DGII)
6. **CRMService**

**Resultado:** Los usuarios pueden solicitar financiamiento, los vendedores gestionan leads, y todas las transacciones tienen factura legal.

---

### 🏃 Sprint 5-6 (4-6 semanas) - CRECIMIENTO Y OPERACIONES
**Objetivo:** Escalar operaciones y aumentar tráfico

7. **AdminService**
8. **IntegrationService** (Carfax + Facebook Marketplace)
9. **MarketingService**

**Resultado:** Administración centralizada, reportes de historial de vehículos, publicación automática en Facebook, campañas de marketing.

---

### 🏃 Sprint 7+ - OPTIMIZACIÓN
**Objetivo:** Eficiencia operacional y analytics

10. ReportsService
11. RateLimitingService
12. SchedulerService
13. FileStorageService
14. Resto según necesidad del negocio

---

## 🎯 RECOMENDACIÓN FINAL

### Empieza con: **SearchService**

**¿Por qué SearchService primero?**

✅ **Impacto inmediato en UX:** Es la funcionalidad #1 más solicitada por usuarios  
✅ **Mejora engagement:** Los usuarios pasan más tiempo explorando inventario  
✅ **No es bloqueante:** Puede desarrollarse en paralelo con otros servicios  
✅ **Complejidad moderada:** 2-3 semanas (no es excesivamente complejo)  
✅ **Independiente:** No depende de servicios críticos que no existen  
✅ **ROI alto:** Aumenta conversión al facilitar que usuarios encuentren lo que buscan  

**Después de SearchService:**
- **ContactService** (2 semanas) - Alta prioridad y rápido de implementar → Quick Win
- **AppointmentService** (2 semanas) - Complementa el flujo de compra

**Criterios de Priorización Usados:**
1. **Impacto en conversión de ventas**
2. **Experiencia del usuario**
3. **Compliance legal (DGII)**
4. **Independencia técnica**
5. **Tiempo de desarrollo**
6. **ROI esperado**

---

## 📈 MÉTRICAS DE ÉXITO

### SearchService
- [ ] Usuarios pueden filtrar por 8+ criterios
- [ ] Tiempo de búsqueda < 200ms
- [ ] Autocomplete funcional
- [ ] 80% de búsquedas retornan resultados

### ContactService
- [ ] 50% de listings reciben al menos 1 inquiry
- [ ] 70% de inquiries reciben respuesta en < 24h
- [ ] Tiempo promedio de respuesta < 4h

### AppointmentService
- [ ] 30% de inquiries resultan en appointment
- [ ] Tasa de no-show < 20%
- [ ] 90% de appointments confirmados reciben recordatorio

---

## 🆕 SERVICIOS DE DATA & ML (Enero 8, 2026)

> **NUEVO:** Se han definido 9 microservicios adicionales para recopilar datos,
> entrenar modelos de ML y ofrecer features inteligentes a dealers y compradores.

### Servicios Críticos de Data & ML

| # | Servicio | Puerto | Prioridad | Descripción |
|---|----------|--------|-----------|-------------|
| 1 | **EventTrackingService** | 5050 | ⭐⭐⭐⭐⭐ | Captura TODOS los eventos de usuario |
| 2 | **DataPipelineService** | 5051 | ⭐⭐⭐⭐⭐ | ETL, transformaciones, agregaciones |
| 3 | **UserBehaviorService** | 5052 | ⭐⭐⭐⭐⭐ | Perfiles de comportamiento y segmentos |
| 4 | **FeatureStoreService** | 5053 | ⭐⭐⭐⭐⭐ | Features centralizados para ML |
| 5 | **RecommendationService** | 5054 | ⭐⭐⭐⭐⭐ | "Vehículos para ti", "Similar" |
| 6 | **LeadScoringService** | 5055 | ⭐⭐⭐⭐⭐ | Hot/Warm/Cold leads para dealers |
| 7 | **VehicleIntelligenceService** | 5056 | ⭐⭐⭐⭐ | Pricing óptimo, predicción de demanda |
| 8 | **MLTrainingService** | 5057 | ⭐⭐⭐ | Pipeline de entrenamiento de modelos |
| 9 | **ListingAnalyticsService** | 5058 | ⭐⭐⭐⭐⭐ | Estadísticas de publicaciones |
| 10 | **ReviewService** | 5059 | ⭐⭐⭐⭐⭐ | Reviews estilo Amazon para dealers/vendedores |
| 11 | **ChatbotService** | 5060 | ⭐⭐⭐⭐⭐ | **Chatbot IA + Calificación leads + WhatsApp** |

### Impacto por Tipo de Usuario

**Para Compradores:**
- "Vehículos para ti" personalizados
- "Usuarios también vieron"
- Alertas de nuevos listings que coinciden con preferencias
- Indicador de "precio justo"
- ⭐ **Reviews de vendedores**: Ver reputación antes de comprar
- 🤖 **Chatbot 24/7**: Respuesta inmediata a preguntas del vehículo

**Para Vendedores Individuales:**
- Compradores potenciales interesados
- Precio sugerido basado en mercado
- Tips para mejorar el listing
- 📊 **Estadísticas de vistas**: "Tu vehículo tiene 156 vistas, 12 contactos"
- ⭐ **Reviews de clientes**: Construir reputación
- 🤖 **Leads pre-calificados**: Solo recibes leads serios

**Para Dealers (MÁXIMO VALOR):**
- 🔥 Lead Scoring: "Este lead es HOT - 92% probabilidad de compra"
- 👥 Compradores potenciales: "3 usuarios muy interesados en tu Toyota"
- 💰 Pricing inteligente: "Tu precio está 15% arriba del mercado"
- 📊 Predicción de demanda: "Toyota RAV4 tiene alta demanda"
- ⏱️ Tiempo estimado de venta: "Este vehículo se venderá en 18 días"
- 📈 **Dashboard de estadísticas**: Vistas, contactos, test drives por vehículo
- ⭐ **Reviews y badges**: "Top Rated Seller", "Trusted Dealer"
- 🤖 **Chatbot inteligente**: Filtra leads fríos, transfiere HOT a WhatsApp

### ChatbotService - Asistente Virtual con Calificación de Leads

```
🤖 OKLA Bot: ¡Hola! Veo que estás viendo este Toyota RAV4. ¿Te puedo ayudar?

👤 Usuario: Cuánto cuesta?

🤖 OKLA Bot: El Toyota RAV4 2024 está en $42,500. 
              ¿Es para uso personal o negocio?

👤 Usuario: Personal, necesito cambiar mi carro urgente

🤖 OKLA Bot: Entiendo la urgencia 👍 ¿Tienes vehículo para trade-in?

👤 Usuario: Sí, un Honda CRV 2019

🤖 OKLA Bot: ¡Perfecto! Te conecto con Juan del dealer por WhatsApp...

[Sistema: Lead calificado como HOT (92%) - Transferido a WhatsApp]
```

> 📄 Ver documentación completa: [docs/CHATBOT_SERVICE_STRATEGY.md](CHATBOT_SERVICE_STRATEGY.md)

### ReviewService - Sistema de Reviews Estilo Amazon

**Funcionalidades Principales:**
```
⭐ Ratings de 1-5 estrellas con distribución visual
⭐ Reviews con título, texto y fotos
⭐ "Compra verificada" badge
⭐ Respuestas del vendedor a reviews
⭐ Votos de utilidad ("45 personas encontraron útil esto")
⭐ Filtrar por estrellas
⭐ Badges: "Top Rated", "Trusted Dealer", "Fast Responder"
⭐ Solicitud automática de review después de compra
⭐ Moderación anti-spam
```

**Vista del Vendedor:**
```
📊 Tu reputación: ⭐⭐⭐⭐⭐ 4.7/5 (156 reviews)
   5⭐ ████████████████████ 77%
   4⭐ ████████             16%
   3⭐ ███                   5%
   2⭐ █                     1%
   1⭐ █                     1%
   
   🏆 Badges: Top Rated Seller | Trusted Dealer
```

### ListingAnalyticsService - Detalle

**Funcionalidad Principal:** Mostrar a dealers y vendedores cuántas personas han visto sus publicaciones.

**Para Vendedor Individual (Vista Simple):**
```
📊 Tu Honda Civic 2020
   👁️ 156 vistas totales (+23 esta semana)
   📱 12 contactos recibidos
   ❤️ 8 favoritos | 🔗 5 compartidos
   📈 Rendimiento: Top 30% en tu categoría
```

**Para Dealer (Dashboard Completo):**
```
📊 Dashboard AutoMax Dealer
   👁️ 12,456 vistas | 📱 892 contactos | 📅 156 test drives
   
   Top 5 vehículos más vistos:
   1. Toyota RAV4 2022 - 856 vistas, 45 contactos
   2. Honda CR-V 2021 - 654 vistas, 32 contactos
   ...
   
   ⚠️ Vehículos que necesitan atención:
   • Mazda 3 2019 - Solo 23 vistas (promedio: 150)
```

### Roadmap de Implementación Data & ML

**Fase 1 (Semanas 1-4):** Fundamentos
- EventTrackingService + SDKs (Web/Mobile)

**Fase 2 (Semanas 5-8):** Procesamiento
- DataPipelineService + UserBehaviorService + FeatureStoreService

**Fase 3 (Semanas 9-14):** ML Básico
- LeadScoringService + RecommendationService + VehicleIntelligenceService

**Fase 4 (Semanas 15-20):** ML Avanzado
- MLTrainingService + A/B Testing + Modelos avanzados

> Ver documento completo: [DATA_ML_MICROSERVICES_STRATEGY.md](DATA_ML_MICROSERVICES_STRATEGY.md)

---

**Última actualización:** Enero 8, 2026  
**Autor:** Equipo de Desarrollo OKLA  
**Revisión:** Pendiente aprobación de Product Owner
