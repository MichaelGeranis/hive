import { useEffect, useState } from 'react'
import {
  Users,
  Star,
  Calendar,
  CheckSquare,
  AlertTriangle,
  TrendingUp,
  Clock,
  Target
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { reportsApi } from '../services/api'
import type { DashboardOverview, OneOnOneFrequency } from '../types'
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
  Cell
} from 'recharts'

const COLORS = ['#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ef4444']

export default function Dashboard() {
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null)
  const [frequency, setFrequency] = useState<OneOnOneFrequency[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadDashboard()
  }, [])

  const loadDashboard = async () => {
    try {
      setLoading(true)
      const [dashboardData, frequencyData] = await Promise.all([
        reportsApi.getDashboard(),
        reportsApi.getOneOnOneFrequency()
      ])
      setDashboard(dashboardData)
      setFrequency(frequencyData)
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
      <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
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

  const ratingData = dashboard.reviews.ratingDistribution.filter(r => r.count > 0)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-slate-500 mt-1">Overview of your team's performance and activities</p>
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
          <CardHeader title="Task Distribution" subtitle="By status" />
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

        {/* Rating Distribution */}
        <Card>
          <CardHeader title="Performance Ratings" subtitle="Distribution across reviews" />
          <CardContent className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={ratingData} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis type="number" />
                <YAxis dataKey="ratingName" type="category" width={120} />
                <Tooltip />
                <Bar dataKey="count" fill="#f59e0b" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* 1:1 Meeting Frequency */}
      <Card>
        <CardHeader
          title="1:1 Meeting Frequency"
          subtitle="Track regular check-ins with your team"
          action={
            <div className="flex gap-2 text-xs">
              <span className="px-2 py-1 bg-green-100 text-green-700 rounded">On Track</span>
              <span className="px-2 py-1 bg-amber-100 text-amber-700 rounded">At Risk</span>
              <span className="px-2 py-1 bg-red-100 text-red-700 rounded">Overdue</span>
            </div>
          }
        />
        <CardContent>
          <div className="space-y-3">
            {frequency.length === 0 ? (
              <p className="text-slate-500 text-center py-4">No team members found</p>
            ) : (
              frequency.map((member) => (
                <div
                  key={member.directReportId}
                  className="flex items-center justify-between p-3 bg-slate-50 rounded-lg"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-slate-200 rounded-full flex items-center justify-center text-slate-600 font-medium">
                      {member.directReportName.split(' ').map(n => n[0]).join('')}
                    </div>
                    <div>
                      <p className="font-medium text-slate-900">{member.directReportName}</p>
                      <p className="text-sm text-slate-500">
                        {member.completedMeetings} meetings completed
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p className="text-sm text-slate-500">Last meeting</p>
                      <p className="font-medium">
                        {member.daysSinceLastMeeting >= 0
                          ? `${member.daysSinceLastMeeting} days ago`
                          : 'Never'}
                      </p>
                    </div>
                    <span
                      className={`px-3 py-1 rounded-full text-sm font-medium ${
                        member.frequencyStatus === 'On Track'
                          ? 'bg-green-100 text-green-700'
                          : member.frequencyStatus === 'At Risk'
                          ? 'bg-amber-100 text-amber-700'
                          : member.frequencyStatus === 'Overdue'
                          ? 'bg-red-100 text-red-700'
                          : 'bg-slate-100 text-slate-700'
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
              <div className="p-4 bg-slate-50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 text-sm">
                  <TrendingUp className="w-4 h-4" />
                  Tasks This Week
                </div>
                <p className="text-2xl font-bold mt-1">
                  {dashboard.tasks.productivity.tasksCompletedThisWeek}
                </p>
              </div>
              <div className="p-4 bg-slate-50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 text-sm">
                  <TrendingUp className="w-4 h-4" />
                  Tasks This Month
                </div>
                <p className="text-2xl font-bold mt-1">
                  {dashboard.tasks.productivity.tasksCompletedThisMonth}
                </p>
              </div>
              <div className="p-4 bg-slate-50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 text-sm">
                  <Clock className="w-4 h-4" />
                  Avg Completion
                </div>
                <p className="text-2xl font-bold mt-1">
                  {dashboard.tasks.productivity.averageTaskCompletionDays} days
                </p>
              </div>
              <div className="p-4 bg-slate-50 rounded-lg">
                <div className="flex items-center gap-2 text-slate-500 text-sm">
                  <Target className="w-4 h-4" />
                  Estimation Accuracy
                </div>
                <p className="text-2xl font-bold mt-1">
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
                <span className="text-slate-600">Total Reviews</span>
                <span className="font-bold">{dashboard.reviews.totalReviews}</span>
              </div>
              <div className="space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-500">Completed</span>
                  <span>{dashboard.reviews.completionRate}%</span>
                </div>
                <div className="w-full bg-slate-100 rounded-full h-2">
                  <div
                    className="bg-green-500 h-2 rounded-full"
                    style={{ width: `${dashboard.reviews.completionRate}%` }}
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4 pt-4 border-t">
                <div className="text-center">
                  <p className="text-2xl font-bold text-amber-500">{dashboard.reviews.draftReviews}</p>
                  <p className="text-sm text-slate-500">Draft</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-blue-500">{dashboard.reviews.submittedReviews}</p>
                  <p className="text-sm text-slate-500">Submitted</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-purple-500">{dashboard.reviews.acknowledgedReviews}</p>
                  <p className="text-sm text-slate-500">Acknowledged</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-green-500">{dashboard.reviews.completedReviews}</p>
                  <p className="text-sm text-slate-500">Completed</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
