<template>
  <div class="org-unit-list">
    <!-- 布局：左侧树 + 右侧列表 -->
    <div class="layout-container">
      <!-- 左侧组织树 -->
      <div class="tree-panel">
        <div class="tree-header">
          <ApartmentOutlined class="header-icon" />
          <h3>组织架构</h3>
        </div>
        
        <!-- 树形结构 -->
        <a-tree 
          :tree-data="orgUnitStore.treeData"
          :field-names="{ children: 'children', title: 'name', key: 'id' }"
          :expanded-keys="expandedKeys"
          :selected-keys="selectedTreeKeys"
          :auto-expand-parent="true"
          class="org-tree"
          @select="handleTreeSelect"
          @expand="handleTreeExpand"
        >
          <template #title="{ name, id }">
            <span @contextmenu.prevent="showTreeMenu($event, id)">
              <component :is="getNodeIcon(id)" style="margin-right: 8px;" />
              {{ name }}
            </span>
          </template>
        </a-tree>

        <!-- 右键菜单 -->
        <a-dropdown 
          v-if="treeMenuVisible"
          :visible="treeMenuVisible"
          :trigger="[]"
          :placement="treeMenuPlacement"
        >
          <a-menu>
            <a-menu-item v-permission="'button.org.create'" key="add-child" @click="handleAddChild">
              <PlusOutlined /> 新增子节点
            </a-menu-item>
            <a-menu-item v-permission="'button.org.edit'" key="edit" @click="handleEditFromTree">
              <EditOutlined /> 编辑
            </a-menu-item>
            <a-menu-item key="move" @click="handleMove">
              <ArrowRightOutlined /> 移动
            </a-menu-item>
            <a-menu-item key="copy" @click="handleCopy">
              <CopyOutlined /> 复制
            </a-menu-item>
            <a-menu-divider />
            <a-menu-item v-permission="'button.org.delete'" key="delete" danger @click="handleDeleteFromTree">
              <DeleteOutlined /> 删除
            </a-menu-item>
          </a-menu>
        </a-dropdown>
      </div>

      <!-- 右侧列表 -->
      <div class="list-panel">
        <!-- 工具栏 -->
        <div class="toolbar">
          <a-space>
            <a-button v-permission="'button.org.create'" type="primary" data-testid="org-unit-add" @click="showAddModal">
              <PlusOutlined /> 新增
            </a-button>
            <a-button v-permission="'button.org.import'" @click="handleBatchImport">
              <UploadOutlined /> 批量导入
            </a-button>
            <a-button v-permission="'button.org.export'" @click="handleBatchExport">
              <DownloadOutlined /> 批量导出
            </a-button>
            <a-button @click="router.push('/org/chart')">
              <ApartmentOutlined /> 组织图
            </a-button>
            <a-button 
              v-permission="'button.org.edit'"
              v-if="selectedRows.length > 0"
              @click="handleBatchEdit"
            >
              <EditOutlined /> 批量修改
            </a-button>
            <a-button 
              v-permission="'button.org.enable'"
              v-if="selectedRows.length > 0"
              @click="handleBatchEnable"
            >
              <CheckCircleOutlined /> 批量启用
            </a-button>
            <a-button 
              v-permission="'button.org.disable'"
              v-if="selectedRows.length > 0"
              danger
              @click="handleBatchDisable"
            >
              <CloseCircleOutlined /> 批量停用
            </a-button>
          </a-space>
        </div>

        <!-- 查询条件 -->
        <div class="advanced-query-panel">
          <a-form :model="queryForm" layout="inline" class="query-form">
            <a-form-item name="name" label="名称">
              <a-input v-model:value="queryForm.name" placeholder="组织名称" allow-clear />
            </a-form-item>
            <a-form-item name="code" label="编码">
              <a-input v-model:value="queryForm.code" placeholder="组织编码" allow-clear />
            </a-form-item>
            <a-form-item name="level" label="类型">
              <a-select v-model:value="queryForm.level" placeholder="请选择" style="width: 120px">
                <a-select-option value="">全部</a-select-option>
                <a-select-option
                  v-for="level in orgLevelOptions"
                  :key="level.value"
                  :value="level.value"
                >
                  {{ level.label }}
                </a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item name="status" label="状态">
              <a-select v-model:value="queryForm.isActive" placeholder="请选择" style="width: 100px">
                <a-select-option value="">全部</a-select-option>
                <a-select-option value="true">启用</a-select-option>
                <a-select-option value="false">停用</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item>
              <a-space>
                <a-button type="primary" @click="handleSearch">
                  <template #icon><SearchOutlined /></template>
                  查询
                </a-button>
                <a-button @click="handleReset">重置</a-button>
              </a-space>
            </a-form-item>
          </a-form>
        </div>

        <!-- 表格 -->
        <a-table 
          :columns="columns" 
          :data-source="data" 
          :loading="loading" 
          row-key="id" 
          :pagination="pagination"
          :row-selection="{ selectedRowKeys: selectedRowKeys, onChange: handleRowSelect }"
          :custom-row="handleTableRow"
          :expanded-row-keys="tableExpandedRowKeys"
          :row-expandable="record => Array.isArray(record.children) && record.children.length > 0"
          @expandedRowsChange="handleTableExpand"
          @change="handleTableChange"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'level'">
              {{ typeMap[String(record.level)] || record.level }}
            </template>
            <template v-if="column.key === 'manager'">
              {{ record.manager || '-' }}
            </template>
            <template v-if="column.key === 'isActive'">
              <a-tag :color="record.isActive ? 'green' : 'red'">
                {{ record.isActive ? '启用' : '停用' }}
              </a-tag>
            </template>
            <template v-if="column.key === 'action'">
              <a-space>
                <a-button v-permission="'button.org.edit'" type="link" size="small" @click="handleEdit(record)">编辑</a-button>
                <a-button type="link" size="small" @click="handleView(record)">查看</a-button>
                <a-button 
                  v-permission="'button.org.edit'"
                  type="link" 
                  size="small" 
                  :danger="!record.isActive"
                  @click="handleToggleStatus(record)"
                >
                  {{ record.isActive ? '停用' : '启用' }}
                </a-button>
                <a-popconfirm
                  title="确定要删除此组织单元吗？"
                  ok-text="确定"
                  cancel-text="取消"
                  @confirm="handleDelete(record)"
                >
                  <a-button v-permission="'button.org.delete'" type="link" size="small" danger>删除</a-button>
                </a-popconfirm>
              </a-space>
            </template>
          </template>
        </a-table>
      </div>
    </div>

    <!-- 新增/编辑弹窗 -->
    <a-modal 
      v-model:open="modalVisible" 
      :title="editingRecord ? '编辑组织单元' : '新增组织单元'" 
      @ok="handleSave"
      :width="600"
      :ok-button-props="{ 'data-testid': 'org-unit-save' }"
      :cancel-button-props="{ 'data-testid': 'org-unit-cancel' }"
    >
      <a-form :model="form" :label-col="{ span: 6 }" :wrapper-col="{ span: 18 }">
        <a-form-item label="上级组织" name="parentId">
          <a-tree-select
            v-model:value="form.parentId"
            :tree-data="orgUnitStore.treeData"
            :field-names="{ children: 'children', label: 'name', value: 'id' }"
            :placeholder="editingRecord ? '选择新的上级组织' : '选择上级组织（可选）'"
            :disabled="isEditingSelf"
            id="org-parent-select"
            allow-clear
            tree-default-expand-all
          />
        </a-form-item>
        <a-form-item
          label="名称"
          name="name"
          :rules="[{ required: true, message: '请输入名称' }]"
        >
          <a-input v-model:value="form.name" id="org-unit-name" data-testid="org-unit-name" />
        </a-form-item>
        <a-form-item
          label="编码"
          name="code"
          :rules="[{ required: true, message: '请输入编码' }]"
        >
          <a-input v-model:value="form.code" :disabled="!!editingRecord" id="org-unit-code" data-testid="org-unit-code" />
        </a-form-item>
        <a-form-item label="类型" name="level">
          <a-select v-model:value="form.level" data-testid="org-unit-level">
            <a-select-option
              v-for="level in orgLevelOptions"
              :key="level.value"
              :value="level.value"
            >
              {{ level.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="负责人" name="manager">
          <EmployeeSelector
            :model-value="form.managerId"
            @update:model-value="val => { form.managerId = val?.id || val; form.manager = val?.name || val }"
            :placeholder="'选择负责人'"
            trigger-type="icon"
            display-mode="name"
            id="manager"
          />
        </a-form-item>
        <a-form-item label="排序号" name="sortOrder">
          <a-input-number v-model:value="form.sortOrder" :min="0" placeholder="排序号" />
        </a-form-item>
        <a-form-item label="状态" name="isActive">
          <a-switch v-model:checked="form.isActive" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 查看详情弹窗 -->
    <a-modal 
      v-model:open="viewModalVisible" 
      title="组织单元详情" 
      :footer="null"
      :width="700"
      class="view-detail-modal"
    >
      <div class="detail-container">
        <div class="detail-header-info">
          <div class="detail-icon-wrapper">
            <component :is="getNodeIcon(viewData.id)" class="detail-main-icon" />
          </div>
          <div class="detail-title-group">
            <h2 class="detail-main-name">{{ viewData.name }}</h2>
            <span class="detail-sub-code">{{ viewData.code }}</span>
          </div>
          <a-tag :color="viewData.isActive ? 'green' : 'red'" class="detail-status-tag">
            {{ viewData.isActive ? '启用中' : '已停用' }}
          </a-tag>
        </div>

        <a-divider />

        <a-descriptions :column="2" bordered size="middle">
          <a-descriptions-item label="组织级别">
            <a-tag color="blue">{{ typeMap[String(viewData.level)] }}</a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="排序号">{{ viewData.sortOrder }}</a-descriptions-item>
          <a-descriptions-item label="上级组织">
            <span v-if="viewData.parentName"><ApartmentOutlined /> {{ viewData.parentName }}</span>
            <span v-else style="color: #999;">无（顶级组织）</span>
          </a-descriptions-item>
          <a-descriptions-item label="负责人">
            <span v-if="viewData.manager"><UserOutlined /> {{ viewData.manager }}</span>
            <span v-else style="color: #999;">暂未设置</span>
          </a-descriptions-item>
          <a-descriptions-item label="创建时间" :span="2">{{ viewData.createdAt || '-' }}</a-descriptions-item>
        </a-descriptions>

        <div class="detail-footer">
          <a-button type="primary" @click="viewModalVisible = false">关闭</a-button>
        </div>
      </div>
    </a-modal>

    <!-- 移动节点弹窗 -->
    <a-modal 
      v-model:open="moveModalVisible" 
      title="移动节点" 
      @ok="handleMoveConfirm"
    >
      <a-form :model="moveForm" layout="vertical">
        <a-form-item name="targetParentId" label="选择目标父节点">
          <a-tree-select
            v-model:value="moveForm.targetParentId"
            :tree-data="orgUnitStore.treeData"
            :disabled-keys="[movingNodeId]"
            placeholder="选择目标父节点"
            id="move-target-select"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 复制节点弹窗 -->
    <a-modal
      v-model:open="copyModalVisible"
      title="复制节点"
      @ok="handleCopyConfirm"
    >
      <a-form :model="copyForm" :label-col="{ span: 6 }" :wrapper-col="{ span: 18 }">
        <a-form-item label="新编码" name="newCode">
          <a-input v-model:value="copyForm.newCode" placeholder="新节点编码" />
        </a-form-item>
        <a-form-item label="新名称" name="newName">
          <a-input v-model:value="copyForm.newName" placeholder="新节点名称" />
        </a-form-item>
        <a-form-item label="目标父节点" name="targetParentId">
          <a-tree-select
            v-model:value="copyForm.targetParentId"
            :tree-data="orgUnitStore.treeData"
            placeholder="选择目标父节点"
            id="copy-target-select"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 批量修改弹窗 -->
    <a-modal
      v-model:open="batchEditModalVisible"
      title="批量修改组织单元"
      @ok="handleBatchEditConfirm"
    >
      <a-form :model="batchEditForm" :label-col="{ span: 6 }" :wrapper-col="{ span: 18 }">
        <a-form-item label="修改范围">
          <span>已选中 {{ selectedRowKeys.length }} 条组织单元</span>
        </a-form-item>
        <a-form-item label="类型" name="level">
          <a-select
            v-model:value="batchEditForm.level"
            placeholder="不修改类型"
            allow-clear
          >
            <a-select-option
              v-for="level in orgLevelOptions"
              :key="level.value"
              :value="level.value"
            >
              {{ level.label }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="状态" name="isActive">
          <a-select
            v-model:value="batchEditForm.isActive"
            placeholder="不修改状态"
            allow-clear
          >
            <a-select-option :value="true">启用</a-select-option>
            <a-select-option :value="false">停用</a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 批量导入弹窗 -->
    <a-modal 
      v-model:open="importModalVisible" 
      title="批量导入组织单元" 
      @ok="handleImportConfirm"
      :footer="null"
      :width="600"
    >
      <a-space direction="vertical" style="width: 100%;">
        <a-button type="primary" @click="downloadTemplate">
          <DownloadOutlined /> 下载模板
        </a-button>
        
        <div class="upload-area" @click="triggerFileInput" @drop.prevent="handleDrop">
          <input 
            ref="fileInput"
            type="file" 
            accept=".xlsx,.xls"
            class="file-input"
            @change="handleFileSelect"
          />
          <UploadOutlined class="upload-icon" />
          <p>点击或拖拽上传Excel文件</p>
          <p class="upload-hint">支持 .xlsx, .xls 格式，文件大小不超过20MB</p>
        </div>

        <div v-if="uploadedFile" class="file-info">
          <a-tag color="blue">{{ uploadedFile.name }}</a-tag>
          <a-button type="text" danger @click="clearUpload">移除</a-button>
        </div>

        <a-form layout="vertical">
          <a-form-item name="importStrategy" label="导入策略">
            <a-select v-model:value="importStrategy">
              <a-select-option value="append">追加（跳过已存在）</a-select-option>
              <a-select-option value="overwrite">覆盖（更新已存在）</a-select-option>
              <a-select-option value="skip">跳过（忽略已存在）</a-select-option>
            </a-select>
          </a-form-item>
        </a-form>

        <div v-if="importPreview" class="import-preview">
          <h4>数据预览（前20行）</h4>
          <a-table :columns="importColumns" :data-source="importPreview" :pagination="false" size="small">
            <template #bodyCell="{ column, record }">
              <span :class="{ 'error-cell': record[column.dataIndex + '_error'] }">
                {{ record[column.dataIndex] }}
              </span>
            </template>
          </a-table>
          <p v-if="errorCount > 0" class="error-count">发现 {{ errorCount }} 条错误数据</p>
        </div>

        <div class="modal-footer">
          <a-button @click="importModalVisible = false">取消</a-button>
          <a-button type="primary" @click="handleImportConfirm" :disabled="!uploadedFile">
            确认导入
          </a-button>
        </div>
      </a-space>
    </a-modal>
  </div>
</template>

<script setup>import { ref, reactive, onMounted, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { message } from 'ant-design-vue';
import { 
  SearchOutlined, 
  PlusOutlined, 
  EditOutlined, 
  DeleteOutlined, 
  UploadOutlined, 
  DownloadOutlined, 
  CheckCircleOutlined, 
  CloseCircleOutlined, 
  ArrowRightOutlined, 
  CopyOutlined,
  HomeOutlined,
  FolderOpenOutlined,
  FileTextOutlined,
  ClockCircleOutlined,
  ApartmentOutlined,
  UserOutlined
} from '@ant-design/icons-vue';
import { orgUnitApi } from '../../api';
import { useOrgUnitStore } from '../../store/orgUnit';
import EmployeeSelector from '../../components/EmployeeSelector.vue';
import { buildOptionLabelMap, loadSelectOptions } from '../../utils/selectOptions';

const orgLevelOptions = ref([]);
const typeMap = computed(() => buildOptionLabelMap(orgLevelOptions.value));

const router = useRouter();
const orgUnitStore = useOrgUnitStore();

// 获取节点图标
const getNodeIcon = (id) => {
  const node = orgUnitStore.listData.find(i => i.id === id);
  if (!node) return ApartmentOutlined;
  const icons = [HomeOutlined, FolderOpenOutlined, FileTextOutlined, ClockCircleOutlined];
  return icons[node.level] || ApartmentOutlined;
};

// 状态
const loading = ref(false);
const data = ref([]);
const expandedKeys = ref([]);
const tableExpandedRowKeys = ref([]);
const selectedTreeKeys = ref([]);
const activeTreeFilterKey = ref(null);
const selectedRowKeys = ref([]);
const selectedRows = ref([]);
const modalVisible = ref(false);
const viewModalVisible = ref(false);
const moveModalVisible = ref(false);
const copyModalVisible = ref(false);
const batchEditModalVisible = ref(false);
const importModalVisible = ref(false);
const editingRecord = ref(null);
const editingManagerRecord = ref(null);
const viewData = ref({});
const movingNodeId = ref(null);
const copyingNodeId = ref(null);
const treeMenuVisible = ref(false);
const treeMenuPlacement = ref('bottomLeft');
const treeMenuKey = ref(null);
const fileInput = ref(null);
const uploadedFile = ref(null);
const importStrategy = ref('append');
const importPreview = ref([]);
const errorCount = ref(0);
// 分页
const pagination = reactive({
 pageSize: 20,
 current: 1,
 total: 0,
 showSizeChanger: true,
 pageSizeOptions: ['10', '20', '50', '100'],
 showTotal: (total, range) => `共 ${total} 条记录`
});
// 查询表单
const queryForm = reactive({
 name: '',
 code: '',
 level: '',
 isActive: ''
});
// 表单数据
const form = reactive({
 parentId: undefined,
 name: '',
 code: '',
 level: 3,
 managerId: null,
 manager: '',
 sortOrder: 0,
 isActive: true
});
// 移动表单
const moveForm = reactive({
 targetParentId: undefined
});
// 复制表单
const copyForm = reactive({
 newCode: '',
 newName: '',
 targetParentId: undefined
});
const batchEditForm = reactive({
 level: undefined,
 isActive: undefined
});
// 导入列
const importColumns = [
 { title: '编码', dataIndex: 'code' },
 { title: '名称', dataIndex: 'name' },
 { title: '类型', dataIndex: 'level' },
 { title: '上级编码', dataIndex: 'parentCode' },
 { title: '负责人', dataIndex: 'manager' }
];
// 表格列
const columns = [
 { title: '名称', dataIndex: 'name', key: 'name', width: 150 },
 { title: '编码', dataIndex: 'code', key: 'code', width: 120 },
 { title: '类型', dataIndex: 'level', key: 'level', width: 100 },
 { title: '负责人', key: 'manager', width: 150 },
 { title: '排序号', dataIndex: 'sortOrder', key: 'sortOrder', width: 80 },
 { title: '状态', dataIndex: 'isActive', key: 'isActive', width: 80 },
 { title: '操作', key: 'action', width: 250 }
];
// 是否正在编辑自己（用于禁用上级选择）
const isEditingSelf = computed(() => !!editingRecord.value);
// 加载数据
const loadOrgLevelOptions = async () => {
 orgLevelOptions.value = await loadSelectOptions('orgLevel');
};

const loadData = async () => {
 loading.value = true;
 try {
 await orgUnitStore.loadListData();
 await orgUnitStore.loadTreeData();
 
 // 默认只展开第1级
 if (orgUnitStore.treeData && orgUnitStore.treeData.length > 0) {
   expandedKeys.value = orgUnitStore.treeData.map(node => node.id);
   tableExpandedRowKeys.value = orgUnitStore.treeData.map(node => node.id);
 }

 applyFilters();
 }
 catch (error) {
 data.value = [];
 }
 finally {
 loading.value = false;
 }
};

// 应用过滤逻辑
const applyFilters = () => {
 const sourceList = Array.isArray(orgUnitStore.listData) ? orgUnitStore.listData : [];
 const itemMap = new Map(sourceList.map(item => [item.id, item]));
 const matchedIds = new Set();
 const hasQueryFilters = !!(
   queryForm.name ||
   queryForm.code ||
   queryForm.level !== '' && queryForm.level !== undefined ||
   queryForm.isActive !== '' && queryForm.isActive !== undefined
 );
 const matchesQuery = (item) => {
   if (!item) {
     return false;
   }
   if (queryForm.name && !(item.name && item.name.includes(queryForm.name))) {
     return false;
   }
   if (queryForm.code && !(item.code && item.code.includes(queryForm.code))) {
     return false;
   }
   if (queryForm.level !== '' && queryForm.level !== undefined && Number(item.level) !== Number(queryForm.level)) {
     return false;
   }
   if (queryForm.isActive !== '' && queryForm.isActive !== undefined) {
     const targetStatus = queryForm.isActive === 'true';
     const itemStatus = item.isActive === true || item.isActive === 1;
     if (itemStatus !== targetStatus) {
       return false;
     }
   }
   return true;
 };
 const buildTableTree = (nodes = []) => {
   return nodes.reduce((result, node) => {
     const currentItem = itemMap.get(node.id) || node;
     const children = buildTableTree(node.children || []);
     const selfMatched = matchesQuery(currentItem);
     if (hasQueryFilters && !selfMatched && children.length === 0) {
       return result;
     }
     matchedIds.add(currentItem.id);
     result.push({
       ...currentItem,
       children
     });
     return result;
   }, []);
 };
 const findTreeNodeById = (nodes = [], targetId) => {
   for (const node of nodes) {
     if (node.id === targetId) {
       return node;
     }
     if (Array.isArray(node.children) && node.children.length > 0) {
       const matchedNode = findTreeNodeById(node.children, targetId);
       if (matchedNode) {
         return matchedNode;
       }
     }
   }
   return null;
 };
 const countTreeNodes = (nodes = []) => {
   return nodes.reduce((count, node) => count + 1 + countTreeNodes(node.children || []), 0);
 };
 const collectExpandKeysByDepth = (nodes = [], depth = 0, maxDepth = 0) => {
   return nodes.reduce((keys, node) => {
     if (depth <= maxDepth && Array.isArray(node.children) && node.children.length > 0) {
       keys.push(node.id);
       keys.push(...collectExpandKeysByDepth(node.children, depth + 1, maxDepth));
     }
     return keys;
   }, []);
 };
 const getAncestorIds = (targetId) => {
   const ancestors = [];
   let currentItem = itemMap.get(targetId);
   while (currentItem?.parentId) {
     ancestors.unshift(currentItem.parentId);
     currentItem = itemMap.get(currentItem.parentId);
   }
   return ancestors;
 };

 let treeSource = Array.isArray(orgUnitStore.treeData) ? orgUnitStore.treeData : [];
 if (activeTreeFilterKey.value) {
   const selectedNode = findTreeNodeById(treeSource, activeTreeFilterKey.value);
   treeSource = selectedNode ? [selectedNode] : [];
   if (selectedNode) {
     tableExpandedRowKeys.value = [...new Set(collectExpandKeysByDepth([selectedNode], 0, 1))];
   }
 }

 const filteredTree = buildTableTree(treeSource);
 const shouldAppendRemainingItems = !activeTreeFilterKey.value && (!Array.isArray(orgUnitStore.treeData) || orgUnitStore.treeData.length === 0);
 const remainingItems = shouldAppendRemainingItems
   ? sourceList
     .filter(item => !matchedIds.has(item.id))
     .filter(item => !hasQueryFilters || matchesQuery(item))
     .map(item => ({ ...item, children: [] }))
   : [];

 data.value = [...filteredTree, ...remainingItems];
 pagination.total = countTreeNodes(data.value);
};

// 选择树节点
const handleTreeSelect = (selectedKeys) => {
 selectedTreeKeys.value = selectedKeys;
 activeTreeFilterKey.value = selectedKeys?.[0] || null;
 applyFilters();
};

// 判断是否为子孙节点或自身
const isDescendantOrSelf = (childId, parentId) => {
 if (childId === parentId) return true;
 const item = orgUnitStore.listData.find(i => i.id === childId);
 if (!item || !item.parentId) return false;
 if (item.parentId === parentId) return true;
 return isDescendantOrSelf(item.parentId, parentId);
};
// 展开树节点
const handleTreeExpand = (keys) => {
 expandedKeys.value = keys;
};
const handleTableExpand = (keys) => {
 tableExpandedRowKeys.value = keys;
};
const syncTreeFromTableRecord = (record) => {
 const rootIds = Array.isArray(orgUnitStore.treeData) ? orgUnitStore.treeData.map(node => node.id) : [];
 const parentMap = new Map((orgUnitStore.listData || []).map(item => [item.id, item.parentId]));
 const ancestorIds = [];
 let currentParentId = record?.parentId;

 while (currentParentId) {
   ancestorIds.unshift(currentParentId);
   currentParentId = parentMap.get(currentParentId);
 }

 selectedTreeKeys.value = record?.id ? [record.id] : [];
 expandedKeys.value = [...new Set([...rootIds, ...ancestorIds])];
 tableExpandedRowKeys.value = [...new Set([...tableExpandedRowKeys.value, ...ancestorIds])];
};
const isTableInteractiveElement = (event) => {
 const target = event?.target;
 return !!target?.closest?.('.ant-btn, .ant-checkbox-wrapper, .ant-checkbox, .ant-table-row-expand-icon, .ant-popconfirm, a, button, input, .ant-switch');
};
const handleTableRow = (record) => {
 return {
   onClick: (event) => {
     if (isTableInteractiveElement(event)) {
       return;
     }
     syncTreeFromTableRecord(record);
   }
 };
};
// 显示树右键菜单
const showTreeMenu = (event, key) => {
 treeMenuKey.value = key;
 treeMenuPlacement.value = 'bottomLeft';
 treeMenuVisible.value = true;
 // 点击其他地方关闭菜单
 setTimeout(() => {
 document.addEventListener('click', closeTreeMenu);
 }, 0);
};
// 关闭树右键菜单
const closeTreeMenu = () => {
 treeMenuVisible.value = false;
 document.removeEventListener('click', closeTreeMenu);
};
// 添加子节点
const handleAddChild = () => {
 closeTreeMenu();
 editingRecord.value = null;
 form.parentId = treeMenuKey.value;
 form.name = '';
 form.code = '';
 form.level = 3;
 form.sortOrder = 0;
 form.isActive = true;
 modalVisible.value = true;
};
// 从树编辑
const handleEditFromTree = () => {
 closeTreeMenu();
 const record = orgUnitStore.listData.find(item => item.id === treeMenuKey.value);
 if (record) {
 handleEdit(record);
 }
};
// 移动节点
const handleMove = () => {
 closeTreeMenu();
 movingNodeId.value = treeMenuKey.value;
 moveForm.targetParentId = undefined;
 moveModalVisible.value = true;
};
// 确认移动
const handleMoveConfirm = async () => {
 try {
 await orgUnitApi.move(movingNodeId.value, moveForm.targetParentId);
 message.success('移动成功');
 moveModalVisible.value = false;
 loadData();
 }
 catch (error) {
 message.error('移动失败，目标不能是子节点');
 }
};
// 复制节点
const handleCopy = () => {
 closeTreeMenu();
 copyingNodeId.value = treeMenuKey.value;
 const record = orgUnitStore.listData.find(item => item.id === treeMenuKey.value);
 copyForm.newCode = record?.code + '_copy';
 copyForm.newName = record?.name + ' (副本)';
 copyForm.targetParentId = record?.parentId;
 copyModalVisible.value = true;
};
// 确认复制
const handleCopyConfirm = async () => {
 try {
 await orgUnitApi.copy(copyingNodeId.value, copyForm);
 message.success('复制成功');
 copyModalVisible.value = false;
 loadData();
 }
 catch (error) {
 message.error('复制失败');
 }
};
// 从树删除
const handleDeleteFromTree = () => {
 closeTreeMenu();
 const record = orgUnitStore.listData.find(item => item.id === treeMenuKey.value);
 if (record) {
  handleDelete(record);
 }
};
// 搜索
const handleSearch = () => {
 pagination.current = 1;
 applyFilters();
};
// 重置
const handleReset = () => {
 Object.assign(queryForm, { name: '', code: '', level: '', isActive: '' });
 selectedTreeKeys.value = [];
 activeTreeFilterKey.value = null;
 pagination.current = 1;
 applyFilters();
};
// 表格变化
const handleTableChange = (paginationInfo) => {
 Object.assign(pagination, paginationInfo);
};
// 选择行
const handleRowSelect = (keys, rows) => {
 selectedRowKeys.value = keys;
 selectedRows.value = rows;
};
const resolveDefaultParentId = () => {
 if (selectedTreeKeys.value?.length) {
   return selectedTreeKeys.value[0];
 }
 if (activeTreeFilterKey.value) {
   return activeTreeFilterKey.value;
 }
 if (Array.isArray(data.value) && data.value.length === 1) {
   return data.value[0]?.id;
 }
 return undefined;
};
// 显示新增弹窗
const showAddModal = () => {
 editingRecord.value = null;
 Object.assign(form, {
 parentId: resolveDefaultParentId(),
 name: '',
 code: '',
 level: 3,
 managerId: null,
 manager: '',
 sortOrder: 0,
 isActive: true
 });
 modalVisible.value = true;
};
// 编辑
const handleEdit = (record) => {
 editingRecord.value = record;
 Object.assign(form, {
 parentId: record.parentId,
 name: record.name,
 code: record.code,
 level: Number.isFinite(Number(record.level)) ? Number(record.level) : record.level,
 managerId: record.managerId || null,
 manager: record.manager || '',
 sortOrder: record.sortOrder || 0,
 isActive: record.isActive
 });
 modalVisible.value = true;
};
// 查看详情
const handleView = (record) => {
 viewData.value = record;
 viewModalVisible.value = true;
};
// 切换状态
const handleToggleStatus = async (record) => {
 try {
 await orgUnitApi.toggleStatus(record.id);
 message.success(record.isActive ? '已停用' : '已启用');
 loadData();
 }
 catch (error) {
 message.error('操作失败');
 }
};
// 删除
const handleDelete = async (record) => {
 // 检查是否有子节点 (前端初步校验)
 const hasChildren = orgUnitStore.listData.some(item => item.parentId === record.id);
 if (hasChildren) {
  message.error('该组织下有子节点，无法删除');
  return;
 }
 
 try {
  await orgUnitStore.deleteOrgUnit(record.id);
  message.success('删除成功');
  loadData();
 }
 catch (error) {
  // 错误信息已由 request.js 拦截器弹出
 }
};
// 保存
const handleSave = async () => {
 try {
 if (editingRecord.value) {
 await orgUnitStore.updateOrgUnit({ id: editingRecord.value.id, ...form });
 message.success('更新成功');
 }
 else {
 await orgUnitStore.addOrgUnit(form);
 message.success('创建成功');
 }
 applyFilters();
 modalVisible.value = false;
 }
 catch (error) {
  // 错误信息已由 request.js 拦截器弹出
 }
};
// 批量导入
const handleBatchImport = () => {
 uploadedFile.value = null;
 importPreview.value = [];
 errorCount.value = 0;
 importModalVisible.value = true;
};
// 下载模板
const downloadTemplate = () => {
 message.info('模板下载中...');
};
// 触发文件选择
const triggerFileInput = () => {
 fileInput.value?.click();
};
// 处理文件选择
const handleFileSelect = (event) => {
 const file = event.target.files?.[0];
 if (file) {
 handleFile(file);
 }
};
// 处理拖拽
const handleDrop = (event) => {
 const file = event.dataTransfer?.files?.[0];
 if (file) {
 handleFile(file);
 }
};
// 处理文件
const handleFile = (file) => {
 if (!file.name.match(/\.(xlsx|xls)$/)) {
 message.error('请选择Excel文件');
 return;
 }
 if (file.size > 20 * 1024 * 1024) {
 message.error('文件大小不能超过20MB');
 return;
 }
 uploadedFile.value = file;
 // 模拟解析预览
 simulateParse(file);
};
// 模拟解析预览
const simulateParse = (file) => {
 importPreview.value = [
 { code: 'DEPT001', name: '测试部门1', type: '2', parentCode: '', manager: '张三' },
 { code: 'DEPT002', name: '测试部门2', type: '3', parentCode: 'DEPT001', manager: '李四' },
 { code: 'DEPT003', name: '', type: '4', parentCode: 'DEPT002', manager: '王五', code_error: '编码已存在' },
 { code: 'DEPT004', name: '测试部门4', type: '4', parentCode: 'DEPT002', manager: '赵六' }
 ];
 errorCount.value = 2;
};
// 清除上传
const clearUpload = () => {
 uploadedFile.value = null;
 importPreview.value = [];
 errorCount.value = 0;
 if (fileInput.value) {
 fileInput.value.value = '';
 }
};
// 确认导入
const handleImportConfirm = async () => {
 try {
 message.info('导入中...');
 await new Promise(resolve => setTimeout(resolve, 1000));
 message.success('导入成功');
 importModalVisible.value = false;
 loadData();
 }
 catch (error) {
 message.error('导入失败');
 }
};
// 批量导出
const handleBatchExport = () => {
 message.info('导出中...');
 setTimeout(() => {
 message.success('导出成功');
 }, 500);
};
// 批量修改
const handleBatchEdit = () => {
 Object.assign(batchEditForm, {
  level: undefined,
  isActive: undefined
 });
 batchEditModalVisible.value = true;
};
const handleBatchEditConfirm = async () => {
 if (!selectedRowKeys.value.length) {
  message.error('请先选择要修改的组织单元');
  return;
 }
 if (batchEditForm.level === undefined && batchEditForm.isActive === undefined) {
  message.error('请至少选择一个修改项');
  return;
 }
 try {
  await orgUnitApi.batchUpdate({
   ids: selectedRowKeys.value,
   level: batchEditForm.level,
   isActive: batchEditForm.isActive
  });
  message.success('批量修改成功');
  batchEditModalVisible.value = false;
  selectedRowKeys.value = [];
  selectedRows.value = [];
  await loadData();
 }
 catch (error) {
  // 错误信息已由 request.js 拦截器弹出
 }
};
// 批量启用
const handleBatchEnable = async () => {
 try {
 const ids = selectedRowKeys.value;
 await orgUnitApi.batchEnable(ids);
 message.success('批量启用成功');
 selectedRowKeys.value = [];
 selectedRows.value = [];
 loadData();
 }
 catch (error) {
 message.error('批量启用失败');
 }
};
// 批量停用
const handleBatchDisable = async () => {
 try {
 const ids = selectedRowKeys.value;
 await orgUnitApi.batchDisable(ids);
 message.success('批量停用成功');
 selectedRowKeys.value = [];
 selectedRows.value = [];
 loadData();
 }
 catch (error) {
 message.error('批量停用失败');
 }
};
onMounted(async () => {
 await loadOrgLevelOptions();
 await loadData();
});
</script>

<style scoped>
.org-unit-list {
  height: calc(100vh - 134px);
  display: flex;
}

.layout-container {
  display: flex;
  width: 100%;
  height: 100%;
}

/* 左侧树面板 */
.tree-panel {
  width: 280px;
  border-right: 1px solid #e8e8e8;
  display: flex;
  flex-direction: column;
}

.tree-header {
  padding: 16px 20px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  align-items: center;
  gap: 10px;
  background-color: #fafafa;
}

.header-icon {
  font-size: 18px;
  color: #1890ff;
}

.tree-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #262626;
}

.org-tree {
  flex: 1;
  overflow-y: auto;
  padding: 12px 16px;
}

/* 右侧列表面板 */
.list-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background-color: #fff;
}

.toolbar {
  padding: 16px 24px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.advanced-query-panel {
  padding: 20px 24px;
  background-color: #fff;
  border-bottom: 1px solid #f0f0f0;
}

.query-form :deep(.ant-form-item) {
  margin-bottom: 12px;
  margin-right: 16px;
}

.list-panel :deep(.ant-table-wrapper) {
  flex: 1;
  overflow: auto;
  padding: 0 24px 24px;
}

.list-panel :deep(.ant-table) {
  background: transparent;
}

.list-panel :deep(.ant-table-thead > tr > th) {
  background-color: #fafafa;
}

/* 弹窗样式 */
.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid #e8e8e8;
}

/* 导入区域 */
.upload-area {
  border: 2px dashed #d9d9d9;
  border-radius: 8px;
  padding: 32px;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s;
}

.upload-area:hover {
  border-color: #667eea;
}

.upload-icon {
  font-size: 48px;
  color: #999;
  margin-bottom: 12px;
}

.upload-hint {
  font-size: 12px;
  color: #999;
}

.file-input {
  display: none;
}

.file-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px;
  background: #f9fafb;
  border-radius: 4px;
}

.import-preview {
  max-height: 300px;
  overflow-y: auto;
}

.error-cell {
  color: #ff4d4f;
}

.error-count {
  color: #ff4d4f;
  font-size: 13px;
  margin-top: 8px;
}

/* 响应式 */
@media (max-width: 768px) {
  .layout-container {
    flex-direction: column;
  }

  .tree-panel {
    width: 100%;
    border-right: none;
    border-bottom: 1px solid #e8e8e8;
    max-height: 200px;
  }

  .toolbar {
    flex-wrap: wrap;
    gap: 8px;
  }
}

.manager-cell {
  display: flex;
  align-items: center;
  gap: 4px;
}

.manager-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.manager-selector-wrapper {
  padding: 16px 0;
}

.manager-selector-tip {
  margin-bottom: 16px;
  color: #666;
  font-size: 14px;
}

.selected-manager-info {
  margin-top: 16px;
  padding: 8px 12px;
  background-color: #f6ffed;
  border: 1px solid #b7eb8f;
  border-radius: 4px;
  color: #52c41a;
}

/* 查看详情弹窗美化 */
.view-detail-modal :deep(.ant-modal-content) {
  border-radius: 12px;
  overflow: hidden;
}

.detail-container {
  padding: 8px 0;
}

.detail-header-info {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 8px;
}

.detail-icon-wrapper {
  width: 64px;
  height: 64px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.detail-main-icon {
  font-size: 32px;
  color: #fff;
}

.detail-title-group {
  flex: 1;
}

.detail-main-name {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
  color: #262626;
}

.detail-sub-code {
  font-size: 14px;
  color: #8c8c8c;
  font-family: monospace;
}

.detail-status-tag {
  font-size: 14px;
  padding: 4px 12px;
  border-radius: 20px;
}

.detail-footer {
  margin-top: 24px;
  text-align: right;
}

:deep(.ant-descriptions-item-label) {
  width: 120px;
  background-color: #fafafa !important;
  font-weight: 500;
}
</style>
