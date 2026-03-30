'use client';

import * as React from 'react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { AlertTriangle, CheckCircle2, ExternalLink, Flag, Mail, Scale, Shield } from 'lucide-react';
import { sanitizeText, sanitizeEmail, sanitizePhone } from '@/lib/security/sanitize';
import { sanitizeUrl } from '@/lib/security/sanitize';

// =============================================================================
// VALIDATION SCHEMA
// =============================================================================

const reporteSchema = z.object({
  urlContenido: z.string().url('URL inválida — debe ser una URL completa (https://...)'),
  tipoInfraccion: z.string().min(1, 'Selecciona el tipo de infracción'),
  descripcion: z
    .string()
    .min(10, 'La descripción debe tener al menos 10 caracteres')
    .max(5000, 'La descripción no puede exceder 5000 caracteres'),
  nombreReclamante: z
    .string()
    .min(2, 'El nombre debe tener al menos 2 caracteres')
    .max(100, 'El nombre no puede exceder 100 caracteres'),
  emailReclamante: z.string().email('Correo electrónico inválido'),
  telefonoReclamante: z
    .string()
    .optional()
    .refine(val => !val || /^[\d\s()\-+]{7,20}$/.test(val), {
      message: 'Formato de teléfono inválido',
    }),
  esPropietario: z.literal(true, {
    error: 'Debes declarar que eres el titular de los derechos',
  }),
});

type ReporteFormData = z.infer<typeof reporteSchema>;

