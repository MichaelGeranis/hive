import { useEffect, useRef, useState } from 'react'
import { formatDate, formatDateTime } from '../utils/dateUtils'
import { ChevronLeft, ChevronRight, StickyNote, MessageSquare, CalendarDays, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { SyncButton } from '../components/SyncButton'
import { googleCalendarApi, meetingsApi, notesApi } from '../services/api'
import type { GoogleCalendarEvent, GoogleCalendarEvents, ManagerNote, OneOnOneMeeting } from '../types'
import { useToast } from '../contexts/ToastContext'
import { getErrorMessage } from '../utils/apiErrors'
import { PageHeader } from '../components/PageHeader'
import { LoadingState } from '../components/PageState'

type CalendarEvent = {
  id: string
  title: string
  /** The day the event belongs on. Both kinds of event land on their own date. */
  date: Date
  type: 'note' | 'one-on-one' | 'google'
  color: string
  details?: string
  /** A timed Google Calendar event's start and end, as HH:mm. */
  time?: { start: string, end: string }
}

const monthNames = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

const EVENT_COLORS = {
  note: 'bg-amber-100 text-amber-700 border-l-4 border-amber-500',
  'one-on-one': 'bg-purple-100 text-purple-700 border-l-4 border-purple-500',
  google: 'bg-sky-100 text-sky-700 border-l-4 border-sky-500',
}

const EVENT_ICONS = {
  note: StickyNote,
  'one-on-one': MessageSquare,
  google: CalendarDays,
}

// yyyy-MM-dd in local time, the form the API takes a day in
const toIsoDay = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`

const toClockTime = (date: Date) =>
  `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`

// One entry per day the Google event covers: an all-day event can span several days
const toGoogleEntries = (event: GoogleCalendarEvent): CalendarEvent[] => {
  const base = {
    title: event.title,
    type: 'google' as const,
    color: EVENT_COLORS.google,
    details: event.location ?? undefined,
  }

  if (event.isAllDay && event.startDate) {
    const entries: CalendarEvent[] = []
    const day = new Date(`${event.startDate}T00:00:00`)
    const last = new Date(`${event.endDate ?? event.startDate}T00:00:00`)
    while (day <= last) {
      entries.push({ ...base, id: `google-${event.id}-${toIsoDay(day)}`, date: new Date(day) })
      day.setDate(day.getDate() + 1)
    }
    return entries
  }

  if (!event.startsAt) return []
  const start = new Date(event.startsAt)
  const end = event.endsAt ? new Date(event.endsAt) : start
  return [{
    ...base,
    id: `google-${event.id}`,
    date: new Date(start.getFullYear(), start.getMonth(), start.getDate()),
    time: { start: toClockTime(start), end: toClockTime(end) },
  }]
}

const parseDateTag = (tags: string[], content: string): Date | null => {
  const tagValues = [...tags, ...(content.match(/#\d{8}/g) ?? [])]
  const dateTag = tagValues.find((tag) => /^#\d{8}$/.test(tag.trim()))
  if (!dateTag) return null

  const value = dateTag.trim().slice(1)
  const year = Number(value.slice(0, 4))
  const month = Number(value.slice(4, 6))
  const day = Number(value.slice(6, 8))
  const date = new Date(year, month - 1, day)
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null
}

export default function Calendar() {
  const [currentDate, setCurrentDate] = useState(new Date())
  const [events, setEvents] = useState<CalendarEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedDate, setSelectedDate] = useState<Date | null>(null)
  // Google events are kept apart from notes and 1:1s, so a sync replaces only them
  const [googleEvents, setGoogleEvents] = useState<CalendarEvent[]>([])
  const [google, setGoogle] = useState<GoogleCalendarEvents | null>(null)
  const [googleError, setGoogleError] = useState<string | null>(null)
  const hasAutoNavigatedToNote = useRef(false)
  const { showError } = useToast()

  useEffect(() => {
    loadEvents()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- loads again when the month changes; loadEvents reads that month
  }, [currentDate])

  const visibleMonth = () => {
    const monthStart = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1)
    const monthEnd = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0)
    return [toIsoDay(monthStart), toIsoDay(monthEnd)] as const
  }

  const showGoogle = (response: GoogleCalendarEvents) => {
    setGoogle(response)
    setGoogleEvents(response.events.flatMap(toGoogleEntries))
  }

  // Asks Google only when the backend holds no sync from the last 30 minutes
  const syncGoogle = async () => {
    try {
      setGoogleError(null)
      showGoogle(await googleCalendarApi.sync(...visibleMonth()))
    } catch (error) {
      setGoogleError(getErrorMessage(error))
    }
  }

  const loadEvents = async () => {
    try {
      setLoading(true)
      // Google events come only from the last sync, never from Google itself, and a failure
      // is shown on its own without hiding the notes and 1:1s
      const [notesResponse, meetingsResponse, googleResponse] = await Promise.all([
        notesApi.getAll(1, 100),
        meetingsApi.getAll(1, 100),
        googleCalendarApi.getEvents(...visibleMonth()).catch((error) => {
          setGoogleError(getErrorMessage(error))
          return null
        })
      ])

      const calendarEvents: CalendarEvent[] = []

      // Date-tagged notes appear on the exact date in their #YYYYMMDD tag.
      notesResponse.items.forEach((note: ManagerNote) => {
        const date = parseDateTag(note.tagsList, note.content)
        if (!date) return

        calendarEvents.push({
          id: `note-${note.id}`,
          title: note.title,
          date,
          type: 'note',
          color: EVENT_COLORS.note,
          details: note.content ? note.content.substring(0, 100) : undefined
        })
      })

      // A 1:1 lands on the day it happened - it is a record, not a deadline.
      meetingsResponse.items.forEach((meeting: OneOnOneMeeting) => {
        calendarEvents.push({
          id: `meeting-${meeting.id}`,
          title: meeting.directReportName ? `1:1 · ${meeting.directReportName}` : '1:1',
          date: new Date(`${meeting.meetingDate}T00:00:00`),
          type: 'one-on-one',
          color: EVENT_COLORS['one-on-one'],
          details: meeting.snippet || meeting.title
        })
      })

      if (googleResponse) showGoogle(googleResponse)

      setEvents(calendarEvents)

      if (!hasAutoNavigatedToNote.current) {
        hasAutoNavigatedToNote.current = true
        const today = new Date()
        today.setHours(0, 0, 0, 0)
        const currentMonthHasNote = calendarEvents.some((event) =>
          event.type === 'note' &&
          event.date.getFullYear() === currentDate.getFullYear() &&
          event.date.getMonth() === currentDate.getMonth())
        const nextNote = calendarEvents
          .filter((event) => event.type === 'note' && event.date >= today)
          .sort((a, b) => a.date.getTime() - b.date.getTime())[0]

        if (!currentMonthHasNote && nextNote) {
          setCurrentDate(new Date(nextNote.date.getFullYear(), nextNote.date.getMonth(), 1))
        }
      }
    } catch (error) {
      console.error('Failed to load calendar events:', error)
      showError(getErrorMessage(error))
    } finally {
      setLoading(false)
    }
  }

  const getDaysInMonth = (date: Date) => {
    const year = date.getFullYear()
    const month = date.getMonth()
    return new Date(year, month + 1, 0).getDate()
  }

  // Check if a day is a weekend (Saturday=6, Sunday=0)
  const isWeekend = (day: number) => {
    const year = currentDate.getFullYear()
    const month = currentDate.getMonth()
    const dayOfWeek = new Date(year, month, day).getDay()
    return dayOfWeek === 0 || dayOfWeek === 6
  }

  const getEventsForDate = (day: number) => {
    const year = currentDate.getFullYear()
    const month = currentDate.getMonth()
    const targetDate = new Date(year, month, day)
    targetDate.setHours(0, 0, 0, 0)

    return [...events, ...googleEvents]
      .filter(event => {
        const eventDate = new Date(event.date)
        eventDate.setHours(0, 0, 0, 0)
        return eventDate.getTime() === targetDate.getTime()
      })
      // Whole-day items first, then timed events in the order they happen
      .sort((a, b) => (a.time?.start ?? '').localeCompare(b.time?.start ?? ''))
  }

  const previousMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1))
  }

  const nextMonth = () => {
    setCurrentDate(new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1))
  }

  const today = () => {
    setCurrentDate(new Date())
  }

  const daysInMonth = getDaysInMonth(currentDate)
  // Filter out weekend days
  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1).filter(day => !isWeekend(day))
  // Calculate padding for Monday-based week (only need padding for weekdays)
  const firstWeekdayOfMonth = (() => {
    const year = currentDate.getFullYear()
    const month = currentDate.getMonth()
    // Find the first weekday of the month
    for (let day = 1; day <= 7; day++) {
      const dayOfWeek = new Date(year, month, day).getDay()
      if (dayOfWeek !== 0 && dayOfWeek !== 6) {
        // Return Monday-based index (Mon=0, Tue=1, ..., Fri=4)
        return dayOfWeek - 1
      }
    }
    return 0
  })()
  const paddingDays = Array.from({ length: firstWeekdayOfMonth }, (_, i) => i)

  const todayDate = new Date()
  todayDate.setHours(0, 0, 0, 0)

  if (loading) {
    return <LoadingState fill />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="Daily Planner"
        subtitle={`Date-tagged notes, the 1:1s you have held${google?.isConfigured ? ' and your Google Calendar' : ''}`}
        actions={
          <>
            {google?.isConfigured && (
              <SyncButton
                onSync={syncGoogle}
                title="Load your Google Calendar. A sync from the last 30 minutes is reused instead of asking Google again"
                label="Sync Google Calendar"
              />
            )}
            <button
              onClick={today}
              className="px-4 py-2 text-sm bg-white border border-slate-300 rounded-lg hover:bg-slate-50 text-slate-700"
            >
              Today
            </button>
            <button
              onClick={previousMonth}
              className="p-2 bg-white border border-slate-300 rounded-lg hover:bg-slate-50"
            >
              <ChevronLeft className="w-5 h-5 text-slate-700" />
            </button>
            <div className="px-4 py-2 bg-white border border-slate-300 rounded-lg">
              <span className="font-medium text-slate-900">
                {monthNames[currentDate.getMonth()]} {currentDate.getFullYear()}
              </span>
            </div>
            <button
              onClick={nextMonth}
              className="p-2 bg-white border border-slate-300 rounded-lg hover:bg-slate-50"
            >
              <ChevronRight className="w-5 h-5 text-slate-700" />
            </button>
          </>
        }
      />

      {/* Legend */}
      <div className="flex items-center gap-4 flex-wrap">
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded-sm bg-amber-500"></div>
          <span className="text-sm text-slate-600">Notes</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded-sm bg-purple-500"></div>
          <span className="text-sm text-slate-600">1:1s</span>
        </div>
        {google?.isConfigured && (
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 rounded-sm bg-sky-500"></div>
            <span className="text-sm text-slate-600">Google Calendar</span>
            <span className="text-xs text-slate-400">
              {google.isLoaded && google.syncedAt
                ? `synced ${formatDateTime(google.syncedAt)}`
                : 'not synced. Press Sync Google Calendar to load it'}
            </span>
          </div>
        )}
      </div>

      {googleError && (
        <div className="flex items-start gap-2 p-3 bg-amber-50 border border-amber-200 rounded-lg text-amber-800 text-sm">
          <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
          <span>Google Calendar events could not be loaded. {googleError}</span>
        </div>
      )}

      {/* Calendar Grid */}
      <Card>
        <CardContent>
          <div className="grid grid-cols-5 gap-px bg-slate-200 border border-slate-200 rounded-lg overflow-hidden">
            {/* Weekday Headers (Mon-Fri only) */}
            {['Mon', 'Tue', 'Wed', 'Thu', 'Fri'].map(day => (
              <div
                key={day}
                className="bg-slate-50 p-2 text-center text-sm font-medium text-slate-700"
              >
                {day}
              </div>
            ))}

            {/* Padding Days */}
            {paddingDays.map(i => (
              <div key={`padding-${i}`} className="bg-white min-h-[120px] p-2"></div>
            ))}

            {/* Calendar Days */}
            {days.map(day => {
              const dayEvents = getEventsForDate(day)
              const isToday =
                day === todayDate.getDate() &&
                currentDate.getMonth() === todayDate.getMonth() &&
                currentDate.getFullYear() === todayDate.getFullYear()

              return (
                <div
                  key={day}
                  className={`bg-white min-h-[120px] p-2 cursor-pointer hover:bg-slate-50 transition-colors ${
                    isToday ? 'ring-2 ring-amber-500' : ''
                  }`}
                  onClick={() => setSelectedDate(new Date(currentDate.getFullYear(), currentDate.getMonth(), day))}
                >
                  <div className={`text-sm font-medium mb-1 ${isToday ? 'text-amber-600' : 'text-slate-700'}`}>
                    {day}
                  </div>
                  <div className="space-y-1">
                    {dayEvents.slice(0, 3).map(event => {
                      const Icon = EVENT_ICONS[event.type]
                      return (
                        <div
                          key={event.id}
                          className={`text-xs p-1 rounded-sm ${event.color} truncate flex items-center gap-1`}
                          title={`${event.title} — ${formatDate(event.date)}${event.time ? ` ${event.time.start}–${event.time.end}` : ''}`}
                        >
                          <Icon className="w-3 h-3 shrink-0" />
                          {event.time && <span className="shrink-0 font-medium">{event.time.start}</span>}
                          <span className="truncate">{event.title}</span>
                        </div>
                      )
                    })}
                    {dayEvents.length > 3 && (
                      <div className="text-xs text-slate-500 pl-1">
                        +{dayEvents.length - 3} more
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        </CardContent>
      </Card>

      {/* Selected Date Events */}
      {selectedDate && (
        <Card>
          <CardHeader
            title={`Items for ${formatDate(selectedDate)}`}
            action={
              <button
                onClick={() => setSelectedDate(null)}
                className="text-slate-400 hover:text-slate-600"
              >
                ×
              </button>
            }
          />
          <CardContent>
            {getEventsForDate(selectedDate.getDate()).length === 0 ? (
              <p className="text-slate-500 text-center py-4">No items scheduled for this day</p>
            ) : (
              <div className="space-y-2">
                {getEventsForDate(selectedDate.getDate()).map(event => {
                  const Icon = EVENT_ICONS[event.type]
                  return (
                    <div key={event.id} className={`p-3 rounded-lg ${event.color}`}>
                      <div className="flex items-start gap-2">
                        <Icon className="w-5 h-5 shrink-0 mt-0.5" />
                        <div className="flex-1">
                          <h4 className="font-medium">{event.title}</h4>
                          <p className="text-xs opacity-70 mt-1">
                            {formatDate(event.date)}{event.time ? ` · ${event.time.start}–${event.time.end}` : ''}
                          </p>
                          {event.details && (
                            <p className="text-sm opacity-80 mt-1">{event.details}</p>
                          )}
                        </div>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
