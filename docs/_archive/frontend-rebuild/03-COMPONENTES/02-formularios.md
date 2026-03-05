# 📝 Componentes de Formularios

> **Tiempo estimado:** 60 minutos
> **Prerrequisitos:** shadcn/ui instalado, react-hook-form, zod

---

## 📋 OBJETIVO

Implementar sistema de formularios completo:

- Validación con Zod
- Integración con react-hook-form
- Componentes reutilizables
- Estados de error/éxito
- Formularios complejos (multi-step, conditional)
- Accesibilidad WCAG 2.1

---

## 🔧 PASO 1: Instalación

```bash
# Instalar dependencias
pnpm add react-hook-form @hookform/resolvers zod

# Instalar componentes shadcn/ui
pnpm dlx shadcn@latest add form input select checkbox radio-group textarea label
```

---

## 🔧 PASO 2: Componentes Base de Formulario

### FormField Wrapper

```typescript
// filepath: src/components/ui/FormField.tsx
"use client";

import * as React from "react";
import { useFormContext, Controller, FieldPath, FieldValues } from "react-hook-form";
import { Label } from "@/components/ui/Label";
import { cn } from "@/lib/utils";

interface FormFieldProps<T extends FieldValues> {
  name: FieldPath<T>;
  label?: string;
  description?: string;
  required?: boolean;
  children: React.ReactElement;
  className?: string;
}

export function FormField<T extends FieldValues>({
  name,
  label,
  description,
  required,
  children,
  className,
}: FormFieldProps<T>) {
  const { control, formState: { errors } } = useFormContext<T>();

  const error = errors[name]?.message as string | undefined;
  const id = `field-${name}`;
  const errorId = `${id}-error`;
  const descId = `${id}-description`;

  return (
    <div className={cn("space-y-2", className)}>
      {label && (
        <Label htmlFor={id} className="flex items-center gap-1">
          {label}
          {required && <span className="text-red-500" aria-hidden="true">*</span>}
        </Label>
      )}

      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          React.cloneElement(children, {
            ...field,
            id,
            "aria-invalid": !!error,
            "aria-describedby": cn(
              error && errorId,
              description && descId
            ) || undefined,
            "aria-required": required,
            className: cn(
              children.props.className,
              error && "border-red-500 focus:ring-red-500"
            ),
          })
        )}
      />

      {description && !error && (
        <p id={descId} className="text-sm text-gray-500">
          {description}
        </p>
      )}

      {error && (
        <p id={errorId} className="text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
```

### Input Component

```typescript
// filepath: src/components/ui/Input.tsx
import * as React from "react";
import { cn } from "@/lib/utils";

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type = "text", error, leftIcon, rightIcon, ...props }, ref) => {
    return (
      <div className="relative">
        {leftIcon && (
          <div className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
            {leftIcon}
          </div>
        )}
        <input
          type={type}
          className={cn(
            "flex h-10 w-full rounded-lg border border-gray-300 bg-white px-3 py-2",
            "text-sm text-gray-900 placeholder:text-gray-400",
            "transition-colors duration-200",
            "focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent",
            "disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-gray-50",
            error && "border-red-500 focus:ring-red-500",
            leftIcon && "pl-10",
            rightIcon && "pr-10",
            className
          )}
          ref={ref}
          {...props}
        />
        {rightIcon && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400">
            {rightIcon}
          </div>
        )}
      </div>
    );
  }
);
Input.displayName = "Input";
```

### Textarea Component

```typescript
// filepath: src/components/ui/Textarea.tsx
import * as React from "react";
import { cn } from "@/lib/utils";

export interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
}

export const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, error, ...props }, ref) => {
    return (
      <textarea
        className={cn(
          "flex min-h-[120px] w-full rounded-lg border border-gray-300 bg-white px-3 py-2",
          "text-sm text-gray-900 placeholder:text-gray-400",
          "transition-colors duration-200 resize-y",
          "focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent",
          "disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-gray-50",
          error && "border-red-500 focus:ring-red-500",
          className
        )}
        ref={ref}
        {...props}
      />
    );
  }
);
Textarea.displayName = "Textarea";
```

### Select Component

