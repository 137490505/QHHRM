-- HRMS Shift & Attendance Module Schema
-- Version: 1.0
-- Date: 2026-05-24
-- Compatible with: MySQL 8.0+ / MySQL 9.x
--
-- Notes:
-- 1. This file is an incremental schema for the scheduling and attendance module.
-- 2. Primary and foreign keys use CHAR(36) to stay consistent with the current HRMS schema.
-- 3. Table names keep the design-document naming so later service/model mapping is clearer.

USE HRMS;

-- Shift master
CREATE TABLE IF NOT EXISTS `shift` (
    Id CHAR(36) PRIMARY KEY,
    ShiftCode VARCHAR(64) NOT NULL,
    ShiftName VARCHAR(100) NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsCrossDay TINYINT(1) NOT NULL DEFAULT 0,
    BreakMinutes INT NOT NULL DEFAULT 0,
    WorkMinutes INT NOT NULL DEFAULT 0,
    AllowOvertime TINYINT(1) NOT NULL DEFAULT 0,
    FlexibleMode TINYINT(1) NOT NULL DEFAULT 0,
    FlexibleInStart TIME NULL,
    FlexibleInEnd TIME NULL,
    Color VARCHAR(20) NULL,
    OrgUnitId CHAR(36) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_shift_code (ShiftCode),
    INDEX idx_shift_org (OrgUnitId, IsActive),
    CONSTRAINT fk_shift_org_unit FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Shift template header
CREATE TABLE IF NOT EXISTS shift_template (
    Id CHAR(36) PRIMARY KEY,
    TemplateCode VARCHAR(64) NOT NULL,
    TemplateName VARCHAR(100) NOT NULL,
    OrgUnitId CHAR(36) NULL,
    LineId CHAR(36) NULL,
    Remark VARCHAR(500) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_shift_template_code (TemplateCode),
    INDEX idx_shift_template_org (OrgUnitId, IsActive),
    INDEX idx_shift_template_line (LineId, IsActive),
    CONSTRAINT fk_shift_template_org_unit FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id) ON DELETE SET NULL,
    CONSTRAINT fk_shift_template_line FOREIGN KEY (LineId) REFERENCES org_unit(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Rotation rule
CREATE TABLE IF NOT EXISTS rotation_rule (
    Id CHAR(36) PRIMARY KEY,
    RuleCode VARCHAR(64) NOT NULL,
    RuleName VARCHAR(100) NOT NULL,
    OrgUnitId CHAR(36) NULL,
    TeamId CHAR(36) NULL,
    CycleUnit VARCHAR(16) NOT NULL COMMENT 'Day/Week/Month',
    CycleLength INT NOT NULL DEFAULT 1,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    SequenceJson JSON NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_rotation_rule_code (RuleCode),
    INDEX idx_rotation_rule_team (TeamId, IsActive),
    INDEX idx_rotation_rule_org (OrgUnitId, IsActive),
    CONSTRAINT fk_rotation_rule_org_unit FOREIGN KEY (OrgUnitId) REFERENCES org_unit(Id) ON DELETE SET NULL,
    CONSTRAINT fk_rotation_rule_team FOREIGN KEY (TeamId) REFERENCES org_unit(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Employee shift assignment
CREATE TABLE IF NOT EXISTS shift_assignment (
    Id CHAR(36) PRIMARY KEY,
    AssignmentDate DATE NOT NULL,
    EmployeeId CHAR(36) NOT NULL,
    EmployeeNo VARCHAR(64) NULL,
    TeamId CHAR(36) NULL,
    LineId CHAR(36) NULL,
    ShiftId CHAR(36) NOT NULL,
    ShiftCode VARCHAR(64) NOT NULL,
    ShiftName VARCHAR(100) NOT NULL,
    SourceType VARCHAR(32) NOT NULL COMMENT 'Manual/Template/Rotation/Default/Import/Exception',
    SourceId VARCHAR(64) NULL,
    Priority INT NOT NULL DEFAULT 0,
    Status VARCHAR(32) NOT NULL DEFAULT 'Active' COMMENT 'Active/Cancelled/Expired',
    Remark VARCHAR(500) NULL,
    CreatedBy VARCHAR(64) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedBy VARCHAR(64) NULL,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_assignment_employee_date_priority_status (EmployeeId, AssignmentDate, Priority, Status),
    INDEX idx_assignment_team_date (TeamId, AssignmentDate),
    INDEX idx_assignment_line_date (LineId, AssignmentDate),
    INDEX idx_assignment_shift_date (ShiftId, AssignmentDate),
    CONSTRAINT fk_shift_assignment_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    CONSTRAINT fk_shift_assignment_team FOREIGN KEY (TeamId) REFERENCES org_unit(Id) ON DELETE SET NULL,
    CONSTRAINT fk_shift_assignment_line FOREIGN KEY (LineId) REFERENCES org_unit(Id) ON DELETE SET NULL,
    CONSTRAINT fk_shift_assignment_shift FOREIGN KEY (ShiftId) REFERENCES `shift`(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Shift exception override
CREATE TABLE IF NOT EXISTS shift_exception (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    AssignmentDate DATE NOT NULL,
    OriginalShiftId CHAR(36) NULL,
    NewShiftId CHAR(36) NOT NULL,
    Reason VARCHAR(500) NULL,
    ApproveStatus VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Pending/Approved/Rejected',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    INDEX idx_shift_exception_employee_date (EmployeeId, AssignmentDate),
    INDEX idx_shift_exception_status (ApproveStatus, AssignmentDate),
    CONSTRAINT fk_shift_exception_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    CONSTRAINT fk_shift_exception_original_shift FOREIGN KEY (OriginalShiftId) REFERENCES `shift`(Id) ON DELETE SET NULL,
    CONSTRAINT fk_shift_exception_new_shift FOREIGN KEY (NewShiftId) REFERENCES `shift`(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Attendance source configuration
CREATE TABLE IF NOT EXISTS attendance_source (
    Id CHAR(36) PRIMARY KEY,
    SourceCode VARCHAR(64) NOT NULL,
    SourceName VARCHAR(100) NOT NULL,
    SourceType VARCHAR(32) NOT NULL COMMENT 'Device/DingTalk/WeCom/App/Excel',
    AdapterType VARCHAR(64) NOT NULL,
    ConfigJson JSON NULL,
    SyncMode VARCHAR(32) NOT NULL DEFAULT 'Manual' COMMENT 'Manual/Scheduled/Realtime',
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_attendance_source_code (SourceCode),
    INDEX idx_attendance_source_type (SourceType, IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Attendance synchronization jobs
CREATE TABLE IF NOT EXISTS attendance_sync_job (
    Id CHAR(36) PRIMARY KEY,
    SourceCode VARCHAR(64) NOT NULL,
    JobType VARCHAR(32) NOT NULL COMMENT 'Scheduled/ManualRetry/ManualBackfill/Import',
    SyncFrom DATETIME NOT NULL,
    SyncTo DATETIME NOT NULL,
    Status VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Pending/Running/Success/PartialSuccess/Failed',
    TotalCount INT NOT NULL DEFAULT 0,
    SuccessCount INT NOT NULL DEFAULT 0,
    FailCount INT NOT NULL DEFAULT 0,
    RetryCount INT NOT NULL DEFAULT 0,
    ErrorMessage VARCHAR(2000) NULL,
    TriggeredBy VARCHAR(64) NULL,
    StartedAt DATETIME NULL,
    FinishedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_attendance_sync_job_source_time (SourceCode, SyncFrom, SyncTo),
    INDEX idx_attendance_sync_job_status (Status, CreatedAt),
    CONSTRAINT fk_attendance_sync_job_source_code FOREIGN KEY (SourceCode) REFERENCES attendance_source(SourceCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Raw attendance records
CREATE TABLE IF NOT EXISTS attendance_raw_record (
    Id CHAR(36) PRIMARY KEY,
    SourceCode VARCHAR(64) NOT NULL,
    SourceRecordId VARCHAR(128) NULL,
    EmployeeNo VARCHAR(64) NULL,
    EmployeeId CHAR(36) NULL,
    PunchTime DATETIME NOT NULL,
    PunchType VARCHAR(32) NULL COMMENT 'In/Out/Unknown',
    DeviceId VARCHAR(64) NULL,
    DeviceName VARCHAR(100) NULL,
    LocationText VARCHAR(500) NULL,
    Latitude DECIMAL(10,6) NULL,
    Longitude DECIMAL(10,6) NULL,
    IsOvertimeTag TINYINT(1) NOT NULL DEFAULT 0,
    OriginFileUrl VARCHAR(500) NULL,
    RawPayload JSON NULL,
    ParseStatus VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Pending/Parsed/Invalid',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uk_attendance_raw_source_record (SourceCode, SourceRecordId),
    INDEX idx_attendance_raw_employee_time (EmployeeId, PunchTime),
    INDEX idx_attendance_raw_source_time (SourceCode, PunchTime),
    CONSTRAINT fk_attendance_raw_source_code FOREIGN KEY (SourceCode) REFERENCES attendance_source(SourceCode),
    CONSTRAINT fk_attendance_raw_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Cleaned daily attendance result
CREATE TABLE IF NOT EXISTS attendance_clean_record (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    AttendanceDate DATE NOT NULL,
    FirstInTime DATETIME NULL,
    LastOutTime DATETIME NULL,
    TotalPunchCount INT NOT NULL DEFAULT 0,
    SourceSummary VARCHAR(500) NULL,
    CleanStatus VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Pending/Cleaned/Abnormal/Skipped',
    AbnormalFlags VARCHAR(500) NULL,
    MatchedShiftId CHAR(36) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    UNIQUE KEY uk_attendance_clean_employee_date (EmployeeId, AttendanceDate),
    INDEX idx_attendance_clean_shift_date (MatchedShiftId, AttendanceDate),
    INDEX idx_attendance_clean_status_date (CleanStatus, AttendanceDate),
    CONSTRAINT fk_attendance_clean_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id),
    CONSTRAINT fk_attendance_clean_shift FOREIGN KEY (MatchedShiftId) REFERENCES `shift`(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Attendance exceptions
CREATE TABLE IF NOT EXISTS attendance_exception (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    AttendanceDate DATE NOT NULL,
    ExceptionType VARCHAR(32) NOT NULL COMMENT 'Late/EarlyLeave/NoSchedule/MissingPunch/InvalidDevice/InvalidLocation',
    ExceptionLevel VARCHAR(16) NOT NULL DEFAULT 'Normal' COMMENT 'Low/Normal/High/Critical',
    SourceRecordId VARCHAR(128) NULL,
    Description VARCHAR(1000) NULL,
    ProcessStatus VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Pending/Processing/Resolved/Ignored',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    INDEX idx_attendance_exception_employee_date (EmployeeId, AttendanceDate),
    INDEX idx_attendance_exception_status (ProcessStatus, AttendanceDate),
    INDEX idx_attendance_exception_type (ExceptionType, AttendanceDate),
    CONSTRAINT fk_attendance_exception_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Attendance repair requests
CREATE TABLE IF NOT EXISTS attendance_repair_request (
    Id CHAR(36) PRIMARY KEY,
    EmployeeId CHAR(36) NOT NULL,
    AttendanceDate DATE NOT NULL,
    RepairType VARCHAR(32) NOT NULL COMMENT 'MissingIn/MissingOut/Adjustment/ManualAdd',
    RepairTime DATETIME NOT NULL,
    Reason VARCHAR(1000) NULL,
    ApproveStatus VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Pending/Approved/Rejected',
    ApprovedBy VARCHAR(64) NULL,
    ApprovedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NULL,
    INDEX idx_attendance_repair_employee_date (EmployeeId, AttendanceDate),
    INDEX idx_attendance_repair_status (ApproveStatus, AttendanceDate),
    CONSTRAINT fk_attendance_repair_employee FOREIGN KEY (EmployeeId) REFERENCES employee(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT 'HRMS shift and attendance schema initialized successfully!' AS Result;
