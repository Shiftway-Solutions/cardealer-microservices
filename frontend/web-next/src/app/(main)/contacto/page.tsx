/**
 * Contact Page
 *
 * Contact form and information
 *
 * Route: /contacto
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { toast } from 'sonner';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Mail, Phone, MapPin, Clock, Send, CheckCircle, MessageCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useSiteConfig } from '@/providers/site-config-provider';
import { sanitizeText, sanitizeEmail, sanitizePhone } from '@/lib/security/sanitize';
import { csrfFetch } from '@/lib/security/csrf';

// =============================================================================
// VALIDATION SCHEMA
// =============================================================================

const contactSchema = z.object({
  name: z
    .string()
    .min(2, 'El nombre debe tener al menos 2 caracteres')
    .max(100, 'El nombre no puede exceder 100 caracteres'),
  email: z.string().email('Correo electrónico inválido'),
  phone: z
    .string()
    .optional()
    .refine(val => !val || /^[\d\s()\-+]{7,20}$/.test(val), {
      message: 'Formato de teléfono inválido',
    }),
  subject: z.string().min(1, 'Selecciona un asunto'),
  message: z
    .string()
    .min(10, 'El mensaje debe tener al menos 10 caracteres')
    .max(5000, 'El mensaje no puede exceder 5000 caracteres'),
});

type ContactFormData = z.infer<typeof contactSchema>;

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function ContactoPage() {
  const config = useSiteConfig();
  const [isSubmitted, setIsSubmitted] = React.useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ContactFormData>({
    resolver: zodResolver(contactSchema),
    defaultValues: {
      name: '',
      email: '',
      phone: '',
      subject: '',
      message: '',
    },
  });

  const onSubmit = async (data: ContactFormData) => {
    // Sanitize all inputs before sending
    const sanitizedData = {
      name: sanitizeText(data.name.trim(), { maxLength: 100 }),
      email: sanitizeEmail(data.email),
      phone: data.phone ? sanitizePhone(data.phone) : '',
      subject: sanitizeText(data.subject.trim(), { maxLength: 200 }),
      message: sanitizeText(data.message.trim(), { maxLength: 5000 }),
    };

    try {
      const response = await csrfFetch('/api/contact', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(sanitizedData),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => null);
        throw new Error(error?.error || 'Error al enviar el mensaje');
      }

      setIsSubmitted(true);
    } catch (err) {
      const error = err as { message?: string };
      toast.error(error?.message || 'Error al enviar el mensaje. Inténtalo de nuevo.');
    }
  };

  if (isSubmitted) {
    return (
      <div className="flex min-h-[70vh] items-center justify-center px-4">
        <div className="text-center">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
            <CheckCircle className="h-8 w-8 text-green-600" />
          </div>
          <h1 className="text-foreground mt-6 text-2xl font-bold">¡Mensaje enviado!</h1>
          <p className="text-muted-foreground mt-2 max-w-md">
            Gracias por contactarnos. Hemos recibido tu mensaje y te responderemos a la brevedad
            posible.
          </p>
          <div className="mt-8 flex flex-col items-center gap-4 sm:flex-row sm:justify-center">
            <Button asChild className="gap-2 bg-[#00A870] hover:bg-[#009663]">
              <Link href="/">Volver al inicio</Link>
            </Button>
            <Button asChild variant="outline">
              <Link href="/vehiculos">Ver vehículos</Link>
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="bg-gradient-to-br from-[#00A870] to-[#007850] py-16 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <h1 className="text-3xl font-bold md:text-4xl">Contáctanos</h1>
            <p className="mt-4 text-white/90">
              ¿Tienes preguntas, sugerencias o necesitas ayuda? Estamos aquí para ti.
            </p>
          </div>
        </div>
      </section>

      {/* Main Content */}
      <section className="py-12">
        <div className="container mx-auto px-4">
          <div className="grid gap-8 lg:grid-cols-3">
            {/* Contact Info */}
            <div className="lg:col-span-1">
              <h2 className="text-foreground text-xl font-semibold">Información de contacto</h2>
              <p className="text-muted-foreground mt-2">
                Puedes comunicarte con nosotros a través de cualquiera de estos canales.
              </p>

              <div className="mt-8 space-y-6">
                <div className="flex gap-4">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#00A870]/10">
                    <Mail className="h-5 w-5 text-[#00A870]" />
                  </div>
                  <div>
                    <div className="text-foreground font-medium">Correo electrónico</div>
                    <a
                      href={`mailto:${config.supportEmail}`}
                      className="text-[#00A870] hover:underline"
                    >
                      {config.supportEmail}
                    </a>
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#00A870]/10">
                    <Phone className="h-5 w-5 text-[#00A870]" />
                  </div>
                  <div>
                    <div className="text-foreground font-medium">Teléfono</div>
                    <a href={config.phoneHref} className="text-[#00A870] hover:underline">
                      {config.supportPhone}
                    </a>
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#00A870]/10">
                    <MessageCircle className="h-5 w-5 text-[#00A870]" />
                  </div>
                  <div>
                    <div className="text-foreground font-medium">WhatsApp</div>
                    <a
                      href={config.whatsappUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-[#00A870] hover:underline"
                    >
                      {config.supportPhone}
                    </a>
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#00A870]/10">
                    <MapPin className="h-5 w-5 text-[#00A870]" />
                  </div>
                  <div>
                    <div className="text-foreground font-medium">Ubicación</div>
                    <div className="text-muted-foreground">
                      {config.address.split(',').map((part, i) => (
                        <React.Fragment key={i}>
                          {part.trim()}
                          {i < config.address.split(',').length - 1 && <br />}
                        </React.Fragment>
                      ))}
                    </div>
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#00A870]/10">
                    <Clock className="h-5 w-5 text-[#00A870]" />
                  </div>
                  <div>
                    <div className="text-foreground font-medium">Horario de atención</div>
                    <div className="text-muted-foreground">
                      Lunes a Viernes: 8:00 AM - 6:00 PM
                      <br />
                      Sábados: 9:00 AM - 1:00 PM
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Contact Form */}
            <div className="lg:col-span-2">
              <Card>
                <CardContent className="p-6">
                  <h2 className="text-foreground text-xl font-semibold">Envíanos un mensaje</h2>
                  <p className="text-muted-foreground mt-2">
                    Completa el formulario y te responderemos en menos de 24 horas.
                  </p>

                  <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-6">
                    <div className="grid gap-6 sm:grid-cols-2">
                      <div className="space-y-2">
                        <Label htmlFor="name">Nombre completo *</Label>
                        <Input
                          id="name"
                          {...register('name')}
                          placeholder="Tu nombre"
                          aria-invalid={!!errors.name}
                        />
                        {errors.name && (
                          <p className="text-sm text-red-500">{errors.name.message}</p>
                        )}
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="email">Correo electrónico *</Label>
                        <Input
                          id="email"
                          type="email"
                          {...register('email')}
                          placeholder="tu@email.com"
                          aria-invalid={!!errors.email}
                        />
                        {errors.email && (
                          <p className="text-sm text-red-500">{errors.email.message}</p>
                        )}
                      </div>
                    </div>

                    <div className="grid gap-6 sm:grid-cols-2">
                      <div className="space-y-2">
                        <Label htmlFor="phone">Teléfono</Label>
                        <Input
                          id="phone"
                          type="tel"
                          {...register('phone')}
                          placeholder="(809) 555-1234"
                        />
                        {errors.phone && (
                          <p className="text-sm text-red-500">{errors.phone.message}</p>
                        )}
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="subject">Asunto *</Label>
                        <select
                          id="subject"
                          {...register('subject')}
                          className="border-input bg-background ring-offset-background focus-visible:ring-ring flex h-10 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:outline-none"
                          aria-invalid={!!errors.subject}
                        >
                          <option value="">Seleccionar asunto...</option>
                          <option value="general">Consulta general</option>
                          <option value="support">Soporte técnico</option>
                          <option value="sales">Información de ventas</option>
                          <option value="dealer">Quiero ser dealer</option>
                          <option value="complaint">Queja o reclamo</option>
                          <option value="suggestion">Sugerencia</option>
                        </select>
                        {errors.subject && (
                          <p className="text-sm text-red-500">{errors.subject.message}</p>
                        )}
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="message">Mensaje *</Label>
                      <textarea
                        id="message"
                        {...register('message')}
                        placeholder="Escribe tu mensaje aquí..."
                        rows={5}
                        className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring flex w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:outline-none"
                        aria-invalid={!!errors.message}
                      />
                      {errors.message && (
                        <p className="text-sm text-red-500">{errors.message.message}</p>
                      )}
                    </div>

                    <Button
                      type="submit"
                      size="lg"
                      className="w-full gap-2 bg-[#00A870] hover:bg-[#009663] sm:w-auto"
                      disabled={isSubmitting}
                    >
                      {isSubmitting ? (
                        <>
                          <div className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                          Enviando...
                        </>
                      ) : (
                        <>
                          <Send className="h-4 w-4" />
                          Enviar mensaje
                        </>
                      )}
                    </Button>
                  </form>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </section>

      {/* FAQ CTA */}
      <section className="bg-muted/50 py-12">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-foreground text-2xl font-bold">¿Tienes preguntas frecuentes?</h2>
          <p className="text-muted-foreground mt-2">
            Revisa nuestra sección de ayuda donde respondemos las dudas más comunes.
          </p>
          <Button asChild variant="outline" className="mt-6">
            <Link href="/ayuda">Ver preguntas frecuentes</Link>
          </Button>
        </div>
      </section>
    </div>
  );
}
