# 📘 Manual de Usuario — Administradores (Admin)

> **Plataforma OKLA** — Gestión completa de la plataforma

---

## 🎯 Funcionalidades Disponibles para Administradores

Como administrador, tienes control total sobre la plataforma: gestión de publicidad, monitoreo de leads con IA, configuración del OKLA Score, y analytics avanzados.

---

## 1. Panel de Publicidad (Admin)

### Acceso

Navega a `/admin/publicidad`.

### Paso a Paso

#### 1.1 Pestaña: Vista General (Overview)

1. **Métricas de revenue:** Total de ingresos por publicidad, campañas activas, impresiones totales, clics totales.
2. **Gráfico de ingresos:** Tendencia de revenue por publicidad en el tiempo.
3. **Top dealers:** Los dealers que más invierten en publicidad.

#### 1.2 Pestaña: Campañas

1. **Lista de todas las campañas:** De todos los dealers y vendedores.
2. **Filtros:** Por estado (activa/pausada/completada), por dealer, por tipo de colocación.
3. **Acciones de moderación:**
   - ✅ **Aprobar** — Aprobar una campaña pendiente
   - ❌ **Rechazar** — Rechazar una campaña que viola políticas
   - 🔒 **Suspender** — Suspender una campaña activa
4. **Detalle:** Clic en cualquier campaña para ver métricas completas.

#### 1.3 Pestaña: Rotación

1. **Configuración del algoritmo de rotación:** Define cómo se distribuyen los anuncios en cada slot.
2. **Slots disponibles:**
   - HomepageFeatured, HomepageBanner
   - SearchTop, SearchSidebar
   - DetailPageSidebar, DetailPageBanner
   - CategorySpotlight, ComparisonBanner
3. **Parámetros configurables por slot:**
   - Peso del Quality Score
   - Peso del CPC
   - Factor de relevancia
   - Máximo de anuncios por slot

#### 1.4 Pestaña: Precios

1. **CPC/CPM floor:** Precios mínimos por tipo de colocación.
2. **Precios de paquetes:** Configurar precios de los paquetes predefinidos.
3. **Descuentos:** Configurar descuentos por volumen o early-bird.

---

## 2. Dashboard de Leads con IA

### Acceso

Navega a `/admin/leads`.

### ¿Qué es?

El sistema de IA analiza el comportamiento de TODOS los visitantes (logueados y anónimos) y predice cuáles tienen mayor probabilidad de comprar un vehículo.

### Paso a Paso

#### 2.1 Vista General

1. **Tarjetas de resumen:**
   - **Total de Leads** — Todos los visitantes con actividad significativa
   - **Leads Hot 🔥** — Score 60+, alta probabilidad de compra
   - **Leads Warm ☀️** — Score 35-59, interés moderado
   - **Leads Cold ❄️** — Score 10-34, explorando
   - **Score Promedio** — Promedio de todos los leads

2. **Indicador de IA:** Badge que muestra "Powered by OKLA AI Engine"

#### 2.2 Filtrar Leads

1. **Por nivel:** Usa los botones de filtro:
   - 🔥 **Hot** — Solo leads calientes
   - ☀️ **Warm** — Solo leads tibios
   - ❄️ **Cold** — Solo leads fríos
   - **Todos** — Ver todos

2. **Ordenar por:**
   - **Score** — Mayor score primero
   - **Reciente** — Más recientes primero
   - **Contactos** — Mayor número de acciones de contacto primero

#### 2.3 Detalle de un Lead

Haz clic en la flecha de un lead para expandir el detalle:

1. **Desglose del Score (0-100):** Barra visual con 4 dimensiones:
   - 🟦 **Engagement** (0-25): Páginas vistas, vehículos únicos visitados, recencia, duración
   - 🟩 **Intención** (0-25): Búsquedas, filtros, galería 360°, comparaciones
   - 🟧 **Contacto** (0-30): Llamadas, WhatsApp, mensajes, test drives
   - 🟪 **Financiero** (0-20): Calculadora de financiamiento, seguros, favoritos

2. **Señales de comportamiento:** Grid mostrando cada acción detectada con su nivel de importancia (alto=rojo, medio=naranja, bajo=gris).

3. **Vehículos de interés:** Lista de vehículos que el lead ha visto, con indicadores de si contactó al vendedor o lo marcó como favorito.

4. **Perfil inferido:** Preferencias detectadas:
   - Marcas preferidas
   - Rango de precio
   - Tipo de vehículo
   - Condición (nuevo/usado)

5. **Probabilidad de conversión:** Porcentaje calculado con regresión logística.

6. **Días estimados para compra:**
   - Hot: 1-7 días
   - Warm: 7-30 días
   - Cold: 60+ días

7. **Acción recomendada:** El sistema sugiere la mejor acción para cada lead:
   - "Contactar de inmediato — alta probabilidad de compra"
   - "Enviar ofertas relevantes"
   - "Nutrir con contenido"

#### 2.4 Metodología de IA

