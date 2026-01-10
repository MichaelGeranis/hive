import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

// We need to reset the module state between tests
// Import functions will be done inside beforeEach
let logStore: typeof import('./logStore')

describe('logStore', () => {
  // Store original console methods
  let originalConsoleLog: typeof console.log
  let originalConsoleWarn: typeof console.warn
  let originalConsoleError: typeof console.error
  let originalConsoleDebug: typeof console.debug

  beforeEach(async () => {
    // Store original console methods
    originalConsoleLog = console.log
    originalConsoleWarn = console.warn
    originalConsoleError = console.error
    originalConsoleDebug = console.debug

    // Reset the module to get fresh state
    vi.resetModules()
    logStore = await import('./logStore')
  })

  afterEach(() => {
    // Restore original console methods
    console.log = originalConsoleLog
    console.warn = originalConsoleWarn
    console.error = originalConsoleError
    console.debug = originalConsoleDebug
  })

  describe('getLogs', () => {
    it('should return empty array initially', () => {
      const logs = logStore.getLogs()
      expect(logs).toEqual([])
    })

    it('should return a copy of logs (not the original array)', () => {
      const logs1 = logStore.getLogs()
      const logs2 = logStore.getLogs()

      expect(logs1).not.toBe(logs2)
      expect(logs1).toEqual(logs2)
    })
  })

  describe('initializeLogCapture', () => {
    it('should add initial log entry on initialization', () => {
      logStore.initializeLogCapture()

      const logs = logStore.getLogs()
      expect(logs.length).toBe(1)
      expect(logs[0].message).toBe('Log capture initialized')
      expect(logs[0].level).toBe('info')
      expect(logs[0].source).toBe('renderer')
    })

    it('should only initialize once', () => {
      logStore.initializeLogCapture()
      logStore.initializeLogCapture()
      logStore.initializeLogCapture()

      const logs = logStore.getLogs()
      // Should only have one initialization message
      const initLogs = logs.filter(l => l.message === 'Log capture initialized')
      expect(initLogs.length).toBe(1)
    })

    it('should capture console.log calls after initialization', () => {
      logStore.initializeLogCapture()

      console.log('Test message')

      const logs = logStore.getLogs()
      const testLog = logs.find(l => l.message === 'Test message')

      expect(testLog).toBeDefined()
      expect(testLog?.level).toBe('info')
      expect(testLog?.source).toBe('renderer')
    })

    it('should capture console.warn calls after initialization', () => {
      logStore.initializeLogCapture()

      console.warn('Warning message')

      const logs = logStore.getLogs()
      const warnLog = logs.find(l => l.message === 'Warning message')

      expect(warnLog).toBeDefined()
      expect(warnLog?.level).toBe('warn')
    })

    it('should capture console.error calls after initialization', () => {
      logStore.initializeLogCapture()

      console.error('Error message')

      const logs = logStore.getLogs()
      const errorLog = logs.find(l => l.message === 'Error message')

      expect(errorLog).toBeDefined()
      expect(errorLog?.level).toBe('error')
    })

    it('should capture console.debug calls after initialization', () => {
      logStore.initializeLogCapture()

      console.debug('Debug message')

      const logs = logStore.getLogs()
      const debugLog = logs.find(l => l.message === 'Debug message')

      expect(debugLog).toBeDefined()
      expect(debugLog?.level).toBe('debug')
    })

    it('should serialize objects to JSON', () => {
      logStore.initializeLogCapture()

      const testObj = { foo: 'bar', num: 42 }
      console.log('Object:', testObj)

      const logs = logStore.getLogs()
      const objLog = logs.find(l => l.message.includes('foo'))

      expect(objLog).toBeDefined()
      expect(objLog?.message).toContain('"foo": "bar"')
      expect(objLog?.message).toContain('"num": 42')
    })

    it('should join multiple arguments', () => {
      logStore.initializeLogCapture()

      console.log('Hello', 'World', 123)

      const logs = logStore.getLogs()
      const multiLog = logs.find(l => l.message.includes('Hello'))

      expect(multiLog).toBeDefined()
      expect(multiLog?.message).toBe('Hello World 123')
    })
  })

  describe('clearLogs', () => {
    it('should clear all logs', () => {
      logStore.initializeLogCapture()
      console.log('Test 1')
      console.log('Test 2')

      expect(logStore.getLogs().length).toBeGreaterThan(0)

      logStore.clearLogs()

      expect(logStore.getLogs().length).toBe(0)
    })

    it('should reset log ID counter', () => {
      logStore.initializeLogCapture()
      console.log('Test')

      logStore.getLogs()[0]

      logStore.clearLogs()

      // After clear, add new log
      console.log('After clear')

      const newLogs = logStore.getLogs()
      // ID should start from 1 again (or close to it)
      expect(newLogs[0].id).toBe(1)
    })
  })

  describe('subscribe', () => {
    it('should notify listener with current logs immediately', () => {
      logStore.initializeLogCapture()
      console.log('Pre-existing log')

      const listener = vi.fn()
      logStore.subscribe(listener)

      expect(listener).toHaveBeenCalledTimes(1)
      expect(listener).toHaveBeenCalledWith(expect.arrayContaining([
        expect.objectContaining({ message: 'Pre-existing log' })
      ]))
    })

    it('should notify listener when new logs are added', () => {
      logStore.initializeLogCapture()

      const listener = vi.fn()
      logStore.subscribe(listener)

      // Clear initial call
      listener.mockClear()

      console.log('New log')

      expect(listener).toHaveBeenCalled()
      const lastCall = listener.mock.calls[listener.mock.calls.length - 1][0]
      expect(lastCall.some((l: { message: string }) => l.message === 'New log')).toBe(true)
    })

    it('should return unsubscribe function', () => {
      logStore.initializeLogCapture()

      const listener = vi.fn()
      const unsubscribe = logStore.subscribe(listener)

      // Clear initial call
      listener.mockClear()

      // Unsubscribe
      unsubscribe()

      console.log('After unsubscribe')

      // Listener should not be called
      expect(listener).not.toHaveBeenCalled()
    })

    it('should support multiple listeners', () => {
      logStore.initializeLogCapture()

      const listener1 = vi.fn()
      const listener2 = vi.fn()

      logStore.subscribe(listener1)
      logStore.subscribe(listener2)

      // Clear initial calls
      listener1.mockClear()
      listener2.mockClear()

      console.log('Multi-listener test')

      expect(listener1).toHaveBeenCalled()
      expect(listener2).toHaveBeenCalled()
    })

    it('should notify listeners on clearLogs', () => {
      logStore.initializeLogCapture()
      console.log('Some log')

      const listener = vi.fn()
      logStore.subscribe(listener)

      // Clear initial call
      listener.mockClear()

      logStore.clearLogs()

      expect(listener).toHaveBeenCalledWith([])
    })
  })

  describe('getLogStats', () => {
    it('should return correct stats for empty logs', () => {
      const stats = logStore.getLogStats()

      expect(stats).toEqual({
        total: 0,
        info: 0,
        warn: 0,
        error: 0,
        debug: 0,
      })
    })

    it('should return correct stats after logging', () => {
      logStore.initializeLogCapture()

      console.log('Info 1')
      console.log('Info 2')
      console.warn('Warning 1')
      console.error('Error 1')
      console.debug('Debug 1')

      const stats = logStore.getLogStats()

      // +1 for initialization message
      expect(stats.total).toBe(6)
      expect(stats.info).toBe(3) // 2 logs + 1 init
      expect(stats.warn).toBe(1)
      expect(stats.error).toBe(1)
      expect(stats.debug).toBe(1)
    })
  })

  describe('MAX_LOGS limit (circular buffer)', () => {
    it('should maintain max 1000 logs', () => {
      logStore.initializeLogCapture()

      // Add more than 1000 logs
      for (let i = 0; i < 1010; i++) {
        console.log(`Log ${i}`)
      }

      const logs = logStore.getLogs()
      expect(logs.length).toBeLessThanOrEqual(1000)
    })

    it('should keep newest logs when limit is reached', () => {
      logStore.initializeLogCapture()

      // Add more than 1000 logs
      for (let i = 0; i < 1010; i++) {
        console.log(`Log ${i}`)
      }

      const logs = logStore.getLogs()

      // Should have the last logs, not the first ones
      // First log should NOT be "Log 0" or "Log capture initialized"
      expect(logs.find(l => l.message === 'Log 0')).toBeUndefined()

      // Should have some of the later logs
      expect(logs.find(l => l.message === 'Log 1009')).toBeDefined()
    })
  })

  describe('log entry structure', () => {
    it('should have correct log entry structure', () => {
      logStore.initializeLogCapture()
      console.log('Test structure')

      const logs = logStore.getLogs()
      const log = logs.find(l => l.message === 'Test structure')

      expect(log).toBeDefined()
      expect(log).toMatchObject({
        id: expect.any(Number),
        timestamp: expect.any(String),
        level: 'info',
        message: 'Test structure',
        source: 'renderer',
      })

      // Verify timestamp is valid ISO string
      expect(new Date(log!.timestamp).toISOString()).toBe(log!.timestamp)
    })

    it('should increment log IDs', () => {
      logStore.initializeLogCapture()

      console.log('First')
      console.log('Second')
      console.log('Third')

      const logs = logStore.getLogs()

      // IDs should be incrementing
      for (let i = 1; i < logs.length; i++) {
        expect(logs[i].id).toBeGreaterThan(logs[i - 1].id)
      }
    })
  })
})
