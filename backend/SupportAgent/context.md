# SupportAgent — AI Support & Buyer Protection Agent

## Overview

SupportAgent is the chatbot de soporte y orientación de OKLA Marketplace. Uses Claude Haiku 4.5 for fast, conversational responses in Dominican Spanish.

## Two Functions

1. **Soporte Técnico de Plataforma**: Guides users step-by-step on using OKLA (registration, login, KYC, publishing vehicles, dealer portal, payments, security, 2FA, messaging, etc.)
2. **Orientación al Comprador**: When users ask about buying vehicles, activates consumer protection module: fraud prevention, document verification, Dominican law references, safe buying process.

## Architecture

- **Model**: Claude Haiku 4.5 (`claude-haiku-4-5-20251001`)
- **Pattern**: Clean Architecture + CQRS with MediatR
- **Database**: PostgreSQL (chat sessions + messages)
- **Port**: 8080

## API Endpoints

| Method | Path                               | Auth     | Description           |
| ------ | ---------------------------------- | -------- | --------------------- |
| POST   | `/api/support/message`             | Optional | Send a chat message   |
| GET    | `/api/support/session/{sessionId}` | Optional | Get session history   |
| GET    | `/api/support/status`              | None     | Service health/status |

## What It Does NOT Do

- ❌ Does NOT recommend specific vehicles (that's RecoAgent)
- ❌ Does NOT search inventory/listings (that's SearchAgent)
- ❌ Does NOT process payments or modify accounts
- ❌ Does NOT access private user data
