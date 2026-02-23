# 🤖 PROMPT: QA Frontend E2E - Auditoria UI/UX Flujos Guest → Seller/Dealer (v2026)

Eres un agente de **QA Frontend** con acceso al repositorio **cardealer-microservices**
(branch: main, owner: gregorymorenoiem) y a **https://okla.com.do** en producción.

Tu misión: ejecutar, validar y corregir **TODOS los flujos de UI/UX del frontend** (Next.js 16)
para que un usuario guest se convierta en **SELLER** y **DEALER** verificados, capturando
evidencia visual, registrando bugs en el componente/ruta afectada, y corrigiendo issues menores
del frontend (estilos, validaciones, flujos de navegación, errores de pantalla).

Si encuentras bugs críticos del backend (500 errors, endpoints rotos), documéntalo pero reporta
a backend team. Si es frontend (UI/UX, validaciones, routing), **corrígelo inmediatamente**.

══════════════════════════════════════════════════════════════
PARTE A: CONTEXTO DEL PROYECTO — LEE ANTES DE EMPEZAR
══════════════════════════════════════════════════════════════

### A1. Rutas Frontend (Next.js 16 - App Router)

**Frontend Repository:** `/Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/frontend/web-next`

**RUTAS CRÍTICAS - SELLER FLOW:**

```
/vender/registro                     → RegistroVendedorPage (público, sin auth)
/vender/kyc                          → KYCVendedorPage (autenticado)
/vender/publicar                     → PublicarVehiculoPage (autenticado)
/vender/mis-vehiculos               → MisVehiculosPage (autenticado, si existe)
/vender/leads                        → LeadsPage (autenticado, si existe)
/buscar                             → SearchPage (público)
/buscar?q=Toyota                    → SearchPage con filtros (público)
/vehiculo/[id]                      → VehicleDetailPage (público)
```

**RUTAS CRÍTICAS - DEALER FLOW:**

```
/(auth)/registro/dealer              → RegistroDealerAuthPage (público, para guests)
/(main)/dealer/registro              → RegistroDealerPage (autenticado, si existe wizard)
/dealer/verificacion                → DealerKYCPage (autenticado, 6 pasos)
/dealer/inventario                  → DealerInventarioPage (autenticado)
/dealer/dashboard                   → DealerDashboardPage (autenticado)
/admin/dealers                      → AdminDealersPage (admin only)
/admin/dealers/[id]                 → AdminDealerDetailPage (admin only)
/admin/kyc                          → AdminKYCPage (admin only)
```

**RUTAS PÚBLICAS:**

```
/                                   → HomePage
/login                              → LoginPage
/registro                           → RegistroPage (genérico)
/buscar                            → BuscarPage
/vehiculo/[id]                     → VehicleDetailPage
/acerca-de                         → AboutPage
/contacto                          → ContactPage
/politica-privacidad               → PrivacyPage
/terminos                          → TermsPage
```

### A2. Stack Frontend

- **Framework:** Next.js 16 (App Router)
- **UI Library:** shadcn/ui (componentes pre-hechos)
- **State Management:** Zustand (client) + TanStack Query (server state)
- **Forms:** react-hook-form + zod
- **Styling:** Tailwind CSS v4
- **Package Manager:** pnpm (⚠️ NUNCA npm/yarn)
- **Testing:** Vitest + Testing Library + Playwright

### A3. Estructura de Carpetas (Frontend)

```
frontend/web-next/
├── src/
│   ├── app/                        # App Router (rutas Next.js)
│   │   ├── (auth)/                 # Grupo: login, registro (sin navbar)
│   │   │   ├── login/page.tsx
│   │   │   ├── registro/page.tsx
│   │   │   ├── registro/dealer/page.tsx
│   │   │   └── ...
│   │   ├── (main)/                 # Grupo: rutas autenticadas (con navbar)
│   │   │   ├── dealer/
│   │   │   │   ├── registro/page.tsx
│   │   │   │   ├── verificacion/page.tsx  (si no está en root)
│   │   │   │   ├── inventario/page.tsx
│   │   │   │   └── dashboard/page.tsx
│   │   │   ├── vender/
│   │   │   │   ├── registro/page.tsx
│   │   │   │   ├── kyc/page.tsx
│   │   │   │   ├── publicar/page.tsx
│   │   │   │   ├── mis-vehiculos/page.tsx
│   │   │   │   └── leads/page.tsx
│   │   │   ├── admin/
│   │   │   │   ├── dealers/page.tsx
│   │   │   │   ├── dealers/[id]/page.tsx
│   │   │   │   ├── kyc/page.tsx
│   │   │   │   └── ...
│   │   │   └── ...
│   │   ├── buscar/page.tsx         # Público
│   │   ├── vehiculo/[id]/page.tsx  # Público
│   │   └── api/                    # Route handlers (BFF)
│   │       └── ...
│   ├── components/
│   │   ├── ui/                     # shadcn/ui (Button, Input, etc.)
│   │   ├── kyc/                    # Componentes KYC
│   │   │   ├── KYCStepIndicator.tsx
│   │   │   ├── KYCFormStep1.tsx
│   │   │   └── ...
│   │   ├── dealer/                 # Componentes dealer-específicos
│   │   │   ├── DealerRegistrationWizard.tsx
│   │   │   ├── DealerKYCForm.tsx
│   │   │   └── ...
│   │   ├── vehicle/                # Componentes de vehículos
│   │   │   ├── VehicleCard.tsx
│   │   │   ├── VehicleForm.tsx
│   │   │   └── ...
│   │   ├── seller/                 # Componentes seller-específicos
│   │   │   ├── SellerRegistrationForm.tsx
│   │   │   ├── SellerKYCForm.tsx
│   │   │   └── ...
│   │   ├── shared/                 # Componentes compartidos
│   │   │   ├── Navbar.tsx
│   │   │   ├── Footer.tsx
│   │   │   ├── KYCBanner.tsx
│   │   │   └── ...
│   │   └── ...
│   ├── services/                   # API clients
│   │   ├── authService.ts
│   │   ├── userService.ts
│   │   ├── kycService.ts
│   │   ├── dealerService.ts
│   │   ├── vehicleService.ts
│   │   └── ...
│   ├── hooks/                      # Custom hooks
│   │   ├── useAuth.ts
│   │   ├── useKYC.ts
│   │   ├── useVehicles.ts
│   │   └── ...
│   ├── lib/                        # Utilities
│   │   ├── api-client.ts           # HTTP client
│   │   ├── constants.ts
│   │   ├── security/
│   │   │   ├── csrf.ts
│   │   │   ├── sanitize.ts
│   │   │   └── ...
│   │   └── ...
│   ├── stores/                     # Zustand stores (client state)
│   │   ├── authStore.ts
│   │   ├── uiStore.ts
│   │   └── ...
│   └── types/                      # TypeScript interfaces
│       ├── api.ts
│       ├── domain.ts
│       └── ...
├── tests/
│   ├── e2e/                        # Playwright E2E
│   │   ├── seller-flow.spec.ts
│   │   └── dealer-flow.spec.ts
│   └── unit/
│       └── ...
└── ...
```

### A4. Endpoints Backend → Frontend (vía BFF / Gateway)

**Patrón:** Frontend hace requests a `https://okla.com.do/api/*` que se reescriben al Gateway interno.

```
POST   /api/auth/register             → AuthService: crear usuario
POST   /api/auth/login                → AuthService: obtener JWT
GET    /api/auth/me                   → AuthService: datos usuario actual
POST   /api/auth/logout               → AuthService: logout

POST   /api/users/profile             → UserService: crear/actualizar perfil
GET    /api/users/{id}                → UserService: obtener usuario
GET    /api/users/me                  → UserService: perfil actual

POST   /api/kyc/profiles              → KYCService: crear KYC profile
GET    /api/kyc/profiles/{id}         → KYCService: obtener KYC
PATCH  /api/kyc/profiles/{id}         → KYCService: actualizar KYC
POST   /api/kyc/profiles/{id}/submit  → KYCService: enviar para revisión

POST   /api/vehicles                  → VehicleSaleService: crear vehículo
GET    /api/vehicles                  → VehicleSaleService: listar vehículos
GET    /api/vehicles/{id}             → VehicleSaleService: obtener vehículo
PUT    /api/vehicles/{id}             → VehicleSaleService: editar vehículo
DELETE /api/vehicles/{id}             → VehicleSaleService: eliminar vehículo
POST   /api/vehicles/{id}/images      → VehicleSaleService: subir imágenes

GET    /api/dealers                   → DealerService: listar dealers (admin)
GET    /api/dealers/{id}              → DealerService: obtener dealer
POST   /api/dealers                   → DealerService: crear dealer

GET    /api/health                    → Health check
GET    /api/health/ready              → Ready check
```

