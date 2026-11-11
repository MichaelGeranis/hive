export type LogLevel = 'info' | 'warn' | 'error' | 'debug'
export type LogSource = 'main' | 'renderer' | 'backend' | 'api'

export type LogEntry = {
  id: number
  timestamp: string
  level: LogLevel
  message: string
  source: LogSource
  /** Stack trace, response body or anything else too long for the one-line message */
  details?: string
  /** Included in a report that was copied or saved */
  reported?: boolean
}

export type LogSession = {
  id: string
  startedAt: string
  size: number
  isCurrent: boolean
}

export type AppInfo = {
  version: string
  isPackaged: boolean
  platform: string
  arch: string
  osRelease: string
  electron: string
  chrome: string
  node: string
}

type BridgeLogEntry = {
  seq?: number
  timestamp?: string
  level: LogLevel
  message: string
  source?: LogSource
  details?: string
}

type LogListener = (logs: LogEntry[]) => void

const MAX_LOGS = 1000
let logs: LogEntry[] = []
let logIdCounter = 0
let lastSeenId = 0
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

function persist(entry: LogEntry) {
  try {
    window.electronAPI?.logs?.write({
      timestamp: entry.timestamp,
      level: entry.level,
      source: entry.source,
      message: entry.message,
      ...(entry.details ? { details: entry.details } : {}),
    })
  } catch {
    // Saving to disk is best effort; the entry is still shown
  }
}

function addLog(
  level: LogLevel,
  message: string,
  source: LogSource = 'renderer',
  details?: string,
  timestamp = new Date().toISOString()
) {
  const entry: LogEntry = {
    id: ++logIdCounter,
    timestamp,
    level,
    message,
    source,
    ...(details ? { details } : {}),
  }

  logs = [...logs.slice(-(MAX_LOGS - 1)), entry]
  // The main process saves its own and the backend's entries
  if (source === 'renderer' || source === 'api') persist(entry)
  notifyListeners()
}

function safeStringify(value: unknown): string {
  const seen = new WeakSet<object>()
  try {
    return JSON.stringify(
      value,
      (_key, v) => {
        if (typeof v === 'object' && v !== null) {
          if (seen.has(v)) return '[Circular]'
          seen.add(v)
        }
        return v
      },
      2
    ) ?? String(value)
  } catch {
    return String(value)
  }
}

type AxiosLikeError = Error & {
  isAxiosError: true
  code?: string
  config?: { method?: string; url?: string }
  response?: { status?: number; statusText?: string; data?: unknown }
}

function isAxiosLikeError(value: unknown): value is AxiosLikeError {
  return value instanceof Error && (value as { isAxiosError?: unknown }).isAxiosError === true
}

/**
 * Turns one console argument into text. An Error serialises to `{}` through JSON, which
 * loses everything a developer needs, so errors are written out by hand: the name and
 * message go in the message, the stack trace and any response body in the details.
 */
export function describeValue(value: unknown): { text: string; details?: string } {
  if (isAxiosLikeError(value)) {
    const method = value.config?.method?.toUpperCase() ?? 'GET'
    const request = value.config?.url ? ` (${method} ${value.config.url})` : ''
    const response = value.response
    const details = [
      response ? `Status: ${response.status ?? ''} ${response.statusText ?? ''}`.trim() : `Code: ${value.code ?? 'no response'}`,
      response?.data !== undefined && response.data !== '' ? `Response: ${typeof response.data === 'string' ? response.data : safeStringify(response.data)}` : '',
      value.stack ?? '',
    ].filter(Boolean).join('\n')
    return { text: `${value.name}: ${value.message}${request}`, details }
  }

  if (value instanceof Error) {
    const cause = (value as { cause?: unknown }).cause
    const details = [
      value.stack ?? '',
      cause !== undefined ? `Caused by: ${describeValue(cause).text}` : '',
    ].filter(Boolean).join('\n')
    return { text: `${value.name}: ${value.message}`, ...(details ? { details } : {}) }
  }

  if (typeof value === 'object' && value !== null) return { text: safeStringify(value) }
  return { text: String(value) }
}

function describeArgs(args: unknown[]): { message: string; details?: string } {
  const described = args.map(describeValue)
  const details = described.map(d => d.details).filter(Boolean).join('\n\n')
  return { message: described.map(d => d.text).join(' '), ...(details ? { details } : {}) }
}

/**
 * Records an entry that did not come through the console, such as a failed API call.
 * It is still echoed to the developer tools console.
 */
export function recordLog(level: LogLevel, message: string, source: LogSource, details?: string) {
  // Once capture is on, the console methods are ours; echo through the originals instead
  const echo = initialized
    ? { info: originalConsoleLog, warn: originalConsoleWarn, error: originalConsoleError, debug: originalConsoleDebug }[level]
    : console[level === 'info' ? 'log' : level]
  echo.call(console, message)
  addLog(level, message, source, details)
}

