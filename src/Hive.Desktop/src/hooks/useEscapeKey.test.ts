import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useEscapeKey } from './useEscapeKey'

describe('useEscapeKey', () => {
  let addEventListenerSpy: ReturnType<typeof vi.spyOn>
  let removeEventListenerSpy: ReturnType<typeof vi.spyOn>

  beforeEach(() => {
    addEventListenerSpy = vi.spyOn(document, 'addEventListener')
    removeEventListenerSpy = vi.spyOn(document, 'removeEventListener')
  })

  afterEach(() => {
    addEventListenerSpy.mockRestore()
    removeEventListenerSpy.mockRestore()
  })

  it('should call handler when Escape key is pressed and isActive is true', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler, true))

    // Simulate Escape key press
    const event = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event)

    expect(handler).toHaveBeenCalledTimes(1)
  })

  it('should NOT call handler when Escape key is pressed and isActive is false', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler, false))

    // Simulate Escape key press
    const event = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event)

    expect(handler).not.toHaveBeenCalled()
  })

  it('should NOT call handler when other keys are pressed', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler, true))

    // Simulate other key presses
    const enterEvent = new KeyboardEvent('keydown', { key: 'Enter' })
    document.dispatchEvent(enterEvent)

    const spaceEvent = new KeyboardEvent('keydown', { key: ' ' })
    document.dispatchEvent(spaceEvent)

    const aEvent = new KeyboardEvent('keydown', { key: 'a' })
    document.dispatchEvent(aEvent)

    expect(handler).not.toHaveBeenCalled()
  })

  it('should add event listener on mount when isActive is true', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler, true))

    expect(addEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function))
  })

  it('should NOT add event listener on mount when isActive is false', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler, false))

    // The listener should not be added for keydown when inactive
    const keydownCalls = addEventListenerSpy.mock.calls.filter(
      call => call[0] === 'keydown'
    )
    expect(keydownCalls.length).toBe(0)
  })

  it('should remove event listener on unmount', () => {
    const handler = vi.fn()

    const { unmount } = renderHook(() => useEscapeKey(handler, true))

    unmount()

    expect(removeEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function))
  })

  it('should default isActive to true when not provided', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler))

    // Simulate Escape key press
    const event = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event)

    expect(handler).toHaveBeenCalledTimes(1)
  })

  it('should respond to isActive changes', () => {
    const handler = vi.fn()

    const { rerender } = renderHook(
      ({ isActive }) => useEscapeKey(handler, isActive),
      { initialProps: { isActive: false } }
    )

    // Should not call handler when inactive
    const event1 = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event1)
    expect(handler).not.toHaveBeenCalled()

    // Activate the hook
    rerender({ isActive: true })

    // Should now call handler
    const event2 = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event2)
    expect(handler).toHaveBeenCalledTimes(1)
  })

  it('should update handler when it changes', () => {
    const handler1 = vi.fn()
    const handler2 = vi.fn()

    const { rerender } = renderHook(
      ({ handler }) => useEscapeKey(handler, true),
      { initialProps: { handler: handler1 } }
    )

    // First handler should be called
    const event1 = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event1)
    expect(handler1).toHaveBeenCalledTimes(1)
    expect(handler2).not.toHaveBeenCalled()

    // Update to new handler
    rerender({ handler: handler2 })

    // New handler should be called
    const event2 = new KeyboardEvent('keydown', { key: 'Escape' })
    document.dispatchEvent(event2)
    expect(handler1).toHaveBeenCalledTimes(1) // Still 1
    expect(handler2).toHaveBeenCalledTimes(1)
  })

  it('should handle multiple rapid Escape presses', () => {
    const handler = vi.fn()

    renderHook(() => useEscapeKey(handler, true))

    // Simulate multiple rapid key presses
    for (let i = 0; i < 5; i++) {
      const event = new KeyboardEvent('keydown', { key: 'Escape' })
      document.dispatchEvent(event)
    }

    expect(handler).toHaveBeenCalledTimes(5)
  })
})
