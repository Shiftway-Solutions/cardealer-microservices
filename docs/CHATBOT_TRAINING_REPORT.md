# 🤖 Reporte de Entrenamiento — Chatbot de Soporte Técnico OKLA

**Plataforma:** OKLA — Marketplace de Vehículos en República Dominicana  
**Dominio:** https://okla.com.do  
**Fecha de generación:** 2 de marzo de 2026  
**Propósito del chatbot:** Soporte técnico y tutor interactivo que enseña a los usuarios a utilizar la plataforma.  
**Idioma principal:** Español (República Dominicana)  
**Alcance:** El chatbot NO habla de vehículos, publicaciones ni inventarios. Solo guía al usuario sobre cómo usar la plataforma.

---

## 📑 Índice

1. [Roles de Usuario](#1-roles-de-usuario)
2. [Registro y Onboarding](#2-registro-y-onboarding)
3. [Inicio de Sesión y Autenticación](#3-inicio-de-sesión-y-autenticación)
4. [Recuperación de Contraseña](#4-recuperación-de-contraseña)
5. [Navegación General de la Plataforma](#5-navegación-general-de-la-plataforma)
6. [Panel de Cuenta (Buyer)](#6-panel-de-cuenta-buyer)
7. [Funcionalidades del Seller](#7-funcionalidades-del-seller)
8. [Portal del Dealer](#8-portal-del-dealer)
9. [Verificación KYC](#9-verificación-kyc)
10. [Seguridad de la Cuenta](#10-seguridad-de-la-cuenta)
11. [Notificaciones y Alertas](#11-notificaciones-y-alertas)
12. [Mensajería](#12-mensajería)
13. [Pagos y Facturación](#13-pagos-y-facturación)
14. [Planes y Precios](#14-planes-y-precios)
15. [Funciones de Búsqueda](#15-funciones-de-búsqueda)
16. [Favoritos y Comparaciones](#16-favoritos-y-comparaciones)
17. [Reseñas](#17-reseñas)
18. [Centro de Ayuda y Soporte](#18-centro-de-ayuda-y-soporte)
19. [PWA y Acceso Offline](#19-pwa-y-acceso-offline)
20. [Preguntas Frecuentes por Rol](#20-preguntas-frecuentes-por-rol)
21. [Flujos Conversacionales Sugeridos](#21-flujos-conversacionales-sugeridos)
22. [Glosario de Términos](#22-glosario-de-términos)

---

## 1. Roles de Usuario

### 1.1 Tipos de Cuenta

| Rol                                      | Descripción                                                                                                                                       | Costo                           | ¿Puede publicar vehículos? |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------- | -------------------------- |
| **Buyer (Comprador)**                    | Usuario registrado que busca vehículos para comprar. Puede guardar favoritos, crear alertas de precio, contactar vendedores y comparar vehículos. | Gratis                          | ❌ No                      |
| **Seller (Vendedor Individual)**         | Persona que quiere vender su vehículo. Tiene acceso a panel de vendedor, estadísticas y gestión de publicaciones.                                 | RD$1,699 por vehículo (45 días) | ✅ Sí                      |
| **Dealer (Concesionario)**               | Empresa o negocio de venta de vehículos. Acceso completo al portal de dealer con inventario, CRM, analíticas, gestión de empleados y sucursales.  | Desde RD$2,899/mes              | ✅ Sí                      |
| **Dealer Employee (Empleado de Dealer)** | Empleado de un concesionario con acceso limitado según el rol que le asigne el dueño del dealer.                                                  | Incluido en plan del dealer     | ✅ Según permisos          |

### 1.2 Intención del Usuario (UserIntent)

Al registrarse, el usuario elige su intención:

| Intención            | Descripción                            |
| -------------------- | -------------------------------------- |
| **Comprar**          | Solo quiere buscar y comprar vehículos |
| **Vender**           | Quiere vender su vehículo              |
| **Comprar y Vender** | Quiere hacer ambas cosas               |
| **Solo Navegar**     | Aún no ha decidido                     |

> **Nota:** La intención puede cambiarse después. Un buyer puede convertirse en seller desde su panel de cuenta.

---

## 2. Registro y Onboarding

### 2.1 Registro de Buyer (Comprador)

**Ruta:** `/registro`

**Pasos:**

1. Ir a https://okla.com.do/registro
2. Completar el formulario:
   - **Nombre de usuario** (único)
   - **Email** (será tu identificador para iniciar sesión)
   - **Contraseña** (mínimo 8 caracteres, debe incluir: mayúscula, minúscula, número y carácter especial)
   - **Nombre y apellido**
   - **Teléfono** (opcional pero recomendado)
   - **Aceptar términos y condiciones**
   - Seleccionar intención: "Quiero comprar", "Quiero vender" o "Ambas"
3. Hacer clic en "Registrarse"
4. Recibirás un **email de verificación** — debes hacer clic en el enlace para activar tu cuenta
5. ¡Listo! Ya puedes iniciar sesión

**Requisitos de contraseña:**

- Mínimo 8 caracteres, máximo 128
- Al menos 1 letra mayúscula (A-Z)
- Al menos 1 letra minúscula (a-z)
- Al menos 1 número (0-9)
- Al menos 1 carácter especial (!@#$%^&\*)

### 2.2 Registro de Dealer (Concesionario)

**Ruta:** `/registro/dealer`

**Pasos:**

1. Ir a https://okla.com.do/registro/dealer
2. Completar formulario de registro de negocio:
   - Datos personales del representante legal
   - Nombre del negocio
   - RNC (Registro Nacional del Contribuyente)
   - Dirección del negocio
   - Teléfonos de contacto
   - Email empresarial
3. Verificar email
4. Completar el proceso de onboarding del dealer (perfil del negocio, documentos)
5. Seleccionar plan de suscripción
6. Completar verificación KYC (identidad)

### 2.3 Registro con Google/OAuth

**Proveedores soportados:** Google, Apple, Facebook, Microsoft

**Pasos:**

1. En la página de login o registro, hacer clic en "Continuar con Google" (u otro proveedor)
2. Autorizar la aplicación en la ventana del proveedor
3. Si es primera vez, se crea tu cuenta automáticamente (email verificado automáticamente)
4. Si ya tienes cuenta con ese email, se vincula al proveedor

> **Nota:** Los usuarios que se registran con OAuth no tienen contraseña inicialmente. Pueden crear una desde Seguridad → "Crear contraseña".

### 2.4 Verificación de Email

- Después del registro, se envía un email con un enlace de verificación
- El enlace lleva a `/verificar-email?token=xxxxx`
- Si el enlace expiró, puedes solicitar uno nuevo desde el login o ir a "Reenviar verificación"
- **Es obligatorio verificar el email** para acceder a todas las funciones

### 2.5 Conversión de Buyer a Seller

**Ruta:** `/cuenta/convert-to-seller`

Si te registraste como buyer y ahora quieres vender:

1. Ir a "Mi Cuenta" → "Convertir a Vendedor"
2. Completar perfil de vendedor
3. Realizar verificación KYC (obligatoria para publicar)
4. ¡Listo! Ahora puedes publicar vehículos

---

## 3. Inicio de Sesión y Autenticación

### 3.1 Login Normal

**Ruta:** `/login`

**Pasos:**

1. Ir a https://okla.com.do/login
2. Ingresar **email** y **contraseña**
3. Hacer clic en "Iniciar sesión"
4. Si tienes 2FA activado, se te pedirá el código de verificación

**Protecciones de seguridad:**

- Después de **2 intentos fallidos**, se activa CAPTCHA (Google reCAPTCHA)
- Después de **5 intentos fallidos**, la cuenta se bloquea por **30 minutos**
- Si inicias sesión desde un dispositivo previamente revocado, se te enviará un código de verificación por email

### 3.2 Login con OAuth

1. Hacer clic en "Continuar con Google" (u otro proveedor)
2. Autorizar en la ventana del proveedor
3. Sesión iniciada automáticamente

### 3.3 Login con 2FA (Autenticación de Dos Factores)

Si tienes 2FA activado:

1. Ingresar email y contraseña normalmente
2. Se mostrará la pantalla de verificación de segundo factor
3. Según el tipo configurado:
   - **Authenticator (TOTP):** Ingresar el código de 6 dígitos de tu app (Google Authenticator, Authy, etc.)
   - **SMS:** Se enviará un código a tu teléfono verificado
   - **Email:** Se enviará un código a tu email
4. Ingresar el código y hacer clic en "Verificar"

**Si perdiste acceso a tu 2FA:**

- Usar un **código de recuperación** (se te dieron 10 al activar 2FA)
- Cada código solo se puede usar **una vez**
- Si perdiste todos los códigos, puedes usar "Recuperación de emergencia" ingresando **los 10 códigos originales**

### 3.4 Sesiones y Tokens

- Tu sesión dura **24 horas** (token de acceso)
- El token de refresco dura **30 días** (se renueva automáticamente)
- Las sesiones se almacenan como cookies HttpOnly (no accesibles por JavaScript — mayor seguridad)
- Puedes tener múltiples sesiones activas en diferentes dispositivos

---

## 4. Recuperación de Contraseña

### 4.1 Olvidé mi Contraseña

**Ruta:** `/recuperar-contrasena`

**Pasos:**

1. Ir a https://okla.com.do/recuperar-contrasena
2. Ingresar tu **email** registrado
3. Hacer clic en "Enviar enlace de recuperación"
4. Revisar tu email (incluyendo la carpeta de spam)
5. Hacer clic en el enlace del email
6. Serás redirigido a `/restablecer-contrasena`
7. Ingresar tu **nueva contraseña** y confirmarla
8. Hacer clic en "Restablecer contraseña"
9. ¡Listo! Puedes iniciar sesión con tu nueva contraseña

> **Importante:** Al cambiar tu contraseña, **todas tus sesiones activas serán cerradas** en todos los dispositivos por seguridad.

### 4.2 Crear Contraseña (Usuarios OAuth)

**Ruta:** `/crear-contrasena`

Si te registraste con Google/Apple y quieres tener también una contraseña:

1. Ir a "Seguridad" en tu cuenta
2. Hacer clic en "Crear contraseña"
3. Se te enviará un email con un enlace
4. Hacer clic en el enlace
5. Crear tu nueva contraseña
6. Ahora puedes iniciar sesión con email+contraseña O con tu proveedor OAuth

---

## 5. Navegación General de la Plataforma

### 5.1 Barra de Navegación (Navbar)

La barra de navegación cambia según tu rol:

**Visitante (sin cuenta):**

- Inicio
- Comprar
- Vender (landing informativo)
- Dealers
- ¿Por qué OKLA?
- Botones: "Iniciar Sesión" | "Registrarse"

**Buyer (Comprador registrado):**

- Inicio
- Comprar
- Vender
- Dealers
- 🔔 Campana de notificaciones
- 💬 Ícono de mensajes
- 👤 Menú de usuario desplegable:
  - Mi Cuenta
  - Favoritos
  - Alertas de Precio
  - Cerrar Sesión

**Seller (Vendedor):**

- Igual que buyer +
- En menú desplegable:
  - Mis Vehículos
  - Estadísticas
  - Pagos

**Dealer (Concesionario):**

- Inicio
- Comprar
- Vender
- Dealers
- **Mi Portal** (enlace directo al portal dealer)
- En menú desplegable:
  - Mi Inventario
  - Consultas
  - Estadísticas
  - Facturación

### 5.2 Páginas Públicas (Accesibles sin cuenta)

| Página                 | URL                     | Descripción                                                    |
| ---------------------- | ----------------------- | -------------------------------------------------------------- |
| Inicio                 | `/`                     | Página principal con vehículos destacados, marcas y categorías |
| Buscar Vehículos       | `/vehiculos`            | Listado con filtros de búsqueda                                |
| Búsqueda Avanzada      | `/buscar`               | Búsqueda con IA, filtros avanzados y búsquedas guardadas       |
| Detalle de Vehículo    | `/vehiculos/[slug]`     | Galería, especificaciones, contacto del vendedor               |
| Vista 360°             | `/vehiculos/[slug]/360` | Visor 360° del vehículo                                        |
| Directorio de Dealers  | `/dealers`              | Lista de todos los concesionarios                              |
| Perfil de Dealer       | `/dealers/[slug]`       | Página del concesionario con inventario                        |
| Comparar Vehículos     | `/comparar`             | Comparación lado a lado                                        |
| Vender tu Auto         | `/vender`               | Landing con información sobre cómo vender                      |
| Guía de Precios        | `/precios`              | Referencia de precios del mercado dominicano                   |
| Impulsar Vehículo      | `/impulsar`             | Información sobre publicidad y promociones                     |
| Contacto               | `/contacto`             | Formulario de contacto                                         |
| Ayuda                  | `/ayuda`                | Centro de ayuda                                                |
| FAQ                    | `/faq`                  | Preguntas frecuentes                                           |
| Términos de Servicio   | `/terminos`             | Términos y condiciones                                         |
| Política de Privacidad | `/privacidad`           | Política de privacidad                                         |
| Acerca de Nosotros     | `/nosotros`             | Información sobre OKLA                                         |

---

## 6. Panel de Cuenta (Buyer)

### 6.1 Dashboard

**Ruta:** `/cuenta`

Tu panel principal muestra:

- Estadísticas de navegación (vehículos vistos, búsquedas realizadas)
- Actividad reciente
- Enlaces rápidos a funciones más usadas
- Notificaciones pendientes

### 6.2 Perfil

**Ruta:** `/cuenta/perfil`

Puedes editar:

- Foto de perfil (avatar)
- Nombre y apellido
- Teléfono
- Información de contacto

### 6.3 Favoritos

**Ruta:** `/cuenta/favoritos`

- Lista de vehículos que has guardado como favoritos
- Puedes marcar/desmarcar favoritos desde cualquier tarjeta de vehículo (ícono de corazón ❤️)
- Acceso rápido a los vehículos que te interesan

### 6.4 Búsquedas Guardadas

**Ruta:** `/cuenta/busquedas`

- Guarda criterios de búsqueda específicos
- Recibe alertas cuando aparezcan vehículos que coincidan con tu búsqueda
- Gestiona y elimina búsquedas guardadas

### 6.5 Alertas de Precio

**Ruta:** `/cuenta/alertas`

- Configura alertas para vehículos específicos
- Recibe notificación cuando baje el precio
- Gestiona tus alertas activas

### 6.6 Mensajes

**Ruta:** `/cuenta/mensajes` o `/mensajes` (vista completa)

- Bandeja de entrada de conversaciones con vendedores/dealers
- Envía y recibe mensajes
- Historial de conversaciones

### 6.7 Notificaciones

**Ruta:** `/cuenta/notificaciones`

- Todas tus notificaciones:
  - Nuevos mensajes
  - Cambios de precio en favoritos
  - Actualizaciones del sistema
  - Alertas de seguridad
- Marca como leídas individual o masivamente

### 6.8 Historial

**Ruta:** `/cuenta/historial`

- Vehículos que has visto recientemente
- Historial de navegación en la plataforma

### 6.9 Seguridad

**Ruta:** `/cuenta/seguridad`

(Ver sección 10 - Seguridad de la Cuenta)

### 6.10 Configuración / Preferencias

**Ruta:** `/cuenta/configuracion`

- Preferencias de notificaciones
- Configuración de privacidad
- Preferencias de búsqueda

### 6.11 Verificación KYC

**Ruta:** `/cuenta/verificacion`

(Ver sección 9 - Verificación KYC)

### 6.12 Reseñas

**Ruta:** `/cuenta/resenas`

- Ver reseñas que has recibido
- Ver reseñas que has dejado

### 6.13 Pagos

**Ruta:** `/cuenta/pagos`

- Métodos de pago registrados
- Historial de transacciones

---

## 7. Funcionalidades del Seller

### 7.1 Cómo Convertirse en Seller

**Opción A — Registro directo:**

1. Al registrarte, selecciona "Quiero vender" como intención

**Opción B — Conversión desde buyer:**

1. Ir a `/cuenta/convert-to-seller`
2. Completar perfil de vendedor
3. Verificar identidad (KYC)

### 7.2 Registro de Vendedor (Onboarding)

**Ruta:** `/vender/registro`

Wizard de 4 pasos:

1. **Paso 1 — Cuenta:** Verificar datos de cuenta
2. **Paso 2 — Perfil:** Completar perfil de vendedor (nombre, ubicación, descripción)
3. **Paso 3 — Vehículo:** Información del primer vehículo (opcional)
4. **Paso 4 — Éxito:** Confirmación y siguiente pasos

### 7.3 Publicar un Vehículo

**Ruta:** `/publicar`

**Requisitos previos:**

- ✅ Email verificado
- ✅ Verificación KYC aprobada (obligatorio)

**Pasos del wizard de publicación:**

1. **VIN / Datos básicos:** Ingresa el VIN para decodificación automática, o completa manualmente: marca, modelo, año, versión
2. **Detalles:** Kilometraje, combustible, transmisión, color, estado, descripción
3. **Fotos:** Sube fotos del vehículo (mín. 1, máx. según plan). La plataforma ofrece **remoción de fondo con IA** para fotos profesionales
4. **Precio:** Establece el precio de venta
5. **Preview:** Revisa cómo se verá tu publicación
6. **Pago:** Paga por la publicación (RD$1,699 por 45 días para sellers individuales)
7. **Publicación:** Tu vehículo aparece en la plataforma

### 7.4 Gestión de Vehículos

**Ruta:** `/mis-vehiculos`

- Lista de todos tus vehículos publicados
- Estado de cada publicación (activo, pendiente, expirado, vendido)
- Editar detalles de una publicación
- Ver estadísticas (vistas, consultas, favoritos)
- Impulsar/promover una publicación

### 7.5 Impulsar Publicación (Boost)

**Ruta:** `/mis-vehiculos/[id]/boost` o `/impulsar`

Opciones de promoción:
| Tipo | Precio | Duración | Beneficio |
|------|--------|----------|-----------|
| Boost Básico | RD$499 | 7 días | Mayor visibilidad en búsquedas |
| Boost Premium | RD$1,499 | 30 días | Destacado + mayor visibilidad |

### 7.6 Dashboard del Seller

**Ruta:** `/vender/dashboard`

- Resumen de rendimiento: vistas, consultas recibidas
- Vehículos activos vs. expirados
- Estadísticas de publicaciones
- Leads recibidos

### 7.7 Leads / Consultas

**Ruta:** `/vender/leads`

- Consultas recibidas de compradores interesados
- Información del comprador
- Historial de contacto

---

## 8. Portal del Dealer

### 8.1 Acceso al Portal

**Ruta:** `/dealer`

El portal del dealer tiene su propio layout con:

- Sidebar de navegación (lateral izquierdo en desktop)
- Navegación inferior en móvil
- Menú hamburguesa

### 8.2 Secciones del Portal

#### 📦 Inventario

**Ruta:** `/dealer/inventario`

- Lista completa de vehículos en inventario
- Agregar vehículo individual (`/dealer/inventario/nuevo`)
- Importación masiva de vehículos (`/dealer/inventario/importar`) — CSV/Excel
- Editar detalles de cada vehículo
- Estado de publicaciones

#### 📋 Leads / Consultas

**Ruta:** `/dealer/leads`

- CRM de leads recibidos
- Detalle de cada lead
- Estado del lead (nuevo, contactado, en negociación, cerrado)
- Filtros y búsqueda

#### 📊 Analíticas

**Ruta:** `/dealer/analiticas`

- Dashboard de rendimiento general
- Análisis de inventario (`/dealer/analiticas/inventario`)
- Análisis de ventas (`/dealer/analiticas/ventas`)
- Métricas clave: vistas, consultas, conversiones

#### 📅 Citas

**Ruta:** `/dealer/citas`

- Gestión de citas con compradores
- Vista de calendario (`/dealer/citas/calendario`)
- Programar y confirmar citas

#### 💬 Mensajes

**Ruta:** `/dealer/mensajes`

- Bandeja de mensajes del dealer
- Conversaciones con compradores

#### ⭐ Reseñas

**Ruta:** `/dealer/resenas`

- Reseñas de clientes
- Responder a reseñas

#### 👥 Empleados (Gestión)

**Ruta:** `/dealer/empleados`

- Lista de empleados del dealer
- Agregar nuevo empleado (`/dealer/empleados/nuevo`)
- Asignar roles y permisos
- Editar o desactivar empleados

#### 📍 Sucursales

**Ruta:** `/dealer/sucursales`

- Gestión de ubicaciones/sucursales del dealer
- Agregar nueva sucursal (`/dealer/sucursales/nueva`)
- Horarios, dirección, contacto de cada sucursal

#### 🤖 Precios IA

**Ruta:** `/dealer/precios-ia`

- Recomendaciones de precios basadas en inteligencia artificial
- Análisis del mercado

#### 📈 Reportes

**Ruta:** `/dealer/reportes`

- Reportes de rendimiento
- Exportación de datos

#### 📣 Publicidad

**Ruta:** `/dealer/publicidad`

- Gestión de campañas publicitarias
- Vehículos destacados y premium

#### ⚙️ Configuración del Dealer

| Sección            | Ruta                                | Descripción                              |
| ------------------ | ----------------------------------- | ---------------------------------------- |
| Perfil del Negocio | `/dealer/configuracion/perfil`      | Logo, nombre, descripción, horarios      |
| Documentos         | `/dealer/configuracion/documentos`  | Documentos legales del negocio           |
| Facturación        | `/dealer/configuracion/facturacion` | Facturación actual e historial           |
| Historial de Pagos | `/dealer/configuracion/pagos`       | Registro de todos los pagos              |
| Suscripción        | `/dealer/configuracion/suscripcion` | Plan actual, cambio de plan, cancelación |
| Configuración      | `/dealer/configuracion`             | Ajustes generales del portal             |

---

## 9. Verificación KYC

### 9.1 ¿Qué es KYC?

KYC (Know Your Customer / Conoce a Tu Cliente) es un proceso de verificación de identidad **obligatorio** para:

- ✅ **Sellers** que quieran publicar vehículos
- ✅ **Dealers** que quieran operar en la plataforma

**No es obligatorio para Buyers** (compradores).

El proceso cumple con la **Ley 155-17** de prevención de lavado de activos de República Dominicana.

### 9.2 ¿Dónde se hace la verificación?

**Ruta:** `/cuenta/verificacion`

### 9.3 Proceso de Verificación Paso a Paso

**Paso 1 — Iniciar Sesión de Verificación**

- Ir a "Mi Cuenta" → "Verificación de Identidad"
- Se crea una sesión que dura **30 minutos**
- Tienes **3 intentos** máximo; si fallas 3 veces, debes esperar **24 horas**

**Paso 2 — Captura del Documento (Frente)**

- Tomar foto del **frente** de tu documento de identidad
- Documentos aceptados:
  - 🇩🇴 **Cédula de Identidad** (preferida)
  - 🛂 Pasaporte
  - 🚗 Licencia de conducir
  - 📄 Carné de Residencia
  - 🏢 RNC (para negocios)
- Formato: JPEG, PNG o HEIC
- Tamaño máximo: 10 MB
- **Consejos:** Buena iluminación, sin reflejos, documento completo en la foto

**Paso 3 — Captura del Documento (Reverso)**

- Tomar foto del **reverso** del documento
- Para la cédula dominicana es **obligatorio** ambos lados
- El sistema extrae automáticamente tus datos por OCR (reconocimiento óptico)

**Paso 4 — Prueba de Vida (Liveness)**

- Se te pedirá completar **3 desafíos** frente a la cámara:
  - Parpadear
  - Sonreír
  - Girar la cabeza a la izquierda
  - Girar la cabeza a la derecha
  - Asentir con la cabeza
  - Abrir la boca
- Esto previene que alguien use una foto impresa o un video
- Se necesita un puntaje mínimo del **70%**

**Paso 5 — Selfie y Comparación Facial**

- Tomar una selfie clara
- El sistema compara tu selfie con la foto del documento
- Se usa **Amazon Rekognition** para la comparación facial
- Umbral de coincidencia: **80%**

**Paso 6 — Información Personal**

- Completar o verificar los datos extraídos:
  - Nombre completo
  - Fecha de nacimiento
  - Nacionalidad
  - Dirección (calle, sector, ciudad, provincia)
  - Ocupación
  - Empleador (opcional)
  - Fuente de ingresos
  - Ingreso estimado

**Paso 7 — Enviar para Revisión**

- Revisar toda la información
- Hacer clic en "Enviar para verificación"
- Estado cambia a **"En Revisión"**

**Paso 8 — Resultado**

- Un oficial de cumplimiento revisa tu solicitud
- Posibles resultados:
  - ✅ **Aprobado** — Válido por **365 días** (1 año)
  - ❌ **Rechazado** — Se te informa el motivo y puedes intentar de nuevo
  - ⏳ **Expirado** — Después de 1 año, debes renovar

### 9.4 Estados de KYC

```
Pendiente → En Progreso → En Revisión → Aprobado ✅
                                       → Rechazado ❌
                                       → Expirado ⏳
                                       → Suspendido 🚫
```

### 9.5 ¿Qué pasa si no completo el KYC?

- **Buyers:** Pueden usar la plataforma normalmente sin KYC
- **Sellers:** No pueden publicar vehículos. Se muestra una barrera (VerificationGate) que les pide completar la verificación
- **Dealers:** No pueden acceder a funciones del portal dealer

---

## 10. Seguridad de la Cuenta

### 10.1 ¿Dónde está la configuración de seguridad?

**Ruta:** `/cuenta/seguridad`

### 10.2 Cambiar Contraseña

**Pasos:**

1. Ir a "Mi Cuenta" → "Seguridad"
2. Sección "Cambiar contraseña"
3. Ingresar contraseña actual
4. Ingresar nueva contraseña y confirmarla
5. Hacer clic en "Cambiar contraseña"

> **⚠️ Importante:** Al cambiar tu contraseña, **todas tus sesiones activas se cerrarán** en todos los dispositivos. Deberás iniciar sesión de nuevo.

### 10.3 Autenticación de Dos Factores (2FA)

**¿Qué es?** Un segundo nivel de protección que requiere un código adicional al iniciar sesión.

**Tipos disponibles:**

| Tipo                     | Descripción                                                       | Recomendado                     |
| ------------------------ | ----------------------------------------------------------------- | ------------------------------- |
| **Authenticator (TOTP)** | App como Google Authenticator o Authy genera códigos de 6 dígitos | ✅ Más seguro                   |
| **SMS**                  | Se envía un código por mensaje de texto a tu teléfono             | ⚠️ Requiere teléfono verificado |
| **Email**                | Se envía un código a tu email                                     | ⚠️ Menor seguridad              |

**Cómo activar 2FA:**

1. Ir a "Mi Cuenta" → "Seguridad"
2. Sección "Autenticación de dos factores"
3. Seleccionar tipo (Authenticator recomendado)
4. **Si eliges Authenticator:**
   - Se mostrará un código QR
   - Escanéalo con Google Authenticator, Authy o 1Password
   - Ingresa el código de 6 dígitos que genera la app
5. **Se generarán 10 códigos de recuperación** — ¡GUÁRDALOS EN UN LUGAR SEGURO!
6. 2FA activado ✅

**Cómo desactivar 2FA:**

1. Ir a "Seguridad" → "Desactivar 2FA"
2. Ingresar tu contraseña para confirmar
3. 2FA desactivado

**Códigos de Recuperación:**

- Se te dan **10 códigos** al activar 2FA
- Cada código es de **un solo uso**
- Úsalos si pierdes acceso a tu app de autenticación o teléfono
- Puedes regenerar nuevos códigos (requiere contraseña)
- **Recuperación de emergencia:** Si perdiste todos los códigos y tu dispositivo, necesitas ingresar los 10 códigos originales para restablecer

### 10.4 Sesiones Activas

**¿Qué es?** Lista de todos los dispositivos donde tienes sesión abierta.

**Información mostrada:**

- Dispositivo y navegador
- Ubicación aproximada (basada en IP)
- IP (parcialmente oculta por seguridad: `192.168.1.***`)
- Última actividad
- Si es la sesión actual (marcada con "Sesión actual")

**Cómo cerrar una sesión:**

1. Ir a "Seguridad" → "Sesiones activas"
2. Encontrar la sesión que quieres cerrar
3. Hacer clic en "Revocar"
4. Se enviará un código de verificación a tu email
5. Ingresar el código
6. Sesión revocada ✅

**Cerrar todas las sesiones:**

- Opción de "Cerrar todas las sesiones" (excepto la actual si lo deseas)
- Útil si sospechas que alguien accedió a tu cuenta

### 10.5 Historial de Inicios de Sesión

- Ver los últimos 20 inicios de sesión
- Información: dispositivo, navegador, ubicación, IP, fecha/hora, si fue exitoso o fallido
- Útil para detectar accesos no autorizados

### 10.6 Vincular/Desvincular Cuentas Externas (OAuth)

**Vincular:**

1. Ir a "Seguridad" → "Cuentas vinculadas"
2. Hacer clic en "Vincular Google" (u otro proveedor)
3. Autorizar la aplicación
4. ¡Vinculado! Ahora puedes iniciar sesión con ese proveedor

**Desvincular:**

1. Ir a "Seguridad" → "Cuentas vinculadas"
2. Hacer clic en "Desvincular" junto al proveedor
3. **Requisito:** Debes tener una contraseña configurada antes de desvincular
4. Se te enviará un código de verificación
5. Confirmar con el código
6. ¡Desvinculado!

> **Solo puedes tener un proveedor externo vinculado a la vez.**

### 10.7 Verificación de Teléfono

**Ruta:** Desde "Seguridad" o "Perfil"

**Pasos:**

1. Ingresar tu número de teléfono
2. Hacer clic en "Enviar código de verificación"
3. Recibirás un SMS con un código de 6 dígitos
4. Ingresar el código
5. ¡Teléfono verificado! ✅

**Límites:** Máximo 3 códigos SMS por hora para evitar abuso.

---

## 11. Notificaciones y Alertas

### 11.1 Tipos de Notificaciones

| Categoría           | Ejemplos                                                                                         | Canales          |
| ------------------- | ------------------------------------------------------------------------------------------------ | ---------------- |
| **Transaccionales** | Confirmación de registro, verificación de email, códigos 2FA, recibos de pago                    | Email, SMS       |
| **Seguridad**       | Nuevo inicio de sesión, cambio de contraseña, dispositivo revocado, alertas de intentos fallidos | Email, SMS, Push |
| **Vehículos**       | Nuevas consultas, cambios de precio en favoritos, publicación aprobada                           | Email, Push      |
| **Sistema**         | Actualizaciones de la plataforma, mantenimiento programado, nuevas funciones                     | Push, Email      |
| **Marketing**       | Promociones, ofertas especiales, novedades                                                       | Email (opcional) |

### 11.2 Canales de Notificación

- **Email:** Todas las notificaciones importantes (verificación, seguridad, transacciones)
- **SMS:** Códigos de verificación, alertas de seguridad críticas
- **Push:** Notificaciones en tiempo real en el navegador/app (requiere permiso)

### 11.3 Configuración de Preferencias

**Ruta:** `/cuenta/configuracion` → Sección "Notificaciones"

Puedes personalizar:

- Activar/desactivar cada canal (email, SMS, push)
- Activar/desactivar por categoría (transaccional, seguridad, vehículos, marketing)
- Los emails de **marketing** tienen enlace de desuscripción obligatorio
- Las notificaciones de **seguridad** no se pueden desactivar

### 11.4 Centro de Notificaciones

**Acceso:** Ícono 🔔 en la barra de navegación

- Ver todas las notificaciones
- Notificaciones no leídas resaltadas
- Marcar como leída
- Filtrar por tipo

---

## 12. Mensajería

### 12.1 Cómo Contactar a un Vendedor

1. Ir a la página de detalle de un vehículo
2. Hacer clic en **"Contactar Vendedor"**
3. Escribir tu mensaje (consulta, negociación, solicitar más info, agendar visita)
4. Enviar

### 12.2 Bandeja de Mensajes

**Ruta:** `/cuenta/mensajes` (dentro del panel de cuenta) o `/mensajes` (vista completa)

- Lista de todas tus conversaciones
- Indicador de mensajes no leídos
- Buscar conversaciones

### 12.3 Dentro de una Conversación

- Historial completo de mensajes
- Enviar nuevos mensajes
- Estado de mensajes: Enviado, Entregado, Leído
- Contexto del vehículo asociado a la conversación

### 12.4 Reglas de Mensajería

- Máximo **10 consultas por día** (para prevenir spam)
- Máximo **50 mensajes por hora** en una conversación
- Las conversaciones se archivan automáticamente después de **30 días de inactividad**
- **Detección de spam:** Si envías el mismo mensaje a más de 5 vendedores en menos de 1 hora, se marca como sospechoso

---

## 13. Pagos y Facturación

### 13.1 Métodos de Pago Aceptados

| Método                     | Descripción                              | Procesador    |
| -------------------------- | ---------------------------------------- | ------------- |
| **Tarjeta de crédito**     | Visa, Mastercard (internacional y local) | Stripe / Azul |
| **Tarjeta de débito**      | Tarjetas dominicanas                     | Azul          |
| **Transferencia bancaria** | Manual                                   | —             |

### 13.2 Procesadores de Pago

- **Stripe:** Para tarjetas internacionales
- **Azul (Banco Popular):** Para tarjetas dominicanas locales
- El sistema selecciona automáticamente el procesador disponible

### 13.3 ¿Cómo pagar por una publicación? (Sellers)

1. Completar el wizard de publicación de vehículo
2. En el paso de pago, seleccionar método de pago
3. Para Stripe: ingresar datos de tarjeta directamente
4. Para Azul: serás redirigido a la página de pago de Azul
5. Al completar el pago, tu vehículo se publica automáticamente
6. Recibirás un email de confirmación con tu factura

### 13.4 ¿Cómo pagar la suscripción? (Dealers)

1. Ir a "Mi Portal" → "Configuración" → "Suscripción"
2. Seleccionar plan
3. Agregar método de pago
4. Confirmar suscripción
5. **Renovación automática** mensual
6. Si el pago falla, tienes **3 días de gracia** antes de la suspensión

### 13.5 Facturación

- Facturas generadas automáticamente con cada pago
- Numeración: `INV-2026-00001`
- Incluye **18% ITBIS** (impuesto dominicano)
- PDF descargable
- Accesible desde "Mi Cuenta" → "Pagos" o "Facturación"

### 13.6 Programa Early Bird (Miembro Fundador)

- Los primeros usuarios reciben **3 meses gratis**
- Badge permanente de **"Miembro Fundador"** 🏅
- Descuento del **25%** permanente
- Disponible por tiempo limitado

### 13.7 Checkout

**Ruta:** `/checkout`

| Página                | Descripción         |
| --------------------- | ------------------- |
| `/checkout`           | Proceso de pago     |
| `/checkout/exito`     | Pago exitoso ✅     |
| `/checkout/error`     | Error en el pago ❌ |
| `/checkout/cancelado` | Pago cancelado      |

---

## 14. Planes y Precios

### 14.1 Para Sellers (Vendedores Individuales)

| Concepto               | Precio   | Duración |
| ---------------------- | -------- | -------- |
| Publicación Individual | RD$1,699 | 45 días  |
| Boost Básico           | RD$499   | 7 días   |
| Boost Premium          | RD$1,499 | 30 días  |

### 14.2 Para Dealers (Concesionarios)

| Plan           | Precio Mensual | Vehículos  | Características                                         |
| -------------- | -------------- | ---------- | ------------------------------------------------------- |
| **Starter**    | RD$2,899/mes   | Hasta 20   | Dashboard, inventario, leads básico                     |
| **Pro**        | RD$7,499/mes   | Hasta 75   | Todo Starter + analíticas avanzadas, empleados, CRM     |
| **Enterprise** | RD$17,499/mes  | Ilimitados | Todo Pro + API, reportes avanzados, soporte prioritario |

### 14.3 Para Buyers (Compradores)

- ✅ **Totalmente gratis**
- Búsqueda ilimitada
- Favoritos ilimitados
- Alertas de precio
- Mensajería
- Comparación de vehículos

### 14.4 Impuestos

- Todos los precios **incluyen ITBIS (18%)** o se desglosa en la factura
- Comisión de plataforma: **2.5%** en transacciones

---

## 15. Funciones de Búsqueda

### 15.1 Búsqueda Básica

**Ruta:** `/vehiculos`

- Barra de búsqueda por texto
- Filtros:
  - Marca
  - Modelo
  - Año (desde/hasta)
  - Precio (desde/hasta)
  - Kilometraje
  - Tipo de combustible
  - Transmisión
  - Color
  - Ubicación
  - Tipo de carrocería

### 15.2 Búsqueda Avanzada con IA

**Ruta:** `/buscar`

- **Búsqueda por lenguaje natural:** Escribe lo que buscas como si hablaras con una persona. Ejemplo: "Toyota Corolla del 2020 automático con menos de 30,000 km"
- Selector de tipo de carrocería visual
- Filtros avanzados combinados
- **Guardar búsqueda:** Guarda criterios para recibir alertas cuando aparezcan nuevos vehículos

### 15.3 Búsquedas Guardadas

**Ruta:** `/cuenta/busquedas`

- Gestiona tus búsquedas guardadas
- Activa/desactiva alertas por búsqueda
- Elimina búsquedas que ya no necesitas

---

## 16. Favoritos y Comparaciones

### 16.1 Favoritos

**Cómo agregar a favoritos:**

- En cualquier tarjeta de vehículo, hacer clic en el ícono ❤️ (corazón)
- Necesitas tener cuenta (si no tienes, se te pedirá iniciar sesión)
- Acceso rápido desde "Mi Cuenta" → "Favoritos" o menú desplegable

### 16.2 Comparar Vehículos

**Ruta:** `/comparar`

- Selecciona hasta 3-4 vehículos para comparar
- Vista lado a lado con:
  - Especificaciones técnicas
  - Precios
  - Fotos
  - Características
- Ideal para tomar una decisión de compra informada

---

## 17. Reseñas

### 17.1 ¿Quién puede dejar reseñas?

- Cualquier usuario registrado puede dejar reseñas sobre:
  - Vendedores (sellers)
  - Concesionarios (dealers)
  - Vehículos comprados

### 17.2 ¿Cómo dejar una reseña?

1. Ir al perfil del vendedor/dealer o vehículo
2. Sección "Reseñas"
3. Hacer clic en "Escribir reseña"
4. Seleccionar calificación (estrellas ⭐)
5. Escribir tu experiencia
6. Enviar

### 17.3 Gestión de Reseñas

- Sellers y dealers pueden **responder** a las reseñas
- Los admins pueden moderar reseñas inapropiadas
- Las reseñas aparecen en el perfil público del vendedor/dealer

---

## 18. Centro de Ayuda y Soporte

### 18.1 Páginas de Ayuda

| Página             | URL                             | Descripción                     |
| ------------------ | ------------------------------- | ------------------------------- |
| Centro de Ayuda    | `/ayuda`                        | Página principal con categorías |
| Categoría de Ayuda | `/ayuda/[categoria]`            | Artículos por categoría         |
| Artículo de Ayuda  | `/ayuda/[categoria]/[articulo]` | Artículo individual             |
| FAQ                | `/faq`                          | Preguntas frecuentes            |
| Guías              | `/guias`                        | Guías paso a paso               |

### 18.2 Formulario de Contacto

**Ruta:** `/contacto`

Campos:

- Nombre
- Email
- Teléfono (opcional)
- Asunto
- Mensaje

### 18.3 Reportar un Problema

**Ruta:** `/reportar`

- Reportar un listado fraudulento
- Reportar un usuario
- Reportar un problema técnico

---

## 19. PWA y Acceso Offline

### 19.1 ¿Qué es PWA?

OKLA es una **Progressive Web App**, lo que significa que puedes:

- Instalarla en tu dispositivo como una app nativa
- Recibir notificaciones push
- Tener acceso básico sin internet

### 19.2 Cómo Instalar OKLA en tu Dispositivo

**En Android (Chrome):**

1. Visitar https://okla.com.do
2. Aparecerá un banner "Agregar a pantalla de inicio"
3. Hacer clic en "Instalar"
4. ¡OKLA aparecerá como una app en tu teléfono!

**En iPhone (Safari):**

1. Visitar https://okla.com.do
2. Hacer clic en el ícono de compartir ⬆️
3. Seleccionar "Agregar a pantalla de inicio"
4. Hacer clic en "Agregar"

**En Desktop (Chrome/Edge):**

1. Visitar https://okla.com.do
2. En la barra de URL, hacer clic en el ícono de instalar
3. Confirmar la instalación

### 19.3 Modo Offline

- Si pierdes conexión a internet, se muestra una página offline básica
- Podrás ver contenido previamente cacheado
- Al restaurar la conexión, la app se actualiza automáticamente

---

## 20. Preguntas Frecuentes por Rol

### 20.1 Preguntas de Buyers (Compradores)

**P: ¿Es gratis usar OKLA como comprador?**
R: ¡Sí! Buscar, guardar favoritos, comparar vehículos, recibir alertas de precio y contactar vendedores es completamente gratis.

**P: ¿Necesito verificar mi identidad para comprar?**
R: No. La verificación KYC solo es necesaria para publicar vehículos (sellers y dealers).

**P: ¿Cómo puedo saber si un vendedor es confiable?**
R: Busca el badge de verificación ✅ en su perfil. Los vendedores verificados han completado el proceso KYC. También puedes ver sus reseñas.

**P: ¿Puedo contactar a un vendedor directamente?**
R: Sí, desde la página de detalle del vehículo, haz clic en "Contactar Vendedor". La comunicación inicial es a través de la plataforma.

**P: ¿Cómo recibo alertas de nuevos vehículos?**
R: Guarda una búsqueda con tus criterios preferidos y activa las alertas. Te notificaremos cuando aparezcan vehículos que coincidan.

**P: ¿Puedo convertirme en vendedor después?**
R: ¡Sí! Ve a "Mi Cuenta" → "Convertir a Vendedor". Necesitarás completar la verificación KYC.

### 20.2 Preguntas de Sellers (Vendedores)

**P: ¿Cuánto cuesta publicar un vehículo?**
R: RD$1,699 por publicación individual, válida por 45 días.

**P: ¿Necesito verificar mi identidad?**
R: Sí, la verificación KYC es obligatoria antes de poder publicar tu primer vehículo.

**P: ¿Qué documentos necesito para la verificación?**
R: Tu cédula de identidad dominicana (ambos lados). También se acepta pasaporte o licencia de conducir.

**P: ¿Cuántas fotos puedo subir?**
R: Depende del tipo de publicación. La plataforma ofrece remoción de fondo con IA para fotos más profesionales.

**P: ¿Cómo puedo destacar mi publicación?**
R: Puedes comprar un "Boost" desde RD$499 para mayor visibilidad en búsquedas y secciones destacadas.

**P: ¿Cómo sé si alguien está interesado en mi vehículo?**
R: Recibirás notificaciones cuando alguien envíe una consulta. Ve a "Leads" para gestionar todos los interesados.

**P: ¿Puedo editar mi publicación después de publicarla?**
R: Sí, ve a "Mis Vehículos" y haz clic en el vehículo que quieres editar.

### 20.3 Preguntas de Dealers (Concesionarios)

**P: ¿Cuánto cuesta la suscripción?**
R: Desde RD$2,899/mes (Starter) hasta RD$17,499/mes (Enterprise). Hay un periodo de prueba de 14 días con Stripe.

**P: ¿Puedo agregar empleados a mi cuenta?**
R: Sí, desde "Mi Portal" → "Empleados" puedes agregar empleados y asignarles roles con permisos específicos.

**P: ¿Cómo importo mi inventario existente?**
R: Usa la función de "Importación Masiva" en "Inventario" → "Importar". Soporta archivos CSV y Excel.

**P: ¿Qué incluye el plan Enterprise?**
R: Vehículos ilimitados, analíticas avanzadas, CRM completo, API de integración, reportes avanzados y soporte prioritario.

**P: ¿Puedo tener múltiples sucursales?**
R: Sí, desde "Sucursales" puedes agregar y gestionar todas tus ubicaciones.

**P: ¿Qué pasa si no pago mi suscripción?**
R: Tienes 3 días de gracia después del fallo del pago. Si no se regulariza, tu cuenta se suspende temporalmente hasta que actualices tu método de pago.

**P: ¿Puedo cambiar de plan?**
R: Sí, puedes subir o bajar de plan en cualquier momento desde "Configuración" → "Suscripción".

**P: ¿Existe el programa Early Bird?**
R: Sí, los primeros dealers en unirse reciben 3 meses gratis y un badge permanente de "Miembro Fundador" con 25% de descuento.

---

## 21. Flujos Conversacionales Sugeridos

### 21.1 Flujo: Nuevo usuario quiere registrarse

```
Chatbot: ¡Hola! 👋 Bienvenido a OKLA. ¿En qué puedo ayudarte?
Usuario: Quiero crear una cuenta
Chatbot: ¡Genial! ¿Qué te gustaría hacer en OKLA?
  1. 🔍 Buscar y comprar vehículos (Gratis)
  2. 💰 Vender mi vehículo (RD$1,699 por publicación)
  3. 🏢 Registrar mi concesionario (Desde RD$2,899/mes)
  4. 🤔 Ambas cosas (comprar y vender)

[Usuario selecciona opción]

Chatbot: Perfecto. Para registrarte:
1. Ve a okla.com.do/registro
2. Completa tus datos personales
3. Revisa tu email y haz clic en el enlace de verificación
4. ¡Listo! Ya puedes empezar

¿Necesitas ayuda con algo más?
```

### 21.2 Flujo: Usuario no puede iniciar sesión

```
Chatbot: Entiendo que tienes problemas para iniciar sesión. Veamos qué puede estar pasando:

¿Cuál de estos describe mejor tu situación?
  1. No recuerdo mi contraseña
  2. Mi cuenta está bloqueada
  3. Me pide un código adicional (2FA)
  4. Inicié sesión con Google y ahora no funciona
  5. Otro problema

[Si elige 1]
Chatbot: Para recuperar tu contraseña:
1. Ve a okla.com.do/recuperar-contrasena
2. Ingresa el email con el que te registraste
3. Revisa tu email (incluida la carpeta de spam)
4. Haz clic en el enlace y crea una nueva contraseña

[Si elige 2]
Chatbot: Tu cuenta se bloquea después de 5 intentos fallidos. El bloqueo dura 30 minutos.
Espera 30 minutos e intenta de nuevo con la contraseña correcta.
Si olvidaste tu contraseña, usa la opción de recuperación.

[Si elige 3]
Chatbot: Si tienes 2FA activado:
- Authenticator: Abre tu app (Google Authenticator o Authy) e ingresa el código
- SMS: Haz clic en "Enviar código por SMS"
- Si perdiste acceso, usa un código de recuperación

[Si elige 4]
Chatbot: Si te registraste con Google:
1. Haz clic en "Continuar con Google" en la página de login
2. Selecciona la cuenta de Google que usaste
3. Si quieres también tener contraseña, ve a Seguridad → "Crear contraseña"
```

### 21.3 Flujo: Seller quiere publicar su primer vehículo

```
Chatbot: ¡Excelente que quieras vender tu vehículo! 🚗

Antes de publicar, necesitas completar estos pasos:

✅ Paso 1: Tener una cuenta registrada
✅ Paso 2: Verificar tu email
⬜ Paso 3: Completar verificación KYC (identidad)
⬜ Paso 4: Publicar tu vehículo

¿En qué paso estás?

[Si necesita KYC]
Chatbot: Para verificar tu identidad:
1. Ve a "Mi Cuenta" → "Verificación de Identidad"
2. Ten tu cédula a la mano
3. Necesitarás una cámara (webcam o celular)
4. El proceso toma aproximadamente 5-10 minutos
5. Un oficial revisará tu solicitud (generalmente en 24-48 horas)

¿Quieres que te explique el proceso paso a paso?
```

### 21.4 Flujo: Dealer quiere configurar su portal

```
Chatbot: ¡Bienvenido al Portal Dealer de OKLA! 🏢

Para empezar a sacarle provecho a tu portal, te recomiendo estos pasos:

1. 🏪 **Perfil del negocio**: Agrega tu logo, descripción y horarios
   → Mi Portal → Configuración → Perfil del Negocio

2. 📍 **Sucursales**: Agrega tus ubicaciones
   → Mi Portal → Sucursales → Nueva Sucursal

3. 👥 **Equipo**: Agrega a tus empleados
   → Mi Portal → Empleados → Nuevo Empleado

4. 📦 **Inventario**: Sube tus vehículos
   → Mi Portal → Inventario → Agregar Vehículo
   O usa la Importación Masiva para subir todo tu inventario de una vez

5. 💳 **Suscripción**: Verifica tu plan activo
   → Mi Portal → Configuración → Suscripción

¿Con cuál paso quieres empezar?
```

### 21.5 Flujo: Usuario quiere activar 2FA

```
Chatbot: La autenticación de dos factores (2FA) agrega una capa extra de seguridad a tu cuenta. ¡Excelente decisión! 🔐

Tienes 3 opciones:
1. 📱 App Authenticator (más segura) — Google Authenticator o Authy
2. 📲 SMS — código por mensaje de texto
3. 📧 Email — código por correo

¿Cuál prefieres?

[Si elige Authenticator]
Chatbot: Para configurar Authenticator:
1. Descarga Google Authenticator o Authy en tu teléfono
2. Ve a okla.com.do → Mi Cuenta → Seguridad → "Activar 2FA"
3. Selecciona "Authenticator"
4. Se mostrará un código QR — escanéalo con la app
5. Ingresa el código de 6 dígitos que genera la app
6. ¡IMPORTANTE! Guarda los 10 códigos de recuperación en un lugar seguro

⚠️ Si pierdes tu teléfono sin los códigos de recuperación, será muy difícil acceder a tu cuenta.
```

### 21.6 Flujo: Buyer quiere recibir alertas de precio

```
Chatbot: ¡Las alertas de precio te notifican cuando baja el precio de un vehículo que te interesa! 💰

Para configurar alertas:

**Opción 1 — Alerta en un vehículo específico:**
1. Ve a la página del vehículo que te interesa
2. Haz clic en "Crear alerta de precio"
3. Te notificaremos si el precio cambia

**Opción 2 — Alerta por búsqueda:**
1. Ve a okla.com.do/buscar
2. Configura tus filtros (marca, modelo, precio, etc.)
3. Haz clic en "Guardar búsqueda"
4. Activa las alertas para esa búsqueda
5. Te notificaremos cuando aparezcan nuevos vehículos que coincidan

Tus alertas las puedes gestionar en:
Mi Cuenta → Alertas de Precio
```

---

## 22. Glosario de Términos

| Término                    | Definición                                                                       |
| -------------------------- | -------------------------------------------------------------------------------- |
| **OKLA**                   | Nombre de la plataforma/marketplace de vehículos en República Dominicana         |
| **Buyer / Comprador**      | Usuario que busca y compra vehículos                                             |
| **Seller / Vendedor**      | Persona individual que vende su vehículo                                         |
| **Dealer / Concesionario** | Empresa de venta de vehículos                                                    |
| **KYC**                    | "Know Your Customer" — Proceso de verificación de identidad                      |
| **2FA**                    | Autenticación de dos factores — segundo paso de seguridad al iniciar sesión      |
| **TOTP**                   | Código temporal generado por app authenticator                                   |
| **OAuth**                  | Inicio de sesión con proveedores externos (Google, Apple, etc.)                  |
| **Boost / Impulsar**       | Pagar para dar mayor visibilidad a una publicación                               |
| **Lead**                   | Persona interesada que hace una consulta sobre un vehículo                       |
| **CRM**                    | Gestión de relaciones con clientes — herramienta para gestionar leads            |
| **ITBIS**                  | Impuesto sobre Transferencia de Bienes Industrializados y Servicios (18% en RD)  |
| **RNC**                    | Registro Nacional del Contribuyente (identificación fiscal de empresas en RD)    |
| **Cédula**                 | Documento de identidad dominicano                                                |
| **DOP / RD$**              | Peso dominicano (moneda local)                                                   |
| **PWA**                    | Progressive Web App — la plataforma se puede instalar como app en tu dispositivo |
| **VIN**                    | Número de Identificación del Vehículo — código único de 17 caracteres            |
| **Liveness**               | Prueba de vida — verificación de que eres una persona real (no una foto/video)   |
| **Early Bird**             | Programa de adopción temprana con beneficios especiales                          |
| **Miembro Fundador**       | Badge permanente para los primeros usuarios del programa Early Bird              |
| **Dashboard**              | Panel de control con resumen de información y métricas                           |
| **Inventario**             | Lista de vehículos disponibles de un dealer                                      |
| **Importación Masiva**     | Subir múltiples vehículos a la vez mediante CSV/Excel                            |
| **Webhook**                | Notificación automática entre sistemas cuando ocurre un evento                   |
| **API**                    | Interfaz de programación que permite integrar sistemas externos                  |
| **Suscripción**            | Plan de pago recurrente (mensual) para dealers                                   |
| **Token de acceso**        | Credencial temporal que te identifica en la plataforma (24h)                     |
| **Token de refresco**      | Credencial que renueva tu sesión automáticamente (30 días)                       |
| **Código de recuperación** | Código de emergencia para acceder si pierdes 2FA                                 |
| **Sesión**                 | Conexión activa entre tu dispositivo y OKLA                                      |
| **Revocar**                | Cerrar/invalidar una sesión activa                                               |
| **CAPTCHA**                | Prueba de seguridad para verificar que no eres un robot                          |
| **Marketplace**            | Plataforma de compra-venta (como OKLA)                                           |
| **Sandbox**                | Ambiente de pruebas (no afecta datos reales)                                     |

---

## 📊 Resumen de Datos para Entrenamiento

### Estadísticas del Reporte

| Métrica                           | Valor                                                                             |
| --------------------------------- | --------------------------------------------------------------------------------- |
| Roles de usuario documentados     | 4 (Buyer, Seller, Dealer, Dealer Employee)                                        |
| Flujos de autenticación           | 7 (registro, login, 2FA, OAuth, recuperación, verificación email, setup password) |
| Páginas públicas                  | 20+                                                                               |
| Páginas de buyer                  | 13                                                                                |
| Páginas de seller                 | 10+                                                                               |
| Páginas de dealer                 | 25+                                                                               |
| Funciones de seguridad            | 10+ (2FA, sesiones, historial, OAuth linking, etc.)                               |
| Métodos de pago                   | 3 (tarjeta crédito, débito, transferencia)                                        |
| Planes de suscripción             | 3 (Starter, Pro, Enterprise)                                                      |
| Flujos conversacionales sugeridos | 6                                                                                 |
| Preguntas frecuentes              | 17                                                                                |
| Términos en glosario              | 35                                                                                |

### Temas Cubiertos para el Chatbot

- ✅ Registro y onboarding por tipo de usuario
- ✅ Login y autenticación (normal, OAuth, 2FA)
- ✅ Recuperación de contraseña
- ✅ Navegación completa de la plataforma
- ✅ Panel de cuenta del buyer
- ✅ Funcionalidades del seller (publicación, gestión, boost)
- ✅ Portal completo del dealer (inventario, CRM, analíticas, empleados, sucursales)
- ✅ Verificación KYC paso a paso
- ✅ Seguridad de la cuenta (2FA, sesiones, contraseñas, OAuth)
- ✅ Notificaciones y preferencias
- ✅ Mensajería entre usuarios
- ✅ Pagos, facturación y suscripciones
- ✅ Planes y precios
- ✅ Búsqueda normal y con IA
- ✅ Favoritos y comparaciones
- ✅ Reseñas
- ✅ Centro de ayuda y soporte
- ✅ PWA y acceso offline
- ✅ Flujos conversacionales listos para implementar
- ✅ Glosario de términos

### Temas Excluidos (fuera del alcance del chatbot)

- ❌ Información específica de vehículos en venta
- ❌ Inventarios de sellers o dealers
- ❌ Publicaciones específicas
- ❌ Precios de vehículos individuales
- ❌ Recomendaciones de vehículos
- ❌ Comparación de vehículos específicos
- ❌ Detalles técnicos de los microservicios
- ❌ Administración del panel admin (uso interno)

---

_Este reporte fue generado automáticamente a partir del análisis del código fuente de la plataforma OKLA._
_Fecha: 2 de marzo de 2026_
