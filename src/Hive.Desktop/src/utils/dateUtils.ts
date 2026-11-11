const DATE_ONLY = /^(\d{4})-(\d{2})-(\d{2})$/

const pad = (n: number) => String(n).padStart(2, '0')

/**
 * Reads a date from the API. A bare `yyyy-MM-dd` is a calendar day and is read in local
 * time: `new Date('2025-10-06')` would read it as UTC midnight, which is the previous day
 * anywhere west of Greenwich.
 */
export function toDate(value: string | Date): Date {
  if (value instanceof Date) return value
  const dateOnly = DATE_ONLY.exec(value)
  if (dateOnly) {
    return new Date(Number(dateOnly[1]), Number(dateOnly[2]) - 1, Number(dateOnly[3]))
  }
  return new Date(value)
}

/**
 * The one display format for dates in Hive: dd/MM/yyyy, e.g. 06/10/2025.
 * Returns an empty string for a missing or unreadable date.
 */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return ''
  const date = toDate(value)
  if (Number.isNaN(date.getTime())) return ''
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`
}

/**
 * A date with its time of day: dd/MM/yyyy HH:mm, on a 24-hour clock.
 */
export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) return ''
  const date = toDate(value)
  if (Number.isNaN(date.getTime())) return ''
  return `${formatDate(date)} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']
const WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

/**
 * A month for a heading, e.g. "September 2026". The same whatever the machine's language.
 */
export function formatMonthYear(value: string | Date): string {
  const date = toDate(value)
  return `${MONTHS[date.getMonth()]} ${date.getFullYear()}`
}

/**
 * The day of the week, "Monday" or, short, "Mon". The same whatever the machine's language.
 */
export function formatWeekday(value: string | Date, style: 'long' | 'short' = 'long'): string {
  const name = WEEKDAYS[toDate(value).getDay()]
  return style === 'short' ? name.slice(0, 3) : name
}

/**
 * A moment to the millisecond, for log entries: dd/MM/yyyy HH:mm:ss.SSS in local time.
 */
export function formatTimestamp(value: string | Date | null | undefined): string {
  if (!value) return ''
  const date = toDate(value)
  if (Number.isNaN(date.getTime())) return ''
  const ms = String(date.getMilliseconds()).padStart(3, '0')
  return `${formatDateTime(date)}:${pad(date.getSeconds())}.${ms}`
}

/**
 * Format a date string to a relative date (e.g., "Due today", "2 days overdue")
 */
export function formatRelativeDate(dateString: string, referenceDate?: Date): string {
  const date = new Date(dateString)
  const now = referenceDate || new Date()
  const diffDays = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  if (diffDays < 0) return `${Math.abs(diffDays)} days overdue`
  if (diffDays === 0) return 'Due today'
  if (diffDays === 1) return 'Due tomorrow'
  if (diffDays <= 7) return `Due in ${diffDays} days`
  return formatDate(dateString)
}

/**
 * Check if a date is overdue (before today)
 */
export function isOverdue(dateString: string | undefined, referenceDate?: Date): boolean {
  if (!dateString) return false
  const date = new Date(dateString)
  const today = referenceDate || new Date()
  today.setHours(0, 0, 0, 0)
  return date < today
}

/**
 * Calculate business days between two dates (excluding weekends)
 */
export function calculateBusinessDays(startDate: string, endDate: string): number {
  const start = new Date(startDate)
  const end = new Date(endDate)

  let count = 0
  const current = new Date(start)

  while (current <= end) {
    const dayOfWeek = current.getDay()
    // 0 = Sunday, 6 = Saturday
    if (dayOfWeek !== 0 && dayOfWeek !== 6) {
      count++
    }
    current.setDate(current.getDate() + 1)
  }

  return count
}

/**
 * Parse tags from a comma/semicolon/space separated string
 */
export function parseTags(tagString: string | undefined): string[] {
  if (!tagString) return []
  return tagString
    .split(/[,;\s]+/)
    .map(tag => tag.trim())
    .filter(tag => tag.length > 0)
}

/**
 * Filter items by search term across multiple fields
 */
export function matchesSearch<T extends Record<string, unknown>>(
  item: T,
  searchTerm: string,
  fields: (keyof T)[]
): boolean {
  if (!searchTerm.trim()) return true

  const query = searchTerm.toLowerCase()
  return fields.some(field => {
    const value = item[field]
    if (typeof value === 'string') {
      return value.toLowerCase().includes(query)
    }
    return false
  })
}
