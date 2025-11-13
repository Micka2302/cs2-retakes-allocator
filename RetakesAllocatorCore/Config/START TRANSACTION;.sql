START TRANSACTION;

ALTER TABLE `UserSettings`
    MODIFY COLUMN `UserId` BIGINT UNSIGNED NOT NULL;

ALTER TABLE `UserSettings`
    MODIFY COLUMN `WeaponPreferences` LONGTEXT NULL;

SET @ddl := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE `UserSettings` ADD COLUMN `ZeusEnabled` TINYINT(1) NOT NULL DEFAULT 0',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserSettings'
      AND COLUMN_NAME = 'ZeusEnabled');
PREPARE ddl_stmt FROM @ddl;
EXECUTE ddl_stmt;
DEALLOCATE PREPARE ddl_stmt;

SET @ddl := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE `UserSettings` ADD COLUMN `EnemyStuffEnabled` TINYINT(1) NOT NULL DEFAULT 0',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserSettings'
      AND COLUMN_NAME = 'EnemyStuffEnabled');
PREPARE ddl_stmt FROM @ddl;
EXECUTE ddl_stmt;
DEALLOCATE PREPARE ddl_stmt;

SET @ddl := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE `UserSettings` ADD COLUMN `EnemyStuffTeamPreference` INT NOT NULL DEFAULT 0',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserSettings'
      AND COLUMN_NAME = 'EnemyStuffTeamPreference');
PREPARE ddl_stmt FROM @ddl;
EXECUTE ddl_stmt;
DEALLOCATE PREPARE ddl_stmt;

UPDATE `UserSettings`
SET `EnemyStuffTeamPreference` = 3
WHERE `EnemyStuffEnabled` = 1;

SET @ddl := (
    SELECT IF(
        COUNT(*) > 0,
        'ALTER TABLE `UserSettings` DROP COLUMN `EnemyStuffEnabled`',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'UserSettings'
      AND COLUMN_NAME = 'EnemyStuffEnabled');
PREPARE ddl_stmt FROM @ddl;
EXECUTE ddl_stmt;
DEALLOCATE PREPARE ddl_stmt;

COMMIT;