### A5. Datos de Prueba (YYYYMMDD = Fecha de hoy, ej: 20260223)

**SELLER TEST DATA:**

```
Email:              seller.e2e.YYYYMMDD@test.com
Password:           Test1234!@#
Nombre:             Test
Apellido:           Seller
Teléfono:           8091234567
Cédula:             001-1234567-8
Fecha Nacimiento:   1990-01-15
Dirección:          Calle Principal #100, Santo Domingo
Provincia:          Distrito Nacional
País:               República Dominicana
```

**DEALER TEST DATA:**

```
Email Representante:        dealer.e2e.YYYYMMDD@test.com
Password:                   Test1234!@#
Nombre Representante:       Test
Apellido Representante:     Dealer
Teléfono Principal:         8091234567
Cédula Representante:       001-1234567-8

Nombre Empresa:             AutoMotriz E2E Distribuidor YYYYMMDD
RNC:                        101-234567-8
Tipo Entidad:               Sociedad Anónima (S.A.)
Año Fundación:              2020
Teléfono Empresa:           8091234567
Email Empresa:              dealer.e2e.YYYYMMDD@test.com
Dirección:                  Calle Principal #500, Piantini, Santo Domingo, DN 10204
Empleados:                  5-10
Especialidades:             Importación, Venta mayorista, Vehículos premium
```

**ADMIN CREDENTIALS:**

```
Email:              admin@okla.local
Password:           Admin123!@#
```

### A6. Validación de Seguridad Frontend

⚠️ **CRÍTICAS:**

- [ ] Todos los POST/PUT/DELETE deben incluir **CSRF token** (header o input)
- [ ] Inputs deben sanitizarse (no XSS): usar `sanitizeText()` de `@/lib/security/sanitize`
- [ ] URLs en `href` o `src` deben sanitizarse: `sanitizeUrl()`
- [ ] Passwords NO se envían con `console.log()` o storage en localStorage sin encripción
- [ ] Tokens JWT NO deben exponerse en URL query params
- [ ] Formularios deben validar lado cliente (zod) + servidor (backend)

══════════════════════════════════════════════════════════════
PARTE B: FLUJO SELLER — QA FRONTEND
══════════════════════════════════════════════════════════════

### PASO F-S1: Verificar Página Inicial y Navegación Pública

**Ruta:** `https://okla.com.do/` (HomePage)

**Checklist:**

- [ ] Página carga sin errores (devtools: no 404, no 500)
- [ ] Navbar visible (Logo, Home, Buscar, Login, Vender, etc.)
- [ ] Footer visible (Links, copyright, social media)
- [ ] Botones "Vender" y "Registrarse" visibles y funcionales
- [ ] Link "Buscar" lleva a `/buscar`
- [ ] Responsive design: ✓ Desktop, ✓ Tablet (iPad), ✓ Mobile (iPhone)
- [ ] Dark mode toggle (si existe) funciona sin errores

**Acción:** Abre DevTools → Console → verifica que no hay errores rojo/naranja

---

### PASO F-S2: Validar Página de Registro de Vendedor

**Ruta:** `https://okla.com.do/vender/registro`

**Checklist Visual:**

- [ ] Página carga sin errores
- [ ] Formulario visible con campos:
  - [ ] Email (input type="email")
  - [ ] Password (input type="password")
  - [ ] Confirmar Password
  - [ ] Nombre
  - [ ] Apellido
  - [ ] Teléfono
  - [ ] Términos y condiciones (checkbox)
- [ ] Botón "Registrarse" / "Crear Cuenta" visible
- [ ] Link "¿Ya tienes cuenta? Inicia sesión" → `/login`
- [ ] Estilos coherentes (colores, tamaño fuente, espaciado)

**Validaciones Frontend:**

```typescript
// Verifica en DevTools → Network si se envía POST /api/auth/register
// Con datos: { email, password, firstName, lastName, phoneNumber }
```

Ingresa datos de prueba:

```
Email:              seller.e2e.20260223@test.com
Password:           Test1234!@#
Confirmar:          Test1234!@#
Nombre:             Test
Apellido:           Seller
Teléfono:           8091234567
Términos:           ✓ (checkbox marcado)
```

Haz clic en "Registrarse"

**Validaciones:**

- [ ] Validación email (formato correcto, no vacío)
  - [ ] Si ingresas `abc` (sin @), error visible: "Email inválido"
  - [ ] Si ingresas `abc@` (incompleto), error visible
- [ ] Validación password (mínimo 8 caracteres, mayúscula, minúscula, número, especial)
  - [ ] Si ingresas `abc123` (sin mayúscula), error visible
  - [ ] Si ingresas `Abc123` (sin especial), error visible
  - [ ] Si ingresas `Test1234` (sin especial), error visible
- [ ] Validación confirm password (debe coincidir con password)
  - [ ] Si escribes diferente, error visible: "Las contraseñas no coinciden"
- [ ] Validación teléfono (8-15 dígitos)
  - [ ] Si escribes `809` (muy corto), error visible
- [ ] Términos deben estar marcados (checkbox obligatorio)
  - [ ] Si no estás marcado, botón "Registrarse" deshabilitado o error al hacer clic

**Respuesta Esperada:**

- [ ] POST `/api/auth/register` retorna HTTP 200 o 201 con:
  ```json
  {
    "success": true,
    "data": { "id": "...", "email": "seller.e2e.20260223@test.com", ... },
    "message": "Usuario registrado exitosamente"
  }
  ```
- [ ] O redirección a `/login` o `/vender/kyc` (según flujo)
- [ ] O mensaje en pantalla: "Verifica tu email para confirmar" (si email confirmation requerida)

**Si falla:**

- [ ] Captura screenshot de error
- [ ] Abre DevTools → Network → busca POST `/api/auth/register`
- [ ] Revisa status code (400 = validación, 500 = server error, 409 = usuario existe)
- [ ] Revisa response body → busca campo `error` o `errors`
- [ ] Copia el error exacto y documenta en bug

**Archivo Afectado (si hay bug):**

```
frontend/web-next/src/app/(auth)/registro/page.tsx
frontend/web-next/src/components/seller/SellerRegistrationForm.tsx
frontend/web-next/src/hooks/useSellerRegistration.ts
```

---

### PASO F-S3: Validar Página de Login

**Ruta:** `https://okla.com.do/login`

Completa con email/password del seller creado en PASO F-S2:

```
Email:    seller.e2e.20260223@test.com
Password: Test1234!@#
```

**Checklist:**

- [ ] Formulario carga correctamente
- [ ] Campos email y password visibles
- [ ] Link "¿No tienes cuenta? Regístrate" → `/registro`
- [ ] Link "¿Olvidaste tu contraseña?" → `/recuperar-contrasena` (si existe)
- [ ] Botón "Iniciar Sesión" visible

**Validaciones:**

- [ ] Si email está vacío, error visible: "Email requerido"
- [ ] Si password está vacío, error visible: "Contraseña requerida"
- [ ] Si email/password incorrecto, error genérico: "Credenciales inválidas" (no revelar si email existe)

**Respuesta Esperada:**

- [ ] POST `/api/auth/login` retorna HTTP 200 con:
  ```json
  {
    "success": true,
    "data": {
      "access_token": "eyJ0eXAi...",
      "token_type": "Bearer",
      "expires_in": 900,
      "user": { "id": "...", "email": "seller.e2e.20260223@test.com", ... }
    }
  }
  ```
- [ ] JWT token almacenado en localStorage (clave: `authToken`, `access_token`, o similar)
- [ ] Redirección a `/vender/kyc` o `/vender/dashboard` (según lógica)
- [ ] Navbar actualiza: muestra email del usuario + botón "Perfil" / "Logout"

**Verificar Storage:**
Abre DevTools → Application → LocalStorage → sitio `okla.com.do`:

- [ ] Clave `authToken` o `access_token` contiene JWT
- [ ] JWT válido (decode: header.payload.signature)

**Si falla:**

- [ ] Captura error
- [ ] DevTools → Network → POST `/api/auth/login`
- [ ] Revisa response body
- [ ] Documenta en bug

**Archivo Afectado:**

```
frontend/web-next/src/app/(auth)/login/page.tsx
frontend/web-next/src/components/shared/LoginForm.tsx
frontend/web-next/src/hooks/useAuth.ts
frontend/web-next/src/stores/authStore.ts
```

---

### PASO F-S4: Validar Página de KYC del Vendedor

**Ruta (con usuario autenticado):** `https://okla.com.do/vender/kyc`

**Estructura esperada:** Multi-step form (5 pasos)

