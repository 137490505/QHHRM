<template>
  <div class="org-unit-management">
    <div class="page-header">
      <div class="page-header-main">
        <div class="page-header-icon">
          <LayoutOutlined class="title-icon" />
        </div>
        <div class="page-header-text">
          <div class="header-title">组织架构管理</div>
          <div class="header-desc">管理企业组织层级结构，支持树状展示和维护</div>
        </div>
      </div>
    </div>

    <a-row :gutter="24">
      <a-col :span="10">
        <a-card :bordered="false" class="tree-card">
          <div class="tree-header">
            <span class="tree-title">组织树</span>
            <div class="tree-actions">
              <a-button type="primary" size="small" @click="addNode">
                <PlusOutlined /> 新增
              </a-button>
            </div>
          </div>
          <a-tree
            v-model:expandedKeys="expandedKeys"
            v-model:selectedKeys="selectedKeys"
            :tree-data="orgUnitStore.treeData"
            :field-names="{ children: 'children', title: 'name', key: 'id' }"
            @select="onSelect"
            class="org-tree"
          >
            <template #title="{ name, id }">
              <span class="tree-title-span">
                <component :is="getNodeIcon(id)" style="margin-right: 8px;" />
                {{ name }}
              </span>
            </template>
          </a-tree>
        </a-card>
      </a-col>

      <a-col :span="14">
        <a-card :bordered="false" class="detail-card">
          <div class="detail-header">
            <span class="detail-title">{{ selectedNode ? '编辑组织' : '组织详情' }}</span>
            <div v-if="selectedNode" class="detail-actions">
              <a-button type="primary" size="small" @click="editNode">
                <EditOutlined /> 编辑
              </a-button>
              <a-popconfirm
                title="确定要删除此组织及其子组织吗？"
                ok-text="确定"
                cancel-text="取消"
                @confirm="deleteNode"
              >
                <a-button type="danger" size="small">
                  <DeleteOutlined /> 删除
                </a-button>
              </a-popconfirm>
            </div>
          </div>

          <div v-if="selectedNode" class="detail-form">
            <a-form :model="nodeForm" layout="vertical">
              <a-row :gutter="16">
                <a-col :span="12">
                  <a-form-item name="name" label="组织名称" :rules="[{ required: true, message: '请输入组织名称' }]">
                    <a-input v-model:value="nodeForm.name" placeholder="请输入组织名称" />
                  </a-form-item>
                </a-col>
                <a-col :span="12">
                  <a-form-item name="code" label="组织编码" :rules="[{ required: true, message: '请输入组织编码' }]">
                    <a-input v-model:value="nodeForm.code" placeholder="请输入组织编码" />
                  </a-form-item>
                </a-col>
              </a-row>
              <a-row :gutter="16">
                <a-col :span="12">
                  <a-form-item name="level" label="组织级别">
                    <a-select v-model:value="nodeForm.level" class="full-width">
                      <a-select-option v-for="level in levelOptions" :key="level.value" :value="level.value">
                        {{ level.label }}
                      </a-select-option>
                    </a-select>
                  </a-form-item>
                </a-col>
                <a-col :span="12">
                  <a-form-item name="manager" label="负责人">
                    <EmployeeSelector 
                      :model-value="nodeForm.managerId" 
                      @update:model-value="val => { nodeForm.managerId = val?.id || val; nodeForm.manager = val?.name || val }" 
                      placeholder="请选择负责人" 
                      id="manager" 
                    />
                  </a-form-item>
                </a-col>
              </a-row>
              <a-row :gutter="16">
                <a-col :span="12">
                  <a-form-item name="phone" label="联系电话">
                    <a-input v-model:value="nodeForm.phone" placeholder="请输入联系电话" />
                  </a-form-item>
                </a-col>
                <a-col :span="12">
                  <a-form-item name="email" label="邮箱">
                    <a-input v-model:value="nodeForm.email" placeholder="请输入邮箱地址" />
                  </a-form-item>
                </a-col>
              </a-row>
              <a-form-item name="remark" label="备注">
                <a-textarea v-model:value="nodeForm.remark" :rows="3" placeholder="请输入备注信息" />
              </a-form-item>
            </a-form>
            <div class="form-actions">
              <a-button type="primary" @click="saveNode">
                <SaveOutlined /> 保存
              </a-button>
              <a-button @click="resetForm">
                重置
              </a-button>
            </div>
          </div>

          <div v-else class="empty-state">
            <LayoutOutlined class="empty-icon" />
            <p class="empty-text">请从左侧选择一个组织节点</p>
          </div>
        </a-card>
      </a-col>
    </a-row>

    <a-modal
      v-model:open="addModalVisible"
      :title="isEditNode ? '编辑组织' : '新增组织'"
      width="500px"
      @ok="handleAddOk"
      :destroyOnClose="true"
    >
      <a-form :model="addForm" layout="vertical">
        <a-form-item name="name" label="组织名称" :rules="[{ required: true, message: '请输入组织名称' }]">
          <a-input v-model:value="addForm.name" placeholder="请输入组织名称" />
        </a-form-item>
        <a-form-item name="code" label="组织编码" :rules="[{ required: true, message: '请输入组织编码' }]">
          <a-input v-model:value="addForm.code" placeholder="请输入组织编码" />
        </a-form-item>
        <a-form-item name="level" label="组织级别">
          <a-select v-model:value="addForm.level" class="full-width">
            <a-select-option v-for="level in levelOptions" :key="level.value" :value="level.value">
              {{ level.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item name="manager" label="负责人">
          <EmployeeSelector 
            :model-value="addForm.managerId" 
            @update:model-value="val => { addForm.managerId = val?.id || val; addForm.manager = val?.name || val }" 
            placeholder="请选择负责人" 
            id="add-manager" 
          />
        </a-form-item>
        <a-form-item name="remark" label="备注">
          <a-textarea v-model:value="addForm.remark" :rows="2" placeholder="请输入备注信息" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import {
  LayoutOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SaveOutlined,
  HomeOutlined,
  FolderOpenOutlined,
  FileTextOutlined,
  ClockCircleOutlined
} from '@ant-design/icons-vue'
import { orgUnitApi } from '../../api'
import { useOrgUnitStore } from '../../store/orgUnit'
import EmployeeSelector from '../../components/EmployeeSelector.vue'
import { loadSelectOptions } from '../../utils/selectOptions'

const orgUnitStore = useOrgUnitStore()

const expandedKeys = ref([])
const selectedKeys = ref([])
const selectedNode = ref(null)
const addModalVisible = ref(false)
const isEditNode = ref(false)
const editingNodeId = ref(null)

const levelOptions = ref([])

const nodeForm = reactive({
  name: '',
  code: '',
  level: 0,
  managerId: null,
  manager: '',
  phone: '',
  email: '',
  remark: ''
})

const addForm = reactive({
  name: '',
  code: '',
  level: 0,
  managerId: null,
  manager: '',
  remark: ''
})

const getNodeIcon = (id) => {
  const node = findNodeById(orgUnitStore.treeData, id)
  if (!node) return LayoutOutlined
  const icons = [HomeOutlined, FolderOpenOutlined, FileTextOutlined, ClockCircleOutlined]
  return icons[node.level] || LayoutOutlined
}

const findNodeById = (nodes, id) => {
  for (const node of nodes) {
    if (node.id === id) return node
    if (node.children && node.children.length > 0) {
      const found = findNodeById(node.children, id)
      if (found) return found
    }
  }
  return null
}

const loadTreeData = async () => {
  try {
    await orgUnitStore.refreshData()
  } catch (error) {
    console.error('Failed to load org data:', error)
  }
}

const loadLevelOptions = async () => {
  levelOptions.value = await loadSelectOptions('orgLevel')
}

const onSelect = (selectedKeysValue) => {
  selectedKeys.value = selectedKeysValue
  if (selectedKeysValue.length > 0) {
    const node = findNodeById(orgUnitStore.treeData, selectedKeysValue[0])
    if (node) {
      selectedNode.value = node
      updateNodeForm(node)
    }
  } else {
    selectedNode.value = null
  }
}

const updateNodeForm = (node) => {
  nodeForm.name = node.name
  nodeForm.code = node.code
  nodeForm.level = node.level
  nodeForm.managerId = node.managerId || null
  nodeForm.manager = node.manager || ''
  nodeForm.phone = node.phone || ''
  nodeForm.email = node.email || ''
  nodeForm.remark = node.remark || ''
}

const resetForm = () => {
  if (selectedNode.value) {
    updateNodeForm(selectedNode.value)
  }
}

const saveNode = async () => {
  if (!selectedNode.value) return
  
  try {
    const updateData = {
      id: selectedNode.value.id,
      name: nodeForm.name,
      code: nodeForm.code,
      level: nodeForm.level,
      managerId: nodeForm.managerId,
      manager: nodeForm.manager,
      phone: nodeForm.phone,
      email: nodeForm.email,
      remark: nodeForm.remark,
      parentId: selectedNode.value.parentId
    }
    
    await orgUnitStore.updateOrgUnit(updateData)
    
    const node = findNodeById(orgUnitStore.treeData, selectedNode.value.id)
    if (node) {
      updateNodeForm(node)
    }
    
    aMessage.success('保存成功')
  } catch (error) {
    aMessage.error('保存失败: ' + (error.message || '未知错误'))
  }
}

const addNode = () => {
  isEditNode.value = false
  editingNodeId.value = null
  addForm.name = ''
  addForm.code = ''
  addForm.level = selectedNode.value ? selectedNode.value.level + 1 : 0
  addForm.managerId = null
  addForm.manager = ''
  addForm.remark = ''
  addModalVisible.value = true
}

const editNode = () => {
  if (!selectedNode.value) return
  
  isEditNode.value = true
  editingNodeId.value = selectedNode.value.id
  addForm.name = selectedNode.value.name
  addForm.code = selectedNode.value.code
  addForm.level = selectedNode.value.level
  addForm.managerId = selectedNode.value.managerId || null
  addForm.manager = selectedNode.value.manager || ''
  addForm.remark = selectedNode.value.remark || ''
  addModalVisible.value = true
}

const handleAddOk = async () => {
  try {
    if (isEditNode.value && editingNodeId.value) {
      const updateData = {
        id: editingNodeId.value,
        name: addForm.name,
        code: addForm.code,
        level: addForm.level,
        managerId: addForm.managerId,
        manager: addForm.manager,
        remark: addForm.remark
      }
      
      await orgUnitStore.updateOrgUnit(updateData)
    } else {
      const createData = {
        name: addForm.name,
        code: addForm.code,
        level: addForm.level,
        managerId: addForm.managerId,
        manager: addForm.manager,
        remark: addForm.remark,
        parentId: selectedKeys.value.length > 0 ? selectedKeys.value[0] : null
      }
      
      await orgUnitStore.addOrgUnit(createData)
    }
    
    addModalVisible.value = false
    
    if (selectedKeys.value.length > 0) {
      expandedKeys.value = [...expandedKeys.value, selectedKeys.value[0]]
    }
    
    aMessage.success(isEditNode.value ? '修改成功' : '新增成功')
  } catch (error) {
    aMessage.error((isEditNode.value ? '修改' : '新增') + '失败: ' + (error.message || '未知错误'))
  }
}

const deleteNode = async () => {
  if (!selectedNode.value) return
  
  // 检查是否有子节点
  const hasChildren = orgUnitStore.listData.some(item => item.parentId === selectedNode.value.id);
  if (hasChildren) {
    aMessage.error('该组织下有子节点，无法删除');
    return;
  }
  
  try {
    await orgUnitStore.deleteOrgUnit(selectedNode.value.id)
    
    selectedKeys.value = []
    selectedNode.value = null
    
    aMessage.success('删除成功')
  } catch (error) {
    aMessage.error('删除失败: ' + (error.message || '未知错误'))
  }
}

import { message as aMessage } from 'ant-design-vue'

onMounted(async () => {
  await loadLevelOptions()
  await loadTreeData()
})
</script>

<style scoped>
.org-unit-management {
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
  background: linear-gradient(135deg, #f6ffed 0%, #eaffd7 100%);
  color: #389e0d;
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

.tree-card,
.detail-card {
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.09);
  height: calc(100vh - 180px);
  display: flex;
  flex-direction: column;
}

.tree-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #f0f0f0;
}

.tree-title {
  font-size: 16px;
  font-weight: 600;
}

.tree-actions {
  display: flex;
  gap: 8px;
}

.org-tree {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}

.tree-title-span {
  display: flex;
  align-items: center;
  gap: 8px;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #f0f0f0;
}

.detail-title {
  font-size: 16px;
  font-weight: 600;
}

.detail-actions {
  display: flex;
  gap: 8px;
}

.detail-form {
  padding: 24px;
  flex: 1;
  overflow-y: auto;
}

.full-width {
  width: 100%;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid #f0f0f0;
}

.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  color: #999;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.empty-text {
  font-size: 14px;
}

:deep(.ant-tree) {
  font-size: 14px;
}

:deep(.ant-tree-title) {
  padding: 4px 0;
}

:deep(.ant-tree-node-selected) {
  background: #e6f7ff;
}

:deep(.ant-form-item-label > label) {
  font-weight: 500;
  color: #333;
}
</style>
