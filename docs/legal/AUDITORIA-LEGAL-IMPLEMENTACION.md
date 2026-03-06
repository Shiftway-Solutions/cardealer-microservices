# 🔍 Auditoría Legal Completa — OKLA Platform

> **Fecha de Auditoría:** 2026-03-05
> **Auditor:** GitHub Copilot (Claude)
> **Documentos Auditados:** 10 documentos legales (01 al 10)

---

## 📋 RESUMEN EJECUTIVO

Se auditaron los 10 documentos legales de la plataforma OKLA. A continuación se lista **todo lo que debe implementarse en la plataforma** (código/infraestructura), separando lo que ya existe de lo que falta.

---

## 1. REQUISITOS LEGALES QUE NECESITAN IMPLEMENTACIÓN EN LA PLATAFORMA

### 🔴 PRIORIDAD ALTA — Implementar Inmediatamente

#### 1.1 Facturación Electrónica (e-CF) — Doc 01

| #   | Requisito                                               | Estado   | Implementación Necesaria                                                              |
| --- | ------------------------------------------------------- | -------- | ------------------------------------------------------------------------------------- |
| 1   | **BillingService** — Nuevo microservicio de facturación | ❌ Falta | Crear microservicio que genere XML e-CF, firme digitalmente y envíe a API DGII        |
| 2   | **Flujo de facturación automática**                     | ❌ Falta | Event handler para `payment.completed` → generar e-CF                                 |
| 3   | **Cálculo de ITBIS (18%)**                              | ❌ Falta | Mostrar precio sin ITBIS + agregar 18% al checkout                                    |
| 4   | **Comprobantes fiscales por email**                     | ❌ Falta | Enviar copia del e-CF al cliente por email tras cada pago                             |
| 5   | **NCF (Número Comprobante Fiscal)**                     | ❌ Falta | Secuencia de NCF por tipo de comprobante                                              |
| 6   | **Tipos de comprobante**                                | ❌ Falta | Soportar tipos 31 (crédito fiscal), 32 (consumo), 33 (nota crédito), 34 (nota débito) |
| 7   | **Conversión USD→RD$**                                  | ❌ Falta | Usar tasa DGII del día para conversión                                                |

#### 1.2 Protección de Datos — Derechos ARCO — Doc 02 & 03

| #   | Requisito                              | Estado     | Implementación Necesaria                                                         |
| --- | -------------------------------------- | ---------- | -------------------------------------------------------------------------------- |
| 8   | **Endpoint Acceso a datos**            | ✅ Existe  | `/api/privacy/my-data/full` — verificar completitud                              |
| 9   | **Endpoint Rectificación**             | ⚠️ Parcial | Verificar que cubra todos los datos personales                                   |
| 10  | **Endpoint Cancelación/Eliminación**   | ✅ Existe  | `/api/privacy/delete-account/confirm` — verificar funcionalidad                  |
| 11  | **Endpoint Oposición**                 | ⚠️ Parcial | Verificar que permita detener tratamiento para fines específicos                 |
| 12  | **Formulario de solicitudes de datos** | ❌ Falta   | Formulario web dedicado para solicitudes ARCO                                    |
| 13  | **Plazos de respuesta automatizados**  | ❌ Falta   | Acceso: 10 días, Rectificación: 5 días, Cancelación: 15 días, Oposición: 10 días |
| 14  | **Página de DPO en el sitio**          | ❌ Falta   | Enlace "Protección de Datos" en footer con datos de contacto del DPO             |
| 15  | **Email dpo@okla.do**                  | ❌ Falta   | Configurar buzón de correo                                                       |

#### 1.3 Evaluación AML/KYC — Doc 04

| #   | Requisito                                           | Estado    | Implementación Necesaria                                                              |
| --- | --------------------------------------------------- | --------- | ------------------------------------------------------------------------------------- |
| 16  | **KYC para vendedores**                             | ✅ Existe | KYCService ya implementado                                                            |
| 17  | **Alertas por actividad inusual**                   | ❌ Falta  | Múltiples listings en corto tiempo, precios anómalos                                  |
| 18  | **Screening de PEPs** (si sujeto obligado)          | ❌ Falta  | Integración con listas de PEPs                                                        |
| 19  | **Monitoreo de transacciones**                      | ❌ Falta  | Alertas por patrones sospechosos (vehículos alto valor, cambios frecuentes de precio) |
| 20  | **Retención de datos 10 años** (si sujeto obligado) | ❌ Falta  | Actualmente retiene 12 meses post-cancelación                                         |

