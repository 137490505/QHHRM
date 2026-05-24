import fs from 'node:fs'
import path from 'node:path'
import mysql from 'mysql2/promise'

const workspaceRoot = path.resolve(process.cwd(), '..')
const backendAppSettingsPath = path.join(workspaceRoot, 'src', 'HRMS.API', 'appsettings.json')
const testDatabaseName = 'HRMS_E2E'

export function getBackendConnectionString() {
  const appSettings = JSON.parse(fs.readFileSync(backendAppSettingsPath, 'utf8'))
  return appSettings.ConnectionStrings.DefaultConnection
}

export function toTestConnectionString(connectionString) {
  if (/Database=/i.test(connectionString)) {
    return connectionString.replace(/Database=([^;]+);/i, `Database=${testDatabaseName};`)
  }

  return `${connectionString};Database=${testDatabaseName};`
}

export function parseConnectionString(connectionString) {
  const map = {}

  for (const segment of connectionString.split(';')) {
    if (!segment.trim()) {
      continue
    }

    const separatorIndex = segment.indexOf('=')
    if (separatorIndex <= 0) {
      continue
    }

    const key = segment.slice(0, separatorIndex).trim().toLowerCase()
    const value = segment.slice(separatorIndex + 1).trim()
    map[key] = value
  }

  return {
    host: map.server || map.host || 'localhost',
    port: map.port ? Number(map.port) : 3306,
    user: map.user || map.uid || map.username || 'root',
    password: map.password || map.pwd || '',
    database: map.database || map.initialcatalog || undefined
  }
}

export async function ensureTestDatabase() {
  const connectionString = toTestConnectionString(getBackendConnectionString())
  const options = parseConnectionString(connectionString)
  const { database, ...serverOptions } = options
  const connection = await mysql.createConnection(serverOptions)

  try {
    await connection.query(`DROP DATABASE IF EXISTS \`${testDatabaseName}\`;`)
    await connection.query(`CREATE DATABASE IF NOT EXISTS \`${testDatabaseName}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`)
  } finally {
    await connection.end()
  }

  return connectionString
}

export async function createTestDbConnection() {
  const connectionString = toTestConnectionString(getBackendConnectionString())
  return mysql.createConnection(parseConnectionString(connectionString))
}

function truncateForColumn(value, maxLength) {
  if (typeof value !== 'string' || value.length <= maxLength) {
    return value
  }

  return value.slice(0, maxLength)
}

export async function ensureTestCheckLogTable() {
  const connection = await createTestDbConnection()

  try {
    await connection.query(`
      CREATE TABLE IF NOT EXISTS test_check_log (
        id INT AUTO_INCREMENT PRIMARY KEY,
        page_name VARCHAR(200) NOT NULL,
        button_name VARCHAR(100) NOT NULL,
        action_type VARCHAR(20) NOT NULL,
        request_url VARCHAR(500) NULL,
        request_body TEXT NULL,
        response_status INT NULL,
        db_check_sql TEXT NULL,
        db_expected_change VARCHAR(50) NULL,
        db_actual_result TEXT NULL,
        passed TINYINT(1) NOT NULL DEFAULT 0,
        fix_attempts INT NOT NULL DEFAULT 0,
        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
      );
    `)
  } finally {
    await connection.end()
  }
}

export async function cleanupOrgUnitTestData(codePrefix = 'E2E_ORG_') {
  const connection = await createTestDbConnection()

  try {
    await connection.execute('DELETE FROM org_unit WHERE Code LIKE ?', [`${codePrefix}%`])
  } finally {
    await connection.end()
  }
}

export async function cleanupEmployeeTestData(employeeNoPrefix = 'E2E_EMP_') {
  const connection = await createTestDbConnection()

  try {
    await connection.execute('DELETE FROM employee WHERE EmployeeNo LIKE ?', [`${employeeNoPrefix}%`])
  } finally {
    await connection.end()
  }
}

export async function cleanupRoleTestData(roleCodePrefix = 'E2E_ROLE_') {
  const connection = await createTestDbConnection()

  try {
    const [roles] = await connection.execute('SELECT Id FROM sys_role WHERE RoleCode LIKE ?', [`${roleCodePrefix}%`])
    const roleIds = roles.map((item) => item.Id)
    if (roleIds.length > 0) {
      await connection.query('DELETE FROM sys_user_role WHERE RoleId IN (?)', [roleIds])
      await connection.query('DELETE FROM sys_role_permission WHERE RoleId IN (?)', [roleIds])
      await connection.query('DELETE FROM sys_role WHERE Id IN (?)', [roleIds])
    }
  } finally {
    await connection.end()
  }
}

export async function cleanupPostTestData(postCodePrefix = 'E2E_POST_') {
  const connection = await createTestDbConnection()

  try {
    const [posts] = await connection.execute('SELECT Id FROM sys_post WHERE PostCode LIKE ?', [`${postCodePrefix}%`])
    const postIds = posts.map((item) => item.Id)
    if (postIds.length > 0) {
      await connection.query('UPDATE sys_user SET PostId = NULL WHERE PostId IN (?)', [postIds])
      await connection.query('DELETE FROM sys_post_permission WHERE PostId IN (?)', [postIds])
      await connection.query('DELETE FROM sys_post WHERE Id IN (?)', [postIds])
    }
  } finally {
    await connection.end()
  }
}

