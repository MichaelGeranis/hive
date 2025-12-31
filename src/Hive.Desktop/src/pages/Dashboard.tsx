import { useEffect, useState, useCallback } from 'react'
import {
  Users,
  CheckSquare,
  AlertTriangle,
  Target,
  X,
  FolderKanban
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { reportsApi, tasksApi, projectsApi } from '../services/api'
import type { DashboardOverview, TeamTask, TeamVelocity, EstimationAccuracy, Project } from '../types'
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
  Legend
} from 'recharts'

const COLORS = ['#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ef4444']

export default function Dashboard() {
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null)
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [velocity, setVelocity] = useState<TeamVelocity | null>(null)
  const [accuracy, setAccuracy] = useState<EstimationAccuracy | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedMember, setSelectedMember] = useState<string | null>(null)
  const [memberProjects, setMemberProjects] = useState<Project[]>([])

  const closeModal = useCallback(() => setSelectedMember(null), [])
  useEscapeKey(closeModal, !!selectedMember)

  useEffect(() => {
    loadDashboard()
  }, [])

  const loadDashboard = async () => {
    try {
      setLoading(true)
      const [dashboardData, tasksData, projectsData, velocityData, accuracyData] = await Promise.all([
        reportsApi.getDashboard(),
        tasksApi.getAll(),
        projectsApi.getAll(),
        reportsApi.getTeamVelocity(),
        reportsApi.getEstimationAccuracy()
      ])
      setDashboard(dashboardData)
      setTasks(tasksData)
      setProjects(projectsData)
      setVelocity(velocityData)
      setAccuracy(accuracyData)
    } catch (err) {
      setError('Failed to load dashboard. Make sure the API is running.')
      console.error(err)
    } finally {
      setLoading(false)
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

  const taskTypeData = dashboard.tasks.tasksByType.map(type => ({
    name: type.typeName,
    value: type.totalTasks,
    completed: type.completedTasks
  })).filter(d => d.value > 0)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Dashboard</h1>
        <p className="text-slate-500 dark:text-slate-400 mt-1">Overview of your team's performance and activities</p>
      </div>

      {/* Top Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Team Members"
          value={dashboard.team.totalDirectReports}
          icon={<Users className="w-6 h-6" />}
          color="blue"
        />
        <StatCard
          title="Active Projects"
          value={dashboard.tasks.projects.activeProjects}
          subtitle={`${dashboard.tasks.projects.totalProjects} total`}
          icon={<Target className="w-6 h-6" />}
          color="purple"
        />
        <StatCard
          title="Task Completion"
          value={`${dashboard.tasks.tasks.completionRate}%`}
          subtitle={`${dashboard.tasks.tasks.doneTasks} of ${dashboard.tasks.tasks.totalTasks} tasks`}
          icon={<CheckSquare className="w-6 h-6" />}
          color="green"
        />
        <StatCard
          title="Overdue Tasks"
          value={dashboard.tasks.tasks.overdueTasks}
          icon={<AlertTriangle className="w-6 h-6" />}
          color={dashboard.tasks.tasks.overdueTasks > 0 ? 'red' : 'green'}
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Project Distribution by Assignee */}
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
                    innerRadius={60}
                    outerRadius={80}
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
      </div>

      {/* Second Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Task Type Distribution */}
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
                    innerRadius={60}
                    outerRadius={80}
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
      </div>

      {/* Task Distribution by Assignee */}
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
              <Bar dataKey="overdue" stackId="a" fill="#ef4444" name="Overdue" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      {/* Team Velocity */}
      {velocity && velocity.sprints.length > 0 && (
        <Card>
          <CardHeader
            title="Team Velocity"
            subtitle={`Average: ${velocity.averageVelocity} SP per sprint | Trend: ${velocity.completionTrend > 0 ? '+' : ''}${velocity.completionTrend}%`}
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
                <p className="text-sm text-slate-500 dark:text-slate-400">Avg Velocity</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-green-500">
                  {Math.round(velocity.sprints.reduce((sum, s) => sum + s.totalTimeSpentMinutes, 0) / 60)}h
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Logged</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${velocity.completionTrend >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                  {velocity.completionTrend > 0 ? '+' : ''}{velocity.completionTrend}%
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Trend</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Estimation Accuracy */}
      {accuracy && accuracy.sprints.length > 0 && (
        <Card>
          <CardHeader
            title="Estimation Accuracy"
            subtitle={`Overall: ${accuracy.overallAccuracyPercentage}% | Variance: ${accuracy.totalVarianceHours > 0 ? '+' : ''}${accuracy.totalVarianceHours}h`}
          />
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={accuracy.sprints}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="sprintName" />
                <YAxis label={{ value: 'Hours', angle: -90, position: 'insideLeft' }} />
                <Tooltip />
                <Legend />
                <Bar dataKey="estimatedHours" fill="#3b82f6" name="Estimated" />
                <Bar dataKey="actualHours" fill="#10b981" name="Actual" />
              </BarChart>
            </ResponsiveContainer>
            <div className="mt-4 grid grid-cols-4 gap-4 pt-4 border-t dark:border-slate-700">
              <div className="text-center">
                <p className="text-2xl font-bold text-blue-500">{accuracy.totalEstimatedHours}h</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Estimated</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-green-500">{accuracy.totalActualHours}h</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Actual</p>
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
          </CardContent>
        </Card>
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
    </div>
  )
}
