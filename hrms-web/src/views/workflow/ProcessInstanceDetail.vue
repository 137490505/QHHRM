<template>
  <div class="process-instance-detail">
    <a-page-header title="流程实例详情" class="detail-header" @back="handleBack">
      <template #extra>
        <a-space>
          <a-button @click="loadPageData">
            <ReloadOutlined />
            刷新
          </a-button>
        </a-space>
      </template>
    </a-page-header>

    <a-spin :spinning="loading">
      <template v-if="instance">
        <div class="detail-content">
          <div class="hero-card">
            <div class="hero-card__main">
              <div class="hero-card__eyebrow">流程实例查看</div>
              <div class="hero-card__title-row">
                <div>
                  <div class="hero-card__title">
                    {{ instance.title || processDefinition?.processName || instance.processCode || '流程实例详情' }}
                  </div>
                  <div class="hero-card__meta">
                    <span>实例编号：{{ instance.instanceNo || '-' }}</span>
                    <span>流程编码：{{ instance.processCode || '-' }}</span>
                    <span>业务单号：{{ instance.businessId || '-' }}</span>
                  </div>
                </div>
                <a-tag class="hero-card__status" :color="getStatusColor(instance.status)">
                  {{ getStatusText(instance.status) }}
                </a-tag>
              </div>
            </div>
            <div class="hero-metrics">
              <div class="hero-metric">
                <div class="hero-metric__label">当前节点</div>
                <div class="hero-metric__value">{{ instance.currentNodeName || '-' }}</div>
              </div>
              <div class="hero-metric">
                <div class="hero-metric__label">当前审批人</div>
                <div class="hero-metric__value">{{ instance.currentTodo?.assigneeName || instance.currentAssigneeName || '-' }}</div>
              </div>
              <div class="hero-metric">
                <div class="hero-metric__label">开始时间</div>
                <div class="hero-metric__value hero-metric__value--small">{{ formatDateTime(instance.startedTime) }}</div>
              </div>
              <div class="hero-metric">
                <div class="hero-metric__label">流程耗时</div>
                <div class="hero-metric__value">{{ formatDuration(instance.startedTime, instance.finishedTime) }}</div>
              </div>
            </div>
          </div>

          <a-card size="small" title="实例信息" class="summary-card">
            <div class="summary-grid">
              <div class="summary-item">
                <div class="summary-item__label">流程名称</div>
                <div class="summary-item__value">{{ processDefinition?.processName || instance.processCode || '-' }}</div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">业务类型</div>
                <div class="summary-item__value">{{ getBusinessTypeText(instance.businessType) }}</div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">发起人</div>
                <div class="summary-item__value">{{ instance.starterName || '-' }}</div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">当前状态</div>
                <div class="summary-item__value">
                  <a-tag :color="getStatusColor(instance.status)">
                    {{ getStatusText(instance.status) }}
                  </a-tag>
                </div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">当前节点</div>
                <div class="summary-item__value">{{ instance.currentNodeName || '-' }}</div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">当前审批人</div>
                <div class="summary-item__value">{{ instance.currentTodo?.assigneeName || instance.currentAssigneeName || '-' }}</div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">开始时间</div>
                <div class="summary-item__value">{{ formatDateTime(instance.startedTime) }}</div>
              </div>
              <div class="summary-item">
                <div class="summary-item__label">完成时间</div>
                <div class="summary-item__value">{{ formatDateTime(instance.finishedTime) }}</div>
              </div>
            </div>
          </a-card>

          <a-row :gutter="[16, 16]" class="instance-main-row">
            <a-col :xs="24" :xl="16" class="instance-main-col">
              <a-card size="small" title="流程图" class="diagram-card">
                <template #extra>
                  <div class="diagram-legend">
                    <span class="diagram-legend__item">
                      <span class="diagram-legend__dot diagram-legend__dot--success"></span>
                      已完成
                    </span>
                    <span class="diagram-legend__item">
                      <span class="diagram-legend__dot diagram-legend__dot--current"></span>
                      当前节点
                    </span>
                    <span class="diagram-legend__item">
                      <span class="diagram-legend__dot diagram-legend__dot--pending"></span>
                      待处理
                    </span>
                    <span class="diagram-legend__item">
                      <span class="diagram-legend__dot diagram-legend__dot--error"></span>
                      异常
                    </span>
                  </div>
                </template>
                <div ref="diagramRef" class="diagram-container"></div>
              </a-card>
            </a-col>

            <a-col :xs="24" :xl="8" class="instance-side-col">
              <div class="instance-side-stack">
                <a-card size="small" title="节点详情" class="side-card">
                  <template v-if="selectedNode">
                    <div class="node-detail">
                      <div class="node-detail__header">
                        <div>
                          <div class="node-detail__title">{{ selectedNode.nodeName || '-' }}</div>
                          <div class="node-detail__subtitle">{{ selectedNode.nodeId || '-' }}</div>
                        </div>
                        <a-tag :color="getNodeStatusColor(selectedNodeStatus)">
                          {{ getNodeStatusText(selectedNodeStatus) }}
                        </a-tag>
                      </div>
                      <div class="node-detail__grid">
                        <div class="node-detail__item">
                          <div class="node-detail__label">节点类型</div>
                          <div class="node-detail__value">{{ getNodeTypeText(selectedNode.nodeType) }}</div>
                        </div>
                        <div class="node-detail__item">
                          <div class="node-detail__label">审批类型</div>
                          <div class="node-detail__value">{{ getAssigneeTypeText(selectedNode.assigneeType) }}</div>
                        </div>
                        <div class="node-detail__item">
                          <div class="node-detail__label">审批对象</div>
                          <div class="node-detail__value">{{ getNodeAssigneeDisplay(selectedNode) }}</div>
                        </div>
                        <div class="node-detail__item">
                          <div class="node-detail__label">审批方式</div>
                          <div class="node-detail__value">{{ getMultiPersonTypeText(selectedNode.multiPersonType) }}</div>
                        </div>
                        <div class="node-detail__item">
                          <div class="node-detail__label">处理人</div>
                          <div class="node-detail__value">{{ selectedNodeHistory?.handlerName || selectedNode.assigneeName || '-' }}</div>
                        </div>
                        <div class="node-detail__item">
                          <div class="node-detail__label">到达时间</div>
                          <div class="node-detail__value">{{ formatDateTime(selectedNodeArriveHistory?.arrivedTime) }}</div>
                        </div>
                        <div class="node-detail__item">
                          <div class="node-detail__label">完成时间</div>
                          <div class="node-detail__value">{{ formatDateTime(selectedNodeHistory?.handledTime) }}</div>
                        </div>
                        <div class="node-detail__item node-detail__item--full">
                          <div class="node-detail__label">审批意见</div>
                          <div class="node-detail__value node-detail__remark">{{ selectedNodeHistory?.comment || '-' }}</div>
                        </div>
                      </div>
                    </div>
                  </template>
                  <a-empty v-else description="暂无节点信息" />
                </a-card>

                <a-card size="small" title="审批时间线" class="side-card">
                  <a-timeline>
                    <a-timeline-item
                      v-for="item in instance.history || []"
                      :key="item.id"
                      :color="getHistoryColor(item.actionResult)"
                    >
                      <div class="timeline-entry">
                        <div class="timeline-entry__header">
                          <div class="timeline-title">{{ item.nodeName || '-' }}</div>
                          <a-tag size="small" :color="getHistoryTagColor(item.actionResult)">
                            {{ getHistoryActionText(item.actionResult) }}
                          </a-tag>
                        </div>
                        <div class="timeline-meta">
                          {{ item.handlerName || item.handlerId || '系统' }} · {{ formatDateTime(item.handledTime || item.arrivedTime) }}
                        </div>
                        <div v-if="item.comment" class="timeline-comment">{{ item.comment }}</div>
                      </div>
                    </a-timeline-item>
                  </a-timeline>
                </a-card>
              </div>
            </a-col>
          </a-row>

          <a-row :gutter="[16, 16]" class="instance-footer-row">
            <a-col :xs="24" :xxl="17">
              <a-card size="small" title="业务回调日志" class="section-card section-card--full">
                <a-table
                  row-key="id"
                  size="small"
                  :columns="callbackColumns"
                  :data-source="callbackLogs"
                  :pagination="false"
                  :scroll="{ x: 860 }"
                  :locale="{ emptyText: '暂无回调日志' }"
                >
                  <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'status'">
                      <a-tag :color="getCallbackStatusColor(record.status)">
                        {{ getCallbackStatusText(record.status) }}
                      </a-tag>
                    </template>
                    <template v-else-if="column.key === 'createdTime'">
                      {{ formatDateTime(record.createdTime) }}
                    </template>
                    <template v-else-if="column.key === 'action'">
                      <a-space>
                        <a-button type="link" size="small" @click="showLogDetail(record)">详情</a-button>
                        <a-button
                          v-if="record.status === 'Failed'"
                          type="link"
                          size="small"
                          @click="handleRetryCallback(record)"
                        >
                          重试
                        </a-button>
                      </a-space>
                    </template>
                  </template>
                </a-table>
              </a-card>
            </a-col>

            <a-col :xs="24" :xxl="7">
              <a-card size="small" title="扩展数据" class="section-card section-card--full">
                <pre class="json-block">{{ formatJson(instance.extData) }}</pre>
              </a-card>
            </a-col>
          </a-row>
        </div>
      </template>

      <a-empty v-else description="未找到流程实例" />
    </a-spin>

    <a-modal v-model:open="logDetailVisible" title="回调日志详情" width="800px" :footer="null">
      <template v-if="currentLog">
        <a-descriptions bordered :column="1" size="small">
          <a-descriptions-item label="请求地址">{{ currentLog.callbackUrl }}</a-descriptions-item>
          <a-descriptions-item label="状态码">{{ currentLog.statusCode }}</a-descriptions-item>
          <a-descriptions-item label="状态">{{ currentLog.status }}</a-descriptions-item>
          <a-descriptions-item label="请求时间">{{ formatDateTime(currentLog.createdTime) }}</a-descriptions-item>
          <a-descriptions-item label="错误信息">{{ currentLog.errorMessage || '-' }}</a-descriptions-item>
        </a-descriptions>
        <a-card size="small" title="请求载荷" class="log-detail-card">
          <pre class="json-block">{{ formatJson(currentLog.payload) }}</pre>
        </a-card>
        <a-card size="small" title="响应内容" class="log-detail-card">
          <pre class="json-block">{{ formatJson(currentLog.response) || currentLog.response }}</pre>
        </a-card>
      </template>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import { ReloadOutlined } from '@ant-design/icons-vue'
