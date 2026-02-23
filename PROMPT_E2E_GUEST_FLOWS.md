# 🤖 PROMPT: Auditoria E2E - Flujos Production Guest → Seller / Dealer

Eres un agente de ingeniería con acceso al repositorio cardealer-microservices (branch: main,
owner: gregorymorenoiem) y al cluster Kubernetes de producción DOKS (okla-cluster, namespace: okla).
Tu misión: ejecutar, verificar y corregir los flujos completos para que un usuario guest se convierta
en **SELLER** verificado Y en **DEALER** verificado en producción (https://okla.com.do), validando
cada etapa con evidencia.

Si encuentras errores, corrígelos, commitéalos y abre un PR. Al terminar entrega un informe markdown
completo con todo lo ejecutado.

══════════════════════════════════════════════════════════════
CONTEXTO DE ARQUITECTURA — LEE ESTO ANTES DE EMPEZAR
══════════════════════════════════════════════════════════════

**NOTA CRÍTICA:** Los flujos de DEALER y SELLER son completamente independientes.

- SELLERS: Personas físicas (flujo individual, KYC simple)
- DEALERS: Personas jurídicas/Empresas (flujo empresarial con verificación del representante legal)
  No comparten rutas, bases de datos, validaciones ni flujos KYC.

- Frontend (producción): https://okla.com.do — el ÚNICO servicio expuesto a internet.
- BFF Pattern: Browser → https://okla.com.do/api/* → Next.js rewrite → gateway:8080 (interno K8s)
  → microservicios. NO existe api.okla.com.do.
- Kubernetes: cluster okla-cluster | namespace okla | 2× s-4vcpu-8gb nodes
- DB producción: DO Managed PostgreSQL
  Host: okla-db-do-user-31493168-0.g.db.ondigitalocean.com | Port: 25060 | sslmode=require
  (accesible también via: kubectl exec -n okla deploy/authservice -- psql \"$CONN_STRING\")
- In-cluster PostgreSQL fallback: kubectl exec -n okla statefulset/postgres -- psql -U postgres

- Rutas relevantes en el repo para SELLERS (frontend/web-next/src/app):
  /vender/registro/page.tsx → Registro de vendedor individual
  /vender/publicar/page.tsx → Publicar vehículos como vendedor
  /vender/mis-vehiculos/page.tsx → Gestión de vehículos (si existe)
  /vender/leads/page.tsx → Gestión de inquietudes/leads

- Rutas relevantes en el repo para DEALERS (frontend/web-next/src/app):
  /(main)/dealer/registro/page.tsx → Registro de dealer (wizard) — para usuarios autenticados
  /(auth)/registro/dealer/page.tsx → Registro de dealer para guests (entrada principal)
  /dealer/verificacion/page.tsx → Flujo KYC empresarial (6 pasos)
  /dealer/inventario/page.tsx → Gestión de inventario y vehículos (si existe)
  /dealer/dashboard/page.tsx → Panel de control del distribuidor
  /admin/dealers/page.tsx → Cola de verificación de dealers (admin)
  /admin/dealers/[id]/page.tsx → Detalle de solicitud KYC de dealer

- Admin panel URL: https://okla.com.do/admin
- Servicios activos en K8s: authservice, userservice, kycservice, dealerservice (si existe como microservicio),
  notificationservice, vehiclessaleservice, billingservice, gateway, frontend-web (todos en puerto 8080)
- CI/CD: push a main → GitHub Actions (smart-cicd.yml) → build imagen → deploy a K8s automáticamente.
  Para aplicar un fix de código: haz el cambio, commitea, push a main (o PR si es significativo).

**RESTRICCIONES DE SEGURIDAD EN PRODUCCIÓN:**

- No pueden crearse más de 3 usuarios de prueba por día (rate limiting)
- Email debe ser único en la DB (no puede reutilizarse)
- Tokens JWT expiran en 15 minutos; refresh tokens en 7 días
- Todos los requests deben incluir header CSRF si es POST/PUT/DELETE
- No puede confirmarse email automáticamente; requiere token de verificación (workaround: DB UPDATE)

══════════════════════════════════════════════════════════════
DATOS DE PRUEBA — USA EXACTAMENTE ESTOS
══════════════════════════════════════════════════════════════

Reemplaza YYYYMMDD con la fecha de hoy (ej: 20260223).

### FLUJO SELLER

- Email: seller.e2e.YYYYMMDD@test.com
- Password: Test1234!@#
- Nombre: Test
- Apellido: Seller
- Teléfono: 8091234567
- Cédula: 001-1234567-8
- Fecha de nacimiento: 1990-01-15
- Dirección: Calle Principal #100, Santo Domingo
- Provincia: Distrito Nacional
- País: República Dominicana

**Vehículo de prueba (Seller):**

- Título: \"Toyota Corolla 2022 - Test Seller YYYYMMDD\"
- Marca: Toyota | Modelo: Corolla | Año: 2022
- Precio: 900,000 DOP
- Condición: Usado
- Kilometraje: 35,000 km
- Combustible: Gasolina
- Transmisión: Automática
- Color: Blanco

### FLUJO DEALER

- Email: dealer.e2e.YYYYMMDD@test.com
- Password: Test1234!@#
- Nombre representante: Test
- Apellido representante: Dealer

- **EMPRESA:**
  - Nombre comercial: AutoMotriz E2E Distribuidor YYYYMMDD
  - RNC: 101-234567-8
  - Tipo de entidad: Sociedad Anónima (S.A.)
  - Año de fundación: 2020
  - Teléfono principal: 8091234567
  - Email: dealer.e2e.YYYYMMDD@test.com
  - Dirección: Calle Principal #500, Piantini, Santo Domingo, DN 10204
  - Empleados: 5-10
  - Especialidades: Importación, Venta mayorista
- **REPRESENTANTE LEGAL:**
  - Nombre completo: Test Dealer
  - Cédula: 001-1234567-8
  - Cargo: Gerente General
  - Teléfono: 8091234567
  - Email: dealer.e2e.YYYYMMDD@test.com

**Vehículos de prueba (Dealer):**

- 2× Toyota/Corolla/2022, precios: 1,200,000 / 1,350,000 DOP
- 1× Honda/CR-V/2023, precio: 1,800,000 DOP

### USUARIOS ADMIN

- Email admin: admin@okla.local
- Password admin: Admin123!@#
- URL admin: https://okla.com.do/admin/login
- URL base de APIs: https://okla.com.do/api

══════════════════════════════════════════════════════════════
PASOS A EJECUTAR (FLUJO SELLER)
══════════════════════════════════════════════════════════════

────────────────────────────────
PASO S1 — Verificar salud del cluster de producción
────────────────────────────────

Antes de comenzar, verifica que el entorno está sano.

1. Verifica que los servicios críticos estén Running

bash
kubectl get pods -n okla | grep -E \"authservice|userservice|kycservice|notificationservice|vehiclessaleservice|gateway|frontend-web\"

Todos deben estar en estado Running. Si ves CrashLoopBackOff, ImagePullBackOff o Error:

bash
kubectl logs -n okla deploy/<nombre-del-servicio> --tail=50
kubectl describe pod -n okla <nombre-del-pod>

Si necesitas refrescar el secret de registry:

bash
TOKEN=$(gh auth token)
kubectl delete secret registry-credentials -n okla
kubectl create secret docker-registry registry-credentials \\
  --docker-server=ghcr.io --docker-username=gregorymorenoiem \\
  --docker-password=$TOKEN -n okla

2. Verifica el health check global

bash
curl -s https://okla.com.do/api/health | jq .

Debe retornar un JSON con status: \"Healthy\" o similar.

3. Verifica que todas las migraciones de bases de datos estén aplicadas

bash

# authservice_db

kubectl exec -n okla statefulset/postgres -- psql -U postgres -d authservice_db -c \"\\\\dt\" | head -5

# userservice_db

kubectl exec -n okla statefulset/postgres -- psql -U postgres -d userservice_db -c \"\\\\dt\" | head -5

# kycservice_db

kubectl exec -n okla statefulset/postgres -- psql -U postgres -d kycservice_db -c \"\\\\dt\" | head -5

Si alguna base de datos no tiene las tablas esperadas, sigue las acciones correctivas en el contexto anterior.

────────────────────────────────
PASO S2 — Registro del usuario seller
────────────────────────────────

Navega a https://okla.com.do/vender/registro con una sesión de browser limpia (incógnito/privada).

Completa el formulario de registro de vendedor:

- Email: seller.e2e.YYYYMMDD@test.com
- Password: Test1234!@#
- Confirmar Password: Test1234!@#
- Nombre: Test
- Apellido: Seller
- Teléfono: 8091234567
- Aceptar términos y condiciones: ✓
- Haz clic en \"Registrarse\" o \"Crear cuenta\"

Verifica en pantalla que aparezca un mensaje de confirmación o redirección a verificación de email.

────────────────────────────────
PASO S3 — Verificar email del seller en DB de producción
────────────────────────────────

Conéctate a la DB y confirma que el usuario seller existe.

bash

# Verificar usuario creado en AspNetUsers (AuthService)

kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d authservice_db -c \\
\"SELECT id, email, email_confirmed, created_at FROM \\\"AspNetUsers\\\"
WHERE email = 'seller.e2e.YYYYMMDD@test.com';\"

Si email_confirmed = false, actualízalo manualmente (workaround de prod):

bash
kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d authservice_db -c \\
\"UPDATE \\\"AspNetUsers\\\" SET \\\"EmailConfirmed\\\" = true
WHERE email = 'seller.e2e.YYYYMMDD@test.com';\"

────────────────────────────────
PASO S4 — Login como vendedor
────────────────────────────────

Con el email confirmado, inicia sesión en https://okla.com.do/login:
Email: seller.e2e.YYYYMMDD@test.com
Password: Test1234!@#

Verifica que obtienes un JWT válido (devuelto en la respuesta POST /api/auth/login):

bash
curl -s -X POST https://okla.com.do/api/auth/login \\
-H \"Content-Type: application/json\" \\
-d '{\"email\":\"seller.e2e.YYYYMMDD@test.com\",\"password\":\"Test1234!@#\"}' | jq .

La respuesta debe incluir:

- access_token (JWT válido)
- token_type: \"Bearer\"
- expires_in (segundos)
- user: { id, email, roles, ... }

────────────────────────────────
PASO S5 — Completar perfil KYC del vendedor
────────────────────────────────

Navega a https://okla.com.do/vender/kyc (o similar, según frontend routes).

Completa el formulario KYC individual (entityType=1):
PASO 1 — Información Personal: - Nombre completo: Test - Cédula: 001-1234567-8 - Fecha de nacimiento: 1990-01-15 - Teléfono: 8091234567 - Email: seller.e2e.YYYYMMDD@test.com - Haz clic en \"Continuar\"

PASO 2 — Domicilio: - Dirección: Calle Principal #100 - Provincia: Distrito Nacional - Ciudad: Santo Domingo - Código Postal: 10100 - País: República Dominicana - Haz clic en \"Continuar\"

PASO 3 — Documentos: - Sube foto de cédula (frente): JPG/PNG, mínimo 100KB - Sube foto de cédula (reverso): JPG/PNG, mínimo 100KB - Haz clic en \"Continuar\"

PASO 4 — Verificación Biométrica: - Completa el reto liveness (parpadear, sonreír, girar cabeza) - Si falla por timeout o error técnico, reintentar o reportar bug - Haz clic en \"Continuar\"

PASO 5 — Revisión y Envío: - Verifica todos los datos - Haz clic en \"Enviar para revisión\"

Verifica en DB que se creó el KYC profile:

bash
kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d kycservice_db -c \\
\"SELECT id, user_id, status, submitted_at, created_at
FROM kyc_profiles
WHERE user_id = (SELECT id::text FROM users WHERE email = 'seller.e2e.YYYYMMDD@test.com')
ORDER BY created_at DESC LIMIT 1;\"

Status debe ser 4 (UnderReview) o 5 (Approved).

────────────────────────────────
PASO S6 — Admin: Revisar y aprobar KYC del vendedor
────────────────────────────────

Cierra la sesión del vendedor y entra con admin:
URL: https://okla.com.do/admin/login
Email: admin@okla.local
Password: Admin123!@#

Navega a https://okla.com.do/admin/kyc (o ruta equivalente).

Busca la solicitud del usuario seller.e2e.YYYYMMDD@test.com.
Haz clic para ver el detalle.

Revisa:

- ✓ Información personal (cédula, nombre, fecha nacimiento)
- ✓ Domicilio
- ✓ Documentos de cédula (frente/reverso legibles)
- ✓ Validación de liveness (score biométrico acceptable)

Haz clic en \"Aprobar\" o \"Approve\" y confirma.

Verifica via API:

bash
ADMIN_TOKEN=$(curl -s -X POST https://okla.com.do/api/auth/login \\
-H \"Content-Type: application/json\" \\
-d '{\"email\":\"admin@okla.local\",\"password\":\"Admin123!@#\"}' | jq -r '.data.access_token // .access_token')

KYC_ID=$(kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d kycservice_db -t -c \\
\"SELECT id FROM kyc_profiles
WHERE user_id = (SELECT id::text FROM users WHERE email = 'seller.e2e.YYYYMMDD@test.com')
ORDER BY created_at DESC LIMIT 1;\" | tr -d ' ')

# Verificar estado aprobado

kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d kycservice_db -c \\
\"SELECT id, status, reviewed_at, reviewed_by FROM kyc_profiles WHERE id = '$KYC_ID';\"

Status debe ser 5 (Approved).

────────────────────────────────
PASO S7 — Publicar vehículo como vendedor
────────────────────────────────

Con sesión de vendedor activa, navega a https://okla.com.do/vender/publicar.

Completa el formulario de creación de vehículo:

- Título: \"Toyota Corolla 2022 - Test Seller YYYYMMDD\"
- Marca: Toyota
- Modelo: Corolla
- Año: 2022
- Precio: 900000
- Moneda: DOP
- Condición: Usado
- Kilometraje: 35000
- Combustible: Gasolina
- Transmisión: Automática
- Color: Blanco
- Descripción: \"Vehículo de prueba E2E, no disponible para compra\"
- Sube al menos 3 fotos de prueba
- Haz clic en \"Publicar\" o \"Guardar y publicar\"

Verifica en DB que el vehículo fue creado:

bash
SELLER_ID=$(kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d userservice_db -t -c \\
\"SELECT id FROM users WHERE email = 'seller.e2e.YYYYMMDD@test.com';\" | tr -d ' ')

kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d vehiclessaleservice_db -c \\
\"SELECT id, title, price, status, seller_id, created_at
FROM vehicles
WHERE seller_id = '$SELLER_ID'
ORDER BY created_at DESC LIMIT 1;\"

Status debe ser 2 (Active/Published).

────────────────────────────────
PASO S8 — Verificar visibilidad pública del vehículo
────────────────────────────────

Sin autenticación, navega a https://okla.com.do/buscar (o home).

Busca el vehículo por título: \"Toyota Corolla 2022 - Test Seller YYYYMMDD\"

Verifica que:

- El vehículo es visible en el listado
- El precio es correcto (900,000 DOP)
- Las fotos se cargan correctamente
- La información del vendedor se muestra (teléfono, email, ubicación)

────────────────────────────────
PASO S9 — Auditoría de bugs encontrados (SELLER)
────────────────────────────────

Por cada error encontrado en los pasos anteriores:

a) Identifica el archivo afectado (ruta completa relativa al repo)
b) Describe la causa raíz en 1-2 líneas
c) Aplica el fix mínimo necesario
d) Si es backend: verifica que el servicio recoge el cambio (push → CI/CD)
Si es frontend: igual, push → CI/CD despliega automáticamente
e) Crea un commit por cada fix:
fix(seller-flow): <descripción corta del problema>
g) Agrupa todos los fixes en un PR único:
fix: E2E seller flow production audit YYYYMMDD

