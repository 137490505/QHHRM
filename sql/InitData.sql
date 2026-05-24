-- 初始化数据脚本（只处理存在的表）

-- 清空现有数据
DELETE FROM third_party_bill_detail;
DELETE FROM payslip;
DELETE FROM salary_calculation;
DELETE FROM timesheet;
DELETE FROM output_record;
DELETE FROM product;
DELETE FROM expense_application;
DELETE FROM employee;
DELETE FROM third_party_bill;
DELETE FROM sys_role_permission;
DELETE FROM sys_role;

-- 重新插入组织单元数据
INSERT INTO org_unit (Id, Code, Name, Level, ParentId, IsActive) VALUES 
('550e8400-e29b-41d4-a716-446655440000', 'COMPANY', '总公司', 1, NULL, 1),
('550e8400-e29b-41d4-a716-446655440001', 'HR', '人事部', 2, '550e8400-e29b-41d4-a716-446655440000', 1),
('550e8400-e29b-41d4-a716-446655440002', 'FIN', '财务部', 2, '550e8400-e29b-41d4-a716-446655440000', 1),
('550e8400-e29b-41d4-a716-446655440003', 'TECH', '技术部', 2, '550e8400-e29b-41d4-a716-446655440000', 1),
('550e8400-e29b-41d4-a716-446655440004', 'SALES', '销售部', 2, '550e8400-e29b-41d4-a716-446655440000', 1),
('550e8400-e29b-41d4-a716-446655440005', 'OPER', '运营部', 2, '550e8400-e29b-41d4-a716-446655440000', 1);

-- 插入系统角色
INSERT INTO sys_role (Id, Name, Code, Description, IsActive) VALUES 
('770e8400-e29b-41d4-a716-446655440000', '超级管理员', 'SUPER_ADMIN', '系统最高权限', 1),
('770e8400-e29b-41d4-a716-446655440001', '管理员', 'ADMIN', '管理权限', 1),
('770e8400-e29b-41d4-a716-446655440002', '普通用户', 'USER', '普通权限', 1);

-- 插入系统角色权限
INSERT INTO sys_role_permission (Id, RoleId, PermissionCode) VALUES 
('880e8400-e29b-41d4-a716-446655440000', '770e8400-e29b-41d4-a716-446655440000', 'org:manage'),
('880e8400-e29b-41d4-a716-446655440001', '770e8400-e29b-41d4-a716-446655440000', 'employee:manage'),
('880e8400-e29b-41d4-a716-446655440002', '770e8400-e29b-41d4-a716-446655440000', 'timesheet:manage'),
('880e8400-e29b-41d4-a716-446655440003', '770e8400-e29b-41d4-a716-446655440000', 'salary:manage'),
('880e8400-e29b-41d4-a716-446655440004', '770e8400-e29b-41d4-a716-446655440000', 'bill:manage');

-- 插入员工数据
INSERT INTO employee (Id, EmployeeNo, Name, IdCard, Phone, Email, OrgUnitId, EmployeeType, SalaryMode, MonthlySalary, HourlyRate, TrialEndDate, IsActive) VALUES 
('990e8400-e29b-41d4-a716-446655440000', 'EMP001', '张三', '110101199001011234', '13900139001', 'zhangsan@hrms.com', '550e8400-e29b-41d4-a716-446655440001', 0, 1, 8000.00, 50.00, '2026-06-01', 1),
('990e8400-e29b-41d4-a716-446655440001', 'EMP002', '李四', '110101199102022345', '13900139002', 'lisi@hrms.com', '550e8400-e29b-41d4-a716-446655440002', 0, 1, 9000.00, 55.00, '2026-06-01', 1),
('990e8400-e29b-41d4-a716-446655440002', 'EMP003', '王五', '110101199203033456', '13900139003', 'wangwu@hrms.com', '550e8400-e29b-41d4-a716-446655440003', 0, 0, 0.00, 30.00, '2026-06-01', 1),
('990e8400-e29b-41d4-a716-446655440003', 'EMP004', '赵六', '110101199304044567', '13900139004', 'zhaoliu@hrms.com', '550e8400-e29b-41d4-a716-446655440004', 0, 2, 0.00, 2.00, '2026-06-01', 1),
('990e8400-e29b-41d4-a716-446655440004', 'EMP005', '钱七', '110101199405055678', '13900139005', 'qianqi@hrms.com', '550e8400-e29b-41d4-a716-446655440005', 1, 0, 0.00, 25.00, '2026-06-01', 1);

-- 插入产品数据
INSERT INTO product (Id, Code, Name, Description, UnitPrice, OrgUnitId, IsActive) VALUES 
('a00e8400-e29b-41d4-a716-446655440000', 'PROD001', '基础服务', '基础人事服务', 100.00, '550e8400-e29b-41d4-a716-446655440001', 1),
('a00e8400-e29b-41d4-a716-446655440001', 'PROD002', '薪资服务', '薪资核算服务', 200.00, '550e8400-e29b-41d4-a716-446655440002', 1),
('a00e8400-e29b-41d4-a716-446655440002', 'PROD003', '考勤服务', '考勤管理服务', 150.00, '550e8400-e29b-41d4-a716-446655440003', 1);