```typescript
// filepath: src/components/ui/Select.tsx
"use client";

import * as React from "react";
import * as SelectPrimitive from "@radix-ui/react-select";
import { Check, ChevronDown, ChevronUp } from "lucide-react";
import { cn } from "@/lib/utils";

export interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface SelectProps {
  value?: string;
  onValueChange?: (value: string) => void;
  options: SelectOption[];
  placeholder?: string;
  disabled?: boolean;
  error?: boolean;
  className?: string;
  "aria-label"?: string;
}

export const Select = React.forwardRef<HTMLButtonElement, SelectProps>(
  (
    {
      value,
      onValueChange,
      options,
      placeholder = "Seleccionar...",
      disabled,
      error,
      className,
      "aria-label": ariaLabel,
    },
    ref
  ) => {
    return (
      <SelectPrimitive.Root value={value} onValueChange={onValueChange} disabled={disabled}>
        <SelectPrimitive.Trigger
          ref={ref}
          aria-label={ariaLabel}
          className={cn(
            "flex h-10 w-full items-center justify-between rounded-lg border border-gray-300 bg-white px-3 py-2",
            "text-sm text-gray-900 placeholder:text-gray-400",
            "transition-colors duration-200",
            "focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent",
            "disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-gray-50",
            error && "border-red-500 focus:ring-red-500",
            className
          )}
        >
          <SelectPrimitive.Value placeholder={placeholder} />
          <SelectPrimitive.Icon>
            <ChevronDown className="h-4 w-4 text-gray-400" />
          </SelectPrimitive.Icon>
        </SelectPrimitive.Trigger>

        <SelectPrimitive.Portal>
          <SelectPrimitive.Content
            className={cn(
              "relative z-dropdown overflow-hidden rounded-lg border border-gray-200 bg-white shadow-lg",
              "data-[state=open]:animate-in data-[state=closed]:animate-out",
              "data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0",
              "data-[side=bottom]:slide-in-from-top-2 data-[side=top]:slide-in-from-bottom-2"
            )}
            position="popper"
            sideOffset={5}
          >
            <SelectPrimitive.ScrollUpButton className="flex h-6 items-center justify-center bg-white">
              <ChevronUp className="h-4 w-4" />
            </SelectPrimitive.ScrollUpButton>

            <SelectPrimitive.Viewport className="p-1 max-h-60">
              {options.map((option) => (
                <SelectPrimitive.Item
                  key={option.value}
                  value={option.value}
                  disabled={option.disabled}
                  className={cn(
                    "relative flex w-full cursor-pointer select-none items-center rounded-md py-2 pl-10 pr-4",
                    "text-sm text-gray-900 outline-none",
                    "focus:bg-gray-100",
                    "data-[disabled]:pointer-events-none data-[disabled]:opacity-50"
                  )}
                >
                  <SelectPrimitive.ItemIndicator className="absolute left-3">
                    <Check className="h-4 w-4 text-primary-600" />
                  </SelectPrimitive.ItemIndicator>
                  <SelectPrimitive.ItemText>{option.label}</SelectPrimitive.ItemText>
                </SelectPrimitive.Item>
              ))}
            </SelectPrimitive.Viewport>

            <SelectPrimitive.ScrollDownButton className="flex h-6 items-center justify-center bg-white">
              <ChevronDown className="h-4 w-4" />
            </SelectPrimitive.ScrollDownButton>
          </SelectPrimitive.Content>
        </SelectPrimitive.Portal>
      </SelectPrimitive.Root>
    );
  }
);
Select.displayName = "Select";
```

### Checkbox Component

```typescript
// filepath: src/components/ui/Checkbox.tsx
"use client";

import * as React from "react";
import * as CheckboxPrimitive from "@radix-ui/react-checkbox";
import { Check, Minus } from "lucide-react";
import { cn } from "@/lib/utils";

interface CheckboxProps extends React.ComponentPropsWithoutRef<typeof CheckboxPrimitive.Root> {
  label?: string;
  description?: string;
}

export const Checkbox = React.forwardRef<
  React.ElementRef<typeof CheckboxPrimitive.Root>,
  CheckboxProps
>(({ className, label, description, id, ...props }, ref) => {
  const generatedId = React.useId();
  const checkboxId = id ?? generatedId;

  return (
    <div className="flex items-start gap-3">
      <CheckboxPrimitive.Root
        ref={ref}
        id={checkboxId}
        className={cn(
          "peer h-5 w-5 shrink-0 rounded border border-gray-300 bg-white",
          "transition-colors duration-200",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2",
          "disabled:cursor-not-allowed disabled:opacity-50",
          "data-[state=checked]:bg-primary-600 data-[state=checked]:border-primary-600 data-[state=checked]:text-white",
          "data-[state=indeterminate]:bg-primary-600 data-[state=indeterminate]:border-primary-600 data-[state=indeterminate]:text-white",
          className
        )}
        {...props}
      >
        <CheckboxPrimitive.Indicator className="flex items-center justify-center text-current">
          {props.checked === "indeterminate" ? (
            <Minus className="h-3.5 w-3.5" />
          ) : (
            <Check className="h-3.5 w-3.5" />
          )}
        </CheckboxPrimitive.Indicator>
      </CheckboxPrimitive.Root>

      {(label || description) && (
        <div className="grid gap-1 leading-none">
          {label && (
            <label
              htmlFor={checkboxId}
              className="text-sm font-medium text-gray-900 cursor-pointer peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              {label}
            </label>
          )}
          {description && (
            <p className="text-sm text-gray-500">{description}</p>
          )}
        </div>
      )}
    </div>
  );
});
Checkbox.displayName = "Checkbox";
```