import Viewer from 'bpmn-js/lib/Viewer'
import 'bpmn-js/dist/assets/diagram-js.css'
import 'bpmn-js/dist/assets/bpmn-js.css'
import 'bpmn-js/dist/assets/bpmn-font/css/bpmn-embedded.css'
import { processCenterApi } from '../../api'
import { buildDiagramXmlFromDefinition, buildFlowRecords } from '../../utils/processDiagram'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const diagramRef = ref(null)
const instance = ref(null)
const processDefinition = ref(null)
const callbackLogs = ref([])
const logDetailVisible = ref(false)
const currentLog = ref(null)
const selectedNodeId = ref('')

let viewer = null
let diagramResizeObserver = null
let fitViewportFrameId = 0
let nodeInfoOverlayIds = []

const callbackColumns = [
  { title: '地址', dataIndex: 'callbackUrl', key: 'callbackUrl', ellipsis: true },
  { title: '状态码', dataIndex: 'statusCode', key: 'statusCode', width: 90 },
  { title: '结果', key: 'status', width: 100 },
  { title: '时间', key: 'createdTime', width: 180 },
  { title: '操作', key: 'action', width: 120 }
]

const orderedNodes = computed(() => [...(processDefinition.value?.nodes || [])]
  .sort((left, right) => (left.sortOrder || 0) - (right.sortOrder || 0)))

