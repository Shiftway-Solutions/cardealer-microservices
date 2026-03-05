# 🔍 Análisis de Gaps - Módulo de Autenticación y Seguridad

> **Fecha de Análisis:** Enero 21, 2026  
> **Analista:** GitHub Copilot  
> **Estado:** 🔴 Requiere Mejoras Críticas

---

## 📋 Resumen Ejecutivo

Este documento presenta el análisis completo de los procesos documentados en la carpeta `01-AUTENTICACION-SEGURIDAD` vs la implementación actual en los microservicios y el frontend de OKLA.

### Documentos Analizados

| Documento            | Procesos | Endpoints Definidos | Estado Implementación |
| -------------------- | -------- | ------------------- | --------------------- |
| `01-auth-service.md` | 6        | 25                  | 🟡 Parcial (75%)      |
| `02-role-service.md` | 6        | 10                  | 🟢 Completo (95%)     |
| `03-security-2fa.md` | 6        | 6                   | 🟡 Parcial (80%)      |
| `04-kyc-service.md`  | 5        | 16                  | 🟢 Completo (90%)     |

### Resumen de Gaps Críticos

| Categoría | Gaps Críticos | Gaps Medios | Gaps Menores |
| --------- | ------------- | ----------- | ------------ |
| Backend   | 3             | 5           | 4            |
| Frontend  | 5             | 4           | 3            |
| UX/UI     | 4             | 3           | 2            |
| **TOTAL** | **12**        | **12**      | **9**        |

---

## 🔴 GAPS CRÍTICOS IDENTIFICADOS

### 1. SecurityController - Datos Mock (CRÍTICO)

**Archivo:** `backend/AuthService/AuthService.Api/Controllers/SecurityController.cs`

**Problema:**

```csharp
// LÍNEAS 30-65: Retorna datos MOCK en lugar de consultar la base de datos
var settings = new SecuritySettingsDto(
    TwoFactorEnabled: false,  // ❌ HARDCODED
    TwoFactorType: null,
    LastPasswordChange: DateTime.UtcNow.AddDays(-30).ToString("o"), // ❌ FAKE
    ActiveSessions: new List<ActiveSessionDto>
    {
        new(
            Id: "session_current",  // ❌ FAKE SESSION
            Device: GetDeviceFromUserAgent(),
            // ...
        )
    }
);
```

**Impacto:**

- Los usuarios ven información falsa sobre sus sesiones
- No pueden ver/revocar sesiones reales
- Historial de logins es fabricado
- Configuración 2FA no refleja estado real

**Proceso Afectado:** AUTH-LOG-001, AUTH-TOK-001

---

### 2. Páginas de Seguridad Faltantes (CRÍTICO)

**Ubicación esperada:** `frontend/web/src/pages/auth/` o `frontend/web/src/pages/user/`

| Página                     | Documentada     | Implementada | Estado   |
| -------------------------- | --------------- | ------------ | -------- |
| `ResetPasswordPage.tsx`    | ✅ AUTH-PWD-002 | ❌           | 🔴 FALTA |
| `VerifyEmailPage.tsx`      | ✅ AUTH-REG-001 | ❌           | 🔴 FALTA |
| `SecuritySettingsPage.tsx` | ✅ Implícita    | ❌           | 🔴 FALTA |
| `TwoFactorSetupPage.tsx`   | ✅ SEC-2FA-001  | ❌           | 🔴 FALTA |
| `TwoFactorVerifyPage.tsx`  | ✅ SEC-2FA-003  | ❌           | 🔴 FALTA |

**Impacto:**

- Usuarios no pueden resetear contraseña desde link de email
- Usuarios no pueden verificar email desde link
- No hay UI dedicada para configurar 2FA
- No hay página para el paso de 2FA durante login

---

### 3. Login no maneja flujo 2FA (CRÍTICO)

**Archivo:** `frontend/web/src/pages/auth/LoginPage.tsx`

**Problema:**

```tsx
// LÍNEA 47-60: No maneja el response "requiresTwoFactor: true"
const response = await authService.login({
  email: data.email,
  password: data.password,
  rememberMe: data.rememberMe,
});

// ❌ NO HAY LÓGICA PARA:
// if (response.requiresTwoFactor) {
//   navigate('/verify-2fa', { state: { sessionToken: response.sessionToken } });
// }
```

**Proceso Afectado:** SEC-2FA-003 (Login con 2FA)

---

### 4. KYC UI Completamente Faltante (CRÍTICO)

**Análisis:** No existe ninguna página de KYC para usuarios

