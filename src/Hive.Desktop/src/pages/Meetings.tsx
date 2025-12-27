import { useEffect, useState } from 'react'
import { Plus, Calendar, Clock, MapPin, CheckCircle, XCircle, MoreVertical, Edit, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { meetingsApi, directReportsApi } from '../services/api'
import { MeetingStatus } from '../types'
import type { OneOnOneMeeting, DirectReport } from '../types'

const statusColors: Record<MeetingStatus, string> = {
  [MeetingStatus.Scheduled]: 'bg-blue-100 text-blue-700',
  [MeetingStatus.Completed]: 'bg-green-100 text-green-700',
  [MeetingStatus.Cancelled]: 'bg-red-100 text-red-700',
  [MeetingStatus.Rescheduled]: 'bg-amber-100 text-amber-700',
}

export default function Meetings() {
  const [meetings, setMeetings] = useState<OneOnOneMeeting[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | 'upcoming' | MeetingStatus>('all')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    directReportId: '',
    scheduledDate: '',
    durationMinutes: '30',
    location: '',
    agenda: ''
  })

  const resetForm = () => {
    setFormData({
      directReportId: '',
      scheduledDate: '',
      durationMinutes: '30',
      location: '',
      agenda: ''
    })
  }

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [meetingsData, drData] = await Promise.all([
        meetingsApi.getAll(),
        directReportsApi.getAll()
      ])
      setMeetings(meetingsData)
      setDirectReports(drData)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const loadMeetings = async () => {
    try {
      const data = await meetingsApi.getAll()
      setMeetings(data)
    } catch (err) {
      console.error(err)
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const meetingData = {
        ...formData,
        durationMinutes: parseInt(formData.durationMinutes)
      }
      if (editingId) {
        await meetingsApi.update(editingId, meetingData)
      } else {
        await meetingsApi.create(meetingData)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadMeetings()
    } catch (err) {
      console.error(err)
    }
  }

  const handleEdit = (meeting: OneOnOneMeeting) => {
    setFormData({
      directReportId: meeting.directReportId,
      scheduledDate: meeting.scheduledDate.slice(0, 16), // Format: YYYY-MM-DDTHH:MM
      durationMinutes: meeting.durationMinutes.toString(),
      location: meeting.location || '',
      agenda: meeting.agenda || ''
    })
    setEditingId(meeting.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this meeting?')) {
      try {
        await meetingsApi.delete(id)
        loadMeetings()
      } catch (err) {
        console.error(err)
      }
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
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          Schedule Meeting
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingId ? 'Edit Meeting' : 'Schedule Meeting'} />
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Team Member</label>
                  <select
                    value={formData.directReportId}
                    onChange={(e) => setFormData({ ...formData, directReportId: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    required
                  >
                    <option value="">Select team member</option>
                    {directReports.map(dr => (
                      <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                    ))}
                  </select>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Date & Time</label>
                    <input
                      type="datetime-local"
                      value={formData.scheduledDate}
                      onChange={(e) => setFormData({ ...formData, scheduledDate: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Duration (minutes)</label>
                    <select
                      value={formData.durationMinutes}
                      onChange={(e) => setFormData({ ...formData, durationMinutes: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    >
                      <option value="15">15 minutes</option>
                      <option value="30">30 minutes</option>
                      <option value="45">45 minutes</option>
                      <option value="60">60 minutes</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Location</label>
                  <input
                    type="text"
                    value={formData.location}
                    onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    placeholder="e.g., Conference Room A, Zoom, etc."
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Agenda</label>
                  <textarea
                    value={formData.agenda}
                    onChange={(e) => setFormData({ ...formData, agenda: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    rows={3}
                    placeholder="Topics to discuss..."
                  />
                </div>
                <div className="flex gap-3 pt-4">
                  <button
                    type="button"
                    onClick={() => { setShowForm(false); setEditingId(null); resetForm() }}
                    className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    {editingId ? 'Update' : 'Schedule'}
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

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
                  <div className="flex items-center gap-2">
                    <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColors[meeting.status]}`}>
                      {meeting.statusName}
                    </span>
                    <div className="relative group">
                      <button className="p-1 hover:bg-slate-100 rounded">
                        <MoreVertical className="w-5 h-5 text-slate-400" />
                      </button>
                      <div className="absolute right-0 mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                        <button
                          onClick={() => handleEdit(meeting)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                        >
                          <Edit className="w-4 h-4" />
                          Edit
                        </button>
                        <button
                          onClick={() => handleDelete(meeting.id)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                        >
                          <Trash2 className="w-4 h-4" />
                          Delete
                        </button>
                      </div>
                    </div>
                  </div>
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
