OKLA
MARKETPLACE
━━━━━━━━━━━━━━━━━━━━━━━━━
ESTUDIO TÉCNICO CIENTÍFICO
OKLA SCORE™ & ALGORITMO DE PRECIO PROMEDIO DE MERCADO
Metodología para la Valoración Objetiva de Vehículos Usados en la República Dominicana

Versión 2.0 | República Dominicana | Marzo 2026
Alineado con Freemium v4.0 — Confidencial — Uso Interno OKLA Marketplace

1. Propósito del Estudio: El Problema Real en RD
   El mercado de vehículos usados en la República Dominicana enfrenta una asimetría de información grave y sistemática. El comprador promedio dominicano carece de herramientas objetivas para evaluar si el precio que le ofrecen es justo o si el vehículo tiene un historial problemático. Esto genera un ecosistema donde el fraude, la manipulación de odómetros y los vehículos con daño estructural oculto son moneda común.
   ¿Por qué el comprador dominicano es tan vulnerable?

La mayoría de los vehículos usados en RD provienen de subastas en Estados Unidos (COPART, IAAI, Manheim). Muchos llegan con títulos 'lavados' (title washing), con daños de inundación no declarados, con odómetros manipulados o reconstruidos tras accidentes graves. El dealer conoce toda esta información; el comprador, nada. OKLA Score nació para equilibrar esa balanza.

1.1 Casos Comunes de Engaño en el Mercado Dominicano
Vehículos de subastas COPART/IAAI con título 'Salvage' o 'Rebuilt' que se venden como 'impecables'
Odómetros manipulados (rollback) — un vehículo de 180,000 millas vendido como si tuviera 80,000
Daño por inundación (flood damage) — corrosión interna que aparece meses después de la compra
Marco doblado (frame damage) — daño estructural que compromete la seguridad y se oculta con pintura
Vehículos robados o con número de VIN clonado
Precios inflados 20–40% por encima del mercado real de EE.UU. sin justificación
Kilometraje reportado en millas vendido como si fuera kilómetros (5.8× de diferencia)

El OKLA Score™ es la respuesta técnica y científica a estos problemas. Es un índice compuesto que combina datos verificables de múltiples fuentes para entregar al comprador dominicano un número entre 0 y 1,000 que resume la confiabilidad, el estado y la relación precio–valor del vehículo. El Score aplica igualmente a vehículos ofrecidos por dealers registrados y por vendedores independientes — con las diferencias en D7 detalladas en la Sección 3.

2. Arquitectura del OKLA Score™
   El OKLA Score es un índice ponderado que agrega múltiples dimensiones del vehículo en una sola calificación. La metodología sigue principios de scoring financiero (similar al Credit Score FICO) y modelos de valoración de activos usados de la industria automotriz norteamericana.
   2.1 Escala del OKLA Score
   Rango
   Nivel
   Semáforo
   Recomendación
   Descripción
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
El OKLA Score se calcula a partir de 7 dimensiones principales. La dimensión D7 fue actualizada en la versión 2.0 para distinguir explícitamente entre dealers registrados y vendedores independientes, con un techo de puntos diferenciado que refleja el nivel de responsabilidad comercial de cada tipo de usuario.

#

DIMENSIÓN
Peso (%)
Pts Máx.
Fuente de Datos Primaria
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
Reputación del Vendedor / Dealer — v2.0 distingue dealer vs. independiente
4%
40
Base interna OKLA / Reseñas

3. Cálculo Detallado por Dimensión
   D1 — Historial en EE.UU. (VIN History) | Peso: 25% | Máx. 250 pts
   Esta es la dimensión más crítica. La gran mayoría de vehículos usados en RD vienen de subastas de EE.UU. (COPART, IAAI, Manheim). El historial del VIN en EE.UU. revela la verdadera historia del vehículo antes de llegar al país. Esta dimensión aplica idénticamente a vehículos de dealers y vendedores independientes.
   Factor
   Puntos
   Lógica de Penalización
   Título Limpio (Clean Title)
   +100
   Base: sin historial de daño total
   Título Salvage/Totaled
   −200
   El vehículo fue declarado pérdida total por aseguradora
   Título Rebuilt/Reconstructed
   −120
   Reparado tras pérdida total — riesgo estructural latente
   Daño por Inundación (Flood)
   −180
   Corrosión interna severa, fallo eléctrico a largo plazo
   Daño de Granizo (Hail)
   −60
   Mayormente cosmético, pero depreciación significativa
   Historial de Accidentes (Leve)
   −30
   1–2 accidentes menores, sin daño estructural
   Historial de Accidentes (Moderado)
   −70
   Daño reportado a carrocería/suspensión
   Daño Estructural (Frame Damage)
   −150
   Compromete la integridad y seguridad total del vehículo
   Vehículo de Alquiler / Flota
   −40
   Uso intensivo, mantenimiento variable
   Lemon Law Buyback
   −180
   El fabricante recompró el vehículo por defectos crónicos
   VIN Clonado / Fraude de Título
   −250 (BLOQUEO)
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