export async function cleanupAccessUserTestData(usernamePrefix = 'e2e_access_') {
  const connection = await createTestDbConnection()

  try {
    const [users] = await connection.execute('SELECT Id FROM sys_user WHERE Username LIKE ?', [`${usernamePrefix}%`])
    const userIds = users.map((item) => item.Id)
    if (userIds.length > 0) {
      await connection.query('DELETE FROM sys_user_role WHERE UserId IN (?)', [userIds])
      await connection.query('DELETE FROM sys_user WHERE Id IN (?)', [userIds])
    }
  } finally {
    await connection.end()
  }
}

export async function cleanupMenuTestData(menuKeyPrefix = 'E2E_MENU_') {
  const connection = await createTestDbConnection()

  try {
    const [menus] = await connection.execute('SELECT Id FROM sys_menu WHERE MenuKey LIKE ?', [`${menuKeyPrefix}%`])
    const menuIds = menus.map((item) => item.Id)
    if (menuIds.length > 0) {
      await connection.query('DELETE FROM sys_role_permission WHERE MenuId IN (?)', [menuIds])
      await connection.query('DELETE FROM sys_post_permission WHERE MenuId IN (?)', [menuIds])
      await connection.query('DELETE FROM sys_menu WHERE Id IN (?)', [menuIds])
    }
  } finally {
    await connection.end()
  }
}

export async function findOrgUnitByCode(code) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT Id, Code, Name, ParentId, ManagerId, IsActive FROM org_unit WHERE Code = ? LIMIT 1',
      [code]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function findRoleByCode(roleCode) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT Id, RoleCode, RoleName, Description, IsActive FROM sys_role WHERE RoleCode = ? LIMIT 1',
      [roleCode]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function getRolePermissionMenuIds(roleId) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT MenuId FROM sys_role_permission WHERE RoleId = ? ORDER BY MenuId',
      [roleId]
    )

    return rows.map((item) => item.MenuId)
  } finally {
    await connection.end()
  }
}

export async function findPostByCode(postCode) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT Id, PostCode, PostName, Description, IsActive FROM sys_post WHERE PostCode = ? LIMIT 1',
      [postCode]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function getPostPermissionMenuIds(postId) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT MenuId FROM sys_post_permission WHERE PostId = ? ORDER BY MenuId',
      [postId]
    )

    return rows.map((item) => item.MenuId)
  } finally {
    await connection.end()
  }
}

export async function findEmployeeByNo(employeeNo) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      `SELECT Id, EmployeeNo, Name, Phone, OrgUnitId, Level, MonthlySalary, ProbationDays, IsActive, DismissDate
       FROM employee
       WHERE EmployeeNo = ?
       LIMIT 1`,
      [employeeNo]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function findAccessUserByUsername(username) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT Id, Username, Name, Phone, Email, PostId, IsAdmin, IsActive FROM sys_user WHERE Username = ? LIMIT 1',
      [username]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function getUserRoleIds(userId) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT RoleId FROM sys_user_role WHERE UserId = ? ORDER BY RoleId',
      [userId]
    )

    return rows.map((item) => item.RoleId)
  } finally {
    await connection.end()
  }
}

export async function findMenuByPermissionCode(permissionCode) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      `SELECT Id, ParentId, MenuKey, MenuName, MenuType, SortOrder, RoutePath, ComponentPath, Icon, IsVisible, IsActive, PermissionCode
       FROM sys_menu
       WHERE PermissionCode = ?
       LIMIT 1`,
      [permissionCode]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function findMenuByKey(menuKey) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      `SELECT Id, ParentId, MenuKey, MenuName, MenuType, SortOrder, RoutePath, ComponentPath, Icon, IsVisible, IsActive, PermissionCode
       FROM sys_menu
       WHERE MenuKey = ?
       LIMIT 1`,
      [menuKey]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function insertTestCheckLog(entry) {
  const connection = await createTestDbConnection()

  try {
    await connection.execute(
      `INSERT INTO test_check_log
       (page_name, button_name, action_type, request_url, request_body, response_status, db_check_sql, db_expected_change, db_actual_result, passed, fix_attempts)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        truncateForColumn(entry.pageName, 200),
        truncateForColumn(entry.buttonName, 100),
        truncateForColumn(entry.actionType, 20),
        truncateForColumn(entry.requestUrl ?? null, 500),
        entry.requestBody ?? null,
        entry.responseStatus ?? null,
        entry.dbCheckSql ?? null,
        truncateForColumn(entry.dbExpectedChange ?? null, 50),
        entry.dbActualResult ?? null,
        entry.passed ? 1 : 0,
        entry.fixAttempts ?? 0
      ]
    )
  } finally {
    await connection.end()
  }
}

export async function getConfigParam(category, paramKey) {
  const connection = await createTestDbConnection()

  try {
    const [rows] = await connection.execute(
      'SELECT Id, Category, ParamKey, ParamValue, IsActive FROM sys_config_param WHERE Category = ? AND ParamKey = ? LIMIT 1',
      [category, paramKey]
    )

    return rows[0] || null
  } finally {
    await connection.end()
  }
}

export async function setLoginCaptchaEnabled(enabled) {
  const connection = await createTestDbConnection()

  try {
    await connection.execute(
      `INSERT INTO sys_config_param
       (Id, Category, ParamKey, ParamValue, Description, IsGlobal, IsActive, TakeEffectImmediately, CreatedAt, UpdatedAt)
       VALUES (UUID(), 'security', 'loginCaptchaEnabled', ?, '是否启用登录验证码', 1, 1, 1, NOW(), NOW())
       ON DUPLICATE KEY UPDATE ParamValue = VALUES(ParamValue), UpdatedAt = VALUES(UpdatedAt), IsActive = 1`,
      [enabled ? 'true' : 'false']
    )
  } finally {
    await connection.end()
  }
}
