import { expect, test } from '@playwright/test'
import { ensureTestCheckLogTable, getConfigParam, insertTestCheckLog, setLoginCaptchaEnabled } from './db.mjs'

test.beforeEach(async () => {
  await ensureTestCheckLogTable()
  await setLoginCaptchaEnabled(false)
})

test('登录安全配置开启后登录页应立即展示验证码', async ({ page, request }) => {
  const dbCheckSql = "SELECT ParamValue FROM sys_config_param WHERE Category = 'security' AND ParamKey = 'loginCaptchaEnabled' LIMIT 1"

  let requestUrl = null
  let requestBody = null
  let responseStatus = null
  let dbResult = null

  try {
    const updateResponse = await request.put('http://127.0.0.1:5000/api/v1/settings/login-security', {
      headers: {
        Authorization: `Bearer ${(await loginAsAdmin(request)).token}`
      },
      data: {
        captchaEnabled: true
      }
    })

    requestUrl = updateResponse.url()
    requestBody = JSON.stringify({ captchaEnabled: true })
    responseStatus = updateResponse.status()

    expect(updateResponse.ok()).toBeTruthy()
    const updateJson = await updateResponse.json()
    expect(responseStatus).toBe(200)
    expect(updateJson.code).toBe(200)

    const dbRow = await getConfigParam('security', 'loginCaptchaEnabled')
    dbResult = JSON.stringify(dbRow)

    expect(dbRow).not.toBeNull()
    expect(String(dbRow.ParamValue)).toBe('true')

    const loginOptionsResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'GET' && candidate.url().includes('/api/v1/auth/login-options')
    )
    const captchaResponsePromise = page.waitForResponse((candidate) =>
      candidate.request().method() === 'GET' && candidate.url().includes('/api/v1/captcha')
    )

    await page.goto('/login')

    const loginOptionsResponse = await loginOptionsResponsePromise
    const captchaResponse = await captchaResponsePromise

    expect(loginOptionsResponse.ok()).toBeTruthy()
    expect(captchaResponse.ok()).toBeTruthy()
    await expect(page.locator('#login-captcha')).toBeVisible()

    await insertTestCheckLog({
      pageName: '系统设置/登录页',
      buttonName: '保存设置',
      actionType: 'edit',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'param_value_eq_true',
      dbActualResult: dbResult,
      passed: true,
      fixAttempts: 0
    })
  } catch (error) {
    await insertTestCheckLog({
      pageName: '系统设置/登录页',
      buttonName: '保存设置',
      actionType: 'edit',
      requestUrl,
      requestBody,
      responseStatus,
      dbCheckSql,
      dbExpectedChange: 'param_value_eq_true',
      dbActualResult: dbResult ?? String(error),
      passed: false,
      fixAttempts: 0
    })

    throw error
  } finally {
    await setLoginCaptchaEnabled(false)
  }
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