-- 插入工时记录
INSERT INTO timesheet (Id, EmployeeId, Date, RegularHours, OvertimeHours, WorkType, Status, ActualOrgUnitId) VALUES 
('b00e8400-e29b-41d4-a716-446655440000', '990e8400-e29b-41d4-a716-446655440000', '2026-05-20', 8.00, 2.00, 0, 1, '550e8400-e29b-41d4-a716-446655440001'),
('b00e8400-e29b-41d4-a716-446655440001', '990e8400-e29b-41d4-a716-446655440001', '2026-05-20', 8.00, 0.00, 0, 1, '550e8400-e29b-41d4-a716-446655440002'),
('b00e8400-e29b-41d4-a716-446655440002', '990e8400-e29b-41d4-a716-446655440002', '2026-05-20', 8.00, 1.50, 0, 1, '550e8400-e29b-41d4-a716-446655440003'),
('b00e8400-e29b-41d4-a716-446655440003', '990e8400-e29b-41d4-a716-446655440003', '2026-05-20', 8.00, 0.00, 1, 1, '550e8400-e29b-41d4-a716-446655440004'),
('b00e8400-e29b-41d4-a716-446655440004', '990e8400-e29b-41d4-a716-446655440004', '2026-05-20', 8.00, 3.00, 0, 1, '550e8400-e29b-41d4-a716-446655440005');

-- 插入薪资核算记录
INSERT INTO salary_calculation (Id, EmployeeId, PayPeriod, GrossAmount, NetAmount, Status, CalculatedAt) VALUES 
('c00e8400-e29b-41d4-a716-446655440000', '990e8400-e29b-41d4-a716-446655440000', '2026-05', 8500.00, 7800.00, 1, NOW()),
('c00e8400-e29b-41d4-a716-446655440001', '990e8400-e29b-41d4-a716-446655440001', '2026-05', 9000.00, 8200.00, 1, NOW()),
('c00e8400-e29b-41d4-a716-446655440002', '990e8400-e29b-41d4-a716-446655440002', '2026-05', 5450.00, 4900.00, 1, NOW());

-- 插入工资条记录
INSERT INTO payslip (Id, EmployeeId, SalaryCalculationId, PayDate, Amount) VALUES 
('d00e8400-e29b-41d4-a716-446655440000', '990e8400-e29b-41d4-a716-446655440000', 'c00e8400-e29b-41d4-a716-446655440000', '2026-05-31', 7800.00),
('d00e8400-e29b-41d4-a716-446655440001', '990e8400-e29b-41d4-a716-446655440001', 'c00e8400-e29b-41d4-a716-446655440001', '2026-05-31', 8200.00),
('d00e8400-e29b-41d4-a716-446655440002', '990e8400-e29b-41d4-a716-446655440002', 'c00e8400-e29b-41d4-a716-446655440002', '2026-05-31', 4900.00);

-- 插入第三方对账单
INSERT INTO third_party_bill (Id, ClientId, BillPeriod, TotalAmount, Status, GeneratedAt) VALUES 
('e00e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440005', '2026-05', 15000.00, 1, NOW());

-- 插入第三方对账单明细
INSERT INTO third_party_bill_detail (Id, ThirdPartyBillId, EmployeeId, Hours, Amount) VALUES 
('f00e8400-e29b-41d4-a716-446655440000', 'e00e8400-e29b-41d4-a716-446655440000', '990e8400-e29b-41d4-a716-446655440004', 160.00, 4000.00);

-- 输出统计
SELECT '=== 数据初始化完成 ===' AS result;
SELECT '组织单元' AS table_name, COUNT(*) AS count FROM org_unit
UNION ALL
SELECT '系统角色' AS table_name, COUNT(*) AS count FROM sys_role
UNION ALL
SELECT '角色权限' AS table_name, COUNT(*) AS count FROM sys_role_permission
UNION ALL
SELECT '员工' AS table_name, COUNT(*) AS count FROM employee
UNION ALL
SELECT '产品' AS table_name, COUNT(*) AS count FROM product
UNION ALL
SELECT '工时记录' AS table_name, COUNT(*) AS count FROM timesheet
UNION ALL
SELECT '薪资核算' AS table_name, COUNT(*) AS count FROM salary_calculation
UNION ALL
SELECT '工资条' AS table_name, COUNT(*) AS count FROM payslip
UNION ALL
SELECT '第三方对账单' AS table_name, COUNT(*) AS count FROM third_party_bill
UNION ALL
SELECT '对账单明细' AS table_name, COUNT(*) AS count FROM third_party_bill_detail;