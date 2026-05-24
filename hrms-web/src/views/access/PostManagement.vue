<template>
  <div class="page-container">
    <a-card :bordered="false">
      <template #title>岗位管理</template>
      <template #extra>
        <a-space>
          <a-button v-permission="'button.access.post.create'" type="primary" data-testid="post-add" @click="openCreateModal">
            新增岗位
          </a-button>
          <a-button @click="loadData">刷新</a-button>
        </a-space>
      </template>

      <a-table :columns="columns" :data-source="posts" :loading="loading" row-key="id" bordered>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="record.isActive ? 'green' : 'red'">{{ record.isActive ? '启用' : '停用' }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-space>
              <a-button v-permission="'button.access.post.edit'" type="link" size="small" :data-testid="`post-edit-${record.id}`" @click="openEditModal(record)">编辑</a-button>
              <a-button v-permission="'button.access.post.assign'" type="link" size="small" :data-testid="`post-assign-${record.id}`" @click="openPermissionDrawer(record)">分配权限</a-button>
              <a-button v-permission="'button.access.post.assign'" type="link" size="small" @click="openCopyModal(record)">复制权限</a-button>
              <a-popconfirm
                title="确认删除该岗位吗？"
                ok-text="确认"
                cancel-text="取消"
                @confirm="handleDelete(record)"
              >
                <a-button v-permission="'button.access.post.delete'" type="link" size="small" :data-testid="`post-delete-${record.id}`" danger>删除</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalVisible"
      :title="form.id ? '编辑岗位' : '新增岗位'"
      @ok="handleSubmit"
      :confirm-loading="submitting"
    >
      <a-form layout="vertical">
        <a-form-item label="岗位编码" required>
          <a-input id="post-code" v-model:value="form.postCode" placeholder="如：LINE_SUPERVISOR" />
        </a-form-item>
        <a-form-item label="岗位名称" required>
          <a-input id="post-name" v-model:value="form.postName" placeholder="请输入岗位名称" />
        </a-form-item>
        <a-form-item label="岗位描述">
          <a-textarea id="post-description" v-model:value="form.description" :rows="3" placeholder="请输入岗位描述" />
        </a-form-item>
        <a-form-item label="启用状态">
          <a-switch v-model:checked="form.isActive" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="copyVisible"
      title="复制岗位权限"
      @ok="handleCopyPermissions"
      :confirm-loading="copySubmitting"
    >
      <a-form layout="vertical">
        <a-form-item label="来源岗位" required>
          <a-select v-model:value="copySourceId" placeholder="请选择要复制的岗位">
            <a-select-option v-for="item in copySourceOptions" :key="item.id" :value="item.id">
              {{ item.postName }} ({{ item.postCode }})
            </a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <a-drawer v-model:open="permissionVisible" width="420" title="分配岗位权限">
      <a-spin :spinning="permissionLoading">
        <a-tree
          v-model:checkedKeys="checkedKeys"
          checkable
          default-expand-all
          :tree-data="permissionTree"
        />
        <div class="drawer-footer">
          <a-space>
            <a-button @click="permissionVisible = false">取消</a-button>
            <a-button type="primary" data-testid="post-permission-save" :loading="permissionSubmitting" @click="handleSavePermissions">保存权限</a-button>
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
const currentPostId = ref('')
const copySourceId = ref(undefined)
const posts = ref([])
const checkedKeys = ref([])
const permissionTree = ref([])
const userStore = useUserStore()

const form = reactive({
  id: '',
  postCode: '',
  postName: '',
  description: '',
  isActive: true
})

const columns = [
  { title: '岗位编码', dataIndex: 'postCode', key: 'postCode', width: 180 },
  { title: '岗位名称', dataIndex: 'postName', key: 'postName', width: 180 },
  { title: '描述', dataIndex: 'description', key: 'description' },
  { title: '用户数', dataIndex: 'userCount', key: 'userCount', width: 100 },
  { title: '状态', key: 'status', width: 100 },
  { title: '操作', key: 'actions', width: 280 }
]

const mapTree = (nodes = []) =>
  nodes.map(item => ({
    key: item.id,
    title: `${item.menuName} (${item.menuType})`,
    children: mapTree(item.children || [])
  }))

const resetForm = () => {
  form.id = ''
  form.postCode = ''
  form.postName = ''
  form.description = ''
  form.isActive = true
}

const loadData = async () => {
  loading.value = true
  try {
    posts.value = await accessApi.getPosts()
  } finally {
    loading.value = false
  }
}

const loadPermissionTree = async () => {
  const menus = await accessApi.getMenuTree(true, true)
  permissionTree.value = mapTree(menus)
}

const shouldRefreshCurrentUser = (postId) => {
  return !!postId && userStore.userInfo?.postId === postId
}

const copySourceOptions = computed(() => posts.value.filter(item => item.id !== currentPostId.value))

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
  form.postCode = record.postCode
  form.postName = record.postName
  form.description = record.description || ''
  form.isActive = record.isActive
  modalVisible.value = true
}

const handleSubmit = async () => {
  if (!form.postCode || !form.postName) {
    message.error('请填写岗位编码和岗位名称')
    return
  }

  submitting.value = true
  try {
    if (form.id) {
      await accessApi.updatePost({ ...form })
      message.success('岗位更新成功')
    } else {
      await accessApi.createPost({ ...form })
      message.success('岗位创建成功')
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
  currentPostId.value = record.id
  permissionVisible.value = true
  permissionLoading.value = true
  try {
    const detail = await accessApi.getPost(record.id)
    checkedKeys.value = detail.permissionMenuIds || []
    await loadPermissionTree()
  } finally {
    permissionLoading.value = false
  }
}

const openCopyModal = (record) => {
  currentPostId.value = record.id
  copySourceId.value = undefined
  copyVisible.value = true
}

const handleSavePermissions = async () => {
  permissionSubmitting.value = true
  try {
    await accessApi.assignPostPermissions(currentPostId.value, checkedKeys.value)
    message.success('岗位权限保存成功')
    permissionVisible.value = false
    await loadData()
    if (shouldRefreshCurrentUser(currentPostId.value)) {
      await refreshCurrentContext()
    }
  } finally {
    permissionSubmitting.value = false
  }
}

const handleCopyPermissions = async () => {
  if (!copySourceId.value) {
    message.error('请选择来源岗位')
    return
  }

  copySubmitting.value = true
  try {
    await accessApi.copyPostPermissions(currentPostId.value, copySourceId.value)
    message.success('岗位权限复制成功')
    copyVisible.value = false
    await loadData()
    if (shouldRefreshCurrentUser(currentPostId.value)) {
      await refreshCurrentContext()
    }
  } finally {
    copySubmitting.value = false
  }
}

const handleDelete = async (record) => {
  const shouldRefresh = shouldRefreshCurrentUser(record.id)
  await accessApi.deletePost(record.id)
  message.success('岗位删除成功')
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
