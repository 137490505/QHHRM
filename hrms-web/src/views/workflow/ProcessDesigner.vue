<template>
  <div class="process-designer">
    <a-spin :spinning="loading">
      <div class="designer-header">
        <a-space>
          <a-button @click="handleBack">
            <ArrowLeftOutlined /> 返回
          </a-button>
          <a-divider type="vertical" />
          <a-input
            v-model:value="formData.processName"
            placeholder="流程名称"
            style="width: 200px"
          />
          <a-input
            v-model:value="formData.processCode"
            placeholder="流程编码"
            style="width: 180px"
          />
          <a-select
            v-model:value="formData.businessType"
            placeholder="业务类型"
            style="width: 160px"
            allow-clear
          >
            <a-select-option value="Leave">请假审批</a-select-option>
            <a-select-option value="Payroll">薪资核算</a-select-option>
            <a-select-option value="Expense">费用申请</a-select-option>
            <a-select-option value="Purchase">采购申请</a-select-option>
            <a-select-option value="Reimbursement">报销申请</a-select-option>
          </a-select>
          <a-input-number
            v-model:value="formData.versionNo"
            placeholder="版本号"
            style="width: 100px"
            :min="1"
            :disabled="!!definitionId"
          />
        </a-space>
        <a-space>
          <a-button @click="handleUndo" :disabled="!canUndo">
            <UndoOutlined /> 撤销
          </a-button>
          <a-button @click="handleRedo" :disabled="!canRedo">
            <RedoOutlined /> 重做
          </a-button>
          <a-divider type="vertical" />
          <a-button @click="handleZoomIn">
            <ZoomInOutlined />
          </a-button>
          <a-button @click="handleZoomOut">
            <ZoomOutOutlined />
          </a-button>
          <a-button @click="handleZoomReset">
            <FullscreenOutlined /> 适应画布
          </a-button>
          <a-button @click="handleAutoBeautify">
            自动美化
          </a-button>
          <a-divider type="vertical" />
          <a-button @click="handleSave" :loading="saving" :disabled="!hasChanges">
            <SaveOutlined /> 保存草稿
          </a-button>
          <a-button type="primary" @click="handlePublish" :loading="publishing">
            <CloudUploadOutlined /> 发布
          </a-button>
        </a-space>
      </div>

      <div class="designer-body">
        <div class="palette-panel">
          <div class="palette-title">基础节点</div>
          <div class="palette-items">
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'startEvent')">
              <PlayCircleOutlined class="palette-icon start-icon" />
              <span>开始事件</span>
            </div>
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'endEvent')">
              <CheckCircleOutlined class="palette-icon end-icon" />
              <span>结束事件</span>
            </div>
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'userTask')">
              <ProfileOutlined class="palette-icon task-icon" />
              <span>用户任务</span>
            </div>
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'serviceTask')">
              <ApiOutlined class="palette-icon service-icon" />
              <span>服务任务</span>
            </div>
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'exclusiveGateway')">
              <BranchesOutlined class="palette-icon gateway-icon" />
              <span>排他网关</span>
            </div>
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'parallelGateway')">
              <ApartmentOutlined class="palette-icon gateway-icon" />
              <span>并行网关</span>
            </div>
            <div class="palette-item" draggable @dragstart="onDragStart($event, 'inclusiveGateway')">
              <ClusterOutlined class="palette-icon gateway-icon" />
              <span>包容网关</span>
            </div>
          </div>
          <div class="palette-title" style="margin-top: 16px;">常用流程模板</div>
          <div class="palette-items">
            <div class="palette-item template" @click="handleLoadTemplate('simple')">
              <span>简单审批流</span>
            </div>
            <div class="palette-item template" @click="handleLoadTemplate('approval')">
              <span>多级审批流</span>
            </div>
            <div class="palette-item template" @click="handleLoadTemplate('branch')">
              <span>条件分支流</span>
            </div>
            <div class="palette-item template" @click="handleLoadTemplate('expense')">
              <span>费用申请流</span>
            </div>
          </div>
        </div>

        <div class="canvas-panel" ref="canvasRef"></div>

        <div class="properties-panel">
          <a-card size="small" title="流程属性" class="properties-card">
            <a-form layout="vertical" :label-col="{ span: 24 }">
              <a-form-item label="流程标识">
                <a-input v-model:value="formData.processCode" placeholder="如：LEAVE_APPROVAL" />
              </a-form-item>
              <a-form-item label="流程名称">
                <a-input v-model:value="formData.processName" placeholder="如：请假审批流程" />
              </a-form-item>
              <a-form-item label="业务类型">
                <a-select v-model:value="formData.businessType" placeholder="选择业务类型">
                  <a-select-option value="Leave">请假审批</a-select-option>
                  <a-select-option value="Payroll">薪资核算</a-select-option>
                  <a-select-option value="Expense">费用申请</a-select-option>
                  <a-select-option value="Purchase">采购申请</a-select-option>
                  <a-select-option value="Reimbursement">报销申请</a-select-option>
                </a-select>
              </a-form-item>
              <a-form-item label="描述">
                <a-textarea v-model:value="formData.description" placeholder="流程描述" :rows="3" />
              </a-form-item>
            </a-form>
          </a-card>

          <a-card size="small" title="节点属性" class="properties-card" v-if="selectedElement">
            <a-form layout="vertical" :label-col="{ span: 24 }">
              <a-form-item label="节点ID">
                <a-input :value="selectedElement.id" disabled />
              </a-form-item>
              <a-form-item label="节点名称">
                <a-input v-model:value="nodeFormData.nodeName" placeholder="节点名称" @change="updateNodeName" />
              </a-form-item>
              <a-form-item label="节点类型">
                <a-select v-model:value="nodeFormData.nodeType" @change="updateNodeType">
                  <a-select-option value="StartEvent">开始事件</a-select-option>
                  <a-select-option value="EndEvent">结束事件</a-select-option>
                  <a-select-option value="UserTask">用户任务</a-select-option>
                  <a-select-option value="ServiceTask">服务任务</a-select-option>
                  <a-select-option value="ExclusiveGateway">排他网关</a-select-option>
                  <a-select-option value="ParallelGateway">并行网关</a-select-option>
                  <a-select-option value="InclusiveGateway">包容网关</a-select-option>
                </a-select>
              </a-form-item>

              <template v-if="nodeFormData.nodeType === 'UserTask'">
                <a-divider>审批设置</a-divider>
                <a-form-item label="分配类型">
                  <a-select v-model:value="nodeFormData.assigneeType">
                    <a-select-option value="User">指定用户</a-select-option>
                    <a-select-option value="Initiator">发起人</a-select-option>
                    <a-select-option value="Role">角色</a-select-option>
                    <a-select-option value="Department">部门</a-select-option>
                  </a-select>
                </a-form-item>
                <a-form-item label="审批人" v-if="nodeFormData.assigneeType === 'User'">
                  <a-input v-model:value="nodeFormData.assigneeId" placeholder="用户ID" />
                </a-form-item>
                <a-form-item label="审批人姓名" v-if="nodeFormData.assigneeType === 'User'">
                  <a-input v-model:value="nodeFormData.assigneeName" placeholder="审批人姓名" />
                </a-form-item>
                <a-form-item label="角色" v-if="nodeFormData.assigneeType === 'Role'">
                  <a-select
                    v-model:value="nodeFormData.assigneeId"
                    placeholder="选择角色"
                    @change="handleRoleChange"
                  >
                    <a-select-option
                      v-for="role in workflowRoleOptions"
                      :key="role.roleCode"
                      :value="role.roleCode"
                    >
                      {{ role.roleName }} ({{ role.roleCode }})
                    </a-select-option>
                  </a-select>
                </a-form-item>
                <a-form-item label="优先级">
                  <a-slider v-model:value="nodeFormData.priority" :marks="{1:'低',2:'中',3:'高'}" :min="1" :max="3" />
                </a-form-item>
                <a-form-item label="多选类型">
                  <a-select v-model:value="nodeFormData.multiPersonType" allow-clear placeholder="不需要多选">
                    <a-select-option value="Any">任一审批人</a-select-option>
                    <a-select-option value="All">全部审批人</a-select-option>
                  </a-select>
                </a-form-item>
              </template>

              <template v-if="nodeFormData.nodeType === 'ServiceTask'">
                <a-divider>服务设置</a-divider>
                <a-form-item label="服务类型">
                  <a-select v-model:value="nodeFormData.serviceType">
                    <a-select-option value="Http">HTTP调用</a-select-option>
                    <a-select-option value="Script">脚本</a-select-option>
                    <a-select-option value="Message">消息</a-select-option>
                  </a-select>
                </a-form-item>
                <a-form-item label="服务地址" v-if="nodeFormData.serviceType === 'Http'">
                  <a-input v-model:value="nodeFormData.serviceUrl" placeholder="如：http://api.example.com/callback" />
                </a-form-item>
              </template>

              <template v-if="['ExclusiveGateway', 'InclusiveGateway', 'ParallelGateway'].includes(nodeFormData.nodeType)">
                <a-divider>分支条件</a-divider>
                <a-form-item label="分支条件类型">
                  <a-select v-model:value="nodeFormData.conditionType">
                    <a-select-option value="Expression">表达式</a-select-option>
                    <a-select-option value="Result">结果</a-select-option>
                  </a-select>
                </a-form-item>
              </template>

              <a-button type="primary" block @click="handleUpdateNode">
                更新节点
              </a-button>
            </a-form>
          </a-card>
          <a-card size="small" title="节点属性" class="properties-card" v-else>
            <a-empty description="请在画布中选择节点" />
          </a-card>
        </div>
      </div>
    </a-spin>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onBeforeUnmount, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { message, Modal } from 'ant-design-vue'
