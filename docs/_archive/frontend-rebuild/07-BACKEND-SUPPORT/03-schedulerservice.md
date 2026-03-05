# ⏰ SchedulerService - Documentación Frontend

> **Servicio:** SchedulerService  
> **Puerto:** 5096 (dev) / 8080 (k8s)  
> **Dashboard:** `/hangfire`  
> **Estado:** ✅ Listo para producción  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de programación de tareas distribuido basado en **Hangfire**. Permite programar, monitorear y gestionar jobs recurrentes y diferidos. El frontend puede interactuar con este servicio para programar tareas en nombre del usuario.

---

## 🎯 Casos de Uso Frontend

### 1. Programar Alerta de Precio

```typescript
// Cuando un usuario configura una alerta de precio, se crea un job recurrente
const createPriceAlert = async (vehicleId: string, targetPrice: number) => {
  const job = await schedulerService.createJob({
    name: `price-alert-${vehicleId}`,
    jobType: "PriceCheckJob",
    cronExpression: "0 */6 * * *", // Cada 6 horas
    parameters: {
      vehicleId,
      targetPrice,
      notifyEmail: user.email,
    },
    isEnabled: true,
  });
  return job.id;
};
```

### 2. Programar Publicación Diferida

```typescript
// Dealer programa publicación de vehículo para fecha futura
const scheduleVehiclePublication = async (
  vehicleId: string,
  publishDate: Date,
) => {
  const job = await schedulerService.createDeferredJob({
    name: `publish-vehicle-${vehicleId}`,
    jobType: "PublishVehicleJob",
    executeAt: publishDate.toISOString(),
    parameters: { vehicleId },
  });
  return job.id;
};
```

### 3. Ver Estado de Tareas

```typescript
// Dashboard de dealer mostrando sus tareas programadas
const getMyScheduledTasks = async () => {
  const jobs = await schedulerService.getJobs({
    filter: { ownerId: currentUser.id },
    status: "Active",
  });
  return jobs;
};
```

---

## 📡 API Endpoints

### Job Management

| Método   | Endpoint                 | Descripción           |
| -------- | ------------------------ | --------------------- |
| `GET`    | `/api/jobs`              | Listar todos los jobs |
| `GET`    | `/api/jobs/active`       | Solo jobs activos     |
| `GET`    | `/api/jobs/{id}`         | Detalle de un job     |
| `POST`   | `/api/jobs`              | Crear nuevo job       |
| `PUT`    | `/api/jobs/{id}`         | Actualizar job        |
| `DELETE` | `/api/jobs/{id}`         | Eliminar job          |
| `POST`   | `/api/jobs/{id}/trigger` | Ejecutar manualmente  |
| `POST`   | `/api/jobs/{id}/pause`   | Pausar job            |
| `POST`   | `/api/jobs/{id}/resume`  | Reanudar job          |

### Executions

| Método | Endpoint                      | Descripción              |
| ------ | ----------------------------- | ------------------------ |
| `GET`  | `/api/executions`             | Historial de ejecuciones |
| `GET`  | `/api/executions/job/{jobId}` | Ejecuciones de un job    |
| `GET`  | `/api/executions/{id}`        | Detalle de ejecución     |

### Dashboard

| Método | Endpoint    | Descripción                |
| ------ | ----------- | -------------------------- |
| `GET`  | `/hangfire` | Dashboard de Hangfire (UI) |

---

## 🔧 Cliente TypeScript

