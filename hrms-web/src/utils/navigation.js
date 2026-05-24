const SETTINGS_MENU_KEYS = ['settings-page', 'config-page']
const MERGED_SETTINGS_MENU_KEY = 'merged-settings-page'
const MERGED_SETTINGS_MENU_NAME = '系统配置'
const ORG_MODULE_MENU_KEY = 'org-module'
const ORG_CHART_ROUTE_PATH = '/org-units/management/chart'
const WORKFLOW_MODULE_MENU_KEY = 'workflow-module'
const PROCESS_DESIGNER_ROUTE_PATH = '/workflow/designer'
const PROCESS_REQUESTS_ROUTE_PATH = '/workflow/requests'

const clone = (value) => JSON.parse(JSON.stringify(value || []))

const isSettingsPage = (menu) => SETTINGS_MENU_KEYS.includes(menu?.menuKey)

const pickPreferredSettingsPage = (items) =>
  items.find(item => item.menuKey === 'config-page') ||
  items.find(item => item.menuKey === 'settings-page') ||
  items[0]

const createMergedSettingsPage = (items) => {
  const preferred = pickPreferredSettingsPage(items)
  if (!preferred) {
    return null
  }

  return {
    ...preferred,
    menuKey: MERGED_SETTINGS_MENU_KEY,
    menuName: MERGED_SETTINGS_MENU_NAME,
    routeAliases: items.map(item => item.routePath).filter(Boolean),
    originalMenuKeys: items.map(item => item.menuKey)
  }
}

export const mergeNavigationMenus = (menuTree = []) => {
  const tree = clone(menuTree).filter(menu => menu?.menuType !== 'button')

  const mergeDirectChildren = (children = []) => {
    const settingsPages = children.filter(isSettingsPage)
    if (!settingsPages.length) {
      return children
    }

    let inserted = false
    return children.reduce((result, child) => {
      if (isSettingsPage(child)) {
        if (!inserted) {
          const mergedPage = createMergedSettingsPage(settingsPages)
          if (mergedPage) {
            result.push(mergedPage)
          }
          inserted = true
        }
        return result
      }

      result.push(child)
      return result
    }, [])
  }

  const injectOrgChartMenu = (menu) => {
    if (menu.menuKey !== ORG_MODULE_MENU_KEY) {
      return menu
    }

    const existingChildren = menu.children || []
    const hasChartMenu = existingChildren.some(child => child.routePath === ORG_CHART_ROUTE_PATH)

    return {
      ...menu,
      children: [
        ...existingChildren,
        ...(hasChartMenu ? [] : [{
          menuKey: 'org-chart',
          parentMenuKey: menu.menuKey,
          menuName: '组织图',
          menuType: 'page',
          sortOrder: 13,
          routePath: ORG_CHART_ROUTE_PATH,
          componentPath: 'views/org/OrgChartView.vue',
          icon: null,
          isVisible: true,
          permissionCode: 'page.org.chart',
          children: []
        }])
      ]
    }
  }

  const injectProcessDesignerMenu = (menu) => {
    if (menu.menuKey !== WORKFLOW_MODULE_MENU_KEY) {
      return menu
    }

    const existingChildren = menu.children || []
    const hasDesignerMenu = existingChildren.some(child => child.routePath === PROCESS_DESIGNER_ROUTE_PATH)

    return {
      ...menu,
      children: [
        ...existingChildren,
        ...(hasDesignerMenu ? [] : [{
          menuKey: 'process-designer-page',
          parentMenuKey: menu.menuKey,
          menuName: '流程设计器',
          menuType: 'page',
          sortOrder: 3,
          routePath: PROCESS_DESIGNER_ROUTE_PATH,
          componentPath: 'views/workflow/ProcessDesigner.vue',
          icon: null,
          isVisible: true,
          permissionCode: 'page.process.designer',
          children: []
        }])
      ]
    }
  }

  const injectProcessRequestsMenu = (menu) => {
    if (menu.menuKey !== WORKFLOW_MODULE_MENU_KEY) {
      return menu
    }

    const existingChildren = menu.children || []
    const hasRequestsMenu = existingChildren.some(child => child.routePath === PROCESS_REQUESTS_ROUTE_PATH)

    return {
      ...menu,
      children: [
        ...existingChildren,
        ...(hasRequestsMenu ? [] : [{
          menuKey: 'process-requests-page',
          parentMenuKey: menu.menuKey,
          menuName: '流程申请',
          menuType: 'page',
          sortOrder: 450,
          routePath: PROCESS_REQUESTS_ROUTE_PATH,
          componentPath: 'views/workflow/ProcessRequests.vue',
          icon: null,
          isVisible: true,
          permissionCode: 'page.process.requests',
          children: []
        }])
      ]
    }
  }

  const normalizeNode = (menu) => {
    const children = mergeDirectChildren(
      (menu.children || [])
        .filter(child => child?.menuType !== 'button')
        .map(normalizeNode)
    )

    return injectProcessRequestsMenu(injectProcessDesignerMenu(injectOrgChartMenu({
      ...menu,
      children
    })))
  }

  return tree.map(normalizeNode)
}

export const flattenMenus = (menus = []) => {
  const result = []

  const loop = (items, parents = []) => {
    items.forEach(item => {
      result.push({ ...item, parents })
      if (item.children?.length) {
        loop(item.children, [...parents, item])
      }
    })
  }

  loop(menus)
  return result
}

export const findMenuByRoutePath = (menus = [], path = '') =>
  menus.find(item => item.routePath === path || item.routeAliases?.includes(path))

export const resolveMergedSettingsPath = (menuTree = [], fallback = '/settings') => {
  const mergedMenus = mergeNavigationMenus(menuTree)
  const flatMenus = flattenMenus(mergedMenus)
  return findMenuByRoutePath(flatMenus, '/config')?.routePath ||
    flatMenus.find(item => item.menuKey === MERGED_SETTINGS_MENU_KEY)?.routePath ||
    fallback
}

export const mergedSettingsMenuKey = MERGED_SETTINGS_MENU_KEY
