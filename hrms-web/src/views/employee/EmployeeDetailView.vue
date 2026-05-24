<template>
  <div class="employee-detail-page">
    <div class="page-header">
      <div class="header-main">
        <div class="header-icon">
          <UserOutlined />
        </div>
        <div class="header-text">
          <h2>员工详情</h2>
          <p>集中查看员工的基础信息、岗位信息、薪资合同信息。</p>
        </div>
      </div>
      <div class="header-actions">
        <a-button @click="handleBack">
          <ArrowLeftOutlined /> 返回
        </a-button>
        <a-button @click="goEmployeeList">
          员工列表
        </a-button>
        <a-button type="primary" :loading="loading" @click="loadEmployeeDetail">
          <ReloadOutlined /> 刷新
        </a-button>
      </div>
    </div>

    <a-spin :spinning="loading">
      <template v-if="employeeDetail">
        <div class="detail-hero">
          <div class="detail-hero-main">
            <div class="detail-avatar-wrapper">
              <UserOutlined class="detail-main-icon" />
            </div>
            <div class="detail-title-group">
              <div class="detail-title-row">
                <h1 class="detail-main-name">{{ employeeDetail.name || '-' }}</h1>
                <span class="detail-no">工号 {{ employeeDetail.employeeNo || '-' }}</span>
              </div>
              <div class="detail-sub-info">
                <a-tag color="blue">{{ employeeTypeText(employeeDetail.employeeType) }}</a-tag>
                <a-tag color="cyan">{{ salaryModeText(employeeDetail.salaryMode) }}</a-tag>
                <span class="org-name"><ApartmentOutlined /> {{ employeeDetail.orgUnitName || '未设置组织' }}</span>
              </div>
            </div>
          </div>
          <a-tag :color="employeeDetail.isActive ? 'green' : 'red'" class="detail-status-tag">
            {{ employeeDetail.isActive ? '在职中' : '已离职' }}
          </a-tag>
        </div>

        <div class="summary-grid">
          <div class="summary-card">
            <span class="summary-label">手机号</span>
            <strong><PhoneOutlined /> {{ employeeDetail.phone || '-' }}</strong>
          </div>
          <div class="summary-card">
            <span class="summary-label">身份证号</span>
            <strong><IdcardOutlined /> {{ employeeDetail.idCard || '-' }}</strong>
          </div>
          <div class="summary-card">
            <span class="summary-label">所属组织</span>
            <strong>{{ employeeDetail.orgUnitName || '-' }}</strong>
          </div>
          <div class="summary-card">
            <span class="summary-label">入职日期</span>
            <strong>{{ formatDate(employeeDetail.hireDate) }}</strong>
          </div>
        </div>

        <a-card :bordered="false" class="detail-card">
          <a-tabs default-active-key="basic">
            <a-tab-pane key="basic" tab="基本信息">
              <a-descriptions :column="2" bordered size="small">
                <a-descriptions-item label="姓名">{{ employeeDetail.name || '-' }}</a-descriptions-item>
                <a-descriptions-item label="工号">{{ employeeDetail.employeeNo || '-' }}</a-descriptions-item>
                <a-descriptions-item label="性别">{{ genderText(employeeDetail.gender) }}</a-descriptions-item>
                <a-descriptions-item label="手机号">{{ employeeDetail.phone || '-' }}</a-descriptions-item>
                <a-descriptions-item label="身份证号">{{ employeeDetail.idCard || '-' }}</a-descriptions-item>
                <a-descriptions-item label="电子邮箱">{{ employeeDetail.email || '-' }}</a-descriptions-item>
                <a-descriptions-item label="员工标识" :span="2">
                  <a-tag v-for="tag in employeeDetail.tags || []" :key="tag" color="blue">{{ tag }}</a-tag>
                  <span v-if="!employeeDetail.tags?.length">-</span>
                </a-descriptions-item>
              </a-descriptions>
            </a-tab-pane>

            <a-tab-pane key="position" tab="岗位信息">
              <a-descriptions :column="2" bordered size="small">
                <a-descriptions-item label="所属组织">{{ employeeDetail.orgUnitName || '-' }}</a-descriptions-item>
                <a-descriptions-item label="岗位级别">{{ employeeDetail.level || '-' }}</a-descriptions-item>
                <a-descriptions-item label="员工类型">{{ employeeTypeText(employeeDetail.employeeType) }}</a-descriptions-item>
                <a-descriptions-item label="职位名称">{{ employeeDetail.jobTitle || '-' }}</a-descriptions-item>
                <a-descriptions-item label="第三方公司">{{ employeeDetail.thirdPartyCompanyName || '-' }}</a-descriptions-item>
                <a-descriptions-item label="状态">{{ employeeDetail.isActive ? '在职' : '离职' }}</a-descriptions-item>
                <a-descriptions-item label="入职日期">{{ formatDate(employeeDetail.hireDate) }}</a-descriptions-item>
                <a-descriptions-item label="离职日期">{{ formatDate(employeeDetail.dismissDate) }}</a-descriptions-item>
              </a-descriptions>
            </a-tab-pane>

            <a-tab-pane key="salary" tab="薪资合同">
              <a-descriptions :column="2" bordered size="small">
                <a-descriptions-item label="薪资模式">{{ salaryModeText(employeeDetail.salaryMode) }}</a-descriptions-item>
                <a-descriptions-item label="时薪单价">¥ {{ employeeDetail.hourlyRate || 0 }}</a-descriptions-item>
                <a-descriptions-item label="固薪金额">¥ {{ employeeDetail.monthlySalary || 0 }}</a-descriptions-item>
                <a-descriptions-item label="计件单价">¥ {{ employeeDetail.pieceRatePrice || 0 }}</a-descriptions-item>
                <a-descriptions-item label="社保基数">{{ employeeDetail.socialSecurityBase || 0 }}</a-descriptions-item>
                <a-descriptions-item label="公积金基数">{{ employeeDetail.housingFundBase || 0 }}</a-descriptions-item>
                <a-descriptions-item label="合同类型">{{ contractTypeText(employeeDetail.contractType) }}</a-descriptions-item>
                <a-descriptions-item label="试用期天数">{{ employeeDetail.probationDays || 0 }}</a-descriptions-item>
                <a-descriptions-item label="合同开始">{{ formatDate(employeeDetail.contractStartDate) }}</a-descriptions-item>
                <a-descriptions-item label="合同结束">{{ formatDate(employeeDetail.contractEndDate) }}</a-descriptions-item>
              </a-descriptions>
            </a-tab-pane>
          </a-tabs>
        </a-card>
      </template>

      <a-empty v-else description="未找到员工信息" class="empty-state" />
    </a-spin>
  </div>
