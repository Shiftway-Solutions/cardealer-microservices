y tambien quiero que me implementes en la plataforma el modelo de negocion de okla y que los precios y todo lo que sea necesario que sea configurable. Este es el modelo de negocio, "ANÁLISIS DE NEGOCIO Y ECONÓMICO
MODELO FREEMIUM OKLA MARKETPLACE
Publicaciones Ilimitadas Gratis · Monetización por Publicidad · Estrategia vs. Facebook Marketplace RD

v3.0 — Reestructuración completa del modelo de ingresos
Versión 3.0 | Febrero 2026 | República Dominicana | Moneda: USD
Normas: PMI PMBOK 7th · IEEE Std 1058-1998 · ISO 31000:2018 · BABOK v3

1. RESUMEN EJECUTIVO — EL MODELO CORRECTO PARA OKLA
   PREMISA CENTRAL: OKLA no debe ganar dinero por el hecho de que un dealer publique un vehículo. Debe ganar dinero porque ese dealer quiere que MÁS compradores vean su vehículo. Esta distinción cambia todo el modelo de negocio y convierte a OKLA en una plataforma que los dealers quieren usar, no una que tienen que pagar para acceder.

La gran pregunta es: ¿cómo puede OKLA competir con Facebook Marketplace si este es gratuito? La respuesta no es cobrar por publicar — eso sería perder antes de empezar. La respuesta es ser MEJOR que Facebook en todo lo que importa al dealer automotriz, y cobrar solo por visibilidad adicional y herramientas profesionales que Facebook no puede ofrecer.

Este documento analiza: (1) el costo real que tiene OKLA cada vez que un dealer publica un vehículo, (2) por qué ese costo hace viable ofrecer publicaciones gratuitas e ilimitadas, (3) la nueva estructura de 4 planes con foco en publicidad y herramientas, y (4) las proyecciones financieras que demuestran la rentabilidad del modelo desde los 45 dealers activos.

Indicadores Clave del Modelo
Costo para OKLA por cada vehículo publicado: $0.025 (solo 2.5 centavos). Un dealer con 100 vehículos cuesta $2.50/mes.
Break-even del marketplace: 45 dealers activos (mezcla de planes + publicidad).
Ventaja competitiva vs. SuperCarros (líder RD): OKLA es GRATIS vs. sus planes de pago con límites.
Ventaja competitiva vs. Facebook: especialización, IA, SEO, ChatAgent 24/7, datos estructurados.
Ingresos proyectados a 100 dealers: $4,615/mes solo de marketplace + $3,487/mes de ChatAgent.

2. COSTO REAL POR PUBLICACIÓN DE VEHÍCULO
   2.1 Anatomía del Costo
   Cada vez que un dealer publica un vehículo, OKLA ejecuta automáticamente varios agentes de inteligencia artificial para procesar y optimizar la publicación. El análisis detallado de cada componente muestra por qué la publicación gratuita ilimitada no solo es viable sino económicamente óptima:

Componente
Agente / Recurso
Modelo
Costo/pub.
Detalle técnico
Procesamiento del listado
ListingAgent
Sonnet 4.5
$0.011
500 tk input fresh + 2,000 cached + 600 output. SEO title, tags, deduplicación.
Moderación de fotos (8 fotos)
ModerationAgent
Haiku 4.5 Vision
$0.013
~1,500 tk/imagen × 8 fotos = 12K tk input + 200 output. Pass/fail + tags.
Almacenamiento fotos (mes 1)
DO Spaces
— CDN —
$0.001
8 fotos originales + 3 tamaños (thumbnails). ~36MB por listing. $0.02/GB.
Almacenamiento mensual recurrente
DO Spaces + CDN
—
$0.001/mes
Por cada listado activo. 1,000 listados = $1.00/mes en storage.
Generación de página SEO
ListingAgent (incluido)
Sonnet 4.5
$0.00
El SEO title/meta se genera en el mismo procesamiento. Sin costo adicional.
Base de datos (PostgreSQL DO)
DO Managed DB
—
$0.0001
Costo marginal de write + storage en DB. Despreciable. Incluido en $15/mes plan.
TOTAL COSTO POR PUBLICACIÓN
2 Agentes
Sonnet + Haiku
~$0.025
Costo total primer mes. Meses siguientes: $0.001/mes por listing activo.
DEALER CON 100 VEHÍCULOS/MES
100 listings

