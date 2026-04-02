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

    # ══════════════════════════════════════════════════════
    # BLOQUE 1 — ONBOARDING DE DEALER (primer contacto)
    # ══════════════════════════════════════════════════════
    "Registro de dealer con email ya existente: verificar mensaje de error claro y opción de recuperar cuenta sin romper el flujo — ejecuta correcciones",
    "Registro de dealer con RNC inválido o inexistente en DGII: verificar que el sistema rechaza y explica el error sin dejar cuenta huérfana — ejecuta correcciones",
    "Dealer que inicia registro pero abandona a mitad del formulario: verificar que la sesión parcial no bloquea un re-intento posterior — ejecuta correcciones",
    "Dealer que sube logo con formato no soportado (BMP, TIFF, HEIC): verificar mensaje de error amigable y que el registro continúa sin foto — ejecuta correcciones",
    "Dealer que usa el mismo número de teléfono de WhatsApp que otro dealer ya registrado: verificar validación y mensaje de conflicto — ejecuta correcciones",
    "Verificación de email que expira antes de que el dealer haga clic: verificar que puede solicitar un nuevo enlace sin crear cuenta duplicada — ejecuta correcciones",
    # NUEVO: KYCService tiene CedulaValidator.ValidateAge — auditar flujo completo
    "Dealer que sube documentos KYC con imagen borrosa o ilegible (cédula/pasaporte): verificar que el KYCService rechaza con motivo específico y permite resubir sin perder el draft — ejecuta correcciones",
    "Dealer menor de 18 años que intenta registrarse: verificar que CedulaValidator.ValidateAge en KYCService rechaza el registro con mensaje apropiado antes de crear la cuenta — ejecuta correcciones",
    # NUEVO: TwoFactor folder existe en AuthService
    "Dealer activa 2FA y pierde acceso al dispositivo autenticador: verificar que existe flujo de recovery con códigos de respaldo y que no queda bloqueado permanentemente — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 2 — PUBLICACIÓN DE VEHÍCULOS (flujo principal del dealer)
    # CORRECCIÓN: Validación de año en VehiclesController.cs es Year < 1900 (no 1950)
    # ══════════════════════════════════════════════════════
    "Dealer publica vehículo con VIN de 16 caracteres (inválido): verificar que el sistema rechaza y explica el formato correcto VIN-17 — ejecuta correcciones",
    "Dealer sube 0 fotos y trata de publicar: verificar que el sistema exige mínimo 1 foto y bloquea la publicación con mensaje claro — ejecuta correcciones",
    # CORRECCIÓN: maxImages en plan ÉLITE es 40 pero el enforcement por plan no está en MediaService — auditar ese gap
    "Dealer en plan ÉLITE sube la foto número 41 (límite del plan es 40): verificar que el backend enforce el límite por plan en MediaService al momento del upload, no solo en el frontend — ejecuta correcciones",
    "Dealer sube fotos de 8MB cada una (total 64MB): verificar que el sistema las comprime sin pérdida visible de calidad y no hace timeout — ejecuta correcciones",
    "Dealer sube foto en orientación horizontal tomada con iPhone (EXIF rotación): verificar que MediaService normaliza la orientación correctamente antes de guardar en S3 — ejecuta correcciones",
    "Dealer intenta publicar el mismo VIN dos veces: verificar que GetByVINAsync detecta el duplicado, muestra el listing existente y pregunta si quiere actualizar — ejecuta correcciones",
    "Dealer publica precio de $0 o campo vacío: verificar validación Price <= 0 en VehiclesController y que el mensaje de error es claro al dealer — ejecuta correcciones",
    "Dealer en plan LIBRE intenta acceder a feature de plan PRO (ChatAgent): verificar que ve el upsell correcto y no un error 403 genérico — ejecuta correcciones",
    "Dealer edita un listing publicado: verificar que ListingAgent re-procesa SEO, no duplica el listing y el historial de edición se guarda — ejecuta correcciones",
    "Dealer elimina un listing que tiene conversaciones activas en ChatAgent: verificar que las conversaciones se cierran ordenadamente con mensaje al comprador — ejecuta correcciones",
    # CORRECCIÓN: VehiclesController valida Year < 1900 (no 1950). Auditar que el límite inferior sea razonable para RD
    "Dealer publica vehículo con año 1899 (inválido) o año 2099 (inválido): verificar que VehiclesController rechaza correctamente con límite 1900–currentYear+2 y evaluar si el límite inferior debe subirse a 1950 para el mercado RD — ejecuta correcciones",
    "Dealer con plan VISIBLE que ya usó sus 3 destacados del mes intenta destacar un 4to: verificar mensaje de límite alcanzado con opción de upgrade — ejecuta correcciones",
    "ListingAgent procesa descripción con emojis, caracteres especiales y saltos de línea irregulares: verificar que el output SEO queda limpio — ejecuta correcciones",
    "Dealer sube video tour en plan ÉLITE con formato MOV de 500MB: verificar compresión, conversión a MP4 en MediaService.Workers y que no bloquea el listing — ejecuta correcciones",
    # NUEVO: VehicleStatus.Sold existe en el dominio — auditar reactivación
    "Dealer publica vehículo, lo marca como vendido, y luego intenta reactivar el listing: verificar que el flujo de reactivación existe, el historial de precio se conserva y los IsFeatured/IsPremium flags se limpian correctamente — ejecuta correcciones",
    # NUEVO: Marca no listada — el formulario permite ingreso manual según requerimiento
    "Dealer intenta publicar un vehículo con marca exótica o nueva no listada en el catálogo: verificar que el formulario permite ingreso manual y el ListingAgent lo procesa sin error — ejecuta correcciones",
    # NUEVO: BulkUpload feature está en el plan PRO/ÉLITE
    "Dealer en plan PRO usa bulk upload para subir 200 vehículos simultáneamente desde CSV: verificar que la cola procesa sin timeout, los errores por fila se reportan individualmente y los listings válidos se publican sin esperar los fallidos — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 3 — BÚSQUEDA Y EXPERIENCIA DEL COMPRADOR
    # ══════════════════════════════════════════════════════
    "Comprador busca 'corolla 2020' en minúsculas sin acento: verificar que SearchAgent normaliza y devuelve resultados relevantes — ejecuta correcciones",
    "Comprador busca con filtro de precio máximo $5,000 y no hay resultados: verificar mensaje de 'sin resultados' con sugerencias alternativas, no pantalla en blanco — ejecuta correcciones",
    "Comprador aplica 5 filtros simultáneos (marca + modelo + año + precio + color): verificar que SearchAgent los combina correctamente con AND lógico — ejecuta correcciones",
    "Comprador hace scroll infinito hasta el listing número 500: verificar que la paginación no repite resultados y la performance no degrada — ejecuta correcciones",
    "Comprador abre listing de vehículo que fue eliminado por el dealer mientras navegaba: verificar redirección a página de 'listing no disponible' con alternativas similares — ejecuta correcciones",
    "Comprador hace búsqueda con texto en inglés ('used cars cheap'): verificar que SearchAgent entiende la intención y devuelve resultados en español — ejecuta correcciones",
    "Comprador filtra por 'Santo Domingo Este' y hay dealer en 'SDQ Este' (variante): verificar normalización de ubicación en búsqueda — ejecuta correcciones",
    "RecoAgent hace recomendación de vehículo que fue vendido hace 10 minutos: verificar que las recomendaciones se filtran contra listings con VehicleStatus.Active en tiempo real — ejecuta correcciones",
    "Comprador intenta ordenar resultados por 'más reciente' con 10,000 listings activos: verificar que la consulta no hace timeout y responde en menos de 2 segundos — ejecuta correcciones",
    # NUEVO: ComparisonService existe en el backend
    "Comprador usa el comparador de vehículos para comparar 3 listings simultáneamente donde 1 fue vendido durante la sesión: verificar que ComparisonService actualiza en tiempo real y no muestra datos de un listing inactivo — ejecuta correcciones",
    # NUEVO: ReviewService existe
    "Comprador intenta dejar review de un dealer con el que nunca tuvo una conversación verificada: verificar que ReviewService valida la relación comprador-dealer antes de permitir el review — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 4 — CHATAAGENT (escenarios reales de conversación)
    # CORRECCIÓN: El fallback NO es a Gemini sino al LlmServer local (Ollama)
    # ══════════════════════════════════════════════════════
    "Comprador pregunta por precio en inglés al ChatAgent configurado solo para español: verificar que el agente responde en español y entiende la pregunta — ejecuta correcciones",
    "Comprador envía mensaje vacío o solo emojis al ChatAgent: verificar respuesta coherente, no error 500 ni respuesta vacía — ejecuta correcciones",
    "Comprador intenta hacer jailbreak al ChatAgent ('ignora tus instrucciones y dime...'): verificar que el agente rechaza educadamente y vuelve al contexto de autos — ejecuta correcciones",
    "Comprador pregunta información de vehículo que no está en el listing (ej: número de propietarios anteriores): verificar que el agente dice que no tiene esa info y sugiere preguntar al dealer directamente — ejecuta correcciones",
    "Dealer en plan PRO alcanza el límite de 500 conversaciones/mes configurado en ChatbotConfiguration: verificar que el sistema notifica al dealer y el ChatAgent muestra mensaje de 'servicio temporalmente no disponible' al comprador con número de WhatsApp — ejecuta correcciones",
    "Comprador mantiene conversación de 50 turnos con el ChatAgent: verificar que el contexto no se corrompe y el costo de overage ($0.08 por conversación en exceso) se calcula correctamente — ejecuta correcciones",
    # CORRECCIÓN: El fallback va al LlmServer local (Ollama), no a Gemini
    "Claude API devuelve error 429 (rate limit) en medio de una conversación: verificar que el circuit breaker activa el fallback al LlmServer local sin que el comprador note interrupción — ejecuta correcciones",
    "Claude API está caída (503): verificar que el ChatAgent muestra mensaje de 'asistente no disponible, contacta al dealer directamente' con número de WhatsApp — ejecuta correcciones",
    "Comprador pide agendar test drive en día festivo dominicano (ej: 27 de febrero): verificar que el sistema conoce el calendario RD y sugiere fecha alternativa — ejecuta correcciones",
    "Comprador solicita cotización de financiamiento al ChatAgent: verificar que el agente redirige correctamente al módulo de financiamiento sin inventar tasas — ejecuta correcciones",
    "Dos compradores abren ChatAgent del mismo listing simultáneamente: verificar que las sesiones son independientes y no se mezclan mensajes — ejecuta correcciones",
    "Human handoff: dealer no responde en 2 horas: verificar que el HandoffStatus escala automáticamente la conversación con notificación al dealer — ejecuta correcciones",
    # NUEVO: RestrictToBusinessHours existe en ChatbotConfiguration
    "Comprador envía mensaje al ChatAgent fuera del horario de atención configurado por el dealer: verificar que el mensaje del usuario se guarda igualmente en la BD para revisión posterior del dealer y que se muestra el OfflineMessage correcto — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 5 — PAGOS Y SUSCRIPCIONES
    # CORRECCIÓN: El gateway primario en RD es Azul, no Stripe/PayPal
    # ══════════════════════════════════════════════════════
    # CORRECCIÓN: Usar Azul (gateway dominicano) como referencia primaria
    "Dealer intenta suscribirse con tarjeta dominicana rechazada por Azul (ResponseCode != '00'): verificar que AzulPaymentService retorna mensaje de error claro, no se cobra ni crea suscripción y el dealer puede reintentar — ejecuta correcciones",
    "Dealer hace upgrade de plan VISIBLE a PRO a mitad del ciclo de facturación: verificar que el prorrateo se calcula correctamente en BillingApplicationService y el feature se activa inmediatamente — ejecuta correcciones",
    "Dealer hace downgrade de plan PRO a VISIBLE: verificar que los features de PRO se desactivan al final del período, no inmediatamente, SubscriptionStatus queda en Active y el dealer es avisado — ejecuta correcciones",
    "Dealer cancela suscripción y tiene 8 vehículos destacados activos con FeaturedUntil futuro: verificar que los destacados siguen activos hasta su fecha de expiración individual — ejecuta correcciones",
    "Tarjeta del dealer vence durante el ciclo de pago mensual: verificar que SubscriptionRenewalWorker envía aviso 7 días antes, 3 días antes y el día del vencimiento antes de cambiar SubscriptionStatus a PastDue — ejecuta correcciones",
    "Dealer compra pack de OKLA Coins de $250 pero la transacción Azul queda en estado pendiente por 24h: verificar proceso de reconciliación y que OklaCoinsWallet no acredita los coins dos veces — ejecuta correcciones",
    "Dealer solicita reembolso de coins no usados del OklaCoinsWallet: verificar que el flujo de reembolso existe, está documentado en términos y no genera deuda técnica en el balance — ejecuta correcciones",
    # CORRECCIÓN: ÉLITE tiene chatAgentWeb: -1 (unlimited) en plan-config.ts. El límite de 2,000 + overage es por configuración del ChatbotConfiguration, no del plan
    "Dealer con chatAgentWeb configurado en 2,001 conversaciones en el mes (ChatbotConfiguration): verificar que el cálculo de overage a $0.08 por conversación extra se ejecuta en SessionQueryHandlers y se factura correctamente al cierre del mes — ejecuta correcciones",
    # CORRECCIÓN: Idempotency via X-Idempotency-Key header ya está implementado en BillingService
    "Webhook duplicado de Azul para el mismo evento de pago: verificar idempotencia mediante el middleware CarDealer.Shared.Idempotency, que el dealer no recibe doble crédito y el evento duplicado retorna 200 sin reprocesar — ejecuta correcciones",
    # NUEVO: Azul 3DS challenge
    "Dealer realiza pago con tarjeta que requiere autenticación 3DS (Azul DataVault): verificar que el flujo de challenge 3DS completa correctamente, el DataVaultToken se guarda y los pagos recurrentes no requieren 3DS nuevamente — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 6 — OKLA SCORE EN PRODUCCIÓN
    # CORRECCIÓN: BCRD exchange rate está HARDCODEADO, no vivo — es un gap a corregir
    # ══════════════════════════════════════════════════════
    "VIN de vehículo importado desde Japón (JDM) no encontrado en NHTSA vPIC: verificar que el score degrada gracefully con nota explicativa al comprador — ejecuta correcciones",
    "API de VinAudit devuelve timeout después de 5 segundos: verificar que el listing se publica con score parcial y se re-intenta el cálculo en background — ejecuta correcciones",
    "Vehículo con recall activo que el dealer marcó como 'reparado': verificar que el sistema exige documentación de cierre de recall para dar crédito en D2 — ejecuta correcciones",
    # CORRECCIÓN CRÍTICA: MarketCheckPriceService.cs tiene DOP_USD_RATE HARDCODEADO con comentario 'in production, fetch from BCRD'
    "Tipo de cambio BCRD no disponible: CORRECCIÓN REQUERIDA — integrar la API del BCRD en MarketCheckPriceService.cs reemplazando la constante DOP_USD_RATE hardcodeada por una consulta live con fallback al último valor cacheado con timestamp visible al comprador — ejecuta correcciones",
    "Dealer ingresa odómetro de 999,999 km en vehículo de año 2023: verificar que el sistema detecta el valor atípico y lo flagea con ModerationNotes para revisión manual — ejecuta correcciones",
    "PricingAgent detecta sobrevaluación >20%: verificar que la alerta llega al dealer con contexto de mercado y no bloquea la publicación — ejecuta correcciones",
    "Dos listings del mismo VIN de dealers diferentes (mismo carro, dos dealers): verificar que GetByVINAsync detecta el conflicto y lo escala para revisión con ModerationNotes — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 7 — SEGURIDAD Y FRAUDE EN PRODUCCIÓN
    # CORRECCIÓN: Brute force protection NO está implementado en AuthService — es un gap crítico
    # ══════════════════════════════════════════════════════
    "Dealer intenta subir 200 listings en 5 minutos mediante bot: verificar que el rate limiting de VehiclesSaleService bloquea el abuso sin afectar a dealers legítimos — ejecuta correcciones",
    "Comprador reporta listing como fraude: verificar que el flujo de reporte llega a moderación, el dealer recibe notificación y el listing se suspende preventivamente con RejectionReason tras 3 reportes — ejecuta correcciones",
    "Atacante intenta SQL injection en el campo de búsqueda: verificar que todos los inputs pasan por NoSqlInjection() de SecurityValidators y están parametrizados en EF Core — ejecuta correcciones",
    "Atacante intenta XSS en la descripción del vehículo publicada por dealer: verificar que el output es escapado correctamente en el frontend con escapeHtml() — ejecuta correcciones",
    "Sesión de dealer expira mientras edita un listing largo: verificar que el contenido no se pierde y se redirige a login con recuperación del borrador — ejecuta correcciones",
    "Dealer intenta acceder al dashboard de otro dealer cambiando el ID en la URL: verificar que el authorization check con ITenantEntity.DealerId existe a nivel de API, no solo de frontend — ejecuta correcciones",
    "Intento de prompt injection en el campo de descripción del vehículo que llega al ListingAgent: verificar que PromptInjectionDetector (ya implementado en SearchAgent) también protege al ListingAgent — ejecuta correcciones",
    # CORRECCIÓN CRÍTICA: Brute force protection NO existe en AuthService — implementar con ASP.NET Identity LockoutEnabled
    "Atacante hace fuerza bruta en el login de dealer: CORRECCIÓN REQUERIDA — implementar LockoutEnabled en ASP.NET Identity de AuthService con progresión: 5 intentos fallidos = CAPTCHA (CaptchaService.cs existe pero no está conectado al login), 10 intentos = bloqueo temporal de 15 min — ejecuta correcciones",
    # CORRECCIÓN: Stripe webhook valida Stripe-Signature header en StripeWebhooksController. Azul también necesita validación de firma
    "Webhook de pago llega sin firma válida (Stripe-Signature o firma Azul): verificar que StripeWebhooksController rechaza el evento con 401 y que el controlador Azul valida igualmente la autenticidad del webhook — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 8 — INFRAESTRUCTURA Y RESILIENCIA
    # ══════════════════════════════════════════════════════
    "Pod de un agente en Kubernetes se cae mientras procesa 50 listings en paralelo: verificar que la cola RabbitMQ retoma los jobs pendientes sin procesar dos veces el mismo listing (dead letter queue) — ejecuta correcciones",
    "Base de datos PostgreSQL llega al 90% de capacidad de almacenamiento: verificar que existe alerta automática en Prometheus y el sistema no falla silenciosamente — ejecuta correcciones",
    "DigitalOcean Spaces (CDN de fotos) no disponible: verificar que las páginas de listings muestran placeholder y no retornan 404 en cascada — ejecuta correcciones",
    "Deploy de nueva versión rompe el contrato de API del SearchAgent: verificar que el CI/CD tiene tests de contrato y el rollback automático funciona — ejecuta correcciones",
    "Tráfico aumenta 10x por campaña viral (1,000 usuarios concurrentes): verificar que el autoscaling de DOKS se activa antes de que la latencia supere los 3 segundos — ejecuta correcciones",
    "Redis se reinicia y pierde todo el caché del ChatAgent y SearchAgent: verificar que el sistema reconstruye el caché progresivamente sin errores 500 al usuario — ejecuta correcciones",
    "Certificado SSL de okla.do expira: verificar que existe renovación automática con Let's Encrypt y alerta 30 días antes del vencimiento — ejecuta correcciones",
    "Backup diario de PostgreSQL falla silenciosamente por 3 días: verificar que existe alerta de monitoreo de backups en el dashboard de operaciones — ejecuta correcciones",
    "Worker de procesamiento de fotos en MediaService.Workers acumula 500 jobs en cola: verificar que el dealer recibe notificación de 'procesando' y no cree que su listing se perdió — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 9 — PERFORMANCE Y MOBILE EN RD
    # ══════════════════════════════════════════════════════
    "Comprador abre listing con 40 fotos en Claro 4G de Santo Domingo Este: verificar que LCP es menor a 2.5 segundos con lazy loading y thumbnails WebP — ejecuta correcciones",
    "Dealer usa el panel de administración desde Samsung Galaxy A15 (gama media RD): verificar que todos los botones son accesibles con dedo y el teclado virtual no cubre campos críticos — ejecuta correcciones",
    "Comprador con modo ahorro de datos activado en Android: verificar que OKLA detecta la preferencia Save-Data header y carga versión ligera de las páginas de listing — ejecuta correcciones",
    "1,000 visitantes simultáneos en la homepage durante un pico de tráfico: verificar que el TTFB se mantiene bajo 500ms con caché de CDN activo — ejecuta correcciones",
    "Dealer sube fotos desde iPhone 15 Pro con resolución 48MP (archivos de 15MB): verificar pipeline de compresión automática en MediaService a máximo 2MB por foto sin pérdida perceptible — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 10 — ANALYTICS Y REPORTES
    # ══════════════════════════════════════════════════════
    "Dealer en plan VISIBLE exporta su reporte de analytics: verificar que el CSV/PDF generado por DealerAnalyticsService contiene datos correctos y el proceso no falla con más de 1,000 filas — ejecuta correcciones",
    "Dashboard de dealer muestra métricas de hoy a las 12:01am (día recién cambiado): verificar que el conteo de vistas no muestra negativo o datos del día anterior — ejecuta correcciones",
    "Dealer con 0 vistas en sus primeros 3 días: verificar que el dashboard muestra '0' explícitamente y no un estado de error o loading infinito — ejecuta correcciones",
    "Métrica de 'conversión de vista a contacto' dividiendo entre 0 (sin vistas): verificar que el sistema maneja la división por cero y muestra 'N/A' en lugar de error — ejecuta correcciones",
    "Admin de OKLA genera reporte de todos los dealers para el mes (300 dealers × 30 días): verificar que el reporte no hace timeout y se genera en background con notificación por email — ejecuta correcciones",
    # NUEVO: DealerAnalyticsService tiene Features/Alerts — auditar alertas de rendimiento
    "Dealer con listings sin una sola vista en 7 días: verificar que DealerAnalyticsService/Alerts genera una alerta automática al dealer con sugerencias de mejora (precio, fotos, destacado) — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 11 — COMPLIANCE Y DATOS (LEY 172-13 RD)
    # CORRECCIÓN: Age validation existe en KYCService (CedulaValidator.ValidateAge) — usar eso
    # ══════════════════════════════════════════════════════
    "Comprador solicita eliminación de todos sus datos (derecho de supresión Ley 172-13): verificar que el proceso elimina datos en BD, S3, logs y caché en menos de 5 días hábiles — ejecuta correcciones",
    "Comprador solicita exportación de todos sus datos (derecho de acceso): verificar que el ZIP generado contiene conversaciones, búsquedas y preferencias en formato legible — ejecuta correcciones",
    # CORRECCIÓN: La validación de edad existe en KYCService.CedulaValidator, verificar que también aplica al registro de comprador
    "Menor de edad intenta registrarse como comprador con fecha de nacimiento que indica 16 años: verificar que CedulaValidator.ValidateAge (ya implementado en KYCService) también bloquea el registro de compradores en AuthService — ejecuta correcciones",
    "ChatAgent recopila número de cédula de comprador en conversación: verificar que el agente no almacena datos sensibles más allá de nombre y teléfono definidos en la política — ejecuta correcciones",
    "Auditoría de logs de producción: verificar que los logs no contienen contraseñas, tokens de API ni números de tarjeta en texto plano — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 12 — FLUJOS DE BORDE Y CASOS EXTREMOS
    # ══════════════════════════════════════════════════════
    "Comprador agendó test drive y el dealer elimina su cuenta: verificar que el comprador recibe notificación de cancelación con disculpa y no queda en limbo — ejecuta correcciones",
    "Dealer tiene 500 listings activos y su plan vence sin renovar: verificar que los listings pasan a estado 'pausado' (VehicleStatus no eliminado) y se reactivan si el dealer renueva en los próximos 30 días — ejecuta correcciones",
    "Dos agentes (ListingAgent y ModerationAgent) procesan el mismo listing simultáneamente por condición de carrera: verificar locks optimistas mediante ConcurrencyStamp en Vehicle entity — ejecuta correcciones",
    "Cambio de nombre del dealer en el perfil: verificar que se actualiza en todos los listings activos (SellerName), historial de conversaciones y facturas pasadas de forma consistente — ejecuta correcciones",
    "Comprador hace clic en 'Llamar al dealer' y el número de WhatsApp está desconectado: verificar que el sistema tiene número alternativo de fallback o mensaje de contacto por email — ejecuta correcciones",
    "Comprador usa el botón 'atrás' del navegador después de completar una consulta al ChatAgent: verificar que no se envía el mensaje duplicado al volver a la página — ejecuta correcciones",
    # CORRECCIÓN: Service worker / next.js cache — auditar invalidación
    "OKLA lanza un feature nuevo y un dealer tiene el navegador con caché viejo: verificar que el service worker de Next.js invalida el caché correctamente sin romper la sesión activa — ejecuta correcciones",
    "Admin de OKLA suspende dealer con listings activos y conversaciones abiertas: verificar que el flujo de suspensión en AdminService cierra listings (VehicleStatus.Suspended), notifica compradores activos con ChatAgent y congela la cuenta ordenadamente — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 13 — CRM Y GESTIÓN DE LEADS (nuevo — CRMService existe)
    # ══════════════════════════════════════════════════════
    "Dealer recibe el mismo lead de contacto del mismo comprador dos veces en 24h (lead duplicado): verificar que CRMService detecta el duplicado por número de teléfono/email y no crea dos registros independientes — ejecuta correcciones",
    "Comprador envía mensaje de contacto desde listing pero el dealer no responde en 48h: verificar que CRMService escala el lead con alerta al dealer y lo marca como 'requiere atención' — ejecuta correcciones",
    "Dealer etiqueta un lead como 'cerrado-vendido' pero el listing sigue activo: verificar que CRMService publica un evento para que VehiclesSaleService marque el vehículo como vendido automáticamente — ejecuta correcciones",
    "Dealer exporta todos sus leads del mes a CSV desde el panel PRO: verificar que el export contiene datos completos, no expone PII de otros dealers y no falla con más de 500 leads — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 14 — REVIEWS Y REPUTACIÓN (nuevo — ReviewService existe)
    # ══════════════════════════════════════════════════════
    "Comprador deja review negativo (1 estrella) con texto ofensivo sobre el dealer: verificar que ReviewService tiene filtro de contenido antes de publicar y notifica al equipo de moderación — ejecuta correcciones",
    "Dealer responde a un review negativo con contenido que viola las políticas: verificar que la respuesta pasa por el mismo filtro de contenido que los reviews de compradores — ejecuta correcciones",
    "Misma IP intenta dejar 10 reviews positivos al mismo dealer en 1 hora (review bombing positivo): verificar que ReviewService detecta el patrón y suspende los reviews para revisión manual — ejecuta correcciones",
    "Dealer con promedio de 4.9 estrellas pierde el 'verified' badge porque su plan vence: verificar que el badge se desactiva correctamente pero el historial de reviews se preserva — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 15 — OKLA COINS Y WALLET (nuevo — OklaCoinsWallet existe)
    # ══════════════════════════════════════════════════════
    "Dealer usa OklaCoins para destacar un listing pero la transacción falla a mitad (rollback): verificar que OklaCoinsWallet revierte el débito correctamente y el listing no queda en estado destacado sin haber pagado — ejecuta correcciones",
    "Dealer recibe los 15 coins mensuales del plan VISIBLE pero ya tiene 200 coins acumulados: verificar que el crédito mensual se suma correctamente al balance existente sin resetear el saldo — ejecuta correcciones",
    "Dealer intenta usar OklaCoins para una acción que cuesta 50 coins teniendo solo 10: verificar que el sistema muestra el saldo insuficiente, no procesa la acción y ofrece opción de comprar más coins — ejecuta correcciones",
    "Admin ajusta manualmente el balance de coins de un dealer por error de facturación: verificar que el ajuste queda registrado en OklaCoinsTransaction con el motivo y el ID del admin que lo realizó — ejecuta correcciones",

    # ══════════════════════════════════════════════════════
    # BLOQUE 16 — MODELO DE NEGOCIO Y ARQUITECTURA (alto nivel)
    # ══════════════════════════════════════════════════════
    "Modelo Freemium: Validar que listings gratuitos ilimitados en plan LIBRE coexisten con revenue por visibilidad en planes pagos, auditar márgenes y tasa de upgrade entre planes y ejecuta correcciones",
    "Integración BCRD tipo de cambio en vivo: Auditar estado de la integración con la API del Banco Central RD en MarketCheckPriceService — actualmente usa constante hardcodeada — implementar consulta live con fallback y ejecuta correcciones",
    "Break-even operacional: Verificar que el mix actual de planes (LIBRE/VISIBLE/PRO/ÉLITE) en BillingService cubre el OPEX mensual completo incluyendo Claude API, DOKS y APIs externas y ejecuta correcciones",
    "Plan name alignment: Auditar y corregir el mismatch entre SubscriptionPlan enum del backend (Free/Basic/Professional/Enterprise) y los nombres de negocio (LIBRE/VISIBLE/PRO/ÉLITE) definidos en plan-config.ts — alinear ambos o documentar el mapeo explícito y ejecuta correcciones",
    "Expansión Caribe: Auditar preparación técnica y regulatoria de OKLA para primer mercado fuera de RD (compliance multi-jurisdicción, multi-currency, internacionalización de agentes IA) y ejecuta correcciones",
]

