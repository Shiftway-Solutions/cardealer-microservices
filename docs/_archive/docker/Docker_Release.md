# ============================================================================

# PLAN DE DESPLIEGUE DE MICROSERVICIOS - OKLA

# ============================================================================

# FASE 0 - Infraestructura Base + Core

# Infraestructura

docker compose up -d postgres_db redis rabbitmq consul seq jaeger

# Servicios Core

docker compose up -d authservice userservice vehiclessaleservice mediaservice contactservice notificationservice

# Gateway + Frontend

docker compose up -d gateway frontend-next

# FASE 1 - Críticos

docker compose up -d errorservice roleservice billingservice searchservice

# FASE 2 - Marketplace

docker compose up -d dealermanagementservice reviewservice alertservice comparisonservice maintenanceservice

# FASE 3 - Analytics

docker compose up -d eventtrackingservice dealeranalyticsservice inventorymanagementservice leadscoringservice

# FASE 4 - Pagos

docker compose up -d stripepaymentservice azulpaymentservice invoicingservice escrowservice

# FASE 5 - IA

docker compose up -d chatbotservice recommendationservice vehicleintelligenceservice backgroundremovalservice

# FASE 6 - Auxiliares

docker compose up -d schedulerservice auditservice crmservice reportingservice reportsservice