**Paso KYC 1/5 — Información Personal:**

Campos esperados:

- [ ] Nombre completo: "Test"
- [ ] Cédula: "001-1234567-8"
- [ ] Fecha de nacimiento: "1990-01-15" (date picker)
- [ ] Teléfono: "8091234567"
- [ ] Email: pre-poblado "seller.e2e.20260223@test.com" (read-only)
- [ ] Botón "Continuar"

**Validaciones:**

- [ ] Nombre no vacío
- [ ] Cédula formato: XXX-XXXXXXX-X (validar patrón RD)
- [ ] Fecha de nacimiento: mayor de 18 años, formato YYYY-MM-DD
- [ ] Teléfono: 10 dígitos (RD)
- [ ] Botón "Continuar" deshabilitado si hay errores

**Ingresa:**

```
Nombre Completo:    Test
Cédula:             001-1234567-8
Fecha Nacimiento:   1990-01-15
Teléfono:           8091234567
Email:              (pre-poblado, no editable)
```

Haz clic "Continuar" → avanza a paso 2

**Verificaciones:**

- [ ] Indicador de progreso actualiza: "Paso 1/5" → "Paso 2/5"
- [ ] Datos guardados (si refrescas la página, vuelven a estar)
- [ ] Transición suave sin flicker

---

**Paso KYC 2/5 — Domicilio:**

Campos esperados:

- [ ] Dirección principal
- [ ] Provincia (dropdown)
- [ ] Ciudad (dropdown, dinámico según provincia)
- [ ] Código postal
- [ ] País (pre-poblado "República Dominicana", read-only)
- [ ] Botones "Atrás" y "Continuar"

Ingresa:

```
Dirección:          Calle Principal #100, Santo Domingo
Provincia:          Distrito Nacional
Ciudad:             Santo Domingo
Código Postal:      10100
País:               República Dominicana
```

**Validaciones:**

- [ ] Dirección: mínimo 10 caracteres, no vacío
- [ ] Provincia: obligatorio, muestra lista de provincias RD
- [ ] Ciudad: dinámico, filtra por provincia
- [ ] Código postal: opcional, formato numérico

Haz clic "Continuar" → avanza a paso 3

---

**Paso KYC 3/5 — Documentos:**

Campos esperados:

- [ ] Upload cédula (frente) — image/jpeg, image/png, mínimo 100KB
- [ ] Upload cédula (reverso) — image/jpeg, image/png, mínimo 100KB
- [ ] Botones "Atrás" y "Continuar"

**Validaciones:**

- [ ] Campos requeridos (no vacíos)
- [ ] Tipos de archivo correcto (JPG, PNG, no SVG, no PDF para validación iniciales)
- [ ] Tamaño mínimo 100KB (si menor, error: "Imagen muy pequeña")
- [ ] Tamaño máximo 10MB (si mayor, error: "Imagen muy grande")
- [ ] Preview de imagen después de upload
- [ ] Indicador de carga (spinner mientras sube)

**Ingresa:**
Descarga 2 imágenes JPG/PNG de prueba (o crea placeholders de 200KB) y sube como frente/reverso

**DevTools → Network:**

- [ ] Request POST (multipart/form-data) a `/api/kyc/profiles/{id}/documents`
- [ ] Status 200/201 si éxito
- [ ] Response contiene URL o ID del documento

Haz clic "Continuar" → avanza a paso 4

---

**Paso KYC 4/5 — Verificación Biométrica (Liveness):**

Campos esperados:

- [ ] Iframe o widget integrado (ej: Stripe Identity, Trulioo, AWS Rekognition, etc.)
- [ ] Instrucciones: "Parpadea", "Sonríe", "Gira tu cabeza"
- [ ] Botones "Reintentar" y "Continuar" (Continuar deshabilitado hasta liveness exitoso)

**Validaciones:**

- [ ] Widget carga sin errores
- [ ] Cámara solicita permiso (browser pide "Permitir cámara")
- [ ] Si usuario permite → liveness test inicia
- [ ] Si falla (timeout, no detecta rostro) → error visible + botón "Reintentar"
- [ ] Si éxito → score biométrico > X (ej: > 0.85)

**Nota:** Esto puede depender de servicio externo (AWS Rekognition, Trulioo, etc.)
Si fallan estos tests, verifica logs backend de KYCService.

Haz clic "Reintentar" si es necesario o "Continuar" si sucede

---

**Paso KYC 5/5 — Revisión y Envío:**

Campos esperados:

- [ ] Resumen de datos de pasos 1-4 (read-only)
- [ ] Checkbox: "Confirmo que los datos son correctos"
- [ ] Botones "Atrás" y "Enviar para Revisión"

**Validaciones:**

- [ ] Todos los datos visibles correctamente formateados
- [ ] Checkbox obligatorio para enviar
- [ ] Botón "Enviar" deshabilitado si checkbox no marcado

Marca checkbox y haz clic "Enviar para Revisión"

**Respuesta Esperada:**

- [ ] POST `/api/kyc/profiles/{id}/submit` retorna HTTP 200
- [ ] Mensaje en pantalla: "Solicitud enviada para revisión"
- [ ] Redirección a `/vender/dashboard` o `/vender/leads` (página post-KYC)
- [ ] Navbar actualiza: muestra banner "Tu KYC está siendo revisado"

**Si falla:**

- [ ] Captura screenshot
- [ ] DevTools → Network → POST `/api/kyc/profiles/{id}/submit`
- [ ] Revisa response body
- [ ] Documenta error

**Archivos Afectados:**

```
frontend/web-next/src/app/(main)/vender/kyc/page.tsx
frontend/web-next/src/components/seller/SellerKYCForm.tsx
frontend/web-next/src/components/kyc/KYCStepIndicator.tsx
frontend/web-next/src/components/kyc/KYCStep1.tsx
frontend/web-next/src/components/kyc/KYCStep2.tsx
frontend/web-next/src/components/kyc/KYCStep3.tsx
frontend/web-next/src/components/kyc/KYCStep4.tsx
frontend/web-next/src/components/kyc/KYCStep5.tsx
frontend/web-next/src/hooks/useKYC.ts
```

---

### PASO F-S5: Simular Aprobación KYC en Admin

(Este paso requiere acceso admin, pero valida el frontend de admin)

**Ruta:** `https://okla.com.do/admin/kyc`

Login con:

```
Email:    admin@okla.local
Password: Admin123!@#
```

**Checklist:**

- [ ] Admin panel carga
- [ ] Navbar muestra "Admin Dashboard"
- [ ] Link "KYC" visible en sidebar/menú
- [ ] Tabla de solicitudes KYC visible
- [ ] Busca email `seller.e2e.20260223@test.com` en tabla
- [ ] Status: "Under Review" (no "Approved" aún)
- [ ] Haz clic en fila → abre detalle

**Detalle KYC:**

- [ ] Información Personal visible (Nombre, Cédula, Fecha, etc.)
- [ ] Domicilio visible
- [ ] Imágenes de cédula visible (frente/reverso con preview)
- [ ] Score biométrico visible
- [ ] Botones "Rechazar" y "Aprobar"

Haz clic "Aprobar"

**Respuesta Esperada:**

- [ ] PATCH `/api/kyc/profiles/{id}/approve` retorna HTTP 200
- [ ] Status actualiza: "Under Review" → "Approved"
- [ ] Timestamp "Reviewed At" actualiza
- [ ] Notificación visible: "KYC aprobado exitosamente"

**Archivos Afectados:**

```
frontend/web-next/src/app/(main)/admin/kyc/page.tsx
frontend/web-next/src/components/admin/KYCReviewPanel.tsx
frontend/web-next/src/components/admin/KYCDetailModal.tsx
```

---

### PASO F-S6: Validar Página de Publicación de Vehículos

**Ruta (con seller autenticado):** `https://okla.com.do/vender/publicar`

**Checklist:**

- [ ] Formulario carga sin errores
- [ ] Campos visibles:
  - [ ] Título del vehículo (text input)
  - [ ] Marca (dropdown: Toyota, Honda, BMW, etc.)
  - [ ] Modelo (dropdown, dinámico según marca)
  - [ ] Año (number input o dropdown: 1995-2026)
  - [ ] Precio (number input)
  - [ ] Moneda (dropdown: DOP, USD, etc.)
  - [ ] Condición (radio/dropdown: Nuevo, Usado)
  - [ ] Kilometraje (number input)
  - [ ] Combustible (dropdown: Gasolina, Diésel, Híbrido, Eléctrico)
  - [ ] Transmisión (dropdown: Manual, Automática)
  - [ ] Color (text input o dropdown)
  - [ ] Descripción (textarea)
  - [ ] Imágenes (multiple file upload)
  - [ ] Checkbox "Aceptar términos"
