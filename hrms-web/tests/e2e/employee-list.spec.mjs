import { expect, test } from '@playwright/test'
import {
  cleanupEmployeeTestData,
  ensureTestCheckLogTable,
  findEmployeeByNo,
  insertTestCheckLog
} from './db.mjs'

const employeeNoPrefix = '990'

async function loginAsAdmin(request) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/auth/login', {
    data: {
      username: 'admin',
      password: '123456'
    }
  })

  expect(response.ok()).toBeTruthy()

  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

async function bootstrapAuthedPage(page, auth) {
  await page.addInitScript((payload) => {
    localStorage.setItem('token', payload.token)
    localStorage.setItem('userInfo', JSON.stringify(payload.userInfo))
    localStorage.setItem('permissions', JSON.stringify(payload.permissions))
    localStorage.setItem('menuTree', JSON.stringify(payload.menus))
  }, auth)

  await page.goto('/')
  await expect(page).not.toHaveURL(/\/login$/)
  await page.goto('/employees')
}

async function getAvailableOrgUnit(request, auth) {
  const response = await request.get('http://127.0.0.1:5000/api/v1/org-units', {
    headers: {
      Authorization: `Bearer ${auth.token}`
    }
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)

  const orgUnit = (payload.data || []).find((item) => Number(item.level) !== 5 && item.isActive !== false)
  expect(orgUnit).toBeTruthy()
  return orgUnit
}

async function createEmployeeByApi(request, auth, data) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/employees', {
    headers: {
      Authorization: `Bearer ${auth.token}`
    },
    data
  })

  expect(response.ok()).toBeTruthy()
  expect(response.status()).toBe(201)

  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

function buildEmployeeSeed(uniqueSuffix, orgUnitId, overrides = {}) {
  const suffix4 = uniqueSuffix.slice(-4)
  const suffix8 = uniqueSuffix.slice(-8)

  return {
    employeeNo: `${employeeNoPrefix}${uniqueSuffix}`,
    name: `E2E员工${uniqueSuffix}`,
    gender: 0,
    idCard: `11010519900101${suffix4}`,
    phone: `138${suffix8}`,
    orgUnitId,
    employeeType: 0,
    salaryMode: 0,
    level: 'P2',
    tags: ['操作员'],
    monthlySalary: 6500,
    probationDays: 30,
    ...overrides
  }
}

async function logCheckResult(entry) {
  await insertTestCheckLog({
    fixAttempts: 0,
    ...entry
  })
}

test.beforeEach(async () => {
  await ensureTestCheckLogTable()
  await cleanupEmployeeTestData(employeeNoPrefix)
})

