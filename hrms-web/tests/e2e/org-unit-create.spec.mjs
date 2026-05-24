import { expect, test } from '@playwright/test'
import {
  cleanupOrgUnitTestData,
  ensureTestCheckLogTable,
  findOrgUnitByCode,
  insertTestCheckLog
} from './db.mjs'

const testCodePrefix = 'E2E_ORG_'

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
  await page.goto('/org-units')
}

async function createOrgUnitByApi(request, auth, { code, name, level = 3, parentId = null }) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/org-units', {
    headers: {
      Authorization: `Bearer ${auth.token}`
    },
    data: {
      code,
      name,
      level,
      parentId
    }
  })

  expect(response.ok()).toBeTruthy()
  expect(response.status()).toBe(201)

  const payload = await response.json()
  expect(payload.code).toBe(200)

  return payload.data
}

async function logCheckResult(entry) {
  await insertTestCheckLog({
    fixAttempts: 0,
    ...entry
  })
}

test.beforeEach(async () => {
  await ensureTestCheckLogTable()
  await cleanupOrgUnitTestData(testCodePrefix)
})

test('组织单元新增按钮应触发正确接口并写入数据库', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const orgCode = `${testCodePrefix}${uniqueSuffix}`
  const orgName = `E2E组织${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, Code, Name, IsActive FROM org_unit WHERE Code = '${orgCode}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await bootstrapAuthedPage(page, auth)

    const addButton = page.locator('[data-testid="org-unit-add"], .toolbar button:has-text("新增")').first()
    const saveButton = page.locator('[data-testid="org-unit-save"], .ant-modal .ant-btn-primary').last()

    await expect(addButton).toBeVisible()

    const createRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/org-units')
    )
    const createResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/org-units')
    )

    await addButton.click()
    await page.locator('#org-unit-name').fill(orgName)
    await page.locator('#org-unit-code').fill(orgCode)
    await saveButton.click()

    const createRequest = await createRequestPromise
    const createResponse = await createResponsePromise
    const responseJson = await createResponse.json()

    requestUrl = createRequest.url()
    requestBody = createRequest.postData()
    responseStatus = createResponse.status()

    expect(requestUrl).toContain('/api/v1/org-units')
    expect(createRequest.postDataJSON()).toMatchObject({
      code: orgCode,
      name: orgName,
      level: 3
    })
    expect(responseStatus).toBe(201)
    expect(responseJson.code).toBe(200)

    const dbRow = await findOrgUnitByCode(orgCode)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(dbRow.Code).toBe(orgCode)
    expect(dbRow.Name).toBe(orgName)
    expect(Number(dbRow.IsActive)).toBe(1)

    await expect(page.locator('.list-panel').getByText(orgName).first()).toBeVisible()

    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '新增',
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
      pageName: '组织单元列表',
      buttonName: '新增',
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

test('组织单元编辑按钮应触发正确接口并更新数据库', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = `${Date.now()}_EDIT`
  const orgCode = `${testCodePrefix}${uniqueSuffix}`
  const originalName = `E2E编辑前${uniqueSuffix}`
  const updatedName = `E2E编辑后${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, Code, Name, IsActive FROM org_unit WHERE Code = '${orgCode}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const created = await createOrgUnitByApi(request, auth, {
      code: orgCode,
      name: originalName
    })

    await bootstrapAuthedPage(page, auth)

    const targetRow = page.locator('.ant-table-row').filter({ hasText: orgCode }).first()
    await expect(targetRow).toBeVisible()

    const updateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/org-units')
    )
    const updateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/org-units')
    )

    await targetRow.getByRole('button', { name: '编辑' }).click()
    await page.locator('#org-unit-name').fill(updatedName)
    await page.locator('[data-testid="org-unit-save"], .ant-modal .ant-btn-primary').last().click()

    const updateRequest = await updateRequestPromise
    const updateResponse = await updateResponsePromise
    const responseJson = await updateResponse.json()

    requestUrl = updateRequest.url()
    requestBody = updateRequest.postData()
    responseStatus = updateResponse.status()

    expect(requestUrl).toContain('/api/v1/org-units')
    expect(updateRequest.postDataJSON()).toMatchObject({
      id: created.id,
      code: orgCode,
      name: updatedName,
      level: 3,
      isActive: true
    })
    expect(responseStatus).toBe(200)
    expect(responseJson.code).toBe(200)

    const dbRow = await findOrgUnitByCode(orgCode)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(dbRow.Id).toBe(created.id)
    expect(dbRow.Name).toBe(updatedName)
    await expect(page.locator('.list-panel').getByText(updatedName).first()).toBeVisible()

    await logCheckResult({
      pageName: '组织单元列表',
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
      pageName: '组织单元列表',
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

test('组织单元删除按钮应触发正确接口并移除数据库记录', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = `${Date.now()}_DELETE`
  const orgCode = `${testCodePrefix}${uniqueSuffix}`
  const orgName = `E2E删除${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, Code, Name, IsActive FROM org_unit WHERE Code = '${orgCode}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const created = await createOrgUnitByApi(request, auth, {
      code: orgCode,
      name: orgName
    })

    await bootstrapAuthedPage(page, auth)

    const targetRow = page.locator('.ant-table-row').filter({ hasText: orgCode }).first()
    await expect(targetRow).toBeVisible()

    const deleteRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'DELETE' && candidate.url().includes(`/api/v1/org-units/${created.id}`)
    )
    const deleteResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'DELETE' && candidate.url().includes(`/api/v1/org-units/${created.id}`)
    )

    await targetRow.getByRole('button', { name: '删除' }).click()
    await page.locator('.ant-popconfirm .ant-btn-primary').click()

    const deleteRequest = await deleteRequestPromise
    const deleteResponse = await deleteResponsePromise
    const responseJson = await deleteResponse.json()

    requestUrl = deleteRequest.url()
    requestBody = deleteRequest.postData()
    responseStatus = deleteResponse.status()

    expect(requestUrl).toContain(`/api/v1/org-units/${created.id}`)
    expect(responseStatus).toBe(200)
    expect(responseJson.code).toBe(200)

    const dbRow = await findOrgUnitByCode(orgCode)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).toBeNull()
    await expect(page.locator('.ant-table-row').filter({ hasText: orgCode })).toHaveCount(0)

    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '删除',
      actionType: 'delete',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'row_count_eq_0',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '删除',
      actionType: 'delete',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'row_count_eq_0',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('组织单元状态切换按钮应触发正确接口并更新数据库状态', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = `${Date.now()}_TOGGLE`
  const orgCode = `${testCodePrefix}${uniqueSuffix}`
  const orgName = `E2E状态切换${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, Code, Name, IsActive FROM org_unit WHERE Code = '${orgCode}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const created = await createOrgUnitByApi(request, auth, {
      code: orgCode,
      name: orgName
    })

    await bootstrapAuthedPage(page, auth)

    const targetRow = page.locator('.ant-table-row').filter({ hasText: orgCode }).first()
    await expect(targetRow).toBeVisible()

    const toggleRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes(`/api/v1/org-units/${created.id}/toggle-status`)
    )
    const toggleResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes(`/api/v1/org-units/${created.id}/toggle-status`)
    )

    await targetRow.getByRole('button', { name: '停用' }).click()

    const toggleRequest = await toggleRequestPromise
    const toggleResponse = await toggleResponsePromise
    const responseJson = await toggleResponse.json()

    requestUrl = toggleRequest.url()
    requestBody = toggleRequest.postData()
    responseStatus = toggleResponse.status()

    expect(requestUrl).toContain(`/api/v1/org-units/${created.id}/toggle-status`)
    expect(responseStatus).toBe(200)
    expect(responseJson.code).toBe(200)

    const dbRow = await findOrgUnitByCode(orgCode)
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(Number(dbRow.IsActive)).toBe(0)
    await expect(targetRow.getByText('停用')).toBeVisible()

    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '停用/启用',
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
      pageName: '组织单元列表',
      buttonName: '停用/启用',
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