$2.50
Costo total para OKLA si un dealer publica 100 vehículos en un mes.

CONCLUSIÓN CRÍTICA: El costo real para OKLA por publicación es de aproximadamente 2.5 centavos de dólar. Un dealer con un inventario de 200 vehículos publicados cuesta a OKLA $5.00 al mes en procesamiento inicial, más $0.20 al mes en almacenamiento activo. Esto es completamente despreciable comparado con cualquier plan de suscripción. Las publicaciones ilimitadas gratuitas tienen un costo de infraestructura cercano a cero.

2.2 Proyección de Costos de Publicación a Escala
El siguiente cálculo demuestra que incluso en el escenario más agresivo de crecimiento, el costo de procesar publicaciones es marginal para OKLA:

Escenario
Dealers activos
Listings totales
Costo proc./mes
Storage activo/mes
Lanzamiento
50
2,500
$62.50
$2.50/mes (25,000 fotos × 36MB ÷ 1,000 = 900GB → $18/mes DO Spaces)
Crecimiento
100
8,000
$200
$8/mes — cubierto por CUALQUIER dealer en plan PRO o ÉLITE
Escala
300
30,000
$750
$30/mes en storage. Con 300 dealers en plan VISIBLE ($29) = $8,700/mes ingresos.

En el peor escenario (300 dealers, 30,000 listings), el costo total de todas las publicaciones es de $750/mes en processing + $30/mes storage = $780/mes. Con apenas 27 dealers en plan VISIBLE ($29) quedan cubiertos todos los costos de publicación de toda la plataforma.

3. ANÁLISIS COMPETITIVO: OKLA VS. FACEBOOK MARKETPLACE Y PORTALES RD
   3.1 El Mercado Automotriz Digital en República Dominicana
   El ecosistema competitivo en República Dominicana tiene tres tipos de actores. Primero, Facebook Marketplace que es gratuito y masivo pero genérico y sin herramientas especializadas. Segundo, portales especializados como SuperCarros.com (líder con 1.25 millones de visitas/mes), Yacarros.com y SuCarroRD.com — todos cobran por publicar y tienen límites de listings. Tercero, WhatsApp y grupos de Facebook que funcionan como classified informales.
   El espacio que OKLA debe ocupar es claro: especialización automotriz de Facebook (gratis, ilimitado) + herramientas profesionales que los portales de pago no tienen (IA, automatización, ChatAgent, datos estructurados).

Plataforma
Publicaciones
Límite listings
Herramientas IA
WhatsApp Bot
Ventaja OKLA
Facebook Marketplace
Gratis
Sin límite
Básico
No nativo
OKLA = igual en listings + superior en tools
SuperCarros.com (líder RD)
Pago
Limitado (planes)
Ninguna
No
OKLA: gratis + IA + WhatsApp bot
SuCarroRD.com
2 gratis, resto pago
30 máximo
Ninguna
No
OKLA: ilimitados + IA gratuito
Yacarros.com
Pago
Limitado
Ninguna
No
OKLA: ilimitados + automatización
OKLA Marketplace
GRATIS ILIMITADO
SIN LÍMITE
IA Completa
Sí (planes Pro+)
Mejor propuesta de valor del mercado

3.2 Por Qué OKLA Puede Ganarle a Facebook
El argumento no es que OKLA tenga más usuarios que Facebook — eso es imposible y no es el objetivo. El argumento es que OKLA tiene MEJOR calidad de compradores para el dealer automotriz:

Dimensión
Facebook Marketplace
OKLA Marketplace
Argumento de venta OKLA
Costo de publicación
GRATIS
GRATIS ILIMITADO
Empate — OKLA no tiene desventaja aquí.
Calidad del lead/comprador
Mixta (turistas, curiosos)
Comprador automotriz activo
"En Facebook compra de todo. En OKLA solo vienen a comprar carros."
Datos del vehículo estructurados
Texto libre
VIN + specs + historial IA
PricingAgent valora el vehículo automáticamente. Transparencia genera confianza.
Atención automática 24/7
Messenger manual
ChatAgent IA (web + WhatsApp)
El comprador a las 11pm recibe respuesta inmediata. FB: silencio.
Agendamiento de citas
No
Automático IA
El agente agenda el test drive sin que el vendedor intervenga.
SEO/Google indexación
Limitada (cerrado)
Página SEO optimizada por IA
Cada vehículo tiene URL propia indexada. El dealer aparece en Google.
Perfil profesional del dealer
Página de Facebook genérica
Showroom digital OKLA
URL okla.do/dealer/[nombre]. Logo, inventario, reseñas, mapa, horarios.
Analytics del dealer
Muy básico
Vistas, contactos, conversiones
El dealer sabe qué vehículos generan más interés y cuándo.
Dependencia del algoritmo
Totalmente dependiente de FB
Plataforma propia
FB puede cambiar el algoritmo mañana y hundir el alcance orgánico del dealer.

