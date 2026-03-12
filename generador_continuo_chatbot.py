import json
import random
import time
import os
from pathlib import Path

# ------------------------------------------------------------
# 1. Construcción de listas grandes de frases (≈50 cada una)
# ------------------------------------------------------------

# Verbos y frases para iniciar la orden
verbos_inicio = [
    "Ejecuta", "Corre", "Inicia", "Lanza", "Procesa", "Realiza", "Lleva a cabo",
    "Arranca", "Pon en marcha", "Comienza con", "Haz", "Ejecuta ahora", "Ejecuta por favor",
    "Ejecuta inmediatamente", "Ejecuta ya", "Ejecuta sin demora", "Procede a ejecutar",
    "Vamos a ejecutar", "Quiero que ejecutes", "Necesito que ejecutes", "Debes ejecutar",
    "Te pido que ejecutes", "Sería bueno que ejecutes", "Ejecuta rápido", "Ejecuta de una vez",
    "Ejecuta cuanto antes", "Ejecuta el día de hoy", "Ejecuta en este momento"
]

# Modificadores para dar variedad
modificadores_inicio = [
    "", "ahora", "de inmediato", "ya mismo", "sin demora", "rápidamente",
    "por favor", "urgentemente", "en este instante", "a continuación"
]

# Construimos INICIOS combinando verbos y modificadores
INICIOS = []
for verbo in verbos_inicio:
    for mod in modificadores_inicio:
        if mod:
            frase = f"{verbo} {mod} el prompt"
        else:
            frase = f"{verbo} el prompt"
        INICIOS.append(frase)
# Eliminar duplicados y limitar a 50
INICIOS = list(set(INICIOS))[:50]

# ------------------------------------------------
PRIMERAS_VECES = [
    "Como si fuera la primera vez",
    "Como si nunca lo hubieras hecho",
    "Como si fuera la primera ocasión",
    "Hazlo como si fuera la primera ejecución",
    "Simula que es la primera vez",
    "Como si no tuvieras experiencia previa",
    "Imaginando que es la primera vez",
    "Como si partieras de cero",
    "Como si jamás lo hubieras realizado",
    "Como si fuese la primera vez",
    "Como si no hubieras ejecutado nada antes",
    "Como si empezaras de nuevo",
    "Como si fuera un sistema nuevo",
    "Como si no hubiera historial",
    "Como si nunca antes hubieras visto este prompt",
    "Hazlo con mentalidad de principiante",
    "Hazlo como si no supieras nada",
    "Hazlo desde cero",
    "Hazlo como si fuera el primer día",
    "Hazlo sin asumir nada previo",
    "Como si no hubiera contexto anterior",
    "Como si no hubieras leído los otros prompts",
    "Como si fuera la primera interacción",
    "Como si no hubieras procesado nada aún",
    "Como si todo empezara ahora",
    "Como si nunca hubieras visto esto",
    "Como si fuera la primera ejecución en la historia",
]

mas_primera = [
    "Como si fuese la primera ocasión",
    "Como si fuera un debut",
    "Como si fuera un estreno",
    "Como si no tuvieras registro",
    "Como si no hubiera pasado nada antes",
    "Como si el prompt fuera nuevo para ti",
    "Como si nunca hubieras trabajado con esto",
    "Como si fuera la primera vez que ves este tipo de instrucción",
]
PRIMERAS_VECES.extend(mas_primera)
PRIMERAS_VECES = list(set(PRIMERAS_VECES))[:50]

# ------------------------------------------------
# Lista original de frases de auditoría (para generación aleatoria en JSON)
TIPOS_AUDITORIA = [
    "y haz otro tipo de auditoría",
    "y realiza una auditoría diferente",
    "y efectúa una revisión distinta",
    "y lleva a cabo una auditoría alternativa",
    "y ejecuta una auditoría con otro enfoque",
    "y cambia el tipo de auditoría",
    "y prueba una auditoría novedosa",
    "y aplica una metodología de auditoría distinta",
    "y haz una auditoría con un criterio diferente",
    "y realiza una inspección distinta",
    "y efectúa un análisis diferente",
    "y lleva a cabo un examen alternativo",
    "y ejecuta una revisión con otro método",
    "y cambia el enfoque de la auditoría",
    "y prueba una nueva forma de auditar",
    "y aplica una técnica de auditoría diferente",
    "y haz una auditoría desde otra perspectiva",
    "y realiza una auditoría con parámetros distintos",
    "y efectúa una auditoría con nuevas reglas",
    "y lleva a cabo una auditoría modificada",
    "y ejecuta una auditoría con variantes",
    "y prueba un estilo de auditoría diferente",
    "y aplica un modelo de auditoría novedoso",
    "y haz una auditoría no convencional",
    "y realiza una auditoría con otro alcance",
    "y efectúa una auditoría con otras métricas",
]
TIPOS_AUDITORIA = list(set(TIPOS_AUDITORIA))[:50]

