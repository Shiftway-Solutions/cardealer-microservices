# 👥 Empleados del Dealer

> **Tiempo estimado:** 30 minutos  
> **Páginas:** DealerEmployeesPage, InviteEmployeePage

---

## 📋 OBJETIVO

Gestión de empleados del dealer:

- Lista de empleados con roles
- Invitar nuevos empleados
- Asignar permisos por rol

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ EQUIPO                                        [+ Invitar]       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 👤 Juan Pérez             Admin           ✓ Activo          │ │
│ │    juan@dealer.com        Desde Ene 2024  [Editar] [...]    │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 👤 María García           Ventas          ✓ Activo          │ │
│ │    maria@dealer.com       Desde Mar 2024  [Editar] [...]    │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 👤 Carlos López           Ventas          ⏳ Pendiente       │ │
│ │    carlos@dealer.com      Invitado        [Reenviar]        │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

### Lista de Empleados

```typescript
// filepath: src/app/(dealer)/dealer/employees/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { dealerService, type Employee } from '@/services/api/dealerService';
import { useToast } from '@/hooks/use-toast';
import { MoreHorizontal, UserPlus, Mail, Trash2 } from 'lucide-react';

const roles = [
  { value: 'admin', label: 'Administrador', description: 'Acceso completo' },
  { value: 'manager', label: 'Gerente', description: 'Gestión de inventario y ventas' },
  { value: 'sales', label: 'Ventas', description: 'Ver y responder consultas' },
  { value: 'viewer', label: 'Visualizador', description: 'Solo lectura' },
];

export default function DealerEmployeesPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('sales');

  const { data: employees, isLoading } = useQuery({
    queryKey: ['dealer-employees'],
    queryFn: () => dealerService.getEmployees(),
  });

  const inviteMutation = useMutation({
    mutationFn: (data: { email: string; role: string }) =>
      dealerService.inviteEmployee(data.email, data.role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-employees'] });
      setInviteOpen(false);
      setInviteEmail('');
      toast({ title: 'Invitación enviada' });
    },
    onError: () => {
      toast({ title: 'Error al enviar invitación', variant: 'destructive' });
    },
  });

  const removeMutation = useMutation({
    mutationFn: (employeeId: string) => dealerService.removeEmployee(employeeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-employees'] });
      toast({ title: 'Empleado eliminado' });
    },
  });

  const resendMutation = useMutation({
    mutationFn: (employeeId: string) => dealerService.resendInvite(employeeId),
    onSuccess: () => {
      toast({ title: 'Invitación reenviada' });
    },
  });

  return (
    <div className="container max-w-4xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Equipo</h1>
          <p className="text-gray-600">Gestiona los empleados de tu dealer</p>
        </div>

        <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
          <DialogTrigger asChild>
            <Button>
              <UserPlus className="w-4 h-4 mr-2" />
              Invitar Empleado
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Invitar Empleado</DialogTitle>
            </DialogHeader>
            <div className="space-y-4 pt-4">
              <div>
                <Label>Email</Label>
                <Input
                  type="email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder="empleado@email.com"
                />
              </div>
              <div>
                <Label>Rol</Label>
                <Select value={inviteRole} onValueChange={setInviteRole}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {roles.map((role) => (
                      <SelectItem key={role.value} value={role.value}>
                        <div>
                          <div className="font-medium">{role.label}</div>
                          <div className="text-xs text-gray-500">{role.description}</div>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button
                className="w-full"
                onClick={() => inviteMutation.mutate({ email: inviteEmail, role: inviteRole })}
                disabled={!inviteEmail || inviteMutation.isPending}
              >
                Enviar Invitación
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="divide-y">
            {employees?.map((employee: Employee) => (
              <div key={employee.id} className="p-4 flex items-center gap-4">
                <Avatar>
                  <AvatarImage src={employee.avatar} />
                  <AvatarFallback>{employee.name?.charAt(0) || employee.email.charAt(0)}</AvatarFallback>
                </Avatar>

                <div className="flex-grow min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="font-medium">
                      {employee.name || employee.email}
                    </span>
                    <Badge variant={employee.status === 'active' ? 'default' : 'secondary'}>
                      {employee.status === 'active' ? 'Activo' : 'Pendiente'}
                    </Badge>
                  </div>
                  <p className="text-sm text-gray-600">{employee.email}</p>
                </div>

                <div className="text-right">
                  <Badge variant="outline">
                    {roles.find(r => r.value === employee.role)?.label}
                  </Badge>
                </div>

                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon">
                      <MoreHorizontal className="w-4 h-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {employee.status === 'pending' && (
                      <DropdownMenuItem onClick={() => resendMutation.mutate(employee.id)}>
                        <Mail className="w-4 h-4 mr-2" />
                        Reenviar invitación
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuItem
                      onClick={() => removeMutation.mutate(employee.id)}
                      className="text-red-600"
                    >
                      <Trash2 className="w-4 h-4 mr-2" />
                      Eliminar
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Roles Reference */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="text-lg">Permisos por Rol</CardTitle>
        </CardHeader>
        <CardContent>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left py-2">Permiso</th>
                <th className="text-center">Admin</th>
                <th className="text-center">Gerente</th>
                <th className="text-center">Ventas</th>
                <th className="text-center">Viewer</th>
              </tr>
            </thead>
            <tbody>
              {[
                ['Gestionar equipo', true, false, false, false],
                ['Facturación', true, false, false, false],
                ['Crear/editar vehículos', true, true, true, false],
                ['Eliminar vehículos', true, true, false, false],
                ['Responder consultas', true, true, true, false],
                ['Ver analytics', true, true, true, true],
              ].map(([perm, ...values]) => (
                <tr key={perm as string} className="border-b">
                  <td className="py-2">{perm}</td>
                  {(values as boolean[]).map((v, i) => (
                    <td key={i} className="text-center">
                      {v ? '✓' : '—'}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                             | Descripción         |
| -------- | ------------------------------------ | ------------------- |
| `GET`    | `/api/dealers/employees`             | Lista de empleados  |
| `POST`   | `/api/dealers/employees/invite`      | Invitar empleado    |
| `DELETE` | `/api/dealers/employees/{id}`        | Eliminar empleado   |
| `POST`   | `/api/dealers/employees/{id}/resend` | Reenviar invitación |

---

## ✅ CHECKLIST

- [ ] Lista de empleados con estado
- [ ] Modal de invitación
- [ ] Selector de roles
- [ ] Reenvío de invitaciones
- [ ] Tabla de permisos

---

_Última actualización: Enero 30, 2026_