ARGUMENTO DE VENTA CLAVE: 'En Facebook, tu listing de Corolla 2022 compite con alguien vendiendo licuadoras y bicicletas. En OKLA, cada persona que ve tu listing llegó porque estaba buscando exactamente ese tipo de vehículo.' La intención de compra en un portal especializado es estructuralmente superior a la de un marketplace generalista.

3.3 La Estrategia de Adquisición de Dealers
La estrategia para convencer a un dealer que usa Facebook Marketplace de registrarse en OKLA tiene tres fases. Primero, reducción de la fricción de entrada: publicaciones ilimitadas sin costo, migración asistida por IA del inventario desde Facebook (el ListingAgent puede procesar fotos y textos existentes), y 30 días gratis en cualquier plan de pago. Segundo, demostración de valor: en los primeros 30 días, el dealer recibe un reporte de OKLA Analytics mostrando cuántas vistas generó, cuántos leads recibió y de qué provincia vienen sus compradores. Facebook no da esa granularidad. Tercero, monetización: una vez que el dealer ve resultados, las conversaciones sobre planes de visibilidad y ChatAgent se vuelven mucho más fáciles.

TÁCTICA: Contactar a los dealers activos en Facebook Marketplace RD y ofrecerles importar su inventario a OKLA gratis. El ListingAgent puede tomar las URLs de sus listings de Facebook y procesarlos automáticamente.
TÁCTICA: Asociarse con los principales concesionarios de Santo Domingo en la fase de beta (mes 1-3). Unos pocos dealers grandes con inventario de 200-500 vehículos crean la masa crítica de listings que atrae compradores.
TÁCTICA: Publicar en grupos de WhatsApp de dealers dominicanos con un pitch simple: 'Publica todos tus carros gratis. Sin límites. Con IA que optimiza tus listings. Pruébalo.'

4. NUEVA ESTRUCTURA DE PLANES — FOCO EN VISIBILIDAD, NO EN ACCESO
   4.1 Principio de Diseño
   La gran falla de los planes anteriores (Starter $49 / Pro $149 / Enterprise $399) es que cobraban por acceso a funciones básicas, incluyendo el número de conversaciones del ChatAgent. El nuevo diseño parte de un principio diferente: el acceso básico siempre es gratuito, los planes se diferencian por visibilidad (cuántos compradores ven mis vehículos) y por herramientas (qué tan bien puedo atenderlos). Esto alinea los incentivos de OKLA con los del dealer: OKLA gana más cuando el dealer vende más.

CARACTERÍSTICA
LIBRE
VISIBLE $29/mes
PRO $89/mes
ÉLITE $199/mes
Publicaciones de vehículos
∞ ILIMITADAS
∞ ILIMITADAS
∞ ILIMITADAS
∞ ILIMITADAS
Fotos por vehículo
Hasta 10
Hasta 20
Hasta 30
Hasta 40 + video tour
Posición en búsquedas
Estándar
Prioridad media
Alta prioridad
Top prioridad
Vehículos DESTACADOS incluidos/mes
—
3 vehículos/mes
10 vehículos/mes
25 vehículos/mes
Créditos publicitarios incluidos/mes
—
$15 créditos
$45 créditos
$120 créditos
Badge 'Dealer Verificado OKLA'
—
✓
✓ Dorado
✓ Premium
Dashboard Analytics
—
Básico
Avanzado
Completo + exportar
ChatAgent web (atención IA al comprador)
—
—
✓ 500 conv/mes
✓ ILIMITADO
ChatAgent WhatsApp
—
—
✓ + 500 conv/mes
✓ ILIMITADO
Agendamiento de citas automático
—
—
✓
✓ + recordatorios WA
Human handoff
—
—
✓ Email alert
✓ Live chat + CRM
Valoración IA del vehículo (PricingAgent)
1 gratis
5/mes
Ilimitada
Ilimitada + informe PDF
Perfil público del dealer
Básico
Mejorado
Premium
Premium + showcase homepage
Soporte OKLA
FAQ
Email 48h
Chat 12h
Dedicado 4h
COSTO OKLA / DEALER / MES
$0.05–1.00
$1.50
$68
$228
MARGEN BRUTO OKLA
N/A (lead gen)
95% ($27.50)
24% ($21)
(-15%) al inicio → 47% a escala

