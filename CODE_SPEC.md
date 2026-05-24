# HRMS 人力资源管理系统 - 代码规格说明书

## 1. 概述

本文档定义了 HRMS 人力资源管理系统的代码规范和开发标准，旨在确保代码质量、可维护性和团队协作效率。

---

## 2. 架构设计

### 2.1 后端架构（分层架构）

```
┌─────────────────────────────────────────────────────────────┐
│                    HRMS.API (表示层)                         │
│  - Controllers (REST API控制层)                              │
│  - Middleware (中间件)                                       │
├─────────────────────────────────────────────────────────────┤
│                 HRMS.Application (应用层)                    │
│  - Services (业务服务层)                                     │
│  - DTOs (数据传输对象)                                       │
│  - Mappers (对象映射)                                        │
├─────────────────────────────────────────────────────────────┤
│                   HRMS.Domain (领域层)                       │
│  - Entities (实体模型)                                       │
│  - Interfaces (仓储接口)                                     │
│  - Enums (枚举定义)                                          │
│  - Exceptions (自定义异常)                                   │
├─────────────────────────────────────────────────────────────┤
│              HRMS.Infrastructure (基础设施层)                │
│  - Data (数据库上下文)                                       │
│  - Repositories (仓储实现)                                   │
│  - Migrations (数据库迁移)                                   │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 前端架构

```
┌─────────────────────────────────────────────────────────────┐
│                      views/ (页面层)                        │
│  - 页面组件 (.vue)                                          │
├─────────────────────────────────────────────────────────────┤
│                     components/ (组件层)                    │
│  - 公共组件 (.vue)                                          │
├─────────────────────────────────────────────────────────────┤
│                        api/ (接口层)                        │
│  - API接口定义                                               │
│  - 请求封装                                                 │
├─────────────────────────────────────────────────────────────┤
│                       store/ (状态层)                       │
│  - Pinia状态管理                                            │
├─────────────────────────────────────────────────────────────┤
│                      router/ (路由层)                       │
│  - 路由配置                                                 │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. 命名规范

### 3.1 后端命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 命名空间 | PascalCase | HRMS.Application.Services |
| 类名 | PascalCase | EmployeeService |
| 方法名 | PascalCase | GetByIdAsync |
| 字段名 | camelCase | _employeeRepository |
| 属性名 | PascalCase | EmployeeNo |
| 变量名 | camelCase | employeeDto |
| 接口名 | I + PascalCase | IEmployeeRepository |
| 枚举名 | PascalCase | EmployeeType |
| 枚举值 | PascalCase | Internal, ThirdParty |

### 3.2 前端命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 组件文件 | PascalCase | EmployeeList.vue |
| 变量名 | camelCase | employeeList |
| 方法名 | camelCase | loadData() |
| 常量名 | UPPER_CASE_SNAKE | API_BASE_URL |
| props属性 | camelCase | orgUnitId |
| emit事件 | kebab-case | update:modelValue |
| ref引用 | camelCase | formRef |

### 3.3 数据库命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 表名 | snake_case | org_unit |
| 字段名 | snake_case | employee_no |
| 主键字段 | id | id |
| 外键字段 | {table}_id | org_unit_id |
| 索引名 | idx_{table}_{column} | idx_employee_no |
| 约束名 | {table}_ibfk_{n} | employee_ibfk_1 |

---

## 4. 代码风格

### 4.1 C# 代码风格

```csharp
// 接口定义
public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(Guid id);
}

// 服务层
public class EmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(MapToDto);
    }
}

// DTO定义
public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

### 4.2 Vue 3 代码风格

```vue
<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { employeeApi } from '../../api'

const loading = ref(false)
const data = ref([])

