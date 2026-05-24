import { spawn } from 'node:child_process'

const child = spawn(
  'npm',
  ['run', 'dev', '--', '--host', '127.0.0.1', '--port', '3001'],
  {
    cwd: process.cwd(),
    stdio: 'inherit',
    shell: true,
    env: {
      ...process.env,
      VITE_DEV_PORT: '3001',
      VITE_API_PROXY_TARGET: 'http://127.0.0.1:5000'
    }
  }
)

child.on('exit', (code) => {
  process.exit(code ?? 0)
})
