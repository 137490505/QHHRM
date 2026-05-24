<template>
  <div class="login-container">
    <a-card class="login-card">
      <div class="logo-section">
        <div class="logo">
          <UserOutlined class="logo-icon" />
        </div>
        <h2>人力资源管理系统</h2>
        <p class="subtitle">Human Resource Management System</p>
      </div>

      <a-form
        :model="form"
        @finish="handleLogin"
      >
        <a-form-item
          name="username"
          :rules="[{ required: true, message: '请输入用户名/手机号/工号' }]"
        >
          <a-input
            v-model:value="form.username"
            placeholder="用户名/手机号/工号"
            size="large"
            :disabled="isLocked"
            id="login-username"
          >
            <template #prefix><UserOutlined /></template>
          </a-input>
        </a-form-item>

        <a-form-item
          name="password"
          :rules="[{ required: true, message: '请输入密码' }]"
        >
          <a-input-password
            v-model:value="form.password"
            placeholder="请输入密码"
            size="large"
            :disabled="isLocked"
            id="login-password"
          >
            <template #prefix><LockOutlined /></template>
          </a-input-password>
        </a-form-item>

        <a-form-item
          v-if="captchaEnabled"
          name="captcha"
          :rules="[{ required: true, message: '请输入验证码' }]"
        >
          <a-row :gutter="8">
            <a-col :span="16">
              <a-input
                v-model:value="form.captcha"
                placeholder="验证码"
                size="large"
                :disabled="isLocked"
                id="login-captcha"
              >
                <template #prefix><CodeOutlined /></template>
              </a-input>
            </a-col>
            <a-col :span="8">
              <div
                class="captcha-img"
                @click="refreshCaptcha"
                v-html="captchaSvg"
              ></div>
            </a-col>
          </a-row>
        </a-form-item>

        <a-form-item class="remember-section">
          <a-checkbox v-model:checked="rememberMe" :disabled="isLocked" id="login-remember">
            记住密码
          </a-checkbox>
          <a-button type="text" @click="handleForgotPassword">
            忘记密码
          </a-button>
        </a-form-item>

        <a-alert
          v-if="isLocked"
          type="error"
          message="账户已锁定"
          :description="`请在 ${remainingMinutes} 分钟后重试`"
          show-icon
        />

        <a-form-item>
          <a-button type="primary" html-type="submit" size="large" block :loading="loading" :disabled="isLocked">
            登录
          </a-button>
        </a-form-item>
      </a-form>
    </a-card>
  </div>
</template>

