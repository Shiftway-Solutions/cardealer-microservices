# 📊 26 - CRM API

**Servicio:** CRMService  
**Puerto:** 8080  
**Base Path:** `/api/crm`  
**Autenticación:** ✅ Requerida (Dealers)

---

## 📖 Descripción

Sistema CRM para gestión de:

- Leads de ventas
- Deals/oportunidades
- Pipelines de ventas (Kanban)
- Actividades y seguimientos

---

## 🎯 Endpoints Disponibles

### LeadsController

| #   | Método   | Endpoint                           | Auth | Descripción            |
| --- | -------- | ---------------------------------- | ---- | ---------------------- |
| 1   | `GET`    | `/api/crm/leads`                   | ✅   | Listar todos los leads |
| 2   | `GET`    | `/api/crm/leads/{id}`              | ✅   | Obtener lead por ID    |
| 3   | `GET`    | `/api/crm/leads/status/{status}`   | ✅   | Leads por estado       |
| 4   | `GET`    | `/api/crm/leads/search`            | ✅   | Buscar leads           |
| 5   | `GET`    | `/api/crm/leads/assigned/{userId}` | ✅   | Leads asignados        |
| 6   | `GET`    | `/api/crm/leads/recent/{count}`    | ✅   | Leads recientes        |
| 7   | `POST`   | `/api/crm/leads`                   | ✅   | Crear lead             |
| 8   | `PUT`    | `/api/crm/leads/{id}`              | ✅   | Actualizar lead        |
| 9   | `DELETE` | `/api/crm/leads/{id}`              | ✅   | Eliminar lead          |

### DealsController

| #   | Método   | Endpoint                               | Auth | Descripción        |
| --- | -------- | -------------------------------------- | ---- | ------------------ |
| 10  | `GET`    | `/api/crm/deals`                       | ✅   | Listar deals       |
| 11  | `GET`    | `/api/crm/deals/{id}`                  | ✅   | Obtener deal       |
| 12  | `GET`    | `/api/crm/deals/pipeline/{pipelineId}` | ✅   | Deals por pipeline |
| 13  | `GET`    | `/api/crm/deals/stage/{stageId}`       | ✅   | Deals por etapa    |
| 14  | `GET`    | `/api/crm/deals/status/{status}`       | ✅   | Deals por estado   |
| 15  | `GET`    | `/api/crm/deals/closing-soon/{days}`   | ✅   | Por cerrar pronto  |
| 16  | `POST`   | `/api/crm/deals`                       | ✅   | Crear deal         |
| 17  | `PUT`    | `/api/crm/deals/{id}`                  | ✅   | Actualizar deal    |
| 18  | `POST`   | `/api/crm/deals/{id}/close`            | ✅   | Cerrar deal        |
| 19  | `DELETE` | `/api/crm/deals/{id}`                  | ✅   | Eliminar deal      |

### PipelinesController

| #   | Método   | Endpoint                        | Auth | Descripción          |
| --- | -------- | ------------------------------- | ---- | -------------------- |
| 20  | `GET`    | `/api/crm/pipelines`            | ✅   | Listar pipelines     |
| 21  | `GET`    | `/api/crm/pipelines/{id}`       | ✅   | Obtener pipeline     |
| 22  | `GET`    | `/api/crm/pipelines/default`    | ✅   | Pipeline por defecto |
| 23  | `POST`   | `/api/crm/pipelines`            | ✅   | Crear pipeline       |
| 24  | `PUT`    | `/api/crm/pipelines/{id}`       | ✅   | Actualizar pipeline  |
| 25  | `DELETE` | `/api/crm/pipelines/{id}`       | ✅   | Eliminar pipeline    |
| 26  | `GET`    | `/api/crm/pipelines/{id}/deals` | ✅   | Deals del pipeline   |
| 27  | `GET`    | `/api/crm/pipelines/{id}/stats` | ✅   | Stats (Kanban)       |

### ActivitiesController

| #   | Método   | Endpoint                              | Auth | Descripción         |
| --- | -------- | ------------------------------------- | ---- | ------------------- |
| 28  | `GET`    | `/api/crm/activities`                 | ✅   | Listar actividades  |
| 29  | `GET`    | `/api/crm/activities/{id}`            | ✅   | Obtener actividad   |
| 30  | `GET`    | `/api/crm/activities/lead/{leadId}`   | ✅   | Por lead            |
| 31  | `GET`    | `/api/crm/activities/deal/{dealId}`   | ✅   | Por deal            |
| 32  | `GET`    | `/api/crm/activities/upcoming/{days}` | ✅   | Próximas            |
| 33  | `GET`    | `/api/crm/activities/overdue`         | ✅   | Vencidas            |
| 34  | `GET`    | `/api/crm/activities/pending-count`   | ✅   | Contador pendientes |
| 35  | `POST`   | `/api/crm/activities`                 | ✅   | Crear actividad     |
| 36  | `PUT`    | `/api/crm/activities/{id}`            | ✅   | Actualizar          |
| 37  | `POST`   | `/api/crm/activities/{id}/complete`   | ✅   | Marcar completada   |
| 38  | `DELETE` | `/api/crm/activities/{id}`            | ✅   | Eliminar            |

