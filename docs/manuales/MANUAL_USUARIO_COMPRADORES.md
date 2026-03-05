# 📘 Manual de Usuario — Compradores (Buyers)

> **Plataforma OKLA** — Tu marketplace de vehículos en República Dominicana

---

## 🎯 Funcionalidades Disponibles para Compradores

Como comprador en OKLA, tienes acceso a herramientas poderosas para encontrar el vehículo perfecto con confianza.

---

## 1. OKLA Score™ — Verificación de Vehículos

### ¿Qué es?

OKLA Score™ es un sistema de puntuación de 0 a 1,000 que evalúa la calidad y confiabilidad de un vehículo usando datos reales (historial VIN, recalls, precio de mercado, kilometraje, confianza del vendedor y documentación).

### Paso a Paso

#### 1.1 Consultar el Score de un Vehículo

1. **Navegar a OKLA Score:** Ve a la página principal y haz clic en **"OKLA Score"** en el menú de navegación, o ve directamente a `/okla-score`.

2. **Ingresar el VIN:** En la página de OKLA Score, verás un campo de texto para ingresar el VIN (Vehicle Identification Number) de 17 caracteres.
   - El VIN lo encuentras en la tarjeta de circulación, en el parabrisas del vehículo, o pídelo al vendedor.
   - Ejemplo: `1HGBH41JXMN109186`

3. **Datos opcionales (mejoran la precisión):**
   - **Precio solicitado (RD$):** Ingresa el precio que el vendedor está pidiendo para obtener un análisis de precio vs mercado.
   - **Kilometraje:** Ingresa los kilómetros del vehículo para evaluar el desgaste.

4. **Hacer clic en "Calcular Score"** — El sistema procesará:
   - Decodificación del VIN (marca, modelo, año, motor, transmisión)
   - Verificación de recalls activos (retiros de seguridad)
   - Análisis de calificaciones de seguridad NHTSA
   - Comparación de precio vs mercado
   - Evaluación de kilometraje vs promedio

5. **Leer el reporte:** El resultado mostrará:
   - **Score general** (0–1,000) con nivel: Excelente (800+), Bueno (600–799), Aceptable (400–599), Riesgo (200–399), Crítico (0–199)
   - **7 dimensiones evaluadas** con barras visuales:
     - D1: Historial VIN (25% del peso)
     - D2: Mecánica y Seguridad
     - D3: Precio de Mercado
     - D4: Kilometraje
     - D5: Confianza del Vendedor
     - D6: Documentación
     - D7: Recalls y Defectos
   - **Alertas** (rojas = críticas, amarillas = advertencia, azules = informativas)
   - **Análisis de precio** — "Excelente Precio", "Buen Precio", "Precio Justo", "Sobre el Mercado", o "Sobreprecio"

#### 1.2 Ver el Score en un Listado de Vehículo

- Cuando navegas los vehículos en `/vehiculos`, algunos listados muestran una **badge de OKLA Score** directamente en la cabecera del detalle del vehículo.
- Haz clic en la badge para ver el reporte completo.

### 💡 Consejos

- Un Score **800+** es excelente — el vehículo está en óptimas condiciones.
- Si ves alertas **rojas** sobre recalls sin resolver, pregúntale al vendedor si los ha atendido.
- Compara el "Precio Solicitado" vs "Precio de Mercado" antes de negociar.

---

## 2. Búsqueda de Vehículos con Publicidad Relevante

### ¿Qué es?

Cuando buscas vehículos en OKLA, verás resultados orgánicos Y resultados patrocinados (de dealers que pagan publicidad). Los patrocinados se identifican claramente con etiquetas como **"Patrocinado"**, **"Destacado"** o **"Premium"**.

### Paso a Paso

1. **Buscar vehículos:** Ve a `/vehiculos` y usa los filtros de búsqueda (marca, modelo, precio, año, etc.).

2. **Identificar anuncios:** Los vehículos patrocinados tienen una etiqueta visual:
   - 🟡 **Patrocinado** — Anuncio pagado por un dealer
   - ⭐ **Destacado** — Posición premium en los resultados
   - 👑 **Premium** — Máxima visibilidad

