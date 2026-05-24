import { expect, test } from '@playwright/test'
import {
  cleanupAccessUserTestData,
  cleanupMenuTestData,
  cleanupPostTestData,
  cleanupRoleTestData,
  ensureTestCheckLogTable,
  findAccessUserByUsername,
  findMenuByKey,
  findMenuByPermissionCode,
  findPostByCode,
  findRoleByCode,
  getPostPermissionMenuIds,
  getRolePermissionMenuIds,
  getUserRoleIds,
  insertTestCheckLog
} from './db.mjs'

const roleCodePrefix = 'E2E_ROLE_'
const postCodePrefix = 'E2E_POST_'
const usernamePrefix = 'e2e_access_'
const menuKeyPrefix = 'E2E_MENU_'

async function loginByCredentials(request, username, password) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/auth/login', {
    data: {
      username,
      password
    }
  })

  expect(response.ok()).toBeTruthy()

  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

async function loginAsAdmin(request) {
  return loginByCredentials(request, 'admin', '123456')
}

async function bootstrapAuthedPage(page, auth, path) {
  await page.addInitScript((payload) => {
    localStorage.setItem('token', payload.token)
    localStorage.setItem('userInfo', JSON.stringify(payload.userInfo))
    localStorage.setItem('permissions', JSON.stringify(payload.permissions))
    localStorage.setItem('menuTree', JSON.stringify(payload.menus))
  }, auth)

  await page.goto('/')
  await expect(page).not.toHaveURL(/\/login$/)
  await page.goto(path)
}

async function createRoleByApi(request, auth, data) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/access/roles', {
    headers: { Authorization: `Bearer ${auth.token}` },
    data
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

async function createPostByApi(request, auth, data) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/access/posts', {
    headers: { Authorization: `Bearer ${auth.token}` },
    data
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

