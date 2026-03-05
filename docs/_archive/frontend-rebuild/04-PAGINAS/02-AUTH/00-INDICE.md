# 📁 02-AUTH - Autenticación y Seguridad

> **Descripción:** Flujos de autenticación, verificación y seguridad  
> **Total:** 6 documentos  
> **Prioridad:** 🔴 P0 - Core del sistema

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                                    | Descripción                                    | Prioridad |
| --- | ---------------------------------------------------------- | ---------------------------------------------- | --------- |
| 1   | [01-auth-login-register.md](01-auth-login-register.md)     | Login, registro, logout                        | P0        |
| 2   | [02-verification-flows.md](02-verification-flows.md)       | Verificación email, teléfono, 2FA              | P0        |
| 3   | [03-oauth-management.md](03-oauth-management.md)           | OAuth con Google, Facebook, Apple              | P1        |
| 4   | [04-kyc-verificacion.md](04-kyc-verificacion.md)           | Know Your Customer - Verificación de identidad | P1        |
| 5   | [05-privacy-gdpr.md](05-privacy-gdpr.md)                   | Privacidad, GDPR, consentimientos              | P1        |
| 6   | [06-user-security-privacy.md](06-user-security-privacy.md) | Configuración de seguridad del usuario         | P2        |

---

## 🎯 Orden de Implementación para IA

```
1. 01-auth-login-register.md → Login y registro básico
2. 02-verification-flows.md  → Verificación de cuenta
3. 03-oauth-management.md    → Login social (Google, etc.)
4. 04-kyc-verificacion.md    → Verificación de identidad
5. 05-privacy-gdpr.md        → Gestión de privacidad
6. 06-user-security-privacy.md → Configuración de seguridad
```

---

## 🔗 Dependencias Externas

- **05-API-INTEGRATION/02-autenticacion.md**: Endpoints de auth
- **02-UX-DESIGN-SYSTEM/**: Formularios, validaciones
- **Backend AuthService**: JWT, refresh tokens

---

## 📊 APIs Utilizadas

| Servicio    | Endpoints Principales                                     |
| ----------- | --------------------------------------------------------- |
| AuthService | POST /auth/login, POST /auth/register, POST /auth/refresh |
| AuthService | POST /auth/verify-email, POST /auth/forgot-password       |
| AuthService | POST /auth/2fa/enable, POST /auth/2fa/verify              |
| UserService | GET /users/me, PUT /users/profile                         |
