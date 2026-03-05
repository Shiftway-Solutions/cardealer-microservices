# 📱 OKLA Mobile App — Documentación Completa

**Versión:** 2.0.0  
**Plataforma:** Flutter (iOS & Android)  
**Última actualización:** 2026-02-22

---

## 1. Visión General

OKLA Mobile es la aplicación móvil nativa del marketplace de vehículos #1 de República Dominicana. Construida con Flutter para iOS y Android, ofrece paridad completa con la plataforma web y experiencias optimizadas para móvil como notificaciones push, autenticación biométrica, y modo offline.

### 1.1 Propuesta de Valor

| Característica | Descripción |
|---|---|
| **Marketplace completo** | Buscar, comparar, contactar, comprar/vender vehículos |
| **Panel de Dealer** | Gestión completa de inventario, leads, empleados, facturación |
| **IA Integrada** | Chatbot, importación inteligente, scoring de vehículos |
| **Notificaciones Push** | Alertas de precio, mensajes, leads, actividad |
| **Modo Offline** | Favoritos, búsquedas guardadas, historial sin conexión |
| **Seguridad** | Biometría, 2FA, JWT con refresh, SSL pinning |

---

## 2. Arquitectura Técnica

### 2.1 Clean Architecture

```
lib/
├── core/                    # Configuración global, temas, rutas
│   ├── config/              # App config, environment, API endpoints
│   ├── constants/           # Colores, dimensiones, strings
│   ├── di/                  # Inyección de dependencias (GetIt)
│   ├── errors/              # Manejo de errores (Failures, Exceptions)
│   ├── network/             # API client, interceptors, SSL pinning
│   ├── router/              # GoRouter navegación declarativa
│   ├── theme/               # Tema OKLA (light/dark)
│   └── utils/               # Utilidades globales
├── data/                    # Capa de datos
│   ├── datasources/         # APIs remotas y almacenamiento local
│   ├── models/              # Modelos JSON (fromJson/toJson)
│   └── repositories/        # Implementaciones de repositorios
├── domain/                  # Capa de dominio (pura)
│   ├── entities/            # Entidades de negocio
│   ├── repositories/        # Interfaces de repositorios
│   └── usecases/            # Casos de uso
├── presentation/            # Capa de presentación
│   ├── blocs/               # BLoC state management
│   ├── pages/               # Pantallas completas
│   ├── widgets/             # Widgets reutilizables
│   └── shared/              # Componentes compartidos
├── l10n/                    # Internacionalización (es/en)
└── main.dart                # Entry point
```

### 2.2 Stack Tecnológico

| Categoría | Tecnología | Versión |
|---|---|---|
| **Framework** | Flutter | 3.38+ |
| **Lenguaje** | Dart | 3.10+ |
| **State Management** | flutter_bloc | ^8.1 |
| **DI** | get_it + injectable | ^8.0 / ^2.5 |
| **Networking** | dio + retrofit | ^5.7 / ^4.4 |
| **Navegación** | go_router | ^14.0 |
| **Cache Local** | hive_flutter | ^2.0 |
| **Seguro** | flutter_secure_storage | ^9.2 |
| **Push** | firebase_messaging | ^15.2 |
| **Analytics** | firebase_analytics | ^11.4 |
| **Crash Report** | firebase_crashlytics | ^4.2 |
| **Pagos** | flutter_stripe | ^11.4 |
| **Mapas** | google_maps_flutter | ^2.14 |
| **Biometría** | local_auth | ^3.0 |
| **Imágenes** | cached_network_image | ^3.4 |
| **Linting** | flutter_lints | ^3.0 |

### 2.3 Conexión con Backend

```
Mobile App → HTTPS → okla.com.do/api/* → BFF (Next.js) → Gateway (Ocelot) → Microservices
```

- **Base URL Producción:** `https://okla.com.do/api`
- **Base URL Staging:** `https://staging.okla.com.do/api`
- **Autenticación:** JWT Bearer token en headers
- **CSRF:** Token en cookie, enviado en header `X-CSRF-Token`
- **SSL Pinning:** Certificado de okla.com.do pinned

---

## 3. Funcionalidades por Módulo

### 3.1 🔐 Autenticación & Seguridad

