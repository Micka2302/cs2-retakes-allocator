using System;

namespace RetakesAllocatorCore.Db;

internal static class MySqlColumnSqlHelper
{
    public static string BuildAddColumnIfMissingSql(string tableName, string columnName, string columnDefinition)
    {
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(columnName, nameof(columnName));

        if (string.IsNullOrWhiteSpace(columnDefinition))
        {
            throw new ArgumentException("Column definition must not be empty.", nameof(columnDefinition));
        }

        return BuildConditionalColumnSql(
            tableName,
            columnName,
            " = 0",
            $"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {columnDefinition}");
    }

    public static string BuildDropColumnIfExistsSql(string tableName, string columnName)
    {
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(columnName, nameof(columnName));

        return BuildConditionalColumnSql(
            tableName,
            columnName,
            " > 0",
            $"ALTER TABLE `{tableName}` DROP COLUMN `{columnName}`");
    }

    private static string BuildConditionalColumnSql(
        string tableName,
        string columnName,
        string comparatorClause,
        string statementWhenConditionMatches)
    {
        var escapedTable = EscapeSqlLiteral(tableName);
        var escapedColumn = EscapeSqlLiteral(columnName);
        var escapedStatement = EscapeSqlLiteral(statementWhenConditionMatches);

        return $"""
SET @ddl := (
    SELECT IF(
        COUNT(*){comparatorClause},
        '{escapedStatement}',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = '{escapedTable}'
      AND COLUMN_NAME = '{escapedColumn}');
PREPARE ddl_stmt FROM @ddl;
EXECUTE ddl_stmt;
DEALLOCATE PREPARE ddl_stmt;
""";
    }

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''");
    }

    private static void ValidateIdentifier(string identifier, string paramName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier must not be null or whitespace.", paramName);
        }

        if (identifier.Contains('`'))
        {
            throw new ArgumentException("Identifier must not contain backticks.", paramName);
        }
    }
}
