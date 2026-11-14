import { app, BrowserWindow, shell, dialog, ipcMain, safeStorage } from 'electron'
import * as path from 'path'
import * as fs from 'fs'
import * as os from 'os'
import { pathToFileURL } from 'url'
import { startBackend, stopBackend } from './backend'
import { isAppUrl, isSafeExternalUrl } from './navigation'
import { loadOrCreateDatabaseKey, newApiPassword } from './secrets'
import {
  startLogSession,
  setLogWindow,
  recordLog,
  writeRendererEntry,
  getBufferedEntries,
  listSessions,
  readSession,
  logsDirectory,
} from './logger'

// Hive shows dates as dd/MM/yyyy. Native date pickers are drawn by Chromium in the app's
// locale, so pin it to en-GB (dd/mm/yyyy) rather than following the OS, which on an en-US
// system would show mm/dd/yyyy. This must run before the app is ready.
app.commandLine.appendSwitch('lang', 'en-GB')

const isDev = process.env.NODE_ENV === 'development' || !app.isPackaged

let mainWindow: BrowserWindow | null = null
let backendStarted = false

// A fresh API password for every launch; see secrets.ts
const apiPassword = newApiPassword()

// Log file for debugging packaged app issues
const logFile = isDev ? null : path.join(app.getPath('userData'), 'hive-debug.log')

function log(message: string, level: 'info' | 'warn' | 'error' | 'debug' = 'info', details?: string) {
  if (logFile) {
    try {
      fs.appendFileSync(logFile, `[${new Date().toISOString()}] [${level.toUpperCase()}] ${message}\n`)
    } catch {
      // Ignore logging errors
    }
  }

  recordLog(level, message, 'main', details)
}

startLogSession()

// The Logs page: the renderer persists what it captures, and reads earlier sessions back
ipcMain.on('log:write', (_event, entry: unknown) => writeRendererEntry(entry))
ipcMain.handle('log:get-buffered', () => getBufferedEntries())
ipcMain.handle('log:list-sessions', () => listSessions())
ipcMain.handle('log:read-session', (_event, id: unknown) => readSession(id))
ipcMain.handle('log:open-folder', () => shell.openPath(logsDirectory()))
// The window asks once, synchronously, for the API credentials of this launch. In development the
// backend runs from source with its development credentials, so there is nothing to hand over.
ipcMain.on('auth:get-api-credentials', (event) => {
  event.returnValue = isDev ? null : { username: 'admin', password: apiPassword }
})
ipcMain.handle('app:get-info', () => ({
  version: app.getVersion(),
  isPackaged: app.isPackaged,
  platform: process.platform,
  arch: process.arch,
  osRelease: os.release(),
  electron: process.versions.electron,
  chrome: process.versions.chrome,
  node: process.versions.node,
}))

function createWindow() {
  log('Creating main window...')

  mainWindow = new BrowserWindow({
    width: 1700,
    height: 1150,
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

  setLogWindow(mainWindow)

  // Show window when ready
  mainWindow.once('ready-to-show', () => {
    log('Window ready to show')
    mainWindow?.show()
    mainWindow?.focus()
  })

  // Web and mail links open in the browser; nothing else is handed to the operating system,
  // and the window itself never leaves the app's page
  const openExternally = (url: string) => {
    if (isSafeExternalUrl(url)) {
      shell.openExternal(url)
    } else {
      log(`Blocked opening a link with an unsupported scheme: ${url.split(':')[0]}:`, 'warn')
    }
  }
  const appUrl = isDev ? 'http://localhost:5173' : pathToFileURL(path.join(__dirname, '../dist/index.html')).href

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    openExternally(url)
    return { action: 'deny' }
  })

  mainWindow.webContents.on('will-navigate', (event, url) => {
    if (!isAppUrl(url, appUrl)) {
      event.preventDefault()
      openExternally(url)
    }
  })

  // Log any load errors
  mainWindow.webContents.on('did-fail-load', (_event, errorCode, errorDescription) => {
    log(`Failed to load: ${errorCode} - ${errorDescription}`, 'error')
  })

  mainWindow.webContents.on('render-process-gone', (_event, details) => {
    log(`Renderer process gone: ${details.reason} (exit code ${details.exitCode})`, 'error')
  })

  if (isDev) {
    log('Loading development URL: http://localhost:5173')
    mainWindow.loadURL('http://localhost:5173')
    mainWindow.webContents.openDevTools()
  } else {
    const indexPath = path.join(__dirname, '../dist/index.html')
    log(`Loading production file: ${indexPath}`)

    if (!fs.existsSync(indexPath)) {
      log(`index.html not found at ${indexPath}`, 'error')
      dialog.showErrorBox('Application Error', `Could not find application files.\n\nExpected: ${indexPath}`)
    }

    mainWindow.loadFile(indexPath)
    // Temporarily enable DevTools for debugging
    // mainWindow.webContents.openDevTools()
  }

  mainWindow.on('closed', () => {
    log('Window closed')
    mainWindow = null
    setLogWindow(null)
  })
}

async function initializeApp() {
  log(`App initializing. isDev: ${isDev}, platform: ${process.platform}, arch: ${process.arch}`)
  log(`App path: ${app.getAppPath()}`)
  log(`Resources path: ${process.resourcesPath}`)
  log(`User data path: ${app.getPath('userData')}`)

  // Start backend before creating window (only in production)
  if (!isDev) {
    let databaseKey: string | null
    try {
      const result = loadOrCreateDatabaseKey(path.join(app.getPath('userData'), 'database-key'), safeStorage)
      databaseKey = result.key
      if (result.encrypted === false) {
        log(`Database stays unencrypted: ${result.reason}`, 'warn')
      } else {
        log(result.created ? 'Created the database encryption key' : 'Loaded the database encryption key')
      }
    } catch (error) {
      // Never fall back to a new key or to no key: either would lock the user out of their data
      log(`Could not read the database encryption key: ${error}`, 'error')
      dialog.showErrorBox(
        'Hive cannot open your data',
        'The key that encrypts your Hive database could not be read from the system keychain. ' +
          'Hive will close without changing anything. Check that you are signed in as the same user.'
      )
      app.quit()
      return
    }

    try {
      log('Starting backend...')
      await startBackend({ apiPassword, databaseKey })
      backendStarted = true
      log('Backend started successfully')
    } catch (error) {
      log(`Failed to start backend: ${error}`, 'error')

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
  log(`App initialization failed: ${error}`, 'error')
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
  log(`Uncaught exception: ${error.message}`, 'error', error.stack)
})

process.on('unhandledRejection', (reason) => {
  log(`Unhandled rejection: ${reason}`, 'error', reason instanceof Error ? reason.stack : undefined)
})