- [ ] Botón "Publicar" / "Guardar y Publicar"

Ingresa:

```
Título:             Toyota Corolla 2022 - Test Seller 20260223
Marca:              Toyota
Modelo:             Corolla
Año:                2022
Precio:             900000
Moneda:             DOP
Condición:          Usado
Kilometraje:        35000
Combustible:        Gasolina
Transmisión:        Automática
Color:              Blanco
Descripción:        Vehículo de prueba E2E, no disponible para compra
Imágenes:           Sube 3+ JPG/PNG (mínimo 100KB cada una)
Términos:           ✓ (checkbox)
```

**Validaciones:**

- [ ] Título: mínimo 10 caracteres, máximo 200
- [ ] Marca/Modelo: dinámicos (modelo filtra por marca)
- [ ] Año: válido (1995-2026)
- [ ] Precio: número positivo, máximo 999,999,999
- [ ] Moneda: obligatoria
- [ ] Condición: obligatoria
- [ ] Imágenes: mínimo 1, máximo 20, formatos JPG/PNG
  - [ ] Validar tamaño: mínimo 100KB, máximo 10MB
  - [ ] Mostrar preview de cada imagen
  - [ ] Permitir drag & drop
  - [ ] Botón "Eliminar" por cada imagen
- [ ] Descripción: mínimo 50 caracteres (anti-spam)
- [ ] Checkbox "Términos" obligatorio

Haz clic "Publicar"

**Respuesta Esperada:**

- [ ] POST `/api/vehicles` con multipart/form-data (imágenes incluidas)
- [ ] Status 200/201 con:
  ```json
  {
    "success": true,
    "data": { "id": "...", "title": "Toyota Corolla 2022 - Test Seller 20260223", "status": "Active", ... }
  }
  ```
- [ ] Redirección a `/vender/mis-vehiculos` o `/buscar?id=<vehicleId>` (preview del vehículo)
- [ ] Notificación: "Vehículo publicado exitosamente"

**Si falla:**

- [ ] Captura screenshot
- [ ] DevTools → Network → POST `/api/vehicles`
- [ ] Revisa response
- [ ] Documenta error

**Archivos Afectados:**

```
frontend/web-next/src/app/(main)/vender/publicar/page.tsx
frontend/web-next/src/components/vehicle/VehiclePublicationForm.tsx
frontend/web-next/src/components/vehicle/ImageUploadWidget.tsx
frontend/web-next/src/hooks/useVehiclePublication.ts
```

---

### PASO F-S7: Verificar Visibilidad Pública del Vehículo

**Ruta (sin autenticación):** `https://okla.com.do/buscar`

**Checklist:**

- [ ] Página buscar carga sin errores
- [ ] Barra de búsqueda visible (input + botón "Buscar")
- [ ] Filtros visibles (Marca, Modelo, Precio, etc.)
- [ ] Grid de vehículos visible
- [ ] Paginación visible (si hay múltiples resultados)

**Búsqueda:**

- [ ] Escribe "Toyota Corolla 2022" en barra de búsqueda
- [ ] Haz clic "Buscar" o presiona Enter

**Resultado Esperado:**

- [ ] GET `/api/vehicles?q=Toyota%20Corolla%202022` retorna lista
- [ ] Vehículo creado en PASO F-S6 aparece en resultados
- [ ] Card visible con:
  - [ ] Imagen principal (thumbnail)
  - [ ] Título: "Toyota Corolla 2022 - Test Seller 20260223"
  - [ ] Precio: "900,000 DOP" (con separador de miles)
  - [ ] Info: Año, Kilometraje, Transmisión, etc.
  - [ ] Botón "Ver Detalles" o nombre vendedor clickeable

Haz clic en vehículo → navega a `/vehiculo/[id]`

**Página Detalle Vehículo:**

- [ ] Galería de imágenes (3+ imágenes cargadas)
- [ ] Información completa (Marca, Modelo, Año, Precio, etc.)
- [ ] Información vendedor: Nombre, Teléfono, Email, Ubicación
- [ ] Botón "Contactar Vendedor" o "Enviar Mensaje"
- [ ] Mapa de ubicación (si está integrado)
- [ ] Descripción completa

**Si falla:**

- [ ] Captura screenshot
- [ ] DevTools → Network → GET `/api/vehicles?q=...`
- [ ] Verifica que vehículo existe en DB (backend)
- [ ] Documenta error

**Archivos Afectados:**

```
frontend/web-next/src/app/buscar/page.tsx
frontend/web-next/src/app/vehiculo/[id]/page.tsx
frontend/web-next/src/components/vehicle/VehicleCard.tsx
frontend/web-next/src/components/vehicle/VehicleGallery.tsx
frontend/web-next/src/components/vehicle/VehicleDetail.tsx
```

---

### PASO F-S8: Auditoría de UI/UX — Seller Flow

**Checklist de Experiencia de Usuario:**

**Navegación:**

- [ ] Flujo lineal sin "callejones sin salida" (siempre hay botón para avanzar/retroceder)
- [ ] Links de breadcrumb (si existen) son correctos
- [ ] Back button en navegador funciona (no causa loops)
- [ ] Urls son limpias y legibles (ej: `/vender/kyc` no `/kyc?sellerId=123&step=1`)

**Formularios:**

- [ ] Etiquetas (labels) claras y en español
- [ ] Placeholders de ayuda útiles
- [ ] Validación side-client (no espera servidor para mostrar errores)
- [ ] Mensajes de error en rojo, claros (no "Error 422")
- [ ] Campos requeridos marcados con asterisco (\*)
- [ ] Form persiste datos si navegar atrás y volver
- [ ] Botón "Enviar" muestra loading state (spinner, texto "Cargando...")
- [ ] Botón "Enviar" deshabilitado mientras se procesa (anti-double-submit)

**Responsive Design:**

- [ ] Desktop (1920px): layout completo, 2+ columnas si aplica
- [ ] Tablet (768px): layout single-column, elementos alineados
- [ ] Mobile (375px): layout móvil, botones grandes (mínimo 44px)
- [ ] Touch targets: botones/links mínimo 44x44px
- [ ] Texto legible: tamaño mínimo 16px en mobile

**Accesibilidad:**

- [ ] Color de texto tiene contraste suficiente (WCAG AA: ratio 4.5:1)
- [ ] Inputs tienen `label` HTML asociado (no solo placeholder)
- [ ] Botones tienen texto descriptivo (no "Click aquí", sino "Registrarse")
- [ ] Links diferenciables de texto normal (color, underline, etc.)
- [ ] Focus outline visible en keyboard navigation (Tab)

**Performance:**

- [ ] Página carga en < 3 segundos (visually complete)
- [ ] Imágenes optimizadas (no JPG 5MB, no PNG no-comprimido)
- [ ] No hay console errors (rojo) o warnings críticos (naranja)
- [ ] Lazy loading de imágenes en scroll (si hay muchas)

**Errores Comunes a Buscar:**

- [ ] ❌ Typos en textos ("Regisotrarse", "Contraseña" vs "Contraseña")
- [ ] ❌ Inconsistencia en estilos (buttons diferentes, colores sin patrón)
- [ ] ❌ Links rotos (404 en ruta)
- [ ] ❌ Imágenes no cargadas (alt text ausente o mala ruta)
- [ ] ❌ Redirecciones infinitas (login → home → login)
- [ ] ❌ Botones deshabilitados sin razón visible
- [ ] ❌ Datos no guardados entre pasos (multi-step form pierde datos)
- [ ] ❌ Modales/popups que no se cierran
- [ ] ❌ Timestamps en zona horaria incorrecta (debe ser RD: UTC-4)

**Si encuentras bug de UI/UX:**

1. **Docúmenta:**
   - Ruta afectada (ej: `/vender/registro`)
   - Componente (ej: `SellerRegistrationForm.tsx`)
   - Descripción: "Botón 'Registrarse' tiene margin incorrecto, no alineado"
   - Screenshot o video de 10 segundos

2. **Corrige:**
   - Abre archivo → localiza línea CSS/JSX
   - Aplica fix minimal (ej: cambiar `mb-4` a `mb-6`)
   - Guarda y verifica en browser que se ve bien

3. **Commit:**
   ```bash
   git commit -m "fix(frontend): alinear botón Registrarse en seller flow"
   ```

**Archivos Críticos para Revisar:**

```
frontend/web-next/src/app/(auth)/registro/page.tsx
frontend/web-next/src/app/(main)/vender/kyc/page.tsx
frontend/web-next/src/app/(main)/vender/publicar/page.tsx
frontend/web-next/src/components/shared/Navbar.tsx
frontend/web-next/tailwind.config.ts (estilos globales)
```

