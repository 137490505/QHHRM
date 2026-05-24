<template>
  <div class="process-request-form-page">
    <a-page-header @back="goBack" title="提交申请">
      <template #breadcrumb>
        <a-breadcrumb>
          <a-breadcrumb-item>
            <router-link to="/workflow/requests">流程申请</router-link>
          </a-breadcrumb-item>
          <a-breadcrumb-item>
            {{ formData.processName }}
          </a-breadcrumb-item>
        </a-breadcrumb>
      </template>
    </a-page-header>

    <a-card class="form-card">
      <a-spin :spinning="loading">
        <a-form
          ref="formRef"
          :model="formData"
          :label-col="{ span: 4 }"
          :wrapper-col="{ span: 16 }"
        >
          <a-form-item label="流程名称">
            <a-input v-model:value="formData.processName" disabled />
          </a-form-item>

          <a-form-item label="申请标题">
            <a-input :value="formData.title" disabled />
          </a-form-item>

          <template v-if="definition?.businessType === 'Leave'">
            <a-form-item
              label="请假类型"
              name="leaveType"
              :rules="[{ required: true, message: '请选择请假类型' }]"
            >
              <a-select v-model:value="formData.leaveType" placeholder="请选择请假类型">
                <a-select-option value="Annual">年假</a-select-option>
                <a-select-option value="Sick">病假</a-select-option>
                <a-select-option value="Personal">事假</a-select-option>
                <a-select-option value="Marriage">婚假</a-select-option>
                <a-select-option value="Maternity">产假</a-select-option>
              </a-select>
            </a-form-item>

            <a-form-item
              label="开始日期"
              name="startDate"
              :rules="[{ required: true, message: '请选择开始日期' }]"
            >
              <a-date-picker v-model:value="formData.startDate" style="width: 100%" />
            </a-form-item>

            <a-form-item
              label="结束日期"
              name="endDate"
              :rules="[{ required: true, message: '请选择结束日期' }]"
            >
              <a-date-picker v-model:value="formData.endDate" style="width: 100%" />
            </a-form-item>

            <a-form-item
              label="请假天数"
              name="days"
              :rules="[{ required: true, message: '请输入请假天数' }]"
            >
              <a-input-number v-model:value="formData.days" :min="0.5" :step="0.5" style="width: 100%" />
            </a-form-item>

            <a-form-item label="请假原因" name="reason">
              <a-textarea v-model:value="formData.reason" :rows="4" placeholder="请输入请假原因" />
            </a-form-item>
          </template>

          <template v-else-if="definition?.businessType === 'Expense'">
            <a-form-item
              label="费用类型"
              name="expenseType"
              :rules="[{ required: true, message: '请选择费用类型' }]"
            >
              <a-select v-model:value="formData.expenseType" placeholder="请选择费用类型">
                <a-select-option value="Travel">差旅费</a-select-option>
                <a-select-option value="Entertainment">招待费</a-select-option>
                <a-select-option value="Office">办公费</a-select-option>
                <a-select-option value="Training">培训费</a-select-option>
                <a-select-option value="Other">其他费用</a-select-option>
              </a-select>
            </a-form-item>

            <a-form-item
              label="费用金额"
              name="amount"
              :rules="[{ required: true, message: '请输入费用金额' }]"
            >
              <a-input-number
                v-model:value="formData.amount"
                :min="0"
                :precision="2"
                style="width: 100%"
                placeholder="请输入费用金额"
              />
            </a-form-item>

            <a-form-item label="费用说明" name="reason">
              <a-textarea v-model:value="formData.reason" :rows="4" placeholder="请输入费用说明" />
            </a-form-item>
          </template>

          <template v-else-if="definition?.businessType === 'Reimbursement'">
            <a-form-item
              label="报销类型"
              name="reimburseType"
              :rules="[{ required: true, message: '请选择报销类型' }]"
            >
              <a-select v-model:value="formData.reimburseType" placeholder="请选择报销类型">
                <a-select-option value="Travel">差旅报销</a-select-option>
                <a-select-option value="Medical">医疗报销</a-select-option>
                <a-select-option value="Education">教育培训报销</a-select-option>
                <a-select-option value="Other">其他报销</a-select-option>
              </a-select>
            </a-form-item>

            <a-form-item
              label="报销金额"
              name="amount"
              :rules="[{ required: true, message: '请输入报销金额' }]"
            >
              <a-input-number
                v-model:value="formData.amount"
                :min="0"
                :precision="2"
                style="width: 100%"
                placeholder="请输入报销金额"
              />
            </a-form-item>

            <a-form-item label="报销说明" name="reason">
              <a-textarea v-model:value="formData.reason" :rows="4" placeholder="请输入报销说明" />
            </a-form-item>
          </template>

          <template v-else>
            <a-form-item label="申请说明" name="reason">
              <a-textarea v-model:value="formData.reason" :rows="4" placeholder="请输入申请说明" />
            </a-form-item>
          </template>

          <a-form-item label="备注" name="remark">
            <a-textarea v-model:value="formData.remark" :rows="3" placeholder="可选备注" />
          </a-form-item>

          <a-form-item :wrapper-col="{ offset: 4, span: 16 }">
            <a-space>
              <a-button type="primary" :loading="submitting" @click="submitForm">
                提交申请
              </a-button>
              <a-button @click="resetForm">重置</a-button>
              <a-button @click="goBack">取消</a-button>
            </a-space>
          </a-form-item>
        </a-form>
      </a-spin>
    </a-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { message } from 'ant-design-vue'