async function createUserByApi(request, auth, data) {
  const response = await request.post('http://127.0.0.1:5000/api/v1/access/users', {
    headers: { Authorization: `Bearer ${auth.token}` },
    data
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

async function updateMenuByApi(request, auth, data) {
  const response = await request.put('http://127.0.0.1:5000/api/v1/access/menus', {
    headers: { Authorization: `Bearer ${auth.token}` },
    data
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)
  return payload.data
}

async function assignRolePermissionsByApi(request, auth, roleId, menuIds) {
  const response = await request.put(`http://127.0.0.1:5000/api/v1/access/roles/${roleId}/permissions`, {
    headers: { Authorization: `Bearer ${auth.token}` },
    data: { menuIds }
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)
}

async function assignPostPermissionsByApi(request, auth, postId, menuIds) {
  const response = await request.put(`http://127.0.0.1:5000/api/v1/access/posts/${postId}/permissions`, {
    headers: { Authorization: `Bearer ${auth.token}` },
    data: { menuIds }
  })

  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  expect(payload.code).toBe(200)
}

async function logCheckResult(entry) {
  await insertTestCheckLog({
    fixAttempts: 0,
    ...entry
  })
}

function toMenuUpdatePayload(menu, overrides = {}) {
  return {
    id: menu.Id,
    parentId: menu.ParentId ?? null,
    menuKey: menu.MenuKey,
    menuName: menu.MenuName,
    menuType: menu.MenuType,
    sortOrder: Number(menu.SortOrder || 0),
    routePath: menu.RoutePath ?? null,
    componentPath: menu.ComponentPath ?? null,
    icon: menu.Icon ?? null,
    isVisible: Boolean(menu.IsVisible),
    isActive: Boolean(menu.IsActive),
    permissionCode: menu.PermissionCode ?? null,
    ...overrides
  }
}

async function ensureRowExpanded(row, expectedChildRow = null) {
  await expect(row).toBeVisible()

  if (expectedChildRow && await expectedChildRow.first().isVisible().catch(() => false)) {
    return
  }

  const expandButton = row.getByRole('button', { name: '展开行' }).first()
  if (await expandButton.count()) {
    await expandButton.click()
  }

  if (expectedChildRow) {
    await expect(expectedChildRow.first()).toBeVisible()
  }
}

async function setMenuActiveInModal(modal, active) {
  const activeSwitch = modal.getByRole('switch').nth(1)
  const checked = await activeSwitch.getAttribute('aria-checked')
  if ((checked === 'true') !== active) {
    await activeSwitch.click()
  }
}

test.beforeEach(async () => {
  await ensureTestCheckLogTable()
  await cleanupAccessUserTestData(usernamePrefix)
  await cleanupMenuTestData(menuKeyPrefix)
  await cleanupRoleTestData(roleCodePrefix)
  await cleanupPostTestData(postCodePrefix)
})

test('菜单权限变化应实时影响当前登录用户的按钮显隐和页面访问', async ({ page, request }) => {
  const adminAuth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const role = await createRoleByApi(request, adminAuth, {
    roleCode: `${roleCodePrefix}MENU_SYNC_${uniqueSuffix}`,
    roleName: `E2E菜单联动角色${uniqueSuffix}`,
    description: '菜单权限联动测试角色',
    isActive: true
  })
  const username = `${usernamePrefix}menu_sync_${uniqueSuffix}`
  const menuPage = await findMenuByPermissionCode('page.access.menu')
  const menuCreateButton = await findMenuByPermissionCode('button.access.menu.create')
  const menuEditButton = await findMenuByPermissionCode('button.access.menu.edit')
  const rolePage = await findMenuByPermissionCode('page.access.role')
  const dbCheckSql = `SELECT PermissionCode, IsActive FROM sys_menu WHERE PermissionCode IN ('button.access.menu.create', 'page.access.role') ORDER BY PermissionCode`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    expect(menuPage).not.toBeNull()
    expect(menuCreateButton).not.toBeNull()
    expect(menuEditButton).not.toBeNull()
    expect(rolePage).not.toBeNull()

    await assignRolePermissionsByApi(request, adminAuth, role.id, [
      menuPage.Id,
      menuCreateButton.Id,
      menuEditButton.Id,
      rolePage.Id
    ])

    await createUserByApi(request, adminAuth, {
      username,
      password: '123456',
      name: `E2E菜单联动用户${uniqueSuffix}`,
      phone: `136${uniqueSuffix.slice(-8)}`,
      email: `${username}@e2e.test`,
      postId: null,
      roleIds: [role.id],
      isAdmin: false,
      isActive: true
    })

    const userAuth = await loginByCredentials(request, username, '123456')
    await bootstrapAuthedPage(page, userAuth, '/access/menus')

    await expect(page.getByRole('button', { name: '新增根节点' })).toBeVisible()

    const menuPageRow = page.locator('.ant-table-tbody tr').filter({ hasText: '/access/menus' }).first()
    const accessModuleRow = page.locator('.ant-table-tbody tr').filter({ hasText: 'module.access' }).first()
    await ensureRowExpanded(accessModuleRow, menuPageRow)

    const createButtonRow = page.locator('.ant-table-tbody tr').filter({ hasText: 'button.access.menu.create' }).first()
    await ensureRowExpanded(menuPageRow, createButtonRow)

    const disableCreateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )
    const disableCreateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )

    await createButtonRow.getByRole('button', { name: '编辑' }).click()
    let menuModal = page.locator('.ant-modal').last()
    await expect(menuModal).toBeVisible()
    await setMenuActiveInModal(menuModal, false)
    await menuModal.locator('.ant-btn-primary').click()

    const disableCreateRequest = await disableCreateRequestPromise
    const disableCreateResponse = await disableCreateResponsePromise
    const disableCreateJson = await disableCreateResponse.json()
    expect(disableCreateResponse.status()).toBe(200)
    expect(disableCreateJson.code).toBe(200)

    requestUrl = disableCreateRequest.url()
    requestBody = disableCreateRequest.postData()
    responseStatus = disableCreateResponse.status()

    await expect.poll(async () => {
      return page.evaluate(() => {
        const permissions = JSON.parse(localStorage.getItem('permissions') || '[]')
        return permissions.includes('button.access.menu.create')
      })
    }).toBe(false)

    await expect(page.getByRole('button', { name: '新增根节点' })).toBeHidden()

    const disableRolePageRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )
    const disableRolePageResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )

    const rolePageRow = page.locator('.ant-table-tbody tr').filter({ hasText: '/access/roles' }).first()
    const refreshedAccessModuleRow = page.locator('.ant-table-tbody tr').filter({ hasText: 'module.access' }).first()
    await ensureRowExpanded(refreshedAccessModuleRow, rolePageRow)
    await rolePageRow.getByRole('button', { name: '编辑' }).click()
    menuModal = page.locator('.ant-modal').last()
    await expect(menuModal).toBeVisible()
    await setMenuActiveInModal(menuModal, false)
    await menuModal.locator('.ant-btn-primary').click()

    const disableRolePageRequest = await disableRolePageRequestPromise
    const disableRolePageResponse = await disableRolePageResponsePromise
    const disableRolePageJson = await disableRolePageResponse.json()
    expect(disableRolePageResponse.status()).toBe(200)
    expect(disableRolePageJson.code).toBe(200)

    requestUrl = disableRolePageRequest.url()
    requestBody = disableRolePageRequest.postData()
    responseStatus = disableRolePageResponse.status()

    await expect.poll(async () => {
      return page.evaluate(() => {
        const permissions = JSON.parse(localStorage.getItem('permissions') || '[]')
        return permissions.includes('page.access.role')
      })
    }).toBe(false)

    await expect.poll(async () => {
      return page.evaluate(() => {
        const menuTree = JSON.parse(localStorage.getItem('menuTree') || '[]')
        return JSON.stringify(menuTree).includes('/access/roles')
      })
    }).toBe(false)

    await page.evaluate((targetPath) => {
      window.history.pushState({}, '', targetPath)
      window.dispatchEvent(new PopStateEvent('popstate'))
    }, '/access/roles')
    await expect(page).toHaveURL(/\/access\/menus$/)
    await expect(page.getByRole('main').getByText('权限 / 菜单管理', { exact: true })).toBeVisible()

    const createButtonMenuAfter = await findMenuByPermissionCode('button.access.menu.create')
    const rolePageAfter = await findMenuByPermissionCode('page.access.role')
    dbResult = JSON.stringify({
      username,
      roleId: role.id,
      createButtonMenuIsActive: createButtonMenuAfter?.IsActive,
      rolePageIsActive: rolePageAfter?.IsActive,
      currentUrl: page.url()
    })

    expect(Boolean(createButtonMenuAfter?.IsActive)).toBe(false)
    expect(Boolean(rolePageAfter?.IsActive)).toBe(false)

    await logCheckResult({
      pageName: '菜单管理',
      buttonName: '权限变化影响按钮显隐/页面访问',
      actionType: 'permission_sync',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'current_user_button_hidden_and_page_redirected_after_menu_deactivation',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '菜单管理',
      buttonName: '权限变化影响按钮显隐/页面访问',
      actionType: 'permission_sync',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'current_user_button_hidden_and_page_redirected_after_menu_deactivation',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  } finally {
    if (menuCreateButton) {
      await updateMenuByApi(request, adminAuth, toMenuUpdatePayload(menuCreateButton))
    }
    if (rolePage) {
      await updateMenuByApi(request, adminAuth, toMenuUpdatePayload(rolePage))
    }
  }
})

test('菜单管理应支持新增根节点、子节点、编辑、排序、删除', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const rootMenuKey = `${menuKeyPrefix}ROOT_${uniqueSuffix}`
  const childMenuKey = `${menuKeyPrefix}BTN_${uniqueSuffix}`
  const rootMenuName = `E2E菜单根节点${uniqueSuffix}`
  const updatedRootMenuName = `E2E菜单根节点已编辑${uniqueSuffix}`
  const childMenuName = `E2E菜单按钮${uniqueSuffix}`
  const routePath = `/access/e2e-menu-${uniqueSuffix}`
  const rootPermissionCode = `page.access.e2e.${uniqueSuffix}`
  const childPermissionCode = `button.access.e2e.${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, ParentId, MenuKey, MenuName, MenuType, SortOrder, PermissionCode FROM sys_menu WHERE MenuKey IN ('${rootMenuKey}', '${childMenuKey}') ORDER BY MenuKey`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await bootstrapAuthedPage(page, auth, '/access/menus')

    const createRootRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/access/menus')
    )
    const createRootResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/access/menus')
    )

    await page.getByRole('button', { name: '新增根节点' }).click()
    let menuModal = page.locator('.ant-modal').last()
    await expect(menuModal).toBeVisible()
    await menuModal.getByPlaceholder('如：employee-list').fill(rootMenuKey)
    await menuModal.getByPlaceholder('请输入菜单名称').fill(rootMenuName)
    await menuModal.getByPlaceholder('如：/employees').fill(routePath)
    await menuModal.getByPlaceholder('如：views/employee/EmployeeList.vue').fill('views/access/RoleManagement.vue')
    await menuModal.getByPlaceholder('如：page.employee.list').fill(rootPermissionCode)
    await menuModal.locator('.ant-btn-primary').click()

    const createRootRequest = await createRootRequestPromise
    const createRootResponse = await createRootResponsePromise
    const createRootJson = await createRootResponse.json()

    requestUrl = createRootRequest.url()
    requestBody = createRootRequest.postData()
    responseStatus = createRootResponse.status()

    expect(responseStatus).toBe(200)
    expect(createRootJson.code).toBe(200)

    const createdRootMenu = await findMenuByKey(rootMenuKey)
    expect(createdRootMenu).not.toBeNull()
    expect(createdRootMenu.MenuName).toBe(rootMenuName)
    expect(createdRootMenu.RoutePath).toBe(routePath)
    expect(createdRootMenu.MenuType).toBe('page')

    const rootRow = page.locator('.ant-table-tbody tr').filter({ hasText: routePath }).first()
    const createChildRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/access/menus')
    )
    const createChildResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/access/menus')
    )

    await rootRow.getByRole('button', { name: '新增子节点' }).click()
    menuModal = page.locator('.ant-modal').last()
    await expect(menuModal).toBeVisible()
    await menuModal.locator('.ant-form-item').filter({ hasText: '菜单类型' }).locator('.ant-select-selector').click()
    await page.locator('.ant-select-dropdown').last().getByText('按钮', { exact: true }).click()
    await menuModal.getByPlaceholder('如：employee-list').fill(childMenuKey)
    await menuModal.getByPlaceholder('请输入菜单名称').fill(childMenuName)
    await menuModal.getByPlaceholder('如：page.employee.list').fill(childPermissionCode)
    await menuModal.locator('.ant-btn-primary').click()

    const createChildResponse = await createChildResponsePromise
    await createChildRequestPromise
    const createChildJson = await createChildResponse.json()
    expect(createChildResponse.status()).toBe(200)
    expect(createChildJson.code).toBe(200)

    const createdChildMenu = await findMenuByKey(childMenuKey)
    expect(createdChildMenu).not.toBeNull()
    expect(createdChildMenu.ParentId).toBe(createdRootMenu.Id)
    expect(createdChildMenu.MenuType).toBe('button')
    expect(createdChildMenu.PermissionCode).toBe(childPermissionCode)

    const updateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )
    const updateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )

    await rootRow.getByRole('button', { name: '编辑' }).click()
    menuModal = page.locator('.ant-modal').last()
    await expect(menuModal).toBeVisible()
    await menuModal.getByPlaceholder('请输入菜单名称').fill(updatedRootMenuName)
    await menuModal.locator('.ant-btn-primary').click()

    const updateResponse = await updateResponsePromise
    const updateRequest = await updateRequestPromise
    const updateJson = await updateResponse.json()
    expect(updateResponse.status()).toBe(200)
    expect(updateJson.code).toBe(200)

    requestUrl = updateRequest.url()
    requestBody = updateRequest.postData()
    responseStatus = updateResponse.status()

    const updatedRootMenu = await findMenuByKey(rootMenuKey)
    expect(updatedRootMenu.MenuName).toBe(updatedRootMenuName)

    const sortRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )
    const sortResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/menus')
    )

    await rootRow.getByRole('button', { name: '排序' }).click()
    const sortModal = page.locator('.ant-modal').last()
    await expect(sortModal).toBeVisible()
    await sortModal.locator('.ant-input-number-input').fill('35')
    await sortModal.locator('.ant-btn-primary').click()

    const sortResponse = await sortResponsePromise
    const sortRequest = await sortRequestPromise
    const sortJson = await sortResponse.json()
    expect(sortResponse.status()).toBe(200)
    expect(sortJson.code).toBe(200)

    requestUrl = sortRequest.url()
    requestBody = sortRequest.postData()
    responseStatus = sortResponse.status()

    const sortedRootMenu = await findMenuByKey(rootMenuKey)
    expect(sortedRootMenu.SortOrder).toBe(40)

    const deleteRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'DELETE' && candidate.url().includes(`/api/v1/access/menus/${createdRootMenu.Id}`)
    )
    const deleteResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'DELETE' && candidate.url().includes(`/api/v1/access/menus/${createdRootMenu.Id}`)
    )

    await rootRow.getByRole('button', { name: '删除' }).click()
    await page.locator('.ant-popconfirm .ant-btn-primary').click()

    const deleteResponse = await deleteResponsePromise
    const deleteRequest = await deleteRequestPromise
    const deleteJson = await deleteResponse.json()
    expect(deleteResponse.status()).toBe(200)
    expect(deleteJson.code).toBe(200)

    requestUrl = deleteRequest.url()
    requestBody = deleteRequest.postData()
    responseStatus = deleteResponse.status()

    const deletedRootMenu = await findMenuByKey(rootMenuKey)
    const deletedChildMenu = await findMenuByKey(childMenuKey)
    dbResult = JSON.stringify({
      createdRootMenu,
      createdChildMenu,
      updatedRootMenu,
      sortedRootMenu,
      deletedRootMenu,
      deletedChildMenu
    })

    expect(deletedRootMenu).toBeNull()
    expect(deletedChildMenu).toBeNull()

    await logCheckResult({
      pageName: '菜单管理',
      buttonName: '新增根节点/新增子节点/编辑/排序/删除',
      actionType: 'crud_sort',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'menu_tree_created_updated_sorted_and_deleted',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '菜单管理',
      buttonName: '新增根节点/新增子节点/编辑/排序/删除',
      actionType: 'crud_sort',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'menu_tree_created_updated_sorted_and_deleted',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('菜单管理新增页面菜单后应自动刷新当前会话动态路由', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const menuKey = `${menuKeyPrefix}ROUTE_${uniqueSuffix}`
  const menuName = `E2E动态路由菜单${uniqueSuffix}`
  const routePath = `/access/e2e-dynamic-${uniqueSuffix}`
  const permissionCode = `page.access.dynamic.${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, MenuKey, MenuName, RoutePath, ComponentPath, PermissionCode FROM sys_menu WHERE MenuKey = '${menuKey}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await bootstrapAuthedPage(page, auth, '/access/menus')

    const createRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/access/menus')
    )
    const createResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/access/menus')
    )

    await page.getByRole('button', { name: '新增根节点' }).click()
    const menuModal = page.locator('.ant-modal').last()
    await expect(menuModal).toBeVisible()
    await menuModal.getByPlaceholder('如：employee-list').fill(menuKey)
    await menuModal.getByPlaceholder('请输入菜单名称').fill(menuName)
    await menuModal.getByPlaceholder('如：/employees').fill(routePath)
    await menuModal.getByPlaceholder('如：views/employee/EmployeeList.vue').fill('views/access/RoleManagement.vue')
    await menuModal.getByPlaceholder('如：page.employee.list').fill(permissionCode)
    await menuModal.locator('.ant-btn-primary').click()

    const createRequest = await createRequestPromise
    const createResponse = await createResponsePromise
    const createJson = await createResponse.json()

    requestUrl = createRequest.url()
    requestBody = createRequest.postData()
    responseStatus = createResponse.status()

    expect(responseStatus).toBe(200)
    expect(createJson.code).toBe(200)

    const createdMenu = await findMenuByKey(menuKey)
    expect(createdMenu).not.toBeNull()
    expect(createdMenu.RoutePath).toBe(routePath)
    expect(createdMenu.ComponentPath).toBe('views/access/RoleManagement.vue')
    expect(createdMenu.PermissionCode).toBe(permissionCode)

    await expect.poll(async () => {
      return page.evaluate((targetPermissionCode) => {
        const menuTree = JSON.parse(localStorage.getItem('menuTree') || '[]')
        const stack = [...menuTree]
        while (stack.length) {
          const current = stack.shift()
          if (!current) {
            continue
          }
          if (current.permissionCode === targetPermissionCode) {
            return true
          }
          if (current.children?.length) {
            stack.push(...current.children)
          }
        }
        return false
      }, permissionCode)
    }).toBe(true)

    await expect.poll(async () => {
      return page.evaluate((targetPermissionCode) => {
        const permissions = JSON.parse(localStorage.getItem('permissions') || '[]')
        return permissions.includes(targetPermissionCode)
      }, permissionCode)
    }).toBe(true)

    await page.evaluate((targetPath) => {
      window.history.pushState({}, '', targetPath)
      window.dispatchEvent(new PopStateEvent('popstate'))
    }, routePath)
    await expect(page).toHaveURL(new RegExp(`${routePath.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}$`))
    await expect(page.getByRole('main').getByText('角色管理', { exact: true })).toBeVisible()

    dbResult = JSON.stringify({
      createdMenu,
      routePath,
      permissionCode,
      currentUrl: page.url()
    })

    await logCheckResult({
      pageName: '菜单管理',
      buttonName: '新增页面菜单后动态路由刷新',
      actionType: 'dynamic_route',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'dynamic_route_registered_in_current_session',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '菜单管理',
      buttonName: '新增页面菜单后动态路由刷新',
      actionType: 'dynamic_route',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'dynamic_route_registered_in_current_session',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('角色管理应支持新增、编辑、分配权限、删除', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const roleCode = `${roleCodePrefix}${uniqueSuffix}`
  const roleName = `E2E角色${uniqueSuffix}`
  const updatedRoleName = `E2E角色已编辑${uniqueSuffix}`
  const targetMenu = await findMenuByPermissionCode('page.employee.list')
  const dbCheckSql = `SELECT Id, RoleCode, RoleName, IsActive FROM sys_role WHERE RoleCode = '${roleCode}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await bootstrapAuthedPage(page, auth, '/access/roles')

    const createRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/access/roles')
    )
    const createResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/access/roles')
    )

    await page.getByTestId('role-add').click()
    await page.locator('#role-code').fill(roleCode)
    await page.locator('#role-name').fill(roleName)
    await page.locator('#role-description').fill('E2E角色描述')
    await page.locator('.ant-modal .ant-btn-primary').last().click()

    const createRequest = await createRequestPromise
    const createResponse = await createResponsePromise
    const createJson = await createResponse.json()

    requestUrl = createRequest.url()
    requestBody = createRequest.postData()
    responseStatus = createResponse.status()

    expect(responseStatus).toBe(200)
    expect(createJson.code).toBe(200)

    const createdRole = await findRoleByCode(roleCode)
    expect(createdRole).not.toBeNull()
    expect(createdRole.RoleName).toBe(roleName)

    const updateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/roles')
    )
    const updateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/roles')
    )

    await page.getByTestId(`role-edit-${createdRole.Id}`).click()
    await page.locator('#role-name').fill(updatedRoleName)
    await page.locator('.ant-modal .ant-btn-primary').last().click()

    const updateResponse = await updateResponsePromise
    await updateRequestPromise
    const updateJson = await updateResponse.json()
    expect(updateResponse.status()).toBe(200)
    expect(updateJson.code).toBe(200)

    await page.getByTestId(`role-assign-${createdRole.Id}`).click()
    await expect(page.locator('.ant-drawer')).toBeVisible()
    await page.locator('.ant-tree-treenode').filter({ hasText: targetMenu.MenuName }).locator('.ant-tree-checkbox').first().click()

    const assignRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes(`/api/v1/access/roles/${createdRole.Id}/permissions`)
    )
    const assignResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes(`/api/v1/access/roles/${createdRole.Id}/permissions`)
    )
    await page.getByTestId('role-permission-save').click()

    const assignResponse = await assignResponsePromise
    await assignRequestPromise
    const assignJson = await assignResponse.json()
    expect(assignResponse.status()).toBe(200)
    expect(assignJson.code).toBe(200)

    const permissionMenuIds = await getRolePermissionMenuIds(createdRole.Id)
    expect(permissionMenuIds).toContain(targetMenu.Id)

    const deleteRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'DELETE' && candidate.url().includes(`/api/v1/access/roles/${createdRole.Id}`)
    )
    const deleteResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'DELETE' && candidate.url().includes(`/api/v1/access/roles/${createdRole.Id}`)
    )
    await page.getByTestId(`role-delete-${createdRole.Id}`).click()
    await page.locator('.ant-popconfirm .ant-btn-primary').click()

    const deleteResponse = await deleteResponsePromise
    await deleteRequestPromise
    const deleteJson = await deleteResponse.json()
    expect(deleteResponse.status()).toBe(200)
    expect(deleteJson.code).toBe(200)

    const deletedRole = await findRoleByCode(roleCode)
    dbResult = JSON.stringify({
      createdRole,
      permissionMenuIds,
      deletedRole
    })

    expect(deletedRole).toBeNull()

    await logCheckResult({
      pageName: '角色管理',
      buttonName: '新增/编辑/分配权限/删除',
      actionType: 'crud_assign',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'role_deleted_after_assign',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '角色管理',
      buttonName: '新增/编辑/分配权限/删除',
      actionType: 'crud_assign',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'role_deleted_after_assign',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('岗位管理应支持新增、编辑、分配权限、删除', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const postCode = `${postCodePrefix}${uniqueSuffix}`
  const postName = `E2E岗位${uniqueSuffix}`
  const updatedPostName = `E2E岗位已编辑${uniqueSuffix}`
  const targetMenu = await findMenuByPermissionCode('page.access.user')
  const dbCheckSql = `SELECT Id, PostCode, PostName, IsActive FROM sys_post WHERE PostCode = '${postCode}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await bootstrapAuthedPage(page, auth, '/access/posts')

    const createRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/access/posts')
    )
    const createResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/access/posts')
    )

    await page.getByTestId('post-add').click()
    await page.locator('#post-code').fill(postCode)
    await page.locator('#post-name').fill(postName)
    await page.locator('#post-description').fill('E2E岗位描述')
    await page.locator('.ant-modal .ant-btn-primary').last().click()

    const createRequest = await createRequestPromise
    const createResponse = await createResponsePromise
    const createJson = await createResponse.json()

    requestUrl = createRequest.url()
    requestBody = createRequest.postData()
    responseStatus = createResponse.status()

    expect(responseStatus).toBe(200)
    expect(createJson.code).toBe(200)

    const createdPost = await findPostByCode(postCode)
    expect(createdPost).not.toBeNull()
    expect(createdPost.PostName).toBe(postName)

    const updateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/posts')
    )
    const updateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/posts')
    )

    await page.getByTestId(`post-edit-${createdPost.Id}`).click()
    await page.locator('#post-name').fill(updatedPostName)
    await page.locator('.ant-modal .ant-btn-primary').last().click()

    const updateResponse = await updateResponsePromise
    await updateRequestPromise
    const updateJson = await updateResponse.json()
    expect(updateResponse.status()).toBe(200)
    expect(updateJson.code).toBe(200)

    await page.getByTestId(`post-assign-${createdPost.Id}`).click()
    await expect(page.locator('.ant-drawer')).toBeVisible()
    await page.locator('.ant-tree-treenode').filter({ hasText: targetMenu.MenuName }).locator('.ant-tree-checkbox').first().click()

    const assignRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes(`/api/v1/access/posts/${createdPost.Id}/permissions`)
    )
    const assignResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes(`/api/v1/access/posts/${createdPost.Id}/permissions`)
    )
    await page.getByTestId('post-permission-save').click()

    const assignResponse = await assignResponsePromise
    await assignRequestPromise
    const assignJson = await assignResponse.json()
    expect(assignResponse.status()).toBe(200)
    expect(assignJson.code).toBe(200)

    const permissionMenuIds = await getPostPermissionMenuIds(createdPost.Id)
    expect(permissionMenuIds).toContain(targetMenu.Id)

    const deleteRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'DELETE' && candidate.url().includes(`/api/v1/access/posts/${createdPost.Id}`)
    )
    const deleteResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'DELETE' && candidate.url().includes(`/api/v1/access/posts/${createdPost.Id}`)
    )
    await page.getByTestId(`post-delete-${createdPost.Id}`).click()
    await page.locator('.ant-popconfirm .ant-btn-primary').click()

    const deleteResponse = await deleteResponsePromise
    await deleteRequestPromise
    const deleteJson = await deleteResponse.json()
    expect(deleteResponse.status()).toBe(200)
    expect(deleteJson.code).toBe(200)

    const deletedPost = await findPostByCode(postCode)
    dbResult = JSON.stringify({
      createdPost,
      permissionMenuIds,
      deletedPost
    })

    expect(deletedPost).toBeNull()

    await logCheckResult({
      pageName: '岗位管理',
      buttonName: '新增/编辑/分配权限/删除',
      actionType: 'crud_assign',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'post_deleted_after_assign',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '岗位管理',
      buttonName: '新增/编辑/分配权限/删除',
      actionType: 'crud_assign',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'post_deleted_after_assign',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('角色管理应支持复制权限', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const sourceRole = await createRoleByApi(request, auth, {
    roleCode: `${roleCodePrefix}SRC_${uniqueSuffix}`,
    roleName: `E2E角色来源${uniqueSuffix}`,
    description: '角色复制权限来源',
    isActive: true
  })
  const targetRole = await createRoleByApi(request, auth, {
    roleCode: `${roleCodePrefix}TGT_${uniqueSuffix}`,
    roleName: `E2E角色目标${uniqueSuffix}`,
    description: '角色复制权限目标',
    isActive: true
  })
  const menuA = await findMenuByPermissionCode('page.employee.list')
  const menuB = await findMenuByPermissionCode('page.access.user')
  const dbCheckSql = `SELECT RoleId, MenuId FROM sys_role_permission WHERE RoleId = '${targetRole.id}' ORDER BY MenuId`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await assignRolePermissionsByApi(request, auth, sourceRole.id, [menuA.Id, menuB.Id])
    await bootstrapAuthedPage(page, auth, '/access/roles')

    const targetRow = page.locator('.ant-table-tbody tr').filter({ hasText: targetRole.roleCode }).first()
    await targetRow.getByRole('button', { name: '复制权限' }).click()

    const copyModal = page.locator('.ant-modal').last()
    await expect(copyModal).toBeVisible()
    await copyModal.locator('.ant-select-selector').click()
    await page.locator('.ant-select-dropdown').last().getByText(sourceRole.roleName, { exact: false }).click()

    const copyRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes(`/api/v1/access/roles/${targetRole.id}/permissions/copy`)
    )
    const copyResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes(`/api/v1/access/roles/${targetRole.id}/permissions/copy`)
    )
    await copyModal.locator('.ant-btn-primary').click()

    const copyRequest = await copyRequestPromise
    const copyResponse = await copyResponsePromise
    const copyJson = await copyResponse.json()

    requestUrl = copyRequest.url()
    requestBody = copyRequest.postData()
    responseStatus = copyResponse.status()

    expect(responseStatus).toBe(200)
    expect(copyJson.code).toBe(200)

    const targetPermissionMenuIds = await getRolePermissionMenuIds(targetRole.id)
    dbResult = JSON.stringify({
      sourceRoleId: sourceRole.id,
      targetRoleId: targetRole.id,
      targetPermissionMenuIds
    })

    expect(targetPermissionMenuIds).toEqual(expect.arrayContaining([menuA.Id, menuB.Id]))

    await logCheckResult({
      pageName: '角色管理',
      buttonName: '复制权限',
      actionType: 'copy_permission',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'target_role_permissions_copied',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '角色管理',
      buttonName: '复制权限',
      actionType: 'copy_permission',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'target_role_permissions_copied',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('岗位管理应支持复制权限', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const sourcePost = await createPostByApi(request, auth, {
    postCode: `${postCodePrefix}SRC_${uniqueSuffix}`,
    postName: `E2E岗位来源${uniqueSuffix}`,
    description: '岗位复制权限来源',
    isActive: true
  })
  const targetPost = await createPostByApi(request, auth, {
    postCode: `${postCodePrefix}TGT_${uniqueSuffix}`,
    postName: `E2E岗位目标${uniqueSuffix}`,
    description: '岗位复制权限目标',
    isActive: true
  })
  const menuA = await findMenuByPermissionCode('page.employee.list')
  const menuB = await findMenuByPermissionCode('page.access.user')
  const dbCheckSql = `SELECT PostId, MenuId FROM sys_post_permission WHERE PostId = '${targetPost.id}' ORDER BY MenuId`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await assignPostPermissionsByApi(request, auth, sourcePost.id, [menuA.Id, menuB.Id])
    await bootstrapAuthedPage(page, auth, '/access/posts')

    const targetRow = page.locator('.ant-table-tbody tr').filter({ hasText: targetPost.postCode }).first()
    await targetRow.getByRole('button', { name: '复制权限' }).click()

    const copyModal = page.locator('.ant-modal').last()
    await expect(copyModal).toBeVisible()
    await copyModal.locator('.ant-select-selector').click()
    await page.locator('.ant-select-dropdown').last().getByText(sourcePost.postName, { exact: false }).click()

    const copyRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes(`/api/v1/access/posts/${targetPost.id}/permissions/copy`)
    )
    const copyResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes(`/api/v1/access/posts/${targetPost.id}/permissions/copy`)
    )
    await copyModal.locator('.ant-btn-primary').click()

    const copyRequest = await copyRequestPromise
    const copyResponse = await copyResponsePromise
    const copyJson = await copyResponse.json()

    requestUrl = copyRequest.url()
    requestBody = copyRequest.postData()
    responseStatus = copyResponse.status()

    expect(responseStatus).toBe(200)
    expect(copyJson.code).toBe(200)

    const targetPermissionMenuIds = await getPostPermissionMenuIds(targetPost.id)
    dbResult = JSON.stringify({
      sourcePostId: sourcePost.id,
      targetPostId: targetPost.id,
      targetPermissionMenuIds
    })

    expect(targetPermissionMenuIds).toEqual(expect.arrayContaining([menuA.Id, menuB.Id]))

    await logCheckResult({
      pageName: '岗位管理',
      buttonName: '复制权限',
      actionType: 'copy_permission',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'target_post_permissions_copied',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '岗位管理',
      buttonName: '复制权限',
      actionType: 'copy_permission',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'target_post_permissions_copied',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('用户管理应展示角色和岗位合并后的生效权限来源', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const role = await createRoleByApi(request, auth, {
    roleCode: `${roleCodePrefix}CTX_${uniqueSuffix}`,
    roleName: `E2E角色来源展示${uniqueSuffix}`,
    description: '用户权限来源角色',
    isActive: true
  })
  const post = await createPostByApi(request, auth, {
    postCode: `${postCodePrefix}CTX_${uniqueSuffix}`,
    postName: `E2E岗位来源展示${uniqueSuffix}`,
    description: '用户权限来源岗位',
    isActive: true
  })
  const sharedMenu = await findMenuByPermissionCode('page.employee.list')
  const roleOnlyMenu = await findMenuByPermissionCode('page.access.role')
  const postOnlyMenu = await findMenuByPermissionCode('page.access.user')
  const username = `${usernamePrefix}ctx_${uniqueSuffix}`
  const dbCheckSql = `SELECT Id, Username, PostId FROM sys_user WHERE Username = '${username}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    await assignRolePermissionsByApi(request, auth, role.id, [sharedMenu.Id, roleOnlyMenu.Id])
    await assignPostPermissionsByApi(request, auth, post.id, [sharedMenu.Id, postOnlyMenu.Id])
    await createUserByApi(request, auth, {
      username,
      password: '123456',
      name: `E2E权限来源用户${uniqueSuffix}`,
      phone: `137${uniqueSuffix.slice(-8)}`,
      email: `${username}@e2e.test`,
      postId: post.id,
      roleIds: [role.id],
      isAdmin: false,
      isActive: true
    })

    await bootstrapAuthedPage(page, auth, '/access/users')

    const createdUser = await findAccessUserByUsername(username)
    expect(createdUser).not.toBeNull()

    const permissionResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'GET' && candidate.url().includes(`/api/v1/access/users/${createdUser.Id}/permissions`)
    )

    await page.getByTestId(`user-view-${createdUser.Id}`).click()
    const permissionResponse = await permissionResponsePromise
    const permissionJson = await permissionResponse.json()
    const permissionContext = permissionJson.data
    const permissionDrawer = page.locator('.ant-drawer').last()

    requestUrl = permissionResponse.url()
    responseStatus = permissionResponse.status()

    expect(responseStatus).toBe(200)
    expect(permissionJson.code).toBe(200)
    expect(permissionContext.rolePermissionMenuIds).toEqual(expect.arrayContaining([sharedMenu.Id, roleOnlyMenu.Id]))
    expect(permissionContext.postPermissionMenuIds).toEqual(expect.arrayContaining([sharedMenu.Id, postOnlyMenu.Id]))
    expect(permissionContext.grantedMenuIds).toEqual(expect.arrayContaining([sharedMenu.Id, roleOnlyMenu.Id, postOnlyMenu.Id]))

    await expect(permissionDrawer).toBeVisible()
    await expect(permissionDrawer.getByText(role.roleName, { exact: true }).first()).toBeVisible()
    await expect(permissionDrawer.getByText(post.postName, { exact: true }).first()).toBeVisible()
    await expect(permissionDrawer.getByText(sharedMenu.PermissionCode, { exact: true })).toBeVisible()
    await expect(permissionDrawer.getByText(roleOnlyMenu.PermissionCode, { exact: true })).toBeVisible()
    await expect(permissionDrawer.getByText(postOnlyMenu.PermissionCode, { exact: true })).toBeVisible()

    const userRoleIds = await getUserRoleIds(createdUser.Id)
    dbResult = JSON.stringify({
      userId: createdUser.Id,
      roleId: role.id,
      postId: post.id,
      userRoleIds,
      rolePermissionMenuIds: permissionContext.rolePermissionMenuIds,
      postPermissionMenuIds: permissionContext.postPermissionMenuIds,
      grantedMenuIds: permissionContext.grantedMenuIds
    })

    expect(userRoleIds).toContain(role.id)

    await logCheckResult({
      pageName: '用户管理',
      buttonName: '查看权限来源',
      actionType: 'permission_context',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'permission_context_contains_role_and_post_sources',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '用户管理',
      buttonName: '查看权限来源',
      actionType: 'permission_context',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'permission_context_contains_role_and_post_sources',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})

test('用户管理应支持新增、编辑、查看权限、删除', async ({ page, request }) => {
  const auth = await loginAsAdmin(request)
  const uniqueSuffix = Date.now().toString()
  const roleCode = `${roleCodePrefix}${uniqueSuffix}`
  const postCode = `${postCodePrefix}${uniqueSuffix}`
  const username = `${usernamePrefix}${uniqueSuffix}`
  const updatedName = `E2E用户已编辑${uniqueSuffix}`
  const targetMenu = await findMenuByPermissionCode('page.employee.list')
  const dbCheckSql = `SELECT Id, Username, Name, PostId, IsActive FROM sys_user WHERE Username = '${username}' LIMIT 1`

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const role = await createRoleByApi(request, auth, {
      roleCode,
      roleName: `E2E角色给用户${uniqueSuffix}`,
      description: '用户管理E2E角色',
      isActive: true
    })
    const post = await createPostByApi(request, auth, {
      postCode,
      postName: `E2E岗位给用户${uniqueSuffix}`,
      description: '用户管理E2E岗位',
      isActive: true
    })
    await assignRolePermissionsByApi(request, auth, role.id, [targetMenu.Id])
    await assignPostPermissionsByApi(request, auth, post.id, [targetMenu.Id])

    await bootstrapAuthedPage(page, auth, '/access/users')

    const createRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'POST' && candidate.url().includes('/api/v1/access/users')
    )
    const createResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'POST' && candidate.url().includes('/api/v1/access/users')
    )

    await page.getByTestId('user-add').click()
    await page.locator('#access-username').fill(username)
    await page.locator('#access-password').fill('123456')
    await page.locator('#access-name').fill(`E2E用户${uniqueSuffix}`)
    await page.locator('#access-phone').fill(`139${uniqueSuffix.slice(-8)}`)
    await page.locator('#access-email').fill(`${username}@e2e.test`)
    await page.locator('#access-post').click()
    await page.locator('.ant-select-dropdown').last().getByText(post.postName).click()
    await page.locator('#access-roles').click()
    await page.locator('.ant-select-dropdown').last().getByText(role.roleName).click()
    await page.locator('.ant-modal .ant-btn-primary').last().click()

    const createRequest = await createRequestPromise
    const createResponse = await createResponsePromise
    const createJson = await createResponse.json()

    requestUrl = createRequest.url()
    requestBody = createRequest.postData()
    responseStatus = createResponse.status()

    expect(responseStatus).toBe(200)
    expect(createJson.code).toBe(200)

    const createdUser = await findAccessUserByUsername(username)
    expect(createdUser).not.toBeNull()
    expect(createdUser.PostId).toBe(post.id)
    let userRoleIds = await getUserRoleIds(createdUser.Id)
    expect(userRoleIds).toContain(role.id)

    const updateRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'PUT' && candidate.url().includes('/api/v1/access/users')
    )
    const updateResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'PUT' && candidate.url().includes('/api/v1/access/users')
    )

    await page.getByTestId(`user-edit-${createdUser.Id}`).click()
    await page.locator('#access-name').fill(updatedName)
    await page.locator('#access-phone').fill(`138${uniqueSuffix.slice(-8)}`)
    await page.locator('.ant-modal .ant-btn-primary').last().click()

    const updateResponse = await updateResponsePromise
    await updateRequestPromise
    const updateJson = await updateResponse.json()
    expect(updateResponse.status()).toBe(200)
    expect(updateJson.code).toBe(200)

    await page.getByTestId(`user-view-${createdUser.Id}`).click()
    const permissionDrawer = page.locator('.ant-drawer').last()
    await expect(permissionDrawer).toBeVisible()
    await expect(permissionDrawer.getByText(role.roleName, { exact: true }).first()).toBeVisible()
    await expect(permissionDrawer.getByText(post.postName, { exact: true }).first()).toBeVisible()
    await expect(permissionDrawer.getByText('page.employee.list', { exact: true })).toBeVisible()
    await page.keyboard.press('Escape')

    const deleteRequestPromise = page.waitForRequest((candidate) =>
      candidate.method() === 'DELETE' && candidate.url().includes(`/api/v1/access/users/${createdUser.Id}`)
    )
    const deleteResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'DELETE' && candidate.url().includes(`/api/v1/access/users/${createdUser.Id}`)
    )
    await page.getByTestId(`user-delete-${createdUser.Id}`).click()
    await page.locator('.ant-popconfirm .ant-btn-primary').click()

    const deleteResponse = await deleteResponsePromise
    await deleteRequestPromise
    const deleteJson = await deleteResponse.json()
    expect(deleteResponse.status()).toBe(200)
    expect(deleteJson.code).toBe(200)

    const deletedUser = await findAccessUserByUsername(username)
    userRoleIds = await getUserRoleIds(createdUser.Id)
    dbResult = JSON.stringify({
      createdUser,
      userRoleIds,
      deletedUser
    })

    expect(deletedUser).toBeNull()
    expect(userRoleIds).toHaveLength(0)

    await logCheckResult({
      pageName: '用户管理',
      buttonName: '新增/编辑/查看权限/删除',
      actionType: 'crud_view',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'user_deleted_after_view',
      dbActualResult: dbResult,
      passed: true
    })
  } catch (error) {
    await logCheckResult({
      pageName: '用户管理',
      buttonName: '新增/编辑/查看权限/删除',
      actionType: 'crud_view',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'user_deleted_after_view',
      dbActualResult: dbResult ?? String(error),
      passed: false
    })

    throw error
  }
})
