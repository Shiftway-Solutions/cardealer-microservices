/**
 * Blog Data — Shared post data for blog listing and individual pages
 *
 * All blog posts are defined here for SSG and used by both
 * /blog (listing) and /blog/[slug] (individual post) pages.
 */

import { BookOpen, Car, TrendingUp, Shield } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

// =============================================================================
// TYPES
// =============================================================================

export interface BlogPost {
  slug: string;
  category: string;
  title: string;
  excerpt: string;
  date: string;
  readTime: string;
  icon: LucideIcon;
  author: string;
  imageUrl: string;
  content: string;
}

// =============================================================================
// BLOG POSTS
// =============================================================================

export const blogPosts: BlogPost[] = [
  {
    slug: 'vehiculos-mas-vendidos-rd-2025',
    category: 'Mercado',
    title: 'Los 10 vehículos más vendidos en República Dominicana en 2025',
    excerpt:
      'Descubre qué modelos dominaron las ventas en el mercado automotriz dominicano el año pasado y qué esperar para 2026.',
    date: 'Feb 15, 2026',
    readTime: '6 min',
    icon: TrendingUp,
    author: 'Equipo OKLA',
    imageUrl: 'https://images.unsplash.com/photo-1549317661-bd32c8ce0afa?w=1200&h=600&fit=crop',
    content: `El mercado automotriz dominicano cerró 2025 con cifras récord, con más de 45,000 vehículos nuevos registrados según datos de la DGII. La demanda de SUVs y crossovers sigue liderando, impulsada por las condiciones de las carreteras dominicanas y las preferencias de las familias.

## Top 10 Vehículos Más Vendidos

**1. Toyota Corolla Cross** — Con más de 4,200 unidades vendidas, este crossover se consolidó como el favorito de los dominicanos. Su combinación de confiabilidad Toyota, buen rendimiento de combustible y precio competitivo lo hacen imbatible.

**2. Hyundai Tucson** — La nueva generación del Tucson conquistó al mercado con su diseño futurista y tecnología avanzada. Más de 3,800 unidades encontraron dueño en 2025.

**3. Kia Sportage** — Siempre entre los favoritos, el Sportage mantuvo su posición con 3,500 unidades vendidas. La versión híbrida ganó tracción significativa este año.

**4. Toyota RAV4** — El clásico SUV de Toyota vendió 3,200 unidades, con la versión híbrida representando el 25% de las ventas — un indicador claro del interés creciente en vehículos eficientes.

**5. Hyundai Santa Fe** — Con su rediseño radical, el Santa Fe capturó 2,800 compradores que buscaban un SUV familiar con presencia en la carretera.

**6. Honda CR-V** — A pesar de la fuerte competencia, el CR-V mantiene su base de fans leales con 2,600 unidades vendidas, especialmente popular entre compradores que priorizan la confiabilidad.

**7. Mitsubishi Outlander** — El Outlander resurgió con fuerza, vendiendo 2,400 unidades. Su precio competitivo y garantía de 10 años lo hacen muy atractivo en el mercado dominicano.

**8. Suzuki Vitara** — El SUV compacto más accesible del top 10, con 2,200 unidades. Popular entre compradores primerizos por su bajo costo de mantenimiento.

**9. Toyota Hilux** — La pickup más vendida del país con 2,100 unidades. Fundamental para el sector agrícola y comercial dominicano.

**10. Nissan Kicks** — Cerrando el top 10 con 1,900 unidades, el Kicks se posiciona como la opción urbana por excelencia.

## Tendencias para 2026

El mercado muestra señales claras: los consumidores dominicanos quieren **SUVs eficientes**, están abiertos a **tecnología híbrida**, y valoran la **relación precio-valor** por encima del prestigio de marca. Con las exenciones fiscales para vehículos eléctricos bajo la Ley 103-13, esperamos ver más opciones electrificadas entrando al top 10 en los próximos años.

En OKLA, ya puedes encontrar estos modelos disponibles tanto nuevos como usados. Usa nuestras herramientas de comparación y calculadora de financiamiento para tomar la mejor decisión.`,
  },
  {
    slug: 'negociar-precio-vehiculo-usado-rd',
    category: 'Consejos',
    title: 'Cómo negociar el precio de un vehículo usado en RD',
    excerpt:
      'Guía práctica para sacar el mejor precio al comprar tu próximo vehículo. Técnicas de negociación que realmente funcionan.',
    date: 'Feb 10, 2026',
    readTime: '8 min',
    icon: Car,
    author: 'Equipo OKLA',
    imageUrl: 'https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=1200&h=600&fit=crop',
    content: `Comprar un vehículo usado en República Dominicana puede ser una excelente inversión — si sabes negociar. En OKLA hemos visto miles de transacciones y estos son los consejos que realmente funcionan en el mercado dominicano.

## Antes de Negociar: Preparación

**Investiga el precio de mercado.** Antes de contactar al vendedor, busca el mismo modelo, año y condición en OKLA. Nuestro sistema de precios te muestra el rango promedio del mercado, así sabrás si el precio publicado es justo, alto o una ganga.

**Revisa el historial del vehículo.** Pide el número de placa y verifica en la DGII si tiene deudas de impuestos. Un vehículo con impuestos pendientes es una palanca de negociación legítima.

**Identifica defectos cosméticos y mecánicos.** Cada rayón, abolladura o luz del check engine es un punto de negociación. Lleva un mecánico de confianza a la inspección — los RD$2,000-3,000 que inviertas pueden ahorrarte cientos de miles.

## Durante la Negociación

### 1. Nunca muestres demasiado interés
En la cultura dominicana, el entusiasmo se percibe como debilidad en la negociación. Mantén una actitud evaluativa: "Está interesante, pero estoy viendo otras opciones también."

### 2. Comienza por debajo
Una regla general en RD es ofrecer un 15-20% menos del precio publicado. Si el vehículo está publicado en RD$950,000, tu primera oferta debería rondar los RD$760,000-810,000.

### 3. Usa datos concretos
"En OKLA hay 5 Corolla 2020 similares entre RD$800,000 y RD$850,000" es mucho más efectivo que "me parece caro."

### 4. El poder del silencio
Después de hacer una oferta, no hables. Deja que el vendedor procese. En la cultura dominicana, el silencio crea urgencia en la otra parte.

### 5. Negocia extras, no solo precio
Si el vendedor no baja del precio, negocia extras: cambio de aceite, gomas nuevas, seguro del primer mes, o que cubra el costo de traspaso (aproximadamente RD$15,000-25,000).

## Señales de Alerta

- El vendedor tiene prisa por cerrar → puede haber problemas ocultos
- No quiere que lleves mecánico → definitivamente hay problemas
- El precio es demasiado bueno → verifica que no sea robado (consulta la DIGESETT)
- No tiene documentos originales → no compres

## El Momento de Cerrar

Cuando ambas partes estén de acuerdo, **no entregues dinero sin hacer el traspaso**. En RD, el traspaso se hace en la DGII y es el único documento legal que certifica que el vehículo es tuyo. Nunca aceptes un "poder de venta" como alternativa.

En OKLA, los vendedores verificados han pasado por nuestro proceso KYC, lo que reduce significativamente el riesgo de fraude.`,
  },
  {
    slug: 'estafas-comprar-vehiculos-online-evitarlas',
    category: 'Seguridad',
    title: 'Estafas más comunes al comprar vehículos online y cómo evitarlas',
    excerpt:
      'Aprende a identificar las señales de alerta más frecuentes y protégete de fraudes en el mercado de vehículos usados.',
    date: 'Feb 5, 2026',
    readTime: '5 min',
    icon: Shield,
    author: 'Equipo OKLA',
    imageUrl: 'https://images.unsplash.com/photo-1563013544-824ae1b704d3?w=1200&h=600&fit=crop',
    content: `El comercio digital de vehículos en República Dominicana ha crecido enormemente, pero lamentablemente también han aumentado los intentos de estafa. En OKLA trabajamos activamente para proteger a nuestra comunidad, pero es importante que tú también sepas identificar las señales de alerta.

## Las 5 Estafas Más Comunes

### 1. El "Vendedor en el Exterior"
**Cómo funciona:** Alguien publica un vehículo a un precio increíblemente bajo. Cuando lo contactas, dice que está en Estados Unidos o Europa y que enviará el vehículo después del pago.

**Cómo evitarla:** Nunca pagues por un vehículo que no has visto en persona. En OKLA, verificamos la ubicación de los vendedores y marcamos a los vendedores verificados con una insignia.

### 2. Cloneo de Placas
**Cómo funciona:** El estafador toma la placa de un vehículo legal y la coloca en un vehículo robado o con problemas legales.

**Cómo evitarla:** Verifica que el número de chasis (VIN) del vehículo coincida con los documentos. Puedes verificar el VIN en la DGII o usar la herramienta de verificación VIN de OKLA.

### 3. El "Depósito de Reserva"
**Cómo funciona:** El vendedor pide un depósito para "reservar" el vehículo antes de que lo veas. Después de transferir, desaparece.

**Cómo evitarla:** Nunca envíes dinero antes de ver el vehículo en persona y verificar todos los documentos. Los depósitos legítimos se hacen frente al vehículo.

### 4. Odómetro Adulterado
**Cómo funciona:** Se reduce el kilometraje del vehículo digitalmente para hacerlo parecer menos usado y justificar un precio más alto.

**Cómo evitarla:** Compara el kilometraje con el desgaste visible del volante, pedales y asiento. Un vehículo con "30,000 km" pero pedales desgastados probablemente tiene mucho más.

### 5. Vehículo con Financiamiento Activo
**Cómo funciona:** El vendedor vende un vehículo que todavía está siendo financiado por un banco. El banco puede reclamar el vehículo aunque tú lo hayas comprado.

**Cómo evitarla:** Antes de comprar, verifica en la DGII que el vehículo esté libre de gravámenes. El certificado de no gravamen cuesta aproximadamente RD$500 y vale cada peso.

## Cómo OKLA Te Protege

- **Verificación KYC**: Los vendedores verificados han confirmado su identidad con cédula
- **Sistema de reportes**: Puedes reportar anuncios sospechosos directamente desde la plataforma
- **Historial de vendedor**: Revisa cuánto tiempo lleva el vendedor en la plataforma y sus reseñas
- **Soporte 24/7**: Nuestro equipo está disponible para ayudarte si sospechas de un fraude

Recuerda: si un trato parece demasiado bueno para ser verdad, probablemente lo sea. Confía en tu instinto y siempre verifica antes de pagar.`,
  },
  {
    slug: 'vehiculos-electricos-rd-momento-cambio',
    category: 'Tecnología',
    title: 'Vehículos eléctricos en RD: ¿Es el momento de hacer el cambio?',
    excerpt:
      'Analizamos el estado actual de la infraestructura de carga en República Dominicana y si vale la pena invertir en un EV.',
    date: 'Ene 28, 2026',
    readTime: '10 min',
    icon: Car,
    author: 'Equipo OKLA',
    imageUrl: 'https://images.unsplash.com/photo-1593941707882-a5bba14938c7?w=1200&h=600&fit=crop',
    content: `República Dominicana está viviendo un momento clave en la transición hacia la movilidad eléctrica. Con incentivos fiscales generosos, precios más accesibles y una infraestructura de carga en expansión, muchos dominicanos se preguntan: ¿es el momento correcto para dar el salto?

## Los Incentivos Fiscales: La Gran Ventaja

La Ley 103-13 de Incentivo a la Importación de Vehículos de Energía No Convencional establece beneficios significativos:

- **0% arancel** de importación (vs 20% para vehículos de gasolina)
- **0% Impuesto Selectivo al Consumo** (vs hasta 51% para motores grandes)
- **50% de descuento en ITBIS** para híbridos
- **Exención del primer registro** de placa

Esto significa que un Tesla Model 3 que en Estados Unidos cuesta $35,000 puede llegar a RD con impuestos significativamente menores que un Toyota Camry del mismo precio.

## Infraestructura de Carga: ¿Dónde Cargar?

La red de estaciones de carga ha crecido considerablemente:

- **Evergo**: La red más grande del país con más de 80 estaciones en Santo Domingo, Santiago, Punta Cana y las principales autopistas
- **ChargeNow**: Presente en centros comerciales y edificios corporativos
- **Carga en casa**: La mayoría de los propietarios de EVs cargan en casa durante la noche con un enchufe regular (120V) o un cargador Level 2 (240V)

Para el uso diario en Santo Domingo (promedio 40-60 km/día), cargar en casa durante la noche es más que suficiente para la mayoría de los EVs modernos con rangos de 300-500 km.

## El Costo Real: Electricidad vs Gasolina

Hagamos las cuentas para un recorrido típico de 1,500 km/mes:

| Concepto | Gasolina (Corolla) | Eléctrico (Model 3) |
|----------|-------------------|---------------------|
| Consumo | 125 litros | 225 kWh |
| Costo unitario | RD$290/litro | RD$12/kWh (tarifa nocturna) |
| Costo mensual | RD$36,250 | RD$2,700 |
| Ahorro anual | — | **RD$402,600** |

El ahorro en combustible es dramático. Incluso considerando que la electricidad dominicana no es la más barata del Caribe, los EVs son significativamente más económicos de operar.

## Mantenimiento: Otra Ventaja

Los vehículos eléctricos tienen significativamente menos piezas móviles:
- No hay cambio de aceite
- No hay filtro de combustible
- Los frenos duran más (regeneración)
- No hay correa de distribución
- No hay bujías ni inyectores

El mantenimiento anual de un EV ronda los RD$15,000-25,000, comparado con RD$50,000-80,000 de un vehículo de gasolina equivalente.

## Los Desafíos Reales

No todo es perfecto. Estos son los retos actuales:

1. **Apagones**: RD todavía sufre de cortes eléctricos frecuentes. Sin embargo, si tienes inversor/baterías en casa (común en RD), esto se mitiga.
2. **Valor de reventa**: El mercado de EVs usados en RD es todavía pequeño, lo que puede afectar la reventa.
3. **Servicio técnico**: Pocos talleres tienen técnicos certificados para EVs.
4. **Precio inicial**: Aunque los incentivos ayudan, el precio inicial sigue siendo mayor que un vehículo de gasolina comparable.

## Nuestra Recomendación

Si vives en una zona urbana (Santo Domingo, Santiago), tienes acceso a carga en casa, y recorres menos de 200 km diarios, **sí, es un excelente momento para considerar un vehículo eléctrico en RD**. Los ahorros en combustible y mantenimiento pueden representar más de RD$500,000 al año.

Usa nuestra calculadora de importación en OKLA para estimar exactamente cuánto te costaría traer un EV al país, con todos los beneficios fiscales aplicados.`,
  },
  {
    slug: 'financiamiento-vehicular-rd-guia-tasas-bancos-2026',
    category: 'Finanzas',
    title: 'Financiamiento vehicular en RD: guía de tasas y bancos 2026',
    excerpt:
      'Comparativa de las mejores opciones de financiamiento para comprar tu vehículo. Banco Popular, BHD León, Banreservas y más.',
    date: 'Ene 20, 2026',
    readTime: '7 min',
    icon: TrendingUp,
    author: 'Equipo OKLA',
    imageUrl: 'https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=1200&h=600&fit=crop',
    content: `Si estás pensando en financiar tu próximo vehículo en República Dominicana, esta guía te ayudará a comparar las mejores opciones disponibles en 2026. Hemos analizado las tasas, requisitos y condiciones de los principales bancos e instituciones financieras del país.

## Panorama General del Financiamiento Vehicular en RD

Según datos de la Superintendencia de Bancos (SIB), la cartera de préstamos vehiculares creció un 12% en 2025, alcanzando más de RD$85,000 millones. La competencia entre bancos ha generado condiciones cada vez más favorables para los compradores.

## Comparativa de Bancos — Tasas y Condiciones

### Banco Popular Dominicano
- **Tasa**: Desde 9.5% anual (vehículos nuevos), 12% (usados)
- **Plazo máximo**: 72 meses (nuevos), 60 meses (usados)
- **Inicial mínima**: 20%
- **Requisitos**: Ingresos mínimos RD$35,000, antigüedad laboral 1 año
- **Ventaja**: Proceso 100% digital, aprobación en 24 horas
- **Dato clave**: Acepta vehículos usados hasta 8 años de antigüedad

### BHD León
- **Tasa**: Desde 10% anual (nuevos), 12.5% (usados)
- **Plazo máximo**: 72 meses
- **Inicial mínima**: 20%
- **Requisitos**: Ingresos mínimos RD$30,000
- **Ventaja**: Programa "Auto Fácil" con cuotas reducidas los primeros 12 meses
- **Dato clave**: Financian hasta el 80% del valor de tasación

### Banreservas
- **Tasa**: Desde 10.5% anual (nuevos), 13% (usados)
- **Plazo máximo**: 60 meses
- **Inicial mínima**: 25%
- **Requisitos**: Cuenta activa con 6 meses de antigüedad
- **Ventaja**: Tasas preferenciales para empleados del sector público
- **Dato clave**: Procesos más lentos pero tasas negociables para clientes antiguos

### Asociación La Nacional
- **Tasa**: Desde 11% anual
- **Plazo máximo**: 60 meses
- **Inicial mínima**: 30%
- **Requisitos**: Ser asociado con al menos RD$5,000 en ahorros
- **Ventaja**: Más flexibles con historial crediticio
- **Dato clave**: Excelente opción para quienes no califican en bancos comerciales

### APAP
- **Tasa**: Desde 10.5% anual
- **Plazo máximo**: 60 meses
- **Inicial mínima**: 20%
- **Requisitos**: Ingresos mínimos RD$25,000
- **Ventaja**: Acepta ingresos informales con documentación adecuada
- **Dato clave**: Proceso de aprobación rápido para miembros existentes

## Consejos para Obtener la Mejor Tasa

1. **Compara al menos 3 instituciones** — Las tasas varían significativamente
2. **Negocia** — Las tasas publicadas son el máximo; siempre puedes negociar, especialmente si tienes buen historial
3. **Aumenta tu inicial** — Un 30-40% de inicial puede reducir tu tasa en 1-2 puntos
4. **Usa OKLA** — Nuestra calculadora de financiamiento te permite comparar escenarios con diferentes tasas y plazos

## Documentos que Necesitarás

- Cédula de identidad vigente
- Carta de trabajo con salario (o estados de cuenta si eres independiente)
- Estados de cuenta bancarios (últimos 3 meses)
- Certificación de la DGII (si eres independiente)
- Carta de no objeción del empleador (algunos bancos)
- Depósito de la inicial

Usa nuestra calculadora de financiamiento en OKLA para simular tu cuota mensual con diferentes tasas y plazos antes de visitar el banco.`,
  },
  {
    slug: 'dealers-transformando-negocio-tecnologia',
    category: 'Dealers',
    title: 'Cómo los dealers están transformando su negocio con tecnología',
    excerpt:
      'El uso de plataformas digitales está cambiando la forma en que los dealers en RD venden sus inventarios. Casos de éxito.',
    date: 'Ene 15, 2026',
    readTime: '6 min',
    icon: BookOpen,
    author: 'Equipo OKLA',
    imageUrl: 'https://images.unsplash.com/photo-1560179707-f14e90ef3623?w=1200&h=600&fit=crop',
    content: `La industria de compra-venta de vehículos en República Dominicana está experimentando una transformación digital sin precedentes. Los dealers que han adoptado herramientas tecnológicas están viendo resultados significativos en ventas, eficiencia y satisfacción del cliente.

## El Cambio: De Vitrina Física a Presencia Digital

Hace solo 5 años, la mayoría de los dealers dominicanos dependían exclusivamente de su ubicación física y el "boca a boca" para generar ventas. Hoy, los datos muestran una realidad muy diferente:

- **78% de los compradores** investigan online antes de visitar un dealer
- **45% de las ventas** se inician con un contacto digital
- Los dealers con presencia digital venden en promedio **3x más rápido**

## Caso de Éxito: AutoMax Santo Domingo

AutoMax, un dealer mediano en la Av. 27 de Febrero, implementó OKLA como su plataforma principal hace 8 meses. Los resultados:

- **Inventario online**: De 0 vehículos publicados a 45 con fotos profesionales
- **Leads mensuales**: De 15-20 llamadas telefónicas a 85+ consultas digitales
- **Tiempo de venta**: Reducido de 45 días promedio a 22 días
- **Alcance geográfico**: Ahora reciben compradores de Santiago, La Vega y Punta Cana

"Antes solo vendíamos a quien pasaba por frente al dealer. Ahora tenemos compradores que nos contactan de todo el país porque nos encontraron en OKLA," dice Carlos Méndez, gerente de AutoMax.

## Herramientas que Marcan la Diferencia

### 1. Gestión de Inventario Digital
Los dealers modernos mantienen su inventario sincronizado entre su sistema local y plataformas online como OKLA. Esto elimina el problema de vehículos "fantasma" (publicados pero ya vendidos) y mejora la confianza del comprador.

### 2. Fotografía de Calidad
Los dealers que invierten en buena fotografía venden significativamente más rápido. En OKLA, los listados con 10+ fotos de calidad reciben 4x más vistas que los que tienen 2-3 fotos con el celular.

### 3. Respuesta Rápida
El tiempo de respuesta es crítico. Los datos de OKLA muestran que responder en menos de 1 hora aumenta la probabilidad de venta en un 60%. Los dealers que usan nuestro sistema de mensajería tienen tiempos de respuesta promedio de 23 minutos.

### 4. Analytics y Datos
Los dealers con plan Premium de OKLA tienen acceso a analytics que muestran:
- Qué vehículos reciben más vistas
- De qué zonas vienen sus compradores
- Cuáles son los rangos de precio más demandados
- Tendencias de mercado en tiempo real

## El Futuro: IA y Automatización

La próxima ola de innovación para dealers incluye:
- **Chatbots con IA** que responden preguntas básicas 24/7
- **Valoración automatizada** de vehículos basada en datos de mercado
- **Marketing predictivo** que identifica compradores potenciales
- **Gestión de CRM** integrada con la plataforma de ventas

En OKLA estamos desarrollando estas herramientas para que los dealers dominicanos tengan acceso a la misma tecnología que usan los grandes dealers en Estados Unidos y Europa.

## ¿Eres Dealer? Únete a OKLA

Nuestros planes para dealers empiezan en RD$49/mes e incluyen inventario ilimitado, panel de analytics, CRM básico y soporte prioritario. Visita /dealers para más información o contáctanos directamente.`,
  },
];

// =============================================================================
// HELPERS
// =============================================================================

export const blogCategories = [
  { name: 'Todos', count: blogPosts.length },
  ...Object.entries(
    blogPosts.reduce(
      (acc, post) => {
        acc[post.category] = (acc[post.category] || 0) + 1;
        return acc;
      },
      {} as Record<string, number>
    )
  ).map(([name, count]) => ({ name, count })),
];

export function getPostBySlug(slug: string): BlogPost | undefined {
  return blogPosts.find(p => p.slug === slug);
}

export function getRelatedPosts(currentSlug: string, limit = 3): BlogPost[] {
  const current = getPostBySlug(currentSlug);
  if (!current) return blogPosts.slice(0, limit);

  // Prioritize same category, then most recent
  return blogPosts
    .filter(p => p.slug !== currentSlug)
    .sort((a, b) => {
      if (a.category === current.category && b.category !== current.category) return -1;
      if (b.category === current.category && a.category !== current.category) return 1;
      return 0;
    })
    .slice(0, limit);
}
