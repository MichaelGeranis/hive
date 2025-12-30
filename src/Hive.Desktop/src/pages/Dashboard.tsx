import { useEffect, useState } from 'react'
import {
  Users,
  CheckSquare,
  AlertTriangle,
  TrendingUp,
  Clock,
  Target
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { reportsApi, tasksApi } from '../services/api'
import type { DashboardOverview, OneOnOneFrequency, TeamTask, TeamVelocity } from '../types'
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
  const [frequency, setFrequency] = useState<OneOnOneFrequency[]>([])
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [velocity, setVelocity] = useState<TeamVelocity | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadDashboard()
  }, [])

  const loadDashboard = async () => {
    try {
      setLoading(true)
      const [dashboardData, frequencyData, tasksData, velocityData] = await Promise.all([
        reportsApi.getDashboard(),
        reportsApi.getOneOnOneFrequency(),
        tasksApi.getAll(),
        reportsApi.getTeamVelocity()
      ])
      setDashboard(dashboardData)
      setFrequency(frequencyData)
      setTasks(tasksData)
      setVelocity(velocityData)
    } catch (err) {
      setError('Failed to load dashboard. Make sure the API is running.')
      console.error(err)
    } finally {
      setLoading(false)
    }
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

  const taskStatusData = [
    { name: 'Backlog', value: dashboard.tasks.tasks.backlogTasks },
    { name: 'Todo', value: dashboard.tasks.tasks.todoTasks },
    { name: 'In Progress', value: dashboard.tasks.tasks.inProgressTasks },
    { name: 'In Review', value: dashboard.tasks.tasks.inReviewTasks },
    { name: 'Done', value: dashboard.tasks.tasks.doneTasks },
  ].filter(d => d.value > 0)

  const tasksByAssigneeData = dashboard.tasks.tasksByAssignee.map(assignee => ({
    name: assignee.assigneeName || 'Unassigned',
    total: assignee.totalTasks,
    completed: assignee.completedTasks,
    inProgress: assignee.inProgressTasks,
    overdue: assignee.overdueTasks
  }))

  // Calculate project distribution by assignee
  const projectsByAssignee = tasks
    .filter(task => task.assigneeId && task.projectId) // Only tasks with both assignee and project
    .reduce((acc, task) => {
      const assigneeName = task.assigneeName || 'Unknown'
      if (!acc[assigneeName]) {
        acc[assigneeName] = new Set<string>()
      }
      acc[assigneeName].add(task.projectId!)
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
        {/* Task Status Distribution */}
        <Card>
          <CardHeader title="Tasks Distribution" subtitle="By status" />
          <CardContent className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={taskStatusData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={80}
                  paddingAngle={5}
                  dataKey="value"
                  label={({ name, value }) => `${name}: ${value}`}
                >
                  {taskStatusData.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

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
                    label={({ name, value }) => `${name}: ${value}`}
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

      {/* 1:1 Meeting Frequency */}
      <Card>
        <CardHeader
          title="1:1 Meetings Frequency"
          subtitle="Track regular check-ins with your team"
          action={
            <div className="flex gap-2 text-xs">
              <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded">On Track</span>
              <span className="px-2 py-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 rounded">At Risk</span>
              <span className="px-2 py-1 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 rounded">Overdue</span>
            </div>
          }
        />
        <CardContent>
          <div className="space-y-3">
            {frequency.length === 0 ? (
              <p className="text-slate-500 dark:text-slate-400 text-center py-4">No team members found</p>
            ) : (
              frequency.map((member) => (
                <div
                  key={member.directReportId}
                  className="flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-700/50 rounded-lg"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-slate-200 dark:bg-slate-600 rounded-full flex items-center justify-center text-slate-600 dark:text-slate-300 font-medium">
                      {member.directReportName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <p className="font-medium text-slate-900 dark:text-slate-100">{member.directReportName}</p>
                      <p className="text-sm text-slate-500 dark:text-slate-400">
                        {member.completedMeetings} meetings completed
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p className="text-sm text-slate-500 dark:text-slate-400">Last meeting</p>
                      <p className="font-medium text-slate-900 dark:text-slate-100">
                        {member.daysSinceLastMeeting >= 0
                          ? `${member.daysSinceLastMeeting} days ago`
                          : 'Never'}
                      </p>
                    </div>
                    <span
                      className={`px-3 py-1 rounded-full text-sm font-medium ${
                        member.frequencyStatus === 'On Track'
                          ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                          : member.frequencyStatus === 'At Risk'
                          ? 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400'
                          : member.frequencyStatus === 'Overdue'
                          ? 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
                          : 'bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {member.frequencyStatus}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>

      {/* Productivity & Reviews Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Productivity Metrics */}
        <Card>
          <CardHeader title="Productivity" subtitle="This period" />
          <CardContent>
            <div className="grid grid-cols-2 gap-4">
              <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-sm">
                  <TrendingUp className="w-4 h-4" />
                  Tasks This Week
                </div>
                <p className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">
                  {dashboard.tasks.productivity.tasksCompletedThisWeek}
                </p>
              </div>
              <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-sm">
                  <TrendingUp className="w-4 h-4" />
                  Tasks This Month
                </div>
                <p className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">
                  {dashboard.tasks.productivity.tasksCompletedThisMonth}
                </p>
              </div>
              <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-sm">
                  <Clock className="w-4 h-4" />
                  Avg Completion
                </div>
                <p className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">
                  {dashboard.tasks.productivity.averageTaskCompletionDays} days
                </p>
              </div>
              <div className="p-4 bg-slate-50 dark:bg-slate-700/50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-sm">
                  <Target className="w-4 h-4" />
                  Estimation Accuracy
                </div>
                <p className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">
                  {dashboard.tasks.productivity.estimationAccuracy}%
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Review Status */}
        <Card>
          <CardHeader title="Performance Reviews" subtitle="Current status" />
          <CardContent>
            <div className="space-y-4">
              <div className="flex justify-between items-center">
                <span className="text-slate-600 dark:text-slate-400">Total Reviews</span>
                <span className="font-bold text-slate-900 dark:text-slate-100">{dashboard.reviews.totalReviews}</span>
              </div>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-500 dark:text-slate-400">Completed</span>
                  <span className="text-slate-900 dark:text-slate-100">{dashboard.reviews.completionRate}%</span>
                </div>
                <div className="w-full bg-slate-100 dark:bg-slate-700 rounded-full h-2">
                  <div
                    className="bg-green-500 h-2 rounded-full"
                    style={{ width: `${dashboard.reviews.completionRate}%` }}
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4 pt-4 border-t dark:border-slate-700">
                <div className="text-center">
                  <p className="text-2xl font-bold text-amber-500">{dashboard.reviews.draftReviews}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Draft</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-blue-500">{dashboard.reviews.submittedReviews}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Submitted</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-purple-500">{dashboard.reviews.acknowledgedReviews}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Acknowledged</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-green-500">{dashboard.reviews.completedReviews}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">Completed</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Team Velocity */}
      {velocity && velocity.sprints.length > 0 && (
        <Card>
          <CardHeader 
            title="Team Velocity" 
            subtitle={`Average: ${velocity.averageVelocity} SP per sprint | Trend: ${velocity.completionTrend > 0 ? '+' : ''}${velocity.completionTrend}%`}
          />
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={velocity.sprints}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="sprintName" />
                <YAxis label={{ value: 'Story Points', angle: -90, position: 'insideLeft' }} />
                <Tooltip />
                <Legend />
                <Line 
                  type="monotone" 
                  dataKey="storyPointsCompleted" 
                  stroke="#f59e0b" 
                  strokeWidth={2}
                  name="Story Points Completed"
                />
              </LineChart>
            </ResponsiveContainer>
            <div className="mt-4 grid grid-cols-3 gap-4 pt-4 border-t dark:border-slate-700">
              <div className="text-center">
                <p className="text-2xl font-bold text-amber-500">{velocity.totalStoryPointsCompleted}</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Total Story Points</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-blue-500">{velocity.averageVelocity}</p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Avg Velocity</p>
              </div>
              <div className="text-center">
                <p className={`text-2xl font-bold ${velocity.completionTrend >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                  {velocity.completionTrend > 0 ? '+' : ''}{velocity.completionTrend}%
                </p>
                <p className="text-sm text-slate-500 dark:text-slate-400">Sprint Trend</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
