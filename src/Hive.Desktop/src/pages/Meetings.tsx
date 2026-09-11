import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { AlertCircle, MessageSquare, Plus, Search, User, Users, X } from 'lucide-react'
import MeetingEditor from '../components/MeetingEditor'
import { directReportsApi, meetingsApi } from '../services/api'
import type { DirectReport, MeetingCount, OneOnOneMeeting } from '../types'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

type Scope = { kind: 'all' } | { kind: 'person'; directReportId: string } | { kind: 'unlinked' }

const AUTOSAVE_DELAY_MS = 700
const PAGE_SIZE = 100

/** The tag that names a person without ambiguity: their first and last name joined. */
function tagFor(report: DirectReport): string {
  return `${report.firstName}${report.lastName}`.replace(/[^a-zA-Z0-9]/g, '').toLowerCase()
}

function formatMeetingDate(value: string): string {
  const date = new Date(`${value}T00:00:00`)
  const now = new Date()
  const sameYear = date.getFullYear() === now.getFullYear()
  return date.toLocaleDateString(
    [],
    sameYear ? { month: 'short', day: 'numeric' } : { year: 'numeric', month: 'short', day: 'numeric' }
  )
}

/**
 * 1:1 Meetings: the people on the left, their 1:1s in the middle, the note itself on the
 * right. A 1:1 is written during the meeting and saved as it is typed.
 */