</template>

<script setup>
import dayjs from 'dayjs'
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import {
  ApartmentOutlined,
  ArrowLeftOutlined,
  IdcardOutlined,
  PhoneOutlined,
  ReloadOutlined,
  UserOutlined
} from '@ant-design/icons-vue'
import { employeeApi } from '../../api'
import { buildOptionLabelMap, loadSelectOptions } from '../../utils/selectOptions'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const employeeDetail = ref(null)
const employeeTypeOptions = ref([])
const salaryModeOptions = ref([])
const contractTypeOptions = ref([])

const employeeTypeMap = computed(() => ({
  ...buildOptionLabelMap(employeeTypeOptions.value),
  Internal: buildOptionLabelMap(employeeTypeOptions.value)['0'] || '自主员工',
  ThirdParty: buildOptionLabelMap(employeeTypeOptions.value)['1'] || '第三方派遣'
}))

const salaryModeMap = computed(() => ({
  ...buildOptionLabelMap(salaryModeOptions.value),
  Hourly: buildOptionLabelMap(salaryModeOptions.value)['0'] || '时薪制',
  Fixed: buildOptionLabelMap(salaryModeOptions.value)['1'] || '固薪制',
  PieceRate: buildOptionLabelMap(salaryModeOptions.value)['2'] || '计件制',
  Mixed: buildOptionLabelMap(salaryModeOptions.value)['3'] || '混合制'
}))

const contractTypeMap = computed(() => buildOptionLabelMap(contractTypeOptions.value))

const employeeId = computed(() => route.params.id)

const employeeTypeText = (value) => employeeTypeMap.value[value] || value || '-'

const salaryModeText = (value) => salaryModeMap.value[value] || value || '-'

const contractTypeText = (value) => contractTypeMap.value[value] || value || '-'
const formatDate = (value) => (value ? dayjs(value).format('YYYY-MM-DD') : '-')

const genderText = (value) => {
  if (value === 0 || value === '0') {
    return '男'
  }
  if (value === 1 || value === '1') {
    return '女'
  }
  return '-'
}

const resolveBackPath = () => {
  const from = typeof route.query.from === 'string' ? route.query.from : ''
  if (from && from !== route.fullPath) {
    return from
  }
  return '/employees'
}