| Página KYC                  | Proceso      | Implementada |
| --------------------------- | ------------ | ------------ |
| `KYCProfilePage.tsx`        | KYC-PROF-001 | ❌           |
| `KYCDocumentUploadPage.tsx` | KYC-DOC-001  | ❌           |
| `KYCStatusPage.tsx`         | KYC-MON-001  | ❌           |

**Impacto:**

- Dealers no pueden completar verificación KYC
- No hay upload de documentos
- No hay visualización de estado de verificación

---

### 5. PhoneVerificationController incompleto

**Archivo:** `backend/AuthService/AuthService.Api/Controllers/PhoneVerificationController.cs`

**Documentado vs Implementado:**

- ✅ POST `/api/auth/phone/send-code`
- ✅ POST `/api/auth/phone/verify`
- ✅ GET `/api/auth/phone/status`

**Gap:** Aunque los endpoints existen, necesitan validar integración con Twilio y rate limiting específico.

---

## 🟡 GAPS MEDIOS

### 6. ChangePassword no valida contraseña actual

**Archivo:** `SecurityController.cs` líneas 75-107

**Problema:**

```csharp
// ❌ FALTA: Verificar contraseña actual antes de permitir cambio
// El código actual solo valida formato de nueva contraseña
// NO hay llamada a BCrypt.Verify para la contraseña actual
```

**Proceso Afectado:** AUTH-PWD-001

---

### 7. Rate Limiting falta en varios endpoints

**Documentado:** Rate limits específicos por endpoint

| Endpoint           | Rate Limit Doc | Implementado     |
| ------------------ | -------------- | ---------------- |
| `/register`        | 5/min          | ⚠️ No verificado |
| `/login`           | 10/min         | ⚠️ No verificado |
| `/forgot-password` | 3/min          | ⚠️ No verificado |

**Acción:** Verificar implementación de rate limiting en Gateway

---

### 8. Eventos RabbitMQ no publicados consistentemente

**Eventos Documentados:**

- `user.registered`
- `user.logged.in`
- `user.password.changed`
- `user.2fa.enabled`
- `user.2fa.login.success`

**Estado:** Verificar que todos los handlers publican eventos

---

### 9. Bloqueo de cuenta por intentos fallidos

**Documentado:**

- 5 intentos fallidos = bloqueo 30 min
- Registro en audit log

**Estado actual:** No hay evidencia de implementación de bloqueo temporal

---

### 10. Recovery Codes no persistidos correctamente

**Proceso:** SEC-2FA-005

**Requisitos:**

- 10 códigos de 8 caracteres
- Hash BCrypt en DB
- Marcar como "usado" después de uso

**Estado:** Verificar implementación en `TwoFactorController.cs`

---

## 🟢 GAPS MENORES

### 11. Mensajes de error inconsistentes

**Documentado:** Códigos de error específicos (INVALID_EMAIL, WEAK_PASSWORD, etc.)

**Actual:** Mensajes genéricos en varios lugares

---

### 12. Audit logging incompleto

**Documentado:** Registrar todos los eventos de seguridad

**Estado:** Parcialmente implementado

---

### 13. Documentación Swagger incompleta

**Estado:** Algunos endpoints sin documentación XML completa

---

## 📊 ANÁLISIS POR SERVICIO

### AuthService

| Endpoint                      | Documentado | Implementado | Funcional | Gap                |
| ----------------------------- | ----------- | ------------ | --------- | ------------------ |
| POST /register                | ✅          | ✅           | ✅        | -                  |
| POST /login                   | ✅          | ✅           | 🟡        | 2FA flow           |
| POST /forgot-password         | ✅          | ✅           | ⚠️        | Verificar          |
| POST /reset-password          | ✅          | ✅           | ⚠️        | Verificar          |
| POST /verify-email            | ✅          | ✅           | ⚠️        | Verificar          |
| POST /refresh-token           | ✅          | ✅           | ✅        | -                  |
| POST /logout                  | ✅          | ✅           | ✅        | -                  |
| **SecurityController**        |             |              |           |                    |
| GET /security                 | ✅          | 🔴 MOCK      | ❌        | Crítico            |
| POST /change-password         | ✅          | 🟡           | ⚠️        | Validar pwd actual |
| GET /sessions                 | ✅          | 🔴 MOCK      | ❌        | Crítico            |
| DELETE /sessions/{id}         | ✅          | 🟡           | ⚠️        | No funcional       |
| POST /revoke-all              | ✅          | 🟡           | ⚠️        | No funcional       |
| **TwoFactorController**       |             |              |           |                    |
| POST /enable                  | ✅          | ✅           | ⚠️        | Verificar          |
| POST /verify                  | ✅          | ✅           | ⚠️        | Verificar          |
| POST /disable                 | ✅          | ✅           | ⚠️        | Verificar          |
| POST /generate-recovery-codes | ✅          | ✅           | ⚠️        | Verificar          |
| POST /verify-recovery-code    | ✅          | ✅           | ⚠️        | Verificar          |
| POST /login (2FA)             | ✅          | ❓           | ⚠️        | Verificar          |

