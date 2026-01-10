import { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  StickyNote,
  Plus,
  Check,
  X,
  Clock,
  Trash2,
  Edit2,
  Filter,
  Search,
  Tag,
  ChevronLeft,
  ChevronRight
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
  const [searchParams, setSearchParams] = useSearchParams()
  const initialSearch = searchParams.get('search') || ''

  const [allNotes, setAllNotes] = useState<ManagerNote[]>([])
  const [allTags, setAllTags] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingNote, setEditingNote] = useState<ManagerNote | null>(null)
  const [filter, setFilter] = useState<FilterType>(initialSearch ? 'all' : 'pending')
  const [searchTerm, setSearchTerm] = useState(initialSearch)
  const [selectedTag, setSelectedTag] = useState<string | null>(null)

  // Pagination state
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)

  // Clear search param from URL after initial load
  useEffect(() => {
    if (initialSearch) {
      setSearchParams({}, { replace: true })
    }
  }, [initialSearch, setSearchParams])
  const [formData, setFormData] = useState<CreateManagerNoteDto>({
    title: '',
    content: '',
    tags: '',
    priority: 1 as NotePriority,
    dueDate: undefined
  })

  const resetForm = useCallback(() => {
    setFormData({
      title: '',
      content: '',
      tags: '',
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
    loadTags()
  }, [])

  const loadTags = async () => {
    try {
      const tags = await notesApi.getTags()
      setAllTags(tags)
    } catch (error) {
      console.error('Failed to load tags:', error)
    }
  }

  const loadData = async (page = pageNumber) => {
    try {
      setLoading(true)
      const result = await notesApi.getAll(page, pageSize)
      setAllNotes(result.items)
      setTotalCount(result.totalCount)
      setTotalPages(result.totalPages)
      setPageNumber(result.pageNumber)
    } catch (error) {
      console.error('Failed to load notes:', error)
    } finally {
      setLoading(false)
    }
  }

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      loadData(newPage)
    }
  }

  const handlePageSizeChange = (newSize: number) => {
    setPageSize(newSize)
    setPageNumber(1)
    // Reload with new page size
    notesApi.getAll(1, newSize).then(result => {
      setAllNotes(result.items)
      setTotalCount(result.totalCount)
      setTotalPages(result.totalPages)
      setPageNumber(result.pageNumber)
    })
  }

  // Calculate status counts
  const statusCounts = useMemo(() => {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return {
      all: allNotes.length,
      pending: allNotes.filter(n => !n.isCompleted).length,
      completed: allNotes.filter(n => n.isCompleted).length,
      overdue: allNotes.filter(n => !n.isCompleted && n.dueDate && new Date(n.dueDate) < today).length,
    }
  }, [allNotes])

  // Filter notes based on current filter, search term, and selected tag
  const notes = useMemo(() => {
    let result = allNotes
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    // Apply status filter
    switch (filter) {
      case 'pending':
        result = result.filter(n => !n.isCompleted)
        break
      case 'completed':
        result = result.filter(n => n.isCompleted)
        break
      case 'overdue':
        result = result.filter(n => !n.isCompleted && n.dueDate && new Date(n.dueDate) < today)
        break
    }

    // Apply tag filter
    if (selectedTag) {
      result = result.filter(n => n.tags?.toLowerCase().includes(selectedTag.toLowerCase()))
    }

    // Apply search filter
    if (searchTerm.trim()) {
      const query = searchTerm.toLowerCase()
      result = result.filter(n =>
        n.title.toLowerCase().includes(query) ||
        n.content?.toLowerCase().includes(query) ||
        n.tags?.toLowerCase().includes(query)
      )
    }

    return result
  }, [allNotes, filter, selectedTag, searchTerm])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await notesApi.create(formData)
      closeModal()
      loadData()
      loadTags()
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
        tags: formData.tags,
        priority: formData.priority,
        dueDate: formData.dueDate
      }
      await notesApi.update(editingNote.id, updateData)
      closeModal()
      loadData()
      loadTags()
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
      loadTags()
    } catch (error) {
      console.error('Failed to delete note:', error)
    }
  }

  const openEditForm = (note: ManagerNote) => {
    setEditingNote(note)
    setFormData({
      title: note.title,
      content: note.content,
      tags: note.tags,
      priority: note.priority,
      dueDate: note.dueDate ? note.dueDate.split('T')[0] : undefined
    })
    setShowForm(true)
  }

  const handleTagClick = (tag: string) => {
    if (selectedTag === tag) {
      setSelectedTag(null)
    } else {
      setSelectedTag(tag)
    }
  }

  const clearFilters = () => {
    setSearchTerm('')
    setSelectedTag(null)
    setFilter('pending')
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

  if (loading && allNotes.length === 0) {
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
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Notes & TODOs</h1>
          <p className="text-slate-500 dark:text-slate-400">
            Keep personal notes & action items
          </p>
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

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search notes..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
          />
          {(searchTerm || selectedTag) && (
            <button
              onClick={clearFilters}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Tags */}
        {allTags.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Tag className="w-4 h-4 text-slate-400" />
            {allTags.map((tag) => (
              <button
                key={tag}
                onClick={() => handleTagClick(tag)}
                className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                  selectedTag === tag
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                {tag}
              </button>
            ))}
          </div>
        )}

        {/* Status Filter */}
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <div className="flex gap-2 flex-wrap">
            {[
              { value: 'all', label: `All (${statusCounts.all})`, count: statusCounts.all },
              { value: 'pending', label: `Pending (${statusCounts.pending})`, count: statusCounts.pending },
              { value: 'completed', label: `Completed (${statusCounts.completed})`, count: statusCounts.completed },
              { value: 'overdue', label: `Overdue (${statusCounts.overdue})`, count: statusCounts.overdue },
            ]
              .filter((f) => f.value === 'all' || f.count > 0)
              .map((f) => (
              <button
                key={f.value}
                onClick={() => setFilter(f.value as FilterType)}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  filter === f.value
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                {f.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Notes List */}
      {notes.length === 0 ? (
        <div className="text-center py-12 bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700">
          <StickyNote className="w-12 h-12 text-slate-300 dark:text-slate-600 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-slate-900 dark:text-white mb-2">No notes found</h3>
          <p className="text-slate-500 dark:text-slate-400">
            {searchTerm || selectedTag
              ? 'Try adjusting your search or filters'
              : filter === 'all'
              ? 'Create your first note to get started'
              : `No ${filter} notes`}
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

                  {/* Tags */}
                  {note.tagsList && note.tagsList.length > 0 && (
                    <div className="mt-2 flex items-center gap-1 flex-wrap">
                      {note.tagsList.map((tag) => (
                        <button
                          key={tag}
                          onClick={() => handleTagClick(tag)}
                          className={`px-2 py-0.5 rounded-full text-xs font-medium transition-colors ${
                            selectedTag === tag
                              ? 'bg-amber-500 text-white'
                              : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                          }`}
                        >
                          {tag}
                        </button>
                      ))}
                    </div>
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

      {/* Pagination Controls */}
      {totalPages > 0 && (
        <div className="flex items-center justify-between px-4 py-3 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg">
          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-600 dark:text-slate-400">
              Showing {((pageNumber - 1) * pageSize) + 1} - {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} notes
            </span>
            <div className="flex items-center gap-2">
              <span className="text-sm text-slate-600 dark:text-slate-400">Per page:</span>
              <select
                value={pageSize}
                onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                className="px-2 py-1 text-sm border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => handlePageChange(pageNumber - 1)}
              disabled={pageNumber <= 1}
              className="flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-700 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-4 h-4" />
              Previous
            </button>
            <span className="px-3 py-1.5 text-sm font-medium text-slate-900 dark:text-slate-100">
              Page {pageNumber} of {totalPages}
            </span>
            <button
              onClick={() => handlePageChange(pageNumber + 1)}
              disabled={pageNumber >= totalPages}
              className="flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-700 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
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

              <div>
                <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                  Tags
                </label>
                <input
                  type="text"
                  value={formData.tags || ''}
                  onChange={(e) => setFormData({ ...formData, tags: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                  placeholder="work, urgent, follow-up (comma separated)"
                />
                <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                  Separate tags with commas, spaces, or semicolons
                </p>
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
