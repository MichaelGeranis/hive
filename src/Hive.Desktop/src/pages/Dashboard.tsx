import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Users,
  AlertTriangle,
  X,
  FolderKanban,
  Settings2,
  Eye,
  EyeOff,
  ListTodo,
  Calendar,
  StickyNote,
  Check,
  Star,
  TrendingUp,
  Download
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { SentimentInsights } from '../components/SentimentInsights'
import { reportsApi, tasksApi, projectsApi, meetingNotesApi, notesApi, knowledgePointsApi, projectKnowledgeApi } from '../services/api'
import type { DashboardOverview, TeamTask, TeamVelocity, EstimationAccuracy, Project, CapacityAnalysis, MeetingNote, ManagerNote, KnowledgeLevelSuggestion } from '../types'
import { TaskStatus } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
import { useToast, getErrorMessage } from '../contexts/ToastContext'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  LineChart,
  Line,
  Legend,
  ReferenceLine
} from 'recharts'

const COLORS = ['#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ef4444']

const ALL_SPRINTS_BADGE = (
  <span className="text-xs px-2 py-0.5 rounded-full border border-slate-300 dark:border-slate-500 text-slate-500 dark:text-slate-400 font-normal">
    All sprints
  </span>
)

const CURRENT_SPRINT_BADGE = (
  <span className="text-xs px-2 py-0.5 rounded-full border border-blue-300 dark:border-blue-500 text-blue-500 dark:text-blue-400 font-normal">
    Current
  </span>
)

// Task type specific colors
const TASK_TYPE_COLORS: Record<string, string> = {
  'Spike': '#ef4444',      // Red
  'Task': '#3b82f6',       // Blue
  'Support': '#f59e0b',    // Orange
  'Story': '#10b981',      // Green
  'Sub-task': '#67e8f9',   // Light blue (cyan)
  'SubTask': '#67e8f9',    // Light blue (alternative naming)
  'Bug': '#8b5cf6',        // Purple
  'Epic': '#ec4899',       // Pink
}

const getTaskTypeColor = (typeName: string): string => {
  return TASK_TYPE_COLORS[typeName] || COLORS[Object.keys(TASK_TYPE_COLORS).length % COLORS.length]
}

// Extended color palette for labels
const LABEL_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1']

const getLabelColor = (index: number): string => {
  return LABEL_COLORS[index % LABEL_COLORS.length]
}

// Get initials from a full name (e.g., "John Doe" -> "JD")
const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map(part => part.charAt(0).toUpperCase())
    .join('')
}

const DASHBOARD_WIDGETS_KEY = 'hive-dashboard-widgets'

interface WidgetVisibility {
  topStats: boolean
  projectsDistribution: boolean
  membersByProject: boolean
  tasksDistribution: boolean
  tasksDistributionSP: boolean
  tasksDistributionHours: boolean
  tasksDistributionLabel: boolean
  componentsDistribution: boolean
  supportDistribution: boolean
  teamSentiment: boolean
  capacityAnalysis: boolean
  knowledgeLevelSuggestions: boolean
  estimationAccuracy: boolean
  teamVelocity: boolean
  membersWorkload: boolean
}

const DEFAULT_WIDGETS: WidgetVisibility = {
  topStats: true,
  projectsDistribution: true,
  membersByProject: true,
  tasksDistribution: true,
  tasksDistributionSP: true,
  tasksDistributionHours: true,
  tasksDistributionLabel: true,
  componentsDistribution: true,
  supportDistribution: true,
  teamSentiment: true,
  capacityAnalysis: true,
  knowledgeLevelSuggestions: true,
  estimationAccuracy: true,
  teamVelocity: true,
  membersWorkload: true
}

const WIDGET_LABELS: Record<keyof WidgetVisibility, string> = {
  topStats: 'Top Stats',
  projectsDistribution: 'Projects Distribution',
  membersByProject: 'Members by Project',
  tasksDistribution: 'Tasks Distribution',
  tasksDistributionSP: 'Tasks Distribution (SP)',
  tasksDistributionHours: 'Tasks Distribution (Hours)',
  tasksDistributionLabel: 'Tasks Distribution (Label)',
  componentsDistribution: 'Components Distribution',
  supportDistribution: 'Support Distribution',
  teamSentiment: 'Team Sentiment',
  capacityAnalysis: 'Capacity Analysis',
  knowledgeLevelSuggestions: 'Knowledge Level Suggestions',
  estimationAccuracy: 'Estimation Accuracy',
  teamVelocity: 'Team Velocity',
  membersWorkload: 'Members Workload'
}

