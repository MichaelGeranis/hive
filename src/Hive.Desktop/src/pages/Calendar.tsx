import { useEffect, useState } from 'react'
import { ChevronLeft, ChevronRight, StickyNote, MessageSquare } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { meetingsApi, notesApi } from '../services/api'
import type { ManagerNote, OneOnOneMeeting } from '../types'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

type CalendarEvent = {
  id: string
  title: string
  /** The day the event belongs on. Both kinds of event land on their own date. */
  date: Date
  type: 'note' | 'one-on-one'
  color: string
  details?: string
}

const monthNames = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

const EVENT_COLORS = {
  note: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-l-4 border-amber-500',
  'one-on-one': 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 border-l-4 border-purple-500',
}

const EVENT_ICONS = {
  note: StickyNote,
  'one-on-one': MessageSquare,
}

const parseDateTag = (tags: string[]): Date | null => {
  const dateTag = tags.find((tag) => /^#\d{8}$/.test(tag.trim()))
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
  const { showError } = useToast()

  useEffect(() => {
    loadEvents()
  }, [currentDate])

  const loadEvents = async () => {
    try {
      setLoading(true)
      const [notesResponse, meetingsResponse] = await Promise.all([
        notesApi.getAll(1, 100, 'all', undefined, undefined, undefined, 'recent'),
        meetingsApi.getAll(1, 100)
      ])

      const calendarEvents: CalendarEvent[] = []

      // Date-tagged notes appear on the exact date in their #YYYYMMDD tag.
      notesResponse.items.forEach((note: ManagerNote) => {
        const date = parseDateTag(note.tagsList)
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

      setEvents(calendarEvents)
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

    return events.filter(event => {
      const eventDate = new Date(event.date)
      eventDate.setHours(0, 0, 0, 0)
      return eventDate.getTime() === targetDate.getTime()
    })
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

  const formatDate = (date: Date) => {
    return `${monthNames[date.getMonth()]} ${date.getDate()}`
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
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Daily Planner</h1>
          <p className="text-slate-500 dark:text-slate-400">Date-tagged notes and the 1:1s you have held</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={today}
            className="px-4 py-2 text-sm bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300"
          >
            Today
          </button>
          <button
            onClick={previousMonth}
            className="p-2 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
          >
            <ChevronLeft className="w-5 h-5 text-slate-700 dark:text-slate-300" />
          </button>
          <div className="px-4 py-2 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg">
            <span className="font-medium text-slate-900 dark:text-slate-100">
              {monthNames[currentDate.getMonth()]} {currentDate.getFullYear()}
            </span>
          </div>
          <button
            onClick={nextMonth}
            className="p-2 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
          >
            <ChevronRight className="w-5 h-5 text-slate-700 dark:text-slate-300" />
          </button>
        </div>
      </div>

      {/* Legend */}
      <div className="flex items-center gap-4 flex-wrap">
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-amber-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">Notes</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-purple-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">1:1s</span>
        </div>
      </div>

      {/* Calendar Grid */}
      <Card>
        <CardContent>
          <div className="grid grid-cols-5 gap-px bg-slate-200 dark:bg-slate-700 border border-slate-200 dark:border-slate-700 rounded-lg overflow-hidden">
            {/* Weekday Headers (Mon-Fri only) */}
            {['Mon', 'Tue', 'Wed', 'Thu', 'Fri'].map(day => (
              <div
                key={day}
                className="bg-slate-50 dark:bg-slate-800 p-2 text-center text-sm font-medium text-slate-700 dark:text-slate-300"
              >
                {day}
              </div>
            ))}

            {/* Padding Days */}
            {paddingDays.map(i => (
              <div key={`padding-${i}`} className="bg-white dark:bg-slate-900 min-h-[120px] p-2"></div>
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
                  className={`bg-white dark:bg-slate-900 min-h-[120px] p-2 cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors ${
                    isToday ? 'ring-2 ring-amber-500' : ''
                  }`}
                  onClick={() => setSelectedDate(new Date(currentDate.getFullYear(), currentDate.getMonth(), day))}
                >
                  <div className={`text-sm font-medium mb-1 ${isToday ? 'text-amber-600 dark:text-amber-400' : 'text-slate-700 dark:text-slate-300'}`}>
                    {day}
                  </div>
                  <div className="space-y-1">
                    {dayEvents.slice(0, 3).map(event => {
                      const Icon = EVENT_ICONS[event.type]
                      return (
                        <div
                          key={event.id}
                          className={`text-xs p-1 rounded ${event.color} truncate flex items-center gap-1`}
                          title={`${event.title} — ${formatDate(event.date)}`}
                        >
                          <Icon className="w-3 h-3 flex-shrink-0" />
                          <span className="truncate">{event.title}</span>
                        </div>
                      )
                    })}
                    {dayEvents.length > 3 && (
                      <div className="text-xs text-slate-500 dark:text-slate-400 pl-1">
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
            title={`Items for ${monthNames[selectedDate.getMonth()]} ${selectedDate.getDate()}, ${selectedDate.getFullYear()}`}
            action={
              <button
                onClick={() => setSelectedDate(null)}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
              >
                ×
              </button>
            }
          />
          <CardContent>
            {getEventsForDate(selectedDate.getDate()).length === 0 ? (
              <p className="text-slate-500 dark:text-slate-400 text-center py-4">No items scheduled for this day</p>
            ) : (
              <div className="space-y-2">
                {getEventsForDate(selectedDate.getDate()).map(event => {
                  const Icon = EVENT_ICONS[event.type]
                  return (
                    <div key={event.id} className={`p-3 rounded-lg ${event.color}`}>
                      <div className="flex items-start gap-2">
                        <Icon className="w-5 h-5 flex-shrink-0 mt-0.5" />
                        <div className="flex-1">
                          <h4 className="font-medium">{event.title}</h4>
                          <p className="text-xs opacity-70 mt-1">{formatDate(event.date)}</p>
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