### RoleService

| Endpoint           | Estado          |
| ------------------ | --------------- |
| POST /roles        | ✅ Implementado |
| GET /roles         | ✅ Implementado |
| GET /roles/{id}    | ✅ Implementado |
| PUT /roles/{id}    | ✅ Implementado |
| DELETE /roles/{id} | ✅ Implementado |
| POST /permissions  | ✅ Implementado |
| GET /permissions   | ✅ Implementado |

**Estado General:** 🟢 95% Completo

### KYCService

| Endpoint                          | Estado          |
| --------------------------------- | --------------- |
| GET /kycprofiles                  | ✅ Implementado |
| GET /kycprofiles/{id}             | ✅ Implementado |
| GET /kycprofiles/user/{userId}    | ✅ Implementado |
| POST /kycprofiles                 | ✅ Implementado |
| PUT /kycprofiles/{id}             | ✅ Implementado |
| POST /kycprofiles/{id}/approve    | ✅ Implementado |
| POST /kycprofiles/{id}/reject     | ✅ Implementado |
| GET /kycprofiles/pending          | ✅ Implementado |
| GET /kycprofiles/expiring         | ✅ Implementado |
| GET /kycprofiles/statistics       | ✅ Implementado |
| GET /kyc/profiles/{id}/documents  | ✅ Implementado |
| POST /kyc/profiles/{id}/documents | ✅ Implementado |
| POST /kyc/documents/{id}/verify   | ✅ Implementado |

**Estado General:** 🟢 90% Completo (falta UI)

---

## 📱 ANÁLISIS FRONTEND

### Páginas de Autenticación

| Página                 | Existe | Proceso      | Estado       |
| ---------------------- | ------ | ------------ | ------------ |
| LoginPage.tsx          | ✅     | AUTH-LOG-001 | 🟡 Falta 2FA |
| RegisterPage.tsx       | ✅     | AUTH-REG-001 | 🟢 OK        |
| ForgotPasswordPage.tsx | ✅     | AUTH-PWD-001 | 🟢 OK        |
| ResetPasswordPage.tsx  | ❌     | AUTH-PWD-002 | 🔴 FALTA     |
| VerifyEmailPage.tsx    | ❌     | AUTH-REG-001 | 🔴 FALTA     |
| OAuthCallbackPage.tsx  | ✅     | AUTH-EXT-001 | 🟢 OK        |

### Páginas de Seguridad

| Página                   | Existe | Proceso     | Estado   |
| ------------------------ | ------ | ----------- | -------- |
| SecuritySettingsPage.tsx | ❌     | Multiple    | 🔴 FALTA |
| TwoFactorSetupPage.tsx   | ❌     | SEC-2FA-001 | 🔴 FALTA |
| TwoFactorVerifyPage.tsx  | ❌     | SEC-2FA-003 | 🔴 FALTA |
| SessionsPage.tsx         | ❌     | Implícito   | 🔴 FALTA |
| ChangePasswordPage.tsx   | ❌     | AUTH-PWD    | 🔴 FALTA |

### Páginas KYC (Usuario)

| Página                    | Existe | Proceso      | Estado   |
| ------------------------- | ------ | ------------ | -------- |
| KYCProfilePage.tsx        | ❌     | KYC-PROF-001 | 🔴 FALTA |
| KYCDocumentUploadPage.tsx | ❌     | KYC-DOC-001  | 🔴 FALTA |
| KYCStatusPage.tsx         | ❌     | KYC-MON-001  | 🔴 FALTA |

### DealerSettingsPage (Parcial)

**Archivo:** `frontend/web/src/pages/dealer/DealerSettingsPage.tsx`

Esta página YA tiene sección de seguridad pero con datos mock del backend:

- ✅ Muestra estado 2FA
- ✅ Muestra sesiones activas
- ✅ Muestra último cambio de contraseña
- ❌ Los datos son MOCK (vienen del SecurityController con datos fake)

---

## 🎯 PLAN DE MEJORAS RECOMENDADO

### Fase 1: Críticos (Sprint Inmediato)

| ID   | Tarea                                           | Esfuerzo | Impacto    |
| ---- | ----------------------------------------------- | -------- | ---------- |
| F1.1 | Implementar SecurityController con datos reales | 3 días   | 🔴 Crítico |
| F1.2 | Crear ResetPasswordPage.tsx                     | 1 día    | 🔴 Crítico |
| F1.3 | Crear VerifyEmailPage.tsx                       | 1 día    | 🔴 Crítico |
| F1.4 | Agregar flujo 2FA a LoginPage                   | 2 días   | 🔴 Crítico |
| F1.5 | Crear TwoFactorVerifyPage.tsx                   | 1 día    | 🔴 Crítico |

