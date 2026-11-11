import { describe, it, expect } from 'vitest'
import type { LogEntry } from '../services/logStore'
import { buildReport, filterLogs, formatRawLogs, groupLogs, summaryLine, REPORT_CONTEXT_ENTRIES } from './logReport'

let nextId = 1
function entry(overrides: Partial<LogEntry> = {}): LogEntry {
  const id = nextId++
  return {
    id,
    timestamp: new Date(Date.UTC(2026, 8, 28, 9, 0, 0, id)).toISOString(),
    level: 'info',
    source: 'renderer',
    message: `Entry ${id}`,
    ...overrides,
  }
}

const allLevels = new Set(['info', 'warn', 'error', 'debug'] as const)
const allSources = new Set(['renderer', 'main', 'backend', 'api'] as const)

describe('filterLogs', () => {
  it('keeps only the chosen levels and sources', () => {
    const entries = [
      entry({ level: 'error', source: 'api' }),
      entry({ level: 'info', source: 'api' }),
      entry({ level: 'error', source: 'main' }),
    ]

    const result = filterLogs(entries, { levels: new Set(['error']), sources: new Set(['api']), search: '' })

    expect(result).toEqual([entries[0]])
  })

  it('searches the message and the details, ignoring case', () => {
    const entries = [
      entry({ message: 'Health check failed' }),
      entry({ message: 'Other', details: 'at HealthMonitor.run' }),
      entry({ message: 'Unrelated' }),
    ]

    const result = filterLogs(entries, { levels: allLevels, sources: allSources, search: 'HEALTH' })

    expect(result).toEqual([entries[0], entries[1]])
  })
})

describe('groupLogs', () => {
  it('collapses identical entries into one group with its first and last occurrence', () => {
    const a1 = entry({ level: 'error', message: 'Health check failed' })
    const b = entry({ level: 'error', message: 'Something else' })
    const a2 = entry({ level: 'error', message: 'Health check failed' })

    const groups = groupLogs([a1, b, a2])

    expect(groups).toHaveLength(2)
    expect(groups[0].message).toBe('Health check failed')
    expect(groups[0].entries).toHaveLength(2)
    expect(groups[0].first).toBe(a1)
    expect(groups[0].last).toBe(a2)
  })

  it('keeps entries apart when their details differ', () => {
    const groups = groupLogs([
      entry({ message: 'Failed', details: 'stack one' }),
      entry({ message: 'Failed', details: 'stack two' }),
    ])

    expect(groups).toHaveLength(2)
  })

  it('orders the most recent group first', () => {
    const older = entry({ message: 'Older' })
    const newer = entry({ message: 'Newer' })

    expect(groupLogs([older, newer]).map(g => g.message)).toEqual(['Newer', 'Older'])
  })

  it('is reported only when every entry in it was reported', () => {
    const groups = groupLogs([
      entry({ message: 'Same', reported: true }),
      entry({ message: 'Same' }),
    ])

    expect(groups[0].reported).toBe(false)
  })
})

describe('summaryLine', () => {
  it('returns the first non-empty line', () => {
    expect(summaryLine('\n  First line  \nsecond')).toBe('First line')
  })
})

describe('buildReport', () => {
  const app = {
    version: '1.2.3',
    isPackaged: true,
    platform: 'darwin',
    arch: 'arm64',
    osRelease: '25.6.0',
    electron: '44.3.0',
    chrome: '140',
    node: '22',
  }

  it('describes the app, the problem, its details and the entries that led to it', () => {
    const before = entry({ message: 'Loading dashboard' })
    const failure = entry({ level: 'error', source: 'api', message: 'API GET /reports - Failed (500)', details: 'Response: boom' })
    const groups = groupLogs([failure])

    const report = buildReport(groups, [before, failure], app, 'This session', new Date(2026, 8, 28, 10, 0))

    expect(report).toContain('Hive error report')
    expect(report).toContain('App version: 1.2.3 (installed app, SQLite database)')
    expect(report).toContain('OS: darwin arm64, release 25.6.0')
    expect(report).toContain('Log session: This session')
    expect(report).toContain('Generated: 28/09/2026 10:00:00.000')
    expect(report).toContain('1. [ERROR] [api] API GET /reports - Failed (500)')
    expect(report).toContain('Details:\nResponse: boom')
    expect(report).toContain('[INFO] [renderer] Loading dashboard')
  })

  it('lists how often a repeated problem happened', () => {
    const groups = groupLogs([
      entry({ level: 'error', message: 'Health check failed' }),
      entry({ level: 'error', message: 'Health check failed' }),
      entry({ level: 'error', message: 'Health check failed' }),
    ])

    const report = buildReport(groups, groups[0].entries, app, 'This session')

    expect(report).toMatch(/Occurrences: 3, first .+, last .+/)
  })

  it('limits the context to the entries just before the first occurrence', () => {
    const earlier = Array.from({ length: REPORT_CONTEXT_ENTRIES + 5 }, (_, i) => entry({ message: `Step ${i}` }))
    const failure = entry({ level: 'error', message: 'Broke' })

    const report = buildReport(groupLogs([failure]), [...earlier, failure], app, 'This session')

    expect(report).not.toContain('Step 4\n')
    expect(report).toContain(`Step ${REPORT_CONTEXT_ENTRIES + 4}`)
    expect(report).toContain(`last ${REPORT_CONTEXT_ENTRIES} entries`)
  })

  it('falls back to the browser description outside the desktop app', () => {
    const report = buildReport([], [], null, 'This session')

    expect(report).toContain('Environment:')
  })
})

describe('formatRawLogs', () => {
  it('writes one line per entry with ISO timestamps, and the details below', () => {
    const e = entry({ level: 'error', message: 'Broke', details: 'stack' })

    expect(formatRawLogs([e])).toBe(`[${e.timestamp}] [ERROR] [renderer] Broke\nstack`)
  })
})
