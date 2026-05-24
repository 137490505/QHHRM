<template>
  <div class="page-container">
    <div class="page-header">
      <h1>系统设置</h1>
      <p>维护登录安全相关配置，保存后立即生效。</p>
    </div>

    <a-card title="登录安全" :bordered="false" :loading="loading">
      <div class="setting-row">
        <div class="setting-content">
          <div class="setting-title">启用登录验证码</div>
          <div class="setting-desc">
            关闭后，登录页不再展示验证码，后端登录接口也不再校验验证码。
          </div>
        </div>
        <a-switch
          v-model:checked="form.captchaEnabled"
          checked-children="开启"
          un-checked-children="关闭"
        />
      </div>

      <div class="actions">
        <a-button @click="loadSettings">重新加载</a-button>
        <a-button type="primary" :loading="saving" @click="saveSettings">保存设置</a-button>
      </div>
    </a-card>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { settingsApi } from '../../api'

const loading = ref(false)
const saving = ref(false)
const form = reactive({
  captchaEnabled: false
})

const loadSettings = async () => {
  loading.value = true
  try {
    const data = await settingsApi.getLoginSecuritySettings()
    form.captchaEnabled = !!data.captchaEnabled
  } finally {
    loading.value = false
  }
}

const saveSettings = async () => {
  saving.value = true
  try {
    const data = await settingsApi.updateLoginSecuritySettings({
      captchaEnabled: form.captchaEnabled
    })
    form.captchaEnabled = !!data.captchaEnabled
    message.success('设置已保存并立即生效')
  } finally {
    saving.value = false
  }
}

onMounted(loadSettings)
</script>

<style scoped>
.page-container {
  padding: 24px;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h1 {
  margin: 0 0 8px;
  font-size: 20px;
  font-weight: 600;
}

.page-header p {
  margin: 0;
  color: #8c8c8c;
}

.setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 12px 0 24px;
}

.setting-content {
  flex: 1;
}

.setting-title {
  margin-bottom: 6px;
  font-size: 16px;
  font-weight: 600;
  color: #262626;
}

.setting-desc {
  color: #8c8c8c;
  line-height: 1.6;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid #f0f0f0;
}
</style>
