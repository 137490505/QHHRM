import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 120000,
  expect: {
    timeout: 15000
  },
  fullyParallel: false,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: 'http://127.0.0.1:3001',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  webServer: [
    {
      command: 'node ./tests/e2e/start-backend.mjs',
      url: 'http://127.0.0.1:5000/api/v1/auth/login-options',
      reuseExistingServer: true,
      timeout: 120000
    },
    {
      command: 'node ./tests/e2e/start-frontend.mjs',
      url: 'http://127.0.0.1:3001/login',
      reuseExistingServer: true,
      timeout: 120000
    }
  ],
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
})
