# 📊 Análisis de Mercado: Sistema de Facturación y Contabilidad para OKLA

**Fecha:** 6 de marzo de 2026  
**Autor:** Equipo OKLA  
**Versión:** 1.0  
**Estado:** Borrador para revisión

---

## Índice

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Sistemas de Facturación Actuales en RD](#2-sistemas-de-facturación-actuales-en-rd)
3. [Análisis Comparativo de Precios](#3-análisis-comparativo-de-precios)
4. [Requisitos Legales y Regulatorios](#4-requisitos-legales-y-regulatorios)
5. [Módulos Necesarios del Sistema](#5-módulos-necesarios-del-sistema)
6. [Análisis de Costos y Rentabilidad](#6-análisis-de-costos-y-rentabilidad)
7. [Propuesta de Precios OKLA](#7-propuesta-de-precios-okla)
8. [Viabilidad Legal para OKLA](#8-viabilidad-legal-para-okla)
9. [Plan de Implementación](#9-plan-de-implementación)
10. [Conclusiones y Recomendaciones](#10-conclusiones-y-recomendaciones)

---

## 1. Resumen Ejecutivo

OKLA, como marketplace de vehículos en República Dominicana, tiene la oportunidad de integrar un **sistema de facturación electrónica y contabilidad básica** como valor agregado para sus dealers y vendedores. Este análisis evalúa la viabilidad técnica, legal y financiera de ofrecer este servicio.

### Hallazgos clave:

- ✅ La facturación electrónica (e-CF) es **obligatoria por Ley 32-23** para todos los contribuyentes en RD
- ✅ Los competidores cobran entre **US$29–US$129/mes** por sistemas de facturación cloud
- ✅ OKLA puede ofrecer un sistema **integrado a la plataforma** a precios significativamente menores
- ✅ La inversión se recuperaría en **8–12 meses** con solo 200+ suscriptores activos
- ⚠️ OKLA necesitaría **certificarse ante la DGII** como proveedor de servicios de facturación electrónica
- ⚠️ El desarrollo requiere inversión inicial estimada de **US$35,000–US$55,000**

---

## 2. Sistemas de Facturación Actuales en RD

### 2.1 Alegra (alegra.com) — ⭐ Líder en LATAM para PYMEs

**Origen:** Colombia, con presencia fuerte en RD  
**Tipo:** Cloud SaaS  
**Certificación DGII:** ✅ Proveedor autorizado de facturación electrónica

| Plan            | Precio/Mes (USD) |  Usuarios  | Límite de Ingresos/Mes (RD$) | e-CF | Contabilidad |
| --------------- | :--------------: | :--------: | :--------------------------: | :--: | :----------: |
| **Emprendedor** |      $29.00      |     1      |           125,000            |  ✅  |    Básica    |
| **PYME**        |      $59.00      |     2      |           500,000            |  ✅  |   Completa   |
| **PRO**         |      $89.00      |     3      |          1,250,000           |  ✅  |   Completa   |
| **PLUS**        |     $129.00      |     8      |          6,250,000           |  ✅  |   Avanzada   |
| **Premium**     |  Personalizado   | Ilimitados |          >6,250,000          |  ✅  |  Enterprise  |

**Funcionalidades incluidas:**

- Facturación electrónica e-CF (todos los tipos)
- Cotizaciones y notas de crédito/débito
- Conciliaciones bancarias
- Centro de costos
- Inventario básico
- Portal de clientes
- Reportes de ventas
- Integración API (desde plan PRO)
- Integración Zapier
- Multimoneda
- Roles de usuarios predefinidos
- 15 días de prueba gratis

---

### 2.2 Odoo (odoo.com) — ERP Full-stack

**Origen:** Bélgica, presencia global  
**Tipo:** Cloud SaaS / On-premise / Odoo.sh  
**Certificación DGII:** ⚠️ Requiere módulo de localización DR (comunidad/partners)

| Plan             |    Precio (USD)    |           Modelo           |                    Incluye                    |
| ---------------- | :----------------: | :------------------------: | :-------------------------------------------: |
| **One App Free** |         $0         | 1 app, usuarios ilimitados |        Solo facturación O contabilidad        |
| **Standard**     | $7.25/usuario/mes  |       Todas las apps       | Facturación + Contabilidad + CRM + Inventario |
| **Custom**       | $10.90/usuario/mes |  Todas las apps + Studio   |          API externa + Multi-empresa          |

**Nota para RD:** Odoo requiere localización dominicana (módulo comunitario `l10n_do`) para NCF/e-CF. No es plug-and-play para cumplimiento DGII sin configuración adicional por un partner.

**Costo real para una PYME en RD (3 usuarios, Standard):**

- $7.25 × 3 = **$21.75/mes** + costo de implementación del partner local (~$500–$2,000 setup)

---

### 2.3 QuickBooks Online (Intuit)

**Origen:** EE.UU.  
**Tipo:** Cloud SaaS  
**Certificación DGII:** ❌ No tiene integración nativa con e-CF de RD

| Plan             | Precio/Mes (USD) | Usuarios |               Notas                |
| ---------------- | :--------------: | :------: | :--------------------------------: |
| **Simple Start** |      $30.00      |    1     |   Facturación e informes básicos   |
| **Essentials**   |      $60.00      |    3     | + Cuentas por pagar, multi-moneda  |
| **Plus**         |      $90.00      |    5     |      + Inventario, proyectos       |
| **Advanced**     |     $200.00      |    25    | + Automatización, soporte dedicado |

**Limitación crítica:** QuickBooks **no soporta e-CF ni NCF** de manera nativa. Los negocios en RD que lo usan necesitan un sistema paralelo para cumplir con la DGII, lo que duplica costos.

---

### 2.4 ContaPyme

**Origen:** Colombia  
**Tipo:** Desktop / Cloud (híbrido)  
**Certificación DGII:** ⚠️ Soporte limitado para RD

| Plan            | Precio/Mes (USD) |           Notas            |
| --------------- | :--------------: | :------------------------: |
| **Básico**      |     ~$15–$25     |    Contabilidad básica     |
| **Profesional** |     ~$35–$50     | Facturación + Contabilidad |
| **Empresarial** |    ~$60–$100     |       Multi-empresa        |

**Limitación:** Enfocado principalmente en Colombia. Adaptación para RD requiere configuración manual. No es proveedor DGII certificado.

---

### 2.5 Bind ERP (bind.com.mx)

**Origen:** México  
**Tipo:** Cloud SaaS  
**Certificación DGII:** ❌ Solo México (CFDI)

| Plan            | Precio/Mes (USD) |             Notas             |
| --------------- | :--------------: | :---------------------------: |
| **Emprendedor** |       ~$40       | 1 usuario, facturación básica |
| **PyME**        |       ~$75       |          3 usuarios           |
| **Profesional** |      ~$120       |    5 usuarios, inventario     |

**No aplica para RD:** Diseñado para el mercado mexicano (CFDI/SAT). No soporta NCF ni e-CF dominicanos.

---

### 2.6 Facturero (facturero.com) — Solución Local RD

**Origen:** República Dominicana  
**Tipo:** Cloud SaaS  
**Certificación DGII:** ✅ Proveedor autorizado

| Plan         | Precio Estimado/Mes (USD) |            Notas             |
| ------------ | :-----------------------: | :--------------------------: |
| **Básico**   |         ~$15–$20          |    e-CF básico, 1 usuario    |
| **Estándar** |         ~$30–$45          | Múltiples usuarios, reportes |
| **Premium**  |         ~$60–$80          |       Completo con API       |

**Ventaja:** Diseñado 100% para el mercado dominicano. Cumple con todos los requisitos DGII desde el inicio.

---

### 2.7 Facturador Gratuito DGII

**Origen:** DGII (gobierno)  
**Tipo:** Herramienta web gratuita  
**Costo:** **$0 (gratuito)**

**Requisitos para uso:**

- Estar inscrito en el RNC
- Tener clave de acceso a la Oficina Virtual (OFV)
- Tener autorización para emitir NCF
- Estar al día con obligaciones tributarias
- Poseer certificado digital para procedimientos tributarios
- Disponer de computador/dispositivo con internet
- **NO haber sido autorizado** a emitir e-CF por otra vía (Ley 32-23, Art. 12)

**Limitaciones:**

- ❌ Sin contabilidad
- ❌ Sin gestión de inventario
- ❌ Sin reportes avanzados
- ❌ Sin API para integración
- ❌ Interfaz básica, no optimizada para alto volumen
- ❌ Sin soporte prioritario
- Diseñado para profesionales liberales, personas físicas y MiPYMEs sin sistema propio

---

### 2.8 Otras Soluciones Locales en RD

| Sistema                   |   Tipo    | Precio Est./Mes | DGII Cert. |            Notas             |
| ------------------------- | :-------: | :-------------: | :--------: | :--------------------------: |
| **Softland**              | ERP Cloud |    $80–$300+    |     ✅     |  Empresas medianas-grandes   |
| **SAP Business One**      |    ERP    |   $150–$500+    |     ✅     |       Empresas grandes       |
| **Defontana**             |   Cloud   |     $30–$70     |     ⚠️     |    Chile, expansión LATAM    |
| **Nubox**                 |   Cloud   |     $20–$50     |     ⚠️     |    Chile, limitado en RD     |
| **Soluciones locales DR** |  Varios   |     $10–$50     |     ✅     | Pequeños proveedores locales |

---

## 3. Análisis Comparativo de Precios

### 3.1 Tabla Resumen de Precios del Mercado

| Sistema           | Plan Básico |   Plan Medio    |  Plan Avanzado  |       e-CF RD       |       Nota       |
| ----------------- | :---------: | :-------------: | :-------------: | :-----------------: | :--------------: |
| **Alegra**        |   $29/mes   |     $59/mes     |  $89–$129/mes   |         ✅          |   Líder LATAM    |
| **Odoo**          | $0 (1 app)  | $22/mes (3 usr) | $33/mes (3 usr) | ⚠️ Requiere partner |   ERP completo   |
| **QuickBooks**    |   $30/mes   |     $60/mes     |  $90–$200/mes   |         ❌          |  No soporta RD   |
| **ContaPyme**     | $15–$25/mes |   $35–$50/mes   |  $60–$100/mes   |         ⚠️          |     Limitado     |
| **Bind ERP**      |   $40/mes   |     $75/mes     |    $120/mes     |         ❌          |   Solo México    |
| **Facturero**     | $15–$20/mes |   $30–$45/mes   |   $60–$80/mes   |         ✅          |     100% RD      |
| **DGII Gratuito** |     $0      |       N/A       |       N/A       |         ✅          |   Muy limitado   |
| **Softland**      |   $80/mes   |    $150/mes     |    $300+/mes    |         ✅          | Empresas grandes |

### 3.2 Precio Promedio del Mercado para PYMEs en RD

- **Plan básico (1–2 usuarios):** US$20–$35/mes
- **Plan medio (2–5 usuarios):** US$45–$75/mes
- **Plan avanzado (5+ usuarios):** US$80–$150/mes

### 3.3 Competidor Directo Principal: Alegra

Alegra es el competidor más relevante porque:

1. Es 100% cloud
2. Está certificado por la DGII
3. Tiene presencia activa en RD
4. Su rango de precios ($29–$129/mes) es el benchmark
5. Está promoviendo activamente la adopción de e-CF (su sitio muestra un countdown para la obligatoriedad)

---

## 4. Requisitos Legales y Regulatorios

### 4.1 Marco Legal Principal

#### Ley 32-23 — Ley de Facturación Electrónica

- **Obligatoriedad:** Todos los contribuyentes deben implementar facturación electrónica según calendario establecido por la DGII
- **Fecha límite:** La DGII ha establecido plazos progresivos. A marzo 2026, la mayoría de contribuyentes ya están obligados o en proceso
- **Sanciones:** Multas y penalidades por incumplimiento

#### Ley 11-92 — Código Tributario de RD

- Regula toda la actividad tributaria en República Dominicana
- Define obligaciones de facturación y documentación fiscal
- Establece el marco para comprobantes fiscales

#### Ley 253-12 — Ley de Reforma Tributaria

- Fortaleció el sistema de comprobantes fiscales
- Amplió requisitos de documentación tributaria

#### Norma General 06-2018 — Sobre Comprobantes Fiscales

- Regula la emisión, uso y control de comprobantes fiscales
- Define los tipos de NCF y sus usos específicos
- Establece requisitos para emisores

### 4.2 Comprobantes Fiscales Electrónicos (e-CF)

#### Definición

El e-CF es un comprobante fiscal generado electrónicamente que acredita la transferencia de bienes, la entrega en uso o la prestación de servicios. Debe cumplir con los requisitos de la Ley 32-23 y normas complementarias.

#### Estructura del e-NCF

El Número de Comprobante Fiscal Electrónico (e-NCF) tiene 13 caracteres:

- **1 letra:** "E" (indica serie electrónica)
- **2 dígitos:** Tipo de comprobante
- **10 dígitos:** Secuencial

Ejemplo: `E310000000001`

#### Tipos de e-CF Obligatorios

| Tipo                          | Código |                   Descripción                    |            Uso Principal             |
| ----------------------------- | :----: | :----------------------------------------------: | :----------------------------------: |
| **Factura de Crédito Fiscal** |   31   |                Transacciones B2B                 |     ✅ **Esencial para dealers**     |
| **Factura de Consumo**        |   32   |          Ventas a consumidores finales           | ✅ **Esencial para ventas directas** |
| **Nota de Débito**            |   33   | Recuperar costos posteriores (intereses, fletes) |             ✅ Necesario             |
| **Nota de Crédito**           |   34   |      Anulaciones, devoluciones, descuentos       |             ✅ Necesario             |
| **Comprobante de Compras**    |   41   |           Compras a no contribuyentes            |               ⚠️ Útil                |
| **Gastos Menores**            |   43   |   Gastos de personal (consumibles, transporte)   |               ⚠️ Útil                |
| **Regímenes Especiales**      |   44   |             Ventas exentas de ITBIS              |           ⚠️ Según aplique           |
| **Gubernamental**             |   45   |                Ventas al gobierno                |           ⚠️ Según aplique           |
| **Exportaciones**             |   46   |           Ventas fuera del territorio            |             ❌ No aplica             |
| **Pagos al Exterior**         |   47   |          Pagos a no residentes fiscales          |           ⚠️ Según aplique           |

#### Tipos Prioritarios para OKLA

Para el marketplace de vehículos, los tipos **obligatorios** a implementar son:

1. **Tipo 31 — Factura de Crédito Fiscal** (ventas entre dealers/empresas)
2. **Tipo 32 — Factura de Consumo** (ventas a personas finales)
3. **Tipo 33 — Nota de Débito** (ajustes posteriores)
4. **Tipo 34 — Nota de Crédito** (anulaciones, devoluciones)

### 4.3 ITBIS (Impuesto a la Transferencia de Bienes y Servicios)

| Concepto               | Tasa |                         Notas                          |
| ---------------------- | :--: | :----------------------------------------------------: |
| **Tasa general ITBIS** | 18%  |         Aplica a mayoría de bienes y servicios         |
| **Tasa reducida**      | 16%  |       Algunos alimentos y productos específicos        |
| **Exento**             |  0%  | Vehículos usados entre particulares (en ciertos casos) |

**Consideraciones para venta de vehículos:**

- La venta de vehículos nuevos está gravada con 18% ITBIS
- Los vehículos usados pueden tener tratamientos especiales según el tipo de vendedor
- Los dealers (personas jurídicas) deben cobrar y reportar ITBIS
- Los vendedores individuales (personas físicas) pueden estar exentos en ciertos casos

### 4.4 Obligaciones de Reporte a la DGII

| Reporte  | Frecuencia |                         Descripción                         |
| -------- | :--------: | :---------------------------------------------------------: |
| **IT-1** |  Mensual   |                 Declaración jurada de ITBIS                 |
| **IR-1** |   Anual    |  Declaración de Impuesto sobre la Renta (Personas Físicas)  |
| **IR-2** |   Anual    | Declaración de Impuesto sobre la Renta (Personas Jurídicas) |
| **606**  |  Mensual   |                  Envío de compras y gastos                  |
| **607**  |  Mensual   |                       Envío de ventas                       |
| **608**  |  Mensual   |                    Comprobantes anulados                    |
| **609**  |  Mensual   |                      Pagos al exterior                      |
| **623**  |  Mensual   |                   Retenciones (ITBIS/ISR)                   |

### 4.5 Requisitos para ser Emisor Electrónico

Para que los dealers/vendedores puedan emitir e-CF:

1. Estar inscrito en el **RNC** (Registro Nacional de Contribuyentes)
2. Tener obligaciones tributarias activas
3. Poseer un **certificado digital** para procedimientos tributarios (emitido por entidad autorizada por INDOTEL)
4. Cumplir con las exigencias técnicas de la DGII
5. Completar solicitud de autorización vía **Oficina Virtual (OFV)**
6. Aprobar proceso de **certificación** ante la DGII
7. Cumplir con Norma General 06-2018

### 4.6 Requisitos para ser Proveedor de Servicios de Facturación Electrónica (PSFE)

**⚠️ CRÍTICO PARA OKLA — Si quiere ser proveedor debe cumplir:**

1. Ser emisor electrónico activo
2. Tener al menos **3 contribuyentes certificados** como emisores electrónicos de manera exitosa
3. Tener registrada en el RNC la **actividad económica de venta y/o desarrollo de aplicaciones informáticas**
4. Estar al día en **obligaciones tributarias y deberes formales**
5. Descargar y completar el **Formulario FI-GDF-017** (Solicitud de Autorización)
6. Presentar documentación requerida (presencial y virtual)
7. Pasar proceso de **certificación técnica** ante la DGII

---

## 5. Módulos Necesarios del Sistema

### 5.1 Módulos Core (Imprescindibles)

#### 📄 Módulo 1: Facturación Electrónica (e-CF)

- Emisión de todos los tipos de e-CF (31, 32, 33, 34, 41, 43)
- Generación de e-NCF con estructura válida
- Firma digital con certificado del contribuyente
- Envío automático a la DGII vía API
- Validación de respuesta DGII (aprobación/rechazo)
- Representación impresa del e-CF (PDF)
- Envío por email al receptor
- Historial y consulta de comprobantes emitidos
- Anulación de comprobantes
- Gestión de secuencias por tipo de comprobante

#### 💰 Módulo 2: Gestión de Cuentas por Cobrar

- Registro de facturas pendientes de cobro
- Seguimiento de pagos parciales
- Alertas de vencimiento
- Estados de cuenta por cliente
- Aging report (antigüedad de saldas)
- Gestión de cobros

#### 💸 Módulo 3: Gestión de Cuentas por Pagar

- Registro de gastos y compras
- Seguimiento de pagos a proveedores
- Calendario de pagos
- Control de flujo de caja

#### 📊 Módulo 4: Reportes DGII

- Generación automática del formato 606 (compras y gastos)
- Generación automática del formato 607 (ventas)
- Generación automática del formato 608 (comprobantes anulados)
- Generación del formato 609 (pagos al exterior)
- Formato 623 (retenciones)
- Pre-llenado del IT-1 (declaración ITBIS)
- Exportación en formatos requeridos por la DGII (.txt, .csv)
- Dashboard de impuestos estimados

#### 🧮 Módulo 5: Cálculo de Impuestos

- Cálculo automático de ITBIS (18%)
- Manejo de tasas reducidas y exenciones
- Cálculo de retenciones (ISR, ITBIS)
- Acumulado de crédito fiscal
- Estimación de ISR mensual/anual

#### 👥 Módulo 6: Gestión de Contactos (Clientes/Proveedores)

- Directorio de clientes con RNC/Cédula
- Validación de RNC contra DGII
- Directorio de proveedores
- Historial de transacciones por contacto
- Clasificación de tipos de contribuyente

### 5.2 Módulos Complementarios (Valor Agregado)

#### 📦 Módulo 7: Inventario Básico de Vehículos

- Registro de vehículos en stock
- Costo de adquisición
- Precio de venta
- Estado del vehículo (disponible, reservado, vendido)
- Vinculación con publicaciones de OKLA
- Cálculo automático de margen de ganancia

#### 💵 Módulo 8: Gestión de Pagos y Métodos de Pago

- Registro de pagos recibidos
- Múltiples métodos de pago (efectivo, transferencia, cheque, tarjeta)
- Conciliación de pagos con facturas
- Recibos de pago digitales

#### 📈 Módulo 9: Reportes y Dashboard

- Dashboard financiero en tiempo real
- Ventas por período (diario, semanal, mensual, anual)
- Top clientes
- Comparativo de períodos
- Gráficos de flujo de caja
- Exportación a Excel/PDF
- Margen de ganancia por vehículo

#### 🏦 Módulo 10: Conciliación Bancaria

- Importación de estados de cuenta bancarios
- Conciliación automática/manual
- Diferencias y partidas pendientes

#### 📋 Módulo 11: Cotizaciones y Proformas

- Creación de cotizaciones formales
- Conversión de cotización a factura
- Seguimiento de cotizaciones (pendiente, aceptada, rechazada)
- Plantillas personalizables

#### 🔔 Módulo 12: Notificaciones y Alertas

- Alerta de facturas por vencer
- Recordatorio de pagos pendientes
- Notificación de fechas límite DGII
- Alertas de errores en envío de e-CF

### 5.3 Módulos Avanzados (Fase 2)

#### 📚 Módulo 13: Contabilidad General

- Plan de cuentas configurable (según normas DR)
- Asientos contables automáticos
- Libro diario y mayor
- Balance general
- Estado de resultados
- Cierre de período contable

#### 🧾 Módulo 14: Gestión de Retenciones

- Retención de ITBIS (30% o 100% según caso)
- Retención de ISR (según tabla de retenciones DGII)
- Generación de comprobantes de retención
- Reporte 623

#### 🔗 Módulo 15: Integraciones

- API REST para integración con otros sistemas
- Integración con DGII (envío/consulta de e-CF)
- Integración con bancos dominicanos (futuro)
- Webhooks para eventos de facturación
- Integración nativa con marketplace OKLA

---

## 6. Análisis de Costos y Rentabilidad

### 6.1 Costos de Desarrollo

| Concepto                            |   Estimado (USD)    |                Notas                |
| ----------------------------------- | :-----------------: | :---------------------------------: |
| **Módulos Core (1-6)**              |   $20,000–$30,000   |       3-4 meses de desarrollo       |
| **Módulos Complementarios (7-12)**  |   $10,000–$15,000   |        2-3 meses adicionales        |
| **Módulos Avanzados (13-15)**       |   $8,000–$12,000    |           Fase 2, 2 meses           |
| **Integración con DGII (API e-CF)** |    $5,000–$8,000    |  Firma digital, envío, validación   |
| **Certificación DGII como PSFE**    |    $3,000–$5,000    |   Proceso, pruebas, documentación   |
| **Certificado Digital**             |    $100–$300/año    |   Por entidad autorizada INDOTEL    |
| **QA y Testing**                    |    $3,000–$5,000    | Testing integral y de certificación |
| **Diseño UX/UI**                    |    $3,000–$5,000    |       Interfaz usuario final        |
| **TOTAL DESARROLLO**                | **$35,000–$55,000** |       **Fase 1 + 2 completa**       |

**Nota:** Gran parte del desarrollo se puede realizar internamente aprovechando la arquitectura de microservicios existente de OKLA (.NET 8, PostgreSQL, RabbitMQ). Esto reduce significativamente los costos comparado con un desarrollo desde cero.

### 6.2 Costos de Infraestructura (Mensual)

| Concepto                             | Costo/Mes (USD)  |                Notas                |
| ------------------------------------ | :--------------: | :---------------------------------: |
| **Kubernetes (nodo adicional DOKS)** |     $24–$48      |  1 droplet adicional si necesario   |
| **PostgreSQL (DB adicional)**        |     $15–$30      |       Managed DB o in-cluster       |
| **Almacenamiento (PDFs, backups)**   |      $5–$15      |        Digital Ocean Spaces         |
| **Certificado Digital (amortizado)** |      $8–$25      |            ~$100-300/año            |
| **Monitoreo y logs**                 |        $0        | Ya incluido en infraestructura OKLA |
| **RabbitMQ (tráfico adicional)**     |        $0        |             Ya incluido             |
| **TOTAL INFRAESTRUCTURA**            | **$52–$118/mes** |                                     |

### 6.3 Costos Operativos (Mensual)

| Concepto                            |   Costo/Mes (USD)   |                Notas                |
| ----------------------------------- | :-----------------: | :---------------------------------: |
| **Soporte técnico**                 |      $300–$600      | Parcial, compartido con equipo OKLA |
| **Mantenimiento y actualizaciones** |      $200–$400      |     Actualizaciones DGII, bugs      |
| **Renovaciones de certificados**    |       $10–$25       |         Amortizado mensual          |
| **TOTAL OPERATIVO**                 | **$510–$1,025/mes** |                                     |

### 6.4 Costo Total Mensual de Operación

| Concepto        |    Mínimo    |     Máximo     |
| --------------- | :----------: | :------------: |
| Infraestructura |     $52      |      $118      |
| Operativo       |     $510     |     $1,025     |
| **TOTAL**       | **$562/mes** | **$1,143/mes** |

### 6.5 Análisis de Break-Even (Punto de Equilibrio)

#### Escenario con precios OKLA propuestos:

| Métrica                            |  Escenario Conservador  |  Escenario Optimista   |
| ---------------------------------- | :---------------------: | :--------------------: |
| **Precio promedio plan**           |         $15/mes         |        $20/mes         |
| **Costo operativo mensual**        |         $1,143          |          $562          |
| **Suscriptores para break-even**   |           77            |           29           |
| **Inversión desarrollo**           |         $55,000         |        $35,000         |
| **Meses para recuperar inversión** | 12 meses (con 100 subs) | 8 meses (con 100 subs) |

#### Proyección de Ingresos (12 meses)

| Mes | Suscriptores | Ingreso Mensual | Costo Mensual | Ganancia Neta | Acumulado |
| --- | :----------: | :-------------: | :-----------: | :-----------: | :-------: |
| 1   |      30      |      $450       |     $850      |     -$400     |   -$400   |
| 2   |      50      |      $750       |     $850      |     -$100     |   -$500   |
| 3   |      80      |     $1,200      |     $850      |     $350      |   -$150   |
| 4   |     110      |     $1,650      |     $900      |     $750      |   $600    |
| 5   |     140      |     $2,100      |     $900      |    $1,200     |  $1,800   |
| 6   |     170      |     $2,550      |     $950      |    $1,600     |  $3,400   |
| 7   |     200      |     $3,000      |     $950      |    $2,050     |  $5,450   |
| 8   |     230      |     $3,450      |    $1,000     |    $2,450     |  $7,900   |
| 9   |     260      |     $3,900      |    $1,000     |    $2,900     |  $10,800  |
| 10  |     290      |     $4,350      |    $1,050     |    $3,300     |  $14,100  |
| 11  |     320      |     $4,800      |    $1,050     |    $3,750     |  $17,850  |
| 12  |     350      |     $5,250      |    $1,100     |    $4,150     |  $22,000  |

**Resultado año 1:** Con inversión de $45,000 y crecimiento de ~30 suscriptores/mes:

- **Ingreso anual:** ~$33,450
- **Costos operativos anuales:** ~$11,400
- **Ganancia operativa anual:** ~$22,000
- **ROI primer año:** ~49% (sin contar inversión inicial)
- **Recuperación total inversión:** ~16–18 meses

---

## 7. Propuesta de Precios OKLA

### 7.1 Estrategia de Precios: Penetración por debajo del mercado

OKLA tiene una **ventaja competitiva única**: ya posee la base de clientes (dealers y vendedores). El sistema de facturación se ofrece como **valor agregado integrado** al marketplace, no como producto independiente.

### 7.2 Planes Propuestos

#### 🆓 Plan Gratis — Incluido en Suscripción OKLA

**Precio:** $0 (incluido en cualquier suscripción OKLA activa)

| Característica                |   Incluido   |
| ----------------------------- | :----------: |
| Facturas de consumo (Tipo 32) | Hasta 10/mes |
| Notas de crédito/débito       | Hasta 5/mes  |
| Reportes básicos              |      ✅      |
| 1 Usuario                     |      ✅      |
| Dashboard básico              |      ✅      |

**Objetivo:** Atraer usuarios al ecosistema, crear dependencia del servicio.

---

#### 💼 Plan Vendedor — Para Sellers Individuales

**Precio:** US$9.99/mes (o US$99/año = 17% descuento)

| Característica                           | Incluido |
| ---------------------------------------- | :------: |
| Todos los tipos de e-CF (31, 32, 33, 34) |    ✅    |
| Facturas ilimitadas                      |    ✅    |
| Cuentas por cobrar                       |    ✅    |
| Reportes DGII (606, 607, 608)            |    ✅    |
| Cálculo automático ITBIS                 |    ✅    |
| Gestión de contactos                     |    ✅    |
| Cotizaciones                             |    ✅    |
| 2 Usuarios                               |    ✅    |
| Soporte por chat                         |    ✅    |

**Vs. Competencia:**

- Alegra Emprendedor: $29/mes → **OKLA ahorra 66%**
- Facturero Básico: ~$18/mes → **OKLA ahorra 44%**

---

#### 🏢 Plan Dealer Básico — Para Dealers Pequeños

**Precio:** US$19.99/mes (o US$199/año = 17% descuento)

| Característica                 | Incluido |
| ------------------------------ | :------: |
| Todo del Plan Vendedor         |    ✅    |
| Inventario de vehículos        |    ✅    |
| Cuentas por pagar              |    ✅    |
| Conciliación bancaria básica   |    ✅    |
| Reportes avanzados + Dashboard |    ✅    |
| Retenciones (ISR, ITBIS)       |    ✅    |
| 5 Usuarios                     |    ✅    |
| Soporte prioritario            |    ✅    |

**Vs. Competencia:**

- Alegra PYME: $59/mes → **OKLA ahorra 66%**
- Odoo Standard (3 usuarios): ~$22/mes → **OKLA es comparable pero integrado**

---

#### 🏆 Plan Dealer Pro — Para Dealers Medianos/Grandes

**Precio:** US$39.99/mes (o US$399/año = 17% descuento)

| Característica                       | Incluido |
| ------------------------------------ | :------: |
| Todo del Plan Dealer Básico          |    ✅    |
| Contabilidad general completa        |    ✅    |
| Multi-sucursal                       |    ✅    |
| API de integración                   |    ✅    |
| Reportes personalizados              |    ✅    |
| Usuarios ilimitados                  |    ✅    |
| Soporte dedicado (WhatsApp/teléfono) |    ✅    |
| Exportación avanzada (Excel, PDF)    |    ✅    |

**Vs. Competencia:**

- Alegra PRO: $89/mes → **OKLA ahorra 55%**
- Alegra PLUS: $129/mes → **OKLA ahorra 69%**

---

### 7.3 Tabla Comparativa OKLA vs. Competencia

| Funcionalidad           | OKLA Vendedor ($10) | OKLA Dealer ($20) | OKLA Pro ($40) | Alegra Emprend. ($29) | Alegra PYME ($59) | Alegra PRO ($89) |
| ----------------------- | :-----------------: | :---------------: | :------------: | :-------------------: | :---------------: | :--------------: |
| e-CF completo           |         ✅          |        ✅         |       ✅       |          ✅           |        ✅         |        ✅        |
| Facturas ilimitadas     |         ✅          |        ✅         |       ✅       |    Límite ingresos    |  Límite ingresos  | Límite ingresos  |
| Reportes DGII           |         ✅          |        ✅         |       ✅       |          ✅           |        ✅         |        ✅        |
| Inventario vehículos    |         ❌          |        ✅         |       ✅       |       Genérico        |     Genérico      |     Genérico     |
| Contabilidad            |       Básica        |       Media       |    Completa    |        Básica         |     Completa      |     Completa     |
| Integración marketplace |         ✅          |        ✅         |       ✅       |          ❌           |        ❌         |        ❌        |
| Usuarios                |          2          |         5         |   Ilimitados   |           1           |         2         |        3         |
| API                     |         ❌          |        ❌         |       ✅       |          ❌           |        ❌         |        ✅        |

### 7.4 Integración con Planes de Suscripción Existentes

Los planes de facturación se pueden **bundlear** con las suscripciones existentes de OKLA:

| Suscripción OKLA Actual | Precio Actual |    Con Facturación Bundled    |       Precio Nuevo        |
| ----------------------- | :-----------: | :---------------------------: | :-----------------------: |
| **Seller Individual**   |  $29/listing  |    + Plan Gratis incluido     |        $29/listing        |
| **Dealer Starter**      |    $49/mes    |   + Plan Vendedor incluido    | $49/mes (valor agregado)  |
| **Dealer Professional** |   $149/mes    | + Plan Dealer Básico incluido | $149/mes (valor agregado) |
| **Dealer Enterprise**   |   $299/mes    |  + Plan Dealer Pro incluido   | $299/mes (valor agregado) |

**Estrategia:** Para los planes de dealer de $149+ y $299+, incluir facturación como beneficio incluido (costo absorbido por la suscripción), aumentando la retención y el valor percibido. Los sellers y dealers más pequeños pueden comprar el módulo por separado.

---

## 8. Viabilidad Legal para OKLA

### 8.1 ¿Puede OKLA ofrecer legalmente servicios de facturación?

**Sí, pero con condiciones:**

#### Opción A: OKLA como Proveedor de Servicios de Facturación Electrónica (PSFE)

- ✅ **Legalmente posible** — La Ley 32-23 permite que empresas se certifiquen como PSFE
- ⚠️ **Requiere certificación DGII** — Proceso formal con requisitos técnicos
- ⚠️ **Requiere actividad económica registrada** en desarrollo de software/aplicaciones informáticas
- ⚠️ **Requiere tener 3 emisores certificados previamente** (se pueden obtener en fase piloto)

#### Opción B: OKLA como facilitador (usando PSFE certificado tercero)

- ✅ **Más rápido de implementar** — Usar API de un PSFE ya certificado
- ✅ **Sin necesidad de certificación propia** ante DGII
- ❌ **Dependencia de tercero** — Menos control, comisiones adicionales
- ❌ **Menor margen de ganancia**

#### Opción C: Modelo híbrido (recomendada)

- **Fase 1:** Iniciar usando API de un PSFE certificado (3-6 meses)
- **Fase 2:** Certificarse como PSFE propio ante la DGII
- **Fase 3:** Migrar usuarios a infraestructura propia
- ✅ **Time-to-market rápido** + independencia a largo plazo

### 8.2 Requisitos Legales para OKLA

| Requisito                    |  Estado Actual  |                Acción Necesaria                 |
| ---------------------------- | :-------------: | :---------------------------------------------: |
| RNC activo                   |       ✅        |             Ya existe como empresa              |
| Actividad económica software |  ⚠️ Verificar   | Agregar actividad económica al RNC si no existe |
| Certificado digital          |   ⚠️ Obtener    |   Solicitar a entidad autorizada por INDOTEL    |
| Emisor electrónico propio    | ⚠️ Certificarse |     Completar proceso de certificación DGII     |
| 3 emisores certificados      |   ⚠️ Obtener    |           Certificar 3 dealers piloto           |
| Cumplimiento tributario      |  ✅ Verificar   |           Debe estar al día con DGII            |
| Formulario FI-GDF-017        |  🔲 Pendiente   |         Completar y presentar solicitud         |

### 8.3 ¿Es la facturación una actividad regulada?

**Sí**, la facturación electrónica en RD es una **actividad regulada** por la DGII:

1. **No es una actividad financiera** — No requiere licencia del Banco Central ni de la SIB (Superintendencia de Bancos)
2. **Es una actividad fiscal/tributaria** — Regulada exclusivamente por la DGII
3. **Los PSFE son supervisados** por la DGII — Se publica la lista de proveedores autorizados
4. **La contabilidad auxiliar** (sin ser firma de contadores) es permitida — OKLA ofrecería herramientas contables, no servicios de contaduría profesional

### 8.4 Riesgos Legales

| Riesgo                                     | Probabilidad | Impacto  |                                           Mitigación                                            |
| ------------------------------------------ | :----------: | :------: | :---------------------------------------------------------------------------------------------: |
| Rechazo de certificación DGII              |     Baja     |   Alto   |                Cumplir todos los requisitos técnicos; contratar consultor fiscal                |
| Cambios regulatorios en e-CF               |    Media     |  Medio   |                    Mantener sistema actualizable; monitorear normativas DGII                    |
| Responsabilidad por errores en facturación |    Media     |   Alto   | Disclaimer claro: OKLA es herramienta, no contador; usuario es responsable de sus declaraciones |
| Competidores certificados impugnen         |     Baja     |   Bajo   |                            Proceso de certificación DGII es abierto                             |
| Hackeo/filtración de datos fiscales        |     Baja     | Muy Alto |             Encriptación, auditoría, cumplimiento Ley 172-13 (protección de datos)              |

### 8.5 ¿Es Beneficioso Legalmente para OKLA?

**✅ SÍ, es muy beneficioso por varias razones:**

1. **Lock-in del cliente:** Dealers que usan facturación OKLA tienen mayor costo de cambio → mayor retención
2. **Data valiosa:** Acceso a datos de transacciones reales del mercado automotriz en RD
3. **Cumplimiento facilitado:** Ayudar a los dealers a cumplir con la Ley 32-23 genera confianza y lealtad
4. **Diferenciación:** Ningún marketplace de vehículos en RD ofrece facturación integrada
5. **Ingresos recurrentes:** Línea de ingresos adicional con bajo costo marginal por suscriptor
6. **Posicionamiento:** Posiciona a OKLA como plataforma integral, no solo clasificados

---

## 9. Plan de Implementación

### 9.1 Cronograma Propuesto

```
Fase 0: Preparación Legal (Semanas 1-4)
├── Verificar/actualizar actividad económica en RNC
├── Obtener certificado digital INDOTEL
├── Contratar consultor fiscal/tributario
└── Iniciar proceso de certificación como emisor electrónico

Fase 1: MVP - Facturación Básica (Semanas 5-16)
├── Nuevo microservicio: BillingService
│   ├── BillingService.Api
│   ├── BillingService.Application
│   ├── BillingService.Domain
│   └── BillingService.Infrastructure
├── Módulo 1: Facturación e-CF (tipos 31, 32, 33, 34)
├── Módulo 5: Cálculo ITBIS
├── Módulo 6: Gestión de contactos
├── Integración con PSFE certificado (API tercero)
├── UI básica en frontend (Next.js)
└── Testing + certificación DGII con 3 pilotos

Fase 2: Funcionalidades Completas (Semanas 17-28)
├── Módulo 2: Cuentas por cobrar
├── Módulo 3: Cuentas por pagar
├── Módulo 4: Reportes DGII (606, 607, 608)
├── Módulo 7: Inventario de vehículos
├── Módulo 8: Gestión de pagos
├── Módulo 9: Dashboard y reportes
├── Módulo 11: Cotizaciones
├── Módulo 12: Notificaciones
└── Certificación como PSFE ante DGII

Fase 3: Avanzado (Semanas 29-40)
├── Módulo 10: Conciliación bancaria
├── Módulo 13: Contabilidad general
├── Módulo 14: Retenciones
├── Módulo 15: API para integraciones
├── Migración a infraestructura propia de e-CF
└── Optimización y escalabilidad
```

### 9.2 Arquitectura Técnica (Integración con OKLA)

```
┌──────────────────────────────────────────────────────────┐
│                    Frontend (Next.js)                      │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────┐  │
│  │  Dashboard   │  │  Facturación │  │   Reportes      │  │
│  │  Financiero  │  │  e-CF        │  │   DGII          │  │
│  └─────────────┘  └──────────────┘  └─────────────────┘  │
└──────────────────────┬────────────────────────────────────┘
                       │ BFF (/api/*)
┌──────────────────────▼────────────────────────────────────┐
│                   Gateway (Ocelot)                         │
└──────────────────────┬────────────────────────────────────┘
                       │ :8080
┌──────────────────────▼────────────────────────────────────┐
│              BillingService (.NET 8)                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │ Invoicing│  │ Payments │  │ Reports  │  │ Accounts │ │
│  │ (e-CF)   │  │          │  │ (DGII)   │  │          │ │
│  └────┬─────┘  └──────────┘  └──────────┘  └──────────┘ │
│       │                                                    │
│  ┌────▼──────────────────────────────────────────────┐   │
│  │         DGII Integration Layer                      │   │
│  │  (Firma Digital + API DGII + Validación)           │   │
│  └────────────────────────────────────────────────────┘   │
└───────────┬──────────────────────┬────────────────────────┘
            │                      │
    ┌───────▼───────┐      ┌──────▼──────┐
    │  PostgreSQL   │      │  RabbitMQ   │
    │  (billing_db) │      │  (events)   │
    └───────────────┘      └─────────────┘
```

---

## 10. Conclusiones y Recomendaciones

### 10.1 Resumen de Viabilidad

| Criterio                  | Evaluación |                        Detalle                        |
| ------------------------- | :--------: | :---------------------------------------------------: |
| **Viabilidad técnica**    |  ✅ Alta   | Se integra naturalmente con la arquitectura existente |
| **Viabilidad legal**      |  ✅ Alta   |    Con certificación DGII, es completamente legal     |
| **Viabilidad financiera** |  ✅ Alta   |          Break-even alcanzable en 8-18 meses          |
| **Demanda de mercado**    |  ✅ Alta   |       Ley 32-23 obliga a dealers a adoptar e-CF       |
| **Ventaja competitiva**   |  ✅ Alta   |      Único marketplace con facturación integrada      |
| **Riesgo**                |  ⚠️ Medio  |       Depende de certificación DGII y adopción        |

### 10.2 Recomendaciones

1. **PROCEDER con el desarrollo** — El análisis muestra viabilidad en todas las dimensiones
2. **Iniciar con Opción C (híbrida)** — Usar PSFE tercero para velocidad, luego certificarse
3. **Priorizar el módulo de e-CF** — Es obligatorio por ley, genera urgencia de adopción
4. **Incluir facturación gratis en planes dealer premium** — Aumenta retención sin costo adicional significativo
5. **Contratar consultor fiscal** — Para el proceso de certificación DGII (inversión ~$1,000-$2,000)
6. **Aprovechar el countdown** — Alegra muestra que quedan ~69 días para que la factura en papel deje de tener validez fiscal. Esto es una ventana de oportunidad ENORME
7. **No reinventar la rueda** — Usar la arquitectura Clean Architecture + CQRS existente de OKLA para el BillingService

### 10.3 Próximos Pasos Inmediatos

- [ ] Verificar actividad económica de OKLA en el RNC
- [ ] Obtener certificado digital de entidad autorizada por INDOTEL
- [ ] Contactar consultor fiscal para proceso de certificación DGII
- [ ] Definir PSFE tercero para integración inicial (evaluar Alegra API, Facturero API)
- [ ] Crear el microservicio `BillingService` siguiendo la arquitectura OKLA
- [ ] Diseñar UI/UX del módulo de facturación
- [ ] Actualizar planes de suscripción con la nueva oferta bundled

---

## Apéndice A: Glosario de Términos

| Término           |                                        Definición                                        |
| ----------------- | :--------------------------------------------------------------------------------------: |
| **DGII**          |           Dirección General de Impuestos Internos — Autoridad tributaria de RD           |
| **RNC**           |                           Registro Nacional de Contribuyentes                            |
| **NCF**           |                               Número de Comprobante Fiscal                               |
| **e-NCF**         |                         Número de Comprobante Fiscal Electrónico                         |
| **e-CF**          |                              Comprobante Fiscal Electrónico                              |
| **ITBIS**         | Impuesto a la Transferencia de Bienes Industrializados y Servicios (IVA dominicano, 18%) |
| **ISR**           |                                 Impuesto Sobre la Renta                                  |
| **PSFE**          |                    Proveedor de Servicios de Facturación Electrónica                     |
| **OFV**           |                       Oficina Virtual (portal en línea de la DGII)                       |
| **RST**           |                           Régimen Simplificado de Tributación                            |
| **INDOTEL**       |                      Instituto Dominicano de las Telecomunicaciones                      |
| **Ley 32-23**     |                           Ley de Facturación Electrónica de RD                           |
| **Ley 11-92**     |                                 Código Tributario de RD                                  |
| **Norma 06-2018** |                        Norma General sobre Comprobantes Fiscales                         |

## Apéndice B: Fuentes

- DGII Portal: https://dgii.gov.do
- Facturación Electrónica DGII: https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF/
- Facturador Gratuito DGII: https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF/Paginas/facturador-gratuito.aspx
- Tipos y Estructura e-CF: https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF/Paginas/TipoyEstructurae-CF.aspx
- Proveedores FE Autorizados: https://dgii.gov.do/cicloContribuyente/facturacion/comprobantesFiscalesElectronicosE-CF/Paginas/Proveedores-servicios-FE-autorizados.aspx
- Alegra RD Precios: https://alegra.com/precios/
- Odoo Pricing: https://www.odoo.com/pricing
- Ley 32-23 (Facturación Electrónica)
- Ley 11-92 (Código Tributario)
- Ley 253-12 (Reforma Tributaria)
- Norma General 06-2018 (Comprobantes Fiscales)

---

_Este documento es un análisis de mercado para uso interno de OKLA. Las cifras de precios de competidores fueron consultadas en marzo 2026 y pueden haber cambiado. Se recomienda validar con un contador público autorizado y un abogado fiscal antes de tomar decisiones de inversión._
