const BPMN_NS = 'http://www.omg.org/spec/BPMN/20100524/MODEL'
const BPMNDI_NS = 'http://www.omg.org/spec/BPMN/20100524/DI'
const DC_NS = 'http://www.omg.org/spec/DD/20100524/DC'
const DI_NS = 'http://www.omg.org/spec/DD/20100524/DI'

export const xmlEscape = (value = '') => String(value)
  .replaceAll('&', '&amp;')
  .replaceAll('"', '&quot;')
  .replaceAll('<', '&lt;')
  .replaceAll('>', '&gt;')

const getChildElements = (element) => Array.from(element?.children || [])

const getAttributeValue = (element, name, fallback = '') => {
  const value = element?.getAttribute?.(name)
  return value == null ? fallback : value
}

const toProcessNodeType = (localName = '') => {
  const map = {
    startEvent: 'StartEvent',
    endEvent: 'EndEvent',
    userTask: 'UserTask',
    serviceTask: 'ServiceTask',
    exclusiveGateway: 'ExclusiveGateway',
    parallelGateway: 'ParallelGateway',
    inclusiveGateway: 'InclusiveGateway'
  }
  return map[localName] || 'UserTask'
}

const toBpmnElementName = (nodeType = '') => {
  const map = {
    StartEvent: 'startEvent',
    EndEvent: 'endEvent',
    UserTask: 'userTask',
    ServiceTask: 'serviceTask',
    ExclusiveGateway: 'exclusiveGateway',
    ParallelGateway: 'parallelGateway',
    InclusiveGateway: 'inclusiveGateway'
  }
  return map[nodeType] || 'userTask'
}

const getNodeSize = (nodeOrType) => {
  const node = typeof nodeOrType === 'object' && nodeOrType !== null ? nodeOrType : null
  const nodeType = node?.nodeType || nodeOrType
  if (node?.diagramWidth || node?.diagramHeight) {
    return {
      width: node?.diagramWidth || 120,
      height: node?.diagramHeight || 80
    }
  }
  if (nodeType === 'StartEvent' || nodeType === 'EndEvent') {
    return { width: 36, height: 36 }
  }
  if (nodeType === 'ExclusiveGateway' || nodeType === 'ParallelGateway' || nodeType === 'InclusiveGateway') {
    return { width: 50, height: 50 }
  }
  return { width: 120, height: 80 }
}

const getUniqueTargetIds = (flows = []) => [...new Set(flows.map((flow) => flow.targetRef).filter(Boolean))]

const createBranchOffsets = (count) => {
  if (count <= 1) {
    return [0]
  }

  const center = (count - 1) / 2
  return Array.from({ length: count }, (_, index) => index - center)
}

const computeAutoLayout = (nodes = [], flows = []) => {
  const xGap = 220
  const yGap = 140
  const baseX = 120
  const baseCenterY = 180
  const outgoingMap = flows.reduce((result, flow) => {
    if (!result[flow.sourceRef]) {
      result[flow.sourceRef] = []
    }
    result[flow.sourceRef].push(flow)
    return result
  }, {})
  const incomingMap = flows.reduce((result, flow) => {
    if (!result[flow.targetRef]) {
      result[flow.targetRef] = []
    }
    result[flow.targetRef].push(flow)
    return result
  }, {})

  const starts = nodes.filter((node) => !incomingMap[node.nodeId]?.length)
  const orderedStarts = (starts.length ? starts : nodes)
    .slice()
    .sort((left, right) => (left.sortOrder || 0) - (right.sortOrder || 0))

  const levelMap = new Map()
  const rowMap = new Map()
  const queue = []

  orderedStarts.forEach((node, index) => {
    levelMap.set(node.nodeId, 0)
    rowMap.set(node.nodeId, index * 2)
    queue.push(node.nodeId)
  })

  while (queue.length) {
    const nodeId = queue.shift()
    const currentLevel = levelMap.get(nodeId) ?? 0
    const outgoingFlows = outgoingMap[nodeId] || []
    outgoingFlows.forEach((flow) => {
      const nextLevel = currentLevel + 1
      const knownLevel = levelMap.get(flow.targetRef)
      if (knownLevel == null || nextLevel > knownLevel) {
        levelMap.set(flow.targetRef, nextLevel)
        queue.push(flow.targetRef)
      }
    })
  }

  nodes
    .slice()
    .sort((left, right) => (levelMap.get(left.nodeId) ?? 0) - (levelMap.get(right.nodeId) ?? 0))
    .forEach((node) => {
      const sourceRow = rowMap.get(node.nodeId)
      const targetIds = getUniqueTargetIds(outgoingMap[node.nodeId] || [])
      if (!targetIds.length || sourceRow == null) {
        return
      }

      if (targetIds.length === 1) {
        const [targetId] = targetIds
        if (rowMap.get(targetId) == null) {
          rowMap.set(targetId, sourceRow)
        }
        return
      }

      const offsets = createBranchOffsets(targetIds.length)
      targetIds.forEach((targetId, index) => {
        if (rowMap.get(targetId) != null) {
          return
        }
        rowMap.set(targetId, sourceRow + offsets[index] * 1.5)
      })
    })

  nodes
    .slice()
    .sort((left, right) => (levelMap.get(left.nodeId) ?? 0) - (levelMap.get(right.nodeId) ?? 0))
    .forEach((node) => {
      if (rowMap.get(node.nodeId) != null) {
        return
      }

      const incomingRows = (incomingMap[node.nodeId] || [])
        .map((flow) => rowMap.get(flow.sourceRef))
        .filter((row) => row != null)

      if (incomingRows.length) {
        const averageRow = incomingRows.reduce((sum, row) => sum + row, 0) / incomingRows.length
        rowMap.set(node.nodeId, averageRow)
      } else {
        rowMap.set(node.nodeId, 0)
      }
    })

  const allRows = [...rowMap.values()]
  const minRow = allRows.length ? Math.min(...allRows) : 0

  return nodes.reduce((result, node, index) => {
    const { width, height } = getNodeSize(node)
    const level = levelMap.get(node.nodeId) ?? index
    const row = rowMap.get(node.nodeId) ?? 0
    const centerY = baseCenterY + (row - minRow) * yGap
    result.set(node.nodeId, {
      x: baseX + level * xGap,
      y: centerY - height / 2,
      width,
      height
    })
    return result
  }, new Map())
}