const nodeMap = computed(() => orderedNodes.value.reduce((result, node) => {
  result[node.nodeId] = node
  return result
}, {}))

const historyList = computed(() => [...(instance.value?.history || [])].sort((left, right) => (left.id || 0) - (right.id || 0)))

const arriveHistoryMap = computed(() => {
  const result = {}
  historyList.value.forEach((item) => {
    if (item.action === 'Arrive') {
      result[item.nodeId] = item
    }
  })
  return result
})

const latestHistoryMap = computed(() => {
  const result = {}
  historyList.value.forEach((item) => {
    result[item.nodeId] = item
  })
  return result
})

const nodeStatusMap = computed(() => {
  const result = {}
  orderedNodes.value.forEach((node) => {
    result[node.nodeId] = 'Pending'
  })

  historyList.value.forEach((item) => {
    if (!item?.nodeId) {
      return
    }
    if (item.actionResult === 'Completed') {
      result[item.nodeId] = 'Completed'
    } else if (['Rejected', 'Returned'].includes(item.actionResult)) {
      result[item.nodeId] = 'Rejected'
    } else if (['Terminated', 'Withdrawn'].includes(item.actionResult)) {
      result[item.nodeId] = 'Terminated'
    } else if (item.actionResult === 'Pending' && !result[item.nodeId]) {
      result[item.nodeId] = 'Pending'
    }
  })

  const currentNodeId = instance.value?.currentNodeId
  if (currentNodeId) {
    if (instance.value?.status === 'Running') {
      result[currentNodeId] = 'Current'
    } else if (instance.value?.status === 'Rejected') {
      result[currentNodeId] = 'Rejected'
    } else if (['Terminated', 'Withdrawn'].includes(instance.value?.status)) {
      result[currentNodeId] = 'Terminated'
    }
  }

  return result
})

const flowRecords = computed(() => buildFlowRecords(orderedNodes.value))

const flowIdMap = computed(() => flowRecords.value.reduce((result, flow) => {
  result[`${flow.sourceRef}->${flow.targetRef}`] = flow.id
  return result
}, {}))

const flowStatusMap = computed(() => {
  const result = {}
  const transitions = []
  let previousArriveNodeId = null

  historyList.value
    .filter((item) => item.action === 'Arrive')
    .forEach((item) => {
      if (previousArriveNodeId && previousArriveNodeId !== item.nodeId) {
        transitions.push(`${previousArriveNodeId}->${item.nodeId}`)
      }
      previousArriveNodeId = item.nodeId
    })

  transitions.forEach((key, index) => {
    const flowId = flowIdMap.value[key]
    if (!flowId) {
      return
    }
    const isLast = index === transitions.length - 1
    result[flowId] = isLast && instance.value?.status === 'Running' ? 'Current' : 'Completed'
  })

  if (['Rejected', 'Terminated', 'Withdrawn'].includes(instance.value?.status)) {
    const currentNodeId = instance.value?.currentNodeId
    const previousKey = transitions[transitions.length - 1]
    const flowId = flowIdMap.value[previousKey]
    if (currentNodeId && flowId) {
      result[flowId] = instance.value.status === 'Rejected' ? 'Rejected' : 'Terminated'
    }
  }

  return result
})

const selectedNode = computed(() => nodeMap.value[selectedNodeId.value] || null)
const selectedNodeHistory = computed(() => latestHistoryMap.value[selectedNodeId.value] || null)
const selectedNodeArriveHistory = computed(() => arriveHistoryMap.value[selectedNodeId.value] || null)
const selectedNodeStatus = computed(() => nodeStatusMap.value[selectedNodeId.value] || 'Pending')

