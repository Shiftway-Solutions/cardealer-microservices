# 08 — Autorización para Comunicaciones SMS ante INDOTEL

> **Prioridad:** 🟡 MEDIA  
> **Tiempo estimado:** 2-4 semanas  
> **Costo:** Varía según proveedor de SMS; trámite ante INDOTEL generalmente gratuito  
> **Responsable:** DPO + Equipo Técnico  
> **Base legal:** Resolución INDOTEL 086-09, Ley 153-98

---

## 1. Contexto: SMS en OKLA

### Uso Actual de SMS en OKLA

OKLA utiliza SMS para:

1. **Verificación de identidad (2FA/OTP)** — Código de un solo uso al registrarse o iniciar sesión
2. **Notificaciones transaccionales** — Confirmación de acciones (listado publicado, pago recibido)
3. **Potencialmente en el futuro:** Notificaciones de marketing (nuevos vehículos, promociones)

### Clasificación de SMS

| Tipo                    | Descripción                           | ¿Necesita autorización?                                                  |
| ----------------------- | ------------------------------------- | ------------------------------------------------------------------------ |
| **Transaccional**       | 2FA, OTP, confirmaciones de pago      | ✅ No requiere autorización especial (es parte del servicio)             |
| **De servicio**         | Alertas de actividad, recordatorios   | ⚠️ Zona gris — consentimiento recomendado                                |
| **Marketing/Comercial** | Promociones, ofertas, nuevos listados | 🔴 Requiere consentimiento explícito y cumplimiento de Resolución 086-09 |

---

## 2. Marco Legal: Resolución INDOTEL 086-09

### Comunicaciones Comerciales No Solicitadas

La Resolución 086-09 establece las reglas para las **comunicaciones comerciales no solicitadas** vía medios electrónicos, incluyendo SMS. Los puntos clave:

1. **Consentimiento previo (Opt-In):** No se pueden enviar comunicaciones comerciales sin el consentimiento previo, expreso e informado del destinatario.

2. **Identificación del remitente:** Toda comunicación debe identificar claramente al remitente.

3. **Mecanismo de baja (Opt-Out):** Cada mensaje debe incluir un mecanismo claro y gratuito para que el destinatario pueda darse de baja.

4. **Horario permitido:** Las comunicaciones comerciales solo deben enviarse en horarios razonables (generalmente 8:00 AM - 8:00 PM).

5. **Registro:** Las empresas que envíen comunicaciones comerciales masivas pueden necesitar registrarse ante INDOTEL.

6. **Sanciones:** El incumplimiento puede resultar en multas y prohibición de envío.

---

## 3. Proceso para Cada Tipo de SMS

### 3.1 SMS Transaccionales (2FA/OTP) — No requiere trámite adicional

Los SMS de verificación de identidad y OTP son parte del servicio contratado por el usuario. Se consideran comunicaciones necesarias para la prestación del servicio.

**Requisitos mínimos:**

- El usuario acepta recibirlos al registrarse (Términos de Servicio)
- Incluir identificación del remitente (ej: "OKLA")
- No incluir contenido comercial en estos mensajes
- Mantener registro de los SMS enviados

**Texto sugerido en Términos de Servicio:**

```
Al registrarse, usted acepta recibir mensajes SMS de verificación
(códigos OTP) necesarios para la seguridad de su cuenta. Estos
mensajes son parte esencial del servicio y no pueden ser
desactivados mientras la cuenta esté activa.
```

### 3.2 SMS de Servicio — Consentimiento recomendado

Notificaciones como "Su listado ha sido publicado" o "Ha recibido un mensaje de un comprador":

**Requisitos:**

- Obtener consentimiento del usuario (puede ser por defecto con opción de desactivar)
- Permitir que el usuario desactive estas notificaciones desde su perfil
- Incluir identificación del remitente
- No incluir contenido comercial

**Implementación:**

```
Preferencias de Notificaciones:
☑️ Recibir notificaciones SMS de actividad en mi cuenta
   (nuevos mensajes, estado de publicaciones, alertas de seguridad)

   [Puede desactivar esta opción en cualquier momento desde
   su perfil → Notificaciones]
```

### 3.3 SMS de Marketing — Requiere autorización formal

Si OKLA planea enviar SMS promocionales (ofertas, nuevos vehículos, promociones):

**⚠️ Este es el tipo que requiere cumplimiento estricto de la Resolución 086-09.**

---

## 4. Proceso de Autorización para SMS de Marketing

### Paso 1: Implementar Mecanismo de Opt-In (Semana 1)

El consentimiento debe ser:

- **Explícito:** El usuario debe activamente aceptar (no casillas premarcadas)
- **Específico:** Separado del consentimiento general de uso
- **Informado:** El usuario debe saber qué tipo de mensajes recibirá
- **Documentado:** Se debe guardar evidencia del consentimiento

**Texto sugerido para el formulario de Opt-In:**

```
COMUNICACIONES PROMOCIONALES POR SMS

☐ Deseo recibir ofertas, promociones y alertas de nuevos vehículos
  de mi interés por SMS al número proporcionado.

Frecuencia estimada: máximo 4 mensajes por mes.
Puede cancelar en cualquier momento respondiendo "BAJA" a cualquier
mensaje o desde su perfil en okla.do/configuracion/notificaciones.

Al marcar esta casilla, usted autoriza a OKLA a enviarle
comunicaciones comerciales por SMS conforme a la Resolución
INDOTEL 086-09.
```

### Paso 2: Implementar Mecanismo de Opt-Out (Semana 1)

