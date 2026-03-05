---
title: "❓ Help Center"
priority: P0
estimated_time: "40 minutos"
dependencies: []
apis: ["NotificationService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# ❓ Help Center

> **Tiempo estimado:** 40 minutos
> **Prerrequisitos:** SupportService (backend)
> **Roles:** USR-ANON, USR-REG

---

## 🚨 AUDITORÍA LEY 358-05 PRO CONSUMIDOR (PROTECCIÓN AL CONSUMIDOR)

> **Marco Legal:** Ley 358-05 de Protección al Consumidor  
> **Regulador:** Pro Consumidor  
> **Fecha de Auditoría:** Enero 8, 2026  
> **Auditor:** Gregory Moreno

---

### 📊 Estado de Implementación

| Aspecto                 | Backend                           | Frontend               | Estado General | Prioridad |
| ----------------------- | --------------------------------- | ---------------------- | -------------- | --------- |
| **Información Básica**  | ✅ 100% (RNC, datos en listings)  | ✅ 90% (visible en UI) | ✅ Completo    | ✅ BAJA   |
| **Sistema de Quejas**   | 🟡 40% (endpoints definidos)      | 🔴 0% (no existe UI)   | 🔴 CRÍTICO     | 🔴 ALTA   |
| **Garantías**           | 🟡 40% (campo warranty en Dealer) | 🟡 30% (solo display)  | 🟡 Incompleto  | 🟡 MEDIA  |
| **Derecho de Retracto** | 🔴 0% (no implementado)           | 🔴 0% (no existe)      | 🔴 CRÍTICO     | 🔴 ALTA   |
| **Resolución Disputas** | 🔴 0% (no implementado)           | 🔴 0% (no existe)      | 🔴 CRÍTICO     | 🔴 ALTA   |
| **Help Center**         | 🔴 0% (SupportService no existe)  | ✅ 80% (solo UI mock)  | 🟡 Incompleto  | 🟡 MEDIA  |
| **Divulgación Info**    | ✅ 80% (VIN, historial en BE)     | ✅ 80% (visible)       | ✅ Completo    | ✅ BAJA   |

**Cobertura Global:** 🔴 **35% CRÍTICO** (3/7 requisitos completos)

---

### 🔍 Análisis Detallado por Proceso

#### ✅ CONS-INFO-001: Información al Consumidor (90% ✅)

**Backend:**

- ✅ Dealers tienen campos: RNC, LegalName, Address, Phone, Email, Website
- ✅ Vehicles tienen: VIN, Year, Make, Model, Mileage, Condition, Description, Price
- ✅ History tracking (AccidentHistory, ServiceRecords)
- ✅ Campo `warrantyInfo` en Vehicle

**Frontend:**

- ✅ [DealerProfilePage.tsx](../../frontend/web/src/pages/dealer/DealerProfilePage.tsx) - Muestra RNC, dirección, contacto
- ✅ [VehicleDetailPage.tsx](../../frontend/web/src/pages/vehicles/VehicleDetailPage.tsx) - Muestra VIN, specs completas, descripción
- ✅ [DealerCard.tsx](../../frontend/web/src/components/dealer/DealerCard.tsx) - Badge "Warranty" si `offersWarranty: true`
- ⚠️ Campo `warrantyInfo` no se muestra en detalle de vehículo

**Gaps:**

- 🟡 Falta sección dedicada "Términos de Garantía" en VehicleDetailPage (2 SP)
- 🟡 Políticas de devolución no visibles por dealer (3 SP)

---

#### 🔴 CONS-QUEJA-001: Sistema de Quejas (0% 🔴 CRÍTICO)

**Backend:**

- 🟡 Procesos definidos en [04-proconsumidor.md](../../docs/process-matrix/08-COMPLIANCE-LEGAL-RD/04-proconsumidor.md):
  - PC-001: Crear Queja (`POST /api/consumer/complaints`)
  - PC-002: Mediación entre Partes
  - PC-003: Escalar a Pro Consumidor
- 🟡 Entidades definidas: `Complaint`, `ComplaintType` (10 tipos), `ComplaintStatus` (9 estados)
- 🔴 Endpoints NO implementados en ningún servicio activo
- 🔴 ComplaintsService no existe (mencionado como "TBD parte de SupportService")

**Frontend:**

- 🔴 NO existe página `/complaints` o `/consumer-protection`
- 🔴 NO existe formulario de quejas
- 🔴 NO existe componente `ComplaintForm`
- 🔴 NO existe servicio `consumerService.ts` o `complaintsService.ts`
- 🔴 Grep search: 0 resultados para "complaint|queja|reclamo" en funcionalidad de quejas

**Riesgo Legal:**

- ⚠️ Ley 358-05 Art. 48: "Todo proveedor debe tener un sistema de atención de quejas"
- ⚠️ Plazo: 5 días hábiles para responder, 15 días para resolver
- ⚠️ Sanción: Multa 10-100 salarios mínimos + cierre temporal

**Implementación Requerida:** 🔴 **CRÍTICA (Compliance Blocker)**

---

#### 🟡 CONS-GAR-001: Gestión de Garantías (30% 🟡)

**Backend:**

- ✅ Dealer tiene `offersWarranty: boolean`
- ✅ Vehicle tiene `warrantyInfo: string | null`
- 🔴 NO existe entidad `Warranty` con detalles legales:
  - Tipo (NewVehicle, UsedDealer, UsedPrivate)
  - Duración (meses/km)
  - Cobertura (motor, transmisión, total)
  - Exclusiones
- 🔴 NO existe proceso de reclamo de garantía

**Frontend:**

- ✅ Badge "Warranty" en DealerCard y DealerProfilePage
- 🔴 NO existe página `/warranty-claims`
- 🔴 NO existe formulario para reclamar garantía
- 🔴 NO muestra términos legales de garantía mínima (Ley 358-05)

**Garantías Legales Mínimas (Art. 45-47):**

| Tipo               | Duración            | Cobertura          |
| ------------------ | ------------------- | ------------------ |
| Vehículo nuevo     | 12 meses / 20,000km | Mecánica completa  |
| Usado (Dealer)     | 3 meses / 5,000km   | Motor, transmisión |
| Usado (Particular) | 30 días             | Vicios ocultos     |

---

#### 🔴 CONS-DEV-001: Derecho de Retracto (0% 🔴 CRÍTICO)

**Backend:**

- 🔴 Proceso PC-004 definido pero NO implementado
- 🔴 Aplica a: Servicios de OKLA (suscripciones), NO a vehículos
- 🔴 Plazo: 48 horas desde compra

**Frontend:**

- 🔴 NO existe botón "Solicitar Retracto"
- 🔴 NO existe página `/retraction` o información del derecho
- 🔴 NO aparece en confirmación de suscripción/compra

**Nota Legal:** Solo aplica a servicios digitales (Stripe subscriptions, planes de dealer), NO a vehículos físicos (Art. 51, excepciones).

---

#### 🔴 PC-002/PC-003: Mediación y Escalamiento (0% 🔴)

**Backend:**

- 🔴 Proceso de mediación NO implementado
- 🔴 Timer de 15 días NO configurado
- 🔴 Generación de expediente PDF NO existe
- 🔴 Integración con Pro Consumidor NO existe
- 🔴 Email `quejas@proconsumidor.gob.do` no configurado

**Frontend:**

- 🔴 NO existe panel de mediación
- 🔴 NO existe chat/mensajería para mediador
- 🔴 NO existe página de "Estado de mi Queja"
- 🔴 NO muestra timeline de resolución

---

#### ✅ HELP-001: Help Center (80% ✅)

**Backend:**

- 🔴 SupportService no existe (0% BE)
- 🔴 Endpoints `/api/support/*` no disponibles
- 🔴 Entidades `HelpArticle`, `SupportTicket` definidas pero NO implementadas

**Frontend:**

- ✅ [HelpCenterPage.tsx](../../frontend/web/src/pages/common/HelpCenterPage.tsx) (209 líneas)
- ✅ Muestra 6 categorías: Buying, Selling, Account, Trust & Safety, Policies, Messaging
- ✅ Barra de búsqueda (mock)
- ✅ 8 artículos populares (mock)
- ✅ Link a `/contact` y `/faq`
- 🔴 Artículos hardcoded, NO dinámicos desde API
- 🔴 NO existe categoría "Quejas y Reclamos" (requerida por Ley 358-05)

**Gaps:**

- 🔴 Crear SupportService backend completo (21 SP - fuera de scope Pro Consumidor)
- 🟡 Agregar categoría "Protección al Consumidor" en HelpCenterPage (1 SP)

---

### 📉 Páginas Faltantes (Frontend)

| Página                      | Ruta Esperada          | Prioridad | Story Points | Estado      |
| --------------------------- | ---------------------- | --------- | ------------ | ----------- |
| **ComplaintsPage**          | `/complaints`          | 🔴 ALTA   | 8 SP         | 🔴 Faltante |
| **ComplaintDetailPage**     | `/complaints/{id}`     | 🔴 ALTA   | 5 SP         | 🔴 Faltante |
| **NewComplaintPage**        | `/complaints/new`      | 🔴 ALTA   | 8 SP         | 🔴 Faltante |
| **WarrantyClaimPage**       | `/warranty-claims`     | 🟡 MEDIA  | 5 SP         | 🔴 Faltante |
| **WarrantyTermsPage**       | `/warranty-terms`      | 🟡 MEDIA  | 3 SP         | 🔴 Faltante |
| **ConsumerRightsPage**      | `/consumer-rights`     | 🟡 MEDIA  | 3 SP         | 🔴 Faltante |
| **RetractionRequestPage**   | `/retraction`          | 🔴 ALTA   | 5 SP         | 🔴 Faltante |
| **MediationDashboardPage**  | `/mediation` (admin)   | 🔴 ALTA   | 8 SP         | 🔴 Faltante |
| **ProConsumidorExportPage** | `/admin/proconsumidor` | 🟡 MEDIA  | 5 SP         | 🔴 Faltante |

**Total:** 9 páginas faltantes, **50 Story Points**

---

### 🛠️ Servicios TypeScript Faltantes

| Servicio                      | Archivo                                 | Prioridad | SP  | Estado      |
| ----------------------------- | --------------------------------------- | --------- | --- | ----------- |
| **ConsumerProtectionService** | `services/consumerProtectionService.ts` | 🔴 ALTA   | 8   | 🔴 Faltante |
| **ComplaintsService**         | `services/complaintsService.ts`         | 🔴 ALTA   | 5   | 🔴 Faltante |
| **WarrantyService**           | `services/warrantyService.ts`           | 🟡 MEDIA  | 5   | 🔴 Faltante |
| **SupportService**            | `services/supportService.ts`            | 🟡 MEDIA  | 5   | 🔴 Faltante |

**Total:** 4 servicios, **23 Story Points**

---

### 📋 Plan de Acción por Prioridad

#### 🔴 CRÍTICO (Compliance Blockers) - 21 SP

**Sprint Inmediato:**

1. **Backend: Crear ConsumerProtectionController** (8 SP)
   - Endpoints: `POST /api/consumer/complaints`, `GET /api/consumer/complaints/my`, `GET /api/consumer/complaints/{id}`
   - Entidades: `Complaint`, `ComplaintType`, `ComplaintStatus`, `ComplaintResolution`
   - Lógica: Crear queja, asignar número de caso (QJ-2026-XXXXX), notificar vendedor

2. **Frontend: ComplaintsPage + NewComplaintPage** (13 SP)
   - Formulario de queja con 10 tipos (VehicleNotAsDescribed, MisleadingInfo, etc.)
   - Upload de evidencia (fotos, documentos)
   - Lista "Mis Quejas" con status
   - Integración con MediaService para adjuntos

#### 🟡 ALTA (Legal Compliance) - 29 SP

**Sprint Siguiente:**

3. **Backend: Sistema de Mediación y Escalamiento** (13 SP)
   - Timer de 15 días (SchedulerService)
   - Workflow de mediación (assign mediator, propose solution)
   - Generación de expediente PDF
   - Email a Pro Consumidor

4. **Frontend: MediationDashboard + Escalation** (8 SP)
   - Panel de mediación para admins
   - Timeline de resolución para usuarios
   - Upload de documentos adicionales
   - Botón "Escalar a Pro Consumidor"

5. **Derecho de Retracto - Backend + Frontend** (8 SP)
   - Endpoint `POST /api/consumer/retraction`
   - Validación de 48h
   - RetractionRequestPage
   - Botón en confirmación de suscripción

#### 🟡 MEDIA (Mejoras Legales) - 16 SP

**Sprint Final:**

6. **Sistema de Garantías Completo** (11 SP)
   - Entidad `Warranty` con términos legales
   - Proceso de reclamo de garantía
   - WarrantyClaimPage + WarrantyTermsPage
   - Mostrar garantías mínimas en VehicleDetailPage

7. **Información al Consumidor** (5 SP)
   - ConsumerRightsPage (derechos del consumidor)
   - Agregar categoría "Protección al Consumidor" en HelpCenter
   - Mostrar políticas de devolución por dealer

---

### 🎯 Story Points Totales

| Prioridad  | Backend | Frontend | Total     |
| ---------- | ------- | -------- | --------- |
| 🔴 CRÍTICO | 8       | 13       | 21        |
| 🟡 ALTA    | 13      | 16       | 29        |
| 🟡 MEDIA   | 5       | 11       | 16        |
| **TOTAL**  | **26**  | **40**   | **66 SP** |

---

### ⚠️ Riesgos Legales

#### Incumplimiento Actual

| Artículo       | Requisito                       | Estado Actual | Sanción Potencial          |
| -------------- | ------------------------------- | ------------- | -------------------------- |
| **Art. 48**    | Sistema de atención de quejas   | 🔴 NO         | Multa 10-100 salarios      |
| **Art. 56**    | Plazo de respuesta 5 días       | 🔴 NO         | Multa + cierre temporal    |
| **Art. 62**    | Información veraz y suficiente  | ✅ SÍ (80%)   | N/A                        |
| **Art. 51**    | Derecho de retracto (servicios) | 🔴 NO         | Multa + reembolso forzado  |
| **Art. 45-47** | Garantía legal mínima           | 🟡 PARCIAL    | Multa + daños y perjuicios |

#### Recomendaciones

1. ⚠️ **Implementar Sistema de Quejas antes del lanzamiento público** - BLOCKER
2. ⚠️ Configurar alertas de 5 días (respuesta) y 15 días (resolución)
3. ⚠️ Firmar convenio con Pro Consumidor para escalamientos
4. ⚠️ Capacitar equipo de soporte en Ley 358-05
5. ⚠️ Incluir términos de garantía en todos los listings de dealers

---

### 📚 Referencias Legales

- [Ley 358-05 - Protección al Consumidor](https://proconsumidor.gob.do/ley-358-05)
- [Pro Consumidor - Portal Oficial](https://proconsumidor.gob.do)
- [process-matrix/08-COMPLIANCE-LEGAL-RD/04-proconsumidor.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/04-proconsumidor.md)

---

### 🔗 Archivos Relacionados

- [43-auditoria-compliance-legal.md](43-auditoria-compliance-legal.md) - Auditoría Master
- [15-admin-compliance.md](15-admin-compliance.md) - Dashboard de Compliance
- [frontend/web/src/pages/common/HelpCenterPage.tsx](../../frontend/web/src/pages/common/HelpCenterPage.tsx)
- [frontend/web/src/pages/dealer/DealerProfilePage.tsx](../../frontend/web/src/pages/dealer/DealerProfilePage.tsx)

---

**CONCLUSIÓN:**  
Pro Consumidor muestra la brecha más grande de compliance (**35%**). Sistema de quejas es **CRÍTICO** para operaciones legales. Implementación debe priorizarse antes del lanzamiento público.

---

## � INTEGRACIÓN CON SERVICIOS DE SOPORTE

> **Referencias:**
>
> - [process-matrix/19-SOPORTE/01-centro-ayuda.md](../../process-matrix/19-SOPORTE/01-centro-ayuda.md)
> - [process-matrix/19-SOPORTE/02-quejas-reclamos.md](../../process-matrix/19-SOPORTE/02-quejas-reclamos.md)

### Servicios Involucrados

| Servicio                | Puerto                        | Estado          | Descripción                            |
| ----------------------- | ----------------------------- | --------------- | -------------------------------------- |
| **SupportService**      | 5087                          | 🔴 0% BE, 0% UI | Centro de ayuda, tickets, feedback     |
| **ComplaintsService**   | TBD (parte de SupportService) | 🔴 0% BE, 0% UI | Quejas Pro Consumidor (Ley 358-05)     |
| **NotificationService** | 5006                          | ✅ 100%         | Notificaciones de tickets y quejas     |
| **MediaService**        | 5007                          | ✅ 100%         | Upload de evidencias/archivos adjuntos |

---

### SupportService - Endpoints de Centro de Ayuda

| Método | Endpoint                             | Descripción               | Auth |
| ------ | ------------------------------------ | ------------------------- | ---- |
| `GET`  | `/api/support/articles`              | Listar artículos de ayuda | ❌   |
| `GET`  | `/api/support/articles/{slug}`       | Ver artículo              | ❌   |
| `GET`  | `/api/support/articles/search`       | Buscar artículos          | ❌   |
| `GET`  | `/api/support/categories`            | Categorías de ayuda       | ❌   |
| `POST` | `/api/support/tickets`               | Crear ticket              | ✅   |
| `GET`  | `/api/support/tickets`               | Mis tickets               | ✅   |
| `GET`  | `/api/support/tickets/{id}`          | Detalle de ticket         | ✅   |
| `POST` | `/api/support/tickets/{id}/messages` | Agregar mensaje           | ✅   |
| `POST` | `/api/support/feedback`              | Enviar feedback           | ✅   |

---

### SupportService - Procesos

| Proceso             | Nombre                    | Pasos | Estado |
| ------------------- | ------------------------- | ----- | ------ |
| **HELP-FAQ-001**    | Buscar en Centro de Ayuda | 7     | 🔴 0%  |
| **HELP-TICKET-001** | Crear Ticket de Soporte   | 8     | 🔴 0%  |
| **HELP-TICKET-002** | Responder Ticket (Agente) | 6     | 🔴 0%  |

---

### SupportService - Entidades

#### HelpArticle

```csharp
public class HelpArticle
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }

    // Contenido
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Summary { get; set; }
    public string Content { get; set; }              // Markdown
    public string ContentHtml { get; set; }          // HTML renderizado

    // Organización
    public ArticleType Type { get; set; }            // FAQ, Tutorial, Guide, Troubleshooting
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }

    // Visibilidad
    public ArticleAudience Audience { get; set; }    // All, Buyers, Sellers, Dealers
    public bool IsPublished { get; set; }

    // Métricas
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public decimal HelpfulPercent { get; set; }

    public List<Guid> RelatedArticleIds { get; set; }
}
```

#### SupportTicket

```csharp
public class SupportTicket
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; }         // OKLA-T-2026-00001
    public Guid UserId { get; set; }

    // Clasificación
    public TicketCategory Category { get; set; }     // AccountIssue, PaymentProblem, TechnicalSupport, etc.
    public TicketPriority Priority { get; set; }     // Low, Medium, High, Urgent
    public TicketStatus Status { get; set; }         // New, Open, InProgress, Resolved, Closed

    // Contenido
    public string Subject { get; set; }
    public string Description { get; set; }
    public List<string> AttachmentUrls { get; set; }

    // Contexto
    public Guid? RelatedVehicleId { get; set; }
    public Guid? RelatedOrderId { get; set; }

    // Asignación
    public Guid? AssignedTo { get; set; }
    public Guid? TeamId { get; set; }

    // Tiempos (SLA)
    public DateTime CreatedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }

    // Satisfacción
    public int? SatisfactionRating { get; set; }     // 1-5 estrellas
    public string SatisfactionComment { get; set; }
}
```

---

### ComplaintsService - Quejas Pro Consumidor (Ley 358-05)

> **Marco Legal:** Ley 358-05 - Protección al Consumidor  
> **Regulador:** Pro Consumidor  
> **Plazo de respuesta:** 5-15 días hábiles según tipo

#### Endpoints de Quejas

| Método | Endpoint                        | Descripción              | Auth |
| ------ | ------------------------------- | ------------------------ | ---- |
| `POST` | `/api/complaints`               | Crear queja              | ✅   |
| `GET`  | `/api/complaints/my`            | Mis quejas               | ✅   |
| `GET`  | `/api/complaints/{id}`          | Detalle de queja         | ✅   |
| `POST` | `/api/complaints/{id}/message`  | Agregar mensaje          | ✅   |
| `POST` | `/api/complaints/{id}/evidence` | Agregar evidencia        | ✅   |
| `POST` | `/api/complaints/{id}/escalate` | Escalar a Pro Consumidor | ✅   |

**Admin Endpoints:**

| Método | Endpoint                             | Descripción             | Auth  |
| ------ | ------------------------------------ | ----------------------- | ----- |
| `GET`  | `/api/admin/complaints`              | Listar todas las quejas | Admin |
| `PUT`  | `/api/admin/complaints/{id}/assign`  | Asignar agente          | Admin |
| `PUT`  | `/api/admin/complaints/{id}/status`  | Cambiar estado          | Admin |
| `POST` | `/api/admin/complaints/{id}/resolve` | Marcar resuelta         | Admin |
| `GET`  | `/api/admin/complaints/stats`        | Estadísticas            | Admin |

**Vendor Endpoints:**

| Método | Endpoint                              | Descripción       | Auth   |
| ------ | ------------------------------------- | ----------------- | ------ |
| `GET`  | `/api/vendor/complaints`              | Quejas contra mí  | Vendor |
| `POST` | `/api/vendor/complaints/{id}/respond` | Responder queja   | Vendor |
| `POST` | `/api/vendor/complaints/{id}/propose` | Proponer solución | Vendor |

#### Procesos de Quejas

| Proceso                | Nombre                        | Pasos | Estado |
| ---------------------- | ----------------------------- | ----- | ------ |
| **QUEJA-CREATE-001**   | Formulario de Queja           | 4     | 🔴 0%  |
| **QUEJA-TRACK-001**    | Seguimiento de Queja          | 3     | 🔴 0%  |
| **QUEJA-RESOLVE-001**  | Resolución de Quejas          | 4     | 🔴 0%  |
| **QUEJA-ESCALATE-001** | Escalamiento a Pro Consumidor | 3     | 🔴 0%  |
| **QUEJA-REPORT-001**   | Reportes de Quejas            | 3     | 🔴 0%  |

#### Complaint Entity

```csharp
public class Complaint
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; }           // QUEJA-2026-00001
    public ComplaintType Type { get; set; }          // ProductNotAsDescribed, MisleadingAdvertising, Breach, etc.
    public ComplaintStatus Status { get; set; }      // New, Assigned, InMediation, Resolved, Escalated

    // Partes
    public Guid ConsumerId { get; set; }
    public Guid VendorId { get; set; }
    public Guid? AssignedAgentId { get; set; }

    // Relacionado
    public Guid? VehicleId { get; set; }
    public Guid? TransactionId { get; set; }

    // Contenido
    public string Description { get; set; }
    public string ExpectedResolution { get; set; }
    public decimal? ClaimedAmount { get; set; }

    // Resolución
    public string Resolution { get; set; }
    public ResolutionType? ResolutionType { get; set; } // Refund, Replacement, Compensation, etc.
    public DateTime? ResolvedAt { get; set; }

    // Plazos legales
    public DateTime CreatedAt { get; set; }
    public DateTime ResponseDueDate { get; set; }     // Ley 358-05: 5-15 días
    public DateTime? EscalatedAt { get; set; }

    public List<ComplaintMessage> Messages { get; set; }
    public List<ComplaintEvidence> Evidences { get; set; }
}
```

#### Tipos de Quejas

| Tipo                     | Descripción                     | Plazo Respuesta |
| ------------------------ | ------------------------------- | --------------- |
| **Producto no conforme** | Vehículo diferente al anunciado | 5 días hábiles  |
| **Publicidad engañosa**  | Información falsa en anuncio    | 5 días hábiles  |
| **Incumplimiento**       | Vendedor no cumple acuerdo      | 10 días hábiles |
| **Garantía**             | Problema cubierto por garantía  | 15 días hábiles |
| **Cobro indebido**       | Cargo no autorizado             | 5 días hábiles  |
| **Servicio deficiente**  | Mal servicio de la plataforma   | 5 días hábiles  |
| **Fraude**               | Estafa o engaño                 | 24 horas        |

---

### Rutas UI Requeridas

#### Centro de Ayuda (Público)

| Ruta                    | Componente        | Usuario | Estado |
| ----------------------- | ----------------- | ------- | ------ |
| `/help`                 | HelpCenterPage    | Público | 🔴 0%  |
| `/help/search`          | HelpSearchResults | Público | 🔴 0%  |
| `/help/article/{slug}`  | HelpArticlePage   | Público | 🔴 0%  |
| `/help/category/{slug}` | HelpCategoryPage  | Público | 🔴 0%  |

#### Tickets de Soporte (Autenticado)

| Ruta                 | Componente       | Usuario     | Estado |
| -------------------- | ---------------- | ----------- | ------ |
| `/help/tickets`      | MyTicketsPage    | Autenticado | 🔴 0%  |
| `/help/tickets/new`  | NewTicketPage    | Autenticado | 🔴 0%  |
| `/help/tickets/{id}` | TicketDetailPage | Autenticado | 🔴 0%  |

#### Quejas y Reclamos (Autenticado)

| Ruta               | Componente            | Usuario     | Estado |
| ------------------ | --------------------- | ----------- | ------ |
| `/complaints`      | ComplaintsLandingPage | Público     | 🔴 0%  |
| `/complaints/new`  | NewComplaintPage      | Autenticado | 🔴 0%  |
| `/complaints/my`   | MyComplaintsPage      | Autenticado | 🔴 0%  |
| `/complaints/{id}` | ComplaintDetailPage   | Autenticado | 🔴 0%  |

#### Admin Dashboard (Admin/Support)

| Ruta                      | Componente          | Usuario     | Estado |
| ------------------------- | ------------------- | ----------- | ------ |
| `/admin/support/tickets`  | SupportDashboard    | ADM-SUPPORT | 🔴 0%  |
| `/admin/support/articles` | ArticlesManager     | ADM-SUPPORT | 🔴 0%  |
| `/admin/complaints`       | ComplaintsDashboard | Admin       | 🔴 0%  |

---

### Estructura de Categorías FAQ Propuesta

```
Centro de Ayuda OKLA
├── 🚗 Comprar un Vehículo
│   ├── Cómo buscar vehículos
│   ├── Filtros de búsqueda
│   ├── Contactar al vendedor
│   ├── Agendar test drive
│   └── Proceso de compra
├── 💰 Vender tu Vehículo
│   ├── Cómo publicar tu vehículo
│   ├── Consejos para mejores fotos
│   ├── Fijar el precio correcto
│   ├── Responder a compradores
│   └── Completar una venta
├── 🏢 Para Dealers
│   ├── Registro de dealer
│   ├── Planes y precios
│   ├── Gestión de inventario
│   ├── Importar vehículos CSV
│   └── Dashboard de analytics
├── 💳 Pagos y Facturación
│   ├── Métodos de pago aceptados
│   ├── Problemas con pagos
│   ├── Facturación y NCF
│   ├── Reembolsos
│   └── Suscripciones
├── 🔒 Cuenta y Seguridad
│   ├── Crear cuenta
│   ├── Verificar identidad
│   ├── Cambiar contraseña
│   ├── Two-factor authentication
│   └── Eliminar cuenta
├── 🛡️ Confianza y Seguridad
│   ├── Consejos para evitar fraudes
│   ├── Reportar un problema
│   ├── Garantía OKLA
│   ├── Inspección de vehículos
│   └── Vendedores verificados
├── 📋 Quejas y Reclamos (Ley 358-05)
│   ├── Cómo presentar una queja
│   ├── Mis derechos como consumidor
│   ├── Proceso de mediación
│   ├── Escalamiento a Pro Consumidor
│   └── Plazos y resoluciones
└── ⚙️ Problemas Técnicos
    ├── La app no carga
    ├── Error al subir fotos
    ├── Problemas de login
    └── Contactar soporte técnico
```

---

### Flujo de Usuario: Ticket de Soporte

```
1. Usuario NO encuentra respuesta en FAQ
   ↓
2. Click "Contactar Soporte" → `/help/tickets/new`
   ↓
3. Completa formulario:
   - Categoría (AccountIssue, PaymentProblem, TechnicalSupport, etc.)
   - Asunto
   - Descripción
   - Archivos adjuntos (opcional)
   ↓
4. POST /api/support/tickets
   ↓
5. SupportService crea ticket con número OKLA-T-2026-00001
   ↓
6. Notificación por email: "Tu ticket ha sido creado"
   ↓
7. Sistema asigna a equipo/agente según categoría
   ↓
8. Usuario puede ver ticket en `/help/tickets`
   ↓
9. Agente responde en `/admin/support/tickets/{id}`
   ↓
10. Usuario recibe notificación: "Tienes una respuesta"
    ↓
11. Usuario puede responder en `/help/tickets/{id}`
    ↓
12. Cuando se resuelve, usuario califica experiencia (1-5 ⭐)
```

---

### Flujo de Usuario: Queja Pro Consumidor

```
1. Usuario tiene problema con vendedor/dealer
   ↓
2. Accede a `/complaints/new`
   ↓
3. Completa formulario de queja:
   - Tipo de queja (Producto no conforme, Publicidad engañosa, etc.)
   - Vendedor/Dealer
   - Vehículo relacionado (si aplica)
   - Descripción detallada
   - Expectativa de resolución
   - Evidencias (fotos, documentos)
   - Monto reclamado (si aplica)
   ↓
4. POST /api/complaints
   ↓
5. ComplaintsService crea caso QUEJA-2026-00001
   ↓
6. Notificación a:
   - Usuario: "Tu queja ha sido recibida"
   - Vendedor: "Nueva queja contra ti"
   - Admin: "Nueva queja para mediación"
   ↓
7. Vendedor tiene 5-15 días para responder (según tipo)
   ↓
8. Mediación entre partes:
   - Mensajes entre usuario y vendedor
   - OKLA media el proceso
   - Timeline visible para ambos
   ↓
9. Posibles resoluciones:
   ✅ RESUELTA: Acuerdo alcanzado
   ⚖️ ESCALADA: Sin acuerdo → Pro Consumidor
   ❌ CERRADA: Sin mérito
   ⏰ VENCIDA: Sin respuesta en plazo
   ↓
10. Si se escala, ComplaintsService genera reporte para Pro Consumidor
```

---

### Notificaciones Automáticas

#### Tickets de Soporte

| Evento              | Destinatario | Template              |
| ------------------- | ------------ | --------------------- |
| Ticket creado       | Usuario      | ticket-created        |
| Primera respuesta   | Usuario      | ticket-first-response |
| Nueva respuesta     | Usuario      | ticket-new-response   |
| Ticket resuelto     | Usuario      | ticket-resolved       |
| Solicitud de rating | Usuario      | ticket-rating-request |

#### Quejas y Reclamos

| Evento                   | Destinatario | Template                      |
| ------------------------ | ------------ | ----------------------------- |
| Queja creada             | Consumidor   | complaint-created             |
| Nueva queja              | Vendedor     | complaint-received            |
| Respuesta del vendedor   | Consumidor   | complaint-vendor-response     |
| Respuesta del consumidor | Vendedor     | complaint-consumer-response   |
| Queja escalada           | Ambos        | complaint-escalated           |
| Queja resuelta           | Ambos        | complaint-resolved            |
| Recordatorio respuesta   | Vendedor     | complaint-reminder (Día 3, 5) |

---

### Métricas y KPIs

#### Centro de Ayuda

| Métrica                 | Objetivo | Descripción                                     |
| ----------------------- | -------- | ----------------------------------------------- |
| % Artículos útiles      | > 80%    | HelpfulCount / (HelpfulCount + NotHelpfulCount) |
| Búsquedas sin resultado | < 10%    | Search queries que no devuelven resultados      |
| Tiempo en artículo      | > 2 min  | Engagement con contenido                        |

#### Tickets de Soporte

| Métrica                              | Objetivo   | Descripción                              |
| ------------------------------------ | ---------- | ---------------------------------------- |
| Tiempo primera respuesta             | < 4 horas  | FirstResponseAt - CreatedAt              |
| Tiempo de resolución                 | < 24 horas | ResolvedAt - CreatedAt                   |
| Satisfacción promedio                | > 4.0/5    | Rating promedio de usuarios              |
| Tasa de resolución primera respuesta | > 70%      | Tickets resueltos en primera interacción |

#### Quejas y Reclamos

| Métrica                          | Objetivo | Descripción                            |
| -------------------------------- | -------- | -------------------------------------- |
| Tiempo promedio de resolución    | < 7 días | Plazo legal cumplido                   |
| Tasa de resolución satisfactoria | > 85%    | Quejas resueltas con acuerdo           |
| Tasa de escalamiento             | < 10%    | Quejas escaladas a Pro Consumidor      |
| Tasa de respuesta del vendedor   | > 90%    | Vendedores que responden a tiempo      |
| NPS post-resolución              | > 50     | Net Promoter Score después de resolver |

---

## �📋 OBJETIVO

Implementar centro de ayuda público:

- Página principal con categorías
- Búsqueda de artículos
- Detalle de artículo
- Formulario de contacto/ticket

---

## 🔧 PASO 1: Página Principal Help

```typescript
// filepath: src/app/(main)/help/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { Search, Car, CreditCard, Shield, MessageCircle, Settings, HelpCircle } from "lucide-react";
import { HelpSearch } from "@/components/help/HelpSearch";
import { HelpCategories } from "@/components/help/HelpCategories";
import { PopularArticles } from "@/components/help/PopularArticles";

export const metadata: Metadata = {
  title: "Centro de Ayuda | OKLA",
  description: "Encuentra respuestas a tus preguntas sobre OKLA",
};

const categories = [
  { icon: Car, title: "Comprar vehículos", slug: "comprar", count: 12 },
  { icon: CreditCard, title: "Pagos y facturación", slug: "pagos", count: 8 },
  { icon: Shield, title: "Seguridad", slug: "seguridad", count: 6 },
  { icon: MessageCircle, title: "Comunicación", slug: "comunicacion", count: 5 },
  { icon: Settings, title: "Mi cuenta", slug: "cuenta", count: 10 },
  { icon: HelpCircle, title: "Dealers", slug: "dealers", count: 15 },
];

export default function HelpPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero */}
      <div className="bg-primary-600 text-white py-16">
        <div className="container max-w-4xl text-center">
          <h1 className="text-4xl font-bold mb-4">¿Cómo podemos ayudarte?</h1>
          <p className="text-primary-100 mb-8">
            Busca en nuestra base de conocimiento o contáctanos
          </p>
          <HelpSearch />
        </div>
      </div>

      <div className="container max-w-6xl py-12">
        {/* Categories */}
        <section className="mb-12">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Explorar por categoría
          </h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            {categories.map((cat) => (
              <Link
                key={cat.slug}
                href={`/help/categoria/${cat.slug}`}
                className="bg-white rounded-xl border p-6 hover:shadow-md transition-shadow"
              >
                <cat.icon size={24} className="text-primary-600 mb-3" />
                <h3 className="font-medium text-gray-900">{cat.title}</h3>
                <p className="text-sm text-gray-500">{cat.count} artículos</p>
              </Link>
            ))}
          </div>
        </section>

        {/* Popular Articles */}
        <section className="mb-12">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Artículos populares
          </h2>
          <PopularArticles />
        </section>

        {/* Contact CTA */}
        <section className="bg-white rounded-xl border p-8 text-center">
          <h2 className="text-xl font-semibold text-gray-900 mb-2">
            ¿No encontraste lo que buscabas?
          </h2>
          <p className="text-gray-600 mb-6">
            Nuestro equipo de soporte está listo para ayudarte
          </p>
          <Link
            href="/help/contacto"
            className="inline-flex items-center gap-2 bg-primary-600 text-white px-6 py-3 rounded-lg hover:bg-primary-700"
          >
            <MessageCircle size={18} />
            Contactar soporte
          </Link>
        </section>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: HelpSearch

```typescript
// filepath: src/components/help/HelpSearch.tsx
"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Search } from "lucide-react";

export function HelpSearch() {
  const [query, setQuery] = useState("");
  const router = useRouter();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      router.push(`/help/buscar?q=${encodeURIComponent(query)}`);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="relative max-w-2xl mx-auto">
      <Search
        size={20}
        className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400"
      />
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Buscar artículos, guías, preguntas frecuentes..."
        className="w-full pl-12 pr-4 py-4 rounded-xl text-gray-900 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-300"
      />
    </form>
  );
}
```

---

## 🔧 PASO 3: Artículo Individual

```typescript
// filepath: src/app/(main)/help/articulos/[slug]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { ChevronLeft, ThumbsUp, ThumbsDown } from "lucide-react";
import { ArticleFeedback } from "@/components/help/ArticleFeedback";
import { RelatedArticles } from "@/components/help/RelatedArticles";
import { supportService } from "@/lib/services/supportService";

interface Props {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const article = await supportService.getArticleBySlug(slug);
  return {
    title: article ? `${article.title} | Ayuda OKLA` : "Artículo no encontrado",
  };
}

export default async function ArticlePage({ params }: Props) {
  const { slug } = await params;
  const article = await supportService.getArticleBySlug(slug);

  if (!article) notFound();

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container max-w-4xl">
        {/* Breadcrumb */}
        <Link
          href="/help"
          className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700 mb-6"
        >
          <ChevronLeft size={16} />
          Volver a Centro de Ayuda
        </Link>

        {/* Article */}
        <article className="bg-white rounded-xl border p-8">
          <header className="mb-8">
            <span className="text-sm text-primary-600 font-medium">
              {article.category}
            </span>
            <h1 className="text-3xl font-bold text-gray-900 mt-2">
              {article.title}
            </h1>
            <p className="text-gray-500 mt-2">
              Actualizado el {new Date(article.updatedAt).toLocaleDateString("es-DO")}
            </p>
          </header>

          {/* Content */}
          <div
            className="prose prose-lg max-w-none"
            dangerouslySetInnerHTML={{ __html: article.content }}
          />

          {/* Feedback */}
          <ArticleFeedback articleId={article.id} />
        </article>

        {/* Related */}
        <section className="mt-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">
            Artículos relacionados
          </h2>
          <RelatedArticles categorySlug={article.categorySlug} currentId={article.id} />
        </section>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: Formulario de Contacto

```typescript
// filepath: src/app/(main)/help/contacto/page.tsx
import { Metadata } from "next";
import { ContactSupportForm } from "@/components/help/ContactSupportForm";

export const metadata: Metadata = {
  title: "Contactar Soporte | OKLA",
};

export default function ContactPage() {
  return (
    <div className="min-h-screen bg-gray-50 py-12">
      <div className="container max-w-2xl">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Contactar Soporte</h1>
          <p className="text-gray-600 mt-2">
            Completa el formulario y te responderemos pronto
          </p>
        </div>

        <div className="bg-white rounded-xl border p-8">
          <ContactSupportForm />
        </div>
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/help/ContactSupportForm.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Select } from "@/components/ui/Select";
import { Button } from "@/components/ui/Button";
import { useFormSubmit } from "@/lib/hooks/useFormSubmit";
import { supportService } from "@/lib/services/supportService";

const ticketSchema = z.object({
  name: z.string().min(2, "Nombre requerido"),
  email: z.string().email("Email inválido"),
  category: z.string().min(1, "Selecciona una categoría"),
  subject: z.string().min(5, "Asunto muy corto"),
  message: z.string().min(20, "Mensaje muy corto"),
});

type TicketFormData = z.infer<typeof ticketSchema>;

export function ContactSupportForm() {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<TicketFormData>({
    resolver: zodResolver(ticketSchema),
  });

  const { submit, isSubmitting } = useFormSubmit({
    onSubmit: (data) => supportService.createTicket(data),
    onSuccess: () => reset(),
    successMessage: "Ticket creado. Te contactaremos pronto.",
  });

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <FormField label="Nombre" error={errors.name?.message}>
          <Input {...register("name")} placeholder="Tu nombre" />
        </FormField>

        <FormField label="Email" error={errors.email?.message}>
          <Input {...register("email")} type="email" placeholder="tu@email.com" />
        </FormField>
      </div>

      <FormField label="Categoría" error={errors.category?.message}>
        <Select {...register("category")}>
          <option value="">Seleccionar categoría</option>
          <option value="account">Mi cuenta</option>
          <option value="vehicle">Vehículos</option>
          <option value="payment">Pagos</option>
          <option value="dealer">Dealers</option>
          <option value="other">Otro</option>
        </Select>
      </FormField>

      <FormField label="Asunto" error={errors.subject?.message}>
        <Input {...register("subject")} placeholder="Resumen de tu consulta" />
      </FormField>

      <FormField label="Mensaje" error={errors.message?.message}>
        <Textarea
          {...register("message")}
          rows={5}
          placeholder="Describe tu problema o pregunta..."
        />
      </FormField>

      <Button type="submit" disabled={isSubmitting} className="w-full">
        {isSubmitting ? "Enviando..." : "Enviar mensaje"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 5: FAQ Dinámico

```typescript
// filepath: src/components/help/FAQSection.tsx
"use client";

import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

interface FAQItem {
  question: string;
  answer: string;
  category?: string;
}

const faqs: FAQItem[] = [
  {
    question: "¿Cómo publico un vehículo?",
    answer: "Para publicar un vehículo, inicia sesión y haz clic en 'Vender' en el menú principal. Completa el formulario con los detalles del vehículo, sube fotos y elige tu plan de publicación.",
    category: "publicar",
  },
  {
    question: "¿Cuánto cuesta publicar?",
    answer: "Publicar un vehículo individual cuesta $29 USD por 30 días. Los dealers tienen planes mensuales desde $49 que incluyen múltiples publicaciones.",
    category: "precios",
  },
  {
    question: "¿Cómo contacto al vendedor?",
    answer: "En la página del vehículo encontrarás botones para llamar, enviar WhatsApp o email. Tu información de contacto se compartirá con el vendedor.",
    category: "comprar",
  },
  {
    question: "¿Qué métodos de pago aceptan?",
    answer: "Aceptamos AZUL, CardNET, PixelPay, Fygaro y PayPal. Todas son opciones seguras con encriptación de datos.",
    category: "pagos",
  },
  {
    question: "¿Cómo verifican los vehículos?",
    answer: "Nuestro equipo revisa cada publicación. Los dealers verificados tienen un badge especial y deben proporcionar documentos legales.",
    category: "seguridad",
  },
  {
    question: "¿Puedo editar mi publicación?",
    answer: "Sí, desde tu dashboard puedes editar detalles, fotos y precio en cualquier momento. Los cambios se reflejan inmediatamente.",
    category: "publicar",
  },
  {
    question: "¿Qué incluye el plan Dealer Pro?",
    answer: "El plan Pro incluye hasta 50 vehículos activos, analytics avanzados, import/export CSV, múltiples ubicaciones y soporte prioritario por $129/mes.",
    category: "planes",
  },
  {
    question: "¿Ofrecen reembolsos?",
    answer: "Las publicaciones son no reembolsables una vez activadas. Las suscripciones Dealer pueden cancelarse en cualquier momento sin penalización.",
    category: "pagos",
  },
];

export function FAQSection() {
  const [openIndex, setOpenIndex] = useState<number | null>(null);
  const [filter, setFilter] = useState<string>("all");

  const filteredFAQs = filter === "all"
    ? faqs
    : faqs.filter(faq => faq.category === filter);

  return (
    <div className="bg-white rounded-xl border p-6">
      <h2 className="text-2xl font-bold text-gray-900 mb-6">
        Preguntas Frecuentes
      </h2>

      {/* Category Filter */}
      <div className="flex flex-wrap gap-2 mb-6">
        {["all", "publicar", "comprar", "pagos", "planes", "seguridad"].map((cat) => (
          <button
            key={cat}
            onClick={() => setFilter(cat)}
            className={cn(
              "px-4 py-2 rounded-lg text-sm font-medium transition-colors",
              filter === cat
                ? "bg-primary-600 text-white"
                : "bg-gray-100 text-gray-700 hover:bg-gray-200"
            )}
          >
            {cat === "all" ? "Todas" : cat.charAt(0).toUpperCase() + cat.slice(1)}
          </button>
        ))}
      </div>

      {/* FAQ Items */}
      <div className="space-y-3">
        {filteredFAQs.map((faq, index) => (
          <div
            key={index}
            className="border rounded-lg overflow-hidden"
          >
            <button
              onClick={() => setOpenIndex(openIndex === index ? null : index)}
              className="w-full flex items-center justify-between p-4 text-left hover:bg-gray-50"
            >
              <span className="font-medium text-gray-900">{faq.question}</span>
              <ChevronDown
                size={20}
                className={cn(
                  "text-gray-500 transition-transform",
                  openIndex === index && "rotate-180"
                )}
              />
            </button>
            {openIndex === index && (
              <div className="px-4 pb-4 text-gray-600">
                {faq.answer}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: Búsqueda Avanzada

```typescript
// filepath: src/app/(main)/help/buscar/page.tsx
import { Metadata } from "next";
import { Search } from "lucide-react";
import { SearchResults } from "@/components/help/SearchResults";
import { HelpSearch } from "@/components/help/HelpSearch";
import { supportService } from "@/lib/services/supportService";

interface Props {
  searchParams: Promise<{ q?: string }>;
}

export async function generateMetadata({ searchParams }: Props): Promise<Metadata> {
  const { q } = await searchParams;
  return {
    title: q ? `Resultados para "${q}" | Ayuda OKLA` : "Buscar | Ayuda OKLA",
  };
}

export default async function SearchPage({ searchParams }: Props) {
  const { q } = await searchParams;
  const results = q ? await supportService.searchArticles(q) : null;

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container max-w-4xl">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">
          Buscar en el Centro de Ayuda
        </h1>

        <div className="bg-white rounded-xl border p-6 mb-8">
          <HelpSearch initialQuery={q} />
        </div>

        {q && (
          <div>
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              Resultados para "{q}" ({results?.totalCount || 0})
            </h2>
            <SearchResults results={results?.items || []} query={q} />
          </div>
        )}

        {!q && (
          <div className="bg-white rounded-xl border p-12 text-center">
            <Search size={48} className="mx-auto text-gray-400 mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              Busca en nuestra base de conocimiento
            </h3>
            <p className="text-gray-600">
              Escribe palabras clave para encontrar artículos relevantes
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/help/SearchResults.tsx
import Link from "next/link";
import { FileText, ChevronRight } from "lucide-react";
import { highlightSearchTerm } from "@/lib/utils";

interface SearchResultsProps {
  results: Array<{
    id: string;
    title: string;
    slug: string;
    excerpt: string;
    category: string;
  }>;
  query: string;
}

export function SearchResults({ results, query }: SearchResultsProps) {
  if (results.length === 0) {
    return (
      <div className="bg-white rounded-xl border p-12 text-center">
        <p className="text-gray-600">
          No se encontraron resultados para "{query}"
        </p>
        <p className="text-sm text-gray-500 mt-2">
          Intenta con otros términos de búsqueda
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {results.map((result) => (
        <Link
          key={result.id}
          href={`/help/articulos/${result.slug}`}
          className="block bg-white rounded-xl border p-6 hover:shadow-md transition-shadow"
        >
          <div className="flex items-start gap-4">
            <div className="p-2 bg-primary-100 rounded-lg">
              <FileText size={20} className="text-primary-600" />
            </div>
            <div className="flex-1 min-w-0">
              <span className="text-xs text-primary-600 font-medium">
                {result.category}
              </span>
              <h3
                className="font-semibold text-gray-900 mt-1"
                dangerouslySetInnerHTML={{
                  __html: highlightSearchTerm(result.title, query)
                }}
              />
              <p
                className="text-sm text-gray-600 mt-2 line-clamp-2"
                dangerouslySetInnerHTML={{
                  __html: highlightSearchTerm(result.excerpt, query)
                }}
              />
            </div>
            <ChevronRight size={20} className="text-gray-400 flex-shrink-0" />
          </div>
        </Link>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 7: Chatbot Widget

```typescript
// filepath: src/components/help/ChatbotWidget.tsx
"use client";

import { useState } from "react";
import { MessageCircle, X, Send } from "lucide-react";
import { cn } from "@/lib/utils";
import { useChatbot } from "@/lib/hooks/useChatbot";

export function ChatbotWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [message, setMessage] = useState("");
  const { messages, sendMessage, isLoading } = useChatbot();

  const handleSend = () => {
    if (!message.trim()) return;
    sendMessage(message);
    setMessage("");
  };

  return (
    <>
      {/* Floating Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={cn(
          "fixed bottom-6 right-6 w-14 h-14 rounded-full shadow-lg flex items-center justify-center z-50 transition-all",
          isOpen
            ? "bg-red-500 hover:bg-red-600"
            : "bg-primary-600 hover:bg-primary-700"
        )}
      >
        {isOpen ? (
          <X size={24} className="text-white" />
        ) : (
          <MessageCircle size={24} className="text-white" />
        )}
      </button>

      {/* Chat Window */}
      {isOpen && (
        <div className="fixed bottom-24 right-6 w-96 h-[500px] bg-white rounded-xl shadow-2xl border flex flex-col z-50">
          {/* Header */}
          <div className="p-4 border-b bg-primary-600 rounded-t-xl">
            <h3 className="font-semibold text-white">Chat de Ayuda</h3>
            <p className="text-sm text-primary-100">
              Respuestas instantáneas a tus preguntas
            </p>
          </div>

          {/* Messages */}
          <div className="flex-1 overflow-y-auto p-4 space-y-4">
            {messages.map((msg, index) => (
              <div
                key={index}
                className={cn(
                  "flex",
                  msg.role === "user" ? "justify-end" : "justify-start"
                )}
              >
                <div
                  className={cn(
                    "max-w-[80%] rounded-lg px-4 py-2",
                    msg.role === "user"
                      ? "bg-primary-600 text-white"
                      : "bg-gray-100 text-gray-900"
                  )}
                >
                  {msg.content}
                </div>
              </div>
            ))}
            {isLoading && (
              <div className="flex justify-start">
                <div className="bg-gray-100 rounded-lg px-4 py-2">
                  <div className="flex gap-1">
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" />
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-100" />
                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-200" />
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Input */}
          <div className="p-4 border-t">
            <div className="flex gap-2">
              <input
                type="text"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && handleSend()}
                placeholder="Escribe tu pregunta..."
                className="flex-1 px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
              <button
                onClick={handleSend}
                disabled={!message.trim() || isLoading}
                className="p-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50"
              >
                <Send size={20} />
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
```

---

## 🔧 PASO 8: Ticket System

```typescript
// filepath: src/app/(main)/help/tickets/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { TicketsList } from "@/components/help/TicketsList";
import { CreateTicketButton } from "@/components/help/CreateTicketButton";

export const metadata: Metadata = {
  title: "Mis Tickets | OKLA",
};

export default async function TicketsPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/help/tickets");
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container max-w-4xl">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Mis Tickets</h1>
            <p className="text-gray-600 mt-1">
              Consultas y solicitudes de soporte
            </p>
          </div>
          <CreateTicketButton />
        </div>

        <TicketsList userId={session.user.id} />
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/help/TicketsList.tsx
"use client";

import Link from "next/link";
import { Clock, CheckCircle, AlertCircle } from "lucide-react";
import { useTickets } from "@/lib/hooks/useTickets";
import { formatRelativeDate } from "@/lib/utils";
import { Badge } from "@/components/ui/Badge";

interface TicketsListProps {
  userId: string;
}

const statusConfig = {
  open: { label: "Abierto", color: "blue", icon: Clock },
  in_progress: { label: "En progreso", color: "yellow", icon: AlertCircle },
  resolved: { label: "Resuelto", color: "green", icon: CheckCircle },
  closed: { label: "Cerrado", color: "gray", icon: CheckCircle },
};

export function TicketsList({ userId }: TicketsListProps) {
  const { data: tickets, isLoading } = useTickets(userId);

  if (isLoading) {
    return <div>Cargando tickets...</div>;
  }

  if (!tickets?.length) {
    return (
      <div className="bg-white rounded-xl border p-12 text-center">
        <p className="text-gray-600">No tienes tickets abiertos</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {tickets.map((ticket) => {
        const config = statusConfig[ticket.status as keyof typeof statusConfig];
        const Icon = config.icon;

        return (
          <Link
            key={ticket.id}
            href={`/help/tickets/${ticket.id}`}
            className="block bg-white rounded-xl border p-6 hover:shadow-md transition-shadow"
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <span className="text-sm text-gray-500">#{ticket.id.slice(0, 8)}</span>
                  <Badge variant={config.color as any}>
                    <Icon size={12} className="mr-1" />
                    {config.label}
                  </Badge>
                </div>
                <h3 className="font-semibold text-gray-900">{ticket.subject}</h3>
                <p className="text-sm text-gray-600 mt-1 line-clamp-2">
                  {ticket.message}
                </p>
                <p className="text-xs text-gray-500 mt-2">
                  {formatRelativeDate(ticket.createdAt)}
                </p>
              </div>
            </div>
          </Link>
        );
      })}
    </div>
  );
}
```

---

## 🔧 PASO 9: Hooks y Services

```typescript
// filepath: src/lib/hooks/useChatbot.ts
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { supportService } from "@/lib/services/supportService";

interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

export function useChatbot() {
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      role: "assistant",
      content: "¡Hola! ¿En qué puedo ayudarte hoy?",
    },
  ]);

  const mutation = useMutation({
    mutationFn: (message: string) => supportService.chatbot(message),
    onSuccess: (response, message) => {
      setMessages((prev) => [
        ...prev,
        { role: "user", content: message },
        { role: "assistant", content: response.message },
      ]);
    },
  });

  const sendMessage = (message: string) => {
    mutation.mutate(message);
  };

  return {
    messages,
    sendMessage,
    isLoading: mutation.isPending,
  };
}
```

```typescript
// filepath: src/lib/services/supportService.ts
import { api } from "@/lib/api";

class SupportService {
  private baseUrl = "/api/support";

  // Articles
  async getArticleBySlug(slug: string) {
    const { data } = await api.get(`${this.baseUrl}/articles/${slug}`);
    return data;
  }

  async searchArticles(query: string) {
    const { data } = await api.get(`${this.baseUrl}/articles/search`, {
      params: { q: query },
    });
    return data;
  }

  async getPopularArticles(limit = 10) {
    const { data } = await api.get(`${this.baseUrl}/articles/popular`, {
      params: { limit },
    });
    return data;
  }

  // Tickets
  async createTicket(ticket: any) {
    const { data } = await api.post(`${this.baseUrl}/tickets`, ticket);
    return data;
  }

  async getTickets(userId: string) {
    const { data } = await api.get(`${this.baseUrl}/tickets`, {
      params: { userId },
    });
    return data;
  }

  async getTicketById(id: string) {
    const { data } = await api.get(`${this.baseUrl}/tickets/${id}`);
    return data;
  }

  // Chatbot
  async chatbot(message: string) {
    const { data } = await api.post(`${this.baseUrl}/chatbot`, { message });
    return data;
  }

  // Feedback
  async submitArticleFeedback(articleId: string, helpful: boolean) {
    const { data } = await api.post(
      `${this.baseUrl}/articles/${articleId}/feedback`,
      {
        helpful,
      },
    );
    return data;
  }
}

export const supportService = new SupportService();
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /help muestra categorías y FAQ
# - Búsqueda funciona correctamente
# - Artículos se cargan con contenido
# - Formulario de contacto envía
# - Chatbot widget funciona
# - /help/tickets muestra lista (auth)
# - Feedback de artículos funciona
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/help-center.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Help Center", () => {
  test("debe mostrar categorías y FAQ", async ({ page }) => {
    await page.goto("/help");

    await expect(
      page.getByRole("heading", { name: /centro de ayuda/i }),
    ).toBeVisible();
    await expect(page.getByTestId("help-categories")).toBeVisible();
    await expect(page.getByTestId("faq-section")).toBeVisible();
  });

  test("debe buscar artículos", async ({ page }) => {
    await page.goto("/help");

    await page.fill('input[placeholder*="buscar"]', "publicar vehículo");
    await page.keyboard.press("Enter");

    await expect(page.getByTestId("search-results")).toBeVisible();
  });

  test("debe navegar a artículo específico", async ({ page }) => {
    await page.goto("/help");

    await page
      .getByRole("link", { name: /cómo publicar/i })
      .first()
      .click();
    await expect(page).toHaveURL(/\/help\/articulos\//);
    await expect(page.getByRole("article")).toBeVisible();
  });

  test("debe enviar formulario de contacto", async ({ page }) => {
    await page.goto("/help/contacto");

    await page.fill('input[name="name"]', "Juan Pérez");
    await page.fill('input[name="email"]', "juan@example.com");
    await page.fill('textarea[name="message"]', "Necesito ayuda con mi cuenta");
    await page.click('button[type="submit"]');

    await expect(page.getByText(/mensaje enviado/i)).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/12-admin-dashboard.md`