const handleBack = () => {
  router.push(resolveBackPath())
}

const goEmployeeList = () => {
  router.push('/employees')
}

const loadOptions = async () => {
  const [employeeTypes, salaryModes, contractTypes] = await Promise.all([
    loadSelectOptions('employeeType'),
    loadSelectOptions('salaryMode'),
    loadSelectOptions('contractType')
  ])

  employeeTypeOptions.value = employeeTypes
  salaryModeOptions.value = salaryModes
  contractTypeOptions.value = contractTypes
}

const loadEmployeeDetail = async () => {
  if (!employeeId.value) {
    employeeDetail.value = null
    return
  }

  loading.value = true
  try {
    const detail = await employeeApi.getById(employeeId.value)
    employeeDetail.value = detail || null

    if (!detail) {
      message.warning('未找到员工信息')
    }
  } catch (error) {
    employeeDetail.value = null
    console.error('Failed to load employee detail:', error)
    message.error('加载员工详情失败')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadOptions()
  await loadEmployeeDetail()
})

watch(() => route.params.id, async (nextId, previousId) => {
  if (!nextId || nextId === previousId) {
    return
  }

  await loadEmployeeDetail()
})
</script>

<style scoped>
.employee-detail-page {
  min-height: calc(100vh - 32px);
  padding: 24px;
  background: #f5f7fa;
  border-radius: 24px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
  padding: 24px 28px;
  background: #ffffff;
  border-radius: 24px;
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.04);
}

.header-main {
  display: flex;
  align-items: center;
  gap: 18px;
}

.header-icon {
  width: 58px;
  height: 58px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 18px;
  background: #eff6ff;
  color: #1677ff;
  font-size: 28px;
  border: 1px solid rgba(191, 219, 254, 0.9);
}

.header-text h2 {
  margin: 0 0 8px;
  color: #0f172a;
  font-size: 26px;
}

.header-text p {
  margin: 0;
  color: #64748b;
  font-size: 14px;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.detail-hero {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 24px 28px;
  margin-bottom: 20px;
  border-radius: 24px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.04);
}

.detail-hero-main {
  display: flex;
  align-items: center;
  gap: 18px;
  min-width: 0;
  flex: 1;
}

.detail-avatar-wrapper {
  width: 72px;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4f8cff 0%, #7a5cff 100%);
  border-radius: 22px;
  box-shadow: 0 8px 20px rgba(79, 140, 255, 0.2);
}

.detail-main-icon {
  font-size: 34px;
  color: #ffffff;
}

.detail-title-group {
  flex: 1;
  min-width: 0;
}

.detail-title-row {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.detail-main-name {
  margin: 0;
  color: #0f172a;
  font-size: 30px;
  line-height: 1.2;
}

.detail-no {
  color: #64748b;
  font-size: 14px;
}

.detail-sub-info {
  margin-top: 10px;
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.org-name {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: #64748b;
  font-size: 14px;
}

.detail-status-tag {
  margin: 0;
  padding: 6px 14px;
  border-radius: 999px;
  font-size: 13px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 20px;
}

.summary-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 18px 20px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.16);
  border-radius: 18px;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.04);
}

.summary-label {
  color: #64748b;
  font-size: 13px;
}

.summary-card strong {
  color: #0f172a;
  font-size: 15px;
  line-height: 1.5;
  word-break: break-word;
}

.detail-card {
  border-radius: 24px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.16);
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.04);
}

.detail-card :deep(.ant-card-body) {
  padding: 20px 22px;
}

:deep(.ant-descriptions-item-label) {
  width: 120px;
  background-color: #fafafa !important;
  font-weight: 500;
}

.empty-state {
  padding: 80px 0;
  background: #ffffff;
  border-radius: 24px;
  border: 1px solid rgba(148, 163, 184, 0.16);
}

@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .employee-detail-page {
    padding: 16px;
  }

  .page-header,
  .detail-hero {
    flex-direction: column;
    align-items: stretch;
    padding: 20px;
  }

  .header-main,
  .detail-hero-main {
    align-items: flex-start;
  }

  .header-actions {
    width: 100%;
  }

  .header-actions :deep(.ant-btn) {
    flex: 1;
  }

  .detail-main-name {
    font-size: 24px;
  }

  .summary-grid {
    grid-template-columns: 1fr;
  }

  .detail-card :deep(.ant-descriptions) {
    overflow-x: auto;
  }
}
</style>
