<template>
  <div class="org-chart-page">
    <div class="page-header">
      <div class="header-main">
        <div class="header-icon">
          <ApartmentOutlined />
        </div>
        <div class="header-text">
          <h2>组织图</h2>
          <p>用更轻量的组织节点展示核心信息，快速浏览公司到部门的层级关系。</p>
          <div class="header-badges">
            <span class="header-pill">总数 {{ stats.total }}</span>
            <span class="header-pill">启用 {{ stats.active }}</span>
            <span class="header-pill">层级 {{ stats.maxDepth }}</span>
          </div>
        </div>
      </div>
      <div class="header-actions">
        <a-button @click="router.back()">
          <ArrowLeftOutlined /> 返回
        </a-button>
        <a-button type="primary" :loading="loading" @click="loadChartData">
          <ReloadOutlined /> 刷新
        </a-button>
      </div>
    </div>

    <div class="summary-grid">
      <div class="summary-card primary-card">
        <div class="summary-top">
          <span class="summary-icon">
            <ApartmentOutlined />
          </span>
          <span class="summary-label">组织总数</span>
        </div>
        <strong>{{ stats.total }}</strong>
        <span class="summary-desc">覆盖全部组织节点</span>
      </div>
      <div class="summary-card">
        <div class="summary-top">
          <span class="summary-icon accent-blue">
            <TeamOutlined />
          </span>
          <span class="summary-label">已配置负责人</span>
        </div>
        <strong>{{ stats.managerAssigned }}</strong>
        <span class="summary-desc">覆盖率 {{ managerCoverageText }}</span>
      </div>
      <div class="summary-card">
        <div class="summary-top">
          <span class="summary-icon accent-green">
            <CheckCircleOutlined />
          </span>
          <span class="summary-label">启用组织</span>
        </div>
        <strong>{{ stats.active }}</strong>
        <span class="summary-desc">停用 {{ stats.inactive }} 个节点</span>
      </div>
      <div class="summary-card">
        <div class="summary-top">
          <span class="summary-icon accent-violet">
            <ApartmentOutlined />
          </span>
          <span class="summary-label">最大层级</span>
        </div>
        <strong>{{ stats.maxDepth }}</strong>
        <span class="summary-desc">按树结构自动计算</span>
      </div>
    </div>

    <div class="content-grid">
      <a-card :bordered="false" class="chart-card">
        <template #title>
          <div class="card-title">
            <span>组织图</span>
            <a-tag color="blue">前两层卡片，后续竖向树状</a-tag>
          </div>
        </template>
        <div class="chart-toolbar">
          <div class="legend-list">
            <span class="legend-item">
              <span class="legend-dot active"></span>
              启用组织
            </span>
            <span class="legend-item">
              <span class="legend-dot inactive"></span>
              停用组织
            </span>
            <span class="legend-item">
              <span class="legend-dot info"></span>
              点击节点查看详情
            </span>
          </div>
          <span class="chart-hint">仅展示组织名称和关键状态信息</span>
        </div>
        <div v-if="loading" class="chart-loading">
          <a-spin tip="正在加载组织图..." />
        </div>
        <div v-else-if="!treeData.length" class="empty-state">
          <ApartmentOutlined class="empty-icon" />
          <p>暂无组织架构数据</p>
        </div>
        <div v-else class="chart-scroll">
          <div class="org-structure-board">
            <div v-for="root in treeData" :key="root.id" class="root-block">
              <div
                class="org-node root-node"
                :class="{ inactive: !root.isActive }"
              >
                <span class="node-main">
                  <span class="node-head">
                    <button
                      type="button"
                      class="node-name node-action"
                      @click="openNode(root)"
                    >
                      {{ root.name }}
                    </button>
                    <span class="node-state" :class="{ inactive: !root.isActive }">
                      <span class="node-state-dot"></span>
                      {{ root.isActive ? '启用' : '停用' }}
                    </span>
                  </span>
                </span>
                <button
                  type="button"
                  class="node-secondary node-action manager-action"
                  :disabled="!root.managerId"
                  @click="openManager(root)"
                >
                  {{ getManagerText(root) }}
                </button>
              </div>

              <div v-if="root.children?.length" class="second-row">
                <div v-for="child in root.children" :key="child.id" class="second-column">
                  <div
                    class="org-node second-node"
                    :class="{ inactive: !child.isActive }"
                  >
                    <span class="node-main">
                      <span class="node-head">
                        <button
                          type="button"
                          class="node-name node-action"
                          @click="openNode(child)"
                        >
                          {{ child.name }}
                        </button>
                        <span class="node-state" :class="{ inactive: !child.isActive }">
                          <span class="node-state-dot"></span>
                          {{ child.isActive ? '启用' : '停用' }}
                        </span>
                      </span>
                    </span>
                    <button
                      type="button"
                      class="node-secondary node-action manager-action"
                      :disabled="!child.managerId"
                      @click="openManager(child)"
                    >
                      {{ getManagerText(child) }}
                    </button>
                  </div>
                  <OrgTreeBranch
                    v-if="child.children?.length"
                    :nodes="child.children"
                    :level="3"
                    @select="openNode"
                    @select-manager="openManager"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </a-card>
    </div>

    <a-modal
      v-model:open="detailModalVisible"
      title="组织详情"
      width="760px"
      :footer="null"
      destroy-on-close
    >
      <div v-if="selectedNode" class="detail-modal">
        <div class="detail-hero">
          <div class="detail-header">
            <div class="detail-header-side"></div>
            <div class="detail-title-block">
              <div class="detail-name">{{ selectedNode.name }}</div>
            </div>
            <div class="detail-header-side detail-header-side-right">
              <div class="detail-status-chip" :class="{ inactive: !selectedNode.isActive }">
                <span class="detail-status-dot"></span>
                {{ selectedNode.isActive ? '启用中' : '已停用' }}
              </div>
            </div>
          </div>
        </div>

        <div class="detail-content-shell">
          <div class="detail-overview-grid simple">
            <div class="overview-card">
              <span class="overview-label">负责人</span>
              <strong>{{ selectedNode.manager || '未设置负责人' }}</strong>
            </div>
            <div class="overview-card">
              <span class="overview-label">上级组织</span>
              <strong>{{ selectedNode.parentName || '顶级组织' }}</strong>
            </div>
            <div class="overview-card">
              <span class="overview-label">组织编码</span>
              <strong>{{ selectedNode.code || '未设置编码' }}</strong>
            </div>
          </div>
        </div>
      </div>
    </a-modal>

  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import {
  ApartmentOutlined,
  ArrowLeftOutlined,
  ReloadOutlined,
  TeamOutlined,
  CheckCircleOutlined
} from '@ant-design/icons-vue'
import { orgUnitApi } from '../../api'
import { buildOptionLabelMap, loadSelectOptions } from '../../utils/selectOptions'
import OrgTreeBranch from './OrgTreeBranch.vue'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const treeData = ref([])
const selectedNode = ref(null)
const detailModalVisible = ref(false)
const orgLevelOptions = ref([])