const loadData = async () => {
  loading.value = true
  try {
    const result = await employeeApi.getAll()
    data.value = result || []
  } catch (error) {
    message.error('加载失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadData()
})
</script>
```

---

## 5. API 规范

### 5.1 基础路径

```
/api/v1/{resource}
```

### 5.2 HTTP 方法约定

| 方法 | 操作 | 示例 |
|------|------|------|
| GET | 查询列表 | GET /api/v1/employees |
| GET | 查询单个 | GET /api/v1/employees/{id} |
| POST | 创建 | POST /api/v1/employees |
| PUT | 更新 | PUT /api/v1/employees |
| DELETE | 删除 | DELETE /api/v1/employees/{id} |

### 5.3 响应格式

```json
{
  "code": 200,
  "message": "success",
  "data": {}
}
```

### 5.4 错误响应

```json
{
  "code": 404,
  "message": "员工不存在",
  "data": null
}
```

### 5.5 状态码约定

| 状态码 | 含义 |
|--------|------|
| 200 | 成功 |
| 400 | 请求参数错误 |
| 401 | 未授权 |
| 403 | 禁止访问 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

---

## 6. 数据库规范

### 6.1 实体设计原则

1. **主键**: 使用 Guid 类型，字段名为 `Id`
2. **必填字段**: 使用 `IsRequired()` 约束
3. **字符串长度**: 明确指定最大长度
4. **枚举类型**: 使用 `int` 类型存储
5. **日期时间**: 使用 `DateTime` 类型，时区统一为 UTC

### 6.2 实体示例

```csharp
public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EmployeeType EmployeeType { get; set; }
    public Guid OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### 6.3 EF Core 配置示例

```csharp
modelBuilder.Entity<Employee>(entity =>
{
    entity.ToTable("employee");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.EmployeeNo).HasMaxLength(50).IsRequired();
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
    entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId);
});
```

---

## 7. 安全规范

### 7.1 认证授权

- 使用 JWT Token 进行认证
- Token 存储在 HTTP Only Cookie 或 Authorization Header
- 接口使用 `[Authorize]` 属性保护

### 7.2 输入验证

- 使用 DataAnnotations 进行模型验证
- 使用 `[ApiController]` 自动返回验证错误
- 前端提交前进行表单验证

### 7.3 敏感数据

- 密码使用 BCrypt 或类似算法加密存储
- 身份证号、手机号等敏感信息加密存储
- 日志中不记录敏感数据

---

## 8. 日志规范

### 8.1 日志级别

| 级别 | 使用场景 |
|------|----------|
| Trace | 详细调试信息 |
| Debug | 开发调试信息 |
| Information | 业务流程记录 |
| Warning | 潜在问题警告 |
| Error | 错误信息 |
| Critical | 严重错误 |

### 8.2 日志格式

```csharp
_logger.LogInformation("员工创建成功: {EmployeeNo}", employeeNo);
_logger.LogError(ex, "员工创建失败: {EmployeeNo}", employeeNo);
```

---

## 9. 异常处理

### 9.1 异常类型

| 异常类型 | 使用场景 |
|----------|----------|
| NotFoundException | 资源不存在 |
| ValidationException | 验证失败 |
| BusinessException | 业务规则违反 |
| UnauthorizedAccessException | 未授权访问 |

### 9.2 全局异常处理

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 处理异常并返回统一格式响应
    }
}
```

---

## 10. 单元测试规范

### 10.1 测试命名

```csharp
[TestClass]
public class EmployeeServiceTests
{
    [TestMethod]
    public async Task GetAllAsync_ShouldReturnAllEmployees()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

### 10.2 测试覆盖

- 核心业务逻辑必须有单元测试
- 边界条件必须测试
- 使用 Moq 进行依赖模拟

---

## 11. 代码审查标准

### 11.1 审查要点

1. **代码正确性**: 逻辑是否正确
2. **代码可读性**: 命名是否清晰，注释是否充分
3. **代码风格**: 是否符合本规范
4. **性能考虑**: 是否有性能问题
5. **安全性**: 是否存在安全漏洞
6. **可测试性**: 是否易于测试

### 11.2 PR 审查流程

1. 自动构建验证
2. 单元测试通过
3. 至少一位同事审查
4. 代码覆盖率达标

---

## 附录：常用命令

### 后端命令

```bash
# 启动开发服务器
dotnet run --project src/HRMS.API

# 构建项目
dotnet build

# 运行测试
dotnet test

# 添加数据库迁移
dotnet ef migrations add InitialCreate --project src/HRMS.Infrastructure --startup-project src/HRMS.API

# 更新数据库
dotnet ef database update --project src/HRMS.Infrastructure --startup-project src/HRMS.API
```

### 前端命令

```bash
# 启动开发服务器
npm run dev

# 构建生产版本
npm run build

# 代码检查
npm run lint

# 预览生产构建
npm run preview
```

---

**版本**: v1.0  
**创建日期**: 2026-05-22  
**适用范围**: HRMS 人力资源管理系统
