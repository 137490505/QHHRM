<template>
  <div class="process-requests-page">
    <a-page-header title="流程申请" sub-title="选择要申请的流程类型">
      <template #extra>
        <a-button type="primary" @click="refreshDefinitions">
          <template #icon>
            <ReloadOutlined />
          </template>
          刷新
        </a-button>
      </template>
    </a-page-header>

    <a-card class="definitions-card">
      <a-spin :spinning="loading">
        <a-row :gutter="[16, 16]">
          <a-col :xs="24" :sm="12" :md="8" :lg="6" v-for="definition in definitions" :key="definition.id">
            <a-card
              hoverable
              class="definition-card"
              @click="startProcess(definition)"
            >
              <template #cover>
                <div class="card-cover">
                  <FileTextOutlined class="cover-icon" />
                </div>
              </template>
              <a-card-meta :title="definition.processName" :description="`编号：${definition.processCode}`">
              </a-card-meta>
              <div class="card-footer">
                <a-tag :color="getStatusColor(definition.status)">{{ getStatusText(definition.status) }}</a-tag>
                <a-button type="primary" size="small" @click.stop="startProcess(definition)">
                  立即申请
                </a-button>
              </div>
            </a-card>
          </a-col>
        </a-row>
        <a-empty v-if="!loading && definitions.length === 0" description="暂无可申请的流程">
          <a-button type="primary" @click="refreshDefinitions">刷新</a-button>
        </a-empty>
      </a-spin>
    </a-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { ReloadOutlined, FileTextOutlined } from '@ant-design/icons-vue'
import { processCenterApi } from '../../api'

const router = useRouter()
const loading = ref(false)
const definitions = ref([])

const loadDefinitions = async () => {
  loading.value = true
  try {
    const data = await processCenterApi.getDefinitions()
    definitions.value = data.filter(d => d.status === 'Published')
  } catch (error) {
    message.error('加载流程定义失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

const refreshDefinitions = () => {
  loadDefinitions()
}

const startProcess = (definition) => {
  router.push(`/workflow/requests/${definition.processCode}`)
}

const getStatusColor = (status) => {
  switch (status) {
    case 'Published':
      return 'green'
    case 'Draft':
      return 'default'
    case 'Deactivated':
      return 'red'
    default:
      return 'default'
  }
}

const getStatusText = (status) => {
  switch (status) {
    case 'Published':
      return '已发布'
    case 'Draft':
      return '草稿'
    case 'Deactivated':
      return '已停用'
    default:
      return status
  }
}

onMounted(() => {
  loadDefinitions()
})
</script>

<style scoped>
.process-requests-page {
  padding: 16px;
}

.definitions-card {
  margin-top: 16px;
}

.definition-card {
  cursor: pointer;
  transition: all 0.3s;
}

.definition-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.card-cover {
  height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.cover-icon {
  font-size: 48px;
  color: white;
}

.card-footer {
  margin-top: 12px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
