import { useEffect, useCallback } from 'react'

export function useEscapeKey(handler: () => void, isActive: boolean = true) {
  const memoizedHandler = useCallback(handler, [handler])

  useEffect(() => {
    if (!isActive) return

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        memoizedHandler()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [memoizedHandler, isActive])
}
