Impelementame ahora esto "
OKLA
MARKETPLACE

━━━━━━━━━━━━━━━━━━━━━━━━━

ESTUDIO TÉCNICO CIENTÍFICO
OKLA SCORE™ & ALGORITMO DE PRECIO PROMEDIO DE MERCADO
Metodología para la Valoración Objetiva de Vehículos Usados
en la República Dominicana

Versión 1.0 | República Dominicana | 2025
Confidencial – Uso Interno OKLA Marketplace

1. Propósito del Estudio: El Problema Real en RD
   El mercado de vehículos usados en la República Dominicana enfrenta una asimetría de información grave y sistemática. El comprador promedio dominicano carece de herramientas objetivas para evaluar si el precio que le ofrecen es justo o si el vehículo tiene un historial problemático. Esto genera un ecosistema donde el fraude, la manipulación de odómetros y los vehículos con daño estructural oculto son moneda común.

¿Por qué el comprador dominicano es tan vulnerable?
La mayoría de los vehículos usados en RD provienen de subastas en Estados Unidos (COPART, IAAI, Manheim). Muchos llegan con títulos 'lavados' (title washing), con daños de inundación no declarados, con odómetros manipulados o reconstruidos tras accidentes graves. El dealer conoce toda esta información; el comprador, nada. OKLA Score nació para equilibrar esa balanza.

1.1 Casos Comunes de Engaño en el Mercado Dominicano
Vehículos de subastas COPART/IAAI con título 'Salvage' o 'Rebuilt' que se venden como 'impecables'
Odómetros manipulados (rollback) – un vehículo de 180,000 millas vendido como si tuviera 80,000
Daño por inundación (flood damage) – corrosión interna que aparece meses después de la compra
Marco doblado (frame damage) – daño estructural que compromete la seguridad y se oculta con pintura
Vehículos robados o con número de VIN clonado
Precios inflados 20–40% por encima del mercado real de EE.UU. sin justificación
Kilometraje reportado en millas vendido como si fuera kilómetros (5.8x de diferencia)

El OKLA Score™ es la respuesta técnica y científica a estos problemas. Es un índice compuesto que combina datos verificables de múltiples fuentes para entregar al comprador dominicano un número entre 0 y 1,000 que resume la confiabilidad, el estado y la relación precio–valor del vehículo.

2. Arquitectura del OKLA Score™
   El OKLA Score es un índice ponderado que agrega múltiples dimensiones del vehículo en una sola calificación. La metodología sigue principios de scoring financiero (similar al Credit Score FICO) y modelos de valoración de activos usados ampliamente utilizados en la industria automotriz norteamericana.

2.1 Escala del OKLA Score
RANGO
NIVEL
SEMÁFORO
RECOMENDACIÓN
DESCRIPCIÓN
850 – 1,000
EXCELENTE
🟢 VERDE
COMPRAR
Vehículo en condición óptima, precio justo, historial limpio. Altamente recomendado.
700 – 849
BUENO
🔵 AZUL
COMPRAR
Buen estado general. Puede tener detalles menores. Precio aceptable.
550 – 699
REGULAR
🟡 AMARILLO
CON CAUTELA
Negociar precio. Solicitar inspección mecánica independiente antes de comprar.
400 – 549
DEFICIENTE
🔴 ROJO
PRECAUCIÓN
Historial problemático o precio muy inflado. Requiere inspección experta.
0 – 399
CRÍTICO
⚫ NEGRO
NO COMPRAR
Vehículo con daño estructural, fraude detectado o precio fraudulento. Evitar.

2.2 Las 7 Dimensiones del OKLA Score™
El OKLA Score se calcula a partir de 7 dimensiones principales, cada una con su propio sub-score y ponderación. El score final es la suma ponderada de todos los sub-scores, escalada al rango 0–1,000.

#

DIMENSIÓN
PESO (%)
PTS MÁX.
FUENTE DE DATOS PRIMARIA
D1
Historial en EE.UU. (VIN History)
25%
250
CARFAX / VinAudit / NMVTIS / NHTSA
D2
Condición Mecánica y Técnica
20%
200
NHTSA Specs API / Edmunds / VehicleDatabases
D3
Kilometraje / Odómetro
18%
180
CARFAX / AutoCheck / NMVTIS
D4
Precio vs. Mercado RD
17%
170
Scraping: Corotos, SuperCarros, Yacarros, MarketCheck
D5
Seguridad y Recalls NHTSA
10%
100
NHTSA API (gratuita)
D6
Depreciación y Año Modelo
6%
60
KBB / Edmunds / MarketCheck
D7
Reputación del Vendedor / Dealer
4%
40
Base interna OKLA / Reseñas

3. Cálculo Detallado por Dimensión
   D1 – Historial en EE.UU. (VIN History) | Peso: 25% | Máx. 250 pts
   Esta es la dimensión más crítica. La gran mayoría de vehículos usados en RD vienen de subastas de EE.UU. (COPART, IAAI, Manheim). El historial del VIN en EE.UU. revela la verdadera historia del vehículo antes de llegar al país.

FACTOR
PUNTOS
LÓGICA DE PENALIZACIÓN
Título Limpio (Clean Title)
+100
Base: sin historial de daño total
Título Salvage/Totaled
-200
El vehículo fue declarado pérdida total por aseguradora
Título Rebuilt/Reconstructed
-120
Reparado tras pérdida total – riesgo estructural latente
Daño por Inundación (Flood)
-180
Corrosión interna severa, fallo eléctrico a largo plazo
Daño de Granizo (Hail)
-60
Mayormente cosmético, pero depreciación significativa
Historial de Accidentes (Leve)
-30
1–2 accidentes menores, sin daño estructural
Historial de Accidentes (Moderado)
-70
Daño reportado a carrocería/suspensión
Daño Estructural (Frame Damage)
-150
Compromete la integridad y seguridad total del vehículo
Vehículo de Alquiler / Flota
-40
Uso intensivo, mantenimiento variable
Lemon Law Buyback
-180
El fabricante recompró el vehículo por defectos crónicos
VIN Clonado / Fraude de Título
-250 (bloqueo)
Bloqueo total: el vehículo no puede ser listado en OKLA
Sin Accidentes Reportados
+50
Bonus por historial limpio documentado
Mantenimiento Verificado en Dealer
+30
Historial de servicio en talleres autorizados
Un Solo Propietario
+20
Indica cuidado más consistente del vehículo

API Principal para D1
VinAudit.com – Market Value API + History API (desde $0.25/reporte). Alternativa: FAXVIN.com o integración directa con NMVTIS (National Motor Vehicle Title Information System) del Departamento de Justicia de EE.UU. NMVTIS proveedores autorizados incluyen: VINCheck.info, VehicleHistory.gov.

D2 – Condición Mecánica y Técnica | Peso: 20% | Máx. 200 pts
Evalúa las especificaciones del vehículo contra su categoría de peso en el mercado. Un vehículo bien equipado de fábrica tiene mayor valor intrínseco.
Motor: cilindrada, tipo (turbo, híbrido, eléctrico) → hasta +60 pts
Transmisión: automática CVT/tiptronic vs manual → diferencia de hasta 30 pts
Tracción: AWD/4WD suma +25 pts vs FWD estándar
Tecnología de seguridad activa: Lane Assist, AEB, Blind Spot → +20 pts
Estado de recalls pendientes sin corregir → -15 pts por recall activo
Número de quejas en NHTSA (complaints) → -5 pts por cada 10 quejas del modelo

