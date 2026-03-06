# 📊 Análisis de Mercado: Sistema de Facturación y Contabilidad para OKLA

**Fecha:** 6 de marzo de 2026  
**Preparado para:** OKLA — Marketplace vehicular, República Dominicana  
**Objetivo:** Evaluar la viabilidad de agregar un módulo de facturación electrónica (e-CF) y contabilidad básica para dealers y vendedores en la plataforma OKLA.

---

## 1. Análisis de Competidores — Sistemas Cloud en República Dominicana

### 1.1 Principales Sistemas

| Sistema                      | Origen                 | Precio Básico/mes | Precio Medio/mes  | Precio Avanzado/mes | Certificado DGII                   | Fortalezas                                                        |
| ---------------------------- | ---------------------- | ----------------- | ----------------- | ------------------- | ---------------------------------- | ----------------------------------------------------------------- |
| **Alegra**                   | Colombia (opera en RD) | USD $29           | USD $59           | USD $89–$129        | ✅ Sí                              | Líder LATAM en cloud, buen soporte español, módulos de inventario |
| **Odoo**                     | Bélgica                | $0–$7.25/usuario  | USD $22 (3 users) | USD $33+            | ⚠️ Requiere módulo localización RD | Open source, muy flexible, comunidad grande                       |
| **Facturero RD**             | Rep. Dominicana        | ~USD $18          | ~USD $38          | ~USD $70            | ✅ Sí                              | 100% local, conoce regulación DGII                                |
| **Siigo/Contífico**          | Colombia               | ~USD $25          | ~USD $55          | ~USD $90            | ⚠️ Parcial                         | Fuerte en Centroamérica                                           |
| **QuickBooks Online**        | EE.UU.                 | USD $30           | USD $55           | USD $80             | ❌ No nativo                       | Marca reconocida, pero no cumple eCF sin addon                    |
| **Bind ERP**                 | México                 | USD $29           | USD $59           | USD $109            | ❌ No                              | Cloud moderno, pero sin presencia real en RD                      |
| **ContaPyme**                | Colombia               | ~USD $20          | ~USD $45          | ~USD $75            | ⚠️ Parcial                         | Popular en pymes LATAM                                            |
| **DGII Facturador Gratuito** | DGII (gobierno)        | **$0**            | N/A               | N/A                 | ✅ Sí                              | Gratis, pero muy limitado (manual, sin API, sin reportes)         |

### 1.2 Observaciones del Mercado

- **Alegra** domina el mercado cloud para pymes en RD (>60% de market share en facturación cloud).
- El **Facturador Gratuito de la DGII** es usado por micro-negocios pero tiene limitaciones serias (sin automatización, sin API, interfaz pobre).
- **Odoo** es competitivo pero requiere configuración técnica que las pymes no pueden hacer solas.
- **Ningún sistema está especializado en el sector automotriz** — esta es la oportunidad de OKLA.

---

## 2. Propuesta de Precios para OKLA

### 2.1 Estrategia: 40–70% por debajo de competidores directos

OKLA tiene una ventaja competitiva única: **el sistema de facturación viene integrado con el marketplace**, eliminando la necesidad de un sistema separado. Esto justifica precios más bajos porque:

- No hay costo de adquisición de cliente adicional (ya están en OKLA)
- Los datos de vehículos/transacciones ya existen en la plataforma
- El costo marginal de agregar facturación a un usuario existente es mínimo

### 2.2 Planes Propuestos

| Plan                                 | Precio/mes                 | Facturas/mes | Módulos                                                     | Comparación                                          |
| ------------------------------------ | -------------------------- | ------------ | ----------------------------------------------------------- | ---------------------------------------------------- |
| 🆓 **Incluido con suscripción OKLA** | **$0**                     | 10           | Facturación básica (eCF), reportes simples                  | vs DGII Gratuito (0 facturas sin límite pero manual) |
| 💼 **Vendedor Pro**                  | **RD$599** (~USD $9.99)    | 50           | + Cuentas por cobrar, reportes ITBIS, exportar PDF          | vs Alegra $29/mes (67% menos)                        |
| 🏢 **Dealer Básico**                 | **RD$1,199** (~USD $19.99) | 200          | + Inventario, multi-usuario (3), gastos, dashboard          | vs Alegra $59/mes (66% menos)                        |
| 🏆 **Dealer Pro**                    | **RD$2,399** (~USD $39.99) | Ilimitadas   | + Contabilidad completa, nómina simple, API, multi-sucursal | vs Alegra $89–$129/mes (55–69% menos)                |