| Feature | Descripción |
|---|---|
| Login email/password | Con validación y rate limiting |
| Login social | Google Sign-In, Apple Sign-In |
| Registro (Buyer) | Formulario con validación |
| Registro (Dealer) | Flujo de registro dealer |
| Verificación de email | Pantalla de verificación |
| Recuperación de contraseña | Flujo completo |
| 2FA (TOTP) | Configuración y verificación |
| Autenticación biométrica | Face ID / Touch ID / Fingerprint |
| Gestión de sesiones | Ver y revocar sesiones activas |
| SSL Pinning | Certificate pinning para API |
| Token refresh | Automático con interceptor |
| Logout seguro | Limpia tokens y datos sensibles |

### 3.2 🏠 Home & Descubrimiento

| Feature | Descripción |
|---|---|
| Hero carousel | Banners destacados con animación |
| Búsqueda NLP | Búsqueda en lenguaje natural |
| Categorías | Cards por tipo de carrocería |
| Vehículos destacados | Grid premium/featured |
| Marcas populares | Slider horizontal de marcas |
| Sección de dealers | Promociones de dealers |
| Por qué OKLA | Features grid |
| CTA Section | Llamadas a la acción |

### 3.3 🔍 Búsqueda & Filtros

| Feature | Descripción |
|---|---|
| Búsqueda avanzada | Múltiples filtros combinados |
| Filtros: marca, modelo, año | Dropdown con autocompletado |
| Filtros: precio (rango) | Slider dual con formato moneda |
| Filtros: tipo combustible | Gasolina, Diesel, Híbrido, Eléctrico |
| Filtros: transmisión | Automática, Manual, CVT |
| Filtros: ubicación/provincia | Todas las provincias RD |
| Filtros: condición | Nuevo, Usado, Certificado |
| Filtros: color, carrocería | Multiselección |
| Ordenamiento | Precio, fecha, relevancia, km |
| Búsquedas guardadas | Con notificaciones automáticas |
| Historial de búsqueda | Últimas búsquedas |

### 3.4 🚗 Detalle de Vehículo

| Feature | Descripción |
|---|---|
| Galería de fotos | Fullscreen, zoom, swipe |
| Vista 360° | Visualización interactiva |
| Especificaciones | Tabla completa de specs |
| OKLA Score | Puntuación con desglose |
| Precio deal rating | Excelente/Bueno/Justo/Alto |
| Ubicación en mapa | Google Maps integrado |
| Contactar vendedor | Mensaje directo |
| Agendar cita | Calendario de disponibilidad |
| Compartir | Share nativo (link, WhatsApp, etc.) |
| Reportar | Reportar anuncio |
| Vehículos similares | Grid de sugerencias |
| Historial de precios | Gráfico de evolución |

### 3.5 ❤️ Favoritos & Comparaciones

| Feature | Descripción |
|---|---|
| Guardar favorito | Con animación y haptic feedback |
| Lista de favoritos | Grid con filtros |
| Comparar vehículos | Hasta 4 side-by-side |
| Compartir comparación | Link de comparación |

### 3.6 📨 Mensajería

| Feature | Descripción |
|---|---|
| Lista de conversaciones | Inbox con preview y unread count |
| Chat en tiempo real | Mensajes con timestamps |
| Enviar texto | Con validación |
| Agendar visita | Desde chat |
| Archivar conversación | Gestión de inbox |
| Notificaciones | Push para mensajes nuevos |

### 3.7 🔔 Notificaciones

| Feature | Descripción |
|---|---|
| Push notifications | Firebase Cloud Messaging |
| Centro de notificaciones | Lista con estados read/unread |
| Alertas de precio | Notificación cuando baja precio |
| Alertas de búsqueda | Nuevos vehículos matching |
| Preferencias | Granular: email, push, in-app |
| Badges | Contador en tab bar |

### 3.8 🏷️ Publicar Vehículo (Seller)

| Feature | Descripción |
|---|---|
| Wizard multipaso | Paso a paso con progreso |
| Datos básicos | Marca, modelo, año, precio |
| Decodificar VIN | Autocompletado por VIN |
| Fotos | Cámara + galería, reordenar |
| AI Background Removal | Remover fondo de fotos |
| Descripción | Editor con sugerencias |
| Ubicación | Mapa para pin de ubicación |
| Preview | Vista previa del anuncio |
| Publicar | Con pago si aplica |
| Importar desde plataformas | Facebook, Corotos, SuperCarros |

