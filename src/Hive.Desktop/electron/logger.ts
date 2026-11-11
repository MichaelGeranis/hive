import { app, BrowserWindow } from 'electron'
import * as fs from 'fs'
import * as path from 'path'

export type LogLevel = 'info' | 'warn' | 'error' | 'debug'
export type LogSource = 'main' | 'renderer' | 'backend' | 'api'

export type PersistedLogEntry = {
  seq: number
  timestamp: string
  level: LogLevel
  source: LogSource
  message: string
  details?: string
}

export type LogSessionInfo = {
  id: string
  startedAt: string
  size: number
  isCurrent: boolean
}

// Every run of the app writes its own session file, one JSON entry per line, so the logs
// of a run that crashed are still there on the next start.
const MAX_SESSIONS = 10
const MAX_SESSION_BYTES = 5 * 1024 * 1024
// Main-process and backend entries are also kept in memory, so a renderer that loads
// after they were written can still show them.
const BUFFER_SIZE = 1000
const SESSION_FILE = /^hive-(\d{4}-\d{2}-\d{2})T(\d{2})-(\d{2})-(\d{2})-(\d{3})Z\.jsonl$/

const LEVELS: LogLevel[] = ['info', 'warn', 'error', 'debug']
const SOURCES: LogSource[] = ['main', 'renderer', 'backend', 'api']

let stream: fs.WriteStream | null = null
let sessionId: string | null = null
let bytesWritten = 0
let sessionFull = false
let seq = 0
let buffer: PersistedLogEntry[] = []
let logWindow: BrowserWindow | null = null

export function logsDirectory(): string {
  return path.join(app.getPath('userData'), 'logs')
}

export function startLogSession(): void {
  const directory = logsDirectory()
  try {
    fs.mkdirSync(directory, { recursive: true })
    sessionId = `hive-${new Date().toISOString().replace(/[:.]/g, '-')}.jsonl`
    stream = fs.createWriteStream(path.join(directory, sessionId), { flags: 'a' })
    stream.on('error', () => {
      stream = null
    })
    pruneOldSessions(directory)
  } catch {
    stream = null
  }
}

export function setLogWindow(window: BrowserWindow | null): void {
  logWindow = window
}

/** Records an entry from the main process or the backend: to disk, memory and the renderer. */
export function recordLog(level: LogLevel, message: string, source: LogSource = 'main', details?: string): void {
  const entry: PersistedLogEntry = {
    seq: ++seq,
    timestamp: new Date().toISOString(),
    level,
    source,
    message,
    ...(details ? { details } : {}),
  }

  console.log(`[${entry.timestamp}] [${level.toUpperCase()}] [${source}] ${message}`)
  writeEntry(entry)
  buffer = [...buffer.slice(-(BUFFER_SIZE - 1)), entry]

  if (logWindow && !logWindow.isDestroyed()) {
    logWindow.webContents.send('main-log', entry)
  }
}

/** Persists an entry the renderer captured. It already holds it in memory. */
export function writeRendererEntry(value: unknown): void {
  const entry = toEntry(value)
  if (entry) writeEntry(entry)
}

export function getBufferedEntries(): PersistedLogEntry[] {
  return [...buffer]
}

export function listSessions(): LogSessionInfo[] {
  const directory = logsDirectory()
  let names: string[]
  try {
    names = fs.readdirSync(directory).filter(name => SESSION_FILE.test(name))
  } catch {
    return []
  }

  return names
    .sort()
    .reverse()
    .map(name => {
      const match = SESSION_FILE.exec(name)!
      let size = 0
      try {
        size = fs.statSync(path.join(directory, name)).size
      } catch {
        // A file removed between listing and stat is reported as empty
      }
      return {
        id: name,
        startedAt: `${match[1]}T${match[2]}:${match[3]}:${match[4]}.${match[5]}Z`,
        size,
        isCurrent: name === sessionId,
      }
    })
}

export function readSession(id: unknown): PersistedLogEntry[] {
  // Only a session file in the logs folder can be read, never an arbitrary path
  if (typeof id !== 'string' || !SESSION_FILE.test(id) || path.basename(id) !== id) return []

  let content: string
  try {
    content = fs.readFileSync(path.join(logsDirectory(), id), 'utf8')
  } catch {
    return []
  }

  const entries: PersistedLogEntry[] = []
  for (const line of content.split('\n')) {
    if (!line.trim()) continue
    try {
      const entry = toEntry(JSON.parse(line))
      if (entry) entries.push(entry)
    } catch {
      // A line cut short by a crash is skipped, not fatal
    }
  }
  return entries
}

function writeEntry(entry: PersistedLogEntry): void {
  if (!stream || sessionFull) return

  const line = JSON.stringify(entry) + '\n'
  if (bytesWritten + line.length > MAX_SESSION_BYTES) {
    sessionFull = true
    stream.write(JSON.stringify({
      ...entry,
      level: 'warn',
      source: 'main',
      message: 'Log file size limit reached; later entries for this session are not saved to disk',
      details: undefined,
    }) + '\n')
    return
  }

  bytesWritten += line.length
  stream.write(line)
}

function pruneOldSessions(directory: string): void {
  const names = fs.readdirSync(directory).filter(name => SESSION_FILE.test(name)).sort()
  for (const name of names.slice(0, Math.max(0, names.length - MAX_SESSIONS))) {
    try {
      fs.unlinkSync(path.join(directory, name))
    } catch {
      // Left for the next start
    }
  }
}

function toEntry(value: unknown): PersistedLogEntry | null {
  if (!value || typeof value !== 'object') return null
  const v = value as Record<string, unknown>
  if (typeof v.message !== 'string' || typeof v.timestamp !== 'string') return null
  if (!LEVELS.includes(v.level as LogLevel) || !SOURCES.includes(v.source as LogSource)) return null

  return {
    seq: typeof v.seq === 'number' ? v.seq : 0,
    timestamp: v.timestamp,
    level: v.level as LogLevel,
    source: v.source as LogSource,
    message: v.message,
    ...(typeof v.details === 'string' && v.details ? { details: v.details } : {}),
  }
}