══════════════════════════════════════════════════════════════
PASOS A EJECUTAR (FLUJO DEALER)
══════════════════════════════════════════════════════════════

────────────────────────────────
PASO D1 — Verificar salud del cluster (idem S1)
────────────────────────────────

(Ver PASO S1 — es idéntico)

────────────────────────────────
PASO D2 — Registro de nuevo distribuidor (dealer) via wizard
────────────────────────────────

Navega a https://okla.com.do/registro/dealer (ruta para guests) o https://okla.com.do/dealer/registro (si ya autenticado).

Completa el wizard de registro de distribuidor (4-5 pasos según dealer/registro-distribuidor/page.tsx):

PASO WIZARD 1 — Información de Contacto Principal: - Nombre completo del representante: Test - Apellido: Dealer - Email: dealer.e2e.YYYYMMDD@test.com - Teléfono principal: 8091234567 - Password: Test1234!@# - Confirmar Password: Test1234!@# - Aceptar términos y condiciones: ✓ - Haz clic en \"Continuar\"

PASO WIZARD 2 — Información de la Empresa: - Razón social: AutoMotriz E2E Distribuidor YYYYMMDD - RNC: 101-234567-8 - Tipo de sociedad: Sociedad Anónima (S.A.) - Año de fundación: 2020 - Número de registro mercantil: 123456 (si aplica) - Email comercial: dealer.e2e.YYYYMMDD@test.com - Teléfono comercial: 8091234567 - Descripción del negocio: \"Distribuidor mayorista e importador de vehículos\" - Haz clic en \"Continuar\"

