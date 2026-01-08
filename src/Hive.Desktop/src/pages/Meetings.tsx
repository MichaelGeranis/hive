import { useEffect, useState, useCallback } from 'react'
import { Plus, Calendar, Clock, MapPin, MoreVertical, Edit, Trash2, ChevronDown, ChevronUp, StickyNote, Check } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { meetingsApi, directReportsApi, meetingNotesApi } from '../services/api'
import { NoteCategory, ActionItemStatus } from '../types'
import type { OneOnOneMeeting, DirectReport, MeetingNote, CreateMeetingNoteDto } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

const categoryColors: Record<NoteCategory, string> = {
  [NoteCategory.Discussion]: 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300',
  [NoteCategory.ActionItem]: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  [NoteCategory.Feedback]: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  [NoteCategory.CareerDevelopment]: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-400',
  [NoteCategory.Blocker]: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  [NoteCategory.Achievement]: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  [NoteCategory.Personal]: 'bg-pink-100 text-pink-700 dark:bg-pink-900/30 dark:text-pink-400',
  [NoteCategory.FollowUp]: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  [NoteCategory.Agenda]: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400',
}

const categoryLabels: Record<NoteCategory, string> = {
  [NoteCategory.Discussion]: 'Discussion',
  [NoteCategory.ActionItem]: 'Action Item',
  [NoteCategory.Feedback]: 'Feedback',
  [NoteCategory.CareerDevelopment]: 'Career',
  [NoteCategory.Blocker]: 'Blocker',
  [NoteCategory.Achievement]: 'Achievement',
  [NoteCategory.Personal]: 'Personal',
  [NoteCategory.FollowUp]: 'Follow Up',
  [NoteCategory.Agenda]: 'Agenda',
}

// All categories available for notes
const allCategories = [
  NoteCategory.Discussion,
  NoteCategory.ActionItem,
  NoteCategory.Feedback,
  NoteCategory.CareerDevelopment,
  NoteCategory.Blocker,
  NoteCategory.Achievement,
  NoteCategory.Personal,
  NoteCategory.FollowUp,
  NoteCategory.Agenda,
]