### 🟡 PRIORIDAD MEDIA — Implementar en 1-3 Meses

#### 1.4 Consentimiento Transferencia Internacional — Doc 05

| #   | Requisito                                          | Estado    | Implementación Necesaria                          |
| --- | -------------------------------------------------- | --------- | ------------------------------------------------- |
| 21  | **Checkbox de consentimiento en registro**         | ✅ Existe | Ya implementado                                   |
| 22  | **Texto informativo sobre servidores en EE.UU.**   | ✅ Existe | Ya implementado                                   |
| 23  | **Cifrado TLS 1.3 y AES-256**                      | ✅ Existe | Ya implementado                                   |
| 24  | **Política de privacidad — sección transferencia** | ❌ Falta  | Actualizar con detalles de DigitalOcean, SCC, DPA |

#### 1.5 ProConsumidor — Doc 06

| #   | Requisito                               | Estado   | Implementación Necesaria                                                                                           |
| --- | --------------------------------------- | -------- | ------------------------------------------------------------------------------------------------------------------ |
| 25  | **Libro de reclamaciones digital**      | ❌ Falta | Formulario accesible desde footer con: datos reclamante, descripción, fecha, solución deseada, subida de evidencia |
| 26  | **Acuse de recibo automático**          | ❌ Falta | Número de caso + email de confirmación                                                                             |
| 27  | **Página de Derechos del Consumidor**   | ❌ Falta | Sección informativa con derechos Ley 358-05                                                                        |
| 28  | **Badge/Sello ProConsumidor en footer** | ❌ Falta | Texto o sello oficial                                                                                              |
| 29  | **Política de reembolso publicada**     | ❌ Falta | Condiciones, proceso, plazo, medio de reembolso                                                                    |
| 30  | **Desglose de ITBIS en precios**        | ❌ Falta | Todos los precios deben mostrar ITBIS separado                                                                     |

#### 1.6 Autorización SMS — Doc 08

| #   | Requisito                                | Estado   | Implementación Necesaria                                               |
| --- | ---------------------------------------- | -------- | ---------------------------------------------------------------------- |
| 31  | **Opt-In explícito para SMS marketing**  | ❌ Falta | Checkbox separado, no premarcado, con texto legal                      |
| 32  | **Opt-Out (responder BAJA)**             | ❌ Falta | Cada SMS marketing incluye instrucción de baja                         |
| 33  | **Preferencias de notificaciones SMS**   | ❌ Falta | Panel en perfil de usuario para activar/desactivar tipos de SMS        |
| 34  | **Base de datos de consentimientos SMS** | ❌ Falta | Tabla con: UserId, PhoneNumber, ConsentDate, ConsentMethod, OptOutDate |
| 35  | **Lista negra (opt-out)**                | ❌ Falta | Verificar contra lista antes de cada envío                             |
| 36  | **Restricción de horario**               | ❌ Falta | Marketing solo 8AM-8PM, lunes a sábado                                 |

### 🟢 PRIORIDAD BAJA — Implementar en 3-6 Meses

#### 1.7 Procedimiento Judicial — Doc 09

| #   | Requisito                              | Estado       | Implementación Necesaria                                                          |
| --- | -------------------------------------- | ------------ | --------------------------------------------------------------------------------- |
| 37  | **Retención de logs ≥90 días**         | ⚠️ Verificar | Verificar configuración actual de ErrorService                                    |
| 38  | **Herramienta de extracción de datos** | ❌ Falta     | Endpoint admin para extraer datos de usuario específico por orden judicial        |
| 39  | **Litigation Hold (preservación)**     | ❌ Falta     | Mecanismo para marcar datos como "preservados" y suspender eliminación automática |
| 40  | **Log de requerimientos judiciales**   | ❌ Falta     | Registro interno de todas las órdenes judiciales recibidas                        |

#### 1.8 Plan de Contingencia de Datos — Doc 10

