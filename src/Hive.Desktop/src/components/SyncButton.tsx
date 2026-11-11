import { useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { useHiveStatus } from '../contexts/HiveStatusContext'

interface SyncButtonProps {
  /** Runs the sync and reports its outcome; the button only shows that it is running. */
  onSync: () => Promise<void>
  /** Tooltip saying what this button pulls. */
  title: string
  label: string
}

/**
 * The one "pull from an outside system" button: Jira on the Dashboard and its pages,
 * Google Calendar on the Daily Planner.
 */
export function SyncButton({ onSync, title, label }: SyncButtonProps) {
  const [syncing, setSyncing] = useState(false)
  const { trackWork } = useHiveStatus()

  const handleClick = async () => {
    setSyncing(true)
    try {
      await trackWork(onSync())
    } finally {
      setSyncing(false)
    }
  }

  return (
    <button
      onClick={handleClick}
      disabled={syncing}
      title={title}
      className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
    >
      <RefreshCw className={`w-4 h-4 ${syncing ? 'animate-spin' : ''}`} />
      {syncing ? 'Syncing…' : label}
    </button>
  )
}