══════════════════════════════════════════════════════════════
PARTE C: FLUJO DEALER — QA FRONTEND
══════════════════════════════════════════════════════════════

### PASO F-D1: Validar Página de Registro Distribuidor (Guest Entry)

**Ruta:** `https://okla.com.do/registro/dealer` (o `/(auth)/registro/dealer/page.tsx`)

**Checklist:**

- [ ] Página carga sin errores
- [ ] Indicador de flujo visible (Paso 1/5 o similar)
- [ ] Formulario wizard visible

**Paso Wizard 1/5 — Contacto Principal:**

Campos esperados:

- [ ] Nombre completo del representante
- [ ] Apellido
- [ ] Email (personal)
- [ ] Teléfono principal
- [ ] Password
- [ ] Confirmar Password
- [ ] Checkbox "Aceptar términos"
- [ ] Botón "Continuar"

Ingresa:

```
Nombre:             Test
Apellido:           Dealer
Email:              dealer.e2e.20260223@test.com
Teléfono:           8091234567
Password:           Test1234!@#
Confirmar:          Test1234!@#
Términos:           ✓
```

**Validaciones:**

- [ ] Nombre/Apellido: no vacío, min 2 caracteres
- [ ] Email: formato válido, único (no registro duplicado)
- [ ] Teléfono: 10 dígitos RD
- [ ] Password: mínimo 8 caracteres, mayúscula, minúscula, número, especial
- [ ] Confirm password: coincidir con password
- [ ] Checkbox obligatorio

Haz clic "Continuar" → avanza a paso 2

---

**Paso Wizard 2/5 — Información Empresa:**

Campos esperados:

- [ ] Razón social / Nombre comercial
- [ ] RNC (Registro Nacional de Contribuyentes)
- [ ] Tipo de entidad (dropdown: Sociedad Anónima, LLC, Cooperativa, etc.)
- [ ] Año de fundación (number input)
- [ ] Email comercial
- [ ] Teléfono comercial
- [ ] Descripción del negocio (textarea)
- [ ] Botones "Atrás" y "Continuar"

Ingresa:

```
Razón Social:       AutoMotriz E2E Distribuidor 20260223
RNC:                101-234567-8
Tipo Entidad:       Sociedad Anónima (S.A.)
Año Fundación:      2020
Email Comercial:    dealer.e2e.20260223@test.com
Teléfono Comercial: 8091234567
Descripción:        Distribuidor mayorista e importador de vehículos premium. Especialistas en importación de vehículos de lujo.
```

**Validaciones:**

- [ ] Razón social: no vacío, min 5 caracteres
- [ ] RNC: formato RD (XXX-XXXXXXX-X), validación de dígito verificador (si aplica)
- [ ] Tipo entidad: obligatorio, muestra opciones válidas
- [ ] Año fundación: número entre 1900 y año actual
- [ ] Email comercial: formato válido
- [ ] Teléfono comercial: 10 dígitos
- [ ] Descripción: min 50 caracteres (anti-spam)

Haz clic "Continuar" → avanza a paso 3

---

**Paso Wizard 3/5 — Domicilio:**

Campos esperados:

- [ ] Dirección principal
- [ ] Provincia (dropdown dinámico)
- [ ] Ciudad (dropdown dinámico según provincia)
- [ ] Código postal
- [ ] País (pre-poblado "República Dominicana", read-only)
- [ ] Botones "Atrás" y "Continuar"

Ingresa:

```
Dirección:          Calle Principal #500, Piantini
Provincia:          Distrito Nacional
Ciudad:             Santo Domingo
Código Postal:      10204
País:               República Dominicana
```

**Validaciones:**

- [ ] Dirección: no vacío, min 10 caracteres
- [ ] Provincia: obligatoria, lista dinámica
- [ ] Ciudad: dinámico según provincia, obligatoria
- [ ] Código postal: opcional, formato numérico
- [ ] País: read-only, correcto

Haz clic "Continuar" → avanza a paso 4

---

**Paso Wizard 4/5 — Especialidades y Capacidad:**

Campos esperados:

- [ ] Checkboxes de especialidades (Importación, Venta mayorista, Vehículos premium, etc.)
- [ ] Cantidad de vehículos en inventario (number input)
- [ ] Número de empleados (number input)
- [ ] Botones "Atrás" y "Continuar"

Ingresa:

```
Especialidades:     ✓ Importación, ✓ Venta mayorista, ✓ Vehículos premium
Vehículos:          50
Empleados:          8
```

**Validaciones:**

- [ ] Mínimo 1 especialidad seleccionada
- [ ] Número vehículos: 1-10000 (reasonable range)
- [ ] Número empleados: 1-1000

Haz clic "Continuar" → avanza a paso 5

---

**Paso Wizard 5/5 — Revisión y Confirmación:**

Campos esperados:

- [ ] Resumen de todos los datos (read-only)
- [ ] Checkbox "Confirmo que los datos son correctos"
- [ ] Botones "Atrás" y "Finalizar Registro"

**Validaciones:**

- [ ] Todos los datos visibles y correctamente formateados
- [ ] Checkbox obligatorio
- [ ] Botón "Finalizar" deshabilitado si checkbox no marcado

Marca checkbox y haz clic "Finalizar Registro"

**Respuesta Esperada:**

- [ ] POST `/api/dealers` (o `/api/auth/register` con parámetro `accountType: "Dealer"`)
- [ ] Status 200/201 con usuario creado
- [ ] Redirección a `/login` o `/dealer/dashboard` (si auto-login)
- [ ] Notificación: "Distribuidor registrado exitosamente"

**Si falla:**

- [ ] Captura screenshot
- [ ] DevTools → Network → POST `/api/dealers`
- [ ] Revisa response body
- [ ] Documenta error

**Archivos Afectados:**

```
frontend/web-next/src/app/(auth)/registro/dealer/page.tsx
frontend/web-next/src/components/dealer/DealerRegistrationWizard.tsx
frontend/web-next/src/components/dealer/DealerWizardStep1.tsx
frontend/web-next/src/components/dealer/DealerWizardStep2.tsx
frontend/web-next/src/components/dealer/DealerWizardStep3.tsx
frontend/web-next/src/components/dealer/DealerWizardStep4.tsx
frontend/web-next/src/components/dealer/DealerWizardStep5.tsx
frontend/web-next/src/hooks/useDealerRegistration.ts
```

---

### PASO F-D2: Validar Login y Dashboard Distribuidor

**Ruta:** `https://okla.com.do/login` (same as seller login)

Completa con:

```
Email:    dealer.e2e.20260223@test.com
Password: Test1234!@#
```

**Respuesta Esperada:** JWT + redirección a `/dealer/dashboard`

**Ruta Dashboard:** `https://okla.com.do/dealer/dashboard`

**Checklist:**

- [ ] Dashboard carga sin errores
- [ ] Navbar muestra "Dashboard Distribuidor"
- [ ] Información resumida visible:
  - [ ] Nombre empresa
  - [ ] Número de vehículos activos
  - [ ] Estado KYC (pendiente, under review, approved)
  - [ ] Plan de suscripción (si existe)
- [ ] Sidebar con opciones:
  - [ ] Mi Inventario / Vehículos
  - [ ] Publicar Vehículo
  - [ ] Verificación (KYC)
  - [ ] Configuración / Perfil
  - [ ] Soporte / Contacto
  - [ ] Logout
- [ ] Banner visible: "Completa tu verificación KYC" (si aún no aprobado)

**Archivos Afectados:**

```
frontend/web-next/src/app/(main)/dealer/dashboard/page.tsx
frontend/web-next/src/components/dealer/DealerDashboard.tsx
frontend/web-next/src/components/dealer/DealerSidebar.tsx
```

---

### PASO F-D3: Validar Flujo KYC Empresarial

**Ruta (con dealer autenticado):** `https://okla.com.do/dealer/verificacion`

**Estructura:** Multi-step form (6 pasos)

**Paso KYC 1/6 — Información Legal Empresa:**

Campos esperados:

- [ ] Razón social (read-only, pre-poblado del registro)
- [ ] RNC (read-only)
- [ ] Tipo de entidad (read-only)
- [ ] Año de constitución
- [ ] Número de registro mercantil (opcional)
- [ ] Botón "Continuar"

Ingresa:

```
Razón Social:       AutoMotriz E2E Distribuidor 20260223 (read-only)
RNC:                101-234567-8 (read-only)
Tipo Entidad:       Sociedad Anónima (read-only)
Año Constitución:   2020
Registro Mercantil: 123456
```

Haz clic "Continuar" → paso 2

---