| #   | Requisito                                      | Estado     | Implementación Necesaria                                                                          |
| --- | ---------------------------------------------- | ---------- | ------------------------------------------------------------------------------------------------- |
| 41  | **Herramienta de exportación masiva de datos** | ⚠️ Parcial | `/api/privacy/my-data/full` existe, pero verificar si genera archivos descargables (JSON/CSV/ZIP) |
| 42  | **Página informativa de cierre**               | ❌ Falta   | Template pre-preparado para activar si hay cierre                                                 |
| 43  | **Proceso de eliminación segura**              | ❌ Falta   | Procedimiento documentado de DROP + sobrescritura                                                 |
| 44  | **Email templates de notificación de cierre**  | ❌ Falta   | Templates para fases de cierre                                                                    |

#### 1.9 Registro de Marca ONAPI — Doc 07

| #   | Requisito                             | Estado | Implementación Necesaria                   |
| --- | ------------------------------------- | ------ | ------------------------------------------ |
| 45  | **No requiere cambios en plataforma** | ✅ N/A | Es un proceso administrativo/legal externo |

---

## 2. PLAN DE IMPLEMENTACIÓN

### Fase 1: Urgente (Semanas 1-4) — 🔴 ALTA

#### Sprint 1 (Semana 1-2): Facturación Electrónica Base

- [ ] Crear `BillingService` con Clean Architecture
- [ ] Implementar generación de XML e-CF
- [ ] Implementar firma digital XAdES-BES
- [ ] Configurar conexión con API DGII (ambiente de pruebas)
- [ ] Crear event handler para `payment.completed`
- [ ] Implementar cálculo de ITBIS (18%)
- [ ] Implementar secuencia de NCF por tipo
- [ ] Crear endpoint para consultar comprobantes
- [ ] Enviar copia de e-CF por email (NotificationService)

#### Sprint 2 (Semana 3-4): Compliance de Datos & AML

- [ ] Implementar formulario web de solicitudes ARCO
- [ ] Configurar plazos automáticos de respuesta ARCO
- [ ] Crear página de DPO con datos de contacto (footer)
- [ ] Implementar alertas por actividad inusual en listings
- [ ] Agregar monitoreo de patrones sospechosos (precios anómalos, múltiples listings)
- [ ] Verificar y ajustar retención de logs ≥90 días

### Fase 2: Media (Semanas 5-12) — 🟡 MEDIA

#### Sprint 3 (Semana 5-6): ProConsumidor & Transparencia

- [ ] Crear libro de reclamaciones digital (formulario + backend)
- [ ] Implementar sistema de acuse de recibo automático
- [ ] Crear página de Derechos del Consumidor
- [ ] Agregar badge/sello ProConsumidor en footer
- [ ] Publicar política de reembolso
- [ ] Implementar desglose de ITBIS en todos los precios

#### Sprint 4 (Semana 7-8): Consentimiento SMS & Preferencias

- [ ] Implementar Opt-In explícito para SMS marketing (checkbox separado)
- [ ] Implementar Opt-Out (responder BAJA, procesar inmediatamente)
- [ ] Crear panel de preferencias de notificaciones SMS en perfil
- [ ] Crear tabla `SmsConsents` con campos legales requeridos
- [ ] Implementar lista negra de opt-out
- [ ] Configurar restricción de horario (8AM-8PM, L-S)

#### Sprint 5 (Semana 9-10): Actualización Política de Privacidad

- [ ] Actualizar sección de transferencia internacional de datos
- [ ] Agregar detalles de DPA con DigitalOcean
- [ ] Documentar salvaguardas implementadas
- [ ] Agregar sección de cese de operaciones

#### Sprint 6 (Semana 11-12): Certificación e-CF

- [ ] Completar 20+ comprobantes de prueba con DGII
- [ ] Resolver observaciones de DGII
- [ ] Activar ambiente de producción
- [ ] Emitir primera factura electrónica real

### Fase 3: Complementaria (Semanas 13-24) — 🟢 BAJA

#### Sprint 7 (Semana 13-14): Herramientas Judiciales

- [ ] Crear endpoint admin para extracción de datos por usuario
- [ ] Implementar mecanismo de Litigation Hold
- [ ] Crear log de requerimientos judiciales
- [ ] Documentar procedimiento interno

#### Sprint 8 (Semana 15-16): Plan de Contingencia

- [ ] Mejorar herramienta de exportación masiva (JSON/CSV/ZIP)
- [ ] Preparar template de página de cierre
- [ ] Crear email templates de notificación de cierre
- [ ] Documentar procedimiento de eliminación segura

#### Sprint 9-12 (Semana 17-24): Screening AML Avanzado (si aplica)

