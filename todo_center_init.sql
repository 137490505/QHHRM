CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `todo_agent_setting` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `PrincipalUserId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `AgentUserId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ScopeType` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `TaskTypeCode` varchar(64) CHARACTER SET utf8mb4 NULL,
    `StartTime` datetime(6) NOT NULL,
    `EndTime` datetime(6) NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedTime` datetime(6) NOT NULL,
    `UpdatedTime` datetime(6) NOT NULL,
    CONSTRAINT `PK_todo_agent_setting` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `todo_task` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `TaskNo` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `TaskTypeCode` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Title` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessSystem` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ProcessInstanceId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `ProcessNodeId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `AssigneeId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `AssigneeName` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `TaskCategory` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `OwnerDeptId` longtext CHARACTER SET utf8mb4 NULL,
    `Priority` int NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Result` longtext CHARACTER SET utf8mb4 NULL,
    `DueTime` datetime(6) NULL,
    `CompletedTime` datetime(6) NULL,
    `ReadTime` datetime(6) NULL,
    `OriginalTaskId` bigint NULL,
    `SourcePayload` longtext CHARACTER SET utf8mb4 NULL,
    `ExtData` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedTime` datetime(6) NOT NULL,
    `UpdatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `UpdatedTime` datetime(6) NOT NULL,
    CONSTRAINT `PK_todo_task` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `todo_task_log` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `TaskId` bigint NOT NULL,
    `Action` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `OperatorId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `OperatorName` varchar(128) CHARACTER SET utf8mb4 NULL,
    `ActionResult` varchar(64) CHARACTER SET utf8mb4 NULL,
    `Comment` longtext CHARACTER SET utf8mb4 NULL,
    `BeforeStatus` varchar(32) CHARACTER SET utf8mb4 NULL,
    `AfterStatus` varchar(32) CHARACTER SET utf8mb4 NULL,
    `ExtData` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedTime` datetime(6) NOT NULL,
    CONSTRAINT `PK_todo_task_log` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_todo_task_log_todo_task_TaskId` FOREIGN KEY (`TaskId`) REFERENCES `todo_task` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `todo_task_notify_log` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `TaskId` bigint NOT NULL,
    `NotifyType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ReceiverId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ReceiverName` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Channel` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `SentTime` datetime(6) NOT NULL,
    `CreatedTime` datetime(6) NOT NULL,
    CONSTRAINT `PK_todo_task_notify_log` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_todo_task_notify_log_todo_task_TaskId` FOREIGN KEY (`TaskId`) REFERENCES `todo_task` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_todo_agent_setting_PrincipalUserId` ON `todo_agent_setting` (`PrincipalUserId`);

CREATE INDEX `IX_todo_task_AssigneeId_Status` ON `todo_task` (`AssigneeId`, `Status`);

CREATE INDEX `IX_todo_task_BusinessId_BusinessType` ON `todo_task` (`BusinessId`, `BusinessType`);

CREATE INDEX `IX_todo_task_ProcessInstanceId` ON `todo_task` (`ProcessInstanceId`);

CREATE UNIQUE INDEX `IX_todo_task_TaskNo` ON `todo_task` (`TaskNo`);

CREATE INDEX `IX_todo_task_log_TaskId` ON `todo_task_log` (`TaskId`);

CREATE INDEX `IX_todo_task_notify_log_TaskId` ON `todo_task_notify_log` (`TaskId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260524095201_InitialCreate', '8.0.0');

COMMIT;

