import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5000'
  const devPort = Number(env.VITE_DEV_PORT || 3000)

  return {
    plugins: [vue()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, 'src')
      }
    },
    server: {
      port: devPort,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
          ws: true,
          pathRewrite: { '^/api': '/api' }
        }
      },
      headers: {
        'Content-Security-Policy': `default-src 'self' 'unsafe-inline' 'unsafe-eval'; script-src 'self' 'unsafe-eval' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:; connect-src 'self' ${apiTarget} ws://localhost:5000 ws://127.0.0.1:5000; object-src 'none'; base-uri 'self'`
      }
    }
  }
})