APIs para D2

1. NHTSA vPIC API: vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/{VIN}?format=json (GRATUITA)2. NHTSA Safety Ratings API: api.nhtsa.gov/SafetyRatings/modelyear/{año}/make/{marca}/model/{modelo} (GRATUITA)3. Edmunds API: edmunds.com/api/ – especificaciones, reviews y pricing (requiere registro)

D3 – Kilometraje / Odómetro | Peso: 18% | Máx. 180 pts
El odómetro es el dato más manipulado en el mercado dominicano. Un vehículo importado de EE.UU. puede tener sus millas registradas y verificables. El algoritmo OKLA cruza el kilometraje declarado por el vendedor con los registros históricos del VIN.

SCORE KM
LECTURA (MILLAS)
LECTURA (KM EQUIV.)
PUNTOS ASIGNADOS
Óptimo
0 – 30,000
0 – 48,280 km
+180 pts (máximo)
Bajo
30,001 – 60,000
48,281 – 96,560 km
+140 pts
Moderado
60,001 – 90,000
96,561 – 144,840 km
+100 pts
Alto
90,001 – 120,000
144,841 – 193,121 km
+60 pts
Muy Alto
120,001 – 150,000
193,122 – 241,401 km
+30 pts
Extremo

> 150,000
> 241,401 km
> +10 pts
> FRAUDE DETECTADO
> Discrepancia > 20%
> vs. registros históricos
> -180 pts (penalización máxima)

Nota Crítica – Conversión Millas/Kilómetros
La diferencia entre millas y kilómetros es de 1:1.60934. Un vehículo con 100,000 MILLAS tiene 160,934 KM reales. Muchos dealers en RD reportan el odómetro en millas como si fuera kilómetros, haciendo parecer que el carro 'tiene menos'. OKLA siempre convierte y valida la unidad correcta.

D4 – Precio vs. Mercado RD | Peso: 17% | Máx. 170 pts
Esta dimensión compara el precio de venta del vehículo con el precio promedio calculado por el Algoritmo de Precio OKLA (detallado en la Sección 4). Es la dimensión que directamente protege el bolsillo del comprador.

DIFERENCIA PRECIO vs. MERCADO
PUNTOS
INTERPRETACIÓN
Precio ≤ -15% del mercado
+170
Oferta excelente – precio muy por debajo del mercado
Precio -15% a -5% del mercado
+140
Buen precio – ligeramente por debajo del promedio
Precio ±5% del mercado
+110
Precio justo – dentro del rango normal de mercado
Precio +5% a +15% sobre mercado
+60
Caro – negociar o buscar alternativas
Precio +15% a +30% sobre mercado
+20
Muy caro – poca justificación de valor
Precio > +30% sobre mercado
0
Precio abusivo – alto riesgo de fraude o engaño

D5 – Seguridad y Recalls NHTSA | Peso: 10% | Máx. 100 pts
Sin recalls activos pendientes: +100 pts
Recalls activos resueltos por el dealer: +80 pts
1 recall activo sin resolver: +60 pts
2–3 recalls activos sin resolver: +30 pts
4+ recalls activos sin resolver: 0 pts
Rating de seguridad NHTSA 5 estrellas: +20 pts adicionales (bonus)
Rating de seguridad NHTSA 4 estrellas: +10 pts adicionales
Rating de seguridad NHTSA 3 o menos estrellas: 0 pts adicionales

API Gratuita NHTSA
Recalls: api.nhtsa.gov/recalls/recallsByVehicle?make={MAKE}&model={MODEL}&modelYear={AÑO}Seguridad: api.nhtsa.gov/SafetyRatings/Quejas: api.nhtsa.gov/complaints/complaintsByVehicle?make={MAKE}&model={MODEL}&modelYear={AÑO}Completamente gratuita, sin API Key requerida.

D6 – Depreciación y Año Modelo | Peso: 6% | Máx. 60 pts
La depreciación es un factor matemático previsible. Se aplica una curva de depreciación calibrada para el mercado dominicano, donde la depreciación es más lenta que en EE.UU. por la menor oferta local.
Año actual (modelo nuevo): +60 pts
1–2 años de antigüedad: +50 pts
3–4 años de antigüedad: +40 pts
5–6 años de antigüedad: +30 pts
7–9 años de antigüedad: +20 pts
10–12 años de antigüedad: +12 pts
13+ años de antigüedad: +5 pts

D7 – Reputación del Vendedor / Dealer | Peso: 4% | Máx. 40 pts
Dealer certificado OKLA Verified + historial positivo: +40 pts
Dealer registrado con historial neutro: +25 pts
Vendedor privado verificado: +20 pts
Dealer con 1–2 disputas resueltas: +10 pts
Dealer con disputas activas o no resueltas: 0 pts
Dealer bloqueado / fraude comprobado: Score bloqueado automáticamente

4. Algoritmo de Precio Promedio de Mercado OKLA
   El precio promedio de mercado es la columna vertebral de la dimensión D4 del OKLA Score. Se calcula mediante un modelo de regresión ponderada que combina datos de múltiples fuentes para producir el "Precio Justo OKLA" en pesos dominicanos (RD$).

4.1 Fórmula Maestra del Precio Justo OKLA
Precio Justo OKLA (USD) =
(P_USA × 0.35) + (P_RD_avg × 0.45) + (P_KBB × 0.20)
× Factor_Ajuste_RD × Factor_Depreciacion × Factor_Condicion

Variables de la Fórmula:
VARIABLE
FUENTE
DESCRIPCIÓN
P_USA
MarketCheck / KBB / Edmunds
Precio promedio del mismo modelo en subastas/mercado USA (USD)
P_RD_avg
Corotos / SuperCarros / Yacarros / Montao / CarrosRD
Precio promedio de listados activos en RD para el mismo año/modelo
P_KBB
Kelley Blue Book IDWS API
Valor de libro azul para ese modelo, año y condición estimada
Factor_Ajuste_RD
Fórmula interna OKLA
Corrección por costo de importación RD (arancel ~20% + ITBIS 18% + Primeras Placas ~3%)
Factor_Depreciacion
Curva actuarial OKLA
Penalización progresiva por antigüedad del modelo
Factor_Condicion
Score D1+D2+D3
Ajuste por daños, kilometraje y mecánica (rango: 0.60–1.15)

4.2 Factor de Ajuste al Mercado Dominicano
Este factor es único para RD y refleja los costos adicionales de tener un vehículo en el país versus comprarlo directamente en EE.UU.:
COMPONENTE
TASA ESTIMADA
APLICABLE A
Arancel de importación (vehículos usados)
20%
Valor CIF del vehículo
ITBIS (IVA dominicano)
18%
Valor + arancel
Primera placa (derecho de circulación)
3%
Valor del vehículo
Flete marítimo (EE.UU. → RD)
$800–$1,500 USD fijo
Costo directo
Seguro de tránsito + despacho
1–2%
Valor del vehículo
Margen del dealer / importador
10–25%
Negociable (calculado implícitamente)

