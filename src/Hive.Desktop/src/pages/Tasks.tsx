import React, { useEffect, useState, useCallback, useRef, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { AlertTriangle, Clock, Trash2, Tag, Zap, Timer, Search, X, Filter, Upload, FileText, CheckCircle, AlertCircle, XCircle, ChevronDown, ChevronUp, ChevronLeft, ChevronRight, Pencil, Pin } from 'lucide-react'
import { Card, CardHeader, CardContent } from '../components/Card'
import { tasksApi, directReportsApi, jiraImportApi, TaskFilters } from '../services/api'
import { TaskStatus, TaskPriority } from '../types'
import type { TeamTask, DirectReport, JiraImportPreview, JiraImportResult, JiraImportRequest, TaskSummaryDto, OverrideTeamTaskFieldsDto, ClearTeamTaskOverridesDto } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
import { useToast, getErrorMessage } from '../contexts/ToastContext'

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
  const [searchParams] = useSearchParams()
  const { showError } = useToast()
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)

  // Initialize filter from URL params if present
  const initialFilter = useMemo(() => {
    const filterParam = searchParams.get('filter')
    if (filterParam === 'overdue') return 'overdue' as const
    const statusValues = Object.values(TaskStatus) as string[]
    if (filterParam && statusValues.includes(filterParam)) {
      return filterParam as unknown as TaskStatus
    }
    return 'all' as const
  }, [searchParams])

  const [filter, setFilter] = useState<'all' | 'overdue' | TaskStatus>(initialFilter)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedLabel, setSelectedLabel] = useState<string | null>(null)
  const [selectedSprint, setSelectedSprint] = useState<string | null>(null)

  // Summary state (from server)
  const [summary, setSummary] = useState<TaskSummaryDto | null>(null)

  // Pagination state
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
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

  // Jira Import State
  const [showImportModal, setShowImportModal] = useState(false)
  const [showImportInstructions, setShowImportInstructions] = useState(false)
  const [csvContent, setCsvContent] = useState<string>('')
  const [importPreview, setImportPreview] = useState<JiraImportPreview | null>(null)
  const [importResult, setImportResult] = useState<JiraImportResult | null>(null)
  const [importLoading, setImportLoading] = useState(false)
  const [importing, setImporting] = useState(false)
  const [updateExisting, setUpdateExisting] = useState(true)
  const [matchField, setMatchField] = useState<'IssueKey' | 'Title'>('IssueKey')
  const [importError, setImportError] = useState<string | null>(null)

  // Override Modal State
  const [showOverrideModal, setShowOverrideModal] = useState(false)
  const [overrideTask, setOverrideTask] = useState<TeamTask | null>(null)
  const [overrideAssigneeEnabled, setOverrideAssigneeEnabled] = useState(false)
  const [overrideAssigneeId, setOverrideAssigneeId] = useState<string>('')
  const [overrideEstimationEnabled, setOverrideEstimationEnabled] = useState(false)
  const [overrideStoryPoints, setOverrideStoryPoints] = useState<string>('')
  const [overrideTimeSpentEnabled, setOverrideTimeSpentEnabled] = useState(false)
  const [overrideTimeSpentMinutes, setOverrideTimeSpentMinutes] = useState<string>('')
  const [overridePreviousSPEnabled, setOverridePreviousSPEnabled] = useState(false)
  const [overridePreviousSP, setOverridePreviousSP] = useState<string>('')
  const [overrideSaving, setOverrideSaving] = useState(false)

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

  const resetImportForm = () => {
    setCsvContent('')
    setImportPreview(null)
    setImportResult(null)
    setImportError(null)
  }

  const closeModal = useCallback(() => {
    setShowForm(false)
    setEditingId(null)
    resetForm()
  }, [])

  const closeImportModal = useCallback(() => {
    setShowImportModal(false)
    resetImportForm()
  }, [])

  const openOverrideModal = useCallback((task: TeamTask) => {
    setOverrideTask(task)
    const overridden = task.overriddenFields || ''
    const overriddenFields = overridden.split(',').map(f => f.trim().toLowerCase())
    const isAssigneeOverridden = overriddenFields.includes('assigneeid')
    const isEstimationOverridden = overriddenFields.includes('storypoints')
    const isTimeSpentOverridden = overriddenFields.includes('timespentminutes')
    const isPreviousSPOverridden = overriddenFields.includes('previoussprintsstorypoints')
    setOverrideAssigneeEnabled(isAssigneeOverridden)
    setOverrideAssigneeId(task.assigneeId || '')
    setOverrideEstimationEnabled(isEstimationOverridden)
    setOverrideStoryPoints(task.storyPoints?.toString() || '')
    setOverrideTimeSpentEnabled(isTimeSpentOverridden)
    setOverrideTimeSpentMinutes(task.timeSpentMinutes?.toString() || '')
    setOverridePreviousSPEnabled(isPreviousSPOverridden)
    setOverridePreviousSP(task.previousSprintsStoryPoints?.toString() || '')
    setShowOverrideModal(true)
  }, [])

  const closeOverrideModal = useCallback(() => {
    setShowOverrideModal(false)
    setOverrideTask(null)
  }, [])

  const handleOverrideSave = async () => {
    if (!overrideTask) return
    try {
      setOverrideSaving(true)
      const dto: OverrideTeamTaskFieldsDto = {
        assigneeId: overrideAssigneeEnabled ? (overrideAssigneeId || null) : null,
        hasAssigneeOverride: overrideAssigneeEnabled,
        storyPoints: overrideEstimationEnabled ? (overrideStoryPoints ? parseInt(overrideStoryPoints) : null) : null,
        hasEstimationOverride: overrideEstimationEnabled,
        timeSpentMinutes: overrideTimeSpentEnabled ? (overrideTimeSpentMinutes ? parseInt(overrideTimeSpentMinutes) : null) : null,
        hasTimeSpentOverride: overrideTimeSpentEnabled,
        previousSprintsStoryPoints: overridePreviousSPEnabled ? (overridePreviousSP ? parseInt(overridePreviousSP) : null) : null,
        hasPreviousSprintsStoryPointsOverride: overridePreviousSPEnabled,
      }
      await tasksApi.overrideFields(overrideTask.id, dto)
      closeOverrideModal()
      loadData()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    } finally {
      setOverrideSaving(false)
    }
  }

  const handleClearOverride = async (taskId: string, field: string) => {
    try {
      const dto: ClearTeamTaskOverridesDto = { fields: [field] }
      await tasksApi.clearOverrides(taskId, dto)
      loadData()
      // Update the modal if it's open for this task
      if (overrideTask?.id === taskId) {
        const updated = await tasksApi.getById(taskId)
        openOverrideModal(updated)
      }
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
    }
  }

  useEscapeKey(closeModal, showForm)
  useEscapeKey(closeImportModal, showImportModal)
  useEscapeKey(closeOverrideModal, showOverrideModal)

  // Build current filters object
  const buildFilters = useCallback((): TaskFilters => {
    const filters: TaskFilters = {}
    if (filter === 'overdue') {
      filters.filter = 'overdue'
    } else if (filter !== 'all') {
      filters.status = filter as number
    }
    if (searchQuery.trim()) {
      filters.search = searchQuery.trim()
    }
    if (selectedLabel) {
      filters.label = selectedLabel
    }
    if (selectedSprint) {
      filters.sprint = selectedSprint
    }
    return filters
  }, [filter, searchQuery, selectedLabel, selectedSprint])

  const loadData = useCallback(async (page = 1, currentFilters?: TaskFilters) => {
    try {
      setLoading(true)
      const filters = currentFilters ?? buildFilters()
      const [tasksResult, summaryData, drData] = await Promise.all([
        tasksApi.getAll(page, pageSize, filters),
        tasksApi.getSummary(),
        directReportsApi.getAll()
      ])
      setTasks(tasksResult.items)
      setTotalCount(tasksResult.totalCount)
      setTotalPages(tasksResult.totalPages)
      setPageNumber(tasksResult.pageNumber)
      setSummary(summaryData)
      setDirectReports(drData)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [pageSize, buildFilters])

  // Load filtered tasks (without reloading summary/other data)
  const loadFilteredTasks = useCallback(async (page = 1) => {
    try {
      setLoading(true)
      const filters = buildFilters()
      const tasksResult = await tasksApi.getAll(page, pageSize, filters)
      setTasks(tasksResult.items)
      setTotalCount(tasksResult.totalCount)
      setTotalPages(tasksResult.totalPages)
      setPageNumber(tasksResult.pageNumber)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [pageSize, buildFilters])

  useEffect(() => {
    loadData()
  }, [])

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      loadFilteredTasks(newPage)
    }
  }

  const handlePageSizeChange = async (newSize: number) => {
    setPageSize(newSize)
    setPageNumber(1)
    // Reload with new page size
    const filters = buildFilters()
    const result = await tasksApi.getAll(1, newSize, filters)
    setTasks(result.items)
    setTotalCount(result.totalCount)
    setTotalPages(result.totalPages)
    setPageNumber(result.pageNumber)
  }

  // Labels and sprints come from summary (all tasks, not just current page)
  const allLabels = summary?.allLabels ?? []
  const allSprints = summary?.allSprints ?? []

  const clearFilters = useCallback(() => {
    setSearchQuery('')
    setSelectedLabel(null)
    setSelectedSprint(null)
    setFilter('all')
    // Reload with no filters
    loadData(1, {})
  }, [loadData])

  // Handle filter changes - trigger server-side filtering
  const handleFilterChange = useCallback((newFilter: 'all' | 'overdue' | TaskStatus) => {
    setFilter(newFilter)
    setPageNumber(1)
  }, [])

  const handleLabelChange = useCallback((label: string | null) => {
    setSelectedLabel(label)
    setPageNumber(1)
  }, [])

  const handleSprintChange = useCallback((sprint: string | null) => {
    setSelectedSprint(sprint)
    setPageNumber(1)
  }, [])

  // Debounced search
  const searchTimeoutRef = useRef<number | null>(null)
  const handleSearchChange = useCallback((value: string) => {
    setSearchQuery(value)
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current)
    }
    searchTimeoutRef.current = window.setTimeout(() => {
      setPageNumber(1)
      loadFilteredTasks(1)
    }, 300)
  }, [loadFilteredTasks])

  // Track if initial data has been loaded
  const isInitialLoadDone = useRef(false)

  // Effect to reload when filters change (except search which is debounced)
  useEffect(() => {
    // Skip initial render - wait until first data load is done
    if (!isInitialLoadDone.current) return
    loadFilteredTasks(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter, selectedLabel, selectedSprint])

  // Mark initial load as done when loading finishes
  useEffect(() => {
    if (!loading && !isInitialLoadDone.current) {
      isInitialLoadDone.current = true
    }
  }, [loading])

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
      showError(getErrorMessage(err))
    }
  }

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this task?')) {
      try {
        await tasksApi.delete(id)
        loadData()
      } catch (err) {
        console.error(err)
        showError(getErrorMessage(err))
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
    const currentTasks = tasks
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
        showError(getErrorMessage(err))
      }
    }
  }

  // Jira Import Handlers
  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = (e) => {
      const content = e.target?.result as string
      setCsvContent(content)
      setImportPreview(null)
      setImportResult(null)
      setImportError(null)
    }
    reader.onerror = () => {
      setImportError('Failed to read file')
    }
    reader.readAsText(file)
  }

  const handlePreview = async () => {
    if (!csvContent) {
      setImportError('Please select a CSV file first')
      return
    }

    try {
      setImportLoading(true)
      setImportError(null)
      setImportResult(null)
      const previewData = await jiraImportApi.preview(csvContent)
      setImportPreview(previewData)
    } catch (err: any) {
      console.error('Failed to preview import', err)
      setImportError(err.response?.data || err.message || 'Failed to preview CSV')
    } finally {
      setImportLoading(false)
    }
  }

  const handleImport = async () => {
    if (!csvContent) {
      setImportError('Please select a CSV file first')
      return
    }

    if (!confirm(`Import ${importPreview?.validRows || 0} tasks from Jira? ${updateExisting ? 'Existing tasks will be updated.' : 'Existing tasks will be skipped.'}`)) {
      return
    }

    try {
      setImporting(true)
      setImportError(null)
      const importRequest: JiraImportRequest = {
        csvContent,
        updateExisting,
        matchField
      }
      const result = await jiraImportApi.import(importRequest)
      setImportResult(result)
      setImportPreview(null)
      loadData() // Refresh tasks list
    } catch (err: any) {
      console.error('Failed to import', err)
      setImportError(err.response?.data || err.message || 'Failed to import CSV')
    } finally {
      setImporting(false)
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

  const isFieldOverridden = (task: TeamTask, fieldName: string) => {
    if (!task.overriddenFields) return false
    return task.overriddenFields.split(',').map(f => f.trim().toLowerCase()).includes(fieldName.toLowerCase())
  }

  // Note: matchedProjectNames and estimatedHours are now provided by the backend

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  // Counts from summary (reflects all tasks, not just current page)
  const overdueCount = summary?.overdueTasks ?? 0
  const backlogCount = summary?.backlogTasks ?? 0
  const todoCount = summary?.todoTasks ?? 0
  const blockedCount = summary?.blockedTasks ?? 0
  const inProgressCount = summary?.inProgressTasks ?? 0
  const inReviewCount = summary?.inReviewTasks ?? 0
  const inTestCount = summary?.inTestTasks ?? 0
  const poAcceptanceCount = summary?.poAcceptanceTasks ?? 0
  const readyToReleaseCount = summary?.readyToReleaseTasks ?? 0
  const doneCount = summary?.doneTasks ?? 0
  const totalTasksCount = summary?.totalTasks ?? 0

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
          <button
            onClick={() => setShowImportModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
          >
            <Upload className="w-5 h-5" />
            Import from Jira
          </button>
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

      {/* Jira Import Modal */}
      {showImportModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 overflow-y-auto py-8">
          <Card className="w-full max-w-3xl mx-4 max-h-[90vh] overflow-y-auto">
            <CardHeader title="Import from Jira" subtitle="Import tasks from Jira CSV export" />
            <CardContent>
              <div className="space-y-6">
                {/* Instructions (collapsible) */}
                <div className="border dark:border-slate-700 rounded-lg">
                  <button
                    onClick={() => setShowImportInstructions(!showImportInstructions)}
                    className="flex items-center justify-between w-full p-4 text-left"
                  >
                    <span className="font-medium text-slate-700 dark:text-slate-300">How to Export from Jira</span>
                    {showImportInstructions ? <ChevronUp className="w-5 h-5 text-slate-400" /> : <ChevronDown className="w-5 h-5 text-slate-400" />}
                  </button>
                  {showImportInstructions && (
                    <div className="px-4 pb-4">
                      <ol className="list-decimal list-inside space-y-2 text-sm text-slate-600 dark:text-slate-400">
                        <li>Go to your Jira project and navigate to Issues</li>
                        <li>Click on the "..." menu and select "Export"</li>
                        <li>Choose "Export CSV (all fields)" or "Export CSV (current fields)"</li>
                        <li>Save the exported CSV file</li>
                        <li>Upload the CSV file below to preview and import</li>
                      </ol>
                      <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
                        <p className="text-sm text-blue-700 dark:text-blue-400">
                          <strong>Tip:</strong> The importer automatically maps Jira fields (Issue Key, Summary, Status, Priority, Assignee, Story Points) to Hive tasks.
                        </p>
                      </div>
                    </div>
                  )}
                </div>

                {/* File Upload */}
                <div>
                  <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                    Select Jira CSV Export
                  </label>
                  <div className="flex items-center gap-4">
                    <input
                      type="file"
                      accept=".csv"
                      onChange={handleFileChange}
                      className="block w-full text-sm text-slate-500 dark:text-slate-400
                        file:mr-4 file:py-2 file:px-4
                        file:rounded-lg file:border-0
                        file:text-sm file:font-semibold
                        file:bg-amber-50 dark:file:bg-amber-900/20 file:text-amber-700 dark:file:text-amber-400
                        hover:file:bg-amber-100 dark:hover:file:bg-amber-900/30
                        cursor-pointer"
                      disabled={importLoading || importing}
                    />
                    {csvContent && (
                      <button
                        onClick={resetImportForm}
                        className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                        disabled={importLoading || importing}
                      >
                        Clear
                      </button>
                    )}
                  </div>
                </div>

                {csvContent && !importPreview && !importResult && (
                  <button
                    onClick={handlePreview}
                    disabled={importLoading || importing}
                    className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <FileText className="w-5 h-5" />
                    {importLoading ? 'Loading Preview...' : 'Preview Import'}
                  </button>
                )}

                {/* Import Options */}
                {csvContent && (
                  <div className="space-y-3 pt-4 border-t dark:border-slate-700">
                    <h3 className="text-sm font-medium text-slate-700 dark:text-slate-300">Import Options</h3>
                    <div className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        id="updateExisting"
                        checked={updateExisting}
                        onChange={(e) => setUpdateExisting(e.target.checked)}
                        className="w-4 h-4 text-amber-500 bg-white dark:bg-slate-700 border-slate-300 dark:border-slate-600 rounded focus:ring-amber-500"
                        disabled={importLoading || importing}
                      />
                      <label htmlFor="updateExisting" className="text-sm text-slate-700 dark:text-slate-300">
                        Update existing tasks (if unchecked, existing tasks will be skipped)
                      </label>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                        Match existing tasks by:
                      </label>
                      <div className="flex gap-3">
                        <button
                          onClick={() => setMatchField('IssueKey')}
                          disabled={importLoading || importing}
                          className={`px-4 py-2 rounded-lg border transition-colors ${
                            matchField === 'IssueKey'
                              ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                              : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                          }`}
                        >
                          Issue Key (Recommended)
                        </button>
                        <button
                          onClick={() => setMatchField('Title')}
                          disabled={importLoading || importing}
                          className={`px-4 py-2 rounded-lg border transition-colors ${
                            matchField === 'Title'
                              ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20 text-amber-700 dark:text-amber-400'
                              : 'border-slate-200 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-600'
                          }`}
                        >
                          Title
                        </button>
                      </div>
                    </div>
                  </div>
                )}

                {importError && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm">
                    {importError}
                  </div>
                )}

                {/* Preview Section */}
                {importPreview && (
                  <div className="space-y-4 pt-4 border-t dark:border-slate-700">
                    <h3 className="font-medium text-slate-900 dark:text-slate-100">
                      Preview: {importPreview.totalRows} rows ({importPreview.validRows} valid, {importPreview.invalidRows} invalid)
                    </h3>

                    <div className="grid grid-cols-3 gap-4">
                      <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                        <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{importPreview.totalRows}</div>
                        <div className="text-sm text-slate-500 dark:text-slate-400">Total Rows</div>
                      </div>
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                        <div className="text-2xl font-bold text-green-700 dark:text-green-400">{importPreview.validRows}</div>
                        <div className="text-sm text-green-600 dark:text-green-500">Valid</div>
                      </div>
                      <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                        <div className="text-2xl font-bold text-red-700 dark:text-red-400">{importPreview.invalidRows}</div>
                        <div className="text-sm text-red-600 dark:text-red-500">Invalid</div>
                      </div>
                    </div>

                    <div>
                      <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Detected Columns:</h4>
                      <div className="flex flex-wrap gap-2">
                        {importPreview.detectedColumns.map((col) => (
                          <span key={col} className="px-2 py-1 bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 text-xs rounded">
                            {col}
                          </span>
                        ))}
                      </div>
                    </div>

                    {importPreview.mappingWarnings.length > 0 && (
                      <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                        <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2">Warnings:</h4>
                        <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500">
                          {importPreview.mappingWarnings.map((warning, idx) => (
                            <li key={idx}>{warning}</li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {importPreview.sampleRows.length > 0 && (
                      <div>
                        <h4 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Sample Rows (first 10):</h4>
                        <div className="overflow-x-auto">
                          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
                            <thead className="bg-slate-50 dark:bg-slate-700/50">
                              <tr>
                                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">#</th>
                                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Issue Key</th>
                                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Summary</th>
                                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Status</th>
                                <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 dark:text-slate-400 uppercase">Valid</th>
                              </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                              {importPreview.sampleRows.map((row) => (
                                <tr key={row.rowNumber} className={row.isValid ? '' : 'bg-red-50 dark:bg-red-900/10'}>
                                  <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.rowNumber}</td>
                                  <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.issueKey || '-'}</td>
                                  <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100 max-w-xs truncate">{row.summary || '-'}</td>
                                  <td className="px-4 py-2 text-sm text-slate-900 dark:text-slate-100">{row.status || '-'}</td>
                                  <td className="px-4 py-2">
                                    {row.isValid ? (
                                      <CheckCircle className="w-5 h-5 text-green-500" />
                                    ) : (
                                      <div className="flex items-center gap-1">
                                        <XCircle className="w-5 h-5 text-red-500" />
                                        <span className="text-xs text-red-600 dark:text-red-400">{row.validationErrors[0]}</span>
                                      </div>
                                    )}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    )}

                    <button
                      onClick={handleImport}
                      disabled={importing || importPreview.validRows === 0}
                      className="flex items-center gap-2 px-6 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      <Upload className="w-5 h-5" />
                      {importing ? 'Importing...' : `Import ${importPreview.validRows} Tasks`}
                    </button>
                  </div>
                )}

                {/* Result Section */}
                {importResult && (
                  <div className="space-y-4 pt-4 border-t dark:border-slate-700">
                    <h3 className="font-medium text-slate-900 dark:text-slate-100">Import Results</h3>

                    <div className="grid grid-cols-4 gap-4">
                      <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                        <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">{importResult.totalRows}</div>
                        <div className="text-sm text-slate-500 dark:text-slate-400">Total</div>
                      </div>
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                        <div className="text-2xl font-bold text-green-700 dark:text-green-400">{importResult.successCount}</div>
                        <div className="text-sm text-green-600 dark:text-green-500">Imported</div>
                      </div>
                      <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                        <div className="text-2xl font-bold text-yellow-700 dark:text-yellow-400">{importResult.skippedCount}</div>
                        <div className="text-sm text-yellow-600 dark:text-yellow-500">Skipped</div>
                      </div>
                      <div className="p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
                        <div className="text-2xl font-bold text-red-700 dark:text-red-400">{importResult.errorCount}</div>
                        <div className="text-sm text-red-600 dark:text-red-500">Errors</div>
                      </div>
                    </div>

                    {importResult.successCount > 0 && (
                      <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                        <div className="flex items-center gap-2 text-green-700 dark:text-green-400">
                          <CheckCircle className="w-5 h-5" />
                          <span className="font-medium">Successfully imported {importResult.successCount} tasks!</span>
                        </div>
                        <div className="mt-2 text-sm text-green-600 dark:text-green-500">
                          {importResult.importedTasks.filter(t => t.isNew).length} new tasks created, {importResult.importedTasks.filter(t => t.isUpdated).length} tasks updated
                        </div>
                      </div>
                    )}

                    {importResult.warnings.length > 0 && (
                      <div className="p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
                        <h4 className="text-sm font-medium text-yellow-800 dark:text-yellow-400 mb-2 flex items-center gap-2">
                          <AlertCircle className="w-4 h-4" />
                          Warnings ({importResult.warnings.length}):
                        </h4>
                        <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 dark:text-yellow-500 max-h-32 overflow-y-auto">
                          {importResult.warnings.map((warning, idx) => (
                            <li key={idx}>{warning}</li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {importResult.errors.length > 0 && (
                      <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                        <h4 className="text-sm font-medium text-red-800 dark:text-red-400 mb-2 flex items-center gap-2">
                          <XCircle className="w-4 h-4" />
                          Errors ({importResult.errors.length}):
                        </h4>
                        <ul className="list-disc list-inside space-y-1 text-sm text-red-700 dark:text-red-500 max-h-32 overflow-y-auto">
                          {importResult.errors.map((error, idx) => (
                            <li key={idx}>{error}</li>
                          ))}
                        </ul>
                      </div>
                    )}

                    <button
                      onClick={resetImportForm}
                      className="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
                    >
                      Import Another File
                    </button>
                  </div>
                )}

                {/* Close Button */}
                <div className="flex justify-end pt-4 border-t dark:border-slate-700">
                  <button
                    onClick={closeImportModal}
                    className="px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
                  >
                    Close
                  </button>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Override Fields Modal */}
      {showOverrideModal && overrideTask && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader title="Override Task Fields" subtitle={`Pin field values to preserve them during Jira re-import: ${overrideTask.title}`} />
            <CardContent>
              <div className="space-y-5">
                {/* Assignee Override */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={overrideAssigneeEnabled}
                        onChange={(e) => setOverrideAssigneeEnabled(e.target.checked)}
                        className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500"
                      />
                      <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Assignee</span>
                      {isFieldOverridden(overrideTask, 'AssigneeId') && (
                        <span className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                          <Pin className="w-3 h-3" /> Pinned
                        </span>
                      )}
                    </label>
                    {isFieldOverridden(overrideTask, 'AssigneeId') && (
                      <button
                        onClick={() => handleClearOverride(overrideTask.id, 'AssigneeId')}
                        className="text-xs text-red-500 hover:text-red-700"
                      >
                        Clear Override
                      </button>
                    )}
                  </div>
                  {overrideAssigneeEnabled && (
                    <select
                      value={overrideAssigneeId}
                      onChange={(e) => setOverrideAssigneeId(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                    >
                      <option value="">Unassigned</option>
                      {directReports.map(dr => (
                        <option key={dr.id} value={dr.id}>{dr.fullName}</option>
                      ))}
                    </select>
                  )}
                </div>

                {/* Story Points Override */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={overrideEstimationEnabled}
                        onChange={(e) => setOverrideEstimationEnabled(e.target.checked)}
                        className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500"
                      />
                      <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Story Points + Estimated Hours</span>
                      {isFieldOverridden(overrideTask, 'StoryPoints') && (
                        <span className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                          <Pin className="w-3 h-3" /> Pinned
                        </span>
                      )}
                    </label>
                    {isFieldOverridden(overrideTask, 'StoryPoints') && (
                      <button
                        onClick={() => handleClearOverride(overrideTask.id, 'StoryPoints')}
                        className="text-xs text-red-500 hover:text-red-700"
                      >
                        Clear Override
                      </button>
                    )}
                  </div>
                  {overrideEstimationEnabled && (
                    <input
                      type="number"
                      value={overrideStoryPoints}
                      onChange={(e) => setOverrideStoryPoints(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                      min="0"
                      placeholder="Story points (estimated hours auto-calculated)"
                    />
                  )}
                </div>

                {/* Time Spent Override */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={overrideTimeSpentEnabled}
                        onChange={(e) => setOverrideTimeSpentEnabled(e.target.checked)}
                        className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500"
                      />
                      <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Time Spent</span>
                      {isFieldOverridden(overrideTask, 'TimeSpentMinutes') && (
                        <span className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                          <Pin className="w-3 h-3" /> Pinned
                        </span>
                      )}
                    </label>
                    {isFieldOverridden(overrideTask, 'TimeSpentMinutes') && (
                      <button
                        onClick={() => handleClearOverride(overrideTask.id, 'TimeSpentMinutes')}
                        className="text-xs text-red-500 hover:text-red-700"
                      >
                        Clear Override
                      </button>
                    )}
                  </div>
                  {overrideTimeSpentEnabled && (
                    <input
                      type="number"
                      value={overrideTimeSpentMinutes}
                      onChange={(e) => setOverrideTimeSpentMinutes(e.target.value)}
                      className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                      min="0"
                      placeholder="Time spent in minutes"
                    />
                  )}
                </div>

                {/* Previous Sprints Story Points Override - only show for multi-sprint tasks */}
                {overrideTask.sprint?.includes(',') && (
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <label className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={overridePreviousSPEnabled}
                          onChange={(e) => setOverridePreviousSPEnabled(e.target.checked)}
                          className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500"
                        />
                        <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Previous Sprints SP</span>
                        {isFieldOverridden(overrideTask, 'PreviousSprintsStoryPoints') && (
                          <span className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                            <Pin className="w-3 h-3" /> Pinned
                          </span>
                        )}
                      </label>
                      {isFieldOverridden(overrideTask, 'PreviousSprintsStoryPoints') && (
                        <button
                          onClick={() => handleClearOverride(overrideTask.id, 'PreviousSprintsStoryPoints')}
                          className="text-xs text-red-500 hover:text-red-700"
                        >
                          Clear Override
                        </button>
                      )}
                    </div>
                    {overridePreviousSPEnabled && (
                      <div className="space-y-2">
                        <input
                          type="number"
                          value={overridePreviousSP}
                          onChange={(e) => setOverridePreviousSP(e.target.value)}
                          className="w-full px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-700 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
                          min="0"
                          max={overrideTask.storyPoints || undefined}
                          placeholder="SP completed in previous sprints"
                        />
                        {overrideTask.storyPoints && (
                          <p className="text-xs text-slate-500 dark:text-slate-400">
                            New SP = Total ({overrideTask.storyPoints}) - Previous ({overridePreviousSP || 0}) = {Math.max(0, overrideTask.storyPoints - (parseInt(overridePreviousSP) || 0))}
                          </p>
                        )}
                      </div>
                    )}
                  </div>
                )}

                <div className="flex gap-3 pt-4 border-t dark:border-slate-700">
                  <button
                    type="button"
                    onClick={closeOverrideModal}
                    className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleOverrideSave}
                    disabled={overrideSaving || (!overrideAssigneeEnabled && !overrideEstimationEnabled && !overrideTimeSpentEnabled && !overridePreviousSPEnabled)}
                    className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {overrideSaving ? 'Saving...' : 'Save Overrides'}
                  </button>
                </div>
              </div>
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

      {/* Search & Filters */}
      <div className="space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search tasks..."
            value={searchQuery}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-amber-500"
          />
          {(searchQuery || selectedLabel || selectedSprint || filter !== 'all') && (
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
                onClick={() => handleLabelChange(selectedLabel === label ? null : label)}
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

        {/* Sprints */}
        {allSprints.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Zap className="w-4 h-4 text-slate-400" />
            {allSprints.map((sprint) => (
              <button
                key={sprint}
                onClick={() => handleSprintChange(selectedSprint === sprint ? null : sprint)}
                className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                  selectedSprint === sprint
                    ? 'bg-amber-500 text-white'
                    : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600'
                }`}
              >
                {sprint}
              </button>
            ))}
          </div>
        )}

        {/* Status Filter */}
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <div className="flex gap-2 flex-wrap">
            {[
              { value: 'all' as const, label: `All (${totalTasksCount})`, count: totalTasksCount },
              { value: 'overdue' as const, label: `Overdue (${overdueCount})`, count: overdueCount },
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
                  onClick={() => handleFilterChange(f.value)}
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

      {/* Select All */}
      {tasks.length > 0 && (
        <div className="flex items-center gap-2 px-4 py-2 bg-slate-50 dark:bg-slate-800 rounded-lg">
          <input
            type="checkbox"
            checked={tasks.length > 0 && selectedIds.size === tasks.length}
            onChange={handleSelectAll}
            className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500 focus:ring-2 cursor-pointer"
          />
          <label className="text-sm text-slate-600 dark:text-slate-300 cursor-pointer" onClick={handleSelectAll}>
            Select All ({tasks.length})
          </label>
        </div>
      )}

      {/* Tasks List */}
      {tasks.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500 dark:text-slate-400">No tasks found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {tasks.map((task) => (
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
                          {isFieldOverridden(task, 'AssigneeId') && <Pin className="w-3 h-3 text-amber-500" title="Overridden - preserved during import" />}
                        </span>
                      )}
                      {task.projectName && (
                        <span className="text-purple-600 dark:text-purple-400">{task.projectName}</span>
                      )}
                      {task.matchedProjectNames.length > 0 && (
                        <span className="flex items-center gap-1 flex-wrap">
                          {task.matchedProjectNames
                            .filter(name => name !== task.projectName) // Exclude direct project if already shown
                            .map((name, idx) => (
                              <span
                                key={idx}
                                className="px-2 py-0.5 bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400 rounded text-xs"
                                title="Matched via labels"
                              >
                                {name}
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
                      {task.estimatedHours && (
                        <span>{task.estimatedHours}h estimated</span>
                      )}
                      {task.storyPoints && (
                        <span className="flex items-center gap-1 font-semibold text-amber-600 dark:text-amber-400">
                          {task.storyPoints} SP
                          {isFieldOverridden(task, 'StoryPoints') && <Pin className="w-3 h-3" title="Overridden - preserved during import" />}
                        </span>
                      )}
                      {task.timeSpentMinutes && (
                        <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                          <Timer className="w-4 h-4" />
                          {formatTimeSpent(task.timeSpentMinutes)} logged
                          {isFieldOverridden(task, 'TimeSpentMinutes') && <Pin className="w-3 h-3 text-amber-500" title="Overridden - preserved during import" />}
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
                    <button
                      onClick={() => openOverrideModal(task)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-700 rounded-lg transition-colors"
                    >
                      <Pencil className="w-4 h-4" />
                      Edit
                    </button>
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

      {/* Pagination Controls */}
      {totalPages > 0 && (
        <div className="flex items-center justify-between px-4 py-3 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg">
          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-600 dark:text-slate-400">
              Showing {((pageNumber - 1) * pageSize) + 1} - {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} tasks
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
    </div>
  )
}
