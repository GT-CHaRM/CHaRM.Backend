query="USE master
GO
IF NOT EXISTS (
    SELECT name
        FROM sys.databases
        WHERE name = N'CharmDB'
)
CREATE DATABASE CharmDB
GO"

/opt/mssql-tools/bin/sqlcmd -S database -U sa -P 3OXa0H8T3U1wLh29 -Q "$query" -o /dev/null
