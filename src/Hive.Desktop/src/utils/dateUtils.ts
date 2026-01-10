/**
 * Format a date string to a readable format
 */
export function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
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
