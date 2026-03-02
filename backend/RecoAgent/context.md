# RecoAgent — AI Vehicle Recommendation Engine

## Overview

RecoAgent is the proactive recommendation engine for OKLA Marketplace. Unlike SearchAgent (which responds to user queries), RecoAgent analyzes user behavior profiles and generates personalized vehicle recommendations with explanations in Dominican Spanish.

## Architecture

- **Model**: Claude Sonnet 4.5 (`claude-sonnet-4-5-20251022`)
- **Pattern**: Clean Architecture + CQRS (MediatR)
- **Database**: PostgreSQL (reco_agent_config, recommendation_logs)
- **Cache**: Redis (batch: 4h TTL, real-time: 15min TTL)
- **Port**: 8080

## Endpoints

| Method | Path                        | Auth   | Description                           |
| ------ | --------------------------- | ------ | ------------------------------------- |
| POST   | `/api/reco-agent/recommend` | Public | Generate personalized recommendations |
| POST   | `/api/reco-agent/feedback`  | Public | Record feedback (thumbs up/down)      |
| GET    | `/api/reco-agent/config`    | Admin  | Get current configuration             |
| PUT    | `/api/reco-agent/config`    | Admin  | Update configuration                  |
| GET    | `/api/reco-agent/status`    | Public | Health/status check                   |

## Business Rules (Absolute)

1. **SIEMPRE RANGO**: Always 8-12 recommendations, never one.
2. **PATROCINADOS CON AFINIDAD**: Sponsored items with affinity >= 0.50 included as "Destacado".
3. **DIVERSIFICACIÓN**: No brand > 40% of recommendations.
4. **EXPLICACIÓN OBLIGATORIA**: Every recommendation has `razon_recomendacion` in Dominican Spanish.
5. **TRANSPARENCIA**: Sponsored items labeled as `tipo_recomendacion: "patrocinado"`.

## Execution Modes

- **Batch**: Pre-calculated every 4h per active user. Served from cache in <200ms.
- **Real-time**: On-demand when user opens a listing. <1,500ms latency target.
- **Hybrid**: Serves cached batch + updates in parallel.

## Cold Start Levels

- **0**: No data → popularity-based (top vehicles in RD)
- **1**: New account, <3 views → onboarding preferences
- **2**: 3-10 views → content-based filtering
- **3**: 10+ views or 1 favorite → full engine

## Configuration Defaults

| Parameter             | Value     |
| --------------------- | --------- |
| Temperature           | 0.5       |
| Max Tokens            | 2,048     |
| Min Recommendations   | 8         |
| Max Recommendations   | 12        |
| Sponsored Threshold   | 0.50      |
| Sponsored Positions   | 2, 6, 11  |
| Sponsored Label       | Destacado |
| Max Same Brand %      | 40%       |
| Batch Refresh         | 4 hours   |
| Cache TTL (batch)     | 14,400s   |
| Cache TTL (real-time) | 900s      |

## Local Development

```bash
cd backend/RecoAgent
dotnet build RecoAgent.sln
dotnet test RecoAgent.Tests/
dotnet run --project RecoAgent.Api/
```

## Docker

```bash
cd backend
docker build -f RecoAgent/Dockerfile -t recoagent:latest .
docker run -p 8080:8080 -e Claude__ApiKey=YOUR_KEY recoagent:latest
```
