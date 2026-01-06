import { createContext, useContext, useState, useEffect, ReactNode } from 'react'

type Theme = 'light' | 'dark' | 'system' | 'schedule'

interface ThemeSchedule {
  darkStart: string // HH:MM format (e.g., "18:00")
  darkEnd: string   // HH:MM format (e.g., "06:00")
}

interface ThemeContextType {
  theme: Theme
  resolvedTheme: 'light' | 'dark'
  setTheme: (theme: Theme) => void
  schedule: ThemeSchedule
  setSchedule: (schedule: ThemeSchedule) => void
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined)

const STORAGE_KEY = 'hive-theme'
const SCHEDULE_KEY = 'hive-theme-schedule'
const DEFAULT_SCHEDULE: ThemeSchedule = { darkStart: '18:00', darkEnd: '06:00' }

function getSystemTheme(): 'light' | 'dark' {
  if (typeof window !== 'undefined' && window.matchMedia) {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  }
  return 'light'
}

function timeToMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number)
  return hours * 60 + minutes
}

function isWithinDarkPeriod(schedule: ThemeSchedule): boolean {
  const now = new Date()
  const currentMinutes = now.getHours() * 60 + now.getMinutes()
  const darkStart = timeToMinutes(schedule.darkStart)
  const darkEnd = timeToMinutes(schedule.darkEnd)

  // Handle schedules that span midnight (e.g., 18:00 to 06:00)
  if (darkStart > darkEnd) {
    // Dark period spans midnight: dark from darkStart to 23:59 OR from 00:00 to darkEnd
    return currentMinutes >= darkStart || currentMinutes < darkEnd
  } else {
    // Dark period within same day (e.g., 20:00 to 22:00)
    return currentMinutes >= darkStart && currentMinutes < darkEnd
  }
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => {
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem(STORAGE_KEY) as Theme | null
      if (stored && ['light', 'dark', 'system', 'schedule'].includes(stored)) {
        return stored
      }
    }
    return 'system'
  })

  const [schedule, setScheduleState] = useState<ThemeSchedule>(() => {
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem(SCHEDULE_KEY)
      if (stored) {
        try {
          return JSON.parse(stored)
        } catch {
          return DEFAULT_SCHEDULE
        }
      }
    }
    return DEFAULT_SCHEDULE
  })

  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>(() => {
    if (theme === 'system') {
      return getSystemTheme()
    }
    if (theme === 'schedule') {
      return isWithinDarkPeriod(schedule) ? 'dark' : 'light'
    }
    return theme as 'light' | 'dark'
  })

  // Function to resolve the theme based on current settings
  const resolveTheme = (): 'light' | 'dark' => {
    if (theme === 'system') {
      return getSystemTheme()
    }
    if (theme === 'schedule') {
      return isWithinDarkPeriod(schedule) ? 'dark' : 'light'
    }
    return theme as 'light' | 'dark'
  }

  // Update resolved theme when theme or schedule changes
  useEffect(() => {
    const resolved = resolveTheme()
    setResolvedTheme(resolved)

    // Update document class
    const root = document.documentElement
    root.classList.remove('light', 'dark')
    root.classList.add(resolved)

    // Update meta theme-color for mobile browsers
    const metaTheme = document.querySelector('meta[name="theme-color"]')
    if (metaTheme) {
      metaTheme.setAttribute('content', resolved === 'dark' ? '#0f172a' : '#f8fafc')
    }
  }, [theme, schedule])

  // Listen for system theme changes
  useEffect(() => {
    if (theme !== 'system') return

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleChange = (e: MediaQueryListEvent) => {
      setResolvedTheme(e.matches ? 'dark' : 'light')
      document.documentElement.classList.remove('light', 'dark')
      document.documentElement.classList.add(e.matches ? 'dark' : 'light')
    }

    mediaQuery.addEventListener('change', handleChange)
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [theme])

  // Check schedule periodically when in schedule mode
  useEffect(() => {
    if (theme !== 'schedule') return

    const checkSchedule = () => {
      const newResolved = isWithinDarkPeriod(schedule) ? 'dark' : 'light'
      if (newResolved !== resolvedTheme) {
        setResolvedTheme(newResolved)
        document.documentElement.classList.remove('light', 'dark')
        document.documentElement.classList.add(newResolved)
      }
    }

    // Check every minute
    const interval = setInterval(checkSchedule, 60000)
    return () => clearInterval(interval)
  }, [theme, schedule, resolvedTheme])

  const setTheme = (newTheme: Theme) => {
    setThemeState(newTheme)
    localStorage.setItem(STORAGE_KEY, newTheme)
  }

  const setSchedule = (newSchedule: ThemeSchedule) => {
    setScheduleState(newSchedule)
    localStorage.setItem(SCHEDULE_KEY, JSON.stringify(newSchedule))
  }

  return (
    <ThemeContext.Provider value={{ theme, resolvedTheme, setTheme, schedule, setSchedule }}>
      {children}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const context = useContext(ThemeContext)
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider')
  }
  return context
}
