import { Fragment, useEffect, useMemo, useState } from 'react'
import {
  Trash2,
  Download,
  Copy,
  Check,
  FileText,
  FolderOpen,
  Search,
  ChevronRight,
  ChevronDown,
  CheckCircle2,
} from 'lucide-react'
import { Card } from '../components/Card'
import { PageHeader } from '../components/PageHeader'
import { useConfirm } from '../contexts/ConfirmContext'
import {
  subscribe,
  clearLogs,
  getLogs,
  markAllSeen,
  markReported,
  listLogSessions,
  readLogSession,
  openLogFolder,
  isLogFolderAvailable,
  getAppInfo,
  type AppInfo,
  type LogEntry,
  type LogLevel,
  type LogSession,
  type LogSource,
} from '../services/logStore'
import {
  buildReport,
  filterLogs,
  formatRawLogs,
  groupLogs,
  summaryLine,
  LEVEL_LABELS,
  SOURCE_LABELS,
  type LogGroup,
} from '../utils/logReport'
import { formatDateTime, formatTimestamp } from '../utils/dateUtils'

const LEVELS: LogLevel[] = ['error', 'warn', 'info', 'debug']
const SOURCES: LogSource[] = ['renderer', 'api', 'backend', 'main']
const CURRENT_SESSION = 'current'

const LEVEL_BADGE: Record<LogLevel, string> = {
  error: 'bg-red-100 text-red-700',
  warn: 'bg-amber-100 text-amber-800',
  info: 'bg-blue-50 text-blue-700',
  debug: 'bg-slate-100 text-slate-600',
}

const LEVEL_CHIP_ACTIVE: Record<LogLevel, string> = {
  error: 'bg-red-50 border-red-300 text-red-700',
  warn: 'bg-amber-50 border-amber-300 text-amber-800',
  info: 'bg-blue-50 border-blue-300 text-blue-700',
  debug: 'bg-slate-100 border-slate-300 text-slate-700',
}

const ROW_ACCENT: Record<LogLevel, string> = {
  error: 'border-l-red-500',
  warn: 'border-l-amber-400',
  info: 'border-l-transparent',
  debug: 'border-l-transparent',
}

const LEVEL_TEXT: Record<LogLevel, string> = {
  error: 'Error',
  warn: 'Warning',
  info: 'Info',
  debug: 'Debug',
}

function toggle<T>(set: ReadonlySet<T>, value: T): Set<T> {
  const next = new Set(set)
  if (next.has(value)) next.delete(value)
  else next.add(value)
  return next
}