const getEdgeWaypoints = (source, target) => {
  const startX = source.x + source.width
  const startY = source.y + source.height / 2
  const endX = target.x
  const endY = target.y + target.height / 2

  if (Math.abs(startY - endY) < 1) {
    return [
      { x: startX, y: startY },
      { x: endX, y: endY }
    ]
  }

  const middleX = startX + Math.max(40, (endX - startX) / 2)
  return [
    { x: startX, y: startY },
    { x: middleX, y: startY },
    { x: middleX, y: endY },
    { x: endX, y: endY }
  ]
}

const getElementChildrenByLocalName = (parent, localName) =>
  getChildElements(parent).filter((element) => element.localName === localName)

const ensureDiagramPlane = (document, processId) => {
  let diagramElement = getChildElements(document.documentElement).find((element) => element.localName === 'BPMNDiagram')
  if (!diagramElement) {
    diagramElement = document.createElementNS(BPMNDI_NS, 'bpmndi:BPMNDiagram')
    diagramElement.setAttribute('id', 'BPMNDiagram_1')
    document.documentElement.appendChild(diagramElement)
  }

  let planeElement = getChildElements(diagramElement).find((element) => element.localName === 'BPMNPlane')
  if (!planeElement) {
    planeElement = document.createElementNS(BPMNDI_NS, 'bpmndi:BPMNPlane')
    planeElement.setAttribute('id', 'BPMNPlane_1')
    diagramElement.appendChild(planeElement)
  }

  planeElement.setAttribute('bpmnElement', processId)
  return planeElement
}

export const buildDefinitionNodesFromXml = (xml) => {
  const parser = new DOMParser()
  const document = parser.parseFromString(xml, 'text/xml')
  if (document.querySelector('parsererror')) {
    throw new Error('流程图 XML 解析失败')
  }

  const processElement = getChildElements(document.documentElement).find((element) => element.localName === 'process')
  if (!processElement) {
    return []
  }

  const processChildren = getChildElements(processElement)
  const sequenceFlows = processChildren
    .filter((element) => element.localName === 'sequenceFlow')
    .map((element) => ({
      id: getAttributeValue(element, 'id'),
      sourceRef: getAttributeValue(element, 'sourceRef'),
      targetRef: getAttributeValue(element, 'targetRef'),
      conditionExpression: getChildElements(element)
        .find((child) => child.localName === 'conditionExpression')
        ?.textContent?.trim()
    }))

  const outgoingFlowMap = sequenceFlows.reduce((result, flow) => {
    if (!result[flow.sourceRef]) {
      result[flow.sourceRef] = []
    }
    result[flow.sourceRef].push(flow)
    return result
  }, {})

  return processChildren
    .filter((element) => element.localName !== 'sequenceFlow')
    .map((element) => {
      const nodeType = toProcessNodeType(element.localName)
      const outgoingFlows = outgoingFlowMap[getAttributeValue(element, 'id')] || []
      const unconditionalFlow = outgoingFlows.find((flow) => !flow.conditionExpression)
      const conditionalFlows = outgoingFlows.filter((flow) => !!flow.conditionExpression)
      const defaultAssigneeType = nodeType === 'StartEvent' ? 'Initiator' : 'User'
      const defaultAssigneeName = nodeType === 'StartEvent' ? '发起人' : ''

      return {
        nodeId: getAttributeValue(element, 'id'),
        nodeName: getAttributeValue(element, 'name', getAttributeValue(element, 'id')),
        nodeType,
        assigneeType: getAttributeValue(element, 'assigneeType', defaultAssigneeType),
        assigneeId: getAttributeValue(element, 'assigneeId'),
        assigneeName: getAttributeValue(element, 'assigneeName', defaultAssigneeName),
        multiPersonType: getAttributeValue(element, 'multiPersonType') || undefined,
        nextNodeId: unconditionalFlow?.targetRef || undefined,
        conditions: conditionalFlows.map((flow) => ({
          conditionExpression: flow.conditionExpression,
          targetNodeId: flow.targetRef
        }))
      }
    })
}

