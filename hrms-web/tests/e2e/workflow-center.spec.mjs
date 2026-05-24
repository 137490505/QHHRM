import { expect, test } from '@playwright/test'
import { ensureTestCheckLogTable, insertTestCheckLog } from './db.mjs'

test.beforeEach(async () => {
  await ensureTestCheckLogTable()
})

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

test.describe('待办中心', () => {
  test('访问待办中心页面', async ({ page, request }) => {
    let requestUrl = null
    let responseStatus = null

    try {
      await loginAsAdmin(request)

      await page.goto('/workflow/todo')

      await expect(page.locator('.ant-layout-content')).toBeVisible()
      await expect(page.locator('text=待办中心')).toBeVisible()

      await insertTestCheckLog({
        pageName: '待办中心',
        buttonName: '访问页面',
        actionType: 'view',
        passed: true,
        fixAttempts: 0
      })
    } catch (error) {
      await insertTestCheckLog({
        pageName: '待办中心',
        buttonName: '访问页面',
        actionType: 'view',
        dbActualResult: String(error),
        passed: false,
        fixAttempts: 0
      })
      throw error
    }
  })

  test('获取我的待办任务列表', async ({ page, request }) => {
    try {
      const { token } = await loginAsAdmin(request)

      const response = await request.get('http://127.0.0.1:5000/api/v1/todo/tasks/my-tasks', {
        headers: { Authorization: `Bearer ${token}` }
      })

      expect(response.ok()).toBeTruthy()
      const body = await response.json()
      expect(body.code).toBe(200)

      await insertTestCheckLog({
        pageName: '待办中心/待办任务列表',
        buttonName: '查询',
        actionType: 'query',
        requestUrl: response.url(),
        responseStatus: response.status(),
        passed: true,
        fixAttempts: 0
      })
    } catch (error) {
      await insertTestCheckLog({
        pageName: '待办中心/待办任务列表',
        buttonName: '查询',
        actionType: 'query',
        dbActualResult: String(error),
        passed: false,
        fixAttempts: 0
      })
      throw error
    }
  })

  test('获取KPI汇总数据', async ({ request }) => {
    try {
      const { token } = await loginAsAdmin(request)

      const response = await request.get('http://127.0.0.1:5000/api/v1/todo/kpi/summary', {
        headers: { Authorization: `Bearer ${token}` }
      })

      expect(response.ok()).toBeTruthy()
      const body = await response.json()
      expect(body.code).toBe(200)

      await insertTestCheckLog({
        pageName: '待办中心/KPI汇总',
        buttonName: '获取KPI',
        actionType: 'query',
        requestUrl: response.url(),
        responseStatus: response.status(),
        passed: true,
        fixAttempts: 0
      })
    } catch (error) {
      await insertTestCheckLog({
        pageName: '待办中心/KPI汇总',
        buttonName: '获取KPI',
        actionType: 'query',
        dbActualResult: String(error),
        passed: false,
        fixAttempts: 0
      })
      throw error
    }
  })
})

test.describe('流程中心', () => {
  test('访问流程中心页面', async ({ page, request }) => {
    try {
      await loginAsAdmin(request)

      await page.goto('/workflow/processes')

      await expect(page.locator('.ant-layout-content')).toBeVisible()
      await expect(page.locator('text=流程中心')).toBeVisible()

      await insertTestCheckLog({
        pageName: '流程中心',
        buttonName: '访问页面',
        actionType: 'view',
        passed: true,
        fixAttempts: 0
      })
    } catch (error) {
      await insertTestCheckLog({
        pageName: '流程中心',
        buttonName: '访问页面',
        actionType: 'view',
        dbActualResult: String(error),
        passed: false,
        fixAttempts: 0
      })
      throw error
    }
  })

  test('获取流程定义列表', async ({ request }) => {
    try {
      const { token } = await loginAsAdmin(request)

      const response = await request.get('http://127.0.0.1:5000/api/v1/process/definitions', {
        headers: { Authorization: `Bearer ${token}` }
      })

      expect(response.ok()).toBeTruthy()
      const body = await response.json()
      expect(body.code).toBe(200)

      await insertTestCheckLog({
        pageName: '流程中心/流程定义',
        buttonName: '查询',
        actionType: 'query',
        requestUrl: response.url(),
        responseStatus: response.status(),
        passed: true,
        fixAttempts: 0
      })
    } catch (error) {
      await insertTestCheckLog({
        pageName: '流程中心/流程定义',
        buttonName: '查询',
        actionType: 'query',
        dbActualResult: String(error),
        passed: false,
        fixAttempts: 0
      })
      throw error
    }
  })

  test('获取流程实例列表', async ({ request }) => {
    try {
      const { token } = await loginAsAdmin(request)

      const response = await request.get('http://127.0.0.1:5000/api/v1/process/instances', {
        headers: { Authorization: `Bearer ${token}` }
      })

      expect(response.ok()).toBeTruthy()
      const body = await response.json()
      expect(body.code).toBe(200)

      await insertTestCheckLog({
        pageName: '流程中心/流程实例',
        buttonName: '查询',
        actionType: 'query',
        requestUrl: response.url(),
        responseStatus: response.status(),
        passed: true,
        fixAttempts: 0
      })
    } catch (error) {
      await insertTestCheckLog({
        pageName: '流程中心/流程实例',
        buttonName: '查询',
        actionType: 'query',
        dbActualResult: String(error),
        passed: false,
        fixAttempts: 0
      })
      throw error
    }
  })
})

