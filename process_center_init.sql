CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `proc_definition` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ProcessCode` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ProcessName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `VersionNo` int NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `CallbackConfig` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `CreatedTime` datetime(6) NOT NULL,
    `UpdatedTime` datetime(6) NOT NULL,
    CONSTRAINT `PK_proc_definition` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `proc_instance` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `InstanceNo` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ProcessCode` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `VersionNo` int NOT NULL,
    `BusinessSystem` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Title` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `CurrentNodeId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `CurrentNodeName` varchar(200) CHARACTER SET utf8mb4 NULL,
    `CurrentAssigneeId` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CurrentAssigneeName` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CurrentNodeIndex` int NOT NULL,
    `StarterId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `StarterName` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `StartedTime` datetime(6) NOT NULL,
    `FinishedTime` datetime(6) NULL,
    `ExtData` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_proc_instance` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `proc_definition_node` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ProcessDefinitionId` bigint NOT NULL,
    `NodeId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `NodeName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `NodeType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `AssigneeType` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `AssigneeId` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `AssigneeName` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `MultiPersonType` varchar(32) CHARACTER SET utf8mb4 NULL,
    `NextNodeId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_proc_definition_node` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_proc_definition_node_proc_definition_ProcessDefinitionId` FOREIGN KEY (`ProcessDefinitionId`) REFERENCES `proc_definition` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `proc_node_history` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ProcessInstanceId` bigint NOT NULL,
    `NodeId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `NodeName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `NodeType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `HandlerId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `HandlerName` varchar(128) CHARACTER SET utf8mb4 NULL,
    `Action` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ActionResult` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Comment` longtext CHARACTER SET utf8mb4 NULL,
    `ArrivedTime` datetime(6) NOT NULL,
    `HandledTime` datetime(6) NULL,
    `DurationMinutes` decimal(65,30) NULL,
    CONSTRAINT `PK_proc_node_history` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_proc_node_history_proc_instance_ProcessInstanceId` FOREIGN KEY (`ProcessInstanceId`) REFERENCES `proc_instance` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `proc_node_condition` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `ProcessDefinitionNodeId` bigint NOT NULL,
    `ConditionExpression` varchar(500) CHARACTER SET utf8mb4 NULL,
    `TargetNodeId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_proc_node_condition` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_proc_node_condition_proc_definition_node_ProcessDefinitionNo~` FOREIGN KEY (`ProcessDefinitionNodeId`) REFERENCES `proc_definition_node` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `proc_callback_log` (
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
    CONSTRAINT `PK_proc_callback_log` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_proc_callback_log_proc_instance_ProcessInstanceId` FOREIGN KEY (`ProcessInstanceId`) REFERENCES `proc_instance` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_proc_definition_ProcessCode_VersionNo` ON `proc_definition` (`ProcessCode`, `VersionNo`);

CREATE INDEX `IX_proc_definition_node_ProcessDefinitionId` ON `proc_definition_node` (`ProcessDefinitionId`);

CREATE INDEX `IX_proc_instance_BusinessId_BusinessType` ON `proc_instance` (`BusinessId`, `BusinessType`);

CREATE UNIQUE INDEX `IX_proc_instance_InstanceNo` ON `proc_instance` (`InstanceNo`);

CREATE INDEX `IX_proc_instance_ProcessCode` ON `proc_instance` (`ProcessCode`);

CREATE INDEX `IX_proc_node_condition_ProcessDefinitionNodeId` ON `proc_node_condition` (`ProcessDefinitionNodeId`);

CREATE INDEX `IX_proc_node_history_ProcessInstanceId` ON `proc_node_history` (`ProcessInstanceId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260524095209_InitialCreate', '8.0.0');

COMMIT;