Factor_Ajuste_RD Simplificado
Para el cálculo automatizado, OKLA aplica un Factor_Ajuste_RD de 1.45 a 1.65 (dependiendo del año y cilindrada del vehículo) como multiplicador del precio USA para estimar el 'precio mínimo razonable de entrada al mercado RD'. Cualquier precio significativamente por encima de este rango calculado activa una alerta de precio inflado.

4.3 Estrategia de Web Scraping de Clasificados RD
Para calcular P_RD_avg con precisión, OKLA implementa un módulo de scraping/crawling que rastrea en tiempo real los siguientes portales dominicanos:
PORTAL
URL
PESO EN PROMEDIO
VENTAJA
Corotos
corotos.com.do
30%
Plataforma líder en RD, mayor volumen, ya calcula 'Buen Precio'
SuperCarros
supercarros.com
25%
Portal especializado, +15,000 anuncios, alto tráfico automotriz
Yacarros
yacarros.com
20%
Portal dedicado exclusivamente a vehículos, datos estructurados
Montao.do
montao.do
12%
Enfoque en dealers formales de SD y Santiago
CarrosRD
carrosrd.com
8%
Asociaciones de dealers (ACOFAVE, ASOCIVU, ANADIVE, ADECI)
Encuentra24
encuentra24.com/dominicana
5%
Clasificados regionales con cobertura adicional

5. Directorio Completo de APIs y Portales
   A continuación se presenta el catálogo completo y priorizado de todas las fuentes de datos recomendadas para implementar el OKLA Score con máxima precisión.

5.1 APIs de Historial de Vehículos (VIN History)
PROVEEDOR
ENDPOINT / URL
COSTO
PRIORIDAD
DATOS CLAVE
VinAudit
vinaudit.com/api
~$0.25/reporte
⭐⭐⭐⭐⭐
NMVTIS, titulos, odómetro, accidentes
CARFAX (Business)
carfax.com/business
Contrato B2B
⭐⭐⭐⭐⭐
Historia completa, mayor cobertura
FaxVin
faxvin.com
$4.99–$9.99/rep.
⭐⭐⭐⭐
NMVTIS, subastas, titulos
Vini.az API
vini.az/en/api
Balances prepago
⭐⭐⭐⭐
CARFAX+AutoCheck+Copart en 1 API
NMVTIS (Gov.)
vehiclehistory.gov
Gratuita (web)
⭐⭐⭐
Fuente oficial EE.UU., solo básico
AutoCheck (Experian)
autocheck.com/b2b
Contrato empresarial
⭐⭐⭐⭐
Alternativa a CARFAX, score propio

5.2 APIs de Valoración y Precio de Mercado USA
PROVEEDOR
ENDPOINT / URL
COSTO
PRIORIDAD
DATOS CLAVE
MarketCheck
marketcheck.com/apis
$99–$499/mes
⭐⭐⭐⭐⭐
Listados activos, precio promedio USA, VIN spec
Kelley Blue Book (B2B)
b2b.kbb.com/idws
Contrato B2B
⭐⭐⭐⭐⭐
Valor de libro, trade-in, retail, private party
Edmunds API
edmunds.com/api
Registro requerido
⭐⭐⭐⭐
MSRP, TMV, especificaciones, pricing
VehicleDatabases
vehicledatabases.com/api
Desde $0.02/query
⭐⭐⭐⭐
Market value, VIN decode, specs, alternativa KBB
BlackBook (S&P Global)
blackbookus.com/api
Contrato empresarial
⭐⭐⭐
Valoración para dealers y financieras
VinAudit Market Value
vinaudit.com/api/market
Incluido en paquete
⭐⭐⭐⭐
Market value por VIN, rápido y económico

5.3 APIs Gubernamentales Gratuitas (EE.UU.)
API
ENDPOINT EJEMPLO
DATOS DISPONIBLES
COSTO
NHTSA vPIC – VIN Decode
vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/{VIN}?format=json
Make, Model, Año, Motor, Planta
GRATIS
NHTSA Safety Ratings
api.nhtsa.gov/SafetyRatings/modelyear/{año}/make/{marca}/model/{modelo}
Calificación estrellas NCAP
GRATIS
NHTSA Recalls
api.nhtsa.gov/recalls/recallsByVehicle?make={M}&model={M}&modelYear={Y}
Lista de recalls activos
GRATIS
NHTSA Complaints
api.nhtsa.gov/complaints/complaintsByVehicle?make={M}&model={M}&modelYear={Y}
Quejas de consumidores
GRATIS

5.4 Portales de Subastas USA (Para verificar precio de origen)
PLATAFORMA
URL
USO EN OKLA
COPART
copart.com
Verificar precio de subasta de origen, tipo de título (Salvage/Clean), fotos de daño
IAAI (IAA)
iaai.com
Similar a COPART, segunda subasta más grande de EE.UU.
Manheim
manheim.com
Subastas dealer-to-dealer (requiere licencia de dealer)
ADESA
adesa.com
Subastas mayoristas de vehículos usados

Insight Estratégico sobre Subastas
Vini.az API ofrece acceso programático a los reportes de COPART a través de su API consolidada. Esto permite que OKLA obtenga automáticamente las fotos de daño, el precio de venta en subasta y el tipo de título del vehículo directamente desde su VIN, lo cual es fundamental para detectar vehículos reconstruidos que se venden como limpios en el mercado dominicano.

6. Implementación Técnica del Motor de Cálculo
   6.1 Flujo de Cálculo (Pipeline)
   PASO
   MÓDULO
   DESCRIPCIÓN
   1
   VIN Input & Decode
   El usuario ingresa el VIN de 17 dígitos. OKLA llama a NHTSA vPIC para decodificar: make, model, año, motor, país de fabricación.
   2
   History Fetch
   Se consulta VinAudit (o CARFAX) para obtener: título, propietarios, accidentes, odómetro histórico, tipo de uso (rental, fleet, personal).
   3
   Odometer Cross-Check
   Se compara el km/millas declarado por el vendedor con los registros del VIN. Si hay discrepancia >20%, se activa alerta de fraude.
   4
   Safety & Recalls
   NHTSA API consulta recalls activos y rating de seguridad NCAP para el modelo/año específico.
   5
   Market Price USA
   MarketCheck o VinAudit API retorna el precio promedio del mismo modelo en el mercado estadounidense actual.
   6
   Price Scraping RD
   El módulo de scraping extrae precios de listados activos en Corotos, SuperCarros, Yacarros y calcula P_RD_avg.
   7
   Precio Justo OKLA
   Se aplica la fórmula maestra (Sección 4.1) para calcular el Precio Justo OKLA en USD y se convierte a RD$ usando el tipo de cambio del Banco Central (BCRD API).
   8
   Sub-Score Calculation
   Se calculan los 7 sub-scores (D1–D7) con sus penalizaciones y bonuses.
   9
   OKLA Score Final
   Se suman los sub-scores ponderados para producir el OKLA Score en escala 0–1,000.
   10
   Reporte & Consejos
   Se genera el reporte OKLA con score, precio justo, alertas y consejos personalizados para el comprador.

