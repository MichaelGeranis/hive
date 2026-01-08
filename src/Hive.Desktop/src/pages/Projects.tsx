import { useEffect, useState, useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Plus, FolderKanban, Calendar, CheckCircle, XCircle, Play, MoreVertical, Edit, Trash2, RotateCcw, Search, Tag, Layers, X, Filter } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { projectsApi } from '../services/api'
import { ProjectStatus } from '../types'
import type { Project } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

const statusColors: Record<ProjectStatus, string> = {
  [ProjectStatus.Planning]: 'bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300',
  [ProjectStatus.Active]: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
  [ProjectStatus.OnHold]: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
  [ProjectStatus.Completed]: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
  [ProjectStatus.Cancelled]: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400',
}

export default function Projects() {
  const [searchParams] = useSearchParams()
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)

  // Initialize filter from URL params if present
  const initialFilter = useMemo(() => {
    const filterParam = searchParams.get('filter')
    if (filterParam === 'active') return 'active' as const
    const statusValues = Object.values(ProjectStatus) as string[]
    if (filterParam && statusValues.includes(filterParam)) {
      return filterParam as unknown as ProjectStatus
    }
    return 'all' as const
  }, [searchParams])

  const [filter, setFilter] = useState<'all' | 'active' | ProjectStatus>(initialFilter)
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    labels: '',
    startDate: '',
    targetEndDate: ''
  })

  const resetForm = () => {
    setFormData({ name: '', description: '', labels: '', startDate: '', targetEndDate: '' })
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  useEscapeKey(closeModal, showForm)

  useEffect(() => {
    loadProjects()
  }, [])

  const loadProjects = async () => {
    try {
      setLoading(true)
      const data = await projectsApi.getAll()
      setProjects(data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const [selectedLabel, setSelectedLabel] = useState<string | null>(null)

  // Get all unique labels from projects
  const allLabels = useMemo(() => {
    const labelSet = new Set<string>()
    projects.forEach(p => {
      if (p.labels) {
        p.labels.split(',').forEach(label => {
          const trimmed = label.trim()
          if (trimmed) labelSet.add(trimmed)
        })
      }
    })
    return Array.from(labelSet).sort()
  }, [projects])

  // Calculate status counts
  const statusCounts = useMemo(() => {
    const activeCount = projects.filter(p => p.status === ProjectStatus.Planning || p.status === ProjectStatus.Active).length
    return {
      all: projects.length,
      active: activeCount,
      [ProjectStatus.Planning]: projects.filter(p => p.status === ProjectStatus.Planning).length,
      [ProjectStatus.OnHold]: projects.filter(p => p.status === ProjectStatus.OnHold).length,
      [ProjectStatus.Completed]: projects.filter(p => p.status === ProjectStatus.Completed).length,
      [ProjectStatus.Cancelled]: projects.filter(p => p.status === ProjectStatus.Cancelled).length,
    }
  }, [projects])

  const clearFilters = () => {
    setSearchQuery('')
    setSelectedLabel(null)
    setFilter('all')
  }

  const filteredProjects = () => {
    let result = projects

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(p =>
        p.name.toLowerCase().includes(query) ||
        p.description?.toLowerCase().includes(query) ||
        p.labels?.toLowerCase().includes(query)
      )
    }

    // Apply label filter
    if (selectedLabel) {
      result = result.filter(p =>
        p.labels?.split(',').some(label => label.trim().toLowerCase() === selectedLabel.toLowerCase())
      )
    }

    // Apply status filter
    if (filter === 'active') {
      result = result.filter(p => p.status === ProjectStatus.Planning || p.status === ProjectStatus.Active)
    } else if (filter !== 'all') {
      result = result.filter(p => p.status === filter)
    }

    return result
  }

  const handleAction = async (id: string, action: 'activate' | 'complete' | 'cancel' | 'hold' | 'reopen') => {
    try {
      if (action === 'activate') await projectsApi.activate(id)
      else if (action === 'complete') await projectsApi.complete(id)
      else if (action === 'cancel') await projectsApi.cancel(id)
      else if (action === 'hold') await projectsApi.hold(id)
      else if (action === 'reopen') await projectsApi.reopen(id)
      loadProjects()
    } catch (err) {
      console.error(err)
    }
  }

  const handleStatusChange = async (projectId: string, newStatus: ProjectStatus) => {
    try {
      // Map status to appropriate API action
      if (newStatus === ProjectStatus.Active) {
        await projectsApi.activate(projectId)
      } else if (newStatus === ProjectStatus.Planning) {
        await projectsApi.reopen(projectId)
      } else if (newStatus === ProjectStatus.OnHold) {
        await projectsApi.hold(projectId)
      } else if (newStatus === ProjectStatus.Completed) {
        await projectsApi.complete(projectId)
      } else if (newStatus === ProjectStatus.Cancelled) {
        await projectsApi.cancel(projectId)
      }
      loadProjects()
    } catch (err) {
      console.error(err)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      // Convert empty strings to null for optional date fields
      const payload = {
        name: formData.name,
        description: formData.description,
        labels: formData.labels,
        startDate: formData.startDate || null,
        targetEndDate: formData.targetEndDate || null
      }
      if (editingId) {
        await projectsApi.update(editingId, payload)
      } else {
        await projectsApi.create(payload)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadProjects()
    } catch (err) {
      console.error(err)
    }
  }

  const handleEdit = (project: Project) => {
    setFormData({
      name: project.name,
      description: project.description || '',
      labels: project.labels || '',
      startDate: project.startDate ? project.startDate.split('T')[0] : '',
      targetEndDate: project.targetEndDate ? project.targetEndDate.split('T')[0] : ''
    })
    setEditingId(project.id)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this project?')) {
      try {
        await projectsApi.delete(id)
        loadProjects()
      } catch (err) {
        console.error(err)
      }
    }
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'Not set'
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
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
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Projects</h1>
          <p className="text-slate-500 dark:text-slate-400">Manage team projects</p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          New Project
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingId ? 'Edit Project' : 'Create Project'} />
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
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description</label>
                  <textarea
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    rows={3}
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Start Date</label>
                    <input
                      type="date"
                      value={formData.startDate}
                      onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Target End Date</label>
                    <input
                      type="date"
                      value={formData.targetEndDate}
                      onChange={(e) => setFormData({ ...formData, targetEndDate: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500"
                    />
                  </div>
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
            placeholder="Search projects..."
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

        {/* Status Filter */}
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <div className="flex gap-2 flex-wrap">
            {[
              { value: 'all', label: `All (${statusCounts.all})`, count: statusCounts.all },
              { value: 'active', label: `Active (${statusCounts.active})`, count: statusCounts.active },
              { value: ProjectStatus.Planning, label: `Planning (${statusCounts[ProjectStatus.Planning]})`, count: statusCounts[ProjectStatus.Planning] },
              { value: ProjectStatus.OnHold, label: `On Hold (${statusCounts[ProjectStatus.OnHold]})`, count: statusCounts[ProjectStatus.OnHold] },
              { value: ProjectStatus.Completed, label: `Completed (${statusCounts[ProjectStatus.Completed]})`, count: statusCounts[ProjectStatus.Completed] },
              { value: ProjectStatus.Cancelled, label: `Cancelled (${statusCounts[ProjectStatus.Cancelled]})`, count: statusCounts[ProjectStatus.Cancelled] },
            ]
              .filter((f) => f.value === 'all' || f.count > 0)
              .map((f) => (
              <button
                key={f.value}
                onClick={() => setFilter(f.value as any)}
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

      {/* Projects Grid */}
      {filteredProjects().length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500 dark:text-slate-400">No projects found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredProjects().map((project) => (
            <Card key={project.id}>
              <CardContent>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3 flex-1">
                    <div className="p-2 bg-purple-100 dark:bg-purple-900/30 rounded-lg">
                      <FolderKanban className="w-5 h-5 text-purple-600 dark:text-purple-400" />
                    </div>
                    <div className="flex-1">
                      <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-1">{project.name}</h3>
                      <select
                        value={project.status}
                        onChange={(e) => handleStatusChange(project.id, Number(e.target.value) as ProjectStatus)}
                        className={`px-2 py-1 rounded-md text-xs font-medium border-none cursor-pointer focus:ring-2 focus:ring-amber-500 ${statusColors[project.status]}`}
                      >
                        <option value={ProjectStatus.Planning}>Planning</option>
                        <option value={ProjectStatus.Active}>Active</option>
                        <option value={ProjectStatus.OnHold}>On Hold</option>
                        <option value={ProjectStatus.Completed}>Completed</option>
                        <option value={ProjectStatus.Cancelled}>Cancelled</option>
                      </select>
                    </div>
                  </div>
                  <div className="relative group">
                    <button className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded">
                      <MoreVertical className="w-5 h-5 text-slate-400" />
                    </button>
                    <div className="absolute right-0 mt-1 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleEdit(project)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700"
                      >
                        <Edit className="w-4 h-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(project.id)}
                        className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                      >
                        <Trash2 className="w-4 h-4" />
                        Delete
                      </button>
                    </div>
                  </div>
                </div>

                {project.description && (
                  <p className="text-sm text-slate-500 dark:text-slate-400 mb-3 line-clamp-2">{project.description}</p>
                )}

                {project.labels && (
                  <div className="flex items-center gap-2 mb-3">
                    <Tag className="w-4 h-4 text-slate-400" />
                    <div className="flex flex-wrap gap-1">
                      {project.labels.split(',').map((label, idx) => (
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

                <div className="space-y-2 text-sm text-slate-600 dark:text-slate-400">
                  <div className="flex items-center gap-2">
                    <Calendar className="w-4 h-4 text-slate-400" />
                    <span>{formatDate(project.startDate)} - {formatDate(project.targetEndDate)}</span>
                  </div>
                  {project.parentCount > 0 && (
                    <div className="flex items-center gap-2">
                      <Layers className="w-4 h-4 text-indigo-500" />
                      <span>{project.parentCount} parent{project.parentCount !== 1 ? 's' : ''}</span>
                    </div>
                  )}
                </div>

                {/* Progress */}
                <div className="mt-4">
                  <div className="flex justify-between text-sm mb-1">
                    <span className="text-slate-500 dark:text-slate-400">Progress</span>
                    <span className="font-medium text-slate-900 dark:text-slate-100">{project.completedTasks}/{project.totalTasks} tasks</span>
                  </div>
                  <div className="w-full bg-slate-100 dark:bg-slate-700 rounded-full h-2">
                    <div
                      className="bg-amber-500 h-2 rounded-full transition-all"
                      style={{
                        width: project.totalTasks > 0
                          ? `${(project.completedTasks / project.totalTasks) * 100}%`
                          : '0%'
                      }}
                    />
                  </div>
                </div>

                {/* Actions */}
                <div className="flex gap-2 mt-4 pt-4 border-t dark:border-slate-700">
                  {project.status === ProjectStatus.Planning && (
                    <button
                      onClick={() => handleAction(project.id, 'activate')}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600"
                    >
                      <Play className="w-4 h-4" />
                      Start
                    </button>
                  )}
                  {project.status === ProjectStatus.Active && (
                    <>
                      <button
                        onClick={() => handleAction(project.id, 'complete')}
                        className="flex items-center gap-1 px-3 py-1.5 text-sm bg-blue-500 text-white rounded-lg hover:bg-blue-600"
                      >
                        <CheckCircle className="w-4 h-4" />
                        Complete
                      </button>
                      <button
                        onClick={() => handleAction(project.id, 'cancel')}
                        className="flex items-center gap-1 px-3 py-1.5 text-sm bg-red-500 text-white rounded-lg hover:bg-red-600"
                      >
                        <XCircle className="w-4 h-4" />
                        Cancel
                      </button>
                    </>
                  )}
                  {(project.status === ProjectStatus.Completed || project.status === ProjectStatus.Cancelled) && (
                    <button
                      onClick={() => handleAction(project.id, 'reopen')}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600"
                    >
                      <RotateCcw className="w-4 h-4" />
                      Reopen
                    </button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