### Radio Group

```typescript
// filepath: src/components/ui/RadioGroup.tsx
"use client";

import * as React from "react";
import * as RadioGroupPrimitive from "@radix-ui/react-radio-group";
import { Circle } from "lucide-react";
import { cn } from "@/lib/utils";

interface RadioOption {
  value: string;
  label: string;
  description?: string;
  disabled?: boolean;
}

interface RadioGroupProps extends React.ComponentPropsWithoutRef<typeof RadioGroupPrimitive.Root> {
  options: RadioOption[];
  orientation?: "horizontal" | "vertical";
}

export const RadioGroup = React.forwardRef<
  React.ElementRef<typeof RadioGroupPrimitive.Root>,
  RadioGroupProps
>(({ className, options, orientation = "vertical", ...props }, ref) => {
  return (
    <RadioGroupPrimitive.Root
      ref={ref}
      className={cn(
        "grid gap-3",
        orientation === "horizontal" && "grid-flow-col auto-cols-max",
        className
      )}
      {...props}
    >
      {options.map((option) => (
        <div key={option.value} className="flex items-start gap-3">
          <RadioGroupPrimitive.Item
            id={`radio-${option.value}`}
            value={option.value}
            disabled={option.disabled}
            className={cn(
              "h-5 w-5 shrink-0 rounded-full border border-gray-300 bg-white",
              "transition-colors duration-200",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2",
              "disabled:cursor-not-allowed disabled:opacity-50",
              "data-[state=checked]:border-primary-600"
            )}
          >
            <RadioGroupPrimitive.Indicator className="flex items-center justify-center">
              <Circle className="h-2.5 w-2.5 fill-primary-600 text-primary-600" />
            </RadioGroupPrimitive.Indicator>
          </RadioGroupPrimitive.Item>

          <div className="grid gap-1 leading-none">
            <label
              htmlFor={`radio-${option.value}`}
              className="text-sm font-medium text-gray-900 cursor-pointer"
            >
              {option.label}
            </label>
            {option.description && (
              <p className="text-sm text-gray-500">{option.description}</p>
            )}
          </div>
        </div>
      ))}
    </RadioGroupPrimitive.Root>
  );
});
RadioGroup.displayName = "RadioGroup";
```

---

## 🔧 PASO 3: Campos Especializados

### Price Input

```typescript
// filepath: src/components/form/PriceInput.tsx
"use client";

import * as React from "react";
import { cn, formatPrice, parsePrice } from "@/lib/utils";

interface PriceInputProps {
  value?: number;
  onChange?: (value: number | undefined) => void;
  placeholder?: string;
  min?: number;
  max?: number;
  disabled?: boolean;
  error?: boolean;
  className?: string;
  id?: string;
  name?: string;
  "aria-describedby"?: string;
}

export const PriceInput = React.forwardRef<HTMLInputElement, PriceInputProps>(
  (
    {
      value,
      onChange,
      placeholder = "0",
      min,
      max,
      disabled,
      error,
      className,
      ...props
    },
    ref
  ) => {
    const [displayValue, setDisplayValue] = React.useState<string>(() =>
      value ? formatPrice(value).replace("RD$", "").trim() : ""
    );

    // Sync display value when external value changes
    React.useEffect(() => {
      if (value !== undefined) {
        setDisplayValue(formatPrice(value).replace("RD$", "").trim());
      } else {
        setDisplayValue("");
      }
    }, [value]);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const rawValue = e.target.value;

      // Remove non-numeric characters except commas
      const cleaned = rawValue.replace(/[^\d,]/g, "");
      setDisplayValue(cleaned);

      // Parse and notify parent
      const parsed = parsePrice(cleaned);

      if (parsed !== undefined) {
        if ((min !== undefined && parsed < min) || (max !== undefined && parsed > max)) {
          return;
        }
      }

      onChange?.(parsed);
    };

    const handleBlur = () => {
      // Format on blur
      if (value !== undefined) {
        setDisplayValue(formatPrice(value).replace("RD$", "").trim());
      }
    };

    return (
      <div className="relative">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm font-medium">
          RD$
        </span>
        <input
          ref={ref}
          type="text"
          inputMode="numeric"
          value={displayValue}
          onChange={handleChange}
          onBlur={handleBlur}
          placeholder={placeholder}
          disabled={disabled}
          className={cn(
            "flex h-10 w-full rounded-lg border border-gray-300 bg-white pl-12 pr-3 py-2",
            "text-sm text-gray-900 placeholder:text-gray-400",
            "transition-colors duration-200",
            "focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent",
            "disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-gray-50",
            error && "border-red-500 focus:ring-red-500",
            className
          )}
          {...props}
        />
      </div>
    );
  }
);
PriceInput.displayName = "PriceInput";
```

### Phone Input

