# 📐 Prompt 10 — Detección y Protección de PII (Datos Sensibles)

> **Fase:** 1 — Diseño de Prompts  
> **Última actualización:** Febrero 15, 2026

---

## 1. Nombre y Rol

**PII Detection & Protection Prompt** — Detecta datos personales sensibles (cédulas, tarjetas de crédito, direcciones completas) en los mensajes del usuario y en las respuestas del bot. Enmascara datos en logs y previene que el chatbot repita información sensible. Se integra como capa de seguridad en el pipeline de mensajes.

---

## 2. Trigger

- **Cuándo se ejecuta:** En CADA mensaje del usuario (pre-procesamiento) y en cada respuesta del bot (post-procesamiento).
- **Qué lo activa:** Automáticamente como parte del pipeline. No requiere detección de intent.

---

## 3. ⚠️ DIRECTIVA DE REEMPLAZO

> El ChatbotService actual con Dialogflow NO tenía protección de PII. Los datos sensibles podían pasar sin filtro. El nuevo sistema LLM DEBE implementar esta capa de seguridad que es CRÍTICA para cumplir con la Ley 172-13.

---

## 4. Variables Dinámicas Requeridas

| Variable           | Fuente              | Tipo   | Ejemplo               |
| ------------------ | ------------------- | ------ | --------------------- |
| `{{input_text}}`   | Mensaje del usuario | string | Texto a analizar      |
| `{{bot_response}}` | Respuesta generada  | string | Respuesta a verificar |

---

## 5. Texto Completo del Prompt

```
═══════════════════════════════════════════
DETECCIÓN Y PROTECCIÓN DE PII — CHATBOT OKLA
═══════════════════════════════════════════

Analiza el siguiente texto y detecta cualquier dato personal sensible (PII).

TEXTO A ANALIZAR:
"""
{{input_text}}
"""

═══════════════════════════════════════════
PATRONES DE PII A DETECTAR (República Dominicana)
═══════════════════════════════════════════

### 1. CÉDULA DE IDENTIDAD
- Formato: XXX-XXXXXXX-X (ej: 001-1234567-8)
- También: sin guiones (00112345678), con espacios
- Acción: ENMASCARAR → ***-*******-X (mantener último dígito)

### 2. TARJETA DE CRÉDITO/DÉBITO
- Formato: 16 dígitos (XXXX-XXXX-XXXX-XXXX, XXXX XXXX XXXX XXXX, sin separadores)
- También: CVV (3-4 dígitos después de tarjeta)
- Acción: BLOQUEAR COMPLETAMENTE → responder con advertencia de seguridad

### 3. CUENTA BANCARIA
- Formato: Números largos (10-20 dígitos) precedidos de contexto bancario
- Acción: ENMASCARAR → ****XXXX (últimos 4)

### 4. DIRECCIÓN COMPLETA
- Formato: Calle + número + sector/barrio + ciudad + provincia
- Acción: MANTENER solo ciudad/sector para propósitos de búsqueda. NO repetir dirección completa.

### 5. NÚMERO DE PASAPORTE
- Formato: Letras + números (2 letras + 7 dígitos)
- Acción: ENMASCARAR → XX-*****XX (primeras 2 letras + últimos 2 dígitos)

### 6. RNC (Registro Nacional del Contribuyente)
- Formato: 9 u 11 dígitos
- Acción: ENMASCARAR para personas físicas, MANTENER para empresas públicas

### 7. NÚMERO DE PLACA
- Formato: Letras + números (A123456, X-123456)
- Acción: MANTENER solo para contexto de servicio de taller. NO almacenar en logs.

═══════════════════════════════════════════
RESPUESTAS ANTE PII DETECTADO
═══════════════════════════════════════════

### Si el usuario comparte TARJETA DE CRÉDITO:
"🔒 Por tu seguridad, no procesamos datos de pago por este canal. Nuestro equipo te contactará para gestionar el pago de forma segura a través de canales protegidos."

→ Activar TRANSFER_TO_AGENT con razón "payment_data"

### Si el usuario comparte CÉDULA sin que se la pidan:
"Gracias por la información. Por tu seguridad, no necesitamos tu número de cédula por este canal. Si es necesario para algún trámite, nuestro equipo lo gestionará de forma segura cuando te contacten."

→ NO almacenar en historial legible

### Si el usuario comparte CONTRASEÑA o datos de acceso:
"⚠️ Nunca compartas contraseñas o datos de acceso por chat. Si necesitas ayuda con tu cuenta, contacta soporte directamente al {{dealer_phone}}."

→ Eliminar del historial inmediatamente

═══════════════════════════════════════════
FORMATO DE RESPUESTA
═══════════════════════════════════════════

{
  "pii_detected": true,
  "pii_items": [
    {
      "type": "cedula",
      "original": "001-1234567-8",
      "masked": "***-*******-8",
      "position": {"start": 45, "end": 58},
      "action": "mask",
      "risk_level": "high"
    }
  ],
  "sanitized_text": "[Texto con PII enmascarado]",
  "should_block_response": false,
  "should_transfer": false,
  "transfer_reason": null,
  "warning_message": null
}

ACCIONES:
- "mask": Reemplazar con versión enmascarada en logs
- "block": No almacenar, responder con advertencia
- "transfer": Transferir a agente humano inmediatamente
- "allow": Dato necesario para la operación (ej: teléfono para cita, con consentimiento)
```

