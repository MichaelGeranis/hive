import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { ThemeProvider, useTheme } from './ThemeContext'
import { ReactNode } from 'react'

// Wrapper component for hook testing
const wrapper = ({ children }: { children: ReactNode }) => (
  <ThemeProvider>{children}</ThemeProvider>
)

describe('ThemeContext', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear()
    // Reset document classes
    document.documentElement.classList.remove('light', 'dark')
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('timeToMinutes', () => {
    // We need to test the internal function through behavior
    // The function converts "HH:MM" to minutes since midnight

    it('should resolve correct theme based on schedule time', () => {
      // Set a specific time: 19:00 (7 PM)
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T19:00:00'))

      // Pre-set a schedule in localStorage
      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '18:00',
        darkEnd: '06:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At 19:00 with schedule 18:00-06:00, should be dark
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should resolve light theme when outside dark schedule', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T12:00:00')) // Noon

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '18:00',
        darkEnd: '06:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At 12:00 with schedule 18:00-06:00, should be light
      expect(result.current.resolvedTheme).toBe('light')
    })
  })

  describe('isWithinDarkPeriod', () => {
    it('should handle midnight-spanning schedule (evening start)', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T23:30:00')) // 11:30 PM

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '22:00',
        darkEnd: '06:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At 23:30 with schedule 22:00-06:00, should be dark
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should handle midnight-spanning schedule (early morning)', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T05:30:00')) // 5:30 AM

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '22:00',
        darkEnd: '06:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At 05:30 with schedule 22:00-06:00, should be dark
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should handle same-day schedule', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T21:00:00')) // 9 PM

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '20:00',
        darkEnd: '22:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At 21:00 with schedule 20:00-22:00, should be dark
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should be light outside same-day schedule', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T19:00:00')) // 7 PM

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '20:00',
        darkEnd: '22:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At 19:00 with schedule 20:00-22:00, should be light
      expect(result.current.resolvedTheme).toBe('light')
    })

    it('should handle edge case at exact start time', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T18:00:00')) // Exactly 6 PM

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '18:00',
        darkEnd: '06:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At exactly 18:00 with schedule 18:00-06:00, should be dark (>= start)
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should handle edge case at exact end time', () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T06:00:00')) // Exactly 6 AM

      localStorage.setItem('hive-theme', 'schedule')
      localStorage.setItem('hive-theme-schedule', JSON.stringify({
        darkStart: '18:00',
        darkEnd: '06:00'
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // At exactly 06:00 with schedule 18:00-06:00, should be light (< end, not <=)
      expect(result.current.resolvedTheme).toBe('light')
    })
  })

  describe('useTheme hook', () => {
    it('should throw error when used outside ThemeProvider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        renderHook(() => useTheme())
      }).toThrow('useTheme must be used within a ThemeProvider')

      consoleSpy.mockRestore()
    })

    it('should provide default theme as system', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      expect(result.current.theme).toBe('system')
    })

    it('should allow setting theme to light', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      act(() => {
        result.current.setTheme('light')
      })

      expect(result.current.theme).toBe('light')
      expect(result.current.resolvedTheme).toBe('light')
    })

    it('should allow setting theme to dark', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      act(() => {
        result.current.setTheme('dark')
      })

      expect(result.current.theme).toBe('dark')
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should persist theme to localStorage', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      act(() => {
        result.current.setTheme('dark')
      })

      expect(localStorage.setItem).toHaveBeenCalledWith('hive-theme', 'dark')
    })

    it('should read theme from localStorage on mount', () => {
      localStorage.setItem('hive-theme', 'dark')

      const { result } = renderHook(() => useTheme(), { wrapper })

      expect(result.current.theme).toBe('dark')
    })

    it('should update document class when theme changes', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      act(() => {
        result.current.setTheme('dark')
      })

      expect(document.documentElement.classList.contains('dark')).toBe(true)
      expect(document.documentElement.classList.contains('light')).toBe(false)
    })

    it('should provide default schedule', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      expect(result.current.schedule).toEqual({
        darkStart: '18:00',
        darkEnd: '06:00'
      })
    })

    it('should allow updating schedule', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      act(() => {
        result.current.setSchedule({
          darkStart: '20:00',
          darkEnd: '07:00'
        })
      })

      expect(result.current.schedule).toEqual({
        darkStart: '20:00',
        darkEnd: '07:00'
      })
    })

    it('should persist schedule to localStorage', () => {
      const { result } = renderHook(() => useTheme(), { wrapper })

      const newSchedule = { darkStart: '20:00', darkEnd: '07:00' }

      act(() => {
        result.current.setSchedule(newSchedule)
      })

      expect(localStorage.setItem).toHaveBeenCalledWith(
        'hive-theme-schedule',
        JSON.stringify(newSchedule)
      )
    })
  })

  describe('system theme detection', () => {
    it('should detect system dark preference', () => {
      // Mock matchMedia to return dark preference
      vi.spyOn(window, 'matchMedia').mockImplementation((query) => ({
        matches: query === '(prefers-color-scheme: dark)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      // Default is 'system', so resolved should follow system preference
      expect(result.current.theme).toBe('system')
      expect(result.current.resolvedTheme).toBe('dark')
    })

    it('should detect system light preference', () => {
      // Mock matchMedia to return light preference
      vi.spyOn(window, 'matchMedia').mockImplementation((query) => ({
        matches: false, // Not dark
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))

      const { result } = renderHook(() => useTheme(), { wrapper })

      expect(result.current.theme).toBe('system')
      expect(result.current.resolvedTheme).toBe('light')
    })
  })

  describe('localStorage recovery', () => {
    it('should handle invalid theme in localStorage', () => {
      localStorage.setItem('hive-theme', 'invalid-theme')

      const { result } = renderHook(() => useTheme(), { wrapper })

      // Should fall back to system
      expect(result.current.theme).toBe('system')
    })

    it('should handle invalid schedule JSON in localStorage', () => {
      localStorage.setItem('hive-theme-schedule', 'not-valid-json')

      const { result } = renderHook(() => useTheme(), { wrapper })

      // Should fall back to default schedule
      expect(result.current.schedule).toEqual({
        darkStart: '18:00',
        darkEnd: '06:00'
      })
    })
  })
})
