import { useEffect, useState } from 'react'
import { Plus, AlertTriangle, Clock, Play, CheckCircle, MoreVertical, Edit, Trash2, RotateCcw } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { tasksApi, directReportsApi, projectsApi } from '../services/api'
import { TaskStatus, TaskPriority } from '../types'
import type { TeamTask, DirectReport, Project } from '../types'

const statusColors: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: 'bg-slate-100 text-slate-700',
  [TaskStatus.Todo]: 'bg-blue-100 text-blue-700',
  [TaskStatus.InProgress]: 'bg-amber-100 text-amber-700',
  [TaskStatus.InReview]: 'bg-purple-100 text-purple-700',
  [TaskStatus.Done]: 'bg-green-100 text-green-700',
  [TaskStatus.Cancelled]: 'bg-red-100 text-red-700',
}

const priorityColors: Record<TaskPriority, string> = {
  [TaskPriority.Low]: 'text-slate-500',
  [TaskPriority.Medium]: 'text-blue-500',
  [TaskPriority.High]: 'text-amber-500',
  [TaskPriority.Critical]: 'text-red-500',
}

export default function Tasks() {
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [filter, setFilter] = useState<'all' | 'overdue' | TaskStatus>('all')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    type: 0,
    priority: 1,
    assigneeId: '',
    projectId: '',
    dueDate: '',
    estimatedHours: ''
  })

  const resetForm = () => {
    setFormData({
      title: '',
      description: '',
      type: 0,
      priority: 1,
      assigneeId: '',
      projectId: '',
      dueDate: '',
      estimatedHours: ''
    })
  }

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    try {
      setLoading(true)
      const [tasksData, drData, projectsData] = await Promise.all([
        tasksApi.getAll(),
        directReportsApi.getAll(),
        projectsApi.getAll()
      ])
      setTasks(tasksData)
      setDirectReports(drData)
      setProjects(projectsData)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const filteredTasks = () => {
    if (filter === 'all') return tasks
    if (filter === 'overdue') return tasks.filter(t => t.isOverdue)
    return tasks.filter(t => t.status === filter)
  }

  const handleAction = async (id: string, action: 'start' | 'complete' | 'reopen') => {
    try {
      if (action === 'start') await tasksApi.start(id)
      else if (action === 'complete') await tasksApi.complete(id)
      else if (action === 'reopen') await tasksApi.reopen(id)
      loadData()
    } catch (err) {
      console.error(err)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const taskData = {
        ...formData,
        assigneeId: formData.assigneeId || null,
        projectId: formData.projectId || null,
        dueDate: formData.dueDate || null,
        estimatedHours: formData.estimatedHours ? parseInt(formData.estimatedHours) : null
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

  const handleEdit = (task: TeamTask) => {
    setFormData({
      title: task.title,
      description: task.description || '',
      type: task.type,
      priority: task.priority,
      assigneeId: task.assigneeId || '',
      projectId: task.projectId || '',
      dueDate: task.dueDate ? task.dueDate.split('T')[0] : '',
      estimatedHours: task.estimatedHours?.toString() || ''
    })
    setEditingId(task.id)
    setShowForm(true)
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

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return null
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
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
  const inProgressCount = tasks.filter(t => t.status === TaskStatus.InProgress).length
  const inReviewCount = tasks.filter(t => t.status === TaskStatus.InReview).length
  const doneCount = tasks.filter(t => t.status === TaskStatus.Done).length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Tasks</h1>
          <p className="text-slate-500 mt-1">Manage team tasks and work items</p>
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
        >
          <Plus className="w-5 h-5" />
          New Task
        </button>
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
                      <option value={1}>Bug</option>
                      <option value={2}>Feature</option>
                      <option value={3}>Improvement</option>
                      <option value={4}>Research</option>
                      <option value={5}>Documentation</option>
                    </select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
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
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-1">Project</label>
                    <select
                      value={formData.projectId}
                      onChange={(e) => setFormData({ ...formData, projectId: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                    >
                      <option value="">No Project</option>
                      {projects.map(p => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>
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
                    <label className="block text-sm font-medium text-slate-700 mb-1">Estimated Hours</label>
                    <input
                      type="number"
                      value={formData.estimatedHours}
                      onChange={(e) => setFormData({ ...formData, estimatedHours: e.target.value })}
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                      min="0"
                    />
                  </div>
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
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          <AlertTriangle className="w-5 h-5" />
          <span><strong>{overdueCount}</strong> overdue task{overdueCount > 1 ? 's' : ''} require attention</span>
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-2 flex-wrap">
        {[
          { value: 'all', label: `All (${tasks.length})` },
          { value: 'overdue', label: `Overdue (${overdueCount})` },
          { value: TaskStatus.Backlog, label: `Backlog (${backlogCount})` },
          { value: TaskStatus.Todo, label: `Todo (${todoCount})` },
          { value: TaskStatus.InProgress, label: `In Progress (${inProgressCount})` },
          { value: TaskStatus.InReview, label: `In Review (${inReviewCount})` },
          { value: TaskStatus.Done, label: `Done (${doneCount})` },
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

      {/* Tasks List */}
      {filteredTasks().length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500">No tasks found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {filteredTasks().map((task) => (
            <Card key={task.id} className={task.isOverdue ? 'border-red-300' : ''}>
              <CardContent>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium text-slate-900">{task.title}</h3>
                      {task.isOverdue && (
                        <span className="px-2 py-0.5 bg-red-100 text-red-700 text-xs font-medium rounded">
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
                      <span className="px-2 py-0.5 bg-slate-100 text-slate-600 rounded text-xs">
                        {task.typeName}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 mt-2 text-sm text-slate-500">
                      {task.assigneeName && (
                        <span className="flex items-center gap-1">
                          <div className="w-5 h-5 bg-amber-100 rounded-full flex items-center justify-center text-amber-700 text-xs font-medium">
                            {task.assigneeName.split(' ').map(n => n[0]).join('')}
                          </div>
                          {task.assigneeName}
                        </span>
                      )}
                      {task.projectName && (
                        <span className="text-purple-600">{task.projectName}</span>
                      )}
                      {task.dueDate && (
                        <span className="flex items-center gap-1">
                          <Clock className="w-4 h-4" />
                          {formatDate(task.dueDate)}
                        </span>
                      )}
                      {task.estimatedHours && (
                        <span>{task.estimatedHours}h estimated</span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {(task.status === TaskStatus.Backlog || task.status === TaskStatus.Todo) && (
                      <button
                        onClick={() => handleAction(task.id, 'start')}
                        className="flex items-center gap-1 px-3 py-1.5 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600"
                      >
                        <Play className="w-4 h-4" />
                        Start
                      </button>
                    )}
                    {(task.status === TaskStatus.InProgress || task.status === TaskStatus.InReview) && (
                      <button
                        onClick={() => handleAction(task.id, 'complete')}
                        className="flex items-center gap-1 px-3 py-1.5 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600"
                      >
                        <CheckCircle className="w-4 h-4" />
                        Done
                      </button>
                    )}
                    {task.status === TaskStatus.Done && (
                      <button
                        onClick={() => handleAction(task.id, 'reopen')}
                        className="flex items-center gap-1 px-3 py-1.5 text-sm bg-slate-500 text-white rounded-lg hover:bg-slate-600"
                      >
                        <RotateCcw className="w-4 h-4" />
                        Reopen
                      </button>
                    )}
                    <div className="relative group">
                      <button className="p-1 hover:bg-slate-100 rounded">
                        <MoreVertical className="w-5 h-5 text-slate-400" />
                      </button>
                      <div className="absolute right-0 mt-1 w-36 bg-white border border-slate-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                        <button
                          onClick={() => handleEdit(task)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
                        >
                          <Edit className="w-4 h-4" />
                          Edit
                        </button>
                        <button
                          onClick={() => handleDelete(task.id)}
                          className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                        >
                          <Trash2 className="w-4 h-4" />
                          Delete
                        </button>
                      </div>
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