---

## 6. Notas de Implementación (.NET 8)

### Implementar como middleware, NO como prompt del LLM:

> ⚠️ **IMPORTANTE**: La detección de PII debe hacerse con REGEX en el backend, NO enviando datos sensibles al LLM. No queremos que el LLM procese cédulas o tarjetas de crédito.

```csharp
// PiiProtectionMiddleware.cs — Se ejecuta ANTES y DESPUÉS del LLM

public class PiiProtectionService
{
    private static readonly Dictionary<string, Regex> PiiPatterns = new()
    {
        // Cédula dominicana: XXX-XXXXXXX-X
        ["cedula"] = new Regex(@"\b(\d{3})-?(\d{7})-?(\d)\b", RegexOptions.Compiled),

        // Tarjeta de crédito: 16 dígitos
        ["credit_card"] = new Regex(@"\b(\d{4})[\s-]?(\d{4})[\s-]?(\d{4})[\s-]?(\d{4})\b", RegexOptions.Compiled),

        // CVV: 3-4 dígitos después de contexto de tarjeta
        ["cvv"] = new Regex(@"\b(?:cvv|cvc|código|seguridad)\s*:?\s*(\d{3,4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Cuenta bancaria: 10-20 dígitos con contexto
        ["bank_account"] = new Regex(@"(?:cuenta|account)\s*:?\s*#?\s*(\d{10,20})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Pasaporte: 2 letras + 7 dígitos
        ["passport"] = new Regex(@"\b([A-Z]{2})\s*(\d{7})\b", RegexOptions.Compiled),

        // RNC: 9 u 11 dígitos
        ["rnc"] = new Regex(@"\b(?:rnc|RNC)\s*:?\s*(\d{9}|\d{11})\b", RegexOptions.Compiled),
    };

    public PiiDetectionResult DetectPii(string text)
    {
        var result = new PiiDetectionResult();

        foreach (var (type, pattern) in PiiPatterns)
        {
            foreach (Match match in pattern.Matches(text))
            {
                result.Items.Add(new PiiItem
                {
                    Type = type,
                    Original = match.Value,
                    Masked = MaskPii(type, match),
                    Position = (match.Index, match.Index + match.Length),
                    Action = GetAction(type),
                    RiskLevel = GetRiskLevel(type)
                });
            }
        }

        result.PiiDetected = result.Items.Any();
        result.ShouldBlockResponse = result.Items.Any(i => i.Action == "block");
        result.ShouldTransfer = result.Items.Any(i => i.Action == "transfer");

        return result;
    }

    public string SanitizeForLog(string text)
    {
        var sanitized = text;
        foreach (var (type, pattern) in PiiPatterns)
        {
            sanitized = pattern.Replace(sanitized, m => MaskPii(type, m));
        }
        return sanitized;
    }

    private string MaskPii(string type, Match match) => type switch
    {
        "cedula" => $"***-*******-{match.Groups[3].Value}",
        "credit_card" => $"****-****-****-{match.Groups[4].Value}",
        "cvv" => "***",
        "bank_account" => $"****{match.Value[^4..]}",
        "passport" => $"{match.Groups[1].Value}-*****{match.Value[^2..]}",
        _ => "****"
    };

    private string GetAction(string type) => type switch
    {
        "credit_card" or "cvv" => "transfer",   // Transferir a agente
        "cedula" or "passport" => "mask",         // Enmascarar en logs
        "bank_account" => "block",                // Bloquear y advertir
        _ => "mask"
    };

    private string GetRiskLevel(string type) => type switch
    {
        "credit_card" or "cvv" or "bank_account" => "critical",
        "cedula" or "passport" => "high",
        _ => "medium"
    };
}
```

### Integración en el pipeline:

```csharp
// En SendMessageCommandHandler:

// 1. ANTES de enviar al LLM — sanitizar input del usuario
var piiResult = _piiService.DetectPii(request.Message);

if (piiResult.ShouldTransfer)
{
    // Tarjeta de crédito → transferir inmediatamente
    return new ChatbotResponse
    {
        Response = "🔒 Por tu seguridad, no procesamos datos de pago por este canal. Te conecto con un asesor.",
        IntentName = "PiiProtection",
        SuggestedAction = "TRANSFER_TO_AGENT"
    };
}

// 2. Sanitizar mensaje para el LLM (no enviar PII real)
var sanitizedMessage = piiResult.PiiDetected
    ? _piiService.SanitizeForLog(request.Message)
    : request.Message;

// 3. Enviar mensaje sanitizado al LLM
var llmResponse = await _llmService.GenerateResponseAsync(
    new LlmRequest { Message = sanitizedMessage, ... }, ct);

// 4. DESPUÉS del LLM — verificar que la respuesta no repite PII
var responsePii = _piiService.DetectPii(llmResponse.Response);
if (responsePii.PiiDetected)
{
    llmResponse.Response = _piiService.SanitizeForLog(llmResponse.Response);
    _logger.LogWarning("LLM response contained PII, sanitized before sending");
}

// 5. En logs, SIEMPRE almacenar versión sanitizada
var userMessage = new ChatMessage
{
    Content = _piiService.SanitizeForLog(request.Message), // Sanitizado
    // ...
};
```
