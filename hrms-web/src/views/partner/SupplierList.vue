<template>
  <div class="partner-page">
    <div class="page-header">
      <div>
        <div class="page-title">供应商列表</div>
        <div class="page-desc">维护供应商档案，并与第三方员工归属关系联动，为后续对账、开票和付款做准备。</div>
      </div>
      <a-space>
        <a-button @click="loadData">
          <ReloadOutlined />
          刷新
        </a-button>
        <a-button v-permission="'button.partner.supplier.create'" type="primary" @click="openCreate">
          <PlusOutlined />
          新增供应商
        </a-button>
      </a-space>
    </div>

    <a-card :bordered="false" class="page-card">
      <div class="toolbar">
        <a-input
          v-model:value="keyword"
          allow-clear
          class="keyword-input"
          placeholder="搜索供应商编码/名称/简称/税号/邮箱/联系人/电话"
        />
      </div>

      <a-table
        :columns="columns"
        :data-source="filteredData"
        :loading="loading"
        row-key="id"
        :pagination="{ pageSize: 10, showSizeChanger: true }"
        :scroll="{ x: 1780 }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'green' : 'red'">
              {{ record.isActive ? '启用' : '停用' }}
            </a-tag>
          </template>
          <template v-if="column.key === 'employeeCount'">
            <a-tag color="blue">{{ record.employeeCount || 0 }} 人</a-tag>
          </template>
          <template v-else-if="['email', 'address', 'bankName', 'bankAccount'].includes(column.key)">
            {{ record[column.key] || '-' }}
          </template>
          <template v-if="column.key === 'action'">
            <a-space>
              <a-button v-permission="'button.partner.supplier.edit'" type="link" @click="openEdit(record)">编辑</a-button>
              <a-popconfirm
                title="确定删除该供应商吗？"
                ok-text="确定"
                cancel-text="取消"
                @confirm="handleDelete(record)"
              >
                <a-button v-permission="'button.partner.supplier.delete'" type="link" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      :title="editingRecord ? '编辑供应商' : '新增供应商'"
      :width="760"
      @ok="handleSave"
    >
      <a-form :model="form" layout="vertical">
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="供应商编码" required>
              <a-input v-model:value="form.code" placeholder="如：SUP-001" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="供应商名称" required>
              <a-input v-model:value="form.name" placeholder="请输入供应商名称" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="供应商简称">
              <a-input v-model:value="form.shortName" placeholder="请输入简称" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="联系人">
              <a-input v-model:value="form.contactPerson" placeholder="请输入联系人" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="联系电话">
              <a-input v-model:value="form.phone" placeholder="请输入联系电话" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="邮箱">
              <a-input v-model:value="form.email" placeholder="请输入邮箱" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="税号">
              <a-input v-model:value="form.taxNo" placeholder="请输入税号" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="开户行">
              <a-input v-model:value="form.bankName" placeholder="请输入开户行" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="银行账号">
              <a-input v-model:value="form.bankAccount" placeholder="请输入银行账号" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="账期(天)">
              <a-input-number v-model:value="form.paymentTermDays" :min="0" style="width: 100%" />
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item label="地址">
              <a-input v-model:value="form.address" placeholder="请输入地址" />
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item label="备注">
              <a-textarea v-model:value="form.remark" :rows="3" placeholder="请输入备注" />
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item label="状态">
              <a-switch v-model:checked="form.isActive" checked-children="启用" un-checked-children="停用" />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { message } from 'ant-design-vue'
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import { supplierApi } from '../../api'

const loading = ref(false)
const keyword = ref('')
const data = ref([])
const modalVisible = ref(false)
const editingRecord = ref(null)

const form = reactive({
  code: '',
  name: '',
  shortName: '',
  contactPerson: '',
  phone: '',
  email: '',
  taxNo: '',
  address: '',
  bankName: '',
  bankAccount: '',
  paymentTermDays: 30,
  remark: '',
  isActive: true
})