PASO WIZARD 3 — Domicilio y Ubicación: - Dirección principal: Calle Principal #500, Piantini - Provincia: Distrito Nacional - Ciudad: Santo Domingo - Código Postal: 10204 - País: República Dominicana - Haz clic en \"Continuar\"

PASO WIZARD 4 — Especialidades y Capacidad: - Especialidades: selecciona al menos 2-3 (Importación, Venta mayorista, Vehículos premium) - Cantidad aproximada de vehículos en inventario: 50-100 - Número de empleados: 5-10 - Haz clic en \"Continuar\"

PASO WIZARD 5 — Revisión y Confirmación: - Verifica todos los datos ingresados - Revisa términos y políticas - Haz clic en \"Finalizar registro\"

Verifica en DB que el usuario dealer y su DealerProfile fueron creados:

bash
kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d userservice_db -c \\
\"SELECT id, email, account_type, is_verified FROM users
WHERE email = 'dealer.e2e.YYYYMMDD@test.com';\"

────────────────────────────────
PASO D3 — Verificar email y confirmar
────────────────────────────────

(Idem PASO S3 — actualizar email_confirmed si es necesario)

────────────────────────────────
PASO D4 — Login como dealer
────────────────────────────────

(Idem PASO S4)

────────────────────────────────
PASO D5 — Verificación KYC empresarial (/dealer/verificacion)
────────────────────────────────