6.2 Tipo de Cambio Dinámico – API del BCRD
Para convertir el Precio Justo OKLA de USD a RD$, el sistema consume la tasa de cambio en tiempo real publicada por el Banco Central de la República Dominicana:

# API Banco Central RD (BCRD)

GET https://api.bcrd.gov.do/estadisticas/tipo-cambio

# Alternativa con ExchangeRate-API (si BCRD no tiene endpoint público):

GET https://v6.exchangerate-api.com/v6/{API_KEY}/latest/USD

7. Guía de Interpretación para el Comprador Dominicano
   Esta sección define exactamente qué significa cada nivel del OKLA Score para el comprador y qué acciones debe tomar. Este es el corazón del valor que OKLA entrega al mercado dominicano.

🟢 OKLA Score 850–1,000: EXCELENTE – Vehículo Recomendado
¿Qué significa para ti?
Este vehículo pasó todas las pruebas de OKLA con excelencia. Tiene historial limpio en EE.UU., precio justo o por debajo del mercado, kilometraje coherente con su historial, sin recalls graves pendientes y el vendedor tiene buena reputación.
Consejos:
✅ Puedes proceder con confianza. Haz la inspección mecánica de rutina igual.✅ Verifica que la documentación local (matrícula, placa) esté en regla.✅ Solicita la garantía del dealer si aplica.✅ Considera que si el precio está muy por debajo del mercado con score alto, puede ser una oportunidad excepcional.

🔵 OKLA Score 700–849: BUENO – Compra Segura con Revisión Básica
¿Qué significa para ti?
El vehículo está en buenas condiciones generales. Puede tener algún accidente menor documentado, kilometraje moderado o un precio ligeramente por encima del óptimo, pero nada que descarte la compra.
Consejos:
🔵 Haz una inspección mecánica completa con un mecánico de confianza.🔵 Si el precio es 5–10% sobre el mercado OKLA, intenta negociar.🔵 Verifica los recalls pendientes y asegúrate de que el dealer los resuelva antes de la entrega.🔵 Solicita el reporte OKLA completo para ver qué factores bajaron el score.

🟡 OKLA Score 550–699: REGULAR – Negocia y Verifica
¿Qué significa para ti?
El vehículo tiene factores que deben preocuparte: precio inflado, historial de accidentes moderado, kilometraje alto, o recalls sin resolver. NO es una compra automática. Necesita evaluación adicional.
Consejos:
🟡 Usa el Precio Justo OKLA como base de negociación – no pagues más.🟡 Exige inspección por mecánico certificado ANTES de firmar cualquier documento.🟡 Solicita al dealer resolución de todos los recalls activos.🟡 Investiga qué sub-score está más bajo (D1–D7) para entender el riesgo específico.🟡 Si el dealer se niega a negociar el precio en un score 550–699, desconfía.

🔴 OKLA Score 400–549: DEFICIENTE – Precaución Extrema
¿Qué significa para ti?
Hay señales serias de alerta: posible historial de daño grave, precio muy por encima del mercado, odómetro sospechoso o título de origen cuestionable. Este vehículo puede representar una pérdida financiera significativa.
Consejos:
🔴 No compres sin una inspección especializada completa (endoscopio, escáner OBD2, análisis de pintura).🔴 Solicita al dealer el reporte CARFAX original del vehículo.🔴 Verifica el historial de subasta en COPART o IAAI directamente con el VIN.🔴 Si el dealer no puede justificar el precio alto con documentación, abandona la negociación.🔴 Consulta con un abogado si el dealer presiona para un cierre rápido – es señal de fraude.

⚫ OKLA Score 0–399: CRÍTICO – No Comprar
¿Qué significa para ti?
OKLA detectó indicadores críticos: daño estructural, inundación severa, VIN potencialmente clonado, fraude de odómetro confirmado o precio completamente fuera de rango. Este vehículo representa un riesgo financiero y de seguridad inaceptable.
Consejos:
⛔ NO COMPRES ESTE VEHÍCULO bajo ninguna circunstancia.⛔ Si ya pagaste algún depósito, contacta inmediatamente al PROCI (Protección al Consumidor) o a la Policía Nacional si hay fraude.⛔ Reporta el anuncio en OKLA Marketplace para proteger a otros compradores.⛔ Nunca firmes documentos de transferencia bajo presión.

8. Impacto Social del OKLA Score en la República Dominicana
   El OKLA Score no es solo una herramienta técnica. Es un instrumento de equidad económica para el comprador dominicano, que históricamente ha carecido de acceso a la información que sí poseen los dealers y vendedores.

PROBLEMA ACTUAL EN RD
SOLUCIÓN OKLA
Comprador no sabe si el precio es justo
Precio Justo OKLA calculado de 6 fuentes verificables
No existe forma de verificar el historial real
Consulta automática a CARFAX/VinAudit/NMVTIS via API
Odómetro manipulado es indetectable visualmente
Cruce de registros históricos del VIN en tiempo real
Título 'lavado' (salvage→clean) no se detecta
D1 detecta discrepancias de título en la cadena de registros
Comprador compra vehículo inundado como bueno
Flag automático de 'Flood Damage' desde el historial VIN
Recalls sin resolver son desconocidos por comprador
NHTSA API verifica recalls activos automáticamente
Dealer tiene información privilegiada de subasta
OKLA recupera datos de COPART/IAAI por VIN
No hay cultura de verificación previa
UI simple: un número (0–1,000) lo resume todo para el usuario

Proyección de Impacto
Con solo un 10% de adopción en el mercado de vehículos usados de RD (estimado en +200,000 transacciones/año), OKLA podría prevenir fraudes y malas compras por valor de más de RD$500 millones anuales. El OKLA Score es para el comprador dominicano lo que CARFAX es para el estadounidense: su derecho a conocer la verdad antes de comprar.

9. Hoja de Ruta de Implementación del Motor OKLA Score
   Fase 1 – MVP (Meses 1–3): Score Básico
   Integrar NHTSA vPIC API para VIN decode (gratuita)
   Integrar VinAudit API para historial básico (~$0.25/reporte)
   Implementar scraping de Corotos y SuperCarros para P_RD_avg
   Calcular D1, D3, D4, D5 con fórmulas simplificadas
   Lanzar OKLA Score v1.0 con semáforo básico (verde/amarillo/rojo)

Fase 2 – Score Completo (Meses 4–6)
Integrar MarketCheck API para precio USA real-time
Agregar scraping completo de Yacarros, Montao, CarrosRD, Encuentra24
Implementar tipo de cambio dinámico (API BCRD o ExchangeRate-API)
Calcular las 7 dimensiones completas D1–D7
Sistema de reputación de dealers (D7)

Fase 3 – Score Premium y ML (Meses 7–12)
Integrar CARFAX Business API o Vini.az para datos premium
Modelo de Machine Learning para predecir precio de mercado RD en 90 días
OKLA Price Guard: alerta automática si el precio sube más del 5% en 7 días
Historial de OKLA Score: tracking del score de un vehículo en el tiempo
Certificación OKLA Verified para dealers con historial limpio

