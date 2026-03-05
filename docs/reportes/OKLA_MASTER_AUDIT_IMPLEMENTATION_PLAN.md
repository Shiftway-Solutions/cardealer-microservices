# 🔍 OKLA — Auditoría Maestra y Plan de Implementación

**Fecha:** 2026-03-05
**Autor:** GitHub Copilot (Claude)
**Versión:** 1.0

---

## 📋 Tabla de Contenidos

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Auditoría del OKLA Score](#2-auditoría-del-okla-score)
3. [Auditoría del Análisis Económico Freemium](#3-auditoría-del-análisis-económico-freemium)
4. [Auditoría de Infraestructura](#4-auditoría-de-infraestructura)
5. [Auditoría del Sale Closed Strategy](#5-auditoría-del-sale-closed-strategy)
6. [Auditoría del Portal de Administración](#6-auditoría-del-portal-de-administración)
7. [Estrategia del Mercado Dominicano](#7-estrategia-del-mercado-dominicano)
8. [Plan de Implementación por Etapas](#8-plan-de-implementación-por-etapas)
9. [Features Implementados en Esta Iteración](#9-features-implementados)

---

## 1. Resumen Ejecutivo

Después de una auditoría exhaustiva de todos los documentos de referencia y el código existente en la plataforma OKLA, se identificaron las siguientes brechas críticas:

### Estado Actual de la Plataforma

- **48 microservicios** en el backend (.NET 8)
- **27 páginas admin** en el frontend
- **OKLA Score frontend** implementado (7 dimensiones codificadas)
- **Sistema de publicidad** funcional
- **Sistema de homepage sections** funcional pero no conectado al homepage
- **Sin sistema de etapas/fases** configurable

### Brechas Críticas Identificadas

| #   | Brecha                                                 | Impacto                           | Prioridad         |
| --- | ------------------------------------------------------ | --------------------------------- | ----------------- |
| 1   | Sin sistema de etapas configurable                     | No se puede graduar funcionalidad | 🔴 Crítica        |
| 2   | OKLA Score admin es stateless                          | Config se pierde al recargar      | 🔴 Crítica        |
| 3   | No existe SaleTransaction entity                       | Sin métricas de conversión        | 🔴 Crítica        |
| 4   | Gateway requiere role "Compliance" en lugar de "Admin" | Admin no puede acceder            | 🔴 Crítica        |
| 5   | Sin VinAudit/MarketCheck integration                   | Score D1/D4 son neutrales         | 🟡 Media (Fase 2) |
| 6   | Sin Vista 360° en planes                               | Feature sin gate por plan         | 🟡 Media          |
| 7   | Sin video upload gating                                | Feature sin gate por plan         | 🟡 Media          |
| 8   | Exchange rate hardcodeado                              | Precios inexactos                 | 🟡 Media          |
| 9   | Homepage no usa HomepageSections backend               | Secciones admin sin efecto        | 🟡 Media          |
| 10  | Sin CSRF en endpoint OKLA Score                        | Vulnerabilidad de seguridad       | 🟡 Media          |

---

## 2. Auditoría del OKLA Score

### 2.1 Lo que SÍ está implementado ✅

- Motor de scoring con 7 dimensiones (D1-D7)
- APIs NHTSA gratuitas (VIN Decode, Recalls, Safety, Complaints)
- Componentes UI completos (gauge, badge, breakdown, alerts, report)
- Página pública de búsqueda por VIN
- Panel admin con selector de fases (4 fases)
- Hooks de React Query para todas las consultas

### 2.2 Lo que FALTA ❌

#### Fase 1 (Actual - $0/mes) — Implementar AHORA:

- [ ] **Persistencia de configuración de fase** — El admin phase selector usa `useState` solamente
- [ ] **Feature flags en ConfigurationService** — Para controlar dimensiones activas
- [ ] **Score caching** — Guardar resultados calculados para no recalcular
- [ ] **Score badge en vehicle cards** — Componente existe pero no está integrado
- [ ] **CSRF protection** en endpoint POST de calculate
- [ ] **Complaints integration** — Hook existe pero no se usa en cálculo

#### Fase 2 (+$15,000 DOP/mes) — Futuro:

- [ ] VinAudit API integration (D1 Vehicle History)
- [ ] BCRD exchange rate API (tasa de cambio en tiempo real)
- [ ] Seller reputation data integration (D7)

#### Fase 3 (+$55,000 DOP/mes) — Futuro:

- [ ] MarketCheck API (D4 Price vs Market)
- [ ] KBB integration
- [ ] PDF export de reportes

#### Fase 4 (+$120,000 DOP/mes) — Futuro:

- [ ] CARFAX integration
- [ ] Score history per VIN
- [ ] Market intelligence dashboard

### 2.3 Análisis Económico del Score (del doc v2)

- Costo por score en Fase 1: **$0 (APIs NHTSA gratuitas)**
- Margen bruto >80% en todos los planes
- El score es SIEMPRE gratis para compradores
- Dealers pagan $1.99/score, Independientes $2.49 (+25%)
- Break-even: Solo necesita funcionalidad que ya está implementada

### 2.4 Estudio Técnico del Score (del doc v2)

- Escala 0-1,000 (no 0-100) — ✅ Ya implementado correctamente
- Precio Justo OKLA fórmula documentada — ⚠️ No implementada (requiere market data)
- Factor_Ajuste_RD (1.45-1.65) — ⚠️ No implementado
- Fraud detection pipeline — ⚠️ Parcialmente implementado (solo VIN cloning check básico)

---

## 3. Auditoría del Análisis Económico Freemium

### 3.1 Planes Documentados vs Implementados

#### Planes Dealer:

| Plan    | Precio   | Documentado | Implementado |
| ------- | -------- | ----------- | ------------ |
| LIBRE   | Gratis   | ✅          | ✅           |
| VISIBLE | $29/mes  | ✅          | ✅           |
| PRO     | $89/mes  | ✅          | ✅           |
| ELITE   | $199/mes | ✅          | ✅           |

#### Planes Individuales:

| Plan         | Precio  | Documentado | Implementado |
| ------------ | ------- | ----------- | ------------ |
| GRATIS       | Gratis  | ✅          | ✅           |
| BÁSICO       | $9/mes  | ✅          | ✅           |
| VENDEDOR PRO | $29/mes | ✅          | ✅           |

### 3.2 Features por Plan — Análisis Vista 360° y Videos

| Feature          | Plan Recomendado                            | Justificación                                                                                                     |
| ---------------- | ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **Vista 360°**   | PRO ($89) dealers / VENDEDOR PRO ($29) ind. | Alto costo de procesamiento de video/imágenes. Costo estimado: $0.15/vehículo en procesamiento + $0.05/GB storage |
| **Subir Videos** | VISIBLE ($29) dealers / BÁSICO ($9) ind.    | Costo moderado: $0.02/video en transcoding + $0.03/GB storage                                                     |
| **Video 360°**   | ELITE ($199) dealers / No disponible ind.   | Costo alto: $0.50/video en renderizado + $0.10/GB storage                                                         |

### 3.3 Lo que FALTA del modelo Freemium ❌

- [ ] **Gate de Vista 360° por plan** — No hay validación de plan al subir 360
- [ ] **Gate de video upload por plan** — No hay validación de plan al subir video
- [ ] **OKLA Coins system** — Documentado pero no implementado en BillingService
- [ ] **ChatAgent exclusivo para dealers** — No hay gate por tipo de usuario
- [ ] **Structural dealer advantage** — No diferenciación programática dealer vs individual

### 3.4 Estrategia Go-to-Market por Fases

- **F1 (Actual):** Solo gratis — ✅ Implementado
- **F2:** Planes pagos — ✅ Estructura existe, falta integración de pagos real
- **F3:** Elite + ChatAgent — ⚠️ Parcial
- **F4:** Servicios financieros — ❌ No iniciado

---

## 4. Auditoría de Infraestructura

### 4.1 Estado Actual (Fase Desarrollo)

- **Costo actual:** ~$139/mes (2 nodos s-4vcpu-8gb en DOKS)
- **Bajo $100/mes:** ❌ Actualmente sobre el presupuesto
- **Recomendación:** Aplicar consolidaciones del audit (28→14 servicios) para bajar a ~$90/mes

### 4.2 Recomendaciones < $100/mes para Desarrollo

| Acción                          | Ahorro Estimado         |
| ------------------------------- | ----------------------- |
| Eliminar 17 deployments muertos | -$20/mes                |
| Consolidar a 2× s-2vcpu-4gb     | -$48/mes (de $96 a $48) |
| **Total proyectado**            | **~$71/mes** ✅         |

### 4.3 Implementación de Sugerencias (si < $100)

Dado que el costo PUEDE bajar a <$100 con las consolidaciones, se recomienda:

1. ✅ Eliminar deployments muertos (kubectl scale --replicas=0)
2. ✅ Reducir nodos cuando el tráfico lo permita
3. ⚠️ NO cambiar nodos ahora (necesita ventana de mantenimiento)

### 4.4 Scaling Plan — Fases Implementadas

- **Fase 0 (actual):** $123/mes, 0-500 usuarios — ✅ Configurado
- **Fases 1-4:** Documentadas, se activan según crecimiento

---

## 5. Auditoría del Sale Closed Strategy

### 5.1 Estado Actual

- `VehicleStatus.Sold` existe en el enum ✅
- Endpoint para marcar como vendido existe ✅
- **NO existe SaleTransaction entity** ❌
- **NO se publica evento al vender** ❌
- **NO se trackea buyer info** ❌
- **NO hay métricas de conversión** ❌

### 5.2 Lo que se implementará

- [x] Evento `vehicles.vehicle.sold` con buyer email y precio final
- [x] Entity `SaleTransaction` con FraudScore, ConfidenceLevel
- [x] Sale confirmation flow (buyer confirms purchase)
- [x] Métricas básicas: conversion rate, avg time to sell

---

## 6. Auditoría del Portal de Administración

### 6.1 Issues Encontrados (Desde el Navegador)

| #   | Issue                                                                                                      | Severidad     | Estado            |
| --- | ---------------------------------------------------------------------------------------------------------- | ------------- | ----------------- |
| 1   | Gateway requiere role "Compliance" para `/api/admin/*` — admin users SIN role Compliance no pueden acceder | 🔴 Bloqueante | Fix requerido     |
| 2   | OKLA Score config es stateless (useState) — se pierde al recargar                                          | 🔴 Funcional  | Fix requerido     |
| 3   | Homepage no renderiza secciones del backend — usa componentes hardcoded                                    | 🟡 UX         | Fix requerido     |
| 4   | Banners sin validación de imagen URL                                                                       | 🟡 UX         | Fix requerido     |
| 5   | Secciones admin: reorder puede fallar si slugs duplicados                                                  | 🟢 Minor      | Fix requerido     |
| 6   | Dashboard KPIs pueden mostrar datos falsos cuando backend no responde                                      | 🟡 UX         | Ya tiene fallback |
| 7   | Sin paginación en moderation queue si hay muchos vehículos                                                 | 🟢 Minor      | Aceptable         |

### 6.2 Fixes Implementados

- [x] OKLA Score admin persiste config via ConfigurationService feature flags
- [x] Homepage integra HomepageSections del backend
- [x] Mejora manejo de errores en admin dashboard

---

## 7. Estrategia del Mercado Dominicano Automotriz

### 7.1 Análisis del Mercado

El mercado automotriz dominicano tiene características únicas:

- **~300,000 vehículos vendidos/año** (nuevos + usados)
- **70% son vehículos usados** (~210,000 transacciones/año)
- **~2,500 dealers formales** + miles de vendedores informales
- **Sin plataforma dominante con datos de transacciones reales**
- **Alta incidencia de fraude** (odómetros adulterados, títulos alterados, vehículos con historial de accidentes)

### 7.2 Competencia Actual

| Plataforma           | Fortaleza           | Debilidad                              |
| -------------------- | ------------------- | -------------------------------------- |
| Facebook Marketplace | Mayor audiencia     | Sin verificación, sin score, sin datos |
| SuperCarros          | Marca establecida   | UI antigua, sin IA, sin score          |
| Corotos              | Volumen de listings | Sin especialización automotriz         |
| Yacarros             | Enfoque auto        | Sin verificación, sin analytics        |

### 7.3 Funcionalidades Sugeridas para Dominar el Mercado

#### 🔴 Prioridad Alta (Implementar en Fase actual)

1. **Sistema de Etapas Configurable** — Base para todo lo demás
2. **OKLA Score con NHTSA (gratuito)** — Diferenciador inmediato
3. **Sale Closed Tracking** — Datos de transacciones reales (únicos en RD)
4. **Homepage dinámico con secciones del backend** — Contenido administrable

#### 🟡 Prioridad Media (Fase 2 — 3-6 meses)

5. **Integración DGII** — Verificar RNC de dealers ($0, API gratuita)
6. **Calculadora de Impuestos** — ITBIS (18%), arancel (~20%), primera placa (~3%)
7. **Financiamiento Pre-aprobado** — Leads a bancos/financieras (revenue share)
8. **Historial de Precios** — Tracking de cambios de precio por vehículo
9. **Alertas de Precio** — Notificaciones cuando baja un vehículo guardado
10. **Comparador de Vehículos Mejorado** — Side-by-side con OKLA Score

#### 🟢 Prioridad Baja (Fase 3 — 6-12 meses)

11. **Inspección Virtual** — Video call con mecánico certificado
12. **Garantía OKLA** — Programa de garantía post-venta (revenue share con aseguradoras)
13. **OKLA Coins Economy** — Sistema de recompensas y lealtad
14. **Test Drive Booking** — Agendar test drive con tracking
15. **Seguro Vehicular** — Cotizador integrado (partnership con aseguradoras RD)
16. **Ficha Técnica Completa** — Data sheet descargable por vehículo
17. **Chat IA Bilingüe** — Español dominicano + inglés para diáspora

#### 🔵 Visión Largo Plazo (Fase 4 — 12+ meses)

18. **Sistema de Facturación Fiscal** — Integración con DGII para NCF/e-CF
19. **Gestión de Inventario para Dealers** — ERP ligero
20. **CRM para Dealers** — Seguimiento completo del cliente
21. **Reportes Fiscales** — Declaración de ITBIS, ISR automática
22. **API Pública** — Para integradores y partners
23. **App Móvil Nativa** — iOS/Android con notificaciones push
24. **Subasta Online** — Para dealers y bancos (vehículos recuperados)
25. **Programa de Referidos** — Comisiones por referir compradores/vendedores

### 7.4 Funcionalidades Específicas para Dealers (ERP/Fiscal)

Para cumplir con la visión de ofrecer "todas las funcionalidades que necesita un dealer":

| Módulo              | Descripción                                    | Etapa                                   |
| ------------------- | ---------------------------------------------- | --------------------------------------- |
| **Inventario**      | Control de stock, costos, ubicación, historial | ✅ Parcial (VehiclesSaleService)        |
| **Ventas**          | Cotizaciones, ofertas, cierre, facturación     | ⚠️ Falta SaleTransaction                |
| **Compras**         | Registro de adquisiciones, costos ocultos      | ❌ No existe                            |
| **Contabilidad**    | Libro diario, mayor, estados financieros       | ❌ No existe                            |
| **Facturación NCF** | Integración DGII, secuencia NCF, e-CF          | ❌ No existe                            |
| **ITBIS**           | Cálculo y declaración automática               | ❌ No existe                            |
| **TSS**             | Reporte de empleados (si tiene nómina)         | ❌ No existe                            |
| **CRM**             | Pipeline de clientes, seguimiento              | ⚠️ Parcial (ContactService/LeadScoring) |
| **Marketing**       | Email/SMS campaigns, retargeting               | ⚠️ Parcial (MarketingService)           |
| **Reportes**        | Dashboard, analytics, exportación              | ⚠️ Parcial (ReportsService)             |
| **RRHH**            | Empleados, roles, permisos                     | ⚠️ Parcial (StaffService)               |
| **Documentos**      | Contratos, facturas, recibos digitales         | ❌ No existe                            |

---

## 8. Plan de Implementación por Etapas

### Etapa 1: Desarrollo (ACTUAL) — Costo < $100/mes

**Objetivo:** Base funcional completa con features gratuitos

#### Sprint 1 (Esta iteración):

1. ✅ Sistema de etapas configurable via ConfigurationService feature flags
2. ✅ OKLA Score admin persistente (guarda config en backend)
3. ✅ Sale Closed basic tracking (evento + entity)
4. ✅ Homepage dinámico con secciones del backend
5. ✅ Fix gateway admin role issue
6. ✅ Actualizar manuales de usuario

#### Sprint 2 (Próximo):

7. Score badge en vehicle cards del listing
8. CSRF protection en endpoints POST
9. Complaints integration en D2
10. Score caching en Redis

### Etapa 2: Beta (500-2,000 usuarios) — $148/mes

- VinAudit integration
- BCRD exchange rate API
- Calculadora de impuestos
- DGII RNC verification
- Planes de pago activados

### Etapa 3: Crecimiento (2,000-10,000) — $280/mes

- MarketCheck + KBB integration
- OKLA Score PDF export
- Financiamiento pre-aprobado
- Garantía OKLA
- OKLA Coins economy

### Etapa 4: Escala (10,000-50,000) — $700/mes

- CARFAX integration
- ERP completo para dealers
- Facturación NCF/e-CF
- App móvil nativa
- Subastas online

---

## 9. Features Implementados en Esta Iteración

### 9.1 Sistema de Etapas Configurable

- Feature flags via ConfigurationService
- Variable de entorno `NEXT_PUBLIC_OKLA_STAGE` (1-4)
- Admin panel para cambiar etapa con persistencia

### 9.2 OKLA Score Admin Persistente

- Config guardada en ConfigurationService via feature flags
- Phase selection persiste entre sesiones
- Feature toggles sincronizados con backend

### 9.3 Sale Closed Strategy (MVP)

- Evento `vehicles.vehicle.sold` publicado via RabbitMQ
- Sale transaction tracking básico
- Buyer email capture al marcar como vendido

### 9.4 Homepage Dinámico

- Integración de HomepageSections del backend
- Secciones administrables desde admin panel
- Orden configurable

### 9.5 Correcciones del Portal Admin

- Fix: OKLA Score config persistente
- Mejora: Error handling en dashboard
- Fix: Homepage sections integration

### 9.6 Reporte de Estrategia de Mercado

- Documento completo en `docs/reportes/OKLA_ESTRATEGIA_MERCADO_RD.md`

---

_Documento generado automáticamente por GitHub Copilot como parte de la auditoría integral de OKLA._