Al final de la página hay una sección que explica cómo funciona el algoritmo:

- Modelo de scoring ponderado de 4 dimensiones
- Regresión logística para probabilidad de conversión: P = 1/(1+e^(-0.08×(score-50)))
- Señales ponderadas: WhatsApp 9pts, Llamada 10pts, Test Drive 12pts, Financiamiento 6pts, etc.

---

## 3. Configuración del OKLA Score™

### Acceso

Navega a `/admin/okla-score`.

### Paso a Paso

1. **Seleccionar fase activa:** El OKLA Score tiene 4 fases de implementación:
   - **Fase 1: Tierra Fértil** — Score básico con datos gratuitos (NHTSA)
   - **Fase 2: Confianza** — Score mejorado con APIs de mercado
   - **Fase 3: Inteligencia** — Score con IA y datos avanzados
   - **Fase 4: El Estándar** — Score completo con todas las fuentes

2. **Configurar por fase:**
   - APIs habilitadas (NHTSA, Carfax, AutoCheck, etc.)
   - Pesos de cada dimensión
   - Umbrales de niveles (Excelente, Bueno, Aceptable, etc.)
   - Features del frontend habilitados

3. **Guardar cambios:** Los cambios se aplican inmediatamente a todos los nuevos cálculos de Score.

---

## 4. Analytics de la Plataforma

### Acceso

Navega a `/admin/analytics`.

### Paso a Paso

1. **Seleccionar período:** 7 días / 30 días / 90 días.

2. **KPIs principales:**
   - **Visitas** — Total de visitantes
   - **Usuarios registrados** — Nuevos y activos
   - **Vehículos publicados** — Nuevos listados
   - **MRR** — Monthly Recurring Revenue
   - Flechas de tendencia (↑ mejora, ↓ baja)

3. **Gráfico semanal:** Tendencia de visitas y usuarios por semana.

4. **Búsquedas populares:** Las marcas y modelos más buscados.

5. **Fuentes de tráfico:** Distribución de tráfico por fuente (directo, orgánico, redes sociales, referral).

6. **Dispositivos:** Desktop vs Tablet vs Mobile.

7. **Tasas de conversión:** Registro → KYC → Publicación → Venta.

8. **Revenue por canal:** Ingresos por suscripciones, publicidad, etc.

9. **Exportar:** Botón para descargar el reporte completo.

---

## 5. Gestión de Usuarios y Dealers

### Acceso

- Dealers: `/admin/dealers`
- Usuarios: Desde el panel de admin
- Equipo: `/admin/equipo`

### Paso a Paso

1. **Ver dealers:** Lista de todos los concesionarios registrados con estado, plan de suscripción, y métricas.
2. **Aprobar KYC:** Revisar y aprobar/rechazar las verificaciones de identidad.
3. **Gestionar equipo:** Agregar/remover miembros del equipo de plataforma.

---

## 6. Moderación de Contenido

### Acceso

- Contenido: `/admin/contenido`
- Sistema: `/admin/sistema`
- Facturación: `/admin/facturacion`
- Mantenimiento: `/admin/mantenimiento`

### Paso a Paso

1. **Contenido:** Moderar listados de vehículos, reseñas, y mensajes reportados.
2. **Sistema:** Configuración general de la plataforma.
3. **Facturación:** Ver facturas, pagos, y revenue.
4. **Mantenimiento:** Modo mantenimiento, tareas programadas.

---

## 💡 Mejores Prácticas para Administradores

1. **Revisa leads hot diariamente:** Los leads con Score 60+ son oportunidades de venta inmediatas. Contacta a los dealers para que los atiendan.
2. **Monitorea el CTR de campañas:** Si el CTR promedio baja del 2%, revisa la calidad de los anuncios.
3. **Ajusta fases del Score gradualmente:** No saltes de Fase 1 a Fase 4 directamente.
4. **Exporta reportes semanalmente:** Mantén un registro histórico para toma de decisiones.
5. **Modera campañas rápidamente:** Las campañas pendientes de aprobación deben resolverse en menos de 24 horas.

---

## ❓ Preguntas Frecuentes

**¿Cómo funciona la predicción de leads con IA?**
El sistema analiza el comportamiento de cada visitante (páginas vistas, búsquedas, contactos, favoritos, tiempo en la plataforma) y asigna un score ponderado. Los leads hot (60+) tienen alta probabilidad de compra en los próximos 7 días.

**¿Puedo cambiar los pesos del algoritmo de leads?**
Actualmente los pesos están optimizados para el mercado dominicano. En futuras versiones se podrán ajustar desde el panel de admin.

**¿Qué pasa si rechazo una campaña?**
El dealer recibe una notificación con el motivo del rechazo y puede modificar y reenviar la campaña.

**¿El OKLA Score afecta la posición de los anuncios?**
No directamente. El Score es una herramienta de confianza para compradores. La posición de anuncios se determina por el sistema de subastas (CPC × Quality Score).
