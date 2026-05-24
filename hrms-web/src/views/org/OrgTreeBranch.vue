<template>
  <ul class="tree-branch" :class="`tree-level-${level}`">
    <li v-for="node in nodes" :key="node.id" class="tree-item">
      <div class="tree-node-row">
        <div
          class="org-node tree-node"
          :class="{ inactive: !node.isActive }"
        >
          <span class="node-main">
            <span class="node-head">
              <button
                type="button"
                class="node-name node-action"
                @click="$emit('select', node)"
              >
                {{ node.name }}
              </button>
              <span class="node-state" :class="{ inactive: !node.isActive }">
                <span class="node-state-dot"></span>
                {{ node.isActive ? '启用' : '停用' }}
              </span>
            </span>
          </span>
          <button
            type="button"
            class="node-secondary node-action manager-action"
            :disabled="!node.managerId"
            @click="$emit('select-manager', node)"
          >
            {{ node.manager || '未设置负责人' }}
          </button>
        </div>
        <button
          v-if="node.children?.length"
          type="button"
          class="branch-toggle"
          :aria-label="isExpanded(node.id) ? '收起下级组织' : '展开下级组织'"
          @click="toggleNode(node.id)"
        >
          {{ isExpanded(node.id) ? '-' : '+' }}
        </button>
      </div>

      <OrgTreeBranch
        v-if="node.children?.length && isExpanded(node.id)"
        :nodes="node.children"
        :level="level + 1"
        @select="$emit('select', $event)"
        @select-manager="$emit('select-manager', $event)"
      />
    </li>
  </ul>
</template>

<script setup>
import { ref } from 'vue'

defineProps({
  nodes: {
    type: Array,
    default: () => []
  },
  level: {
    type: Number,
    default: 3
  }
})

defineEmits(['select', 'select-manager'])

const expandedNodeIds = ref([])

const isExpanded = (nodeId) => expandedNodeIds.value.includes(nodeId)

const toggleNode = (nodeId) => {
  if (isExpanded(nodeId)) {
    expandedNodeIds.value = expandedNodeIds.value.filter(id => id !== nodeId)
    return
  }

  expandedNodeIds.value = [...expandedNodeIds.value, nodeId]
}
</script>

<style scoped>
.tree-node-row {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
}

.node-head {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.node-main {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 10px 12px 12px;
  border-radius: 14px;
  background: #fafbfd;
  border: 1px solid rgba(226, 232, 240, 0.95);
}

.node-name {
  display: inline-flex;
  align-items: center;
  justify-content: flex-start;
  flex: 1;
  min-width: 0;
  font-size: 14px;
  font-weight: 700;
  line-height: 1.5;
  word-break: break-word;
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

.node-secondary {
  width: 100%;
  padding: 8px 12px;
  border-radius: 12px;
  background: #f8fafc;
  border: 1px solid rgba(226, 232, 240, 0.9);
  color: #475569;
  font-size: 12px;
  line-height: 1.4;
  word-break: break-word;
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

.branch-toggle {
  flex: 0 0 28px;
  width: 28px;
  height: 28px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid rgba(148, 163, 184, 0.22);
  border-radius: 999px;
  background: #ffffff;
  color: #1677ff;
  font-size: 16px;
  line-height: 1;
  cursor: pointer;
  box-shadow: 0 3px 10px rgba(15, 23, 42, 0.04);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.branch-toggle:hover {
  border-color: rgba(148, 163, 184, 0.32);
  box-shadow: 0 6px 14px rgba(15, 23, 42, 0.06);
}
</style>