NOTA SOBRE COSTOS: El plan ÉLITE muestra un margen negativo inicial (-15%) porque el DealerChatAgent sin restricción de conversaciones puede costar hasta $228/mes a OKLA en Claude API. Sin embargo, a escala (con caching optimizado y mayor volumen), el margen se normaliza al 47%. La solución es un límite soft de 2,000 conversaciones/mes para el plan ÉLITE con overage de $0.08/conversación adicional.

4.2 Lógica del Escalado de Precios
La progresión $0 → $29 → $89 → $199 está diseñada psicológicamente para maximizar la conversión. El salto del plan LIBRE al VISIBLE ($29) es el más importante y tiene el mayor margen (95%). El dealer que publica vehículos y no recibe contactos es candidato natural para el plan VISIBLE — el argumento es: 'tus vehículos están publicados, solo necesitas que más compradores los vean'. El salto al PRO ($89) se justifica con el acceso al ChatAgent, que para un dealer con personal es inmediatamente tangible en ahorro de tiempo. El ÉLITE ($199) se vende por el ROI del ChatAgent ilimitado y el showcase en homepage.

PSICOLOGÍA DE PRECIOS: El plan VISIBLE a $29/mes es el 'anzuelo' — tiene el margen más alto (95%) y es lo suficientemente barato para que casi cualquier dealer lo pruebe. Una vez dentro, la upsell al PRO ($89) se convierte en una conversación sobre el ChatAgent, que el dealer puede ver funcionando en su perfil OKLA.

5. CATÁLOGO DE PRODUCTOS PUBLICITARIOS
   5.1 Estructura de Ingresos Publicitarios
   Además de las suscripciones a planes, OKLA genera ingresos de publicidad de dos formas: productos de visibilidad para vehículos específicos (pagar para que UN vehículo sea más visible) y productos de marca para el dealer como entidad (pagar para que el DEALER sea más visible). Ambos tipos tienen costo marginal casi nulo para OKLA — son cambios en el algoritmo de ranking y espacios de display que no requieren infraestructura adicional.

Producto Publicitario
Precio/día
Precio/semana
Precio/mes
Descripción
Costo OKLA
Listing Destacado (por vehículo)
$0.50
$2.50
$6.00
Badge dorado, prioridad en resultados, icono especial
~$0
Posición Top 3 búsquedas (por vehículo)
$1.50
$7.00
$20.00
Aparece entre los 3 primeros resultados para búsquedas relevantes
~$0
Oferta del Día (homepage + email)
$15.00
N/A
N/A
Un vehículo en sección 'Oferta del día'. Envío a subscribers de alertas.
~$0
Banner Homepage (máx. 3 simultáneos)
N/A
N/A
$120.00
Banner 728×90 en homepage. Máximo 3 dealers simultáneos. Rotación equitativa.
~$0
Dealer Showcase (directorio destacado)
N/A
N/A
$50.00
El dealer aparece primero en el directorio de dealers de OKLA
~$0
Pack Alertas Email (por modelo/segmento)
N/A
N/A
$35.00
Los vehículos del dealer se incluyen en alertas automáticas a compradores por modelo
$0.10
PAQUETE VISIBILIDAD TOTAL (bundle)
N/A
N/A
$175.00
Banner + Showcase + 10 destacados + Pack alertas. Ahorro vs. individual: $82
~$0.10

El modelo de publicidad es puro margen: los productos de visibilidad (Listing Destacado, Posición Top 3) son simplemente cambios de prioridad en el algoritmo de búsqueda. El banner de homepage es HTML en una posición del layout. El costo operativo es aproximadamente $0. Todo el precio es margen.