```typescript
// filepath: src/components/form/PhoneInput.tsx
"use client";

import * as React from "react";
import { Phone } from "lucide-react";
import { cn } from "@/lib/utils";

interface PhoneInputProps {
  value?: string;
  onChange?: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  error?: boolean;
  className?: string;
  id?: string;
  name?: string;
}

export const PhoneInput = React.forwardRef<HTMLInputElement, PhoneInputProps>(
  (
    {
      value = "",
      onChange,
      placeholder = "809-123-4567",
      disabled,
      error,
      className,
      ...props
    },
    ref
  ) => {
    const formatPhone = (input: string): string => {
      // Remove all non-digits
      const digits = input.replace(/\D/g, "");

      // Format as XXX-XXX-XXXX
      if (digits.length <= 3) return digits;
      if (digits.length <= 6) return `${digits.slice(0, 3)}-${digits.slice(3)}`;
      return `${digits.slice(0, 3)}-${digits.slice(3, 6)}-${digits.slice(6, 10)}`;
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const formatted = formatPhone(e.target.value);
      onChange?.(formatted);
    };

    return (
      <div className="relative">
        <Phone
          className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          size={18}
        />
        <input
          ref={ref}
          type="tel"
          value={value}
          onChange={handleChange}
          placeholder={placeholder}
          disabled={disabled}
          maxLength={12} // XXX-XXX-XXXX
          className={cn(
            "flex h-10 w-full rounded-lg border border-gray-300 bg-white pl-10 pr-3 py-2",
            "text-sm text-gray-900 placeholder:text-gray-400",
            "transition-colors duration-200",
            "focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent",
            "disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-gray-50",
            error && "border-red-500 focus:ring-red-500",
            className
          )}
          {...props}
        />
      </div>
    );
  }
);
PhoneInput.displayName = "PhoneInput";
```

### Year Selector

```typescript
// filepath: src/components/form/YearSelector.tsx
"use client";

import * as React from "react";
import { Select } from "@/components/ui/Select";

interface YearSelectorProps {
  value?: string;
  onChange?: (value: string) => void;
  minYear?: number;
  maxYear?: number;
  placeholder?: string;
  disabled?: boolean;
  error?: boolean;
  className?: string;
}

export function YearSelector({
  value,
  onChange,
  minYear = 1990,
  maxYear = new Date().getFullYear() + 1,
  placeholder = "Seleccionar año",
  disabled,
  error,
  className,
}: YearSelectorProps) {
  const years = React.useMemo(() => {
    const result = [];
    for (let year = maxYear; year >= minYear; year--) {
      result.push({ value: year.toString(), label: year.toString() });
    }
    return result;
  }, [minYear, maxYear]);

  return (
    <Select
      value={value}
      onValueChange={onChange}
      options={years}
      placeholder={placeholder}
      disabled={disabled}
      error={error}
      className={className}
    />
  );
}
```

### Image Upload

