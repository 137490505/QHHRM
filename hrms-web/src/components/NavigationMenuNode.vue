<template>
  <a-sub-menu v-if="menu.children?.length" :key="menu.menuKey">
    <template #title>
      <component v-if="menu.icon" :is="getIcon(menu.icon)" />
      <span>{{ menu.menuName }}</span>
    </template>
    <NavigationMenuNode
      v-for="child in menu.children"
      :key="child.menuKey"
      :menu="child"
      :get-icon="getIcon"
    />
  </a-sub-menu>
  <a-menu-item v-else :key="menu.menuKey">
    <component v-if="menu.icon" :is="getIcon(menu.icon)" />
    <span>{{ menu.menuName }}</span>
  </a-menu-item>
</template>

<script setup>
defineOptions({ name: 'NavigationMenuNode' })

defineProps({
  menu: {
    type: Object,
    required: true
  },
  getIcon: {
    type: Function,
    required: true
  }
})
</script>
