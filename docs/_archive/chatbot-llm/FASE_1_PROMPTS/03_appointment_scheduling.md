# 📐 Prompt 03 — Agendamiento de Citas (Dual-Mode v2.0)

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 17, 2026  
> **Versión:** 2.0 — Mode-Aware Scheduling

---

## 1. Nombre y Rol

**Appointment Scheduling Prompt** — Protocolo paso a paso para agendar citas (test drive, visita de compra, taller mecánico). Recopila datos con consentimiento legal, valida horarios y genera JSON estructurado para el backend.

---

## 2. Diferenciación por Modo

| Aspecto               | SingleVehicle (SV)                  | DealerInventory (DI)                   |
| --------------------- | ----------------------------------- | -------------------------------------- |
| **Vehículo conocido** | ✅ Ya está en contexto              | ❌ Puede necesitar identificar primero |
| **Paso 2 (vehículo)** | Skip — confirmar el que está viendo | Preguntar o confirmar del RAG          |
| **Tipo de cita**      | Test drive o visita de compra       | Test drive, visita, o taller           |
| **WhatsApp**          | Mensajes más cortos, sin tablas     | Mensajes cortos, sin tablas            |

### Flujo SV (simplificado)

```
Usuario: "Quiero probar este carro"
   → Vehículo YA conocido (skip paso 2)
   → Ir directo a Paso 3 (fechas)
```

### Flujo DI

```
Usuario: "Quiero probar un carro"
   → ¿Cuál de los que vimos?
   → Confirmar vehículo → Paso 3 (fechas)
```

---

## 3. Trigger

- **Cuándo se ejecuta:** Cuando el intent es `TestDriveSchedule` o cuando el usuario expresa deseo de visitar, probar o llevar su vehículo a taller.
- **Qué lo activa:** Keywords como "agendar", "cita", "visita", "probar", "test drive", "prueba de manejo", "taller", "servicio", "mantenimiento", "reparar".

---

## 3. Variables Dinámicas Requeridas

| Variable                | Fuente                          | Tipo   | Ejemplo                        |
| ----------------------- | ------------------------------- | ------ | ------------------------------ |
| `{{dealer_name}}`       | ChatbotConfiguration            | string | "Auto Toyota Dominicana"       |
| `{{dealer_phone}}`      | ChatbotConfiguration            | string | "+1-809-555-0100"              |
| `{{dealer_hours}}`      | BusinessHoursJson (parseado)    | string | "Lun-Vie 8AM-6PM, Sáb 9AM-1PM" |
| `{{dealer_address}}`    | ChatbotConfiguration            | string | "Av. 27 de Febrero #100, SD"   |
| `{{service_available}}` | ChatbotConfiguration            | bool   | true                           |
| `{{current_date}}`      | DateTime.Now                    | string | "Sábado 15 de febrero de 2026" |
| `{{timezone}}`          | TimeZone                        | string | "America/Santo_Domingo"        |
| `{{vehicle_context}}`   | CurrentVehicleId/Name si existe | string | "Toyota RAV4 2024"             |

---

## 4. Texto Completo del Prompt

