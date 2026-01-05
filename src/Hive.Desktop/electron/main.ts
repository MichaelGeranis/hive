import { app, BrowserWindow, shell, dialog } from 'electron'
import * as path from 'path'
import * as fs from 'fs'
import { startBackend, stopBackend } from './backend'

const isDev = process.env.NODE_ENV === 'development' || !app.isPackaged

let mainWindow: BrowserWindow | null = null
let backendStarted = false

// Log file for debugging packaged app issues
const logFile = isDev ? null : path.join(app.getPath('userData'), 'hive-debug.log')

function log(message: string) {
  const timestamp = new Date().toISOString()
  const logMessage = `[${timestamp}] ${message}`
  console.log(logMessage)

  if (logFile) {
    try {
      fs.appendFileSync(logFile, logMessage + '\n')
    } catch {
      // Ignore logging errors
    }
  }
}

function createWindow() {
  log('Creating main window...')

  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1024,
    minHeight: 768,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    },
    titleBarStyle: 'hiddenInset',
    trafficLightPosition: { x: 15, y: 15 },
    show: false,
    backgroundColor: '#1e293b' // Match loading screen background
  })

  // Show window when ready
  mainWindow.once('ready-to-show', () => {
    log('Window ready to show')
    mainWindow?.show()
    mainWindow?.focus()
  })

  // Open external links in browser
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url)
    return { action: 'deny' }
  })

  // Log any load errors
  mainWindow.webContents.on('did-fail-load', (_event, errorCode, errorDescription) => {
    log(`Failed to load: ${errorCode} - ${errorDescription}`)
  })

  mainWindow.webContents.on('crashed', () => {
    log('Renderer process crashed')
  })

  if (isDev) {
    log('Loading development URL: http://localhost:5173')
    mainWindow.loadURL('http://localhost:5173')
    mainWindow.webContents.openDevTools()
  } else {
    const indexPath = path.join(__dirname, '../dist/index.html')
    log(`Loading production file: ${indexPath}`)

    if (!fs.existsSync(indexPath)) {
      log(`ERROR: index.html not found at ${indexPath}`)
      dialog.showErrorBox('Application Error', `Could not find application files.\n\nExpected: ${indexPath}`)
    }

    mainWindow.loadFile(indexPath)
    // Temporarily enable DevTools for debugging
    mainWindow.webContents.openDevTools()
  }

  mainWindow.on('closed', () => {
    log('Window closed')
    mainWindow = null
  })
}

async function initializeApp() {
  log(`App initializing. isDev: ${isDev}, platform: ${process.platform}, arch: ${process.arch}`)
  log(`App path: ${app.getAppPath()}`)
  log(`Resources path: ${process.resourcesPath}`)
  log(`User data path: ${app.getPath('userData')}`)

  // Start backend before creating window (only in production)
  if (!isDev) {
    try {
      log('Starting backend...')
      await startBackend()
      backendStarted = true
      log('Backend started successfully')
    } catch (error) {
      log(`Failed to start backend: ${error}`)

      // Show error dialog but still create window
      // The React app will show error state
      dialog.showMessageBox({
        type: 'warning',
        title: 'Backend Warning',
        message: 'The backend service failed to start. Some features may not work correctly.',
        detail: String(error),
        buttons: ['Continue Anyway', 'Quit']
      }).then(({ response }) => {
        if (response === 1) {
          app.quit()
          return
        }
      })
    }
  } else {
    log('Development mode: expecting backend to run separately')
  }

  createWindow()
}

app.whenReady().then(initializeApp).catch((error) => {
  log(`App initialization failed: ${error}`)
  dialog.showErrorBox('Startup Error', `Failed to start application: ${error}`)
  app.quit()
})

app.on('activate', () => {
  log('App activated')
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow()
  }
})

app.on('window-all-closed', () => {
  log('All windows closed')
  if (process.platform !== 'darwin') {
    app.quit()
  }
})

// Clean up backend on quit
app.on('before-quit', async (event) => {
  log('App quitting...')
  if (backendStarted) {
    event.preventDefault()
    await stopBackend()
    backendStarted = false
    app.quit()
  }
})

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
  log(`Uncaught exception: ${error.stack || error}`)
})

process.on('unhandledRejection', (reason) => {
  log(`Unhandled rejection: ${reason}`)
})