```typescript
// services/schedulerService.ts

import { apiClient } from "./apiClient";

// Tipos
interface Job {
  id: string;
  name: string;
  jobType: string;
  cronExpression?: string;
  executeAt?: string;
  parameters: Record<string, any>;
  isEnabled: boolean;
  status: JobStatus;
  lastExecution?: string;
  nextExecution?: string;
  createdAt: string;
  updatedAt: string;
}

type JobStatus = "Active" | "Paused" | "Completed" | "Failed" | "Disabled";

interface CreateJobRequest {
  name: string;
  jobType: string;
  cronExpression?: string; // Para jobs recurrentes
  executeAt?: string; // Para jobs diferidos
  parameters?: Record<string, any>;
  isEnabled?: boolean;
  timeoutSeconds?: number;
  maxRetries?: number;
}

interface JobExecution {
  id: string;
  jobId: string;
  status: "Running" | "Completed" | "Failed" | "Retrying";
  startedAt: string;
  completedAt?: string;
  durationMs?: number;
  error?: string;
  stackTrace?: string;
}

interface JobFilter {
  status?: JobStatus;
  jobType?: string;
  ownerId?: string;
}

export const schedulerService = {
  // Listar jobs
  async getJobs(filter?: JobFilter): Promise<Job[]> {
    const response = await apiClient.get("/api/jobs", { params: filter });
    return response.data;
  },

  // Obtener job por ID
  async getJob(id: string): Promise<Job> {
    const response = await apiClient.get(`/api/jobs/${id}`);
    return response.data;
  },

  // Crear job recurrente (cron)
  async createJob(request: CreateJobRequest): Promise<Job> {
    const response = await apiClient.post("/api/jobs", request);
    return response.data;
  },

  // Crear job diferido (una sola ejecución)
  async createDeferredJob(
    request: Omit<CreateJobRequest, "cronExpression">,
  ): Promise<Job> {
    const response = await apiClient.post("/api/jobs", {
      ...request,
      cronExpression: undefined,
    });
    return response.data;
  },

  // Actualizar job
  async updateJob(
    id: string,
    updates: Partial<CreateJobRequest>,
  ): Promise<Job> {
    const response = await apiClient.put(`/api/jobs/${id}`, updates);
    return response.data;
  },

  // Eliminar job
  async deleteJob(id: string): Promise<void> {
    await apiClient.delete(`/api/jobs/${id}`);
  },

  // Ejecutar job manualmente
  async triggerJob(id: string): Promise<JobExecution> {
    const response = await apiClient.post(`/api/jobs/${id}/trigger`);
    return response.data;
  },

  // Pausar job
  async pauseJob(id: string): Promise<Job> {
    const response = await apiClient.post(`/api/jobs/${id}/pause`);
    return response.data;
  },

  // Reanudar job
  async resumeJob(id: string): Promise<Job> {
    const response = await apiClient.post(`/api/jobs/${id}/resume`);
    return response.data;
  },

  // Historial de ejecuciones
  async getExecutions(
    jobId?: string,
    limit: number = 50,
  ): Promise<JobExecution[]> {
    const url = jobId ? `/api/executions/job/${jobId}` : "/api/executions";
    const response = await apiClient.get(url, { params: { limit } });
    return response.data;
  },

  // Detalle de ejecución
  async getExecution(id: string): Promise<JobExecution> {
    const response = await apiClient.get(`/api/executions/${id}`);
    return response.data;
  },
};
```

---

## 🪝 Hook de React

```typescript
// hooks/useScheduledJobs.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { schedulerService } from "../services/schedulerService";

interface UseScheduledJobsOptions {
  status?: "Active" | "Paused" | "Completed" | "Failed";
  jobType?: string;
}

export function useScheduledJobs(options: UseScheduledJobsOptions = {}) {
  const queryClient = useQueryClient();

  // Query para listar jobs
  const {
    data: jobs = [],
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ["scheduled-jobs", options],
    queryFn: () => schedulerService.getJobs(options),
    refetchInterval: 30000, // Refrescar cada 30s
  });

  // Mutation para crear job
  const createJob = useMutation({
    mutationFn: schedulerService.createJob,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["scheduled-jobs"] });
    },
  });

  // Mutation para pausar/reanudar
  const toggleJob = useMutation({
    mutationFn: async (job: { id: string; isPaused: boolean }) => {
      return job.isPaused
        ? schedulerService.resumeJob(job.id)
        : schedulerService.pauseJob(job.id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["scheduled-jobs"] });
    },
  });

  // Mutation para ejecutar manualmente
  const triggerJob = useMutation({
    mutationFn: schedulerService.triggerJob,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["scheduled-jobs"] });
    },
  });

  // Mutation para eliminar
  const deleteJob = useMutation({
    mutationFn: schedulerService.deleteJob,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["scheduled-jobs"] });
    },
  });

  return {
    jobs,
    isLoading,
    error,
    refetch,
    createJob,
    toggleJob,
    triggerJob,
    deleteJob,
  };
}
```

---

## 🧩 Componente de Ejemplo

