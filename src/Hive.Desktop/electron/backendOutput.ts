export type BackendLogLine = {
  level: 'info' | 'warn' | 'error' | 'debug'
  message: string
  details?: string
}

// ASP.NET Core's console logger writes "info: Category[EventId]" and then the message on
// indented lines below it, with any exception and stack trace after the message.
const HEADER = /^(trce|dbug|info|warn|fail|crit): (.*)$/

const LEVELS: Record<string, BackendLogLine['level']> = {
  trce: 'debug',
  dbug: 'debug',
  info: 'info',
  warn: 'warn',
  fail: 'error',
  crit: 'error',
}

/**
 * Splits a chunk of backend output into log entries. Output that is not in the console
 * logger's format becomes one entry per line at `fallbackLevel`.
 */
export function parseBackendOutput(text: string, fallbackLevel: BackendLogLine['level']): BackendLogLine[] {
  const entries: { level: BackendLogLine['level']; category: string; lines: string[] }[] = []

  for (const rawLine of text.split(/\r?\n/)) {
    if (!rawLine.trim()) continue
    const header = HEADER.exec(rawLine)
    if (header) {
      entries.push({ level: LEVELS[header[1]], category: header[2].trim(), lines: [] })
    } else if (entries.length > 0 && /^\s/.test(rawLine)) {
      entries[entries.length - 1].lines.push(rawLine.replace(/^ {6}/, ''))
    } else {
      entries.push({ level: fallbackLevel, category: '', lines: [rawLine] })
    }
  }

  return entries.map(({ level, category, lines }) => {
    const message = lines.length > 0 ? lines[0].trim() : category
    const rest = lines.slice(1)
    const details = [category && lines.length > 0 ? `Category: ${category}` : '', rest.join('\n')]
      .filter(Boolean)
      .join('\n')
    return details ? { level, message, details } : { level, message }
  })
}