const typeMap = computed(() => buildOptionLabelMap(orgLevelOptions.value))

const getNodeField = (node, key) => {
  if (!node || typeof node !== 'object') {
    return undefined
  }

  if (node[key] !== undefined) {
    return node[key]
  }

  const pascalKey = `${key.charAt(0).toUpperCase()}${key.slice(1)}`
  return node[pascalKey]
}

const getNodeChildren = (node) => {
  const children = getNodeField(node, 'children')
  return Array.isArray(children) ? children : []
}

const extractSortNumber = (value = '') => {
  const match = String(value).match(/\d+/)
  return match ? Number(match[0]) : Number.POSITIVE_INFINITY
}

const compareNodes = (left, right) => {
  const leftCode = String(left.code || '')
  const rightCode = String(right.code || '')
  const leftNumber = extractSortNumber(leftCode)
  const rightNumber = extractSortNumber(rightCode)

  if (Number.isFinite(leftNumber) || Number.isFinite(rightNumber)) {
    if (leftNumber !== rightNumber) {
      return leftNumber - rightNumber
    }
  }

  return leftCode.localeCompare(rightCode, 'zh-CN', { numeric: true, sensitivity: 'base' })
}

const normalizeTree = (nodes = [], parentName = null) => {
  const safeNodes = Array.isArray(nodes) ? nodes : []

  return safeNodes.map(node => {
    const name = `${getNodeField(node, 'name') || ''}`.trim()
    const code = `${getNodeField(node, 'code') || ''}`.trim()
    const level = Number(getNodeField(node, 'level') ?? 0)
    const children = normalizeTree(getNodeChildren(node), name)

    return {
      ...node,
      id: getNodeField(node, 'id'),
      code,
      name,
      level,
      parentId: getNodeField(node, 'parentId') ?? null,
      parentName: getNodeField(node, 'parentName') || parentName,
      managerId: getNodeField(node, 'managerId') ?? null,
      manager: getNodeField(node, 'manager') || null,
      isActive: Boolean(getNodeField(node, 'isActive') ?? true),
      levelLabel: typeMap.value[String(level)] || `层级 ${level}`,
      childrenCount: children.length,
      children
    }
  }).sort(compareNodes)
}