Cada SMS de marketing debe incluir instrucciones de baja:

**Formato del SMS:**

```
OKLA: ¡Nuevos Toyota Corolla desde RD$850,000 en tu zona!
Ver en okla.do/s/abc123
Responde BAJA para no recibir más ofertas.
```

**Requisitos del Opt-Out:**

- El usuario puede responder "BAJA", "STOP", "NO", "CANCELAR"
- La baja debe procesarse **inmediatamente** (dentro de 24 horas máximo)
- No se puede cobrar por la baja
- Se debe enviar confirmación: "Has sido dado de baja exitosamente"
- No se pueden enviar más mensajes de marketing después de la baja

### Paso 3: Registrar ante INDOTEL (Semana 2-3)

1. Contactar a INDOTEL para verificar si se requiere registro formal:
   - **Teléfono:** 809-732-5555
   - **Email:** info@indotel.gob.do
   - Preguntar por el departamento de telecomunicaciones/comunicaciones comerciales

2. Si se requiere registro, preparar:
   - Datos de la empresa (RNC, razón social, dirección)
   - Descripción de los tipos de SMS a enviar
   - Estimado de volumen mensual de SMS
   - Proveedor de SMS utilizado (Twilio, AWS SNS, etc.)
   - Copia del mecanismo de Opt-In implementado
   - Copia del mecanismo de Opt-Out implementado
   - Política de privacidad
   - Designación del responsable de comunicaciones

3. Presentar la solicitud y esperar aprobación

### Paso 4: Establecer Políticas Internas (Semana 3-4)

Documentar las siguientes políticas:

```
POLÍTICA DE COMUNICACIONES SMS — OKLA

1. HORARIO DE ENVÍO
   - SMS transaccionales: 24/7 (son parte del servicio)
   - SMS de marketing: Solo de 8:00 AM a 8:00 PM, de lunes a sábado
   - NO se envían SMS de marketing en domingos ni días feriados

2. FRECUENCIA
   - SMS de marketing: Máximo 4 por mes por usuario
   - SMS transaccionales: Sin límite (según necesidad del servicio)

3. CONTENIDO
   - Todos los SMS deben identificar a OKLA como remitente
   - Los SMS de marketing deben incluir instrucción de baja
   - No se permite contenido engañoso o misleading
   - Los enlaces deben dirigir a okla.do (no URLs acortadas sospechosas)

4. REGISTROS
   - Mantener registro de todos los consentimientos (opt-in)
   - Mantener registro de todas las bajas (opt-out)
   - Mantener registro de todos los SMS enviados
   - Retención mínima: 12 meses

5. LISTAS NEGRAS
   - Mantener lista de números que solicitaron baja
   - Verificar contra la lista negra antes de cada envío
   - Nunca re-agregar un número que solicitó baja sin nuevo consentimiento

6. PROVEEDORES
   - Proveedor actual: [Twilio/AWS SNS/otro]
   - Verificar que el proveedor cumpla con regulaciones de INDOTEL
   - Contrato con proveedor debe incluir cláusulas de cumplimiento
```

---

## 5. Registro de Consentimientos

### Base de Datos de Consentimiento SMS

Para cada usuario que acepte SMS de marketing, registrar:

| Campo         | Descripción                                |
| ------------- | ------------------------------------------ |
| UserId        | Identificador del usuario                  |
| PhoneNumber   | Número de teléfono                         |
| ConsentDate   | Fecha y hora del consentimiento            |
| ConsentMethod | Cómo se obtuvo (registro, perfil, campaña) |
| ConsentText   | Texto exacto aceptado                      |
| IPAddress     | IP al momento del consentimiento           |
| OptOutDate    | Fecha de baja (null si activo)             |
| OptOutMethod  | Cómo solicitó la baja (SMS, web, llamada)  |

### Auditoría

- Generar reportes mensuales de opt-in vs opt-out
- Monitorear tasa de quejas
- Si la tasa de quejas supera 1%, revisar prácticas

---

## 6. Sanciones por Incumplimiento

| Infracción                    | Posible Sanción                     |
| ----------------------------- | ----------------------------------- |
| Envío sin consentimiento      | Multa + prohibición de envío        |
| No incluir mecanismo de baja  | Multa                               |
| No procesar baja a tiempo     | Multa + orden de cumplimiento       |
| Envío en horario no permitido | Advertencia + multa en reincidencia |
| No identificar al remitente   | Multa                               |

---

## 7. Información de Contacto

| Concepto         | Detalle                                 |
| ---------------- | --------------------------------------- |
| **INDOTEL**      | 809-732-5555                            |
| **Website**      | indotel.gob.do                          |
| **Email**        | info@indotel.gob.do                     |
| **Dirección**    | Av. Abraham Lincoln #962, Santo Domingo |
| **Departamento** | Dirección de Telecomunicaciones         |

---

## 8. Checklist de Completitud

- [ ] SMS transaccionales (2FA) — términos de servicio actualizados
- [ ] SMS de servicio — mecanismo de preferencias implementado
- [ ] Mecanismo de Opt-In para SMS marketing implementado
- [ ] Mecanismo de Opt-Out implementado (responder BAJA)
- [ ] Consulta con INDOTEL realizada
- [ ] Registro ante INDOTEL completado (si requerido)
- [ ] Política interna de SMS documentada
- [ ] Base de datos de consentimientos implementada
- [ ] Proveedor de SMS verificado (cumplimiento regulatorio)
- [ ] Horarios de envío configurados
- [ ] Lista negra (opt-out) implementada
- [ ] Reportes mensuales programados
