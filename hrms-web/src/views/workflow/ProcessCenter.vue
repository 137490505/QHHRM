<template>
  <div class="process-center">
    <a-card size="small" class="definition-card">
      <template #title>流程定义</template>
      <a-space wrap>
        <a-tag
          v-for="item in definitions"
          :key="item.id"
          :color="queryForm.processCode === item.processCode ? 'processing' : 'default'"
          class="definition-tag"
          @click="selectProcessCode(item.processCode)"
        >
          {{ item.processName }} / {{ item.processCode }} / V{{ item.versionNo }}
        </a-tag>
      </a-space>
    </a-card>

    <a-card size="small" class="query-card">
      <a-form layout="inline">
        <a-form-item label="流程编码">
          <a-select v-model:value="queryForm.processCode" allow-clear placeholder="全部流程" style="width: 220px">
            <a-select-option v-for="item in definitions" :key="item.id" :value="item.processCode">
              {{ item.processCode }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="状态">
          <a-select v-model:value="queryForm.status" allow-clear placeholder="全部状态" style="width: 160px">
            <a-select-option v-for="item in statusOptions" :key="item.value" :value="item.value">
              {{ item.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="业务类型">
          <a-select v-model:value="queryForm.businessType" allow-clear placeholder="全部业务" style="width: 160px">
            <a-select-option value="PayrollRun">PayrollRun</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="业务单号">
          <a-input v-model:value="queryForm.businessId" allow-clear placeholder="如 RUN-2026-05" style="width: 220px" />
        </a-form-item>
        <a-form-item label="关键字">
          <a-input v-model:value="queryForm.keyword" allow-clear placeholder="实例号/标题/业务单号" style="width: 240px" />
        </a-form-item>
        <a-form-item>
          <a-space>
            <a-button type="primary" @click="loadInstances">查询</a-button>
            <a-button @click="handleReset">重置</a-button>
            <a-button @click="loadAll">
              <SyncOutlined />
              刷新
            </a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>

    <a-table
      row-key="id"
      :loading="loading"
      :columns="columns"
      :data-source="instances"
      :pagination="{ pageSize: 10, showSizeChanger: true }"
      :scroll="{ x: 1380 }"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'currentTodo'">
          {{ record.currentTodo?.assigneeName || '-' }}
        </template>
        <template v-else-if="column.key === 'startedTime' || column.key === 'finishedTime'">
          {{ formatDateTime(record[column.key]) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="openDetailPage(record.id)">
              查看
            </a-button>
            <a-button
              v-if="record.status === 'Running'"
              v-permission="'button.process.reject'"
              type="link"
              size="small"
              danger
              @click="handleReject(record)"
            >
              驳回
            </a-button>
            <a-button
              v-if="record.status === 'Running'"
              v-permission="'button.process.rebuildTodo'"
              type="link"
              size="small"
              @click="handleRebuildTodo(record)"
            >
              重建待办
            </a-button>
            <a-button
              v-if="record.status === 'Running'"
              v-permission="'button.process.terminate'"
              type="link"
              size="small"
              danger
              @click="handleTerminate(record)"
            >
              终止
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-drawer
      v-model:open="detailVisible"
      title="流程详情"
      :width="900"
      destroy-on-close
    >
      <a-spin :spinning="detailLoading">
        <template v-if="currentInstance">
          <a-descriptions bordered :column="2" size="small" class="detail-section">
            <a-descriptions-item label="实例编号">{{ currentInstance.instanceNo || '-' }}</a-descriptions-item>
            <a-descriptions-item label="流程编码">{{ currentInstance.processCode || '-' }}</a-descriptions-item>
            <a-descriptions-item label="流程标题" :span="2">{{ currentInstance.title || '-' }}</a-descriptions-item>
            <a-descriptions-item label="业务类型">{{ currentInstance.businessType || '-' }}</a-descriptions-item>
            <a-descriptions-item label="业务单号">{{ currentInstance.businessId || '-' }}</a-descriptions-item>
            <a-descriptions-item label="发起人">{{ currentInstance.starterName || '-' }}</a-descriptions-item>
            <a-descriptions-item label="当前节点">{{ currentInstance.currentNodeName || '-' }}</a-descriptions-item>
            <a-descriptions-item label="当前审批人">{{ currentInstance.currentTodo?.assigneeName || '-' }}</a-descriptions-item>
            <a-descriptions-item label="状态">
              <a-tag :color="getStatusColor(currentInstance.status)">
                {{ getStatusText(currentInstance.status) }}
              </a-tag>
            </a-descriptions-item>
            <a-descriptions-item label="发起时间">{{ formatDateTime(currentInstance.startedTime) }}</a-descriptions-item>
            <a-descriptions-item label="完成时间">{{ formatDateTime(currentInstance.finishedTime) }}</a-descriptions-item>
          </a-descriptions>

          <a-card size="small" title="流转轨迹" class="detail-section">
            <a-timeline>
              <a-timeline-item v-for="item in currentInstance.history || []" :key="item.id">
                <div class="timeline-title">{{ item.nodeName }} / {{ item.actionResult || '-' }}</div>
                <div class="timeline-meta">
                  {{ item.handlerName || item.handlerId || '系统' }} · {{ formatDateTime(item.handledTime || item.arrivedTime) }}
                </div>
                <div v-if="item.comment" class="timeline-comment">{{ item.comment }}</div>
              </a-timeline-item>
            </a-timeline>
          </a-card>

          <a-card size="small" title="扩展数据" class="detail-section">
            <pre class="json-block">{{ formatJson(currentInstance.extData) }}</pre>
          </a-card>

          <a-card v-if="callbackLogs.length" size="small" title="业务回调日志" class="detail-section">
            <a-table
              row-key="id"
              size="small"
              :columns="callbackColumns"
              :data-source="callbackLogs"
              :pagination="false"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'status'">
                  <a-tag :color="record.status === 'Success' ? 'success' : 'error'">
                    {{ record.status }}
                  </a-tag>
                </template>
                <template v-else-if="column.key === 'createdTime'">
                  {{ formatDateTime(record.createdTime) }}
                </template>
                <template v-else-if="column.key === 'action'">
                  <a-space>
                    <a-button type="link" size="small" @click="showLogDetail(record)">详情</a-button>
                    <a-button
                      v-if="record.status === 'Failed'"
                      type="link"
                      size="small"
                      @click="handleRetryCallback(record)"
                    >
                      重试
                    </a-button>
                  </a-space>
                </template>
              </template>
            </a-table>
          </a-card>
        </template>
      </a-spin>
    </a-drawer>

    <a-modal v-model:open="logDetailVisible" title="回调日志详情" width="800px" :footer="null">
      <template v-if="currentLog">
        <a-descriptions bordered :column="1" size="small">
          <a-descriptions-item label="请求地址">{{ currentLog.callbackUrl }}</a-descriptions-item>
          <a-descriptions-item label="状态码">{{ currentLog.statusCode }}</a-descriptions-item>
          <a-descriptions-item label="状态">{{ currentLog.status }}</a-descriptions-item>
          <a-descriptions-item label="请求时间">{{ formatDateTime(currentLog.createdTime) }}</a-descriptions-item>
          <a-descriptions-item label="错误信息">{{ currentLog.errorMessage || '-' }}</a-descriptions-item>
        </a-descriptions>
        <a-card size="small" title="请求载荷" class="log-detail-card">
          <pre class="json-block">{{ formatJson(currentLog.payload) }}</pre>
        </a-card>
        <a-card size="small" title="响应内容" class="log-detail-card">
          <pre class="json-block">{{ formatJson(currentLog.response) || currentLog.response }}</pre>
        </a-card>
      </template>
    </a-modal>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message, Modal } from 'ant-design-vue'
import { SyncOutlined } from '@ant-design/icons-vue'
import { processCenterApi } from '../../api'
import { useUserStore } from '../../store/user'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const detailLoading = ref(false)
const detailVisible = ref(false)
const logDetailVisible = ref(false)
const definitions = ref([])
const instances = ref([])
const currentInstance = ref(null)
const callbackLogs = ref([])
const currentLog = ref(null)

const queryForm = reactive({
  processCode: undefined,
  status: undefined,
  businessType: undefined,
  businessId: '',
  keyword: ''
})

const statusOptions = [
  { label: '运行中', value: 'Running' },
  { label: '已完成', value: 'Completed' },
  { label: '已驳回', value: 'Rejected' },
  { label: '已终止', value: 'Terminated' }
]

const columns = [
  { title: '实例编号', dataIndex: 'instanceNo', key: 'instanceNo', width: 190 },
  { title: '流程编码', dataIndex: 'processCode', key: 'processCode', width: 160 },
  { title: '标题', dataIndex: 'title', key: 'title', width: 240 },
  { title: '业务类型', dataIndex: 'businessType', key: 'businessType', width: 120 },
  { title: '业务单号', dataIndex: 'businessId', key: 'businessId', width: 200 },
  { title: '当前节点', dataIndex: 'currentNodeName', key: 'currentNodeName', width: 150 },
  { title: '当前审批人', key: 'currentTodo', width: 140 },
  { title: '发起人', dataIndex: 'starterName', key: 'starterName', width: 120 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 100 },
  { title: '发起时间', dataIndex: 'startedTime', key: 'startedTime', width: 180 },
  { title: '完成时间', dataIndex: 'finishedTime', key: 'finishedTime', width: 180 },
  { title: '操作', key: 'action', width: 220, fixed: 'right' }
]

const callbackColumns = [
  { title: '地址', dataIndex: 'callbackUrl', key: 'callbackUrl', ellipsis: true },
  { title: '状态码', dataIndex: 'statusCode', key: 'statusCode', width: 80 },
  { title: '结果', key: 'status', width: 100 },
  { title: '时间', key: 'createdTime', width: 180 },
  { title: '操作', key: 'action', width: 80 }
]

const loadDefinitions = async () => {
  definitions.value = await processCenterApi.getDefinitions()
}

const loadInstances = async () => {
  loading.value = true
  try {
    instances.value = await processCenterApi.getInstances({
      processCode: queryForm.processCode || undefined,
      status: queryForm.status || undefined,
      businessType: queryForm.businessType || undefined,
      businessId: queryForm.businessId.trim() || undefined,
      keyword: queryForm.keyword.trim() || undefined
    })
  } finally {
    loading.value = false
  }
}

const loadAll = async () => {
  await Promise.all([loadDefinitions(), loadInstances()])
}

const openDetailPage = (id) => {
  router.push({
    path: `/workflow/processes/instances/${id}`
  })
}

const openDetail = async (id) => {
  detailVisible.value = true
  detailLoading.value = true
  try {
    const [instance, logs] = await Promise.all([
      processCenterApi.getInstance(id),
      processCenterApi.getCallbackLogs(id)
    ])
    currentInstance.value = instance
    callbackLogs.value = logs || []
    if (`${route.query.instanceId || ''}` !== `${id}`) {
      router.replace({
        path: route.path,
        query: { ...route.query, instanceId: id }
      })
    }
  } finally {
    detailLoading.value = false
  }
}

const showLogDetail = (log) => {
  currentLog.value = log
  logDetailVisible.value = true
}

const handleRetryCallback = async (log) => {
  try {
    await processCenterApi.retryCallback(log.processInstanceId, log.id)
    message.success('重试请求已发送')
    const logs = await processCenterApi.getCallbackLogs(log.processInstanceId)
    callbackLogs.value = logs || []
  } catch (error) {
    message.error(error?.response?.data?.message || '重试失败')
  }
}

const tryOpenQueryInstance = async () => {
  const instanceId = `${route.query.instanceId || ''}`.trim()
  if (!instanceId) {
    return
  }
  const parsed = Number(instanceId)
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return
  }
  await openDetail(parsed)
}

const selectProcessCode = (processCode) => {
  queryForm.processCode = processCode
  loadInstances()
}

const handleReset = () => {
  queryForm.processCode = undefined
  queryForm.status = undefined
  queryForm.businessType = undefined
  queryForm.businessId = ''
  queryForm.keyword = ''
  loadInstances()
}

const handleTerminate = (record) => {
  Modal.confirm({
    title: '确认终止该流程？',
    content: `实例：${record.instanceNo} / ${record.title}`,
    okType: 'danger',
    async onOk() {
      loading.value = true
      try {
        await processCenterApi.terminateInstance(record.id, {
          operatorId: `${userStore.userInfo?.id || ''}`,
          operatorName: userStore.userInfo?.name || '管理员',
          comment: '主系统流程终止'
        })
        message.success('流程已终止')
        await loadInstances()
        if (detailVisible.value && currentInstance.value?.id === record.id) {
          await openDetail(record.id)
        }
      } finally {
        loading.value = false
      }
    }
  })
}

const handleReject = (record) => {
  Modal.confirm({
    title: '确认驳回该流程？',
    content: `实例：${record.instanceNo} / ${record.title}`,
    okType: 'danger',
    async onOk() {
      loading.value = true
      try {
        await processCenterApi.rejectInstance(record.id, {
          operatorId: `${userStore.userInfo?.id || ''}`,
          operatorName: userStore.userInfo?.name || '管理员',
          comment: '主系统流程驳回'
        })
        message.success('流程已驳回')
        await loadInstances()
        if (detailVisible.value && currentInstance.value?.id === record.id) {
          await openDetail(record.id)
        }
      } finally {
        loading.value = false
      }
    }
  })
}

const handleRebuildTodo = (record) => {
  Modal.confirm({
    title: '确认重建当前待办？',
    content: `实例：${record.instanceNo} / ${record.title}`,
    async onOk() {
      loading.value = true
      try {
        const result = await processCenterApi.rebuildCurrentTodo(record.id, {
          operatorId: `${userStore.userInfo?.id || ''}`,
          operatorName: userStore.userInfo?.name || '管理员',
          comment: '主系统重建待办'
        })
        message.success(`待办已重建：${result?.task?.taskNo || '已派发'}`)
        await loadInstances()
        if (detailVisible.value && currentInstance.value?.id === record.id) {
          await openDetail(record.id)
        }
      } finally {
        loading.value = false
      }
    }
  })
}

const getStatusText = (status) => {
  const map = {
    Running: '运行中',
    Completed: '已完成',
    Rejected: '已驳回',
    Terminated: '已终止',
    Withdrawn: '已撤回'
  }
  return map[status] || status || '-'
}

const getStatusColor = (status) => {
  const map = {
    Running: 'processing',
    Completed: 'success',
    Rejected: 'error',
    Terminated: 'default',
    Withdrawn: 'warning'
  }
  return map[status] || 'default'
}

const formatDateTime = (value) => {
  if (!value) {
    return '-'
  }
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString()
}

const formatJson = (value) => {
  if (!value) {
    return '{}'
  }
  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }
  return JSON.stringify(value, null, 2)
}

watch(
  () => route.query.instanceId,
  async (value, oldValue) => {
    if (!value || value === oldValue) {
      return
    }
    await tryOpenQueryInstance()
  }
)

onMounted(async () => {
  queryForm.businessId = `${route.query.businessId || ''}`
  await loadAll()
  await tryOpenQueryInstance()
})
</script>

<style scoped>
.definition-card,
.query-card {
  margin-bottom: 16px;
}

.definition-tag {
  cursor: pointer;
}

.detail-section {
  margin-bottom: 16px;
}

.log-detail-card {
  margin-top: 16px;
}

.json-block {
  background: #f5f5f5;
  padding: 12px;
  border-radius: 4px;
  overflow-x: auto;
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
}

.timeline-title {
  font-weight: 500;
  color: #333;
}

.timeline-meta {
  color: #999;
  font-size: 12px;
  margin-top: 4px;
}

.timeline-comment {
  margin-top: 8px;
  padding: 8px;
  background: #f9f9f9;
  border-radius: 4px;
  font-size: 12px;
}
</style>
