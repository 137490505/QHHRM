<template>
  <div class="config-management">
    <div class="page-header">
      <div class="page-header-main">
        <div class="page-header-icon">
          <SettingOutlined class="title-icon" />
        </div>
        <div class="page-header-text">
          <div class="header-title">系统配置</div>
          <div class="header-desc">管理系统中的下拉选项、KPI指标和系统参数配置</div>
        </div>
      </div>
    </div>

    <a-card :bordered="false" class="config-card">
      <a-tabs v-model:activeKey="activeTab" class="config-tabs" size="large">
        <a-tab-pane key="options" tab="下拉选项">
          <div class="panel-content">
            <div class="panel-toolbar">
              <div class="toolbar-left">
                <a-select
                  v-model:value="selectedOptionCategory"
                  placeholder="选择分类"
                  class="category-select"
                  size="large"
                  @change="loadOptions"
                >
                  <a-select-option v-for="cat in optionCategories" :key="cat.value" :value="cat.value">
                    <span class="option-label">{{ cat.label }}</span>
                  </a-select-option>
                </a-select>
                <div class="category-tip">
                  <span class="category-name">{{ currentOptionCategoryLabel }}</span>
                  <span class="category-desc">{{ currentOptionCategoryDescription }}</span>
                </div>
              </div>
              <div class="toolbar-right">
                <a-button type="primary" size="large" @click="showAddOptionModal">
                  <PlusOutlined /> 新增选项
                </a-button>
              </div>
            </div>

            <div class="option-summary">
              <div class="summary-card">
                <span class="summary-label">当前分类</span>
                <strong>{{ currentOptionCategoryLabel }}</strong>
              </div>
              <div class="summary-card">
                <span class="summary-label">选项数量</span>
                <strong>{{ optionList.length }}</strong>
              </div>
              <div class="summary-card">
                <span class="summary-label">列表展示</span>
                <strong>仅显示业务标签</strong>
              </div>
            </div>

            <div class="table-wrapper">
              <a-table
                :key="`${selectedOptionCategory}-${optionTableVersion}`"
                class="option-table"
                :columns="optionColumns"
                :data-source="optionList"
                :loading="optionLoading"
                row-key="value"
                :pagination="{ pageSize: 10, showSizeChanger: true, showQuickJumper: true }"
              >
                <template #bodyCell="{ column, record }">
                  <template v-if="column.key === 'label'">
                    <div class="option-item">
                      <span class="option-text">{{ record.label }}</span>
                    </div>
                  </template>
                  <template v-if="column.key === 'sortOrder'">
                    <a-input-number
                      :value="record.sortOrder"
                      :min="1"
                      :precision="0"
                      :disabled="sortSavingValue === record.value"
                      size="small"
                      class="sort-order-input"
                      @change="value => updateOptionSortOrder(record, value)"
                    />
                  </template>
                  <template v-if="column.key === 'action'">
                    <a-space class="action-group" :size="4" wrap>
                      <a-button type="link" @click="editOption(record)">
                        <EditOutlined /> 编辑
                      </a-button>
                      <a-popconfirm
                        title="确定要删除此选项吗？"
                        ok-text="确定"
                        cancel-text="取消"
                        @confirm="deleteOption(record)"
                      >
                        <a-button type="link" danger>
                          <DeleteOutlined /> 删除
                        </a-button>
                      </a-popconfirm>
                    </a-space>
                  </template>
                </template>
              </a-table>
            </div>
          </div>
        </a-tab-pane>

        <a-tab-pane key="kpi" tab="KPI指标">
          <div class="panel-content">
            <div class="panel-toolbar">
              <div class="toolbar-left">
                <a-select
                  v-model:value="selectedKPICategory"
                  placeholder="选择分类"
                  class="category-select"
                  size="large"
                  @change="loadKPI"
                >
                  <a-select-option v-for="cat in kpiCategories" :key="cat.value" :value="cat.value">
                    <span class="option-label">{{ cat.label }}</span>
                  </a-select-option>
                </a-select>
              </div>
            </div>

            <div class="table-wrapper">
              <a-table
                :columns="kpiColumns"
                :data-source="kpiList"
                row-key="key"
                :pagination="{ pageSize: 10, showSizeChanger: true, showQuickJumper: true }"
                :scroll="{ x: 1000 }"
              >
                <template #bodyCell="{ column, record }">
                  <template v-if="column.key === 'target'">
                    <span class="target-value">{{ record.target }}{{ record.unit }}</span>
                  </template>
                  <template v-if="column.key === 'weight'">
                    <a-progress :percent="record.weight" :stroke-color="'#1890ff'" size="small" />
                  </template>
                  <template v-if="column.key === 'action'">
                    <a-button type="link" @click="editKPI(record)">
                      <EditOutlined /> 编辑
                    </a-button>
                  </template>
                </template>
              </a-table>
            </div>
          </div>
        </a-tab-pane>

        <a-tab-pane key="settings" tab="系统参数">
          <div class="panel-content">
            <div class="panel-toolbar">
              <div class="toolbar-left">
                <a-select
                  v-model:value="selectedSettingsCategory"
                  placeholder="选择分类"
                  class="category-select"
                  size="large"
                  @change="loadSettings"
                >
                  <a-select-option v-for="cat in settingsCategories" :key="cat.value" :value="cat.value">
                    <span class="option-label">{{ cat.label }}</span>
                  </a-select-option>
                </a-select>
              </div>
              <div class="toolbar-right">
                <a-button type="primary" size="large" :loading="settingsSaving" @click="saveSettings">
                  <SaveOutlined /> 保存设置
                </a-button>
              </div>
            </div>

            <div class="settings-grid">
              <a-form :model="settingsForm" layout="vertical" class="settings-form">
                <a-row :gutter="[24, 16]">
                  <a-col :span="12" v-for="(value, key) in currentSettings" :key="key">
                    <a-form-item :name="key" :label="getSettingLabel(key)">
                      <a-switch
                        v-if="typeof value === 'boolean'"
                        v-model:checked="settingsForm[key]"
                        checked-children="开启"
                        un-checked-children="关闭"
                      />
                      <a-input-number
                        v-else
                        v-model:value="settingsForm[key]"
                        :min="0"
                        :step="typeof value === 'number' && value < 1 ? 0.01 : 1"
                        :precision="typeof value === 'number' && value < 1 ? 2 : 0"
                        class="setting-input"
                      />
                    </a-form-item>
                  </a-col>
                </a-row>
              </a-form>
            </div>
          </div>
        </a-tab-pane>
      </a-tabs>
    </a-card>

    <a-modal
      v-model:open="optionModalVisible"
      :title="editingOption ? '编辑选项' : '新增选项'"
      width="500px"
      @ok="saveOption"
      :destroyOnClose="true"
    >
      <a-form :model="optionForm" layout="vertical" class="modal-form">
        <a-form-item v-if="!editingOption" label="值生成规则">
          <a-alert
            type="info"
            show-icon
            :message="currentOptionAutoValueTip"
          />
        </a-form-item>
        <a-form-item name="label" label="标签" :rules="[{ required: true, message: '请输入标签' }]">
          <a-input v-model:value="optionForm.label" placeholder="请输入中文标签" />
        </a-form-item>
        <a-form-item name="sortOrder" label="排序">
          <a-input-number
            v-model:value="optionForm.sortOrder"
            :min="1"
            :precision="0"
            class="full-width"
            placeholder="请输入排序"
          />
        </a-form-item>
        <a-form-item name="labelEn" label="英文标签（可选）">
          <a-input v-model:value="optionForm.labelEn" placeholder="请输入英文标签" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="kpiModalVisible"
      title="编辑KPI指标"
      width="600px"
      @ok="saveKPI"
      :destroyOnClose="true"
    >
      <a-form :model="kpiForm" layout="vertical" class="modal-form">
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="name" label="指标名称" :rules="[{ required: true, message: '请输入指标名称' }]">
              <a-input v-model:value="kpiForm.name" placeholder="如：考勤率" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="unit" label="单位">
              <a-input v-model:value="kpiForm.unit" placeholder="如：%、分、天" />
            </a-form-item>
          </a-col>
        </a-row>
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item name="target" label="目标值" :rules="[{ required: true, message: '请输入目标值' }]">
              <a-input-number v-model:value="kpiForm.target" :step="0.1" class="full-width" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item name="weight" label="权重(%)" :rules="[{ required: true, message: '请输入权重' }]">
              <a-input-number v-model:value="kpiForm.weight" :min="0" :max="100" class="full-width" />
            </a-form-item>
          </a-col>
        </a-row>
        <a-form-item name="formula" label="计算公式">
          <a-textarea v-model:value="kpiForm.formula" :rows="2" placeholder="如：实际出勤天数 / 应出勤天数 * 100%" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { SettingOutlined, PlusOutlined, EditOutlined, DeleteOutlined, SaveOutlined } from '@ant-design/icons-vue'