const handleBack = () => {
  router.back()
}

const getAssigneeTypeText = (assigneeType) => {
  const map = {
    Initiator: '发起人',
    User: '指定人员',
    Role: '角色',
    Department: '部门',
    Post: '岗位'
  }
  return map[assigneeType] || assigneeType || '-'
}

const getMultiPersonTypeText = (multiPersonType) => {
  const map = {
    Any: '任一审批人',
    All: '全部审批人'
  }
  return map[multiPersonType] || '单人审批'
}

const getNodeAssigneeLabel = (node) => {
  const assigneeType = node?.assigneeType
  if (assigneeType === 'Department') {
    return '部门'
  }
  if (assigneeType === 'Post') {
    return '岗位'
  }
  if (assigneeType === 'Role') {
    return '角色'
  }
  return '人员'
}

const getNodeAssigneeDisplay = (node) => {
  if (!node) {
    return '-'
  }

  if (node.assigneeType === 'Initiator') {
    return '发起人'
  }

  return node.assigneeName || node.assigneeId || '-'
}

const getNodeInfoText = (node) => {
  if (!node) {
    return ''
  }

  if (node.nodeType === 'StartEvent' || node.nodeType === 'EndEvent') {
    return getNodeTypeText(node.nodeType)
  }

  if (['ExclusiveGateway', 'ParallelGateway', 'InclusiveGateway'].includes(node.nodeType)) {
    return getNodeTypeText(node.nodeType)
  }

  if (node.nodeType === 'ServiceTask') {
    return '服务任务'
  }

  const assigneeText = `${getNodeAssigneeLabel(node)}: ${getNodeAssigneeDisplay(node)}`
  if (!node.multiPersonType) {
    return assigneeText
  }

  return `${assigneeText}\n${getMultiPersonTypeText(node.multiPersonType)}`
}

const getNodeOverlayModel = (node) => {
  if (!node) {
    return null
  }

  return {
    title: node.nodeName || node.nodeId,
    typeText: getNodeTypeText(node.nodeType),
    assigneeLabel: getNodeAssigneeLabel(node),
    assigneeValue: getNodeAssigneeDisplay(node),
    assigneeType: node.assigneeType || 'User',
    modeText: getMultiPersonTypeText(node.multiPersonType),
    nodeType: node.nodeType
  }
}

const buildDiagramDefinition = (definition) => {
  if (!definition?.nodes?.length) {
    return definition
  }

  return {
    ...definition,
    nodes: definition.nodes.map((node) => {
      const infoText = getNodeInfoText(node)
      if (!infoText) {
        return node
      }

      return {
        ...node,
        nodeName: '',
        diagramWidth: 168,
        diagramHeight: 104
      }
    })
  }
}

const scheduleFitViewport = () => {
  if (!viewer) {
    return
  }

  if (fitViewportFrameId) {
    cancelAnimationFrame(fitViewportFrameId)
  }

  fitViewportFrameId = requestAnimationFrame(() => {
    fitViewportFrameId = requestAnimationFrame(() => {
      fitViewportFrameId = 0
      if (!viewer || !diagramRef.value) {
        return
      }

      const canvas = viewer.get('canvas')
      canvas.resized()
      canvas.zoom('fit-viewport', 'auto')
      renderNodeInfoOverlays()
    })
  })
}

const clearNodeInfoOverlays = () => {
  if (!viewer || !nodeInfoOverlayIds.length) {
    return
  }

  const overlays = viewer.get('overlays')
  nodeInfoOverlayIds.forEach((id) => overlays.remove(id))
  nodeInfoOverlayIds = []
}

const renderNodeInfoOverlays = () => {
  if (!viewer) {
    return
  }

  clearNodeInfoOverlays()

  const overlays = viewer.get('overlays')
  const elementRegistry = viewer.get('elementRegistry')
  orderedNodes.value.forEach((node) => {
    const overlayModel = getNodeOverlayModel(node)
    if (!overlayModel) {
      return
    }

    const shape = elementRegistry.get(node.nodeId)

    const wrapper = document.createElement('div')
    wrapper.className = 'node-card-overlay'
    const overlayWidth = Math.max(Number(shape?.width) || 0, 96)
    wrapper.style.width = `${overlayWidth}px`
    wrapper.style.maxWidth = `${overlayWidth}px`

    const title = document.createElement('div')
    title.className = 'node-card-overlay__title'
    title.textContent = overlayModel.title
    wrapper.appendChild(title)

    const meta = document.createElement('div')
    meta.className = 'node-card-overlay__meta'

    const typeTag = document.createElement('span')
    typeTag.className = 'node-card-overlay__tag node-card-overlay__tag--type'
    typeTag.textContent = overlayModel.typeText
    meta.appendChild(typeTag)

    if (overlayModel.nodeType === 'UserTask') {
      const assigneeTag = document.createElement('span')
      assigneeTag.className = `node-card-overlay__tag node-card-overlay__tag--${String(overlayModel.assigneeType).toLowerCase()}`
      assigneeTag.textContent = `${overlayModel.assigneeLabel}: ${overlayModel.assigneeValue}`
      meta.appendChild(assigneeTag)
    }

    if (overlayModel.nodeType === 'UserTask' && overlayModel.modeText && overlayModel.modeText !== '单人审批') {
      const modeTag = document.createElement('span')
      modeTag.className = 'node-card-overlay__tag node-card-overlay__tag--mode'
      modeTag.textContent = overlayModel.modeText
      meta.appendChild(modeTag)
    }

    wrapper.appendChild(meta)

    const overlayId = overlays.add(node.nodeId, {
      position: {
        bottom: 0,
        left: 0
      },
      html: wrapper
    })

    nodeInfoOverlayIds.push(overlayId)
  })
}

