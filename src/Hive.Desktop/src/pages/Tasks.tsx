import React, { useEffect, useState, useCallback, useRef, useMemo } from 'react'
import { formatDate } from '../utils/dateUtils'
import { useSearchParams } from 'react-router-dom'
import { AlertTriangle, Clock, Trash2, Tag, Zap, Search, X, Filter, Upload, FileText, CheckCircle, AlertCircle, XCircle, ChevronDown, ChevronUp, ChevronLeft, ChevronRight, Pencil, Pin, Copy, Layers, ExternalLink } from 'lucide-react'
import { Card, CardContent } from '../components/Card'
import CollapsibleTags from '../components/CollapsibleTags'
import { tasksApi, directReportsApi, jiraImportApi, settingsApi, jiraSyncApi, sprintsApi, TaskFilters } from '../services/api'
import { TaskStatus, TaskPriority } from '../types'
import type { TeamTask, DirectReport, JiraImportPreview, JiraImportResult, JiraImportRequest, TaskSummaryDto, OverrideTeamTaskFieldsDto, ClearTeamTaskOverridesDto } from '../types'
import { useToast } from '../contexts/ToastContext'
import { apiErrorMessage, getErrorMessage } from '../utils/apiErrors'
import { useConfirm } from '../contexts/ConfirmContext'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { LoadingState } from '../components/PageState'
import { JiraSyncButton } from '../components/JiraSyncButton'
import { AiBeeBadge } from '../components/AiBeeBadge'
import { describeParentSync, describeTaskSync, parentSyncChangedAnything } from '../utils/jiraSync'

const getJiraIssueKey = (tags: string): string | null => {
  const match = tags.match(/jira:([^,]+)/)
  return match ? match[1] : null
}

const statusColors: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: 'bg-slate-100 text-slate-700',
  [TaskStatus.Todo]: 'bg-blue-100 text-blue-700',
  [TaskStatus.Blocked]: 'bg-orange-100 text-orange-700',
  [TaskStatus.InProgress]: 'bg-amber-100 text-amber-700',
  [TaskStatus.InReview]: 'bg-purple-100 text-purple-700',
  [TaskStatus.InTest]: 'bg-cyan-100 text-cyan-700',
  [TaskStatus.POAcceptance]: 'bg-indigo-100 text-indigo-700',
  [TaskStatus.ReadyToRelease]: 'bg-emerald-100 text-emerald-700',
  [TaskStatus.Done]: 'bg-green-100 text-green-700',
  [TaskStatus.Cancelled]: 'bg-red-100 text-red-700',
}

const priorityColors: Record<TaskPriority, string> = {
  [TaskPriority.Low]: 'text-slate-500',
  [TaskPriority.Medium]: 'text-blue-500',
  [TaskPriority.High]: 'text-amber-500',
  [TaskPriority.Critical]: 'text-red-500',
}

function DrillDownChip({ label, onClear }: { label: string; onClear: () => void }) {
  return (
    <span className="inline-flex items-center gap-1 px-2 py-1 bg-amber-100 text-amber-800 rounded-lg">
      {label}
      <button type="button" onClick={onClear} aria-label={`Clear ${label}`} className="hover:text-amber-950">
        <X className="w-3.5 h-3.5" />
      </button>
    </span>
  )
}

// The task form; components are sent only once something is typed in them
interface TaskForm {
  title: string
  description: string
  type: number
  priority: number
  assigneeId: string
  dueDate: string
  storyPoints: string
  labels: string
  sprint: string
  components?: string
}

// The CSV import can answer with a plain-text reason as well as { message }
function importFailure(err: unknown, fallback: string): string {
  const data = (err as { response?: { data?: unknown } } | null)?.response?.data
  if (typeof data === 'string' && data.length > 0) return data
  return apiErrorMessage(err) || (err instanceof Error ? err.message : '') || fallback
}

