<template>
  <div class="employee-selector">
    <template v-if="triggerType === 'icon'">
      <div class="selector-icon-wrapper">
        <a-button
          type="default"
          @click="openModal"
          class="selector-icon-button"
          :id="id || undefined"
        >
          <template #icon><UserOutlined /></template>
        </a-button>
        <span class="selector-display-text" @click="openModal">
          {{ displayValue || placeholder }}
        </span>
      </div>
    </template>
    <template v-else-if="triggerType === 'button'">
      <div class="selector-button-wrapper">
        <a-button
          type="default"
          @click="openModal"
          class="selector-button"
          :id="id || undefined"
        >
          {{ displayValue || placeholder }}
        </a-button>
      </div>
    </template>
    <template v-else>
      <a-input
        :placeholder="placeholder"
        :readonly="true"
        :value="displayValue"
        @click="openModal"
        class="selector-input"
        :id="id || undefined"
      >
        <template #suffix>
          <UserOutlined v-if="!multiple && selectedEmployee" />
          <TeamOutlined v-else-if="multiple && selectedEmployees.length > 0" />
          <DownOutlined v-else />
        </template>
      </a-input>
    </template>

    <a-modal
      v-model:open="modalVisible"
      :title="modalTitle"
      :width="900"
      :bodyStyle="{ height: '550px', padding: '0' }"
      @ok="handleOk"
      @cancel="handleCancel"
      :maskClosable="false"
    >
      <div class="selector-content">
        <div class="left-panel">
          <div class="panel-header">
            <span class="panel-title">组织架构</span>
          </div>
          <div class="org-tree-container">
            <div class="org-search-wrapper">
              <input
                type="text"
                :value="orgSearchText"
                @input="onOrgSearchInput"
                @keyup.enter="onOrgSearch"
                placeholder="搜索组织"
                class="ant-input org-search-native"
              />
            </div>
            <a-tree
              v-model:selectedKeys="selectedOrgKeys"
              :tree-data="orgTreeData"
              :field-names="{ children: 'children', title: 'name', key: 'id' }"
              :showIcon="true"
              @select="handleOrgSelect"
              class="org-tree"
              ref="orgTreeRef"
            >
              <template #icon><FolderOutlined /></template>
            </a-tree>
          </div>
        </div>

        <div class="right-panel">
          <div class="panel-header">
            <span class="panel-title">员工列表</span>
            <span class="employee-count" v-if="selectedOrgName">当前: {{ selectedOrgName }}</span>
          </div>
          <div class="search-area">
            <div class="keyword-search-wrapper">
              <input
                type="text"
                :value="keyword"
                @input="onKeywordInput"
                @keyup.enter="handleSearch"
                placeholder="搜索姓名/工号"
                class="ant-input keyword-search-native"
              />
              <span v-if="keyword" class="ant-input-clear-icon" @click="clearKeyword">✕</span>
            </div>
          </div>

          <div class="employee-table-container">
            <a-table
              :columns="columns"
              :dataSource="employeeList"
              :pagination="pagination"
              :loading="loading"
              :row-selection="rowSelection"
              :scroll="{ y: 280 }"
              :size="'small'"
              @change="handleTableChange"
              row-key="id"
              class="employee-table"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'avatar'">
                  <a-avatar :size="28" style="background-color: #1890ff">
                    {{ record.name?.charAt(0) }}
                  </a-avatar>
                </template>
                <template v-else-if="column.key === 'employeeType'">
                  <a-tag :color="getEmployeeTypeColor(record.employeeType)">
                    {{ getEmployeeTypeText(record.employeeType) }}
                  </a-tag>
                </template>
              </template>
            </a-table>
          </div>

          <div class="selected-area" v-if="multiple">
            <div class="selected-header">
              <span>已选员工 ({{ selectedEmployees.length }})</span>
              <a-button type="link" size="small" @click="clearAll" v-if="selectedEmployees.length > 0">
                清除全部
              </a-button>
            </div>
            <div class="selected-tags">
              <a-tag
                v-for="emp in selectedEmployees"
                :key="emp.id"
                closable
                @close="removeEmployee(emp)"
                class="selected-tag"
              >
                <UserOutlined /> {{ emp.name }} ({{ emp.employeeNo }})
              </a-tag>
              <span v-if="selectedEmployees.length === 0" class="no-selected">暂无选择</span>
            </div>
          </div>
        </div>
      </div>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, computed, watch, reactive } from 'vue'
import {
  UserOutlined,
  TeamOutlined,
  DownOutlined,
  SearchOutlined,
  FolderOutlined
} from '@ant-design/icons-vue'
import { orgUnitApi, employeeApi } from '../api'
import { useOrgUnitStore } from '../store/orgUnit'