5.2 Modelo de Créditos Publicitarios (OKLA Coins)
Para flexibilizar el acceso a publicidad y crear un modelo de prepago que mejora el flujo de caja, OKLA ofrece paquetes de créditos que los dealers pueden usar en cualquier producto publicitario. Los créditos tienen bonificación por volumen para incentivar la compra anticipada:

Paquete
Créditos base
Créditos bonus
Total créditos
Valor USD
Pack Básico
2,500
—
2,500
$25.00
Pack Intermedio (+10%)
5,000
+500
5,500
$50.00
Pack Profesional (+20%)
10,000
+2,000
12,000
$100.00
Pack Dealer (+30%)
25,000
+7,500
32,500
$250.00

Tabla de precio en créditos por producto:

Producto
Créditos/día
Créditos/semana
Equivalente USD
Listing Destacado
50 cr
250 cr
$0.50/día · $2.50/semana · 1 pack básico = 50 días destacado
Posición Top 3
150 cr
750 cr
$1.50/día · 1 pack básico = 16 días Top 3
Oferta del Día
1,500 cr
—
$15/día. 1 pack intermedio = 3 Ofertas del Día
Dealer Showcase
—
—
5,000 cr/mes ($50). Pack Básico × 2 = 1 mes Showcase

6. PROYECCIONES FINANCIERAS Y PUNTO DE EQUILIBRIO
   6.1 Estructura de Costos Fijos Mensuales OKLA
   Los costos operativos de la plataforma OKLA en estado de madurez (sin incluir DealerChatAgent, que se autofinancia con sus ingresos):

Categoría de Costo Fijo
Costo/mes
% del total
Descripción
Claude API — 8 agentes base
$374.25
17%
SearchAgent, RecoAgent, SupportAgent, ListingAgent, PricingAgent, ModerationAgent, AnalyticsAgent, OrchestratorAgent
GitHub (Copilot + Actions + repo × 4 devs)
$132.00
6%
GitHub Team $16 + Copilot Business $76 + Actions overage $40
DigitalOcean (Droplet + DB + Redis + LB + Spaces)
$203.40
9%
Infraestructura cloud completa. DO Spaces para fotos + CDN.
Almacenamiento adicional por listings (variable)
$5-50
2%
Escala con número de listings activos. 10,000 listings ≈ $50/mes.
Desarrollador mantenimiento (1 part-time)
$1,000.00
45%
Mantenimiento, mejoras menores, soporte a dealers Enterprise.
Marketing y adquisición de dealers
$500.00
22%
Google Ads segmentado a dealers RD + social media OKLA.
TOTAL OPEX MENSUAL
~$2,215
100%
Costo mensual para mantener la plataforma OKLA operativa.

6.2 Proyecciones de Ingresos y Margen
Fuente de Ingresos
25 dealers
50 dealers
100 dealers
200 dealers
Suposición
Plan VISIBLE ($29) — 25% dealers
$181
$363
$725
$1,450
1 de cada 4
Plan PRO ($89) — 15% dealers
$334
$668
$1,335
$2,670
1 de cada 7
Plan ÉLITE ($199) — 5% dealers
$249
$498
$995
$1,990
1 de cada 20
Publicidad à la carte (avg $18/dealer pagador)
$225
$450
$900
$1,800
50% dealers compra al menos 1 ad/mes
Banners homepage (3 slots × $120)
$120
$240
$360
$360
Lleno con 3+ dealers
Oferta del Día ($15/día × 20 días)
$150
$300
$300
$300
Satura rápido (1 slot)
INGRESOS TOTALES / MES
$1,259
$2,519
$4,615
$8,570
Solo marketplace
(-) OPEX base OKLA/mes
-$2,210
-$2,210
-$2,210
-$2,310
Claude 8 ag.+GitHub+DO+1dev
MARGEN NETO (solo marketplace)
-$951
+$309
+$2,405
+$6,260
Break-even: ~45 dealers
(+) DealerChatAgent SaaS (si aplica)
+$0
+$1,750
+$3,487
+$12,000
75 dealers Pro/Elite con chatbot
MARGEN NETO TOTAL (marketplace + chatbot)
-$951
+$2,059
+$5,892
+$18,260
Modelo sostenible

