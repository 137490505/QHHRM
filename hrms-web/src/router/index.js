import { createRouter, createWebHistory } from 'vue-router'
import { message } from 'ant-design-vue'
import Login from '../views/Login.vue'
import Layout from '../views/Layout.vue'
import { authApi } from '../api/auth'
import { useUserStore } from '../store/user'

const Unauthorized = () => import('../views/Unauthorized.vue')
const viewModules = import.meta.glob([
  '../views/**/*.vue',
  '!../views/Login.vue',
  '!../views/Layout.vue',
  '!../views/Unauthorized.vue'
])

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: { requiresAuth: false }
  },
  {
    path: '/403',
    name: 'Unauthorized',
    component: Unauthorized,
    meta: { requiresAuth: false }
  },
  {
    path: '/org-units/management/chart',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.org.chart',
      title: '组织图',
      menuKey: 'org-chart'
    },
    children: [
      {
        path: '',
        name: 'OrgChart',
        component: () => import('../views/org/OrgChartView.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.org.chart',
          title: '组织图',
          menuKey: 'org-chart'
        }
      }
    ]
  },
  {
    path: '/org/chart',
    redirect: '/org-units/management/chart'
  },
  {
    path: '/workflow/processes',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.process.center',
      title: '流程中心',
      menuKey: 'process-center-page'
    },
    children: [
      {
        path: '',
        name: 'ProcessCenter',
        component: () => import('../views/workflow/ProcessCenter.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.process.center',
          title: '流程中心',
          menuKey: 'process-center-page'
        }
      }
    ]
  },
  {
    path: '/workflow/processes/instances/:id',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.process.center',
      title: '流程实例详情',
      menuKey: 'process-center-page'
    },
    children: [
      {
        path: '',
        name: 'ProcessInstanceDetail',
        component: () => import('../views/workflow/ProcessInstanceDetail.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.process.center',
          title: '流程实例详情',
          menuKey: 'process-center-page'
        }
      }
    ]
  },
  {
    path: '/workflow/todo',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.todo.center',
      title: '待办中心',
      menuKey: 'todo-center-page'
    },
    children: [
      {
        path: '',
        name: 'TodoCenter',
        component: () => import('../views/workflow/TodoCenter.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.todo.center',
          title: '待办中心',
          menuKey: 'todo-center-page'
        }
      }
    ]
  },
  {
    path: '/workflow/designer',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.process.designer',
      title: '流程设计器',
      menuKey: 'process-designer-page'
    },
    children: [
      {
        path: '',
        name: 'ProcessDesigner',
        component: () => import('../views/workflow/ProcessDesigner.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.process.designer',
          title: '流程设计器',
          menuKey: 'process-designer-page'
        }
      }
    ]
  },
  {
    path: '/workflow/requests',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.process.requests',
      title: '流程申请',
      menuKey: 'process-requests-page'
    },
    children: [
      {
        path: '',
        name: 'ProcessRequests',
        component: () => import('../views/workflow/ProcessRequests.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.process.requests',
          title: '流程申请',
          menuKey: 'process-requests-page'
        }
      }
    ]
  },
  {
    path: '/workflow/requests/:processCode',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.process.requests',
      title: '提交申请',
      menuKey: 'process-requests-page'
    },
    children: [
      {
        path: '',
        name: 'ProcessRequestForm',
        component: () => import('../views/workflow/ProcessRequestForm.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.process.requests',
          title: '提交申请',
          menuKey: 'process-requests-page'
        }
      }
    ]
  },
  {
    path: '/employees/detail/:id',
    component: Layout,
    meta: {
      requiresAuth: true,
      permissionCode: 'page.employee.list',
      title: '员工详情',
      menuKey: 'employee-list'
    },
    children: [
      {
        path: '',
        name: 'EmployeeDetail',
        component: () => import('../views/employee/EmployeeDetailView.vue'),
        meta: {
          requiresAuth: true,
          permissionCode: 'page.employee.list',
          title: '员工详情',
          menuKey: 'employee-list'
        }
      }
    ]
  },
  {
    path: '/',
    redirect: '/login'
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/login'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

const flattenPageMenus = (menus = []) => {
  const pages = []
  menus.forEach(menu => {
    if (menu.menuType === 'page' && menu.routePath && menu.componentPath) {
      pages.push(menu)
    }
    if (menu.children?.length) {
      pages.push(...flattenPageMenus(menu.children))
    }
  })
  return pages
}

export const registerDynamicRoutes = (menus = []) => {
  const userStore = useUserStore()
  const pageMenus = flattenPageMenus(menus)

  pageMenus.forEach(menu => {
    const routeName = `route-${menu.menuKey}`
    const viewKey = `../${menu.componentPath}`
    const component = viewModules[viewKey]

    if (!component || router.hasRoute(routeName)) {
      return
    }

    router.addRoute({
      path: menu.routePath,
      component: Layout,
      meta: {
        requiresAuth: true,
        permissionCode: menu.permissionCode,
        title: menu.menuName,
        menuKey: menu.menuKey
      },
      children: [
        {
          path: '',
          name: routeName,
          component,
          meta: {
            requiresAuth: true,
            permissionCode: menu.permissionCode,
            title: menu.menuName,
            menuKey: menu.menuKey
          }
        }
      ]
    })
  })

  userStore.setRoutesRegistered(true)
}

const ensureAccessContext = async () => {
  const userStore = useUserStore()

  if (!userStore.token) {
    return false
  }

  try {
    if (!userStore.userInfo || !userStore.permissions.length || !userStore.menuTree.length) {
      const context = await authApi.me()
      userStore.setAuthContext(context)
    }

    if (!userStore.routesRegistered) {
      registerDynamicRoutes(userStore.menuTree)
    }

    return true
  } catch (error) {
    userStore.logout()
    return false
  }
}

router.beforeEach(async (to, from, next) => {
  const userStore = useUserStore()
  const token = userStore.token || localStorage.getItem('token')
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  const hadRoutesRegistered = userStore.routesRegistered

  if (!token && requiresAuth) {
    next('/login')
    return
  }

  if (!token) {
    next()
    return
  }

  const ready = await ensureAccessContext()
  if (!ready) {
    next('/login')
    return
  }

  const isFallbackMatch = to.matched.some(record => record.path === '/:pathMatch(.*)*')
  const resolvedTarget = router.resolve(to.fullPath)
  const hasConcreteMatch = resolvedTarget.matched.some(record => record.path !== '/:pathMatch(.*)*')

  if (isFallbackMatch && hasConcreteMatch) {
    next({ path: resolvedTarget.fullPath, replace: true })
    return
  }

  // Dynamic routes are registered during the first guarded navigation.
  // Retry the target once so Vue Router can rematch against the new routes.
  if (!hadRoutesRegistered && userStore.routesRegistered && to.matched.length === 0) {
    next({ path: to.fullPath, replace: true })
    return
  }

  if (to.path === '/login') {
    next(userStore.firstPagePath)
    return
  }

  if (to.matched.length === 0) {
    next({ path: userStore.firstPagePath, replace: true })
    return
  }

  const permissionCode = to.meta.permissionCode
  if (permissionCode && !userStore.hasPermission(permissionCode)) {
    message.warning('当前账号没有访问该页面的权限')
    next(userStore.firstPagePath || '/403')
    return
  }

  next()
})

export default router