### 2.3 Ajuste de Planes de Suscripción OKLA Existentes

Los planes actuales de OKLA:

- **Vendedor Individual**: $29/listing → Mantener precio, incluir 10 facturas gratis
- **Dealer Básico**: $49/mes → Subir a **$59/mes** (incluye Facturación Vendedor Pro gratis, valor agregado)
- **Dealer Standard**: $149/mes → Subir a **$169/mes** (incluye Facturación Dealer Básico gratis)
- **Dealer Premium**: $299/mes → Subir a **$349/mes** (incluye Facturación Dealer Pro gratis)

**Impacto estimado**: Los dealers perciben +$10–$50/mes en valor agregado de facturación a cambio de +$10–$50/mes de aumento, manteniendo la percepción de precio justo.

---

## 3. Requisitos Legales en República Dominicana

### 3.1 Marco Legal Vigente

| Ley/Norma                         | Descripción                                  | Impacto para OKLA                                                     |
| --------------------------------- | -------------------------------------------- | --------------------------------------------------------------------- |
| **Ley 11-92** (Código Tributario) | Base legal para tributación en RD            | Define obligaciones de contribuyentes                                 |
| **Ley 253-12**                    | Fortalecimiento de la capacidad recaudatoria | Establece sanciones por incumplimiento fiscal                         |
| **Ley 32-23**                     | Ley de Facturación Electrónica               | **⚠️ CRÍTICA** — Obligatorio para todos los contribuyentes emitir eCF |
| **Norma General 06-2018**         | Regulación de NCF (Comprobantes Fiscales)    | Define tipos de comprobantes (B01, B02, B14, B15, etc.)               |
| **Norma General 05-2019**         | Comprobantes Fiscales Electrónicos           | Requisitos técnicos para eCF                                          |
| **Resolución DGII 2023**          | Calendario de implementación eCF             | Todas las empresas deben migrar a eCF                                 |

### 3.2 Tipos de Comprobantes Fiscales Electrónicos (eCF)

| Tipo                               | Código       | Uso                         | ¿Necesario para OKLA?                             |
| ---------------------------------- | ------------ | --------------------------- | ------------------------------------------------- |
| Factura de Crédito Fiscal          | **e-CF B01** | Venta a contribuyente (B2B) | ✅ **Esencial** — dealers comprando suscripciones |
| Factura de Consumo                 | **e-CF B02** | Venta a consumidor final    | ✅ **Esencial** — vendedores individuales         |
| Nota de Crédito                    | **e-CF B04** | Anulación/devolución        | ✅ **Esencial** — reembolsos                      |
| Nota de Débito                     | **e-CF B03** | Ajustes a favor del emisor  | ⚠️ Recomendado                                    |
| Registro de Proveedores Informales | **e-CF B11** | Compras a informales        | ❌ No necesario                                   |
| Régimen Especial                   | **e-CF B14** | Zonas francas               | ❌ No necesario                                   |
| Gubernamental                      | **e-CF B15** | Venta al gobierno           | ❌ No necesario                                   |
| Exportación                        | **e-CF B16** | Exportaciones               | ❌ No necesario                                   |

### 3.3 Requisitos Técnicos DGII para eCF

Para que OKLA emita eCF válidos:

1. **Certificación como PSFE** (Proveedor de Servicios de Facturación Electrónica):
   - Solicitud formal a la DGII
   - Auditoría técnica del sistema
   - Pruebas de interoperabilidad con el sistema DGII
   - **Costo estimado**: RD$150,000–$300,000 (~USD $2,500–$5,000) + tiempo 3–6 meses

2. **Firma Digital**: Cada eCF debe estar firmado digitalmente
   - Certificado de firma electrónica de una entidad certificadora acreditada
   - **Costo**: ~RD$15,000–$30,000/año (~USD $250–$500/año)

3. **Formato XML**: Estructura definida por la DGII (esquema XSD publicado)

4. **Comunicación con DGII**: API REST para envío y recepción de eCF
   - Ambiente de pruebas (sandbox) disponible
   - Ambiente de producción tras certificación

5. **Almacenamiento**: Conservar eCF por 10 años (Ley 11-92, Art. 50)

### 3.4 Impuesto ITBIS

- **Tasa general**: 18% (aplica a servicios digitales desde 2020)
- **OKLA debe cobrar ITBIS** en sus suscripciones
- **Declaración**: Mensual (IT-1) y Formato 606/607
- Los dealers que facturen deben declarar su propio ITBIS

---

## 4. Módulos Necesarios para el Sistema

### 4.1 Módulos Esenciales (MVP)