export const buildFlowRecords = (nodes = []) => {
  const records = []
  const seenKeys = new Set()

  nodes.forEach((node) => {
    ;(node.conditions || []).forEach((condition) => {
      const key = `${node.nodeId}->${condition.targetNodeId}->${condition.conditionExpression || ''}`
      if (seenKeys.has(key)) {
        return
      }
      seenKeys.add(key)
      records.push({
        id: `Flow_${records.length + 1}`,
        sourceRef: node.nodeId,
        targetRef: condition.targetNodeId,
        conditionExpression: condition.conditionExpression
      })
    })

    if (!node.nextNodeId) {
      return
    }

    const key = `${node.nodeId}->${node.nextNodeId}->`
    if (seenKeys.has(key)) {
      return
    }

    seenKeys.add(key)
    records.push({
      id: `Flow_${records.length + 1}`,
      sourceRef: node.nodeId,
      targetRef: node.nextNodeId
    })
  })

  return records
}

export const buildDiagramXmlFromDefinition = (definition) => {
  const nodes = [...(definition?.nodes || [])].sort((left, right) => (left.sortOrder || 0) - (right.sortOrder || 0))
  if (!nodes.length) {
    return `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="${BPMN_NS}"
                  xmlns:bpmndi="${BPMNDI_NS}"
                  xmlns:dc="${DC_NS}"
                  xmlns:di="${DI_NS}"
                  id="Definitions_1"
                  targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_1" isExecutable="true">
    <bpmn:startEvent id="StartEvent_1" name="开始" />
  </bpmn:process>
  <bpmndi:BPMNDiagram id="BPMNDiagram_1">
    <bpmndi:BPMNPlane id="BPMNPlane_1" bpmnElement="Process_1" />
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`
  }

  const flows = buildFlowRecords(nodes)
  const positions = computeAutoLayout(nodes, flows)
  const nodeXml = nodes.map((node) => {
    const tagName = toBpmnElementName(node.nodeType)
    const attributes = [
      `id="${xmlEscape(node.nodeId)}"`,
      `name="${xmlEscape(node.nodeName || node.nodeId)}"`
    ]

    if (node.assigneeType) {
      attributes.push(`assigneeType="${xmlEscape(node.assigneeType)}"`)
    }
    if (node.assigneeId) {
      attributes.push(`assigneeId="${xmlEscape(node.assigneeId)}"`)
    }
    if (node.assigneeName) {
      attributes.push(`assigneeName="${xmlEscape(node.assigneeName)}"`)
    }
    if (node.multiPersonType) {
      attributes.push(`multiPersonType="${xmlEscape(node.multiPersonType)}"`)
    }

    return `    <bpmn:${tagName} ${attributes.join(' ')} />`
  }).join('\n')

  const flowXml = flows.map((flow) => {
    const conditionExpression = flow.conditionExpression
      ? `
      <bpmn:conditionExpression>${xmlEscape(flow.conditionExpression)}</bpmn:conditionExpression>`
      : ''
    return `    <bpmn:sequenceFlow id="${flow.id}" sourceRef="${xmlEscape(flow.sourceRef)}" targetRef="${xmlEscape(flow.targetRef)}">${conditionExpression}
    </bpmn:sequenceFlow>`
  }).join('\n')

  const shapeXml = nodes.map((node) => {
    const position = positions.get(node.nodeId)
    return `    <bpmndi:BPMNShape id="${xmlEscape(node.nodeId)}_di" bpmnElement="${xmlEscape(node.nodeId)}">
      <dc:Bounds x="${position.x}" y="${position.y}" width="${position.width}" height="${position.height}" />
    </bpmndi:BPMNShape>`
  }).join('\n')

  const edgeXml = flows.map((flow) => {
    const source = positions.get(flow.sourceRef)
    const target = positions.get(flow.targetRef)
    if (!source || !target) {
      return ''
    }
    const waypoints = getEdgeWaypoints(source, target)

    return `    <bpmndi:BPMNEdge id="${flow.id}_di" bpmnElement="${flow.id}">
${waypoints.map((point) => `      <di:waypoint x="${point.x}" y="${point.y}" />`).join('\n')}
    </bpmndi:BPMNEdge>`
  }).filter(Boolean).join('\n')

  return `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="${BPMN_NS}"
                  xmlns:bpmndi="${BPMNDI_NS}"
                  xmlns:dc="${DC_NS}"
                  xmlns:di="${DI_NS}"
                  id="Definitions_${xmlEscape(definition?.processCode || '1')}"
                  targetNamespace="http://bpmn.io/schema/bpmn">
  <bpmn:process id="Process_${xmlEscape(definition?.processCode || '1')}" isExecutable="true">
${nodeXml}
${flowXml}
  </bpmn:process>
  <bpmndi:BPMNDiagram id="BPMNDiagram_1">
    <bpmndi:BPMNPlane id="BPMNPlane_1" bpmnElement="Process_${xmlEscape(definition?.processCode || '1')}">
${shapeXml}
${edgeXml}
    </bpmndi:BPMNPlane>
  </bpmndi:BPMNDiagram>
</bpmn:definitions>`
}