# ------------------------------------------------
# CICLO MAESTRO OKLA MARKETPLACE — Rotación completa integrada
# Cubre los 16 ciclos: Modelo de Negocio, Dealers, OKLA Score,
# Agentes IA, Prompt Engineering, Base de Conocimiento, RAG,
# Aprendizaje Continuo, Seguridad, Infraestructura, UX/UI,
# Testing, Analytics, Arquitectura y Estrategia de Mercado RD.
TIPOS_AUDITORIA_SECUENCIAL = [

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #1 — COLD START: Sin compradores iniciales, los dealers
    # no reciben leads y abandonan la plataforma
    # ══════════════════════════════════════════════════════════════════

    # ── Tráfico y SEO orgánico
    "SEO por listing: Auditar que cada vehículo tiene URL canónica única, meta title con marca/modelo/año/precio, meta description con CTA, y está indexado en Google Search Console con menos de 48h de demora y ejecuta correcciones",
    "Sitemap dinámico: Auditar que el sitemap.xml se regenera automáticamente cuando se publica o elimina un listing, y que Google puede crawlearlo sin errores 404 o 500 y ejecuta correcciones",
    "Structured Data (JSON-LD): Auditar que cada listing emite schema tipo 'Car' con VIN, precio, mileage, offerCount y condition, y que Google Rich Results Test lo valida sin errores y ejecuta correcciones",
    "Core Web Vitals producción: Auditar LCP menor a 2.5s, CLS menor a 0.1 e INP menor a 200ms en PageSpeed Insights para la homepage y páginas de listing en mobile 4G dominicana y ejecuta correcciones",
    "Indexación de listings beta: Auditar que los 1,500 listings de dealers beta están todos indexados antes del lanzamiento público, con cobertura verificada en Google Search Console y ejecuta correcciones",

    # ── SEM y campañas de adquisición de compradores
    "Páginas de aterrizaje SEM: Auditar que cada URL de destino de Google Ads carga en menos de 1.5s, tiene el CTA visible sin scroll y el formulario de contacto funciona end-to-end sin errores de conversión y ejecuta correcciones",
    "UTM tracking end-to-end: Auditar que los parámetros UTM de campañas SEM se preservan a través de todo el funnel (landing → listing → ChatAgent → contacto) y llegan correctamente a Google Analytics 4 y ejecuta correcciones",
    "Píxeles de remarketing: Auditar que el píxel de Google Ads y Meta Pixel se disparan correctamente en eventos de vista de listing, inicio de ChatAgent y clic en WhatsApp, para habilitar remarketing a compradores que no convirtieron y ejecuta correcciones",

    # ── Retención del comprador en la plataforma
    "Sistema de alertas de nuevos listings: Auditar que un comprador que activa una alerta de búsqueda (ej: 'Toyota Corolla 2019-2021 bajo $18,000') recibe email o push notification en menos de 30 minutos cuando se publica un vehículo que coincide y ejecuta correcciones",
    "Comparador de vehículos: Auditar que el comprador puede comparar hasta 3 vehículos simultáneamente en una tabla de specs, y que la función responde en menos de 1s sin hacer múltiples roundtrips a la API y ejecuta correcciones",
    "Páginas de modelo y marca (SEO de largo plazo): Auditar que existen páginas estáticas generadas para combinaciones populares ('Toyota Corolla usados en República Dominicana') con contenido único generado por IA y actualizadas semanalmente con el inventario real y ejecuta correcciones",
    "Widget de vehículos similares: Auditar que RecoAgent muestra 4-6 vehículos similares al final de cada listing (mismo segmento, ±20% precio, ±2 años) y que los vehículos sugeridos tienen disponibilidad real verificada en tiempo real y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #2 — CULTURA COMERCIAL DEL DEALER: Informalidad,
    # baja adopción tecnológica, resistencia al cambio
    # ══════════════════════════════════════════════════════════════════

    # ── Fricción cero en onboarding
    "Importador desde Facebook Marketplace: Auditar que el ListingAgent acepta URLs de listings de Facebook, extrae título, precio, descripción y fotos, y crea un borrador en OKLA en menos de 3 minutos sin requerir código ni configuración al dealer y ejecuta correcciones",
    "Importador masivo desde Excel/CSV: Auditar que el dealer puede subir un archivo Excel con columnas básicas (marca, modelo, año, precio, km, descripción) y el sistema crea todos los listings automáticamente con validación fila por fila y reporte de errores descargable y ejecuta correcciones",
    "Onboarding asistido en 3 pasos: Auditar que el flujo de primer listing tarda menos de 8 minutos medidos con un dealer real que nunca usó OKLA, y que el sistema guía con tooltips contextuales en cada campo sin lenguaje técnico y ejecuta correcciones",
    "App móvil del dealer (PWA): Auditar que el panel del dealer funciona como PWA instalable en Android e iOS, permite subir fotos directamente desde la cámara del celular, y las primeras 3 acciones del dealer no requieren abrir un navegador de escritorio y ejecuta correcciones",
    "Notificaciones WhatsApp al dealer: Auditar que el dealer recibe mensaje de WhatsApp (no solo email) cuando: recibe un nuevo lead, un comprador agenda test drive, o un listing es destacado. Verificar entrega en menos de 60 segundos y ejecuta correcciones",

    # ── Demostración de valor en los primeros 30 días
    "Reporte automático de primeros 7 días: Auditar que todo dealer nuevo recibe automáticamente al día 7 un email con: número de vistas por listing, número de consultas recibidas, vehículo más visto y comparativa vs. el promedio del mercado OKLA, incluso si los números son bajos y ejecuta correcciones",
    "Dashboard simplificado para dealer no técnico: Auditar que la vista principal del dashboard muestra exactamente 3 métricas clave (vistas esta semana, consultas este mes, listings activos) sin tablas complejas, y que un dealer sin experiencia digital las entiende sin explicación y ejecuta correcciones",
    "Notificación de 'primera consulta recibida': Auditar que cuando el dealer recibe su primera consulta de comprador, se dispara una notificación especial de celebración por WhatsApp y email con el mensaje del comprador incluido, reforzando el valor de OKLA desde el primer evento positivo y ejecuta correcciones",
    "Video tutorial contextual: Auditar que en cada sección del panel del dealer existe un botón de ayuda que abre un video corto (menos de 90 segundos en español dominicano) explicando exactamente esa función, y que los videos cargan en menos de 2 segundos sin requerir YouTube y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #3 — CONVERSIÓN FREEMIUM A PAGO: Dealers que permanecen
    # indefinidamente en plan LIBRE sin convertir
    # ══════════════════════════════════════════════════════════════════

    # ── Sistema de nudges automáticos
    "Trigger de conversión por actividad: Auditar que el sistema detecta automáticamente cuando un dealer en plan LIBRE recibe 5 o más consultas en 30 días y dispara un email personalizado con el cálculo exacto de ROI ('recibiste 7 consultas, con VISIBLE proyectamos 25, eso equivale a X ventas adicionales') y ejecuta correcciones",
    "Trigger de conversión por inactividad de listing: Auditar que cuando un vehículo lleva 45 días publicado sin venderse, el sistema envía al dealer una sugerencia de Listing Destacado con el precio ($6/mes), el impacto esperado en vistas y un botón de activación con un clic y ejecuta correcciones",
    "Contador de oportunidades perdidas: Auditar que el dashboard del dealer en plan LIBRE muestra un banner dinámico con el número de compradores que buscaron exactamente su tipo de vehículo en OKLA esta semana pero no vieron su listing por estar en posición baja, creando urgencia real medida con datos y ejecuta correcciones",
    "Email de 'resumen mensual con benchmark': Auditar que el primer día de cada mes el dealer recibe un email con sus métricas vs. el promedio de dealers en plan VISIBLE, mostrando concretamente cuántas más vistas y consultas tienen los dealers pagantes, con CTA de upgrade y ejecuta correcciones",
    "Prueba gratuita de 30 días de plan VISIBLE: Auditar que el sistema puede activar automáticamente una prueba de 30 días para dealers del Segmento B que completan 20+ listings, que el feature de visibilidad se activa inmediatamente al activar la prueba, y que al día 25 se envía recordatorio de vencimiento con oferta de descuento del 20% en el primer mes pago y ejecuta correcciones",

    # ── Reducción de fricción en el pago
    "Pago con tarjeta dominicana (Visa/MC local): Auditar que el flujo de pago acepta tarjetas emitidas por bancos dominicanos (BHD, Popular, BanReservas) sin fricción adicional, y que la tasa de rechazo por 'tarjeta internacional no soportada' es 0% y ejecuta correcciones",
    "Garantía de resultados visible en el checkout: Auditar que en la página de upgrade a plan VISIBLE aparece explícitamente la garantía ('si no recibes 10 consultas en 30 días, el mes 2 es gratis') con enlace a los términos, reduciendo la objeción de riesgo antes del pago y ejecuta correcciones",
    "Flujo de upgrade sin salir del dashboard: Auditar que el dealer puede hacer upgrade de plan LIBRE a VISIBLE en menos de 3 clics sin abandonar el panel de administración, y que el feature de visibilidad se activa en menos de 60 segundos después del pago confirmado y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #4 — DEPENDENCIA DE ANTHROPIC API: Riesgo de proveedor
    # único que puede cambiar precios o términos sin previo aviso
    # ══════════════════════════════════════════════════════════════════

    # ── Arquitectura multi-LLM
    "LLMGateway — fallback automático en cascada Claude → Gemini → Llama: Auditar que cuando Claude API devuelve error 429, 500 o 503, el LLMGateway redirige automáticamente a Gemini 1.5 Flash en menos de 500ms; si Gemini también falla (error 429, 500 o 503) dentro de los siguientes 500ms, el sistema redirige al tercer nivel con Llama 3.1 70B hosteado en instancia propia de DigitalOcean GPU; verificar que en ninguno de los tres escenarios el usuario ve un error visible; verificar que el log registra para cada evento: agente afectado, modelo primario que falló, modelo de fallback activado, nivel de fallback alcanzado (1=Gemini, 2=Llama), latencia total del redireccionamiento y timestamp; verificar que si los tres modelos fallan simultáneamente el sistema activa el modo degradado con respuestas desde caché Redis sin llamada a ningún LLM; y verificar que el dashboard de administración muestra en tiempo real el porcentaje de requests resueltos por cada modelo (Claude/Gemini/Llama/caché) en las últimas 24 horas y ejecuta correcciones",
    "LLMGateway — paridad funcional Gemini y Llama: Auditar que los 8 agentes de OKLA (SearchAgent, RecoAgent, DealerChatAgent, PricingAgent, ListingAgent, ModerationAgent, AnalyticsAgent, OrchestratorAgent) producen outputs de calidad equivalente cuando corren sobre Gemini 1.5 Flash o Llama 3.1 70B vs. Claude, validado con un test suite de 50 casos por agente en cada modelo, y que las diferencias de formato o longitud de respuesta no rompen el parsing del sistema y ejecuta correcciones",
    "Alerta de costo de API: Auditar que el sistema tiene alertas configuradas en tres niveles ($300/mes = warning, $500/mes = alerta crítica, $700/mes = activación automática de modo caché agresivo que desvía 40% del tráfico a Gemini Flash y 20% a Llama local) y que las alertas llegan al CTO en menos de 5 minutos del evento y ejecuta correcciones",
    "Monitoreo de costo por agente y por modelo: Auditar que el dashboard de administración de OKLA muestra el costo acumulado del mes actual desglosado por agente (ListingAgent, ChatAgent, etc.) y por modelo utilizado (Claude/Gemini/Llama), actualizado diariamente, de modo que el equipo técnico identifica cuál agente consume el presupuesto desproporcionadamente y en qué modelo, y puede tomar decisiones de rerouting antes de superar el presupuesto mensual y ejecuta correcciones",
    
    # ── Sistema de caché para reducir dependencia
    "Caché Redis de respuestas del ListingAgent: Auditar que cuando el ListingAgent procesa un vehículo de marca/modelo/año/trim ya procesado anteriormente, sirve el resultado desde caché Redis en lugar de llamar a la API, midiendo que el hit rate de caché supera el 50% después del primer mes de operación y ejecuta correcciones",
    "Caché de respuestas frecuentes del ChatAgent: Auditar que las 50 preguntas más frecuentes por modelo de vehículo (precio, consumo, mantenimiento, garantía) están pre-generadas y cacheadas en Redis, y que el 70% de las consultas del ChatAgent se resuelven sin llamada a la API de LLM y ejecuta correcciones",
    "Prompt Caching de Anthropic (Context Window Cache): Auditar que el system prompt del DealerChatAgent (que es idéntico para todos los dealers) usa el feature de Prompt Caching de Anthropic, logrando reducción de costo de tokens de entrada mayor al 60% medido en la factura mensual vs. el mes anterior sin caché y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #5 — PLAN ÉLITE CON MARGEN NEGATIVO: El ChatAgent
    # ilimitado puede costar más de $199/mes por dealer
    # ══════════════════════════════════════════════════════════════════

    "Hard limit de 2,000 conversaciones en plan ÉLITE: Auditar que cuando un dealer ÉLITE alcanza 1,600 conversaciones en el mes (80% del límite), recibe notificación por WhatsApp y email con proyección del mes. Al alcanzar 2,000, el ChatAgent entra en modo básico (respuestas cortas sin recuperación de contexto extendido) hasta el inicio del siguiente ciclo de facturación y ejecuta correcciones",
    "Cálculo y facturación de overage en ÉLITE: Auditar que las conversaciones 2,001 a N se registran en un contador separado, se multiplican por $0.08 y se añaden automáticamente a la factura del mes siguiente, con detalle de fecha/hora de cada conversación de overage descargable por el dealer y ejecuta correcciones",
    "Monitoreo de costo real por dealer ÉLITE: Auditar que el sistema calcula diariamente el costo real de API (tokens de entrada + tokens de salida × precio Claude) para cada dealer en plan ÉLITE, y que se dispara una alerta interna cuando el costo proyectado del mes supera $180 (90% del ingreso del plan) para que el equipo revise el caso antes del cierre y ejecuta correcciones",
    "Límite soft con degradación progresiva: Auditar que entre 1,600 y 2,000 conversaciones el ChatAgent sigue funcionando normalmente pero el sistema activa agresivamente el caché de preguntas frecuentes para reducir el costo marginal por conversación adicional, midiendo que el costo promedio por conversación en esa franja no supera $0.04 y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #6 — CAPITAL Y CONTROL FINANCIERO: Burn rate no
    # controlado puede agotar el runway antes del break-even
    # ══════════════════════════════════════════════════════════════════

    "Dashboard financiero interno (burn rate): Auditar que el administrador de OKLA tiene una vista interna que muestra: gastos del mes actual por categoría (API, infraestructura, marketing, desarrollo), ingresos del mes por plan y fuente, margen neto en tiempo real, y proyección de runway en meses basada en el burn rate de los últimos 30 días y ejecuta correcciones",
    "Reconciliación de pagos de dealers: Auditar que cada suscripción cobrada en Stripe tiene su correspondiente registro en la base de datos de OKLA con estado confirmado, y que el proceso de reconciliación diaria detecta y alerta sobre pagos recibidos sin suscripción activa o suscripciones activas sin pago registrado y ejecuta correcciones",
    "Alertas de umbral de ingresos: Auditar que el sistema envía alerta automática al fundador cuando los ingresos acumulados del mes proyectados al día 30 están por debajo del OPEX mensual de $2,215, con tiempo suficiente para activar acciones de conversión adicionales antes del cierre del mes y ejecuta correcciones",
    "Control de costos de infraestructura: Auditar que existe una alerta en DigitalOcean que notifica cuando el gasto proyectado del mes de cloud supera el presupuesto asignado de $210, y que el equipo técnico tiene un runbook documentado para reducir recursos no críticos en caso de alerta activada y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #7 — LEY 172-13 Y COMPLIANCE: Incumplimiento de
    # protección de datos dominicanos y disputas legales
    # ══════════════════════════════════════════════════════════════════

    "Disclosure de bot en ChatAgent: Auditar que el primer mensaje de cada sesión del DealerChatAgent incluye obligatoriamente: nombre del dealer, texto 'Soy un asistente virtual de OKLA', enlace a la política de privacidad en okla.do/privacidad, y confirmación de aceptación antes de que el comprador pueda escribir, todo en menos de 200ms de carga y ejecuta correcciones",
    "Derecho de supresión Ley 172-13 (end-to-end): Auditar que cuando un comprador solicita eliminación de sus datos desde su perfil, el sistema elimina en cascada: registro de usuario, historial de conversaciones del ChatAgent, historial de búsquedas, preferencias guardadas, y datos en Redis/caché, con confirmación por email al usuario en menos de 5 días hábiles y ejecuta correcciones",
    "Derecho de acceso y portabilidad: Auditar que el comprador puede descargar desde su perfil un archivo ZIP con todos sus datos (conversaciones, búsquedas, alertas configuradas, datos de perfil) en formato JSON legible, generado en menos de 10 minutos y disponible por link de descarga segura de 24 horas y ejecuta correcciones",
    "Encriptación de datos personales en reposo: Auditar que los campos teléfono, cédula (si se recopila), email y transcripciones del ChatAgent están encriptados en la base de datos PostgreSQL usando AES-256, y que las claves de encriptación están almacenadas en variables de entorno separadas de la base de datos y ejecuta correcciones",
    "Anonimización de datos en logs: Auditar que los logs de producción (CloudWatch, DigitalOcean Monitoring) no contienen en texto plano: contraseñas, tokens de sesión, números de tarjeta, cédulas ni transcripciones completas del ChatAgent, verificado con un script automatizado que escanea los últimos 7 días de logs y ejecuta correcciones",
    "Cláusula de exoneración de responsabilidad dealer/plataforma: Auditar que en el proceso de publicación de cada listing existe un checkbox obligatorio no pre-marcado que el dealer debe activar manualmente confirmando que la información es exacta y de su propiedad, que este evento queda registrado en base de datos con timestamp e IP, y que los Términos de Servicio están redactados para proteger a OKLA de disputas por información incorrecta del dealer y ejecuta correcciones",
    "Consentimiento explícito para comunicaciones de marketing: Auditar que el dealer y el comprador tienen opciones separadas de opt-in para: emails transaccionales (obligatorio), emails de marketing (opt-in voluntario), y WhatsApp de marketing (opt-in voluntario), y que el sistema honra estas preferencias en todos los envíos automatizados y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #8 — COMPETENCIA: SuperCarros lanza freemium o Facebook
    # agrega herramientas especializadas para autos en RD
    # ══════════════════════════════════════════════════════════════════

    # ── Construir el moat de datos
    "OKLA Market Index (datos propietarios): Auditar que el sistema genera automáticamente el primer día de cada mes un reporte de 'Índice de Precios Automotriz OKLA' con: precio promedio por los 20 modelos más transaccionados, variación mensual, y modelos con mayor y menor demanda, publicado en okla.do/mercado como página indexable y distribuido por email a todos los dealers registrados y ejecuta correcciones",
    "OKLA Score como diferenciador público: Auditar que el OKLA Score es visible en los resultados de búsqueda de Google (via structured data), que ningún competidor RD tiene un sistema equivalente documentado públicamente, y que la página de explicación del Score (okla.do/okla-score) está optimizada para keyword 'cómo saber si un carro usado es bueno República Dominicana' y ejecuta correcciones",

    # ── Construir el moat de red
    "Sistema de reseñas verificadas de dealers: Auditar que el sistema permite a compradores que tuvieron conversación con un dealer dejar una reseña verificada (1-5 estrellas + comentario), que las reseñas falsas son bloqueadas por el sistema de moderación, y que un dealer con 10+ reseñas tiene un costo de migración real a otra plataforma porque su reputación no es portátil y ejecuta correcciones",
    "Perfil público del dealer con URL propia: Auditar que cada dealer tiene una URL pública okla.do/dealer/[nombre-dealer] con su inventario completo, reseñas, badge de verificación y datos de contacto, que esta página está indexada en Google, y que el dealer puede compartir su URL de OKLA en WhatsApp y redes sociales como su 'showroom digital' y ejecuta correcciones",

    # ── Construir el moat de integración
    "ChatAgent WhatsApp Business API: Auditar que la integración con WhatsApp Business API de Meta está activa para dealers en plan PRO y ÉLITE, que los compradores pueden iniciar conversación con el dealer desde un número de WhatsApp dedicado de OKLA, y que la latencia de respuesta del agente por WhatsApp es menor a 3 segundos desde que el comprador envía el mensaje y ejecuta correcciones",
    "Registro de marca OKLA en ONAPI: Auditar que la solicitud de registro de marca 'OKLA' y 'OKLA Marketplace' está presentada ante la Oficina Nacional de Propiedad Industrial de RD, que el dominio okla.do está registrado con renovación automática activa por 5 años, y que existe registro de copyright del OKLA Score como metodología propietaria y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #9 — ADOPCIÓN TECNOLÓGICA DEL DEALER: Dealers que no
    # usan el analytics y no entienden el valor de los datos
    # ══════════════════════════════════════════════════════════════════

    "Analytics en lenguaje humano, no técnico: Auditar que el dashboard del dealer traduce cada métrica a una frase accionable en español dominicano (ej: '312 personas vieron tu Corolla esta semana. Eso es 40% más que la semana pasada.' en lugar de 'CTR: 2.3% WoW +40%'), verificado con prueba de usabilidad con 3 dealers del Segmento B sin experiencia digital y ejecuta correcciones",
    "Recomendación automática de acción semanal: Auditar que cada lunes el sistema envía al dealer por WhatsApp una única recomendación accionable generada por AnalyticsAgent (ej: 'Tu Honda Civic 2018 tiene 80 vistas pero 0 consultas. Prueba bajar el precio $500 o agrega 5 fotos del interior.') basada en los datos reales del dealer de la semana anterior y ejecuta correcciones",
    "Benchmark contra dealers similares: Auditar que el dashboard muestra al dealer en plan LIBRE cuántas vistas y consultas recibe en promedio un dealer comparable en plan VISIBLE, usando datos anonimizados reales de la plataforma, con el mensaje '¿Quieres estos resultados?' y el botón de upgrade directamente debajo y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #10 — FRAUDE Y CONFIANZA DEL COMPRADOR: Listings
    # con información falsa destruyen la reputación de OKLA
    # ══════════════════════════════════════════════════════════════════

    "Verificación automática de VIN vs. datos del listing: Auditar que cuando un dealer publica un vehículo, el sistema cruza el VIN con la API de NHTSA vPIC para verificar que la marca, modelo, año y tipo de carrocería declarados coinciden con el VIN, y que si hay discrepancia el listing queda en revisión con mensaje explicativo al dealer antes de publicarse y ejecuta correcciones",
    "Sistema de reporte de listing por compradores: Auditar que el botón 'Reportar este vehículo' está visible en cada listing sin requerir registro, que el formulario tiene categorías específicas (precio incorrecto, vehículo no disponible, fotos no corresponden, posible fraude), que el reporte llega a moderación en menos de 5 minutos, y que tras 3 reportes el listing se suspende automáticamente pendiente de revisión del dealer y ejecuta correcciones",
    "Badge de Dealer Verificado con criterios claros: Auditar que el badge 'Dealer Verificado OKLA' se otorga solo cuando se cumplen: RNC activo verificado en DGII, número de WhatsApp verificado por OTP, al menos 10 listings con fotos reales procesadas por ModerationAgent, y sin reportes de fraude en los últimos 90 días, y que los criterios de verificación son públicos en okla.do/verificacion y ejecuta correcciones",
    "Detección de rollback de odómetro por IA: Auditar que PricingAgent cruza el odómetro declarado por el dealer con el historial de odómetros de VinAudit/CARFAX cuando está disponible, que si detecta un odómetro declarado menor al registrado previamente en la historia del vehículo el listing es flaggeado para revisión manual, y que el comprador ve una alerta visible ('Verificar historial de odómetro') en el listing afectado y ejecuta correcciones",
    "Moderación automática de fotos con IA: Auditar que ModerationAgent rechaza automáticamente: fotos con texto de marca de agua de otro portal (SuperCarros, OLX), fotos claramente de internet (imágenes genéricas del modelo sin placa dominicana visible), fotos con resolución menor a 400x300px, y fotos de documentos o personas, con mensaje al dealer explicando el rechazo específico de cada foto y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #11 — RESILIENCIA OPERACIONAL: Fallos de infraestructura
    # que afectan la experiencia en producción
    # ══════════════════════════════════════════════════════════════════

    "Circuit breaker en todos los servicios externos: Auditar que existe un circuit breaker implementado para cada integración externa (Claude API, Gemini API, NHTSA vPIC, VinAudit, BCRD tipo de cambio, Stripe, WhatsApp Business API) que abre el circuito después de 5 fallos consecutivos en 60 segundos, y que cada agente tiene un comportamiento degradado documentado cuando su API externa está caída y ejecuta correcciones",
    "Health checks y auto-healing en Kubernetes: Auditar que cada pod en DOKS tiene liveness probe y readiness probe configurados correctamente, que un pod que falla 3 veces en 5 minutos es reiniciado automáticamente, y que el sistema envía alerta a Slack del equipo técnico cuando un pod está en CrashLoopBackOff por más de 10 minutos y ejecuta correcciones",
    "Backup y recuperación ante desastre: Auditar que el backup diario de PostgreSQL se ejecuta a las 3am hora RD, se almacena en un bucket de DO Spaces diferente a la región de producción, y que el proceso de restauración completa de la base de datos está documentado y testeado, con un RTO (tiempo de recuperación) menor a 2 horas desde un fallo total y ejecuta correcciones",
    "Degradación elegante cuando la IA no está disponible: Auditar que cuando el LLMGateway no puede alcanzar ni Claude ni Gemini, la plataforma sigue funcionando en modo básico: búsqueda por filtros sin IA, listings visibles sin optimización SEO automática, y el ChatAgent muestra 'Asistente temporalmente no disponible, contacta al dealer directamente' con el WhatsApp del dealer prominente y ejecuta correcciones",
    "Test de carga pre-lanzamiento: Auditar que se ejecuta un test de carga con k6 o Artillery simulando 500 usuarios concurrentes durante 10 minutos, que el sistema mantiene un P95 de latencia menor a 3 segundos, que no hay errores 5xx, y que el autoscaling de DOKS se activa antes de que la latencia supere 2 segundos, con reporte de resultados documentado y ejecuta correcciones",

    # ══════════════════════════════════════════════════════════════════
    # CONTRA #12 — SWITCHING COSTS BAJOS: Dealers que pueden irse
    # a la competencia sin costo después de usar OKLA gratis
    # ══════════════════════════════════════════════════════════════════

    "Historial de analytics no exportable a competidores: Auditar que el dealer tiene acceso a su historial completo de 12 meses de analytics en OKLA (vistas, leads, conversiones por listing) en su dashboard, que estos datos son valiosos para su operación, y que al cancelar su cuenta el acceso a datos históricos se mantiene en modo lectura por 6 meses adicionales como incentivo a la retención y ejecuta correcciones",
    "OKLA Score histórico del vehículo: Auditar que cada vehículo publicado en OKLA acumula un historial de cambios de precio, tiempo en plataforma y número de consultas recibidas, que este historial es visible al comprador como señal de transparencia, y que si el dealer lleva el vehículo a otra plataforma pierde ese historial acumulado creando un switching cost real y ejecuta correcciones",
    "Reseñas de compradores atadas al perfil OKLA del dealer: Auditar que las reseñas verificadas de compradores están asociadas al perfil único del dealer en OKLA, que no son portables a otras plataformas, y que el sistema muestra al dealer cuántas reseñas acumuladas perdería si cancela su cuenta, reforzando el costo de migración con datos concretos y ejecuta correcciones",
]

