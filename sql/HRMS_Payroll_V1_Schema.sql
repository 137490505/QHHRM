-- HRMS Payroll Module Schema
-- Version: 1.0
-- Date: 2026-05-24
-- Compatible with: MySQL 8.0+ / MySQL 9.x
--
-- Notes:
-- 1. This file is an incremental schema for the payroll module.
-- 2. It reuses the existing employee/org_unit tables and does not replace the current salary_calculation table yet.
-- 3. Missing payroll-specific employee fields are stored in employee_payroll_profile to reduce impact on the current employee model.
-- 4. OrgUnitId on the existing employee table is treated as the current site/department reference in V1.

USE HRMS;

-- Payroll employee type configuration
CREATE TABLE IF NOT EXISTS employee_type (
    Id CHAR(36) PRIMARY KEY,
    TypeCode VARCHAR(64) NOT NULL,
    TypeName VARCHAR(100) NOT NULL,
    SalaryMode VARCHAR(32) NOT NULL COMMENT 'fixed/hourly/piecework/base+piece',
    HasOvertime TINYINT(1) NOT NULL DEFAULT 0,
    HasMealSubsidy TINYINT(1) NOT NULL DEFAULT 0,
    HasNightSubsidy TINYINT(1) NOT NULL DEFAULT 0,
    HasPerformance TINYINT(1) NOT NULL DEFAULT 0,
    HasSocialSecurity TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_employee_type_code (TypeCode),
    UNIQUE KEY uk_employee_type_name (TypeName),
    INDEX idx_employee_type_active (IsActive, SortOrder)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Payroll extension profile for existing employees
CREATE TABLE IF NOT EXISTS employee_payroll_profile (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    EmployeeTypeId CHAR(36) NOT NULL,
    ShiftType VARCHAR(32) NULL COMMENT 'day/night/flexible',
    BankAccount VARCHAR(64) NULL,
    PayrollStatus VARCHAR(32) NOT NULL DEFAULT 'Active' COMMENT 'Active/Suspended/Left',
    JoinPayrollDate DATE NULL,
    LeavePayrollDate DATE NULL,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_employee_payroll_profile_employee (EmployeeId),
    INDEX idx_employee_payroll_profile_type (EmployeeTypeId, PayrollStatus),
    CONSTRAINT fk_employee_payroll_profile_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    CONSTRAINT fk_employee_payroll_profile_type FOREIGN KEY (EmployeeTypeId) REFERENCES employee_type(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Salary rule by payroll employee type
CREATE TABLE IF NOT EXISTS salary_rule (
    Id CHAR(36) PRIMARY KEY,
    RuleCode VARCHAR(64) NOT NULL,
    RuleName VARCHAR(100) NOT NULL,
    EmployeeTypeId CHAR(36) NOT NULL,
    EffectiveStart DATE NOT NULL,
    EffectiveEnd DATE NULL,
    FixedSalary DECIMAL(12,2) NULL,
    HourlyRate DECIMAL(10,2) NULL,
    PieceworkUnitPrice DECIMAL(10,2) NULL,
    BaseSalaryForPiecework DECIMAL(12,2) NULL,
    MealSubsidyPerDay DECIMAL(10,2) NOT NULL DEFAULT 0,
    NightSubsidyPerDay DECIMAL(10,2) NOT NULL DEFAULT 0,
    PerformanceBase DECIMAL(12,2) NULL,
    DefaultOvertimeMultiplier DECIMAL(6,2) NOT NULL DEFAULT 1.50,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_salary_rule_code (RuleCode),
    INDEX idx_salary_rule_type_date (EmployeeTypeId, EffectiveStart, EffectiveEnd),
    INDEX idx_salary_rule_active (IsActive, EffectiveStart),
    CONSTRAINT fk_salary_rule_employee_type FOREIGN KEY (EmployeeTypeId) REFERENCES employee_type(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Overtime multiplier by payroll employee type and holiday category
CREATE TABLE IF NOT EXISTS overtime_rate_config (
    Id CHAR(36) PRIMARY KEY,
    ConfigCode VARCHAR(64) NOT NULL,
    EmployeeTypeId CHAR(36) NOT NULL,
    HolidayType VARCHAR(32) NOT NULL COMMENT 'weekday/weekend/legal/company',
    Multiplier DECIMAL(6,2) NOT NULL,
    EffectiveStart DATE NOT NULL,
    EffectiveEnd DATE NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_overtime_rate_config_code (ConfigCode),
    INDEX idx_overtime_rate_type_holiday (EmployeeTypeId, HolidayType, EffectiveStart),
    CONSTRAINT fk_overtime_rate_employee_type FOREIGN KEY (EmployeeTypeId) REFERENCES employee_type(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Holiday and special day rules
CREATE TABLE IF NOT EXISTS holiday_rule (
    Id CHAR(36) PRIMARY KEY,
    HolidayCode VARCHAR(64) NOT NULL,
    HolidayName VARCHAR(100) NOT NULL,
    HolidayDate DATE NOT NULL,
    HolidayType VARCHAR(32) NOT NULL COMMENT 'legal/weekend/company',
    OvertimeMultiplier DECIMAL(6,2) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_holiday_rule_code (HolidayCode),
    UNIQUE KEY uk_holiday_rule_date_type (HolidayDate, HolidayType),
    INDEX idx_holiday_rule_date (HolidayDate, IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Daily attendance and piecework input for payroll calculation
CREATE TABLE IF NOT EXISTS attendance (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    WorkDate DATE NOT NULL,
    NormalHours DECIMAL(6,2) NOT NULL DEFAULT 0,
    OvertimeHours DECIMAL(6,2) NOT NULL DEFAULT 0,
    OvertimeType VARCHAR(32) NULL COMMENT 'weekday/weekend/legal/company',
    PieceworkQty DECIMAL(12,2) NOT NULL DEFAULT 0,
    ShiftType VARCHAR(32) NULL COMMENT 'day/night/flexible',
    IsNightShift TINYINT(1) NOT NULL DEFAULT 0,
    AttendanceSource VARCHAR(32) NOT NULL DEFAULT 'Manual' COMMENT 'Manual/Import/Device/API',
    SourceRecordId VARCHAR(128) NULL,
    Status VARCHAR(32) NOT NULL DEFAULT 'Draft' COMMENT 'Draft/Confirmed/Locked',
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_attendance_employee_date (EmployeeId, WorkDate),
    INDEX idx_attendance_work_date (WorkDate, Status),
    INDEX idx_attendance_source (AttendanceSource, SourceRecordId),
    CONSTRAINT fk_attendance_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Payroll adjustments: subsidy, deduction, insurance, housing fund, tax, etc.
CREATE TABLE IF NOT EXISTS salary_adjustment (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    YearMonth CHAR(7) NOT NULL COMMENT 'YYYY-MM',
    AdjustmentType VARCHAR(64) NOT NULL COMMENT 'SocialSecurityEmployee/SocialSecurityCompany/ProvidentFundEmployee/ProvidentFundCompany/IncomeTax/MealSubsidy/NightSubsidy/PerformanceBonus/DisciplineDeduction/OtherDeduction/OtherAllowance',
    Amount DECIMAL(12,2) NOT NULL,
    SourceType VARCHAR(32) NOT NULL DEFAULT 'Manual' COMMENT 'Manual/Import/System',
    SourceId VARCHAR(64) NULL,
    IsAutoGenerated TINYINT(1) NOT NULL DEFAULT 0,
    Remark VARCHAR(500) NULL,
    CreatedBy VARCHAR(64) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(64) NULL,
    UpdatedAt DATETIME NULL,
    INDEX idx_salary_adjustment_employee_month (EmployeeId, YearMonth),
    INDEX idx_salary_adjustment_month_type (YearMonth, AdjustmentType),
    CONSTRAINT fk_salary_adjustment_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Optional tax bracket configuration for automatic income tax calculation
CREATE TABLE IF NOT EXISTS income_tax_rule (
    Id CHAR(36) PRIMARY KEY,
    RuleYear INT NOT NULL,
    LevelNo INT NOT NULL,
    MinTaxableAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    MaxTaxableAmount DECIMAL(12,2) NULL,
    TaxRate DECIMAL(6,4) NOT NULL,
    QuickDeduction DECIMAL(12,2) NOT NULL DEFAULT 0,
    ThresholdAmount DECIMAL(12,2) NOT NULL DEFAULT 5000,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_income_tax_rule_year_level (RuleYear, LevelNo),
    INDEX idx_income_tax_rule_active (RuleYear, IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Monthly payroll run header used for calculate/recalculate/rollback tracking
CREATE TABLE IF NOT EXISTS payroll_run (
    Id CHAR(36) PRIMARY KEY,
    RunNo VARCHAR(64) NOT NULL,
    YearMonth CHAR(7) NOT NULL COMMENT 'YYYY-MM',
    RunType VARCHAR(32) NOT NULL DEFAULT 'Monthly' COMMENT 'Monthly/Recalculate/Rollback',
    Status VARCHAR(32) NOT NULL DEFAULT 'Draft' COMMENT 'Draft/Running/Completed/Failed/RolledBack',
    EmployeeScopeJson JSON NULL,
    RuleSnapshotJson JSON NULL,
    TriggeredBy VARCHAR(64) NULL,
    ApprovalRequestId VARCHAR(128) NULL,
    ApprovalSubmittedAt DATETIME NULL,
    ApprovedBy VARCHAR(64) NULL,
    ApprovedAt DATETIME NULL,
    StartedAt DATETIME NULL,
    FinishedAt DATETIME NULL,
    Remark VARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_payroll_run_no (RunNo),
    INDEX idx_payroll_run_month_status (YearMonth, Status),
    INDEX idx_payroll_run_type_time (RunType, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Payroll monthly summary
CREATE TABLE IF NOT EXISTS payroll (
    Id CHAR(36) PRIMARY KEY,
    PayrollRunId CHAR(36) NOT NULL,
    EmployeeId CHAR(36) NOT NULL,
    EmployeeTypeId CHAR(36) NULL,
    YearMonth CHAR(7) NOT NULL COMMENT 'YYYY-MM',
    EmployeeNoSnapshot VARCHAR(64) NOT NULL,
    EmployeeNameSnapshot VARCHAR(100) NOT NULL,
    OrgUnitIdSnapshot CHAR(36) NULL,
    SalaryModeSnapshot VARCHAR(32) NOT NULL,
    NormalWage DECIMAL(12,2) NOT NULL DEFAULT 0,
    OvertimeWage DECIMAL(12,2) NOT NULL DEFAULT 0,
    PieceworkWage DECIMAL(12,2) NOT NULL DEFAULT 0,
    FixedSalary DECIMAL(12,2) NOT NULL DEFAULT 0,
    BaseSalary DECIMAL(12,2) NOT NULL DEFAULT 0,
    MealSubsidy DECIMAL(12,2) NOT NULL DEFAULT 0,
    NightSubsidy DECIMAL(12,2) NOT NULL DEFAULT 0,
    PerformanceBonus DECIMAL(12,2) NOT NULL DEFAULT 0,
    OtherAllowance DECIMAL(12,2) NOT NULL DEFAULT 0,
    SocialSecurityEmployee DECIMAL(12,2) NOT NULL DEFAULT 0,
    SocialSecurityCompany DECIMAL(12,2) NOT NULL DEFAULT 0,
    ProvidentFundEmployee DECIMAL(12,2) NOT NULL DEFAULT 0,
    ProvidentFundCompany DECIMAL(12,2) NOT NULL DEFAULT 0,
    IncomeTax DECIMAL(12,2) NOT NULL DEFAULT 0,
    OtherDeduction DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalGross DECIMAL(12,2) NOT NULL DEFAULT 0,
    NetSalary DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalCompanyCost DECIMAL(12,2) NOT NULL DEFAULT 0,
    Status VARCHAR(32) NOT NULL DEFAULT 'Draft' COMMENT 'Draft/Calculated/Approved/Paid/RolledBack',
    CalculatedAt DATETIME NULL,
    PaidAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_payroll_run_employee (PayrollRunId, EmployeeId),
    INDEX idx_payroll_employee_month (EmployeeId, YearMonth),
    INDEX idx_payroll_month_status (YearMonth, Status),
    CONSTRAINT fk_payroll_run FOREIGN KEY (PayrollRunId) REFERENCES payroll_run(Id),
    CONSTRAINT fk_payroll_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    CONSTRAINT fk_payroll_employee_type FOREIGN KEY (EmployeeTypeId) REFERENCES employee_type(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Detailed payroll component lines
CREATE TABLE IF NOT EXISTS payroll_detail (
    Id CHAR(36) PRIMARY KEY,
    PayrollId CHAR(36) NOT NULL,
    ComponentCode VARCHAR(64) NOT NULL,
    ComponentName VARCHAR(100) NOT NULL,
    ComponentCategory VARCHAR(32) NOT NULL COMMENT 'Earning/Deduction/CompanyCost/Tax',
    Amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    Quantity DECIMAL(12,2) NULL,
    UnitPrice DECIMAL(12,2) NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    SourceType VARCHAR(32) NOT NULL DEFAULT 'System' COMMENT 'System/Adjustment/Import/Manual',
    SourceId VARCHAR(64) NULL,
    Remark VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_payroll_detail_payroll (PayrollId, SortOrder),
    INDEX idx_payroll_detail_component (ComponentCode, ComponentCategory),
    CONSTRAINT fk_payroll_detail_payroll FOREIGN KEY (PayrollId) REFERENCES payroll(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT 'HRMS payroll v1 schema initialized successfully!' AS Result;