export default function Meetings() {
  const { showError } = useToast()

  const [reports, setReports] = useState<DirectReport[]>([])
  const [counts, setCounts] = useState<MeetingCount[]>([])
  const [meetings, setMeetings] = useState<OneOnOneMeeting[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [scope, setScope] = useState<Scope>({ kind: 'all' })
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [searchInput, setSearchInput] = useState('')
  const [searchTerm, setSearchTerm] = useState('')
  const [loading, setLoading] = useState(true)
  const [draft, setDraft] = useState('')
  const [saving, setSaving] = useState(false)
  const [startInEditMode, setStartInEditMode] = useState(false)

  // Autosave bookkeeping: what is being written, and what has not reached the server yet.
  const draftRef = useRef('')
  const editingIdRef = useRef<string | null>(null)
  const dirtyRef = useRef(false)
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const selectedMeeting = useMemo(
    () => meetings.find((meeting) => meeting.id === selectedId) ?? null,
    [meetings, selectedId]
  )

  const countFor = useCallback(
    (directReportId: string | null) =>
      counts.find((c) => (c.directReportId ?? null) === directReportId)?.count ?? 0,
    [counts]
  )

  const patchMeeting = useCallback((updated: OneOnOneMeeting) => {
    setMeetings((current) => current.map((meeting) => (meeting.id === updated.id ? updated : meeting)))
  }, [])

  const loadSidebar = useCallback(async () => {
    try {
      const [people, meetingCounts] = await Promise.all([
        directReportsApi.getAll(),
        meetingsApi.getCounts()
      ])
      setReports(people)
      setCounts(meetingCounts)
    } catch (error) {
      console.error('Failed to load 1:1 sidebar:', error)
    }
  }, [])

  const openMeeting = useCallback((meeting: OneOnOneMeeting | null, editing = false) => {
    editingIdRef.current = meeting?.id ?? null
    dirtyRef.current = false
    draftRef.current = meeting?.content ?? ''
    setSelectedId(meeting?.id ?? null)
    setDraft(meeting?.content ?? '')
    setStartInEditMode(editing)
  }, [])

  const saveDraft = useCallback(async () => {
    const meetingId = editingIdRef.current
    if (!meetingId || !dirtyRef.current) return

    dirtyRef.current = false
    setSaving(true)
    try {
      patchMeeting(await meetingsApi.updateContent(meetingId, draftRef.current))
    } catch (error) {
      dirtyRef.current = true
      console.error('Failed to save 1:1:', error)
      showError(getErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }, [patchMeeting, showError])

  const flushPendingSave = useCallback(async () => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current)
      saveTimerRef.current = null
    }
    await saveDraft()
  }, [saveDraft])

  const loadMeetings = useCallback(
    async (currentScope: Scope, search: string, keepSelection = false) => {
      setLoading(true)
      try {
        const result = await meetingsApi.getAll(
          1,
          PAGE_SIZE,
          currentScope.kind === 'person' ? currentScope.directReportId : undefined,
          currentScope.kind === 'unlinked',
          search || undefined
        )
        setMeetings(result.items)
        setTotalCount(result.totalCount)

        const stillVisible = keepSelection && result.items.some((m) => m.id === editingIdRef.current)
        if (!stillVisible) {
          openMeeting(result.items[0] ?? null)
        }
      } catch (error) {
        console.error('Failed to load 1:1s:', error)
        showError(getErrorMessage(error))
      } finally {
        setLoading(false)
      }
    },
    [openMeeting, showError]
  )

  useEffect(() => {
    loadSidebar()
  }, [loadSidebar])

  // The list follows the person selected and the search box, saving first so that
  // switching away never loses a keystroke.
  useEffect(() => {
    let cancelled = false
    const run = async () => {
      await flushPendingSave()
      if (!cancelled) {
        await loadMeetings(scope, searchTerm, true)
      }
    }
    run()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [scope, searchTerm])

  useEffect(() => {
    const timeoutId = setTimeout(() => setSearchTerm(searchInput), 300)
    return () => clearTimeout(timeoutId)
  }, [searchInput])

  // Save whatever is still unsaved when leaving the page.
  useEffect(() => {
    return () => {
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current)
      }
      if (dirtyRef.current && editingIdRef.current) {
        meetingsApi.updateContent(editingIdRef.current, draftRef.current).catch(() => undefined)
      }
    }
  }, [])

  const handleContentChange = useCallback(
    (value: string) => {
      setDraft(value)
      draftRef.current = value
      dirtyRef.current = true
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current)
      }
      saveTimerRef.current = setTimeout(() => {
        saveTimerRef.current = null
        saveDraft()
      }, AUTOSAVE_DELAY_MS)
    },
    [saveDraft]
  )

  const handleNewMeeting = useCallback(async () => {
    await flushPendingSave()
    try {
      const person = scope.kind === 'person' ? reports.find((r) => r.id === scope.directReportId) : undefined
      const created = await meetingsApi.createBlank({ tags: person ? tagFor(person) : undefined })
      setMeetings((current) => [created, ...current])
      setTotalCount((count) => count + 1)
      openMeeting(created, true)
      loadSidebar()
    } catch (error) {
      console.error('Failed to start a 1:1 note:', error)
      showError(getErrorMessage(error))
    }
  }, [flushPendingSave, loadSidebar, openMeeting, reports, scope, showError])

  const handleSelect = async (meeting: OneOnOneMeeting) => {
    if (meeting.id === selectedId) return
    await flushPendingSave()
    openMeeting(meeting)
  }

  const handleTagsChange = async (tags: string) => {
    if (!selectedMeeting) return
    await flushPendingSave()
    try {
      const updated = await meetingsApi.updateTags(selectedMeeting.id, tags)
      patchMeeting(updated)
      loadSidebar()

      // Re-tagging can move the 1:1 out of the list being shown.
      const leftScope =
        (scope.kind === 'person' && updated.directReportId !== scope.directReportId) ||
        (scope.kind === 'unlinked' && !updated.isUnlinked)
      if (leftScope) {
        await loadMeetings(scope, searchTerm, false)
      }
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const handleDateChange = async (meetingDate: string) => {
    if (!selectedMeeting) return
    try {
      patchMeeting(await meetingsApi.updateDate(selectedMeeting.id, meetingDate))
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const handleDelete = async () => {
    if (!selectedMeeting) return
    if (!confirm(`Delete this 1:1? This cannot be undone.`)) return

    const deletedId = selectedMeeting.id
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current)
      saveTimerRef.current = null
    }
    dirtyRef.current = false

    try {
      await meetingsApi.delete(deletedId)
      const remaining = meetings.filter((meeting) => meeting.id !== deletedId)
      setMeetings(remaining)
      setTotalCount((count) => Math.max(0, count - 1))
      openMeeting(remaining[0] ?? null)
      loadSidebar()
    } catch (error) {
      showError(getErrorMessage(error))
    }
  }

  const scopeTitle =
    scope.kind === 'all'
      ? 'All 1:1s'
      : scope.kind === 'unlinked'
      ? 'Unlinked'
      : reports.find((r) => r.id === scope.directReportId)?.fullName ?? '1:1s'

  const railButton = (
    key: string,
    label: string,
    count: number,
    active: boolean,
    icon: JSX.Element,
    onClick: () => void
  ) => (
    <button
      key={key}
      onClick={onClick}
      aria-label={`${label}, ${count} 1:1s`}
      className={`flex w-full items-center gap-2 rounded-lg px-2 py-2 text-left text-sm font-medium transition-colors ${
        active
          ? 'bg-purple-500 text-white'
          : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-700/60'
      }`}
    >
      {icon}
      <span className="flex-1 truncate">{label}</span>
      <span className={`text-xs ${active ? 'text-purple-100' : 'text-slate-400'}`}>{count}</span>
    </button>
  )

  return (
    <div className="flex h-[calc(100vh-5rem)] overflow-hidden rounded-xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
      {/* People */}
      <aside className="hidden w-56 shrink-0 flex-col gap-0.5 overflow-y-auto border-r border-slate-200 bg-slate-50 p-2 dark:border-slate-700 dark:bg-slate-900/40 lg:flex">
        {railButton(
          'all',
          'All 1:1s',
          counts.reduce((sum, c) => sum + c.count, 0),
          scope.kind === 'all',
          <Users className="h-4 w-4 shrink-0" />,
          () => setScope({ kind: 'all' })
        )}

        <div className="px-2 pb-1 pt-3 text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          Team
        </div>

        {reports.map((report) =>
          railButton(
            report.id,
            report.fullName,
            countFor(report.id),
            scope.kind === 'person' && scope.directReportId === report.id,
            <User className="h-4 w-4 shrink-0" />,
            () => setScope({ kind: 'person', directReportId: report.id })
          )
        )}

        {countFor(null) > 0 &&
          railButton(
            'unlinked',
            'Unlinked',
            countFor(null),
            scope.kind === 'unlinked',
            <AlertCircle className="h-4 w-4 shrink-0 text-amber-500" />,
            () => setScope({ kind: 'unlinked' })
          )}
      </aside>

      {/* 1:1 list */}
      <section
        aria-label="1:1 list"
        className="flex w-full shrink-0 flex-col border-r border-slate-200 dark:border-slate-700 sm:w-80"
      >
        <div className="space-y-3 border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <div className="flex items-center justify-between gap-2">
            <div className="min-w-0">
              <h1 className="truncate text-lg font-bold text-slate-900 dark:text-white">{scopeTitle}</h1>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {totalCount} {totalCount === 1 ? '1:1' : '1:1s'}
              </p>
            </div>
            <button
              onClick={handleNewMeeting}
              title="New 1:1"
              aria-label="New 1:1"
              className="flex items-center gap-1.5 rounded-lg bg-purple-500 px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-purple-600"
            >
              <Plus className="h-4 w-4" />
              New
            </button>
          </div>

          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Search 1:1s"
              aria-label="Search 1:1s"
              className="w-full rounded-lg border border-slate-300 bg-white py-2 pl-9 pr-8 text-sm text-slate-900 focus:ring-2 focus:ring-purple-500 dark:border-slate-600 dark:bg-slate-700 dark:text-white"
            />
            {searchInput && (
              <button
                onClick={() => setSearchInput('')}
                aria-label="Clear search"
                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
        </div>

        <div className="flex-1 overflow-y-auto">
          {loading && meetings.length === 0 ? (
            <div className="flex h-32 items-center justify-center">
              <div className="h-6 w-6 animate-spin rounded-full border-b-2 border-purple-500" />
            </div>
          ) : meetings.length === 0 ? (
            <div className="px-6 py-12 text-center">
              <MessageSquare className="mx-auto mb-3 h-10 w-10 text-slate-300 dark:text-slate-600" />
              <p className="text-sm text-slate-500 dark:text-slate-400">
                {searchTerm ? 'No 1:1s match your search.' : 'No 1:1s here yet.'}
              </p>
              {!searchTerm && (
                <button
                  onClick={handleNewMeeting}
                  className="mt-3 text-sm font-medium text-purple-600 hover:text-purple-700"
                >
                  Write the first one
                </button>
              )}
            </div>
          ) : (
            meetings.map((meeting) => (
              <button
                key={meeting.id}
                onClick={() => handleSelect(meeting)}
                className={`w-full border-b border-slate-100 px-4 py-3 text-left transition-colors dark:border-slate-700/60 ${
                  meeting.id === selectedId
                    ? 'bg-purple-500/10 dark:bg-purple-500/15'
                    : 'hover:bg-slate-50 dark:hover:bg-slate-700/40'
                }`}
              >
                <div className="flex items-center gap-2">
                  {meeting.isUnlinked && <AlertCircle className="h-3 w-3 shrink-0 text-amber-500" />}
                  <span className="flex-1 truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {meeting.title}
                  </span>
                </div>
                <div className="mt-1 flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
                  <span className="shrink-0">{formatMeetingDate(meeting.meetingDate)}</span>
                  {scope.kind !== 'person' && meeting.directReportName && (
                    <span className="shrink-0 text-purple-600 dark:text-purple-400">
                      {meeting.directReportName}
                    </span>
                  )}
                  <span className="truncate">{meeting.snippet || 'Nothing written yet'}</span>
                </div>
              </button>
            ))
          )}
          {totalCount > meetings.length && (
            <p className="px-4 py-3 text-center text-xs text-slate-400">
              Showing the {meetings.length} most recent of {totalCount}. Narrow the list with search.
            </p>
          )}
        </div>
      </section>

      {/* Editor */}
      <section className="hidden flex-1 sm:flex">
        {selectedMeeting ? (
          <div className="h-full w-full">
            <MeetingEditor
              meeting={selectedMeeting}
              content={draft}
              saving={saving}
              startInEditMode={startInEditMode}
              onContentChange={handleContentChange}
              onTagsChange={handleTagsChange}
              onDateChange={handleDateChange}
              onDelete={handleDelete}
            />
          </div>
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center gap-3 text-center">
            <MessageSquare className="h-12 w-12 text-slate-300 dark:text-slate-600" />
            <p className="text-slate-500 dark:text-slate-400">Select a 1:1, or start a new one.</p>
            <button
              onClick={handleNewMeeting}
              className="flex items-center gap-2 rounded-lg bg-purple-500 px-4 py-2 text-sm font-medium text-white hover:bg-purple-600"
            >
              <Plus className="h-4 w-4" />
              New 1:1
            </button>
          </div>
        )}
      </section>
    </div>
  )
}