export default function Tasks() {
  const [searchParams] = useSearchParams()
  const { showError, showSuccess, showWarning } = useToast()
  const confirm = useConfirm()
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [loading, setLoading] = useState(true)
  const [jiraBrowseUrl, setJiraBrowseUrl] = useState<string | null>(null)

  // Initialize filter from URL params if present
  const initialFilter = useMemo(() => {
    const filterParam = searchParams.get('filter')
    if (filterParam === 'overdue') return 'overdue' as const
    if (filterParam === 'stale') return 'stale' as const
    // A status by name (?filter=Blocked) or by number (?filter=3)
    if (filterParam && filterParam in TaskStatus) {
      const status = TaskStatus[filterParam as keyof typeof TaskStatus]
      return (typeof status === 'number' ? status : Number(filterParam)) as TaskStatus
    }
    return 'all' as const
  }, [searchParams])

  const [filter, setFilter] = useState<'all' | 'overdue' | 'stale' | TaskStatus>(initialFilter)
  // ?search= opens on a search, e.g. a Jira key from the dashboard
  const [searchInput, setSearchInput] = useState(() => searchParams.get('search') ?? '')
  const [searchQuery, setSearchQuery] = useState(() => searchParams.get('search') ?? '')
  // The dashboard drills down here with ?label=, ?component=, ?type= and ?assignee=
  const [selectedLabel, setSelectedLabel] = useState<string | null>(() => searchParams.get('label'))
  const [selectedComponent, setSelectedComponent] = useState<string | null>(() => searchParams.get('component'))
  const [selectedType, setSelectedType] = useState<string | null>(() => searchParams.get('type'))
  const [selectedAssignee, setSelectedAssignee] = useState<string | null>(() => searchParams.get('assignee'))
  // ?unattributed=true: only tasks whose components name no product
  const [unattributedOnly, setUnattributedOnly] = useState(() => searchParams.get('unattributed') === 'true')
  // ?unestimated=true: only tasks missing the story points they should have
  const [unestimatedOnly, setUnestimatedOnly] = useState(() => searchParams.get('unestimated') === 'true')
  // ?noAcceptanceCriteria=true: only the upcoming stories and tasks with no acceptance criteria
  const [noAcceptanceCriteriaOnly, setNoAcceptanceCriteriaOnly] = useState(() => searchParams.get('noAcceptanceCriteria') === 'true')
  const [selectedSprint, setSelectedSprint] = useState<string | null>(null)
  // The sprint the team is in today, decided by the backend
  const [currentSprintName, setCurrentSprintName] = useState<string | null>(null)
  const [excludeParents, setExcludeParents] = useState(() => searchParams.get('excludeParents') === 'true')

  // Summary state (from server)
  const [summary, setSummary] = useState<TaskSummaryDto | null>(null)

  // Aggregate state (from filtered paged response)
  const [totalStoryPoints, setTotalStoryPoints] = useState(0)

  // Pagination state
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [formData, setFormData] = useState<TaskForm>({
    title: '',
    description: '',
    type: 0,
    priority: 1,
    assigneeId: '',
    dueDate: '',
    storyPoints: '',
    labels: '',
    sprint: ''
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
  const [overrideStoryPointsEnabled, setOverrideStoryPointsEnabled] = useState(false)
  const [overrideStoryPoints, setOverrideStoryPoints] = useState<string>('')
  const [overridePreviousSPEnabled, setOverridePreviousSPEnabled] = useState(false)
  const [overridePreviousSP, setOverridePreviousSP] = useState<string>('')
  const [overrideSprintEnabled, setOverrideSprintEnabled] = useState(false)
  const [overrideSprint, setOverrideSprint] = useState<string>('')
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
      sprint: ''
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
    const isStoryPointsOverridden = overriddenFields.includes('storypoints')
    const isPreviousSPOverridden = overriddenFields.includes('previoussprintsstorypoints')
    const isSprintOverridden = overriddenFields.includes('sprint')
    setOverrideAssigneeEnabled(isAssigneeOverridden)
    setOverrideAssigneeId(task.assigneeId || '')
    setOverrideStoryPointsEnabled(isStoryPointsOverridden)
    setOverrideStoryPoints(task.storyPoints?.toString() || '')
    setOverridePreviousSPEnabled(isPreviousSPOverridden)
    setOverridePreviousSP(task.previousSprintsStoryPoints?.toString() || '')
    setOverrideSprintEnabled(isSprintOverridden)
    setOverrideSprint(task.sprint || '')
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
        storyPoints: overrideStoryPointsEnabled ? (overrideStoryPoints ? parseInt(overrideStoryPoints) : null) : null,
        hasStoryPointsOverride: overrideStoryPointsEnabled,
        previousSprintsStoryPoints: overridePreviousSPEnabled ? (overridePreviousSP ? parseInt(overridePreviousSP) : null) : null,
        hasPreviousSprintsStoryPointsOverride: overridePreviousSPEnabled,
        sprint: overrideSprintEnabled ? (overrideSprint || null) : null,
        hasSprintOverride: overrideSprintEnabled,
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

  // Build current filters object
  const buildFilters = useCallback((): TaskFilters => {
    const filters: TaskFilters = {}
    if (filter === 'overdue' || filter === 'stale') {
      filters.filter = filter
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
    if (selectedComponent) {
      filters.component = selectedComponent
    }
    if (selectedType) {
      filters.type = selectedType
    }
    if (selectedAssignee) {
      filters.assignee = selectedAssignee
    }
    if (unattributedOnly) {
      filters.unattributed = true
    }
    if (unestimatedOnly) {
      filters.unestimated = true
    }
    if (noAcceptanceCriteriaOnly) {
      filters.noAcceptanceCriteria = true
    }
    if (excludeParents) {
      filters.excludeParents = true
    }
    return filters
  }, [filter, searchQuery, selectedLabel, selectedSprint, selectedComponent, selectedType, selectedAssignee, unattributedOnly, unestimatedOnly, noAcceptanceCriteriaOnly, excludeParents])

  const loadData = useCallback(async (page = 1, currentFilters?: TaskFilters) => {
    try {
      setLoading(true)
      const filters = currentFilters ?? buildFilters()
      const [tasksResult, summaryData, drData, appSettings] = await Promise.all([
        tasksApi.getAll(page, pageSize, filters),
        tasksApi.getSummary(),
        directReportsApi.getAll(),
        settingsApi.get()
      ])
      setTasks(tasksResult.items)
      setTotalCount(tasksResult.totalCount)
      setTotalPages(tasksResult.totalPages)
      setPageNumber(tasksResult.pageNumber)
      setTotalStoryPoints(tasksResult.totalStoryPoints ?? 0)
      setSummary(summaryData)
      setDirectReports(drData)
      setJiraBrowseUrl(appSettings.jiraBrowseUrl ?? null)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [pageSize, buildFilters])

  // Tasks come from the team's sprints in Jira; parents are synced first so every task links
  const handleJiraSync = async () => {
    try {
      const result = await jiraSyncApi.syncTasks()
      const tasks = describeTaskSync(result)
      showSuccess(tasks.success)
      if (tasks.warning) showWarning(tasks.warning)
      if (result.parents && parentSyncChangedAnything(result.parents)) {
        const parents = describeParentSync(result.parents)
        showSuccess(parents.success)
        if (parents.warning) showWarning(parents.warning)
      }
      await loadData(pageNumber)
    } catch (err) {
      console.error('Failed to sync tasks from Jira:', err)
      showError(getErrorMessage(err))
    }
  }

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
      setTotalStoryPoints(tasksResult.totalStoryPoints ?? 0)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [pageSize, buildFilters])

  // `?sprint=current` opens on the sprint the team is in today (the dashboard links here);
  // `?sprint=<name>` opens on that sprint.
  useEffect(() => {
    const sprintParam = searchParams.get('sprint')
    const current = sprintsApi.getCurrent().catch(err => {
      console.error('Failed to load the current sprint:', err)
      return null
    })
    current.then(sprint => setCurrentSprintName(sprint?.name ?? null))

    const openOn = (sprint: string | null) => {
      if (!sprint) return loadData()
      setSelectedSprint(sprint)
      return loadData(1, { ...buildFilters(), sprint })
    }
    if (sprintParam === 'current') {
      current.then(sprint => openOn(sprint?.name ?? null))
    } else {
      openOn(sprintParam)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
    setTotalStoryPoints(result.totalStoryPoints ?? 0)
  }

  // Labels and sprints come from summary (all tasks, not just current page)
  const allLabels = summary?.allLabels ?? []
  const allSprints = summary?.allSprints ?? []

  const clearFilters = useCallback(() => {
    setSearchInput('')
    setSearchQuery('')
    setSelectedLabel(null)
    setSelectedSprint(null)
    setSelectedComponent(null)
    setSelectedType(null)
    setSelectedAssignee(null)
    setUnattributedOnly(false)
    setUnestimatedOnly(false)
    setNoAcceptanceCriteriaOnly(false)
    setFilter('all')
    setExcludeParents(false)
    // Reload with no filters
    loadData(1, {})
  }, [loadData])

  // Handle filter changes - trigger server-side filtering
  const handleFilterChange = useCallback((newFilter: 'all' | 'overdue' | 'stale' | TaskStatus) => {
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

  // Search on Enter — state change triggers the useEffect below
  const handleSearchSubmit = useCallback(() => {
    setSearchQuery(searchInput)
  }, [searchInput])

  // Track if initial data has been loaded
  const isInitialLoadDone = useRef(false)

  // Effect to reload when filters change (including search on Enter)
  useEffect(() => {
    // Skip initial render - wait until first data load is done
    if (!isInitialLoadDone.current) return
    loadFilteredTasks(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter, searchQuery, selectedLabel, selectedSprint, selectedComponent, selectedType, selectedAssignee, unattributedOnly, unestimatedOnly, noAcceptanceCriteriaOnly, excludeParents])

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
        storyPoints: formData.storyPoints ? parseInt(formData.storyPoints) : null
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
    if (await confirm({ title: 'Delete this task?', message: 'This cannot be undone.', confirmLabel: 'Delete' })) {
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

    if (await confirm({
      title: `Delete ${selectedIds.size} task${selectedIds.size > 1 ? 's' : ''}?`,
      message: 'This cannot be undone.',
      confirmLabel: 'Delete'
    })) {
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

  const handleEdit = (task: TeamTask) => {
    setFormData({
      title: task.title,
      description: task.description || '',
      type: task.type,
      priority: task.priority,
      assigneeId: task.assigneeId || '',
      dueDate: task.dueDate ? task.dueDate.split('T')[0] : '',
      storyPoints: task.storyPoints?.toString() || '',
      labels: task.labels || '',
      sprint: task.sprint || ''
    })
    setEditingId(task.id)
    setShowForm(true)
  }

  const handleDuplicate = async (id: string) => {
    try {
      await tasksApi.duplicate(id)
      loadData()
    } catch (err) {
      console.error(err)
      showError(getErrorMessage(err))
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
    } catch (err) {
      console.error('Failed to preview import', err)
      setImportError(importFailure(err, 'Failed to preview CSV'))
    } finally {
      setImportLoading(false)
    }
  }

  const handleImport = async () => {
    if (!csvContent) {
      setImportError('Please select a CSV file first')
      return
    }

    if (!(await confirm({
      title: `Import ${importPreview?.validRows || 0} tasks from Jira?`,
      message: updateExisting ? 'Existing tasks will be updated.' : 'Existing tasks will be skipped.',
      confirmLabel: 'Import',
      tone: 'default'
    }))) {
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
    } catch (err) {
      console.error('Failed to import', err)
      setImportError(importFailure(err, 'Failed to import CSV'))
    } finally {
      setImporting(false)
    }
  }

  const isFieldOverridden = (task: TeamTask, fieldName: string) => {
    if (!task.overriddenFields) return false
    return task.overriddenFields.split(',').map(f => f.trim().toLowerCase()).includes(fieldName.toLowerCase())
  }

  if (loading) {
    return <LoadingState fill />
  }

  // Counts from summary (reflects all tasks, not just current page)
  const overdueCount = summary?.overdueTasks ?? 0
  const staleCount = summary?.staleTasks ?? 0
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
      <PageHeader
        title="Tasks"
        subtitle="Track Jira tasks"
        actions={
          <>
            {selectedIds.size > 0 && (
              <button
                onClick={handleBulkDelete}
                className="flex items-center gap-2 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
              >
                <Trash2 className="w-5 h-5" />
                Delete Selected ({selectedIds.size})
              </button>
            )}
            <JiraSyncButton
              onSync={handleJiraSync}
              title="Add, update and remove tasks to match the team's sprints in Jira"
            />
            <button
              onClick={() => setShowImportModal(true)}
              title="Import a Jira CSV export, for one-off corrections"
              className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
            >
              <Upload className="w-5 h-5" />
              Import CSV
            </button>
          </>
        }
      />

      {/* Form Modal */}
      {showForm && (
        <Modal title={editingId ? 'Edit Task' : 'Create Task'} onClose={closeModal} size="md">
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
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Components</label>
              <input
                type="text"
                value={formData.components || ''}
                onChange={(e) => setFormData({ ...formData, ...(e.target.value && { components: e.target.value }) })}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-amber-500"
                placeholder="Authentication, API, Database (comma-separated)"
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
        </Modal>
      )}

      {/* Jira Import Modal */}
      {showImportModal && (
        <Modal title="Import from a Jira CSV" subtitle="For one-off corrections; Sync Jira keeps tasks current" onClose={closeImportModal} size="xl">
          <div className="space-y-6">
            {/* Instructions (collapsible) */}
            <div className="border rounded-lg">
              <button
                onClick={() => setShowImportInstructions(!showImportInstructions)}
                className="flex items-center justify-between w-full p-4 text-left"
              >
                <span className="font-medium text-slate-700">How to Export from Jira</span>
                {showImportInstructions ? <ChevronUp className="w-5 h-5 text-slate-400" /> : <ChevronDown className="w-5 h-5 text-slate-400" />}
              </button>
              {showImportInstructions && (
                <div className="px-4 pb-4">
                  <ol className="list-decimal list-inside space-y-2 text-sm text-slate-600">
                    <li>Go to your Jira project and navigate to Issues</li>
                    <li>Click on the "..." menu and select "Export"</li>
                    <li>Choose "Export CSV (all fields)" or "Export CSV (current fields)"</li>
                    <li>Save the exported CSV file</li>
                    <li>Upload the CSV file below to preview and import</li>
                  </ol>
                  <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
                    <p className="text-sm text-blue-700">
                      <strong>Tip:</strong> The importer automatically maps Jira fields (Issue Key, Summary, Status, Priority, Assignee, Story Points) to Hive tasks.
                    </p>
                  </div>
                </div>
              )}
            </div>

            {/* File Upload */}
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Select Jira CSV Export
              </label>
              <div className="flex items-center gap-4">
                <input
                  type="file"
                  accept=".csv"
                  onChange={handleFileChange}
                  className="block w-full text-sm text-slate-500
                    file:mr-4 file:py-2 file:px-4
                    file:rounded-lg file:border-0
                    file:text-sm file:font-semibold
                    file:bg-amber-50 file:text-amber-700
                    hover:file:bg-amber-100
                    cursor-pointer"
                  disabled={importLoading || importing}
                />
                {csvContent && (
                  <button
                    onClick={resetImportForm}
                    className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50 transition-colors"
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
              <div className="space-y-3 pt-4 border-t">
                <h3 className="text-sm font-medium text-slate-700">Import Options</h3>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="updateExisting"
                    checked={updateExisting}
                    onChange={(e) => setUpdateExisting(e.target.checked)}
                    className="w-4 h-4 text-amber-500 bg-white border-slate-300 rounded focus:ring-amber-500"
                    disabled={importLoading || importing}
                  />
                  <label htmlFor="updateExisting" className="text-sm text-slate-700">
                    Update existing tasks (if unchecked, existing tasks will be skipped)
                  </label>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-2">
                    Match existing tasks by:
                  </label>
                  <div className="flex gap-3">
                    <button
                      onClick={() => setMatchField('IssueKey')}
                      disabled={importLoading || importing}
                      className={`px-4 py-2 rounded-lg border transition-colors ${
                        matchField === 'IssueKey'
                          ? 'border-amber-500 bg-amber-50 text-amber-700'
                          : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
                      }`}
                    >
                      Issue Key (Recommended)
                    </button>
                    <button
                      onClick={() => setMatchField('Title')}
                      disabled={importLoading || importing}
                      className={`px-4 py-2 rounded-lg border transition-colors ${
                        matchField === 'Title'
                          ? 'border-amber-500 bg-amber-50 text-amber-700'
                          : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
                      }`}
                    >
                      Title
                    </button>
                  </div>
                </div>
              </div>
            )}

            {importError && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                {importError}
              </div>
            )}

            {/* Preview Section */}
            {importPreview && (
              <div className="space-y-4 pt-4 border-t">
                <h3 className="font-medium text-slate-900">
                  Preview: {importPreview.totalRows} rows ({importPreview.validRows} valid, {importPreview.invalidRows} invalid)
                </h3>

                <div className="grid grid-cols-3 gap-4">
                  <div className="p-4 bg-slate-50 rounded-lg">
                    <div className="text-2xl font-bold text-slate-900">{importPreview.totalRows}</div>
                    <div className="text-sm text-slate-500">Total Rows</div>
                  </div>
                  <div className="p-4 bg-green-50 rounded-lg">
                    <div className="text-2xl font-bold text-green-700">{importPreview.validRows}</div>
                    <div className="text-sm text-green-600">Valid</div>
                  </div>
                  <div className="p-4 bg-red-50 rounded-lg">
                    <div className="text-2xl font-bold text-red-700">{importPreview.invalidRows}</div>
                    <div className="text-sm text-red-600">Invalid</div>
                  </div>
                </div>

                <div>
                  <h4 className="text-sm font-medium text-slate-700 mb-2">Detected Columns:</h4>
                  <div className="flex flex-wrap gap-2">
                    {importPreview.detectedColumns.map((col) => (
                      <span key={col} className="px-2 py-1 bg-slate-100 text-slate-700 text-xs rounded">
                        {col}
                      </span>
                    ))}
                  </div>
                </div>

                {importPreview.mappingWarnings.length > 0 && (
                  <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <h4 className="text-sm font-medium text-yellow-800 mb-2">Warnings:</h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700">
                      {importPreview.mappingWarnings.map((warning, idx) => (
                        <li key={idx}>{warning}</li>
                      ))}
                    </ul>
                  </div>
                )}

                {importPreview.sampleRows.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium text-slate-700 mb-2">Sample Rows (first 10):</h4>
                    <div className="overflow-x-auto">
                      <table className="min-w-full divide-y divide-slate-200">
                        <thead className="bg-slate-50">
                          <tr>
                            <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 uppercase">#</th>
                            <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 uppercase">Issue Key</th>
                            <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 uppercase">Summary</th>
                            <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 uppercase">Status</th>
                            <th className="px-4 py-2 text-left text-xs font-medium text-slate-500 uppercase">Valid</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-200">
                          {importPreview.sampleRows.map((row) => (
                            <tr key={row.rowNumber} className={row.isValid ? '' : 'bg-red-50'}>
                              <td className="px-4 py-2 text-sm text-slate-900">{row.rowNumber}</td>
                              <td className="px-4 py-2 text-sm text-slate-900">{row.issueKey || '-'}</td>
                              <td className="px-4 py-2 text-sm text-slate-900 max-w-xs truncate">{row.summary || '-'}</td>
                              <td className="px-4 py-2 text-sm text-slate-900">{row.status || '-'}</td>
                              <td className="px-4 py-2">
                                {row.isValid ? (
                                  <CheckCircle className="w-5 h-5 text-green-500" />
                                ) : (
                                  <div className="flex items-center gap-1">
                                    <XCircle className="w-5 h-5 text-red-500" />
                                    <span className="text-xs text-red-600">{row.validationErrors[0]}</span>
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
              <div className="space-y-4 pt-4 border-t">
                <h3 className="font-medium text-slate-900">Import Results</h3>

                <div className="grid grid-cols-4 gap-4">
                  <div className="p-4 bg-slate-50 rounded-lg">
                    <div className="text-2xl font-bold text-slate-900">{importResult.totalRows}</div>
                    <div className="text-sm text-slate-500">Total</div>
                  </div>
                  <div className="p-4 bg-green-50 rounded-lg">
                    <div className="text-2xl font-bold text-green-700">{importResult.successCount}</div>
                    <div className="text-sm text-green-600">Imported</div>
                  </div>
                  <div className="p-4 bg-yellow-50 rounded-lg">
                    <div className="text-2xl font-bold text-yellow-700">{importResult.skippedCount}</div>
                    <div className="text-sm text-yellow-600">Skipped</div>
                  </div>
                  <div className="p-4 bg-red-50 rounded-lg">
                    <div className="text-2xl font-bold text-red-700">{importResult.errorCount}</div>
                    <div className="text-sm text-red-600">Errors</div>
                  </div>
                </div>

                {importResult.successCount > 0 && (
                  <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
                    <div className="flex items-center gap-2 text-green-700">
                      <CheckCircle className="w-5 h-5" />
                      <span className="font-medium">Successfully imported {importResult.successCount} tasks!</span>
                    </div>
                    <div className="mt-2 text-sm text-green-600">
                      {importResult.importedTasks.filter(t => t.isNew).length} new tasks created, {importResult.importedTasks.filter(t => t.isUpdated).length} tasks updated
                    </div>
                  </div>
                )}

                {importResult.warnings.length > 0 && (
                  <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <h4 className="text-sm font-medium text-yellow-800 mb-2 flex items-center gap-2">
                      <AlertCircle className="w-4 h-4" />
                      Warnings ({importResult.warnings.length}):
                    </h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-yellow-700 max-h-32 overflow-y-auto">
                      {importResult.warnings.map((warning, idx) => (
                        <li key={idx}>{warning}</li>
                      ))}
                    </ul>
                  </div>
                )}

                {importResult.errors.length > 0 && (
                  <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                    <h4 className="text-sm font-medium text-red-800 mb-2 flex items-center gap-2">
                      <XCircle className="w-4 h-4" />
                      Errors ({importResult.errors.length}):
                    </h4>
                    <ul className="list-disc list-inside space-y-1 text-sm text-red-700 max-h-32 overflow-y-auto">
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
            <div className="flex justify-end pt-4 border-t">
              <button
                onClick={closeImportModal}
                className="px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                Close
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Override Fields Modal */}
      {showOverrideModal && overrideTask && (
        <Modal title="Override Task Fields" subtitle={`Pin field values to preserve them during Jira re-import: ${overrideTask.title}`} onClose={closeOverrideModal} size="md">
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
                  <span className="text-sm font-medium text-slate-700">Assignee</span>
                  {isFieldOverridden(overrideTask, 'AssigneeId') && (
                    <span className="flex items-center gap-1 text-xs text-amber-600">
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
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
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
                    checked={overrideStoryPointsEnabled}
                    onChange={(e) => setOverrideStoryPointsEnabled(e.target.checked)}
                    className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500"
                  />
                  <span className="text-sm font-medium text-slate-700">Story Points</span>
                  {isFieldOverridden(overrideTask, 'StoryPoints') && (
                    <span className="flex items-center gap-1 text-xs text-amber-600">
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
              {overrideStoryPointsEnabled && (
                <input
                  type="number"
                  value={overrideStoryPoints}
                  onChange={(e) => setOverrideStoryPoints(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                  min="0"
                  placeholder="Story points"
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
                    <span className="text-sm font-medium text-slate-700">Previous Sprints SP</span>
                    {isFieldOverridden(overrideTask, 'PreviousSprintsStoryPoints') && (
                      <span className="flex items-center gap-1 text-xs text-amber-600">
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
                      className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                      min="0"
                      max={overrideTask.storyPoints || undefined}
                      placeholder="SP completed in previous sprints"
                    />
                    {overrideTask.storyPoints && (
                      <p className="text-xs text-slate-500">
                        New SP = Total ({overrideTask.storyPoints}) - Previous ({overridePreviousSP || 0}) = {Math.max(0, overrideTask.storyPoints - (parseInt(overridePreviousSP) || 0))}
                      </p>
                    )}
                  </div>
                )}
              </div>
            )}

            {/* Sprint Override */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={overrideSprintEnabled}
                    onChange={(e) => setOverrideSprintEnabled(e.target.checked)}
                    className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500"
                  />
                  <span className="text-sm font-medium text-slate-700">Sprint</span>
                  {isFieldOverridden(overrideTask, 'Sprint') && (
                    <span className="flex items-center gap-1 text-xs text-amber-600">
                      <Pin className="w-3 h-3" /> Pinned
                    </span>
                  )}
                </label>
                {isFieldOverridden(overrideTask, 'Sprint') && (
                  <button
                    onClick={() => handleClearOverride(overrideTask.id, 'Sprint')}
                    className="text-xs text-red-500 hover:text-red-700"
                  >
                    Clear Override
                  </button>
                )}
              </div>
              {overrideSprintEnabled && (
                <input
                  type="text"
                  value={overrideSprint}
                  onChange={(e) => setOverrideSprint(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
                  placeholder="Sprint name (e.g., Sprint 42)"
                />
              )}
            </div>

            <div className="flex gap-3 pt-4 border-t">
              <button
                type="button"
                onClick={closeOverrideModal}
                className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={handleOverrideSave}
                disabled={overrideSaving || (!overrideAssigneeEnabled && !overrideStoryPointsEnabled && !overridePreviousSPEnabled && !overrideSprintEnabled)}
                className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {overrideSaving ? 'Saving...' : 'Save Overrides'}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Overdue Alert */}
      {overdueCount > 0 && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
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
            placeholder="Search tasks... (press Enter)"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => { if (e.key === 'Enter') handleSearchSubmit() }}
            className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
          />
          {(searchInput || selectedLabel || selectedSprint || selectedComponent || selectedType || selectedAssignee || unattributedOnly || unestimatedOnly || noAcceptanceCriteriaOnly || filter !== 'all' || excludeParents) && (
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
            <CollapsibleTags
              items={allLabels}
              alwaysShow={(label) => label === selectedLabel}
              renderItem={(label) => (
                <button
                  key={label}
                  onClick={() => handleLabelChange(selectedLabel === label ? null : label)}
                  className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                    selectedLabel === label
                      ? 'bg-amber-500 text-white'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {label}
                </button>
              )}
            />
          </div>
        )}

        {/* Sprints */}
        {allSprints.length > 0 && (
          <div className="flex items-center gap-2 flex-wrap">
            <Zap className="w-4 h-4 text-slate-400" />
            {currentSprintName && (
              <button
                onClick={() => handleSprintChange(selectedSprint === currentSprintName ? null : currentSprintName)}
                aria-pressed={selectedSprint === currentSprintName}
                title={currentSprintName}
                className={`px-2 py-1 rounded-full text-xs font-semibold transition-colors ${
                  selectedSprint === currentSprintName
                    ? 'bg-amber-500 text-white'
                    : 'bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100'
                }`}
              >
                Current sprint
              </button>
            )}
            <CollapsibleTags
              items={allSprints}
              alwaysShow={(sprint) => sprint === selectedSprint}
              renderItem={(sprint) => (
                <button
                  key={sprint}
                  onClick={() => handleSprintChange(selectedSprint === sprint ? null : sprint)}
                  className={`px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                    selectedSprint === sprint
                      ? 'bg-amber-500 text-white'
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {sprint}
                </button>
              )}
            />
          </div>
        )}

        {/* Exclude Parents Toggle */}
        <div className="flex items-center gap-2">
          <Layers className="w-4 h-4 text-slate-400" />
          <button
            onClick={() => setExcludeParents(!excludeParents)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
              excludeParents
                ? 'bg-amber-500 text-white'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            Exclude Parents
          </button>
        </div>

        {/* Status Filter */}
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <div className="flex gap-2 flex-wrap">
            {[
              { value: 'all' as const, label: `All (${totalTasksCount})`, count: totalTasksCount },
              { value: 'overdue' as const, label: `Overdue (${overdueCount})`, count: overdueCount },
              { value: 'stale' as const, label: `Stale (${staleCount})`, count: staleCount },
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
                      : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {f.label}
                </button>
              ))}
          </div>
        </div>

        {/* Drill-down filters set from the dashboard */}
        {(selectedAssignee || selectedType || selectedComponent || unattributedOnly || unestimatedOnly || noAcceptanceCriteriaOnly) && (
          <div className="flex items-center gap-2 flex-wrap text-sm">
            <span className="text-slate-500">Showing only:</span>
            {selectedAssignee && (
              <DrillDownChip
                label={selectedAssignee === 'unassigned'
                  ? 'Unassigned'
                  : directReports.find(dr => dr.id === selectedAssignee)?.fullName ?? 'One person'}
                onClear={() => setSelectedAssignee(null)}
              />
            )}
            {selectedType && <DrillDownChip label={`Type: ${selectedType}`} onClear={() => setSelectedType(null)} />}
            {selectedComponent && <DrillDownChip label={`Component: ${selectedComponent}`} onClear={() => setSelectedComponent(null)} />}
            {unattributedOnly && <DrillDownChip label="Not attributed to a product" onClear={() => setUnattributedOnly(false)} />}
            {unestimatedOnly && <DrillDownChip label="Missing story points" onClear={() => setUnestimatedOnly(false)} />}
            {noAcceptanceCriteriaOnly && <DrillDownChip label="Upcoming, no acceptance criteria" onClear={() => setNoAcceptanceCriteriaOnly(false)} />}
          </div>
        )}
        {filter === 'stale' && summary && (
          <p className="text-sm text-slate-500">
            In progress, blocked, in review, in test, awaiting PO acceptance or ready to release, with no status change for {summary.staleTaskDays}+ days since Hive last saw one.
          </p>
        )}
      </div>

      {/* Aggregates Summary Bar */}
      {totalStoryPoints > 0 && (
        <div className="flex items-center gap-6 px-4 py-2.5 bg-slate-50 rounded-lg border border-slate-200">
          <span className="text-sm text-slate-500 font-medium">Totals:</span>
          <span className="flex items-center gap-1 text-sm font-semibold text-amber-600">
            <Zap className="w-4 h-4" />
            {totalStoryPoints} SP
          </span>
        </div>
      )}

      {/* Select All */}
      {tasks.length > 0 && (
        <div className="flex items-center gap-2 px-4 py-2 bg-slate-50 rounded-lg">
          <input
            type="checkbox"
            checked={tasks.length > 0 && selectedIds.size === tasks.length}
            onChange={handleSelectAll}
            className="w-4 h-4 text-amber-500 rounded focus:ring-amber-500 focus:ring-2 cursor-pointer"
          />
          <label className="text-sm text-slate-600 cursor-pointer" onClick={handleSelectAll}>
            Select All ({tasks.length})
          </label>
        </div>
      )}

      {/* Tasks List */}
      {tasks.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-slate-500">No tasks found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {tasks.map((task) => (
            <Card key={task.id} className={task.isOverdue ? 'border-red-300' : ''}>
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
                      {jiraBrowseUrl && getJiraIssueKey(task.tags) && (
                        <a
                          href={`${jiraBrowseUrl}/${getJiraIssueKey(task.tags)}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          onClick={(e) => e.stopPropagation()}
                          title={`Open ${getJiraIssueKey(task.tags)} in Jira`}
                          className="text-blue-500 hover:text-blue-700 flex-shrink-0"
                        >
                          <ExternalLink className="w-4 h-4" />
                        </a>
                      )}
                      <h3 className="font-medium text-slate-900">{task.title}</h3>
                      {task.aiReview && <AiBeeBadge review={task.aiReview} />}
                      {task.isOverdue && (
                        <span className="px-2 py-0.5 bg-red-100 text-red-700 text-xs font-medium rounded">
                          Overdue
                        </span>
                      )}
                      {task.isParentTask && (
                        <span className="px-2 py-0.5 bg-indigo-100 text-indigo-700 text-xs font-medium rounded">
                          Parent
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-3 mt-2 text-sm">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[task.status]}`}>
                        {task.statusName}
                      </span>
                      {filter === 'stale' && (
                        <span className="text-xs text-slate-500">since {formatDate(task.statusChangedAt)}</span>
                      )}
                      <span className={`font-medium ${priorityColors[task.priority]}`}>
                        {task.priorityName}
                      </span>
                      <span className="px-2 py-0.5 bg-slate-100 text-slate-600 rounded text-xs">
                        {task.typeName}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 mt-2 text-sm text-slate-500 flex-wrap">
                          {task.assigneeName && (
                        <span className="flex items-center gap-1">
                          <div className="w-5 h-5 bg-amber-100 rounded-full flex items-center justify-center text-amber-700 text-xs font-medium">
                            {task.assigneeName.split(' ').map(n => n[0]).join('')}
                          </div>
                          {task.assigneeName}
                          {isFieldOverridden(task, 'AssigneeId') && <span title="Overridden - preserved during import"><Pin className="w-3 h-3 text-amber-500" /></span>}
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
                      {task.storyPoints && (
                        <span className="flex items-center gap-1 font-semibold text-amber-600">
                          {task.previousSprintsStoryPoints && task.previousSprintsStoryPoints > 0 ? (
                            <span title={`${task.storyPoints - task.previousSprintsStoryPoints} new + ${task.previousSprintsStoryPoints} carried over`}>
                              {task.storyPoints - task.previousSprintsStoryPoints}+<span className="text-slate-400">{task.previousSprintsStoryPoints}</span> SP
                            </span>
                          ) : (
                            <>{task.storyPoints} SP</>
                          )}
                          {isFieldOverridden(task, 'StoryPoints') && <span title="Overridden - preserved during import"><Pin className="w-3 h-3" /></span>}
                          {isFieldOverridden(task, 'PreviousSprintsStoryPoints') && <span title="Previous SP overridden"><Pin className="w-3 h-3" /></span>}
                        </span>
                      )}
                      {task.sprint && (
                        <span className="flex items-center gap-1 text-blue-600">
                          <Zap className="w-4 h-4" />
                          {task.sprint}
                          {isFieldOverridden(task, 'Sprint') && <span title="Overridden - preserved during import"><Pin className="w-3 h-3 text-amber-500" /></span>}
                        </span>
                      )}
                    </div>
                    {task.labels && (
                      <div className="flex items-center gap-2 mt-2 flex-wrap">
                        <Tag className="w-4 h-4 text-slate-400" />
                        <CollapsibleTags
                          items={task.labels.split(',')}
                          renderItem={(label, idx) => (
                            <span key={idx} className="px-2 py-0.5 bg-slate-100 text-slate-600 rounded text-xs">
                              {label.trim()}
                            </span>
                          )}
                        />
                      </div>
                    )}
                    {task.components && (
                      <div className="flex items-center gap-2 mt-2 flex-wrap">
                        <CollapsibleTags
                          items={task.components.split(',')}
                          renderItem={(component, idx) => (
                            <span key={idx} className="px-2 py-0.5 bg-purple-100 text-purple-700 rounded text-xs">
                              {component.trim()}
                            </span>
                          )}
                        />
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handleEdit(task)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-50 rounded-lg transition-colors"
                      title="Edit task"
                    >
                      <Pencil className="w-4 h-4" />
                      Edit
                    </button>
                    <button
                      onClick={() => handleDuplicate(task.id)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-50 rounded-lg transition-colors"
                      title="Duplicate task"
                    >
                      <Copy className="w-4 h-4" />
                      Duplicate
                    </button>
                    <button
                      onClick={() => openOverrideModal(task)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm text-amber-600 hover:bg-amber-50 rounded-lg transition-colors"
                      title="Pin fields to preserve during Jira import"
                    >
                      <Pin className="w-4 h-4" />
                      Pin
                    </button>
                    <button
                      onClick={() => handleDelete(task.id)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                      title="Delete task"
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
        <div className="flex items-center justify-between px-4 py-3 bg-white border border-slate-200 rounded-lg">
          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-600">
              Showing {((pageNumber - 1) * pageSize) + 1} - {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} tasks
            </span>
            <div className="flex items-center gap-2">
              <span className="text-sm text-slate-600">Per page:</span>
              <select
                value={pageSize}
                onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                className="px-2 py-1 text-sm border border-slate-300 rounded-lg bg-white text-slate-900 focus:ring-2 focus:ring-amber-500"
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
              className="flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-slate-600 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-4 h-4" />
              Previous
            </button>
            <span className="px-3 py-1.5 text-sm font-medium text-slate-900">
              Page {pageNumber} of {totalPages}
            </span>
            <button
              onClick={() => handlePageChange(pageNumber + 1)}
              disabled={pageNumber >= totalPages}
              className="flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-slate-600 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
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