# ------------------------------------------------
MONITOREOS = [
    "Y sigue monitoriando el archivo .prompts/prompt_6.md.",
    "Continúa monitoreando el archivo .prompts/prompt_6.md.",
    "No olvides monitorear .prompts/prompt_6.md.",
    "Sigue vigilando el archivo .prompts/prompt_6.md.",
    "Mantén el monitoreo de .prompts/prompt_6.md.",
    "Y continúa con la monitorización de .prompts/prompt_6.md.",
    "Además, sigue monitoreando .prompts/prompt_6.md.",
    "Y no dejes de monitorear .prompts/prompt_6.md.",
    "Sigue con el monitoreo de .prompts/prompt_6.md.",
    "Y permanece atento a .prompts/prompt_6.md.",
    "No detengas la monitorización de .prompts/prompt_6.md.",
    "Y prosigue con la vigilancia de .prompts/prompt_6.md.",
    "Y continúa observando .prompts/prompt_6.md.",
    "Sigue revisando .prompts/prompt_6.md.",
    "Mantén la vigilancia sobre .prompts/prompt_6.md.",
    "No pares de monitorear .prompts/prompt_6.md.",
    "Y sigue pendiente de .prompts/prompt_6.md.",
    "Continúa atento a .prompts/prompt_6.md.",
    "Y no descuides el monitoreo de .prompts/prompt_6.md.",
    "Sigue supervisando .prompts/prompt_6.md.",
    "Mantén el control sobre .prompts/prompt_6.md.",
    "Y sigue chequeando .prompts/prompt_6.md.",
    "Continúa con la supervisión de .prompts/prompt_6.md.",
    "No interrumpas la monitorización de .prompts/prompt_6.md.",
]
MONITOREOS = list(set(MONITOREOS))[:50]