function download(content: string, fileName: string) {
  const blob = new Blob([content], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}

export default function Logs() {
  const [liveLogs, setLiveLogs] = useState<LogEntry[]>(getLogs())
  const [sessions, setSessions] = useState<LogSession[]>([])
  const [sessionId, setSessionId] = useState(CURRENT_SESSION)
  const [sessionLogs, setSessionLogs] = useState<LogEntry[]>([])
  const [appInfo, setAppInfo] = useState<AppInfo | null>(null)
  const [levels, setLevels] = useState<Set<LogLevel>>(new Set(['error', 'warn']))
  const [sources, setSources] = useState<Set<LogSource>>(new Set(SOURCES))
  const [search, setSearch] = useState('')
  const [expanded, setExpanded] = useState<Set<string>>(new Set())
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [copied, setCopied] = useState<string | null>(null)
  const confirm = useConfirm()

  const isLive = sessionId === CURRENT_SESSION
  const logs = isLive ? liveLogs : sessionLogs

  useEffect(() => subscribe(setLiveLogs), [])

  // Errors on this page count as seen, which clears the badge on the sidebar
  useEffect(() => {
    if (isLive) markAllSeen()
  }, [liveLogs, isLive])

  useEffect(() => {
    listLogSessions().then(setSessions)
    getAppInfo().then(setAppInfo)
  }, [])

  useEffect(() => {
    if (isLive) return
    let cancelled = false
    readLogSession(sessionId)
      .then(entries => { if (!cancelled) setSessionLogs(entries) })
      .catch(() => { if (!cancelled) setSessionLogs([]) })
    return () => { cancelled = true }
  }, [sessionId, isLive])

  useEffect(() => {
    if (!copied) return
    const timer = setTimeout(() => setCopied(null), 2000)
    return () => clearTimeout(timer)
  }, [copied])

  const previousSessions = sessions.filter(s => !s.isCurrent)
  const sessionLabel = isLive
    ? 'This session'
    : `Session started ${formatDateTime(previousSessions.find(s => s.id === sessionId)?.startedAt)}`

  // Level counts follow the source and search filters, so each chip says what it would show
  const levelCounts = useMemo(() => {
    const matching = filterLogs(logs, { levels: new Set(LEVELS), sources, search })
    return Object.fromEntries(LEVELS.map(level => [level, matching.filter(l => l.level === level).length])) as Record<LogLevel, number>
  }, [logs, sources, search])

  const groups = useMemo(
    () => groupLogs(filterLogs(logs, { levels, sources, search })),
    [logs, levels, sources, search]
  )

  const problemCount = logs.filter(l => l.level === 'error' || l.level === 'warn').length
  const selectedGroups = groups.filter(g => selected.has(g.key))
  const reportGroups = selectedGroups.length > 0
    ? selectedGroups
    : groups.filter(g => g.level === 'error' || g.level === 'warn')
  const allSelected = groups.length > 0 && groups.every(g => selected.has(g.key))

  const selectSession = (id: string) => {
    setSessionId(id)
    setSelected(new Set())
    setExpanded(new Set())
  }

  const reportFor = (reportOn: LogGroup[]) => buildReport(reportOn, logs, appInfo, sessionLabel)

  const markGroupsReported = (reportOn: LogGroup[]) => {
    if (isLive) markReported(reportOn.flatMap(g => g.entries.map(e => e.id)))
  }

  const copyText = async (text: string, key: string, reportOn: LogGroup[]) => {
    try {
      await navigator.clipboard.writeText(text)
      markGroupsReported(reportOn)
      setCopied(key)
    } catch {
      // Clipboard refused (no focus, no permission): save the report instead
      saveReport(reportOn)
    }
  }

  const saveReport = (reportOn: LogGroup[]) => {
    download(reportFor(reportOn), `hive-error-report-${new Date().toISOString().replace(/[:.]/g, '-')}.txt`)
    markGroupsReported(reportOn)
  }

  const downloadAll = () => {
    const ordered = [...logs].sort((a, b) => a.timestamp.localeCompare(b.timestamp) || a.id - b.id)
    const name = isLive ? `hive-logs-${new Date().toISOString().split('T')[0]}.txt` : sessionId.replace(/\.jsonl$/, '.txt')
    download(formatRawLogs(ordered), name)
  }

  const askToClear = async () => {
    const confirmed = await confirm({
      title: 'Clear logs',
      message: `This empties the list on this page. ${isLogFolderAvailable()
        ? 'The entries stay saved in the log folder and can still be opened from there.'
        : 'Copy a report first if you still need to send something.'}`,
      confirmLabel: 'Clear'
    })
    if (!confirmed) return
    clearLogs()
    setSelected(new Set())
    setExpanded(new Set())
  }

  const reportLabel = selectedGroups.length > 0 ? `Copy report (${selectedGroups.length})` : 'Copy report'
  const reportTitle = selectedGroups.length > 0
    ? 'Copy the selected problems, with app details and the entries leading up to each'
    : 'Copy every error and warning shown, with app details and the entries leading up to each'

  return (
    <div className="space-y-4">
      {/* Header */}
      <PageHeader
        title="Logs"
        subtitle="Find what went wrong and send it to the developers"
        actions={
          <>
            {previousSessions.length > 0 && (
              <select
                value={sessionId}
                onChange={(e) => selectSession(e.target.value)}
                aria-label="Log session"
                className="px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-700"
              >
                <option value={CURRENT_SESSION}>This session</option>
                {previousSessions.map(s => (
                  <option key={s.id} value={s.id}>
                    Started {formatDateTime(s.startedAt)}
                  </option>
                ))}
              </select>
            )}
            {isLogFolderAvailable() && (
              <button
                onClick={() => { openLogFolder() }}
                className="flex items-center gap-2 px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg hover:bg-slate-50 text-slate-700"
                title="Open the folder with the saved log files"
              >
                <FolderOpen className="w-4 h-4" />
                Log folder
              </button>
            )}
            <button
              onClick={downloadAll}
              className="p-2 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 text-slate-700"
              title="Download every entry in this session"
              aria-label="Download all logs"
            >
              <Download className="w-5 h-5" />
            </button>
            {isLive && (
              <button
                onClick={askToClear}
                disabled={logs.length === 0}
                className="p-2 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-40 disabled:cursor-not-allowed"
                title="Clear logs"
                aria-label="Clear logs"
              >
                <Trash2 className="w-5 h-5" />
              </button>
            )}
          </>
        }
      />

      {/* Health */}
      {problemCount === 0 && logs.length > 0 && (
        <div className="flex items-center gap-3 px-4 py-3 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-800">
          <CheckCircle2 className="w-5 h-5 shrink-0" />
          <span className="text-sm font-medium">
            No errors or warnings {isLive ? 'since' : 'in the session started'} {formatDateTime(logs[0].timestamp)}
          </span>
        </div>
      )}

      {!isLive && (
        <div className="px-4 py-2 bg-slate-100 border border-slate-200 rounded-lg text-sm text-slate-600">
          Viewing a previous session, read from the log folder.{' '}
          <button onClick={() => selectSession(CURRENT_SESSION)} className="font-medium text-amber-700 hover:underline">
            Back to this session
          </button>
        </div>
      )}

      {/* Filters and report */}
      <div className="flex flex-wrap items-center gap-2">
        {LEVELS.map(level => (
          <button
            key={level}
            onClick={() => setLevels(toggle(levels, level))}
            aria-pressed={levels.has(level)}
            className={`px-3 py-1.5 text-sm rounded-full border font-medium transition-colors ${
              levels.has(level) ? LEVEL_CHIP_ACTIVE[level] : 'bg-white border-slate-200 text-slate-500 hover:bg-slate-50'
            }`}
          >
            {LEVEL_LABELS[level]} <span className="tabular-nums">{levelCounts[level]}</span>
          </button>
        ))}
        <span className="mx-1 h-6 border-l border-slate-200" />
        {SOURCES.map(source => (
          <button
            key={source}
            onClick={() => setSources(toggle(sources, source))}
            aria-pressed={sources.has(source)}
            className={`px-3 py-1.5 text-sm rounded-full border transition-colors ${
              sources.has(source) ? 'bg-slate-800 border-slate-800 text-white' : 'bg-white border-slate-200 text-slate-500 hover:bg-slate-50'
            }`}
          >
            {SOURCE_LABELS[source]}
          </button>
        ))}
        <div className="relative ml-auto">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search messages"
            aria-label="Search logs"
            className="pl-9 pr-3 py-2 w-64 text-sm bg-white border border-slate-300 rounded-lg text-slate-700"
          />
        </div>
        <button
          onClick={() => copyText(reportFor(reportGroups), 'report', reportGroups)}
          disabled={reportGroups.length === 0}
          title={reportTitle}
          className="flex items-center gap-2 px-4 py-2 text-sm font-medium bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {copied === 'report' ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
          {copied === 'report' ? 'Copied' : reportLabel}
        </button>
        <button
          onClick={() => saveReport(reportGroups)}
          disabled={reportGroups.length === 0}
          title="Save the report as a text file to attach"
          aria-label="Save report"
          className="p-2 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          <FileText className="w-5 h-5" />
        </button>
      </div>

      {/* Entries */}
      <Card className="overflow-hidden">
        <div className="max-h-[calc(100vh-300px)] min-h-[300px] overflow-auto">
          <table className="w-full text-sm">
            <thead className="sticky top-0 bg-slate-50 border-b border-slate-200 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 z-10">
              <tr>
                <th className="w-10 pl-4 py-2">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={() => setSelected(allSelected ? new Set() : new Set(groups.map(g => g.key)))}
                    disabled={groups.length === 0}
                    aria-label="Select all"
                    className="rounded-sm border-slate-300"
                  />
                </th>
                <th className="w-6 py-2" />
                <th className="py-2 pr-4 whitespace-nowrap">Last seen</th>
                <th className="py-2 pr-4">Level</th>
                <th className="py-2 pr-4">Source</th>
                <th className="py-2 pr-4 w-full">Message</th>
                <th className="py-2 pr-4 text-right">Count</th>
              </tr>
            </thead>
            <tbody>
              {groups.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-slate-500">
                    {logs.length === 0 ? 'No log entries yet.' : 'No entries match these filters.'}
                  </td>
                </tr>
              ) : (
                groups.map(group => {
                  const isExpanded = expanded.has(group.key)
                  return (
                    <Fragment key={group.key}>
                      <tr
                        onClick={() => setExpanded(toggle(expanded, group.key))}
                        className={`border-b border-slate-100 border-l-4 ${ROW_ACCENT[group.level]} cursor-pointer hover:bg-slate-50 ${
                          isExpanded ? 'bg-slate-50' : ''
                        } ${group.reported ? 'opacity-60' : ''}`}
                      >
                        <td className="pl-3 py-2 align-top" onClick={(e) => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={selected.has(group.key)}
                            onChange={() => setSelected(toggle(selected, group.key))}
                            aria-label={`Select ${summaryLine(group.message)}`}
                            className="rounded-sm border-slate-300"
                          />
                        </td>
                        <td className="py-2 align-top text-slate-400">
                          {isExpanded ? <ChevronDown className="w-4 h-4" /> : <ChevronRight className="w-4 h-4" />}
                        </td>
                        <td className="py-2 pr-4 align-top whitespace-nowrap font-mono text-xs text-slate-500">
                          {formatTimestamp(group.last.timestamp)}
                        </td>
                        <td className="py-2 pr-4 align-top">
                          <span className={`px-2 py-0.5 rounded-sm text-xs font-medium ${LEVEL_BADGE[group.level]}`}>
                            {LEVEL_TEXT[group.level]}
                          </span>
                        </td>
                        <td className="py-2 pr-4 align-top text-slate-600 whitespace-nowrap">
                          {SOURCE_LABELS[group.source]}
                        </td>
                        <td className="py-2 pr-4 align-top text-slate-900 max-w-0">
                          <div className="flex items-center gap-2">
                            <span className="truncate font-mono text-xs">{summaryLine(group.message)}</span>
                            {group.reported && (
                              <span className="shrink-0 px-1.5 py-0.5 rounded-sm bg-slate-200 text-slate-600 text-[10px] font-medium uppercase">
                                Reported
                              </span>
                            )}
                          </div>
                        </td>
                        <td className="py-2 pr-4 align-top text-right tabular-nums text-slate-600">
                          {group.entries.length > 1 ? `×${group.entries.length}` : ''}
                        </td>
                      </tr>
                      {isExpanded && (
                        <tr className={`border-b border-slate-200 border-l-4 ${ROW_ACCENT[group.level]} bg-slate-50`}>
                          <td colSpan={7} className="px-6 pb-4 pt-1">
                            <div className="flex items-center justify-between gap-4 mb-2 text-xs text-slate-500">
                              <span>
                                {group.entries.length > 1
                                  ? `${group.entries.length} times, first ${formatTimestamp(group.first.timestamp)}, last ${formatTimestamp(group.last.timestamp)}`
                                  : formatTimestamp(group.first.timestamp)}
                              </span>
                              <button
                                onClick={() => copyText(reportFor([group]), group.key, [group])}
                                className="flex items-center gap-1.5 px-2.5 py-1 bg-white border border-slate-300 rounded-md hover:bg-slate-100 text-slate-700"
                              >
                                {copied === group.key ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                                {copied === group.key ? 'Copied' : 'Copy'}
                              </button>
                            </div>
                            <pre className="whitespace-pre-wrap wrap-break-word font-mono text-xs text-slate-900 bg-white border border-slate-200 rounded-md p-3">
                              {group.message}
                            </pre>
                            {group.details && (
                              <pre className="mt-2 max-h-80 overflow-auto whitespace-pre-wrap wrap-break-word font-mono text-xs text-slate-600 bg-white border border-slate-200 rounded-md p-3">
                                {group.details}
                              </pre>
                            )}
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )
}