const columns = [
  { title: '供应商编码', dataIndex: 'code', key: 'code', width: 130 },
  { title: '供应商名称', dataIndex: 'name', key: 'name', width: 180, ellipsis: true },
  { title: '供应商简称', dataIndex: 'shortName', key: 'shortName', width: 120 },
  { title: '联系人', dataIndex: 'contactPerson', key: 'contactPerson', width: 120 },
  { title: '联系电话', dataIndex: 'phone', key: 'phone', width: 140 },
  { title: '邮箱', dataIndex: 'email', key: 'email', width: 180, ellipsis: true },
  { title: '税号', dataIndex: 'taxNo', key: 'taxNo', width: 180 },
  { title: '公司地址', dataIndex: 'address', key: 'address', width: 240, ellipsis: true },
  { title: '开户行', dataIndex: 'bankName', key: 'bankName', width: 180, ellipsis: true },
  { title: '银行账号', dataIndex: 'bankAccount', key: 'bankAccount', width: 180, ellipsis: true },
  { title: '账期(天)', dataIndex: 'paymentTermDays', key: 'paymentTermDays', width: 100 },
  { title: '关联三方员工', key: 'employeeCount', width: 120 },
  { title: '状态', key: 'isActive', width: 90 },
  { title: '操作', key: 'action', width: 140, fixed: 'right' }
]

const filteredData = computed(() => {
  const search = keyword.value.trim().toLowerCase()
  if (!search) {
    return data.value
  }

  return data.value.filter(item =>
    [item.code, item.name, item.shortName, item.contactPerson, item.phone, item.email, item.taxNo, item.address, item.bankName, item.bankAccount]
      .filter(Boolean)
      .some(value => value.toLowerCase().includes(search))
  )
})

const resetForm = () => {
  Object.assign(form, {
    code: '',
    name: '',
    shortName: '',
    contactPerson: '',
    phone: '',
    email: '',
    taxNo: '',
    address: '',
    bankName: '',
    bankAccount: '',
    paymentTermDays: 30,
    remark: '',
    isActive: true
  })
}

const buildPayload = () => ({
  code: form.code.trim(),
  name: form.name.trim(),
  shortName: form.shortName?.trim() || null,
  contactPerson: form.contactPerson?.trim() || null,
  phone: form.phone?.trim() || null,
  email: form.email?.trim() || null,
  taxNo: form.taxNo?.trim() || null,
  address: form.address?.trim() || null,
  bankName: form.bankName?.trim() || null,
  bankAccount: form.bankAccount?.trim() || null,
  paymentTermDays: Number(form.paymentTermDays || 0),
  remark: form.remark?.trim() || null,
  isActive: form.isActive
})

const loadData = async () => {
  loading.value = true
  try {
    data.value = await supplierApi.getAll()
  } finally {
    loading.value = false
  }
}

const openCreate = () => {
  editingRecord.value = null
  resetForm()
  modalVisible.value = true
}

const openEdit = (record) => {
  editingRecord.value = record
  Object.assign(form, {
    code: record.code || '',
    name: record.name || '',
    shortName: record.shortName || '',
    contactPerson: record.contactPerson || '',
    phone: record.phone || '',
    email: record.email || '',
    taxNo: record.taxNo || '',
    address: record.address || '',
    bankName: record.bankName || '',
    bankAccount: record.bankAccount || '',
    paymentTermDays: record.paymentTermDays ?? 30,
    remark: record.remark || '',
    isActive: record.isActive !== false
  })
  modalVisible.value = true
}

const handleSave = async () => {
  if (!form.code.trim() || !form.name.trim()) {
    message.error('请填写供应商编码和供应商名称')
    return
  }

  const payload = buildPayload()
  if (editingRecord.value) {
    await supplierApi.update({ id: editingRecord.value.id, ...payload })
    message.success('供应商更新成功')
  } else {
    await supplierApi.create(payload)
    message.success('供应商创建成功')
  }

  modalVisible.value = false
  await loadData()
}

const handleDelete = async (record) => {
  await supplierApi.delete(record.id)
  message.success('供应商删除成功')
  await loadData()
}

onMounted(loadData)
</script>

<style scoped>
.partner-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.page-title {
  font-size: 22px;
  font-weight: 600;
  color: #1f1f1f;
}

.page-desc {
  margin-top: 6px;
  color: #8c8c8c;
}

.page-card {
  border-radius: 12px;
}

.toolbar {
  margin-bottom: 16px;
}

.keyword-input {
  width: 320px;
}
</style>