print(f"Tamaños: INICIOS={len(INICIOS)}, PRIMERAS_VECES={len(PRIMERAS_VECES)}, TIPOS_AUDITORIA={len(TIPOS_AUDITORIA)}, MONITOREOS={len(MONITOREOS)}")

# ------------------------------------------------------------
# 2. Funciones de generación de mensajes
# ------------------------------------------------------------

def generar_mensaje(prompt_num, tipo_auditoria=None):
    """
    Genera un mensaje combinando aleatoriamente una frase de cada lista.
    Si se proporciona tipo_auditoria (string), se usa ese tipo en lugar de elegir
    uno aleatorio de TIPOS_AUDITORIA, y se añade la orden de ejecutar recomendaciones.
    """
    inicio = random.choice(INICIOS)
    primera_vez = random.choice(PRIMERAS_VECES)
    monitoreo = random.choice(MONITOREOS)

    if tipo_auditoria is not None:
        # Modo secuencial: usamos el tipo proporcionado y ordenamos ejecutar recomendaciones
        parte_auditoria = f"y realiza una auditoría de {tipo_auditoria} y ejecuta inmediatamente sus recomendaciones."
    else:
        # Modo aleatorio (para generar JSON): elegimos una frase de la lista antigua
        parte_auditoria = random.choice(TIPOS_AUDITORIA)

    return f"{inicio} {prompt_num}. {primera_vez} {parte_auditoria} {monitoreo}"