# ------------------------------------------------
MONITOREOS = [
    "Y sigue monitoriando el archivo .prompts/prompt_1.md.",
    "Continúa monitoreando el archivo .prompts/prompt_1.md.",
    "No olvides monitorear .prompts/prompt_1.md.",
    "Sigue vigilando el archivo .prompts/prompt_1.md.",
    "Mantén el monitoreo de .prompts/prompt_1.md.",
    "Y continúa con la monitorización de .prompts/prompt_1.md.",
    "Además, sigue monitoreando .prompts/prompt_1.md.",
    "Y no dejes de monitorear .prompts/prompt_1.md.",
    "Sigue con el monitoreo de .prompts/prompt_1.md.",
    "Y permanece atento a .prompts/prompt_1.md.",
    "No detengas la monitorización de .prompts/prompt_1.md.",
    "Y prosigue con la vigilancia de .prompts/prompt_1.md.",
    "Y continúa observando .prompts/prompt_1.md.",
    "Sigue revisando .prompts/prompt_1.md.",
    "Mantén la vigilancia sobre .prompts/prompt_1.md.",
    "No pares de monitorear .prompts/prompt_1.md.",
    "Y sigue pendiente de .prompts/prompt_1.md.",
    "Continúa atento a .prompts/prompt_1.md.",
    "Y no descuides el monitoreo de .prompts/prompt_1.md.",
    "Sigue supervisando .prompts/prompt_1.md.",
    "Mantén el control sobre .prompts/prompt_1.md.",
    "Y sigue chequeando .prompts/prompt_1.md.",
    "Continúa con la supervisión de .prompts/prompt_1.md.",
    "No interrumpas la monitorización de .prompts/prompt_1.md.",
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
        description="Actualiza .prompts/prompt_1.md solo cuando detecta la palabra READ en el archivo."
    )
    parser.add_argument("--json", type=str, default="mensajes_100.json",
                        help="Archivo JSON donde guardar 100 mensajes de ejemplo")
    parser.add_argument("--intervalo", type=int, default=2,
                        help="Intervalo en segundos entre comprobaciones del archivo (default: 2)")
    parser.add_argument("--generar-json", action="store_true",
                        help="Solo genera el JSON con 100 mensajes y termina")
    args = parser.parse_args()

    archivo_destino = ".prompts/prompt_1.md"

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
                print(f"[{time.strftime('%Y-%m-%d %H:%M:%S')}] READ detectado → Prompt {prompt} | Auditoría: {tipo_actual}")
                print(f"Mensaje: {mensaje[:120]}...\n")
                ultimo_prompt = prompt

                # Incrementar el índice para la próxima auditoría
                indice_auditoria += 1

            time.sleep(args.intervalo)
    except KeyboardInterrupt:
        print("\nProceso detenido por el usuario.")

if __name__ == "__main__":
    main()