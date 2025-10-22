-- Script para crear las tablas Outbox y ProcessedEvents
-- Ejecutar este script en la base de datos ticketflow

-- Tabla OutboxMessages
CREATE TABLE IF NOT EXISTS "OutboxMessages" (
    "Id" UUID PRIMARY KEY,
    "Type" VARCHAR(200) NOT NULL,
    "PayloadJson" TEXT NOT NULL,
    "OccurredAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "CorrelationId" VARCHAR(100),
    "DispatchedAt" TIMESTAMP WITHOUT TIME ZONE,
    "Attempts" INTEGER NOT NULL DEFAULT 0,
    "Error" VARCHAR(2000)
);

-- Índices para OutboxMessages
CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_Pending" 
    ON "OutboxMessages" ("DispatchedAt", "OccurredAt") 
    WHERE "DispatchedAt" IS NULL;

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_Type" 
    ON "OutboxMessages" ("Type");

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_CorrelationId" 
    ON "OutboxMessages" ("CorrelationId") 
    WHERE "CorrelationId" IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_DispatchedAt" 
    ON "OutboxMessages" ("DispatchedAt") 
    WHERE "DispatchedAt" IS NOT NULL;

-- Tabla ProcessedEvents
CREATE TABLE IF NOT EXISTS "ProcessedEvents" (
    "EventId" UUID PRIMARY KEY,
    "ProcessedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL
);

-- Índice para ProcessedEvents
CREATE INDEX IF NOT EXISTS "IX_ProcessedEvents_ProcessedAt" 
    ON "ProcessedEvents" ("ProcessedAt");

-- Confirmar que las tablas se crearon
SELECT 'Tablas Outbox creadas exitosamente' AS mensaje;