test('组织单元批量停用和启用应触发正确接口并批量更新数据库状态', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = `${Date.now()}_BATCH`
  const firstCode = `${testCodePrefix}${uniqueSuffix}_A`
  const secondCode = `${testCodePrefix}${uniqueSuffix}_B`
  const dbCheckSql = `SELECT Code, IsActive FROM org_unit WHERE Code IN ('${firstCode}', '${secondCode}') ORDER BY Code`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await createOrgUnitByApi(request, auth, {
      code: firstCode,
      name: `E2E批量A${uniqueSuffix}`
    })
    await createOrgUnitByApi(request, auth, {
      code: secondCode,
      name: `E2E批量B${uniqueSuffix}`
    })

    await bootstrapAuthedPage(page, auth)

    const firstRow = page.locator('.ant-table-row').filter({ hasText: firstCode }).first()
    const secondRow = page.locator('.ant-table-row').filter({ hasText: secondCode }).first()
    await expect(firstRow).toBeVisible()
    await expect(secondRow).toBeVisible()

    await firstRow.locator('.ant-checkbox-input').check({ force: true })
    await secondRow.locator('.ant-checkbox-input').check({ force: true })

    const disableRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/org-units/batch-disable')
    )
    const disableResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/org-units/batch-disable')
    )

    await page.getByRole('button', { name: '批量停用' }).click()

    const disableRequest = await disableRequestPromise
    const disableResponse = await disableResponsePromise
    const disableJson = await disableResponse.json()

    requestUrl = disableRequest.url()
    requestBody = disableRequest.postData()
    responseStatus = disableResponse.status()

    expect(requestUrl).toContain('/api/v1/org-units/batch-disable')
    expect(responseStatus).toBe(200)
    expect(disableJson.code).toBe(200)

    let firstDbRow = await findOrgUnitByCode(firstCode)
    let secondDbRow = await findOrgUnitByCode(secondCode)
    dbResult = JSON.stringify([firstDbRow, secondDbRow])

    expect(Number(firstDbRow.IsActive)).toBe(0)
    expect(Number(secondDbRow.IsActive)).toBe(0)

    const enableRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/org-units/batch-enable')
    )
    const enableResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/org-units/batch-enable')
    )

    await firstRow.locator('.ant-checkbox-input').check({ force: true })
    await secondRow.locator('.ant-checkbox-input').check({ force: true })
    await page.getByRole('button', { name: '批量启用' }).click()

    const enableRequest = await enableRequestPromise
    const enableResponse = await enableResponsePromise
    const enableJson = await enableResponse.json()

    requestUrl = enableRequest.url()
    requestBody = enableRequest.postData()
    responseStatus = enableResponse.status()

    expect(requestUrl).toContain('/api/v1/org-units/batch-enable')
    expect(responseStatus).toBe(200)
    expect(enableJson.code).toBe(200)

    firstDbRow = await findOrgUnitByCode(firstCode)
    secondDbRow = await findOrgUnitByCode(secondCode)
    dbResult = JSON.stringify([firstDbRow, secondDbRow])

    expect(Number(firstDbRow.IsActive)).toBe(1)
    expect(Number(secondDbRow.IsActive)).toBe(1)

    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '批量停用/批量启用',
      actionType: 'batch_toggle',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'all_is_active_eq_1',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '批量停用/批量启用',
      actionType: 'batch_toggle',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'all_is_active_eq_1',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('组织单元批量修改应触发正确接口并批量更新数据库状态', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = `${Date.now()}_BATCH_EDIT`
  const firstCode = `${testCodePrefix}${uniqueSuffix}_A`
  const secondCode = `${testCodePrefix}${uniqueSuffix}_B`
  const dbCheckSql = `SELECT Code, IsActive FROM org_unit WHERE Code IN ('${firstCode}', '${secondCode}') ORDER BY Code`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const firstCreated = await createOrgUnitByApi(request, auth, {
      code: firstCode,
      name: `E2E批量修改A${uniqueSuffix}`
    })
    const secondCreated = await createOrgUnitByApi(request, auth, {
      code: secondCode,
      name: `E2E批量修改B${uniqueSuffix}`
    })

    await bootstrapAuthedPage(page, auth)

    const firstRow = page.locator('.ant-table-row').filter({ hasText: firstCode }).first()
    const secondRow = page.locator('.ant-table-row').filter({ hasText: secondCode }).first()
    await expect(firstRow).toBeVisible()
    await expect(secondRow).toBeVisible()

    await firstRow.locator('.ant-checkbox-input').check({ force: true })
    await secondRow.locator('.ant-checkbox-input').check({ force: true })

    const batchUpdateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/org-units/batch-update')
    )
    const batchUpdateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/org-units/batch-update')
    )

    await page.getByRole('button', { name: '批量修改' }).click()

    const modal = page.locator('.ant-modal').filter({ hasText: '批量修改组织单元' }).last()
    await expect(modal).toBeVisible()
    await modal.locator('.ant-select').nth(1).click()
    await page.locator('.ant-select-dropdown').last().getByText('停用').click()
    await modal.getByRole('button', { name: '确 定' }).click()

    const batchUpdateRequest = await batchUpdateRequestPromise
    const batchUpdateResponse = await batchUpdateResponsePromise
    const responseJson = await batchUpdateResponse.json()

    requestUrl = batchUpdateRequest.url()
    requestBody = batchUpdateRequest.postData()
    responseStatus = batchUpdateResponse.status()

    expect(requestUrl).toContain('/api/v1/org-units/batch-update')
    expect(batchUpdateRequest.postDataJSON()).toMatchObject({
      ids: expect.arrayContaining([firstCreated.id, secondCreated.id]),
      isActive: false
    })
    expect(responseStatus).toBe(200)
    expect(responseJson.code).toBe(200)

    const firstDbRow = await findOrgUnitByCode(firstCode)
    const secondDbRow = await findOrgUnitByCode(secondCode)
    dbResult = JSON.stringify([firstDbRow, secondDbRow])

    expect(Number(firstDbRow.IsActive)).toBe(0)
    expect(Number(secondDbRow.IsActive)).toBe(0)

    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '批量修改',
      actionType: 'batch_edit',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'all_is_active_eq_0',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '组织单元列表',
      buttonName: '批量修改',
      actionType: 'batch_edit',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'all_is_active_eq_0',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})
