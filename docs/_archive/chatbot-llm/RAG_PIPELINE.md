# OKLA Chatbot LLM — RAG Pipeline with pgvector

## Overview

The RAG (Retrieval-Augmented Generation) pipeline enables the Dealer Inventory
mode to search across thousands of vehicle listings without exceeding the
LLM's 8192-token context window.

---

## Architecture

```
User Query
    │
    ▼
┌─────────────────────────┐
│   EmbeddingService      │  sentence-transformers
│   all-MiniLM-L6-v2      │  384 dimensions
│   POST /v1/embeddings   │
└──────────┬──────────────┘
           │ query_embedding[384]
           ▼
┌─────────────────────────┐
│   VehicleEmbedding      │  PostgreSQL + pgvector
│   Table (pgvector)      │  IVFFlat index
│                         │  cosine similarity
│   + SQL filter:         │
│     WHERE dealer_id=X   │
│     AND price BETWEEN   │
│     AND fuel_type=...   │
└──────────┬──────────────┘
           │ top-5 results
           ▼
┌─────────────────────────┐
│   DealerInventoryStrategy│
│   BuildSystemPrompt()   │
│   → Formats vehicles    │
│   → Injects into prompt │
└──────────┬──────────────┘
           │ system_prompt + user_message
           ▼
┌─────────────────────────┐
│   Llama 3.1 8B          │
│   GBNF grammar          │
│   → JSON response       │
└─────────────────────────┘
```

## Embedding Pipeline

### 1. Vehicle Ingestion

When vehicles are synced from VehiclesSaleService:

```csharp
// InventorySyncService calls this after syncing
await _vectorSearchService.RebuildDealerEmbeddingsAsync(dealerId, vehicles, ct);
```

**Text representation for embedding:**

```
{Year} {Make} {Model} {Trim} {FuelType} {Transmission} {BodyType}
{Condition} {ExteriorColor} RD${Price} {Mileage}km {Location}
{Description (first 200 chars)}
```

### 2. Query Embedding

User message → 384-dim vector via `/v1/embeddings`:

```
"Busco un SUV Toyota automático" → [0.032, -0.118, 0.256, ...]
```

### 3. Hybrid Search

Combines **semantic similarity** (cosine distance) with **SQL filters**:

```sql
SELECT *, 1 - (embedding <=> @queryEmbedding) as similarity
FROM vehicle_embeddings
WHERE dealer_id = @dealerId
  AND (@make IS NULL OR LOWER(make) = LOWER(@make))
  AND (@minPrice IS NULL OR price >= @minPrice)
  AND (@maxPrice IS NULL OR price <= @maxPrice)
  AND (@fuelType IS NULL OR LOWER(fuel_type) = LOWER(@fuelType))
ORDER BY embedding <=> @queryEmbedding
LIMIT 5;
```

### 4. Filter Extraction

The `DealerInventoryStrategy` extracts filters from natural language:

| User says             | Extracted filter                      |
| --------------------- | ------------------------------------- |
| "Toyota Corolla"      | make=Toyota, model=Corolla            |
| "SUV automático"      | bodyType=SUV, transmission=Automática |
| "menos de 2 millones" | maxPrice=2000000                      |
| "gasolina"            | fuelType=Gasolina                     |
| "2023 o más nuevo"    | minYear=2023                          |

## pgvector Setup

### Extension & Table

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS vehicle_embeddings (
    id UUID PRIMARY KEY,
    vehicle_id UUID NOT NULL,
    dealer_id UUID NOT NULL,
    embedding vector(384) NOT NULL,
    make VARCHAR(100),
    model VARCHAR(100),
    year INT,
    price DECIMAL(18,2),
    fuel_type VARCHAR(50),
    transmission VARCHAR(50),
    body_type VARCHAR(50),
    condition VARCHAR(50),
    mileage INT,
    text_content TEXT,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- IVFFlat index for fast similarity search
CREATE INDEX IF NOT EXISTS idx_vehicle_embeddings_vector
ON vehicle_embeddings USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

-- Filter indexes
CREATE INDEX IF NOT EXISTS idx_vehicle_embeddings_dealer
ON vehicle_embeddings (dealer_id);
```

### Performance

| Metric                | Target     |
| --------------------- | ---------- |
| Embedding generation  | <50ms/text |
| Vector search (top-5) | <20ms      |
| Full RAG pipeline     | <100ms     |
| Max embeddings/dealer | 10,000     |

## Embedding Sync Strategy

1. **Initial load**: Full bulk upsert when dealer enables chatbot
2. **Incremental**: On vehicle CRUD events via RabbitMQ
3. **Scheduled**: Daily rebuild at 3AM to catch any drift
4. **On-demand**: Admin can trigger rebuild via API

## Fallback Strategy

If pgvector or embedding service is unavailable:

1. Fall back to SQL-only search (ChatbotVehicle table)
2. Use `BuildInventorySection()` from old approach (top 20 vehicles)
3. Log warning for monitoring