import { configTable } from '../../config/configTable'
import { settingsApi } from '../../api'
import { getSelectOptionValueType, loadSelectOptions, normalizeSelectOptions } from '../../utils/selectOptions'

const activeTab = ref('options')
const AUTO_VALUE_RULES = {
  orgLevel: {
    getTip: () => '系统自动按当前最大数值 + 1 生成组织级别值'
  },
  employeeType: {
    getTip: () => '系统自动按当前最大数值 + 1 生成员工类型值'
  },
  salaryMode: {
    getTip: () => '系统自动按当前最大数值 + 1 生成薪资模式值'
  },
  contractType: {
    getTip: () => '系统自动按当前最大数值 + 1 生成合同类型值'
  },
  employeeTag: {
    getTip: () => '系统自动使用标签内容作为员工标签值'
  }
}

const optionCategories = ref([
  { value: 'orgLevel', label: '组织级别' },
  { value: 'employeeType', label: '员工类型' },
  { value: 'salaryMode', label: '薪资模式' },
  { value: 'contractType', label: '合同类型' },
  { value: 'employeeTag', label: '员工标签' }
])
const selectedOptionCategory = ref('orgLevel')
const optionList = ref([])
const optionLoading = ref(false)
const optionTableVersion = ref(0)
const sortSavingValue = ref(null)
const optionModalVisible = ref(false)
const editingOption = ref(null)
const optionForm = reactive({
  value: 0,
  label: '',
  labelEn: '',
  sortOrder: 1
})

