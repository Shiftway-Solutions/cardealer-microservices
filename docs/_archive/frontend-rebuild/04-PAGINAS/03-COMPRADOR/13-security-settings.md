# 🔐 Configuración de Seguridad

> **Tiempo estimado:** 20 minutos  
> **Página:** SecuritySettingsPage

---

## 📋 OBJETIVO

Configuración de seguridad de la cuenta:

- Cambio de contraseña
- Autenticación de dos factores (2FA)
- Sesiones activas
- Historial de actividad

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ SEGURIDAD                                                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ CONTRASEÑA                                                      │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Última actualización: hace 3 meses                          │ │
│ │                                       [Cambiar contraseña]  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ AUTENTICACIÓN DE DOS FACTORES                                   │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🔒 2FA está DESACTIVADO                                     │ │
│ │ Agrega una capa extra de seguridad a tu cuenta              │ │
│ │                                              [Activar 2FA]  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ SESIONES ACTIVAS                                                │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 💻 Chrome en MacOS          Santo Domingo    Ahora (actual) │ │
│ │ 📱 Safari en iPhone         Santiago         hace 2 días    │ │
│ │                                          [Cerrar todas]     │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ACTIVIDAD RECIENTE                                              │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ✅ Login exitoso           Chrome, MacOS    hace 1 hora     │ │
│ │ ✅ Login exitoso           iPhone           hace 2 días     │ │
│ │ ❌ Login fallido           Firefox          hace 1 semana   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(user)/settings/security/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { authService } from '@/services/api/authService';
import { formatRelativeTime } from '@/lib/format';
import {
  Lock, Shield, Smartphone, Monitor, LogOut,
  CheckCircle, XCircle, Key
} from 'lucide-react';
import { toast } from 'sonner';

