# 📋 Formularios - Patrones de Envío

> **Tiempo estimado:** 35 minutos
> **Prerrequisitos:** React Hook Form, Zod

---

## 📋 OBJETIVO

Patrones estándar para formularios:

- Validación con Zod
- Manejo de errores de API
- Loading states
- Dirty state tracking

---

## 🔧 PASO 1: useFormSubmit Hook

```typescript
// filepath: src/lib/hooks/useFormSubmit.ts
"use client";

import * as React from "react";
import { showToast } from "@/lib/toast";

interface UseFormSubmitOptions<TData, TResult> {
  onSubmit: (data: TData) => Promise<TResult>;
  onSuccess?: (result: TResult) => void;
  onError?: (error: Error) => void;
  successMessage?: string;
  errorMessage?: string;
}

export function useFormSubmit<TData, TResult>({
  onSubmit,
  onSuccess,
  onError,
  successMessage = "Guardado exitosamente",
  errorMessage = "Error al guardar",
}: UseFormSubmitOptions<TData, TResult>) {
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const submit = React.useCallback(
    async (data: TData) => {
      setIsSubmitting(true);
      setError(null);

      try {
        const result = await onSubmit(data);
        showToast.success(successMessage);
        onSuccess?.(result);
        return result;
      } catch (err) {
        const message = err instanceof Error ? err.message : errorMessage;
        setError(message);
        showToast.error(message);
        onError?.(err instanceof Error ? err : new Error(message));
        throw err;
      } finally {
        setIsSubmitting(false);
      }
    },
    [onSubmit, onSuccess, onError, successMessage, errorMessage],
  );

  return { submit, isSubmitting, error };
}
```

---

## 🔧 PASO 2: Formulario de Contacto

```typescript
// filepath: src/components/forms/ContactForm.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Button } from "@/components/ui/Button";
import { useFormSubmit } from "@/lib/hooks/useFormSubmit";
import { contactService } from "@/lib/services/contactService";

const contactSchema = z.object({
  name: z.string().min(2, "Nombre muy corto"),
  email: z.string().email("Email inválido"),
  phone: z.string().min(10, "Teléfono inválido"),
  message: z.string().min(10, "Mensaje muy corto"),
});

type ContactFormData = z.infer<typeof contactSchema>;

interface ContactFormProps {
  vehicleId: string;
  onSuccess?: () => void;
}

export function ContactForm({ vehicleId, onSuccess }: ContactFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<ContactFormData>({
    resolver: zodResolver(contactSchema),
  });

  const { submit, isSubmitting } = useFormSubmit({
    onSubmit: (data) =>
      contactService.sendInquiry({ ...data, vehicleId }),
    onSuccess: () => {
      reset();
      onSuccess?.();
    },
    successMessage: "Mensaje enviado al vendedor",
  });

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-4">
      <FormField label="Nombre" error={errors.name?.message}>
        <Input {...register("name")} placeholder="Tu nombre" />
      </FormField>

      <FormField label="Email" error={errors.email?.message}>
        <Input {...register("email")} type="email" placeholder="tu@email.com" />
      </FormField>

      <FormField label="Teléfono" error={errors.phone?.message}>
        <Input {...register("phone")} type="tel" placeholder="809-000-0000" />
      </FormField>

      <FormField label="Mensaje" error={errors.message?.message}>
        <Textarea
          {...register("message")}
          rows={4}
          placeholder="Escribe tu mensaje..."
        />
      </FormField>

      <Button type="submit" disabled={isSubmitting || !isDirty} className="w-full">
        {isSubmitting ? "Enviando..." : "Enviar mensaje"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 3: Formulario de Perfil

```typescript
// filepath: src/components/forms/ProfileForm.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Button } from "@/components/ui/Button";
import { AvatarUpload } from "@/components/forms/AvatarUpload";
import { useFormSubmit } from "@/lib/hooks/useFormSubmit";
import { userService } from "@/lib/services/userService";
import type { User } from "@/types";

const profileSchema = z.object({
  name: z.string().min(2, "Nombre muy corto"),
  email: z.string().email(),
  phone: z.string().optional(),
  bio: z.string().max(500).optional(),
  location: z.string().optional(),
  image: z.string().optional(),
});

