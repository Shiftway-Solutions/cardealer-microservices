# 📋 Auditoría de Cumplimiento Legal — Plataforma OKLA

**Marketplace de Vehículos en República Dominicana**

---

| Campo                 | Detalle                         |
| --------------------- | ------------------------------- |
| **Fecha del informe** | 5 de marzo de 2026              |
| **Dominio**           | okla.com.do                     |
| **Entidad auditada**  | OKLA (marketplace de vehículos) |
| **Auditor**           | Equipo Legal & Compliance       |
| **Versión**           | 1.0                             |
| **Clasificación**     | Confidencial — Solo uso interno |

---

## Índice

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Ley 172-13 — Protección de Datos Personales](#2-ley-172-13--protección-de-datos-personales)
3. [Ley 126-02 — Comercio Electrónico y Firmas Digitales](#3-ley-126-02--comercio-electrónico-y-firmas-digitales)
4. [Ley 53-07 — Crímenes y Delitos de Alta Tecnología](#4-ley-53-07--crímenes-y-delitos-de-alta-tecnología)
5. [Ley 358-05 — Protección al Consumidor](#5-ley-358-05--protección-al-consumidor)
6. [Código Tributario (Ley 11-92) y Resoluciones DGII](#6-código-tributario-ley-11-92-y-resoluciones-dgii)
7. [Ley 155-17 — Lavado de Activos (AML/KYC)](#7-ley-155-17--lavado-de-activos-amlkyc)
8. [INDOTEL — Regulaciones de Telecomunicaciones](#8-indotel--regulaciones-de-telecomunicaciones)
9. [ProConsumidor — Protección al Consumidor](#9-proconsumidor--protección-al-consumidor)
10. [Ley 20-00 — Propiedad Industrial](#10-ley-20-00--propiedad-industrial)
11. [Ley 65-00 — Derecho de Autor](#11-ley-65-00--derecho-de-autor)
12. [Ley 1-12 — Estrategia Nacional de Desarrollo](#12-ley-1-12--estrategia-nacional-de-desarrollo)
13. [Regulaciones Vehiculares (DGTT/INTRANT)](#13-regulaciones-vehiculares-dgttintrant)
14. [Ley 141-15 — Reestructuración y Liquidación](#14-ley-141-15--reestructuración-y-liquidación)
15. [Hallazgos Transversales](#15-hallazgos-transversales)
16. [Matriz de Riesgos y Prioridades](#16-matriz-de-riesgos-y-prioridades)
17. [Plan de Acción con Cronograma](#17-plan-de-acción-con-cronograma)
18. [Conclusiones](#18-conclusiones)

---

## 1. Resumen Ejecutivo

### 1.1 Descripción de la Plataforma

OKLA es un marketplace digital de vehículos que opera en República Dominicana bajo el dominio **okla.com.do**. La plataforma conecta compradores y vendedores de vehículos, cobrando $29 USD por anuncio a vendedores individuales y suscripciones de $49–$299 USD/mes a concesionarios (dealers). Los compradores acceden de forma gratuita.

**Datos recopilados:** nombres, correos electrónicos, teléfonos, cédulas, direcciones, fotos personales, documentos comerciales (RNC, registro mercantil), datos de vehículos (VIN, placa, historial), datos de pago.

**Infraestructura:** Servidores en DigitalOcean (NYC, Estados Unidos), base de datos PostgreSQL, procesamiento de pagos vía Stripe, notificaciones vía SendGrid.

### 1.2 Resumen de Cumplimiento

| Categoría                         | ✅ Cumple | ⚠️ Parcial | ❌ No Cumple | 🔍 Verificar |
| --------------------------------- | :-------: | :--------: | :----------: | :----------: |
| Protección de Datos (172-13)      |     4     |     3      |      2       |      1       |
| Comercio Electrónico (126-02)     |     3     |     2      |      1       |      0       |
| Delitos Tecnológicos (53-07)      |     3     |     1      |      0       |      1       |
| Protección al Consumidor (358-05) |     3     |     2      |      1       |      0       |
| Tributario (11-92) / DGII         |     2     |     1      |      2       |      1       |
| Anti-Lavado (155-17)              |     1     |     2      |      2       |      1       |
| INDOTEL                           |     1     |     1      |      1       |      0       |
| ProConsumidor                     |     2     |     1      |      1       |      0       |
| Propiedad Industrial (20-00)      |     1     |     0      |      0       |      2       |
| Derecho de Autor (65-00)          |     2     |     1      |      0       |      0       |
| Vehiculares (DGTT/INTRANT)        |     0     |     1      |      1       |      2       |
| **TOTAL**                         |  **22**   |   **15**   |    **11**    |    **8**     |

### 1.3 Hallazgos Críticos

| #   | Hallazgo                                                                                        | Ley             | Prioridad |
| --- | ----------------------------------------------------------------------------------------------- | --------------- | --------- |
| 1   | Datos de ciudadanos dominicanos almacenados en servidores de EE.UU. sin autorización de INDOTEL | 172-13, Art. 27 | 🔴 Alta   |
| 2   | Ausencia de banner de consentimiento de cookies conforme a la ley                               | 172-13          | 🔴 Alta   |
| 3   | No se emiten comprobantes fiscales electrónicos (e-CF) por servicios cobrados                   | Ley 11-92       | 🔴 Alta   |
| 4   | Falta registro como sujeto obligado ante la UAF para prevención de lavado                       | 155-17          | 🔴 Alta   |
| 5   | No se verifica la titularidad del vehículo contra registros DGTT                                | DGTT/INTRANT    | 🟡 Media  |
| 6   | Política de privacidad no nombra al Oficial de Protección de Datos                              | 172-13          | 🟡 Media  |
| 7   | Exportación de datos del usuario es parcial (descarga placeholder)                              | 172-13          | 🟡 Media  |

---

## 2. Ley 172-13 — Protección de Datos Personales

**Ley No. 172-13 sobre Protección Integral de los Datos Personales**
Vigente desde el 15 de diciembre de 2013.

### 2.1 Requisitos y Estado de Cumplimiento

| #      | Requisito                                                     | Artículo   | Estado       | Prioridad |
| ------ | ------------------------------------------------------------- | ---------- | ------------ | --------- |
| 2.1.1  | Consentimiento informado para recolección de datos            | Art. 5     | ⚠️ Parcial   | Alta      |
| 2.1.2  | Finalidad legítima y proporcionada                            | Art. 4     | ✅ Cumple    | —         |
| 2.1.3  | Derechos ARCO (Acceso, Rectificación, Cancelación, Oposición) | Arts. 9-12 | ✅ Cumple    | —         |
| 2.1.4  | Registro de bases de datos ante autoridad competente          | Art. 22    | ❌ No Cumple | Alta      |
| 2.1.5  | Designación de Oficial de Protección de Datos                 | Art. 23    | ❌ No Cumple | Alta      |
| 2.1.6  | Medidas de seguridad técnicas y organizativas                 | Art. 13    | ✅ Cumple    | —         |
| 2.1.7  | Portabilidad de datos                                         | —          | ⚠️ Parcial   | Media     |
| 2.1.8  | Transferencia internacional de datos                          | Art. 27    | ⚠️ Parcial   | Alta      |
| 2.1.9  | Notificación de brechas de seguridad                          | Art. 14    | 🔍 Verificar | Alta      |
| 2.1.10 | Consentimiento para cookies y tracking                        | Art. 5     | ❌ No Cumple | Alta      |

### 2.2 Análisis Detallado

#### ✅ Derechos ARCO — Implementación Ejemplar

La plataforma cuenta con un `PrivacyController` en el UserService que implementa los 5 derechos ARCO de manera completa:

- **Acceso**: Endpoints `GET /api/privacy/my-data` y `GET /api/privacy/my-data/full` permiten al usuario consultar todos sus datos.
- **Rectificación**: Implementado a través del `UsersController` para actualizar perfil.
- **Cancelación**: Flujo completo de eliminación de cuenta con solicitud, confirmación por código, período de gracia de 15 días, y posibilidad de cancelar. El `AccountDeletionWorker` ejecuta la eliminación de forma programada.
- **Oposición**: Gestión de preferencias de comunicación con opt-out individual y masivo (`POST /api/privacy/preferences/unsubscribe-all`).
- **Portabilidad**: Solicitud de exportación de datos con opción de seleccionar categorías (perfil, actividad, mensajes, favoritos, transacciones).

El endpoint público `GET /api/privacy/rights-info` (acceso anónimo) informa explícitamente a los usuarios sobre sus derechos bajo la Ley 172-13 — esto es una práctica ejemplar.

#### ⚠️ Portabilidad — Implementación Incompleta

El endpoint `GET /api/privacy/export/download/{token}` actualmente retorna datos placeholder (`"Este es un archivo de ejemplo"`). La exportación real de datos no está implementada. Esto debe completarse para cumplir plenamente con el derecho de portabilidad.

**Acciones requeridas:**

1. Implementar la generación real del archivo de exportación (JSON/CSV) con todos los datos del usuario.
2. Incluir datos de todos los microservicios (vehículos publicados, mensajes, transacciones, favoritos).
3. Asegurar que el enlace de descarga expire según lo informado al usuario.

#### ❌ Registro de Bases de Datos

No hay evidencia de que las bases de datos que contienen datos personales estén registradas ante la autoridad competente (actualmente INDOTEL actúa como autoridad de datos en RD).

**Acciones requeridas:**

1. Identificar todas las bases de datos con datos personales (UserService, KYCService, ContactService, NotificationService).
2. Preparar formulario de registro según el formato requerido.
3. Presentar inscripción ante INDOTEL.

#### ❌ Oficial de Protección de Datos

Ni la Política de Privacidad ni los Términos y Condiciones mencionan al Oficial de Protección de Datos (DPO). La Ley 172-13 requiere designar una persona responsable del cumplimiento.

**Acciones requeridas:**

1. Designar formalmente un Oficial de Protección de Datos.
2. Publicar su información de contacto en la Política de Privacidad.
3. Asegurar que el DPO tenga acceso a todos los procesos de datos.

#### ⚠️ Transferencia Internacional de Datos

Los servidores están en DigitalOcean (NYC, Estados Unidos). La Ley 172-13, Art. 27, establece que la transferencia internacional de datos personales solo es permisible si el país destino ofrece un nivel adecuado de protección o si el titular ha dado consentimiento expreso.

**Estado actual:** La Política de Privacidad menciona que "Sus datos pueden ser transferidos y procesados en servidores ubicados fuera de República Dominicana" y que se asegurarán "protecciones adecuadas según las leyes aplicables", pero:

- No solicita consentimiento explícito para la transferencia internacional.
- No menciona mecanismos específicos de protección (cláusulas contractuales estándar, etc.).
- No identifica Estados Unidos como destino específico.

**Acciones requeridas:**

1. Incluir consentimiento expreso para transferencia internacional en el registro de usuario.
2. Implementar cláusulas contractuales estándar con DigitalOcean.
3. Evaluar migración a servidores en la región (DigitalOcean tiene datacenter en Miami — más cercano pero sigue siendo EE.UU.) o implementar encriptación en reposo con claves controladas desde RD.
4. Actualizar Política de Privacidad con detalle del país destino y justificación legal.

#### ❌ Consentimiento de Cookies

La plataforma no implementa un banner de consentimiento de cookies. La Política de Privacidad describe categorías de cookies pero remite al usuario a "la configuración de su navegador" — esto **no cumple** con el requisito de consentimiento previo para cookies no esenciales.

**Acciones requeridas:**

1. Implementar banner de cookies con las categorías: esenciales (no requieren consentimiento), de preferencias, analíticas, y de marketing.
2. Las cookies no esenciales no deben cargarse hasta obtener consentimiento.
3. Permitir al usuario gestionar preferencias granulares.
4. Registrar y almacenar el consentimiento otorgado.

---

## 3. Ley 126-02 — Comercio Electrónico y Firmas Digitales

**Ley No. 126-02 sobre Comercio Electrónico, Documentos y Firmas Digitales**

### 3.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                                     | Artículo | Estado       | Prioridad |
| ----- | --------------------------------------------- | -------- | ------------ | --------- |
| 3.1.1 | Identificación del prestador de servicios     | Art. 8   | ✅ Cumple    | —         |
| 3.1.2 | Información clara sobre precios y condiciones | Art. 10  | ✅ Cumple    | —         |
| 3.1.3 | Confirmación de recepción de pedidos          | Art. 11  | ✅ Cumple    | —         |
| 3.1.4 | Derecho de retracto (7 días)                  | Art. 12  | ⚠️ Parcial   | Media     |
| 3.1.5 | Validez de contratos electrónicos             | Art. 3   | ⚠️ Parcial   | Media     |
| 3.1.6 | Conservación de documentos electrónicos       | Art. 5   | ❌ No Cumple | Media     |

### 3.2 Análisis Detallado

#### ✅ Identificación del Prestador

Los Términos y Condiciones en `/terminos` identifican a OKLA como prestador del servicio, incluyen dirección física (Av. Winston Churchill #1099, Santo Domingo), correo electrónico de contacto (legal@okla.com.do) y descripción del servicio.

#### ⚠️ Derecho de Retracto

La Ley 126-02 otorga al consumidor 7 días hábiles para retractarse de una compra online. Dado que OKLA cobra por publicación de anuncios ($29) y suscripciones ($49-$299), debe ofrecer esta posibilidad.

**Estado actual:** No se menciona política de retracto ni reembolsos específicos. Los Términos mencionan vagamente "políticas de reembolso disponibles en nuestro Centro de Ayuda" que no existe.

**Acciones requeridas:**

1. Definir política de reembolso/retracto para anuncios pagados.
2. Publicar dicha política de forma visible antes del pago.
3. Implementar mecanismo de solicitud de reembolso en la plataforma.

#### ❌ Conservación de Documentos Electrónicos

La ley requiere conservar documentos electrónicos (contratos, comprobantes, comunicaciones) por un período mínimo. No hay evidencia de una política formal de retención de documentos electrónicos que cubra transacciones comerciales.

**Acciones requeridas:**

1. Definir política de retención de documentos (mínimo 10 años para documentos tributarios según Código Tributario).
2. Implementar sistema de archivo de transacciones y comprobantes.
3. Asegurar que los backups de base de datos cumplan con los períodos de retención.

---

## 4. Ley 53-07 — Crímenes y Delitos de Alta Tecnología

**Ley No. 53-07 contra Crímenes y Delitos de Alta Tecnología**

### 4.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                                       | Artículo | Estado       | Prioridad |
| ----- | ----------------------------------------------- | -------- | ------------ | --------- |
| 4.1.1 | Protección contra acceso ilícito a sistemas     | Art. 6   | ✅ Cumple    | —         |
| 4.1.2 | Protección de datos almacenados                 | Art. 9   | ✅ Cumple    | —         |
| 4.1.3 | Conservación de datos de tráfico                | Art. 56  | 🔍 Verificar | Alta      |
| 4.1.4 | Cooperación con autoridades                     | Art. 55  | ⚠️ Parcial   | Media     |
| 4.1.5 | Prevención de uso de la plataforma para estafas | Art. 13  | ✅ Cumple    | —         |

### 4.2 Análisis Detallado

#### ✅ Protección Contra Acceso Ilícito

La plataforma implementa múltiples capas de seguridad:

- **Autenticación**: JWT con HttpOnly cookies (Secure, SameSite=Lax), 2FA vía SMS y TOTP.
- **Autorización**: RBAC con roles (Admin, Dealer, Seller, User).
- **CSRF Protection**: Middleware de validación CSRF implementado.
- **Rate Limiting**: Políticas configuradas por endpoint, incluyendo restricciones estrictas para pagos.
- **Validación de entrada**: SecurityValidators con protección NoSqlInjection y NoXss en todos los servicios.
- **Encriptación**: HTTPS/TLS, bcrypt para contraseñas.

#### 🔍 Conservación de Datos de Tráfico

El Art. 56 de la Ley 53-07 obliga a los prestadores de servicios a conservar datos de tráfico por un período de 90 días para posibles investigaciones judiciales. El AuditService registra operaciones, pero debe verificarse:

**Acciones requeridas:**

1. Verificar que los logs de acceso (IP, timestamp, acción) se conserven al menos 90 días.
2. Verificar que los logs incluyan: dirección IP origen, fecha/hora, tipo de servicio utilizado, identificación del usuario.
3. Implementar política de retención específica de 90 días mínimo para datos de tráfico.
4. Documentar procedimiento de respuesta ante requerimientos judiciales (Art. 55).

---

## 5. Ley 358-05 — Protección al Consumidor

**Ley No. 358-05 General de Protección de los Derechos del Consumidor o Usuario**

### 5.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                                  | Artículo | Estado       | Prioridad |
| ----- | ------------------------------------------ | -------- | ------------ | --------- |
| 5.1.1 | Información veraz sobre servicios          | Art. 33  | ✅ Cumple    | —         |
| 5.1.2 | Prohibición de publicidad engañosa         | Art. 90  | ✅ Cumple    | —         |
| 5.1.3 | Mecanismo de reclamación                   | Art. 80  | ⚠️ Parcial   | Media     |
| 5.1.4 | Libro de reclamaciones / sistema de quejas | Art. 81  | ❌ No Cumple | Media     |
| 5.1.5 | Garantía de servicio                       | Art. 42  | ⚠️ Parcial   | Baja      |
| 5.1.6 | Referencia a ProConsumidor                 | Art. 2   | ✅ Cumple    | —         |

### 5.2 Análisis Detallado

#### ✅ Referencia a ProConsumidor

Los Términos y Condiciones (sección 14) incluyen expresamente los derechos del consumidor bajo la Ley 358-05 e incluyen datos de contacto de ProConsumidor (Tel. 809-567-7755, www.proconsumidor.gob.do). Esto es una práctica positiva y correcta.

#### ❌ Sistema de Quejas Interno

Aunque los Términos mencionan el derecho a reclamar ante ProConsumidor, no existe un sistema interno de gestión de quejas/reclamaciones que permita:

- Registrar reclamaciones de usuarios.
- Dar seguimiento con número de caso.
- Establecer plazos de respuesta.
- Escalar a ProConsumidor si no se resuelve.

**Acciones requeridas:**

1. Implementar módulo de reclamaciones (podría integrarse en ContactService).
2. Definir SLAs de respuesta (máximo 15 días hábiles según regulación ProConsumidor).
3. Generar comprobante de reclamación para el usuario.
4. Implementar escalamiento automático si no se resuelve en plazo.

---

## 6. Código Tributario (Ley 11-92) y Resoluciones DGII

**Ley No. 11-92 (Código Tributario) y Resoluciones de la DGII**

### 6.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                                    | Artículo/Resolución   | Estado       | Prioridad |
| ----- | -------------------------------------------- | --------------------- | ------------ | --------- |
| 6.1.1 | Registro Nacional de Contribuyentes (RNC)    | Art. 50 CT            | 🔍 Verificar | Alta      |
| 6.1.2 | Emisión de comprobantes fiscales (NCF/e-CF)  | Norma General 06-2018 | ❌ No Cumple | Alta      |
| 6.1.3 | Cobro y declaración de ITBIS (18%)           | Arts. 335-358 CT      | ❌ No Cumple | Alta      |
| 6.1.4 | Declaración de Impuesto sobre la Renta (ISR) | Arts. 267-297 CT      | ⚠️ Parcial   | Alta      |
| 6.1.5 | Reportes 606/607 a la DGII                   | Norma General 07-2018 | ✅ Cumple\*  | Media     |
| 6.1.6 | Retención de ITBIS a proveedores             | Art. 346 CT           | ✅ Cumple\*  | Media     |

_\*El TaxComplianceService tiene la estructura implementada para Reportes 606/607, pero debe verificarse si está activo y funcionando._

### 6.2 Análisis Detallado

#### ❌ Comprobantes Fiscales Electrónicos (e-CF)

**Hallazgo Crítico:** OKLA cobra por sus servicios ($29/anuncio, $49-$299/mes suscripciones) pero no emite comprobantes fiscales electrónicos (e-CF). La DGII exige que toda transacción comercial esté respaldada por un comprobante fiscal.

El `TaxComplianceService` existe y tiene entidades como `NcfSequence`, `TaxDeclaration`, `Reporte606Item`, y `Reporte607Item`, pero:

- No se evidencia integración con la API de e-CF de la DGII.
- No se genera NCF/e-CF al momento del pago en el flujo de Stripe.

**Acciones requeridas:**

1. Registrarse como emisor de e-CF ante la DGII (si no se ha hecho).
2. Integrar la API de facturación electrónica de la DGII (e-CF) al TaxComplianceService.
3. Generar e-CF automáticamente al procesar cada pago vía Stripe.
4. Enviar el e-CF al comprador por correo electrónico.
5. Reportar comprobantes emitidos a la DGII según calendario.

#### ❌ ITBIS sobre Servicios Digitales

Los servicios digitales en República Dominicana están gravados con ITBIS (18%). Al cobrar $29/anuncio o suscripciones mensuales, OKLA debe:

- Incluir el ITBIS en el precio (o sumarlo al precio).
- Declarar y pagar el ITBIS mensualmente (formulario IT-1).
- Si cobra en USD, convertir a DOP al tipo de tasa oficial del día.

**Acciones requeridas:**

1. Determinar si los precios actuales incluyen ITBIS o se suman.
2. Actualizar la interfaz de pago para mostrar desglose de ITBIS.
3. Configurar la declaración mensual de ITBIS en el TaxComplianceService.
4. Establecer proceso de conversión USD→DOP para fines fiscales.

---

## 7. Ley 155-17 — Lavado de Activos (AML/KYC)

**Ley No. 155-17 contra el Lavado de Activos y el Financiamiento del Terrorismo**

### 7.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                                 | Artículo | Estado       | Prioridad |
| ----- | ----------------------------------------- | -------- | ------------ | --------- |
| 7.1.1 | Debida diligencia del cliente (KYC)       | Art. 39  | ✅ Cumple    | —         |
| 7.1.2 | Registro ante la UAF como sujeto obligado | Art. 35  | ❌ No Cumple | Alta      |
| 7.1.3 | Oficial de cumplimiento designado         | Art. 42  | ❌ No Cumple | Alta      |
| 7.1.4 | Reportes de operaciones sospechosas (ROS) | Art. 43  | ⚠️ Parcial   | Alta      |
| 7.1.5 | Programa de prevención de lavado          | Art. 38  | ⚠️ Parcial   | Alta      |
| 7.1.6 | Identificación de PEPs                    | Art. 40  | 🔍 Verificar | Media     |

### 7.2 Análisis Detallado

#### Aplicabilidad de la Ley 155-17 a OKLA

La Ley 155-17 aplica a "actividades y profesiones no financieras designadas" (APNFD), que incluyen **agentes inmobiliarios y de bienes raíces**. Aunque OKLA es un marketplace de vehículos y no un agente inmobiliario per se, la compraventa de vehículos de alto valor puede caer bajo supervisión de la Unidad de Análisis Financiero (UAF) dependiendo de umbrales de transacción.

**Nota:** Si los vehículos publicados superan umbrales establecidos por la UAF (generalmente RD$500,000 o equivalente), OKLA podría ser considerado sujeto obligado. Se recomienda consulta legal especializada.

#### ✅ KYC — Implementación Robusta

El KYCService implementa un proceso de verificación de identidad para dealers:

- Recopilación de cédula/RNC.
- Verificación con selfie.
- Documentos comerciales (registro mercantil, RNC).
- Flujo de aprobación/rechazo con notificaciones vía email.
- Eventos `KYCProfileStatusChangedEvent` para comunicación entre servicios.

#### ❌ Registro ante la UAF

No hay evidencia de registro ante la Unidad de Análisis Financiero (UAF). Si OKLA es determinado como sujeto obligado, debe:

**Acciones requeridas:**

1. Consultar con abogado especializado si OKLA califica como sujeto obligado.
2. Si aplica, registrarse ante la UAF.
3. Designar un Oficial de Cumplimiento.
4. Implementar programa de prevención de lavado de activos.
5. Establecer mecanismo para reportar operaciones sospechosas (ROS).
6. Implementar screening contra listas de sanciones y PEPs.

---

## 8. INDOTEL — Regulaciones de Telecomunicaciones

**Instituto Dominicano de las Telecomunicaciones**

### 8.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                                                    | Resolución/Norma | Estado       | Prioridad |
| ----- | ------------------------------------------------------------ | ---------------- | ------------ | --------- |
| 8.1.1 | Registro como proveedor de servicios de internet (si aplica) | Ley 153-98       | ✅ No Aplica | —         |
| 8.1.2 | Cumplimiento como autoridad de protección de datos           | Ley 172-13       | ⚠️ Parcial   | Alta      |
| 8.1.3 | Autorización para envío de comunicaciones comerciales (SMS)  | Res. 086-09      | ❌ No Cumple | Media     |

### 8.2 Análisis Detallado

#### ❌ Comunicaciones Comerciales por SMS

OKLA implementa verificación por SMS (2FA) y notificaciones. La Resolución 086-09 de INDOTEL regula las comunicaciones comerciales no solicitadas. Si OKLA envía SMS promocionales, debe:

**Acciones requeridas:**

1. Obtener consentimiento previo y expreso para SMS de marketing (opt-in).
2. Incluir mecanismo de opt-out en cada SMS promocional.
3. Verificar que el servicio de SMS (si usa proveedor local) cumpla con regulaciones de INDOTEL.
4. Mantener registro de consentimientos otorgados y revocados.

**Nota:** Los SMS de 2FA y verificación son transaccionales y no requieren consentimiento de marketing.

---

## 9. ProConsumidor — Protección al Consumidor

**Instituto Nacional de Protección de los Derechos del Consumidor**

### 9.1 Requisitos y Estado de Cumplimiento

| #     | Requisito                               | Base Legal          | Estado       | Prioridad |
| ----- | --------------------------------------- | ------------------- | ------------ | --------- |
| 9.1.1 | Registro ante ProConsumidor (si aplica) | Ley 358-05          | 🔍 Verificar | Media     |
| 9.1.2 | Información clara de precios            | Ley 358-05, Art. 83 | ✅ Cumple    | —         |
| 9.1.3 | Publicidad no engañosa                  | Ley 358-05, Art. 90 | ✅ Cumple    | —         |
| 9.1.4 | Mecanismo de reclamaciones              | Ley 358-05, Art. 80 | ⚠️ Parcial   | Media     |
| 9.1.5 | Protección datos de tarjetas            | Ley 358-05, Art. 48 | ✅ Cumple\*  | —         |

_\*Stripe maneja PCI-DSS compliance._

### 9.2 Acciones Requeridas

1. Verificar si OKLA debe registrarse ante ProConsumidor como proveedor de servicios.
2. Implementar libro de reclamaciones digital (ver sección 5).
3. Publicar procedimiento de quejas de manera visible en el sitio.

---

## 10. Ley 20-00 — Propiedad Industrial

**Ley No. 20-00 sobre Propiedad Industrial**

### 10.1 Requisitos y Estado de Cumplimiento

| #      | Requisito                           | Artículo    | Estado       | Prioridad |
| ------ | ----------------------------------- | ----------- | ------------ | --------- |
| 10.1.1 | Registro de marca "OKLA" ante ONAPI | Arts. 71-73 | 🔍 Verificar | Alta      |
| 10.1.2 | Protección del dominio okla.com.do  | —           | ✅ Cumple    | —         |
| 10.1.3 | No infracción de marcas de terceros | Arts. 95-97 | 🔍 Verificar | Media     |

### 10.2 Acciones Requeridas

1. **Verificar registro de marca:** Confirmar que "OKLA" y el logo están registrados ante la Oficina Nacional de Propiedad Industrial (ONAPI) en las clases relevantes (Clase 35: publicidad y negocios; Clase 12: vehículos; Clase 42: servicios tecnológicos).
2. **Vigilancia de marca:** Monitorear posibles infracciones o registros similares.
3. **Verificar que las marcas de vehículos** (Toyota, Honda, etc.) se usan solo de manera descriptiva y no como endorsement.

---

## 11. Ley 65-00 — Derecho de Autor

**Ley No. 65-00 sobre Derecho de Autor**

### 11.1 Requisitos y Estado de Cumplimiento

| #      | Requisito                                           | Artículo | Estado     | Prioridad |
| ------ | --------------------------------------------------- | -------- | ---------- | --------- |
| 11.1.1 | Protección del código fuente como obra literaria    | Art. 2   | ✅ Cumple  | —         |
| 11.1.2 | Licencia de uso de contenido generado por usuarios  | Art. 38  | ✅ Cumple  | —         |
| 11.1.3 | Procedimiento de reclamo por infracción (DMCA-like) | Art. 169 | ⚠️ Parcial | Media     |

### 11.2 Análisis Detallado

#### ✅ Licencia de Contenido de Usuarios

Los Términos y Condiciones (sección 7) establecen correctamente que el usuario retiene la propiedad de su contenido pero otorga a OKLA una "licencia mundial, no exclusiva, libre de regalías para usar, modificar, reproducir y distribuir dicho contenido en conexión con el servicio."

#### ⚠️ Procedimiento de Takedown

No existe un procedimiento formal publicado para que terceros reporten contenido que infrinja derechos de autor (por ejemplo, fotos robadas de otros anuncios o sitios).

**Acciones requeridas:**

1. Publicar procedimiento de notificación y retirada (notice & takedown) en los Términos.
2. Designar agente receptor de notificaciones de infracción.
3. Implementar formulario de reporte de contenido infractor.

---

## 12. Ley 1-12 — Estrategia Nacional de Desarrollo

**Ley No. 1-12 (Estrategia Nacional de Desarrollo 2030)**

### 12.1 Alineamiento con Objetivos Digitales

La Ley 1-12 establece objetivos de digitalización de la economía dominicana. OKLA se alinea positivamente con:

| Objetivo END 2030                            | Contribución de OKLA                 | Estado      |
| -------------------------------------------- | ------------------------------------ | ----------- |
| Eje 3: Economía articulada, innovadora       | Digitalización del mercado vehicular | ✅ Alineado |
| Obj. 3.3.4: Fomento del comercio electrónico | Marketplace digital accesible        | ✅ Alineado |
| Obj. 2.3.3: Inclusión digital                | Plataforma web + móvil               | ✅ Alineado |

**Nota:** No hay requisitos de cumplimiento específicos bajo esta ley, pero el alineamiento con la END 2030 puede facilitar gestiones gubernamentales y acceso a incentivos para empresas de tecnología.

---

## 13. Regulaciones Vehiculares (DGTT/INTRANT)

**Dirección General de Tránsito Terrestre (DGTT) / Instituto Nacional de Tránsito y Transporte Terrestre (INTRANT)**

### 13.1 Requisitos y Estado de Cumplimiento

| #      | Requisito                                                    | Base Legal     | Estado       | Prioridad |
| ------ | ------------------------------------------------------------ | -------------- | ------------ | --------- |
| 13.1.1 | Verificación de titularidad vehicular                        | Ley 241 / DGTT | ❌ No Cumple | Media     |
| 13.1.2 | Verificación de estatus legal del vehículo (embargos, robos) | DGTT           | 🔍 Verificar | Media     |
| 13.1.3 | Información sobre proceso de transferencia                   | INTRANT/DGTT   | ⚠️ Parcial   | Baja      |
| 13.1.4 | Verificación de VIN válido                                   | —              | 🔍 Verificar | Media     |

### 13.2 Análisis Detallado

#### ❌ Verificación de Titularidad

OKLA permite publicar vehículos declarando ser propietario, pero no verifica contra los registros de la DGTT. Aunque los Términos establecen que el vendedor "declara y garantiza que es el propietario legítimo", esto no sustituye la verificación real.

**Acciones requeridas:**

1. Evaluar integración con bases de datos de DGTT/INTRANT para verificar titularidad.
2. Verificar estatus del vehículo (no reportado como robado, sin embargos judiciales).
3. Si no es posible integración directa, implementar verificación manual para vehículos de alto valor.
4. Incluir advertencia visible de que OKLA no verifica titularidad y que el comprador debe verificar documentos.

#### ⚠️ Información sobre Transferencia

Los Términos no incluyen información sobre el proceso legal de transferencia vehicular en RD (requiere visita a DGTT, pago de impuesto de transferencia del 2%, matrícula nueva).

**Acciones requeridas:**

1. Agregar sección informativa sobre el proceso de transferencia legal.
2. Incluir lista de documentos necesarios (matrícula, cédula, seguro, inspección técnica).
3. Advertir sobre el impuesto de transferencia vehicular (2% del valor).

---

## 14. Ley 141-15 — Reestructuración y Liquidación

**Ley No. 141-15 de Reestructuración y Liquidación de Empresas**

### 14.1 Aplicabilidad

Esta ley aplica principalmente a empresas en dificultades financieras. Para OKLA en su etapa actual, los requisitos más relevantes son:

| #      | Requisito                                                     | Estado       | Prioridad |
| ------ | ------------------------------------------------------------- | ------------ | --------- |
| 14.1.1 | Registro mercantil actualizado                                | 🔍 Verificar | Media     |
| 14.1.2 | Contabilidad organizada según normas                          | 🔍 Verificar | Media     |
| 14.1.3 | Plan de contingencia para datos de usuarios en caso de cierre | ❌ No Cumple | Baja      |

**Acciones requeridas:**

1. Definir plan de contingencia que establezca qué sucede con los datos de usuarios si OKLA cesa operaciones.
2. Incluir esta información en la Política de Privacidad.
3. Asegurar que existan backups accesibles para devolver datos a usuarios.

---

## 15. Hallazgos Transversales

### 15.1 Términos y Condiciones

| Elemento                      | Estado | Observación                                    |
| ----------------------------- | ------ | ---------------------------------------------- |
| Identificación de la empresa  | ✅     | Nombre, dirección, email                       |
| Descripción del servicio      | ✅     | Clara y precisa                                |
| Precios y tarifas             | ⚠️     | Falta desglose de ITBIS                        |
| Jurisdicción aplicable        | ✅     | República Dominicana                           |
| Referencia a Ley 358-05       | ✅     | Sección 14 completa                            |
| Política de reembolso         | ❌     | No existe, solo referencia a "Centro de Ayuda" |
| Edad mínima                   | ✅     | 18 años (en Política de Privacidad)            |
| Limitación de responsabilidad | ✅     | Sección 8                                      |
| Modificación de términos      | ⚠️     | No especifica plazo de notificación previa     |
| Procedimiento de disputas     | ❌     | No existe procedimiento de resolución          |

### 15.2 Política de Privacidad

| Elemento                       | Estado | Observación                                |
| ------------------------------ | ------ | ------------------------------------------ |
| Datos recopilados              | ✅     | Categorías bien definidas                  |
| Finalidad del tratamiento      | ✅     | Sección 2 completa                         |
| Base legal del tratamiento     | ⚠️     | No cita artículos específicos              |
| Compartir con terceros         | ✅     | Categorías identificadas                   |
| Derechos ARCO                  | ✅     | Sección 5 — referencia a Ley 172-13        |
| Oficial de Protección de Datos | ❌     | No designado                               |
| Cookies y tracking             | ⚠️     | Descrita pero sin banner de consentimiento |
| Transferencia internacional    | ⚠️     | Mencionada pero sin detalle                |
| Retención de datos             | ✅     | Sección 7                                  |
| Menores de edad                | ✅     | Sección 9 — prohibido menores de 18        |
| Contacto para privacidad       | ✅     | privacidad@okla.com.do                     |
| Autoridad de supervisión       | ✅     | Menciona ProConsumidor e INDOTEL           |

### 15.3 Cookies y Consentimiento

| Elemento                   | Estado | Observación                               |
| -------------------------- | ------ | ----------------------------------------- |
| Banner de cookies          | ❌     | No implementado                           |
| Clasificación de cookies   | ⚠️     | Descrita en política pero no funcional    |
| Consentimiento previo      | ❌     | Cookies se cargan sin consentimiento      |
| Gestión de preferencias    | ❌     | Solo remite a configuración del navegador |
| Registro de consentimiento | ❌     | No existe                                 |

### 15.4 Verificación de Edad

| Elemento                | Estado | Observación                                 |
| ----------------------- | ------ | ------------------------------------------- |
| Declaración en política | ✅     | "No dirigido a menores de 18"               |
| Verificación activa     | ⚠️     | No hay checkbox ni verificación en registro |
| Mecanismo de reporte    | ⚠️     | Mencionado pero sin formulario              |

### 15.5 Ubicación de Datos

| Aspecto                  | Detalle                  | Riesgo                      |
| ------------------------ | ------------------------ | --------------------------- |
| Servidor aplicación      | DigitalOcean NYC (US)    | 🟡 Medio                    |
| Base de datos PostgreSQL | DigitalOcean NYC (US)    | 🔴 Alto                     |
| Procesamiento de pagos   | Stripe (US)              | 🟢 Bajo (Stripe es PCI-DSS) |
| Correo electrónico       | SendGrid (US)            | 🟡 Medio                    |
| Imágenes/media           | DigitalOcean Spaces (US) | 🟡 Medio                    |

**Recomendación:** Aunque no hay prohibición absoluta, se recomienda:

1. Implementar encriptación en reposo con claves gestionadas fuera de DigitalOcean.
2. Documentar las salvaguardas de protección implementadas.
3. Obtener consentimiento expreso para transferencia internacional.
4. Evaluar a mediano plazo migrar a datacenter más cercano o implementar encriptación end-to-end.

---

## 16. Matriz de Riesgos y Prioridades

### 16.1 Riesgos por Impacto y Probabilidad

| Riesgo                                                   | Impacto | Probabilidad | Nivel         | Acción                                |
| -------------------------------------------------------- | ------- | ------------ | ------------- | ------------------------------------- |
| Multa DGII por no emitir e-CF                            | Alto    | Alta         | 🔴 Crítico    | Implementar facturación electrónica   |
| Sanción por transferencia internacional sin autorización | Alto    | Media        | 🔴 Crítico    | Obtener consentimiento + salvaguardas |
| Multa por falta de banner de cookies                     | Medio   | Media        | 🟡 Importante | Implementar banner                    |
| Sanción UAF por no registrarse (si aplica)               | Alto    | Baja         | 🟡 Importante | Evaluar obligatoriedad                |
| Demanda de consumidor sin mecanismo de quejas            | Medio   | Media        | 🟡 Importante | Implementar sistema                   |
| Infracción por contenido no autorizado                   | Bajo    | Media        | 🟢 Moderado   | Implementar takedown                  |
| Reclamo por falta de reembolso                           | Medio   | Baja         | 🟢 Moderado   | Definir política                      |

### 16.2 Resumen de Acciones por Prioridad

#### 🔴 Prioridad Alta (0-3 meses)

| #   | Acción                                                              | Ley/Norma  | Esfuerzo | Responsable Sugerido      |
| --- | ------------------------------------------------------------------- | ---------- | -------- | ------------------------- |
| A1  | Implementar facturación electrónica (e-CF) integrada con DGII       | Ley 11-92  | Alto     | Desarrollo + Contabilidad |
| A2  | Implementar banner de cookies con consentimiento granular           | Ley 172-13 | Medio    | Frontend                  |
| A3  | Designar Oficial de Protección de Datos (DPO)                       | Ley 172-13 | Bajo     | Dirección                 |
| A4  | Registrar bases de datos ante INDOTEL                               | Ley 172-13 | Bajo     | Legal                     |
| A5  | Implementar consentimiento expreso para transferencia internacional | Ley 172-13 | Medio    | Backend + Frontend        |
| A6  | Configurar cobro y declaración de ITBIS (18%)                       | Ley 11-92  | Alto     | Desarrollo + Contabilidad |
| A7  | Evaluar obligación como sujeto AML ante la UAF                      | Ley 155-17 | Medio    | Legal                     |

#### 🟡 Prioridad Media (3-6 meses)

| #   | Acción                                                               | Ley/Norma  | Esfuerzo | Responsable Sugerido |
| --- | -------------------------------------------------------------------- | ---------- | -------- | -------------------- |
| B1  | Completar implementación de exportación de datos (portabilidad real) | Ley 172-13 | Medio    | Backend              |
| B2  | Implementar sistema de reclamaciones/quejas                          | Ley 358-05 | Medio    | Backend + Frontend   |
| B3  | Definir y publicar política de reembolso/retracto                    | Ley 126-02 | Bajo     | Legal + Frontend     |
| B4  | Verificar registro de marca OKLA ante ONAPI                          | Ley 20-00  | Bajo     | Legal                |
| B5  | Implementar procedimiento de notice & takedown                       | Ley 65-00  | Bajo     | Legal + Backend      |
| B6  | Verificar conservación de datos de tráfico (90 días mínimo)          | Ley 53-07  | Bajo     | DevOps               |
| B7  | Implementar verificación de edad activa en registro                  | Ley 172-13 | Bajo     | Frontend             |
| B8  | Documentar procedimiento de respuesta ante requerimiento judicial    | Ley 53-07  | Bajo     | Legal                |

#### 🟢 Prioridad Baja (6-12 meses)

| #   | Acción                                                               | Ley/Norma  | Esfuerzo | Responsable Sugerido |
| --- | -------------------------------------------------------------------- | ---------- | -------- | -------------------- |
| C1  | Evaluar integración con DGTT/INTRANT para verificación vehicular     | DGTT       | Alto     | Backend              |
| C2  | Agregar información sobre proceso de transferencia vehicular         | DGTT       | Bajo     | Frontend             |
| C3  | Definir plan de contingencia para datos en caso de cierre            | Ley 141-15 | Bajo     | Legal                |
| C4  | Implementar política formal de retención de documentos               | Ley 126-02 | Medio    | DevOps + Legal       |
| C5  | Evaluar migración o encriptación adicional de datos en servidores US | Ley 172-13 | Alto     | DevOps               |
| C6  | Agregar procedimiento de resolución de disputas en Términos          | Ley 358-05 | Bajo     | Legal                |

---

## 17. Plan de Acción con Cronograma

### Fase 1: Urgente (Marzo–Mayo 2026)

```
Semana 1-2:  A3 — Designar DPO
Semana 2-4:  A2 — Implementar banner de cookies
Semana 2-4:  A5 — Consentimiento transferencia internacional
Semana 3-6:  A4 — Registro bases de datos ante INDOTEL
Semana 4-8:  A7 — Consulta legal sobre obligación AML/UAF
Semana 4-12: A1 — Integración e-CF con DGII
Semana 4-12: A6 — Configuración ITBIS
```

### Fase 2: Importante (Junio–Agosto 2026)

```
Semana 13-16: B1 — Exportación de datos completa
Semana 13-16: B3 — Política de reembolso
Semana 13-16: B7 — Verificación de edad en registro
Semana 15-18: B2 — Sistema de reclamaciones
Semana 15-18: B4 — Verificación marca ONAPI
Semana 17-20: B5 — Procedimiento takedown
Semana 17-20: B6 — Verificación retención datos tráfico
Semana 18-20: B8 — Procedimiento requerimiento judicial
```

### Fase 3: Mejora Continua (Septiembre–Diciembre 2026)

```
Sep-Oct:  C1 — Evaluación integración DGTT
Sep-Oct:  C2 — Información transferencia vehicular
Oct-Nov:  C3 — Plan contingencia datos
Oct-Nov:  C4 — Política retención documentos
Nov-Dic:  C5 — Evaluación migración/encriptación
Nov-Dic:  C6 — Procedimiento resolución disputas
```

---

## 18. Conclusiones

### 18.1 Fortalezas Identificadas

1. **Derechos ARCO ejemplares**: El `PrivacyController` con implementación completa de Acceso, Rectificación, Cancelación, Oposición y Portabilidad, referenciando explícitamente la Ley 172-13, es una implementación sobresaliente.

2. **Seguridad técnica robusta**: HttpOnly cookies, CSRF protection, rate limiting, validación anti-SQLi/XSS, 2FA, y encriptación de contraseñas demuestran un compromiso serio con la seguridad.

3. **Términos y Condiciones bien estructurados**: Incluyen referencia a la Ley 358-05, datos de ProConsumidor, ley aplicable de RD, y cláusulas apropiadas de uso.

4. **Política de Privacidad sólida**: Cubre las categorías principales de datos, derechos ARCO, y referencia a autoridades competentes.

5. **TaxComplianceService pre-construido**: La infraestructura para cumplimiento tributario (entidades para NCF, Reportes 606/607, declaraciones) existe y solo necesita integración con la DGII.

6. **KYC robusto**: Proceso de verificación de identidad para dealers con documentación, selfie, y flujo de aprobación.

### 18.2 Áreas Críticas de Mejora

1. **Facturación electrónica**: La ausencia de e-CF es el riesgo fiscal más significativo. La DGII puede imponer multas desde RD$10,000 hasta RD$50,000 por cada comprobante no emitido.

2. **Consentimiento de cookies**: La ausencia de un banner funcional expone a la empresa a sanciones bajo la Ley 172-13.

3. **Transferencia internacional de datos**: Almacenar datos de ciudadanos dominicanos en servidores de EE.UU. sin consentimiento expreso ni salvaguardas documentadas es un riesgo legal significativo.

4. **Formalización institucional**: La falta de DPO, registro de bases de datos, y evaluación AML representan brechas de gobernanza que deben cerrarse rápidamente.

### 18.3 Evaluación General

| Área                             | Calificación       |
| -------------------------------- | ------------------ |
| Protección de datos técnica      | ⭐⭐⭐⭐ (4/5)     |
| Protección de datos legal/formal | ⭐⭐⭐ (3/5)       |
| Comercio electrónico             | ⭐⭐⭐ (3/5)       |
| Seguridad informática            | ⭐⭐⭐⭐⭐ (5/5)   |
| Cumplimiento tributario          | ⭐⭐ (2/5)         |
| Anti-lavado (AML)                | ⭐⭐ (2/5)         |
| Protección al consumidor         | ⭐⭐⭐ (3/5)       |
| Propiedad intelectual            | ⭐⭐⭐ (3/5)       |
| Regulaciones vehiculares         | ⭐⭐ (2/5)         |
| **Promedio general**             | **⭐⭐⭐ (3.0/5)** |

La plataforma demuestra una base técnica sólida con áreas de cumplimiento formal que necesitan atención. Con la implementación del plan de acción propuesto, OKLA puede alcanzar un nivel de cumplimiento de 4.5/5 en un plazo de 12 meses.

---

## Anexos

### Anexo A: Leyes y Regulaciones Referenciadas

| #   | Ley/Regulación                    | Autoridad Competente   | URL/Referencia           |
| --- | --------------------------------- | ---------------------- | ------------------------ |
| 1   | Ley 172-13 (Datos Personales)     | INDOTEL                | Gaceta Oficial No. 10737 |
| 2   | Ley 126-02 (Comercio Electrónico) | INDOTEL                | Gaceta Oficial No. 10173 |
| 3   | Ley 53-07 (Delitos Tecnológicos)  | Procuraduría General   | Gaceta Oficial No. 10420 |
| 4   | Ley 358-05 (Consumidor)           | ProConsumidor          | Gaceta Oficial No. 10349 |
| 5   | Ley 11-92 (Código Tributario)     | DGII                   | dgii.gov.do              |
| 6   | Ley 155-17 (Lavado de Activos)    | UAF                    | uaf.gob.do               |
| 7   | Ley 20-00 (Propiedad Industrial)  | ONAPI                  | onapi.gov.do             |
| 8   | Ley 65-00 (Derecho de Autor)      | ONDA                   | onda.gob.do              |
| 9   | Ley 1-12 (END 2030)               | MEPyD                  | end.gob.do               |
| 10  | Ley 141-15 (Reestructuración)     | Jurisdicción Comercial | —                        |
| 11  | Ley 153-98 (Telecomunicaciones)   | INDOTEL                | indotel.gob.do           |
| 12  | Ley 241 (Tránsito)                | INTRANT/DGTT           | intrant.gob.do           |

### Anexo B: Archivos del Codebase Auditados

| Archivo                                                                                                            | Relevancia              |
| ------------------------------------------------------------------------------------------------------------------ | ----------------------- |
| `frontend/web-next/src/app/(main)/terminos/page.tsx`                                                               | Términos y Condiciones  |
| `frontend/web-next/src/app/(main)/privacidad/page.tsx`                                                             | Política de Privacidad  |
| `backend/UserService/UserService.Api/Controllers/PrivacyController.cs`                                             | Derechos ARCO           |
| `backend/UserService/UserService.Infrastructure/BackgroundJobs/AccountDeletionWorker.cs`                           | Eliminación de cuentas  |
| `backend/AuthService/AuthService.Api/Helpers/AuthCookieHelper.cs`                                                  | Gestión de cookies auth |
| `backend/TaxComplianceService/TaxComplianceService.Domain/Entities/TaxEntities.cs`                                 | Entidades tributarias   |
| `backend/TaxComplianceService/TaxComplianceService.Api/Controllers/TaxControllers.cs`                              | API tributaria          |
| `backend/Gateway/Gateway.Api/Middleware/CsrfValidationMiddleware.cs`                                               | Protección CSRF         |
| `backend/NotificationService/NotificationService.Infrastructure/Messaging/KYCStatusChangedNotificationConsumer.cs` | Notificaciones KYC      |
| `backend/_Shared/CarDealer.Shared.RateLimiting/Extensions/RateLimitingExtensions.cs`                               | Rate limiting           |

### Anexo C: Contactos de Autoridades Reguladoras

| Autoridad                                      | Teléfono     | Web                  |
| ---------------------------------------------- | ------------ | -------------------- |
| DGII (Dirección General de Impuestos Internos) | 809-689-3444 | dgii.gov.do          |
| ProConsumidor                                  | 809-567-7755 | proconsumidor.gob.do |
| INDOTEL                                        | 809-732-5555 | indotel.gob.do       |
| ONAPI                                          | 809-567-7474 | onapi.gov.do         |
| UAF (Unidad de Análisis Financiero)            | 809-686-0888 | uaf.gob.do           |
| INTRANT                                        | 809-686-6468 | intrant.gob.do       |
| ONDA (Oficina Nacional de Derecho de Autor)    | 809-200-0614 | onda.gob.do          |

---

> **Descargo de responsabilidad:** Este informe tiene fines informativos y de guía interna. No constituye asesoría legal. Se recomienda validar los hallazgos y acciones con un abogado dominicano especializado en derecho digital, protección de datos y derecho comercial antes de su implementación.

---

_Informe generado el 5 de marzo de 2026 — OKLA Legal & Compliance_
