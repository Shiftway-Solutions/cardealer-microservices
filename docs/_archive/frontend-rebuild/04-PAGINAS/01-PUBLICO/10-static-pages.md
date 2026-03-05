# 📄 Páginas Estáticas

> **Tiempo estimado:** 15 minutos  
> **Páginas:** AboutPage, ContactPage, FAQPage, TermsPage, PrivacyPage

---

## 📋 OBJETIVO

Páginas informativas y legales:

- Sobre nosotros
- Contacto
- FAQ
- Términos y condiciones
- Política de privacidad

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ SOBRE NOSOTROS                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ [Imagen Hero]                                               │ │
│ │                                                             │ │
│ │              OKLA - El Marketplace de Vehículos             │ │
│ │               más grande de República Dominicana            │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ NUESTRA HISTORIA                                                │
│ Lorem ipsum dolor sit amet, consectetur adipiscing elit...      │
│                                                                 │
│ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐        │
│ │ 🚗 10,000+     │ │ 👥 50,000+     │ │ ✅ 500+        │        │
│ │ Vehículos      │ │ Usuarios       │ │ Dealers        │        │
│ └────────────────┘ └────────────────┘ └────────────────┘        │
│                                                                 │
│ NUESTRO EQUIPO                                                  │
│ [Foto] [Foto] [Foto] [Foto]                                     │
│ CEO   CTO   CMO   COO                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

### Página Sobre Nosotros

```typescript
// filepath: src/app/(public)/about/page.tsx
import { Card, CardContent } from '@/components/ui/card';
import { Car, Users, Building, Award } from 'lucide-react';

const stats = [
  { icon: Car, value: '10,000+', label: 'Vehículos' },
  { icon: Users, value: '50,000+', label: 'Usuarios' },
  { icon: Building, value: '500+', label: 'Dealers' },
  { icon: Award, value: '#1', label: 'En RD' },
];

export default function AboutPage() {
  return (
    <div className="container max-w-4xl mx-auto py-12 px-4">
      {/* Hero */}
      <div className="text-center mb-12">
        <h1 className="text-4xl font-bold mb-4">Sobre OKLA</h1>
        <p className="text-xl text-gray-600">
          El marketplace de vehículos más grande de República Dominicana
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-12">
        {stats.map((stat) => (
          <Card key={stat.label}>
            <CardContent className="pt-6 text-center">
              <stat.icon className="w-8 h-8 mx-auto text-primary-600 mb-2" />
              <div className="text-2xl font-bold">{stat.value}</div>
              <div className="text-sm text-gray-600">{stat.label}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Content */}
      <div className="prose prose-lg max-w-none">
        <h2>Nuestra Historia</h2>
        <p>
          OKLA nació con una visión simple: hacer que comprar y vender vehículos
          en República Dominicana sea fácil, seguro y transparente.
        </p>

        <h2>Nuestra Misión</h2>
        <p>
          Conectar compradores y vendedores de vehículos a través de una plataforma
          tecnológica que priorice la confianza y la experiencia del usuario.
        </p>

        <h2>Nuestros Valores</h2>
        <ul>
          <li><strong>Transparencia:</strong> Precios claros sin sorpresas</li>
          <li><strong>Seguridad:</strong> Verificación de dealers y vehículos</li>
          <li><strong>Innovación:</strong> Tecnología al servicio del usuario</li>
          <li><strong>Servicio:</strong> Soporte cuando lo necesites</li>
        </ul>
      </div>
    </div>
  );
}
```

### Página de Contacto

```typescript
// filepath: src/app/(public)/contact/page.tsx
'use client';

import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { publicService } from '@/services/api/publicService';
import { Phone, Mail, MapPin, Clock, Send } from 'lucide-react';
import { toast } from 'sonner';

export default function ContactPage() {
  const [form, setForm] = useState({ name: '', email: '', subject: '', message: '' });

  const submitMutation = useMutation({
    mutationFn: (data: typeof form) => publicService.submitContactForm(data),
    onSuccess: () => {
      setForm({ name: '', email: '', subject: '', message: '' });
      toast.success('Mensaje enviado. Te responderemos pronto.');
    },
  });

  return (
    <div className="container max-w-5xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold text-center mb-8">Contáctanos</h1>

      <div className="grid md:grid-cols-2 gap-8">
        {/* Contact Info */}
        <div className="space-y-6">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-start gap-4">
                <Phone className="w-6 h-6 text-primary-600" />
                <div>
                  <div className="font-medium">Teléfono</div>
                  <a href="tel:+18095551234" className="text-gray-600">+1 (809) 555-1234</a>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-start gap-4">
                <Mail className="w-6 h-6 text-primary-600" />
                <div>
                  <div className="font-medium">Email</div>
                  <a href="mailto:info@okla.com.do" className="text-gray-600">info@okla.com.do</a>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-start gap-4">
                <MapPin className="w-6 h-6 text-primary-600" />
                <div>
                  <div className="font-medium">Dirección</div>
                  <p className="text-gray-600">
                    Av. Winston Churchill #1099<br />
                    Santo Domingo, República Dominicana
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-start gap-4">
                <Clock className="w-6 h-6 text-primary-600" />
                <div>
                  <div className="font-medium">Horario</div>
                  <p className="text-gray-600">
                    Lunes a Viernes: 9:00 AM - 6:00 PM<br />
                    Sábados: 9:00 AM - 1:00 PM
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Contact Form */}
        <Card>
          <CardHeader>
            <CardTitle>Envíanos un mensaje</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              className="space-y-4"
              onSubmit={(e) => { e.preventDefault(); submitMutation.mutate(form); }}
            >
              <Input
                placeholder="Tu nombre"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                required
              />
              <Input
                type="email"
                placeholder="Tu email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                required
              />
              <Input
                placeholder="Asunto"
                value={form.subject}
                onChange={(e) => setForm({ ...form, subject: e.target.value })}
                required
              />
              <Textarea
                placeholder="Tu mensaje"
                rows={5}
                value={form.message}
                onChange={(e) => setForm({ ...form, message: e.target.value })}
                required
              />
              <Button type="submit" className="w-full" disabled={submitMutation.isPending}>
                <Send className="w-4 h-4 mr-2" />
                Enviar Mensaje
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
```