import {
  ApiOutlined,
  ApartmentOutlined,
  ArrowLeftOutlined,
  BranchesOutlined,
  CheckCircleOutlined,
  ClusterOutlined,
  SaveOutlined,
  CloudUploadOutlined,
  UndoOutlined,
  RedoOutlined,
  ZoomInOutlined,
  ZoomOutOutlined,
  FullscreenOutlined,
  PlayCircleOutlined,
  ProfileOutlined
} from '@ant-design/icons-vue'
import BpmnModeler from 'bpmn-js/lib/Modeler'
import BpmnContextPadProvider from 'bpmn-js/lib/features/context-pad/ContextPadProvider'
import TranslateModule from 'diagram-js/lib/i18n/translate'
import customTranslateModule from '../../utils/customTranslate'
import { beautifyDiagramXml, buildDefinitionNodesFromXml, buildDiagramXmlFromDefinition } from '../../utils/processDiagram'
import 'bpmn-js/dist/assets/diagram-js.css'
import 'bpmn-js/dist/assets/bpmn-js.css'
import 'bpmn-js/dist/assets/bpmn-font/css/bpmn-embedded.css'
import { accessApi, processCenterApi } from '../../api'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const saving = ref(false)
const publishing = ref(false)
const canvasRef = ref(null)
const definitionId = ref(route.query.id ? Number(route.query.id) : null)
const definitionStatus = ref('Draft')
const hasChanges = ref(false)

