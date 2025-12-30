import { spawn, ChildProcess } from 'child_process'
import * as path from 'path'
import * as fs from 'fs'
import { app } from 'electron'
import * as http from 'http'

const BACKEND_PORT = 5000
const HEALTH_CHECK_URL = `http://localhost:${BACKEND_PORT}/health`
const HEALTH_CHECK_INTERVAL = 500
const HEALTH_CHECK_TIMEOUT = 30000

let backendProcess: ChildProcess | null = null
let isShuttingDown = false

/**
 * Gets the path to the backend executable based on platform and packaging
 */
function getBackendPath(): string {
  const isDev = process.env.NODE_ENV === 'development' || !app.isPackaged

  if (isDev) {
    // In development, backend should be run separately
    // Return empty to indicate we should skip spawning
    return ''
  }

  const platform = process.platform
  const arch = process.arch

  let rid: string
  if (platform === 'darwin') {
    rid = arch === 'arm64' ? 'osx-arm64' : 'osx-x64'
  } else if (platform === 'win32') {
    rid = 'win-x64'
  } else {
    rid = 'linux-x64'
  }

  const executableName = platform === 'win32' ? 'Hive.Api.exe' : 'Hive.Api'

  // In packaged app, backend is in the resources folder
  const resourcesPath = process.resourcesPath
  return path.join(resourcesPath, 'backend', rid, executableName)
}

/**
 * Checks if the backend is healthy by calling the health endpoint
 */
function checkHealth(): Promise<boolean> {
  return new Promise((resolve) => {
    const req = http.get(HEALTH_CHECK_URL, (res) => {
      resolve(res.statusCode === 200)
    })

    req.on('error', () => {
      resolve(false)
    })

    req.setTimeout(2000, () => {
      req.destroy()
      resolve(false)
    })
  })
}

/**
 * Waits for the backend to become healthy
 */
async function waitForHealth(): Promise<boolean> {
  const startTime = Date.now()

  while (Date.now() - startTime < HEALTH_CHECK_TIMEOUT) {
    if (await checkHealth()) {
      return true
    }
    await new Promise((resolve) => setTimeout(resolve, HEALTH_CHECK_INTERVAL))
  }

  return false
}

/**
 * Starts the backend process
 */
export async function startBackend(): Promise<void> {
  const backendPath = getBackendPath()

  // In development, don't spawn - assume backend is running separately
  if (!backendPath) {
    console.log('Development mode: skipping backend spawn')

    // Wait for external backend to be ready
    const isHealthy = await waitForHealth()
    if (!isHealthy) {
      console.warn('Backend health check failed - ensure backend is running on port 5000')
    }
    return
  }

  console.log(`Starting backend from: ${backendPath}`)

  // Check if backend executable exists
  if (!fs.existsSync(backendPath)) {
    throw new Error(`Backend executable not found at: ${backendPath}`)
  }

  // Ensure executable permissions on Unix
  if (process.platform !== 'win32') {
    try {
      fs.chmodSync(backendPath, 0o755)
    } catch (error) {
      console.warn('Could not set executable permissions:', error)
    }
  }

  // Set environment variables for production mode
  const env = {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Production',
    UseInMemoryDatabase: 'false',
    ASPNETCORE_URLS: `http://localhost:${BACKEND_PORT}`
  }

  backendProcess = spawn(backendPath, [], {
    env,
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true
  })

  // Log backend output
  backendProcess.stdout?.on('data', (data) => {
    console.log(`[Backend] ${data.toString().trim()}`)
  })

  backendProcess.stderr?.on('data', (data) => {
    console.error(`[Backend Error] ${data.toString().trim()}`)
  })

  backendProcess.on('error', (error) => {
    console.error('Failed to start backend:', error)
  })

  backendProcess.on('exit', (code, signal) => {
    if (!isShuttingDown) {
      console.error(`Backend exited unexpectedly with code ${code}, signal ${signal}`)
    }
    backendProcess = null
  })

  // Wait for backend to become healthy
  console.log('Waiting for backend to become healthy...')
  const isHealthy = await waitForHealth()

  if (isHealthy) {
    console.log('Backend is healthy and ready')
  } else {
    console.error('Backend failed to become healthy within timeout')
    throw new Error('Backend failed to start')
  }
}

/**
 * Stops the backend process gracefully
 */
export async function stopBackend(): Promise<void> {
  if (!backendProcess) {
    return
  }

  isShuttingDown = true
  console.log('Stopping backend...')

  return new Promise((resolve) => {
    const timeout = setTimeout(() => {
      console.log('Backend did not exit gracefully, forcing kill')
      backendProcess?.kill('SIGKILL')
      resolve()
    }, 5000)

    backendProcess!.on('exit', () => {
      clearTimeout(timeout)
      console.log('Backend stopped')
      resolve()
    })

    // Send SIGTERM for graceful shutdown
    if (process.platform === 'win32') {
      backendProcess!.kill()
    } else {
      backendProcess!.kill('SIGTERM')
    }
  })
}

/**
 * Returns whether the backend process is running
 */
export function isBackendRunning(): boolean {
  return backendProcess !== null && !backendProcess.killed
}

/**
 * Returns the backend URL
 */
export function getBackendUrl(): string {
  return `http://localhost:${BACKEND_PORT}`
}