PUNTO DE EQUILIBRIO: Con 45 dealers activos (mix de planes Libre, Visible, Pro, Élite + publicidad à la carte), OKLA cubre su OPEX mensual completo. Dado que el costo por listing es despreciable ($0.025), agregar más dealers en plan LIBRE no afecta negativamente la rentabilidad — por el contrario, aumentan la masa crítica de listings que atrae compradores y convierte a más dealers en clientes pagos.

6.3 Modelo de Ingresos a 2 Años
Proyección de ingresos totales (marketplace + DealerChatAgent) en tres escenarios de adopción:

Indicador
Conservador
Base
Optimista
Supuestos base
Dealers activos — Mes 6
50
100
200
Captación desde lanzamiento. Meta: 10 dealers/mes mínimo.
Ingresos marketplace — Mes 6
$1,259
$4,615
$9,200
Subs + publicidad à la carte
Ingresos ChatAgent — Mes 6
$0
$3,487
$7,875
25% de dealers PRO/ÉLITE activan ChatAgent
INGRESOS TOTALES — Mes 6
$1,259
$8,102
$17,075

Dealers activos — Mes 12
80
200
400
Crecimiento orgánico por listings. SEO indexado.
INGRESOS TOTALES — Mes 12
$2,800
$15,500
$32,000

INGRESOS TOTALES — Mes 24
$5,500
$28,000
$60,000
Año 2 + servicios financieros/seguros
Payback inversión de desarrollo
20 meses
7 meses
4 meses
Basado en TCO de desarrollo ~$22,713

7. FUENTES DE INGRESOS ADICIONALES (AÑO 2+)
   7.1 Tres Pilares de Ingresos a Largo Plazo
   La plataforma OKLA, una vez que tiene el tráfico y los datos, puede monetizar de tres formas adicionales que amplifican significativamente los ingresos sin aumentar costos operativos:

PILAR 1: Servicios Financieros (Lead Generation a Bancos y Financieras)
Cada buyer que consulta sobre un vehículo con precio mayor a $10,000 es un lead potencial para bancos y financieras de la República Dominicana. BanReservas, Popular, BHD y entidades de crédito automotriz pagan comisiones por leads calificados. El PricingAgent ya identifica rangos de precio y perfil del vehículo. Con integración básica, OKLA puede ofrecer al comprador un botón 'Solicitar financiamiento' que genera un lead valorado en $25-75 por conversión.
Estimado: 5% de conversación → solicitud de financiamiento. 100 dealers × 30 conversaciones/día = 3,000 conv/mes × 5% = 150 solicitudes × $25 = $3,750/mes adicional.

PILAR 2: Informes de Valoración (Buyer Paid Service)
Los compradores quieren saber si el precio de un vehículo es justo. El PricingAgent ya hace esta análisis para los dealers. Con mínimo desarrollo adicional, OKLA puede ofrecer al comprador un 'Informe de Valoración OKLA' por $5-10 que detalla si el precio está en mercado, el historial de precios del modelo, y una recomendación de oferta. Este es ingreso directo del comprador — un segmento completamente nuevo.
Estimado: 1 informe × $7 × 5% de compradores activos = con 5,000 vistas/día → 250 solicitudes × $7 = $1,750/mes adicional desde mes 8.

PILAR 3: Seguros Vehiculares (Afiliación)
Toda compra de vehículo requiere seguro. OKLA puede asociarse con aseguradoras locales (SEGUROS RESERVAS, La Colonial, etc.) y ofrecer cotización instantánea al comprador en el momento de la consulta. Comisión típica en RD: 8-12% de la prima, o $50-150 por póliza vendida. El ChatAgent puede mencionar naturalmente el seguro en el contexto de la conversación.
Estimado: Mes 12+ con 200 dealers. 20 ventas de seguros/mes × $75 promedio = $1,500/mes.

INGRESO POTENCIAL TOTAL MES 24 (escenario base): $28,000 (marketplace + ChatAgent) + $3,750 (finanzas) + $1,750 (informes) + $1,500 (seguros) = $35,000/mes de ingresos totales con OPEX de ~$3,000/mes = MARGEN NETO >$32,000/mes.