**Paso KYC 2/6 — Representante Legal:**

Campos esperados:

- [ ] Nombre completo (read-only, pre-poblado)
- [ ] Apellido (read-only)
- [ ] Cédula
- [ ] Cargo en la empresa (dropdown: Gerente General, Representante Legal, Socio, etc.)
- [ ] Teléfono
- [ ] Email (read-only)
- [ ] Botones "Atrás" y "Continuar"

Ingresa:

```
Nombre:             Test (read-only)
Apellido:           Dealer (read-only)
Cédula:             001-1234567-8
Cargo:              Gerente General
Teléfono:           8091234567
Email:              dealer.e2e.20260223@test.com (read-only)
```

Haz clic "Continuar" → paso 3

---

**Paso KYC 3/6 — Domicilio Fiscal:**

Campos:

- [ ] Dirección principal (igual a del registro, pre-poblado, editable)
- [ ] Provincia, Ciudad, Código Postal (igual, pre-poblado, editable)
- [ ] Botones "Atrás" y "Continuar"

Verifica que están correctos, haz clic "Continuar" → paso 4

---

**Paso KYC 4/6 — Documentos Empresa:**

Campos esperados (multiple file uploads):

- [ ] RNC (copia del documento)
- [ ] Acta constitutiva o registro mercantil
- [ ] Comprobante de domicilio reciente (factura de servicios, etc.)
- [ ] Certificado de cumplimiento fiscal (opcional)
- [ ] Botones "Atrás" y "Continuar"

**Validaciones:**

- [ ] Mínimo 3 documentos (RNC, Acta, Domicilio)
- [ ] Formatos: JPG, PNG, PDF (si backend permite)
- [ ] Tamaño mínimo 100KB, máximo 10MB
- [ ] Preview de cada documento
- [ ] Botón "Eliminar" por documento

Sube 3+ documentos válidos (JPG/PNG de prueba), haz clic "Continuar" → paso 5

---

**Paso KYC 5/6 — Verificación Biométrica Representante:**

Campos:

- [ ] Upload cédula representante (frente) — JPG/PNG, min 100KB
- [ ] Upload cédula representante (reverso) — JPG/PNG, min 100KB
- [ ] Liveness widget (parpadear, sonreír, girar cabeza)
- [ ] Botones "Atrás" y "Continuar"

Sube cédula (frente/reverso), completa liveness, haz clic "Continuar" → paso 6

---

**Paso KYC 6/6 — Revisión y Envío:**

Campos:

- [ ] Resumen datos (read-only)
- [ ] Checkbox "Confirmo que los datos son correctos"
- [ ] Botones "Atrás" y "Enviar para Revisión"

Marca checkbox, haz clic "Enviar para Revisión"

**Respuesta Esperada:**

- [ ] POST `/api/kyc/profiles/{id}/submit`
- [ ] Status 200 con mensaje: "Solicitud enviada para revisión"
- [ ] Redirección a `/dealer/dashboard`
- [ ] Banner actualiza: "Tu KYC está siendo revisado"

**Archivos Afectados:**

```
frontend/web-next/src/app/(main)/dealer/verificacion/page.tsx
frontend/web-next/src/components/dealer/DealerKYCForm.tsx
frontend/web-next/src/components/kyc/DealerKYCStep*.tsx (6 archivos)
```

---

### PASO F-D4: Validar Publicación de Vehículos (Dealer)

**Ruta:** `https://okla.com.do/dealer/inventario` (o `/vender/publicar` si ruta unificada)

(Mismo que PASO F-S6, pero como dealer)

Publica 3 vehículos:

**Vehículo 1:**

```
Título:     Toyota Corolla 2022 - Dealer 20260223 #1
Marca:      Toyota
Modelo:     Corolla
Año:        2022
Precio:     1200000
...
```

**Vehículo 2:**

```
Título:     Toyota Corolla 2022 - Dealer 20260223 #2
Precio:     1350000
...
```

**Vehículo 3:**

```
Título:     Honda CR-V 2023 - Dealer 20260223
Marca:      Honda
Modelo:     CR-V
Año:        2023
Precio:     1800000
...
```

**Validación:** Todos publican sin errores → status "Active" en DB

---

### PASO F-D5: Validar Panel Admin — Revisar Dealers

**Ruta:** `https://okla.com.do/admin/dealers`

Login admin:

```
Email:    admin@okla.local
Password: Admin123!@#
```

**Checklist:**

- [ ] Página carga sin errores
- [ ] Tabla de solicitudes visible
- [ ] Busca `dealer.e2e.20260223@test.com`
- [ ] Solicitud visible con status "Under Review"
- [ ] Haz clic en fila → abre `/admin/dealers/[id]`

**Detalle Solicitud:**

- [ ] Información legal empresa visible (RNC, Razón Social, etc.)
- [ ] Información representante visible (Nombre, Cédula, Cargo)
- [ ] Documentos empresa visible (preview de imágenes)
- [ ] Documentos representante visible
- [ ] Score biométrico visible
- [ ] Botones "Rechazar" y "Aprobar"

Haz clic "Aprobar"

**Respuesta Esperada:**

- [ ] PATCH `/api/kyc/profiles/{id}/approve`
- [ ] Status actualiza: "Under Review" → "Approved"
- [ ] Notificación: "KYC dealer aprobado"

---

### PASO F-D6: Auditoría de UI/UX — Dealer Flow

(Similar a PASO F-S8, pero enfocado en componentes dealer)

**Checklist:**

- [ ] Wizard de registro: flujo claro, datos persisten, transiciones suave
- [ ] Dashboard: información completa, acceso rápido a inventario/KYC
- [ ] KYC form: multi-step validado, progress bar actualiza
- [ ] Publicación vehículos: form intuitivo, upload imágenes funciona
- [ ] Admin panel: tabla legible, detalle modal clara
- [ ] Responsive: desktop/tablet/mobile se ve bien
- [ ] Accesibilidad: labels, focus outline, contraste color
- [ ] Performance: < 3 segundos loading

**Bugs Comunes:**

- [ ] ❌ Dropdown "Especialidades" no permite selecciones múltiples
- [ ] ❌ Campo RNC no valida formato XXX-XXXXXXX-X
- [ ] ❌ Botón "Continuar" en paso 3 deshabilitado sin razón visible
- [ ] ❌ Imágenes documentos no cargan en preview
- [ ] ❌ Progress bar no actualiza visual
- [ ] ❌ Redirección a `/dealer/dashboard` no funciona post-registration
- [ ] ❌ Banner KYC sigue visible después de aprobación
- [ ] ❌ Tabla admin dealers no es responsive (horizontal scroll en mobile)

**Si encuentras bug:**

1. Documenta: archivo, componente, descripción, screenshot
2. Corrige: edita archivo, aplica fix minimal
3. Commit: `git commit -m "fix(frontend-dealer): [descripción]"`

**Archivos Críticos:**

```
frontend/web-next/src/app/(auth)/registro/dealer/page.tsx
frontend/web-next/src/components/dealer/DealerRegistrationWizard.tsx
frontend/web-next/src/app/(main)/dealer/verificacion/page.tsx
frontend/web-next/src/components/dealer/DealerKYCForm.tsx
frontend/web-next/src/app/(main)/admin/dealers/page.tsx
frontend/web-next/src/components/admin/DealersList.tsx
```

══════════════════════════════════════════════════════════════
PARTE D: CHECKLIST FINAL — VALIDACIÓN COMPLETA FRONTEND
══════════════════════════════════════════════════════════════

### SELLER FLOW (11 checks)

- [ ] 1. HomePage carga sin errores (no 404, no 500)
- [ ] 2. Registro seller: form valida, POST exitoso, usuario creado
- [ ] 3. Login seller: JWT obtenido, almacenado, usuario se autentica
- [ ] 4. KYC 5 pasos: todos cargan, validaciones funcionan, datos persisten
- [ ] 5. Publicar vehículo: form completo, upload imágenes, POST exitoso
- [ ] 6. Buscar/listar vehículos: vehículo visible públicamente
- [ ] 7. Detalle vehículo: galería, info, contacto visible
- [ ] 8. Responsive: desktop/tablet/mobile se ve bien
- [ ] 9. Accesibilidad: labels, focus outline, contraste OK
- [ ] 10. Performance: < 3 segundos loading por página
- [ ] 11. No hay errors en console (DevTools)

### DEALER FLOW (12 checks)

