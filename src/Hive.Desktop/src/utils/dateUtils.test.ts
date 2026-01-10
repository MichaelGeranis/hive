import { describe, it, expect } from 'vitest'
import {
  formatDate,
  formatRelativeDate,
  isOverdue,
  calculateBusinessDays,
  parseTags,
  matchesSearch,
} from './dateUtils'

describe('dateUtils', () => {
  describe('formatDate', () => {
    it('should format date correctly', () => {
      const result = formatDate('2024-01-15')
      expect(result).toBe('Jan 15, 2024')
    })

    it('should handle different months', () => {
      expect(formatDate('2024-12-25')).toBe('Dec 25, 2024')
      expect(formatDate('2024-06-01')).toBe('Jun 1, 2024')
    })

    it('should handle ISO date strings', () => {
      const result = formatDate('2024-01-15T10:30:00Z')
      expect(result).toContain('Jan')
      expect(result).toContain('2024')
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
      expect(result).toBe('Jan 30, 2024')
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