| #   | Módulo                              | Descripción                                                             | Prioridad     |
| --- | ----------------------------------- | ----------------------------------------------------------------------- | ------------- |
| 1   | **Facturación Electrónica (eCF)**   | Emisión de e-CF B01, B02, B04 con firma digital y envío a DGII          | 🔴 Crítico    |
| 2   | **Catálogo de Productos/Servicios** | Gestión de planes, suscripciones, listings como "productos" facturables | 🔴 Crítico    |
| 3   | **Clientes**                        | Registro de dealers/vendedores con RNC/Cédula, datos fiscales           | 🔴 Crítico    |
| 4   | **Cuentas por Cobrar**              | Seguimiento de facturas pendientes, vencidas, pagadas                   | 🔴 Crítico    |
| 5   | **Reportes Fiscales**               | Generación de Formato 606, 607, IT-1 para declaración DGII              | 🔴 Crítico    |
| 6   | **Dashboard Financiero**            | Ingresos, gastos, impuestos, métricas clave                             | 🟡 Importante |

### 4.2 Módulos Fase 2

| #   | Módulo                      | Descripción                                             | Prioridad     |
| --- | --------------------------- | ------------------------------------------------------- | ------------- |
| 7   | **Cuentas por Pagar**       | Gastos del dealer (alquiler, servicios, nómina)         | 🟡 Importante |
| 8   | **Inventario Vehicular**    | Integración con listados OKLA, costo vs precio de venta | 🟡 Importante |
| 9   | **Contabilidad General**    | Plan de cuentas, libro diario, mayor general, balance   | 🟡 Importante |
| 10  | **Multi-usuario con Roles** | Administrador, contador, vendedor                       | 🟡 Importante |
| 11  | **Recibos de Ingreso**      | Para pagos parciales o adelantos                        | 🟢 Deseable   |

### 4.3 Módulos Fase 3

| #   | Módulo                    | Descripción                                         | Prioridad   |
| --- | ------------------------- | --------------------------------------------------- | ----------- |
| 12  | **Nómina Simplificada**   | Cálculo de ISR, TSS para empleados del dealer       | 🟢 Deseable |
| 13  | **Multi-sucursal**        | Para dealers con múltiples ubicaciones              | 🟢 Deseable |
| 14  | **API Pública**           | Para integración con sistemas contables externos    | 🟢 Deseable |
| 15  | **Reportes Avanzados**    | Análisis de rentabilidad por vehículo, proyecciones | 🟢 Deseable |
| 16  | **Conciliación Bancaria** | Match de pagos recibidos con facturas emitidas      | 🟢 Deseable |

---

## 5. Análisis de Costos y Rentabilidad

### 5.1 Costos de Desarrollo

| Concepto                                   | Costo Estimado (USD) |
| ------------------------------------------ | -------------------- |
| Desarrollo MVP (Módulos 1–6)               | $25,000–$35,000      |
| Integración DGII (eCF API + certificación) | $5,000–$10,000       |
| Firma digital (certificado anual)          | $250–$500/año        |
| Certificación PSFE (auditoría DGII)        | $2,500–$5,000        |
| Pruebas y QA                               | $3,000–$5,000        |
| **Total desarrollo inicial**               | **$35,750–$55,500**  |

### 5.2 Costos Operativos Mensuales

| Concepto                                         | Costo/mes (USD) |
| ------------------------------------------------ | --------------- |
| Infraestructura adicional (DB, storage, compute) | $150–$300       |
| API DGII (si aplica costo por transacción)       | $50–$200        |
| Certificado de firma digital (prorrateado)       | $21–$42         |
| Soporte técnico (proporcional)                   | $200–$400       |
| Almacenamiento de eCF (10 años req.)             | $20–$50         |
| Monitoreo y backups                              | $50–$100        |
| **Total operativo mensual**                      | **$491–$1,092** |

### 5.3 Proyección de Ingresos (Año 1)

Asumiendo adopción gradual:

| Mes   | Vendedor Pro ($9.99) | Dealer Básico ($19.99) | Dealer Pro ($39.99) | Ingreso/mes |
| ----- | -------------------- | ---------------------- | ------------------- | ----------: |
| 1–3   | 10                   | 5                      | 2                   |        $280 |
| 4–6   | 25                   | 12                     | 5                   |        $689 |
| 7–9   | 50                   | 25                     | 10                  |      $1,400 |
| 10–12 | 80                   | 40                     | 15                  |      $2,198 |

**Ingreso acumulado Año 1**: ~$54,800  
**Costos Año 1**: ~$55,500 (desarrollo) + ~$9,500 (operativo) = ~$65,000