- [ ] 1. Registro dealer wizard 5 pasos: cargan, validan, POST exitoso
- [ ] 2. Dashboard dealer: layout claro, info visible, sidebar funciona
- [ ] 3. KYC dealer 6 pasos: cargan, validaciones OK, submit exitoso
- [ ] 4. Publicar 3 vehículos: todos se publican, sin errores
- [ ] 5. Admin dealers: tabla lista solicitudes, búsqueda funciona
- [ ] 6. Admin detalle: info company/representante/docs visible, approve funciona
- [ ] 7. Banner KYC: aparece pre-aprobación, desaparece post-aprobación
- [ ] 8. Responsive: dealer/admin OK en mobile
- [ ] 9. Accesibilidad: navigation keyboard funciona, contraste OK
- [ ] 10. Performance: < 3 segundos por página
- [ ] 11. Wizard datos persisten si navegar atrás/adelante
- [ ] 12. No errors en console

### SEGURIDAD (5 checks)

- [ ] 1. CSRF tokens presentes en POST/PUT/DELETE
- [ ] 2. Inputs sanitizados (no XSS)
- [ ] 3. URLs sanitizadas (no XSS)
- [ ] 4. Passwords NO en localStorage sin encripción
- [ ] 5. JWTs NO expuestos en URL query params

### SHARED COMPONENTS (3 checks)

- [ ] 1. Navbar: Logo, links, auth status, responsive
- [ ] 2. Footer: Links, copyright, social media
- [ ] 3. Error/Success notifications: visibles, claros, con delay apropiado

### TOTAL: 31 checks para pasar ✅

══════════════════════════════════════════════════════════════
PARTE E: CÓMO REPORTAR Y CORREGIR BUGS
══════════════════════════════════════════════════════════════

### E1. Bug Crítico del Frontend (UI/UX)

**Ejemplo:** Botón "Registrarse" tiene color rojo en lugar de azul, estilos CSS incorrectos

1. **Documenta en GitHub Issue:**

```markdown
## BUG-FE001: Botón Registrarse color incorrecto

**Ruta:** /vender/registro
**Componente:** SellerRegistrationForm.tsx
**Descripción:** Botón "Registrarse" tiene fondo rojo (#FF0000) en lugar de azul (#3B82F6)
**Impacto:** UI inconsistent, confunde a usuarios

**Steps to reproduce:**

1. Navega a https://okla.com.do/vender/registro
2. Observa botón "Registrarse"

**Esperado:** Fondo azul (#3B82F6)
**Actual:** Fondo rojo (#FF0000)

**Screenshot:** [adjunta imagen]
```

2. **Corrige el bug:**

```bash
# Abre el archivo
code frontend/web-next/src/components/seller/SellerRegistrationForm.tsx

# Busca el botón
# Encontrarás algo como: <button className="bg-red-600">Registrarse</button>
# Cambia a: <button className="bg-blue-500">Registrarse</button>

# Verifica en browser que se ve bien
```

3. **Commit y Push:**

```bash
git add frontend/web-next/src/components/seller/SellerRegistrationForm.tsx
git commit -m "fix(frontend): color botón Registrarse - cambiar rojo a azul"
git push origin main
```

4. **CI/CD automático:** GitHub Actions construye imagen Next.js y despliega a K8s

---

### E2. Bug Validación de Formulario

**Ejemplo:** Input de email no valida formato correctamente, acepta "abc" (sin @)

1. **Ubica el archivo:**

```
frontend/web-next/src/components/seller/SellerRegistrationForm.tsx
```

2. **Revisa la validación (zod schema):**

```typescript
// Busca algo como:
const schema = z.object({
  email: z.string().email("Email inválido"),
  ...
});
```

3. **Si el schema es correcto, revisa el componente:**

```typescript
// Revisa que el error se muestre:
<div>{form.formState.errors.email?.message}</div>
```

4. **Corrige:**

```typescript
// Asegúrate que el schema valida correctamente
const schema = z.object({
  email: z.string().email("Email inválido").min(5, "Email muy corto"),
  password: z.string().min(8, "Mínimo 8 caracteres")
    .regex(/[A-Z]/, "Requiere mayúscula")
    .regex(/[0-9]/, "Requiere número")
    .regex(/[^a-zA-Z0-9]/, "Requiere carácter especial"),
  ...
});
```

5. **Test en browser:**

```javascript
// DevTools Console
// Abre la página de registro
// Intenta ingresar "abc"
// Debe mostrar error inmediato: "Email inválido"
```

6. **Commit:**

```bash
git commit -m "fix(frontend): validación email en formulario seller"
```

---

### E3. Bug de Routing / Redirección

**Ejemplo:** Después de KYC submit, no redirige a `/vender/leads`, se queda en `/vender/kyc`

1. **Ubica el archivo:**

```
frontend/web-next/src/app/(main)/vender/kyc/page.tsx
```

2. **Revisa la lógica de submit:**

```typescript
const handleSubmit = async (data) => {
  try {
    const response = await submitKYC(data);
    // Después de éxito, ¿hay router.push() ?
    if (response.success) {
      router.push("/vender/leads"); // ← ESTO FALTA?
    }
  } catch (error) {
    // error handling
  }
};
```

3. **Corrige:**

```typescript
// Agrega el router.push() si falta
import { useRouter } from "next/navigation"; // ← Importar

export default function KYCPage() {
  const router = useRouter(); // ← Declarar

  const handleSubmit = async (data) => {
    try {
      const response = await submitKYC(data);
      if (response.success) {
        router.push("/vender/leads"); // ← Agregar redirección
      }
    } catch (error) {
      console.error(error);
    }
  };

  // ...
}
```

4. **Test:**

```javascript
// Completa KYC form y submit
// Verifica que redirija a /vender/leads (URL cambia)
```

5. **Commit:**

```bash
git commit -m "fix(frontend): agregar redirección post-KYC en seller flow"
```

---

### E4. Bug de Componente No Renderiza

**Ejemplo:** Banner "Completa tu KYC" no aparece en `/dealer/dashboard`

1. **Revisa si el componente está incluido en la página:**

```typescript
// frontend/web-next/src/app/(main)/dealer/dashboard/page.tsx
import KYCBanner from '@/components/shared/KYCBanner';

export default function DealerDashboardPage() {
  return (
    <div>
      {/* ¿ESTÁ EL COMPONENTE? */}
      <KYCBanner /> {/* ← Si no está, AGREGAR */}
      <DealerStats />
      ...
    </div>
  );
}
```

2. **Si está incluido, revisa la lógica de visibilidad:**

```typescript
// frontend/web-next/src/components/shared/KYCBanner.tsx
export default function KYCBanner() {
  const { user } = useAuth();

  // ¿Qué condición decide si mostrar?
  if (!user || user.is_verified) {
    return null; // No mostrar si usuario ya verificado
  }

  return <div>Completa tu verificación KYC</div>;
}
```

3. **Corrige:**

```typescript
// Si la lógica es incorrect:
export default function KYCBanner() {
  const { user } = useAuth();

  // Verifica que la condición es correcta
  // Si user.is_verified === true → no mostrar (correcto)
  // Si user.is_verified === false → mostrar (correcto)
  if (!user || user.is_verified === true) {
    return null;
  }

  return <div className="bg-yellow-100 p-4">
    Completa tu verificación KYC antes de publicar vehículos
  </div>;
}
```

4. **Verifica en BD que user.is_verified es correcto:**

```bash
# Backend check:
kubectl exec -n okla statefulset/postgres -- \
psql -U postgres -d userservice_db -c \
"SELECT id, email, is_verified, kyc_status FROM users WHERE email = 'dealer.e2e.20260223@test.com';"
```

5. **Commit:**

```bash
git commit -m "fix(frontend): mostrar banner KYC en dealer dashboard"
```

---

### E5. Generación de Commit y PR

**Patrón de Commit:**

```bash
# Pequeños fixes:
git commit -m "fix(frontend): [descripción corta y clara]"

# Ejemplos:
git commit -m "fix(frontend): validación email en seller registration"
git commit -m "fix(frontend): redirección post-KYC en seller flow"
git commit -m "fix(frontend): color botón Registrarse en dealer wizard"
git commit -m "fix(frontend): mostrar banner KYC en dashboard"
git commit -m "fix(frontend): dropdown especialidades permite múltiple en dealer KYC"
```

**Si múltiples fixes (crear PR):**

```bash
# Crear rama
git checkout -b fix/e2e-frontend-qa-20260223

# Haz commits
git commit -m "fix(frontend): validación email en seller flow"
git commit -m "fix(frontend): redirección post-KYC"
git commit -m "fix(frontend): banner KYC en dealer dashboard"

# Push a rama
git push origin fix/e2e-frontend-qa-20260223

# Abre PR en GitHub:
# Title: fix: E2E frontend QA - seller/dealer flows 20260223
# Description:
# - Validación email en seller registration
# - Redirección post-KYC submit
# - Banner KYC en dealer dashboard
# - [más fixes]
```