const props = defineProps({
  modelValue: {
    type: [String, Number, Array, Object],
    default: null
  },
  multiple: {
    type: Boolean,
    default: false
  },
  placeholder: {
    type: String,
    default: '请选择员工'
  },
  id: {
    type: String,
    default: ''
  },
  orgFilter: {
    type: Array,
    default: () => []
  },
  employeeType: {
    type: String,
    default: null
  },
  triggerType: {
    type: String,
    default: 'input'
  },
  displayMode: {
    type: String,
    default: 'detail'
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const orgUnitStore = useOrgUnitStore()

const modalVisible = ref(false)
const modalTitle = computed(() => props.multiple ? '选择员工（多选）' : '选择员工（单选）')

const orgTreeData = computed(() => orgUnitStore.treeData)
const selectedOrgKeys = ref([])
const selectedOrgName = ref('')
const orgSearchText = ref('')

const keyword = ref('')
const employeeList = ref([])
const loading = ref(false)

const selectedEmployees = ref([])
const selectedEmployee = ref(null)

const pagination = reactive({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50'],
  showTotal: (total) => `共 ${total} 条`
})

const columns = [
  {
    title: '',
    key: 'avatar',
    width: 50,
    align: 'center'
  },
  {
    title: '工号',
    dataIndex: 'employeeNo',
    key: 'employeeNo',
    width: 100
  },
  {
    title: '姓名',
    dataIndex: 'name',
    key: 'name',
    width: 80
  },
  {
    title: '所属部门',
    dataIndex: 'orgUnitName',
    key: 'orgUnitName',
    width: 120
  },
  {
    title: '员工类型',
    dataIndex: 'employeeType',
    key: 'employeeType',
    width: 90
  },
  {
    title: '手机号',
    dataIndex: 'phone',
    key: 'phone',
    width: 120
  }
]

const rowSelection = computed(() => ({
  type: props.multiple ? 'checkbox' : 'radio',
  selectedRowKeys: props.multiple
    ? selectedEmployees.value.map(e => e.id)
    : (selectedEmployee.value ? [selectedEmployee.value.id] : []),
  onChange: (selectedRowKeys, selectedRows) => {
    if (props.multiple) {
      selectedEmployees.value = [...selectedRows]
    } else {
      selectedEmployee.value = selectedRows[0] || null
    }
  }
}))

const displayValue = computed(() => {
  if (props.multiple) {
    if (selectedEmployees.value.length === 0) return ''
    if (selectedEmployees.value.length === 1) {
      return props.displayMode === 'name'
        ? (selectedEmployees.value[0].name || '')
        : `${selectedEmployees.value[0].name || ''}${selectedEmployees.value[0].employeeNo ? ` (${selectedEmployees.value[0].employeeNo})` : ''}`
    }
    return `已选 ${selectedEmployees.value.length} 人`
  } else {
    if (!selectedEmployee.value) return ''
    if (props.displayMode === 'name') {
      return selectedEmployee.value.name || ''
    }
    return `${selectedEmployee.value.name || ''}${selectedEmployee.value.employeeNo ? ` (${selectedEmployee.value.employeeNo})` : ''}`
  }
})

let searchTimer = null
const debouncedSearch = () => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    handleSearch()
  }, 300)
}

const openModal = async () => {
  modalVisible.value = true
  await loadOrgTree()
  await loadEmployees()
  initSelectedData()
}

const initSelectedData = () => {
  if (props.multiple && Array.isArray(props.modelValue) && props.modelValue.length > 0) {
    selectedEmployees.value = props.modelValue.map(v => {
      if (typeof v === 'object') return v
      return { id: v }
    })
    if (selectedEmployees.value.length > 0 && !selectedEmployees.value[0].name) {
      loadSelectedEmployeeDetails()
    }
  } else if (!props.multiple && props.modelValue) {
    if (typeof props.modelValue === 'object') {
      selectedEmployee.value = props.modelValue
    } else {
      selectedEmployee.value = { id: props.modelValue }
      loadSelectedEmployeeDetail()
    }
  } else {
    selectedEmployees.value = []
    selectedEmployee.value = null
  }
}

const loadSelectedEmployeeDetails = async () => {
  try {
    const ids = selectedEmployees.value.map(e => e.id)
    const promises = ids.map(id => employeeApi.getById(id))
    const results = await Promise.all(promises)
    selectedEmployees.value = results
      .map(r => r?.data || r)
      .filter(Boolean)
  } catch (error) {
    console.error('Failed to load employee details:', error)
  }
}

const loadSelectedEmployeeDetail = async () => {
  try {
    const response = await employeeApi.getById(selectedEmployee.value.id)
    const employee = response?.data || response
    if (employee) {
      selectedEmployee.value = employee
    }
  } catch (error) {
    console.error('Failed to load employee detail:', error)
  }
}

const loadOrgTree = async () => {
  try {
    await orgUnitStore.loadTreeData()
  } catch (error) {
    console.error('Failed to load org tree:', error)
  }
}

const loadEmployees = async () => {
  loading.value = true
  let response

  try {
    const params = {
      pageNum: pagination.current,
      pageSize: pagination.pageSize
    }

    if (selectedOrgKeys.value.length > 0) {
      params.orgUnitId = selectedOrgKeys.value[0]
    }

    if (keyword.value) {
      params.keyword = keyword.value
    }

    if (props.employeeType) {
      params.employeeType = props.employeeType
    }

    if (props.orgFilter.length > 0) {
      params.orgFilter = props.orgFilter.join(',')
    }

    response = await employeeApi.getAll(params)

    if (response) {
      employeeList.value = response.list || response
      pagination.total = response.total || employeeList.value.length
    } else {
      employeeList.value = []
      pagination.total = 0
    }
  } catch (error) {
    console.error('Failed to load employees:', error)
    employeeList.value = []
  } finally {
    loading.value = false
  }
}