10. Conclusión
    El OKLA Score™ representa el estándar más avanzado y científicamente fundamentado para la evaluación de vehículos usados en la República Dominicana. Su metodología multidimensional, basada en datos verificables de fuentes de primer nivel como NHTSA, CARFAX, MarketCheck y los principales clasificados del país, lo posiciona como la herramienta definitiva para proteger al comprador dominicano.

La combinación del Historial VIN en EE.UU. (25%), la evaluación de Precio vs. Mercado (17%) y la verificación de Odómetro (18%) cubre las tres formas de fraude más comunes en el mercado local. Mientras que las dimensiones de Seguridad NHTSA y Depreciación añaden la capa técnica que convierte a OKLA en una herramienta de confianza institucional, no solo de comparación de precios.

OKLA no compite con los dealers honestos: los certifica y les da un sello de confianza. OKLA sí pone en evidencia a quienes engañan, y protege a los compradores que hoy no tienen con quién contar al momento de tomar la decisión más grande de su economía familiar.

"El OKLA Score es la verdad que el mercado dominicano siempre necesitó."
OKLA Marketplace | oklamarketplace.do

" pero por etapa, "
OKLA
MARKETPLACE

──────────────────────────────

ESTRATEGIA DE INTRODUCCIÓN
DEL OKLA SCORE™ Y ALGORITMO DE PRECIO
Cómo Ganar a los Dealers sin Perder al Comprador

Go-to-Market Strategy | República Dominicana | 2025
Documento Estratégico Confidencial – OKLA Marketplace

1. El Dilema Central del Marketplace de Dos Lados
   Todo marketplace enfrenta el mismo problema al nacer: no puedes tener compradores sin inventario, y no puedes tener inventario sin compradores. Para OKLA, este dilema tiene una capa adicional de complejidad: la herramienta que más valor le da al comprador (el OKLA Score) es potencialmente la que más puede alejar al dealer.

La Tensión Fundamental
El dealer sabe que su vehículo podría recibir un score bajo si tiene historial de daño, precio inflado u odómetro sospechoso. Su reacción instintiva será: "Si ese Score me perjudica, no listo aquí." Sin embargo, si el Score no existe desde el inicio, OKLA pierde su razón de ser diferenciadora en el mercado.

La solución no es eliminar el Score ni ocultarlo permanentemente. La solución es una estrategia de introducción en fases que primero construye dependencia del dealer hacia la plataforma, y luego introduce el Score cuando el costo de salir ya es mayor que el costo de quedarse.

Principio Estratégico Maestro
"Primero sé indispensable para el dealer. Después sé transparente para el comprador."

El dealer debe percibir al OKLA Score no como una amenaza, sino como un sello de calidad que lo diferencia de la competencia informal. Ese cambio de percepción es el objetivo central de esta estrategia.

2. Mapa Estratégico de las 4 Fases
   La estrategia se divide en cuatro fases progresivas. Cada fase tiene un objetivo distinto con el dealer, y el OKLA Score se introduce gradualmente como una ventaja para ellos, no como una auditoría en su contra.

FASE
PERÍODO
NOMBRE
OBJETIVO CON EL DEALER
OKLA SCORE
1
Meses 1–4
TIERRA FÉRTIL
Ser el portal más fácil y barato para listar
⛔ Invisible (no existe aún)
2
Meses 5–9
EL ESPEJO
Dar al dealer inteligencia de mercado que no tiene
🔒 Privado (solo el dealer lo ve)
3
Meses 10–15
EL SELLO
Crear aspiración: los buenos quieren el badge
🏅 Opcional (OKLA Verified voluntario)
4
Mes 16+
EL ESTÁNDAR
OKLA es el mercado; salir tiene un costo alto
✅ Público en todos los listados

FASE 1
TIERRA FÉRTIL – Ser el Portal Más Valioso
Meses 1–4

Objetivo Principal
En esta fase OKLA no existe como herramienta de evaluación de vehículos. OKLA existe como el portal más poderoso, fácil y económico para que los dealers vendan. El foco es 100% en el dealer. El comprador llega como consecuencia del inventario.

Mensaje Central al Dealer en Fase 1
"OKLA te pone donde están los compradores. Sin comisiones, sin complicaciones.
Solo sube tus carros y empieza a vender más rápido que con cualquier otro portal."

Lo que OKLA ofrece al dealer en esta fase:
Listados gratuitos o muy baratos (0–RD$500/mes): La barrera de entrada debe ser cero. El dealer no arriesga nada.
Panel de control simple: Subir fotos, precio, descripción y specs en menos de 5 minutos.
Distribución multicnal incluida: OKLA distribuye el listado automáticamente a WhatsApp Business, Facebook Marketplace y Google. El dealer publica una vez, aparece en todas partes.
Lead directo sin intermediarios: El comprador contacta directamente al dealer. OKLA no es el cuello de botella.
Estadísticas básicas de vistas: "Tu Toyota Camry 2019 tuvo 248 vistas esta semana" — esto crea valor percibido inmediato.
Soporte humano en RD: WhatsApp directo con un asesor OKLA. Esto diferencia de portales internacionales fríos.

Lo que OKLA NO hace en esta fase:
No muestra ningún score, calificación ni evaluación visible al comprador
No compara precios públicamente
No menciona historial del VIN en el front-end
No emite ningún juicio sobre la calidad del inventario del dealer

Lo que OKLA SÍ hace internamente (sin decírselo al dealer todavía):
Recopila el VIN de todos los vehículos listados
Empieza a construir silenciosamente la base de datos de precios del mercado RD
Rastrea el historial de cada dealer: cuántos carros vende, cuántos reclamos recibe
Prepara el motor del OKLA Score pero NO lo activa públicamente

KPI Clave de Éxito – Fase 1
• Meta: 150–300 dealers activos con al menos 5 listados cada uno
• Meta: 2,000+ vehículos en inventario activo
• Meta: 15,000+ usuarios registrados (compradores)
• Meta: NPS del dealer ≥ 60 ("Recomendaría OKLA a otro dealer")

Cómo Reclutar los Primeros Dealers (Táctica de Arranque)
Táctica 1 – Los 10 Dealers Ancla
Identifica los 10 dealers más respetados en RD (uno por zona: Santo Domingo Este, Oeste, Norte, Santiago, etc.) y ofrecerles un programa "Fundador OKLA": gratis por 12 meses, badge especial, y prioridad en búsquedas. Su presencia da credibilidad al portal y hace que los dealers menores quieran estar también.
Táctica 2 – La Red de WhatsApp de Dealers
Los dealers en RD operan mayoritariamente por WhatsApp. Crea un grupo privado de "Dealers OKLA" donde compartes inteligencia de mercado: qué modelos están buscando más los compradores, cuáles se están vendiendo más rápido. Esto convierte a OKLA en una fuente de inteligencia que los dealers no quieren perder.
Táctica 3 – Asociaciones Gremiales
Aliarte formalmente con ACOFAVE, ASOCIVU, ANADIVE y ADECI — las mismas asociaciones que ya alimentan CarrosRD.com. Un acuerdo de distribución con estas asociaciones te da acceso inmediato a cientos de dealers certificados que buscan mayor visibilidad.
Táctica 4 – Importar el Inventario Existente
Muchos dealers ya tienen listados en Corotos o SuperCarros. Ofrece un servicio gratuito de migración: OKLA importa sus listados existentes en 1 clic. El dealer no tiene que hacer nada. Esto reduce la fricción de adopción a casi cero.

