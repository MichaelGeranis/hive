import { useEffect, useState } from 'react'
import { History } from 'lucide-react'
import { backupApi } from '../services/api'
import { formatDateTime } from '../utils/dateUtils'
import type { AutomaticBackupStatus } from '../types'

/**
 * The automatic backups of the database file: when they run, where they go and the last one.
 */
export function AutomaticBackupsPanel() {
  const [status, setStatus] = useState<AutomaticBackupStatus | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let cancelled = false
    backupApi.getAutomaticStatus()
      .then(result => { if (!cancelled) setStatus(result) })
      .catch(err => {
        console.error('Failed to load the automatic backups', err)
        if (!cancelled) setFailed(true)
      })
    return () => { cancelled = true }
  }, [])

  return (
    <div>
      <h3 className="mb-2 flex items-center gap-2 text-sm font-medium text-slate-700">
        <History className="h-4 w-4 text-slate-400" />
        Automatic Backups
      </h3>
      {failed ? (
        <p className="text-sm text-slate-500">Could not read the automatic backups.</p>
      ) : !status ? (
        <p className="text-sm text-slate-400">Checking…</p>
      ) : !status.enabled ? (
        <p className="text-sm text-slate-600">
          This Hive keeps its data in memory, so there is no database file to back up. Automatic backups run with the
          SQLite database.
        </p>
      ) : (
        <div className="space-y-1 text-sm text-slate-600">
          <p>
            Hive copies its whole database when it starts and every {status.intervalHours === 1 ? 'hour' : `${status.intervalHours} hours`} while
            it runs, when something changed. It keeps a day of copies, then one a day for a month.
          </p>
          <p data-testid="last-automatic-backup">
            {status.lastBackupAt
              ? <>Last backup: <strong className="font-medium text-slate-900">{formatDateTime(status.lastBackupAt)}</strong> · {status.count} kept</>
              : 'No backup yet.'}
          </p>
          {status.folder && (
            <p className="break-all text-xs text-slate-400">In {status.folder}</p>
          )}
        </div>
      )}
    </div>
  )
}