const kpiCategories = ref([
  { value: 'attendance', label: '考勤KPI' },
  { value: 'performance', label: '绩效KPI' },
  { value: 'sales', label: '销售KPI' },
  { value: 'production', label: '生产KPI' },
  { value: 'admin', label: '行政KPI' }
])
const selectedKPICategory = ref('attendance')
const kpiList = ref([])
const kpiModalVisible = ref(false)
const editingKPI = ref(null)
const kpiForm = reactive({
  name: '',
  nameEn: '',
  unit: '',
  target: 0,
  weight: 0,
  formula: ''
})

const settingsCategories = ref([
  { value: 'attendance', label: '考勤设置' },
  { value: 'salary', label: '薪资设置' },
  { value: 'performance', label: '绩效设置' },
  { value: 'security', label: '安全设置' }
])
const selectedSettingsCategory = ref('attendance')
const settingsData = ref({})
const settingsForm = reactive({})
const settingsSaving = ref(false)

const optionColumns = [
  { title: '标签', dataIndex: 'label', key: 'label' },
  { title: '排序', dataIndex: 'sortOrder', key: 'sortOrder', width: 110 },
  { title: '操作', dataIndex: 'action', key: 'action', width: 160 }
]

const kpiColumns = [
  { title: '指标名称', dataIndex: 'name', key: 'name', width: 150 },
  { title: '单位', dataIndex: 'unit', key: 'unit', width: 100 },
  { title: '目标值', dataIndex: 'target', key: 'target', width: 120 },
  { title: '权重', dataIndex: 'weight', key: 'weight', width: 150 },
  { title: '计算公式', dataIndex: 'formula', key: 'formula' },
  { title: '操作', dataIndex: 'action', key: 'action', width: 100, fixed: 'right' }
]

const currentSettings = computed(() => {
  return settingsData.value[selectedSettingsCategory.value] || {}
})

const currentOptionValueType = computed(() => getSelectOptionValueType(selectedOptionCategory.value))
const currentOptionCategory = computed(() => {
  return optionCategories.value.find(item => item.value === selectedOptionCategory.value) || optionCategories.value[0]
})
const currentOptionCategoryLabel = computed(() => currentOptionCategory.value?.label || '下拉选项')
const optionCategoryDescriptions = {
  orgLevel: '用于组织单元、组织架构管理等页面的类型下拉',
  employeeType: '用于员工、薪资等页面的员工类型下拉',
  salaryMode: '用于薪资核算和员工薪资模式下拉',
  contractType: '用于员工合同信息中的合同类型下拉',
  employeeTag: '用于员工标识、员工标签相关下拉'
}
const currentOptionCategoryDescription = computed(() => {
  return optionCategoryDescriptions[selectedOptionCategory.value] || '统一维护业务下拉选项'
})
const currentOptionAutoValueTip = computed(() => {
  return AUTO_VALUE_RULES[selectedOptionCategory.value]?.getTip?.() || '系统会自动生成该选项值'
})

