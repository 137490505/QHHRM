CREATE TABLE IF NOT EXISTS org_unit (
    Id CHAR(36) PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    Level INT NOT NULL,
    ParentId CHAR(36) NULL,
    StandardDailyHours DECIMAL(5,2) NOT NULL DEFAULT 8.00,
    OvertimeMultiplier DECIMAL(3,2) NOT NULL DEFAULT 1.50,
    ManagerId VARCHAR(100) NULL,
    CostCenter VARCHAR(100) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (ParentId) REFERENCES org_unit(Id) ON DELETE SET NULL,
    INDEX idx_code (Code),
    INDEX idx_parent (ParentId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS employee (
    Id CHAR(36) PRIMARY KEY,
    EmployeeNo VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(100) NOT NULL,
    IdCard VARCHAR(18) NULL,
    Phone VARCHAR(20) NULL,
    Email VARCHAR(100) NULL,
    EmployeeType INT NOT NULL,
    SalaryMode INT NOT NULL,
    OrgUnitId CHAR(36) NOT NULL,
    ThirdPartyCompanyId CHAR(36) NULL,
    JobTitle VARCHAR(100) NULL,
    `Level` VARCHAR(20) NULL,
    Tags JSON NULL,
    HourlyRate DECIMAL(10,2) NULL,
    MonthlySalary DECIMAL(12,2) NULL,
    PieceRatePrice DECIMAL(10,2) NULL,
    TrialEndDate DATE NULL,
    TrialDaysRemaining INT NOT NULL DEFAULT 3,
    ContractStartDate DATE NULL,
    ContractEndDate DATE NULL,
    IsBlacklisted TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id),
    FOREIGN KEY (ThirdPartyCompanyId) REFERENCES org_unit(Id),
    INDEX idx_employee_no (EmployeeNo),
    INDEX idx_org_unit (OrgUnitId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS timesheet (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    Date DATE NOT NULL,
    ActualOrgUnitId CHAR(36) NOT NULL,
    WorkingHours DECIMAL(5,2) NOT NULL,
    OvertimeHours DECIMAL(5,2) NOT NULL DEFAULT 0,
    ShiftType INT NOT NULL DEFAULT 0,
    Remark VARCHAR(500) NULL,
    ApprovalStatus INT NOT NULL DEFAULT 0,
    ApproverId VARCHAR(100) NULL,
    ApprovedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    FOREIGN KEY (ActualOrgUnitId) REFERENCES org_unit(Id),
    INDEX idx_employee_date (EmployeeId, Date),
    INDEX idx_org_date (ActualOrgUnitId, Date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS salary_calculation (
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

CREATE TABLE IF NOT EXISTS payslip (
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

CREATE TABLE IF NOT EXISTS product (
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

CREATE TABLE IF NOT EXISTS output_record (
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

CREATE TABLE IF NOT EXISTS third_party_bill (
    Id CHAR(36) PRIMARY KEY,
    BillNo VARCHAR(50) NOT NULL UNIQUE,
    ClientId CHAR(36) NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    TotalWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    ManagementFee DECIMAL(12,2) NOT NULL DEFAULT 0,
    ManagementFeeRate DECIMAL(10,4) NOT NULL DEFAULT 0,
    ManagementFeeType VARCHAR(50) NOT NULL DEFAULT 'Fixed',
    OtherFees DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    Status INT NOT NULL DEFAULT 0,
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

CREATE TABLE IF NOT EXISTS third_party_bill_detail (
    Id CHAR(36) PRIMARY KEY,
    ThirdPartyBillId CHAR(36) NOT NULL,
    Tag VARCHAR(100) NOT NULL,
    TotalWages DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalManagementFee DECIMAL(12,2) NOT NULL DEFAULT 0,
    EmployeeCount INT NOT NULL DEFAULT 0,
    TotalHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (ThirdPartyBillId) REFERENCES third_party_bill(Id) ON DELETE CASCADE,
    INDEX idx_bill (ThirdPartyBillId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS expense_application (
    Id CHAR(36) PRIMARY KEY,
    ApplicationNo VARCHAR(50) NOT NULL UNIQUE,
    ExpenseType INT NOT NULL,
    ApplicantId CHAR(36) NOT NULL,
    OrgUnitId CHAR(36) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Description VARCHAR(2000) NOT NULL,
    Status INT NOT NULL DEFAULT 0,
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

CREATE TABLE IF NOT EXISTS sys_config_param (
    Id CHAR(36) PRIMARY KEY,
    Category VARCHAR(50) NOT NULL,
    ParamKey VARCHAR(100) NOT NULL,
    ParamValue VARCHAR(4000) NOT NULL,
    Description VARCHAR(500) NULL,
    OrgUnitId CHAR(36) NULL,
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

CREATE TABLE IF NOT EXISTS sys_menu (
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

CREATE TABLE IF NOT EXISTS sys_role (
    Id CHAR(36) PRIMARY KEY,
    RoleName VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    OrgUnitId CHAR(36) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    INDEX idx_role_name (RoleName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS sys_role_permission (
    Id CHAR(36) PRIMARY KEY,
    RoleId CHAR(36) NOT NULL,
    PermissionCode VARCHAR(100) NOT NULL,
    PermissionType VARCHAR(50) NOT NULL,
    FOREIGN KEY (RoleId) REFERENCES sys_role(Id) ON DELETE CASCADE,
    UNIQUE KEY uk_role_permission (RoleId, PermissionCode),
    INDEX idx_permission (PermissionCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO sys_config_param (Id, Category, ParamKey, ParamValue, Description, IsGlobal) VALUES
(UUID(), 'Salary', 'TrialDays', '3', 'Trial days', 1),
(UUID(), 'Salary', 'TrialDailySalary', '80', 'Trial daily salary', 1),
(UUID(), 'Salary', 'TrialNoSalaryIfInsufficient', 'true', 'No salary if trial days insufficient', 1),
(UUID(), 'Salary', 'TrialIgnoreOvertime', 'true', 'Ignore overtime during trial', 1),
(UUID(), 'Salary', 'StandardDailyHours', '8', 'Standard daily hours', 1),
(UUID(), 'Salary', 'WeekdayOvertimeMultiplier', '1.5', 'Weekday overtime multiplier', 1),
(UUID(), 'Salary', 'WeekendOvertimeMultiplier', '2.0', 'Weekend overtime multiplier', 1),
(UUID(), 'Salary', 'HolidayOvertimeMultiplier', '3.0', 'Holiday overtime multiplier', 1),
(UUID(), 'Salary', 'MealAllowanceTiers', '[{\"MinHours\":0,\"MaxHours\":4,\"Amount\":0},{\"MinHours\":4,\"MaxHours\":6,\"Amount\":10},{\"MinHours\":6,\"MaxHours\":8,\"Amount\":15},{\"MinHours\":8,\"MaxHours\":999,\"Amount\":20}]', 'Meal allowance tiers', 1),
(UUID(), 'SocialSecurity', 'PersonalRate', '10.5', 'Social security personal rate', 1),
(UUID(), 'HousingFund', 'PersonalRate', '12', 'Housing fund personal rate', 1);

INSERT IGNORE INTO sys_role (Id, RoleName, Description, IsActive) VALUES
(UUID(), 'Admin', 'Super administrator', 1);

SELECT 'Tables created successfully!' AS Result;