```typescript
// filepath: src/components/form/ImageUpload.tsx
"use client";

import * as React from "react";
import Image from "next/image";
import { Upload, X, ImageIcon, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface UploadedImage {
  id: string;
  url: string;
  name: string;
}

interface ImageUploadProps {
  value?: UploadedImage[];
  onChange?: (images: UploadedImage[]) => void;
  maxFiles?: number;
  maxSize?: number; // in bytes
  onUpload?: (file: File) => Promise<UploadedImage>;
  disabled?: boolean;
  error?: string;
  className?: string;
}

export function ImageUpload({
  value = [],
  onChange,
  maxFiles = 10,
  maxSize = 5 * 1024 * 1024, // 5MB
  onUpload,
  disabled,
  error,
  className,
}: ImageUploadProps) {
  const [isUploading, setIsUploading] = React.useState(false);
  const [uploadError, setUploadError] = React.useState<string | null>(null);
  const inputRef = React.useRef<HTMLInputElement>(null);

  const canUploadMore = value.length < maxFiles;

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    if (files.length === 0) return;

    setUploadError(null);

    // Check file count
    if (value.length + files.length > maxFiles) {
      setUploadError(`Máximo ${maxFiles} imágenes permitidas`);
      return;
    }

    // Check file sizes
    const oversizedFile = files.find((f) => f.size > maxSize);
    if (oversizedFile) {
      setUploadError(`"${oversizedFile.name}" excede el tamaño máximo de ${maxSize / 1024 / 1024}MB`);
      return;
    }

    // Check file types
    const invalidFile = files.find((f) => !f.type.startsWith("image/"));
    if (invalidFile) {
      setUploadError(`"${invalidFile.name}" no es una imagen válida`);
      return;
    }

    if (!onUpload) {
      console.warn("ImageUpload: onUpload prop is required");
      return;
    }

    setIsUploading(true);

    try {
      const uploadedImages: UploadedImage[] = [];

      for (const file of files) {
        const uploaded = await onUpload(file);
        uploadedImages.push(uploaded);
      }

      onChange?.([...value, ...uploadedImages]);
    } catch (err) {
      setUploadError("Error al subir las imágenes. Intenta de nuevo.");
    } finally {
      setIsUploading(false);
      // Clear input
      if (inputRef.current) {
        inputRef.current.value = "";
      }
    }
  };

  const handleRemove = (id: string) => {
    onChange?.(value.filter((img) => img.id !== id));
  };

  const handleReorder = (fromIndex: number, toIndex: number) => {
    const newImages = [...value];
    const [removed] = newImages.splice(fromIndex, 1);
    newImages.splice(toIndex, 0, removed);
    onChange?.(newImages);
  };

  return (
    <div className={cn("space-y-4", className)}>
      {/* Upload Area */}
      {canUploadMore && (
        <div
          className={cn(
            "relative border-2 border-dashed rounded-lg p-8 text-center transition-colors",
            disabled || isUploading
              ? "border-gray-200 bg-gray-50 cursor-not-allowed"
              : "border-gray-300 hover:border-primary-400 cursor-pointer"
          )}
          onClick={() => !disabled && !isUploading && inputRef.current?.click()}
        >
          <input
            ref={inputRef}
            type="file"
            accept="image/*"
            multiple
            onChange={handleFileSelect}
            disabled={disabled || isUploading}
            className="sr-only"
            aria-label="Subir imágenes"
          />

          <div className="flex flex-col items-center gap-3">
            {isUploading ? (
              <Loader2 className="h-10 w-10 text-primary-600 animate-spin" />
            ) : (
              <Upload className="h-10 w-10 text-gray-400" />
            )}

            <div>
              <p className="text-sm font-medium text-gray-700">
                {isUploading
                  ? "Subiendo imágenes..."
                  : "Arrastra imágenes o haz clic para subir"}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                PNG, JPG o WebP. Máx {maxSize / 1024 / 1024}MB por imagen
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Error Message */}
      {(error || uploadError) && (
        <p className="text-sm text-red-600" role="alert">
          {error || uploadError}
        </p>
      )}

      {/* Image Grid */}
      {value.length > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
          {value.map((image, index) => (
            <div
              key={image.id}
              className="relative aspect-[4/3] rounded-lg overflow-hidden border border-gray-200 group"
            >
              <Image
                src={image.url}
                alt={image.name}
                fill
                className="object-cover"
                sizes="(max-width: 640px) 50vw, (max-width: 768px) 33vw, 25vw"
              />

              {/* Primary Badge */}
              {index === 0 && (
                <span className="absolute top-2 left-2 bg-primary-600 text-white text-xs px-2 py-0.5 rounded">
                  Principal
                </span>
              )}

              {/* Remove Button */}
              <Button
                type="button"
                variant="destructive"
                size="icon"
                className="absolute top-2 right-2 h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={() => handleRemove(image.id)}
                disabled={disabled}
              >
                <X size={14} />
                <span className="sr-only">Eliminar imagen</span>
              </Button>
            </div>
          ))}
        </div>
      )}

      {/* Upload Count */}
      <p className="text-sm text-gray-500">
        {value.length} de {maxFiles} imágenes
      </p>
    </div>
  );
}
```

---

## 🔧 PASO 4: Formulario de Búsqueda

### SearchFilters Component

