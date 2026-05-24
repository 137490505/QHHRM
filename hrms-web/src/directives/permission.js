import { AUTH_CONTEXT_CHANGED_EVENT } from '../store/user'

const PERMISSION_HANDLER_KEY = Symbol('permission-handler')

const getPermissions = () => {
  try {
    return JSON.parse(localStorage.getItem('permissions') || '[]')
  } catch {
    return []
  }
}

const getUserInfo = () => {
  try {
    return JSON.parse(localStorage.getItem('userInfo') || 'null')
  } catch {
    return null
  }
}

const applyPermission = (el, binding) => {
  const permissionCode = binding.value
  if (!permissionCode) {
    el.style.display = ''
    return
  }

  const userInfo = getUserInfo()
  if (userInfo?.isAdmin) {
    el.style.display = ''
    return
  }

  const permissions = getPermissions()
  el.style.display = permissions.includes(permissionCode) ? '' : 'none'
}

export const setupPermissionDirective = (app) => {
  app.directive('permission', {
    mounted(el, binding) {
      applyPermission(el, binding)
      const handler = () => applyPermission(el, binding)
      window.addEventListener(AUTH_CONTEXT_CHANGED_EVENT, handler)
      el[PERMISSION_HANDLER_KEY] = handler
    },
    updated(el, binding) {
      applyPermission(el, binding)
    },
    unmounted(el) {
      const handler = el[PERMISSION_HANDLER_KEY]
      if (handler) {
        window.removeEventListener(AUTH_CONTEXT_CHANGED_EVENT, handler)
        delete el[PERMISSION_HANDLER_KEY]
      }
    }
  })
}
