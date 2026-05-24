<template>
  <div class="salary-list">
    <div class="toolbar">
      <a-space wrap>
        <a-button v-permission="'page.salary.calculate'" type="primary" @click="$router.push('/salary/calculate')">
          <CalculatorOutlined /> 薪资核算
        </a-button>
        <a-button
          v-permission="'button.salary.approve'"
          :disabled="approvableRows.length === 0"
          @click="handleBatchApprove"
        >
          <CheckCircleOutlined /> 批量审核
        </a-button>
        <a-button
          v-permission="'button.salary.generatePayslip'"
          :disabled="payableRows.length === 0"
          @click="handleBatchPay"
        >
          <PayCircleOutlined /> 批量发放
        </a-button>
        <a-button
          v-permission="'button.salary.adjust'"
          :disabled="rollbackableRows.length === 0"
          @click="handleBatchRollback"
        >
          <RollbackOutlined /> 批量回滚
        </a-button>
        <a-button @click="loadData"><SyncOutlined /> 刷新</a-button>
      </a-space>
    </div>

    <a-card size="small" class="query-card">
      <a-form :model="queryForm" layout="inline">
        <a-form-item label="核算月份">
          <a-month-picker v-model:value="queryForm.period" />
        </a-form-item>
        <a-form-item label="批次状态">
          <a-select v-model:value="queryForm.status" allow-clear placeholder="全部状态" style="width: 160px">
            <a-select-option v-for="option in statusOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="关键字">
          <a-input v-model:value="queryForm.keyword" placeholder="批次号/员工工号/姓名" class="query-input" />
        </a-form-item>
        <a-form-item>
          <a-button type="primary" @click="handleSearch">查询</a-button>
          <a-button @click="handleReset">重置</a-button>
        </a-form-item>
      </a-form>
    </a-card>

    <div class="summary-bar">
      <a-row :gutter="16">
        <a-col :span="6">
          <a-statistic title="批次数" :value="summary.runCount" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="工资单数" :value="summary.payrollCount" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="应发合计" :value="summary.totalGross" prefix="¥" :precision="2" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="实发合计" :value="summary.totalNetSalary" prefix="¥" :precision="2" />
        </a-col>
      </a-row>
    </div>

    <a-table
      :columns="columns"
      :data-source="filteredRuns"
      :loading="loading"
      row-key="id"
      :pagination="pagination"
      :row-selection="{ selectedRowKeys, onChange: handleRowSelect }"
      :scroll="{ x: 1320 }"
      @change="handleTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'totalGross' || column.key === 'totalNetSalary'">
          {{ formatCurrency(record[column.key]) }}
        </template>
        <template v-else-if="column.key === 'startedAt' || column.key === 'finishedAt' || column.key === 'approvedAt'">
          {{ formatDateTime(record[column.key]) }}
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="openRunDetail(record)">查看明细</a-button>
            <a-button
              v-permission="'button.salary.approve'"
              v-if="record.status === 'Calculated'"
              type="link"
              size="small"
              @click="handleApprove(record)"
            >
              审核
            </a-button>
            <a-button
              v-permission="'button.salary.generatePayslip'"
              v-if="record.status === 'Approved'"
              type="link"
              size="small"
              @click="handlePay(record)"
            >
              发放
            </a-button>
            <a-button
              v-permission="'button.salary.adjust'"
              v-if="canRollback(record.status)"
              type="link"
              size="small"
              @click="handleRollback(record)"
            >
              回滚
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-modal v-model:open="detailVisible" title="薪资批次明细" width="1200px" :footer="null">
      <a-spin :spinning="detailLoading">
        <a-descriptions :column="3" bordered size="small" class="detail-desc">
          <a-descriptions-item label="批次号">{{ currentRun?.runNo || '-' }}</a-descriptions-item>
          <a-descriptions-item label="核算月份">{{ currentRun?.yearMonth || '-' }}</a-descriptions-item>
          <a-descriptions-item label="状态">
            <a-tag v-if="currentRun" :color="getStatusColor(currentRun.status)">
              {{ getStatusText(currentRun.status) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="批次类型">{{ getRunTypeText(currentRun?.runType) }}</a-descriptions-item>
          <a-descriptions-item label="工资单数">{{ currentRun?.payrollCount || 0 }}</a-descriptions-item>
          <a-descriptions-item label="审批人">{{ currentRun?.approvedBy || '-' }}</a-descriptions-item>
          <a-descriptions-item label="应发合计">{{ formatCurrency(currentRun?.totalGross) }}</a-descriptions-item>
          <a-descriptions-item label="实发合计">{{ formatCurrency(currentRun?.totalNetSalary) }}</a-descriptions-item>
          <a-descriptions-item label="完成时间">{{ formatDateTime(currentRun?.finishedAt) }}</a-descriptions-item>
          <a-descriptions-item label="备注" :span="3">{{ currentRun?.remark || '-' }}</a-descriptions-item>
        </a-descriptions>

        <a-table
          class="detail-table"
          :columns="payrollColumns"
          :data-source="currentRun?.payrolls || []"
          row-key="payrollId"
          :pagination="false"
          :scroll="{ x: 900, y: 360 }"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'totalGross' || column.key === 'netSalary'">
              {{ formatCurrency(record[column.key]) }}
            </template>
            <template v-else-if="column.key === 'status'">
              <a-tag :color="getStatusColor(record.status)">
                {{ getStatusText(record.status) }}
              </a-tag>
            </template>
            <template v-else-if="column.key === 'salaryMode'">
              {{ getSalaryModeText(record.salaryMode) }}
            </template>
            <template v-else-if="column.key === 'action'">
              <a-button type="link" size="small" @click="openPayrollDetail(record)">
                查看工资单
              </a-button>
            </template>
          </template>
        </a-table>
      </a-spin>
    </a-modal>

    <a-modal v-model:open="payrollDetailVisible" title="员工工资单" width="1000px" :footer="null">
      <a-spin :spinning="payrollDetailLoading">
        <a-descriptions :column="2" bordered size="small" class="detail-desc">
          <a-descriptions-item label="员工工号">{{ currentPayroll?.employeeNo || '-' }}</a-descriptions-item>
          <a-descriptions-item label="员工姓名">{{ currentPayroll?.employeeName || '-' }}</a-descriptions-item>
          <a-descriptions-item label="员工类型">{{ currentPayroll?.employeeTypeName || '-' }}</a-descriptions-item>
          <a-descriptions-item label="薪资模式">{{ getSalaryModeText(currentPayroll?.salaryMode) }}</a-descriptions-item>
          <a-descriptions-item label="工资单状态">
            <a-tag v-if="currentPayroll" :color="getStatusColor(currentPayroll.status)">
              {{ getStatusText(currentPayroll.status) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="发放时间">{{ formatDateTime(currentPayroll?.paidAt) }}</a-descriptions-item>
          <a-descriptions-item label="应发合计">{{ formatCurrency(currentPayroll?.totalGross) }}</a-descriptions-item>
          <a-descriptions-item label="实发工资">{{ formatCurrency(currentPayroll?.netSalary) }}</a-descriptions-item>
          <a-descriptions-item label="公司成本">{{ formatCurrency(currentPayroll?.totalCompanyCost) }}</a-descriptions-item>
          <a-descriptions-item label="个税">{{ formatCurrency(currentPayroll?.incomeTax) }}</a-descriptions-item>
        </a-descriptions>

        <a-table
          class="detail-table"
          :columns="detailColumns"
          :data-source="currentPayroll?.details || []"
          row-key="id"
          :pagination="false"
          :scroll="{ x: 760, y: 360 }"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'amount' || column.key === 'quantity' || column.key === 'unitPrice'">
              {{ formatNumber(record[column.key]) }}
            </template>
          </template>
        </a-table>
      </a-spin>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import dayjs from 'dayjs'
import { message, Modal } from 'ant-design-vue'
import {
  CalculatorOutlined,
  CheckCircleOutlined,
  PayCircleOutlined,
  RollbackOutlined,
  SyncOutlined
} from '@ant-design/icons-vue'
import { payrollApi } from '../../api'

const loading = ref(false)
const detailLoading = ref(false)
const payrollDetailLoading = ref(false)
const data = ref([])
const detailVisible = ref(false)
const payrollDetailVisible = ref(false)
const currentRun = ref(null)
const currentPayroll = ref(null)
const selectedRowKeys = ref([])
const selectedRows = ref([])

const pagination = reactive({
  current: 1,
  pageSize: 10,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showTotal: (total) => `共 ${total} 条记录`
})

const queryForm = reactive({
  period: dayjs(),
  status: undefined,
  keyword: ''
})

const statusOptions = [
  { label: '已核算', value: 'Calculated' },
  { label: '已审核', value: 'Approved' },
  { label: '已发放', value: 'Paid' },
  { label: '已驳回', value: 'ApprovalRejected' },
  { label: '已回滚', value: 'RolledBack' },
  { label: '草稿', value: 'Draft' }
]

const columns = [
  { title: '核算月份', dataIndex: 'yearMonth', key: 'yearMonth', width: 120 },
  { title: '批次号', dataIndex: 'runNo', key: 'runNo', width: 220 },
  { title: '批次类型', dataIndex: 'runType', key: 'runType', width: 100, customRender: ({ record }) => getRunTypeText(record.runType) },
  { title: '工资单数', dataIndex: 'payrollCount', key: 'payrollCount', width: 100 },
  { title: '应发合计', dataIndex: 'totalGross', key: 'totalGross', width: 120 },
  { title: '实发合计', dataIndex: 'totalNetSalary', key: 'totalNetSalary', width: 120 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 120 },
  { title: '提交时间', dataIndex: 'startedAt', key: 'startedAt', width: 180 },
  { title: '完成时间', dataIndex: 'finishedAt', key: 'finishedAt', width: 180 },
  { title: '审核时间', dataIndex: 'approvedAt', key: 'approvedAt', width: 180 },
  { title: '操作', key: 'action', width: 220, fixed: 'right' }
]

const payrollColumns = [
  { title: '工号', dataIndex: 'employeeNo', key: 'employeeNo', width: 120 },
  { title: '姓名', dataIndex: 'employeeName', key: 'employeeName', width: 120 },
  { title: '员工类型', dataIndex: 'employeeTypeName', key: 'employeeTypeName', width: 140 },
  { title: '薪资模式', dataIndex: 'salaryMode', key: 'salaryMode', width: 120 },
  { title: '应发', dataIndex: 'totalGross', key: 'totalGross', width: 120 },
  { title: '实发', dataIndex: 'netSalary', key: 'netSalary', width: 120 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 120 },
  { title: '操作', key: 'action', width: 120, fixed: 'right' }
]

const detailColumns = [
  { title: '项目编码', dataIndex: 'componentCode', key: 'componentCode', width: 120 },
  { title: '项目名称', dataIndex: 'componentName', key: 'componentName', width: 140 },
  { title: '分类', dataIndex: 'componentCategory', key: 'componentCategory', width: 120 },
  { title: '数量', dataIndex: 'quantity', key: 'quantity', width: 100 },
  { title: '单价', dataIndex: 'unitPrice', key: 'unitPrice', width: 100 },
  { title: '金额', dataIndex: 'amount', key: 'amount', width: 120 },
  { title: '来源', dataIndex: 'sourceType', key: 'sourceType', width: 120 },
  { title: '备注', dataIndex: 'remark', key: 'remark', width: 180 }
]

const filteredRuns = computed(() => {
  const keyword = queryForm.keyword.trim().toLowerCase()
  return data.value.filter(run => {
    if (queryForm.status && run.status !== queryForm.status) {
      return false
    }

    if (!keyword) {
      return true
    }

    const matchedRun = [run.runNo, run.yearMonth, run.remark]
      .filter(Boolean)
      .some(item => `${item}`.toLowerCase().includes(keyword))

    if (matchedRun) {
      return true
    }

    return (run.payrolls || []).some(payroll =>
      [payroll.employeeNo, payroll.employeeName]
        .filter(Boolean)
        .some(item => `${item}`.toLowerCase().includes(keyword))
    )
  })
})

const summary = computed(() => {
  return filteredRuns.value.reduce((result, run) => {
    result.runCount += 1
    result.payrollCount += Number(run.payrollCount || 0)
    result.totalGross += Number(run.totalGross || 0)
    result.totalNetSalary += Number(run.totalNetSalary || 0)
    return result
  }, {
    runCount: 0,
    payrollCount: 0,
    totalGross: 0,
    totalNetSalary: 0
  })
})

const approvableRows = computed(() => selectedRows.value.filter(item => item.status === 'Calculated'))
const payableRows = computed(() => selectedRows.value.filter(item => item.status === 'Approved'))
const rollbackableRows = computed(() => selectedRows.value.filter(item => canRollback(item.status)))

watch(filteredRuns, (value) => {
  if (pagination.current > 1) {
    const maxPage = Math.max(1, Math.ceil(value.length / pagination.pageSize))
    if (pagination.current > maxPage) {
      pagination.current = maxPage
    }
  }
}, { immediate: true })

const getPeriodValue = () => {
  if (!queryForm.period) {
    return dayjs().format('YYYY-MM')
  }
  return typeof queryForm.period?.format === 'function'
    ? queryForm.period.format('YYYY-MM')
    : dayjs(queryForm.period).format('YYYY-MM')
}

const loadData = async () => {
  loading.value = true
  try {
    const result = await payrollApi.getRuns(getPeriodValue())
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
  loadData()
}

const handleReset = () => {
  queryForm.period = dayjs()
  queryForm.status = undefined
  queryForm.keyword = ''
  pagination.current = 1
  loadData()
}

const handleTableChange = (pager) => {
  pagination.current = pager.current
  pagination.pageSize = pager.pageSize
}

const handleRowSelect = (keys, rows) => {
  selectedRowKeys.value = keys
  selectedRows.value = rows
}

const getRunTypeText = (value) => {
  return value === 'Trial' ? '试算' : value === 'Monthly' ? '正式核算' : value || '-'
}

const getSalaryModeText = (value) => {
  const map = {
    Fixed: '固定薪',
    Hourly: '时薪',
    Piecework: '计件',
    BasePlusPiecework: '底薪+计件',
    ThirdParty: '第三方',
    Management: '管理岗'
  }
  return map[value] || value || '-'
}

const getStatusText = (value) => {
  const map = {
    Draft: '草稿',
    Calculated: '已核算',
    Approved: '已审核',
    Paid: '已发放',
    ApprovalRejected: '审批驳回',
    RolledBack: '已回滚'
  }
  return map[value] || value || '-'
}

const getStatusColor = (value) => {
  const map = {
    Draft: 'default',
    Calculated: 'processing',
    Approved: 'blue',
    Paid: 'green',
    ApprovalRejected: 'red',
    RolledBack: 'orange'
  }
  return map[value] || 'default'
}

const canRollback = (status) => ['Calculated', 'Approved', 'ApprovalRejected'].includes(status)

const formatCurrency = (value) => {
  const amount = Number(value || 0)
  return `¥${amount.toFixed(2)}`
}

const formatNumber = (value) => {
  if (value === null || value === undefined || value === '') {
    return '-'
  }
  return Number(value).toFixed(2)
}

const formatDateTime = (value) => {
  if (!value) {
    return '-'
  }
  return dayjs(value).format('YYYY-MM-DD HH:mm:ss')
}

const openRunDetail = async (record) => {
  detailVisible.value = true
  detailLoading.value = true
  try {
    currentRun.value = await payrollApi.getRun(record.id)
  } finally {
    detailLoading.value = false
  }
}

const openPayrollDetail = async (record) => {
  if (!currentRun.value?.id) {
    return
  }

  payrollDetailVisible.value = true
  payrollDetailLoading.value = true
  try {
    currentPayroll.value = await payrollApi.getRunEmployee(currentRun.value.id, record.employeeId)
  } finally {
    payrollDetailLoading.value = false
  }
}

const refreshCurrentRun = async (runId) => {
  if (currentRun.value?.id === runId) {
    currentRun.value = await payrollApi.getRun(runId)
  }
}

const approveRun = async (runId) => {
  await payrollApi.approve(runId, {})
}

const payRun = async (runId) => {
  await payrollApi.pay(runId, {})
}

const rollbackRun = async (runId) => {
  await payrollApi.rollback(runId, '前端手动回滚')
}

const handleApprove = async (record) => {
  try {
    await approveRun(record.id)
    message.success('批次审核成功')
    await loadData()
    await refreshCurrentRun(record.id)
  } catch (error) {
    message.error(error?.response?.data?.message || '批次审核失败')
  }
}

const handlePay = async (record) => {
  try {
    await payRun(record.id)
    message.success('批次发放成功')
    await loadData()
    await refreshCurrentRun(record.id)
  } catch (error) {
    message.error(error?.response?.data?.message || '批次发放失败')
  }
}

const handleRollback = (record) => {
  Modal.confirm({
    title: '确认回滚',
    content: `确定回滚批次 ${record.runNo} 吗？`,
    okText: '确认',
    cancelText: '取消',
    onOk: async () => {
      try {
        await rollbackRun(record.id)
        message.success('批次回滚成功')
        await loadData()
        await refreshCurrentRun(record.id)
      } catch (error) {
        message.error(error?.response?.data?.message || '批次回滚失败')
      }
    }
  })
}

const runBatchAction = async (rows, action, successText) => {
  if (!rows.length) {
    return
  }

  loading.value = true
  try {
    for (const row of rows) {
      await action(row.id)
    }
    message.success(successText)
    await loadData()
    if (currentRun.value?.id) {
      await refreshCurrentRun(currentRun.value.id)
    }
    selectedRowKeys.value = []
    selectedRows.value = []
  } catch (error) {
    message.error(error?.response?.data?.message || `${successText}失败`)
  } finally {
    loading.value = false
  }
}

const handleBatchApprove = () => runBatchAction(approvableRows.value, approveRun, '批量审核完成')
const handleBatchPay = () => runBatchAction(payableRows.value, payRun, '批量发放完成')
const handleBatchRollback = () => {
  Modal.confirm({
    title: '确认批量回滚',
    content: `确定回滚选中的 ${rollbackableRows.value.length} 个批次吗？`,
    okText: '确认',
    cancelText: '取消',
    onOk: () => runBatchAction(rollbackableRows.value, rollbackRun, '批量回滚完成')
  })
}

onMounted(loadData)
</script>

<style scoped>
.salary-list {
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

.query-card {
  border-radius: 8px;
}

.query-input {
  width: 240px;
}

.summary-bar {
  padding: 16px;
  background: #fafafa;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
}

.detail-desc,
.detail-table {
  margin-top: 16px;
}
</style>
