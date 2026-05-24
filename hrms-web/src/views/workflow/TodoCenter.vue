<template>
  <div class="todo-center">
    <a-row :gutter="16" class="summary-row">
      <a-col :span="6">
        <a-card size="small">
          <a-statistic title="全部任务" :value="summary.totalCount || 0" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card size="small">
          <a-statistic title="待处理" :value="summary.pendingCount || 0" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card size="small">
          <a-statistic title="已完成" :value="summary.completedCount || 0" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card size="small">
          <a-statistic title="按时率" :value="(summary.onTimeRate || 0) * 100" suffix="%" :precision="2" />
        </a-card>
      </a-col>
    </a-row>

    <div class="toolbar">
      <a-space wrap>
        <a-button
          v-permission="'button.todo.batchComplete'"
          type="primary"
          :disabled="selectedPendingRows.length === 0"
          @click="handleBatchComplete"
        >
          批量通过
        </a-button>
        <a-button
          v-permission="'button.todo.batchReject'"
          danger
          :disabled="selectedPendingRows.length === 0"
          @click="handleBatchReject"
        >
          批量驳回
        </a-button>
        <a-button
          v-permission="'button.todo.transfer'"
          :disabled="selectedPendingRows.length === 0"
          @click="openBatchTransferModal"
        >
          批量转交
        </a-button>
        <a-button
          v-permission="'button.todo.urge'"
          :disabled="selectedPendingRows.length === 0"
          @click="handleBatchUrge"
        >
          批量催办
        </a-button>
        <a-button
          v-permission="'button.todo.agent'"
          @click="openAgentDrawer"
        >
          代理设置
        </a-button>
        <a-button @click="loadData">
          <SyncOutlined />
          刷新
        </a-button>
      </a-space>
    </div>

    <a-card size="small" class="query-card">
      <a-form layout="inline">
        <a-form-item label="状态">
          <a-select v-model:value="queryForm.status" allow-clear placeholder="全部状态" style="width: 150px">
            <a-select-option v-for="item in statusOptions" :key="item.value" :value="item.value">
              {{ item.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="任务类型">
          <a-select v-model:value="queryForm.taskTypeCode" allow-clear placeholder="全部类型" style="width: 220px">
            <a-select-option v-for="item in taskTypeOptions" :key="item.value" :value="item.value">
              {{ item.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="业务单号">
          <a-input v-model:value="queryForm.businessId" allow-clear placeholder="如 RUN-2026-05" style="width: 220px" />
        </a-form-item>
        <a-form-item label="流程实例">
          <a-input v-model:value="queryForm.processInstanceId" allow-clear placeholder="如 1001" style="width: 180px" />
        </a-form-item>
        <a-form-item label="关键字">
          <a-input v-model:value="queryForm.keyword" allow-clear placeholder="标题/业务单号/月份" style="width: 240px" />
        </a-form-item>
        <a-form-item>
          <a-space>
            <a-button type="primary" @click="loadData">查询</a-button>
            <a-button @click="handleReset">重置</a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>

    <a-table
      row-key="id"
      :loading="loading"
      :columns="columns"
      :data-source="tasks"
      :pagination="{ pageSize: 10, showSizeChanger: true }"
      :row-selection="rowSelection"
      :scroll="{ x: 1460 }"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'priority'">
          <a-tag :color="getPriorityColor(record.priority)">
            {{ getPriorityText(record.priority) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'yearMonth'">
          {{ record.sourcePayloadData?.yearMonth || '-' }}
        </template>
        <template v-else-if="column.key === 'totalNetSalary'">
          {{ formatCurrency(record.sourcePayloadData?.totalNetSalary) }}
        </template>
        <template v-else-if="column.key === 'createdTime' || column.key === 'completedTime'">
          {{ formatDateTime(record[column.key]) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space wrap>
            <a-button type="link" size="small" @click="openDetail(record)">详情</a-button>
            <a-button
              v-if="record.processInstanceId"
              type="link"
              size="small"
              @click="goToProcess(record.processInstanceId)"
            >
              流程
            </a-button>
            <a-button
              v-if="record.status === 'Pending'"
              v-permission="'button.todo.complete'"
              type="link"
              size="small"
              @click="handleComplete(record)"
            >
              通过
            </a-button>
            <a-button
              v-if="record.status === 'Pending'"
              v-permission="'button.todo.reject'"
              type="link"
              size="small"
              danger
              @click="handleReject(record)"
            >
              驳回
            </a-button>
            <a-button
              v-if="record.status === 'Pending'"
              v-permission="'button.todo.transfer'"
              type="link"
              size="small"
              @click="openTransferModal(record)"
            >
              转交
            </a-button>
            <a-button
              v-if="record.status === 'Pending'"
              v-permission="'button.todo.urge'"
              type="link"
              size="small"
              @click="handleUrge(record)"
            >
              催办
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-drawer v-model:open="detailVisible" title="待办详情" :width="860" destroy-on-close>
      <a-spin :spinning="detailLoading">
        <template v-if="detail.task">
          <a-descriptions bordered :column="2" size="small" class="detail-section">
            <a-descriptions-item label="任务编号">{{ detail.task.taskNo || '-' }}</a-descriptions-item>
            <a-descriptions-item label="状态">
              <a-tag :color="getStatusColor(detail.task.status)">
                {{ getStatusText(detail.task.status) }}
              </a-tag>
            </a-descriptions-item>
            <a-descriptions-item label="标题" :span="2">{{ detail.task.title || '-' }}</a-descriptions-item>
            <a-descriptions-item label="业务类型">{{ detail.task.businessType || '-' }}</a-descriptions-item>
            <a-descriptions-item label="业务单号">{{ detail.task.businessId || '-' }}</a-descriptions-item>
            <a-descriptions-item label="审批人">{{ detail.task.assigneeName || '-' }}</a-descriptions-item>
            <a-descriptions-item label="流程实例">{{ detail.task.processInstanceId || '-' }}</a-descriptions-item>
            <a-descriptions-item label="核算月份">{{ detail.task.sourcePayloadData?.yearMonth || '-' }}</a-descriptions-item>
            <a-descriptions-item label="实发合计">{{ formatCurrency(detail.task.sourcePayloadData?.totalNetSalary) }}</a-descriptions-item>
            <a-descriptions-item label="创建时间">{{ formatDateTime(detail.task.createdTime) }}</a-descriptions-item>
            <a-descriptions-item label="完成时间">{{ formatDateTime(detail.task.completedTime) }}</a-descriptions-item>
          </a-descriptions>

          <a-card size="small" title="业务载荷" class="detail-section">
            <pre class="json-block">{{ formatJson(detail.task.sourcePayloadData) }}</pre>
          </a-card>

          <a-card size="small" title="处理日志" class="detail-section">
            <a-timeline>
              <a-timeline-item v-for="item in detail.logs" :key="item.id">
                <div class="timeline-title">{{ item.action }} / {{ item.actionResult || '-' }}</div>
                <div class="timeline-meta">
                  {{ item.operatorName || item.operatorId || '系统' }} · {{ formatDateTime(item.createdTime) }}
                </div>
                <div v-if="item.comment" class="timeline-comment">{{ item.comment }}</div>
              </a-timeline-item>
            </a-timeline>
          </a-card>

          <a-card size="small" title="催办记录" class="detail-section">
            <a-empty v-if="!detail.notifyLogs.length" description="暂无催办记录" />
            <a-timeline v-else>
              <a-timeline-item v-for="item in detail.notifyLogs" :key="item.id">
                <div class="timeline-title">{{ item.notifyType }} / {{ item.status || '-' }}</div>
                <div class="timeline-meta">
                  {{ item.receiverName || item.receiverId || '-' }} · {{ formatDateTime(item.sentTime || item.createdTime) }}
                </div>
                <div v-if="item.content" class="timeline-comment">{{ item.content }}</div>
              </a-timeline-item>
            </a-timeline>
          </a-card>
        </template>
      </a-spin>
    </a-drawer>

    <a-modal
      v-model:open="transferVisible"
      :title="transferMode === 'batch' ? '批量转交待办' : '转交待办'"
      :ok-text="transferMode === 'batch' ? '确认批量转交' : '确认转交'"
      @ok="transferMode === 'batch' ? submitBatchTransfer() : submitTransfer()"
    >
      <a-form layout="vertical">
        <a-form-item label="当前任务">
          <a-input
            :value="transferMode === 'batch'
              ? `已选择 ${selectedPendingRows.length} 条待办`
              : (transferTarget?.title || '')"
            disabled
          />
        </a-form-item>
        <a-form-item label="转交给">
          <a-select
            v-model:value="transferForm.targetAssigneeId"
            show-search
            placeholder="请选择用户"
            :filter-option="filterUserOption"
            @change="handleTransferAssigneeChange"
          >
            <a-select-option v-for="item in userOptions" :key="item.id" :value="item.id">
              {{ item.name }}（{{ item.username }}）
            </a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <a-drawer v-model:open="agentVisible" title="代理设置" :width="760" destroy-on-close>
      <div class="agent-toolbar">
        <a-button v-permission="'button.todo.agent'" type="primary" @click="openAgentCreateModal">
          新建代理
        </a-button>
      </div>

      <a-table
        row-key="id"
        size="small"
        :loading="agentLoading"
        :columns="agentColumns"
        :data-source="agentSettings"
        :pagination="false"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'scopeType'">
            {{ record.scopeType === 'All' ? '全部任务' : (record.taskTypeCode || '-') }}
          </template>
          <template v-else-if="column.key === 'timeRange'">
            {{ formatDateTime(record.startTime) }} ~ {{ formatDateTime(record.endTime) }}
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag :color="getAgentStatusColor(record.status)">
              {{ getAgentStatusText(record.status) }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'agentUserId'">
            {{ resolveUserName(record.agentUserId) }}
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space>
              <a-button type="link" size="small" @click="openAgentEditModal(record)">编辑</a-button>
              <a-button
                v-if="record.status === 'Active'"
                type="link"
                size="small"
                danger
                @click="handleDisableAgentSetting(record)"
              >
                停用
              </a-button>
              <a-button type="link" size="small" danger @click="handleDeleteAgentSetting(record)">删除</a-button>
            </a-space>
          </template>
        </template>
      </a-table>

      <a-modal
        v-model:open="agentFormVisible"
        :title="agentFormMode === 'edit' ? '编辑代理规则' : '新建代理规则'"
        ok-text="保存"
        @ok="submitAgentSetting"
      >
        <a-form layout="vertical">
          <a-form-item label="代理人">
            <a-select
              v-model:value="agentForm.agentUserId"
              show-search
              placeholder="请选择代理人"
              :filter-option="filterUserOption"
            >
              <a-select-option v-for="item in userOptions" :key="item.id" :value="item.id">
                {{ item.name }}（{{ item.username }}）
              </a-select-option>
            </a-select>
          </a-form-item>
          <a-form-item label="范围">
            <a-select v-model:value="agentForm.scopeType">
              <a-select-option value="All">全部任务</a-select-option>
              <a-select-option value="TaskType">指定任务类型</a-select-option>
            </a-select>
          </a-form-item>
          <a-form-item v-if="agentForm.scopeType === 'TaskType'" label="任务类型">
            <a-select v-model:value="agentForm.taskTypeCode" placeholder="请选择任务类型">
              <a-select-option v-for="item in taskTypeOptions" :key="item.value" :value="item.value">
                {{ item.label }}
              </a-select-option>
            </a-select>
          </a-form-item>
          <a-form-item label="开始时间">
            <a-date-picker v-model:value="agentForm.startTime" show-time style="width: 100%" />
          </a-form-item>
          <a-form-item label="结束时间">
            <a-date-picker v-model:value="agentForm.endTime" show-time style="width: 100%" />
          </a-form-item>
        </a-form>
      </a-modal>
    </a-drawer>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import dayjs from 'dayjs'
import { useRoute, useRouter } from 'vue-router'
import { message, Modal } from 'ant-design-vue'
import { SyncOutlined } from '@ant-design/icons-vue'
import { accessApi, todoCenterApi } from '../../api'
import { useUserStore } from '../../store/user'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const detailLoading = ref(false)
const detailVisible = ref(false)
const transferVisible = ref(false)
const transferMode = ref('single')
const agentVisible = ref(false)
const agentFormVisible = ref(false)
const agentLoading = ref(false)
const tasks = ref([])
const summary = ref({})
const selectedRowKeys = ref([])
const userOptions = ref([])
const agentSettings = ref([])
const transferTarget = ref(null)
const editingAgentId = ref(null)
const agentFormMode = ref('create')
const detail = reactive({
  task: null,
  logs: [],
  notifyLogs: []
})

const queryForm = reactive({
  status: 'Pending',
  taskTypeCode: undefined,
  businessId: '',
  processInstanceId: '',
  keyword: ''
})

const transferForm = reactive({
  targetAssigneeId: '',
  targetAssigneeName: ''
})

const agentForm = reactive({
  agentUserId: '',
  scopeType: 'All',
  taskTypeCode: undefined,
  startTime: dayjs().add(1, 'hour'),
  endTime: dayjs().add(1, 'day')
})

const statusOptions = [
  { label: '待处理', value: 'Pending' },
  { label: '已完成', value: 'Completed' },
  { label: '已驳回', value: 'Rejected' },
  { label: '已转交', value: 'Transferred' }
]

const taskTypeOptions = ref([
  { label: '薪资审批', value: 'PAYROLL_APPROVAL.Approve' }
])

const loadTaskTypes = async () => {
  const options = new Map(taskTypeOptions.value.map(item => [item.value, item]))
  ;[tasks.value, agentSettings.value].forEach((list) => {
    ;(list || []).forEach((item) => {
      const value = item?.taskTypeCode
      if (!value || options.has(value)) {
        return
      }
      options.set(value, { label: value, value })
    })
  })
  taskTypeOptions.value = Array.from(options.values())
}

const columns = [
  { title: '任务编号', dataIndex: 'taskNo', key: 'taskNo', width: 190 },
  { title: '标题', dataIndex: 'title', key: 'title', width: 260 },
  { title: '业务类型', dataIndex: 'businessType', key: 'businessType', width: 120 },
  { title: '业务单号', dataIndex: 'businessId', key: 'businessId', width: 180 },
  { title: '核算月份', key: 'yearMonth', width: 100 },
  { title: '实发合计', key: 'totalNetSalary', width: 130 },
  { title: '审批人', dataIndex: 'assigneeName', key: 'assigneeName', width: 120 },
  { title: '优先级', dataIndex: 'priority', key: 'priority', width: 100 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 100 },
  { title: '创建时间', dataIndex: 'createdTime', key: 'createdTime', width: 180 },
  { title: '完成时间', dataIndex: 'completedTime', key: 'completedTime', width: 180 },
  { title: '操作', key: 'action', width: 280, fixed: 'right' }
]

const agentColumns = [
  { title: '代理人', dataIndex: 'agentUserId', key: 'agentUserId', width: 180 },
  { title: '范围', key: 'scopeType', width: 180 },
  { title: '生效时间', key: 'timeRange', width: 320 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 120 },
  { title: '操作', key: 'action', width: 180, fixed: 'right' }
]

const currentUserId = computed(() => `${userStore.userInfo?.id || ''}`)
const currentUserName = computed(() => userStore.userInfo?.name || '管理员')
const selectedPendingRows = computed(() =>
  tasks.value.filter(task => selectedRowKeys.value.includes(task.id) && task.status === 'Pending')
)

const rowSelection = computed(() => ({
  selectedRowKeys: selectedRowKeys.value,
  onChange: (keys) => {
    selectedRowKeys.value = keys
  },
  getCheckboxProps: (record) => ({
    disabled: record.status !== 'Pending'
  })
}))

const loadUsers = async () => {
  const users = await accessApi.getUsers()
  userOptions.value = users || []
}

const loadData = async () => {
  loading.value = true
  try {
    const params = {
      assigneeId: currentUserId.value,
      status: queryForm.status || undefined,
      taskTypeCode: queryForm.taskTypeCode || undefined,
      businessId: queryForm.businessId.trim() || undefined,
      processInstanceId: queryForm.processInstanceId.trim() || undefined,
      keyword: queryForm.keyword.trim() || undefined
    }
    const [taskList, kpi] = await Promise.all([
      todoCenterApi.getMyTasks(params),
      todoCenterApi.getKpiSummary(currentUserId.value)
    ])
    tasks.value = taskList || []
    summary.value = kpi || {}
    loadTaskTypes()
    selectedRowKeys.value = selectedRowKeys.value.filter(id =>
      tasks.value.some(task => task.id === id && task.status === 'Pending')
    )
  } finally {
    loading.value = false
  }
}

const loadAgentSettings = async () => {
  agentLoading.value = true
  try {
    agentSettings.value = await todoCenterApi.getAgentSettings(currentUserId.value)
    loadTaskTypes()
  } finally {
    agentLoading.value = false
  }
}

const openDetail = async (record) => {
  detailVisible.value = true
  detailLoading.value = true
  try {
    const result = await todoCenterApi.getTaskDetail(record.id)
    detail.task = result?.task || null
    detail.logs = result?.logs || []
    detail.notifyLogs = result?.notifyLogs || []
  } finally {
    detailLoading.value = false
  }
}

const executeAndReload = async (action, successMessage, targetRecord) => {
  await action()
  message.success(successMessage)
  await loadData()
  if (detailVisible.value && detail.task?.id === targetRecord?.id) {
    await openDetail(targetRecord)
  }
}

const handleComplete = (record) => {
  Modal.confirm({
    title: '确认通过该待办？',
    content: `任务：${record.title}`,
    async onOk() {
      await executeAndReload(() => todoCenterApi.completeTask(record.id, {
        operatorId: currentUserId.value,
        operatorName: currentUserName.value,
        comment: '审批通过'
      }), '待办已处理', record)
    }
  })
}

const handleReject = (record) => {
  Modal.confirm({
    title: '确认驳回该待办？',
    content: `任务：${record.title}`,
    okType: 'danger',
    async onOk() {
      await executeAndReload(() => todoCenterApi.rejectTask(record.id, {
        operatorId: currentUserId.value,
        operatorName: currentUserName.value,
        comment: '审批驳回'
      }), '待办已驳回', record)
    }
  })
}

const handleBatchComplete = () => {
  Modal.confirm({
    title: '确认批量通过选中的待办？',
    content: `本次将处理 ${selectedPendingRows.value.length} 条待办`,
    async onOk() {
      await todoCenterApi.batchComplete({
        taskIds: selectedPendingRows.value.map(item => item.id),
        operatorId: currentUserId.value,
        operatorName: currentUserName.value,
        comment: '批量审批通过'
      })
      message.success('批量处理成功')
      selectedRowKeys.value = []
      await loadData()
    }
  })
}

const handleBatchReject = () => {
  Modal.confirm({
    title: '确认批量驳回选中的待办？',
    content: `本次将驳回 ${selectedPendingRows.value.length} 条待办`,
    okType: 'danger',
    async onOk() {
      await todoCenterApi.batchReject({
        taskIds: selectedPendingRows.value.map(item => item.id),
        operatorId: currentUserId.value,
        operatorName: currentUserName.value,
        comment: '批量审批驳回'
      })
      message.success('批量驳回成功')
      selectedRowKeys.value = []
      await loadData()
    }
  })
}

const openBatchTransferModal = () => {
  transferMode.value = 'batch'
  transferTarget.value = null
  transferForm.targetAssigneeId = ''
  transferForm.targetAssigneeName = ''
  transferVisible.value = true
}

const openTransferModal = (record) => {
  transferMode.value = 'single'
  transferTarget.value = record
  transferForm.targetAssigneeId = ''
  transferForm.targetAssigneeName = ''
  transferVisible.value = true
}

const handleTransferAssigneeChange = (value) => {
  const user = userOptions.value.find(item => `${item.id}` === `${value}`)
  transferForm.targetAssigneeName = user?.name || user?.username || ''
}

const submitTransfer = async () => {
  if (!transferTarget.value) {
    return
  }
  if (!transferForm.targetAssigneeId || !transferForm.targetAssigneeName) {
    message.warning('请选择转交人')
    return
  }

  await todoCenterApi.transferTask(transferTarget.value.id, {
    operatorId: currentUserId.value,
    operatorName: currentUserName.value,
    targetAssigneeId: transferForm.targetAssigneeId,
    targetAssigneeName: transferForm.targetAssigneeName,
    comment: '主系统转交'
  })
  transferVisible.value = false
  message.success('待办已转交')
  await loadData()
}

const submitBatchTransfer = async () => {
  if (!transferForm.targetAssigneeId || !transferForm.targetAssigneeName) {
    message.warning('请选择转交人')
    return
  }

  await todoCenterApi.batchTransfer({
    taskIds: selectedPendingRows.value.map(item => item.id),
    operatorId: currentUserId.value,
    operatorName: currentUserName.value,
    targetAssigneeId: transferForm.targetAssigneeId,
    targetAssigneeName: transferForm.targetAssigneeName,
    comment: '主系统批量转交'
  })
  transferVisible.value = false
  selectedRowKeys.value = []
  message.success('批量转交成功')
  await loadData()
}

const handleUrge = async (record) => {
  await todoCenterApi.urgeTask(record.id, {
    operatorId: currentUserId.value,
    operatorName: currentUserName.value,
    comment: '请尽快处理'
  })
  message.success('催办已发送')
  if (detailVisible.value && detail.task?.id === record.id) {
    await openDetail(record)
  }
}

const handleBatchUrge = () => {
  Modal.confirm({
    title: '确认批量催办选中的待办？',
    content: `本次将催办 ${selectedPendingRows.value.length} 条待办`,
    async onOk() {
      await todoCenterApi.batchUrge({
        taskIds: selectedPendingRows.value.map(item => item.id),
        operatorId: currentUserId.value,
        operatorName: currentUserName.value,
        comment: '主系统批量催办'
      })
      message.success('批量催办成功')
      if (detailVisible.value && detail.task?.id) {
        const currentDetailTask = tasks.value.find(item => item.id === detail.task.id) || detail.task
        await openDetail(currentDetailTask)
      }
    }
  })
}

const openAgentDrawer = async () => {
  agentVisible.value = true
  await loadAgentSettings()
}

const resetAgentForm = () => {
  editingAgentId.value = null
  agentFormMode.value = 'create'
  agentForm.agentUserId = ''
  agentForm.scopeType = 'All'
  agentForm.taskTypeCode = undefined
  agentForm.startTime = dayjs().add(1, 'hour')
  agentForm.endTime = dayjs().add(1, 'day')
}

const openAgentCreateModal = () => {
  resetAgentForm()
  agentFormVisible.value = true
}

const openAgentEditModal = (record) => {
  editingAgentId.value = record.id
  agentFormMode.value = 'edit'
  agentForm.agentUserId = record.agentUserId || ''
  agentForm.scopeType = record.scopeType || 'All'
  agentForm.taskTypeCode = record.taskTypeCode || undefined
  agentForm.startTime = dayjs(record.startTime)
  agentForm.endTime = dayjs(record.endTime)
  agentFormVisible.value = true
}

const submitAgentSetting = async () => {
  if (!agentForm.agentUserId) {
    message.warning('请选择代理人')
    return
  }
  if (agentForm.scopeType === 'TaskType' && !agentForm.taskTypeCode) {
    message.warning('请选择任务类型')
    return
  }
  if (!agentForm.startTime || !agentForm.endTime) {
    message.warning('请选择生效时间')
    return
  }

  const payload = {
    agentUserId: agentForm.agentUserId,
    scopeType: agentForm.scopeType,
    taskTypeCode: agentForm.scopeType === 'TaskType' ? agentForm.taskTypeCode : null,
    startTime: agentForm.startTime.toISOString(),
    endTime: agentForm.endTime.toISOString()
  }
  if (agentFormMode.value === 'edit' && editingAgentId.value) {
    await todoCenterApi.updateAgentSetting(editingAgentId.value, payload)
  } else {
    await todoCenterApi.createAgentSetting({
      principalUserId: currentUserId.value,
      ...payload
    })
  }
  agentFormVisible.value = false
  message.success(agentFormMode.value === 'edit' ? '代理规则已更新' : '代理规则已创建')
  resetAgentForm()
  await loadAgentSettings()
}

const handleDisableAgentSetting = (record) => {
  Modal.confirm({
    title: '确认停用该代理规则？',
    content: `代理人：${resolveUserName(record.agentUserId)}`,
    okType: 'danger',
    async onOk() {
      await todoCenterApi.disableAgentSetting(record.id)
      message.success('代理规则已停用')
      await loadAgentSettings()
    }
  })
}

const handleDeleteAgentSetting = (record) => {
  Modal.confirm({
    title: '确认删除该代理规则？',
    content: `代理人：${resolveUserName(record.agentUserId)}`,
    okType: 'danger',
    async onOk() {
      await todoCenterApi.deleteAgentSetting(record.id)
      message.success('代理规则已删除')
      await loadAgentSettings()
    }
  })
}

const resolveUserName = (userId) => {
  const user = userOptions.value.find(item => `${item.id}` === `${userId}`)
  return user?.name || user?.username || userId || '-'
}

const filterUserOption = (input, option) => {
  const label = `${option.children || ''}`.toLowerCase()
  return label.includes((input || '').toLowerCase())
}

const goToProcess = (instanceId) => {
  router.push({
    path: `/workflow/processes/instances/${instanceId}`
  })
}

const handleReset = () => {
  queryForm.status = 'Pending'
  queryForm.taskTypeCode = undefined
  queryForm.businessId = ''
  queryForm.processInstanceId = ''
  queryForm.keyword = ''
  loadData()
}

const getStatusText = (status) => {
  const map = {
    Pending: '待处理',
    Completed: '已完成',
    Rejected: '已驳回',
    Transferred: '已转交'
  }
  return map[status] || status || '-'
}

const getStatusColor = (status) => {
  const map = {
    Pending: 'processing',
    Completed: 'success',
    Rejected: 'error',
    Transferred: 'warning'
  }
  return map[status] || 'default'
}

const getPriorityText = (priority) => {
  const map = {
    1: '低',
    2: '中',
    3: '高'
  }
  return map[priority] || `P${priority || 0}`
}

const getPriorityColor = (priority) => {
  const map = {
    1: 'default',
    2: 'blue',
    3: 'red'
  }
  return map[priority] || 'default'
}

const getAgentStatusText = (status) => {
  const map = {
    Active: '生效中',
    Disabled: '已停用',
    Expired: '已过期'
  }
  return map[status] || status || '-'
}

const getAgentStatusColor = (status) => {
  const map = {
    Active: 'success',
    Disabled: 'default',
    Expired: 'warning'
  }
  return map[status] || 'default'
}

const formatCurrency = (value) => {
  const amount = Number(value)
  return Number.isFinite(amount) ? `¥${amount.toFixed(2)}` : '-'
}

const formatDateTime = (value) => {
  if (!value) {
    return '-'
  }
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString()
}

const formatJson = (value) => {
  return value ? JSON.stringify(value, null, 2) : '{}'
}

onMounted(async () => {
  queryForm.businessId = `${route.query.businessId || ''}`
  queryForm.processInstanceId = `${route.query.instanceId || ''}`
  await Promise.all([loadUsers(), loadData(), loadAgentSettings()])
})
</script>

<style scoped>
.summary-row,
.toolbar,
.query-card,
.detail-section {
  margin-bottom: 16px;
}

.agent-toolbar {
  margin-bottom: 12px;
}

.json-block {
  margin: 0;
  padding: 12px;
  overflow: auto;
  background: #f7f7f7;
  border-radius: 6px;
}

.timeline-title {
  font-weight: 600;
}

.timeline-meta {
  color: #8c8c8c;
  font-size: 12px;
}

.timeline-comment {
  margin-top: 4px;
}
</style>
