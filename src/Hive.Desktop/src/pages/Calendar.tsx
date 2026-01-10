import { useEffect, useState } from 'react'
import { ChevronLeft, ChevronRight, Users, MessageCircle, FolderKanban, CheckSquare } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { leavesApi, meetingsApi, projectsApi, tasksApi } from '../services/api'
import type { Leave, OneOnOneMeeting, Project, TeamTask } from '../types'

type CalendarEvent = {
  id: string
  title: string
  date: Date
  endDate?: Date
  type: 'leave' | 'meeting' | 'project-deadline' | 'task-deadline'
  color: string
  details?: string
}

const monthNames = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

const EVENT_COLORS = {
  leave: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 border-l-4 border-blue-500',
  meeting: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 border-l-4 border-purple-500',
  'project-deadline': 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-l-4 border-amber-500',
  'task-deadline': 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 border-l-4 border-green-500',
}

const EVENT_ICONS = {
  leave: Users,
  meeting: MessageCircle,
  'project-deadline': FolderKanban,
  'task-deadline': CheckSquare,
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
      const [leaves, meetings, projects, tasks] = await Promise.all([
        leavesApi.getAll(),
        meetingsApi.getAll(),
        projectsApi.getAll(),
        tasksApi.getAll(),
      ])

      const calendarEvents: CalendarEvent[] = []

      // Add leaves as events
      leaves.forEach((leave: Leave) => {
        calendarEvents.push({
          id: `leave-${leave.id}`,
          title: `${leave.directReportName} - ${leave.type} Leave`,
          date: new Date(leave.startDate),
          endDate: new Date(leave.endDate),
          type: 'leave',
          color: EVENT_COLORS.leave,
          details: `${leave.businessDaysCount} days`,
        })
      })

      // Add 1:1 meetings as events
      meetings.forEach((meeting: OneOnOneMeeting) => {
        calendarEvents.push({
          id: `meeting-${meeting.id}`,
          title: `1:1 with ${meeting.directReportName}`,
          date: new Date(meeting.meetingDate),
          type: 'meeting',
          color: EVENT_COLORS.meeting,
          details: meeting.location || undefined,
        })
      })

      // Add project deadlines as events
      projects.forEach((project: Project) => {
        if (project.targetEndDate) {
          calendarEvents.push({
            id: `project-${project.id}`,
            title: `${project.name} Deadline`,
            date: new Date(project.targetEndDate),
            type: 'project-deadline',
            color: EVENT_COLORS['project-deadline'],
            details: project.description || undefined,
          })
        }
      })

      // Add task deadlines as events
      tasks.items.forEach((task: TeamTask) => {
        if (task.dueDate) {
          calendarEvents.push({
            id: `task-${task.id}`,
            title: task.title,
            date: new Date(task.dueDate),
            type: 'task-deadline',
            color: EVENT_COLORS['task-deadline'],
            details: task.assigneeName ? `Assigned to: ${task.assigneeName}` : 'Unassigned',
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
      const eventDate = new Date(event.date)
      eventDate.setHours(0, 0, 0, 0)

      // For multi-day events (leaves), check if target date is within range
      if (event.endDate) {
        const endDate = new Date(event.endDate)
        endDate.setHours(0, 0, 0, 0)
        return targetDate >= eventDate && targetDate <= endDate
      }

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
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Calendar</h1>
          <p className="text-slate-500 dark:text-slate-400">Team schedule and deadlines</p>
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
          <div className="w-4 h-4 rounded bg-blue-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">Leaves</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-purple-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">1:1 Meetings</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-amber-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">Project Deadlines</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-green-500"></div>
          <span className="text-sm text-slate-600 dark:text-slate-400">Task Deadlines</span>
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
                          title={event.title}
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
            title={`Events for ${monthNames[selectedDate.getMonth()]} ${selectedDate.getDate()}, ${selectedDate.getFullYear()}`}
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
              <p className="text-slate-500 dark:text-slate-400 text-center py-4">No events on this day</p>
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
                          {event.details && (
                            <p className="text-sm opacity-80 mt-1">{event.details}</p>
                          )}
                          {event.endDate && (
                            <p className="text-xs opacity-70 mt-1">
                              {event.date.toLocaleDateString()} - {event.endDate.toLocaleDateString()}
                            </p>
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
