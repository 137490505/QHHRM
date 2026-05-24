<template>
  <div class="page-container">
    <a-card :bordered="false">
      <template #title>权限 / 菜单管理</template>
      <template #extra>
        <a-space>
          <a-button v-permission="'button.access.menu.create'" type="primary" @click="openCreateModal()">
            新增根节点
          </a-button>
          <a-button @click="loadData">刷新</a-button>
        </a-space>
      </template>

      <a-table :columns="columns" :data-source="treeRows" :loading="loading" row-key="id" :pagination="false">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'menuType'">
            <a-tag :color="typeColorMap[record.menuType] || 'blue'">{{ record.menuType }}</a-tag>
          </template>
          <template v-if="column.key === 'isVisible'">
            <a-tag :color="record.isVisible ? 'green' : 'default'">{{ record.isVisible ? '显示' : '隐藏' }}</a-tag>
          </template>
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'green' : 'red'">{{ record.isActive ? '启用' : '停用' }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-space>
              <a-button v-permission="'button.access.menu.edit'" type="link" size="small" @click="openSortModal(record)">排序</a-button>
              <a-button v-permission="'button.access.menu.create'" type="link" size="small" @click="openCreateModal(record)">新增子节点</a-button>
              <a-button v-permission="'button.access.menu.edit'" type="link" size="small" @click="openEditModal(record)">编辑</a-button>
              <a-popconfirm
                title="确认删除该节点及其下级节点吗？"
                ok-text="确认"
                cancel-text="取消"
                @confirm="handleDelete(record)"
              >
                <a-button v-permission="'button.access.menu.delete'" type="link" size="small" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      :title="form.id ? '编辑菜单' : '新增菜单'"
      width="720"
      @ok="handleSubmit"
      :confirm-loading="submitting"
    >
      <a-form layout="vertical">
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="上级节点">
              <a-tree-select
                v-model:value="form.parentId"
                allow-clear
                tree-default-expand-all
                :tree-data="treeSelectData"
                placeholder="可选，不选则为根节点"
              />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="菜单类型" required>
              <a-select v-model:value="form.menuType">
                <a-select-option value="module">模块</a-select-option>
                <a-select-option value="page">页面</a-select-option>
                <a-select-option value="button">按钮</a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="菜单Key" required>
              <a-input v-model:value="form.menuKey" placeholder="如：employee-list" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="菜单名称" required>
              <a-input v-model:value="form.menuName" placeholder="请输入菜单名称" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="路由路径">
              <a-input v-model:value="form.routePath" placeholder="如：/employees" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="组件路径">
              <a-input v-model:value="form.componentPath" placeholder="如：views/employee/EmployeeList.vue" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="图标">
              <a-input v-model:value="form.icon" placeholder="如：TeamOutlined" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="权限编码">
              <a-input v-model:value="form.permissionCode" placeholder="如：page.employee.list" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="排序">
              <a-input-number v-model:value="form.sortOrder" style="width: 100%" :min="0" />
            </a-form-item>
          </a-col>
          <a-col :span="6">
            <a-form-item label="显示">
              <a-switch v-model:checked="form.isVisible" />
            </a-form-item>
          </a-col>
          <a-col :span="6">
            <a-form-item label="启用">
              <a-switch v-model:checked="form.isActive" />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="sortModalVisible"
      title="菜单排序"
      width="420"
      @ok="handleSortSubmit"
      :confirm-loading="sortSubmitting"
      @cancel="resetSortForm"
    >
      <a-form layout="vertical">
        <a-form-item label="菜单名称">
          <a-input :value="sortForm.menuName" disabled />
        </a-form-item>
        <a-form-item label="排序值">
          <a-input-number v-model:value="sortForm.sortOrder" style="width: 100%" :min="0" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { message } from 'ant-design-vue'
import { accessApi } from '../../api'
import { authApi } from '../../api/auth'
import { registerDynamicRoutes } from '../../router'
import { useUserStore } from '../../store/user'

const loading = ref(false)
const submitting = ref(false)
const modalVisible = ref(false)
const sortModalVisible = ref(false)
const sortSubmitting = ref(false)
const menuTree = ref([])
const userStore = useUserStore()
const sortRecord = ref(null)
const MENU_SORT_INTERVAL = 20

const typeColorMap = {
  module: 'purple',
  page: 'blue',
  button: 'orange'
}

const form = reactive({
  id: '',
  parentId: undefined,
  menuKey: '',
  menuName: '',
  menuType: 'page',
  sortOrder: 0,
  routePath: '',
  componentPath: '',
  icon: '',
  isVisible: true,
  isActive: true,
  permissionCode: ''
})

const sortForm = reactive({
  id: '',
  menuName: '',
  sortOrder: 0
})