3. **Interactuar normalmente:** Los vehículos patrocinados son vehículos reales de dealers verificados. Puedes verlos, contactar al vendedor, y hacer tu proceso de compra como con cualquier otro listado.

### 💡 Datos importantes

- OKLA **NO** modifica el Score de un vehículo por publicidad — el Score es siempre objetivo.
- Los anuncios patrocinados son vehículos reales disponibles para compra.
- Si buscas un Toyota, los anuncios que verás serán de Toyota u otras marcas similares a tu búsqueda.

---

## 3. Retargeting — Anuncios en Redes Sociales

### ¿Qué es?

Después de visitar OKLA y buscar vehículos, es posible que veas anuncios relacionados en **Facebook**, **Instagram**, **Google**, **YouTube** y **TikTok**. Estos anuncios son de vehículos disponibles en OKLA que coinciden con tu búsqueda.

### ¿Cómo funciona?

- Cuando buscas un "Toyota RAV4 2024" en OKLA, la plataforma registra tu interés.
- Luego, al navegar por Facebook o Google, podrías ver un anuncio de un Toyota RAV4 disponible en un dealer de OKLA.
- **Los anuncios muestran vehículos de dealers que pagan publicidad** y que coinciden con tu búsqueda.

### Control de Privacidad

- OKLA usa **Facebook Pixel**, **Google Analytics 4** y **TikTok Pixel** para el retargeting.
- Puedes controlar estos anuncios desde la configuración de anuncios de cada plataforma:
  - Facebook: Configuración → Anuncios → Preferencias de anuncios
  - Google: myaccount.google.com → Datos y privacidad → Anuncios personalizados
  - TikTok: Configuración → Privacidad → Personalización de anuncios

---

## 4. Dashboard del Comprador

### Paso a Paso

1. **Acceder a tu cuenta:** Inicia sesión y ve a `/cuenta`.

2. **Panel principal:** Verás:
   - **Favoritos** — Vehículos que has guardado
   - **Búsquedas guardadas** — Tus filtros de búsqueda anteriores
   - **Alertas de precio** — Notificaciones cuando un vehículo baja de precio
   - **Historial** — Vehículos que has visto
   - **Mensajes** — Conversaciones con vendedores

3. **Acciones rápidas:**
   - 🔍 Buscar vehículos
   - ❤️ Ver favoritos (`/cuenta/favoritos`)
   - 🔔 Alertas de precio (`/cuenta/alertas`)
   - 📊 Historial (`/cuenta/historial`)
   - 💬 Mensajes (`/mensajes`)
   - ⚙️ Perfil (`/cuenta/perfil`)

---

## 5. Contactar Vendedores

### Paso a Paso

1. **Encontrar un vehículo:** Navega a un vehículo de tu interés en `/vehiculos/[slug]`.

2. **Opciones de contacto:**
   - 📞 **Llamar** — Botón de llamada directa
   - 💬 **WhatsApp** — Abre conversación en WhatsApp
   - ✉️ **Mensaje** — Envía un mensaje interno en OKLA
   - 📅 **Test Drive** — Solicita una prueba de manejo

3. **Tip:** Los vendedores pueden ver tu perfil cuando los contactas, así que completa tu perfil para generar confianza.

---

## ❓ Preguntas Frecuentes

**¿El OKLA Score es gratis?**
Sí, la consulta básica del Score es gratuita para todos los usuarios.

**¿Por qué veo anuncios de OKLA en mis redes sociales?**
Porque visitaste la plataforma y se activaron pixeles de retargeting. Puedes desactivarlos desde la configuración de anuncios de cada red social.

**¿Los vehículos patrocinados son confiables?**
Sí, son vehículos reales de dealers verificados. El Score de OKLA no se modifica por publicidad pagada.

**¿Cómo guardo un vehículo en favoritos?**
Haz clic en el ícono de corazón (❤️) en cualquier listado de vehículo.