8. PLAN DE IMPLEMENTACIÓN
   Fase
   Período
   Acciones clave
   Meta dealers
   Ingresos estimados
   F1
   Meses 1-3 (Construcción)
   Desarrollar plataforma. Solo plan LIBRE. 0 cobros. Foco en conseguir listings.
   50 dealers, 500 listados
   $0 (inversión pura)
   F2
   Meses 4-6 (Lanzamiento)
   Lanzar VISIBLE y PRO. Activar productos publicitarios. 30 días gratis.
   100 dealers, 2,000 listados
   $2,500/mes al mes 6
   F3
   Meses 7-12 (Escala)
   Lanzar ÉLITE + DealerChatAgent. Activar WhatsApp. Expandir a Santiago.
   200 dealers, 8,000 listados
   $6,000-10,000/mes
   F4
   Año 2 (Expansión)
   Servicios financieros (leads a bancos). Seguros. Data business. Nacional.
   400+ dealers, 25,000+ listados
   $20,000+/mes

8.1 Tácticas de Conversión de Dealers
FASE 1 (primeros 60 días): Registrar los 20 principales dealers de Santo Domingo en plan LIBRE. El objetivo es tener 3,000+ vehículos en la plataforma antes del lanzamiento público. Listings > tráfico > compradores > valor para dealers.
FASE 1 (mes 2-3): Activar la herramienta de importación desde Facebook Marketplace. El ListingAgent procesa las URLs de Facebook del dealer y crea los listings en OKLA automáticamente. Fricción de migración = 0.
FASE 2 (mes 4): Enviar a cada dealer un reporte de 'Primeros 30 días en OKLA' con sus métricas reales (vistas, leads, consultas). Incluir una oferta de activar plan VISIBLE gratis por 30 días adicionales.
FASE 2 (mes 5-6): Lanzar campaña 'OKLA vs. Facebook' con datos reales de conversión de los primeros dealers. Mostrar cuántas consultas generó OKLA vs. cuántas generó su publicación de Facebook.
FASE 3 (mes 7+): Lanzar DealerChatAgent como upsell natural para dealers en plan PRO y ÉLITE. El pitch: 'Recibes 300 consultas al mes en OKLA. ¿Cuántas respondes a tiempo? El ChatAgent responde todas, 24/7, y agenda el test drive.'

9. RESUMEN FINANCIERO CONSOLIDADO
   Indicador Financiero
   Valor
   Referencia
   Costo OKLA por vehículo publicado
   ~$0.025 USD
   Sección 2.1
   Dealer con 100 vehículos publicados — costo a OKLA
   $2.50/mes
   Sección 2.1
   OPEX mensual plataforma OKLA (sin ChatAgent)
   ~$2,215 USD
   Sección 6.1
   Break-even del marketplace (dealers)
   45 dealers activos
   Sección 6.2
   Ingresos marketplace a 100 dealers (mes 6)
   $4,615/mes
   Sección 6.2
   Ingresos totales a 100 dealers (marketplace + ChatAgent)
   $8,102/mes
   Sección 6.2
   Margen neto a 100 dealers
   $5,892/mes
   Sección 6.2
   Payback del proyecto (escenario base)
   7 meses
   Sección 6.2
   Ventaja vs. SuperCarros/Yacarros (misma propuesta)
   ILIMITADO GRATIS vs. PAGO con LÍMITES
   Sección 3.1
   Ventaja vs. Facebook Marketplace
   Especialización + IA + SEO + ChatAgent 24/7
   Sección 3.2
   Ingresos proyectados Mes 24 (base + servicios fin.)
   > $35,000/mes
   > Sección 7.1

CONCLUSIÓN ESTRATÉGICA: OKLA debe entrar al mercado con publicaciones ilimitadas y gratuitas como arma de adquisición masiva. Este modelo destruye la propuesta de valor de SuperCarros, Yacarros y SuCarroRD (todos cobran y tienen límites), iguala a Facebook Marketplace en precio y lo supera ampliamente en valor. La monetización viene de los dealers que ya confían en la plataforma y quieren más visibilidad — ese es el momento exacto correcto para vender publicidad y herramientas.

Análisis de Negocio y Económico OKLA Marketplace v3.0 | Febrero 2026
PMI PMBOK 7th Ed. · IEEE Std 1058-1998 · ISO 31000:2018 · BABOK v3 | Modelo Freemium + Publicidad + DealerChatAgent SaaS
"