---

## 📝 Detalle de Endpoints

### 7. POST `/api/crm/leads` - Crear Lead

**Headers:** `X-Dealer-Id: {dealerId}`

**Request:**

```json
{
  "firstName": "Carlos",
  "lastName": "Martínez",
  "email": "carlos@email.com",
  "phone": "+1 809-555-0100",
  "company": "Empresa ABC",
  "source": "Website"
}
```

**Sources:** `Website`, `Referral`, `Advertisement`, `SocialMedia`, `Chatbot`, `Phone`, `WalkIn`

**Response 201:**

```json
{
  "id": "lead-001",
  "firstName": "Carlos",
  "lastName": "Martínez",
  "email": "carlos@email.com",
  "phone": "+1 809-555-0100",
  "status": "New",
  "score": 0,
  "source": "Website",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 16. POST `/api/crm/deals` - Crear Deal

**Request:**

```json
{
  "title": "Venta Toyota Camry 2024",
  "value": 1800000,
  "pipelineId": "pipeline-001",
  "stageId": "stage-001",
  "leadId": "lead-001",
  "contactId": null
}
```

**Response 201:**

```json
{
  "id": "deal-001",
  "title": "Venta Toyota Camry 2024",
  "value": 1800000,
  "currency": "DOP",
  "probability": 20,
  "status": "Open",
  "pipelineId": "pipeline-001",
  "stageId": "stage-001",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 27. GET `/api/crm/pipelines/{id}/stats` - Stats para Kanban

**Response 200:**

```json
[
  {
    "stageId": "stage-001",
    "stageName": "Nuevo",
    "color": "#6366F1",
    "deals": [{ "id": "deal-001", "title": "Toyota Camry", "value": 1800000 }],
    "totalValue": 1800000,
    "count": 1
  },
  {
    "stageId": "stage-002",
    "stageName": "Contactado",
    "color": "#22C55E",
    "deals": [],
    "totalValue": 0,
    "count": 0
  }
]
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// LEADS
// ============================================================================

export type LeadStatus =
  | "New"
  | "Contacted"
  | "Qualified"
  | "Proposal"
  | "Won"
  | "Lost";
export type LeadSource =
  | "Website"
  | "Referral"
  | "Advertisement"
  | "SocialMedia"
  | "Chatbot"
  | "Phone"
  | "WalkIn";

export interface Lead {
  id: string;
  dealerId: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  company?: string;
  jobTitle?: string;
  status: LeadStatus;
  score: number;
  source: LeadSource;
  assignedToUserId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeadRequest {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  company?: string;
  source: string;
}

// ============================================================================
// DEALS
// ============================================================================

export type DealStatus = "Open" | "Won" | "Lost" | "Abandoned";

export interface Deal {
  id: string;
  dealerId: string;
  title: string;
  description?: string;
  value: number;
  currency: string;
  probability: number;
  status: DealStatus;
  pipelineId: string;
  stageId: string;
  leadId?: string;
  contactId?: string;
  expectedCloseDate?: string;
  actualCloseDate?: string;
  lostReason?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDealRequest {
  title: string;
  value: number;
  pipelineId?: string;
  stageId?: string;
  leadId?: string;
  contactId?: string;
}

export interface CloseDealRequest {
  isWon: boolean;
  notes?: string;
  lostReason?: string;
}

// ============================================================================
// PIPELINES
// ============================================================================

export interface Pipeline {
  id: string;
  dealerId: string;
  name: string;
  description?: string;
  isDefault: boolean;
  stages: PipelineStage[];
  createdAt: string;
}

export interface PipelineStage {
  id: string;
  name: string;
  order: number;
  color?: string;
}

export interface PipelineStageStats {
  stageId: string;
  stageName: string;
  color: string;
  deals: Deal[];
  totalValue: number;
  count: number;
}

// ============================================================================
// ACTIVITIES
// ============================================================================

export type ActivityType =
  | "Call"
  | "Email"
  | "Meeting"
  | "Task"
  | "Note"
  | "FollowUp";
export type ActivityPriority = "Low" | "Normal" | "High" | "Urgent";
export type ActivityStatus = "Pending" | "Completed" | "Cancelled";

export interface Activity {
  id: string;
  dealerId: string;
  type: ActivityType;
  subject: string;
  description?: string;
  priority: ActivityPriority;
  status: ActivityStatus;
  dueDate?: string;
  completedAt?: string;
  outcome?: string;
  durationMinutes?: number;
  leadId?: string;
  dealId?: string;
  contactId?: string;
  assignedToUserId?: string;
  createdByUserId: string;
  createdAt: string;
}

export interface CreateActivityRequest {
  type: string;
  subject: string;
  description?: string;
  dueDate?: string;
  leadId?: string;
  dealId?: string;
  contactId?: string;
  assignedToUserId?: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/crmService.ts
import { apiClient } from "./api-client";
import type {
  Lead,
  CreateLeadRequest,
  Deal,
  CreateDealRequest,
  CloseDealRequest,
  Pipeline,
  PipelineStageStats,
  Activity,
  CreateActivityRequest,
} from "@/types/crm";

class CRMService {
  // ============================================================================
  // LEADS
  // ============================================================================

  async getLeads(): Promise<Lead[]> {
    const response = await apiClient.get<Lead[]>("/api/crm/leads");
    return response.data;
  }

  async getLead(id: string): Promise<Lead> {
    const response = await apiClient.get<Lead>(`/api/crm/leads/${id}`);
    return response.data;
  }

  async searchLeads(query: string): Promise<Lead[]> {
    const response = await apiClient.get<Lead[]>("/api/crm/leads/search", {
      params: { query },
    });
    return response.data;
  }

  async createLead(
    request: CreateLeadRequest,
    dealerId: string,
  ): Promise<Lead> {
    const response = await apiClient.post<Lead>("/api/crm/leads", request, {
      headers: { "X-Dealer-Id": dealerId },
    });
    return response.data;
  }

  async updateLead(id: string, request: Partial<Lead>): Promise<Lead> {
    const response = await apiClient.put<Lead>(`/api/crm/leads/${id}`, request);
    return response.data;
  }

  async deleteLead(id: string): Promise<void> {
    await apiClient.delete(`/api/crm/leads/${id}`);
  }

  // ============================================================================
  // DEALS
  // ============================================================================

  async getDeals(): Promise<Deal[]> {
    const response = await apiClient.get<Deal[]>("/api/crm/deals");
    return response.data;
  }

  async getDeal(id: string): Promise<Deal> {
    const response = await apiClient.get<Deal>(`/api/crm/deals/${id}`);
    return response.data;
  }

  async getDealsByPipeline(pipelineId: string): Promise<Deal[]> {
    const response = await apiClient.get<Deal[]>(
      `/api/crm/deals/pipeline/${pipelineId}`,
    );
    return response.data;
  }

  async createDeal(
    request: CreateDealRequest,
    dealerId: string,
  ): Promise<Deal> {
    const response = await apiClient.post<Deal>("/api/crm/deals", request, {
      headers: { "X-Dealer-Id": dealerId },
    });
    return response.data;
  }

  async closeDeal(id: string, request: CloseDealRequest): Promise<Deal> {
    const response = await apiClient.post<Deal>(
      `/api/crm/deals/${id}/close`,
      request,
    );
    return response.data;
  }

  // ============================================================================
  // PIPELINES
  // ============================================================================

  async getPipelines(): Promise<Pipeline[]> {
    const response = await apiClient.get<Pipeline[]>("/api/crm/pipelines");
    return response.data;
  }

  async getDefaultPipeline(): Promise<Pipeline> {
    const response = await apiClient.get<Pipeline>(
      "/api/crm/pipelines/default",
    );
    return response.data;
  }

  async getPipelineStats(id: string): Promise<PipelineStageStats[]> {
    const response = await apiClient.get<PipelineStageStats[]>(
      `/api/crm/pipelines/${id}/stats`,
    );
    return response.data;
  }

  // ============================================================================
  // ACTIVITIES
  // ============================================================================

  async getActivities(): Promise<Activity[]> {
    const response = await apiClient.get<Activity[]>("/api/crm/activities");
    return response.data;
  }

  async getUpcomingActivities(days: number): Promise<Activity[]> {
    const response = await apiClient.get<Activity[]>(
      `/api/crm/activities/upcoming/${days}`,
    );
    return response.data;
  }

  async getOverdueActivities(): Promise<Activity[]> {
    const response = await apiClient.get<Activity[]>(
      "/api/crm/activities/overdue",
    );
    return response.data;
  }

  async createActivity(
    request: CreateActivityRequest,
    dealerId: string,
    userId: string,
  ): Promise<Activity> {
    const response = await apiClient.post<Activity>(
      "/api/crm/activities",
      request,
      {
        headers: { "X-Dealer-Id": dealerId, "X-User-Id": userId },
      },
    );
    return response.data;
  }

  async completeActivity(
    id: string,
    outcome?: string,
    durationMinutes?: number,
  ): Promise<Activity> {
    const response = await apiClient.post<Activity>(
      `/api/crm/activities/${id}/complete`,
      {
        outcome,
        durationMinutes,
      },
    );
    return response.data;
  }
}

export const crmService = new CRMService();
```

---

## 🎉 Resumen

✅ **38 Endpoints documentados**  
✅ **TypeScript Types** (Leads, Deals, Pipelines, Activities)  
✅ **Service Layer** (20+ métodos)  
✅ **Kanban Stats** para tableros visuales

---

_Última actualización: Enero 30, 2026_