export const beautifyDiagramXml = (xml) => {
  const parser = new DOMParser()
  const document = parser.parseFromString(xml, 'text/xml')
  if (document.querySelector('parsererror')) {
    throw new Error('流程图 XML 解析失败')
  }

  const processElement = getChildElements(document.documentElement).find((element) => element.localName === 'process')
  if (!processElement) {
    throw new Error('未找到流程定义')
  }

  const processId = getAttributeValue(processElement, 'id', 'Process_1')
  const processChildren = getChildElements(processElement)
  const nodeElements = processChildren.filter((element) => element.localName !== 'sequenceFlow')
  const flowElements = processChildren.filter((element) => element.localName === 'sequenceFlow')

  const nodes = nodeElements.map((element, index) => ({
    nodeId: getAttributeValue(element, 'id'),
    nodeType: toProcessNodeType(element.localName),
    sortOrder: index + 1
  }))

  const flows = flowElements.map((element, index) => ({
    id: getAttributeValue(element, 'id', `Flow_${index + 1}`),
    sourceRef: getAttributeValue(element, 'sourceRef'),
    targetRef: getAttributeValue(element, 'targetRef'),
    conditionExpression: getElementChildrenByLocalName(element, 'conditionExpression')[0]?.textContent?.trim()
  }))

  const positions = computeAutoLayout(nodes, flows)
  const planeElement = ensureDiagramPlane(document, processId)

  getChildElements(planeElement).forEach((element) => {
    if (element.localName === 'BPMNShape' || element.localName === 'BPMNEdge') {
      planeElement.removeChild(element)
    }
  })

  nodes.forEach((node) => {
    const position = positions.get(node.nodeId)
    if (!position) {
      return
    }

    const shapeElement = document.createElementNS(BPMNDI_NS, 'bpmndi:BPMNShape')
    shapeElement.setAttribute('id', `${node.nodeId}_di`)
    shapeElement.setAttribute('bpmnElement', node.nodeId)

    const boundsElement = document.createElementNS(DC_NS, 'dc:Bounds')
    boundsElement.setAttribute('x', String(position.x))
    boundsElement.setAttribute('y', String(position.y))
    boundsElement.setAttribute('width', String(position.width))
    boundsElement.setAttribute('height', String(position.height))

    shapeElement.appendChild(boundsElement)
    planeElement.appendChild(shapeElement)
  })

  flows.forEach((flow) => {
    const source = positions.get(flow.sourceRef)
    const target = positions.get(flow.targetRef)
    if (!source || !target) {
      return
    }

    const edgeElement = document.createElementNS(BPMNDI_NS, 'bpmndi:BPMNEdge')
    edgeElement.setAttribute('id', `${flow.id}_di`)
    edgeElement.setAttribute('bpmnElement', flow.id)

    getEdgeWaypoints(source, target).forEach((point) => {
      const waypointElement = document.createElementNS(DI_NS, 'di:waypoint')
      waypointElement.setAttribute('x', String(point.x))
      waypointElement.setAttribute('y', String(point.y))
      edgeElement.appendChild(waypointElement)
    })

    planeElement.appendChild(edgeElement)
  })

  return new XMLSerializer().serializeToString(document)
}
