-- Query optimizada para obtener el agente con menor carga
-- 
-- ALGORITMO:
-- 1. load_score = open_count + 1.5 * in_progress_count
-- 2. Desempates: load_score ASC → LastAssignedAt NULLS FIRST → AgentId ASC
-- 3. Solo agentes activos (Role='AGENT' AND IsActive=TRUE)
--
-- AJUSTES DE PESOS:
-- - Cambiar 1.5 a otro valor si IN_PROGRESS debe pesar más o menos
-- - open_count incluye: NEW, IN_PROGRESS, ON_HOLD

WITH agent_base AS (
  SELECT u."Id" AS agent_id, u."LastAssignedAt"
  FROM "Users" u
  WHERE u."Role" = 'AGENT' AND u."IsActive" = TRUE
),
counts AS (
  SELECT
    t."AssignedTo" AS agent_id,
    SUM(CASE WHEN t."Status" IN ('nuevo','en-proceso','en-espera') THEN 1 ELSE 0 END) AS open_count,
    SUM(CASE WHEN t."Status" = 'en-proceso' THEN 1 ELSE 0 END) AS in_progress_count
  FROM "Tickets" t
  WHERE t."AssignedTo" IS NOT NULL
  GROUP BY t."AssignedTo"
)
SELECT 
  a.agent_id,
  COALESCE(c.open_count, 0) AS open_count,
  COALESCE(c.in_progress_count, 0) AS in_progress_count,
  (COALESCE(c.open_count, 0) + 1.5 * COALESCE(c.in_progress_count, 0))::numeric AS load_score,
  a."LastAssignedAt"
FROM agent_base a
LEFT JOIN counts c ON c.agent_id = a.agent_id
ORDER BY load_score ASC, a."LastAssignedAt" NULLS FIRST, a.agent_id ASC
LIMIT 1;