### Página FAQ

```typescript
// filepath: src/app/(public)/faq/page.tsx
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';

const faqs = [
  {
    question: '¿Cómo publico mi vehículo?',
    answer: 'Regístrate, haz clic en "Vender" y completa el formulario con las fotos y detalles de tu vehículo. Es gratis para particulares.',
  },
  {
    question: '¿Cuánto cuesta publicar?',
    answer: 'Para particulares, la primera publicación es gratis. Para dealers, tenemos planes desde $49/mes.',
  },
  {
    question: '¿Cómo contacto a un vendedor?',
    answer: 'En cada listado verás botones para llamar, enviar WhatsApp o mensaje interno.',
  },
  {
    question: '¿Los vehículos están verificados?',
    answer: 'Los dealers verificados tienen un badge especial. Siempre recomendamos inspeccionar el vehículo antes de comprar.',
  },
  {
    question: '¿Ofrecen financiamiento?',
    answer: 'Trabajamos con bancos asociados que ofrecen financiamiento. Consulta en cada listado.',
  },
];

export default function FAQPage() {
  return (
    <div className="container max-w-3xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold text-center mb-8">Preguntas Frecuentes</h1>

      <Accordion type="single" collapsible className="w-full">
        {faqs.map((faq, i) => (
          <AccordionItem key={i} value={`item-${i}`}>
            <AccordionTrigger className="text-left">
              {faq.question}
            </AccordionTrigger>
            <AccordionContent>
              {faq.answer}
            </AccordionContent>
          </AccordionItem>
        ))}
      </Accordion>

      <div className="mt-12 text-center">
        <p className="text-gray-600 mb-4">
          ¿No encontraste lo que buscabas?
        </p>
        <a href="/contact" className="text-primary-600 font-medium hover:underline">
          Contáctanos →
        </a>
      </div>
    </div>
  );
}
```

### Página de Términos

```typescript
// filepath: src/app/(public)/terms/page.tsx
export default function TermsPage() {
  return (
    <div className="container max-w-3xl mx-auto py-12 px-4">
      <h1 className="text-3xl font-bold mb-8">Términos y Condiciones</h1>

      <div className="prose prose-lg max-w-none">
        <p className="text-gray-600">Última actualización: Enero 2026</p>

        <h2>1. Aceptación de los Términos</h2>
        <p>Al acceder y utilizar OKLA, aceptas estos términos...</p>

        <h2>2. Uso del Servicio</h2>
        <p>OKLA es una plataforma que conecta compradores y vendedores...</p>

        <h2>3. Registro de Cuenta</h2>
        <p>Para utilizar ciertas funciones, debes crear una cuenta...</p>

        <h2>4. Publicación de Vehículos</h2>
        <p>Al publicar un vehículo, garantizas que la información es veraz...</p>

        <h2>5. Pagos y Facturación</h2>
        <p>Los pagos se procesan a través de pasarelas seguras...</p>

        <h2>6. Limitación de Responsabilidad</h2>
        <p>OKLA actúa como intermediario y no garantiza las transacciones...</p>

        <h2>7. Propiedad Intelectual</h2>
        <p>Todo el contenido de la plataforma es propiedad de OKLA...</p>

        <h2>8. Contacto</h2>
        <p>Para consultas: legal@okla.com.do</p>
      </div>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método | Endpoint       | Descripción                                 |
| ------ | -------------- | ------------------------------------------- |
| `POST` | `/api/contact` | Enviar formulario de contacto               |
| `GET`  | `/api/faq`     | Obtener FAQs (opcional, puede ser estático) |

---

## ✅ CHECKLIST

- [ ] Página About con stats y misión
- [ ] Página Contact con formulario
- [ ] Página FAQ con acordeón
- [ ] Página Terms con contenido legal
- [ ] Página Privacy con políticas
- [ ] Navegación en footer

---

_Última actualización: Enero 31, 2026_
