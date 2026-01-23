import { useEffect, useState } from 'react'
import { ChevronLeft, ChevronRight, ClipboardList, CheckCircle2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { notesApi, meetingNotesApi } from '../services/api'
import type { ManagerNote, MeetingNote } from '../types'

type CalendarEvent = {
  id: string
  title: string
  displayDate: Date  // The date to show on calendar (1 day before due)
  dueDate: Date      // The actual due date
  type: 'todo' | 'action-item'
  color: string
  details?: string
  priority?: string
  isOverdue: boolean
}

const monthNames = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

const EVENT_COLORS = {
  todo: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-l-4 border-amber-500',
  'todo-overdue': 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 border-l-4 border-red-500',
  'action-item': 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 border-l-4 border-purple-500',
  'action-item-overdue': 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 border-l-4 border-red-500',
}

const EVENT_ICONS = {
  todo: ClipboardList,
  'action-item': CheckCircle2,
}

export default function Calendar() {
  const [currentDate, setCurrentDate] = useState(new Date())
  const [events, setEvents] = useState<CalendarEvent[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedDate, setSelectedDate] = useState<Date | null>(null)

  useEffect(() => {
    loadEvents()
  }, [currentDate])

  const loadEvents = async () => {
    try {
      setLoading(true)
      const [todosResponse, actionItems] = await Promise.all([
        notesApi.getPending(),  // Get incomplete TODOs
        meetingNotesApi.getOpenActionItems(),  // Get open action items
      ])

      const calendarEvents: CalendarEvent[] = []

      // Add TODOs with due dates (show 1 day before due)
      todosResponse.forEach((todo: ManagerNote) => {
        if (todo.dueDate) {
          const dueDate = new Date(todo.dueDate)
          const displayDate = new Date(dueDate)
          displayDate.setDate(displayDate.getDate() - 1)  // 1 day before due

          calendarEvents.push({
            id: `todo-${todo.id}`,
            title: todo.title,
            displayDate,
            dueDate,
            type: 'todo',
            color: todo.isOverdue ? EVENT_COLORS['todo-overdue'] : EVENT_COLORS.todo,
            details: todo.content ? todo.content.substring(0, 100) : undefined,
            priority: todo.priorityName,
            isOverdue: todo.isOverdue,
          })
        }
      })

      // Add Action Items with due dates (show 1 day before due)
      actionItems.forEach((actionItem: MeetingNote) => {
        if (actionItem.actionDueDate) {
          const dueDate = new Date(actionItem.actionDueDate)
          const displayDate = new Date(dueDate)
          displayDate.setDate(displayDate.getDate() - 1)  // 1 day before due

          calendarEvents.push({
            id: `action-${actionItem.id}`,
            title: actionItem.content.substring(0, 50) + (actionItem.content.length > 50 ? '...' : ''),
            displayDate,
            dueDate,
            type: 'action-item',
            color: actionItem.isOverdue ? EVENT_COLORS['action-item-overdue'] : EVENT_COLORS['action-item'],
            details: `1:1 with ${actionItem.directReportName}${actionItem.actionAssignee ? ` • Assigned: ${actionItem.actionAssignee}` : ''}`,
            isOverdue: actionItem.isOverdue,
          })
        }
      })

      setEvents(calendarEvents)
    } catch (error) {
      console.error('Failed to load calendar events:', error)
    } finally {
      setLoading(false)
    }
  }

  const getDaysInMonth = (date: Date) => {
    const year = date.getFullYear()
    const month = date.getMonth()
    return new Date(year, month + 1, 0).getDate()
  }

  const getFirstDayOfMonth = (date: Date) => {
    const year = date.getFullYear()
    const month = date.getMonth()
    return new Date(year, month, 1).getDay()
  }

  const getEventsForDate = (day: number) => {
    const year = currentDate.getFullYear()
    const month = currentDate.getMonth()
    const targetDate = new Date(year, month, day)
    targetDate.setHours(0, 0, 0, 0)

    return events.filter(event => {
      const eventDate = new Date(event.displayDate)
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
  const firstDay = getFirstDayOfMonth(currentDate)
  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1)
  const paddingDays = Array.from({ length: firstDay }, (_, i) => i)

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
          <p className="text-slate-500 dark:text-slate-400">TODOs and Action Items (shown 1 day before due)</p>
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
          <span className="text-sm text-slate-600 dark:text-slate-400">TODOs</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-purple-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">1:1 Action Items</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-red-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">Overdue</span>
        </div>
      </div>

      {/* Calendar Grid */}
      <Card>
        <CardContent>
          <div className="grid grid-cols-7 gap-px bg-slate-200 dark:bg-slate-700 border border-slate-200 dark:border-slate-700 rounded-lg overflow-hidden">
            {/* Weekday Headers */}
            {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => (
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
                          title={`${event.title} (Due: ${formatDate(event.dueDate)})`}
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
            title={`Tasks for ${monthNames[selectedDate.getMonth()]} ${selectedDate.getDate()}, ${selectedDate.getFullYear()}`}
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
              <p className="text-slate-500 dark:text-slate-400 text-center py-4">No tasks scheduled for this day</p>
            ) : (
              <div className="space-y-2">
                {getEventsForDate(selectedDate.getDate()).map(event => {
                  const Icon = EVENT_ICONS[event.type]
                  return (
                    <div key={event.id} className={`p-3 rounded-lg ${event.color}`}>
                      <div className="flex items-start gap-2">
                        <Icon className="w-5 h-5 flex-shrink-0 mt-0.5" />
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <h4 className="font-medium">{event.title}</h4>
                            {event.priority && (
                              <span className="text-xs px-1.5 py-0.5 rounded bg-black/10 dark:bg-white/10">
                                {event.priority}
                              </span>
                            )}
                            {event.isOverdue && (
                              <span className="text-xs px-1.5 py-0.5 rounded bg-red-500 text-white">
                                Overdue
                              </span>
                            )}
                          </div>
                          <p className="text-xs opacity-70 mt-1">
                            Due: {formatDate(event.dueDate)}
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