const settingLabels = {
  attendance: {
    normalDailyHours: '正常工作时长(小时)',
    lateThreshold: '迟到阈值(分钟)',
    earlyLeaveThreshold: '早退阈值(分钟)',
    absentThreshold: '旷工判定(小时)',
    maxOvertimeHoursPerMonth: '月度最大加班时长(小时)',
    annualLeaveDays: '年假标准天数'
  },
  salary: {
    socialSecurityBaseMin: '社保基数下限',
    socialSecurityBaseMax: '社保基数上限',
    housingFundBaseMin: '公积金基数下限',
    housingFundBaseMax: '公积金基数上限',
    taxThreshold: '个税起征点',
    socialSecurityPersonalRate: '社保个人比例',
    socialSecurityCompanyRate: '社保公司比例',
    housingFundPersonalRate: '公积金个人比例',
    housingFundCompanyRate: '公积金公司比例',
    hourlyMinWage: '小时工最低工资',
    weekdayOvertimeMultiplier: '工作日加班费倍数',
    weekendOvertimeMultiplier: '周末加班费倍数',
    holidayOvertimeMultiplier: '节假日加班费倍数'
  },
  performance: {
    evaluationCycleMonths: '评估周期(月)',
    excellentThreshold: '优秀阈值',
    goodThreshold: '良好阈值',
    qualifiedThreshold: '合格阈值',
    excellentRatioLimit: '优秀比例上限',
    unqualifiedRatioLimit: '不合格比例上限',
    selfEvaluationWeight: '自评权重',
    supervisorEvaluationWeight: '上级评分权重',
    peerEvaluationWeight: '同事评分权重'
  },
  security: {
    captchaEnabled: '启用登录验证码'
  }
}

const getSettingLabel = (key) => {
  const labels = settingLabels[selectedSettingsCategory.value] || {}
  return labels[key] || key
}

const syncSelectOptionsCache = (category, options) => {
  configTable.selectOptions[category] = options.map(option => ({ ...option }))
}

const getInitialOptionValue = () => {
  return currentOptionValueType.value === 'number' ? 0 : ''
}

const normalizeOptionValue = (value) => {
  if (currentOptionValueType.value === 'number') {
    const parsedValue = Number(value)
    return Number.isNaN(parsedValue) ? null : parsedValue
  }

  const normalizedValue = `${value ?? ''}`.trim()
  return normalizedValue || null
}

const generateNextNumericOptionValue = (options) => {
  const numericValues = options
    .map(option => Number(option.value))
    .filter(value => !Number.isNaN(value))

  return numericValues.length > 0 ? Math.max(...numericValues) + 1 : 0
}

const generateOptionValue = (label) => {
  if (currentOptionValueType.value === 'number') {
    return generateNextNumericOptionValue(optionList.value)
  }

  return label
}

const generateNextSortOrder = (options) => {
  const sortOrders = options
    .map(option => Number(option.sortOrder))
    .filter(value => Number.isFinite(value) && value > 0)

  return sortOrders.length > 0 ? Math.max(...sortOrders) + 1 : 1
}

const normalizeSortOrderValue = (value, fallback = 1) => {
  const parsedValue = Number(value)
  if (Number.isFinite(parsedValue) && parsedValue > 0) {
    return Math.trunc(parsedValue)
  }

  return fallback
}

const loadOptions = async () => {
  optionLoading.value = true
  try {
    const options = normalizeSelectOptions(
      selectedOptionCategory.value,
      await loadSelectOptions(selectedOptionCategory.value)
    )

    optionList.value = options
    syncSelectOptionsCache(selectedOptionCategory.value, options)
    optionTableVersion.value += 1
  } finally {
    optionLoading.value = false
  }
}

const loadKPI = () => {
  const kpiData = configTable.kpi[selectedKPICategory.value] || {}
  kpiList.value = Object.entries(kpiData).map(([key, value]) => ({
    key,
    ...value
  }))
}

const resetSettingsForm = () => {
  Object.keys(settingsForm).forEach(key => {
    delete settingsForm[key]
  })
}