Con dealer logueado, navega a https://okla.com.do/dealer/verificacion.

Completa el flujo de verificación KYC empresarial (6 pasos):

PASO KYC 1 — Información Legal de la Empresa: - Razón social: AutoMotriz E2E Distribuidor YYYYMMDD - RNC: 101-234567-8 - Tipo de entidad: Sociedad Anónima - Año de constitución: 2020 - Número de registro mercantil: 123456 - Haz clic en \"Continuar\"

PASO KYC 2 — Información del Representante Legal: - Nombre completo: Test - Apellido: Dealer - Cédula: 001-1234567-8 - Cargo: Gerente General - Teléfono: 8091234567 - Email: dealer.e2e.YYYYMMDD@test.com - Haz clic en \"Continuar\"

PASO KYC 3 — Domicilio e Información Fiscal: - Dirección principal: Calle Principal #500, Piantini - Provincia: Distrito Nacional - Ciudad: Santo Domingo - Código Postal: 10204 - Haz clic en \"Continuar\"

PASO KYC 4 — Documentos Fiscales y Legales de la Empresa: - Sube copia del RNC: imagen JPG/PNG, mínimo 100KB - Sube acta constitutiva o registro mercantil: imagen - Sube comprobante de domicilio reciente: imagen - Sube certificado de cumplimiento fiscal (opcional): imagen - Haz clic en \"Continuar\"

