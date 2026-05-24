-- HRMS 角色、部门负责人及普通员工初始化脚本
-- 适用数据库：MySQL 8.0+
-- 说明：
-- 1. 本脚本按当前 HRMS 实体模型生成角色、组织、员工、账号、角色权限数据。
-- 2. 若 sys_menu 尚未由系统初始化，本脚本会补齐本次角色所需的最小权限菜单。
-- 3. “普通员工仅查看个人工资条、申请请假”在当前系统中映射为：
--    - `page.salary.list`：工资列表/工资条入口
--    - `page.process.requests`：流程申请入口（用于请假申请）
--    数据范围仍需后端按登录人做限制，本脚本仅处理菜单/角色权限。

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS `HRMS`
DEFAULT CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE `HRMS`;

START TRANSACTION;

SET @now = NOW(6);

-- =========================================================
-- 一、基础表：若不存在则创建
-- =========================================================

CREATE TABLE IF NOT EXISTS `org_unit` (
    `Id` char(36) NOT NULL,
    `Code` varchar(50) NOT NULL,
    `Name` varchar(200) NOT NULL,
    `Level` int NOT NULL,
    `ParentId` char(36) DEFAULT NULL,
    `ManagerId` varchar(100) DEFAULT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_org_unit_Code` (`Code`),
    KEY `IX_org_unit_ParentId` (`ParentId`),
    CONSTRAINT `FK_org_unit_org_unit_ParentId`
        FOREIGN KEY (`ParentId`) REFERENCES `org_unit` (`Id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `employee` (
    `Id` char(36) NOT NULL,
    `EmployeeNo` varchar(50) NOT NULL,
    `Name` varchar(100) NOT NULL,
    `Gender` int NOT NULL DEFAULT 0,
    `IdCard` varchar(18) DEFAULT NULL,
    `Phone` varchar(20) DEFAULT NULL,
    `Email` varchar(100) DEFAULT NULL,
    `EmployeeType` int NOT NULL DEFAULT 0,
    `SalaryMode` int NOT NULL DEFAULT 1,
    `OrgUnitId` char(36) NOT NULL,
    `ThirdPartyCompanyId` char(36) DEFAULT NULL,
    `JobTitle` varchar(100) DEFAULT NULL,
    `Level` varchar(50) DEFAULT NULL,
    `Tags` json DEFAULT NULL,
    `HourlyRate` decimal(10,2) DEFAULT NULL,
    `MonthlySalary` decimal(12,2) DEFAULT NULL,
    `PieceRatePrice` decimal(10,2) DEFAULT NULL,
    `SocialSecurityBase` decimal(12,2) DEFAULT NULL,
    `HousingFundBase` decimal(12,2) DEFAULT NULL,
    `TrialEndDate` datetime(6) DEFAULT NULL,
    `TrialDaysRemaining` int NOT NULL DEFAULT 3,
    `ProbationDays` int NOT NULL DEFAULT 30,
    `ContractType` int NOT NULL DEFAULT 0,
    `ContractStartDate` datetime(6) DEFAULT NULL,
    `ContractEndDate` datetime(6) DEFAULT NULL,
    `HireDate` datetime(6) DEFAULT NULL,
    `IsBlacklisted` tinyint(1) NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `DismissDate` datetime(6) DEFAULT NULL,
    `DismissReason` varchar(500) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_employee_EmployeeNo` (`EmployeeNo`),
    KEY `IX_employee_OrgUnitId` (`OrgUnitId`),
    KEY `IX_employee_ThirdPartyCompanyId` (`ThirdPartyCompanyId`),
    CONSTRAINT `FK_employee_org_unit_OrgUnitId`
        FOREIGN KEY (`OrgUnitId`) REFERENCES `org_unit` (`Id`),
    CONSTRAINT `FK_employee_org_unit_ThirdPartyCompanyId`
        FOREIGN KEY (`ThirdPartyCompanyId`) REFERENCES `org_unit` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sys_menu` (
    `Id` char(36) NOT NULL,
    `MenuKey` varchar(100) NOT NULL,
    `MenuName` varchar(100) NOT NULL,
    `ParentId` char(36) DEFAULT NULL,
    `MenuType` varchar(20) NOT NULL,
    `SortOrder` int NOT NULL DEFAULT 0,
    `RoutePath` varchar(200) DEFAULT NULL,
    `ComponentPath` varchar(200) DEFAULT NULL,
    `Icon` varchar(50) DEFAULT NULL,
    `IsVisible` tinyint(1) NOT NULL DEFAULT 1,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `PermissionCode` varchar(100) DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_sys_menu_MenuKey` (`MenuKey`),
    KEY `IX_sys_menu_ParentId` (`ParentId`),
    KEY `IX_sys_menu_PermissionCode` (`PermissionCode`),
    CONSTRAINT `FK_sys_menu_sys_menu_ParentId`
        FOREIGN KEY (`ParentId`) REFERENCES `sys_menu` (`Id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sys_role` (
    `Id` char(36) NOT NULL,
    `RoleCode` varchar(100) NOT NULL,
    `RoleName` varchar(100) NOT NULL,
    `Description` varchar(500) DEFAULT NULL,
    `OrgUnitId` char(36) DEFAULT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_sys_role_RoleCode` (`RoleCode`),
    KEY `IX_sys_role_OrgUnitId` (`OrgUnitId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sys_user` (
    `Id` char(36) NOT NULL,
    `Username` varchar(100) NOT NULL,
    `Password` varchar(200) NOT NULL,
    `Name` varchar(100) NOT NULL,
    `Phone` varchar(20) DEFAULT NULL,
    `Email` varchar(100) DEFAULT NULL,
    `EmployeeId` char(36) DEFAULT NULL,
    `PostId` char(36) DEFAULT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `IsAdmin` tinyint(1) NOT NULL DEFAULT 0,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_sys_user_Username` (`Username`),
    KEY `IX_sys_user_EmployeeId` (`EmployeeId`),
    KEY `IX_sys_user_PostId` (`PostId`),
    CONSTRAINT `FK_sys_user_employee_EmployeeId`
        FOREIGN KEY (`EmployeeId`) REFERENCES `employee` (`Id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sys_user_role` (
    `Id` char(36) NOT NULL,
    `UserId` char(36) NOT NULL,
    `RoleId` char(36) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_sys_user_role_UserId_RoleId` (`UserId`, `RoleId`),
    KEY `IX_sys_user_role_RoleId` (`RoleId`),
    CONSTRAINT `FK_sys_user_role_sys_user_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `sys_user` (`Id`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_sys_user_role_sys_role_RoleId`
        FOREIGN KEY (`RoleId`) REFERENCES `sys_role` (`Id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sys_role_permission` (
    `Id` char(36) NOT NULL,
    `RoleId` char(36) NOT NULL,
    `MenuId` char(36) NOT NULL,
    `PermissionCode` varchar(100) NOT NULL,
    `PermissionType` varchar(50) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_sys_role_permission_RoleId_MenuId` (`RoleId`, `MenuId`),
    KEY `IX_sys_role_permission_MenuId` (`MenuId`),
    KEY `IX_sys_role_permission_PermissionCode` (`PermissionCode`),
    CONSTRAINT `FK_sys_role_permission_sys_role_RoleId`
        FOREIGN KEY (`RoleId`) REFERENCES `sys_role` (`Id`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_sys_role_permission_sys_menu_MenuId`
        FOREIGN KEY (`MenuId`) REFERENCES `sys_menu` (`Id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =========================================================
-- 二、补齐本次角色需要的最小菜单权限（仅在缺失时插入）
-- =========================================================

DROP TEMPORARY TABLE IF EXISTS `tmp_seed_menu`;
CREATE TEMPORARY TABLE `tmp_seed_menu` (
    `MenuKey` varchar(100) NOT NULL,
    `ParentMenuKey` varchar(100) DEFAULT NULL,
    `MenuName` varchar(100) NOT NULL,
    `MenuType` varchar(20) NOT NULL,
    `SortOrder` int NOT NULL,
    `RoutePath` varchar(200) DEFAULT NULL,
    `ComponentPath` varchar(200) DEFAULT NULL,
    `Icon` varchar(50) DEFAULT NULL,
    `IsVisible` tinyint(1) NOT NULL,
    `PermissionCode` varchar(100) NOT NULL,
    PRIMARY KEY (`MenuKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `tmp_seed_menu`
(`MenuKey`, `ParentMenuKey`, `MenuName`, `MenuType`, `SortOrder`, `RoutePath`, `ComponentPath`, `Icon`, `IsVisible`, `PermissionCode`)
VALUES
('dashboard', NULL, '首页', 'page', 1, '/dashboard', 'views/Dashboard.vue', 'HomeOutlined', 1, 'page.dashboard'),

('employee-module', NULL, '员工管理', 'module', 20, NULL, NULL, 'TeamOutlined', 1, 'module.employee'),
('employee-list', 'employee-module', '员工列表', 'page', 21, '/employees', 'views/employee/EmployeeList.vue', NULL, 1, 'page.employee.list'),
('employee-create', 'employee-list', '新增员工', 'button', 211, NULL, NULL, NULL, 0, 'button.employee.create'),
('employee-edit', 'employee-list', '编辑员工', 'button', 212, NULL, NULL, NULL, 0, 'button.employee.edit'),
('employee-import', 'employee-list', '导入员工', 'button', 214, NULL, NULL, NULL, 0, 'button.employee.import'),
('employee-export', 'employee-list', '导出员工', 'button', 215, NULL, NULL, NULL, 0, 'button.employee.export'),
('employee-dismiss', 'employee-list', '办理离职', 'button', 216, NULL, NULL, NULL, 0, 'button.employee.dismiss'),
('employee-reset-password', 'employee-list', '重置密码', 'button', 217, NULL, NULL, NULL, 0, 'button.employee.resetPassword'),

('timesheet-module', NULL, '考勤工时', 'module', 30, NULL, NULL, 'ClockCircleOutlined', 1, 'module.timesheet'),
('timesheet-list', 'timesheet-module', '工时记录', 'page', 31, '/timesheets', 'views/timesheet/TimesheetList.vue', NULL, 1, 'page.timesheet.list'),
('timesheet-import-page', 'timesheet-module', '工时导入', 'page', 32, '/timesheets/import', 'views/timesheet/TimesheetImport.vue', NULL, 1, 'page.timesheet.import'),
('timesheet-create', 'timesheet-list', '新增工时', 'button', 311, NULL, NULL, NULL, 0, 'button.timesheet.create'),
('timesheet-edit', 'timesheet-list', '编辑工时', 'button', 312, NULL, NULL, NULL, 0, 'button.timesheet.edit'),
('timesheet-approve', 'timesheet-list', '审批工时', 'button', 314, NULL, NULL, NULL, 0, 'button.timesheet.approve'),
('timesheet-import', 'timesheet-import-page', '导入工时', 'button', 321, NULL, NULL, NULL, 0, 'button.timesheet.import'),
('timesheet-export', 'timesheet-list', '导出工时', 'button', 322, NULL, NULL, NULL, 0, 'button.timesheet.export'),

('salary-module', NULL, '薪资管理', 'module', 40, NULL, NULL, 'MoneyCollectOutlined', 1, 'module.salary'),
('salary-list', 'salary-module', '薪资列表', 'page', 41, '/salary', 'views/salary/SalaryList.vue', NULL, 1, 'page.salary.list'),
('salary-calculate-page', 'salary-module', '薪资核算', 'page', 42, '/salary/calculate', 'views/salary/SalaryCalculate.vue', NULL, 1, 'page.salary.calculate'),
('salary-calculate', 'salary-calculate-page', '执行核算', 'button', 421, NULL, NULL, NULL, 0, 'button.salary.calculate'),
('salary-approve', 'salary-list', '审批薪资', 'button', 411, NULL, NULL, NULL, 0, 'button.salary.approve'),
('salary-reject', 'salary-list', '驳回薪资', 'button', 412, NULL, NULL, NULL, 0, 'button.salary.reject'),
('salary-export', 'salary-list', '导出薪资', 'button', 413, NULL, NULL, NULL, 0, 'button.salary.export'),
('salary-payslip', 'salary-list', '生成工资单', 'button', 414, NULL, NULL, NULL, 0, 'button.salary.generatePayslip'),
('salary-bank', 'salary-list', '银行代发', 'button', 415, NULL, NULL, NULL, 0, 'button.salary.bankExport'),
('salary-adjust', 'salary-list', '调整薪资', 'button', 416, NULL, NULL, NULL, 0, 'button.salary.adjust'),

('workflow-module', NULL, '协同审批', 'module', 45, NULL, NULL, 'FileTextOutlined', 1, 'module.workflow'),
('todo-center-page', 'workflow-module', '待办中心', 'page', 451, '/workflow/todo', 'views/workflow/TodoCenter.vue', NULL, 1, 'page.todo.center'),
('process-center-page', 'workflow-module', '流程中心', 'page', 452, '/workflow/processes', 'views/workflow/ProcessCenter.vue', NULL, 1, 'page.process.center'),
('process-requests-page', 'workflow-module', '流程申请', 'page', 454, '/workflow/requests', 'views/workflow/ProcessRequests.vue', NULL, 1, 'page.process.requests'),
('todo-complete', 'todo-center-page', '完成待办', 'button', 4511, NULL, NULL, NULL, 0, 'button.todo.complete'),
('todo-reject', 'todo-center-page', '驳回待办', 'button', 4512, NULL, NULL, NULL, 0, 'button.todo.reject'),
('todo-urge', 'todo-center-page', '催办待办', 'button', 4513, NULL, NULL, NULL, 0, 'button.todo.urge'),
('todo-transfer', 'todo-center-page', '转交待办', 'button', 4514, NULL, NULL, NULL, 0, 'button.todo.transfer'),
('process-reject', 'process-center-page', '驳回流程', 'button', 4520, NULL, NULL, NULL, 0, 'button.process.reject'),
('process-terminate', 'process-center-page', '终止流程', 'button', 4521, NULL, NULL, NULL, 0, 'button.process.terminate'),

('reports', NULL, '报表中心', 'page', 50, '/reports', 'views/reports/Reports.vue', 'FileTextOutlined', 1, 'page.reports');

INSERT INTO `sys_menu`
(`Id`, `MenuKey`, `MenuName`, `ParentId`, `MenuType`, `SortOrder`, `RoutePath`, `ComponentPath`, `Icon`, `IsVisible`, `IsActive`, `PermissionCode`, `CreatedAt`, `UpdatedAt`)
SELECT
    UUID(),
    t.`MenuKey`,
    t.`MenuName`,
    (SELECT p.`Id` FROM `sys_menu` p WHERE p.`MenuKey` = t.`ParentMenuKey` LIMIT 1),
    t.`MenuType`,
    t.`SortOrder`,
    t.`RoutePath`,
    t.`ComponentPath`,
    t.`Icon`,
    t.`IsVisible`,
    1,
    t.`PermissionCode`,
    @now,
    @now
FROM `tmp_seed_menu` t
WHERE NOT EXISTS (
    SELECT 1
    FROM `sys_menu` m
    WHERE m.`MenuKey` = t.`MenuKey`
       OR (m.`PermissionCode` IS NOT NULL AND m.`PermissionCode` = t.`PermissionCode`)
)
ORDER BY t.`SortOrder`;

-- =========================================================
-- 三、角色定义（5个角色）
-- =========================================================

INSERT INTO `sys_role`
(`Id`, `RoleCode`, `RoleName`, `Description`, `OrgUnitId`, `IsActive`, `CreatedAt`, `UpdatedAt`)
VALUES
(UUID(), 'SYS_ADMIN', '系统管理员', '拥有系统全部权限，负责平台级配置与运维管理', NULL, 1, @now, @now),
(UUID(), 'HR_SPECIALIST', 'HR专员', '负责员工信息、考勤工时、薪资核算等人事工作', NULL, 1, @now, @now),
(UUID(), 'FINANCE_OFFICER', '财务人员', '负责薪资发放、社保公积金核算及财务报表', NULL, 1, @now, @now),
(UUID(), 'DEPT_MANAGER', '部门经理', '负责本部门员工审批、团队管理与绩效评估', NULL, 1, @now, @now),
(UUID(), 'GENERAL_EMPLOYEE', '普通员工', '仅查看个人薪资相关页面并发起请假等流程申请', NULL, 1, @now, @now)
ON DUPLICATE KEY UPDATE
    `RoleName` = VALUES(`RoleName`),
    `Description` = VALUES(`Description`),
    `IsActive` = VALUES(`IsActive`),
    `UpdatedAt` = VALUES(`UpdatedAt`);

-- 清理这 5 个角色已有权限，按本脚本重新分配
DELETE rp
FROM `sys_role_permission` rp
INNER JOIN `sys_role` r ON r.`Id` = rp.`RoleId`
WHERE r.`RoleCode` IN ('SYS_ADMIN', 'HR_SPECIALIST', 'FINANCE_OFFICER', 'DEPT_MANAGER', 'GENERAL_EMPLOYEE');

-- 系统管理员：拥有当前 sys_menu 中全部有效权限
INSERT INTO `sys_role_permission`
(`Id`, `RoleId`, `MenuId`, `PermissionCode`, `PermissionType`)
SELECT
    UUID(),
    r.`Id`,
    m.`Id`,
    m.`PermissionCode`,
    m.`MenuType`
FROM `sys_role` r
INNER JOIN `sys_menu` m ON m.`IsActive` = 1 AND m.`PermissionCode` IS NOT NULL
WHERE r.`RoleCode` = 'SYS_ADMIN';

DROP TEMPORARY TABLE IF EXISTS `tmp_role_permission`;
CREATE TEMPORARY TABLE `tmp_role_permission` (
    `RoleCode` varchar(100) NOT NULL,
    `PermissionCode` varchar(100) NOT NULL,
    PRIMARY KEY (`RoleCode`, `PermissionCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `tmp_role_permission` (`RoleCode`, `PermissionCode`) VALUES
('HR_SPECIALIST', 'page.dashboard'),
('HR_SPECIALIST', 'module.employee'),
('HR_SPECIALIST', 'page.employee.list'),
('HR_SPECIALIST', 'button.employee.create'),
('HR_SPECIALIST', 'button.employee.edit'),
('HR_SPECIALIST', 'button.employee.import'),
('HR_SPECIALIST', 'button.employee.export'),
('HR_SPECIALIST', 'button.employee.dismiss'),
('HR_SPECIALIST', 'button.employee.resetPassword'),
('HR_SPECIALIST', 'module.timesheet'),
('HR_SPECIALIST', 'page.timesheet.list'),
('HR_SPECIALIST', 'page.timesheet.import'),
('HR_SPECIALIST', 'button.timesheet.create'),
('HR_SPECIALIST', 'button.timesheet.edit'),
('HR_SPECIALIST', 'button.timesheet.approve'),
('HR_SPECIALIST', 'button.timesheet.import'),
('HR_SPECIALIST', 'button.timesheet.export'),
('HR_SPECIALIST', 'module.salary'),
('HR_SPECIALIST', 'page.salary.list'),
('HR_SPECIALIST', 'page.salary.calculate'),
('HR_SPECIALIST', 'button.salary.calculate'),
('HR_SPECIALIST', 'button.salary.export'),
('HR_SPECIALIST', 'button.salary.generatePayslip'),
('HR_SPECIALIST', 'button.salary.adjust'),
('HR_SPECIALIST', 'module.workflow'),
('HR_SPECIALIST', 'page.todo.center'),
('HR_SPECIALIST', 'button.todo.complete'),
('HR_SPECIALIST', 'button.todo.reject'),
('HR_SPECIALIST', 'button.todo.transfer'),

('FINANCE_OFFICER', 'page.dashboard'),
('FINANCE_OFFICER', 'module.timesheet'),
('FINANCE_OFFICER', 'page.timesheet.list'),
('FINANCE_OFFICER', 'module.salary'),
('FINANCE_OFFICER', 'page.salary.list'),
('FINANCE_OFFICER', 'button.salary.approve'),
('FINANCE_OFFICER', 'button.salary.reject'),
('FINANCE_OFFICER', 'button.salary.export'),
('FINANCE_OFFICER', 'button.salary.generatePayslip'),
('FINANCE_OFFICER', 'button.salary.bankExport'),
('FINANCE_OFFICER', 'button.salary.adjust'),
('FINANCE_OFFICER', 'module.workflow'),
('FINANCE_OFFICER', 'page.todo.center'),
('FINANCE_OFFICER', 'page.process.center'),
('FINANCE_OFFICER', 'button.todo.complete'),
('FINANCE_OFFICER', 'button.todo.reject'),
('FINANCE_OFFICER', 'button.todo.urge'),
('FINANCE_OFFICER', 'button.process.reject'),
('FINANCE_OFFICER', 'button.process.terminate'),
('FINANCE_OFFICER', 'page.reports'),

('DEPT_MANAGER', 'page.dashboard'),
('DEPT_MANAGER', 'module.employee'),
('DEPT_MANAGER', 'page.employee.list'),
('DEPT_MANAGER', 'module.timesheet'),
('DEPT_MANAGER', 'page.timesheet.list'),
('DEPT_MANAGER', 'button.timesheet.approve'),
('DEPT_MANAGER', 'module.workflow'),
('DEPT_MANAGER', 'page.todo.center'),
('DEPT_MANAGER', 'page.process.requests'),
('DEPT_MANAGER', 'button.todo.complete'),
('DEPT_MANAGER', 'button.todo.reject'),
('DEPT_MANAGER', 'button.todo.urge'),
('DEPT_MANAGER', 'button.todo.transfer'),

('GENERAL_EMPLOYEE', 'page.dashboard'),
('GENERAL_EMPLOYEE', 'module.salary'),
('GENERAL_EMPLOYEE', 'page.salary.list'),
('GENERAL_EMPLOYEE', 'module.workflow'),
('GENERAL_EMPLOYEE', 'page.process.requests');

INSERT INTO `sys_role_permission`
(`Id`, `RoleId`, `MenuId`, `PermissionCode`, `PermissionType`)
SELECT
    UUID(),
    r.`Id`,
    m.`Id`,
    m.`PermissionCode`,
    m.`MenuType`
FROM `tmp_role_permission` trp
INNER JOIN `sys_role` r ON r.`RoleCode` = trp.`RoleCode`
INNER JOIN `sys_menu` m ON m.`PermissionCode` = trp.`PermissionCode`;

-- =========================================================
-- 四、组织架构与负责人配置
-- 组织级别：0=总部，2=部门
-- =========================================================

INSERT INTO `org_unit`
(`Id`, `Code`, `Name`, `Level`, `ParentId`, `ManagerId`, `IsActive`, `CreatedAt`, `UpdatedAt`)
VALUES
(UUID(), 'HQ', '人力资源管理总部', 0, NULL, NULL, 1, @now, @now)
ON DUPLICATE KEY UPDATE
    `Name` = VALUES(`Name`),
    `Level` = VALUES(`Level`),
    `ParentId` = VALUES(`ParentId`),
    `IsActive` = VALUES(`IsActive`),
    `UpdatedAt` = VALUES(`UpdatedAt`);

SET @org_hq = (SELECT `Id` FROM `org_unit` WHERE `Code` = 'HQ' LIMIT 1);

INSERT INTO `org_unit`
(`Id`, `Code`, `Name`, `Level`, `ParentId`, `ManagerId`, `IsActive`, `CreatedAt`, `UpdatedAt`)
VALUES
(UUID(), 'DEPT-HR', '人力资源部', 2, @org_hq, NULL, 1, @now, @now),
(UUID(), 'DEPT-FIN', '财务部', 2, @org_hq, NULL, 1, @now, @now),
(UUID(), 'DEPT-TECH', '技术部', 2, @org_hq, NULL, 1, @now, @now),
(UUID(), 'DEPT-ADMIN', '行政部', 2, @org_hq, NULL, 1, @now, @now),
(UUID(), 'DEPT-OPS', '运营部', 2, @org_hq, NULL, 1, @now, @now)
ON DUPLICATE KEY UPDATE
    `Name` = VALUES(`Name`),
    `Level` = VALUES(`Level`),
    `ParentId` = VALUES(`ParentId`),
    `IsActive` = VALUES(`IsActive`),
    `UpdatedAt` = VALUES(`UpdatedAt`);

SET @org_hr = (SELECT `Id` FROM `org_unit` WHERE `Code` = 'DEPT-HR' LIMIT 1);
SET @org_fin = (SELECT `Id` FROM `org_unit` WHERE `Code` = 'DEPT-FIN' LIMIT 1);
SET @org_tech = (SELECT `Id` FROM `org_unit` WHERE `Code` = 'DEPT-TECH' LIMIT 1);
SET @org_admin = (SELECT `Id` FROM `org_unit` WHERE `Code` = 'DEPT-ADMIN' LIMIT 1);
SET @org_ops = (SELECT `Id` FROM `org_unit` WHERE `Code` = 'DEPT-OPS' LIMIT 1);

-- =========================================================
-- 五、员工数据（负责人 + 普通员工）
-- =========================================================

INSERT INTO `employee`
(`Id`, `EmployeeNo`, `Name`, `Gender`, `IdCard`, `Phone`, `Email`, `EmployeeType`, `SalaryMode`, `OrgUnitId`,
 `JobTitle`, `Level`, `Tags`, `MonthlySalary`, `SocialSecurityBase`, `HousingFundBase`,
 `ProbationDays`, `ContractType`, `ContractStartDate`, `ContractEndDate`, `HireDate`,
 `IsBlacklisted`, `IsActive`, `CreatedAt`, `UpdatedAt`)
VALUES
(UUID(), 'E0001', '林峻峰', 1, '320101198802150011', '13810000001', 'lin.junfeng@hrms.local', 0, 1, @org_hq, '系统平台主管', 'M4', JSON_ARRAY('系统管理员', '总部'), 26000.00, 26000.00, 26000.00, 30, 1, '2023-01-01 09:00:00', '2027-12-31 18:00:00', '2023-01-01 09:00:00', 0, 1, @now, @now),
(UUID(), 'E1001', '周明轩', 1, '320101198903180021', '13810000011', 'zhou.mingxuan@hrms.local', 0, 1, @org_hr, '人力资源总监', 'M3', JSON_ARRAY('负责人', 'HR'), 20000.00, 20000.00, 20000.00, 30, 1, '2023-02-01 09:00:00', '2027-12-31 18:00:00', '2023-02-01 09:00:00', 0, 1, @now, @now),
(UUID(), 'E1002', '李若彤', 0, '320101199404220022', '13810000012', 'li.ruotong@hrms.local', 0, 1, @org_hr, '招聘专员', 'P3', JSON_ARRAY('普通员工', 'HR'), 9000.00, 9000.00, 9000.00, 30, 1, '2024-03-01 09:00:00', '2027-12-31 18:00:00', '2024-03-01 09:00:00', 0, 1, @now, @now),
(UUID(), 'E1003', '郑博文', 1, '320101199507150023', '13810000013', 'zheng.bowen@hrms.local', 0, 1, @org_hr, '薪酬绩效专员', 'P3', JSON_ARRAY('普通员工', 'HR'), 9800.00, 9800.00, 9800.00, 30, 1, '2024-04-10 09:00:00', '2027-12-31 18:00:00', '2024-04-10 09:00:00', 0, 1, @now, @now),

(UUID(), 'E2001', '孙雅琴', 0, '320101198811120031', '13810000021', 'sun.yaqin@hrms.local', 0, 1, @org_fin, '财务经理', 'M3', JSON_ARRAY('负责人', '财务'), 22000.00, 22000.00, 22000.00, 30, 1, '2023-03-01 09:00:00', '2027-12-31 18:00:00', '2023-03-01 09:00:00', 0, 1, @now, @now),
(UUID(), 'E2002', '陈晓岚', 0, '320101199211080032', '13810000022', 'chen.xiaolan@hrms.local', 0, 1, @org_fin, '薪资会计', 'P3', JSON_ARRAY('普通员工', '财务'), 10800.00, 10800.00, 10800.00, 30, 1, '2024-02-15 09:00:00', '2027-12-31 18:00:00', '2024-02-15 09:00:00', 0, 1, @now, @now),
(UUID(), 'E2003', '蒋文博', 1, '320101199306210033', '13810000023', 'jiang.wenbo@hrms.local', 0, 1, @org_fin, '社保公积金专员', 'P3', JSON_ARRAY('普通员工', '财务'), 10200.00, 10200.00, 10200.00, 30, 1, '2024-05-20 09:00:00', '2027-12-31 18:00:00', '2024-05-20 09:00:00', 0, 1, @now, @now),

(UUID(), 'E3001', '赵立成', 1, '320101198707100041', '13810000031', 'zhao.licheng@hrms.local', 0, 1, @org_tech, '技术部经理', 'M3', JSON_ARRAY('负责人', '技术'), 21000.00, 21000.00, 21000.00, 30, 1, '2023-01-15 09:00:00', '2027-12-31 18:00:00', '2023-01-15 09:00:00', 0, 1, @now, @now),
(UUID(), 'E3002', '刘思源', 1, '320101199508180042', '13810000032', 'liu.siyuan@hrms.local', 0, 1, @org_tech, '后端工程师', 'P4', JSON_ARRAY('普通员工', '技术'), 12500.00, 12500.00, 12500.00, 30, 1, '2024-06-01 09:00:00', '2027-12-31 18:00:00', '2024-06-01 09:00:00', 0, 1, @now, @now),
(UUID(), 'E3003', '何嘉宁', 0, '320101199609250043', '13810000033', 'he.jianing@hrms.local', 0, 1, @org_tech, '前端工程师', 'P4', JSON_ARRAY('普通员工', '技术'), 11800.00, 11800.00, 11800.00, 30, 1, '2024-07-10 09:00:00', '2027-12-31 18:00:00', '2024-07-10 09:00:00', 0, 1, @now, @now),

(UUID(), 'E4001', '钱雯婷', 0, '320101198910050051', '13810000041', 'qian.wenting@hrms.local', 0, 1, @org_admin, '行政部经理', 'M2', JSON_ARRAY('负责人', '行政'), 16500.00, 16500.00, 16500.00, 30, 1, '2023-04-01 09:00:00', '2027-12-31 18:00:00', '2023-04-01 09:00:00', 0, 1, @now, @now),
(UUID(), 'E4002', '唐雨欣', 0, '320101199702140052', '13810000042', 'tang.yuxin@hrms.local', 0, 1, @org_admin, '行政专员', 'P2', JSON_ARRAY('普通员工', '行政'), 8200.00, 8200.00, 8200.00, 30, 1, '2024-03-12 09:00:00', '2027-12-31 18:00:00', '2024-03-12 09:00:00', 0, 1, @now, @now),
(UUID(), 'E4003', '孟子航', 1, '320101199611300053', '13810000043', 'meng.zihang@hrms.local', 0, 1, @org_admin, '后勤专员', 'P2', JSON_ARRAY('普通员工', '行政'), 7800.00, 7800.00, 7800.00, 30, 1, '2024-04-08 09:00:00', '2027-12-31 18:00:00', '2024-04-08 09:00:00', 0, 1, @now, @now),

(UUID(), 'E5001', '吴承泽', 1, '320101198805170061', '13810000051', 'wu.chengze@hrms.local', 0, 1, @org_ops, '运营部经理', 'M2', JSON_ARRAY('负责人', '运营'), 17500.00, 17500.00, 17500.00, 30, 1, '2023-05-06 09:00:00', '2027-12-31 18:00:00', '2023-05-06 09:00:00', 0, 1, @now, @now),
(UUID(), 'E5002', '方可心', 0, '320101199703280062', '13810000052', 'fang.kexin@hrms.local', 0, 1, @org_ops, '运营专员', 'P3', JSON_ARRAY('普通员工', '运营'), 8800.00, 8800.00, 8800.00, 30, 1, '2024-05-08 09:00:00', '2027-12-31 18:00:00', '2024-05-08 09:00:00', 0, 1, @now, @now),
(UUID(), 'E5003', '罗宇晨', 1, '320101199408110063', '13810000053', 'luo.yuchen@hrms.local', 0, 1, @org_ops, '数据运营专员', 'P3', JSON_ARRAY('普通员工', '运营'), 9200.00, 9200.00, 9200.00, 30, 1, '2024-06-18 09:00:00', '2027-12-31 18:00:00', '2024-06-18 09:00:00', 0, 1, @now, @now)
ON DUPLICATE KEY UPDATE
    `Name` = VALUES(`Name`),
    `Gender` = VALUES(`Gender`),
    `IdCard` = VALUES(`IdCard`),
    `Phone` = VALUES(`Phone`),
    `Email` = VALUES(`Email`),
    `EmployeeType` = VALUES(`EmployeeType`),
    `SalaryMode` = VALUES(`SalaryMode`),
    `OrgUnitId` = VALUES(`OrgUnitId`),
    `JobTitle` = VALUES(`JobTitle`),
    `Level` = VALUES(`Level`),
    `Tags` = VALUES(`Tags`),
    `MonthlySalary` = VALUES(`MonthlySalary`),
    `SocialSecurityBase` = VALUES(`SocialSecurityBase`),
    `HousingFundBase` = VALUES(`HousingFundBase`),
    `ContractType` = VALUES(`ContractType`),
    `ContractStartDate` = VALUES(`ContractStartDate`),
    `ContractEndDate` = VALUES(`ContractEndDate`),
    `HireDate` = VALUES(`HireDate`),
    `IsBlacklisted` = VALUES(`IsBlacklisted`),
    `IsActive` = VALUES(`IsActive`),
    `UpdatedAt` = VALUES(`UpdatedAt`);

SET @emp_sys_admin = (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E0001' LIMIT 1);
SET @emp_hr_head   = (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E1001' LIMIT 1);
SET @emp_fin_head  = (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E2001' LIMIT 1);
SET @emp_tech_head = (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E3001' LIMIT 1);
SET @emp_admin_head= (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E4001' LIMIT 1);
SET @emp_ops_head  = (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E5001' LIMIT 1);

UPDATE `org_unit` SET `ManagerId` = CAST(@emp_hr_head AS CHAR(36)), `UpdatedAt` = @now WHERE `Code` = 'DEPT-HR';
UPDATE `org_unit` SET `ManagerId` = CAST(@emp_fin_head AS CHAR(36)), `UpdatedAt` = @now WHERE `Code` = 'DEPT-FIN';
UPDATE `org_unit` SET `ManagerId` = CAST(@emp_tech_head AS CHAR(36)), `UpdatedAt` = @now WHERE `Code` = 'DEPT-TECH';
UPDATE `org_unit` SET `ManagerId` = CAST(@emp_admin_head AS CHAR(36)), `UpdatedAt` = @now WHERE `Code` = 'DEPT-ADMIN';
UPDATE `org_unit` SET `ManagerId` = CAST(@emp_ops_head AS CHAR(36)), `UpdatedAt` = @now WHERE `Code` = 'DEPT-OPS';

-- =========================================================
-- 六、系统账号
-- 默认密码统一为：123456
-- =========================================================

INSERT INTO `sys_user`
(`Id`, `Username`, `Password`, `Name`, `Phone`, `Email`, `EmployeeId`, `PostId`, `IsActive`, `IsAdmin`, `CreatedAt`, `UpdatedAt`)
VALUES
(UUID(), 'sysadmin', '123456', '林峻峰', '13810000001', 'lin.junfeng@hrms.local', @emp_sys_admin, NULL, 1, 1, @now, @now),
(UUID(), 'hr.head', '123456', '周明轩', '13810000011', 'zhou.mingxuan@hrms.local', @emp_hr_head, NULL, 1, 0, @now, @now),
(UUID(), 'hr.staff01', '123456', '李若彤', '13810000012', 'li.ruotong@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E1002' LIMIT 1), NULL, 1, 0, @now, @now),
(UUID(), 'hr.staff02', '123456', '郑博文', '13810000013', 'zheng.bowen@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E1003' LIMIT 1), NULL, 1, 0, @now, @now),

(UUID(), 'finance.head', '123456', '孙雅琴', '13810000021', 'sun.yaqin@hrms.local', @emp_fin_head, NULL, 1, 0, @now, @now),
(UUID(), 'finance.staff01', '123456', '陈晓岚', '13810000022', 'chen.xiaolan@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E2002' LIMIT 1), NULL, 1, 0, @now, @now),
(UUID(), 'finance.staff02', '123456', '蒋文博', '13810000023', 'jiang.wenbo@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E2003' LIMIT 1), NULL, 1, 0, @now, @now),

(UUID(), 'tech.head', '123456', '赵立成', '13810000031', 'zhao.licheng@hrms.local', @emp_tech_head, NULL, 1, 0, @now, @now),
(UUID(), 'tech.staff01', '123456', '刘思源', '13810000032', 'liu.siyuan@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E3002' LIMIT 1), NULL, 1, 0, @now, @now),
(UUID(), 'tech.staff02', '123456', '何嘉宁', '13810000033', 'he.jianing@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E3003' LIMIT 1), NULL, 1, 0, @now, @now),

(UUID(), 'admin.head', '123456', '钱雯婷', '13810000041', 'qian.wenting@hrms.local', @emp_admin_head, NULL, 1, 0, @now, @now),
(UUID(), 'admin.staff01', '123456', '唐雨欣', '13810000042', 'tang.yuxin@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E4002' LIMIT 1), NULL, 1, 0, @now, @now),
(UUID(), 'admin.staff02', '123456', '孟子航', '13810000043', 'meng.zihang@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E4003' LIMIT 1), NULL, 1, 0, @now, @now),

(UUID(), 'ops.head', '123456', '吴承泽', '13810000051', 'wu.chengze@hrms.local', @emp_ops_head, NULL, 1, 0, @now, @now),
(UUID(), 'ops.staff01', '123456', '方可心', '13810000052', 'fang.kexin@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E5002' LIMIT 1), NULL, 1, 0, @now, @now),
(UUID(), 'ops.staff02', '123456', '罗宇晨', '13810000053', 'luo.yuchen@hrms.local', (SELECT `Id` FROM `employee` WHERE `EmployeeNo` = 'E5003' LIMIT 1), NULL, 1, 0, @now, @now)
ON DUPLICATE KEY UPDATE
    `Password` = VALUES(`Password`),
    `Name` = VALUES(`Name`),
    `Phone` = VALUES(`Phone`),
    `Email` = VALUES(`Email`),
    `EmployeeId` = VALUES(`EmployeeId`),
    `IsActive` = VALUES(`IsActive`),
    `IsAdmin` = VALUES(`IsAdmin`),
    `UpdatedAt` = VALUES(`UpdatedAt`);

-- =========================================================
-- 七、为负责人和普通员工分配角色
-- HR / 财务负责人同时兼任“部门经理”
-- =========================================================

DELETE ur
FROM `sys_user_role` ur
INNER JOIN `sys_user` u ON u.`Id` = ur.`UserId`
WHERE u.`Username` IN (
    'sysadmin',
    'hr.head', 'hr.staff01', 'hr.staff02',
    'finance.head', 'finance.staff01', 'finance.staff02',
    'tech.head', 'tech.staff01', 'tech.staff02',
    'admin.head', 'admin.staff01', 'admin.staff02',
    'ops.head', 'ops.staff01', 'ops.staff02'
);

DROP TEMPORARY TABLE IF EXISTS `tmp_user_role`;
CREATE TEMPORARY TABLE `tmp_user_role` (
    `Username` varchar(100) NOT NULL,
    `RoleCode` varchar(100) NOT NULL,
    PRIMARY KEY (`Username`, `RoleCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `tmp_user_role` (`Username`, `RoleCode`) VALUES
('sysadmin', 'SYS_ADMIN'),

('hr.head', 'HR_SPECIALIST'),
('hr.head', 'DEPT_MANAGER'),
('finance.head', 'FINANCE_OFFICER'),
('finance.head', 'DEPT_MANAGER'),
('tech.head', 'DEPT_MANAGER'),
('admin.head', 'DEPT_MANAGER'),
('ops.head', 'DEPT_MANAGER'),

('hr.staff01', 'GENERAL_EMPLOYEE'),
('hr.staff02', 'GENERAL_EMPLOYEE'),
('finance.staff01', 'GENERAL_EMPLOYEE'),
('finance.staff02', 'GENERAL_EMPLOYEE'),
('tech.staff01', 'GENERAL_EMPLOYEE'),
('tech.staff02', 'GENERAL_EMPLOYEE'),
('admin.staff01', 'GENERAL_EMPLOYEE'),
('admin.staff02', 'GENERAL_EMPLOYEE'),
('ops.staff01', 'GENERAL_EMPLOYEE'),
('ops.staff02', 'GENERAL_EMPLOYEE');

INSERT INTO `sys_user_role`
(`Id`, `UserId`, `RoleId`)
SELECT
    UUID(),
    u.`Id`,
    r.`Id`
FROM `tmp_user_role` tur
INNER JOIN `sys_user` u ON u.`Username` = tur.`Username`
INNER JOIN `sys_role` r ON r.`RoleCode` = tur.`RoleCode`;

COMMIT;

SET FOREIGN_KEY_CHECKS = 1;

-- =========================================================
-- 八、验证查询：各部门、负责人及负责人角色
-- =========================================================

SELECT
    ou.`Code` AS `部门编码`,
    ou.`Name` AS `部门名称`,
    e.`EmployeeNo` AS `负责人编号`,
    e.`Name` AS `负责人姓名`,
    su.`Username` AS `负责人账号`,
    GROUP_CONCAT(DISTINCT sr.`RoleName` ORDER BY sr.`RoleName` SEPARATOR '、') AS `负责人角色`
FROM `org_unit` ou
LEFT JOIN `employee` e
    ON CAST(e.`Id` AS CHAR(36)) = ou.`ManagerId`
LEFT JOIN `sys_user` su
    ON su.`EmployeeId` = e.`Id`
LEFT JOIN `sys_user_role` sur
    ON sur.`UserId` = su.`Id`
LEFT JOIN `sys_role` sr
    ON sr.`Id` = sur.`RoleId`
WHERE ou.`Code` IN ('DEPT-HR', 'DEPT-FIN', 'DEPT-TECH', 'DEPT-ADMIN', 'DEPT-OPS')
GROUP BY ou.`Id`, ou.`Code`, ou.`Name`, e.`EmployeeNo`, e.`Name`, su.`Username`
ORDER BY ou.`Code`;