const formData = reactive({
  processCode: '',
  processName: '',
  businessType: undefined,
  description: '',
  versionNo: 1
})

const nodeFormData = reactive({
  nodeId: '',
  nodeName: '',
  nodeType: '',
  assigneeType: 'User',
  assigneeId: '',
  assigneeName: '',
  priority: 2,
  multiPersonType: undefined,
  serviceType: 'Http',
  serviceUrl: '',
  conditionType: 'Expression'
})

const selectedElement = ref(null)
const canUndo = ref(false)
const canRedo = ref(false)
const commandStack = ref([])
const commandStackIndex = ref(-1)
const workflowRoleOptions = ref([])

let bpmnModeler = null
let modeling = null
const TASK_NODE_WIDTH = 140
const TASK_NODE_HEIGHT = 80

function CustomContextPadProvider(...args) {
  BpmnContextPadProvider.call(this, ...args)

  const baseGetContextPadEntries = this.getContextPadEntries

  this.getContextPadEntries = function(element) {
    const entries = baseGetContextPadEntries.call(this, element)
    delete entries.replace
    return entries
  }
}

CustomContextPadProvider.prototype = Object.create(BpmnContextPadProvider.prototype)
CustomContextPadProvider.prototype.constructor = CustomContextPadProvider
CustomContextPadProvider.$inject = BpmnContextPadProvider.$inject

