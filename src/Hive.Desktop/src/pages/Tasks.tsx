import { useEffect, useState, useCallback } from 'react'
import { AlertTriangle, Clock, Trash2, Tag, Zap, Timer, Search } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { tasksApi, directReportsApi, projectsApi, settingsApi } from '../services/api'
import { TaskStatus, TaskPriority } from '../types'
import type { TeamTask, DirectReport, Project, StoryPointMapping } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'

const statusColors: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: 'bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300',
  [TaskStatus.Todo]: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
  [TaskStatus.Blocked]: 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400',
  [TaskStatus.InProgress]: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
  [TaskStatus.InReview]: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
  [TaskStatus.InTest]: 'bg-cyan-100 dark:bg-cyan-900/30 text-cyan-700 dark:text-cyan-400',
  [TaskStatus.POAcceptance]: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400',
  [TaskStatus.ReadyToRelease]: 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400',
  [TaskStatus.Done]: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
  [TaskStatus.Cancelled]: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400',
}

const priorityColors: Record<TaskPriority, string> = {
  [TaskPriority.Low]: 'text-slate-500 dark:text-slate-400',
  [TaskPriority.Medium]: 'text-blue-500 dark:text-blue-400',
  [TaskPriority.High]: 'text-amber-500 dark:text-amber-400',
  [TaskPriority.Critical]: 'text-red-500 dark:text-red-400',
}

