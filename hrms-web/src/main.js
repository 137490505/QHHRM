import { createApp } from 'vue'
import { createPinia } from 'pinia'
import Antd from 'ant-design-vue'
import App from './App.vue'
import router, { registerDynamicRoutes } from './router'
import { setupPermissionDirective } from './directives/permission'
import 'ant-design-vue/dist/reset.css'
import './styles/common.css'

const app = createApp(App)
const pinia = createPinia()

const getCachedMenuTree = () => {
  try {
    return JSON.parse(localStorage.getItem('menuTree') || '[]')
  } catch {
    return []
  }
}

app.use(pinia)
registerDynamicRoutes(getCachedMenuTree())
app.use(router)
app.use(Antd)
setupPermissionDirective(app)

app.mount('#app')