VinAudit.com — Market Value API + History API (desde $0.25/reporte). Alternativa: FAXVIN.com o integración directa con NMVTIS (National Motor Vehicle Title Information System) del Departamento de Justicia de EE.UU. NMVTIS proveedores autorizados incluyen: VINCheck.info, VehicleHistory.gov.

D2 — Condición Mecánica y Técnica | Peso: 20% | Máx. 200 pts
Evalúa las especificaciones del vehículo contra su categoría de peso en el mercado. Un vehículo bien equipado de fábrica tiene mayor valor intrínseco. Aplica idénticamente a todos los vendedores.
Motor: cilindrada, tipo (turbo, híbrido, eléctrico) → hasta +60 pts
Transmisión: automática CVT/tiptronic vs manual → diferencia de hasta 30 pts
Tracción: AWD/4WD suma +25 pts vs FWD estándar
Tecnología de seguridad activa: Lane Assist, AEB, Blind Spot → +20 pts
Estado de recalls pendientes sin corregir → −15 pts por recall activo
Número de quejas en NHTSA (complaints) → −5 pts por cada 10 quejas del modelo
APIs para D2

1. NHTSA vPIC API: vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/{VIN}?format=json (GRATUITA)
2. NHTSA Safety Ratings API: api.nhtsa.gov/SafetyRatings/modelyear/{año}/make/{marca}/model/{modelo} (GRATUITA)
3. Edmunds API: edmunds.com/api/ — especificaciones, reviews y pricing (requiere registro)

D3 — Kilometraje / Odómetro | Peso: 18% | Máx. 180 pts
El odómetro es el dato más manipulado en el mercado dominicano. Un vehículo importado de EE.UU. puede tener sus millas registradas y verificables. El algoritmo OKLA cruza el kilometraje declarado por el vendedor con los registros históricos del VIN. Esta validación es especialmente crítica en el caso de vendedores independientes que no tienen historial de reputación acumulado en la plataforma.
Score KM
Lectura (Millas)
Lectura (KM equiv.)
Puntos Asignados
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
> −180 pts (penalización máxima)
> Nota Crítica — Conversión Millas/Kilómetros

La diferencia entre millas y kilómetros es de 1:1.60934. Un vehículo con 100,000 MILLAS tiene 160,934 KM reales. Muchos dealers en RD reportan el odómetro en millas como si fuera kilómetros, haciendo parecer que el carro 'tiene menos'. OKLA siempre convierte y valida la unidad correcta.

D4 — Precio vs. Mercado RD | Peso: 17% | Máx. 170 pts
Esta dimensión compara el precio de venta del vehículo con el Precio Justo OKLA (Sección 4). Es la dimensión que directamente protege el bolsillo del comprador. Se aplica idénticamente a todos los vendedores. Los vendedores independientes que sobrevaloren sus vehículos verán esta dimensión castigar fuertemente su Score.
Diferencia Precio vs. Mercado
Puntos
Interpretación
Precio ≤ −15% del mercado
+170
Oferta excelente — precio muy por debajo del mercado
Precio −15% a −5% del mercado
+140
Buen precio — ligeramente por debajo del promedio
Precio ±5% del mercado
+110
Precio justo — dentro del rango normal de mercado
Precio +5% a +15% sobre mercado
+60
Caro — negociar o buscar alternativas
Precio +15% a +30% sobre mercado
+20
Muy caro — poca justificación de valor
Precio > +30% sobre mercado
0
Precio abusivo — alto riesgo de fraude o engaño

