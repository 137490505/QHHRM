import { defineStore } from 'pinia'

export const AUTH_CONTEXT_CHANGED_EVENT = 'hrms:auth-context-changed'

const parseJson = (key, fallback) => {
  try {
    const raw = localStorage.getItem(key)
    return raw ? JSON.parse(raw) : fallback
  } catch {
    return fallback
  }
}

const emitAuthContextChanged = () => {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent(AUTH_CONTEXT_CHANGED_EVENT))
  }
}

export const useUserStore = defineStore('user', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    userInfo: parseJson('userInfo', null),
    permissions: parseJson('permissions', []),
    menuTree: parseJson('menuTree', []),
    routesRegistered: false
  }),

  getters: {
    firstPagePath: (state) => {
      const stack = [...state.menuTree]
      while (stack.length) {
        const current = stack.shift()
        if (!current) {
          continue
        }
        if (current.menuType === 'page' && current.routePath) {
          return current.routePath
        }
        if (current.children?.length) {
          stack.unshift(...current.children)
        }
      }
      return '/dashboard'
    }
  },

  actions: {
    setToken(token) {
      this.token = token
      localStorage.setItem('token', token)
    },

    setUserInfo(userInfo) {
      this.userInfo = userInfo
      localStorage.setItem('userInfo', JSON.stringify(userInfo || null))
      emitAuthContextChanged()
    },

    setPermissions(permissions) {
      this.permissions = permissions || []
      localStorage.setItem('permissions', JSON.stringify(this.permissions))
      emitAuthContextChanged()
    },

    setMenuTree(menuTree) {
      this.menuTree = menuTree || []
      localStorage.setItem('menuTree', JSON.stringify(this.menuTree))
    },

    setRoutesRegistered(registered) {
      this.routesRegistered = registered
    },

    setAuthContext(payload) {
      this.setToken(payload.token)
      this.setUserInfo(payload.userInfo)
      this.setPermissions(payload.permissions)
      this.setMenuTree(payload.menus)
    },

    hasPermission(permissionCode) {
      if (!permissionCode) {
        return true
      }
      if (this.userInfo?.isAdmin) {
        return true
      }
      return this.permissions.includes(permissionCode)
    },

    logout() {
      this.token = ''
      this.userInfo = null
      this.permissions = []
      this.menuTree = []
      this.routesRegistered = false
      localStorage.removeItem('token')
      localStorage.removeItem('userInfo')
      localStorage.removeItem('permissions')
      localStorage.removeItem('menuTree')
      emitAuthContextChanged()
    }
  }
})