- [ ] Integrar servicio de screening de PEPs (World-Check/Dow Jones)
- [ ] Implementar Due Diligence Reforzada (EDD)
- [ ] Ajustar retención de datos a 10 años (si sujeto obligado)
- [ ] Implementar sistema de ROS (Reporte Operaciones Sospechosas)

---

## 3. RESUMEN DE COMPONENTES TÉCNICOS A CREAR/MODIFICAR

### Nuevos Microservicios

| Servicio           | Propósito                                |
| ------------------ | ---------------------------------------- |
| **BillingService** | Facturación electrónica e-CF, ITBIS, NCF |

### Modificaciones a Servicios Existentes

| Servicio                | Cambios                                                   |
| ----------------------- | --------------------------------------------------------- |
| **AuthService**         | Agregar campos de consentimiento SMS, mejorar ARCO        |
| **NotificationService** | Opt-in/opt-out SMS, horarios, lista negra, templates e-CF |
| **ErrorService**        | Verificar retención ≥90 días, litigation hold             |
| **AdminService**        | Herramienta de extracción judicial, log de requerimientos |
| **ContactService**      | Libro de reclamaciones digital (ProConsumidor)            |
| **MediaService**        | Exportación masiva de archivos del usuario                |

### Frontend (Next.js)

| Página/Componente              | Propósito                                                 |
| ------------------------------ | --------------------------------------------------------- |
| Footer — "Protección de Datos" | Enlace a DPO, derechos, ProConsumidor                     |
| Formulario ARCO                | Solicitudes de acceso/rectificación/cancelación/oposición |
| Libro de Reclamaciones         | Formulario ProConsumidor                                  |
| Derechos del Consumidor        | Página informativa Ley 358-05                             |
| Preferencias SMS               | Panel en perfil de usuario                                |
| Checkout — ITBIS               | Desglose de impuestos                                     |
| Política de Reembolso          | Nueva página                                              |
| Política de Privacidad         | Actualización sección transferencia internacional         |

### Base de Datos (Nuevas Tablas)

| Tabla              | Servicio            | Campos Clave                                       |
| ------------------ | ------------------- | -------------------------------------------------- |
| `Invoices` (e-CF)  | BillingService      | NCF, tipo, monto, ITBIS, XML, TrackId DGII         |
| `SmsConsents`      | NotificationService | UserId, PhoneNumber, ConsentDate, OptOutDate       |
| `SmsBlacklist`     | NotificationService | PhoneNumber, OptOutDate, OptOutMethod              |
| `Complaints`       | ContactService      | CaseNumber, Description, Status, Resolution        |
| `JudicialRequests` | AdminService        | CaseNumber, Authority, DataRequested, DateReceived |
| `LitigationHolds`  | AdminService        | UserId, HoldReason, StartDate, EndDate             |
| `ArcoRequests`     | AuthService/Privacy | Type, Status, RequestDate, DueDate, CompletedDate  |

---

## 4. ESTIMACIÓN DE ESFUERZO TOTAL

| Fase                    | Duración       | Esfuerzo             |
| ----------------------- | -------------- | -------------------- |
| Fase 1 — Urgente        | 4 semanas      | ~320 horas dev       |
| Fase 2 — Media          | 8 semanas      | ~480 horas dev       |
| Fase 3 — Complementaria | 12 semanas     | ~360 horas dev       |
| **TOTAL**               | **24 semanas** | **~1,160 horas dev** |

---

## 5. PROCESOS ADMINISTRATIVOS (No requieren código)

Estos son trámites legales/administrativos que se ejecutan en paralelo:

1. ✅ **Registro RNC/DGII** — Verificar/obtener RNC
2. ❌ **Designar DPO** — Contratar consultor externo o interno
3. ❌ **Registrar bases de datos ante INDOTEL** — Con fichas por servicio
4. ❌ **Consultar UAF** — Determinar si OKLA es sujeto obligado
5. ❌ **Firmar DPA con DigitalOcean** — Aceptar Data Processing Agreement
6. ❌ **Consultar ProConsumidor** — Verificar obligación de registro
7. ❌ **Registrar marca ONAPI** — Clases 35, 42, 12
8. ❌ **Consultar INDOTEL** — Sobre SMS marketing
9. ❌ **Preparar procedimiento judicial** — Documentación interna
10. ❌ **Aprobar plan de contingencia** — Revisión legal del plan