const loadSettings = async () => {
  resetSettingsForm()

  if (selectedSettingsCategory.value === 'security') {
    const data = await settingsApi.getLoginSecuritySettings()
    settingsData.value = {
      ...configTable.settings,
      security: {
        captchaEnabled: !!data.captchaEnabled
      }
    }
  } else {
    settingsData.value = { ...configTable.settings }
  }

  Object.keys(settingsData.value[selectedSettingsCategory.value] || {}).forEach(key => {
    settingsForm[key] = settingsData.value[selectedSettingsCategory.value][key]
  })
}

const showAddOptionModal = () => {
  editingOption.value = null
  optionForm.value = getInitialOptionValue()
  optionForm.label = ''
  optionForm.labelEn = ''
  optionForm.sortOrder = generateNextSortOrder(optionList.value)
  optionModalVisible.value = true
}

const editOption = (record) => {
  const normalizedValue = normalizeOptionValue(record?.value)
  if (normalizedValue === null) {
    message.error('当前选项值为空，请刷新页面后重试')
    return
  }

  editingOption.value = { ...record, value: normalizedValue }
  optionForm.value = normalizedValue
  optionForm.label = record.label
  optionForm.labelEn = record.labelEn || ''
  optionForm.sortOrder = Number(record.sortOrder) > 0 ? Number(record.sortOrder) : 1
  optionModalVisible.value = true
}

const saveOption = async () => {
  const label = optionForm.label.trim()
  const labelEn = optionForm.labelEn.trim()

  if (!label) {
    message.error('请输入标签')
    return
  }

  const normalizedValue = editingOption.value
    ? normalizeOptionValue(editingOption.value.value)
    : normalizeOptionValue(generateOptionValue(label))

  if (normalizedValue === null) {
    message.error('系统未能生成有效的选项值，请检查当前分类配置')
    return
  }

  const nextOption = {
    value: normalizedValue,
    label,
    labelEn: labelEn || null,
    sortOrder: normalizeSortOrderValue(
      optionForm.sortOrder,
      editingOption.value
        ? Number(editingOption.value.sortOrder) || 1
        : generateNextSortOrder(optionList.value)
    )
  }

  const options = optionList.value.map(option => ({ ...option }))
  const duplicateIndex = options.findIndex(option => option.value === normalizedValue)
  if (
    duplicateIndex !== -1 &&
    (!editingOption.value || options[duplicateIndex].value !== editingOption.value.value)
  ) {
    message.error('选项值不能重复')
    return
  }

  if (editingOption.value) {
    const index = options.findIndex(option => option.value === editingOption.value.value)
    if (index === -1) {
      message.error('当前编辑项不存在，请刷新后重试')
      return
    }
    options[index] = nextOption
  } else {
    options.push(nextOption)
  }

  await settingsApi.updateSelectOptions(selectedOptionCategory.value, { options })
  await loadOptions()

  optionModalVisible.value = false
  message.success('下拉选项已保存')
}

const deleteOption = async (record) => {
  const options = optionList.value
    .filter(option => option.value !== record.value)
    .map(option => ({ ...option }))

  if (options.length === optionList.value.length) {
    message.error('未找到要删除的选项，请刷新后重试')
    return
  }

  await settingsApi.updateSelectOptions(selectedOptionCategory.value, { options })
  await loadOptions()

  message.success('下拉选项已删除')
}

const updateOptionSortOrder = async (record, value) => {
  const nextSortOrder = Number(value)
  if (!Number.isFinite(nextSortOrder) || nextSortOrder <= 0 || nextSortOrder === Number(record.sortOrder)) {
    return
  }

  const currentSortOrder = Number(record.sortOrder) || 1
  const options = optionList.value.map(option => ({
    ...option,
    sortOrder: option.value === record.value ? nextSortOrder : option.sortOrder
  }))

  sortSavingValue.value = record.value
  try {
    await settingsApi.updateSelectOptions(selectedOptionCategory.value, { options })
    await loadOptions()
    message.success('排序已更新')
  } catch (error) {
    record.sortOrder = currentSortOrder
    throw error
  } finally {
    sortSavingValue.value = null
  }
}

const editKPI = (record) => {
  editingKPI.value = record
  kpiForm.name = record.name
  kpiForm.nameEn = record.nameEn
  kpiForm.unit = record.unit
  kpiForm.target = record.target
  kpiForm.weight = record.weight
  kpiForm.formula = record.formula
  kpiModalVisible.value = true
}

const saveKPI = () => {
  if (editingKPI.value) {
    const kpiData = configTable.kpi[selectedKPICategory.value]
    kpiData[editingKPI.value.key] = { ...kpiForm }
  }
  kpiModalVisible.value = false
  loadKPI()
}

