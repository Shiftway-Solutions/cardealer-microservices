# 📘 Tutorial: Configuración del Modelo de Negocio OKLA en el Panel de Administración

**Versión:** 1.0  
**Última actualización:** Febrero 2026  
**Audiencia:** Administradores y SuperAdmins de la plataforma OKLA  
**URL del Panel:** `https://okla.com.do/admin`

---

## 📑 Tabla de Contenido

1. [Acceso al Panel de Administración](#1-acceso-al-panel-de-administración)
2. [Configuración de Planes de Suscripción (LIBRE, VISIBLE, PRO, ÉLITE)](#2-configuración-de-planes-de-suscripción)
3. [Gestión de Precios y Comisiones](#3-gestión-de-precios-y-comisiones)
4. [Configuración de Productos Publicitarios](#4-configuración-de-productos-publicitarios)
5. [Sistema de Créditos OKLA Coins](#5-sistema-de-créditos-okla-coins)
6. [Configuración de Proveedores de Pago](#6-configuración-de-proveedores-de-pago)
7. [Programa Early Bird](#7-programa-early-bird)
8. [Gestión de Banners y Contenido](#8-gestión-de-banners-y-contenido)
9. [Gestión de Dealers y Suscripciones](#9-gestión-de-dealers-y-suscripciones)
10. [Panel de Facturación y Revenue](#10-panel-de-facturación-y-revenue)
11. [Analytics y Métricas del Negocio](#11-analytics-y-métricas-del-negocio)
12. [Configuración del ChatAgent para Dealers](#12-configuración-del-chatagent-para-dealers)
13. [Moderación de Vehículos](#13-moderación-de-vehículos)
14. [Gestión de Promociones](#14-gestión-de-promociones)
15. [Configuración de Seguridad](#15-configuración-de-seguridad)
16. [Referencia Rápida de Valores Recomendados](#16-referencia-rápida-de-valores-recomendados)

---

## 1. Acceso al Panel de Administración

### 1.1 Requisitos Previos

- Cuenta con rol **Admin** o **SuperAdmin** en la plataforma OKLA
- Navegador actualizado (Chrome, Firefox, Edge, Safari)
- Credenciales de acceso proporcionadas por el SuperAdmin

### 1.2 Inicio de Sesión

1. Navega a **`https://okla.com.do/login`**
2. Ingresa tu correo electrónico de administrador (ej: `admin@okla.local`)
3. Ingresa tu contraseña
4. Serás redirigido automáticamente al **Dashboard de Administración** (`/admin`)

### 1.3 Roles y Permisos

| Rol | Acceso |
|-----|--------|
| **SuperAdmin** | Acceso total: configuración de plataforma, gestión de equipo, roles, secretos de pago |
| **Admin** | Gestión de dealers, usuarios, vehículos, moderación, reportes |
| **Moderator** | Moderación de vehículos, reviews, reportes de contenido |
| **Support** | Soporte al usuario, mensajes, tickets |
| **Analyst** | Analytics y reportes (solo lectura) |

> ⚠️ **Importante:** Solo los usuarios con rol **SuperAdmin** pueden modificar la configuración de precios, proveedores de pago y settings de la plataforma.

### 1.4 Dashboard Principal

Al acceder a `/admin`, verás el dashboard con:
- **Tarjetas de estadísticas**: Total de usuarios, vehículos activos, dealers registrados, MRR (Ingreso Mensual Recurrente)
- **Acciones pendientes**: Items de moderación, verificaciones KYC, reportes abiertos
- **Actividad reciente**: Últimas acciones realizadas en la plataforma
- **Accesos rápidos**: Links directos a las secciones más utilizadas

---

## 2. Configuración de Planes de Suscripción

### 2.1 Modelo de 4 Planes OKLA

OKLA utiliza un modelo freemium con 4 niveles diseñados para maximizar la adopción y conversión de dealers:

| Plan | Precio/mes | Objetivo | Margen OKLA |
|------|-----------|----------|-------------|
| **LIBRE** | $0 (gratis) | Adquisición masiva de dealers y listings | N/A (lead gen) |
| **VISIBLE** | $29/mes | Primer upgrade — visibilidad mejorada | 95% ($27.50) |
| **PRO** | $89/mes | Acceso a ChatAgent + analytics avanzados | 24% ($21) |
| **ÉLITE** | $199/mes | Todo ilimitado + showcase homepage | 47% a escala |

### 2.2 Configurar Precios de Planes

**Ruta:** `Panel Admin → Configuración → Precios y Comisiones`

1. Navega a **`/admin/configuracion`** en el menú lateral
2. En la sección **"Precios y Comisiones"** (icono 💲), localiza los campos de planes:

| Campo en el Panel | Clave de Configuración | Valor Recomendado |
|---|---|---|
| Plan Starter | `pricing.dealer_starter` | $29.00 (Plan VISIBLE) |
| Plan Pro | `pricing.dealer_pro` | $89.00 (Plan PRO) |
| Plan Enterprise | `pricing.dealer_enterprise` | $199.00 (Plan ÉLITE) |

3. Modifica los valores en el campo de moneda (prefijo RD$)
4. Haz clic en **"Guardar Cambios"** (botón verde en la parte superior)
5. Espera la confirmación: *"Configuración guardada exitosamente"*

> 💡 **Nota sobre el Plan LIBRE:** No requiere configuración de precio ya que es gratuito por defecto. Todos los dealers que se registren sin plan activo están automáticamente en el plan LIBRE.

### 2.3 Configurar Límites por Plan

En la misma sección de **"Precios y Comisiones"**, configura los límites:

| Campo | Clave | LIBRE | VISIBLE | PRO | ÉLITE |
|-------|-------|-------|---------|-----|-------|
| Vehículos máx. Starter | `pricing.starter_max_vehicles` | Ilimitado | 50 | — | — |
| Vehículos máx. Pro | `pricing.pro_max_vehicles` | — | — | 200 | — |
| Fotos máx. (gratis) | `pricing.free_max_photos` | 5 | — | — | — |
| Fotos máx. Starter | `pricing.starter_max_photos` | — | 15 | — | — |
| Fotos máx. Pro | `pricing.pro_max_photos` | — | — | 30 | — |
| Fotos máx. Enterprise | `pricing.enterprise_max_photos` | — | — | — | 50 |

**Pasos:**
1. En `/admin/configuracion`, busca la sección de **límites por plan**
2. Para cada plan, establece el número máximo de vehículos y fotos
3. Para el plan LIBRE, configura `pricing.free_max_photos` = **5** (fotos por vehículo)
4. Guarda los cambios

### 2.4 Características por Plan

Referencia de funcionalidades incluidas por plan:

| Funcionalidad | LIBRE | VISIBLE ($29) | PRO ($89) | ÉLITE ($199) |
|---|---|---|---|---|
| Publicar vehículos | ✅ Ilimitado | ✅ Ilimitado | ✅ Ilimitado | ✅ Ilimitado |
| Prioridad en búsquedas | Estándar | Media | Alta | Top |
| Vehículos destacados/mes | — | 3 | 10 | 25 |
| Créditos publicitarios/mes | — | $15 | $45 | $120 |
| Badge "Dealer Verificado OKLA" | — | ✅ | ✅ Dorado | ✅ Premium |
| Dashboard Analytics | — | Básico | Avanzado | Completo + exportar |
| ChatAgent Web | — | — | ✅ 500 conv/mes | ✅ Ilimitado |
| ChatAgent WhatsApp | — | — | ✅ 500 conv/mes | ✅ Ilimitado |
| Agendamiento de citas | — | — | ✅ | ✅ + recordatorios WA |
| Valoración IA (PricingAgent) | 1 gratis | 5/mes | Ilimitada | Ilimitada + PDF |
| Perfil público dealer | Básico | Mejorado | Premium | Premium + showcase |
| Soporte OKLA | FAQ | Email 48h | Chat 12h | Dedicado 4h |

---

## 3. Gestión de Precios y Comisiones

### 3.1 Acceder a la Configuración de Precios

**Ruta:** `/admin/configuracion` → Pestaña **"Precios y Comisiones"**

La página de configuración organiza los settings por categorías. Haz clic en la categoría **"Precios y Comisiones"** (icono 💲) para expandirla.

### 3.2 Precios de Publicaciones

| Campo | Clave | Descripción | Valor Sugerido |
|-------|-------|-------------|----------------|
| Publicación Básica | `pricing.basic_listing` | Publicación estándar (gratis para plan LIBRE) | $0.00 |
| Publicación Destacada | `pricing.featured_listing` | Badge dorado + prioridad (7 días) | $6.00 |
| Publicación Premium | `pricing.premium_listing` | Top resultados (30 días) | $20.00 |
| Plan Vendedor Premium | `pricing.seller_premium_price` | Plan mensual vendedor individual | $29.00 |
| Precio publicación individual | `pricing.individual_listing_price` | Vendedor no-dealer, por publicación | $0.00 |

### 3.3 Duración de Publicaciones

| Campo | Clave | Valor Sugerido |
|-------|-------|----------------|
| Duración publicación gratuita | `pricing.basic_listing_days` | 90 días |
| Duración publicación individual | `pricing.individual_listing_days` | 30 días |

### 3.4 Precios de Boosts

Los boosts permiten a los dealers impulsar la visibilidad de vehículos individuales:

| Campo | Clave | Valor Sugerido |
|-------|-------|----------------|
| Boost Básico | `pricing.boost_basic_price` | $2.50 |
| Boost Básico (días) | `pricing.boost_basic_days` | 3 |
| Boost Pro | `pricing.boost_pro_price` | $7.00 |
| Boost Pro (días) | `pricing.boost_pro_days` | 7 |
| Boost Premium | `pricing.boost_premium_price` | $20.00 |
| Boost Premium (días) | `pricing.boost_premium_days` | 30 |

### 3.5 Comisiones e Impuestos

| Campo | Clave | Valor Sugerido |
|-------|-------|----------------|
| Comisión plataforma (%) | `pricing.platform_commission` | 0 (OKLA no cobra comisión sobre ventas) |
| ITBIS (%) | `pricing.itbis_percentage` | 18 (tasa estándar RD) |
| Moneda | `pricing.currency` | DOP |

### 3.6 Configuración Early Bird

| Campo | Clave | Valor Sugerido |
|-------|-------|----------------|
| Descuento Early Bird (%) | `pricing.early_bird_discount` | 30 |
| Fecha límite Early Bird | `pricing.early_bird_deadline` | 2026-06-30 |
| Meses gratis Early Bird | `pricing.early_bird_free_months` | 3 |

**Pasos para guardar:**
1. Modifica los valores necesarios
2. Los campos de moneda muestran el prefijo **RD$** automáticamente
3. Presiona **"Guardar Cambios"** en la barra superior
4. Verifica que aparezca la notificación de éxito (toast verde)

---

## 4. Configuración de Productos Publicitarios

### 4.1 Catálogo de Productos

OKLA ofrece productos publicitarios que generan ingresos de margen casi puro (~100%). Estos se configuran desde dos secciones:

**Para precios:** `/admin/configuracion` → Precios y Comisiones  
**Para gestión visual:** `/admin/banners` y `/admin/promociones`

### 4.2 Tabla de Productos y Precios

| Producto | Precio/día | Precio/semana | Precio/mes | Costo OKLA |
|----------|-----------|---------------|-----------|------------|
| **Listing Destacado** (por vehículo) | $0.50 | $2.50 | $6.00 | ~$0 |
| **Posición Top 3** (por vehículo) | $1.50 | $7.00 | $20.00 | ~$0 |
| **Oferta del Día** (homepage + email) | $15.00 | N/A | N/A | ~$0 |
| **Banner Homepage** (máx. 3 simultáneos) | N/A | N/A | $120.00 | ~$0 |
| **Dealer Showcase** (directorio) | N/A | N/A | $50.00 | ~$0 |
| **Pack Alertas Email** | N/A | N/A | $35.00 | ~$0.10 |
| **PAQUETE VISIBILIDAD TOTAL** (bundle) | N/A | N/A | $175.00 | ~$0.10 |

### 4.3 Configurar Listings Destacados

Los listings destacados se gestionan desde el módulo de **Vehículos**:

1. Ve a **`/admin/vehiculos`**
2. Busca el vehículo que el dealer quiere destacar
3. Haz clic en el botón **"Destacar"** (icono ⭐)
4. El sistema asigna el badge dorado y prioriza en resultados
5. La duración depende del boost adquirido

### 4.4 Configurar Posición Top 3

Actualmente se gestiona a través de los boosts configurados en la sección de precios (§3.4). Cuando un dealer compra un Boost Premium, su vehículo aparece en las primeras posiciones de búsquedas relevantes.

### 4.5 Configurar Oferta del Día

1. Ve a **`/admin/banners`**
2. Crea un nuevo banner con posición **"Inline"**
3. Configura:
   - **Título:** Nombre del vehículo en oferta
   - **Imagen:** Foto principal del vehículo
   - **URL destino:** Link al listing del vehículo
   - **Fecha inicio/fin:** Un solo día
   - **Posición:** `inline` (aparece en la sección "Oferta del Día" del homepage)
4. Activa el toggle para publicar

### 4.6 Configurar Banners de Homepage

**Ruta:** `/admin/banners`

1. Haz clic en **"Nuevo Banner"**
2. Configura:
   - **Título:** Nombre del dealer/campaña
   - **Imagen:** Banner 728×90 px (formato recomendado)
   - **URL destino:** Link al perfil del dealer o landing
   - **Posición:** `hero` (homepage principal)
   - **Dispositivos:** Desktop, Mobile o ambos
   - **Fecha inicio:** Fecha de activación
   - **Fecha fin:** Fecha de expiración
3. Activa el toggle **"Activo"**
4. Guarda el banner

> ⚠️ **Límite:** Máximo 3 banners simultáneos en el homepage. Los banners rotan equitativamente.

### 4.7 Gestión de Banners Activos

En la vista de banners (`/admin/banners`), cada banner muestra:
- **Preview visual** de la imagen
- **Estado**: Activo/Inactivo (toggle)
- **Estadísticas**: Impresiones, Clics, CTR (Click-Through Rate)
- **Dispositivos**: Indicadores de Desktop/Mobile
- **Acciones**: Editar, Eliminar

---

## 5. Sistema de Créditos OKLA Coins

### 5.1 Concepto

OKLA Coins son créditos prepagados que los dealers compran para usar en cualquier producto publicitario. Los paquetes ofrecen bonificación por volumen.

### 5.2 Paquetes de Créditos

| Paquete | Créditos Base | Bonus | Total | Precio |
|---------|--------------|-------|-------|--------|
| **Pack Básico** | 2,500 | — | 2,500 | $25.00 |
| **Pack Intermedio** (+10%) | 5,000 | +500 | 5,500 | $50.00 |
| **Pack Profesional** (+20%) | 10,000 | +2,000 | 12,000 | $100.00 |
| **Pack Dealer** (+30%) | 25,000 | +7,500 | 32,500 | $250.00 |

### 5.3 Tabla de Conversión: Créditos ↔ Productos

| Producto | Créditos/día | Créditos/semana | Equivalencia |
|----------|-------------|----------------|--------------|
| Listing Destacado | 50 cr | 250 cr | 1 Pack Básico = 50 días destacado |
| Posición Top 3 | 150 cr | 750 cr | 1 Pack Básico = 16 días Top 3 |
| Oferta del Día | 1,500 cr | — | 1 Pack Intermedio = 3 Ofertas del Día |
| Dealer Showcase | — | — | 5,000 cr/mes ($50) |

### 5.4 Cómo Configurar OKLA Coins

> 📝 **Estado actual:** El sistema de OKLA Coins está diseñado pero pendiente de implementación completa en el backend. Los precios y conversiones se pueden pre-configurar en la sección de Configuración.

1. Ve a **`/admin/configuracion`** → **"Precios y Comisiones"**
2. En la sección futura de créditos, configura:
   - Precio por crédito base: $0.01 (100 créditos = $1)
   - Bonificación Pack Intermedio: 10%
   - Bonificación Pack Profesional: 20%
   - Bonificación Pack Dealer: 30%
3. Guarda los cambios

---

## 6. Configuración de Proveedores de Pago

### 6.1 Proveedores Soportados

OKLA integra con 6 proveedores de pago, priorizando opciones locales de República Dominicana:

| Proveedor | Tipo | Mercado | Estado |
|-----------|------|---------|--------|
| **Azul** 🇩🇴 | Pasarela local (VISA/MC) | RD Principal | Listo para configurar |
| **CardNET** 🇩🇴 | Pasarela local | RD Secundario | Listo para configurar |
| **PixelPay** 🌎 | Pasarela regional | Centroamérica/Caribe | Listo para configurar |
| **Fygaro** 🌎 | Pagos recurrentes | Suscripciones | Listo para configurar |
| **Stripe** 🌐 | Pasarela internacional | Internacional | Listo para configurar |
| **PayPal** 🌐 | Billetera digital | Internacional | Listo para configurar |

### 6.2 Configurar Azul (Proveedor Principal RD)

**Ruta:** `/admin/configuracion` → **"Facturación y Pagos"**

1. Localiza la sección **"Azul"** (colapsable)
2. Activa el toggle **"Habilitado"**
3. Configura:

| Campo | Clave | Descripción |
|-------|-------|-------------|
| Ambiente | `billing.azul_environment` | `sandbox` (pruebas) o `production` |
| Nombre del Comercio | `billing.azul_merchant_name` | "OKLA Marketplace SRL" |
| Merchant ID | `billing.azul_merchant_id` | Proporcionado por Azul |
| API Key | `billing.azul_api_key` | 🔒 Campo secreto — clic en 👁️ para ver |
| Tipo de Comercio | `billing.azul_merchant_type` | Tipo MCC proporcionado por Azul |
| Código de Moneda | `billing.azul_currency_code` | `214` (DOP) |

4. Haz clic en **"Guardar"**
5. Los campos secretos (API Key) se guardan de forma encriptada

> ⚠️ **Importante:** Comienza siempre en ambiente `sandbox` para pruebas. Cambia a `production` solo cuando haya sido verificado por el equipo de pagos.

### 6.3 Configurar CardNET (Backup RD)

1. Localiza la sección **"CardNET"**
2. Activa el toggle
3. Configura:

| Campo | Clave |
|-------|-------|
| Ambiente | `billing.cardnet_environment` |
| API Key | `billing.cardnet_api_key` 🔒 |
| Terminal ID | `billing.cardnet_terminal_id` |
| Merchant ID | `billing.cardnet_merchant_id` |

### 6.4 Configurar PixelPay

1. Localiza la sección **"PixelPay"**
2. Activa el toggle
3. Configura:

| Campo | Clave |
|-------|-------|
| Ambiente | `billing.pixelpay_environment` |
| Public Key | `billing.pixelpay_public_key` 🔒 |
| Secret Key | `billing.pixelpay_secret_key` 🔒 |
| Webhook Secret | `billing.pixelpay_webhook_secret` 🔒 |

### 6.5 Configurar Fygaro (Suscripciones Recurrentes)

1. Localiza la sección **"Fygaro"**
2. Activa el toggle
3. Configura:

| Campo | Clave |
|-------|-------|
| Ambiente | `billing.fygaro_environment` |
| API Key | `billing.fygaro_api_key` 🔒 |
| Merchant ID | `billing.fygaro_merchant_id` |
| Webhook Secret | `billing.fygaro_webhook_secret` 🔒 |
| Habilitar suscripciones | `billing.fygaro_enable_subscriptions` |

> 💡 **Recomendación:** Fygaro es ideal para las suscripciones recurrentes de los planes dealer (VISIBLE, PRO, ÉLITE) porque maneja automáticamente los cobros mensuales.

### 6.6 Configurar Stripe (Internacional)

1. Localiza la sección **"Stripe"**
2. Activa el toggle
3. Configura:

| Campo | Clave |
|-------|-------|
| Ambiente | `billing.stripe_environment` |
| Publishable Key | `billing.stripe_publishable_key` |
| Secret Key | `billing.stripe_secret_key` 🔒 |
| Webhook Secret | `billing.stripe_webhook_secret` 🔒 |
| Días de prueba | `billing.stripe_trial_days` |

### 6.7 Configurar PayPal

1. Localiza la sección **"PayPal"**
2. Activa el toggle
3. Configura:

| Campo | Clave |
|-------|-------|
| Ambiente | `billing.paypal_environment` |
| Client ID | `billing.paypal_client_id` |
| Client Secret | `billing.paypal_client_secret` 🔒 |

### 6.8 Seguridad de Credenciales de Pago

- Todos los campos marcados con 🔒 son **secretos** que se almacenan encriptados
- Para ver un valor secreto, haz clic en el icono 👁️ junto al campo
- Para editar un secreto, haz clic en el botón de **"Editar"** junto al campo
- Los secretos se guardan a través de un endpoint separado y encriptado (`/api/admin/configuration/secrets`)
- **Nunca** compartas los valores de las API Keys fuera del panel de administración

---

## 7. Programa Early Bird

### 7.1 Concepto

El programa Early Bird ofrece beneficios especiales a los primeros dealers que se unan a OKLA, incentivando la adopción temprana.

### 7.2 Configurar el Programa

**Ruta:** `/admin/early-bird`

1. Navega a **`/admin/early-bird`** en el menú lateral
2. Verás el estado actual del programa (Activo/Inactivo)
3. Haz clic en **"Configurar"** para ajustar:

| Parámetro | Descripción | Valor Recomendado |
|-----------|-------------|-------------------|
| **Espacios totales** | Número máximo de dealers Early Bird | 50 |
| **Descuento (%)** | Porcentaje de descuento permanente | 30% |
| **Lock-in (meses)** | Tiempo mínimo de permanencia | 12 meses |

4. El panel muestra una **barra de progreso** con los espacios usados vs. disponibles

### 7.3 Gestionar Miembros Early Bird

En la sección inferior de la página, una tabla muestra:

| Columna | Descripción |
|---------|-------------|
| Dealer | Nombre del dealer |
| Plan | Plan contratado (VISIBLE/PRO/ÉLITE) |
| Fecha de Ingreso | Cuándo se unió al programa |
| Descuento | Porcentaje aplicado |
| Ahorro/Mes | Cuánto ahorra el dealer |
| Estado | Activo/Expirado/Cancelado |

### 7.4 Configurar en Settings Generales

Los valores también se pueden ajustar en `/admin/configuracion`:

- `pricing.early_bird_discount` → 30 (%)
- `pricing.early_bird_deadline` → Fecha límite para unirse
- `pricing.early_bird_free_months` → 3 (meses gratis antes de empezar a cobrar)

---

## 8. Gestión de Banners y Contenido

### 8.1 Panel de Banners

**Ruta:** `/admin/banners`

La vista de banners muestra una cuadrícula con todos los banners creados. Cada tarjeta muestra:

- **Preview:** Vista previa de la imagen del banner (aspect-ratio video)
- **Badge de estado:** Activo (verde) / Inactivo (gris)
- **Badge de posición:** Hero, Sidebar, Inline, Popup
- **Toggle:** Activar/desactivar rápidamente
- **Estadísticas:** Impresiones, Clics, CTR
- **Dispositivos:** Indicadores de compatibilidad (Desktop/Mobile)

### 8.2 Crear un Nuevo Banner

1. En `/admin/banners`, haz clic en **"Nuevo Banner"**
2. Rellena el formulario:

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| Título | Nombre interno del banner | "Banner Toyota Premium Motors" |
| Imagen | Upload de imagen (728×90, 300×250, o responsive) | — |
| URL Destino | Link al hacer clic | `https://okla.com.do/dealer/toyota-premium` |
| Posición | Ubicación en la web | `hero` / `sidebar` / `inline` / `popup` |
| Fecha Inicio | Cuándo se activa | 2026-03-01 |
| Fecha Fin | Cuándo expira | 2026-03-31 |
| Desktop | Mostrar en escritorio | ✅ |
| Mobile | Mostrar en móvil | ✅ |

3. Guarda y activa el banner

### 8.3 Gestión de Contenido

**Ruta:** `/admin/contenido`

La página de contenido agrupa tres secciones:
- **Banners:** Acceso rápido a la gestión de banners
- **Páginas estáticas:** Editar páginas como "Términos", "Privacidad", "Sobre Nosotros"
- **Blog:** Gestión de artículos del blog OKLA

---

## 9. Gestión de Dealers y Suscripciones

### 9.1 Lista de Dealers

**Ruta:** `/admin/dealers`

La página de dealers muestra una vista completa con:

**Estadísticas superiores (5 tarjetas):**
- Total de dealers registrados
- Dealers activos
- Dealers pendientes de verificación
- Dealers Enterprise (plan ÉLITE)
- MRR (Monthly Recurring Revenue) total

**Filtros disponibles:**
- 🔍 **Búsqueda:** Por nombre, email o RNC del dealer
- 📊 **Estado:** Todos / Activo / Pendiente / Suspendido
- 💰 **Plan:** Starter (VISIBLE) / Pro / Enterprise (ÉLITE)
- ✅ **Verificado:** Filtrar por estado de verificación

### 9.2 Acciones sobre Dealers

| Acción | Disponible cuando | Descripción |
|--------|-------------------|-------------|
| **Ver detalle** | Siempre | Navega al perfil completo del dealer |
| **Verificar** | Estado = Pendiente | Marca al dealer como verificado (badge) |
| **Suspender** | Estado = Activo | Suspende al dealer con motivo obligatorio |
| **Reactivar** | Estado = Suspendido | Restaura acceso al dealer |

### 9.3 Detalle del Dealer

**Ruta:** `/admin/dealers/[id]`

En la vista de detalle de cada dealer puedes ver:
- Información del negocio (nombre, dirección, RNC, contacto)
- Plan actual y fecha de renovación
- Listado de vehículos publicados
- Estadísticas de rendimiento (vistas, leads, conversiones)
- Historial de pagos
- Rating promedio de reviews

### 9.4 Panel de Suscripciones

**Ruta:** `/admin/suscripciones`

Este panel muestra un resumen de todas las suscripciones activas:

**Tarjetas de estadísticas:**
- **Total de suscripciones**
- **Suscripciones activas** (con porcentaje)
- **MRR** en RD$ (con cálculo anualizado)
- **Tasa de conversión** (dealers pagos / dealers totales)

**Distribución por plan:**
3 tarjetas con el desglose de dealers en cada plan:
- VISIBLE ($29) — cantidad y porcentaje
- PRO ($89) — cantidad y porcentaje
- ÉLITE ($199) — cantidad y porcentaje

**Tabla de suscripciones:**

| Columna | Descripción |
|---------|-------------|
| Dealer | Nombre del dealer |
| Plan | VISIBLE / PRO / ÉLITE |
| Ciclo | Mensual / Anual |
| Monto | Precio pagado |
| Vehículos | Cantidad publicada |
| Estado | Activa / Trial / Vencida / Suspendida / Cancelada |
| Próximo Cobro | Fecha del siguiente cobro |

**Filtros:** Buscar por nombre + filtrar por plan + filtrar por estado

### 9.5 Flujo de Verificación de Dealer

1. Un dealer se registra → Aparece en `/admin/dealers` con estado **"Pendiente"**
2. El admin revisa los documentos del dealer en `/admin/kyc/[id]`
3. Si todo está correcto, hace clic en **"Verificar"** en la lista de dealers
4. El dealer recibe el badge **"Dealer Verificado OKLA"**
5. El tipo de badge depende del plan:
   - VISIBLE → Badge estándar ✓
   - PRO → Badge Dorado ✓✓
   - ÉLITE → Badge Premium ✓✓✓

---

## 10. Panel de Facturación y Revenue

### 10.1 Dashboard de Facturación

**Ruta:** `/admin/facturacion`

El panel de facturación muestra métricas financieras en tiempo real:

**Tarjetas de revenue (4 métricas principales):**

| Métrica | Descripción | Color |
|---------|-------------|-------|
| **MRR** | Ingreso Mensual Recurrente | Verde (gradiente primario) |
| **ARR** | Ingreso Anual Recurrente (MRR × 12) | Azul |
| **Suscripciones Activas** | Número de suscripciones pagando | Púrpura |
| **Churn Rate** | Tasa de cancelación mensual | Rojo/Verde |

### 10.2 Transacciones Recientes

La sección de transacciones muestra las últimas operaciones:

| Campo | Descripción |
|-------|-------------|
| Dealer | Nombre del dealer que pagó |
| Plan | Plan suscrito |
| Fecha | Fecha de la transacción |
| Monto | Cantidad pagada |
| Estado | Completado ✅ / Pendiente ⏳ / Fallido ❌ |

### 10.3 Pagos Pendientes

Sección especial (fondo ámbar) que destaca pagos que no se han procesado:
- Nombre del dealer
- Monto adeudado
- Fecha de vencimiento
- Días de atraso
- Botón **"Enviar Recordatorios"** para notificar al dealer

### 10.4 Revenue por Plan

Desglose visual de ingresos por cada plan:
- Dot de color + nombre del plan + monto de revenue
- Porcentaje del ingreso total
- Número de suscriptores

### 10.5 Exportar Reportes

1. Haz clic en el botón **"Exportar"** en la esquina superior
2. Selecciona formato: **PDF** o **CSV**
3. El sistema genera y descarga el reporte de facturación

---

## 11. Analytics y Métricas del Negocio

### 11.1 Dashboard de Analytics

**Ruta:** `/admin/analytics`

El panel de analytics proporciona visibilidad completa del rendimiento:

### 11.2 Métricas Disponibles

| Sección | Endpoint | Datos |
|---------|----------|-------|
| **Overview** | `/api/admin/analytics/overview` | Resumen general de la plataforma |
| **Tráfico Semanal** | `/api/admin/analytics/weekly` | Visitantes por día de la semana |
| **Top Vehículos** | `/api/admin/analytics/top-vehicles` | Vehículos más buscados/vistos |
| **Fuentes de Tráfico** | `/api/admin/analytics/traffic-sources` | Orgánico, social, directo, referral |
| **Dispositivos** | `/api/admin/analytics/devices` | Desktop vs. Mobile vs. Tablet |
| **Conversiones** | `/api/admin/analytics/conversions` | Tasas de conversión por embudo |
| **Canales de Revenue** | `/api/admin/analytics/revenue-channels` | Ingresos por canal |

### 11.3 KPIs Clave para el Modelo de Negocio

Monitorea estos indicadores para medir la salud del negocio:

| KPI | Fórmula | Meta |
|-----|---------|------|
| **MRR** | Suma de suscripciones activas | > $2,215 (break-even) |
| **Tasa de conversión Free→Paid** | Dealers pagos / Dealers totales | > 25% |
| **ARPU** | MRR / Dealers pagos | > $60 |
| **Churn mensual** | Cancelaciones / Suscripciones activas | < 5% |
| **Dealers activos** | Dealers con al menos 1 listing activo | > 45 (break-even) |
| **Listings por dealer** | Total listings / Total dealers | > 20 |
| **Vistas por listing** | Total vistas / Total listings | > 50/mes |
| **Lead rate** | Contactos / Vistas | > 3% |

### 11.4 Exportar Analytics

1. Haz clic en **"Exportar Reporte"** en la esquina superior
2. Selecciona el rango de fechas
3. Formato disponible: PDF con gráficos y tablas

---

## 12. Configuración del ChatAgent para Dealers

### 12.1 Concepto

El DealerChatAgent es una IA que responde consultas de compradores **en nombre del dealer**, utilizando únicamente la información del inventario de ese dealer específico.

### 12.2 Límites por Plan

| Plan | ChatAgent Web | ChatAgent WhatsApp | Human Handoff |
|------|--------------|--------------------|----|
| LIBRE | ❌ | ❌ | ❌ |
| VISIBLE | ❌ | ❌ | ❌ |
| PRO | ✅ 500 conv/mes | ✅ 500 conv/mes | Email alert |
| ÉLITE | ✅ Ilimitado | ✅ Ilimitado | Live chat + CRM |

### 12.3 Cómo Funciona

1. Un comprador visita el listing de un vehículo de un dealer con plan PRO o ÉLITE
2. Ve el botón **"Chatear con [Nombre del Dealer] (IA)"**
3. Al hacer clic, se abre el ChatAgent que:
   - Responde **en nombre del dealer** (no de OKLA)
   - Solo conoce y habla sobre **el inventario del dealer**
   - Puede agendar citas y test drives
   - Escala a un humano si el comprador lo solicita

### 12.4 Gestión desde el Admin

**Ruta:** `/admin/search-agent` (módulo de agentes IA)

Desde esta sección puedes:
- Ver el uso de conversaciones por dealer
- Monitorear el consumo de API (Claude)
- Ver alertas cuando un dealer PRO se acerca al límite de 500 conversaciones
- Configurar el límite soft de 2,000 conversaciones para plan ÉLITE
- Ver métricas de satisfacción del ChatAgent

### 12.5 Costos de API

| Plan | Costo estimado OKLA/mes | Incluido en plan |
|------|------------------------|------------------|
| PRO (500 conv) | ~$68 | Sí ($89 plan) |
| ÉLITE (ilimitado) | ~$228 máx | Sí ($199 plan) |

> 💡 **Nota sobre margen ÉLITE:** El plan ÉLITE puede tener margen negativo al inicio (-15%), pero con caching optimizado y mayor volumen, el margen se normaliza al 47%. Se recomienda un límite soft de 2,000 conversaciones/mes para ÉLITE con cobro de $0.08/conversación adicional.

---

## 13. Moderación de Vehículos

### 13.1 Cola de Moderación

**Ruta:** `/admin/vehiculos`

**Estadísticas superiores:**
- Total de vehículos
- Vehículos activos
- Pendientes de aprobación
- Vehículos destacados

**Filtros:**
- 🔍 Búsqueda por marca, modelo, título
- 📊 Estado: Pendiente / Aprobado / Rechazado
- 👤 Tipo de vendedor: Particular / Dealer
- ⭐ Destacado: Sí / No

### 13.2 Acciones de Moderación

| Acción | Descripción |
|--------|-------------|
| **Aprobar** | Publica el vehículo en la plataforma |
| **Rechazar** | Devuelve al vendedor con motivo |
| **Destacar** | Activa/desactiva el badge de destacado |
| **Eliminar** | Soft-delete del vehículo |

### 13.3 Flujo de Moderación

1. Un vendedor/dealer publica un vehículo → Entra en la cola de moderación
2. El moderador revisa: fotos, descripción, precio, categoría
3. Si cumple las políticas → **Aprobar** → El vehículo aparece en la plataforma
4. Si no cumple → **Rechazar** → El vendedor recibe notificación con el motivo

---

## 14. Gestión de Promociones

### 14.1 Panel de Promociones

**Ruta:** `/admin/promociones`

### 14.2 Tipos de Promociones

| Tipo | Descripción | Ejemplo |
|------|-------------|---------|
| **Promoción general** | Descuento en suscripciones | "30% off primer mes" |
| **Descuento** | Reducción de precio directo | "$10 off Plan VISIBLE" |
| **Bundle** | Paquete de productos | "Paquete Visibilidad Total" |
| **Early Bird** | Para primeros adoptantes | "3 meses gratis Early Bird" |

### 14.3 Crear una Promoción

1. En `/admin/promociones`, haz clic en **"Nueva Promoción"**
2. Configura:

| Campo | Descripción |
|-------|-------------|
| Nombre | Nombre de la promoción |
| Descripción | Texto explicativo |
| Tipo | Promoción / Descuento / Bundle / Early Bird |
| Código | Código promocional (ej: `OKLA30`) |
| Descuento (%) | Porcentaje de descuento |
| Monto máx. descuento | Tope del descuento en $ |
| Usos máximos | Cuántas veces se puede usar |
| Fecha inicio | Cuándo se activa |
| Fecha fin | Cuándo expira |
| Plan aplicable | A qué plan(es) aplica |

3. Guarda y activa la promoción

### 14.4 Monitorear Promociones

Cada promoción muestra:
- **Estado:** Activa / Programada / Expirada / Pausada / Finalizada
- **Impresiones:** Cuántas veces se mostró
- **Clics:** Cuántas veces se hizo clic
- **Conversiones:** Cuántas veces se usó efectivamente
- **Acciones:** Editar, Pausar/Reanudar, Eliminar

---

## 15. Configuración de Seguridad

### 15.1 Settings de Seguridad

**Ruta:** `/admin/configuracion` → Categoría **"Seguridad"**

Los settings de seguridad incluyen:
- Rate limiting
- CORS
- JWT configuration
- Captcha settings
- IP whitelisting/blacklisting

### 15.2 Feature Flags

Los feature flags permiten activar/desactivar funcionalidades sin desplegar código:

**Ruta:** `/admin/configuracion` → Sección **"Feature Flags"**

Cada feature flag muestra:
- Nombre de la funcionalidad
- Toggle de activación
- Porcentaje de rollout (si aplica)

### 15.3 Logs y Auditoría

**Ruta:** `/admin/logs`

Registra todas las acciones realizadas por administradores:
- Quién realizó la acción
- Qué acción (crear, modificar, eliminar, aprobar, rechazar)
- Sobre qué entidad (dealer, vehículo, usuario)
- Valores antes/después del cambio
- IP del administrador
- Timestamp

---

## 16. Referencia Rápida de Valores Recomendados

### 16.1 Configuración Inicial Mínima (Fase 1: Lanzamiento)

```
# Planes
pricing.dealer_starter     = $0    (Plan LIBRE - gratis)
pricing.dealer_pro         = $0    (Fase 1: todos gratis)
pricing.dealer_enterprise  = $0    (Fase 1: todos gratis)

# Publicaciones
pricing.basic_listing      = $0    (Gratis para todos)
pricing.featured_listing   = $0    (Gratis en Fase 1)
pricing.basic_listing_days = 90    (3 meses de duración)

# Límites generosos
pricing.starter_max_vehicles = 999  (Sin límite real)
pricing.free_max_photos      = 10   (Generoso)

# Impuestos
pricing.itbis_percentage     = 18   (ITBIS estándar RD)
pricing.currency             = DOP
```

### 16.2 Configuración Fase 2 (Monetización)

```
# Planes (activar precios)
pricing.dealer_starter     = $29   (Plan VISIBLE)
pricing.dealer_pro         = $89   (Plan PRO)
pricing.dealer_enterprise  = $199  (Plan ÉLITE)

# Publicaciones
pricing.basic_listing      = $0    (Siempre gratis)
pricing.featured_listing   = $6    (Por mes)
pricing.premium_listing    = $20   (Top 3 por mes)

# Boosts
pricing.boost_basic_price  = $2.50
pricing.boost_basic_days   = 3
pricing.boost_pro_price    = $7.00
pricing.boost_pro_days     = 7
pricing.boost_premium_price = $20.00
pricing.boost_premium_days  = 30

# Límites por plan
pricing.starter_max_vehicles = 50   (VISIBLE)
pricing.pro_max_vehicles     = 200  (PRO)
pricing.free_max_photos      = 5    (LIBRE)
pricing.starter_max_photos   = 15   (VISIBLE)
pricing.pro_max_photos       = 30   (PRO)
pricing.enterprise_max_photos = 50  (ÉLITE)

# Early Bird
pricing.early_bird_discount    = 30
pricing.early_bird_free_months = 3
```

### 16.3 Fases del Negocio

| Fase | Período | Acción en Admin | Meta |
|------|---------|----------------|------|
| **F1** | Meses 1-3 | Todos los planes en $0. Solo captar dealers. | 50 dealers, 500 listings |
| **F2** | Meses 4-6 | Activar VISIBLE y PRO. 30 días gratis. Activar boosts. | 100 dealers, 2,000 listings |
| **F3** | Meses 7-12 | Activar ÉLITE. Activar ChatAgent. Activar banners. | 200 dealers, 8,000 listings |
| **F4** | Año 2+ | Todos los productos activos. Servicios financieros. | 400+ dealers, 25,000+ listings |

### 16.4 Break-Even Reference

| Escenario | Dealers Necesarios | MRR Necesario |
|-----------|-------------------|---------------|
| **Break-even marketplace** | 45 dealers activos | $2,215/mes |
| **Rentabilidad + ChatAgent** | 50 dealers | $2,059+ neto |
| **Escala completa** | 200 dealers | $18,260+ neto |

---

## 📌 Checklist de Configuración Inicial

- [ ] Acceder al panel admin con cuenta SuperAdmin
- [ ] Configurar datos generales de la plataforma (`/admin/configuracion` → General)
- [ ] Configurar precios de planes según la fase actual (F1 = todo gratis)
- [ ] Configurar límites por plan (vehículos, fotos)
- [ ] Configurar duración de publicaciones
- [ ] Habilitar al menos un proveedor de pago (Azul para RD)
- [ ] Configurar impuestos (ITBIS 18%)
- [ ] Configurar programa Early Bird (si aplica)
- [ ] Verificar que los feature flags estén configurados correctamente
- [ ] Crear los primeros banners para el homepage
- [ ] Verificar los primeros dealers registrados
- [ ] Revisar la cola de moderación de vehículos
- [ ] Verificar que el dashboard de analytics muestra datos

---

## 🔗 Links Rápidos del Panel Admin

| Sección | URL | Descripción |
|---------|-----|-------------|
| Dashboard | `/admin` | Vista general |
| Configuración | `/admin/configuracion` | Settings de la plataforma |
| Dealers | `/admin/dealers` | Gestión de dealers |
| Suscripciones | `/admin/suscripciones` | Suscripciones activas |
| Facturación | `/admin/facturacion` | Revenue y pagos |
| Vehículos | `/admin/vehiculos` | Moderación de vehículos |
| Analytics | `/admin/analytics` | Métricas y reportes |
| Banners | `/admin/banners` | Publicidad visual |
| Promociones | `/admin/promociones` | Códigos y campañas |
| Early Bird | `/admin/early-bird` | Programa early adopters |
| Contenido | `/admin/contenido` | Páginas y blog |
| KYC | `/admin/kyc` | Verificación de identidad |
| Reportes | `/admin/reportes` | Reportes de contenido |
| Reviews | `/admin/reviews` | Moderación de reviews |
| Usuarios | `/admin/usuarios` | Gestión de usuarios |
| Equipo | `/admin/equipo` | Equipo interno |
| Roles | `/admin/roles` | Permisos y roles |
| Mensajes | `/admin/mensajes` | Inbox del admin |
| Soporte | `/admin/soporte` | Tickets de soporte |
| Logs | `/admin/logs` | Auditoría |
| Sistema | `/admin/sistema` | Salud del sistema |
| Compliance | `/admin/compliance` | Cumplimiento |
| Mantenimiento | `/admin/mantenimiento` | Modo mantenimiento |
| Search Agent | `/admin/search-agent` | Agente IA de búsqueda |

---

*Tutorial creado por el equipo de desarrollo OKLA — Febrero 2026*
*Para soporte técnico, contactar al equipo de desarrollo vía Slack #okla-dev*