<script setup>
import { reactive, ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { UserOutlined, LockOutlined, CodeOutlined } from '@ant-design/icons-vue'
import { useUserStore } from '../store/user'
import { authApi } from '../api/auth'
import { registerDynamicRoutes } from '../router'

const router = useRouter()
const userStore = useUserStore()
const REMEMBER_LOGIN_KEY = 'hrms_remember_login'
const LEGACY_REMEMBER_USER_KEY = 'hrms_remember_user'

const form = reactive({
  username: '',
  password: '',
  captcha: ''
})

const loading = ref(false)
const rememberMe = ref(false)
const captchaEnabled = ref(false)
const captchaSvg = ref('')
const errorCount = ref(0)
const lockTime = ref(null)

const isLocked = computed(() => {
  if (!lockTime.value) return false
  const now = Date.now()
  const lockDuration = 15 * 60 * 1000
  return now < lockTime.value + lockDuration
})

const remainingMinutes = computed(() => {
  if (!lockTime.value) return 0
  const now = Date.now()
  const lockDuration = 15 * 60 * 1000
  const remaining = Math.ceil((lockTime.value + lockDuration - now) / (60 * 1000))
  return remaining > 0 ? remaining : 0
})

const loadLoginOptions = async () => {
  try {
    const options = await authApi.getLoginOptions()
    captchaEnabled.value = !!options.captchaEnabled
    if (captchaEnabled.value) {
      await refreshCaptcha()
    } else {
      captchaSvg.value = ''
      form.captcha = ''
    }
  } catch (error) {
    // 无法获取登录选项时，默认启用验证码，避免弱化登录安全。
    captchaEnabled.value = true
    await refreshCaptcha()
  }
}

const refreshCaptcha = async () => {
  if (!captchaEnabled.value) {
    captchaSvg.value = ''
    form.captcha = ''
    return
  }

  try {
    const response = await fetch('/api/v1/captcha', {
      credentials: 'include',
      cache: 'no-store'
    })
    if (!response.ok) {
      throw new Error('获取验证码失败')
    }
    const svg = await response.text()
    captchaSvg.value = svg
    form.captcha = ''
  } catch (error) {
    message.error('获取验证码失败')
  }
}

const handleForgotPassword = () => {
  message.info('忘记密码功能开发中')
}

const saveRememberedLogin = () => {
  if (rememberMe.value) {
    localStorage.setItem(REMEMBER_LOGIN_KEY, JSON.stringify({
      username: form.username,
      password: form.password
    }))
    localStorage.setItem(LEGACY_REMEMBER_USER_KEY, form.username)
    return
  }

  localStorage.removeItem(REMEMBER_LOGIN_KEY)
  localStorage.removeItem(LEGACY_REMEMBER_USER_KEY)
}

const restoreRememberedLogin = () => {
  const savedLogin = localStorage.getItem(REMEMBER_LOGIN_KEY)
  if (savedLogin) {
    try {
      const parsedLogin = JSON.parse(savedLogin)
      form.username = parsedLogin?.username || ''
      form.password = parsedLogin?.password || ''
      rememberMe.value = !!(form.username || form.password)
      return
    } catch {
      localStorage.removeItem(REMEMBER_LOGIN_KEY)
    }
  }

  const savedUser = localStorage.getItem(LEGACY_REMEMBER_USER_KEY)
  if (savedUser) {
    form.username = savedUser
    rememberMe.value = true
  }
}

const handleLogin = async () => {
  if (loading.value) {
    return
  }

  if (isLocked.value) {
    message.error('账户已锁定')
    return
  }

  loading.value = true
  try {
    const response = await authApi.login({
      username: form.username,
      password: form.password,
      captcha: form.captcha
    })

    userStore.setAuthContext(response)
    registerDynamicRoutes(response.menus)
    saveRememberedLogin()

    errorCount.value = 0
    lockTime.value = null
    message.success('登录成功')
    router.push(userStore.firstPagePath)
  } catch (error) {
    const serverMessage = error?.response?.data?.message || error?.message || ''
    const isCaptchaError = serverMessage.includes('验证码')

    if (!isCaptchaError) {
      errorCount.value++
    }

    if (!isCaptchaError && errorCount.value >= 5) {
      lockTime.value = Date.now()
      message.error('账户已锁定，请15分钟后重试')
    }
    if (captchaEnabled.value) {
      await refreshCaptcha()
    }
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  restoreRememberedLogin()
  await loadLoginOptions()
})
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 420px;
  border-radius: 12px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
}

.logo-section {
  text-align: center;
  margin-bottom: 32px;
}

.logo {
  width: 72px;
  height: 72px;
  margin: 0 auto 16px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.logo-icon {
  font-size: 36px;
  color: #fff;
}

.logo-section h2 {
  margin: 0 0 8px;
  color: #333;
}

.subtitle {
  margin: 0;
  color: #666;
  font-size: 14px;
}

.remember-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.captcha-img {
  width: 100%;
  height: 40px;
  border-radius: 6px;
  cursor: pointer;
  background-color: #f3f4f6;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.captcha-img :deep(svg) {
  width: 100%;
  height: 100%;
}

:deep(.ant-alert) {
  margin-bottom: 16px;
}
</style>