test('员工列表新增应触发接口并写入数据库', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const orgUnit = await getAvailableOrgUnit(request, auth)
  const uniqueSuffix = Date.now().toString()
  const employeeNo = `${employeeNoPrefix}${uniqueSuffix}`
  const employeeName = `E2E新增员工${uniqueSuffix}`
  const idCard = `11010519900101${uniqueSuffix.slice(-4)}`
  const phone = `138${uniqueSuffix.slice(-8)}`
  const dbCheckSql = `SELECT Id, EmployeeNo, Name, Phone, OrgUnitId, IsActive FROM employee WHERE EmployeeNo = '${employeeNo}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await bootstrapAuthedPage(page, auth)

    const createRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/employees')
    )
    const createResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/employees')
    )

    await page.getByTestId('employee-add').click()
    await page.locator('#employee-no').fill(employeeNo)
    await page.locator('#employee-name').fill(employeeName)
    await page.locator('#employee-id-card').fill(idCard)
    await page.locator('#employee-phone').fill(phone)
    await page.getByRole('button', { name: '下一步' }).click()

    await page.locator('#employee-org-unit').click()
    await page.locator('.ant-select-tree').getByText(orgUnit.name).first().click()
    await page.getByRole('button', { name: '下一步' }).click()
    await page.getByRole('button', { name: '下一步' }).click()
    await page.getByTestId('employee-save').click()

    const createRequest = await createRequestPromise
    const createResponse = await createResponsePromise
    const responseJson = await createResponse.json()

    requestUrl = createRequest.url()
    requestBody = createRequest.postData()
    responseStatus = createResponse.status()

    expect(requestUrl).toContain('/api/v1/employees')
    expect(createRequest.postDataJSON()).toMatchObject({
      employeeNo,
      name: employeeName,
      idCard,
      phone,
      orgUnitId: orgUnit.id
    })
    expect(responseStatus).toBe(201)
    expect(responseJson.code).toBe(200)

    const dbRow = await findEmployeeByNo(employeeNo)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(dbRow.EmployeeNo).toBe(employeeNo)
    expect(dbRow.Name).toBe(employeeName)
    expect(dbRow.Phone).toBe(phone)
    expect(dbRow.OrgUnitId).toBe(orgUnit.id)
    expect(Number(dbRow.IsActive)).toBe(1)

    await expect(page.locator('.ant-table-row').filter({ hasText: employeeNo }).first()).toBeVisible()

    await logCheckResult({
      pageName: '员工列表',
      buttonName: '新增员工',
      actionType: 'add',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'row_count_gt_0',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '员工列表',
      buttonName: '新增员工',
      actionType: 'add',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'row_count_gt_0',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('员工列表编辑应触发接口并更新数据库', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const orgUnit = await getAvailableOrgUnit(request, auth)
  const uniqueSuffix = Date.now().toString()
  const original = buildEmployeeSeed(uniqueSuffix, orgUnit.id)
  const updatedName = `E2E编辑后${uniqueSuffix}`
  const updatedPhone = `139${Date.now().toString().slice(-8)}`
  const dbCheckSql = `SELECT Name, Phone, MonthlySalary, ProbationDays FROM employee WHERE EmployeeNo = '${original.employeeNo}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const created = await createEmployeeByApi(request, auth, original)
    await bootstrapAuthedPage(page, auth)

    const targetRow = page.locator('.ant-table-row').filter({ hasText: original.employeeNo }).first()
    await expect(targetRow).toBeVisible()

    const updateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/employees')
    )
    const updateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/employees')
    )

    await targetRow.getByRole('button', { name: '编辑' }).click()
    await page.locator('#employee-name').fill(updatedName)
    await page.locator('#employee-phone').fill(updatedPhone)
    await page.getByRole('button', { name: '下一步' }).click()
    await page.getByRole('button', { name: '下一步' }).click()
    await page.locator('#employee-monthly-salary').fill('7800')
    await page.getByRole('button', { name: '下一步' }).click()
    await page.locator('#employee-probation-days').fill('45')
    await page.getByTestId('employee-save').click()

    const updateRequest = await updateRequestPromise
    const updateResponse = await updateResponsePromise
    const responseJson = await updateResponse.json()

    requestUrl = updateRequest.url()
    requestBody = updateRequest.postData()
    responseStatus = updateResponse.status()

    expect(requestUrl).toContain('/api/v1/employees')
    expect(updateRequest.postDataJSON()).toMatchObject({
      id: created.id,
      name: updatedName,
      phone: updatedPhone,
      monthlySalary: 7800,
      probationDays: 45
    })
    expect(responseStatus).toBe(200)
    expect(responseJson.code).toBe(200)

    const dbRow = await findEmployeeByNo(original.employeeNo)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(dbRow.Name).toBe(updatedName)
    expect(dbRow.Phone).toBe(updatedPhone)
    expect(Number(dbRow.MonthlySalary)).toBe(7800)
    expect(Number(dbRow.ProbationDays)).toBe(45)

    await logCheckResult({
      pageName: '员工列表',
      buttonName: '编辑',
      actionType: 'edit',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: `name_eq_${updatedName}`,
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '员工列表',
      buttonName: '编辑',
      actionType: 'edit',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: `name_eq_${updatedName}`,
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('员工列表离职应触发接口并保留离职员工可查', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const orgUnit = await getAvailableOrgUnit(request, auth)
  const uniqueSuffix = Date.now().toString()
  const employee = buildEmployeeSeed(uniqueSuffix, orgUnit.id)
  const dbCheckSql = `SELECT IsActive, DismissDate FROM employee WHERE EmployeeNo = '${employee.employeeNo}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const created = await createEmployeeByApi(request, auth, employee)
    await bootstrapAuthedPage(page, auth)

    const targetRow = page.locator('.ant-table-row').filter({ hasText: employee.employeeNo }).first()
    await expect(targetRow).toBeVisible()

    const dismissRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes(`/api/v1/employees/${created.id}/toggle-dismiss`)
    )
    const dismissResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes(`/api/v1/employees/${created.id}/toggle-dismiss`)
    )

    await targetRow.getByRole('button', { name: '离职' }).click()
    await page.locator('.ant-popconfirm .ant-btn-primary').click()

    const dismissRequest = await dismissRequestPromise
    const dismissResponse = await dismissResponsePromise
    const responseJson = await dismissResponse.json()

    requestUrl = dismissRequest.url()
    requestBody = dismissRequest.postData()
    responseStatus = dismissResponse.status()

    expect(requestUrl).toContain(`/api/v1/employees/${created.id}/toggle-dismiss`)
    expect(responseStatus).toBe(200)
    expect(responseJson.code).toBe(200)

    const dbRow = await findEmployeeByNo(employee.employeeNo)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(Number(dbRow.IsActive)).toBe(0)
    expect(dbRow.DismissDate).toBeTruthy()
    await expect(page.locator('.ant-table-row').filter({ hasText: employee.employeeNo }).first()).toContainText('离职')

    await logCheckResult({
      pageName: '员工列表',
      buttonName: '离职',
      actionType: 'toggle',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'is_active_eq_0',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '员工列表',
      buttonName: '离职',
      actionType: 'toggle',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'is_active_eq_0',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('员工列表查看应跳转详情页并展示员工信息', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const orgUnit = await getAvailableOrgUnit(request, auth)
  const uniqueSuffix = Date.now().toString()
  const employee = buildEmployeeSeed(uniqueSuffix, orgUnit.id, {
    probationDays: 60
  })
  const dbCheckSql = `SELECT Id, EmployeeNo, Name FROM employee WHERE EmployeeNo = '${employee.employeeNo}' LIMIT 1`

  let dbResult = null

  try {
    const created = await createEmployeeByApi(request, auth, employee)
    await bootstrapAuthedPage(page, auth)

    const targetRow = page.locator('.ant-table-row').filter({ hasText: employee.employeeNo }).first()
    await expect(targetRow).toBeVisible()

    await targetRow.getByRole('button', { name: '查看' }).click()

    await expect(page).toHaveURL(new RegExp(`/employees/detail/${created.id}`))
    await expect(page.locator('.detail-main-name')).toHaveText(employee.name)
    await expect(page.getByText(`工号 ${employee.employeeNo}`)).toBeVisible()
    await expect(page.getByText(orgUnit.name).first()).toBeVisible()

    const dbRow = await findEmployeeByNo(employee.employeeNo)
    dbResult = JSON.stringify(dbRow)

    await logCheckResult({
      pageName: '员工列表',
      buttonName: '查看',
      actionType: 'view',
      requestUrl: `/employees/detail/${created.id}`,
      requestBody: null,
      responseStatus: 200,
      dbCheckSql,
      dbExpectedChange: 'route_jump_success',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '员工列表',
      buttonName: '查看',
      actionType: 'view',
      requestUrl: null,
      requestBody: null,
      responseStatus: null,
      dbCheckSql,
      dbExpectedChange: 'route_jump_success',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})
