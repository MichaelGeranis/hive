import { app, BrowserWindow, shell, dialog, ipcMain } from 'electron'
import * as path from 'path'
import * as fs from 'fs'
import { startBackend, stopBackend } from './backend'
import { fetchAppleCalendarEvents, extractDirectReportName, checkCalendarAccess } from './appleCalendar'

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

// IPC Handlers for Apple Calendar integration
function setupIpcHandlers() {
  // Check if Calendar access is available
  ipcMain.handle('calendar:checkAccess', async () => {
    try {
      const hasAccess = await checkCalendarAccess()
      log(`Calendar access check: ${hasAccess}`)
      return { success: true, hasAccess }
    } catch (error) {
      log(`Calendar access check failed: ${error}`)
      return { success: false, error: String(error) }
    }
  })

  // Fetch calendar events
  ipcMain.handle('calendar:fetchEvents', async (_event, calendarEmail: string) => {
    try {
      log(`Fetching calendar events from: ${calendarEmail}`)
      const events = await fetchAppleCalendarEvents(calendarEmail)
      log(`Fetched ${events.length} events`)
      return { success: true, events }
    } catch (error) {
      log(`Failed to fetch calendar events: ${error}`)
      return { success: false, error: String(error) }
    }
  })

  // Sync calendar events to backend
  ipcMain.handle('calendar:sync', async (_event, calendarEmail: string) => {
    try {
      log(`Starting calendar sync for: ${calendarEmail}`)

      // Fetch events from Apple Calendar
      const events = await fetchAppleCalendarEvents(calendarEmail)
      log(`Fetched ${events.length} events from Apple Calendar`)

      // Convert to API format
      const apiEvents = events.map(event => ({
        id: event.id,
        title: event.title,
        startDate: new Date(event.startDate).toISOString(),
        endDate: new Date(event.endDate).toISOString(),
        location: event.location,
        calendar: event.calendar
      }))

      // Send to backend API
      const response = await fetch('http://localhost:5000/api/calendarsync/sync', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Basic ' + Buffer.from('admin:admin123').toString('base64')
        },
        body: JSON.stringify({
          calendarEmail: calendarEmail,
          events: apiEvents
        })
      })

      if (!response.ok) {
        const errorText = await response.text()
        throw new Error(`API error: ${response.status} - ${errorText}`)
      }

      const result = await response.json()
      log(`Sync completed. Created: ${result.createdCount}, Updated: ${result.updatedCount}`)

      return { success: true, result }
    } catch (error) {
      log(`Calendar sync failed: ${error}`)
      return { success: false, error: String(error) }
    }
  })
}

async function initializeApp() {
  log(`App initializing. isDev: ${isDev}, platform: ${process.platform}, arch: ${process.arch}`)
  log(`App path: ${app.getAppPath()}`)
  log(`Resources path: ${process.resourcesPath}`)
  log(`User data path: ${app.getPath('userData')}`)

  // Setup IPC handlers
  setupIpcHandlers()
  log('IPC handlers registered')

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

  // Note: Auto-sync disabled on startup to prevent Calendar app crashes.
  // Users can manually sync from the Settings page.
  log('Auto-sync disabled on startup. Use Settings page to manually sync calendar.')
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
