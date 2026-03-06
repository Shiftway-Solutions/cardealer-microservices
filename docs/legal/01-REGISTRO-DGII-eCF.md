# 01 — Registro de Facturación Electrónica (e-CF) ante la DGII

> **Prioridad:** 🔴 ALTA  
> **Tiempo estimado:** 4-8 semanas  
> **Costo:** Gratuito el registro; software certificado $15,000-50,000 RD$  
> **Responsable:** Gerencia General + Contador  
> **Entidad:** Dirección General de Impuestos Internos (DGII)

---

## 1. ¿Por Qué es Necesario?

OKLA cobra por sus servicios (listados de vehículos $29/listing, planes de dealer $49-$299/mes). Toda empresa que venda bienes o servicios en República Dominicana **debe emitir comprobantes fiscales**. Desde 2019, la DGII ha implementado la facturación electrónica (e-CF) como el estándar obligatorio para contribuyentes.

### Consecuencias del incumplimiento:

- Multa de **RD$10,000 a RD$50,000** por cada comprobante no emitido
- Clausura temporal del establecimiento (hasta 10 días)
- Imposibilidad de deducir gastos sin comprobantes válidos
- Riesgo de investigación fiscal

---

## 2. Requisitos Previos

Antes de iniciar el proceso de e-CF, verificar que OKLA cuenta con:

- [ ] **RNC (Registro Nacional del Contribuyente)** — Si la empresa ya está registrada como SRL/SAS
- [ ] **Registro Mercantil** vigente en la Cámara de Comercio
- [ ] **Cédula del representante legal** (o pasaporte si es extranjero)
- [ ] **Acta constitutiva** de la sociedad
- [ ] **Dirección fiscal** verificable
- [ ] **Correo electrónico** corporativo para notificaciones de la DGII
- [ ] **Certificado digital** (se obtiene durante el proceso)

---

## 3. Proceso Paso a Paso

### Paso 1: Verificar el RNC (Semana 1)

Si OKLA ya tiene RNC:

1. Ir a **dgii.gov.do** → "Consultas" → "Consulta de RNC"
2. Verificar que el estado sea **ACTIVO**
3. Verificar que la actividad económica incluya servicios tecnológicos / marketplace

Si OKLA **no tiene RNC**:

1. Ir a la Oficina de la DGII más cercana con:
   - Acta constitutiva notarizada
   - Registro Mercantil
   - Cédula del representante legal
   - Comprobante de dirección (factura de servicio)
2. Completar el formulario **RC-01** (Declaración Jurada para Registro)
3. Esperar asignación del RNC (1-3 días hábiles)

### Paso 2: Registrarse en la Oficina Virtual (Semana 1-2)

1. Ir a **ofv.dgii.gov.do** (Oficina Virtual de la DGII)
2. Si no tiene cuenta: hacer clic en "Registrarse"
3. Completar el registro con RNC y datos del representante legal
4. Activar la cuenta mediante el correo electrónico de confirmación
5. Iniciar sesión y verificar datos fiscales

### Paso 3: Solicitar Autorización como Emisor e-CF (Semana 2-3)

1. En la Oficina Virtual, ir a: **"Comprobantes Fiscales"** → **"Solicitud e-CF"**
2. Completar el formulario de solicitud que incluye:
   - Datos de la empresa
   - Tipo de comprobantes a emitir (ver sección 4)
   - Información del software/sistema de facturación
   - Datos del certificado digital
3. Adjuntar documentación requerida:
   - Copia del Registro Mercantil vigente
   - Copia de la cédula del representante legal
   - Carta de designación del responsable técnico
4. Enviar solicitud

### Paso 4: Obtener Certificado Digital (Semana 3-4)

El certificado digital es necesario para firmar los e-CF:

1. La DGII emitirá un **certificado digital** una vez aprobada la solicitud
2. Alternativamente, se puede adquirir un certificado de un emisor autorizado:
   - **CertiSign Dominicana**
   - **COMODO**
3. El certificado se instala en el servidor de facturación
4. Vigencia: generalmente 1-2 años (requiere renovación)

### Paso 5: Configurar Software de Facturación (Semana 4-6)

Opciones para OKLA:

**Opción A: Desarrollo propio (recomendado para tech companies)**

- Integrar con la API de e-CF de la DGII
- Endpoint de pruebas: `ecf.dgii.gov.do/TesteCF`
- Endpoint de producción: `ecf.dgii.gov.do/CerteCF`
- Usar las librerías de firma digital XML (XAdES-BES)
- Documentación técnica: disponible en dgii.gov.do/ecf

**Opción B: Software certificado de terceros**

- Buscar en la lista de software certificado en dgii.gov.do
- Algunos proveedores: Softland, CG One, FacturaDigital.do
- Costo: RD$15,000-50,000/año dependiendo del volumen

### Paso 6: Período de Pruebas (Semana 6-7)