```typescript
// filepath: src/components/search/SearchFilters.tsx
"use client";

import * as React from "react";
import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Filter, X } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { Input } from "@/components/ui/Input";
import { PriceInput } from "@/components/form/PriceInput";
import { YearSelector } from "@/components/form/YearSelector";
import { useMakes, useModels } from "@/lib/hooks/useCatalog";
import { cn } from "@/lib/utils";

const searchFiltersSchema = z.object({
  query: z.string().optional(),
  makeId: z.string().optional(),
  modelId: z.string().optional(),
  yearMin: z.string().optional(),
  yearMax: z.string().optional(),
  priceMin: z.number().optional(),
  priceMax: z.number().optional(),
  condition: z.enum(["new", "used", "all"]).optional(),
  bodyType: z.string().optional(),
  transmission: z.enum(["automatic", "manual", "all"]).optional(),
  fuelType: z.string().optional(),
});

export type SearchFiltersData = z.infer<typeof searchFiltersSchema>;

interface SearchFiltersProps {
  defaultValues?: Partial<SearchFiltersData>;
  onSubmit: (data: SearchFiltersData) => void;
  isLoading?: boolean;
  className?: string;
  layout?: "horizontal" | "vertical";
}

export function SearchFilters({
  defaultValues,
  onSubmit,
  isLoading,
  className,
  layout = "vertical",
}: SearchFiltersProps) {
  const methods = useForm<SearchFiltersData>({
    resolver: zodResolver(searchFiltersSchema),
    defaultValues: {
      condition: "all",
      transmission: "all",
      ...defaultValues,
    },
  });

  const { watch, setValue, reset, handleSubmit } = methods;
  const selectedMakeId = watch("makeId");

  // Fetch data
  const { data: makes } = useMakes();
  const { data: models } = useModels(selectedMakeId ?? "");

  // Reset model when make changes
  React.useEffect(() => {
    setValue("modelId", undefined);
  }, [selectedMakeId, setValue]);

  // Check if any filters are active
  const hasActiveFilters = React.useMemo(() => {
    const values = methods.getValues();
    return Object.entries(values).some(([key, value]) => {
      if (key === "condition" && value === "all") return false;
      if (key === "transmission" && value === "all") return false;
      return value !== undefined && value !== "";
    });
  }, [methods]);

  const handleReset = () => {
    reset({
      query: "",
      makeId: undefined,
      modelId: undefined,
      yearMin: undefined,
      yearMax: undefined,
      priceMin: undefined,
      priceMax: undefined,
      condition: "all",
      bodyType: undefined,
      transmission: "all",
      fuelType: undefined,
    });
    onSubmit({} as SearchFiltersData);
  };

  const makeOptions = React.useMemo(
    () =>
      makes?.data.map((m) => ({ value: m.id, label: m.name })) ?? [],
    [makes]
  );

  const modelOptions = React.useMemo(
    () =>
      models?.data.map((m) => ({ value: m.id, label: m.name })) ?? [],
    [models]
  );

  return (
    <FormProvider {...methods}>
      <form
        onSubmit={handleSubmit(onSubmit)}
        className={cn(
          layout === "horizontal"
            ? "flex flex-wrap items-end gap-4"
            : "space-y-4",
          className
        )}
      >
        {/* Search Query */}
        <div className={cn(layout === "horizontal" && "flex-1 min-w-[200px]")}>
          <label className="text-sm font-medium text-gray-700 mb-1 block">
            Buscar
          </label>
          <Input
            {...methods.register("query")}
            placeholder="Marca, modelo, palabra clave..."
          />
        </div>

        {/* Make */}
        <div className={cn(layout === "horizontal" && "w-40")}>
          <label className="text-sm font-medium text-gray-700 mb-1 block">
            Marca
          </label>
          <Select
            value={watch("makeId") ?? ""}
            onValueChange={(v) => setValue("makeId", v || undefined)}
            options={makeOptions}
            placeholder="Todas"
          />
        </div>

        {/* Model */}
        <div className={cn(layout === "horizontal" && "w-40")}>
          <label className="text-sm font-medium text-gray-700 mb-1 block">
            Modelo
          </label>
          <Select
            value={watch("modelId") ?? ""}
            onValueChange={(v) => setValue("modelId", v || undefined)}
            options={modelOptions}
            placeholder="Todos"
            disabled={!selectedMakeId}
          />
        </div>

        {/* Year Range */}
        <div className={cn(layout === "horizontal" && "flex gap-2")}>
          <div className={cn(layout === "horizontal" && "w-28")}>
            <label className="text-sm font-medium text-gray-700 mb-1 block">
              Año desde
            </label>
            <YearSelector
              value={watch("yearMin")}
              onChange={(v) => setValue("yearMin", v || undefined)}
              placeholder="Desde"
            />
          </div>
          <div className={cn(layout === "horizontal" && "w-28")}>
            <label className="text-sm font-medium text-gray-700 mb-1 block">
              Año hasta
            </label>
            <YearSelector
              value={watch("yearMax")}
              onChange={(v) => setValue("yearMax", v || undefined)}
              placeholder="Hasta"
            />
          </div>
        </div>

        {/* Price Range */}
        <div className={cn(layout === "horizontal" && "flex gap-2")}>
          <div className={cn(layout === "horizontal" && "w-36")}>
            <label className="text-sm font-medium text-gray-700 mb-1 block">
              Precio desde
            </label>
            <PriceInput
              value={watch("priceMin")}
              onChange={(v) => setValue("priceMin", v)}
              placeholder="Mínimo"
            />
          </div>
          <div className={cn(layout === "horizontal" && "w-36")}>
            <label className="text-sm font-medium text-gray-700 mb-1 block">
              Precio hasta
            </label>
            <PriceInput
              value={watch("priceMax")}
              onChange={(v) => setValue("priceMax", v)}
              placeholder="Máximo"
            />
          </div>
        </div>

        {/* Condition */}
        <div className={cn(layout === "horizontal" && "w-32")}>
          <label className="text-sm font-medium text-gray-700 mb-1 block">
            Condición
          </label>
          <Select
            value={watch("condition") ?? "all"}
            onValueChange={(v) =>
              setValue("condition", v as "new" | "used" | "all")
            }
            options={[
              { value: "all", label: "Todos" },
              { value: "new", label: "Nuevos" },
              { value: "used", label: "Usados" },
            ]}
          />
        </div>

        {/* Actions */}
        <div className={cn(
          "flex gap-2",
          layout === "horizontal" && "items-end"
        )}>
          <Button type="submit" loading={isLoading}>
            <Filter size={16} className="mr-1" />
            Buscar
          </Button>

          {hasActiveFilters && (
            <Button type="button" variant="ghost" onClick={handleReset}>
              <X size={16} className="mr-1" />
              Limpiar
            </Button>
          )}
        </div>
      </form>
    </FormProvider>
  );
}
```

