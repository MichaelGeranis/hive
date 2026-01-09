import { useEffect, useState, useRef } from 'react'
import { Trash2, Download } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { subscribe, clearLogs, getLogs, type LogEntry } from '../services/logStore'

const LOG_COLORS = {
  info: 'text-blue-600 dark:text-blue-400',
  warn: 'text-amber-600 dark:text-amber-400',
  error: 'text-red-600 dark:text-red-400',
  debug: 'text-slate-500 dark:text-slate-400',
}

const LOG_BG = {
  info: '',
  warn: 'bg-amber-50 dark:bg-amber-900/10',
  error: 'bg-red-50 dark:bg-red-900/10',
  debug: '',
}

type FilterType = 'all' | 'warn-error' | 'info' | 'warn' | 'error' | 'debug'

export default function Logs() {
  const [logs, setLogs] = useState<LogEntry[]>(getLogs())
  const [filter, setFilter] = useState<FilterType>('warn-error')
  const [autoScroll, setAutoScroll] = useState(true)
  const logContainerRef = useRef<HTMLDivElement>(null)
  const prevLogsLengthRef = useRef(logs.length)

  // Subscribe to log updates
  useEffect(() => {
    const unsubscribe = subscribe((newLogs) => {
      setLogs(newLogs)
    })

    return unsubscribe
  }, [])

  // Auto-scroll to bottom when new logs arrive
  useEffect(() => {
    if (autoScroll && logContainerRef.current && logs.length > prevLogsLengthRef.current) {
      logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight
    }
    prevLogsLengthRef.current = logs.length
  }, [logs, autoScroll])

  const handleClearLogs = () => {
    clearLogs()
  }

  const downloadLogs = () => {
    const content = logs.map(log =>
      `[${log.timestamp}] [${log.level.toUpperCase()}] [${log.source}] ${log.message}`
    ).join('\n')

    const blob = new Blob([content], { type: 'text/plain' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `hive-logs-${new Date().toISOString().split('T')[0]}.txt`
    a.click()
    URL.revokeObjectURL(url)
  }

  const filteredLogs = (() => {
    switch (filter) {
      case 'all':
        return logs
      case 'warn-error':
        return logs.filter(log => log.level === 'warn' || log.level === 'error')
      default:
        return logs.filter(log => log.level === filter)
    }
  })()

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp)
    return date.toLocaleTimeString('en-US', {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      fractionalSecondDigits: 3
    })
  }

  const getFilterLabel = () => {
    switch (filter) {
      case 'all': return 'entries'
      case 'warn-error': return 'warnings/errors'
      default: return `${filter} entries`
    }
  }

  // Count by level for status display
  const errorCount = logs.filter(l => l.level === 'error').length
  const warnCount = logs.filter(l => l.level === 'warn').length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Logs</h1>
          <p className="text-slate-500 dark:text-slate-400">Application logs and debug information</p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as FilterType)}
            className="px-3 py-2 text-sm bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-700 dark:text-slate-300"
          >
            <option value="warn-error">Warnings & Errors</option>
            <option value="all">All Levels</option>
            <option value="info">Info</option>
            <option value="warn">Warning</option>
            <option value="error">Error</option>
            <option value="debug">Debug</option>
          </select>
          <label className="flex items-center gap-2 px-3 py-2 text-sm bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-700 dark:text-slate-300 cursor-pointer">
            <input
              type="checkbox"
              checked={autoScroll}
              onChange={(e) => setAutoScroll(e.target.checked)}
              className="rounded border-slate-300 dark:border-slate-600 text-amber-500 focus:ring-amber-500"
            />
            Auto-scroll
          </label>
          <button
            onClick={downloadLogs}
            className="p-2 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300"
            title="Download all logs"
          >
            <Download className="w-5 h-5" />
          </button>
          <button
            onClick={handleClearLogs}
            className="p-2 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300"
            title="Clear logs"
          >
            <Trash2 className="w-5 h-5" />
          </button>
        </div>
      </div>

      {/* Status Bar */}
      <div className="flex items-center gap-4 text-sm">
        <span className="text-slate-600 dark:text-slate-400">
          {filteredLogs.length} {getFilterLabel()} (of {logs.length} total)
        </span>
        {errorCount > 0 && (
          <span className="px-2 py-1 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 rounded text-xs font-medium">
            {errorCount} errors
          </span>
        )}
        {warnCount > 0 && (
          <span className="px-2 py-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 rounded text-xs font-medium">
            {warnCount} warnings
          </span>
        )}
      </div>

      {/* Log Viewer */}
      <Card>
        <CardHeader title="Console Output" />
        <CardContent>
          <div
            ref={logContainerRef}
            className="h-[calc(100vh-350px)] min-h-[400px] overflow-auto bg-slate-950 rounded-lg p-4 font-mono text-sm"
          >
            {filteredLogs.length === 0 ? (
              <div className="text-slate-500 text-center py-8">
                {filter === 'warn-error'
                  ? 'No warnings or errors. Application is running smoothly.'
                  : 'No logs matching the selected filter.'}
              </div>
            ) : (
              filteredLogs.map(log => (
                <div
                  key={log.id}
                  className={`py-1 px-2 hover:bg-slate-900 rounded ${LOG_BG[log.level]}`}
                >
                  <span className="text-slate-500">{formatTimestamp(log.timestamp)}</span>
                  <span className={`ml-2 font-semibold ${LOG_COLORS[log.level]}`}>
                    [{log.level.toUpperCase()}]
                  </span>
                  <span className="ml-2 text-slate-400">[{log.source}]</span>
                  <span className="ml-2 text-slate-200 whitespace-pre-wrap break-all">{log.message}</span>
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
