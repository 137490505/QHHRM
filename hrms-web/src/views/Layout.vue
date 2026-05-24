<template>
  <a-layout class="layout-container" :class="{ 'dark-theme': isDark }">
    <a-layout-sider v-model:collapsed="collapsed" :trigger="null" collapsible class="sider" :width="220">
      <div class="logo" @click="router.push(userStore.firstPagePath)">
        <UserOutlined class="logo-icon" />
        <span v-if="!collapsed" class="logo-text">HRMS</span>
      </div>

      <a-menu
        v-model:selectedKeys="selectedKeys"
        v-model:openKeys="openKeys"
        theme="dark"
        mode="inline"
        @click="handleMenuClick"
      >
        <NavigationMenuNode
          v-for="menu in visibleMenus"
          :key="menu.menuKey"
          :menu="menu"
          :get-icon="getIcon"
        />
      </a-menu>
    </a-layout-sider>

    <a-layout>
      <a-layout-header class="header">
        <div class="header-left">
          <menu-fold-outlined v-if="collapsed" class="trigger" @click="collapsed = false" />
          <menu-unfold-outlined v-else class="trigger" @click="collapsed = true" />

          <a-breadcrumb class="breadcrumb">
            <a-breadcrumb-item v-for="(item, index) in breadcrumbItems" :key="`${item.menuKey || item.routePath || item.menuName}-${index}`">
              <a v-if="item.clickable" @click.prevent="router.push(item.routePath)">
                {{ item.menuName }}
              </a>
              <span v-else>{{ item.menuName }}</span>
            </a-breadcrumb-item>
          </a-breadcrumb>
        </div>

        <div class="header-right">
          <a-button type="text" class="header-btn" @click="router.go(0)" title="刷新">
            <SyncOutlined />
          </a-button>
          <a-button type="text" class="header-btn" @click="toggleTheme" title="主题切换">
            <SwapOutlined />
          </a-button>

          <a-dropdown>
            <div class="user-info">
              <UserOutlined class="user-icon" />
              <span class="user-name">{{ userStore.userInfo?.name || '用户' }}</span>
            </div>
            <template #overlay>
              <a-menu>
                <a-menu-item key="logout" @click="handleLogout">
                  <LogoutOutlined />
                  <span>退出登录</span>
                </a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>

      <a-layout-content class="content">
        <router-view />
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '../store/user'
import NavigationMenuNode from '../components/NavigationMenuNode.vue'
import { findMenuByRoutePath, flattenMenus, mergeNavigationMenus } from '../utils/navigation'
import {
  ClockCircleOutlined,
  ClusterOutlined,
  FileTextOutlined,
  HomeOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  MoneyCollectOutlined,
  SafetyCertificateOutlined,
  SettingOutlined,
  SwapOutlined,
  SyncOutlined,
  TeamOutlined,
  UserOutlined
} from '@ant-design/icons-vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const collapsed = ref(false)
const isDark = ref(false)
const selectedKeys = ref([])
const openKeys = ref([])

const iconMap = {
  HomeOutlined,
  ClusterOutlined,
  TeamOutlined,
  ClockCircleOutlined,
  MoneyCollectOutlined,
  FileTextOutlined,
  SettingOutlined,
  SafetyCertificateOutlined
}

const getIcon = (iconName) => iconMap[iconName] || HomeOutlined

const visibleMenus = computed(() => {
  return mergeNavigationMenus(userStore.menuTree || [])
})

const flatMenus = computed(() => {
  return flattenMenus(visibleMenus.value)
})

const currentMenu = computed(() => findMenuByRoutePath(flatMenus.value, route.path))

const breadcrumbItems = computed(() => {
  const pageTitle = `${route.meta?.title || ''}`.trim()

  if (!currentMenu.value) {
    return pageTitle
      ? [{
          menuKey: `${route.name || route.path}-page`,
          menuName: pageTitle,
          routePath: null,
          clickable: false
        }]
      : []
  }

  const menuItems = [...currentMenu.value.parents, currentMenu.value]
    .filter(item => item.menuType !== 'button')
    .map(item => ({
      menuKey: item.menuKey,
      menuName: item.menuName,
      routePath: item.routePath,
      clickable: Boolean(item.routePath)
    }))

  if (pageTitle) {
    const lastMenu = menuItems[menuItems.length - 1]
    if (lastMenu) {
      lastMenu.clickable = Boolean(lastMenu.routePath)
    }

    menuItems.push({
      menuKey: `${currentMenu.value.menuKey}-page`,
      menuName: pageTitle,
      routePath: null,
      clickable: false
    })
    return menuItems
  }

  if (menuItems.length) {
    menuItems[menuItems.length - 1].clickable = false
  }

  return menuItems
})

const handleMenuClick = ({ key }) => {
  const target = flatMenus.value.find(item => item.menuKey === key)
  if (target?.routePath) {
    router.push(target.routePath)
  }
}

const handleLogout = () => {
  userStore.logout()
  router.push('/login')
}

const toggleTheme = () => {
  isDark.value = !isDark.value
  localStorage.setItem('hrms-theme', isDark.value ? 'dark' : 'light')
}

watch(
  () => route.path,
  (path) => {
    const menu = findMenuByRoutePath(flatMenus.value, path)
    if (!menu) {
      return
    }
    selectedKeys.value = [menu.menuKey]
    openKeys.value = menu.parents.map(item => item.menuKey)
  },
  { immediate: true }
)

onMounted(() => {
  isDark.value = localStorage.getItem('hrms-theme') === 'dark'
})
</script>

<style scoped>
.layout-container {
  min-height: 100vh;
}

.layout-container.dark-theme {
  background: #111827;
}

.sider {
  background: #001529;
}

.logo {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #fff;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.logo-icon {
  font-size: 22px;
}

.logo-text {
  margin-left: 10px;
  font-size: 18px;
  font-weight: 600;
}

.header {
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #fff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.dark-theme .header {
  background: #1f2937;
}

.header-left,
.header-right,
.user-info {
  display: flex;
  align-items: center;
}

.trigger,
.header-btn,
.user-icon,
.user-name,
.breadcrumb :deep(a),
.breadcrumb :deep(span) {
  color: inherit;
}

.trigger {
  font-size: 18px;
  cursor: pointer;
}

.breadcrumb {
  margin-left: 16px;
}

.user-info {
  cursor: pointer;
  padding: 0 12px;
}

.user-icon {
  margin-right: 8px;
  font-size: 18px;
}

.content {
  margin: 16px;
  padding: 16px;
  min-height: calc(100vh - 96px);
  background: #f5f5f5;
}

.dark-theme .content {
  background: #111827;
}
</style>