const buildTreeFromList = (list = []) => {
  if (!Array.isArray(list)) {
    return []
  }

  const nodeMap = new Map()
  const roots = []

  list.forEach(item => {
    const id = getNodeField(item, 'id')
    if (!id) {
      return
    }

    nodeMap.set(id, {
      ...item,
      id,
      code: `${getNodeField(item, 'code') || ''}`.trim(),
      name: `${getNodeField(item, 'name') || ''}`.trim(),
      level: Number(getNodeField(item, 'level') ?? 0),
      parentId: getNodeField(item, 'parentId') ?? null,
      parentName: getNodeField(item, 'parentName') || null,
      managerId: getNodeField(item, 'managerId') ?? null,
      manager: getNodeField(item, 'manager') || null,
      isActive: Boolean(getNodeField(item, 'isActive') ?? true),
      children: []
    })
  })

  nodeMap.forEach(node => {
    if (node.parentId && node.parentId !== node.id && nodeMap.has(node.parentId)) {
      nodeMap.get(node.parentId).children.push(node)
      return
    }

    roots.push(node)
  })

  return roots
}

const stats = computed(() => {
  const flatNodes = []
  const traverse = (nodes = [], depth = 1) => {
    nodes.forEach(node => {
      flatNodes.push({ ...node, depth })
      traverse(node.children || [], depth + 1)
    })
  }

  traverse(treeData.value)

  return {
    total: flatNodes.length,
    managerAssigned: flatNodes.filter(node => node.manager).length,
    active: flatNodes.filter(node => node.isActive).length,
    inactive: flatNodes.filter(node => !node.isActive).length,
    maxDepth: flatNodes.length ? Math.max(...flatNodes.map(node => node.depth)) : 0
  }
})

const managerCoverageText = computed(() => {
  if (!stats.value.total) {
    return '暂无组织数据'
  }

  return `${Math.round((stats.value.managerAssigned / stats.value.total) * 100)}% 节点已配置`
})

const getManagerText = (node) => node?.manager || '未设置负责人'

const openNode = (node) => {
  selectedNode.value = node
  detailModalVisible.value = true
}

const openManager = (node) => {
  if (!node?.managerId) {
    message.info('该组织未设置负责人')
    return
  }

  router.push({
    path: `/employees/detail/${node.managerId}`,
    query: {
      from: route.fullPath
    }
  })
}