D5 — Seguridad y Recalls NHTSA | Peso: 10% | Máx. 100 pts
Sin recalls activos pendientes: +100 pts
Recalls activos resueltos por el dealer: +80 pts
1 recall activo sin resolver: +60 pts
2–3 recalls activos sin resolver: +30 pts
4+ recalls activos sin resolver: 0 pts
Rating de seguridad NHTSA 5 estrellas: +20 pts adicionales (bonus)
Rating de seguridad NHTSA 4 estrellas: +10 pts adicionales
Rating de seguridad NHTSA 3 o menos estrellas: 0 pts adicionales
API Gratuita NHTSA

Recalls: api.nhtsa.gov/recalls/recallsByVehicle?make={MAKE}&model={MODEL}&modelYear={AÑO}
Seguridad: api.nhtsa.gov/SafetyRatings/
Quejas: api.nhtsa.gov/complaints/complaintsByVehicle?make={MAKE}&model={MODEL}&modelYear={AÑO}
Completamente gratuita, sin API Key requerida.

D6 — Depreciación y Año Modelo | Peso: 6% | Máx. 60 pts
La depreciación es un factor matemático previsible. Se aplica una curva calibrada para el mercado dominicano, donde la depreciación es más lenta que en EE.UU. por la menor oferta local. Aplica idénticamente a todos los vendedores.
Año actual (modelo nuevo): +60 pts
1–2 años de antigüedad: +50 pts
3–4 años de antigüedad: +40 pts
5–6 años de antigüedad: +30 pts
7–9 años de antigüedad: +20 pts
10–12 años de antigüedad: +12 pts
13+ años de antigüedad: +5 pts

D7 — Reputación del Vendedor / Dealer | Peso: 4% | Máx. 40 pts
La dimensión D7 es la única que trata diferente a dealers y vendedores independientes. A partir de la versión 2.0, D7 establece un techo de puntos diferenciado: +40 pts máximo para dealers certificados OKLA Verified y +18 pts máximo para vendedores independientes verificados. Esta diferencia refleja el nivel de responsabilidad comercial, el historial verificable y el compromiso formal con la plataforma.
D7 — Reputación del Vendedor / Dealer | Peso: 4% | Máx. 40 pts
Factor / Condición
Puntos
Aplica a
Dealer certificado OKLA Verified + historial positivo (Score promedio inventario ≥ 700)
+40
Solo Dealers registrados
Dealer registrado con historial neutro (sin disputas)
+25
Solo Dealers registrados
Vendedor independiente verificado (identidad confirmada, sin disputas)
+18
Vendedores independientes verificados
Vendedor independiente no verificado (solo perfil básico)
+10
Independientes sin verificación de identidad
Dealer con 1–2 disputas resueltas
+10
Dealers
Vendedor independiente con 1 disputa resuelta
+5
Vendedores independientes
Dealer con disputas activas o no resueltas
0
Dealers
Vendedor independiente con disputas activas
0
Vendedores independientes
Dealer bloqueado / fraude comprobado
BLOQUEO
Cualquier tipo de usuario
NOTA IMPORTANTE
Los vendedores independientes no pueden alcanzar el máximo de D7 (+40 pts), ya que ese nivel está reservado para dealers certificados OKLA Verified. Un independiente verificado obtiene un máximo de +18 pts en D7. Esta diferencia refuerza la confianza del comprador en los dealers certificados vs. los vendedores individuales.

Badges y Umbrales de Score — Comparativa Dealer vs. Vendedor Independiente
BADGES Y UMBRALES DE SCORE — Comparativa Dealer vs. Vendedor Independiente
Badge
Tipo de usuario
Score mínimo requerido
Beneficio
Dealer Verificado
Dealer registrado en OKLA
Score promedio inventario ≥ 700
Badge estándar azul en perfil y listings
Dealer Verificado Dorado
Dealer en plan PRO+
Score promedio inventario ≥ 700
Badge dorado + mayor visibilidad en búsquedas
Dealer Verificado Premium
Dealer en plan ELITE
Score promedio inventario ≥ 700
Badge premium + posición Top en directorio
Vendedor Verificado
Vendedor independiente
Score ≥ 750 (+50 pts más alto que dealer)
Badge verde básico en listings. Sin posición privilegiada en búsquedas.
DIFERENCIA CLAVE
Los dealers necesitan Score ≥ 700 para obtener badge. Los vendedores independientes necesitan Score ≥ 750 (50 puntos adicionales). Esta asimetría reconoce la mayor responsabilidad comercial del dealer e incentiva al independiente con volumen a registrarse como dealer para obtener el badge con menor umbral.