const ensureViewer = () => {
  if (viewer || !diagramRef.value) {
    return
  }

  viewer = new Viewer({
    container: diagramRef.value
  })

  const eventBus = viewer.get('eventBus')
  eventBus.on('element.click', ({ element }) => {
    const elementId = element?.businessObject?.id || element?.id
    if (!elementId || !nodeMap.value[elementId]) {
      return
    }
    selectedNodeId.value = elementId
  })

  diagramResizeObserver = new ResizeObserver(() => {
    scheduleFitViewport()
  })
  diagramResizeObserver.observe(diagramRef.value)
}

const clearMarkers = () => {
  if (!viewer) {
    return
  }

  const canvas = viewer.get('canvas')
  orderedNodes.value.forEach((node) => {
    ;['node-completed', 'node-current', 'node-pending', 'node-rejected', 'node-terminated']
      .forEach((marker) => canvas.removeMarker(node.nodeId, marker))
  })
  flowRecords.value.forEach((flow) => {
    ;['flow-completed', 'flow-current', 'flow-rejected', 'flow-terminated']
      .forEach((marker) => canvas.removeMarker(flow.id, marker))
  })
}

const applyMarkers = () => {
  if (!viewer) {
    return
  }

  clearMarkers()

  const canvas = viewer.get('canvas')
  orderedNodes.value.forEach((node) => {
    const status = nodeStatusMap.value[node.nodeId]
    if (!status) {
      return
    }
    const marker = {
      Completed: 'node-completed',
      Current: 'node-current',
      Pending: 'node-pending',
      Rejected: 'node-rejected',
      Terminated: 'node-terminated'
    }[status]
    if (marker) {
      canvas.addMarker(node.nodeId, marker)
    }
  })

  flowRecords.value.forEach((flow) => {
    const status = flowStatusMap.value[flow.id]
    const marker = {
      Completed: 'flow-completed',
      Current: 'flow-current',
      Rejected: 'flow-rejected',
      Terminated: 'flow-terminated'
    }[status]
    if (marker) {
      canvas.addMarker(flow.id, marker)
    }
  })
}

const renderDiagram = async () => {
  if (!instance.value || !processDefinition.value) {
    return
  }

  await nextTick()
  ensureViewer()

  const xml = buildDiagramXmlFromDefinition(buildDiagramDefinition(processDefinition.value))
  await viewer.importXML(xml)
  scheduleFitViewport()
  applyMarkers()
  renderNodeInfoOverlays()
}

const loadDefinition = async () => {
  if (!instance.value?.processCode) {
    processDefinition.value = null
    return
  }

  const definitions = await processCenterApi.getDefinitions({
    processCode: instance.value.processCode
  })

  processDefinition.value = (definitions || []).find((item) =>
    item.processCode === instance.value.processCode && item.versionNo === instance.value.versionNo
  ) || null
}

const loadPageData = async () => {
  const id = Number(route.params.id)
  if (!Number.isFinite(id) || id <= 0) {
    instance.value = null
    processDefinition.value = null
    callbackLogs.value = []
    return
  }

  loading.value = true
  try {
    const detail = await processCenterApi.getInstance(id)
    instance.value = detail
    selectedNodeId.value = detail?.currentNodeId || detail?.history?.[detail.history.length - 1]?.nodeId || ''
    await Promise.all([
      loadDefinition(),
      processCenterApi.getCallbackLogs(id).then((logs) => {
        callbackLogs.value = logs || []
      })
    ])
    await renderDiagram()
  } catch (error) {
    instance.value = null
    processDefinition.value = null
    callbackLogs.value = []
    message.error(error?.response?.data?.message || '加载流程实例失败')
  } finally {
    loading.value = false
  }
}

const showLogDetail = (log) => {
  currentLog.value = log
  logDetailVisible.value = true
}

const handleRetryCallback = async (log) => {
  try {
    await processCenterApi.retryCallback(log.processInstanceId, log.id)
    message.success('重试请求已发送')
    callbackLogs.value = await processCenterApi.getCallbackLogs(log.processInstanceId)
  } catch (error) {
    message.error(error?.response?.data?.message || '重试失败')
  }
}

const getStatusText = (status) => {
  const map = {
    Running: '运行中',
    Completed: '已完成',
    Rejected: '已驳回',
    Terminated: '已终止',
    Withdrawn: '已撤回'
  }
  return map[status] || status || '-'
}