const loadChartData = async () => {
  loading.value = true
  try {
    const levelOptions = await loadSelectOptions('orgLevel')
    orgLevelOptions.value = levelOptions

    let orgTree = []
    let usedListFallback = false

    try {
      const treeResponse = await orgUnitApi.getTree()
      orgTree = Array.isArray(treeResponse) ? treeResponse : []
    } catch (treeError) {
      console.warn('Failed to load org unit tree, fallback to list data:', treeError)
      const listResponse = await orgUnitApi.getAll()
      orgTree = buildTreeFromList(listResponse)
      usedListFallback = true
    }

    treeData.value = normalizeTree(orgTree)
    selectedNode.value = null
    detailModalVisible.value = false

    if (usedListFallback) {
      message.warning('组织树接口异常，已自动使用列表数据生成组织图')
    }
  } catch (error) {
    treeData.value = []
    selectedNode.value = null
    detailModalVisible.value = false
    console.error('Failed to load org chart data:', error)
    message.error('加载组织图失败')
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadChartData()
})
</script>

<style scoped>
.org-chart-page {
  --surface: #ffffff;
  --surface-strong: #ffffff;
  --surface-soft: #ffffff;
  --surface-tint: #ffffff;
  --border-soft: rgba(148, 163, 184, 0.16);
  --border-blue: rgba(226, 232, 240, 0.96);
  --shadow-soft: 0 4px 14px rgba(15, 23, 42, 0.04);
  --shadow-hover: 0 8px 18px rgba(15, 23, 42, 0.06);
  min-height: calc(100vh - 32px);
  background: #f5f7fa;
  border-radius: 24px;
  padding: 24px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
  padding: 24px 28px;
  background: var(--surface-strong);
  border-radius: 24px;
  color: #1f2937;
  border: 1px solid var(--border-soft);
  box-shadow: var(--shadow-soft);
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
  max-width: 620px;
}

.header-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 14px;
}

.header-pill {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  border-radius: 999px;
  background: rgba(22, 119, 255, 0.06);
  border: 1px solid rgba(22, 119, 255, 0.12);
  color: #334155;
  font-size: 12px;
  font-weight: 600;
}

.header-actions {
  display: flex;
  gap: 12px;
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
  background: var(--surface);
  border: 1px solid var(--border-soft);
  border-radius: 18px;
  box-shadow: var(--shadow-soft);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.summary-card:hover {
  border-color: rgba(203, 213, 225, 0.92);
  box-shadow: var(--shadow-hover);
}

.summary-top {
  display: flex;
  align-items: center;
  gap: 10px;
}

.summary-icon {
  width: 34px;
  height: 34px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.18);
  color: inherit;
  font-size: 16px;
}

.accent-blue {
  background: rgba(22, 119, 255, 0.1);
  color: #1677ff;
}

.accent-green {
  background: rgba(22, 163, 74, 0.12);
  color: #16a34a;
}

.accent-violet {
  background: rgba(124, 58, 237, 0.1);
  color: #7c3aed;
}

.primary-card {
  background: #ffffff;
  color: #0f172a;
  border-color: var(--border-soft);
}

.summary-label {
  font-size: 13px;
  color: inherit;
  opacity: 0.78;
}

.summary-card strong {
  font-size: 30px;
  line-height: 1.1;
  color: inherit;
}

.summary-desc {
  font-size: 12px;
  color: inherit;
  opacity: 0.7;
}

.content-grid {
  display: block;
}

.chart-card {
  background: var(--surface-strong);
  border: 1px solid var(--border-soft);
  border-radius: 24px;
  box-shadow: var(--shadow-soft);
}

.chart-card :deep(.ant-card-head) {
  min-height: 60px;
  background: #ffffff;
  border-bottom: 1px solid rgba(226, 232, 240, 0.9);
}

.chart-card :deep(.ant-card-body) {
  padding: 20px;
}

.card-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.chart-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
  margin-bottom: 18px;
  padding: 14px 16px;
  border-radius: 16px;
  background: #f8fafc;
  border: 1px solid rgba(226, 232, 240, 0.9);
}

