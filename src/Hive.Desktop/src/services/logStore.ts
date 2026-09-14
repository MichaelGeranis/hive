export type LogEntry = {
  id: number
  timestamp: string
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
  source: 'main' | 'renderer'
}

type LogListener = (logs: LogEntry[]) => void

const MAX_LOGS = 1000
let logs: LogEntry[] = []
let logIdCounter = 0
const listeners: Set<LogListener> = new Set()
let initialized = false

// Original console methods
let originalConsoleLog: typeof console.log
let originalConsoleWarn: typeof console.warn
let originalConsoleError: typeof console.error
let originalConsoleDebug: typeof console.debug

function notifyListeners() {
  listeners.forEach(listener => listener([...logs]))
}

function addLog(level: LogEntry['level'], message: string, source: LogEntry['source'] = 'renderer') {
  const entry: LogEntry = {
    id: ++logIdCounter,
    timestamp: new Date().toISOString(),
    level,
    message,
    source
  }

  logs = [...logs.slice(-(MAX_LOGS - 1)), entry]
  notifyListeners()
}

export function initializeLogCapture() {
  if (initialized) return
  initialized = true

  // Store original console methods
  originalConsoleLog = console.log
  originalConsoleWarn = console.warn
  originalConsoleError = console.error
  originalConsoleDebug = console.debug

  // Override console methods
  console.log = (...args: unknown[]) => {
    originalConsoleLog.apply(console, args)
    const message = args.map(arg =>
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ')
    addLog('info', message, 'renderer')
  }

  console.warn = (...args: unknown[]) => {
    originalConsoleWarn.apply(console, args)
    const message = args.map(arg =>
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ')
    addLog('warn', message, 'renderer')
  }

  console.error = (...args: unknown[]) => {
    originalConsoleError.apply(console, args)
    const message = args.map(arg =>
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ')
    addLog('error', message, 'renderer')
  }

  console.debug = (...args: unknown[]) => {
    originalConsoleDebug.apply(console, args)
    const message = args.map(arg =>
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ')
    addLog('debug', message, 'renderer')
  }

  // Listen for logs from main process
  if (window.electronAPI?.onMainLog) {
    window.electronAPI.onMainLog((entry) => {
      addLog(entry.level, entry.message, 'main')
    })
  }

  // Add initial log entry
  addLog('info', 'Log capture initialized', 'renderer')
}

export function getLogs(): LogEntry[] {
  return [...logs]
}

export function clearLogs() {
  logs = []
  logIdCounter = 0
  notifyListeners()
}

export function subscribe(listener: LogListener): () => void {
  listeners.add(listener)
  // Immediately notify with current logs
  listener([...logs])

  return () => {
    listeners.delete(listener)
  }
}

export function getLogStats() {
  return {
    total: logs.length,
    info: logs.filter(l => l.level === 'info').length,
    warn: logs.filter(l => l.level === 'warn').length,
    error: logs.filter(l => l.level === 'error').length,
    debug: logs.filter(l => l.level === 'debug').length,
  }
}
