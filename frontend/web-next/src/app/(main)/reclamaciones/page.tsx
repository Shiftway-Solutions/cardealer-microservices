'use client';

import * as React from 'react';
import { useState } from 'react';
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
import {
  AlertTriangle,
  CheckCircle2,
  Clock,
  ExternalLink,
  FileText,
  Phone,
  Shield,
  Upload,
} from 'lucide-react';

export default function ReclamacionesPage() {
  const [submitted, setSubmitted] = useState(false);
  const [ticketNumber, setTicketNumber] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [formData, setFormData] = useState({
    tipoReclamacion: '',
    descripcion: '',
    nombreCompleto: '',
    cedula: '',
    email: '',
    telefono: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    // Simulate ticket generation
    await new Promise(resolve => setTimeout(resolve, 1000));
    const ticket = `OKLA-${Date.now().toString(36).toUpperCase()}-${Math.random().toString(36).substring(2, 6).toUpperCase()}`;
    setTicketNumber(ticket);
    setSubmitted(true);
    setIsLoading(false);
  };

  if (submitted) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-12">
        <Card className="border-primary/20">
          <CardContent className="flex flex-col items-center gap-4 p-8 text-center">
            <div className="rounded-full bg-green-100 p-3 dark:bg-green-900/30">
              <CheckCircle2 className="h-8 w-8 text-green-600 dark:text-green-400" />
            </div>
            <h2 className="text-foreground text-xl font-bold">Reclamación Enviada</h2>
            <p className="text-muted-foreground text-sm">
              Tu reclamación ha sido registrada exitosamente. Guarda tu número de ticket para
              seguimiento.
            </p>
            <div className="border-border bg-muted/50 rounded-lg border px-6 py-3">
              <p className="text-muted-foreground text-xs">Número de ticket</p>
              <p className="text-foreground font-mono text-lg font-bold">{ticketNumber}</p>
            </div>
            <div className="text-muted-foreground mt-2 space-y-2 text-left text-sm">
              <p>
                <Clock className="mr-1.5 inline h-4 w-4" />
                Tiempo de respuesta estimado: <strong>15 días hábiles</strong>
              </p>
              <p>
                Recibirás una confirmación en tu correo electrónico con los detalles de tu
                reclamación.
              </p>
            </div>
            <Button
              onClick={() => {
                setSubmitted(false);
                setFormData({
                  tipoReclamacion: '',
                  descripcion: '',
                  nombreCompleto: '',
                  cedula: '',
                  email: '',
                  telefono: '',
                });
              }}
              variant="outline"
              className="mt-4"
            >
              Enviar otra reclamación
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-12">
      <div className="mb-8 space-y-2">
        <h1 className="text-foreground text-3xl font-bold">Sistema de Reclamaciones</h1>
        <p className="text-muted-foreground">
          Conforme a la <strong>Ley 358-05</strong> (Arts. 80-81), tienes derecho a presentar
          reclamaciones sobre productos y servicios. OKLA se compromete a responder en un plazo
          máximo de 15 días hábiles.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Form */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <FileText className="text-primary h-5 w-5" />
                Formulario de Reclamación
              </CardTitle>
              <CardDescription>
                Complete todos los campos requeridos para registrar su reclamación.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-5">
                {/* Tipo de reclamación */}
                <div className="space-y-2">
                  <Label htmlFor="tipoReclamacion">Tipo de reclamación *</Label>
                  <Select
                    value={formData.tipoReclamacion}
                    onValueChange={value =>
                      setFormData(prev => ({ ...prev, tipoReclamacion: value }))
                    }
                    required
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccione el tipo de reclamación" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="producto-defectuoso">
                        Vehículo con defectos no informados
                      </SelectItem>
                      <SelectItem value="publicidad-enganosa">
                        Publicidad engañosa en listado
                      </SelectItem>
                      <SelectItem value="incumplimiento-contrato">
                        Incumplimiento de acuerdo de venta
                      </SelectItem>
                      <SelectItem value="cobro-indebido">
                        Cobro indebido de servicios OKLA
                      </SelectItem>
                      <SelectItem value="problema-reembolso">Problema con reembolso</SelectItem>
                      <SelectItem value="fraude">Sospecha de fraude</SelectItem>
                      <SelectItem value="atencion-cliente">Mala atención al cliente</SelectItem>
                      <SelectItem value="otro">Otro</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* Descripción */}
                <div className="space-y-2">
                  <Label htmlFor="descripcion">Descripción detallada *</Label>
                  <Textarea
                    id="descripcion"
                    placeholder="Describa su reclamación con el mayor detalle posible, incluyendo fechas, montos y cualquier información relevante..."
                    value={formData.descripcion}
                    onChange={e => setFormData(prev => ({ ...prev, descripcion: e.target.value }))}
                    required
                    rows={5}
                    className="resize-y"
                  />
                </div>

                {/* Datos de contacto */}
                <div className="border-border bg-muted/30 space-y-4 rounded-lg border p-4">
                  <h3 className="text-foreground text-sm font-semibold">Datos del reclamante</h3>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="nombreCompleto">Nombre completo *</Label>
                      <Input
                        id="nombreCompleto"
                        placeholder="Juan Pérez"
                        value={formData.nombreCompleto}
                        onChange={e =>
                          setFormData(prev => ({ ...prev, nombreCompleto: e.target.value }))
                        }
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="cedula">Cédula *</Label>
                      <Input
                        id="cedula"
                        placeholder="001-0000000-0"
                        value={formData.cedula}
                        onChange={e => setFormData(prev => ({ ...prev, cedula: e.target.value }))}
                        required
                      />
                    </div>
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="email">Correo electrónico *</Label>
                      <Input
                        id="email"
                        type="email"
                        placeholder="tu@email.com"
                        value={formData.email}
                        onChange={e => setFormData(prev => ({ ...prev, email: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="telefono">Teléfono *</Label>
                      <Input
                        id="telefono"
                        type="tel"
                        placeholder="809-555-0123"
                        value={formData.telefono}
                        onChange={e => setFormData(prev => ({ ...prev, telefono: e.target.value }))}
                        required
                      />
                    </div>
                  </div>
                </div>

                {/* Adjuntos */}
                <div className="space-y-2">
                  <Label>Documentos de soporte (opcional)</Label>
                  <div className="border-border bg-muted/20 flex items-center justify-center rounded-lg border-2 border-dashed p-6 text-center">
                    <div className="space-y-1">
                      <Upload className="text-muted-foreground mx-auto h-8 w-8" />
                      <p className="text-muted-foreground text-sm">
                        Arrastra archivos aquí o haz clic para seleccionar
                      </p>
                      <p className="text-muted-foreground text-xs">
                        PDF, JPG, PNG (máx. 5MB por archivo)
                      </p>
                    </div>
                  </div>
                </div>

                <Button
                  type="submit"
                  className="w-full"
                  disabled={
                    isLoading ||
                    !formData.tipoReclamacion ||
                    !formData.descripcion ||
                    !formData.nombreCompleto ||
                    !formData.cedula ||
                    !formData.email ||
                    !formData.telefono
                  }
                >
                  {isLoading ? 'Enviando...' : 'Enviar Reclamación'}
                </Button>
              </form>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar info */}
        <div className="space-y-4">
          {/* Timeframe */}
          <Card className="border-amber-200 bg-amber-50/50 dark:border-amber-900/50 dark:bg-amber-950/20">
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <Clock className="mt-0.5 h-5 w-5 text-amber-600 dark:text-amber-400" />
                <div>
                  <h3 className="text-foreground text-sm font-semibold">Plazo de respuesta</h3>
                  <p className="text-muted-foreground mt-1 text-xs">
                    OKLA responderá su reclamación en un plazo máximo de{' '}
                    <strong>15 días hábiles</strong> a partir de la fecha de registro.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Escalation */}
          <Card className="border-blue-200 bg-blue-50/50 dark:border-blue-900/50 dark:bg-blue-950/20">
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <Shield className="mt-0.5 h-5 w-5 text-blue-600 dark:text-blue-400" />
                <div>
                  <h3 className="text-foreground text-sm font-semibold">
                    ¿No se resolvió tu caso?
                  </h3>
                  <p className="text-muted-foreground mt-1 text-xs">
                    Si tu reclamación no es resuelta satisfactoriamente, puedes escalar tu caso ante{' '}
                    <strong>ProConsumidor</strong>, la entidad gubernamental encargada de proteger
                    los derechos del consumidor en República Dominicana.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* ProConsumidor */}
          <Card>
            <CardContent className="p-4">
              <div className="flex items-start gap-3">
                <AlertTriangle className="text-primary mt-0.5 h-5 w-5" />
                <div className="space-y-2">
                  <h3 className="text-foreground text-sm font-semibold">ProConsumidor</h3>
                  <p className="text-muted-foreground text-xs">
                    Instituto Nacional de Protección de los Derechos del Consumidor
                  </p>
                  <div className="text-muted-foreground space-y-1.5 text-xs">
                    <p className="flex items-center gap-1.5">
                      <Phone className="h-3.5 w-3.5" />
                      Tel: 809-567-7755
                    </p>
                    <a
                      href="https://www.proconsumidor.gob.do"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-primary flex items-center gap-1.5 hover:underline"
                    >
                      <ExternalLink className="h-3.5 w-3.5" />
                      www.proconsumidor.gob.do
                    </a>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Legal reference */}
          <Card>
            <CardContent className="p-4">
              <h3 className="text-foreground text-sm font-semibold">Base Legal</h3>
              <p className="text-muted-foreground mt-1 text-xs">
                Este sistema de reclamaciones cumple con los artículos 80 y 81 de la{' '}
                <strong>Ley 358-05</strong> General de Protección de los Derechos del Consumidor o
                Usuario de la República Dominicana.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