def generar_lista_mensajes(cantidad):
    """Genera una lista de mensajes con prompts aleatorios sin repetición consecutiva."""
    mensajes = []
    ultimo_prompt = None
    for _ in range(cantidad):
        opciones = [p for p in range(1, 6) if p != ultimo_prompt]
        prompt = random.choice(opciones)
        mensajes.append(generar_mensaje(prompt))  # Sin tipo, usa aleatorio
        ultimo_prompt = prompt
    return mensajes

# ------------------------------------------------------------
# 3. Manejo de archivos
# ------------------------------------------------------------

def guardar_json(mensajes, archivo):
    with open(archivo, "w", encoding="utf-8") as f:
        json.dump(mensajes, f, indent=2, ensure_ascii=False)

def cargar_json(archivo):
    with open(archivo, "r", encoding="utf-8") as f:
        return json.load(f)

def asegurar_directorio(ruta):
    Path(ruta).parent.mkdir(parents=True, exist_ok=True)

def escribir_en_archivo(archivo, mensaje):
    asegurar_directorio(archivo)
    with open(archivo, "w", encoding="utf-8") as f:
        f.write(mensaje)

# ------------------------------------------------------------
# 4. Lectura segura del archivo de destino
# ------------------------------------------------------------

def leer_archivo(archivo):
    """Lee el contenido del archivo; retorna cadena vacía si no existe."""
    try:
        with open(archivo, "r", encoding="utf-8") as f:
            return f.read()
    except FileNotFoundError:
        return ""