```tsx
// components/ScheduledJobsList.tsx

import { useScheduledJobs } from "../hooks/useScheduledJobs";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export function ScheduledJobsList() {
  const { jobs, isLoading, toggleJob, triggerJob, deleteJob } =
    useScheduledJobs({
      status: "Active",
    });

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold">Tareas Programadas</h2>

      {jobs.length === 0 ? (
        <EmptyState message="No hay tareas programadas" />
      ) : (
        <ul className="divide-y">
          {jobs.map((job) => (
            <li key={job.id} className="py-4 flex justify-between items-center">
              <div>
                <h3 className="font-medium">{job.name}</h3>
                <p className="text-sm text-gray-500">
                  {job.cronExpression
                    ? `Recurrente: ${job.cronExpression}`
                    : `Programado: ${new Date(job.executeAt!).toLocaleString()}`}
                </p>
                {job.nextExecution && (
                  <p className="text-xs text-gray-400">
                    Próxima ejecución:{" "}
                    {formatDistanceToNow(new Date(job.nextExecution), {
                      addSuffix: true,
                      locale: es,
                    })}
                  </p>
                )}
              </div>

              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => triggerJob.mutate(job.id)}
                  disabled={triggerJob.isPending}
                >
                  Ejecutar Ahora
                </Button>

                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() =>
                    toggleJob.mutate({
                      id: job.id,
                      isPaused: job.status === "Paused",
                    })
                  }
                >
                  {job.status === "Paused" ? "Reanudar" : "Pausar"}
                </Button>

                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    if (confirm("¿Eliminar esta tarea?")) {
                      deleteJob.mutate(job.id);
                    }
                  }}
                >
                  Eliminar
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
```

---

## 🧪 Testing

### Vitest Mocks

```typescript
// __mocks__/schedulerService.ts
export const schedulerService = {
  getJobs: vi.fn().mockResolvedValue([
    {
      id: "1",
      name: "price-alert-123",
      jobType: "PriceCheckJob",
      cronExpression: "0 */6 * * *",
      status: "Active",
      nextExecution: new Date(Date.now() + 3600000).toISOString(),
    },
  ]),
  createJob: vi.fn().mockResolvedValue({ id: "2", name: "new-job" }),
  pauseJob: vi.fn().mockResolvedValue({ id: "1", status: "Paused" }),
  resumeJob: vi.fn().mockResolvedValue({ id: "1", status: "Active" }),
  triggerJob: vi.fn().mockResolvedValue({ id: "exec-1", status: "Running" }),
  deleteJob: vi.fn().mockResolvedValue(undefined),
};
```

### E2E Test (Playwright)

```typescript
// e2e/scheduler.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Scheduler Integration", () => {
  test("should create price alert job", async ({ page }) => {
    await page.goto("/dashboard/alerts");

    // Crear alerta
    await page.click('[data-testid="create-alert"]');
    await page.fill('[name="vehicleId"]', "vehicle-123");
    await page.fill('[name="targetPrice"]', "25000");
    await page.click('[data-testid="submit-alert"]');

    // Verificar que se creó
    await expect(page.locator('[data-testid="alert-list"]')).toContainText(
      "price-alert",
    );
  });

  test("should show scheduled jobs list", async ({ page }) => {
    await page.goto("/admin/scheduler");

    await expect(page.locator("h2")).toContainText("Tareas Programadas");
    await expect(
      page.locator('[data-testid="job-list"] li'),
    ).toHaveCount.greaterThan(0);
  });
});
```

---

## 📊 Expresiones Cron Comunes

| Expresión     | Descripción         |
| ------------- | ------------------- |
| `0 * * * *`   | Cada hora           |
| `0 */6 * * *` | Cada 6 horas        |
| `0 0 * * *`   | Diario a medianoche |
| `0 0 * * 1`   | Lunes a medianoche  |
| `0 0 1 * *`   | Primer día del mes  |
| `*/5 * * * *` | Cada 5 minutos      |

---

## ⚙️ Tipos de Jobs Disponibles

| Job Type            | Descripción                | Parámetros                                |
| ------------------- | -------------------------- | ----------------------------------------- |
| `PriceCheckJob`     | Verifica cambios de precio | `vehicleId`, `targetPrice`, `notifyEmail` |
| `PublishVehicleJob` | Publica vehículo diferido  | `vehicleId`                               |
| `CleanupExpiredJob` | Limpia datos expirados     | `olderThanDays`                           |
| `DailyReportJob`    | Genera reporte diario      | `recipientEmail`, `reportType`            |
| `SyncInventoryJob`  | Sincroniza inventario      | `dealerId`                                |
| `SendReminderJob`   | Envía recordatorios        | `userId`, `message`                       |

---

## 🔗 Referencias

- [README del servicio](../../../backend/SchedulerService/README.md)
- [Hangfire Documentation](https://docs.hangfire.io/)
- [Cron Expression Generator](https://crontab.guru/)

---

_Acceder al dashboard de Hangfire en `/hangfire` para monitoreo visual._