```
═══════════════════════════════════════════
PROTOCOLO DE AGENDAMIENTO DE CITAS
═══════════════════════════════════════════

El usuario quiere agendar una cita. Sigue este protocolo paso a paso. NO saltes pasos.

TIPOS DE CITA DISPONIBLES:
1. **Prueba de manejo (test drive)** — Requiere: nombre, teléfono, vehículo de interés, fecha/hora, licencia de conducir vigente
2. **Visita para compra** — Requiere: nombre, teléfono, vehículo(s) de interés, fecha/hora
3. **Taller mecánico (service)** — Solo si {{service_available}} = true. Requiere: nombre, teléfono, vehículo del cliente (marca/modelo/año), descripción del problema, fecha

═══════════════════════════════════════════
PASO 1: IDENTIFICAR TIPO DE CITA
═══════════════════════════════════════════

Si el tipo no es claro, preguntar:
"¿Qué tipo de cita te gustaría agendar?
1️⃣ Prueba de manejo (test drive)
2️⃣ Visita para ver un vehículo
3️⃣ Cita de taller/servicio"

Si {{service_available}} = false, NO ofrecer opción 3.

═══════════════════════════════════════════
PASO 2: CONFIRMAR VEHÍCULO DE INTERÉS
═══════════════════════════════════════════

- Para test drive y visita: "¿Qué vehículo te interesa probar/ver?"
  - Si ya mencionó uno antes ({{vehicle_context}}), confirmar: "¿Es para el {{vehicle_context}} que estuvimos viendo?"
  - Verificar que el vehículo EXISTE en el inventario. Si no: "Ese modelo no lo tenemos disponible actualmente. ¿Te interesaría ver alguna alternativa?"

- Para taller: "¿Cuál es el vehículo que necesita servicio? (marca, modelo y año)"
  - Aquí el vehículo NO necesita estar en inventario (es del cliente).
  - Preguntar: "¿Podrías describirme brevemente el problema o el servicio que necesita?"

═══════════════════════════════════════════
PASO 3: PROPONER FECHAS Y HORARIOS
═══════════════════════════════════════════

Horarios de {{dealer_name}}: {{dealer_hours}}
Fecha actual: {{current_date}}
Zona horaria: {{timezone}}

- Proponer 2-3 opciones de fecha/hora DENTRO de los horarios de atención.
- NO proponer fechas pasadas.
- NO proponer domingos a menos que el dealer abra domingos.
- Formato: "¿Te conviene alguna de estas opciones?
  📅 Martes 18 de febrero, 10:00 AM
  📅 Miércoles 19 de febrero, 2:00 PM
  📅 Jueves 20 de febrero, 11:00 AM"

Si el usuario propone una fecha/hora:
- Validar que esté dentro de horarios de atención.
- Si NO: "Ese horario está fuera de nuestro horario de atención ({{dealer_hours}}). ¿Te parece alguno de estos?"
- Si SÍ: "¡Perfecto! Anotado para el [fecha] a las [hora]."

═══════════════════════════════════════════
PASO 4: RECOPILAR DATOS CON CONSENTIMIENTO (Ley 172-13)
═══════════════════════════════════════════

⚠️ OBLIGATORIO — Antes de pedir datos, SIEMPRE decir:
"Para confirmar tu cita necesito algunos datos. Tu información será usada únicamente para coordinar la visita y está protegida según la Ley 172-13 de Protección de Datos Personales. ¿Deseas continuar?"

Si dice SÍ → pedir datos.
Si dice NO → respetar: "Entendido. Si cambias de opinión, puedes llamarnos al {{dealer_phone}} o visitarnos directamente en {{dealer_address}}."

Datos a recopilar (uno a uno, NO todos de golpe):
1. "¿A nombre de quién agendamos la cita?" → nombre completo
2. "¿Un número de teléfono para confirmarte?" → teléfono
3. Para test drive: "¿Cuentas con licencia de conducir vigente?" → sí/no

IMPORTANTE:
- NO pedir cédula ni datos financieros para agendar.
- NO pedir email si no es necesario (solo teléfono).
- Si el usuario ya proporcionó datos durante la conversación, NO volver a pedirlos.

═══════════════════════════════════════════
PASO 5: CONFIRMAR CON RESUMEN
═══════════════════════════════════════════

Presentar resumen completo antes de confirmar:

"Perfecto, te confirmo los detalles de tu cita:

📋 **Resumen de cita**
- Tipo: [Prueba de manejo / Visita / Taller]
- Vehículo: [Marca Modelo Año]
- Fecha: [día de la semana, fecha]
- Hora: [hora]
- Nombre: [nombre]
- Teléfono: [teléfono]
- Ubicación: {{dealer_address}}

⚠️ Esta cita está sujeta a disponibilidad y será confirmada por nuestro equipo vía [teléfono/WhatsApp] al número proporcionado.

¿Confirmamos?"

═══════════════════════════════════════════
PASO 6: GENERAR JSON ESTRUCTURADO
═══════════════════════════════════════════

Cuando el usuario confirme, ADEMÁS de la respuesta de texto, incluye en el campo "suggestedAction" el valor "SCHEDULE_APPOINTMENT" y en "parameters" el JSON completo:

{
  "action": "SCHEDULE_APPOINTMENT",
  "type": "test_drive | purchase_visit | service",
  "vehicleId": "uuid-del-vehiculo-si-aplica",
  "vehicleName": "Toyota RAV4 2024",
  "customerName": "Juan Pérez",
  "customerPhone": "+18095551234",
  "preferredDate": "2026-02-20",
  "preferredTime": "10:00",
  "notes": "Notas adicionales (problema del vehículo, interés en financiamiento, etc.)",
  "hasDriversLicense": true,
  "consentGiven": true
}

REGLAS DEL JSON:
- "type" solo puede ser: "test_drive", "purchase_visit", "service"
- "vehicleId" debe ser el UUID real del inventario. Si es taller (vehículo del cliente), usar null.
- "customerPhone" en formato internacional: +1XXXXXXXXXX
- "preferredDate" en formato ISO: YYYY-MM-DD
- "preferredTime" en formato 24h: HH:MM
- "consentGiven" SIEMPRE true (ya confirmó en Paso 4)
- "hasDriversLicense" solo para test_drive
```

---

## 5. Ejemplo Real de Input/Output (Conversación multi-turno)

### Turno 1:

**User:** "Quiero hacer un test drive del RAV4"

**Output:**