export default function Meetings() {
  const [meetings, setMeetings] = useState<OneOnOneMeeting[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [filterDirectReportId, setFilterDirectReportId] = useState<string>('all')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    directReportId: '',
    meetingDate: '',
    durationMinutes: '30',
    location: '',
    agenda: ''
  })

  // Notes state
  const [meetingNotes, setMeetingNotes] = useState<Record<string, MeetingNote[]>>({})
  const [showNoteForm, setShowNoteForm] = useState(false)
  const [expandedAgendas, setExpandedAgendas] = useState<Set<string>>(new Set())
  const [expandedNotes, setExpandedNotes] = useState<Set<string>>(new Set())
  const [noteFormData, setNoteFormData] = useState<CreateMeetingNoteDto>({
    meetingId: '',
    content: '',
    category: NoteCategory.Discussion,
    isPrivate: false
  })

  const resetForm = () => {
    setFormData({
      directReportId: '',
      meetingDate: '',
      durationMinutes: '30',
      location: '',
      agenda: ''
    })
  }

  const resetNoteForm = () => {
    setNoteFormData({
      meetingId: '',
      content: '',
      category: NoteCategory.Discussion,
      isPrivate: false
    })
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  const closeNoteModal = useCallback(() => {
    setShowNoteForm(false)
    resetNoteForm()
  }, [])

  useEscapeKey(closeModal, showForm)
  useEscapeKey(closeNoteModal, showNoteForm)

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
      // Sort by meeting date descending
      meetingsData.sort((a, b) => new Date(b.meetingDate).getTime() - new Date(a.meetingDate).getTime())
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
      // Sort by meeting date descending
      data.sort((a, b) => new Date(b.meetingDate).getTime() - new Date(a.meetingDate).getTime())
      setMeetings(data)
    } catch (err) {
      console.error(err)
    }
  }

  const loadMeetingNotes = async (meetingId: string) => {
    try {
      const notes = await meetingNotesApi.getByMeeting(meetingId)
      setMeetingNotes(prev => ({ ...prev, [meetingId]: notes }))
    } catch (err) {
      console.error(err)
    }
  }

  const toggleNotesExpand = async (meetingId: string) => {
    setExpandedNotes(prev => {
      const newSet = new Set(prev)
      if (newSet.has(meetingId)) {
        newSet.delete(meetingId)
      } else {
        newSet.add(meetingId)
        // Load notes if not already loaded
        if (!meetingNotes[meetingId]) {
          loadMeetingNotes(meetingId)
        }
      }
      return newSet
    })
  }

  const filteredMeetings = () => {
    if (filterDirectReportId === 'all') return meetings
    return meetings.filter(m => m.directReportId === filterDirectReportId)
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
      meetingDate: meeting.meetingDate.slice(0, 16),
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

  const handleCreateNote = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await meetingNotesApi.create(noteFormData)
      await loadMeetingNotes(noteFormData.meetingId)
      closeNoteModal()
    } catch (err) {
      console.error(err)
    }
  }

  const handleCompleteAction = async (noteId: string, meetingId: string) => {
    try {
      await meetingNotesApi.completeAction(noteId)
      await loadMeetingNotes(meetingId)
    } catch (err) {
      console.error(err)
    }
  }

  const handleDeleteNote = async (noteId: string, meetingId: string) => {
    if (confirm('Are you sure you want to delete this note?')) {
      try {
        await meetingNotesApi.delete(noteId)
        await loadMeetingNotes(meetingId)
      } catch (err) {
        console.error(err)
      }
    }
  }

  const openNoteForm = (meetingId: string) => {
    setNoteFormData({
      meetingId,
      content: '',
      category: NoteCategory.Discussion,
      isPrivate: false
    })
    setShowNoteForm(true)
  }

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr)
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const isPastMeeting = (meeting: OneOnOneMeeting) => {
    return new Date(meeting.meetingDate) < new Date()
  }

  const toggleAgendaExpand = (meetingId: string) => {
    setExpandedAgendas(prev => {
      const newSet = new Set(prev)
      if (newSet.has(meetingId)) {
        newSet.delete(meetingId)
      } else {
        newSet.add(meetingId)
      }
      return newSet
    })
  }

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
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">1:1 Meetings</h1>
          <p className="text-slate-500 dark:text-slate-400">Keep one-on-one agenda & meeting notes</p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          Add Meeting
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingId ? 'Edit Meeting' : 'Add Meeting'} />
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Team Member</label>
                  <select
                    value={formData.directReportId}
                    onChange={(e) => setFormData({ ...formData, directReportId: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
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
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Date & Time</label>
                    <input
                      type="datetime-local"
                      value={formData.meetingDate}
                      onChange={(e) => setFormData({ ...formData, meetingDate: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Duration (minutes)</label>
                    <select
                      value={formData.durationMinutes}
                      onChange={(e) => setFormData({ ...formData, durationMinutes: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                    >
                      <option value="15">15 minutes</option>
                      <option value="30">30 minutes</option>
                      <option value="45">45 minutes</option>
                      <option value="60">60 minutes</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Location</label>
                  <input
                    type="text"
                    value={formData.location}
                    onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                    placeholder="e.g., Conference Room A, Zoom, etc."
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Agenda</label>
                  <textarea
                    value={formData.agenda}
                    onChange={(e) => setFormData({ ...formData, agenda: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                    rows={3}
                    placeholder="Topics to discuss..."
                  />
                </div>
                <div className="flex gap-3 pt-4">
                  <button
                    type="button"
                    onClick={closeModal}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    {editingId ? 'Update' : 'Add'}
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Note Form Modal */}
      {showNoteForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title="Add Note" />
            <CardContent>
              <form onSubmit={handleCreateNote} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Category</label>
                  <select
                    value={noteFormData.category}
                    onChange={(e) => setNoteFormData({ ...noteFormData, category: parseInt(e.target.value) as NoteCategory })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                  >
                    {allCategories.map((cat) => (
                      <option key={cat} value={cat}>{categoryLabels[cat]}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Content</label>
                  <textarea
                    value={noteFormData.content}
                    onChange={(e) => setNoteFormData({ ...noteFormData, content: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                    rows={3}
                    required
                    placeholder="Note content..."
                  />
                </div>
                {noteFormData.category === NoteCategory.ActionItem && (
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Assignee</label>
                      <input
                        type="text"
                        value={noteFormData.actionAssignee || ''}
                        onChange={(e) => setNoteFormData({ ...noteFormData, actionAssignee: e.target.value })}
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                        placeholder="Who's responsible?"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Due Date</label>
                      <input
                        type="date"
                        value={noteFormData.actionDueDate || ''}
                        onChange={(e) => setNoteFormData({ ...noteFormData, actionDueDate: e.target.value })}
                        className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                      />
                    </div>
                  </div>
                )}
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isPrivate"
                    checked={noteFormData.isPrivate}
                    onChange={(e) => setNoteFormData({ ...noteFormData, isPrivate: e.target.checked })}
                    className="w-4 h-4 rounded border-slate-300"
                  />
                  <label htmlFor="isPrivate" className="text-sm text-slate-700 dark:text-slate-300">
                    Private note (only visible to you)
                  </label>
                </div>
                <div className="flex gap-3 pt-4">
                  <button
                    type="button"
                    onClick={closeNoteModal}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    Add Note
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Filter by Direct Report */}
      <div className="flex gap-2 flex-wrap">
        <button
          onClick={() => setFilterDirectReportId('all')}
          className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
            filterDirectReportId === 'all'
              ? 'bg-amber-500 text-white'
              : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'
          }`}
        >
          All
        </button>
        {directReports.map((dr) => (
          <button
            key={dr.id}
            onClick={() => setFilterDirectReportId(dr.id)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
              filterDirectReportId === dr.id
                ? 'bg-amber-500 text-white'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'
            }`}
          >
            {dr.fullName}
          </button>
        ))}
      </div>

      {/* All Meetings */}
      <div className="space-y-3">
        {filteredMeetings().length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-slate-500 dark:text-slate-400">No meetings found</p>
            </CardContent>
          </Card>
        ) : (
          filteredMeetings().map((meeting) => (
            <Card key={meeting.id}>
              <CardContent>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-12 h-12 bg-amber-100 dark:bg-amber-900/30 rounded-full flex items-center justify-center text-amber-700 dark:text-amber-400 font-semibold">
                      {meeting.directReportName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-900 dark:text-white">{meeting.directReportName}</h3>
                      <div className="flex items-center gap-4 text-sm text-slate-500 dark:text-slate-400 mt-1">
                        <span className="flex items-center gap-1">
                          <Calendar className="w-4 h-4" />
                          {formatDate(meeting.meetingDate)}
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
                    {/* Show if meeting is past or upcoming */}
                    <span className={`px-3 py-1 rounded-full text-sm font-medium ${
                      isPastMeeting(meeting)
                        ? 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300'
                        : 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
                    }`}>
                      {isPastMeeting(meeting) ? 'Past' : 'Upcoming'}
                    </span>
                    {/* Note count indicator */}
                    {meeting.noteCount > 0 && (
                      <span className="flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
                        <StickyNote className="w-3 h-3" />
                        {meeting.noteCount}
                      </span>
                    )}
                    <div className="relative group">
                      <button className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded">
                        <MoreVertical className="w-5 h-5 text-slate-400" />
                      </button>
                      <div className="absolute right-0 mt-1 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                        <button
                          onClick={() => handleEdit(meeting)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700"
                        >
                          <Edit className="w-4 h-4" />
                          Edit
                        </button>
                        <button
                          onClick={() => handleDelete(meeting.id)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50 dark:hover:bg-red-900/30"
                        >
                          <Trash2 className="w-4 h-4" />
                          Delete
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Agenda */}
                {meeting.agenda && (
                  <div className="mt-4 pt-4 border-t dark:border-slate-700">
                    <button
                      onClick={() => toggleAgendaExpand(meeting.id)}
                      className="flex items-center gap-2 text-sm font-medium text-slate-700 dark:text-slate-300 hover:text-slate-900 dark:hover:text-slate-100 w-full text-left"
                    >
                      {expandedAgendas.has(meeting.id) ? (
                        <ChevronUp className="w-4 h-4" />
                      ) : (
                        <ChevronDown className="w-4 h-4" />
                      )}
                      Agenda
                    </button>
                    {expandedAgendas.has(meeting.id) && (
                      <p className="text-sm text-slate-500 dark:text-slate-400 mt-2 whitespace-pre-wrap ml-6">{meeting.agenda}</p>
                    )}
                  </div>
                )}

                {/* Notes Section */}
                <div className="mt-4 pt-4 border-t dark:border-slate-700">
                  <div className="flex items-center justify-between">
                    <button
                      onClick={() => toggleNotesExpand(meeting.id)}
                      className="flex items-center gap-2 text-sm font-medium text-slate-700 dark:text-slate-300 hover:text-slate-900 dark:hover:text-slate-100"
                    >
                      {expandedNotes.has(meeting.id) ? (
                        <ChevronUp className="w-4 h-4" />
                      ) : (
                        <ChevronDown className="w-4 h-4" />
                      )}
                      <StickyNote className="w-4 h-4" />
                      Notes
                      {meeting.noteCount > 0 && (
                        <span className="text-xs text-slate-500 dark:text-slate-400">({meeting.noteCount})</span>
                      )}
                    </button>
                    <button
                      onClick={() => openNoteForm(meeting.id)}
                      className="text-sm text-amber-600 hover:text-amber-700 flex items-center gap-1"
                    >
                      <Plus className="w-4 h-4" />
                      Add
                    </button>
                  </div>

                  {expandedNotes.has(meeting.id) && (
                    <div className="mt-3 ml-6">
                      {(() => {
                        const notes = meetingNotes[meeting.id] || []
                        return notes.length > 0 ? (
                          <div className="space-y-2">
                            {notes.map((note) => (
                              <div
                                key={note.id}
                                className={`p-3 rounded-lg bg-slate-50 dark:bg-slate-800/50 ${
                                  note.category === NoteCategory.ActionItem && note.isOverdue ? 'border-l-4 border-red-500' : ''
                                }`}
                              >
                                <div className="flex items-start justify-between gap-2">
                                  <div className="flex-1">
                                    <div className="flex items-center gap-2 mb-1">
                                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${categoryColors[note.category]}`}>
                                        {note.categoryName}
                                      </span>
                                      {note.isPrivate && (
                                        <span className="text-xs px-2 py-0.5 rounded-full bg-slate-200 dark:bg-slate-600 text-slate-600 dark:text-slate-300">
                                          Private
                                        </span>
                                      )}
                                      {note.actionStatusName && (
                                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                                          note.actionStatus === ActionItemStatus.Completed
                                            ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                                            : note.actionStatus === ActionItemStatus.InProgress
                                            ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
                                            : 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300'
                                        }`}>
                                          {note.actionStatusName}
                                        </span>
                                      )}
                                    </div>
                                    <p className="text-sm text-slate-700 dark:text-slate-300">{note.content}</p>
                                    {note.actionDueDate && (
                                      <p className={`text-xs mt-1 ${note.isOverdue ? 'text-red-500 font-medium' : 'text-slate-500 dark:text-slate-400'}`}>
                                        Due: {new Date(note.actionDueDate).toLocaleDateString()}
                                        {note.actionAssignee && ` | Assigned: ${note.actionAssignee}`}
                                      </p>
                                    )}
                                  </div>
                                  <div className="flex items-center gap-1">
                                    {note.category === NoteCategory.ActionItem && note.actionStatus !== ActionItemStatus.Completed && (
                                      <button
                                        onClick={() => handleCompleteAction(note.id, meeting.id)}
                                        className="p-1 text-green-600 hover:bg-green-50 dark:hover:bg-green-900/30 rounded"
                                        title="Complete action"
                                      >
                                        <Check className="w-4 h-4" />
                                      </button>
                                    )}
                                    <button
                                      onClick={() => handleDeleteNote(note.id, meeting.id)}
                                      className="p-1 text-slate-400 hover:text-red-500 rounded"
                                      title="Delete note"
                                    >
                                      <Trash2 className="w-4 h-4" />
                                    </button>
                                  </div>
                                </div>
                              </div>
                            ))}
                          </div>
                        ) : (
                          <p className="text-sm text-slate-500 dark:text-slate-400">
                            No notes yet. Click "Add" to capture key takeaways.
                          </p>
                        )
                      })()}
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  )
}
