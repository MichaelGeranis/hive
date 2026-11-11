import { createContext, ReactNode, useCallback, useContext, useMemo, useRef, useState } from 'react'

interface HiveStatus {
  /** How many warnings Needs attention showed when the dashboard last loaded; null until it has */
  attention: number | null
  reportAttention: (count: number) => void
  /** Something is running: a sync, a backup */
  busy: boolean
  /** Marks Hive busy while the work runs, however it ends */
  trackWork: <T>(work: Promise<T>) => Promise<T>
}

// Without a provider (in a component's own test) the status is simply never shown
const HiveStatusContext = createContext<HiveStatus>({
  attention: null,
  reportAttention: () => {},
  busy: false,
  trackWork: work => work
})

/**
 * What Hive's mark shows from any page: whether something needs attention, and whether Hive
 * is working.
 */
export function HiveStatusProvider({ children }: { children: ReactNode }) {
  const [attention, setAttention] = useState<number | null>(null)
  const [running, setRunning] = useState(0)
  const runningRef = useRef(0)

  const trackWork = useCallback(<T,>(work: Promise<T>) => {
    runningRef.current += 1
    setRunning(runningRef.current)
    return work.finally(() => {
      runningRef.current -= 1
      setRunning(runningRef.current)
    })
  }, [])

  const value = useMemo<HiveStatus>(
    () => ({ attention, reportAttention: setAttention, busy: running > 0, trackWork }),
    [attention, running, trackWork]
  )

  return <HiveStatusContext.Provider value={value}>{children}</HiveStatusContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components -- the hook belongs with its provider
export function useHiveStatus() {
  return useContext(HiveStatusContext)
}
