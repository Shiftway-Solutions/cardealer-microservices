-- Fix test users email confirmation status
-- Run this after docker compose down -v / postgres volume reset
-- Affected users: test credentials used in sprint audits
--
-- Context: Users registered via API default to EmailConfirmed=false.
-- Test users need EmailConfirmed=true to be able to login without
-- going through the email verification flow.

\c authservice;

UPDATE "Users"
SET "EmailConfirmed" = true
WHERE "Email" IN (
    'gmoreno@okla.com.do',
    'buyer002@okla-test.com',
    'nmateo@okla.com.do',
    'admin@okla.local'
)
AND "EmailConfirmed" = false;

-- Verify
SELECT "Email", "EmailConfirmed" FROM "Users" ORDER BY "Email";
