# 前端页面目录清单

## 项目结构

```
src/
├── api/                    # API接口模块
│   ├── index.js            # 接口定义
│   └── request.js          # 请求封装
├── router/                 # 路由配置
│   └── index.js            # 路由定义
├── store/                  # 状态管理
│   └── user.js             # 用户状态
├── styles/                 # 全局样式
│   └── common.css          # 通用样式
├── views/                  # 页面视图
│   ├── employee/           # 员工管理
│   │   └── EmployeeList.vue    # 员工列表
│   ├── org/                # 组织管理
│   │   └── OrgUnitList.vue     # 组织单元列表
│   ├── salary/             # 薪资管理
│   │   ├── SalaryCalculate.vue  # 薪资核算
│   │   └── SalaryList.vue      # 薪资列表
│   ├── timesheet/          # 工时管理
│   │   ├── TimesheetImport.vue  # 工时导入
│   │   └── TimesheetList.vue    # 工时列表
│   ├── Dashboard.vue       # 仪表盘首页
│   ├── Layout.vue          # 布局组件
│   └── Login.vue           # 登录页面
├── App.vue                 # 根组件
└── main.js                 # 入口文件
```

## 页面清单

| 模块 | 页面名称 | 文件路径 | 功能描述 |
|------|----------|----------|----------|
| 公共 | 登录页面 | views/Login.vue | 用户登录验证 |
| 公共 | 布局组件 | views/Layout.vue | 页面布局框架 |
| 公共 | 仪表盘 | views/Dashboard.vue | 系统首页仪表盘 |
| 组织管理 | 组织单元列表 | views/org/OrgUnitList.vue | 组织架构管理 |
| 员工管理 | 员工列表 | views/employee/EmployeeList.vue | 员工信息管理 |
| 工时管理 | 工时列表 | views/timesheet/TimesheetList.vue | 工时记录查询 |
| 工时管理 | 工时导入 | views/timesheet/TimesheetImport.vue | 工时批量导入 |
| 薪资管理 | 薪资列表 | views/salary/SalaryList.vue | 薪资记录查询 |
| 薪资管理 | 薪资核算 | views/salary/SalaryCalculate.vue | 薪资计算处理 |

## 路由配置

| 路由路径 | 页面组件 | 权限要求 |
|----------|----------|----------|
| /login | Login.vue | 无需登录 |
| / | Dashboard.vue | 需要登录 |
| /dashboard | Dashboard.vue | 需要登录 |
| /org-units | OrgUnitList.vue | 需要登录 |
| /employees | EmployeeList.vue | 需要登录 |
| /timesheets | TimesheetList.vue | 需要登录 |
| /timesheets/import | TimesheetImport.vue | 需要登录 |
| /salary | SalaryList.vue | 需要登录 |
| /salary/calculate | SalaryCalculate.vue | 需要登录 |

## API接口模块

| 模块 | 接口文件 | 功能说明 |
|------|----------|----------|
| 组织单元 | orgUnitApi | 组织单元CRUD操作 |
| 员工 | employeeApi | 员工信息CRUD操作 |
| 工时 | timesheetApi | 工时记录管理 |
| 薪资 | salaryApi | 薪资核算相关 |

## 技术栈

- **框架**: Vue 3 + Composition API
- **路由**: Vue Router 4
- **状态管理**: Pinia
- **UI组件**: Ant Design Vue
- **构建工具**: Vite
- **HTTP客户端**: Axios

## 开发环境

- 开发服务器: http://localhost:3000
- 生产构建: npm run build
- 开发模式: npm run dev
