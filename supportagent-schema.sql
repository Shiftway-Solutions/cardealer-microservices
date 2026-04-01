CREATE TABLE IF NOT EXISTS chat_sessions (
    "Id" uuid NOT NULL PRIMARY KEY,
    "SessionId" varchar(64) NOT NULL,
    "UserId" varchar(128),
    "IpAddress" varchar(45),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "LastActivityAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "IsActive" boolean NOT NULL DEFAULT true,
    "MessageCount" integer NOT NULL DEFAULT 0,
    "LastModule" varchar(50)
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_chat_sessions_SessionId" ON chat_sessions ("SessionId");
CREATE INDEX IF NOT EXISTS "IX_chat_sessions_UserId" ON chat_sessions ("UserId");
CREATE INDEX IF NOT EXISTS "IX_chat_sessions_CreatedAt" ON chat_sessions ("CreatedAt");

CREATE TABLE IF NOT EXISTS chat_messages (
    "Id" uuid NOT NULL PRIMARY KEY,
    "SessionId" uuid NOT NULL REFERENCES chat_sessions("Id") ON DELETE CASCADE,
    "Role" varchar(20) NOT NULL,
    "Content" text NOT NULL,
    "DetectedModule" varchar(50),
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "InputTokens" integer,
    "OutputTokens" integer,
    "LatencyMs" integer
);
CREATE INDEX IF NOT EXISTS "IX_chat_messages_SessionId" ON chat_messages ("SessionId");
CREATE INDEX IF NOT EXISTS "IX_chat_messages_CreatedAt" ON chat_messages ("CreatedAt");

CREATE TABLE IF NOT EXISTS support_agent_config (
    "Id" integer NOT NULL PRIMARY KEY,
    "ModelId" varchar(100) NOT NULL,
    "MaxTokens" integer NOT NULL DEFAULT 512,
    "Temperature" real NOT NULL DEFAULT 0.3,
    "MaxConversationHistory" integer NOT NULL DEFAULT 10,
    "SessionTimeoutMinutes" integer NOT NULL DEFAULT 30,
    "IsActive" boolean NOT NULL DEFAULT true,
    "UpdatedBy" varchar(128),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW()
);

INSERT INTO support_agent_config ("Id", "ModelId", "MaxTokens", "Temperature", "MaxConversationHistory", "SessionTimeoutMinutes", "IsActive", "UpdatedBy", "UpdatedAt")
VALUES (1, 'claude-haiku-4-5-20251001', 512, 0.3, 10, 30, true, 'system', '2026-03-01 00:00:00+00')
ON CONFLICT ("Id") DO NOTHING;