### 5.4 Punto de Equilibrio

- **Break-even**: ~14–18 meses con adopción conservadora
- **Break-even acelerado**: ~8–10 meses si se incluye facturación gratis en planes y se suben precios de suscripción

**Si se implementa la estrategia de subir precios de suscripción:**

- Aumento promedio de $20/mes × 150 dealers = $3,000/mes adicionales
- Esto cubre los costos operativos desde el día 1
- El desarrollo se paga con el ingreso incremental en ~12–15 meses

---

## 6. Análisis Legal: ¿Le conviene a OKLA?

### 6.1 ¿Puede OKLA ofrecer servicios de facturación?

| Pregunta                              | Respuesta                                                  |
| ------------------------------------- | ---------------------------------------------------------- |
| ¿Es una actividad regulada?           | **Sí**, requiere certificación PSFE de la DGII             |
| ¿Necesita licencia especial?          | **Sí**, certificación como Proveedor de eCF                |
| ¿Hay riesgo legal?                    | **Bajo**, si se obtiene la certificación                   |
| ¿Hay precedentes?                     | **Sí**, Alegra, Facturero, y otros operan legalmente en RD |
| ¿OKLA necesita ser contribuyente RNC? | **Ya lo es** (como empresa registrada)                     |

### 6.2 Ventajas Legales

1. **Cumplimiento obligatorio**: La Ley 32-23 obliga a todos a usar eCF. OKLA facilita el cumplimiento de sus dealers/vendedores → **valor agregado que reduce churn**.
2. **Datos ya existentes**: OKLA ya tiene los datos de transacciones → la facturación es una extensión natural.
3. **Barrera de salida**: Un dealer que usa la contabilidad de OKLA es menos probable que migre a otro marketplace.
4. **Ingreso recurrente**: Facturación genera revenue mensual predecible (SaaS).

### 6.3 Riesgos Legales

1. **Responsabilidad por datos fiscales**: Si el sistema genera un eCF incorrecto, el contribuyente (dealer) es responsable, no OKLA. Pero OKLA debe tener disclaimer claro.
2. **Privacidad de datos financieros**: Ley 172-13 sobre Protección de Datos Personales aplica. Los datos contables son sensibles.
3. **Auditoría DGII**: Si OKLA es PSFE, la DGII puede auditar el sistema en cualquier momento.

### 6.4 Recomendación

| Opción                                                     | Pros                            | Contras                                           | Recomendación          |
| ---------------------------------------------------------- | ------------------------------- | ------------------------------------------------- | ---------------------- |
| **A: Desarrollar internamente + certificar PSFE**          | Control total, márgenes altos   | Costo alto, 3–6 meses certificación               | ✅ Largo plazo         |
| **B: Integrar con proveedor certificado (ej. Alegra API)** | Rápido (1–2 meses), bajo riesgo | Dependencia, márgenes menores                     | ✅ **MVP recomendado** |
| **C: No hacer nada**                                       | Sin costo                       | Pierde oportunidad, dealers buscan solución igual | ❌ No recomendado      |

### 🎯 **Recomendación Final**: Opción B → A (Híbrida)

1. **Fase 1 (MVP)**: Integrar con un proveedor certificado (Alegra API o similar) para ofrecer facturación básica en 1–2 meses.
2. **Fase 2**: Desarrollar sistema propio y certificarse como PSFE con la DGII (3–6 meses después).
3. **Fase 3**: Migrar usuarios al sistema propio, maximizando márgenes.

---

## 7. Resumen Ejecutivo

| Métrica                       | Valor                                     |
| ----------------------------- | ----------------------------------------- |
| **Inversión total estimada**  | USD $35,750–$55,500                       |
| **Costo operativo mensual**   | USD $491–$1,092                           |
| **Precio propuesto más bajo** | RD$599/mes (USD $9.99)                    |
| **Descuento vs competidores** | 40–70% menos que Alegra                   |
| **Break-even**                | 8–18 meses                                |
| **Ingreso proyectado Año 1**  | ~USD $54,800                              |
| **Certificación necesaria**   | PSFE (DGII)                               |
| **Tiempo a mercado (MVP)**    | 1–2 meses (con proveedor)                 |
| **¿Legalmente viable?**       | ✅ Sí, con certificación                  |
| **¿Recomendado?**             | ✅ **Sí — alta conveniencia estratégica** |

---

_Este análisis fue generado el 6 de marzo de 2026 y debe ser validado con un contador público autorizado (CPA) y un abogado tributarista en República Dominicana antes de tomar decisiones de implementación._
