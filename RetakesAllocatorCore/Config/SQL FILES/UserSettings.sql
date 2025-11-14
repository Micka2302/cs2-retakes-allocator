-- MySQL schema dump for cs2-retakes-allocator
-- Contains the complete structure required by the plugin.

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `UserSettings`;

CREATE TABLE `UserSettings` (
    `UserId` BIGINT UNSIGNED NOT NULL,
    `WeaponPreferences` LONGTEXT NULL,
    `ZeusEnabled` TINYINT(1) NOT NULL DEFAULT 0,
    `EnemyStuffTeamPreference` INT NOT NULL DEFAULT 0,
    CONSTRAINT `PK_UserSettings` PRIMARY KEY (`UserId`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

DROP TABLE IF EXISTS `__EFMigrationsHistory`;

CREATE TABLE `__EFMigrationsHistory` (
    `MigrationId` VARCHAR(150) NOT NULL,
    `ProductVersion` VARCHAR(32) NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
    ('20240105045524_InitialCreate', '7.0.14'),
    ('20240105050248_DontAutoIncrement', '7.0.14'),
    ('20240116025022_BigIntTime', '7.0.14'),
    ('20250927201835_AddZeusPreferenceToUserSettings', '7.0.14'),
    ('20251007120000_AddEnemyStuffToUserSettings', '7.0.14'),
    ('20251012021500_AddEnemyStuffTeamPreferenceToUserSettings', '7.0.14');

SET FOREIGN_KEY_CHECKS = 1;