4. Algoritmo de Precio Promedio de Mercado OKLA
   4.1 Fórmula Maestra del Precio Justo OKLA
   Precio Justo OKLA (USD) =
   (P_USA × 0.35) + (P_RD_avg × 0.45) + (P_KBB × 0.20)
   × Factor_Ajuste_RD × Factor_Depreciacion × Factor_Condicion

Variable
Fuente
Descripción
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
Componente
Tasa Estimada
Aplicable a
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
Para calcular P_RD_avg con precisión, OKLA implementa un módulo de scraping/crawling que rastrea en tiempo real los principales portales dominicanos:
Portal
URL
Peso
Ventaja
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
   APIs de Historial de Vehículos (VIN History)
   Proveedor
   Endpoint / URL
   Costo
   Prioridad
   Datos Clave
   VinAudit
   vinaudit.com/api
   ~$0.25/reporte
   ⭐⭐⭐⭐⭐
   NMVTIS, títulos, odómetro, accidentes
   CARFAX (Business)
   carfax.com/business
   Contrato B2B
   ⭐⭐⭐⭐⭐
   Historia completa, mayor cobertura
   FaxVin
   faxvin.com
   $4.99–$9.99/rep.
   ⭐⭐⭐⭐
   NMVTIS, subastas, títulos
   Vini.az API
   vini.az/en/api
   Balances prepago
   ⭐⭐⭐⭐
   CARFAX+AutoCheck+Copart en 1 API
   NMVTIS (Gov.)
   vehiclehistory.gov
   Gratuita
   ⭐⭐⭐
   Fuente oficial EE.UU., solo básico
   AutoCheck (Experian)
   autocheck.com/b2b
   Contrato empresarial
   ⭐⭐⭐⭐
   Alternativa a CARFAX, score propio

APIs de Valoración y Precio de Mercado USA
Proveedor
Endpoint / URL
Costo
Prioridad
Datos Clave
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
VinAudit Market Value
vinaudit.com/api/market
Incluido en paquete
⭐⭐⭐⭐
Market value por VIN, rápido y económico

APIs Gubernamentales Gratuitas (EE.UU.)
API
Endpoint Ejemplo
Datos
Costo
NHTSA vPIC — VIN Decode
vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/{VIN}?format=json
Make, Model, Año, Motor
GRATIS
NHTSA Safety Ratings
api.nhtsa.gov/SafetyRatings/modelyear/{año}/make/{marca}/model/{modelo}
Calificación NCAP
GRATIS
NHTSA Recalls
api.nhtsa.gov/recalls/recallsByVehicle?make={M}&model={M}&modelYear={Y}
Lista de recalls
GRATIS
NHTSA Complaints
api.nhtsa.gov/complaints/complaintsByVehicle?make={M}&model={M}&modelYear={Y}
Quejas de consumidores
GRATIS

Portales de Subastas USA (Para verificar precio de origen)
Plataforma
URL
Uso en OKLA
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
   Paso
   Módulo
   Descripción
   1
   VIN Input & Decode
   El usuario ingresa el VIN de 17 dígitos. OKLA llama a NHTSA vPIC para decodificar: make, model, año, motor, país de fabricación.
   2
   History Fetch
   Se consulta VinAudit (o CARFAX) para obtener: título, propietarios, accidentes, odómetro histórico, tipo de uso (rental, fleet, personal).
   3
   Odometer Cross-Check
   Se compara el km/millas declarado por el vendedor con los registros del VIN. Si hay discrepancia >20%, se activa alerta de fraude automáticamente.
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
   Se aplica la fórmula maestra (Sección 4.1) para calcular el Precio Justo OKLA en USD y se convierte a RD$ usando el tipo de cambio del BCRD API.
   8
   Sub-Score Calculation
   Se calculan los 7 sub-scores (D1–D7). D7 aplica la categorización diferenciada: dealer certificado (máx +40) vs. independiente verificado (máx +18).
   9
   OKLA Score Final
   Se suman los sub-scores ponderados para producir el OKLA Score en escala 0–1,000.
   10
   Reporte & Badge
   Se genera el reporte OKLA con score, precio justo, alertas y consejos para el comprador. El badge se asigna: dealers ≥700 → Badge Dealer Verificado; independientes ≥750 → Badge Vendedor Verificado.

6.2 Tipo de Cambio Dinámico — API del BCRD
Para convertir el Precio Justo OKLA de USD a RD$, el sistema consume la tasa de cambio en tiempo real del Banco Central de la República Dominicana:

# API Banco Central RD (BCRD)

GET https://api.bcrd.gov.do/estadisticas/tipo-cambio

# Alternativa con ExchangeRate-API:

GET https://v6.exchangerate-api.com/v6/{API_KEY}/latest/USD

7. Guía de Interpretación para el Comprador Dominicano
   Esta sección define qué significa cada nivel del OKLA Score y qué acciones debe tomar el comprador. El Score se muestra de la misma forma independientemente de si el vehículo lo vende un dealer o un vendedor independiente. El badge indica el tipo y nivel de confianza del vendedor.

🟢 OKLA Score 850–1,000: EXCELENTE — Vehículo Recomendado
¿Qué significa para ti?
Este vehículo pasó todas las pruebas de OKLA con excelencia. Tiene historial limpio en EE.UU., precio justo o por debajo del mercado, kilometraje coherente con su historial, sin recalls graves pendientes y el vendedor tiene buena reputación.

Consejos:
✅ Puedes proceder con confianza. Haz la inspección mecánica de rutina igual.
✅ Verifica que la documentación local (matrícula, placa) esté en regla.
✅ Si tiene Badge Dealer Verificado: confianza máxima.
✅ Si tiene Badge Vendedor Verificado: vehículo con Score ≥ 750, calidad excepcional para vendedor individual.
✅ Considera que si el precio está muy por debajo del mercado con Score alto, puede ser una oportunidad excepcional.

🔵 OKLA Score 700–849: BUENO — Compra Segura con Revisión Básica
¿Qué significa para ti?
El vehículo está en buenas condiciones generales. Puede tener algún accidente menor documentado, kilometraje moderado o precio ligeramente sobre el óptimo.

🔵 Haz una inspección mecánica completa con un mecánico de confianza.
🔵 Si el precio es 5–10% sobre el mercado OKLA, intenta negociar.
🔵 Verifica los recalls pendientes y asegúrate de que el vendedor los resuelva antes de la entrega.
🔵 NOTA: Un dealer puede tener Badge Dealer Verificado (≥700). Un independiente en 700–749 aún NO tiene badge (necesita ≥750).

🟡 OKLA Score 550–699: REGULAR — Negocia y Verifica
¿Qué significa para ti?
El vehículo tiene factores que deben preocuparte: precio inflado, historial de accidentes moderado, kilometraje alto, o recalls sin resolver. NO es una compra automática.

🟡 Usa el Precio Justo OKLA como base de negociación — no pagues más.
🟡 Exige inspección por mecánico certificado ANTES de firmar cualquier documento.
🟡 Solicita al vendedor resolución de todos los recalls activos.
🟡 Si el vendedor se niega a negociar el precio en un Score 550–699, desconfía.

🔴 OKLA Score 400–549: DEFICIENTE — Precaución Extrema
¿Qué significa para ti?
Hay señales serias de alerta: posible historial de daño grave, precio muy por encima del mercado, odómetro sospechoso o título de origen cuestionable.

🔴 No compres sin una inspección especializada completa (endoscopio, escáner OBD2, análisis de pintura).
🔴 Solicita al vendedor el reporte CARFAX original del vehículo.
🔴 Verifica el historial de subasta en COPART o IAAI directamente con el VIN.
🔴 Si el vendedor presiona para un cierre rápido — es señal de fraude.

⚫ OKLA Score 0–399: CRÍTICO — No Comprar
¿Qué significa para ti?
OKLA detectó indicadores críticos: daño estructural, inundación severa, VIN potencialmente clonado, fraude de odómetro confirmado o precio completamente fuera de rango.

⛔ NO COMPRES ESTE VEHÍCULO bajo ninguna circunstancia.
⛔ Si ya pagaste algún depósito, contacta inmediatamente al PROCI (Protección al Consumidor).
⛔ Reporta el anuncio en OKLA Marketplace para proteger a otros compradores.
⛔ Nunca firmes documentos de transferencia bajo presión.

8. Impacto Social del OKLA Score en la República Dominicana
   El OKLA Score no es solo una herramienta técnica. Es un instrumento de equidad económica para el comprador dominicano, que históricamente ha carecido de acceso a la información que sí poseen los dealers y vendedores.
   Problema Actual en RD
   Solución OKLA
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
   UI simple: un número (0–1,000) lo resume todo
   Independiente vende vehículo sin transparencia de historial
   D7 diferencia dealer (máx +40) vs. independiente (máx +18), transparentando nivel de responsabilidad