**CI/CD automático:**

- GitHub Actions detecta push
- Construye imagen Docker Next.js
- Corre linter (ESLint)
- Corre tests (si existen)
- Despliega a K8s (si main branch)

**Verifica en GitHub:**

```
https://github.com/gregorymorenoiem/cardealer-microservices/actions
```

Busca tu commit hash → verifica que build pasa (✅ green)

══════════════════════════════════════════════════════════════
PARTE F: ENTREGABLES FINALES
══════════════════════════════════════════════════════════════

### F1. Informe Final (Markdown)

**Archivo:** `REPORT_QA_FRONTEND_E2E_20260223.md`

**Contenido:**

```markdown
# 📋 Reporte QA Frontend E2E — Flujos Seller/Dealer

**Fecha:** 2026-02-23
**QA Tester:** [Tu nombre]
**Duración:** X minutos
**Status:** ✅ Completado / ❌ Con bloqueadores

## Resumen Ejecutivo

- Seller Flow: ✅ Funcional completo (11/11 checks)
- Dealer Flow: ✅ Funcional completo (12/12 checks)
- Seguridad: ✅ CSRF, sanitización OK
- Responsive: ✅ Desktop/Tablet/Mobile OK
- Bugs encontrados: 0 críticos, 2 menores
- Bugs arreglados: 2
- PR abierta: #123

## FLUJO SELLER

### Paso F-S1: HomePage

- ✅ Carga sin errores
- ✅ Navbar visible
- ✅ Responsive en mobile

### Paso F-S2: Registro Seller

- ✅ Formulario carga
- ✅ Validaciones funcionan
- ✅ Email format check: ✅
- ✅ Password strength: ✅
- ✅ POST exitoso → usuario creado

### [... cada paso ...]

## FLUJO DEALER

### Paso F-D1: Registro Wizard

- ✅ 5 pasos cargan sin errores
- ✅ Paso 1: validaciones OK
- ✅ Paso 2: RNC valida formato
- ✅ [...]

### [... cada paso ...]

## BUGS ENCONTRADOS

### BUG-FE001: Menor

**Ruta:** /vender/registro
**Componente:** SellerRegistrationForm.tsx
**Descripción:** Botón "Registrarse" margin incorrecto, no centrado
**Status:** ✅ FIXED (commit abc123)

### BUG-FE002: Menor

**Ruta:** /dealer/dashboard
**Componente:** DealerDashboard.tsx
**Descripción:** Banner KYC no desaparece después de aprobación
**Status:** ✅ FIXED (commit def456)

## CHECKLIST FINAL (31 items)

### Seller Flow (11/11 ✅)

- [x] HomePage sin errores
- [x] Registro validaciones
- [x] Login JWT OK
- [x] KYC 5 pasos
- [x] Publicar vehículos
- [x] Búsqueda publica
- [x] Detalle vehículo
- [x] Responsive OK
- [x] Accesibilidad OK
- [x] Performance < 3s
- [x] No errors console

### Dealer Flow (12/12 ✅)

- [x] Wizard 5 pasos
- [x] Dashboard
- [x] KYC 6 pasos
- [x] Publicar 3 vehículos
- [x] Admin panel
- [x] Admin detalle
- [x] Banner KYC
- [x] Responsive
- [x] Accesibilidad
- [x] Performance
- [x] Wizard persist data
- [x] No errors console

### Seguridad (5/5 ✅)

- [x] CSRF tokens
- [x] Input sanitization
- [x] URL sanitization
- [x] No passwords en storage
- [x] No JWT en query params

### Shared Components (3/3 ✅)

- [x] Navbar OK
- [x] Footer OK
- [x] Notifications OK

## COMMITS Y PR

| Commit | Mensaje                                     | Archivo                    |
| ------ | ------------------------------------------- | -------------------------- |
| abc123 | fix(frontend): button registrarse alignment | SellerRegistrationForm.tsx |
| def456 | fix(frontend): kyc banner persistence       | DealerDashboard.tsx        |

**PR:** [#123 - E2E Frontend QA](https://github.com/gregorymorenoiem/cardealer-microservices/pull/123)

## CONCLUSIÓN

✅ **Frontend Ready for Production**

Todos los flujos funcionales, validaciones OK, seguridad verificada, responsive diseño,
accesibilidad en línea. 2 bugs menores reportados y arreglados.

**Recomendación:** Merge a main + deploy a producción.

---

**Reportado por:** QA Team
**Revisado por:** [PM/Tech Lead]
```

### F2. Screenshots / Videos (Opcional)

- Captura de cada paso crítico (registro, KYC, publicar, etc.)
- Video de 30 segundos: flujo completo seller (registro → vehículo visible)
- Video de 30 segundos: flujo completo dealer (registro → 3 vehículos)

### F3. Playwright E2E Tests (Si aplica)

**Archivo:** `frontend/web-next/tests/e2e/guest-flows.spec.ts`

```typescript
import { test, expect } from "@playwright/test";

test.describe("E2E: Guest to Seller Flow", () => {
  test("seller registration -> kyc -> publish vehicle", async ({ page }) => {
    // Step 1: Registro
    await page.goto("https://okla.com.do/vender/registro");
    await page.fill('input[type="email"]', "seller.e2e.20260223@test.com");
    await page.fill('input[name="password"]', "Test1234!@#");
    // ... more steps

    // Step 2: KYC
    // ... 5 pasos

    // Step 3: Publicar vehículo
    // ... llenar form

    // Step 4: Verificar visible
    await page.goto("https://okla.com.do/buscar");
    // ... buscar vehículo

    // Assertions
    expect(page).toHaveTitle(/Búsqueda/);
  });
});

test.describe("E2E: Guest to Dealer Flow", () => {
  test("dealer registration -> kyc -> publish 3 vehicles", async ({ page }) => {
    // Similar a seller pero con dealer flow
  });
});
```

**Ejecutar:**

```bash
cd frontend/web-next
pnpm test:e2e:guest-flows
```

══════════════════════════════════════════════════════════════
PARTE G: ATAJOS Y TIPS
══════════════════════════════════════════════════════════════

### G1. DevTools Shortcuts

```javascript
// En DevTools Console, testing del frontend:

// Ver user actual
console.log(localStorage.getItem("authToken"));

// Limpiar localStorage (reset auth)
localStorage.clear();

// Ver todas las requests de API
// DevTools → Network → filtrar por XHR/Fetch

// Simular error de red
// DevTools → Network → Throttle → Offline
```

### G2. Testing de Responsive

```bash
# DevTools → Toggle device toolbar (Ctrl+Shift+M)
# Ir a device presets:
# - iPhone 12 (390x844)
# - iPad (768x1024)
# - Desktop (1920x1080)
```

### G3. Performance Testing

```bash
# DevTools → Performance tab
# 1. Haz clic Record
# 2. Navega página
# 3. Haz clic Stop
# 4. Analiza: FCP, LCP, CLS, FID
```

### G4. Accessibility Testing

```bash
# DevTools → Lighthouse
# 1. Category: Accessibility
# 2. Analyze page load
# 3. Revisa problemas (labels, contrast, etc.)
```

### G5. Keyboard Navigation Testing

```
# Tab: navegar entre elementos
# Shift+Tab: retroceder
# Enter: activar botón/submit
# Space: check checkbox, activar button
# Arrow keys: navegar dentro de dropdown/menu
# Escape: cerrar modal/dropdown
```

══════════════════════════════════════════════════════════════
EMPIEZA AHORA: FLUJO RECOMENDADO
══════════════════════════════════════════════════════════════

1. **Familiarización** (5 min)
   - Lee Parte A (contexto del proyecto)
   - Entiende rutas, stack, estructura

2. **SELLER FLOW** (20-30 min)
   - Ejecuta PASO F-S1 → F-S8 secuencialmente
   - Completa checklist
   - Documenta hallazgos

3. **DEALER FLOW** (20-30 min)
   - Ejecuta PASO F-D1 → F-D6
   - Completa checklist
   - Documenta hallazgos

4. **BUG FIXING** (10-20 min)
   - Si hay bugs, corrígelos (Parte E)
   - Crea commits
   - Push a main

5. **REPORT** (10 min)
   - Escribe informe final (Parte F1)
   - Adjunta screenshots
   - Crea PR (si múltiples fixes)

**TOTAL ESTIMADO:** 75-100 minutos

══════════════════════════════════════════════════════════════

✅ **LISTO PARA QA FRONTEND**

Usa este prompt como referencia paso a paso. Si necesitas ayuda o clarificación, revisa
la Parte A (contexto) o consulta el equipo.

¡A testear! 🚀
