using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HRMS.API.Services;

public class RbacDataInitializer
{
    private readonly HrmsDbContext _dbContext;

    public RbacDataInitializer(HrmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync()
    {
        await EnsureSchemaAsync();
        await SeedMenusAsync();
        await SeedSystemSettingsAsync();
        await SeedOrgUnitsAsync();
        await SeedCustomersAsync();
        await SeedSuppliersAsync();
        await SeedAdminDataAsync();
        await SeedDemoDirectoryDataAsync();
    }

    private async Task EnsureSchemaAsync()
    {
        if (!_dbContext.Database.IsRelational())
        {
            // InMemory 等非关系型提供程序不支持 INFORMATION_SCHEMA/ALTER TABLE。
            return;
        }

        var sqlStatements = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS `sys_menu` (
              `Id` char(36) NOT NULL,
              `MenuKey` varchar(100) NOT NULL,
              `MenuName` varchar(100) NOT NULL,
              `ParentId` char(36) NULL,
              `MenuType` varchar(20) NOT NULL,
              `SortOrder` int NOT NULL DEFAULT 0,
              `RoutePath` varchar(200) NULL,
              `ComponentPath` varchar(200) NULL,
              `Icon` varchar(50) NULL,
              `IsVisible` tinyint(1) NOT NULL DEFAULT 1,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `PermissionCode` varchar(100) NULL,
              `CreatedAt` datetime(6) NOT NULL,
              `UpdatedAt` datetime(6) NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_role` (
              `Id` char(36) NOT NULL,
              `RoleCode` varchar(100) NOT NULL,
              `RoleName` varchar(100) NOT NULL,
              `Description` varchar(500) NULL,
              `OrgUnitId` char(36) NULL,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `CreatedAt` datetime(6) NOT NULL,
              `UpdatedAt` datetime(6) NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_role_permission` (
              `Id` char(36) NOT NULL,
              `RoleId` char(36) NOT NULL,
              `MenuId` char(36) NOT NULL,
              `PermissionCode` varchar(100) NOT NULL,
              `PermissionType` varchar(50) NOT NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_token_session` (
              `Id` char(36) NOT NULL,
              `Token` varchar(500) NOT NULL,
              `UserId` char(36) NOT NULL,
              `IsAdmin` tinyint(1) NOT NULL DEFAULT 0,
              `PermissionsJson` longtext NULL,
              `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
              `ExpiresAt` datetime(6) NOT NULL,
              PRIMARY KEY (`Id`),
              UNIQUE KEY `IX_sys_token_session_Token` (`Token`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_post` (
              `Id` char(36) NOT NULL,
              `PostCode` varchar(100) NOT NULL,
              `PostName` varchar(100) NOT NULL,
              `Description` varchar(500) NULL,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `CreatedAt` datetime(6) NOT NULL,
              `UpdatedAt` datetime(6) NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_post_permission` (
              `Id` char(36) NOT NULL,
              `PostId` char(36) NOT NULL,
              `MenuId` char(36) NOT NULL,
              `PermissionCode` varchar(100) NOT NULL,
              `PermissionType` varchar(50) NOT NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_user` (
              `Id` char(36) NOT NULL,
              `Username` varchar(100) NOT NULL,
              `Password` varchar(200) NOT NULL,
              `Name` varchar(100) NOT NULL,
              `Phone` varchar(20) NULL,
              `Email` varchar(100) NULL,
              `EmployeeId` char(36) NULL,
              `PostId` char(36) NULL,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `IsAdmin` tinyint(1) NOT NULL DEFAULT 0,
              `CreatedAt` datetime(6) NOT NULL,
              `UpdatedAt` datetime(6) NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_user_role` (
              `Id` char(36) NOT NULL,
              `UserId` char(36) NOT NULL,
              `RoleId` char(36) NOT NULL,
              PRIMARY KEY (`Id`)
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS `sys_config_param` (
              `Id` char(36) NOT NULL,
              `Category` varchar(50) NOT NULL,
              `ParamKey` varchar(100) NOT NULL,
              `ParamValue` varchar(4000) NULL,
              `Description` varchar(500) NULL,
              `OrgUnitId` char(36) NULL,
              `IsGlobal` tinyint(1) NOT NULL DEFAULT 1,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `TakeEffectImmediately` tinyint(1) NOT NULL DEFAULT 1,
              `ScheduledTakeEffectDate` datetime(6) NULL,
              `CreatedAt` datetime(6) NOT NULL,
              `UpdatedAt` datetime(6) NULL,
              `ChangeReason` varchar(500) NULL,
              PRIMARY KEY (`Id`)
            );
            """
        };

        foreach (var sql in sqlStatements)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        await EnsureColumnAsync("sys_menu", "MenuKey", "`MenuKey` varchar(100) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("sys_menu", "ParentId", "`ParentId` char(36) NULL");
        await EnsureColumnAsync("sys_menu", "MenuType", "`MenuType` varchar(20) NOT NULL DEFAULT 'page'");
        await EnsureColumnAsync("sys_menu", "IsActive", "`IsActive` tinyint(1) NOT NULL DEFAULT 1");

        await EnsureColumnAsync("sys_role", "RoleCode", "`RoleCode` varchar(100) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("sys_role", "IsActive", "`IsActive` tinyint(1) NOT NULL DEFAULT 1");

        await EnsureColumnAsync("sys_role_permission", "MenuId", "`MenuId` char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        await EnsureColumnAsync("sys_config_param", "Category", "`Category` varchar(50) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("sys_config_param", "ParamKey", "`ParamKey` varchar(100) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("sys_config_param", "ParamValue", "`ParamValue` longtext NULL");
        await EnsureColumnAsync("sys_config_param", "Description", "`Description` varchar(500) NULL");
        await EnsureColumnAsync("sys_config_param", "OrgUnitId", "`OrgUnitId` char(36) NULL");
        await EnsureColumnAsync("sys_config_param", "IsGlobal", "`IsGlobal` tinyint(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync("sys_config_param", "IsActive", "`IsActive` tinyint(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync("sys_config_param", "TakeEffectImmediately", "`TakeEffectImmediately` tinyint(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync("sys_config_param", "ScheduledTakeEffectDate", "`ScheduledTakeEffectDate` datetime(6) NULL");
        await EnsureColumnAsync("sys_config_param", "CreatedAt", "`CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)");
        await EnsureColumnAsync("sys_config_param", "UpdatedAt", "`UpdatedAt` datetime(6) NULL");
        await EnsureColumnAsync("sys_config_param", "ChangeReason", "`ChangeReason` varchar(500) NULL");

        await EnsureTableAsync(
            "org_unit",
            """
            CREATE TABLE IF NOT EXISTS `org_unit` (
              `Id` char(36) NOT NULL,
              `Code` varchar(50) NOT NULL,
              `Name` varchar(200) NOT NULL,
              `Level` int NOT NULL DEFAULT 0,
              `ParentId` char(36) NULL,
              `ManagerId` varchar(100) NULL,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
              `UpdatedAt` datetime(6) NULL,
              PRIMARY KEY (`Id`)
            );
            """);
        await EnsureColumnAsync("org_unit", "Code", "`Code` varchar(50) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("org_unit", "Name", "`Name` varchar(200) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("org_unit", "Level", "`Level` int NOT NULL DEFAULT 0");
        await EnsureColumnAsync("org_unit", "ParentId", "`ParentId` char(36) NULL");
        await EnsureColumnAsync("org_unit", "ManagerId", "`ManagerId` varchar(100) NULL");
        await EnsureColumnAsync("org_unit", "IsActive", "`IsActive` tinyint(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync("org_unit", "CreatedAt", "`CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)");
        await EnsureColumnAsync("org_unit", "UpdatedAt", "`UpdatedAt` datetime(6) NULL");

        await EnsureTableAsync(
            "employee",
            """
            CREATE TABLE IF NOT EXISTS `employee` (
              `Id` char(36) NOT NULL,
              `EmployeeNo` varchar(50) NOT NULL,
              `Name` varchar(100) NOT NULL,
              `Gender` int NOT NULL DEFAULT 0,
              `IdCard` varchar(18) NULL,
              `Phone` varchar(20) NULL,
              `Email` varchar(100) NULL,
              `EmployeeType` int NOT NULL DEFAULT 0,
              `SalaryMode` int NOT NULL DEFAULT 0,
              `OrgUnitId` char(36) NOT NULL,
              `ThirdPartyCompanyId` char(36) NULL,
              `JobTitle` varchar(100) NULL,
              `Level` varchar(50) NULL,
              `Tags` json NULL,
              `HourlyRate` decimal(10,2) NULL,
              `MonthlySalary` decimal(12,2) NULL,
              `PieceRatePrice` decimal(10,2) NULL,
              `SocialSecurityBase` decimal(12,2) NULL,
              `HousingFundBase` decimal(12,2) NULL,
              `TrialEndDate` datetime(6) NULL,
              `TrialDaysRemaining` int NOT NULL DEFAULT 3,
              `ProbationDays` int NOT NULL DEFAULT 30,
              `ContractType` int NOT NULL DEFAULT 0,
              `ContractStartDate` datetime(6) NULL,
              `ContractEndDate` datetime(6) NULL,
              `HireDate` datetime(6) NULL,
              `IsBlacklisted` tinyint(1) NOT NULL DEFAULT 0,
              `IsActive` tinyint(1) NOT NULL DEFAULT 1,
              `DismissDate` datetime(6) NULL,
              `DismissReason` varchar(500) NULL,
              `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
              `UpdatedAt` datetime(6) NULL,
              PRIMARY KEY (`Id`)
            );
            """);
        await EnsureColumnAsync("employee", "EmployeeNo", "`EmployeeNo` varchar(50) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("employee", "Name", "`Name` varchar(100) NOT NULL DEFAULT ''");
        await EnsureColumnAsync("employee", "Gender", "`Gender` int NOT NULL DEFAULT 0");
        await EnsureColumnAsync("employee", "IdCard", "`IdCard` varchar(18) NULL");
        await EnsureColumnAsync("employee", "Phone", "`Phone` varchar(20) NULL");
        await EnsureColumnAsync("employee", "Email", "`Email` varchar(100) NULL");
        await EnsureColumnAsync("employee", "EmployeeType", "`EmployeeType` int NOT NULL DEFAULT 0");
        await EnsureColumnAsync("employee", "SalaryMode", "`SalaryMode` int NOT NULL DEFAULT 0");
        await EnsureColumnAsync("employee", "OrgUnitId", "`OrgUnitId` char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        await EnsureColumnAsync("employee", "ThirdPartyCompanyId", "`ThirdPartyCompanyId` char(36) NULL");
        await EnsureColumnAsync("employee", "JobTitle", "`JobTitle` varchar(100) NULL");
        await EnsureColumnAsync("employee", "Level", "`Level` varchar(50) NULL");
        await EnsureColumnAsync("employee", "Tags", "`Tags` json NULL");
        await EnsureColumnAsync("employee", "HourlyRate", "`HourlyRate` decimal(10,2) NULL");
        await EnsureColumnAsync("employee", "MonthlySalary", "`MonthlySalary` decimal(12,2) NULL");
        await EnsureColumnAsync("employee", "PieceRatePrice", "`PieceRatePrice` decimal(10,2) NULL");
        await EnsureColumnAsync("employee", "SocialSecurityBase", "`SocialSecurityBase` decimal(12,2) NULL");
        await EnsureColumnAsync("employee", "HousingFundBase", "`HousingFundBase` decimal(12,2) NULL");
        await EnsureColumnAsync("employee", "TrialEndDate", "`TrialEndDate` datetime(6) NULL");
        await EnsureColumnAsync("employee", "TrialDaysRemaining", "`TrialDaysRemaining` int NOT NULL DEFAULT 3");
        await EnsureColumnAsync("employee", "ProbationDays", "`ProbationDays` int NOT NULL DEFAULT 30");
        await EnsureColumnAsync("employee", "ContractType", "`ContractType` int NOT NULL DEFAULT 0");
        await EnsureColumnAsync("employee", "ContractStartDate", "`ContractStartDate` datetime(6) NULL");
        await EnsureColumnAsync("employee", "ContractEndDate", "`ContractEndDate` datetime(6) NULL");
        await EnsureColumnAsync("employee", "HireDate", "`HireDate` datetime(6) NULL");
        await EnsureColumnAsync("employee", "IsBlacklisted", "`IsBlacklisted` tinyint(1) NOT NULL DEFAULT 0");
        await EnsureColumnAsync("employee", "IsActive", "`IsActive` tinyint(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync("employee", "DismissDate", "`DismissDate` datetime(6) NULL");
        await EnsureColumnAsync("employee", "DismissReason", "`DismissReason` varchar(500) NULL");
        await EnsureColumnAsync("employee", "CreatedAt", "`CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)");
        await EnsureColumnAsync("employee", "UpdatedAt", "`UpdatedAt` datetime(6) NULL");

        await EnsureIndexAsync("employee", "IX_employee_EmployeeNo", new[] { "EmployeeNo" }, unique: true);
        await EnsureIndexAsync("employee", "IX_employee_OrgUnitId", new[] { "OrgUnitId" }, unique: false);
        await EnsureIndexAsync("employee", "IX_employee_ThirdPartyCompanyId", new[] { "ThirdPartyCompanyId" }, unique: false);
        await DropIndexIfExistsAsync("employee", "idx_employee_no");

        await EnsureIndexAsync("org_unit", "IX_org_unit_Code", new[] { "Code" }, unique: true);
        await EnsureIndexAsync("org_unit", "IX_org_unit_ParentId", new[] { "ParentId" }, unique: false);

        await EnsureForeignKeyAsync("org_unit", "FK_org_unit_org_unit_ParentId", "ParentId", "org_unit", "Id");
        await EnsureForeignKeyAsync("employee", "FK_employee_org_unit_OrgUnitId", "OrgUnitId", "org_unit", "Id");
        await EnsureForeignKeyAsync("employee", "FK_employee_org_unit_ThirdPartyCompanyId", "ThirdPartyCompanyId", "org_unit", "Id");
    }

    private async Task EnsureTableAsync(string tableName, string createTableSql)
    {
        var exists = await QueryAsync(
            """
            SELECT 1
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
            LIMIT 1
            """,
            command => AddParameter(command, "@tableName", tableName),
            reader => reader.GetInt32(0));

        if (exists.Count == 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync(createTableSql);
        }
    }

    private async Task EnsureColumnAsync(string tableName, string columnName, string columnDefinition)
    {
        var column = await GetColumnMetadataAsync(tableName, columnName);
        if (column == null)
        {
            await _dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE `{tableName}` ADD COLUMN {columnDefinition};");
            return;
        }

        var expectedColumnType = ExtractColumnType(columnDefinition);
        var expectedNullable = !columnDefinition.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase);
        var typeMatches = string.Equals(column.ColumnType, expectedColumnType, StringComparison.OrdinalIgnoreCase);
        var nullableMatches = column.IsNullable == expectedNullable;

        if (!typeMatches || !nullableMatches)
        {
            await _dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE `{tableName}` MODIFY COLUMN {columnDefinition};");
        }
    }

    private async Task EnsureIndexAsync(string tableName, string indexName, IReadOnlyList<string> columns, bool unique)
    {
        var indexes = await GetIndexMetadataAsync(tableName);
        var exists = indexes
            .GroupBy(x => x.IndexName, StringComparer.OrdinalIgnoreCase)
            .Any(group =>
            {
                var orderedColumns = group
                    .OrderBy(x => x.SeqInIndex)
                    .Select(x => x.ColumnName)
                    .ToList();

                return group.First().NonUnique != unique
                    && orderedColumns.SequenceEqual(columns, StringComparer.OrdinalIgnoreCase);
            });

        if (exists)
        {
            return;
        }

        var columnSql = string.Join(", ", columns.Select(column => $"`{column}`"));
        var createSql = unique
            ? $"CREATE UNIQUE INDEX `{indexName}` ON `{tableName}` ({columnSql});"
            : $"CREATE INDEX `{indexName}` ON `{tableName}` ({columnSql});";
        await _dbContext.Database.ExecuteSqlRawAsync(createSql);
    }

    private async Task DropIndexIfExistsAsync(string tableName, string indexName)
    {
        var indexes = await GetIndexMetadataAsync(tableName);
        if (!indexes.Any(x => string.Equals(x.IndexName, indexName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync($"DROP INDEX `{indexName}` ON `{tableName}`;");
    }

    private async Task EnsureForeignKeyAsync(string tableName, string constraintName, string columnName, string referencedTableName, string referencedColumnName)
    {
        var foreignKeys = await GetForeignKeyMetadataAsync(tableName);
        var exists = foreignKeys.Any(x =>
            string.Equals(x.ColumnName, columnName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.ReferencedTableName, referencedTableName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.ReferencedColumnName, referencedColumnName, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE `{tableName}` ADD CONSTRAINT `{constraintName}` FOREIGN KEY (`{columnName}`) REFERENCES `{referencedTableName}` (`{referencedColumnName}`);");
    }

    private async Task<ColumnMetadata?> GetColumnMetadataAsync(string tableName, string columnName)
    {
        var rows = await QueryAsync(
            """
            SELECT COLUMN_TYPE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName
            LIMIT 1
            """,
            command =>
            {
                AddParameter(command, "@tableName", tableName);
                AddParameter(command, "@columnName", columnName);
            },
            reader => new ColumnMetadata(
                reader.GetString(0),
                string.Equals(reader.GetString(1), "YES", StringComparison.OrdinalIgnoreCase)));

        return rows.FirstOrDefault();
    }

    private async Task<List<IndexMetadata>> GetIndexMetadataAsync(string tableName)
    {
        return await QueryAsync(
            """
            SELECT INDEX_NAME, NON_UNIQUE, SEQ_IN_INDEX, COLUMN_NAME
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
            ORDER BY INDEX_NAME, SEQ_IN_INDEX
            """,
            command => AddParameter(command, "@tableName", tableName),
            reader => new IndexMetadata(
                reader.GetString(0),
                reader.GetInt32(1) == 1,
                reader.GetInt32(2),
                reader.GetString(3)));
    }

    private async Task<List<ForeignKeyMetadata>> GetForeignKeyMetadataAsync(string tableName)
    {
        return await QueryAsync(
            """
            SELECT kcu.CONSTRAINT_NAME, kcu.COLUMN_NAME, kcu.REFERENCED_TABLE_NAME, kcu.REFERENCED_COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            WHERE kcu.CONSTRAINT_SCHEMA = DATABASE()
              AND kcu.TABLE_NAME = @tableName
              AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
            ORDER BY kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION
            """,
            command => AddParameter(command, "@tableName", tableName),
            reader => new ForeignKeyMetadata(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Action<IDbCommand> configure, Func<IDataReader, T> map)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            configure(command);

            var results = new List<T>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(map(reader));
            }

            return results;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string ExtractColumnType(string columnDefinition)
    {
        var definition = columnDefinition.Trim();
        var closingIndex = definition.IndexOf('`', 1);
        if (closingIndex < 0)
        {
            return definition;
        }

        var remainder = definition[(closingIndex + 1)..].Trim();
        var separators = new[] { " NOT NULL", " NULL", " DEFAULT", " COMMENT", " COLLATE" };
        var endIndex = separators
            .Select(separator => remainder.IndexOf(separator, StringComparison.OrdinalIgnoreCase))
            .Where(index => index >= 0)
            .DefaultIfEmpty(remainder.Length)
            .Min();

        return remainder[..endIndex].Trim();
    }

    private sealed record ColumnMetadata(string ColumnType, bool IsNullable);
    private sealed record IndexMetadata(string IndexName, bool NonUnique, int SeqInIndex, string ColumnName);
    private sealed record ForeignKeyMetadata(string ConstraintName, string ColumnName, string ReferencedTableName, string ReferencedColumnName);

    private async Task SeedOrgUnitsAsync()
    {
        var orgUnits = await _dbContext.OrgUnits.OrderBy(x => x.CreatedAt).ToListAsync();
        if (orgUnits.Count == 0)
        {
            await SeedDefaultOrgUnitsAsync();
            return;
        }

        var changed = await SyncDefaultOrgUnitsAsync(orgUnits);
        changed |= NormalizeOrgLevelsByDepth(orgUnits);

        if (changed)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedDefaultOrgUnitsAsync()
    {
        var now = DateTime.UtcNow;

        var headquarter = new OrgUnit
        {
            Code = "HQ",
            Name = "总公司（合肥）",
            Level = OrgLevel.Headquarters,
            IsActive = true,
            CreatedAt = now
        };

        var fuqingBranch = new OrgUnit
        {
            Code = "BRANCH-FQ",
            Name = "福清分公司",
            Level = OrgLevel.Branch,
            ParentId = headquarter.Id,
            IsActive = true,
            CreatedAt = now
        };

        var qingdaoBranch = new OrgUnit
        {
            Code = "BRANCH-QD",
            Name = "青岛分公司",
            Level = OrgLevel.Branch,
            ParentId = headquarter.Id,
            IsActive = true,
            CreatedAt = now
        };

        var nanjingBranch = new OrgUnit
        {
            Code = "BRANCH-NJ",
            Name = "南京分公司",
            Level = OrgLevel.Branch,
            ParentId = headquarter.Id,
            IsActive = true,
            CreatedAt = now
        };

        var chongqingBranch = new OrgUnit
        {
            Code = "BRANCH-CQ",
            Name = "重庆分公司",
            Level = OrgLevel.Branch,
            ParentId = headquarter.Id,
            IsActive = true,
            CreatedAt = now
        };

        var fuqingHr = new OrgUnit
        {
            Code = "FQ-HR",
            Name = "人事部",
            Level = OrgLevel.Department,
            ParentId = fuqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var fuqingSales = new OrgUnit
        {
            Code = "FQ-SALES",
            Name = "销售部",
            Level = OrgLevel.Department,
            ParentId = fuqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var fuqingOba = new OrgUnit
        {
            Code = "FQ-OBA",
            Name = "OBA",
            Level = OrgLevel.Department,
            ParentId = fuqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var fuqingMaterialPrep = new OrgUnit
        {
            Code = "FQ-PREP",
            Name = "备料",
            Level = OrgLevel.Department,
            ParentId = fuqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var fuqingRepair = new OrgUnit
        {
            Code = "FQ-REPAIR",
            Name = "维修",
            Level = OrgLevel.Department,
            ParentId = fuqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var qingdaoHr = new OrgUnit
        {
            Code = "QD-HR",
            Name = "人事部",
            Level = OrgLevel.Department,
            ParentId = qingdaoBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var qingdaoSales = new OrgUnit
        {
            Code = "QD-SALES",
            Name = "销售部",
            Level = OrgLevel.Department,
            ParentId = qingdaoBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var qingdaoOba = new OrgUnit
        {
            Code = "QD-OBA",
            Name = "OBA",
            Level = OrgLevel.Department,
            ParentId = qingdaoBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var qingdaoMaterialPrep = new OrgUnit
        {
            Code = "QD-PREP",
            Name = "备料",
            Level = OrgLevel.Department,
            ParentId = qingdaoBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var qingdaoRepair = new OrgUnit
        {
            Code = "QD-REPAIR",
            Name = "维修",
            Level = OrgLevel.Department,
            ParentId = qingdaoBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var nanjingHr = new OrgUnit
        {
            Code = "NJ-HR",
            Name = "人事部",
            Level = OrgLevel.Department,
            ParentId = nanjingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var nanjingSales = new OrgUnit
        {
            Code = "NJ-SALES",
            Name = "销售部",
            Level = OrgLevel.Department,
            ParentId = nanjingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var nanjingOba = new OrgUnit
        {
            Code = "NJ-OBA",
            Name = "OBA",
            Level = OrgLevel.Department,
            ParentId = nanjingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var nanjingMaterialPrep = new OrgUnit
        {
            Code = "NJ-PREP",
            Name = "备料",
            Level = OrgLevel.Department,
            ParentId = nanjingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var nanjingRepair = new OrgUnit
        {
            Code = "NJ-REPAIR",
            Name = "维修",
            Level = OrgLevel.Department,
            ParentId = nanjingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var chongqingHr = new OrgUnit
        {
            Code = "CQ-HR",
            Name = "人事部",
            Level = OrgLevel.Department,
            ParentId = chongqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var chongqingSales = new OrgUnit
        {
            Code = "CQ-SALES",
            Name = "销售部",
            Level = OrgLevel.Department,
            ParentId = chongqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var chongqingOba = new OrgUnit
        {
            Code = "CQ-OBA",
            Name = "OBA",
            Level = OrgLevel.Department,
            ParentId = chongqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var chongqingMaterialPrep = new OrgUnit
        {
            Code = "CQ-PREP",
            Name = "备料",
            Level = OrgLevel.Department,
            ParentId = chongqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        var chongqingRepair = new OrgUnit
        {
            Code = "CQ-REPAIR",
            Name = "维修",
            Level = OrgLevel.Department,
            ParentId = chongqingBranch.Id,
            IsActive = true,
            CreatedAt = now
        };

        _dbContext.OrgUnits.AddRange(
            headquarter,
            fuqingBranch,
            qingdaoBranch,
            nanjingBranch,
            chongqingBranch,
            fuqingHr,
            fuqingSales,
            fuqingOba,
            fuqingMaterialPrep,
            fuqingRepair,
            qingdaoHr,
            qingdaoSales,
            qingdaoOba,
            qingdaoMaterialPrep,
            qingdaoRepair,
            nanjingHr,
            nanjingSales,
            nanjingOba,
            nanjingMaterialPrep,
            nanjingRepair,
            chongqingHr,
            chongqingSales,
            chongqingOba,
            chongqingMaterialPrep,
            chongqingRepair);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<bool> SyncDefaultOrgUnitsAsync(List<OrgUnit> orgUnits)
    {
        var now = DateTime.UtcNow;
        var definitions = GetOrgUnitDefinitions();
        var targetMap = new Dictionary<string, OrgUnit>(StringComparer.OrdinalIgnoreCase);
        var reusedIds = new HashSet<Guid>();
        var changed = false;

        foreach (var definition in definitions)
        {
            var parent = definition.ParentKey is null ? null : targetMap[definition.ParentKey];
            var orgUnit = FindMatchingOrgUnit(definition, orgUnits, targetMap, reusedIds);

            if (orgUnit == null)
            {
                orgUnit = new OrgUnit
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Level = (OrgLevel)definition.Level,
                    ParentId = parent?.Id,
                    IsActive = true,
                    CreatedAt = now
                };
                _dbContext.OrgUnits.Add(orgUnit);
                orgUnits.Add(orgUnit);
                changed = true;
            }
            else
            {
                var orgUnitChanged = false;
                if (!string.Equals(orgUnit.Code, definition.Code, StringComparison.OrdinalIgnoreCase))
                {
                    orgUnit.Code = definition.Code;
                    orgUnitChanged = true;
                }

                if (!string.Equals(orgUnit.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                {
                    orgUnit.Name = definition.Name;
                    orgUnitChanged = true;
                }

                if ((int)orgUnit.Level != definition.Level)
                {
                    orgUnit.Level = (OrgLevel)definition.Level;
                    orgUnitChanged = true;
                }

                if (orgUnit.ParentId != parent?.Id)
                {
                    orgUnit.ParentId = parent?.Id;
                    orgUnitChanged = true;
                }

                if (!orgUnit.IsActive)
                {
                    orgUnit.IsActive = true;
                    orgUnitChanged = true;
                }

                if (orgUnitChanged)
                {
                    orgUnit.UpdatedAt = now;
                    changed = true;
                }
            }

            targetMap[definition.Key] = orgUnit;
            reusedIds.Add(orgUnit.Id);
        }

        var orgMap = orgUnits.ToDictionary(x => x.Id);
        var obsoleteOrgUnits = orgUnits
            .Where(x => !reusedIds.Contains(x.Id) && !IsSupplierOrgUnit(x))
            .ToList();

        foreach (var orgUnit in obsoleteOrgUnits)
        {
            var replacement = ResolveReplacementOrgUnit(orgUnit, orgMap, targetMap);
            if (replacement.Id == orgUnit.Id)
            {
                continue;
            }

            changed |= await ReassignOrgUnitReferencesAsync(orgUnit.Id, replacement.Id, now);
        }

        foreach (var orgUnit in obsoleteOrgUnits.OrderByDescending(x => GetDepth(x, orgMap)))
        {
            _dbContext.OrgUnits.Remove(orgUnit);
            orgUnits.Remove(orgUnit);
            changed = true;
        }

        return changed;
    }

    private OrgUnit? FindMatchingOrgUnit(
        OrgUnitSeedDefinition definition,
        IReadOnlyCollection<OrgUnit> orgUnits,
        IReadOnlyDictionary<string, OrgUnit> targetMap,
        ISet<Guid> reusedIds)
    {
        var byCode = orgUnits.FirstOrDefault(x =>
            !reusedIds.Contains(x.Id) &&
            string.Equals(x.Code, definition.Code, StringComparison.OrdinalIgnoreCase));
        if (byCode != null)
        {
            return byCode;
        }

        if (definition.ParentKey == null)
        {
            return orgUnits.FirstOrDefault(x =>
                       !reusedIds.Contains(x.Id) &&
                       string.Equals(x.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                   ?? FindHeadquarter(orgUnits.Where(x => !reusedIds.Contains(x.Id)));
        }

        var parent = targetMap[definition.ParentKey];
        var byNameUnderParent = orgUnits.FirstOrDefault(x =>
            !reusedIds.Contains(x.Id) &&
            x.ParentId == parent.Id &&
            string.Equals(x.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
        if (byNameUnderParent != null)
        {
            return byNameUnderParent;
        }

        if (definition.Level == 1)
        {
            var branchKey = ExtractBranchKey(definition.Name);
            if (branchKey != null)
            {
                return orgUnits.FirstOrDefault(x =>
                    !reusedIds.Contains(x.Id) &&
                    x.ParentId == parent.Id &&
                    IsBranchMatch(x, branchKey));
            }
        }

        return null;
    }

    private static OrgUnit ResolveReplacementOrgUnit(
        OrgUnit orgUnit,
        IReadOnlyDictionary<Guid, OrgUnit> orgMap,
        IReadOnlyDictionary<string, OrgUnit> targetMap)
    {
        var branchKey = GetPreferredBranchKey(orgUnit, orgMap);
        if (branchKey != null)
        {
            var departmentKey = ExtractDepartmentKey(orgUnit.Name);
            if (departmentKey != null && targetMap.TryGetValue($"{branchKey}-{departmentKey}", out var department))
            {
                return department;
            }

            if (targetMap.TryGetValue(branchKey, out var branch))
            {
                return branch;
            }
        }

        return targetMap["hq"];
    }

    private async Task<bool> ReassignOrgUnitReferencesAsync(Guid sourceOrgUnitId, Guid targetOrgUnitId, DateTime now)
    {
        var changed = false;

        var employees = await _dbContext.Employees
            .Where(x => x.OrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var employee in employees)
        {
            employee.OrgUnitId = targetOrgUnitId;
            employee.UpdatedAt = now;
            changed = true;
        }

        var thirdPartyEmployees = await _dbContext.Employees
            .Where(x => x.ThirdPartyCompanyId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var employee in thirdPartyEmployees)
        {
            employee.ThirdPartyCompanyId = targetOrgUnitId;
            employee.UpdatedAt = now;
            changed = true;
        }

        var timesheets = await _dbContext.Timesheets
            .Where(x => x.ActualOrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var timesheet in timesheets)
        {
            timesheet.ActualOrgUnitId = targetOrgUnitId;
            changed = true;
        }

        var products = await _dbContext.Products
            .Where(x => x.OrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var product in products)
        {
            product.OrgUnitId = targetOrgUnitId;
            changed = true;
        }

        var outputRecords = await _dbContext.OutputRecords
            .Where(x => x.OrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var outputRecord in outputRecords)
        {
            outputRecord.OrgUnitId = targetOrgUnitId;
            changed = true;
        }

        var expenseApplications = await _dbContext.ExpenseApplications
            .Where(x => x.OrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var expenseApplication in expenseApplications)
        {
            expenseApplication.OrgUnitId = targetOrgUnitId;
            changed = true;
        }

        var configParams = await _dbContext.SysConfigParams
            .Where(x => x.OrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var configParam in configParams)
        {
            configParam.OrgUnitId = targetOrgUnitId;
            configParam.UpdatedAt = now;
            changed = true;
        }

        var roles = await _dbContext.SysRoles
            .Where(x => x.OrgUnitId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var role in roles)
        {
            role.OrgUnitId = targetOrgUnitId;
            role.UpdatedAt = now;
            changed = true;
        }

        var bills = await _dbContext.ThirdPartyBills
            .Where(x => x.ClientId == sourceOrgUnitId)
            .ToListAsync();
        foreach (var bill in bills)
        {
            bill.ClientId = targetOrgUnitId;
            changed = true;
        }

        return changed;
    }

    private static int GetDepth(OrgUnit orgUnit, IReadOnlyDictionary<Guid, OrgUnit> orgMap)
    {
        var depth = 0;
        var currentParentId = orgUnit.ParentId;
        var visited = new HashSet<Guid> { orgUnit.Id };

        while (currentParentId.HasValue && orgMap.TryGetValue(currentParentId.Value, out var parent))
        {
            depth++;
            if (!visited.Add(parent.Id))
            {
                break;
            }

            currentParentId = parent.ParentId;
        }

        return depth;
    }

    private static string? GetPreferredBranchKey(OrgUnit orgUnit, IReadOnlyDictionary<Guid, OrgUnit> orgMap)
    {
        var current = orgUnit;
        var visited = new HashSet<Guid>();

        while (visited.Add(current.Id))
        {
            var branchKey = ExtractBranchKey($"{current.Code} {current.Name}");
            if (branchKey != null)
            {
                return branchKey;
            }

            if (!current.ParentId.HasValue || !orgMap.TryGetValue(current.ParentId.Value, out current))
            {
                break;
            }
        }

        return null;
    }

    private static bool IsBranchMatch(OrgUnit orgUnit, string branchKey)
    {
        var text = $"{orgUnit.Code} {orgUnit.Name}";
        return string.Equals(ExtractBranchKey(text), branchKey, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractBranchKey(string text)
    {
        if (text.Contains("福清", StringComparison.OrdinalIgnoreCase) || text.Contains("FQ", StringComparison.OrdinalIgnoreCase))
        {
            return "fq";
        }

        if (text.Contains("青岛", StringComparison.OrdinalIgnoreCase) || text.Contains("QD", StringComparison.OrdinalIgnoreCase))
        {
            return "qd";
        }

        if (text.Contains("南京", StringComparison.OrdinalIgnoreCase) || text.Contains("NJ", StringComparison.OrdinalIgnoreCase))
        {
            return "nj";
        }

        if (text.Contains("重庆", StringComparison.OrdinalIgnoreCase) || text.Contains("CQ", StringComparison.OrdinalIgnoreCase))
        {
            return "cq";
        }

        return null;
    }

    private static string? ExtractDepartmentKey(string text)
    {
        if (text.Contains("人事", StringComparison.OrdinalIgnoreCase) || text.Contains("HR", StringComparison.OrdinalIgnoreCase))
        {
            return "hr";
        }

        if (text.Contains("销售", StringComparison.OrdinalIgnoreCase) || text.Contains("SALES", StringComparison.OrdinalIgnoreCase))
        {
            return "sales";
        }

        if (text.Contains("OBA", StringComparison.OrdinalIgnoreCase))
        {
            return "oba";
        }

        if (text.Contains("备料", StringComparison.OrdinalIgnoreCase) || text.Contains("PREP", StringComparison.OrdinalIgnoreCase))
        {
            return "prep";
        }

        if (text.Contains("维修", StringComparison.OrdinalIgnoreCase) || text.Contains("REPAIR", StringComparison.OrdinalIgnoreCase))
        {
            return "repair";
        }

        return null;
    }

    private static List<OrgUnitSeedDefinition> GetOrgUnitDefinitions()
    {
        return new List<OrgUnitSeedDefinition>
        {
            new("hq", "HQ", "总公司（合肥）", 0, null),
            new("fq", "BRANCH-FQ", "福清分公司", 1, "hq"),
            new("qd", "BRANCH-QD", "青岛分公司", 1, "hq"),
            new("nj", "BRANCH-NJ", "南京分公司", 1, "hq"),
            new("cq", "BRANCH-CQ", "重庆分公司", 1, "hq"),
            new("fq-hr", "FQ-HR", "人事部", 2, "fq"),
            new("fq-sales", "FQ-SALES", "销售部", 2, "fq"),
            new("fq-oba", "FQ-OBA", "OBA", 2, "fq"),
            new("fq-prep", "FQ-PREP", "备料", 2, "fq"),
            new("fq-repair", "FQ-REPAIR", "维修", 2, "fq"),
            new("qd-hr", "QD-HR", "人事部", 2, "qd"),
            new("qd-sales", "QD-SALES", "销售部", 2, "qd"),
            new("qd-oba", "QD-OBA", "OBA", 2, "qd"),
            new("qd-prep", "QD-PREP", "备料", 2, "qd"),
            new("qd-repair", "QD-REPAIR", "维修", 2, "qd"),
            new("nj-hr", "NJ-HR", "人事部", 2, "nj"),
            new("nj-sales", "NJ-SALES", "销售部", 2, "nj"),
            new("nj-oba", "NJ-OBA", "OBA", 2, "nj"),
            new("nj-prep", "NJ-PREP", "备料", 2, "nj"),
            new("nj-prep-line-1", "NJ-PREP-L1", "1线", 3, "nj-prep"),
            new("nj-prep-line-1-class-1", "NJ-PREP-L1-C1", "1班", 4, "nj-prep-line-1"),
            new("nj-prep-line-1-class-2", "NJ-PREP-L1-C2", "2班", 4, "nj-prep-line-1"),
            new("nj-prep-line-2", "NJ-PREP-L2", "2线", 3, "nj-prep"),
            new("nj-prep-line-2-class-1", "NJ-PREP-L2-C1", "1班", 4, "nj-prep-line-2"),
            new("nj-prep-line-2-class-2", "NJ-PREP-L2-C2", "2班", 4, "nj-prep-line-2"),
            new("nj-prep-line-3", "NJ-PREP-L3", "3线", 3, "nj-prep"),
            new("nj-prep-line-3-class-1", "NJ-PREP-L3-C1", "1班", 4, "nj-prep-line-3"),
            new("nj-prep-line-3-class-2", "NJ-PREP-L3-C2", "2班", 4, "nj-prep-line-3"),
            new("nj-prep-line-4", "NJ-PREP-L4", "4线", 3, "nj-prep"),
            new("nj-prep-line-4-class-1", "NJ-PREP-L4-C1", "1班", 4, "nj-prep-line-4"),
            new("nj-prep-line-4-class-2", "NJ-PREP-L4-C2", "2班", 4, "nj-prep-line-4"),
            new("nj-prep-line-5", "NJ-PREP-L5", "5线", 3, "nj-prep"),
            new("nj-prep-line-5-class-1", "NJ-PREP-L5-C1", "1班", 4, "nj-prep-line-5"),
            new("nj-prep-line-5-class-2", "NJ-PREP-L5-C2", "2班", 4, "nj-prep-line-5"),
            new("nj-prep-line-6", "NJ-PREP-L6", "6线", 3, "nj-prep"),
            new("nj-prep-line-6-class-1", "NJ-PREP-L6-C1", "1班", 4, "nj-prep-line-6"),
            new("nj-prep-line-6-class-2", "NJ-PREP-L6-C2", "2班", 4, "nj-prep-line-6"),
            new("nj-prep-line-7", "NJ-PREP-L7", "7线", 3, "nj-prep"),
            new("nj-prep-line-7-class-1", "NJ-PREP-L7-C1", "1班", 4, "nj-prep-line-7"),
            new("nj-prep-line-7-class-2", "NJ-PREP-L7-C2", "2班", 4, "nj-prep-line-7"),
            new("nj-repair", "NJ-REPAIR", "维修", 2, "nj"),
            new("cq-hr", "CQ-HR", "人事部", 2, "cq"),
            new("cq-sales", "CQ-SALES", "销售部", 2, "cq"),
            new("cq-oba", "CQ-OBA", "OBA", 2, "cq"),
            new("cq-prep", "CQ-PREP", "备料", 2, "cq"),
            new("cq-repair", "CQ-REPAIR", "维修", 2, "cq")
        };
    }

    private static bool NormalizeOrgLevelsByDepth(IReadOnlyCollection<OrgUnit> orgUnits)
    {
        var changed = false;
        var orgMap = orgUnits.ToDictionary(x => x.Id);

        foreach (var orgUnit in orgUnits)
        {
            var expectedLevel = CalculateOrgLevel(orgUnit, orgMap);
            if ((int)orgUnit.Level == expectedLevel)
            {
                continue;
            }

            orgUnit.Level = (OrgLevel)expectedLevel;
            orgUnit.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        return changed;
    }

    private static int CalculateOrgLevel(OrgUnit orgUnit, IReadOnlyDictionary<Guid, OrgUnit> orgMap)
    {
        if (IsSupplierOrgUnit(orgUnit))
        {
            return (int)OrgLevel.Supplier;
        }

        var level = 0;
        var visited = new HashSet<Guid> { orgUnit.Id };
        var currentParentId = orgUnit.ParentId;

        while (currentParentId.HasValue && orgMap.TryGetValue(currentParentId.Value, out var parent))
        {
            level++;
            if (!visited.Add(parent.Id))
            {
                break;
            }

            currentParentId = parent.ParentId;
        }

        return Math.Min(level, 4);
    }

    private static OrgUnit? FindHeadquarter(IEnumerable<OrgUnit> orgUnits)
    {
        return orgUnits
            .OrderBy(x => x.ParentId.HasValue ? 1 : 0)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefault(x =>
                !x.ParentId.HasValue ||
                x.Level == OrgLevel.Headquarters ||
                x.Name.Contains("总部", StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains("总公司", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSupplierOrgUnit(OrgUnit orgUnit)
    {
        return (int)orgUnit.Level >= 5 ||
               orgUnit.Name.Contains("供应商", StringComparison.OrdinalIgnoreCase) ||
               orgUnit.Code.Contains("SUPPLIER", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SeedMenusAsync()
    {
        var definitions = GetMenuDefinitions();
        var existingMenus = await _dbContext.SysMenus.ToListAsync();
        var menuMap = existingMenus.ToDictionary(x => x.MenuKey, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (!menuMap.TryGetValue(definition.MenuKey, out var menu))
            {
                menu = new SysMenu
                {
                    MenuKey = definition.MenuKey,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.SysMenus.Add(menu);
                menuMap[definition.MenuKey] = menu;
            }

            menu.MenuName = definition.MenuName;
            menu.MenuType = definition.MenuType;
            menu.SortOrder = definition.SortOrder;
            menu.RoutePath = definition.RoutePath;
            menu.ComponentPath = definition.ComponentPath;
            menu.Icon = definition.Icon;
            menu.IsVisible = definition.IsVisible;
            menu.IsActive = true;
            menu.PermissionCode = definition.PermissionCode;
            menu.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        foreach (var definition in definitions)
        {
            var menu = menuMap[definition.MenuKey];
            menu.ParentId = string.IsNullOrWhiteSpace(definition.ParentKey) ? null : menuMap[definition.ParentKey!].Id;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedAdminDataAsync()
    {
        var role = await _dbContext.SysRoles.FirstOrDefaultAsync(x => x.RoleCode == "ADMIN");
        if (role == null)
        {
            role = new SysRole
            {
                RoleCode = "ADMIN",
                RoleName = "系统管理员",
                Description = "拥有所有模块、页面和按钮权限",
                IsActive = true
            };
            _dbContext.SysRoles.Add(role);
            await _dbContext.SaveChangesAsync();
        }

        var post = await _dbContext.SysPosts.FirstOrDefaultAsync(x => x.PostCode == "ADMIN_POST");
        if (post == null)
        {
            post = new SysPost
            {
                PostCode = "ADMIN_POST",
                PostName = "系统管理岗",
                Description = "默认系统管理岗位",
                IsActive = true
            };
            _dbContext.SysPosts.Add(post);
            await _dbContext.SaveChangesAsync();
        }

        var adminUser = await _dbContext.SysUsers.FirstOrDefaultAsync(x => x.Username == "admin");
        if (adminUser == null)
        {
            adminUser = new SysUser
            {
                Username = "admin",
                Password = "123456",
                Name = "管理员",
                IsActive = true,
                IsAdmin = true,
                PostId = post.Id
            };
            _dbContext.SysUsers.Add(adminUser);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            adminUser.Name = "管理员";
            adminUser.Password = string.IsNullOrWhiteSpace(adminUser.Password) ? "123456" : adminUser.Password;
            adminUser.IsAdmin = true;
            adminUser.IsActive = true;
            adminUser.PostId = post.Id;
            adminUser.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        if (!await _dbContext.SysUserRoles.AnyAsync(x => x.UserId == adminUser.Id && x.RoleId == role.Id))
        {
            _dbContext.SysUserRoles.Add(new SysUserRole
            {
                UserId = adminUser.Id,
                RoleId = role.Id
            });
            await _dbContext.SaveChangesAsync();
        }

        var allMenus = await _dbContext.SysMenus.Where(x => x.IsActive).ToListAsync();

        var existingRolePermissions = await _dbContext.SysRolePermissions
            .Where(x => x.RoleId == role.Id)
            .ToListAsync();
        if (existingRolePermissions.Count > 0)
        {
            _dbContext.SysRolePermissions.RemoveRange(existingRolePermissions);
        }

        var existingPostPermissions = await _dbContext.SysPostPermissions
            .Where(x => x.PostId == post.Id)
            .ToListAsync();
        if (existingPostPermissions.Count > 0)
        {
            _dbContext.SysPostPermissions.RemoveRange(existingPostPermissions);
        }

        if (existingRolePermissions.Count > 0 || existingPostPermissions.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        _dbContext.SysRolePermissions.AddRange(allMenus.Select(menu => new SysRolePermission
        {
            RoleId = role.Id,
            MenuId = menu.Id,
            PermissionCode = menu.PermissionCode ?? menu.MenuKey,
            PermissionType = menu.MenuType
        }));

        _dbContext.SysPostPermissions.AddRange(allMenus.Select(menu => new SysPostPermission
        {
            PostId = post.Id,
            MenuId = menu.Id,
            PermissionCode = menu.PermissionCode ?? menu.MenuKey,
            PermissionType = menu.MenuType
        }));

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedDemoDirectoryDataAsync()
    {
        var orgUnits = await _dbContext.OrgUnits
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Code)
            .ToListAsync();
        if (orgUnits.Count == 0)
        {
            return;
        }

        var supplier = await EnsureDefaultSupplierAsync();
        if (supplier != null && orgUnits.All(x => x.Id != supplier.Id))
        {
            orgUnits.Add(supplier);
        }

        var employeeSeeds = BuildDemoEmployeeSeeds(orgUnits, supplier);
        if (employeeSeeds.Count == 0)
        {
            return;
        }

        var employeesByNo = await _dbContext.Employees
            .ToDictionaryAsync(x => x.EmployeeNo, StringComparer.OrdinalIgnoreCase);
        var seededEmployees = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var seed in employeeSeeds)
        {
            if (!employeesByNo.TryGetValue(seed.EmployeeNo, out var employee))
            {
                employee = new Employee
                {
                    EmployeeNo = seed.EmployeeNo,
                    CreatedAt = now
                };
                _dbContext.Employees.Add(employee);
                employeesByNo[seed.EmployeeNo] = employee;
            }

            employee.Name = seed.Name;
            employee.Gender = seed.Gender;
            employee.IdCard = seed.IdCard;
            employee.Phone = seed.Phone;
            employee.Email = seed.Email;
            employee.EmployeeType = seed.EmployeeType;
            employee.SalaryMode = seed.SalaryMode;
            employee.OrgUnitId = seed.OrgUnitId;
            employee.ThirdPartyCompanyId = seed.ThirdPartyCompanyId;
            employee.JobTitle = seed.JobTitle;
            employee.Level = seed.Level;
            employee.Tags = seed.Tags.ToList();
            employee.HourlyRate = seed.HourlyRate;
            employee.MonthlySalary = seed.MonthlySalary;
            employee.ContractType = 0;
            employee.ProbationDays = 30;
            employee.TrialDaysRemaining = 30;
            employee.HireDate ??= now.Date;
            employee.IsActive = true;
            employee.UpdatedAt = now;

            seededEmployees[seed.Key] = employee;
        }

        await _dbContext.SaveChangesAsync();

        var managerOrgUnits = orgUnits
            .Where(x => x.Level is OrgLevel.Headquarters or OrgLevel.Branch or OrgLevel.Department)
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var managerChanged = false;
        foreach (var seed in employeeSeeds.Where(x => !string.IsNullOrWhiteSpace(x.ManagesOrgCode)))
        {
            if (!managerOrgUnits.TryGetValue(seed.ManagesOrgCode!, out var orgUnit))
            {
                continue;
            }

            var managerId = seededEmployees[seed.Key].Id.ToString();
            if (string.Equals(orgUnit.ManagerId, managerId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            orgUnit.ManagerId = managerId;
            orgUnit.UpdatedAt = now;
            managerChanged = true;
        }

        if (managerChanged)
        {
            await _dbContext.SaveChangesAsync();
        }

        await SeedWorkflowRolesAndUsersAsync(employeeSeeds, seededEmployees);
    }

    private async Task<OrgUnit?> EnsureDefaultSupplierAsync()
    {
        var supplier = await _dbContext.OrgUnits.FirstOrDefaultAsync(x => x.Code == "SUPPLIER-DEFAULT");
        if (supplier != null)
        {
            supplier.Name = "示例外包服务商";
            supplier.Level = OrgLevel.Supplier;
            supplier.IsActive = true;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return supplier;
        }

        supplier = new OrgUnit
        {
            Code = "SUPPLIER-DEFAULT",
            Name = "示例外包服务商",
            Level = OrgLevel.Supplier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OrgUnits.Add(supplier);
        await _dbContext.SaveChangesAsync();
        return supplier;
    }

    private async Task SeedWorkflowRolesAndUsersAsync(
        IReadOnlyList<DemoEmployeeSeed> employeeSeeds,
        IReadOnlyDictionary<string, Employee> seededEmployees)
    {
        var roleDefinitions = new[]
        {
            new WorkflowRoleSeed("dept_leader", "部门主管", "流程审批中的部门主管角色"),
            new WorkflowRoleSeed("branch_manager", "分公司经理", "流程审批中的分公司经理角色"),
            new WorkflowRoleSeed("ceo", "总经理", "流程审批中的总经理角色"),
            new WorkflowRoleSeed("finance", "财务", "流程审批中的财务角色"),
            new WorkflowRoleSeed("hr", "人事", "流程审批中的人事角色")
        };

        var rolesByCode = await _dbContext.SysRoles
            .ToDictionaryAsync(x => x.RoleCode, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var definition in roleDefinitions)
        {
            if (!rolesByCode.TryGetValue(definition.RoleCode, out var role))
            {
                role = new SysRole
                {
                    RoleCode = definition.RoleCode,
                    CreatedAt = now
                };
                _dbContext.SysRoles.Add(role);
                rolesByCode[definition.RoleCode] = role;
            }

            role.RoleName = definition.RoleName;
            role.Description = definition.Description;
            role.IsActive = true;
            role.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();

        var usersByUsername = await _dbContext.SysUsers
            .ToDictionaryAsync(x => x.Username, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in employeeSeeds.Where(x => x.RoleCodes.Count > 0))
        {
            if (!usersByUsername.TryGetValue(seed.Username, out var user))
            {
                user = new SysUser
                {
                    Username = seed.Username,
                    CreatedAt = now
                };
                _dbContext.SysUsers.Add(user);
                usersByUsername[seed.Username] = user;
            }

            var employee = seededEmployees[seed.Key];
            user.Password = "123456";
            user.Name = seed.Name;
            user.Phone = seed.Phone;
            user.Email = seed.Email;
            user.EmployeeId = employee.Id;
            user.IsActive = true;
            user.IsAdmin = false;
            user.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();

        var seedUsers = employeeSeeds
            .Where(x => x.RoleCodes.Count > 0)
            .ToDictionary(x => x.Username, x => x, StringComparer.OrdinalIgnoreCase);
        var userIds = usersByUsername
            .Where(x => seedUsers.ContainsKey(x.Key))
            .Select(x => x.Value.Id)
            .ToList();

        var existingLinks = await _dbContext.SysUserRoles
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync();
        if (existingLinks.Count > 0)
        {
            _dbContext.SysUserRoles.RemoveRange(existingLinks);
            await _dbContext.SaveChangesAsync();
        }

        var userRoles = new List<SysUserRole>();
        foreach (var seed in employeeSeeds.Where(x => x.RoleCodes.Count > 0))
        {
            var user = usersByUsername[seed.Username];
            foreach (var roleCode in seed.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!rolesByCode.TryGetValue(roleCode, out var role))
                {
                    continue;
                }

                userRoles.Add(new SysUserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }
        }

        if (userRoles.Count > 0)
        {
            _dbContext.SysUserRoles.AddRange(userRoles);
            await _dbContext.SaveChangesAsync();
        }
    }

    private static List<DemoEmployeeSeed> BuildDemoEmployeeSeeds(
        IReadOnlyCollection<OrgUnit> orgUnits,
        OrgUnit? supplier)
    {
        var orgByCode = orgUnits.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var seeds = new List<DemoEmployeeSeed>();
        var index = 0;

        if (orgByCode.TryGetValue("HQ", out var headquarter))
        {
            seeds.Add(CreateDemoEmployeeSeed(
                key: "hq-ceo",
                employeeNo: "10000",
                name: "集团总经理",
                orgUnitId: headquarter.Id,
                jobTitle: "总经理",
                level: "M3",
                managesOrgCode: "HQ",
                roleCodes: new[] { "ceo" },
                index: index++,
                monthlySalary: 32000m));

            seeds.Add(CreateDemoEmployeeSeed(
                key: "hq-finance",
                employeeNo: "10001",
                name: "财务负责人",
                orgUnitId: headquarter.Id,
                jobTitle: "财务经理",
                level: "M2",
                roleCodes: new[] { "finance" },
                index: index++,
                monthlySalary: 18000m));
        }

        var branchUnits = orgUnits
            .Where(x => x.Level == OrgLevel.Branch)
            .OrderBy(x => x.Code)
            .ToList();
        foreach (var branch in branchUnits)
        {
            seeds.Add(CreateDemoEmployeeSeed(
                key: $"manager-{branch.Code}",
                employeeNo: $"11{index:000}",
                name: $"{GetOrgDisplayName(branch)}经理",
                orgUnitId: branch.Id,
                jobTitle: "分公司经理",
                level: "M2",
                managesOrgCode: branch.Code,
                roleCodes: new[] { "branch_manager" },
                index: index++,
                monthlySalary: 22000m));
        }

        var departmentUnits = orgUnits
            .Where(x => x.Level == OrgLevel.Department)
            .OrderBy(x => x.Code)
            .ToList();
        foreach (var department in departmentUnits)
        {
            var roleCodes = new List<string> { "dept_leader" };
            var jobTitle = "部门主管";
            if (department.Name.Contains("人事", StringComparison.OrdinalIgnoreCase))
            {
                roleCodes.Add("hr");
                jobTitle = "人事负责人";
            }

            seeds.Add(CreateDemoEmployeeSeed(
                key: $"manager-{department.Code}",
                employeeNo: $"12{index:000}",
                name: $"{GetOrgDisplayName(department)}负责人",
                orgUnitId: department.Id,
                jobTitle: jobTitle,
                level: "M1",
                managesOrgCode: department.Code,
                roleCodes: roleCodes,
                index: index++,
                monthlySalary: 15000m));
        }

        if (supplier != null)
        {
            seeds.Add(CreateDemoEmployeeSeed(
                key: "supplier-worker",
                employeeNo: "20001",
                name: "外包员工示例",
                orgUnitId: headquarter?.Id ?? supplier.Id,
                jobTitle: "外包操作员",
                level: "P1",
                roleCodes: Array.Empty<string>(),
                index: index++,
                monthlySalary: null,
                hourlyRate: 30m,
                employeeType: EmployeeType.ThirdParty,
                thirdPartyCompanyId: supplier.Id,
                tags: new[] { "外包", "示例数据" }));
        }

        return seeds;
    }

    private static DemoEmployeeSeed CreateDemoEmployeeSeed(
        string key,
        string employeeNo,
        string name,
        Guid orgUnitId,
        string jobTitle,
        string level,
        IEnumerable<string> roleCodes,
        int index,
        decimal? monthlySalary,
        string? managesOrgCode = null,
        decimal? hourlyRate = null,
        EmployeeType employeeType = EmployeeType.Internal,
        Guid? thirdPartyCompanyId = null,
        IEnumerable<string>? tags = null)
    {
        var safeTags = (tags ?? new[] { "示例数据" }).ToList();
        if (!safeTags.Contains("示例数据", StringComparer.OrdinalIgnoreCase))
        {
            safeTags.Add("示例数据");
        }

        if (!safeTags.Contains("负责人", StringComparer.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(managesOrgCode))
        {
            safeTags.Add("负责人");
        }

        return new DemoEmployeeSeed(
            key,
            employeeNo,
            $"user{employeeNo}",
            name,
            CreatePhone(index),
            CreateEmail(employeeNo),
            CreateIdCard(index),
            0,
            employeeType,
            monthlySalary.HasValue ? SalaryMode.Fixed : SalaryMode.Hourly,
            orgUnitId,
            thirdPartyCompanyId,
            jobTitle,
            level,
            safeTags,
            hourlyRate,
            monthlySalary,
            managesOrgCode,
            roleCodes.ToList());
    }

    private static string GetOrgDisplayName(OrgUnit orgUnit)
    {
        return orgUnit.Name
            .Replace("分公司", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("总公司", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("（合肥）", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string CreatePhone(int index)
    {
        return $"138{(10000000 + index).ToString()[..8]}";
    }

    private static string CreateEmail(string employeeNo)
    {
        return $"seed{employeeNo}@example.com";
    }

    private static string CreateIdCard(int index)
    {
        var month = (index % 12) + 1;
        var day = (index % 28) + 1;
        return $"1101051990{month:00}{day:00}{index:0000}";
    }

    private async Task SeedSystemSettingsAsync()
    {
        var captchaSetting = await _dbContext.SysConfigParams
            .FirstOrDefaultAsync(x => x.Category == "security" && x.ParamKey == "loginCaptchaEnabled");

        if (captchaSetting != null)
        {
            return;
        }

        _dbContext.SysConfigParams.Add(new SysConfigParam
        {
            Category = "security",
            ParamKey = "loginCaptchaEnabled",
            ParamValue = "false",
            Description = "是否启用登录验证码",
            IsGlobal = true,
            IsActive = true,
            TakeEffectImmediately = true
        });

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedCustomersAsync()
    {
        if (await _dbContext.Customers.AnyAsync())
        {
            return;
        }

        _dbContext.Customers.AddRange(
            new Customer
            {
                Code = "CUS-001",
                Name = "南京示例客户",
                ShortName = "南京客户",
                ContactPerson = "张经理",
                Phone = "13810000001",
                Email = "customer01@example.com",
                TaxNo = "91320100CUS000001",
                Address = "南京市江宁区示例路 1 号",
                InvoiceTitle = "南京示例客户有限公司",
                Remark = "默认客户数据"
            },
            new Customer
            {
                Code = "CUS-002",
                Name = "福清示例客户",
                ShortName = "福清客户",
                ContactPerson = "李经理",
                Phone = "13810000002",
                Email = "customer02@example.com",
                TaxNo = "91350100CUS000002",
                Address = "福清市示例大道 8 号",
                InvoiceTitle = "福清示例客户有限公司",
                Remark = "默认客户数据"
            });

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedSuppliersAsync()
    {
        var supplierOrgUnits = await _dbContext.OrgUnits
            .Where(x => x.Level == OrgLevel.Supplier)
            .OrderBy(x => x.Code)
            .ToListAsync();

        foreach (var orgUnit in supplierOrgUnits)
        {
            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == orgUnit.Id);
            if (supplier == null)
            {
                supplier = new Supplier
                {
                    Id = orgUnit.Id,
                    CreatedAt = orgUnit.CreatedAt
                };
                _dbContext.Suppliers.Add(supplier);
            }

            supplier.Code = orgUnit.Code;
            supplier.Name = orgUnit.Name;
            supplier.ContactPerson ??= "供应商联系人";
            supplier.Phone ??= "13819990000";
            supplier.Email ??= $"{orgUnit.Code.ToLowerInvariant()}@example.com";
            supplier.TaxNo ??= $"TAX-{orgUnit.Code}";
            supplier.Address ??= "供应商默认地址";
            supplier.BankName ??= "中国银行";
            supplier.BankAccount ??= "6222000000000000000";
            supplier.PaymentTermDays = supplier.PaymentTermDays <= 0 ? 30 : supplier.PaymentTermDays;
            supplier.Remark ??= "默认供应商数据";
            supplier.IsActive = orgUnit.IsActive;
            supplier.UpdatedAt = DateTime.UtcNow;
        }

        if (supplierOrgUnits.Count == 0 && !await _dbContext.Suppliers.AnyAsync())
        {
            var supplierId = Guid.NewGuid();
            _dbContext.Suppliers.Add(new Supplier
            {
                Id = supplierId,
                Code = "SUP-001",
                Name = "示例外包服务商",
                ContactPerson = "王对接",
                Phone = "13818880001",
                Email = "supplier01@example.com",
                TaxNo = "91320000SUP000001",
                Address = "供应商示例地址",
                BankName = "中国建设银行",
                BankAccount = "6227000000000000000",
                PaymentTermDays = 30,
                Remark = "默认供应商数据"
            });

            _dbContext.OrgUnits.Add(new OrgUnit
            {
                Id = supplierId,
                Code = "SUP-001",
                Name = "示例外包服务商",
                Level = OrgLevel.Supplier,
                IsActive = true
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private static List<MenuSeedDefinition> GetMenuDefinitions()
    {
        return new List<MenuSeedDefinition>
        {
            new("dashboard", null, "首页", "page", 1, "/dashboard", "views/Dashboard.vue", "HomeOutlined", true, "page.dashboard"),

            new("org-module", null, "组织架构", "module", 10, null, null, "ClusterOutlined", true, "module.org"),
            new("org-list", "org-module", "组织单元", "page", 11, "/org-units", "views/org/OrgUnitList.vue", null, true, "page.org.list"),
            new("org-management", "org-module", "架构管理", "page", 12, "/org-units/management", "views/org/OrgUnitManagement.vue", null, true, "page.org.management"),
            new("org-chart", "org-module", "组织图", "page", 13, "/org-units/management/chart", "views/org/OrgChartView.vue", null, true, "page.org.chart"),
            new("org-create", "org-list", "新增组织", "button", 111, null, null, null, false, "button.org.create"),
            new("org-edit", "org-list", "编辑组织", "button", 112, null, null, null, false, "button.org.edit"),
            new("org-delete", "org-list", "删除组织", "button", 113, null, null, null, false, "button.org.delete"),
            new("org-import", "org-list", "导入组织", "button", 114, null, null, null, false, "button.org.import"),
            new("org-export", "org-list", "导出组织", "button", 115, null, null, null, false, "button.org.export"),
            new("org-enable", "org-list", "启用组织", "button", 116, null, null, null, false, "button.org.enable"),
            new("org-disable", "org-list", "停用组织", "button", 117, null, null, null, false, "button.org.disable"),

            new("employee-module", null, "员工管理", "module", 20, null, null, "TeamOutlined", true, "module.employee"),
            new("employee-list", "employee-module", "员工列表", "page", 21, "/employees", "views/employee/EmployeeList.vue", null, true, "page.employee.list"),
            new("employee-create", "employee-list", "新增员工", "button", 211, null, null, null, false, "button.employee.create"),
            new("employee-edit", "employee-list", "编辑员工", "button", 212, null, null, null, false, "button.employee.edit"),
            new("employee-delete", "employee-list", "删除员工", "button", 213, null, null, null, false, "button.employee.delete"),
            new("employee-import", "employee-list", "导入员工", "button", 214, null, null, null, false, "button.employee.import"),
            new("employee-export", "employee-list", "导出员工", "button", 215, null, null, null, false, "button.employee.export"),
            new("employee-dismiss", "employee-list", "办理离职", "button", 216, null, null, null, false, "button.employee.dismiss"),
            new("employee-reset-password", "employee-list", "重置密码", "button", 217, null, null, null, false, "button.employee.resetPassword"),

            new("partner-module", null, "客户供应商", "module", 25, null, null, "TeamOutlined", true, "module.partner"),
            new("partner-customer-page", "partner-module", "客户列表", "page", 251, "/partners/customers", "views/partner/CustomerList.vue", null, true, "page.partner.customer"),
            new("partner-customer-create", "partner-customer-page", "新增客户", "button", 2511, null, null, null, false, "button.partner.customer.create"),
            new("partner-customer-edit", "partner-customer-page", "编辑客户", "button", 2512, null, null, null, false, "button.partner.customer.edit"),
            new("partner-customer-delete", "partner-customer-page", "删除客户", "button", 2513, null, null, null, false, "button.partner.customer.delete"),
            new("partner-supplier-page", "partner-module", "供应商列表", "page", 252, "/partners/suppliers", "views/partner/SupplierList.vue", null, true, "page.partner.supplier"),
            new("partner-supplier-create", "partner-supplier-page", "新增供应商", "button", 2521, null, null, null, false, "button.partner.supplier.create"),
            new("partner-supplier-edit", "partner-supplier-page", "编辑供应商", "button", 2522, null, null, null, false, "button.partner.supplier.edit"),
            new("partner-supplier-delete", "partner-supplier-page", "删除供应商", "button", 2523, null, null, null, false, "button.partner.supplier.delete"),

            new("timesheet-module", null, "考勤工时", "module", 30, null, null, "ClockCircleOutlined", true, "module.timesheet"),
            new("timesheet-list", "timesheet-module", "工时记录", "page", 31, "/timesheets", "views/timesheet/TimesheetList.vue", null, true, "page.timesheet.list"),
            new("timesheet-import-page", "timesheet-module", "工时导入", "page", 32, "/timesheets/import", "views/timesheet/TimesheetImport.vue", null, true, "page.timesheet.import"),
            new("timesheet-create", "timesheet-list", "新增工时", "button", 311, null, null, null, false, "button.timesheet.create"),
            new("timesheet-edit", "timesheet-list", "编辑工时", "button", 312, null, null, null, false, "button.timesheet.edit"),
            new("timesheet-submit", "timesheet-list", "提交工时", "button", 313, null, null, null, false, "button.timesheet.submit"),
            new("timesheet-approve", "timesheet-list", "审批工时", "button", 314, null, null, null, false, "button.timesheet.approve"),
            new("timesheet-delete", "timesheet-list", "删除工时", "button", 315, null, null, null, false, "button.timesheet.delete"),
            new("timesheet-import", "timesheet-import-page", "导入工时", "button", 321, null, null, null, false, "button.timesheet.import"),
            new("timesheet-export", "timesheet-list", "导出工时", "button", 322, null, null, null, false, "button.timesheet.export"),

            new("salary-module", null, "薪资管理", "module", 40, null, null, "MoneyCollectOutlined", true, "module.salary"),
            new("salary-list", "salary-module", "薪资列表", "page", 41, "/salary", "views/salary/SalaryList.vue", null, true, "page.salary.list"),
            new("salary-calculate-page", "salary-module", "薪资核算", "page", 42, "/salary/calculate", "views/salary/SalaryCalculate.vue", null, true, "page.salary.calculate"),
            new("salary-calculate", "salary-calculate-page", "执行核算", "button", 421, null, null, null, false, "button.salary.calculate"),
            new("salary-approve", "salary-list", "审批薪资", "button", 411, null, null, null, false, "button.salary.approve"),
            new("salary-reject", "salary-list", "驳回薪资", "button", 412, null, null, null, false, "button.salary.reject"),
            new("salary-export", "salary-list", "导出薪资", "button", 413, null, null, null, false, "button.salary.export"),
            new("salary-payslip", "salary-list", "生成工资单", "button", 414, null, null, null, false, "button.salary.generatePayslip"),
            new("salary-bank", "salary-list", "银行代发", "button", 415, null, null, null, false, "button.salary.bankExport"),
            new("salary-adjust", "salary-list", "调整薪资", "button", 416, null, null, null, false, "button.salary.adjust"),

            new("workflow-module", null, "协同审批", "module", 45, null, null, "FileTextOutlined", true, "module.workflow"),
            new("todo-center-page", "workflow-module", "待办中心", "page", 451, "/workflow/todo", "views/workflow/TodoCenter.vue", null, true, "page.todo.center"),
            new("process-center-page", "workflow-module", "流程中心", "page", 452, "/workflow/processes", "views/workflow/ProcessCenter.vue", null, true, "page.process.center"),
            new("process-designer-page", "workflow-module", "流程设计器", "page", 453, "/workflow/designer", "views/workflow/ProcessDesigner.vue", null, true, "page.process.designer"),
            new("process-requests-page", "workflow-module", "流程申请", "page", 454, "/workflow/requests", "views/workflow/ProcessRequests.vue", null, true, "page.process.requests"),
            new("todo-complete", "todo-center-page", "完成待办", "button", 4511, null, null, null, false, "button.todo.complete"),
            new("todo-reject", "todo-center-page", "驳回待办", "button", 4512, null, null, null, false, "button.todo.reject"),
            new("todo-urge", "todo-center-page", "催办待办", "button", 4513, null, null, null, false, "button.todo.urge"),
            new("todo-transfer", "todo-center-page", "转交待办", "button", 4514, null, null, null, false, "button.todo.transfer"),
            new("todo-batch-complete", "todo-center-page", "批量完成待办", "button", 4515, null, null, null, false, "button.todo.batchComplete"),
            new("todo-batch-reject", "todo-center-page", "批量驳回待办", "button", 4516, null, null, null, false, "button.todo.batchReject"),
            new("todo-agent", "todo-center-page", "代理设置", "button", 4517, null, null, null, false, "button.todo.agent"),
            new("process-reject", "process-center-page", "驳回流程", "button", 4520, null, null, null, false, "button.process.reject"),
            new("process-terminate", "process-center-page", "终止流程", "button", 4521, null, null, null, false, "button.process.terminate"),
            new("process-rebuild-todo", "process-center-page", "重建待办", "button", 4522, null, null, null, false, "button.process.rebuildTodo"),

            new("reports", null, "报表中心", "page", 50, "/reports", "views/reports/Reports.vue", "FileTextOutlined", true, "page.reports"),

            new("settings-module", null, "系统设置", "module", 60, null, null, "SettingOutlined", true, "module.settings"),
            new("settings-page", "settings-module", "系统配置", "page", 61, "/settings", "views/settings/Settings.vue", null, true, "page.settings"),
            new("config-page", "settings-module", "配置管理", "page", 62, "/config", "views/config/ConfigManagement.vue", null, true, "page.config"),

            new("access-module", null, "权限中心", "module", 70, null, null, "SafetyCertificateOutlined", true, "module.access"),
            new("access-role-page", "access-module", "角色管理", "page", 71, "/access/roles", "views/access/RoleManagement.vue", null, true, "page.access.role"),
            new("access-post-page", "access-module", "岗位管理", "page", 72, "/access/posts", "views/access/PostManagement.vue", null, true, "page.access.post"),
            new("access-user-page", "access-module", "用户管理", "page", 73, "/access/users", "views/access/UserManagement.vue", null, true, "page.access.user"),
            new("access-menu-page", "access-module", "权限菜单", "page", 74, "/access/menus", "views/access/MenuManagement.vue", null, true, "page.access.menu"),
            new("access-role-create", "access-role-page", "新增角色", "button", 711, null, null, null, false, "button.access.role.create"),
            new("access-role-edit", "access-role-page", "编辑角色", "button", 712, null, null, null, false, "button.access.role.edit"),
            new("access-role-delete", "access-role-page", "删除角色", "button", 713, null, null, null, false, "button.access.role.delete"),
            new("access-role-assign", "access-role-page", "分配角色权限", "button", 714, null, null, null, false, "button.access.role.assign"),
            new("access-post-create", "access-post-page", "新增岗位", "button", 721, null, null, null, false, "button.access.post.create"),
            new("access-post-edit", "access-post-page", "编辑岗位", "button", 722, null, null, null, false, "button.access.post.edit"),
            new("access-post-delete", "access-post-page", "删除岗位", "button", 723, null, null, null, false, "button.access.post.delete"),
            new("access-post-assign", "access-post-page", "分配岗位权限", "button", 724, null, null, null, false, "button.access.post.assign"),
            new("access-user-create", "access-user-page", "新增用户", "button", 731, null, null, null, false, "button.access.user.create"),
            new("access-user-edit", "access-user-page", "编辑用户", "button", 732, null, null, null, false, "button.access.user.edit"),
            new("access-user-delete", "access-user-page", "删除用户", "button", 733, null, null, null, false, "button.access.user.delete"),
            new("access-menu-create", "access-menu-page", "新增菜单", "button", 741, null, null, null, false, "button.access.menu.create"),
            new("access-menu-edit", "access-menu-page", "编辑菜单", "button", 742, null, null, null, false, "button.access.menu.edit"),
            new("access-menu-delete", "access-menu-page", "删除菜单", "button", 743, null, null, null, false, "button.access.menu.delete")
        };
    }

    private sealed record MenuSeedDefinition(
        string MenuKey,
        string? ParentKey,
        string MenuName,
        string MenuType,
        int SortOrder,
        string? RoutePath,
        string? ComponentPath,
        string? Icon,
        bool IsVisible,
        string PermissionCode);

    private sealed record OrgUnitSeedDefinition(
        string Key,
        string Code,
        string Name,
        int Level,
        string? ParentKey);

    private sealed record WorkflowRoleSeed(
        string RoleCode,
        string RoleName,
        string Description);

    private sealed record DemoEmployeeSeed(
        string Key,
        string EmployeeNo,
        string Username,
        string Name,
        string Phone,
        string Email,
        string IdCard,
        int Gender,
        EmployeeType EmployeeType,
        SalaryMode SalaryMode,
        Guid OrgUnitId,
        Guid? ThirdPartyCompanyId,
        string JobTitle,
        string Level,
        IReadOnlyList<string> Tags,
        decimal? HourlyRate,
        decimal? MonthlySalary,
        string? ManagesOrgCode,
        IReadOnlyList<string> RoleCodes);
}
