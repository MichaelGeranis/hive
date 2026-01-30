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
  TrendingUp
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { SentimentInsights } from '../components/SentimentInsights'
import { reportsApi, tasksApi, projectsApi, leavesApi, meetingNotesApi, notesApi, sprintsApi, sprintCapacityApi, directReportsApi, parentsApi, knowledgePointsApi, projectKnowledgeApi } from '../services/api'
import type { DashboardOverview, TeamTask, TeamVelocity, EstimationAccuracy, Project, CapacityAnalysis, TeamLeaveOverview, SprintCapacityAnalysis, MeetingNote, ManagerNote, Sprint, SprintCapacity, DirectReport, Parent, KnowledgeLevelSuggestion } from '../types'
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

// Get initials from a full name (e.g., "John Doe" -> "JD")
const getInitials = (name: string): string => {
  return name
    .split(' ')
    .map(part => part.charAt(0).toUpperCase())
    .join('')
}

// Custom pie label renderer with smaller font (10% smaller = ~11px from default 12px)
const RADIAN = Math.PI / 180
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const createPieLabel = (formatter: (props: any) => string) => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return (props: any) => {
    const { cx, cy, midAngle, outerRadius, fill } = props
    const radius = outerRadius * 1.35
    const x = cx + radius * Math.cos(-midAngle * RADIAN)
    const y = cy + radius * Math.sin(-midAngle * RADIAN)
    return (
      <text x={x} y={y} fill={fill} textAnchor={x > cx ? 'start' : 'end'} dominantBaseline="central" fontSize={11}>
        {formatter(props)}
      </text>
    )
  }
}

const DASHBOARD_WIDGETS_KEY = 'hive-dashboard-widgets'

interface SprintCapacitySuggestion {
  sprint: Sprint
  currentCapacity: SprintCapacity | null
  peopleOnLeave: number
  totalTeamSize: number
  suggestedAvailableMembers: number
  leaveDaysInSprint: number
  affectedMembers: Set<string>
}

interface WidgetVisibility {
  topStats: boolean
  projectsDistribution: boolean
  membersByProject: boolean
  tasksDistribution: boolean
  tasksDistributionSP: boolean
  tasksDistributionHours: boolean
  supportDistribution: boolean
  teamSentiment: boolean
  capacityAnalysis: boolean
  sprintCapacitySuggestions: boolean
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
  supportDistribution: true,
  teamSentiment: true,
  capacityAnalysis: true,
  sprintCapacitySuggestions: true,
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
  supportDistribution: 'Support Distribution',
  teamSentiment: 'Team Sentiment',
  capacityAnalysis: 'Capacity Analysis',
  sprintCapacitySuggestions: 'Sprint Capacity Suggestions',
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
  const [capacityAnalysis, setCapacityAnalysis] = useState<CapacityAnalysis | null>(null)
  const [leaveOverview, setLeaveOverview] = useState<TeamLeaveOverview | null>(null)
  const [sprints, setSprints] = useState<Sprint[]>([])
  const [sprintCapacities, setSprintCapacities] = useState<SprintCapacity[]>([])
  const [leaves, setLeaves] = useState<{ id: string; directReportId: string; startDate: string; endDate: string }[]>([])
  const [directReports, setDirectReports] = useState<DirectReport[]>([])
  const [parents, setParents] = useState<Parent[]>([])
  const [actionItems, setActionItems] = useState<MeetingNote[]>([])
  const [showActionItemsModal, setShowActionItemsModal] = useState(false)
  const [priorityNotes, setPriorityNotes] = useState<ManagerNote[]>([])
  const [showPriorityNotesModal, setShowPriorityNotesModal] = useState(false)
  const [knowledgeSuggestions, setKnowledgeSuggestions] = useState<KnowledgeLevelSuggestion[]>([])
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

  // Lazy load velocity data when widget becomes visible
  useEffect(() => {
    if (widgets.teamVelocity && !velocity && !loadingStates.velocity) {
      loadVelocityData()
    }
  }, [widgets.teamVelocity, sprintFilter])

  // Lazy load accuracy data when widget becomes visible
  useEffect(() => {
    if (widgets.estimationAccuracy && !accuracy && !loadingStates.accuracy) {
      loadAccuracyData()
    }
  }, [widgets.estimationAccuracy, sprintFilter])

  // Lazy load capacity data when widget becomes visible
  useEffect(() => {
    if (widgets.capacityAnalysis && !capacityAnalysis && !loadingStates.capacity) {
      loadCapacityData()
    }
  }, [widgets.capacityAnalysis, sprintFilter])