const columns = [
  { title: '名称', dataIndex: 'menuName', key: 'menuName', width: 220 },
  { title: '类型', key: 'menuType', width: 100 },
  { title: '排序', dataIndex: 'sortOrder', key: 'sortOrder', width: 90 },
  { title: '路由', dataIndex: 'routePath', key: 'routePath', width: 220 },
  { title: '组件', dataIndex: 'componentPath', key: 'componentPath', width: 260 },
  { title: '权限编码', dataIndex: 'permissionCode', key: 'permissionCode', width: 220 },
  { title: '显示', key: 'isVisible', width: 80 },
  { title: '启用', key: 'isActive', width: 80 },
  { title: '操作', key: 'actions', width: 220, fixed: 'right' }
]

const mapTreeSelect = (nodes = []) =>
  nodes.map(item => ({
    title: item.menuName,
    value: item.id,
    key: item.id,
    children: mapTreeSelect(item.children || [])
  }))

const treeSelectData = computed(() => mapTreeSelect(menuTree.value))
const treeRows = computed(() => menuTree.value)

const resetForm = () => {
  form.id = ''
  form.parentId = undefined
  form.menuKey = ''
  form.menuName = ''
  form.menuType = 'page'
  form.sortOrder = MENU_SORT_INTERVAL
  form.routePath = ''
  form.componentPath = ''
  form.icon = ''
  form.isVisible = true
  form.isActive = true
  form.permissionCode = ''
}

const resetSortForm = () => {
  sortForm.id = ''
  sortForm.menuName = ''
  sortForm.sortOrder = MENU_SORT_INTERVAL
  sortRecord.value = null
}

const loadData = async () => {
  loading.value = true
  try {
    menuTree.value = await accessApi.getMenuTree(true, true)
  } finally {
    loading.value = false
  }
}

const refreshCurrentContext = async () => {
  const context = await authApi.me()
  userStore.setAuthContext(context)
  userStore.setRoutesRegistered(false)
  registerDynamicRoutes(context.menus || [])
}

const openCreateModal = (parent = null) => {
  resetForm()
  if (parent) {
    form.parentId = parent.id
    form.sortOrder = Number(parent.sortOrder || 0) + MENU_SORT_INTERVAL
  }
  modalVisible.value = true
}

const openEditModal = (record) => {
  form.id = record.id
  form.parentId = record.parentId
  form.menuKey = record.menuKey
  form.menuName = record.menuName
  form.menuType = record.menuType
  form.sortOrder = Number(record.sortOrder || MENU_SORT_INTERVAL)
  form.routePath = record.routePath || ''
  form.componentPath = record.componentPath || ''
  form.icon = record.icon || ''
  form.isVisible = record.isVisible
  form.isActive = record.isActive
  form.permissionCode = record.permissionCode || ''
  modalVisible.value = true
}

const openSortModal = (record) => {
  sortRecord.value = { ...record }
  sortForm.id = record.id
  sortForm.menuName = record.menuName
  sortForm.sortOrder = Number(record.sortOrder || MENU_SORT_INTERVAL)
  sortModalVisible.value = true
}

const normalizeSortOrder = (value) => {
  const number = Number(value)
  if (!Number.isFinite(number) || number <= 0) {
    return MENU_SORT_INTERVAL
  }

  return Math.ceil(number / MENU_SORT_INTERVAL) * MENU_SORT_INTERVAL
}

const handleSubmit = async () => {
  if (!form.menuKey || !form.menuName || !form.menuType) {
    message.error('请完善菜单基础信息')
    return
  }

  submitting.value = true
  try {
    const payload = {
      ...form,
      sortOrder: normalizeSortOrder(form.sortOrder),
      routePath: form.routePath || null,
      componentPath: form.componentPath || null,
      icon: form.icon || null,
      permissionCode: form.permissionCode || null
    }
    if (form.id) {
      await accessApi.updateMenu(payload)
      message.success('菜单更新成功')
    } else {
      await accessApi.createMenu(payload)
      message.success('菜单创建成功')
    }
    modalVisible.value = false
    await loadData()
    await refreshCurrentContext()
  } finally {
    submitting.value = false
  }
}

const handleSortSubmit = async () => {
  if (!sortRecord.value || !sortForm.id) {
    return
  }

  sortSubmitting.value = true
  try {
    await accessApi.updateMenu({
      id: sortRecord.value.id,
      parentId: sortRecord.value.parentId || null,
      menuKey: sortRecord.value.menuKey,
      menuName: sortRecord.value.menuName,
      menuType: sortRecord.value.menuType,
      sortOrder: normalizeSortOrder(sortForm.sortOrder),
      routePath: sortRecord.value.routePath || null,
      componentPath: sortRecord.value.componentPath || null,
      icon: sortRecord.value.icon || null,
      isVisible: sortRecord.value.isVisible,
      isActive: sortRecord.value.isActive,
      permissionCode: sortRecord.value.permissionCode || null
    })
    message.success('菜单排序更新成功')
    sortModalVisible.value = false
    resetSortForm()
    await loadData()
    await refreshCurrentContext()
  } finally {
    sortSubmitting.value = false
  }
}

const handleDelete = async (record) => {
  await accessApi.deleteMenu(record.id)
  message.success('菜单删除成功')
  await loadData()
  await refreshCurrentContext()
}

onMounted(loadData)
</script>

<style scoped>
.page-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