### 3.9 📊 Dashboard Seller

| Feature | Descripción |
|---|---|
| Resumen | Stats rápidas (vistas, contactos, favoritos) |
| Mis vehículos | Lista con estado y acciones |
| Editar anuncio | Formulario completo |
| Boost/Impulsar | Promocionar anuncio |
| Leads | Contactos recibidos |
| Estadísticas | Gráficos de rendimiento |

### 3.10 🏢 Dashboard Dealer

| Feature | Descripción |
|---|---|
| Home dashboard | KPIs, gráficos, alertas |
| Inventario | CRUD completo de vehículos |
| Importación masiva | Bulk import de anuncios |
| CRM / Leads | Gestión de leads con pipeline |
| Empleados | Invitar, roles, suspender |
| Ubicaciones | Multi-sede |
| Citas | Calendario y gestión |
| Analytics | Dashboard con métricas |
| Publicidad | Campañas, tracking, ROI |
| Facturación | Invoices, métodos de pago |
| Suscripción | Planes y upgrade |
| Reportes | Exportables (PDF, CSV) |
| Reseñas | Responder reseñas |
| Configuración | Notificaciones, seguridad |

### 3.11 💳 Pagos

| Feature | Descripción |
|---|---|
| Stripe | Tarjetas internacionales |
| Azul | Tarjetas dominicanas |
| Checkout | Flujo de pago seguro |
| Planes dealer | Comparación y selección |
| Historial de pagos | Transacciones pasadas |
| Métodos de pago | CRUD de tarjetas |

### 3.12 🤖 IA & Chatbot

| Feature | Descripción |
|---|---|
| Chatbot OKLA | Asistente IA en español/slang DR |
| Soporte automático | Respuestas a preguntas frecuentes |
| Importación IA | Extraer datos de anuncios |
| Sugerencia de precio | Precio recomendado con IA |

### 3.13 ⭐ Reseñas

| Feature | Descripción |
|---|---|
| Ver reseñas | Lista con rating y comentarios |
| Escribir reseña | Stars + texto |
| Responder (dealer) | Respuesta del vendedor |
| Votar reseña | Útil/no útil |
| Reportar reseña | Contenido inapropiado |

### 3.14 ⚙️ Configuración & Perfil

| Feature | Descripción |
|---|---|
| Editar perfil | Nombre, email, avatar |
| Tema | Light/dark/system |
| Notificaciones | Preferencias granulares |
| Seguridad | Contraseña, 2FA, biometría |
| Privacidad | Configuración de privacidad |
| Idioma | Español / English |
| Caché | Limpiar datos en caché |
| Sobre OKLA | Versión, legal, contacto |
| Eliminar cuenta | Con confirmación |

### 3.15 📄 Legal & Info

| Feature | Descripción |
|---|---|
| Términos de servicio | Página completa |
| Política de privacidad | RGPD + App Store compliant |
| Política de cookies | Info |
| Contacto | Formulario de contacto |
| FAQ | Preguntas frecuentes |
| Ayuda | Centro de ayuda |

---

## 4. Requisitos App Store (Apple)

### 4.1 Cumplimiento obligatorio

| Requisito | Implementación |
|---|---|
| **Privacy Policy** | URL pública + in-app |
| **App Privacy Labels** | Declaración de datos recolectados |
| **IDFA (ATT)** | App Tracking Transparency dialog |
| **Sign in with Apple** | Implementado como login social |
| **Push Notification Permission** | Solicitado en contexto, no al inicio |
| **Location Permission** | Solo cuando necesario (mapa, publicar) |
| **Camera Permission** | Solo al subir fotos |
| **Minimum OS** | iOS 16.0+ |
| **64-bit** | Universal binary (arm64) |
| **IPv6** | Soporte completo |
| **HTTPS** | ATS (App Transport Security) |
| **Content Rating** | 4+ (marketplace, sin contenido adulto) |
| **In-App Purchases** | No aplica (pagos por servicio, no digital goods) |
| **Account Deletion** | Opción de eliminar cuenta |
| **Accessibility** | VoiceOver, Dynamic Type, contraste |

### 4.2 App Store Metadata