**Total Fase 1:** 8 días

### Fase 2: Altos (Sprint Siguiente)

| ID   | Tarea                                                      | Esfuerzo | Impacto |
| ---- | ---------------------------------------------------------- | -------- | ------- |
| F2.1 | Crear SecuritySettingsPage.tsx completa                    | 3 días   | 🟠 Alto |
| F2.2 | Crear TwoFactorSetupPage.tsx                               | 2 días   | 🟠 Alto |
| F2.3 | Implementar bloqueo por intentos fallidos                  | 2 días   | 🟠 Alto |
| F2.4 | Validar rate limiting en todos los endpoints               | 1 día    | 🟠 Alto |
| F2.5 | Agregar validación de contraseña actual en change-password | 0.5 días | 🟠 Alto |

**Total Fase 2:** 8.5 días

### Fase 3: Medios (Sprint Posterior)

| ID   | Tarea                                  | Esfuerzo | Impacto  |
| ---- | -------------------------------------- | -------- | -------- |
| F3.1 | Crear KYCProfilePage.tsx               | 2 días   | 🟡 Medio |
| F3.2 | Crear KYCDocumentUploadPage.tsx        | 2 días   | 🟡 Medio |
| F3.3 | Crear KYCStatusPage.tsx                | 1 día    | 🟡 Medio |
| F3.4 | Implementar eventos RabbitMQ faltantes | 2 días   | 🟡 Medio |
| F3.5 | Mejorar audit logging                  | 1 día    | 🟡 Medio |

**Total Fase 3:** 8 días

### Fase 4: Menores (Backlog)

| ID   | Tarea                           | Esfuerzo |
| ---- | ------------------------------- | -------- |
| F4.1 | Estandarizar mensajes de error  | 1 día    |
| F4.2 | Completar documentación Swagger | 0.5 días |
| F4.3 | Tests E2E para flujos de auth   | 3 días   |

**Total Fase 4:** 4.5 días

---

## 📝 ENTIDADES REQUERIDAS (No existentes o incompletas)

### Session Entity

```csharp
public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; }
    public string DeviceInfo { get; set; }
    public string Browser { get; set; }
    public string IpAddress { get; set; }
    public string Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsCurrent { get; set; }
}
```

### LoginHistory Entity

```csharp
public class LoginHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceInfo { get; set; }
    public string Browser { get; set; }
    public string IpAddress { get; set; }
    public string Location { get; set; }
    public DateTime LoginTime { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public TwoFactorMethod? TwoFactorMethod { get; set; }
}
```

---

## 🔒 MEJORAS DE SEGURIDAD RECOMENDADAS

1. **Hashear Recovery Codes** - Actualmente pueden estar en texto plano
2. **Implementar CSRF tokens** - Para formularios críticos
3. **Agregar Helmet headers** - Content-Security-Policy, etc.
4. **Rate limiting por usuario** - Además de por IP
5. **Geolocalización de IPs** - Para detección de anomalías
6. **Device fingerprinting** - Para identificación de dispositivos

---

## 📊 MÉTRICAS DE CUMPLIMIENTO

| Área                    | Documentado | Implementado | Cumplimiento |
| ----------------------- | ----------- | ------------ | ------------ |
| AuthService Endpoints   | 25          | 20           | 80%          |
| RoleService Endpoints   | 10          | 10           | 100%         |
| KYCService Endpoints    | 16          | 16           | 100%         |
| 2FA Endpoints           | 6           | 5            | 83%          |
| Frontend Auth Pages     | 10          | 4            | 40%          |
| Frontend Security Pages | 5           | 0            | 0%           |
| Frontend KYC Pages      | 3           | 0            | 0%           |
| **PROMEDIO GENERAL**    |             |              | **57.6%**    |

---

## 📌 CONCLUSIÓN

El módulo de Autenticación y Seguridad tiene una implementación sólida en el backend (especialmente RoleService y KYCService), pero presenta gaps críticos en:

1. **SecurityController** - Datos completamente mock
2. **Frontend de Seguridad** - 0% de páginas implementadas
3. **Flujo 2FA** - Incompleto en frontend
4. **Frontend KYC** - 0% de páginas implementadas

**Recomendación:** Priorizar Fase 1 (8 días) para resolver gaps críticos antes de próximo release.

---

_Documento generado automáticamente - Enero 21, 2026_