export default function Dashboard() {
  const navigate = useNavigate()
  const { showError } = useToast()
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null)
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [velocity, setVelocity] = useState<TeamVelocity | null>(null)
  const [accuracy, setAccuracy] = useState<EstimationAccuracy | null>(null)
  const [includeSupportEstimate, setIncludeSupportEstimate] = useState(false)
  const [capacityAnalysis, setCapacityAnalysis] = useState<CapacityAnalysis | null>(null)
  const [actionItems, setActionItems] = useState<MeetingNote[]>([])
  const [showActionItemsModal, setShowActionItemsModal] = useState(false)
  const [priorityNotes, setPriorityNotes] = useState<ManagerNote[]>([])
  const [showPriorityNotesModal, setShowPriorityNotesModal] = useState(false)
  const [knowledgeSuggestions, setKnowledgeSuggestions] = useState<KnowledgeLevelSuggestion[]>([])
  const [exporting, setExporting] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedMember, setSelectedMember] = useState<string | null>(null)
  const [memberProjects, setMemberProjects] = useState<Project[]>([])
  const [sprintFilter, setSprintFilter] = useState<number | undefined>(undefined)

  // Individual loading states for lazy-loaded widgets
  const [loadingStates, setLoadingStates] = useState({
    velocity: false,
    accuracy: false,
    capacity: false
  })

  // Widget customization state
  const [showCustomize, setShowCustomize] = useState(false)
  const [widgets, setWidgets] = useState<WidgetVisibility>(() => {
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem(DASHBOARD_WIDGETS_KEY)
      if (stored) {
        try {
          return { ...DEFAULT_WIDGETS, ...JSON.parse(stored) }
        } catch {
          return DEFAULT_WIDGETS
        }
      }
    }
    return DEFAULT_WIDGETS
  })

  const toggleWidget = (widget: keyof WidgetVisibility) => {
    const newWidgets = { ...widgets, [widget]: !widgets[widget] }
    setWidgets(newWidgets)
    localStorage.setItem(DASHBOARD_WIDGETS_KEY, JSON.stringify(newWidgets))
  }

  const resetWidgets = () => {
    setWidgets(DEFAULT_WIDGETS)
    localStorage.setItem(DASHBOARD_WIDGETS_KEY, JSON.stringify(DEFAULT_WIDGETS))
  }

  const closeModal = useCallback(() => setSelectedMember(null), [])
  const closeCustomizeModal = useCallback(() => setShowCustomize(false), [])
  const closeActionItemsModal = useCallback(() => setShowActionItemsModal(false), [])
  const closePriorityNotesModal = useCallback(() => setShowPriorityNotesModal(false), [])
  useEscapeKey(closeModal, !!selectedMember)
  useEscapeKey(closeCustomizeModal, showCustomize && !selectedMember)
  useEscapeKey(closeActionItemsModal, showActionItemsModal && !selectedMember && !showCustomize)
  useEscapeKey(closePriorityNotesModal, showPriorityNotesModal && !selectedMember && !showCustomize && !showActionItemsModal)

  // Load core data (always needed)
  useEffect(() => {
    loadCoreData()
  }, [sprintFilter])

  // Clear lazy-loaded data when sprint filter changes so it reloads
  useEffect(() => {
    setVelocity(null)
    setAccuracy(null)
    setCapacityAnalysis(null)
  }, [sprintFilter])

  // Lazy load velocity data when widget becomes visible or filter changes
  useEffect(() => {
    if (widgets.teamVelocity && !velocity && !loadingStates.velocity) {
      loadVelocityData()
    }
  }, [widgets.teamVelocity, velocity])

  // Lazy load accuracy data when widget becomes visible or filter changes
  useEffect(() => {
    if (widgets.estimationAccuracy && !accuracy && !loadingStates.accuracy) {
      loadAccuracyData()
    }
  }, [widgets.estimationAccuracy, accuracy])

  // Lazy load capacity data when widget becomes visible or filter changes
  useEffect(() => {
    if (widgets.capacityAnalysis && !capacityAnalysis && !loadingStates.capacity) {
      loadCapacityData()
    }
  }, [widgets.capacityAnalysis, capacityAnalysis])

  const loadCoreData = async () => {
    try {
      setLoading(true)
      const [dashboardData, tasksData, projectsData, actionItemsData, notesData, knowledgeSuggestionsData] = await Promise.all([
        reportsApi.getDashboard(sprintFilter),
        tasksApi.getAll(),
        projectsApi.getAll(),
        meetingNotesApi.getOpenActionItems(),
        notesApi.getPending(),
        knowledgePointsApi.getSuggestions()
      ])
      setDashboard(dashboardData)
      setTasks(tasksData.items)
      setProjects(projectsData)
      setKnowledgeSuggestions(knowledgeSuggestionsData)
      // Sort action items by due date ascending (earliest first)
      const sortedActionItems = actionItemsData.sort((a, b) => {
        if (!a.actionDueDate && !b.actionDueDate) return 0
        if (!a.actionDueDate) return 1
        if (!b.actionDueDate) return -1
        return new Date(a.actionDueDate).getTime() - new Date(b.actionDueDate).getTime()
      })
      setActionItems(sortedActionItems)
      // Filter for urgent (3) and high (2) priority notes
      // Sort by due date (closest first), then by priority (urgent first)
      const highPriorityNotes = notesData
        .filter((note: ManagerNote) => note.priority >= 2)
        .sort((a: ManagerNote, b: ManagerNote) => {
          // First sort by due date (closest to today first, null dates last)
          if (a.dueDate && b.dueDate) {
            const dateCompare = new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
            if (dateCompare !== 0) return dateCompare
          } else if (a.dueDate && !b.dueDate) {
            return -1 // a has date, b doesn't - a comes first
          } else if (!a.dueDate && b.dueDate) {
            return 1 // b has date, a doesn't - b comes first
          }
          // Then sort by priority (higher priority first: 3=urgent before 2=high)
          return b.priority - a.priority
        })
      setPriorityNotes(highPriorityNotes)
    } catch (err) {
      setError('Failed to load dashboard. Make sure the API is running.')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const loadVelocityData = async () => {
    try {
      setLoadingStates(prev => ({ ...prev, velocity: true }))
      const velocityData = await reportsApi.getTeamVelocity(sprintFilter)
      setVelocity(velocityData)
    } catch (err) {
      console.error('Failed to load velocity data:', err)
    } finally {
      setLoadingStates(prev => ({ ...prev, velocity: false }))
    }
  }

  const loadAccuracyData = async () => {
    try {
      setLoadingStates(prev => ({ ...prev, accuracy: true }))
      const accuracyData = await reportsApi.getEstimationAccuracy(sprintFilter)
      setAccuracy(accuracyData)
    } catch (err) {
      console.error('Failed to load accuracy data:', err)
    } finally {
      setLoadingStates(prev => ({ ...prev, accuracy: false }))
    }
  }

  const loadCapacityData = async () => {
    try {
      setLoadingStates(prev => ({ ...prev, capacity: true }))
      const capacityData = await reportsApi.getCapacityAnalysis(sprintFilter)
      setCapacityAnalysis(capacityData)
    } catch (err) {
      console.error('Failed to load capacity data:', err)
    } finally {
      setLoadingStates(prev => ({ ...prev, capacity: false }))
    }
  }

  const handleCompleteActionItem = async (noteId: string) => {
    try {
      await meetingNotesApi.completeAction(noteId)
      // Remove the completed item from the list
      setActionItems(prev => prev.filter(item => item.id !== noteId))
    } catch (err) {
      console.error('Failed to complete action item:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleCompletePriorityNote = async (noteId: string) => {
    try {
      await notesApi.toggle(noteId)
      // Remove the completed item from the list
      setPriorityNotes(prev => prev.filter(item => item.id !== noteId))
    } catch (err) {
      console.error('Failed to complete priority note:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleIncreaseKnowledgeLevel = async (suggestion: KnowledgeLevelSuggestion) => {
    try {
      // Increase the knowledge level
      await projectKnowledgeApi.createOrUpdate({
        directReportId: suggestion.directReportId,
        projectId: suggestion.projectId,
        knowledgeLevel: suggestion.suggestedLevel
      })
      // Reset points after level increase
      await knowledgePointsApi.resetPoints(suggestion.directReportId, suggestion.projectId)
      // Remove the suggestion from the list
      setKnowledgeSuggestions(prev => prev.filter(
        s => !(s.directReportId === suggestion.directReportId && s.projectId === suggestion.projectId)
      ))
    } catch (err) {
      console.error('Failed to increase knowledge level:', err)
      showError(getErrorMessage(err))
    }
  }

  const handleExport = async () => {
    try {
      setExporting(true)
      const blob = await reportsApi.exportDashboardToExcel(sprintFilter)

      // Create a download link
      const url = window.URL.createObjectURL(blob as Blob)
      const link = document.createElement('a')
      link.href = url

      const fileName = `Dashboard-Report_${new Date().toISOString().split('T')[0]}.xlsx`
      link.download = fileName

      document.body.appendChild(link)
      link.click()

      // Cleanup
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch (err) {
      console.error('Failed to export dashboard:', err)
      showError(getErrorMessage(err))
    } finally {
      setExporting(false)
    }
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

  // Handle clicking on project distribution pie section
  const handleMemberClick = (memberName: string) => {
    // Find all tasks for this member that have labels
    const memberTasks = tasks.filter(t => t.assigneeName === memberName && t.labels)

    // Collect unique projects via label matching
    const projectIdSet = new Set<string>()
    memberTasks.forEach(task => {
      getMatchedProjects(task.labels).forEach(p => projectIdSet.add(p.id))
    })

    // Get the actual project objects
    const memberProjectsList = projects.filter(p => projectIdSet.has(p.id))

    setSelectedMember(memberName)
    setMemberProjects(memberProjectsList)
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 text-red-700 dark:text-red-400">
        {error}
      </div>
    )
  }

  if (!dashboard) return null

  const tasksByAssigneeData = dashboard.tasks.tasksByAssignee.map(assignee => ({
    name: assignee.assigneeName || 'Unassigned',
    total: assignee.totalTasks,
    completed: assignee.completedTasks,
    inProgress: assignee.inProgressTasks,
    pending: assignee.totalTasks - assignee.completedTasks - assignee.inProgressTasks,
    overdue: assignee.overdueTasks
  }))

  // Use backend-computed workload warnings
  const workloadWarnings = dashboard.insights.workloadWarnings.map(w => ({
    name: w.assigneeName,
    inProgress: w.inProgressTasks,
    blocked: w.blockedTasks,
    inReview: w.inReviewTasks,
    issues: w.issues
  }))

  // Calculate project distribution by assignee (using label-based matching)
  const projectsByAssignee = tasks
    .filter(task => task.assigneeId && task.labels) // Only tasks with assignee and labels
    .reduce((acc, task) => {
      const assigneeName = task.assigneeName || 'Unknown'
      if (!acc[assigneeName]) {
        acc[assigneeName] = new Set<string>()
      }
      // Find all projects that match this task's labels
      const matchedProjects = getMatchedProjects(task.labels)
      matchedProjects.forEach(project => {
        acc[assigneeName].add(project.id)
      })
      return acc
    }, {} as Record<string, Set<string>>)

  const projectDistributionData = Object.entries(projectsByAssignee)
    .map(([name, projectIds]) => ({
      name,
      initials: getInitials(name),
      value: projectIds.size
    }))
    .sort((a, b) => b.value - a.value)
    .filter(d => d.value > 0)

  // Use backend-computed unengaged members (already excludes those on leave)
  const unengagedDirectReports = dashboard.insights.unengagedMembers.map(u => u.fullName)

  // Calculate members distribution by project (reverse of projectsByAssignee)
  // This helps identify knowledge silos - projects with less than 2 members
  const membersByProject = tasks
    .filter(task => task.assigneeId && task.labels)
    .reduce((acc, task) => {
      const assigneeName = task.assigneeName || 'Unknown'
      const matchedProjects = getMatchedProjects(task.labels)
      matchedProjects.forEach(project => {
        if (!acc[project.id]) {
          acc[project.id] = { name: project.name, members: new Set<string>() }
        }
        acc[project.id].members.add(assigneeName)
      })
      return acc
    }, {} as Record<string, { name: string; members: Set<string> }>)

  const membersDistributionData = Object.entries(membersByProject)
    .map(([_, data]) => ({
      name: data.name.length > 15 ? data.name.substring(0, 15) + '...' : data.name,
      fullName: data.name,
      value: data.members.size,
      isSilo: data.members.size < 2
    }))
    .sort((a, b) => a.value - b.value) // Sort ascending so silos appear first

  // Use backend-computed knowledge silos count
  const siloCount = dashboard.insights.knowledgeSilos.length

  const taskTypeData = dashboard.tasks.tasksByType.map(type => ({
    name: type.typeName,
    value: type.totalTasks,
    completed: type.completedTasks
  })).filter(d => d.value > 0)

  // Use backend-computed story points distribution by task type
  const taskTypeSPChartData = dashboard.tasks.tasksByTypeSP
    .map(t => ({ name: t.typeName, value: t.totalStoryPoints, tasks: t.taskCount }))
    .sort((a, b) => b.value - a.value)

  // Use backend-computed hours distribution by task type
  const taskTypeHoursChartData = dashboard.tasks.tasksByTypeHours
    .map(t => ({ name: t.typeName, value: t.totalHours, tasks: t.taskCount }))
    .sort((a, b) => b.value - a.value)

  // Use backend-computed label distribution
  const taskLabelChartData = (dashboard.tasks.tasksByLabel || [])
    .map(l => ({ name: l.label, value: l.totalTasks, completed: l.completedTasks, sp: l.totalStoryPoints, completedSP: l.completedStoryPoints, pct: l.percentageOfTotal }))
    .sort((a, b) => b.value - a.value)

  // Use backend-computed component distribution (using hours instead of task count)
  const taskComponentChartData = (dashboard.tasks.tasksByComponent || [])
    .map(c => ({
      name: c.component,
      value: c.totalHours,
      totalTasks: c.totalTasks,
      completed: c.completedTasks,
      sp: c.totalStoryPoints,
      completedSP: c.completedStoryPoints,
      completedHours: c.completedHours,
      pct: c.percentageOfTotalHours
    }))
    .sort((a, b) => b.value - a.value)

  // Use backend-computed total story points for current sprint
  const currentSprintTotalSP = capacityAnalysis?.currentSprint?.totalStoryPoints ?? 0
  // New SP only (excludes carried-over) — matches newCompletedPoints numerator
  const currentSprintNewSP = currentSprintTotalSP - (capacityAnalysis?.currentSprint?.carriedOverPoints ?? 0)

  // Calculate current sprint task counts from unfiltered tasks array
  // This ensures the widget always shows current sprint data regardless of sprint filter
  const currentSprintName = capacityAnalysis?.currentSprint?.sprintName
  const currentSprintTasks = currentSprintName
    ? tasks.filter(t => {
        if (!t.sprint) return false
        // Sprint field can be comma-separated (e.g., "LP_4Q25_S5,LP_4Q25_S6")
        const taskSprints = t.sprint.split(',').map(s => s.trim())
        return taskSprints.includes(currentSprintName)
      })
    : []
  const currentSprintDoneTasks = currentSprintTasks.filter(t => t.status === TaskStatus.Done).length
  const currentSprintTotalTasks = currentSprintTasks.length
  const currentSprintCompletionRate = currentSprintTotalTasks > 0
    ? Math.round(currentSprintDoneTasks / currentSprintTotalTasks * 100)
    : 0

  // Use backend-computed unmatched task count
  const unmatchedTaskCount = dashboard.insights.unmatchedTaskCount

  // Use backend-computed support distribution
  const { supportDistribution } = dashboard.tasks
  const supportByAssigneeData = supportDistribution.byAssignee
    .map(a => ({
      name: a.assigneeName,
      completedHours: a.completedHours,
      allHours: a.allHours,
      maintCompletedHours: a.maintenanceCompletedHours,
      maintAllHours: a.maintenanceAllHours,
    }))

  // Support & maintenance hours data for pie chart
  const supportComparisonData = [
    { name: 'Support (Completed)', hours: supportDistribution.completedHours, color: '#22c55e' },
    { name: 'Support (All)', hours: supportDistribution.allHours, color: '#ef4444' },
    { name: 'Maintenance (Completed)', hours: supportDistribution.maintenanceCompletedHours, color: '#3b82f6' },
    { name: 'Maintenance (All)', hours: supportDistribution.maintenanceAllHours, color: '#8b5cf6' },
  ].filter(d => d.hours > 0)

  // Calculate total warning count from all sources
  const warningCount = (() => {
    let count = 0
    // Workload warnings
    count += workloadWarnings.length
    // Unengaged team members
    if (unengagedDirectReports.length > 0) count++
    // Knowledge silos
    if (siloCount > 0) count++
    // Scope creep (when currentSprintTotalSP > committedPoints and committedPoints > 0)
    if (capacityAnalysis?.currentSprint && currentSprintTotalSP > (capacityAnalysis.currentSprint.committedPoints ?? 0) && (capacityAnalysis.currentSprint.committedPoints ?? 0) > 0) count++
    // Unmatched tasks
    if (unmatchedTaskCount > 0) count++
    // Low capacity utilization
    if ((capacityAnalysis?.averageUtilization ?? 100) < 75) count++
    // Negative velocity trend
    if (velocity && velocity.completionTrend < 0) count++
    // Low estimation accuracy
    if (accuracy && accuracy.sprints.some(s => s.accuracyPercentage < 75)) count++
    return count
  })()

  // Determine warning tile color intensity based on count
  const getWarningBgClass = (count: number): string => {
    if (count === 0) return 'bg-green-50 dark:bg-green-900/20'
    if (count <= 2) return 'bg-amber-50 dark:bg-amber-900/20'
    if (count <= 4) return 'bg-amber-100 dark:bg-amber-900/40'
    if (count <= 6) return 'bg-orange-100 dark:bg-orange-900/40'
    return 'bg-red-100 dark:bg-red-900/40'
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Dashboard</h1>
          <p className="text-slate-500 dark:text-slate-400">Overview team performance</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleExport}
            disabled={exporting}
            className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {exporting ? (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-slate-500"></div>
            ) : (
              <Download className="w-4 h-4" />
            )}
            Export
          </button>
          <button
            onClick={() => setShowCustomize(true)}
            className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
          >
            <Settings2 className="w-4 h-4" />
            Customize
          </button>
        </div>
      </div>

      {/* Sprint Filter */}
      <div className="flex justify-end">
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300">
            Sprint History:
          </label>
          <select
            value={sprintFilter ?? 'all'}
            onChange={(e) => {
              const value = e.target.value === 'all' ? undefined : parseInt(e.target.value);
              setSprintFilter(value);
            }}
            className="px-3 py-2 border border-slate-300 dark:border-slate-600 rounded-lg
                       bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100
                       focus:ring-2 focus:ring-amber-500 focus:border-amber-500"
          >
            <option value="all">All sprints</option>
            <option value="1">Current sprint</option>
            <option value="3">Last 3 sprints</option>
            <option value="6">Last 6 sprints</option>
            <option value="12">Last 12 sprints</option>
          </select>
        </div>
      </div>

      {/* Top Stats */}
      {widgets.topStats && (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-4 auto-rows-fr">
        <StatCard
          title="Team Members"
          value={dashboard.team.totalReports}
          icon={<Users className="w-6 h-6" />}
          badge={ALL_SPRINTS_BADGE}
          color="amber"
          onClick={() => navigate('/team')}
        />
        <StatCard
          title="Projects"
          value={dashboard.tasks.projects.totalProjects}
          icon={<FolderKanban className="w-6 h-6" />}
          badge={ALL_SPRINTS_BADGE}
          color="purple"
          onClick={() => navigate('/projects?filter=active')}
        />
        <StatCard
          title="1:1 Action Items"
          value={actionItems.length}
          subtitle={actionItems.filter(a => a.isOverdue).length > 0 ? `${actionItems.filter(a => a.isOverdue).length} overdue` : undefined}
          icon={<ListTodo className="w-6 h-6" />}
          badge={ALL_SPRINTS_BADGE}
          color={actionItems.some(a => a.isOverdue) ? 'red' : 'blue'}
          onClick={() => setShowActionItemsModal(true)}
        />
        <StatCard
          title="TODOs"
          value={priorityNotes.length}
          subtitle={priorityNotes.filter(n => n.priority === 3).length > 0 ? `${priorityNotes.filter(n => n.priority === 3).length} urgent` : priorityNotes.length > 0 ? `${priorityNotes.filter(n => n.priority === 2).length} high` : undefined}
          icon={<StickyNote className="w-6 h-6" />}
          badge={ALL_SPRINTS_BADGE}
          color={priorityNotes.some(n => n.priority === 3) ? 'red' : priorityNotes.length > 0 ? 'amber' : 'green'}
          onClick={() => setShowPriorityNotesModal(true)}
        />
        <div className={`rounded-xl p-4 ${getWarningBgClass(warningCount)} transition-colors`}>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-600 dark:text-slate-400">Warnings</p>
              <p className={`text-3xl font-bold mt-1 ${
                warningCount === 0 ? 'text-green-600 dark:text-green-400' :
                warningCount <= 3 ? 'text-amber-600 dark:text-amber-400' :
                'text-red-600 dark:text-red-400'
              }`}>
                {warningCount}
              </p>
            </div>
            <div className={`p-3 rounded-lg ${
              warningCount === 0 ? 'bg-green-100 dark:bg-green-800/30' :
              warningCount <= 3 ? 'bg-amber-100 dark:bg-amber-800/30' :
              'bg-red-100 dark:bg-red-800/30'
            }`}>
              <AlertTriangle className={`w-6 h-6 ${
                warningCount === 0 ? 'text-green-600 dark:text-green-400' :
                warningCount <= 3 ? 'text-amber-600 dark:text-amber-400' :
                'text-red-600 dark:text-red-400'
              }`} />
            </div>
          </div>
        </div>

      </div>
      )}

      {/* Sprint & Tasks Overview Widget - Full Width */}
      <div
        className="cursor-pointer"
        onClick={() => navigate('/tasks')}
      >
        <Card className="hover:shadow-lg transition-shadow">
          <CardContent className="py-4">
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
              <div className="flex items-center gap-2">
                <p className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                  {capacityAnalysis?.currentSprint?.sprintName || 'Current Sprint'}
                </p>
                {CURRENT_SPRINT_BADGE}
              </div>
              <div className="flex flex-wrap items-center gap-6">
                {/* SP Progress - New SP only (carried-over excluded from both numerator and denominator) */}
                <div className="flex items-center gap-2">
                  <span className="text-2xl font-bold text-blue-600 dark:text-blue-400">{capacityAnalysis?.currentSprint?.newCompletedPoints ?? 0}/{currentSprintNewSP}</span>
                  <span className="text-sm text-slate-500 dark:text-slate-400">SP {currentSprintNewSP > 0 ? Math.round((capacityAnalysis?.currentSprint?.newCompletedPoints ?? 0) / currentSprintNewSP * 100) : 0}%</span>
                </div>
                <div className="hidden sm:block w-px h-8 bg-slate-200 dark:bg-slate-700" />
                {/* Tasks Count */}
                <div className="flex items-center gap-2">
                  <span className="text-2xl font-bold text-purple-600 dark:text-purple-400">{currentSprintDoneTasks}/{currentSprintTotalTasks}</span>
                  <span className="text-sm text-slate-500 dark:text-slate-400">Tasks {currentSprintCompletionRate}%</span>
                </div>
                {/* Warnings indicators */}
                {(capacityAnalysis?.currentSprint?.carriedOverPoints ?? 0) > 0 && (
                  <div className="flex items-center gap-2 px-3 py-1 bg-blue-50 dark:bg-blue-900/20 rounded-lg text-blue-600 dark:text-blue-400" title={`${capacityAnalysis?.currentSprint?.carriedOverPoints} SP carried over from previous sprints`}>
                    <AlertTriangle className="w-4 h-4" />
                    <span className="text-xs font-medium">+{capacityAnalysis?.currentSprint?.carriedOverPoints} SP carried</span>
                  </div>
                )}
                {(capacityAnalysis?.currentSprint && currentSprintTotalSP > (capacityAnalysis.currentSprint.committedPoints ?? 0) && (capacityAnalysis.currentSprint.committedPoints ?? 0) > 0) && (
                  <div className="flex items-center gap-2 px-3 py-1 bg-amber-50 dark:bg-amber-900/20 rounded-lg text-amber-600 dark:text-amber-400" title={`Scope creep: ${currentSprintTotalSP} SP vs ${capacityAnalysis.currentSprint.committedPoints} committed`}>
                    <AlertTriangle className="w-4 h-4" />
                    <span className="text-xs font-medium">+{currentSprintTotalSP - (capacityAnalysis.currentSprint.committedPoints ?? 0)} SP creep</span>
                  </div>
                )}
                {unmatchedTaskCount > 0 && (
                  <div className="flex items-center gap-2 px-3 py-1 bg-amber-50 dark:bg-amber-900/20 rounded-lg text-amber-600 dark:text-amber-400" title={`${unmatchedTaskCount} tasks not matched to any project`}>
                    <AlertTriangle className="w-4 h-4" />
                    <span className="text-xs font-medium">{unmatchedTaskCount} unmatched</span>
                  </div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Knowledge Level Suggestions */}
      {widgets.knowledgeLevelSuggestions && knowledgeSuggestions.length > 0 && (
        <Card>
          <CardHeader
            title="Knowledge Level Suggestions"
            badge={ALL_SPRINTS_BADGE}
            subtitle={`${knowledgeSuggestions.length} team member${knowledgeSuggestions.length > 1 ? 's have' : ' has'} accumulated enough points for a knowledge level increase`}
            action={
              <button
                onClick={() => navigate('/knowledge')}
                className="text-sm text-amber-500 hover:text-amber-600 flex items-center gap-1"
              >
                View Matrix
                <TrendingUp className="w-4 h-4" />
              </button>
            }
          />
          <CardContent>
            <div className="space-y-3">
              {knowledgeSuggestions.map((suggestion, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-full bg-amber-500 text-white flex items-center justify-center">
                      <Star className="w-4 h-4" />
                    </div>
                    <div>
                      <p className="font-medium text-slate-900 dark:text-slate-100">
                        {suggestion.directReportName}
                      </p>
                      <p className="text-sm text-slate-600 dark:text-slate-400">
                        {suggestion.projectName} - {suggestion.totalPoints} pts accumulated
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-slate-500 dark:text-slate-400">
                      Level {suggestion.currentLevel || 0} → {suggestion.suggestedLevel}
                    </span>
                    <button
                      onClick={() => handleIncreaseKnowledgeLevel(suggestion)}
                      className="px-3 py-1.5 bg-amber-500 hover:bg-amber-600 text-white rounded-lg text-sm font-medium transition-colors"
                    >
                      Increase Level
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Row 3: All Distribution Charts */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-6">
        {/* Projects Distribution by Member */}
        {widgets.projectsDistribution && (
        <Card>
          <CardHeader title="Projects Distribution" badge={ALL_SPRINTS_BADGE} subtitle="How many projects each team member is engaged in" />
          <CardContent>
            {projectDistributionData.length > 0 ? (
              <>
                <div className="h-48">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={projectDistributionData}
                        cx="50%"
                        cy="50%"
                        innerRadius={40}
                        outerRadius={60}
                        paddingAngle={5}
                        dataKey="value"
                        onClick={(data) => handleMemberClick(data.name)}
                        style={{ cursor: 'pointer' }}
                      >
                        {projectDistributionData.map((_, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Pie>
                      <Tooltip
                        formatter={(value: number) => [`${value} projects`]}
                        labelFormatter={(_, payload) => payload?.[0]?.payload?.name || ''}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-2 space-y-1 max-h-32 overflow-y-auto">
                  {projectDistributionData.map((e, i) => ({ ...e, _i: i })).sort((a, b) => b.value - a.value).map((entry) => (
                    <div key={entry.name} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: COLORS[entry._i % COLORS.length] }} />
                        <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                      </div>
                      <span className="font-medium text-slate-900 dark:text-slate-100 ml-2 flex-shrink-0">{entry.value}</span>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                No project assignments found
              </div>
            )}
          </CardContent>
          {unengagedDirectReports.length > 0 && (
            <div className="px-4 pb-4">
              <div className="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
                <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Unengaged team members</p>
                  <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                    {unengagedDirectReports.length === 1
                      ? `${unengagedDirectReports[0]} is not engaged in any projects.`
                      : `${unengagedDirectReports.slice(0, 3).join(', ')}${unengagedDirectReports.length > 3 ? ` and ${unengagedDirectReports.length - 3} more` : ''} are not engaged in any projects.`}
                  </p>
                </div>
              </div>
            </div>
          )}
        </Card>
        )}

        {/* Members Distribution by Project - Knowledge Silos */}
        {widgets.membersByProject && (
        <Card>
          <CardHeader
            title="Members by Project"
            badge={ALL_SPRINTS_BADGE}
            subtitle="How many members are engaged per project"
          />
          <CardContent className="h-64">
            {membersDistributionData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={membersDistributionData} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" allowDecimals={false} />
                  <YAxis type="category" dataKey="name" width={100} tick={{ fontSize: 11 }} />
                  <Tooltip
                    formatter={(value: number) => [`${value} member${value !== 1 ? 's' : ''}`, 'Members']}
                    labelFormatter={(label) => {
                      const item = membersDistributionData.find(d => d.name === label)
                      return item?.fullName || label
                    }}
                  />
                  <Bar
                    dataKey="value"
                    name="Members"
                    radius={[0, 4, 4, 0]}
                  >
                    {membersDistributionData.map((entry, index) => (
                      <Cell
                        key={`cell-${index}`}
                        fill={entry.isSilo ? '#ef4444' : '#10b981'}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
                No project assignments found
              </div>
            )}
          </CardContent>
          {siloCount > 0 && (
            <div className="px-4 pb-4">
              <div className="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
                <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Knowledge silos detected</p>
                  <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                    {siloCount} project{siloCount !== 1 ? 's have' : ' has'} only one member assigned. Consider cross-training or adding backup resources.
                  </p>
                </div>
              </div>
            </div>
          )}
        </Card>
        )}

        {/* Task Type Distribution */}
        {widgets.tasksDistribution && (
        <Card>
          <CardHeader title="Tasks Distribution" subtitle="By type (count)" />
          <CardContent>
            {taskTypeData.length > 0 ? (
              <>
                <div className="h-48">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={taskTypeData}
                        cx="50%"
                        cy="50%"
                        innerRadius={40}
                        outerRadius={60}
                        paddingAngle={5}
                        dataKey="value"
                      >
                        {taskTypeData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={getTaskTypeColor(entry.name)} />
                        ))}
                      </Pie>
                      <Tooltip />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-2 space-y-1 max-h-32 overflow-y-auto">
                  {[...taskTypeData].sort((a, b) => b.value - a.value).map((entry) => (
                    <div key={entry.name} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getTaskTypeColor(entry.name) }} />
                        <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                      </div>
                      <span className="font-medium text-slate-900 dark:text-slate-100 ml-2 flex-shrink-0">{entry.value}</span>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                No tasks found
              </div>
            )}
          </CardContent>
        </Card>
        )}

        {/* Task Type Distribution by Story Points */}
        {widgets.tasksDistributionSP && (
        <Card>
          <CardHeader title="Tasks Distribution" subtitle={`By type (SP)`} />
          <CardContent>
            {taskTypeSPChartData.length > 0 ? (
              <>
                <div className="h-48">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={taskTypeSPChartData}
                        cx="50%"
                        cy="50%"
                        innerRadius={40}
                        outerRadius={60}
                        paddingAngle={5}
                        dataKey="value"
                      >
                        {taskTypeSPChartData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={getTaskTypeColor(entry.name)} />
                        ))}
                      </Pie>
                      <Tooltip
                        formatter={(value: number) => [`${value} SP`, 'Story Points']}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-2 space-y-1 max-h-32 overflow-y-auto">
                  {[...taskTypeSPChartData].sort((a, b) => b.value - a.value).map((entry) => (
                    <div key={entry.name} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getTaskTypeColor(entry.name) }} />
                        <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                      </div>
                      <span className="font-medium text-slate-900 dark:text-slate-100 ml-2 flex-shrink-0">{entry.value} SP</span>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                No story points assigned
              </div>
            )}
          </CardContent>
        </Card>
        )}

        {/* Task Type Distribution by Hours */}
        {widgets.tasksDistributionHours && (
        <Card>
          <CardHeader title="Tasks Distribution" subtitle={`By type (Hours Logged)`} />
          <CardContent>
            {taskTypeHoursChartData.length > 0 ? (
              <>
                <div className="h-48">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={taskTypeHoursChartData}
                        cx="50%"
                        cy="50%"
                        innerRadius={40}
                        outerRadius={60}
                        paddingAngle={5}
                        dataKey="value"
                      >
                        {taskTypeHoursChartData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={getTaskTypeColor(entry.name)} />
                        ))}
                      </Pie>
                      <Tooltip
                        formatter={(value: number) => [`${value}h`, 'Hours Logged']}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-2 space-y-1 max-h-32 overflow-y-auto">
                  {[...taskTypeHoursChartData].sort((a, b) => b.value - a.value).map((entry) => (
                    <div key={entry.name} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getTaskTypeColor(entry.name) }} />
                        <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                      </div>
                      <span className="font-medium text-slate-900 dark:text-slate-100 ml-2 flex-shrink-0">{entry.value}h</span>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                No hours logged
              </div>
            )}
          </CardContent>
        </Card>
        )}

        {/* Task Distribution by Label */}
        {widgets.tasksDistributionLabel && (
        <Card>
          <CardHeader title="Tasks Distribution" subtitle="By label" />
          <CardContent>
            {taskLabelChartData.length > 0 ? (
              <>
                <div className="h-48">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={taskLabelChartData}
                        cx="50%"
                        cy="50%"
                        innerRadius={40}
                        outerRadius={60}
                        paddingAngle={5}
                        dataKey="value"
                      >
                        {taskLabelChartData.map((_, index) => (
                          <Cell key={`cell-${index}`} fill={getLabelColor(index)} />
                        ))}
                      </Pie>
                      <Tooltip
                        formatter={(value: number, _name, props) => {
                          const payload = props.payload as typeof taskLabelChartData[0]
                          return [`${value} tasks (${payload.sp} SP)`, 'Total']
                        }}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-2 space-y-1 max-h-32 overflow-y-auto">
                  {taskLabelChartData.map((entry, index) => (
                    <div key={entry.name} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getLabelColor(index) }} />
                        <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                      </div>
                      <div className="flex items-center gap-2 ml-2 flex-shrink-0">
                        <span className="font-medium text-slate-900 dark:text-slate-100">{entry.value}</span>
                        <span className="text-slate-400">({entry.pct}%)</span>
                      </div>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                No labels assigned
              </div>
            )}
          </CardContent>
        </Card>
        )}

        {/* Components Distribution */}
        {widgets.componentsDistribution && taskComponentChartData.length > 0 && (
        <Card>
          <CardHeader title="Components Distribution" subtitle="By time logged (hours)" />
          <CardContent>
            <div className="h-48">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={taskComponentChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={40}
                    outerRadius={60}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {taskComponentChartData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={getLabelColor(index)} />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(value: number, _name, props) => {
                      const payload = props.payload as typeof taskComponentChartData[0]
                      return [`${value} hours (${payload.totalTasks} tasks, ${payload.sp} SP)`, 'Total']
                    }}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="mt-2 space-y-1 max-h-32 overflow-y-auto">
              {taskComponentChartData.map((entry, index) => (
                <div key={entry.name} className="flex items-center justify-between text-xs">
                  <div className="flex items-center gap-1.5 min-w-0">
                    <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: getLabelColor(index) }} />
                    <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                  </div>
                  <div className="flex items-center gap-2 ml-2 flex-shrink-0">
                    <span className="font-medium text-slate-900 dark:text-slate-100">{entry.value}h</span>
                    <span className="text-slate-400">({entry.pct}%)</span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
        )}
      </div>

      {/* Row 4: Support Distribution */}
      {widgets.supportDistribution && (supportDistribution.allHours > 0 || supportDistribution.maintenanceAllHours > 0) && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Support & Maintenance Hours */}
          <Card>
            <CardHeader
              title="Support & Maintenance Hours"
              subtitle={`${supportDistribution.allTaskCount} support · ${supportDistribution.maintenanceAllTaskCount} maintenance`}
            />
            <CardContent>
              {supportComparisonData.length > 0 ? (
                <>
                  <div className="h-48">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={supportComparisonData}
                          cx="50%"
                          cy="50%"
                          innerRadius={40}
                          outerRadius={60}
                          paddingAngle={5}
                          dataKey="hours"
                        >
                          {supportComparisonData.map((entry) => (
                            <Cell key={entry.name} fill={entry.color} />
                          ))}
                        </Pie>
                        <Tooltip
                          formatter={(value: number) => [`${value}h`, 'Hours']}
                          labelFormatter={(name) => name}
                        />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                  <div className="mt-2 space-y-1">
                    {[...supportComparisonData].sort((a, b) => b.hours - a.hours).map((entry) => (
                      <div key={entry.name} className="flex items-center justify-between text-xs">
                        <div className="flex items-center gap-1.5 min-w-0">
                          <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: entry.color }} />
                          <span className="truncate text-slate-700 dark:text-slate-300">{entry.name}</span>
                        </div>
                        <span className="font-medium text-slate-900 dark:text-slate-100 ml-2 flex-shrink-0">{entry.hours}h</span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <div className="flex items-center justify-center h-64 text-slate-500 dark:text-slate-400">
                  No hours logged
                </div>
              )}
            </CardContent>
            <div className="px-4 pb-4">
              <div className="grid grid-cols-2 gap-4 text-center">
                <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <p className="text-2xl font-bold text-green-600 dark:text-green-400">{supportDistribution.completedHours}h</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{supportDistribution.completedTaskCount} Support completed</p>
                </div>
                <div className="p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
                  <p className="text-2xl font-bold text-red-600 dark:text-red-400">{supportDistribution.allHours}h</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{supportDistribution.allTaskCount} Support all</p>
                </div>
                <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                  <p className="text-2xl font-bold text-blue-600 dark:text-blue-400">{supportDistribution.maintenanceCompletedHours}h</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{supportDistribution.maintenanceCompletedTaskCount} Maint. completed</p>
                </div>
                <div className="p-3 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                  <p className="text-2xl font-bold text-purple-600 dark:text-purple-400">{supportDistribution.maintenanceAllHours}h</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{supportDistribution.maintenanceAllTaskCount} Maint. all</p>
                </div>
              </div>
            </div>
          </Card>

          {/* Support & Maintenance Hours by Assignee */}
          <Card>
            <CardHeader
              title="Support & Maintenance by Assignee"
              subtitle={`Support: ${supportDistribution.allHours}h · Maintenance: ${supportDistribution.maintenanceAllHours}h`}
            />
            <CardContent className="h-80">
              {supportByAssigneeData.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={supportByAssigneeData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" unit="h" />
                    <YAxis type="category" dataKey="name" width={100} tick={{ fontSize: 11 }} />
                    <Tooltip
                      formatter={(value: number, name: string) => [`${value}h`, name]}
                    />
                    <Legend />
                    <Bar
                      dataKey="completedHours"
                      name="Support (Completed)"
                      fill="#22c55e"
                      radius={[0, 4, 4, 0]}
                    />
                    <Bar
                      dataKey="allHours"
                      name="Support (All)"
                      fill="#ef4444"
                      radius={[0, 4, 4, 0]}
                    />
                    <Bar
                      dataKey="maintCompletedHours"
                      name="Maint. (Completed)"
                      fill="#3b82f6"
                      radius={[0, 4, 4, 0]}
                    />
                    <Bar
                      dataKey="maintAllHours"
                      name="Maint. (All)"
                      fill="#8b5cf6"
                      radius={[0, 4, 4, 0]}
                    />
                  </BarChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
                  No hours logged
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

   {/* Task Distribution by Assignee - Members Workload*/}
      {widgets.membersWorkload && (
      <Card>
        <CardHeader title="Members Workload" subtitle="" />
        <CardContent className="h-80">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={tasksByAssigneeData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" angle={-45} textAnchor="end" height={80} />
              <YAxis />
              <Tooltip />
              <Bar dataKey="completed" stackId="a" fill="#10b981" name="Completed" radius={[0, 0, 0, 0]} />
              <Bar dataKey="inProgress" stackId="a" fill="#3b82f6" name="In Progress" radius={[0, 0, 0, 0]} />
              <Bar dataKey="pending" stackId="a" fill="#f59e0b" name="Pending" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
        {workloadWarnings.length > 0 && (
          <div className="px-4 pb-4">
            <div className="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
              <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Workload concerns detected</p>
                <ul className="text-xs text-amber-700 dark:text-amber-300 mt-1 space-y-1">
                  {workloadWarnings.map(warning => (
                    <li key={warning.name}>
                      <span className="font-medium">{warning.name}</span>: {warning.issues.join(', ')}
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>
      )}

      {/* Team Sentiment */}
      {widgets.teamSentiment && (
        <SentimentInsights showTeamOverview={true} badge={ALL_SPRINTS_BADGE} />
      )}

      {/* Capacity Analysis */}
      {widgets.capacityAnalysis && (
        loadingStates.capacity ? (
          <Card>
            <CardHeader title="Capacity" />
            <CardContent>
              <div className="flex items-center justify-center h-64">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
              </div>
            </CardContent>
          </Card>
        ) : capacityAnalysis && dashboard && (capacityAnalysis.pastSprints.length > 0 || capacityAnalysis.currentSprint || capacityAnalysis.futureSprints.length > 0) ? (() => {
        // Calculate average completed SP from past sprints, falling back to current sprint if no history
        const avgCompletedSP = capacityAnalysis.pastSprints.length > 0
          ? Math.round(capacityAnalysis.pastSprints.reduce((sum, sprint) => sum + (sprint.completedPoints ?? 0), 0) / capacityAnalysis.pastSprints.length)
          : (capacityAnalysis.currentSprint?.completedPoints ?? 0);

        const totalMembers = dashboard.team.totalDirectReports;
        const avgUtilization = capacityAnalysis.averageUtilization ?? 0;

        // Predicted capacity stat = first future sprint's prediction from backend
        const predictedCapacity = capacityAnalysis.futureSprints.length > 0
          ? (capacityAnalysis.futureSprints[0].predictedPoints ?? 0)
          : 0;

        // Prepare chart data — predictions come from the backend on future sprints
        const chartData = [
          ...capacityAnalysis.pastSprints.map(s => ({ ...s, name: s.sprintName, predictedPoints: undefined as number | undefined, isPast: true, isCurrent: false, isFuture: false, isPredicted: false })),
          ...(capacityAnalysis.currentSprint ? [{ ...capacityAnalysis.currentSprint, name: capacityAnalysis.currentSprint.sprintName, predictedPoints: undefined as number | undefined, isPast: false, isCurrent: true, isFuture: false, isPredicted: false }] : []),
          ...capacityAnalysis.futureSprints.map(s => ({ ...s, name: s.sprintName, isPast: false, isCurrent: false, isFuture: true, isPredicted: false })),
        ];

        return (
          <Card>
            <CardHeader
              title="Capacity"
              subtitle={`Average Utilization: ${avgUtilization}%. Average Completed SP: ${avgCompletedSP}. Team Size: ${totalMembers}.`}
            />
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" angle={-15} textAnchor="end" height={80} tick={{ fontSize: 11 }} />
                  <YAxis label={{ value: 'Story Points', angle: -90, position: 'insideLeft' }} />
                  <Tooltip
                    formatter={(value: number, name: string) => [value, name]}
                    labelFormatter={(label) => `Sprint: ${label}`}
                  />
                  <Legend />
                  <ReferenceLine y={0} stroke="#000" />
                  <ReferenceLine
                    y={avgCompletedSP}
                    stroke="#f59e0b"
                    strokeDasharray="5 5"
                    strokeWidth={2}
                    label={{ value: `Avg SP: ${avgCompletedSP}`, position: 'right', fill: '#f59e0b', fontSize: 12 }}
                  />
                  <Line type="monotone" dataKey="committedPoints" stroke="#3b82f6" strokeWidth={2} name="Committed SP" dot={{ fill: '#3b82f6' }} />
                  <Line type="monotone" dataKey="completedPoints" stroke="#10b981" strokeWidth={2} name="Completed SP" dot={{ fill: '#10b981' }} />
                  <Line type="monotone" dataKey="predictedPoints" stroke="#8b5cf6" strokeWidth={2} strokeDasharray="5 5" name="Predicted SP" dot={{ fill: '#8b5cf6' }} connectNulls={false} />
                </LineChart>
              </ResponsiveContainer>
              <div className="mt-4 grid grid-cols-4 gap-4 pt-4 border-t dark:border-slate-700">
                <div className="text-center">
                  <p className="text-2xl font-bold text-slate-500">{capacityAnalysis.pastSprints.length}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Past Sprints</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-blue-500">{capacityAnalysis.currentSprint ? 1 : 0}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Current Sprint</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-purple-500">{capacityAnalysis.futureSprints.length}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Future Sprints</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-violet-500">{predictedCapacity} SP</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Predicted Capacity</p>
                </div>
              </div>
              {(capacityAnalysis.averageUtilization ?? 0) < 75 && (
                <div className="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
                  <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Low capacity utilization detected</p>
                    <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                      Average utilization is below 75%.
                    </p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        );
      })() : null
      )}

      {/* Team Velocity */}
      {widgets.teamVelocity && (
        loadingStates.velocity ? (
          <Card>
            <CardHeader title="Team Velocity" />
            <CardContent>
              <div className="flex items-center justify-center h-64">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
              </div>
            </CardContent>
          </Card>
        ) : velocity && velocity.sprints.length > 0 ? (
        <Card>
          <CardHeader
            title="Velocity"
            subtitle={`#completed_tasks #with_sp #no_support`}
          />
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={velocity.sprints.map(s => ({
                ...s,
                actualHours: Math.round(s.totalTimeSpentMinutes / 60 * 10) / 10,
                estimatedHours: s.totalEstimatedHours
              }))}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="sprintName" />
                <YAxis yAxisId="left" label={{ value: 'Story Points', angle: -90, position: 'insideLeft' }} />
                <YAxis yAxisId="right" orientation="right" label={{ value: 'Hours', angle: 90, position: 'insideRight' }} />
                <Tooltip />
                <Legend />
                <Bar
                  yAxisId="left"
                  dataKey="newStoryPointsCompleted"
                  stackId="sp"
                  fill="#3b82f6"
                  name="New SP"
                />
                <Bar
                  yAxisId="left"
                  dataKey="carriedOverStoryPoints"
                  stackId="sp"
                  fill="#94a3b8"
                  name="Carried Over SP"
                />
                <Bar
                  yAxisId="right"
                  dataKey="estimatedHours"
                  fill="#10b981"
                  name="Estimated Hours"
                  opacity={0.7}
                />
                <Bar
                  yAxisId="right"
                  dataKey="actualHours"
                  fill="#f59e0b"
                  name="Actual Hours"
                  opacity={0.7}
                />
              </BarChart>
            </ResponsiveContainer>
            <div className="mt-4 grid grid-cols-4 gap-4 pt-4 border-t dark:border-slate-700">
              <div className="text-center">
                <p className="text-2xl font-bold text-amber-500">{velocity.totalStoryPointsCompleted}</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total SP</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-blue-500">{velocity.averageVelocity}</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Avg Velocity (SP)</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-green-500">
                  {Math.round(velocity.sprints.reduce((sum, s) => sum + s.totalTimeSpentMinutes, 0) / 60)}h
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Hours Logged</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${velocity.completionTrend >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                  {velocity.completionTrend > 0 ? '+' : ''}{velocity.completionTrend}%
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Trend</p>
              </div>
            </div>
            {velocity.completionTrend < 0 && (
              <div className="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
                <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Negative velocity trend detected</p>
                  <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                    Team velocity is declining.
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
        ) : null
      )}

      {/* Estimation Accuracy */}
      {widgets.estimationAccuracy && (
        loadingStates.accuracy ? (
          <Card>
            <CardHeader title="Estimation Accuracy" />
            <CardContent>
              <div className="flex items-center justify-center h-64">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-amber-500"></div>
              </div>
            </CardContent>
          </Card>
        ) : accuracy && accuracy.sprints.length > 0 ? (() => {
          const supportOffset = includeSupportEstimate ? 56 : 0
          const adjustedEstimated = accuracy.totalEstimatedHours + supportOffset
          const adjustedVariance = accuracy.totalActualHours - adjustedEstimated
          const adjustedAccuracy = adjustedEstimated > 0
            ? Math.max(0, Math.round((100 - Math.abs(adjustedVariance * 100 / adjustedEstimated)) * 10) / 10)
            : 0
          return (
        <Card>
          <CardHeader
            title="Estimation Accuracy"
            subtitle={`#all_tasks`}
            action={
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={includeSupportEstimate}
                  onChange={(e) => setIncludeSupportEstimate(e.target.checked)}
                  className="rounded border-slate-300 dark:border-slate-600 text-amber-500 focus:ring-amber-500"
                />
                <span className="text-sm text-slate-600 dark:text-slate-400">+56h support estimate</span>
              </label>
            }
          />
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={accuracy.sprints}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="sprintName" />
                <YAxis yAxisId="left" label={{ value: 'Hours', angle: -90, position: 'insideLeft' }} />
                <YAxis yAxisId="right" orientation="right" domain={[0, 100]} label={{ value: 'Accuracy %', angle: 90, position: 'insideRight' }} />
                <Tooltip />
                <Legend />
                <Line yAxisId="left" type="monotone" dataKey="estimatedHours" stroke="#3b82f6" strokeWidth={2} name="Estimated hours" dot={{ fill: '#3b82f6' }} />
                <Line yAxisId="left" type="monotone" dataKey="actualHours" stroke="#10b981" strokeWidth={2} name="Actual hours" dot={{ fill: '#10b981' }} />
                <Line yAxisId="right" type="monotone" dataKey="accuracyPercentage" stroke="#f59e0b" strokeWidth={2} strokeDasharray="5 5" name="Accuracy %" dot={{ fill: '#f59e0b' }} />
              </LineChart>
            </ResponsiveContainer>
            <div className="mt-4 grid grid-cols-4 gap-4 pt-4 border-t dark:border-slate-700">
              <div className="text-center">
                <p className="text-2xl font-bold text-blue-500">{adjustedEstimated}h</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Estimated Hours</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-green-500">{accuracy.totalActualHours}h</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Actual Hours</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${adjustedVariance <= 0 ? 'text-green-500' : 'text-red-500'}`}>
                  {adjustedVariance > 0 ? '+' : ''}{adjustedVariance}h
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Variance</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${adjustedAccuracy >= 80 ? 'text-green-500' : adjustedAccuracy >= 60 ? 'text-amber-500' : 'text-red-500'}`}>
                  {adjustedAccuracy}%
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Accuracy</p>
              </div>
            </div>
            {accuracy.sprints.some(s => s.accuracyPercentage < 75) && (
              <div className="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-2">
                <AlertTriangle className="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Low estimation accuracy detected</p>
                  <p className="text-xs text-amber-700 dark:text-amber-300 mt-1">
                    Some sprints show accuracy below 75%.
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
          )
        })() : null
      )}
   
      {/* Customize Dashboard Modal */}
      {showCustomize && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-md mx-4">
            <CardHeader
              title="Customize Dashboard"
              subtitle="Choose which widgets to display"
              action={
                <button
                  onClick={closeCustomizeModal}
                  className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
                >
                  <X className="w-5 h-5 text-slate-500" />
                </button>
              }
            />
            <CardContent>
              <div className="space-y-3">
                {(Object.keys(widgets) as Array<keyof WidgetVisibility>).map((widget) => (
                  <button
                    key={widget}
                    onClick={() => toggleWidget(widget)}
                    className="flex items-center justify-between w-full p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                  >
                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
                      {WIDGET_LABELS[widget]}
                    </span>
                    {widgets[widget] ? (
                      <Eye className="w-5 h-5 text-green-500" />
                    ) : (
                      <EyeOff className="w-5 h-5 text-slate-400" />
                    )}
                  </button>
                ))}
              </div>
              <div className="mt-4 pt-4 border-t dark:border-slate-700 flex gap-3">
                <button
                  onClick={resetWidgets}
                  className="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                >
                  Reset to Default
                </button>
                <button
                  onClick={closeCustomizeModal}
                  className="flex-1 px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
                >
                  Done
                </button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Member Projects Modal */}
      {selectedMember && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-lg mx-4">
            <CardHeader
              title={`${selectedMember}'s Projects`}
              subtitle={`${memberProjects.length} project${memberProjects.length !== 1 ? 's' : ''}`}
              action={
                <button
                  onClick={() => setSelectedMember(null)}
                  className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
                >
                  <X className="w-5 h-5 text-slate-500" />
                </button>
              }
            />
            <CardContent>
              {memberProjects.length > 0 ? (
                <div className="space-y-3 max-h-80 overflow-y-auto">
                  {memberProjects.map(project => (
                    <div
                      key={project.id}
                      className="flex items-center gap-3 p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg"
                    >
                      <FolderKanban className="w-5 h-5 text-purple-500" />
                      <div className="flex-1">
                        <p className="font-medium text-slate-900 dark:text-slate-100">{project.name}</p>
                        {project.description && (
                          <p className="text-sm text-slate-500 dark:text-slate-400 line-clamp-1">
                            {project.description}
                          </p>
                        )}
                        {project.labels && (
                          <div className="flex gap-1 mt-1 flex-wrap">
                            {project.labels.split(',').slice(0, 3).map((label, idx) => (
                              <span
                                key={idx}
                                className="px-2 py-0.5 bg-slate-200 dark:bg-slate-600 text-slate-600 dark:text-slate-300 rounded text-xs"
                              >
                                {label.trim()}
                              </span>
                            ))}
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-slate-500 dark:text-slate-400 text-center py-4">
                  No projects found for this member
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* Action Items Modal */}
      {showActionItemsModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-2xl mx-4">
            <CardHeader
              title="1:1 Action Items"
              subtitle={`${actionItems.length} open item${actionItems.length !== 1 ? 's' : ''}${actionItems.filter(a => a.isOverdue).length > 0 ? ` (${actionItems.filter(a => a.isOverdue).length} overdue)` : ''}`}
              action={
                <button
                  onClick={closeActionItemsModal}
                  className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
                >
                  <X className="w-5 h-5 text-slate-500" />
                </button>
              }
            />
            <CardContent>
              {actionItems.length > 0 ? (
                <div className="space-y-3 max-h-96 overflow-y-auto">
                  {actionItems.map(item => (
                    <div
                      key={item.id}
                      className={`p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg ${item.isOverdue ? 'border-l-4 border-red-500' : ''}`}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex-1">
                          <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{item.content}</p>
                          <div className="flex items-center gap-3 mt-2 text-xs text-slate-500 dark:text-slate-400">
                            <span className="flex items-center gap-1">
                              <Users className="w-3 h-3" />
                              {item.directReportName}
                            </span>
                            {item.actionDueDate && (
                              <span className={`flex items-center gap-1 ${item.isOverdue ? 'text-red-500 font-medium' : ''}`}>
                                <Calendar className="w-3 h-3" />
                                {new Date(item.actionDueDate).toLocaleDateString()}
                                {item.isOverdue && ' (Overdue)'}
                              </span>
                            )}
                            {item.actionAssignee && (
                              <span>Assigned: {item.actionAssignee}</span>
                            )}
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          {item.isOverdue && (
                            <span className="px-2 py-1 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 text-xs rounded-full font-medium">
                              Overdue
                            </span>
                          )}
                          <button
                            onClick={() => handleCompleteActionItem(item.id)}
                            className="p-1.5 text-green-600 hover:bg-green-100 dark:hover:bg-green-900/30 rounded-lg transition-colors"
                            title="Mark as completed"
                          >
                            <Check className="w-4 h-4" />
                          </button>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-slate-500 dark:text-slate-400 text-center py-8">
                  No open action items from 1:1 meetings
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* Priority Notes Modal */}
      {showPriorityNotesModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <Card className="w-full max-w-2xl mx-4">
            <CardHeader
              title="Priorities"
              subtitle={`${priorityNotes.length} high priority item${priorityNotes.length !== 1 ? 's' : ''}${priorityNotes.filter(n => n.priority === 3).length > 0 ? ` (${priorityNotes.filter(n => n.priority === 3).length} urgent)` : ''}`}
              action={
                <button
                  onClick={closePriorityNotesModal}
                  className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded"
                >
                  <X className="w-5 h-5 text-slate-500" />
                </button>
              }
            />
            <CardContent>
              {priorityNotes.length > 0 ? (
                <div className="space-y-3 max-h-96 overflow-y-auto">
                  {priorityNotes.map(note => (
                    <div
                      key={note.id}
                      className={`p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg ${note.priority === 3 ? 'border-l-4 border-red-500' : 'border-l-4 border-amber-500'}`}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div
                          className="flex-1 cursor-pointer hover:opacity-80 transition-opacity"
                          onClick={() => {
                            closePriorityNotesModal()
                            navigate(`/notes?search=${encodeURIComponent(note.title)}`)
                          }}
                        >
                          <div className="flex items-center gap-2">
                            <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{note.title}</p>
                            <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${
                              note.priority === 3
                                ? 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
                                : 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400'
                            }`}>
                              {note.priorityName}
                            </span>
                          </div>
                          {note.content && (
                            <p className="text-sm text-slate-600 dark:text-slate-400 mt-1 line-clamp-2">
                              {note.content}
                            </p>
                          )}
                          <div className="flex items-center gap-3 mt-2 text-xs text-slate-500 dark:text-slate-400">
                            {note.dueDate && (
                              <span className={`flex items-center gap-1 ${note.isOverdue ? 'text-red-500 font-medium' : ''}`}>
                                <Calendar className="w-3 h-3" />
                                {new Date(note.dueDate).toLocaleDateString()}
                                {note.isOverdue && ' (Overdue)'}
                              </span>
                            )}
                            {note.tagsList && note.tagsList.length > 0 && (
                              <div className="flex gap-1">
                                {note.tagsList.slice(0, 2).map((tag, idx) => (
                                  <span key={idx} className="px-1.5 py-0.5 bg-slate-200 dark:bg-slate-600 rounded text-xs">
                                    {tag}
                                  </span>
                                ))}
                                {note.tagsList.length > 2 && (
                                  <span className="text-xs">+{note.tagsList.length - 2}</span>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                        <button
                          onClick={() => handleCompletePriorityNote(note.id)}
                          className="p-1.5 text-green-600 hover:bg-green-100 dark:hover:bg-green-900/30 rounded-lg transition-colors flex-shrink-0"
                          title="Mark as completed"
                        >
                          <Check className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-slate-500 dark:text-slate-400 text-center py-8">
                  No urgent or high priority TODO items found
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