const saveSettings = async () => {
  settingsSaving.value = true
  try {
    if (selectedSettingsCategory.value === 'security') {
      const data = await settingsApi.updateLoginSecuritySettings({
        captchaEnabled: !!settingsForm.captchaEnabled
      })
      settingsData.value.security = {
        captchaEnabled: !!data.captchaEnabled
      }
      settingsForm.captchaEnabled = !!data.captchaEnabled
      message.success('安全设置已保存并立即生效')
      return
    }

    const settings = configTable.settings[selectedSettingsCategory.value]
    Object.keys(settings).forEach(key => {
      if (settingsForm[key] !== undefined) {
        settings[key] = settingsForm[key]
      }
    })
    message.success('系统参数已保存')
  } finally {
    settingsSaving.value = false
  }
}

onMounted(async () => {
  await loadOptions()
  loadKPI()
  await loadSettings()
})

selectedOptionCategory.value = 'orgLevel'
selectedKPICategory.value = 'attendance'
selectedSettingsCategory.value = 'attendance'
</script>

<style scoped>
.config-management {
  padding: 24px;
  background: #f0f2f5;
  min-height: 100vh;
}

.page-header {
  margin-bottom: 24px;
}

.page-header-main {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 18px 22px;
  background: #fff;
  border: 1px solid #f0f0f0;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.page-header-icon {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  background: linear-gradient(135deg, #e6f4ff 0%, #f0f8ff 100%);
  color: #1677ff;
  flex-shrink: 0;
}

.page-header-text {
  min-width: 0;
}

.header-title {
  color: #1f1f1f;
  font-size: 22px;
  font-weight: 600;
  line-height: 1.4;
}

.title-icon {
  font-size: 22px;
}

.header-desc {
  margin-top: 4px;
  color: #8c8c8c;
  font-size: 14px;
  line-height: 1.6;
}

.config-card {
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.09);
}

.panel-content {
  padding: 8px 0;
}

.panel-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding: 16px 20px;
  background: #fafafa;
  border-radius: 8px;
  border: 1px solid #f0f0f0;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.category-select {
  width: 200px;
}

.option-label {
  font-weight: 500;
}

.category-tip {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 2px;
}

.category-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f1f1f;
}

.category-desc {
  font-size: 12px;
  color: #8c8c8c;
}

.option-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.summary-card {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 16px 18px;
  background: linear-gradient(180deg, #ffffff 0%, #fafcff 100%);
  border: 1px solid #e6f4ff;
  border-radius: 10px;
}

.summary-label {
  font-size: 12px;
  color: #8c8c8c;
}

.table-wrapper {
  background: #fff;
  border-radius: 8px;
  border: 1px solid #f0f0f0;
}

.action-group {
  row-gap: 0;
}

.action-group :deep(.ant-btn) {
  padding-inline: 4px;
}

.option-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.option-text {
  font-weight: 500;
  color: #262626;
}

.option-value {
  font-family: Consolas, 'Courier New', monospace;
  color: #595959;
}

.target-value {
  font-weight: 600;
  color: #1890ff;
}

.settings-grid {
  padding: 24px;
  background: #fff;
  border-radius: 8px;
}

.settings-form {
  max-width: 100%;
}

.setting-input {
  width: 100%;
}

.modal-form {
  padding: 8px 0;
}

.full-width {
  width: 100%;
}

:deep(.ant-tabs-nav) {
  padding: 0 24px;
  margin-bottom: 0;
}

:deep(.ant-tabs-tab) {
  font-size: 15px;
  padding: 12px 0;
}

:deep(.ant-tabs-tab-active .ant-tabs-tab-btn) {
  font-weight: 600;
}

:deep(.ant-table-thead > tr > th) {
  background: #fafafa;
  font-weight: 600;
}

:deep(.ant-table-tbody > tr:hover > td) {
  background: #e6f7ff;
}

.option-table :deep(.ant-table-content) {
  overflow-x: hidden !important;
}

.option-table :deep(table) {
  width: 100% !important;
  table-layout: fixed;
}

.option-table :deep(.ant-table-cell) {
  word-break: break-word;
}

:deep(.ant-form-item-label > label) {
  font-weight: 500;
  color: #333;
}

@media (max-width: 768px) {
  .panel-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }

  .toolbar-left,
  .toolbar-right {
    width: 100%;
  }

  .category-select {
    width: 100%;
  }

  .option-summary {
    grid-template-columns: 1fr;
  }
}
</style>