# ------------------------------------------------------------
# 5. Programa principal
# ------------------------------------------------------------

def main():
    import argparse
    parser = argparse.ArgumentParser(
        description="Actualiza .prompts/prompt_6.md solo cuando detecta la palabra READ en el archivo."
    )
    parser.add_argument("--json", type=str, default="mensajes_100.json",
                        help="Archivo JSON donde guardar 100 mensajes de ejemplo")
    parser.add_argument("--intervalo", type=int, default=2,
                        help="Intervalo en segundos entre comprobaciones del archivo (default: 2)")
    parser.add_argument("--generar-json", action="store_true",
                        help="Solo genera el JSON con 100 mensajes y termina")
    args = parser.parse_args()

    archivo_destino = ".prompts/prompt_6.md"

    if args.generar_json:
        print("Generando 100 mensajes de ejemplo...")
        mensajes_ejemplo = generar_lista_mensajes(100)
        guardar_json(mensajes_ejemplo, args.json)
        print(f"JSON guardado en '{args.json}'")
        return

    print(f"Monitoreando '{archivo_destino}' cada {args.intervalo} segundos.")
    print("Se actualizará el contenido cada vez que se detecte la palabra READ (en mayúsculas).")
    ultimo_prompt = None
    indice_auditoria = 0  # Índice para la lista secuencial

    try:
        while True:
            contenido = leer_archivo(archivo_destino)
            if "READ" in contenido:
                # Calcular el tipo actual (circular)
                tipo_actual = TIPOS_AUDITORIA_SECUENCIAL[indice_auditoria % len(TIPOS_AUDITORIA_SECUENCIAL)]

                # Si es el inicio de un nuevo ciclo, mostramos un mensaje informativo
                if indice_auditoria % len(TIPOS_AUDITORIA_SECUENCIAL) == 0:
                    print("\n--- 🔄 Ciclo completo de auditorías finalizado. Comenzando nuevo ciclo. ---\n")

                # Elegir número de prompt sin repetir el último
                opciones = [p for p in range(1, 6) if p != ultimo_prompt]
                prompt = random.choice(opciones)
                mensaje = generar_mensaje(prompt, tipo_auditoria=tipo_actual)
                escribir_en_archivo(archivo_destino, mensaje)
                print(f"[{time.strftime('%Y-%m-%d %H:%M:%S')}] READ detectado → Prompt 6| Auditoría: {tipo_actual}")
                print(f"Mensaje: {mensaje[:120]}...\n")
                ultimo_prompt = prompt

                # Incrementar el índice para la próxima auditoría
                indice_auditoria += 1

            time.sleep(args.intervalo)
    except KeyboardInterrupt:
        print("\nProceso detenido por el usuario.")

if __name__ == "__main__":
    main()