export function initializeLogCapture() {
  if (initialized) return
  initialized = true

  // Store original console methods
  originalConsoleLog = console.log
  originalConsoleWarn = console.warn
  originalConsoleError = console.error
  originalConsoleDebug = console.debug

  const capture = (level: LogLevel, original: typeof console.log) => (...args: unknown[]) => {
    original.apply(console, args)
    const { message, details } = describeArgs(args)
    addLog(level, message, 'renderer', details)
  }

  // Override console methods
  console.log = capture('info', originalConsoleLog)
  console.warn = capture('warn', originalConsoleWarn)
  console.error = capture('error', originalConsoleError)
  console.debug = capture('debug', originalConsoleDebug)

  // Errors nobody caught never reach the console methods above
  window.addEventListener('error', (event) => {
    const described = event.error !== undefined && event.error !== null ? describeValue(event.error) : { text: event.message }
    const location = event.filename ? `at ${event.filename}:${event.lineno}:${event.colno}` : ''
    addLog('error', `Uncaught ${described.text}`, 'renderer', described.details || location || undefined)
  })
  window.addEventListener('unhandledrejection', (event) => {
    const described = describeValue(event.reason)
    addLog('error', `Unhandled promise rejection: ${described.text}`, 'renderer', described.details)
  })

  connectMainProcess()

  // Add initial log entry
  addLog('info', 'Log capture initialized', 'renderer')
}

/**
 * Receives main-process and backend entries. Those written before this page loaded are
 * fetched once; `seq` keeps an entry that arrives both ways from being shown twice.
 */
function connectMainProcess() {
  const bridge = window.electronAPI
  if (!bridge?.onMainLog) return

  const addMainEntry = (entry: BridgeLogEntry) =>
    addLog(entry.level, entry.message, entry.source ?? 'main', entry.details, entry.timestamp)

  if (!bridge.logs?.getBuffered) {
    bridge.onMainLog(addMainEntry)
    return
  }

  let lastBufferedSeq: number | null = null
  const pending: BridgeLogEntry[] = []
  bridge.onMainLog((entry: BridgeLogEntry) => {
    if (lastBufferedSeq === null) pending.push(entry)
    else if ((entry.seq ?? Infinity) > lastBufferedSeq) addMainEntry(entry)
  })

  bridge.logs.getBuffered()
    .catch(() => [] as BridgeLogEntry[])
    .then((buffered) => {
      lastBufferedSeq = buffered.reduce((max, e) => Math.max(max, e.seq ?? 0), 0)
      const earlier = buffered.map(e => ({
        id: 0,
        timestamp: e.timestamp ?? new Date().toISOString(),
        level: e.level,
        message: e.message,
        source: e.source ?? 'main',
        ...(e.details ? { details: e.details } : {}),
      }))
      // Earlier entries go in time order ahead of what the renderer logged since
      const merged = [...earlier, ...logs].sort((a, b) => a.timestamp.localeCompare(b.timestamp))
      logs = merged.slice(-MAX_LOGS).map((e, i) => ({ ...e, id: i + 1 }))
      logIdCounter = logs.length
      for (const entry of pending) {
        if ((entry.seq ?? Infinity) > lastBufferedSeq) addMainEntry(entry)
      }
      pending.length = 0
      notifyListeners()
    })
}

export function getLogs(): LogEntry[] {
  return [...logs]
}

export function clearLogs() {
  logs = []
  logIdCounter = 0
  lastSeenId = 0
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

/** Errors logged since the Logs page was last open: the count on the sidebar. */
export function getUnseenErrorCount(): number {
  return logs.filter(l => l.level === 'error' && l.id > lastSeenId).length
}

export function markAllSeen() {
  if (lastSeenId === logIdCounter) return
  lastSeenId = logIdCounter
  notifyListeners()
}

export function markReported(ids: number[]) {
  const reported = new Set(ids)
  logs = logs.map(l => (reported.has(l.id) && !l.reported ? { ...l, reported: true } : l))
  notifyListeners()
}

// Earlier sessions are read from disk by the main process; outside Electron there are none

export function isLogFolderAvailable(): boolean {
  return Boolean(window.electronAPI?.logs)
}

export async function listLogSessions(): Promise<LogSession[]> {
  try {
    return (await window.electronAPI?.logs?.listSessions()) ?? []
  } catch {
    return []
  }
}

export async function readLogSession(id: string): Promise<LogEntry[]> {
  const entries = (await window.electronAPI?.logs?.readSession(id)) ?? []
  return entries.map((e, i) => ({
    id: i + 1,
    timestamp: e.timestamp ?? '',
    level: e.level,
    message: e.message,
    source: e.source ?? 'main',
    ...(e.details ? { details: e.details } : {}),
  }))
}

export async function openLogFolder(): Promise<void> {
  await window.electronAPI?.logs?.openFolder()
}

export async function getAppInfo(): Promise<AppInfo | null> {
  try {
    return (await window.electronAPI?.getAppInfo?.()) ?? null
  } catch {
    return null
  }
}
