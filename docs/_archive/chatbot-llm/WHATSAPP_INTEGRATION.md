# OKLA Chatbot LLM — WhatsApp Integration

## Overview

WhatsApp integration via Meta Cloud API enables dealers to offer
AI-powered customer service on WhatsApp with seamless bot↔human handoff.

---

## Architecture

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  WhatsApp    │────►│  Meta Cloud API  │────►│  OKLA Gateway   │
│  User (RD)   │◄────│  Webhooks        │◄────│  (internal K8s) │
└──────────────┘     └──────────────────┘     └────────┬────────┘
                                                       │
                                              ┌────────▼────────┐
                                              │  WhatsAppCtrl   │
                                              │  /api/whatsapp  │
                                              │  /webhook       │
                                              └────────┬────────┘
                                                       │
                                              ┌────────▼────────┐
                                              │  ChatbotService │
                                              │  Pipeline       │
                                              │  (same as web)  │
                                              └────────┬────────┘
                                                       │
                                              ┌────────▼────────┐
                                              │  WhatsAppService│
                                              │  SendReply()    │
                                              └─────────────────┘
```

## Message Flow

### Inbound (User → Bot)

1. User sends WhatsApp message
2. Meta Cloud API POST → `/api/whatsapp/webhook`
3. Parse payload → `WhatsAppInboundMessage`
4. Security checks:
   - Rate limit (10/min per phone)
   - Country filter (only +1809/829/849)
5. Session lookup (by `channel=whatsapp`, `channelUserId=phone`)
6. If no session → `StartSessionCommand` (auto DealerInventory mode)
7. If `HandoffStatus == HumanActive` → save message, don't process with bot
8. Else → `SendMessageCommand` (full pipeline: injection → PII → strategy → LLM)
9. `WhatsAppService.SendTextMessageAsync()` reply

### Outbound (Bot → User)

```csharp
await _whatsAppService.SendTextMessageAsync(toPhone, botResponse, ct);
```

### Interactive Messages (Quick Replies)

```csharp
await _whatsAppService.SendInteractiveMessageAsync(
    toPhone,
    "¿Necesitas ayuda?",
    "¿Te gustaría hablar con un asesor?",
    new List<(string, string)>
    {
        ("agent_yes", "Sí, con un asesor"),
        ("agent_no", "No, gracias")
    }, ct);
```

## Bot ↔ Human Handoff

### Dealer Takes Over

```
POST /api/chat/handoff/takeover
{
    "sessionToken": "abc123",
    "agentId": "dealer-uuid",
    "agentName": "Juan Pérez",
    "reason": "Cliente interesado en Toyota Corolla"
}
```

**Result:**

- `HandoffStatus` → `HumanActive`
- System message sent to user: "Un asesor se ha unido"
- Bot stops processing messages
- Messages are saved for dealer to see in real-time

### Dealer Returns Control

```
POST /api/chat/handoff/return-to-bot
{
    "sessionToken": "abc123"
}
```

**Result:**

- `HandoffStatus` → `ReturnedToBot`
- System message: "El asesor ha finalizado. Soy tu asistente virtual de nuevo."
- Bot resumes processing

## Meta Cloud API Setup

### Prerequisites

1. Meta Business Account
2. WhatsApp Business API access
3. Phone number verified

### Environment Variables

```env
WhatsApp__VerifyToken=okla-whatsapp-verify-2026
WhatsApp__AccessToken=EAAG...  # Meta access token
WhatsApp__PhoneNumberId=1234567890  # WhatsApp Business phone number ID
WhatsApp__ApiVersion=v18.0
WhatsApp__AllowedCountryCodes=1809,1829,1849
```

### Webhook Configuration (Meta Dashboard)

| Field             | Value                                    |
| ----------------- | ---------------------------------------- |
| Callback URL      | https://okla.com.do/api/whatsapp/webhook |
| Verify Token      | okla-whatsapp-verify-2026                |
| Subscribed Fields | messages                                 |

### Gateway Route (ocelot.json)

```json
{
  "UpstreamPathTemplate": "/api/whatsapp/{everything}",
  "DownstreamPathTemplate": "/api/whatsapp/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "chatbotservice", "Port": 8080 }],
  "UpstreamHttpMethod": ["GET", "POST"]
}
```

## Rate Limits

| Scope      | Limit           | Window   |
| ---------- | --------------- | -------- |
| Per phone  | 10 messages     | 1 minute |
| Per dealer | 1000 messages   | 1 hour   |
| Global     | 10,000 messages | 1 hour   |

## Country Filter

Only Dominican Republic phone numbers are accepted:

- +1 (809) XXX-XXXX
- +1 (829) XXX-XXXX
- +1 (849) XXX-XXXX

All other numbers receive a polite rejection message.