const getStatusColor = (status) => {
  const map = {
    Running: 'processing',
    Completed: 'success',
    Rejected: 'error',
    Terminated: 'default',
    Withdrawn: 'warning'
  }
  return map[status] || 'default'
}

const getBusinessTypeText = (businessType) => {
  const map = {
    Leave: '请假审批',
    Payroll: '薪资核算',
    Expense: '费用申请',
    Purchase: '采购申请',
    Reimbursement: '报销申请'
  }
  return map[businessType] || businessType || '-'
}

const getNodeStatusText = (status) => {
  const map = {
    Completed: '已完成',
    Current: '当前处理',
    Pending: '待处理',
    Rejected: '已驳回',
    Terminated: '已终止'
  }
  return map[status] || status || '-'
}

const getNodeStatusColor = (status) => {
  const map = {
    Completed: 'success',
    Current: 'processing',
    Pending: 'default',
    Rejected: 'error',
    Terminated: 'default'
  }
  return map[status] || 'default'
}

const getNodeTypeText = (nodeType) => {
  const map = {
    StartEvent: '开始事件',
    EndEvent: '结束事件',
    UserTask: '用户任务',
    ServiceTask: '服务任务',
    ExclusiveGateway: '排他网关',
    ParallelGateway: '并行网关',
    InclusiveGateway: '包容网关'
  }
  return map[nodeType] || nodeType || '-'
}

const getHistoryActionText = (actionResult) => {
  const map = {
    Pending: '到达待处理',
    Completed: '审批通过',
    Rejected: '已驳回',
    Terminated: '已终止',
    Withdrawn: '已撤回',
    Returned: '已退回',
    Transferred: '已转派'
  }
  return map[actionResult] || actionResult || '-'
}

const getHistoryColor = (actionResult) => {
  const map = {
    Pending: 'blue',
    Completed: 'green',
    Rejected: 'red',
    Terminated: 'gray',
    Withdrawn: 'orange',
    Returned: 'orange',
    Transferred: 'blue'
  }
  return map[actionResult] || 'blue'
}

const getHistoryTagColor = (actionResult) => {
  const map = {
    Pending: 'processing',
    Completed: 'success',
    Rejected: 'error',
    Terminated: 'default',
    Withdrawn: 'warning',
    Returned: 'warning',
    Transferred: 'processing'
  }
  return map[actionResult] || 'default'
}

const getCallbackStatusText = (status) => {
  const map = {
    Success: '成功',
    Failed: '失败',
    Pending: '待执行'
  }
  return map[status] || status || '-'
}

const getCallbackStatusColor = (status) => {
  const map = {
    Success: 'success',
    Failed: 'error',
    Pending: 'processing'
  }
  return map[status] || 'default'
}

const formatDateTime = (value) => {
  if (!value) {
    return '-'
  }
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString()
}

const formatDuration = (start, end) => {
  if (!start) {
    return '-'
  }

  const startDate = new Date(start)
  const endDate = end ? new Date(end) : new Date()
  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime())) {
    return '-'
  }

  const diff = Math.max(0, endDate.getTime() - startDate.getTime())
  const minutes = Math.floor(diff / 60000)
  if (minutes < 60) {
    return `${minutes} 分钟`
  }
  const hours = Math.floor(minutes / 60)
  const remainMinutes = minutes % 60
  if (hours < 24) {
    return `${hours} 小时 ${remainMinutes} 分钟`
  }
  const days = Math.floor(hours / 24)
  const remainHours = hours % 24
  return `${days} 天 ${remainHours} 小时`
}

const formatJson = (value) => {
  if (!value) {
    return '{}'
  }
  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }
  return JSON.stringify(value, null, 2)
}

watch(() => route.params.id, loadPageData)

onMounted(loadPageData)

onBeforeUnmount(() => {
  if (fitViewportFrameId) {
    cancelAnimationFrame(fitViewportFrameId)
    fitViewportFrameId = 0
  }
  clearNodeInfoOverlays()
  if (diagramResizeObserver) {
    diagramResizeObserver.disconnect()
    diagramResizeObserver = null
  }
  if (viewer) {
    viewer.destroy()
    viewer = null
  }
})
</script>

<style scoped>
.process-instance-detail {
  padding: 8px 0 24px;
}

.detail-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding-bottom: 24px;
}

.detail-header {
  margin-bottom: 16px;
  padding: 0;
}

.hero-card {
  padding: 24px;
  border-radius: 20px;
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.22), rgba(255, 255, 255, 0) 28%),
    linear-gradient(135deg, #1677ff 0%, #4096ff 48%, #69b1ff 100%);
  color: #fff;
  box-shadow: 0 16px 40px rgba(22, 119, 255, 0.18);
}

.hero-card__main {
  margin-bottom: 20px;
}

.hero-card__eyebrow {
  font-size: 13px;
  letter-spacing: 0.08em;
  opacity: 0.88;
  margin-bottom: 12px;
}

.hero-card__title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.hero-card__title {
  font-size: 28px;
  line-height: 1.3;
  font-weight: 700;
}

.hero-card__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 18px;
  margin-top: 10px;
  font-size: 13px;
  opacity: 0.92;
}

