<template>
  <div class="salary-calculate">
    <a-card title="核算参数" class="param-card">
      <a-form :model="form" :label-col="{ span: 4 }">
        <a-form-item label="核算月份">
          <a-month-picker v-model:value="form.month" />
        </a-form-item>
        <a-form-item label="组织范围">
          <a-tree-select
            v-model:value="form.orgUnitIds"
            :tree-data="orgTreeData"
            tree-checkable
            multiple
            allow-clear
            placeholder="不选则按全部参与算薪员工"
            style="width: 100%;"
          />
        </a-form-item>
        <a-form-item label="备注">
          <a-textarea v-model:value="form.remark" :rows="3" placeholder="可填写本次核算说明" />
        </a-form-item>
        <a-form-item :wrapper-col="{ offset: 4 }">
          <a-space>
            <a-button v-permission="'button.salary.calculate'" type="primary" :loading="trialLoading" @click="handleTrial">
              <CalculatorOutlined /> 试算
            </a-button>
            <a-button
              v-permission="'button.salary.calculate'"
              type="primary"
              danger
              :loading="calculateLoading"
              @click="handleCalculate"
            >
              <CheckCircleOutlined /> 正式核算
            </a-button>
            <a-button @click="handleReset">重置</a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>

    <a-alert
      v-if="previewRun"
      type="info"
      show-icon
      class="result-alert"
      :message="previewRun.id ? '已生成正式薪资批次，可到薪资列表继续审核/发放。' : '当前为试算结果，未写入正式批次。'"
    />

    <a-card v-if="previewRun" title="核算结果" class="param-card">
      <a-row :gutter="16">
        <a-col :span="6">
          <a-statistic title="月份" :value="previewRun.yearMonth" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="工资单数" :value="previewRun.payrollCount" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="应发合计" :value="previewRun.totalGross" prefix="¥" :precision="2" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="实发合计" :value="previewRun.totalNetSalary" prefix="¥" :precision="2" />
        </a-col>
      </a-row>

      <a-divider />

      <a-row :gutter="16">
        <a-col v-for="item in groupedBySalaryMode" :key="item.mode" :span="6">
          <a-card size="small">
            <a-statistic :title="getSalaryModeText(item.mode)" :value="item.total" prefix="¥" :precision="2" />
            <div class="group-extra">{{ item.count }} 人</div>
          </a-card>
        </a-col>
      </a-row>
    </a-card>

    <a-card v-if="previewRun" title="工资单明细" class="param-card">
      <a-table
        :columns="detailColumns"
        :data-source="previewRun.payrolls || []"
        row-key="payrollId"
        :pagination="{ pageSize: 10, showSizeChanger: true }"
        :scroll="{ x: 1200 }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'salaryMode'">
            {{ getSalaryModeText(record.salaryMode) }}
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag :color="getStatusColor(record.status)">
              {{ getStatusText(record.status) }}
            </a-tag>
          </template>
          <template v-else-if="amountFields.includes(column.key)">
            {{ formatCurrency(record[column.key]) }}
          </template>
        </template>
      </a-table>
    </a-card>

    <a-card title="本月历史批次">
      <a-table
        :columns="historyColumns"
        :data-source="historyRuns"
        :loading="historyLoading"
        row-key="id"
        :pagination="false"
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
          <template v-else-if="column.key === 'finishedAt'">
            {{ formatDateTime(record.finishedAt) }}
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import dayjs from 'dayjs'
import { message, Modal } from 'ant-design-vue'
import { CalculatorOutlined, CheckCircleOutlined } from '@ant-design/icons-vue'
import { orgUnitApi, payrollApi } from '../../api'

const trialLoading = ref(false)
const calculateLoading = ref(false)
const historyLoading = ref(false)
const orgTreeData = ref([])
const historyRuns = ref([])
const previewRun = ref(null)

const amountFields = [
  'normalWage',
  'overtimeWage',
  'pieceworkWage',
  'fixedSalary',
  'baseSalary',
  'mealSubsidy',
  'nightSubsidy',
  'performanceBonus',
  'otherAllowance',
  'incomeTax',
  'otherDeduction',
  'totalGross',
  'netSalary',
  'totalCompanyCost'
]

const form = reactive({
  month: dayjs(),
  orgUnitIds: [],
  remark: ''
})

