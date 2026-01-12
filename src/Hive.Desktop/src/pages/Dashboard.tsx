import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Users,
  CheckSquare,
  AlertTriangle,
  X,
  FolderKanban,
  ZapIcon,
  Settings2,
  Eye,
  EyeOff,
  ListTodo,
  Calendar,
  StickyNote,
  Check
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { reportsApi, tasksApi, projectsApi, leavesApi, meetingNotesApi, notesApi } from '../services/api'
import type { DashboardOverview, TeamTask, TeamVelocity, EstimationAccuracy, Project, CapacityAnalysis, TeamLeaveOverview, SprintCapacityAnalysis, MeetingNote, ManagerNote } from '../types'
import { useEscapeKey } from '../hooks/useEscapeKey'
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

const DASHBOARD_WIDGETS_KEY = 'hive-dashboard-widgets'

interface WidgetVisibility {
  topStats: boolean
  projectsDistribution: boolean
  membersByProject: boolean
  tasksDistribution: boolean
  capacityAnalysis: boolean
  estimationAccuracy: boolean
  teamVelocity: boolean
  membersWorkload: boolean
}

const DEFAULT_WIDGETS: WidgetVisibility = {
  topStats: true,
  projectsDistribution: true,
  membersByProject: true,
  tasksDistribution: true,
  capacityAnalysis: true,
  estimationAccuracy: true,
  teamVelocity: true,
  membersWorkload: true
}

const WIDGET_LABELS: Record<keyof WidgetVisibility, string> = {
  topStats: 'Top Stats',
  projectsDistribution: 'Projects Distribution',
  membersByProject: 'Members by Project',
  tasksDistribution: 'Tasks Distribution',
  capacityAnalysis: 'Capacity Analysis',
  estimationAccuracy: 'Estimation Accuracy',
  teamVelocity: 'Team Velocity',
  membersWorkload: 'Members Workload'
}