export default function Tasks() {
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [storyPointMappings, setStoryPointMappings] = useState<StoryPointMapping[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | 'overdue' | TaskStatus>('all')
  const [searchQuery, setSearchQuery] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    type: 0,
    priority: 1,
    assigneeId: '',
    dueDate: '',
    storyPoints: '',
    labels: '',
    sprint: '',
    timeSpentMinutes: ''
  })

  const resetForm = () => {
    setFormData({
      title: '',
      description: '',
      type: 0,
      priority: 1,
      assigneeId: '',
      dueDate: '',
      storyPoints: '',
      labels: '',
      sprint: '',
      timeSpentMinutes: ''
    })
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  useEscapeKey(closeModal, showForm)

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [tasksData, drData, projectsData, settingsData] = await Promise.all([
        tasksApi.getAll(),
        directReportsApi.getAll(),
        projectsApi.getAll(),
        settingsApi.get()
      ])
      setTasks(tasksData)
      setDirectReports(drData)
      setProjects(projectsData)
      setStoryPointMappings(settingsData.storyPointMappings || [])
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const filteredTasks = () => {
    let result = tasks

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase()
      result = result.filter(t =>
        t.title.toLowerCase().includes(query) ||
        t.description?.toLowerCase().includes(query) ||
        t.assigneeName?.toLowerCase().includes(query) ||
        t.projectName?.toLowerCase().includes(query) ||
        t.labels?.toLowerCase().includes(query) ||
        t.sprint?.toLowerCase().includes(query) ||
        t.tags?.toLowerCase().includes(query)
      )
    }

    // Apply status filter
    if (filter === 'overdue') {
      result = result.filter(t => t.isOverdue)
    } else if (filter !== 'all') {
      result = result.filter(t => t.status === filter)
    }

    return result
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const taskData = {
        ...formData,
        assigneeId: formData.assigneeId || null,
        projectId: null, // Projects are linked via labels, not direct assignment
        dueDate: formData.dueDate || null,
        storyPoints: formData.storyPoints ? parseInt(formData.storyPoints) : null,
        timeSpentMinutes: formData.timeSpentMinutes ? parseInt(formData.timeSpentMinutes) : null
      }
      if (editingId) {
        await tasksApi.update(editingId, taskData)
      } else {
        await tasksApi.create(taskData)
      }
      setShowForm(false)
      setEditingId(null)
      resetForm()
      loadData()
    } catch (err) {
      console.error(err)
    }
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this task?')) {
      try {
        await tasksApi.delete(id)
        loadData()
      } catch (err) {
        console.error(err)
      }
    }
  }

  const handleToggleSelect = (id: string) => {
    const newSelected = new Set(selectedIds)
    if (newSelected.has(id)) {
      newSelected.delete(id)
    } else {
      newSelected.add(id)
    }
    setSelectedIds(newSelected)
  }

  const handleSelectAll = () => {
    const currentTasks = filteredTasks()
    if (selectedIds.size === currentTasks.length) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(currentTasks.map(t => t.id)))
    }
  }

  const handleBulkDelete = async () => {
    if (selectedIds.size === 0) return

    if (confirm(`Are you sure you want to delete ${selectedIds.size} task${selectedIds.size > 1 ? 's' : ''}?`)) {
      try {
        await tasksApi.bulkDelete(Array.from(selectedIds))
        setSelectedIds(new Set())
        loadData()
      } catch (err) {
        console.error(err)
      }
    }
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return null
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  }

  const formatTimeSpent = (minutes?: number) => {
    if (!minutes) return null
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    if (hours > 0 && mins > 0) return `${hours}h ${mins}m`
    if (hours > 0) return `${hours}h`
    return `${mins}m`
  }

  // Find projects that share at least one label with the task
  const getMatchedProjects = (taskLabels?: string): Project[] => {
    if (!taskLabels) return []
    const taskLabelSet = new Set(
      taskLabels.split(',').map(l => l.trim().toLowerCase()).filter(l => l)
    )
    if (taskLabelSet.size === 0) return []

    return projects.filter(project => {
      if (!project.labels) return false
      const projectLabels = project.labels.split(',').map(l => l.trim().toLowerCase())
      return projectLabels.some(pl => taskLabelSet.has(pl))
    })
  }

  // Calculate estimated hours from story points using the mappings
  const getEstimatedHours = (task: TeamTask): number | null => {
    // If task already has estimated hours, use that
    if (task.estimatedHours) return task.estimatedHours

    // Otherwise calculate from story points
    if (!task.storyPoints || task.storyPoints <= 0 || storyPointMappings.length === 0) {
      return null
    }

    // Find exact match
    const exactMatch = storyPointMappings.find(m => m.points === task.storyPoints)
    if (exactMatch) return exactMatch.hours

    // Sort mappings by points for interpolation
    const sorted = [...storyPointMappings].sort((a, b) => a.points - b.points)

    // If below minimum, use minimum's ratio
    if (task.storyPoints < sorted[0].points) {
      const ratio = sorted[0].hours / sorted[0].points
      return Math.round(task.storyPoints * ratio)
    }

    // If above maximum, use maximum's ratio
    if (task.storyPoints > sorted[sorted.length - 1].points) {
      const last = sorted[sorted.length - 1]
      const ratio = last.hours / last.points
      return Math.round(task.storyPoints * ratio)
    }

    // Linear interpolation between two closest points
    const lower = sorted.filter(m => m.points <= task.storyPoints!).pop()
    const upper = sorted.find(m => m.points >= task.storyPoints!)

    if (lower && upper && lower.points !== upper.points) {
      const ratio = (task.storyPoints - lower.points) / (upper.points - lower.points)
      return Math.round(lower.hours + ratio * (upper.hours - lower.hours))
    }

    return null
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  const overdueCount = tasks.filter(t => t.isOverdue).length
  const backlogCount = tasks.filter(t => t.status === TaskStatus.Backlog).length
  const todoCount = tasks.filter(t => t.status === TaskStatus.Todo).length
  const blockedCount = tasks.filter(t => t.status === TaskStatus.Blocked).length
  const inProgressCount = tasks.filter(t => t.status === TaskStatus.InProgress).length
  const inReviewCount = tasks.filter(t => t.status === TaskStatus.InReview).length
  const inTestCount = tasks.filter(t => t.status === TaskStatus.InTest).length
  const poAcceptanceCount = tasks.filter(t => t.status === TaskStatus.POAcceptance).length
  const readyToReleaseCount = tasks.filter(t => t.status === TaskStatus.ReadyToRelease).length
  const doneCount = tasks.filter(t => t.status === TaskStatus.Done).length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Tasks</h1>
          <p className="text-slate-500 dark:text-slate-400">Track Jira tasks</p>
        </div>
        <div className="flex items-center gap-3">
          {selectedIds.size > 0 && (
            <button
              onClick={handleBulkDelete}
              className="flex items-center gap-2 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
            >
              <Trash2 className="w-5 h-5" />
              Delete Selected ({selectedIds.size})
            </button>
          )}
          {/* Tasks are imported from Jira - manual creation disabled */}
        </div>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title={editingId ? 'Edit Task' : 'Create Task'} />
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Title</label>
                  <input
                    type="text"
                    value={formData.title}
                    onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
                  <textarea
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    rows={2}
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Priority</label>
                    <select
                      value={formData.priority}
                      onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    >
                      <option value={0}>Low</option>
                      <option value={1}>Medium</option>
                      <option value={2}>High</option>
                      <option value={3}>Critical</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Type</label>
                    <select
                      value={formData.type}
                      onChange={(e) => setFormData({ ...formData, type: parseInt(e.target.value) })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    >
                      <option value={0}>Task</option>
                      <option value={1}>Epic</option>
                      <option value={2}>Story</option>
                      <option value={3}>Sub-task</option>
                      <option value={4}>Bug</option>
                      <option value={5}>Spike</option>
                      <option value={6}>Support</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Assignee</label>
                  <select
                    value={formData.assigneeId}
                    onChange={(e) => setFormData({ ...formData, assigneeId: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                  >
                    <option value="">Unassigned</option>
                    {directReports.map(dr => (
                      <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                    ))}
                  </select>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Due Date</label>
                    <input
                      type="date"
                      value={formData.dueDate}
                      onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Story Points</label>
                    <input
                      type="number"
                      value={formData.storyPoints}
                      onChange={(e) => setFormData({ ...formData, storyPoints: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                      min="0"
                      placeholder="1, 2, 3, 5, 8..."
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Time Spent (minutes)</label>
                  <input
                    type="number"
                    value={formData.timeSpentMinutes}
                    onChange={(e) => setFormData({ ...formData, timeSpentMinutes: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    min="0"
                    placeholder="60"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Sprint</label>
                  <input
                    type="text"
                    value={formData.sprint}
                    onChange={(e) => setFormData({ ...formData, sprint: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    placeholder="Sprint 1"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Labels</label>
                  <input
                    type="text"
                    value={formData.labels}
                    onChange={(e) => setFormData({ ...formData, labels: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    placeholder="frontend, urgent, bug-fix (comma-separated)"
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
                    {editingId ? 'Update' : 'Create'}
                  </button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Overdue Alert */}
      {overdueCount > 0 && (
        <div className="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400">
          <AlertTriangle className="w-5 h-5" />
          <span><strong>{overdueCount}</strong> overdue task{overdueCount > 1 ? 's' : ''} require attention</span>
        </div>
      )}

      {/* Search and Filters */}
      <div className="flex flex-col sm:flex-row gap-4">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search tasks..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100 focus:ring-2 focus:ring-amber-500 focus:border-transparent"
          />
        </div>
        <div className="flex gap-2 flex-wrap">
          {[
            { value: 'all', label: `All (${tasks.length})`, count: tasks.length },
            { value: 'overdue', label: `Overdue (${overdueCount})`, count: overdueCount },
            { value: TaskStatus.Backlog, label: `Backlog (${backlogCount})`, count: backlogCount },
            { value: TaskStatus.Todo, label: `To Do (${todoCount})`, count: todoCount },
            { value: TaskStatus.Blocked, label: `Blocked (${blockedCount})`, count: blockedCount },
            { value: TaskStatus.InProgress, label: `In Progress (${inProgressCount})`, count: inProgressCount },
            { value: TaskStatus.InReview, label: `In Review (${inReviewCount})`, count: inReviewCount },
            { value: TaskStatus.InTest, label: `In Test (${inTestCount})`, count: inTestCount },
            { value: TaskStatus.POAcceptance, label: `PO Acceptance (${poAcceptanceCount})`, count: poAcceptanceCount },
            { value: TaskStatus.ReadyToRelease, label: `Ready To Release (${readyToReleaseCount})`, count: readyToReleaseCount },
            { value: TaskStatus.Done, label: `Done (${doneCount})`, count: doneCount },
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

      {/* Select All */}
      {filteredTasks().length > 0 && (
        <div className="flex items-center gap-2 px-4 py-2 bg-slate-50 dark:bg-slate-800 rounded-lg">
          <input
            type="checkbox"
            checked={filteredTasks().length > 0 && selectedIds.size === filteredTasks().length}
            onChange={handleSelectAll}
            className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500 focus:ring-2 cursor-pointer"
          />
          <label className="text-sm text-slate-600 dark:text-slate-300 cursor-pointer" onClick={handleSelectAll}>
            Select All ({filteredTasks().length})
          </label>
        </div>
      )}

      {/* Tasks List */}
      {filteredTasks().length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500 dark:text-slate-400">No tasks found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {filteredTasks().map((task) => (
            <Card key={task.id} className={task.isOverdue ? 'border-red-300 dark:border-red-700' : ''}>
              <CardContent>
                <div className="flex items-start gap-3">
                  <input
                    type="checkbox"
                    checked={selectedIds.has(task.id)}
                    onChange={() => handleToggleSelect(task.id)}
                    className="mt-1 w-4 h-4 text-amber-500 rounded focus:ring-amber-500 focus:ring-2 cursor-pointer"
                  />
                  <div className="flex items-start justify-between flex-1">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                      <h3 className="font-medium text-slate-900 dark:text-slate-100">{task.title}</h3>
                      {task.isOverdue && (
                        <span className="px-2 py-0.5 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 text-xs font-medium rounded">
                          Overdue
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-3 mt-2 text-sm">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[task.status]}`}>
                        {task.statusName}
                      </span>
                      <span className={`font-medium ${priorityColors[task.priority]}`}>
                        {task.priorityName}
                      </span>
                      <span className="px-2 py-0.5 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 rounded text-xs">
                        {task.typeName}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 mt-2 text-sm text-slate-500 dark:text-slate-400 flex-wrap">
                      {task.assigneeName && (
                        <span className="flex items-center gap-1">
                          <div className="w-5 h-5 bg-amber-100 dark:bg-amber-900/30 rounded-full flex items-center justify-center text-amber-700 dark:text-amber-400 text-xs font-medium">
                            {task.assigneeName.split(' ').map(n => n[0]).join('')}
                          </div>
                          {task.assigneeName}
                        </span>
                      )}
                      {task.projectName && (
                        <span className="text-purple-600 dark:text-purple-400">{task.projectName}</span>
                      )}
                      {getMatchedProjects(task.labels).length > 0 && (
                        <span className="flex items-center gap-1 flex-wrap">
                          {getMatchedProjects(task.labels)
                            .filter(p => p.name !== task.projectName) // Exclude direct project if already shown
                            .map(p => (
                              <span
                                key={p.id}
                                className="px-2 py-0.5 bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400 rounded text-xs"
                                title={`Matched via labels: ${p.labels}`}
                              >
                                {p.name}
                              </span>
                            ))
                          }
                        </span>
                      )}
                      {task.dueDate && (
                        <span className="flex items-center gap-1">
                          <Clock className="w-4 h-4" />
                          {formatDate(task.dueDate)}
                        </span>
                      )}
                      {getEstimatedHours(task) && (
                        <span>{getEstimatedHours(task)}h estimated</span>
                      )}
                      {task.storyPoints && (
                        <span className="font-semibold text-amber-600 dark:text-amber-400">{task.storyPoints} SP</span>
                      )}
                      {task.timeSpentMinutes && (
                        <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                          <Timer className="w-4 h-4" />
                          {formatTimeSpent(task.timeSpentMinutes)} logged
                        </span>
                      )}
                      {task.sprint && (
                        <span className="flex items-center gap-1 text-blue-600 dark:text-blue-400">
                          <Zap className="w-4 h-4" />
                          {task.sprint}
                        </span>
                      )}
                    </div>
                    {task.labels && (
                      <div className="flex items-center gap-2 mt-2 flex-wrap">
                        <Tag className="w-4 h-4 text-slate-400" />
                        {task.labels.split(',').map((label, idx) => (
                          <span key={idx} className="px-2 py-0.5 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 rounded text-xs">
                            {label.trim()}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    {/* Status change buttons disabled - tasks are read-only */}
                    <button
                      onClick={() => handleDelete(task.id)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                      Delete
                    </button>
                  </div>
                </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