---

## 🔧 PASO 5: Formulario de Publicar Vehículo

### Multi-step Form

```typescript
// filepath: src/components/vehicles/VehicleForm.tsx
"use client";

import * as React from "react";
import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/Button";
import { StepIndicator } from "@/components/ui/StepIndicator";
import { VehicleFormStep1 } from "./VehicleFormStep1";
import { VehicleFormStep2 } from "./VehicleFormStep2";
import { VehicleFormStep3 } from "./VehicleFormStep3";
import { VehicleFormStep4 } from "./VehicleFormStep4";

const vehicleFormSchema = z.object({
  // Step 1: Basic Info
  makeId: z.string({ required_error: "Selecciona una marca" }),
  modelId: z.string({ required_error: "Selecciona un modelo" }),
  year: z.string({ required_error: "Selecciona el año" }),
  trim: z.string().optional(),
  condition: z.enum(["new", "used"], { required_error: "Selecciona la condición" }),

  // Step 2: Details
  mileage: z.number().min(0, "Kilometraje inválido").optional(),
  fuelType: z.string().optional(),
  transmission: z.enum(["automatic", "manual"]).optional(),
  drivetrain: z.string().optional(),
  exteriorColor: z.string().optional(),
  interiorColor: z.string().optional(),
  vin: z.string().length(17, "VIN debe tener 17 caracteres").optional().or(z.literal("")),

  // Step 3: Description & Price
  title: z.string().min(10, "Mínimo 10 caracteres").max(100, "Máximo 100 caracteres"),
  description: z.string().min(50, "Mínimo 50 caracteres").max(5000, "Máximo 5000 caracteres"),
  price: z.number().min(1, "Ingresa un precio válido"),
  priceNegotiable: z.boolean().default(false),

  // Step 4: Images
  images: z.array(z.object({
    id: z.string(),
    url: z.string(),
    name: z.string(),
  })).min(3, "Mínimo 3 fotos").max(20, "Máximo 20 fotos"),

  // Step 5: Contact
  contactPhone: z.string().min(10, "Teléfono requerido"),
  city: z.string({ required_error: "Selecciona la ciudad" }),
  showExactLocation: z.boolean().default(false),
});

export type VehicleFormData = z.infer<typeof vehicleFormSchema>;

const STEPS = [
  { id: 1, title: "Información Básica" },
  { id: 2, title: "Detalles" },
  { id: 3, title: "Descripción y Precio" },
  { id: 4, title: "Fotos" },
];

interface VehicleFormProps {
  defaultValues?: Partial<VehicleFormData>;
  onSubmit: (data: VehicleFormData) => Promise<void>;
  isEditing?: boolean;
}

export function VehicleForm({
  defaultValues,
  onSubmit,
  isEditing = false,
}: VehicleFormProps) {
  const [currentStep, setCurrentStep] = React.useState(1);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  const methods = useForm<VehicleFormData>({
    resolver: zodResolver(vehicleFormSchema),
    defaultValues: {
      condition: "used",
      priceNegotiable: false,
      showExactLocation: false,
      images: [],
      ...defaultValues,
    },
    mode: "onChange",
  });

  // Fields to validate per step
  const stepFields: Record<number, (keyof VehicleFormData)[]> = {
    1: ["makeId", "modelId", "year", "condition"],
    2: ["mileage", "transmission"],
    3: ["title", "description", "price"],
    4: ["images", "contactPhone", "city"],
  };

  const validateStep = async (step: number): Promise<boolean> => {
    const fields = stepFields[step];
    const result = await methods.trigger(fields);
    return result;
  };

  const handleNext = async () => {
    const isValid = await validateStep(currentStep);
    if (isValid && currentStep < STEPS.length) {
      setCurrentStep((prev) => prev + 1);
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep((prev) => prev - 1);
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const handleFormSubmit = async (data: VehicleFormData) => {
    setIsSubmitting(true);
    try {
      await onSubmit(data);
    } finally {
      setIsSubmitting(false);
    }
  };

  const renderStep = () => {
    switch (currentStep) {
      case 1:
        return <VehicleFormStep1 />;
      case 2:
        return <VehicleFormStep2 />;
      case 3:
        return <VehicleFormStep3 />;
      case 4:
        return <VehicleFormStep4 />;
      default:
        return null;
    }
  };

  return (
    <FormProvider {...methods}>
      <form onSubmit={methods.handleSubmit(handleFormSubmit)}>
        {/* Step Indicator */}
        <StepIndicator
          steps={STEPS}
          currentStep={currentStep}
          className="mb-8"
        />

        {/* Step Content */}
        <div className="min-h-[400px]">{renderStep()}</div>

        {/* Navigation */}
        <div className="flex justify-between mt-8 pt-6 border-t">
          <Button
            type="button"
            variant="outline"
            onClick={handleBack}
            disabled={currentStep === 1}
          >
            Anterior
          </Button>

          {currentStep < STEPS.length ? (
            <Button type="button" onClick={handleNext}>
              Siguiente
            </Button>
          ) : (
            <Button type="submit" loading={isSubmitting}>
              {isEditing ? "Guardar Cambios" : "Publicar Vehículo"}
            </Button>
          )}
        </div>
      </form>
    </FormProvider>
  );
}
```

