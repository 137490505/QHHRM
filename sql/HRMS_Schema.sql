-- HRMS Database Schema
-- Version: 3.0
-- Date: 2026-05-22
-- Compatible with: MySQL 8.0+ / MySQL 9.x

-- Drop database if exists (for development)
-- DROP DATABASE IF EXISTS HRMS;

CREATE DATABASE IF NOT EXISTS HRMS DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE HRMS;

-- Organization Unit Table
CREATE TABLE org_unit (
    Id CHAR(36) PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    Level INT NOT NULL COMMENT '0:总部 1:项目部 2:生产线 3:班组',
    ParentId CHAR(36) NULL,
    ManagerId VARCHAR(100) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (ParentId) REFERENCES org_unit(Id) ON DELETE SET NULL,
    INDEX idx_code (Code),
    INDEX idx_parent (ParentId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Employee Table
CREATE TABLE employee (
    Id CHAR(36) PRIMARY KEY,
    EmployeeNo VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    Gender INT NOT NULL DEFAULT 0,
    IdCard VARCHAR(18) NULL,
    Phone VARCHAR(20) NULL,
    Email VARCHAR(100) NULL,
    EmployeeType INT NOT NULL COMMENT '0:自主员工 1:第三方派遣',
    SalaryMode INT NOT NULL COMMENT '0:时薪制 1:固薪制 2:计件制 3:混合制',
    OrgUnitId CHAR(36) NOT NULL,
    ThirdPartyCompanyId CHAR(36) NULL,
    JobTitle VARCHAR(100) NULL,
    `Level` VARCHAR(20) NULL COMMENT 'M1-M5, P1-P8',
    Tags JSON NULL COMMENT '动态标签数组',
    HourlyRate DECIMAL(10,2) NULL,
    MonthlySalary DECIMAL(12,2) NULL,
    PieceRatePrice DECIMAL(10,2) NULL,
    SocialSecurityBase DECIMAL(12,2) NULL,
    HousingFundBase DECIMAL(12,2) NULL,
    TrialEndDate DATE NULL,
    TrialDaysRemaining INT NOT NULL DEFAULT 3,
    ProbationDays INT NOT NULL DEFAULT 30,
    ContractType INT NOT NULL DEFAULT 0,
    ContractStartDate DATE NULL,
    ContractEndDate DATE NULL,
    HireDate DATE NULL,
    IsBlacklisted TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    DismissDate DATETIME NULL,
    DismissReason VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id),
    FOREIGN KEY (ThirdPartyCompanyId) REFERENCES org_unit(Id),
    INDEX idx_employee_no (EmployeeNo),
    INDEX idx_org_unit (OrgUnitId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Timesheet Table
CREATE TABLE timesheet (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    Date DATE NOT NULL,
    ActualOrgUnitId CHAR(36) NOT NULL COMMENT '实际工作组织（支持借调）',
    WorkingHours DECIMAL(5,2) NOT NULL,
    OvertimeHours DECIMAL(5,2) NOT NULL DEFAULT 0,
    ShiftType INT NOT NULL DEFAULT 0 COMMENT '0:工作日 1:休息日 2:节假日',
    Remark VARCHAR(500) NULL,
    ApprovalStatus INT NOT NULL DEFAULT 0 COMMENT '0:待审批 1:已通过 2:已拒绝',
    ApproverId VARCHAR(100) NULL,
    ApprovedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    FOREIGN KEY (ActualOrgUnitId) REFERENCES org_unit(Id),
    INDEX idx_employee_date (EmployeeId, Date),
    INDEX idx_org_date (ActualOrgUnitId, Date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Salary Calculation Table
CREATE TABLE salary_calculation (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    RegularHours DECIMAL(8,2) NOT NULL DEFAULT 0,
    OvertimeHours DECIMAL(8,2) NOT NULL DEFAULT 0,
    RegularWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    OvertimeWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    MealAllowance DECIMAL(10,2) NOT NULL DEFAULT 0,
    Benefits DECIMAL(10,2) NOT NULL DEFAULT 0,
    PerformanceBonus DECIMAL(10,2) NOT NULL DEFAULT 0,
    AbsenceDeduction DECIMAL(10,2) NOT NULL DEFAULT 0,
    OtherDeductions DECIMAL(10,2) NOT NULL DEFAULT 0,
    GrossWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    SocialSecurityPersonal DECIMAL(10,2) NOT NULL DEFAULT 0,
    HousingFundPersonal DECIMAL(10,2) NOT NULL DEFAULT 0,
    IncomeTax DECIMAL(10,2) NOT NULL DEFAULT 0,
    TotalDeductions DECIMAL(12,2) NOT NULL DEFAULT 0,
    NetWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    CalculatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    UNIQUE KEY uk_employee_month (EmployeeId, Year, Month),
    INDEX idx_year_month (Year, Month)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Payslip Table
CREATE TABLE payslip (
    Id CHAR(36) PRIMARY KEY,
    SalaryCalculationId CHAR(36) NOT NULL,
    EmployeeId CHAR(36) NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    PdfUrl VARCHAR(500) NULL,
    IsSigned TINYINT(1) NOT NULL DEFAULT 0,
    SignedAt DATETIME NULL,
    HasComplaint TINYINT(1) NOT NULL DEFAULT 0,
    ComplaintReason VARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SalaryCalculationId) REFERENCES salary_calculation(Id),
    FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    INDEX idx_employee_month (EmployeeId, Year, Month)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Product Table
CREATE TABLE product (
    Id CHAR(36) PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    Unit VARCHAR(20) NULL,
    ClientUnitPrice DECIMAL(10,2) NOT NULL,
    ClientPriceEffectiveFrom DATETIME NULL,
    ClientPriceEffectiveTo DATETIME NULL,
    EmployeeUnitPrice DECIMAL(10,2) NOT NULL,
    EmployeePriceEffectiveFrom DATETIME NULL,
    EmployeePriceEffectiveTo DATETIME NULL,
    OrgUnitId CHAR(36) NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id),
    INDEX idx_code (Code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Output Record Table
CREATE TABLE output_record (
    Id CHAR(36) PRIMARY KEY,
    ProductId CHAR(36) NOT NULL,
    Date DATE NOT NULL,
    OrgUnitId CHAR(36) NOT NULL,
    QualifiedQuantity DECIMAL(10,2) NOT NULL,
    IsConfirmedByClient TINYINT(1) NOT NULL DEFAULT 0,
    ConfirmedAt DATETIME NULL,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (ProductId) REFERENCES product(Id),
    FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id),
    INDEX idx_date_org (Date, OrgUnitId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Third Party Bill Table
CREATE TABLE third_party_bill (
    Id CHAR(36) PRIMARY KEY,
    BillNo VARCHAR(50) NOT NULL UNIQUE,
    ClientId CHAR(36) NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    TotalWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    ManagementFee DECIMAL(12,2) NOT NULL DEFAULT 0,
    ManagementFeeRate DECIMAL(10,4) NOT NULL DEFAULT 0,
    ManagementFeeType VARCHAR(50) NOT NULL DEFAULT 'Fixed' COMMENT 'Fixed/PercentOfWages/PercentOfHours',
    OtherFees DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    Status INT NOT NULL DEFAULT 0 COMMENT '0:草稿 1:已确认 2:已开票 3:已收款',
    ConfirmedAt DATETIME NULL,
    InvoiceNo VARCHAR(100) NULL,
    InvoicedAt DATETIME NULL,
    PaidAt DATETIME NULL,
    Remark VARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (ClientId) REFERENCES org_unit(Id),
    INDEX idx_bill_no (BillNo),
    INDEX idx_client_period (ClientId, Year, Month)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Third Party Bill Detail Table
CREATE TABLE third_party_bill_detail (
    Id CHAR(36) PRIMARY KEY,
    ThirdPartyBillId CHAR(36) NOT NULL,
    Tag VARCHAR(100) NOT NULL COMMENT '工种、技能等级等标注',
    TotalWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalManagementFee DECIMAL(12,2) NOT NULL DEFAULT 0,
    EmployeeCount INT NOT NULL DEFAULT 0,
    TotalHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (ThirdPartyBillId) REFERENCES third_party_bill(Id) ON DELETE CASCADE,
    INDEX idx_bill (ThirdPartyBillId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Expense Application Table
CREATE TABLE expense_application (
    Id CHAR(36) PRIMARY KEY,
    ApplicationNo VARCHAR(50) NOT NULL UNIQUE,
    ExpenseType INT NOT NULL COMMENT '0:报销 1:采购 2:其他',
    ApplicantId CHAR(36) NOT NULL,
    OrgUnitId CHAR(36) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Description VARCHAR(2000) NOT NULL,
    Status INT NOT NULL DEFAULT 0 COMMENT '0:草稿 1:已提交 2:已通过 3:已拒绝 4:已付款',
    ApproverId CHAR(36) NULL,
    ApprovedAt DATETIME NULL,
    ApprovalComment VARCHAR(1000) NULL,
    AttachmentUrls JSON NULL,
    BudgetId CHAR(36) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id),
    INDEX idx_app_no (ApplicationNo),
    INDEX idx_applicant (ApplicantId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- System Config Parameter Table
CREATE TABLE sys_config_param (
    Id CHAR(36) PRIMARY KEY,
    Category VARCHAR(50) NOT NULL,
    ParamKey VARCHAR(100) NOT NULL,
    ParamValue VARCHAR(4000) NOT NULL,
    Description VARCHAR(500) NULL,
    OrgUnitId CHAR(36) NULL COMMENT 'NULL表示全局配置',
    IsGlobal TINYINT(1) NOT NULL DEFAULT 1,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    TakeEffectImmediately TINYINT(1) NOT NULL DEFAULT 1,
    ScheduledTakeEffectDate DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    ChangeReason VARCHAR(500) NULL,
    UNIQUE KEY uk_category_key (Category, ParamKey),
    INDEX idx_category (Category),
    INDEX idx_org (OrgUnitId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- System Menu Table
CREATE TABLE sys_menu (
    Id CHAR(36) PRIMARY KEY,
    MenuName VARCHAR(100) NOT NULL,
    ParentId VARCHAR(36) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    RoutePath VARCHAR(200) NULL,
    ComponentPath VARCHAR(200) NULL,
    Icon VARCHAR(50) NULL,
    IsVisible TINYINT(1) NOT NULL DEFAULT 1,
    PermissionCode VARCHAR(100) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    INDEX idx_parent (ParentId),
    INDEX idx_permission (PermissionCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- System Role Table
CREATE TABLE sys_role (
    Id CHAR(36) PRIMARY KEY,
    RoleName VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    OrgUnitId CHAR(36) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    INDEX idx_role_name (RoleName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- System Role Permission Table
CREATE TABLE sys_role_permission (
    Id CHAR(36) PRIMARY KEY,
    RoleId CHAR(36) NOT NULL,
    PermissionCode VARCHAR(100) NOT NULL,
    PermissionType VARCHAR(50) NOT NULL COMMENT 'Menu/Button/Data',
    FOREIGN KEY (RoleId) REFERENCES sys_role(Id) ON DELETE CASCADE,
    UNIQUE KEY uk_role_permission (RoleId, PermissionCode),
    INDEX idx_permission (PermissionCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert Default Config Parameters
-- Using REPLACE to avoid duplicates during multiple executions
REPLACE INTO sys_config_param (Id, Category, ParamKey, ParamValue, Description, IsGlobal) VALUES
(UUID(), 'Salary', 'TrialDays', '3', '试岗期天数', 1),
(UUID(), 'Salary', 'TrialDailySalary', '80', '试岗期固定日薪', 1),
(UUID(), 'Salary', 'TrialNoSalaryIfInsufficient', 'true', '试岗期出勤不足是否无工资', 1),
(UUID(), 'Salary', 'TrialIgnoreOvertime', 'true', '试岗期是否忽略加班', 1),
(UUID(), 'Salary', 'StandardDailyHours', '8', '标准日工时', 1),
(UUID(), 'Salary', 'WeekdayOvertimeMultiplier', '1.5', '工作日加班系数', 1),
(UUID(), 'Salary', 'WeekendOvertimeMultiplier', '2.0', '休息日加班系数', 1),
(UUID(), 'Salary', 'HolidayOvertimeMultiplier', '3.0', '节假日加班系数', 1),
(UUID(), 'Salary', 'MealAllowanceTiers', '[{\"MinHours\":0,\"MaxHours\":4,\"Amount\":0},{\"MinHours\":4,\"MaxHours\":6,\"Amount\":10},{\"MinHours\":6,\"MaxHours\":8,\"Amount\":15},{\"MinHours\":8,\"MaxHours\":999,\"Amount\":20}]', '饭补阶梯配置(JSON)', 1),
(UUID(), 'SocialSecurity', 'PersonalRate', '10.5', '社保个人缴费比例(%)', 1),
(UUID(), 'HousingFund', 'PersonalRate', '12', '公积金个人缴费比例(%)', 1);

-- Insert Initial Admin Role
REPLACE INTO sys_role (Id, RoleName, Description, IsActive) VALUES
(UUID(), '超级管理员', '系统最高权限', 1);

-- Insert Initial Menu Items
REPLACE INTO sys_menu (Id, MenuName, ParentId, SortOrder, RoutePath, ComponentPath, Icon, IsVisible) VALUES
(UUID(), '系统管理', NULL, 100, NULL, NULL, 'Settings', 1),
(UUID(), '参数配置', (SELECT Id FROM sys_menu WHERE MenuName = '系统管理'), 1, '/config', 'config/index', 'Settings', 1);

-- Insert Initial Permissions
REPLACE INTO sys_role_permission (Id, RoleId, PermissionCode, PermissionType) VALUES
(UUID(), (SELECT Id FROM sys_role WHERE RoleName = '超级管理员'), 'config:view', 'Menu'),
(UUID(), (SELECT Id FROM sys_role WHERE RoleName = '超级管理员'), 'config:edit', 'Button');

-- Create View for Org Unit Tree Path (for MySQL 8.0+)
DROP VIEW IF EXISTS v_org_unit_path;
CREATE VIEW v_org_unit_path AS
WITH RECURSIVE org_path AS (
    SELECT 
        Id, 
        Code, 
        Name, 
        Level, 
        ParentId,
        CAST(Name AS CHAR(500)) AS PathName,
        CAST(Code AS CHAR(200)) AS PathCode
    FROM org_unit 
    WHERE ParentId IS NULL
    UNION ALL
    SELECT 
        o.Id, 
        o.Code, 
        o.Name, 
        o.Level, 
        o.ParentId,
        CONCAT(op.PathName, ' > ', o.Name) AS PathName,
        CONCAT(op.PathCode, ' > ', o.Code) AS PathCode
    FROM org_unit o
    INNER JOIN org_path op ON o.ParentId = op.Id
)
SELECT * FROM org_path;

-- Create View for Employee with Org Path
DROP VIEW IF EXISTS v_employee_org;
CREATE VIEW v_employee_org AS
SELECT 
    e.*,
    op.PathName AS OrgPathName,
    op.PathCode AS OrgPathCode
FROM employee e
INNER JOIN v_org_unit_path op ON e.OrgUnitId = op.Id;

-- Create View for Timesheet Summary
DROP VIEW IF EXISTS v_timesheet_summary;
CREATE VIEW v_timesheet_summary AS
SELECT 
    t.EmployeeId,
    e.Name AS EmployeeName,
    e.EmployeeNo,
    YEAR(t.Date) AS Year,
    MONTH(t.Date) AS Month,
    SUM(t.WorkingHours) AS TotalHours,
    SUM(t.OvertimeHours) AS TotalOvertimeHours,
    COUNT(*) AS RecordCount
FROM timesheet t
INNER JOIN employee e ON t.EmployeeId = e.Id
GROUP BY t.EmployeeId, e.Name, e.EmployeeNo, YEAR(t.Date), MONTH(t.Date);

-- Create View for Salary Summary
DROP VIEW IF EXISTS v_salary_summary;
CREATE VIEW v_salary_summary AS
SELECT 
    YEAR(sc.CalculatedAt) AS Year,
    MONTH(sc.CalculatedAt) AS Month,
    COUNT(*) AS EmployeeCount,
    SUM(sc.GrossWages) AS TotalGrossWages,
    SUM(sc.NetWages) AS TotalNetWages,
    SUM(sc.OvertimeWages) AS TotalOvertimeWages,
    AVG(sc.NetWages) AS AvgNetWages
FROM salary_calculation sc
GROUP BY YEAR(sc.CalculatedAt), MONTH(sc.CalculatedAt);

-- Create View for Third Party Bill Summary
DROP VIEW IF EXISTS v_third_party_bill_summary;
CREATE VIEW v_third_party_bill_summary AS
SELECT 
    tp.ClientId,
    o.Name AS ClientName,
    tp.Year,
    tp.Month,
    SUM(tp.TotalAmount) AS TotalAmount,
    SUM(tp.TotalWages) AS TotalWages,
    SUM(tp.ManagementFee) AS TotalManagementFee,
    COUNT(*) AS BillCount
FROM third_party_bill tp
INNER JOIN org_unit o ON tp.ClientId = o.Id
GROUP BY tp.ClientId, o.Name, tp.Year, tp.Month;

-- Create Indexes for Performance
-- Note: Most indexes are already created with the tables above

-- Add Full-Text Index for Employee Search
ALTER TABLE employee ADD FULLTEXT INDEX ft_employee_name (Name, EmployeeNo);

-- Add Full-Text Index for Org Unit Search  
ALTER TABLE org_unit ADD FULLTEXT INDEX ft_org_unit_name (Name, Code);

-- Optimize Tables (for MySQL 8.0+)
SET GLOBAL innodb_flush_log_at_trx_commit = 2;
SET GLOBAL innodb_buffer_pool_size = 268435456;
SET GLOBAL innodb_log_file_size = 67108864;

-- Create Event for Monthly Salary Calculation (Optional)
-- DELIMITER //
-- CREATE EVENT IF NOT EXISTS event_monthly_salary_calculation
-- ON SCHEDULE EVERY 1 MONTH
-- STARTS '2026-06-01 00:00:00'
-- DO
-- BEGIN
--     -- This is a placeholder for automated salary calculation
--     -- Actual implementation would call a stored procedure or external service
-- END //
-- DELIMITER ;

-- Enable Events (if not already enabled)
-- SET GLOBAL event_scheduler = ON;

SELECT 'HRMS Database Schema initialized successfully!' AS Result;
