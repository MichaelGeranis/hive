import { useEffect, useState, useCallback } from 'react'
import {
  Users,
  CheckSquare,
  AlertTriangle,
  Target,
  X,
  FolderKanban,
  TrendingUp
} from 'lucide-react'
import { Card, CardHeader, CardContent, StatCard } from '../components/Card'
import { reportsApi, tasksApi, projectsApi, leavesApi } from '../services/api'
import type { DashboardOverview, TeamTask, TeamVelocity, EstimationAccuracy, Project, CapacityAnalysis, TeamLeaveOverview, SprintCapacityAnalysis } from '../types'
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

export default function Dashboard() {
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null)
  const [tasks, setTasks] = useState<TeamTask[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [velocity, setVelocity] = useState<TeamVelocity | null>(null)
  const [accuracy, setAccuracy] = useState<EstimationAccuracy | null>(null)
  const [capacityAnalysis, setCapacityAnalysis] = useState<CapacityAnalysis | null>(null)
  const [leaveOverview, setLeaveOverview] = useState<TeamLeaveOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedMember, setSelectedMember] = useState<string | null>(null)
  const [memberProjects, setMemberProjects] = useState<Project[]>([])
  const [sprintFilter, setSprintFilter] = useState<number | undefined>(3) // Default: Last 3 sprints

  const closeModal = useCallback(() => setSelectedMember(null), [])
  useEscapeKey(closeModal, !!selectedMember)

  useEffect(() => {
    loadDashboard()
  }, [sprintFilter]) // Re-fetch when filter changes

  const loadDashboard = async () => {
    try {
      setLoading(true)
      const [dashboardData, tasksData, projectsData, velocityData, accuracyData, capacityData, leaveData] = await Promise.all([
        reportsApi.getDashboard(sprintFilter),
        tasksApi.getAll(),
        projectsApi.getAll(),
        reportsApi.getTeamVelocity(sprintFilter),
        reportsApi.getEstimationAccuracy(sprintFilter),
        reportsApi.getCapacityAnalysis(sprintFilter),
        leavesApi.getOverview()
      ])
      setDashboard(dashboardData)
      setTasks(tasksData)
      setProjects(projectsData)
      setVelocity(velocityData)
      setAccuracy(accuracyData)
      setCapacityAnalysis(capacityData)
      setLeaveOverview(leaveData)
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
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Dashboard</h1>
        <p className="text-slate-500 dark:text-slate-400 mt-1">Overview team performance & activities</p>
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
            <option value="3">Last 3 sprints</option>
            <option value="6">Last 6 sprints</option>
            <option value="12">Last 12 sprints</option>
            <option value="all">All sprints</option>
          </select>
        </div>
      </div>

      {/* Top Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
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
         {capacityAnalysis?.currentSprint && (
          <StatCard
            title="Current Sprint"
            value={`${capacityAnalysis.currentSprint.utilizationPercentage ?? 0}%`}
            subtitle={`${capacityAnalysis.currentSprint.completedPoints ?? 0}/${capacityAnalysis.currentSprint.committedPoints ?? 0} SP`}
            icon={<TrendingUp className="w-6 h-6" />}
            color={(capacityAnalysis.currentSprint.utilizationPercentage ?? 0) > 100 ? 'red' : (capacityAnalysis.currentSprint.utilizationPercentage ?? 0) > 80 ? 'amber' : 'blue'}
          />
        )}
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

      {/* Distribution Charts Row - 3 columns */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Projects Distribution by Member */}
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

        {/* Members Distribution by Project - Knowledge Silos */}
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
        </Card>
      </div>

      {/* Capacity Analysis */}
      {capacityAnalysis && dashboard && leaveOverview && (capacityAnalysis.pastSprints.length > 0 || capacityAnalysis.currentSprint || capacityAnalysis.futureSprints.length > 0) && (() => {
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
              title="Sprint Capacity Analysis"
              subtitle={`Average Utilization: ${capacityAnalysis.averageUtilization ?? 0}%. Average Completed SP: ${avgCompletedSP}. Available Members: ${availableMembers}/${totalMembers} (${membersOnLeave} on leave).`}
            />
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData}>
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
                  <Bar dataKey="committedPoints" name="Committed SP">
                    {chartData.map((entry, index) => (
                      <Cell
                        key={`cell-committed-${index}`}
                        fill={entry.isPredicted ? '#c084fc' : entry.isFuture ? '#a78bfa' : entry.isCurrent ? '#60a5fa' : '#3b82f6'}
                      />
                    ))}
                  </Bar>
                  <Bar dataKey="completedPoints" name="Completed SP">
                    {chartData.map((entry, index) => (
                      <Cell
                        key={`cell-completed-${index}`}
                        fill={entry.isPredicted ? '#a855f7' : entry.isFuture ? '#8b5cf6' : entry.isCurrent ? '#34d399' : '#10b981'}
                      />
                    ))}
                  </Bar>
                </BarChart>
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
            </CardContent>
          </Card>
        );
      })()}

      {/* Estimation Accuracy */}
      {accuracy && accuracy.sprints.length > 0 && (
        <Card>
          <CardHeader
            title="Estimation Accuracy"
            subtitle={`from all tasks`}
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
      
      
      {/* Team Velocity */}
      {velocity && velocity.sprints.length > 0 && (
        <Card>
          <CardHeader
            title="Team Velocity"
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


      {/* Task Distribution by Assignee - Members Workload*/}
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