test.describe('待办中心API测试', () => {
  test('待办任务API - 获取任务详情', async ({ request }) => {
    const { token } = await loginAsAdmin(request)
    const response = await request.get('http://127.0.0.1:5000/api/v1/todo/tasks/1/detail', {
      headers: { Authorization: `Bearer ${token}` }
    })

    await insertTestCheckLog({
      pageName: '待办中心/API',
      buttonName: '获取任务详情',
      actionType: 'query',
      requestUrl: response.url(),
      responseStatus: response.status(),
      passed: response.ok(),
      fixAttempts: 0
    })

    expect(response.status()).toBe(200)
  })

  test('待办任务API - 完成任务', async ({ request }) => {
    const { token } = await loginAsAdmin(request)
    const response = await request.put('http://127.0.0.1:5000/api/v1/todo/tasks/1/complete', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        operatorId: 'test',
        operatorName: '测试用户',
        comment: '测试完成'
      }
    })

    await insertTestCheckLog({
      pageName: '待办中心/API',
      buttonName: '完成任务',
      actionType: 'edit',
      requestUrl: response.url(),
      responseStatus: response.status(),
      passed: response.ok(),
      fixAttempts: 0
    })

    expect(response.status()).toBe(200)
  })

  test('待办任务API - 驳回任务', async ({ request }) => {
    const { token } = await loginAsAdmin(request)
    const response = await request.put('http://127.0.0.1:5000/api/v1/todo/tasks/1/reject', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        operatorId: 'test',
        operatorName: '测试用户',
        comment: '测试驳回'
      }
    })

    await insertTestCheckLog({
      pageName: '待办中心/API',
      buttonName: '驳回任务',
      actionType: 'edit',
      requestUrl: response.url(),
      responseStatus: response.status(),
      passed: response.ok(),
      fixAttempts: 0
    })

    expect(response.status()).toBe(200)
  })

  test('待办任务API - 催办任务', async ({ request }) => {
    const { token } = await loginAsAdmin(request)
    const response = await request.post('http://127.0.0.1:5000/api/v1/todo/tasks/1/urge', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        operatorId: 'test',
        operatorName: '测试用户',
        comment: '测试催办'
      }
    })

    await insertTestCheckLog({
      pageName: '待办中心/API',
      buttonName: '催办任务',
      actionType: 'edit',
      requestUrl: response.url(),
      responseStatus: response.status(),
      passed: response.ok(),
      fixAttempts: 0
    })

    expect(response.status()).toBe(200)
  })
})

test.describe('流程中心API测试', () => {
  test('流程API - 获取流程实例详情', async ({ request }) => {
    const { token } = await loginAsAdmin(request)
    const response = await request.get('http://127.0.0.1:5000/api/v1/process/instance/1', {
      headers: { Authorization: `Bearer ${token}` }
    })

    await insertTestCheckLog({
      pageName: '流程中心/API',
      buttonName: '获取流程实例详情',
      actionType: 'query',
      requestUrl: response.url(),
      responseStatus: response.status(),
      passed: response.ok(),
      fixAttempts: 0
    })

    expect(response.status()).toBe(200)
  })

  test('流程API - 终止流程实例', async ({ request }) => {
    const { token } = await loginAsAdmin(request)
    const response = await request.post('http://127.0.0.1:5000/api/v1/process/instance/1/terminate', {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        operatorId: 'test',
        operatorName: '测试用户',
        comment: '测试终止流程'
      }
    })

    await insertTestCheckLog({
      pageName: '流程中心/API',
      buttonName: '终止流程实例',
      actionType: 'edit',
      requestUrl: response.url(),
      responseStatus: response.status(),
      passed: response.ok(),
      fixAttempts: 0
    })

    expect(response.status()).toBe(200)
  })
})
