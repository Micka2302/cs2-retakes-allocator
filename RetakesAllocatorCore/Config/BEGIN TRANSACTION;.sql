BEGIN TRANSACTION;

ALTER TABLE "UserSettings"
    ADD COLUMN IF NOT EXISTS "ZeusEnabled" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "UserSettings"
    ADD COLUMN IF NOT EXISTS "EnemyStuffEnabled" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "UserSettings"
    ADD COLUMN IF NOT EXISTS "EnemyStuffTeamPreference" INTEGER NOT NULL DEFAULT 0;

UPDATE "UserSettings"
SET "EnemyStuffTeamPreference" = 3
WHERE "EnemyStuffEnabled" = 1;

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

CREATE TABLE "ef_temp_UserSettings" (
    "UserId" bigint NOT NULL CONSTRAINT "PK_UserSettings" PRIMARY KEY,
    "EnemyStuffTeamPreference" INTEGER NOT NULL,
    "WeaponPreferences" text NULL,
    "ZeusEnabled" INTEGER NOT NULL
);

INSERT INTO "ef_temp_UserSettings" ("UserId", "EnemyStuffTeamPreference", "WeaponPreferences", "ZeusEnabled")
SELECT "UserId", "EnemyStuffTeamPreference", "WeaponPreferences", "ZeusEnabled"
FROM "UserSettings";

DROP TABLE "UserSettings";

ALTER TABLE "ef_temp_UserSettings" RENAME TO "UserSettings";

COMMIT;

PRAGMA foreign_keys = 1;


