# 📋 Normativas de República Dominicana - Cumplimiento OKLA

> **Documento:** Marco Legal y Regulatorio  
> **Plataforma:** OKLA (Marketplace de Vehículos)  
> **Última actualización:** Enero 25, 2026  
> **Estado:** 🟡 EN CUMPLIMIENTO PARCIAL

---

## 📑 Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Leyes Obligatorias](#leyes-obligatorias)
3. [Ley 155-17 - Lavado de Activos](#1-ley-155-17---lavado-de-activos-amlpld)
4. [Ley 172-13 - Protección de Datos](#2-ley-172-13---protección-de-datos-personales)
5. [Ley 358-05 - Protección al Consumidor](#3-ley-358-05---protección-al-consumidor)
6. [DGII - Obligaciones Fiscales](#4-dgii---obligaciones-fiscales)
7. [Ley 126-02 - Comercio Electrónico](#5-ley-126-02---comercio-electrónico)
8. [Otras Normativas](#6-otras-normativas-aplicables)
9. [Estado de Cumplimiento](#estado-de-cumplimiento)
10. [Calendario de Vencimientos](#calendario-de-vencimientos)
11. [Prioridades Críticas](#prioridades-críticas)
12. [Contactos Reguladores](#contactos-de-reguladores)

---

## Resumen Ejecutivo

OKLA, como plataforma de comercio electrónico de vehículos en República Dominicana, está sujeta a **6 leyes principales** y múltiples normativas complementarias. Este documento detalla cada obligación legal y el estado actual de implementación.

### Leyes Obligatorias

| #   | Ley/Norma              | Regulador         | Descripción                               | Implementación |
| --- | ---------------------- | ----------------- | ----------------------------------------- | -------------- |
| 1   | **Ley 155-17**         | UAF               | Prevención de Lavado de Activos (PLD/AML) | ✅ 80% Backend |
| 2   | **Ley 172-13**         | SB / Procuraduría | Protección de Datos Personales            | 🟡 60% Backend |
| 3   | **Ley 358-05**         | Pro Consumidor    | Protección al Consumidor                  | 🟡 40% Backend |
| 4   | **Ley 11-92 / 253-12** | DGII              | Código Tributario y NCF                   | 🟡 50% Backend |
| 5   | **Ley 126-02**         | INDOTEL           | Comercio Electrónico                      | 🟡 70% Backend |
| 6   | **Ley 63-17**          | INTRANT           | Registro Vehicular                        | 🔴 Pendiente   |

---

## 1. Ley 155-17 - Lavado de Activos (AML/PLD)

### 1.1 Información General

| Campo              | Valor                                                              |
| ------------------ | ------------------------------------------------------------------ |
| **Nombre Oficial** | Ley contra el Lavado de Activos y el Financiamiento del Terrorismo |
| **Número**         | 155-17                                                             |
| **Fecha**          | 1 de junio de 2017                                                 |
| **Regulador**      | Unidad de Análisis Financiero (UAF)                                |
| **Aplica a OKLA**  | ✅ Sí (transacciones de alto valor)                                |

### 1.2 Obligaciones de OKLA

| Obligación       | Descripción                                         | Umbral                    | Estado          |
| ---------------- | --------------------------------------------------- | ------------------------- | --------------- |
| **KYC**          | Conoce a Tu Cliente - Identificación y verificación | Todos los usuarios        | ✅ Implementado |
| **DDC**          | Debida Diligencia del Cliente                       | Transacciones > RD$50,000 | ✅ Implementado |
| **EDD**          | Debida Diligencia Reforzada                         | PEPs y alto riesgo        | ✅ Backend OK   |
| **ROS**          | Reporte de Operaciones Sospechosas                  | Cualquier sospecha        | ✅ Backend OK   |
| **Umbral**       | Reporte automático a la UAF                         | > RD$500,000              | 🟡 Parcial      |
| **Registros**    | Conservar documentos                                | 10 años                   | ✅ AuditService |
| **Oficial PLD**  | Designar responsable de cumplimiento                | Obligatorio               | ⚠️ Pendiente    |
| **Capacitación** | Entrenar empleados en PLD                           | Anual                     | 🔴 Pendiente    |

### 1.3 Niveles de Due Diligence

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    NIVELES DE DUE DILIGENCE                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   NIVEL SIMPLIFICADO (SDD)                                              │
│   ├── Transacciones < RD$50,000                                         │
│   ├── Clientes de bajo riesgo                                           │
│   └── Verificación básica: Cédula/RNC                                   │
│                                                                         │
│   NIVEL ESTÁNDAR (CDD)                                                  │
│   ├── Transacciones RD$50,000 - RD$500,000                              │
│   ├── Verificación completa de identidad                                │
│   ├── Origen de fondos                                                  │
│   └── Propósito de la transacción                                       │
│                                                                         │
│   NIVEL REFORZADO (EDD)                                                 │
│   ├── Transacciones > RD$500,000                                        │
│   ├── Personas Políticamente Expuestas (PEPs)                           │
│   ├── Países de alto riesgo                                             │
│   ├── Investigación profunda de origen de fondos                        │
│   └── Aprobación de alta gerencia                                       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 1.4 Señales de Alerta (Red Flags)

| Señal                               | Descripción                    | Acción              |
| ----------------------------------- | ------------------------------ | ------------------- |
| 🔴 Pago en efectivo > RD$500K       | Transacción grande en efectivo | Reportar a UAF      |
| 🔴 Múltiples transacciones pequeñas | Estructuración (smurfing)      | Investigar patrón   |
| 🔴 Discrepancia de información      | Datos inconsistentes           | Verificar identidad |
| 🔴 PEP involucrado                  | Persona políticamente expuesta | Aplicar EDD         |
| 🔴 País de alto riesgo              | Origen en país sancionado      | Rechazar o EDD      |
| 🔴 Prisa inusual                    | Urgencia injustificada         | Investigar motivo   |
| 🔴 Tercero como beneficiario        | Vehículo a nombre de otro      | Verificar relación  |

### 1.5 Implementación en OKLA

| Componente                 | Servicio Backend            | Estado  |
| -------------------------- | --------------------------- | ------- |
| Verificación KYC           | `KYCService`                | ✅ 100% |
| Due Diligence              | `KYCService`                | ✅ 100% |
| Monitoreo de transacciones | `TransactionMonitorService` | 🟡 70%  |
| Reportes UAF               | `ComplianceService`         | ✅ 80%  |
| Watchlist screening        | `WatchlistService`          | 🟡 60%  |
| Audit Trail                | `AuditService`              | ✅ 100% |

---

## 2. Ley 172-13 - Protección de Datos Personales

### 2.1 Información General

| Campo                         | Valor                                                       |
| ----------------------------- | ----------------------------------------------------------- |
| **Nombre Oficial**            | Ley Orgánica sobre Protección de Datos de Carácter Personal |
| **Número**                    | 172-13                                                      |
| **Fecha**                     | 13 de diciembre de 2013                                     |
| **Regulador**                 | Superintendencia de Bancos / Procuraduría General           |
| **Equivalente Internacional** | Similar al GDPR europeo                                     |
| **Aplica a OKLA**             | ✅ Sí (procesa datos personales)                            |

### 2.2 Definiciones Clave

| Término              | Definición                             | Rol de OKLA |
| -------------------- | -------------------------------------- | ----------- |
| **Datos personales** | Información de persona identificable   | Procesa     |
| **Titular**          | Dueño de los datos                     | Usuario     |
| **Responsable**      | Decide finalidad del tratamiento       | **OKLA**    |
| **Encargado**        | Trata datos por cuenta del responsable | Proveedores |
| **Tratamiento**      | Cualquier operación sobre datos        | Múltiples   |
| **Consentimiento**   | Manifestación libre e informada        | Requerido   |

### 2.3 Derechos ARCO del Titular

| Derecho           | Descripción                   | Plazo de Respuesta | Estado OKLA     |
| ----------------- | ----------------------------- | ------------------ | --------------- |
| **A**cceso        | Ver todos mis datos           | 10 días hábiles    | ✅ ProfilePage  |
| **R**ectificación | Corregir datos incorrectos    | 10 días hábiles    | ✅ SettingsPage |
| **C**ancelación   | Eliminar mi cuenta y datos    | 15 días hábiles    | 🟡 Manual       |
| **O**posición     | No recibir comunicaciones     | Inmediato          | 🟡 Parcial      |
| **Portabilidad**  | Exportar mis datos (JSON/CSV) | 15 días hábiles    | 🔴 Pendiente    |

### 2.4 Datos Sensibles (Requieren Consentimiento Expreso)

| Categoría               | Ejemplos                | ¿OKLA Recolecta?         |
| ----------------------- | ----------------------- | ------------------------ |
| Origen racial/étnico    | Nacionalidad, etnia     | ❌ No                    |
| Opiniones políticas     | Afiliación partidaria   | ❌ No                    |
| Convicciones religiosas | Religión, creencias     | ❌ No                    |
| Afiliación sindical     | Membresía en sindicatos | ❌ No                    |
| Datos de salud          | Condiciones médicas     | ❌ No                    |
| Vida sexual             | Orientación sexual      | ❌ No                    |
| Datos biométricos       | Huellas, facial         | 🟡 Opcional (selfie KYC) |
| Datos financieros       | Ingresos, cuentas       | ✅ Sí (verificación)     |

### 2.5 Obligaciones de OKLA como Responsable

| Obligación                  | Descripción                          | Estado                    |
| --------------------------- | ------------------------------------ | ------------------------- |
| **Consentimiento**          | Obtener antes de procesar            | ✅ CheckBox en registro   |
| **Finalidad**               | Usar solo para fines declarados      | ✅ Política de privacidad |
| **Proporcionalidad**        | No recolectar más de lo necesario    | ✅ Campos mínimos         |
| **Calidad**                 | Mantener datos actualizados          | ✅ Usuario puede editar   |
| **Seguridad**               | Proteger contra acceso no autorizado | ✅ Encriptación           |
| **Notificación de brechas** | Avisar en 72 horas                   | 🟡 Proceso manual         |
| **Registro de actividades** | Documentar tratamientos              | ✅ AuditService           |

### 2.6 Implementación en OKLA

| Componente           | Servicio Backend | UI                 | Estado    |
| -------------------- | ---------------- | ------------------ | --------- |
| Consentimiento       | `AuthService`    | `/register`        | ✅ 100%   |
| Ver mis datos        | `UserService`    | `/profile`         | ✅ 100%   |
| Editar datos         | `UserService`    | `/settings`        | ✅ 100%   |
| Eliminar cuenta      | `UserService`    | `/settings/delete` | 🟡 Manual |
| Exportar datos       | ❌ No existe     | ❌ No existe       | 🔴 0%     |
| Centro de privacidad | ❌ No existe     | ❌ No existe       | 🔴 0%     |

---

## 3. Ley 358-05 - Protección al Consumidor

### 3.1 Información General

| Campo              | Valor                                                                            |
| ------------------ | -------------------------------------------------------------------------------- |
| **Nombre Oficial** | Ley General de Protección de los Derechos del Consumidor                         |
| **Número**         | 358-05                                                                           |
| **Fecha**          | 9 de septiembre de 2005                                                          |
| **Regulador**      | Pro Consumidor (Instituto Nacional de Protección de los Derechos del Consumidor) |
| **Aplica a OKLA**  | ✅ Sí (facilita comercio de bienes)                                              |

### 3.2 Derechos Fundamentales del Consumidor

| Derecho            | Descripción                          | Implementación OKLA       | Estado |
| ------------------ | ------------------------------------ | ------------------------- | ------ |
| **Información**    | Conocer características del producto | Ficha técnica detallada   | ✅     |
| **Elección**       | Libertad de escoger                  | Sin cláusulas abusivas    | ✅     |
| **Seguridad**      | Productos seguros                    | Verificación de vehículos | 🟡     |
| **Indemnización**  | Compensación por daños               | Proceso de disputas       | 🔴     |
| **Representación** | Ser escuchado                        | Soporte 24/7              | ✅     |
| **Educación**      | Información clara                    | Guías de compra           | 🟡     |

### 3.3 Obligaciones de OKLA

| Obligación                    | Descripción                        | Estado               |
| ----------------------------- | ---------------------------------- | -------------------- |
| **Transparencia de precios**  | Mostrar precio total con impuestos | ✅ Implementado      |
| **Información veraz**         | No permitir publicidad engañosa    | ✅ Moderación activa |
| **Derecho de retracto**       | 48 horas para cancelar servicios   | 🔴 No implementado   |
| **Garantía mínima**           | Facilitar reclamos de garantía     | 🟡 Parcial           |
| **Canal de quejas**           | Atención al cliente accesible      | 🔴 Sin formulario    |
| **Registro de transacciones** | Mantener por 3 años                | ✅ AuditService      |
| **Facturación**               | Entregar comprobante fiscal        | 🟡 NCF parcial       |

### 3.4 Información Obligatoria en Anuncios de Vehículos

| Campo                       | Descripción                      | Obligatorio    |
| --------------------------- | -------------------------------- | -------------- |
| **Marca y modelo**          | Identificación del vehículo      | ✅ Sí          |
| **Año de fabricación**      | Año de manufactura               | ✅ Sí          |
| **Kilometraje**             | Lectura actual del odómetro      | ✅ Sí          |
| **Precio**                  | Precio en pesos dominicanos      | ✅ Sí          |
| **Condición**               | Nuevo, usado, certificado        | ✅ Sí          |
| **Ubicación**               | Dónde está el vehículo           | ✅ Sí          |
| **Contacto**                | Cómo contactar al vendedor       | ✅ Sí          |
| **Historial de accidentes** | Si ha tenido siniestros          | 🟡 Recomendado |
| **Número de dueños**        | Cantidad de propietarios previos | 🟡 Recomendado |

### 3.5 Implementación en OKLA

| Componente            | Servicio Backend      | UI              | Estado  |
| --------------------- | --------------------- | --------------- | ------- |
| Ficha de vehículo     | `VehiclesSaleService` | `/vehicles/:id` | ✅ 100% |
| Precios transparentes | `BillingService`      | Checkout        | ✅ 100% |
| Formulario de quejas  | ❌ No existe          | ❌ No existe    | 🔴 0%   |
| Reclamos de garantía  | ❌ No existe          | ❌ No existe    | 🔴 0%   |
| Derecho de retracto   | ❌ No existe          | ❌ No existe    | 🔴 0%   |
| Centro de ayuda       | `SupportService`      | `/help`         | 🟡 50%  |

---

## 4. DGII - Obligaciones Fiscales

### 4.1 Marco Regulatorio

| Ley/Norma         | Descripción                                  |
| ----------------- | -------------------------------------------- |
| **Ley 11-92**     | Código Tributario de la República Dominicana |
| **Ley 253-12**    | Ley sobre Comprobantes Fiscales              |
| **Norma 06-2018** | Factura Electrónica (e-CF)                   |
| **Norma 08-2019** | Secuencias de NCF                            |

### 4.2 Obligaciones Fiscales de OKLA

| Obligación                    | Frecuencia | Plazo                    | Estado       |
| ----------------------------- | ---------- | ------------------------ | ------------ |
| **Declaración ITBIS (IT-1)**  | Mensual    | Día 20 del mes siguiente | 🔴 Manual    |
| **Formato 606** (Compras)     | Mensual    | Día 15 del mes siguiente | 🔴 Pendiente |
| **Formato 607** (Ventas)      | Mensual    | Día 15 del mes siguiente | 🔴 Pendiente |
| **Formato 608** (Anulaciones) | Mensual    | Día 15 del mes siguiente | 🔴 Pendiente |
| **Declaración ISR**           | Anual      | 30 de Abril              | 🔴 Manual    |
| **Retenciones IR-17**         | Mensual    | Día 10 del mes siguiente | 🔴 Manual    |

### 4.3 Comprobantes Fiscales (NCF)

| Tipo NCF                       | Código  | Uso en OKLA                     |
| ------------------------------ | ------- | ------------------------------- |
| **Factura de Crédito Fiscal**  | B01     | Ventas a empresas (Dealers)     |
| **Factura de Consumo**         | B02     | Ventas a consumidores finales   |
| **Nota de Débito**             | B03     | Ajustes que aumentan el monto   |
| **Nota de Crédito**            | B04     | Devoluciones y ajustes          |
| **Compras**                    | B11     | Proveedores informales          |
| **Registro Único de Ingresos** | B13     | Consolidación de ventas menores |
| **Regímenes Especiales**       | B14     | Exportaciones, zonas francas    |
| **Gubernamental**              | B15     | Ventas al gobierno              |
| **Factura Electrónica**        | E31/E32 | Formato digital certificado     |

### 4.4 Estructura del NCF

```
NCF: B0100000001
     ││└──────── Secuencial (8 dígitos)
     │└───────── Tipo de comprobante (01 = Crédito Fiscal)
     └────────── Prefijo obligatorio (B)
```

### 4.5 Implementación en OKLA

| Componente          | Servicio Backend    | UI                 | Estado  |
| ------------------- | ------------------- | ------------------ | ------- |
| Validación RNC      | `KYCService`        | `/dealer/register` | ✅ 100% |
| Generación NCF      | `InvoicingService`  | ❌ No existe       | 🟡 50%  |
| Formato 606         | ❌ No existe        | ❌ No existe       | 🔴 0%   |
| Formato 607         | ❌ No existe        | ❌ No existe       | 🔴 0%   |
| Factura Electrónica | ❌ No existe        | ❌ No existe       | 🔴 0%   |
| Reportes DGII       | `ComplianceService` | ❌ No existe       | 🟡 40%  |

---

## 5. Ley 126-02 - Comercio Electrónico

### 5.1 Información General

| Campo              | Valor                                                      |
| ------------------ | ---------------------------------------------------------- |
| **Nombre Oficial** | Ley de Comercio Electrónico, Documentos y Firmas Digitales |
| **Número**         | 126-02                                                     |
| **Fecha**          | 29 de julio de 2002                                        |
| **Regulador**      | INDOTEL                                                    |
| **Aplica a OKLA**  | ✅ Sí (plataforma de comercio electrónico)                 |

### 5.2 Requisitos de la Ley

| Requisito                        | Descripción                       | Estado OKLA        |
| -------------------------------- | --------------------------------- | ------------------ |
| **Identificación del proveedor** | Nombre, RNC, dirección, contacto  | ✅ Footer y About  |
| **Términos y Condiciones**       | Condiciones de uso publicadas     | ✅ `/terms`        |
| **Política de Privacidad**       | Tratamiento de datos personales   | ✅ `/privacy`      |
| **Precios claros**               | Incluir impuestos y cargos        | ✅ Implementado    |
| **Confirmación de pedido**       | Email con detalles de transacción | ✅ Implementado    |
| **Derecho de desistimiento**     | Información sobre cancelación     | 🟡 Parcial         |
| **Seguridad de pagos**           | Cifrado y protección              | ✅ HTTPS + Stripe  |
| **Firma digital**                | Para contratos electrónicos       | 🔴 No implementado |

### 5.3 Validez de Documentos Electrónicos

| Documento                | Validez Legal     | Implementación OKLA |
| ------------------------ | ----------------- | ------------------- |
| Contratos de suscripción | ✅ Válido         | Aceptación digital  |
| Facturas electrónicas    | ✅ Válido con NCF | 🟡 Parcial          |
| Comunicaciones oficiales | ✅ Válido         | Email               |
| Contratos de compraventa | ⚠️ Requiere firma | 🔴 No implementado  |

---

## 6. Otras Normativas Aplicables

### 6.1 Ley 63-17 - INTRANT (Registro Vehicular)

| Aspecto                        | Descripción                          | Estado OKLA  |
| ------------------------------ | ------------------------------------ | ------------ |
| **Verificación de matrícula**  | Validar que vehículo está registrado | 🔴 Pendiente |
| **Historial de propietarios**  | Consultar cadena de titularidad      | 🔴 Pendiente |
| **Multas pendientes**          | Verificar deudas del vehículo        | 🔴 Pendiente |
| **Estado de revisión técnica** | Vigencia de inspección               | 🔴 Pendiente |

### 6.2 Normativas de Seguros

| Aspecto                          | Descripción              | Estado OKLA        |
| -------------------------------- | ------------------------ | ------------------ |
| **Seguro obligatorio**           | Verificar póliza vigente | 🔴 No implementado |
| **Partnership con aseguradoras** | Ofrecer seguros          | 🔴 Pendiente       |

### 6.3 Código de Trabajo (si hay empleados)

| Obligación            | Frecuencia | Estado     |
| --------------------- | ---------- | ---------- |
| **Nómina TSS**        | Mensual    | ✅ Externo |
| **Aportes AFP**       | Mensual    | ✅ Externo |
| **Aportes ARS**       | Mensual    | ✅ Externo |
| **Riesgos Laborales** | Mensual    | ✅ Externo |

---

## Estado de Cumplimiento

### Resumen General

| Normativa               | Backend | UI     | Estado General | Riesgo   |
| ----------------------- | ------- | ------ | -------------- | -------- |
| Ley 155-17 (AML)        | ✅ 80%  | 🔴 0%  | ⚠️ 40%         | 🔴 Alto  |
| Ley 172-13 (Datos)      | 🟡 60%  | 🟡 40% | 🟡 50%         | 🟡 Medio |
| Ley 358-05 (Consumidor) | 🟡 40%  | 🟡 30% | ⚠️ 35%         | 🟡 Medio |
| DGII (Fiscal)           | 🟡 50%  | 🔴 0%  | 🔴 25%         | 🔴 Alto  |
| Ley 126-02 (E-Commerce) | 🟡 70%  | ✅ 80% | ✅ 75%         | 🟢 Bajo  |
| Ley 63-17 (INTRANT)     | 🔴 0%   | 🔴 0%  | 🔴 0%          | 🟡 Medio |

### Detalle por Ley

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ESTADO DE CUMPLIMIENTO                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   Ley 155-17 (AML)        ████████░░░░░░░░░░░░  40%  ⚠️ RIESGO ALTO   │
│   ├── Backend             ████████████████░░░░  80%  ✅               │
│   └── UI (Dashboard)      ░░░░░░░░░░░░░░░░░░░░   0%  🔴               │
│                                                                         │
│   Ley 172-13 (Datos)      ██████████░░░░░░░░░░  50%  🟡 RIESGO MEDIO  │
│   ├── Backend             ████████████░░░░░░░░  60%  🟡               │
│   └── UI (Privacy Center) ████████░░░░░░░░░░░░  40%  🟡               │
│                                                                         │
│   Ley 358-05 (Consumidor) ███████░░░░░░░░░░░░░  35%  ⚠️ RIESGO MEDIO  │
│   ├── Backend             ████████░░░░░░░░░░░░  40%  🟡               │
│   └── UI (Quejas)         ██████░░░░░░░░░░░░░░  30%  🟡               │
│                                                                         │
│   DGII (Fiscal)           █████░░░░░░░░░░░░░░░  25%  🔴 RIESGO ALTO   │
│   ├── Backend             ██████████░░░░░░░░░░  50%  🟡               │
│   └── UI (606/607)        ░░░░░░░░░░░░░░░░░░░░   0%  🔴               │
│                                                                         │
│   Ley 126-02 (E-Commerce) ███████████████░░░░░  75%  ✅ RIESGO BAJO   │
│   ├── Backend             ██████████████░░░░░░  70%  🟡               │
│   └── UI (Términos)       ████████████████░░░░  80%  ✅               │
│                                                                         │
│   Ley 63-17 (INTRANT)     ░░░░░░░░░░░░░░░░░░░░   0%  🟡 RIESGO MEDIO  │
│   ├── Backend             ░░░░░░░░░░░░░░░░░░░░   0%  🔴               │
│   └── UI (Verificación)   ░░░░░░░░░░░░░░░░░░░░   0%  🔴               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Calendario de Vencimientos

### Vencimientos Mensuales

| Día        | Obligación                | Destino | Responsable  |
| ---------- | ------------------------- | ------- | ------------ |
| **10**     | Retenciones IR-17         | DGII    | Contabilidad |
| **15**     | Formato 606 (Compras)     | DGII    | Contabilidad |
| **15**     | Formato 607 (Ventas)      | DGII    | Contabilidad |
| **15**     | Formato 608 (Anulaciones) | DGII    | Contabilidad |
| **20**     | Declaración ITBIS (IT-1)  | DGII    | Contabilidad |
| **Último** | Nómina TSS                | TSS     | RRHH         |
| **Último** | Reportes AML (si aplica)  | UAF     | Compliance   |

### Vencimientos Anuales

| Fecha          | Obligación       | Destino          | Responsable  |
| -------------- | ---------------- | ---------------- | ------------ |
| **30 Abril**   | Declaración ISR  | DGII             | Contabilidad |
| **31 Marzo**   | Capacitación PLD | Interno          | Compliance   |
| **Trimestral** | Reporte AML      | Superintendencia | Compliance   |

### Recordatorios Automáticos

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    CALENDARIO MENSUAL DGII                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   L   M   M   J   V   S   D                                             │
│   ─   ─   ─   ─   ─   ─   ─                                             │
│   1   2   3   4   5   6   7                                             │
│   8   9  🔴  11  12  13  14    ← Día 10: IR-17                         │
│  🔴 🔴  17  18  19 🔴  21    ← Días 15-16: 606/607/608                 │
│  22  23  24  25  26  27  28    ← Día 20: ITBIS                         │
│  29  30  31                                                             │
│                                                                         │
│   🔴 = Fecha límite                                                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Prioridades Críticas

### 🔴 Prioridad Alta (Riesgo Legal Inminente)

| #   | Tarea                              | Normativa  | Responsable | Plazo     |
| --- | ---------------------------------- | ---------- | ----------- | --------- |
| 1   | **Dashboard de Compliance**        | Ley 155-17 | Dev Team    | 2 semanas |
| 2   | **Generador Formato 606**          | DGII       | Dev Team    | 2 semanas |
| 3   | **Generador Formato 607**          | DGII       | Dev Team    | 2 semanas |
| 4   | **Formulario de Quejas**           | Ley 358-05 | Dev Team    | 1 semana  |
| 5   | **Alertas transacciones >RD$500K** | Ley 155-17 | Dev Team    | 1 semana  |

### 🟡 Prioridad Media (Mejora de Cumplimiento)

| #   | Tarea                       | Normativa  | Responsable | Plazo     |
| --- | --------------------------- | ---------- | ----------- | --------- |
| 6   | Exportación de datos (ARCO) | Ley 172-13 | Dev Team    | 3 semanas |
| 7   | Centro de Privacidad        | Ley 172-13 | Dev Team    | 3 semanas |
| 8   | Derecho de retracto         | Ley 358-05 | Dev Team    | 4 semanas |
| 9   | Factura Electrónica (e-CF)  | DGII       | Dev Team    | 6 semanas |
| 10  | Integración INTRANT         | Ley 63-17  | Dev Team    | 8 semanas |

### 🟢 Prioridad Baja (Mejoras Futuras)

| #   | Tarea                   | Normativa          | Responsable | Plazo   |
| --- | ----------------------- | ------------------ | ----------- | ------- |
| 11  | Firma digital           | Ley 126-02         | Dev Team    | Q2 2026 |
| 12  | Partnership seguros     | Regulación Seguros | BD Team     | Q2 2026 |
| 13  | Capacitación PLD online | Ley 155-17         | HR          | Q2 2026 |

---

## Contactos de Reguladores

### Entidades Gubernamentales

| Entidad                        | Función               | Teléfono       | Web                  |
| ------------------------------ | --------------------- | -------------- | -------------------- |
| **UAF**                        | Lavado de Activos     | (809) 221-8181 | uaf.gob.do           |
| **DGII**                       | Impuestos             | (809) 689-3444 | dgii.gov.do          |
| **Pro Consumidor**             | Consumidor            | (809) 200-1600 | proconsumidor.gob.do |
| **INDOTEL**                    | Telecomunicaciones    | (809) 732-5555 | indotel.gob.do       |
| **INTRANT**                    | Tránsito              | (809) 920-2020 | intrant.gob.do       |
| **Superintendencia de Bancos** | Regulación Financiera | (809) 685-8141 | sb.gob.do            |

### Portales Importantes

| Portal               | URL                        | Uso                       |
| -------------------- | -------------------------- | ------------------------- |
| Oficina Virtual DGII | oficinavirtual.dgii.gov.do | Declaraciones y NCF       |
| TSS Online           | tss.gob.do                 | Nómina y seguridad social |
| SISALRIL             | sisalril.gov.do            | Seguro de salud           |
| SIPEN                | sipen.gov.do               | Pensiones                 |

---

## Sanciones por Incumplimiento

### Ley 155-17 (AML)

| Infracción                       | Sanción                  |
| -------------------------------- | ------------------------ |
| No reportar operación sospechosa | 50-200 salarios mínimos  |
| No aplicar DDC                   | 100-500 salarios mínimos |
| Falsificar información           | 2-10 años de prisión     |
| Estructuración de operaciones    | 2-10 años de prisión     |

### Ley 172-13 (Datos)

| Infracción                         | Sanción                  |
| ---------------------------------- | ------------------------ |
| Tratamiento sin consentimiento     | 10-50 salarios mínimos   |
| No atender derechos ARCO           | 25-100 salarios mínimos  |
| Filtración de datos                | 50-200 salarios mínimos  |
| Transferencia internacional ilegal | 100-400 salarios mínimos |

### Ley 358-05 (Consumidor)

| Infracción                | Sanción                 |
| ------------------------- | ----------------------- |
| Publicidad engañosa       | 5-50 salarios mínimos   |
| No entregar factura       | 3-30 salarios mínimos   |
| Negarse a atender reclamo | 10-100 salarios mínimos |
| Cláusulas abusivas        | 20-100 salarios mínimos |

### DGII

| Infracción           | Sanción                       |
| -------------------- | ----------------------------- |
| No presentar 606/607 | 5-30 salarios mínimos         |
| Evasión fiscal       | 2-6 años de prisión           |
| NCF falso            | 2-6 años de prisión           |
| Mora en pago         | Recargos del 10% + 4% mensual |

---

## Apéndice: Documentación Técnica

### Archivos de Referencia en el Proyecto

| Documento          | Ubicación                                                             |
| ------------------ | --------------------------------------------------------------------- |
| Ley 155-17 - AML   | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-ley-155-17.md`         |
| Ley 172-13 - Datos | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/02-ley-172-13.md`         |
| DGII Integration   | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/03-dgii-integration.md`   |
| Pro Consumidor     | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/04-proconsumidor.md`      |
| Compliance Reports | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/05-compliance-reports.md` |
| ComplianceService  | `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-compliance-service.md` |

### Servicios Backend Relacionados

| Servicio            | Puerto | Propósito                 |
| ------------------- | ------ | ------------------------- |
| `ComplianceService` | 5073   | Gestión de cumplimiento   |
| `KYCService`        | 5074   | Verificación de identidad |
| `AuditService`      | 5075   | Logs de auditoría         |
| `InvoicingService`  | 5046   | Facturación DGII          |

---

## Historial de Revisiones

| Versión | Fecha      | Autor    | Cambios           |
| ------- | ---------- | -------- | ----------------- |
| 1.0     | 25/01/2026 | Dev Team | Documento inicial |

---

> **⚠️ AVISO LEGAL:** Este documento es una guía de referencia para el equipo de desarrollo de OKLA. No constituye asesoría legal. Para interpretaciones oficiales de las leyes, consultar con abogados especializados y las entidades reguladoras correspondientes.

---

**Documento generado:** Enero 25, 2026  
**Próxima revisión:** Febrero 25, 2026  
**Responsable:** Equipo de Compliance OKLA