.legend-list {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.legend-item {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: #475569;
  font-size: 12px;
  font-weight: 500;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
}

.legend-dot.active {
  background: #1677ff;
}

.legend-dot.inactive {
  background: #94a3b8;
}

.legend-dot.info {
  background: #0f172a;
}

.chart-hint {
  color: #64748b;
  font-size: 12px;
}

.chart-loading,
.empty-state {
  min-height: 320px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #8c8c8c;
}

.empty-icon {
  font-size: 40px;
  margin-bottom: 12px;
}

.chart-scroll {
  width: 100%;
  overflow-x: auto;
  overflow-y: hidden;
  padding-bottom: 8px;
}

.chart-scroll::-webkit-scrollbar {
  height: 8px;
}

.chart-scroll::-webkit-scrollbar-thumb {
  background: rgba(148, 163, 184, 0.55);
  border-radius: 999px;
}

.chart-scroll::-webkit-scrollbar-track {
  background: rgba(226, 232, 240, 0.6);
  border-radius: 999px;
}

.org-structure-board {
  min-width: 1120px;
  padding: 12px 4px 4px;
}

.root-block {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.root-block + .root-block {
  margin-top: 36px;
}

.root-node {
  width: min(280px, 100%);
  min-height: 112px;
  margin: 0 auto;
}

.second-row {
  position: relative;
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  align-items: start;
  column-gap: clamp(28px, 3vw, 56px);
  row-gap: 28px;
  margin-top: 30px;
  padding-top: 24px;
  width: 100%;
}

.second-row::before {
  content: '';
  position: absolute;
  top: 0;
  left: 50%;
  width: calc(100% - 120px);
  height: 1px;
  transform: translateX(-50%);
  background: rgba(203, 213, 225, 0.9);
}

.second-column {
  position: relative;
  padding-top: 18px;
  min-width: 0;
}

.second-column::before {
  content: '';
  position: absolute;
  top: -6px;
  left: 50%;
  width: 1px;
  height: 20px;
  transform: translateX(-50%);
  background: rgba(203, 213, 225, 0.9);
}

.second-node {
  width: 100%;
}

:deep(.tree-branch) {
  list-style: none;
  margin: 18px 0 0 0;
  width: 100%;
  padding: 0 0 0 40px;
  border-left: 1px solid rgba(148, 163, 184, 0.26);
}

:deep(.tree-item) {
  position: relative;
  width: 100%;
  padding-left: 28px;
  margin-bottom: 18px;
}

:deep(.tree-item:last-child) {
  margin-bottom: 0;
}

:deep(.tree-item::before) {
  content: '';
  position: absolute;
  top: 19px;
  left: -1px;
  width: 28px;
  height: 1px;
  background: rgba(148, 163, 184, 0.26);
}

:deep(.tree-level-4),
:deep(.tree-level-5),
:deep(.tree-level-6) {
  margin-top: 12px;
}

:deep(.tree-node) {
  width: 100%;
  text-align: left;
}

:deep(.tree-node .node-name) {
  justify-content: flex-start;
}

:deep(.org-node) {
  position: relative;
  display: inline-flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  gap: 10px;
  padding: 16px;
  border: 1px solid var(--border-soft);
  border-radius: 16px;
  background: #ffffff;
  color: #1f2937;
  box-shadow: 0 3px 10px rgba(15, 23, 42, 0.03);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
  text-align: left;
}

.node-main {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 12px 12px 14px;
  border-radius: 14px;
  background: #fafbfd;
  border: 1px solid rgba(226, 232, 240, 0.95);
}

:deep(.org-node:hover) {
  border-color: rgba(148, 163, 184, 0.32);
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.05);
}

:deep(.org-node.inactive) {
  border-color: rgba(148, 163, 184, 0.24);
  color: #64748b;
  background: #fcfcfd;
  box-shadow: none;
}

.root-node {
  background: #ffffff;
}

.second-node {
  background: #ffffff;
}

.node-head {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.node-state {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 999px;
  background: #f8fafc;
  border: 1px solid rgba(226, 232, 240, 0.9);
  color: #64748b;
  font-size: 11px;
  white-space: nowrap;
}

.node-state.inactive {
  color: #94a3b8;
}

.node-state-dot {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: #16a34a;
}

.node-state.inactive .node-state-dot {
  background: #94a3b8;
}

.node-name {
  display: inline-flex;
  align-items: center;
  justify-content: flex-start;
  flex: 1;
  min-width: 0;
  font-size: 15px;
  font-weight: 700;
  line-height: 1.5;
}

.node-action {
  appearance: none;
  border: none;
  background: transparent;
  padding: 0;
  margin: 0;
  text-align: left;
  cursor: pointer;
}

.node-action:hover {
  color: #1677ff;
}

.node-secondary {
  width: 100%;
  padding: 10px 12px;
  border-radius: 12px;
  background: #f8fafc;
  border: 1px solid rgba(226, 232, 240, 0.9);
  color: #475569;
  font-size: 12px;
  line-height: 1.4;
  min-height: 40px;
}

.manager-action {
  transition: border-color 0.2s ease, background-color 0.2s ease, color 0.2s ease;
}

.manager-action:hover:not(:disabled) {
  background: #f0f7ff;
  border-color: rgba(22, 119, 255, 0.22);
  color: #1677ff;
}

.manager-action:disabled {
  cursor: not-allowed;
  color: #94a3b8;
}

.detail-modal {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.detail-hero {
  padding: 22px 24px;
  border-radius: 20px;
  background: #ffffff;
  color: #1f2937;
  border: 1px solid var(--border-soft);
  box-shadow: var(--shadow-soft);
}

.detail-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.detail-header-side {
  display: flex;
  align-items: center;
  min-width: 120px;
}

.detail-header-side-right {
  justify-content: flex-end;
}

.detail-title-block {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.detail-status-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 999px;
  background: rgba(22, 163, 74, 0.08);
  color: #15803d;
  font-size: 12px;
  font-weight: 600;
  border: 1px solid rgba(22, 163, 74, 0.12);
}

.detail-status-chip.inactive {
  background: rgba(148, 163, 184, 0.12);
  color: #64748b;
  border-color: rgba(148, 163, 184, 0.18);
}

.detail-status-dot {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: currentColor;
}

.detail-name {
  font-size: 26px;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.35;
}

.detail-content-shell {
  padding: 16px;
  border-radius: 18px;
  background: #f8fafc;
  border: 1px solid rgba(226, 232, 240, 0.9);
}

.detail-overview-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.detail-overview-grid.simple {
  gap: 12px;
}

.overview-card {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-height: 116px;
  padding: 18px 20px;
  border-radius: 16px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.14);
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.03);
}

.overview-label {
  color: #8c8c8c;
  font-size: 12px;
}

.overview-card strong {
  color: #1f1f1f;
  font-size: 15px;
  line-height: 1.5;
  word-break: break-word;
}

@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .org-structure-board {
    min-width: 860px;
  }

  .second-row {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .detail-overview-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 768px) {
  .org-chart-page {
    padding: 16px;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
    padding: 20px;
  }

  .header-main {
    align-items: flex-start;
  }

  .header-text h2 {
    font-size: 24px;
  }

  .header-actions {
    width: 100%;
  }

  .header-actions :deep(.ant-btn) {
    flex: 1;
  }

  .summary-grid {
    grid-template-columns: 1fr;
  }

  .second-row {
    grid-template-columns: 1fr;
  }

  .chart-toolbar {
    align-items: flex-start;
  }

  .detail-hero {
    padding: 18px;
  }

  .detail-header {
    flex-direction: column;
    align-items: stretch;
  }

  .detail-header-side {
    min-width: 0;
  }

  .detail-header-side-right {
    justify-content: center;
  }

  .detail-title-block {
    order: -1;
  }

  .detail-name {
    font-size: 22px;
  }

  .detail-overview-grid {
    grid-template-columns: 1fr;
  }
}
</style>