Proyección de Impacto

Con solo un 10% de adopción en el mercado de vehículos usados de RD (estimado en +200,000 transacciones/año), OKLA podría prevenir fraudes y malas compras por valor de más de RD$500 millones anuales. El OKLA Score es para el comprador dominicano lo que CARFAX es para el estadounidense: su derecho a conocer la verdad antes de comprar.

Impacto adicional de la diferenciación dealer vs. independiente: el comprador ahora puede ver de un vistazo si está tratando con un comercio formal (badge dealer) o con un vendedor particular, y ajustar su nivel de diligencia en consecuencia.

9. Hoja de Ruta de Implementación del Motor OKLA Score v2.0
   Fase 1 — MVP (Meses 1–3): Score Básico
1. Integrar NHTSA vPIC API para VIN decode (gratuita) — aplica a dealers e independientes
1. Integrar VinAudit API para historial básico (~$0.25/reporte)
1. Implementar scraping de Corotos y SuperCarros para P_RD_avg
1. Calcular D1, D3, D4, D5 con fórmulas simplificadas
1. Calcular D7 con diferenciación: dealer (máx +40) vs. independiente (máx +18)
1. Lanzar OKLA Score v1.0 con semáforo básico (verde/amarillo/rojo)

Fase 2 — Score Completo (Meses 4–6) 7. Integrar MarketCheck API para precio USA real-time 8. Agregar scraping completo de Yacarros, Montao, CarrosRD, Encuentra24 9. Implementar tipo de cambio dinámico (API BCRD o ExchangeRate-API) 10. Calcular las 7 dimensiones completas D1–D7 11. Sistema de reputación de dealers en D7 — scoring diferenciado por tipo de usuario 12. Activar Badge Dealer Verificado (Score ≥ 700) y Badge Vendedor Verificado (Score ≥ 750)

Fase 3 — Score Premium y ML (Meses 7–12) 13. Integrar CARFAX Business API o Vini.az para datos premium 14. Modelo de Machine Learning para predecir precio de mercado RD en 90 días 15. OKLA Price Guard: alerta automática si el precio sube más del 5% en 7 días 16. Historial de OKLA Score: tracking del score de un vehículo en el tiempo 17. Certificación OKLA Verified para dealers con historial limpio 18. Diferenciación plena de D7: scoring avanzado independiente vs. dealer por tipo de transacción y volumen

10. Conclusión
    El OKLA Score™ v2.0 representa el estándar más avanzado y científicamente fundamentado para la evaluación de vehículos usados en la República Dominicana. Su metodología multidimensional, basada en datos verificables de fuentes de primer nivel como NHTSA, CARFAX, MarketCheck y los principales clasificados del país, lo posiciona como la herramienta definitiva para proteger al comprador dominicano.
    La actualización a la versión 2.0 introduce la diferenciación explícita entre dealers registrados y vendedores independientes en la dimensión D7, y establece umbrales de badge diferenciados (≥700 para dealers, ≥750 para independientes). Esta asimetría no perjudica al comprador — el Score del vehículo refleja fielmente su calidad — sino que añade una capa de transparencia: el comprador ahora puede distinguir de un vistazo si trata con un comercio formal certificado o con un vendedor particular.
    La combinación del Historial VIN en EE.UU. (25%), la evaluación de Precio vs. Mercado (17%) y la verificación de Odómetro (18%) cubre las tres formas de fraude más comunes en el mercado local. Las dimensiones de Seguridad NHTSA y Depreciación añaden la capa técnica que convierte a OKLA en una herramienta de confianza institucional, no solo de comparación de precios.
    OKLA no compite con los dealers honestos: los certifica y les da un sello de confianza. OKLA no discrimina a los vendedores independientes honestos: les da acceso al mismo Score con las mismas reglas técnicas. OKLA sí pone en evidencia a quienes engañan — sean dealers o independientes — y protege a los compradores que hoy no tienen con quién contar al momento de tomar la decisión más grande de su economía familiar.

"El OKLA Score es la verdad que el mercado dominicano siempre necesitó."
OKLA Marketplace | oklamarketplace.do

OKLA Marketplace · Estudio Técnico OKLA Score™ v2.0 · República Dominicana · Marzo 2026
Modelo Freemium v4.0 · Publicaciones Ilimitadas Gratuitas para Dealers · Score Gratuito para Compradores