  const loadCoreData = async () => {
    try {
      setLoading(true)
      const [dashboardData, tasksData, projectsData, leaveData, actionItemsData, notesData, sprintsData, capacitiesData, leavesData, directReportsData, parentsData, knowledgeSuggestionsData] = await Promise.all([
        reportsApi.getDashboard(sprintFilter),
        tasksApi.getAll(),
        projectsApi.getAll(),
        leavesApi.getOverview(),
        meetingNotesApi.getOpenActionItems(),
        notesApi.getPending(),
        sprintsApi.getAll(),
        sprintCapacityApi.getAll(),
        leavesApi.getAll(),
        directReportsApi.getAll(),
        parentsApi.getAll(),
        knowledgePointsApi.getSuggestions()
      ])
      setDashboard(dashboardData)
      setTasks(tasksData.items)
      setProjects(projectsData)
      setLeaveOverview(leaveData)
      const sortedSprints = sprintsData.sort((a: Sprint, b: Sprint) => {
        const aSort = a.year * 1000 + a.quarter * 100 + a.sprintNumber
        const bSort = b.year * 1000 + b.quarter * 100 + b.sprintNumber
        return bSort - aSort
      })
      setSprints(sortedSprints)
      setSprintCapacities(capacitiesData)
      setLeaves(leavesData)
      setDirectReports(directReportsData)
      setParents(parentsData)
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

  // Helper function to calculate working days between two dates (excluding weekends)
  const getWorkingDays = (start: Date, end: Date): number => {
    let count = 0
    const current = new Date(start)
    while (current <= end) {
      const dayOfWeek = current.getDay()
      if (dayOfWeek !== 0 && dayOfWeek !== 6) {
        count++
      }
      current.setDate(current.getDate() + 1)
    }
    return count
  }

  // Month name to number mapping
  const monthMap: Record<string, number> = {
    'JAN': 0, 'FEB': 1, 'MAR': 2, 'APR': 3, 'MAY': 4, 'JUN': 5,
    'JUL': 6, 'AUG': 7, 'SEP': 8, 'OCT': 9, 'NOV': 10, 'DEC': 11
  }

  // Helper to get sprint start date from dates, name parsing, or year/quarter/sprintNumber
  const getSprintStartDate = (sprint: Sprint): Date => {
    // 1. Use actual startDate if available
    if (sprint.startDate) {
      return new Date(sprint.startDate)
    }

    // 2. Try to parse from name format like "MAR_1Q26_S1" or "MAR_Q1_26_S1"
    const nameMatch = sprint.name.match(/^([A-Z]{3})_(\d)?Q(\d{2})_S(\d+)$/i)
    if (nameMatch) {
      const monthStr = nameMatch[1].toUpperCase()
      const year = 2000 + parseInt(nameMatch[3], 10)
      const sprintNum = parseInt(nameMatch[4], 10)
      const month = monthMap[monthStr]

      if (month !== undefined) {
        // Estimate day based on sprint number within the month (each sprint ~2 weeks)
        const day = 1 + ((sprintNum - 1) % 2) * 14
        return new Date(year, month, day)
      }
    }

    // 3. Fallback: calculate from year, quarter, sprintNumber
    // Quarter start month: Q1=Jan(0), Q2=Apr(3), Q3=Jul(6), Q4=Oct(9)
    const quarterStartMonth = (sprint.quarter - 1) * 3
    // Each sprint is ~2 weeks, so sprint 1 starts day 1, sprint 2 starts day 15, etc.
    const dayOfQuarter = 1 + (sprint.sprintNumber - 1) * 14
    return new Date(sprint.year, quarterStartMonth, dayOfQuarter)
  }

  // Sprint capacity suggestions calculation
  const calculateSprintSuggestions = (): SprintCapacitySuggestion[] => {
    if (!dashboard) return []

    const today = new Date()
    const threeMonthsLater = new Date(today.getFullYear(), today.getMonth() + 3, today.getDate())

    // Only consider direct reports (not indirect reports)
    const directReportIds = new Set(
      directReports.filter(dr => dr.isDirect).map(dr => dr.id)
    )

    // Filter upcoming sprints, sort chronologically (earliest first), then apply limit
    let upcomingSprints = sprints
      .filter(sprint => {
        const sprintDate = getSprintStartDate(sprint)
        return sprintDate >= today && sprintDate <= threeMonthsLater
      })
      .sort((a, b) => {
        // Sort by actual date (earliest first)
        return getSprintStartDate(a).getTime() - getSprintStartDate(b).getTime()
      })

    // Apply sprint filter limit if set (now correctly gets the nearest N sprints)
    if (sprintFilter !== undefined) {
      upcomingSprints = upcomingSprints.slice(0, sprintFilter)
    }

    return upcomingSprints.map(sprint => {
      // Determine sprint start and end dates
      let sprintStart: Date
      let sprintEnd: Date
      let workingDaysInSprint: number

      if (sprint.startDate && sprint.endDate) {
        // Use actual sprint dates
        sprintStart = new Date(sprint.startDate)
        sprintEnd = new Date(sprint.endDate)
        workingDaysInSprint = getWorkingDays(sprintStart, sprintEnd)
      } else {
        // Use the same logic as getSprintStartDate for consistency
        sprintStart = getSprintStartDate(sprint)
        sprintEnd = new Date(sprintStart)
        sprintEnd.setDate(sprintEnd.getDate() + 13) // 2 weeks minus 1 day
        workingDaysInSprint = 9
      }

      const affectedMembers = new Set<string>()
      let totalLeaveDays = 0

      // Only count leaves from direct reports, counting only working days
      leaves.filter(leave => directReportIds.has(leave.directReportId)).forEach(leave => {
        const leaveStart = new Date(leave.startDate)
        const leaveEnd = new Date(leave.endDate)

        if (leaveStart <= sprintEnd && leaveEnd >= sprintStart) {
          affectedMembers.add(leave.directReportId)

          // Calculate overlap period
          const overlapStart = leaveStart > sprintStart ? leaveStart : sprintStart
          const overlapEnd = leaveEnd < sprintEnd ? leaveEnd : sprintEnd

          // Count only working days in the overlap
          const workingLeaveDays = getWorkingDays(overlapStart, overlapEnd)
          totalLeaveDays += workingLeaveDays
        }
      })

      const currentCapacity = sprintCapacities.find(c => c.sprintId === sprint.id) || null
      const totalTeamSize = dashboard.team.totalDirectReports

      // Calculate lost capacity based on proportion of leave days
      // Example: 2 leave days in a 9-day sprint = 2/9 = 0.22 people lost
      const lostCapacity = workingDaysInSprint > 0 ? totalLeaveDays / workingDaysInSprint : 0
      const suggestedAvailableMembers = Math.max(0, Math.floor(totalTeamSize - lostCapacity))

      return {
        sprint,
        currentCapacity,
        peopleOnLeave: affectedMembers.size,
        totalTeamSize,
        suggestedAvailableMembers,
        leaveDaysInSprint: totalLeaveDays,
        affectedMembers
      }
    }).filter(suggestion => suggestion.leaveDaysInSprint > 0) // Only show sprints with leave impact
  }

  const sprintSuggestions = calculateSprintSuggestions()

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

  // Calculate total SP for current sprint from parents involved in the sprint
  const currentSprintParentIds = capacityAnalysis?.currentSprint
    ? new Set(
        tasks
          .filter(t => t.sprint === capacityAnalysis.currentSprint?.sprintName && t.parentId)
          .map(t => t.parentId)
      )
    : new Set<string>()
  const currentSprintTotalSP = parents
    .filter(p => currentSprintParentIds.has(p.id))
    .reduce((sum, p) => sum + (p.totalStoryPoints ?? 0), 0)

  // Calculate tasks not matched to any project (no labels or labels don't match any project)
  const unmatchedTasks = tasks.filter(task => {
    const matchedProjects = getMatchedProjects(task.labels)
    return matchedProjects.length === 0
  })
  const unmatchedTaskCount = unmatchedTasks.length

  // Use backend-computed support distribution
  const { supportDistribution } = dashboard.tasks
  const supportByAssigneeData = supportDistribution.byAssignee
    .map(a => ({ name: a.assigneeName, completedHours: a.completedHours, allHours: a.allHours, completedTasks: a.completedTaskCount, allTasks: a.allTaskCount }))

  // Support hours data for pie chart
  const supportComparisonData = [
    { name: 'Completed', hours: supportDistribution.completedHours },
    { name: 'All Tasks', hours: supportDistribution.allHours },
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
    // Sprint capacity suggestions needing adjustment
    const suggestionsNeedingAdjustment = sprintSuggestions.filter(s =>
      s.currentCapacity && s.currentCapacity.availableMembers !== s.suggestedAvailableMembers
    )
    count += suggestionsNeedingAdjustment.length
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
        <button
          onClick={() => setShowCustomize(true)}
          className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
        >
          <Settings2 className="w-4 h-4" />
          Customize
        </button>
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
          color="amber"
          onClick={() => navigate('/team')}
        />
        <StatCard
          title="Projects"
          value={dashboard.tasks.projects.totalProjects}
          icon={<FolderKanban className="w-6 h-6" />}
          color="purple"
          onClick={() => navigate('/projects?filter=active')}
        />
        <StatCard
          title="1:1 Action Items"
          value={actionItems.length}
          subtitle={actionItems.filter(a => a.isOverdue).length > 0 ? `${actionItems.filter(a => a.isOverdue).length} overdue` : undefined}
          icon={<ListTodo className="w-6 h-6" />}
          color={actionItems.some(a => a.isOverdue) ? 'red' : 'blue'}
          onClick={() => setShowActionItemsModal(true)}
        />
        <StatCard
          title="TODOs"
          value={priorityNotes.length}
          subtitle={priorityNotes.filter(n => n.priority === 3).length > 0 ? `${priorityNotes.filter(n => n.priority === 3).length} urgent` : priorityNotes.length > 0 ? `${priorityNotes.filter(n => n.priority === 2).length} high` : undefined}
          icon={<StickyNote className="w-6 h-6" />}
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
              <p className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                {capacityAnalysis?.currentSprint?.sprintName || 'Current Sprint'}
              </p>
              <div className="flex flex-wrap items-center gap-6">
                {/* SP Progress */}
                <div className="flex items-center gap-2">
                  <span className="text-2xl font-bold text-blue-600 dark:text-blue-400">{capacityAnalysis?.currentSprint?.completedPoints ?? 0}/{currentSprintTotalSP}</span>
                  <span className="text-sm text-slate-500 dark:text-slate-400">SP {currentSprintTotalSP > 0 ? Math.round((capacityAnalysis?.currentSprint?.completedPoints ?? 0) / currentSprintTotalSP * 100) : 0}%</span>
                </div>
                <div className="hidden sm:block w-px h-8 bg-slate-200 dark:bg-slate-700" />
                {/* Tasks Count */}
                <div className="flex items-center gap-2">
                  <span className="text-2xl font-bold text-purple-600 dark:text-purple-400">{dashboard.tasks.tasks.doneTasks}/{dashboard.tasks.tasks.totalTasks}</span>
                  <span className="text-sm text-slate-500 dark:text-slate-400">Tasks {dashboard.tasks.tasks.completionRate}%</span>
                </div>
                {/* Warnings indicators */}
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
          <CardHeader title="Projects Distribution" subtitle="How many projects each team member is engaged in" />
          <CardContent className="h-64">
            {projectDistributionData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={projectDistributionData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={70}
                    paddingAngle={5}
                    dataKey="value"
                    label={createPieLabel(({ initials, value }) => `${initials}: ${value}`)}
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
            ) : (
              <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
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
          <CardContent className="h-64">
            {taskTypeData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={taskTypeData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={70}
                    paddingAngle={5}
                    dataKey="value"
                    label={createPieLabel(({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`)}
                  >
                    {taskTypeData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={getTaskTypeColor(entry.name)} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
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
          <CardContent className="h-64">
            {taskTypeSPChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={taskTypeSPChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={70}
                    paddingAngle={5}
                    dataKey="value"
                    label={createPieLabel(({ name, value, percent }) => `${name}: ${value} SP (${(percent * 100).toFixed(0)}%)`)}
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
            ) : (
              <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
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
          <CardContent className="h-64">
            {taskTypeHoursChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={taskTypeHoursChartData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={70}
                    paddingAngle={5}
                    dataKey="value"
                    label={createPieLabel(({ name, value, percent }) => `${name}: ${value}h (${(percent * 100).toFixed(0)}%)`)}
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
            ) : (
              <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
                No hours logged
              </div>
            )}
          </CardContent>
        </Card>
        )}
      </div>

      {/* Row 4: Support Distribution */}
      {widgets.supportDistribution && supportDistribution.allHours > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Support Hours */}
          <Card>
            <CardHeader
              title="Support Hours"
              subtitle={`${supportDistribution.allTaskCount} support tasks (${supportDistribution.completedTaskCount} completed)`}
            />
            <CardContent className="h-64">
              {supportComparisonData.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={supportComparisonData}
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={70}
                      paddingAngle={5}
                      dataKey="hours"
                      label={createPieLabel(({ name, hours }) => `${name}: ${hours}h`)}
                    >
                      <Cell fill="#22c55e" />
                      <Cell fill="#ef4444" />
                    </Pie>
                    <Tooltip
                      formatter={(value: number) => [`${value}h`, 'Hours']}
                      labelFormatter={(name) => name}
                    />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
                  No support hours logged
                </div>
              )}
            </CardContent>
            <div className="px-4 pb-4">
              <div className="grid grid-cols-2 gap-4 text-center">
                <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <p className="text-2xl font-bold text-green-600 dark:text-green-400">{supportDistribution.completedHours}h</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{supportDistribution.completedTaskCount} Completed</p>
                </div>
                <div className="p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
                  <p className="text-2xl font-bold text-red-600 dark:text-red-400">{supportDistribution.allHours}h</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{supportDistribution.allTaskCount} All tasks</p>
                </div>
              </div>
            </div>
          </Card>

          {/* Support Hours by Assignee */}
          <Card>
            <CardHeader
              title="Support Hours by Assignee"
              subtitle={`${supportDistribution.allHours}h total (${supportDistribution.completedHours}h completed)`}
            />
            <CardContent className="h-64">
              {supportByAssigneeData.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={supportByAssigneeData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" unit="h" />
                    <YAxis type="category" dataKey="name" width={100} tick={{ fontSize: 11 }} />
                    <Tooltip
                      formatter={(value: number, name: string) => [`${value}h`, name]}
                    />
                    <Bar
                      dataKey="completedHours"
                      name="Completed"
                      fill="#22c55e"
                      radius={[0, 4, 4, 0]}
                    />
                    <Bar
                      dataKey="allHours"
                      name="All Tasks"
                      fill="#ef4444"
                      radius={[0, 4, 4, 0]}
                    />
                  </BarChart>
                </ResponsiveContainer>
              ) : (
                <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
                  No support hours logged
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
        <SentimentInsights showTeamOverview={true} />
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
        ) : capacityAnalysis && dashboard && leaveOverview && (capacityAnalysis.pastSprints.length > 0 || capacityAnalysis.currentSprint || capacityAnalysis.futureSprints.length > 0) ? (() => {
        // Calculate average completed SP from past sprints
        const avgCompletedSP = capacityAnalysis.pastSprints.length > 0
          ? Math.round(capacityAnalysis.pastSprints.reduce((sum, sprint) => sum + (sprint.completedPoints ?? 0), 0) / capacityAnalysis.pastSprints.length)
          : 0;

        // Calculate available team members (total - on leave today)
        const totalMembers = dashboard.team.totalDirectReports;
        const membersOnLeave = leaveOverview.teamMembersOnLeaveToday;
        const availableMembers = totalMembers - membersOnLeave;
        const availabilityRatio = totalMembers > 0 ? availableMembers / totalMembers : 1;

        // Calculate predicted capacity for next sprint
        const predictedCapacity = Math.round(avgCompletedSP * availabilityRatio);

        // Create a predicted future sprint
        const predictedSprint: SprintCapacityAnalysis = {
          sprintId: 'predicted',
          sprintName: 'Next Sprint (Predicted)',
          year: new Date().getFullYear(),
          quarter: Math.floor(new Date().getMonth() / 3) + 1,
          sprintNumber: 99,
          committedPoints: 0,
          completedPoints: predictedCapacity,
          utilizationPercentage: 0,
          status: 'Future'
        };

        // Prepare chart data with color coding
        const chartData = [
          ...capacityAnalysis.pastSprints.map(s => ({ ...s, name: s.sprintName, isPast: true, isCurrent: false, isFuture: false, isPredicted: false })),
          ...(capacityAnalysis.currentSprint ? [{ ...capacityAnalysis.currentSprint, name: capacityAnalysis.currentSprint.sprintName, isPast: false, isCurrent: true, isFuture: false, isPredicted: false }] : []),
          ...capacityAnalysis.futureSprints.map(s => ({ ...s, name: s.sprintName, isPast: false, isCurrent: false, isFuture: true, isPredicted: false })),
          { ...predictedSprint, name: predictedSprint.sprintName, isPast: false, isCurrent: false, isFuture: false, isPredicted: true }
        ];

        return (
          <Card>
            <CardHeader
              title="Capacity"
              subtitle={`Average Utilization: ${capacityAnalysis.averageUtilization ?? 0}%. Average Completed SP: ${avgCompletedSP}. Available Members: ${availableMembers}/${totalMembers} (${membersOnLeave} on leave).`}
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

      {/* Sprint Capacity Suggestions - Shows warnings when they exist */}
      {widgets.sprintCapacitySuggestions && (() => {
        const suggestionsNeedingAdjustment = sprintSuggestions.filter(suggestion =>
          suggestion.currentCapacity &&
          suggestion.currentCapacity.availableMembers !== suggestion.suggestedAvailableMembers
        )

        if (suggestionsNeedingAdjustment.length === 0) {
          return null
        }

        return (
          <Card>
            <CardHeader
              title="Sprint Capacity Suggestions"
              subtitle="Based on upcoming leaves, these sprints need capacity adjustments"
            />
            <CardContent>
              <div className="space-y-3">
                {suggestionsNeedingAdjustment.map(suggestion => (
                  <div
                    key={suggestion.sprint.id}
                    className="p-4 rounded-lg border bg-amber-50 dark:bg-amber-900/20 border-amber-300 dark:border-amber-700"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-3">
                          <h3 className="font-semibold text-slate-900 dark:text-slate-100">
                            {suggestion.sprint.name}
                          </h3>
                          <div className="flex items-center gap-1 text-amber-600 dark:text-amber-400">
                            <AlertTriangle className="w-4 h-4" />
                            <span className="text-xs font-medium">Needs Adjustment</span>
                          </div>
                        </div>
                        <div className="mt-2 grid grid-cols-4 gap-4 text-sm">
                          <div>
                            <span className="text-slate-500 dark:text-slate-400">Team Size:</span>
                            <span className="ml-2 font-medium text-slate-900 dark:text-slate-100">{suggestion.totalTeamSize}</span>
                          </div>
                          <div>
                            <span className="text-slate-500 dark:text-slate-400">On Leave:</span>
                            <span className="ml-2 font-medium text-orange-600 dark:text-orange-400">{suggestion.peopleOnLeave}</span>
                          </div>
                          <div>
                            <span className="text-slate-500 dark:text-slate-400">Current Capacity:</span>
                            <span className="ml-2 font-medium text-slate-900 dark:text-slate-100">
                              {suggestion.currentCapacity?.availableMembers || 'Not set'}
                            </span>
                          </div>
                          <div>
                            <span className="text-slate-500 dark:text-slate-400">Suggested:</span>
                            <span className="ml-2 font-semibold text-green-600 dark:text-green-400">
                              {suggestion.suggestedAvailableMembers} people
                            </span>
                          </div>
                        </div>
                        {suggestion.leaveDaysInSprint > 0 && (
                          <div className="mt-2 text-xs text-slate-600 dark:text-slate-400">
                            Total leave days in sprint: {suggestion.leaveDaysInSprint} days
                          </div>
                        )}
                      </div>
                      <button
                        onClick={() => navigate('/sprints')}
                        className="ml-4 px-3 py-1.5 text-sm bg-amber-500 text-white rounded hover:bg-amber-600 transition-colors"
                      >
                        Update in Sprints
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )
      })()}

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
            subtitle={`from completed tasks`}
          />
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={velocity.sprints.map(s => ({
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
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="storyPointsCompleted"
                  stroke="#f59e0b"
                  strokeWidth={2}
                  strokeDasharray="5 5"
                  name="Story Points"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="estimatedHours"
                  stroke="#3b82f6"
                  strokeWidth={2}
                  name="Estimated Hours"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="actualHours"
                  stroke="#10b981"
                  strokeWidth={2}
                  name="Actual Hours"
                />
              </LineChart>
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
        ) : accuracy && accuracy.sprints.length > 0 ? (
        <Card>
          <CardHeader
            title="Estimation Accuracy"
            subtitle={`from all tasks`}
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
                <p className="text-2xl font-bold text-blue-500">{accuracy.totalEstimatedHours}h</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Estimated Hours</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-green-500">{accuracy.totalActualHours}h</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Actual Hours</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${accuracy.totalVarianceHours <= 0 ? 'text-green-500' : 'text-red-500'}`}>
                  {accuracy.totalVarianceHours > 0 ? '+' : ''}{accuracy.totalVarianceHours}h
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Variance</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${accuracy.overallAccuracyPercentage >= 80 ? 'text-green-500' : accuracy.overallAccuracyPercentage >= 60 ? 'text-amber-500' : 'text-red-500'}`}>
                  {accuracy.overallAccuracyPercentage}%
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
        ) : null
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