import { processCenterApi } from '../../api'
import { useUserStore } from '../../store/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const formRef = ref(null)
const loading = ref(false)
const submitting = ref(false)
const definition = ref(null)

const leaveTypeMap = {
  'Annual': '年假',
  'Sick': '病假',
  'Personal': '事假',
  'Marriage': '婚假',
  'Maternity': '产假'
}

const expenseTypeMap = {
  'Travel': '差旅费',
  'Entertainment': '招待费',
  'Office': '办公费',
  'Training': '培训费',
  'Other': '其他费用'
}

const reimburseTypeMap = {
  'Travel': '差旅报销',
  'Medical': '医疗报销',
  'Education': '教育培训报销',
  'Other': '其他报销'
}

const formData = reactive({
  processCode: '',
  processName: '',
  title: '',
  businessType: '',
  amount: null,
  reason: '',
  remark: '',
  leaveType: '',
  startDate: null,
  endDate: null,
  days: null,
  expenseType: '',
  reimburseType: ''
})

const generateTitle = () => {
  const userName = userStore.userInfo?.name || '员工'
  const businessType = definition.value?.businessType

  if (businessType === 'Leave' && formData.leaveType) {
    formData.title = `${userName}申请${leaveTypeMap[formData.leaveType] || formData.leaveType}`
  } else if (businessType === 'Expense' && formData.expenseType) {
    formData.title = `${userName}申请${expenseTypeMap[formData.expenseType] || formData.expenseType}`
  } else if (businessType === 'Reimbursement' && formData.reimburseType) {
    formData.title = `${userName}申请${reimburseTypeMap[formData.reimburseType] || formData.reimburseType}`
  } else {
    formData.title = `${userName}的申请`
  }
}

watch(() => formData.leaveType, generateTitle)
watch(() => formData.expenseType, generateTitle)
watch(() => formData.reimburseType, generateTitle)

const loadDefinition = async () => {
  loading.value = true
  try {
    const processCode = route.params.processCode
    const data = await processCenterApi.getDefinitions()
    const found = data.find(d => d.processCode === processCode && d.status === 'Published')
    if (!found) {
      message.error('流程不存在或未发布')
      router.back()
      return
    }
    definition.value = found
    formData.processCode = found.processCode
    formData.processName = found.processName
    formData.businessType = found.businessType
  } catch (error) {
    message.error('加载流程定义失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

const submitForm = async () => {
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const extData = {
      ...formData,
      amount: formData.amount,
      userId: userStore.userInfo?.id,
      userName: userStore.userInfo?.name
    }

    const result = await processCenterApi.startProcess({
      processCode: formData.processCode,
      businessSystem: 'HRMS',
      businessType: formData.businessType,
      businessId: Date.now().toString(),
      title: formData.title,
      starterId: userStore.userInfo?.id.toString(),
      starterName: userStore.userInfo?.name,
      requestId: `request-${Date.now()}`,
      extData: JSON.stringify(extData)
    })

    message.success('申请提交成功')
    router.push('/workflow/processes')
  } catch (error) {
    message.error('提交申请失败')
    console.error(error)
  } finally {
    submitting.value = false
  }
}

const resetForm = () => {
  formRef.value?.resetFields()
}

const goBack = () => {
  router.back()
}

onMounted(() => {
  loadDefinition()
})
</script>

<style scoped>
.process-request-form-page {
  padding: 16px;
}

.form-card {
  margin-top: 16px;
}
</style>
