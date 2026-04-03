CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL,
    "ProductVersion" varchar(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE IF NOT EXISTS search_agent_config (
    "Id" uuid NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "Model" varchar(100) NOT NULL,
    "Temperature" real NOT NULL,
    "MaxTokens" integer NOT NULL,
    "MinResultsPerPage" integer NOT NULL,
    "MaxResultsPerPage" integer NOT NULL,
    "SponsoredAffinityThreshold" real NOT NULL,
    "MaxSponsoredPercentage" real NOT NULL,
    "SponsoredPositions" varchar(50) NOT NULL,
    "SponsoredLabel" varchar(50) NOT NULL,
    "PriceRelaxPercent" integer NOT NULL,
    "YearRelaxRange" integer NOT NULL,
    "MaxRelaxationLevel" integer NOT NULL,
    "EnableCache" boolean NOT NULL,
    "CacheTtlSeconds" integer NOT NULL,
    "SemanticCacheThreshold" real NOT NULL,
    "MaxQueriesPerMinutePerIp" integer NOT NULL,
    "AiSearchTrafficPercent" integer NOT NULL,
    "SystemPromptOverride" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UpdatedBy" varchar(100),
    CONSTRAINT "PK_search_agent_config" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS search_queries (
    "Id" uuid NOT NULL,
    "OriginalQuery" varchar(500) NOT NULL,
    "ReformulatedQuery" varchar(500),
    "UserId" varchar(100),
    "SessionId" varchar(100),
    "IpAddress" varchar(45),
    "FiltersJson" jsonb,
    "Confidence" real NOT NULL,
    "FilterLevel" integer NOT NULL,
    "OrganicResultCount" integer NOT NULL,
    "SponsoredResultCount" integer NOT NULL,
    "TotalResultCount" integer NOT NULL,
    "LatencyMs" integer NOT NULL,
    "WasCached" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_search_queries" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_search_queries_CreatedAt" ON search_queries ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_search_queries_UserId" ON search_queries ("UserId");

INSERT INTO search_agent_config ("Id", "AiSearchTrafficPercent", "CacheTtlSeconds", "CreatedAt", "EnableCache", "IsEnabled", "MaxQueriesPerMinutePerIp", "MaxRelaxationLevel", "MaxResultsPerPage", "MaxSponsoredPercentage", "MaxTokens", "MinResultsPerPage", "Model", "PriceRelaxPercent", "SemanticCacheThreshold", "SponsoredAffinityThreshold", "SponsoredLabel", "SponsoredPositions", "SystemPromptOverride", "Temperature", "UpdatedAt", "UpdatedBy", "YearRelaxRange")
VALUES ('a1b2c3d4-e5f6-7890-abcd-ef1234567890', 100, 3600, '2026-03-02T20:50:45.5412440Z', true, true, 60, 5, 40, 0.25, 1024, 8, 'claude-haiku-4-5-20251001', 25, 0.95, 0.45, 'Patrocinado', '1,5,10', NULL, 0.2, '2026-03-02T20:50:45.5412440Z', NULL, 2)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260302205045_InitialCreate', '8.0.2')
ON CONFLICT DO NOTHING;
