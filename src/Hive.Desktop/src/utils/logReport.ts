import type { AppInfo, LogEntry, LogLevel, LogSource } from '../services/logStore'
import { formatTimestamp } from './dateUtils'

/** Identical entries (same level, source, message and details), shown as one row. */
export type LogGroup = {
  key: string
  level: LogLevel
  source: LogSource
  message: string
  details?: string
  entries: LogEntry[]
  first: LogEntry
  last: LogEntry
  reported: boolean
}

export type LogFilter = {
  levels: ReadonlySet<LogLevel>
  sources: ReadonlySet<LogSource>
  search: string
}

/** How many entries before an error go into a report, so a developer sees what led to it. */
export const REPORT_CONTEXT_ENTRIES = 20

export const LEVEL_LABELS: Record<LogLevel, string> = {
  error: 'Errors',
  warn: 'Warnings',
  info: 'Info',
  debug: 'Debug',
}

export const SOURCE_LABELS: Record<LogSource, string> = {
  renderer: 'Renderer',
  main: 'Main',
  backend: 'Backend',
  api: 'API',
}

export function filterLogs(entries: LogEntry[], filter: LogFilter): LogEntry[] {
  const search = filter.search.trim().toLowerCase()
  return entries.filter(e =>
    filter.levels.has(e.level) &&
    filter.sources.has(e.source) &&
    (!search || e.message.toLowerCase().includes(search) || (e.details?.toLowerCase().includes(search) ?? false))
  )
}

/** Groups identical entries, the group that happened most recently first. */
export function groupLogs(entries: LogEntry[]): LogGroup[] {
  const groups = new Map<string, LogGroup>()
  for (const entry of entries) {
    const key = [entry.level, entry.source, entry.message, entry.details ?? ''].join('\u0000')
    const group = groups.get(key)
    if (group) {
      group.entries.push(entry)
      if (entry.timestamp < group.first.timestamp) group.first = entry
      if (entry.timestamp >= group.last.timestamp) group.last = entry
      group.reported = group.reported && Boolean(entry.reported)
    } else {
      groups.set(key, {
        key,
        level: entry.level,
        source: entry.source,
        message: entry.message,
        details: entry.details,
        entries: [entry],
        first: entry,
        last: entry,
        reported: Boolean(entry.reported),
      })
    }
  }
  return [...groups.values()].sort((a, b) =>
    b.last.timestamp.localeCompare(a.last.timestamp) || b.last.id - a.last.id
  )
}

/** The first line of a message, for the collapsed row. */
export function summaryLine(message: string): string {
  const first = message.split('\n').find(line => line.trim()) ?? ''
  return first.trim()
}

function entryLine(entry: LogEntry): string {
  return `${formatTimestamp(entry.timestamp)} [${entry.level.toUpperCase()}] [${entry.source}] ${summaryLine(entry.message)}`
}

function describeApp(app: AppInfo | null): string[] {
  if (!app) {
    return [`Environment: ${typeof navigator !== 'undefined' ? navigator.userAgent : 'unknown'}`]
  }
  return [
    `App version: ${app.version} (${app.isPackaged ? 'installed app, SQLite database' : 'development build'})`,
    `OS: ${app.platform} ${app.arch}, release ${app.osRelease}`,
    `Runtime: Electron ${app.electron}, Chrome ${app.chrome}, Node ${app.node}`,
  ]
}

function describeGroup(group: LogGroup, index: number, allEntries: LogEntry[]): string {
  const lines = [
    `${index}. [${group.level.toUpperCase()}] [${group.source}] ${summaryLine(group.message)}`,
    group.entries.length > 1
      ? `Occurrences: ${group.entries.length}, first ${formatTimestamp(group.first.timestamp)}, last ${formatTimestamp(group.last.timestamp)}`
      : `When: ${formatTimestamp(group.first.timestamp)}`,
    '',
    'Message:',
    group.message,
  ]

  if (group.details) lines.push('', 'Details:', group.details)

  const before = allEntries
    .filter(e => e.timestamp < group.first.timestamp || (e.timestamp === group.first.timestamp && e.id < group.first.id))
    .slice(-REPORT_CONTEXT_ENTRIES)
  if (before.length > 0) {
    lines.push('', `What happened before (last ${before.length} entries):`, ...before.map(entryLine))
  }

  return lines.join('\n')
}

/**
 * The text a manager sends to the developers: where it happened, then each problem with
 * its details and the log entries that led up to it.
 */
export function buildReport(
  groups: LogGroup[],
  allEntries: LogEntry[],
  app: AppInfo | null,
  sessionLabel: string,
  generatedAt: Date = new Date()
): string {
  const header = [
    'Hive error report',
    `Generated: ${formatTimestamp(generatedAt)} (${generatedAt.toISOString()})`,
    ...describeApp(app),
    `Log session: ${sessionLabel}`,
    `Problems: ${groups.length}`,
  ]

  const sorted = [...allEntries].sort((a, b) => a.timestamp.localeCompare(b.timestamp) || a.id - b.id)
  const sections = groups.map((group, i) => describeGroup(group, i + 1, sorted))

  return [header.join('\n'), ...sections].join('\n\n' + '='.repeat(72) + '\n\n') + '\n'
}

/** Every entry, oldest first, one per line with ISO timestamps: the raw log file. */
export function formatRawLogs(entries: LogEntry[]): string {
  return entries
    .map(log => {
      const line = `[${log.timestamp}] [${log.level.toUpperCase()}] [${log.source}] ${log.message}`
      return log.details ? `${line}\n${log.details}` : line
    })
    .join('\n')
}