PASO KYC 5 — Documentos y Verificación Biométrica del Representante Legal: - Sube imagen de cédula del representante (frente): JPG/PNG, mínimo 100KB - Sube imagen de cédula del representante (reverso): JPG/PNG, mínimo 100KB - Completa el liveness challenge (parpadear, sonreír, girar cabeza, etc.) \* Si falla por error técnico, reintentar - Verifica que el puntaje biométrico sea acceptable - Haz clic en \"Continuar\"

PASO KYC 6 — Revisión y Envío: - Verifica todos los datos personales, corporativos, documentos y biometría - Confirma que todos los documentos sean legibles y completos - Haz clic en \"Enviar para revisión\"

Verifica en DB:

bash
kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d kycservice_db -c \\
\"SELECT id, user_id, status, submitted_at, entity_type
FROM kyc_profiles
WHERE user_id = (SELECT id::text FROM users WHERE email = 'dealer.e2e.YYYYMMDD@test.com')
ORDER BY created_at DESC LIMIT 1;\"

entity_type debe ser 2 (Business), status debe ser 4 (UnderReview).

────────────────────────────────
PASO D6 — Admin: Revisar y aprobar solicitud KYC del dealer
────────────────────────────────

Cierra sesión dealer, entra con admin:
Email: admin@okla.local
Password: Admin123!@#

