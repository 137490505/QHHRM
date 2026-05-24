import { spawn } from 'node:child_process'
import path from 'node:path'
import { ensureTestDatabase, toTestConnectionString, getBackendConnectionString } from './db.mjs'

await ensureTestDatabase()

const workspaceRoot = path.resolve(process.cwd(), '..')
const backendProjectPath = path.join(workspaceRoot, 'src', 'HRMS.API', 'HRMS.API.csproj')
const connectionString = toTestConnectionString(getBackendConnectionString())

const child = spawn(
  'dotnet',
  ['run', '--project', backendProjectPath, '--no-launch-profile'],
  {
    cwd: workspaceRoot,
    stdio: 'inherit',
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Testing',
      ASPNETCORE_URLS: 'http://127.0.0.1:5000',
      ConnectionStrings__DefaultConnection: connectionString
    }
  }
)

child.on('exit', (code) => {
  process.exit(code ?? 0)
})