const detailColumns = [
  { title: '工号', dataIndex: 'employeeNo', key: 'employeeNo', width: 120 },
  { title: '姓名', dataIndex: 'employeeName', key: 'employeeName', width: 120 },
  { title: '员工类型', dataIndex: 'employeeTypeName', key: 'employeeTypeName', width: 140 },
  { title: '薪资模式', dataIndex: 'salaryMode', key: 'salaryMode', width: 120 },
  { title: '正常工资', dataIndex: 'normalWage', key: 'normalWage', width: 120 },
  { title: '加班费', dataIndex: 'overtimeWage', key: 'overtimeWage', width: 120 },
  { title: '计件工资', dataIndex: 'pieceworkWage', key: 'pieceworkWage', width: 120 },
  { title: '固定工资', dataIndex: 'fixedSalary', key: 'fixedSalary', width: 120 },
  { title: '应发', dataIndex: 'totalGross', key: 'totalGross', width: 120 },
  { title: '实发', dataIndex: 'netSalary', key: 'netSalary', width: 120 },
  { title: '状态', dataIndex: 'status', key: 'status', width: 120 }
]

const historyColumns = [
  { title: '批次号', dataIndex: 'runNo', key: 'runNo' },
  { title: '状态', dataIndex: 'status', key: 'status', width: 120 },
  { title: '工资单数', dataIndex: 'payrollCount', key: 'payrollCount', width: 120 },
  { title: '应发合计', dataIndex: 'totalGross', key: 'totalGross', width: 140 },
  { title: '实发合计', dataIndex: 'totalNetSalary', key: 'totalNetSalary', width: 140 },
  { title: '完成时间', dataIndex: 'finishedAt', key: 'finishedAt', width: 180 }
]

const groupedBySalaryMode = computed(() => {
  if (!previewRun.value?.payrolls?.length) {
    return []
  }

  const map = new Map()
  previewRun.value.payrolls.forEach(item => {
    const key = item.salaryMode || 'Unknown'
    const current = map.get(key) || { mode: key, count: 0, total: 0 }
    current.count += 1
    current.total += Number(item.netSalary || 0)
    map.set(key, current)
  })
  return Array.from(map.values())
})

const getYearMonth = () => {
  return typeof form.month?.format === 'function'
    ? form.month.format('YYYY-MM')
    : dayjs(form.month).format('YYYY-MM')
}

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

const loadHistoryRuns = async () => {
  historyLoading.value = true
  try {
    const result = await payrollApi.getRuns(getYearMonth())
    historyRuns.value = Array.isArray(result) ? result : []
  } catch (error) {
    historyRuns.value = []
  } finally {
    historyLoading.value = false
  }
}

const buildPayload = () => ({
  yearMonth: getYearMonth(),
  orgUnitIds: form.orgUnitIds?.length ? [...form.orgUnitIds] : null,
  remark: form.remark?.trim() || null
})

const handleTrial = async () => {
  trialLoading.value = true
  try {
    previewRun.value = await payrollApi.trial(buildPayload())
    message.success('试算完成')
  } catch (error) {
    message.error(error?.response?.data?.message || '试算失败')
  } finally {
    trialLoading.value = false
  }
}

const handleCalculate = () => {
  Modal.confirm({
    title: '确认正式核算',
    content: `确定生成 ${getYearMonth()} 的正式薪资批次吗？`,
    okText: '确认',
    cancelText: '取消',
    onOk: async () => {
      calculateLoading.value = true
      try {
        previewRun.value = await payrollApi.calculate(buildPayload())
        message.success('正式核算成功')
        await loadHistoryRuns()
      } catch (error) {
        message.error(error?.response?.data?.message || '正式核算失败')
      } finally {
        calculateLoading.value = false
      }
    }
  })
}

const handleReset = () => {
  form.month = dayjs()
  form.orgUnitIds = []
  form.remark = ''
  previewRun.value = null
  loadHistoryRuns()
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

const formatCurrency = (value) => `¥${Number(value || 0).toFixed(2)}`
const formatDateTime = (value) => value ? dayjs(value).format('YYYY-MM-DD HH:mm:ss') : '-'

onMounted(async () => {
  await Promise.all([loadOrgUnits(), loadHistoryRuns()])
})
</script>

<style scoped>
.salary-calculate {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding-bottom: 24px;
}

.param-card {
  border-radius: 8px;
}

.result-alert {
  margin-bottom: 0;
}

.group-extra {
  margin-top: 8px;
  color: #8c8c8c;
}
</style>
