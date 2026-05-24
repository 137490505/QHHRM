<template>
  <div class="timesheet-list">
    <div class="toolbar">
      <a-space wrap>
        <a-button v-permission="'button.timesheet.create'" type="primary" @click="showAddModal">
          <PlusOutlined /> 新增工时
        </a-button>
        <a-button v-permission="'page.timesheet.import'" @click="$router.push('/timesheets/import')">
          <UploadOutlined /> 批量导入
        </a-button>
        <a-button
          v-permission="'button.timesheet.approve'"
          :disabled="approvableRows.length === 0"
          @click="handleBatchApprove"
        >
          <CheckCircleOutlined /> 批量审批
        </a-button>
        <a-button @click="loadData"><SyncOutlined /> 刷新</a-button>
      </a-space>
    </div>

    <a-card size="small">
      <a-form :model="queryForm" layout="inline">
        <a-form-item label="关键字">
          <a-input v-model:value="queryForm.keyword" placeholder="员工工号/姓名" class="query-input" />
        </a-form-item>
        <a-form-item label="工作日期">
          <a-range-picker v-model:value="queryForm.dateRange" />
        </a-form-item>
        <a-form-item label="实际班组">
          <a-tree-select
            v-model:value="queryForm.orgUnitId"
            :tree-data="orgTreeData"
            allow-clear
            placeholder="全部班组"
            style="width: 220px"
          />
        </a-form-item>
        <a-form-item label="班次类型">
          <a-select v-model:value="queryForm.shiftType" allow-clear placeholder="全部类型" style="width: 150px">
            <a-select-option v-for="option in shiftOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="审批状态">
          <a-select v-model:value="queryForm.approvalStatus" allow-clear placeholder="全部状态" style="width: 150px">
            <a-select-option v-for="option in statusOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item>
          <a-button type="primary" @click="handleSearch">查询</a-button>
          <a-button @click="handleReset">重置</a-button>
        </a-form-item>
      </a-form>
    </a-card>

    <div class="summary-bar">
      <a-row :gutter="16">
        <a-col :span="8">
          <a-statistic title="记录数" :value="summary.total" />
        </a-col>
        <a-col :span="8">
          <a-statistic title="总工时" :value="summary.workingHours" :precision="1" suffix="小时" />
        </a-col>
        <a-col :span="8">
          <a-statistic title="加班工时" :value="summary.overtimeHours" :precision="1" suffix="小时" />
        </a-col>
      </a-row>
    </div>

    <a-table
      :columns="columns"
      :data-source="filteredData"
      :loading="loading"
      row-key="id"
      :pagination="pagination"
      :row-selection="{ selectedRowKeys, onChange: handleRowSelect }"
      :scroll="{ x: 1180 }"
      @change="handleTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'date'">
          {{ formatDate(record.date) }}
        </template>
        <template v-else-if="column.key === 'shiftType'">
          {{ getShiftText(record.shiftType) }}
        </template>
        <template v-else-if="column.key === 'approvalStatus'">
          <a-tag :color="getStatusColor(record.approvalStatus)">
            {{ getStatusText(record.approvalStatus) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'regularHours'">
          {{ formatHours(getRegularHours(record)) }}
        </template>
        <template v-else-if="column.key === 'workingHours' || column.key === 'overtimeHours'">
          {{ formatHours(record[column.key]) }}
        </template>
        <template v-else-if="column.key === 'createdAt'">
          {{ formatDateTime(record.createdAt) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="handleView(record)">查看</a-button>
            <a-button
              v-permission="'button.timesheet.approve'"
              v-if="record.approvalStatus !== 'Approved'"
              type="link"
              size="small"
              @click="handleApprove(record)"
            >
              审批
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-modal v-model:open="modalVisible" title="新增工时" @ok="handleSave">
      <a-form :label-col="{ span: 6 }">
        <a-form-item label="员工" required>
          <EmployeeSelector v-model="selectedEmployee" trigger-type="button" />
        </a-form-item>
        <a-form-item label="工作日期" required>
          <a-date-picker v-model:value="form.date" style="width: 100%;" />
        </a-form-item>
        <a-form-item label="实际班组" required>
          <a-tree-select
            v-model:value="form.actualOrgUnitId"
            :tree-data="orgTreeData"
            placeholder="选择班组"
            style="width: 100%;"
          />
        </a-form-item>
        <a-form-item label="工作时长" required>
          <a-input-number v-model:value="form.workingHours" :min="0.5" :max="24" :step="0.5" style="width: 100%;" />
        </a-form-item>
        <a-form-item label="班次类型">
          <a-select v-model:value="form.shiftType">
            <a-select-option v-for="option in shiftOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="备注">
          <a-textarea v-model:value="form.remark" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal v-model:open="viewModalVisible" title="工时详情" :footer="null">
      <a-descriptions :column="2" bordered size="small">
        <a-descriptions-item label="员工工号">{{ viewData.employeeNo }}</a-descriptions-item>
        <a-descriptions-item label="员工姓名">{{ viewData.employeeName }}</a-descriptions-item>
        <a-descriptions-item label="工作日期">{{ formatDate(viewData.date) }}</a-descriptions-item>
        <a-descriptions-item label="实际班组">{{ viewData.actualOrgUnitName || '-' }}</a-descriptions-item>
        <a-descriptions-item label="班次类型">{{ getShiftText(viewData.shiftType) }}</a-descriptions-item>
        <a-descriptions-item label="审批状态">
          <a-tag :color="getStatusColor(viewData.approvalStatus)">
            {{ getStatusText(viewData.approvalStatus) }}
          </a-tag>
        </a-descriptions-item>
        <a-descriptions-item label="工作时长">{{ formatHours(viewData.workingHours) }}</a-descriptions-item>
        <a-descriptions-item label="加班工时">{{ formatHours(viewData.overtimeHours) }}</a-descriptions-item>
        <a-descriptions-item label="备注" :span="2">{{ viewData.remark || '-' }}</a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import dayjs from 'dayjs'
import { message } from 'ant-design-vue'
import { CheckCircleOutlined, PlusOutlined, SyncOutlined, UploadOutlined } from '@ant-design/icons-vue'
import { orgUnitApi, timesheetApi } from '../../api'
import { useUserStore } from '../../store/user'
import EmployeeSelector from '../../components/EmployeeSelector.vue'

const userStore = useUserStore()
const loading = ref(false)
const data = ref([])
const modalVisible = ref(false)
const viewModalVisible = ref(false)
const viewData = ref({})
const selectedRowKeys = ref([])
const selectedRows = ref([])
const selectedEmployee = ref(null)
const orgTreeData = ref([])

const pagination = reactive({
  current: 1,
  pageSize: 10,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showTotal: (total) => `共 ${total} 条记录`
})

const queryForm = reactive({
  keyword: '',
  dateRange: null,
  orgUnitId: undefined,
  shiftType: undefined,
  approvalStatus: undefined
})

const form = reactive({
  date: dayjs(),
  actualOrgUnitId: undefined,
  workingHours: 8,
  shiftType: 'Weekday',
  remark: ''
})

const shiftOptions = [
  { label: '工作日', value: 'Weekday' },
  { label: '周末', value: 'Weekend' },
  { label: '节假日', value: 'Holiday' }
]

const statusOptions = [
  { label: '待审批', value: 'Pending' },
  { label: '已审批', value: 'Approved' },
  { label: '已驳回', value: 'Rejected' }
]

const columns = [
  { title: '员工工号', dataIndex: 'employeeNo', key: 'employeeNo', width: 120 },
  { title: '姓名', dataIndex: 'employeeName', key: 'employeeName', width: 100 },
  { title: '工作日期', dataIndex: 'date', key: 'date', width: 120 },
  { title: '实际班组', dataIndex: 'actualOrgUnitName', key: 'actualOrgUnitName', width: 180 },
  { title: '班次类型', dataIndex: 'shiftType', key: 'shiftType', width: 120 },
  { title: '工作时长', dataIndex: 'workingHours', key: 'workingHours', width: 100 },
  { title: '正常工时', key: 'regularHours', width: 100 },
  { title: '加班工时', dataIndex: 'overtimeHours', key: 'overtimeHours', width: 100 },
  { title: '审批状态', dataIndex: 'approvalStatus', key: 'approvalStatus', width: 120 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 180 },
  { title: '备注', dataIndex: 'remark', key: 'remark', width: 180 },
  { title: '操作', key: 'action', width: 140, fixed: 'right' }
]

const filteredData = computed(() => {
  const keyword = queryForm.keyword.trim().toLowerCase()
  return data.value.filter(item => {
    if (queryForm.orgUnitId && item.actualOrgUnitId !== queryForm.orgUnitId) {
      return false
    }
    if (queryForm.shiftType && item.shiftType !== queryForm.shiftType) {
      return false
    }
    if (queryForm.approvalStatus && item.approvalStatus !== queryForm.approvalStatus) {
      return false
    }
    if (queryForm.dateRange?.length === 2) {
      const current = dayjs(item.date)
      const start = queryForm.dateRange[0].startOf('day')
      const end = queryForm.dateRange[1].endOf('day')
      if (!current.isAfter(start.subtract(1, 'millisecond')) || !current.isBefore(end.add(1, 'millisecond'))) {
        return false
      }
    }
    if (!keyword) {
      return true
    }
    return [item.employeeNo, item.employeeName, item.actualOrgUnitName]
      .filter(Boolean)
      .some(text => `${text}`.toLowerCase().includes(keyword))
  })
})

const summary = computed(() => {
  return filteredData.value.reduce((result, item) => {
    result.total += 1
    result.workingHours += Number(item.workingHours || 0)
    result.overtimeHours += Number(item.overtimeHours || 0)
    return result
  }, {
    total: 0,
    workingHours: 0,
    overtimeHours: 0
  })
})

const approvableRows = computed(() => selectedRows.value.filter(item => item.approvalStatus !== 'Approved'))

watch(filteredData, (value) => {
  if (pagination.current > 1) {
    const maxPage = Math.max(1, Math.ceil(value.length / pagination.pageSize))
    if (pagination.current > maxPage) {
      pagination.current = maxPage
    }
  }
}, { immediate: true })

const buildOrgTree = (list) => {
  const map = new Map()
  const roots = []
  list.forEach(item => {
    map.set(item.id, {
      title: item.name,
      key: item.id,
      value: item.id,
      children: []
    })
  })
  list.forEach(item => {
    const node = map.get(item.id)
    if (item.parentId && map.has(item.parentId)) {
      map.get(item.parentId).children.push(node)
    } else {
      roots.push(node)
    }
  })
  orgTreeData.value = roots
}

const loadOrgUnits = async () => {
  try {
    const result = await orgUnitApi.getAll()
    buildOrgTree(Array.isArray(result) ? result : [])
  } catch (error) {
    orgTreeData.value = []
  }
}

const loadData = async () => {
  loading.value = true
  try {
    const result = await timesheetApi.getAll()
    data.value = Array.isArray(result) ? result : []
    selectedRowKeys.value = []
    selectedRows.value = []
  } catch (error) {
    data.value = []
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.current = 1
}

const handleReset = () => {
  queryForm.keyword = ''
  queryForm.dateRange = null
  queryForm.orgUnitId = undefined
  queryForm.shiftType = undefined
  queryForm.approvalStatus = undefined
  pagination.current = 1
}

const handleTableChange = (pager) => {
  pagination.current = pager.current
  pagination.pageSize = pager.pageSize
}

const handleRowSelect = (keys, rows) => {
  selectedRowKeys.value = keys
  selectedRows.value = rows
}

const getShiftText = (value) => {
  const map = {
    Weekday: '工作日',
    Weekend: '周末',
    Holiday: '节假日'
  }
  return map[value] || value || '-'
}

const getStatusText = (value) => {
  const map = {
    Pending: '待审批',
    Approved: '已审批',
    Rejected: '已驳回'
  }
  return map[value] || value || '-'
}

const getStatusColor = (value) => {
  const map = {
    Pending: 'orange',
    Approved: 'green',
    Rejected: 'red'
  }
  return map[value] || 'default'
}

const formatDate = (value) => value ? dayjs(value).format('YYYY-MM-DD') : '-'
const formatDateTime = (value) => value ? dayjs(value).format('YYYY-MM-DD HH:mm:ss') : '-'
const formatHours = (value) => `${Number(value || 0).toFixed(1)}`
const getRegularHours = (record) => Number(record.workingHours || 0) - Number(record.overtimeHours || 0)

const showAddModal = () => {
  selectedEmployee.value = null
  form.date = dayjs()
  form.actualOrgUnitId = undefined
  form.workingHours = 8
  form.shiftType = 'Weekday'
  form.remark = ''
  modalVisible.value = true
}

const handleView = (record) => {
  viewData.value = record
  viewModalVisible.value = true
}

const shiftValueToNumber = (value) => {
  if (typeof value === 'number') {
    return value
  }
  const map = { Weekday: 0, Weekend: 1, Holiday: 2 }
  return map[value] ?? 0
}

const getApproverId = () => userStore.userInfo?.id

const handleSave = async () => {
  if (!selectedEmployee.value?.id) {
    message.error('请选择员工')
    return
  }
  if (!form.actualOrgUnitId) {
    message.error('请选择实际班组')
    return
  }

  try {
    await timesheetApi.create({
      employeeId: selectedEmployee.value.id,
      date: form.date?.toISOString?.() || dayjs(form.date).toISOString(),
      actualOrgUnitId: form.actualOrgUnitId,
      workingHours: form.workingHours,
      shiftType: shiftValueToNumber(form.shiftType),
      remark: form.remark || null
    })
    message.success('新增成功')
    modalVisible.value = false
    await loadData()
  } catch (error) {
    message.error(error?.response?.data?.message || '新增失败')
  }
}

const approveOne = async (record) => {
  const approverId = getApproverId()
  if (!approverId) {
    message.error('当前登录用户缺少审批人标识')
    return false
  }

  try {
    await timesheetApi.approve(record.id, approverId)
    return true
  } catch (error) {
    message.error(error?.response?.data?.message || `审批失败：${record.employeeName}`)
    return false
  }
}

const handleApprove = async (record) => {
  const success = await approveOne(record)
  if (success) {
    message.success('审批成功')
    await loadData()
  }
}

const handleBatchApprove = async () => {
  if (!approvableRows.value.length) {
    return
  }

  loading.value = true
  try {
    let successCount = 0
    for (const row of approvableRows.value) {
      if (await approveOne(row)) {
        successCount += 1
      }
    }
    await loadData()
    message.success(`批量审批完成，成功 ${successCount} 条`)
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadData(), loadOrgUnits()])
})
</script>

<style scoped>
.timesheet-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.toolbar {
  padding: 12px 16px;
  background: #fff;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
}

.query-input {
  width: 220px;
}

.summary-bar {
  padding: 16px;
  background: #fafafa;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
}
</style>
