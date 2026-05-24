-- HRMS Database Migration Script
-- Date: 2026-05-24
-- Description: Fix consistency between entities and database schema

-- 1. Update HRMS Database (Employee & PayrollRun)
USE HRMS;

ALTER TABLE employee 
ADD COLUMN Gender INT NOT NULL DEFAULT 0 AFTER Name,
ADD COLUMN SocialSecurityBase DECIMAL(12,2) NULL AFTER PieceRatePrice,
ADD COLUMN HousingFundBase DECIMAL(12,2) NULL AFTER SocialSecurityBase,
ADD COLUMN ProbationDays INT NOT NULL DEFAULT 30 AFTER TrialDaysRemaining,
ADD COLUMN ContractType INT NOT NULL DEFAULT 0 AFTER ProbationDays,
ADD COLUMN HireDate DATE NULL AFTER ContractEndDate,
ADD COLUMN DismissDate DATETIME NULL AFTER IsActive,
ADD COLUMN DismissReason VARCHAR(500) NULL AFTER DismissDate;

ALTER TABLE payroll_run 
ADD COLUMN ApprovalSubmittedAt DATETIME NULL AFTER ApprovalRequestId;

-- 2. Update ProcessCenter Database (Callback features)
USE ProcessCenter;

ALTER TABLE proc_definition 
ADD COLUMN CallbackConfig VARCHAR(2000) NULL AFTER Status;

CREATE TABLE IF NOT EXISTS `proc_callback_log` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ProcessInstanceId` bigint NOT NULL,
    `CallbackUrl` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Payload` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StatusCode` int NOT NULL,
    `Response` longtext CHARACTER SET utf8mb4 NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `RetryCount` int NOT NULL DEFAULT 0,
    `ErrorMessage` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedTime` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_proc_callback_log_proc_instance_ProcessInstanceId` FOREIGN KEY (`ProcessInstanceId`) REFERENCES `proc_instance` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SELECT 'Migration completed successfully!' AS Result;
