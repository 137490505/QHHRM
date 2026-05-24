<template>
  <div class="employee-list">
    <!-- 工具栏 -->
    <div class="toolbar">
      <a-space>
        <a-button v-permission="'button.employee.create'" type="primary" data-testid="employee-add" @click="showAddModal"><PlusOutlined /> 新增员工</a-button>
        <a-button v-permission="'button.employee.import'" @click="handleBatchImport"><UploadOutlined /> 批量导入</a-button>
        <a-button v-permission="'button.employee.export'" @click="handleBatchExport"><DownloadOutlined /> 批量导出</a-button>
        <a-button 
          v-permission="'button.employee.dismiss'"
          v-if="selectedRows.length > 0" 
          @click="handleBatchDismiss" 
          danger
        ><UserDeleteOutlined /> 批量离职</a-button>
        <a-button @click="loadData"><SyncOutlined /> 刷新</a-button>
      </a-space>
    </div>

    <!-- 查询区 -->
    <div class="advanced-query-panel">
      <a-form :model="queryForm" layout="inline" class="query-form">
        <a-form-item name="keyword" label="关键字">
          <a-input
            v-model:value="queryForm.keyword"
            placeholder="工号/姓名/身份证/手机"
            class="query-input"
            allow-clear
          />
        </a-form-item>
        <a-form-item name="orgUnitId" label="所属组织">
          <a-tree-select
            v-model:value="queryForm.orgUnitId"
            :tree-data="orgTreeData"
            placeholder="请选择组织"
            style="width: 200px"
            allow-clear
            :tree-default-expand-all="false"
          />
        </a-form-item>
        <a-form-item name="employeeType" label="员工类型">
          <a-select v-model:value="queryForm.employeeType" placeholder="请选择" style="width: 120px" allow-clear>
            <a-select-option value="">全部</a-select-option>
            <a-select-option
              v-for="option in employeeTypeOptions"
              :key="option.value"
              :value="option.value"
            >
              {{ option.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item name="isActive" label="状态">
          <a-select v-model:value="queryForm.isActive" placeholder="请选择" style="width: 100px" allow-clear>
            <a-select-option value="">全部</a-select-option>
            <a-select-option value="true">在职</a-select-option>
            <a-select-option value="false">离职</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item>
          <a-space>
            <a-button type="primary" @click="handleSearch">
              <template #icon><SearchOutlined /></template>
              查询
            </a-button>
            <a-button @click="handleReset">重置</a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </div>

    <!-- 表格 -->
    <a-table 
      :columns="columns" 
      :data-source="data" 
      :loading="loading" 
      row-key="id" 
      :pagination="pagination"
      :row-selection="{ selectedRowKeys: selectedRowKeys, onChange: handleRowSelect }"
      @change="handleTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'employeeType'">
          {{ employeeTypeMap[record.employeeType] || record.employeeType }}
        </template>
        <template v-if="column.key === 'salaryMode'">
          {{ salaryModeMap[record.salaryMode] || record.salaryMode }}
        </template>
        <template v-if="column.key === 'hireDate'">
          {{ formatDate(record.hireDate) }}
        </template>
        <template v-if="column.key === 'tags'">
          <a-tag v-for="tag in record.tags" :key="tag" color="blue">{{ tag }}</a-tag>
        </template>
        <template v-if="column.key === 'isActive'">
          <a-tag :color="record.isActive ? 'green' : 'red'">
            {{ record.isActive ? '在职' : '离职' }}
          </a-tag>
        </template>
        <template v-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" :data-testid="`employee-view-${record.id}`" @click="handleView(record)">查看</a-button>
            <a-button v-permission="'button.employee.edit'" type="link" size="small" :data-testid="`employee-edit-${record.id}`" @click="handleEdit(record)">编辑</a-button>
            <a-popconfirm
              :title="record.isActive ? '确定要办理离职吗？' : '确定要办理复职吗？'"
              @confirm="handleToggleDismiss(record)"
            >
              <a-button 
                v-permission="'button.employee.dismiss'"
                type="link" 
                size="small" 
                :data-testid="`employee-dismiss-${record.id}`"
                :danger="record.isActive"
              >{{ record.isActive ? '离职' : '复职' }}</a-button>
            </a-popconfirm>
            <a-button v-permission="'button.employee.resetPassword'" type="link" size="small" @click="handleResetPassword(record)">重置密码</a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <!-- 新增/编辑弹窗 -->
    <a-modal 
      v-model:open="modalVisible" 
      :title="editingRecord ? '编辑员工' : '新增员工'" 
      :width="800"
      @ok="handleSave"
      :destroyOnClose="true"
    >
      <a-steps :current="currentStep" class="steps" size="small">
        <a-step title="基本信息" />
        <a-step title="组织信息" />
        <a-step title="薪资信息" />
        <a-step title="合同信息" />
      </a-steps>

      <div class="step-content">
        <!-- 基本信息 -->
        <div v-if="currentStep === 0">
          <a-form :model="form" :label-col="{ span: 6 }" layout="horizontal">
            <a-form-item name="employeeNo" label="工号" :rules="[{ required: true, message: '请输入工号' }]">
              <a-input id="employee-no" v-model:value="form.employeeNo" :disabled="!!editingRecord" placeholder="请输入数字工号" />
            </a-form-item>
            <a-form-item name="name" label="姓名" :rules="[{ required: true, message: '请输入姓名' }]">
              <a-input id="employee-name" v-model:value="form.name" />
            </a-form-item>
            <a-form-item name="gender" label="性别">
              <a-radio-group v-model:value="form.gender">
                <a-radio :value="0">男</a-radio>
                <a-radio :value="1">女</a-radio>
              </a-radio-group>
            </a-form-item>
            <a-form-item name="idCard" label="身份证号" :rules="[{ required: true, message: '请输入身份证号' }]">
              <a-input id="employee-id-card" v-model:value="form.idCard" placeholder="18位身份证号" />
            </a-form-item>
            <a-form-item name="phone" label="手机号" :rules="[{ required: true, message: '请输入手机号' }]">
              <a-input id="employee-phone" v-model:value="form.phone" placeholder="11位手机号" />
            </a-form-item>
          </a-form>
        </div>

        <!-- 组织信息 -->
        <div v-if="currentStep === 1">
          <a-form :model="form" :label-col="{ span: 6 }" layout="horizontal">
            <a-form-item name="orgUnitId" label="所属组织" :rules="[{ required: true, message: '请选择组织' }]">
              <a-tree-select
                id="employee-org-unit"
                v-model:value="form.orgUnitId"
                :tree-data="orgTreeData"
                placeholder="选择组织单元"
                allow-clear
                tree-default-expand-all
              />
            </a-form-item>
            <a-form-item name="employeeType" label="员工类型">
              <a-select v-model:value="form.employeeType" id="employee-type">
                <a-select-option
                  v-for="option in employeeTypeOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ option.label }}
                </a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item name="level" label="岗级">
              <a-select v-model:value="form.level" placeholder="请选择岗级" id="employee-level">
                <a-select-option v-for="opt in jobLevelOptions" :key="opt.value" :value="opt.value">
                  {{ opt.label }}
                </a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item 
              v-if="Number(form.employeeType) === 1" 
              name="thirdPartyCompanyId" 
              label="第三方公司" 
              :rules="[{ required: true, message: '请选择第三方公司' }]"
            >
              <a-select v-model:value="form.thirdPartyCompanyId" placeholder="选择供应商" id="employee-third-party-company">
                <a-select-option v-for="s in suppliers" :key="s.id" :value="s.id">
                  {{ s.name }}
                </a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item name="tags" label="员工标识">
              <a-select v-model:value="form.tags" mode="multiple" placeholder="请选择或输入标签">
                <a-select-option value="操作员">操作员</a-select-option>
                <a-select-option value="测试员">测试员</a-select-option>
                <a-select-option value="组装工">组装工</a-select-option>
                <a-select-option value="点胶/涂覆/贴合">点胶/涂覆/贴合</a-select-option>
                <a-select-option value="功能测试员">功能测试员</a-select-option>
                <a-select-option value="外观检验员">外观检验员</a-select-option>
                <a-select-option value="清洁/擦拭工">清洁/擦拭工</a-select-option>
                <a-select-option value="包装工">包装工</a-select-option>
                <a-select-option value="物料与产线配送">物料与产线配送</a-select-option>
                <a-select-option value="备料员">备料员</a-select-option>
                <a-select-option value="产线物料员">产线物料员</a-select-option>
                <a-select-option value="功能不良品维修">功能不良品维修</a-select-option>
              </a-select>
            </a-form-item>
          </a-form>
        </div>

        <!-- 薪资信息 -->
        <div v-if="currentStep === 2">
          <a-form :model="form" :label-col="{ span: 6 }" layout="horizontal">
            <a-form-item name="salaryMode" label="薪资模式">
              <a-select v-model:value="form.salaryMode" id="employee-salary-mode">
                <a-select-option
                  v-for="option in salaryModeOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ option.label }}
                </a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item name="hourlyRate" label="时薪单价">
              <a-input-number id="employee-hourly-rate" v-model:value="form.hourlyRate" :min="0" :precision="2" style="width: 100%" />
            </a-form-item>
            <a-form-item name="monthlySalary" label="固薪金额">
              <a-input-number id="employee-monthly-salary" v-model:value="form.monthlySalary" :min="0" :precision="2" style="width: 100%" />
            </a-form-item>
            <a-form-item name="socialSecurityBase" label="社保基数">
              <a-input-number id="employee-social-security-base" v-model:value="form.socialSecurityBase" :min="0" style="width: 100%" />
            </a-form-item>
            <a-form-item name="housingFundBase" label="公积金基数">
              <a-input-number id="employee-housing-fund-base" v-model:value="form.housingFundBase" :min="0" style="width: 100%" />
            </a-form-item>
          </a-form>
        </div>

        <!-- 合同信息 -->
        <div v-if="currentStep === 3">
          <a-form :model="form" :label-col="{ span: 6 }" layout="horizontal">
            <a-form-item name="contractType" label="合同类型">
              <a-select v-model:value="form.contractType" id="employee-contract-type">
                <a-select-option
                  v-for="option in contractTypeOptions"
                  :key="option.value"
                  :value="option.value"
                >
                  {{ option.label }}
                </a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item name="contractStartDate" label="开始日期">
              <a-date-picker id="employee-contract-start-date" v-model:value="form.contractStartDate" style="width: 100%" />
            </a-form-item>
            <a-form-item name="contractEndDate" label="结束日期">
              <a-date-picker id="employee-contract-end-date" v-model:value="form.contractEndDate" style="width: 100%" />
            </a-form-item>
            <a-form-item name="probationDays" label="试用期天数">
              <a-input-number id="employee-probation-days" v-model:value="form.probationDays" :min="0" style="width: 100%" />
            </a-form-item>
            <a-form-item name="hireDate" label="入职日期">
              <a-date-picker id="employee-hire-date" v-model:value="form.hireDate" style="width: 100%" />
            </a-form-item>
          </a-form>
        </div>
      </div>

      <template #footer>
        <a-button 
          v-if="currentStep > 0" 
          @click="currentStep--"
        >上一步</a-button>
        <a-button 
          v-if="currentStep < 3" 
          type="primary" 
          @click="currentStep++"
        >下一步</a-button>
        <a-button 
          v-if="currentStep === 3" 
          type="primary" 
          data-testid="employee-save"
          @click="handleSave"
        >{{ editingRecord ? '保存修改' : '创建员工' }}</a-button>
        <a-button @click="modalVisible = false">取消</a-button>
      </template>
    </a-modal>

    <!-- 批量离职弹窗 -->
    <a-modal v-model:open="batchDismissModalVisible" title="批量离职" @ok="handleBatchDismissConfirm">
      <a-form :model="batchDismissForm" layout="vertical">
        <a-form-item name="dismissDate" label="离职日期" :rules="[{ required: true, message: '请选择离职日期' }]">
          <a-date-picker v-model:value="batchDismissForm.dismissDate" style="width: 100%;" id="emp-dismiss-datepicker" />
        </a-form-item>
        <a-form-item name="reason" label="离职原因">
          <a-textarea v-model:value="batchDismissForm.reason" rows="3" id="emp-dismiss-reason" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 重置密码弹窗 -->
    <a-modal v-model:open="resetPasswordModalVisible" title="重置密码" @ok="handleResetPasswordConfirm">
      <a-form :model="resetPasswordForm" layout="vertical">
        <a-form-item name="password" label="新密码">
          <a-input-password v-model:value="resetPasswordForm.password" id="emp-new-password" />
        </a-form-item>
        <a-form-item name="confirmPassword" label="确认密码">
          <a-input-password v-model:value="resetPasswordForm.confirmPassword" id="emp-confirm-password" />
        </a-form-item>
        <a-form-item>
          <a-button type="text" @click="generatePassword">生成随机密码</a-button>
          <a-button type="text" @click="sendPassword">发送密码</a-button>
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import dayjs from 'dayjs'
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { 
  PlusOutlined, 
  UploadOutlined, 
  DownloadOutlined, 
  UserDeleteOutlined, 
  SyncOutlined,
  SearchOutlined
} from '@ant-design/icons-vue'
import { employeeApi, orgUnitApi, supplierApi } from '../../api'
import { buildOptionLabelMap, loadSelectOptions } from '../../utils/selectOptions'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const data = ref([])
const modalVisible = ref(false)
const batchDismissModalVisible = ref(false)
const resetPasswordModalVisible = ref(false)
const editingRecord = ref(null)
const selectedRowKeys = ref([])
const selectedRows = ref([])
const currentStep = ref(0)
const orgUnits = ref([])
const orgTreeData = ref([])
const orgUnitOptions = ref([])

const pagination = reactive({
  pageSize: 20,
  current: 1,
  total: 0,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  showTotal: (total, range) => `共 ${total} 条记录`
})

const queryForm = reactive({
  keyword: '',
  employeeType: '',
  salaryMode: '',
  orgUnitId: undefined,
  level: '',
  isActive: '',
  hireDateRange: null,
  dismissDateRange: null
})

const form = reactive({
  employeeNo: '',
  name: '',
  gender: 0,
  idCard: '',
  phone: '',
  orgUnitId: undefined,
  thirdPartyCompanyId: undefined,
  employeeType: 0,
  salaryMode: 0,
  level: undefined,
  tags: [],
  hourlyRate: null,
  monthlySalary: null,
  socialSecurityBase: null,
  housingFundBase: null,
  contractType: 0,
  contractStartDate: null,
  contractEndDate: null,
  probationDays: 30,
  hireDate: null
})

const batchDismissForm = reactive({
  dismissDate: null,
  reason: ''
})

const resetPasswordForm = reactive({
  password: '',
  confirmPassword: ''
})

const jobLevelOptions = [
  { label: 'P1-初级', value: 'P1' },
  { label: 'P2-中级', value: 'P2' },
  { label: 'P3-高级', value: 'P3' },
  { label: 'P4-资深', value: 'P4' },
  { label: 'P5-专家', value: 'P5' },
  { label: 'M1-主管', value: 'M1' },
  { label: 'M2-经理', value: 'M2' },
  { label: 'M3-总监', value: 'M3' }
]

const suppliers = ref([])
const employeeTypeOptions = ref([])
const salaryModeOptions = ref([])
const contractTypeOptions = ref([])

const employeeTypeMap = computed(() => {
  const labelMap = buildOptionLabelMap(employeeTypeOptions.value)
  return {
    ...labelMap,
    Internal: labelMap['0'] || '自主员工',
    ThirdParty: labelMap['1'] || '第三方派遣'
  }
})

const salaryModeMap = computed(() => {
  const labelMap = buildOptionLabelMap(salaryModeOptions.value)
  return {
    ...labelMap,
    Hourly: labelMap['0'] || '时薪制',
    Fixed: labelMap['1'] || '固薪制',
    PieceRate: labelMap['2'] || '计件制',
    Mixed: labelMap['3'] || '混合制'
  }
})

const columns = [
  { title: '工号', dataIndex: 'employeeNo', key: 'employeeNo', width: 100 },
  { title: '姓名', dataIndex: 'name', key: 'name', width: 100 },
  { title: '手机', dataIndex: 'phone', key: 'phone', width: 120 },
  { title: '员工类型', dataIndex: 'employeeType', key: 'employeeType', width: 100 },
  { title: '供应商', dataIndex: 'thirdPartyCompanyName', key: 'thirdPartyCompanyName', width: 160 },
  { title: '所属组织', dataIndex: 'orgUnitName', key: 'orgUnitName', width: 150 },
  { title: '级别', dataIndex: 'level', key: 'level', width: 80 },
  { title: '员工标识', key: 'tags', width: 150 },
  { title: '状态', key: 'isActive', width: 80 },
  { title: '入职日期', dataIndex: 'hireDate', key: 'hireDate', width: 120 },
  { title: '操作', key: 'action', width: 250 }
]

const fullData = ref([])
const enumAliases = {
  employeeType: {
    Internal: 0,
    ThirdParty: 1
  },
  salaryMode: {
    Hourly: 0,
    Fixed: 1,
    PieceRate: 2,
    Mixed: 3
  }
}

const getNumericValue = (value, aliases = {}, fallback = 0) => {
  if (value === null || value === undefined || value === '') {
    return fallback
  }

  if (typeof value === 'number') {
    return value
  }

  if (Object.prototype.hasOwnProperty.call(aliases, value)) {
    return aliases[value]
  }

  const numericValue = Number(value)
  return Number.isNaN(numericValue) ? fallback : numericValue
}

const toPickerValue = (value) => (value ? dayjs(value) : null)
const toIsoValue = (value) => (value ? dayjs(value).toISOString() : null)
const formatDate = (value) => (value ? dayjs(value).format('YYYY-MM-DD') : '-')

const buildEmployeePayload = () => ({
  employeeNo: form.employeeNo?.trim(),
  name: form.name?.trim(),
  gender: getNumericValue(form.gender),
  idCard: form.idCard?.trim(),
  phone: form.phone?.trim(),
  email: editingRecord.value?.email || null,
  orgUnitId: form.orgUnitId,
  thirdPartyCompanyId: form.thirdPartyCompanyId || null,
  employeeType: getNumericValue(form.employeeType, enumAliases.employeeType),
  salaryMode: getNumericValue(form.salaryMode, enumAliases.salaryMode),
  jobTitle: editingRecord.value?.jobTitle || null,
  level: form.level || null,
  tags: [...(form.tags || [])],
  hourlyRate: form.hourlyRate,
  monthlySalary: form.monthlySalary,
  pieceRatePrice: editingRecord.value?.pieceRatePrice ?? null,
  socialSecurityBase: form.socialSecurityBase,
  housingFundBase: form.housingFundBase,
  contractType: getNumericValue(form.contractType),
  contractStartDate: toIsoValue(form.contractStartDate),
  contractEndDate: toIsoValue(form.contractEndDate),
  probationDays: getNumericValue(form.probationDays, {}, 30),
  hireDate: toIsoValue(form.hireDate)
})

const resetForm = () => {
  Object.assign(form, {
    employeeNo: '',
    name: '',
    gender: 0,
    idCard: '',
    phone: '',
    orgUnitId: undefined,
    thirdPartyCompanyId: undefined,
    employeeType: 0,
    salaryMode: 0,
    level: undefined,
    tags: [],
    hourlyRate: null,
    monthlySalary: null,
    socialSecurityBase: null,
    housingFundBase: null,
    contractType: 0,
    contractStartDate: null,
    contractEndDate: null,
    probationDays: 30,
    hireDate: null
  })
}

const loadConfigOptions = async () => {
  const [employeeTypes, salaryModes, contractTypes] = await Promise.all([
    loadSelectOptions('employeeType'),
    loadSelectOptions('salaryMode'),
    loadSelectOptions('contractType')
  ])

  employeeTypeOptions.value = employeeTypes
  salaryModeOptions.value = salaryModes
  contractTypeOptions.value = contractTypes
}

const loadData = async () => {
  loading.value = true
  try {
    const result = await employeeApi.getAll()
    fullData.value = result || []
    applyFilters()
  } catch (error) {
    fullData.value = []
    data.value = []
  } finally {
    loading.value = false
  }
}

// 应用过滤逻辑
const applyFilters = () => {
  let filtered = [...fullData.value]

  // 关键字过滤
  if (queryForm.keyword) {
    const lowerSearch = queryForm.keyword.toLowerCase()
    filtered = filtered.filter(item => 
      (item.name && item.name.toLowerCase().includes(lowerSearch)) || 
      (item.employeeNo && item.employeeNo.toLowerCase().includes(lowerSearch)) ||
      (item.idCard && item.idCard.toLowerCase().includes(lowerSearch)) ||
      (item.phone && item.phone.toLowerCase().includes(lowerSearch))
    )
  }

  // 组织过滤
  if (queryForm.orgUnitId) {
    filtered = filtered.filter(item => item.orgUnitId === queryForm.orgUnitId)
  }

  // 员工类型过滤
  if (queryForm.employeeType !== '' && queryForm.employeeType !== undefined) {
    filtered = filtered.filter(item => {
      const typeStr = String(item.employeeType)
      const targetType = String(queryForm.employeeType)
      return typeStr === targetType || 
             (targetType === '0' && typeStr === 'Internal') ||
             (targetType === '1' && typeStr === 'ThirdParty')
    })
  }

  // 状态过滤
  if (queryForm.isActive !== '' && queryForm.isActive !== undefined) {
    const targetStatus = queryForm.isActive === 'true'
    filtered = filtered.filter(item => {
      const itemStatus = item.isActive === true || item.isActive === 1
      return itemStatus === targetStatus
    })
  }

  data.value = filtered
  pagination.total = filtered.length
}

const loadOrgUnits = async () => {
  try {
    const result = await orgUnitApi.getAll()
    orgUnits.value = result || []
    buildOrgTree(result)
    buildOrgOptions(result)
  } catch (error) {
    orgUnits.value = []
  }
}

const loadSuppliers = async () => {
  try {
    suppliers.value = await supplierApi.getAll()
  } catch (error) {
    suppliers.value = []
  }
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

const buildOrgOptions = (list) => {
  const buildOptions = (items, parentCode = '') => {
    return items.map(item => {
      const code = parentCode ? `${parentCode}-${item.code}` : item.code
      const children = list.filter(i => i.parentId === item.id)
      return {
        value: item.id,
        label: item.name,
        code,
        children: children.length ? buildOptions(children, code) : undefined
      }
    })
  }
  orgUnitOptions.value = buildOptions(list.filter(i => !i.parentId))
}

const handleSearch = () => {
  pagination.current = 1
  applyFilters()
}

const handleReset = () => {
  Object.assign(queryForm, {
    keyword: '',
    employeeType: '',
    salaryMode: '',
    orgUnitId: undefined,
    level: '',
    isActive: '',
    hireDateRange: null,
    dismissDateRange: null
  })
  pagination.current = 1
  applyFilters()
}

const handleTableChange = (paginationInfo) => {
  Object.assign(pagination, paginationInfo)
}

const handleRowSelect = (keys, rows) => {
  selectedRowKeys.value = keys
  selectedRows.value = rows
}

const showAddModal = () => {
  editingRecord.value = null
  currentStep.value = 0
  resetForm()
  modalVisible.value = true
}

const handleEdit = (record) => {
  editingRecord.value = record
  currentStep.value = 0
  Object.assign(form, {
    employeeNo: record.employeeNo,
    name: record.name,
    gender: getNumericValue(record.gender),
    idCard: record.idCard,
    phone: record.phone,
    orgUnitId: record.orgUnitId,
    thirdPartyCompanyId: record.thirdPartyCompanyId,
    employeeType: getNumericValue(record.employeeType, enumAliases.employeeType),
    salaryMode: getNumericValue(record.salaryMode, enumAliases.salaryMode),
    level: record.level,
    tags: record.tags || [],
    hourlyRate: record.hourlyRate,
    monthlySalary: record.monthlySalary,
    socialSecurityBase: record.socialSecurityBase,
    housingFundBase: record.housingFundBase,
    contractType: getNumericValue(record.contractType),
    contractStartDate: toPickerValue(record.contractStartDate),
    contractEndDate: toPickerValue(record.contractEndDate),
    probationDays: getNumericValue(record.probationDays, {}, 30),
    hireDate: toPickerValue(record.hireDate)
  })
  modalVisible.value = true
}

const handleView = (record) => {
  router.push({
    path: `/employees/detail/${record.id}`,
    query: {
      from: route.fullPath
    }
  })
}

const handleSave = async () => {
  if (!form.orgUnitId) {
    message.error('请选择组织单元')
    return
  }
  try {
    const payload = buildEmployeePayload()
    if (editingRecord.value) {
      await employeeApi.update({ id: editingRecord.value.id, ...payload })
      message.success('更新成功')
    } else {
      await employeeApi.create(payload)
      message.success('创建成功')
    }
    modalVisible.value = false
    await loadData()
  } catch (error) {
    message.error('保存失败')
  }
}

const handleToggleDismiss = async (record) => {
  try {
    await employeeApi.toggleDismiss(record.id)
    message.success(record.isActive ? '已办理离职' : '已办理复职')
    loadData()
  } catch (error) {
    message.error('操作失败')
  }
}

const handleBatchImport = () => {
  message.info('批量导入功能开发中')
}

const handleBatchExport = () => {
  message.info('批量导出功能开发中')
}

const handleBatchDismiss = () => {
  batchDismissForm.dismissDate = null
  batchDismissForm.reason = ''
  batchDismissModalVisible.value = true
}

const handleBatchDismissConfirm = async () => {
  if (!batchDismissForm.dismissDate) {
    message.error('请选择离职日期')
    return
  }
  try {
    const ids = selectedRowKeys.value
    await employeeApi.batchDismiss({
      ids,
      dismissDate: toIsoValue(batchDismissForm.dismissDate),
      reason: batchDismissForm.reason
    })
    message.success('批量离职成功')
    batchDismissModalVisible.value = false
    selectedRowKeys.value = []
    selectedRows.value = []
    loadData()
  } catch (error) {
    message.error('批量离职失败')
  }
}

const handleResetPassword = (record) => {
  resetPasswordForm.password = ''
  resetPasswordForm.confirmPassword = ''
  resetPasswordModalVisible.value = true
}

const handleResetPasswordConfirm = async () => {
  if (resetPasswordForm.password !== resetPasswordForm.confirmPassword) {
    message.error('两次输入的密码不一致')
    return
  }
  try {
    message.success('密码重置成功')
    resetPasswordModalVisible.value = false
  } catch (error) {
    message.error('密码重置失败')
  }
}

const generatePassword = () => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
  let password = ''
  for (let i = 0; i < 8; i++) {
    password += chars.charAt(Math.floor(Math.random() * chars.length))
  }
  resetPasswordForm.password = password
  resetPasswordForm.confirmPassword = password
}

const sendPassword = () => {
  message.info('密码已发送')
}

onMounted(async () => {
  await loadConfigOptions()
  await loadData()
  await loadOrgUnits()
  await loadSuppliers()
})
</script>

<style scoped>
.employee-list {
  height: calc(100vh - 134px);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.toolbar {
  padding: 12px 16px;
  border-bottom: 1px solid #e8e8e8;
}

.advanced-query-panel {
  padding: 16px;
  background-color: #fafafa;
  border-bottom: 1px solid #e8e8e8;
}

.query-form :deep(.ant-form-item) {
  margin-bottom: 8px;
  margin-right: 24px;
}

.query-input {
  width: 200px;
}

.steps {
  margin-bottom: 24px;
  padding: 0 24px;
}

.step-content {
  min-height: 300px;
  padding: 0 24px;
}
</style>