const handleOrgSelect = async (keys, e) => {
  if (keys.length > 0) {
    selectedOrgKeys.value = keys
    const node = e.node
    selectedOrgName.value = node.title
    pagination.current = 1
    await loadEmployees()
  }
}

const handleSearch = async () => {
  pagination.current = 1
  await loadEmployees()
}

const handleTableChange = (pag) => {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  loadEmployees()
}

const onOrgSearchInput = (e) => {
  const target = e.target
  orgSearchText.value = target.value
}

const onOrgSearch = () => {
  // 这里可以添加组织搜索的逻辑
}

const onKeywordInput = (e) => {
  const target = e.target
  keyword.value = target.value
  debouncedSearch()
}

const clearKeyword = () => {
  keyword.value = ''
  handleSearch()
}

const removeEmployee = (emp) => {
  const index = selectedEmployees.value.findIndex(e => e.id === emp.id)
  if (index > -1) {
    selectedEmployees.value.splice(index, 1)
  }
}

const clearAll = () => {
  selectedEmployees.value = []
}

const handleOk = () => {
  if (props.multiple) {
    emit('update:modelValue', selectedEmployees.value)
    emit('change', selectedEmployees.value)
  } else {
    emit('update:modelValue', selectedEmployee.value)
    emit('change', selectedEmployee.value)
  }
  modalVisible.value = false
}

const handleCancel = () => {
  modalVisible.value = false
  resetState()
}

const resetState = () => {
  selectedOrgKeys.value = []
  selectedOrgName.value = ''
  keyword.value = ''
  orgSearchText.value = ''
  pagination.current = 1
}

const getEmployeeTypeColor = (type) => {
  const colors = {
    '正式': 'blue',
    '临时': 'orange',
    '派遣': 'green',
    '实习': 'purple'
  }
  return colors[type] || 'default'
}

const getEmployeeTypeText = (type) => {
  return type || '-'
}

watch(() => props.modelValue, () => {
  initSelectedData()
}, { immediate: true })
</script>

<style scoped>
.employee-selector {
  width: 100%;
}

.selector-input {
  cursor: pointer;
}

.selector-button-wrapper {
  width: 100%;
}

.selector-button {
  width: 100%;
  justify-content: flex-start;
}

.selector-icon-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.selector-icon-button {
  flex-shrink: 0;
}

.selector-display-text {
  color: rgba(0, 0, 0, 0.88);
  cursor: pointer;
  line-height: 32px;
}

.selector-content {
  display: flex;
  height: 100%;
}

.left-panel {
  width: 250px;
  border-right: 1px solid #f0f0f0;
  display: flex;
  flex-direction: column;
}

.right-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.panel-header {
  padding: 12px 16px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.panel-title {
  font-weight: 500;
  color: #333;
}

.employee-count {
  font-size: 12px;
  color: #999;
}

.org-tree-container {
  flex: 1;
  overflow: auto;
  padding: 8px;
}

.org-tree {
  background: transparent;
}

.search-area {
  padding: 12px 16px;
  border-bottom: 1px solid #f0f0f0;
}

.employee-table-container {
  flex: 1;
  overflow: auto;
}

.employee-table {
  padding: 0 8px;
}

.selected-area {
  border-top: 1px solid #f0f0f0;
  padding: 12px 16px;
  max-height: 120px;
  overflow: auto;
}

.selected-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  font-size: 13px;
  color: #333;
}

.selected-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.selected-tag {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  background: #f5f5f5;
  border-radius: 4px;
}

.no-selected {
  color: #999;
  font-size: 12px;
}

.org-search-wrapper,
.keyword-search-wrapper {
  position: relative;
  width: 100%;
}

.org-search-native,
.keyword-search-native {
  width: 100%;
  height: 32px;
  padding: 4px 11px;
  color: rgba(0, 0, 0, 0.88);
  font-size: 14px;
  line-height: 1.5714285714285714;
  background-color: #ffffff;
  background-image: none;
  border-width: 1px;
  border-style: solid;
  border-color: #d9d9d9;
  border-radius: 6px;
  transition: all 0.2s;
  box-sizing: border-box;
}

.org-search-native:hover,
.keyword-search-native:hover {
  border-color: #4096ff;
}

.org-search-native:focus,
.keyword-search-native:focus {
  border-color: #4096ff;
  box-shadow: 0 0 0 2px rgba(5, 145, 255, 0.1);
  outline: none;
}

.ant-input-clear-icon {
  position: absolute;
  top: 50%;
  right: 11px;
  transform: translateY(-50%);
  color: rgba(0, 0, 0, 0.25);
  cursor: pointer;
  font-size: 12px;
}

.ant-input-clear-icon:hover {
  color: rgba(0, 0, 0, 0.45);
}

.org-search-wrapper {
  margin-bottom: 8px;
}
</style>
