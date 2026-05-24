<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h1>报表中心</h1>
        <p>基于现有后端数据实时汇总员工、工时、薪资和外包账单。</p>
      </div>
      <a-button @click="loadData"><ReloadOutlined /> 刷新数据</a-button>
    </div>

    <a-row :gutter="16" class="stats-row">
      <a-col :span="6">
        <a-card :loading="loading">
          <a-statistic title="员工总数" :value="summary.employeeCount" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card :loading="loading">
          <a-statistic title="在职员工" :value="summary.activeEmployeeCount" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card :loading="loading">
          <a-statistic title="本月工时" :value="summary.monthWorkingHours" :precision="1" suffix="小时" />
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card :loading="loading">
          <a-statistic title="本月实发工资" :value="summary.monthNetSalary" prefix="¥" :precision="2" />
        </a-card>
      </a-col>
    </a-row>

    <a-row :gutter="16">
      <a-col :span="12">
        <a-card title="组织工时排行" :loading="loading">
          <a-table :columns="orgColumns" :data-source="orgRows" row-key="name" :pagination="false" />
        </a-card>
      </a-col>
      <a-col :span="12">
        <a-card title="本月薪资 Top 10" :loading="loading">
          <a-table :columns="salaryColumns" :data-source="salaryRows" row-key="id" :pagination="false" />
        </a-card>
      </a-col>
    </a-row>

    <a-row :gutter="16" class="bottom-row">
      <a-col :span="12">
        <a-card title="员工类型分布" :loading="loading">
          <a-table :columns="employeeTypeColumns" :data-source="employeeTypeRows" row-key="name" :pagination="false" />
        </a-card>
      </a-col>
      <a-col :span="12">
        <a-card title="外包账单概况" :loading="loading">
          <a-descriptions :column="2" bordered size="small">
            <a-descriptions-item label="账单数">{{ billSummary.count }}</a-descriptions-item>
            <a-descriptions-item label="账单总额">{{ formatCurrency(billSummary.totalAmount) }}</a-descriptions-item>
            <a-descriptions-item label="已支付">{{ billSummary.paidCount }}</a-descriptions-item>
            <a-descriptions-item label="待支付">{{ billSummary.pendingCount }}</a-descriptions-item>
          </a-descriptions>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import dayjs from 'dayjs'
import { employeeApi, salaryApi, thirdPartyBillApi, timesheetApi } from '../../api'
import { ReloadOutlined } from '@ant-design/icons-vue'

const loading = ref(false)
const employees = ref([])
const salaries = ref([])
const timesheets = ref([])
const bills = ref([])

function formatCurrency(value) {
  return `¥${Number(value || 0).toFixed(2)}`
}

const orgColumns = [
  { title: '组织', dataIndex: 'name', key: 'name' },
  { title: '工时', dataIndex: 'workingHours', key: 'workingHours' },
  { title: '加班工时', dataIndex: 'overtimeHours', key: 'overtimeHours' }
]

const salaryColumns = [
  { title: '工号', dataIndex: 'employeeNo', key: 'employeeNo', width: 120 },
  { title: '姓名', dataIndex: 'employeeName', key: 'employeeName', width: 120 },
  { title: '实发工资', dataIndex: 'netWages', key: 'netWages', width: 140, customRender: ({ record }) => formatCurrency(record.netWages) },
  { title: '应发工资', dataIndex: 'grossWages', key: 'grossWages', width: 140, customRender: ({ record }) => formatCurrency(record.grossWages) }
]

const employeeTypeColumns = [
  { title: '员工类型', dataIndex: 'name', key: 'name' },
  { title: '人数', dataIndex: 'count', key: 'count' }
]

const currentMonth = dayjs().format('YYYY-MM')

const monthTimesheets = computed(() => {
  return timesheets.value.filter(item => dayjs(item.date).format('YYYY-MM') === currentMonth)
})

const monthSalaries = computed(() => {
  return salaries.value.filter(item => `${item.year}-${String(item.month).padStart(2, '0')}` === currentMonth)
})

const summary = computed(() => {
  return {
    employeeCount: employees.value.length,
    activeEmployeeCount: employees.value.filter(item => item.isActive && !item.dismissDate).length,
    monthWorkingHours: monthTimesheets.value.reduce((sum, item) => sum + Number(item.workingHours || 0), 0),
    monthNetSalary: monthSalaries.value.reduce((sum, item) => sum + Number(item.netWages || 0), 0)
  }
})

const orgRows = computed(() => {
  const map = new Map()
  monthTimesheets.value.forEach(item => {
    const key = item.actualOrgUnitName || '未分配班组'
    const current = map.get(key) || { name: key, workingHours: 0, overtimeHours: 0 }
    current.workingHours += Number(item.workingHours || 0)
    current.overtimeHours += Number(item.overtimeHours || 0)
    map.set(key, current)
  })
  return Array.from(map.values())
    .sort((a, b) => b.workingHours - a.workingHours)
    .slice(0, 10)
})

const salaryRows = computed(() => {
  return [...monthSalaries.value]
    .sort((a, b) => Number(b.netWages || 0) - Number(a.netWages || 0))
    .slice(0, 10)
})

const employeeTypeRows = computed(() => {
  const map = new Map()
  employees.value.forEach(item => {
    const key = item.employeeType || '未配置'
    map.set(key, (map.get(key) || 0) + 1)
  })
  return Array.from(map.entries()).map(([name, count]) => ({ name, count }))
})

const billSummary = computed(() => {
  return bills.value.reduce((result, item) => {
    result.count += 1
    result.totalAmount += Number(item.totalAmount || item.amount || 0)
    if (`${item.status}` === 'Paid') {
      result.paidCount += 1
    } else {
      result.pendingCount += 1
    }
    return result
  }, {
    count: 0,
    totalAmount: 0,
    paidCount: 0,
    pendingCount: 0
  })
})

const loadData = async () => {
  loading.value = true
  try {
    const [employeeResult, salaryResult, timesheetResult, billResult] = await Promise.all([
      employeeApi.getAll(),
      salaryApi.getAll(),
      timesheetApi.getAll(),
      thirdPartyBillApi.getAll()
    ])
    employees.value = Array.isArray(employeeResult?.list) ? employeeResult.list : (Array.isArray(employeeResult) ? employeeResult : [])
    salaries.value = Array.isArray(salaryResult) ? salaryResult : []
    timesheets.value = Array.isArray(timesheetResult) ? timesheetResult : []
    bills.value = Array.isArray(billResult) ? billResult : []
  } finally {
    loading.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.page-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
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

.stats-row,
.bottom-row {
  margin-top: 0;
}
</style>
