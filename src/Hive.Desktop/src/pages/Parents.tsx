import { useEffect, useState, useCallback, useMemo } from 'react'
import { Plus, Layers, Search, Tag, Edit, Trash2, MoreVertical, CheckCircle, Clock, X } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { parentsApi } from '../services/api'
import type { Parent } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

export default function Parents() {
  const { showError } = useToast()
  const [parents, setParents] = useState<Parent[]>([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    name: '',
    labels: ''
  })

  const resetForm = () => {
    setFormData({ name: '', labels: '' })
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  useEscapeKey(closeModal, showForm)

  useEffect(() => {
    loadParents()
  }, [])

  const loadParents = async () => {
    try {
      setLoading(true)
      const data = await parentsApi.getAll()
      setParents(data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const [selectedLabel, setSelectedLabel] = useState<string | null>(null)

  // Get all unique labels from parents
  const allLabels = useMemo(() => {
    const labelSet = new Set<string>()
    parents.forEach(p => {
      if (p.labels) {
        p.labels.split(',').forEach(label => {
          const trimmed = label.trim()
          if (trimmed) labelSet.add(trimmed)
        })
      }
    })
    return Array.from(labelSet).sort()
  }, [parents])

  const clearFilters = () => {
    setSearchQuery('')
    setSelectedLabel(null)
  }

  const filteredParents = () => {
    let result = parents

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(p =>
        p.name.toLowerCase().includes(query) ||
        p.labels?.toLowerCase().includes(query)
      )
    }

    // Apply label filter
    if (selectedLabel) {
      result = result.filter(p =>
        p.labels?.split(',').some(label => label.trim().toLowerCase() === selectedLabel.toLowerCase())
      )
    }

    return result
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const payload = {
        name: formData.name,
        labels: formData.labels || undefined
      }
      if (editingId) {
        await parentsApi.update(editingId, payload)
      } else {
        await parentsApi.create(payload)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadParents()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  const handleEdit = (parent: Parent) => {
    setFormData({
      name: parent.name,
      labels: parent.labels || ''
    })
    setEditingId(parent.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this parent?')) {
      try {
        await parentsApi.delete(id)
        loadParents()
      } catch (err) {
        console.error(err)
        showError(getErrorMessage(err))
      }
    }
  }

  const formatTime = (minutes?: number) => {
    if (!minutes) return '0h'
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    if (hours === 0) return `${mins}m`
    if (mins === 0) return `${hours}h`
    return `${hours}h ${mins}m`
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
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Parents</h1>
          <p className="text-slate-500 dark:text-slate-400">Track task groups</p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          New Parent
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingId ? 'Edit Parent' : 'Create Parent'} />
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Name</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Labels</label>
                  <input
                    type="text"
                    value={formData.labels}
                    onChange={(e) => setFormData({ ...formData, labels: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    placeholder="frontend, backend, urgent (comma-separated)"
                  />
                  <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    Labels link this parent to projects with matching labels
                  </p>
                </div>
                <div className="flex gap-3 pt-4">
                  <button
                    type="button"
                    onClick={() => { setShowForm(false); setEditingId(null); resetForm() }}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                  >
                    {editingId ? 'Update' : 'Create'}
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search parents..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
          />
          {(searchQuery || selectedLabel) && (
            <button
              onClick={clearFilters}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              <X className="w-4 h-4" />
            </button>
          )}
        </div>

        {/* Labels/Tags */}
        {allLabels.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Tag className="w-4 h-4 text-slate-400" />
            {allLabels.map((label) => (
              <button
                key={label}
                onClick={() => setSelectedLabel(selectedLabel === label ? null : label)}
                className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                  selectedLabel === label
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Parents Grid */}
      {filteredParents().length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500 dark:text-slate-400">
              {parents.length === 0
                ? 'No parents found. Parents are automatically created during CSV import when tasks have a "Parent" column.'
                : 'No parents match your search'}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredParents().map((parent) => (
            <Card key={parent.id}>
              <CardContent>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3 flex-1">
                    <div className="p-2 bg-indigo-100 dark:bg-indigo-900/30 rounded-lg">
                      <Layers className="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold text-slate-900 dark:text-slate-100">{parent.name}</h3>
                        {parent.totalTasks === 0 && (
                          <span className="px-2 py-0.5 text-xs font-medium bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 rounded-full">
                            Epic
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="relative group">
                    <button className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded">
                      <MoreVertical className="w-5 h-5 text-slate-400" />
                    </button>
                    <div className="absolute right-0 mt-1 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleEdit(parent)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700"
                      >
                        <Edit className="w-4 h-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(parent.id)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                      >
                        <Trash2 className="w-4 h-4" />
                        Delete
                      </button>
                    </div>
                  </div>
                </div>

                {parent.labels && (
                  <div className="flex items-center gap-2 mb-3">
                    <Tag className="w-4 h-4 text-slate-400" />
                    <div className="flex flex-wrap gap-1">
                      {parent.labels.split(',').map((label, idx) => (
                        <span
                          key={idx}
                          className="px-2 py-0.5 text-xs bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 rounded-full"
                        >
                          {label.trim()}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                {/* Stats - only show if there are countable tasks */}
                {parent.totalTasks > 0 && (
                  <>
                    <div className="grid grid-cols-2 gap-3 mt-4">
                      <div className="flex items-center gap-2 text-sm">
                        <CheckCircle className="w-4 h-4 text-green-500" />
                        <span className="text-slate-600 dark:text-slate-400">
                          {parent.completedTasks}/{parent.totalTasks} tasks
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-sm">
                        <span className="text-slate-600 dark:text-slate-400">
                          {parent.totalStoryPoints} pts
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-sm col-span-2">
                        <Clock className="w-4 h-4 text-blue-500" />
                        <span className="text-slate-600 dark:text-slate-400">
                          {formatTime(parent.totalTimeSpentMinutes)} logged (children)
                        </span>
                      </div>
                      {parent.timeSpentMinutes !== undefined && parent.timeSpentMinutes > 0 && (
                        <div className="flex items-center gap-2 text-sm col-span-2">
                          <Clock className="w-4 h-4 text-amber-500" />
                          <span className="text-slate-600 dark:text-slate-400">
                            {formatTime(parent.timeSpentMinutes)} logged (parent)
                          </span>
                        </div>
                      )}
                    </div>

                    {/* Progress */}
                    <div className="mt-4">
                      <div className="flex justify-between text-sm mb-1">
                        <span className="text-slate-500 dark:text-slate-400">Progress</span>
                        <span className="font-medium text-slate-900 dark:text-slate-100">
                          {Math.round((parent.completedTasks / parent.totalTasks) * 100)}%
                        </span>
                      </div>
                      <div className="w-full bg-slate-100 dark:bg-slate-700 rounded-full h-2">
                        <div
                          className="bg-amber-500 h-2 rounded-full transition-all"
                          style={{
                            width: `${(parent.completedTasks / parent.totalTasks) * 100}%`
                          }}
                        />
                      </div>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