export default function ReportarContenidoPage() {
  const [submitted, setSubmitted] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ReporteFormData>({
    resolver: zodResolver(reporteSchema),
    defaultValues: {
      urlContenido: '',
      tipoInfraccion: '',
      descripcion: '',
      nombreReclamante: '',
      emailReclamante: '',
      telefonoReclamante: '',
      esPropietario: undefined as unknown as true,
    },
  });

  const tipoInfraccion = watch('tipoInfraccion');
  const esPropietario = watch('esPropietario');

  const onSubmit = async (data: ReporteFormData) => {
    // Sanitize all inputs before sending
    const sanitizedData = {
      urlContenido: sanitizeUrl(data.urlContenido),
      tipoInfraccion: sanitizeText(data.tipoInfraccion, { maxLength: 50 }),
      descripcion: sanitizeText(data.descripcion.trim(), { maxLength: 5000 }),
      nombreReclamante: sanitizeText(data.nombreReclamante.trim(), { maxLength: 100 }),
      emailReclamante: sanitizeEmail(data.emailReclamante),
      telefonoReclamante: data.telefonoReclamante ? sanitizePhone(data.telefonoReclamante) : '',
      esPropietario: data.esPropietario,
    };

    try {
      // TODO: Replace with csrfFetch when API endpoint is ready
      await new Promise(resolve => setTimeout(resolve, 1000));
      setSubmitted(true);
      toast.success('Reporte enviado exitosamente');
    } catch {
      toast.error('Error al enviar el reporte. Inténtalo de nuevo.');
    }
  };

  if (submitted) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-12">
        <Card className="border-primary/20">
          <CardContent className="flex flex-col items-center gap-4 p-8 text-center">
            <div className="rounded-full bg-green-100 p-3 dark:bg-green-900/30">
              <CheckCircle2 className="h-8 w-8 text-green-600 dark:text-green-400" />
            </div>
            <h2 className="text-foreground text-xl font-bold">Reporte Recibido</h2>
            <p className="text-muted-foreground text-sm">
              Hemos recibido tu reporte de contenido. Nuestro equipo legal lo revisará y tomará las
              acciones correspondientes conforme a la <strong>Ley 65-00</strong>.
            </p>
            <p className="text-muted-foreground text-sm">
              Recibirás una respuesta en tu correo electrónico dentro de{' '}
              <strong>5 días hábiles</strong>.
            </p>
            <Button
              onClick={() => {
                setSubmitted(false);
                reset();
              }}
              variant="outline"
              className="mt-4"
            >
              Enviar otro reporte
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-12">
      <div className="mb-8 space-y-2">
        <h1 className="text-foreground text-3xl font-bold">
          Reporte de Contenido / Solicitud de Retiro
        </h1>
        <p className="text-muted-foreground">
          Si consideras que algún contenido publicado en OKLA infringe tus derechos de propiedad
          intelectual o viola nuestros términos de uso, puedes solicitar su retiro conforme a la{' '}
          <strong>Ley 65-00</strong> sobre Derecho de Autor en República Dominicana.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Flag className="text-primary h-5 w-5" />
                Formulario de Reporte
              </CardTitle>
              <CardDescription>
                Complete la información para reportar contenido que infringe derechos o viola
                términos.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                {/* URL del contenido */}
                <div className="space-y-2">
                  <Label htmlFor="urlContenido">URL del contenido infractor *</Label>
                  <Input
                    id="urlContenido"
                    type="url"
                    placeholder="https://okla.com.do/vehiculos/..."
                    {...register('urlContenido')}
                  />
                  {errors.urlContenido ? (
                    <p className="text-sm text-red-500">{errors.urlContenido.message}</p>
                  ) : (
                    <p className="text-muted-foreground text-xs">
                      Pega la URL completa del listado o contenido que deseas reportar.
                    </p>
                  )}
                </div>

                {/* Tipo de infracción */}
                <div className="space-y-2">
                  <Label htmlFor="tipoInfraccion">Tipo de infracción *</Label>
                  <Select
                    value={tipoInfraccion}
                    onValueChange={value =>
                      setValue('tipoInfraccion', value, { shouldValidate: true })
                    }
                  >
                    <SelectTrigger id="tipoInfraccion">
                      <SelectValue placeholder="Seleccione el tipo de infracción" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="propiedad-intelectual">
                        Infracción de propiedad intelectual (fotos, textos, marcas)
                      </SelectItem>
                      <SelectItem value="marca-registrada">
                        Uso no autorizado de marca registrada
                      </SelectItem>
                      <SelectItem value="derechos-autor">Violación de derechos de autor</SelectItem>
                      <SelectItem value="contenido-falso">Contenido falso o engañoso</SelectItem>
                      <SelectItem value="suplantacion">Suplantación de identidad</SelectItem>
                      <SelectItem value="datos-personales">
                        Publicación de datos personales sin consentimiento
                      </SelectItem>
                      <SelectItem value="otro">Otro tipo de violación</SelectItem>
                    </SelectContent>
                  </Select>
                  {errors.tipoInfraccion && (
                    <p className="text-sm text-red-500">{errors.tipoInfraccion.message}</p>
                  )}
                </div>

                {/* Descripción */}
                <div className="space-y-2">
                  <Label htmlFor="descripcion">Descripción de la infracción *</Label>
                  <Textarea
                    id="descripcion"
                    placeholder="Describa detalladamente la infracción, incluyendo qué derechos se están violando y cómo puede verificarse la titularidad de los mismos..."
                    {...register('descripcion')}
                    rows={5}
                    className="resize-y"
                  />
                  {errors.descripcion && (
                    <p className="text-sm text-red-500">{errors.descripcion.message}</p>
                  )}
                </div>

                {/* Datos del reclamante */}
                <div className="border-border bg-muted/30 space-y-4 rounded-lg border p-4">
                  <h3 className="text-foreground text-sm font-semibold">Datos del reclamante</h3>

                  <div className="space-y-2">
                    <Label htmlFor="nombreReclamante">Nombre completo o razón social *</Label>
                    <Input
                      id="nombreReclamante"
                      placeholder="Nombre del titular de los derechos"
                      {...register('nombreReclamante')}
                    />
                    {errors.nombreReclamante && (
                      <p className="text-sm text-red-500">{errors.nombreReclamante.message}</p>
                    )}
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="emailReclamante">Correo electrónico *</Label>
                      <Input
                        id="emailReclamante"
                        type="email"
                        placeholder="tu@email.com"
                        {...register('emailReclamante')}
                      />
                      {errors.emailReclamante && (
                        <p className="text-sm text-red-500">{errors.emailReclamante.message}</p>
                      )}
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="telefonoReclamante">Teléfono</Label>
                      <Input
                        id="telefonoReclamante"
                        type="tel"
                        placeholder="809-000-0000"
                        {...register('telefonoReclamante')}
                      />
                      {errors.telefonoReclamante && (
                        <p className="text-sm text-red-500">{errors.telefonoReclamante.message}</p>
                      )}
                    </div>
                  </div>

                  <label className="flex cursor-pointer items-start gap-2">
                    <input
                      type="checkbox"
                      checked={esPropietario === true}
                      onChange={e =>
                        setValue('esPropietario', e.target.checked as true, {
                          shouldValidate: true,
                        })
                      }
                      className="text-primary focus:ring-primary border-border mt-0.5 h-4 w-4 rounded"
                    />
                    <span className="text-muted-foreground text-xs">
                      Declaro bajo juramento que soy el titular de los derechos infringidos o estoy
                      autorizado para actuar en nombre del titular, y que la información
                      proporcionada es veraz y exacta. *
                    </span>
                  </label>
                  {errors.esPropietario && (
                    <p className="text-sm text-red-500">{errors.esPropietario.message}</p>
                  )}
                </div>

                <Button type="submit" className="w-full" disabled={isSubmitting}>
                  {isSubmitting ? 'Enviando...' : 'Enviar Reporte'}
                </Button>
              </form>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          {/* Legal reference */}
          <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-900/50 dark:bg-blue-950/20">
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <Scale className="mt-0.5 h-5 w-5 text-blue-600 dark:text-blue-400" />
                <div>
                  <h3 className="text-foreground text-sm font-semibold">Ley 65-00</h3>
                  <p className="text-muted-foreground mt-1 text-xs">
                    La <strong>Ley 65-00</strong> sobre Derecho de Autor en República Dominicana
                    protege las obras del intelecto humano. OKLA respeta los derechos de propiedad
                    intelectual y actuará conforme a esta ley ante reportes válidos.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* ONDA */}
          <Card>
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <Shield className="text-primary mt-0.5 h-5 w-5" />
                <div className="space-y-2">
                  <h3 className="text-foreground text-sm font-semibold">ONDA</h3>
                  <p className="text-muted-foreground text-xs">
                    Oficina Nacional de Derecho de Autor – entidad encargada de la protección del
                    derecho de autor en República Dominicana.
                  </p>
                  <a
                    href="https://www.onda.gob.do"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-primary flex items-center gap-1.5 text-xs hover:underline"
                  >
                    <ExternalLink className="h-3.5 w-3.5" />
                    www.onda.gob.do
                  </a>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Designated agent */}
          <Card>
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <Mail className="text-primary mt-0.5 h-5 w-5" />
                <div>
                  <h3 className="text-foreground text-sm font-semibold">
                    Agente Designado para Takedowns
                  </h3>
                  <p className="text-muted-foreground mt-1 text-xs">
                    Para solicitudes formales de retiro de contenido por infracción de propiedad
                    intelectual, contacte a:
                  </p>
                  <a
                    href="mailto:legal@okla.com.do"
                    className="text-primary mt-1 block text-sm hover:underline"
                  >
                    legal@okla.com.do
                  </a>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Warning */}
          <Card className="border-amber-200 bg-amber-50/50 dark:border-amber-900/50 dark:bg-amber-950/20">
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <AlertTriangle className="mt-0.5 h-5 w-5 text-amber-600 dark:text-amber-400" />
                <div>
                  <h3 className="text-foreground text-sm font-semibold">Importante</h3>
                  <p className="text-muted-foreground mt-1 text-xs">
                    Presentar reclamaciones falsas o de mala fe puede acarrear responsabilidad
                    legal. Solo envíe reportes cuando tenga motivos legítimos para creer que existe
                    una infracción.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