.hero-card__status {
  margin-inline-end: 0;
  padding: 6px 12px;
  border-radius: 999px;
  font-size: 13px;
  font-weight: 600;
}

.hero-metrics {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.hero-metric {
  padding: 16px 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.14);
  border: 1px solid rgba(255, 255, 255, 0.18);
  backdrop-filter: blur(6px);
}

.hero-metric__label {
  font-size: 12px;
  opacity: 0.82;
}

.hero-metric__value {
  margin-top: 8px;
  font-size: 18px;
  line-height: 1.4;
  font-weight: 600;
  word-break: break-word;
}

.hero-metric__value--small {
  font-size: 15px;
}

.summary-card,
.diagram-card,
.side-card,
.section-card {
  margin-bottom: 0;
  border-radius: 18px;
  border: 1px solid #edf2ff;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.04);
  overflow: hidden;
}

.summary-card :deep(.ant-card-head),
.diagram-card :deep(.ant-card-head),
.side-card :deep(.ant-card-head),
.section-card :deep(.ant-card-head) {
  min-height: 54px;
  padding: 0 18px;
  border-bottom: 1px solid #f0f4fa;
}

.summary-card :deep(.ant-card-head-title),
.diagram-card :deep(.ant-card-head-title),
.side-card :deep(.ant-card-head-title),
.section-card :deep(.ant-card-head-title) {
  font-weight: 700;
  color: #1f1f1f;
}

.summary-card :deep(.ant-card-body),
.diagram-card :deep(.ant-card-body),
.side-card :deep(.ant-card-body),
.section-card :deep(.ant-card-body) {
  padding: 18px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 12px;
}

.summary-item {
  min-height: 88px;
  padding: 16px;
  border-radius: 14px;
  background: linear-gradient(180deg, #ffffff 0%, #fafcff 100%);
  border: 1px solid #edf2ff;
}

.summary-item__label {
  font-size: 12px;
  color: #8c8c8c;
  margin-bottom: 8px;
}

.summary-item__value {
  font-size: 15px;
  line-height: 1.6;
  font-weight: 600;
  color: #262626;
  word-break: break-word;
}

.diagram-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 14px;
  font-size: 12px;
  color: #595959;
}

.instance-main-row,
.instance-footer-row {
  align-items: stretch;
}

.instance-main-col,
.instance-side-col {
  display: flex;
  min-width: 0;
}

.diagram-card,
.section-card--full {
  width: 100%;
}

.instance-side-stack {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
}

.diagram-legend__item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.diagram-legend__dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.diagram-legend__dot--success {
  background: #52c41a;
}

.diagram-legend__dot--current {
  background: #1677ff;
}

.diagram-legend__dot--pending {
  background: #bfbfbf;
}

.diagram-legend__dot--error {
  background: #ff4d4f;
}

