<template>
  <div class="page-container">
    <a-card :bordered="false">
      <template #title>用户管理</template>
      <template #extra>
        <a-space>
          <a-button v-permission="'button.access.user.create'" type="primary" data-testid="user-add" @click="openCreateModal">
            新增用户
          </a-button>
          <a-button @click="loadData">刷新</a-button>
        </a-space>
      </template>

      <a-table :columns="columns" :data-source="users" :loading="loading" row-key="id" bordered>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'roles'">
            <a-space wrap>
              <a-tag v-for="role in record.roleNames" :key="role" color="blue">{{ role }}</a-tag>
            </a-space>
          </template>
          <template v-if="column.key === 'postName'">
            {{ record.postName || '-' }}
          </template>
          <template v-if="column.key === 'status'">
            <a-tag :color="record.isActive ? 'green' : 'red'">{{ record.isActive ? '启用' : '停用' }}</a-tag>
          </template>
          <template v-if="column.key === 'admin'">
            <a-tag :color="record.isAdmin ? 'gold' : 'default'">{{ record.isAdmin ? '管理员' : '普通用户' }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" size="small" :data-testid="`user-view-${record.id}`" @click="openPermissionDrawer(record)">查看权限</a-button>
              <a-button v-permission="'button.access.user.edit'" type="link" size="small" :data-testid="`user-edit-${record.id}`" @click="openEditModal(record)">编辑</a-button>
              <a-popconfirm
                title="确认删除该用户吗？"
                ok-text="确认"
                cancel-text="取消"
                @confirm="handleDelete(record)"
              >
                <a-button v-permission="'button.access.user.delete'" type="link" size="small" :data-testid="`user-delete-${record.id}`" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      :title="form.id ? '编辑用户' : '新增用户'"
      width="680"
      @ok="handleSubmit"
      :confirm-loading="submitting"
    >
      <a-form layout="vertical">
        <a-row :gutter="16">
          <a-col :span="12">
            <a-form-item label="用户名" required>
              <a-input id="access-username" v-model:value="form.username" placeholder="请输入用户名" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item :label="form.id ? '新密码（留空则不修改）' : '密码'" :required="!form.id">
              <a-input-password id="access-password" v-model:value="form.password" placeholder="请输入密码" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="姓名" required>
              <a-input id="access-name" v-model:value="form.name" placeholder="请输入姓名" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="手机号">
              <a-input id="access-phone" v-model:value="form.phone" placeholder="请输入手机号" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="邮箱">
              <a-input id="access-email" v-model:value="form.email" placeholder="请输入邮箱" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="岗位">
              <a-select id="access-post" v-model:value="form.postId" allow-clear placeholder="请选择岗位">
                <a-select-option v-for="item in postOptions" :key="item.id" :value="item.id">
                  {{ item.name }}
                </a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item label="角色">
              <a-select id="access-roles" v-model:value="form.roleIds" mode="multiple" placeholder="请选择角色">
                <a-select-option v-for="item in roleOptions" :key="item.id" :value="item.id">
                  {{ item.name }}
                </a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="管理员">
              <a-switch v-model:checked="form.isAdmin" />
            </a-form-item>
          </a-col>
          <a-col :span="12">
            <a-form-item label="启用状态">
              <a-switch v-model:checked="form.isActive" />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>

    <a-drawer
      v-model:open="permissionVisible"
      width="560"
      title="用户生效权限"
    >
      <a-spin :spinning="permissionLoading">
        <a-descriptions bordered size="small" :column="1">
          <a-descriptions-item label="用户名">{{ permissionContext?.username || '-' }}</a-descriptions-item>
          <a-descriptions-item label="姓名">{{ permissionContext?.name || '-' }}</a-descriptions-item>
          <a-descriptions-item label="岗位">{{ permissionContext?.postName || '-' }}</a-descriptions-item>
          <a-descriptions-item label="角色">{{ (permissionContext?.roleNames || []).join('、') || '-' }}</a-descriptions-item>
          <a-descriptions-item label="账号类型">
            {{ permissionContext?.isAdmin ? '管理员（拥有全部权限）' : '普通用户' }}
          </a-descriptions-item>
        </a-descriptions>

        <a-alert
          style="margin: 16px 0"
          type="info"
          show-icon
          :message="`生效节点 ${permissionStats.menuCount} 个，权限编码 ${permissionStats.codeCount} 个`"
        />

        <div class="permission-section-title">生效权限树</div>
        <a-tree
          checkable
          default-expand-all
          :checkedKeys="permissionContext?.grantedMenuIds || []"
          :tree-data="permissionTree"
        />

        <div class="permission-section-title">权限编码</div>
        <div class="permission-code-list">
          <a-tag v-for="code in permissionContext?.permissionCodes || []" :key="code" color="blue">
            {{ code }}
          </a-tag>
          <span v-if="!(permissionContext?.permissionCodes || []).length" class="empty-text">暂无权限编码</span>
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
const modalVisible = ref(false)
const permissionVisible = ref(false)
const permissionLoading = ref(false)
const users = ref([])
const roleOptions = ref([])
const postOptions = ref([])
const permissionContext = ref(null)
const userStore = useUserStore()

const form = reactive({
  id: '',
  username: '',
  password: '',
  name: '',
  phone: '',
  email: '',
  postId: undefined,
  roleIds: [],
  isAdmin: false,
  isActive: true
})

const columns = [
  { title: '用户名', dataIndex: 'username', key: 'username', width: 160 },
  { title: '姓名', dataIndex: 'name', key: 'name', width: 140 },
  { title: '手机号', dataIndex: 'phone', key: 'phone', width: 140 },
  { title: '岗位', key: 'postName', width: 140 },
  { title: '角色', key: 'roles' },
  { title: '管理员', key: 'admin', width: 100 },
  { title: '状态', key: 'status', width: 100 },
  { title: '操作', key: 'actions', width: 220 }
]

const permissionStats = computed(() => ({
  menuCount: permissionContext.value?.grantedMenuIds?.length || 0,
  codeCount: permissionContext.value?.permissionCodes?.length || 0
}))

const getPermissionSourceLabel = (menuId) => {
  if (permissionContext.value?.isAdmin) {
    return '管理员'
  }

  const fromRole = (permissionContext.value?.rolePermissionMenuIds || []).includes(menuId)
  const fromPost = (permissionContext.value?.postPermissionMenuIds || []).includes(menuId)
  if (fromRole && fromPost) {
    return '角色+岗位'
  }
  if (fromRole) {
    return '角色'
  }
  if (fromPost) {
    return '岗位'
  }
  return '继承'
}

const mapPermissionTree = (nodes = []) =>
  nodes.map(item => ({
    key: item.id,
    title: `${item.menuName} (${item.menuType} / ${getPermissionSourceLabel(item.id)})`,
    children: mapPermissionTree(item.children || [])
  }))

const permissionTree = computed(() => mapPermissionTree(permissionContext.value?.menuTree || []))

const resetForm = () => {
  form.id = ''
  form.username = ''
  form.password = ''
  form.name = ''
  form.phone = ''
  form.email = ''
  form.postId = undefined
  form.roleIds = []
  form.isAdmin = false
  form.isActive = true
}

const loadData = async () => {
  loading.value = true
  try {
    users.value = await accessApi.getUsers()
  } finally {
    loading.value = false
  }
}

const loadOptions = async () => {
  const data = await accessApi.getOptions()
  roleOptions.value = data.roles || []
  postOptions.value = data.posts || []
}

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
  form.username = record.username
  form.password = ''
  form.name = record.name
  form.phone = record.phone || ''
  form.email = record.email || ''
  form.postId = record.postId
  form.roleIds = [...(record.roleIds || [])]
  form.isAdmin = record.isAdmin
  form.isActive = record.isActive
  modalVisible.value = true
}

const openPermissionDrawer = async (record) => {
  permissionVisible.value = true
  permissionLoading.value = true
  try {
    permissionContext.value = await accessApi.getUserPermissionContext(record.id)
  } finally {
    permissionLoading.value = false
  }
}

const handleSubmit = async () => {
  if (!form.username || !form.name || (!form.id && !form.password)) {
    message.error('请完善用户必填信息')
    return
  }

  submitting.value = true
  try {
    const payload = {
      ...form,
      password: form.password || undefined
    }
    if (form.id) {
      await accessApi.updateUser(payload)
      message.success('用户更新成功')
    } else {
      await accessApi.createUser(payload)
      message.success('用户创建成功')
    }
    modalVisible.value = false
    await loadData()
    if (form.id && userStore.userInfo?.id === form.id) {
      await refreshCurrentContext()
    }
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (record) => {
  await accessApi.deleteUser(record.id)
  message.success('用户删除成功')
  await loadData()
}

onMounted(async () => {
  await Promise.all([loadData(), loadOptions()])
})
</script>

<style scoped>
.page-container {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.permission-section-title {
  margin: 16px 0 8px;
  font-weight: 600;
}

.permission-code-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.empty-text {
  color: #999;
}
</style>
