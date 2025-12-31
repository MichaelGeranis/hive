import { useState, useEffect, useCallback } from 'react'
import {
  StickyNote,
  Plus,
  Check,
  X,
  Clock,
  AlertTriangle,
  Trash2,
  Edit2,
  Filter
} from 'lucide-react'
import { notesApi } from '../services/api'
import type { ManagerNote, CreateManagerNoteDto, UpdateManagerNoteDto, NotePriority } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

const priorityLabels: Record<number, string> = {
  0: 'Low',
  1: 'Normal',
  2: 'High',
  3: 'Urgent'
}

const priorityColors: Record<number, string> = {
  0: 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300',
  1: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
  2: 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400',
  3: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
}

const priorityBorderColors: Record<number, string> = {
  0: 'border-l-slate-400',
  1: 'border-l-blue-500',
  2: 'border-l-orange-500',
  3: 'border-l-red-500'
}

type FilterType = 'all' | 'pending' | 'completed' | 'overdue'

export default function Notes() {
  const [notes, setNotes] = useState<ManagerNote[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingNote, setEditingNote] = useState<ManagerNote | null>(null)
  const [filter, setFilter] = useState<FilterType>('pending')
  const [formData, setFormData] = useState<CreateManagerNoteDto>({
    title: '',
    content: '',
    priority: 1 as NotePriority,
    dueDate: undefined
  })

  const resetForm = useCallback(() => {
    setFormData({
      title: '',
      content: '',
      priority: 1 as NotePriority,
      dueDate: undefined
    })
    setEditingNote(null)
  }, [])

  const closeModal = useCallback(() => {
    setShowForm(false)
    resetForm()
  }, [resetForm])

  useEscapeKey(closeModal, showForm)

  useEffect(() => {
    loadData()
  }, [filter])

  const loadData = async () => {
    try {
      setLoading(true)
      let data: ManagerNote[]
      switch (filter) {
        case 'pending':
          data = await notesApi.getPending()
          break
        case 'completed':
          data = await notesApi.getCompleted()
          break
        case 'overdue':
          data = await notesApi.getOverdue()
          break
        default:
          data = await notesApi.getAll()
      }
      setNotes(data)
    } catch (error) {
      console.error('Failed to load notes:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await notesApi.create(formData)
      closeModal()
      loadData()
    } catch (error) {
      console.error('Failed to create note:', error)
    }
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingNote) return
    try {
      const updateData: UpdateManagerNoteDto = {
        title: formData.title,
        content: formData.content,
        priority: formData.priority,
        dueDate: formData.dueDate
      }
      await notesApi.update(editingNote.id, updateData)
      closeModal()
      loadData()
    } catch (error) {
      console.error('Failed to update note:', error)
    }
  }

  const handleToggle = async (id: string) => {
    try {
      await notesApi.toggle(id)
      loadData()
    } catch (error) {
      console.error('Failed to toggle note:', error)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this note?')) return
    try {
      await notesApi.delete(id)
      loadData()
    } catch (error) {
      console.error('Failed to delete note:', error)
    }
  }

  const openEditForm = (note: ManagerNote) => {
    setEditingNote(note)
    setFormData({
      title: note.title,
      content: note.content,
      priority: note.priority,
      dueDate: note.dueDate ? note.dueDate.split('T')[0] : undefined
    })
    setShowForm(true)
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    })
  }

  const formatRelativeDate = (dateString: string) => {
    const date = new Date(dateString)
    const now = new Date()
    const diffDays = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

    if (diffDays < 0) return `${Math.abs(diffDays)} days overdue`
    if (diffDays === 0) return 'Due today'
    if (diffDays === 1) return 'Due tomorrow'
    if (diffDays <= 7) return `Due in ${diffDays} days`
    return formatDate(dateString)
  }

  const pendingCount = notes.filter(n => !n.isCompleted).length
  const overdueCount = notes.filter(n => n.isOverdue).length

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
        <div className="flex items-center gap-3">
          <StickyNote className="w-8 h-8 text-amber-500" />
          <div>
            <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Notes & TODOs</h1>
            <p className="text-sm text-slate-500 dark:text-slate-400">
              Personal notes and action items
            </p>
          </div>
        </div>
        <button
          onClick={() => {
            resetForm()
            setShowForm(true)
          }}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-4 h-4" />
          Add Note
        </button>
      </div>

      {/* Stats & Filters */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
            <Clock className="w-4 h-4" />
            <span>{pendingCount} pending</span>
          </div>
          {overdueCount > 0 && (
            <div className="flex items-center gap-2 text-sm text-red-600 dark:text-red-400">
              <AlertTriangle className="w-4 h-4" />
              <span>{overdueCount} overdue</span>
            </div>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as FilterType)}
            className="text-sm border border-slate-300 dark:border-slate-600 rounded-lg px-3 py-1.5 bg-white dark:bg-slate-800 text-slate-900 dark:text-white"
          >
            <option value="all">All Notes</option>
            <option value="pending">Pending</option>
            <option value="completed">Completed</option>
            <option value="overdue">Overdue</option>
          </select>
        </div>
      </div>

      {/* Notes List */}
      {notes.length === 0 ? (
        <div className="text-center py-12 bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700">
          <StickyNote className="w-12 h-12 text-slate-300 dark:text-slate-600 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-slate-900 dark:text-white mb-2">No notes found</h3>
          <p className="text-slate-500 dark:text-slate-400">
            {filter === 'all' ? 'Create your first note to get started' : `No ${filter} notes`}
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {notes.map((note) => (
            <div
              key={note.id}
              className={`bg-white dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700 p-4 border-l-4 ${priorityBorderColors[note.priority]} ${
                note.isCompleted ? 'opacity-60' : ''
              }`}
            >
              <div className="flex items-start gap-3">
                {/* Checkbox */}
                <button
                  onClick={() => handleToggle(note.id)}
                  className={`mt-0.5 w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                    note.isCompleted
                      ? 'bg-green-500 border-green-500 text-white'
                      : 'border-slate-300 dark:border-slate-600 hover:border-green-500'
                  }`}
                >
                  {note.isCompleted && <Check className="w-3 h-3" />}
                </button>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2">
                    <h3 className={`font-medium text-slate-900 dark:text-white ${note.isCompleted ? 'line-through' : ''}`}>
                      {note.title}
                    </h3>
                    <div className="flex items-center gap-2">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${priorityColors[note.priority]}`}>
                        {priorityLabels[note.priority]}
                      </span>
                      <button
                        onClick={() => openEditForm(note)}
                        className="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
                      >
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleDelete(note.id)}
                        className="p-1 text-slate-400 hover:text-red-500"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  {note.content && (
                    <p className={`mt-1 text-sm text-slate-600 dark:text-slate-400 ${note.isCompleted ? 'line-through' : ''}`}>
                      {note.content}
                    </p>
                  )}

                  <div className="mt-2 flex items-center gap-4 text-xs text-slate-500 dark:text-slate-400">
                    {note.dueDate && (
                      <span className={`flex items-center gap-1 ${note.isOverdue && !note.isCompleted ? 'text-red-500 font-medium' : ''}`}>
                        <Clock className="w-3 h-3" />
                        {formatRelativeDate(note.dueDate)}
                      </span>
                    )}
                    <span>Created {formatDate(note.createdAt)}</span>
                    {note.completedAt && (
                      <span className="text-green-600 dark:text-green-400">
                        Completed {formatDate(note.completedAt)}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create/Edit Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-slate-800 rounded-xl shadow-xl w-full max-w-lg mx-4 overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-700">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">
                {editingNote ? 'Edit Note' : 'New Note'}
              </h2>
              <button
                onClick={closeModal}
                className="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={editingNote ? handleUpdate : handleCreate} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Title *
                </label>
                <input
                  type="text"
                  required
                  value={formData.title}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                  placeholder="What needs to be done?"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Content
                </label>
                <textarea
                  value={formData.content}
                  onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                  rows={3}
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                  placeholder="Additional details..."
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Priority
                  </label>
                  <select
                    value={formData.priority}
                    onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) as NotePriority })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                  >
                    <option value={0}>Low</option>
                    <option value={1}>Normal</option>
                    <option value={2}>High</option>
                    <option value={3}>Urgent</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                    Due Date
                  </label>
                  <input
                    type="date"
                    value={formData.dueDate || ''}
                    onChange={(e) => setFormData({ ...formData, dueDate: e.target.value || undefined })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                  />
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="px-4 py-2 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
                >
                  {editingNote ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