.diagram-container {
  height: min(68vh, 720px);
  min-height: 420px;
  background: linear-gradient(180deg, #fcfcfd 0%, #f6f9fc 100%);
  border: 1px solid #edf2f7;
  border-radius: 14px;
  overflow: hidden;
}

.diagram-card :deep(.djs-container) {
  background: linear-gradient(180deg, #fcfcfc 0%, #f7f9fc 100%);
  cursor: default;
}

.diagram-card :deep(.djs-container .viewport) {
  cursor: default;
}

.diagram-card :deep(.bjs-powered-by),
.diagram-card :deep(.bjs-powered-by-lightbox) {
  display: none !important;
}

.diagram-card :deep(.node-card-overlay) {
  padding: 4px 6px 5px;
  border-radius: 0 0 8px 8px;
  background: rgba(255, 255, 255, 0.94);
  border-top: 1px solid rgba(22, 119, 255, 0.14);
  color: #1f1f1f;
  pointer-events: none;
  box-sizing: border-box;
  overflow: hidden;
}

.diagram-card :deep(.node-card-overlay__title) {
  font-size: 11px;
  line-height: 1.25;
  font-weight: 600;
  color: #262626;
  margin-bottom: 3px;
  word-break: break-word;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.diagram-card :deep(.node-card-overlay__meta) {
  display: flex;
  flex-wrap: wrap;
  gap: 3px;
}

.diagram-card :deep(.node-card-overlay__tag) {
  display: inline-flex;
  align-items: center;
  max-width: 100%;
  padding: 1px 5px;
  border-radius: 999px;
  font-size: 9px;
  line-height: 1.2;
  white-space: normal;
  word-break: break-word;
  border: 1px solid transparent;
}

.diagram-card :deep(.node-card-overlay__tag--type) {
  background: rgba(22, 119, 255, 0.08);
  color: #1677ff;
  border-color: rgba(22, 119, 255, 0.12);
}

.diagram-card :deep(.node-card-overlay__tag--user) {
  background: rgba(230, 244, 255, 0.9);
  color: #0958d9;
  border-color: rgba(9, 88, 217, 0.1);
}

.diagram-card :deep(.node-card-overlay__tag--role) {
  background: rgba(249, 240, 255, 0.9);
  color: #722ed1;
  border-color: rgba(114, 46, 209, 0.1);
}

.diagram-card :deep(.node-card-overlay__tag--department) {
  background: rgba(246, 255, 237, 0.92);
  color: #389e0d;
  border-color: rgba(56, 158, 13, 0.1);
}

.diagram-card :deep(.node-card-overlay__tag--post) {
  background: rgba(255, 247, 230, 0.92);
  color: #d46b08;
  border-color: rgba(212, 107, 8, 0.1);
}

.diagram-card :deep(.node-card-overlay__tag--initiator) {
  background: rgba(255, 241, 240, 0.92);
  color: #cf1322;
  border-color: rgba(207, 19, 34, 0.1);
}

.diagram-card :deep(.node-card-overlay__tag--mode) {
  background: rgba(0, 0, 0, 0.04);
  color: #595959;
  border-color: rgba(0, 0, 0, 0.06);
}

.diagram-card :deep(.node-completed .djs-visual > :nth-child(1)) {
  fill: #f6ffed !important;
  stroke: #52c41a !important;
  stroke-width: 2px !important;
}

.diagram-card :deep(.djs-label) {
  font-size: 12px;
  line-height: 1.35;
}

.diagram-card :deep(.node-current .djs-visual > :nth-child(1)) {
  fill: #e6f4ff !important;
  stroke: #1677ff !important;
  stroke-width: 3px !important;
}

.diagram-card :deep(.node-pending .djs-visual > :nth-child(1)) {
  fill: #fafafa !important;
  stroke: #bfbfbf !important;
}

.diagram-card :deep(.node-rejected .djs-visual > :nth-child(1)) {
  fill: #fff2f0 !important;
  stroke: #ff4d4f !important;
  stroke-width: 2px !important;
}

.diagram-card :deep(.node-terminated .djs-visual > :nth-child(1)) {
  fill: #f5f5f5 !important;
  stroke: #8c8c8c !important;
  stroke-width: 2px !important;
}

.diagram-card :deep(.flow-completed .djs-visual path) {
  stroke: #52c41a !important;
  stroke-width: 3px !important;
}

.diagram-card :deep(.flow-current .djs-visual path) {
  stroke: #1677ff !important;
  stroke-width: 3px !important;
}

.diagram-card :deep(.flow-rejected .djs-visual path) {
  stroke: #ff4d4f !important;
  stroke-width: 3px !important;
}

.diagram-card :deep(.flow-terminated .djs-visual path) {
  stroke: #8c8c8c !important;
  stroke-width: 3px !important;
}

.node-detail {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.node-detail__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 16px;
  border-radius: 16px;
  background: linear-gradient(180deg, #f8fbff 0%, #eef5ff 100%);
  border: 1px solid #dbe8ff;
}

.node-detail__title {
  font-size: 18px;
  line-height: 1.4;
  font-weight: 700;
  color: #1f1f1f;
}

.node-detail__subtitle {
  margin-top: 6px;
  font-size: 12px;
  color: #8c8c8c;
  word-break: break-all;
}

.node-detail__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.node-detail__item {
  padding: 14px 14px 12px;
  border-radius: 14px;
  background: #fafcff;
  border: 1px solid #eef2f6;
}

.node-detail__item--full {
  grid-column: 1 / -1;
}

.node-detail__label {
  font-size: 12px;
  color: #8c8c8c;
  margin-bottom: 8px;
}

.node-detail__value {
  font-size: 14px;
  line-height: 1.6;
  color: #262626;
  font-weight: 600;
  word-break: break-word;
}

.node-detail__remark {
  min-height: 44px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #fff;
  border: 1px dashed #d9d9d9;
}

.timeline-entry {
  padding: 12px 14px;
  border-radius: 14px;
  background: linear-gradient(180deg, #ffffff 0%, #fafcff 100%);
  border: 1px solid #edf2ff;
}

.timeline-entry__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.json-block {
  min-height: 260px;
  background: linear-gradient(180deg, #0f172a 0%, #111827 100%);
  color: #dbeafe;
  padding: 14px;
  border-radius: 12px;
  overflow-x: auto;
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
}

.timeline-title {
  font-weight: 600;
  color: #262626;
  line-height: 1.5;
}

.timeline-meta {
  margin-top: 4px;
  color: #8c8c8c;
  font-size: 12px;
}

.timeline-comment {
  margin-top: 8px;
  padding: 8px;
  background: #fff;
  border: 1px dashed #d9d9d9;
  border-radius: 6px;
}

.log-detail-card {
  margin-top: 16px;
}

@media (max-width: 1400px) {
  .diagram-container {
    height: 560px;
  }

  .json-block {
    min-height: 220px;
  }
}

@media (max-width: 768px) {
  .process-instance-detail {
    padding-top: 0;
  }

  .hero-card {
    padding: 18px;
    border-radius: 16px;
  }

  .hero-card__title-row {
    flex-direction: column;
  }

  .hero-card__title {
    font-size: 22px;
  }

  .node-detail__grid {
    grid-template-columns: 1fr;
  }

  .diagram-container {
    height: 440px;
    min-height: 360px;
  }

  .timeline-entry__header {
    flex-direction: column;
  }

  .summary-card :deep(.ant-card-body),
  .diagram-card :deep(.ant-card-body),
  .side-card :deep(.ant-card-body),
  .section-card :deep(.ant-card-body) {
    padding: 14px;
  }
}
</style>