```
Bundle ID: do.okla.app
App Name: OKLA - Vehículos RD
Subtitle: Marketplace de vehículos
Category: Shopping
Secondary: Lifestyle
Age Rating: 4+
Languages: Spanish (Primary), English
Price: Free
```

### 4.3 Google Play Metadata

```
Package: do.okla.app
App Name: OKLA - Vehículos RD
Category: Shopping
Content Rating: Everyone
Languages: Spanish (Primary), English
Price: Free
```

---

## 5. Notificaciones Push

### 5.1 Tipos de Notificaciones

| Canal | Trigger | Prioridad |
|---|---|---|
| `messages` | Nuevo mensaje recibido | Alta |
| `leads` | Nuevo lead/contacto | Alta |
| `price_alerts` | Precio bajó de umbral | Alta |
| `search_alerts` | Nuevo vehículo match | Normal |
| `appointments` | Cita confirmada/recordatorio | Alta |
| `deals` | Promociones y ofertas | Baja |
| `system` | Actualizaciones de sistema | Baja |
| `billing` | Factura, pago, suscripción | Alta |
| `reviews` | Nueva reseña recibida | Normal |
| `advertising` | Campaña completada/stats | Normal |

### 5.2 Implementación

- **Firebase Cloud Messaging** para iOS y Android
- **Foreground handling** con local notifications
- **Deep linking** a pantalla relevante
- **Badges** actualizados en tiempo real
- **Silenciamiento** por horario configurable

---

## 6. Seguridad

### 6.1 Medidas Implementadas

| Medida | Detalle |
|---|---|
| SSL/TLS Pinning | Certificado pinned de okla.com.do |
| JWT Tokens | Access + Refresh con rotación |
| Biometric Auth | Face ID, Touch ID, Fingerprint |
| Secure Storage | Tokens en Keychain (iOS) / Keystore (Android) |
| 2FA TOTP | Google Authenticator compatible |
| Input Validation | Sanitización de inputs |
| Root/Jailbreak Detection | Advertencia, no bloqueo |
| ProGuard/R8 | Ofuscación en release |
| Anti-tampering | Integrity checks |
| Session Management | Timeout, revocación |

---

## 7. Rendimiento

### 7.1 Objetivos

| Métrica | Target |
|---|---|
| Cold start | < 2 segundos |
| Hot start | < 500ms |
| Frame rate | 60 FPS constante |
| APK size | < 30 MB |
| IPA size | < 40 MB |
| Memory usage | < 200 MB en uso activo |
| Battery drain | < 5%/hora en uso activo |
| Offline start | < 1 segundo |

### 7.2 Optimizaciones

- Lazy loading de imágenes con cache
- Paginación infinita
- Skeleton loaders
- Precarga de rutas frecuentes
- Compresión de imágenes antes de upload
- Widgets `const` donde sea posible
- `RepaintBoundary` en listas largas

---

## 8. Testing

### 8.1 Estrategia

| Tipo | Herramienta | Cobertura |
|---|---|---|
| Unit Tests | dart test | > 80% domain/data |
| Widget Tests | flutter_test | > 60% presentación |
| BLoC Tests | bloc_test | 100% BLoCs |
| Integration | integration_test | Flujos críticos |
| E2E | Patrol / Appium | Smoke tests |
| Snapshot | golden_toolkit | Componentes clave |

### 8.2 Flujos Críticos a Testear

1. Login → Dashboard
2. Búsqueda → Detalle → Contactar
3. Publicar vehículo (wizard completo)
4. Checkout → Pago exitoso
5. Chat → Enviar mensaje → Recibir respuesta
6. Push notification → Deep link → Pantalla correcta

---

## 9. Modo Offline

| Feature | Offline Behavior |
|---|---|
| Favoritos | Disponibles desde cache |
| Búsquedas guardadas | Disponibles |
| Historial | Últimos 50 vehículos vistos |
| Mensajes | Últimos mensajes cacheados |
| Publicar | Draft guardado, sube al reconectar |
| Dashboard | Datos en caché con timestamp |

---

## 10. Internacionalización

- **Idioma primario:** Español (República Dominicana)
- **Idioma secundario:** Inglés
- **Monedas:** DOP (principal), USD (referencia)
- **Formato fecha:** dd/MM/yyyy
- **Formato número:** 1,234,567.89
- **Slang DR:** El chatbot entiende dominicano coloquial