1. La DGII asignará un **ambiente de certificación**
2. Se deben enviar al menos **20 comprobantes de prueba** exitosos
3. Los comprobantes deben incluir diferentes tipos y escenarios
4. La DGII revisará y aprobará o solicitará correcciones

### Paso 7: Inicio de Operaciones (Semana 7-8)

1. Una vez aprobada la certificación, la DGII activará el ambiente de producción
2. A partir de la fecha de activación, **todos los comprobantes deben ser electrónicos**
3. Los comprobantes físicos ya no serán válidos

---

## 4. Tipos de Comprobantes Fiscales para OKLA

| Tipo                                  | Código | Uso en OKLA                                      |
| ------------------------------------- | ------ | ------------------------------------------------ |
| Factura de Crédito Fiscal (e-CF)      | 31     | Ventas a empresas (dealers con RNC)              |
| Factura de Consumo (e-CF)             | 32     | Ventas a personas físicas (sellers individuales) |
| Nota de Crédito (e-CF)                | 33     | Devoluciones, cancelaciones, ajustes             |
| Nota de Débito (e-CF)                 | 34     | Cargos adicionales, ajustes positivos            |
| Comprobante para Regímenes Especiales | 44     | Si aplica (zonas francas, etc.)                  |

---

## 5. Configuración del ITBIS

### ¿Incluir ITBIS en el precio o separarlo?

**Recomendación para OKLA:** Mostrar precios **sin ITBIS** y agregarlo al momento del checkout.

```
Precio del listado:  RD$ 1,653.00  (equivalente a $29 USD)
ITBIS (18%):         RD$   297.54
Total a pagar:       RD$ 1,950.54
```

### Consideraciones:

- Los servicios digitales en RD están gravados con ITBIS al **18%**
- Si OKLA cobra en USD, debe convertir a RD$ usando la tasa de la DGII del día
- Los comprobantes fiscales **siempre** deben estar en RD$
- El ITBIS cobrado se declara mensualmente en el formulario IT-1

---

## 6. Declaración Mensual IT-1

Cada mes, antes del **día 20**, se debe:

1. Acceder a la Oficina Virtual de la DGII
2. Completar el formulario **IT-1** (Declaración Jurada del ITBIS)
3. Reportar:
   - Total de ventas gravadas del mes
   - ITBIS cobrado (18% de las ventas)
   - ITBIS pagado en compras (crédito fiscal)
   - ITBIS neto a pagar = cobrado - pagado
4. Pagar el monto resultante mediante:
   - Transferencia bancaria autorizada
   - Pago en ventanilla de bancos autorizados
   - Pago en línea vía la Oficina Virtual

### Penalidades por declaración tardía:

- **Recargo:** 10% el primer mes, 4% cada mes adicional
- **Interés indemnizatorio:** 1.73% mensual (tasa vigente)
- **Multa por mora:** RD$5,000 - RD$25,000

---

## 7. Integración Técnica con el Sistema OKLA

### Flujo de facturación propuesto:

```
1. Usuario compra un servicio (listing/plan)
   ↓
2. PaymentService procesa el pago
   ↓
3. Se genera evento "payment.completed"
   ↓
4. BillingService (nuevo) recibe el evento
   ↓
5. BillingService genera el XML del e-CF
   ↓
6. Se firma digitalmente con certificado
   ↓
7. Se envía a la API de DGII
   ↓
8. DGII valida y retorna TrackId
   ↓
9. Se almacena el comprobante y se envía copia al cliente por email
```

### Campos obligatorios del e-CF:

- RNC del emisor (OKLA)
- RNC/Cédula del receptor (si aplica)
- Número de comprobante fiscal (NCF) asignado por DGII
- Fecha de emisión
- Descripción del servicio
- Monto sin ITBIS
- ITBIS
- Total

---

## 8. Información de Contacto DGII

| Concepto            | Detalle                                         |
| ------------------- | ----------------------------------------------- |
| **Teléfono**        | 809-689-3444                                    |
| **Website**         | dgii.gov.do                                     |
| **Oficina Virtual** | ofv.dgii.gov.do                                 |
| **Portal e-CF**     | dgii.gov.do/ecf                                 |
| **Email**           | informacion@dgii.gov.do                         |
| **Dirección**       | Av. México esq. Leopoldo Navarro, Santo Domingo |
| **Horario**         | Lunes a Viernes, 8:00 AM - 4:00 PM              |

---

## 9. Checklist de Completitud

- [ ] RNC verificado/obtenido
- [ ] Cuenta en Oficina Virtual creada
- [ ] Solicitud de emisor e-CF enviada
- [ ] Certificado digital obtenido e instalado
- [ ] Software de facturación configurado
- [ ] Pruebas de certificación completadas (20+ comprobantes)
- [ ] Ambiente de producción activado
- [ ] Primera factura electrónica emitida exitosamente
- [ ] Proceso de declaración IT-1 documentado
- [ ] Contador notificado del nuevo proceso