const customContextPadModule = {
  contextPadProvider: ['type', CustomContextPadProvider]
}

const isUniformTaskShape = (element) => {
  const elementType = element?.businessObject?.$type
  return ['bpmn:UserTask', 'bpmn:ServiceTask', 'bpmn:Task'].includes(elementType)
}

const normalizeTaskWidths = () => {
  if (!bpmnModeler || !modeling) return

  const elementRegistry = bpmnModeler.get('elementRegistry')
  const taskShapes = elementRegistry.filter(isUniformTaskShape)

  taskShapes.forEach((shape) => {
    if (shape.width === TASK_NODE_WIDTH && shape.height === TASK_NODE_HEIGHT) {
      return
    }

    modeling.resizeShape(shape, {
      x: shape.x,
      y: shape.y,
      width: TASK_NODE_WIDTH,
      height: TASK_NODE_HEIGHT
    })
  })
}

const initBpmnEditor = () => {
  if (!canvasRef.value) return

  try {
    bpmnModeler = new BpmnModeler({
      container: canvasRef.value,
      propertiesPanel: {
        parent: '#properties-panel'
      },
      additionalModules: [
        customContextPadModule,
        TranslateModule,
        customTranslateModule
      ]
    })
  } catch (error) {
    message.error(`流程设计器初始化失败：${error.message || '未知错误'}`)
    return
  }

  modeling = bpmnModeler.get('modeling')
  const eventBus = bpmnModeler.get('eventBus')

  eventBus.on('selection.changed', (event) => {
    const { newSelection } = event
    if (newSelection && newSelection.length === 1) {
      selectedElement.value = newSelection[0]
      loadNodeProperties(newSelection[0])
    } else {
      selectedElement.value = null
    }
  })

  eventBus.on('commandStack.changed', () => {
    hasChanges.value = true
    canUndo.value = bpmnModeler.get('commandStack').canUndo()
    canRedo.value = bpmnModeler.get('commandStack').canRedo()
  })

  eventBus.on('import.done', () => {
    normalizeTaskWidths()
  })

  const newDiagramXML = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL"
                  xmlns:bpmndi="http://www.omg.org/spec/BPMN/20100524/DI"
                  xmlns:dc="http://www.omg.org/spec/DD/20100524/DC"
                  xmlns:di="http://www.omg.org/spec/DD/20100524/DI"
                  id="Definitions_1"
                  targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:startEvent id="StartEvent_1" name="开始" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="BPMNDiagram_1">
    <bpmndi:BPMNPlane id="BPMNPlane_1" bpmnElement="Process_1" />
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`

  bpmnModeler.importXML(newDiagramXML)
}

const loadNodeProperties = (element) => {
  const businessObject = element.businessObject
  nodeFormData.nodeId = element.id
  nodeFormData.nodeName = businessObject.name || ''
  nodeFormData.nodeType = businessObject.$type?.replace('bpmn:', '') || ''

  if (nodeFormData.nodeType === 'UserTask') {
    nodeFormData.assigneeType = businessObject.assigneeType || 'User'
    nodeFormData.assigneeId = businessObject.assigneeId || ''
    nodeFormData.assigneeName = businessObject.assigneeName || ''
    nodeFormData.priority = businessObject.priority || 2
    nodeFormData.multiPersonType = businessObject.multiPersonType
  } else if (nodeFormData.nodeType === 'ServiceTask') {
    nodeFormData.serviceType = businessObject.serviceType || 'Http'
    nodeFormData.serviceUrl = businessObject.serviceUrl || ''
  }
}

const updateNodeName = () => {
  if (!selectedElement.value || !modeling) return
  modeling.updateProperties(selectedElement.value, {
    name: nodeFormData.nodeName
  })
}

const updateNodeType = () => {
  message.info('节点类型变更需要重新设计流程')
}

const handleUpdateNode = () => {
  if (!selectedElement.value || !modeling) return

  const updateData = { name: nodeFormData.nodeName }

  if (nodeFormData.nodeType === 'UserTask') {
    Object.assign(updateData, {
      assigneeType: nodeFormData.assigneeType,
      assigneeId: nodeFormData.assigneeId,
      assigneeName: nodeFormData.assigneeName,
      priority: nodeFormData.priority,
      multiPersonType: nodeFormData.multiPersonType
    })
  } else if (nodeFormData.nodeType === 'ServiceTask') {
    Object.assign(updateData, {
      serviceType: nodeFormData.serviceType,
      serviceUrl: nodeFormData.serviceUrl
    })
  }

  modeling.updateProperties(selectedElement.value, updateData)
  message.success('节点属性已更新')
}

const handleRoleChange = (roleCode) => {
  const matchedRole = workflowRoleOptions.value.find(item => item.roleCode === roleCode)
  if (matchedRole) {
    nodeFormData.assigneeName = matchedRole.roleName
  }
}

const onDragStart = (event, type) => {
  event.dataTransfer.setData('text/plain', type)
  event.dataTransfer.effectAllowed = 'move'
}

const getTemplateDefinition = (templateType) => {
  const templates = {
    simple: {
      processCode: 'TEMPLATE_SIMPLE',
      processName: '简单审批流',
      nodes: [
        {
          nodeId: 'StartEvent_1',
          nodeName: '开始',
          nodeType: 'StartEvent',
          assigneeType: 'Initiator',
          assigneeName: '发起人',
          nextNodeId: 'Activity_Approve',
          sortOrder: 1
        },
        {
          nodeId: 'Activity_Approve',
          nodeName: '审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'approver',
          assigneeName: '审批人',
          nextNodeId: 'EndEvent_1',
          sortOrder: 2
        },
        {
          nodeId: 'EndEvent_1',
          nodeName: '结束',
          nodeType: 'EndEvent',
          sortOrder: 3
        }
      ]
    },
    approval: {
      processCode: 'TEMPLATE_APPROVAL',
      processName: '多级审批流',
      nodes: [
        {
          nodeId: 'StartEvent_1',
          nodeName: '开始',
          nodeType: 'StartEvent',
          assigneeType: 'Initiator',
          assigneeName: '发起人',
          nextNodeId: 'Activity_Manager',
          sortOrder: 1
        },
        {
          nodeId: 'Activity_Manager',
          nodeName: '部门经理审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'dept_manager',
          assigneeName: '部门经理',
          nextNodeId: 'Activity_Director',
          sortOrder: 2
        },
        {
          nodeId: 'Activity_Director',
          nodeName: '总监审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'director',
          assigneeName: '总监',
          nextNodeId: 'Activity_HR',
          sortOrder: 3
        },
        {
          nodeId: 'Activity_HR',
          nodeName: '人事审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'hr',
          assigneeName: '人事',
          nextNodeId: 'EndEvent_1',
          sortOrder: 4
        },
        {
          nodeId: 'EndEvent_1',
          nodeName: '结束',
          nodeType: 'EndEvent',
          sortOrder: 5
        }
      ]
    },
    branch: {
      processCode: 'TEMPLATE_BRANCH',
      processName: '条件分支流',
      nodes: [
        {
          nodeId: 'StartEvent_1',
          nodeName: '开始',
          nodeType: 'StartEvent',
          assigneeType: 'Initiator',
          assigneeName: '发起人',
          nextNodeId: 'Gateway_1',
          sortOrder: 1
        },
        {
          nodeId: 'Gateway_1',
          nodeName: '条件判断',
          nodeType: 'ExclusiveGateway',
          conditions: [
            {
              conditionExpression: 'amount > 1000',
              targetNodeId: 'Activity_Approve'
            },
            {
              conditionExpression: 'amount <= 1000',
              targetNodeId: 'Activity_Auto'
            }
          ],
          sortOrder: 2
        },
        {
          nodeId: 'Activity_Approve',
          nodeName: '经理审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'manager',
          assigneeName: '经理',
          nextNodeId: 'EndEvent_1',
          sortOrder: 3
        },
        {
          nodeId: 'Activity_Auto',
          nodeName: '自动处理',
          nodeType: 'ServiceTask',
          serviceType: 'Http',
          serviceUrl: '/api/workflow/auto-handle',
          nextNodeId: 'EndEvent_1',
          sortOrder: 4
        },
        {
          nodeId: 'EndEvent_1',
          nodeName: '结束',
          nodeType: 'EndEvent',
          sortOrder: 5
        }
      ]
    },
    expense: {
      processCode: 'TEMPLATE_EXPENSE',
      processName: '费用申请流',
      nodes: [
        {
          nodeId: 'StartEvent_1',
          nodeName: '开始',
          nodeType: 'StartEvent',
          assigneeType: 'Initiator',
          assigneeName: '发起人',
          nextNodeId: 'Activity_Fill',
          sortOrder: 1
        },
        {
          nodeId: 'Activity_Fill',
          nodeName: '填写费用申请',
          nodeType: 'UserTask',
          assigneeType: 'Initiator',
          assigneeName: '发起人',
          nextNodeId: 'Gateway_Amount',
          sortOrder: 2
        },
        {
          nodeId: 'Gateway_Amount',
          nodeName: '费用金额判断',
          nodeType: 'ExclusiveGateway',
          conditions: [
            {
              conditionExpression: 'amount <= 1000',
              targetNodeId: 'Activity_Dept'
            },
            {
              conditionExpression: 'amount > 1000 && amount <= 5000',
              targetNodeId: 'Activity_Branch'
            },
            {
              conditionExpression: 'amount > 5000',
              targetNodeId: 'Activity_Headquarters'
            }
          ],
          sortOrder: 3
        },
        {
          nodeId: 'Activity_Dept',
          nodeName: '部门审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'dept_manager',
          assigneeName: '部门经理',
          nextNodeId: 'Gateway_Join',
          sortOrder: 4
        },
        {
          nodeId: 'Activity_Branch',
          nodeName: '分公司审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'branch_manager',
          assigneeName: '分公司经理',
          nextNodeId: 'Gateway_Join',
          sortOrder: 5
        },
        {
          nodeId: 'Activity_Headquarters',
          nodeName: '总公司审批',
          nodeType: 'UserTask',
          assigneeType: 'Role',
          assigneeId: 'headquarters_manager',
          assigneeName: '总公司审批人',
          nextNodeId: 'Gateway_Join',
          sortOrder: 6
        },
        {
          nodeId: 'Gateway_Join',
          nodeName: '审批合并',
          nodeType: 'ExclusiveGateway',
          nextNodeId: 'EndEvent_1',
          sortOrder: 7
        },
        {
          nodeId: 'EndEvent_1',
          nodeName: '结束',
          nodeType: 'EndEvent',
          sortOrder: 8
        }
      ]
    }
  }

  return templates[templateType] || null
}

const handleLoadTemplate = async (templateType) => {
  const templateDefinition = getTemplateDefinition(templateType)

  if (!templateDefinition) {
    message.error('未找到对应的流程模板')
    return
  }

  try {
    const templateXml = buildDiagramXmlFromDefinition(templateDefinition)
    await bpmnModeler.importXML(templateXml)
    bpmnModeler.get('canvas').zoom('fit-viewport')
    selectedElement.value = null
    hasChanges.value = true
    message.success(`${templateDefinition.processName}已加载`)
  } catch (error) {
    message.error('模板加载失败：' + error.message)
  }
}

const buildDefinitionPayload = async () => {
  const { xml } = await bpmnModeler.saveXML({ format: true })
  const nodes = buildDefinitionNodesFromXml(xml)

  if (!nodes.length) {
    throw new Error('流程图中未识别到可保存的节点')
  }

  return {
    processCode: formData.processCode,
    processName: formData.processName,
    businessType: formData.businessType,
    description: formData.description,
    versionNo: formData.versionNo,
    diagramXml: xml,
    callbackConfig: undefined,
    nodes
  }
}

const saveDraftDefinition = async ({ silent = false } = {}) => {
  const payload = await buildDefinitionPayload()
  const targetId = definitionStatus.value === 'Draft' ? definitionId.value : null
  const result = await processCenterApi.saveDefinition(targetId, payload)
  definitionId.value = result?.id ?? definitionId.value
  definitionStatus.value = result?.status || 'Draft'
  formData.versionNo = result?.versionNo ?? formData.versionNo

  if (!silent) {
    message.success('保存成功')
  }

  hasChanges.value = false
  return result
}

const handleSave = async () => {
  if (!formData.processCode) {
    message.error('请填写流程编码')
    return
  }
  if (!formData.processName) {
    message.error('请填写流程名称')
    return
  }
  if (!formData.businessType) {
    message.error('请选择业务类型')
    return
  }

  saving.value = true
  try {
    await saveDraftDefinition()
  } catch (error) {
    message.error('保存失败：' + (error.message || '未知错误'))
  } finally {
    saving.value = false
  }
}

const handlePublish = async () => {
  if (!formData.processCode) {
    message.error('请填写流程编码')
    return
  }
  if (!formData.processName) {
    message.error('请填写流程名称')
    return
  }
  if (!formData.businessType) {
    message.error('请选择业务类型')
    return
  }

  Modal.confirm({
    title: '确认发布流程？',
    content: '发布后流程将可用于创建实例，是否继续？',
    async onOk() {
      publishing.value = true
      try {
        const draft = await saveDraftDefinition({ silent: true })
        const publishId = draft?.id || definitionId.value
        if (!publishId) {
          throw new Error('未获取到可发布的流程定义')
        }

        const result = await processCenterApi.publishDefinition(publishId)
        message.success('发布成功')
        hasChanges.value = false
        definitionId.value = result?.id || publishId
        definitionStatus.value = result?.status || 'Published'
        formData.versionNo = result?.versionNo ?? formData.versionNo
      } catch (error) {
        message.error('发布失败：' + (error.message || '未知错误'))
      } finally {
        publishing.value = false
      }
    }
  })
}

const handleUndo = () => {
  bpmnModeler.get('commandStack').undo()
}

const handleRedo = () => {
  bpmnModeler.get('commandStack').redo()
}

const handleZoomIn = () => {
  bpmnModeler.get('canvas').zoom(bpmnModeler.get('canvas').zoom() * 1.2)
}

const handleZoomOut = () => {
  bpmnModeler.get('canvas').zoom(bpmnModeler.get('canvas').zoom() / 1.2)
}

const handleZoomReset = () => {
  bpmnModeler.get('canvas').zoom('fit-viewport')
}

const handleAutoBeautify = async () => {
  if (!bpmnModeler) {
    return
  }

  try {
    const { xml } = await bpmnModeler.saveXML({ format: true })
    const beautifiedXml = beautifyDiagramXml(xml)
    await bpmnModeler.importXML(beautifiedXml)
    bpmnModeler.get('canvas').zoom('fit-viewport')
    selectedElement.value = null
    hasChanges.value = true
    message.success('流程图已自动美化')
  } catch (error) {
    message.error('自动美化失败：' + (error.message || '未知错误'))
  }
}

const handleBack = () => {
  if (hasChanges.value) {
    Modal.confirm({
      title: '有未保存的更改',
      content: '确定要离开吗？',
      onOk() {
        router.push('/workflow/process-center')
      }
    })
  } else {
    router.push('/workflow/process-center')
  }
}

const loadDefinition = async (id) => {
  loading.value = true
  try {
    const definition = await processCenterApi.getDefinition(id)
    if (definition) {
      definitionId.value = definition.id
      definitionStatus.value = definition.status || 'Draft'
      formData.processCode = definition.processCode
      formData.processName = definition.processName
      formData.businessType = definition.businessType
      formData.description = definition.description || ''
      formData.versionNo = definition.versionNo

      const xml = definition.diagramXml || buildDiagramXmlFromDefinition(definition)
      if (xml) {
        await bpmnModeler.importXML(xml)
        bpmnModeler.get('canvas').zoom('fit-viewport')
        hasChanges.value = false
        canUndo.value = false
        canRedo.value = false
      }
    }
  } catch (error) {
    message.error('加载流程定义失败')
  } finally {
    loading.value = false
  }
}

const loadWorkflowRoles = async () => {
  try {
    const roles = await accessApi.getRoles()
    workflowRoleOptions.value = (roles || [])
      .filter(item => item.isActive !== false)
      .map(item => ({
        roleCode: item.roleCode,
        roleName: item.roleName
      }))
  } catch (error) {
    workflowRoleOptions.value = [
      { roleCode: 'dept_leader', roleName: '部门主管' },
      { roleCode: 'branch_manager', roleName: '分公司经理' },
      { roleCode: 'ceo', roleName: '总经理' },
      { roleCode: 'finance', roleName: '财务' },
      { roleCode: 'hr', roleName: '人事' }
    ]
  }
}

onMounted(async () => {
  initBpmnEditor()
  await loadWorkflowRoles()
  if (definitionId.value) {
    await loadDefinition(definitionId.value)
  }
})

onBeforeUnmount(() => {
  if (bpmnModeler) {
    bpmnModeler.destroy()
  }
})
</script>

<style scoped>
.process-designer {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #f0f2f5;
}

.designer-header {
  display: flex;
  justify-content: space-between;
  padding: 12px 16px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
}

.designer-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.palette-panel {
  width: 200px;
  background: #fff;
  border-right: 1px solid #e8e8e8;
  padding: 16px;
  overflow-y: auto;
}

.palette-title {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  margin-bottom: 12px;
}

.palette-items {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.palette-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: #fafafa;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  cursor: move;
  transition: all 0.2s;
}

.palette-item:hover {
  background: #e6f7ff;
  border-color: #1890ff;
}

.palette-item.template {
  cursor: pointer;
  background: #f0f5ff;
  border-color: #adc6ff;
}

.palette-item.template:hover {
  background: #d4e4ff;
}

.palette-icon {
  font-size: 18px;
  width: 24px;
  height: 24px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.start-icon {
  color: #52c41a;
}

.end-icon {
  color: #ff4d4f;
}

.task-icon {
  color: #1890ff;
}

.service-icon {
  color: #722ed1;
}

.gateway-icon {
  color: #fa8c16;
}

.canvas-panel {
  flex: 1;
  background: #ffffff;
  overflow: hidden;
}

.canvas-panel :deep(.bpmn-js-container) {
  height: 100%;
}

.canvas-panel :deep(.bjs-powered-by),
.canvas-panel :deep(.bjs-powered-by-lightbox) {
  display: none !important;
}

:deep(.djs-context-pad .entry.replace),
:deep(.djs-context-pad .bpmn-icon-screw-wrench) {
  display: none !important;
}

.properties-panel {
  width: 320px;
  background: #fff;
  border-left: 1px solid #e8e8e8;
  overflow-y: auto;
}

.properties-card {
  border-radius: 0;
  border-bottom: 1px solid #e8e8e8;
}

.properties-card :deep(.ant-card-head) {
  min-height: 40px;
  padding: 0 12px;
}

.properties-card :deep(.ant-card-body) {
  padding: 12px;
}
</style>
