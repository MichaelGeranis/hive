import { describe, it, expect } from 'vitest'
import {
  formatDate,
  formatDateTime,
  formatTimestamp,
  formatMonthYear,
  formatWeekday,
  formatRelativeDate,
  isOverdue,
  calculateBusinessDays,
  parseTags,
  matchesSearch,
} from './dateUtils'

describe('formatMonthYear and formatWeekday', () => {
  it('name the month and the day in English, whatever the machine\'s language', () => {
    expect(formatMonthYear(new Date(2026, 8, 29))).toBe('September 2026')
    expect(formatMonthYear('2026-01-01')).toBe('January 2026')
    expect(formatWeekday(new Date(2026, 8, 29))).toBe('Tuesday')
    expect(formatWeekday('2026-09-27', 'short')).toBe('Sun')
  })
})

describe('dateUtils', () => {
  describe('formatDate', () => {
    it('should format as dd/MM/yyyy', () => {
      expect(formatDate('2024-01-15')).toBe('15/01/2024')
    })

    it('should pad single-digit days and months', () => {
      expect(formatDate('2024-12-25')).toBe('25/12/2024')
      expect(formatDate('2024-06-01')).toBe('01/06/2024')
    })

    it('should read a bare date as that calendar day in every time zone', () => {
      // Parsed as UTC midnight this would be 31/05/2024 west of Greenwich
      expect(formatDate('2024-06-01')).toBe('01/06/2024')
    })

    it('should handle ISO date-time strings', () => {
      expect(formatDate('2024-01-15T12:00:00')).toBe('15/01/2024')
    })

    it('should accept Date objects', () => {
      expect(formatDate(new Date(2024, 0, 15))).toBe('15/01/2024')
    })

    it('should return an empty string for a missing or unreadable date', () => {
      expect(formatDate(undefined)).toBe('')
      expect(formatDate(null)).toBe('')
      expect(formatDate('not a date')).toBe('')
    })
  })

  describe('formatDateTime', () => {
    it('should format as dd/MM/yyyy HH:mm on a 24-hour clock', () => {
      expect(formatDateTime(new Date(2024, 0, 15, 14, 5))).toBe('15/01/2024 14:05')
      expect(formatDateTime(new Date(2024, 0, 15, 9, 30))).toBe('15/01/2024 09:30')
    })
  })

  describe('formatTimestamp', () => {
    it('should format as dd/MM/yyyy HH:mm:ss.SSS', () => {
      expect(formatTimestamp(new Date(2024, 0, 15, 9, 5, 7, 42))).toBe('15/01/2024 09:05:07.042')
    })

    it('should return an empty string for a missing or unreadable value', () => {
      expect(formatTimestamp(undefined)).toBe('')
      expect(formatTimestamp('not a date')).toBe('')
    })
  })

  describe('formatRelativeDate', () => {
    const referenceDate = new Date('2024-01-15T12:00:00')

    it('should return "Due today" for same day', () => {
      const result = formatRelativeDate('2024-01-15', referenceDate)
      expect(result).toBe('Due today')
    })

    it('should return "Due tomorrow" for next day', () => {
      const result = formatRelativeDate('2024-01-16', referenceDate)
      expect(result).toBe('Due tomorrow')
    })

    it('should return "Due in X days" for dates within a week', () => {
      expect(formatRelativeDate('2024-01-17', referenceDate)).toBe('Due in 2 days')
      expect(formatRelativeDate('2024-01-20', referenceDate)).toBe('Due in 5 days')
      expect(formatRelativeDate('2024-01-22', referenceDate)).toBe('Due in 7 days')
    })

    it('should return formatted date for dates more than a week away', () => {
      const result = formatRelativeDate('2024-01-30', referenceDate)
      expect(result).toBe('30/01/2024')
    })

    it('should return "X days overdue" for past dates', () => {
      expect(formatRelativeDate('2024-01-14', referenceDate)).toBe('1 days overdue')
      expect(formatRelativeDate('2024-01-10', referenceDate)).toBe('5 days overdue')
    })
  })

  describe('isOverdue', () => {
    const referenceDate = new Date('2024-01-15T12:00:00')

    it('should return true for past dates', () => {
      expect(isOverdue('2024-01-14', referenceDate)).toBe(true)
      expect(isOverdue('2024-01-01', referenceDate)).toBe(true)
      expect(isOverdue('2023-12-31', referenceDate)).toBe(true)
    })

    it('should return false for today', () => {
      expect(isOverdue('2024-01-15', referenceDate)).toBe(false)
    })

    it('should return false for future dates', () => {
      expect(isOverdue('2024-01-16', referenceDate)).toBe(false)
      expect(isOverdue('2024-02-01', referenceDate)).toBe(false)
    })

    it('should return false for undefined/empty date', () => {
      expect(isOverdue(undefined, referenceDate)).toBe(false)
      expect(isOverdue('', referenceDate)).toBe(false)
    })
  })

  describe('calculateBusinessDays', () => {
    it('should count weekdays only', () => {
      // Mon Jan 15 to Fri Jan 19 = 5 business days
      expect(calculateBusinessDays('2024-01-15', '2024-01-19')).toBe(5)
    })

    it('should exclude weekends', () => {
      // Mon Jan 15 to Mon Jan 22 = 6 business days (excluding Sat 20, Sun 21)
      expect(calculateBusinessDays('2024-01-15', '2024-01-22')).toBe(6)
    })

    it('should return 1 for same day (if weekday)', () => {
      // Mon Jan 15 = 1 business day
      expect(calculateBusinessDays('2024-01-15', '2024-01-15')).toBe(1)
    })

    it('should return 0 for weekend day', () => {
      // Sat Jan 20 only = 0 business days
      expect(calculateBusinessDays('2024-01-20', '2024-01-20')).toBe(0)
    })

    it('should handle full week', () => {
      // Mon to Sun = 5 business days
      expect(calculateBusinessDays('2024-01-15', '2024-01-21')).toBe(5)
    })

    it('should handle two weeks', () => {
      // 2 weeks = 10 business days
      expect(calculateBusinessDays('2024-01-15', '2024-01-28')).toBe(10)
    })

    it('should handle month boundaries', () => {
      // Wed Jan 31 to Fri Feb 2 = 3 business days
      expect(calculateBusinessDays('2024-01-31', '2024-02-02')).toBe(3)
    })
  })

  describe('parseTags', () => {
    it('should parse comma-separated tags', () => {
      expect(parseTags('tag1, tag2, tag3')).toEqual(['tag1', 'tag2', 'tag3'])
    })

    it('should parse semicolon-separated tags', () => {
      expect(parseTags('tag1;tag2;tag3')).toEqual(['tag1', 'tag2', 'tag3'])
    })

    it('should parse space-separated tags', () => {
      expect(parseTags('tag1 tag2 tag3')).toEqual(['tag1', 'tag2', 'tag3'])
    })

    it('should handle mixed separators', () => {
      expect(parseTags('tag1, tag2; tag3 tag4')).toEqual(['tag1', 'tag2', 'tag3', 'tag4'])
    })

    it('should trim whitespace', () => {
      expect(parseTags('  tag1  ,  tag2  ')).toEqual(['tag1', 'tag2'])
    })

    it('should filter empty strings', () => {
      expect(parseTags('tag1,,tag2,,')).toEqual(['tag1', 'tag2'])
    })

    it('should return empty array for undefined', () => {
      expect(parseTags(undefined)).toEqual([])
    })

    it('should return empty array for empty string', () => {
      expect(parseTags('')).toEqual([])
    })
  })

  describe('matchesSearch', () => {
    const item = {
      title: 'Test Title',
      content: 'Some Content Here',
      tags: 'important, urgent',
      priority: 1,
    }

    it('should match title', () => {
      expect(matchesSearch(item, 'test', ['title', 'content'])).toBe(true)
      expect(matchesSearch(item, 'Title', ['title', 'content'])).toBe(true)
    })

    it('should match content', () => {
      expect(matchesSearch(item, 'content', ['title', 'content'])).toBe(true)
    })

    it('should be case insensitive', () => {
      expect(matchesSearch(item, 'TEST', ['title'])).toBe(true)
      expect(matchesSearch(item, 'CONTENT', ['content'])).toBe(true)
    })

    it('should return false for non-matching term', () => {
      expect(matchesSearch(item, 'xyz', ['title', 'content'])).toBe(false)
    })

    it('should return true for empty search term', () => {
      expect(matchesSearch(item, '', ['title', 'content'])).toBe(true)
      expect(matchesSearch(item, '   ', ['title', 'content'])).toBe(true)
    })

    it('should only search specified fields', () => {
      expect(matchesSearch(item, 'urgent', ['title'])).toBe(false)
      expect(matchesSearch(item, 'urgent', ['tags'])).toBe(true)
    })

    it('should handle partial matches', () => {
      expect(matchesSearch(item, 'Tit', ['title'])).toBe(true)
      expect(matchesSearch(item, 'ome Con', ['content'])).toBe(true)
    })
  })
})