export default function SecuritySettingsPage() {
  const [isPasswordOpen, setIsPasswordOpen] = useState(false);
  const [is2FAOpen, setIs2FAOpen] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const queryClient = useQueryClient();

  const { data: security } = useQuery({
    queryKey: ['user-security'],
    queryFn: () => authService.getSecurityInfo(),
  });

  const { data: sessions } = useQuery({
    queryKey: ['user-sessions'],
    queryFn: () => authService.getActiveSessions(),
  });

  const { data: activity } = useQuery({
    queryKey: ['user-activity'],
    queryFn: () => authService.getLoginActivity(),
  });

  const changePasswordMutation = useMutation({
    mutationFn: (data: typeof passwordForm) => authService.changePassword(data),
    onSuccess: () => {
      setIsPasswordOpen(false);
      setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      toast.success('Contraseña actualizada');
    },
    onError: (error: any) => {
      toast.error(error.message || 'Error al cambiar contraseña');
    },
  });

  const toggle2FAMutation = useMutation({
    mutationFn: (enable: boolean) =>
      enable ? authService.enable2FA() : authService.disable2FA(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-security'] });
      setIs2FAOpen(false);
      toast.success(security?.twoFactorEnabled ? '2FA desactivado' : '2FA activado');
    },
  });

  const revokeSessionMutation = useMutation({
    mutationFn: (sessionId: string) => authService.revokeSession(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-sessions'] });
      toast.success('Sesión cerrada');
    },
  });

  const revokeAllSessionsMutation = useMutation({
    mutationFn: () => authService.revokeAllSessions(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-sessions'] });
      toast.success('Todas las sesiones cerradas');
    },
  });

  return (
    <div className="container max-w-2xl mx-auto py-8 px-4">
      <h1 className="text-2xl font-bold mb-6">Seguridad</h1>

      {/* Password */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Lock className="w-5 h-5" />
            Contraseña
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">
                Última actualización: {formatRelativeTime(security?.passwordUpdatedAt)}
              </p>
            </div>
            <Button variant="outline" onClick={() => setIsPasswordOpen(true)}>
              Cambiar contraseña
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* 2FA */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5" />
            Autenticación de Dos Factores
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <Badge variant={security?.twoFactorEnabled ? 'success' : 'secondary'}>
                {security?.twoFactorEnabled ? '🔒 Activado' : '🔓 Desactivado'}
              </Badge>
              <p className="text-sm text-gray-600 mt-1">
                Agrega una capa extra de seguridad a tu cuenta
              </p>
            </div>
            <Button
              variant={security?.twoFactorEnabled ? 'destructive' : 'default'}
              onClick={() => setIs2FAOpen(true)}
            >
              {security?.twoFactorEnabled ? 'Desactivar' : 'Activar'} 2FA
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Active Sessions */}
      <Card className="mb-6">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Sesiones Activas</CardTitle>
            <Button variant="ghost" size="sm" onClick={() => revokeAllSessionsMutation.mutate()}>
              <LogOut className="w-4 h-4 mr-1" />
              Cerrar todas
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {sessions?.map((session: any) => (
              <div key={session.id} className="flex items-center justify-between py-2 border-b last:border-0">
                <div className="flex items-center gap-3">
                  {session.deviceType === 'mobile' ? (
                    <Smartphone className="w-5 h-5 text-gray-400" />
                  ) : (
                    <Monitor className="w-5 h-5 text-gray-400" />
                  )}
                  <div>
                    <div className="font-medium">
                      {session.browser} en {session.os}
                      {session.isCurrent && (
                        <Badge variant="outline" className="ml-2">Actual</Badge>
                      )}
                    </div>
                    <div className="text-sm text-gray-600">
                      {session.location} • {formatRelativeTime(session.lastActive)}
                    </div>
                  </div>
                </div>
                {!session.isCurrent && (
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => revokeSessionMutation.mutate(session.id)}
                  >
                    <XCircle className="w-4 h-4" />
                  </Button>
                )}
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Login Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Actividad Reciente</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {activity?.map((log: any, i: number) => (
              <div key={i} className="flex items-center gap-3 py-2">
                {log.success ? (
                  <CheckCircle className="w-5 h-5 text-green-500" />
                ) : (
                  <XCircle className="w-5 h-5 text-red-500" />
                )}
                <div className="flex-1">
                  <div className="font-medium">
                    {log.success ? 'Login exitoso' : 'Login fallido'}
                  </div>
                  <div className="text-sm text-gray-600">
                    {log.browser}, {log.os}
                  </div>
                </div>
                <span className="text-sm text-gray-500">
                  {formatRelativeTime(log.timestamp)}
                </span>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Change Password Dialog */}
      <Dialog open={isPasswordOpen} onOpenChange={setIsPasswordOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Cambiar Contraseña</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <Input
              type="password"
              placeholder="Contraseña actual"
              value={passwordForm.currentPassword}
              onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
            />
            <Input
              type="password"
              placeholder="Nueva contraseña"
              value={passwordForm.newPassword}
              onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
            />
            <Input
              type="password"
              placeholder="Confirmar nueva contraseña"
              value={passwordForm.confirmPassword}
              onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
            />
            <Button
              className="w-full"
              onClick={() => changePasswordMutation.mutate(passwordForm)}
              disabled={changePasswordMutation.isPending}
            >
              Cambiar Contraseña
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* 2FA Dialog */}
      <Dialog open={is2FAOpen} onOpenChange={setIs2FAOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {security?.twoFactorEnabled ? 'Desactivar' : 'Activar'} 2FA
            </DialogTitle>
          </DialogHeader>
          <div className="text-center py-4">
            <Shield className="w-16 h-16 mx-auto text-primary-600 mb-4" />
            <p className="text-gray-600 mb-6">
              {security?.twoFactorEnabled
                ? '¿Estás seguro de desactivar la autenticación de dos factores?'
                : 'Se enviará un código a tu email cada vez que inicies sesión'
              }
            </p>
            <Button
              className="w-full"
              variant={security?.twoFactorEnabled ? 'destructive' : 'default'}
              onClick={() => toggle2FAMutation.mutate(!security?.twoFactorEnabled)}
            >
              {security?.twoFactorEnabled ? 'Desactivar' : 'Activar'} 2FA
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                    | Descripción        |
| -------- | --------------------------- | ------------------ |
| `GET`    | `/api/auth/security`        | Info de seguridad  |
| `POST`   | `/api/auth/change-password` | Cambiar contraseña |
| `POST`   | `/api/auth/2fa/enable`      | Activar 2FA        |
| `POST`   | `/api/auth/2fa/disable`     | Desactivar 2FA     |
| `GET`    | `/api/auth/sessions`        | Sesiones activas   |
| `DELETE` | `/api/auth/sessions/{id}`   | Cerrar sesión      |
| `DELETE` | `/api/auth/sessions`        | Cerrar todas       |
| `GET`    | `/api/auth/activity`        | Historial de login |

---

## ✅ CHECKLIST

- [ ] Cambio de contraseña con validación
- [ ] Toggle de 2FA con confirmación
- [ ] Lista de sesiones activas
- [ ] Cerrar sesiones individuales/todas
- [ ] Historial de actividad de login

---

_Última actualización: Enero 31, 2026_
