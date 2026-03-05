---
title: "36. Sistema de Notificaciones - Admin, Templates, Marketing y Campañas"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["BillingService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 36. Sistema de Notificaciones - Admin, Templates, Marketing y Campañas

**Objetivo:** Panel de administración para gestión de templates de notificación, programación de envíos, campañas de marketing por email, y página de vehículo vendido con alternativas.

**Prioridad:** P2 (Media - Admin tools + Marketing)  
**Complejidad:** 🔴 Alta (Templates Handlebars, Scheduling, Campaign Manager, Audience Segmentation)  
**Dependencias:** NotificationService (✅ Backend 100%), MarketingService (🔴 Planificado Q2 2026)

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura General](#arquitectura-general)
2. [Templates de Notificación (Admin)](#templates-de-notificación-admin)
3. [Programación de Envíos (Scheduling)](#programación-de-envíos-scheduling)
4. [Página Vehículo Vendido](#página-vehículo-vendido)
5. [Campañas de Marketing](#campañas-de-marketing)
6. [Audiencias y Segmentos](#audiencias-y-segmentos)
7. [Integraciones (Teams)](#integraciones-teams)
8. [API Endpoints](#api-endpoints)
9. [Tipos TypeScript](#tipos-typescript)
10. [Checklist de Implementación](#checklist-de-implementación)

---

## 🏗️ ARQUITECTURA GENERAL

### Flujo Completo del Sistema de Notificaciones

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                   NOTIFICATION SYSTEM - COMPLETE ARCHITECTURE                │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                        ADMIN PANEL (Nueva UI)                           ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────────┐  ││
│  │  │  Templates  │  │  Scheduled  │  │  Campaigns  │  │ Integrations  │  ││
│  │  │  Editor     │  │  Calendar   │  │  Manager    │  │ (Teams, etc)  │  ││
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └───────┬───────┘  ││
│  │         │                │                │                  │          ││
│  └─────────┼────────────────┼────────────────┼──────────────────┼──────────┘│
│            │                │                │                  │           │
│            ▼                ▼                ▼                  ▼           │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                       BACKEND SERVICES                                  ││
│  │  ┌───────────────────────────┐     ┌───────────────────────────────┐   ││
│  │  │    NotificationService    │     │      MarketingService         │   ││
│  │  │    (Puerto 5010) ✅ 100%  │     │      (Puerto 5045) 🔴 0%      │   ││
│  │  │  ┌─────────────────────┐  │     │  ┌──────────────────────────┐ │   ││
│  │  │  │ TemplatesController │  │     │  │ CampaignsController      │ │   ││
│  │  │  │ ScheduledController │  │     │  │ AudiencesController      │ │   ││
│  │  │  │ PreferencesController│ │     │  │ EmailTemplatesController │ │   ││
│  │  │  │ TeamsController     │  │     │  └──────────────────────────┘ │   ││
│  │  │  └─────────────────────┘  │     └───────────────────────────────┘   ││
│  │  └───────────────────────────┘                                          ││
│  │            │                                      │                     ││
│  │            ▼                                      ▼                     ││
│  │  ┌─────────────────────┐     ┌─────────────────────┐    ┌────────────┐ ││
│  │  │     Resend API      │     │   Event Tracking    │    │  MS Teams  │ ││
│  │  │     (Email ✅)      │     │   (Opens/Clicks)    │    │  Webhooks  │ ││
│  │  └─────────────────────┘     └─────────────────────┘    └────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                         USER-FACING (Existing 25-notificaciones.md)     ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────────┐ ││
│  │  │ Bell Badge  │  │ Notif List  │  │ Preferences │  │ Vehicle Sold   │ ││
│  │  │ (Navbar)    │  │ (Centro)    │  │ (Settings)  │  │ Page (Nueva)   │ ││
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Estado de Implementación

| Componente                | Backend | UI     | Observación                         |
| ------------------------- | ------- | ------ | ----------------------------------- |
| **Templates CRUD**        | ✅ 100% | 🔴 0%  | TemplatesController listo           |
| **Preview Templates**     | ✅ 100% | 🔴 0%  | Endpoint `/preview` listo           |
| **Scheduled Notif**       | ✅ 100% | 🔴 0%  | ScheduledController listo           |
| **Preferencias Usuario**  | ✅ 100% | 🟡 30% | UI básico en /settings              |
| **Vehículo Vendido Page** | 🟡 50%  | 🔴 0%  | Handler parcial, sin UI             |
| **Marketing Campaigns**   | 🔴 0%   | 🔴 0%  | MarketingService NO implementado    |
| **Audiencias/Segmentos**  | 🔴 0%   | 🔴 0%  | MarketingService NO implementado    |
| **Teams Integration**     | 🔴 0%   | N/A    | Integración interna, baja prioridad |

---

## 🎨 TEMPLATES DE NOTIFICACIÓN (ADMIN)

### Ruta: `/admin/notifications/templates`

Editor visual para crear y gestionar templates de notificación con Handlebars.

### PASO 1: TemplatesListPage

```typescript
// filepath: src/app/(admin)/admin/notifications/templates/page.tsx
"use client";

import { useState } from "react";
import { Plus, Search, Filter, Eye, Edit, Trash2, Copy } from "lucide-react";
import { useTemplates, useDeleteTemplate, useDuplicateTemplate } from "@/lib/hooks/useNotificationTemplates";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Select } from "@/components/ui/Select";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { TemplatePreviewModal } from "./TemplatePreviewModal";

const TEMPLATE_CATEGORIES = [
  { value: "all", label: "Todas las categorías" },
  { value: "auth", label: "Autenticación" },
  { value: "payment", label: "Pagos" },
  { value: "vehicle", label: "Vehículos" },
  { value: "marketing", label: "Marketing" },
  { value: "system", label: "Sistema" },
];

const TEMPLATE_TYPES = [
  { value: "all", label: "Todos los tipos" },
  { value: "Email", label: "Email" },
  { value: "SMS", label: "SMS" },
  { value: "Push", label: "Push" },
  { value: "InApp", label: "In-App" },
];

export default function TemplatesListPage() {
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("all");
  const [type, setType] = useState("all");
  const [previewTemplate, setPreviewTemplate] = useState<any>(null);

  const { data: templates, isLoading } = useTemplates({
    search,
    category: category !== "all" ? category : undefined,
    type: type !== "all" ? type : undefined,
  });

  const { mutate: deleteTemplate } = useDeleteTemplate();
  const { mutate: duplicateTemplate } = useDuplicateTemplate();

  const getTypeBadgeColor = (type: string) => {
    switch (type) {
      case "Email": return "blue";
      case "SMS": return "green";
      case "Push": return "purple";
      case "InApp": return "orange";
      default: return "gray";
    }
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Templates de Notificación</h1>
          <p className="text-gray-600 mt-1">
            Gestiona los templates de email, SMS, push e in-app
          </p>
        </div>
        <Button href="/admin/notifications/templates/new">
          <Plus className="w-4 h-4 mr-2" />
          Nuevo Template
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-4 mb-6">
        <div className="flex-1 min-w-[200px]">
          <Input
            placeholder="Buscar templates..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            leftIcon={<Search className="w-4 h-4" />}
          />
        </div>
        <Select
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          options={TEMPLATE_CATEGORIES}
        />
        <Select
          value={type}
          onChange={(e) => setType(e.target.value)}
          options={TEMPLATE_TYPES}
        />
      </div>

      {/* Templates Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {templates?.map((template) => (
          <div
            key={template.id}
            className="bg-white rounded-lg border hover:shadow-md transition-shadow"
          >
            <div className="p-4">
              {/* Header */}
              <div className="flex items-start justify-between mb-3">
                <div>
                  <h3 className="font-semibold text-gray-900">{template.name}</h3>
                  <p className="text-sm text-gray-500 mt-1">
                    {template.description || "Sin descripción"}
                  </p>
                </div>
                <Badge color={getTypeBadgeColor(template.type)}>
                  {template.type}
                </Badge>
              </div>

              {/* Subject (for email) */}
              {template.type === "Email" && template.subject && (
                <div className="text-sm text-gray-600 mb-3 truncate">
                  <span className="font-medium">Asunto:</span> {template.subject}
                </div>
              )}

              {/* Tags */}
              {template.tags && (
                <div className="flex flex-wrap gap-1 mb-3">
                  {template.tags.split(",").map((tag) => (
                    <Badge key={tag} variant="outline" size="sm">
                      {tag.trim()}
                    </Badge>
                  ))}
                </div>
              )}

              {/* Meta */}
              <div className="flex items-center justify-between text-xs text-gray-500">
                <span>v{template.version}</span>
                <span>
                  {template.isActive ? (
                    <span className="text-green-600">● Activo</span>
                  ) : (
                    <span className="text-gray-400">○ Inactivo</span>
                  )}
                </span>
              </div>
            </div>

            {/* Actions */}
            <div className="border-t px-4 py-3 flex items-center justify-end gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPreviewTemplate(template)}
              >
                <Eye className="w-4 h-4" />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => duplicateTemplate(template.id)}
              >
                <Copy className="w-4 h-4" />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                href={`/admin/notifications/templates/${template.id}/edit`}
              >
                <Edit className="w-4 h-4" />
              </Button>
              <ConfirmDialog
                title="Eliminar template"
                message="¿Estás seguro? Esta acción no se puede deshacer."
                onConfirm={() => deleteTemplate(template.id)}
              >
                <Button variant="ghost" size="sm" className="text-red-600">
                  <Trash2 className="w-4 h-4" />
                </Button>
              </ConfirmDialog>
            </div>
          </div>
        ))}
      </div>

      {/* Preview Modal */}
      {previewTemplate && (
        <TemplatePreviewModal
          template={previewTemplate}
          onClose={() => setPreviewTemplate(null)}
        />
      )}
    </div>
  );
}
```

### PASO 2: TemplateEditorPage

```typescript
// filepath: src/app/(admin)/admin/notifications/templates/[id]/edit/page.tsx
"use client";

import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ArrowLeft, Eye, Send, Save } from "lucide-react";
import { useTemplate, useUpdateTemplate, usePreviewTemplate, useTestTemplate } from "@/lib/hooks/useNotificationTemplates";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { Textarea } from "@/components/ui/Textarea";
import { Switch } from "@/components/ui/Switch";
import { FormField } from "@/components/ui/FormField";
import { CodeEditor } from "@/components/ui/CodeEditor";
import { toast } from "sonner";

const templateSchema = z.object({
  name: z.string().min(3, "Mínimo 3 caracteres"),
  subject: z.string().optional(),
  body: z.string().min(10, "El contenido es muy corto"),
  type: z.enum(["Email", "SMS", "Push", "InApp", "WhatsApp"]),
  category: z.string(),
  tags: z.string().optional(),
  description: z.string().optional(),
  isActive: z.boolean(),
});

type TemplateFormData = z.infer<typeof templateSchema>;

export default function TemplateEditorPage() {
  const params = useParams();
  const router = useRouter();
  const isNew = params.id === "new";

  const { data: template, isLoading } = useTemplate(params.id as string, {
    enabled: !isNew,
  });

  const { mutate: updateTemplate, isPending: isSaving } = useUpdateTemplate();
  const { mutate: previewTemplate, data: previewData } = usePreviewTemplate();
  const { mutate: testTemplate, isPending: isTesting } = useTestTemplate();

  const [previewMode, setPreviewMode] = useState(false);
  const [testEmail, setTestEmail] = useState("");

  const form = useForm<TemplateFormData>({
    resolver: zodResolver(templateSchema),
    defaultValues: {
      name: "",
      subject: "",
      body: "",
      type: "Email",
      category: "system",
      tags: "",
      description: "",
      isActive: true,
    },
  });

  // Populate form when template loads
  useEffect(() => {
    if (template) {
      form.reset({
        name: template.name,
        subject: template.subject || "",
        body: template.body,
        type: template.type,
        category: template.category,
        tags: template.tags || "",
        description: template.description || "",
        isActive: template.isActive,
      });
    }
  }, [template, form]);

  const watchType = form.watch("type");
  const watchBody = form.watch("body");

  const handlePreview = () => {
    previewTemplate({
      templateId: params.id as string,
      data: {
        userName: "Juan Pérez",
        vehicleName: "Toyota Camry 2023",
        price: "RD$ 1,250,000",
        actionUrl: "https://okla.com.do/vehicles/example",
      },
    });
    setPreviewMode(true);
  };

  const handleTest = () => {
    if (!testEmail) {
      toast.error("Ingresa un email para la prueba");
      return;
    }
    testTemplate({
      templateId: params.id as string,
      email: testEmail,
    });
  };

  const onSubmit = (data: TemplateFormData) => {
    updateTemplate(
      { id: params.id as string, ...data },
      {
        onSuccess: () => {
          toast.success("Template guardado");
          router.push("/admin/notifications/templates");
        },
      }
    );
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button
                variant="ghost"
                onClick={() => router.back()}
              >
                <ArrowLeft className="w-4 h-4" />
              </Button>
              <h1 className="text-xl font-semibold">
                {isNew ? "Nuevo Template" : `Editar: ${template?.name}`}
              </h1>
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" onClick={handlePreview}>
                <Eye className="w-4 h-4 mr-2" />
                Preview
              </Button>
              <Button type="submit" form="template-form" disabled={isSaving}>
                <Save className="w-4 h-4 mr-2" />
                Guardar
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Form */}
          <div className="lg:col-span-2">
            <form id="template-form" onSubmit={form.handleSubmit(onSubmit)}>
              <div className="bg-white rounded-lg border p-6 space-y-6">
                {/* Basic Info */}
                <div className="grid grid-cols-2 gap-4">
                  <FormField label="Nombre del template" required error={form.formState.errors.name?.message}>
                    <Input {...form.register("name")} placeholder="welcome_email" />
                  </FormField>
                  <FormField label="Tipo" required>
                    <Select {...form.register("type")} options={[
                      { value: "Email", label: "Email" },
                      { value: "SMS", label: "SMS" },
                      { value: "Push", label: "Push Notification" },
                      { value: "InApp", label: "In-App" },
                      { value: "WhatsApp", label: "WhatsApp" },
                    ]} />
                  </FormField>
                </div>

                {/* Subject (Email only) */}
                {watchType === "Email" && (
                  <FormField label="Asunto del email" error={form.formState.errors.subject?.message}>
                    <Input
                      {...form.register("subject")}
                      placeholder="¡Bienvenido a OKLA, {{userName}}!"
                    />
                    <p className="text-xs text-gray-500 mt-1">
                      Usa variables Handlebars: {"{{userName}}"}, {"{{vehicleName}}"}
                    </p>
                  </FormField>
                )}

                {/* Body Editor */}
                <FormField
                  label="Contenido"
                  required
                  error={form.formState.errors.body?.message}
                >
                  {watchType === "Email" ? (
                    <CodeEditor
                      language="handlebars"
                      value={watchBody}
                      onChange={(value) => form.setValue("body", value)}
                      height="400px"
                    />
                  ) : (
                    <Textarea
                      {...form.register("body")}
                      rows={watchType === "SMS" ? 3 : 6}
                      placeholder={
                        watchType === "SMS"
                          ? "Tu código es {{code}}. Expira en {{expiresIn}} minutos."
                          : "Contenido de la notificación..."
                      }
                      maxLength={watchType === "SMS" ? 160 : undefined}
                    />
                  )}
                  {watchType === "SMS" && (
                    <p className="text-xs text-gray-500 mt-1">
                      {watchBody.length}/160 caracteres
                    </p>
                  )}
                </FormField>

                {/* Meta */}
                <div className="grid grid-cols-2 gap-4">
                  <FormField label="Categoría">
                    <Select {...form.register("category")} options={[
                      { value: "auth", label: "Autenticación" },
                      { value: "payment", label: "Pagos" },
                      { value: "vehicle", label: "Vehículos" },
                      { value: "marketing", label: "Marketing" },
                      { value: "system", label: "Sistema" },
                    ]} />
                  </FormField>
                  <FormField label="Tags (separadas por coma)">
                    <Input {...form.register("tags")} placeholder="welcome, onboarding" />
                  </FormField>
                </div>

                <FormField label="Descripción">
                  <Textarea {...form.register("description")} rows={2} />
                </FormField>

                {/* Active toggle */}
                <div className="flex items-center justify-between py-4 border-t">
                  <div>
                    <p className="font-medium text-gray-900">Template activo</p>
                    <p className="text-sm text-gray-500">
                      Templates inactivos no se pueden usar para enviar notificaciones
                    </p>
                  </div>
                  <Switch {...form.register("isActive")} />
                </div>
              </div>
            </form>
          </div>

          {/* Sidebar - Variables & Preview */}
          <div className="space-y-6">
            {/* Variables */}
            <div className="bg-white rounded-lg border p-4">
              <h3 className="font-semibold text-gray-900 mb-3">Variables Disponibles</h3>
              <div className="space-y-2 text-sm">
                <VariableItem name="userName" description="Nombre del usuario" />
                <VariableItem name="email" description="Email del usuario" />
                <VariableItem name="vehicleName" description="Nombre del vehículo" />
                <VariableItem name="price" description="Precio formateado" />
                <VariableItem name="actionUrl" description="URL de acción" />
                <VariableItem name="code" description="Código OTP" />
                <VariableItem name="expiresIn" description="Tiempo de expiración" />
              </div>

              <div className="mt-4 pt-4 border-t">
                <h4 className="font-medium text-gray-900 mb-2">Helpers Handlebars</h4>
                <div className="space-y-1 text-xs text-gray-600 font-mono">
                  <p>{"{{#if condition}}...{{/if}}"}</p>
                  <p>{"{{#each items}}...{{/each}}"}</p>
                  <p>{"{{formatCurrency price}}"}</p>
                  <p>{"{{formatDate date}}"}</p>
                </div>
              </div>
            </div>

            {/* Test Send */}
            {!isNew && watchType === "Email" && (
              <div className="bg-white rounded-lg border p-4">
                <h3 className="font-semibold text-gray-900 mb-3">Enviar Prueba</h3>
                <div className="space-y-3">
                  <Input
                    type="email"
                    placeholder="tu@email.com"
                    value={testEmail}
                    onChange={(e) => setTestEmail(e.target.value)}
                  />
                  <Button
                    variant="outline"
                    fullWidth
                    onClick={handleTest}
                    disabled={isTesting}
                  >
                    <Send className="w-4 h-4 mr-2" />
                    {isTesting ? "Enviando..." : "Enviar Test"}
                  </Button>
                </div>
              </div>
            )}

            {/* Live Preview */}
            {previewMode && previewData && (
              <div className="bg-white rounded-lg border p-4">
                <h3 className="font-semibold text-gray-900 mb-3">Vista Previa</h3>
                {watchType === "Email" ? (
                  <div className="border rounded-lg overflow-hidden">
                    <div className="bg-gray-100 px-3 py-2 text-sm font-medium">
                      {previewData.subject}
                    </div>
                    <div
                      className="p-4 text-sm"
                      dangerouslySetInnerHTML={{ __html: previewData.body }}
                    />
                  </div>
                ) : (
                  <div className="bg-gray-100 rounded-lg p-3 text-sm">
                    {previewData.body}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function VariableItem({ name, description }: { name: string; description: string }) {
  const copy = () => {
    navigator.clipboard.writeText(`{{${name}}}`);
    toast.success("Copiado al portapapeles");
  };

  return (
    <div
      className="flex items-center justify-between p-2 rounded hover:bg-gray-50 cursor-pointer"
      onClick={copy}
    >
      <code className="text-primary-600">{`{{${name}}}`}</code>
      <span className="text-gray-500">{description}</span>
    </div>
  );
}
```

---

## ⏰ PROGRAMACIÓN DE ENVÍOS (SCHEDULING)

### Ruta: `/admin/notifications/scheduled`

Calendario para programar notificaciones y campañas.

### PASO 3: ScheduledNotificationsPage

```typescript
// filepath: src/app/(admin)/admin/notifications/scheduled/page.tsx
"use client";

import { useState } from "react";
import { format, startOfMonth, endOfMonth, eachDayOfInterval, isSameDay, isToday } from "date-fns";
import { es } from "date-fns/locale";
import { ChevronLeft, ChevronRight, Plus, Clock, Pause, Play, Trash2 } from "lucide-react";
import { useScheduledNotifications, usePauseScheduled, useCancelScheduled } from "@/lib/hooks/useScheduledNotifications";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { ScheduleNotificationModal } from "./ScheduleNotificationModal";

const SCHEDULE_STATUS_COLORS = {
  Pending: "yellow",
  Completed: "green",
  Failed: "red",
  Cancelled: "gray",
  Paused: "blue",
};

export default function ScheduledNotificationsPage() {
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);

  const { data: scheduled, isLoading } = useScheduledNotifications({
    from: startOfMonth(currentMonth),
    to: endOfMonth(currentMonth),
  });

  const { mutate: pauseScheduled } = usePauseScheduled();
  const { mutate: cancelScheduled } = useCancelScheduled();

  const days = eachDayOfInterval({
    start: startOfMonth(currentMonth),
    end: endOfMonth(currentMonth),
  });

  const getScheduledForDate = (date: Date) => {
    return scheduled?.filter((s) => isSameDay(new Date(s.scheduledFor), date)) || [];
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Notificaciones Programadas</h1>
          <p className="text-gray-600 mt-1">
            Calendario de envíos programados y recurrentes
          </p>
        </div>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="w-4 h-4 mr-2" />
          Programar Envío
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Calendar */}
        <div className="lg:col-span-2 bg-white rounded-lg border p-6">
          {/* Month Navigation */}
          <div className="flex items-center justify-between mb-6">
            <Button
              variant="ghost"
              onClick={() => setCurrentMonth((prev) => new Date(prev.getFullYear(), prev.getMonth() - 1))}
            >
              <ChevronLeft className="w-5 h-5" />
            </Button>
            <h2 className="text-xl font-semibold">
              {format(currentMonth, "MMMM yyyy", { locale: es })}
            </h2>
            <Button
              variant="ghost"
              onClick={() => setCurrentMonth((prev) => new Date(prev.getFullYear(), prev.getMonth() + 1))}
            >
              <ChevronRight className="w-5 h-5" />
            </Button>
          </div>

          {/* Day Headers */}
          <div className="grid grid-cols-7 mb-2">
            {["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"].map((day) => (
              <div key={day} className="text-center text-sm font-medium text-gray-500 py-2">
                {day}
              </div>
            ))}
          </div>

          {/* Days Grid */}
          <div className="grid grid-cols-7 gap-1">
            {days.map((day) => {
              const dayScheduled = getScheduledForDate(day);
              const hasScheduled = dayScheduled.length > 0;

              return (
                <div
                  key={day.toISOString()}
                  onClick={() => setSelectedDate(day)}
                  className={`
                    min-h-[80px] p-2 border rounded-lg cursor-pointer transition
                    ${isToday(day) ? "bg-primary-50 border-primary-300" : "hover:bg-gray-50"}
                    ${isSameDay(day, selectedDate) ? "ring-2 ring-primary-500" : ""}
                  `}
                >
                  <div className={`text-sm font-medium ${isToday(day) ? "text-primary-600" : "text-gray-900"}`}>
                    {format(day, "d")}
                  </div>
                  {hasScheduled && (
                    <div className="mt-1 space-y-1">
                      {dayScheduled.slice(0, 2).map((s) => (
                        <div
                          key={s.id}
                          className="text-xs bg-primary-100 text-primary-700 rounded px-1 py-0.5 truncate"
                        >
                          {format(new Date(s.scheduledFor), "HH:mm")} {s.notification?.template?.name}
                        </div>
                      ))}
                      {dayScheduled.length > 2 && (
                        <div className="text-xs text-gray-500">
                          +{dayScheduled.length - 2} más
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {/* Sidebar - Selected Date Details */}
        <div className="bg-white rounded-lg border p-6">
          <h3 className="font-semibold text-gray-900 mb-4">
            {selectedDate
              ? format(selectedDate, "EEEE, d 'de' MMMM", { locale: es })
              : "Selecciona una fecha"}
          </h3>

          {selectedDate && (
            <div className="space-y-3">
              {getScheduledForDate(selectedDate).length === 0 ? (
                <p className="text-gray-500 text-sm">No hay envíos programados</p>
              ) : (
                getScheduledForDate(selectedDate).map((scheduled) => (
                  <div key={scheduled.id} className="border rounded-lg p-3">
                    <div className="flex items-start justify-between mb-2">
                      <div>
                        <p className="font-medium text-gray-900">
                          {scheduled.notification?.template?.name || "Notificación"}
                        </p>
                        <div className="flex items-center gap-2 mt-1">
                          <Clock className="w-3 h-3 text-gray-400" />
                          <span className="text-sm text-gray-600">
                            {format(new Date(scheduled.scheduledFor), "HH:mm")}
                          </span>
                        </div>
                      </div>
                      <Badge color={SCHEDULE_STATUS_COLORS[scheduled.status]}>
                        {scheduled.status}
                      </Badge>
                    </div>

                    {scheduled.isRecurring && (
                      <p className="text-xs text-gray-500 mb-2">
                        🔄 Recurrente: {scheduled.recurrenceType}
                      </p>
                    )}

                    {scheduled.status === "Pending" && (
                      <div className="flex gap-2 mt-3">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => pauseScheduled(scheduled.id)}
                        >
                          <Pause className="w-3 h-3 mr-1" />
                          Pausar
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-red-600"
                          onClick={() => cancelScheduled(scheduled.id)}
                        >
                          <Trash2 className="w-3 h-3 mr-1" />
                          Cancelar
                        </Button>
                      </div>
                    )}

                    {scheduled.status === "Paused" && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => pauseScheduled(scheduled.id)} // resume
                      >
                        <Play className="w-3 h-3 mr-1" />
                        Reanudar
                      </Button>
                    )}
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </div>

      {/* Create Modal */}
      {showCreateModal && (
        <ScheduleNotificationModal onClose={() => setShowCreateModal(false)} />
      )}
    </div>
  );
}
```

---

## 🚗 PÁGINA VEHÍCULO VENDIDO

### Ruta: `/vehicles/:slug/sold`

Página que se muestra cuando un usuario intenta acceder a un vehículo que ya fue vendido, mostrando alternativas similares.

### PASO 4: VehicleSoldPage

```typescript
// filepath: src/app/(main)/vehicles/[slug]/sold/page.tsx
import { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { Heart, Bell, ArrowLeft, Eye, Clock, Users } from "lucide-react";
import { getVehicleSoldInfo, getSimilarVehicles } from "@/lib/api/vehicles";
import { Button } from "@/components/ui/Button";
import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { formatCurrency } from "@/lib/utils/format";

interface Props {
  params: { slug: string };
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const vehicle = await getVehicleSoldInfo(params.slug);

  return {
    title: `${vehicle?.title || "Vehículo"} - Ya Vendido | OKLA`,
    description: `Este vehículo ya fue vendido. Te mostramos alternativas similares.`,
  };
}

export default async function VehicleSoldPage({ params }: Props) {
  const vehicle = await getVehicleSoldInfo(params.slug);
  const alternatives = await getSimilarVehicles(params.slug, 6);

  if (!vehicle) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-16 text-center">
        <h1 className="text-2xl font-bold text-gray-900 mb-4">Vehículo no encontrado</h1>
        <Button href="/vehicles">Ver todos los vehículos</Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Back Button */}
      <div className="max-w-7xl mx-auto px-4 py-4">
        <Link href="/vehicles" className="inline-flex items-center text-gray-600 hover:text-gray-900">
          <ArrowLeft className="w-4 h-4 mr-2" />
          Volver a vehículos
        </Link>
      </div>

      {/* Sold Vehicle Card */}
      <div className="max-w-4xl mx-auto px-4 py-8">
        <div className="bg-white rounded-2xl shadow-lg overflow-hidden">
          <div className="md:flex">
            {/* Image */}
            <div className="md:w-1/2 relative">
              <div className="aspect-[4/3] relative">
                <Image
                  src={vehicle.mainImage || "/placeholder-vehicle.jpg"}
                  alt={vehicle.title}
                  fill
                  className="object-cover grayscale"
                />
                {/* Sold Overlay */}
                <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                  <div className="text-center">
                    <div className="bg-red-500 text-white text-2xl font-bold px-8 py-3 rounded-lg transform -rotate-12">
                      VENDIDO
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Info */}
            <div className="md:w-1/2 p-6 md:p-8">
              <div className="flex items-center gap-2 mb-4">
                <span className="text-4xl">😢</span>
                <h1 className="text-2xl font-bold text-gray-900">
                  ¡Este vehículo ya se vendió!
                </h1>
              </div>

              <h2 className="text-xl text-gray-700 mb-2">{vehicle.title}</h2>
              <p className="text-3xl font-bold text-gray-400 line-through mb-6">
                {formatCurrency(vehicle.price)}
              </p>

              {/* Stats */}
              <div className="bg-gray-50 rounded-lg p-4 mb-6">
                <h3 className="text-sm font-medium text-gray-700 mb-3">
                  📊 Este vehículo tuvo:
                </h3>
                <div className="grid grid-cols-3 gap-4 text-center">
                  <div>
                    <Eye className="w-5 h-5 mx-auto mb-1 text-gray-400" />
                    <div className="text-lg font-semibold text-gray-700">
                      {vehicle.stats?.totalViews?.toLocaleString() || "0"}
                    </div>
                    <div className="text-xs text-gray-500">vistas</div>
                  </div>
                  <div>
                    <Heart className="w-5 h-5 mx-auto mb-1 text-gray-400" />
                    <div className="text-lg font-semibold text-gray-700">
                      {vehicle.stats?.totalFavorites || 0}
                    </div>
                    <div className="text-xs text-gray-500">favoritos</div>
                  </div>
                  <div>
                    <Clock className="w-5 h-5 mx-auto mb-1 text-gray-400" />
                    <div className="text-lg font-semibold text-gray-700">
                      {vehicle.stats?.daysListed || 0}
                    </div>
                    <div className="text-xs text-gray-500">días</div>
                  </div>
                </div>
              </div>

              {/* CTA */}
              <Button
                href="/alertas/nueva"
                fullWidth
                variant="primary"
              >
                <Bell className="w-4 h-4 mr-2" />
                Crear Alerta para Vehículos Similares
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Similar Vehicles */}
      {alternatives && alternatives.length > 0 && (
        <div className="max-w-7xl mx-auto px-4 py-12">
          <div className="text-center mb-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-2">
              ✨ Alternativas Similares Para Ti
            </h2>
            <p className="text-gray-600">
              Encontramos {alternatives.length} vehículos que podrían interesarte
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {alternatives.map((alt) => (
              <div key={alt.id} className="relative">
                <VehicleCard vehicle={alt} />
                {/* Similarity Badge */}
                <div className="absolute top-4 left-4 bg-white/90 backdrop-blur-sm rounded-full px-3 py-1">
                  <span className="text-sm font-medium text-primary-600">
                    {Math.round(alt.similarity * 100)}% similar
                  </span>
                </div>
              </div>
            ))}
          </div>

          <div className="text-center mt-8">
            <Button href="/vehicles" variant="outline">
              Ver Todos los Vehículos
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## 📢 CAMPAÑAS DE MARKETING

> ⚠️ **NOTA:** MarketingService está planificado para Q2 2026. El backend NO existe actualmente.

### Ruta: `/dealer/marketing` (Dealers) y `/admin/marketing` (Admin)

### PASO 5: CampaignsListPage (Diseño Futuro)

```typescript
// filepath: src/app/(dealer)/dealer/marketing/page.tsx
"use client";

import { useState } from "react";
import { Plus, Send, Pause, Play, BarChart3, Users, Mail } from "lucide-react";
import { useCampaigns } from "@/lib/hooks/useCampaigns";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { EmptyState } from "@/components/ui/EmptyState";

const CAMPAIGN_STATUS_CONFIG = {
  Draft: { color: "gray", label: "Borrador" },
  Scheduled: { color: "yellow", label: "Programada" },
  Running: { color: "green", label: "En ejecución" },
  Paused: { color: "blue", label: "Pausada" },
  Completed: { color: "primary", label: "Completada" },
  Cancelled: { color: "red", label: "Cancelada" },
};

export default function CampaignsPage() {
  const [activeTab, setActiveTab] = useState("all");
  const { data: campaigns, isLoading } = useCampaigns({ status: activeTab !== "all" ? activeTab : undefined });

  // MarketingService no implementado
  const isServiceAvailable = false;

  if (!isServiceAvailable) {
    return (
      <div className="p-6">
        <div className="max-w-2xl mx-auto text-center py-16">
          <div className="w-20 h-20 bg-yellow-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <Mail className="w-10 h-10 text-yellow-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 mb-4">
            Campañas de Marketing
          </h1>
          <p className="text-gray-600 mb-6">
            El sistema de campañas de email marketing estará disponible pronto.
            Te permitirá crear campañas, segmentar audiencias y analizar resultados.
          </p>
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 text-left">
            <h3 className="font-semibold text-yellow-800 mb-2">🚧 Próximamente (Q2 2026)</h3>
            <ul className="text-sm text-yellow-700 space-y-1">
              <li>• Crear campañas de email marketing</li>
              <li>• Segmentar audiencias por criterios</li>
              <li>• Programar envíos automáticos</li>
              <li>• Analizar opens, clicks y conversiones</li>
              <li>• Templates drag & drop</li>
            </ul>
          </div>
        </div>
      </div>
    );
  }

  // UI cuando el servicio esté disponible
  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Campañas de Marketing</h1>
          <p className="text-gray-600 mt-1">
            Crea y gestiona campañas de email para tus clientes
          </p>
        </div>
        <Button href="/dealer/marketing/campaigns/new">
          <Plus className="w-4 h-4 mr-2" />
          Nueva Campaña
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <StatCard
          icon={<Mail className="w-5 h-5" />}
          label="Campañas Activas"
          value={campaigns?.filter((c) => c.status === "Running").length || 0}
          color="green"
        />
        <StatCard
          icon={<Users className="w-5 h-5" />}
          label="Suscriptores"
          value="1,234"
          color="blue"
        />
        <StatCard
          icon={<Send className="w-5 h-5" />}
          label="Emails Enviados (Mes)"
          value="5,678"
          color="purple"
        />
        <StatCard
          icon={<BarChart3 className="w-5 h-5" />}
          label="Tasa de Apertura"
          value="24.5%"
          color="orange"
        />
      </div>

      {/* Campaigns Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="all">Todas</TabsTrigger>
          <TabsTrigger value="Draft">Borradores</TabsTrigger>
          <TabsTrigger value="Scheduled">Programadas</TabsTrigger>
          <TabsTrigger value="Running">En Ejecución</TabsTrigger>
          <TabsTrigger value="Completed">Completadas</TabsTrigger>
        </TabsList>

        <TabsContent value={activeTab} className="mt-6">
          {campaigns?.length === 0 ? (
            <EmptyState
              icon={<Mail className="w-12 h-12" />}
              title="No hay campañas"
              description="Crea tu primera campaña de marketing"
              action={
                <Button href="/dealer/marketing/campaigns/new">
                  Crear Campaña
                </Button>
              }
            />
          ) : (
            <div className="space-y-4">
              {campaigns?.map((campaign) => (
                <CampaignCard key={campaign.id} campaign={campaign} />
              ))}
            </div>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
}

function StatCard({ icon, label, value, color }) {
  const colorClasses = {
    green: "bg-green-100 text-green-600",
    blue: "bg-blue-100 text-blue-600",
    purple: "bg-purple-100 text-purple-600",
    orange: "bg-orange-100 text-orange-600",
  };

  return (
    <div className="bg-white rounded-lg border p-4">
      <div className="flex items-center gap-3">
        <div className={`p-2 rounded-lg ${colorClasses[color]}`}>{icon}</div>
        <div>
          <p className="text-2xl font-bold text-gray-900">{value}</p>
          <p className="text-sm text-gray-500">{label}</p>
        </div>
      </div>
    </div>
  );
}

function CampaignCard({ campaign }) {
  const statusConfig = CAMPAIGN_STATUS_CONFIG[campaign.status];

  return (
    <div className="bg-white rounded-lg border p-4 hover:shadow-md transition">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-2">
            <h3 className="font-semibold text-gray-900">{campaign.name}</h3>
            <Badge color={statusConfig.color}>{statusConfig.label}</Badge>
          </div>
          <p className="text-sm text-gray-600 mb-3">{campaign.description}</p>
          <div className="flex items-center gap-4 text-sm text-gray-500">
            <span>📧 {campaign.recipientCount} destinatarios</span>
            {campaign.scheduledFor && (
              <span>📅 Programada: {new Date(campaign.scheduledFor).toLocaleDateString()}</span>
            )}
          </div>
        </div>
        <div className="flex gap-2">
          {campaign.status === "Running" && (
            <Button variant="ghost" size="sm">
              <Pause className="w-4 h-4" />
            </Button>
          )}
          {campaign.status === "Paused" && (
            <Button variant="ghost" size="sm">
              <Play className="w-4 h-4" />
            </Button>
          )}
          <Button variant="ghost" size="sm" href={`/dealer/marketing/campaigns/${campaign.id}/stats`}>
            <BarChart3 className="w-4 h-4" />
          </Button>
        </div>
      </div>

      {/* Stats (if running or completed) */}
      {(campaign.status === "Running" || campaign.status === "Completed") && campaign.stats && (
        <div className="mt-4 pt-4 border-t grid grid-cols-4 gap-4 text-center">
          <div>
            <p className="text-lg font-semibold text-gray-900">{campaign.stats.sent}</p>
            <p className="text-xs text-gray-500">Enviados</p>
          </div>
          <div>
            <p className="text-lg font-semibold text-green-600">{campaign.stats.openRate}%</p>
            <p className="text-xs text-gray-500">Abiertos</p>
          </div>
          <div>
            <p className="text-lg font-semibold text-blue-600">{campaign.stats.clickRate}%</p>
            <p className="text-xs text-gray-500">Clicks</p>
          </div>
          <div>
            <p className="text-lg font-semibold text-purple-600">{campaign.stats.conversions}</p>
            <p className="text-xs text-gray-500">Conversiones</p>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## 👥 AUDIENCIAS Y SEGMENTOS

### Ruta: `/dealer/marketing/audiences`

```typescript
// filepath: src/app/(dealer)/dealer/marketing/audiences/page.tsx
"use client";

import { useState } from "react";
import { Plus, Users, Filter, RefreshCw } from "lucide-react";
import { useAudiences } from "@/lib/hooks/useAudiences";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";

// Tipos de segmentos predefinidos
const SEGMENT_TYPES = [
  {
    id: "interested_suv",
    name: "Interesados en SUVs",
    description: "Usuarios que han visto o guardado SUVs",
    criteria: { bodyType: "SUV" },
    icon: "🚙",
  },
  {
    id: "high_budget",
    name: "Presupuesto Alto",
    description: "Usuarios que buscan vehículos > $50,000",
    criteria: { minPrice: 50000 },
    icon: "💎",
  },
  {
    id: "recent_visitors",
    name: "Visitantes Recientes",
    description: "Usuarios que visitaron en últimos 7 días",
    criteria: { lastVisit: "7d" },
    icon: "🕐",
  },
  {
    id: "abandoned_favorites",
    name: "Favoritos Abandonados",
    description: "Usuarios con favoritos pero sin contacto",
    criteria: { hasFavorites: true, hasContact: false },
    icon: "❤️",
  },
];

export default function AudiencesPage() {
  const { data: audiences, isLoading } = useAudiences();

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Audiencias</h1>
          <p className="text-gray-600 mt-1">
            Segmenta tu audiencia para campañas más efectivas
          </p>
        </div>
        <Button href="/dealer/marketing/audiences/new">
          <Plus className="w-4 h-4 mr-2" />
          Nueva Audiencia
        </Button>
      </div>

      {/* Quick Segments */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Segmentos Rápidos</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {SEGMENT_TYPES.map((segment) => (
            <div
              key={segment.id}
              className="bg-white rounded-lg border p-4 hover:shadow-md transition cursor-pointer"
            >
              <div className="text-2xl mb-2">{segment.icon}</div>
              <h3 className="font-semibold text-gray-900">{segment.name}</h3>
              <p className="text-sm text-gray-600 mt-1">{segment.description}</p>
              <Button variant="ghost" size="sm" className="mt-3">
                <Filter className="w-3 h-3 mr-1" />
                Usar Segmento
              </Button>
            </div>
          ))}
        </div>
      </div>

      {/* Custom Audiences */}
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Mis Audiencias</h2>
        <div className="bg-white rounded-lg border divide-y">
          {audiences?.length === 0 ? (
            <div className="p-8 text-center text-gray-500">
              <Users className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No tienes audiencias personalizadas</p>
            </div>
          ) : (
            audiences?.map((audience) => (
              <div key={audience.id} className="p-4 flex items-center justify-between">
                <div>
                  <h3 className="font-medium text-gray-900">{audience.name}</h3>
                  <p className="text-sm text-gray-500 mt-1">{audience.description}</p>
                  <div className="flex items-center gap-2 mt-2">
                    <Badge variant="outline">
                      <Users className="w-3 h-3 mr-1" />
                      {audience.memberCount} miembros
                    </Badge>
                    <span className="text-xs text-gray-400">
                      Actualizado: {new Date(audience.lastRefreshed).toLocaleDateString()}
                    </span>
                  </div>
                </div>
                <div className="flex gap-2">
                  <Button variant="ghost" size="sm">
                    <RefreshCw className="w-4 h-4" />
                  </Button>
                  <Button variant="outline" size="sm">
                    Usar en Campaña
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
```

---

## 🔗 INTEGRACIONES (TEAMS)

### Ruta: `/admin/integrations/teams`

> ⚠️ **NOTA:** Teams integration está planificado como integración interna. Baja prioridad.

```typescript
// filepath: src/app/(admin)/admin/integrations/teams/page.tsx
"use client";

import { useState } from "react";
import { Plus, Trash2, TestTube, CheckCircle, XCircle } from "lucide-react";
import { useTeamsChannels, useTestTeamsWebhook } from "@/lib/hooks/useTeamsIntegration";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { Badge } from "@/components/ui/Badge";

const CHANNEL_TYPES = [
  { value: "Alerts", label: "🚨 Alertas del Sistema" },
  { value: "Sales", label: "💰 Ventas" },
  { value: "Support", label: "🎧 Soporte" },
  { value: "Compliance", label: "📋 Compliance" },
  { value: "Reports", label: "📊 Reportes" },
  { value: "General", label: "💬 General" },
];

export default function TeamsIntegrationPage() {
  const { data: channels, isLoading } = useTeamsChannels();
  const { mutate: testWebhook, isPending: isTesting } = useTestTeamsWebhook();

  const [showAddForm, setShowAddForm] = useState(false);
  const [newChannel, setNewChannel] = useState({
    name: "",
    webhookUrl: "",
    type: "General",
  });

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Integración Microsoft Teams</h1>
          <p className="text-gray-600 mt-1">
            Configura webhooks para notificaciones internas en Teams
          </p>
        </div>
        <Button onClick={() => setShowAddForm(!showAddForm)}>
          <Plus className="w-4 h-4 mr-2" />
          Agregar Canal
        </Button>
      </div>

      {/* Add Channel Form */}
      {showAddForm && (
        <div className="bg-white rounded-lg border p-6 mb-6">
          <h3 className="font-semibold text-gray-900 mb-4">Nuevo Canal de Teams</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input
              label="Nombre del canal"
              placeholder="#alerts-okla"
              value={newChannel.name}
              onChange={(e) => setNewChannel({ ...newChannel, name: e.target.value })}
            />
            <Input
              label="Webhook URL"
              placeholder="https://outlook.office.com/webhook/..."
              value={newChannel.webhookUrl}
              onChange={(e) => setNewChannel({ ...newChannel, webhookUrl: e.target.value })}
            />
            <Select
              label="Tipo"
              value={newChannel.type}
              onChange={(e) => setNewChannel({ ...newChannel, type: e.target.value })}
              options={CHANNEL_TYPES}
            />
          </div>
          <div className="mt-4 flex gap-2">
            <Button variant="outline" onClick={() => setShowAddForm(false)}>
              Cancelar
            </Button>
            <Button>Guardar Canal</Button>
          </div>
        </div>
      )}

      {/* Channels List */}
      <div className="bg-white rounded-lg border divide-y">
        {channels?.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            No hay canales de Teams configurados
          </div>
        ) : (
          channels?.map((channel) => (
            <div key={channel.id} className="p-4 flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="text-2xl">
                  {CHANNEL_TYPES.find((t) => t.value === channel.type)?.label.split(" ")[0]}
                </div>
                <div>
                  <h3 className="font-medium text-gray-900">{channel.name}</h3>
                  <p className="text-sm text-gray-500 truncate max-w-md">
                    {channel.webhookUrl}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Badge color={channel.isActive ? "green" : "gray"}>
                  {channel.isActive ? "Activo" : "Inactivo"}
                </Badge>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => testWebhook(channel.id)}
                  disabled={isTesting}
                >
                  <TestTube className="w-4 h-4" />
                </Button>
                <Button variant="ghost" size="sm" className="text-red-600">
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Info Box */}
      <div className="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h4 className="font-medium text-blue-800 mb-2">ℹ️ Cómo obtener un Webhook URL</h4>
        <ol className="text-sm text-blue-700 space-y-1 list-decimal list-inside">
          <li>En Microsoft Teams, ve al canal donde quieres recibir notificaciones</li>
          <li>Haz clic en los 3 puntos (...) junto al nombre del canal</li>
          <li>Selecciona "Conectores"</li>
          <li>Busca "Incoming Webhook" y configúralo</li>
          <li>Copia la URL del webhook y pégala aquí</li>
        </ol>
      </div>
    </div>
  );
}
```

---

## 🔌 API ENDPOINTS

### NotificationService (Backend ✅ 100%)

| Método   | Endpoint                                       | Descripción                 | Auth     |
| -------- | ---------------------------------------------- | --------------------------- | -------- |
| `POST`   | `/api/templates`                               | Crear template              | ✅ Admin |
| `GET`    | `/api/templates/{id}`                          | Obtener template            | ✅       |
| `GET`    | `/api/templates`                               | Listar templates            | ✅       |
| `PUT`    | `/api/templates/{id}`                          | Actualizar template         | ✅ Admin |
| `DELETE` | `/api/templates/{id}`                          | Eliminar template           | ✅ Admin |
| `POST`   | `/api/templates/{id}/duplicate`                | Duplicar template           | ✅ Admin |
| `POST`   | `/api/templates/{id}/preview`                  | Preview con datos           | ✅       |
| `POST`   | `/api/templates/{id}/test`                     | Enviar test email           | ✅       |
| `POST`   | `/api/notifications/scheduled`                 | Programar notificación      | ✅       |
| `GET`    | `/api/notifications/scheduled`                 | Listar programadas          | ✅       |
| `PUT`    | `/api/notifications/scheduled/{id}/reschedule` | Reprogramar                 | ✅       |
| `DELETE` | `/api/notifications/scheduled/{id}`            | Cancelar                    | ✅       |
| `POST`   | `/api/notifications/scheduled/{id}/pause`      | Pausar recurrente           | ✅       |
| `POST`   | `/api/notifications/teams/send`                | Enviar a Teams              | ✅ Admin |
| `GET`    | `/api/notifications/teams/channels`            | Listar canales configurados | ✅ Admin |

### MarketingService (Backend 🔴 0% - Planificado Q2 2026)

| Método   | Endpoint                       | Descripción        | Auth      |
| -------- | ------------------------------ | ------------------ | --------- |
| `GET`    | `/api/campaigns`               | Listar campañas    | ✅ Dealer |
| `POST`   | `/api/campaigns`               | Crear campaña      | ✅ Dealer |
| `GET`    | `/api/campaigns/{id}`          | Obtener campaña    | ✅ Dealer |
| `PUT`    | `/api/campaigns/{id}`          | Actualizar         | ✅ Dealer |
| `DELETE` | `/api/campaigns/{id}`          | Eliminar           | ✅ Dealer |
| `POST`   | `/api/campaigns/{id}/schedule` | Programar envío    | ✅ Dealer |
| `POST`   | `/api/campaigns/{id}/start`    | Iniciar campaña    | ✅ Dealer |
| `POST`   | `/api/campaigns/{id}/pause`    | Pausar campaña     | ✅ Dealer |
| `GET`    | `/api/campaigns/{id}/stats`    | Estadísticas       | ✅ Dealer |
| `GET`    | `/api/audiences`               | Listar audiencias  | ✅ Dealer |
| `POST`   | `/api/audiences`               | Crear audiencia    | ✅ Dealer |
| `GET`    | `/api/audiences/{id}/members`  | Listar miembros    | ✅ Dealer |
| `POST`   | `/api/audiences/{id}/refresh`  | Refrescar segmento | ✅ Dealer |

---

## 📦 TIPOS TYPESCRIPT

```typescript
// filepath: src/types/notificationAdmin.ts

// === TEMPLATES ===
export type NotificationTemplateType =
  | "Email"
  | "SMS"
  | "Push"
  | "InApp"
  | "WhatsApp";

export interface NotificationTemplate {
  id: string;
  name: string;
  subject?: string;
  body: string;
  type: NotificationTemplateType;
  description?: string;
  category: string;
  tags?: string;
  isActive: boolean;
  version: number;
  previousVersionId?: string;
  variables: Record<string, string>;
  previewData?: string;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

// === SCHEDULING ===
export type ScheduleStatus =
  | "Pending"
  | "Completed"
  | "Failed"
  | "Cancelled"
  | "Paused";
export type RecurrenceType =
  | "None"
  | "Daily"
  | "Weekly"
  | "Monthly"
  | "Quarterly"
  | "Yearly"
  | "Custom";

export interface ScheduledNotification {
  id: string;
  notificationId: string;
  scheduledFor: string;
  timeZone: string;
  status: ScheduleStatus;
  isRecurring: boolean;
  recurrenceType?: RecurrenceType;
  cronExpression?: string;
  nextExecution?: string;
  lastExecution?: string;
  executionCount: number;
  maxExecutions?: number;
  failureCount: number;
  lastError?: string;
  createdAt: string;
  createdBy?: string;
  notification?: {
    template?: NotificationTemplate;
  };
}

// === VEHICLE SOLD ===
export interface VehicleSoldInfo {
  id: string;
  title: string;
  slug: string;
  mainImage?: string;
  price: number;
  soldAt: string;
  stats?: {
    totalViews: number;
    totalFavorites: number;
    totalContacts: number;
    daysListed: number;
  };
}

export interface SimilarVehicle {
  id: string;
  title: string;
  slug: string;
  mainImage?: string;
  price: number;
  similarity: number; // 0.0 - 1.0
}

// === CAMPAIGNS (Futuro) ===
export type CampaignStatus =
  | "Draft"
  | "Scheduled"
  | "Running"
  | "Paused"
  | "Completed"
  | "Cancelled";
export type CampaignType =
  | "Newsletter"
  | "Promotional"
  | "Announcement"
  | "Drip"
  | "Transactional"
  | "Remarketing";

export interface Campaign {
  id: string;
  dealerId: string;
  name: string;
  description?: string;
  type: CampaignType;
  status: CampaignStatus;
  templateId?: string;
  audienceId?: string;
  recipientCount: number;
  scheduledFor?: string;
  startedAt?: string;
  completedAt?: string;
  stats?: CampaignStats;
  createdAt: string;
}

export interface CampaignStats {
  sent: number;
  delivered: number;
  opened: number;
  clicked: number;
  bounced: number;
  unsubscribed: number;
  openRate: number;
  clickRate: number;
  conversions: number;
}

// === AUDIENCES (Futuro) ===
export interface Audience {
  id: string;
  dealerId: string;
  name: string;
  description?: string;
  criteria: Record<string, any>;
  memberCount: number;
  lastRefreshed: string;
  createdAt: string;
}

// === TEAMS INTEGRATION ===
export type TeamsChannelType =
  | "Alerts"
  | "Sales"
  | "Support"
  | "Compliance"
  | "Reports"
  | "General";

export interface TeamsChannel {
  id: string;
  name: string;
  webhookUrl: string;
  type: TeamsChannelType;
  isActive: boolean;
  createdAt: string;
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Backend (NotificationService) ✅ 100%

- [x] TemplatesController - CRUD completo
- [x] ScheduledNotificationsController - Scheduling completo
- [x] NotificationPreferencesController - Preferencias usuario
- [x] UserNotificationsController - In-App notifications
- [x] WebhooksController - SendGrid/Twilio webhooks
- [x] TeamsController - Endpoints básicos (no integrado)

### Backend (MarketingService) 🔴 0%

- [ ] CampaignsController
- [ ] AudiencesController
- [ ] EmailTemplatesController (marketing)
- [ ] Integración con EventTrackingService
- [ ] Integración con BillingService (cobro por campaña)

### Frontend - Admin Templates 🔴 0%

- [ ] TemplatesListPage - Grid de templates
- [ ] TemplateEditorPage - Editor con CodeEditor
- [ ] TemplatePreviewModal - Preview con datos
- [ ] Test send functionality
- [ ] Variables sidebar

### Frontend - Scheduling 🔴 0%

- [ ] ScheduledNotificationsPage - Calendario
- [ ] ScheduleNotificationModal - Form
- [ ] Recurrence configuration
- [ ] Pause/Resume/Cancel actions

### Frontend - Vehicle Sold Page 🔴 0%

- [ ] VehicleSoldPage - Diseño responsivo
- [ ] Stats de vehículo vendido
- [ ] Alternativas similares grid
- [ ] CTA crear alerta

### Frontend - Marketing (Futuro Q2 2026) 🔴 0%

- [ ] CampaignsListPage
- [ ] CampaignEditorPage
- [ ] CampaignStatsPage
- [ ] AudiencesPage
- [ ] AudienceBuilderPage

### Frontend - Teams Integration 🔴 0%

- [ ] TeamsIntegrationPage
- [ ] Add channel form
- [ ] Test webhook button

### Hooks y Servicios 🔴 0%

- [ ] useNotificationTemplates
- [ ] useScheduledNotifications
- [ ] useCampaigns
- [ ] useAudiences
- [ ] useTeamsIntegration

---

## 📚 REFERENCIAS

- [25-notificaciones.md](25-notificaciones.md) - Centro de notificaciones (usuario)
- [process-matrix/07-NOTIFICACIONES/01-notification-service.md](../../process-matrix/07-NOTIFICACIONES/01-notification-service.md)
- [process-matrix/07-NOTIFICACIONES/02-templates-scheduling.md](../../process-matrix/07-NOTIFICACIONES/02-templates-scheduling.md)
- [process-matrix/07-NOTIFICACIONES/02-notificacion-vehiculo-vendido.md](../../process-matrix/07-NOTIFICACIONES/02-notificacion-vehiculo-vendido.md)
- [process-matrix/07-NOTIFICACIONES/03-marketing-service.md](../../process-matrix/07-NOTIFICACIONES/03-marketing-service.md)
- [process-matrix/07-NOTIFICACIONES/04-teams-integration.md](../../process-matrix/07-NOTIFICACIONES/04-teams-integration.md)

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/notificaciones-admin.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Notificaciones", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de notificaciones", async ({ page }) => {
    await page.goto("/admin/notifications");

    await expect(page.getByTestId("notifications-dashboard")).toBeVisible();
  });

  test("debe ver templates de notificación", async ({ page }) => {
    await page.goto("/admin/notifications/templates");

    await expect(page.getByTestId("templates-list")).toBeVisible();
  });

  test("debe crear nuevo template", async ({ page }) => {
    await page.goto("/admin/notifications/templates");

    await page.getByRole("button", { name: /nuevo template/i }).click();
    await page.fill('input[name="name"]', "Nuevo Template");
    await page.fill(
      'textarea[name="content"]',
      "Hola {{nombre}}, tu vehículo...",
    );
    await page.getByRole("button", { name: /guardar/i }).click();

    await expect(page.getByText(/template creado/i)).toBeVisible();
  });

  test("debe ver historial de envíos", async ({ page }) => {
    await page.goto("/admin/notifications/history");

    await expect(page.getByTestId("notifications-history")).toBeVisible();
  });

  test("debe programar notificación masiva", async ({ page }) => {
    await page.goto("/admin/notifications/broadcast");

    await page.getByRole("combobox", { name: /audiencia/i }).click();
    await page.getByRole("option", { name: /todos los dealers/i }).click();
    await page.fill(
      'textarea[name="message"]',
      "Importante: Actualización de plataforma",
    );
    await page.getByRole("button", { name: /programar envío/i }).click();

    await expect(page.getByText(/notificación programada/i)).toBeVisible();
  });
});
```

---

**Documento anterior:** `10-dealer-crm.md` - CRM, Leads, Scoring  
**Siguiente documento:** (Siguiente dominio del process-matrix)