```json
{
  "response": "¡Excelente elección! 🚗 La Toyota RAV4 es una de nuestras favoritas.\n\nPara agendar tu prueba de manejo, ¿cuándo te queda bien?\n\n📅 Lunes 17 de febrero, 10:00 AM\n📅 Martes 18 de febrero, 2:00 PM\n📅 Miércoles 19 de febrero, 11:00 AM",
  "intent": "TestDriveSchedule",
  "confidence": 0.96,
  "isFallback": false,
  "parameters": {
    "vehicleName": "Toyota RAV4 2024",
    "appointmentType": "test_drive"
  },
  "leadSignals": {
    "mentionedBudget": false,
    "requestedTestDrive": true,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggestedAction": null,
  "quickReplies": ["Lunes 10 AM", "Martes 2 PM", "Otra fecha"]
}
```

### Turno 2:

**User:** "El martes a las 2 me queda bien"

**Output:**

```json
{
  "response": "¡Perfecto! Martes 18 de febrero a las 2:00 PM. 👍\n\nPara confirmar tu cita necesito algunos datos. Tu información será usada únicamente para coordinar la visita y está protegida según la Ley 172-13 de Protección de Datos Personales. ¿Deseas continuar?",
  "intent": "TestDriveSchedule",
  "confidence": 0.95,
  "isFallback": false,
  "parameters": { "preferredDate": "2026-02-18", "preferredTime": "14:00" },
  "leadSignals": {
    "mentionedBudget": false,
    "requestedTestDrive": true,
    "askedFinancing": false,
    "providedContactInfo": false
  },
  "suggestedAction": null,
  "quickReplies": ["Sí, continuar", "No, gracias"]
}
```

### Turno 3:

**User:** "Sí dale"

### Turno 4:

**User:** "Juan Pérez, 809-555-1234, sí tengo licencia"

**Output:**

```json
{
  "response": "Perfecto, te confirmo los detalles de tu cita:\n\n📋 **Resumen de cita**\n- Tipo: Prueba de manejo (test drive)\n- Vehículo: Toyota RAV4 2024\n- Fecha: Martes 18 de febrero de 2026\n- Hora: 2:00 PM\n- Nombre: Juan Pérez\n- Teléfono: +1-809-555-1234\n- Ubicación: Av. 27 de Febrero #100, Santo Domingo\n\n⚠️ Esta cita está sujeta a disponibilidad y será confirmada por nuestro equipo vía WhatsApp/llamada al número proporcionado.\n\n¿Confirmamos?",
  "intent": "TestDriveSchedule",
  "confidence": 0.97,
  "isFallback": false,
  "parameters": {
    "action": "SCHEDULE_APPOINTMENT",
    "type": "test_drive",
    "vehicleId": "abc-123",
    "vehicleName": "Toyota RAV4 2024",
    "customerName": "Juan Pérez",
    "customerPhone": "+18095551234",
    "preferredDate": "2026-02-18",
    "preferredTime": "14:00",
    "hasDriversLicense": true,
    "consentGiven": true,
    "notes": ""
  },
  "leadSignals": {
    "mentionedBudget": false,
    "requestedTestDrive": true,
    "askedFinancing": false,
    "providedContactInfo": true
  },
  "suggestedAction": "SCHEDULE_APPOINTMENT",
  "quickReplies": ["Confirmar cita", "Cambiar fecha", "Cancelar"]
}
```

---

## 6. Notas de Implementación (.NET 8)

### Procesamiento del JSON de agendamiento en el backend:

```csharp
// En SendMessageCommandHandler — Después de obtener LlmResponse:

if (llmResponse.SuggestedAction == "SCHEDULE_APPOINTMENT")
{
    var appointmentData = JsonSerializer.Deserialize<AppointmentRequest>(
        JsonSerializer.Serialize(llmResponse.Parameters));

    if (appointmentData != null && appointmentData.ConsentGiven)
    {
        // 1. Crear lead automáticamente
        var lead = new ChatLead
        {
            FullName = appointmentData.CustomerName,
            Phone = appointmentData.CustomerPhone,
            InterestedVehicleId = appointmentData.VehicleId,
            Status = LeadStatus.Qualified,
            Temperature = LeadTemperature.Hot,
            QualificationScore = 85,
            Notes = $"Cita de {appointmentData.Type} agendada para {appointmentData.PreferredDate}"
        };

        // 2. Enviar notificación al dealer
        await _notificationClient.SendAsync(new NotificationRequest
        {
            Channel = "whatsapp",
            To = config.DealerPhone,
            Template = "new_appointment",
            Data = appointmentData
        });

        // 3. Registrar en auditoría
        await _auditClient.LogActionAsync(new AuditLogRequest
        {
            Action = "APPOINTMENT_SCHEDULED",
            EntityType = "ChatSession",
            EntityId = session.Id.ToString(),
            Details = JsonSerializer.Serialize(appointmentData)
        });
    }
}
```

### DTO para appointment:

```csharp
public record AppointmentRequest(
    string Action,
    string Type,          // test_drive, purchase_visit, service
    Guid? VehicleId,
    string? VehicleName,
    string CustomerName,
    string CustomerPhone,
    string PreferredDate,  // YYYY-MM-DD
    string PreferredTime,  // HH:MM
    string? Notes,
    bool? HasDriversLicense,
    bool ConsentGiven
);
```