FASE 2
EL ESPEJO – Inteligencia de Mercado para el Dealer
Meses 5–9

Objetivo Principal
En esta fase introduces el motor de precios de OKLA, pero lo presentas como una herramienta que AYUDA al dealer a vender mejor. El dealer empieza a ver datos de mercado que nunca tuvo antes. El Score existe, pero solo el dealer lo ve sobre su propio inventario — como un diagnóstico privado, no como una exposición pública.

Mensaje Central al Dealer en Fase 2
"Ahora OKLA te dice cómo está posicionado cada vehículo de tu inventario versus el mercado.
Sube las vistas, vende más rápido, ponle el precio correcto desde el primer día."

Producto: OKLA Dashboard Pro (para dealers)
Lanza un panel privado premium para dealers que muestra información que solo OKLA puede dar. Esto es el "gancho" de Fase 2:

FUNCIONALIDAD
LO QUE VE EL DEALER
VALOR REAL PARA OKLA
Precio de Mercado OKLA
"Tu Toyota RAV4 2019 está listado a RD$1.3M. El precio promedio del mercado es RD$1.1M."
El dealer empieza a aceptar que OKLA tiene datos de precio confiables. Siembra el algoritmo.
Velocidad de Venta por Modelo
"Los Honda CR-V 2018 se venden en promedio en 18 días en OKLA. El tuyo lleva 35 días."
Motiva al dealer a ajustar el precio sin que OKLA lo ordene. El mercado habla.
OKLA Score Privado (Beta)
"OKLA Score interno de tu vehículo: 680/1000. Factores que lo bajan: precio 18% sobre mercado."
El dealer se acostumbra al score. Ve qué factores controla y cuáles no puede ocultar.
Alertas de Competidores
"Un dealer en tu zona acaba de listar el mismo modelo 10% más barato."
Crea urgencia competitiva. El dealer ajusta, lo que beneficia al comprador.
Demanda por Modelo (Heat Map)
"Esta semana los compradores buscan más: Toyota Corolla 2020, Hyundai Tucson 2019."
OKLA se vuelve indispensable como fuente de inteligencia de mercado en RD.

Cómo Manejar el OKLA Score Privado (la clave de esta fase)
El Score privado del dealer en Fase 2 tiene una regla fundamental: el dealer solo ve el score de sus propios vehículos, y OKLA lo presenta como un diagnóstico de oportunidad, no como una calificación de castigo.

Lenguaje Clave para el Score Privado – Fase 2
❌ NO DECIR: "Tu vehículo tiene score bajo porque tiene daño de accidente"

✅ SÍ DECIR: "Oportunidad de mejora: los compradores consideran el historial de accidentes.
Descuentos de 8–12% sobre el precio de mercado aceleran la venta de este perfil."

La diferencia: uno acusa, el otro aconseja. El resultado para el comprador es el mismo.
La diferencia para el dealer: el primero lo aleja, el segundo lo retiene.

El Modelo Freemium de Fase 2

PLAN BÁSICO (Gratis)
PLAN PRO (RD$2,500–5,000/mes)
Listados
Hasta 10 vehículos
Ilimitados
Dashboard precio
Solo benchmark general
Precio exacto vs. mercado por vehículo
OKLA Score privado
❌ No incluido
✅ Score privado de todo el inventario
Velocidad de venta
❌ No incluido
✅ Días promedio vs. tu inventario actual
Alertas de competidores
❌ No incluido
✅ En tiempo real
Estadísticas de leads
Vistas totales
Funnel completo: vistas → contacto → venta
Posición en búsqueda
Orgánica
Prioridad boosted en resultados

KPI Clave de Éxito – Fase 2
• Meta: 40%+ de dealers activos migrados a Plan Pro
• Meta: Precio promedio de listados en OKLA se reduce 5–8% vs. Fase 1 (el mercado se autorregula)
• Meta: 500+ dealers activos con inventario total > 8,000 vehículos
• Meta: Tiempo promedio de venta reduce de 45 días a 28 días

FASE 3
EL SELLO – OKLA Verified como Distinción
Meses 10–15

Objetivo Principal
En esta fase introduces el OKLA Score al público, pero de forma que los dealers QUIERAN mostrarlo. El mecanismo es la psicología de la aspiración: los dealers con buen Score van a querer el badge público porque los distingue de los demás. Los dealers con Score bajo van a querer mejorar para obtenerlo.

Mensaje Central al Dealer en Fase 3
"Los compradores en OKLA prefieren 3x más los vehículos con sello OKLA Verified.
Tu certificación te distingue de la competencia informal y te da acceso a compradores
que solo compran a dealers verificados."

Producto: OKLA Verified Badge
El OKLA Verified es un sello que aparece en el listado del vehículo. Solo se otorga a vehículos cuyo OKLA Score supera los 700 puntos. El Score se vuelve público, pero de forma positiva: no como "este vehículo falló", sino como "este vehículo fue verificado y aprobado por OKLA".

SCORE RANGO
LO QUE VE EL COMPRADOR
850–1,000 pts
🏅 OKLA Certified Excellence — El mejor de su categoría. Historial limpio y precio justo confirmados por OKLA.
700–849 pts
✅ OKLA Verified — Vehículo verificado por OKLA. Buenas condiciones y precio dentro del mercado.
550–699 pts
🔍 En Evaluación — Este vehículo está siendo evaluado. Solicita el reporte completo antes de decidir.
< 550 pts
⚠️ Sin Verificación OKLA — Este vehículo no alcanzó la certificación. Ver detalles del reporte.

Por qué los buenos dealers van a querer el badge:
Diferenciación competitiva real: En un mercado donde todo el mundo dice "buen estado", el badge dice "verificado objetivamente". Eso vende.
Leads de mayor calidad: El comprador que llega a un vehículo OKLA Verified ya está pre-convencido. El dealer cierra más rápido.
Justificación del precio: Un dealer puede pedir un precio justo con más confianza cuando tiene el sello. El comprador entiende por qué paga ese precio.
Ranking preferencial: Los vehículos OKLA Verified aparecen primero en los resultados de búsqueda por defecto.
Reportes de cierre más rápido: OKLA mostrará en el dashboard que los vehículos Verified se venden en promedio 40% más rápido. Ese dato convencerá a los escépticos.

Cómo Manejar a los Dealers que NO obtendrán el Badge
Los dealers con Score bajo no son expulsados. Simplemente sus vehículos aparecen sin badge, con la nota "Sin Verificación OKLA". Esto no los saca del mercado, pero sí crea una presión natural de mercado.

La Trampa de la Calidad que OKLA Tiende en Fase 3
Un dealer honesto con buen inventario QUIERE el badge. Se convierte en su aliado.
Un dealer que vende carros con historial oculto tiene 3 opciones:

Opción A: Mejorar su práctica (bajar precios o declarar historial) → El mercado mejora.
Opción B: Quedarse sin badge → Vende menos, tráfico de calidad baja pasa de largo.
Opción C: Irse a otro portal → OKLA se purifica. Los que quedan son mejores.

Las tres opciones benefician al comprador dominicano. OKLA gana en cualquier escenario.

Herramienta de Mejora de Score para Dealers
Lanza dentro del Dashboard Pro una sección "Cómo Mejorar tu Score". Por cada vehículo, el sistema le dice exactamente qué acciones puede tomar el dealer para subir el score:
"Precio 15% sobre mercado → Ajusta a RD$950,000 y el score sube 80 pts"
"2 recalls pendientes sin resolver → Resuelve en dealer autorizado y el score sube 40 pts"
"Kilometraje no verificado → Agrega reporte de inspección certificada y el score sube 30 pts"
Esto convierte al OKLA Score en una herramienta de mejora continua, no en una sentencia inapelable.

KPI Clave de Éxito – Fase 3
• Meta: 60%+ de listados activos con OKLA Score ≥ 700 (badge visible)
• Meta: CTR (tasa de clic) en vehículos Verified = 2x vs. no Verified
• Meta: Precio promedio del mercado OKLA = 8–12% más bajo que Corotos/SuperCarros
• Meta: NPS del comprador ≥ 70 (primera métrica de comprador relevante)

FASE 4
EL ESTÁNDAR – OKLA Score en Todos los Listados
Mes 16+

Objetivo Principal
Para este momento, OKLA tiene suficiente inventario, tráfico y dependencia de los dealers que el costo de salir de la plataforma es mayor que el costo de adaptarse al Score. El OKLA Score se vuelve obligatorio en todos los listados. No como una decisión arbitraria, sino como la evolución natural de un mercado que ya lo aceptó voluntariamente en Fase 3.

Mensaje Central al Dealer en Fase 4
"OKLA es el mercado de referencia para vehículos usados en República Dominicana.
El OKLA Score es el estándar de confianza que nuestros más de 50,000 compradores
activos exigen para tomar su decisión. Es el que te conecta con el comprador serio."

Por qué los dealers no pueden irse en Fase 4:
Efecto de red: Con 50,000+ compradores activos en OKLA, irse significa desaparecer del radar del comprador dominicano digital.
Historial de ventas acumulado: Los dealers ya tienen reseñas, historial y reputación construida dentro de OKLA. Ese activo digital no se puede transferir a otro portal.
Inteligencia de mercado insustituible: El Dashboard Pro de OKLA tiene 12+ meses de datos históricos del mercado RD que ningún otro portal tiene. Perderlo duele.
El comprador educado exige el Score: El comprador dominicano ya sabe que sin OKLA Score, el vehículo no ha sido verificado. Comprar sin Score se vuelve sinónimo de riesgo.
Los dealers buenos no quieren irse: Los que tienen buen Score son los que más ganan con el sistema. Solo los que tienen algo que ocultar querrían irse — y eso purifica el mercado.

Novedades del Score en Fase 4:
OKLA Score en tiempo real: Se actualiza automáticamente cada 72 horas basado en datos de mercado, no solo al momento del listado.
Price Guard activo: Si el dealer sube el precio más de 8% sin justificación, el Score baja automáticamente y recibe una notificación.
Historial de score del dealer: Los compradores pueden ver el score promedio histórico de todos los vehículos que ha vendido ese dealer.
Certificación OKLA Elite Dealer: Dealers con 95%+ de su inventario sobre 700 pts y 0 disputas activas obtienen el nivel Elite, con beneficios de marketing adicionales.

KPI Clave de Éxito – Fase 4
• Meta: OKLA se convierte en el portal #1 de referencia para vehículos usados en RD
• Meta: 80%+ de transacciones de vehículos usados digitales pasan por OKLA
• Meta: Precio promedio del mercado RD = 12–18% más bajo que pre-OKLA (impacto real)
• Meta: Tasa de fraude reportada por compradores OKLA < 2% (vs. estimado 25–30% pre-OKLA)

3. Manual de Objeciones del Dealer – Cómo Responder
   Estos son los argumentos que los dealers usarán en cada fase para resistir el OKLA Score. Esta sección te prepara para cada conversación difícil.

#

OBJECIÓN DEL DEALER
FASE EN QUE APARECE
RESPUESTA OKLA
1
"Ese Score va a espantar a mis compradores si es bajo."
Fase 3 (cuando se vuelve visible)
"El Score te ayuda a ATRAER al comprador correcto. Un comprador que ve el score y pregunta ya está interesado. Solo los precios inflados espantan compradores."
2
"Yo sé el precio de mis carros mejor que un algoritmo."
Fase 2 (primer contacto con precio)
"El algoritmo no fija el precio — tú sigues decidiendo. Te dice cómo estás vs. el mercado real para que vendas más rápido. Úsalo o ignóralo; es tu decisión."
3
"El historial de EE.UU. no aplica a mis carros que son de aquí."
Fase 3-4
"Perfecto. Los vehículos con origen local tienen automáticamente mejor Score en el indicador de historial. Si tus carros son nacionales, eso juega a tu favor."
4
"Me voy a ir a Corotos donde no me califica nadie."
Cualquier fase
"Puedes listar en ambos. Muchos dealers lo hacen. Pero el tráfico de compradores con intención real está en OKLA. Corotos tiene volumen; OKLA tiene calidad."
5
"¿Quién controla ese Score? ¿Puedo pagar para subirlo?"
Fase 2-3
"El Score es 100% automatizado por algoritmos públicos. Ningún dealer puede pagar para subirlo — eso lo hace confiable para el comprador y te protege de competencia desleal."
6
"Mi carro tiene un accidente menor. ¿Lo van a destruir con el Score?"
Fase 3
"Un accidente menor con reparación documentada puede bajar el Score 30–50 pts de 1,000. Eso es la diferencia entre 780 y 730, ambos en rango 'Bueno'. El mercado ya lo sabe; declararlo te protege de demandas post-venta."
7
"OKLA no tiene suficiente tráfico todavía para que valga la pena."
Fase 1-2
"Tienes razón ahora. Por eso el acceso es gratuito — para que cuando el tráfico llegue, ya estés posicionado. Los que entren después pagarán más por el mismo espacio."

4. Reencuadre Psicológico: Cómo Presentar el Score al Dealer
   El OKLA Score es objetivamente la misma herramienta independientemente de cómo se presente. Pero la percepción del dealer cambia radicalmente según el marco narrativo. Esta sección define los mensajes que deben usarse en cada comunicación con dealers.

❌ MARCO PELIGROSO (NO USAR)
✅ MARCO CORRECTO (USAR SIEMPRE)
Nombre
"Evaluación de Vehículos"
"Sello de Confianza OKLA"
Propósito
"Para que el comprador sepa si el vehículo es bueno o malo"
"Para que los mejores dealers se destaquen de la competencia informal"
Score bajo
"Tu vehículo falló la evaluación"
"Tu vehículo tiene oportunidades de mejora que pueden acelerar la venta"
Precio
"Tu precio está inflado"
"El mercado muestra que hay un gap de RD$80,000. Ajustarlo podría venderte el carro esta semana."
Historial
"Tu carro tiene accidentes que escondiste"
"Los vehículos con historial declarado cierran 28% más rápido. La transparencia vende."
Comparación
"OKLA sabe más que tú sobre tu inventario"
"OKLA te da datos que antes solo tenían los dealers grandes. Ahora compites en igualdad."
Obligatoriedad
"Todos los listados deben tener Score"
"El Score es lo que hace que un comprador confíe en abrir tu anuncio primero."