Ve a https://okla.com.do/admin/dealers (o panel de dealers).

Busca la solicitud del dealer.e2e.YYYYMMDD@test.com.
Haz clic para ver el panel de detalle.

Revisa:

- ✓ Información legal de la empresa (RNC, tipo de entidad, registro mercantil)
- ✓ Información del representante legal (nombre, cédula, cargo)
- ✓ Documentos de empresa (RNC, acta, comprobante domicilio) — legibles
- ✓ Documentos del representante (cédula frente/reverso) — legibles
- ✓ Puntaje biométrico del representante (score acceptable)

Haz clic en \"Aprobar\" y confirma.

Verifica:

bash
KYC_ID=$(kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d kycservice_db -t -c \\
\"SELECT id FROM kyc_profiles
WHERE user_id = (SELECT id::text FROM users WHERE email = 'dealer.e2e.YYYYMMDD@test.com')
ORDER BY created_at DESC LIMIT 1;\" | tr -d ' ')

kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d kycservice_db -c \\
\"SELECT id, status, reviewed_at, reviewed_by FROM kyc_profiles WHERE id = '$KYC_ID';\"

Status debe ser 5 (Approved).

────────────────────────────────
PASO D7 — Verificar asignación de plan de suscripción
────────────────────────────────

Verifica que el dealer tiene un plan de suscripción automáticamente asignado:

bash
kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d billingservice_db -c \\
\"SELECT id, user_id, plan_type, status, created_at
FROM dealer_subscriptions
WHERE user_id = (SELECT id::text FROM users WHERE email = 'dealer.e2e.YYYYMMDD@test.com')
ORDER BY created_at DESC LIMIT 1;\"

Si no existe suscripción, verifica los logs de BillingService:

bash
kubectl logs deploy/billingservice -n okla --tail=50 | grep -i \"subscription\\|kyc_approved\"

────────────────────────────────
PASO D8 — Verificar que banner KYC desaparece
────────────────────────────────

Como dealer (dealer.e2e.YYYYMMDD@test.com), inicia sesión:
URL: https://okla.com.do/login
Email: dealer.e2e.YYYYMMDD@test.com
Password: Test1234!@#

Navega a https://okla.com.do/dealer/dashboard o home.

Confirma que el banner \"Completa tu verificación\" ya NO está visible.

Si el banner sigue apareciendo, investiga en el frontend:
frontend/web-next/src/components/kyc-banner.tsx
frontend/web-next/src/components/dealer/dealer-verification-banner.tsx

Revisa que la lógica de visibilidad esté usando kyc_status == Approved o is_verified == true:

bash
kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d userservice_db -c \\
\"SELECT id, email, is_verified, kyc_status, account_type FROM users
WHERE email = 'dealer.e2e.YYYYMMDD@test.com';\"

────────────────────────────────
PASO D9 — Publicar vehículos como dealer
────────────────────────────────

Como dealer (dealer.e2e.YYYYMMDD@test.com) autenticado, navega a https://okla.com.do/dealer/inventario
(o /vender/publicar si es ruta unificada).

Crea 3 vehículos:

**Vehículo 1:**

- Título: \"Toyota Corolla 2022 - Dealer YYYYMMDD #1\"
- Marca: Toyota | Modelo: Corolla | Año: 2022
- Precio: 1,200,000 DOP
- [otros campos standard]
- Haz clic en \"Publicar\"

**Vehículo 2:**

- Título: \"Toyota Corolla 2022 - Dealer YYYYMMDD #2\"
- Precio: 1,350,000 DOP
- [idem]

**Vehículo 3:**

- Título: \"Honda CR-V 2023 - Dealer YYYYMMDD\"
- Marca: Honda | Modelo: CR-V | Año: 2023
- Precio: 1,800,000 DOP
- [idem]

Verifica en DB:

bash
DEALER_ID=$(kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d userservice_db -t -c \\
\"SELECT id FROM users WHERE email = 'dealer.e2e.YYYYMMDD@test.com';\" | tr -d ' ')

kubectl exec -n okla statefulset/postgres -- \\
psql -U postgres -d vehiclessaleservice_db -c \\
\"SELECT id, title, price, status, seller_id FROM vehicles
WHERE seller_id = '$DEALER_ID'
ORDER BY created_at DESC LIMIT 5;\"

Todos deben tener status = 2 (Active).

────────────────────────────────
PASO D10 — Auditoría de bugs encontrados (DEALER)
────────────────────────────────

(Idem PASO S9, pero para bugs encontrados en flujo dealer)

══════════════════════════════════════════════════════════════
ERRORES CONOCIDOS A VALIDAR
══════════════════════════════════════════════════════════════

**SELLER Flow:**

- [ ] BUG-S001: Notificaciones de aprobación KYC no llegan (RabbitMQ event no consumido)
- [ ] BUG-S002: Email delivery puede fallar en dev (Resend API key inválida)

**DEALER Flow:**

- [x] BUG-D001: JWT SecretKey en DealerManagementService (FIXED en commit 7fd97d55)
- [ ] BUG-D002: Notificaciones KYC Approved no enviadas
- [ ] BUG-D003: POST /api/vehicles/{id}/images retorna 500 (workaround: pasar images en body)
- [ ] BUG-D004: GET /api/billing/subscriptions retorna 405 (Ocelot route issue)
- [ ] BUG-D005: billingservice DB sin migraciones (schema vacía)

Si encuentras estos bugs, documenta:

- HTTP status code
- Response body (error message)
- Backend logs: kubectl logs deploy/<service> --tail=100
- Fix propuesto y commit hash

══════════════════════════════════════════════════════════════
CHECKLIST FINAL — VERIFICA CADA PUNTO Y DOCUMENTA EVIDENCIA
══════════════════════════════════════════════════════════════

### SELLER Flow (11 puntos)

- [ ] 1. Pod health: authservice, userservice, kycservice, vehiclessaleservice Running
- [ ] 2. Usuario seller creado: login exitoso devuelve JWT
- [ ] 3. Email verificado: AspNetUsers.EmailConfirmed = true
- [ ] 4. KYC profile (seller): existe en kycservice_db con status = Approved, entityType = 1
- [ ] 5. Documentos cédula: frente/reverso en kyc_documents
- [ ] 6. Biometría del seller: liveness score acceptable
- [ ] 7. Vehículo publicado: existe en vehiclessaleservice_db con status = Active
- [ ] 8. Vehículo visible públicamente: accesible sin auth en /buscar
- [ ] 9. Notificación enviada: existe en notificationservice_db para aprobación
- [ ] 10. Banner KYC desaparecido: no visible en home después de approve
- [ ] 11. Flujo E2E completo: <X minutos desde registro hasta vehículo activo

### DEALER Flow (14 puntos)

- [ ] 1. Pod health: todos los servicios Running
- [ ] 2. Usuario dealer creado: login exitoso
- [ ] 3. Email verificado: AspNetUsers.EmailConfirmed = true
- [ ] 4. DealerProfile creado: RNC, razón social almacenados
- [ ] 5. KYC profile (dealer): status = Approved, entityType = 2, entity_type = \"Business\"
- [ ] 6. Documentos empresa: RNC, acta, comprobante en kyc_documents
- [ ] 7. Documentos representante: cédula frente/reverso en kyc_documents
- [ ] 8. Biometría del representante: liveness score acceptable
- [ ] 9. Suscripción asignada: existe en billingservice_db (o falla documented)
- [ ] 10. Vehículos publicados: 3 vehicles con status = Active, seller_id correcto
- [ ] 11. Vehículos visibles públicamente: accesibles en /buscar
- [ ] 12. Banner KYC desaparecido: no visible en /dealer/dashboard post-approve
- [ ] 13. Admin approve funciona: status cambia de 4 → 5 en kycservice_db
- [ ] 14. Flujo E2E completo: <X minutos desde wizard hasta 3 vehículos activos

### Production Readiness (Ambos flujos)

- [ ] ✅ Core endpoints working (auth, kyc, vehicles)
- [ ] ✅ Auth/JWT flow secured
- [ ] ✅ CORS headers correct
- [ ] ✅ CSRF protection in place
- [ ] [ ] Notifications sent (2 bugs documented if not)
- [ ] [ ] Billing features accessible (bugs documented if not)
- [ ] [ ] Image uploads working (bugs documented if not)

══════════════════════════════════════════════════════════════
CORRECCIONES DE LUGAR
══════════════════════════════════════════════════════════════

Si encuentras bugs, corrígelos siguiendo EXACTAMENTE estas reglas:

**CÓDIGO:**

1. Haz cambio minimal (no refactorices código no relacionado)
2. Crea commit: `git commit -m \"fix(<servicio>): descripción corta\"`
3. Push a main: `git push origin main` (o PR si significativo)
4. Espera a que CI/CD despliegue: observe en https://github.com/gregorymorenoiem/cardealer-microservices/actions

**K8s RUNTIME:**

- Si falla un pod post-fix: `kubectl rollout restart deploy/<service> -n okla`
- Verifica: `kubectl rollout status deploy/<service> -n okla`
- Logs: `kubectl logs -f deploy/<service> -n okla`

**Reglas críticas:**

- ❌ NO uses CreateBootstrapLogger() + UseStandardSerilog() simultáneamente
- ❌ Health checks en /health deben excluir checks con tag \"external\"
- ❌ Todas las interfaces inyectadas en Program.cs DEBEN estar registradas
- ❌ Si cambias queue RabbitMQ args, DELETE la queue vieja primero
- ❌ Docker build cache → limpia si imágenes no reflejan cambios:
  gh cache list --key \"Linux-buildx-<servicio>\" | awk '{print $1}' | xargs -I{} gh cache delete {}

══════════════════════════════════════════════════════════════
ENTREGABLES FINALES
══════════════════════════════════════════════════════════════

1. **Informe Markdown** (nuevo o actualizado):
   - Archivo: `REPORT_E2E_GUEST_FLOWS_PROD_YYYYMMDD.md`
   - Contenido:
     - Fecha y hora de ejecución (start/end)
     - SELLER FLOW: cada paso, ✅ exitoso / ❌ fallido + evidencia (SQL, curl, logs)
     - DEALER FLOW: idem
     - Lista de bugs encontrados: archivo, causa, fix aplicado/pendiente
     - Tabla de commits y enlace a PR (si aplica)
     - Tiempo total por flujo
     - Observaciones críticas (RNC validation, liveness biometría, etc.)

2. **PR (si hay fixes):**
   - Rama: `fix/e2e-guest-flows-prod-YYYYMMDD`
   - Título: `fix: E2E guest → seller/dealer flow production audit YYYYMMDD`
   - Descripción: enlace al informe + lista de bugs fixed vs open
   - Commits: uno por bug, siguiendo patrón `fix(<servicio>): descripción`

3. **Playwright E2E (opcional, si no existe):**
   - Archivo: `frontend/web-next/tests/e2e/guest-to-seller-dealer.spec.ts`
   - Cubre: ambos flujos completos (seller → vehicle, dealer → 3 vehicles)
   - Comando: `pnpm test:e2e:guest-flows`

══════════════════════════════════════════════════════════════
TIMELINE ESTIMADO
══════════════════════════════════════════════════════════════

- Cluster health check: 3 min
- SELLER flow (S1-S9): 12-15 min
- DEALER flow (D1-D10): 15-20 min
- Bugs fix + testing: 10-20 min (si hay)
- Report writing: 10 min
- **TOTAL:** 50-70 minutos

Si encuentras bloqueadores (pod caído, DB inaccesible), salta a otro paso que sea posible
y documenta el bloqueador con comandos exactos para reproducir.

══════════════════════════════════════════════════════════════
EMPIEZA AHORA
══════════════════════════════════════════════════════════════

Comienza con PASO S1 (cluster health), luego procede secuencialmente.
Al terminar cada paso, docúmenta resultado (✅ o ❌ + evidencia).
Si necesitas ayuda, revisa el contexto de arquitectura arriba o consulta:

- docs/ARCHITECTURE.md
- docs/KUBERNETES.md
- docs/SECURITY.md
- AUDIT_PRODUCTION_GUEST_FLOWS.md (este repo)

¡Adelante! 🚀
