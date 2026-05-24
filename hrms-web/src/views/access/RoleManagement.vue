<template>
  <div class="page-container">
    <a-card :bordered="false">
      <template #title>角色管理</template>
      <template #extra>
        <a-space>
          <a-button v-permission="'button.access.role.create'" type="primary" data-testid="role-add" @click="openCreateModal">
            新增角色
          </a-button>
          <a-button @click="loadData">刷新</a-button>
        </a-space>
      </template>

      <a-table :columns="columns" :data-source="roles" :loading="loading" row-key="id" bordered>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="record.isActive ? 'green' : 'red'">{{ record.isActive ? '启用' : '停用' }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-space>
              <a-button v-permission="'button.access.role.edit'" type="link" size="small" :data-testid="`role-edit-${record.id}`" @click="openEditModal(record)">编辑</a-button>
              <a-button v-permission="'button.access.role.assign'" type="link" size="small" :data-testid="`role-assign-${record.id}`" @click="openPermissionDrawer(record)">分配权限</a-button>
              <a-button v-permission="'button.access.role.assign'" type="link" size="small" @click="openCopyModal(record)">复制权限</a-button>
              <a-popconfirm
                title="确认删除该角色吗？"
                ok-text="确认"
                cancel-text="取消"
                @confirm="handleDelete(record)"
              >
                <a-button v-permission="'button.access.role.delete'" type="link" size="small" :data-testid="`role-delete-${record.id}`" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      :title="form.id ? '编辑角色' : '新增角色'"
      @ok="handleSubmit"
      :confirm-loading="submitting"
    >
      <a-form layout="vertical">
        <a-form-item label="角色编码" required>
          <a-input id="role-code" v-model:value="form.roleCode" placeholder="如：HR_MANAGER" />
        </a-form-item>
        <a-form-item label="角色名称" required>
          <a-input id="role-name" v-model:value="form.roleName" placeholder="请输入角色名称" />
        </a-form-item>
        <a-form-item label="角色描述">
          <a-textarea id="role-description" v-model:value="form.description" :rows="3" placeholder="请输入角色描述" />
        </a-form-item>
        <a-form-item label="启用状态">
          <a-switch v-model:checked="form.isActive" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="copyVisible"
      title="复制角色权限"
      @ok="handleCopyPermissions"
      :confirm-loading="copySubmitting"
    >
      <a-form layout="vertical">
        <a-form-item label="来源角色" required>
          <a-select v-model:value="copySourceId" placeholder="请选择要复制的角色">
            <a-select-option v-for="item in copySourceOptions" :key="item.id" :value="item.id">
              {{ item.roleName }} ({{ item.roleCode }})
            </a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <a-drawer
      v-model:open="permissionVisible"
      width="420"
      title="分配角色权限"
      @close="permissionLoading = false"
    >
      <a-spin :spinning="permissionLoading">
        <a-alert
          type="info"
          show-icon
          message="按模块 / 页面 / 按钮树形勾选权限"
          style="margin-bottom: 16px"
        />
        <a-tree
          v-model:checkedKeys="checkedKeys"
          checkable
          default-expand-all
          :tree-data="permissionTree"
        />
        <div class="drawer-footer">
          <a-space>
            <a-button @click="permissionVisible = false">取消</a-button>
            <a-button type="primary" data-testid="role-permission-save" :loading="permissionSubmitting" @click="handleSavePermissions">保存权限</a-button>
          </a-space>
        </div>
      </a-spin>
    </a-drawer>
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
const permissionLoading = ref(false)
const permissionSubmitting = ref(false)
const copyVisible = ref(false)
const copySubmitting = ref(false)
const modalVisible = ref(false)
const permissionVisible = ref(false)
const currentRoleId = ref('')
const copySourceId = ref(undefined)
const roles = ref([])
const checkedKeys = ref([])
const permissionTree = ref([])
const userStore = useUserStore()

const form = reactive({
  id: '',
  roleCode: '',
  roleName: '',
  description: '',
  isActive: true
})

const columns = [
  { title: '角色编码', dataIndex: 'roleCode', key: 'roleCode', width: 180 },
  { title: '角色名称', dataIndex: 'roleName', key: 'roleName', width: 180 },
  { title: '描述', dataIndex: 'description', key: 'description' },
  { title: '用户数', dataIndex: 'userCount', key: 'userCount', width: 100 },
  { title: '状态', key: 'status', width: 100 },
  { title: '操作', key: 'actions', width: 280, fixed: 'right' }
]

const resetForm = () => {
  form.id = ''
  form.roleCode = ''
  form.roleName = ''
  form.description = ''
  form.isActive = true
}

const mapTree = (nodes = []) =>
  nodes.map(item => ({
    key: item.id,
    title: `${item.menuName} (${item.menuType})`,
    children: mapTree(item.children || [])
  }))

const loadData = async () => {
  loading.value = true
  try {
    roles.value = await accessApi.getRoles()
  } finally {
    loading.value = false
  }
}

const loadPermissionTree = async () => {
  const menus = await accessApi.getMenuTree(true, true)
  permissionTree.value = mapTree(menus)
}

const shouldRefreshCurrentUser = (roleId) => {
  return !!roleId && (userStore.userInfo?.roleIds || []).includes(roleId)
}

const copySourceOptions = computed(() => roles.value.filter(item => item.id !== currentRoleId.value))

const refreshCurrentContext = async () => {
  const context = await authApi.me()
  userStore.setAuthContext(context)
  userStore.setRoutesRegistered(false)
  registerDynamicRoutes(context.menus || [])
}

const openCreateModal = () => {
  resetForm()
  modalVisible.value = true
}

const openEditModal = (record) => {
  form.id = record.id
  form.roleCode = record.roleCode
  form.roleName = record.roleName
  form.description = record.description || ''
  form.isActive = record.isActive
  modalVisible.value = true
}

const handleSubmit = async () => {
  if (!form.roleCode || !form.roleName) {
    message.error('请填写角色编码和角色名称')
    return
  }

  submitting.value = true
  try {
    if (form.id) {
      await accessApi.updateRole({ ...form })
      message.success('角色更新成功')
    } else {
      await accessApi.createRole({ ...form })
      message.success('角色创建成功')
    }
    modalVisible.value = false
    await loadData()
    if (shouldRefreshCurrentUser(form.id)) {
      await refreshCurrentContext()
    }
  } finally {
    submitting.value = false
  }
}

const openPermissionDrawer = async (record) => {
  currentRoleId.value = record.id
  permissionVisible.value = true
  permissionLoading.value = true
  try {
    const detail = await accessApi.getRole(record.id)
    checkedKeys.value = detail.permissionMenuIds || []
    await loadPermissionTree()
  } finally {
    permissionLoading.value = false
  }
}

const openCopyModal = (record) => {
  currentRoleId.value = record.id
  copySourceId.value = undefined
  copyVisible.value = true
}

const handleSavePermissions = async () => {
  permissionSubmitting.value = true
  try {
    await accessApi.assignRolePermissions(currentRoleId.value, checkedKeys.value)
    message.success('角色权限保存成功')
    permissionVisible.value = false
    await loadData()
    if (shouldRefreshCurrentUser(currentRoleId.value)) {
      await refreshCurrentContext()
    }
  } finally {
    permissionSubmitting.value = false
  }
}

const handleCopyPermissions = async () => {
  if (!copySourceId.value) {
    message.error('请选择来源角色')
    return
  }

  copySubmitting.value = true
  try {
    await accessApi.copyRolePermissions(currentRoleId.value, copySourceId.value)
    message.success('角色权限复制成功')
    copyVisible.value = false
    await loadData()
    if (shouldRefreshCurrentUser(currentRoleId.value)) {
      await refreshCurrentContext()
    }
  } finally {
    copySubmitting.value = false
  }
}

const handleDelete = async (record) => {
  const shouldRefresh = shouldRefreshCurrentUser(record.id)
  await accessApi.deleteRole(record.id)
  message.success('角色删除成功')
  await loadData()
  if (shouldRefresh) {
    await refreshCurrentContext()
  }
}

onMounted(async () => {
  await Promise.all([loadData(), loadPermissionTree()])
})
</script>

<style scoped>
.page-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.drawer-footer {
  margin-top: 16px;
  text-align: right;
}
</style>
