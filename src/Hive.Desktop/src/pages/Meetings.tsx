import { useEffect, useState } from 'react'
import { Plus, Calendar, Clock, MapPin, CheckCircle, XCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { meetingsApi } from '../services/api'
import { MeetingStatus } from '../types'
import type { OneOnOneMeeting } from '../types'

const statusColors: Record<MeetingStatus, string> = {
  [MeetingStatus.Scheduled]: 'bg-blue-100 text-blue-700',
  [MeetingStatus.Completed]: 'bg-green-100 text-green-700',
  [MeetingStatus.Cancelled]: 'bg-red-100 text-red-700',
  [MeetingStatus.Rescheduled]: 'bg-amber-100 text-amber-700',
}

export default function Meetings() {
  const [meetings, setMeetings] = useState<OneOnOneMeeting[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | 'upcoming' | MeetingStatus>('all')

  useEffect(() => {
    loadMeetings()
  }, [])

  const loadMeetings = async () => {
    try {
      setLoading(true)
      const data = await meetingsApi.getAll()
      setMeetings(data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const filteredMeetings = () => {
    if (filter === 'all') return meetings
    if (filter === 'upcoming') {
      return meetings.filter(
        m => m.status === MeetingStatus.Scheduled && new Date(m.scheduledDate) >= new Date()
      )
    }
    return meetings.filter(m => m.status === filter)
  }

  const handleComplete = async (id: string) => {
    try {
      await meetingsApi.complete(id)
      loadMeetings()
    } catch (err) {
      console.error(err)
    }
  }

  const handleCancel = async (id: string) => {
    try {
      await meetingsApi.cancel(id)
      loadMeetings()
    } catch (err) {
      console.error(err)
    }
  }

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr)
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const isUpcoming = (meeting: OneOnOneMeeting) => {
    return meeting.status === MeetingStatus.Scheduled && new Date(meeting.scheduledDate) >= new Date()
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  const upcomingMeetings = meetings.filter(isUpcoming).slice(0, 5)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">1:1 Meetings</h1>
          <p className="text-slate-500 mt-1">Schedule and track your one-on-ones</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors">
          <Plus className="w-5 h-5" />
          Schedule Meeting
        </button>
      </div>

      {/* Upcoming Meetings */}
      {upcomingMeetings.length > 0 && (
        <Card>
          <CardHeader title="Upcoming Meetings" subtitle="Next 5 scheduled" />
          <CardContent className="divide-y">
            {upcomingMeetings.map((meeting) => (
              <div key={meeting.id} className="flex items-center justify-between py-3 first:pt-0 last:pb-0">
                <div className="flex items-center gap-4">
                  <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center text-blue-700 font-medium">
                    {meeting.directReportName.split(' ').map(n => n[0]).join('')}
                  </div>
                  <div>
                    <p className="font-medium text-slate-900">{meeting.directReportName}</p>
                    <div className="flex items-center gap-3 text-sm text-slate-500">
                      <span className="flex items-center gap-1">
                        <Calendar className="w-4 h-4" />
                        {formatDate(meeting.scheduledDate)}
                      </span>
                      <span className="flex items-center gap-1">
                        <Clock className="w-4 h-4" />
                        {meeting.durationMinutes} min
                      </span>
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => handleComplete(meeting.id)}
                    className="p-2 text-green-600 hover:bg-green-50 rounded-lg"
                    title="Mark as completed"
                  >
                    <CheckCircle className="w-5 h-5" />
                  </button>
                  <button
                    onClick={() => handleCancel(meeting.id)}
                    className="p-2 text-red-600 hover:bg-red-50 rounded-lg"
                    title="Cancel"
                  >
                    <XCircle className="w-5 h-5" />
                  </button>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      {/* Filters */}
      <div className="flex gap-2">
        {[
          { value: 'all', label: 'All' },
          { value: 'upcoming', label: 'Upcoming' },
          { value: MeetingStatus.Completed, label: 'Completed' },
          { value: MeetingStatus.Cancelled, label: 'Cancelled' },
        ].map((f) => (
          <button
            key={f.value}
            onClick={() => setFilter(f.value as any)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
              filter === f.value
                ? 'bg-amber-500 text-white'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            {f.label}
          </button>
        ))}
      </div>

      {/* All Meetings */}
      <div className="space-y-3">
        {filteredMeetings().length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-slate-500">No meetings found</p>
            </CardContent>
          </Card>
        ) : (
          filteredMeetings().map((meeting) => (
            <Card key={meeting.id}>
              <CardContent>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-12 h-12 bg-amber-100 rounded-full flex items-center justify-center text-amber-700 font-semibold">
                      {meeting.directReportName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-900">{meeting.directReportName}</h3>
                      <div className="flex items-center gap-4 text-sm text-slate-500 mt-1">
                        <span className="flex items-center gap-1">
                          <Calendar className="w-4 h-4" />
                          {formatDate(meeting.scheduledDate)}
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="w-4 h-4" />
                          {meeting.durationMinutes} min
                        </span>
                        {meeting.location && (
                          <span className="flex items-center gap-1">
                            <MapPin className="w-4 h-4" />
                            {meeting.location}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColors[meeting.status]}`}>
                    {meeting.statusName}
                  </span>
                </div>
                {meeting.agenda && (
                  <div className="mt-4 pt-4 border-t">
                    <p className="text-sm font-medium text-slate-700">Agenda</p>
                    <p className="text-sm text-slate-500 mt-1">{meeting.agenda}</p>
                  </div>
                )}
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  )
}