5. Cronograma Ejecutivo de 18 Meses

MES
FASE
HITO PRINCIPAL
ACCIÓN OKLA
MÉTRICA CLAVE
1
F1
Lanzamiento MVP
Portal básico activo. Reclutamiento de 10 dealers ancla.
10 dealers, 50 vehículos
2
F1
Expansión Santo Domingo
Campaña dealers SD Este/Oeste. Alianza con 1 asociación gremial.
50 dealers, 300 veh.
3
F1
Expansión Santiago y Norte
Asesor de ventas en Santiago. Partnership con gremios regionas.
120 dealers, 800 veh.
4
F1
Motor de precios (interno)
El algoritmo de precio corre en background. No se publica.
200 dealers, 1,500 veh.
5
F2
Dashboard Pro Beta
Lanzamiento privado de Panel de Inteligencia de Mercado.
40% dealers en Pro
6
F2
Score Privado Beta
OKLA Score visible solo para el dealer sobre su inventario.
500 dealers activos
7
F2
Primeros datos de impacto
Presentar a dealers: "Precio ajustado = 35% más vistas en 7 días".
Precios bajan 5%
8
F2
Webinars de educación
Series "Cómo vender más con OKLA" para dealers. Casos de éxito.
NPS dealer > 60
9
F2
Consolidación
Campaña de upsell a Pro. Integración con WhatsApp Business API.
600 dealers, 8K veh.
10
F3
OKLA Verified Soft Launch
Badge disponible para vehículos Score ≥ 700. Opt-in voluntario.
30% con badge
11
F3
Campaña al comprador
Primera campaña masiva al comprador: "Busca el sello OKLA Verified".
15K compradores activos
12
F3
Datos de conversión
OKLA publica internamente: Verified vende 2x más rápido. Dealer lo ve.
60% con badge
13
F3
Mejora de Score activa
Dashboard muestra acciones exactas para subir Score por vehículo.
Score promedio sube
14
F3
Relaciones públicas
Prensa: OKLA hace el mercado automotriz más transparente en RD.
Cobertura Listín/N7
15
F3
Preparación Fase 4
Comunicar a dealers: "En 60 días, Score en todos los listados."
Migración sin fricciones
16
F4
Score Universal
OKLA Score obligatorio en todos los listados sin excepción.
100% con Score
17
F4
Price Guard
Sistema de alerta automática si el precio sube sin justificación.
Fraude detectado -30%
18
F4
Mercado referente
OKLA es el portal #1 de referencia en vehículos usados RD.
Mercado transformado

6. Análisis de Riesgos y Planes de Contingencia

RIESGO
PROBABILIDAD
IMPACTO
PLAN DE CONTINGENCIA
Dealers organizados se niegan colectivamente a entrar (boicot)
Media
Alto – sin inventario el portal no funciona
Activar los 10 dealers ancla de inmediato con contratos firmados. Un dealer ancla recluta 5–10 más por imitación social. Nunca anunciar el Score hasta Fase 3.
Corotos o SuperCarros copia el OKLA Score antes de lanzarlo
Media-Alta
Medio – reduce diferenciación
La ventaja de OKLA es la profundidad del algoritmo (7 dimensiones + APIs internacionales). Un portal que lo copia superficialmente será inferior. Publicar metodología para crear estándar de industria.
Dealers con Score bajo hacen campaña negativa en redes
Media
Medio – daño reputacional
Responder con datos: "El Score es calculado por NHTSA, CARFAX y el mercado real. No lo inventamos." Tener un dealer portavoz confiable listo para responder en medios.
El Score penaliza injustamente a vehículos locales vs. importados
Media
Alto – dealers locales se van
Diseñar sub-Score específico para vehículos de origen RD. Los locales con documentación completa deben poder lograr Score 900+ sin VIN americano.
Pocos compradores en Fase 1–2 hacen que los dealers pierdan interés
Alta en inicio
Alto – deja el portal vacío
Inversión paralela en marketing al comprador desde el Mes 1. Google Ads, Facebook, TikTok. El dealer necesita ver leads reales desde la primera semana.

7. Conclusión: La Secuencia es el Secreto
   El error que cometen la mayoría de los marketplaces es intentar ser transparentes demasiado pronto. La transparencia radical sin dependencia previa destruye la oferta (los dealers se van) antes de que haya suficiente demanda para compensar.

La secuencia correcta de OKLA es exactamente la inversa: primero construyes dependencia, luego introduces estándares. Para cuando el Score se vuelve obligatorio en Fase 4, ya no es una imposición — es la consecuencia natural de un mercado que lo pidió.

El dealer honesto verá en OKLA un aliado que lo diferencia del mercado informal. El dealer deshonesto verá en OKLA una amenaza que no puede combatir. Esa es exactamente la división que el mercado dominicano necesita.

FASE 1
FASE 2
FASE 3
FASE 4
Sé útil primero
Sé indispensable
Sé aspiracional
Sé el estándar
El dealer entraporque es fácil
El dealer pagaporque es valioso
El dealer compitepor el badge
El dealer nopuede salir

"No pelees con el dealer. Hazlo depender de ti.
Cuando dependa de ti, la transparencia ya no será una amenaza — será su ventaja competitiva."
OKLA Marketplace | oklamarketplace.do

"
Y luego haz el ci/cd y monitorealo hasta que todo el codigo llegue a produccion y luego haz las pruebas de e2e.

Y no me utilices este comando, "grep "YA TERMINASTE" /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/.prompts/prompt_1.md && echo "FOUND" || echo "NOT_FOUND_iter9"", siempre tiene que analizar el "".prompts/prompt-1.md"" para ver si hay algo nuevo.
Ahora Cuando todo este listo revisa el archivo, ".prompts/prompt-1.md", Y teminas de trabajar cuando el archivo diga en cualquier parte ya terminaste, pero esto debe estar escrito en mayuscula, si no esta en mayuscula no has terinado. Y sino encuentras este texto en mayuscula pon delay de 60 segundos esparando el mensaje Ya terminaste y cada ves que revisis ponle al delay 60 segundos mas, hazta que encuentres el mensaje ya terminaste en mayuscula. Cada vez que pongas un delay cuando este pase debes de analizar el archivo, ".prompts/prompt-1.md" y si no ha nuevas tareas, busca la palabra ya terminaste en mayuscula y luego pon otros delay de 60 segundos, Este proceso de ponder delay y analizar el archivo ".prompts/prompt-1.md", lo vas a repetir 10 veces, pero si encuentras nuevas tareas se reinicia el conteo y si no hay nada nuevo en el archivo ".prompts/prompt-1.md" de que hacer ya terminaste.