type ProfileFormData = z.infer<typeof profileSchema>;

interface ProfileFormProps {
  user: User;
  onSuccess?: () => void;
}

export function ProfileForm({ user, onSuccess }: ProfileFormProps) {
  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isDirty },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      name: user.name || "",
      email: user.email,
      phone: user.phone || "",
      bio: user.bio || "",
      location: user.location || "",
      image: user.image || "",
    },
  });

  const { submit, isSubmitting } = useFormSubmit({
    onSubmit: (data) => userService.updateProfile(user.id, data),
    onSuccess,
    successMessage: "Perfil actualizado",
  });

  const currentImage = watch("image");

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-6">
      {/* Avatar */}
      <div className="flex justify-center">
        <AvatarUpload
          currentImage={currentImage}
          name={user.name}
          onUpload={(url) => setValue("image", url, { shouldDirty: true })}
        />
      </div>

      {/* Name */}
      <FormField label="Nombre completo" error={errors.name?.message}>
        <Input {...register("name")} />
      </FormField>

      {/* Email (disabled) */}
      <FormField label="Email">
        <Input {...register("email")} disabled className="bg-gray-50" />
      </FormField>

      {/* Phone */}
      <FormField label="Teléfono" error={errors.phone?.message}>
        <Input {...register("phone")} type="tel" placeholder="809-000-0000" />
      </FormField>

      {/* Location */}
      <FormField label="Ubicación" error={errors.location?.message}>
        <Input {...register("location")} placeholder="Santo Domingo" />
      </FormField>

      {/* Bio */}
      <FormField label="Biografía" error={errors.bio?.message}>
        <Textarea
          {...register("bio")}
          rows={3}
          placeholder="Cuéntanos sobre ti..."
        />
      </FormField>

      {/* Submit */}
      <Button type="submit" disabled={isSubmitting || !isDirty}>
        {isSubmitting ? "Guardando..." : "Guardar cambios"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 4: UnsavedChangesWarning

```typescript
// filepath: src/components/forms/UnsavedChangesWarning.tsx
"use client";

import * as React from "react";
import { useRouter } from "next/navigation";

interface UnsavedChangesWarningProps {
  isDirty: boolean;
  message?: string;
}

export function UnsavedChangesWarning({
  isDirty,
  message = "Tienes cambios sin guardar. ¿Estás seguro de que quieres salir?",
}: UnsavedChangesWarningProps) {
  const router = useRouter();

  React.useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isDirty) {
        e.preventDefault();
        e.returnValue = message;
        return message;
      }
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isDirty, message]);

  return null;
}

// Usage:
// <UnsavedChangesWarning isDirty={formState.isDirty} />
```

---

## 🔧 PASO 5: Test del Form Hook

```typescript
// filepath: src/lib/hooks/__tests__/useFormSubmit.test.ts
import { renderHook, act, waitFor } from "@testing-library/react";
import { useFormSubmit } from "../useFormSubmit";

describe("useFormSubmit", () => {
  it("submits successfully", async () => {
    const onSubmit = vi.fn().mockResolvedValue({ id: "1" });
    const onSuccess = vi.fn();

    const { result } = renderHook(() =>
      useFormSubmit({
        onSubmit,
        onSuccess,
      }),
    );

    expect(result.current.isSubmitting).toBe(false);

    await act(async () => {
      await result.current.submit({ name: "Test" });
    });

    expect(onSubmit).toHaveBeenCalledWith({ name: "Test" });
    expect(onSuccess).toHaveBeenCalledWith({ id: "1" });
  });

  it("handles errors", async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error("Failed"));
    const onError = vi.fn();

    const { result } = renderHook(() =>
      useFormSubmit({
        onSubmit,
        onError,
      }),
    );

    await act(async () => {
      try {
        await result.current.submit({ name: "Test" });
      } catch {}
    });

    expect(result.current.error).toBe("Failed");
    expect(onError).toHaveBeenCalled();
  });
});
```

---

## ✅ VALIDACIÓN

```bash
pnpm test src/lib/hooks/__tests__/useFormSubmit.test.ts
# Verificar:
# - Submit exitoso
# - Manejo de errores
# - Loading state
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/06-TESTING/02-coverage-ci.md`
