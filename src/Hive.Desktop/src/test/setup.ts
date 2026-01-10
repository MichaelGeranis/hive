import '@testing-library/jest-dom'
import { afterAll, afterEach, beforeAll, beforeEach, vi } from 'vitest'
import { server } from './mocks/server'

// Store original console methods
const originalConsole = {
  log: console.log,
  warn: console.warn,
  error: console.error,
  debug: console.debug,
}

// Store original stderr.write to filter jsdom error output
const originalStderrWrite = process.stderr.write.bind(process.stderr)
process.stderr.write = ((chunk: string | Uint8Array, ...args: unknown[]) => {
  const text = typeof chunk === 'string' ? chunk : chunk.toString()
  // Filter expected test errors from stderr
  if (
    text.includes('useTheme must be used within a ThemeProvider') ||
    text.includes('Warning: `--localstorage-file`')
  ) {
    return true
  }
  return originalStderrWrite(chunk, ...args)
}) as typeof process.stderr.write

// Start MSW server before all tests
beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })

  // Suppress console output during tests (less noisy output)
  console.log = vi.fn()
  console.warn = vi.fn()
  console.debug = vi.fn()
  // Keep console.error for debugging failed tests, but filter React/testing-library noise
  console.error = vi.fn((...args) => {
    // Convert all args to string for matching
    const fullMessage = args.map(arg => {
      if (arg instanceof Error) return arg.message + '\n' + arg.stack
      return String(arg)
    }).join(' ')

    // Filter out expected React errors and testing-library noise
    if (
      fullMessage.includes('useTheme must be used within a ThemeProvider') ||
      fullMessage.includes('The above error occurred in') ||
      fullMessage.includes('Consider adding an error boundary') ||
      fullMessage.includes('Error: Uncaught') ||
      fullMessage.includes('act(...)') ||
      // Filter logStore test output (tests that explicitly call console.error)
      fullMessage === 'Error message' ||
      fullMessage === 'Error 1'
    ) {
      return
    }
    originalConsole.error(...args)
  })
})

// Reset handlers after each test
afterEach(() => server.resetHandlers())

// Close server after all tests and restore console
afterAll(() => {
  server.close()
  // Restore original console methods
  console.log = originalConsole.log
  console.warn = originalConsole.warn
  console.error = originalConsole.error
  console.debug = originalConsole.debug
})

// Mock window.matchMedia for theme tests
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key]
    }),
    clear: vi.fn(() => {
      store = {}
    }),
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
})

// Mock Electron API
Object.defineProperty(window, 'electronAPI', {
  writable: true,
  value: {
    onMainLog: vi.fn(),
    onBackendPort: vi.fn(),
  },
})

// Suppress unhandled error output for expected test errors
const originalOnError = window.onerror
window.onerror = (message) => {
  if (typeof message === 'string' && message.includes('useTheme must be used within a ThemeProvider')) {
    return true // Suppress the error
  }
  return originalOnError?.call(window, message as string) ?? false
}

// Clean up localStorage before each test
beforeEach(() => {
  localStorageMock.clear()
  vi.clearAllMocks()
})