### Step Indicator

```typescript
// filepath: src/components/ui/StepIndicator.tsx
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

interface Step {
  id: number;
  title: string;
}

interface StepIndicatorProps {
  steps: Step[];
  currentStep: number;
  className?: string;
}

export function StepIndicator({
  steps,
  currentStep,
  className,
}: StepIndicatorProps) {
  return (
    <nav aria-label="Progreso del formulario" className={className}>
      <ol className="flex items-center justify-between">
        {steps.map((step, index) => {
          const isCompleted = currentStep > step.id;
          const isCurrent = currentStep === step.id;

          return (
            <li
              key={step.id}
              className={cn(
                "flex items-center",
                index < steps.length - 1 && "flex-1"
              )}
            >
              <div className="flex flex-col items-center">
                {/* Circle */}
                <div
                  className={cn(
                    "flex h-10 w-10 items-center justify-center rounded-full border-2 text-sm font-medium transition-colors",
                    isCompleted
                      ? "bg-primary-600 border-primary-600 text-white"
                      : isCurrent
                      ? "border-primary-600 text-primary-600 bg-white"
                      : "border-gray-300 text-gray-500 bg-white"
                  )}
                  aria-current={isCurrent ? "step" : undefined}
                >
                  {isCompleted ? (
                    <Check size={20} aria-hidden="true" />
                  ) : (
                    step.id
                  )}
                </div>

                {/* Label */}
                <span
                  className={cn(
                    "mt-2 text-xs font-medium text-center",
                    isCurrent ? "text-primary-600" : "text-gray-500"
                  )}
                >
                  {step.title}
                </span>
              </div>

              {/* Connector Line */}
              {index < steps.length - 1 && (
                <div
                  className={cn(
                    "flex-1 h-0.5 mx-4",
                    isCompleted ? "bg-primary-600" : "bg-gray-300"
                  )}
                  aria-hidden="true"
                />
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
```

---

## ✅ VALIDACIÓN

### Tests de Formularios

```typescript
// filepath: __tests__/components/form/PriceInput.test.tsx
import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import { PriceInput } from "@/components/form/PriceInput";

describe("PriceInput", () => {
  it("displays formatted value", () => {
    render(<PriceInput value={1500000} />);

    expect(screen.getByRole("textbox")).toHaveValue("1,500,000");
  });

  it("calls onChange with parsed number", async () => {
    const handleChange = vi.fn();
    const { user } = render(<PriceInput onChange={handleChange} />);

    await user.type(screen.getByRole("textbox"), "1500000");

    expect(handleChange).toHaveBeenCalledWith(1500000);
  });

  it("shows RD$ prefix", () => {
    render(<PriceInput />);

    expect(screen.getByText("RD$")).toBeInTheDocument();
  });
});
```

### Ejecutar verificación

```bash
# Tests
pnpm test components/form

# Verificar formularios
pnpm dev
# Navegar a /publicar y verificar:
# - Validación en tiempo real
# - Navegación entre pasos
# - Subida de imágenes
# - Formateo de precio
```

---

## 📊 RESUMEN

| Componente    | Archivo                    | Función                 |
| ------------- | -------------------------- | ----------------------- |
| FormField     | `ui/FormField.tsx`         | Wrapper con label/error |
| Input         | `ui/Input.tsx`             | Input base              |
| Textarea      | `ui/Textarea.tsx`          | Área de texto           |
| Select        | `ui/Select.tsx`            | Dropdown con Radix      |
| Checkbox      | `ui/Checkbox.tsx`          | Checkbox accesible      |
| RadioGroup    | `ui/RadioGroup.tsx`        | Radio buttons           |
| PriceInput    | `form/PriceInput.tsx`      | Precio con formato RD$  |
| PhoneInput    | `form/PhoneInput.tsx`      | Teléfono DR format      |
| YearSelector  | `form/YearSelector.tsx`    | Selector de año         |
| ImageUpload   | `form/ImageUpload.tsx`     | Subida múltiple         |
| SearchFilters | `search/SearchFilters.tsx` | Filtros de búsqueda     |
| VehicleForm   | `vehicles/VehicleForm.tsx` | Formulario multi-step   |
| StepIndicator | `ui/StepIndicator.tsx`     | Indicador de pasos      |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/03-COMPONENTES/03-vehiculos.md`