export default function Dashboard() {
  const navigate = useNavigate()
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null)
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [velocity, setVelocity] = useState<TeamVelocity | null>(null)
  const [accuracy, setAccuracy] = useState<EstimationAccuracy | null>(null)
  const [capacityAnalysis, setCapacityAnalysis] = useState<CapacityAnalysis | null>(null)
  const [leaveOverview, setLeaveOverview] = useState<TeamLeaveOverview | null>(null)
  const [actionItems, setActionItems] = useState<MeetingNote[]>([])
  const [showActionItemsModal, setShowActionItemsModal] = useState(false)
  const [priorityNotes, setPriorityNotes] = useState<ManagerNote[]>([])
  const [showPriorityNotesModal, setShowPriorityNotesModal] = useState(false)
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
      const [dashboardData, tasksData, projectsData, leaveData, actionItemsData, notesData] = await Promise.all([
        reportsApi.getDashboard(sprintFilter),
        tasksApi.getAll(),
        projectsApi.getAll(),
        leavesApi.getOverview(),
        meetingNotesApi.getOpenActionItems(),
        notesApi.getPending()
      ])
      setDashboard(dashboardData)
      setTasks(tasksData.items)
      setProjects(projectsData)
      setLeaveOverview(leaveData)
      // Sort action items by due date ascending (earliest first)
      const sortedActionItems = actionItemsData.sort((a, b) => {
        if (!a.actionDueDate && !b.actionDueDate) return 0
        if (!a.actionDueDate) return 1
        if (!b.actionDueDate) return -1
        return new Date(a.actionDueDate).getTime() - new Date(b.actionDueDate).getTime()
      })
      setActionItems(sortedActionItems)
      // Filter for urgent (3) and high (2) priority notes, sort by priority descending
      const highPriorityNotes = notesData
        .filter((note: ManagerNote) => note.priority >= 2)
        .sort((a: ManagerNote, b: ManagerNote) => b.priority - a.priority)
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
    }
  }

  const handleCompletePriorityNote = async (noteId: string) => {
    try {
      await notesApi.toggle(noteId)
      // Remove the completed item from the list
      setPriorityNotes(prev => prev.filter(item => item.id !== noteId))
    } catch (err) {
      console.error('Failed to complete priority note:', err)
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
      value: projectIds.size
    }))
    .sort((a, b) => b.value - a.value)
    .filter(d => d.value > 0)

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

  const siloCount = membersDistributionData.filter(d => d.isSilo).length

  const taskTypeData = dashboard.tasks.tasksByType.map(type => ({
    name: type.typeName,
    value: type.totalTasks,
    completed: type.completedTasks
  })).filter(d => d.value > 0)

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
          title="Active Projects"
          value={dashboard.tasks.projects.activeProjects}
          subtitle={`${dashboard.tasks.projects.totalProjects} total`}
          icon={<FolderKanban className="w-6 h-6" />}
          color="purple"
          onClick={() => navigate('/projects?filter=active')}
        />
         {capacityAnalysis?.currentSprint && (
          <StatCard
            title={'Current Sprint' + (capacityAnalysis.currentSprint.sprintName ? `: ${capacityAnalysis.currentSprint.sprintName}` : '')}
            value={`${capacityAnalysis.currentSprint.utilizationPercentage ?? 0}%`}
            subtitle={`${capacityAnalysis.currentSprint.completedPoints ?? 0}/${capacityAnalysis.currentSprint.committedPoints ?? 0} SP`}
            icon={<ZapIcon className="w-6 h-6" />}
            color={(capacityAnalysis.currentSprint.utilizationPercentage ?? 0) > 100 ? 'red' : (capacityAnalysis.currentSprint.utilizationPercentage ?? 0) > 80 ? 'amber' : 'blue'}
            onClick={() => navigate('/sprints')}
          />
        )}
        <StatCard
          title="Task Completion"
          value={`${dashboard.tasks.tasks.completionRate}%`}
          subtitle={`${dashboard.tasks.tasks.doneTasks} of ${dashboard.tasks.tasks.totalTasks} tasks`}
          icon={<CheckSquare className="w-6 h-6" />}
          color="green"
          onClick={() => navigate('/tasks')}
        />
        <StatCard
          title="Overdue Tasks"
          value={dashboard.tasks.tasks.overdueTasks}
          icon={<AlertTriangle className="w-6 h-6" />}
          color={dashboard.tasks.tasks.overdueTasks > 0 ? 'red' : 'green'}
          onClick={() => navigate('/tasks?filter=overdue')}
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
          title="Priority Notes"
          value={priorityNotes.length}
          subtitle={priorityNotes.filter(n => n.priority === 3).length > 0 ? `${priorityNotes.filter(n => n.priority === 3).length} urgent` : priorityNotes.length > 0 ? `${priorityNotes.filter(n => n.priority === 2).length} high` : undefined}
          icon={<StickyNote className="w-6 h-6" />}
          color={priorityNotes.some(n => n.priority === 3) ? 'red' : priorityNotes.length > 0 ? 'amber' : 'green'}
          onClick={() => setShowPriorityNotesModal(true)}
        />

      </div>
      )}

      {/* Distribution Charts Row - 3 columns */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Projects Distribution by Member */}
        {widgets.projectsDistribution && (
        <Card>
          <CardHeader title="Projects Distribution" subtitle="By member" />
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
                    label={({ name, value }) => `${name}: ${value}`}
                    onClick={(data) => handleMemberClick(data.name)}
                    style={{ cursor: 'pointer' }}
                  >
                    {projectDistributionData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-full text-slate-500 dark:text-slate-400">
                No project assignments found
              </div>
            )}
          </CardContent>
        </Card>
        )}

        {/* Members Distribution by Project - Knowledge Silos */}
        {widgets.membersByProject && (
        <Card>
          <CardHeader
            title="Members by Project"
            subtitle={siloCount > 0 ? `${siloCount} potential silo${siloCount !== 1 ? 's' : ''}` : 'No silos detected'}
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
          <CardHeader title="Tasks Distribution" subtitle="By type" />
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
                    label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                  >
                    {taskTypeData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
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
      </div>

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
                  name="Story Points"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="estimatedHours"
                  stroke="#3b82f6"
                  strokeWidth={2}
                  strokeDasharray="5 5"
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
                <Line yAxisId="right" type="monotone" dataKey="accuracyPercentage" stroke="#f59e0b" strokeWidth={2} name="Accuracy %" dot={{ fill: '#f59e0b' }} />
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
      </Card>
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
              title="Priority Notes & TODOs"
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
                  No urgent or high priority notes
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
